using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.TurnBasedMode;
using Kingmaker.UI.Selection;
using Kingmaker.UI.UnitSettings;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.Utility;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;
using UnityModManagerNet;

namespace KingmakerMountedCombat.Diagnostics
{
    // F3 (HUD-01/02/03, ECON-01/02, TURN-01, item 8) rows. Two mounted pairs in one turn-based encounter (pair A
    // rider-first, pair B mount-first), every input the native one a player makes: ground clicks through the native
    // ground handler with fresh predictions, stock attacks through the native unit handler, the ordinary Charge through
    // the rider's own bar slot, step mode through the native movement-limit input. Setup is disclosed (initiative,
    // hostile places, path drawn for a screenshot, healing, slot placement). The rows record raw native observations,
    // the preview the native path settings return, native turn starts, native Prepare entries and the live initiative
    // tracker; the external reader owns PASS/FAIL. The real-time neighbor records the destination marker style.
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal const string Finalize3EconomyTurnBased = "finalize3-economy-tb", Finalize3EconomyRealTime = "finalize3-economy-rt";
        internal static bool IsFinalize3EconomyScenario(string scenario) => scenario == Finalize3EconomyTurnBased || scenario == Finalize3EconomyRealTime || IsFinalize6FallsScenario(scenario);
        private bool IsFinalize3Economy => IsFinalize3EconomyScenario(request.Scenario);
        private bool Finalize3Tb => request.Scenario == Finalize3EconomyTurnBased || IsFinalize6Falls;
        internal const int Finalize3SchemaVersion = 54;
        internal const double Finalize3ScenarioDeadlineSeconds = 420.0d, Finalize3HostDeadlineSeconds = 560.0d;
        internal static readonly string[] Finalize3TurnBasedCases = { "F3-tb-setup", "F3-tb-a-round1", "F3-tb-b-round1", "F3-tb-a-step", "F3-tb-b-charge", "F3-tb-a-double", "F3-tb-b-dismount" };
        internal static readonly string[] Finalize3RealTimeCases = { "F3-rt-mounted-marker", "F3-rt-unmounted-marker", "F3-rt-attack" };
        private string[] Finalize3Cases => IsFinalize6Falls ? Finalize6Cases : Finalize3Tb ? Finalize3TurnBasedCases : Finalize3RealTimeCases;
        private string Finalize3Case => Finalize3Cases[f3CaseIndex];
        private const float Finalize3StepBoundSeconds = 25f, Finalize3CaptureBoundSeconds = 5f;
        private const int Finalize3SettleFrames = 20, Finalize3IdleFrames = 12, Finalize3RefusalFrames = 40, Finalize3AttackFrames = 360, Finalize3MountAttempts = 6;
        private const string Finalize3Contract = "finalize3-movement-economy-v2";
        private const BindingFlags F3Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private static readonly float[] Finalize3LaneRadii = { 12f, 10f, 14f, 9f, 16f, 8f, 18f, 7f, 20f };

        private int f3CaseIndex, f3Stage, f3Step, f3Frames, f3BoundStep = -1, f3CaptureGeneration, f3MountAttempts, f3RulesAt, f3IdleFrames;
        private int f3InitiativeRider, f3InitiativeMount, f3InitiativeSecondRider, f3InitiativeSecondMount, f3HealCount, f3DismountRound = -1, f3FirstRound = -1;
        private bool f3InitiativeSet, f3CaptureActive, f3MountClicked, f3SecondDismountTried;
        private float f3BoundAt, f3CaptureStartedAt, f3PeakForced, f3PeakAllocationForced, f3CleanupStartedAt = -1f;
        private Vector3 f3From;
        private UnitEntityData f3BRider, f3BMount;
        private TurnController f3LastTurn, f3RowTurn, f3EndedTurn, f3LastATurn, f3LastBTurn;
        private MountedPairAttackCommand f3Charge;
        private JObject f3Facts, f3Measure, f3Current;
        private JArray f3Shots, f3Setup, f3TurnStarts, f3Preparations, f3Trackers, f3EndTurns;
        private Sprint6RuleObserver f3Rules;
        private readonly Dictionary<UnitEntityData, int> f3Grants = new Dictionary<UnitEntityData, int>();
        private MethodInfo f3PathSettings, f3StepOn, f3StepOff;
        private FieldInfo f3MarkAlpha, f3SurpriseField;
        private float f3MarkMaxAlpha = -1f, f3MarkMinAlpha = -1f;
        private int f3MarkFrames, f3MarkVisibleFrames;

        private void BeginFinalize3Economy()
        {
            if (!settings.EnablePairedActivation || settings.EnableUnifiedMountedTurn ||
                settings.EnablePairedCommandScheduler || settings.EnableDiagnosticOverlay || playerAction.OverlayPresent)
                throw new InvalidOperationException("F3 rows require the accepted single paired authority.");
            if (nativeControls.MountAbility == null || nativeControls.DismountAbility == null || nativeControls.MountedChargeAbility == null)
                throw new InvalidOperationException("F3 rows require the registered Mount, Dismount and mounted Charge shells.");
            // Exact native contracts read or driven by the rows (local 2.1.7b metadata).
            f3PathSettings = typeof(PathVisualizer).GetMethods(F3Flags).FirstOrDefault(m => m.Name == "GetDefaultVisualPathSettings" && m.MetadataToken == 0x06007015);
            f3StepOn = typeof(CombatController).GetMethods(F3Flags).FirstOrDefault(m => m.Name == "ModifyMovementLimitOn" && m.MetadataToken == 0x06000BDF);
            f3StepOff = typeof(CombatController).GetMethods(F3Flags).FirstOrDefault(m => m.Name == "ModifyMovementLimitOff" && m.MetadataToken == 0x06000BE0);
            f3MarkAlpha = typeof(Kingmaker.UI.ClickPointerPrefab).GetField("m_CurrentTransparency", F3Flags);
            f3SurpriseField = typeof(TurnController).GetField("m_ActingInSurpriseRound", F3Flags);
            if (f3PathSettings == null || f3StepOn == null || f3StepOff == null || f3MarkAlpha == null || f3MarkAlpha.MetadataToken != 0x04001DE8 ||
                f3SurpriseField == null || f3SurpriseField.MetadataToken != 0x0400066D)
                throw new InvalidOperationException("The exact native path-settings, movement-limit, marker or surprise contract changed.");
            CaptureIdleFixturePartyForCleanup();
            castingTrace = new NativeCastingItemTrace(rider, horse);
            castingCosts = new NativeActorAllocationTrace(rider, horse, combat);
            s3Rules = new Chunk4IncomingRuleObserver(rider, horse);
            f3Rules = new Sprint6RuleObserver(rider, horse);
            f3Shots = new JArray(); f3Setup = new JArray(); f3TurnStarts = new JArray(); f3Preparations = new JArray(); f3Trackers = new JArray(); f3EndTurns = new JArray();
            f3Measure = new JObject { ["contract"] = Finalize3Contract, ["mode"] = Finalize3Tb ? "TB" : "RT",
                ["rider"] = rider.UniqueId, ["mount"] = horse.UniqueId, ["mountBlueprint"] = horse.Blueprint.AssetGuid, ["cases"] = new JArray(Finalize3Cases),
                ["names"] = new JObject { ["aRider"] = rider.CharacterName, ["aMount"] = horse.CharacterName },
                ["stockCharge"] = MountedChargeSafetyPolicy.ChargeBlueprintId, ["mountedChargeShell"] = nativeControls.MountedChargeAbility.AssetGuid,
                ["attackRules"] = f3Rules.Attacks, ["screenshots"] = f3Shots, ["disclosedSetup"] = f3Setup, ["turnStarts"] = f3TurnStarts,
                ["preparations"] = f3Preparations, ["trackers"] = f3Trackers, ["endTurns"] = f3EndTurns, ["startRelationship"] = relationship.State.ToString(),
                ["policy"] = "Locomotion belongs to the mount whichever pair actor is selected; each actor keeps its own native actions." };
            observations[CastingMeasurementKeyFor(request.Scenario)] = f3Measure;
            if (IsFinalize6Falls) BeginFinalize6Measure();
            castingOriginalSlots = new NativeCastingOriginalSlots(rider, castingTrace);
            F3NewFacts();
            step = Phase3dHorseStep.Phase3gControls;
            ResetLeafClock();
        }

        private JObject CaptureFinalize3DeadlineProgress() => new JObject { ["stage"] = f3Stage, ["step"] = f3Step, ["caseIndex"] = f3CaseIndex,
            ["case"] = f3CaseIndex < Finalize3Cases.Length ? Finalize3Case : null, ["facts"] = f3Facts?.DeepClone(), ["state"] = F3Snap() };

        private bool DrainFinalize3Fixture()
        {
            if (f3CaptureActive && Time.realtimeSinceStartup - f3CaptureStartedAt <= Finalize3CaptureBoundSeconds) return false;
            f3CaptureActive = false;
            var handler = Game.Instance?.SelectedAbilityHandler;
            if (handler?.Ability != null) handler.DropAbility();
            if (f3InitiativeSet)
            {
                rider.Stats.Initiative.BaseValue = f3InitiativeRider; horse.Stats.Initiative.BaseValue = f3InitiativeMount;
                if (f3BRider != null) f3BRider.Stats.Initiative.BaseValue = f3InitiativeSecondRider;
                if (f3BMount != null) f3BMount.Stats.Initiative.BaseValue = f3InitiativeSecondMount;
                f3InitiativeSet = false;
                if (f3Measure != null) f3Measure["initiativeRestored"] = true;
            }
            if (f3Rules != null) { f3Rules.Dispose(); f3Rules = null; }
            // The second pair leaves the fixture as it found it: the hidden Dismount shell, bounded, as Sprint 6 does.
            if (F3BState() == RelationshipState.Mounted.ToString())
            {
                if (f3CleanupStartedAt < 0f) f3CleanupStartedAt = Time.realtimeSinceStartup;
                if (Time.realtimeSinceStartup - f3CleanupStartedAt < 12f)
                {
                    if (f3BRider.Commands.Empty) { Sprint5Fixture.Click(nativeControls, f3BRider, nativeControls.DismountAbility, f3BRider); f3SecondDismountTried = true; }
                    return false;
                }
                if (f3Measure != null) f3Measure["secondPairLeftMounted"] = true;
            }
            if (f3Measure != null) f3Measure["secondDismountTriedAtCleanup"] = f3SecondDismountTried;
            return DrainSprint4Fixture();
        }

        // r03's teardown stalled with the game in EscMode from about the turn-based end on (opener not identified;
        // native leave-combat time does not advance there). Every mode change during cleanup is recorded. Once the
        // fixture's own restorations are done, an EscMode is closed once through the native window that owns it --
        // a full-screen tutorial or the Esc menu -- and native leave-combat then finishes on its own. Anything else
        // is recorded and left alone, so the bounded cleanup fails with the facts.
        private JArray f3CleanupModes;
        private JObject f3EscIntervention;
        private static readonly FieldInfo NativeTutorialDontShow =
            typeof(Kingmaker.UI.Tooltip.TutorialWindow).GetField("m_DontShowToggle", BindingFlags.Instance | BindingFlags.NonPublic);

