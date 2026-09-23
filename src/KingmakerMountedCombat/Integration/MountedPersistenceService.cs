using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using KingmakerMountedCombat.Diagnostics;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;

namespace KingmakerMountedCombat.Integration
{
    // Game-thread serialization/rehydration service. Archive metadata never owns
    // an actor, command, view or scheduler object across a world boundary.
    internal sealed partial class MountedPersistenceService
    {
        private readonly GameMountedRelationshipService relationship;
        private readonly NativeMountedControlService controls;
        private readonly DiagnosticSettings settings;
        private readonly IModLogger logger;
        private SaveScope activeSave;
        // A save whose wrapper was disposed early while its archive worker was
        // still able to commit. Its protections stay held until the worker
        // settles, so this is never null at the same time as a released scope.
        private SaveScope drainingSave;

        internal int DeferredSaveCancellationCount { get; private set; }
        internal int DrainedSaveCount { get; private set; }
        internal bool LastDrainedSaveCommitted { get; private set; }
        // False when a commit landed that was not this operation's: the outcome
        // is then reported as unconfirmed rather than as either success or an
        // unchanged previous archive.
        internal bool LastDrainedSaveOutcomeEstablished { get; private set; } = true;
        // Where the interrupted save actually landed. The commit replaces the
        // target archive in place and rebinds the path, so this is not the
        // prepared leaf name joined to the save root.
        internal string LastDrainedSavePath { get; private set; }
        internal bool SaveDraining => drainingSave != null;
        // The overlap guard reads this. A draining save must still own it, so a
        // second serialization cannot begin over a worker that can still commit.
        internal bool HasActiveSaveScope => activeSave != null;
        // Per-operation identity, so a test can prove THIS save's worker was
        // still running when interruption was requested rather than relying on a
        // cumulative count.
        internal string ActiveSaveLeaf => (activeSave ?? drainingSave)?.Prepared?.FileName;
        internal int ActiveSaveWorkerId
        {
            get { System.Threading.Tasks.Task task; ResolveWorker(activeSave ?? drainingSave, out task); return task == null ? -1 : task.Id; }
        }
        internal bool ActiveSaveWorkerRunning
        {
            get { System.Threading.Tasks.Task task; ResolveWorker(activeSave ?? drainingSave, out task); return task != null && !task.IsCompleted; }
        }
        // The RAW latch, with no resolution. A test uses this to prove an
        // interruption was requested while the worker was live but had not yet
        // been observed by the wrapper, which is the window a cached-null read
        // would have released.
        internal bool ActiveSaveWorkerCached => (activeSave ?? drainingSave)?.Worker != null;
        internal int TeardownDrainCount { get; private set; }
        internal bool LastTeardownDrainSettled { get; private set; } = true;
        // True only while an owned world replacement is actually in flight: this
        // service holds a load scope AND the engine reports the load running.
        // Cleanup here would tear down the pair across a world that is being
        // replaced underneath it. Once the engine finishes loading this goes
        // false even if presentation is still pending, because a disable then is
        // safe: Update refuses to present into a disabled mod.
        // The Instance null check is not redundant: this is read from the disable
        // and session-stop paths, which can run when the engine is already gone,
        // and an exception there would be caught as "cleanup retained residue"
        // and block the very teardown it is meant to protect.
        internal bool LoadInFlight =>
            restoreLoad != null && LoadingProcess.Instance != null && LoadingProcess.Instance.IsLoadingInProcess;
        private long loadSequence;
        private LoadScope restoreLoad;
        private MountedSaveReadResult loaded;
        private string selectedCampaign;
        private bool presentationPending;
        private readonly Dictionary<string, UnitEntityData> restoredActors =
            new Dictionary<string, UnitEntityData>(StringComparer.Ordinal);

        internal bool Enabled { get; set; }
        internal bool SaveSuspended => relationship.SaveSerializationSuspended;
        internal string Feedback { get; private set; } = "No mounted save has been loaded.";
        internal int SnapshotCount { get; private set; }
        internal event Action SaveSnapshotStarting;
        internal event Action SaveSnapshotStaged;
        internal int SemanticRestoreCount { get; private set; }
        internal int PresentationRestoreCount { get; private set; }
        // A native load that throws after Game.DisposeState has already destroyed
        // the previous world leaves no completed world behind. Record that it
        // happened so a failed load can never be accounted as a finished one; the
        // native error itself is preserved and rethrown untouched.
        internal int NativeLoadFailureCount { get; private set; }
        internal string NativeLoadFailure { get; private set; }
        internal MountedSaveData LoadedData => loaded?.Data;

