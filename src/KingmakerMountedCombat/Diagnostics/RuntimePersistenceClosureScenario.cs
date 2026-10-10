using System;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.GameModes;
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
    // Chunk 6 closeout: the four remaining Chunk 6A persistence behaviors, each an isolated P04 checkpoint on the
    // unchanged Chunk 5 persistence machinery and the real-time combat Mount foundation flow (the registered
    // selected-ability click, exactly as a player issues it):
    //
    //   combat-mount-routes          CM07-save-slot-routes          the settled combat-mounted pair written through the three
    //                                                               representative native routes (manual, quick, auto)
    //   combat-mount-unsettled-save  CM07-unsettled-save-deferred   a native save requested during the unsettled Mount approach,
    //                                                               deferred by the product until the transition settled
    //   combat-mount-area-reload     CM07-area-reload               the real native area reload after the combat-mounted save
    //   pending-mount-area           CM04-area-session-transition   the real native area reload during the pending Mount approach
    //
    // The scenario performs the boundary, records what the engine and the product did and whether the sequence
    // settled; the external validator decides acceptance. Nothing here writes a cooldown, a relationship, a
    // native turn or a position.
    internal sealed partial class RuntimePersistenceScenario
    {
        private bool RealtimeClosure => RealtimeCase && RuntimeRequest.IsClosurePersistenceCase(Checkpoint);
        private bool ClosureRoutes => Checkpoint == "combat-mount-routes";
        private bool ClosureUnsettled => Checkpoint == "combat-mount-unsettled-save";
        private bool ClosureAreaReload => Checkpoint == "combat-mount-area-reload";
        private bool ClosurePendingArea => Checkpoint == "pending-mount-area";
        private int closureRouteOrdinal, closureFrames, closureSettled;
        private bool closureLoadingObserved, closureFixtureReleased, closureClickCaptured, closureApproachPositioned;
        private string closureArea, closureArchiveHash, closureArchivePath, closureRiderId, closureMountId;
        private Vector3 closureApproachOrigin;
        // The foundation fixture starts the rider about 1.8 m from the mount, inside the 2.85 m Mount approach radius, so the
        // exact pending shell delivers without any approach; the two cases that observe the pending approach first move the
        // rider to a walkable point this far from the mount with its own native ground order (preview.207 stages 11 and 13).
        private const float ClosureApproachMetres = 6.5f;
        private Player closureWorld;
        private Vector3 closureClickPosition;
        private JObject closureBaseline;
        private SaveInfo closureRouteDescriptor;

        private static JArray ClosureOverrideComponents(UnitEntityData actor)
        {
            var view = actor?.View;
            return view == null ? new JArray() : new JArray(view.GetComponents<RiderMovementAgent>().Select(c => c.GetInstanceID()));
        }

        private JObject ClosureObservation()
        {
            var game = Game.Instance;
            var loading = LoadingProcess.Instance;
            var liveRider = closureRiderId == null ? rider : game.State.Units.SingleOrDefault(u => u.UniqueId == closureRiderId) ?? rider;
            var liveMount = closureMountId == null ? mount : game.State.Units.SingleOrDefault(u => u.UniqueId == closureMountId) ?? mount;
            var snapshot = controls.CaptureSnapshot();
            var observation = FoundationObservation();
            observation["case"] = Checkpoint;
            observation["frame"] = Time.frameCount;
            observation["gameTicks"] = game.TimeController.GameTime.Ticks;
            observation["area"] = game.CurrentlyLoadedArea?.AssetGuidThreadSafe;
            observation["sourceArea"] = closureArea;
            observation["sameWorld"] = closureWorld == null || ReferenceEquals(closureWorld, game.Player);
            observation["currentMode"] = game.CurrentMode.ToString();
            observation["loadingInProcess"] = loading.IsLoadingInProcess;
            observation["loadingObserved"] = closureLoadingObserved;
            observation["loadingFrames"] = closureFrames;
            observation["suspensions"] = persistence.AreaSuspensionCount;
            observation["resumes"] = persistence.AreaResumeCount;
            observation["areaRefused"] = persistence.RefusedAreaTransferCount;
            observation["areaPending"] = persistence.AreaTransitionPending;
            observation["deferredSaves"] = persistence.DeferredSaveCount;
            observation["snapshots"] = persistence.SnapshotCount;
            observation["failedSaves"] = persistence.FailedSaveCount;
            observation["nativeSaveWaiting"] = NativeDeferredSave.Waiting(loading);
            observation["saveSuspended"] = persistence.SaveSuspended;
            observation["serializationSuspended"] = controls.SerializationSuspended;
            observation["exactFactCount"] = snapshot.ExactFactCount;
            observation["duplicateFactCount"] = snapshot.DuplicateFactCount;
            observation["managedHotbarSlotCount"] = snapshot.ManagedHotbarSlotCount;
            observation["riderView"] = liveRider?.View == null ? JValue.CreateNull() : new JValue(liveRider.View.GetInstanceID());
            observation["mountView"] = liveMount?.View == null ? JValue.CreateNull() : new JValue(liveMount.View.GetInstanceID());
            observation["riderViewBound"] = liveRider?.View != null && ReferenceEquals(liveRider.View.EntityData, liveRider);
            observation["mountViewBound"] = liveMount?.View != null && ReferenceEquals(liveMount.View.EntityData, liveMount);
            observation["riderOverrideComponents"] = ClosureOverrideComponents(liveRider);
            observation["mountOverrideComponents"] = ClosureOverrideComponents(liveMount);
            observation["runtimeMovementAgent"] = relationship.Runtime.MovementAgent == null ? 0 : relationship.Runtime.MovementAgent.GetInstanceID();
            observation["riderAgentOverride"] = liveRider?.View?.AgentOverride == null ? 0 : liveRider.View.AgentOverride.GetInstanceID();
            observation["riderStockAgentEnabled"] = liveRider?.View?.AgentASP != null && liveRider.View.AgentASP.enabled;
            observation["mountStockAgentEnabled"] = liveMount?.View?.AgentASP != null && liveMount.View.AgentASP.enabled;
            observation["liveRiderCommandsEmpty"] = liveRider?.Commands.Empty;
            observation["liveMountCommandsEmpty"] = liveMount?.Commands.Empty;
            observation["liveRiderInCombat"] = liveRider?.IsInCombat;
            observation["liveMountInCombat"] = liveMount?.IsInCombat;
            observation["nativeActorCounts"] = new JObject
            {
                ["rider"] = closureRiderId == null ? 1 : game.State.Units.Count(u => u.UniqueId == closureRiderId),
                ["mount"] = closureMountId == null ? 1 : game.State.Units.Count(u => u.UniqueId == closureMountId)
            };
            observation["riderPosition"] = RealtimePoint(liveRider);
            observation["mountPosition"] = RealtimePoint(liveMount);
            observation["riderReallyMoving"] = liveRider?.View?.AgentASP?.IsReallyMoving ?? false;
            observation["fixtureReleased"] = closureFixtureReleased;
            observation["feedback"] = persistence.Feedback;
            var pendingShell = ClosurePendingShell();
            observation["pendingShell"] = pendingShell == null ? null : DescribeFoundationCommand(pendingShell);
            // The product's own save-deferral term for a registered, unfinished Mount shell (the approach included); the
            // ledger's admit-to-settle window is synchronous inside one call and is never observable from a frame.
            observation["unsettledShellOwned"] = pendingShell != null && controls.OwnsUnsettledRelationshipShell(pendingShell);
            observation["transitionRecords"] = ClosureTransitionRecords();
            return observation;
        }

        private UnitUseAbility ClosurePendingShell()
        {
            var shell = rider?.Commands?.GetCommand(UnitCommand.CommandType.Move) as UnitUseAbility;
            return shell != null && ReferenceEquals(shell.Spell?.Blueprint, controls.MountAbility) ? shell : null;
        }

        // Every transition ledger record, so a reader can tell one real forced detach of the exact pair from a later
        // announcement on the already-detached relationship (the native area unload of a combat pair records both).
        private JArray ClosureTransitionRecords() => new JArray(controls.RelationshipTransitionLedger.Records.Select(r => new JObject
        {
            ["kind"] = r.Kind.ToString(), ["identity"] = r.ControlIdentity, ["rider"] = r.RiderId, ["mount"] = r.MountId,
            ["generation"] = r.GenerationBefore, ["trigger"] = r.Trigger, ["settled"] = r.Settled, ["accepted"] = r.Accepted
        }));

        private static JArray ClosurePoint(Vector3 p) => new JArray(p.x, p.y, p.z);

        private Vector3 FindClosureApproachOrigin()
        {
            for (var i = 0; i < 16; i++)
            {
                var wanted = mount.Position + Quaternion.Euler(0, i * 22.5f, 0) * Vector3.forward * ClosureApproachMetres;
                var actual = Kingmaker.View.ObstacleAnalyzer.TraceAlongNavmesh(rider.Position, wanted);
                if (GeometryUtils.MechanicsDistance(actual, wanted) <= 0.25f &&
                    GeometryUtils.MechanicsDistance(actual, mount.Position) >= ClosureApproachMetres - 0.5f) return actual;
            }
            throw new InvalidOperationException("No native walkable point outside the Mount approach radius exists for the closure approach.");
        }

        // One native ground order (the ClickGroundHandler input a player issues) moves the rider outside the approach
        // radius; the Mount click follows only after that order has settled, so the shell has a real approach to observe.
        private void AdvanceClosureApproachPositioning()
        {
            var game = Game.Instance;
            if (!closureApproachPositioned)
            {
                if (!rider.Commands.Empty || !mount.Commands.Empty) return;
                closureApproachOrigin = rider.Position;
                var destination = FindClosureApproachOrigin();
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                game.DefaultPointerController.ClearPointerMode();
                bool clicked;
                using (var input = new NativeOrdinaryAttackInput(destination)) clicked = input.Click();
                Check(clicked, "RT-closure-approach-native-ground-order-admitted");
                closureApproachPositioned = true;
                Write("closure-approach-positioning", new JObject
                {
                    ["destination"] = ClosurePoint(destination),
                    ["destinationMountDistance"] = GeometryUtils.MechanicsDistance(destination, mount.Position),
                    ["actual"] = ClosureObservation()
                });
                return;
            }
            if (!rider.Commands.Empty || (rider.View?.AgentASP?.IsReallyMoving ?? false)) return;
            var distance = GeometryUtils.MechanicsDistance(rider.Position, mount.Position);
            Check(distance >= ClosureApproachMetres - 1.0f, "RT-closure-approach-rider-settled-outside-the-mount-approach-radius");
            Write("closure-approach-positioned", new JObject
            {
                ["mountDistance"] = distance,
                ["displacement"] = GeometryUtils.MechanicsDistance(closureApproachOrigin, rider.Position),
                ["actual"] = ClosureObservation()
            });
            stage = 40;
        }

        // The disposable world references are released only after the boundary has been requested, so the
        // native reload replaces a world no diagnostic lease still owns. Nothing production-owned is touched.
        private void ReleaseClosureFixture()
        {
            if (closureFixtureReleased) return;
            targetService?.Dispose(); targetService = null; combatTarget = null;
            restoreRealtimeAi?.Invoke(); restoreRealtimeAi = null;
            closureFixtureReleased = true;
        }

        private void BeginClosureUnsettledSave()
        {
            var game = Game.Instance;
            Check(game.SaveManager.IsSaveAllowed(), "RT-unsettled-native-save-admission-retains-policy");
            var shell = ClosurePendingShell();
            // The ledger's admit-to-settle window is synchronous inside one call; the observable unsettled state during the
            // approach is the product's own deferral term: the rider owns a registered, unfinished Mount shell.
            Check(shell != null && !shell.IsActed && !shell.IsFinished && controls.OwnsUnsettledRelationshipShell(shell),
                "RT-unsettled-save-requested-while-the-exact-mount-shell-is-pending");
            closureBaseline = ClosureObservation();
            Write("rt-unsettled-save-requested", ClosureObservation());
            var save = game.SaveManager.CreateNewSave("KMC_P01");
            callback = false;
            game.SaveGame(save, () => callback = true);
            Check(persistence.DeferredSaveCount == 1 && persistence.SnapshotCount == 0 &&
                NativeDeferredSave.Waiting(LoadingProcess.Instance) && !persistence.SaveSuspended && !controls.SerializationSuspended,
                "RT-unsettled-transition-save-truthfully-deferred-before-capture");
            Write("rt-unsettled-save-deferred", ClosureObservation());
            stage = 86;
        }

        private void AdvanceClosure()
        {
            var game = Game.Instance;
            var loading = LoadingProcess.Instance;
            if (clock.Elapsed.TotalSeconds > 240)
                throw new InvalidOperationException("P04 " + Checkpoint + " timed out at closure stage " + stage + ": " + persistence.Feedback);
            if (stage == 85)
            {
                if (loading.IsLoadingInProcess) { closureLoadingObserved = true; closureFrames++; return; }
                AdvanceClosureAfterReload();
                return;
            }
            if (loading.IsLoadingInProcess || (game.CurrentMode != GameModeType.Default && game.CurrentMode != GameModeType.Pause)) return;
            if (!closureFixtureReleased && combatTarget != null && !targetService.RefreshBidirectionalCombatMemoryLease())
                throw new InvalidOperationException("RT owned native combat memory was lost during the closure case.");
            if (game.IsPaused) { Write("fixture-native-unpause"); game.IsPaused = false; return; }
            if (stage == 82) { AdvanceClosureRoutes(); return; }
            if (stage == 83) { AdvanceClosurePendingArea(); return; }
            if (stage == 84) { BeginClosureAreaReload(); return; }
            if (stage == 86) { AdvanceClosureUnsettledSave(); return; }
            throw new InvalidOperationException("Unknown closure stage " + stage + ".");
        }

        // ---- combat-mount-routes ------------------------------------------------------------------------

        private void AdvanceClosureRoutes()
        {
            var game = Game.Instance;
            var types = new[] { SaveInfo.SaveType.Manual, SaveInfo.SaveType.Quick, SaveInfo.SaveType.Auto };
            if (closureRouteOrdinal >= types.Length)
            {
                Check(persistence.SnapshotCount == types.Length && persistence.FailedSaveCount == 0 && persistence.DeferredSaveCount == 0 &&
                    relationship.State == RelationshipState.Mounted && controls.CaptureSnapshot().DuplicateFactCount == 0 &&
                    controls.CaptureSnapshot().ExactFactCount == beforeControls.ExactFactCount,
                    "RT-three-native-routes-captured-the-same-settled-pair-without-residue");
                Write("routes-complete", ClosureObservation());
                Dispose();
                Result = new RuntimeSubscenarioResult { Name = request.Scenario, Status = "PASS",
                    AssertionPassCount = passed, AssertionFailCount = 0, Errors = new string[0] };
                Completed = true;
                return;
            }
            var type = types[closureRouteOrdinal];
            if (closureRouteDescriptor == null)
            {
                if (!rider.Commands.Empty || !mount.Commands.Empty) return;
                Check(game.SaveManager.IsSaveAllowed(), "RT-routes-native-save-admission-" + type);
                if (type != SaveInfo.SaveType.Manual) NativePersistenceIsolation.EnableNativeSlotRotation();
                var descriptor = type == SaveInfo.SaveType.Quick ? game.SaveManager.GetNextQuickslot() :
                    type == SaveInfo.SaveType.Auto ? game.SaveManager.GetNextAutoslot() :
                    game.SaveManager.SingleOrDefault(s => s.Name == "KMC_P01") ?? game.SaveManager.CreateNewSave("KMC_P01");
                Check(descriptor != null && descriptor.Type == type && descriptor.Name == SlotName(type),
                    "RT-routes-exact-native-slot-" + type);
                closureRouteDescriptor = descriptor;
                callback = false;
                Write("routes-write-requested", new JObject {
                    ["ordinal"] = closureRouteOrdinal + 1, ["nativeType"] = type.ToString(), ["name"] = descriptor.Name,
                    ["overwrite"] = descriptor.IsActuallySaved, ["actual"] = ClosureObservation() });
                game.SaveGame(descriptor, () => callback = true);
                return;
            }
            if (!callback || NativePersistenceIsolation.HasPendingWrites) return;
            // The native SaveManager lists the written archive under its own SaveInfo; the requested descriptor object is not
            // updated for a new save (every settled fixture re-queries the manager; preview.207 stage 10 waited on the request).
            var requested = closureRouteDescriptor;
            var save = game.SaveManager.FirstOrDefault(s => s.Type == type && s.Name == requested.Name && s.HasFileOnDisk &&
                s.OperationState == SaveInfo.StateType.None);
            if (save == null) return;
            var read = NativeMountedSaveStorage.Read(save.Saver);
            Check(read.Kind == MountedSaveReadKind.Current && read.Data.Mounted && read.Data.Combat != null && !read.Data.Combat.TurnBased &&
                read.Data.Rider.Id == rider.UniqueId && read.Data.Mount.Id == mount.UniqueId &&
                read.Data.CampaignId == request.Fixture.Working.GameId && read.Data.AreaId == request.Fixture.Working.Area &&
                persistence.SnapshotCount == closureRouteOrdinal + 1, "RT-routes-native-archive-carries-the-settled-pair-" + type);
            Write("routes-write-complete", new JObject {
                ["ordinal"] = closureRouteOrdinal + 1, ["nativeType"] = save.Type.ToString(), ["name"] = save.Name,
                ["path"] = save.FolderName, ["sha256"] = Hash(save.FolderName), ["length"] = new FileInfo(save.FolderName).Length,
                ["nativeCallback"] = callback, ["operation"] = save.OperationState.ToString(),
                ["snapshot"] = JObject.FromObject(read.Data, MountedSaveCodec.CreateSerializer()), ["actual"] = ClosureObservation() });
            closureRouteDescriptor = null;
            closureRouteOrdinal++;
        }

        // ---- combat-mount-unsettled-save -----------------------------------------------------------------

        private void AdvanceClosureUnsettledSave()
        {
            var game = Game.Instance;
            var settled = relationship.State == RelationshipState.Mounted && rider.Commands.Empty && mount.Commands.Empty &&
                !controls.HasUnsettledRelationshipTransition && !(rider.View?.AgentASP?.IsReallyMoving ?? false) &&
                !(mount.View?.AgentASP?.IsReallyMoving ?? false);
            if (!settled) { closureSettled = 0; return; }
            if (++closureSettled == 1) Write("rt-unsettled-save-transition-settled", ClosureObservation());
            if (!callback || NativePersistenceIsolation.HasPendingWrites) return;
            var save = game.SaveManager.SingleOrDefault(s => s.Name == "KMC_P01");
            if (save == null || !save.HasFileOnDisk || save.OperationState != SaveInfo.StateType.None) return;
            var read = NativeMountedSaveStorage.Read(save.Saver);
            Check(read.Kind == MountedSaveReadKind.Current && read.Data.Mounted && read.Data.Combat != null && !read.Data.Combat.TurnBased &&
                read.Data.Rider.Id == rider.UniqueId && read.Data.Mount.Id == mount.UniqueId && persistence.SnapshotCount == 1 &&
                persistence.DeferredSaveCount == 1 && persistence.FailedSaveCount == 0,
                "RT-deferred-save-captured-only-the-settled-mounted-pair");
            Check(!persistence.SaveSuspended && !controls.SerializationSuspended && controls.CaptureSnapshot().DuplicateFactCount == 0,
                "RT-deferred-save-restored-live-control-scopes");
            Write("native-write-complete", new JObject {
                ["path"] = save.FolderName, ["sha256"] = Hash(save.FolderName), ["length"] = new FileInfo(save.FolderName).Length,
                ["nativeType"] = save.Type.ToString(), ["nativeCallback"] = callback, ["operation"] = save.OperationState.ToString(),
                ["snapshot"] = JObject.FromObject(read.Data, MountedSaveCodec.CreateSerializer()), ["actual"] = ClosureObservation(),
                ["requested"] = closureBaseline });
            Write("unsettled-save-complete", ClosureObservation());
            Dispose();
            Result = new RuntimeSubscenarioResult { Name = request.Scenario, Status = "PASS",
                AssertionPassCount = passed, AssertionFailCount = 0, Errors = new string[0] };
            Completed = true;
        }

        // ---- area reloads ---------------------------------------------------------------------------------

        private void BeginClosureReload(string kind)
        {
            var game = Game.Instance;
            closureWorld = game.Player;
            closureArea = game.CurrentlyLoadedArea.AssetGuidThreadSafe;
            closureRiderId = rider.UniqueId; closureMountId = mount.UniqueId;
            closureBaseline = ClosureObservation();
            Write(kind, closureBaseline);
            ReleaseClosureFixture();
            game.ReloadArea(); // Exact native 06000CD6; real unload/replacement.
            closureFrames = 0; closureLoadingObserved = false; closureSettled = 0;
            stage = 85;
        }

        private void AdvanceClosurePendingArea()
        {
            if (!closureClickCaptured) { closureClickPosition = rider.Position; closureClickCaptured = true; }
            var shell = ClosurePendingShell();
            if (shell == null || shell.IsActed || shell.IsFinished || relationship.State != RelationshipState.Unmounted)
                throw new InvalidOperationException("The exact pending Mount shell left its approach before the area reload could be requested.");
            var moved = Vector3.Distance(new Vector3(closureClickPosition.x, 0f, closureClickPosition.z), new Vector3(rider.Position.x, 0f, rider.Position.z));
            if (!(rider.View?.AgentASP?.IsReallyMoving ?? false) || moved <= 0.25f) return;
            Check(controls.OwnsUnsettledRelationshipShell(shell), "RT-pending-mount-approach-observed-before-area-reload");
            var observed = ClosureObservation();
            observed["riderDisplacement"] = moved;
            observed["shell"] = DescribeFoundationCommand(shell);
            Write("pending-mount-approach-observed", observed);
            BeginClosureReload("pending-mount-area-requested");
        }

        private void BeginClosureAreaReload()
        {
            var save = Game.Instance.SaveManager.SingleOrDefault(s => s.Name == "KMC_P01");
            Check(save != null && save.HasFileOnDisk && relationship.State == RelationshipState.Mounted && persistence.SnapshotCount == 1,
                "RT-combat-mounted-archive-present-before-area-reload");
            closureArchivePath = save.FolderName;
            closureArchiveHash = Hash(save.FolderName);
            BeginClosureReload("combat-area-reload-requested");
        }

        private void AdvanceClosureAfterReload()
        {
            var game = Game.Instance;
            if (!closureLoadingObserved || persistence.AreaTransitionPending ||
                game.CurrentlyLoadedArea?.AssetGuidThreadSafe != closureArea || NativePersistenceIsolation.HasPendingWrites) return;
            if (game.CurrentMode != GameModeType.Default) { if (game.IsPaused) game.IsPaused = false; return; }
            if (game.IsPaused) { Write("fixture-native-unpause"); game.IsPaused = false; return; }
            if (++closureSettled < 3) return;
            var liveRider = game.State.Units.SingleOrDefault(u => u.UniqueId == closureRiderId);
            var liveMount = game.State.Units.SingleOrDefault(u => u.UniqueId == closureMountId);
            var observation = ClosureObservation();
            observation["baseline"] = closureBaseline;
            if (ClosureAreaReload)
            {
                observation["archivePath"] = closureArchivePath;
                observation["archiveSha256Before"] = closureArchiveHash;
                observation["archiveSha256After"] = File.Exists(closureArchivePath) ? Hash(closureArchivePath) : null;
            }
            Write(ClosureAreaReload ? "combat-area-reload-complete" : "pending-mount-area-complete", observation);
            Check(closureFrames > 0 && ReferenceEquals(closureWorld, game.Player) && game.CurrentlyLoadedArea.AssetGuidThreadSafe == closureArea,
                "P04-real-area-unload-and-same-native-world");
            Check(liveRider != null && liveMount != null && game.State.Units.Count(u => u.UniqueId == closureRiderId) == 1 &&
                game.State.Units.Count(u => u.UniqueId == closureMountId) == 1 && liveRider.View != null && liveMount.View != null &&
                ReferenceEquals(liveRider.View.EntityData, liveRider) && ReferenceEquals(liveMount.View.EntityData, liveMount),
                "P04-same-actors-with-bound-views-after-native-area-placement");
            // A combat pair is never carried across an area change (the transfer is not armed in combat): the
            // supported lifecycle is exactly one clean dismount at the native area unload, and no restoration.
            Check(relationship.State == RelationshipState.Unmounted && relationship.Rider == null && relationship.Mount == null &&
                persistence.AreaSuspensionCount == 0 && persistence.AreaResumeCount == 0 && !persistence.AreaTransitionPending &&
                !controls.HasUnsettledRelationshipTransition, "P04-combat-pair-cleaned-once-at-area-unload-not-carried");
            Check(liveRider.View.GetComponents<RiderMovementAgent>().Length == 0 && liveMount.View.GetComponents<RiderMovementAgent>().Length == 0 &&
                relationship.Runtime.MovementAgent == null && liveRider.View.AgentOverride == null && liveMount.View.AgentOverride == null &&
                liveRider.Commands.Empty && liveMount.Commands.Empty && controls.CaptureSnapshot().DuplicateFactCount == 0,
                "P04-no-movement-presentation-or-command-residue-after-reload");
            if (ClosureAreaReload)
                Check(File.Exists(closureArchivePath) && Hash(closureArchivePath) == closureArchiveHash && persistence.SnapshotCount == 1,
                    "P04-combat-mounted-archive-unchanged-by-the-area-reload");
            else
                Check(persistence.SnapshotCount == 0 && controls.NativeCastRequestCount == 1,
                    "P04-pending-mount-area-reload-wrote-nothing-and-replayed-no-request");
            rider = liveRider; mount = liveMount;
            Dispose();
            Result = new RuntimeSubscenarioResult { Name = request.Scenario, Status = "PASS",
                AssertionPassCount = passed, AssertionFailCount = 0, Errors = new string[0] };
            Completed = true;
        }
    }
}
