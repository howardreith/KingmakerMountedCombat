using System;
using System.Collections;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.TurnBasedMode;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Utility;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using Pathfinding;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7CombatScenario
    {
        private UnitMoveTo movement;
        private UnitUseAbility attackShell;
        private MountedPairAttackOutcome priorOutcome;
        private JObject moveObservation, attackObservation, relationshipObservation;
        private Vector3 moveStart;
        private void BeginRelationship(UnitEntityData caster, UnitEntityData targetActor, BlueprintAbility ability, UnitEntityData switchTo, string label)
        {
            var observation = new JObject { ["label"] = label, ["before"] = WorldSnapshot("relationship-before") };
            relationshipObservation = observation; inputs.Add(observation);
            relationshipShell = Chunk7NativeFixture.Issue(controls, caster, targetActor, ability, switchTo, observation);
            issuedCommands.Add(relationshipShell);
        }
        private bool RelationshipSettled()
        {
            var settled = relationshipShell != null && relationshipShell.IsFinished &&
                (relationshipShell.ExecutionProcess == null || relationshipShell.ExecutionProcess.IsEnded) && !controls.HasUnsettledRelationshipTransition;
            if (settled && relationshipObservation["terminal"] == null)
            {
                relationshipObservation["terminal"] = true; relationshipObservation["processTerminal"] = true;
                relationshipObservation["after"] = WorldSnapshot("relationship-after");
            }
            return settled;
        }
        // Owner-seed TB rows 7/8 (first-action Mount contract v3): the paired full-activation move is searched like the
        // independent Horse step. Other callers (native life, checkpoint sources, registered disable) keep the fixed click.
        internal const string PairedStepSearchContract = "native-tb-paired-step-destination-v1";
        private bool PairedStepSearchSelected => !checkpointOnly && OwnerSeedDerived && !RegisteredDisableCase &&
            (SelectedScenario == RiderFirst || SelectedScenario == MountFirst);
        private void BeginMove()
        {
            var rider = actingPair.Relationship.Rider; var mount = actingPair.Relationship.Mount;
            Chunk7NativeFixture.Select(rider); Game.Instance.IsPaused = true;
            moveStart = mount.Position;
            var delta = target.Position - moveStart; delta.y = 0f;
            Require(delta.sqrMagnitude > .01f, "Native target direction is unavailable.");
            // Different distances make the two actor allocation histories distinguishable.
            var stepDistance = actingIndex == 0 ? .75f : .5f;
            var destination = moveStart + delta.normalized * stepDistance;
            moveObservation = new JObject { ["kind"] = "native-ground", ["before"] = WorldSnapshot("move-before"),
                ["selectedId"] = rider.UniqueId, ["executorId"] = mount.UniqueId, ["requestedDistance"] = Vector3.Distance(moveStart, destination) };
            activation["movement"] = moveObservation;
            if (PairedStepSearchSelected) moveObservation["clicked"] = ClickSearchedPairedStep(delta.normalized, stepDistance);
            else using (var input = new NativeOrdinaryAttackInput(destination))
            {
                input.Predict(); var attempts = 0;
                while ((actingTurn.EnabledFiveFootStep || actingTurn.EnabledSingleActionMove) && attempts++ < 4)
                { input.Click(button: 1); input.Predict(); }
                moveObservation["clicked"] = input.Click();
            }
            movement = mount.Commands.Move as UnitMoveTo;
            Require(movement?.Executor == mount && (bool)moveObservation["clicked"], "Native paired ground input did not admit the exact mount command.");
            issuedCommands.Add(movement); moveObservation["command"] = trace.ObjectIdentity(movement);
            Chunk7NativeFixture.Select(actingIndex == 0 ? br : ar); Game.Instance.IsPaused = false;
        }
        private bool FinishMove()
        {
            if (movement == null || !movement.IsFinished || actingPair.Combat.HasActiveGroundMovement ||
                !actingPair.Relationship.Rider.Commands.Empty || !actingPair.Relationship.Mount.Commands.Empty) return false;
            var rider = actingPair.Relationship.Rider; var mount = actingPair.Relationship.Mount;
            var after = WorldSnapshot("move-after"); moveObservation["after"] = after;
            var before = (JObject)moveObservation["before"];
            var distance = Vector3.Distance(moveStart, mount.Position); moveObservation["distance"] = distance;
            var beforeMount = Actor(before, mount); var afterMount = Actor(after, mount);
            // First-action Mount v4 (F6 review): the searched paired step records what the mount actually travelled, beside the
            // admitted destination, before the assertion: MechanicsDistance, its bearing relative to the target position
            // observed at move completion (Quaternion.Euler convention), and the end's residual to the clicked destination.
            if (moveObservation["destinationSearch"] != null)
            {
                Vector3 toward = target.Position - moveStart, travel = mount.Position - moveStart; toward.y = 0f; travel.y = 0f;
                var destination = moveObservation["destination"];
                var end = new Vector3((float)destination[0], (float)destination[1], (float)destination[2]);
                moveObservation["travelled"] = new JObject { ["targetPosition"] = new JArray(target.Position.x, target.Position.y, target.Position.z),
                    ["distance"] = GeometryUtils.MechanicsDistance(moveStart, mount.Position),
                    ["bearingOffset"] = travel.sqrMagnitude < 1e-8f ? null : new JValue(Math.Atan2(toward.z * travel.x - toward.x * travel.z, toward.x * travel.x + toward.z * travel.z) * 180.0 / Math.PI),
                    ["residualToDestination"] = GeometryUtils.MechanicsDistance(mount.Position, end) };
            }
            Require(ReferenceEquals(actingTurn, Game.Instance.TurnBasedCombatController.CurrentTurn) && distance > .02f &&
                (float)afterMount["move"] > (float)beforeMount["move"] &&
                (float)Actor(before, rider)["move"] == (float)Actor(after, rider)["move"] &&
                NeighbourBudgetsUnchanged(before, after), "Mounted native transport did not preserve separate actor expenditure.");
            moveObservation["terminal"] = true; return true;
        }
        // The fixed paired click admitted 0.265-0.507 m of travel for 0.5 m (b213, F3), the F3 r07/r08 mechanism: the
        // turn-based click consumes the native PathVisualizer preview of the paired input unit (ClickGroundHandler.OnClick
        // reads it through the paired path-unit seam), whose end snaps to the grid. The same ground click is issued only for
        // the first step destination (declared distance, target bearing first, NativeGroundStepDestinationPolicy offsets)
        // whose own fresh, completed preview ends within the click's approach radius of it and outside it from the mount.
        // Every candidate is recorded before the choice; none refuses the row as uncovered before any input.
        private bool ClickSearchedPairedStep(Vector3 toward, float stepDistance)
        {
            var candidates = new JArray();
            moveObservation["destinationSearch"] = new JObject { ["contract"] = PairedStepSearchContract, ["stepDistance"] = stepDistance,
                ["approachRadius"] = NativeGroundStepDestinationPolicy.NativeGroundApproachRadius,
                ["minimumNativeApproach"] = NativeGroundStepDestinationPolicy.MinimumNativeApproach, ["candidates"] = candidates };
            NativeOrdinaryAttackInput chosen = null;
            try
            {
                foreach (var offset in NativeGroundStepDestinationPolicy.BearingOffsets())
                {
                    var destination = moveStart + Quaternion.Euler(0f, offset, 0f) * toward * stepDistance;
                    var row = new JObject { ["bearingOffset"] = offset, ["destination"] = new JArray(destination.x, destination.y, destination.z) };
                    candidates.Add(row);
                    var unit = PairedPreviewUnit(); var held = unit?.View == null ? null : PathVisualizer.Instance?.CurrentPathForUnit(unit.View);
                    NativeOrdinaryAttackInput input;
                    try { input = new NativeOrdinaryAttackInput(destination); }
                    catch (InvalidOperationException error) { row["reason"] = "native-handler-not-ground"; row["detail"] = error.Message; continue; }
                    var admitted = false;
                    try
                    {
                        input.Predict(); var attempts = 0;
                        while ((actingTurn.EnabledFiveFootStep || actingTurn.EnabledSingleActionMove) && attempts++ < 4) { input.Click(button: 1); input.Predict(); }
                        unit = PairedPreviewUnit();
                        var path = unit?.View == null ? null : PathVisualizer.Instance?.CurrentPathForUnit(unit.View); var points = path?.vectorPath;
                        var fresh = path != null && !ReferenceEquals(path, held);
                        var complete = path != null && path.CompleteState == PathCompleteState.Complete && points != null && points.Count > 0;
                        var end = complete ? points[points.Count - 1] : moveStart;
                        var residual = complete ? (double)GeometryUtils.MechanicsDistance(end, destination) : double.NaN;
                        var displacement = complete ? (double)GeometryUtils.MechanicsDistance(end, moveStart) : double.NaN;
                        admitted = NativeGroundStepDestinationPolicy.Admits(fresh, complete, residual, displacement);
                        row["previewUnitId"] = unit?.UniqueId; row["freshPreview"] = fresh; row["previewState"] = path?.CompleteState.ToString();
                        row["previewPoints"] = points?.Count; row["previewEnd"] = complete ? new JArray(end.x, end.y, end.z) : null;
                        row["endResidual"] = complete ? new JValue(residual) : null; row["displacement"] = complete ? new JValue(displacement) : null;
                        row["reason"] = NativeGroundStepDestinationPolicy.Reason(fresh, complete, residual, displacement);
                    }
                    finally { if (!admitted) input.Dispose(); }
                    if (!admitted) continue;
                    chosen = input; moveObservation["destination"] = new JArray(destination.x, destination.y, destination.z);
                    moveObservation["bearingOffset"] = offset; moveObservation["requestedDistance"] = Vector3.Distance(moveStart, destination);
                    break;
                }
                Require(chosen != null, "No paired step destination's own native preview leaves the mount's approach radius; the full-activation " +
                    "move is uncovered on this fixture: " + candidates.ToString(Newtonsoft.Json.Formatting.None));
                return chosen.Click();
            }
            finally { chosen?.Dispose(); }
        }
        // The unit whose preview ClickGroundHandler.OnClick consumes: the paired native input context's unit (the product's
        // paired path-unit seam), else the native current unit.
        private UnitEntityData PairedPreviewUnit() =>
            world.UnifiedTurn?.SelectedNativeInputContext(Game.Instance.TurnBasedCombatController.CurrentTurn)?.Unit ?? TurnBased.Controllers.CombatController.CurrentUnit;
        private void BeginAttack(bool mountAttack, string label)
        {
            var rider = actingPair.Relationship.Rider;
            var ability = mountAttack ? controls.MountPrimaryAbility : controls.RiderPrimaryAbility;
            priorOutcome = actingPair.Combat.LastOutcome;
            attackObservation = new JObject { ["label"] = label, ["mountAttack"] = mountAttack, ["pair"] = actingIndex,
                ["before"] = WorldSnapshot("attack-before") };
            inputs.Add(attackObservation);
            Require(targetService.BeginExpectedAttackDispatch(target), "Exact diagnostic target attack authorization refused.");
            Game.Instance.IsPaused = true; Chunk7NativeFixture.Select(rider); controls.Update();
            var data = rider.Descriptor.Abilities.GetAbility(ability)?.Data; var handler = Game.Instance.SelectedAbilityHandler;
            Require(data != null && handler != null, "Native actor primary ability is unavailable.");
            var casts = controls.NativeCastRequestCount; var refusals = controls.NativeRefusalCount;
            handler.SetAbility(data); bool clicked;
            try { clicked = handler.OnClick(target.View.gameObject, target.Position, 0, false, false); }
            finally { handler.DropAbility(); }
            attackShell = rider.Commands.Raw.OfType<UnitUseAbility>().SingleOrDefault(c => ReferenceEquals(c.Spell?.Blueprint, ability));
            attackObservation["clicked"] = clicked; attackObservation["nativeCastDelta"] = controls.NativeCastRequestCount - casts;
            attackObservation["nativeRefusalDelta"] = controls.NativeRefusalCount - refusals;
            attackObservation["command"] = trace.ObjectIdentity(attackShell); attackObservation["casterId"] = rider.UniqueId;
            attackObservation["executorId"] = attackShell?.Executor?.UniqueId; attackObservation["abilityGuid"] = ability.AssetGuid;
            Require(clicked && controls.NativeCastRequestCount == casts + 1 && controls.NativeRefusalCount == refusals &&
                attackShell?.Executor == rider && attackShell.Target?.Unit == target,
                "Native primary click did not admit one exact actor/target command.");
            issuedCommands.Add(attackShell);
            Chunk7NativeFixture.Select(actingIndex == 0 ? br : ar);
            attackObservation["selectionAfterInput"] = SelectionManager.Instance.SelectedUnits.Single().UniqueId;
            Game.Instance.IsPaused = false;
        }
        private bool FinishAttack(bool mountAttack)
        {
            var outcome = actingPair.Combat.LastOutcome;
            if (ReferenceEquals(outcome, priorOutcome) || actingPair.Combat.HasActiveCommand || actingPair.Combat.HasActiveGroundMovement ||
                !actingPair.Relationship.Rider.Commands.Empty || !actingPair.Relationship.Mount.Commands.Empty ||
                attackShell == null || !attackShell.IsFinished || attackShell.ExecutionProcess != null && !attackShell.ExecutionProcess.IsEnded) return false;
            var actor = mountAttack ? actingPair.Relationship.Mount : actingPair.Relationship.Rider;
            var other = mountAttack ? actingPair.Relationship.Rider : actingPair.Relationship.Mount;
            var before = (JObject)attackObservation["before"]; var after = WorldSnapshot("attack-after");
            attackObservation["after"] = after;
            attackObservation["outcome"] = new JObject { ["actorId"] = outcome.ActorId, ["resourceOwnerId"] = outcome.ResourceOwnerId,
                ["targetId"] = outcome.TargetId, ["result"] = outcome.Result, ["nativeAttackRule"] = outcome.NativeAttackRuleObserved,
                ["completedAttacks"] = outcome.NativeCompletedAttackCount, ["singleAttack"] = outcome.SingleAttackMode };
            Require(ReferenceEquals(actingTurn, Game.Instance.TurnBasedCombatController.CurrentTurn) && outcome.Result == "Success" &&
                outcome.ActorId == actor.UniqueId && outcome.ResourceOwnerId == actor.UniqueId && outcome.TargetId == target.UniqueId &&
                outcome.NativeAttackRuleObserved && outcome.NativeCompletedAttackCount == 1 &&
                (float)Actor(after, actor)["standard"] > (float)Actor(before, actor)["standard"] &&
                (float)Actor(after, other)["standard"] == (float)Actor(before, other)["standard"] &&
                NeighbourBudgetsUnchanged(before, after), "Primary attack escaped its exact actor allocation or affected the neighbouring pair.");
            return true;
        }
        private void ProveChargeRefusal()
        {
            if (activation["tbChargeRefusal"] != null) return;
            var rider = actingPair.Relationship.Rider; Chunk7NativeFixture.Select(rider); Game.Instance.IsPaused = true;
            var before = WorldSnapshot("tb-charge-before"); var availability = controls.Evaluate(NativeMountedControlKind.MountedCharge, rider);
            var data = rider.Descriptor.Abilities.GetAbility(controls.MountedChargeAbility)?.Data;
            Require(data != null, "Native Charge fact is absent for the declared refusal proof.");
            var handler = Game.Instance.SelectedAbilityHandler; var casts = controls.NativeCastRequestCount; var refused = controls.NativeRefusalCount;
            handler.SetAbility(data); bool clicked;
            try { clicked = handler.OnClick(target.View.gameObject, target.Position, 0, false, false); }
            finally { handler.DropAbility(); }
            var after = WorldSnapshot("tb-charge-after");
            activation["tbChargeRefusal"] = new JObject { ["featureEnabled"] = settings.EnableMountedCharge,
                ["available"] = availability.IsEnabled, ["reason"] = availability.Reason, ["clicked"] = clicked,
                ["castDelta"] = controls.NativeCastRequestCount - casts, ["refusalDelta"] = controls.NativeRefusalCount - refused,
                ["before"] = before, ["after"] = after };
            Require(!availability.IsEnabled && controls.NativeCastRequestCount == casts && SameBudgets(before, after) &&
                !rider.Commands.Raw.OfType<UnitUseAbility>().Any(c => c.Spell?.Blueprint == controls.MountedChargeAbility),
                "Unsupported TB Charge admitted work or altered native expenditure.");
            if (actingIndex == 1) Pass("C7-combat-tb-charge-refusal");
            Game.Instance.IsPaused = false;
        }
        private void ExerciseNativePendingSurvivor()
        {
            Game.Instance.IsPaused = true;
            var pending = actingPair; var rider = pending.Relationship.Rider; var mount = pending.Relationship.Mount;
            var before = WorldSnapshot("pending-survivor-before");
            Require((float)Actor(before, mount)["standard"] > 0f && pending.Relationship.State == RelationshipState.Mounted,
                "Pending survivor stimulus requires mounted native mount expenditure.");
            // Labelled native lifecycle stimulus; the engine methods own removal and
            // any legitimate removed-actor consequences. No resource field is written.
            rider.LeaveCombat();
            var controller = Game.Instance.TurnBasedCombatController;
            if (controller.SortedUnits.Contains(rider))
            {
                var removal = typeof(TurnBased.Controllers.CombatController).GetMethods(Flags)
                    .Single(m => m.MetadataToken == 0x06000BE6);
                Require(removal.Name == "RemoveUnit" && removal.GetParameters().Length == 1 &&
                    removal.GetParameters()[0].ParameterType == typeof(UnitEntityData), "Pinned native RemoveUnit signature differs.");
                removal.Invoke(controller, new object[] { rider });
            }
            var participation = pending.Participant.CaptureParticipation(); var after = WorldSnapshot("pending-survivor-after");
            evidence["pendingSurvivor"] = new JObject { ["stimulus"] = "UnitEntityData.LeaveCombat then native RemoveUnit if still registered",
                // The persistence codec's serializer: the game's global default settings serialize this
                // save type as an empty reference shell ({"$id":"1"}, batch 6 slot 3).
                ["before"] = before, ["after"] = after,
                ["participation"] = participation == null ? null : JObject.FromObject(participation, MountedSaveCodec.CreateSerializer()) };
            Require(pending.Relationship.State == RelationshipState.Mounted && participation != null && participation.Activation == null &&
                participation.PendingSplitId == mount.UniqueId && participation.RiderId == null && participation.MountId == null &&
                // Paired removal forfeits through native ForceToEnd(setCooldowns: true): max(before, 6/3/6).
                new[] { "standard", "move", "swift" }.All(f => Math.Abs((float)Actor(after, mount)[f] -
                    Math.Max((float)Actor(before, mount)[f], f == "move" ? 3f : 6f)) <= .001f) &&
                (int)Actor(before, mount)["reactions"] == (int)Actor(after, mount)["reactions"],
                "Native rider removal did not preserve its valid mounted pending-only survivor and mount debt.");
            Pass("C7-combat-native-pending-survivor");
        }
        private void ExerciseLocalCleanupFailure()
        {
            var before = WorldSnapshot("local-cleanup-failure-before"); var untouchedLedger = AllocationRecord(am);
            var expected = new InvalidOperationException("C7 selected pair B cleanup subscriber failure");
            cleanupFault = trigger => { throw expected; }; b.Relationship.Dismounting += cleanupFault;
            var aVisited = false;
            try
            {
                var failures = world.Visit(pair => {
                    if (ReferenceEquals(pair, b))
                    {
                        var result = pair.Relationship.Dismount(CleanupTrigger.Exception);
                        Require(result.Succeeded, "Pair B retained the selected cleanup failure.");
                    }
                    else if (ReferenceEquals(pair, a)) { aVisited = true; pair.Combat.Cancel("C7 independent neighbour cleanup visit"); }
                });
                var held = WorldSnapshot("local-cleanup-failure-held");
                evidence["localFailure"] = new JObject { ["injectedPair"] = "b", ["before"] = before, ["held"] = held,
                    ["failureCount"] = failures.Length, ["healthyNeighbourVisited"] = aVisited };
                Require(failures.Length == 1 && aVisited && b.Relationship.PairOwner != null &&
                    ReferenceEquals(world.Registry.FindOwner(br), b.Relationship.PairOwner) &&
                    ReferenceEquals(world.Registry.FindOwner(bm), b.Relationship.PairOwner) &&
                    ReferenceEquals(untouchedLedger, AllocationRecord(am)) && SameActorBudget(Actor(before, am), Actor(held, am)),
                    "Failed pair cleanup lost custody or modified the neighbour allocation.");
            }
            finally { b.Relationship.Dismounting -= cleanupFault; cleanupFault = null; }
            Require(b.Relationship.Dismount(CleanupTrigger.Exception).Succeeded, "Pair B cleanup retry did not settle its injected failure.");
            ((JObject)evidence["localFailure"])["afterRetry"] = WorldSnapshot("local-cleanup-retried");
            Pass("C7-combat-local-cleanup-failure");
        }
        private object AllocationRecord(UnitEntityData actor)
        {
            var adapter = typeof(MountedPairTurnParticipant).GetField("movementState", Flags).GetValue(a.Participant);
            return ((IDictionary)adapter.GetType().GetField("allocations", Flags).GetValue(adapter))[actor];
        }
        private JObject WorldSnapshot(string label) => new JObject { ["label"] = label, ["frame"] = Time.frameCount,
            ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks, ["round"] = Game.Instance.TurnBasedCombatController.RoundNumber,
            ["currentActor"] = Game.Instance.TurnBasedCombatController.CurrentTurn?.Unit?.UniqueId,
            ["actors"] = new JArray((actors ?? originalParty ?? new UnitEntityData[0]).Select(actor => new JObject {
                ["id"] = actor.UniqueId, ["position"] = new JObject { ["x"] = actor.Position.x, ["y"] = actor.Position.y, ["z"] = actor.Position.z },
                ["standard"] = actor.CombatState.Cooldown.StandardAction,
                ["move"] = actor.CombatState.Cooldown.MoveAction, ["swift"] = actor.CombatState.Cooldown.SwiftAction,
                ["reactions"] = actor.CombatState.AttackOfOpportunityCount, ["reactionCooldown"] = actor.CombatState.Cooldown.AttackOfOpportunity,
                ["prepared"] = actor.CombatState.Prepared, ["native"] = trace?.Snapshot(actor) })),
            ["owners"] = new JArray(world.Registry.Owners.Select(owner => new JObject { ["id"] = owner.Id.ToString(),
                ["riderId"] = owner.RiderId, ["mountId"] = owner.MountId, ["state"] = owner.State.ToString(),
                ["generation"] = owner.Generation, ["retainsObligations"] = owner.RetainsObligations() })) };
        private static JObject Actor(JObject state, UnitEntityData actor) => (JObject)((JArray)state["actors"]).Single(x => (string)x["id"] == actor.UniqueId);
        private static bool SameActorBudget(JObject left, JObject right) => new[] { "standard", "move", "swift", "reactions", "reactionCooldown" }
            .All(field => JToken.DeepEquals(left[field], right[field]));
        // An actor that never spends inside a window: no cooldown grows, none is negative, the reaction count is
        // unchanged, and any decrease is bounded by the native game time between the two snapshots (TB cooldowns of
        // every unit decay with time while another actor's command runs). A larger drop is a refund, not decay.
        private static bool NeverSpentNatively(JObject beforeWorld, JObject afterWorld, UnitEntityData actor)
        {
            var before = Actor(beforeWorld, actor); var after = Actor(afterWorld, actor);
            var elapsed = ((long)afterWorld["gameTicks"] - (long)beforeWorld["gameTicks"]) / 10000000.0;
            return elapsed >= 0 && (int)after["reactions"] == (int)before["reactions"] &&
                new[] { "standard", "move", "swift", "reactionCooldown" }.All(field =>
                {
                    var was = (float)before[field]; var now = (float)after[field];
                    return now >= 0 && now <= was + .001f && was - now <= elapsed * 1.1 + .001;
                });
        }
        private static bool SameBudgets(JObject before, JObject after) => ((JArray)before["actors"]).Cast<JObject>()
            .All(row => SameActorBudget(row, (JObject)((JArray)after["actors"]).Single(other => (string)other["id"] == (string)row["id"])));
        private bool NeighbourBudgetsUnchanged(JObject before, JObject after) =>
            (actingIndex == 0 ? new[] { br, bm } : new[] { ar, am }).All(actor => SameActorBudget(Actor(before, actor), Actor(after, actor)));
    }
}
