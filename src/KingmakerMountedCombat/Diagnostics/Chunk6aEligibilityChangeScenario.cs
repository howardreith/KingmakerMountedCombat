using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal const string Chunk6aSizeFormChangeScenario = "chunk6a-size-form-change";
        internal const string Chunk6aLostDirectControlScenario = "chunk6a-lost-direct-control";
        private const string Chunk6aSizeFormRow = "CM02-size-form-change";
        private const string Chunk6aLostDirectControlRow = "CM02-lost-direct-control";
        private bool Chunk6aSizeFormOnly { get { return request.Scenario == Chunk6aSizeFormChangeScenario; } }
        private bool Chunk6aLostDirectControlOnly { get { return request.Scenario == Chunk6aLostDirectControlScenario; } }

        private JObject chunk6aSizeEvidence, chunk6aSizeOriginal;
        private UnitUseAbility chunk6aSizeCommand;
        private Buff chunk6aSizeBuff;
        private bool chunk6aSizeStimulated;
        private int chunk6aSizeStimulusCount, chunk6aSizeRemovalCount, chunk6aSizeCleanupInterruptCount;

        private JObject chunk6aControlEvidence, chunk6aControlOriginal;
        private UnitUseAbility chunk6aControlCommand;
        private NativePendingMountFearLease chunk6aFearLease;
        private NativeFearControlProbe chunk6aFearProbe;
        private bool chunk6aControlStimulated, chunk6aFearRestorationRequested;
        private int chunk6aControlStimulusCount, chunk6aControlRemovalCount, chunk6aControlCleanupInterruptCount;
        private int chunk6aControlCleanupTraceStart = -1;

        private void TickChunk6aSizeFormChange()
        {
            if (!Chunk6aSizeFormOnly || Chunk6aTurnBased)
                throw new InvalidOperationException("Size/form change requires its isolated RT transaction.");
            var blueprint = RuntimePersistenceScenario.FindEligibilityBuff();
            if (chunk6aStage == 38)
            {
                if (!Chunk6aIdle || !nativeControls.Evaluate(NativeMountedControlKind.MountCompanion, rider).IsEnabled) return;
                if (!EnsureChunk6aRiderSelection(Chunk6aSizeFormRow)) return;
                var start = CaptureChunk6aGeometry("size-form-pre-click");
                if ((bool)start["isAdjacent"])
                    throw new InvalidOperationException("Size/form-change Mount must start outside transition reach.");
                chunk6aSizeOriginal = CaptureChunk6aSizeState(blueprint);
                chunk6aSizeEvidence = new JObject {
                    ["contract"] = "native-size-fact-invalidates-exact-mount-approach",
                    ["start"] = start, ["stateBefore"] = chunk6aSizeOriginal.DeepClone(),
                    ["diagnosticInterruptCount"] = 0
                };
                observations["chunk6aSizeFormChange"] = chunk6aSizeEvidence;
                if (blueprint == null || (string)chunk6aSizeOriginal["buffName"] != RuntimePersistenceScenario.EligibilityBuffName ||
                    (int)chunk6aSizeOriginal["changeUnitSizeComponents"] != 1 || (int)chunk6aSizeOriginal["buffCount"] != 0 ||
                    (int)chunk6aSizeOriginal["riderSize"] != 4 || (int)chunk6aSizeOriginal["mountSize"] <= 4 ||
                    (int)chunk6aSizeOriginal["riderPolymorphObject"] != 0 || (int)chunk6aSizeOriginal["mountPolymorphObject"] != 0)
                {
                    FailCurrent(Chunk6aSizeFormRow, "Size/form fixture lacks one unowned authored Enlarge Person size effect or the exact Medium/larger unpolymorphed pair: " + chunk6aSizeOriginal);
                    BeginCleanup(); return;
                }
                chunk6aCommandWindow = new NativeRelationshipCommandProbe(nativeControls, allocationTrace, rider, horse,
                    nativeControls.MountAbility.AssetGuid, CaptureChunk6aCausalState, true);
                var clicked = TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "chunk6a-size-form-change-click");
                chunk6aCommandWindow.ClickCompleted(clicked);
                if (!clicked)
                {
                    FailCurrent(Chunk6aSizeFormRow, "Exact rider native Mount click was refused before the size change: " + playerAction.LastFeedback);
                    BeginCleanup(); return;
                }
                chunk6aSizeCommand = chunk6aCommandWindow.Command;
                chunk6aStage = 39; ResetLeafClock(); return;
            }

            var command = chunk6aCommandWindow == null ? null : chunk6aCommandWindow.Command;
            if (!chunk6aSizeStimulated)
            {
                if (command == null || !ReferenceEquals(command, chunk6aSizeCommand) || command.IsStarted || command.IsActed ||
                    command.IsFinished || command.ExecutionProcess != null)
                {
                    FailCurrent(Chunk6aSizeFormRow, "The exact Mount left pending approach before the native size fact.");
                    BeginCleanup(); return;
                }
                var moved = Chunk6aPlanarDistance((JObject)chunk6aSizeEvidence["start"]["riderPosition"], CapturePosition(rider.Position));
                if (!chunk6aCommandWindow.ApproachObserved || !rider.View.AgentASP.IsReallyMoving || moved <= 0.25f) return;
                var geometry = CaptureChunk6aGeometry("size-form-trigger");
                var slot = rider.Commands.GetCommand(UnitCommand.CommandType.Move);
                var selected = SelectionManager.Instance.SelectedUnits;
                var before = CaptureChunk6aSizeState(blueprint);
                if ((bool)geometry["isAdjacent"] || !ReferenceEquals(slot, command) || selected.Count != 1 || selected[0] != rider ||
                    !JToken.DeepEquals(before, chunk6aSizeOriginal))
                {
                    FailCurrent(Chunk6aSizeFormRow, "Size change missed the exact selected rider's non-adjacent pending Mount or its native inputs changed first.");
                    BeginCleanup(); return;
                }
                chunk6aSizeEvidence["trigger"] = CaptureChunk6aInvalidationTrigger(command, slot, geometry, moved);
                var stimulus = new JObject { ["contract"] = "one-authored-enlarge-person-buff-through-native-rulebook",
                    ["before"] = before.DeepClone(), ["commandBefore"] = CaptureOrdinaryCommand(command),
                    ["frameBefore"] = Time.frameCount, ["gameTicksBefore"] = Game.Instance.TimeController.GameTime.Ticks };
                chunk6aSizeEvidence["stimulus"] = stimulus;
                chunk6aSizeStimulated = true; chunk6aSizeStimulusCount++;
                chunk6aSizeBuff = rider.Buffs.AddBuff(blueprint, rider, TimeSpan.FromMinutes(10));
                var after = CaptureChunk6aSizeState(blueprint);
                stimulus["count"] = chunk6aSizeStimulusCount;
                stimulus["buffObject"] = chunk6aSizeBuff == null ? 0 : RuntimeHelpers.GetHashCode(chunk6aSizeBuff);
                stimulus["after"] = after.DeepClone(); stimulus["commandAfter"] = CaptureOrdinaryCommand(command);
                stimulus["frameAfter"] = Time.frameCount; stimulus["gameTicksAfter"] = Game.Instance.TimeController.GameTime.Ticks;
                chunk6aSizeEvidence["stateAfterApplication"] = after.DeepClone();
                if (chunk6aSizeBuff == null || (int)after["buffCount"] != 1 || (int)after["riderSize"] <= 4 ||
                    (int)after["riderSize"] < (int)after["mountSize"] || (int)after["riderPolymorphObject"] != 0)
                {
                    FailCurrent(Chunk6aSizeFormRow, "The authored Enlarge Person buff did not create the exact unsupported size state.");
                    BeginCleanup(); return;
                }
                ResetLeafClock(); return;
            }

            if (command == null || command.IsActed || command.ExecutionProcess != null || relationship.State != RelationshipState.Unmounted)
            {
                FailCurrent(Chunk6aSizeFormRow, "The size-invalidated Mount acted, acquired a process, or transitioned instead of ending unacted.");
                BeginCleanup(); return;
            }
            if (!chunk6aCommandWindow.UnactedTerminal) return;
            var proof = chunk6aCommandWindow.FinishUnacted("unacted-native-size-form-change-no-cost-or-transition");
            chunk6aSizeEvidence["commandProof"] = proof;
            chunk6aSizeEvidence["terminal"] = CaptureOrdinaryCommand(command);
            chunk6aSizeEvidence["stateBeforeRemoval"] = CaptureChunk6aSizeState(blueprint);
            chunk6aCommandWindow.Dispose(); chunk6aCommandWindow = null;
            var removedObject = RuntimeHelpers.GetHashCode(chunk6aSizeBuff);
            chunk6aSizeBuff.Remove(); chunk6aSizeBuff = null; chunk6aSizeRemovalCount++;
            var restoredState = CaptureChunk6aSizeState(blueprint);
            chunk6aSizeEvidence["removal"] = new JObject { ["contract"] = "remove-only-exact-owned-native-buff-after-command-terminal",
                ["count"] = chunk6aSizeRemovalCount, ["buffObject"] = removedObject,
                ["frame"] = Time.frameCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
                ["after"] = restoredState.DeepClone() };
            chunk6aSizeEvidence["stateAfterRestoration"] = restoredState.DeepClone();
            chunk6aSizeEvidence["stimulusCount"] = chunk6aSizeStimulusCount;
            chunk6aSizeEvidence["removalCount"] = chunk6aSizeRemovalCount;
            chunk6aSizeEvidence["diagnosticInterruptCount"] = chunk6aSizeCleanupInterruptCount;
            var restored = JToken.DeepEquals(chunk6aSizeOriginal, restoredState);
            var noResidue = relationship.State == RelationshipState.Unmounted && !playerAction.HasVoluntaryTransitionInFlight &&
                rider.Commands.Empty && horse.Commands.Empty;
            chunk6aSizeEvidence["restored"] = restored; chunk6aSizeEvidence["noResidue"] = noResidue;
            var pass = (bool)proof["pass"] && restored && noResidue && chunk6aSizeStimulusCount == 1 &&
                chunk6aSizeRemovalCount == 1 && chunk6aSizeCleanupInterruptCount == 0;
            AddRow(Chunk6aSizeFormRow, pass,
                pass ? "One authored Enlarge Person buff changed the exact pending rider from Medium to an unsupported Large/equal-size state; that same Mount ended unacted with no process, cost, transition or residue, then removal of only the owned buff restored the original native state." :
                "The exact native size-change terminal or owned-buff restoration proof failed.", chunk6aSizeEvidence);
            chunk6aStage = 99; BeginCleanup();
        }

        private void TickChunk6aLostDirectControl()
        {
            if (!Chunk6aLostDirectControlOnly || Chunk6aTurnBased)
                throw new InvalidOperationException("Lost-direct-control requires its isolated RT transaction.");
            if (chunk6aStage == 40)
            {
                if (!Chunk6aIdle || !nativeControls.Evaluate(NativeMountedControlKind.MountCompanion, rider).IsEnabled) return;
                if (!EnsureChunk6aRiderSelection(Chunk6aLostDirectControlRow)) return;
                var start = CaptureChunk6aGeometry("lost-control-pre-click");
                if ((bool)start["isAdjacent"])
                    throw new InvalidOperationException("Lost-direct-control Mount must start outside transition reach.");
                chunk6aControlOriginal = CaptureChunk6aDirectControlState();
                chunk6aControlEvidence = new JObject {
                    ["contract"] = "native-fear-controller-invalidates-exact-mount-approach",
                    ["start"] = start, ["stateBefore"] = chunk6aControlOriginal.DeepClone(),
                    ["diagnosticInterruptCount"] = 0
                };
                observations["chunk6aLostDirectControl"] = chunk6aControlEvidence;
                if (!(bool)chunk6aControlOriginal["directlyControllable"] || (bool)chunk6aControlOriginal["panicked"] ||
                    (bool)chunk6aControlOriginal["frightened"] || (bool)chunk6aControlOriginal["frightenedImmune"] ||
                    (int)chunk6aControlOriginal["visibleConsciousEnemies"] < 1)
                {
                    FailCurrent(Chunk6aLostDirectControlRow, "Lost-control fixture lacks a clean directly controlled rider and one visible conscious enemy for the native fear controller: " + chunk6aControlOriginal);
                    BeginCleanup(); return;
                }
                chunk6aCommandWindow = new NativeRelationshipCommandProbe(nativeControls, allocationTrace, rider, horse,
                    nativeControls.MountAbility.AssetGuid, CaptureChunk6aCausalState, true);
                var clicked = TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "chunk6a-lost-direct-control-click");
                chunk6aCommandWindow.ClickCompleted(clicked);
                if (!clicked)
                {
                    FailCurrent(Chunk6aLostDirectControlRow, "Exact rider native Mount click was refused before native fear: " + playerAction.LastFeedback);
                    BeginCleanup(); return;
                }
                chunk6aControlCommand = chunk6aCommandWindow.Command;
                chunk6aStage = 41; ResetLeafClock(); return;
            }

            var command = chunk6aControlCommand;
            if (chunk6aStage == 41 && !chunk6aControlStimulated)
            {
                if (command == null || !ReferenceEquals(command, chunk6aCommandWindow == null ? null : chunk6aCommandWindow.Command) ||
                    command.IsStarted || command.IsActed || command.IsFinished || command.ExecutionProcess != null)
                {
                    FailCurrent(Chunk6aLostDirectControlRow, "The exact Mount left pending approach before native fear control loss.");
                    BeginCleanup(); return;
                }
                var moved = Chunk6aPlanarDistance((JObject)chunk6aControlEvidence["start"]["riderPosition"], CapturePosition(rider.Position));
                if (!chunk6aCommandWindow.ApproachObserved || !rider.View.AgentASP.IsReallyMoving || moved <= 0.25f) return;
                var geometry = CaptureChunk6aGeometry("lost-control-trigger");
                var slot = rider.Commands.GetCommand(UnitCommand.CommandType.Move);
                var selected = SelectionManager.Instance.SelectedUnits;
                var before = CaptureChunk6aDirectControlState();
                if ((bool)geometry["isAdjacent"] || !ReferenceEquals(slot, command) || selected.Count != 1 || selected[0] != rider ||
                    !JToken.DeepEquals(before, chunk6aControlOriginal))
                {
                    FailCurrent(Chunk6aLostDirectControlRow, "Native fear missed the exact selected rider's non-adjacent pending Mount or its control inputs changed first.");
                    BeginCleanup(); return;
                }
                chunk6aControlEvidence["trigger"] = CaptureChunk6aInvalidationTrigger(command, slot, geometry, moved);
                chunk6aFearProbe = new NativeFearControlProbe(rider, command, CaptureChunk6aCausalState);
                chunk6aFearLease = new NativePendingMountFearLease(rider);
                chunk6aControlStimulated = true; chunk6aControlStimulusCount++;
                var immediate = CaptureChunk6aDirectControlState();
                chunk6aControlEvidence["stimulus"] = new JObject { ["contract"] = "one-owned-native-frightened-fact",
                    ["count"] = chunk6aControlStimulusCount, ["commandBefore"] = CaptureOrdinaryCommand(command),
                    ["lease"] = chunk6aFearLease.Capture(), ["immediateState"] = immediate,
                    ["frame"] = Time.frameCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks };
                if (!(bool)immediate["frightened"] || (bool)immediate["panicked"] || !(bool)immediate["directlyControllable"])
                {
                    FailCurrent(Chunk6aLostDirectControlRow, "The native Frightened fact bypassed or failed the required UnitFearController boundary.");
                    BeginCleanup(); return;
                }
                ResetLeafClock(); return;
            }

            if (chunk6aStage == 41)
            {
                if (command == null || command.IsActed || command.ExecutionProcess != null || relationship.State != RelationshipState.Unmounted)
                {
                    FailCurrent(Chunk6aLostDirectControlRow, "The control-invalidated Mount acted, acquired a process, or transitioned instead of ending unacted.");
                    BeginCleanup(); return;
                }
                if (!chunk6aFearProbe.LossObserved) return;
                if (!chunk6aCommandWindow.UnactedTerminal) return;
                var proof = chunk6aCommandWindow.FinishUnacted("unacted-native-lost-direct-control-no-cost-or-transition");
                chunk6aControlEvidence["commandProof"] = proof;
                chunk6aControlEvidence["terminal"] = CaptureOrdinaryCommand(command);
                chunk6aControlEvidence["stateAtControlLoss"] = CaptureChunk6aDirectControlState();
                chunk6aCommandWindow.Dispose(); chunk6aCommandWindow = null;
                chunk6aControlCleanupTraceStart = allocationTrace.EventCount;
                chunk6aControlEvidence["leaseBeforeRemoval"] = chunk6aFearLease.Capture();
                chunk6aFearLease.Dispose(); chunk6aControlRemovalCount++;
                chunk6aFearRestorationRequested = true;
                chunk6aControlEvidence["leaseAfterRemoval"] = chunk6aFearLease.Capture();
                chunk6aControlEvidence["stateAfterFactRemoval"] = CaptureChunk6aDirectControlState();
                chunk6aStage = 42; ResetLeafClock(); return;
            }

            if (!chunk6aFearProbe.RestorationObserved) return;
            var restoredState = CaptureChunk6aDirectControlState();
            var probe = chunk6aFearProbe.Capture();
            var cleanupEvents = new JArray(allocationTrace.EventsSince(chunk6aControlCleanupTraceStart));
            var cleanupNoCost = !cleanupEvents.OfType<JObject>().Any(item => {
                var name = (string)item["boundary"];
                return name.StartsWith("cost-", StringComparison.Ordinal) || name.StartsWith("actor-cost-", StringComparison.Ordinal) ||
                    name.StartsWith("prepare-", StringComparison.Ordinal) || name.StartsWith("clear-", StringComparison.Ordinal) ||
                    name.StartsWith("combat-clear", StringComparison.Ordinal);
            });
            chunk6aControlEvidence["fearController"] = probe;
            chunk6aControlEvidence["restoration"] = new JObject { ["contract"] = "native-fear-controller-restores-control-after-owned-fact-removal",
                ["removalCount"] = chunk6aControlRemovalCount, ["state"] = restoredState,
                ["allocationEvents"] = cleanupEvents, ["noCostOrPreparationCallbacks"] = cleanupNoCost,
                ["frame"] = Time.frameCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks };
            chunk6aControlEvidence["stimulusCount"] = chunk6aControlStimulusCount;
            chunk6aControlEvidence["removalCount"] = chunk6aControlRemovalCount;
            chunk6aControlEvidence["diagnosticInterruptCount"] = chunk6aControlCleanupInterruptCount;
            var restored = Chunk6aDirectControlRestored(chunk6aControlOriginal, restoredState);
            var noResidue = relationship.State == RelationshipState.Unmounted && !playerAction.HasVoluntaryTransitionInFlight &&
                rider.Commands.Empty && horse.Commands.Empty;
            chunk6aControlEvidence["restored"] = restored; chunk6aControlEvidence["noResidue"] = noResidue;
            var commandProof = (JObject)chunk6aControlEvidence["commandProof"];
            var pass = (bool)commandProof["pass"] && (bool)probe["complete"] && (bool)probe["lossObserved"] &&
                (bool)probe["restorationObserved"] && cleanupNoCost && restored && noResidue && chunk6aControlStimulusCount == 1 &&
                chunk6aControlRemovalCount == 1 && chunk6aControlCleanupInterruptCount == 0;
            chunk6aFearProbe.Dispose(); chunk6aFearProbe = null;
            AddRow(Chunk6aLostDirectControlRow, pass,
                pass ? "One owned native Frightened fact reached the installed UnitFearController, which removed direct control and interrupted the exact pending Mount before action/cost/transition; removal of only that Fact let the same controller restore control and clear its native flee command without resource callbacks or residue." :
                "The exact native direct-control loss, unacted terminal, or native restoration proof failed.", chunk6aControlEvidence);
            chunk6aStage = 99; BeginCleanup();
        }

        private JObject CaptureChunk6aSizeState(BlueprintBuff blueprint)
        {
            var riderPolymorph = rider.GetActivePolymorph();
            var mountPolymorph = horse.GetActivePolymorph();
            var buffs = blueprint == null ? new Buff[0] : rider.Buffs.Enumerable.Where(item => item.Blueprint == blueprint).ToArray();
            return new JObject { ["riderId"] = rider.UniqueId, ["mountId"] = horse.UniqueId,
                ["buffName"] = blueprint == null ? null : blueprint.name, ["buffGuid"] = blueprint == null ? null : blueprint.AssetGuid,
                ["changeUnitSizeComponents"] = blueprint == null ? 0 : blueprint.ComponentsArray.Count(item => item != null && item.GetType().FullName == "Kingmaker.Designers.Mechanics.Buffs.ChangeUnitSize"),
                ["buffCount"] = buffs.Length, ["buffObjects"] = new JArray(buffs.Select(RuntimeHelpers.GetHashCode)),
                ["riderSize"] = (int)rider.Descriptor.State.Size, ["mountSize"] = (int)horse.Descriptor.State.Size,
                ["riderPolymorphObject"] = riderPolymorph == null ? 0 : RuntimeHelpers.GetHashCode(riderPolymorph),
                ["mountPolymorphObject"] = mountPolymorph == null ? 0 : RuntimeHelpers.GetHashCode(mountPolymorph) };
        }

        private JObject CaptureChunk6aDirectControlState()
        {
            var visible = rider.Memory.Enemies.Where(item => item.Unit != null && item.Unit.Descriptor.State.IsConscious && rider.HasLOS(item.Unit)).ToArray();
            return new JObject { ["riderId"] = rider.UniqueId, ["mountId"] = horse.UniqueId,
                ["directlyControllable"] = rider.IsDirectlyControllable, ["inGame"] = rider.IsInGame,
                ["panicked"] = rider.Descriptor.State.IsPanicked,
                ["frightened"] = rider.Descriptor.State.HasCondition(UnitCondition.Frightened),
                ["frightenedImmune"] = rider.Descriptor.State.HasConditionImmunity(UnitCondition.Frightened),
                ["visibleConsciousEnemies"] = visible.Length,
                ["visibleEnemyIds"] = new JArray(visible.Select(item => item.Unit.UniqueId)) };
        }

        private static bool Chunk6aDirectControlRestored(JObject before, JObject after)
        {
            if (before == null || after == null) return false;
            var fields = new[] { "riderId", "mountId", "directlyControllable", "inGame", "panicked", "frightened", "frightenedImmune" };
            return fields.All(field => JToken.DeepEquals(before[field], after[field]));
        }

        private JObject CaptureChunk6aInvalidationTrigger(UnitCommand command, UnitCommand slot, JObject geometry, float moved)
        {
            return new JObject { ["frame"] = Time.frameCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
                ["approachObserved"] = true, ["riderReallyMoving"] = rider.View.AgentASP.IsReallyMoving,
                ["riderDisplacement"] = moved, ["commandObject"] = RuntimeHelpers.GetHashCode(command),
                ["moveSlotObject"] = RuntimeHelpers.GetHashCode(slot), ["started"] = command.IsStarted,
                ["acted"] = command.IsActed, ["finished"] = command.IsFinished, ["geometry"] = geometry };
        }

        private bool CaptureChunk6aEligibilityDeadline()
        {
            if (Chunk6aSizeFormOnly && chunk6aStage >= 38 && chunk6aStage <= 39)
            {
                if (chunk6aSizeEvidence != null) {
                    chunk6aSizeEvidence["deadlineState"] = CaptureChunk6aSizeState(RuntimePersistenceScenario.FindEligibilityBuff());
                    chunk6aSizeEvidence["deadlineCommand"] = CaptureOrdinaryCommand(chunk6aSizeCommand);
                }
                FailCurrent(Chunk6aSizeFormRow, chunk6aSizeStimulated ?
                    "The exact size-invalidated Mount did not reach a native unacted terminal at the unchanged 30-second deadline." :
                    "The exact Mount did not reach the measured approach boundary for size invalidation at the unchanged 30-second deadline.");
                return true;
            }
            if (Chunk6aLostDirectControlOnly && chunk6aStage >= 40 && chunk6aStage <= 42)
            {
                if (chunk6aControlEvidence != null) {
                    chunk6aControlEvidence["deadlineState"] = CaptureChunk6aDirectControlState();
                    chunk6aControlEvidence["deadlineCommand"] = CaptureOrdinaryCommand(chunk6aControlCommand);
                    if (chunk6aFearProbe != null) chunk6aControlEvidence["deadlineFearController"] = chunk6aFearProbe.Capture();
                }
                FailCurrent(Chunk6aLostDirectControlRow, !chunk6aControlStimulated ?
                    "The exact Mount did not reach the measured approach boundary for native fear at the unchanged 30-second deadline." :
                    !chunk6aFearRestorationRequested ? "The native fear controller did not remove control and terminate the exact Mount before the unchanged deadline." :
                    "The native fear controller did not restore control after exact Fact removal before the unchanged deadline.");
                return true;
            }
            return false;
        }

        private void CleanupChunk6aEligibilityChanges()
        {
            if (Chunk6aSizeFormOnly)
            {
                if (chunk6aSizeBuff != null) { chunk6aSizeBuff.Remove(); chunk6aSizeBuff = null; chunk6aSizeRemovalCount++; }
                if (chunk6aSizeCommand != null && !chunk6aSizeCommand.IsFinished) { chunk6aSizeCommand.Interrupt(); chunk6aSizeCleanupInterruptCount++; }
            }
            if (Chunk6aLostDirectControlOnly)
            {
                if (chunk6aFearLease != null) { chunk6aFearLease.Dispose(); }
                if (chunk6aControlCommand != null && !chunk6aControlCommand.IsFinished) { chunk6aControlCommand.Interrupt(); chunk6aControlCleanupInterruptCount++; }
                if (chunk6aFearProbe != null) {
                    if (chunk6aControlEvidence != null) chunk6aControlEvidence["cleanupFearController"] = chunk6aFearProbe.Capture();
                    chunk6aFearProbe.Dispose(); chunk6aFearProbe = null;
                }
            }
        }
    }
}