        internal MountedPersistenceService(GameMountedRelationshipService relationship,
            NativeMountedControlService controls, UnifiedMountedTurnCoordinator unifiedTurn,
            DiagnosticSettings settings, IModLogger logger)
        {
            this.relationship = relationship;
            this.controls = controls;
            this.unifiedTurn = unifiedTurn;
            this.settings = settings;
            this.logger = logger;
        }

        internal IEnumerator<object> WrapSaveRoutine(IEnumerator<object> routine, SaveInfo requestedSave)
        {
            var scope = new SaveScope { RequestedPath = requestedSave?.FolderName };
            var scoped = new ScopedEnumerator<object>(TrackNativeSave(routine, scope), () =>
            {
                if (activeSave != null) throw new InvalidOperationException("Overlapping native save enumerations.");
                // The operation starts here, after every earlier queued save has
                // finished, so this is where its commit bracket and the state of
                // its slot are taken.
                scope.Began = true;
                scope.CommitsAtStart = NativeMountedArchiveCommit.CommitCount;
                scope.PreviousExisted = !string.IsNullOrEmpty(scope.RequestedPath) &&
                    System.IO.File.Exists(scope.RequestedPath);
                activeSave = scope;
            }, () =>
            {
                // A native StopAll disposes this wrapper without the archive
                // worker having finished, and disposal never reaches the
                // completion path's wait. Releasing here would resume AI,
                // controls and serialization while the worker can still commit,
                // and would clear the overlap guard. Nothing can cancel a started
                // worker, so defer the release and let Update drain it; the
                // outcome is only reported once the worker settles.
                System.Threading.Tasks.Task pending;
                var settled = ResolveWorker(scope, out pending);
                if (ReferenceEquals(activeSave, scope) && !NativeSaveWorkerBoundary.CanReleaseScope(settled, pending))
                {
                    if (drainingSave == null)
                    {
                        drainingSave = scope;
                        DeferredSaveCancellationCount++;
                        NotifySaveStatus("This save is already writing and cannot be canceled; " +
                            "finishing it before mounted controls resume.");
                    }
                    return;
                }
                ReleaseSaveScope(scope);
            });
            var fault = diagnosticWait?.TryClaim(requestedSave) == true ? diagnosticWait : null;
            var operation = Enabled ? DeferNativeSave(scoped, fault) : scoped;
            // The object handed to the native queue is the key a retired failure
            // comes back with, so its outcome is described from its own scope.
            ScopesByOperation.Add(operation, scope);
            return operation;
        }

        // Operation-keyed, never process-global: queued saves are wrapped long
        // before they run, and only the wrapper the native queue retires can say
        // which operation actually failed.
        private System.Runtime.CompilerServices.ConditionalWeakTable<object, SaveScope> scopesByOperation;
        private System.Runtime.CompilerServices.ConditionalWeakTable<object, SaveScope> ScopesByOperation =>
            scopesByOperation ?? (scopesByOperation = new System.Runtime.CompilerServices.ConditionalWeakTable<object, SaveScope>());

        // Saves whose archive was committed and whose later cleanup, descriptor
        // rebinding or notification then failed. They are reported as written,
        // not as failed, and are counted here rather than in FailedSaveCount.
        internal int CommittedThenFailedCount { get; private set; }

        // The single factual description shared by ordinary completion, an
        // abandoned operation that drained, and a commit whose cleanup failed.
        private NativeSaveOutcomeReport DescribeOutcome(SaveScope scope, string detail)
        {
            // An operation that never began its routine cannot have committed,
            // and the state of its slot now IS its previous state.
            var kind = !scope.Began
                ? NativeSaveCommitKind.NotWritten
                : NativeSaveCommitOutcome.Decide(scope.CommitsAtStart, NativeMountedArchiveCommit.CommitCount,
                    NativeMountedArchiveCommit.LastCommittedDestination, scope.RequestedPath,
                    scope.Prepared?.FolderName, System.IO.File.Exists);
            var present = !string.IsNullOrEmpty(scope.RequestedPath) && System.IO.File.Exists(scope.RequestedPath);
            var previousExisted = scope.Began ? scope.PreviousExisted : present;
            return NativeSaveOutcomeReport.Describe(kind, previousExisted, present, detail);
        }

