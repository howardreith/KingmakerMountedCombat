using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Harmony12;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.View;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Observe the actual call, UI notification and return. A managed stack is
    // supplementary evidence: an omitted frame cannot replace this call bracket.
    internal sealed class NativeViewAttachmentProbe : IDisposable
    {
        private const string HarmonyId = "KingmakerMountedCombat.Diagnostics.ViewAttachment";
        private const BindingFlags Flags = BindingFlags.Static | BindingFlags.NonPublic;
        private static NativeViewAttachmentProbe active;
        private readonly UnitEntityData actor;
        private readonly HarmonyInstance harmony;
        private readonly MethodInfo method;
        private readonly JArray events = new JArray(), errors = new JArray();
        private EntityViewBase pendingView;
        private int invocation, pending;
        private bool disposed;

        internal NativeViewAttachmentProbe(UnitEntityData actor)
        {
            if (active != null || actor == null) throw new InvalidOperationException("View attachment observer already active or actor absent.");
            this.actor = actor;
            method = (MethodInfo)typeof(UnitEntityData).Module.ResolveMethod(0x06007E9D);
            var parameters = method.GetParameters();
            if (method.Module.ModuleVersionId != new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7") ||
                method.DeclaringType != typeof(EntityDataBase) || method.Name != "AttachToViewOnLoad" ||
                method.ReturnType != typeof(void) || parameters.Length != 1 ||
                parameters[0].Name != "view" || parameters[0].ParameterType != typeof(EntityViewBase))
                throw new InvalidOperationException("Pinned native view attachment signature changed.");
            harmony = HarmonyInstance.Create(HarmonyId); active = this;
            try {
                harmony.Patch(method,
                    new HarmonyMethod(typeof(Hooks).GetMethod("Before", Flags)) { prioritiy = Priority.First },
                    new HarmonyMethod(typeof(Hooks).GetMethod("After", Flags)) { prioritiy = Priority.Last });
            }
            catch { Dispose(); throw; }
        }

        private void Record(string boundary, EntityViewBase view)
        {
            if (events.Count >= 16) throw new InvalidOperationException("View attachment observation bound exceeded.");
            events.Add(new JObject { ["sequence"] = events.Count + 1, ["boundary"] = boundary,
                ["invocation"] = pending, ["frame"] = Time.frameCount,
                ["actor"] = actor.UniqueId, ["actorObject"] = RuntimeHelpers.GetHashCode(actor),
                ["argumentView"] = view == null ? 0 : view.GetInstanceID(),
                ["currentView"] = actor.View == null ? 0 : actor.View.GetInstanceID(),
                ["bound"] = actor.View != null && actor.View.Data == actor });
        }
        private void Before(EntityViewBase view)
        {
            if (pending != 0 || view == null) throw new InvalidOperationException("Nested or missing native view attachment.");
            pending = ++invocation; pendingView = view; Record("attach-before", view);
        }
        private void After(EntityViewBase view)
        {
            if (pending == 0 || !ReferenceEquals(pendingView, view)) throw new InvalidOperationException("Unmatched native view return.");
            Record("attach-after", view); pending = 0; pendingView = null;
        }
        internal int Notification(UnitEntityData unit)
        {
            var observed = 0;
            Safe(() => {
                if (!ReferenceEquals(unit, actor) || pending == 0 || !ReferenceEquals(actor.View, pendingView))
                    throw new InvalidOperationException("View notification lacks its exact active native call.");
                Record("view-notification", pendingView); observed = pending;
            });
            return observed;
        }
        internal void Fault(Exception exception) { if (errors.Count < 8) errors.Add(exception.ToString()); }
        private void Safe(Action action) { try { action(); } catch (Exception exception) { Fault(exception); } }
        internal JObject Capture() => new JObject {
            ["contract"] = "native-view-attachment-call-v1", ["actor"] = actor.UniqueId,
            ["actorObject"] = RuntimeHelpers.GetHashCode(actor), ["closed"] = disposed, ["pending"] = pending,
            ["hook"] = new JObject { ["token"] = method.MetadataToken.ToString("x8"),
                ["method"] = method.Name, ["moduleMvid"] = method.Module.ModuleVersionId.ToString("D") },
            ["events"] = events.DeepClone(), ["errors"] = errors.DeepClone()
        };
        public void Dispose()
        {
            if (disposed) return;
            harmony.UnpatchAll(HarmonyId);
            if (ReferenceEquals(active, this)) active = null;
            disposed = true;
        }
        private static class Hooks
        {
            internal static void Before(EntityDataBase __instance, EntityViewBase view)
            { var probe = active; if (probe != null && ReferenceEquals(__instance, probe.actor)) probe.Safe(() => probe.Before(view)); }
            internal static void After(EntityDataBase __instance, EntityViewBase view)
            { var probe = active; if (probe != null && ReferenceEquals(__instance, probe.actor)) probe.Safe(() => probe.After(view)); }
        }
    }
}
