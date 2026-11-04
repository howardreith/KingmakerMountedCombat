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
        public string RidingDogUnitGuid { get; set; }
        public string RidingDogFeatureGuid { get; set; }
        public string[] RidingDogSelectionGuids { get; set; }
        public string RidingDogFailure { get; set; }
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

    internal sealed partial class HorseCompanionBlueprintService : IDisposable
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
        internal const string RidingDogUnitGuid = SupportedMountedProfiles.RidingDogBlueprintGuid;
        internal const string RidingDogFeatureGuid = "5c8e2b9a417f4d63a1d07e3bf26c9184";
        internal const string RidingDogUpgradeGuid = "e31d6f08a95c47b2b8c4f27a6d10e5c7";
        internal const string RidingDogSizeGuid = "92fb07c3d6e14a58bd23c8e1f47a0d69";

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
            { "KMC.Horse.Upgrade.Description", "At animal-companion rank 4, the horse gains +2 Strength and +2 Constitution." },
            { "KMC.RidingDog.Name", "Riding Dog" },
            { "KMC.RidingDog.Feature.Name", "Animal Companion — Riding Dog" },
            { "KMC.RidingDog.Description", "A Medium dog bred to carry a rider. It bites and trips like any dog and can carry a Small rider (Halfling or Gnome) from the first level it is gained. A Medium rider is too large for it." },
            { "KMC.RidingDog.Size.Name", "Riding Dog Build" },
            { "KMC.RidingDog.Size.Description", "A riding dog is Medium from the start." },
            { "KMC.RidingDog.Upgrade.Name", "Riding Dog Advancement" },
            { "KMC.RidingDog.Upgrade.Description", "At animal-companion rank 4, the riding dog gains +2 Strength and +2 Constitution. It does not grow larger." },
            { "KMC.MountedCombat.Name", "Mounted Combat" },
            { "KMC.MountedCombat.Description", "Prerequisite: 1 rank in Mobility. Once per round, when your mount is hit by an attack, you attempt a Mobility check as an immediate action. If your check result is greater than the attack result, the hit is negated. Used on your turn, this spends that turn's swift action. Used on another's turn, it spends your next turn's swift action, and you cannot use another immediate or swift action until after that turn. In real time, it spends your swift action for 6 seconds. The reaction is used automatically on the first hit you can react to; turn it off with the Mounted Combat Reaction toggle." },
            { "KMC.MountedCombat.Toggle.Name", "Mounted Combat Reaction" },
            { "KMC.MountedCombat.Toggle.Description", "While this is on, you automatically attempt the Mounted Combat reaction on the first hit on your mount that you can react to. Turning it off or on never restores a reaction or a swift action you have already spent." },
            { "KMC.MountedCombat.Spent.Name", "Mounted Combat: Reaction Used" },
            { "KMC.MountedCombat.Spent.Description", "You have used your Mounted Combat reaction this round." },
            { "KMC.MountedCombat.Owed.Name", "Mounted Combat: Swift Action Owed" },
            { "KMC.MountedCombat.Owed.Description", "You used an immediate action outside your turn. Your next turn's swift action is spent, and you cannot use another immediate or swift action until after that turn." },
            { "KMC.DivineBond.Name", "Divine Bond" },
            { "KMC.DivineBond.Description", "Upon reaching 5th level, a paladin forms a divine bond with her god: a bonded weapon or a bonded mount. Once the form is chosen, it can only be changed by retraining." },
            { "KMC.DivineMount.Name", "Divine Mount" },
            { "KMC.DivineMount.Description", "You gain the service of an unusually intelligent, strong and loyal steed. It is an animal companion using your paladin level as your effective druid level, and it does not grant a second companion if you already have one: your existing companion becomes your bonded mount and your levels stack. You can call it to your side. At 11th level it gains celestial protections and a smite, and at 15th level spell resistance equal to your paladin level + 11. It is an ordinary persistent companion: a fallen mount recovers as other companions do, with no 30-day penalty." },
            { "KMC.DivineMount.Horse.Name", "Divine Mount: Horse" },
            { "KMC.DivineMount.Horse.Description", "Your bonded mount is a Large horse. Medium and Small riders can ride it." },
            { "KMC.DivineMount.RidingDog.Name", "Divine Mount: Riding Dog" },
            { "KMC.DivineMount.RidingDog.Description", "Your bonded mount is a Medium riding dog. It can carry a Small rider; a Medium rider is too large for it." },
            { "KMC.DivineMount.Call.Name", "Call Divine Mount" },
            { "KMC.DivineMount.Call.Description", "As a full-round action, you call your mount to your side. Once per day at 5th level, plus once per day for every 4 levels thereafter, to four times per day at 17th level. It cannot be used while mounted or if your mount cannot act." },
            { "KMC.DivineMount.CallUse.Name", "Call Divine Mount: Additional Use" },
            { "KMC.DivineMount.CallUse.Description", "You can call your mount one more time per day." },
            { "KMC.DivineMount.Intelligence.Name", "Bonded Mount: Intelligence" },
            { "KMC.DivineMount.Intelligence.Description", "Your mount gains +4 Intelligence, so an ordinary animal companion has an Intelligence of 6." },
            { "KMC.DivineMount.Celestial.Name", "Celestial Mount" },
            { "KMC.DivineMount.Celestial.Description", "Your mount gains damage reduction 5/evil, resistance 10 to cold, acid and electricity, and Celestial Smite once per day. It remains an animal for spells that target animals." },
            { "KMC.DivineMount.GreaterCelestial.Name", "Greater Celestial Mount" },
            { "KMC.DivineMount.GreaterCelestial.Description", "Your mount's celestial protections improve to damage reduction 10/evil and resistance 15 to cold, acid and electricity." },
            { "KMC.DivineMount.SpellResistance.Name", "Mount Spell Resistance" },
            { "KMC.DivineMount.SpellResistance.Description", "Your mount has spell resistance equal to your paladin level + 11." },
            { "KMC.DivineMount.Smite.Name", "Celestial Smite" },
            { "KMC.DivineMount.Smite.Description", "As a swift action, once per day, the mount marks an evil target. Until the target is dead or the mark ends, the mount adds its Charisma bonus to attack rolls and its Hit Dice to damage rolls against it." }
        };
        private readonly List<UnityEngine.Object> ownedObjects = new List<UnityEngine.Object>();
        private readonly Dictionary<string, HorseActivationObservation> observedActivationByOwner =
            new Dictionary<string, HorseActivationObservation>(StringComparer.Ordinal);
        private readonly HashSet<string> observedOwnersThisUpdate = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<string> expiredObservationOwners = new List<string>();
        private HorseCompanionBlueprintState state;
        private string failure;
        private bool selectionDesired;
        private BlueprintPortrait horsePortrait;
        private Sprite horseIcon;
        private Sprite mountSaddleIcon;
        private Sprite dismountSaddleIcon;
        private BlueprintUnit horseUnit;
        private BlueprintFeature horseFeature;
        private BlueprintFeature horseUpgrade;
        private BlueprintUnit ridingDogUnit;
        private BlueprintFeature ridingDogFeature;
        private BlueprintFeature ridingDogUpgrade;
        private BlueprintFeature ridingDogSize;
        private string ridingDogFailure;
        private readonly List<KeyValuePair<BlueprintFeatureSelection, ExactAppendOnlyArrayLease<BlueprintFeature>>> ridingDogSelections =
            new List<KeyValuePair<BlueprintFeatureSelection, ExactAppendOnlyArrayLease<BlueprintFeature>>>();
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
        public Sprite DismountSaddleIcon => dismountSaddleIcon;

        public BlueprintFeature HorseFeature => horseFeature;

        public BlueprintFeature HorseUpgrade => horseUpgrade;

        public BlueprintUnit RidingDogUnit => ridingDogUnit;

        public BlueprintFeature RidingDogFeature => ridingDogFeature;

        public BlueprintFeature RidingDogUpgrade => ridingDogUpgrade;

        internal IEnumerable<BlueprintFeatureSelection> RidingDogSelections => ridingDogSelections.Select(pair => pair.Key);

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

        private struct HorseActivationObservation
        {
            private readonly int rank;
            private readonly bool hasAddPet;
            private readonly string petId, blueprintId, masterId, negativeSign;
            internal string Signature;

            internal HorseActivationObservation(int rank, bool hasAddPet, string petId,
                string blueprintId, string masterId, string negativeSign)
            {
                this.rank = rank;
                this.hasAddPet = hasAddPet;
                this.petId = petId;
                this.blueprintId = blueprintId;
                this.masterId = masterId;
                this.negativeSign = negativeSign;
                Signature = null;
            }

            internal bool Matches(HorseActivationObservation other) =>
                rank == other.rank && hasAddPet == other.hasAddPet &&
                string.Equals(petId, other.petId, StringComparison.Ordinal) &&
                string.Equals(blueprintId, other.blueprintId, StringComparison.Ordinal) &&
                string.Equals(masterId, other.masterId, StringComparison.Ordinal) &&
                string.Equals(negativeSign, other.negativeSign, StringComparison.Ordinal);

            internal string FormatSignature() => "rank=" + rank +
                ";addPet=" + hasAddPet + ";pet=" + petId +
                ";blueprint=" + blueprintId + ";master=" + masterId;
        }

        private void SynchronizeLiveHorseCompanionProgression()
        {
            var player = Game.Instance?.Player;
            var party = player?.PartyCharacters;
            if (party == null || horseFeature == null) { return; }

            observedOwnersThisUpdate.Clear();
            expiredObservationOwners.Clear();
            try
            {
                foreach (var reference in party)
                {
                    var owner = reference.Value;
                    var fact = owner?.Descriptor?.GetFact(horseFeature) ??
                        (ridingDogFeature == null ? null : owner?.Descriptor?.GetFact(ridingDogFeature)) ??
                        (divineMountHorse == null ? null : owner?.Descriptor?.GetFact(divineMountHorse)) ??
                        (divineMountRidingDog == null ? null : owner?.Descriptor?.GetFact(divineMountRidingDog));
                    var addPet = fact?.Get<HorseCompanionAddPet>();
                    if (fact != null)
                    {
                        observedOwnersThisUpdate.Add(owner.UniqueId);
                        var pet = owner.Descriptor.Pet;
                        var rank = fact.GetRank();
                        // Int32 default formatting only varies by the negative sign.
                        // Retain that value, not a culture or native object.
                        var negativeSign = rank < 0
                            ? System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NegativeSign : null;
                        var observation = new HorseActivationObservation(rank, addPet != null,
                            pet?.UniqueId ?? "<null>", pet?.Blueprint?.AssetGuid ?? "<null>",
                            pet?.Descriptor?.Master.Value?.UniqueId ?? "<null>", negativeSign);
                        var observedOwnerId = owner.UniqueId;
                        ApplyDivineMountPetBenefits(owner, observation, observedOwnerId);
                        HorseActivationObservation prior;
                        if (!observedActivationByOwner.TryGetValue(observedOwnerId, out prior) ||
                            !prior.Matches(observation))
                        {
                            var signature = observation.FormatSignature();
                            observation.Signature = signature;
                            if (!string.Equals(prior.Signature, signature, StringComparison.Ordinal))
                            {
                                observedActivationByOwner[owner.UniqueId] = observation;
                                logger.Info("Observed live KMC Horse feature: owner=" + owner.UniqueId + ";" + signature + ".");
                            }
                            else
                            {
                                // Preserve the prior string-based deduplication even
                                // when different field values format identically.
                                observedActivationByOwner[observedOwnerId] = observation;
                            }
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

                // This cache contains observation values only, never native progression
                // or cleanup obligations. Retire absence only after a complete
                // authoritative PartyCharacters enumeration, not a partial read.
                foreach (var id in observedActivationByOwner.Keys)
                    if (!observedOwnersThisUpdate.Contains(id)) expiredObservationOwners.Add(id);
                foreach (var id in expiredObservationOwners) observedActivationByOwner.Remove(id);
            }
            finally
            {
                observedOwnersThisUpdate.Clear();
                expiredObservationOwners.Clear();
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
                RidingDogUnitGuid = ridingDogUnit?.AssetGuid,
                RidingDogFeatureGuid = ridingDogFeature?.AssetGuid,
                RidingDogSelectionGuids = ridingDogSelections.Select(pair => pair.Key.AssetGuid).ToArray(),
                RidingDogFailure = ridingDogFailure,
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
            observedActivationByOwner.Clear();
            observedOwnersThisUpdate.Clear();
            expiredObservationOwners.Clear();
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
            dismountSaddleIcon = CreateEmbeddedSprite(
                "KingmakerMountedCombat.Assets.DismountSaddleIcon.png",
                "KMC_Dismount_Saddle_Icon");

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
                CopyAddClassLevels(classLevels, "KMC_Horse_AddClassLevels"),
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
            horseFeature.ComponentsArray = new BlueprintComponent[] { addPet, CreateNoCompanionPrerequisite("KMC_Horse_NoCompanion") };

            AddExactBlueprintRegistration(library, horsePortrait);
            AddExactBlueprintRegistration(library, horseUnit);
            AddExactBlueprintRegistration(library, horseUpgrade);
            AddExactBlueprintRegistration(library, horseFeature);

            // A Riding Dog fault never costs the player the Horse or the mounted controls.
            try { RegisterRidingDog(library, dogFeature, dogUnit, dogClassLevels); }
            catch (Exception exception)
            {
                ridingDogFailure = exception.GetType().Name + ": " + exception.Message;
                logger.Exception("Riding Dog companion blueprint registration", exception);
                RemoveRidingDog(library);
            }

            RegisterMountedCombatFeatReported(library);

            biteGuid = bite.AssetGuid;
            biteName = bite.name;
            hoofName = hoof.name;
            // Installed BlueprintFeatureSelection.Items enumerates AllFeatures,
            // not Features. The live Ranger Features surface is not the same
            // admitted runtime selection array, so it is intentionally left
            // untouched. Lease only the exact Items-authoritative surface.
            selectionLease = new ExactAppendOnlyArrayLease<BlueprintFeature>(
                RequireExactRangerOptions(rangerSelection.AllFeatures, "AllFeatures"),
                ridingDogFeature == null ? new[] { horseFeature } : new[] { horseFeature, ridingDogFeature });
            // Every other native selection that offers the stock Dog offers the Riding Dog too, with its own
            // class's rank progression. The Ranger's own lease above carries both of its additions.
            foreach (var selection in blueprintList.OfType<BlueprintFeatureSelection>().Where(value =>
                ridingDogFeature != null && !ReferenceEquals(value, rangerSelection) && value.AllFeatures != null && value.AllFeatures.Contains(dogFeature) &&
                !value.AllFeatures.Contains(null)))
            {
                ridingDogSelections.Add(new KeyValuePair<BlueprintFeatureSelection, ExactAppendOnlyArrayLease<BlueprintFeature>>(
                    selection, new ExactAppendOnlyArrayLease<BlueprintFeature>(selection.AllFeatures, ridingDogFeature)));
            }
            // After the Ranger selection exists: the bond reads its group and mode.
            RegisterDivineMountReported(library);
            logger.Info("Riding Dog selections: Ranger + " + string.Join(",", ridingDogSelections.Select(pair => pair.Key.name)) + "; failure=" + (ridingDogFailure ?? "none") + ".");
            string error;
            if (!ApplySelectionDesired(out error))
            {
                throw new InvalidOperationException(error);
            }
            ValidateRegisteredContract(library);
        }

        private void RemoveRidingDog(LibraryScriptableObject library)
        {
            RemoveExact(library, blueprintList, RidingDogFeatureGuid, ridingDogFeature);
            RemoveExact(library, blueprintList, RidingDogUpgradeGuid, ridingDogUpgrade);
            RemoveExact(library, blueprintList, RidingDogUnitGuid, ridingDogUnit);
            RemoveExact(library, blueprintList, RidingDogSizeGuid, ridingDogSize);
            ridingDogFeature = null; ridingDogUpgrade = null; ridingDogUnit = null; ridingDogSize = null;
            ridingDogSelections.Clear();
        }

        // Additive beside the stock Dog. The stock definitions are only read: this copies from the
        // Dog's unit, and native size steps from its blueprint size keep the native view scaling
        // (the same scale the stock Dog reaches when it grows) with no change to any other dog.
        private void RegisterRidingDog(LibraryScriptableObject library, BlueprintFeature dogFeature, BlueprintUnit dogUnit, AddClassLevels dogClassLevels)
        {
            var stockAddPet = dogFeature.GetComponent<AddPet>();
            AssertReservedGuidAbsent(library, blueprintList, RidingDogUnitGuid);
            AssertReservedGuidAbsent(library, blueprintList, RidingDogFeatureGuid);
            AssertReservedGuidAbsent(library, blueprintList, RidingDogUpgradeGuid);
            AssertReservedGuidAbsent(library, blueprintList, RidingDogSizeGuid);
            if (stockAddPet == null || dogUnit.Size > Size.Medium)
            {
                throw new InvalidOperationException("The stock Dog companion has no AddPet or is larger than Medium.");
            }
            var contract = stockAddPet.UpgradeFeature ?? dogFeature;
            var sharedName = CreateOwned<SharedStringAsset>("KMC_RidingDog_LocalizedName");
            sharedName.String = NewLocalizedString("KMC.RidingDog.Name");
            var icon = dogFeature.Icon;

            ridingDogSize = CreateOwned<BlueprintFeature>("AnimalCompanionRidingDogSize");
            ridingDogSize.AssetGuid = RidingDogSizeGuid;
            CopyFeatureContract(contract, ridingDogSize);
            SetFeaturePresentation(ridingDogSize, NewLocalizedString("KMC.RidingDog.Size.Name"), NewLocalizedString("KMC.RidingDog.Size.Description"), icon);
            var size = CreateOwned<Kingmaker.Designers.Mechanics.Buffs.ChangeUnitSize>("KMC_RidingDog_ChangeUnitSize");
            size.SizeDelta = Size.Medium - dogUnit.Size;
            ridingDogSize.ComponentsArray = new BlueprintComponent[] { size };

            ridingDogUpgrade = CreateOwned<BlueprintFeature>("AnimalCompanionUpgradeRidingDog");
            ridingDogUpgrade.AssetGuid = RidingDogUpgradeGuid;
            CopyFeatureContract(contract, ridingDogUpgrade);
            SetFeaturePresentation(ridingDogUpgrade, NewLocalizedString("KMC.RidingDog.Upgrade.Name"), NewLocalizedString("KMC.RidingDog.Upgrade.Description"), icon);
            ridingDogUpgrade.ComponentsArray = new BlueprintComponent[]
            {
                CreateStatBonus("KMC_RidingDogUpgrade_Strength", StatType.Strength, 2),
                CreateStatBonus("KMC_RidingDogUpgrade_Constitution", StatType.Constitution, 2)
            };

            ridingDogUnit = CreateOwned<BlueprintUnit>("AnimalCompanionUnitRidingDog");
            ridingDogUnit.AssetGuid = RidingDogUnitGuid;
            ridingDogUnit.Type = dogUnit.Type;
            ridingDogUnit.LocalizedName = sharedName;
            ridingDogUnit.Gender = dogUnit.Gender;
            ridingDogUnit.Size = dogUnit.Size;
            ridingDogUnit.IsLeftHanded = dogUnit.IsLeftHanded;
            ridingDogUnit.Color = dogUnit.Color;
            ridingDogUnit.Race = dogUnit.Race;
            ridingDogUnit.Alignment = dogUnit.Alignment;
            PortraitField.SetValue(ridingDogUnit, PortraitField.GetValue(dogUnit));
            ridingDogUnit.Prefab = dogUnit.Prefab;
            ridingDogUnit.CustomizationPreset = dogUnit.CustomizationPreset;
            ridingDogUnit.ConsoleOverridePrefab = dogUnit.ConsoleOverridePrefab;
            ridingDogUnit.Visual = dogUnit.Visual;
            ridingDogUnit.Faction = dogUnit.Faction;
            ridingDogUnit.FactionOverrides = dogUnit.FactionOverrides;
            ridingDogUnit.StartingInventory = CloneArray(dogUnit.StartingInventory);
            ridingDogUnit.Brain = dogUnit.Brain;
            ridingDogUnit.Body = dogUnit.Body;
            // Pathfinder riding dog base statistics. Native companion levels add everything else once.
            ridingDogUnit.Strength = 15;
            ridingDogUnit.Dexterity = 15;
            ridingDogUnit.Constitution = 15;
            ridingDogUnit.Intelligence = 2;
            ridingDogUnit.Wisdom = 12;
            ridingDogUnit.Charisma = 6;
            ridingDogUnit.Speed = dogUnit.Speed;
            ridingDogUnit.BaseAttackBonus = dogUnit.BaseAttackBonus;
            ridingDogUnit.Skills = CopySkills(dogUnit.Skills);
            ridingDogUnit.MaxHP = dogUnit.MaxHP;
            ridingDogUnit.AdditionalTemplates = CloneArray(dogUnit.AdditionalTemplates);
            // A size fact on the stock Dog belongs to its own growth (another mod may add one). The Riding Dog is Medium from the start.
            var stockFacts = CloneArray(dogUnit.AddFacts).Where(fact => !SetsSize(fact)).ToArray();
            ridingDogUnit.AddFacts = dogUnit.Size == Size.Medium ? stockFacts
                : stockFacts.Concat(new BlueprintUnitFact[] { ridingDogSize }).ToArray();
            ridingDogUnit.ComponentsArray = new BlueprintComponent[]
            {
                CopyAddClassLevels(dogClassLevels, "KMC_RidingDog_AddClassLevels"),
                CreateOwned<AllowDyingCondition>("KMC_RidingDog_AllowDyingCondition")
            };

            ridingDogFeature = CreateOwned<BlueprintFeature>("AnimalCompanionFeatureRidingDog");
            ridingDogFeature.AssetGuid = RidingDogFeatureGuid;
            CopyFeatureContract(dogFeature, ridingDogFeature);
            SetFeaturePresentation(ridingDogFeature, NewLocalizedString("KMC.RidingDog.Feature.Name"), NewLocalizedString("KMC.RidingDog.Description"), icon);
            var addPet = CreateOwned<HorseCompanionAddPet>("KMC_RidingDog_AddPet");
            addPet.Pet = ridingDogUnit;
            addPet.LevelRank = levelRank;
            addPet.UpgradeFeature = ridingDogUpgrade;
            // The published rank. The stock Dog's own level differs by installation (7 in the base game, 4 under Call of the Wild).
            addPet.UpgradeLevel = 4;
            ridingDogFeature.ComponentsArray = new BlueprintComponent[] { addPet, CreateNoCompanionPrerequisite("KMC_RidingDog_NoCompanion") };
            logger.Info("Riding Dog registered from the stock Dog: stockSize=" + dogUnit.Size + "; sizeStep=" + size.SizeDelta +
                "; stockFacts=" + string.Join(",", CloneArray(dogUnit.AddFacts).Select(fact => fact?.name)) +
                "; droppedSizeFacts=" + string.Join(",", CloneArray(dogUnit.AddFacts).Where(SetsSize).Select(fact => fact.name)) +
                "; stockUpgrade=" + stockAddPet.UpgradeFeature?.name + "@" + stockAddPet.UpgradeLevel + ".");

            AddExactBlueprintRegistration(library, ridingDogSize);
            AddExactBlueprintRegistration(library, ridingDogUnit);
            AddExactBlueprintRegistration(library, ridingDogUpgrade);
            AddExactBlueprintRegistration(library, ridingDogFeature);
        }

        // Every selection is attempted. Another mod's drift in a Riding Dog selection is reported, and it
        // never decides whether the mod itself is enabled: the Ranger's own lease does.
        private bool ApplySelectionDesired(out string error)
        {
            ApplyRidingDogSelectionsReported(selectionDesired);
            ApplyMountedCombatSelectionsReported(selectionDesired);
            ApplyDivineBondReported(selectionDesired);
            return ApplyRangerSelection(out error);
        }

        private void ApplyRidingDogSelectionsReported(bool desired)
        {
            string dogError;
            if (ApplyRidingDogSelections(desired, out dogError)) return;
            ridingDogFailure = dogError;
            logger.Error(dogError);
        }

        private static string JoinErrors(string first, string second) =>
            first == null ? second : second == null ? first : first + " " + second;

        // A native size change, or another mod's permanent size override.
        private static bool SetsSize(BlueprintUnitFact fact) => fact?.ComponentsArray != null && fact.ComponentsArray.Any(component =>
            component is Kingmaker.Designers.Mechanics.Buffs.ChangeUnitSize || component != null && component.GetType().Name.EndsWith("SizeOverride", StringComparison.Ordinal));

        private bool ApplyRangerSelection(out string error)
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

            return TryRestoreRangerSelection(out error);
        }

        private bool ApplyRidingDogSelections(bool desired, out string error)
        {
            error = null;
            foreach (var pair in ridingDogSelections)
            {
                var current = pair.Key.AllFeatures;
                if (desired ? pair.Value.MatchesAppended(current) : pair.Value.MatchesOriginal(current)) continue;
                if (desired ? !pair.Value.MatchesOriginal(current) : !pair.Value.MatchesAppended(current))
                {
                    error = JoinErrors(error, "Companion selection " + pair.Key.name + " changed outside KMC; no overwrite was attempted.");
                    continue;
                }
                pair.Key.AllFeatures = desired ? pair.Value.CreateAppendedValue() : pair.Value.CreateOriginalValue();
            }
            return error == null;
        }

        private bool TryRestoreSelection(out string error)
        {
            ApplyRidingDogSelectionsReported(false);
            ApplyMountedCombatSelectionsReported(false);
            ApplyDivineBondReported(false);
            return TryRestoreRangerSelection(out error);
        }

        private bool TryRestoreRangerSelection(out string error)
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

        internal static void RequireNativeRangerOptions(BlueprintFeature[] features, BlueprintFeature horse)
        {
            if (features == null || features.Length != 14 || horse == null ||
                !string.Equals(horse.AssetGuid, FeatureGuid, StringComparison.Ordinal) ||
                !ReferenceEquals(features[12], horse) ||
                !string.Equals(features[13]?.AssetGuid, RidingDogFeatureGuid, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Base-only bootstrap requires the exact native Ranger append.");
            }
            RequireExactRangerOptions(features.Take(12).ToArray(), "native bootstrap prefix");
        }

        internal static BlueprintFeature[] RequireExactRangerOptions(BlueprintFeature[] features, string surface)
        {
            // These are two separately observed exact contracts. The historical
            // seven-entry shape is a CotW rewrite; the independent native game
            // exposes twelve. Neither admits an arbitrary extension or reordering.
            var historicalGuids = new[]
            {
                "472091361cf118049a2b4339c4ea836a",
                DogFeatureGuid,
                "e992949eba096644784592dc7f51a5c7",
                "aa92fea676be33d4dafd176d699d7996",
                "2ee2ba60850dd064e8b98bf5c2c946ba",
                "ece6bde3dfc76ba4791376428e70621a",
                "67a9dc42b15d0954ca4689b13e8dedea"
            };
            var nativeGuids = new[]
            {
                "472091361cf118049a2b4339c4ea836a",
                "f6f1cdcc404f10c4493dc1e51208fd6f",
                "afb817d80b843cc4fa7b12289e6ebe3d",
                "f9ef7717531f5914a9b6ecacfad63f46",
                DogFeatureGuid,
                "e992949eba096644784592dc7f51a5c7",
                "aa92fea676be33d4dafd176d699d7996",
                "2ee2ba60850dd064e8b98bf5c2c946ba",
                MammothFeatureGuid,
                "ece6bde3dfc76ba4791376428e70621a",
                "126712ef923ab204983d6f107629c895",
                "67a9dc42b15d0954ca4689b13e8dedea"
            };
            var expectedGuids = features != null && features.Length == nativeGuids.Length
                ? nativeGuids : historicalGuids;
            if (features == null || features.Length != expectedGuids.Length)
            {
                throw new InvalidOperationException("The exact Ranger companion " + surface +
                    " surface matches neither historical seven nor native twelve options; observedCount=" +
                    (features == null ? "null" : features.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)) +
                    "; observedFirst32=" + (features == null ? "<null>" : string.Join(",",
                        features.Take(32).Select(feature => feature == null ? "<null>" : feature.AssetGuid))) + ".");
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

        private AddClassLevels CopyAddClassLevels(AddClassLevels source, string objectName)
        {
            var copy = CreateOwned<AddClassLevels>(objectName);
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

        private static void SetFeaturePresentation(BlueprintUnitFact target, LocalizedString name, LocalizedString description, Sprite icon)
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
            RemoveDivineMount(library);
            RemoveRidingDog(library);
            RemoveMountedCombatFeat(library);
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
            dismountSaddleIcon = null;
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
