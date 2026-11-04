using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.UI.SettingsUI;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // PAL-01's save route. The second rider becomes a Divine Mount paladin through native levels, and a remote companion is
    // built on the stock class the way a save from before this bond holds one. The cold load reads both and levels the old one.
    internal sealed partial class Chunk7PersistenceScenario
    {
        internal const string PaladinBondCaseName = "paladin-bond-exploration-manual";
        private const string PaladinWeaponName = "KMC_S8_OldWeapon";
        private bool PaladinBondCase => request.PersistenceCase == PaladinBondCaseName;

        private bool AcquirePaladinBond(JObject record)
        {
            // Disclosed setup: the lawful good alignment the class requires, and enough experience. The levels are native.
            secondRider.Descriptor.Alignment.Set(Alignment.LawfulGood);
            var build = new Sprint8Build(logger) { ClassFor = next => horses.PaladinClass };
            build.Choices[HorseCompanionBlueprintService.DivineBondSelectionGuid] = HorseCompanionBlueprintService.DivineMountSelectionGuid;
            build.Choices[HorseCompanionBlueprintService.DivineMountSelectionGuid] = HorseCompanionBlueprintService.DivineMountHorseGuid;
            var progression = secondRider.Descriptor.Progression;
            for (var level = 0; level < 5; level++)
            {
                var needed = BlueprintRoot.Instance.Progression.XPTable.GetBonus(progression.CharacterLevel + 1);
                if (progression.Experience < needed) progression.AdvanceExperienceTo(needed, false);
                build.CommitOwnLevel(secondRider.Descriptor);
            }
            record["levels"] = build.Levels;
            var pet = secondRider.Descriptor.Pet;
            if (pet == null) return false;
            var commits = new JArray(); record["petCommits"] = commits;
            var petClass = pet.Blueprint.GetComponent<AddClassLevels>().CharacterClass;
            for (var guard = 0; guard < 20 && Kingmaker.UnitLogic.Class.LevelUp.LevelUpController.CanLevelUp(pet.Descriptor); guard++)
                commits.Add(NativeHorseFixtureAcquisition.CommitPetLevel(pet, petClass, horses.HorseUpgrade, logger));
            record["owner"] = Sprint8Facts.Owner(horses, secondRider.Descriptor);
            BuildOldWeaponCompanion(record);
            return record["owner"].Value<int>("rank") == 5 && record["owner"].Value<int>("addPetFacts") == 1;
        }

        private void BuildOldWeaponCompanion(JObject record)
        {
            horses.SetSelectionEnabled(false);
            var original = SettingsRoot.Instance.AutoLevelup.CurrentValue;
            try
            {
                SettingsRoot.Instance.AutoLevelup.CurrentValue = AutolevelupState.Off;
                var build = new Sprint8Build(logger) { ClassFor = next => horses.PaladinClass, Name = PaladinWeaponName, Gender = Kingmaker.Blueprints.Gender.Male,
                    Race = BlueprintRoot.Instance.Progression.CharacterRaces.Single(value => value.name == "HumanRace") };
                build.StartHire();
                for (var guard = 0; guard < 24 && build.CommitBuilderLevel(); guard++) { }
                record["oldWeapon"] = Sprint8Facts.Owner(horses, build.Hired);
                record["oldWeaponStockEntry"] = new JArray(horses.PaladinClass.Progression.LevelEntries.Where(entry => entry.Level == 5).SelectMany(entry => entry.Features).Select(feature => feature.name));
            }
            finally
            {
                SettingsRoot.Instance.AutoLevelup.CurrentValue = original;
                horses.SetSelectionEnabled(true);
            }
        }

        // The loaded world, as the save holds it, then one more ordinary level for the old character.
        private void RecordPaladinBondCold()
        {
            var record = new JObject(); evidence["paladinBondCold"] = record;
            record["owner"] = Sprint8Facts.Owner(horses, secondRider.Descriptor);
            record["restoredProfile"] = saved.Pairs.Single(pair => pair.Rider.Id == secondRider.UniqueId).ProfileId;
            record["resolvedProfile"] = SupportedMountedProfiles.Resolve(secondRider, secondMount)?.Id;
            record["ownerPetIsPairMount"] = ReferenceEquals(secondRider.Descriptor.Pet, secondMount);
            var old = Game.Instance.Player.AllCharacters.FirstOrDefault(unit => unit.CharacterName == PaladinWeaponName);
            record["oldWeaponFound"] = old != null;
            if (old == null) return;
            record["oldWeapon"] = Sprint8Facts.Owner(horses, old.Descriptor);
            var progression = old.Descriptor.Progression;
            var needed = BlueprintRoot.Instance.Progression.XPTable.GetBonus(progression.CharacterLevel + 1);
            if (progression.Experience < needed) progression.AdvanceExperienceTo(needed, false);
            var build = new Sprint8Build(logger) { ClassFor = next => horses.PaladinClass };
            var level = build.CommitOwnLevel(old.Descriptor);
            record["oldWeaponNextLevel"] = level;
            record["oldWeaponAfter"] = Sprint8Facts.Owner(horses, old.Descriptor);
        }
    }
}
