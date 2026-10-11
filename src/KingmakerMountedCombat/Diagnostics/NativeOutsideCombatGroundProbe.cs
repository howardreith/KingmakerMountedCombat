using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace KingmakerMountedCombat.Diagnostics
{
    // Owns an observation flag and strong command reference only. The caller must
    // establish exact selection before construction and use one ordinary ground input.
    internal sealed class NativeOutsideCombatGroundProbe : IDisposable
    {
        private readonly NativeActorAllocationTrace trace;
        private readonly UnitEntityData rider, mount, mover;
        private readonly bool priorObservation, priorEligibility;
        private readonly JObject before;
        private readonly int start;
        private UnitMoveTo command;
        private JObject completed;
        private bool disposed;
        internal NativeOutsideCombatGroundProbe(NativeActorAllocationTrace trace,
            UnitEntityData rider, UnitEntityData mount, UnitEntityData mover)
        {
            this.trace = trace ?? throw new ArgumentNullException(nameof(trace));
            this.rider = rider ?? throw new ArgumentNullException(nameof(rider));
            this.mount = mount ?? throw new ArgumentNullException(nameof(mount));
            this.mover = mover ?? throw new ArgumentNullException(nameof(mover));
            if (ReferenceEquals(rider, mount) || mover != rider && mover != mount ||
                rider.IsInCombat || mount.IsInCombat || Game.Instance.Player.IsInCombat ||
                trace.GrantCount(rider) != 0 || trace.GrantCount(mount) != 0)
                throw new InvalidOperationException("Ground observation requires the exact fresh pair outside combat.");
            priorObservation = trace.ObserveReactionResources; priorEligibility = trace.ObserveGroundCooldownEligibility;
            start = trace.EventCount;
            trace.ObserveReactionResources = true; trace.ObserveGroundCooldownEligibility = true;
            try { before = Snapshot(); } catch { Dispose(); throw; }
        }
        private JObject Snapshot() => new JObject {
            ["frame"] = Time.frameCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
            ["allocationSequence"] = trace.EventCount, ["rider"] = trace.Snapshot(rider), ["mount"] = trace.Snapshot(mount)
        };
        internal void Bind(UnitMoveTo value)
        {
            if (disposed || command != null || value == null || value.Executor != mover || !value.CreatedByPlayer ||
                !ReferenceEquals(mover.Commands.Move, value) || value.Type != UnitCommand.CommandType.Move)
                throw new InvalidOperationException("Ground input did not install one exact selected-actor command.");
            command = value;
        }
        internal JObject Capture()
        {
            if (completed != null) return (JObject)completed.DeepClone();
            if (disposed) throw new ObjectDisposedException(nameof(NativeOutsideCombatGroundProbe));
            var after = Snapshot(); var end = (int)after["allocationSequence"];
            return new JObject { ["contract"] = "one-native-ground-command-outside-combat-with-observed-time-only",
                ["riderId"] = rider.UniqueId, ["mountId"] = mount.UniqueId, ["turnBased"] = false,
                ["traceComplete"] = trace.Complete, ["observerHooks"] = trace.ObserverHooks,
                ["cooldownEligibilityContract"] = "observed-native-outside-combat-skip",
                ["before"] = before.DeepClone(), ["after"] = after,
                ["groundCommand"] = new JObject { ["commandObject"] = command == null ? 0 : RuntimeHelpers.GetHashCode(command),
                    ["casterId"] = command?.Executor?.UniqueId, ["type"] = command?.GetType().FullName,
                    ["createdByPlayer"] = command?.CreatedByPlayer, ["finished"] = command?.IsFinished,
                    ["nativeResult"] = command?.Result.ToString() },
                ["events"] = new JArray(trace.EventsSince(start).Take(end - start)) };
        }
        internal JObject Finish()
        {
            if (completed != null) throw new InvalidOperationException("Ground resource window already closed.");
            var proof = Capture();
            try {
                if (command == null || !command.IsFinished || command.Result != UnitCommand.ResultType.Success)
                    throw new InvalidOperationException("Ground command has no exact successful native terminal state.");
                NativePassiveResourceEvidence.AssertGround(proof); proof["pass"] = true;
            } catch (Exception exception) { proof["pass"] = false; proof["failure"] = exception.Message; }
            completed = proof; return (JObject)completed.DeepClone();
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            trace.ObserveReactionResources = priorObservation; trace.ObserveGroundCooldownEligibility = priorEligibility;
        }
    }
}
