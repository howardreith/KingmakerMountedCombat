using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Equipment;
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
    // 7E row 57 fold (C11 prone stand-up attack of opportunity, owner-seed-native-life-cleanup only). After the
    // unchanged incapacitation measurement (the independent surviving mount's N+1 turn), from round N+2 on:
    // another party actor outside both pairs (one that uses the scroll without Use Magic Device, chosen and given
    // the disposable stack out of combat) heals the unconscious rider B with one native Cure Light Wounds scroll
    // click; at most two attempts, each on the healer's own native turn. Rider B is then conscious and natively
    // prone. On the target's own native turn in between, a target not yet threatening rider B makes one ordinary native
    // ground move toward it (BeginProneTargetTurn); rider B itself never moves before it stands. On rider B's own native
    // turn one ordinary native ground order is its action attempt, the native stand-up
    // runs (it spends the Move and drops the triggering command) and the threatening diagnostic target makes its
    // native attack of opportunity. Nothing is granted, refunded or written; a missing prerequisite refuses as
    // uncovered before the input it would need.
    internal sealed partial class Chunk7CombatScenario
    {
        private bool ProneFoldCase => request.OwnedPairs?.Case == Chunk7OwnedPairsSelection.OwnerSeedNativeLifeCase;
        internal const int ProneHealAttempts = 2;
        private UnitEntityData lifeHealer;
        private FoldedScrollFixture lifeScroll;
        private JObject prone, proneAttempt, proneStand;
        private JArray proneAttempts;
        private UnitUseAbility proneShell;
        private TurnController proneHealTurn;
        private UnitMoveTo proneMove;
        private Chunk7PairRuleObserver proneRules;
        private int proneFrames, proneHealRound;
        private Vector3 proneStart;
        internal const string ProneHealerUncovered = "No party actor outside both pairs can use the native Cure Light Wounds scroll without Use Magic Device; the C11 prone stand-up is uncovered on this fixture.";

        // Out of combat, before the encounter: the healer and its one disposable scroll stack.
        private void PrepareLifeHealer()
        {
            prone = new JObject { ["contract"] = "owner-seed-prone-stand-up-v1", ["riderId"] = br.UniqueId, ["healAttemptsAllowed"] = ProneHealAttempts };
            evidence["proneStandUp"] = prone;
            var scroll = ResourcesLibrary.TryGetBlueprint<BlueprintItemEquipmentUsable>(NativeCastingItemLease.CureScroll);
            Require(scroll != null, "Native Cure Light Wounds scroll blueprint is unavailable.");
            var rows = new JArray(); UnitEntityData chosen = null;
            var pairActors = new[] { ar, am, br, bm };
            // A companion that can never be the unconscious control (essential, immortal or not allowed to be dying) is
            // preferred, so the healer does not take the control's place; party order otherwise.
            foreach (var unit in Game.Instance.Player.PartyCharacters.Select(reference => reference.Value).Where(unit => unit != null && !pairActors.Contains(unit))
                .OrderBy(unit => ControlStaticallyIneligible(unit) ? 0 : 1))
            {
                var state = unit.Descriptor.State; var umd = scroll.IsUnitNeedUMDForUse(unit.Descriptor);
                var eligible = !unit.Descriptor.IsPet && state.IsConscious && unit.IsInGame && unit.IsDirectlyControllable && !umd &&
                    unit.Commands.Empty && !unit.IsInCombat;
                rows.Add(new JObject { ["actorId"] = unit.UniqueId, ["pet"] = unit.Descriptor.IsPet, ["conscious"] = state.IsConscious,
                    ["inGame"] = unit.IsInGame, ["controllable"] = unit.IsDirectlyControllable, ["needsUseMagicDevice"] = umd,
                    ["commandsEmpty"] = unit.Commands.Empty, ["inCombat"] = unit.IsInCombat,
                    ["controlStaticallyIneligible"] = ControlStaticallyIneligible(unit), ["eligible"] = eligible });
                if (eligible && chosen == null) chosen = unit;
            }
            prone["healerCandidates"] = rows;
            Require(chosen != null, ProneHealerUncovered);
            lifeHealer = chosen; prone["healerId"] = chosen.UniqueId;
            var stack = new JObject(); prone["healerScroll"] = stack;
            lifeScroll = AcquireFoldedScroll(chosen, br, stack,
                "The healer cannot use the native Cure Light Wounds scroll; the C11 prone stand-up is uncovered on this fixture.");
        }
        private static bool ControlStaticallyIneligible(UnitEntityData unit) => unit.Descriptor.IsEssentialForGame ||
            (bool)unit.Descriptor.State.Immortality || !(bool)unit.Descriptor.State.AllowDyingCondition;
        private void BeginProneFold()
        {
            Require(prone != null && lifeHealer != null && lifeScroll != null, "The prone stand-up healer was not prepared out of combat.");
            prone["healerAtFold"] = new JObject { ["inCombat"] = lifeHealer.IsInCombat, ["conscious"] = lifeHealer.Descriptor.State.IsConscious,
                ["canAct"] = lifeHealer.Descriptor.State.CanAct };
            Require(lifeHealer.IsInCombat && lifeHealer.Descriptor.State.IsConscious && lifeHealer.Descriptor.State.CanAct,
                "The prone healer is not a conscious acting combatant in the encounter; the C11 prone stand-up is uncovered on this fixture.");
            proneAttempts = new JArray(); prone["healAttempts"] = proneAttempts;
            proneRules = new Chunk7PairRuleObserver(ar, am, br, bm, lifeHealer, target);
            prone["firstRound"] = lifeDamageRound + 2;
        }
        private JObject ProneRider(string label) => new JObject { ["label"] = label, ["round"] = Game.Instance.TurnBasedCombatController.RoundNumber,
            ["frame"] = Time.frameCount, ["life"] = LifeActor(br), ["prone"] = br.Descriptor.State.Prone.Active, ["proneRequested"] = br.Descriptor.State.Prone.ShouldBeActive,
            ["position"] = Point(br.Position), ["targetDistance"] = Flat(br.Position, target.Position), ["commandsEmpty"] = br.Commands.Empty,
            ["engagedByTarget"] = target.CombatState.IsEngage(br), ["remembersTarget"] = br.Memory.Contains(target),
            ["pairState"] = b.Relationship.State.ToString(), ["registryActive"] = world.Registry.FindActive(br) != null,
            ["pairCommandA"] = a.Combat.HasActiveCommand, ["pairCommandB"] = b.Combat.HasActiveCommand };
        private JObject ProneTarget(string label)
        {
            var state = TargetReactionState(label);
            state["engagesSubject"] = target.CombatState.IsEngage(br); state["subjectRemembersTarget"] = br.Memory.Contains(target);
            // The native engagement terms toward rider B (UnitEngagementExtension.IsEngage): the target's group memory of it,
            // the threat hand's weapon reach, both corpulences, the native mechanics distance and line of sight.
            state["targetRemembersSubject"] = target.Memory.Contains(br);
            // The entity reach (ItemEntityWeapon.AttackRange: the wielder's weapon range, ReachRange included) is the one native IsReach reads.
            state["threatReachMeters"] = target.GetThreatHand()?.MaybeWeapon?.AttackRange.Meters;
            state["subjectCorpulence"] = br.View.Corpulence; state["subjectMechanicsDistance"] = target.DistanceTo(br);
            state["subjectLineOfSight"] = target.HasLOS(br);
            return state;
        }
        // 7E row 57 (b207 r57): the healed prone rider B lay outside the target's threatened area (2.06 m, engaged by nobody), so
        // the stand-up had no native attack of opportunity to draw. On the target's own native turn between the heal and rider
        // B's turn, the brain-leased target makes one ordinary native ground move toward rider B (the TB hostile-command precedent:
        // a command for the hostile is issued only on its own turn), natively costed by its TurnController (TickMovement). Rider B
        // never moves before it stands. Units that already engage the target keep reaching its destination (native IsReach with the
        // destination), so no disengagement attack is provoked by design; any attack in the window FAILs. A target that cannot
        // engage rider B refuses as uncovered before the move; a clean move that leaves rider B unthreatened refuses as uncovered.
        private JObject proneTargetTurn;
        private TurnController proneTargetController;
        private UnitMoveTo proneTargetMove;
        private Vector3 proneTargetStart, proneTargetRiderStart;
        private bool proneTargetCaptured;
        private void BeginProneTargetTurn(CombatController controller, TurnController turn)
        {
            var before = ProneTarget("target-turn-before"); var engaged = target.CombatState.IsEngage(br);
            proneTargetTurn = new JObject { ["round"] = controller.RoundNumber, ["targetBefore"] = before, ["engagedBefore"] = engaged, ["approached"] = !engaged,
                ["traceSequenceBefore"] = trace.EventCount };
            prone["targetTurn"] = proneTargetTurn;
            if (engaged) { EndNative(turn); return; }
            var hand = target.GetThreatHand(); var weapon = hand?.MaybeWeapon;
            Require(target.Memory.Contains(br) && weapon != null && target.Descriptor.State.CanAct && target.Commands.Empty &&
                br.Descriptor.State.IsConscious && br.Descriptor.State.Prone.Active,
                "The diagnostic target cannot engage the healed prone rider on its own native turn (group memory, threat hand, ability to act or a pending command); the C11 prone stand-up is uncovered on this fixture: " +
                before.ToString(Newtonsoft.Json.Formatting.None));
            var corpulence = target.View.Corpulence + br.View.Corpulence; var meters = weapon.AttackRange.Meters; var reach = corpulence + meters;
            // The destination band: clear of rider B by 5 cm (no push of the prone rider) and 15 cm inside the native reach; aimed at its middle.
            var near = corpulence + .05f; var far = reach - .15f; var gap = (near + far) / 2f;
            // Distance is the only term a move can change: inside reach and in line of sight, a non-engaging target is not a geometry gap.
            Require(target.DistanceTo(br) >= reach || !target.HasLOS(br),
                "The diagnostic target already reaches the healed prone rider in line of sight without engaging it; no native move can supply the missing engagement term, so the C11 prone stand-up is uncovered on this fixture: " +
                before.ToString(Newtonsoft.Json.Formatting.None));
            Require(far > near, "The diagnostic target's native reach leaves no destination band clear of the healed prone rider; the C11 prone stand-up is uncovered on this fixture: " +
                before.ToString(Newtonsoft.Json.Formatting.None));
            var engagers = target.CombatState.EngagedBy.ToArray();
            proneTargetTurn["geometry"] = new JObject { ["corpulence"] = corpulence, ["weaponReachMeters"] = meters, ["reach"] = reach, ["near"] = near, ["far"] = far, ["gap"] = gap,
                ["engagers"] = new JArray(engagers.Select(unit => unit.UniqueId)) };
            var proposals = new JArray(); proneTargetTurn["proposals"] = proposals; Vector3 destination;
            var found = TryNavmeshDestination(target.Position, br.Position - target.Position, Math.Max(.6f, Flat(target.Position, br.Position) - gap),
                point => Kingmaker.Utility.GeometryUtils.MechanicsDistance(point, br.Position) >= far ? "outside the predicted threat reach" :
                    Flat(point, br.Position) < near ? "too close to rider B" :
                    engagers.Any(unit => unit.GetThreatHand() == null || !unit.IsReach(target, unit.GetThreatHand(), point)) ? "leaves an engaging unit's reach" : null,
                proposals, FullCircleFan, new[] { .8f, 1f }, out destination);
            Require(found, "No walkable native destination puts the diagnostic target's threat on the healed prone rider without leaving an engaging unit's reach; the C11 prone stand-up is uncovered on this fixture.");
            proneTargetTurn["destination"] = Point(destination);
            proneTargetTurn["before"] = WorldSnapshot("prone-target-before"); proneTargetTurn["ruleCountBefore"] = proneRules.EventCount;
            proneTargetTurn["traceSequenceBefore"] = trace.EventCount;
            proneTargetTurn["turnBefore"] = ProneTargetTurnCost(turn);
            proneTargetController = turn; proneTargetStart = target.Position; proneTargetRiderStart = br.Position; proneTargetCaptured = false;
            proneTargetMove = new UnitMoveTo(destination, .1f) { CreatedByPlayer = true };
            target.Commands.Run(proneTargetMove); issuedCommands.Add(proneTargetMove);
            proneTargetTurn["command"] = trace.ObjectIdentity(proneTargetMove);
            proneFrames = 0; LifeStage(16);
        }
        private JObject ProneTargetTurnCost(TurnController turn) => new JObject { ["timeMoved"] = turn.TimeMoved,
            ["metersFiveFootStep"] = turn.MetersMovedByFiveFootStep, ["fiveFootStep"] = turn.EnabledFiveFootStep,
            ["move"] = target.CombatState.Cooldown.MoveAction, ["standard"] = target.CombatState.Cooldown.StandardAction, ["swift"] = target.CombatState.Cooldown.SwiftAction };
        // The brain-leased target is not directly controllable: TurnController.ContinueActing ends its turn natively shortly after it
        // idles (End() then sets its Standard and interrupts commands), so everything is read at the first frame its move has finished
        // and it stands still, from the target's own TurnController object, whether or not its turn has ended natively since.
        private void TickProneTargetApproach(CombatController controller, TurnController turn)
        {
            var record = proneTargetTurn;
            if (!proneTargetCaptured)
            {
                if (!proneTargetMove.IsFinished || target.View?.AgentASP?.IsReallyMoving == true) return;
                proneTargetCaptured = true; proneFrames = 0;
                record["after"] = WorldSnapshot("prone-target-after"); record["turnAfter"] = ProneTargetTurnCost(proneTargetController);
                record["turnEndedNatively"] = !ReferenceEquals(controller.CurrentTurn, proneTargetController);
                record["riderAfter"] = ProneRider("target-turn-after");
                record["rules"] = proneRules.Since((int)record["ruleCountBefore"]); record["traceSequenceAfter"] = trace.EventCount;
                record["displacement"] = Flat(proneTargetStart, target.Position); record["commandResult"] = proneTargetMove.Result.ToString();
                var cost = (JObject)record["turnBefore"]; var spent = (JObject)record["turnAfter"];
                Require(((JArray)record["rules"]).Count == 0 && new[] { ar, am, br, bm }.All(actor => NeverSpentNatively((JObject)record["before"], (JObject)record["after"], actor)),
                    "The diagnostic target's native approach drew an attack or spent another actor's budget: " + record["rules"].ToString(Newtonsoft.Json.Formatting.None));
                Require(Flat(proneTargetRiderStart, br.Position) <= .05f && br.Descriptor.State.IsConscious && br.Descriptor.State.Prone.Active &&
                    b.Relationship.State == RelationshipState.Unmounted, "Rider B moved or left its conscious prone unpaired state during the target's approach.");
                Require(proneTargetMove.Result == UnitCommand.ResultType.Success,
                    "The diagnostic target's native approach did not complete on its own turn (" + record["commandResult"] + "); the C11 prone stand-up is uncovered on this fixture.");
                Require((float)spent["timeMoved"] > (float)cost["timeMoved"] &&
                    ((float)spent["move"] > (float)cost["move"] || (float)spent["metersFiveFootStep"] > (float)cost["metersFiveFootStep"]),
                    "The diagnostic target's approach was not natively costed on its own turn.");
                return;
            }
            // Native engagement ticks every frame: it is read two frames after the move settled.
            if (++proneFrames < 2) return;
            record["targetAfter"] = ProneTarget("target-turn-after"); record["engagedAfter"] = target.CombatState.IsEngage(br);
            Require((bool)record["engagedAfter"],
                "The diagnostic target's native approach did not put the healed prone rider inside its threatened area; the C11 prone stand-up is uncovered on this fixture: " +
                record["targetAfter"].ToString(Newtonsoft.Json.Formatting.None));
            if (ReferenceEquals(controller.CurrentTurn, proneTargetController)) EndNative(proneTargetController);
            LifeStage(14);
        }
        // b208 r57: the native order put rider B's next turn after the heal before the target's (rider A, rider B, target, healer, ...),
        // so the stand-up came before the target could approach. When the target has not acted since the heal and does not engage
        // rider B, rider B's turn is given up with no input through the native End Turn (Game.PauseBind admits it only through
        // TurnController.CanEndTurnAndNoActing and runs ForceToEnd: Standard 6, Move 3, Swift 6, the native forfeit of the turn).
        // No command, movement or attack is issued; rider B stays natively prone into its next turn, where it stands up.
        private JObject proneRiderSkip;
        private TurnController proneSkipController;
        private Vector3 proneSkipStart;
        private JObject ProneRiderTurnCost(TurnController turn) => new JObject { ["actorId"] = turn.Unit?.UniqueId,
            ["currentActor"] = Game.Instance.TurnBasedCombatController.CurrentTurn?.Unit?.UniqueId, ["status"] = turn.Status.ToString(), ["timeMoved"] = turn.TimeMoved,
            ["acted"] = turn.IsActed(), ["standard"] = br.CombatState.Cooldown.StandardAction, ["move"] = br.CombatState.Cooldown.MoveAction,
            ["swift"] = br.CombatState.Cooldown.SwiftAction, ["commandsEmpty"] = br.Commands.Empty };
        private void BeginProneRiderSkip(CombatController controller, TurnController turn)
        {
            var rider = ProneRider("skip-before");
            proneRiderSkip = new JObject { ["round"] = controller.RoundNumber, ["rider"] = rider, ["targetBefore"] = ProneTarget("skip-before"),
                ["before"] = WorldSnapshot("prone-skip-before"), ["ruleCountBefore"] = proneRules.EventCount, ["traceSequenceBefore"] = trace.EventCount,
                ["turnBefore"] = ProneRiderTurnCost(turn) };
            prone["riderSkip"] = proneRiderSkip;
            Require(br.Descriptor.State.IsConscious && br.Descriptor.State.Prone.Active,
                "The healed rider is not natively prone on its own turn; the C11 prone stand-up is uncovered on this fixture: " + rider.ToString(Newtonsoft.Json.Formatting.None));
            Require(!turn.IsActed() && turn.TimeMoved == 0f && br.Commands.Empty && b.Relationship.State == RelationshipState.Unmounted && world.Registry.FindActive(br) == null,
                "Rider B's turn before the skip is not an untouched unpaired native turn.");
            proneSkipController = turn; proneSkipStart = br.Position; LifeStage(17);
        }
        private void TickProneRiderSkip(CombatController controller)
        {
            Require(ReferenceEquals(controller.CurrentTurn, proneSkipController), "Rider B's native turn ended before its native End Turn.");
            if (!EndNative(proneSkipController)) return;
            var record = proneRiderSkip;
            record["turnAfter"] = ProneRiderTurnCost(proneSkipController); record["riderAfter"] = ProneRider("skip-after");
            record["after"] = WorldSnapshot("prone-skip-after"); record["rules"] = proneRules.Since((int)record["ruleCountBefore"]);
            record["traceSequenceAfter"] = trace.EventCount; record["displacement"] = Flat(proneSkipStart, br.Position);
            var cost = (JObject)record["turnAfter"];
            Require(((JArray)record["rules"]).Count == 0 && (float)record["displacement"] <= .05f && br.Commands.Empty && (float)cost["timeMoved"] == 0f &&
                (float)cost["standard"] == 6f && (float)cost["move"] == 3f && (float)cost["swift"] == 6f &&
                br.Descriptor.State.IsConscious && br.Descriptor.State.Prone.Active &&
                new[] { ar, am, bm }.All(actor => NeverSpentNatively((JObject)record["before"], (JObject)record["after"], actor)),
                "Rider B's native End Turn was not a clean forfeit (an attack, a command, movement, a lost prone state or another actor's spend): " +
                cost.ToString(Newtonsoft.Json.Formatting.None));
            LifeStage(14);
        }
        private void TickProneFold(CombatController controller, TurnController turn)
        {
            prone["stage"] = lifeStage;
            Require(controller.RoundNumber <= lifeDamageRound + 6, "The prone stand-up fold exceeded its bounded native rounds.");
            if (lifeStage == 12)
            {
                // Heal attempts run from round N+2 on, on the healer's own native turn, never twice in one turn.
                var ready = controller.RoundNumber >= lifeDamageRound + 2 && turn?.Unit == lifeHealer && !ReferenceEquals(turn, proneHealTurn) &&
                    Chunk7NativeFixture.ReadyForTurnInput(turn);
                if (!ready) { if (turn?.Unit?.Descriptor.State.IsConscious == true && (turn.Unit != lifeHealer || ReferenceEquals(turn, proneHealTurn) || controller.RoundNumber < lifeDamageRound + 2)) EndNative(turn); return; }
                Require(proneAttempts.Count < ProneHealAttempts, "The healer's native scroll did not restore rider B's consciousness in " + ProneHealAttempts + " native attempts.");
                Require(!br.Descriptor.State.IsConscious && !br.Descriptor.State.IsDead && lifeHealer.Descriptor.State.IsConscious,
                    "The prone stand-up heal lost its unconscious living rider or its conscious healer.");
                proneHealTurn = turn;
                proneAttempt = new JObject { ["round"] = controller.RoundNumber, ["riderBefore"] = ProneRider("heal-before"), ["targetBefore"] = ProneTarget("heal-before"),
                    ["healerEngaged"] = target.CombatState.IsEngage(lifeHealer), ["before"] = WorldSnapshot("prone-heal-before"),
                    ["ruleCountBefore"] = proneRules.EventCount, ["traceSequenceBefore"] = trace.EventCount };
                proneAttempts.Add(proneAttempt);
                // The healer is a leased non-pair member whose AI stays off: declare its one native command per attempt to the lease
                // (at most two sequential declarations, same healer only), mirroring the owner-seed companion action.
                Require(targetService.NonPairPartyAiLease.BeginDeclaredMemberCommand(lifeHealer, ProneHealAttempts),
                    "The healer's declared native scroll command was refused by the non-pair AI lease.");
                proneShell = IssueFoldedScrollCast(lifeScroll, br, proneAttempt);
                proneFrames = 0; LifeStage(13); return;
            }
            if (lifeStage == 13)
            {
                if (!FoldedCastSettled(lifeScroll, proneShell, br, ref proneFrames)) return;
                Require(targetService.NonPairPartyAiLease.EndDeclaredMemberCommand(lifeHealer), "The healer's declared scroll command did not settle back into the non-pair AI lease.");
                proneAttempt["declaredLeaseCommands"] = targetService.NonPairPartyAiLease.DeclaredMemberCommandCount;
                FinishFoldedCast(lifeScroll, proneShell, proneAttempt);
                proneAttempt["riderAfter"] = ProneRider("heal-after"); proneAttempt["targetAfter"] = ProneTarget("heal-after");
                proneAttempt["after"] = WorldSnapshot("prone-heal-after"); proneAttempt["rules"] = proneRules.Since((int)proneAttempt["ruleCountBefore"]);
                proneAttempt["traceSequenceAfter"] = trace.EventCount;
                var spends = CastEvents(proneAttempt, "item-spend-after", lifeHealer.UniqueId).Where(e => (int)e["identity"] == (int)proneAttempt["ability"]["sourceItem"]).ToArray();
                var itemsBefore = (JObject)proneAttempt["itemsBefore"]; var itemsAfter = (JObject)proneAttempt["itemsAfter"];
                Require(spends.Length == 1 && (bool?)spends[0]["result"] == true && (int)itemsAfter["count"] == (int)itemsBefore["count"] - 1 && (bool)itemsAfter["exactSlot"],
                    "The healer's scroll did not spend the exact equipped stack exactly once in place.");
                Require(!br.Descriptor.State.IsDead, "Rider B died inside the heal window.");
                proneAttempt["riderConsciousAfter"] = br.Descriptor.State.IsConscious;
                if (!br.Descriptor.State.IsConscious) { LifeStage(12); return; }
                proneHealRound = controller.RoundNumber; prone["healedRound"] = proneHealRound;
                Pass("C7-life-prone-native-heal"); LifeStage(14); return;
            }
            if (lifeStage == 14)
            {
                // Rider B's own native turn after the heal: the native stand-up needs its own action attempt.
                if (ReferenceEquals(turn, proneHealTurn)) { EndNative(turn); return; }
                // One forfeited rider-B turn (below) moves its stand-up one round later; never more.
                Require(controller.RoundNumber <= (proneRiderSkip != null ? (int)proneRiderSkip["round"] + 1 : proneHealRound + 1), "Rider B's native turn after the heal missed its round.");
                // The target's own native turn between the heal and rider B's turn (b207 r57: it did not threaten the rider).
                if (turn?.Unit == target && proneTargetTurn == null && Chunk7NativeFixture.ReadyForTurnInput(turn)) { BeginProneTargetTurn(controller, turn); return; }
                if (turn?.Unit == br && proneTargetTurn == null && proneRiderSkip == null && Chunk7NativeFixture.ReadyForTurnInput(turn) && !target.CombatState.IsEngage(br))
                { BeginProneRiderSkip(controller, turn); return; }
                if (turn?.Unit != br || !Chunk7NativeFixture.ReadyForTurnInput(turn)) { if (turn?.Unit?.Descriptor.State.IsConscious == true && turn.Unit != br) EndNative(turn); return; }
                var rider = ProneRider("stand-precondition"); var gates = ProneTarget("stand-precondition");
                proneStand = new JObject { ["round"] = controller.RoundNumber, ["rider"] = rider, ["gates"] = gates }; prone["standUp"] = proneStand;
                Require(br.Descriptor.State.IsConscious && br.Descriptor.State.Prone.Active,
                    "The healed rider is not natively prone on its own turn; the C11 prone stand-up is uncovered on this fixture: " + rider.ToString(Newtonsoft.Json.Formatting.None));
                Require(target.CombatState.CanAttackOfOpportunity && target.CombatState.AttackOfOpportunityCount == 1 && (bool)gates["canActInCombat"] &&
                    !(bool)gates["waitingInitiative"] && !(bool)gates["surprised"] && (bool)gates["canAct"] && (bool)gates["threatHand"] &&
                    (bool)gates["engagesSubject"] && (bool)gates["subjectRemembersTarget"] && target.Descriptor.State.IsConscious && !target.Descriptor.State.Prone.Active,
                    "The diagnostic target does not threaten the healed rider with one available native attack of opportunity; the C11 prone stand-up is uncovered on this fixture: " +
                    gates.ToString(Newtonsoft.Json.Formatting.None));
                Require(b.Relationship.State == RelationshipState.Unmounted && world.Registry.FindActive(br) == null && !a.Combat.HasActiveCommand && !b.Combat.HasActiveCommand,
                    "Rider B is not an unpaired native actor before its stand-up.");
                // One ordinary native ground order away from the target: the action attempt that stands rider B up.
                var proposals = new JArray();
                var destination = NavmeshDestination(br.Position, br.Position - target.Position, 2f,
                    point => Flat(point, target.Position) > Flat(br.Position, target.Position), proposals);
                proneStand["proposals"] = proposals; proneStand["destination"] = Point(destination);
                proneStand["before"] = WorldSnapshot("prone-stand-before"); proneStand["ruleCountBefore"] = proneRules.EventCount;
                proneStand["traceSequenceBefore"] = trace.EventCount; proneStart = br.Position;
                Chunk7NativeFixture.Select(br); Game.Instance.IsPaused = true;
                bool clicked; var cycles = 0;
                using (var input = new NativeOrdinaryAttackInput(destination))
                {
                    input.Predict();
                    while ((turn.EnabledFiveFootStep || turn.EnabledSingleActionMove) && cycles++ < 4) { input.Click(button: 1); input.Predict(); }
                    proneStand["fiveFootStep"] = turn.EnabledFiveFootStep; proneStand["singleActionMove"] = turn.EnabledSingleActionMove;
                    clicked = input.Click();
                }
                Game.Instance.IsPaused = false;
                proneStand["clicked"] = clicked; proneStand["cursorCycles"] = cycles;
                proneMove = br.Commands.Move as UnitMoveTo; if (proneMove != null) issuedCommands.Add(proneMove);
                proneStand["command"] = trace.ObjectIdentity(proneMove); proneStand["commandCreated"] = proneMove != null;
                Require(clicked && !turn.EnabledFiveFootStep && !turn.EnabledSingleActionMove, "Rider B's ordinary native ground input was not admitted.");
                proneFrames = 0; LifeStage(15); return;
            }
            if (lifeStage == 16) { TickProneTargetApproach(controller, turn); return; }
            if (lifeStage == 17) { TickProneRiderSkip(controller); return; }
            if (lifeStage == 15)
            {
                var settled = (proneMove == null || proneMove.IsFinished) && br.Commands.Empty && target.Commands.Empty && !br.AreHandsBusyWithAnimation &&
                    !target.AreHandsBusyWithAnimation && br.View?.AgentASP?.IsReallyMoving != true;
                if (!settled) { proneFrames = 0; return; }
                if (++proneFrames < 10) return;
                var stand = proneStand;
                stand["after"] = WorldSnapshot("prone-stand-after"); stand["riderAfter"] = ProneRider("stand-after"); stand["targetAfter"] = ProneTarget("stand-after");
                stand["rules"] = proneRules.Since((int)stand["ruleCountBefore"]); stand["traceSequenceAfter"] = trace.EventCount;
                stand["displacement"] = Flat(proneStart, br.Position); stand["commandResult"] = proneMove?.Result.ToString();
                var rules = ((JArray)stand["rules"]).OfType<JObject>().ToArray();
                var before = (JObject)stand["before"]; var after = (JObject)stand["after"];
                var riderBefore = Actor(before, br); var riderAfter = Actor(after, br);
                Require(rules.Length == 1 && (bool)rules[0]["opportunity"] && (string)rules[0]["actor"] == target.UniqueId && (string)rules[0]["target"] == br.UniqueId &&
                    (int)stand["gates"]["reactions"] == 1 && (int)stand["targetAfter"]["reactions"] == 0,
                    "The native stand-up did not draw exactly one attack of opportunity from the threatening target: " + new JArray(rules).ToString(Newtonsoft.Json.Formatting.None));
                // A hit may incapacitate the healed rider again (native damage, recorded); it must then have taken damage.
                var reIncapacitated = !br.Descriptor.State.IsConscious; stand["reIncapacitatedByAttack"] = reIncapacitated;
                Require(!br.Descriptor.State.IsDead && (reIncapacitated ? (int)stand["riderAfter"]["life"]["damage"] > (int)stand["rider"]["life"]["damage"] : !br.Descriptor.State.Prone.Active) &&
                    (float)riderAfter["move"] > (float)riderBefore["move"] &&
                    new[] { "standard", "swift" }.All(f => (float)riderAfter[f] <= (float)riderBefore[f] + .001f) &&
                    (int)stand["riderAfter"]["life"]["damage"] >= (int)stand["rider"]["life"]["damage"],
                    "Rider B's native stand-up did not spend its Move, spent another action, was refunded or healed by the attack.");
                Require(new[] { ar, am, bm }.All(actor => NeverSpentNatively(before, after, actor)) &&
                    b.Relationship.State == RelationshipState.Unmounted && world.Registry.FindActive(br) == null && !a.Combat.HasActiveCommand && !b.Combat.HasActiveCommand,
                    "The stand-up window spent another actor's budget or created pair command authority.");
                var capture = proneRules.Capture();
                Require((int)capture["dropped"] == 0 && ((JArray)capture["errors"]).Count == 0 &&
                    ((JArray)capture["events"]).Count == proneAttempts.OfType<JObject>().Sum(x => ((JArray)x["rules"]).Count) + rules.Length,
                    "Prone stand-up rule observation dropped, failed or recorded attacks outside its windows.");
                prone["rules"] = capture; proneRules.Dispose(); proneRules = null;
                ReleaseFoldedScrollFixtures();
                prone["complete"] = true; Pass("C7-life-prone-stand-up-opportunity");
                nativeLife["complete"] = true; BeginCleanup();
            }
        }
    }
}
