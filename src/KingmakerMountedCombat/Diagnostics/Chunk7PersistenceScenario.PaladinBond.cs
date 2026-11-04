using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.UI.SettingsUI;
using Kingmaker.UnitLogic;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // PAL-01's save route. The second rider becomes a Divine Mount paladin through native levels, and a remote companion is
    // built on the stock class the way a save from before this bond holds one. The cold load reads both and levels the old one.
    internal sealed partial class Chunk7PersistenceScenario
    {
        internal const string PaladinBondCaseName = "paladin-bond-exploration-manual";
        private const string PaladinWeaponName = "KMC_S8_OldWeapon", PaladinHighName = "KMC_S8_HighOwner";
        private UnitEntityData paladinHighOwner, paladinSmiteTarget;
        private int paladinHighStep;
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
            BuildHighOwner(record);
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

        // A party member past every feature boundary, whose pet's celestial facts and spell resistance the save must carry.
        private void BuildHighOwner(JObject record)
        {
            var original = SettingsRoot.Instance.AutoLevelup.CurrentValue;
            try
            {
                SettingsRoot.Instance.AutoLevelup.CurrentValue = AutolevelupState.Off;
                var build = new Sprint8Build(logger) { ClassFor = next => horses.PaladinClass, Name = PaladinHighName, Gender = Kingmaker.Blueprints.Gender.Male,
                    Race = BlueprintRoot.Instance.Progression.CharacterRaces.Single(value => value.name == "HumanRace") };
                build.Choices[HorseCompanionBlueprintService.DivineBondSelectionGuid] = HorseCompanionBlueprintService.DivineMountSelectionGuid;
                build.Choices[HorseCompanionBlueprintService.DivineMountSelectionGuid] = HorseCompanionBlueprintService.DivineMountHorseGuid;
                build.StartHire();
                for (var guard = 0; guard < 24 && build.CommitBuilderLevel(); guard++) { }
                var progression = build.Hired.Progression;
                while (progression.CharacterLevel < 15)
                {
                    var needed = BlueprintRoot.Instance.Progression.XPTable.GetBonus(progression.CharacterLevel + 1);
                    if (progression.Experience < needed) progression.AdvanceExperienceTo(needed, false);
                    build.CommitOwnLevel(build.Hired);
                }
                paladinHighOwner = build.Hired.Unit;
                Sprint8Build.BringToField(paladinHighOwner, firstRider.Position + Vector3.left * 4f);
            }
            finally { SettingsRoot.Instance.AutoLevelup.CurrentValue = original; }
        }

        // The hired owner's pet arrives after it joins, and the live sync gives it the bond's benefits.
        private bool PaladinHighOwnerReady()
        {
            var pet = paladinHighOwner?.Descriptor?.Pet;
            if (pet?.View?.AgentASP == null || !pet.IsInState || !pet.Descriptor.Progression.Features.HasFact(horses.DivineMountSpellResistance)) return false;
            var ability = pet.Descriptor.Abilities.GetAbility(horses.DivineMountSmiteAbility)?.Data;
            switch (paladinHighStep)
            {
                case 0:
                    var petClass = pet.Blueprint.GetComponent<AddClassLevels>().CharacterClass;
                    for (var guard = 0; guard < 20 && Kingmaker.UnitLogic.Class.LevelUp.LevelUpController.CanLevelUp(pet.Descriptor); guard++)
                        NativeHorseFixtureAcquisition.CommitPetLevel(pet, petClass, horses.HorseUpgrade, logger);
                    // Disclosed setup: a distant idle stock unit made evil, holding a real mark across the save. It is not hostile, so the save stays out of combat.
                    var stock = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(HorseCompanionBlueprintService.DogFeatureGuid).GetComponent<Kingmaker.UnitLogic.FactLogic.AddPet>().Pet;
                    paladinSmiteTarget = Game.Instance.EntityCreator.SpawnUnit(stock, firstRider.Position + Vector3.left * 8f, Quaternion.identity, Game.Instance.State.LoadedAreaState.MainState);
                    paladinHighStep = 1;
                    return false;
                case 1:
                    if (paladinSmiteTarget.View == null) return false;
                    paladinSmiteTarget.Descriptor.Alignment.Set(Alignment.NeutralEvil);
                    ability.Spend();
                    Kingmaker.RuleSystem.Rulebook.Trigger(new Kingmaker.RuleSystem.Rules.Abilities.RuleCastSpell(ability, new Kingmaker.Utility.TargetWrapper(paladinSmiteTarget)));
                    paladinHighStep = 2;
                    return false;
                case 2:
                    if (!paladinSmiteTarget.Descriptor.HasFact(horses.DivineMountSmiteBuff)) return false;
                    paladinHighStep = 3;
                    break;
            }
            var record = (JObject)evidence["acquisition"];
            record["highOwner"] = Sprint8Facts.Owner(horses, paladinHighOwner.Descriptor);
            record["markSources"] = new JArray(DivineMountSmiteBonus.MarkSources(paladinSmiteTarget, horses.DivineMountSmiteBuff).Select(source => source?.UniqueId));
            record["markTarget"] = paladinSmiteTarget.UniqueId;
            return true;
        }

        // The loaded world, as the save holds it, then one more ordinary level for the old character.
        private void RecordPaladinBondCold()
        {
            var record = new JObject(); evidence["paladinBondCold"] = record;
            record["owner"] = Sprint8Facts.Owner(horses, secondRider.Descriptor);
            record["restoredProfile"] = saved.Pairs.Single(pair => pair.Rider.Id == secondRider.UniqueId).ProfileId;
            record["resolvedProfile"] = SupportedMountedProfiles.Resolve(secondRider, secondMount)?.Id;
            record["ownerPetIsPairMount"] = ReferenceEquals(secondRider.Descriptor.Pet, secondMount);
            var high = Game.Instance.Player.AllCharacters.FirstOrDefault(unit => unit.CharacterName == PaladinHighName);
            record["highOwner"] = high == null ? null : Sprint8Facts.Owner(horses, high.Descriptor);
            var highPet = high?.Descriptor?.Pet;
            if (highPet != null)
            {
                // The mark as the save holds it: its source is the loaded mount, on the mount's current target, and the bonus reaches the real rule.
                var marked = Game.Instance.State.Units.Where(unit => unit.Descriptor.HasFact(horses.DivineMountSmiteBuff)).ToArray();
                var smite = new JObject { ["markedUnits"] = new JArray(marked.Select(unit => unit.UniqueId)), ["petHitDice"] = highPet.Descriptor.Progression.CharacterLevel,
                    ["sources"] = new JArray(marked.SelectMany(unit => DivineMountSmiteBonus.MarkSources(unit, horses.DivineMountSmiteBuff)).Select(source => source?.UniqueId)) };
                record["smite"] = smite;
                if (marked.Length == 1)
                {
                    using (var probe = new Sprint8SmiteProbe(highPet))
                    {
                        Kingmaker.RuleSystem.Rulebook.Trigger(new Kingmaker.RuleSystem.Rules.RuleAttackWithWeapon(highPet, marked[0], highPet.GetFirstWeapon(), 0));
                        smite["additionalDamage"] = probe.LastAdditionalDamage;
                    }
                }
            }
            var old = Game.Instance.Player.AllCharacters.FirstOrDefault(unit => unit.CharacterName == PaladinWeaponName);
            record["oldWeaponFound"] = old != null;
            if (old == null) return;
            record["oldWeapon"] = Sprint8Facts.Owner(horses, old.Descriptor);
            var progression = old.Descriptor.Progression;
            var needed = BlueprintRoot.Instance.Progression.XPTable.GetBonus(progression.CharacterLevel + 1);
            if (progression.Experience < needed) progression.AdvanceExperienceTo(needed, false);
            // A loaded remote companion has no view until it joins a scene, and the native commit refreshes its equipment there.
            if (old.View == null) old.AttachToViewOnLoad(null);
            var build = new Sprint8Build(logger) { ClassFor = next => horses.PaladinClass };
            var level = build.CommitOwnLevel(old.Descriptor);
            record["oldWeaponNextLevel"] = level;
            record["oldWeaponAfter"] = Sprint8Facts.Owner(horses, old.Descriptor);
        }
    }
}
