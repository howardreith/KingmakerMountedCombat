using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Root;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UI.SettingsUI;
using Kingmaker.UI.LevelUp;
using Kingmaker.UI.Selection;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.View;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using KingmakerMountedCombat.Logging;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // One character through the native builder: the same controller and commit the ordinary screens use, with the on-screen
    // clicks replaced by the controller's own calls. Nothing here writes a level, a feature or a pet.
    internal sealed class Sprint8Build
    {
        internal readonly JArray Levels = new JArray();
        internal readonly Dictionary<string, string> Choices = new Dictionary<string, string>(StringComparer.Ordinal);
        internal Func<int, BlueprintCharacterClass> ClassFor;
        internal BlueprintRace Race;
        internal Gender Gender = Gender.Male;
        internal string Name;
        internal string Archetype;
        internal Func<UnitDescriptor, JObject> Snapshot;
        internal Action<LevelUpController, FeatureSelectionState, JObject> OnSelection;
        private readonly IModLogger logger;

        internal Sprint8Build(IModLogger logger) { this.logger = logger; }

        private static CharacterBuildController Builder => Game.Instance.UI.CharacterBuildController;

        internal UnitDescriptor Hired;

        internal void StartHire()
        {
            if (Builder == null || Builder.LevelUpController != null) throw new InvalidOperationException("The native builder is unavailable or busy.");
            new CreateCustomCompanion { ForFree = true, MatchPlayerXpExactly = false, ImportableInDungeon = false }.RunAction();
            Hired = Builder.Unit;
            if (Hired == null || Builder.LevelUpController?.State.Mode != LevelUpState.CharBuildMode.CharGen) throw new InvalidOperationException("The native hire did not open character creation.");
        }

        internal void StartRespec(UnitDescriptor unit)
        {
            if (Builder == null || Builder.LevelUpController != null) throw new InvalidOperationException("The native builder is unavailable or busy.");
            Game.Instance.Player.RespecCompanion(unit.Unit);
            Hired = unit;
            if (Builder.LevelUpController?.State.Mode != LevelUpState.CharBuildMode.Respec) throw new InvalidOperationException("The native respec did not open its builder.");
        }

        // One level of the open native chain. False once the chain has ended (no more experience for a level).
        internal bool CommitBuilderLevel()
        {
            var controller = Builder.LevelUpController;
            if (controller == null || Builder.Unit == null) return false;
            var record = new JObject { ["mode"] = controller.State.Mode.ToString(), ["nextLevel"] = controller.State.NextLevel };
            Fill(controller, record);
            if (!controller.State.IsComplete()) throw new InvalidOperationException("The native level was not complete at " + controller.State.NextLevel + ": " + record);
            Builder.Commit();
            Finish(record);
            return Builder.LevelUpController != null && Builder.Unit != null;
        }

        // One level outside the chain, for a disposable character with more disclosed experience.
        internal JObject CommitOwnLevel(UnitDescriptor unit)
        {
            var original = SettingsRoot.Instance.AutoLevelup.CurrentValue;
            LevelUpController controller = null;
            var committed = false;
            var record = new JObject { ["mode"] = "LevelUp", ["nextLevel"] = unit.Progression.CharacterLevel + 1 };
            try
            {
                SettingsRoot.Instance.AutoLevelup.CurrentValue = AutolevelupState.Off;
                if (!LevelUpController.CanLevelUp(unit)) throw new InvalidOperationException("No native level is due at " + unit.Progression.CharacterLevel + ".");
                controller = LevelUpController.StartWithoutAssigningStaticInstance(unit);
                Fill(controller, record);
                if (!controller.State.IsComplete()) throw new InvalidOperationException("The native level was not complete: " + record);
                controller.Commit();
                committed = true;
            }
            finally
            {
                if (controller != null && !committed) controller.Cancel();
                SettingsRoot.Instance.AutoLevelup.CurrentValue = original;
            }
            Hired = unit;
            Finish(record);
            return record;
        }

        private void Finish(JObject record)
        {
            if (Snapshot != null && Hired != null) record["after"] = Snapshot(Hired);
            Levels.Add(record);
            logger.Info("Sprint 8 native level committed: " + record.Value<int>("nextLevel") + ".");
        }

        private void Fill(LevelUpController controller, JObject record)
        {
            var state = controller.State;
            if (state.CanSelectPortrait)
                Require(controller.SelectPortrait(BlueprintRoot.Instance.CharGen.Portraits.First(value => value != null)), "portrait");
            if (state.CanSelectRace) Require(controller.SelectRace(Race), "race");
            if (state.CanSelectGender) Require(controller.SelectGender(Gender), "gender");
            if (controller.Doll != null && (state.Mode == LevelUpState.CharBuildMode.CharGen || state.Mode == LevelUpState.CharBuildMode.Respec) && Race != null && Race.Presets.Length > 0)
            {
                controller.Doll.SetRacePreset(Race.Presets[0]);
                controller.Doll.SetLeftHanded(false);
            }
            var characterClass = ClassFor(state.NextLevel);
            if (state.SelectedClass == null)
            {
                if (Archetype != null && controller.Preview.Progression.GetClassLevel(characterClass) == 0)
                    record["archetypeAdded"] = controller.AddArchetype(characterClass, characterClass.Archetypes.Single(value => value.AssetGuid == Archetype));
                Require(controller.SelectClass(characterClass), "class " + characterClass.name);
            }
            record["class"] = state.SelectedClass?.name;
            foreach (var stat in new[] { StatType.Strength, StatType.Charisma, StatType.Constitution, StatType.Dexterity, StatType.Wisdom, StatType.Intelligence })
                while (!state.StatsDistribution.IsComplete() && state.StatsDistribution.CanAdd(stat) && controller.AddStatPoint(stat)) { }
            if (state.CanSelectRaceStat) Require(controller.SelectRaceStat(StatType.Strength), "racial stat");
            while (state.AttributePoints > 0) Require(controller.SpendAttributePoint(StatType.Strength), "attribute");
            for (var pass = 0; pass < 32; pass++)
            {
                var pending = state.Selections.FirstOrDefault(selection => !selection.Selected && selection.CanSelectAnything(state, controller.Preview));
                if (pending == null) break;
                Choose(controller, pending, record);
            }
            foreach (var skill in StatTypeHelper.Skills)
                while (state.SkillPointsRemaining > 0 && controller.Preview.Stats.GetStat(skill).BaseValue < state.NextLevel && controller.SpendSkillPoint(skill)) { }
            if (state.CanSelectName) Require(controller.SelectName(Name), "name");
            if (state.CanSelectAlignment)
                Require(controller.SelectAlignment(Enum.GetValues(typeof(Alignment)).Cast<Alignment>().First(value => !state.IsAlignmentRestricted(value))), "alignment");
            if (state.CanSelectVoice)
                Require(controller.SelectVoice((Gender == Gender.Male ? BlueprintRoot.Instance.CharGen.MaleVoices : BlueprintRoot.Instance.CharGen.FemaleVoices).First(value => value != null)), "voice");
        }

        private void Choose(LevelUpController controller, FeatureSelectionState pending, JObject record)
        {
            var selection = (BlueprintScriptableObject)pending.Selection;
            OnSelection?.Invoke(controller, pending, record);
            var items = pending.Selection.Items.ToArray();
            string wanted;
            IFeatureSelectionItem chosen;
            if (Choices.TryGetValue(selection.AssetGuid, out wanted))
                chosen = items.Single(item => item.Feature?.AssetGuid == wanted);
            else
                chosen = items.Where(item => item.Feature != null && item.Feature.GetComponent<AddPet>() == null &&
                    ((BlueprintFeatureSelection)pending.Selection).CanSelect(controller.Preview, controller.State, pending, item))
                    .OrderBy(item => item.Feature.AssetGuid, StringComparer.Ordinal).ThenBy(item => item.Param == null ? string.Empty : item.Param.ToString(), StringComparer.Ordinal).FirstOrDefault();
            if (chosen == null) throw new InvalidOperationException("No legal choice in " + selection.name + ".");
            if (!controller.SelectFeature(pending, chosen)) throw new InvalidOperationException("The native controller refused " + chosen.Feature.name + " in " + selection.name + ".");
            var choices = record["choices"] as JArray ?? new JArray();
            record["choices"] = choices;
            choices.Add(selection.name + "=" + chosen.Feature.name);
        }

        private static void Require(bool accepted, string what)
        {
            if (!accepted) throw new InvalidOperationException("The native builder refused the " + what + ".");
        }

        // The ordinary party member a player would bring along, and the pet that comes with it.
        internal static void BringToField(UnitEntityData unit, Vector3 near)
        {
            var player = Game.Instance.Player;
            if (!player.PartyCharacters.Any(reference => reference.Value == unit)) player.AddCompanion(unit);
            unit.IsInGame = true;
            FreePlaceSelector.PlaceSpawnPlaces(2, unit.View.Corpulence, near);
            unit.Position = FreePlaceSelector.GetRelaxedPosition(0, true);
            var pet = unit.Descriptor.Pet;
            if (pet == null) return;
            pet.IsInGame = true;
            pet.Position = FreePlaceSelector.GetRelaxedPosition(1, true);
        }
    }

    // Observes the additional damage a smite adds while the inner attack roll is being made.
    internal sealed class Sprint8SmiteProbe : IGlobalRulebookHandler<RuleAttackRoll>, IDisposable
    {
        private readonly UnitEntityData attacker;
        internal int LastAdditionalDamage = int.MinValue, LastAttackBonus = int.MinValue;

        internal Sprint8SmiteProbe(UnitEntityData attacker) { this.attacker = attacker; EventBus.Subscribe(this); }

        public void OnEventAboutToTrigger(RuleAttackRoll evt) { }

        public void OnEventDidTrigger(RuleAttackRoll evt)
        {
            if (evt.Initiator != attacker) return;
            LastAdditionalDamage = attacker.Stats.AdditionalDamage.ModifiedValue;
            LastAttackBonus = attacker.Stats.AdditionalAttackBonus.ModifiedValue;
        }

        public void Dispose() => EventBus.Unsubscribe(this);
    }

    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal const string Sprint8Choice = "sprint8-paladin-choice-rt", Sprint8Mount = "sprint8-paladin-mount-rt", Sprint8Dog = "sprint8-paladin-dog-rt", Sprint8Mixed = "sprint8-paladin-mixed-rt";
        internal static bool IsSprint8PaladinScenario(string scenario) => scenario == Sprint8Choice || scenario == Sprint8Mount || scenario == Sprint8Dog || scenario == Sprint8Mixed;
        private bool IsSprint8Paladin => IsSprint8PaladinScenario(request.Scenario);
        internal const int Sprint8SchemaVersion = 52;
        internal const double Sprint8ScenarioDeadlineSeconds = 400.0d, Sprint8HostDeadlineSeconds = 560.0d;
        private string[] Sprint8Cases => request.Scenario == Sprint8Choice ? new[] { "S8-inventory", "S8-weapon", "S8-existing", "S8-retrain" } :
            request.Scenario == Sprint8Mount ? new[] { "S8-mount-horse", "S8-control" } :
            request.Scenario == Sprint8Dog ? new[] { "S8-dog" } : new[] { "S8-mixed", "S8-archetype" };
        private const string Sprint8HumanRace = "0a5d473ead98b0646b94495af250fdc4";

        private int s8CaseIndex;
        private IEnumerator s8Script;
        private JObject s8Facts, s8Measure;
        private UnitEntityData s8Weapon, s8Owner;
        private Sprint8SmiteProbe s8Probe;
        private bool s8SelectionDisabled;

        private void BeginSprint8Paladin()
        {
            if (!settings.EnablePairedActivation || settings.EnableUnifiedMountedTurn ||
                settings.EnablePairedCommandScheduler || settings.EnableDiagnosticOverlay || playerAction.OverlayPresent)
                throw new InvalidOperationException("Sprint 8 rows require the accepted single paired authority.");
            CaptureIdleFixturePartyForCleanup();
            castingTrace = new NativeCastingItemTrace(rider, horse);
            castingCosts = new NativeActorAllocationTrace(rider, horse, combat);
            s3Rules = new Chunk4IncomingRuleObserver(rider, horse);
            s8Measure = new JObject { ["contract"] = "native-paladin-divine-mount-v1", ["rider"] = rider.UniqueId, ["mount"] = horse.UniqueId, ["mountBlueprint"] = horse.Blueprint.AssetGuid,
                ["cases"] = new JArray(Sprint8Cases), ["bondApplied"] = horseService.DivineBondApplied, ["bondFailure"] = horseService.DivineMountFailure,
                ["experienceIsDisclosedSetup"] = true, ["driver"] = "native LevelUpController and builder commit, without the on-screen clicks",
                ["policy"] = "Characters come from the native hire and respec; every feature is a native selection or level entry; every pet level is a native commit." };
            observations["sprint8Paladin"] = s8Measure;
            castingOriginalSlots = new NativeCastingOriginalSlots(rider, castingTrace);
            step = Phase3dHorseStep.Phase3gControls;
            ResetLeafClock();
        }

        private JObject CaptureSprint8DeadlineProgress() => new JObject { ["caseIndex"] = s8CaseIndex, ["case"] = s8CaseIndex < Sprint8Cases.Length ? Sprint8Cases[s8CaseIndex] : null, ["facts"] = s8Facts?.DeepClone() };

        private bool DrainSprint8Fixture()
        {
            if (s8Probe != null) { s8Probe.Dispose(); s8Probe = null; }
            if (s8SelectionDisabled) { horseService.SetSelectionEnabled(true); s8SelectionDisabled = false; }
            var builder = Game.Instance.UI.CharacterBuildController;
            if (builder?.LevelUpController != null && builder.Unit != null) { builder.LevelUpController.Cancel(); builder.Show(false); }
            var second = s8Owner == null ? null : relationship.World.Registry.FindActive(s8Owner)?.Boundary;
            if (second?.State == RelationshipState.Mounted)
            {
                if (s8Owner.Commands.Empty) Sprint5Fixture.Click(nativeControls, s8Owner, nativeControls.DismountAbility, s8Owner);
                return false;
            }
            return DrainSprint4Fixture();
        }

        private void TickSprint8Paladin()
        {
            var game = Game.Instance;
            if (game.IsPaused) { game.IsPaused = false; return; }
            ResetLeafClock();
            if (s8Script == null)
            {
                if (s8CaseIndex >= Sprint8Cases.Length) { BeginCleanup(); return; }
                s8Facts = new JObject { ["case"] = Sprint8Cases[s8CaseIndex] };
                s8Script = Sprint8CaseScript(Sprint8Cases[s8CaseIndex]);
            }
            try
            {
                if (s8Script.MoveNext()) return;
            }
            catch (Exception exception)
            {
                s8Facts["failure"] = exception.GetType().Name + ": " + exception.Message;
                logger.Exception("Sprint 8 case " + Sprint8Cases[s8CaseIndex], exception);
                AddRow(Sprint8Cases[s8CaseIndex], false, exception.Message, s8Facts);
                s8Script = null;
                s8CaseIndex = Sprint8Cases.Length;
                BeginCleanup();
                return;
            }
            AddRow(Sprint8Cases[s8CaseIndex], true, "observed", s8Facts);
            s8Script = null;
            s8CaseIndex++;
        }

        private IEnumerator Sprint8CaseScript(string name)
        {
            switch (name)
            {
                case "S8-inventory": return Sprint8Inventory();
                case "S8-weapon": return Sprint8WeaponCase();
                case "S8-existing": return Sprint8ExistingCase();
                case "S8-retrain": return Sprint8RetrainCase();
                case "S8-mount-horse": return Sprint8MountHorseCase();
                case "S8-control": return Sprint8ControlCase();
                case "S8-dog": return Sprint8DogCase();
                case "S8-mixed": return Sprint8MixedCase();
                case "S8-archetype": return Sprint8ArchetypeCase();
                default: throw new InvalidOperationException("Unknown Sprint 8 case " + name);
            }
        }

        // ---- facts ------------------------------------------------------------------------------------------------------
        private static JArray S8ComponentTypes(BlueprintScriptableObject blueprint) =>
            new JArray((blueprint?.ComponentsArray ?? new BlueprintComponent[0]).Select(component => component?.GetType().Name + (component is AddFeatureOnApply apply ? ":" + apply.Feature?.name : "")));

        private static JObject S8Blueprint(BlueprintFeature feature) => feature == null ? null : new JObject {
            ["name"] = feature.name, ["guid"] = feature.AssetGuid, ["ranks"] = feature.Ranks, ["classFeature"] = feature.IsClassFeature, ["hidden"] = feature.HideInUI,
            ["groups"] = new JArray(feature.Groups.Select(group => group.ToString())), ["components"] = S8ComponentTypes(feature) };

        private JObject Sprint8Snapshot(UnitDescriptor unit)
        {
            var progression = unit.Progression;
            var features = progression.Features;
            var pet = unit.Pet;
            var paladin = horseService.PaladinClass;
            var mountBond = features.HasFact(horseService.DivineMountSelection);
            var facts = new JObject {
                ["id"] = unit.Unit.UniqueId, ["characterLevel"] = progression.CharacterLevel, ["paladinLevel"] = progression.GetClassLevel(paladin), ["size"] = unit.OriginalSize.ToString(),
                ["archetypes"] = new JArray(progression.Classes.SelectMany(data => data.Archetypes).Select(value => value.name)),
                ["classes"] = new JArray(progression.Classes.Select(data => data.CharacterClass.name + ":" + data.Level)),
                ["rank"] = unit.GetFact(horseService.LevelRank)?.GetRank() ?? 0,
                ["weaponBond"] = features.HasFact(horseService.WeaponBondFeature),
                ["weaponUpgradeRanks"] = horseService.WeaponBondUpgrades.OfType<BlueprintFeature>().Sum(value => features.GetRank(value)),
                ["weaponAbilities"] = unit.ActivatableAbilities.Enumerable.Count(value => value.Blueprint.name.StartsWith("WeaponBond", StringComparison.Ordinal)) +
                    unit.Abilities.Enumerable.Count(value => value.Blueprint.name.StartsWith("WeaponBond", StringComparison.Ordinal)),
                ["weaponResources"] = horseService.WeaponBondFeature.GetComponents<AddAbilityResources>().Sum(value => unit.Resources.GetResourceAmount(value.Resource)),
                ["bondSelectionFact"] = features.HasFact(horseService.DivineBondSelection),
                ["mountBond"] = mountBond,
                ["mountChoice"] = features.HasFact(horseService.DivineMountHorse) ? "Horse" : horseService.DivineMountRidingDog != null && features.HasFact(horseService.DivineMountRidingDog) ? "RidingDog" : null,
                ["addPetFacts"] = features.Enumerable.Count(value => value.Get<AddPet>() != null),
                ["callAbility"] = unit.Abilities.GetAbility(horseService.DivineMountCallAbility) != null,
                ["callUses"] = unit.Resources.GetResourceAmount(horseService.DivineMountCallResource),
                ["ownedPets"] = Game.Instance.State.Units.Where(value => value.Descriptor.IsPet && value.Descriptor.Master.Value == unit.Unit).Select(value => value.UniqueId).Count()
            };
            facts["pet"] = pet == null ? null : Sprint8PetFacts(pet);
            return facts;
        }

        private JObject Sprint8PetFacts(UnitEntityData pet)
        {
            var progression = pet.Descriptor.Progression;
            var features = progression.Features;
            var resistance = pet.Descriptor.Get<UnitPartSpellResistance>();
            var reduction = pet.Descriptor.Get<UnitPartDamageReduction>();
            var petClass = pet.Blueprint.GetComponent<AddClassLevels>()?.CharacterClass;
            return new JObject {
                ["id"] = pet.UniqueId, ["blueprint"] = pet.Blueprint.AssetGuid, ["master"] = pet.Descriptor.Master.Value?.UniqueId, ["characterLevel"] = progression.CharacterLevel,
                ["classLevel"] = petClass == null ? 0 : progression.GetClassLevel(petClass), ["experience"] = progression.Experience, ["canLevelUp"] = LevelUpController.CanLevelUp(pet.Descriptor),
                ["strength"] = pet.Stats.Strength.ModifiedValue, ["constitution"] = pet.Stats.Constitution.ModifiedValue, ["intelligence"] = pet.Stats.Intelligence.ModifiedValue, ["charisma"] = pet.Stats.Charisma.ModifiedValue,
                ["upgrade"] = (horseService.HorseUpgrade != null && features.HasFact(horseService.HorseUpgrade)) || (horseService.RidingDogUpgrade != null && features.HasFact(horseService.RidingDogUpgrade)),
                ["intelligenceBond"] = features.GetRank(horseService.DivineMountIntelligence), ["celestial"] = features.GetRank(horseService.DivineMountCelestial),
                ["greaterCelestial"] = features.GetRank(horseService.DivineMountGreaterCelestial), ["spellResistanceFact"] = features.GetRank(horseService.DivineMountSpellResistance),
                ["spellResistance"] = resistance == null ? 0 : resistance.GetValue(null),
                ["reductionSources"] = new JArray((reduction?.AllSources ?? Enumerable.Empty<AddDamageResistanceBase>()).Select(source => source.GetType().Name + "=" + source.GetCurrentValue())),
                ["smiteAbility"] = pet.Descriptor.Abilities.GetAbility(horseService.DivineMountSmiteAbility) != null,
                ["smiteUses"] = pet.Descriptor.Resources.GetResourceAmount(horseService.DivineMountSmiteResource) };
        }

        private Sprint8Build Sprint8NewBuild(string race, Gender gender, string name)
        {
            var build = new Sprint8Build(logger) {
                Race = BlueprintRoot.Instance.Progression.CharacterRaces.Single(value => value.AssetGuid == race), Gender = gender, Name = name,
                ClassFor = level => horseService.PaladinClass, Snapshot = Sprint8Snapshot };
            build.OnSelection = (controller, state, record) =>
            {
                var selection = state.Selection as BlueprintFeatureSelection;
                if (selection == null) return;
                if (!ReferenceEquals(selection, horseService.DivineBondSelection) && !ReferenceEquals(selection, horseService.DivineMountSelection) &&
                    selection.AssetGuid != HorseCompanionBlueprintService.RangerSelectionGuid) return;
                var offers = record[selection.name] as JObject ?? new JObject();
                record[selection.name] = offers;
                offers["level"] = state.Level;
                offers["completeBeforeChoice"] = controller.State.IsComplete();
                offers["items"] = new JArray(selection.Items.Select(item => new JObject { ["name"] = item.Feature.name, ["guid"] = item.Feature.AssetGuid,
                    ["canSelect"] = selection.CanSelect(controller.Preview, controller.State, state, item) }));
            };
            return build;
        }

        private void Sprint8GrantExperienceFor(UnitDescriptor unit, int level)
        {
            var needed = BlueprintRoot.Instance.Progression.XPTable.GetBonus(level);
            if (unit.Progression.Experience < needed) unit.Progression.AdvanceExperienceTo(needed, false);
        }

        private IEnumerator Sprint8BuildChain(Sprint8Build build)
        {
            var guard = 0;
            while (build.CommitBuilderLevel())
            {
                if (++guard > 24) throw new InvalidOperationException("The native chain did not end.");
                yield return null;
            }
            yield return null;
        }

        private IEnumerator Sprint8Hire(Sprint8Build build, int firstLevels)
        {
            var original = SettingsRoot.Instance.AutoLevelup.CurrentValue;
            SettingsRoot.Instance.AutoLevelup.CurrentValue = AutolevelupState.Off;
            try
            {
                build.StartHire();
                yield return null;
                var chain = Sprint8BuildChain(build);
                while (chain.MoveNext()) yield return null;
            }
            finally { SettingsRoot.Instance.AutoLevelup.CurrentValue = original; }
            while (build.Hired.Progression.CharacterLevel < firstLevels)
            {
                Sprint8GrantExperienceFor(build.Hired, build.Hired.Progression.CharacterLevel + 1);
                build.CommitOwnLevel(build.Hired);
                yield return null;
            }
        }

        private IEnumerator Sprint8LevelTo(Sprint8Build build, UnitDescriptor unit, int level)
        {
            while (unit.Progression.CharacterLevel < level)
            {
                Sprint8GrantExperienceFor(unit, unit.Progression.CharacterLevel + 1);
                build.CommitOwnLevel(unit);
                yield return null;
            }
        }

        // Every level the owner's rank has made available, each with its choices actually made.
        private IEnumerator Sprint8CommitPetLevels(UnitEntityData pet, JArray commits)
        {
            var petClass = pet.Blueprint.GetComponent<AddClassLevels>().CharacterClass;
            var upgrade = pet.Blueprint.AssetGuid == HorseCompanionBlueprintService.RidingDogUnitGuid ? horseService.RidingDogUpgrade : horseService.HorseUpgrade;
            for (var guard = 0; guard < 20 && LevelUpController.CanLevelUp(pet.Descriptor); guard++)
            {
                var record = NativeHorseFixtureAcquisition.CommitPetLevel(pet, petClass, upgrade, logger);
                commits.Add(record);
                if (record.Value<bool>("committed") != true) throw new InvalidOperationException("A native pet level did not commit: " + record);
                yield return null;
            }
        }

        // ---- cases ------------------------------------------------------------------------------------------------------
        private IEnumerator Sprint8Inventory()
        {
            var paladin = horseService.PaladinClass;
            var progression = paladin?.Progression;
            s8Facts["bondApplied"] = horseService.DivineBondApplied;
            s8Facts["failure"] = horseService.DivineMountFailure;
            s8Facts["leases"] = new JArray(horseService.DivineBondLeaseOwners);
            s8Facts["levelFive"] = progression == null ? null : new JArray(progression.LevelEntries.Where(entry => entry.Level == 5).SelectMany(entry => entry.Features).Select(feature => feature.name));
            s8Facts["bondItems"] = new JArray(horseService.DivineBondSelection.AllFeatures.Select(feature => feature.AssetGuid));
            s8Facts["mountItems"] = new JArray(horseService.DivineMountSelection.AllFeatures.Select(feature => feature.AssetGuid));
            s8Facts["archetypes"] = new JArray(paladin.Archetypes.Select(archetype => new JObject { ["name"] = archetype.name, ["guid"] = archetype.AssetGuid,
                ["removesAtFive"] = new JArray((archetype.RemoveFeatures ?? new LevelEntry[0]).Where(entry => entry.Level == 5).SelectMany(entry => entry.Features).Select(feature => feature.name)) }));
            s8Facts["weaponBond"] = S8Blueprint(horseService.WeaponBondFeature);
            s8Facts["weaponUpgrades"] = new JArray(horseService.WeaponBondUpgrades.OfType<BlueprintFeature>().Select(S8Blueprint));
            s8Facts["rank"] = S8Blueprint(horseService.LevelRank);
            var library = ResourcesLibrary.LibraryObject.BlueprintsByAssetId;
            foreach (var pair in new[] { new { key = "empty", guid = HorseCompanionBlueprintService.EmptyCompanionGuid }, new { key = "stockMammoth", guid = HorseCompanionBlueprintService.MammothFeatureGuid },
                new { key = "stockDog", guid = HorseCompanionBlueprintService.DogFeatureGuid }, new { key = "rangerHorse", guid = HorseCompanionBlueprintService.FeatureGuid },
                new { key = "rangerSelection", guid = HorseCompanionBlueprintService.RangerSelectionGuid }, new { key = "huntersBond", guid = HorseCompanionBlueprintService.HuntersBondGuid },
                new { key = "druidRank", guid = "125af359f8bc9a145968b5d8fd8159b8" } })
                s8Facts[pair.key] = S8Blueprint(library.TryGetValue(pair.guid, out var value) ? value as BlueprintFeature : null);
            var rangerRank = library.TryGetValue("125af359f8bc9a145968b5d8fd8159b8", out var druid) ? druid as BlueprintProgression : null;
            s8Facts["druidRankClasses"] = rangerRank == null ? null : new JArray(rangerRank.Classes.Select(value => value.name));
            s8Facts["party"] = new JArray(Game.Instance.Player.PartyCharacters.Select(reference => reference.Value).Select(unit => new JObject { ["id"] = unit.UniqueId, ["blueprint"] = unit.Blueprint.name,
                ["level"] = unit.Descriptor.Progression.CharacterLevel, ["pet"] = unit.Descriptor.Pet?.Blueprint?.name, ["size"] = unit.Descriptor.OriginalSize.ToString() }));
            var mammoth = library.TryGetValue(HorseCompanionBlueprintService.MammothUnitGuid, out var unitValue) ? unitValue as BlueprintUnit : null;
            s8Facts["petFacts"] = mammoth == null ? null : new JArray(mammoth.AddFacts.Select(fact => fact?.name));
            yield break;
        }

        private IEnumerator Sprint8WeaponCase()
        {
            var build = Sprint8NewBuild(Sprint8HumanRace, Gender.Male, "KMC_S8_Weapon");
            build.Choices[HorseCompanionBlueprintService.DivineBondSelectionGuid] = HorseCompanionBlueprintService.WeaponBondGuid;
            var hire = Sprint8Hire(build, 5);
            while (hire.MoveNext()) yield return null;
            s8Weapon = build.Hired.Unit;
            var level = Sprint8LevelTo(build, build.Hired, 9);
            while (level.MoveNext()) yield return null;
            s8Facts["levels"] = build.Levels;
            s8Facts["final"] = Sprint8Snapshot(build.Hired);
        }

        // The class as it stood before this bond existed: the stock forced grant. A character made so is what a pre-Sprint 8 save holds.
        private IEnumerator Sprint8ExistingCase()
        {
            horseService.SetSelectionEnabled(false);
            s8SelectionDisabled = true;
            s8Facts["stockLevelFive"] = new JArray(horseService.PaladinClass.Progression.LevelEntries.Where(entry => entry.Level == 5).SelectMany(entry => entry.Features).Select(feature => feature.name));
            var build = Sprint8NewBuild(Sprint8HumanRace, Gender.Female, "KMC_S8_Existing");
            var hire = Sprint8Hire(build, 8);
            while (hire.MoveNext()) yield return null;
            s8Facts["stockBuilt"] = Sprint8Snapshot(build.Hired);
            s8Facts["stockChoices"] = build.Levels.DeepClone();
            horseService.SetSelectionEnabled(true);
            s8SelectionDisabled = false;
            yield return null;
            s8Facts["afterBondEnabled"] = Sprint8Snapshot(build.Hired);
            var later = Sprint8NewBuild(Sprint8HumanRace, Gender.Female, "KMC_S8_Existing");
            var level = Sprint8LevelTo(later, build.Hired, 9);
            while (level.MoveNext()) yield return null;
            s8Facts["laterLevels"] = later.Levels;
            s8Facts["final"] = Sprint8Snapshot(build.Hired);
        }

        private IEnumerator Sprint8Respec(Sprint8Build build, UnitDescriptor unit)
        {
            var original = SettingsRoot.Instance.AutoLevelup.CurrentValue;
            SettingsRoot.Instance.AutoLevelup.CurrentValue = AutolevelupState.Off;
            try
            {
                build.StartRespec(unit);
                yield return null;
                var chain = Sprint8BuildChain(build);
                while (chain.MoveNext()) yield return null;
            }
            finally { SettingsRoot.Instance.AutoLevelup.CurrentValue = original; }
        }

        private IEnumerator Sprint8RetrainCase()
        {
            if (s8Weapon == null) throw new InvalidOperationException("The weapon-bond character is missing.");
            var unit = s8Weapon.Descriptor;
            s8Facts["respecAllowed"] = Game.Instance.Player.Difficulty.RespecAllowed;
            s8Facts["before"] = Sprint8Snapshot(unit);
            var experience = unit.Progression.Experience;
            var toMount = Sprint8NewBuild(Sprint8HumanRace, Gender.Male, "KMC_S8_Weapon");
            toMount.Choices[HorseCompanionBlueprintService.DivineBondSelectionGuid] = HorseCompanionBlueprintService.DivineMountSelectionGuid;
            toMount.Choices[HorseCompanionBlueprintService.DivineMountSelectionGuid] = HorseCompanionBlueprintService.DivineMountHorseGuid;
            var respec = Sprint8Respec(toMount, unit);
            while (respec.MoveNext()) yield return null;
            for (var frame = 0; frame < 60; frame++) yield return null;
            s8Facts["retrainedLevels"] = toMount.Levels;
            s8Facts["experienceUnchanged"] = unit.Progression.Experience == experience;
            s8Facts["mountPath"] = Sprint8Snapshot(unit);
            var pet = unit.Pet;
            s8Facts["firstPet"] = pet?.UniqueId;
            var toWeapon = Sprint8NewBuild(Sprint8HumanRace, Gender.Male, "KMC_S8_Weapon");
            toWeapon.Choices[HorseCompanionBlueprintService.DivineBondSelectionGuid] = HorseCompanionBlueprintService.WeaponBondGuid;
            var back = Sprint8Respec(toWeapon, unit);
            while (back.MoveNext()) yield return null;
            for (var frame = 0; frame < 60; frame++) yield return null;
            s8Facts["backLevels"] = toWeapon.Levels;
            s8Facts["weaponPath"] = Sprint8Snapshot(unit);
            s8Facts["firstPetDestroyed"] = pet == null || pet.Destroyed;
            s8Facts["firstPetMaster"] = pet?.Descriptor?.Master.Value?.UniqueId;
        }

        private IEnumerator Sprint8MountOwner(Sprint8Build build, string choiceGuid, int levels)
        {
            build.Choices[HorseCompanionBlueprintService.DivineBondSelectionGuid] = HorseCompanionBlueprintService.DivineMountSelectionGuid;
            build.Choices[HorseCompanionBlueprintService.DivineMountSelectionGuid] = choiceGuid;
            var hire = Sprint8Hire(build, levels);
            while (hire.MoveNext()) yield return null;
            s8Owner = build.Hired.Unit;
        }

        private IEnumerator Sprint8MountHorseCase()
        {
            var build = Sprint8NewBuild(Sprint8HumanRace, Gender.Male, "KMC_S8_Horse");
            var owner = Sprint8MountOwner(build, HorseCompanionBlueprintService.DivineMountHorseGuid, 5);
            while (owner.MoveNext()) yield return null;
            var unit = s8Owner.Descriptor;
            var commits = new JArray();
            s8Facts["petCommits"] = commits;
            var boundaries = new JObject();
            s8Facts["boundaries"] = boundaries;
            // The chain may pass level five by the party's own experience; these boundaries are the feature's own.
            foreach (var level in new[] { 8, 9, 11, 13, 15, 17 })
            {
                var progress = Sprint8LevelTo(build, unit, level);
                while (progress.MoveNext()) yield return null;
                var before = Sprint8Snapshot(unit);
                var pet = unit.Pet;
                var petLevels = Sprint8CommitPetLevels(pet, commits);
                while (petLevels.MoveNext()) yield return null;
                boundaries[level.ToString()] = new JObject { ["before"] = before, ["after"] = Sprint8Snapshot(unit) };
            }
            s8Facts["levels"] = build.Levels;
            s8Facts["final"] = Sprint8Snapshot(unit);
        }

        private IEnumerator Sprint8ControlCase()
        {
            if (s8Owner == null) throw new InvalidOperationException("The Divine Mount owner is missing.");
            var pet = s8Owner.Descriptor.Pet;
            Sprint8Build.BringToField(s8Owner, rider.Position + rider.OrientationDirection * 4f);
            for (var frame = 0; frame < 60 && (pet.View?.AgentASP == null || !pet.IsInState); frame++) yield return null;
            for (var frame = 0; frame < 30; frame++) yield return null;
            s8Facts["inField"] = new JObject { ["owner"] = s8Owner.IsInGame, ["pet"] = pet.IsInGame, ["profile"] = SupportedMountedProfiles.Resolve(s8Owner, pet)?.Id };
            s8Facts["before"] = Sprint8Snapshot(s8Owner.Descriptor);

            // Call: from a distance, one use, adjacent afterwards.
            var data = s8Owner.Descriptor.Abilities.GetAbility(horseService.DivineMountCallAbility)?.Data;
            pet.Translocate(FindWalkablePoint(s8Owner.Position, 12f, 3f), null);
            yield return null;
            var call = new JObject { ["available"] = data?.IsAvailableForCast, ["usesBefore"] = s8Owner.Descriptor.Resources.GetResourceAmount(horseService.DivineMountCallResource), ["distanceBefore"] = Vector3.Distance(pet.Position, s8Owner.Position) };
            s8Facts["call"] = call;
            s8Owner.Commands.Run(new UnitUseAbility(data, s8Owner));
            for (var frame = 0; frame < 240 && !s8Owner.Commands.Empty; frame++) yield return null;
            for (var frame = 0; frame < 10; frame++) yield return null;
            call["distanceAfter"] = Vector3.Distance(pet.Position, s8Owner.Position);
            call["usesAfter"] = s8Owner.Descriptor.Resources.GetResourceAmount(horseService.DivineMountCallResource);

            // Ride, walk and dismount through the ordinary controls.
            for (var frame = 0; frame < 240 && !Sprint5Fixture.Click(nativeControls, s8Owner, nativeControls.MountAbility, pet); frame++) yield return null;
            Func<RelationshipState?> state = () => relationship.World.Registry.FindActive(s8Owner)?.Boundary?.State;
            for (var frame = 0; frame < 900 && state() != RelationshipState.Mounted; frame++) yield return null;
            s8Facts["mounted"] = state() == RelationshipState.Mounted;
            s8Facts["mountedProfile"] = SupportedMountedProfiles.Resolve(s8Owner, pet)?.Id;
            var whileMounted = s8Owner.Descriptor.Abilities.GetAbility(horseService.DivineMountCallAbility)?.Data;
            s8Facts["callWhileMounted"] = new JObject { ["available"] = whileMounted?.IsAvailableForCast, ["usesBefore"] = s8Owner.Descriptor.Resources.GetResourceAmount(horseService.DivineMountCallResource) };
            for (var frame = 0; frame < 20; frame++) yield return null;
            var from = pet.Position;
            SelectionManager.Instance.SelectUnit(s8Owner.View, true, true, false);
            ClickGroundHandler.MoveSelectedUnitsToPoint(FindWalkablePoint(pet.Position, 5f, 1.5f), false);
            for (var frame = 0; frame < 30; frame++) yield return null;
            for (var frame = 0; frame < 600 && (pet.View?.AgentASP?.IsReallyMoving == true || !pet.Commands.Empty); frame++) yield return null;
            s8Facts["movedMetres"] = Vector3.Distance(pet.Position, from);
            s8Facts["riderWithMount"] = Vector3.Distance(pet.Position, s8Owner.Position);

            // A compact attack neighbour: the ordinary hostile click, and the pair settling afterwards.
            s8Facts["attack"] = new JObject { ["clicked"] = false };
            if (target != null)
            {
                target.Position = FindWalkablePoint(pet.Position, 3f, 1f);
                yield return null;
                var attacksBefore = 0;
                SelectionManager.Instance.SelectUnit(s8Owner.View, true, true, false);
                using (var input = new NativeOrdinaryAttackInput(target))
                    s8Facts["attack"] = new JObject { ["clicked"] = input.Click(), ["attacksBefore"] = attacksBefore };
                for (var frame = 0; frame < 240; frame++)
                {
                    if (!s8Owner.Commands.Empty) ((JObject)s8Facts["attack"])["commanded"] = true;
                    yield return null;
                }
                s8Owner.Commands.InterruptAll();
                pet.Commands.InterruptAll();
                for (var frame = 0; frame < 60 && !(s8Owner.Commands.Empty && pet.Commands.Empty); frame++) yield return null;
            }

            for (var frame = 0; frame < 240 && !Sprint5Fixture.Click(nativeControls, s8Owner, nativeControls.DismountAbility, s8Owner); frame++) yield return null;
            for (var frame = 0; frame < 600 && state() == RelationshipState.Mounted; frame++) yield return null;
            s8Facts["dismounted"] = state() != RelationshipState.Mounted;
            for (var frame = 0; frame < 30; frame++) yield return null;

            // Celestial Smite through the pet's own ability: the marked evil target takes the pet's Hit Dice in damage.
            if (target != null)
            {
                target.Descriptor.Alignment.Set(Alignment.NeutralEvil);
                var smite = new JObject { ["usesBefore"] = pet.Descriptor.Resources.GetResourceAmount(horseService.DivineMountSmiteResource) };
                s8Facts["smite"] = smite;
                var weapon = pet.GetFirstWeapon();
                s8Probe = new Sprint8SmiteProbe(pet);
                target.Position = FindWalkablePoint(pet.Position, 2f, 1f);
                yield return null;
                Rulebook.Trigger(new RuleAttackWithWeapon(pet, target, weapon, 0));
                smite["additionalDamageUnmarked"] = s8Probe.LastAdditionalDamage;
                var ability = pet.Descriptor.Abilities.GetAbility(horseService.DivineMountSmiteAbility)?.Data;
                smite["canTarget"] = ability?.CanTarget(new Kingmaker.Utility.TargetWrapper(target));
                pet.Commands.Run(new UnitUseAbility(ability, new Kingmaker.Utility.TargetWrapper(target)));
                for (var frame = 0; frame < 240 && !pet.Commands.Empty; frame++) yield return null;
                for (var frame = 0; frame < 10; frame++) yield return null;
                smite["usesAfter"] = pet.Descriptor.Resources.GetResourceAmount(horseService.DivineMountSmiteResource);
                smite["marked"] = target.Descriptor.HasFact(horseService.DivineMountSmiteBuff);
                Rulebook.Trigger(new RuleAttackWithWeapon(pet, target, weapon, 0));
                smite["additionalDamageMarked"] = s8Probe.LastAdditionalDamage;
                smite["petHitDice"] = pet.Descriptor.Progression.CharacterLevel;
                s8Probe.Dispose(); s8Probe = null;
                var mark = target.Descriptor.Buffs.GetFact(horseService.DivineMountSmiteBuff);
                if (mark != null) target.Descriptor.RemoveFact(mark);
            }
            s8Facts["after"] = Sprint8Snapshot(s8Owner.Descriptor);
        }

        private IEnumerator Sprint8DogCase()
        {
            var build = Sprint8NewBuild(Sprint5Fixture.HalflingRace, Gender.Female, "KMC_S8_Dog");
            var owner = Sprint8MountOwner(build, HorseCompanionBlueprintService.DivineMountRidingDogGuid, 5);
            while (owner.MoveNext()) yield return null;
            var unit = s8Owner.Descriptor;
            var pet = unit.Pet;
            var commits = new JArray();
            s8Facts["petCommits"] = commits;
            var levels = Sprint8CommitPetLevels(pet, commits);
            while (levels.MoveNext()) yield return null;
            s8Facts["levels"] = build.Levels;
            Sprint8Build.BringToField(s8Owner, rider.Position + rider.OrientationDirection * 4f);
            for (var frame = 0; frame < 60 && (pet.View?.AgentASP == null || !pet.IsInState); frame++) yield return null;
            for (var frame = 0; frame < 30; frame++) yield return null;
            s8Facts["profile"] = SupportedMountedProfiles.Resolve(s8Owner, pet)?.Id;
            s8Facts["refusal"] = SupportedMountedProfiles.DescribeRefusal(s8Owner, pet);
            for (var frame = 0; frame < 240 && !Sprint5Fixture.Click(nativeControls, s8Owner, nativeControls.MountAbility, pet); frame++) yield return null;
            Func<RelationshipState?> state = () => relationship.World.Registry.FindActive(s8Owner)?.Boundary?.State;
            for (var frame = 0; frame < 900 && state() != RelationshipState.Mounted; frame++) yield return null;
            s8Facts["mounted"] = state() == RelationshipState.Mounted;
            var from = pet.Position;
            SelectionManager.Instance.SelectUnit(s8Owner.View, true, true, false);
            ClickGroundHandler.MoveSelectedUnitsToPoint(FindWalkablePoint(pet.Position, 5f, 1.5f), false);
            for (var frame = 0; frame < 30; frame++) yield return null;
            for (var frame = 0; frame < 600 && (pet.View?.AgentASP?.IsReallyMoving == true || !pet.Commands.Empty); frame++) yield return null;
            s8Facts["movedMetres"] = Vector3.Distance(pet.Position, from);
            for (var frame = 0; frame < 240 && !Sprint5Fixture.Click(nativeControls, s8Owner, nativeControls.DismountAbility, s8Owner); frame++) yield return null;
            for (var frame = 0; frame < 600 && state() == RelationshipState.Mounted; frame++) yield return null;
            s8Facts["dismounted"] = state() != RelationshipState.Mounted;
            s8Facts["final"] = Sprint8Snapshot(unit);
        }

        // A companion from another class stays the one companion, and the levels stack.
        private IEnumerator Sprint8MixedCase()
        {
            var build = Sprint8NewBuild(Sprint8HumanRace, Gender.Male, "KMC_S8_Mixed");
            var ranger = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(Chunk7OrdinaryBuildDriver.RangerClassGuid);
            build.ClassFor = next => next <= 4 ? ranger : horseService.PaladinClass;
            build.Choices[HorseCompanionBlueprintService.HuntersBondGuid] = HorseCompanionBlueprintService.RangerSelectionGuid;
            build.Choices[HorseCompanionBlueprintService.RangerSelectionGuid] = HorseCompanionBlueprintService.DogFeatureGuid;
            build.Choices[HorseCompanionBlueprintService.DivineBondSelectionGuid] = HorseCompanionBlueprintService.DivineMountSelectionGuid;
            build.Choices[HorseCompanionBlueprintService.DivineMountSelectionGuid] = HorseCompanionBlueprintService.EmptyCompanionGuid;
            var hire = Sprint8Hire(build, 4);
            while (hire.MoveNext()) yield return null;
            var unit = build.Hired;
            s8Facts["beforePaladin"] = Sprint8Snapshot(unit);
            var original = unit.Pet;
            var level = Sprint8LevelTo(build, unit, 9);
            while (level.MoveNext()) yield return null;
            for (var frame = 0; frame < 30; frame++) yield return null;
            s8Facts["levels"] = build.Levels;
            s8Facts["samePet"] = original != null && ReferenceEquals(original, unit.Pet) && !original.Destroyed;
            var commits = new JArray();
            s8Facts["petCommits"] = commits;
            var petLevels = Sprint8CommitPetLevels(unit.Pet, commits);
            while (petLevels.MoveNext()) yield return null;
            s8Facts["final"] = Sprint8Snapshot(unit);
            s8Owner = null;
        }

        // An archetype that replaces the bond stays legal and is offered no bond.
        private IEnumerator Sprint8ArchetypeCase()
        {
            var paladin = horseService.PaladinClass;
            var archetype = paladin.Archetypes.FirstOrDefault(value => (value.RemoveFeatures ?? new LevelEntry[0]).Any(entry => entry.Level == 5 && entry.Features.Contains(horseService.DivineBondSelection)));
            s8Facts["archetype"] = archetype?.name;
            if (archetype == null) yield break;
            var build = Sprint8NewBuild(Sprint8HumanRace, Gender.Male, "KMC_S8_Archetype");
            build.Archetype = archetype.AssetGuid;
            var hire = Sprint8Hire(build, 5);
            while (hire.MoveNext()) yield return null;
            s8Facts["levels"] = build.Levels;
            s8Facts["final"] = Sprint8Snapshot(build.Hired);
        }
    }
}
