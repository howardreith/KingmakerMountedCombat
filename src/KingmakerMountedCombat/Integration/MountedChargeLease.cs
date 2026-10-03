using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Kingmaker;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Utility;
using Kingmaker.View;
using KingmakerMountedCombat.Logging;
using Newtonsoft.Json.Linq;
using Pathfinding;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    // Chunk 6B increment 6B.2: the charge mechanics of the pair-owned Mounted Charge, in one removable place.
    //
    // Every call here is a native primitive the stock AbilityCustomCharge already makes, applied to the right
    // actor of the pair: the charging flag, the doubled speed override and the straight forced path go on the
    // MOUNT's agent because the mount is the mover, while the charge buff and the charging state go on the
    // RIDER because the rider is the attacker. Nothing is written to a cooldown, a resource, a turn or a
    // preparation, and nothing is ever refunded.
    //
    // Two facts measured on previews 156 and 157 shape the lease. A forced path lives only while the mover
    // holds a live command, so the lease is applied on top of the pair's own admitted delegated ground move and
    // is maintained while that carrier lives. UnitMovementAgent.Stop() leaves force mode latched until the next
    // OnPathComplete, exactly as the stock charge does, so Restore clears what the lease set and records the
    // latch rather than pretending to clear it.
    internal sealed class MountedChargeLease
    {
        private static readonly FieldInfo ForceModeField = ResolveForceModeField();
        private const float ForcedApproachRadius = 1000000f;

        private readonly UnitEntityData rider;
        private readonly UnitEntityData mount;
        private readonly UnitEntityData target;
        private readonly IModLogger logger;
        private readonly List<string> observations = new List<string>();

        private bool chargingBefore;
        private float? speedOverrideBefore;
        private bool riderChargingBefore;

        internal MountedChargeLease(
            UnitEntityData rider,
            UnitEntityData mount,
            UnitEntityData target,
            IModLogger logger)
        {
            this.rider = rider ?? throw new ArgumentNullException(nameof(rider));
            this.mount = mount ?? throw new ArgumentNullException(nameof(mount));
            this.target = target ?? throw new ArgumentNullException(nameof(target));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private static FieldInfo ResolveForceModeField()
        {
            var field = typeof(UnitMovementAgent).GetField("m_IsInForceMode", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null || field.MetadataToken != 0x040011AE || field.FieldType != typeof(bool))
            {
                throw new MissingFieldException(typeof(UnitMovementAgent).FullName, "m_IsInForceMode");
            }

            return field;
        }

        internal bool Applied { get; private set; }

        internal bool Restored { get; private set; }

        internal bool BuffApplied { get; private set; }

        internal bool RiderAgentTouched { get; private set; }

        internal int ForcedPathCount { get; private set; }

        internal float SpeedOverrideApplied { get; private set; }

        internal bool ChargingAppliedExactly { get; private set; }

        internal bool ChargingObservedThroughout { get; private set; } = true;

        internal bool ForceModeAfterApply { get; private set; }

        internal bool ForceModeAtRestore { get; private set; }

        internal bool ChargingRestoredExactly { get; private set; }

        internal bool SpeedOverrideRestoredExactly { get; private set; }

        internal bool RiderChargingRestoredExactly { get; private set; }

        private UnitMovementAgent MountAgent => mount.View == null ? null : mount.View.AgentASP;

        internal bool ForceMode
        {
            get
            {
                var agent = MountAgent;
                return agent != null && (bool)ForceModeField.GetValue(agent);
            }
        }

        // The three stock calls on the mover's agent, plus the attacker's charge buff and charging state.
        // Called once, immediately after the pair's delegated ground move has proved it owns the mount Move slot.
        internal void Apply()
        {
            if (Applied)
            {
                throw new InvalidOperationException("The mounted charge lease was applied twice.");
            }

            var agent = MountAgent;
            if (agent == null || rider.View == null || rider.Descriptor == null)
            {
                throw new InvalidOperationException("The mounted charge lease requires the exact live pair views.");
            }

            var riderAgent = rider.View.AgentASP;
            var riderChargingAgentBefore = riderAgent != null && riderAgent.IsCharging;
            var riderSpeedAgentBefore = riderAgent == null ? null : riderAgent.MaxSpeedOverride;
            var riderAgentEnabledBefore = riderAgent != null && riderAgent.enabled;

            chargingBefore = agent.IsCharging;
            speedOverrideBefore = agent.MaxSpeedOverride;
            riderChargingBefore = rider.Descriptor.State.IsCharging;
            SpeedOverrideApplied = Math.Max(speedOverrideBefore ?? 0f, mount.CombatSpeedMps * 2f);

            agent.IsCharging = true;
            agent.MaxSpeedOverride = SpeedOverrideApplied;
            ForcePathToTarget("initial");
            ForceModeAfterApply = ForceMode;
            ChargingAppliedExactly = agent.IsCharging;

            // The attacker's own charge state: the stock buff for one round and the native charging state.
            // The buff's duration is native and is never shortened here; Restore clears only the state flag.
            var chargeBuff = BlueprintRoot.Instance == null || BlueprintRoot.Instance.SystemMechanics == null
                ? null
                : BlueprintRoot.Instance.SystemMechanics.ChargeBuff;
            if (chargeBuff == null)
            {
                throw new InvalidOperationException("The stock charge buff blueprint is unavailable.");
            }

            BuffApplied = rider.Buffs.AddBuff(chargeBuff, rider, 1.Rounds().Seconds) != null;
            rider.Descriptor.State.IsCharging = true;

            RiderAgentTouched = riderAgent != null &&
                (riderAgent.IsCharging != riderChargingAgentBefore ||
                 riderAgent.MaxSpeedOverride != riderSpeedAgentBefore ||
                 riderAgent.enabled != riderAgentEnabledBefore);
            Applied = true;
            Observe("applied", "speed=" + Format(SpeedOverrideApplied) + ";buff=" + BuffApplied +
                ";forceMode=" + ForceModeAfterApply + ";riderAgentTouched=" + RiderAgentTouched);
            logger.Info("Mounted charge lease applied: mountId=" + mount.UniqueId +
                "; riderId=" + rider.UniqueId + "; targetId=" + target.UniqueId +
                "; speedOverride=" + Format(SpeedOverrideApplied) +
                "; chargingBefore=" + chargingBefore + "; forceMode=" + ForceModeAfterApply + ".");
        }

        // The stock runtime routine re-forces its path whenever the target has moved; the pair carrier also
        // drops force mode when its own native approach recomputes a path. Both are handled here, and the
        // doubled speed is re-asserted exactly as the stock routine re-asserts it on every tick.
        internal void Maintain(bool carrierAlive)
        {
            if (!Applied || Restored)
            {
                return;
            }

            var agent = MountAgent;
            if (agent == null)
            {
                return;
            }

            ChargingObservedThroughout &= agent.IsCharging;
            agent.MaxSpeedOverride = Math.Max(agent.MaxSpeedOverride ?? 0f, SpeedOverrideApplied);
            if (!carrierAlive || ForceMode)
            {
                return;
            }

            ForcePathToTarget("re-force");
        }

        internal void Restore()
        {
            if (Restored)
            {
                return;
            }

            Restored = true;
            ForceModeAtRestore = ForceMode;
            var agent = MountAgent;
            if (agent != null)
            {
                if (Applied)
                {
                    agent.IsCharging = chargingBefore;
                    agent.MaxSpeedOverride = speedOverrideBefore;
                }

                ChargingRestoredExactly = agent.IsCharging == chargingBefore;
                SpeedOverrideRestoredExactly = agent.MaxSpeedOverride == speedOverrideBefore;
            }

            if (Applied && rider.Descriptor != null)
            {
                rider.Descriptor.State.IsCharging = riderChargingBefore;
                RiderChargingRestoredExactly = rider.Descriptor.State.IsCharging == riderChargingBefore;
            }
            else
            {
                RiderChargingRestoredExactly = true;
            }

            Observe("restored", "charging=" + ChargingRestoredExactly + ";speed=" + SpeedOverrideRestoredExactly +
                ";riderCharging=" + RiderChargingRestoredExactly + ";forceModeLatched=" + ForceModeAtRestore);
            logger.Info("Mounted charge lease restored: mountId=" + mount.UniqueId +
                "; charging=" + ChargingRestoredExactly + "; speedOverride=" + SpeedOverrideRestoredExactly +
                "; riderCharging=" + RiderChargingRestoredExactly +
                "; forcedPaths=" + ForcedPathCount + "; forceModeLatched=" + ForceModeAtRestore + ".");
        }

        private void ForcePathToTarget(string reason)
        {
            var agent = MountAgent;
            if (agent == null)
            {
                return;
            }

            agent.ForcePath(new ForcedPath(new List<Vector3> { mount.Position, target.Position }), ForcedApproachRadius);
            ForcedPathCount++;
            Observe("forced-path", reason + ";count=" + ForcedPathCount + ";forceMode=" + ForceMode +
                ";distance=" + Format(mount.DistanceTo(target)));
        }

        private void Observe(string stage, string detail)
        {
            if (observations.Count >= 64)
            {
                return;
            }

            observations.Add(stage + ":" + detail);
        }

        private static string Format(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        internal JObject CaptureEvidence()
        {
            return new JObject
            {
                ["applied"] = Applied,
                ["buffApplied"] = BuffApplied,
                ["chargingBefore"] = chargingBefore,
                ["speedOverrideBefore"] = speedOverrideBefore,
                ["speedOverrideApplied"] = SpeedOverrideApplied,
                ["mountCombatSpeedMps"] = mount.CombatSpeedMps,
                ["chargingAppliedExactly"] = ChargingAppliedExactly,
                ["chargingObservedThroughout"] = ChargingObservedThroughout,
                ["forceModeAfterApply"] = ForceModeAfterApply,
                ["forcedPathCount"] = ForcedPathCount,
                ["riderAgentTouched"] = RiderAgentTouched,
                ["restored"] = Restored,
                ["chargingRestoredExactly"] = ChargingRestoredExactly,
                ["speedOverrideRestoredExactly"] = SpeedOverrideRestoredExactly,
                ["riderChargingRestoredExactly"] = RiderChargingRestoredExactly,
                ["forceModeAtRestore"] = ForceModeAtRestore,
                ["riderChargingBefore"] = riderChargingBefore,
                ["observations"] = new JArray(observations.ToArray())
            };
        }

        internal string Describe()
        {
            return "applied=" + Applied + ";restored=" + Restored + ";buff=" + BuffApplied +
                ";forcedPaths=" + ForcedPathCount + ";speedOverride=" + Format(SpeedOverrideApplied) +
                ";chargingThroughout=" + ChargingObservedThroughout +
                ";forceModeLatched=" + ForceModeAtRestore +
                ";riderAgentTouched=" + RiderAgentTouched;
        }
    }
}
