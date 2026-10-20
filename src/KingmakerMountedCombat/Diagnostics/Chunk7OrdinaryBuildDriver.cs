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
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // The owning scenario establishes and continuously asserts exact isolated
    // load/write authority (including native autosave) before calling Tick.
    // This helper drives the installed UI controller, not a replacement level
    // controller. It cannot authorize a live transaction or qualify a profile.
    internal enum Chunk7OrdinaryBuildMode { NewSmallCustomCompanion, ExistingRangerLevels, ExistingHorseLevels, NativeRespecHorse, NativeRespecMammoth, NewMediumCustomCompanion }
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
        internal readonly Size ExpectedBodySize;
        internal readonly bool RequireStockMammothRankSeven;
        internal bool IsRespec => Mode == Chunk7OrdinaryBuildMode.NativeRespecHorse || Mode == Chunk7OrdinaryBuildMode.NativeRespecMammoth;

        internal bool NewPaidCompanion => Mode == Chunk7OrdinaryBuildMode.NewSmallCustomCompanion || Mode == Chunk7OrdinaryBuildMode.NewMediumCustomCompanion;

        internal Chunk7OrdinaryBuildPins(string runId, string nativeInventorySha256, Chunk7OrdinaryBuildMode mode,
            string smallRaceGuid, string smallPresetGuid, string skeletonName, Gender gender, bool leftHanded,
            int minimumRangerLevel = 4, Size expectedBodySize = Size.Small, bool allowNativeAutoLevelupOverride = false,
            bool requireStockMammothRankSeven = false)
        {
            RunId = runId; NativeInventorySha256 = nativeInventorySha256; Mode = mode;
            SmallRaceGuid = smallRaceGuid; SmallPresetGuid = smallPresetGuid; SkeletonName = skeletonName;
            Gender = gender; LeftHanded = leftHanded; MinimumRangerLevel = minimumRangerLevel;
            ExpectedBodySize = expectedBodySize; AllowNativeAutoLevelupOverride = allowNativeAutoLevelupOverride;
            RequireStockMammothRankSeven = requireStockMammothRankSeven;
            Validate();
        }

        // Opt-in for an existing actor whose stock level plan the native controller
        // applies under the difficulty auto-levelup setting: the driver then takes the
        // native UI's own player override (choosing another class on an auto-levelup
        // controller -> CharacterBuildController.SetClass). Default false keeps the refusal.
        internal readonly bool AllowNativeAutoLevelupOverride;

        internal void Validate()
        {
            Require(Regex.IsMatch(RunId ?? "", "^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$"), "Invalid run identity.");
            Require(!AllowNativeAutoLevelupOverride || Mode == Chunk7OrdinaryBuildMode.ExistingRangerLevels,
                "Native auto-levelup override is limited to existing-actor Ranger levels.");
            Require(!RequireStockMammothRankSeven || Mode == Chunk7OrdinaryBuildMode.ExistingRangerLevels &&
                MinimumRangerLevel == 10 && ExpectedBodySize == Size.Medium,
                "The stock Mammoth rank-seven fixture requires an existing Medium Ranger reaching exactly level ten.");
            Require(Regex.IsMatch(NativeInventorySha256 ?? "", "^[a-f0-9]{64}$"), "Exact native inventory hash is required.");
            Require(NewPaidCompanion || Mode == Chunk7OrdinaryBuildMode.ExistingRangerLevels ||
                Mode == Chunk7OrdinaryBuildMode.ExistingHorseLevels || IsRespec,
                "Unknown ordinary build mode.");
            Require(MinimumRangerLevel >= 4 && MinimumRangerLevel <= 20, "Unsupported Ranger target.");
            if (!NewPaidCompanion && !IsRespec) return;
            Require(Regex.IsMatch(SmallRaceGuid ?? "", "^[a-f0-9]{32}$") &&
                Regex.IsMatch(SmallPresetGuid ?? "", "^[a-f0-9]{32}$"), "Observed exact native race and preset IDs are required.");
            Require(!string.IsNullOrWhiteSpace(SkeletonName) && SkeletonName.Length <= 160, "Observed exact native skeleton is required.");
            Require(Gender == Gender.Male || Gender == Gender.Female, "Unknown native gender.");
            Require(Mode == Chunk7OrdinaryBuildMode.NewSmallCustomCompanion ? ExpectedBodySize == Size.Small :
                Mode == Chunk7OrdinaryBuildMode.NewMediumCustomCompanion ? ExpectedBodySize == Size.Medium :
                ExpectedBodySize == Size.Small || ExpectedBodySize == Size.Medium, "Native body size differs from the declared bounded build mode.");
        }
        private static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); }
    }

    internal static class Chunk7StockMammothBuildAdmission
    {
        internal static void RequireExactXp(int actualXp, int rangerTenThreshold, int naturallyReadyLevel)
        {
            if (rangerTenThreshold != 105000 || actualXp != rangerTenThreshold || naturallyReadyLevel != 10)
                throw new InvalidOperationException("Stock Mammoth fixture requires the installed Ranger-ten threshold and no additional ready level.");
        }
    }

    internal sealed class Chunk7OrdinaryBuildDriver
    {
        internal const string RangerClassGuid = "cda0615668a6df14eb36ba19ee881af6";
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
        private readonly NativeProgressionRespecProbe nativeRespec;
        private readonly Stopwatch elapsed = Stopwatch.StartNew();
        private CharacterBuildController builder;
        private LevelUpController observedController;
        private UnitEntityData actor;
        private UnitEntityData originalHorse;
        private UnitDescriptor descriptor;
        private BlueprintCharacterClass ranger;
        private BlueprintRace race;
        private BlueprintRaceVisualPreset preset;
        private BlueprintFeature targetCompanionFeature;
        private string targetCompanionUnitGuid;
        private bool respecRebound;
        private HashSet<string> originalActorIds;
        private bool started, complete, awaitingCommit, commitRecorded;
        private int lastFrame = -1, choices, commits, levelBeforeCommit, expectedFinalLevel, initialXp;
        private long moneyBefore, hireCost, nativeStartingGold;
        private bool nativeStartingGoldObserved;
        private bool nativeHireSpendingObserved;
        private JObject commitBefore;
        private string actorId;
        private bool presetSelected, handednessSelected;
        private double commitDeadline;

        internal Chunk7OrdinaryBuildDriver(Chunk7OrdinaryBuildPins pins, UnitEntityData existingActor,
            BlueprintFeature horseFeature, string horseUnitGuid, Action assertExactIsolatedAuthority,
            Action<JObject> publishObservation, NativeProgressionRespecProbe nativeRespec = null)
        {
            this.pins = pins ?? throw new ArgumentNullException(nameof(pins));
            pins.Validate();
            this.horseFeature = horseFeature ?? throw new ArgumentNullException(nameof(horseFeature));
            Require(Regex.IsMatch(horseUnitGuid ?? "", "^[a-f0-9]{32}$"), "Exact native Horse blueprint identity is missing.");
            Require(!pins.RequireStockMammothRankSeven || horseFeature.AssetGuid == HorseCompanionBlueprintService.MammothFeatureGuid &&
                horseUnitGuid == HorseCompanionBlueprintService.MammothUnitGuid,
                "Rank-seven fixture must select the exact installed stock Mammoth feature and unit before native build.");
            this.horseUnitGuid = horseUnitGuid;
            this.assertExactIsolatedAuthority = assertExactIsolatedAuthority ?? throw new ArgumentNullException(nameof(assertExactIsolatedAuthority));
            this.publishObservation = publishObservation ?? throw new ArgumentNullException(nameof(publishObservation));
            this.nativeRespec = nativeRespec;
            Require((!pins.NewPaidCompanion) == (existingActor != null), "Existing actor binding differs from the selected mode.");
            Require(pins.IsRespec == (nativeRespec != null) && (!pins.IsRespec || ReferenceEquals(nativeRespec.Original, existingActor)),
                "Respec choices require the exact independently observed native original actor.");
            actor = existingActor;
        }

        internal UnitEntityData Actor => actor;
        internal UnitEntityData ActiveNativeBuilderActor => builder?.LevelUpController?.Unit?.Unit;
        internal bool Complete => complete;
        internal int AcceptedChoiceCount => choices;
        internal int NativeCommitCount => commits;
        internal long NativeStartingGold
        {
            get
            {
                Require(pins.NewPaidCompanion && nativeStartingGoldObserved && nativeHireSpendingObserved,
                    "Native starting gold is unavailable before the exact paid hire settles.");
                return nativeStartingGold;
            }
        }
        internal JObject CaptureState() => Snapshot();
        internal bool TryGetOwnedBuildSession(out CharacterBuildController owned)
        {
            owned = null;
            if (builder?.LevelUpController == null) return false;
            TryAdoptNativeRespecRebind();
            var current = builder.LevelUpController.Unit?.Unit;
            var exact = actor != null ? ReferenceEquals(current, actor) : pins.NewPaidCompanion &&
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
            RequireNativeHireMoney(false);
            if (awaitingCommit) return ObserveCommit();
            var controller = builder.LevelUpController;
            Require(controller != null, "Native builder closed before the selected progression completed.");
            if (actor == null) BindNewActor(controller);
            Require(ReferenceEquals(controller.Unit, descriptor) && ReferenceEquals(builder.Unit, descriptor) &&
                ReferenceEquals(descriptor.Unit, actor) && actor.UniqueId == actorId, "Native level controller changed its captured actor.");
            if (pins.AllowNativeAutoLevelupOverride) Require(!controller.AutoCommit, "An automatic commit replaced the declared ordinary manual choices.");
            else Require(!controller.AutoCommit && !controller.IsAutoLevelup, "An automatic level plan replaced the declared ordinary manual choices.");
            Require(LevelUpController.CanLevelUp(descriptor), "Native actual XP/life/combat gate does not permit this level.");
            Require(descriptor.Progression.Experience == initialXp, "Native actor XP changed outside the declared ordinary progression.");
            if (!ReferenceEquals(observedController, controller))
            {
                observedController = controller;
                Require(controller.State.NextLevel == descriptor.Progression.CharacterLevel + 1,
                    "Native pending level does not follow the actual actor level.");
                Emit("native-level-open", Snapshot());
            }
            if (pins.AllowNativeAutoLevelupOverride && controller.IsAutoLevelup) return OverrideNativeAutoLevelupPlan(controller);
            return SelectNextChoice(controller);
        }

        internal int AutoLevelupOverrideCount => autoLevelupOverrides;
        private int autoLevelupOverrides;

        // The native UI path a player takes when an auto-levelup plan opened the
        // level: choosing another class (CharBuildSelectorItem -> CancelAutoLevelup
        // confirmation -> CharacterBuildController.SetClass). SelectClass then marks
        // the plan changed, so IsAutoLevelup is false and the commit drops the plans.
        private Chunk7OrdinaryBuildStep OverrideNativeAutoLevelupPlan(LevelUpController controller)
        {
            Require(autoLevelupOverrides < 20, "Native auto-levelup overrides exceeded the declared bound.");
            autoLevelupOverrides++;
            Emit("native-auto-levelup-plan-observed", new JObject { ["plannedClass"] = controller.State.SelectedClass?.AssetGuid,
                ["nextLevel"] = controller.State.NextLevel, ["setting"] = Game.Instance.Player.Difficulty.AutoLevelup.ToString(),
                ["overrideCount"] = autoLevelupOverrides });
            return Choose("class-over-native-auto-levelup-plan", ranger.AssetGuid, () => builder.SetClass(ranger),
                () => controller.State.SelectedClass == ranger && !controller.IsAutoLevelup);
        }

        private void Start()
        {
            Require(Game.Instance?.Player != null && Game.Instance.UI?.CharacterBuildController != null &&
                !Game.Instance.Player.IsInCombat, "Native campaign/builder is unavailable or combat is active.");
            builder = Game.Instance.UI.CharacterBuildController;
            ranger = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(RangerClassGuid);
            Require(ranger != null, "Installed stock Ranger class is unavailable.");
            targetCompanionFeature = horseFeature; targetCompanionUnitGuid = horseUnitGuid;
            if (pins.IsRespec) { StartObservedRespec(); return; }
            Require(!builder.HideInterface && builder.LevelUpController == null && builder.Unit == null,
                "A pre-existing native build session is active or its ordinary UI is unavailable.");
            originalActorIds = new HashSet<string>(Game.Instance.Player.PartyCharacters.Concat(Game.Instance.Player.RemoteCompanions)
                .Select(reference => reference.Value).Where(unit => unit != null).Select(unit => unit.UniqueId), StringComparer.Ordinal);
            moneyBefore = Game.Instance.Player.Money;
            if (pins.NewPaidCompanion)
            {
                race = BlueprintRoot.Instance.Progression.CharacterRaces.SingleOrDefault(value => value?.AssetGuid == pins.SmallRaceGuid);
                Require(race != null && race.Size == pins.ExpectedBodySize, "Pinned race is not the declared installed native character size.");
                preset = (race.Presets ?? new BlueprintRaceVisualPreset[0]).SingleOrDefault(value => value?.AssetGuid == pins.SmallPresetGuid);
                Require(preset != null && preset.RaceId == race.RaceId, "Pinned native preset does not belong to the declared native race.");
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
                    ["spendObservationContract"] = "native-hire-spent-v2",
                    ["nativeInventorySha256"] = pins.NativeInventorySha256 });
                started = true;
                assertExactIsolatedAuthority();
                new CreateCustomCompanion { ForFree = false, MatchPlayerXpExactly = false, ImportableInDungeon = false }.RunAction();
            }
            else
            {
                Require(originalActorIds.Contains(actor.UniqueId) && actor.IsInState && actor.IsInGame &&
                    actor.Descriptor.Progression.CharacterLevel > 0,
                    "Existing progression actor is absent or out of the native field.");
                if (pins.Mode == Chunk7OrdinaryBuildMode.ExistingRangerLevels)
                    Require(actor.Descriptor.Pet == null, "Existing acquisition actor already owns a pet.");
                else
                {
                    originalHorse = actor.Descriptor.Pet;
                    var originalFact = actor.Descriptor.GetFact(horseFeature) as Feature;
                    Require(originalHorse != null && originalHorse.IsInState && originalHorse.IsInGame &&
                        originalHorse.Blueprint?.AssetGuid == horseUnitGuid && ReferenceEquals(originalHorse.Descriptor.Master.Value, actor) &&
                        originalFact != null && originalFact.Source != null &&
                        actor.Descriptor.Progression.GetClassLevel(ranger) >= pins.MinimumRangerLevel,
                        "Existing Horse progression requires the exact sourced reciprocal native Horse and attained Ranger level.");
                }
                descriptor = actor.Descriptor; actorId = actor.UniqueId; initialXp = descriptor.Progression.Experience;
                expectedFinalLevel = ReadyLevel(initialXp);
                if (pins.RequireStockMammothRankSeven)
                    Chunk7StockMammothBuildAdmission.RequireExactXp(initialXp,
                        BlueprintRoot.Instance.Progression.XPTable.GetBonus(10), expectedFinalLevel);
                Require(LevelUpController.CanLevelUp(descriptor) && descriptor.Progression.GetClassLevel(ranger) +
                    expectedFinalLevel - descriptor.Progression.CharacterLevel >= pins.MinimumRangerLevel,
                    "Actual native XP cannot reach the selected ordinary Ranger acquisition level.");
                started = true;
                Emit(pins.Mode == Chunk7OrdinaryBuildMode.ExistingHorseLevels ? "native-existing-horse-level-admission" :
                    "native-existing-level-admission", Snapshot());
                assertExactIsolatedAuthority();
                builder.HandleLevelUpStart(descriptor);
            }
        }

        private void StartObservedRespec()
        {
            Require(!builder.HideInterface, "Native respec builder hides its ordinary choice interface.");
            nativeRespec.RequireExactPreview(builder);
            var original = nativeRespec.Original;
            race = BlueprintRoot.Instance.Progression.CharacterRaces.SingleOrDefault(value => value?.AssetGuid == pins.SmallRaceGuid);
            preset = (race?.Presets ?? new BlueprintRaceVisualPreset[0]).SingleOrDefault(value => value?.AssetGuid == pins.SmallPresetGuid);
            var skeleton = pins.Gender == Gender.Male ? preset?.MaleSkeleton : preset?.FemaleSkeleton;
            Require(original.Blueprint == BlueprintRoot.Instance.CustomCompanion && original.Descriptor.Progression.Race == race &&
                race != null && race.Size == pins.ExpectedBodySize && original.Descriptor.OriginalSize == pins.ExpectedBodySize &&
                original.Descriptor.State.Size == pins.ExpectedBodySize && original.GetActivePolymorph() == null &&
                original.Descriptor.Doll?.RacePreset == preset && original.Descriptor.Doll.Gender == pins.Gender &&
                original.Descriptor.Doll.LeftHanded == pins.LeftHanded && skeleton?.name == pins.SkeletonName,
                "Native respec body pins do not match the observed original custom companion.");
            if (pins.Mode == Chunk7OrdinaryBuildMode.NativeRespecMammoth)
            {
                targetCompanionFeature = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(HorseCompanionBlueprintService.MammothFeatureGuid);
                targetCompanionUnitGuid = HorseCompanionBlueprintService.MammothUnitGuid;
                Require(targetCompanionFeature?.GetComponent<AddPet>()?.Pet?.AssetGuid == targetCompanionUnitGuid,
                    "Installed stock Mammoth companion choice differs.");
            }
            actor = nativeRespec.Preview; descriptor = actor.Descriptor; actorId = actor.UniqueId;
            initialXp = nativeRespec.InitialExperience; expectedFinalLevel = ReadyLevel(initialXp);
            Require(descriptor.Progression.CharacterLevel == 0 && descriptor.Progression.Experience == initialXp &&
                expectedFinalLevel >= pins.MinimumRangerLevel && LevelUpController.CanLevelUp(descriptor),
                "Actual native respec preview XP cannot reach the declared Ranger companion choice.");
            moneyBefore = Game.Instance.Player.Money; started = true;
            Emit("native-respec-build-admission", Snapshot());
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
                Require((pins.NewPaidCompanion || pins.IsRespec) && race != null, "Unexpected unbound native race choice.");
                // The headless fixture has no race-description selector. The UI
                // wrapper selects the native race, then dereferences that selector;
                // use its exact native selection call and verify the result.
                var selected = false;
                return Choose("race", race.AssetGuid, () => selected = controller.SelectRace(race),
                    () => selected && !controller.State.CanSelectRace && controller.Preview.Progression.Race == race);
            }
            if (state.CanSelectGender)
                return Choose("gender", pins.Gender.ToString(), () => builder.SetGender(pins.Gender), () => !controller.State.CanSelectGender && controller.Doll?.Gender == pins.Gender);
            if ((state.Mode == LevelUpState.CharBuildMode.CharGen || state.Mode == LevelUpState.CharBuildMode.Respec) && !presetSelected)
            {
                Require(controller.Doll != null && controller.Doll.Race == race, "Native doll does not carry the selected race.");
                return Choose("native-race-preset", preset.AssetGuid, () => controller.Doll.SetRacePreset(preset), () =>
                { presetSelected = controller.Doll.RacePreset == preset && controller.Doll.GetSkeleton()?.name == pins.SkeletonName; return presetSelected; });
            }
            if ((state.Mode == LevelUpState.CharBuildMode.CharGen || state.Mode == LevelUpState.CharBuildMode.Respec) && !handednessSelected)
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
                var name = pins.IsRespec ? nativeRespec.Original.CharacterName :
                    "KMC_C7_" + pins.RunId.Substring(0, Math.Min(32, pins.RunId.Length));
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
            // The general feat selection offers every feat (base game and DLC), not only legal ones.
            Require(items.Length <= 1024, "Native feature choice list exceeds the declared bound.");
            var selectionId = (selection.Selection as BlueprintScriptableObject)?.AssetGuid;
            string required = selectionId == HuntersBondGuid ? RangerCompanionGuid : selectionId == RangerCompanionGuid ? targetCompanionFeature.AssetGuid : null;
            var legal = items.Where(item => item?.Feature != null &&
                (required == null || item.Feature.AssetGuid == required) &&
                (item.Feature.GetComponent<AddPet>() == null || ReferenceEquals(item.Feature, targetCompanionFeature)) &&
                selection.Selection.CanSelect(controller.Preview, controller.State, selection, item))
                // FeatureParam is a class; ordinary features offer a null Param.
                .OrderBy(item => item.Feature.AssetGuid, StringComparer.Ordinal).ThenBy(item => item.Param == null ? string.Empty : item.Param.ToString(), StringComparer.Ordinal).ToArray();
            Require(legal.Length != 0, "Native feature choice has no legal admitted option: " + selectionId + "; required=" + required);
            var chosen = legal[0]; var index = selection.Index; var level = selection.Level; var selectionBlueprint = selection.Selection;
            return Choose("feature", chosen.Feature.AssetGuid + "/" + chosen.Param, () => builder.SetFeature(selection, chosen), () =>
            {
                var current = controller.State.Selections.SingleOrDefault(value => ReferenceEquals(value.Selection, selectionBlueprint) && value.Index == index && value.Level == level);
                return current?.SelectedItem != null && ReferenceEquals(current.SelectedItem.Feature, chosen.Feature) && object.Equals(current.SelectedItem.Param, chosen.Param);
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
            TryAdoptNativeRespecRebind();
            Require(actor != null && ReferenceEquals(actor.Descriptor, descriptor) && actor.UniqueId == actorId,
                "Native commit replaced the captured actor outside the selected mode.");
            Require(elapsed.Elapsed.TotalSeconds <= commitDeadline, "Native ordinary completion/UI/pet settlement exceeded thirty seconds.");
            if (descriptor.Progression.CharacterLevel == levelBeforeCommit) return Chunk7OrdinaryBuildStep.Waiting;
            Require(descriptor.Progression.CharacterLevel == levelBeforeCommit + 1 && descriptor.Progression.Experience == initialXp,
                "Native completion did not advance exactly one actual level with unchanged XP.");
            var expectedMoney = pins.IsRespec ? nativeRespec.ExpectedMoneyAfter : moneyBefore;
            if (pins.NewPaidCompanion) RequireNativeHireMoney(false);
            else Require(Game.Instance.Player.Money == expectedMoney, "Native ordinary acquisition changed money outside its selected native callback.");
            if (!commitRecorded)
            {
                commits++; commitRecorded = true;
                Emit("native-level-committed", new JObject { ["before"] = commitBefore, ["after"] = Snapshot(),
                    ["moneyExpected"] = Game.Instance.Player.Money, ["commitCount"] = commits });
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
            if (pins.NewPaidCompanion && !nativeHireSpendingObserved) return Chunk7OrdinaryBuildStep.Waiting;
            RequireNativeHireMoney(true);
            Require(!LevelUpController.CanLevelUp(descriptor) && descriptor.Progression.CharacterLevel == expectedFinalLevel &&
                descriptor.Progression.GetClassLevel(ranger) >= pins.MinimumRangerLevel, "Native builder ended before the declared naturally ready progression.");
            var horse = descriptor.Pet;
            if (horse == null) return Chunk7OrdinaryBuildStep.Waiting;
            var fact = descriptor.GetFact(targetCompanionFeature) as Feature;
            Require(fact != null && fact.Source != null && horse != null && horse.Blueprint?.AssetGuid == targetCompanionUnitGuid &&
                ReferenceEquals(horse.Descriptor.Master.Value, actor), "Ordinary native progression did not produce the sourced reciprocal Horse companion.");
            if (pins.RequireStockMammothRankSeven)
            {
                var rank = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(HorseCompanionBlueprintService.LevelRankGuid);
                var upgrade = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(HorseCompanionBlueprintService.MammothUpgradeGuid);
                Require(targetCompanionFeature.AssetGuid == HorseCompanionBlueprintService.MammothFeatureGuid &&
                    targetCompanionUnitGuid == HorseCompanionBlueprintService.MammothUnitGuid && rank != null && upgrade != null &&
                    descriptor.Progression.GetClassLevel(ranger) == 10 && descriptor.Progression.CharacterLevel == 10 &&
                    descriptor.GetFact(rank)?.GetRank() >= 7 && horse.Descriptor.GetFact(upgrade) != null &&
                    descriptor.OriginalSize == Size.Medium && descriptor.State.Size == Size.Medium &&
                    horse.Descriptor.State.Size == Size.Large && actor.GetActivePolymorph() == null && horse.GetActivePolymorph() == null,
                    "Native stock Mammoth did not attain its sourced rank-seven Large upgrade over the exact Medium Ranger.");
                Emit("native-stock-mammoth-rank-seven", new JObject { ["rangerLevel"] = descriptor.Progression.GetClassLevel(ranger),
                    ["companionRank"] = descriptor.GetFact(rank).GetRank(), ["riderSize"] = descriptor.State.Size.ToString(),
                    ["mountSize"] = horse.Descriptor.State.Size.ToString(), ["upgradeGuid"] = upgrade.AssetGuid,
                    ["featureGuid"] = targetCompanionFeature.AssetGuid, ["petGuid"] = horse.Blueprint.AssetGuid });
            }
            Require(pins.Mode != Chunk7OrdinaryBuildMode.ExistingHorseLevels || ReferenceEquals(horse, originalHorse),
                "Ordinary existing-Horse leveling unexpectedly replaced its native pet.");
            if (pins.NewPaidCompanion || pins.IsRespec)
                Require(descriptor.Progression.Race == race && descriptor.OriginalSize == pins.ExpectedBodySize && descriptor.Doll != null &&
                    descriptor.Doll.RacePreset == preset && descriptor.Doll.Gender == pins.Gender && descriptor.Doll.LeftHanded == pins.LeftHanded,
                    "Committed native body differs from its exact admitted choices.");
            complete = true;
            Emit("native-builder-complete", new JObject { ["actor"] = Snapshot(), ["ordinaryChoicesComplete"] = true,
                ["nativeFieldAdmissionProven"] = false, ["nativeSaveSettlementProven"] = false, ["poseQualified"] = false });
            return Chunk7OrdinaryBuildStep.Complete;
        }

        private void RequireNativeHireMoney(bool complete)
        {
            if (!pins.NewPaidCompanion) return;
            // Tick also runs while the native CharGen actor is still level zero.
            // No class data or starting-item grant exists until its first commit.
            if (descriptor == null || descriptor.Progression.CharacterLevel == 0)
            {
                Require(!complete && !nativeStartingGoldObserved && !nativeHireSpendingObserved &&
                    Game.Instance.Player.Money == moneyBefore,
                    "Native paid hire changed money before its first committed class level.");
                return;
            }
            // Installed LevelUpController.Commit calls SetupNewCharacher before
            // the paid success callback. Its LevelUpHelper.AddStartingItems
            // grants the selected class's starting gold once at first level.
            Require(ReferenceEquals(descriptor.Progression.GetMaxClass(), ranger),
                "Native paid hire changed the selected maximum class.");
            Require(!BlueprintRoot.Instance.DlcSettings.IsPlayingDlcGame(DlcType.Endless),
                "The selected paid hire does not admit the Endless campaign's different starting-gold rule.");
            var classData = descriptor.Progression.GetClassData(ranger);
            Require(classData != null, "Native paid hire has no committed Ranger class data.");
            var replacement = classData.Archetypes.FirstOrDefault(value => value.ReplaceStartingEquipment);
            var observedStartingGold = replacement == null ? ranger.StartingGold : replacement.StartingGold;
            Require(observedStartingGold >= 0, "Native paid hire has invalid class starting gold.");
            if (!nativeStartingGoldObserved)
            {
                nativeStartingGold = observedStartingGold;
                nativeStartingGoldObserved = true;
            }
            Require(nativeStartingGold == observedStartingGold,
                "Native paid hire changed its selected starting-gold authority.");
            var money = Game.Instance.Player.Money;
            Require(Game.Instance.Player.GetCustomCompanionCost() == hireCost,
                "Native paid hire cost changed before its success callback.");
            var paid = checked(moneyBefore + nativeStartingGold - hireCost);
            if (money != moneyBefore && money != paid)
                Emit("native-hire-money-mismatch", new JObject { ["moneyBefore"] = moneyBefore,
                    ["quotedCost"] = hireCost, ["moneyNow"] = money,
                    ["observedDebit"] = moneyBefore - money,
                    ["nativeStartingGold"] = nativeStartingGold,
                    ["nativeCostAtObservation"] = Game.Instance.Player.GetCustomCompanionCost(),
                    ["partyLevelAtObservation"] = Game.Instance.Player.PartyLevel,
                    ["actorId"] = actorId, ["actorLevel"] = descriptor?.Progression.CharacterLevel });
            Require(money == moneyBefore || money == paid,
                "Native ordinary acquisition changed money by something other than its exact single hire cost.");
            Require(!nativeHireSpendingObserved || money == paid,
                "Native ordinary acquisition reversed its observed hire expenditure.");
            if (money == paid && !nativeHireSpendingObserved)
            {
                nativeHireSpendingObserved = true;
                Emit("native-hire-spent", new JObject { ["moneyBefore"] = moneyBefore,
                    ["nativeCost"] = hireCost, ["nativeStartingGold"] = nativeStartingGold,
                    ["moneyAfter"] = money });
            }
            if (complete) Require(nativeHireSpendingObserved,
                "Native ordinary acquisition completed without exact once-only hire expenditure.");
        }

        // Native respec replaces the vacuum preview at its first commit, before
        // terminal builder completion. Cleanup may run before the next Tick.
        // Adoption still requires the exact observed native callback and effects.
        private void TryAdoptNativeRespecRebind()
        {
            if (!pins.IsRespec || respecRebound || !nativeRespec.TryRequireNativeRebind(builder)) return;
            actor = nativeRespec.Original; descriptor = actor.Descriptor; respecRebound = true;
            Emit("native-respec-builder-rebound", Snapshot());
        }

        private void ValidateSmallPreview(LevelUpController controller)
        {
            if (!pins.NewPaidCompanion && !pins.IsRespec ||
                controller.State.Mode != LevelUpState.CharBuildMode.CharGen && controller.State.Mode != LevelUpState.CharBuildMode.Respec) return;
            Require(presetSelected && handednessSelected && controller.Doll != null && controller.Doll.Race == race &&
                controller.Doll.RacePreset == preset && controller.Doll.Gender == pins.Gender && controller.Doll.LeftHanded == pins.LeftHanded &&
                controller.Doll.GetSkeleton()?.name == pins.SkeletonName && controller.Preview.OriginalSize == pins.ExpectedBodySize,
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
                ["petId"] = descriptor?.Pet?.UniqueId, ["petBlueprint"] = descriptor?.Pet?.Blueprint?.AssetGuid,
                ["petObject"] = descriptor?.Pet == null ? 0 : RuntimeHelpers.GetHashCode(descriptor.Pet),
                ["petLevel"] = descriptor?.Pet?.Descriptor.Progression.CharacterLevel,
                ["petExperience"] = descriptor?.Pet?.Descriptor.Progression.Experience,
                ["originalHorseRetained"] = originalHorse != null && ReferenceEquals(descriptor?.Pet, originalHorse),
                ["nativeRespecRebound"] = respecRebound };
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
