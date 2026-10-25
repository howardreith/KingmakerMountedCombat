using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Commands;
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
    // prone; on its own native turn one ordinary native ground order is its action attempt, the native stand-up
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
            return state;
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
                Require(controller.RoundNumber <= proneHealRound + 1, "Rider B's native turn after the heal missed its round.");
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
