using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.UI.Selection;
using Kingmaker.UI.UnitSettings;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Utility;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using KingmakerMountedCombat.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Two native processes, one archive. The load variant may inspect metadata,
    // but never creates/acquires a companion or invokes any Mount operation.
    internal sealed partial class RuntimePersistenceScenario : IDisposable
    {
        private readonly RuntimeRequest request;
        private readonly GameMountedRelationshipService relationship;
        private readonly NativeMountedControlService controls;
        private readonly MountedPersistenceService persistence;
        private readonly DiagnosticSettings settings;
        private readonly IModLogger logger;
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly string evidence;
        private UnitEntityData rider;
        private UnitEntityData mount;
        private UnitMoveTo move;
        private Vector3 origin;
        private Vector3 destination;
        private NativeMountedControlSnapshot beforeControls;
        private DiagnosticCombatTargetService targetService;
        private MountedCombatRuleProbe ruleProbe;
        private NativeModeTransitionProbe realtime;
        private int stage;
        private int passed;
        private bool callback;
        private bool disposed;
        internal bool Completed { get; private set; }
        internal RuntimeSubscenarioResult Result { get; private set; }
        private bool Cold => request.Scenario == "persistence-p01-load" || request.Scenario == "persistence-p02-load";
        private bool CombatCase => request.Scenario == "persistence-p02-save" || request.Scenario == "persistence-p02-load";
        private readonly MountedCombatController combat;

        internal RuntimePersistenceScenario(RuntimeRequest request, GameMountedRelationshipService relationship,
            NativeMountedControlService controls, MountedPersistenceService persistence, MountedCombatController combat,
            DiagnosticSettings settings, IModLogger logger)
        {
            this.request = request; this.relationship = relationship; this.controls = controls;
            this.persistence = persistence; this.combat = combat; this.settings = settings; this.logger = logger;
            evidence = Path.Combine(request.EvidenceRoot, "persistence-observations.jsonl");
        }

        internal void Update()
        {
            if (Completed) return;
            try { if (CombatCase) AdvanceCombat(); else Advance(); }
            catch (Exception exception)
            {
                var errors = new List<string> { exception.GetType().Name + ": " + exception.Message };
                try { Write("scenario-failed", new JObject { ["error"] = errors[0] }); }
                catch (Exception error) { errors.Add("Failure observation: " + error.Message); }
                try { Dispose(); }
                catch (Exception error) { errors.Add("P01 cleanup: " + error.Message); }
                Result = new RuntimeSubscenarioResult { Name = request.Scenario, Status = "FAIL",
                    AssertionPassCount = passed, AssertionFailCount = 1, Errors = errors.ToArray() };
                Completed = true;
            }
        }

        private void Advance()
        {
            if (clock.Elapsed.TotalSeconds > 150) throw new InvalidOperationException("P01 native stage timed out: " + stage);
            var game = Game.Instance;
            if (LoadingProcess.Instance.IsLoadingInProcess) return;
            if (stage >= 4 && stage <= 5)
            {
                if (!targetService.RefreshBidirectionalCombatMemoryLease())
                    throw new InvalidOperationException("P01 native combat memory fixture lease was lost.");
                // The owned RT attack scenario explicitly resumes native auto-pause
                // before waiting for initiative, which cannot recover while paused.
                if (game.IsPaused)
                {
                    Write("fixture-native-unpause");
                    game.IsPaused = false;
                    return;
                }
            }
            if (stage == 0)
            {
                Check(settings.EnablePairedActivation && !settings.EnableUnifiedMountedTurn &&
                    !settings.EnablePairedCommandScheduler && !settings.EnableDiagnosticOverlay, "required-policy");
                if (Cold)
                {
                    Check(persistence.LoadedData?.Mounted == true && relationship.State == RelationshipState.Mounted,
                        "cold-pair-restored-from-archive: " + persistence.Feedback);
                    rider = relationship.Rider; mount = relationship.Mount;
                    Check(persistence.SemanticRestoreCount == 2 && persistence.PresentationRestoreCount == 1,
                        "early-semantic-and-once-presentation");
                    Check(rider.UniqueId == persistence.LoadedData.Rider.Id && mount.UniqueId == persistence.LoadedData.Mount.Id,
                        "same-saved-native-actors");
                    Check(game.State.Units.Count(u => u.UniqueId == rider.UniqueId) == 1 &&
                        game.State.Units.Count(u => u.UniqueId == mount.UniqueId) == 1, "unique-native-actors");
                    var bindings = controls.CapturePersistentSlots();
                    Check(bindings.Length == persistence.LoadedData.Slots.Length && persistence.LoadedData.Slots.All(
                        s => bindings.Any(x => x.ActorId == s.ActorId && x.Index == s.Index && x.Kind == s.Kind)),
                        "cold-owned-hotbar-bindings");
                    Check(controls.NativeCastRequestCount == 0, "cold-restoration-did-not-cast-mount");
                }
                else
                {
                    UnitEntityData selectedRider; UnitEntityData selectedMount; string error;
                    if (!relationship.TryResolveAutomationPair(out selectedRider, out selectedMount, out error))
                        throw new InvalidOperationException(error);
                    rider = selectedRider; mount = selectedMount;
                    Check(!game.Player.IsInCombat && relationship.MountRiderOn(rider, mount).Succeeded, "mount-before-combat");
                    controls.Update();
                    BindOwnedControlSlots();
                }
                controls.Update();
                beforeControls = controls.CaptureSnapshot();
                Check(beforeControls.ExactFactCount > 0 && beforeControls.DuplicateFactCount == 0 &&
                    !beforeControls.SerializationSuspended, "controls-present-once");
                Write("initial");
                if (Cold) { stage = 2; return; }
                var descriptor = game.SaveManager.CreateNewSave("KMC_P01");
                Check(descriptor.Name == "KMC_P01" && descriptor.Type == SaveInfo.SaveType.Manual &&
                    game.SaveManager.IsSaveAllowed(), "actual-native-manual-admission");
                game.SaveGame(descriptor, () => callback = true);
                stage = 1; return;
            }
            if (stage == 1)
            {
                if (!callback) return;
                var saved = game.SaveManager.SingleOrDefault(s => s.Name == "KMC_P01");
                if (saved == null || saved.OperationState != SaveInfo.StateType.None || !saved.HasFileOnDisk) return;
                var read = NativeMountedSaveStorage.Read(saved.Saver);
                Check(read.Kind == MountedSaveReadKind.Current && read.Data.Mounted &&
                    read.Data.Rider.Id == rider.UniqueId && read.Data.Mount.Id == mount.UniqueId, "actual-archive-pair-metadata");
                Check(persistence.SnapshotCount == 1 && relationship.State == RelationshipState.Mounted &&
                    relationship.Rider == rider && relationship.Mount == mount, "save-retains-live-pair");
                var after = controls.CaptureSnapshot();
                Check(after.ExactFactCount == beforeControls.ExactFactCount && after.DuplicateFactCount == 0 &&
                    after.ManagedHotbarSlotCount == beforeControls.ManagedHotbarSlotCount && !after.SerializationSuspended,
                    "save-restores-owned-controls-once");
                Check(!game.IsPaused && game.CurrentMode == Kingmaker.GameModes.GameModeType.Default, "save-resumes-native-play");
                var elapsedGame = Math.Max(0, (game.TimeController.GameTime.Ticks - read.Data.GameTimeTicks) / (double)TimeSpan.TicksPerSecond);
                Check(LegitimateContinuation(read.Data.Rider, MountedPersistenceService.CaptureActor(rider), elapsedGame) &&
                    LegitimateContinuation(read.Data.Mount, MountedPersistenceService.CaptureActor(mount), elapsedGame),
                    "save-preserves-legitimate-native-debt");
                var file = new FileInfo(saved.FolderName);
                Write("native-write-complete", new JObject
                {
                    ["path"] = saved.FolderName, ["sha256"] = Hash(saved.FolderName), ["length"] = file.Length,
                    ["nativeType"] = saved.Type.ToString(), ["snapshot"] = JObject.FromObject(read.Data, MountedSaveCodec.CreateSerializer()),
                    ["nativeCallback"] = callback, ["operation"] = saved.OperationState.ToString()
                });
                stage = 2; return;
            }
            if (stage == 2)
            {
                if (rider.Commands.Move != null || mount.Commands.Move != null) return;
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                // SetAbility(null) still enters Ability mode in this native build.
                // Use the native Escape/cancel path before an ordinary point click.
                game.DefaultPointerController.ClearPointerMode();
                Check(game.DefaultPointerController.Mode == Kingmaker.Controllers.Clicks.PointerMode.Default &&
                    game.SelectedAbilityHandler.Ability == null, "native-pointer-cancel-before-ground-input");
                origin = mount.Position; destination = FindDestination(3f);
                using (var input = new NativeOrdinaryAttackInput(destination))
                    Check(input.Click(), "ordinary-ground-input");
                move = mount.Commands.Move as UnitMoveTo;
                Check(move != null && move.Executor == mount && move.CreatedByPlayer, "native-mount-movement-owner");
                Write("movement-dispatched"); stage = 3; return;
            }
            if (stage == 3)
            {
                if (!move.IsFinished) return;
                Check(move.Result == UnitCommand.ResultType.Success &&
                    GeometryUtils.MechanicsDistance(origin, mount.Position) > 1f, "normal-native-movement");
                Check(relationship.State == RelationshipState.Mounted &&
                    rider.CombatState.Cooldown.MoveAction <= 0.001f, "transport-retains-pair-without-rider-move-tax");
                Write("movement-completed", NativeGroundMovementObservation.Capture(mount, move));
                realtime = new NativeModeTransitionProbe(false);
                realtime.DispatchTemporaryValueIfRequired();
                targetService = new DiagnosticCombatTargetService(logger);
                var target = targetService.Spawn(rider, mount, FindDestination(8f), request.RunId, true, true);
                Check(targetService.PrepareForPlayerClick(target), "owned-target-prepared");
                Check(targetService.QueueBidirectionalCombatMemory(rider, target), "native-combat-requested");
                stage = 4; return;
            }
            if (stage == 4)
            {
                if (!game.Player.IsInCombat || !rider.CombatState.CanActInCombat) return;
                Check(!TurnBased.Controllers.CombatController.IsInTurnBasedCombat(), "native-rt-control");
                var target = targetService.Target;
                Check(targetService.PrepareForPlayerClick(target) && targetService.BeginExpectedAttackDispatch(target),
                    "ordinary-attack-fixture-admission");
                ruleProbe = new MountedCombatRuleProbe();
                ruleProbe.Arm(rider, mount, rider, target);
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                using (var input = new NativeOrdinaryAttackInput(target)) Check(input.Click(), "ordinary-attack-input");
                Write("attack-dispatched"); stage = 5; return;
            }
            if (stage == 5)
            {
                targetService.RefreshBidirectionalCombatMemoryLease();
                if (ruleProbe.AttackRuleCount < 1 || ruleProbe.AttackRollCount < 1) return;
                Check(ruleProbe.LastInitiatorId == rider.UniqueId && ruleProbe.LastTargetId == targetService.TargetId &&
                    ruleProbe.UnexpectedPairAttackCount == 0, "native-ordinary-attack-delivered");
                Write("attack-delivered", new JObject { ["rules"] = ruleProbe.AttackRuleCount,
                    ["rolls"] = ruleProbe.AttackRollCount, ["damage"] = ruleProbe.TotalDamage });
                SelectionManager.Instance.Stop();
                stage = 6; return;
            }
            if (stage == 6)
            {
                if (!targetService.DestroyAndVerify()) return;
                Check(relationship.State == RelationshipState.Mounted, "mounted-continuation-after-save-or-cold-load");
                Write("usable-continuation-complete");
                Dispose();
                Result = new RuntimeSubscenarioResult { Name = request.Scenario, Status = "PASS",
                    AssertionPassCount = passed, AssertionFailCount = 0, Errors = new string[0] };
                Completed = true;
            }
        }

        private void BindOwnedControlSlots()
        {
            var kinds = new[] { controls.DismountAbility, controls.RiderPrimaryAbility };
            foreach (var blueprint in kinds)
            {
                var fact = rider.Descriptor.Abilities.GetAbility(blueprint);
                var slots = rider.UISettings.Slots;
                var index = Array.FindIndex(slots, s => s == null || s is MechanicActionBarSlotEmpty);
                Check(index >= 0 && index < 128 && fact != null, "available-owned-control-slot");
                rider.UISettings.SetSlot(new MechanicActionBarSlotAbility { Unit = rider, Ability = fact.Data }, index);
            }
        }

        private Vector3 FindDestination(float distance)
        {
            for (var i = 0; i < 16; i++)
            {
                var wanted = mount.Position + Quaternion.Euler(0, i * 22.5f, 0) * Vector3.forward * distance;
                var actual = Kingmaker.View.ObstacleAnalyzer.TraceAlongNavmesh(mount.Position, wanted);
                if (GeometryUtils.MechanicsDistance(actual, wanted) <= 0.25f &&
                    GeometryUtils.MechanicsDistance(actual, mount.Position) > distance - 0.5f) return actual;
            }
            throw new InvalidOperationException("No native walkable destination exists for the owned P01 fixture.");
        }

        private void Check(bool condition, string label)
        {
            if (!condition) { Write("assertion-failed", new JObject { ["assertion"] = label }); throw new InvalidOperationException(label); }
            passed++;
        }

        private void Write(string kind, JObject detail = null)
        {
            var row = new JObject
            {
                ["runId"] = request.RunId, ["scenario"] = request.Scenario, ["processId"] = Process.GetCurrentProcess().Id,
                ["kind"] = kind, ["stage"] = stage, ["time"] = DateTimeOffset.UtcNow.ToString("o"),
                ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks, ["source"] = request.Commit,
                ["dll"] = request.DllSha256, ["relationship"] = relationship.State.ToString(),
                ["rider"] = rider == null ? null : JObject.FromObject(MountedPersistenceService.CaptureActor(rider), MountedSaveCodec.CreateSerializer()),
                ["mount"] = mount == null ? null : JObject.FromObject(MountedPersistenceService.CaptureActor(mount), MountedSaveCodec.CreateSerializer()),
                ["controls"] = JObject.FromObject(controls.CaptureSnapshot(), MountedSaveCodec.CreateSerializer()),
                ["native"] = new JObject { ["paused"] = Game.Instance.IsPaused,
                    ["mode"] = Game.Instance.CurrentMode.ToString(), ["partyCombat"] = Game.Instance.Player.IsInCombat,
                    ["riderCombat"] = rider?.IsInCombat, ["mountCombat"] = mount?.IsInCombat,
                    ["riderCanAct"] = rider?.CombatState.CanActInCombat,
                    ["targetCombat"] = targetService?.Target?.IsInCombat, ["targetId"] = targetService?.TargetId },
                ["persistence"] = new JObject { ["semantics"] = persistence.SemanticRestoreCount,
                    ["presentation"] = persistence.PresentationRestoreCount, ["feedback"] = persistence.Feedback },
                ["detail"] = detail
            };
            File.AppendAllText(evidence, row.ToString(Formatting.None) + Environment.NewLine);
        }

        private static bool LegitimateContinuation(SavedNativeActor before, SavedNativeActor after, double elapsed)
        {
            var a = new[] { before.Standard, before.Move, before.Swift, before.Initiative, before.Reaction };
            var b = new[] { after.Standard, after.Move, after.Swift, after.Initiative, after.Reaction };
            return before.Id == after.Id && before.ReactionsRemaining == after.ReactionsRemaining &&
                before.LastSurpriseTicks == after.LastSurpriseTicks &&
                Enumerable.Range(0, a.Length).All(i => b[i] <= a[i] + 0.001f && b[i] + elapsed + 0.001 >= a[i]);
        }

        private static string Hash(string path)
        {
            using (var sha = SHA256.Create()) using (var stream = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        public void Dispose()
        {
            if (disposed) return;
            targetService?.Dispose(); ruleProbe?.Dispose(); realtime?.Dispose();
            relationship.Dismount(CleanupTrigger.ProcessTeardown);
            settings.EnableUnsafeMovementExperiment = false;
            disposed = true;
        }
    }
}
