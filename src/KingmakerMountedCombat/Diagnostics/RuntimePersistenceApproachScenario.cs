using System;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Utility;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class RuntimePersistenceScenario
    {
        private Vector3 realtimeApproachOrigin;
        private JObject realtimeApproachBarrier;
        private int approachRoundBaseline;

        private static JArray RealtimePoint(UnitEntityData unit) => unit == null ? null :
            new JArray(unit.Position.x, unit.Position.y, unit.Position.z);

        private JObject RealtimeApproachObservation()
        {
            var mover = RealtimeMounted ? mount : rider;
            return mover?.View?.AgentASP == null || combatTarget == null ? null : new JObject {
                ["actor"] = mover.UniqueId, ["moving"] = mover.View.AgentASP.IsReallyMoving,
                ["travelled"] = GeometryUtils.MechanicsDistance(mover.Position, realtimeApproachOrigin),
                ["remaining"] = GeometryUtils.MechanicsDistance(mover.Position, combatTarget.Position) };
        }

        private void ObserveApproachSnapshot()
        {
            if (realtimeApproachBarrier != null) throw new InvalidOperationException("Duplicate native approach snapshot.");
            realtimeApproachBarrier = RealtimeObservation();
            Write("rt-native-snapshot", realtimeApproachBarrier);
        }

        private void AdvanceApproachRequest()
        {
            var mover = RealtimeMounted ? mount : rider;
            if (!mover.View.AgentASP.IsReallyMoving ||
                GeometryUtils.MechanicsDistance(mover.Position, realtimeApproachOrigin) < 0.5f) return;
            Check(GeometryUtils.MechanicsDistance(mover.Position, combatTarget.Position) > 5f &&
                realtimeProbe.RiderResolvedCount == 0 && realtimeProbe.RiderNonOpportunityAttackRuleCount == 0 &&
                realtimeProbe.PairForcedD20Count == 0, "RT-request-during-partial-approach-before-native-attack");
            Write("rt-approach-save-request", RealtimeObservation());
            RequestRealtimeSave();
        }

        private void BeginApproachContinuation()
        {
            Check(realtimeProbe.RiderResolvedCount == 0 && realtimeProbe.RiderNonOpportunityAttackRuleCount == 0,
                "RT-approach-save-has-no-delivered-or-replayed-attack");
            approachRoundBaseline = realtimeRounds.Count;
            // Warm continuation must retain its original native order. The cold
            // path issues ordinary new input after observing retired transient intent.
            if (Cold) QueueRealtimeAttack();
            Write("rt-approach-continuation", RealtimeObservation());
            stage = 22;
        }

        private void AdvanceApproachContinuation()
        {
            if (realtimeProbe.RiderResolvedCount == 0) return;
            Check(realtimeProbe.RiderResolvedCount == 1 && realtimeProbe.PairForcedD20Count == 0 &&
                realtimeRounds.Count == approachRoundBaseline && realtimeInputRequests == 1,
                "RT-approach-retains-one-normal-delivery-without-reissue-or-fresh-round");
            Write("rt-approach-first-delivery", RealtimeObservation());
            BeginRealtimeContinuation(false);
        }
    }
}
