using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Localization;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.FactLogic;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    internal enum HorseCompanionBlueprintState
    {
        Pending,
        Registered,
        Faulted,
        Disposed
    }

    internal sealed class HorseCompanionBlueprintSnapshot
    {
        public HorseCompanionBlueprintState State { get; set; }
        public string Failure { get; set; }
        public string UnitGuid { get; set; }
        public string FeatureGuid { get; set; }
        public string UpgradeGuid { get; set; }
        public string RangerSelectionGuid { get; set; }
        public int RangerOriginalOptionCount { get; set; }
        public int RangerCurrentOptionCount { get; set; }
        public bool RangerAppendOwned { get; set; }
        public bool RangerSelectionDesired { get; set; }
        public string NativeViewAssetId { get; set; }
        public string CompanionClassGuid { get; set; }
        public int InitialClassLevels { get; set; }
        public int StockMammothInitialClassLevels { get; set; }
        public int StockDogInitialClassLevels { get; set; }
        public string LevelRankGuid { get; set; }
        public int UpgradeLevel { get; set; }
        public string BiteGuid { get; set; }
        public string BiteName { get; set; }
        public string HoofGuid { get; set; }
        public string HoofName { get; set; }
        public int NaturalAttackCount { get; set; }
        public int UnitComponentCount { get; set; }
        public int UpgradeComponentCount { get; set; }
        public int Strength { get; set; }
        public int Dexterity { get; set; }
        public int Constitution { get; set; }
        public int Intelligence { get; set; }
        public int Wisdom { get; set; }
        public int Charisma { get; set; }
        public int SpeedFeet { get; set; }
        public string Size { get; set; }
    }

    internal sealed class HorseCompanionBlueprintService : IDisposable
    {
        internal const string UnitGuid = "4016c7db400ab721ff125aef9e65e202";
        internal const string FeatureGuid = "7db7c50677e39f09feef56f3831fc723";
        internal const string UpgradeGuid = "98e651899e6278d938de77af1d69bd32";
        internal const string NativeHorseGuid = "9e9e75c484e68734487e609714565202";
        internal const string MammothUnitGuid = "e7aa96d15a45238438ae4cfb476f6bb9";
        internal const string MammothFeatureGuid = "6adc3aab7cde56b40aa189a797254271";
        internal const string MammothUpgradeGuid = "6a23d16a4476af644af89d91f9f96790";
        internal const string DogFeatureGuid = "f894e003d31461f48a02f5caec4e3359";
        internal const string RangerSelectionGuid = "ee63330662126374e8785cc901941ac7";
        internal const string LevelRankGuid = "1670990255e4fe948a863bafd5dbda5d";
        internal const string HoofGuid = "b0e472a49ff2a294f93faa3ab757a4a5";

        private const int DisplayNameFieldToken = 0x04006955;
        private const int DescriptionFieldToken = 0x04006956;
        private const int IconFieldToken = 0x04006957;
        private const int PortraitFieldToken = 0x04005FFD;
        private const int LocalizedStringKeyFieldToken = 0x04004C56;

        private static readonly FieldInfo DisplayNameField = ResolveField(typeof(BlueprintUnitFact), "m_DisplayName", DisplayNameFieldToken, typeof(LocalizedString));
        private static readonly FieldInfo DescriptionField = ResolveField(typeof(BlueprintUnitFact), "m_Description", DescriptionFieldToken, typeof(LocalizedString));
        private static readonly FieldInfo IconField = ResolveField(typeof(BlueprintUnitFact), "m_Icon", IconFieldToken, typeof(Sprite));
        private static readonly FieldInfo PortraitField = ResolveField(typeof(BlueprintUnit), "m_Portrait", PortraitFieldToken, typeof(BlueprintPortrait));
        private static readonly FieldInfo LocalizedStringKeyField = ResolveField(typeof(LocalizedString), "m_Key", LocalizedStringKeyFieldToken, typeof(string));

        private readonly IModLogger logger;
        private readonly Dictionary<string, string> localization = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "KMC.Horse.Name", "Horse" },
            { "KMC.Horse.Description", "A Large native Kingmaker horse animal companion. It is fully controllable while unmounted and can be used by KMC's private-alpha mounted profile." },
            { "KMC.Horse.Upgrade.Name", "Horse Animal Companion Advancement" },
            { "KMC.Horse.Upgrade.Description", "At animal-companion rank 4, the horse gains +2 Strength and +2 Constitution." }
        };
        private readonly List<UnityEngine.Object> ownedObjects = new List<UnityEngine.Object>();
        private HorseCompanionBlueprintState state;
        private string failure;
        private bool selectionDesired;
        private BlueprintUnit horseUnit;
        private BlueprintFeature horseFeature;
        private BlueprintFeature horseUpgrade;
        private BlueprintFeatureSelection rangerSelection;
        private ExactAppendOnlyArrayLease<BlueprintFeature> selectionLease;
        private string biteGuid;
        private string biteName;
        private string hoofName;
        private BlueprintCharacterClass companionClass;
        private BlueprintFeature levelRank;
        private int stockMammothInitialClassLevels;
        private int stockDogInitialClassLevels;
        private int upgradeLevel;
        private bool disposed;

        public HorseCompanionBlueprintService(IModLogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            state = HorseCompanionBlueprintState.Pending;
        }

        public HorseCompanionBlueprintState State => state;

        public string Failure => failure;

        public BlueprintUnit HorseUnit => horseUnit;

        public BlueprintFeature HorseFeature => horseFeature;

        public BlueprintFeature HorseUpgrade => horseUpgrade;

        public BlueprintFeature LevelRank => levelRank;

        public void Update()
        {
            if (disposed) { return; }
            if (state == HorseCompanionBlueprintState.Registered)
            {
                SynchronizeLiveHorseCompanionProgression();
                return;
            }
            if (state != HorseCompanionBlueprintState.Pending) { return; }
            LibraryScriptableObject library = null;
            try
            {
                library = ResourcesLibrary.LibraryObject;
                if (library == null || library.BlueprintsByAssetId == null || library.BlueprintsByAssetId.Count == 0)
                {
                    return;
                }
                if (!EnsureLocalization()) { return; }

                Register(library);
                state = HorseCompanionBlueprintState.Registered;
                logger.Info("Registered the original KMC horse companion blueprint trio and exact Ranger append transaction.");
            }
            catch (Exception exception)
            {
                failure = exception.GetType().Name + ": " + exception.Message;
                try
                {
                    RollBackPartialRegistration(library);
                }
                catch (Exception rollbackException)
                {
                    failure += " | rollback " + rollbackException.GetType().Name + ": " + rollbackException.Message;
                    logger.Exception("Horse companion blueprint registration rollback", rollbackException);
                }
                state = HorseCompanionBlueprintState.Faulted;
                logger.Exception("Horse companion blueprint registration", exception);
            }
        }

        private void SynchronizeLiveHorseCompanionProgression()
        {
            var player = Game.Instance?.Player;
            if (player?.PartyCharacters == null || horseFeature == null) { return; }

            foreach (var reference in player.PartyCharacters)
            {
                var owner = reference.Value;
                var fact = owner?.Descriptor?.GetFact(horseFeature);
                var addPet = fact?.Get<HorseCompanionAddPet>();
                if (addPet == null || !addPet.DeferredProgressionPending) { continue; }

                try
                {
                    addPet.TryDeferredProgressionSynchronization();
                    if (addPet.TryMarkDeferredProgressionFailureReported())
                    {
                        logger.Error(
                            "The exact KMC horse remained below its native AddPet rank mapping after the one bounded deferred retry: owner=" +
                            owner.UniqueId + ", expected=" + addPet.ExpectedCharacterLevel +
                            ", observed=" + addPet.DeferredCharacterLevelAfter + ".");
                    }
                }
                catch (Exception exception)
                {
                    if (addPet.TryMarkDeferredProgressionFailureReported())
                    {
                        logger.Exception("Deferred exact KMC horse AddPet synchronization", exception);
                    }
                }
            }
        }

        public bool SetSelectionEnabled(bool enabled)
        {
            if (disposed) { return false; }
            selectionDesired = enabled;
            if (state == HorseCompanionBlueprintState.Pending)
            {
                Update();
                return state != HorseCompanionBlueprintState.Faulted;
            }
            if (state != HorseCompanionBlueprintState.Registered)
            {
                return false;
            }

            string error;
            if (!ApplySelectionDesired(out error))
            {
                failure = error;
                logger.Error(error);
                return false;
            }
            return true;
        }

        public HorseCompanionBlueprintSnapshot CaptureSnapshot()
        {
            var classLevels = horseUnit?.GetComponent<AddClassLevels>();
            var body = horseUnit?.Body;
            return new HorseCompanionBlueprintSnapshot
            {
                State = state,
                Failure = failure,
                UnitGuid = horseUnit?.AssetGuid,
                FeatureGuid = horseFeature?.AssetGuid,
                UpgradeGuid = horseUpgrade?.AssetGuid,
                RangerSelectionGuid = rangerSelection?.AssetGuid,
                RangerOriginalOptionCount = selectionLease?.OriginalCount ?? 0,
                RangerCurrentOptionCount = rangerSelection?.AllFeatures?.Length ?? 0,
                RangerAppendOwned = selectionLease != null && selectionLease.MatchesAppended(rangerSelection?.AllFeatures),
                RangerSelectionDesired = selectionDesired,
                NativeViewAssetId = horseUnit?.Prefab?.AssetId,
                CompanionClassGuid = classLevels?.CharacterClass?.AssetGuid,
                InitialClassLevels = classLevels?.Levels ?? -1,
                StockMammothInitialClassLevels = stockMammothInitialClassLevels,
                StockDogInitialClassLevels = stockDogInitialClassLevels,
                LevelRankGuid = levelRank?.AssetGuid,
                UpgradeLevel = upgradeLevel,
                BiteGuid = biteGuid,
                BiteName = biteName,
                HoofGuid = body?.AdditionalLimbs?.FirstOrDefault()?.AssetGuid,
                HoofName = hoofName,
                NaturalAttackCount = (body?.PrimaryHand == null ? 0 : 1) + (body?.AdditionalLimbs?.Length ?? 0),
                UnitComponentCount = horseUnit?.ComponentsArray?.Length ?? 0,
                UpgradeComponentCount = horseUpgrade?.ComponentsArray?.Length ?? 0,
                Strength = horseUnit?.Strength ?? 0,
                Dexterity = horseUnit?.Dexterity ?? 0,
                Constitution = horseUnit?.Constitution ?? 0,
                Intelligence = horseUnit?.Intelligence ?? 0,
                Wisdom = horseUnit?.Wisdom ?? 0,
                Charisma = horseUnit?.Charisma ?? 0,
                SpeedFeet = horseUnit == null ? 0 : horseUnit.Speed.Value,
                Size = horseUnit?.Size.ToString()
            };
        }

        public void Dispose()
        {
            if (disposed) { return; }
            if (state == HorseCompanionBlueprintState.Registered && rangerSelection != null && selectionLease != null)
            {
                BlueprintFeature[] restored;
                string error;
                if (!selectionLease.TryRestore(rangerSelection.AllFeatures, out restored, out error))
                {
                    throw new InvalidOperationException(error);
                }
                rangerSelection.AllFeatures = restored;
            }

            // Blueprint facts may already be serialized into the active Working
            // process. Keep the exact KMC definitions in the in-memory library
            // until process exit; only the selectable Ranger surface is leased.
            disposed = true;
            state = HorseCompanionBlueprintState.Disposed;
        }

        private void Register(LibraryScriptableObject library)
        {
            AssertReservedGuidAbsent(library, UnitGuid);
            AssertReservedGuidAbsent(library, FeatureGuid);
            AssertReservedGuidAbsent(library, UpgradeGuid);

            var nativeHorse = RequireBlueprint<BlueprintUnit>(library, NativeHorseGuid, "CR1_HorseRiding");
            var mammothUnit = RequireBlueprint<BlueprintUnit>(library, MammothUnitGuid, "AnimalCompanionUnitMammoth");
            var mammothFeature = RequireBlueprint<BlueprintFeature>(library, MammothFeatureGuid, "AnimalCompanionFeatureMammoth");
            var mammothUpgrade = RequireBlueprint<BlueprintFeature>(library, MammothUpgradeGuid, "AnimalCompanionUpgradeMammoth");
            var dogFeature = RequireBlueprint<BlueprintFeature>(library, DogFeatureGuid, "AnimalCompanionFeatureDog");
            rangerSelection = RequireBlueprint<BlueprintFeatureSelection>(library, RangerSelectionGuid, "AnimalCompanionSelectionRanger");
            levelRank = RequireBlueprint<BlueprintFeature>(library, LevelRankGuid, "AnimalCompanionRank");
            var hoof = RequireBlueprint<BlueprintItemWeapon>(library, HoofGuid, "Hoof1d4");
            var bite = RequireUniqueNamedBlueprint<BlueprintItemWeapon>(library, "Bite1d4");
            var classLevels = mammothUnit.GetComponent<AddClassLevels>();
            if (classLevels == null || classLevels.CharacterClass == null)
            {
                throw new InvalidOperationException("The exact stock Mammoth companion has no usable AddClassLevels contract.");
            }
            var dogUnit = dogFeature.GetComponent<AddPet>()?.Pet;
            var dogClassLevels = dogUnit?.GetComponent<AddClassLevels>();
            if (dogClassLevels == null || dogClassLevels.CharacterClass == null ||
                !ReferenceEquals(dogClassLevels.CharacterClass, classLevels.CharacterClass) ||
                classLevels.Levels != 0 || dogClassLevels.Levels != 0)
            {
                throw new InvalidOperationException("The exact stock Mammoth/Dog zero-level AddPet bootstrap contract changed.");
            }
            companionClass = classLevels.CharacterClass;
            stockMammothInitialClassLevels = classLevels.Levels;
            stockDogInitialClassLevels = dogClassLevels.Levels;

            var name = NewLocalizedString("KMC.Horse.Name");
            var description = NewLocalizedString("KMC.Horse.Description");
            var upgradeName = NewLocalizedString("KMC.Horse.Upgrade.Name");
            var upgradeDescription = NewLocalizedString("KMC.Horse.Upgrade.Description");
            var sharedName = CreateOwned<SharedStringAsset>("KMC_Horse_LocalizedName");
            sharedName.String = name;

            horseUpgrade = CreateOwned<BlueprintFeature>("AnimalCompanionUpgradeHorse");
            horseUpgrade.AssetGuid = UpgradeGuid;
            CopyFeatureContract(mammothUpgrade, horseUpgrade);
            SetFeaturePresentation(horseUpgrade, upgradeName, upgradeDescription, ResolveHorseIcon(nativeHorse, dogFeature));
            horseUpgrade.ComponentsArray = new BlueprintComponent[]
            {
                CreateStatBonus("KMC_HorseUpgrade_Strength", StatType.Strength, 2),
                CreateStatBonus("KMC_HorseUpgrade_Constitution", StatType.Constitution, 2)
            };

            horseUnit = CreateOwned<BlueprintUnit>("AnimalCompanionUnitHorse");
            horseUnit.AssetGuid = UnitGuid;
            horseUnit.Type = mammothUnit.Type;
            horseUnit.LocalizedName = sharedName;
            horseUnit.Gender = nativeHorse.Gender;
            horseUnit.Size = Size.Large;
            horseUnit.IsLeftHanded = nativeHorse.IsLeftHanded;
            horseUnit.Color = nativeHorse.Color;
            horseUnit.Race = nativeHorse.Race;
            horseUnit.Alignment = nativeHorse.Alignment;
            PortraitField.SetValue(horseUnit, PortraitField.GetValue(nativeHorse) ?? PortraitField.GetValue(mammothUnit));
            horseUnit.Prefab = nativeHorse.Prefab;
            horseUnit.CustomizationPreset = nativeHorse.CustomizationPreset;
            horseUnit.ConsoleOverridePrefab = nativeHorse.ConsoleOverridePrefab;
            horseUnit.Visual = nativeHorse.Visual;
            horseUnit.Faction = mammothUnit.Faction;
            horseUnit.FactionOverrides = mammothUnit.FactionOverrides;
            horseUnit.StartingInventory = CloneArray(nativeHorse.StartingInventory);
            horseUnit.Brain = mammothUnit.Brain;
            horseUnit.Body = CopyBody(nativeHorse.Body, bite, hoof);
            // Pathfinder animal-companion horse base statistics. The native
            // riding prototype supplies the view; companion mechanics are owned
            // explicitly by KMC rather than inherited wholesale.
            horseUnit.Strength = 16;
            horseUnit.Dexterity = 13;
            horseUnit.Constitution = 15;
            horseUnit.Intelligence = 2;
            horseUnit.Wisdom = 12;
            horseUnit.Charisma = 6;
            horseUnit.Speed = nativeHorse.Speed;
            horseUnit.BaseAttackBonus = nativeHorse.BaseAttackBonus;
            horseUnit.Skills = CopySkills(nativeHorse.Skills);
            horseUnit.MaxHP = nativeHorse.MaxHP;
            horseUnit.AdditionalTemplates = CloneArray(mammothUnit.AdditionalTemplates);
            horseUnit.AddFacts = CloneArray(mammothUnit.AddFacts);
            horseUnit.ComponentsArray = new BlueprintComponent[] { CopyAddClassLevels(classLevels) };

            horseFeature = CreateOwned<BlueprintFeature>("AnimalCompanionFeatureHorse");
            horseFeature.AssetGuid = FeatureGuid;
            CopyFeatureContract(mammothFeature, horseFeature);
            SetFeaturePresentation(horseFeature, name, description, ResolveHorseIcon(nativeHorse, dogFeature));
            var addPet = CreateOwned<HorseCompanionAddPet>("KMC_Horse_AddPet");
            addPet.Pet = horseUnit;
            addPet.LevelRank = levelRank;
            addPet.UpgradeFeature = horseUpgrade;
            addPet.UpgradeLevel = 4;
            upgradeLevel = addPet.UpgradeLevel;
            horseFeature.ComponentsArray = new BlueprintComponent[] { addPet };

            library.BlueprintsByAssetId.Add(UnitGuid, horseUnit);
            library.BlueprintsByAssetId.Add(UpgradeGuid, horseUpgrade);
            library.BlueprintsByAssetId.Add(FeatureGuid, horseFeature);

            biteGuid = bite.AssetGuid;
            biteName = bite.name;
            hoofName = hoof.name;
            selectionLease = new ExactAppendOnlyArrayLease<BlueprintFeature>(RequireExactRangerOptions(rangerSelection), horseFeature);
            string error;
            if (!ApplySelectionDesired(out error))
            {
                throw new InvalidOperationException(error);
            }
            ValidateRegisteredContract(library);
        }

        private bool ApplySelectionDesired(out string error)
        {
            error = null;
            if (rangerSelection == null || selectionLease == null)
            {
                error = "The exact Ranger selection lease is unavailable.";
                return false;
            }

            var current = rangerSelection.AllFeatures;
            if (selectionDesired)
            {
                if (selectionLease.MatchesAppended(current)) { return true; }
                if (!selectionLease.MatchesOriginal(current))
                {
                    error = "The Ranger companion selection changed outside KMC before append; no overwrite was attempted.";
                    return false;
                }
                rangerSelection.AllFeatures = selectionLease.CreateAppendedValue();
                return true;
            }

            BlueprintFeature[] restored;
            if (!selectionLease.TryRestore(current, out restored, out error))
            {
                return false;
            }
            rangerSelection.AllFeatures = restored;
            return true;
        }

        private void ValidateRegisteredContract(LibraryScriptableObject library)
        {
            if (!ReferenceEquals(library.BlueprintsByAssetId[UnitGuid], horseUnit) ||
                !ReferenceEquals(library.BlueprintsByAssetId[FeatureGuid], horseFeature) ||
                !ReferenceEquals(library.BlueprintsByAssetId[UpgradeGuid], horseUpgrade))
            {
                throw new InvalidOperationException("The initialized blueprint dictionary did not retain exact KMC horse references.");
            }
            var addPet = horseFeature.GetComponent<AddPet>();
            var classLevels = horseUnit.GetComponent<AddClassLevels>();
            if (addPet == null || !ReferenceEquals(addPet.Pet, horseUnit) || !ReferenceEquals(addPet.LevelRank, levelRank) ||
                !ReferenceEquals(addPet.UpgradeFeature, horseUpgrade) || addPet.UpgradeLevel != 4 ||
                classLevels == null || !ReferenceEquals(classLevels.CharacterClass, companionClass) ||
                classLevels.Levels != stockMammothInitialClassLevels ||
                classLevels.Levels != stockDogInitialClassLevels ||
                horseUnit.Size != Size.Large || horseUnit.Body == null || horseUnit.Body.DisableHands ||
                horseUnit.Body.PrimaryHand == null ||
                !ReferenceEquals(horseUnit.Body.EmptyHandWeapon, horseUnit.Body.PrimaryHand) ||
                horseUnit.Body.AdditionalLimbs.Length != 2)
            {
                throw new InvalidOperationException("The constructed KMC horse companion contract failed exact self-validation.");
            }
        }

        private BlueprintFeature[] RequireExactRangerOptions(BlueprintFeatureSelection selection)
        {
            var expectedGuids = new[]
            {
                "472091361cf118049a2b4339c4ea836a",
                DogFeatureGuid,
                "e992949eba096644784592dc7f51a5c7",
                "aa92fea676be33d4dafd176d699d7996",
                "2ee2ba60850dd064e8b98bf5c2c946ba",
                "ece6bde3dfc76ba4791376428e70621a",
                "67a9dc42b15d0954ca4689b13e8dedea"
            };
            var features = selection.AllFeatures;
            if (features == null || features.Length != expectedGuids.Length)
            {
                throw new InvalidOperationException("The exact Ranger companion selection no longer has seven stock options.");
            }
            for (var index = 0; index < expectedGuids.Length; index++)
            {
                if (features[index] == null || !string.Equals(features[index].AssetGuid, expectedGuids[index], StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("The exact Ranger companion option order changed at index " + index + ".");
                }
            }
            return features;
        }

        private static BlueprintUnit.UnitBody CopyBody(BlueprintUnit.UnitBody source, BlueprintItemWeapon bite, BlueprintItemWeapon hoof)
        {
            if (source == null) { throw new InvalidOperationException("The native horse body is unavailable."); }
            return new BlueprintUnit.UnitBody
            {
                // Kingmaker's stock UnitAttack enumerator consults the primary
                // hand only when hands are enabled, then appends AdditionalLimbs.
                // This yields the intended Bite primary plus two Hoof limbs.
                DisableHands = false,
                EmptyHandWeapon = bite,
                PrimaryHand = bite,
                SecondaryHand = null,
                PrimaryHandAlternative1 = null,
                SecondaryHandAlternative1 = null,
                PrimaryHandAlternative2 = null,
                SecondaryHandAlternative2 = null,
                PrimaryHandAlternative3 = null,
                SecondaryHandAlternative3 = null,
                ActiveHandSet = 0,
                AdditionalLimbs = new[] { hoof, hoof },
                AdditionalSecondaryLimbs = new BlueprintItemWeapon[0],
                Armor = source.Armor,
                Belt = source.Belt,
                Head = source.Head,
                Feet = source.Feet,
                Gloves = source.Gloves,
                Neck = source.Neck,
                Ring1 = source.Ring1,
                Ring2 = source.Ring2,
                Wrist = source.Wrist,
                Shoulders = source.Shoulders,
                QuickSlots = CloneArray(source.QuickSlots)
            };
        }

        private static BlueprintUnit.UnitSkills CopySkills(BlueprintUnit.UnitSkills source)
        {
            if (source == null) { return new BlueprintUnit.UnitSkills(); }
            return new BlueprintUnit.UnitSkills
            {
                Acrobatics = source.Acrobatics,
                Physique = source.Physique,
                Diplomacy = source.Diplomacy,
                Thievery = source.Thievery,
                LoreNature = source.LoreNature,
                Perception = source.Perception,
                Stealth = source.Stealth,
                UseMagicDevice = source.UseMagicDevice,
                LoreReligion = source.LoreReligion,
                KnowledgeWorld = source.KnowledgeWorld,
                KnowledgeArcana = source.KnowledgeArcana
            };
        }

        private AddClassLevels CopyAddClassLevels(AddClassLevels source)
        {
            var copy = CreateOwned<AddClassLevels>("KMC_Horse_AddClassLevels");
            copy.CharacterClass = source.CharacterClass;
            copy.Archetypes = CloneArray(source.Archetypes);
            copy.Levels = source.Levels;
            copy.RaceStat = source.RaceStat;
            copy.LevelsStat = source.LevelsStat;
            copy.Skills = CloneArray(source.Skills);
            copy.SelectSpells = CloneArray(source.SelectSpells);
            copy.MemorizeSpells = CloneArray(source.MemorizeSpells);
            copy.Selections = CloneArray(source.Selections);
            copy.DoNotApplyAutomatically = source.DoNotApplyAutomatically;
            return copy;
        }

        private AddStatBonus CreateStatBonus(string objectName, StatType stat, int value)
        {
            var component = CreateOwned<AddStatBonus>(objectName);
            component.Descriptor = ModifierDescriptor.Racial;
            component.Stat = stat;
            component.Value = value;
            component.ScaleByBasicAttackBonus = false;
            return component;
        }

        private static void CopyFeatureContract(BlueprintFeature source, BlueprintFeature target)
        {
            target.Groups = CloneArray(source.Groups);
            target.Ranks = 1;
            target.ReapplyOnLevelUp = source.ReapplyOnLevelUp;
            target.IsClassFeature = source.IsClassFeature;
            target.HideInUI = source.HideInUI;
            target.HideInCharacterSheetAndLevelUp = source.HideInCharacterSheetAndLevelUp;
            target.HideNotAvailibleInUI = source.HideNotAvailibleInUI;
            target.DlcType = source.DlcType;
        }

        private static void SetFeaturePresentation(BlueprintFeature target, LocalizedString name, LocalizedString description, Sprite icon)
        {
            DisplayNameField.SetValue(target, name);
            DescriptionField.SetValue(target, description);
            IconField.SetValue(target, icon);
        }

        private static Sprite ResolveHorseIcon(BlueprintUnit nativeHorse, BlueprintFeature fallback)
        {
            var portrait = PortraitField.GetValue(nativeHorse) as BlueprintPortrait;
            return portrait == null ? fallback.Icon : portrait.SmallPortrait;
        }

        private LocalizedString NewLocalizedString(string key)
        {
            var value = new LocalizedString();
            LocalizedStringKeyField.SetValue(value, key);
            return value;
        }

        private bool EnsureLocalization()
        {
            var pack = LocalizationManager.CurrentPack;
            if (pack == null || pack.Strings == null) { return false; }
            foreach (var entry in localization)
            {
                string current;
                if (pack.Strings.TryGetValue(entry.Key, out current))
                {
                    if (!string.Equals(current, entry.Value, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("A localization key collision exists for " + entry.Key + ".");
                    }
                    continue;
                }
                pack.Strings.Add(entry.Key, entry.Value);
            }
            return true;
        }

        private T CreateOwned<T>(string objectName) where T : ScriptableObject
        {
            var value = ScriptableObject.CreateInstance<T>();
            value.name = objectName;
            value.hideFlags = HideFlags.HideAndDontSave;
            ownedObjects.Add(value);
            return value;
        }

        private static T RequireBlueprint<T>(LibraryScriptableObject library, string guid, string expectedName)
            where T : BlueprintScriptableObject
        {
            BlueprintScriptableObject value;
            if (!library.BlueprintsByAssetId.TryGetValue(guid, out value) || !(value is T))
            {
                throw new InvalidOperationException("Required blueprint " + guid + " is missing or has the wrong type.");
            }
            if (!string.Equals(value.name, expectedName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Required blueprint " + guid + " has unexpected name " + value.name + ".");
            }
            return (T)value;
        }

        private static T RequireUniqueNamedBlueprint<T>(LibraryScriptableObject library, string name)
            where T : BlueprintScriptableObject
        {
            var matches = library.BlueprintsByAssetId.Values.OfType<T>()
                .Where(value => string.Equals(value.name, name, StringComparison.Ordinal)).ToList();
            if (matches.Count != 1)
            {
                throw new InvalidOperationException("Expected exactly one " + typeof(T).Name + " named " + name + "; observed " + matches.Count + ".");
            }
            return matches[0];
        }

        private static void AssertReservedGuidAbsent(LibraryScriptableObject library, string guid)
        {
            if (library.BlueprintsByAssetId.ContainsKey(guid))
            {
                throw new InvalidOperationException("Reserved KMC blueprint GUID collision: " + guid + ".");
            }
        }

        private void RollBackPartialRegistration(LibraryScriptableObject library)
        {
            if (selectionLease != null && rangerSelection != null)
            {
                BlueprintFeature[] restored;
                string ignored;
                if (selectionLease.TryRestore(rangerSelection.AllFeatures, out restored, out ignored))
                {
                    rangerSelection.AllFeatures = restored;
                }
            }
            RemoveExact(library, FeatureGuid, horseFeature);
            RemoveExact(library, UpgradeGuid, horseUpgrade);
            RemoveExact(library, UnitGuid, horseUnit);
            for (var index = ownedObjects.Count - 1; index >= 0; index--)
            {
                if (ownedObjects[index] != null) { UnityEngine.Object.Destroy(ownedObjects[index]); }
            }
            ownedObjects.Clear();
            horseUnit = null;
            horseFeature = null;
            horseUpgrade = null;
            rangerSelection = null;
            selectionLease = null;
        }

        private static void RemoveExact(LibraryScriptableObject library, string guid, BlueprintScriptableObject expected)
        {
            if (library == null || expected == null) { return; }
            BlueprintScriptableObject current;
            if (library.BlueprintsByAssetId.TryGetValue(guid, out current) && ReferenceEquals(current, expected))
            {
                library.BlueprintsByAssetId.Remove(guid);
            }
        }

        private static FieldInfo ResolveField(Type declaringType, string name, int token, Type fieldType)
        {
            var field = declaringType.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null || field.MetadataToken != token || field.FieldType != fieldType)
            {
                throw new MissingFieldException(declaringType.FullName, name + " exact token " + token.ToString("X8"));
            }
            return field;
        }

        private static T[] CloneArray<T>(T[] value)
        {
            return value == null ? new T[0] : (T[])value.Clone();
        }
    }
}