        private void F3ObserveCleanupMode(bool restorationsDone)
        {
            var game = Game.Instance;
            var mode = game.CurrentMode.ToString();
            if (f3CleanupModes == null) { f3CleanupModes = new JArray(); if (f3Measure != null) f3Measure["cleanupModes"] = f3CleanupModes; }
            var last = f3CleanupModes.Count == 0 ? null : (string)f3CleanupModes[f3CleanupModes.Count - 1]["mode"];
            if (last != mode && f3CleanupModes.Count < 16)
                f3CleanupModes.Add(new JObject { ["mode"] = mode, ["frame"] = Time.frameCount, ["realtime"] = Time.realtimeSinceStartup,
                    ["paused"] = game.IsPaused, ["inCombat"] = game.Player.IsInCombat });
            var escMode = game.CurrentMode == Kingmaker.GameModes.GameModeType.EscMode;
            if (!escMode) return;
            var tutorial = game.UI?.DescriptionController?.TutorialFullScreen;
            var escMenu = UnityEngine.Object.FindObjectOfType<Kingmaker.UI.ServiceWindow.EscMenuWindow>();
            var page = tutorial?.Data?.TutorialPage?.FirstOrDefault();
            var toggle = tutorial?.Data != null ? NativeTutorialDontShow?.GetValue(tutorial) : null;
            var dontShow = toggle?.GetType().GetProperty("isOn")?.GetValue(toggle, null) as bool?;
            // Closing with "don't show" ticked would change the player's TutorialMessages setting: refused.
            var action = Finalize3CleanupEscModePolicy.Decide(restorationsDone, f3EscIntervention != null, escMode, game.IsPaused,
                tutorial?.Data != null, dontShow, escMenu != null && escMenu.IsShow);
            if (action == Finalize3EscModeAction.None) return;
            var record = new JObject { ["frame"] = Time.frameCount, ["realtime"] = Time.realtimeSinceStartup, ["modeBefore"] = mode,
                ["tutorialShown"] = tutorial?.Data != null, ["tutorialPage"] = page?.name, ["tutorialType"] = page?.Type.ToString(),
                ["dontShowToggle"] = dontShow, ["escMenuShown"] = escMenu != null && escMenu.IsShow, ["action"] = action.ToString() };
            // A refusal closes nothing and does not use the one intervention: the first and the count are recorded.
            if (action == Finalize3EscModeAction.RefuseTutorialSetting || action == Finalize3EscModeAction.RefuseUnidentified)
            {
                if (f3Measure == null) return;
                if (f3Measure["escModeRefusal"] == null) f3Measure["escModeRefusal"] = record;
                f3Measure["escModeRefusalFrames"] = (f3Measure.Value<int?>("escModeRefusalFrames") ?? 0) + 1;
                return;
            }
            f3EscIntervention = record;
            if (f3Measure != null) f3Measure["escModeIntervention"] = f3EscIntervention;
            if (action == Finalize3EscModeAction.CloseTutorial) tutorial.CloseWindow();
            else if (action == Finalize3EscModeAction.CloseEscMenu) escMenu.Show(false);
            f3EscIntervention["modeAfter"] = game.CurrentMode.ToString();
        }

        private void F3NewFacts() => f3Facts = new JObject { ["case"] = Finalize3Case, ["steps"] = new JObject() };

        private void F3Disclose(string action) =>
            f3Setup.Add(new JObject { ["case"] = f3CaseIndex < Finalize3Cases.Length ? Finalize3Case : null, ["frame"] = Time.frameCount, ["action"] = action });

        private void FinishF3Row(bool passed, string detail)
        {
            f3Facts["after"] = F3Snap();
            AddRow(Finalize3Case, passed, detail, f3Facts);
            f3CaseIndex++; f3Step = 0; f3Frames = 0;
            if (f3CaseIndex == Finalize3Cases.Length || !passed) { BeginCleanup(); return; }
            F3NewFacts();
            ResetLeafClock();
        }

        private void F3Fail(string why) { f3Facts["failure"] = why; FinishF3Row(false, why); }

        private JObject F3Steps => (JObject)f3Facts["steps"];

