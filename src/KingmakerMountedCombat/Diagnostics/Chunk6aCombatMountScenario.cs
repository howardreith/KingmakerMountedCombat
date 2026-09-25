using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Selection;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Chunk 6A: voluntary combat Mount and Dismount through the normal native
    // KMC ability path, inside a real encounter the pair did not start mounted.
    // Every cost is Kingmaker's own: the scenario reads native cooldowns and
    // native preparation counts and never writes either.
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal const string Chunk6aCombatMountRealTimeScenario = "chunk6a-combat-mount-rt";
        internal const string Chunk6aCombatMountTurnBasedScenario = "chunk6a-combat-mount-tb";

        internal static bool IsChunk6aCombatMountScenario(string scenario) =>
            string.Equals(scenario, Chunk6aCombatMountRealTimeScenario, StringComparison.Ordinal) ||
            string.Equals(scenario, Chunk6aCombatMountTurnBasedScenario, StringComparison.Ordinal);

        private bool IsChunk6aCombatMount => IsChunk6aCombatMountScenario(request.Scenario);

        private bool Chunk6aTurnBased =>
            string.Equals(request.Scenario, Chunk6aCombatMountTurnBasedScenario, StringComparison.Ordinal);

        private int chunk6aStage;
        private int chunk6aDispatchesBefore;
        private int chunk6aRejectionsBefore;
        private long chunk6aGenerationBefore;
        private JObject chunk6aPreMount;
        private JObject chunk6aPreDismount;
        private JObject chunk6aCancelBefore;
        private JObject chunk6aExplorationDismountBefore;
        private JObject chunk6aExplorationDismountAfter;
        private TurnController chunk6aMountTurn;
        private int chunk6aMountRound;
        private bool chunk6aMountClicked;
        private bool chunk6aDismountClicked;
        private bool chunk6aRepeatClicked;
        private MidEncounterAdoption chunk6aDisposition = MidEncounterAdoption.Unavailable;
        private string chunk6aDispositionRefusal;
        private bool chunk6aPreparingObserved;
        private JObject chunk6aCompensationBefore;
        private int chunk6aCompensationDispatchesBefore;
        private long chunk6aCompensationGenerationBefore;
        private long chunk6aCompensationRollbacksBefore;
        private long chunk6aCompensationAdoptionsBefore;
        private bool chunk6aCompensationClicked;
        private IDisposable chunk6aAdoptionFault;
        private readonly JArray chunk6aSamples = new JArray();
        private readonly HashSet<TurnController> chunk6aVisitedTurns = new HashSet<TurnController>();
        private int chunk6aMountTurnsWhileMounted;

        // The diagnostic adoption fault is owned by this scenario and is disarmed
        // on use and on every abort path, so it can never outlive its own row.
        private void Chunk6aDisposeAdoptionFault()
        {
            var fault = chunk6aAdoptionFault;
            chunk6aAdoptionFault = null;
            if (fault != null) { fault.Dispose(); }
        }

        private void BeginChunk6aCombatMount()
        {
            if (!settings.EnablePairedActivation || settings.EnableUnifiedMountedTurn ||
                settings.EnablePairedCommandScheduler || settings.EnableDiagnosticOverlay ||
                playerAction.OverlayPresent)
            {
                throw new InvalidOperationException(
                    "Chunk 6A combat mounting requires the accepted single paired authority and no overlay.");
            }
            CaptureIdleFixturePartyForCleanup();
            allocationTrace = new NativeActorAllocationTrace(rider, horse, combat);
            allocationTrace.BeginEncounter(request.Scenario);
            observations["chunk6aCombatMount"] = chunk6aSamples;
            step = Phase3dHorseStep.Phase3gControls;
            ResetLeafClock();
        }

        private bool Chunk6aIdle => !combat.HasActiveCommand && !combat.HasStockAttackIntent &&
            !combat.HasActiveGroundMovement && rider.Commands.Empty && horse.Commands.Empty &&
            !rider.AreHandsBusyWithAnimation && !horse.AreHandsBusyWithAnimation &&
            !Game.Instance.HandsEquipmentController.IsUpdateScheduledFor(rider) &&
            !Game.Instance.HandsEquipmentController.IsUpdateScheduledFor(horse);

        private JObject Chunk6aCooldowns(UnitEntityData actor)
        {
            var cooldown = actor.CombatState?.Cooldown;
            return new JObject
            {
                ["actor"] = actor.UniqueId,
                ["standard"] = cooldown?.StandardAction,
                ["move"] = cooldown?.MoveAction,
                ["swift"] = cooldown?.SwiftAction,
                ["initiative"] = cooldown?.Initiative,
                ["attackOfOpportunity"] = cooldown?.AttackOfOpportunity,
                ["reactions"] = actor.CombatState?.AttackOfOpportunityCount,
                ["hasMove"] = actor.HasMoveAction(),
                ["hasStandard"] = actor.HasStandardAction(),
                ["prepared"] = actor.CombatState?.Prepared,
                ["nativePrepareCount"] = allocationTrace.GrantCount(actor),
                ["timeToNextTurn"] = actor.GetTimeToNextTurn()
            };
        }

        private JObject CaptureChunk6aState(string kind)
        {
            var controller = Game.Instance.TurnBasedCombatController;
            var turn = controller?.CurrentTurn;
            var order = controller == null ? new List<UnitEntityData>() : controller.SortedUnits.ToList();
            var ledger = playerAction.TransitionLedger;
            var sample = new JObject
            {
                ["kind"] = kind,
                ["frame"] = Time.frameCount,
                ["seconds"] = clock.Elapsed.TotalSeconds,
                ["turnBased"] = CombatController.IsInTurnBasedCombat(),
                ["round"] = controller?.RoundNumber,
                ["currentTurnActor"] = turn?.Unit?.UniqueId,
                ["currentTurnStatus"] = turn?.Status.ToString(),
                ["riderRosterIndex"] = order.IndexOf(rider),
                ["mountRosterIndex"] = order.IndexOf(horse),
                ["relationshipState"] = relationship.State.ToString(),
                ["relationshipGeneration"] = relationship.MountedPairGeneration,
                ["pairedIdentity"] = combat.PairedActivationIdentity,
                ["pairedSequence"] = combat.PairedActivationSequence,
                ["pairedSplit"] = combat.PairedActivationSplit,
                ["pairedFinalized"] = combat.PairedActivationFinalized,
                ["adoptionCount"] = combat.MidEncounterAdoptionCount,
                ["adoptionObservation"] = combat.LastPairedAdoptionObservation,
                ["partnerContextActor"] = combat.PairedPartnerContext?.Unit?.UniqueId,
                ["dispatchAccepted"] = nativeControls.DispatchAcceptedCount,
                ["dispatchRejected"] = nativeControls.DispatchRejectedCount,
                ["relationshipShells"] = nativeControls.NativeRelationshipShellCount,
                ["transitionLedger"] = ledger.Describe(),
                ["acceptedMountCount"] = ledger.AcceptedMountCount,
                ["acceptedDismountCount"] = ledger.AcceptedDismountCount,
                ["forcedDetachCount"] = ledger.ForcedDetachCount,
                ["playerActionFeedback"] = playerAction.LastFeedback,
                ["rider"] = Chunk6aCooldowns(rider),
                ["mount"] = Chunk6aCooldowns(horse),
                ["mountTurnsWhileMounted"] = chunk6aMountTurnsWhileMounted
            };
            chunk6aSamples.Add(sample);
            return sample;
        }

        // The relationship transition itself must leave every other native
        // resource, reaction allowance and initiative value exactly as it was.
        private static bool Chunk6aUnchangedExcept(JObject before, JObject after, params string[] allowedToRise)
        {
            foreach (var name in new[] { "standard", "move", "swift", "initiative", "attackOfOpportunity", "reactions" })
            {
                if (allowedToRise.Contains(name, StringComparer.Ordinal))
                {
                    continue;
                }
                if (!JToken.DeepEquals(before[name], after[name]))
                {
                    return false;
                }
            }
            return true;
        }

        private void TickChunk6aCombatMount()
        {
            var game = Game.Instance;
            var controller = game.TurnBasedCombatController;
            var turn = controller?.CurrentTurn;
            observations["chunk6aProgress"] = new JObject
            {
                ["stage"] = chunk6aStage,
                ["turnBasedRow"] = Chunk6aTurnBased,
                ["mountClicked"] = chunk6aMountClicked,
                ["dismountClicked"] = chunk6aDismountClicked,
                ["disposition"] = chunk6aDisposition.ToString(),
                ["relationshipState"] = relationship.State.ToString(),
                ["turnActor"] = turn?.Unit?.UniqueId,
                ["feedback"] = playerAction.LastFeedback
            };
            if (turn != null && chunk6aVisitedTurns.Add(turn) && turn.Unit == horse &&
                relationship.State == RelationshipState.Mounted && !combat.PairedActivationSplit)
            {
                chunk6aMountTurnsWhileMounted++;
            }
            if (game.IsPaused && chunk6aStage != 2)
            {
                game.IsPaused = false;
                return;
            }

            // Stage 0: the parent flow hands over an adjacent mounted pair out of
            // combat. Dismount through the same normal control first: outside an
            // encounter Kingmaker's own UpdateCooldowns writes nothing, so this
            // also proves the free exploration transition costs nothing.
            if (chunk6aStage == 0)
            {
                if (!Chunk6aIdle)
                {
                    return;
                }
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                if (relationship.State == RelationshipState.Mounted)
                {
                    if (rider.IsInCombat || horse.IsInCombat || Game.Instance.Player.IsInCombat)
                    {
                        FailCurrent("CM01-combat-mount-setup",
                            "Chunk 6A must establish its unmounted baseline before any encounter begins.");
                        BeginCleanup();
                        return;
                    }
                    if (chunk6aExplorationDismountBefore == null)
                    {
                        chunk6aExplorationDismountBefore = CaptureChunk6aState("exploration-dismount-before");
                        chunk6aDispatchesBefore = (int)nativeControls.DispatchAcceptedCount;
                        if (!TryNativeAbilityTargetClick(
                                nativeControls.DismountAbility, rider, "chunk6a-exploration-dismount-click"))
                        {
                            FailCurrent("CM01-exploration-dismount-costs-nothing",
                                "Exact native out-of-combat Dismount target click was not admitted.");
                            BeginCleanup();
                            return;
                        }
                    }
                    return;
                }
                if (relationship.State != RelationshipState.Unmounted)
                {
                    return;
                }
                if (chunk6aExplorationDismountBefore != null && chunk6aExplorationDismountAfter == null)
                {
                    chunk6aExplorationDismountAfter = CaptureChunk6aState("exploration-dismount-after");
                    var riderBefore = (JObject)chunk6aExplorationDismountBefore["rider"];
                    var riderAfter = (JObject)chunk6aExplorationDismountAfter["rider"];
                    AddRow("CM01-exploration-dismount-costs-nothing",
                        nativeControls.DispatchAcceptedCount == chunk6aDispatchesBefore + 1 &&
                            Chunk6aUnchangedExcept(riderBefore, riderAfter) &&
                            Chunk6aUnchangedExcept(
                                (JObject)chunk6aExplorationDismountBefore["mount"],
                                (JObject)chunk6aExplorationDismountAfter["mount"]) &&
                            playerAction.TransitionLedger.AcceptedDismountCount == 1,
                        "The same native Dismount control outside combat performed one transition and charged nothing, because Kingmaker's own UpdateCooldowns writes no resource out of combat.",
                        new JObject
                        {
                            ["before"] = chunk6aExplorationDismountBefore,
                            ["after"] = chunk6aExplorationDismountAfter
                        });
                }
                if (!PrepareUnmountedHorseAiIsolation())
                {
                    return;
                }
                if (Chunk6aTurnBased)
                {
                    if (turnBasedModeProbe == null)
                    {
                        turnBasedModeProbe = new NativeModeTransitionProbe(true);
                    }
                    if (!turnBasedModeProbe.TemporaryValueIsCurrent)
                    {
                        turnBasedModeProbe.DispatchTemporaryValueIfRequired();
                        return;
                    }
                }
                BeginTarget(6f, "chunk6a-combat-mount");
                ruleProbe.Arm(target, false);
                chunk6aStage = 1;
                ResetLeafClock();
                return;
            }

            // Stage 1: wait for the encounter, then for the exact rider boundary.
            if (chunk6aStage == 1)
            {
                if (!IsCombatReady(false))
                {
                    return;
                }
                if (Chunk6aTurnBased)
                {
                    if (!CombatController.IsInTurnBasedCombat())
                    {
                        return;
                    }
                    // R3 proof: while the rider's own turn is still Preparing, the
                    // transition must be refused, and refused with the reason that
                    // names the preparing boundary rather than a generic one. The
                    // native turn reaches Acting on its own, so this observation is
                    // taken in passing and nothing is forced.
                    if (turn?.Unit == rider && turn.Status == TurnController.TurnStatus.Preparing &&
                        !chunk6aPreparingObserved)
                    {
                        chunk6aPreparingObserved = true;
                        var preparingBefore = CaptureChunk6aState("mount-preparing-before");
                        var preparing = nativeControls.Evaluate(NativeMountedControlKind.MountCompanion, rider);
                        var preparingAfter = CaptureChunk6aState("mount-preparing-after");
                        AddRow("CM01-combat-mount-preparing-refused",
                            !preparing.IsEnabled &&
                                preparing.Reason != null && preparing.Reason.Contains("finished preparing") &&
                                relationship.State == RelationshipState.Unmounted &&
                                Chunk6aUnchangedExcept((JObject)preparingBefore["rider"], (JObject)preparingAfter["rider"]) &&
                                Chunk6aUnchangedExcept((JObject)preparingBefore["mount"], (JObject)preparingAfter["mount"]),
                            "While the rider's own native turn was still Preparing, combat Mount was refused with the reason that names the preparing boundary, and sampling it charged nothing.",
                            new JObject
                            {
                                ["enabled"] = preparing.IsEnabled,
                                ["visible"] = preparing.IsVisible,
                                ["reason"] = preparing.Reason,
                                ["turnStatus"] = turn.Status.ToString(),
                                ["before"] = preparingBefore,
                                ["after"] = preparingAfter
                            });
                        return;
                    }
                    if (turn?.Unit != rider || !turn.IsActing)
                    {
                        TryEndPhase3gFixtureTurn(turn);
                        return;
                    }
                }
                if (!Chunk6aIdle)
                {
                    return;
                }
                var availability = nativeControls.Evaluate(NativeMountedControlKind.MountCompanion, rider);
                if (!availability.IsEnabled)
                {
                    observations["chunk6aMountAvailability"] = new JObject
                    {
                        ["visible"] = availability.IsVisible,
                        ["enabled"] = availability.IsEnabled,
                        ["reason"] = availability.Reason,
                        ["state"] = CaptureChunk6aState("mount-availability-pending")
                    };
                    return;
                }
                chunk6aMountTurn = turn;
                chunk6aMountRound = controller?.RoundNumber ?? -1;
                string dispositionRefusal;
                chunk6aDisposition = combat.ResolveMidEncounterAdoption(rider, horse, out dispositionRefusal);
                chunk6aDispositionRefusal = dispositionRefusal;

                // CM02 negative control: exact native target selection started and
                // cancelled before commitment must change nothing at all.
                chunk6aCancelBefore = CaptureChunk6aState("mount-cancel-before");
                var handler = game.SelectedAbilityHandler;
                var data = rider.Descriptor.Abilities.GetAbility(nativeControls.MountAbility)?.Data;
                if (handler == null || data == null)
                {
                    FailCurrent("CM01-combat-mount-cancel-costs-nothing",
                        "Exact native Mount fact or selected-ability handler was unavailable in combat.");
                    BeginCleanup();
                    return;
                }
                handler.SetAbility(data);
                handler.DropAbility();
                var cancelAfter = CaptureChunk6aState("mount-cancel-after");
                AddRow("CM01-combat-mount-cancel-costs-nothing",
                    relationship.State == RelationshipState.Unmounted &&
                        (long)chunk6aCancelBefore["relationshipGeneration"] == (long)cancelAfter["relationshipGeneration"] &&
                        (long)chunk6aCancelBefore["dispatchAccepted"] == (long)cancelAfter["dispatchAccepted"] &&
                        Chunk6aUnchangedExcept((JObject)chunk6aCancelBefore["rider"], (JObject)cancelAfter["rider"]) &&
                        Chunk6aUnchangedExcept((JObject)chunk6aCancelBefore["mount"], (JObject)cancelAfter["mount"]),
                    "Starting and cancelling exact native combat Mount target selection performed no transition and charged nothing.",
                    new JObject { ["before"] = chunk6aCancelBefore, ["after"] = cancelAfter });

                chunk6aStage = 11;
                ResetLeafClock();
                return;
            }

            // Stage 11: R2 regression. The adoption plan is invalidated between
            // availability and delivery, so the relationship attachment must be
            // compensated rather than left standing without a paired activation.
            // The invalidation is injected through the bounded diagnostic adoption
            // fault, which makes the commit refuse at its first check exactly as a
            // late invalidation does; nothing else about the path changes and the
            // scenario writes no resource.
            if (chunk6aStage == 11)
            {
                if (!Chunk6aIdle)
                {
                    return;
                }
                chunk6aCompensationBefore = CaptureChunk6aState("mount-compensation-before");
                chunk6aCompensationDispatchesBefore = (int)nativeControls.DispatchAcceptedCount;
                chunk6aCompensationGenerationBefore = relationship.MountedPairGeneration;
                chunk6aCompensationRollbacksBefore = combat.AdoptionRollbackCount;
                chunk6aCompensationAdoptionsBefore = combat.MidEncounterAdoptionCount;
                chunk6aAdoptionFault = combat.ArmMidEncounterAdoptionFault(rider, horse);
                chunk6aCompensationClicked = TryNativeAbilityTargetClick(
                    nativeControls.MountAbility, horse, "chunk6a-combat-mount-compensation-click");
                if (!chunk6aCompensationClicked)
                {
                    FailCurrent("CM02-adoption-plan-invalidated",
                        "The exact native combat Mount click was not admitted for the compensation regression.");
                    Chunk6aDisposeAdoptionFault();
                    BeginCleanup();
                    return;
                }
                chunk6aStage = 12;
                ResetLeafClock();
                return;
            }

            // Stage 12: the compensated transaction's terminal state.
            if (chunk6aStage == 12)
            {
                if (relationship.State == RelationshipState.Mounted)
                {
                    FailCurrent("CM02-adoption-plan-invalidated",
                        "A refused encounter adoption left the relationship mounted: " +
                        relationship.LastAdoptionTransactionObservation);
                    Chunk6aDisposeAdoptionFault();
                    BeginCleanup();
                    return;
                }
                if (combat.AdoptionFaultConsumedCount == 0)
                {
                    // The fault fires inside the adoption commit, which runs only
                    // after the relationship attaches. If the delivery was refused
                    // earlier the commit never ran, so say that exactly instead of
                    // waiting for the leaf deadline.
                    if (Chunk6aIdle &&
                        nativeControls.DispatchAcceptedCount == chunk6aCompensationDispatchesBefore)
                    {
                        FailCurrent("CM02-adoption-plan-invalidated",
                            "The compensation regression's native Mount was refused before the adoption commit, " +
                            "so the compensating path was never exercised: " + playerAction.LastFeedback);
                        Chunk6aDisposeAdoptionFault();
                        BeginCleanup();
                    }
                    return;
                }
                if (!Chunk6aIdle)
                {
                    return;
                }
                Chunk6aDisposeAdoptionFault();
                var compensated = CaptureChunk6aState("mount-compensation-after");
                var riderBefore = (JObject)chunk6aCompensationBefore["rider"];
                var riderAfter = (JObject)compensated["rider"];
                var mountBefore = (JObject)chunk6aCompensationBefore["mount"];
                var mountAfter = (JObject)compensated["mount"];

                // No mounted or activation residue.
                var noResidue = relationship.State == RelationshipState.Unmounted &&
                    relationship.Runtime.NoPreparedPairResidue &&
                    combat.PairedActivationIdentity == null &&
                    combat.PairedPartnerContext == null &&
                    combat.AdoptionRollbackCount == chunk6aCompensationRollbacksBefore + 1 &&
                    combat.MidEncounterAdoptionCount == chunk6aCompensationAdoptionsBefore &&
                    relationship.AdoptionCompensatedMountCount == 1;

                // No duplicate preparation for either actor.
                var noDuplicatePreparation =
                    (int)riderBefore["nativePrepareCount"] == (int)riderAfter["nativePrepareCount"] &&
                    (int)mountBefore["nativePrepareCount"] == (int)mountAfter["nativePrepareCount"];

                // No refund: Kingmaker committed the rider's Move for the native
                // shell, and the compensation must leave it exactly as committed.
                var expectedMove = Chunk6aTurnBased
                    ? (float?)((float)riderBefore["move"] + 3f)
                    : null;
                var moveStillCommitted = Chunk6aTurnBased
                    ? Math.Abs((float)riderAfter["move"] - expectedMove.Value) <= 0.0001f
                    : (float)riderAfter["move"] > 2.5f && (float)riderAfter["move"] <= 3.0001f;

                // Nothing else moved: the mount keeps every resource and the
                // generation advanced exactly once and was never rewound.
                var mountUntouched = Chunk6aUnchangedExcept(mountBefore, mountAfter);
                var riderOtherResourcesHeld = Chunk6aUnchangedExcept(riderBefore, riderAfter, "move");
                var generationAdvancedOnce =
                    relationship.MountedPairGeneration == chunk6aCompensationGenerationBefore + 1;
                var ledgerTruthful = playerAction.TransitionLedger.AcceptedMountCount == 0 &&
                    playerAction.TransitionLedger.AdmittedMountCount == 1 &&
                    !playerAction.HasVoluntaryTransitionInFlight;

                AddRow("CM02-adoption-plan-invalidated",
                    noResidue && noDuplicatePreparation && moveStillCommitted && mountUntouched &&
                        riderOtherResourcesHeld && generationAdvancedOnce && ledgerTruthful,
                    "An encounter adoption refused after the relationship attached was compensated exactly: the relationship returned to unmounted with no activation, partner context or prepared-pair residue, neither actor was prepared again, the rider's already-committed native Move was not refunded, the mount kept every resource, and the relationship generation advanced once and was never rewound.",
                    new JObject
                    {
                        ["before"] = chunk6aCompensationBefore,
                        ["after"] = compensated,
                        ["noResidue"] = noResidue,
                        ["noDuplicatePreparation"] = noDuplicatePreparation,
                        ["moveStillCommitted"] = moveStillCommitted,
                        ["expectedRiderMove"] = expectedMove,
                        ["mountUntouched"] = mountUntouched,
                        ["riderOtherResourcesHeld"] = riderOtherResourcesHeld,
                        ["generationAdvancedOnce"] = generationAdvancedOnce,
                        ["ledgerTruthful"] = ledgerTruthful,
                        ["adoptionFaultConsumed"] = combat.AdoptionFaultConsumedCount,
                        ["adoptionRollbacks"] = combat.AdoptionRollbackCount,
                        ["compensatedMounts"] = relationship.AdoptionCompensatedMountCount,
                        ["planObservation"] = combat.LastAdoptionPlanObservation,
                        ["adoptionObservation"] = combat.LastPairedAdoptionObservation,
                        ["transactionObservation"] = relationship.LastAdoptionTransactionObservation,
                        ["ledger"] = playerAction.TransitionLedger.Describe(),
                        ["allocationTrace"] = allocationTrace.Capture()
                    });

                // A compensated attachment must not lock the pair out of a later
                // lawful transition, which the rest of this scenario then performs.
                AddRow("CM02-adoption-compensation-releases",
                    relationship.State == RelationshipState.Unmounted &&
                        !playerAction.HasVoluntaryTransitionInFlight &&
                        combat.PairedActivationIdentity == null,
                    "A compensated combat Mount left the pair free to attempt a later lawful transition.",
                    new JObject
                    {
                        ["state"] = relationship.State.ToString(),
                        ["inFlight"] = playerAction.HasVoluntaryTransitionInFlight,
                        ["activation"] = combat.PairedActivationIdentity
                    });
                chunk6aStage = 13;
                ResetLeafClock();
                return;
            }

            // Stage 13: the real transition, on the turn the pair still owns.
            if (chunk6aStage == 13)
            {
                if (!Chunk6aIdle)
                {
                    return;
                }
                if (Chunk6aTurnBased && (turn?.Unit != rider || !turn.IsActing))
                {
                    FailCurrent("CM01-combat-mount-accepted",
                        "The rider's acting turn was lost during the compensation regression.");
                    BeginCleanup();
                    return;
                }
                chunk6aPreMount = CaptureChunk6aState("mount-before");
                chunk6aDispatchesBefore = (int)nativeControls.DispatchAcceptedCount;
                chunk6aRejectionsBefore = (int)nativeControls.DispatchRejectedCount;
                chunk6aGenerationBefore = relationship.MountedPairGeneration;
                observations["chunk6aAdoptionDisposition"] = new JObject
                {
                    ["disposition"] = chunk6aDisposition.ToString(),
                    ["refusal"] = chunk6aDispositionRefusal,
                    ["riderRosterIndex"] = chunk6aPreMount["riderRosterIndex"],
                    ["mountRosterIndex"] = chunk6aPreMount["mountRosterIndex"]
                };
                chunk6aMountClicked = TryNativeAbilityTargetClick(
                    nativeControls.MountAbility, horse, "chunk6a-combat-mount-click");
                if (!chunk6aMountClicked)
                {
                    FailCurrent("CM01-combat-mount-accepted",
                        "Exact native combat Mount target click was not admitted.");
                    BeginCleanup();
                    return;
                }
                chunk6aStage = 2;
                ResetLeafClock();
                return;
            }

            // Stage 2: await the native command's own terminal state and delivery.
            if (chunk6aStage == 2)
            {
                if (relationship.State == RelationshipState.Unmounted && Chunk6aIdle &&
                    nativeControls.DispatchAcceptedCount == chunk6aDispatchesBefore &&
                    nativeControls.DispatchRejectedCount > chunk6aRejectionsBefore)
                {
                    FailCurrent("CM01-combat-mount-accepted",
                        "The admitted native combat Mount shell refused its own delivery: " + playerAction.LastFeedback);
                    BeginCleanup();
                    return;
                }
                if (relationship.State != RelationshipState.Mounted || !Chunk6aIdle)
                {
                    return;
                }
                var after = CaptureChunk6aState("mount-after");
                var riderBefore = (JObject)chunk6aPreMount["rider"];
                var riderAfter = (JObject)after["rider"];
                var mountBefore = (JObject)chunk6aPreMount["mount"];
                var mountAfter = (JObject)after["mount"];
                var expectedMove = Chunk6aTurnBased
                    ? (float?)((float)riderBefore["move"] + 3f)
                    : null;
                var moveCommitted = Chunk6aTurnBased
                    ? Math.Abs((float)riderAfter["move"] - expectedMove.Value) <= 0.0001f
                    : (float)riderAfter["move"] > 2.5f && (float)riderAfter["move"] <= 3.0001f;
                var exactPair = relationship.Rider == rider && relationship.Mount == horse;
                var oneDelivery = nativeControls.DispatchAcceptedCount == chunk6aDispatchesBefore + 1;
                var oneTransition = relationship.MountedPairGeneration == chunk6aGenerationBefore + 1 &&
                    playerAction.TransitionLedger.AcceptedMountCount == 1 &&
                    playerAction.TransitionLedger.ForcedDetachCount == 0;
                AddRow("CM01-combat-mount-accepted",
                    exactPair && oneDelivery && oneTransition && moveCommitted &&
                        relationship.Runtime.PoseConfigured && relationship.Runtime.PoseHealthy,
                    "One exact native combat Mount delivery produced one relationship transition and committed the rider's native Move exactly once.",
                    new JObject
                    {
                        ["before"] = chunk6aPreMount,
                        ["after"] = after,
                        ["expectedRiderMove"] = expectedMove,
                        ["oneDelivery"] = oneDelivery,
                        ["oneTransition"] = oneTransition,
                        ["moveCommitted"] = moveCommitted,
                        ["turnBased"] = Chunk6aTurnBased
                    });

                // The transition itself must not touch any other resource, and it
                // must not repeat a native preparation for either actor.
                var riderOtherResourcesHeld = Chunk6aUnchangedExcept(riderBefore, riderAfter, "move");
                var mountResourcesHeld = Chunk6aUnchangedExcept(mountBefore, mountAfter);
                var riderPrepareUnchanged =
                    (int)riderBefore["nativePrepareCount"] == (int)riderAfter["nativePrepareCount"];
                var expectedMountPrepareDelta =
                    chunk6aDisposition == MidEncounterAdoption.PreparePartnerThisRound ? 1 : 0;
                var mountPrepareDelta =
                    (int)mountAfter["nativePrepareCount"] - (int)mountBefore["nativePrepareCount"];
                AddRow("CM03-combat-mount-conserves-debt",
                    riderOtherResourcesHeld && mountResourcesHeld,
                    "The combat Mount transition charged only the rider's native Move and left every other rider and mount resource, reaction allowance and initiative value exactly as it was.",
                    new JObject
                    {
                        ["riderOtherResourcesHeld"] = riderOtherResourcesHeld,
                        ["mountResourcesHeld"] = mountResourcesHeld,
                        ["riderBefore"] = riderBefore, ["riderAfter"] = riderAfter,
                        ["mountBefore"] = mountBefore, ["mountAfter"] = mountAfter
                    });
                AddRow("CM03-combat-mount-adoption-preparations",
                    riderPrepareUnchanged && mountPrepareDelta == expectedMountPrepareDelta &&
                        combat.MidEncounterAdoptionCount == 1 &&
                        (!Chunk6aTurnBased || combat.PairedActivationSequence == 1) &&
                        (Chunk6aTurnBased || combat.PairedActivationSequence == 0),
                    "Mid-encounter adoption repeated no native preparation for the principal and performed exactly the partner preparation its disposition requires.",
                    new JObject
                    {
                        ["disposition"] = chunk6aDisposition.ToString(),
                        ["riderNativePrepareBefore"] = riderBefore["nativePrepareCount"],
                        ["riderNativePrepareAfter"] = riderAfter["nativePrepareCount"],
                        ["mountNativePrepareDelta"] = mountPrepareDelta,
                        ["expectedMountPrepareDelta"] = expectedMountPrepareDelta,
                        ["adoptionCount"] = combat.MidEncounterAdoptionCount,
                        ["pairedSequence"] = combat.PairedActivationSequence,
                        ["adoptionObservation"] = combat.LastPairedAdoptionObservation,
                        ["allocationTrace"] = allocationTrace.Capture()
                    });
                chunk6aStage = 3;
                ResetLeafClock();
                return;
            }

            // Stage 3: a repeated request while mounted is refused with an exact
            // reason and neither transitions nor charges again.
            if (chunk6aStage == 3)
            {
                if (!Chunk6aIdle)
                {
                    return;
                }
                var repeatBefore = CaptureChunk6aState("mount-repeat-before");
                chunk6aRepeatClicked = TryNativeAbilityTargetClick(
                    nativeControls.MountAbility, horse, "chunk6a-combat-mount-repeat-click");
                var repeatAfter = CaptureChunk6aState("mount-repeat-after");
                AddRow("CM06-combat-mount-repeat-refused",
                    !chunk6aRepeatClicked &&
                        relationship.State == RelationshipState.Mounted &&
                        (long)repeatBefore["relationshipGeneration"] == (long)repeatAfter["relationshipGeneration"] &&
                        (long)repeatBefore["dispatchAccepted"] == (long)repeatAfter["dispatchAccepted"] &&
                        playerAction.TransitionLedger.AcceptedMountCount == 1 &&
                        Chunk6aUnchangedExcept((JObject)repeatBefore["rider"], (JObject)repeatAfter["rider"]) &&
                        Chunk6aUnchangedExcept((JObject)repeatBefore["mount"], (JObject)repeatAfter["mount"]),
                    "A repeated native Mount request while already mounted was refused with an exact reason and produced no second transition and no second charge.",
                    new JObject
                    {
                        ["clicked"] = chunk6aRepeatClicked,
                        ["reason"] = nativeControls.Evaluate(NativeMountedControlKind.MountCompanion, rider).Reason,
                        ["before"] = repeatBefore, ["after"] = repeatAfter
                    });
                chunk6aStage = 4;
                ResetLeafClock();
                return;
            }

            // Stage 4: voluntary combat Dismount through the same normal control.
            if (chunk6aStage == 4)
            {
                if (!Chunk6aIdle)
                {
                    return;
                }
                if (Chunk6aTurnBased &&
                    (turn?.Unit != rider || turn.Status != TurnController.TurnStatus.Preparing && !turn.IsActing))
                {
                    TryEndPhase3gFixtureTurn(turn);
                    return;
                }
                var dismountAvailability = nativeControls.Evaluate(NativeMountedControlKind.Dismount, rider);
                if (!dismountAvailability.IsEnabled)
                {
                    observations["chunk6aDismountAvailability"] = new JObject
                    {
                        ["visible"] = dismountAvailability.IsVisible,
                        ["enabled"] = dismountAvailability.IsEnabled,
                        ["reason"] = dismountAvailability.Reason,
                        ["state"] = CaptureChunk6aState("dismount-availability-pending")
                    };
                    return;
                }
                chunk6aPreDismount = CaptureChunk6aState("dismount-before");
                chunk6aDispatchesBefore = (int)nativeControls.DispatchAcceptedCount;
                chunk6aDismountClicked = TryNativeAbilityTargetClick(
                    nativeControls.DismountAbility, rider, "chunk6a-combat-dismount-click");
                if (!chunk6aDismountClicked)
                {
                    FailCurrent("CM05-combat-dismount-accepted",
                        "Exact native combat Dismount target click was not admitted.");
                    BeginCleanup();
                    return;
                }
                chunk6aStage = 5;
                ResetLeafClock();
                return;
            }

            // Stage 5: the Dismount's own terminal state, then its accounting.
            if (chunk6aStage == 5)
            {
                if (relationship.State != RelationshipState.Unmounted || !Chunk6aIdle)
                {
                    return;
                }
                var after = CaptureChunk6aState("dismount-after");
                var riderBefore = (JObject)chunk6aPreDismount["rider"];
                var riderAfter = (JObject)after["rider"];
                var mountBefore = (JObject)chunk6aPreDismount["mount"];
                var mountAfter = (JObject)after["mount"];
                var expectedMove = Chunk6aTurnBased ? (float?)((float)riderBefore["move"] + 3f) : null;
                var moveCommitted = Chunk6aTurnBased
                    ? Math.Abs((float)riderAfter["move"] - expectedMove.Value) <= 0.0001f
                    : (float)riderAfter["move"] > 2.5f && (float)riderAfter["move"] <= 3.0001f;
                var oneDelivery = nativeControls.DispatchAcceptedCount == chunk6aDispatchesBefore + 1;
                AddRow("CM05-combat-dismount-accepted",
                    oneDelivery && moveCommitted &&
                        playerAction.TransitionLedger.AcceptedDismountCount == 1 &&
                        playerAction.TransitionLedger.ForcedDetachCount == 0 &&
                        rider.IsInState && horse.IsInState &&
                        rider.Descriptor.State.IsConscious && horse.Descriptor.State.IsConscious,
                    "One exact native voluntary combat Dismount delivery committed the rider's native Move exactly once and left two valid separate actors.",
                    new JObject
                    {
                        ["before"] = chunk6aPreDismount, ["after"] = after,
                        ["expectedRiderMove"] = expectedMove,
                        ["oneDelivery"] = oneDelivery, ["moveCommitted"] = moveCommitted,
                        ["voluntaryOnly"] = playerAction.TransitionLedger.Describe()
                    });
                // Existing debt may only ever rise; nothing is refunded or refreshed.
                var riderHeld = (float)riderAfter["standard"] >= (float)riderBefore["standard"] &&
                    (float)riderAfter["move"] >= (float)riderBefore["move"] &&
                    (float)riderAfter["swift"] >= (float)riderBefore["swift"] &&
                    JToken.DeepEquals(riderBefore["initiative"], riderAfter["initiative"]);
                var mountHeld = (float)mountAfter["standard"] >= (float)mountBefore["standard"] &&
                    (float)mountAfter["move"] >= (float)mountBefore["move"] &&
                    (float)mountAfter["swift"] >= (float)mountBefore["swift"] &&
                    JToken.DeepEquals(mountBefore["initiative"], mountAfter["initiative"]);
                AddRow("CM05-combat-dismount-conserves-debt",
                    riderHeld && mountHeld &&
                        (int)riderBefore["nativePrepareCount"] == (int)riderAfter["nativePrepareCount"] &&
                        (int)mountBefore["nativePrepareCount"] == (int)mountAfter["nativePrepareCount"],
                    "Voluntary combat Dismount refunded and refreshed nothing and repeated no native preparation for either actor.",
                    new JObject
                    {
                        ["riderHeld"] = riderHeld, ["mountHeld"] = mountHeld,
                        ["riderBefore"] = riderBefore, ["riderAfter"] = riderAfter,
                        ["mountBefore"] = mountBefore, ["mountAfter"] = mountAfter
                    });
                AddRow("CM05-no-duplicate-mount-turn",
                    chunk6aMountTurnsWhileMounted == 0 &&
                        (!Chunk6aTurnBased || combat.PairedActivationSplit),
                    "The mount took no independent native turn while the pair was mounted, and voluntary Dismount split the activation rather than creating one.",
                    new JObject
                    {
                        ["mountTurnsWhileMounted"] = chunk6aMountTurnsWhileMounted,
                        ["pairedSplit"] = combat.PairedActivationSplit,
                        ["mountRound"] = chunk6aMountRound,
                        ["round"] = controller?.RoundNumber,
                        ["visitedTurns"] = chunk6aVisitedTurns.Count
                    });
                chunk6aStage = 6;
                ResetLeafClock();
                BeginCleanup();
                return;
            }
        }
    }
}
