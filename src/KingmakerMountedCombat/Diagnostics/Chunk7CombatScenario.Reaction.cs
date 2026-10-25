using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Controllers.Combat;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // 7E X1 C11 reactions (owner-seed-combat-reaction, on the TB rider-first engine). On rider A's first
    // prepared turn (after one native same-round Delay behind the target if the target is still resolving its
    // native initiative), mounted pair A stands inside the diagnostic target's native threatened area (an
    // ordinary native ground approach first only if it is not) and receives one ordinary native ground
    // order out of it. Stock UnitCombatState.Disengage owns the target's attack of opportunity, its
    // counter and its rolls; the scenario only clicks and observes. Nothing is granted, refunded or
    // written. The prone stand-up half has no deterministic native prone source on this fixture and is
    // recorded as uncovered, never fabricated with a scripted condition.
    internal sealed partial class Chunk7CombatScenario
    {
        private bool ReactionCase => request.OwnedPairs?.Case == Chunk7OwnedPairsSelection.OwnerSeedCombatReactionCase;
        private Chunk7PairRuleObserver reactionRules;
        private JObject reaction, reactionLeg, reactionDelay;
        private UnitMoveTo reactionMove;
        private int reactionStage, reactionRound, reactionSettledFrames, reactionCastFrames;
        // F04 defensive casting folded into this case (round N+1): rider A casts the scroll at mount A from inside
        // the target's threatened area; the native casting-defensively check runs on the shell (a touch cast at
        // another unit acts at about 1.5 s, past the native one-second window).
        private FoldedScrollFixture reactionScroll;
        private JObject reactionDefense, reactionCast;
        private UnitUseAbility reactionCastShell;
        private Vector3 reactionStart;
        internal const string ProneHalfUncovered = "No deterministic native prone source exists for the diagnostic target on the owner-seed fixture. " +
            "The only prone stand-up observed (batch 5/6 slot 1, rt-aoo-diagnosis01, fix 4850445b) was an inferred roll-dependent knockdown " +
            "during concurrent RT primaries; a scripted Prone condition would be fabricated. The pair's own reaction to a native stand-up is uncovered.";

        private static float Flat(Vector3 left, Vector3 right) { var delta = left - right; delta.y = 0f; return delta.magnitude; }
        private static JArray Point(Vector3 value) => new JArray(value.x, value.y, value.z);
        private JObject TargetReactionState(string label) => new JObject { ["label"] = label, ["id"] = target.UniqueId,
            ["round"] = Game.Instance.TurnBasedCombatController.RoundNumber, ["frame"] = Time.frameCount,
            ["reactions"] = target.CombatState.AttackOfOpportunityCount, ["reactionsPerRound"] = target.CombatState.AttackOfOpportunityPerRound,
            ["canAttackOfOpportunity"] = target.CombatState.CanAttackOfOpportunity,
            // The native AttackOfOpportunity gates beyond the counter (IL of 0x060093A1): able to act in combat (not waiting
            // on initiative or surprised), able to act, a threat hand, and the moving unit's group memory of the attacker.
            ["canActInCombat"] = target.CombatState.CanActInCombat, ["waitingInitiative"] = target.CombatState.IsWaitingInitiative,
            ["initiativeCooldown"] = target.CombatState.Cooldown.Initiative, ["surprised"] = Game.Instance.TurnBasedCombatController.IsSurprised(target),
            ["canAct"] = target.Descriptor.State.CanAct, ["threatHand"] = target.GetThreatHand() != null,
            ["riderRemembersTarget"] = ar.Memory.Contains(target), ["mountRemembersTarget"] = am.Memory.Contains(target),
            ["standard"] = target.CombatState.Cooldown.StandardAction, ["move"] = target.CombatState.Cooldown.MoveAction,
            ["swift"] = target.CombatState.Cooldown.SwiftAction, ["prone"] = target.Descriptor.State.Prone.Active,
            ["proneRequested"] = target.Descriptor.State.Prone.ShouldBeActive, ["conscious"] = target.Descriptor.State.IsConscious,
            ["commandsEmpty"] = target.Commands.Empty, ["engagesRider"] = target.CombatState.IsEngage(ar), ["engagesMount"] = target.CombatState.IsEngage(am),
            ["engagedUnits"] = new JArray(target.CombatState.EngagedUnits.Select(unit => unit.UniqueId)),
            ["position"] = Point(target.Position), ["mountDistance"] = Flat(target.Position, am.Position),
            ["corpulence"] = target.View.Corpulence, ["mountCorpulence"] = am.View.Corpulence };
        private JObject PairResidue() => new JObject { ["activeCommand"] = a.Combat.HasActiveCommand, ["groundMovement"] = a.Combat.HasActiveGroundMovement,
            ["riderCommandsEmpty"] = ar.Commands.Empty, ["mountCommandsEmpty"] = am.Commands.Empty, ["state"] = a.Relationship.State.ToString(),
            ["riderConscious"] = ar.Descriptor.State.IsConscious, ["mountConscious"] = am.Descriptor.State.IsConscious,
            ["riderProne"] = ar.Descriptor.State.Prone.Active, ["mountProne"] = am.Descriptor.State.Prone.Active,
            ["riderDamage"] = ar.Damage, ["riderMaxHp"] = ar.MaxHP, ["mountDamage"] = am.Damage, ["mountMaxHp"] = am.MaxHP };
        private static readonly float[] ForwardFan = { 0f, 22.5f, -22.5f, 45f, -45f, 67.5f, -67.5f };
        // The forward fan first, then the rest of the circle (including back toward the start).
        internal static readonly float[] FullCircleFan = ForwardFan.Concat(new[] { 90f, -90f, 112.5f, -112.5f, 135f, -135f, 157.5f, -157.5f, 180f }).ToArray();
        // A walkable native destination whose navmesh trace from the start reaches it (bounded fan of
        // directions, the requested length first, then the extra lengths in their given order). Every proposal is recorded, including on refusal.
        private Vector3 NavmeshDestination(Vector3 start, Vector3 direction, float length, Func<Vector3, bool> accepts, JArray proposals,
            float[] angles = null, float[] shorterLengths = null) =>
            NavmeshDestinationRefusing(start, direction, length, p => accepts(p) ? null : "geometry", proposals, angles, shorterLengths);
        private Vector3 NavmeshDestinationRefusing(Vector3 start, Vector3 direction, float length, Func<Vector3, string> refuses, JArray proposals,
            float[] angles = null, float[] shorterLengths = null)
        {
            Vector3 goal;
            if (TryNavmeshDestination(start, direction, length, refuses, proposals, angles, shorterLengths, out goal)) return goal;
            throw new InvalidOperationException("No walkable native destination satisfied the declared geometry (" + (proposals?.Count ?? 0) + " proposals recorded).");
        }
        // Each proposal records the first term that refused it (Chunk7StagedGeometry.Refusal) and, once the node,
        // trace and length passed, the declared geometry's own refusal (geometryAccepted is now only set there); eligibility is unchanged: walkable, native
        // trace within 0.3 m, a leg over 0.5 m, and the declared geometry.
        private bool TryNavmeshDestination(Vector3 start, Vector3 direction, float length, Func<Vector3, string> refuses, JArray proposals,
            float[] angles, float[] shorterLengths, out Vector3 destination)
        {
            Require(AstarPath.active != null, "Active native navigation graph is unavailable.");
            direction.y = 0f; Require(direction.sqrMagnitude > .0001f, "Native destination direction is unavailable.");
            foreach (var requestedLength in new[] { length }.Concat(shorterLengths ?? new float[0]))
                foreach (var angle in angles ?? ForwardFan)
                {
                    var requested = start + Quaternion.Euler(0f, angle, 0f) * direction.normalized * requestedLength;
                    var nearest = AstarPath.active.GetNearest(requested);
                    var walkable = nearest.node != null && nearest.node.Walkable;
                    var goal = nearest.clampedPosition;
                    var traced = walkable ? Kingmaker.View.ObstacleAnalyzer.TraceAlongNavmesh(start, goal) : start;
                    // The declared geometry (which may run a lookahead) is evaluated only once every cheaper term passed.
                    var refusal = Chunk7StagedGeometry.Refusal(walkable, Flat(traced, goal), Flat(start, goal), null);
                    var geometry = refusal == null ? refuses(goal) : null; if (refusal == null) refusal = geometry;
                    proposals?.Add(new JObject { ["angle"] = angle, ["length"] = requestedLength, ["requested"] = Point(requested), ["goal"] = Point(goal),
                        ["walkable"] = walkable, ["traceResidual"] = Flat(traced, goal), ["geometryAccepted"] = refusal == null,
                        ["geometryRefusal"] = geometry, ["refusal"] = refusal, ["eligible"] = refusal == null });
                    if (refusal == null) { destination = goal; return true; }
                }
            destination = start; return false;
        }
        private void ReactionStage(int value) { reactionStage = value; reactionSettledFrames = 0; stepStarted = clock.Elapsed.TotalSeconds; }
        private void BeginReaction()
        {
            Require(ReactionCase && OwnerSeedDerived && SelectedScenario == RiderFirst, "Reaction admission differs.");
            reactionRules = new Chunk7PairRuleObserver(ar, am, br, bm, target);
            reactionDelay = null;
            reaction = new JObject { ["contract"] = "owner-seed-combat-reaction-v2", ["riderId"] = ar.UniqueId, ["mountId"] = am.UniqueId,
                ["targetId"] = target.UniqueId, ["proneStandUp"] = new JObject { ["covered"] = false, ["reason"] = ProneHalfUncovered } };
            evidence["reaction"] = reaction;
            Require(reactionDefense != null && reactionScroll != null, "The reaction's defensive-cast scroll fixture was not prepared out of combat.");
            reaction["defensiveCast"] = reactionDefense;
            ReactionStage(0);
        }
        // One ordinary native ground order for pair A (the mount executes); five-foot-step and
        // single-action modes are switched off by the native right click, as in the full activations.
        private JObject IssueReactionLeg(string label, Vector3 destination)
        {
            var turn = Game.Instance.TurnBasedCombatController.CurrentTurn;
            Chunk7NativeFixture.Select(ar); Game.Instance.IsPaused = true;
            reactionStart = am.Position;
            var leg = new JObject { ["label"] = label, ["pairBefore"] = PairResidue(), ["round"] = Game.Instance.TurnBasedCombatController.RoundNumber,
                ["destination"] = Point(destination), ["requestedDistance"] = Flat(reactionStart, destination),
                ["before"] = WorldSnapshot(label + "-before"), ["targetBefore"] = TargetReactionState(label + "-before"),
                ["ruleCountBefore"] = reactionRules.EventCount, ["traceSequenceBefore"] = trace.EventCount };
            bool clicked;
            using (var input = new NativeOrdinaryAttackInput(destination))
            {
                input.Predict(); var attempts = 0;
                while ((turn.EnabledFiveFootStep || turn.EnabledSingleActionMove) && attempts++ < 4) { input.Click(button: 1); input.Predict(); }
                leg["fiveFootStep"] = turn.EnabledFiveFootStep; leg["singleActionMove"] = turn.EnabledSingleActionMove;
                clicked = input.Click();
            }
            leg["clicked"] = clicked;
            reactionMove = am.Commands.Move as UnitMoveTo;
            Require(clicked && reactionMove?.Executor == am && !turn.EnabledFiveFootStep && !turn.EnabledSingleActionMove, "Native paired ground input did not admit the exact ordinary mount move.");
            issuedCommands.Add(reactionMove); leg["command"] = trace.ObjectIdentity(reactionMove);
            Chunk7NativeFixture.Select(br); Game.Instance.IsPaused = false;
            reactionLeg = leg; return leg;
        }
        // The move, the pair and the target's own native opportunity command have all ended.
        private bool ReactionLegSettled()
        {
            if (reactionMove == null || !reactionMove.IsFinished || a.Combat.HasActiveCommand || a.Combat.HasActiveGroundMovement ||
                !ar.Commands.Empty || !am.Commands.Empty || !target.Commands.Empty || ar.AreHandsBusyWithAnimation ||
                am.AreHandsBusyWithAnimation || target.AreHandsBusyWithAnimation) { reactionSettledFrames = 0; return false; }
            return ++reactionSettledFrames >= 10;
        }
        private void FinishReactionLeg()
        {
            var leg = reactionLeg; var label = (string)leg["label"];
            leg["after"] = WorldSnapshot(label + "-after"); leg["targetAfter"] = TargetReactionState(label + "-after");
            leg["rules"] = reactionRules.Since((int)leg["ruleCountBefore"]); leg["traceSequenceAfter"] = trace.EventCount;
            leg["distance"] = Flat(reactionStart, am.Position); leg["result"] = reactionMove.Result.ToString();
            leg["residue"] = PairResidue(); leg["turnActor"] = Game.Instance.TurnBasedCombatController.CurrentTurn?.Unit?.UniqueId;
            reactionMove = null;
        }
        private void TickReaction()
        {
            var controller = Game.Instance.TurnBasedCombatController; var turn = controller.CurrentTurn;
            reaction["stage"] = reactionStage;
            if (reactionStage == 0)
            {
                if (EndSurprisedRiderTurn(turn, reaction)) return;
                // Rider A's own turn is never ended here; its partner context may still be binding.
                if (!ReadyPrincipal(a)) { if (turn?.Unit != target && turn?.Unit != ar) EndNative(turn); return; }
                if (reactionDelay != null)
                    Require(controller.RoundNumber == (int)reactionDelay["round"], "Rider A did not resume its delayed native turn in the same round.");
                if (reactionDelay == null && (target.CombatState.IsWaitingInitiative || controller.IsSurprised(target)))
                {
                    // b201 cr: in round 1 the target had not resolved its native initiative (waiting, cooldown 3.0),
                    // so it could not make an attack of opportunity. Rider A's turn is never ended for it: rider A
                    // uses the player's own native Delay behind the target (TurnController.DelayInitiaive, routed
                    // through the product's paired Delay) and resumes in the same round once the target's own
                    // turn has resolved its initiative. One Delay only; the pre-input gate below is unchanged.
                    var units = controller.SortedUnits.ToList();
                    reactionDelay = new JObject { ["round"] = controller.RoundNumber, ["targetBefore"] = TargetReactionState("delay-before"),
                        ["canDelay"] = turn.CanDelay(), ["riderSlot"] = units.IndexOf(ar), ["targetSlot"] = units.IndexOf(target),
                        ["targetWait"] = target.GetTimeToNextTurn(), ["nextRoundWait"] = controller.TimeToNextRound,
                        ["before"] = WorldSnapshot("reaction-delay-before"), ["traceSequenceBefore"] = trace.EventCount };
                    reaction["initiativeDelay"] = reactionDelay;
                    Require(turn.CanDelay() && units.IndexOf(target) > units.IndexOf(ar) && target.GetTimeToNextTurn() < controller.TimeToNextRound,
                        "The diagnostic target is still resolving its native initiative and rider A has no native same-round Delay behind it; " +
                        "C11 disengagement is uncovered on this fixture: " + reactionDelay.ToString(Newtonsoft.Json.Formatting.None));
                    turn.DelayInitiaive(target);
                    reactionDelay["statusAfter"] = turn.Status.ToString(); reactionDelay["suspended"] = !ReferenceEquals(turn, controller.CurrentTurn);
                    Require(turn.Status == TurnController.TurnStatus.Delayed && !ReferenceEquals(turn, controller.CurrentTurn),
                        "Native Delay behind the diagnostic target did not suspend rider A's turn.");
                    return;
                }
                if (reactionDelay != null) { reactionDelay["resumed"] = WorldSnapshot("reaction-delay-resumed"); reactionDelay["traceSequenceResumed"] = trace.EventCount; }
                reactionRound = controller.RoundNumber; reaction["firstRound"] = reactionRound; actingPair = a; actingIndex = 0; actingTurn = turn;
                var state = TargetReactionState("precondition"); reaction["precondition"] = state;
                Require(a.Relationship.State == RelationshipState.Mounted, "Pair A is not mounted at its first prepared turn.");
                Require(target.CombatState.CanAttackOfOpportunity && target.CombatState.AttackOfOpportunityPerRound == 1 &&
                    target.CombatState.AttackOfOpportunityCount == 1 && target.Descriptor.State.IsConscious && !target.Descriptor.State.Prone.Active &&
                    (bool)state["canActInCombat"] && !(bool)state["waitingInitiative"] && !(bool)state["surprised"] && (bool)state["canAct"] &&
                    (bool)state["threatHand"] && (bool)state["riderRemembersTarget"] && (bool)state["mountRemembersTarget"],
                    "The diagnostic target has no single available native attack of opportunity; C11 disengagement is uncovered on this fixture: " +
                    state.ToString(Newtonsoft.Json.Formatting.None));
                if (target.CombatState.IsEngage(am) || target.CombatState.IsEngage(ar)) { ReactionStage(2); return; }
                // Not yet threatened: one ordinary native ground approach to the target's edge.
                var inward = target.Position - am.Position; var reach = am.View.Corpulence + target.View.Corpulence + .4f;
                var proposals = new JArray();
                var approach = NavmeshDestination(am.Position, inward, Math.Max(.6f, Flat(target.Position, am.Position) - reach),
                    point => Flat(point, target.Position) <= reach + .5f, proposals);
                IssueReactionLeg("approach", approach)["proposals"] = proposals; reaction["approach"] = reactionLeg;
                ReactionStage(1); return;
            }
            if (reactionStage == 1)
            {
                if (!ReactionLegSettled()) return;
                FinishReactionLeg();
                Require(ReferenceEquals(turn, actingTurn) && (target.CombatState.IsEngage(am) || target.CombatState.IsEngage(ar)) &&
                    ((JArray)reactionLeg["rules"]).Count == 0,
                    "The native approach did not bring pair A inside the target's threatened area without an attack.");
                ReactionStage(2); return;
            }
            if (reactionStage == 2)
            {
                Require(ReferenceEquals(turn, actingTurn) && ReadyPrincipal(a), "Pair A lost its native turn before the disengagement.");
                // The native gates are read again at the disengagement itself (after any approach): a change is uncovered, not a FAIL.
                var gates = TargetReactionState("disengage-gates"); reaction["disengageGates"] = gates;
                Require(target.CombatState.CanAttackOfOpportunity && target.CombatState.AttackOfOpportunityCount == 1 &&
                    (bool)gates["canActInCombat"] && !(bool)gates["waitingInitiative"] && !(bool)gates["surprised"] && (bool)gates["canAct"] &&
                    (bool)gates["threatHand"] && (bool)gates["riderRemembersTarget"] && (bool)gates["mountRemembersTarget"],
                    "The diagnostic target lost its single available native attack of opportunity before the disengagement; C11 disengagement is uncovered on this fixture: " +
                    gates.ToString(Newtonsoft.Json.Formatting.None));
                var away = am.Position - target.Position;
                var clear = Math.Max(Flat(am.Position, target.Position) + 4.5f, am.View.Corpulence + target.View.Corpulence + 6f);
                var proposals = new JArray();
                var destination = NavmeshDestination(am.Position, away, clear - Flat(am.Position, target.Position),
                    point => Flat(point, target.Position) >= clear - .5f, proposals);
                IssueReactionLeg("disengage", destination)["proposals"] = proposals; reaction["disengage"] = reactionLeg;
                reactionLeg["clearDistance"] = clear;
                ReactionStage(3); return;
            }
            if (reactionStage == 3)
            {
                if (!ReactionLegSettled()) return;
                FinishReactionLeg();
                var leg = reactionLeg; var before = (JObject)leg["before"]; var after = (JObject)leg["after"];
                var targetBefore = (JObject)leg["targetBefore"]; var targetAfter = (JObject)leg["targetAfter"];
                var rules = ((JArray)leg["rules"]).OfType<JObject>().ToArray();
                var opportunity = rules.Where(e => (bool)e["opportunity"] && (string)e["actor"] == target.UniqueId).ToArray();
                var victims = new[] { ar.UniqueId, am.UniqueId };
                Require(ReferenceEquals(turn, actingTurn) && rules.Length == 1 && opportunity.Length == 1 && victims.Contains((string)opportunity[0]["target"]) &&
                    (int)targetBefore["reactions"] == 1 && (int)targetAfter["reactions"] == 0 &&
                    !(bool)targetAfter["engagesRider"] && !(bool)targetAfter["engagesMount"],
                    "Native disengagement did not draw exactly one attack of opportunity from the target's single reaction: " +
                    new JArray(rules).ToString(Newtonsoft.Json.Formatting.None));
                var residue = (JObject)leg["residue"]; var pairBefore = (JObject)leg["pairBefore"];
                // The attack of opportunity may miss: the victim's damage never falls and the other pair actor's is unchanged.
                var victimRider = (string)opportunity[0]["target"] == ar.UniqueId;
                Require((int)residue[victimRider ? "riderDamage" : "mountDamage"] >= (int)pairBefore[victimRider ? "riderDamage" : "mountDamage"] &&
                    (int)residue[victimRider ? "mountDamage" : "riderDamage"] == (int)pairBefore[victimRider ? "mountDamage" : "riderDamage"],
                    "The attack of opportunity changed the other pair actor's damage or healed its victim.");
                Require((float)leg["distance"] > .5f && (float)Actor(after, am)["move"] > (float)Actor(before, am)["move"] &&
                    new[] { "standard", "move", "swift" }.All(f => (float)Actor(after, ar)[f] == (float)Actor(before, ar)[f]) &&
                    (float)Actor(after, am)["standard"] == (float)Actor(before, am)["standard"] &&
                    // Pair B never spends inside the window (a finished turn's cooldowns may only decay natively).
                    new[] { br, bm }.All(actor => NeverSpentNatively(before, after, actor)) &&
                    new[] { "standard", "move", "swift" }.All(f => (float)targetAfter[f] <= (float)targetBefore[f] + .001f) &&
                    !(bool)residue["activeCommand"] && !(bool)residue["groundMovement"] && (string)residue["state"] == "Mounted" &&
                    (bool)residue["riderConscious"] && (bool)residue["mountConscious"] && !(bool)residue["riderProne"] && !(bool)residue["mountProne"] &&
                    // An interrupted move must still have left the threatened area; a completed one reaches its clear distance.
                    ((string)leg["result"] == "Interrupt" || (float)targetAfter["mountDistance"] >= (float)leg["clearDistance"] - .6f),
                    "Disengagement escaped the mount's own native move, refunded it or left pair residue.");
                var capture = reactionRules.Capture();
                var approachRules = (reaction["approach"]?["rules"] as JArray)?.Count ?? 0;
                Require((int)capture["dropped"] == 0 && ((JArray)capture["errors"]).Count == 0 && ((JArray)capture["events"]).Count == approachRules + rules.Length,
                    "Reaction rule observation dropped, failed or recorded attacks outside its windows.");
                reaction["rules"] = capture;
                Pass("C7-combat-reaction-disengage-opportunity");
                ReactionStage(5); return;
            }
            if (reactionStage >= 5) TickReactionDefensiveCast(controller, turn);
        }
        // Out of combat, before the encounter: the one disposable scroll stack on rider A (cast at mount A in N+1).
        private void PrepareReactionCast()
        {
            reactionDefense = new JObject { ["contract"] = "reaction-defensive-cast-v1", ["riderId"] = ar.UniqueId, ["mountId"] = am.UniqueId };
            reactionScroll = AcquireFoldedScroll(ar, am, reactionDefense,
                "Rider A cannot use the native Cure Light Wounds scroll; the F04 defensive casting check is uncovered on this fixture.");
        }
        // Round N+1, rider A's next prepared turn: pair A is (or ordinarily approaches back) inside the target's
        // threatened area, then rider A's one native scroll click on mount A. The native engine decides the
        // casting-defensively check, a failed check's attack of opportunity, any concentration check and the spend.
        private void TickReactionDefensiveCast(CombatController controller, TurnController turn)
        {
            reactionDefense["stage"] = reactionStage;
            if (reactionStage == 5)
            {
                if (ReferenceEquals(turn, actingTurn)) { EndNative(turn); return; }
                Require(controller.RoundNumber <= reactionRound + 1, "Rider A's next native turn for the defensive cast missed its round.");
                var due = controller.RoundNumber == reactionRound + 1;
                if (!due || !ReadyPrincipal(a)) { if (turn?.Unit != target && (turn?.Unit != ar || !due)) EndNative(turn); return; }
                actingTurn = turn; reactionDefense["round"] = controller.RoundNumber;
                var state = TargetReactionState("defensive-precondition"); reactionDefense["precondition"] = state;
                Require(a.Relationship.State == RelationshipState.Mounted && target.CombatState.CanAttackOfOpportunity &&
                    target.CombatState.AttackOfOpportunityCount == 1 && target.Descriptor.State.IsConscious && !target.Descriptor.State.Prone.Active &&
                    (bool)state["canActInCombat"] && !(bool)state["waitingInitiative"] && !(bool)state["surprised"] && (bool)state["canAct"] &&
                    (bool)state["threatHand"],
                    "The diagnostic target holds no single available native attack of opportunity in round N+1; the F04 defensive casting check is uncovered on this fixture: " +
                    state.ToString(Newtonsoft.Json.Formatting.None));
                // The native casting-defensively window keys on the caster: rider A itself must be engaged.
                if (target.CombatState.IsEngage(ar)) { ReactionStage(7); return; }
                var inward = target.Position - am.Position; var reach = am.View.Corpulence + target.View.Corpulence + .4f;
                var proposals = new JArray();
                var approach = NavmeshDestination(am.Position, inward, Math.Max(.6f, Flat(target.Position, am.Position) - reach),
                    point => Flat(point, target.Position) <= reach + .5f, proposals);
                IssueReactionLeg("defensive-approach", approach)["proposals"] = proposals; reactionDefense["approach"] = reactionLeg;
                ReactionStage(6); return;
            }
            if (reactionStage == 6)
            {
                if (!ReactionLegSettled()) return;
                FinishReactionLeg();
                Require(ReferenceEquals(turn, actingTurn) && ((JArray)reactionLeg["rules"]).Count == 0,
                    "The native approach back toward the target drew an attack or left rider A's native turn.");
                Require(target.CombatState.IsEngage(ar),
                    "Rider A is not inside the target's threatened area after the native approach; the F04 defensive casting check is uncovered on this fixture.");
                ReactionStage(7); return;
            }
            if (reactionStage == 7)
            {
                Require(ReferenceEquals(turn, actingTurn) && ReadyPrincipal(a) && target.CombatState.IsEngage(ar),
                    "Pair A lost its native turn or the target's threatened area before the defensive cast.");
                reactionCast = new JObject { ["round"] = controller.RoundNumber, ["before"] = WorldSnapshot("defensive-cast-before"),
                    ["targetBefore"] = TargetReactionState("defensive-cast-before"), ["pairBefore"] = PairResidue(),
                    ["riderEngaged"] = target.CombatState.IsEngage(ar), ["mountEngaged"] = target.CombatState.IsEngage(am),
                    ["ruleCountBefore"] = reactionRules.EventCount, ["traceSequenceBefore"] = trace.EventCount };
                reactionDefense["cast"] = reactionCast;
                reactionCastShell = IssueFoldedScrollCast(reactionScroll, am, reactionCast);
                reactionCastFrames = 0; ReactionStage(8); return;
            }
            if (reactionStage == 8)
            {
                if (!FoldedCastSettled(reactionScroll, reactionCastShell, am, ref reactionCastFrames)) return;
                FinishFoldedCast(reactionScroll, reactionCastShell, reactionCast);
                var cast = reactionCast;
                cast["after"] = WorldSnapshot("defensive-cast-after"); cast["targetAfter"] = TargetReactionState("defensive-cast-after");
                cast["residue"] = PairResidue(); cast["rules"] = reactionRules.Since((int)cast["ruleCountBefore"]);
                cast["traceSequenceAfter"] = trace.EventCount; cast["turnActor"] = controller.CurrentTurn?.Unit?.UniqueId;
                var defensive = CastEvents(cast, "defensive-rule-after", ar.UniqueId);
                var hooks = CastEvents(cast, "defensive-before", ar.UniqueId);
                var spends = CastEvents(cast, "item-spend-after", ar.UniqueId).Where(e => (int)e["identity"] == (int)cast["ability"]["sourceItem"]).ToArray();
                var check = defensive.Length == 1 ? new JObject { ["kind"] = "casting-defensively", ["dc"] = defensive[0]["dc"],
                    ["roll"] = defensive[0]["roll"], ["success"] = defensive[0]["success"] } : null;
                reactionDefense["check"] = check;
                reactionDefense["outcome"] = check == null ? null : (bool)check["success"] ? "success" : "failure";
                // The native window must have opened: one casting-defensively hook and one rule on the engaged shell.
                Require(hooks.Length == 1 && defensive.Length == 1,
                    "The native casting-defensively check did not run exactly once on rider A's engaged scroll cast (hooks " + hooks.Length +
                    ", rules " + defensive.Length + ", shell time " + (string)cast["terminal"]["timeSinceStart"] + ").");
                var rules = ((JArray)cast["rules"]).OfType<JObject>().ToArray();
                var opportunities = rules.Where(e => (bool)e["opportunity"] && (string)e["actor"] == target.UniqueId &&
                    ((string)e["target"] == ar.UniqueId || (string)e["target"] == am.UniqueId)).ToArray();
                var success = (bool)check["success"];
                // Exactly one native outcome of the shell: it acted (one scroll cast and rider A's Standard charged) or a
                // failed concentration check force-finished it before it acted (spent, no cast, no cost).
                var casts = CastEvents(cast, "cast-after", ar.UniqueId).Where(e => (string)e["ability"]?["blueprint"] == StagedClwGuid).ToArray();
                var failedConcentration = CastEvents(cast, "concentration-rule-after", ar.UniqueId).Any(e => (bool?)e["success"] == false);
                var riderStandardBefore = (float)Actor((JObject)cast["before"], ar)["standard"]; var riderStandardAfter = (float)Actor((JObject)cast["after"], ar)["standard"];
                Require(casts.Length == 1 ? !failedConcentration && riderStandardAfter > riderStandardBefore + .001f : casts.Length == 0 && failedConcentration,
                    "The defensive scroll cast neither acted once with rider A's native Standard charged nor was lost to a failed concentration check.");
                var itemsBefore = (JObject)cast["itemsBefore"]; var itemsAfter = (JObject)cast["itemsAfter"];
                Require(spends.Length == 1 && (bool?)spends[0]["result"] == true && (bool)itemsBefore["exactSlot"] && (bool)itemsAfter["exactSlot"] &&
                    (int)itemsBefore["count"] >= 2 && (int)itemsAfter["count"] == (int)itemsBefore["count"] - 1,
                    "The defensive scroll cast did not spend the exact equipped stack exactly once in place.");
                Require(rules.Length == opportunities.Length && (success ? opportunities.Length == 0 : opportunities.Length <= 1) &&
                    (int)cast["targetAfter"]["reactions"] == (int)cast["targetBefore"]["reactions"] - opportunities.Length,
                    "The defensive check's native attack of opportunity differs from its outcome: " + new JArray(rules).ToString(Newtonsoft.Json.Formatting.None));
                var before = (JObject)cast["before"]; var after = (JObject)cast["after"];
                var elapsed = ((long)after["gameTicks"] - (long)before["gameTicks"]) / 10000000.0;
                var residue = (JObject)cast["residue"];
                Require(NeverSpentNatively(before, after, am) && new[] { br, bm }.All(actor => NeverSpentNatively(before, after, actor)) &&
                    new[] { "move", "swift" }.All(f => (float)Actor(after, ar)[f] <= (float)Actor(before, ar)[f] + .001f) &&
                    (float)Actor(after, ar)["standard"] >= (float)Actor(before, ar)["standard"] - elapsed * 1.1 - .001 &&
                    !(bool)residue["activeCommand"] && !(bool)residue["groundMovement"] && (string)residue["state"] == "Mounted",
                    "The defensive cast spent mount A or pair B, refunded rider A, or left pair residue.");
                var capture = reactionRules.Capture();
                var approachRules = (reactionDefense["approach"]?["rules"] as JArray)?.Count ?? 0;
                Require((int)capture["dropped"] == 0 && ((JArray)capture["errors"]).Count == 0 &&
                    ((JArray)capture["events"]).Count == ((JArray)reaction["rules"]["events"]).Count + approachRules + rules.Length,
                    "Reaction rule observation dropped, failed or recorded attacks outside its windows.");
                reactionDefense["rulesCapture"] = capture; reactionRules.Dispose(); reactionRules = null;
                ReleaseFoldedScrollFixtures();
                reactionDefense["complete"] = true; Pass("C7-combat-reaction-defensive-cast");
                reaction["complete"] = true;
                BeginCleanup();
            }
        }
    }
}
