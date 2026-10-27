using System;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Three closed RT cases share native fixture setup and ownership cleanup.
    // No RT case changes initiative, grants a turn, or restores actor resources.
    internal sealed partial class Chunk7CombatScenario
    {
        private Chunk4IncomingRuleObserver realtimeEffects;
        private int realtimeStage;
        private JObject realtimeWindow, realtimeStop, unmountedInput;
        private UnitAttack unmountedAttack;
        private UnitMoveTo realtimeMovement;
        private Vector3 realtimeMoveStart;
        private void BeginRealtimeObservations()
        {
            realtimeEffects = new Chunk4IncomingRuleObserver(ar, am);
            realtimeEffects.BeginCase(request.Scenario);
            evidence["realTime"] = true;
            evidence["stimulusKinds"] = new JArray("native-selected-ability-handler", "native-ground-input", "native-selected-Stop",
                "native-Dismount", "native-ordinary-enemy-click", "one-owned-cleanup-subscriber-fault", "native-spell-slot-if-casting-case");
            evidence["chargeSettingAtAdmission"] = settings.EnableMountedCharge;
            evidence["initiativeInputsChanged"] = false;
            evidence["realtimeWindows"] = new JArray();
        }
        private bool PairIdle(MountedPairServices pair) => pair.Relationship.Rider != null && pair.Relationship.Mount != null &&
            pair.Relationship.Rider.Commands.Empty && pair.Relationship.Mount.Commands.Empty &&
            !pair.Relationship.Rider.AreHandsBusyWithAnimation && !pair.Relationship.Mount.AreHandsBusyWithAnimation &&
            !pair.Combat.HasActiveCommand && !pair.Combat.HasActiveGroundMovement && !pair.Combat.HasChargeOwnership &&
            !controls.HasUnsettledShellFor(pair.Relationship);
        private bool NativeActorReady(UnitEntityData actor) => actor.IsInCombat && actor.CombatState.CanActInCombat &&
            actor.CombatState.Prepared && actor.Commands.Empty && !actor.AreHandsBusyWithAnimation &&
            actor.CombatState.Cooldown.StandardAction <= .001f && actor.CombatState.Cooldown.MoveAction <= .001f &&
            !Game.Instance.HandsEquipmentController.IsUpdateScheduledFor(actor);
        private void NextRealtime(int value) { realtimeStage = value; stepStarted = clock.Elapsed.TotalSeconds; }
        private void TickRealtime()
        {
            Require(RealtimeScenario && !CombatController.IsInTurnBasedCombat(), "RT case entered unsupported mode.");
            if (!Game.Instance.Player.IsInCombat || actors.Any(x => !x.IsInCombat)) return;
            if (SelectedScenario == RealtimeCasting) { TickRealtimeCasting(); return; }
            if (SelectedScenario == RealtimeCharge) { TickRealtimeCharge(); return; }
            switch (realtimeStage)
            {
                case 0:
                    if (!PairIdle(a) || !PairIdle(b) || !actors.All(NativeActorReady)) return;
                    ProveDefaultOffCharge();
                    realtimeWindow = new JObject { ["kind"] = "two-native-primary-inputs", ["before"] = WorldSnapshot("rt-concurrent-before") };
                    ((JArray)evidence["realtimeWindows"]).Add(realtimeWindow);
                    actingPair = a; actingIndex = 0; BeginAttack(false, "rt-core-a-rider-primary"); realtimeA = CaptureRealtimeAttack(ar);
                    actingPair = b; actingIndex = 1; BeginAttack(true, "rt-core-b-mount-primary"); realtimeB = CaptureRealtimeAttack(bm);
                    realtimeWindow["admitted"] = WorldSnapshot("rt-concurrent-admitted");
                    realtimeWindow["a"] = realtimeA.Observation; realtimeWindow["b"] = realtimeB.Observation;
                    realtimeWindow["admittedShells"] = new JArray(ObserveNativeShell(realtimeA.Shell), ObserveNativeShell(realtimeB.Shell));
                    Require((int)realtimeA.Observation["before"]["frame"] == (int)realtimeB.Observation["before"]["frame"] &&
                        !realtimeA.Shell.IsFinished && !realtimeB.Shell.IsFinished && !ReferenceEquals(a, b), "Two RT inputs did not overlap exact independent native shells.");
                    NextRealtime(1); break;
                case 1:
                    var aDone = RealtimeAttackFinished(realtimeA); var bDone = RealtimeAttackFinished(realtimeB);
                    if (!aDone || !bDone) return;
                    // Assigning an already-parented JObject stores a copy, so the window kept the admission-time
                    // records; store the settled ones (outcome and after) explicitly.
                    realtimeWindow["a"] = realtimeA.Observation.DeepClone(); realtimeWindow["b"] = realtimeB.Observation.DeepClone();
                    realtimeWindow["after"] = WorldSnapshot("rt-concurrent-after"); Pass("C7-rt-concurrent-primary");
                    // Owner-seed-derived scenario 3: one ordinary native action by the unrelated companion.
                    NextRealtime(OwnerSeedDerived ? 10 : 2); break;
                case 10:
                    if (!CompanionActionReady()) return;
                    BeginCompanionAction(); NextRealtime(11); break;
                case 11:
                    if (!FinishCompanionAction()) return;
                    Pass("C7-rt-companion-native-action"); NextRealtime(12); break;
                case 12:
                    // Directive 4 R1: one real ordinary player hostile click while native proposals continue.
                    if (!PlayerClickReady()) return;
                    BeginPlayerClick(); NextRealtime(13); break;
                case 13:
                    if (!FinishPlayerClick()) return;
                    Pass("C7-rt-player-click-integrity"); NextRealtime(2); break;
                case 2:
                    if (!PairIdle(a) || !PairIdle(b) || !actors.All(NativeActorReady)) return;
                    realtimeWindow = new JObject { ["kind"] = "native-stop-local", ["before"] = WorldSnapshot("rt-local-stop-before") };
                    ((JArray)evidence["realtimeWindows"]).Add(realtimeWindow);
                    realtimeMoveStart = am.Position;
                    realtimeMovement = IssueRealtimeMove(a, am.Position + Vector3.left * 5f, "rt-a-local-move", realtimeWindow);
                    actingPair = b; actingIndex = 1; BeginAttack(true, "rt-core-neighbour-during-stop"); realtimeB = CaptureRealtimeAttack(bm);
                    realtimeWindow["neighbour"] = realtimeB.Observation; NextRealtime(3); break;
                case 3:
                    if (am.View?.MovementAgent.IsReallyMoving != true || Vector3.Distance(realtimeMoveStart, am.Position) <= .02f) return;
                    Require(!realtimeB.Shell.IsFinished || b.Combat.HasActiveCommand, "Native A movement did not overlap B execution before local Stop.");
                    Game.Instance.IsPaused = true;
                    realtimeStop = new JObject { ["before"] = WorldSnapshot("rt-stop-before"), ["neighbourCommandsBefore"] = NativeCommands(b),
                        ["selectedId"] = ar.UniqueId, ["neighbourOwnerBefore"] = b.Relationship.PairOwner?.Id.ToString(), ["neighbourGenerationBefore"] = b.Relationship.MountedPairGeneration };
                    Chunk7NativeFixture.Select(ar); SelectionManager.Instance.Stop();
                    realtimeStop["after"] = WorldSnapshot("rt-stop-after"); realtimeStop["neighbourCommandsAfter"] = NativeCommands(b);
                    realtimeStop["neighbourOwnerAfter"] = b.Relationship.PairOwner?.Id.ToString(); realtimeStop["neighbourGenerationAfter"] = b.Relationship.MountedPairGeneration;
                    Require(JToken.DeepEquals(realtimeStop["neighbourCommandsBefore"], realtimeStop["neighbourCommandsAfter"]) &&
                        JToken.DeepEquals(realtimeStop["neighbourOwnerBefore"], realtimeStop["neighbourOwnerAfter"]) &&
                        JToken.DeepEquals(realtimeStop["neighbourGenerationBefore"], realtimeStop["neighbourGenerationAfter"]) &&
                        new[] { br, bm }.All(x => SameActorBudget(Actor((JObject)realtimeStop["before"], x), Actor((JObject)realtimeStop["after"], x))),
                        "Native Stop of A mutated pair B command, generation, ownership or expenditure in the same paused callback.");
                    realtimeWindow["stop"] = realtimeStop; Game.Instance.IsPaused = false; NextRealtime(4); break;
                case 4:
                    if (!realtimeMovement.IsFinished || !PairIdle(a) || !RealtimeAttackFinished(realtimeB)) return;
                    realtimeWindow["neighbour"] = realtimeB.Observation.DeepClone(); realtimeWindow["after"] = WorldSnapshot("rt-local-stop-settled");
                    Require(a.Relationship.State == RelationshipState.Mounted && b.Relationship.State == RelationshipState.Mounted,
                        "Native local Stop changed mounted ownership.");
                    Pass("C7-rt-local-cancel"); BeginRelationship(ar, ar, controls.DismountAbility, br, "rt-native-dismount-a"); NextRealtime(5); break;
                case 5:
                    if (a.Relationship.State != RelationshipState.Unmounted || !RelationshipSettled() || !NativeActorReady(ar) || !PairIdle(b)) return;
                    Require(world.Registry.FindActive(ar) == null && b.Relationship.State == RelationshipState.Mounted,
                        "Independent native Dismount did not leave the neighbour mounted.");
                    BeginUnmountedNativeControl(); NextRealtime(6); break;
                case 6:
                    if (!FinishUnmountedNativeControl()) return;
                    NextRealtime(7); break;
                case 7:
                    if (!unmountedAttack.IsFinished || !ar.Commands.Empty || !PairIdle(b)) return;
                    unmountedInput["terminal"] = true;
                    Pass("C7-rt-unmounted-native-control");
                    ExerciseLocalCleanupFailure(); evidence["selectedRealtimeCaseComplete"] = true; BeginCleanup(); break;
            }
        }
        private void ProveDefaultOffCharge()
        {
            Require(!settings.EnableMountedCharge, "Default-off proof entered with Charge enabled.");
            var rows = new JArray(); evidence["defaultOffCharge"] = rows;
            Game.Instance.IsPaused = true;
            foreach (var pair in new[] { a, b })
            {
                var rider = pair.Relationship.Rider; Chunk7NativeFixture.Select(rider); controls.Update();
                var before = WorldSnapshot("rt-default-off-before"); var available = controls.Evaluate(NativeMountedControlKind.MountedCharge, rider);
                var data = rider.Descriptor.Abilities.GetAbility(controls.MountedChargeAbility)?.Data;
                var casts = controls.NativeCastRequestCount; var refused = controls.NativeRefusalCount;
                var facts = rider.Descriptor.Abilities.Enumerable.Select(fact => fact.Blueprint.AssetGuid).OrderBy(id => id).ToArray();
                // Default-off withholds this exact native fact, so there is no legal
                // selected-ability click. Do not grant a diagnostic fact to fake one.
                var after = WorldSnapshot("rt-default-off-after");
                rows.Add(new JObject { ["casterId"] = rider.UniqueId, ["featureEnabled"] = false, ["available"] = available.IsEnabled,
                    ["visible"] = available.IsVisible, ["nativeAbilityFacts"] = new JArray(facts), ["nativeFactPresent"] = data != null,
                    ["stimulus"] = "read-only native ability inventory and availability; default-off fact is not leased",
                    ["reason"] = available.Reason, ["clicked"] = false, ["inputAttempted"] = false,
                    ["castDelta"] = controls.NativeCastRequestCount - casts, ["refusalDelta"] = controls.NativeRefusalCount - refused,
                    ["before"] = before, ["after"] = after });
                Require(data == null && !available.IsVisible && !available.IsEnabled && available.Reason == MountedCombatController.MountedChargeDisabledReason &&
                    facts.Contains(NativeMountedControlService.DismountAbilityGuid) && !facts.Contains(NativeMountedControlService.MountedChargeAbilityGuid) &&
                    controls.NativeCastRequestCount == casts && !pair.Combat.HasChargeOwnership && SameBudgets(before, after) &&
                    !rider.Commands.Raw.OfType<UnitUseAbility>().Any(c => c.Spell?.Blueprint == controls.MountedChargeAbility),
                    "Default-off Charge remained leased, visible, admitted work or spent actor resources.");
            }
            Game.Instance.IsPaused = false; Pass("C7-rt-charge-default-off");
        }
        private UnitMoveTo IssueRealtimeMove(MountedPairServices pair, Vector3 destination, string label, JObject window)
        {
            Game.Instance.IsPaused = true; var rider = pair.Relationship.Rider; var mount = pair.Relationship.Mount;
            Chunk7NativeFixture.Select(rider); Game.Instance.DefaultPointerController.ClearPointerMode();
            var row = new JObject { ["label"] = label, ["casterId"] = rider.UniqueId, ["executorId"] = mount.UniqueId,
                ["before"] = WorldSnapshot(label + "-before"), ["destination"] = new JArray(destination.x, destination.y, destination.z) };
            using (var input = new NativeOrdinaryAttackInput(destination)) { input.Predict(); row["clicked"] = input.Click(); }
            var command = mount.Commands.Move as UnitMoveTo;
            Require((bool)row["clicked"] && command?.Executor == mount, "Native RT ground click did not create exact mount movement.");
            row["command"] = trace.ObjectIdentity(command); window["movement"] = row; issuedCommands.Add(command);
            Chunk7NativeFixture.Select(ReferenceEquals(pair, a) ? br : ar); row["selectionAfterInput"] = SelectionManager.Instance.SelectedUnits.Single().UniqueId;
            Game.Instance.IsPaused = false; return command;
        }
        private JObject ObserveNativeShell(UnitUseAbility command) => new JObject { ["identity"] = trace.ObjectIdentity(command),
            ["actorId"] = command.Executor?.UniqueId, ["targetId"] = command.Target?.Unit?.UniqueId,
            ["type"] = command.GetType().FullName, ["abilityGuid"] = command.Spell?.Blueprint?.AssetGuid,
            ["started"] = command.IsStarted, ["acted"] = command.IsActed, ["finished"] = command.IsFinished };
        private JArray NativeCommands(MountedPairServices pair) => new JArray(new[] { pair.Relationship.Rider, pair.Relationship.Mount }
            .Where(x => x != null).SelectMany(x => x.Commands.Raw.Concat(x.Commands.Queue)).Where(command => command != null).Distinct().Select(command => new JObject {
                ["identity"] = trace.ObjectIdentity(command), ["actorId"] = command.Executor?.UniqueId,
                ["type"] = command.GetType().FullName, ["finished"] = command.IsFinished }));
        private void BeginUnmountedNativeControl()
        {
            Game.Instance.IsPaused = true; Chunk7NativeFixture.Select(ar); Game.Instance.DefaultPointerController.ClearPointerMode();
            Require(targetService.BeginExpectedAttackDispatch(target), "Unmounted native target authorization refused.");
            unmountedInput = new JObject { ["before"] = WorldSnapshot("unmounted-input-before"), ["actorId"] = ar.UniqueId,
                ["neighbourOwner"] = b.Relationship.PairOwner.Id.ToString(), ["effectsBefore"] = realtimeEffects.Capture() };
            evidence["unmountedControl"] = unmountedInput;
            using (var input = new NativeOrdinaryAttackInput(target)) unmountedInput["clicked"] = input.Click();
            unmountedAttack = ar.Commands.Raw.OfType<UnitAttack>().SingleOrDefault(c => c.Target == target);
            Require((bool)unmountedInput["clicked"] && unmountedAttack?.Executor == ar && !(unmountedAttack is MountedPairAttackCommand),
                "Ordinary unmounted enemy click did not retain the stock native command.");
            issuedCommands.Add(unmountedAttack); unmountedInput["command"] = trace.ObjectIdentity(unmountedAttack);
            unmountedInput["commandType"] = unmountedAttack.GetType().FullName;
            Chunk7NativeFixture.Select(br); unmountedInput["selectionAfterInput"] = br.UniqueId; Game.Instance.IsPaused = false;
        }
        private bool FinishUnmountedNativeControl()
        {
            var effects = realtimeEffects.Capture(); var offset = ((JArray)unmountedInput["effectsBefore"]["events"]).Count;
            var resolved = ((JArray)effects["events"]).Skip(offset).Count(x => (string)x["kind"] == "weapon-resolved" &&
                (string)x["actor"] == ar.UniqueId && (string)x["target"] == target.UniqueId);
            if (resolved == 0) return false;
            Require(resolved == 1 && b.Relationship.State == RelationshipState.Mounted &&
                b.Relationship.PairOwner.Id.ToString() == (string)unmountedInput["neighbourOwner"],
                "Unmounted native attack repeated or changed the independent neighbour owner.");
            Game.Instance.IsPaused = true; Chunk7NativeFixture.Select(ar); SelectionManager.Instance.Stop(); Game.Instance.IsPaused = false;
            unmountedInput["effectsAfter"] = effects; unmountedInput["after"] = WorldSnapshot("unmounted-control-after");
            unmountedInput["nativeStopSent"] = true; return true;
        }
    }
}
