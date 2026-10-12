using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony12;
using Kingmaker;
using Kingmaker.Controllers.Combat;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Installed before cold native LoadGame. Observation only: no native argument,
    // return, resource, preparation or patch ordering contract is rewritten.
    internal sealed class NativeArchiveRestorationProbe : IDisposable
    {
        private const string HarmonyId = "KingmakerMountedCombat.Diagnostics.Chunk7Restoration";
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        private const int MaximumEvents = 2048;
        private static NativeArchiveRestorationProbe active;
        private readonly MountedPersistenceService persistence;
        private readonly MountedWorldService world;
        private HarmonyInstance harmony;
        private readonly JArray events = new JArray(), hooks = new JArray();
        private int dropped, errors;
        private bool disposed, installed, installationStarted;
        internal bool Complete => installed && dropped == 0 && errors == 0;
        internal NativeArchiveRestorationProbe(MountedPersistenceService persistence, MountedWorldService world = null)
        {
            this.persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            this.world = world;
        }
        internal void Install()
        {
            if (disposed) throw new ObjectDisposedException(nameof(NativeArchiveRestorationProbe));
            if (installed) return;
            if (installationStarted) throw new InvalidOperationException("A failed observer installation retains cleanup custody and cannot be replayed.");
            if (active != null) throw new InvalidOperationException("Another observer already owns this native boundary.");
            installationStarted = true;
            // Native contract validation; no patch ownership exists before the Harmony boundary.
            if (Game.Instance?.CurrentlyLoadedArea != null)
                throw new InvalidOperationException("Cold restoration observation must begin before any native world is loaded.");
            if (typeof(UnitEntityData).Assembly.ManifestModule.ModuleVersionId != new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"))
                throw new InvalidOperationException("Native restoration observer MVID differs.");
            harmony = HarmonyInstance.Create(HarmonyId); active = this;
            Patch(typeof(TurnController), 0x06000C3C, "PrepareBefore", "PrepareAfter");
            Patch(typeof(UnitCombatState), 0x0600939D, "RoundBefore", "RoundAfter");
            PatchProduct(nameof(NativeCombatActorPersistence.RestoreActor), "RestoreActorBefore", "RestoreActorAfter");
            PatchProduct(nameof(NativeCombatActorPersistence.RestoreDebt), "RestoreDebtBefore", "RestoreDebtAfter");
            if (world != null)
            {
                Install(typeof(MountedPersistenceService).GetMethod("TryRestoreArchiveCombat", Flags), "SharedWorldBefore", "SharedWorldAfter");
                Install(typeof(MountedPersistenceService).GetMethod("RestoreArchivePresentation", Flags), "PresentationBefore", "PresentationAfter");
            }
            installed = true;
        }
        private void Patch(Type type, int token, string prefix, string postfix)
        {
            var method = type.GetMethods(Flags).SingleOrDefault(m => m.MetadataToken == token);
            if (method == null) throw new MissingMethodException(type.FullName, token.ToString("X8"));
            Install(method, prefix, postfix);
        }
        private void PatchProduct(string name, string prefix, string postfix)
        {
            var method = typeof(NativeCombatActorPersistence).GetMethod(name, Flags);
            if (method == null) throw new MissingMethodException(typeof(NativeCombatActorPersistence).FullName, name);
            Install(method, prefix, postfix);
        }
        private void Install(MethodInfo method, string prefix, string postfix)
        {
            if (method == null) throw new MissingMethodException("Required native archive observation boundary is absent.");
            harmony.Patch(method, prefix == null ? null : new HarmonyMethod(typeof(Hooks).GetMethod(prefix, Flags)) { prioritiy = Priority.First },
                new HarmonyMethod(typeof(Hooks).GetMethod(postfix, Flags)) { prioritiy = Priority.Last });
            hooks.Add(new JObject { ["declaringType"] = method.DeclaringType.FullName, ["method"] = method.Name,
                ["token"] = method.MetadataToken.ToString("X8"), ["moduleMvid"] = method.Module.ModuleVersionId.ToString(),
                ["prefix"] = prefix, ["postfix"] = postfix });
        }
        private void Record(string kind, UnitEntityData actor)
        {
            try
            {
                if (events.Count >= MaximumEvents) { dropped++; return; }
                events.Add(new JObject { ["kind"] = kind, ["actorId"] = actor?.UniqueId, ["frame"] = Time.frameCount,
                    ["gameTicks"] = Game.Instance?.TimeController?.GameTime.Ticks,
                    ["restorePhase"] = persistence.ArchiveRestorePhase,
                    ["restorationPending"] = persistence.CombatRestorationPending,
                    ["debt"] = actor == null ? null : JObject.FromObject(MountedPersistenceService.CaptureActor(actor), MountedSaveCodec.CreateSerializer()),
                    ["prepared"] = actor?.CombatState.Prepared, ["inCombat"] = actor?.IsInCombat });
            }
            catch { errors++; }
        }
        private void RecordWorld(string kind)
        {
            try
            {
                if (events.Count >= MaximumEvents) { dropped++; return; }
                events.Add(new JObject { ["kind"] = kind, ["frame"] = Time.frameCount,
                    ["restorePhase"] = persistence.ArchiveRestorePhase,
                    ["snapshot"] = Chunk7ArchiveNativeSnapshot.Capture(world, persistence.LoadedArchive) });
            }
            catch { errors++; }
        }
        internal int Count(string kind, IEnumerable<string> actors)
        {
            var ids = new HashSet<string>(actors, StringComparer.Ordinal);
            return events.Count(e => (string)e["kind"] == kind && ids.Contains((string)e["actorId"]));
        }
        internal JObject Capture() => new JObject { ["contract"] = "chunk7-native-restoration-entry-observer-v1",
            ["events"] = events.DeepClone(), ["hooks"] = hooks.DeepClone(), ["maximumEvents"] = MaximumEvents,
            ["dropped"] = dropped, ["errors"] = errors + (installed ? 0 : 1), ["complete"] = Complete,
            ["installedBeforeWorldLoad"] = true, ["prepareMetric"] = "native-method-entries-including-production-refused-attempts" };
        public void Dispose()
        {
            if (disposed) return;
            harmony?.UnpatchAll(HarmonyId);
            if (ReferenceEquals(active, this)) active = null;
            disposed = true;
        }
        private static class Hooks
        {
            internal static void PrepareBefore(TurnController __instance) => active?.Record("prepare-before", __instance.Unit);
            internal static void PrepareAfter(TurnController __instance) => active?.Record("prepare-after", __instance.Unit);
            internal static void RoundBefore(UnitCombatState __instance) => active?.Record("round-before", __instance.Unit);
            internal static void RoundAfter(UnitCombatState __instance) => active?.Record("round-after", __instance.Unit);
            internal static void RestoreActorBefore(UnitEntityData unit) => active?.Record("restore-actor-before", unit);
            internal static void RestoreActorAfter(UnitEntityData unit) => active?.Record("restore-actor-after", unit);
            internal static void RestoreDebtBefore(UnitEntityData unit) => active?.Record("restore-debt-before", unit);
            internal static void RestoreDebtAfter(UnitEntityData unit) => active?.Record("restore-debt-after", unit);
            internal static void SharedWorldBefore(MountedPersistenceService __instance, out string __state)
            {
                __state = ReferenceEquals(active?.persistence, __instance) ? __instance.ArchiveRestorePhase : null;
            }
            internal static void SharedWorldAfter(MountedPersistenceService __instance, string __state)
            {
                if (ReferenceEquals(active?.persistence, __instance) && __state != null &&
                    __state != "shared-and-pair-participation-restored" &&
                    __instance.ArchiveRestorePhase == "shared-and-pair-participation-restored")
                    active.RecordWorld("shared-world-restored");
            }
            internal static void PresentationBefore(MountedPersistenceService __instance, out string __state)
            {
                __state = ReferenceEquals(active?.persistence, __instance) ? __instance.ArchiveRestorePhase : null;
            }
            internal static void PresentationAfter(MountedPersistenceService __instance, bool __result, string __state)
            {
                if (__result && ReferenceEquals(active?.persistence, __instance) && __state != null &&
                    __state != "all-pair-presentation-restored" && __instance.ArchiveRestorePhase == "all-pair-presentation-restored")
                    active.RecordWorld("all-owner-presentation-restored");
            }
        }
    }
}
