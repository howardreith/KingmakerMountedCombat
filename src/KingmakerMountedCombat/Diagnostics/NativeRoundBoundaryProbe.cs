using System;
using System.Linq;
using System.Reflection;
using Harmony12;
using Kingmaker;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // One selected fixture callback after the engine advances the round, before
    // ChooseNextUnit resumes selecting a successor. No round/context/resource write.
    internal sealed class NativeRoundBoundaryProbe : IDisposable
    {
        private const string HarmonyId = "KingmakerMountedCombat.Diagnostics.Chunk7RoundBoundary";
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        private static NativeRoundBoundaryProbe active;
        private HarmonyInstance harmony;
        private readonly JArray events = new JArray(), errors = new JArray();
        private bool disposed, installed, installationStarted;
        internal Action<CombatController> AfterNativeRound { get; set; }
        internal NativeRoundBoundaryProbe() { }
        internal void Install()
        {
            if (disposed) throw new ObjectDisposedException(nameof(NativeRoundBoundaryProbe));
            if (installed) return;
            if (installationStarted) throw new InvalidOperationException("A failed observer installation retains cleanup custody and cannot be replayed.");
            if (active != null) throw new InvalidOperationException("Another observer already owns this native boundary.");
            installationStarted = true;
            // Native contract validation; no patch ownership exists before the Harmony boundary.
            if (typeof(CombatController).Assembly.ManifestModule.ModuleVersionId !=
                new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"))
                throw new InvalidOperationException("Native round boundary observer identity or ownership differs.");
            var method = typeof(CombatController).GetMethods(Flags).Single(m => m.MetadataToken == 0x06000BD3);
            if (method.Name != "StartRound" || method.ReturnType != typeof(void) || method.GetParameters().Length != 0)
                throw new MissingMemberException("Pinned native StartRound signature differs.");
            harmony = HarmonyInstance.Create(HarmonyId); active = this;
            harmony.Patch(method, null, new HarmonyMethod(typeof(NativeRoundBoundaryProbe).GetMethod(nameof(After), Flags)) { prioritiy = Priority.First });
            installed = true;
        }
        private static void After(CombatController __instance)
        {
            var scope = active;
            if (scope == null || scope.disposed || !scope.installed) return;
            try
            {
                if (scope.events.Count >= 16) throw new InvalidOperationException("Native round boundary observation bound exceeded.");
                scope.events.Add(new JObject { ["round"] = __instance.RoundNumber, ["roundStartTicks"] = __instance.RoundStartTime.Ticks,
                    ["frame"] = Time.frameCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
                    ["nativeCurrentActor"] = __instance.CurrentTurn?.Unit?.UniqueId });
                scope.AfterNativeRound?.Invoke(__instance);
            }
            catch (Exception error) { scope.errors.Add(error.ToString()); }
        }
        internal JObject Capture() => new JObject { ["method"] = "CombatController.StartRound", ["token"] = "06000BD3",
            ["moduleMvid"] = typeof(CombatController).Assembly.ManifestModule.ModuleVersionId.ToString(),
            ["events"] = events.DeepClone(), ["errors"] = CaptureErrors() };
        private JArray CaptureErrors()
        {
            var result = (JArray)errors.DeepClone();
            if (!installed) result.Add("Native observer installation did not complete.");
            return result;
        }
        public void Dispose()
        {
            if (disposed) return;
            AfterNativeRound = null;
            harmony?.UnpatchAll(HarmonyId);
            disposed = true; if (ReferenceEquals(active, this)) active = null;
        }
    }
}