        internal NativeSaveOutcomeReport DescribeFailedOperation(object operation, Exception exception)
        {
            SaveScope scope;
            if (operation == null || !ScopesByOperation.TryGetValue(operation, out scope))
            {
                // Not an operation this service wrapped: nothing about its
                // archive is known, and nothing about it may be promised.
                return NativeSaveOutcomeReport.Describe(NativeSaveCommitKind.Unconfirmed, false, false, exception?.Message);
            }
            return DescribeOutcome(scope, exception?.Message);
        }

        private static IEnumerator<object> TrackNativeSave(IEnumerator<object> routine, SaveScope scope)
        {
            scope.Routine = routine;
            using (routine)
            {
                while (true)
                {
                    NativeSaveWorkerBoundary.RestoreCompletedPlayerReference(routine, scope.World, scope.PartyState);
                    // The step itself publishes the worker, so the cache is
                    // refreshed on BOTH sides of it. Reading only before MoveNext
                    // leaves a window where the worker exists but the cache is
                    // still null, and an early disposal in that window would
                    // treat "no cached task" as "no worker" and release.
                    CaptureWorker(routine, scope);
                    bool moved;
                    // The publishing store happens inside this step, so the latch
                    // is taken in a finally: a step that creates the worker and
                    // then throws must not leave ownership unobserved.
                    try { moved = routine.MoveNext(); }
                    finally { CaptureWorker(routine, scope); }
                    if (!moved) break;
                    yield return routine.Current;
                }
            }
            if (scope.Json == null)
                throw new CompletedSaveFailureException("Native save ended before its snapshot; no completed write is reported.");
            // Native SaveRoutine can finish its TurnOn phase while the archive
            // worker is still committing. Keep completion/controls scoped until
            // the real worker has finished, rather than reporting that callback.
            var task = NativeSaveWorkerBoundary.TaskOf(routine);
            if (task == null) throw new InvalidOperationException("Native snapshot did not start its save worker.");
            while (!task.IsCompleted) yield return null;
            NativeSaveWorkerBoundary.RestoreCompletedPlayerReference(routine, scope.World, scope.PartyState);
            if (!ReferenceEquals(Game.Instance?.Player, scope.World))
                throw new InvalidOperationException("Native save world changed before completion.");
            if (task.IsFaulted || task.IsCanceled || scope.Prepared.OperationState != SaveInfo.StateType.None ||
                string.IsNullOrEmpty(scope.Prepared.FolderName) || !System.IO.File.Exists(scope.Prepared.FolderName))
                throw new CompletedSaveFailureException("The native archive worker did not commit the requested save.");
        }

        // Latching: once a worker has been seen it is never forgotten, so a later
        // unreadable routine cannot turn established ownership back into "none".
        private static void CaptureWorker(IEnumerator<object> routine, SaveScope scope)
        {
            if (scope.Worker != null) return;
            System.Threading.Tasks.Task worker;
            if (NativeSaveWorkerBoundary.TryReadWorker(routine, out worker) && worker != null)
                scope.Worker = worker;
        }

        // Resolved at the decision point, not trusted from the cache: the step
        // that publishes the worker may have run since the cache was last
        // refreshed, so a null cached task is never proof that no worker exists.
        // Reports whether the answer was actually established; an unreadable
        // routine yields false so the caller defers rather than releasing.
        private static bool ResolveWorker(SaveScope scope, out System.Threading.Tasks.Task worker)
        {
            worker = scope?.Worker;
            if (worker != null) return true;
            // No scope, or one whose enumeration never began, cannot own a
            // worker: that is established, not unknown. Only a read that fails
            // leaves the question open.
            if (scope?.Routine == null) return true;
            if (!NativeSaveWorkerBoundary.TryReadWorker(scope.Routine, out worker)) return false;
            if (worker != null) scope.Worker = worker;
            return true;
        }