        private void TickFinalize3Economy()
        {
            var game = Game.Instance; var turn = game.TurnBasedCombatController.CurrentTurn;
            if (game.IsPaused) { game.IsPaused = false; return; }
            CastingMeasurement["progress"] = new JObject { ["stage"] = f3Stage, ["caseIndex"] = f3CaseIndex, ["step"] = f3Step };
            if (f3CaptureActive)
            {
                if (Time.realtimeSinceStartup - f3CaptureStartedAt <= Finalize3CaptureBoundSeconds) return;
                f3CaptureActive = false;
                f3Shots.Add(new JObject { ["error"] = "The end-of-frame capture did not complete within its bound.", ["frame"] = Time.frameCount });
            }
            if (f3Stage == 0) { TickF3SecondOwner(); return; }
            if (f3Stage == 1)
            {
                if (!rider.Commands.Empty || !horse.Commands.Empty || rider.AreHandsBusyWithAnimation) return;
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                var isolationReady = PrepareUnmountedHorseAiIsolation();
                var pairMounted = relationship.State == RelationshipState.Mounted;
                if (CastingEntryMayRequestMount(true, pairMounted, isolationReady, f3MountClicked))
                    f3MountClicked = TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "finalize3-exploration-mount");
                if (!CastingEntryReady(true, pairMounted, isolationReady)) return;
                f3Measure["entry"] = F3Snap();
                f3Frames = 0; f3Stage = Finalize3Tb ? 2 : 3; ResetLeafClock(); return;
            }
            if (f3Stage == 2) { TickF3SecondMount(); return; }
            if (f3Step != f3BoundStep) { f3BoundStep = f3Step; f3BoundAt = Time.realtimeSinceStartup; f3Frames = 0; ResetLeafClock(); }
            else if (Time.realtimeSinceStartup - f3BoundAt > Finalize3StepBoundSeconds) { F3Fail(Finalize3Case + " step " + f3Step + " exceeded its bound."); return; }
            targetService?.ObserveTargetLifeState(); targetService?.RefreshBidirectionalCombatMemoryLease();
            if (Finalize3Tb) { F3ObserveTurns(turn); F3ObservePreparations(); F3KeepStanding(); }
            switch (Finalize3Case)
            {
                case "F3-tb-setup": TickF3Setup(turn); return;
                case "F3-tb-a-round1": TickF3ARound1(turn); return;
                case "F3-tb-b-round1": TickF3BRound1(turn); return;
                case "F3-tb-a-step": TickF3AStep(turn); return;
                case "F3-tb-b-charge": TickF3BCharge(turn); return;
                case "F3-tb-a-double": TickF3ADouble(turn); return;
                case "F3-tb-b-dismount": TickF3BDismount(turn); return;
                case "F6-tb-a-death-fall": TickF6ADeathFall(turn); return;
                case "F6-tb-b-prone-fall": TickF6BProneFall(turn); return;
                case "F3-rt-mounted-marker": TickF3RtMounted(); return;
                case "F3-rt-unmounted-marker": TickF3RtUnmounted(); return;
                case "F3-rt-attack": TickF3RtAttack(); return;
                default: throw new InvalidOperationException("Unknown F3 case " + Finalize3Case);
            }
        }

        // ---- the second owner and its pet ----------------------------------------------------------------------------
        private void TickF3SecondOwner()
        {
            if (!rider.Commands.Empty || rider.AreHandsBusyWithAnimation) return;
            var party = Game.Instance.Player.PartyCharacters.Select(reference => reference.Value)
                .Where(member => member != null && member != rider && member != horse && !member.Descriptor.IsPet && member.IsInGame && member.Descriptor.State.IsConscious).ToArray();
            var owner = party.FirstOrDefault(member => member.Descriptor.Pet != null && member.Descriptor.Pet != horse && member.Descriptor.Pet.IsInState);
            var source = "existing-pet";
            if (owner == null && Finalize3Tb)
            {
                // Disclosed fallback only when the fixture has no second companion: the Sprint 6 native companion acquisition.
                var ranger = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(Chunk7OrdinaryBuildDriver.RangerClassGuid);
                owner = party.Where(member => member.Descriptor.Pet == null && member.Descriptor.State.Size == Size.Medium &&
                    member.Descriptor.Progression.GetClassLevel(ranger) == 0).OrderBy(member => member.UniqueId, StringComparer.Ordinal).FirstOrDefault();
                if (owner != null)
                {
                    var acquisition = new JObject(); f3Measure["secondCompanionAcquisition"] = acquisition;
                    F3Disclose("The fixture had no second companion: one was acquired through the native Ranger level-up (Sprint 6 route).");
                    // Sprint 6 setup: the disposable owner first receives the experience for the four native levels it takes.
                    var progression = owner.Descriptor.Progression;
                    var needed = Kingmaker.Blueprints.Root.BlueprintRoot.Instance.Progression.XPTable.GetBonus(progression.CharacterLevel + 4);
                    acquisition["experienceSetup"] = new JObject { ["levelBefore"] = progression.CharacterLevel, ["experienceBefore"] = progression.Experience, ["experienceAfter"] = Math.Max(needed, progression.Experience) };
                    if (progression.Experience < needed) progression.AdvanceExperienceTo(needed, false);
                    F3Disclose("Setup: the second owner received the experience for four native levels before the Ranger companion acquisition.");
                    if (!NativeHorseFixtureAcquisition.Create(owner, horseService, logger, acquisition)) { F3Fail("The second pair's native companion did not commit."); return; }
                    source = "acquired";
                }
            }
            if (owner == null && !Finalize3Tb) owner = party.OrderBy(member => member.UniqueId, StringComparer.Ordinal).FirstOrDefault();
            if (owner == null) { F3Fail("The fixture has no second party member for the second pair or the unmounted control."); return; }
            f3BRider = owner; f3BMount = owner.Descriptor.Pet;
            if (Finalize3Tb && f3BMount == null) { F3Fail("The second owner has no companion to mount."); return; }
            if (f3BMount != null) f3Rules.Watch(f3BMount);
            f3Rules.Watch(f3BRider);
            f3Measure["secondRider"] = f3BRider.UniqueId; f3Measure["secondMount"] = f3BMount?.UniqueId; f3Measure["secondPairSource"] = source;
            ((JObject)f3Measure["names"])["bRider"] = f3BRider.CharacterName; ((JObject)f3Measure["names"])["bMount"] = f3BMount?.CharacterName;
            f3Stage = 1; ResetLeafClock();
        }

        // Exploration Mount of the second pair, the ordinary way, after the first pair's AI isolation holds (Sprint 6 r05).
        private void TickF3SecondMount()
        {
            if (F3BState() == RelationshipState.Mounted.ToString())
            {
                if (++f3Frames < 30 || !f3BRider.Commands.Empty || !f3BMount.Commands.Empty) return;
                f3Measure["secondEntry"] = F3Snap();
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                f3Frames = 0; f3Stage = 3; ResetLeafClock(); return;
            }
            if (f3BMount?.View?.AgentASP == null || !f3BMount.IsInState || !f3BRider.Commands.Empty || f3BRider.AreHandsBusyWithAnimation || ++f3Frames < 30) return;
            if (++f3MountAttempts > Finalize3MountAttempts) { F3Fail("The second pair's exploration Mount did not commit."); return; }
            f3Measure["secondMountClicks"] = f3MountAttempts;
            if (Sprint5Fixture.Click(nativeControls, f3BRider, nativeControls.MountAbility, f3BMount)) ResetLeafClock();
            f3Frames = 0;
        }

        // ---- observation ---------------------------------------------------------------------------------------------
        private MountedPairServices F3BServices => f3BRider == null ? null : relationship.World?.ForActor(f3BRider);
        private MountedCombatController F3BCombat => F3BServices?.Combat;
        private MountedPairTurnParticipant F3BParticipant => F3BServices?.Participant;

        private string F3BState()
        {
            if (f3BRider == null) return null;
            var owner = relationship.World?.Registry.FindOwner(f3BRider);
            return owner?.Boundary == null ? "None" : owner.Boundary.State.ToString();
        }

        private string F3Role(UnitEntityData unit) => unit == null ? null : unit == rider ? "aRider" : unit == horse ? "aMount" :
            unit == f3BRider && f3BRider != null ? "bRider" : unit == f3BMount && f3BMount != null ? "bMount" : "other";

        private bool F3PartyActor(UnitEntityData unit) => unit != null && (unit == rider || unit == horse || unit == f3BRider || unit == f3BMount);

        private static string F3SelectedId()
        {
            var selection = SelectionManager.Instance?.SelectedUnits;
            return selection != null && selection.Count == 1 ? selection[0]?.UniqueId : null;
        }

        private static void F3Select(UnitEntityData unit)
        {
            var selection = SelectionManager.Instance.SelectedUnits;
            if (selection == null || selection.Count != 1 || selection[0] != unit) SelectionManager.Instance.SelectUnit(unit.View, true, true, false);
        }

        private static JObject F3Actor(UnitEntityData unit)
        {
            if (unit == null) return null;
            var cooldown = unit.CombatState.Cooldown;
            return new JObject { ["id"] = unit.UniqueId, ["standard"] = cooldown.StandardAction, ["move"] = cooldown.MoveAction, ["swift"] = cooldown.SwiftAction,
                ["usedStandard"] = unit.UsedStandardAction(), ["position"] = CapturePosition(unit.Position), ["inCombat"] = unit.IsInCombat, ["hp"] = unit.HPLeft,
                ["commandsEmpty"] = unit.Commands.Empty, ["moving"] = unit.View?.AgentASP?.IsReallyMoving };
        }

        private JObject F3Snap()
        {
            var controller = Game.Instance.TurnBasedCombatController; var turn = controller.CurrentTurn;
            return new JObject { ["frame"] = Time.frameCount, ["round"] = controller.RoundNumber, ["turnUnit"] = turn?.Unit?.UniqueId, ["turnRole"] = F3Role(turn?.Unit),
                ["turnStatus"] = turn?.Status.ToString(), ["activationA"] = combat.TurnParticipant?.ActivationIdentity, ["activationB"] = F3BParticipant?.ActivationIdentity,
                ["aRelationship"] = relationship.State.ToString(), ["bRelationship"] = F3BState(), ["selected"] = F3SelectedId(), ["selectedRole"] = F3Role(SelectionManager.Instance?.SelectedUnits?.Count == 1 ? SelectionManager.Instance.SelectedUnits[0] : null),
                ["aRider"] = F3Actor(rider), ["aMount"] = F3Actor(horse), ["bRider"] = F3Actor(f3BRider), ["bMount"] = F3Actor(f3BMount),
                ["target"] = target == null ? null : CapturePosition(target.Position), ["feedbackA"] = combat.LastFeedback, ["feedbackB"] = F3BCombat?.LastFeedback };
        }

        private static JObject F3Allocation(MountedMovementState movement) => movement == null ? null : new JObject { ["timeMoved"] = movement.TimeMoved,
            ["timeForced"] = movement.TimeForced, ["timeStepped"] = movement.TimeStepped, ["metresStepped"] = movement.MetresStepped };

        private static JObject F3Context(TurnController context)
        {
            if (context == null) return null;
            var result = new JObject { ["unit"] = context.Unit?.UniqueId, ["timeMoved"] = context.TimeMoved, ["timeForced"] = context.TimeMovedInForceMode,
                ["timeStepped"] = context.TimeMovedByFiveFootStep, ["metresStepped"] = context.MetersMovedByFiveFootStep,
                ["modifyLimit"] = context.ModifyMovementLimitValue, ["enabledStep"] = context.EnabledFiveFootStep, ["enabledSingle"] = context.EnabledSingleActionMove };
            try { result["remaining"] = context.GetRemainingMovementRange(false, false); result["remainingTotal"] = context.GetRemainingMovementRange(true, false);
                result["stepRange"] = context.GetRemainingFiveFootStepRange(); }
            catch (Exception exception) { result["rangeError"] = exception.GetType().Name + ": " + exception.Message; }
            return result;
        }

        // The preview the native path settings return now (the transpiled PathVisualizer.GetDefaultVisualPathSettings, read
        // through reflection on the live instance), with the mount's own movement records beside it.
        private JObject F3Probe(UnitEntityData mount, MountedPairTurnParticipant participant)
        {
            var result = new JObject { ["frame"] = Time.frameCount, ["round"] = Game.Instance.TurnBasedCombatController.RoundNumber, ["selectedRole"] = F3Role(SelectionManager.Instance?.SelectedUnits?.Count == 1 ? SelectionManager.Instance.SelectedUnits[0] : null) };
            try
            {
                var visualizer = PathVisualizer.Instance;
                if (visualizer == null) throw new InvalidOperationException("The native path visualizer is absent.");
                var settingsValue = f3PathSettings.Invoke(visualizer, null);
                var type = settingsValue.GetType();
                Func<string, object> field = name => type.GetField(name, F3Flags)?.GetValue(settingsValue);
                var unit = field("Unit") as UnitEntityData;
                result["unit"] = unit?.UniqueId; result["unitRole"] = F3Role(unit); result["unitIsMount"] = unit != null && unit == mount;
                result["moveActionCooldown"] = Convert.ToDouble(field("MoveActionCooldown"));
                result["movePrediction"] = field("MovePredictionState")?.ToString(); result["standardPrediction"] = field("StandardPredictionState")?.ToString();
                result["remaining"] = Convert.ToDouble(field("RemainingMovementRange")); result["remainingTotal"] = Convert.ToDouble(field("RemainingMovementRangeTotal"));
                result["singleActionMove"] = Convert.ToBoolean(field("EnabledSingleActionMove")); result["fiveFootStep"] = Convert.ToBoolean(field("EnabledFiveFeetStep"));
            }
            catch (Exception exception) { result["error"] = exception.GetType().Name + ": " + (exception.InnerException?.Message ?? exception.Message); }
            result["mountSpeed"] = mount?.CurrentSpeedMps; result["mountCombatSpeed"] = mount?.CombatSpeedMps;
            result["partner"] = F3Context(participant?.PartnerContext);
            result["allocation"] = F3Allocation(participant?.PeekMountAllocation());
            result["path"] = F3PathState(mount);
            return result;
        }

        private static JObject F3PathState(UnitEntityData mount)
        {
            var result = new JObject();
            try
            {
                var visualizer = PathVisualizer.Instance;
                if (visualizer == null) return result;
                var path = mount?.View == null ? null : visualizer.CurrentPathForUnit(mount.View);
                result["state"] = path?.CompleteState.ToString(); result["points"] = path?.vectorPath?.Count;
                foreach (var name in new[] { "m_WillUseFiveFootStep", "m_WillUseMoveAction", "m_WillUseStandardAction", "m_TotalPathLength" })
                {
                    var value = typeof(PathVisualizer).GetField(name, F3Flags)?.GetValue(visualizer);
                    result[name] = value == null ? null : (JToken)Convert.ToDouble(value);
                }
                result["outOfRange"] = visualizer.IsOutOfRange;
            }
            catch (Exception exception) { result["error"] = exception.GetType().Name + ": " + exception.Message; }
            return result;
        }

        private JObject F3Participation(MountedPairTurnParticipant participant)
        {
            var saved = participant?.CaptureParticipation();
            if (saved == null) return new JObject { ["present"] = false };
            return new JObject { ["present"] = true, ["sequence"] = saved.Activation?.Sequence, ["split"] = saved.Activation?.Split, ["finalized"] = saved.Activation?.Finalized,
                ["riderPrepared"] = saved.Activation?.Rider?.Prepared, ["mountGranted"] = saved.Activation?.Mount?.Granted, ["mountPrepared"] = saved.Activation?.Mount?.Prepared,
                ["mountEnded"] = saved.Activation?.Mount?.Ended, ["splitReleaseRound"] = saved.SplitReleaseRound, ["pendingSplitId"] = saved.PendingSplitId, ["pendingSplitRound"] = saved.PendingSplitRound };
        }

        // The live initiative tracker the HUD shows (its bound view model); a constructed one only when the view is absent.
        private JObject F3Tracker(string label)
        {
            var controller = Game.Instance.TurnBasedCombatController;
            var result = new JObject { ["label"] = label, ["round"] = controller.RoundNumber, ["frame"] = Time.frameCount, ["turnRole"] = F3Role(controller.CurrentTurn?.Unit) };
            Kingmaker.UI._ConsoleUI.TurnBasedMode.InitiativeTrackerVM constructed = null;
            try
            {
                Kingmaker.UI._ConsoleUI.TurnBasedMode.InitiativeTrackerVM vm = null;
                var viewType = typeof(UnitEntityData).Assembly.GetType("Kingmaker.UI.TurnBasedMode.InitiativeTrackerHorizontalPCView");
                var view = viewType == null ? null : UnityEngine.Object.FindObjectOfType(viewType);
                for (var type = viewType; view != null && vm == null && type != null; type = type.BaseType)
                {
                    vm = type.GetProperty("ViewModel", F3Flags | BindingFlags.DeclaredOnly)?.GetValue(view, null) as Kingmaker.UI._ConsoleUI.TurnBasedMode.InitiativeTrackerVM;
                    if (vm == null)
                        foreach (var field in type.GetFields(F3Flags | BindingFlags.DeclaredOnly).Where(f => f.FieldType == typeof(Kingmaker.UI._ConsoleUI.TurnBasedMode.InitiativeTrackerVM)))
                        { vm = field.GetValue(view) as Kingmaker.UI._ConsoleUI.TurnBasedMode.InitiativeTrackerVM; if (vm != null) break; }
                }
                result["source"] = vm != null ? "live" : "constructed";
                if (vm == null) { constructed = new Kingmaker.UI._ConsoleUI.TurnBasedMode.InitiativeTrackerVM(); vm = constructed; }
                var units = vm.Units.Where(item => item?.Unit != null).Select(item => item.Unit).ToArray();
                result["entries"] = new JArray(units.Select(unit => unit.UniqueId));
                result["aRider"] = units.Count(unit => unit == rider); result["aMount"] = units.Count(unit => unit == horse);
                result["bRider"] = f3BRider == null ? 0 : units.Count(unit => unit == f3BRider); result["bMount"] = f3BMount == null ? 0 : units.Count(unit => unit == f3BMount);
            }
            catch (Exception exception) { result["error"] = exception.GetType().Name + ": " + exception.Message; }
            finally { constructed?.Dispose(); }
            result["hiddenAMount"] = combat.TurnParticipant?.HidesPairedTrackerEntry(horse);
            result["hiddenBMount"] = f3BMount == null ? null : F3BParticipant?.HidesPairedTrackerEntry(f3BMount);
            result["bRelationship"] = F3BState();
            f3Trackers.Add(result);
            return result;
        }

        // Every native turn start, by the current turn controller's identity: a mount ridden in a pair never owns one.
        private void F3ObserveTurns(TurnController turn)
        {
            if (ReferenceEquals(turn, f3LastTurn)) return;
            f3LastTurn = turn; f3BoundAt = Time.realtimeSinceStartup; ResetLeafClock();
            if (turn?.Unit == null || f3FirstRound < 0) return;
            f3TurnStarts.Add(new JObject { ["sequence"] = f3TurnStarts.Count + 1, ["round"] = Game.Instance.TurnBasedCombatController.RoundNumber, ["unit"] = turn.Unit.UniqueId,
                ["role"] = F3Role(turn.Unit), ["frame"] = Time.frameCount, ["status"] = turn.Status.ToString(), ["surprise"] = F3Surprise(turn),
                ["aRelationship"] = relationship.State.ToString(), ["bRelationship"] = F3BState() });
        }

        // Native Prepare entries (the allocation trace's TurnController.Prepare hook), attributed to their round.
        private void F3ObservePreparations()
        {
            if (castingCosts == null || f3FirstRound < 0) return;
            foreach (var unit in new[] { rider, horse, f3BRider, f3BMount })
            {
                if (unit == null) continue;
                int last; f3Grants.TryGetValue(unit, out last);
                var now = castingCosts.GrantCount(unit);
                if (now == last) continue;
                f3Grants[unit] = now;
                f3Preparations.Add(new JObject { ["round"] = Game.Instance.TurnBasedCombatController.RoundNumber, ["unit"] = unit.UniqueId, ["role"] = F3Role(unit),
                    ["count"] = now - last, ["frame"] = Time.frameCount, ["bRelationship"] = F3BState() });
            }
        }

        // Setup only: every actor stands for the whole encounter; the hostile's hits are not under test.
        private void F3KeepStanding()
        {
            foreach (var unit in new[] { rider, horse, f3BRider, f3BMount })
                if (unit != null && !f6NoHeal.Contains(unit) && unit.HPLeft < unit.MaxHP / 2 && unit.Descriptor.State.IsConscious)
                {
                    unit.Damage = 0; f3HealCount++; f3Measure["healCount"] = f3HealCount;
                    if (f3HealCount == 1) F3Disclose("An actor below half hit points was healed (Damage = 0); repeated silently, counted in healCount.");
                }
        }

        private bool F3Surprise(TurnController turn) => turn != null && (bool)f3SurpriseField.GetValue(turn);

        // ---- input ---------------------------------------------------------------------------------------------------
        // The native ground click: fresh predictions, then the ground handler's own click (it consumes the paired preview).
        private JObject F3GroundClick(string name, Vector3 destination, bool turnBased)
        {
            var record = new JObject { ["destination"] = CapturePosition(destination), ["selectedRole"] = F3Role(SelectionManager.Instance?.SelectedUnits?.Count == 1 ? SelectionManager.Instance.SelectedUnits[0] : null) };
            F3Steps[name + "Click"] = record;
            try
            {
                using (var input = new NativeOrdinaryAttackInput(destination))
                {
                    var turn = Game.Instance.TurnBasedCombatController.CurrentTurn;
                    if (turnBased && turn != null)
                    {
                        input.Predict();
                        var unit = relationship.World?.UnifiedTurn?.PairedMovementContext(turn)?.Unit ?? turn.Unit;
                        var path = unit?.View == null ? null : PathVisualizer.Instance?.CurrentPathForUnit(unit.View);
                        var points = path?.vectorPath;
                        record["previewUnitRole"] = F3Role(unit); record["previewState"] = path?.CompleteState.ToString();
                        record["previewEnd"] = points != null && points.Count > 0 ? CapturePosition(points[points.Count - 1]) : null;
                    }
                    record["clicked"] = input.Click();
                }
            }
            catch (Exception exception) { record["clicked"] = false; record["error"] = exception.GetType().Name + ": " + exception.Message; }
            record["frame"] = Time.frameCount;
            return record;
        }

        private JObject F3UnitClick(string name, UnitEntityData actor, bool turnBased)
        {
            F3Select(actor);
            var record = new JObject { ["actorRole"] = F3Role(actor), ["selectedRole"] = F3Role(SelectionManager.Instance?.SelectedUnits?.Count == 1 ? SelectionManager.Instance.SelectedUnits[0] : null),
                ["targetId"] = target?.UniqueId, ["rulesBefore"] = f3Rules.Attacks.Count };
            F3Steps[name + "Click"] = record;
            f3RulesAt = f3Rules.Attacks.Count;
            try
            {
                using (var input = new NativeOrdinaryAttackInput(target))
                {
                    if (turnBased) input.Predict();
                    record["clicked"] = input.Click();
                }
            }
            catch (Exception exception) { record["clicked"] = false; record["error"] = exception.GetType().Name + ": " + exception.Message; }
            record["frame"] = Time.frameCount;
            return record;
        }

        private int F3RulesBy(UnitEntityData actor, bool charge = false) =>
            f3Rules.Attacks.Skip(f3RulesAt).Count(rule => (string)rule["attacker"] == actor.UniqueId && (string)rule["target"] == target?.UniqueId && (!charge || (bool?)rule["charge"] == true));

        private static bool F3Idle(UnitEntityData a, UnitEntityData b, MountedCombatController pair) =>
            a.Commands.Empty && (b == null || b.Commands.Empty) && !a.AreHandsBusyWithAnimation && (pair == null || !pair.HasActiveCommand && !pair.HasActiveGroundMovement) &&
            !(b?.View?.AgentASP?.IsReallyMoving ?? false) && !(a.View?.AgentASP?.IsReallyMoving ?? false);

        private bool F3Settled(UnitEntityData a, UnitEntityData b, MountedCombatController pair)
        {
            if (!F3Idle(a, b, pair)) { f3IdleFrames = 0; return false; }
            return ++f3IdleFrames >= Finalize3IdleFrames;
        }

        private JObject F3Begin(string name, UnitEntityData mount, MountedPairTurnParticipant participant)
        {
            f3From = mount.Position; f3IdleFrames = 0;
            var record = new JObject { ["before"] = F3Snap() };
            if (Finalize3Tb) record["probeBefore"] = F3Probe(mount, participant);
            F3Steps[name] = record; f3Current = record;
            return record;
        }

        private void F3End(UnitEntityData mount, MountedPairTurnParticipant participant, bool probe = true)
        {
            f3Current["after"] = F3Snap();
            f3Current["mountDelta"] = HorizontalDistance(f3From, mount.Position);
            if (probe && Finalize3Tb) f3Current["probe"] = F3Probe(mount, participant);
        }

        // A point at the requested distance from the mount, clear of the hostile and of the other pair.
        private Vector3 F3Destination(UnitEntityData mount, UnitEntityData otherMount, float distance)
        {
            Func<Vector3, bool> clear = point => (target == null || HorizontalDistance(point, target.Position) >= 4f) &&
                (otherMount == null || HorizontalDistance(point, otherMount.Position) >= 4f);
            try { return FindWalkablePoint(mount.Position, distance, Math.Max(0.5f, distance * 0.08f), clear); }
            catch (InvalidOperationException) { return FindWalkablePoint(mount.Position, distance, Math.Max(0.75f, distance * 0.15f)); }
        }

        private void F3PlaceAdjacent(UnitEntityData mount, string why)
        {
            // Bodies nearly touching: both the mount's natural weapon and the carried rider's weapon reach without an approach.
            var distance = (mount.View?.Corpulence ?? 0.5f) + (target.View?.Corpulence ?? 0.5f) + 0.4f;
            Vector3 point;
            try { point = FindWalkablePoint(mount.Position, distance, .3f); }
            catch (InvalidOperationException) { point = FindWalkablePoint(mount.Position, distance, .6f); }
            target.Translocate(point, null);
            F3Disclose("The hostile was translocated " + distance.ToString("0.00") + " m from the " + F3Role(mount) + " (" + why + ").");
        }

        // Controlled setup for a screenshot: the movement context's approach point and the native path calculation.
        private void F3DrawPath(string name, UnitEntityData mount, MountedPairTurnParticipant participant, float distance)
        {
            var record = new JObject { ["distance"] = distance };
            F3Steps[name + "PathSetup"] = record;
            try
            {
                var context = participant?.PartnerContext;
                if (context == null || PathVisualizer.Instance == null) throw new InvalidOperationException("No movement context or path visualizer.");
                var point = F3Destination(mount, null, distance);
                var actions = context.ActionsStates;
                actions.ApproachPoint = point; actions.ApproachRadius = 0.3f;
                PathVisualizer.Instance.CalculatePathForCommand(actions, true, true);
                record["point"] = CapturePosition(point); record["path"] = F3PathState(mount);
                F3Disclose("Controlled setup: the " + F3Role(mount) + " movement context's ApproachPoint was set " + distance.ToString("0.0") +
                    " m away and PathVisualizer.CalculatePathForCommand drew its native path for the screenshot.");
            }
            catch (Exception exception) { record["error"] = exception.GetType().Name + ": " + exception.Message; }
        }

        // The native End Turn input the casting rows use, for the scenario's own pair actors only.
        private bool F3EndTurn(TurnController turn, string reason)
        {
            var actor = turn?.Unit; var controller = Game.Instance.TurnBasedCombatController;
            if (actor == null || ReferenceEquals(turn, f3EndedTurn) || !F3PartyActor(actor) || !actor.IsDirectlyControllable || !actor.Commands.Empty ||
                actor.AreHandsBusyWithAnimation || !turn.CanEndTurnAndNoActing() || controller.WaitingForUI || GetPendingNextUnit(controller) != null) return false;
            castingCosts?.Record("finalize3-end-turn-input-before", actor);
            Game.Instance.PauseBind();
            f3EndedTurn = turn;
            f3EndTurns.Add(new JObject { ["round"] = controller.RoundNumber, ["role"] = F3Role(actor), ["unit"] = actor.UniqueId, ["reason"] = reason, ["frame"] = Time.frameCount });
            ResetLeafClock();
            return true;
        }

        // The pair's activation: the principal's own native turn, ready for input, not one already used. Other party turns
        // reached meanwhile are ended natively and recorded; hostile turns are the native AI's.
        private bool F3Activation(TurnController turn, UnitEntityData principal, UnitEntityData partner, TurnController previous)
        {
            if (turn?.Unit == null) return false;
            if (turn.Unit == principal && !ReferenceEquals(turn, previous))
            {
                if (F3Surprise(turn)) { F3EndTurn(turn, "surprise-round"); return false; }
                return Chunk7NativeFixture.ReadyForTurnInput(turn) && principal.Commands.Empty && (partner == null || partner.Commands.Empty) &&
                    !principal.AreHandsBusyWithAnimation && !Game.Instance.TurnBasedCombatController.WaitingForUI;
            }
            if (F3PartyActor(turn.Unit) && (turn.Unit != principal || ReferenceEquals(turn, previous))) F3EndTurn(turn, "awaiting " + F3Role(principal));
            return false;
        }

        private void F3StartActivation(TurnController turn, string label, UnitEntityData principal, UnitEntityData mount, MountedPairTurnParticipant participant)
        {
            f3RowTurn = turn;
            F3Select(principal);
            F3Steps["start"] = new JObject { ["snap"] = F3Snap(), ["probe"] = F3Probe(mount, participant), ["participation"] = F3Participation(participant),
                ["tracker"] = F3Tracker(label), ["surprise"] = F3Surprise(turn) };
        }

        // Row end: the native End Turn once the pair is idle, then the row closes when the turn has moved on.
        private bool F3CloseActivation(TurnController turn, UnitEntityData principal, UnitEntityData partner, MountedCombatController pair)
        {
            if (ReferenceEquals(turn, f3RowTurn))
            {
                if (F3Idle(principal, partner, pair)) F3EndTurn(turn, "activation end");
                return false;
            }
            F3Steps["end"] = new JObject { ["snap"] = F3Snap(), ["endedNatively"] = ReferenceEquals(f3EndedTurn, f3RowTurn) };
            return true;
        }

        // ---- shots ---------------------------------------------------------------------------------------------------
        private void F3Shot(string name, UnitEntityData selected)
        {
            var row = new JObject { ["name"] = name, ["requestedFrame"] = Time.frameCount };
            var runner = UnityModManager.UI.Instance;
            if (runner == null) { row["error"] = "The end-of-frame runner is missing."; f3Shots.Add(row); return; }
            // Native SelectUnit drops a selected ability; select only when the actor is not already the one selection.
            F3Select(selected);
            // The mod-manager window opens at start in base-game staging and covers the HUD: closed for the frame, reopened after.
            row["ummClosedForCapture"] = runner.Opened;
            if (runner.Opened) runner.ToggleWindow(false);
            var generation = ++f3CaptureGeneration;
            f3CaptureActive = true; f3CaptureStartedAt = Time.realtimeSinceStartup;
            runner.StartCoroutine(F3Capture(row, generation, runner));
        }

        private IEnumerator F3Capture(JObject row, int generation, UnityModManager.UI runner)
        {
            yield return null;
            yield return new WaitForEndOfFrame();
            try
            {
                row["frame"] = Time.frameCount; row["ummOpenAtCapture"] = runner.Opened; row["state"] = F3Snap();
                var bytes = Screenshot.CapturePNG();
                if (bytes == null || bytes.Length == 0) throw new InvalidOperationException("The frame encoder returned no bytes.");
                var folder = Path.Combine(request.EvidenceRoot, "movement-visuals");
                Directory.CreateDirectory(folder);
                var file = "finalize3-" + (string)row["name"] + ".png";
                File.WriteAllBytes(Path.Combine(folder, file), bytes);
                using (var algorithm = SHA256.Create())
                    row["sha256"] = BitConverter.ToString(algorithm.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
                row["file"] = "movement-visuals/" + file; row["bytes"] = bytes.Length; row["width"] = Screen.width; row["height"] = Screen.height;
            }
            catch (Exception exception) { row["error"] = exception.GetType().Name + ": " + exception.Message; }
            if ((bool?)row["ummClosedForCapture"] == true && ReferenceEquals(UnityModManager.UI.Instance, runner)) runner.ToggleWindow(true);
            f3Shots.Add(row);
            if (generation == f3CaptureGeneration) { f3CaptureActive = false; ResetLeafClock(); }
        }

        // ---- F3-tb-setup: both pairs mounted before combat, both pre-pair initiative arrangements, TB on ------------------
        private void TickF3Setup(TurnController turn)
        {
            switch (f3Step)
            {
                case 0:
                {
                    if (turnBasedModeProbe == null) turnBasedModeProbe = new NativeModeTransitionProbe(true);
                    if (!turnBasedModeProbe.TemporaryValueIsCurrent) { turnBasedModeProbe.DispatchTemporaryValueIfRequired(); return; }
                    if (pairedAutomaticEndProbe == null) pairedAutomaticEndProbe = new NativeAutomaticEndProbe(false);
                    if (!rider.Commands.Empty || !horse.Commands.Empty || !f3BRider.Commands.Empty || !f3BMount.Commands.Empty) return;
                    // Disclosed setup: each actor's own native initiative before combat, as the Sprint 7 rows set it.
                    f3InitiativeRider = rider.Stats.Initiative.BaseValue; f3InitiativeMount = horse.Stats.Initiative.BaseValue;
                    f3InitiativeSecondRider = f3BRider.Stats.Initiative.BaseValue; f3InitiativeSecondMount = f3BMount.Stats.Initiative.BaseValue; f3InitiativeSet = true;
                    rider.Stats.Initiative.BaseValue = 40; horse.Stats.Initiative.BaseValue = -40;
                    f3BMount.Stats.Initiative.BaseValue = 40; f3BRider.Stats.Initiative.BaseValue = -40;
                    f3Measure["initiativeSetup"] = new JObject { ["aRider"] = 40, ["aMount"] = -40, ["bRider"] = -40, ["bMount"] = 40,
                        ["original"] = new JObject { ["aRider"] = f3InitiativeRider, ["aMount"] = f3InitiativeMount, ["bRider"] = f3InitiativeSecondRider, ["bMount"] = f3InitiativeSecondMount } };
                    F3Disclose("Initiative base values set before combat: pair A rider +40 / mount -40, pair B mount +40 / rider -40; restored at cleanup.");
                    F3BeginTarget(10f, "f3-tb");
                    ruleProbe.Arm(target, false); f3Rules.Watch(target);
                    castingCosts.BeginEncounter(request.RunId + ":finalize3-tb");
                    // Native Prepare entries are counted from before combat, so the first activation's own are kept.
                    foreach (var unit in new[] { rider, horse, f3BRider, f3BMount }) f3Grants[unit] = castingCosts.GrantCount(unit);
                    F3Disclose("A durable hostile was spawned 10 m from pair A to start turn-based combat (the second pair declared, not leased).");
                    F3Steps["entry"] = F3Snap();
                    f3Step = 1; return;
                }
                case 1:
                {
                    if (!rider.IsInCombat || !horse.IsInCombat || !f3BRider.IsInCombat || !f3BMount.IsInCombat || turn == null) return;
                    var controller = Game.Instance.TurnBasedCombatController;
                    f3FirstRound = controller.RoundNumber;
                    F3Steps["combat"] = new JObject { ["snap"] = F3Snap(), ["initiative"] = new JObject { ["aRider"] = rider.CombatState.Initiative,
                        ["aMount"] = horse.CombatState.Initiative, ["bRider"] = f3BRider.CombatState.Initiative, ["bMount"] = f3BMount.CombatState.Initiative },
                        ["sorted"] = new JArray(controller.SortedUnits.Select(unit => F3Role(unit) ?? unit.UniqueId)), ["turnBased"] = CombatController.IsInTurnBasedCombat(),
                        ["targetId"] = target.UniqueId };
                    f3LastTurn = null; F3ObserveTurns(turn);
                    FinishF3Row(true, "Turn-based combat with both pairs mounted observed.");
                    return;
                }
            }
        }

        // The tranche's BeginTarget with the second pair declared, so its actors are commanded rather than leased.
        private void F3BeginTarget(float distance, string suffix)
        {
            if (targetService != null) throw new InvalidOperationException("Phase 3D target lease is already active.");
            targetService = new DiagnosticCombatTargetService(logger, repeatedNativeSequences: true);
            var point = FindWalkablePoint(rider.Position, distance, distance >= 10f ? 1.0f : 0.5f,
                candidate => f3BMount == null || HorizontalDistance(candidate, f3BMount.Position) >= 5f);
            target = targetService.Spawn(rider, horse, point, request.RunId + "-" + suffix, true, true, false,
                f3BRider != null && f3BMount != null ? new[] { f3BRider, f3BMount } : null);
            if (!targetService.PrepareForPlayerClick(target) || !targetService.QueueBidirectionalCombatMemory(rider, target))
                throw new InvalidOperationException("F3 hostile target visibility/combat-memory lease failed.");
            ruleProbe.Arm(target, true);
            observations["target-" + suffix] = new JObject { ["targetId"] = target.UniqueId, ["distance"] = rider.DistanceTo(target),
                ["bidirectionalHostility"] = targetService.BidirectionalHostilityVerified, ["noLoot"] = targetService.TargetHasNoLoot,
                ["durabilityLease"] = targetService.TargetDurabilityLeaseAcquired, ["nothingToLease"] = targetService.NonPairPartyAiLease?.NothingToLease };
            ResetLeafClock();
        }

        // ---- F3-tb-a-round1: preview, rider-selected move, mount attack, rider attack, refusal (pair A, rider-first) ------
        private void TickF3ARound1(TurnController turn)
        {
            var participant = combat.TurnParticipant;
            switch (f3Step)
            {
                case 0:
                    if (!F3Activation(turn, rider, horse, f3LastATurn)) return;
                    f3LastATurn = turn;
                    F3StartActivation(turn, "a-round1-start", rider, horse, participant);
                    F3Shot("tracker-round1", rider);
                    f3Step = 1; return;
                case 1:
                {
                    var speed = horse.CurrentSpeedMps;
                    var record = F3Begin("a2", horse, participant);
                    var destination = F3Destination(horse, f3BMount, 0.5f * 3f * speed);
                    record["requestedMetres"] = HorizontalDistance(horse.Position, destination);
                    record["autoStopAfterFirstMove"] = Kingmaker.UI.SettingsUI.SettingsRoot.Instance.AutoStopAfterFirstMoveAction.CurrentValue;
                    F3GroundClick("a2", destination, true);
                    f3Step = 2; return;
                }
                case 2:
                    if (!F3Settled(rider, horse, combat)) return;
                    F3End(horse, participant);
                    {
                        var remaining = (3f - horse.CombatState.Cooldown.MoveAction) * horse.CurrentSpeedMps;
                        F3PanelHover("a2", horse, participant, Mathf.Max(0.5f, 0.5f * remaining));
                        F3DrawPath("a2", horse, participant, Mathf.Max(1f, remaining) + 3f);
                    }
                    F3Shot("a2-after-move", rider);
                    f3Step = 3; return;
                case 3:
                    F3PlaceAdjacent(horse, "pair A mount attack");
                    f3Step = 4; return;
                case 4:
                    if (++f3Frames < Finalize3SettleFrames) return;
                    F3Begin("a3", horse, participant);
                    F3UnitClick("a3", horse, true);
                    f3Step = 5; return;
                case 5:
                    if (!F3AttackSettled(horse, rider, horse, combat)) return;
                    F3End(horse, participant, false); f3Current["actorRules"] = F3RulesBy(horse);
                    F3Select(rider); f3Step = 6; return;
                case 6:
                    if (++f3Frames < Finalize3SettleFrames) return;
                    F3Begin("a4", horse, participant);
                    F3UnitClick("a4", rider, true);
                    f3Step = 7; return;
                case 7:
                    if (!F3AttackSettled(rider, rider, horse, combat)) return;
                    F3End(horse, participant, false); f3Current["actorRules"] = F3RulesBy(rider);
                    f3Step = 8; return;
                case 8:
                    if (++f3Frames < Finalize3SettleFrames) return;
                    F3BeginRefusal("a4Refusal", rider, horse, combat, participant);
                    f3Step = 9; return;
                case 9:
                    if (++f3Frames < Finalize3RefusalFrames) return;
                    F3EndRefusal(rider, horse, combat, participant);
                    f3Step = 10; return;
                case 10:
                    if (!F3CloseActivation(turn, rider, horse, combat)) return;
                    FinishF3Row(true, "Pair A round-one observations recorded.");
                    return;
            }
        }

        private bool F3AttackSettled(UnitEntityData actor, UnitEntityData principal, UnitEntityData partner, MountedCombatController pair)
        {
            var attacked = F3RulesBy(actor) > 0;
            if (!attacked && ++f3Frames < Finalize3AttackFrames) return false;
            if (!F3Settled(principal, partner, pair)) return false;
            f3Current["attackObserved"] = attacked;
            return true;
        }

        private void F3BeginRefusal(string name, UnitEntityData actor, UnitEntityData mount, MountedCombatController pair, MountedPairTurnParticipant participant)
        {
            var record = F3Begin(name, mount, participant);
            record["feedbackBefore"] = pair?.LastFeedback; record["traceBefore"] = pair?.LastStandardRefusalTrace;
            F3UnitClick(name, actor, true);
        }

        private void F3EndRefusal(UnitEntityData actor, UnitEntityData mount, MountedCombatController pair, MountedPairTurnParticipant participant)
        {
            F3End(mount, participant, false);
            f3Current["actorRules"] = F3RulesBy(actor);
            f3Current["feedback"] = pair?.LastFeedback; f3Current["trace"] = pair?.LastStandardRefusalTrace;
            f3Current["actorName"] = actor.CharacterName;
        }

        // The live prediction panel the player reads (TurnBasedModeUIController's PredictionPanelVM), slot by slot, with the
        // actor whose native action each slot resolves to now.
        private static readonly FieldInfo F3PanelVm =
            typeof(Kingmaker.UI.TurnBasedMode.TurnBasedModeUIController).GetField("m_PredictionPanelVM", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly PropertyInfo F3SlotAction =
            typeof(Kingmaker.UI._ConsoleUI.TurnBasedMode.PredictionPanelActionVM).GetProperty("CombatAction", BindingFlags.Instance | BindingFlags.NonPublic);

        private JObject F3Panel(MountedPairTurnParticipant participant)
        {
            var result = new JObject { ["frame"] = Time.frameCount };
            try
            {
                var vm = F3PanelVm?.GetValue(Game.Instance.UI.TurnBasedUI) as Kingmaker.UI._ConsoleUI.TurnBasedMode.PredictionPanelVM;
                if (vm == null) { result["error"] = "No live prediction panel."; return result; }
                var riderStates = Game.Instance.TurnBasedCombatController.CurrentTurn?.ActionsStates;
                var mountStates = participant?.PartnerContext?.ActionsStates;
                Func<Kingmaker.TurnBasedMode.ActionsState, Kingmaker.TurnBasedMode.Controllers.CombatAction, bool> owns = (states, action) =>
                    states != null && (ReferenceEquals(action, states.Move) || ReferenceEquals(action, states.FiveFootStep) ||
                        ReferenceEquals(action, states.Standard) || ReferenceEquals(action, states.Swift) || ReferenceEquals(action, states.Free));
                Func<Kingmaker.UI._ConsoleUI.TurnBasedMode.PredictionPanelActionVM, JObject> slot = s =>
                {
                    if (s == null) return null;
                    var action = F3SlotAction.GetValue(s, null) as Kingmaker.TurnBasedMode.Controllers.CombatAction;
                    // The slot's reactive values (UniRx ReactiveProperty, read by reflection: not a KMC reference).
                    Func<string, object> value = name =>
                    {
                        var property = s.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public)?.GetValue(s);
                        return property?.GetType().GetProperty("Value")?.GetValue(property, null);
                    };
                    Func<string, JToken> text = name => value(name)?.ToString();
                    Func<string, JToken> number = name => value(name) is float distance ? (JToken)distance : null;
                    return new JObject { ["owner"] = owns(mountStates, action) ? "mount" : owns(riderStates, action) ? "rider" : action == null ? null : "other",
                        ["state"] = text("State"), ["moveActivity"] = text("MoveActivityState"), ["attackActivity"] = text("AttackActivityState"),
                        ["possible"] = number("PossibleMoveDistance"), ["remaining"] = number("RemainingMoveDistance"), ["predicted"] = number("PredictedMoveDistance") };
                };
                result["fiveFeet"] = slot(vm.FiveFeetAction); result["move"] = slot(vm.MoveAction); result["standard"] = slot(vm.StandardAction);
                result["fullRound"] = slot(vm.FullAction); result["swift"] = slot(vm.SwiftAction);
            }
            catch (Exception exception) { result["error"] = exception.GetType().Name + ": " + (exception.InnerException?.Message ?? exception.Message); }
            return result;
        }

        // A genuine native ground hover (pointer over terrain, native UpdateActionPredictions) without a click, then the
        // live panel and the path it drew.
        private JObject F3PanelHover(string name, UnitEntityData mount, MountedPairTurnParticipant participant, float distance)
        {
            var record = new JObject { ["distance"] = distance, ["before"] = F3Snap() };
            F3Steps[name + "Panel"] = record;
            try
            {
                var point = F3Destination(mount, null, distance);
                record["point"] = CapturePosition(point);
                using (var input = new NativeOrdinaryAttackInput(point))
                {
                    input.Predict();
                    record["panel"] = F3Panel(participant);
                    record["probe"] = F3Probe(mount, participant);
                }
            }
            catch (Exception exception) { record["error"] = exception.GetType().Name + ": " + exception.Message; }
            record["after"] = F3Snap();
            return record;
        }

        private UnitAttack f3BAttack;

        private static JObject F3AttackFacts(UnitAttack attack) => attack == null ? null : new JObject { ["type"] = attack.GetType().Name,
            ["executor"] = attack.Executor?.UniqueId, ["isFullAttack"] = attack.IsFullAttack, ["isSingleAttack"] = attack.IsSingleAttack,
            ["plannedAttacks"] = attack.AllAttacks.Count, ["deliveredAttacks"] = attack.GetAttackIndex(), ["shouldApproach"] = attack.ShouldUnitApproach,
            ["isFinished"] = attack.IsFinished, ["result"] = attack.Result.ToString() };

        // ---- F3-tb-b-round1: a genuine native rider full attack with the mount unmoved, then a rider-selected move on the
        // mount's own Move in the same activation (pair B, mount-first)
        private void TickF3BRound1(TurnController turn)
        {
            var participant = F3BParticipant; var pair = F3BCombat;
            switch (f3Step)
            {
                case 0:
                    if (!F3Activation(turn, f3BRider, f3BMount, f3LastBTurn)) return;
                    f3LastBTurn = turn;
                    F3StartActivation(turn, "b-round1-start", f3BRider, f3BMount, participant);
                    F3PlaceAdjacent(f3BMount, "pair B rider full attack");
                    f3Step = 1; return;
                case 1:
                {
                    if (++f3Frames < Finalize3SettleFrames) return;
                    var record = F3Begin("b1", f3BMount, participant);
                    F3Select(f3BRider);
                    record["selectedRole"] = F3Role(SelectionManager.Instance?.SelectedUnits?.Count == 1 ? SelectionManager.Instance.SelectedUnits[0] : null);
                    record["riderDistanceToTarget"] = f3BRider.DistanceTo(target);
                    record["estimatedFullAttacks"] = UnitAttack.EstimateFullAttacks(f3BRider);
                    record["riderHasFullRound"] = f3BRider.HasFullRoundAction();
                    f3RulesAt = f3Rules.Attacks.Count; f3BAttack = null;
                    // The ordinary player input: hover the target, cycle the native attack variant to Full with the native
                    // right-click (Chunk 4's recipe), then left-click. Hovering and cycling must not change any live state.
                    try
                    {
                        using (var input = new NativeOrdinaryAttackInput(target))
                        {
                            var before = new JArray(F3Actor(f3BRider), F3Actor(f3BMount));
                            input.Predict(turn); input.Predict(turn);
                            var cycles = 0;
                            while (!turn.EnabledFullAttack && cycles++ < 8) { input.Click(button: 1); input.Predict(turn); }
                            record["hoverChangedState"] = !JToken.DeepEquals(before, new JArray(F3Actor(f3BRider), F3Actor(f3BMount)));
                            record["cursorCycles"] = cycles; record["enabledFullAttack"] = turn.EnabledFullAttack;
                            record["clicked"] = turn.EnabledFullAttack && input.Click();
                        }
                    }
                    catch (Exception exception) { record["clicked"] = false; record["error"] = exception.GetType().Name + ": " + exception.Message; }
                    if ((bool?)record["clicked"] != true) { F3Fail("The ordinary native Full attack input was unavailable or refused."); return; }
                    f3Step = 2; return;
                }
                case 2:
                    if (f3BAttack == null) f3BAttack = f3BRider.Commands.GetCommand(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard) as UnitAttack;
                    // Native UnitAttack.OnStart plans the strikes and decides IsFullAttack; read them once it has started.
                    if (f3BAttack != null && f3BAttack.IsStarted && f3Current["attackStarted"] == null)
                        f3Current["attackStarted"] = new JObject { ["facts"] = F3AttackFacts(f3BAttack), ["snap"] = F3Snap() };
                    if (!F3AttackSettled(f3BRider, f3BRider, f3BMount, pair)) return;
                    if (++f3Frames < Finalize3SettleFrames) return;
                    F3End(f3BMount, participant); f3Current["actorRules"] = F3RulesBy(f3BRider);
                    f3Current["attack"] = F3AttackFacts(f3BAttack);
                    f3Current["riderHasNormalMovement"] = turn.HasNormalMovement();
                    f3Current["riderLimit"] = turn.CurrentMovementLimit.ToString();
                    F3PanelHover("b1", f3BMount, participant, 0.4f * 3f * f3BMount.CurrentSpeedMps);
                    F3DrawPath("b1", f3BMount, participant, 3f * f3BMount.CurrentSpeedMps + 3f);
                    F3Shot("b1-after-genuine-full-attack", f3BRider);
                    f3Step = 3; return;
                case 3:
                {
                    var record = F3Begin("b2", f3BMount, participant);
                    var destination = F3Destination(f3BMount, horse, 0.4f * 3f * f3BMount.CurrentSpeedMps);
                    record["requestedMetres"] = HorizontalDistance(f3BMount.Position, destination);
                    record["partnerLimit"] = participant?.PartnerContext?.CurrentMovementLimit.ToString();
                    F3GroundClick("b2", destination, true);
                    f3Step = 4; return;
                }
                case 4:
                    if (!F3Settled(f3BRider, f3BMount, pair)) return;
                    F3End(f3BMount, participant);
                    F3PanelHover("b2", f3BMount, participant, 0.3f * 3f * f3BMount.CurrentSpeedMps);
                    F3Shot("b2-after-mount-move", f3BRider);
                    f3Step = 5; return;
                case 5:
                    if (!F3CloseActivation(turn, f3BRider, f3BMount, pair)) return;
                    FinishF3Row(true, "Pair B round-one observations recorded.");
                    return;
            }
        }

        // ---- F3-tb-a-step: five-foot step through the native movement-limit input, then a refused ordinary move ---------
        private void TickF3AStep(TurnController turn)
        {
            var participant = combat.TurnParticipant;
            switch (f3Step)
            {
                case 0:
                    if (!F3Activation(turn, rider, horse, f3LastATurn)) return;
                    f3LastATurn = turn;
                    F3StartActivation(turn, "a-round2-start", rider, horse, participant);
                    f3Step = 1; return;
                case 1:
                {
                    var record = F3Begin("s1", horse, participant);
                    var contextsBefore = new JObject { ["current"] = F3Context(turn), ["partner"] = F3Context(participant?.PartnerContext) };
                    record["contextsBefore"] = contextsBefore;
                    f3StepOn.Invoke(Game.Instance.TurnBasedCombatController, null);
                    record["contextsAfterInput"] = new JObject { ["current"] = F3Context(turn), ["partner"] = F3Context(participant?.PartnerContext) };
                    f3Step = 2; return;
                }
                case 2:
                {
                    var partner = participant?.PartnerContext;
                    if ((partner == null || !partner.EnabledFiveFootStep) && ++f3Frames < 30) return;
                    var current = F3Context(turn); var partnerNow = F3Context(partner);
                    var before = (JObject)f3Current["contextsBefore"];
                    f3Current["contextsStepMode"] = new JObject { ["current"] = current, ["partner"] = partnerNow };
                    var currentChanged = F3Limit(before["current"]) != F3Limit(current);
                    var partnerChanged = F3Limit(before["partner"]) != F3Limit(partnerNow);
                    f3Current["changedContext"] = partnerChanged && !currentChanged ? "partner" : currentChanged && !partnerChanged ? "current" : partnerChanged ? "both" : "none";
                    // Beyond the step allowance: the native step clamps, so the whole step is spent.
                    F3GroundClick("s1", F3Destination(horse, f3BMount, 3f), true);
                    f3Step = 3; return;
                }
                case 3:
                    if (!F3Settled(rider, horse, combat) && ++f3Frames < 240) return;
                    f3StepOff.Invoke(Game.Instance.TurnBasedCombatController, null);
                    f3Step = 4; return;
                case 4:
                    if (++f3Frames < 4) return;
                    F3End(horse, participant);
                    f3Current["contextsAfterStepOff"] = new JObject { ["current"] = F3Context(turn), ["partner"] = F3Context(participant?.PartnerContext) };
                    F3PanelHover("s1", horse, participant, 4f);
                    F3Shot("s1-after-step", rider);
                    f3Step = 5; return;
                case 5:
                    F3Begin("s2", horse, participant);
                    F3GroundClick("s2", F3Destination(horse, f3BMount, 4f), true);
                    f3Step = 6; return;
                case 6:
                    if (++f3Frames < 60 || !F3Idle(rider, horse, combat)) return;
                    F3End(horse, participant);
                    f3Step = 7; return;
                case 7:
                    if (!F3CloseActivation(turn, rider, horse, combat)) return;
                    FinishF3Row(true, "Pair A step observations recorded.");
                    return;
            }
        }

        // ---- F3-tb-b-charge: the ordinary Charge from the rider's own slot, then no movement of any kind, then the mount -
        private void TickF3BCharge(TurnController turn)
        {
            var participant = F3BParticipant; var pair = F3BCombat;
            switch (f3Step)
            {
                case 0:
                    if (!F3Activation(turn, f3BRider, f3BMount, f3LastBTurn)) return;
                    f3LastBTurn = turn;
                    F3StartActivation(turn, "b-round2-start", f3BRider, f3BMount, participant);
                    f3Step = 1; return;
                case 1:
                {
                    if (pair == null) { F3Fail("The second pair's combat controller is unavailable."); return; }
                    JObject lane;
                    f3Facts["laneSearches"] = (f3Facts.Value<int?>("laneSearches") ?? 0) + 1;
                    if (!MountedChargeFixture.TryPlaceOnLane(f3BMount, f3BRider, target, Finalize3LaneRadii, out lane)) { f3Facts["lastAvailability"] = "no clear lane"; return; }
                    var availability = pair.ObserveMountedChargeTarget(f3BRider, target);
                    f3Facts["lastAvailability"] = availability.Reason;
                    f3Facts["lastLane"] = lane;
                    if (!availability.IsAllowed) return;
                    f3Facts["lane"] = lane;
                    F3Disclose("The hostile was placed on a clear charge lane from pair B's mount (MountedChargeFixture.TryPlaceOnLane).");
                    f3Frames = 0; f3Step = 2; return;
                }
                case 2:
                {
                    if (++f3Frames < Finalize3SettleFrames) return;
                    var record = F3Begin("c1", f3BMount, participant);
                    f3PeakForced = 0f; f3PeakAllocationForced = 0f; f3Charge = null; f3RulesAt = f3Rules.Attacks.Count;
                    record["click"] = F3StockChargeClick(pair);
                    if ((bool?)record["click"]["clicked"] != true) { F3Fail("The ordinary Charge click was refused: " + pair.LastMountedChargeRefusal); return; }
                    f3Step = 3; return;
                }
                case 3:
                {
                    f3Charge = pair.OwnedChargeCommand ?? f3Charge;
                    var forced = participant?.PartnerContext?.TimeMovedInForceMode ?? 0f;
                    if (forced > f3PeakForced) f3PeakForced = forced;
                    var allocation = participant?.PeekMountAllocation();
                    if (allocation != null && allocation.TimeForced > f3PeakAllocationForced) f3PeakAllocationForced = allocation.TimeForced;
                    if (f3Charge == null || !f3Charge.IsFinished || pair.HasChargeOwnership) return;
                    if (!F3Settled(f3BRider, f3BMount, pair)) return;
                    F3End(f3BMount, participant);
                    f3Current["terminal"] = new JObject { ["result"] = f3Charge.Result.ToString(), ["sequenceLawful"] = f3Charge.ChargeSequenceLawful,
                        ["revalidationFailed"] = f3Charge.ChargeRevalidationFailed, ["ownership"] = pair.LastDrainedChargeOwnership?.DeepClone() };
                    f3Current["chargeRules"] = F3RulesBy(f3BRider, true); f3Current["riderRules"] = F3RulesBy(f3BRider);
                    f3Current["peakPartnerForced"] = f3PeakForced; f3Current["peakAllocationForced"] = f3PeakAllocationForced;
                    f3Current["allocation"] = F3Allocation(participant?.PeekMountAllocation());
                    f3Step = 4; return;
                }
                case 4:
                    if (++f3Frames < 10) return;
                    F3Steps["c2"] = new JObject { ["snap"] = F3Snap(), ["probe"] = F3Probe(f3BMount, participant) };
                    F3PanelHover("c2", f3BMount, participant, 4f);
                    F3Shot("c2-after-charge", f3BRider);
                    f3Step = 5; return;
                case 5:
                    F3Select(f3BRider);
                    F3Begin("c3Move", f3BMount, participant);
                    F3GroundClick("c3Move", F3Destination(f3BMount, horse, 4f), true);
                    f3Step = 6; return;
                case 6:
                    if (++f3Frames < 60 || !F3Idle(f3BRider, f3BMount, pair)) return;
                    F3End(f3BMount, participant);
                    f3Step = 7; return;
                case 7:
                {
                    var record = F3Begin("c3Step", f3BMount, participant);
                    f3StepOn.Invoke(Game.Instance.TurnBasedCombatController, null);
                    record["contextsAfterInput"] = new JObject { ["current"] = F3Context(turn), ["partner"] = F3Context(participant?.PartnerContext) };
                    f3Step = 8; return;
                }
                case 8:
                    if (++f3Frames < 10) return;
                    F3GroundClick("c3Step", F3Destination(f3BMount, horse, 1f), true);
                    f3Step = 9; return;
                case 9:
                    if (++f3Frames < 70 || !F3Idle(f3BRider, f3BMount, pair)) return;
                    f3StepOff.Invoke(Game.Instance.TurnBasedCombatController, null);
                    F3End(f3BMount, participant);
                    f3Step = 10; return;
                case 10:
                    if (++f3Frames < 4) return;
                    F3Begin("c4", f3BMount, participant);
                    F3UnitClick("c4", f3BMount, true);
                    f3Step = 11; return;
                case 11:
                    if (!F3AttackSettled(f3BMount, f3BRider, f3BMount, pair)) return;
                    F3End(f3BMount, participant, false); f3Current["actorRules"] = F3RulesBy(f3BMount);
                    F3Select(f3BRider); f3Step = 12; return;
                case 12:
                    if (!F3CloseActivation(turn, f3BRider, f3BMount, pair)) return;
                    FinishF3Row(true, "Pair B charge observations recorded.");
                    return;
            }
        }

        // The ordinary Charge: the second rider's own bar slot holding its stock Charge, then the native target click.
        private JObject F3StockChargeClick(MountedCombatController pair)
        {
            var record = new JObject();
            var stock = f3BRider.Descriptor.Abilities.Enumerable.FirstOrDefault(fact => fact.Active && MountedChargeSafetyService.IsExactNativeCharge(fact.Data));
            record["stockPresent"] = stock != null;
            if (stock == null) { record["clicked"] = false; return record; }
            var slots = f3BRider.UISettings.Slots; var index = -1;
            for (var i = 0; i < slots.Length && index < 0; i++)
                if (ReferenceEquals((slots[i] as MechanicActionBarSlotAbility)?.Ability?.Blueprint, stock.Blueprint)) index = i;
            record["slotSource"] = index >= 0 ? "pre-existing" : "placed-setup";
            if (index < 0)
            {
                for (var i = 0; i < slots.Length && index < 0; i++) if (F2SlotKey(slots[i]) == "") index = i;
                if (index < 0) { record["clicked"] = false; record["error"] = "No empty slot for the stock Charge."; return record; }
                f3BRider.UISettings.SetSlot(new MechanicActionBarSlotAbility { Unit = f3BRider, Ability = stock.Data }, index);
                F3Disclose("The second rider's stock Charge fact was placed in empty bar slot " + index + ", as a player places it.");
            }
            F3Select(f3BRider);
            nativeControls.Update();
            var slot = f3BRider.UISettings.Slots[index] as MechanicActionBarSlotAbility;
            var handler = Game.Instance.SelectedAbilityHandler;
            var castsBefore = nativeControls.NativeCastRequestCount; var admittedBefore = pair.MountedChargeAdmittedCount;
            record["index"] = index; record["slotAbilityBefore"] = slot?.Ability?.Blueprint?.AssetGuid;
            record["availability"] = pair.ObserveMountedChargeTarget(f3BRider, target).Reason;
            slot?.OnClick();
            record["slotAbilityAfter"] = slot?.Ability?.Blueprint?.AssetGuid;
            record["handlerAfter"] = handler?.Ability?.Blueprint?.AssetGuid;
            record["handlerIsShell"] = ReferenceEquals(handler?.Ability?.Blueprint, nativeControls.MountedChargeAbility);
            var clicked = false;
            try { clicked = handler?.Ability != null && target?.View != null && handler.OnClick(target.View.gameObject, target.Position, 0, false, false); }
            finally { if (handler?.Ability != null) handler.DropAbility(); }
            record["clicked"] = clicked; record["castDelta"] = nativeControls.NativeCastRequestCount - castsBefore;
            record["admittedDelta"] = pair.MountedChargeAdmittedCount - admittedBefore;
            record["shell"] = pair.OwnedChargeShell != null; record["shellTarget"] = pair.OwnedChargeShell?.Target?.Unit?.UniqueId; record["targetId"] = target?.UniqueId;
            return record;
        }

        // ---- F3-tb-a-double: a move into the native Standard conversion, then the mount's refused attack -----------------
        private void TickF3ADouble(TurnController turn)
        {
            var participant = combat.TurnParticipant;
            switch (f3Step)
            {
                case 0:
                    if (!F3Activation(turn, rider, horse, f3LastATurn)) return;
                    f3LastATurn = turn;
                    F3StartActivation(turn, "a-round3-start", rider, horse, participant);
                    f3Step = 1; return;
                case 1:
                {
                    var record = F3Begin("d1", horse, participant);
                    // Straight-line 3.8 s of speed: the path ends in the second Move (beyond the green range, inside the total).
                    var destination = F3Destination(horse, f3BMount, 3.8f * horse.CurrentSpeedMps);
                    record["requestedMetres"] = HorizontalDistance(horse.Position, destination);
                    record["autoStopAfterFirstMove"] = Kingmaker.UI.SettingsUI.SettingsRoot.Instance.AutoStopAfterFirstMoveAction.CurrentValue;
                    F3GroundClick("d1", destination, true);
                    f3Step = 2; return;
                }
                case 2:
                    if (!F3Settled(rider, horse, combat)) return;
                    F3End(horse, participant);
                    F3PanelHover("d1", horse, participant, 2f);
                    F3PlaceAdjacent(horse, "pair A mount attack after the double move");
                    f3Step = 3; return;
                case 3:
                    if (++f3Frames < Finalize3SettleFrames) return;
                    F3BeginRefusal("d2", horse, horse, combat, participant);
                    f3Step = 4; return;
                case 4:
                    if (++f3Frames < Finalize3RefusalFrames) return;
                    F3EndRefusal(horse, horse, combat, participant);
                    F3Select(rider);
                    f3Step = 5; return;
                case 5:
                    if (!F3CloseActivation(turn, rider, horse, combat)) return;
                    FinishF3Row(true, "Pair A double-move observations recorded.");
                    return;
            }
        }

        // ---- F3-tb-b-dismount: dismount in the activation, then the released mount's own native turn next round ---------
        private void TickF3BDismount(TurnController turn)
        {
            var participant = F3BParticipant; var pair = F3BCombat;
            var round = Game.Instance.TurnBasedCombatController.RoundNumber;
            switch (f3Step)
            {
                case 0:
                    if (!F3Activation(turn, f3BRider, f3BMount, f3LastBTurn)) return;
                    f3LastBTurn = turn;
                    F3StartActivation(turn, "b-round3-start", f3BRider, f3BMount, participant);
                    // A turn becomes Acting only through a real order (native facts): a short ground move first, disclosed.
                    F3Begin("actingSetup", f3BMount, participant);
                    F3GroundClick("actingSetup", F3Destination(f3BMount, horse, 2f), true);
                    F3Disclose("A short ground move of pair B made its turn Acting before the combat Dismount.");
                    f3Step = 1; return;
                case 1:
                    if (!F3Settled(f3BRider, f3BMount, pair)) return;
                    F3End(f3BMount, participant, false);
                    f3Step = 2; return;
                case 2:
                {
                    var record = F3Begin("dismount", f3BMount, participant);
                    var control = nativeControls.MountStateAbility; var index = -1;
                    var slots = f3BRider.UISettings?.Slots;
                    if (control != null && slots != null)
                        for (var i = 0; i < slots.Length && index < 0; i++)
                            if (ReferenceEquals((slots[i] as MechanicActionBarSlotAbility)?.Ability?.Blueprint, control)) index = i;
                    F3Select(f3BRider);
                    nativeControls.Update();
                    if (index >= 0)
                    {
                        record["route"] = "public-control"; record["index"] = index;
                        (slots[index] as MechanicActionBarSlotAbility)?.OnClick();
                        var handler = Game.Instance.SelectedAbilityHandler;
                        if (handler?.Ability != null) handler.DropAbility();
                    }
                    else
                    {
                        record["route"] = "dismount-shell";
                        F3Disclose("The second rider has no public control on its bar: Dismount through the hidden shell (Sprint 6 route).");
                        record["clicked"] = Sprint5Fixture.Click(nativeControls, f3BRider, nativeControls.DismountAbility, f3BRider);
                    }
                    f3Step = 3; return;
                }
                case 3:
                    if (F3BState() == RelationshipState.Mounted.ToString() || !F3Idle(f3BRider, f3BMount, null)) return;
                    if (++f3Frames < Finalize3SettleFrames) return;
                    F3End(f3BMount, participant, false);
                    f3DismountRound = round;
                    f3Current["dismountRound"] = round; f3Current["dismountFrame"] = Time.frameCount;
                    f3Current["participation"] = F3Participation(participant);
                    f3Current["tracker"] = F3Tracker("b-round3-after-dismount");
                    f3Facts["dismountRound"] = round;
                    f3Step = 4; return;
                case 4:
                    if (ReferenceEquals(turn, f3RowTurn)) { if (F3Idle(f3BRider, f3BMount, null)) F3EndTurn(turn, "activation end after dismount"); return; }
                    F3Steps["end"] = new JObject { ["snap"] = F3Snap(), ["endedNatively"] = ReferenceEquals(f3EndedTurn, f3RowTurn) };
                    f3Step = 5; return;
                case 5:
                {
                    // Every party turn up to the second rider's next one: the released mount's own turn is observed, then ended.
                    if (turn?.Unit == null || !F3PartyActor(turn.Unit)) return;
                    if (round > f3DismountRound && F3Steps["round4Tracker"] == null) F3Steps["round4Tracker"] = F3Tracker("round-after-dismount");
                    if (turn.Unit == f3BMount && F3Steps["mountOwnTurn"] == null)
                        F3Steps["mountOwnTurn"] = new JObject { ["snap"] = F3Snap(), ["round"] = round, ["tracker"] = F3Tracker("mount-own-turn") };
                    if (turn.Unit == f3BRider && round > f3DismountRound && !ReferenceEquals(turn, f3RowTurn))
                    {
                        F3Steps["nextRiderTurn"] = new JObject { ["snap"] = F3Snap(), ["round"] = round };
                        FinishF3Row(true, "Pair B dismount observations recorded.");
                        return;
                    }
                    F3EndTurn(turn, "after dismount");
                    return;
                }
            }
        }

        // ---- real time: the pair's destination marker and the ordinary attack ------------------------------------------
        private JObject F3Marker(UnitEntityData unit)
        {
            var result = new JObject { ["unit"] = unit?.UniqueId };
            try
            {
                var manager = Kingmaker.UI.ClickPointerManager.Instance;
                Kingmaker.UI.ClickPointerPrefab mark = null;
                result["manager"] = manager != null;
                if (manager?.UnitMarks != null && unit != null && manager.UnitMarks.TryGetValue(unit, out mark) && mark != null)
                {
                    result["present"] = true; result["visible"] = mark.IsVisible; result["alpha"] = Convert.ToDouble(f3MarkAlpha.GetValue(mark));
                    result["active"] = mark.gameObject.activeInHierarchy;
                }
                else result["present"] = false;
                if (manager?.UnitMarks != null) result["keys"] = new JArray(manager.UnitMarks.Keys.Where(key => key != null).Select(key => F3Role(key) ?? key.UniqueId));
            }
            catch (Exception exception) { result["error"] = exception.GetType().Name + ": " + exception.Message; }
            return result;
        }

        private void F3BeginMarkerWatch() { f3MarkMaxAlpha = -1f; f3MarkMinAlpha = -1f; f3MarkFrames = 0; f3MarkVisibleFrames = 0; }

        private void F3WatchMarker(UnitEntityData marked)
        {
            var marker = F3Marker(marked);
            f3MarkFrames++;
            if ((bool?)marker["present"] != true) return;
            if ((bool?)marker["visible"] == true) f3MarkVisibleFrames++;
            var alpha = (float)(double)marker["alpha"];
            if (f3MarkMaxAlpha < 0f || alpha > f3MarkMaxAlpha) f3MarkMaxAlpha = alpha;
            if (f3MarkMinAlpha < 0f || alpha < f3MarkMinAlpha) f3MarkMinAlpha = alpha;
        }

        private JObject F3MarkerSummary(UnitEntityData marked) => new JObject { ["role"] = F3Role(marked), ["framesWatched"] = f3MarkFrames, ["visibleFrames"] = f3MarkVisibleFrames,
            ["maxAlpha"] = f3MarkMaxAlpha < 0f ? (double?)null : f3MarkMaxAlpha, ["minAlpha"] = f3MarkMinAlpha < 0f ? (double?)null : f3MarkMinAlpha, ["last"] = F3Marker(marked) };

        private void TickF3RtMounted()
        {
            switch (f3Step)
            {
                case 0:
                {
                    if (relationship.State != RelationshipState.Mounted || !F3Idle(rider, horse, combat)) return;
                    F3Select(rider);
                    var record = F3Begin("move", horse, null);
                    record["riderFrom"] = CapturePosition(rider.Position);
                    F3BeginMarkerWatch();
                    F3GroundClick("move", F3Destination(horse, null, 6f), false);
                    f3Step = 1; return;
                }
                case 1:
                {
                    var moving = horse.View?.AgentASP?.IsReallyMoving ?? false;
                    if (moving) { F3WatchMarker(horse); if (f3Current["whileMoving"] == null && f3MarkFrames >= 3) { f3Current["whileMoving"] = new JObject { ["mount"] = F3Marker(horse), ["rider"] = F3Marker(rider), ["selected"] = F3SelectedId() }; F3Shot("rt-marker", rider); } }
                    if (!F3Settled(rider, horse, combat) && ++f3Frames < 600) return;
                    F3End(horse, null, false);
                    f3Current["marker"] = F3MarkerSummary(horse);
                    f3Current["riderDelta"] = HorizontalDistance(F3Vector((JObject)f3Current["riderFrom"]), rider.Position);
                    f3Current["riderMountSeparation"] = HorizontalDistance(rider.Position, horse.Position);
                    FinishF3Row(true, "Mounted destination-marker observations recorded.");
                    return;
                }
            }
        }

        private static bool? F3Limit(JToken context) => (bool?)(context as JObject)?["modifyLimit"];

        private static Vector3 F3Vector(JObject value) => new Vector3((float)value["x"], (float)value["y"], (float)value["z"]);

        private void TickF3RtUnmounted()
        {
            switch (f3Step)
            {
                case 0:
                {
                    if (f3BRider == null) { F3Fail("No second party member for the unmounted control."); return; }
                    if (!f3BRider.Commands.Empty || f3BRider.AreHandsBusyWithAnimation || ++f3Frames < 10) return;
                    F3Select(f3BRider);
                    f3From = f3BRider.Position; f3IdleFrames = 0;
                    var record = new JObject { ["before"] = F3Snap(), ["riderMounted"] = F3BState() == RelationshipState.Mounted.ToString() };
                    F3Steps["controlMove"] = record; f3Current = record;
                    F3BeginMarkerWatch();
                    F3GroundClick("controlMove", FindWalkablePoint(f3BRider.Position, 6f, 1f), false);
                    f3Step = 1; return;
                }
                case 1:
                {
                    var moving = f3BRider.View?.AgentASP?.IsReallyMoving ?? false;
                    if (moving) { F3WatchMarker(f3BRider); if (f3Current["whileMoving"] == null && f3MarkFrames >= 3) f3Current["whileMoving"] = new JObject { ["rider"] = F3Marker(f3BRider), ["selected"] = F3SelectedId() }; }
                    if (!F3Settled(f3BRider, null, null) && ++f3Frames < 600) return;
                    f3Current["after"] = F3Snap(); f3Current["riderDelta"] = HorizontalDistance(f3From, f3BRider.Position);
                    f3Current["marker"] = F3MarkerSummary(f3BRider);
                    F3Select(rider);
                    FinishF3Row(true, "Unmounted destination-marker observations recorded.");
                    return;
                }
            }
        }

        private void TickF3RtAttack()
        {
            switch (f3Step)
            {
                case 0:
                    if (!F3Idle(rider, horse, combat) || f3BRider != null && !f3BRider.Commands.Empty || f3BMount != null && !f3BMount.Commands.Empty) return;
                    BeginTarget(8f, "f3-rt"); ruleProbe.Arm(target, false); f3Rules.Watch(target);
                    F3Disclose("A durable hostile was spawned 8 m from the rider to start real-time combat.");
                    f3Step = 1; return;
                case 1:
                    if (!rider.IsInCombat || !horse.IsInCombat || !rider.CombatState.CanActInCombat || !target.IsVisibleForPlayer) return;
                    F3Begin("attack", horse, null);
                    F3UnitClick("attack", rider, false);
                    f3Step = 2; return;
                case 2:
                    if (F3RulesBy(rider) == 0 && ++f3Frames < Finalize3AttackFrames) return;
                    F3End(horse, null, false);
                    f3Current["actorRules"] = F3RulesBy(rider);
                    f3Current["rule"] = f3Rules.Attacks.Skip(f3RulesAt).FirstOrDefault(rule => (string)rule["attacker"] == rider.UniqueId && (string)rule["target"] == target.UniqueId)?.DeepClone();
                    FinishF3Row(true, "Real-time ordinary attack observations recorded.");
                    return;
            }
        }
    }
}
