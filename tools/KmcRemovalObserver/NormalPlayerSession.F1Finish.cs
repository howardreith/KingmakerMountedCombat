using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony12;
using Kingmaker;
using Kingmaker.Assets.UI.LevelUp;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityModManagerNet;

namespace KmcRemovalObserver
{
    // Explicit external F1 mode only. No KMC runtime request, permanent save,
    // blueprint replacement, XP, resource or original archive mutation.
    internal sealed partial class NormalPlayerSession
    {
        private const string F1Feat = "a7d2e94c6f1b4830b5c9e28d17f3a604";
        private const string F1Toggle = "3c81f5a02be74d96a4e07d1c92b6f385";
        private HarmonyInstance f1SaveGuard;
        private static NormalPlayerSession f1Active;
        private object f1FirstController;
        private bool f1Open, f1Removed, f1Fresh, f1On, f1Off, f1CreatorsDone;
        private int f1Closed;
        private double f1NextRead;
        private ActivatableAbility f1ToggleFact;
        private bool? f1LastOn;
        private readonly JArray f1Events = new JArray();
        private int f1RefreshEvents, f1RemovedReads;
        private JObject f1SelectedBefore, f1RemovedRow, f1Reselected, f1FreshMounted, f1Comparison, f1PreSyncStale;
        private bool f1SelectedRowLeft, f1SelectedNavigation, f1ReselectedRowLeft, f1ReselectedNavigation, f1RemovedStale, f1ComparisonOverlap;
        private object f1HeldSelection;
        private int f1HeldIndex;
        private readonly HashSet<string> f1ComparisonFeatures = new HashSet<string>();