        private void ReleaseSaveScope(SaveScope scope)
        {
            // Exactly once, whichever path gets here first: the end action of
            // an ordinary enumeration, the per-frame drain, or teardown.
            if (scope == null || scope.Released) return;
            scope.Released = true;
            try
            {
                try { scope.RestoreAi?.Invoke(); }
                finally { if (scope.ControlsSuspended) controls.EndSaveSerializationScope(); }
            }
            finally
            {
                if (ReferenceEquals(activeSave, scope))
                {
                    relationship.SaveSerializationSuspended = false;
                    activeSave = null;
                }
                if (ReferenceEquals(drainingSave, scope)) drainingSave = null;
            }
        }

        // Teardown only. Disposal unpatches the archive-commit transpiler and
        // runs a full cleanup over the exact live graphs an owned worker is
        // serializing, and unlike the per-frame drain there is no later frame in
        // which to finish. Nothing can cancel a started worker, so wait for it —
        // bounded, so a stuck worker can never hang the game's own unload, and
        // reporting whether it actually settled rather than assuming it did.
        // The verdict must be CONSUMED by the caller: Refused means nothing was
        // released and nothing may be unpatched or cleaned up. A bounded wait
        // expiring is not evidence that cleanup is safe, and ownership that could
        // not be established never authorizes it. Logger-free on purpose, so the
        // decision is testable on a bare service; the caller reports.
        internal OwnedWorkerTeardownVerdict DrainForTeardown(int milliseconds)
        {
            var scope = activeSave ?? drainingSave;
            System.Threading.Tasks.Task worker = null;
            var established = scope != null && ResolveWorker(scope, out worker);
            if (scope != null) TeardownDrainCount++;
            var verdict = OwnedWorkerTeardownPolicy.Decide(scope != null, established, worker,
                NativeSaveWorkerBoundary.WaitForWorkerSettlement, milliseconds);
            LastTeardownDrainSettled = verdict != OwnedWorkerTeardownVerdict.Refused;
            if (verdict == OwnedWorkerTeardownVerdict.Settled) FinalizeSettledScope(scope);
            return verdict;
        }

        // The worker, if there was one, has settled, so nothing on another
        // thread reads the live graphs any more. Finalize exactly once whichever
        // scope owns the operation: a draining one reports its truthful outcome;
        // an active one that was never abandoned is released here, because
        // teardown leaves no later frame for its own end action, which then
        // finds the scope already released and does nothing.
        private void FinalizeSettledScope(SaveScope scope)
        {
            if (scope == null || scope.Released) return;
            if (ReferenceEquals(drainingSave, scope)) { DrainAbandonedSave(); return; }
            ReleaseSaveScope(scope);
        }

        // Non-blocking: called once per frame so required completion work keeps
        // running while the abandoned worker finishes on its own thread.
        private void DrainAbandonedSave()
        {
            var scope = drainingSave;
            // Nothing is draining on an ordinary frame, and that is not the same
            // question as whether a scope owns a worker: resolving a null scope
            // legitimately answers "no worker", so this must return before that.
            if (scope == null) return;
            System.Threading.Tasks.Task worker;
            var established = ResolveWorker(scope, out worker);
            // A deferral taken while ownership was unknown resolves here: once it
            // is established that no worker was ever created, waiting can never
            // settle anything, so release instead of holding the scope forever.
            if (established && worker == null) { ReleaseSaveScope(scope); return; }
            if (worker == null || !worker.IsCompleted) return;
            // Whether the archive was written is decided at the actual commit
            // boundary, not inferred from the task. A worker that committed and
            // then faulted in cleanup, descriptor rebinding or notification has
            // still written the save, and a faulted task alone never establishes
            // that the previous archive is untouched.
            var landed = NativeMountedArchiveCommit.LastCommittedDestination;
            var report = DescribeOutcome(scope, null);
            var committed = report.Kind == NativeSaveCommitKind.Committed;
            var outcomeEstablished = report.Kind != NativeSaveCommitKind.Unconfirmed;
            try
            {
                // The worker temporarily clears this reference; the completion
                // path restores it, and an abandoned one must do the same.
                if (scope.Routine != null)
                    NativeSaveWorkerBoundary.RestoreCompletedPlayerReference(scope.Routine, scope.World, scope.PartyState);
            }
            catch (Exception exception) { logger.Exception("Abandoned save could not restore its native world reference", exception); }
            DrainedSaveCount++;
            LastDrainedSaveCommitted = committed;
            LastDrainedSaveOutcomeEstablished = outcomeEstablished;
            // Where the save actually landed, which the commit boundary knows and
            // the prepared leaf does not: the commit replaces its target in place
            // and rebinds the path.
            LastDrainedSavePath = committed ? landed : null;
            if (report.CountsAsFailed) FailedSaveCount++;
            ReleaseSaveScope(scope);
            // Same factual report as the ordinary completion path; only the
            // prefix says this one was interrupted.
            NotifySaveStatus("Interrupted save: " + report.Message);
        }

