using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.CharGen;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Root;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UI.LevelUp;
using Kingmaker.UI.LevelUp.Phase;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.FactLogic;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // The owning scenario establishes and continuously asserts exact isolated
    // load/write authority (including native autosave) before calling Tick.
    // This helper drives the installed UI controller, not a replacement level
    // controller. It cannot authorize a live transaction or qualify a profile.
    internal enum Chunk7OrdinaryBuildMode { NewSmallCustomCompanion, ExistingRangerLevels }
    internal enum Chunk7OrdinaryBuildStep { Waiting, Advanced, Complete }

    internal sealed class Chunk7OrdinaryBuildPins
    {
        internal readonly string RunId;
        internal readonly string NativeInventorySha256;
        internal readonly Chunk7OrdinaryBuildMode Mode;
        internal readonly string SmallRaceGuid;
        internal readonly string SmallPresetGuid;
        internal readonly string SkeletonName;
        internal readonly Gender Gender;
        internal readonly bool LeftHanded;
        internal readonly int MinimumRangerLevel;

        internal Chunk7OrdinaryBuildPins(string runId, string nativeInventorySha256, Chunk7OrdinaryBuildMode mode,
            string smallRaceGuid, string smallPresetGuid, string skeletonName, Gender gender, bool leftHanded,
            int minimumRangerLevel = 4)
        {
            RunId = runId; NativeInventorySha256 = nativeInventorySha256; Mode = mode;
            SmallRaceGuid = smallRaceGuid; SmallPresetGuid = smallPresetGuid; SkeletonName = skeletonName;
            Gender = gender; LeftHanded = leftHanded; MinimumRangerLevel = minimumRangerLevel;
            Validate();
        }

        internal void Validate()
        {
            Require(Regex.IsMatch(RunId ?? "", "^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$"), "Invalid run identity.");
            Require(Regex.IsMatch(NativeInventorySha256 ?? "", "^[a-f0-9]{64}$"), "Exact native inventory hash is required.");
            Require(Mode == Chunk7OrdinaryBuildMode.NewSmallCustomCompanion || Mode == Chunk7OrdinaryBuildMode.ExistingRangerLevels,
                "Unknown ordinary build mode.");
            Require(MinimumRangerLevel >= 4 && MinimumRangerLevel <= 20, "Unsupported Ranger target.");
            if (Mode != Chunk7OrdinaryBuildMode.NewSmallCustomCompanion) return;
            Require(Regex.IsMatch(SmallRaceGuid ?? "", "^[a-f0-9]{32}$") &&
                Regex.IsMatch(SmallPresetGuid ?? "", "^[a-f0-9]{32}$"), "Observed exact Small race and preset IDs are required.");
            Require(!string.IsNullOrWhiteSpace(SkeletonName) && SkeletonName.Length <= 160, "Observed exact Small skeleton is required.");
            Require(Gender == Gender.Male || Gender == Gender.Female, "Unknown native gender.");
        }
        private static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); }
    }

    internal sealed class Chunk7OrdinaryBuildDriver
    {
        private const string RangerClassGuid = "cda0615668a6df14eb36ba19ee881af6";
        private const string HuntersBondGuid = "b705c5184a96a84428eeb35ae2517a14";
        private const string RangerCompanionGuid = "ee63330662126374e8785cc901941ac7";
        private const int ChoiceLimit = 512;
        private const int CommitLimit = 20;
        private static readonly StatType[] Attributes = { StatType.Dexterity, StatType.Constitution, StatType.Wisdom,
            StatType.Strength, StatType.Intelligence, StatType.Charisma };
        private readonly Chunk7OrdinaryBuildPins pins;
        private readonly BlueprintFeature horseFeature;
        private readonly string horseUnitGuid;
        private readonly Action assertExactIsolatedAuthority;
        private readonly Action<JObject> publishObservation;
        private readonly Stopwatch elapsed = Stopwatch.StartNew();
        private CharacterBuildController builder;
        private LevelUpController observedController;
        private UnitEntityData actor;
        private UnitDescriptor descriptor;
        private BlueprintCharacterClass ranger;
        private BlueprintRace race;
        private BlueprintRaceVisualPreset preset;
        private HashSet<string> originalActorIds;
        private bool started, complete, awaitingCommit, commitRecorded;
        private int lastFrame = -1, choices, commits, levelBeforeCommit, expectedFinalLevel, initialXp;
        private long moneyBefore, hireCost;
        private JObject commitBefore;
        private string actorId;
        private bool presetSelected, handednessSelected;
        private double commitDeadline;

        internal Chunk7OrdinaryBuildDriver(Chunk7OrdinaryBuildPins pins, UnitEntityData existingActor,
            BlueprintFeature horseFeature, string horseUnitGuid, Action assertExactIsolatedAuthority,
            Action<JObject> publishObservation)
        {
            this.pins = pins ?? throw new ArgumentNullException(nameof(pins));
            pins.Validate();
            this.horseFeature = horseFeature ?? throw new ArgumentNullException(nameof(horseFeature));
            Require(Regex.IsMatch(horseUnitGuid ?? "", "^[a-f0-9]{32}$"), "Exact native Horse blueprint identity is missing.");
            this.horseUnitGuid = horseUnitGuid;
            this.assertExactIsolatedAuthority = assertExactIsolatedAuthority ?? throw new ArgumentNullException(nameof(assertExactIsolatedAuthority));
            this.publishObservation = publishObservation ?? throw new ArgumentNullException(nameof(publishObservation));
            Require((pins.Mode == Chunk7OrdinaryBuildMode.ExistingRangerLevels) == (existingActor != null), "Existing actor binding differs from the selected mode.");
            actor = existingActor;
        }

        internal UnitEntityData Actor => actor;
        internal bool Complete => complete;
        internal int AcceptedChoiceCount => choices;
        internal int NativeCommitCount => commits;
        internal JObject CaptureState() => Snapshot();
        internal bool TryGetOwnedBuildSession(out CharacterBuildController owned)
        {
            owned = null;
            if (builder?.LevelUpController == null) return false;
            var current = builder.LevelUpController.Unit?.Unit;
            var exact = actor != null ? ReferenceEquals(current, actor) : pins.Mode == Chunk7OrdinaryBuildMode.NewSmallCustomCompanion &&
                started && current != null && originalActorIds != null && !originalActorIds.Contains(current.UniqueId) &&
                current.Blueprint == BlueprintRoot.Instance.CustomCompanion && current.Descriptor.Progression.CharacterLevel == 0 &&
                current.Descriptor.Progression.Experience == initialXp;
            Require(exact && ReferenceEquals(Game.Instance.UI.CharacterBuildController, builder) &&
                ReferenceEquals(builder.Unit, builder.LevelUpController.Unit), "A foreign native builder cannot be cancelled by this driver.");
            owned = builder; return true;
        }

        internal Chunk7OrdinaryBuildStep Tick()
        {
            // The callback must inspect the real active native save root, exact
            // scenario/run scope, allowed autosave leaf and absence of fatal
            // authorization or unexpected UI state; a string path alone is not
            // authority. Invoke even while waiting and after native completion.
            assertExactIsolatedAuthority();
            Require(elapsed.Elapsed.TotalSeconds <= 600, "Ordinary build exceeded its selected ten-minute bound.");
            Require(choices <= ChoiceLimit && commits <= CommitLimit, "Ordinary build exceeded bounded choices/commits.");
            if (lastFrame == UnityEngine.Time.frameCount) return Chunk7OrdinaryBuildStep.Waiting;
            lastFrame = UnityEngine.Time.frameCount;
            if (complete) return Chunk7OrdinaryBuildStep.Complete;
            if (!started) { Start(); return Chunk7OrdinaryBuildStep.Advanced; }
            Require(Game.Instance?.UI?.CharacterBuildController == builder && Game.Instance.Player != null &&
                !Game.Instance.Player.IsInCombat, "Exact native builder or idle campaign changed.");
            if (awaitingCommit) return ObserveCommit();
            var controller = builder.LevelUpController;
            Require(controller != null, "Native builder closed before the selected progression completed.");
            if (actor == null) BindNewActor(controller);
            Require(ReferenceEquals(controller.Unit, descriptor) && ReferenceEquals(builder.Unit, descriptor) &&
                ReferenceEquals(descriptor.Unit, actor) && actor.UniqueId == actorId, "Native level controller changed its captured actor.");
            Require(!controller.AutoCommit && !controller.IsAutoLevelup, "An automatic level plan replaced the declared ordinary manual choices.");
            Require(LevelUpController.CanLevelUp(descriptor), "Native actual XP/life/combat gate does not permit this level.");
            Require(descriptor.Progression.Experience == initialXp, "Native actor XP changed outside the declared ordinary progression.");
            if (!ReferenceEquals(observedController, controller))
            {
                observedController = controller;
                Require(controller.State.NextLevel == descriptor.Progression.CharacterLevel + 1,
                    "Native pending level does not follow the actual actor level.");
                Emit("native-level-open", Snapshot());
            }
            return SelectNextChoice(controller);
        }

        private void Start()
        {
            Require(Game.Instance?.Player != null && Game.Instance.UI?.CharacterBuildController != null &&
                !Game.Instance.Player.IsInCombat, "Native campaign/builder is unavailable or combat is active.");
            builder = Game.Instance.UI.CharacterBuildController;
            Require(!builder.HideInterface && builder.LevelUpController == null && builder.Unit == null,
                "A pre-existing native build session is active or its ordinary UI is unavailable.");
            ranger = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(RangerClassGuid);
            Require(ranger != null, "Installed stock Ranger class is unavailable.");
            originalActorIds = new HashSet<string>(Game.Instance.Player.PartyCharacters.Concat(Game.Instance.Player.RemoteCompanions)
                .Select(reference => reference.Value).Where(unit => unit != null).Select(unit => unit.UniqueId), StringComparer.Ordinal);
            moneyBefore = Game.Instance.Player.Money;
            if (pins.Mode == Chunk7OrdinaryBuildMode.NewSmallCustomCompanion)
            {
                race = BlueprintRoot.Instance.Progression.CharacterRaces.SingleOrDefault(value => value?.AssetGuid == pins.SmallRaceGuid);
                Require(race != null && race.Size == Size.Small, "Pinned race is not an installed native Small character race.");
                preset = (race.Presets ?? new BlueprintRaceVisualPreset[0]).SingleOrDefault(value => value?.AssetGuid == pins.SmallPresetGuid);
                Require(preset != null && preset.RaceId == race.RaceId, "Pinned native preset does not belong to the declared Small race.");
                var skeleton = pins.Gender == Gender.Male ? preset.MaleSkeleton : preset.FemaleSkeleton;
                Require(skeleton != null && skeleton.name == pins.SkeletonName, "Pinned sex/preset skeleton differs from native inventory.");
                var main = Game.Instance.Player.MainCharacter.Value;
                Require(main != null && main.Descriptor.Progression.CharacterLevel >= pins.MinimumRangerLevel,
                    "Native main-character level cannot supply the requested ordinary hire progression.");
                initialXp = BlueprintRoot.Instance.Progression.XPTable.GetBonus(main.Descriptor.Progression.CharacterLevel);
                hireCost = Game.Instance.Player.GetCustomCompanionCost();
                Require(hireCost > 0 && moneyBefore >= hireCost, "Actual native money cannot pay the ordinary nonzero hire cost.");
                expectedFinalLevel = ReadyLevel(initialXp);
                Require(expectedFinalLevel >= pins.MinimumRangerLevel,
                    "The exact native default hire XP cannot reach the selected ordinary Ranger acquisition level.");
                Emit("native-paid-hire-admission", new JObject { ["moneyBefore"] = moneyBefore, ["nativeCost"] = hireCost,
                    ["nativeInheritedXp"] = initialXp, ["nativeExpectedFinalLevel"] = expectedFinalLevel,
                    ["nativeInventorySha256"] = pins.NativeInventorySha256 });
                started = true;
                assertExactIsolatedAuthority();
                new CreateCustomCompanion { ForFree = false, MatchPlayerXpExactly = false, ImportableInDungeon = false }.RunAction();
            }
            else
            {
                Require(originalActorIds.Contains(actor.UniqueId) && actor.IsInState && actor.IsInGame && actor.Descriptor.Pet == null &&
                    actor.Descriptor.Progression.CharacterLevel > 0,
                    "Existing acquisition actor is absent, out of the native field or already owns a pet.");
                descriptor = actor.Descriptor; actorId = actor.UniqueId; initialXp = descriptor.Progression.Experience;
                expectedFinalLevel = ReadyLevel(initialXp);
                Require(LevelUpController.CanLevelUp(descriptor) && descriptor.Progression.GetClassLevel(ranger) +
                    expectedFinalLevel - descriptor.Progression.CharacterLevel >= pins.MinimumRangerLevel,
                    "Actual native XP cannot reach the selected ordinary Ranger acquisition level.");
                started = true;
                Emit("native-existing-level-admission", Snapshot());
                assertExactIsolatedAuthority();
                builder.HandleLevelUpStart(descriptor);
            }
        }

        private void BindNewActor(LevelUpController controller)
        {
            descriptor = controller.Unit; actor = descriptor?.Unit; actorId = actor?.UniqueId;
            Require(actor != null && !string.IsNullOrEmpty(actorId) && !originalActorIds.Contains(actorId) &&
                actor.Blueprint == BlueprintRoot.Instance.CustomCompanion && descriptor.Progression.CharacterLevel == 0 &&
                descriptor.Progression.Experience == initialXp && controller.State.Mode == LevelUpState.CharBuildMode.CharGen,
                "Paid native hire did not open one exact new custom-companion CharGen unit with native inherited XP.");
            Require(Game.Instance.Player.Money == moneyBefore, "Native hire money changed before its success callback.");
            Emit("native-new-actor-bound", Snapshot());
        }

        private Chunk7OrdinaryBuildStep SelectNextChoice(LevelUpController controller)
        {
            var state = controller.State;
            Require(state.Selections.Count <= 32, "Native pending feature states exceed the declared bound.");
            if (state.CanSelectPortrait)
            {
                var portrait = BlueprintRoot.Instance.CharGen.Portraits.Where(value => value != null).OrderBy(value => value.AssetGuid, StringComparer.Ordinal).FirstOrDefault();
                Require(portrait != null, "No native stock portrait is available.");
                return Choose("portrait", portrait.AssetGuid, () => builder.SetPortrait(portrait), () => !controller.State.CanSelectPortrait);
            }
            if (state.CanSelectRace)
            {
                Require(pins.Mode == Chunk7OrdinaryBuildMode.NewSmallCustomCompanion && race != null, "Unexpected unbound native race choice.");
                return Choose("race", race.AssetGuid, () => builder.SetRace(race), () => !controller.State.CanSelectRace && controller.Preview.Progression.Race == race);
            }
            if (state.CanSelectGender)
                return Choose("gender", pins.Gender.ToString(), () => builder.SetGender(pins.Gender), () => !controller.State.CanSelectGender && controller.Doll?.Gender == pins.Gender);
            if (state.Mode == LevelUpState.CharBuildMode.CharGen && !presetSelected)
            {
                Require(controller.Doll != null && controller.Doll.Race == race, "Native doll does not carry the selected race.");
                return Choose("native-race-preset", preset.AssetGuid, () => controller.Doll.SetRacePreset(preset), () =>
                { presetSelected = controller.Doll.RacePreset == preset && controller.Doll.GetSkeleton()?.name == pins.SkeletonName; return presetSelected; });
            }
            if (state.Mode == LevelUpState.CharBuildMode.CharGen && !handednessSelected)
                return Choose("native-handedness", pins.LeftHanded.ToString(), () => controller.Doll.SetLeftHanded(pins.LeftHanded), () =>
                { handednessSelected = controller.Doll.LeftHanded == pins.LeftHanded; return handednessSelected; });
            if (state.SelectedClass == null)
                return Choose("class", ranger.AssetGuid, () => builder.SetClass(ranger), () => controller.State.SelectedClass == ranger);
            Require(state.SelectedClass == ranger, "Native selected class differs from the declared Ranger progression.");
            if (!state.StatsDistribution.IsComplete())
            {
                var candidates = Attributes.Where(candidate => state.StatsDistribution.CanAdd(candidate)).ToArray();
                Require(candidates.Length != 0, "Native stat distribution is incomplete with no legal declared purchase.");
                var attribute = candidates[0]; var points = state.StatsDistribution.Points;
                var value = state.StatsDistribution.StatValues[attribute]; var cost = state.StatsDistribution.GetAddCost(attribute);
                return Choose("attribute-purchase", attribute.ToString(), () => builder.BuyAttribute(attribute, true), () =>
                    controller.State.StatsDistribution.Points == points - cost && controller.State.StatsDistribution.StatValues[attribute] == value + 1);
            }
            if (state.CanSelectRaceStat)
                return Choose("racial-attribute", StatType.Dexterity.ToString(), () => builder.SetRacialBonus(StatType.Dexterity), () =>
                    !controller.State.CanSelectRaceStat && controller.State.SelectedRaceStat == StatType.Dexterity);
            if (state.AttributePoints > 0)
            {
                var points = state.AttributePoints; var value = controller.Preview.Stats.GetStat(StatType.Dexterity).BaseValue;
                return Choose("level-attribute", StatType.Dexterity.ToString(), () => builder.SpendAttributePoint(StatType.Dexterity, true), () =>
                    controller.State.AttributePoints == points - 1 && controller.Preview.Stats.GetStat(StatType.Dexterity).BaseValue == value + 1);
            }
            var pending = state.Selections.FirstOrDefault(selection => !selection.Selected && selection.CanSelectAnything(state, controller.Preview));
            if (pending != null) return ChooseFeature(controller, pending);
            if (!state.IsSkillPointsComplete())
            {
                var candidates = StatTypeHelper.Skills.Where(candidate => controller.Preview.Stats.GetStat(candidate).BaseValue < state.NextLevel).ToArray();
                Require(state.SkillPointsRemaining > 0 && candidates.Length != 0, "Native skills are incomplete with no legal remaining point.");
                var skill = candidates[0]; var spent = state.SpentSkillPoints; var value = controller.Preview.Stats.GetStat(skill).BaseValue;
                return Choose("skill", skill.ToString(), () => builder.SpendSkillPoint(skill, true), () =>
                    controller.State.SpentSkillPoints == spent + 1 && controller.Preview.Stats.GetStat(skill).BaseValue == value + 1);
            }
            Require(!state.SpellSelections.Any(selection => selection.CanSelectAnything(controller.Preview)),
                "Required native spell choices need an exact observed UI spell-selection plan; no choices were skipped.");
            if (state.CanSelectName)
            {
                var name = "KMC_C7_" + pins.RunId.Substring(0, Math.Min(32, pins.RunId.Length));
                return Choose("name", name, () => builder.SetName(name), () => !controller.State.CanSelectName);
            }
            if (state.CanSelectAlignment)
            {
                var legal = Enum.GetValues(typeof(Alignment)).Cast<Alignment>().Where(value => !state.IsAlignmentRestricted(value))
                    .OrderBy(value => value.ToString(), StringComparer.Ordinal).ToArray();
                Require(legal.Length != 0, "No legal native alignment remains."); var alignment = legal[0];
                return Choose("alignment", alignment.ToString(), () => builder.SelectAlignment(alignment), () => !controller.State.CanSelectAlignment);
            }
            if (state.CanSelectVoice)
            {
                var voices = pins.Gender == Gender.Male ? BlueprintRoot.Instance.CharGen.MaleVoices : BlueprintRoot.Instance.CharGen.FemaleVoices;
                var voice = voices.Where(value => value != null).OrderBy(value => value.AssetGuid, StringComparer.Ordinal).FirstOrDefault();
                Require(voice != null, "No legal native voice is available.");
                return Choose("voice", voice.AssetGuid, () => builder.SetVoice(voice), () => !controller.State.CanSelectVoice);
            }
            Require(state.IsComplete() && LevelUpController.CanLevelUp(descriptor), "Native completion/readiness is still false after all observed choices.");
            if (builder.CurrentPhase != CharBPhase.Type.Total)
                return Choose("native-total-phase", "Total", () => builder.Next(), () => builder.CurrentPhase == CharBPhase.Type.Total && controller.State.IsComplete());
            Require(commits < CommitLimit, "Native level commit bound exhausted.");
            ValidateSmallPreview(controller);
            levelBeforeCommit = descriptor.Progression.CharacterLevel; commitBefore = Snapshot(); awaitingCommit = true;
            commitRecorded = false; commitDeadline = elapsed.Elapsed.TotalSeconds + 30;
            Emit("native-total-next-requested", commitBefore);
            assertExactIsolatedAuthority();
            builder.Next(); // Native IsComplete + Total gate; never call Commit directly.
            return Chunk7OrdinaryBuildStep.Advanced;
        }

        private Chunk7OrdinaryBuildStep ChooseFeature(LevelUpController controller, FeatureSelectionState selection)
        {
            var items = selection.Selection.Items.ToArray();
            Require(items.Length <= 256, "Native feature choice list exceeds the declared bound.");
            var selectionId = (selection.Selection as BlueprintScriptableObject)?.AssetGuid;
            string required = selectionId == HuntersBondGuid ? RangerCompanionGuid : selectionId == RangerCompanionGuid ? horseFeature.AssetGuid : null;
            var legal = items.Where(item => item?.Feature != null &&
                (required == null || item.Feature.AssetGuid == required) &&
                (item.Feature.GetComponent<AddPet>() == null || ReferenceEquals(item.Feature, horseFeature)) &&
                selection.Selection.CanSelect(controller.Preview, controller.State, selection, item))
                .OrderBy(item => item.Feature.AssetGuid, StringComparer.Ordinal).ThenBy(item => item.Param.ToString(), StringComparer.Ordinal).ToArray();
            Require(legal.Length != 0, "Native feature choice has no legal admitted option: " + selectionId + "; required=" + required);
            var chosen = legal[0]; var index = selection.Index; var level = selection.Level; var selectionBlueprint = selection.Selection;
            return Choose("feature", chosen.Feature.AssetGuid + "/" + chosen.Param, () => builder.SetFeature(selection, chosen), () =>
            {
                var current = controller.State.Selections.SingleOrDefault(value => ReferenceEquals(value.Selection, selectionBlueprint) && value.Index == index && value.Level == level);
                return current?.SelectedItem != null && ReferenceEquals(current.SelectedItem.Feature, chosen.Feature) && current.SelectedItem.Param.Equals(chosen.Param);
            }, selectionId);
        }

        private Chunk7OrdinaryBuildStep Choose(string kind, string choice, Action apply, Func<bool> accepted, string selectionGuid = null)
        {
            Require(choices < ChoiceLimit, "Native choice bound exhausted.");
            assertExactIsolatedAuthority();
            var before = Snapshot();
            Emit("native-choice-requested", new JObject { ["kind"] = kind, ["choice"] = choice, ["nativeSelectionGuid"] = selectionGuid, ["before"] = before.DeepClone() });
            apply();
            var row = new JObject { ["kind"] = kind, ["choice"] = choice, ["nativeSelectionGuid"] = selectionGuid, ["before"] = before, ["after"] = Snapshot() };
            var valid = accepted(); row["nativeChoiceAccepted"] = valid; Emit("native-choice", row);
            Require(valid, "Native UI did not accept the declared " + kind + " choice.");
            choices++;
            return Chunk7OrdinaryBuildStep.Advanced;
        }

        private Chunk7OrdinaryBuildStep ObserveCommit()
        {
            Require(actor != null && ReferenceEquals(actor.Descriptor, descriptor) && actor.UniqueId == actorId,
                "Native commit replaced the captured actor outside the selected mode.");
            Require(elapsed.Elapsed.TotalSeconds <= commitDeadline, "Native ordinary completion/UI/pet settlement exceeded thirty seconds.");
            if (descriptor.Progression.CharacterLevel == levelBeforeCommit) return Chunk7OrdinaryBuildStep.Waiting;
            Require(descriptor.Progression.CharacterLevel == levelBeforeCommit + 1 && descriptor.Progression.Experience == initialXp,
                "Native completion did not advance exactly one actual level with unchanged XP.");
            var expectedMoney = moneyBefore - (pins.Mode == Chunk7OrdinaryBuildMode.NewSmallCustomCompanion ? hireCost : 0);
            Require(Game.Instance.Player.Money == expectedMoney, "Native ordinary acquisition did not preserve exact once-only hire expenditure.");
            if (!commitRecorded)
            {
                commits++; commitRecorded = true;
                Emit("native-level-committed", new JObject { ["before"] = commitBefore, ["after"] = Snapshot(),
                    ["moneyExpected"] = expectedMoney, ["commitCount"] = commits });
            }
            if (ReferenceEquals(builder.LevelUpController, observedController)) return Chunk7OrdinaryBuildStep.Waiting;
            if (builder.LevelUpController != null)
            {
                Require(builder.LevelUpController.State.NextLevel == descriptor.Progression.CharacterLevel + 1,
                    "Native chained builder has the wrong next level.");
                awaitingCommit = false;
                return Chunk7OrdinaryBuildStep.Advanced;
            }
            if (LevelUpController.CanLevelUp(descriptor)) return Chunk7OrdinaryBuildStep.Waiting;
            Require(!LevelUpController.CanLevelUp(descriptor) && descriptor.Progression.CharacterLevel == expectedFinalLevel &&
                descriptor.Progression.GetClassLevel(ranger) >= pins.MinimumRangerLevel, "Native builder ended before the declared naturally ready progression.");
            var horse = descriptor.Pet;
            if (horse == null) return Chunk7OrdinaryBuildStep.Waiting;
            var fact = descriptor.GetFact(horseFeature) as Feature;
            Require(fact != null && fact.Source != null && horse != null && horse.Blueprint?.AssetGuid == horseUnitGuid &&
                ReferenceEquals(horse.Descriptor.Master.Value, actor), "Ordinary native progression did not produce the sourced reciprocal Horse companion.");
            if (pins.Mode == Chunk7OrdinaryBuildMode.NewSmallCustomCompanion)
                Require(descriptor.Progression.Race == race && descriptor.OriginalSize == Size.Small && descriptor.Doll != null &&
                    descriptor.Doll.RacePreset == preset && descriptor.Doll.Gender == pins.Gender && descriptor.Doll.LeftHanded == pins.LeftHanded,
                    "Committed native Small body differs from its exact admitted choices.");
            complete = true;
            Emit("native-builder-complete", new JObject { ["actor"] = Snapshot(), ["ordinaryChoicesComplete"] = true,
                ["nativeFieldAdmissionProven"] = false, ["nativeSaveSettlementProven"] = false, ["poseQualified"] = false });
            return Chunk7OrdinaryBuildStep.Complete;
        }

        private void ValidateSmallPreview(LevelUpController controller)
        {
            if (pins.Mode != Chunk7OrdinaryBuildMode.NewSmallCustomCompanion || controller.State.Mode != LevelUpState.CharBuildMode.CharGen) return;
            Require(presetSelected && handednessSelected && controller.Doll != null && controller.Doll.Race == race &&
                controller.Doll.RacePreset == preset && controller.Doll.Gender == pins.Gender && controller.Doll.LeftHanded == pins.LeftHanded &&
                controller.Doll.GetSkeleton()?.name == pins.SkeletonName && controller.Preview.OriginalSize == Size.Small,
                "Native complete preview does not retain the exact Small body pins.");
        }

        private JObject Snapshot()
        {
            var controller = builder?.LevelUpController; var state = controller?.State;
            return new JObject { ["actorId"] = actor?.UniqueId, ["actorObject"] = actor == null ? 0 : RuntimeHelpers.GetHashCode(actor),
                ["descriptorObject"] = descriptor == null ? 0 : RuntimeHelpers.GetHashCode(descriptor),
                ["nativeControllerObject"] = controller == null ? 0 : RuntimeHelpers.GetHashCode(controller),
                ["level"] = descriptor?.Progression.CharacterLevel, ["xp"] = descriptor?.Progression.Experience,
                ["rangerLevel"] = descriptor == null || ranger == null ? 0 : descriptor.Progression.GetClassLevel(ranger),
                ["nativeCanLevelUp"] = descriptor != null && LevelUpController.CanLevelUp(descriptor),
                ["nativeMode"] = state?.Mode.ToString(), ["phase"] = builder?.CurrentPhase?.ToString(),
                ["nativeIsComplete"] = state != null && state.IsComplete(), ["nextLevel"] = state?.NextLevel,
                ["selectedClass"] = state?.SelectedClass?.AssetGuid, ["attributePoints"] = state?.AttributePoints,
                ["distributionPoints"] = state?.StatsDistribution.Points, ["skillTotal"] = state?.TotalSkillPoints,
                ["skillSpent"] = state?.SpentSkillPoints, ["skillRemaining"] = state?.SkillPointsRemaining,
                ["pendingFeatures"] = state?.RemainingSelections(),
                ["pendingSpellGroups"] = state == null ? 0 : state.SpellSelections.Count(value => value.CanSelectAnything(controller.Preview)),
                ["money"] = Game.Instance?.Player?.Money,
                ["petId"] = descriptor?.Pet?.UniqueId, ["petBlueprint"] = descriptor?.Pet?.Blueprint?.AssetGuid };
        }

        private void Emit(string kind, JObject observation)
        {
            observation["event"] = kind; observation["runId"] = pins.RunId;
            observation["frame"] = UnityEngine.Time.frameCount; observation["elapsedSeconds"] = elapsed.Elapsed.TotalSeconds;
            publishObservation(observation);
        }
        private static int ReadyLevel(int xp)
        {
            var level = 0;
            while (level < 20 && BlueprintRoot.Instance.Progression.XPTable.GetBonus(level + 1) <= xp) level++;
            return level;
        }
        private static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); }
    }
}
