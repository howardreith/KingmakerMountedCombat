using System;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;
namespace KingmakerMountedCombat.Diagnostics
{
    // Owns only the diagnostic tick-observation flag. Capture/Finish are read-only;
    // the calling scenario owns its clock bounds, native commands and restoration.
    internal sealed class NativePassiveResourceProbe : IDisposable
    {
        private readonly NativeActorAllocationTrace trace;
        private readonly UnitEntityData rider, mount;
        private readonly bool priorObservation, turnBased;
        private readonly int start;
        private readonly JObject before;
        private JObject terminal;
        private bool disposed;
        internal NativePassiveResourceProbe(NativeActorAllocationTrace trace, UnitEntityData rider, UnitEntityData mount)
        {
            this.trace = trace ?? throw new ArgumentNullException(nameof(trace));
            this.rider = rider ?? throw new ArgumentNullException(nameof(rider));
            this.mount = mount ?? throw new ArgumentNullException(nameof(mount));
            if (ReferenceEquals(rider, mount)) throw new InvalidOperationException("Passive resource pair aliases one actor.");
            priorObservation = trace.ObserveReactionResources;
            turnBased = CombatController.IsInTurnBasedCombat(); start = trace.EventCount;
            trace.ObserveReactionResources = true;
            try { before = Snapshot(); } catch { Dispose(); throw; }
        }
        private JObject Snapshot() => new JObject {
            ["frame"] = Time.frameCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
            ["allocationSequence"] = trace.EventCount, ["rider"] = trace.Snapshot(rider), ["mount"] = trace.Snapshot(mount)
        };
        internal JObject Capture()
        {
            if (terminal != null) return (JObject)terminal.DeepClone();
            if (disposed) throw new ObjectDisposedException(nameof(NativePassiveResourceProbe));
            var after = Snapshot(); var end = (int)after["allocationSequence"];
            return new JObject { ["contract"] = "same-allocation-no-command-cost-with-observed-native-time-only",
                ["riderId"] = rider.UniqueId, ["mountId"] = mount.UniqueId, ["turnBased"] = turnBased,
                ["traceComplete"] = trace.Complete, ["observerHooks"] = trace.ObserverHooks,
                ["before"] = before.DeepClone(), ["after"] = after,
                ["events"] = new JArray(trace.EventsSince(start).Take(end - start)) };
        }
        internal JObject Finish()
        {
            if (terminal != null) throw new InvalidOperationException("Passive resource window already closed.");
            terminal = Capture();
            try { NativePassiveResourceEvidence.AssertComplete(terminal); terminal["pass"] = true; }
            catch (Exception exception) { terminal["pass"] = false; terminal["failure"] = exception.Message; }
            return (JObject)terminal.DeepClone();
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            trace.ObserveReactionResources = priorObservation;
        }
    }
}
