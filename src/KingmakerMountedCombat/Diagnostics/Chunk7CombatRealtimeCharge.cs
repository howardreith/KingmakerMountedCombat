using System;
using System.Linq;
using Kingmaker;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Commands;
using KingmakerMountedCombat.Domain;
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
        internal static readonly float[] ChargeTargetDistances = { 8f, 7f, 9f, 6f };
        internal const float ChargeTargetMaxRise = 1f, ChargeTargetMaxOffset = .3f, ChargeLaneMargin = .5f, ChargeLaneOtherFloor = 2f, ChargeLaneFactor = .8f;
        // b201 k4: on the charge seed's start the fixed point 8 m along world +Z from mount A was not on a walkable
        // native node (mount at y 0.78; the rider's 2.20 is its seat). The target point is now selected, never the
        // party moved: the 8 m forward point first, then the rest of the circle and other charge distances, each a
        // walkable navmesh point at the mount's level whose native straight navmesh trace from the mount reaches it
        // exactly (the Vector3 equality MountedChargeGeometry.StraightRoute applies at charge time), inside the
        // bounded spawn range and the native charge range, with no other awake unit on the charge lane. The lane uses
        // the stock landing clearance ((mount + unit corpulence) * 0.8) along the segment beyond the mount's own
        // footprint, for units on the mount's level (within 2 m vertically). Every unit and candidate is recorded
        // before any is chosen; none admitted refuses before any fixture mutation.
        private Vector3 SelectChargeTargetPosition()
        {
            Require(AstarPath.active != null, "Active native navigation graph is unavailable for the charge target placement.");
            var start = am.Position; var candidates = new JArray(); var mountCorpulence = am.View.Corpulence;
            var minimum = a.Combat.GetMountedChargeMinimumRange(ar); var maximum = MountedChargeGeometry.MaximumRange(am);
            var others = Game.Instance.State.AwakeUnits.Where(u => u != ar && u != am && u.View != null).ToArray();
            var placement = new JObject { ["contract"] = "chunk7-rt-charge-target-placement-v2", ["mountPosition"] = Point(start),
                ["riderPosition"] = Point(ar.Position), ["mountCorpulence"] = mountCorpulence, ["minimumChargeRange"] = minimum, ["maximumChargeRange"] = maximum,
                ["units"] = new JArray(others.Select(u => new JObject { ["id"] = u.UniqueId, ["position"] = Point(u.Position), ["corpulence"] = u.View.Corpulence })),
                ["candidates"] = candidates };
            evidence["chargeTargetPlacement"] = placement;
            foreach (var distance in ChargeTargetDistances)
                foreach (var angle in FullCircleFan)
                {
                    var requested = start + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * distance;
                    var nearest = AstarPath.active.GetNearest(requested);
                    var walkable = nearest.node != null && nearest.node.Walkable; var goal = nearest.clampedPosition;
                    var traced = walkable ? Kingmaker.View.ObstacleAnalyzer.TraceAlongNavmesh(start, goal) : start;
                    var traceExact = walkable && traced == goal;
                    string blocker = null;
                    foreach (var u in others)
                        if (ChargeLaneBlocked(start, goal, u.Position, mountCorpulence, u.View.Corpulence)) { blocker = u.UniqueId; break; }
                    var chargeDistance = Flat(start, goal); var riderDistance = Flat(ar.Position, goal);
                    var eligible = walkable && Flat(goal, requested) <= ChargeTargetMaxOffset && Mathf.Abs(goal.y - start.y) <= ChargeTargetMaxRise &&
                        traceExact && MountedCombatSpatialPolicy.IsWithinDiagnosticSpawnBounds(riderDistance) &&
                        chargeDistance >= minimum + ChargeLaneMargin && chargeDistance <= maximum - ChargeLaneMargin && blocker == null;
                    candidates.Add(new JObject { ["angle"] = angle, ["distance"] = distance, ["requested"] = Point(requested), ["walkable"] = walkable,
                        ["goal"] = Point(goal), ["traceEnd"] = Point(traced), ["traceExact"] = traceExact, ["laneBlocker"] = blocker, ["eligible"] = eligible });
                    if (!eligible) continue;
                    placement["selected"] = candidates.Count - 1; placement["position"] = Point(goal);
                    return goal;
                }
            placement["selected"] = null;
            throw new InvalidOperationException("Diagnostic target placement refused before any measured step: no walkable same-level native charge target " +
                "with a straight clear lane from mount A (" + candidates.Count + " candidates recorded); RT Charge is uncovered on this start.");
        }
        // A unit on the mount's level blocks the lane when it lies within the stock landing clearance of the segment
        // beyond the mount's own footprint (the mount's corpulence plus the unit's, measured from the start).
        internal static bool ChargeLaneBlocked(Vector3 from, Vector3 to, Vector3 unit, float mountCorpulence, float unitCorpulence)
        {
            if (Mathf.Abs(unit.y - from.y) > ChargeLaneOtherFloor) return false;
            Vector2 a = new Vector2(from.x, from.z), b = new Vector2(to.x, to.z), p = new Vector2(unit.x, unit.z), ab = b - a;
            var length = ab.magnitude; if (length < 1e-3f) return false;
            var along = Vector2.Dot(p - a, ab) / length; var skip = mountCorpulence + unitCorpulence;
            if (along < skip) return false;
            var t = Mathf.Clamp01(along / length);
            return (a + ab * t - p).magnitude < (mountCorpulence + unitCorpulence) * ChargeLaneFactor;
        }
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