        internal void ObservePreparedSave(SaveInfo save)
        {
            if (activeSave == null) return;
            if (activeSave.Prepared != null)
                throw new InvalidOperationException("A native save enumeration allocated two descriptors.");
            activeSave.Prepared = save;
        }

        internal void BeforeNativeHeader(ISaver saver, string name)
        {
            var scope = activeSave;
            if (scope == null || name != "header" || !ReferenceEquals(scope.Prepared?.Saver, saver)) return;
            if (scope.Json != null) throw new InvalidOperationException("A native save crossed the snapshot barrier twice.");
            // This call is in SaveRoutine's game-thread header block, before
            // TurnOff/PreSave or any entity serialization worker is started.
            scope.World = Game.Instance.Player;
            scope.PartyState = scope.World.CrossSceneState;
            SaveSnapshotStarting?.Invoke();
            if (loaded != null && loaded.Kind != MountedSaveReadKind.Current && loaded.Kind != MountedSaveReadKind.Missing)
            {
                if (loaded.OriginalJson == null)
                    throw new InvalidOperationException("Cannot preserve unreadable mounted metadata; source save remains intact.");
                scope.Json = loaded.OriginalJson;
            }
            else scope.Json = MountedSaveCodec.Encode(Capture());
            scope.ControlsSuspended = true;
            relationship.SaveSerializationSuspended = true;
            if (!controls.BeginSaveSerializationScope())
                throw new InvalidOperationException("Mounted controls could not enter native serialization scope.");
            scope.RestoreAi = relationship.Runtime.SuspendSerializedAiLease();
            NativeMountedSaveStorage.Stage(saver, scope.Json);
            SnapshotCount++;
            SaveSnapshotStaged?.Invoke();
            logger.Info("Mounted immutable snapshot staged at native header barrier; capture=" + SnapshotCount + ".");
        }

        private MountedSaveData Capture()
        {
            var game = Game.Instance;
            if (game?.Player == null || game.CurrentlyLoadedArea == null)
                throw new InvalidOperationException("No native world exists at the mounted save barrier.");
            var mounted = Enabled && relationship.State == RelationshipState.Mounted;
            if (mounted && (!settings.EnablePairedActivation || settings.EnableUnifiedMountedTurn || settings.EnablePairedCommandScheduler))
                throw new InvalidOperationException("Mounted saving requires the qualified paired policy.");
            var combat = Enabled && game.Player.IsInCombat ? CaptureCombat() : null;
            return new MountedSaveData
            {
                SchemaVersion = MountedSaveData.CurrentSchema,
                CampaignId = game.Player.GameId,
                AreaId = game.CurrentlyLoadedArea.AssetGuidThreadSafe,
                GameTimeTicks = game.TimeController.GameTime.Ticks,
                Policy = MountedSaveData.PairedPolicy,
                RulesId = MountedSaveData.Rules,
                Mounted = mounted,
                ProfileId = mounted ? relationship.Runtime.MountProfileId : null,
                Rider = mounted ? CaptureActor(relationship.Rider) : null,
                Mount = mounted ? CaptureActor(relationship.Mount) : null,
                Slots = controls.CapturePersistentSlots(),
                Combat = combat
            };
        }

        internal static SavedNativeActor CaptureActor(UnitEntityData unit)
        {
            var state = unit.CombatState;
            return new SavedNativeActor
            {
                Id = unit.UniqueId,
                Standard = state.Cooldown.StandardAction,
                Move = state.Cooldown.MoveAction,
                Swift = state.Cooldown.SwiftAction,
                Initiative = state.Cooldown.Initiative,
                Reaction = state.Cooldown.AttackOfOpportunity,
                ReactionsRemaining = state.AttackOfOpportunityCount,
                LastSurpriseTicks = state.LastSurpriseActionTime.Ticks
            };
        }

