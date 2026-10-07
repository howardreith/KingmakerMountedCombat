using System;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.RuleSystem.Rules;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class RuntimePersistenceScenario
    {
        private NativeActorAllocationTrace chargeContinuationTrace;
        private JObject chargeContinuationBefore;
        private readonly JArray chargeContinuationAttacks = new JArray();
        private readonly JArray chargeContinuationErrors = new JArray();

        private JObject ChargeContinuationPoint() => new JObject {
            ["frame"] = Time.frameCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
            ["allocationSequence"] = chargeContinuationTrace.EventCount,
            ["rider"] = chargeContinuationTrace.Snapshot(rider),
            ["mount"] = chargeContinuationTrace.Snapshot(mount),
            ["resolved"] = realtimeProbe.RiderResolvedCount, ["rounds"] = realtimeRounds.Count
        };

        private void BeginChargeContinuationTrace()
        {
            chargeContinuationTrace = new NativeActorAllocationTrace(rider, mount, combat) { ObserveReactionResources = true };
            chargeContinuationBefore = ChargeContinuationPoint();
            realtimeProbe.NativeAttackObserved += ObserveChargeContinuationAttack;
        }

        private void ObserveChargeContinuationAttack(string boundary, RuleAttackWithWeapon rule)
        {
            // This observer must never throw into a native rule callback or modify it.
            try {
                if (chargeContinuationAttacks.Count >= 64) throw new InvalidOperationException("Continuation attack observation bound exceeded.");
                chargeContinuationTrace.Record("continuation-" + boundary, rule.Initiator,
                    rule.Initiator.Commands.Standard, callback: rule);
                chargeContinuationAttacks.Add(new JObject {
                    ["boundary"] = boundary, ["allocationSequence"] = chargeContinuationTrace.EventCount,
                    ["rule"] = RuntimeHelpers.GetHashCode(rule), ["actor"] = rule.Initiator.UniqueId,
                    ["target"] = rule.Target.UniqueId, ["charge"] = rule.IsCharge,
                    ["opportunity"] = rule.IsAttackOfOpportunity, ["fullAttack"] = rule.IsFullAttack,
                    ["attackNumber"] = rule.AttackNumber, ["attacksCount"] = rule.AttacksCount
                });
            }
            catch (Exception exception) {
                if (chargeContinuationErrors.Count < 8) chargeContinuationErrors.Add(exception.ToString());
            }
        }

        private void CloseChargeContinuationTrace()
        {
            if (chargeContinuationTrace == null) return;
            realtimeProbe.NativeAttackObserved -= ObserveChargeContinuationAttack;
            var after = ChargeContinuationPoint();
            var trace = chargeContinuationTrace.Capture();
            chargeContinuationTrace.Dispose();
            chargeContinuationTrace = null;
            Write("charge-continuation-trace", new JObject {
                ["contract"] = "native-rt-charge-continuation-v1", ["closed"] = true,
                ["riderId"] = rider.UniqueId, ["mountId"] = mount.UniqueId, ["targetId"] = combatTarget.UniqueId,
                ["before"] = chargeContinuationBefore, ["after"] = after,
                ["trace"] = trace, ["attacks"] = chargeContinuationAttacks.DeepClone(),
                ["errors"] = chargeContinuationErrors.DeepClone()
            });
        }

        private void AdvanceChargeContinuation()
        {
            // Bounded raw facts only. The external validator owns debt/cost/round/attack
            // acceptance. GameTime's rounded ticks remain diagnostic, never a readiness oracle.
            var delivered = realtimeProbe.RiderResolvedCount - realtimeAttackBaseline;
            if (delivered <= realtimeLaterAttacks) return;
            realtimeLaterAttacks = delivered;
            Write("rt-later-attack", RealtimeObservation());
            if (delivered < 2) return;
            StopRealtimePartyOrders();
            CloseChargeContinuationTrace();
            Write("usable-continuation-complete", RealtimeObservation());
            Dispose();
            Result = new RuntimeSubscenarioResult { Name = request.Scenario, Status = "PASS",
                AssertionPassCount = passed, AssertionFailCount = 0, Errors = new string[0] };
            Completed = true;
        }
    }
}
