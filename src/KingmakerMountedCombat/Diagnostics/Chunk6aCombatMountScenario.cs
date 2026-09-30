using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json;
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

        // The narrow real-time non-adjacent native-approach scenario. It shares every
        // Chunk 6A stage up to and including CM02-approach-arrival and then stops, so a
        // failure in the approach itself is attributable without paying for the whole
        // suite. It emits the same tranche evidence kind, so it introduces no new leaf.
        internal const string Chunk6aMountApproachScenario = "chunk6a-mount-approach";
        internal const string Chunk6aObstructionScenario = "chunk6a-obstruction";
        internal const string Chunk6aGeometryChangeScenario = "chunk6a-geometry-change";
        internal const string Chunk6aCompensationRealTimeScenario = "chunk6a-adoption-compensation-rt";
        internal const string Chunk6aCompensationTurnBasedScenario = "chunk6a-adoption-compensation-tb";

        internal static bool IsChunk6aCombatMountScenario(string scenario) =>
            IsChunk6aAutoUseScenario(scenario) || IsChunk6aDismountEscapeScenario(scenario) || IsChunk6aMountOrderScenario(scenario) ||
            IsChunk6aRefusedScenario(scenario) ||
            string.Equals(scenario, Chunk6aCombatMountRealTimeScenario, StringComparison.Ordinal) ||
            string.Equals(scenario, Chunk6aCombatMountTurnBasedScenario, StringComparison.Ordinal) ||
            Chunk6aMammothScenarioEngine.SupportsScenario(scenario) ||
            string.Equals(scenario, Chunk6aHotbarScenario, StringComparison.Ordinal) ||
            string.Equals(scenario, Chunk6aMountApproachScenario, StringComparison.Ordinal) ||
            string.Equals(scenario, Chunk6aStopApproachScenario, StringComparison.Ordinal) ||
            string.Equals(scenario, Chunk6aReplacementApproachScenario, StringComparison.Ordinal) ||
            string.Equals(scenario, Chunk6aRepeatedRequestScenario, StringComparison.Ordinal) ||
            string.Equals(scenario, Chunk6aOwnershipChangeScenario, StringComparison.Ordinal) ||
            string.Equals(scenario, Chunk6aSizeFormChangeScenario, StringComparison.Ordinal) ||
            string.Equals(scenario, Chunk6aLostDirectControlScenario, StringComparison.Ordinal) ||
            string.Equals(scenario, Chunk6aRiderIncapacitatedScenario, StringComparison.Ordinal) ||
            string.Equals(scenario, Chunk6aMountIncapacitatedScenario, StringComparison.Ordinal) ||
            scenario == Chunk6aPausedQueueScenario ||
            string.Equals(scenario, Chunk6aObstructionScenario, StringComparison.Ordinal) ||
            string.Equals(scenario, Chunk6aGeometryChangeScenario, StringComparison.Ordinal) ||
            string.Equals(scenario, Chunk6aCompensationRealTimeScenario, StringComparison.Ordinal) ||
            string.Equals(scenario, Chunk6aCompensationTurnBasedScenario, StringComparison.Ordinal);

        private bool IsChunk6aCombatMount => IsChunk6aCombatMountScenario(request.Scenario);

        private bool Chunk6aTurnBased => Chunk6aMountOrderOnly ||
            request.Scenario == Chunk6aMammothScenarioEngine.TurnBasedScenario ||
            string.Equals(request.Scenario, Chunk6aCombatMountTurnBasedScenario, StringComparison.Ordinal) ||
            string.Equals(request.Scenario, Chunk6aCompensationTurnBasedScenario, StringComparison.Ordinal);

        private bool Chunk6aCompensationOnly =>
            string.Equals(request.Scenario, Chunk6aCompensationRealTimeScenario, StringComparison.Ordinal) ||
            string.Equals(request.Scenario, Chunk6aCompensationTurnBasedScenario, StringComparison.Ordinal);

        private bool Chunk6aApproachOnly =>
            Chunk6aMammothScenarioEngine.SupportsScenario(request.Scenario) ||
            Chunk6aHotbarOnly || string.Equals(request.Scenario, Chunk6aMountApproachScenario, StringComparison.Ordinal) || Chunk6aPausedQueueOnly;

        private bool Chunk6aObstructionOnly => string.Equals(request.Scenario, Chunk6aObstructionScenario, StringComparison.Ordinal);

        private bool Chunk6aGeometryOnly => string.Equals(request.Scenario, Chunk6aGeometryChangeScenario, StringComparison.Ordinal);

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
        // The observed wait for Kingmaker to restore the native Move its earlier refused
        // delivery kept. Published so the evidence shows the resource came back on the
        // engine's own clock rather than from anything this scenario did.
        private JObject chunk6aMoveRestorationWait;

        // Positive/geometry separation uses ordinary native Horse ground input.
        // The real obstruction fixture is owned by Chunk6aDoorFixture.
        private Vector3 chunk6aSeparationDestination;
        private UnitMoveTo chunk6aSeparationCommand;
        private JObject chunk6aGeometryChangeBefore;
        private JObject chunk6aGeometryChangeLedgerBefore;
        private JObject chunk6aGeometryChangeStart;
        private JObject chunk6aGeometryChangeAtChange;
        private Vector3 chunk6aGeometryChangeDestination;
        private UnitMoveTo chunk6aGeometryChangeCommand;
        private bool chunk6aGeometryChanged;
        private JObject chunk6aEncounterLiveness;
        private bool chunk6aPreparingObserved;
        // Bounded geometry and command evidence, one sample per named boundary.
        private readonly JArray chunk6aGeometry = new JArray();
        private JObject chunk6aApproachStart;
        private JObject chunk6aApproachClosed;
        private JObject chunk6aExplorationLedgerBefore;
        private JObject chunk6aMountLedgerBefore;
        private JObject chunk6aCompensationLedgerBefore;
        private JObject chunk6aRepeatLedgerBefore;
        private JObject chunk6aDismountLedgerBefore;

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
            if (request.Scenario == Chunk6aCombatMountRealTimeScenario || request.Scenario == Chunk6aCombatMountTurnBasedScenario)
                observations["chunk6aFullTransactionContract"] = "one-positive-mount-and-dismount-with-separate-campaign-support";
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
            var currentTurn = Game.Instance?.TurnBasedCombatController?.CurrentTurn;
            var nativeTurn = currentTurn?.Unit == actor ? currentTurn : combat.PairedPartnerContext?.Unit == actor ? combat.PairedPartnerContext : null;
            return new JObject
            {
                ["actor"] = actor.UniqueId,
                ["standard"] = cooldown?.StandardAction,
                ["move"] = cooldown?.MoveAction,
                ["swift"] = cooldown?.SwiftAction,
                ["initiative"] = cooldown?.Initiative, // legacy cooldown alias; ordering is separate below
                ["initiativeCooldown"] = cooldown?.Initiative,
                ["initiativeOrder"] = actor.CombatState?.Initiative,
                ["reactionCooldown"] = cooldown?.AttackOfOpportunity,
                ["reactionsPerRound"] = actor.CombatState?.AttackOfOpportunityPerRound,
                ["attackOfOpportunity"] = cooldown?.AttackOfOpportunity,
                ["reactions"] = actor.CombatState?.AttackOfOpportunityCount,
                ["hasMove"] = actor.HasMoveAction(),
                ["hasStandard"] = actor.HasStandardAction(),
                ["prepared"] = actor.CombatState?.Prepared,
                ["nativePrepareCount"] = allocationTrace.GrantCount(actor),
                ["nativeTurnObject"] = nativeTurn == null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(nativeTurn),
                ["timeMoved"] = nativeTurn?.TimeMoved,
                ["timeForced"] = nativeTurn?.TimeMovedInForceMode,
                ["timeStepped"] = nativeTurn?.TimeMovedByFiveFootStep,
                ["timeToNextTurn"] = actor.GetTimeToNextTurn()
            };
        }

        // The transition ledger as counters, so a window can be measured as a DELTA.
        // Absolute counts include the separate exploration Mount and Dismount windows.
        // Positive and compensation requests belong to separate fresh scenarios;
        // neither may borrow the other request's counters. Deltas pin one exact window.
        private JObject Chunk6aLedgerCounters()
        {
            var ledger = playerAction.TransitionLedger;
            return new JObject
            {
                ["admittedMount"] = ledger.AdmittedMountCount,
                ["acceptedMount"] = ledger.AcceptedMountCount,
                ["admittedDismount"] = ledger.AdmittedDismountCount,
                ["acceptedDismount"] = ledger.AcceptedDismountCount,
                ["refusedVoluntary"] = ledger.RefusedVoluntaryCount,
                ["forcedDetach"] = ledger.ForcedDetachCount,
                ["duplicateSuppressed"] = ledger.DuplicateControlSuppressedCount,
                ["concurrentSuppressed"] = ledger.ConcurrentControlSuppressedCount
            };
        }

        private bool Chunk6aLedgerDelta(JObject before, string name, long expected)
        {
            if (before == null || before[name] == null) { return false; }
            return (long)Chunk6aLedgerCounters()[name] - (long)before[name] == expected;
        }

        // Out of combat every action resource, the reaction allowance and initiative must
        // read zero: Kingmaker charges nothing there, which is precisely the claim.
        private static bool Chunk6aExplorationResourcesClear(JObject actor)
        {
            if (actor == null) { return false; }
            foreach (var name in new[] { "standard", "move", "swift", "initiative", "attackOfOpportunity" })
            {
                var value = actor[name];
                if (value == null || Math.Abs((float)value) > 0.0001f) { return false; }
            }
            return true;
        }

        // A Horse that walks into reach would satisfy adjacency while proving nothing about
        // rider approach, so the Horse must be effectively stationary for
        // CM02-approach-arrival to count. This is a measurement tolerance for idle drift and
        // animation settle, not a licence to move it.
        private const float Chunk6aStationaryToleranceMeters = 0.35f;

        // The diagnostic encounter stays alive only while the bidirectional combat-memory
        // lease keeps refreshing, and a run lost combat between the combat Mount and the
        // combat Dismount with nothing recording why: the Dismount then ran out of combat,
        // where Kingmaker charges nothing, and the row failed on a derived clause instead of
        // the real cause. This records the encounter's liveness each frame, bounded to the
        // first lapse of each kind plus running counts, so the next run names the exact lease
        // that stopped validating rather than leaving it to be guessed at.
        private void ObserveChunk6aEncounterLiveness(bool? combatMemoryRefreshed)
        {
            // Cleanup has already archived and released allocationTrace. Its intentional
            // encounter teardown is outside the measured command windows.
            if (cleanupStarted) { return; }
            var live = rider != null && horse != null &&
                rider.IsInCombat && horse.IsInCombat && Game.Instance.Player.IsInCombat;
            if (chunk6aEncounterLiveness == null)
            {
                chunk6aEncounterLiveness = new JObject
                {
                    ["refreshObservedCount"] = 0,
                    ["refreshFailedCount"] = 0,
                    ["firstRefreshFailure"] = null,
                    ["firstCombatLoss"] = null,
                    ["lastLiveFrame"] = -1,
                    ["lastLiveSeconds"] = -1d
                };
                observations["chunk6aEncounterLiveness"] = chunk6aEncounterLiveness;
            }
            if (combatMemoryRefreshed.HasValue)
            {
                chunk6aEncounterLiveness["refreshObservedCount"] =
                    (int)chunk6aEncounterLiveness["refreshObservedCount"] + 1;
                if (!combatMemoryRefreshed.Value)
                {
                    chunk6aEncounterLiveness["refreshFailedCount"] =
                        (int)chunk6aEncounterLiveness["refreshFailedCount"] + 1;
                    if (chunk6aEncounterLiveness["firstRefreshFailure"] == null ||
                        chunk6aEncounterLiveness["firstRefreshFailure"].Type == JTokenType.Null)
                    {
                        chunk6aEncounterLiveness["firstRefreshFailure"] = Chunk6aLivenessMark(live);
                    }
                }
            }
            if (live)
            {
                chunk6aEncounterLiveness["lastLiveFrame"] = Time.frameCount;
                chunk6aEncounterLiveness["lastLiveSeconds"] = clock.Elapsed.TotalSeconds;
                return;
            }
            if (chunk6aEncounterLiveness["firstCombatLoss"] == null ||
                chunk6aEncounterLiveness["firstCombatLoss"].Type == JTokenType.Null)
            {
                chunk6aEncounterLiveness["firstCombatLoss"] = Chunk6aLivenessMark(false);
            }
        }

        private JObject Chunk6aLivenessMark(bool live)
        {
            return new JObject
            {
                ["frame"] = Time.frameCount,
                ["seconds"] = clock.Elapsed.TotalSeconds,
                ["stage"] = chunk6aStage,
                ["encounterLive"] = live,
                ["riderInCombat"] = rider != null && rider.IsInCombat,
                ["horseInCombat"] = horse != null && horse.IsInCombat,
                ["playerInCombat"] = Game.Instance.Player.IsInCombat,
                ["targetPresent"] = target != null,
                ["targetInState"] = target != null && target.IsInState,
                ["targetConscious"] = target != null && target.Descriptor != null &&
                    target.Descriptor.State.IsConscious,
                ["targetHitPoints"] = target?.HPLeft,
                ["targetState"] = targetService == null ? null : targetService.State.ToString(),
                ["targetBrainLeaseReleased"] = targetService?.TargetBrainLeaseReleased,
                ["targetSleeplessLeaseReleased"] = targetService?.TargetSleeplessLeaseReleased,
                ["targetDurabilityLeaseReleased"] = targetService?.TargetDurabilityLeaseReleased,
                ["relationshipState"] = relationship.State.ToString(),
                ["riderResources"] = Chunk6aCooldowns(rider)
            };
        }

        // Send the Horse AWAY from the rider through Kingmaker's own ground-click input, so
        // an approach case can start from measured non-adjacency. FindWalkablePointNearTarget
        // picks a walkable node at the requested distance from the RIDER along the
        // rider-to-Horse direction, so asking for more than the current separation can only
        // widen the gap. This creates a normal player-owned Horse Move command and writes no
        // transform; it is the same native input every other ground-movement case uses.
        // A bounded walkable point may genuinely not exist in this fixture's geometry, and
        // The bounded route/footprint selector says so by throwing. That is an exact, reportable
        // obstacle rather than a reason to abort the tick opaquely, so the refusal is
        // returned as a message the caller turns into a named row failure.
        private UnitMoveTo Chunk6aSendHorseAway(
            float extraMeters, out Vector3 destination, out string refusal)
        {
            destination = horse?.Position ?? Vector3.zero;
            refusal = null;
            try
            {
                SelectionManager.Instance.SelectUnit(horse.View, true, true, false);
                destination = FindChunk6aGroundDestination(extraMeters);
            }
            catch (Exception exception)
            {
                refusal = exception.GetType().Name + ": " + exception.Message;
                return null;
            }
            ClickGroundHandler.MoveSelectedUnitsToPoint(destination, false);
            var order = horse.Commands.Move as UnitMoveTo;
            // Hand the selection straight back to the rider. Ground input acts on the current
            // selection, so the Horse has to be selected to receive the order -- but combat
            // Mount availability and target admission both require the exact rider to be the
            // single selected unit, and a run left the selection on the Horse and then spun
            // to the leaf deadline while availability reported every selection-derived
            // refusal it has. The selection is restored here, not at the later click, so no
            // caller can observe availability through the wrong actor.
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            return order;
        }

        // The largest rider Move cooldown any sample recorded at or after a frame, with the
        // moment it was seen. A real-time charge RAISES the cooldown to roughly one Move, so
        // this is how a genuine native commitment is observed to have been taken -- and,
        // paired with the elapsed clock, how it is observed to have been KEPT rather than
        // refunded.
        private JObject Chunk6aPeakRiderMoveCooldownSince(int frame)
        {
            var peak = new JObject { ["cooldown"] = 0f, ["seconds"] = 0d, ["frame"] = frame };
            foreach (var sample in chunk6aGeometry.OfType<JObject>())
            {
                if ((int)sample["frame"] < frame) { continue; }
                var resources = sample["riderResources"] as JObject;
                if (resources == null || resources["move"] == null) { continue; }
                var value = (float)resources["move"];
                if (value <= (float)peak["cooldown"]) { continue; }
                peak["cooldown"] = value;
                peak["seconds"] = sample["seconds"];
                peak["frame"] = sample["frame"];
            }
            return peak;
        }

        // A real-time Move cooldown drains with the clock and nothing else may touch it, so
        // a later reading must be at least the observed charge minus the seconds that have
        // passed since. A refund -- a write that cleared or credited the resource -- shows up
        // as a reading below that floor. The tolerance covers frame pacing only.
        private static bool Chunk6aMoveCostRetained(JObject peak, float currentCooldown, double nowSeconds)
        {
            var charged = (float)peak["cooldown"];
            if (charged <= 0f) { return true; }
            var elapsed = nowSeconds - (double)peak["seconds"];
            if (elapsed < 0d) { return false; }
            var floor = charged - (float)elapsed - 0.25f;
            return floor <= 0f || currentCooldown >= floor;
        }

        private static float Chunk6aPlanarDistance(JObject from, JObject to)
        {
            if (from == null || to == null) { return -1f; }
            var dx = (float)to["x"] - (float)from["x"];
            var dz = (float)to["z"] - (float)from["z"];
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        // Bounded geometry and command evidence for one boundary of the approach.
        //
        // CM02-approach-arrival asserts that Kingmaker's own rider approach closed the
        // distance, so the claim is only checkable if the distance and the command that
        // closed it were actually measured on both sides. This records the measurement
        // itself -- positions, the native envelope derived from live corpulence, the
        // IsAdjacent verdict, the exact Move-slot command and its lifecycle flags, and both
        // actors' resource ledgers -- and it only READS. It writes no transform, creates no
        // command, and never moves either actor.
        private JObject CaptureChunk6aGeometry(string boundary)
        {
            var riderPosition = rider?.Position ?? Vector3.zero;
            var horsePosition = horse?.Position ?? Vector3.zero;
            var riderCorpulence = rider?.View == null ? -1f : rider.View.Corpulence;
            var horseCorpulence = horse?.View == null ? -1f : horse.View.Corpulence;
            var centerDistance = rider == null || horse == null ? -1f : rider.DistanceTo(horse);
            var horizontal = new Vector2(riderPosition.x - horsePosition.x, riderPosition.z - horsePosition.z).magnitude;
            var envelope = riderCorpulence < 0f || horseCorpulence < 0f
                ? -1f
                : riderCorpulence + horseCorpulence + CombatMountDismountPolicy.NativeAdjacentReachMeters;
            float approachRadius;
            var approachRadiusResolved = CombatMountDismountPolicy.TryGetMountApproachRadius(
                float.PositiveInfinity, riderCorpulence, horseCorpulence, out approachRadius);
            var slot = rider?.Commands?.GetCommand(UnitCommand.CommandType.Move);
            var slotAbility = slot as UnitUseAbility;
            var sample = new JObject
            {
                ["boundary"] = boundary,
                ["frame"] = Time.frameCount,
                ["seconds"] = clock.Elapsed.TotalSeconds,
                ["riderPosition"] = new JObject { ["x"] = riderPosition.x, ["y"] = riderPosition.y, ["z"] = riderPosition.z },
                ["horsePosition"] = new JObject { ["x"] = horsePosition.x, ["y"] = horsePosition.y, ["z"] = horsePosition.z },
                ["riderCorpulence"] = riderCorpulence,
                ["horseCorpulence"] = horseCorpulence,
                ["centerDistance"] = centerDistance,
                ["horizontalDistance"] = horizontal,
                ["legalAdjacencyEnvelope"] = envelope,
                ["isAdjacent"] = rider != null && horse != null && rider.View != null && horse.View != null &&
                    CombatMountDismountPolicy.IsAdjacent(centerDistance, riderCorpulence, horseCorpulence),
                ["approachRadiusResolved"] = approachRadiusResolved,
                ["approachRadius"] = approachRadius,
                ["relationshipState"] = relationship.State.ToString(),
                ["relationshipGeneration"] = relationship.MountedPairGeneration,
                ["command"] = slot == null ? null : new JObject
                {
                    ["type"] = slot.Type.ToString(),
                    ["typeName"] = slot.GetType().Name,
                    ["isUseAbility"] = slotAbility != null,
                    ["abilityGuid"] = slotAbility?.Spell?.Blueprint?.AssetGuid,
                    ["executorId"] = slot.Executor?.UniqueId,
                    ["targetId"] = slot.Target?.Unit?.UniqueId,
                    ["createdByPlayer"] = slot.CreatedByPlayer,
                    ["aiActionPresent"] = slot.AiAction != null,
                    ["queued"] = rider?.Commands?.Queue?.Contains(slot) == true,
                    ["started"] = slot.IsStarted,
                    ["running"] = slot.IsRunning,
                    ["finished"] = slot.IsFinished,
                    ["acted"] = slot.IsActed,
                    ["result"] = slot.Result.ToString(),
                    ["unitEnoughClose"] = slot.IsUnitEnoughClose,
                    ["shouldUnitApproach"] = slot.ShouldUnitApproach,
                    ["approachRadius"] = slot.ApproachRadius,
                    ["hasCooldown"] = rider?.CombatState?.HasCooldownForCommand(slot) == true,
                    ["executionProcessPresent"] = slotAbility?.ExecutionProcess != null,
                    ["executionProcessEnded"] = slotAbility?.ExecutionProcess?.IsEnded,
                    ["executionContextPresent"] = slotAbility?.ExecutionProcess?.Context != null
                },
                ["shellState"] = nativeControls.DescribeRelationshipShellState(rider),
                ["transitionLedger"] = playerAction.TransitionLedger.Describe(),
                ["riderResources"] = Chunk6aCooldowns(rider),
                ["horseResources"] = Chunk6aCooldowns(horse)
            };
            chunk6aGeometry.Add(sample);
            observations["chunk6aGeometry"] = chunk6aGeometry;
            return sample;
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
        // Conservation of native resources across a window.
        //
        // In TURN-BASED combat a cooldown is static between boundaries, so exact equality is
        // the right test. In REAL TIME it is not: Kingmaker's cooldowns tick down
        // continuously, and a run measured a rider's Standard falling 4.447 -> 3.084 across
        // one mount purely because time passed. Demanding equality there fails a correct
        // transition, so the real-time invariant is the one that actually expresses
        // conservation: a CHARGE RAISES a cooldown, therefore no unexcepted cooldown may
        // INCREASE. Discrete reaction allowance and initiative ORDER stay exact here.
        // This endpoint helper alone cannot certify a committed command; those rows use
        // the exact native event proof, including lawful reaction and initiative decay.
        private static bool Chunk6aResourcesHeld(
            JObject before, JObject after, bool turnBased, params string[] allowedToRise)
        {
            foreach (var name in new[] { "standard", "move", "swift", "initiativeCooldown", "initiativeOrder", "reactionCooldown", "reactions", "reactionsPerRound" })
            {
                if (allowedToRise.Contains(name, StringComparer.Ordinal))
                {
                    continue;
                }
                var exact = turnBased || name == "reactions" || name == "reactionsPerRound" || name == "initiativeOrder";
                if (exact)
                {
                    if (!JToken.DeepEquals(before[name], after[name])) { return false; }
                    continue;
                }
                if (before[name] == null || after[name] == null) { return false; }
                // No charge: the cooldown may fall with the clock but must never rise.
                if ((float)after[name] > (float)before[name] + 0.0001f) { return false; }
            }
            return true;
        }

        private bool Chunk6aUnchangedExcept(JObject before, JObject after, params string[] allowedToRise)
        {
            return Chunk6aResourcesHeld(before, after, Chunk6aTurnBased, allowedToRise);
        }

        private void TickChunk6aCombatMount()
        {
            ObserveChunk6aMountOrderTurn();
            var game = Game.Instance;
            var controller = game.TurnBasedCombatController;
            var turn = controller?.CurrentTurn;
            if (chunk6aCommandWindow != null) observations["chunk6aLiveCommand"] = chunk6aCommandWindow.Capture();
            if (chunk6aApproachPath != null) observations["chunk6aApproachPath"] = chunk6aApproachPath.Capture();
            observations["chunk6aProgress"] = new JObject
            {
                ["stage"] = chunk6aStage,
                ["turnBasedRow"] = Chunk6aTurnBased,
                ["mountClicked"] = chunk6aMountClicked,
                ["dismountClicked"] = chunk6aDismountClicked,
                ["disposition"] = chunk6aDisposition.ToString(),
                ["relationshipState"] = relationship.State.ToString(),
                ["turnActor"] = turn?.Unit?.UniqueId,
                ["turnStatus"] = turn?.Status.ToString(),
                ["riderTurnIsCurrent"] = turn?.Unit == rider,
                ["round"] = controller?.RoundNumber,
                ["frame"] = Time.frameCount,
                ["feedback"] = playerAction.LastFeedback
            };
            if (turn != null && chunk6aVisitedTurns.Add(turn) && turn.Unit == horse &&
                relationship.State == RelationshipState.Mounted && !combat.PairedActivationSplit)
            {
                chunk6aMountTurnsWhileMounted++;
            }
            // Stage 32 owns pause until its native auto-use input and lease restoration complete.
            if (game.IsPaused && chunk6aStage != 2 && !(Chunk6aAutoUseOnly && chunk6aStage == 32))
            {
                game.IsPaused = false;
                return;
            }

            // This scenario owns both exploration windows. The parent supplies an
            // unmounted pair and cannot contribute unmeasured transition counters.
            if (chunk6aStage == 0)
            {
                TickChunk6aExplorationAndSetup();
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
                    // native turn reaches Acting after a real ground order below;
                    // this observation precedes that separately measured setup.
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
                    if (turn?.Unit != rider)
                    {
                        TryEndPhase3gFixtureTurn(turn);
                        return;
                    }
                    if (!PrepareChunk6aNativeActingTurn(turn)) return;
                }
                if (!Chunk6aIdle)
                {
                    return;
                }
                var availability = nativeControls.Evaluate(NativeMountedControlKind.MountCompanion, rider);
                // Mount availability is the APPROACH phase. A legal pair outside adjacency
                // must be admitted here so Kingmaker's own Move-typed command can close the
                // distance; waiting for adjacency to appear by itself is what turned a
                // mandatory product defect into a generic leaf deadline.
                var firstReady = CaptureChunk6aGeometry("mount-availability");
                observations["chunk6aMountAvailability"] = new JObject
                {
                    ["visible"] = availability.IsVisible,
                    ["enabled"] = availability.IsEnabled,
                    ["transitionReady"] = availability.IsTransitionReady,
                    ["reason"] = availability.Reason,
                    ["geometry"] = firstReady,
                    ["state"] = CaptureChunk6aState("mount-availability")
                };
                if (!availability.IsEnabled)
                {
                    // If distance is the only thing still refusing approach admission, the
                    // phase split has regressed. Say so exactly and immediately rather than
                    // burning the leaf deadline on a condition that can never resolve.
                    if (availability.Reason != null &&
                        (availability.Reason.IndexOf("adjacent", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         availability.Reason.IndexOf("approach", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        FailCurrent("CM02-approach-arrival",
                            "Combat Mount refused APPROACH admission for distance alone: \"" + availability.Reason +
                            "\". Kingmaker's own Move command is what closes the distance, so this refusal is circular " +
                            "and makes CM02-approach-arrival unreachable. Measured geometry: " +
                            firstReady.ToString(Formatting.None));
                        BeginCleanup();
                        return;
                    }
                    return;
                }
                // The positive geometry baseline is captured only after exact selection in Stage 13.
                chunk6aMountTurn = turn;
                chunk6aMountRound = controller?.RoundNumber ?? -1;
                string dispositionRefusal;
                chunk6aDisposition = combat.ResolveMidEncounterAdoption(rider, horse, out dispositionRefusal);
                chunk6aDispositionRefusal = dispositionRefusal;

                // CM02 negative control: exact native target selection started and
                // cancelled before commitment must change nothing at all.
                if (!EnsureChunk6aRiderSelection("CM01-combat-mount-cancel-costs-nothing")) return;
                chunk6aCancelBefore = CaptureChunk6aState("mount-cancel-before");
                var cancelTraceStart = allocationTrace.EventCount;
                var cancelGeometry = CaptureChunk6aGeometry("cancel-before");
                var cancelShells = nativeControls.NativeRelationshipShellCount;
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
                var cancelGeometryAfter = CaptureChunk6aGeometry("cancel-after");
                AddRow("CM01-combat-mount-cancel-costs-nothing",
                    relationship.State == RelationshipState.Unmounted &&
                        rider.Commands.Empty && horse.Commands.Empty &&
                        nativeControls.NativeRelationshipShellCount == cancelShells &&
                        allocationTrace.EventCount == cancelTraceStart &&
                        JToken.DeepEquals(cancelGeometry["riderPosition"], cancelGeometryAfter["riderPosition"]) &&
                        JToken.DeepEquals(cancelGeometry["horsePosition"], cancelGeometryAfter["horsePosition"]) &&
                        (long)chunk6aCancelBefore["relationshipGeneration"] == (long)cancelAfter["relationshipGeneration"] &&
                        (long)chunk6aCancelBefore["dispatchAccepted"] == (long)cancelAfter["dispatchAccepted"] &&
                        Chunk6aUnchangedExcept((JObject)chunk6aCancelBefore["rider"], (JObject)cancelAfter["rider"]) &&
                        Chunk6aUnchangedExcept((JObject)chunk6aCancelBefore["mount"], (JObject)cancelAfter["mount"]),
                    "Starting and cancelling exact native combat Mount target selection performed no transition and charged nothing.",
                    new JObject { ["before"] = chunk6aCancelBefore, ["after"] = cancelAfter });

                chunk6aStage = Chunk6aAutoUseOnly && !Chunk6aAutoUseDismount ? 31 : Chunk6aCompensationOnly ? 11 : Chunk6aRefusedOnly ? 24 : Chunk6aStopOnly ? 22 : Chunk6aReplacementOnly ? 34 : Chunk6aRepeatedRequestOnly ? 45 : Chunk6aOwnershipOnly ? 36 : Chunk6aSizeFormOnly ? 38 : Chunk6aLostDirectControlOnly ? 40 : Chunk6aPendingIncapacityOnly ? 43 : Chunk6aGeometryOnly ? 16 : Chunk6aObstructionOnly ? 18 : 13;
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
                if (!Chunk6aCompensationOnly) throw new InvalidOperationException("Compensation requires a separate native allocation.");
                if (!EnsureChunk6aRiderSelection("CM02-adoption-plan-invalidated")) return;
                chunk6aCompensationBefore = CaptureChunk6aState("mount-compensation-before");
                chunk6aCompensationLedgerBefore = Chunk6aLedgerCounters();
                chunk6aCompensationDispatchesBefore = (int)nativeControls.DispatchAcceptedCount;
                chunk6aCompensationGenerationBefore = relationship.MountedPairGeneration;
                chunk6aCompensationRollbacksBefore = combat.AdoptionRollbackCount;
                chunk6aCompensationAdoptionsBefore = combat.MidEncounterAdoptionCount;
                BeginChunk6aCommandWindow(nativeControls.MountAbility.AssetGuid);
                chunk6aAdoptionFault = combat.ArmMidEncounterAdoptionFault(rider, horse);
                chunk6aCompensationClicked = TryNativeAbilityTargetClick(
                    nativeControls.MountAbility, horse, "chunk6a-combat-mount-compensation-click");
                chunk6aCommandWindow.ClickCompleted(chunk6aCompensationClicked);
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
                if (chunk6aCommandWindow?.Terminal != true) return;
                Chunk6aDisposeAdoptionFault();
                var compensationProof = FinishChunk6aCommandWindow("compensation", true, 0, false);
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
                var moveStillCommitted = (bool)compensationProof["pass"];

                // Nothing else moved: the mount keeps every resource and the
                // generation advanced exactly once and was never rewound.
                var mountUntouched = (bool)compensationProof["resourceWindow"]["pass"];
                var riderOtherResourcesHeld = (bool)compensationProof["resourceWindow"]["pass"];
                var generationAdvancedOnce =
                    relationship.MountedPairGeneration == chunk6aCompensationGenerationBefore + 1;
                // Measured as the compensation window own delta: one mount admitted, none
                // accepted, one voluntary refusal booked, and exactly one cleanup detach for
                // the compensating Dismount. Nothing is in flight afterwards.
                var ledgerTruthful =
                    Chunk6aLedgerDelta(chunk6aCompensationLedgerBefore, "admittedMount", 1) &&
                    Chunk6aLedgerDelta(chunk6aCompensationLedgerBefore, "acceptedMount", 0) &&
                    Chunk6aLedgerDelta(chunk6aCompensationLedgerBefore, "refusedVoluntary", 1) &&
                    Chunk6aLedgerDelta(chunk6aCompensationLedgerBefore, "forcedDetach", 1) &&
                    !playerAction.HasVoluntaryTransitionInFlight;

                AddRow("CM02-adoption-plan-invalidated",
                    noResidue && noDuplicatePreparation && moveStillCommitted && mountUntouched &&
                        riderOtherResourcesHeld && generationAdvancedOnce && ledgerTruthful,
                    "An encounter adoption refused after the relationship attached was compensated exactly: the relationship returned to unmounted with no activation, partner context or prepared-pair residue, neither actor was prepared again, the rider's already-committed native Move was not refunded, the mount kept every resource, and the relationship generation advanced once and was never rewound.",
                    new JObject
                    {
                        ["commandProof"] = compensationProof,
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

                // A compensated attachment releases its in-flight ownership. A later
                // positive Mount belongs to a separate fresh scenario transaction.
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
                // Compensation ends its transaction. No positive Mount can inherit its
                // movement, debt, generation, selection or native allocation.
                chunk6aStage = 99;
                BeginCleanup();
                return;
            }

            if (chunk6aStage == 31) { BeginChunk6aAutoUse(null); return; }
            if (chunk6aStage == 32) { TickChunk6aAutoUse(); return; }
            if (chunk6aStage == 27 || chunk6aStage == 28) { TickChunk6aDismountEscape(); return; }
            if (chunk6aStage == 29 || chunk6aStage == 30) { TickChunk6aMountOrderCompletion(); return; }
            if (chunk6aStage == 24) { TickChunk6aRefusedMount(); return; }
            if (chunk6aStage == 34 || chunk6aStage == 35) { TickChunk6aReplacementApproach(); return; }
            if (chunk6aStage == 45 || chunk6aStage == 46) { TickChunk6aRepeatedRequest(); return; }
            if (chunk6aStage == 36 || chunk6aStage == 37) { TickChunk6aOwnershipChange(); return; }
            if (chunk6aStage == 38 || chunk6aStage == 39) { TickChunk6aSizeFormChange(); return; }
            if (chunk6aStage >= 40 && chunk6aStage <= 42) { TickChunk6aLostDirectControl(); return; }
            if (chunk6aStage == 43 || chunk6aStage == 44) { TickChunk6aPendingIncapacity(); return; }
            if (chunk6aStage == 22 || chunk6aStage == 23) { TickChunk6aStopApproach(); return; }
            if (chunk6aStage == 16 || chunk6aStage == 17)
            {
                TickChunk6aGeometryChange();
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
                        "The rider's acting turn was lost before the positive Mount request.");
                    BeginCleanup();
                    return;
                }
                // Admission reads actual native Move readiness. Native RT cooldown
                // decay may be awaited; this fresh allocation has no compensation.
                var readinessCooldowns = Chunk6aCooldowns(rider);
                var moveReadiness = MountedNativeMoveReadinessPolicy.Decide(
                    readinessCooldowns["hasMove"] != null && (bool)readinessCooldowns["hasMove"],
                    readinessCooldowns["move"] == null ? 0f : (float)readinessCooldowns["move"],
                    Chunk6aTurnBased);
                if (chunk6aMoveRestorationWait == null)
                {
                    chunk6aMoveRestorationWait = new JObject
                    {
                        ["firstReadiness"] = moveReadiness.ToString(),
                        ["firstDescription"] = MountedNativeMoveReadinessPolicy.Describe(
                            moveReadiness,
                            readinessCooldowns["move"] == null ? 0f : (float)readinessCooldowns["move"],
                            Chunk6aTurnBased),
                        ["firstCooldowns"] = readinessCooldowns,
                        ["waitedFrames"] = 0
                    };
                    observations["chunk6aMoveRestorationWait"] = chunk6aMoveRestorationWait;
                }
                if (MountedNativeMoveReadinessPolicy.ShouldWait(moveReadiness))
                {
                    chunk6aMoveRestorationWait["waitedFrames"] =
                        (int)chunk6aMoveRestorationWait["waitedFrames"] + 1;
                    chunk6aMoveRestorationWait["lastWaitingCooldowns"] = readinessCooldowns;
                    return;
                }
                if (moveReadiness == MountedNativeMoveReadiness.Unavailable)
                {
                    FailCurrent("CM01-combat-mount-accepted",
                        "The rider held no native Move when the combat Mount request became due, and none was being restored. " +
                        MountedNativeMoveReadinessPolicy.Describe(
                            moveReadiness,
                            readinessCooldowns["move"] == null ? 0f : (float)readinessCooldowns["move"],
                            Chunk6aTurnBased) +
                        " cooldowns=" + readinessCooldowns.ToString(Formatting.None));
                    BeginCleanup();
                    return;
                }
                chunk6aMoveRestorationWait["resolvedReadiness"] = moveReadiness.ToString();
                chunk6aMoveRestorationWait["resolvedCooldowns"] = readinessCooldowns;
                if (Chunk6aCompensationOnly) throw new InvalidOperationException("Positive Mount cannot follow compensation.");
                if (!EnsureChunk6aRiderSelection("CM02-approach-arrival")) return;
                if (!PrepareChunk6aHotbar()) return;
                PrepareChunk6aPausedQueue();
                chunk6aPreMount = CaptureChunk6aState("mount-before");
                chunk6aApproachStart = CaptureChunk6aGeometry("positive-pre-click");
                if ((bool)chunk6aApproachStart["isAdjacent"])
                {
                    FailCurrent("CM02-approach-arrival", "Positive pre-click geometry is already inside the transition envelope.");
                    BeginCleanup();
                    return;
                }
                chunk6aMountLedgerBefore = Chunk6aLedgerCounters();
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
                if (Chunk6aTurnBased)
                {
                    chunk6aApproachPath = new NativeMountApproachPathProbe(rider, horse);
                    // Capture only after the native target preview passes IgnoreClick.
                }
                BeginChunk6aCommandWindow(nativeControls.MountAbility.AssetGuid);
                if (Chunk6aTurnBased) { BeginChunk6aNativePointer(); return; }
                chunk6aMountClicked = Chunk6aHotbarOnly ? InvokeChunk6aHotbar() : TryNativeAbilityTargetClick(
                    nativeControls.MountAbility, horse, "chunk6a-combat-mount-click");
                CompleteChunk6aPositiveClick();
                return;
            }

            if (chunk6aStage == 26) { TickChunk6aNativePointer(); return; }

            // Stage 2: await the native command's own terminal state and delivery.
            if (chunk6aStage == 2)
            {
                if (TickChunk6aPausedHold()) return;
                // Sample the approach while Kingmaker's own command still exists. The
                // command leaves the Move slot before the relationship transition lands, so
                // the acted/resource-commitment boundary can only be observed here; keeping
                // the LAST such sample gives the geometry and command state at commitment.
                var approachSlot = rider?.Commands?.GetCommand(UnitCommand.CommandType.Move) as UnitUseAbility;
                if (approachSlot != null &&
                    ReferenceEquals(approachSlot.Spell?.Blueprint, nativeControls.MountAbility))
                {
                    var approachSample = CaptureChunk6aGeometry(
                        approachSlot.IsActed ? "acted-resource-commitment" : "approach-start");
                    if (approachSlot.IsActed || chunk6aApproachClosed == null)
                    {
                        chunk6aApproachClosed = approachSample;
                    }
                }
                if (relationship.State == RelationshipState.Unmounted && Chunk6aIdle &&
                    nativeControls.DispatchAcceptedCount == chunk6aDispatchesBefore &&
                    nativeControls.DispatchRejectedCount > chunk6aRejectionsBefore)
                {
                    FailCurrent("CM01-combat-mount-accepted",
                        "The admitted native combat Mount shell refused its own delivery: " + playerAction.LastFeedback);
                    BeginCleanup();
                    return;
                }
                if (relationship.State != RelationshipState.Mounted || !Chunk6aIdle ||
                    chunk6aCommandWindow?.Terminal != true)
                {
                    return;
                }
                var expectedMountPrepare = chunk6aDisposition == MidEncounterAdoption.PreparePartnerThisRound ? 1 : 0;
                var mountProof = FinishChunk6aCommandWindow("positive-mount", true, expectedMountPrepare, true);
                FinishChunk6aHotbar(mountProof);
                FinishChunk6aPausedQueue(mountProof);
                var after = CaptureChunk6aState("mount-after");
                var riderBefore = (JObject)chunk6aPreMount["rider"];
                var riderAfter = (JObject)after["rider"];
                var mountBefore = (JObject)chunk6aPreMount["mount"];
                var mountAfter = (JObject)after["mount"];
                var expectedMove = Chunk6aTurnBased
                    ? (float?)((float)riderBefore["move"] + 3f)
                    : null;
                var moveCommitted = (bool)mountProof["resourceWindow"]["pass"];
                var exactPair = relationship.Rider == rider && relationship.Mount == horse;
                var oneDelivery = nativeControls.DispatchAcceptedCount == chunk6aDispatchesBefore + 1;
                var oneTransition = relationship.MountedPairGeneration == chunk6aGenerationBefore + 1 &&
                    Chunk6aLedgerDelta(chunk6aMountLedgerBefore, "acceptedMount", 1) &&
                    Chunk6aLedgerDelta(chunk6aMountLedgerBefore, "admittedMount", 1) &&
                    Chunk6aLedgerDelta(chunk6aMountLedgerBefore, "forcedDetach", 0) &&
                    Chunk6aLedgerDelta(chunk6aMountLedgerBefore, "refusedVoluntary", 0);
                AddRow("CM01-combat-mount-accepted",
                    exactPair && oneDelivery && oneTransition && moveCommitted && (bool)mountProof["pass"] &&
                        relationship.Runtime.PoseConfigured && relationship.Runtime.PoseHealthy,
                    "One exact native combat Mount delivery produced one relationship transition and committed the rider's native Move exactly once.",
                    new JObject
                    {
                        ["commandProof"] = mountProof,
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
                var riderOtherResourcesHeld = (bool)mountProof["resourceWindow"]["pass"];
                var mountResourcesHeld = (bool)mountProof["resourceWindow"]["pass"];
                var riderPrepareUnchanged =
                    (int)riderBefore["nativePrepareCount"] == (int)riderAfter["nativePrepareCount"];
                var expectedMountPrepareDelta =
                    chunk6aDisposition == MidEncounterAdoption.PreparePartnerThisRound ? 1 : 0;
                var mountPrepareDelta =
                    (int)mountAfter["nativePrepareCount"] - (int)mountBefore["nativePrepareCount"];
                AddRow("CM03-combat-mount-conserves-debt",
                    riderOtherResourcesHeld && mountResourcesHeld && (bool)mountProof["resourceWindow"]["pass"],
                    "The exact combat Mount charged one rider Move, observed no other native action cost, and conserved debt on the native clock except for the declared partner preparation.",
                    new JObject
                    {
                        ["resourceWindow"] = mountProof["resourceWindow"].DeepClone(),
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

                // CM02-approach-arrival: "Mount from outside adjacency through legal native
                // rider approach and legal arrival, with no teleport or manufactured
                // endpoint." Every clause is checked against measured geometry rather than
                // asserted, and the measurements come from the same boundaries the evidence
                // publishes.
                if (!(bool)mountProof["pass"])
                {
                    AddRow("CM02-approach-arrival", false, "The exact command proof failed; endpoint geometry cannot replace a missing or inconsistent causal boundary.", mountProof);
                    BeginCleanup(); return;
                }
                var terminalGeometry = CaptureChunk6aGeometry("transition-result");
                // Arrival is measured at Deliver BEFORE attachment can move the rider view.
                var arrival = mountProof["samples"].OfType<JObject>().Single(sample =>
                    (string)sample["boundary"] == "deliver")["state"]["geometry"] as JObject;
                var startDistance = (float)chunk6aApproachStart["centerDistance"];
                var startEnvelope = (float)chunk6aApproachStart["legalAdjacencyEnvelope"];
                var arrivalDistance = (float)arrival["centerDistance"];
                var arrivalEnvelope = (float)arrival["legalAdjacencyEnvelope"];
                var startedOutside = startEnvelope > 0f && startDistance > startEnvelope &&
                    (bool)chunk6aApproachStart["isAdjacent"] == false;
                var arrivedInside = arrivalEnvelope > 0f && arrivalDistance <= arrivalEnvelope &&
                    (bool)arrival["isAdjacent"];
                var riderDisplacement = Chunk6aPlanarDistance(
                    (JObject)chunk6aApproachStart["riderPosition"], (JObject)arrival["riderPosition"]);
                var horseDisplacement = Chunk6aPlanarDistance(
                    (JObject)chunk6aApproachStart["horsePosition"], (JObject)arrival["horsePosition"]);
                // The RIDER closes the distance. A Horse walking into reach would satisfy
                // adjacency while proving nothing about rider approach, so it is refused as
                // evidence: the Horse must be effectively stationary and the rider must have
                // covered at least the distance the envelope required.
                var riderClosedTheDistance = riderDisplacement >= startDistance - arrivalEnvelope - 0.5f &&
                    riderDisplacement > horseDisplacement &&
                    horseDisplacement <= Chunk6aStationaryToleranceMeters;
                var approachBoundary = mountProof["samples"].OfType<JObject>().FirstOrDefault(sample =>
                    (string)sample["boundary"] == "approach-start");
                chunk6aApproachClosed = approachBoundary?["state"]?["geometry"] as JObject;
                var approachCommand = (JObject)chunk6aApproachClosed?["command"];
                var oneRiderOwnedApproach = approachCommand != null &&
                    (bool)approachCommand["isUseAbility"] &&
                    (string)approachCommand["executorId"] == rider.UniqueId &&
                    (string)approachCommand["targetId"] == horse.UniqueId &&
                    (string)approachCommand["type"] == UnitCommand.CommandType.Move.ToString() &&
                    (string)approachCommand["abilityGuid"] == nativeControls.MountAbility.AssetGuid;
                var actedObserved = (bool)mountProof["exactActedObserved"];
                var sameCommand = (bool)mountProof["sameCommandAtEveryBoundary"];
                // One request for this transition: the window admitted exactly one mount,
                // suppressed no duplicate of its own, and repeated no preparation.
                var noDuplicateRequest =
                    Chunk6aLedgerDelta(chunk6aMountLedgerBefore, "duplicateSuppressed", 0) &&
                    Chunk6aLedgerDelta(chunk6aMountLedgerBefore, "admittedMount", 1) &&
                    riderPrepareUnchanged;
                AddRow("CM02-approach-arrival",
                    startedOutside && arrivedInside && riderClosedTheDistance &&
                        oneRiderOwnedApproach && actedObserved && sameCommand && oneDelivery && oneTransition &&
                        moveCommitted && noDuplicateRequest,
                    "Combat Mount was admitted at a measured distance outside the transition envelope, Kingmaker's own rider-owned native Move command closed the distance and arrived inside that envelope, and exactly one acted Move commitment, one shell delivery and one relationship transition followed with no Horse movement, no duplicate request and no repeated preparation.",
                    new JObject
                    {
                        ["approachStart"] = chunk6aApproachStart,
                        ["approachClosed"] = chunk6aApproachClosed,
                        ["arrival"] = arrival,
                        ["terminalGeometry"] = terminalGeometry,
                        ["startDistance"] = startDistance,
                        ["startEnvelope"] = startEnvelope,
                        ["arrivalDistance"] = arrivalDistance,
                        ["arrivalEnvelope"] = arrivalEnvelope,
                        ["startedOutside"] = startedOutside,
                        ["arrivedInside"] = arrivedInside,
                        ["riderDisplacement"] = riderDisplacement,
                        ["horseDisplacement"] = horseDisplacement,
                        ["riderClosedTheDistance"] = riderClosedTheDistance,
                        ["oneRiderOwnedApproach"] = oneRiderOwnedApproach,
                        ["commandProof"] = mountProof,
                        ["actedObserved"] = actedObserved,
                        ["noDuplicateRequest"] = noDuplicateRequest,
                        ["transitionLedger"] = playerAction.TransitionLedger.Describe()
                    });

                // The narrow approach scenario stops here. Its whole claim is the
                // non-adjacent admission and the native approach that followed, so it
                // finishes rather than continuing into the rest of the 6A ledger.
                if (Chunk6aApproachOnly)
                {
                    BeginCleanup();
                    return;
                }
                if (Chunk6aAutoUseOnly) { BeginChunk6aAutoUse(mountProof); return; }
                if (Chunk6aDismountEscapeOnly) { BeginChunk6aDismountEscape(mountProof); return; }
                if (Chunk6aMountOrderOnly) { FinishChunk6aMountOrder(mountProof); return; }
                if (request.Scenario == "chunk6a-combat-mount-tb") allocationTrace.ObserveReactionResources = true;
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
                chunk6aRepeatLedgerBefore = Chunk6aLedgerCounters();
                chunk6aRepeatClicked = TryNativeAbilityTargetClick(
                    nativeControls.MountAbility, horse, "chunk6a-combat-mount-repeat-click");
                var repeatAfter = CaptureChunk6aState("mount-repeat-after");
                AddRow("CM06-combat-mount-repeat-refused",
                    !chunk6aRepeatClicked &&
                        relationship.State == RelationshipState.Mounted &&
                        (long)repeatBefore["relationshipGeneration"] == (long)repeatAfter["relationshipGeneration"] &&
                        (long)repeatBefore["dispatchAccepted"] == (long)repeatAfter["dispatchAccepted"] &&
                        Chunk6aLedgerDelta(chunk6aRepeatLedgerBefore, "acceptedMount", 0) &&
                        Chunk6aLedgerDelta(chunk6aRepeatLedgerBefore, "admittedMount", 0) &&
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
                if (!PrepareChunk6aDismountTurn(turn)) return;
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
                // The combat Dismount is only a COMBAT Dismount while the encounter is live.
                // A run reached this stage with the rider's Move still draining from the
                // combat Mount, the encounter ended while it waited, and the Dismount then
                // ran out of combat where Kingmaker correctly charges nothing -- so the row
                // failed on its derived move-commitment clause rather than on the real cause.
                // The encounter is now a stated precondition, reported with the liveness
                // observation that names which lease stopped validating.
                if (!rider.IsInCombat || !horse.IsInCombat || !Game.Instance.Player.IsInCombat)
                {
                    FailCurrent("CM05-combat-dismount-accepted",
                        "The encounter ended before the combat Dismount became available, so no combat Dismount could be " +
                        "observed and Kingmaker would charge nothing for one performed outside combat. " +
                        "riderInCombat=" + rider.IsInCombat + "; horseInCombat=" + horse.IsInCombat +
                        "; playerInCombat=" + Game.Instance.Player.IsInCombat +
                        "; liveness=" + (chunk6aEncounterLiveness == null
                            ? "not-observed"
                            : chunk6aEncounterLiveness.ToString(Formatting.None)));
                    BeginCleanup();
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
                if (!EnsureChunk6aRiderSelection("CM05-combat-dismount-accepted")) return;
                CaptureChunk6aDismountReady();
                chunk6aPreDismount = CaptureChunk6aState("dismount-before");
                BeginChunk6aCommandWindow(nativeControls.DismountAbility.AssetGuid);
                chunk6aDismountLedgerBefore = Chunk6aLedgerCounters();
                chunk6aDispatchesBefore = (int)nativeControls.DispatchAcceptedCount;
                chunk6aDismountClicked = TryNativeAbilityTargetClick(
                    nativeControls.DismountAbility, rider, "chunk6a-combat-dismount-click");
                chunk6aCommandWindow.ClickCompleted(chunk6aDismountClicked);
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
                if (relationship.State != RelationshipState.Unmounted || !Chunk6aIdle ||
                    chunk6aCommandWindow?.Terminal != true)
                {
                    return;
                }
                var dismountProof = FinishChunk6aCommandWindow("combat-dismount", true, 0, false);
                var after = CaptureChunk6aState("dismount-after");
                var riderBefore = (JObject)chunk6aPreDismount["rider"];
                var riderAfter = (JObject)after["rider"];
                var mountBefore = (JObject)chunk6aPreDismount["mount"];
                var mountAfter = (JObject)after["mount"];
                var expectedMove = Chunk6aTurnBased ? (float?)((float)riderBefore["move"] + 3f) : null;
                var moveCommitted = (bool)dismountProof["resourceWindow"]["pass"];
                var oneDelivery = nativeControls.DispatchAcceptedCount == chunk6aDispatchesBefore + 1;
                AddRow("CM05-combat-dismount-accepted",
                    oneDelivery && moveCommitted && (bool)dismountProof["pass"] &&
                        Chunk6aLedgerDelta(chunk6aDismountLedgerBefore, "acceptedDismount", 1) &&
                        Chunk6aLedgerDelta(chunk6aDismountLedgerBefore, "forcedDetach", 0) &&
                        rider.IsInState && horse.IsInState &&
                        rider.Descriptor.State.IsConscious && horse.Descriptor.State.IsConscious,
                    "One exact native voluntary combat Dismount delivery committed the rider's native Move exactly once and left two valid separate actors.",
                    new JObject
                    {
                        ["commandProof"] = dismountProof,
                        ["before"] = chunk6aPreDismount, ["after"] = after,
                        ["expectedRiderMove"] = expectedMove,
                        ["oneDelivery"] = oneDelivery, ["moveCommitted"] = moveCommitted,
                        ["voluntaryOnly"] = playerAction.TransitionLedger.Describe()
                    });
                var riderHeld = (bool)dismountProof["resourceWindow"]["pass"];
                var mountHeld = riderHeld;
                AddRow("CM05-combat-dismount-conserves-debt",
                    riderHeld && mountHeld &&
                        (int)riderBefore["nativePrepareCount"] == (int)riderAfter["nativePrepareCount"] &&
                        (int)mountBefore["nativePrepareCount"] == (int)mountAfter["nativePrepareCount"],
                    "Voluntary combat Dismount refunded and refreshed nothing and repeated no native preparation for either actor.",
                    new JObject
                    {
                        ["resourceWindow"] = dismountProof["resourceWindow"].DeepClone(),
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
                // Geometry and obstruction are mandatory separate fresh transactions.
                // The external campaign gate binds them to this exact candidate and suite.
                chunk6aStage = 99;
                BeginCleanup();
                return;
            }

            if (chunk6aStage >= 18 && chunk6aStage <= 21)
            {
                TickChunk6aObstruction();
                return;
            }

        }
    }
}
