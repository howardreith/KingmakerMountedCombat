using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI.Log;
using Kingmaker.UnitLogic;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // F6 (FALL-01/FALL-02) turn-based rows. The F3 encounter (two mounted pairs, pair A rider-first, pair B mount-first,
    // a durable hostile) first runs its ordinary move, attack, step and Charge rows; then, on pair A's own activation,
    // pair A's mount dies to one native lethal damage rule and its rider takes the soft fall labelled to fail, followed by
    // the rider's ordinary order, which the native stand-up answers; on pair B's own activation its mount is knocked prone
    // by the native knock-off writer with the soft fall labelled to pass, then the already unseated mount is knocked
    // again and killed. Setup is disclosed; the check, damage, prone, stand-up and costs are native. The external reader
    // owns PASS/FAIL.
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal const string Finalize6FallsTurnBased = "finalize6-falls-tb";
        internal const int Finalize6SchemaVersion = 55;
        internal static readonly string[] Finalize6TurnBasedCases = { "F3-tb-setup", "F3-tb-a-round1", "F3-tb-b-round1", "F3-tb-a-step", "F3-tb-b-charge",
            "F6-tb-a-death-fall", "F6-tb-b-prone-fall" };
        private const string Finalize6Contract = "finalize6-falls-v1";
        private const int Finalize6SettleFrames = 30, Finalize6LogTail = 10;
        private bool IsFinalize6Falls => request.Scenario == Finalize6FallsTurnBased;
        private readonly HashSet<UnitEntityData> f6NoHeal = new HashSet<UnitEntityData>();
        private JArray f6Falls;
        private long f6BegunAt, f6ResolvedAt;

        private static MountedRiderFallService F6Service =>
            MountedRiderFallService.Current ?? throw new InvalidOperationException("The rider fall service is not present.");

        private void BeginFinalize6Measure()
        {
            f6Falls = new JArray();
            f3Measure["contract"] = Finalize6Contract;
            f3Measure["falls"] = f6Falls;
            f3Measure["fallPolicy"] = new JObject { ["dc"] = MountedRiderFallPolicy.DifficultyClass, ["damageDice"] = MountedRiderFallPolicy.DamageDice,
                ["damageDieSides"] = MountedRiderFallPolicy.DamageDieSides, ["stat"] = "SkillMobility" };
            f3Measure["fallServiceAtStart"] = F6ServiceState();
        }

        private static JObject F6ServiceState()
        {
            var service = F6Service;
            return new JObject { ["begun"] = service.BegunCount, ["resolved"] = service.ResolvedCount, ["abandoned"] = service.AbandonedCount,
                ["pending"] = service.PendingCount, ["lastRefusal"] = service.LastRefusal.ToString() };
        }

        internal static JObject F6Record(MountedRiderFallRecord r) => r == null ? null : new JObject {
            ["sequence"] = r.Sequence, ["generation"] = r.Generation, ["cause"] = r.Cause, ["riderId"] = r.RiderId, ["mountId"] = r.MountId,
            ["riderName"] = r.RiderName, ["mountName"] = r.MountName, ["mode"] = r.Mode, ["round"] = r.Round, ["captureFrame"] = r.CaptureFrame,
            ["resolveFrame"] = r.ResolveFrame, ["resolved"] = r.Resolved, ["abandonedReason"] = r.AbandonedReason, ["inability"] = r.Inability,
            ["attempted"] = r.Attempted, ["forced"] = r.Forced, ["forcedPass"] = r.ForcedPass, ["passed"] = r.Passed, ["thresholdAgrees"] = r.ThresholdAgrees,
            ["checkD20"] = r.CheckD20, ["checkStat"] = r.CheckStat, ["checkBonus"] = r.CheckBonus, ["checkResult"] = r.CheckResult, ["checkDc"] = r.CheckDc,
            ["checkDcAdjustment"] = r.CheckDcAdjustment, ["outcome"] = r.Outcome, ["damageDealt"] = r.DamageDealt, ["damageRolled"] = r.DamageRolled,
            ["damageBeforeDifficulty"] = r.DamageBeforeDifficulty, ["damageApplied"] = r.DamageApplied, ["damageImmune"] = r.DamageImmune,
            ["riderDamageBefore"] = r.RiderDamageBefore, ["riderDamageAfter"] = r.RiderDamageAfter, ["proneImposed"] = r.ProneImposed,
            ["proneRefusal"] = r.ProneRefusal, ["riderProneBefore"] = r.RiderProneBefore,
            ["standardBefore"] = r.StandardBefore, ["standardAfter"] = r.StandardAfter, ["moveBefore"] = r.MoveBefore, ["moveAfter"] = r.MoveAfter,
            ["swiftBefore"] = r.SwiftBefore, ["swiftAfter"] = r.SwiftAfter, ["logText"] = r.LogText };

        internal static JObject F6Unit(UnitEntityData unit)
        {
            if (unit == null) return null;
            var state = unit.Descriptor.State; var cooldown = unit.CombatState.Cooldown;
            return new JObject { ["id"] = unit.UniqueId, ["name"] = unit.CharacterName, ["damage"] = unit.Damage, ["hpLeft"] = unit.HPLeft,
                ["life"] = state.LifeState.ToString(), ["prone"] = state.Prone.Active, ["proneQueued"] = state.Prone.ShouldBeActive,
                ["standard"] = cooldown.StandardAction, ["move"] = cooldown.MoveAction, ["swift"] = cooldown.SwiftAction,
                ["hasMove"] = unit.HasMoveAction(), ["hasStandard"] = unit.HasStandardAction(), ["hasSwift"] = unit.HasSwiftAction(),
                ["position"] = CapturePosition(unit.Position), ["commandsEmpty"] = unit.Commands.Empty, ["inCombat"] = unit.IsInCombat };
        }

        private JObject F6Pair(UnitEntityData pairRider)
        {
            var owner = pairRider == null ? null : relationship.World?.Registry.FindOwner(pairRider);
            var boundary = owner?.Boundary;
            return new JObject { ["state"] = boundary?.State.ToString() ?? "None", ["generation"] = boundary?.MountedPairGeneration,
                ["ownerId"] = owner?.Id.ToString("N"), ["riderId"] = boundary?.Rider?.UniqueId, ["mountId"] = boundary?.Mount?.UniqueId };
        }

        // The native battle log's own entries (every channel), newest last: the check, the damage and the explanation.
        private static readonly FieldInfo F6LogData = typeof(BattleLogView).GetField("m_LogDataList", BindingFlags.NonPublic | BindingFlags.Static);
        internal static JArray F6LogTailRows(int count)
        {
            var rows = new JArray();
            try
            {
                var manager = F6LogData?.FieldType == typeof(LogDataManager) ? F6LogData.GetValue(null) as LogDataManager : null;
                var items = manager?.GetChannelData((int)LogChannel.None);
                if (items == null) { rows.Add(new JObject { ["error"] = "The native battle log data is unavailable." }); return rows; }
                foreach (var item in items.Skip(Math.Max(0, items.Count - count))) rows.Add(item?.Msg);
            }
            catch (Exception exception) { rows.Add(new JObject { ["error"] = exception.GetType().Name + ": " + exception.Message }); }
            return rows;
        }

        private JObject F6Snapshot() => new JObject { ["frame"] = Time.frameCount, ["round"] = Game.Instance.TurnBasedCombatController.RoundNumber,
            ["turnRole"] = F3Role(Game.Instance.TurnBasedCombatController.CurrentTurn?.Unit), ["service"] = F6ServiceState(),
            ["aPair"] = F6Pair(rider), ["bPair"] = F6Pair(f3BRider), ["aRider"] = F6Unit(rider), ["aMount"] = F6Unit(horse),
            ["bRider"] = F6Unit(f3BRider), ["bMount"] = F6Unit(f3BMount), ["healCount"] = f3HealCount };

        // One self-initiated native damage rule sized past death (party initiator: no difficulty scaling), as the Sprint 3 fixture kill does.
        private JObject F6Kill(UnitEntityData mount, string why)
        {
            var needed = Math.Max(1, mount.Stats.HitPoints.ModifiedValue + mount.Stats.Constitution.ModifiedValue + mount.Stats.TemporaryHitPoints.ModifiedValue - mount.Damage + 1);
            f6NoHeal.Add(mount);
            var rule = Rulebook.Trigger(new RuleDealDamage(mount, mount, new DamageBundle(new DirectDamage(new DiceFormula(0, DiceType.Zero), needed))));
            mount.Wake(1f);
            F3Disclose("The " + F3Role(mount) + " was killed by one self-initiated native damage rule of " + needed + " (" + why + "); it is excluded from the setup healing.");
            return new JObject { ["requested"] = needed, ["applied"] = rule.Damage, ["frame"] = Time.frameCount };
        }

        private JObject F6KnockOff(UnitEntityData mount, string why)
        {
            var canBeKnockedOff = mount.CanBeKnockedOff();
            if (canBeKnockedOff) mount.Descriptor.State.Prone.ShouldBeActive = true;
            F3Disclose("The " + F3Role(mount) + " was knocked prone by the native knock-off writer (" + why + "); allowed=" + canBeKnockedOff + ".");
            return new JObject { ["canBeKnockedOff"] = canBeKnockedOff, ["wasProne"] = mount.Descriptor.State.Prone.Active, ["frame"] = Time.frameCount };
        }

        private MountedRiderFallRecord F6LastFallOf(UnitEntityData fallenRider) =>
            F6Service.Records.LastOrDefault(record => record.RiderId == fallenRider.UniqueId);

        private void F6Label(UnitEntityData fallenRider, bool pass)
        {
            var id = fallenRider.UniqueId;
            F6Service.ScenarioOutcome = record => record.RiderId == id ? pass : (bool?)null;
            F3Disclose("The " + F3Role(fallenRider) + " soft fall is labelled to " + (pass ? "pass (the d20 that meets the native DC exactly)" : "fail (the d20 one below the native DC)") + ".");
        }

        private void F6KeepFall(string label, UnitEntityData fallenRider)
        {
            var record = F6LastFallOf(fallenRider);
            f6Falls.Add(new JObject { ["label"] = label, ["record"] = F6Record(record), ["log"] = F6LogTailRows(Finalize6LogTail) });
            F6Service.ScenarioOutcome = null;
        }

        // ---- F6-tb-a-death-fall: pair A's mount dies in pair A's activation; the rider falls hard, then stands natively ------
        private void TickF6ADeathFall(TurnController turn)
        {
            var participant = combat.TurnParticipant;
            switch (f3Step)
            {
                case 0:
                    if (!F3Activation(turn, rider, horse, f3LastATurn)) return;
                    f3LastATurn = turn;
                    F3StartActivation(turn, "a-fall-start", rider, horse, participant);
                    f3Step = 1; return;
                case 1:
                {
                    var service = F6Service;
                    f6BegunAt = service.BegunCount; f6ResolvedAt = service.ResolvedCount;
                    F3Steps["fallBefore"] = F6Snapshot();
                    F6Label(rider, false);
                    F3Steps["kill"] = F6Kill(horse, "pair A mount death in pair A's activation");
                    f3Frames = 0; f3Step = 2; return;
                }
                case 2:
                    if (!horse.Descriptor.State.IsDead || F6Service.ResolvedCount <= f6ResolvedAt) return;
                    F3Steps["fallAfter"] = F6Snapshot();
                    F3Steps["placement"] = new JObject { ["riderToMount"] = HorizontalDistance(rider.Position, horse.Position), ["riderCorpulence"] = rider.Corpulence, ["mountCorpulence"] = horse.Corpulence };
                    F6KeepFall("tb-a-death", rider);
                    f3Frames = 0; f3Step = 3; return;
                case 3:
                    if (++f3Frames < Finalize6SettleFrames) return;
                    F3Steps["fallSettled"] = F6Snapshot();
                    F3Steps["fallSettledLog"] = F6LogTailRows(Finalize6LogTail);
                    // r01: the existing paired split ends the activation it interrupts (native ForceToEnd), so the rider's
                    // turn in which its mount died is over; recorded, not changed.
                    F3Steps["deathActivationEnded"] = !ReferenceEquals(turn, f3RowTurn);
                    F3Shot("f6-a-death-fall", rider);
                    f3Step = 4; return;
                case 4:
                    // The rider's own next native turn, prepared by the engine; other party turns meanwhile are ended natively.
                    if (!F3Activation(turn, rider, null, f3RowTurn)) return;
                    f3LastATurn = turn; f3RowTurn = turn;
                    F3Steps["nextTurn"] = F6Snapshot();
                    f3Step = 5; return;
                case 5:
                {
                    // The rider's ordinary order on its own turn: the native stand-up answers it.
                    F3Select(rider);
                    F3Steps["standUpBefore"] = F6Snapshot();
                    F3GroundClick("standUp", F3Destination(rider, f3BMount, 3f), true);
                    f3Frames = 0; f3Step = 6; return;
                }
                case 6:
                    if (rider.Descriptor.State.Prone.Active || !F3Settled(rider, null, null)) return;
                    F3Steps["standUpAfter"] = F6Snapshot();
                    F3Shot("f6-a-stood-up", rider);
                    f3Step = 7; return;
                case 7:
                    if (!F3CloseActivation(turn, rider, null, null)) return;
                    FinishF3Row(true, "Pair A mount death and soft fall observations recorded.");
                    return;
            }
        }

        // ---- F6-tb-b-prone-fall: pair B's mount falls prone in pair B's activation; the rider lands safely -----------------
        private void TickF6BProneFall(TurnController turn)
        {
            var participant = F3BParticipant;
            switch (f3Step)
            {
                case 0:
                    if (!F3Activation(turn, f3BRider, f3BMount, f3LastBTurn)) return;
                    f3LastBTurn = turn;
                    F3StartActivation(turn, "b-fall-start", f3BRider, f3BMount, participant);
                    f3Step = 1; return;
                case 1:
                {
                    var service = F6Service;
                    f6BegunAt = service.BegunCount; f6ResolvedAt = service.ResolvedCount;
                    F3Steps["fallBefore"] = F6Snapshot();
                    F6Label(f3BRider, true);
                    f6NoHeal.Add(f3BMount);
                    F3Steps["knockOff"] = F6KnockOff(f3BMount, "pair B mount prone in pair B's activation");
                    f3Frames = 0; f3Step = 2; return;
                }
                case 2:
                    if (F3BState() == RelationshipState.Mounted.ToString() || F6Service.ResolvedCount <= f6ResolvedAt) return;
                    F3Steps["fallAfter"] = F6Snapshot();
                    F3Steps["placement"] = new JObject { ["riderToMount"] = HorizontalDistance(f3BRider.Position, f3BMount.Position), ["riderCorpulence"] = f3BRider.Corpulence, ["mountCorpulence"] = f3BMount.Corpulence };
                    F6KeepFall("tb-b-prone", f3BRider);
                    F3Steps["proneActivationEnded"] = !ReferenceEquals(turn, f3RowTurn);
                    f3Frames = 0; f3Step = 3; return;
                case 3:
                    if (++f3Frames < Finalize6SettleFrames) return;
                    F3Steps["fallSettled"] = F6Snapshot();
                    // The same fall's later callbacks: the unseated mount is knocked again (already prone) and then dies.
                    f6BegunAt = F6Service.BegunCount;
                    F3Steps["repeatKnockOff"] = F6KnockOff(f3BMount, "repeat on the already unseated mount");
                    F3Steps["deathAfterProne"] = F6Kill(f3BMount, "death of the unseated prone mount");
                    f3Frames = 0; f3Step = 4; return;
                case 4:
                    if (!f3BMount.Descriptor.State.IsDead || ++f3Frames < Finalize6SettleFrames) return;
                    F3Steps["afterDeath"] = F6Snapshot();
                    F3Steps["afterDeathLog"] = F6LogTailRows(Finalize6LogTail);
                    F3Steps["begunSinceUnseated"] = F6Service.BegunCount - f6BegunAt;
                    F3Shot("f6-b-prone-fall", f3BRider);
                    f3Step = 5; return;
                case 5:
                    if (!F3CloseActivation(turn, f3BRider, null, null)) return;
                    FinishF3Row(true, "Pair B prone soft fall observations recorded.");
                    return;
            }
        }
    }
}