        private void ArmF1Finish()
        {
            f1Active = this;
            f1SaveGuard = HarmonyInstance.Create("KmcRemovalObserver.F1Finish");
            var method = typeof(SaveManager).GetMethod("PrepareSave", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(SaveInfo) }, null);
            if (method == null || method.MetadataToken != 0x06008025) throw new InvalidOperationException("F1 save guard contract differs.");
            f1SaveGuard.Patch(method, new HarmonyMethod(typeof(NormalPlayerSession).GetMethod(nameof(F1DenySave), BindingFlags.Static | BindingFlags.NonPublic)), null, null);
            // Ahead of KMC's own postfix, so this sees what the native refresh alone left behind.
            var afterRefresh = new HarmonyMethod(typeof(NormalPlayerSession).GetMethod(nameof(F1ScrollAfter), BindingFlags.Static | BindingFlags.NonPublic));
            afterRefresh.prioritiy = Priority.First;
            f1SaveGuard.Patch(typeof(CharBSelectorLayer).GetMethod("FillFeatureDataLight"),
                new HarmonyMethod(typeof(NormalPlayerSession).GetMethod(nameof(F1ScrollBefore), BindingFlags.Static | BindingFlags.NonPublic)),
                afterRefresh, null);
            Write("f1-creator-ready", new JObject { ["instructions"] = "Keep unavailable feats shown throughout. Mobility1, select Mounted Combat, Next and Back; remove to0 and inspect the row; regain, reselect, Next and Back; cancel; fresh preview, select two plain native feats in turn; cancel again. No campaign entry." });
        }
        private static bool F1DenySave() { f1Active?.Fail("F1 finishing mode forbids every native save request."); return false; }
        private static void F1ScrollBefore(CharBSelectorLayer __instance, out float __state) { __state = F1ScrollPosition(__instance); }
        private static float F1ScrollPosition(CharBSelectorLayer layer) => layer.GetComponentInParent<ScrollRect>()?.verticalNormalizedPosition ?? -99f;
        private static void F1ScrollAfter(CharBSelectorLayer __instance, bool refreshSort, float __state)
        {
            var active = f1Active;
            if (active == null) return;
            active.ObserveF1NativeRefreshResidue(__instance);
            if (active.f1RefreshEvents++ >= 30) return;
            active.Write("f1-native-light-refresh-scroll", new JObject { ["sorted"] = refreshSort, ["before"] = __state, ["after"] = F1ScrollPosition(__instance), ["layer"] = __instance.name });
        }
        private void ObserveF1NativeRefreshResidue(CharBSelectorLayer layer)
        {
            var state = layer.CurrentSelectionState;
            if (f1PreSyncStale != null || f1HeldSelection == null || state == null || state.SelectedItem != null || layer.SelectorItems == null ||
                !ReferenceEquals(state.Selection, f1HeldSelection) || state.Index != f1HeldIndex) return;
            var stale = layer.SelectorItems.Where(r => r != null && r.Toggle != null && r.Toggle.IsActive() && r.Toggle.isOn && r.Feature?.Feature?.AssetGuid == F1Feat).ToArray();
            if (stale.Length == 0) return;
            f1PreSyncStale = new JObject { ["rows"] = new JArray(stale.Select(F1RowState)), ["seconds"] = clock.Elapsed.TotalSeconds };
            Write("f1-native-refresh-left-stale-row", (JObject)f1PreSyncStale.DeepClone());
        }
        private static JObject F1RowState(CharBuildSelectorItem row)
        {
            var toggle = row.Toggle;
            var group = toggle.group;
            var state = row.FeatureSelection;
            var graphic = toggle.graphic;
            var renderer = graphic == null ? null : typeof(Graphic).GetProperty("canvasRenderer").GetValue(graphic, null);
            var label = typeof(CharBuildSelectorItem).GetField("m_ItemName", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(row);
            return new JObject
            {
                ["featureGuid"] = row.Feature?.Feature?.AssetGuid,
                ["name"] = row.Feature?.Name,
                ["selectionGuid"] = (state?.Selection as Kingmaker.Blueprints.BlueprintScriptableObject)?.AssetGuid,
                ["selectionIndex"] = state == null ? (int?)null : state.Index,
                ["toggleIsOn"] = toggle.isOn,
                ["interactable"] = toggle.interactable,
                ["groupAllowSwitchOff"] = group == null ? (bool?)null : group.allowSwitchOff,
                ["groupAnyOn"] = group == null ? (bool?)null : group.AnyTogglesOn(),
                ["graphicAlpha"] = renderer == null ? (float?)null : (float)renderer.GetType().GetMethod("GetAlpha").Invoke(renderer, null),
                ["selectedInCurrentPreview"] = state != null && state.SelectedItem != null && state.SelectedItem == row.Feature,
                ["labelColor"] = label == null ? null : label.GetType().GetProperty("color").GetValue(label, null).ToString(),
                ["labelSelectedColor"] = typeof(CharBuildSelectorItem).GetField("m_SelectedColor", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(row).ToString(),
                ["labelNonSelectedColor"] = typeof(CharBuildSelectorItem).GetField("m_NonSelectedColor", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(row).ToString()
            };
        }
        private void ObserveF1FirstCreatorRow(Kingmaker.UnitLogic.Class.LevelUp.LevelUpController controller, int rank, CharBuildSelectorItem[] rows)
        {
            var row = rows.FirstOrDefault(r => r.Feature?.Feature?.AssetGuid == F1Feat && r.FeatureSelection != null &&
                (f1HeldSelection == null || (ReferenceEquals(r.FeatureSelection.Selection, f1HeldSelection) && r.FeatureSelection.Index == f1HeldIndex)));
            var selected = controller.State.Selections.Any(s => s.SelectedItem?.Feature?.AssetGuid == F1Feat);
            var bound = row != null && controller.State.Selections.Any(s => ReferenceEquals(s, row.FeatureSelection));
            var selectedRow = rank >= 1 && selected && bound && row.Toggle.isOn;
            if (f1SelectedBefore == null)
            {
                if (!selectedRow) return;
                f1SelectedBefore = F1RowState(row);
                f1HeldSelection = row.FeatureSelection.Selection;
                f1HeldIndex = row.FeatureSelection.Index;
                Write("f1-row-selected-before-removal", (JObject)f1SelectedBefore.DeepClone());
                return;
            }
            if (!f1Removed)
            {
                if (row == null && rank >= 1) f1SelectedRowLeft = true;
                if (selectedRow && f1SelectedRowLeft && !f1SelectedNavigation)
                {
                    f1SelectedNavigation = true;
                    Write("f1-selected-row-returned-after-navigation", F1RowState(row));
                }
            }
            if (rank == 0 && !selected && bound && !row.Toggle.interactable)
            {
                // Three reads a fifth of a second apart: the layout and the 0.1 s crossfade have settled.
                if (++f1RemovedReads >= 3)
                {
                    f1RemovedRow = F1RowState(row);
                    f1RemovedStale |= (bool)f1RemovedRow["toggleIsOn"] || (float?)f1RemovedRow["graphicAlpha"] >= 0.01f;
                    if (!f1Removed)
                    {
                        f1Removed = true;
                        Check(true, "f1-rank-removal-rebinds-current-preview-disabled-row-and-clears-selection");
                        Write("f1-rank-removed", (JObject)f1RemovedRow.DeepClone());
                    }
                }
            }
            else f1RemovedReads = 0;
            if (!f1Removed) return;
            if (selectedRow)
            {
                if (f1Reselected == null)
                {
                    f1Reselected = F1RowState(row);
                    Write("f1-row-reselected", (JObject)f1Reselected.DeepClone());
                }
                else if (f1ReselectedRowLeft && !f1ReselectedNavigation)
                {
                    f1ReselectedNavigation = true;
                    Write("f1-reselected-row-returned-after-navigation", F1RowState(row));
                }
            }
            if (f1Reselected != null && row == null) f1ReselectedRowLeft = true;
        }
        private void ObserveF1FreshCreatorRows(Kingmaker.UnitLogic.Class.LevelUp.LevelUpController controller, int rank, CharBuildSelectorItem[] rows)
        {
            var mounted = rows.FirstOrDefault(r => r.Feature?.Feature?.AssetGuid == F1Feat);
            if (rank == 0 && mounted != null)
            {
                if (f1FreshMounted == null) Write("f1-fresh-mounted-row", F1RowState(mounted));
                f1FreshMounted = F1RowState(mounted);
            }
            var on = rows.Where(r => r.Toggle.isOn && r.FeatureSelection != null && controller.State.Selections.Any(s => ReferenceEquals(s, r.FeatureSelection))).ToArray();
            var comparison = on.FirstOrDefault(r => r.Feature?.Feature?.AssetGuid != F1Feat && r.FeatureSelection.SelectedItem != null && r.FeatureSelection.SelectedItem == r.Feature);
            if (comparison != null)
            {
                f1Comparison = F1RowState(comparison);
                f1Comparison["activeOnRows"] = on.Length;
                f1ComparisonOverlap |= on.Length != 1;
                if (f1ComparisonFeatures.Add((string)f1Comparison["featureGuid"])) Write("f1-fresh-comparison-row", (JObject)f1Comparison.DeepClone());
                f1Comparison["distinctFeaturesSelected"] = f1ComparisonFeatures.Count;
                f1Comparison["overlapSeen"] = f1ComparisonOverlap;
            }
        }
        private static bool F1ScrollFixPresent(UnityModManager.ModEntry[] entries)
        {
            var scroll = entries.SingleOrDefault(e => e.Info.Id == "KingmakerLevelUpSelectionScrollFix");
            var assembly = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(a => a.GetName().Name == "KingmakerLevelUpSelectionScrollFix");
            return entries.Length == 3 && scroll != null && scroll.Active && scroll.Info.Version == "1.0.0" && assembly != null &&
                RemovalObserverSession.Sha256File(assembly.Location) == "ca5c1cf454d1a66e44dc067dae2023c1d13cca3143730545e1ec5b317e440136";
        }
        private bool ObserveF1Creators(Game game)
        {
            if (f1CreatorsDone) return true;
            if (game?.CurrentlyLoadedArea != null) { Fail("F1 creator phase entered a campaign before two cancellations."); return false; }
            var controller = game?.UI?.CharacterBuildController?.LevelUpController;
            var open = controller != null;
            if (open && !f1Open)
            {
                if (f1FirstController == null) f1FirstController = controller;
                f1Events.Add(new JObject { ["event"] = "creator-open", ["ordinal"] = f1Closed + 1, ["seconds"] = clock.Elapsed.TotalSeconds });
            }
            if (!open && f1Open)
            {
                f1Closed++;
                f1Events.Add(new JObject { ["event"] = "creator-cancel", ["ordinal"] = f1Closed, ["seconds"] = clock.Elapsed.TotalSeconds });
                Write("f1-creator-cancelled", new JObject { ["closed"] = f1Closed });
            }
            f1Open = open;
            if (open && controller.Preview != null && clock.Elapsed.TotalSeconds >= f1NextRead)
            {
                f1NextRead = clock.Elapsed.TotalSeconds + .2;
                var rank = controller.Preview.Stats.SkillMobility.BaseValue;
                if (f1Closed == 1 && !f1Fresh)
                {
                    var feat = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(F1Feat);
                    f1Fresh = !ReferenceEquals(f1FirstController, controller) && controller.Preview.Progression.Features.GetFact(feat) == null &&
                        !controller.State.Selections.Any(s => s.SelectedItem?.Feature?.AssetGuid == F1Feat);
                    Check(f1Fresh, "f1-fresh-controller-has-no-inherited-MountedCombat-fact-or-selection");
                    if (!f1Fresh) { Complete("FAIL"); return false; }
                }
                var rows = UnityEngine.Object.FindObjectsOfType<CharBuildSelectorItem>().Where(r => r.gameObject.activeInHierarchy && r.Toggle != null).ToArray();
                if (ReferenceEquals(controller, f1FirstController)) ObserveF1FirstCreatorRow(controller, rank, rows);
                else ObserveF1FreshCreatorRows(controller, rank, rows);
            }
            observations["f1Creators"] = new JObject { ["events"] = f1Events, ["closed"] = f1Closed, ["rankRemoved"] = f1Removed, ["fresh"] = f1Fresh,
                ["nativeRefreshResidue"] = f1PreSyncStale, ["selectedBefore"] = f1SelectedBefore, ["selectedNavigation"] = f1SelectedNavigation, ["removedRow"] = f1RemovedRow, ["removedRowStaleRead"] = f1RemovedStale,
                ["reselected"] = f1Reselected, ["reselectedNavigation"] = f1ReselectedNavigation, ["freshMounted"] = f1FreshMounted, ["comparison"] = f1Comparison };
            if (f1Closed < 2) return false;
            Check(f1Removed && f1Fresh, "f1-two-cancellations-follow-rank-removal-and-genuinely-fresh-preview");
            Check(f1SelectedBefore != null, "f1-mounted-combat-row-selected-before-rank-removal");
            Check(f1PreSyncStale != null, "f1-native-refresh-left-the-stale-row-before-the-clear");
            Check(f1RemovedRow != null && !f1RemovedStale, "f1-removed-row-native-state-unselected-after-layout");
            Check(f1Reselected != null, "f1-reselected-row-native-state-selected-after-regaining-rank");
            Check(f1SelectedNavigation && f1ReselectedNavigation, "f1-selected-rows-survive-next-and-back");
            Check(f1FreshMounted != null && !(bool)f1FreshMounted["toggleIsOn"], "f1-fresh-mounted-combat-row-native-state-unselected");
            Check(f1Comparison != null && f1ComparisonFeatures.Count >= 2 && !f1ComparisonOverlap, "f1-fresh-native-comparison-rows-select-exclusively");
            if (errors.Count != 0) { Complete("FAIL"); return false; }
            f1CreatorsDone = true;
            return true;
        }
        private void GrantF1PresentationFeature()
        {
            var feature = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(F1Feat);
            Check(feature != null && rider.UniqueId == "73681ef2-b281-4bc6-bc05-2de68c1616cb", "f1-presentation-setup-is-exact-feature-and-disposable-rider");
            if (errors.Count != 0) throw new InvalidOperationException("F1 setup identity rejected.");
            var had = rider.Descriptor.Progression.Features.GetFact(feature) != null;
            if (!had) rider.Descriptor.Progression.Features.AddFeature(feature);
            f1ToggleFact = rider.Descriptor.ActivatableAbilities.RawFacts.OfType<ActivatableAbility>().SingleOrDefault(f => f.Blueprint.AssetGuid == F1Toggle);
            Check(f1ToggleFact != null, "f1-native-feature-grant-provides-real-reaction-toggle");
            observations["f1PresentationSetup"] = new JObject { ["riderId"] = rider.UniqueId, ["featureGuid"] = F1Feat, ["grantedInMemory"] = !had,
                ["ordinaryAcquisitionProven"] = false, ["saveWriteAuthorized"] = false, ["toggleGuid"] = F1Toggle };
        }
        private void ObserveF1Presentation(Game game)
        {
            if (game.Player.IsInCombat || game.CurrentlyLoadedArea.AssetGuidThreadSafe != request.Archive.Area) { Fail("F1 presentation left its declared quiet scene."); return; }
            var on = f1ToggleFact.IsOn;
            if (f1LastOn != on)
            {
                if (f1LastOn.HasValue) { if (on) f1On = true; else f1Off = true; }
                f1LastOn = on;
                Write("f1-reaction-state", new JObject { ["on"] = on, ["seconds"] = clock.Elapsed.TotalSeconds });
            }
            observations["f1Reaction"] = new JObject { ["observedOnTransition"] = f1On, ["observedOffTransition"] = f1Off, ["on"] = on };
        }
        internal void DrawF1Finish()
        {
            if (!roles.F1Finish || completed) return;
            GUILayout.Label("F1 finishing observer: " + f1Closed + " cancelled creators; rank removal=" + f1Removed + "; fresh=" + f1Fresh + "; stage=" + stage);
            GUILayout.Label("First creator: selected=" + (f1SelectedBefore != null) + "; navigated=" + f1SelectedNavigation + "; native residue=" + (f1PreSyncStale != null) + "; removed row stale=" + f1RemovedStale +
                "; reselected=" + (f1Reselected != null) + "; navigated=" + f1ReselectedNavigation + ". Fresh creator: row=" + (f1FreshMounted != null) + "; comparison features=" + f1ComparisonFeatures.Count);
            GUILayout.Label("Presentation grant is disposable in-memory test setup. No ordinary acquisition or save writeback is claimed.");
            if (stage != 9) return;
            GUILayout.Label("Capture reaction on/off and tooltip, Charge tooltip, then ordinary KMC page after actions. This button only closes the supervised native run.");
            GUI.enabled = f1On && f1Off;
            if (GUILayout.Button("Finish F1 native observation and exit"))
            {
                Check(f1On && f1Off, "f1-native-reaction-on-and-off-transitions-observed");
                Finish(Game.Instance);
            }
            GUI.enabled = true;
        }
    }
}
