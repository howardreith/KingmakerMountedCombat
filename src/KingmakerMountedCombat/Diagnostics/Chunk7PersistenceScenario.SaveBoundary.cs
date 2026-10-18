using System;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Utility;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7PersistenceScenario
    {
        internal const string PendingMountSaveCase = "multi-nonselected-mount-unsettled-manual";
        internal const string WorkerCancelSaveCase = "multi-owned-worker-cancel-manual";
        internal static bool IsSaveBoundaryCase(string value) => value == PendingMountSaveCase || value == WorkerCancelSaveCase;
        private bool PendingMountSave => request.PersistenceCase == PendingMountSaveCase;
        private bool WorkerCancelSave => request.PersistenceCase == WorkerCancelSaveCase;
        private bool SourceSaveBoundary => !Cold && IsSaveBoundaryCase(request.PersistenceCase);
        private int BoundaryWriteCount => WorkerCancelSave ? 2 : 1;
        private Chunk7SaveBoundaryProbe saveBoundaryProbe;
        private RetainedObjectIdentity saveBoundaryIds;
        private IDisposable saveWorkerHold;
        private SaveInfo boundaryRequestedSave;
        private MountedPairOwner<UnitEntityData, GameMountedRelationshipService> healthySaveOwner;
        private RelationshipCommandIdentity pendingSaveIdentity;
        private GameMountedRelationshipService pendingSaveBoundary;
        private UnitUseAbility pendingSaveShell;
        private UnitCommand boundaryApproach;
        private Vector3 boundaryApproachOrigin, boundaryApproachGoal, boundaryMountClickOrigin;
        private readonly JObject saveBoundaryEvidence = new JObject();
        private readonly JArray saveBoundaryRows = new JArray();
        private JObject heldStable, pendingDeferred, pendingContinued;
        private int holdEntriesBefore, holdCountBefore, heldWorkerId = -1, heldFrames, pauseDepthBeforeCancel;
        private bool pausedBeforeCancel, boundaryDrainedCommitted, saveBoundaryEnded;
        private string heldWorkerLeaf;

        private void BeginSaveBoundaryObservation()
        {
            RequireNativeWorld();
            Require(SourceSaveBoundary && saveBoundaryProbe == null && saveBoundaryIds == null &&
                replayAuthorization != null && replaySaveAuthority != null &&
                replayAuthorization.IsExactPersistenceScope(replaySaveAuthority) && replaySaveAuthority.MaximumNativeWriteRequests == BoundaryWriteCount &&
                replaySaveAuthority.AdmittedNativeWriteRequests == 0 && persistence.SnapshotCount == snapshotBefore &&
                persistence.CompletedSaveCount == 0 && persistence.DrainedSaveCount == 0 && persistence.DeferredSaveCount == 0,
                "Selected save boundary lacks its exact fresh selected isolated authority.");
            saveBoundaryIds = new RetainedObjectIdentity(128);
            healthySaveOwner = world.Registry.FindActive(firstRider);
            RequireHealthySaveOwner();
            evidence["saveBoundary"] = saveBoundaryEvidence;
            saveBoundaryEvidence["case"] = request.PersistenceCase;
            saveBoundaryEvidence["rows"] = saveBoundaryRows;
            // This flag describes the selected final operation; the worker seed has its own mandatory callback.
            saveBoundaryEvidence["ordinaryNativeCallbackRequired"] = !WorkerCancelSave;
            saveBoundaryEvidence["seedNativeCallbackRequired"] = WorkerCancelSave;
            saveBoundaryEvidence["actualWorkerFaultProof"] = false;
            saveBoundaryEvidence["manualResourceWrites"] = 0;
            saveBoundaryEvidence["diagnosticsOffProof"] = false;
            saveBoundaryProbe = new Chunk7SaveBoundaryProbe(persistence, actors, CaptureSaveBoundary, BoundaryWriteCount);
            saveBoundaryProbe.Install();
        }
        private void RequireSaveBoundaryAuthority()
        {
            RequireNativeWorld();
            Require(replayAuthorization != null && replaySaveAuthority != null &&
                replayAuthorization.IsExactPersistenceScope(replaySaveAuthority) &&
                replayAuthorization.AuthorityGeneration == replayAuthorityGeneration &&
                NativePersistenceIsolation.IsExactAuthority(replaySaveAuthority) &&
                Game.Instance.SaveManager.SavePath == replaySaveAuthority.Root &&
                replayAuthorization.AuthorizedLoadCount == 1 && replayAuthorization.AuthorizedWriteCount <= BoundaryWriteCount &&
                replaySaveAuthority.AdmittedNativeWriteRequests <= BoundaryWriteCount && replaySaveAuthority.MaximumNativeWriteRequests == BoundaryWriteCount &&
                replayAuthorization.UnauthorizedLoadCount == 0 && replayAuthorization.UnauthorizedWriteCount == 0 &&
                replayAuthorization.FatalViolationCount == 0 && replayAuthorization.BaselineLoadRequestCount == 0 &&
                !replayAuthorization.IsOneShotWorkingWriteSuppressionArmed && replayAuthorization.SuppressedWorkingWriteCount == 0,
                "Selected save boundary lost exact native world/authority or admitted an extra operation.");
        }
        private void RequireHealthySaveOwner()
        {
            Require(healthySaveOwner != null && world.Registry.IsActiveOwner(healthySaveOwner, firstRider, firstMount) &&
                ReferenceEquals(healthySaveOwner.Boundary.PairOwner, healthySaveOwner) &&
                healthySaveOwner.Boundary.State == RelationshipState.Mounted &&
                healthySaveOwner.Generation == healthySaveOwner.Boundary.MountedPairGeneration,
                "The independent selected pair lost its exact owner or generation.");
        }
        private void BeginPendingMountSave()
        {
            // At this point A mounted through real input, B is still unmounted.
            // Only A's two real mounted hotbar slots exist at the save request;
            // B's later attachment must not invent hotbar bindings.
            if (OwnerSeedCheckpointSave) RequireOwnerSeedPreparedSlots(); else BindSlots();
            BeginSaveBoundaryObservation();
            Require(actors.All(a => a.Commands.Empty) && !Mounted(secondRider, secondMount), "Native approach cannot start over a command or mounted B.");
            boundaryApproachOrigin = secondRider.Position;
            var found = false;
            for (var i = 0; i < 16; i++)
            {
                var wanted = secondMount.Position + Quaternion.Euler(0, i * 22.5f, 0) * Vector3.forward * 6.5f;
                var actual = Kingmaker.View.ObstacleAnalyzer.TraceAlongNavmesh(secondRider.Position, wanted);
                if (GeometryUtils.MechanicsDistance(actual, wanted) > 0.25f || GeometryUtils.MechanicsDistance(actual, secondMount.Position) < 6f) continue;
                boundaryApproachGoal = actual; found = true; break;
            }
            Require(found, "The exact fixture has no walkable native approach point outside Mount reach; no actor is moved directly.");
            IssuePendingApproach();
        }
        private void IssuePendingApproach()
        {
            RequireNativeWorld();
            Require(!LoadingProcess.Instance.IsLoadingInProcess && !LoadingProcess.Instance.QueuedNames.Any(),
                "An unrelated native loading operation blocks approach input.");
            Chunk7NativeFixture.Select(secondRider); Game.Instance.IsPaused = true;
            var previous = actors.ToDictionary(a => a, a => a.Commands.Move);
            try { ClickGroundHandler.MoveSelectedUnitsToPoint(boundaryApproachGoal, false); }
            finally
            {
                foreach (var actor in actors)
                {
                    var observed = actor.Commands.Move;
                    if (observed == null || ReferenceEquals(observed, previous[actor])) continue;
                    if (ReferenceEquals(actor, secondRider)) boundaryApproach = observed;
                    if (!issued.Contains(observed)) issued.Add(observed);
                }
            }
            Require(boundaryApproach != null && !ReferenceEquals(boundaryApproach, previous[secondRider]) &&
                actors.Where(a => !ReferenceEquals(a, secondRider)).All(a => ReferenceEquals(a.Commands.Move, previous[a])),
                "Native approach positioning changed another actor's command.");
            Chunk7NativeFixture.Select(firstRider); Game.Instance.IsPaused = false;
            saveBoundaryEvidence["positioning"] = new JObject { ["actorId"] = secondRider.UniqueId,
                ["commandObject"] = saveBoundaryIds.Get(boundaryApproach), ["commandType"] = boundaryApproach.GetType().FullName,
                ["start"] = Point(boundaryApproachOrigin), ["goal"] = Point(boundaryApproachGoal), ["otherCommandsUnchanged"] = true };
            Next(80);
        }
        private void BeginWorkerCancelSave()
        {
            BeginSaveBoundaryObservation();
            holdEntriesBefore = NativeSaveWorkerBoundary.WorkerEntryCount; holdCountBefore = NativeSaveWorkerBoundary.WorkerHoldCount;
            Require(!NativeSaveWorkerBoundary.WorkerHeld && !NativePersistenceIsolation.HasPendingWrites &&
                !persistence.HasActiveSaveScope && !persistence.HasSaveAdmissionFence, "Another worker already owns the selected boundary.");
            saveBoundaryRows.Add(CaptureSaveBoundary("before-worker-save"));
            // A first-ever archive has no native replacement-commit callback.
            // Complete its real seed before holding the second operation.
            RequestSave(); Next(86);
        }
        private bool UpdateSaveBoundary()
        {
            if (!SourceSaveBoundary || stage < 80 || stage > 86) return false;
            RequireSaveBoundaryAuthority(); RequireHealthySaveOwner();
            Require(!Game.Instance.Player.IsInCombat, "Exploration save boundary unexpectedly entered combat.");
            if (stage == 80 || stage == 81)
                Require(!LoadingProcess.Instance.IsLoadingInProcess && !LoadingProcess.Instance.QueuedNames.Any(),
                    "An unrelated native loading operation blocks the next approach or save request.");
            if (stage == 80)
            {
                if (!boundaryApproach.IsFinished || secondRider.View.AgentASP.IsReallyMoving || !secondRider.Commands.Empty) return true;
                var position = (JObject)saveBoundaryEvidence["positioning"];
                position["end"] = Point(secondRider.Position); position["terminal"] = true;
                position["distance"] = Vector3.Distance(boundaryApproachOrigin, secondRider.Position);
                position["mountPosition"] = Point(secondMount.Position);
                position["mountDistance"] = Vector3.Distance(secondRider.Position, secondMount.Position);
                Require((float)position["mountDistance"] >= 5.5f && (float)position["distance"] >= 0.75f,
                    "Native rider positioning did not reach a measurable outside-reach approach.");
                boundaryMountClickOrigin = secondRider.Position;
                Input(secondRider, secondMount, controls.MountAbility, firstRider, "mount-horse");
                pendingSaveShell = shell; pendingSaveIdentity = controls.CaptureRelationshipCommandIdentity(shell);
                pendingSaveBoundary = world.Pairs.Select(p => p.Relationship).Single(p => controls.HasUnsettledShellFor(p));
                Require(pendingSaveIdentity?.UnactedRequestComplete == true && !ReferenceEquals(pendingSaveBoundary, healthySaveOwner.Boundary),
                    "The pending second pair does not have its own exact admitted command boundary.");
                Next(81); return true;
            }
            if (stage == 81)
            {
                Require(pendingSaveShell != null && !pendingSaveShell.IsActed && !pendingSaveShell.IsFinished &&
                    pendingSaveIdentity.MatchesUnacted(controls.CaptureRelationshipCommandIdentity(pendingSaveShell)) &&
                    controls.OwnsUnsettledRelationshipShell(pendingSaveShell) && controls.HasUnsettledShellFor(pendingSaveBoundary) &&
                    SelectionManager.Instance.SelectedUnits.Count == 1 && ReferenceEquals(SelectionManager.Instance.SelectedUnits[0], firstRider),
                    "B's exact unselected Mount approach settled or changed before native save admission.");
                var displacement = Vector3.Distance(boundaryMountClickOrigin, secondRider.Position);
                if (!secondRider.View.AgentASP.IsReallyMoving || displacement <= 0.25f) return true;
                saveBoundaryEvidence["pendingApproachDisplacement"] = displacement;
                saveBoundaryRows.Add(CaptureSaveBoundary("pending-before-save"));
                RequestSave();
                Require(persistence.DeferredSaveCount == 1 && persistence.SnapshotCount == snapshotBefore &&
                    NativeDeferredSave.Waiting(LoadingProcess.Instance) && persistence.HasSaveAdmissionFence &&
                    !persistence.HasActiveSaveScope && !controls.SerializationSuspended && !persistence.SaveSuspended,
                    "Native save did not defer before capture on the nonselected second-pair shell.");
                RequirePendingSaveWait();
                pendingDeferred = CaptureSaveBoundary("pending-save-deferred"); saveBoundaryRows.Add(pendingDeferred); Next(82); return true;
            }
            if (stage == 82)
            {
                if (NativeDeferredSave.Waiting(LoadingProcess.Instance))
                {
                    RequirePendingSaveWait();
                    if (pendingContinued == null && !pendingSaveShell.IsActed && !pendingSaveShell.IsFinished &&
                        secondRider.View.AgentASP.IsReallyMoving && Game.Instance.TimeController.GameTime.Ticks > (long)pendingDeferred["gameTicks"])
                    {
                        var prior = pendingDeferred["actors"].Single(a => (string)a["id"] == secondRider.UniqueId)["position"];
                        var priorPoint = new Vector3((float)prior["x"], (float)prior["y"], (float)prior["z"]);
                        if (Vector3.Distance(priorPoint, secondRider.Position) > 0.1f)
                        {
                            Require(pendingSaveIdentity.MatchesUnacted(controls.CaptureRelationshipCommandIdentity(pendingSaveShell)),
                                "B changed command/process authority while its native save waited.");
                            pendingContinued = CaptureSaveBoundary("pending-save-continuing"); saveBoundaryRows.Add(pendingContinued);
                        }
                    }
                    return true;
                }
                if (LoadingProcess.Instance.IsLoadingInProcess || LoadingProcess.Instance.QueuedNames.Any() ||
                    NativePersistenceIsolation.HasPendingWrites || persistence.HasActiveSaveScope || persistence.HasSaveAdmissionFence || !callback) return true;
                Require(Mounted(secondRider, secondMount) && InputSettled(), "Native deferred save completed without B's exact Mount settlement.");
                FinishInput();
                if (OwnerSeedCheckpointSave) RememberOwnerSeedRemountedSecondPair(); else RememberOwners();
                AssertOwners(); AssertSlots();
                Require(pendingContinued != null && saveBoundaryProbe.Complete && persistence.DeferredSaveCount == 1 && persistence.FailedSaveCount == 0,
                    "Deferred native header/preparation observation failed.");
                if (!OwnerSeedCheckpointSave) Pass("C7-persistence-two-native-mounts");
                CompleteSaveBoundary("deferred-save-settled");
                Pass("C7-persistence-nonselected-mount-save-deferral"); Next(10); return true;
            }
            if (stage == 86) { PollWorkerSeed(); return true; }
            if (stage == 83)
            {
                if (!NativeSaveWorkerBoundary.WorkerHeld) return true;
                // Read the raw latch before the authoritative getter resolves it.
                saveBoundaryEvidence["workerCachedAtHold"] = persistence.ActiveSaveWorkerCached;
                heldWorkerId = persistence.ActiveSaveWorkerId; heldWorkerLeaf = persistence.ActiveSaveLeaf;
                Require(heldWorkerId >= 0 && persistence.ActiveSaveWorkerRunning &&
                    NativeSaveWorkerBoundary.WorkerEntryCount == holdEntriesBefore + 2 &&
                    NativeSaveWorkerBoundary.WorkerHoldCount == holdCountBefore + 1 && NativeSaveWorkerBoundary.HeldWorkerLeaf == heldWorkerLeaf &&
                    heldWorkerLeaf == boundaryReplacementLeaf && !callback && boundarySeedCallbacks == 1 && boundaryReplacementCallbacks == 0,
                    "The hold did not identify this exact native operation's own running worker.");
                ObserveWorkerReplacementPrepared();
                pausedBeforeCancel = Game.Instance.IsPaused; pauseDepthBeforeCancel = MountedPersistenceService.NativePauseDepth();
                saveBoundaryRows.Add(CaptureSaveBoundary("worker-held-before-stop"));
                LoadingProcess.Instance.StopAll();
                RequireWorkerCustody(); saveBoundaryRows.Add(CaptureSaveBoundary("worker-stop-once"));
                LoadingProcess.Instance.StopAll();
                RequireWorkerCustody(); saveBoundaryRows.Add(CaptureSaveBoundary("worker-stop-twice"));
                heldFrames = 0; Next(84); return true;
            }
            if (stage == 84)
            {
                RequireWorkerCustody();
                if (!Game.Instance.IsPaused) return true;
                if (heldStable == null) { heldStable = CaptureSaveBoundary("worker-held-stable"); saveBoundaryRows.Add(heldStable); heldFrames = 0; return true; }
                if (++heldFrames < 20) return true;
                var after = CaptureSaveBoundary("worker-held-after-ticks"); saveBoundaryRows.Add(after);
                Require((long)after["gameTicks"] == (long)heldStable["gameTicks"] &&
                    JToken.DeepEquals(after["actors"], heldStable["actors"]), "The native serialized world changed while its worker remained held.");
                ReleaseSaveBoundaryHold(); Next(85); return true;
            }
            if (stage == 85)
            {
                if (NativePersistenceIsolation.HasPendingWrites || persistence.HasActiveSaveScope || persistence.HasSaveAdmissionFence ||
                    persistence.SaveDraining || persistence.WorldHoldReleasePending || world.SerializationBusy ||
                    LoadingProcess.Instance.IsLoadingInProcess || LoadingProcess.Instance.QueuedNames.Any()) return true;
                Require(persistence.DrainedSaveCount == 1 && persistence.DeferredSaveCancellationCount == 1 &&
                    persistence.LastDrainedSaveCommitted && persistence.LastDrainedSaveOutcomeEstablished &&
                    persistence.LastDrainedSavePath == Path.Combine(replaySaveAuthority.Root, SelectedOutputLeaf(SaveInfo.SaveType.Manual)) &&
                    persistence.CompletedSaveCount == 1 && ReferenceEquals(persistence.LastCompletedSave, boundarySeedCompletion) && persistence.FailedSaveCount == 0 &&
                    !callback && !NativeSaveWorkerBoundary.WorkerHeld &&
                    MountedPersistenceService.NativePauseDepth() == pauseDepthBeforeCancel && Game.Instance.IsPaused == pausedBeforeCancel,
                    "Interrupted native iterator did not establish its own committed worker outcome and exact once-only release.");
                ObserveWorkerReplacementSettled();
                boundaryDrainedCommitted = true;
                CompleteSaveBoundary("worker-drain-settled");
                Pass("C7-persistence-owned-worker-cancel-drain"); Next(10); return true;
            }
            throw new InvalidOperationException("Unknown selected save boundary stage.");
        }
        private void RequirePendingSaveWait()
        {
            Require(NativeDeferredSave.Waiting(LoadingProcess.Instance) && !LoadingProcess.Instance.IsLoadingInProcess &&
                !LoadingProcess.Instance.IsManualLoadingScreenActive &&
                (Game.Instance.CurrentMode == Kingmaker.GameModes.GameModeType.Default || Game.Instance.CurrentMode == Kingmaker.GameModes.GameModeType.Pause) &&
                !Game.Instance.IsPaused && persistence.SnapshotCount == snapshotBefore && persistence.DeferredSaveCount == 1 &&
                persistence.HasSaveAdmissionFence && !persistence.HasActiveSaveScope && !persistence.SaveSuspended &&
                !controls.SerializationSuspended && !world.SerializationBusy &&
                world.Pairs.All(p => !p.Relationship.SaveSerializationSuspended && p.Combat.ChargeAdmissionFenced) &&
                replayAuthorization.AuthorizedWriteCount == 1 && replaySaveAuthority.AdmittedNativeWriteRequests == 1,
                "The admitted native save stopped the exact pending world before its Mount shell could settle.");
        }
        private void RequireWorkerCustody()
        {
            Require(NativeSaveWorkerBoundary.WorkerHeld && persistence.ActiveSaveWorkerRunning && persistence.ActiveSaveWorkerId == heldWorkerId &&
                persistence.ActiveSaveLeaf == heldWorkerLeaf && NativeSaveWorkerBoundary.HeldWorkerLeaf == heldWorkerLeaf &&
                persistence.SaveDraining && persistence.HasActiveSaveScope && persistence.HasSaveAdmissionFence &&
                persistence.SaveSuspended && controls.SerializationSuspended && world.SerializationBusy &&
                world.Pairs.All(p => p.Relationship.SaveSerializationSuspended && p.Combat.ChargeAdmissionFenced) &&
                persistence.DeferredSaveCancellationCount == 1 && persistence.DrainedSaveCount == 0 && !callback,
                "A repeated native cancellation released an independently owned pair or lost the same live worker.");
        }
        private void CompleteSaveBoundary(string kind)
        {
            RequireSaveBoundaryAuthority(); AssertOwners(); AssertSlots();
            Require(saveBoundaryProbe.Complete && replayAuthorization.AuthorizedWriteCount == BoundaryWriteCount &&
                replaySaveAuthority.AdmittedNativeWriteRequests == BoundaryWriteCount && persistence.SnapshotCount == snapshotBefore + BoundaryWriteCount,
                "Selected save boundary did not observe its declared native headers and no preparation.");
            saveBoundaryRows.Add(CaptureSaveBoundary(kind));
            ObserveSaved(); EndSaveBoundaryObservation();
        }
        private JObject CaptureSaveBoundary(string kind)
        {
            RequireSaveBoundaryAuthority(); RequireHealthySaveOwner();
            var identity = controls.CaptureRelationshipCommandIdentity(pendingSaveShell);
            return new JObject { ["kind"] = kind, ["frame"] = Time.frameCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
                ["gameObject"] = saveBoundaryIds.Get(Game.Instance), ["playerObject"] = saveBoundaryIds.Get(Game.Instance.Player),
                ["stateObject"] = saveBoundaryIds.Get(Game.Instance.State), ["saveManagerObject"] = saveBoundaryIds.Get(Game.Instance.SaveManager),
                ["requestedSaveObject"] = saveBoundaryIds.Get(boundaryRequestedSave), ["authorityGeneration"] = replayAuthorityGeneration,
                ["selected"] = new JArray(SelectionManager.Instance.SelectedUnits.Select(a => a.UniqueId)),
                ["owners"] = new JArray(world.Registry.Owners.Select(o => new JObject { ["id"] = o.Id.ToString("N"), ["generation"] = o.Generation,
                    ["riderId"] = o.RiderId, ["mountId"] = o.MountId, ["boundaryObject"] = saveBoundaryIds.Get(o.Boundary), ["state"] = o.State.ToString() })),
                ["boundaries"] = new JArray(world.Pairs.Select(p => new JObject { ["object"] = saveBoundaryIds.Get(p.Relationship),
                    ["ownerId"] = p.Relationship.PairOwner?.Id.ToString("N"), ["state"] = p.Relationship.State.ToString(),
                    ["generation"] = p.Relationship.MountedPairGeneration, ["shellUnsettled"] = controls.HasUnsettledShellFor(p.Relationship),
                    ["saveSuspended"] = p.Relationship.SaveSerializationSuspended, ["chargeFence"] = p.Combat.ChargeAdmissionFenced })),
                ["actors"] = new JArray(actors.Select(a => new JObject { ["id"] = a.UniqueId, ["object"] = saveBoundaryIds.Get(a),
                    ["native"] = JObject.FromObject(MountedPersistenceService.CaptureActor(a), MountedSaveCodec.CreateSerializer()),
                    ["prepared"] = a.CombatState.Prepared, ["position"] = Point(a.Position), ["commandsEmpty"] = a.Commands.Empty,
                    ["moving"] = a.View.AgentASP.IsReallyMoving })),
                ["shell"] = pendingSaveShell == null ? null : new JObject { ["object"] = saveBoundaryIds.Get(pendingSaveShell),
                    ["boundaryObject"] = saveBoundaryIds.Get(pendingSaveBoundary), ["control"] = identity?.Control,
                    ["casterId"] = identity?.Caster, ["targetId"] = identity?.Target, ["generation"] = identity?.Generation,
                    ["processObject"] = saveBoundaryIds.Get(identity?.Process), ["contextObject"] = saveBoundaryIds.Get(identity?.Context),
                    ["acted"] = pendingSaveShell.IsActed, ["finished"] = pendingSaveShell.IsFinished,
                    ["ownedUnsettled"] = controls.OwnsUnsettledRelationshipShell(pendingSaveShell),
                    ["processEnded"] = pendingSaveShell.ExecutionProcess == null || pendingSaveShell.ExecutionProcess.IsEnded },
                ["slots"] = JArray.FromObject(controls.CapturePersistentSlots(), MountedSaveCodec.CreateSerializer()),
                ["snapshots"] = persistence.SnapshotCount, ["deferred"] = persistence.DeferredSaveCount,
                ["completed"] = persistence.CompletedSaveCount, ["failed"] = persistence.FailedSaveCount,
                ["cancelDeferrals"] = persistence.DeferredSaveCancellationCount, ["drains"] = persistence.DrainedSaveCount,
                ["draining"] = persistence.SaveDraining, ["activeScope"] = persistence.HasActiveSaveScope,
                ["admissionFence"] = persistence.HasSaveAdmissionFence, ["worldBusy"] = world.SerializationBusy,
                ["saveSuspended"] = persistence.SaveSuspended, ["controlsSuspended"] = controls.SerializationSuspended,
                ["nativeWaiting"] = NativeDeferredSave.Waiting(LoadingProcess.Instance), ["callback"] = callback,
                ["nativeLoading"] = LoadingProcess.Instance.IsLoadingInProcess, ["manualLoadingScreen"] = LoadingProcess.Instance.IsManualLoadingScreenActive,
                ["mode"] = Game.Instance.CurrentMode.ToString(),
                ["authorizedWrites"] = replayAuthorization.AuthorizedWriteCount, ["admittedWrites"] = replaySaveAuthority.AdmittedNativeWriteRequests,
                ["workerId"] = persistence.ActiveSaveWorkerId, ["workerRunning"] = persistence.ActiveSaveWorkerRunning,
                ["workerHeld"] = NativeSaveWorkerBoundary.WorkerHeld, ["workerLeaf"] = persistence.ActiveSaveLeaf,
                ["workerEntries"] = NativeSaveWorkerBoundary.WorkerEntryCount - holdEntriesBefore,
                ["workerHolds"] = NativeSaveWorkerBoundary.WorkerHoldCount - holdCountBefore,
                ["drainCommitted"] = persistence.LastDrainedSaveCommitted, ["drainOutcomeEstablished"] = persistence.LastDrainedSaveOutcomeEstablished,
                ["drainPath"] = persistence.LastDrainedSavePath, ["paused"] = Game.Instance.IsPaused,
                ["pauseDepth"] = MountedPersistenceService.NativePauseDepth(), ["worldReleasePending"] = persistence.WorldHoldReleasePending };
        }
        private void ReleaseSaveBoundaryHold() { saveWorkerHold?.Dispose(); saveWorkerHold = null; }
        private void EndSaveBoundaryObservation()
        {
            if (saveBoundaryEnded || saveBoundaryProbe == null && saveBoundaryIds == null) return;
            Require(!NativePersistenceIsolation.HasPendingWrites && !persistence.HasActiveSaveScope && !persistence.HasSaveAdmissionFence &&
                !world.SerializationBusy && !persistence.SaveDraining, "Save observation still owns a native iterator/worker window.");
            ReleaseSaveBoundaryHold();
            if (saveBoundaryProbe != null)
            {
                saveBoundaryEvidence["probe"] = saveBoundaryProbe.Capture();
                saveBoundaryProbe.Dispose(); saveBoundaryProbe = null;
            }
            if (saveBoundaryIds != null)
            {
                var ids = saveBoundaryIds.Ids; var faults = saveBoundaryIds.FaultCount;
                saveBoundaryIds.Dispose();
                saveBoundaryEvidence["identities"] = new JObject { ["capacity"] = 128, ["faults"] = faults,
                    ["ids"] = new JArray(ids), ["released"] = saveBoundaryIds.Released, ["retainedAfterRelease"] = saveBoundaryIds.RetainedCount };
            }
            saveBoundaryEnded = true;
        }
    }
}
