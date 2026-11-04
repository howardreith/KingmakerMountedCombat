using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Harmony12;
using Kingmaker;
using Kingmaker.UI.LevelUp;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class RuntimeAutomationHost
    {
        private FinalizeF1Timing finalizeF1Timing;
        private bool finalizeF1InBuilder;
        private int finalizeF1Cancelled;
        private void UpdateFinalizeF1Creation()
        {
            if (Game.Instance?.CurrentlyLoadedArea != null)
                throw new InvalidOperationException("F1 creator session must not enter a campaign area.");
            if (finalizeF1Timing == null) finalizeF1Timing = new FinalizeF1Timing(logger.Info);
            finalizeF1Timing.Update();
            var builder = Game.Instance?.UI?.CharacterBuildController;
            var open = builder != null && builder.LevelUpController != null;
            if (open && !finalizeF1InBuilder)
                logger.Info("F1 fresh creator opened; session=" + (finalizeF1Cancelled + 1));
            if (!open && finalizeF1InBuilder)
            {
                finalizeF1Cancelled++;
                logger.Info("F1 creator cancelled; session=" + finalizeF1Cancelled);
            }
            finalizeF1InBuilder = open;
            // This PASS establishes cancellation/lifetime only. UI correctness and timing require the retained observations.
            if (finalizeF1Cancelled < 2) return;
            finalizeF1Timing.Dispose();
            subscenarioResults = new[] { new RuntimeSubscenarioResult {
                Name = "finalize-f1-creation-ui", Status = "PASS", AssertionPassCount = 2,
                AssertionFailCount = 0, Errors = new string[0] } };
            Complete("PASS", new string[0]);
        }
    }

    // Diagnostic-only input-to-next-ready-frame sampling. No gameplay or entitlement changes.
    internal sealed class FinalizeF1Timing : IDisposable
    {
        private static FinalizeF1Timing active;
        private static readonly FieldInfo PreviewPending = typeof(Kingmaker.UnitLogic.Class.LevelUp.LevelUpController).GetField("m_RecalculatePreview", BindingFlags.Instance | BindingFlags.NonPublic);
        private readonly HarmonyInstance harmony;
        private readonly Action<string> log;
        private long started;
        private int inputFrame, sequence;
        private string input;
        internal FinalizeF1Timing(Action<string> log)
        {
            this.log = log;
            harmony = HarmonyInstance.Create("KingmakerMountedCombat.Diagnostics.FinalizeF1");
            active = this;
            try
            {
                foreach (var name in new[] { "SetFeature", "SpendSkillPoint", "Next", "Back" })
                {
                    var method = typeof(CharacterBuildController).GetMethod(name, BindingFlags.Instance | BindingFlags.Public);
                    if (method == null) throw new MissingMethodException(name);
                    harmony.Patch(method, new HarmonyMethod(typeof(FinalizeF1Timing).GetMethod(nameof(Input), BindingFlags.Static | BindingFlags.NonPublic)));
                }
            }
            catch { Dispose(); throw; }
        }
        private static void Input(MethodBase __originalMethod)
        {
            if (active == null) return;
            if (active.started != 0) active.log("F1 timing superseded: " + active.input);
            active.started = Stopwatch.GetTimestamp(); active.inputFrame = Time.frameCount;
            active.input = __originalMethod.Name;
        }
        internal void Update()
        {
            if (started == 0 || Time.frameCount <= inputFrame) return;
            var controller = Game.Instance?.UI?.CharacterBuildController?.LevelUpController;
            if (controller != null && PreviewPending != null && (bool)PreviewPending.GetValue(controller)) return;
            // UMM update runs before the next rendered frame. This is a cheap next-frame bound,
            // not a GPU presentation timestamp; screenshots and UI observations establish visibility separately.
            log("F1 timing input=" + input + ";sample=" + (++sequence) + ";nextFrameMs=" +
                ((Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency).ToString("F2", System.Globalization.CultureInfo.InvariantCulture) +
                ";frames=" + (Time.frameCount - inputFrame));
            started = 0;
        }
        public void Dispose()
        {
            harmony.UnpatchAll("KingmakerMountedCombat.Diagnostics.FinalizeF1");
            if (ReferenceEquals(active, this)) active = null;
        }
    }
}
