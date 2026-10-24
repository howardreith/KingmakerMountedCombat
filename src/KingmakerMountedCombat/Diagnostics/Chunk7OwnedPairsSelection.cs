using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace KingmakerMountedCombat.Diagnostics
{
    // A separate native campaign input. These IDs are copied only from a complete
    // same-candidate two-owner Manual receipt; this declaration creates no actors.
    public sealed class Chunk7OwnedPairsSelection
    {
        public const string Scenario = "chunk7-owned-two-pair-campaign";
        public const string SourceLeaf = "owned-two-pair-source.json";
        public const string SourceCase = "preserve-two-acquired-field";
        // Owner-supplied seed kind: two eligible Medium riders whose Horses are
        // created by disclosed native fixture setup in the disposable run copy.
        // It carries seed pins and never the producer receipt or pet pins.
        public const string OwnerSeedCase = "owner-seed-exploration";
        // Owner-seed-derived persistence producer: the unchanged disclosed setup, both
        // native Mounts, then exactly one genuine native Manual into the run root. Its
        // ancestry is owner-seed-derived with disclosed assistance, never acquired or
        // ordinary, and its output name differs from every acquired-ancestry Manual.
        public const string OwnerSeedManualCase = "owner-seed-persistence-save";
        // The separate-process cold consumer of exactly that Manual, pinned by the
        // producer run, its world evidence and the written Manual bytes.
        public const string OwnerSeedLoadCase = "owner-seed-persistence-load";
        public const string OwnerSeedManualName = "KMC_C7_SEED_WORLD";
        internal const string OwnerSeedCheckpoint = "multi-exploration-manual";
        // Owner-seed-derived combat (batch 3 scenarios 3-5): a disposable copy of the
        // owner-seed-derived Manual, cold-loaded with both pairs restored mounted. The
        // producer pins are Source*, the ACCEPTED cold consumer receipt is Checkpoint*.
        public const string OwnerSeedRealtimeCase = "owner-seed-realtime-core";
        public const string OwnerSeedRiderFirstCase = "owner-seed-combat-rider-first";
        public const string OwnerSeedMountFirstCase = "owner-seed-combat-mount-first";
        public const string OwnerSeedDisableCase = "owner-seed-combat-disable";
        // 7E X1: the existing two-pair native life case on the same restored mounted start.
        public const string OwnerSeedNativeLifeCase = "owner-seed-native-life-cleanup";
        // 7E X1 death proper: the same native life engine with one labelled native damage past HP+Constitution.
        public const string OwnerSeedNativeDeathCase = "owner-seed-native-death-cleanup";
        public const string OwnerSeedLoadRefusalCase = "owner-seed-load-refusal";
        internal const string OwnerSeedLoadRefusalLeaf = "Manual_812_KMC_C7_SEED_REFUSAL.zks";
        internal static bool LoadRefusalCheckpoint(string value) => value == "multi-malformed-refusal-manual" || value == "multi-future-refusal-manual";
        // Owner-seed checkpoint producer (Directive 4): a disposable copy of the same accepted
        // Manual, cold-loaded with both pairs restored mounted, writes one genuine native
        // checkpoint through the generic persistence machinery. Source* is the Manual producer,
        // Checkpoint* the ACCEPTED cold consumer, exactly as derived combat.
        public const string OwnerSeedCheckpointSaveCase = "owner-seed-checkpoint-save";
        // Its separate-process cold consumer: Source* keeps the owner-seed Manual ancestry,
        // Checkpoint* is the checkpoint producer run and its persistence evidence.
        public const string OwnerSeedCheckpointLoadCase = "owner-seed-checkpoint-load";
        // The qualified seven-actor F09 Manual is owner-seed-derived fixture
        // preparation, never acquired-Manual ancestry. A distinct two-pair
        // persistence pair exercises its now-declared ordinary Small profile.
        public const string SmallFixtureSaveCase = "small-fixture-persistence-save";
        public const string SmallFixtureLoadCase = "small-fixture-persistence-load";
        // Diagnostic-only, zero-write derivative of the exact unmounted F09
        // Manual. It proves one additional native Horse before six-pair setup.
        public const string SmallThirdHorseCase = "small-fixture-third-horse-setup";
        // A separate one-Manual derivative of the same immutable seven-actor source.
        // The zero-write diagnostic above retains its original authority and evidence.
        public const string SmallThirdHorseSaveCase = "small-fixture-third-horse-persistence-save";
        public const string SmallFixtureManualName = "KMC_C7_SMALL_WORLD";
        // The accepted three-Horse Manual is a distinct eight-actor source.
        // This zero-write derivative advances Valerie by native XP/ordinary
        // Ranger commitments; it is neither the seven-actor Small source nor
        // permission for a four-pair archive or ordinary higher capacity.
        public const string FourthHorseCase = "third-horse-fixture-fourth-horse-setup";
        public const string FourthHorseSaveCase = "third-horse-fixture-fourth-horse-persistence-save";
        // CreateNewSave makes a repeated name unique in the native inventory.
        // The source Manual already owns SmallFixtureManualName.
        public const string FourthHorseManualName = "KMC_C7_FOUR_HORSE_WORLD";
        public const string FourthHorseSourceRun = "20261021-c7b-world171-b171s1-thirdhorse-manual";
        public const string FourthHorseSourceArtifact = "57ab380ae5c4c95fbed1647446b3ca264f7e140c91a3b7ea4bb41037085450b1";
        public const string FourthHorseSourceArchive = "f70a54865c86a599c3b25f6f2574abde2f663cd07b82472eac4bcbe245ea8c84";
        public const string SixPairStressCase = "six-horse-cold-fixture-lifecycle-stress";
        // The genuine six-mounted native Manual of the same cold-qualified fixture,
        // and its separate-process cold consumer. Source* stays the batch185 cold
        // qualification of the unmounted Manual308; the consumer's Checkpoint* pins
        // the mounted producer run and its persistence evidence.
        public const string SixPairSaveCase = "six-horse-mounted-persistence-save";
        public const string SixPairLoadCase = "six-horse-mounted-persistence-load";
        public const string SixPairManualName = "KMC_C7_SIX_MOUNTED_WORLD";
        // The producer's reservation is the source Manual308's number plus one.
        public const string SixPairManualLeaf = "Manual_309_" + SixPairManualName + ".zks";
        public const string SixPairSourceRun = "20261022-c7b-world185-b185s1-sixthhorse-cold";
        public const string SixPairSourceArtifact = "012d1caff175d34614fcd255f0b6ad897e0ef1a9650791cb3c0d55ce13b47241";
        public const string SixPairSourceArchive = "b1e817a60c8bbe35b5947b4eab3b3f35060493ca20873d5de5fdf8a81efbc291";
        // Exact source-observed roles, never created or inferred from selection.
        internal static string[] SixPairRoles => new[] {
            "73681ef2-b281-4bc6-bc05-2de68c1616cb", "30b79228-3fe5-48ac-aa88-2de9085837b7",
            "c4e2c853-4b72-4904-ad84-d0b7bfba7470", "0e0c7d2d-06b4-4510-9f81-d847340d1424",
            "fd213210-71cd-41b0-b35b-68e1b18e9b43", "da28f0a7-6eb0-4392-b2e0-f401a889b3aa",
            "5dd7bd1d-ae4c-4df7-9687-50fceb027344", "e486e6e9-aa36-423e-9d9e-9dfbe4a94a07",
            "82deb3d9-d734-4843-81c6-384becf082d9", "1ed01148-1646-469c-a3d5-153df01192b8",
            "d8c2f840-9d59-4b5e-9250-c628a36c5589", "130fd1b5-c9f4-4b9f-80de-6980e0d21556" };
        public const string FourthHorseColdArtifact = "4439fbea3608dad76634174b555f9d4ea243f2b4d0480029b03b7de66f9c8766";
        public const string FourthHorsePriorRider = "fd213210-71cd-41b0-b35b-68e1b18e9b43";
        public const string FourthHorsePriorMount = "da28f0a7-6eb0-4392-b2e0-f401a889b3aa";
        public const string FourthHorseTargetRider = "5dd7bd1d-ae4c-4df7-9687-50fceb027344";
        // Distinct paid Medium Human/Horse fixture, with its own completed
        // native Manual and separate-process cold ancestry (batch114).
        public const string MediumFixtureSaveCase = "medium-fixture-persistence-save";
        public const string MediumFixtureLoadCase = "medium-fixture-persistence-load";
        public const string MediumFixtureManualName = "KMC_C7_MEDIUM_WORLD";
        public const string MediumFixtureProducerRun = "20261021-c7b-world118-b114s1-medium-paid";
        public const string MediumFixtureProducerArtifact = "c890e8b2e5aa196407dda420d6b69deb5173ade26afe74e456a827ce1148cd4b";
        public const string MediumFixtureProducerArchive = "d25aa931174027b1f1f661db8f5a0db3054fcbe89e930ada1747f327e41a65e3";
        public const string MediumFixtureColdArtifact = "9183cd19a9df95678eda235a632701a429b57d95bfd8fdb7eb6a5eeef95b6b17";
        public const string MediumFixtureMainHorse = "a3e58763-2386-4ee7-a96d-8ccc21123b22";
        public const string MediumFixtureRider = "93f5f59d-403a-4ab2-a5e2-f91ff4d57f5f";
        public const string MediumFixtureHorse = "af4e8966-4d8d-4573-a16c-c2b402a71693";
        internal static bool IsSmallFixtureManualLeaf(string value)
        {
            const string prefix = "Manual_", suffix = "_" + SmallFixtureManualName + ".zks";
            if (value == null || !value.StartsWith(prefix, StringComparison.Ordinal) ||
                !value.EndsWith(suffix, StringComparison.Ordinal) || value.Length <= prefix.Length + suffix.Length ||
                !int.TryParse(value.Substring(prefix.Length, value.Length - prefix.Length - suffix.Length),
                    NumberStyles.None, CultureInfo.InvariantCulture, out var number) || number <= 0 || number == int.MaxValue)
                return false;
            return value == prefix + number.ToString(CultureInfo.InvariantCulture) + suffix;
        }
        internal static bool IsMediumFixtureManualLeaf(string value)
        {
            const string prefix = "Manual_", suffix = "_" + MediumFixtureManualName + ".zks";
            if (value == null || !value.StartsWith(prefix, StringComparison.Ordinal) ||
                !value.EndsWith(suffix, StringComparison.Ordinal) || value.Length <= prefix.Length + suffix.Length ||
                !int.TryParse(value.Substring(prefix.Length, value.Length - prefix.Length - suffix.Length),
                    NumberStyles.None, CultureInfo.InvariantCulture, out var number) || number <= 0 || number == int.MaxValue)
                return false;
            return value == prefix + number.ToString(CultureInfo.InvariantCulture) + suffix;
        }
        public const string SmallFixtureProducerRun = "20261020-c7b-world71-b53s1-small";
        public const string SmallFixtureProducerArtifact = "9d26bd146970fc53192bf2f28cef8c6655a7c79beb1326c075fec0a5c406e52f";
        public const string SmallFixtureProducerArchive = "ddf2b9cf4c0c0a49c93a659d2628ae6622f02e0a400a2df287bd5713f12ed829";
        public const string SmallFixtureColdReceipt = "d188012fb1f9a19410a572b67381bfb15be5e813a215a79196ed734b49c59bfc";
        public const string SmallFixtureMainHorse = "30b79228-3fe5-48ac-aa88-2de9085837b7";
        public const string SmallFixtureRider = "c4e2c853-4b72-4904-ad84-d0b7bfba7470";
        public const string SmallFixtureHorse = "0e0c7d2d-06b4-4510-9f81-d847340d1424";
        // The checkpoints the owner-seed checkpoint pair admits; extend this one list as each
        // further family is connected.
        internal static readonly string[] OwnerSeedCheckpoints = { "multi-exploration-quick", "multi-exploration-auto",
            "multi-rt-debt-manual", "multi-tb-debt-manual", "zero-mounted-tb-retained-manual", "legacy-mounted-pending-tb-manual", "multi-area-autosave" };
        internal const string ManticoreFixtureCheckpoint = "owner-seed-manticore-fixture-manual";
        internal const string TombFixtureCheckpoint = "owner-seed-tomb-door-fixture-manual";
        internal const string ThornRiverFixtureCheckpoint = "owner-seed-thorn-river-door-fixture-manual";
        internal const string ManticoreManualColdCheckpoint = "manticore-fixture-cold-manual";
        internal const string ManticoreFixtureManualName = "KMC_C7_AREA_FIXTURE";
        internal const string ManticoreFixtureArea = "314e8c3b15b896a4f847c85f9250f02b";
        internal const string ManticoreReturnRoute = "manticore-to-spider-v1";
        internal const string ManticoreReturnEntry = "00becd5302e6d1d4a8ad93b6c612b927";
        internal const string ManticoreReturnArea = "b10e86516b6437d45a629931c30936fe";
        internal const string ManticoreFixtureManualSha256 = "86a5ff4bc18852f48388156f627ddebf1c9047ecf69f73a1b4b3b92b231f1b30";
        internal const string ManticoreFixtureSourceSha256 = "b4d1ede7843c600f201dcd7b71cd2558cad44210e7e0eb78d9a58f6e89f64003";
        internal const string TombFixtureSourceSha256 = ManticoreFixtureSourceSha256;
        internal const string TombFixtureManualName = "KMC_C7_TOMB_DOOR_FIXTURE";
        internal const string TombFixtureArea = "81b13ce8765469045864ffb878b0944b";
        internal const string ThornRiverFixtureSourceSha256 = ManticoreFixtureSourceSha256;
        internal const string ThornRiverFixtureManualName = "KMC_C7_THORN_RIVER_DOOR_FIXTURE";
        internal const string ThornRiverFixtureArea = "7d72372ade2f14448a3fec6d61e06bd8";
        internal static bool IsManticoreManualCold(string checkpoint) => checkpoint == ManticoreManualColdCheckpoint;
        // Matrix rows 58-59 (owner decision, b195 x4c): the one admitted capital route. Native
        // IsDirectlyControllable in a capital admits only the main character and its pet, so the
        // non-main pair is retired on arrival. Every other case keeps refusing capitals.
        internal const string CapitalArrivalCheckpoint = "multi-exploration-capital-arrival-manual";
        internal const string CapitalArrivalColdCheckpoint = "multi-exploration-capital-arrival-cold-manual";
        internal const string CapitalArrivalManualName = "KMC_C7_CAPITAL";
        // Oleg's Trading Post, observed destinationIsCapital=true (run 20261019-c7b-world48-ownerseed-b23s3-areaauto).
        internal const string CapitalArrivalEntry = "fd225a93828d26449803a4847947f8c1";
        internal const string CapitalArrivalArea = "ead426a6c23d39548a670ee515d77df4";
        internal static bool CapitalCheckpoint(string checkpoint) => checkpoint == CapitalArrivalCheckpoint || checkpoint == CapitalArrivalColdCheckpoint;
        internal static bool IsOwnerSeedSourceOnlyCheckpoint(string checkpoint) => SupportedSaveBoundary(checkpoint) || OwnedSchema1Source(checkpoint) || checkpoint == ManticoreFixtureCheckpoint || checkpoint == TombFixtureCheckpoint || checkpoint == ThornRiverFixtureCheckpoint;
        // The connected combat checkpoints: genuine native Manuals written mid-combat from the restored
        // two-pair start under the owner-approved diagnostic target. Their native Manual name is distinct
        // from every acquired-ancestry KMC_C7_WORLD and from the derived KMC_C7_SEED_WORLD input.
        public const string OwnerSeedCheckpointManualName = "KMC_C7_SEED_CHECKPOINT";
        internal static bool IsOwnerSeedCombatCheckpoint(string checkpoint) =>
            checkpoint == "multi-rt-debt-manual" || checkpoint == "multi-tb-debt-manual" || checkpoint == "zero-mounted-tb-retained-manual" ||
            checkpoint == "legacy-mounted-pending-tb-manual";
        // The native Manual name a writing owned selection reserves; a non-Manual selection writes its slot name.
        internal string OwnedManualOutputName(string checkpoint) => IsSixPairSave ? SixPairManualName : IsFourthHorseSave ? FourthHorseManualName : IsSmallFixtureSave || IsSmallThirdHorseSave ? SmallFixtureManualName : IsMediumFixtureSave ? MediumFixtureManualName : IsOwnerSeedManual ? OwnerSeedManualName :
            IsOwnerSeedCheckpointSave ? (checkpoint == ManticoreFixtureCheckpoint ? ManticoreFixtureManualName : checkpoint == TombFixtureCheckpoint ? TombFixtureManualName : checkpoint == ThornRiverFixtureCheckpoint ? ThornRiverFixtureManualName : OwnerSeedCheckpointManualName) : Cold ? (OwnedSchema1Cold(checkpoint) ? "KMC_C7_SCHEMA1" : IsCapitalArrivalProducer(checkpoint) ? CapitalArrivalManualName : "KMC_C7_SCHEMA2") : "KMC_C7_WORLD";
        // Only the generic acquired-ancestry cold consumer admits the capital pair of checkpoints.
        internal bool IsCapitalArrivalProducer(string checkpoint) => Case == "persistence-load" && checkpoint == CapitalArrivalCheckpoint;
        internal bool IsCapitalArrivalCold(string checkpoint) => Case == "persistence-load" && checkpoint == CapitalArrivalColdCheckpoint;
        // The exact area the staged generic two-owner acquired Manual must record as written in. Every case
        // loads in that area except the owned area autosave pair (its original Manual area) and row 59, which
        // loads row 58's Manual in the capital: its lineage area is the selection's SourceArea, which the
        // runner binds to row 58's recorded capital-arrival source area and the receipt's own write area.
        internal string AcquiredLineageArea(string checkpoint, string loadArea) =>
            checkpoint == "multi-area-autosave" || IsCapitalArrivalCold(checkpoint) ? SourceArea : loadArea;
        public string Case { get; set; }
        public string SourceRunId { get; set; }
        public string SourceArtifactSha256 { get; set; }
        public string SourceArchiveSha256 { get; set; }
        public string CheckpointSourceRunId { get; set; }
        public string CheckpointSourceArtifactSha256 { get; set; }
        public string SeedId { get; set; }
        public string SeedSha256 { get; set; }
        public string ColdReceiptSha256 { get; set; }
        public string RiderAId { get; set; }
        public string MountAId { get; set; }
        public string RiderBId { get; set; }
        public string MountBId { get; set; }
        public string[] PartyIds { get; set; }
        public Chunk7OwnerSeedXpAssistance XpAssistance { get; set; }
        public string SourceArea { get; set; }
        // Present only on the distinct Manticore Manual -> visited Spider Cave area route.
        public string AreaRoute { get; set; }
        // Scenario 3 only: the unrelated companion whose one ordinary native action is observed.
        public string CompanionId { get; set; }
        // Keep every historical serialized selection byte shape unchanged.
        public bool ShouldSerializeSourceArea() => SourceArea != null;
        public bool ShouldSerializeAreaRoute() => AreaRoute != null;
        public bool ShouldSerializeCompanionId() => CompanionId != null;
        public bool ShouldSerializeSourceRunId() => !OwnerSeedSetup || SourceRunId != null;
        public bool ShouldSerializeSourceArtifactSha256() => !OwnerSeedSetup || SourceArtifactSha256 != null;
        public bool ShouldSerializeSourceArchiveSha256() => !OwnerSeedSetup || SourceArchiveSha256 != null;
        public bool ShouldSerializeCheckpointSourceRunId() => !OwnerSeedSetup && !IsOwnerSeedDerivedLoad || CheckpointSourceRunId != null;
        public bool ShouldSerializeCheckpointSourceArtifactSha256() => !OwnerSeedSetup && !IsOwnerSeedDerivedLoad || CheckpointSourceArtifactSha256 != null;
        public bool ShouldSerializeMountAId() => !OwnerSeedSetup || MountAId != null;
        public bool ShouldSerializeMountBId() => !OwnerSeedSetup || MountBId != null;
        public bool ShouldSerializeSeedId() => SeedId != null;
        public bool ShouldSerializeSeedSha256() => SeedSha256 != null;
        public bool ShouldSerializeColdReceiptSha256() => ColdReceiptSha256 != null;
        // The owner-seed shape always names its assistance key (null when rider B needs none).
        public bool ShouldSerializeXpAssistance() => OwnerSeedSetup || XpAssistance != null;
        public bool BaseGameOnly { get; set; }
        public int ExpectedNativeLoadCount { get; set; }
        public int ExpectedNativeWriteCount { get; set; }
        internal bool IsOwnerSeed => Case == OwnerSeedCase;
        internal bool IsOwnerSeedManual => Case == OwnerSeedManualCase;
        internal bool IsOwnerSeedDerivedLoad => Case == OwnerSeedLoadCase;
        internal bool IsOwnerSeedLoadRefusal => Case == OwnerSeedLoadRefusalCase;
        // Both kinds that load the owner's seed and run its disclosed native setup.
        internal bool OwnerSeedSetup => IsOwnerSeed || IsOwnerSeedManual;
        internal bool IsOwnerSeedDerivedCombat => Case == OwnerSeedRealtimeCase || Case == OwnerSeedRiderFirstCase || Case == OwnerSeedMountFirstCase || Case == OwnerSeedDisableCase ||
            Case == OwnerSeedNativeLifeCase || Case == OwnerSeedNativeDeathCase;
        internal bool IsOwnerSeedCheckpointSave => Case == OwnerSeedCheckpointSaveCase;
        internal bool IsOwnerSeedCheckpointLoad => Case == OwnerSeedCheckpointLoadCase;
        internal bool IsSmallFixtureSave => Case == SmallFixtureSaveCase;
        internal bool IsSmallFixtureLoad => Case == SmallFixtureLoadCase;
        internal bool IsSmallThirdHorse => Case == SmallThirdHorseCase;
        internal bool IsSmallThirdHorseSave => Case == SmallThirdHorseSaveCase;
        internal bool SmallThirdHorseSetup => IsSmallThirdHorse || IsSmallThirdHorseSave;
        internal bool SmallFixtureLineage => IsSmallFixtureSave || IsSmallFixtureLoad || SmallThirdHorseSetup;
        internal bool IsFourthHorseSave => Case == FourthHorseSaveCase;
        internal bool IsSixPairStress => Case == SixPairStressCase;
        internal bool IsSixPairSave => Case == SixPairSaveCase;
        internal bool IsSixPairLoad => Case == SixPairLoadCase;
        internal bool SixPairPersistence => IsSixPairSave || IsSixPairLoad;
        // Every kind whose fixture is the twelve-actor cold-qualified six-Horse field.
        internal bool SixPairLineage => IsSixPairStress || SixPairPersistence;
        internal bool IsFourthHorse => Case == FourthHorseCase || IsFourthHorseSave;
        internal bool IsMediumFixtureSave => Case == MediumFixtureSaveCase;
        internal bool IsMediumFixtureLoad => Case == MediumFixtureLoadCase;
        internal bool MediumFixtureLineage => IsMediumFixtureSave || IsMediumFixtureLoad;
        internal bool PaidFixtureLineage => SmallFixtureLineage || MediumFixtureLineage;
        internal bool IsManticoreReturnArea => AreaRoute == ManticoreReturnRoute;
        // Every kind whose native input is the owner-seed-derived Manual itself.
        internal bool OwnerSeedDerivedInput => IsOwnerSeedDerivedLoad || IsOwnerSeedDerivedCombat || IsOwnerSeedCheckpointSave || IsOwnerSeedLoadRefusal;
        // Every kind whose staged source receipt is the owner-seed Manual producer's world evidence.
        internal bool OwnerSeedDerivedLineage => OwnerSeedDerivedInput || IsOwnerSeedCheckpointLoad;
        // Kinds whose receipts may come from an earlier candidate, each pinned to its own recorded identity.
        internal bool CrossCandidateAllowed => IsOwnerSeedDerivedCombat || IsOwnerSeedCheckpointSave || IsOwnerSeedCheckpointLoad || IsOwnerSeedLoadRefusal;
        internal bool CrossCandidateAllowedFor(string checkpoint) => CrossCandidateAllowed || IsOwnerSeedDerivedLoad &&
            (checkpoint == "multi-exploration-three-loads-manual" || IsManticoreManualCold(checkpoint));
        internal const int MaximumSetupLevelupWrites = 2;
        internal string[] PairIds => SixPairLineage ? SixPairRoles : new[] { RiderAId, MountAId, RiderBId, MountBId };
        internal string RuntimeScenario
        {
            get
            {
                switch (Case)
                {
                    case "exploration": return "chunk7-two-pair-exploration";
                    case OwnerSeedCase: return "chunk7-two-pair-exploration";
                    case OwnerSeedManualCase: return "chunk7-two-pair-exploration";
                    case OwnerSeedLoadCase: return "chunk7-persistence-load";
                    case OwnerSeedLoadRefusalCase: return "chunk7-persistence-load";
                    case OwnerSeedRealtimeCase: return "chunk7-two-pair-realtime-core";
                    case OwnerSeedRiderFirstCase: return "chunk7-two-pair-combat-rider-first";
                    case OwnerSeedMountFirstCase: return "chunk7-two-pair-combat-mount-first";
                    case OwnerSeedDisableCase: return "chunk7-two-pair-combat-rider-first";
                    case OwnerSeedNativeLifeCase: return "chunk7-two-pair-native-life-cleanup";
                    case OwnerSeedNativeDeathCase: return "chunk7-two-pair-native-life-cleanup";
                    case OwnerSeedCheckpointSaveCase: return "chunk7-persistence-save";
                    case OwnerSeedCheckpointLoadCase: return "chunk7-persistence-load";
                    case SmallFixtureSaveCase: return "chunk7-persistence-save";
                    case SmallFixtureLoadCase: return "chunk7-persistence-load";
                    case SmallThirdHorseCase: return "chunk7-two-pair-exploration";
                    case SmallThirdHorseSaveCase: return "chunk7-two-pair-exploration";
                    case FourthHorseCase: return "chunk7-two-pair-exploration";
                    case FourthHorseSaveCase: return "chunk7-two-pair-exploration";
                    case SixPairStressCase: return "chunk7-two-pair-exploration";
                    case SixPairSaveCase: return "chunk7-persistence-save";
                    case SixPairLoadCase: return "chunk7-persistence-load";
                    case MediumFixtureSaveCase: return "chunk7-persistence-save";
                    case MediumFixtureLoadCase: return "chunk7-persistence-load";
                    case "combat-rider-first": return "chunk7-two-pair-combat-rider-first";
                    case "combat-mount-first": return "chunk7-two-pair-combat-mount-first";
                    case "realtime-core": return "chunk7-two-pair-realtime-core";
                    case "realtime-casting": return "chunk7-two-pair-realtime-casting";
                    case "realtime-charge": return "chunk7-two-pair-realtime-charge";
                    case "native-life-cleanup": return "chunk7-two-pair-native-life-cleanup";
                    case "persistence-save": return "chunk7-persistence-save";
                    case "persistence-load": return "chunk7-persistence-load";
                    default: return null;
                }
            }
        }
        internal bool Persistence => Case == "persistence-save" || Case == "persistence-load" || IsOwnerSeedManual || IsOwnerSeedDerivedLoad ||
            IsOwnerSeedCheckpointSave || IsOwnerSeedCheckpointLoad || IsOwnerSeedLoadRefusal || IsSmallFixtureSave || IsSmallFixtureLoad || IsSmallThirdHorseSave || IsFourthHorseSave || MediumFixtureLineage ||
            SixPairPersistence;
        internal bool Cold => Case == "persistence-load" || IsOwnerSeedDerivedLoad || IsOwnerSeedCheckpointLoad || IsOwnerSeedLoadRefusal || IsSmallFixtureLoad || IsMediumFixtureLoad ||
            IsSixPairLoad;
        internal static bool SupportedCheckpoint(string value) => Array.IndexOf(new[] {
            "multi-exploration-manual", "multi-exploration-quick", "multi-exploration-auto",
            "multi-rt-debt-manual", "multi-tb-debt-manual", "zero-mounted-tb-retained-manual",
            "legacy-mounted-pending-tb-manual", "multi-area-autosave" }, value) >= 0;
        internal static bool OwnedSchema1Source(string value) => value == "owned-schema1-manual";
        internal static bool OwnedSchema1Cold(string value) => value == "owned-schema1-resave-manual";
        internal static bool CurrentMigrationCheckpoint(string value) => value == "owned-schema1-current-manual" ||
            value == "owned-schema2-pending-current-manual";
        internal static bool IsOwnerSeedLoadOnlyCheckpoint(string value) => OwnedSchema1Cold(value) || CurrentMigrationCheckpoint(value) ||
            value == "pending-schema3-control-manual";
        internal static bool SupportedSaveBoundary(string value) => value == "multi-nonselected-mount-unsettled-manual" ||
            value == "multi-owned-worker-cancel-manual";
        internal static bool RepeatedCheckpoint(string value) => value == "multi-exploration-three-loads-manual" ||
            value == "multi-exploration-three-area-transfers-manual";
        // A cold archive can still be an exploration field load. Only the combat
        // consumers may skip the ordinary idle/empty-command readiness check.
        internal bool RequiresCombatColdBind(string checkpoint) => Cold &&
            checkpoint != "multi-exploration-manual" && checkpoint != "multi-exploration-quick" &&
            checkpoint != "multi-exploration-auto" && checkpoint != "multi-area-autosave" &&
            !RepeatedCheckpoint(checkpoint) && !IsManticoreManualCold(checkpoint) && !CapitalCheckpoint(checkpoint) &&
            !OwnedSchema1Cold(checkpoint) && checkpoint != "owned-schema1-current-manual" &&
            !IsOwnerSeedLoadRefusal;
        internal int LoadCount(string checkpoint) => Cold && checkpoint == "multi-exploration-three-loads-manual" ? 3 : 1;
        // Owner seed: exactly the native setup level-up Autos its selection declares (bound at admission).
        // Owner-seed Manual producer: exactly its one measured Manual (no setup write exists).
        internal int WriteCount(string checkpoint) => IsOwnerSeed ? ExpectedNativeWriteCount : IsOwnerSeedManual || IsSmallFixtureSave || IsSmallThirdHorseSave || IsFourthHorseSave || IsMediumFixtureSave || IsSixPairSave ? 1 :
            IsOwnerSeedCheckpointSave ? (checkpoint == "multi-owned-worker-cancel-manual" ? 2 : 1) :
            IsOwnerSeedCheckpointLoad ? (checkpoint == "legacy-mounted-pending-tb-manual" || OwnedSchema1Cold(checkpoint) ? 1 : 0) :
            Case == "persistence-save" && checkpoint == "multi-owned-worker-cancel-manual" ? 2 : Case == "persistence-save" ||
            Cold && (checkpoint == "legacy-mounted-pending-tb-manual" || OwnedSchema1Cold(checkpoint)) || IsCapitalArrivalProducer(checkpoint) ? 1 : 0;
        public IReadOnlyList<string> Validate(string scenario, string runId, string checkpoint)
        {
            if (AreaRoute != null && (!(IsOwnerSeedCheckpointSave || IsOwnerSeedCheckpointLoad) ||
                checkpoint != "multi-area-autosave" || !IsManticoreReturnArea))
                return new[] { "Only the exact owner-seed checkpoint area pair admits the distinct Manticore return route." };
            if (OwnerSeedSetup) return ValidateOwnerSeed(scenario, runId, checkpoint);
            if (IsOwnerSeedDerivedLoad) return ValidateOwnerSeedDerivedLoad(scenario, runId, checkpoint);
            if (IsOwnerSeedLoadRefusal)
            {
                var refusalErrors = DerivedInputErrors(scenario, runId);
                if (!LoadRefusalCheckpoint(checkpoint) || CompanionId != null || !Run(CheckpointSourceRunId) ||
                    CheckpointSourceRunId == runId || CheckpointSourceRunId == SourceRunId || CheckpointSourceRunId == SeedId ||
                    !Hash(CheckpointSourceArtifactSha256) || CheckpointSourceArtifactSha256 == SourceArtifactSha256 ||
                    CheckpointSourceArtifactSha256 == SourceArchiveSha256 || CheckpointSourceArtifactSha256 == SeedSha256)
                    refusalErrors.Add("Load refusal requires only its exact selected checkpoint and distinct accepted owner-seed cold-consumer pins.");
                return refusalErrors;
            }
            if (IsOwnerSeedDerivedCombat) return ValidateOwnerSeedDerivedCombat(scenario, runId, checkpoint);
            if (IsOwnerSeedCheckpointSave || IsOwnerSeedCheckpointLoad) return ValidateOwnerSeedCheckpoint(scenario, runId, checkpoint);
            if (IsFourthHorse) return ValidateFourthHorse(scenario, runId, checkpoint);
            if (IsSixPairStress) return ValidateSixPairStress(scenario, runId, checkpoint);
            if (SixPairPersistence) return ValidateSixPairPersistence(scenario, runId, checkpoint);
            if (SmallFixtureLineage) return ValidateSmallFixture(scenario, runId, checkpoint);
            if (MediumFixtureLineage) return ValidateMediumFixture(scenario, runId, checkpoint);
            var errors = new List<string>();
            if (CompanionId != null) errors.Add("Only owner-seed-derived RT combat names an unrelated companion.");
            if (SeedId != null || SeedSha256 != null || XpAssistance != null)
                errors.Add("Producer-backed owned pairs cannot carry owner-seed pins or setup assistance.");
            if (scenario != Scenario || RuntimeScenario == null) errors.Add("Owned pairs require the separately closed native campaign family and case.");
            if (!Run(SourceRunId) || SourceRunId == runId || !Hash(SourceArtifactSha256) || !Hash(SourceArchiveSha256))
                errors.Add("The exact distinct successful two-owner acquired Manual receipt and native bytes are required.");
            if (!PairIds.All(Chunk7ManualDerivedSelection.NativeId) || PairIds.Distinct(StringComparer.Ordinal).Count() != 4)
                errors.Add("Four distinct observed native owner/pet IDs are required.");
            if (PartyIds == null || PartyIds.Length < 4 || PartyIds.Length > 12 ||
                !PartyIds.All(Chunk7ManualDerivedSelection.NativeId) || PartyIds.Distinct(StringComparer.Ordinal).Count() != PartyIds.Length ||
                PairIds.Any(id => !PartyIds.Contains(id))) errors.Add("The bounded full native field roster must contain both exact pairs once.");
            if (Persistence ? !SupportedCheckpoint(checkpoint) && !(Cold && (RepeatedCheckpoint(checkpoint) || OwnedSchema1Cold(checkpoint))) &&
                !IsCapitalArrivalProducer(checkpoint) && !IsCapitalArrivalCold(checkpoint) && (Cold || !SupportedSaveBoundary(checkpoint) && !OwnedSchema1Source(checkpoint)) : checkpoint != null)
                errors.Add("Owned-pair cold persistence requires a core checkpoint; the two save boundaries are source-only.");
            var area = checkpoint == "multi-area-autosave";
            if (area ? !Regex.IsMatch(SourceArea ?? "", "^[a-f0-9]{32}$") || SourceArea == new string('0', 32) : SourceArea != null && !IsCapitalArrivalCold(checkpoint))
                errors.Add("Only the owned area checkpoint requires its exact original acquired Manual area.");
            // Row 59 loads in the capital; its acquired lineage Manual was written outside it, in exactly SourceArea.
            if (IsCapitalArrivalCold(checkpoint) && (!Regex.IsMatch(SourceArea ?? "", "^[a-f0-9]{32}$") || SourceArea == new string('0', 32) ||
                SourceArea == CapitalArrivalArea))
                errors.Add("The capital cold consumer requires its exact non-capital acquired lineage Manual area.");
            if (Cold ? !Run(CheckpointSourceRunId) || CheckpointSourceRunId == runId || CheckpointSourceRunId == SourceRunId ||
                !Hash(CheckpointSourceArtifactSha256) : CheckpointSourceRunId != null || CheckpointSourceArtifactSha256 != null)
                errors.Add("Only cold persistence consumes an exact separate successful owned-pair source checkpoint.");
            if (!BaseGameOnly || ExpectedNativeLoadCount != LoadCount(checkpoint) || ExpectedNativeWriteCount != WriteCount(checkpoint))
                errors.Add("Owned pairs require base-only staging and the exact selected native load/write quotas.");
            return errors;
        }
        private IReadOnlyList<string> ValidateFourthHorse(string scenario, string runId, string checkpoint)
        {
            var errors = new List<string>();
            if (scenario != Scenario || checkpoint != (IsFourthHorseSave ? OwnerSeedCheckpoint : null))
                errors.Add("Fourth-Horse setup and Manual are distinct closed exploration cases.");
            if (SourceRunId != FourthHorseSourceRun || SourceArtifactSha256 != FourthHorseSourceArtifact ||
                SourceArchiveSha256 != FourthHorseSourceArchive || ColdReceiptSha256 != FourthHorseColdArtifact ||
                SeedId != Chunk7SmallFixtureSelection.SeedIdentity || SeedSha256 != Chunk7SmallFixtureSelection.SeedHash ||
                runId == SourceRunId || runId == SeedId)
                errors.Add("Fourth-Horse setup requires the accepted eight-actor producer and original-PASS cold lineage.");
            if (RiderAId != Chunk7SmallFixtureSelection.MainActorId || MountAId != SmallFixtureMainHorse ||
                RiderBId != SmallFixtureRider || MountBId != SmallFixtureHorse ||
                PartyIds == null || PartyIds.Length != 8 ||
                !PartyIds.All(Chunk7ManualDerivedSelection.NativeId) ||
                PartyIds.Distinct(StringComparer.Ordinal).Count() != 8 ||
                !new[] { RiderAId, MountAId, RiderBId, MountBId, FourthHorsePriorRider,
                    FourthHorsePriorMount, FourthHorseTargetRider, "8265f8cf-0969-4ec7-a266-2448de91c8c4" }
                    .OrderBy(id => id, StringComparer.Ordinal)
                    .SequenceEqual(PartyIds.OrderBy(id => id, StringComparer.Ordinal)))
                errors.Add("Fourth-Horse setup requires all eight exact native field actors, including three inherited Horses.");
            if (!BaseGameOnly || ExpectedNativeLoadCount != 1 || ExpectedNativeWriteCount != (IsFourthHorseSave ? 1 : 0) ||
                CheckpointSourceRunId != null || CheckpointSourceArtifactSha256 != null ||
                SourceArea != null || AreaRoute != null || CompanionId != null || XpAssistance != null)
                errors.Add("Fourth-Horse selection permits one base-only native load, its exact selected write count and no alternate target.");
            return errors;
        }
        private IReadOnlyList<string> ValidateSixPairStress(string scenario, string runId, string checkpoint)
        {
            var errors = new List<string>(); var roles = SixPairRoles;
            if (scenario != Scenario || checkpoint != null || !Run(runId) || runId == SixPairSourceRun ||
                SourceRunId != SixPairSourceRun || SourceArtifactSha256 != SixPairSourceArtifact || SourceArchiveSha256 != SixPairSourceArchive)
                errors.Add("Six-pair stress requires the exact accepted batch185 cold checkpoint and distinct run.");
            if (RiderAId != roles[0] || MountAId != roles[1] || RiderBId != roles[2] || MountBId != roles[3] ||
                PartyIds == null || !PartyIds.OrderBy(id => id, StringComparer.Ordinal).SequenceEqual(roles.OrderBy(id => id, StringComparer.Ordinal)))
                errors.Add("Six-pair stress requires all twelve exact sourced native roles once.");
            if (!BaseGameOnly || ExpectedNativeLoadCount != 1 || ExpectedNativeWriteCount != 0 ||
                CheckpointSourceRunId != null || CheckpointSourceArtifactSha256 != null || ColdReceiptSha256 != null ||
                SeedId != null || SeedSha256 != null || XpAssistance != null || SourceArea != null || AreaRoute != null || CompanionId != null)
                errors.Add("Six-pair stress admits one base-only load, zero saves and no fixture/progression assistance.");
            return errors;
        }
        // The producer and its cold consumer keep the batch185 cold qualification as
        // fixture provenance. Only the consumer pins a mounted producer, which must be
        // distinct from itself, the fixture run and both fixture hashes.
        private IReadOnlyList<string> ValidateSixPairPersistence(string scenario, string runId, string checkpoint)
        {
            var errors = new List<string>(); var roles = SixPairRoles;
            if (scenario != Scenario || checkpoint != OwnerSeedCheckpoint || !Run(runId) || runId == SixPairSourceRun ||
                SourceRunId != SixPairSourceRun || SourceArtifactSha256 != SixPairSourceArtifact || SourceArchiveSha256 != SixPairSourceArchive)
                errors.Add("Six-pair persistence requires the exact accepted batch185 cold checkpoint, its exploration Manual checkpoint and a distinct run.");
            if (RiderAId != roles[0] || MountAId != roles[1] || RiderBId != roles[2] || MountBId != roles[3] ||
                PartyIds == null || PartyIds.Length != 12 ||
                !PartyIds.OrderBy(id => id, StringComparer.Ordinal).SequenceEqual(roles.OrderBy(id => id, StringComparer.Ordinal)))
                errors.Add("Six-pair persistence requires all twelve exact sourced native roles once.");
            if (!BaseGameOnly || ExpectedNativeLoadCount != 1 || ExpectedNativeWriteCount != (IsSixPairSave ? 1 : 0) ||
                ColdReceiptSha256 != null || SeedId != null || SeedSha256 != null || XpAssistance != null ||
                SourceArea != null || AreaRoute != null || CompanionId != null)
                errors.Add("Six-pair persistence admits one base-only load, one producer Manual or zero cold writes, and no fixture/progression assistance.");
            if (IsSixPairSave ? CheckpointSourceRunId != null || CheckpointSourceArtifactSha256 != null :
                !Run(CheckpointSourceRunId) || CheckpointSourceRunId == runId || CheckpointSourceRunId == SourceRunId ||
                !Hash(CheckpointSourceArtifactSha256) || CheckpointSourceArtifactSha256 == SourceArtifactSha256 ||
                CheckpointSourceArtifactSha256 == SourceArchiveSha256)
                errors.Add("Only the six-pair cold consumer carries a distinct completed six-mounted producer pin.");
            return errors;
        }
        private IReadOnlyList<string> ValidateSmallFixture(string scenario, string runId, string checkpoint)
        {
            var errors = new List<string>();
            if (scenario != Scenario || (IsSmallThirdHorse ? checkpoint != null : checkpoint != OwnerSeedCheckpoint))
                errors.Add("Small fixture persistence selects only its exact exploration Manual checkpoint.");
            if (SourceRunId != SmallFixtureProducerRun || SourceArtifactSha256 != SmallFixtureProducerArtifact ||
                SourceArchiveSha256 != SmallFixtureProducerArchive || ColdReceiptSha256 != SmallFixtureColdReceipt ||
                SeedId != Chunk7SmallFixtureSelection.SeedIdentity || SeedSha256 != Chunk7SmallFixtureSelection.SeedHash ||
                runId == SourceRunId || runId == SeedId)
                errors.Add("Small persistence requires its qualified owner-seed-derived F09 Manual and accepted cold receipt.");
            if (RiderAId != Chunk7SmallFixtureSelection.MainActorId || MountAId != SmallFixtureMainHorse ||
                RiderBId != SmallFixtureRider || MountBId != SmallFixtureHorse || PartyIds == null || PartyIds.Length != 7 ||
                !PartyIds.All(Chunk7ManualDerivedSelection.NativeId) || PartyIds.Distinct(StringComparer.Ordinal).Count() != 7 ||
                PairIds.Any(id => PartyIds == null || !PartyIds.Contains(id)))
                errors.Add("Small persistence requires the seven distinct qualified native field actors and exact Medium/Small pair roles.");
            if (!BaseGameOnly || ExpectedNativeLoadCount != 1 || ExpectedNativeWriteCount != (IsSmallFixtureSave || IsSmallThirdHorseSave ? 1 : 0) ||
                SourceArea != null || AreaRoute != null || CompanionId != null || XpAssistance != null)
                errors.Add("Small persistence permits only base-only one-load exploration, one producer Manual or zero cold writes, and no added setup.");
            if (IsSmallFixtureSave || SmallThirdHorseSetup ? CheckpointSourceRunId != null || CheckpointSourceArtifactSha256 != null :
                !Run(CheckpointSourceRunId) || CheckpointSourceRunId == runId || CheckpointSourceRunId == SourceRunId ||
                CheckpointSourceRunId == SeedId ||
                !Hash(CheckpointSourceArtifactSha256) || CheckpointSourceArtifactSha256 == SourceArtifactSha256 ||
                CheckpointSourceArtifactSha256 == SourceArchiveSha256)
                errors.Add("Only the Small cold consumer carries a distinct completed mounted-checkpoint producer pin.");
            return errors;
        }
        private IReadOnlyList<string> ValidateMediumFixture(string scenario, string runId, string checkpoint)
        {
            var errors = new List<string>();
            if (scenario != Scenario || checkpoint != OwnerSeedCheckpoint)
                errors.Add("Medium fixture persistence selects only its exact exploration Manual checkpoint.");
            if (SourceRunId != MediumFixtureProducerRun || SourceArtifactSha256 != MediumFixtureProducerArtifact ||
                SourceArchiveSha256 != MediumFixtureProducerArchive || ColdReceiptSha256 != MediumFixtureColdArtifact ||
                SeedId != Chunk7SmallFixtureSelection.SeedIdentity || SeedSha256 != Chunk7SmallFixtureSelection.SeedHash ||
                runId == SourceRunId || runId == SeedId)
                errors.Add("Medium persistence requires its qualified paid Human/Horse Manual and completed separate cold artifact.");
            if (RiderAId != Chunk7SmallFixtureSelection.MainActorId || MountAId != MediumFixtureMainHorse ||
                RiderBId != MediumFixtureRider || MountBId != MediumFixtureHorse || PartyIds == null || PartyIds.Length != 7 ||
                !PartyIds.All(Chunk7ManualDerivedSelection.NativeId) || PartyIds.Distinct(StringComparer.Ordinal).Count() != 7 ||
                PairIds.Any(id => !PartyIds.Contains(id)))
                errors.Add("Medium persistence requires the seven distinct qualified native field actors and exact Human/Horse roles.");
            if (!BaseGameOnly || ExpectedNativeLoadCount != 1 || ExpectedNativeWriteCount != (IsMediumFixtureSave ? 1 : 0) ||
                SourceArea != null || AreaRoute != null || CompanionId != null || XpAssistance != null)
                errors.Add("Medium persistence permits only base-only one-load exploration, one producer Manual or zero cold writes, and no added setup.");
            if (IsMediumFixtureSave ? CheckpointSourceRunId != null || CheckpointSourceArtifactSha256 != null :
                !Run(CheckpointSourceRunId) || CheckpointSourceRunId == runId || CheckpointSourceRunId == SourceRunId ||
                CheckpointSourceRunId == SeedId ||
                !Hash(CheckpointSourceArtifactSha256) || CheckpointSourceArtifactSha256 == SourceArtifactSha256 ||
                CheckpointSourceArtifactSha256 == SourceArchiveSha256)
                errors.Add("Only the Medium cold consumer carries a distinct completed mounted-checkpoint producer pin.");
            return errors;
        }
        // Static owner-seed validation. Rider size, petless state, eligibility and the
        // exact minimal XP target are re-checked against the loaded native actors by
        // Chunk7OwnerSeedSetupPlan before any setup mutation.
        private IReadOnlyList<string> ValidateOwnerSeed(string scenario, string runId, string checkpoint)
        {
            var errors = new List<string>();
            if (scenario != Scenario) errors.Add("Owner-seed two-pair exploration requires its exact closed native campaign family.");
            if (SourceRunId != null || SourceArtifactSha256 != null || SourceArchiveSha256 != null || CheckpointSourceRunId != null ||
                CheckpointSourceArtifactSha256 != null || MountAId != null || MountBId != null || SourceArea != null || CompanionId != null)
                errors.Add("Owner-seed two-pair exploration has no producer, checkpoint, area, companion or pre-granted pet pins.");
            if (!Run(SeedId) || SeedId == runId) errors.Add("An exact registered owner seed identity is required.");
            if (!Hash(SeedSha256)) errors.Add("An exact owner seed SHA256 is required.");
            if (!Chunk7ManualDerivedSelection.NativeId(RiderAId) || !Chunk7ManualDerivedSelection.NativeId(RiderBId) ||
                string.Equals(RiderAId, RiderBId, StringComparison.Ordinal))
                errors.Add("Two distinct observed native rider IDs are required.");
            if (PartyIds == null || PartyIds.Length < 2 || PartyIds.Length > 6 ||
                !PartyIds.All(Chunk7ManualDerivedSelection.NativeId) || PartyIds.Distinct(StringComparer.Ordinal).Count() != PartyIds.Length ||
                !PartyIds.Contains(RiderAId) || !PartyIds.Contains(RiderBId))
                errors.Add("The bounded full native party must contain both selected riders once.");
            if (XpAssistance != null && (XpAssistance.UnitId != RiderBId || XpAssistance.TargetCharacterLevel < 2 ||
                XpAssistance.TargetCharacterLevel > Chunk7OwnerSeedSetupPlan.MaximumTargetLevel))
                errors.Add("Native XP assistance may target only rider B, within the bounded setup ceiling.");
            if (IsOwnerSeedManual)
            {
                // The producer's only write is its one measured native Manual after SETUP-COMPLETE.
                if (checkpoint != OwnerSeedCheckpoint) errors.Add("Owner-seed-derived Manual production selects exactly the multi-exploration-manual checkpoint.");
                if (!BaseGameOnly || ExpectedNativeLoadCount != 1 || ExpectedNativeWriteCount != 1)
                    errors.Add("Owner-seed-derived Manual production requires base-only staging, one native load and exactly one native Manual write.");
                return errors;
            }
            if (checkpoint != null) errors.Add("Owner-seed exploration cannot select a persistence checkpoint.");
            // Writes are only the native setup level-up Autos: at most one per rider build,
            // bound exactly to the native autosave observation before any mutation.
            if (!BaseGameOnly || ExpectedNativeLoadCount != 1 || ExpectedNativeWriteCount < 0 || ExpectedNativeWriteCount > MaximumSetupLevelupWrites)
                errors.Add("Owner-seed exploration requires base-only staging, one native load and only its bounded setup level-up Auto writes.");
            return errors;
        }
        // The cold consumer of one owner-seed-derived Manual: its producer run, world
        // evidence and written bytes are exact pins; the owner's seed identity is carried
        // only to prove the derived Manual is not the immutable master. No setup, no
        // checkpoint parent, one load and zero writes.
        private IReadOnlyList<string> ValidateOwnerSeedDerivedLoad(string scenario, string runId, string checkpoint)
        {
            var errors = DerivedInputErrors(scenario, runId, 0, checkpoint);
            if (CheckpointSourceRunId != null || CheckpointSourceArtifactSha256 != null)
                errors.Add("The owner-seed-derived consumer carries no checkpoint parent.");
            if (CompanionId != null) errors.Add("Only owner-seed-derived RT combat names an unrelated companion.");
            if (checkpoint != OwnerSeedCheckpoint && checkpoint != "multi-exploration-three-loads-manual" && !IsManticoreManualCold(checkpoint))
                errors.Add("Owner-seed-derived cold load restores only its declared owner-seed or Manticore Manual.");
            return errors;
        }
        // Owner-seed-derived combat: the same producer and seed pins plus the ACCEPTED
        // separate-process consumer receipt (Checkpoint*) that cold-loaded the same Manual.
        // The combat runs may use a later candidate; the runner pins both receipts by
        // their own recorded candidate identity. One load, zero writes, no checkpoint.
        private IReadOnlyList<string> ValidateOwnerSeedDerivedCombat(string scenario, string runId, string checkpoint)
        {
            var errors = DerivedInputErrors(scenario, runId, 0, checkpoint);
            if (!Run(CheckpointSourceRunId) || CheckpointSourceRunId == runId || CheckpointSourceRunId == SourceRunId || CheckpointSourceRunId == SeedId ||
                !Hash(CheckpointSourceArtifactSha256) || CheckpointSourceArtifactSha256 == SourceArtifactSha256 ||
                CheckpointSourceArtifactSha256 == SourceArchiveSha256 || CheckpointSourceArtifactSha256 == SeedSha256)
                errors.Add("Owner-seed-derived combat requires the exact distinct accepted cold-consumer run and its evidence SHA256.");
            if (Case == OwnerSeedRealtimeCase
                ? !Chunk7ManualDerivedSelection.NativeId(CompanionId) || PartyIds == null || !PartyIds.Contains(CompanionId) || PairIds.Contains(CompanionId)
                : CompanionId != null)
                errors.Add("Owner-seed-derived RT combat names exactly one unrelated party companion; the TB cases name none.");
            if (checkpoint != null) errors.Add("Owner-seed-derived combat selects no persistence checkpoint.");
            return errors;
        }
        // Owner-seed checkpoint pair. The producer cold-loads the accepted Manual (Source*, plus the
        // ACCEPTED consumer receipt as Checkpoint*) and writes exactly one selected native checkpoint;
        // the consumer cold-loads that checkpoint (Checkpoint* = the producer run and its evidence).
        private IReadOnlyList<string> ValidateOwnerSeedCheckpoint(string scenario, string runId, string checkpoint)
        {
            var errors = DerivedInputErrors(scenario, runId, WriteCount(checkpoint), checkpoint);
            if (IsManticoreReturnArea && (SourceArea != ManticoreFixtureArea ||
                SourceArchiveSha256 != ManticoreFixtureManualSha256))
                errors.Add("Manticore return requires the exact admitted native Manual and source area.");
            if (!Run(CheckpointSourceRunId) || CheckpointSourceRunId == runId || CheckpointSourceRunId == SourceRunId || CheckpointSourceRunId == SeedId ||
                !Hash(CheckpointSourceArtifactSha256) || CheckpointSourceArtifactSha256 == SourceArtifactSha256 ||
                CheckpointSourceArtifactSha256 == SourceArchiveSha256 || CheckpointSourceArtifactSha256 == SeedSha256)
                errors.Add(IsOwnerSeedCheckpointSave ? "The owner-seed checkpoint producer requires the exact distinct accepted cold-consumer run and its evidence SHA256." :
                    "The owner-seed checkpoint consumer requires the exact distinct checkpoint producer run and its persistence evidence SHA256.");
            if (CompanionId != null) errors.Add("Only owner-seed-derived RT combat names an unrelated companion.");
            if (checkpoint == ManticoreFixtureCheckpoint && (!IsOwnerSeedCheckpointSave || SourceArchiveSha256 != ManticoreFixtureSourceSha256))
                errors.Add("Manticore fixture preparation is source-only and requires the exact accepted mounted owner-seed Manual.");
            if (checkpoint == TombFixtureCheckpoint && (!IsOwnerSeedCheckpointSave || SourceArchiveSha256 != TombFixtureSourceSha256))
                errors.Add("Tomb door fixture preparation is source-only and requires the exact accepted mounted owner-seed Manual.");
            if (checkpoint == ThornRiverFixtureCheckpoint && (!IsOwnerSeedCheckpointSave || SourceArchiveSha256 != ThornRiverFixtureSourceSha256))
                errors.Add("Thorn River door fixture preparation is source-only and requires the exact accepted mounted owner-seed Manual.");
            if (Array.IndexOf(OwnerSeedCheckpoints, checkpoint) < 0 &&
                !(IsOwnerSeedCheckpointSave && IsOwnerSeedSourceOnlyCheckpoint(checkpoint)) &&
                !(IsOwnerSeedCheckpointLoad && IsOwnerSeedLoadOnlyCheckpoint(checkpoint)))
                errors.Add("The owner-seed checkpoint pair admits only its connected checkpoints: " + string.Join(", ", OwnerSeedCheckpoints) + ".");
            return errors;
        }
        private List<string> DerivedInputErrors(string scenario, string runId, int writes = 0, string checkpoint = null)
        {
            var errors = new List<string>();
            if (scenario != Scenario) errors.Add("Owner-seed-derived cold load requires its exact closed native campaign family.");
            if (!Run(SourceRunId) || SourceRunId == runId || SourceRunId == SeedId || !Hash(SourceArtifactSha256) || !Hash(SourceArchiveSha256))
                errors.Add("The exact distinct successful owner-seed-derived Manual producer run, world evidence and written Manual bytes are required.");
            if (!Run(SeedId) || SeedId == runId || !Hash(SeedSha256) || SeedSha256 == SourceArchiveSha256 || SeedSha256 == SourceArtifactSha256)
                errors.Add("The owner's immutable seed identity is required and cannot alias the derived Manual or its evidence.");
            var area = (IsOwnerSeedCheckpointSave || IsOwnerSeedCheckpointLoad) && checkpoint == "multi-area-autosave";
            var manticoreCold = IsOwnerSeedDerivedLoad && IsManticoreManualCold(checkpoint);
            if (XpAssistance != null || (area ? SourceArea != (IsManticoreReturnArea ? ManticoreFixtureArea : "b10e86516b6437d45a629931c30936fe") :
                manticoreCold ? SourceArea != ManticoreFixtureArea : SourceArea != null))
                errors.Add("Only the selected owner-seed area checkpoint carries its exact original Manual area; setup assistance is forbidden.");
            if (!PairIds.All(Chunk7ManualDerivedSelection.NativeId) || PairIds.Distinct(StringComparer.Ordinal).Count() != 4)
                errors.Add("Four distinct observed native owner/pet IDs are required.");
            if (PartyIds == null || PartyIds.Length < 4 || PartyIds.Length > 12 ||
                !PartyIds.All(Chunk7ManualDerivedSelection.NativeId) || PartyIds.Distinct(StringComparer.Ordinal).Count() != PartyIds.Length ||
                PairIds.Any(id => !PartyIds.Contains(id))) errors.Add("The bounded full native field roster must contain both exact pairs once.");
            if (!BaseGameOnly || ExpectedNativeLoadCount != LoadCount(checkpoint) || ExpectedNativeWriteCount != writes)
                errors.Add(writes == 0 ? "Owner-seed-derived cold load requires base-only staging, one native load and zero native writes." :
                    "The owner-seed checkpoint producer requires base-only staging, one native load and exactly one native write.");
            return errors;
        }
        private static bool Run(string value) => Regex.IsMatch(value ?? "", "^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$");
        private static bool Hash(string value) => Regex.IsMatch(value ?? "", "^[a-f0-9]{64}$");
    }

    public sealed class Chunk7OwnerSeedXpAssistance
    {
        public string UnitId { get; set; }
        public int TargetCharacterLevel { get; set; }
    }

    // Observed native facts for one selected rider, read before any setup mutation.
    internal sealed class Chunk7OwnerSeedRiderFacts
    {
        internal string Id { get; set; }
        internal string Size { get; set; }
        internal string OriginalSize { get; set; }
        internal bool InParty { get; set; }
        internal bool Controllable { get; set; }
        internal bool IsPet { get; set; }
        internal bool HasPet { get; set; }
        internal bool HasDualCompanion { get; set; }
        internal bool InCombat { get; set; }
        internal bool Idle { get; set; }
        internal bool Polymorphed { get; set; }
        internal bool HasHorseRank { get; set; }
        internal int CharacterLevel { get; set; }
        internal int RangerLevel { get; set; }
        internal int Experience { get; set; }
        internal int ReadyLevel { get; set; }
        internal int ExperienceRate { get; set; }
    }

    // Pure owner-seed setup planning under the ordinary build driver's own Ranger
    // eligibility rule. A refusal is a SETUP failure before any mutation, never a
    // behaviour result.
    internal static class Chunk7OwnerSeedSetupPlan
    {
        internal const int RequiredRangerLevel = 4, MaximumTargetLevel = 8;
        // Chunk7OrdinaryBuildDriver: rangerLevel + readyLevel(xp) - characterLevel >= 4.
        internal static bool EligibleWithoutAssistance(Chunk7OwnerSeedRiderFacts rider) =>
            rider.RangerLevel + rider.ReadyLevel - rider.CharacterLevel >= RequiredRangerLevel;
        // The ordinary build levels to the XP-ready level: Ranger must land on exactly 4.
        internal static bool ExactlyRangerFour(Chunk7OwnerSeedRiderFacts rider) =>
            rider.RangerLevel + rider.ReadyLevel - rider.CharacterLevel == RequiredRangerLevel;
        internal static int MinimalTargetLevel(Chunk7OwnerSeedRiderFacts rider) =>
            rider.CharacterLevel + RequiredRangerLevel - rider.RangerLevel;
        internal static IReadOnlyList<string> RiderErrors(Chunk7OwnerSeedRiderFacts rider, string role)
        {
            var errors = new List<string>();
            if (rider == null) { errors.Add("Rider " + role + " is absent from the loaded native party."); return errors; }
            if (!rider.InParty) errors.Add("Rider " + role + " is not in the exact loaded native party.");
            if (rider.Size != "Medium" || rider.OriginalSize != "Medium" || rider.Polymorphed)
                errors.Add("Rider " + role + " is not an unpolymorphed Medium body; only medium-humanoid-horse-v1 is supported.");
            if (!rider.Controllable || rider.IsPet || rider.InCombat || !rider.Idle)
                errors.Add("Rider " + role + " is not a controllable idle non-pet actor outside combat.");
            if (rider.HasPet || rider.HasDualCompanion || rider.HasHorseRank)
                errors.Add("Rider " + role + " already owns a pet, a dual companion or Horse progression; the fixture requires a petless actor.");
            if (rider.CharacterLevel < 1 || rider.RangerLevel < 0 || rider.RangerLevel > rider.CharacterLevel || rider.ReadyLevel < rider.CharacterLevel)
                errors.Add("Rider " + role + " native progression observation is inconsistent.");
            return errors;
        }
        // The installed game (2.1.7b) has no level-up autosave: GameModesFactory builds its
        // controllers explicitly and never constructs or registers LevelUpAutoSaveController
        // (no reference anywhere in Assembly-CSharp, no reflective registration), so nothing
        // handles ILevelUpCompleteUIHandler.HandleLevelUpComplete with a save. Observed natively
        // in run 20261017-c7b-world19-ownerseed-b2s1-twopair (completionEntries=0,
        // saveRequests=0, preparedWrites=0). Setup expects zero writes for every setting.
        internal static int ExpectedSetupWrites(bool autosaveEnabled, int riderBuilds) => 0;
        internal static IReadOnlyList<string> WriteErrors(Chunk7OwnedPairsSelection selection, bool autosaveEnabled, bool onlyOneSave, int riderBuilds)
        {
            var errors = new List<string>();
            if (onlyOneSave) errors.Add("Native OnlyOneSave/Ironman is on; its single-slot saves cannot be run-owned setup level-up Autos.");
            if (riderBuilds < 0 || riderBuilds > Chunk7OwnedPairsSelection.MaximumSetupLevelupWrites) errors.Add("Owner-seed rider build count is outside its bound.");
            var expected = ExpectedSetupWrites(autosaveEnabled, riderBuilds);
            // The Manual producer's selected write is its measured Manual, never a setup write.
            var selectedSetupWrites = selection == null ? -1 : SelectedSetupWrites(selection);
            if (selection == null || selectedSetupWrites != expected)
                errors.Add("Selected native setup write count " + (selection == null ? "null" : selectedSetupWrites.ToString()) +
                    " must be " + expected + ": the native game has no level-up autosave (LevelUpAutoSaveController is never registered), whatever the autosave setting (" +
                    (autosaveEnabled ? "on" : "off") + ", " + riderBuilds + " rider builds).");
            return errors;
        }
        internal static int SelectedSetupWrites(Chunk7OwnedPairsSelection selection) =>
            selection.IsOwnerSeedManual ? selection.ExpectedNativeWriteCount - 1 : selection.ExpectedNativeWriteCount;
        internal static IReadOnlyList<string> AssistanceErrors(Chunk7OwnedPairsSelection selection, Chunk7OwnerSeedRiderFacts riderA, Chunk7OwnerSeedRiderFacts riderB)
        {
            var errors = new List<string>();
            if (selection == null || !selection.OwnerSeedSetup || riderA == null || riderB == null) { errors.Add("Owner-seed setup plan lacks its selection or riders."); return errors; }
            if (riderA.Id != selection.RiderAId || riderB.Id != selection.RiderBId) errors.Add("Observed riders differ from the selected rider roles.");
            if (!EligibleWithoutAssistance(riderA))
                errors.Add("Rider A is not eligible for Ranger 4 / Hunter's Bond from its own native XP; no assistance is authorized for rider A.");
            else if (!ExactlyRangerFour(riderA)) errors.Add("Rider A's native XP would carry it beyond Ranger 4; extra levels are refused.");
            var assistance = selection.XpAssistance;
            if (EligibleWithoutAssistance(riderB))
            {
                if (assistance != null) errors.Add("Rider B is already eligible; XP assistance beyond what eligibility requires is refused.");
                else if (!ExactlyRangerFour(riderB)) errors.Add("Rider B's native XP would carry it beyond Ranger 4; extra levels are refused.");
                return errors;
            }
            if (riderB.ReadyLevel != riderB.CharacterLevel) errors.Add("Assisted rider B must start inside its current level's XP band (no pending level).");
            var minimal = MinimalTargetLevel(riderB);
            if (assistance == null) errors.Add("Rider B needs native XP to character level " + minimal + " but no assistance was selected.");
            else if (assistance.UnitId != riderB.Id) errors.Add("XP assistance names an actor other than rider B.");
            else if (assistance.TargetCharacterLevel != minimal)
                errors.Add("XP assistance target " + assistance.TargetCharacterLevel + " differs from the exact minimal eligible level " + minimal + ".");
            if (minimal > MaximumTargetLevel) errors.Add("Rider B would need more than the bounded setup ceiling of character level " + MaximumTargetLevel + ".");
            if (riderB.ExperienceRate != 100) errors.Add("Exact native XP assistance requires ExperienceRatePercent 100.");
            return errors;
        }
    }

    // One item observation: its blueprint, stack count and the unit whose slot holds it
    // (null for the shared, unequipped stash).
    internal sealed class Chunk7OwnerSeedItemRow
    {
        internal string Blueprint { get; set; }
        internal int Count { get; set; }
        internal string HolderId { get; set; }
    }

    // The conservative money/item digest, identical before setup and at SETUP-COMPLETE:
    // EVERY row of the shared Player.Inventory with its holder id (null = unheld stash),
    // plus the fixed pre-setup party characters' equipped slots. Any item added, removed
    // or moved between holders in the shared collection changes it. A companion pet keeps
    // its own collection, so the setup Horses' items are disclosed separately.
    internal static class Chunk7OwnerSeedEconomyDigest
    {
        internal static string[] Rows(IEnumerable<Chunk7OwnerSeedItemRow> sharedInventory, IEnumerable<Chunk7OwnerSeedItemRow> equippedSlots,
            ICollection<string> characterIds)
        {
            var rows = new List<string>();
            foreach (var item in sharedInventory ?? Enumerable.Empty<Chunk7OwnerSeedItemRow>())
                if (item != null) rows.Add("shared|" + (item.HolderId ?? "-") + "|" + item.Blueprint + "|" + item.Count);
            foreach (var item in equippedSlots ?? Enumerable.Empty<Chunk7OwnerSeedItemRow>())
                if (item != null && characterIds.Contains(item.HolderId)) rows.Add("slot|" + item.HolderId + "|" + item.Blueprint + "|" + item.Count);
            return rows.OrderBy(row => row, StringComparer.Ordinal).ToArray();
        }
    }

    // The SETUP-COMPLETE boundary. Every assistance item is recorded while it is open;
    // after Complete any further setup mutation is refused and counted.
    internal sealed class Chunk7OwnerSeedSetupLedger
    {
        internal sealed class Item
        {
            internal string UnitId, Kind, NativeApi, Detail;
            internal long Before, After, Amount;
        }
        private readonly List<Item> items = new List<Item>();
        internal bool Completed { get; private set; }
        internal int RefusedAfterBoundary { get; private set; }
        internal int CompletedFrame { get; private set; } = -1;
        internal IReadOnlyList<Item> Items => items;
        internal void Record(string unitId, string kind, string nativeApi, long before, long after, long amount, string detail)
        {
            RequireOpen(kind);
            if (string.IsNullOrEmpty(unitId) || string.IsNullOrEmpty(kind) || string.IsNullOrEmpty(nativeApi))
                throw new InvalidOperationException("Setup assistance must name its unit, kind and native API.");
            items.Add(new Item { UnitId = unitId, Kind = kind, NativeApi = nativeApi, Before = before, After = after, Amount = amount, Detail = detail });
        }
        internal void RequireOpen(string operation)
        {
            if (!Completed) return;
            RefusedAfterBoundary++;
            throw new InvalidOperationException("Setup mutation refused after SETUP-COMPLETE: " + operation + ".");
        }
        internal void Complete(int frame)
        {
            if (Completed) throw new InvalidOperationException("SETUP-COMPLETE may be crossed only once.");
            Completed = true; CompletedFrame = frame;
        }
    }
}
