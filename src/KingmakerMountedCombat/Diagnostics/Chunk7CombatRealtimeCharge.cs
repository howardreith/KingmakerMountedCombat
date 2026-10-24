using System;
using System.Linq;
using Kingmaker;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Commands;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7CombatScenario
    {
        private JObject realtimeCharge;
        private UnitUseAbility realtimeChargeShell;
        private MountedPairAttackCommand realtimeChargeCommand;
        private MountedPairAttackOutcome realtimeChargePriorOutcome;
        private UnitMoveTo realtimeNeighbourMove, realtimeRetreat;
        private Vector3 realtimeChargeOrigin;
        private void TickRealtimeCharge()
        {
            switch (realtimeStage)
            {
                case 0:
                    if (!PairIdle(a) || !PairIdle(b) || !actors.All(NativeActorReady)) return;
                    realtimeChargeOrigin = am.Position;
                    AdmitChargeWeapon();
                    BeginRealtimeCharge(false); NextRealtime(1); break;
                case 1:
                    ObserveRealtimeCharge();
                    if (!ChargeTerminal() || !realtimeNeighbourMove.IsFinished || !PairIdle(b)) return;
                    var outcome = a.Combat.LastOutcome;
                    Require(!ReferenceEquals(outcome, realtimeChargePriorOutcome) && outcome.Result == "Success" &&
                        outcome.ActorId == ar.UniqueId && outcome.ResourceOwnerId == ar.UniqueId && outcome.TargetId == target.UniqueId &&
                        outcome.NativeAttackRuleObserved && outcome.NativeCompletedAttackCount == 1,
                        "Opt-in native Charge did not complete one exact rider-owned attack.");
                    realtimeCharge["outcome"] = new JObject { ["actorId"] = outcome.ActorId, ["resourceOwnerId"] = outcome.ResourceOwnerId,
                        ["targetId"] = outcome.TargetId, ["result"] = outcome.Result, ["nativeAttackRule"] = outcome.NativeAttackRuleObserved,
                        ["completedAttacks"] = outcome.NativeCompletedAttackCount, ["singleAttack"] = outcome.SingleAttackMode };
                    FinishRealtimeCharge(); Pass("C7-rt-charge-native-success"); Pass("C7-rt-charge-neighbour-movement"); NextRealtime(2); break;
                case 2:
                    if (!PairIdle(a) || !PairIdle(b) || !actors.All(NativeActorReady)) return;
                    var retreat = new JObject(); evidence["chargeRetreat"] = retreat;
                    realtimeRetreat = IssueRealtimeMove(a, realtimeChargeOrigin, "rt-charge-native-retreat", retreat); NextRealtime(3); break;
                case 3:
                    if (!realtimeRetreat.IsFinished || !PairIdle(a) || !NativeActorReady(ar)) return;
                    ((JObject)evidence["chargeRetreat"])["after"] = WorldSnapshot("rt-charge-retreat-after");
                    BeginRealtimeCharge(true); NextRealtime(4); break;
                case 4:
                    ObserveRealtimeCharge();
                    if (realtimeChargeShell?.IsActed != true || realtimeChargeCommand == null || realtimeChargeCommand.IsFinished ||
                        am.View?.AgentASP?.IsCharging != true || am.View.AgentASP.IsReallyMoving != true ||
                        Vector3.Distance(PositionOf((JObject)realtimeCharge["before"], am), am.Position) <= .02f) return;
                    Require(ChargeWindowAttackCount() == 0, "Charge cancellation window already delivered a native rider attack.");
                    Require(!realtimeNeighbourMove.IsFinished, "Native moving Charge no longer overlaps the neighbour command at cancellation.");
                    Game.Instance.IsPaused = true;
                    var stop = new JObject { ["before"] = WorldSnapshot("rt-charge-stop-before"), ["neighbourCommandsBefore"] = NativeCommands(b),
                        ["neighbourOwnerBefore"] = b.Relationship.PairOwner.Id.ToString(), ["neighbourGenerationBefore"] = b.Relationship.MountedPairGeneration,
                        ["charge"] = ChargeObservation() };
                    Chunk7NativeFixture.Select(ar); SelectionManager.Instance.Stop();
                    stop["after"] = WorldSnapshot("rt-charge-stop-after"); stop["neighbourCommandsAfter"] = NativeCommands(b);
                    stop["neighbourOwnerAfter"] = b.Relationship.PairOwner.Id.ToString(); stop["neighbourGenerationAfter"] = b.Relationship.MountedPairGeneration;
                    Require(JToken.DeepEquals(stop["neighbourCommandsBefore"], stop["neighbourCommandsAfter"]) &&
                        JToken.DeepEquals(stop["neighbourOwnerBefore"], stop["neighbourOwnerAfter"]) &&
                        JToken.DeepEquals(stop["neighbourGenerationBefore"], stop["neighbourGenerationAfter"]) &&
                        new[] { br, bm }.All(x => SameActorBudget(Actor((JObject)stop["before"], x), Actor((JObject)stop["after"], x))),
                        "Native Charge Stop escaped exact pair custody into its neighbour.");
                    realtimeCharge["stop"] = stop; Game.Instance.IsPaused = false; NextRealtime(5); break;
                case 5:
                    ObserveRealtimeCharge();
                    if (!ChargeTerminal() || !realtimeNeighbourMove.IsFinished || !PairIdle(b)) return;
                    Require(ChargeWindowAttackCount() == 0, "Cancelled Charge delivered or replayed a rider attack.");
                    FinishRealtimeCharge();
                    var weapon = (JObject)evidence["chargeWeapon"]; weapon["after"] = ChargeWeaponState();
                    Require(JToken.DeepEquals(weapon["before"], weapon["after"]), "Rider A's equipped hand set or weapon changed during the Charge case.");
                    Pass("C7-rt-charge-native-cancel");
                    evidence["selectedRealtimeCaseComplete"] = true; BeginCleanup(); break;
            }
        }
        // Rider A's own equipped hand set and primary weapon, observed only; the scenario never switches or equips.
        private JObject ChargeWeaponState()
        {
            var primary = ar.Body.PrimaryHand.MaybeWeapon; var first = ar.GetFirstWeapon();
            return new JObject { ["handSet"] = ar.Body.CurrentHandEquipmentSetIndex,
                ["primaryItem"] = primary == null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(primary),
                ["primaryBlueprint"] = primary?.Blueprint.AssetGuid, ["category"] = primary?.Blueprint.Category.ToString(),
                ["melee"] = primary?.Blueprint.IsMelee == true, ["ranged"] = primary?.Blueprint.IsRanged == true,
                ["firstWeaponIsPrimary"] = primary != null && ReferenceEquals(first, primary) };
        }
        // Mounted Charge needs a melee weapon: rider A must wield one as saved. On the owner-seed Charge lineage that is
        // exactly the seed's saved active hand set and its primary weapon; anything else is refused before input.
        private void AdmitChargeWeapon()
        {
            var before = ChargeWeaponState(); evidence["chargeWeapon"] = new JObject { ["before"] = before };
            var ownerSeed = request.OwnedPairs?.Case == Chunk7OwnedPairsSelection.OwnerSeedRealtimeChargeCase;
            Require((bool)before["melee"] && !(bool)before["ranged"] && (bool)before["firstWeaponIsPrimary"] &&
                (!ownerSeed || (int)before["handSet"] == Chunk7OwnedPairsSelection.ChargeSeedHandSetIndex &&
                    (string)before["primaryBlueprint"] == Chunk7OwnedPairsSelection.ChargeSeedPrimaryWeapon),
                "Rider A does not wield its saved melee weapon; RT Charge is uncovered on this source: " + before.ToString(Newtonsoft.Json.Formatting.None));
        }
        private static Vector3 PositionOf(JObject snapshot, Kingmaker.EntitySystem.Entities.UnitEntityData actor)
        {
            var value = Actor(snapshot, actor)["position"];
            return new Vector3((float)value["x"], (float)value["y"], (float)value["z"]);
        }
        private void BeginRealtimeCharge(bool cancel)
        {
            Game.Instance.IsPaused = true; Chunk7NativeFixture.Select(ar); controls.Update();
            var ability = ar.Descriptor.Abilities.GetAbility(controls.MountedChargeAbility)?.Data;
            var availability = a.Combat.ObserveMountedChargeTarget(ar, target);
            realtimeCharge = new JObject { ["kind"] = cancel ? "charge-cancel" : "charge-success", ["before"] = WorldSnapshot("rt-charge-before"),
                ["featureEnabled"] = settings.EnableMountedCharge, ["available"] = ability?.IsAvailableForCast,
                ["targetAllowed"] = availability.IsAllowed, ["reason"] = availability.Reason,
                ["straightRoute"] = MountedChargeGeometry.StraightRoute(am, target), ["landingBlocked"] = MountedChargeGeometry.LandingBlocked(am, ar, target),
                ["effectsBefore"] = realtimeEffects.Capture(), ["neighbourOwner"] = b.Relationship.PairOwner.Id.ToString() };
            ((JArray)evidence["realtimeWindows"]).Add(realtimeCharge);
            Require(settings.EnableMountedCharge && ability?.IsAvailableForCast == true && ability.CanTarget(target) && availability.IsAllowed &&
                (bool)realtimeCharge["straightRoute"] && !(bool)realtimeCharge["landingBlocked"] &&
                targetService.BeginExpectedAttackDispatch(target), "Concrete native Charge fixture cannot establish supported availability and geometry: " + realtimeCharge);
            var casts = controls.NativeCastRequestCount; var refused = controls.NativeRefusalCount;
            var handler = Game.Instance.SelectedAbilityHandler; realtimeChargePriorOutcome = a.Combat.LastOutcome;
            handler.SetAbility(ability); bool clicked;
            try { clicked = handler.OnClick(target.View.gameObject, target.Position, 0, false, false); }
            finally { handler.DropAbility(); }
            realtimeChargeShell = a.Combat.OwnedChargeShell;
            Require(clicked && controls.NativeCastRequestCount == casts + 1 && controls.NativeRefusalCount == refused &&
                realtimeChargeShell?.Executor == ar && realtimeChargeShell.Target?.Unit == target &&
                realtimeChargeShell.Spell?.Blueprint == controls.MountedChargeAbility,
                "Native opt-in Charge click did not admit the exact rider/target shell.");
            issuedCommands.Add(realtimeChargeShell); realtimeChargeCommand = null;
            realtimeCharge["clicked"] = clicked; realtimeCharge["castDelta"] = controls.NativeCastRequestCount - casts;
            realtimeCharge["refusalDelta"] = controls.NativeRefusalCount - refused;
            realtimeCharge["shell"] = trace.ObjectIdentity(realtimeChargeShell); realtimeCharge["casterId"] = ar.UniqueId;
            realtimeCharge["targetId"] = target.UniqueId; realtimeCharge["abilityGuid"] = ability.Blueprint.AssetGuid;
            realtimeNeighbourMove = IssueRealtimeMove(b, bm.Position + Vector3.right * 5f, "rt-charge-neighbour-ground", realtimeCharge);
            Chunk7NativeFixture.Select(br); realtimeCharge["selectionAfterInput"] = br.UniqueId;
            realtimeCharge["admitted"] = WorldSnapshot("rt-charge-and-neighbour-admitted");
            realtimeCharge["admittedShell"] = ObserveNativeShell(realtimeChargeShell);
            realtimeCharge["admittedNeighbourCommands"] = NativeCommands(b); Game.Instance.IsPaused = false;
        }
        private void ObserveRealtimeCharge()
        {
            if (realtimeChargeCommand == null) realtimeChargeCommand = a.Combat.OwnedChargeCommand;
            if (realtimeChargeCommand != null && realtimeCharge["command"] == null)
                realtimeCharge["command"] = trace.ObjectIdentity(realtimeChargeCommand);
            if (realtimeChargeShell.IsActed && realtimeChargeCommand != null && realtimeCharge["committed"] == null)
                realtimeCharge["committed"] = ChargeObservation();
        }
        private JObject ChargeObservation() => new JObject { ["world"] = WorldSnapshot("rt-charge-observation"),
            ["owner"] = a.Combat.CaptureChargeOwnership(), ["lastDrained"] = a.Combat.LastDrainedChargeOwnership?.DeepClone(),
            ["lease"] = realtimeChargeCommand?.CaptureChargeLeaseEvidence(), ["shellActed"] = realtimeChargeShell?.IsActed,
            ["shellFinished"] = realtimeChargeShell?.IsFinished,
            ["executionEnded"] = realtimeChargeShell?.ExecutionProcess == null || realtimeChargeShell.ExecutionProcess.IsEnded,
            ["commandFinished"] = realtimeChargeCommand?.IsFinished, ["mountCharging"] = am.View?.AgentASP?.IsCharging,
            ["mountMoving"] = am.View?.AgentASP?.IsReallyMoving, ["nativeBoundaries"] = a.Combat.CaptureChargeNativeBoundaries() };
        private bool ChargeTerminal() => realtimeChargeCommand != null && realtimeChargeCommand.IsFinished &&
            realtimeChargeCommand.ChargeCleanupComplete && realtimeChargeCommand.ChargeLeaseRestored &&
            realtimeChargeShell.IsFinished && (realtimeChargeShell.ExecutionProcess == null || realtimeChargeShell.ExecutionProcess.IsEnded) &&
            !a.Combat.HasChargeOwnership && PairIdle(a);
        private int ChargeWindowAttackCount()
        {
            var offset = ((JArray)realtimeCharge["effectsBefore"]["events"]).Count;
            return ((JArray)realtimeEffects.Capture()["events"]).Skip(offset).Count(x => (string)x["kind"] == "attack-before" &&
                (string)x["actor"] == ar.UniqueId && (string)x["target"] == target.UniqueId && (bool?)x["opportunity"] == false);
        }
        private void FinishRealtimeCharge()
        {
            var after = WorldSnapshot("rt-charge-settled"); var movementBefore = (JObject)realtimeCharge["movement"]["before"];
            Require(Vector3.Distance(PositionOf(movementBefore, bm), bm.Position) > .02f &&
                b.Relationship.PairOwner.Id.ToString() == (string)realtimeCharge["neighbourOwner"] &&
                br.CombatState.Cooldown.StandardAction == 0f && bm.CombatState.Cooldown.StandardAction == 0f,
                "Independent pair movement did not complete without paying rider Charge expenditure.");
            realtimeCharge["after"] = after; realtimeCharge["settled"] = ChargeObservation(); realtimeCharge["effectsAfter"] = realtimeEffects.Capture();
            var lease = (JObject)realtimeCharge["settled"]["lease"];
            Require(lease != null && (bool)lease["applied"] && (bool)lease["restored"] &&
                !(bool)lease["forcedPathOutstanding"] && !(bool)lease["buffOutstanding"] &&
                (bool)lease["chargingRestoredExactly"] && (bool)lease["speedOverrideRestoredExactly"] && (bool)lease["riderChargingRestoredExactly"],
                "Native Charge lease has not restored every owned speed/path/charging/buff obligation.");
        }
    }
}
