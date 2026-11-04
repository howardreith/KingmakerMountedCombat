using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.Visual.Animation.Kingmaker.Actions;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    // PAL-01. The stock blueprints are only read, apart from the leased level-5 slots: the Paladin's level entry and each
    // archetype's removal of it. Companion features are the bond's own, selected directly, so native respec (which
    // restores source-less features that are not class features) never brings a former mount's feature back.
    internal sealed partial class HorseCompanionBlueprintService
    {
        internal const string PaladinClassGuid = "bfa11238e7ae3544bbeb4d0b92e897ec";
        internal const string PaladinProgressionGuid = "fd325cbba872e5f40b618970678db002";
        internal const string WeaponBondGuid = "1c7cdc1605554954f838d85bbdd22d90";
        internal const string EmptyCompanionGuid = "472091361cf118049a2b4339c4ea836a";
        internal const string HuntersBondGuid = "b705c5184a96a84428eeb35ae2517a14";
        private static readonly string[] WeaponBondUpgradeGuids =
        {
            "c8db0772b7059ec4eabe55b7e0e79824", "d2f45a2034d4f7643ba1a450bc5c4c06", "6d73f49b602e29a43a6faa2ea1e4a425",
            "f17c3ba33bb44d44782cb3851d823011", "b936ee90c070edb46bd76025dc1c5936", "5a64de5435667da4eae2e4c95ec87917"
        };

        internal const string DivineBondSelectionGuid = "6e0f3b7ac1d94f2e8a51c7d0294be163";
        internal const string DivineMountSelectionGuid = "b14c9d27e6a3480fa97e03d5c18f6a42";
        internal const string DivineMountProgressionGuid = "2f8a61d3c7b04e95ba2c4e917d03f8a6";
        internal const string DivineMountHorseGuid = "d97c20e5a84b4f16b3e87a1604cd2f95";
        internal const string DivineMountRidingDogGuid = "47ea1f96b0c24d38a6f5d2c83e917b04";
        internal const string DivineMountCallFeatureGuid = "a36d08f4e1c7492db58f16e2c07b9d31";
        internal const string DivineMountCallAbilityGuid = "0c95e7b2d46f4a18873d9fa6b2c1e048";
        internal const string DivineMountCallResourceGuid = "e82b4c16f90a4d73a1c65be7d3f08291";
        internal const string DivineMountCallUseGuid = "75d1fa93c28e40b6bd47e0a3961c8f2d";
        internal const string DivineMountIntelligenceLinkGuid = "19b6e4d07fa34c82965ec3d1a8f02b67";
        internal const string DivineMountIntelligenceGuid = "c4f207ab96e14d5f8e3b71d2a06c95e8";
        internal const string DivineMountCelestialLinkGuid = "83ad5f1ec07b4692b1d48c6e2f97a035";
        internal const string DivineMountCelestialGuid = "f60e92c7b1d34a58a7f2e39d184bc607";
        internal const string DivineMountGreaterCelestialLinkGuid = "5ab7d30f28e94c61b04f97e6c2a1d38f";
        internal const string DivineMountGreaterCelestialGuid = "2e91c6a4f7d0483bb96a5d03e8f1c742";
        internal const string DivineMountSpellResistanceLinkGuid = "98f3b1d6c4e2407aa5d7206ebf19c3e4";
        internal const string DivineMountSpellResistanceGuid = "d15a07e8b3f94c26b82e6fc4190da7b3";
        internal const string DivineMountSmiteAbilityGuid = "37c8e2f5a09d4b16972d1ae4c6f5b08d";
        internal const string DivineMountSmiteResourceGuid = "ab60d94f13e7425c8bf9d2078e3a1c56";
        internal const string DivineMountSmiteBuffGuid = "6fd2a8c1e50b47d3a94e87b1c3d6f029";

        internal static readonly string[] DivineMountOwnedGuids =
        {
            DivineBondSelectionGuid, DivineMountSelectionGuid, DivineMountProgressionGuid, DivineMountHorseGuid, DivineMountRidingDogGuid,
            DivineMountCallFeatureGuid, DivineMountCallAbilityGuid, DivineMountCallResourceGuid, DivineMountCallUseGuid,
            DivineMountIntelligenceLinkGuid, DivineMountIntelligenceGuid, DivineMountCelestialLinkGuid, DivineMountCelestialGuid,
            DivineMountGreaterCelestialLinkGuid, DivineMountGreaterCelestialGuid, DivineMountSpellResistanceLinkGuid, DivineMountSpellResistanceGuid,
            DivineMountSmiteAbilityGuid, DivineMountSmiteResourceGuid, DivineMountSmiteBuffGuid
        };

        private const int ResourceMaxAmountFieldToken = 0x04005FA3, ResourceMaxFieldToken = 0x04005FA5;

        private sealed class FeatureSlotLease
        {
            internal readonly string Owner;
            private readonly List<BlueprintFeatureBase> slots;
            private readonly int index;
            private readonly BlueprintFeatureBase stock, replacement;

            internal FeatureSlotLease(string owner, List<BlueprintFeatureBase> slots, int index, BlueprintFeatureBase stock, BlueprintFeatureBase replacement)
            {
                Owner = owner; this.slots = slots; this.index = index; this.stock = stock; this.replacement = replacement;
            }

            // Exact slot or nothing: another mod's later edit of the entry is reported and left alone.
            internal bool TryApply(bool desired)
            {
                var from = desired ? stock : replacement;
                var to = desired ? replacement : stock;
                if (index < slots.Count && ReferenceEquals(slots[index], to)) return true;
                if (index >= slots.Count || !ReferenceEquals(slots[index], from)) return false;
                slots[index] = to;
                return true;
            }
        }

        private readonly List<FeatureSlotLease> divineBondLeases = new List<FeatureSlotLease>();
        private readonly List<BlueprintScriptableObject> divineMountOwned = new List<BlueprintScriptableObject>();
        private readonly HashSet<BlueprintFeatureBase> weaponBondUpgrades = new HashSet<BlueprintFeatureBase>();
        private BlueprintFeature weaponBondFeature;
        private BlueprintCharacterClass paladinClass;
        private BlueprintFeatureSelection divineBondSelection, divineMountSelection;
        private BlueprintFeature divineMountHorse, divineMountRidingDog, divineMountIntelligence, divineMountCelestial, divineMountGreaterCelestial, divineMountSpellResistance;
        private BlueprintProgression divineMountProgression;
        private BlueprintAbility divineMountCallAbility, divineMountSmiteAbility;
        private BlueprintAbilityResource divineMountCallResource, divineMountSmiteResource;
        private BlueprintBuff divineMountSmiteBuff;
        private string divineMountFailure;
        private bool divineBondApplied;

        internal BlueprintFeature WeaponBondFeature => weaponBondFeature;
        internal BlueprintCharacterClass PaladinClass => paladinClass;
        internal BlueprintFeatureSelection DivineBondSelection => divineBondSelection;
        internal BlueprintFeatureSelection DivineMountSelection => divineMountSelection;
        internal BlueprintFeature DivineMountHorse => divineMountHorse;
        internal BlueprintFeature DivineMountRidingDog => divineMountRidingDog;
        internal BlueprintFeature DivineMountIntelligence => divineMountIntelligence;
        internal BlueprintFeature DivineMountCelestial => divineMountCelestial;
        internal BlueprintFeature DivineMountGreaterCelestial => divineMountGreaterCelestial;
        internal BlueprintFeature DivineMountSpellResistance => divineMountSpellResistance;
        internal BlueprintProgression DivineMountProgression => divineMountProgression;
        internal BlueprintAbility DivineMountCallAbility => divineMountCallAbility;
        internal BlueprintAbility DivineMountSmiteAbility => divineMountSmiteAbility;
        internal BlueprintAbilityResource DivineMountCallResource => divineMountCallResource;
        internal BlueprintAbilityResource DivineMountSmiteResource => divineMountSmiteResource;
        internal BlueprintBuff DivineMountSmiteBuff => divineMountSmiteBuff;
        internal string DivineMountFailure => divineMountFailure;
        internal bool DivineBondApplied => divineBondApplied;
        internal IEnumerable<BlueprintFeatureBase> WeaponBondUpgrades => weaponBondUpgrades;
        internal int DivineBondLeaseCount => divineBondLeases.Count;
        internal IEnumerable<string> DivineBondLeaseOwners => divineBondLeases.Select(lease => lease.Owner);

        // A bond fault never costs the player the companions or the mounted controls, and leaves the stock class untouched.
        private void RegisterDivineMountReported(LibraryScriptableObject library)
        {
            try { RegisterDivineMount(library); }
            catch (Exception exception)
            {
                divineMountFailure = exception.GetType().Name + ": " + exception.Message;
                logger.Error("Divine Mount was not registered, and the Paladin keeps the stock bond: " + divineMountFailure);
                RemoveDivineMount(library);
            }
            logger.Info("Divine Bond leases: " + string.Join(",", divineBondLeases.Select(lease => lease.Owner).ToArray()) + "; failure=" + (divineMountFailure ?? "none") + ".");
        }

        private void RegisterDivineMount(LibraryScriptableObject library)
        {
            foreach (var guid in DivineMountOwnedGuids) AssertReservedGuidAbsent(library, blueprintList, guid);
            paladinClass = RequireBlueprint<BlueprintCharacterClass>(library, PaladinClassGuid, "PaladinClass");
            var progression = RequireBlueprint<BlueprintProgression>(library, PaladinProgressionGuid, "PaladinProgression");
            weaponBondFeature = RequireBlueprint<BlueprintFeature>(library, WeaponBondGuid, "WeaponBondFeature");
            var empty = RequireBlueprint<BlueprintFeature>(library, EmptyCompanionGuid, "AnimalCompanionEmptyCompanion");
            var huntersBond = RequireBlueprint<BlueprintFeatureSelection>(library, HuntersBondGuid, "HuntersBondSelection");
            weaponBondUpgrades.Clear();
            foreach (var guid in WeaponBondUpgradeGuids)
            {
                BlueprintScriptableObject upgrade;
                if (!library.BlueprintsByAssetId.TryGetValue(guid, out upgrade) || !(upgrade is BlueprintFeature)) throw new InvalidOperationException("The stock weapon-bond upgrade " + guid + " is missing.");
                weaponBondUpgrades.Add((BlueprintFeature)upgrade);
            }

            // A second bond choice from another mod is its author's decision: nothing is overwritten and the class stays as found.
            var foreign = blueprintList.OfType<BlueprintFeatureSelection>().FirstOrDefault(selection => selection.AllFeatures != null && selection.AllFeatures.Contains(weaponBondFeature));
            if (foreign != null) throw new InvalidOperationException("Another selection already offers the weapon bond: " + foreign.name + "/" + foreign.AssetGuid + ".");
            var levelFive = progression.LevelEntries.Where(entry => entry.Level == DivineMountPolicy.BondLevel).ToArray();
            var slots = levelFive.SelectMany(entry => entry.Features.Select((feature, index) => new { entry, feature, index }))
                .Where(slot => ReferenceEquals(slot.feature, weaponBondFeature)).ToArray();
            if (slots.Length != 1) throw new InvalidOperationException("The Paladin's level 5 entries hold " + slots.Length + " weapon bonds, not one.");

            var rank = levelRank;
            var horseOnly = ridingDogFeature == null;
            var icon = horseIcon;

            divineMountCallResource = CreateResource("DivineMountCallResource", DivineMountCallResourceGuid, 1);
            divineMountSmiteResource = CreateResource("DivineMountSmiteResource", DivineMountSmiteResourceGuid, DivineMountPolicy.SmiteUsesPerDay);

            divineMountCallAbility = CreateDivineAbility<DivineMountCallLogic>("DivineMountCallAbility", DivineMountCallAbilityGuid, "KMC.DivineMount.Call.Name", "KMC.DivineMount.Call.Description",
                icon, divineMountCallResource, AbilityRange.Personal, enemies: false, self: true, UnitCommand.CommandType.Standard, fullRound: true);

            divineMountSmiteBuff = CreateOwned<BlueprintBuff>("DivineMountSmiteBuff");
            divineMountSmiteBuff.AssetGuid = DivineMountSmiteBuffGuid;
            divineMountSmiteBuff.Stacking = StackingType.Replace;
            divineMountSmiteBuff.FxOnStart = new PrefabLink();
            divineMountSmiteBuff.FxOnRemove = new PrefabLink();
            divineMountSmiteBuff.ResourceAssetIds = new string[0];
            divineMountSmiteBuff.ComponentsArray = new BlueprintComponent[0];
            SetFeaturePresentation(divineMountSmiteBuff, NewLocalizedString("KMC.DivineMount.Smite.Name"), NewLocalizedString("KMC.DivineMount.Smite.Description"), icon);

            divineMountSmiteAbility = CreateDivineAbility<DivineMountSmiteLogic>("DivineMountSmiteAbility", DivineMountSmiteAbilityGuid, "KMC.DivineMount.Smite.Name", "KMC.DivineMount.Smite.Description",
                icon, divineMountSmiteResource, AbilityRange.Long, enemies: true, self: false, UnitCommand.CommandType.Swift, fullRound: false);
            divineMountSmiteAbility.GetComponent<DivineMountSmiteLogic>().Mark = divineMountSmiteBuff;

            divineMountIntelligence = CreateBondFeature("DivineMountIntelligenceFeature", DivineMountIntelligenceGuid, "KMC.DivineMount.Intelligence", icon, false,
                CreateStatBonus("KMC_DivineMount_Intelligence", StatType.Intelligence, DivineMountPolicy.IntelligenceBonus));
            var smiteBonus = CreateOwned<DivineMountSmiteBonus>("KMC_DivineMount_SmiteBonus");
            smiteBonus.Mark = divineMountSmiteBuff;
            var smiteFacts = CreateOwned<AddFacts>("KMC_DivineMount_SmiteFacts");
            smiteFacts.Facts = new BlueprintUnitFact[] { divineMountSmiteAbility };
            var smiteUses = CreateOwned<AddAbilityResources>("KMC_DivineMount_SmiteUses");
            smiteUses.Resource = divineMountSmiteResource;
            smiteUses.RestoreAmount = true;

            divineMountCelestial = CreateBondFeature("DivineMountCelestialFeature", DivineMountCelestialGuid, "KMC.DivineMount.Celestial", icon, false,
                CelestialComponents("Celestial", DivineMountPolicy.CelestialReduction, DivineMountPolicy.CelestialResistance, smiteFacts, smiteUses, smiteBonus));
            var greaterFacts = CreateOwned<AddFacts>("KMC_DivineMount_GreaterSmiteFacts");
            greaterFacts.Facts = smiteFacts.Facts;
            var greaterUses = CreateOwned<AddAbilityResources>("KMC_DivineMount_GreaterSmiteUses");
            greaterUses.Resource = divineMountSmiteResource;
            greaterUses.RestoreAmount = true;
            var greaterBonus = CreateOwned<DivineMountSmiteBonus>("KMC_DivineMount_GreaterSmiteBonus");
            greaterBonus.Mark = divineMountSmiteBuff;
            divineMountGreaterCelestial = CreateBondFeature("DivineMountGreaterCelestialFeature", DivineMountGreaterCelestialGuid, "KMC.DivineMount.GreaterCelestial", icon, false,
                CelestialComponents("GreaterCelestial", DivineMountPolicy.GreaterCelestialReduction, DivineMountPolicy.GreaterCelestialResistance, greaterFacts, greaterUses, greaterBonus));
            var spellResistance = CreateOwned<DivineMountSpellResistance>("KMC_DivineMount_SpellResistance");
            spellResistance.PaladinClass = paladinClass;
            divineMountSpellResistance = CreateBondFeature("DivineMountSpellResistanceFeature", DivineMountSpellResistanceGuid, "KMC.DivineMount.SpellResistance", icon, false, spellResistance);

            var intelligenceLink = CreateLink("DivineMountIntelligenceLink", DivineMountIntelligenceLinkGuid, "KMC.DivineMount.Intelligence", icon, divineMountIntelligence, null);
            var celestialLink = CreateLink("DivineMountCelestialLink", DivineMountCelestialLinkGuid, "KMC.DivineMount.Celestial", icon, divineMountCelestial, null);
            var greaterLink = CreateLink("DivineMountGreaterCelestialLink", DivineMountGreaterCelestialLinkGuid, "KMC.DivineMount.GreaterCelestial", icon, divineMountGreaterCelestial, divineMountCelestial);
            var resistanceLink = CreateLink("DivineMountSpellResistanceLink", DivineMountSpellResistanceLinkGuid, "KMC.DivineMount.SpellResistance", icon, divineMountSpellResistance, null);

            var callUse = CreateOwned<IncreaseResourceAmount>("KMC_DivineMount_CallUse");
            callUse.Resource = divineMountCallResource;
            callUse.Value = 1;
            var additionalCall = CreateBondFeature("DivineMountCallUseFeature", DivineMountCallUseGuid, "KMC.DivineMount.CallUse", icon, false, callUse);
            additionalCall.Ranks = 3;

            var callFacts = CreateOwned<AddFacts>("KMC_DivineMount_CallFacts");
            callFacts.Facts = new BlueprintUnitFact[] { divineMountCallAbility };
            var callUses = CreateOwned<AddAbilityResources>("KMC_DivineMount_CallUses");
            callUses.Resource = divineMountCallResource;
            callUses.RestoreAmount = true;
            var callFeature = CreateBondFeature("DivineMountCallFeature", DivineMountCallFeatureGuid, "KMC.DivineMount.Call", icon, false, callFacts, callUses);

            divineMountProgression = CreateOwned<BlueprintProgression>("DivineMountProgression");
            divineMountProgression.AssetGuid = DivineMountProgressionGuid;
            CopyFeatureContract(weaponBondFeature, divineMountProgression);
            divineMountProgression.IsClassFeature = true;
            divineMountProgression.HideInUI = true;
            divineMountProgression.HideInCharacterSheetAndLevelUp = true;
            divineMountProgression.Classes = new[] { paladinClass };
            divineMountProgression.Archetypes = new BlueprintArchetype[0];
            divineMountProgression.UIDeterminatorsGroup = new BlueprintFeatureBase[0];
            divineMountProgression.UIGroups = new UIGroup[0];
            divineMountProgression.ComponentsArray = new BlueprintComponent[0];
            SetFeaturePresentation(divineMountProgression, NewLocalizedString("KMC.DivineMount.Name"), NewLocalizedString("KMC.DivineMount.Description"), icon);
            divineMountProgression.LevelEntries = Enumerable.Range(DivineMountPolicy.BondLevel + 1, DivineMountPolicy.MaximumClassLevel - DivineMountPolicy.BondLevel).Select(level =>
            {
                var entry = new LevelEntry { Level = level };
                entry.Features.Add(rank);
                if (DivineMountPolicy.GrantsAdditionalCall(level)) entry.Features.Add(additionalCall);
                if (level == DivineMountPolicy.CelestialLevel) entry.Features.Add(celestialLink);
                if (level == DivineMountPolicy.GreaterCelestialLevel) entry.Features.Add(greaterLink);
                if (level == DivineMountPolicy.SpellResistanceLevel) entry.Features.Add(resistanceLink);
                return entry;
            }).ToArray();

            divineMountHorse = CreateMountChoice("DivineMountHorseFeature", DivineMountHorseGuid, "KMC.DivineMount.Horse", horseIcon, horseUnit, horseUpgrade, false);
            if (!horseOnly) divineMountRidingDog = CreateMountChoice("DivineMountRidingDogFeature", DivineMountRidingDogGuid, "KMC.DivineMount.RidingDog", ridingDogFeature.Icon, ridingDogUnit, ridingDogUpgrade, true);

            divineMountSelection = CreateOwned<BlueprintFeatureSelection>("DivineMountSelection");
            divineMountSelection.AssetGuid = DivineMountSelectionGuid;
            CopyFeatureContract(rangerSelection, divineMountSelection);
            divineMountSelection.IsClassFeature = true;
            divineMountSelection.Group = rangerSelection.Group;
            divineMountSelection.Group2 = rangerSelection.Group2;
            divineMountSelection.Obligatory = false;
            divineMountSelection.Mode = rangerSelection.Mode;
            divineMountSelection.AllFeatures = new[] { empty, divineMountHorse, divineMountRidingDog }.Where(item => item != null).ToArray();
            divineMountSelection.Features = new BlueprintFeature[0];
            SetFeaturePresentation(divineMountSelection, NewLocalizedString("KMC.DivineMount.Name"), NewLocalizedString("KMC.DivineMount.Description"), icon);
            var components = new List<BlueprintComponent>();
            for (var index = 0; index < DivineMountPolicy.BondLevel; index++)
            {
                var rankOnApply = CreateOwned<AddFeatureOnApply>("KMC_DivineMount_Rank" + index);
                rankOnApply.Feature = rank;
                components.Add(rankOnApply);
            }
            var progressionOnApply = CreateOwned<AddFeatureOnApply>("KMC_DivineMount_Progression");
            progressionOnApply.Feature = divineMountProgression;
            var callOnApply = CreateOwned<AddFeatureOnApply>("KMC_DivineMount_Call");
            callOnApply.Feature = callFeature;
            var linkOnApply = CreateOwned<AddFeatureOnApply>("KMC_DivineMount_IntelligenceLink");
            linkOnApply.Feature = intelligenceLink;
            components.Add(progressionOnApply); components.Add(callOnApply); components.Add(linkOnApply);
            divineMountSelection.ComponentsArray = components.ToArray();

            divineBondSelection = CreateOwned<BlueprintFeatureSelection>("DivineBondSelection");
            divineBondSelection.AssetGuid = DivineBondSelectionGuid;
            CopyFeatureContract(huntersBond, divineBondSelection);
            divineBondSelection.IsClassFeature = true;
            divineBondSelection.Group = huntersBond.Group;
            divineBondSelection.Group2 = huntersBond.Group2;
            divineBondSelection.Mode = huntersBond.Mode;
            divineBondSelection.Obligatory = false;
            divineBondSelection.AllFeatures = new BlueprintFeature[] { weaponBondFeature, divineMountSelection };
            divineBondSelection.Features = new BlueprintFeature[0];
            divineBondSelection.ComponentsArray = new BlueprintComponent[0];
            SetFeaturePresentation(divineBondSelection, NewLocalizedString("KMC.DivineBond.Name"), NewLocalizedString("KMC.DivineBond.Description"), weaponBondFeature.Icon);

            foreach (var blueprint in new BlueprintScriptableObject[]
            {
                divineMountCallResource, divineMountSmiteResource, divineMountCallAbility, divineMountSmiteBuff, divineMountSmiteAbility,
                divineMountIntelligence, divineMountCelestial, divineMountGreaterCelestial, divineMountSpellResistance,
                intelligenceLink, celestialLink, greaterLink, resistanceLink, additionalCall, callFeature, divineMountProgression,
                divineMountHorse, divineMountRidingDog, divineMountSelection, divineBondSelection
            }.Where(item => item != null))
            {
                AddExactBlueprintRegistration(library, blueprint);
                divineMountOwned.Add(blueprint);
            }

            divineBondLeases.Add(new FeatureSlotLease(progression.name, slots[0].entry.Features, slots[0].index, weaponBondFeature, divineBondSelection));
            // CanAddArchetype compares removals with the live entry: an archetype that replaces the stock bond must now name the choice.
            foreach (var archetype in blueprintList.OfType<BlueprintArchetype>().Where(value => value.RemoveFeatures != null))
                foreach (var removal in archetype.RemoveFeatures.Where(entry => entry.Level == DivineMountPolicy.BondLevel))
                {
                    var index = removal.Features.IndexOf(weaponBondFeature);
                    if (index >= 0) divineBondLeases.Add(new FeatureSlotLease(archetype.name, removal.Features, index, weaponBondFeature, divineBondSelection));
                }
        }

        private BlueprintComponent[] CelestialComponents(string prefix, int reduction, int resistance, params BlueprintComponent[] others)
        {
            var dr = CreateOwned<AddDamageResistancePhysical>("KMC_DivineMount_" + prefix + "_DR");
            dr.Value = reduction;
            dr.BypassedByAlignment = true;
            dr.Alignment = DamageAlignment.Evil;
            var list = new List<BlueprintComponent> { dr };
            foreach (var type in new[] { DamageEnergyType.Cold, DamageEnergyType.Acid, DamageEnergyType.Electricity })
            {
                var energy = CreateOwned<AddDamageResistanceEnergy>("KMC_DivineMount_" + prefix + "_" + type);
                energy.Type = type;
                energy.Value = resistance;
                list.Add(energy);
            }
            list.AddRange(others);
            return list.ToArray();
        }

        private BlueprintFeature CreateBondFeature(string name, string guid, string key, UnityEngine.Sprite icon, bool shown, params BlueprintComponent[] components)
        {
            var feature = CreateOwned<BlueprintFeature>(name);
            feature.AssetGuid = guid;
            CopyFeatureContract(weaponBondFeature, feature);
            feature.IsClassFeature = true;
            feature.HideInUI = !shown;
            feature.ComponentsArray = components;
            SetFeaturePresentation(feature, NewLocalizedString(key + ".Name"), NewLocalizedString(key + ".Description"), icon);
            return feature;
        }

        // The owner-side fact: it holds the benefit on whichever companion is current, and takes it away with the owner's bond.
        private BlueprintFeature CreateLink(string name, string guid, string key, UnityEngine.Sprite icon, BlueprintFeature benefit, BlueprintFeature replaces)
        {
            var link = CreateOwned<DivineMountPetBenefit>("KMC_" + name);
            link.Feature = benefit;
            link.Replaces = replaces;
            return CreateBondFeature(name, guid, key, icon, true, link);
        }

        private BlueprintFeature CreateMountChoice(string name, string guid, string key, UnityEngine.Sprite icon, BlueprintUnit unit, BlueprintFeature upgrade, bool smallOnly)
        {
            var addPet = CreateOwned<HorseCompanionAddPet>("KMC_" + name + "_AddPet");
            addPet.Pet = unit;
            addPet.LevelRank = levelRank;
            addPet.UpgradeFeature = upgrade;
            addPet.UpgradeLevel = 4;
            var noPet = CreateOwned<PrerequisitePet>("KMC_" + name + "_NoPet");
            noPet.NoCompanion = true;
            var components = new List<BlueprintComponent> { noPet, addPet };
            if (smallOnly) components.Insert(0, CreateOwned<DivineMountRiderSizePrerequisite>("KMC_" + name + "_Small"));
            return CreateBondFeature(name, guid, key, icon, true, components.ToArray());
        }

        private BlueprintAbilityResource CreateResource(string name, string guid, int amount)
        {
            var resource = CreateOwned<BlueprintAbilityResource>(name);
            resource.AssetGuid = guid;
            resource.ComponentsArray = new BlueprintComponent[0];
            var maxAmount = ResolveField(typeof(BlueprintAbilityResource), "m_MaxAmount", ResourceMaxAmountFieldToken, typeof(BlueprintAbilityResource).GetNestedType("Amount", BindingFlags.NonPublic));
            var boxed = maxAmount.GetValue(resource);
            boxed.GetType().GetField("BaseValue").SetValue(boxed, amount);
            boxed.GetType().GetField("Class").SetValue(boxed, new BlueprintCharacterClass[0]);
            boxed.GetType().GetField("Archetypes").SetValue(boxed, new BlueprintArchetype[0]);
            boxed.GetType().GetField("ClassDiv").SetValue(boxed, new BlueprintCharacterClass[0]);
            maxAmount.SetValue(resource, boxed);
            ResolveField(typeof(BlueprintAbilityResource), "m_Max", ResourceMaxFieldToken, typeof(int)).SetValue(resource, 20);
            return resource;
        }

        private BlueprintAbility CreateDivineAbility<TLogic>(string name, string guid, string nameKey, string descriptionKey, UnityEngine.Sprite icon, BlueprintAbilityResource resource,
            AbilityRange range, bool enemies, bool self, UnitCommand.CommandType action, bool fullRound) where TLogic : BlueprintComponent
        {
            var ability = CreateOwned<BlueprintAbility>(name);
            ability.AssetGuid = guid;
            ability.Type = AbilityType.SpellLike;
            ability.Range = range;
            ability.CanTargetPoint = false;
            ability.CanTargetEnemies = enemies;
            ability.CanTargetFriends = false;
            ability.CanTargetSelf = self;
            ability.SpellResistance = false;
            ability.LocalizedDuration = NewLocalizedString("KMC.Native.Empty");
            ability.LocalizedSavingThrow = NewLocalizedString("KMC.Native.Empty");
            ability.ActionType = action;
            if (fullRound) ability.SetIsFullRoundAction(true);
            ability.Animation = UnitAnimationActionCastSpell.CastAnimationStyle.Immediate;
            ability.HasFastAnimation = true;
            ability.ResourceAssetIds = new string[0];
            SetFeaturePresentation(ability, NewLocalizedString(nameKey), NewLocalizedString(descriptionKey), icon);
            var logic = CreateOwned<TLogic>("KMC_" + name + "_Logic");
            var uses = CreateOwned<AbilityResourceLogic>("KMC_" + name + "_Uses");
            uses.RequiredResource = resource;
            uses.IsSpendResource = true;
            uses.Amount = 1;
            ability.ComponentsArray = new BlueprintComponent[] { logic, uses };
            return ability;
        }

        private void RemoveDivineMount(LibraryScriptableObject library)
        {
            ApplyDivineBondReported(false);
            foreach (var blueprint in divineMountOwned) RemoveExact(library, blueprintList, blueprint.AssetGuid, blueprint);
            divineMountOwned.Clear();
            divineBondLeases.Clear();
            weaponBondUpgrades.Clear();
            divineBondSelection = null; divineMountSelection = null; divineMountProgression = null;
            divineMountHorse = null; divineMountRidingDog = null; divineMountIntelligence = null;
            divineMountCelestial = null; divineMountGreaterCelestial = null; divineMountSpellResistance = null;
            divineMountCallAbility = null; divineMountSmiteAbility = null; divineMountCallResource = null;
            divineMountSmiteResource = null; divineMountSmiteBuff = null;
        }

        // Every slot is attempted. Drift is reported and leaves that entry as found; the bond counts as applied only when
        // the Paladin's own entry and every archetype removal agree.
        private void ApplyDivineBondReported(bool desired)
        {
            if (divineBondLeases.Count == 0) { divineBondApplied = false; return; }
            var refused = divineBondLeases.Where(lease => !lease.TryApply(desired)).Select(lease => lease.Owner).ToArray();
            divineBondApplied = desired && refused.Length == 0;
            if (refused.Length == 0) return;
            divineMountFailure = "A Divine Bond level-5 slot changed outside KMC; no overwrite was attempted: " + string.Join(",", refused);
            logger.Error(divineMountFailure);
        }

        // The native level-up's one choke point for class level entries. Nothing else about the level is touched.
        internal IList<BlueprintFeatureBase> FilterLevelEntryFeatures(UnitDescriptor unit, IList<BlueprintFeatureBase> features)
        {
            if (!divineBondApplied || unit == null || features == null) return features;
            var substitute = DivineMountPolicy.SubstitutesStockBond(unit.IsPlayerFaction) && features.Contains(divineBondSelection);
            var hasMount = unit.Progression.Features.HasFact(divineMountSelection);
            if (!substitute && !hasMount) return features;
            var result = new List<BlueprintFeatureBase>(features.Count);
            foreach (var feature in features)
            {
                if (substitute && ReferenceEquals(feature, divineBondSelection)) { result.Add(weaponBondFeature); continue; }
                if (DivineMountPolicy.KeepsLevelEntryFeature(weaponBondUpgrades.Contains(feature), hasMount)) result.Add(feature);
            }
            return result;
        }
    }
}
