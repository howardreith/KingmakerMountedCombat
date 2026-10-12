using System;
using System.Linq;
using System.Reflection;
using Harmony12;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // A read-only observation of the actual native Tick -> SetLifeState ->
    // EventBus -> existing production subscriber call chain. No callback is invoked.
    internal sealed class NativeLifeDeliveryProbe : IDisposable
    {
        private const string Id = "KingmakerMountedCombat.Diagnostics.Chunk7NativeLife";
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private static NativeLifeDeliveryProbe active;
        private HarmonyInstance harmony;
        private readonly UnitEntityData actor;
        private readonly MountedLifecycleSubscriber lifecycle;
        private readonly Func<string, JObject> snapshot;
        private readonly JArray events = new JArray(), errors = new JArray();
        private readonly MethodInfo callback;
        private int tickDepth, setDepth, setSequence;
        private bool disposed, delivered, captureCompleted, installed, installationStarted;
        internal NativeLifeDeliveryProbe(UnitEntityData actor, MountedLifecycleSubscriber lifecycle, Func<string, JObject> snapshot)
        {
            this.actor = actor ?? throw new ArgumentNullException(nameof(actor));
            this.lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
            this.snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            callback = typeof(MountedLifecycleSubscriber).GetMethod(nameof(MountedLifecycleSubscriber.HandleUnitLifeStateChanged), Flags);
        }
        internal void Install()
        {
            if (disposed) throw new ObjectDisposedException(nameof(NativeLifeDeliveryProbe));
            if (installed) return;
            if (installationStarted) throw new InvalidOperationException("A failed observer installation retains cleanup custody and cannot be replayed.");
            if (active != null) throw new InvalidOperationException("Another observer already owns this native boundary.");
            installationStarted = true;
            // Native contract validation; no patch ownership exists before the Harmony boundary.
            if (typeof(UnitEntityData).Module.ModuleVersionId != new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"))
                throw new InvalidOperationException("Native life observer has foreign identity.");
            var type = typeof(UnitEntityData).Assembly.GetType("Kingmaker.Controllers.Units.UnitLifeController", true);
            var tick = type.GetMethods(Flags).Single(m => m.MetadataToken == 0x06009162);
            var set = type.GetMethods(Flags).Single(m => m.MetadataToken == 0x06009164);
            if (tick.Name != "TickOnUnit" || tick.ReturnType != typeof(void) || tick.GetParameters().Length != 1 || tick.GetParameters()[0].Name != "unit" ||
                tick.GetParameters()[0].ParameterType != typeof(UnitEntityData) || set.Name != "SetLifeState" || !set.IsStatic ||
                set.ReturnType != typeof(void) || set.GetParameters().Length != 2 || set.GetParameters()[0].Name != "unit" ||
                set.GetParameters()[0].ParameterType != typeof(UnitEntityData) || set.GetParameters()[1].ParameterType != typeof(UnitLifeState))
                throw new MissingMemberException("Pinned native life Tick/SetLifeState signatures differ.");
            harmony = HarmonyInstance.Create(Id); active = this;
            Patch(tick, nameof(TickBefore), nameof(TickAfter)); Patch(set, nameof(SetBefore), nameof(SetAfter));
            Patch(callback, nameof(DeliveryBefore), nameof(DeliveryAfter));
            installed = true;
        }
        private void Patch(MethodInfo method, string before, string after) => harmony.Patch(method,
            new HarmonyMethod(typeof(NativeLifeDeliveryProbe).GetMethod(before, Flags)) { prioritiy = Priority.First },
            new HarmonyMethod(typeof(NativeLifeDeliveryProbe).GetMethod(after, Flags)) { prioritiy = Priority.Last });
        private static void TickBefore(UnitEntityData unit) { var p = For(unit); if (p == null) return; p.tickDepth++; p.Record("native-tick-before"); }
        private static void TickAfter(UnitEntityData unit) { var p = For(unit); if (p == null) return; p.Record("native-tick-after"); p.tickDepth--; if (p.delivered && p.tickDepth == 0) p.captureCompleted = true; }
        private static void SetBefore(UnitEntityData unit) { var p = For(unit); if (p == null) return; p.setDepth++; p.setSequence++; p.Record("native-set-before"); }
        private static void SetAfter(UnitEntityData unit) { var p = For(unit); if (p == null) return; p.Record("native-set-after"); p.setDepth--; }
        private static void DeliveryBefore(MountedLifecycleSubscriber __instance, UnitEntityData unit, UnitLifeState prevLifeState)
        { var p = For(unit); if (p != null && ReferenceEquals(p.lifecycle, __instance)) p.Record("production-callback-before", prevLifeState.ToString()); }
        private static void DeliveryAfter(MountedLifecycleSubscriber __instance, UnitEntityData unit, UnitLifeState prevLifeState)
        { var p = For(unit); if (p != null && ReferenceEquals(p.lifecycle, __instance)) { p.Record("production-callback-after", prevLifeState.ToString()); p.delivered = true; } }
        private static NativeLifeDeliveryProbe For(UnitEntityData unit) => active != null && active.installed && !active.disposed && !active.captureCompleted && ReferenceEquals(active.actor, unit) ? active : null;
        private void Record(string boundary, string previous = null)
        {
            // Tick is frequent; capture only the actual transition window, which
            // includes at most one native Tick before the selected damage arrives.
            if (boundary.StartsWith("native-tick", StringComparison.Ordinal) && actor.Damage < actor.Stats.HitPoints.ModifiedValue) return;
            try
            {
                if (events.Count >= 128) throw new InvalidOperationException("Native life observation bound exceeded.");
                events.Add(new JObject { ["sequence"] = events.Count + 1, ["boundary"] = boundary, ["actorId"] = actor.UniqueId,
                    ["frame"] = Time.frameCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
                    ["round"] = Game.Instance.TurnBasedCombatController.RoundNumber, ["nativeTickDepth"] = tickDepth,
                    ["nativeSetDepth"] = setDepth, ["nativeSetSequence"] = setSequence,
                    ["previousLife"] = previous, ["currentLife"] = actor.Descriptor.State.LifeState.ToString(),
                    ["damage"] = actor.Damage, ["world"] = snapshot("life-" + boundary),
                    ["ledgerSequence"] = lifecycle.SnapshotNativeDeliveries().Select(x => x.Sequence).DefaultIfEmpty().Max() });
            }
            catch (Exception error) { if (!errors.Any(e => (string)e == error.ToString())) errors.Add(error.ToString()); }
        }
        internal JObject Capture() => new JObject { ["actorId"] = actor.UniqueId, ["nativeModuleMvid"] = typeof(UnitEntityData).Module.ModuleVersionId.ToString(),
            ["nativeTickToken"] = "06009162", ["nativeSetToken"] = "06009164", ["callbackToken"] = callback.MetadataToken.ToString("X8"),
            ["callbackMvid"] = callback.Module.ModuleVersionId.ToString(), ["callbackType"] = callback.DeclaringType.FullName,
            ["callbackName"] = callback.Name, ["tickDepth"] = tickDepth, ["setDepth"] = setDepth,
            ["completedNativeTick"] = installed && captureCompleted, ["events"] = events.DeepClone(), ["errors"] = CaptureErrors() };
        private JArray CaptureErrors()
        {
            var result = (JArray)errors.DeepClone();
            if (!installed) result.Add("Native observer installation did not complete.");
            return result;
        }
        public void Dispose()
        {
            if (disposed) return;
            if (tickDepth != 0 || setDepth != 0) throw new InvalidOperationException("Native life observation still owns an active callback window.");
            harmony?.UnpatchAll(Id); disposed = true; if (ReferenceEquals(active, this)) active = null;
        }
    }
}
