using System;
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
        private object f1FirstController, f1CurrentController;
        private bool f1Open, f1HadRank, f1Removed, f1Fresh, f1On, f1Off, f1CreatorsDone;
        private int f1Closed;
        private double f1NextRead;
        private ActivatableAbility f1ToggleFact;
        private bool? f1LastOn;
        private readonly JArray f1Events = new JArray();
        private int f1RefreshEvents;

        private void ArmF1Finish()
        {
            f1Active = this;
            f1SaveGuard = HarmonyInstance.Create("KmcRemovalObserver.F1Finish");
            var method = typeof(SaveManager).GetMethod("PrepareSave", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(SaveInfo) }, null);
            if (method == null || method.MetadataToken != 0x06008025) throw new InvalidOperationException("F1 save guard contract differs.");
            f1SaveGuard.Patch(method, new HarmonyMethod(typeof(NormalPlayerSession).GetMethod(nameof(F1DenySave), BindingFlags.Static | BindingFlags.NonPublic)), null, null);
            f1SaveGuard.Patch(typeof(CharBSelectorLayer).GetMethod("FillFeatureDataLight"),
                new HarmonyMethod(typeof(NormalPlayerSession).GetMethod(nameof(F1ScrollBefore), BindingFlags.Static | BindingFlags.NonPublic)),
                new HarmonyMethod(typeof(NormalPlayerSession).GetMethod(nameof(F1ScrollAfter), BindingFlags.Static | BindingFlags.NonPublic)), null);
            Write("f1-creator-ready", new JObject { ["instructions"] = "Mobility1 then remove to0; inspect disabled regroup; cancel; fresh preview has no inherited selection; cancel again. No campaign entry." });
        }
        private static bool F1DenySave() { f1Active?.Fail("F1 finishing mode forbids every native save request."); return false; }
        private static void F1ScrollBefore(CharBSelectorLayer __instance, out float __state) { __state = F1ScrollPosition(__instance); }
        private static float F1ScrollPosition(CharBSelectorLayer layer) => layer.GetComponentInParent<ScrollRect>()?.verticalNormalizedPosition ?? -99f;
        private static void F1ScrollAfter(CharBSelectorLayer __instance, bool refreshSort, float __state)
        {
            var active = f1Active;
            if (active == null || active.f1RefreshEvents++ >= 30) return;
            active.Write("f1-native-light-refresh-scroll", new JObject { ["sorted"] = refreshSort, ["before"] = __state, ["after"] = F1ScrollPosition(__instance), ["layer"] = __instance.name });
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
                f1CurrentController = controller;
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
                if (ReferenceEquals(f1CurrentController, f1FirstController) && rank >= 1) f1HadRank = true;
                if (ReferenceEquals(f1CurrentController, f1FirstController) && f1HadRank && rank == 0 && !f1Removed)
                {
                    var rows = UnityEngine.Object.FindObjectsOfType<CharBuildSelectorItem>();
                    var row = rows.FirstOrDefault(r => r.gameObject.activeInHierarchy && r.Feature?.Feature?.AssetGuid == F1Feat);
                    if (row != null && !row.Toggle.interactable && row.FeatureSelection != null &&
                        controller.State.Selections.Any(s => ReferenceEquals(s, row.FeatureSelection)) &&
                        !controller.State.Selections.Any(s => s.SelectedItem?.Feature?.AssetGuid == F1Feat))
                    {
                        f1Removed = true;
                        Check(true, "f1-rank-removal-rebinds-current-preview-disabled-row-and-clears-selection");
                        Write("f1-rank-removed", new JObject { ["rank"] = rank, ["disabled"] = true, ["currentPreviewBound"] = true });
                    }
                }
            }
            observations["f1Creators"] = new JObject { ["events"] = f1Events, ["closed"] = f1Closed, ["rankRemoved"] = f1Removed, ["fresh"] = f1Fresh };
            if (f1Closed < 2) return false;
            Check(f1Removed && f1Fresh, "f1-two-cancellations-follow-rank-removal-and-genuinely-fresh-preview");
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
