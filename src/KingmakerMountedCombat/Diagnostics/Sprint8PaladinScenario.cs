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
using Kingmaker.Controllers.Rest;
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

        internal JObject CancelOwnLevel(UnitDescriptor unit)
        {
            var original = SettingsRoot.Instance.AutoLevelup.CurrentValue;
            LevelUpController controller = null;
            var record = new JObject { ["nextLevel"] = unit.Progression.CharacterLevel + 1 };
            try
            {
                SettingsRoot.Instance.AutoLevelup.CurrentValue = AutolevelupState.Off;
                controller = LevelUpController.StartWithoutAssigningStaticInstance(unit);
                Fill(controller, record);
                record["complete"] = controller.State.IsComplete();
            }
            finally
            {
                controller?.Cancel();
                SettingsRoot.Instance.AutoLevelup.CurrentValue = original;
            }
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
            // A recalculated preview replaces the controller's state, so it is read afresh each time.
            if (controller.State.CanSelectPortrait)
                Require(controller.SelectPortrait(BlueprintRoot.Instance.CharGen.Portraits.First(value => value != null)), "portrait");
            if (controller.State.CanSelectRace) Require(controller.SelectRace(Race), "race");
            if (controller.State.CanSelectGender) Require(controller.SelectGender(Gender), "gender");
            if (controller.Doll != null && (controller.State.Mode == LevelUpState.CharBuildMode.CharGen || controller.State.Mode == LevelUpState.CharBuildMode.Respec) && Race != null && Race.Presets.Length > 0)
            {
                controller.Doll.SetRacePreset(Race.Presets[0]);
                controller.Doll.SetLeftHanded(false);
            }
            var characterClass = ClassFor(controller.State.NextLevel);
            if (controller.State.SelectedClass == null)
            {
                if (Archetype != null && controller.Preview.Progression.GetClassLevel(characterClass) == 0)
                    record["archetypeAdded"] = controller.AddArchetype(characterClass, characterClass.Archetypes.Single(value => value.AssetGuid == Archetype));
                if (!controller.SelectClass(characterClass))
                    throw new InvalidOperationException("The native builder refused the class " + characterClass.name + " at " + controller.State.NextLevel + "; alignment=" + controller.Preview.Alignment.Value +
                        "; failing=" + string.Join(",", characterClass.ComponentsArray.OfType<Kingmaker.Blueprints.Classes.Prerequisites.Prerequisite>().Where(value => !value.Check(null, controller.Preview, controller.State)).Select(value => value.GetType().Name).ToArray()));
            }
            record["class"] = controller.State.SelectedClass?.name;
            foreach (var stat in new[] { StatType.Strength, StatType.Charisma, StatType.Constitution, StatType.Dexterity, StatType.Wisdom, StatType.Intelligence })
                while (!controller.State.StatsDistribution.IsComplete() && controller.State.StatsDistribution.CanAdd(stat) && controller.AddStatPoint(stat)) { }
            if (controller.State.CanSelectRaceStat) Require(controller.SelectRaceStat(StatType.Strength), "racial stat");
            while (controller.State.AttributePoints > 0) Require(controller.SpendAttributePoint(StatType.Strength), "attribute");
            for (var pass = 0; pass < 32; pass++)
            {
                var pending = controller.State.Selections.FirstOrDefault(selection => !selection.Selected && selection.CanSelectAnything(controller.State, controller.Preview));
                if (pending == null) break;
                Choose(controller, pending, record);
            }
            foreach (var skill in StatTypeHelper.Skills)
                while (controller.State.SkillPointsRemaining > 0 && controller.Preview.Stats.GetStat(skill).BaseValue < controller.State.NextLevel && controller.SpendSkillPoint(skill)) { }
            if (controller.State.CanSelectName) Require(controller.SelectName(Name), "name");
            if (controller.State.CanSelectAlignment)
                Require(controller.SelectAlignment(!controller.State.IsAlignmentRestricted(Alignment.LawfulGood) ? Alignment.LawfulGood : Enum.GetValues(typeof(Alignment)).Cast<Alignment>().First(value => !controller.State.IsAlignmentRestricted(value))), "alignment");
            if (controller.State.CanSelectVoice)
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
            {
                chosen = items.Single(item => item.Feature?.AssetGuid == wanted);
                var refusals = new JArray();
                record[selection.name + "IllegalAttempts"] = refusals;
                foreach (var illegal in items.Where(item => !((BlueprintFeatureSelection)pending.Selection).CanSelect(controller.Preview, controller.State, pending, item)))
                    refusals.Add(new JObject { ["name"] = illegal.Feature.name, ["accepted"] = controller.SelectFeature(pending, illegal) });
            }
            else
            {
                chosen = items.Where(item => item.Feature != null && item.Feature.GetComponent<AddPet>() == null && !(item.Feature is BlueprintParametrizedFeature) &&
                    ((BlueprintFeatureSelection)pending.Selection).CanSelect(controller.Preview, controller.State, pending, item))
                    .OrderBy(item => item.Feature.AssetGuid, StringComparer.Ordinal).FirstOrDefault(item => controller.SelectFeature(pending, item));
                if (chosen == null) throw new InvalidOperationException("No legal choice in " + selection.name + ".");
                RecordChoice(record, selection.name + "=" + chosen.Feature.name);
                return;
            }
            if (chosen == null) throw new InvalidOperationException("No legal choice in " + selection.name + ".");
            if (!controller.SelectFeature(pending, chosen)) throw new InvalidOperationException("The native controller refused " + chosen.Feature.name + " in " + selection.name + ".");
            record[selection.name + "DuplicateAccepted"] = controller.SelectFeature(pending, chosen);
            RecordChoice(record, selection.name + "=" + chosen.Feature.name);
        }

        private static void RecordChoice(JObject record, string choice)
        {
            var choices = record["choices"] as JArray;
            if (choices == null) { choices = new JArray(); record["choices"] = choices; }
            choices.Add(choice);
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

    // The persisted facts both rows and the cold load read.
    internal static class Sprint8Facts
    {
        internal static JObject Owner(HorseCompanionBlueprintService service, UnitDescriptor unit)
        {
            var progression = unit.Progression;
            var features = progression.Features;
            var pet = unit.Pet;
            var paladin = service.PaladinClass;
            var mountBond = features.HasFact(service.DivineMountSelection);
            var facts = new JObject {
                ["id"] = unit.Unit.UniqueId, ["characterLevel"] = progression.CharacterLevel, ["paladinLevel"] = progression.GetClassLevel(paladin), ["size"] = unit.OriginalSize.ToString(),
                ["archetypes"] = new JArray(progression.Classes.SelectMany(data => data.Archetypes).Select(value => value.name)),
                ["classes"] = new JArray(progression.Classes.Select(data => data.CharacterClass.name + ":" + data.Level)),
                ["rank"] = unit.GetFact(service.LevelRank)?.GetRank() ?? 0,
                ["weaponBond"] = features.HasFact(service.WeaponBondFeature),
                ["weaponUpgradeRanks"] = service.WeaponBondUpgrades.OfType<BlueprintFeature>().Sum(value => features.GetRank(value)),
                ["weaponAbilities"] = unit.ActivatableAbilities.Enumerable.Count(value => value.Blueprint.name.StartsWith("WeaponBond", StringComparison.Ordinal)) +
                    unit.Abilities.Enumerable.Count(value => value.Blueprint.name.StartsWith("WeaponBond", StringComparison.Ordinal)),
                ["weaponResources"] = service.WeaponBondFeature.GetComponents<AddAbilityResources>().Sum(value => unit.Resources.GetResourceAmount(value.Resource)),
                ["bondSelectionFact"] = features.HasFact(service.DivineBondSelection),
                ["mountBond"] = mountBond,
                ["mountChoice"] = features.HasFact(service.DivineMountHorse) ? "Horse" : service.DivineMountRidingDog != null && features.HasFact(service.DivineMountRidingDog) ? "RidingDog" : null,
                ["addPetFacts"] = features.Enumerable.Count(value => value.Get<AddPet>() != null),
                ["callAbility"] = unit.Abilities.GetAbility(service.DivineMountCallAbility) != null,
                ["callUses"] = unit.Resources.GetResourceAmount(service.DivineMountCallResource),
                ["callMax"] = service.DivineMountCallResource.GetMaxAmount(unit),
                ["weaponUsesMax"] = service.WeaponBondFeature.GetComponents<AddAbilityResources>().Sum(value => value.Resource.GetMaxAmount(unit)),
                ["ownedPets"] = Game.Instance.State.Units.Concat(Game.Instance.Player.CrossSceneState.AllEntityData.OfType<UnitEntityData>()).Where(value => value.Descriptor.IsPet && value.Descriptor.Master.Value == unit.Unit).Select(value => value.UniqueId).Distinct().Count()
            };
            facts["pet"] = pet == null ? null : Pet(service, pet);
            return facts;
        }

        internal static JObject Pet(HorseCompanionBlueprintService service, UnitEntityData pet)
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
                ["upgrade"] = (service.HorseUpgrade != null && features.HasFact(service.HorseUpgrade)) || (service.RidingDogUpgrade != null && features.HasFact(service.RidingDogUpgrade)),
                ["intelligenceBond"] = features.GetRank(service.DivineMountIntelligence), ["lesserCelestial"] = features.GetRank(service.DivineMountLesserCelestial), ["celestial"] = features.GetRank(service.DivineMountCelestial),
                ["greaterCelestial"] = features.GetRank(service.DivineMountGreaterCelestial), ["spellResistanceFact"] = features.GetRank(service.DivineMountSpellResistance),
                ["spellResistance"] = resistance == null ? 0 : resistance.GetValue(null),
                ["reductionSources"] = new JArray((reduction?.AllSources ?? Enumerable.Empty<AddDamageResistanceBase>()).Select(source => source.GetType().Name + "=" + source.GetCurrentValue())),
                ["smiteFact"] = features.GetRank(service.DivineMountSmite), ["smiteAbility"] = pet.Descriptor.Abilities.GetAbility(service.DivineMountSmiteAbility) != null,
                ["smiteUses"] = pet.Descriptor.Resources.GetResourceAmount(service.DivineMountSmiteResource) };
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
            LastAttackBonus = evt.AttackBonus;
        }

        public void Dispose() => EventBus.Unsubscribe(this);
    }

    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal const string Sprint8Choice = "sprint8-paladin-choice-rt", Sprint8Mount = "sprint8-paladin-mount-rt", Sprint8Dog = "sprint8-paladin-dog-rt", Sprint8Mixed = "sprint8-paladin-mixed-rt", Sprint8Celestial = "sprint8-paladin-celestial-rt", Sprint8TurnBased = "sprint8-paladin-tb";
        internal static bool IsSprint8PaladinScenario(string scenario) => scenario == Sprint8Choice || scenario == Sprint8Mount || scenario == Sprint8Dog || scenario == Sprint8Mixed || scenario == Sprint8Celestial || scenario == Sprint8TurnBased;
        private bool IsSprint8Paladin => IsSprint8PaladinScenario(request.Scenario);
        internal const int Sprint8SchemaVersion = 52;
        internal const double Sprint8ScenarioDeadlineSeconds = 400.0d, Sprint8HostDeadlineSeconds = 560.0d;
        private string[] Sprint8Cases => request.Scenario == Sprint8Choice ? new[] { "S8-inventory", "S8-weapon", "S8-existing", "S8-retrain" } :
            request.Scenario == Sprint8Mount ? new[] { "S8-mount-horse", "S8-control" } :
            request.Scenario == Sprint8Celestial ? new[] { "S8-tiers", "S8-smite" } :
            request.Scenario == Sprint8TurnBased ? new[] { "S8-tb" } :
            request.Scenario == Sprint8Dog ? new[] { "S8-dog" } : new[] { "S8-mixed", "S8-mixed-reverse", "S8-archetype" };
        private const string Sprint8HumanRace = "0a5d473ead98b0646b94495af250fdc4";

        private int s8CaseIndex;
        private IEnumerator s8Script;
        private JObject s8Facts, s8Measure;
        private UnitEntityData s8Weapon, s8Owner;
        private Sprint8SmiteProbe s8Probe;
        private bool s8SelectionDisabled;
        private DiagnosticCombatTargetService s8TargetService;
        private UnitEntityData s8Hostile;
        private Sprint6RuleObserver s8Rules;
        private DiagnosticCombatTargetService s8SecondTargetService;
        private UnitEntityData s8TierOwner, s8MixedOwner;

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
            if (s8Rules != null) { s8Rules.Dispose(); s8Rules = null; }
            if (s8SecondTargetService != null)
            {
                if (!s8SecondTargetService.DestroyAndVerify()) return false;
                s8SecondTargetService.Dispose(); s8SecondTargetService = null;
            }
            if (s8TargetService != null)
            {
                if (!s8TargetService.DestroyAndVerify()) return false;
                s8TargetService.Dispose(); s8TargetService = null; s8Hostile = null;
            }
            if (s8SelectionDisabled) { horseService.SetSelectionEnabled(true); s8SelectionDisabled = false; }
            var builder = Game.Instance.UI.CharacterBuildController;
            if (builder?.LevelUpController != null && builder.Unit != null) { builder.LevelUpController.Cancel(); builder.Show(false); }
            var second = s8Owner == null ? null : relationship.World.Registry.FindActive(s8Owner)?.Boundary;
            if (s8CharismaPet != null) { s8CharismaPet.Stats.Charisma.BaseValue = s8CharismaOriginal; s8CharismaPet = null; }
            if (second?.State == RelationshipState.Mounted)
            {
                TryEndCastingCleanupTurn();
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
                case "S8-mixed-reverse": return Sprint8MixedReverseCase();
                case "S8-archetype": return Sprint8ArchetypeCase();
                case "S8-tiers": return Sprint8TiersCase();
                case "S8-smite": return Sprint8SmiteCase();
                case "S8-tb": return Sprint8TurnBasedCase();
                default: throw new InvalidOperationException("Unknown Sprint 8 case " + name);
            }
        }

        // ---- facts ------------------------------------------------------------------------------------------------------
        private static JArray S8ComponentTypes(BlueprintScriptableObject blueprint) =>
            new JArray((blueprint?.ComponentsArray ?? new BlueprintComponent[0]).Select(component => component?.GetType().Name + (component is AddFeatureOnApply apply ? ":" + apply.Feature?.name : component is AddFeatureToCompanion companion ? ":" + companion.Feature?.name : component is AddFacts add ? ":" + string.Join("+", add.Facts.Select(fact => fact?.name).ToArray()) : component is Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteFeaturesFromList list ? ":" + string.Join("+", list.Features.Select(fact => fact?.name).ToArray()) : "")));

        private static JObject S8Blueprint(BlueprintFeature feature) => feature == null ? null : new JObject {
            ["name"] = feature.name, ["guid"] = feature.AssetGuid, ["ranks"] = feature.Ranks, ["classFeature"] = feature.IsClassFeature, ["hidden"] = feature.HideInUI,
            ["groups"] = new JArray(feature.Groups.Select(group => group.ToString())), ["components"] = S8ComponentTypes(feature) };

        private JObject Sprint8Snapshot(UnitDescriptor unit) => Sprint8Facts.Owner(horseService, unit);

        private JObject Sprint8PetFacts(UnitEntityData pet) => Sprint8Facts.Pet(horseService, pet);

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
            var bondEntry = progression.LevelEntries.Single(value => value.Level == 5 && value.Features.Contains(horseService.DivineBondSelection));
            var slot = bondEntry.Features.IndexOf(horseService.DivineBondSelection);
            var foreign = progression.LevelEntries.First(value => value.Level == 4).Features[0];
            bondEntry.Features[slot] = foreign;
            var drift = new JObject { ["enableAccepted"] = horseService.SetSelectionEnabled(true), ["applied"] = horseService.DivineBondApplied, ["failure"] = horseService.DivineMountFailure, ["slotKept"] = ReferenceEquals(bondEntry.Features[slot], foreign) };
            s8Facts["drift"] = drift;
            bondEntry.Features[slot] = horseService.DivineBondSelection;
            horseService.SetSelectionEnabled(true);
            drift["reapplied"] = horseService.DivineBondApplied;
            drift["failureAfter"] = horseService.DivineMountFailure;
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
            var field = Sprint8Field();
            while (field.MoveNext()) yield return null;
            var commits = new JArray();
            s8Facts["petCommits"] = commits;
            var boundaries = new JObject();
            s8Facts["boundaries"] = boundaries;
            // The chain may pass level five by the party's own experience; these boundaries are the feature's own.
            foreach (var level in new[] { 8, 9, 11, 13, 15, 17 })
            {
                var progress = Sprint8LevelTo(build, unit, level);
                while (progress.MoveNext()) yield return null;
                for (var frame = 0; frame < 10; frame++) yield return null; // the live companion sync runs once per update
                var before = Sprint8Snapshot(unit);
                var pet = unit.Pet;
                var petLevels = Sprint8CommitPetLevels(pet, commits);
                while (petLevels.MoveNext()) yield return null;
                boundaries[level.ToString()] = new JObject { ["before"] = before, ["after"] = Sprint8Snapshot(unit) };
            }
            s8Facts["levels"] = build.Levels;
            s8Facts["final"] = Sprint8Snapshot(unit);
        }

        private IEnumerator Sprint8Field()
        {
            Sprint8Build.BringToField(s8Owner, rider.Position + rider.OrientationDirection * 4f);
            var pet = s8Owner.Descriptor.Pet;
            for (var frame = 0; frame < 60 && (pet == null || pet.View?.AgentASP == null || !pet.IsInState); frame++) yield return null;
            for (var frame = 0; frame < 30; frame++) yield return null;
        }

        private IEnumerator Sprint8ControlCase()
        {
            if (s8Owner == null) throw new InvalidOperationException("The Divine Mount owner is missing.");
            var pet = s8Owner.Descriptor.Pet;
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

            // A compact attack neighbour: the ordinary hostile click on a native target, the same way every mounted row attacks.
            s8TargetService = new DiagnosticCombatTargetService(logger, repeatedNativeSequences: true);
            s8Hostile = s8TargetService.Spawn(s8Owner, pet, FindWalkablePoint(s8Owner.Position, 4.5f, 1f), request.RunId + "-s8", true, true);
            if (!s8TargetService.PrepareForPlayerClick(s8Hostile) || !s8TargetService.QueueBidirectionalCombatMemory(s8Owner, s8Hostile))
                throw new InvalidOperationException("The Sprint 8 target's visibility or combat-memory lease failed.");
            s8TargetService.BeginExpectedAttackDispatch(s8Hostile);
            s8Rules = new Sprint6RuleObserver(s8Hostile);
            for (var frame = 0; frame < 20; frame++) yield return null;
            SelectionManager.Instance.SelectUnit(s8Owner.View, true, true, false);
            var attack = new JObject();
            s8Facts["attack"] = attack;
            using (var input = new NativeOrdinaryAttackInput(s8Hostile)) attack["clicked"] = input.Click();
            var until = Time.time + 12f;
            while (Time.time < until && !s8Rules.Attacks.Any(row => row.Value<string>("attacker") == s8Owner.UniqueId))
            {
                s8TargetService.ObserveTargetLifeState(); s8TargetService.RefreshBidirectionalCombatMemoryLease();
                yield return null;
            }
            attack["riderAttacks"] = s8Rules.Attacks.Count(row => row.Value<string>("attacker") == s8Owner.UniqueId);
            attack["mounted"] = state() == RelationshipState.Mounted;
            s8Owner.Commands.InterruptAll();
            pet.Commands.InterruptAll();
            for (var frame = 0; frame < 60 && !(s8Owner.Commands.Empty && pet.Commands.Empty); frame++) yield return null;

            // Celestial Smite through the pet's own ability: the marked evil target takes the pet's Hit Dice in damage.
            s8Hostile.Descriptor.Alignment.Set(Alignment.NeutralEvil); // disclosed setup: the spawned target is not evil
            var smite = new JObject { ["usesBefore"] = pet.Descriptor.Resources.GetResourceAmount(horseService.DivineMountSmiteResource) };
            s8Facts["smite"] = smite;
            var weapon = pet.GetFirstWeapon();
            s8Probe = new Sprint8SmiteProbe(pet);
            // Disclosed setup: a Charisma with a bonus, since a horse's has none to add.
            var charisma = pet.Stats.Charisma.BaseValue;
            pet.Stats.Charisma.BaseValue = 14;
            smite["charismaBonus"] = pet.Stats.Charisma.Bonus;
            Rulebook.Trigger(new RuleAttackWithWeapon(pet, s8Hostile, weapon, 0));
            smite["additionalDamageUnmarked"] = s8Probe.LastAdditionalDamage;
            smite["attackBonusUnmarked"] = s8Probe.LastAttackBonus;
            var ability = pet.Descriptor.Abilities.GetAbility(horseService.DivineMountSmiteAbility)?.Data;
            var wrapper = new Kingmaker.Utility.TargetWrapper(s8Hostile);
            smite["canTarget"] = ability?.CanTarget(wrapper);
            pet.Commands.Run(new UnitUseAbility(ability, wrapper));
            for (var frame = 0; frame < 240 && !pet.Commands.Empty; frame++) { s8TargetService.RefreshBidirectionalCombatMemoryLease(); yield return null; }
            for (var frame = 0; frame < 10; frame++) yield return null;
            smite["usesAfter"] = pet.Descriptor.Resources.GetResourceAmount(horseService.DivineMountSmiteResource);
            smite["marked"] = s8Hostile.Descriptor.HasFact(horseService.DivineMountSmiteBuff);
            Rulebook.Trigger(new RuleAttackWithWeapon(pet, s8Hostile, weapon, 0));
            smite["additionalDamageMarked"] = s8Probe.LastAdditionalDamage;
            smite["attackBonusMarked"] = s8Probe.LastAttackBonus;
            pet.Stats.Charisma.BaseValue = charisma;
            var mark = s8Hostile.Descriptor.Buffs.GetFact(horseService.DivineMountSmiteBuff) as Kingmaker.UnitLogic.Buffs.Buff;
            smite["markExpires"] = mark != null && !mark.IsPermanent;
            smite["petHitDice"] = pet.Descriptor.Progression.CharacterLevel;
            s8Probe.Dispose(); s8Probe = null;
            s8Rules.Dispose(); s8Rules = null;
            // With the choice switched off, a mount-bonded character still takes no weapon upgrade.
            horseService.SetSelectionEnabled(false);
            s8SelectionDisabled = true;
            s8Facts["whileDisabled"] = new JObject { ["applied"] = horseService.DivineBondApplied,
                ["entry"] = new JArray(horseService.FilterLevelEntryFeatures(s8Owner.Descriptor, horseService.WeaponBondUpgrades.ToList()).Select(value => value.name)) };
            horseService.SetSelectionEnabled(true);
            s8SelectionDisabled = false;
            // A character outside the party keeps the stock bond at the same entry.
            var entryFeatures = new System.Collections.Generic.List<BlueprintFeatureBase> { horseService.DivineBondSelection };
            s8Facts["outsideParty"] = new JObject { ["playerFaction"] = s8Hostile.Descriptor.IsPlayerFaction, ["entry"] = new JArray(horseService.FilterLevelEntryFeatures(s8Hostile.Descriptor, entryFeatures).Select(value => value.name)) };
            for (var frame = 0; frame < 300 && !s8TargetService.DestroyAndVerify(); frame++) yield return null;
            s8TargetService.Dispose(); s8TargetService = null; s8Hostile = null;

            for (var frame = 0; frame < 240 && !Sprint5Fixture.Click(nativeControls, s8Owner, nativeControls.DismountAbility, s8Owner); frame++) yield return null;
            for (var frame = 0; frame < 600 && state() == RelationshipState.Mounted; frame++) yield return null;
            s8Facts["dismounted"] = state() != RelationshipState.Mounted;
            for (var frame = 0; frame < 30; frame++) yield return null;

            // A fallen mount: the call refuses, and the same companion comes back by the native rule, its facts intact.
            var fallen = new JObject { ["petBefore"] = pet.UniqueId };
            s8Facts["fallen"] = fallen;
            pet.Damage = pet.MaxHP + 1;
            for (var frame = 0; frame < 240 && pet.Descriptor.State.IsConscious; frame++) yield return null;
            fallen["conscious"] = pet.Descriptor.State.IsConscious;
            fallen["dead"] = pet.Descriptor.State.IsFinallyDead;
            var fallenCall = s8Owner.Descriptor.Abilities.GetAbility(horseService.DivineMountCallAbility)?.Data;
            s8Owner.Descriptor.Resources.Restore(horseService.DivineMountCallResource);
            fallen["callAvailable"] = fallenCall?.IsAvailableForCast;
            pet.Descriptor.ResurrectAndFullRestore();
            var settled = 0;
            for (; settled < 600 && !(pet.Descriptor.State.CanAct && s8Owner.Descriptor.Resources.GetResourceAmount(horseService.DivineMountCallResource) > 0 && fallenCall?.IsAvailableForCast == true); settled++) yield return null;
            fallen["settleFrames"] = settled;
            fallen["consciousAfter"] = pet.Descriptor.State.IsConscious;
            fallen["petCanActAfter"] = pet.Descriptor.State.CanAct;
            fallen["petProneAfter"] = pet.Descriptor.State.Prone.Active;
            fallen["callUsesAfter"] = s8Owner.Descriptor.Resources.GetResourceAmount(horseService.DivineMountCallResource);
            fallen["ownerCanAct"] = s8Owner.Descriptor.State.CanAct;
            fallen["ownerCommands"] = s8Owner.Commands.Empty;
            fallen["callReason"] = fallenCall?.Blueprint.GetComponent<DivineMountCallLogic>()?.GetReason();
            fallen["callAvailableAfter"] = fallenCall?.IsAvailableForCast;
            fallen["petAfter"] = s8Owner.Descriptor.Pet?.UniqueId;

            s8Facts["after"] = Sprint8Snapshot(s8Owner.Descriptor);
        }

        // Sprint 9: the unexecuted turn-based neighbor. Every action is the ordinary ability or attack click on the actor's own native turn.
        private TurnBased.Controllers.TurnController s8EndedTurn;
        private UnitEntityData s8CharismaPet;
        private int s8CharismaOriginal;
        private IEnumerator Sprint8TbTurn(UnitEntityData actor)
        {
            for (var frame = 0; frame < 6000; frame++)
            {
                var game = Game.Instance; var turn = game.TurnBasedCombatController.CurrentTurn;
                S8Refresh();
                if (turn?.Unit == actor && !ReferenceEquals(turn, s8EndedTurn) && Chunk7NativeFixture.ReadyForTurnInput(turn) && actor.Commands.Empty && !actor.AreHandsBusyWithAnimation) yield break;
                if (turn?.Unit != null && turn.Unit.IsDirectlyControllable && !ReferenceEquals(turn, s8EndedTurn) && turn.Unit.Commands.Empty && !turn.Unit.AreHandsBusyWithAnimation &&
                    turn.CanEndTurnAndNoActing() && !game.TurnBasedCombatController.WaitingForUI && MountedPairTurnParticipant.NativeNextCandidate(game.TurnBasedCombatController) == null)
                {
                    game.PauseBind();
                    s8EndedTurn = turn;
                }
                yield return null;
            }
            throw new InvalidOperationException("The native turn of " + actor.CharacterName + " never became ready.");
        }

        private IEnumerator Sprint8TbSettle(UnitEntityData actor, int bound = 900)
        {
            for (var frame = 0; frame < 20; frame++) yield return null;
            for (var frame = 0; frame < bound && (!actor.Commands.Empty || actor.AreHandsBusyWithAnimation); frame++)
            {
                S8Refresh();
                yield return null;
            }
        }

        private DivineMountSmiteLogic S8SmiteLogic => horseService.DivineMountSmiteAbility.GetComponent<DivineMountSmiteLogic>();
        private Kingmaker.UnitLogic.Abilities.AbilityData S8SmiteData(UnitEntityData pet) => pet.Descriptor.Abilities.GetAbility(horseService.DivineMountSmiteAbility)?.Data;
        private int S8SmiteRefusals => relationship.World.ForActor(s8Owner).Combat.MountedSmiteRefusalCount;

        private JObject S8SmiteResources(UnitEntityData pet, UnitEntityData victim) => new JObject {
            ["uses"] = pet.Descriptor.Resources.GetResourceAmount(horseService.DivineMountSmiteResource), ["mountSwift"] = pet.CombatState.Cooldown.SwiftAction,
            ["riderStandard"] = s8Owner.CombatState.Cooldown.StandardAction, ["riderMove"] = s8Owner.CombatState.Cooldown.MoveAction, ["marked"] = victim.Descriptor.HasFact(horseService.DivineMountSmiteBuff),
            ["turnStatus"] = Game.Instance.TurnBasedCombatController.CurrentTurn?.Status.ToString(), ["commands"] = new JArray(pet.Commands.Raw.Where(command => command != null).Select(command => command.GetType().Name)) };

        private void S8Refresh()
        {
            s8TargetService?.ObserveTargetLifeState(); s8TargetService?.RefreshBidirectionalCombatMemoryLease();
            s8SecondTargetService?.ObserveTargetLifeState(); s8SecondTargetService?.RefreshBidirectionalCombatMemoryLease();
        }

        // A click on a target that needs an approach: the native handler refuses it, and nothing exists to start.
        private IEnumerator S8TbFarClick(JObject record, UnitEntityData pet, UnitEntityData victim)
        {
            JObject place;
            if (!MountedChargeFixture.TryPlaceBeyond(pet, victim, S8SmiteData(pet).GetApproachDistance(victim), out place)) throw new InvalidOperationException("No walkable point beyond the Smite's reach.");
            record["place"] = place;
            for (var frame = 0; frame < 20; frame++) { S8Refresh(); yield return null; }
            var from = pet.Position; var refusals = S8SmiteRefusals;
            record["before"] = S8SmiteResources(pet, victim);
            record["needsApproach"] = S8SmiteLogic.NeedsApproach(pet, victim);
            record["canTarget"] = S8SmiteData(pet).CanTarget(new Kingmaker.Utility.TargetWrapper(victim));
            record["clicked"] = Sprint5Fixture.Click(nativeControls, pet, horseService.DivineMountSmiteAbility, victim);
            Game.Instance.SelectedAbilityHandler?.DropAbility();
            for (var frame = 0; frame < 30; frame++) { S8Refresh(); yield return null; }
            record["after"] = S8SmiteResources(pet, victim);
            record["refusals"] = S8SmiteRefusals - refusals;
            record["feedback"] = relationship.World.ForActor(s8Owner).Combat.LastFeedback;
            record["mountMoved"] = Vector3.Distance(pet.Position, from);
        }

        private IEnumerator Sprint8TbSmiteAttempt(JObject record, UnitEntityData pet, UnitEntityData victim)
        {
            var ability = S8SmiteData(pet);
            var turn = Game.Instance.TurnBasedCombatController.CurrentTurn;
            record["turnUnit"] = turn?.Unit?.UniqueId; record["turnStatus"] = turn?.Status.ToString();
            record["available"] = ability?.IsAvailableForCast; record["canTarget"] = ability?.CanTarget(new Kingmaker.Utility.TargetWrapper(victim));
            record["maySelectPartner"] = relationship.World.ForActor(s8Owner) != null && SelectionManager.Instance != null;
            record["clicked"] = Sprint5Fixture.Click(nativeControls, pet, horseService.DivineMountSmiteAbility, victim);
            record["selected"] = SelectionManager.Instance.SingleSelectedUnit?.UniqueId;
            record["selectedIsPet"] = SelectionManager.Instance.SingleSelectedUnit == pet;
            for (var frame = 0; frame < 5; frame++) yield return null;
            record["commandsAfterClick"] = new JArray(pet.Commands.Raw.Where(command => command != null).Select(command => command.GetType().Name + ":started=" + command.IsStarted + ":finished=" + command.IsFinished));
            record["queueAfterClick"] = pet.Commands.Queue.Count();
            record["statusAfterClick"] = Game.Instance.TurnBasedCombatController.CurrentTurn?.Status.ToString();
            var settle = Sprint8TbSettle(pet, 300);
            while (settle.MoveNext()) yield return null;
            record["commandsAfterSettle"] = new JArray(pet.Commands.Raw.Where(command => command != null).Select(command => command.GetType().Name + ":started=" + command.IsStarted + ":finished=" + command.IsFinished + ":result=" + command.Result));
            record["usesAfter"] = pet.Descriptor.Resources.GetResourceAmount(horseService.DivineMountSmiteResource);
            record["marked"] = victim.Descriptor.HasFact(horseService.DivineMountSmiteBuff);
            record["mountSwiftAfter"] = pet.CombatState.Cooldown.SwiftAction;
            record["riderStandardAfter"] = s8Owner.CombatState.Cooldown.StandardAction;
            if (!pet.Commands.Empty) pet.Commands.InterruptAll();
        }

        private IEnumerator Sprint8TurnBasedCase()
        {
            var build = Sprint8NewBuild(Sprint8HumanRace, Gender.Male, "KMC_S8_TurnBased");
            var owner = Sprint8MountOwner(build, HorseCompanionBlueprintService.DivineMountHorseGuid, 5);
            while (owner.MoveNext()) yield return null;
            var unit = s8Owner.Descriptor;
            var field = Sprint8Field();
            while (field.MoveNext()) yield return null;
            var progress = Sprint8LevelTo(build, unit, 11);
            while (progress.MoveNext()) yield return null;
            for (var frame = 0; frame < 10; frame++) yield return null;
            var pet = unit.Pet;
            var commits = new JArray(); s8Facts["petCommits"] = commits;
            var petLevels = Sprint8CommitPetLevels(pet, commits);
            while (petLevels.MoveNext()) yield return null;
            s8Facts["before"] = Sprint8Snapshot(unit);
            s8Facts["removalReferences"] = new JArray(MountedRemovalPreparation.FindPermanentReferences(Game.Instance, horseService).ToArray());

            // Disclosed setup: the turn-based mode, a target, and the pet sent away so the Call has something to do.
            if (turnBasedModeProbe == null) turnBasedModeProbe = new NativeModeTransitionProbe(true);
            while (!turnBasedModeProbe.TemporaryValueIsCurrent) { turnBasedModeProbe.DispatchTemporaryValueIfRequired(); yield return null; }
            if (pairedAutomaticEndProbe == null) pairedAutomaticEndProbe = new NativeAutomaticEndProbe(false);
            pet.Translocate(FindWalkablePoint(s8Owner.Position, 12f, 3f), null);
            yield return null;
            s8TargetService = new DiagnosticCombatTargetService(logger, repeatedNativeSequences: true);
            s8Hostile = s8TargetService.Spawn(s8Owner, pet, FindWalkablePoint(s8Owner.Position, 7f, 1f), request.RunId + "-s8tb", true, true);
            if (!s8TargetService.PrepareForPlayerClick(s8Hostile) || !s8TargetService.QueueBidirectionalCombatMemory(s8Owner, s8Hostile))
                throw new InvalidOperationException("The Sprint 8 turn-based target's visibility or combat-memory lease failed.");
            s8TargetService.BeginExpectedAttackDispatch(s8Hostile);
            s8Rules = new Sprint6RuleObserver(s8Hostile);
            for (var frame = 0; frame < 900 && !(s8Owner.IsInCombat && pet.IsInCombat); frame++) { s8TargetService.RefreshBidirectionalCombatMemoryLease(); yield return null; }
            if (!s8Owner.IsInCombat || !pet.IsInCombat) throw new InvalidOperationException("Turn-based combat did not start for the owner and its mount.");

            // Unmounted control: the same far target stays targetable, with the native approach untouched.
            s8Hostile.Descriptor.Alignment.Set(Alignment.NeutralEvil); // disclosed setup: the spawned target is not evil
            JObject unmountedPlace; var hostileHome = s8Hostile.Position;
            if (!MountedChargeFixture.TryPlaceBeyond(pet, s8Hostile, S8SmiteData(pet).GetApproachDistance(s8Hostile), out unmountedPlace)) throw new InvalidOperationException("No walkable point beyond the Smite's reach.");
            s8Facts["unmountedFar"] = new JObject { ["place"] = unmountedPlace, ["needsApproach"] = S8SmiteLogic.NeedsApproach(pet, s8Hostile), ["canTarget"] = S8SmiteData(pet).CanTarget(new Kingmaker.Utility.TargetWrapper(s8Hostile)) };
            s8Hostile.Translocate(hostileHome, null);
            s8Facts["order"] = new JObject { ["ownerInitiative"] = s8Owner.CombatState.Initiative, ["mountInitiative"] = pet.CombatState.Initiative, ["hostileInitiative"] = s8Hostile.CombatState.Initiative };

            // First turn: the Call, a full-round ability.
            var wait = Sprint8TbTurn(s8Owner);
            while (wait.MoveNext()) yield return null;
            var data = unit.Abilities.GetAbility(horseService.DivineMountCallAbility)?.Data;
            var call = new JObject { ["available"] = data?.IsAvailableForCast, ["reason"] = data?.Blueprint.GetComponent<DivineMountCallLogic>()?.GetReason(), ["petCanAct"] = pet.Descriptor.State.CanAct, ["petInGame"] = pet.IsInGame, ["petInPair"] = DivineMountCallLogic.IsMountedPairMember?.Invoke(pet),
                ["turnStatus"] = Game.Instance.TurnBasedCombatController.CurrentTurn?.Status.ToString(), ["usesBefore"] = unit.Resources.GetResourceAmount(horseService.DivineMountCallResource), ["distanceBefore"] = Vector3.Distance(pet.Position, s8Owner.Position) };
            s8Facts["call"] = call;
            call["availableAfterSelect"] = null;
            SelectionManager.Instance.SelectUnit(s8Owner.View, true, true, false);
            yield return null;
            call["availableAfterSelect"] = data?.IsAvailableForCast;
            call["clicked"] = Sprint5Fixture.Click(nativeControls, s8Owner, horseService.DivineMountCallAbility, s8Owner);
            var settle = Sprint8TbSettle(s8Owner);
            while (settle.MoveNext()) yield return null;
            call["distanceAfter"] = Vector3.Distance(pet.Position, s8Owner.Position);
            call["usesAfter"] = unit.Resources.GetResourceAmount(horseService.DivineMountCallResource);
            call["standard"] = s8Owner.CombatState.Cooldown.StandardAction;

            // Second turn: mount, then the Call refuses.
            var ended = Game.Instance.TurnBasedCombatController.CurrentTurn;
            for (var frame = 0; frame < 600 && !(ended.CanEndTurnAndNoActing() && !Game.Instance.TurnBasedCombatController.WaitingForUI); frame++) yield return null;
            Game.Instance.PauseBind(); s8EndedTurn = ended;
            wait = Sprint8TbTurn(s8Owner);
            while (wait.MoveNext()) yield return null;
            s8Facts["mountClicked"] = Sprint5Fixture.Click(nativeControls, s8Owner, nativeControls.MountAbility, pet);
            Func<RelationshipState?> state = () => relationship.World.Registry.FindActive(s8Owner)?.Boundary?.State;
            for (var frame = 0; frame < 900 && state() != RelationshipState.Mounted; frame++) { s8TargetService.RefreshBidirectionalCombatMemoryLease(); yield return null; }
            s8Facts["mounted"] = state() == RelationshipState.Mounted;
            var whileMounted = unit.Abilities.GetAbility(horseService.DivineMountCallAbility)?.Data;
            var uses = unit.Resources.GetResourceAmount(horseService.DivineMountCallResource);
            var mountedRefusal = new JObject { ["available"] = whileMounted?.IsAvailableForCast, ["reason"] = whileMounted?.Blueprint.GetComponent<DivineMountCallLogic>()?.GetReason(), ["usesBefore"] = uses };
            s8Facts["callWhileMounted"] = mountedRefusal;
            mountedRefusal["clicked"] = Sprint5Fixture.Click(nativeControls, s8Owner, horseService.DivineMountCallAbility, s8Owner);
            Game.Instance.SelectedAbilityHandler?.DropAbility();
            for (var frame = 0; frame < 30; frame++) yield return null;
            mountedRefusal["usesAfter"] = unit.Resources.GetResourceAmount(horseService.DivineMountCallResource);

            // Third turn: the mount's Smite on its own Swift action, the rider's charge, then the mount's own attack carrying the mark.
            ended = Game.Instance.TurnBasedCombatController.CurrentTurn;
            for (var frame = 0; frame < 600 && !(ended.CanEndTurnAndNoActing() && !Game.Instance.TurnBasedCombatController.WaitingForUI); frame++) yield return null;
            Game.Instance.PauseBind(); s8EndedTurn = ended;
            wait = Sprint8TbTurn(s8Owner);
            while (wait.MoveNext()) yield return null;
            s8Hostile.Descriptor.Alignment.Set(Alignment.NeutralEvil); // disclosed setup: the spawned target is not evil
            s8CharismaPet = pet; s8CharismaOriginal = pet.Stats.Charisma.BaseValue;
            pet.Stats.Charisma.BaseValue = 14; // disclosed setup: a Charisma with a bonus, since a horse's has none to add
            var smite = new JObject { ["usesBefore"] = pet.Descriptor.Resources.GetResourceAmount(horseService.DivineMountSmiteResource), ["riderStandardBefore"] = s8Owner.CombatState.Cooldown.StandardAction, ["charismaBonus"] = pet.Stats.Charisma.Bonus };
            s8Facts["smite"] = smite;
            s8Probe = new Sprint8SmiteProbe(pet);
            Rulebook.Trigger(new RuleAttackWithWeapon(pet, s8Hostile, pet.GetFirstWeapon(), 0));
            smite["additionalDamageUnmarked"] = s8Probe.LastAdditionalDamage;
            smite["attackBonusUnmarked"] = s8Probe.LastAttackBonus;
            // Out of reach, in Preparing: the click is refused. Then a target that moves away between the click and the start.
            var farPreparing = new JObject(); s8Facts["smiteFarPreparing"] = farPreparing;
            var far = S8TbFarClick(farPreparing, pet, s8Hostile);
            while (far.MoveNext()) yield return null;
            var near = FindWalkablePoint(pet.Position, 6f, 1f);
            s8Hostile.Translocate(near, null);
            for (var frame = 0; frame < 20; frame++) { S8Refresh(); yield return null; }
            var moved = new JObject { ["before"] = S8SmiteResources(pet, s8Hostile) }; s8Facts["smiteMovedAway"] = moved;
            var movedFrom = pet.Position; var movedRefusals = S8SmiteRefusals;
            moved["clicked"] = Sprint5Fixture.Click(nativeControls, pet, horseService.DivineMountSmiteAbility, s8Hostile);
            JObject movedPlace;
            if (!MountedChargeFixture.TryPlaceBeyond(pet, s8Hostile, S8SmiteData(pet).GetApproachDistance(s8Hostile), out movedPlace)) throw new InvalidOperationException("No walkable point beyond the Smite's reach.");
            moved["place"] = movedPlace;
            for (var frame = 0; frame < 45; frame++) { S8Refresh(); yield return null; }
            moved["after"] = S8SmiteResources(pet, s8Hostile); moved["refusals"] = S8SmiteRefusals - movedRefusals; moved["mountMoved"] = Vector3.Distance(pet.Position, movedFrom);
            s8Hostile.Translocate(near, null);
            for (var frame = 0; frame < 20; frame++) { S8Refresh(); yield return null; }
            var attempt = Sprint8TbSmiteAttempt(smite, pet, s8Hostile);
            while (attempt.MoveNext()) yield return null;
            ended = Game.Instance.TurnBasedCombatController.CurrentTurn;
            for (var frame = 0; frame < 600 && !(ended.CanEndTurnAndNoActing() && !Game.Instance.TurnBasedCombatController.WaitingForUI); frame++) yield return null;
            smite["canEndTurnAfter"] = ended.CanEndTurnAndNoActing();
            Game.Instance.PauseBind(); s8EndedTurn = ended;
            wait = Sprint8TbTurn(s8Owner);
            while (wait.MoveNext()) yield return null;
            smite["nextTurnReached"] = true;
            smite["markOnNextTurn"] = s8Hostile.Descriptor.HasFact(horseService.DivineMountSmiteBuff);
            smite["usesOnNextTurn"] = pet.Descriptor.Resources.GetResourceAmount(horseService.DivineMountSmiteResource);

            // Fourth turn, Acting: the pair's own move carries on through a refused far request; then an in-reach Smite on a second target.
            s8SecondTargetService = new DiagnosticCombatTargetService(logger, repeatedNativeSequences: true);
            var second = s8SecondTargetService.Spawn(s8Owner, pet, FindWalkablePoint(pet.Position, 6f, 1f), request.RunId + "-s8tb2", true, true);
            if (!s8SecondTargetService.PrepareForPlayerClick(second) || !s8SecondTargetService.QueueBidirectionalCombatMemory(s8Owner, second))
                throw new InvalidOperationException("The second turn-based target's visibility or combat-memory lease failed.");
            second.Descriptor.Alignment.Set(Alignment.NeutralEvil); // disclosed setup
            s8Rules.Watch(second);
            RestController.ApplyRest(pet.Descriptor); // disclosed setup: the native rest returns the one daily use, so the same row can read the Acting path
            var farMoving = new JObject(); s8Facts["smiteFarWhileMoving"] = farMoving;
            JObject movingPlace;
            if (!MountedChargeFixture.TryPlaceBeyond(pet, second, S8SmiteData(pet).GetApproachDistance(second) + 8f, out movingPlace)) throw new InvalidOperationException("No walkable point beyond the Smite's reach.");
            farMoving["place"] = movingPlace;
            for (var frame = 0; frame < 20; frame++) { S8Refresh(); yield return null; }
            var moveFrom = pet.Position;
            SelectionManager.Instance.SelectUnit(s8Owner.View, true, true, false);
            ClickGroundHandler.MoveSelectedUnitsToPoint(FindWalkablePoint(pet.Position, 5f, 1.5f), false);
            for (var frame = 0; frame < 180 && pet.View?.AgentASP?.IsReallyMoving != true; frame++) { S8Refresh(); yield return null; }
            var moveCommand = pet.Commands.Raw.FirstOrDefault(raw => raw is UnitMoveTo);
            farMoving["movingAtClick"] = pet.View?.AgentASP?.IsReallyMoving == true; farMoving["moveCommand"] = moveCommand?.GetType().Name;
            var movingRefusals = S8SmiteRefusals;
            farMoving["before"] = S8SmiteResources(pet, second);
            farMoving["canTarget"] = S8SmiteData(pet).CanTarget(new Kingmaker.Utility.TargetWrapper(second));
            farMoving["clicked"] = Sprint5Fixture.Click(nativeControls, pet, horseService.DivineMountSmiteAbility, second);
            Game.Instance.SelectedAbilityHandler?.DropAbility();
            yield return null;
            farMoving["sameMoveCommand"] = moveCommand != null && pet.Commands.Raw.Contains(moveCommand) && !moveCommand.IsFinished;
            farMoving["movingAfterClick"] = pet.View?.AgentASP?.IsReallyMoving == true;
            farMoving["refusals"] = S8SmiteRefusals - movingRefusals;
            for (var frame = 0; frame < 900 && (pet.View?.AgentASP?.IsReallyMoving == true || !pet.Commands.Empty); frame++) { S8Refresh(); yield return null; }
            farMoving["moveResult"] = moveCommand?.Result.ToString(); farMoving["travelled"] = Vector3.Distance(pet.Position, moveFrom);
            farMoving["after"] = S8SmiteResources(pet, second);
            second.Translocate(FindWalkablePoint(pet.Position, 6f, 1f), null);
            for (var frame = 0; frame < 20; frame++) { S8Refresh(); yield return null; }
            var acting = new JObject { ["before"] = S8SmiteResources(pet, second) }; s8Facts["smiteActing"] = acting;
            attempt = Sprint8TbSmiteAttempt(acting, pet, second);
            while (attempt.MoveNext()) yield return null;
            acting["firstTargetMarkRetired"] = !s8Hostile.Descriptor.HasFact(horseService.DivineMountSmiteBuff);
            s8Hostile = second; // the current mark's target carries the rest of the row; the first target stays with its own service until the end

            // Fifth turn: the charge and the mount's attack with the mark.
            ended = Game.Instance.TurnBasedCombatController.CurrentTurn;
            for (var frame = 0; frame < 600 && !(ended.CanEndTurnAndNoActing() && !Game.Instance.TurnBasedCombatController.WaitingForUI); frame++) { S8Refresh(); yield return null; }
            Game.Instance.PauseBind(); s8EndedTurn = ended;
            wait = Sprint8TbTurn(s8Owner);
            while (wait.MoveNext()) yield return null;

            var charge = new JObject(); s8Facts["charge"] = charge;
            var combatOwner = relationship.World.ForActor(s8Owner).Combat;
            JObject lane;
            if (!MountedChargeFixture.TryPlaceOnLane(pet, s8Owner, s8Hostile, new[] { 12f, 10f, 14f, 9f, 16f, 8f }, out lane)) throw new InvalidOperationException("No clear charge lane exists for the Paladin rider.");
            charge["lane"] = lane;
            var handler = Game.Instance.SelectedAbilityHandler;
            try { charge["clicked"] = Sprint5Fixture.Click(nativeControls, s8Owner, nativeControls.MountedChargeAbility, s8Hostile); }
            finally { handler?.DropAbility(); }
            charge["refusal"] = combatOwner.LastMountedChargeRefusal;
            MountedPairAttackCommand command = null;
            for (var frame = 0; frame < 1800; frame++)
            {
                command = combatOwner.OwnedChargeCommand ?? command;
                if (command != null && command.IsFinished && !combatOwner.HasChargeOwnership) break;
                s8TargetService.ObserveTargetLifeState(); s8TargetService.RefreshBidirectionalCombatMemoryLease();
                yield return null;
            }
            charge["result"] = command?.Result.ToString();
            charge["sequenceLawful"] = command?.ChargeSequenceLawful;
            charge["chargeRules"] = s8Rules.Attacks.Count(row => row.Value<bool?>("charge") == true);
            charge["riderStandard"] = s8Owner.CombatState.Cooldown.StandardAction;
            charge["riderMove"] = s8Owner.CombatState.Cooldown.MoveAction;
            charge["buffCount"] = MountedChargeFixture.NativeBuffCount(s8Owner);
            charge["stillMarked"] = s8Hostile.Descriptor.HasFact(horseService.DivineMountSmiteBuff);

            var attack = new JObject(); s8Facts["mountAttack"] = attack;
            var rulesBefore = s8Rules.Attacks.Count;
            SelectionManager.Instance.SelectUnit(pet.View, true, true, false);
            attack["clicked"] = new ClickUnitHandler().OnClick(s8Hostile.View.gameObject, s8Hostile.Position, 0, false, false);
            for (var frame = 0; frame < 600 && s8Rules.Attacks.Skip(rulesBefore).All(row => row.Value<string>("attacker") != pet.UniqueId); frame++)
            {
                s8TargetService.ObserveTargetLifeState(); s8TargetService.RefreshBidirectionalCombatMemoryLease();
                yield return null;
            }
            attack["rules"] = s8Rules.Attacks.Skip(rulesBefore).Count(row => row.Value<string>("attacker") == pet.UniqueId);
            attack["additionalDamage"] = s8Probe.LastAdditionalDamage;
            for (var frame = 0; frame < 600 && !pet.Commands.Empty; frame++)
            {
                s8TargetService.ObserveTargetLifeState(); s8TargetService.RefreshBidirectionalCombatMemoryLease();
                yield return null;
            }
            if (!pet.Commands.Empty) { pet.Commands.InterruptAll(); yield return null; }
            Rulebook.Trigger(new RuleAttackWithWeapon(pet, s8Hostile, pet.GetFirstWeapon(), 0));
            smite["additionalDamageMarked"] = s8Probe.LastAdditionalDamage;
            smite["attackBonusMarked"] = s8Probe.LastAttackBonus;
            s8Facts["final"] = Sprint8Snapshot(unit);
            s8Probe.Dispose(); s8Probe = null;
            s8Rules.Dispose(); s8Rules = null;
            pet.Stats.Charisma.BaseValue = s8CharismaOriginal; s8CharismaPet = null;

            // The fight ends with the fixture's own target; the owner then dismounts through the ordinary control.
            var destroyed = false;
            for (var frame = 0; frame < 900 && !(destroyed = s8SecondTargetService.DestroyAndVerify()); frame++) yield return null;
            if (destroyed) { s8SecondTargetService.Dispose(); s8SecondTargetService = null; destroyed = false; }
            for (var frame = 0; frame < 900 && !(destroyed = s8TargetService.DestroyAndVerify()); frame++) yield return null;
            s8Facts["targetDestroyed"] = destroyed;
            if (!destroyed) throw new InvalidOperationException("The Sprint 8 turn-based target was not verified destroyed.");
            s8TargetService.Dispose(); s8TargetService = null; s8Hostile = null;
            for (var frame = 0; frame < 1800 && (s8Owner.IsInCombat || Game.Instance.Player.IsInCombat); frame++) { TryEndCastingCleanupTurn(); yield return null; }
            s8Facts["combatCleared"] = !s8Owner.IsInCombat && !Game.Instance.Player.IsInCombat;
            for (var frame = 0; frame < 240 && !Sprint5Fixture.Click(nativeControls, s8Owner, nativeControls.DismountAbility, s8Owner); frame++) yield return null;
            for (var frame = 0; frame < 600 && state() == RelationshipState.Mounted; frame++) yield return null;
            s8Facts["dismounted"] = state() != RelationshipState.Mounted;
        }

        private IEnumerator Sprint8DogCase()
        {
            var build = Sprint8NewBuild(Sprint5Fixture.HalflingRace, Gender.Female, "KMC_S8_Dog");
            var owner = Sprint8MountOwner(build, HorseCompanionBlueprintService.DivineMountRidingDogGuid, 5);
            while (owner.MoveNext()) yield return null;
            var unit = s8Owner.Descriptor;
            var pet = unit.Pet;
            var field = Sprint8Field();
            while (field.MoveNext()) yield return null;
            var commits = new JArray();
            s8Facts["petCommits"] = commits;
            var levels = Sprint8CommitPetLevels(pet, commits);
            while (levels.MoveNext()) yield return null;
            s8Facts["levels"] = build.Levels;
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

            // The native paid dismissal: the owner leaves and takes the companion with it.
            for (var frame = 0; frame < 60; frame++) yield return null;
            var petId = pet.UniqueId;
            var ownerId = s8Owner.UniqueId;
            Game.Instance.Player.DismissCompanion(s8Owner);
            s8Owner = null;
            for (var frame = 0; frame < 60; frame++) yield return null;
            s8Facts["dismissal"] = new JObject { ["ownerInParty"] = Game.Instance.Player.AllCharacters.Any(value => value.UniqueId == ownerId), ["petDestroyed"] = pet.Destroyed,
                ["petMaster"] = pet.Destroyed ? null : pet.Descriptor.Master.Value?.UniqueId, ["orphanPets"] = Game.Instance.State.Units.Count(value => value.UniqueId == petId && !value.Destroyed) };
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
            s8Owner = unit.Unit;
            var field = Sprint8Field();
            while (field.MoveNext()) yield return null;
            original = unit.Pet;
            s8Facts["beforePaladin"] = Sprint8Snapshot(unit);
            var approach = Sprint8LevelTo(build, unit, 8);
            while (approach.MoveNext()) yield return null;
            // The bond chosen in a level that is then abandoned leaves the owner and the real pet as they were.
            Sprint8GrantExperienceFor(unit, 9);
            s8Facts["cancelled"] = build.CancelOwnLevel(unit);
            yield return null;
            s8Facts["afterCancel"] = Sprint8Snapshot(unit);
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

        // The other order: the bonded mount stays the one companion when a companion class comes later.
        private IEnumerator Sprint8MixedReverseCase()
        {
            var build = Sprint8NewBuild(Sprint8HumanRace, Gender.Male, "KMC_S8_Reverse");
            var ranger = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(Chunk7OrdinaryBuildDriver.RangerClassGuid);
            build.ClassFor = next => next <= 5 ? horseService.PaladinClass : ranger;
            build.Choices[HorseCompanionBlueprintService.HuntersBondGuid] = HorseCompanionBlueprintService.RangerSelectionGuid;
            build.Choices[HorseCompanionBlueprintService.RangerSelectionGuid] = HorseCompanionBlueprintService.EmptyCompanionGuid;
            var owner = Sprint8MountOwner(build, HorseCompanionBlueprintService.DivineMountHorseGuid, 5);
            while (owner.MoveNext()) yield return null;
            var unit = build.Hired;
            var field = Sprint8Field();
            while (field.MoveNext()) yield return null;
            s8Facts["beforeRanger"] = Sprint8Snapshot(unit);
            var original = unit.Pet;
            var level = Sprint8LevelTo(build, unit, 9);
            while (level.MoveNext()) yield return null;
            for (var frame = 0; frame < 30; frame++) yield return null;
            s8Facts["levels"] = build.Levels;
            s8Facts["samePet"] = original != null && ReferenceEquals(original, unit.Pet) && !original.Destroyed;
            s8Facts["final"] = Sprint8Snapshot(unit);
            s8Owner = null;
        }

        // The tier the live reconciliation left on the real pet, from the pet's own committed level.
        private JObject Sprint8TierFacts(UnitEntityData owner, string label)
        {
            var facts = Sprint8Snapshot(owner.Descriptor);
            facts["label"] = label;
            return facts;
        }

        private IEnumerator Sprint8Settle()
        {
            for (var frame = 0; frame < 10; frame++) yield return null;
        }

        private IEnumerator Sprint8CommitPetTo(UnitEntityData pet, int hitDice, JArray steps, UnitEntityData owner)
        {
            var petClass = pet.Blueprint.GetComponent<AddClassLevels>().CharacterClass;
            var upgrade = pet.Blueprint.AssetGuid == HorseCompanionBlueprintService.RidingDogUnitGuid ? horseService.RidingDogUpgrade : horseService.HorseUpgrade;
            for (var guard = 0; guard < 20 && pet.Descriptor.Progression.CharacterLevel < hitDice && LevelUpController.CanLevelUp(pet.Descriptor); guard++)
            {
                var record = NativeHorseFixtureAcquisition.CommitPetLevel(pet, petClass, upgrade, logger);
                if (record.Value<bool>("committed") != true) throw new InvalidOperationException("A native pet level did not commit: " + record);
                var settle = Sprint8Settle();
                while (settle.MoveNext()) yield return null;
                steps.Add(Sprint8TierFacts(owner, "committed"));
            }
        }

        private IEnumerator Sprint8TiersCase()
        {
            // A pure Paladin 11 whose pet has not taken its pending levels: the unlock is the owner's, the tier the pet's.
            var build = Sprint8NewBuild(Sprint8HumanRace, Gender.Male, "KMC_S8_Tiers");
            var owner = Sprint8MountOwner(build, HorseCompanionBlueprintService.DivineMountHorseGuid, 7);
            while (owner.MoveNext()) yield return null;
            s8TierOwner = s8Owner;
            var field = Sprint8Field();
            while (field.MoveNext()) yield return null;
            var level = Sprint8LevelTo(build, s8TierOwner.Descriptor, 10);
            while (level.MoveNext()) yield return null;
            var settle = Sprint8Settle();
            while (settle.MoveNext()) yield return null;
            s8Facts["beforeUnlock"] = Sprint8TierFacts(s8TierOwner, "paladin-10");
            level = Sprint8LevelTo(build, s8TierOwner.Descriptor, 11);
            while (level.MoveNext()) yield return null;
            settle = Sprint8Settle();
            while (settle.MoveNext()) yield return null;
            s8Facts["unlockUnlevelled"] = Sprint8TierFacts(s8TierOwner, "paladin-11-pet-pending");
            var pet = s8TierOwner.Descriptor.Pet;
            // Disclosed setup: the day's use is already spent, through the native resource, so a refund would show.
            pet.Descriptor.Resources.Spend(horseService.DivineMountSmiteResource, 1);
            s8Facts["smiteSpentForTransitions"] = pet.Descriptor.Resources.GetResourceAmount(horseService.DivineMountSmiteResource);
            var steps = new JArray();
            s8Facts["petSteps"] = steps;
            var commit = Sprint8CommitPetTo(pet, 20, steps, s8TierOwner);
            while (commit.MoveNext()) yield return null;
            // The owner moves ahead of the pet: Paladin 13 with a pet still below 11 Hit Dice keeps the lower tier.
            level = Sprint8LevelTo(build, s8TierOwner.Descriptor, 13);
            while (level.MoveNext()) yield return null;
            settle = Sprint8Settle();
            while (settle.MoveNext()) yield return null;
            s8Facts["ownerAheadOfPet"] = Sprint8TierFacts(s8TierOwner, "paladin-13-pet-pending");
            commit = Sprint8CommitPetTo(pet, 20, steps, s8TierOwner);
            while (commit.MoveNext()) yield return null;
            level = Sprint8LevelTo(build, s8TierOwner.Descriptor, 15);
            while (level.MoveNext()) yield return null;
            settle = Sprint8Settle();
            while (settle.MoveNext()) yield return null;
            s8Facts["resistanceAtFifteen"] = Sprint8TierFacts(s8TierOwner, "paladin-15");
            s8Facts["petIdentity"] = pet.UniqueId;

            // Mixed classes: above the upper boundary before the unlock, so the higher tier arrives at Paladin 11 itself.
            var mixed = Sprint8NewBuild(Sprint8HumanRace, Gender.Male, "KMC_S8_MixedTiers");
            var ranger = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(Chunk7OrdinaryBuildDriver.RangerClassGuid);
            mixed.ClassFor = next => next <= 7 ? ranger : horseService.PaladinClass;
            mixed.Choices[HorseCompanionBlueprintService.HuntersBondGuid] = HorseCompanionBlueprintService.RangerSelectionGuid;
            mixed.Choices[HorseCompanionBlueprintService.RangerSelectionGuid] = HorseCompanionBlueprintService.DogFeatureGuid;
            mixed.Choices[HorseCompanionBlueprintService.DivineBondSelectionGuid] = HorseCompanionBlueprintService.DivineMountSelectionGuid;
            mixed.Choices[HorseCompanionBlueprintService.DivineMountSelectionGuid] = HorseCompanionBlueprintService.EmptyCompanionGuid;
            var hire = Sprint8Hire(mixed, 7);
            while (hire.MoveNext()) yield return null;
            s8MixedOwner = mixed.Hired.Unit;
            s8Owner = s8MixedOwner;
            field = Sprint8Field();
            while (field.MoveNext()) yield return null;
            level = Sprint8LevelTo(mixed, s8MixedOwner.Descriptor, 17);
            while (level.MoveNext()) yield return null;
            settle = Sprint8Settle();
            while (settle.MoveNext()) yield return null;
            var mixedPet = s8MixedOwner.Descriptor.Pet;
            commit = Sprint8CommitPetTo(mixedPet, 20, new JArray(), s8MixedOwner);
            while (commit.MoveNext()) yield return null;
            s8Facts["mixedBeforeUnlock"] = Sprint8TierFacts(s8MixedOwner, "paladin-10");
            level = Sprint8LevelTo(mixed, s8MixedOwner.Descriptor, 18);
            while (level.MoveNext()) yield return null;
            settle = Sprint8Settle();
            while (settle.MoveNext()) yield return null;
            s8Facts["mixedAtUnlock"] = Sprint8TierFacts(s8MixedOwner, "paladin-11");

            // Removal and return of the bond's celestial fact on the same pet: nothing stays behind, one tier comes back, the smite is untouched.
            var features = s8MixedOwner.Descriptor.Progression.Features;
            mixedPet.Descriptor.Resources.Spend(horseService.DivineMountSmiteResource, 1);
            features.RemoveFact(horseService.DivineMountCelestialLink);
            settle = Sprint8Settle();
            while (settle.MoveNext()) yield return null;
            s8Facts["mixedLinkRemoved"] = Sprint8TierFacts(s8MixedOwner, "link-removed");
            features.AddFeature(horseService.DivineMountCelestialLink);
            settle = Sprint8Settle();
            while (settle.MoveNext()) yield return null;
            s8Facts["mixedLinkRestored"] = Sprint8TierFacts(s8MixedOwner, "link-restored");

            // Replacement: a respec of the high owner brings a new pet at the bottom of the table, a real downgrade of the owner's tier.
            var retrain = Sprint8NewBuild(Sprint8HumanRace, Gender.Male, "KMC_S8_Tiers");
            retrain.Choices[HorseCompanionBlueprintService.DivineBondSelectionGuid] = HorseCompanionBlueprintService.DivineMountSelectionGuid;
            retrain.Choices[HorseCompanionBlueprintService.DivineMountSelectionGuid] = HorseCompanionBlueprintService.DivineMountHorseGuid;
            var respec = Sprint8Respec(retrain, s8TierOwner.Descriptor);
            while (respec.MoveNext()) yield return null;
            for (var frame = 0; frame < 60; frame++) yield return null;
            s8Facts["replaced"] = Sprint8TierFacts(s8TierOwner, "new-pet");
            s8Facts["oldPetDestroyed"] = pet.Destroyed;
            s8Owner = null;
        }

        private JObject Sprint8SmiteRead(UnitEntityData attacker, UnitEntityData victim, string label)
        {
            using (var probe = new Sprint8SmiteProbe(attacker))
            {
                Rulebook.Trigger(new RuleAttackWithWeapon(attacker, victim, attacker.GetFirstWeapon(), 0));
                return new JObject { ["label"] = label, ["attacker"] = attacker.UniqueId, ["target"] = victim.UniqueId, ["hitDice"] = attacker.Descriptor.Progression.CharacterLevel,
                    ["additionalDamage"] = probe.LastAdditionalDamage, ["attackBonus"] = probe.LastAttackBonus,
                    ["uses"] = attacker.Descriptor.Resources.GetResourceAmount(horseService.DivineMountSmiteResource),
                    ["sources"] = new JArray(DivineMountSmiteBonus.MarkSources(victim, horseService.DivineMountSmiteBuff).Select(source => source?.UniqueId)) };
            }
        }

        private JObject Sprint8SmiteTargetable(UnitEntityData caster, UnitEntityData victim) => new JObject { ["caster"] = caster.UniqueId, ["target"] = victim.UniqueId,
            ["canTarget"] = caster.Descriptor.Abilities.GetAbility(horseService.DivineMountSmiteAbility)?.Data?.CanTarget(new Kingmaker.Utility.TargetWrapper(victim)) };

        private IEnumerator Sprint8CastSmite(UnitEntityData caster, UnitEntityData victim)
        {
            for (var frame = 0; frame < 480 && (caster.CombatState.HasCooldownForCommand(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Swift) || !caster.Commands.Empty); frame++)
            { s8TargetService.RefreshBidirectionalCombatMemoryLease(); s8SecondTargetService.RefreshBidirectionalCombatMemoryLease(); yield return null; }
            var ability = caster.Descriptor.Abilities.GetAbility(horseService.DivineMountSmiteAbility)?.Data;
            if (ReferenceEquals(caster, s8MixedOwner.Descriptor.Pet) &&
                (!s8TargetService.NonPairPartyAiLease.BeginDeclaredMemberCommand(caster) || !s8SecondTargetService.NonPairPartyAiLease.BeginDeclaredMemberCommand(caster)))
                throw new InvalidOperationException("The target leases refused the other mount's one declared command.");
            caster.Commands.Run(new UnitUseAbility(ability, new Kingmaker.Utility.TargetWrapper(victim)));
            for (var frame = 0; frame < 240 && !caster.Commands.Empty; frame++)
            { s8TargetService.RefreshBidirectionalCombatMemoryLease(); s8SecondTargetService.RefreshBidirectionalCombatMemoryLease(); yield return null; }
            for (var frame = 0; frame < 10; frame++) yield return null;
        }

        // Two mounts and two targets: each mark belongs to the mount that placed it.
        private IEnumerator Sprint8SmiteCase()
        {
            var a = s8TierOwner.Descriptor.Pet;
            var b = s8MixedOwner.Descriptor.Pet;
            RestController.ApplyRest(a.Descriptor);
            RestController.ApplyRest(b.Descriptor);
            s8TargetService = new DiagnosticCombatTargetService(logger, repeatedNativeSequences: true);
            s8Hostile = s8TargetService.Spawn(s8TierOwner, a, FindWalkablePoint(a.Position, 4.5f, 1f), request.RunId + "-s8x", true, true);
            s8SecondTargetService = new DiagnosticCombatTargetService(logger, repeatedNativeSequences: true);
            var y = s8SecondTargetService.Spawn(s8TierOwner, a, FindWalkablePoint(a.Position, 5f, 1f), request.RunId + "-s8y", true, true);
            if (!s8TargetService.PrepareForPlayerClick(s8Hostile) || !s8TargetService.QueueBidirectionalCombatMemory(s8TierOwner, s8Hostile) ||
                !s8SecondTargetService.PrepareForPlayerClick(y) || !s8SecondTargetService.QueueBidirectionalCombatMemory(s8TierOwner, y))
                throw new InvalidOperationException("The Sprint 8 targets' visibility or combat-memory lease failed.");
            var x = s8Hostile;
            // Disclosed setup: the spawned targets are not evil.
            x.Descriptor.Alignment.Set(Alignment.NeutralEvil);
            y.Descriptor.Alignment.Set(Alignment.NeutralEvil);
            for (var frame = 0; frame < 20; frame++) yield return null;
            var reads = new JArray();
            s8Facts["reads"] = reads;
            s8Facts["mountA"] = a.UniqueId; s8Facts["mountB"] = b.UniqueId; s8Facts["targetX"] = x.UniqueId; s8Facts["targetY"] = y.UniqueId;
            s8Facts["usesAtStart"] = new JObject { ["a"] = a.Descriptor.Resources.GetResourceAmount(horseService.DivineMountSmiteResource), ["b"] = b.Descriptor.Resources.GetResourceAmount(horseService.DivineMountSmiteResource) };
            reads.Add(Sprint8SmiteRead(a, x, "a-x-unmarked"));

            var cast = Sprint8CastSmite(a, x);
            while (cast.MoveNext()) yield return null;
            reads.Add(Sprint8SmiteRead(a, x, "a-x-own-mark"));
            reads.Add(Sprint8SmiteRead(b, x, "b-x-a-mark-only"));
            s8Facts["bMayMarkX"] = Sprint8SmiteTargetable(b, x);
            s8Facts["aMayRemarkX"] = Sprint8SmiteTargetable(a, x);

            cast = Sprint8CastSmite(b, x);
            while (cast.MoveNext()) yield return null;
            reads.Add(Sprint8SmiteRead(a, x, "a-x-both-marks"));
            reads.Add(Sprint8SmiteRead(b, x, "b-x-both-marks"));

            // A rest returns the use and leaves the mark: the policy is a day, not the rest. The next mark retires the first.
            RestController.ApplyRest(a.Descriptor);
            s8Facts["aRested"] = new JObject { ["uses"] = a.Descriptor.Resources.GetResourceAmount(horseService.DivineMountSmiteResource),
                ["sourcesOnX"] = new JArray(DivineMountSmiteBonus.MarkSources(x, horseService.DivineMountSmiteBuff).Select(source => source?.UniqueId)) };
            reads.Add(Sprint8SmiteRead(a, x, "a-x-after-rest"));
            cast = Sprint8CastSmite(a, y);
            while (cast.MoveNext()) yield return null;
            reads.Add(Sprint8SmiteRead(a, x, "a-x-after-changing-target"));
            reads.Add(Sprint8SmiteRead(a, y, "a-y-current"));
            reads.Add(Sprint8SmiteRead(b, x, "b-x-keeps-its-mark"));
            reads.Add(Sprint8SmiteRead(b, y, "b-y-a-mark-only"));

            // Removal: the mount loses its bonus and gets no use back. Expiry: game time alone ends the other.
            var bMark = x.Descriptor.Buffs.Enumerable.First(buff => ReferenceEquals(buff.Blueprint, horseService.DivineMountSmiteBuff) && ReferenceEquals(buff.MaybeContext?.MaybeCaster, b));
            bMark.Remove();
            reads.Add(Sprint8SmiteRead(b, x, "b-x-removed"));
            var aMark = y.Descriptor.Buffs.Enumerable.First(buff => ReferenceEquals(buff.Blueprint, horseService.DivineMountSmiteBuff) && ReferenceEquals(buff.MaybeContext?.MaybeCaster, a));
            s8Facts["aMarkHoursLeft"] = (aMark.EndTime - Game.Instance.TimeController.GameTime).TotalHours;
            Game.Instance.AdvanceGameTime(TimeSpan.FromHours(25));
            for (var frame = 0; frame < 60; frame++) { s8TargetService.RefreshBidirectionalCombatMemoryLease(); s8SecondTargetService.RefreshBidirectionalCombatMemoryLease(); yield return null; }
            reads.Add(Sprint8SmiteRead(a, y, "a-y-expired"));
            s8Facts["aMayMarkYAfterExpiry"] = Sprint8SmiteTargetable(a, y);
            s8Facts["usesAtEnd"] = new JObject { ["a"] = a.Descriptor.Resources.GetResourceAmount(horseService.DivineMountSmiteResource), ["b"] = b.Descriptor.Resources.GetResourceAmount(horseService.DivineMountSmiteResource) };

            for (var frame = 0; frame < 300 && !s8SecondTargetService.DestroyAndVerify(); frame++) yield return null;
            s8SecondTargetService.Dispose(); s8SecondTargetService = null;
            for (var frame = 0; frame < 300 && !s8TargetService.DestroyAndVerify(); frame++) yield return null;
            s8TargetService.Dispose(); s8TargetService = null; s8Hostile = null;
        }

        // An archetype that replaces the bond stays legal and is offered no bond.
        private IEnumerator Sprint8ArchetypeCase()
        {
            var paladin = horseService.PaladinClass;
            var archetype = paladin.Archetypes.FirstOrDefault(value => (value.RemoveFeatures ?? new LevelEntry[0]).Any(entry => entry.Level == 5 && (entry.Features.Contains(horseService.DivineBondSelection) || entry.Features.Contains(horseService.WeaponBondFeature))));
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
