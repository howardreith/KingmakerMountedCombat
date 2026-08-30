using System;
using System.Collections.Generic;
using System.IO;
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
        public bool StockMammothAllowDyingConditionComponent { get; set; }
        public bool StockDogAllowDyingConditionComponent { get; set; }
        public bool HorseAllowDyingConditionComponent { get; set; }
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
        internal const string PortraitGuid = "6874a165bf8bda3531ee4e2abc10c899";
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
            { "KMC.Horse.Feature.Name", "Animal Companion — Horse" },
            { "KMC.Horse.Description", "A Large native Kingmaker horse animal companion. It is fully controllable while unmounted and can be used by KMC's private-alpha mounted profile." },
            { "KMC.Horse.Upgrade.Name", "Horse Animal Companion Advancement" },
            { "KMC.Horse.Upgrade.Description", "At animal-companion rank 4, the horse gains +2 Strength and +2 Constitution." }
        };
        private readonly List<UnityEngine.Object> ownedObjects = new List<UnityEngine.Object>();
        private readonly Dictionary<string, string> observedActivationByOwner =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private HorseCompanionBlueprintState state;
        private string failure;
        private bool selectionDesired;
        private BlueprintPortrait horsePortrait;
        private Sprite horseIcon;
        private Sprite mountSaddleIcon;
        private BlueprintUnit horseUnit;
        private BlueprintFeature horseFeature;
        private BlueprintFeature horseUpgrade;
        private BlueprintFeatureSelection rangerSelection;
        private ExactAppendOnlyArrayLease<BlueprintFeature> selectionLease;
        private List<BlueprintScriptableObject> blueprintList;
        private string biteGuid;
        private string biteName;
        private string hoofName;
        private BlueprintCharacterClass companionClass;
        private BlueprintFeature levelRank;
        private int stockMammothInitialClassLevels;
        private int stockDogInitialClassLevels;
        private bool stockMammothAllowDyingConditionComponent;
        private bool stockDogAllowDyingConditionComponent;
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

        public BlueprintPortrait HorsePortrait => horsePortrait;

        public Sprite HorseIcon => horseIcon;

        public Sprite MountSaddleIcon => mountSaddleIcon;

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
                if (fact != null)
                {
                    var pet = owner.Descriptor.Pet;
                    var signature = "rank=" + fact.GetRank() +
                        ";addPet=" + (addPet != null) +
                        ";pet=" + (pet?.UniqueId ?? "<null>") +
                        ";blueprint=" + (pet?.Blueprint?.AssetGuid ?? "<null>") +
                        ";master=" + (pet?.Descriptor?.Master.Value?.UniqueId ?? "<null>");
                    string prior;
                    if (!observedActivationByOwner.TryGetValue(owner.UniqueId, out prior) ||
                        !string.Equals(prior, signature, StringComparison.Ordinal))
                    {
                        observedActivationByOwner[owner.UniqueId] = signature;
                        logger.Info("Observed live KMC Horse feature: owner=" + owner.UniqueId + ";" + signature + ".");
                    }
                }
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
                RangerAppendOwned = selectionLease != null &&
                    selectionLease.MatchesAppended(rangerSelection?.AllFeatures),
                RangerSelectionDesired = selectionDesired,
                NativeViewAssetId = horseUnit?.Prefab?.AssetId,
                CompanionClassGuid = classLevels?.CharacterClass?.AssetGuid,
                InitialClassLevels = classLevels?.Levels ?? -1,
                StockMammothInitialClassLevels = stockMammothInitialClassLevels,
                StockDogInitialClassLevels = stockDogInitialClassLevels,
                StockMammothAllowDyingConditionComponent = stockMammothAllowDyingConditionComponent,
                StockDogAllowDyingConditionComponent = stockDogAllowDyingConditionComponent,
                HorseAllowDyingConditionComponent = horseUnit?.GetComponent<AllowDyingCondition>() != null,
                LevelRankGuid = levelRank?.AssetGuid,
                UpgradeLevel = upgradeLevel,
                BiteGuid = biteGuid,
                BiteName = biteName,
                HoofGuid = body?.AdditionalLimbs?.Skip(1).FirstOrDefault()?.AssetGuid,
                HoofName = hoofName,
                NaturalAttackCount = body?.AdditionalLimbs?.Length ?? 0,
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
            if (state == HorseCompanionBlueprintState.Registered && rangerSelection != null &&
                selectionLease != null)
            {
                string error;
                if (!TryRestoreSelection(out error))
                {
                    throw new InvalidOperationException(error);
                }
            }

            // Blueprint facts may already be serialized into the active Working
            // process. Keep the exact KMC definitions in the in-memory library
            // until process exit; only the selectable Ranger surface is leased.
            disposed = true;
            state = HorseCompanionBlueprintState.Disposed;
        }

        private void Register(LibraryScriptableObject library)
        {
            blueprintList = library.GetAllBlueprints();
            if (blueprintList == null)
            {
                throw new InvalidOperationException("The initialized blueprint library has no canonical all-blueprints list.");
            }
            AssertReservedGuidAbsent(library, blueprintList, UnitGuid);
            AssertReservedGuidAbsent(library, blueprintList, FeatureGuid);
            AssertReservedGuidAbsent(library, blueprintList, UpgradeGuid);
            AssertReservedGuidAbsent(library, blueprintList, PortraitGuid);

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
            stockMammothAllowDyingConditionComponent = mammothUnit.GetComponent<AllowDyingCondition>() != null;
            stockDogAllowDyingConditionComponent = dogUnit.GetComponent<AllowDyingCondition>() != null;
            if (!stockMammothAllowDyingConditionComponent || !stockDogAllowDyingConditionComponent)
            {
                throw new InvalidOperationException(
                    "The exact stock Mammoth/Dog companion dying-condition component contract changed.");
            }

            var name = NewLocalizedString("KMC.Horse.Name");
            var featureName = NewLocalizedString("KMC.Horse.Feature.Name");
            var description = NewLocalizedString("KMC.Horse.Description");
            var upgradeName = NewLocalizedString("KMC.Horse.Upgrade.Name");
            var upgradeDescription = NewLocalizedString("KMC.Horse.Upgrade.Description");
            var sharedName = CreateOwned<SharedStringAsset>("KMC_Horse_LocalizedName");
            sharedName.String = name;

            horsePortrait = CreateOriginalHorsePortrait();
            horseIcon = CreateEmbeddedSprite(
                "KingmakerMountedCombat.Assets.HorseIcon.png",
                "KMC_Horse_Icon");
            mountSaddleIcon = CreateEmbeddedSprite(
                "KingmakerMountedCombat.Assets.MountSaddleIcon.png",
                "KMC_Mount_Saddle_Icon");

            horseUpgrade = CreateOwned<BlueprintFeature>("AnimalCompanionUpgradeHorse");
            horseUpgrade.AssetGuid = UpgradeGuid;
            CopyFeatureContract(mammothUpgrade, horseUpgrade);
            SetFeaturePresentation(horseUpgrade, upgradeName, upgradeDescription, horseIcon);
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
            PortraitField.SetValue(horseUnit, horsePortrait);
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
            horseUnit.ComponentsArray = new BlueprintComponent[]
            {
                CopyAddClassLevels(classLevels),
                CreateOwned<AllowDyingCondition>("KMC_Horse_AllowDyingCondition")
            };

            horseFeature = CreateOwned<BlueprintFeature>("AnimalCompanionFeatureHorse");
            horseFeature.AssetGuid = FeatureGuid;
            CopyFeatureContract(mammothFeature, horseFeature);
            SetFeaturePresentation(horseFeature, featureName, description, horseIcon);
            var addPet = CreateOwned<HorseCompanionAddPet>("KMC_Horse_AddPet");
            addPet.Pet = horseUnit;
            addPet.LevelRank = levelRank;
            addPet.UpgradeFeature = horseUpgrade;
            addPet.UpgradeLevel = 4;
            upgradeLevel = addPet.UpgradeLevel;
            horseFeature.ComponentsArray = new BlueprintComponent[] { addPet };

            AddExactBlueprintRegistration(library, horsePortrait);
            AddExactBlueprintRegistration(library, horseUnit);
            AddExactBlueprintRegistration(library, horseUpgrade);
            AddExactBlueprintRegistration(library, horseFeature);

            biteGuid = bite.AssetGuid;
            biteName = bite.name;
            hoofName = hoof.name;
            // Installed BlueprintFeatureSelection.Items enumerates AllFeatures,
            // not Features. The live Ranger Features surface is not the same
            // seven-entry runtime selection array, so it is intentionally left
            // untouched. Lease only the exact Items-authoritative surface.
            selectionLease = new ExactAppendOnlyArrayLease<BlueprintFeature>(
                RequireExactRangerOptions(rangerSelection.AllFeatures, "AllFeatures"), horseFeature);
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
                    error = "The Ranger companion AllFeatures selection changed outside KMC before append; no overwrite was attempted.";
                    return false;
                }
                rangerSelection.AllFeatures = selectionLease.CreateAppendedValue();
                return true;
            }

            return TryRestoreSelection(out error);
        }

        private bool TryRestoreSelection(out string error)
        {
            error = null;
            BlueprintFeature[] restored;
            string leaseError;
            if (!selectionLease.TryRestore(rangerSelection.AllFeatures, out restored, out leaseError))
            {
                error = "Ranger AllFeatures restore rejected: " + leaseError;
                return false;
            }
            rangerSelection.AllFeatures = restored;
            return true;
        }

        private void ValidateRegisteredContract(LibraryScriptableObject library)
        {
            if (!ReferenceEquals(library.BlueprintsByAssetId[UnitGuid], horseUnit) ||
                !ReferenceEquals(library.BlueprintsByAssetId[FeatureGuid], horseFeature) ||
                !ReferenceEquals(library.BlueprintsByAssetId[UpgradeGuid], horseUpgrade) ||
                !ReferenceEquals(library.BlueprintsByAssetId[PortraitGuid], horsePortrait) ||
                CountExactBlueprint(blueprintList, horseUnit) != 1 ||
                CountExactBlueprint(blueprintList, horseFeature) != 1 ||
                CountExactBlueprint(blueprintList, horseUpgrade) != 1 ||
                CountExactBlueprint(blueprintList, horsePortrait) != 1 ||
                !ReferenceEquals(PortraitField.GetValue(horseUnit), horsePortrait) ||
                horsePortrait.Data == null || !horsePortrait.Data.HasPortrait ||
                !ReferenceEquals(horseFeature.Icon, horseIcon) ||
                !ReferenceEquals(horseUpgrade.Icon, horseIcon) ||
                horseFeature.DlcType != Kingmaker.Blueprints.Root.DlcType.None ||
                horseUpgrade.DlcType != Kingmaker.Blueprints.Root.DlcType.None ||
                !selectionLease.MatchesAppended(rangerSelection.AllFeatures))
            {
                throw new InvalidOperationException("The initialized library, entitlement, or Ranger selection surfaces did not retain the exact KMC horse contract.");
            }
            var addPet = horseFeature.GetComponent<AddPet>();
            var classLevels = horseUnit.GetComponent<AddClassLevels>();
            if (addPet == null || !ReferenceEquals(addPet.Pet, horseUnit) || !ReferenceEquals(addPet.LevelRank, levelRank) ||
                !ReferenceEquals(addPet.UpgradeFeature, horseUpgrade) || addPet.UpgradeLevel != 4 ||
                classLevels == null || !ReferenceEquals(classLevels.CharacterClass, companionClass) ||
                classLevels.Levels != stockMammothInitialClassLevels ||
                classLevels.Levels != stockDogInitialClassLevels ||
                horseUnit.Size != Size.Large || horseUnit.Body == null || !horseUnit.Body.DisableHands ||
                horseUnit.Body.EmptyHandWeapon != null || horseUnit.Body.PrimaryHand != null ||
                horseUnit.Body.SecondaryHand != null || horseUnit.Body.AdditionalLimbs == null ||
                horseUnit.Body.AdditionalLimbs.Length != 3 ||
                !string.Equals(horseUnit.Body.AdditionalLimbs[0]?.AssetGuid, biteGuid, StringComparison.Ordinal) ||
                horseUnit.Body.AdditionalLimbs.Skip(1).Any(item => item == null ||
                    !string.Equals(item.AssetGuid, HoofGuid, StringComparison.Ordinal)) ||
                horseUnit.Body.AdditionalSecondaryLimbs == null ||
                horseUnit.Body.AdditionalSecondaryLimbs.Length != 0)
            {
                throw new InvalidOperationException("The constructed KMC horse companion contract failed exact self-validation.");
            }
        }

        private static BlueprintFeature[] RequireExactRangerOptions(BlueprintFeature[] features, string surface)
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
            if (features == null || features.Length != expectedGuids.Length)
            {
                throw new InvalidOperationException("The exact Ranger companion " + surface + " surface no longer has seven stock options.");
            }
            for (var index = 0; index < expectedGuids.Length; index++)
            {
                if (features[index] == null || !string.Equals(features[index].AssetGuid, expectedGuids[index], StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("The exact Ranger companion " + surface + " option order changed at index " + index + ".");
                }
            }
            return features;
        }

        private static BlueprintUnit.UnitBody CopyBody(BlueprintUnit.UnitBody source, BlueprintItemWeapon bite, BlueprintItemWeapon hoof)
        {
            if (source == null) { throw new InvalidOperationException("The native horse body is unavailable."); }
            return new BlueprintUnit.UnitBody
            {
                // The exact native horse disables hands. Kingmaker gives both
                // enabled hand slots attacks, including the secondary empty-hand
                // fallback, so a hand-owned Bite is duplicated in a full attack.
                // Ordered natural limbs yield one Bite and exactly two Hooves;
                // the same first Bite is stock CreateSingleAttack's selection.
                DisableHands = true,
                EmptyHandWeapon = null,
                PrimaryHand = null,
                SecondaryHand = null,
                PrimaryHandAlternative1 = null,
                SecondaryHandAlternative1 = null,
                PrimaryHandAlternative2 = null,
                SecondaryHandAlternative2 = null,
                PrimaryHandAlternative3 = null,
                SecondaryHandAlternative3 = null,
                ActiveHandSet = 0,
                AdditionalLimbs = new[] { bite, hoof, hoof },
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
            // KMC owns these definitions. Copying the stock Mammoth's DLC
            // entitlement can hide or reject the Horse on installations whose
            // owned DLC set differs, so the private-alpha feature is explicitly
            // base-game available.
            target.DlcType = Kingmaker.Blueprints.Root.DlcType.None;
        }

        private static void SetFeaturePresentation(BlueprintFeature target, LocalizedString name, LocalizedString description, Sprite icon)
        {
            DisplayNameField.SetValue(target, name);
            DescriptionField.SetValue(target, description);
            IconField.SetValue(target, icon);
        }

        private BlueprintPortrait CreateOriginalHorsePortrait()
        {
            var small = CreateEmbeddedSprite(
                "KingmakerMountedCombat.Assets.HorsePortraitSmall.png",
                "KMC_Horse_Portrait_Small");
            var medium = CreateEmbeddedSprite(
                "KingmakerMountedCombat.Assets.HorsePortraitMedium.png",
                "KMC_Horse_Portrait_Medium");
            var large = CreateEmbeddedSprite(
                "KingmakerMountedCombat.Assets.HorsePortraitLarge.png",
                "KMC_Horse_Portrait_Large");
            var portrait = CreateOwned<BlueprintPortrait>("KMC_Horse_Portrait");
            portrait.AssetGuid = PortraitGuid;
            portrait.ComponentsArray = new BlueprintComponent[0];
            // Direct in-memory sprites keep this runtime-owned portrait out of
            // Kingmaker's serialized custom-portrait directory contract.
            portrait.Data = new PortraitData(null, small, medium, large);
            portrait.BackupPortrait = null;
            return portrait;
        }

        private Sprite CreateEmbeddedSprite(string resourceName, string objectName)
        {
            byte[] bytes;
            var assembly = typeof(HorseCompanionBlueprintService).Assembly;
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException("Embedded original Horse art is missing: " + resourceName + ".");
                }
                using (var buffer = new MemoryStream())
                {
                    stream.CopyTo(buffer);
                    bytes = buffer.ToArray();
                }
            }

            var texture = new Texture2D(2, 2, TextureFormat.ARGB32, false)
            {
                name = objectName + "_Texture",
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 1
            };
            ownedObjects.Add(texture);
            if (!ImageConversion.LoadImage(texture, bytes, true) || texture.width <= 0 || texture.height <= 0)
            {
                throw new InvalidOperationException("Embedded original Horse art could not be decoded: " + resourceName + ".");
            }
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0u,
                SpriteMeshType.FullRect);
            sprite.name = objectName;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            ownedObjects.Add(sprite);
            return sprite;
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

        private static void AssertReservedGuidAbsent(
            LibraryScriptableObject library,
            IEnumerable<BlueprintScriptableObject> allBlueprints,
            string guid)
        {
            if (library.BlueprintsByAssetId.ContainsKey(guid) ||
                allBlueprints.Any(item => item != null && string.Equals(item.AssetGuid, guid, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException("Reserved KMC blueprint GUID collision: " + guid + ".");
            }
        }

        private void AddExactBlueprintRegistration(LibraryScriptableObject library, BlueprintScriptableObject blueprint)
        {
            if (library == null || blueprintList == null || blueprint == null)
            {
                throw new InvalidOperationException("The exact KMC blueprint registration surface is unavailable.");
            }
            AssertReservedGuidAbsent(library, blueprintList, blueprint.AssetGuid);
            library.BlueprintsByAssetId.Add(blueprint.AssetGuid, blueprint);
            blueprintList.Add(blueprint);
        }

        private static int CountExactBlueprint(
            IEnumerable<BlueprintScriptableObject> values,
            BlueprintScriptableObject expected)
        {
            if (values == null || expected == null) { return 0; }
            return values.Count(item => ReferenceEquals(item, expected) &&
                string.Equals(item.AssetGuid, expected.AssetGuid, StringComparison.Ordinal));
        }

        private void RollBackPartialRegistration(LibraryScriptableObject library)
        {
            if (selectionLease != null && rangerSelection != null)
            {
                string ignored;
                TryRestoreSelection(out ignored);
            }
            RemoveExact(library, blueprintList, FeatureGuid, horseFeature);
            RemoveExact(library, blueprintList, UpgradeGuid, horseUpgrade);
            RemoveExact(library, blueprintList, UnitGuid, horseUnit);
            RemoveExact(library, blueprintList, PortraitGuid, horsePortrait);
            for (var index = ownedObjects.Count - 1; index >= 0; index--)
            {
                if (ownedObjects[index] != null) { UnityEngine.Object.Destroy(ownedObjects[index]); }
            }
            ownedObjects.Clear();
            horseUnit = null;
            horsePortrait = null;
            horseIcon = null;
            mountSaddleIcon = null;
            horseFeature = null;
            horseUpgrade = null;
            rangerSelection = null;
            selectionLease = null;
            blueprintList = null;
        }

        private static void RemoveExact(
            LibraryScriptableObject library,
            IList<BlueprintScriptableObject> allBlueprints,
            string guid,
            BlueprintScriptableObject expected)
        {
            if (expected == null) { return; }
            BlueprintScriptableObject current;
            if (library != null && library.BlueprintsByAssetId != null &&
                library.BlueprintsByAssetId.TryGetValue(guid, out current) && ReferenceEquals(current, expected))
            {
                library.BlueprintsByAssetId.Remove(guid);
            }
            if (allBlueprints == null) { return; }
            for (var index = allBlueprints.Count - 1; index >= 0; index--)
            {
                if (ReferenceEquals(allBlueprints[index], expected))
                {
                    allBlueprints.RemoveAt(index);
                }
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
