using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Diagnostics;
using System.Reflection;
using Harmony12;
using Kingmaker;
using Kingmaker.UI.LevelUp;
using Kingmaker.Assets.UI.LevelUp;
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
        private int visibleGeneration;
        private bool disposed;
        private CharacterBuildController cachedBuilder;
        private CharBFeatureSelector[] selectors;
        private static readonly FieldInfo LayoutPending = typeof(CharBSelectorLayer).GetField("m_UpdateLayout", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo ContentGroup = typeof(CharBSelectorLayer).GetField("ContentCanvasGroup");
        private static readonly PropertyInfo ContentAlpha = ContentGroup.FieldType.GetProperty("alpha");
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
        private static void Input(MethodBase __originalMethod, object[] __args)
        {
            if (active == null) return;
            if (active.started != 0) active.log("F1 timing superseded: " + active.input);
            active.started = Stopwatch.GetTimestamp(); active.inputFrame = Time.frameCount;
            active.input = __originalMethod.Name;
            var generation = ++active.visibleGeneration;
            if (__originalMethod.Name == "SetFeature")
            {
                var builder = Game.Instance?.UI?.CharacterBuildController;
                var selection = __args[0] as Kingmaker.UnitLogic.Class.LevelUp.FeatureSelectionState;
                if (builder != null && selection != null)
                    builder.StartCoroutine(active.ObserveVisibleFeatureFrame(builder, generation, active.started,
                        selection.Selection, selection.Index, selection.Level, __args[1]));
            }
        }

        private IEnumerator ObserveVisibleFeatureFrame(CharacterBuildController builder, int generation, long start,
            object selection, int index, int level, object selectedItem)
        {
            if (!ReferenceEquals(cachedBuilder, builder))
            {
                cachedBuilder = builder;
                selectors = builder.GetComponentsInChildren<CharBFeatureSelector>(true);
            }
            var deadline = Time.realtimeSinceStartup + 20f;
            while (!disposed && generation == visibleGeneration && Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForEndOfFrame();
                if (disposed || generation != visibleGeneration) yield break;
                var controller = builder.LevelUpController;
                if (controller == null) yield break;
                if ((bool)PreviewPending.GetValue(controller)) continue;
                foreach (var selector in selectors)
                {
                    if (selector == null || !selector.gameObject.activeInHierarchy) continue;
                    for (var pane = 0; pane < 2; pane++)
                    {
                    var layer = pane == 0 ? selector.SelectorLayerBody : selector.SelectorLayerBodySecond;
                    var state = layer?.CurrentSelectionState;
                    if (state == null || !ReferenceEquals(state.Selection, selection) || state.Index != index || state.Level != level ||
                        !ReferenceEquals(state.SelectedItem, selectedItem) ||
                        !controller.State.Selections.Any(current => ReferenceEquals(current, state)) ||
                        !layer.gameObject.activeInHierarchy ||
                        (bool)LayoutPending.GetValue(layer)) continue;
                    var group = ContentGroup.GetValue(layer);
                    if (group == null || (float)ContentAlpha.GetValue(group, null) < 0.99f) continue;
                    var rows = 0;
                    var bound = true;
                    foreach (var row in layer.SelectorItems)
                    {
                        if (row == null || !row.gameObject.activeInHierarchy) continue;
                        rows++;
                        bound &= ReferenceEquals(row.FeatureSelection, state);
                    }
                    if (!bound || rows == 0) continue;
                    log("F1 visible input=SetFeature;endpoint=end-of-rendered-frame-current-preview-bound-rows;rows=" + rows +
                        ";ms=" + ((Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency).ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
                    yield break;
                    }
                }
            }
            if (!disposed && generation == visibleGeneration) log("F1 visible input=SetFeature;endpoint-timeout=20s");
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
            disposed = true;
            visibleGeneration++;
            cachedBuilder = null;
            selectors = null;
            harmony.UnpatchAll("KingmakerMountedCombat.Diagnostics.FinalizeF1");
            if (ReferenceEquals(active, this)) active = null;
        }
    }
}
