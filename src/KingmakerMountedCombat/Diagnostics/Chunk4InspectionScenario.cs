using System;
using System.Reflection;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Group;
using Kingmaker.UI.Selection;
using Kingmaker.UI.ServiceWindow;
using Kingmaker.UI.ServiceWindow.CharacterScreen;
using Kingmaker.UnitLogic;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        private bool IsChunk4Inspection => request.Scenario == "chunk4-inspection-rt";
        private int chunk4InspectionStage;
        private int chunk4InspectionActor;
        private bool chunk4InspectionMountSent;
        private bool chunk4InspectionWindowOwned;
        private UnitEntityData chunk4InspectionOriginalGroupActor;
        private CharacterScreenController chunk4CharacterSheet;
        private FieldInfo chunk4CharacterBinding;
        private MethodInfo chunk4CharacterOpen;
        private MethodInfo chunk4CharacterClose;
        private PropertyInfo chunk4CharacterScreenIndex;
        private JObject chunk4InspectionBefore;
        private UnitEntityData Chunk4InspectedActor => chunk4InspectionActor == 0 ? rider : horse;

        private void BeginChunk4Inspection()
        {
            if (!settings.EnablePairedActivation || settings.EnableUnifiedMountedTurn || settings.EnablePairedCommandScheduler ||
                settings.EnableDiagnosticOverlay || playerAction.OverlayPresent)
                throw new InvalidOperationException("Independent inspection requires the accepted paired configuration.");
            CaptureIdleFixturePartyForCleanup();
            ordinaryAttackTrace = new NativeOrdinaryAttackTrace(rider, horse, combat, () => relationship.State.ToString());
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            chunk4CharacterBinding = typeof(CharacterScreenController).GetField("m_CurrentCharacter", flags);
            chunk4CharacterOpen = typeof(ServiceWindowController).GetMethod("OnHotKeyShowChracterScreen", flags);
            chunk4CharacterClose = typeof(ServiceWindowTabs).GetMethod("Hide", flags);
            chunk4CharacterScreenIndex = typeof(FullScreenTabsWindow).GetProperty("ScreenIndex", flags);
            if (chunk4CharacterBinding?.MetadataToken != 0x04003140 ||
                chunk4CharacterOpen?.MetadataToken != 0x06004768 ||
                typeof(GroupController).GetMethod("SelectUnit", flags)?.MetadataToken != 0x06003F29 ||
                chunk4CharacterClose?.MetadataToken != 0x0600477A ||
                chunk4CharacterScreenIndex?.GetGetMethod(true)?.MetadataToken != 0x060046D0)
                throw new MissingMemberException("Exact installed character-sheet inspection contract changed.");
            step = Phase3dHorseStep.Phase3gControls; ResetLeafClock();
        }

        private void TickChunk4Inspection()
        {
            var game = Game.Instance;
            var tabs = game.UI.ServiceWindow.WindowTabs;
            if (chunk4InspectionStage == 0)
            {
                if (game.IsPaused) { game.IsPaused = false; return; }
                if (!Chunk4PairedPlayIdle || rider.IsInCombat || horse.IsInCombat) return;
                if (turnBasedModeProbe == null) turnBasedModeProbe = new NativeModeTransitionProbe(false);
                if (!turnBasedModeProbe.TemporaryValueIsCurrent) { turnBasedModeProbe.DispatchTemporaryValueIfRequired(); return; }
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                if (relationship.State != RelationshipState.Mounted)
                {
                    if (!chunk4InspectionMountSent) chunk4InspectionMountSent = TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "inspection-pre-combat-mount");
                    return;
                }
                if (tabs.IsShow) throw new InvalidOperationException("Inspection fixture refuses to replace a pre-existing service window.");
                chunk4InspectionOriginalGroupActor = GroupController.Instance.GetCurrentCharacter();
                chunk4InspectionBefore = CaptureOrdinaryLiveState();
                chunk4CharacterSheet = tabs.SubWindowsList[1].SubWindow as CharacterScreenController;
                if (chunk4CharacterSheet == null) throw new InvalidOperationException("Native character tab is not the expected installed window.");
                chunk4InspectionWindowOwned = true;
                chunk4CharacterOpen.Invoke(game.UI.ServiceWindow, null);
                chunk4InspectionStage = 1; ResetLeafClock(); return;
            }
            observations["chunk4InspectionProgress"] = new JObject { ["stage"] = chunk4InspectionStage,
                ["actorIndex"] = chunk4InspectionActor, ["windowShown"] = tabs.IsShow,
                ["sheetShown"] = chunk4CharacterSheet.IsShow, ["nativeMode"] = game.CurrentMode.ToString(),
                ["boundActor"] = (chunk4CharacterBinding.GetValue(chunk4CharacterSheet) as UnitDescriptor)?.Unit.UniqueId,
                ["live"] = CaptureOrdinaryLiveState() };
            if (chunk4InspectionStage == 1)
            {
                if (!tabs.IsShow || !chunk4CharacterSheet.IsShow) return;
                GroupController.Instance.SelectUnit(Chunk4InspectedActor);
                chunk4InspectionStage = 2; ResetLeafClock(); return;
            }
            if (chunk4InspectionStage == 2)
            {
                var bound = chunk4CharacterBinding.GetValue(chunk4CharacterSheet) as UnitDescriptor;
                if (bound != Chunk4InspectedActor.Descriptor || leafClock.Elapsed.TotalSeconds < 0.35d) return;
                if (!tabs.IsShow || !chunk4CharacterSheet.IsShow || !chunk4CharacterSheet.gameObject.activeInHierarchy ||
                    GroupController.Instance.GetCurrentCharacter() != Chunk4InspectedActor || relationship.State != RelationshipState.Mounted)
                    throw new InvalidOperationException("Native independent inspection lost its actor binding, visible sheet or mounted pair.");
                var after = CaptureOrdinaryLiveState();
                foreach (var actor in new[] { "rider", "mount" }) foreach (var field in new[] { "standard", "move", "swift", "position" })
                    if (!JToken.DeepEquals(chunk4InspectionBefore[actor][field], after[actor][field]))
                        throw new InvalidOperationException("Idle native character inspection changed actor movement or resources.");
                AddRow("C4-INSPECTION-" + (chunk4InspectionActor == 0 ? "rider" : "mount"), true,
                    "Native character tab bound the exact actor through ordinary group selection; physical input and human visual review remain separate.",
                    new JObject { ["level"] = "NATIVE INTEGRATION", ["caseId"] = "C4-INSPECTION-" + (chunk4InspectionActor == 0 ? "rider" : "mount"),
                        ["inputKind"] = "native-character-hotkey-and-group-selection",
                        ["actor"] = Chunk4InspectedActor.UniqueId, ["boundActor"] = bound.Unit.UniqueId,
                        ["groupActor"] = GroupController.Instance.GetCurrentCharacter().UniqueId,
                        ["screenIndex"] = (int)chunk4CharacterScreenIndex.GetValue(tabs, null),
                        ["shown"] = chunk4CharacterSheet.IsShow, ["active"] = chunk4CharacterSheet.gameObject.activeInHierarchy,
                        ["displayedName"] = chunk4CharacterSheet.CharName.text, ["before"] = chunk4InspectionBefore, ["after"] = after });
                chunk4InspectionActor++;
                if (chunk4InspectionActor < 2) { chunk4InspectionStage = 1; ResetLeafClock(); return; }
                CleanupChunk4Inspection(); chunk4InspectionStage = 3; ResetLeafClock(); return;
            }
            if (chunk4InspectionStage == 3)
            {
                if (tabs.IsShow || chunk4CharacterSheet.IsShow) return;
                observations["chunk4InspectionClosed"] = true;
                BeginCleanup();
            }
        }

        private void CleanupChunk4Inspection()
        {
            if (!chunk4InspectionWindowOwned) return;
            if (chunk4InspectionOriginalGroupActor != null && chunk4InspectionOriginalGroupActor.IsInState)
                GroupController.Instance.SelectUnit(chunk4InspectionOriginalGroupActor);
            chunk4CharacterClose.Invoke(Game.Instance.UI.ServiceWindow.WindowTabs, null);
            chunk4InspectionWindowOwned = false;
        }
    }
}
