using System;
using System.Reflection;
using Harmony12;
using Kingmaker.EntitySystem.Entities;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;

namespace KingmakerMountedCombat.Diagnostics
{
    // A one-shot fixture input at a real Mount delivery boundary. It leaves the
    // original dispatch, native command, perception and turn processing intact.
    internal sealed class DiagnosticQueuedMountWindow : IDisposable
    {
        private const string HarmonyId = "KingmakerMountedCombat.Diagnostics.QueuedMountWindow";
        private static DiagnosticQueuedMountWindow active;
        private readonly HarmonyInstance harmony;
        private readonly NativeMountedControlService service;
        private readonly UnitEntityData rider;
        private readonly UnitEntityData mount;
        private readonly Action before;
        private readonly Action<bool> after;
        private bool entered;
        internal string Error { get; private set; }

        internal DiagnosticQueuedMountWindow(NativeMountedControlService service, UnitEntityData rider,
            UnitEntityData mount, Action before, Action<bool> after)
        {
            if (active != null) throw new InvalidOperationException("A queued Mount fixture is already active.");
            this.service = service; this.rider = rider; this.mount = mount;
            this.before = before; this.after = after;
            harmony = HarmonyInstance.Create(HarmonyId); active = this;
            try
            {
                var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
                harmony.Patch(typeof(NativeMountedControlService).GetMethod("TryDispatch", flags),
                    new HarmonyMethod(typeof(DiagnosticQueuedMountWindow).GetMethod("Before", flags)),
                    new HarmonyMethod(typeof(DiagnosticQueuedMountWindow).GetMethod("After", flags)));
            }
            catch { Dispose(); throw; }
        }

        private static void Before(NativeMountedControlService __instance, NativeMountedControlKind kind,
            UnitEntityData caster, UnitEntityData target, out bool __state)
        {
            var current = active;
            __state = current != null && !current.entered && current.service == __instance &&
                kind == NativeMountedControlKind.MountCompanion && caster == current.rider && target == current.mount;
            if (!__state) return;
            current.entered = true;
            try { current.before(); }
            catch (Exception exception) { current.Error = exception.ToString(); }
        }

        private static void After(bool __state, bool __result)
        {
            if (!__state || active == null || active.Error != null) return;
            try { active.after(__result); }
            catch (Exception exception) { active.Error = exception.ToString(); }
        }

        public void Dispose()
        {
            harmony.UnpatchAll(HarmonyId);
            if (ReferenceEquals(active, this)) active = null;
        }
    }
}
