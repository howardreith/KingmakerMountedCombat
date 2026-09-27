using System;
using System.Linq;
using Kingmaker;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        private NativeRelationshipCommandProbe chunk6aCommandWindow;
        private readonly JArray chunk6aCommandProofs = new JArray();
        private int chunk6aExplorationStage;
        private JObject chunk6aExplorationMountBefore, chunk6aExplorationMountAfter;
        private JObject chunk6aExplorationMountLedgerBefore, chunk6aExplorationMountProof;
        private JObject chunk6aExplorationDismountProof;

        private bool EnsureChunk6aRiderSelection(string row)
        {
            var manager = SelectionManager.Instance;
            if (manager != null && rider?.View != null)
                manager.SelectUnit(rider.View, true, true, false);
            var selectedUnits = manager?.SelectedUnits;
            var exact = selectedUnits != null && selectedUnits.Count == 1 && selectedUnits[0] == rider;
            var evidence = new JObject
            {
                ["riderId"] = rider?.UniqueId, ["selectedCount"] = selectedUnits?.Count ?? -1,
                ["selectedIds"] = selectedUnits == null ? null : new JArray(selectedUnits.Select(u => u?.UniqueId)),
                ["exactSingleRider"] = exact, ["frame"] = Time.frameCount
            };
            observations["chunk6aSelection-" + row] = evidence;
            if (exact) return true;
            FailCurrent(row, "Exact rider selection failed before SetAbility or OnClick: " + evidence.ToString(Formatting.None));
            BeginCleanup();
            return false;
        }

        private JObject CaptureChunk6aCausalState() => new JObject
        {
            ["rider"] = Chunk6aCooldowns(rider), ["mount"] = Chunk6aCooldowns(horse),
            ["selectedIds"] = new JArray(SelectionManager.Instance.SelectedUnits.Select(unit => unit.UniqueId)),
            ["ledger"] = Chunk6aLedgerCounters(), ["geometry"] = CaptureChunk6aGeometry("command-boundary"),
            ["relationshipState"] = relationship.State.ToString(),
            ["generation"] = relationship.MountedPairGeneration
        };

        private void BeginChunk6aCommandWindow(string abilityGuid)
        {
            if (chunk6aCommandWindow != null) throw new InvalidOperationException("Previous command window did not settle.");
            chunk6aCommandWindow = new NativeRelationshipCommandProbe(nativeControls, allocationTrace,
                rider, horse, abilityGuid, CaptureChunk6aCausalState);
        }

        private JObject FinishChunk6aCommandWindow(string name, bool inCombat, int partnerPrepares, bool requireApproach)
        {
            var proof = chunk6aCommandWindow.Finish(inCombat, Chunk6aTurnBased && inCombat, partnerPrepares, requireApproach);
            proof["window"] = name;
            chunk6aCommandProofs.Add(proof.DeepClone());
            observations["chunk6aCommandProofs"] = chunk6aCommandProofs;
            chunk6aCommandWindow.Dispose();
            chunk6aCommandWindow = null;
            return proof;
        }

        private bool Chunk6aExplorationWindowPassed(JObject proof, JObject before, string kind)
        {
            return (bool)proof["pass"] && Chunk6aLedgerDelta(before, "admitted" + kind, 1) &&
                Chunk6aLedgerDelta(before, "accepted" + kind, 1) &&
                Chunk6aLedgerDelta(before, "refusedVoluntary", 0) &&
                Chunk6aLedgerDelta(before, "forcedDetach", 0) &&
                Chunk6aLedgerDelta(before, "duplicateSuppressed", 0) &&
                Chunk6aLedgerDelta(before, "concurrentSuppressed", 0);
        }

        private void TickChunk6aExplorationAndSetup()
        {
            if (!Chunk6aIdle) return;
            if (rider.IsInCombat || horse.IsInCombat || Game.Instance.Player.IsInCombat)
            {
                FailCurrent("CM01-exploration-free", "Exploration window unexpectedly entered combat.");
                BeginCleanup(); return;
            }
            if (chunk6aExplorationStage == 0)
            {
                if (relationship.State != RelationshipState.Unmounted)
                {
                    FailCurrent("CM01-exploration-free", "Chunk 6A requires an unmounted parent handoff with no parent Mount.");
                    BeginCleanup(); return;
                }
                if (!EnsureChunk6aRiderSelection("CM01-exploration-free")) return;
                chunk6aExplorationMountBefore = CaptureChunk6aState("exploration-mount-before");
                chunk6aExplorationMountLedgerBefore = Chunk6aLedgerCounters();
                BeginChunk6aCommandWindow(nativeControls.MountAbility.AssetGuid);
                var clicked = TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "chunk6a-exploration-mount-click");
                chunk6aCommandWindow.ClickCompleted(clicked);
                if (!clicked) { FailCurrent("CM01-exploration-free", "Exploration Mount click was refused."); BeginCleanup(); return; }
                chunk6aExplorationStage = 1; ResetLeafClock(); return;
            }
            if (chunk6aExplorationStage == 1)
            {
                if (chunk6aCommandWindow?.Terminal != true) return;
                chunk6aExplorationMountProof = FinishChunk6aCommandWindow("exploration-mount", false, 0, false);
                chunk6aExplorationMountAfter = CaptureChunk6aState("exploration-mount-after");
                var passed = relationship.State == RelationshipState.Mounted &&
                    Chunk6aExplorationWindowPassed(chunk6aExplorationMountProof, chunk6aExplorationMountLedgerBefore, "Mount");
                if (!passed)
                {
                    AddRow("CM01-exploration-free", false, "The exploration Mount window did not prove one free exact transition.", chunk6aExplorationMountProof);
                    BeginCleanup(); return;
                }
                chunk6aExplorationStage = 2; ResetLeafClock(); return;
            }
            if (chunk6aExplorationStage == 2)
            {
                if (!EnsureChunk6aRiderSelection("CM01-exploration-dismount-costs-nothing")) return;
                chunk6aExplorationDismountBefore = CaptureChunk6aState("exploration-dismount-before");
                chunk6aExplorationLedgerBefore = Chunk6aLedgerCounters();
                BeginChunk6aCommandWindow(nativeControls.DismountAbility.AssetGuid);
                var clicked = TryNativeAbilityTargetClick(nativeControls.DismountAbility, rider, "chunk6a-exploration-dismount-click");
                chunk6aCommandWindow.ClickCompleted(clicked);
                if (!clicked) { FailCurrent("CM01-exploration-free", "Exploration Dismount click was refused."); BeginCleanup(); return; }
                chunk6aExplorationStage = 3; ResetLeafClock(); return;
            }
            if (chunk6aExplorationStage == 3)
            {
                if (chunk6aCommandWindow?.Terminal != true) return;
                chunk6aExplorationDismountProof = FinishChunk6aCommandWindow("exploration-dismount", false, 0, false);
                chunk6aExplorationDismountAfter = CaptureChunk6aState("exploration-dismount-after");
                var passed = relationship.State == RelationshipState.Unmounted &&
                    Chunk6aExplorationWindowPassed(chunk6aExplorationDismountProof, chunk6aExplorationLedgerBefore, "Dismount");
                AddRow("CM01-exploration-dismount-costs-nothing", passed,
                    "This exact exploration Dismount window added one accepted transition, no cost writes and no suppression or forced detach.",
                    new JObject { ["before"] = chunk6aExplorationDismountBefore, ["after"] = chunk6aExplorationDismountAfter,
                        ["ledgerBeforeWindow"] = chunk6aExplorationLedgerBefore, ["ledgerAfterWindow"] = Chunk6aLedgerCounters(),
                        ["commandProof"] = chunk6aExplorationDismountProof });
                AddRow("CM01-exploration-free", passed,
                    "Separate exact exploration Mount and Dismount windows each observed their own acted boundary and native callback without a resource write. Each window added no suppression or forced detach; carried counters are published independently.",
                    new JObject { ["mountBefore"] = chunk6aExplorationMountBefore, ["mountAfter"] = chunk6aExplorationMountAfter,
                        ["mountLedgerBefore"] = chunk6aExplorationMountLedgerBefore,
                        ["mountProof"] = chunk6aExplorationMountProof,
                        ["dismountBefore"] = chunk6aExplorationDismountBefore, ["dismountAfter"] = chunk6aExplorationDismountAfter,
                        ["dismountLedgerBefore"] = chunk6aExplorationLedgerBefore, ["ledgerAfter"] = Chunk6aLedgerCounters(),
                        ["dismountProof"] = chunk6aExplorationDismountProof });
                if (!passed) { BeginCleanup(); return; }
                chunk6aExplorationStage = 4; ResetLeafClock(); return;
            }
            if (chunk6aExplorationStage == 4)
            {
                if (!EnsureChunk6aRiderSelection("CM01-combat-mount-setup")) return;
                if (!PrepareUnmountedHorseAiIsolation() || !PrepareCombatMountRiderAiIsolation()) return;
                observations["chunk6aRiderAiIsolation"] = CaptureCombatMountRiderAiIsolation();
                observations["chunk6aHorseAiIsolation"] = CaptureUnmountedHorseAiIsolation();
                if (!Chunk6aCompensationOnly)
                {
                    // Create non-adjacent geometry outside combat, before the fresh native
                    // encounter allocation. The rider has issued no combat Mount.
                    if (chunk6aSeparationCommand == null)
                    {
                        string refusal;
                        chunk6aSeparationCommand = Chunk6aSendHorseAway(4f, out chunk6aSeparationDestination, out refusal);
                        if (chunk6aSeparationCommand == null)
                        {
                            FailCurrent("CM02-approach-arrival", "Pre-encounter separation failed: " + refusal);
                            BeginCleanup();
                        }
                        return;
                    }
                    if (!chunk6aSeparationCommand.IsFinished) return;
                    var separation = new JObject
                    {
                        ["command"] = CaptureOrdinaryCommand(chunk6aSeparationCommand),
                        ["destination"] = CapturePosition(chunk6aSeparationDestination),
                        ["geometry"] = CaptureChunk6aGeometry("pre-encounter-separation-terminal"),
                        ["movement"] = NativeGroundMovementObservation.Capture(horse, chunk6aSeparationCommand)
                    };
                    observations["chunk6aPreEncounterSeparation"] = separation;
                    if (chunk6aSeparationCommand.Result != UnitCommand.ResultType.Success)
                    {
                        FailCurrent("CM02-approach-arrival", "Pre-encounter native separation did not succeed: " + separation.ToString(Formatting.None));
                        BeginCleanup(); return;
                    }
                    var geometry = CaptureChunk6aGeometry("pre-encounter-separated");
                    if ((bool)geometry["isAdjacent"])
                    {
                        FailCurrent("CM02-approach-arrival", "Pre-encounter separation remained within the transition envelope.");
                        BeginCleanup(); return;
                    }
                    chunk6aSeparationCommand = null;
                }
                else
                {
                    // Fault compensation needs a fresh native encounter, not an unrelated
                    // long approach. Record the post-Dismount geometry without another Move.
                    observations["chunk6aCompensationSetupGeometry"] =
                        CaptureChunk6aGeometry("compensation-fresh-allocation-geometry");
                }
                chunk6aExplorationStage = 5;
            }
            if (Chunk6aTurnBased)
            {
                if (turnBasedModeProbe == null) turnBasedModeProbe = new NativeModeTransitionProbe(true);
                if (!turnBasedModeProbe.TemporaryValueIsCurrent) { turnBasedModeProbe.DispatchTemporaryValueIfRequired(); return; }
            }
            BeginTarget(6f, "chunk6a-combat-mount");
            ruleProbe.Arm(target, false);
            chunk6aStage = 1;
            ResetLeafClock();
        }
    }
}