        internal IEnumerator<object> WrapLoadRoutine(IEnumerator<object> routine, SaveInfo save)
        {
            CancelAreaTransition();
            var scope = new LoadScope { Sequence = ++loadSequence };
            // Queueing B invalidates unfinished restoration of A immediately.
            // A completed world's save semantics remain valid until B actually
            // starts; disposing an unstarted iterator cannot erase that world.
            if (restoreLoad != null && (!restoreLoad.World.NativeCompleted || presentationPending))
            {
                FenceAbandonedCombat();
                restoreLoad.World.Close();
                restoreLoad = null;
                presentationPending = false;
                restoredActors.Clear();
                loaded = null;
            }
            return new ScopedEnumerator<object>(TrackNativeLoad(routine, scope), () =>
            {
                if (scope.Sequence != loadSequence) return;
                restoreLoad?.World.Close();
                presentationPending = false;
                restoredActors.Clear();
                loaded = null;
                restoreLoad = scope;
                scope.World.Begin(Game.Instance?.Player);
                BeginLoadHousekeeping();
                SelectLoad(save);
            }, () =>
            {
                if (scope.Sequence != loadSequence) { scope.World.Close(); return; }
                if (!scope.World.NativeCompleted)
                {
                    FenceAbandonedCombat();
                    scope.World.Close();
                    presentationPending = false;
                    restoredActors.Clear();
                    loaded = null;
                    if (combatRestoreFailure == null)
                        Report("Native load was canceled or failed; unfinished mounted restoration discarded.");
                }
            });
        }

        private IEnumerator<object> TrackNativeLoad(IEnumerator<object> routine, LoadScope scope)
        {
            using (routine)
            {
                while (true)
                {
                    bool moved;
                    // Only the native step is guarded, and the original exception
                    // is rethrown unchanged: observing a failure must not alter
                    // it, swallow it, or let the scope complete.
                    try { moved = routine.MoveNext(); }
                    catch (Exception exception) { ObserveNativeLoadFailure(exception); throw; }
                    if (!moved) break;
                    yield return routine.Current;
                }
            }
            // A failing native Dispose cannot turn an abandoned load into a
            // completed presentation scope.
            if (scope.Sequence == loadSequence) scope.World.Complete(Game.Instance?.Player);
        }

        internal void ObserveNativeLoadFailure(Exception exception)
        {
            NativeLoadFailureCount++;
            NativeLoadFailure = exception.GetType().Name + ": " + exception.Message;
            logger.Exception("Native load failed after the previous world was disposed", exception);
        }

        private void SelectLoad(SaveInfo save)
        {
            selectedCampaign = save.GameId;
            loaded = NativeMountedSaveStorage.Read(save.Saver);
            if (loaded.Kind == MountedSaveReadKind.Current &&
                (loaded.Data.CampaignId != save.GameId || loaded.Data.AreaId != save.Area?.AssetGuidThreadSafe))
                loaded = new MountedSaveReadResult(MountedSaveReadKind.Invalid, null, loaded.OriginalJson,
                    "Mounted metadata campaign/area differs from the selected native archive.");
            presentationPending = loaded.Kind == MountedSaveReadKind.Current;
            Feedback = loaded.Feedback ?? "Mounted save selected; awaiting native entities.";
            logger.Info(Feedback);
        }

        internal void RestoreActorAfterPostLoad(UnitEntityData unit)
        {
            var data = loaded?.Data;
            if (!Enabled || data == null || (!data.Mounted && data.Combat == null) || unit == null || restoreLoad == null ||
                restoreLoad.Sequence != loadSequence || !restoreLoad.World.TryBind(Game.Instance?.Player)) return;
            // SaveManager publishes GameId only AFTER PlayerState.PostLoad. The
            // selected archive/header and newly deserialized world own this phase;
            // the published campaign is checked before presentation/admission.
            var combatActor = data.Combat?.Actors.SingleOrDefault(a => a.Native.Id == unit.UniqueId);
            var saved = combatActor?.Native ?? (unit.UniqueId == data.Rider?.Id ? data.Rider :
                unit.UniqueId == data.Mount?.Id ? data.Mount : null);
            if (saved == null) return;
            if (restoredActors.TryGetValue(unit.UniqueId, out var prior))
            {
                if (!ReferenceEquals(prior, unit))
                    throw new InvalidOperationException("Loaded mounted actor ID is not unique.");
                return;
            }
            if (combatActor != null)
            {
                try { NativeCombatActorPersistence.RestoreActor(unit, combatActor, data.GameTimeTicks); }
                catch (System.IO.InvalidDataException exception)
                {
                    // Installed semantic validation precedes JoinCombat/Prepare.
                    // Preserve independently valid expenditure without accepting
                    // an unknown AI action or granting native participation.
                    NativeCombatActorPersistence.RestoreDebt(unit, saved);
                    BlockCombatRestoration("An actor's saved semantic references are incompatible: " + exception.Message);
                }
            }
            else NativeCombatActorPersistence.RestoreDebt(unit, saved);
            restoredActors.Add(unit.UniqueId, unit);
            SemanticRestoreCount++;
            logger.Info("Mounted native actor current debt restored after PostLoad: " + unit.UniqueId + ".");
        }

