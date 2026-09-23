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
            var scope = new SaveScope();
            var scoped = new ScopedEnumerator<object>(TrackNativeSave(routine, scope), () =>
            {
                if (activeSave != null) throw new InvalidOperationException("Overlapping native save enumerations.");
                activeSave = scope;
            }, () =>
            {
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
                }
            });
            var fault = diagnosticWait?.TryClaim(requestedSave) == true ? diagnosticWait : null;
            return Enabled ? DeferNativeSave(scoped, fault) : scoped;
        }

        private static IEnumerator<object> TrackNativeSave(IEnumerator<object> routine, SaveScope scope)
        {
            using (routine)
            {
                while (true)
                {
                    NativeSaveWorkerBoundary.RestoreCompletedPlayerReference(routine, scope.World, scope.PartyState);
                    if (!routine.MoveNext()) break;
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

        private void ObserveNativeLoadFailure(Exception exception)
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
        }
    }
}