        internal void Update()
        {
            DrainAbandonedSave();
            CompleteAreaTransitionIfReady();
            TryRestoreCombat();
            if (CombatRestorationPending) return;
            if (!Enabled || !presentationPending || SaveSuspended || restoreLoad == null ||
                !restoreLoad.World.CanPresent(Game.Instance?.Player) ||
                Game.Instance?.CurrentlyLoadedArea == null || LoadingProcess.Instance.IsLoadingInProcess) return;
            var game = Game.Instance;
            if (game.CurrentMode != Kingmaker.GameModes.GameModeType.Default) return;
            presentationPending = false;
            restoreLoad.World.Close();
            var data = loaded.Data;
            if (game.Player.GameId != selectedCampaign || game.Player.GameId != data.CampaignId ||
                game.CurrentlyLoadedArea.AssetGuidThreadSafe != data.AreaId)
            {
                Report("Loaded world changed before mounted restoration; source metadata remains intact.");
                return;
            }
            if (!data.Mounted)
            {
                controls.RestorePersistentSlots(data.Slots);
                Report("Native unmounted save restored without inventing a pair.");
                return;
            }
            var riders = game.State.Units.Where(u => u.UniqueId == data.Rider.Id).ToArray();
            var mounts = game.State.Units.Where(u => u.UniqueId == data.Mount.Id).ToArray();
            if (riders.Length != 1 || mounts.Length != 1 ||
                !restoredActors.TryGetValue(data.Rider.Id, out var restoredRider) || riders[0] != restoredRider ||
                !restoredActors.TryGetValue(data.Mount.Id, out var restoredMount) || mounts[0] != restoredMount)
            {
                Report("Saved pair did not resolve to two uniquely restored native actors; rider=" + riders.Length +
                    "; mount=" + mounts.Length + "; early=" + restoredActors.Count + "; relationship remains unmounted.");
                return;
            }
            var result = relationship.RestoreSavedPair(riders[0], mounts[0], data.ProfileId);
            if (!result.Succeeded)
            {
                Report("Saved relationship was not eligible: " + string.Join("; ", result.Errors));
                return;
            }
            controls.RestorePersistentSlots(data.Slots);
            PresentationRestoreCount++;
            Report("Saved mounted relationship restored once without Mount execution or a new activation.");
        }

        private void Report(string feedback) { Feedback = feedback; logger.Info(feedback); }

        private sealed class LoadScope
        {
            internal long Sequence;
            internal readonly NativeLoadWorld<Player> World = new NativeLoadWorld<Player>();
        }

        private sealed class SaveScope
        {
            internal SaveInfo Prepared;
            internal Player World;
            internal Kingmaker.EntitySystem.SceneEntitiesState PartyState;
            internal string Json;
            internal bool ControlsSuspended;
            internal Action RestoreAi;
            // Captured once the native iterator starts its archive worker, so an
            // early disposal can tell whether that worker can still commit.
            internal System.Threading.Tasks.Task Worker;
            // Captured when the operation actually BEGINS its native routine, not
            // when it is wrapped: queued saves are wrapped long before they run,
            // and an earlier queued save committing to the same slot in between
            // would otherwise be attributed to this one. Lets the outcome tell a
            // commit of ITS own from any other, and say whether a previous
            // archive existed at all rather than promising one that never did.
            internal bool Began;
            internal int CommitsAtStart;
            internal string RequestedPath;
            internal bool PreviousExisted;
            internal IEnumerator<object> Routine;
            // Exactly-once finalization across every path that can end the
            // scope: ordinary completion, StopAll, disable, unload, update
            // failure and session stop.
            internal bool Released;
        }
    }
}
