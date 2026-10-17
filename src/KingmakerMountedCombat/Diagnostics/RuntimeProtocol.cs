using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace KingmakerMountedCombat.Diagnostics
{
    public sealed class RuntimeRequest
    {
        public const int CurrentSchemaVersion = 1;
        public const int SaveBackedSchemaVersion = 2;
        public const string WorkingSaveName = "KMC_AUTOMATION_WORKING";
        public const string BaselineSaveName = "KMC_AUTOMATION_BASELINE";
        public const string ManualReviewScenario = "manual-visual-review";

        private static readonly HashSet<string> SaveBackedScenarios = new HashSet<string>(StringComparer.Ordinal)
        {
            "chunk7-owned-two-pair-campaign", "chunk7-cumulative-acquisition", "chunk7-acquired-manual-derivative", "chunk7-manual-derived-acquisition", "chunk7-native-progression", "chunk7-assisted-fixture-preparation", "chunk7-assisted-cold-inventory", "chunk7-native-main-bootstrap", "chunk7-bootstrap-cold-inventory", "chunk7-bootstrap-manual-seed", "chunk7-small-profile-calibration", "chunk7-ordinary-field-admission", "chunk7-ordinary-acquisition", "chunk7-two-pair-exploration", "chunk7-persistence-save", "chunk7-persistence-load",
            "chunk7-two-pair-combat-rider-first", "chunk7-two-pair-combat-mount-first",
            "chunk7-two-pair-realtime-core", "chunk7-two-pair-realtime-casting", "chunk7-two-pair-realtime-charge",
            "chunk7-two-pair-native-life-cleanup",
            "export-mounted-contracts",
            "export-candidate-mount-rigs",
            "observe-mount-diagnostic-availability",
            "horse-native-asset-audit",
            "horse-companion-blueprint-registration",
            "horse-companion-unmounted-suite",
            "horse-mounted-alpha-suite",
            "horse-native-controls-ux-suite",
            "chunk6a-mount-preamble",
            "chunk6c-casting-rt", "chunk6c-casting-tb", "chunk6c-casting-unmounted-rt", "chunk6c-casting-unmounted-tb",
            "chunk6d-staged-rt", "chunk6d-staged-tb", "chunk6e-reaction-rt", "chunk6e-reaction-tb",
            "chunk6b-charge-rt","chunk6b-charge-core-rt","chunk6b-charge-interruption-rt","chunk6b-charge-lifecycle-rt", "chunk6b-charge-tb",
            "chunk6b-charge-path-rt", "chunk6b-charge-path-tb",
            "chunk6a-mammoth-mount-rt", "chunk6a-mammoth-mount-tb", "chunk6a-paused-queue", "chunk6a-refused-policy-disabled","chunk6a-refused-foreign-companion","chunk6a-refused-wrong-creature-target","chunk6a-refused-mount-selected","chunk6a-refused-multiple-selection","chunk6a-refused-foreign-selection", "chunk6a-mount-approach", "chunk6a-geometry-change", "chunk6a-obstruction", "chunk6a-stop-approach", "chunk6a-combat-end-approach", "chunk6a-disable-approach", "chunk6a-command-replacement", "chunk6a-repeated-mount-request", "chunk6a-ownership-change", "chunk6a-size-form-change", "chunk6a-lost-direct-control", "chunk6a-rider-incapacitated", "chunk6a-mount-incapacitated", "chunk6a-hotbar-approach", "chunk6a-allocation-rider-first-tb","chunk6a-allocation-mount-first-tb","chunk6a-dismount-feature-disabled-rt","chunk6a-dismount-policy-disabled-rt","chunk6a-auto-use-mount-rt","chunk6a-auto-use-dismount-rt",
            "chunk6a-adoption-compensation-rt", "chunk6a-adoption-compensation-tb",
            "chunk6a-left-area", "chunk6a-view-agent-lost", "chunk6a-loading-cutscene", "chunk6a-generation-change",
            "chunk6a-injected-exception", "chunk6a-rider-death-approach", "chunk6a-mount-death-approach",
            "chunk6a-turn-end-approach-tb", "chunk6a-mode-exit-tb",
            "chunk6a-mount-spent-move-tb", "chunk6a-mount-spent-standard-tb", "chunk6a-mount-spent-all-tb", "chunk6a-rider-without-move-tb", "chunk6a-rider-other-action-tb", "chunk6a-unrelated-candidate-between-tb", "chunk6a-dismount-after-rider-expenditure-tb", "chunk6a-dismount-after-mount-expenditure-tb", "chunk6a-dismount-immediately-after-mount-tb",
            "chunk6a-combat-mount-rt", "chunk6a-combat-mount-tb", "chunk4-rider-incapacitation-tb", "chunk4-rider-death-tb", "chunk4-mount-death-tb", "chunk4-targeting-rider-rt", "chunk4-targeting-mount-rt", "chunk4-ground-arrival-rt", "chunk4-horse-strike-comparison-rt", "chunk4-targeting-area-unmounted-rt", "chunk4-obstruction-ranged-rt", "chunk4-ranged-native-control-rt", "chunk4-interrupt-melee-rt", "chunk4-interrupt-ranged-rt", "chunk4-inspection-rt", "chunk4-session-rt", "chunk4-session-tb", "chunk4-sustained-melee-rt", "chunk4-sustained-ranged-rt", "chunk4-sustained-tb", "chunk4-charge-safety-rt", "chunk4-charge-safety-tb", "actor-allocation-rider-first-tb", "actor-allocation-mount-first-tb", "actor-allocation-rider-first-unmounted-tb", "actor-allocation-mount-first-unmounted-tb", "ordinary-attack-controls-tb", "unmounted-attack-controls-rt", "phase3h-combat-loop-rt", "phase3h-combat-loop-tb", "phase3g-native-controls-rt", "phase3g-native-controls-tb", "phase3d-unified-combat-rt-suite",
            "phase3d-unified-combat-tb-suite",
            "phase3d-horse-presentation-suite",
            "player-action-availability",
            "mount-dismount-user-flow",
            "mounted-pair-create-and-clear",
            "mounted-pair-double-mount-rejected",
            "mounted-pair-invalid-pair-rejected",
            "mounted-pair-cleanup-idempotent",
            "mounted-pair-death-cleanup",
            "mounted-pair-combat-start-cleanup",
            "mounted-pair-area-unload-cleanup",
            "mounted-pair-mod-disable-cleanup",
            "mounted-pair-combat-start-retained",
            "mounted-pair-combat-end-retained",
            "mounted-pair-rider-death-cleanup",
            "mounted-pair-mount-death-cleanup",
            "mounted-pair-rider-incapacitated-cleanup",
            "mounted-pair-mount-incapacitated-cleanup",
            "mounted-pair-rider-native-incapacitated-cleanup",
            "mounted-pair-mount-native-incapacitated-cleanup",
            "mounted-pair-companion-removal-cleanup",
            "mounted-pair-view-destroyed-cleanup",
            "mounted-pair-exception-cleanup",
            "mounted-pair-open-ground",
            "mounted-pair-stop-start",
            "mounted-pair-turns-and-corners",
            "mounted-pair-doorway",
            "mounted-distance-door-interaction",
            "mounted-pair-selection",
            "mounted-pair-party-formation",
            "mounted-pair-pause-unpause",
            "mounted-pair-destination-cancel",
            "mounted-pair-turn-based-entry-cleanup",
            "mounted-pair-realtime-entry-cleanup",
            "mounted-pair-save-safety",
            "mounted-pair-load-safety",
            "mounted-pair-area-transition-safety",
            "native-save-clean-dismount",
            "native-area-clean-dismount",
            "native-mode-transition-cleanup",
            "presentation-residue-and-uninstall-safety",
            "pose-idle",
            "pose-walk-run",
            "pose-turn-stop",
            "pose-doorway-formation",
            "pose-equipment-variants",
            "ui-selection-portrait-actionbar",
            "camera-follow-and-command-routing",
            "fixture-intake",
            "persistence-isolation", "persistence-p07-save", "persistence-p07-load", "persistence-p01-save", "persistence-p01-load", "persistence-p02-save", "persistence-p02-load", "persistence-p03-save", "persistence-p03-load", "persistence-p04-save", "persistence-p04-load", "persistence-p05-save", "persistence-p05-load", "persistence-p06-load",
            "lifecycle-suite",
            "combat-lifecycle-suite",
            "chunk4-traversal-core", "chunk4-traversal-slope", "chunk4-area-cleanup", "movement-suite",
            "boundary-suite",
            "presentation-suite",
            "mounted-rider-melee-hit-rt",
            "mounted-rider-melee-hit-tb",
            "mounted-rider-melee-miss-rt",
            "mounted-mammoth-primary-hit-rt",
            "mounted-mammoth-primary-hit-tb",
            "mounted-rider-melee-move-to-attack-rt",
            "mounted-rider-melee-move-to-attack-tb",
            "mounted-rider-melee-command-cancel-rt",
            "mounted-rider-melee-command-cancel-tb",
            "mounted-rider-melee-command-interrupt-rt",
            "mounted-rider-melee-command-interrupt-tb",
            "mounted-rider-melee-combat-end-rt",
            "mounted-rider-melee-combat-end-tb",
            "mounted-rider-melee-human-play-path-rt",
            "mounted-rider-melee-human-play-path-tb",
            "combat-core-control-suite",
            ManualReviewScenario
        };

        public int SchemaVersion { get; set; }

        public string RunId { get; set; }

        public string Scenario { get; set; }

        public string Branch { get; set; }

        public string Commit { get; set; }

        public string ProductVersion { get; set; }

        public string DllSha256 { get; set; }

        public string DllMvid { get; set; }

        public string TransactionToken { get; set; }

        public string EvidenceRoot { get; set; }

        // Schema v1 compatibility fields. Schema v2 deliberately omits both.
        public bool SaveAccessAllowed { get; set; }

        public string SaveName { get; set; }

        public RuntimeFixtureIdentity Fixture { get; set; }

        public bool? ObserveWorkingDeserialization { get; set; }
        public bool ShouldSerializeObserveWorkingDeserialization() => ObserveWorkingDeserialization.HasValue;

        public RuntimeSaveDescriptor PersistenceLoad { get; set; }
        public RuntimeSaveDescriptor PersistenceAlternate { get; set; }
        public string Schema1DerivationSha256 { get; set; }
        public RuntimeAreaTransitionTarget PersistenceAreaTarget { get; set; }
        public RuntimeAreaTransitionTarget[] PersistenceReplayTargets { get; set; }
        public Chunk7OrdinaryAcquisitionSelection OrdinaryAcquisition { get; set; }
        public Chunk7OrdinaryFieldSelection OrdinaryField { get; set; }
        public Chunk7SmallCalibrationSelection SmallCalibration { get; set; }
        public Chunk7AssistedFixtureSelection AssistedFixture { get; set; }
        public bool ShouldSerializeAssistedFixture() => AssistedFixture != null;
        public Chunk7AssistedColdInventorySelection AssistedColdInventory { get; set; }
        public bool ShouldSerializeAssistedColdInventory() => AssistedColdInventory != null;
        public Chunk7NativeBootstrapSelection NativeBootstrap { get; set; }
        public Chunk7BootstrapInventorySelection BootstrapInventory { get; set; }
        public Chunk7ManualSeedSelection ManualSeed { get; set; }
        public Chunk7ManualDerivedSelection ManualDerived { get; set; }
        public Chunk7CumulativeAcquisitionSelection CumulativeAcquisition { get; set; }
        public Chunk7AcquiredManualSelection AcquiredManual { get; set; }
        public Chunk7OwnedPairsSelection OwnedPairs { get; set; }
        internal string SelectedChunk7Scenario => OwnedPairs == null ? Scenario : OwnedPairs.RuntimeScenario;
        internal RuntimeSaveDescriptor SelectedChunk7Fixture => OwnedPairs != null
            || Scenario == Chunk7AssistedColdInventorySelection.Scenario
            || Scenario == Chunk7SmallCalibrationSelection.Scenario && SmallCalibration?.UsesManualField == true
            || Scenario == Chunk7ProgressionSelection.Scenario && Progression?.IsOwned == true
            ? PersistenceLoad : Fixture?.Working;
        public Chunk7ProgressionSelection Progression { get; set; }

        // Cross-area cases declare their destination up front so the isolated
        // save authority can pin post-transition writes to that exact area.
        internal static bool IsChargeLifecycleCase(string persistenceCase) => Array.IndexOf(new[] {
            "mounted-charge-area", "mounted-charge-session", "mounted-charge-disable", "mounted-charge-removal" }, persistenceCase) >= 0;

        // Chunk 6 closeout P04 checkpoints on the real-time combat Mount foundation flow (RuntimePersistenceClosureScenario).
        internal static bool IsClosurePersistenceCase(string persistenceCase) => Array.IndexOf(new[] {
            "combat-mount-routes", "combat-mount-unsettled-save", "combat-mount-area-reload", "pending-mount-area" }, persistenceCase) >= 0;

        internal static bool IsCrossAreaCase(string persistenceCase) =>
            persistenceCase == "area-cross-entry" || persistenceCase == "area-cross-exit";

        // Cold-only cases that load the transition autosave a cross-area source
        // actually produced, rather than its separate destination manual
        // archive. AfterEntry committed in the destination; BeforeExit committed
        // in the departure area before suspension, so each expects its own world.
        internal static bool IsTransitionAutoCase(string persistenceCase) =>
            persistenceCase == "area-cross-entry-auto" || persistenceCase == "area-cross-exit-auto";

        internal static bool IsCrossAreaFamilyCase(string persistenceCase) =>
            IsCrossAreaCase(persistenceCase) || IsTransitionAutoCase(persistenceCase);

        // The harmful-lifecycle boundary cases: the rider or the mount dies to
        // real native damage while mounted, and the no-pair archive that follows
        // is reloaded in-process and cold.
        internal static bool IsDeathCase(string persistenceCase) =>
            persistenceCase == "rider-death" || persistenceCase == "mount-death";

        // The live eligibility change: the engine's own size effect makes the
        // mounted rider Large, KMC's invariant ends the pair, and the no-pair
        // archive that follows is reloaded in-process and cold.
        internal static bool IsEligibilityCase(string persistenceCase) => persistenceCase == "rider-size-change";

        // The one P06 derivative that corrupts a native area member, so that the
        // load fails after Game.DisposeState rather than at admission.
        internal static bool IsFailedLoad(string persistenceCase) =>
            persistenceCase == "failed-area-load";

        internal string ExpectedTransitionAutoArea =>
            PersistenceCase == "area-cross-entry-auto" ? PersistenceAreaTarget?.Area : Fixture?.Working?.Area;

        // A transition autosave is a native Auto save, so the loader must expect
        // that exact type rather than the destination manual leaf.
        internal static bool IsChunk7CombatScenario(string scenario) => scenario == "chunk7-two-pair-combat-rider-first" || scenario == "chunk7-two-pair-combat-mount-first" || IsChunk7RealtimeScenario(scenario) || IsChunk7NativeLifeScenario(scenario);
        internal static bool IsChunk7RealtimeScenario(string scenario) => scenario == "chunk7-two-pair-realtime-core" || scenario == "chunk7-two-pair-realtime-casting" || scenario == "chunk7-two-pair-realtime-charge";
        internal static bool IsChunk7NativeLifeScenario(string scenario) => scenario == "chunk7-two-pair-native-life-cleanup";
        internal static bool IsChunk7PersistenceScenario(string scenario) => scenario == "chunk7-persistence-save" || scenario == "chunk7-persistence-load";
        internal static bool IsChunk7PersistenceCase(string value) => Array.IndexOf(new[] {
            "multi-exploration-manual", "multi-exploration-quick", "multi-exploration-auto", "multi-rt-debt-manual",
            "multi-tb-debt-manual", "zero-mounted-tb-retained-manual", "legacy-mounted-pending-tb-manual", "multi-area-autosave", "legacy-schema1-resave-manual", "owned-schema1-manual", "owned-schema1-resave-manual", "multi-exploration-three-loads-manual", "multi-exploration-three-area-transfers-manual",
            "multi-nonselected-mount-unsettled-manual", "multi-owned-worker-cancel-manual" }, value) >= 0;

        internal string ExpectedChunk7ArchiveArea => PersistenceCase == "multi-area-autosave" ? PersistenceAreaTarget?.Area : SelectedChunk7Fixture?.Area;
        internal string ExpectedNativeLoadType => (Scenario == Chunk7AcquiredManualSelection.Scenario || Scenario == Chunk7ManualSeedSelection.Scenario || Scenario == Chunk7BootstrapInventorySelection.Scenario || Scenario == Chunk7SmallCalibrationSelection.Scenario && SmallCalibration?.UsesManualField != true || Scenario == Chunk7OrdinaryFieldSelection.Scenario) ? "Auto" : SelectedChunk7Scenario == "chunk7-persistence-load" && PersistenceLoad != null && IsChunk7PersistenceCase(PersistenceCase) ?
            (PersistenceCase == "multi-exploration-quick" ? "Quick" : PersistenceCase == "multi-exploration-auto" || PersistenceCase == "multi-area-autosave" ? "Auto" : "Manual") :
            IsTransitionAutoCase(PersistenceCase) ? "Auto" :
            Scenario == "persistence-p05-load" && PersistenceLoad != null ?
            (PersistenceCase == "quick" ? "Quick" : PersistenceCase == "auto" ? "Auto" : "Manual") : "Manual";
        public string PersistenceCase { get; set; }

        public RuntimeQualificationSuiteIdentity QualificationSuite { get; set; }

        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();
            if (ObserveWorkingDeserialization.HasValue && (ObserveWorkingDeserialization != true ||
                SchemaVersion != SaveBackedSchemaVersion || Scenario != "export-mounted-contracts" ||
                Fixture?.Working?.InternalName != WorkingSaveName || PersistenceLoad != null || PersistenceAlternate != null ||
                PersistenceCase != null || PersistenceAreaTarget != null || PersistenceReplayTargets != null ||
                OwnedPairs != null || AcquiredManual != null || ManualDerived != null || CumulativeAcquisition != null ||
                ManualSeed != null || BootstrapInventory != null || NativeBootstrap != null || AssistedFixture != null || AssistedColdInventory != null || SmallCalibration != null ||
                OrdinaryField != null || OrdinaryAcquisition != null || Progression != null))
                errors.Add("Working deserialization observation requires exact true on the unchanged qualified Working export request only.");
            if (SchemaVersion != CurrentSchemaVersion && SchemaVersion != SaveBackedSchemaVersion)
            {
                errors.Add("Unsupported request schemaVersion.");
            }

            if (Scenario == Chunk7OwnedPairsSelection.Scenario)
            {
                if (SchemaVersion != SaveBackedSchemaVersion || OwnedPairs == null) errors.Add("Owned pairs require their exact schema-2 inherited selection.");
                else errors.AddRange(OwnedPairs.Validate(Scenario, RunId, PersistenceCase));
            }
            else if (OwnedPairs != null) errors.Add("Inherited pair selection is forbidden under historical or foreign families.");
            if (Scenario == Chunk7CumulativeAcquisitionSelection.Scenario)
            {
                if (SchemaVersion != SaveBackedSchemaVersion || CumulativeAcquisition == null) errors.Add("Cumulative acquisition requires its exact separate schema-2 selection.");
                else errors.AddRange(CumulativeAcquisition.Validate(Scenario, RunId));
            }
            else if (CumulativeAcquisition != null) errors.Add("Cumulative acquisition selection is forbidden under another scenario.");
            if (Scenario == Chunk7AcquiredManualSelection.Scenario)
            {
                if (SchemaVersion != SaveBackedSchemaVersion || AcquiredManual == null) errors.Add("Acquired Manual requires its exact schema-2 field-preserving selection.");
                else errors.AddRange(AcquiredManual.Validate(Scenario, RunId));
            }
            else if (AcquiredManual != null) errors.Add("Acquired Manual selection is forbidden under another scenario.");
            if (Scenario == Chunk7ManualDerivedSelection.Scenario)
            {
                if (SchemaVersion != SaveBackedSchemaVersion || ManualDerived == null) errors.Add("Manual-derived acquisition requires its exact schema-2 selection.");
                else errors.AddRange(ManualDerived.Validate(Scenario, RunId));
            }
            else if (ManualDerived != null) errors.Add("Manual-derived selection is forbidden under another scenario.");
            if (Scenario == Chunk7ManualSeedSelection.Scenario)
            {
                if (SchemaVersion != SaveBackedSchemaVersion || ManualSeed == null) errors.Add("Manual seed requires its exact schema-2 selection.");
                else errors.AddRange(ManualSeed.Validate(Scenario, RunId));
            }
            else if (ManualSeed != null) errors.Add("Manual seed selection is forbidden under another scenario.");
            if (Scenario == Chunk7BootstrapInventorySelection.Scenario)
            {
                if (SchemaVersion != SaveBackedSchemaVersion || BootstrapInventory == null) errors.Add("Bootstrap cold inventory requires its exact schema-2 selection.");
                else errors.AddRange(BootstrapInventory.Validate(Scenario, RunId));
            }
            else if (BootstrapInventory != null) errors.Add("Bootstrap inventory selection is forbidden under another scenario.");
            if (Scenario == Chunk7AssistedColdInventorySelection.Scenario)
            {
                if (SchemaVersion != SaveBackedSchemaVersion || AssistedColdInventory == null) errors.Add("Assisted cold inventory requires its exact separate schema-2 selection.");
                else errors.AddRange(AssistedColdInventory.Validate(Scenario, RunId));
                if (PersistenceAlternate != null || PersistenceCase != null || PersistenceAreaTarget != null || PersistenceReplayTargets != null)
                    errors.Add("Assisted cold inventory permits only its one qualified immutable Manual source.");
            }
            else if (AssistedColdInventory != null) errors.Add("Assisted cold selection is forbidden under every historical or foreign family.");
            if (Scenario == Chunk7AssistedFixtureSelection.Scenario)
            {
                if (SchemaVersion != SaveBackedSchemaVersion || AssistedFixture == null) errors.Add("Assisted fixture requires its exact separate schema-2 selection.");
                else errors.AddRange(AssistedFixture.Validate(Scenario));
                if (PersistenceLoad != null || PersistenceAlternate != null || PersistenceCase != null || PersistenceAreaTarget != null || PersistenceReplayTargets != null)
                    errors.Add("Assisted fixture cannot consume or relabel any existing native campaign.");
            }
            else if (AssistedFixture != null) errors.Add("Assisted setup is forbidden under every historical or foreign family.");
            if (Scenario == Chunk7NativeBootstrapSelection.Scenario)
            {
                if (SchemaVersion != SaveBackedSchemaVersion || NativeBootstrap == null) errors.Add("Native main bootstrap requires its explicit schema-2 selection.");
                else errors.AddRange(NativeBootstrap.Validate(Scenario));
            }
            else if (NativeBootstrap != null) errors.Add("Native bootstrap selection is forbidden under another scenario.");
            if (Scenario == Chunk7SmallCalibrationSelection.Scenario)
            {
                if (SchemaVersion != SaveBackedSchemaVersion || SmallCalibration == null) errors.Add("Compiled Small calibration requires its exact schema-2 selection.");
                else errors.AddRange(SmallCalibration.Validate(Scenario, RunId));
            }
            else if (SmallCalibration != null) errors.Add("Small calibration selection is forbidden under another scenario.");
            if (Scenario == Chunk7OrdinaryFieldSelection.Scenario)
            {
                if (SchemaVersion != SaveBackedSchemaVersion || OrdinaryField == null) errors.Add("Read-only field admission requires its schema-2 selection.");
                else errors.AddRange(OrdinaryField.Validate(Scenario, RunId));
            }
            else if (OrdinaryField != null) errors.Add("Field admission selection is forbidden under another scenario.");
            if (Scenario == Chunk7ProgressionSelection.Scenario)
            {
                if (SchemaVersion != SaveBackedSchemaVersion || Progression == null) errors.Add("Native progression requires its schema-2 exact selection.");
                else errors.AddRange(Progression.Validate(Scenario, RunId));
            }
            else if (Progression != null) errors.Add("Native progression selection is forbidden under another scenario.");
            if (Scenario == Chunk7OrdinaryAcquisitionSelection.Scenario)
            {
                if (SchemaVersion != SaveBackedSchemaVersion || OrdinaryAcquisition == null)
                    errors.Add("Ordinary acquisition requires its explicit schema-2 selection.");
                else errors.AddRange(OrdinaryAcquisition.Validate(Scenario, RunId));
            }
            else if (OrdinaryAcquisition != null) errors.Add("Ordinary acquisition selection is forbidden under another scenario.");
            Require(errors, RunId, "runId");
            Require(errors, Scenario, "scenario");
            Require(errors, Branch, "branch");
            Require(errors, Commit, "commit");
            Require(errors, ProductVersion, "productVersion");
            RequireSha256(errors, DllSha256, "dllSha256");
            RequireGuid(errors, DllMvid, "dllMvid");
            Require(errors, EvidenceRoot, "evidenceRoot");
            RequireSha256(errors, TransactionToken, "transactionToken");

            if (string.IsNullOrEmpty(RunId) || !Regex.IsMatch(RunId, "^[A-Za-z0-9._-]{1,120}$"))
            {
                errors.Add("runId is outside the exact runtime allowlist.");
            }

            if (string.IsNullOrEmpty(Branch) || !Branch.StartsWith("codex/mounted-combat-", StringComparison.Ordinal))
            {
                errors.Add("branch must use the codex/mounted-combat- prefix.");
            }

            if (!IsLowerHex(Commit, 40))
            {
                errors.Add("commit must be a 40-character lowercase Git SHA.");
            }

            if (!string.Equals(ProductVersion, BuildIdentity.ProductVersion, StringComparison.Ordinal))
            {
                errors.Add("productVersion does not match this diagnostic build.");
            }

            if (SchemaVersion == CurrentSchemaVersion)
            {
                ValidateLegacyNoSaveRequest(errors);
            }
            else if (SchemaVersion == SaveBackedSchemaVersion)
            {
                ValidateSaveBackedRequest(errors);
            }

            return errors;
        }

        public static bool IsMissionScenario(string scenario)
        {
            return RuntimeSubscenarioResult.IsMissionScenario(scenario);
        }

        public static bool IsSaveBackedScenario(string scenario)
        {
            return !string.IsNullOrEmpty(scenario) && SaveBackedScenarios.Contains(scenario);
        }

        public static bool IsManualReviewScenario(string scenario)
        {
            return string.Equals(scenario, ManualReviewScenario, StringComparison.Ordinal);
        }

        internal static void Require(List<string> errors, string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add(name + " is required.");
            }
        }

        internal static void RequireSha256(List<string> errors, string value, string name)
        {
            if (!IsLowerHex(value, 64))
            {
                errors.Add(name + " must be a 64-character lowercase hexadecimal SHA-256.");
            }
        }

        internal static void RequireGuid(List<string> errors, string value, string name)
        {
            Guid parsed;
            if (!Guid.TryParse(value, out parsed))
            {
                errors.Add(name + " must be a GUID.");
            }
        }

        internal static bool IsLowerHex(string value, int length)
        {
            if (string.IsNullOrEmpty(value) || value.Length != length)
            {
                return false;
            }

            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (!((character >= '0' && character <= '9') || (character >= 'a' && character <= 'f')))
                {
                    return false;
                }
            }

            return true;
        }

        private void ValidateLegacyNoSaveRequest(List<string> errors)
        {
            if (PersistenceLoad != null || PersistenceAlternate != null) errors.Add("No-save requests cannot select an archive.");
            if (PersistenceCase != null) errors.Add("No-save requests cannot select a persistence case.");
            if (PersistenceReplayTargets != null) errors.Add("No-save requests cannot declare repeated native area targets.");
            if (Schema1DerivationSha256 != null) errors.Add("No-save requests cannot select schema-1 derivative provenance.");
            if (SaveAccessAllowed)
            {
                errors.Add("Schema v1 never authorizes save access.");
            }

            if (!string.IsNullOrEmpty(SaveName))
            {
                errors.Add("No-save requests must not include saveName.");
            }

            if (Fixture != null)
            {
                errors.Add("Schema v1 requests must not include fixture identity.");
            }

            if (QualificationSuite != null)
            {
                errors.Add("Schema v1 requests must not include qualification-suite identity.");
            }

            if (!string.Equals(Scenario, "mod-load-smoke", StringComparison.Ordinal))
            {
                errors.Add("Only mod-load-smoke is implemented as a schema-v1 no-save scenario.");
            }
        }

        private void ValidateSaveBackedRequest(List<string> errors)
        {
            var chunk7 = IsChunk7PersistenceScenario(Scenario) || Scenario == Chunk7OwnedPairsSelection.Scenario && OwnedPairs?.Persistence == true;
            var replay = PersistenceCase == "multi-exploration-three-loads-manual" || PersistenceCase == "multi-exploration-three-area-transfers-manual";
            var replayAreas = PersistenceCase == "multi-exploration-three-area-transfers-manual";
            if (replay && Scenario != "chunk7-persistence-load" &&
                !(Scenario == Chunk7OwnedPairsSelection.Scenario && OwnedPairs?.Cold == true))
                errors.Add("Repeated worlds are cold-load only.");
            if (!replayAreas && PersistenceReplayTargets != null) errors.Add("Only repeated-area requests may carry route targets.");
            if (replayAreas)
            {
                if (PersistenceReplayTargets == null || PersistenceReplayTargets.Length != 3) errors.Add("Repeated areas require three exact targets.");
                else
                {
                    var prior = OwnedPairs != null ? PersistenceLoad?.Area : Fixture?.Working?.Area;
                    foreach (var target in PersistenceReplayTargets)
                    {
                        if (target == null || !IsLowerHex(target.EnterPoint, 32) || !IsLowerHex(target.Area, 32) || target.EnterPoint == new string('0', 32) ||
                            target.Area == new string('0', 32) || target.EnterPoint == target.Area || target.Area == prior || target.AutoSaveMode != "None")
                            errors.Add("Repeated area target must be exact, differ from the prior area, and forbid autosaving.");
                        prior = target?.Area;
                    }
                }
            }
            var ownedSchema1 = Scenario == Chunk7OwnedPairsSelection.Scenario && OwnedPairs?.Cold == true && Chunk7OwnedPairsSelection.OwnedSchema1Cold(PersistenceCase);
            if ((Chunk7OwnedPairsSelection.OwnedSchema1Source(PersistenceCase) || Chunk7OwnedPairsSelection.OwnedSchema1Cold(PersistenceCase)) &&
                (Scenario != Chunk7OwnedPairsSelection.Scenario || OwnedPairs == null ||
                OwnedPairs.Case != (Chunk7OwnedPairsSelection.OwnedSchema1Cold(PersistenceCase) ? "persistence-load" : "persistence-save")))
                errors.Add("Owned schema-1 fixture cases require their exact directional inherited source/cold family.");
            var schema1 = Scenario == "chunk7-persistence-load" && PersistenceCase == "legacy-schema1-resave-manual" || ownedSchema1;
            if (PersistenceCase == "legacy-schema1-resave-manual" && !schema1)
                errors.Add("Schema-1 resave is a cold-load-only case.");
            if (schema1 ? Schema1DerivationSha256 == null || !Regex.IsMatch(Schema1DerivationSha256, "^[0-9a-f]{64}$", RegexOptions.CultureInvariant) : Schema1DerivationSha256 != null)
                errors.Add("Schema-1 derivative provenance must be exact and is exclusive to its cold-load case.");
            var p07 = Scenario == "persistence-p07-save" || Scenario == "persistence-p07-load";
            var p06 = Scenario == "persistence-p06-load";
            var p03 = Scenario == "persistence-p03-save" || Scenario == "persistence-p03-load";
            var p05 = Scenario == "persistence-p05-save" || Scenario == "persistence-p05-load";
            var p04 = Scenario == "persistence-p04-save" || Scenario == "persistence-p04-load";
            if (chunk7 ? !IsChunk7PersistenceCase(PersistenceCase) : p07 ? Array.IndexOf(new[] { "timeout", "cancel-wait", "locked-replace", "serialization-cancel", "serialization-cancel-output", "disable-reenable", "campaign-b", "prepare-removal", "disable-during-load", "mounted-charge-area", "mounted-charge-session", "mounted-charge-disable", "mounted-charge-removal", "absent-kmc", "removal-no-dll", "mounted-charge-removal-no-dll", "rider-death", "mount-death", "rider-size-change", "area-reload", "area-cross-entry", "area-cross-exit", "area-cross-entry-auto", "area-cross-exit-auto" }, PersistenceCase) < 0 :
                p06 ? Array.IndexOf(new[] { "legacy", "schema1", "future", "malformed", "profile", "campaign", "foreign-header-campaign", "missing-rider", "missing-mount", "mismatched-profile", "policy", "combat-missing", "combat-ai", "failed-area-load" }, PersistenceCase) < 0 :
                p05 ? Array.IndexOf(Scenario == "persistence-p05-load" ?
                new[] { "manual", "quick", "auto", "manual-renamed", "alternating", "queued" } : new[] { "manual", "quick", "auto", "alternating", "queued" }, PersistenceCase) < 0 :
                p04 ? Array.IndexOf(new[] { "unmounted-spent", "mounted-spent", "unmounted-attack", "mounted-attack", "unmounted-projectile", "mounted-projectile", "unmounted-approach", "mounted-approach", "unmounted-casting", "mounted-casting", "mounted-casting-items", "combat-mount-rt", "combat-dismount-rt", "mounted-charge-pending", "mounted-charge-settled", "mounted-charge-cancelled", "mounted-charge-failed", "mounted-charge-drained", "combat-mount-routes", "combat-mount-unsettled-save", "combat-mount-area-reload", "pending-mount-area" }, PersistenceCase) < 0 : p03 ? Array.IndexOf(new[] { "step", "conversion", "round-effect", "reaction", "condition", "condition-preparing", "suspended" }, PersistenceCase) < 0 :
                PersistenceCase != null && (Scenario != "persistence-p02-save" && Scenario != "persistence-p02-load" ||
                Array.IndexOf(new[] { "partial-movement", "rider-spent", "between-partner-orders", "exhausted", "explicit-end", "combat-mount-tb", "casting-items" }, PersistenceCase) < 0))
                errors.Add("Persistence case is outside its exact combat checkpoint contract.");
            // Removal preparation and disable-during-load write and probe a live
            // world; the integration-absent case only ever opens a cleanup archive.
            if (Scenario == "persistence-p07-load" && (PersistenceCase == "prepare-removal" || PersistenceCase == "disable-during-load" || IsChargeLifecycleCase(PersistenceCase)))
                errors.Add("A removal or disable-during-load case is save-only.");
            if (Scenario == "persistence-p07-save" && (PersistenceCase == "absent-kmc" || (PersistenceCase == "removal-no-dll" || PersistenceCase == "mounted-charge-removal-no-dll")))
                errors.Add("The integration-absent and no-DLL cases are cold-load only.");
            if (Scenario == Chunk7OwnedPairsSelection.Scenario)
            {
                if (PersistenceLoad == null || OwnedPairs == null) errors.Add("Inherited pairs require their exact qualified native input descriptor.");
                else
                {
                    var inputName = OwnedPairs.Cold ? (PersistenceCase == "multi-exploration-quick" || (PersistenceCase == "multi-exploration-auto" || PersistenceCase == "multi-area-autosave") ? PersistenceLoad.InternalName : "KMC_C7_WORLD") : Chunk7AcquiredManualSelection.OutputName;
                    var inputPattern = OwnedPairs.Cold ? (PersistenceCase == "multi-exploration-quick" ? "^Quick_1\\.zks$" : (PersistenceCase == "multi-exploration-auto" || PersistenceCase == "multi-area-autosave") ? "^Auto_1\\.zks$" : "^Manual_[1-9][0-9]*_KMC_C7_WORLD\\.zks$") : "^Manual_[1-9][0-9]*_KMC_C7_ACQUIRED_FIELD\\.zks$";
                    errors.AddRange(PersistenceLoad.Validate("persistenceLoad", inputName, inputPattern));
                    if (PersistenceCase == "multi-area-autosave" && PersistenceLoad.Area !=
                        (OwnedPairs.Cold ? PersistenceAreaTarget?.Area : OwnedPairs.SourceArea))
                        errors.Add("Owned area input differs from the selected original Manual or completed destination Auto.");
                    if (Fixture?.Working == null || PersistenceLoad.GameId == Fixture.Working.GameId ||
                        !Guid.TryParseExact(PersistenceLoad.GameId, "D", out var inheritedGameId) || inheritedGameId == Guid.Empty ||
                        PersistenceLoad.Sha256 == Fixture.Working.Sha256 || PersistenceLoad.Sha256 == Fixture.Baseline?.Sha256 ||
                        !OwnedPairs.Cold && PersistenceLoad.Sha256 != OwnedPairs.SourceArchiveSha256)
                        errors.Add("Inherited pairs cannot borrow Working, Baseline, a foreign native world or unqualified source bytes.");
                }
            }
            else if (Scenario == Chunk7CumulativeAcquisitionSelection.Scenario)
            {
                if (PersistenceLoad == null) errors.Add("Cumulative acquisition requires its exact acquired-field Manual source.");
                else
                {
                    var cumulativeName = CumulativeAcquisition?.SelectedManualName ?? Chunk7AcquiredManualSelection.OutputName;
                    errors.AddRange(PersistenceLoad.Validate("persistenceLoad", cumulativeName, "^Manual_[1-9][0-9]*_" + cumulativeName + "\\.zks$"));
                    if (CumulativeAcquisition == null || !CumulativeAcquisition.IsSelectedManualLeaf(PersistenceLoad.FileName) || Fixture?.Working == null ||
                        PersistenceLoad.GameId == Fixture.Working.GameId || !Guid.TryParseExact(PersistenceLoad.GameId, "D", out var cumulativeGameId) || cumulativeGameId == Guid.Empty ||
                        string.IsNullOrWhiteSpace(PersistenceLoad.GameName) || PersistenceLoad.Sha256 == Fixture.Working.Sha256 ||
                        PersistenceLoad.Sha256 == Fixture.Baseline?.Sha256 || PersistenceLoad.Sha256 != CumulativeAcquisition?.SourceArchiveSha256)
                        errors.Add("Cumulative source lacks its exact separately minted first-owner Manual identity.");
                }
            }
            else if (Scenario == Chunk7ManualDerivedSelection.Scenario)
            {
                if (PersistenceLoad == null) errors.Add("Manual-derived acquisition requires its exact native Manual seed source.");
                else
                {
                    errors.AddRange(PersistenceLoad.Validate("persistenceLoad", Chunk7ManualSeedSelection.OutputName, "^Manual_1_KMC_C7_NATIVE_SEED\\.zks$"));
                    if (Fixture?.Working == null || PersistenceLoad.GameId == Fixture.Working.GameId ||
                        !Guid.TryParseExact(PersistenceLoad.GameId, "D", out var mintedDerivedId) || mintedDerivedId == Guid.Empty ||
                        string.IsNullOrWhiteSpace(PersistenceLoad.GameName) || PersistenceLoad.Sha256 == Fixture.Working.Sha256 ||
                        PersistenceLoad.Sha256 == Fixture.Baseline?.Sha256 || PersistenceLoad.Sha256 != ManualDerived?.SourceArchiveSha256)
                        errors.Add("Manual-derived source lacks its exact separately minted same-candidate identity.");
                }
            }
            else if (Scenario == Chunk7AcquiredManualSelection.Scenario)
            {
                if (PersistenceLoad == null) errors.Add("Acquired Manual requires its exact native Auto_1 source.");
                else
                {
                    if (string.IsNullOrWhiteSpace(PersistenceLoad.InternalName) || PersistenceLoad.InternalName.Length > 256 || PersistenceLoad.InternalName.Any(char.IsControl))
                        errors.Add("Native bootstrap autosave name is missing or invalid.");
                    errors.AddRange(PersistenceLoad.Validate("persistenceLoad", PersistenceLoad.InternalName, "^Auto_1\\.zks$"));
                    if (Fixture?.Working == null || PersistenceLoad.GameId == Fixture.Working.GameId ||
                        !Guid.TryParseExact(PersistenceLoad.GameId, "D", out var mintedAcquiredId) || mintedAcquiredId == Guid.Empty ||
                        string.IsNullOrWhiteSpace(PersistenceLoad.GameName) || PersistenceLoad.Sha256 == Fixture.Working.Sha256 ||
                        PersistenceLoad.Sha256 == Fixture.Baseline?.Sha256 || PersistenceLoad.Sha256 != AcquiredManual?.SourceArchiveSha256)
                        errors.Add("Acquired Manual source lacks its exact separately minted disposable identity.");
                }
            }
            else if (Scenario == Chunk7ManualSeedSelection.Scenario)
            {
                if (PersistenceLoad == null) errors.Add("Manual seed requires its exact native Auto_1 source.");
                else
                {
                    if (string.IsNullOrWhiteSpace(PersistenceLoad.InternalName) || PersistenceLoad.InternalName.Length > 256 || PersistenceLoad.InternalName.Any(char.IsControl))
                        errors.Add("Native bootstrap autosave name is missing or invalid.");
                    errors.AddRange(PersistenceLoad.Validate("persistenceLoad", PersistenceLoad.InternalName, "^Auto_1\\.zks$"));
                    if (Fixture?.Working == null || PersistenceLoad.GameId == Fixture.Working.GameId ||
                        !Guid.TryParseExact(PersistenceLoad.GameId, "D", out var mintedSeedId) || mintedSeedId == Guid.Empty ||
                        string.IsNullOrWhiteSpace(PersistenceLoad.GameName) || PersistenceLoad.Sha256 == Fixture.Working.Sha256 ||
                        PersistenceLoad.Sha256 == Fixture.Baseline?.Sha256 || PersistenceLoad.Sha256 != ManualSeed?.SourceArchiveSha256)
                        errors.Add("Manual seed source lacks its exact separately minted disposable identity.");
                }
            }
            else if (Scenario == Chunk7AssistedColdInventorySelection.Scenario)
            {
                if (PersistenceLoad == null) errors.Add("Assisted cold inventory requires its exact qualified final Manual.");
                else if (AssistedColdInventory?.IsOwnerSeed == true)
                {
                    // Registry-supplied exact names, admitted generically; never the producer fixture identity.
                    errors.AddRange(PersistenceLoad.Validate("persistenceLoad", PersistenceLoad.InternalName, "^[A-Za-z0-9][A-Za-z0-9_.-]{0,95}\\.zks$"));
                    if (!Chunk7AssistedColdInventorySelection.IsOwnerSeedName(PersistenceLoad.InternalName) ||
                        !Chunk7AssistedColdInventorySelection.IsOwnerSeedLeaf(PersistenceLoad.FileName) ||
                        Fixture?.Working == null || PersistenceLoad.GameId == Fixture.Working.GameId || PersistenceLoad.GameId == Fixture.Baseline?.GameId ||
                        !Guid.TryParseExact(PersistenceLoad.GameId, "D", out var ownerSeedId) || ownerSeedId == Guid.Empty ||
                        string.IsNullOrWhiteSpace(PersistenceLoad.GameName) || PersistenceLoad.Sha256 == Fixture.Working.Sha256 ||
                        PersistenceLoad.Sha256 == Fixture.Baseline?.Sha256 || PersistenceLoad.Sha256 != AssistedColdInventory.SeedSha256)
                        errors.Add("Owner seed lacks its exact registered non-aliasing identity.");
                }
                else
                {
                    errors.AddRange(PersistenceLoad.Validate("persistenceLoad", Chunk7AssistedColdInventorySelection.ManualName, "^Manual_[1-9][0-9]*_KMC_C7_ASSISTED_FIXTURE\\.zks$"));
                    if (Fixture?.Working == null || !Chunk7AssistedColdInventorySelection.IsManualLeaf(PersistenceLoad.FileName) || PersistenceLoad.GameId == Fixture.Working.GameId ||
                        !Guid.TryParseExact(PersistenceLoad.GameId, "D", out var assistedColdId) || assistedColdId == Guid.Empty ||
                        string.IsNullOrWhiteSpace(PersistenceLoad.GameName) || PersistenceLoad.Sha256 == Fixture.Working.Sha256 ||
                        PersistenceLoad.Sha256 == Fixture.Baseline?.Sha256 || PersistenceLoad.Sha256 != AssistedColdInventory?.SourceArchiveSha256)
                        errors.Add("Assisted cold source lacks its exact separately minted disposable identity.");
                }
            }
            else if (Scenario == Chunk7BootstrapInventorySelection.Scenario)
            {
                if (PersistenceLoad == null) errors.Add("Bootstrap cold inventory requires its exact native Auto_1 source.");
                else
                {
                    if (string.IsNullOrWhiteSpace(PersistenceLoad.InternalName) || PersistenceLoad.InternalName.Length > 256 || PersistenceLoad.InternalName.Any(char.IsControl))
                        errors.Add("Native bootstrap autosave name is missing or invalid.");
                    errors.AddRange(PersistenceLoad.Validate("persistenceLoad", PersistenceLoad.InternalName, "^Auto_1\\.zks$"));
                    if (Fixture?.Working == null || PersistenceLoad.GameId == Fixture.Working.GameId ||
                        !Guid.TryParseExact(PersistenceLoad.GameId, "D", out var mintedBootstrapId) || mintedBootstrapId == Guid.Empty ||
                        string.IsNullOrWhiteSpace(PersistenceLoad.GameName) || PersistenceLoad.Sha256 == Fixture.Working.Sha256 ||
                        PersistenceLoad.Sha256 == Fixture.Baseline?.Sha256 || PersistenceLoad.Sha256 != BootstrapInventory?.SourceArchiveSha256)
                        errors.Add("Bootstrap inventory source lacks its exact separately minted disposable identity.");
                }
            }
            else if (Scenario == Chunk7SmallCalibrationSelection.Scenario && SmallCalibration?.UsesManualField == true)
            {
                if (PersistenceLoad == null) errors.Add("Small Manual calibration requires its exact qualified field Manual.");
                else
                {
                    errors.AddRange(PersistenceLoad.Validate("persistenceLoad", Chunk7AcquiredManualSelection.OutputName, "^Manual_[1-9][0-9]*_KMC_C7_ACQUIRED_FIELD\\.zks$"));
                    if (Fixture?.Working == null || PersistenceLoad.GameId == Fixture.Working.GameId ||
                        !Chunk7SmallManualSourceSelection.IsNativeManualLeaf(PersistenceLoad.FileName) || !Guid.TryParseExact(PersistenceLoad.GameId, "D", out var smallManualGameId) || smallManualGameId == Guid.Empty ||
                        string.IsNullOrWhiteSpace(PersistenceLoad.GameName) || PersistenceLoad.Sha256 == Fixture.Working.Sha256 ||
                        PersistenceLoad.Sha256 == Fixture.Baseline?.Sha256 || PersistenceLoad.Sha256 != SmallCalibration.ManualSource?.SourceArchiveSha256)
                        errors.Add("Small Manual source lacks its exact separately minted disposable identity.");
                }
            }
            else if (Scenario == Chunk7OrdinaryFieldSelection.Scenario || Scenario == Chunk7SmallCalibrationSelection.Scenario)
            {
                if (PersistenceLoad == null) errors.Add("Read-only field admission requires its exact paid Auto_1 archive.");
                else
                {
                    if (string.IsNullOrWhiteSpace(PersistenceLoad.InternalName) || PersistenceLoad.InternalName.Length > 256 || PersistenceLoad.InternalName.Any(char.IsControl))
                        errors.Add("Native paid autosave name is missing or invalid.");
                    errors.AddRange(PersistenceLoad.Validate("persistenceLoad", PersistenceLoad.InternalName, "^Auto_1\\.zks$"));
                    if (Fixture?.Working == null || PersistenceLoad.GameId != Fixture.Working.GameId || PersistenceLoad.GameName != Fixture.Working.GameName ||
                        PersistenceLoad.Area != Fixture.Working.Area || PersistenceLoad.Sha256 == Fixture.Working.Sha256 ||
                        PersistenceLoad.Sha256 == Fixture.Baseline?.Sha256 || PersistenceLoad.Sha256 != (Scenario == Chunk7SmallCalibrationSelection.Scenario ? SmallCalibration?.Source?.SourceArchiveSha256 : OrdinaryField?.SourceArchiveSha256))
                        errors.Add("Field admission archive differs from its exact disposable acquisition identity.");
                }
            }
            else if (Scenario == Chunk7ProgressionSelection.Scenario)
            {
                if (Progression?.IsOwned == true)
                    errors.AddRange(Progression.ValidateOwnedDescriptor(PersistenceLoad, Fixture));
                else if (PersistenceLoad == null) errors.Add("Native progression requires its exact same-candidate Manual300 source.");
                else
                {
                    errors.AddRange(PersistenceLoad.Validate("persistenceLoad", Chunk7ProgressionSelection.SourceName, "^Manual_300_KMC_C7_WORLD\\.zks$"));
                    if (Fixture?.Working == null || PersistenceLoad.GameId != Fixture.Working.GameId || PersistenceLoad.GameName != Fixture.Working.GameName ||
                        PersistenceLoad.Area != Fixture.Working.Area || PersistenceLoad.Sha256 == Fixture.Working.Sha256 ||
                        PersistenceLoad.Sha256 == Fixture.Baseline?.Sha256 || PersistenceLoad.Sha256 != Progression?.SourceArchiveSha256)
                        errors.Add("Native progression source differs from its declared disposable archive.");
                }
            }
            else if (Scenario == "chunk7-persistence-load")
            {
                if (PersistenceLoad == null) errors.Add("Chunk 7 cold loading requires its exact completed source archive.");
                else
                {
                    var type = ExpectedNativeLoadType;
                    var nativeSlot = type != "Manual";
                    var leaf = schema1 ? "^Manual_812_KMC_P06\\.zks$" : nativeSlot ? "^" + type + "_1\\.zks$" : "^Manual_300_KMC_C7_WORLD\\.zks$";
                    if (nativeSlot && (string.IsNullOrWhiteSpace(PersistenceLoad.InternalName) ||
                        PersistenceLoad.InternalName.Length > 256 || PersistenceLoad.InternalName.Any(char.IsControl)))
                        errors.Add("Chunk 7 native slot name is missing or oversized.");
                    errors.AddRange(PersistenceLoad.Validate("persistenceLoad", schema1 ? "KMC_P01" : nativeSlot ? PersistenceLoad.InternalName : "KMC_C7_WORLD", leaf));
                    if (Fixture?.Working == null || PersistenceLoad.GameId != Fixture.Working.GameId ||
                        PersistenceLoad.GameName != Fixture.Working.GameName || PersistenceLoad.Area != ExpectedChunk7ArchiveArea ||
                        PersistenceLoad.Sha256 == Fixture.Baseline?.Sha256)
                        errors.Add("Chunk 7 source must remain in its exact disposable campaign/area and must not alias Baseline.");
                }
            }
            else if (Scenario == "persistence-p07-load" || Scenario == "persistence-p01-load" || Scenario == "persistence-p02-load" || Scenario == "persistence-p03-load" || Scenario == "persistence-p04-load" || Scenario == "persistence-p05-load" || p06)
            {
                if (PersistenceLoad == null) errors.Add("Cold loading requires its actual owned archive identity.");
                else
                {
                    var nativeSlot = p05 && (PersistenceCase == "quick" || PersistenceCase == "auto") ||
                        IsTransitionAutoCase(PersistenceCase);
                    // The integration-absent cold load opens the cleanup archive a
                    // prepare-removal run wrote, under that archive's own name.
                    var cleanup = Scenario == "persistence-p07-load" && (PersistenceCase == "absent-kmc" || (PersistenceCase == "removal-no-dll" || PersistenceCase == "mounted-charge-removal-no-dll"));
                    // A death cold load opens the no-pair archive a death run wrote.
                    var death = Scenario == "persistence-p07-load" && IsDeathCase(PersistenceCase);
                    // An eligibility cold load opens the no-pair archive a size-change run wrote.
                    var eligibility = Scenario == "persistence-p07-load" && IsEligibilityCase(PersistenceCase);
                    // A campaign-B cold load opens B's own manual archive under B's
                    // engine-minted identity, which is exactly NOT the fixture's.
                    var campaignB = Scenario == "persistence-p07-load" && PersistenceCase == "campaign-b";
                    var slotPattern = nativeSlot ? (PersistenceCase == "quick" ? "^Quick_1\\.zks$" : "^Auto_1\\.zks$") :
                        cleanup ? "^Manual_301_KMC_CLEANUP\\.zks$" :
                        death ? "^Manual_301_KMC_DEATH\\.zks$" :
                        eligibility ? "^Manual_301_KMC_SIZE\\.zks$" :
                        campaignB ? "^Manual_302_KMC_B\\.zks$" :
                        p05 && PersistenceCase == "queued" ? "^Manual_302_KMC_P01\\.zks$" :
                        p05 && PersistenceCase == "manual-renamed" ? "^Manual_811_KMC_RENAMED\\.zks$" : "^Manual_300_KMC_P01\\.zks$";
                    if (nativeSlot && (string.IsNullOrWhiteSpace(PersistenceLoad.InternalName) ||
                        PersistenceLoad.InternalName.Length > 256 || PersistenceLoad.InternalName.Any(char.IsControl)))
                        errors.Add("Cold native slot name is missing or oversized.");
                    errors.AddRange(PersistenceLoad.Validate("persistenceLoad",
                        nativeSlot ? PersistenceLoad.InternalName : cleanup ? "KMC_CLEANUP" : death ? "KMC_DEATH" :
                        eligibility ? "KMC_SIZE" : campaignB ? "KMC_B" : "KMC_P01", slotPattern));
                    // A cross-area source archive is committed after the transition,
                    // so its native header carries the declared destination area.
                    // A transition autosave instead carries the area it actually
                    // committed in: the destination for AfterEntry, the departure
                    // area for BeforeExit.
                    var expectedArea = PersistenceAreaTarget == null ? Fixture?.Working?.Area :
                        IsTransitionAutoCase(PersistenceCase) ? ExpectedTransitionAutoArea :
                        IsCrossAreaCase(PersistenceCase) ? PersistenceAreaTarget.Area : Fixture?.Working?.Area;
                    if (campaignB)
                    {
                        if (Fixture?.Working == null || PersistenceLoad.GameId == Fixture.Working.GameId ||
                            !Guid.TryParse(PersistenceLoad.GameId, out var minted) || minted == Guid.Empty ||
                            string.IsNullOrWhiteSpace(PersistenceLoad.GameName) || PersistenceLoad.Area == Fixture.Working.Area)
                            errors.Add("A campaign-B cold archive must carry B's own minted identity and area, never the fixture's.");
                    }
                    else if (Fixture?.Working == null || PersistenceLoad.GameId != Fixture.Working.GameId ||
                        PersistenceLoad.GameName != Fixture.Working.GameName || PersistenceLoad.Area != expectedArea)
                        errors.Add("Cold archive campaign/area differs from the disposable fixture contract.");
                }
            }
            else if (PersistenceLoad != null) errors.Add("This scenario cannot select a persistence archive.");
            if (chunk7 && PersistenceCase == "multi-area-autosave")
            {
                if (PersistenceAreaTarget == null) errors.Add("Chunk 7 area autosave requires its exact native destination.");
                else
                {
                    errors.AddRange(PersistenceAreaTarget.Validate("persistenceAreaTarget", OwnedPairs != null ? OwnedPairs.SourceArea : Fixture?.Working?.Area));
                    if (PersistenceAreaTarget.AutoSaveMode != "AfterEntry") errors.Add("Chunk 7 area autosave requires AfterEntry.");
                }
            }
            else if (IsCrossAreaFamilyCase(PersistenceCase))
            {
                if (!p07) errors.Add("A cross-area transfer requires the exact P07 scenario.");
                if (PersistenceAreaTarget == null) errors.Add("A cross-area transfer requires its declared native destination.");
                else errors.AddRange(PersistenceAreaTarget.Validate("persistenceAreaTarget", Fixture?.Working?.Area));
                // The transition autosave already exists; it is only ever loaded.
                if (IsTransitionAutoCase(PersistenceCase) && Scenario != "persistence-p07-load")
                    errors.Add("A transition autosave case is cold-load only.");
                if (IsTransitionAutoCase(PersistenceCase) && PersistenceAreaTarget != null &&
                    PersistenceAreaTarget.AutoSaveMode !=
                    (PersistenceCase == "area-cross-entry-auto" ? "AfterEntry" : "BeforeExit"))
                    errors.Add("A transition autosave case must declare the mode that produced it.");
            }
            else if (PersistenceAreaTarget != null)
                errors.Add("Only an exact cross-area transfer may declare a native destination.");
            if (p06 || Scenario == "persistence-p05-load" && PersistenceCase == "alternating")
            {
                if (PersistenceAlternate == null) errors.Add("Alternating native loads require the second exact archive.");
                else
                {
                    // The failed-load variant is the one P06 derivative that edits
                    // a native member, so it gets its own leaf and must differ in
                    // bytes; the metadata-only 'policy' case is deliberately equal.
                    var failedLoad = IsFailedLoad(PersistenceCase);
                    // The foreign-header derivative is campaign B's own archive
                    // whose KMC member claims A: its native header is B's.
                    var foreignHeader = p06 && PersistenceCase == "foreign-header-campaign";
                    errors.AddRange(PersistenceAlternate.Validate("persistenceAlternate", foreignHeader ? "KMC_B" : p06 ? "KMC_P01" : "KMC_P05_UNMOUNTED",
                        failedLoad ? "^Manual_813_KMC_P06_AREA\\.zks$" :
                        p06 ? "^Manual_812_KMC_P06\\.zks$" : "^Manual_301_KMC_P05_UNMOUNTED\\.zks$"));
                    if (foreignHeader)
                    {
                        if (PersistenceLoad == null || PersistenceAlternate.Sha256 == PersistenceLoad.Sha256 ||
                            PersistenceAlternate.GameId == PersistenceLoad.GameId || !Guid.TryParse(PersistenceAlternate.GameId, out var minted) ||
                            minted == Guid.Empty || PersistenceAlternate.Area == PersistenceLoad.Area)
                            errors.Add("A foreign-header derivative must carry B's own native identity and area, distinct from A's.");
                    }
                    else if (PersistenceLoad == null || ((!p06 || failedLoad) && PersistenceAlternate.Sha256 == PersistenceLoad.Sha256) ||
                        PersistenceAlternate.GameId != PersistenceLoad.GameId ||
                        PersistenceAlternate.GameName != PersistenceLoad.GameName ||
                        PersistenceAlternate.Area != PersistenceLoad.Area)
                        errors.Add("Alternating archive must be distinct and from the exact same native campaign/area.");
                }
            }
            else if (PersistenceAlternate != null) errors.Add("Only exact P05 alternating or P06 validation loads may select a second archive.");
            if (SaveAccessAllowed || !string.IsNullOrEmpty(SaveName))
            {
                errors.Add("Schema v2 uses only its exact fixture write authorization.");
            }

            if (!IsSaveBackedScenario(Scenario))
            {
                errors.Add("scenario is outside the exact save-backed mission allowlist.");
            }

            if (Fixture == null)
            {
                errors.Add("fixture is required for schema-v2 requests.");
                return;
            }

            errors.AddRange(Fixture.Validate());
            if (QualificationSuite == null)
            {
                errors.Add("qualificationSuite is required for schema-v2 requests.");
            }
            else
            {
                errors.AddRange(QualificationSuite.Validate());
            }
            if (Fixture.WriteAuthorization != null)
            {
                var expectedMode = IsManualReviewScenario(Scenario) ? "read-only" : "working-only";
                if (!string.Equals(Fixture.WriteAuthorization.Mode, expectedMode, StringComparison.Ordinal))
                {
                    errors.Add("fixture.writeAuthorization.mode does not match the selected runtime scenario.");
                }
            }
        }
    }

    public sealed class RuntimeQualificationSuiteIdentity
    {
        public string SuiteId { get; set; }

        public string SnapshotSha256 { get; set; }

        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();
            if (string.IsNullOrEmpty(SuiteId) || !Regex.IsMatch(SuiteId, "^[A-Za-z0-9._-]{1,120}$", RegexOptions.CultureInvariant))
            {
                errors.Add("qualificationSuite.suiteId is outside the exact runtime allowlist.");
            }

            RuntimeRequest.RequireSha256(errors, SnapshotSha256, "qualificationSuite.snapshotSha256");
            return errors;
        }
    }

    public sealed class RuntimeFixtureIdentity
    {
        public RuntimeSaveDescriptor Baseline { get; set; }

        public RuntimeSaveDescriptor Working { get; set; }

        public RuntimeSaveWriteAuthorization WriteAuthorization { get; set; }

        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();
            if (Baseline == null)
            {
                errors.Add("fixture.baseline is required.");
            }
            else
            {
                errors.AddRange(Baseline.Validate("fixture.baseline", RuntimeRequest.BaselineSaveName, "^Manual_[0-9]+_KMC_AUTOMATION_BASELINE\\.zks$"));
            }

            if (Working == null)
            {
                errors.Add("fixture.working is required.");
            }
            else
            {
                errors.AddRange(Working.Validate("fixture.working", RuntimeRequest.WorkingSaveName, "^Manual_[0-9]+_KMC_AUTOMATION_WORKING\\.zks$"));
            }

            if (Baseline != null && Working != null)
            {
                if (string.Equals(Baseline.FileName, Working.FileName, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add("fixture save leaves must be distinct.");
                }

                if (!string.Equals(Baseline.GameId, Working.GameId, StringComparison.Ordinal) ||
                    !string.Equals(Baseline.GameName, Working.GameName, StringComparison.Ordinal) ||
                    !string.Equals(Baseline.Area, Working.Area, StringComparison.Ordinal))
                {
                    errors.Add("fixture GameId, GameName, and Area must match as raw ordinal strings.");
                }
            }

            if (WriteAuthorization == null)
            {
                errors.Add("fixture.writeAuthorization is required.");
            }
            else
            {
                errors.AddRange(WriteAuthorization.Validate(Working));
            }

            return errors;
        }
    }

    public sealed class RuntimeSaveDescriptor
    {
        public string InternalName { get; set; }

        public string FileName { get; set; }

        public string Sha256 { get; set; }

        public long Length { get; set; }

        public long LastWriteTimeUtcTicks { get; set; }

        public string GameId { get; set; }

        public string GameName { get; set; }

        public string Area { get; set; }

        internal IReadOnlyList<string> Validate(string prefix, string expectedName, string fileNamePattern)
        {
            var errors = new List<string>();
            if (!string.Equals(InternalName, expectedName, StringComparison.Ordinal))
            {
                errors.Add(prefix + ".internalName is not exact.");
            }

            if (string.IsNullOrEmpty(FileName) || !Regex.IsMatch(FileName, fileNamePattern, RegexOptions.CultureInvariant))
            {
                errors.Add(prefix + ".fileName is not a canonical KMC fixture leaf.");
            }

            RuntimeRequest.RequireSha256(errors, Sha256, prefix + ".sha256");
            if (Length <= 0)
            {
                errors.Add(prefix + ".length must be positive.");
            }

            if (LastWriteTimeUtcTicks <= 0 || LastWriteTimeUtcTicks > DateTime.MaxValue.Ticks)
            {
                errors.Add(prefix + ".lastWriteTimeUtcTicks is outside the DateTime range.");
            }

            RuntimeRequest.RequireGuid(errors, GameId, prefix + ".gameId");
            RuntimeRequest.Require(errors, GameName, prefix + ".gameName");
            if (!RuntimeRequest.IsLowerHex(Area, 32))
            {
                errors.Add(prefix + ".area must be an exact lowercase 32-character blueprint GUID.");
            }

            return errors;
        }
    }

    // The exact native destination of an ordinary cross-area transfer. Both the
    // enter point and its area are declared so the scenario can verify the
    // resolved blueprint rather than trusting whatever the engine loads.
    public sealed class RuntimeAreaTransitionTarget
    {
        public string EnterPoint { get; set; }

        public string Area { get; set; }

        public string AutoSaveMode { get; set; }

        internal IReadOnlyList<string> Validate(string prefix, string workingArea)
        {
            var errors = new List<string>();
            if (!RuntimeRequest.IsLowerHex(EnterPoint, 32))
                errors.Add(prefix + ".enterPoint must be an exact lowercase 32-character blueprint GUID.");
            if (!RuntimeRequest.IsLowerHex(Area, 32))
                errors.Add(prefix + ".area must be an exact lowercase 32-character blueprint GUID.");
            if (Area != null && Area == workingArea)
                errors.Add(prefix + ".area must differ from the loaded fixture area for a real transfer.");
            if (AutoSaveMode != "BeforeExit" && AutoSaveMode != "AfterEntry")
                errors.Add(prefix + ".autoSaveMode must be an ordinary authored BeforeExit or AfterEntry transition.");
            return errors;
        }
    }

    public sealed class RuntimeSaveWriteAuthorization
    {
        public string Mode { get; set; }

        public string AllowedInternalName { get; set; }

        public string AllowedFileName { get; set; }

        public bool BaselineImmutable { get; set; }

        internal IReadOnlyList<string> Validate(RuntimeSaveDescriptor working)
        {
            var errors = new List<string>();
            var workingOnly = string.Equals(Mode, "working-only", StringComparison.Ordinal);
            var readOnly = string.Equals(Mode, "read-only", StringComparison.Ordinal);
            if (!workingOnly && !readOnly)
            {
                errors.Add("fixture.writeAuthorization.mode must be working-only or read-only.");
            }

            if (workingOnly && !string.Equals(AllowedInternalName, RuntimeRequest.WorkingSaveName, StringComparison.Ordinal))
            {
                errors.Add("fixture.writeAuthorization.allowedInternalName is not exact.");
            }

            if (workingOnly && (working == null || !string.Equals(AllowedFileName, working.FileName, StringComparison.Ordinal)))
            {
                errors.Add("fixture.writeAuthorization.allowedFileName does not match Working.");
            }

            if (readOnly && (!string.IsNullOrEmpty(AllowedInternalName) || !string.IsNullOrEmpty(AllowedFileName)))
            {
                errors.Add("fixture.writeAuthorization read-only mode must not name an allowed save target.");
            }

            if (!BaselineImmutable)
            {
                errors.Add("fixture.writeAuthorization must require baseline immutability.");
            }

            return errors;
        }
    }

    public sealed class RuntimeSubscenarioResult
    {
        private static readonly HashSet<string> MissionScenarios = new HashSet<string>(StringComparer.Ordinal)
        {
            "chunk7-two-pair-combat-rider-first", "chunk7-two-pair-combat-mount-first",
            "chunk7-two-pair-realtime-core", "chunk7-two-pair-realtime-casting", "chunk7-two-pair-realtime-charge",
            "chunk7-two-pair-native-life-cleanup",
            "C7-combat-fixture", "C7-combat-native-mounts", "C7-combat-native-orders", "C7-combat-pair-a-full-activation",
            "C7-combat-pair-b-full-activation", "C7-combat-tb-charge-refusal", "C7-combat-refresh", "C7-combat-r1-round-release",
            "C7-combat-native-pending-survivor", "C7-combat-local-cleanup-failure", "C7-combat-cleanup", "C7-combat-runtime", "C7-combat-interrupted",
            "C7-life-safe-native-damage", "C7-life-native-callback", "C7-life-healthy-neighbour", "C7-life-independent-survivor",
            "C7-rt-charge-default-off", "C7-rt-concurrent-primary", "C7-rt-local-cancel", "C7-rt-unmounted-native-control",
            "C7-rt-native-casting", "C7-rt-casting-neighbour", "C7-rt-charge-native-success", "C7-rt-charge-neighbour-movement", "C7-rt-charge-native-cancel",
            "chunk7-owned-two-pair-campaign", "C7-owned-pairs-inherited-custody", "chunk7-acquired-manual-derivative", "C7-acquired-manual-source", "C7-acquired-manual-native-load", "C7-acquired-manual-restored-owner", "C7-acquired-manual-native-field", "C7-acquired-manual-numbering", "C7-acquired-manual-native-save", "C7-acquired-manual-no-arrangement", "C7-acquired-manual-cleanup", "C7-acquired-manual-runtime", "C7-acquired-manual-interrupted",
            "chunk7-manual-derived-acquisition", "C7-manual-derived-source", "C7-manual-derived-native-load", "C7-manual-derived-eligibility", "C7-manual-derived-native-choices", "C7-manual-derived-native-autosave", "C7-manual-derived-native-field", "C7-manual-derived-cleanup", "C7-manual-derived-runtime", "C7-manual-derived-interrupted",
            "chunk7-cumulative-acquisition", "C7-cumulative-source", "C7-cumulative-native-load", "C7-cumulative-eligibility", "C7-cumulative-native-choices", "C7-cumulative-native-autosave", "C7-cumulative-native-field", "C7-cumulative-cleanup", "C7-cumulative-runtime", "C7-cumulative-interrupted",
            "chunk7-bootstrap-manual-seed", "C7-manual-seed-source", "C7-manual-seed-native-load", "C7-manual-seed-native-save", "C7-manual-seed-no-arrangement", "C7-manual-seed-cleanup", "C7-manual-seed-runtime", "C7-manual-seed-interrupted",
            "chunk7-assisted-cold-inventory", "C7-assisted-cold-source", "C7-assisted-cold-native-load", "C7-assisted-cold-readonly", "C7-assisted-cold-cleanup", "C7-assisted-cold-runtime", "C7-assisted-cold-interrupted",
            "chunk7-bootstrap-cold-inventory", "C7-bootstrap-inventory-source", "C7-bootstrap-inventory-native-load", "C7-bootstrap-inventory-readonly", "C7-bootstrap-inventory-cleanup", "C7-bootstrap-inventory-runtime", "C7-bootstrap-inventory-interrupted",
            "chunk7-assisted-fixture-preparation", "C7-assisted-preflight", "C7-assisted-native-start", "C7-assisted-story", "C7-assisted-setup", "C7-assisted-native-choices", "C7-assisted-manual", "C7-assisted-cleanup", "C7-assisted-runtime", "C7-assisted-interrupted",
            "chunk7-native-main-bootstrap", "C7-bootstrap-preflight", "C7-bootstrap-native-start", "C7-bootstrap-native-world", "C7-bootstrap-autosave", "C7-bootstrap-cleanup", "C7-bootstrap-runtime", "C7-bootstrap-interrupted",
            "chunk7-small-profile-calibration", "C7-small-readonly-source", "C7-small-native-party-placement", "C7-small-production-mount", "C7-small-native-ground", "C7-small-rider-primary", "C7-small-mount-primary", "C7-small-native-dismount", "C7-small-native-pose-observation", "C7-small-cleanup", "C7-small-runtime", "C7-small-interrupted",
            "chunk7-native-progression", "C7-progression-cold-source", "C7-progression-native-operation", "C7-progression-cleanup", "C7-progression-runtime", "C7-progression-interrupted",
            "chunk7-ordinary-field-admission", "C7-field-readonly-source", "C7-field-native-party-placement", "C7-field-cleanup", "C7-field-runtime", "C7-field-interrupted",
            "chunk7-ordinary-acquisition", "C7-ordinary-fixture", "C7-ordinary-native-choices", "C7-ordinary-native-autosave",
            "C7-ordinary-native-field-inventory", "C7-ordinary-cleanup", "C7-ordinary-runtime", "C7-ordinary-interrupted",
            "chunk7-persistence-save", "chunk7-persistence-load", "C7-persistence-two-native-mounts", "C7-persistence-one-native-mount-legacy-source",
            "C7-persistence-native-category-all-pair-archive", "C7-persistence-cold-all-pair-identity-and-no-prepare",
            "C7-persistence-owned-schema1-source", "C7-persistence-schema1-native-restoration", "C7-persistence-schema1-native-resave", "C7-persistence-schema2-native-resave",
            "C7-persistence-three-native-loads", "C7-persistence-three-native-area-transfers",
            "C7-persistence-nonselected-mount-save-deferral", "C7-persistence-owned-worker-cancel-drain",
            "C7-persistence-native-continuation", "C7-persistence-all-pair-area-before-autosave", "C7-persistence-native-combat-continuation", "C7-persistence-cleanup", "C7-persistence-runtime", "C7-persistence-interrupted",
            "chunk7-two-pair-exploration", "C7-world-fixture", "C7-world-native-acquisition", "C7-world-two-native-mounts",
            "C7-world-selection-routing", "C7-world-independent-dismount", "C7-world-cycles", "C7-world-sampling", "C7-world-cleanup", "C7-world-runtime", "C7-world-visual", "C7-world-interrupted",
            "persistence-p07-save", "persistence-p07-load", "persistence-p01-save", "persistence-p01-load", "persistence-p02-save", "persistence-p02-load", "persistence-p03-save", "persistence-p03-load", "persistence-p04-save", "persistence-p04-load", "persistence-p05-save", "persistence-p05-load", "persistence-p06-load",
                    "C4-LIFE-rider-incapacitation",
        "C4-LIFE-rider-death-live-command",
        "C4-LIFE-mount-death-live-command",
        "C4-TARGETING-rider-heal",
        "C4-TARGETING-rider-hostile",
        "C4-TARGETING-mount-heal",
        "C4-TARGETING-mount-hostile",
        "C4-TARGETING-area-unmounted", "C4-TARGETING-area-both",
        "C4-GROUND-mounted-arrival",
        "C4-GROUND-unmounted-arrival",
        "C4-HORSE-mounted-three-primaries",
        "C4-HORSE-unmounted-strike-recovery",
        "C4-OBSTRUCTION-ranged-native-geometry", "C4-RANGED-native-mixed-range", "C4-INTERRUPT-melee-pause-resume", "C4-INTERRUPT-melee-pause-stop-recover", "C4-INTERRUPT-melee-moving-target", "C4-INTERRUPT-melee-retarget-windup", "C4-INTERRUPT-melee-target-death-windup", "C4-INTERRUPT-melee-target-death-midroutine", "C4-INTERRUPT-ranged-pause-resume", "C4-INTERRUPT-ranged-pause-stop-recover", "C4-INTERRUPT-ranged-moving-target", "C4-INTERRUPT-ranged-retarget-windup", "C4-INTERRUPT-ranged-retarget-inflight", "C4-INTERRUPT-ranged-target-death-windup", "C4-INTERRUPT-ranged-target-death-inflight", "C4-INSPECTION-rider", "C4-INSPECTION-mount", "C4-SESSION-RT-1", "C4-SESSION-RT-2", "C4-SESSION-RT-3", "C4-SESSION-TB-1", "C4-SESSION-TB-2", "C4-SESSION-TB-3",
        "C4-SUSTAINED-melee-adjacent-held",
            "C4-SUSTAINED-melee-adjacent-repeat",
            "C4-SUSTAINED-melee-approach-held",
            "C4-SUSTAINED-melee-approach-repeat",
            "C4-SUSTAINED-ranged-adjacent-held",
            "C4-SUSTAINED-ranged-adjacent-repeat",
            "C4-SUSTAINED-ranged-approach-held",
            "C4-SUSTAINED-ranged-approach-repeat",
            "C4-SUSTAINED-TB-rider-first",
            "C4-SUSTAINED-TB-mount-first",
            "C4-SUSTAINED-TB-rider-exhausted",
            "C4-SUSTAINED-TB-mount-exhausted",
            "C4-SUSTAINED-TB-early-end",
            "C4-SUSTAINED-TB-after-early-end",
            "C6D-move-cast-move", "C6D-cast-then-move", "C6D-double-move-ranged", "C6D-movement-exhausted", "C6D-auto-stop-boundary",
            "C6E-swift-on-own-turn", "C6E-swift-out-of-turn", "C6E-attack-on-mount-observed", "C6E-reaction-window",
            "C6C-quickened-self", "C6C-standard-self", "C6C-standard-friendly", "C6C-standard-hostile", "C6C-prepared-interrupt-after", "C6C-scroll-interrupt-after", "C6C-invalid-target", "C6C-cancel-before", "C6C-interrupt-before", "C6C-potion-self", "C6C-scroll-friendly", "C6C-full-round", "C6C-movement-policy", "C6C-rider-incapacity", "C6C-mount-incapacity", "C6C-under-threat",
            "C6B-CHARGE-default-off", "C6B-CHARGE-positive", "C6B-CHARGE-below-minimum",
            "C6B-CHARGE-spent-standard", "C6B-CHARGE-stock-rejected",
            "C6B-CHARGE-interrupted", "C6B-CHARGE-combat-ended", "C6B-CHARGE-obstructed-line", "C6B-CHARGE-cancelled",
            "C6B-CHARGE-blocked-clearance", "C6B-CHARGE-exception-cleanup", "C6B-CHARGE-target-moved",
            "C6B-CHARGE-target-lost", "C6B-CHARGE-rider-incapacitated", "C6B-CHARGE-mount-incapacitated",
            "C6B-PATH-straight-arrival", "C6B-PATH-interrupt-stop",
            "C4-CHARGE-mounted-rider", "C4-CHARGE-unmounted-rider",
            "C4-CHARGE-mounted-mount", "C4-CHARGE-unrelated-actor", "C4-CHARGE-queued-state-change",
            "C01-B", "C01-C", "C01-D",
            "C03-rapid-off-B", "C03-rapid-off-C", "C03-bab-B", "C03-bab-C", "C03-haste-B", "C03-haste-C",
            "C02-restricted-B", "C02-restricted-C", "C03-single-B", "C03-single-C", "C03-spent-standard-B", "C03-spent-standard-C",
            "C03-rider-move-B", "C03-carried-move-C", "C03-mixed-range-B", "C03-mixed-range-C",
            "mod-load-smoke",
            "export-mounted-contracts",
            "export-candidate-mount-rigs",
            "observe-mount-diagnostic-availability",
            "horse-native-asset-audit",
            "horse-companion-blueprint-registration",
            "horse-companion-unmounted-suite",
            "horse-mounted-alpha-suite",
            "horse-native-controls-ux-suite",
            "chunk6a-mount-preamble",
            "chunk6c-casting-rt", "chunk6c-casting-tb", "chunk6c-casting-unmounted-rt", "chunk6c-casting-unmounted-tb",
            "chunk6d-staged-rt", "chunk6d-staged-tb", "chunk6e-reaction-rt", "chunk6e-reaction-tb",
            "chunk6b-charge-rt","chunk6b-charge-core-rt","chunk6b-charge-interruption-rt","chunk6b-charge-lifecycle-rt", "chunk6b-charge-tb",
            "chunk6b-charge-path-rt", "chunk6b-charge-path-tb",
            "chunk6a-mammoth-mount-rt", "chunk6a-mammoth-mount-tb", "chunk6a-paused-queue", "chunk6a-refused-policy-disabled","chunk6a-refused-foreign-companion","chunk6a-refused-wrong-creature-target","chunk6a-refused-mount-selected","chunk6a-refused-multiple-selection","chunk6a-refused-foreign-selection", "chunk6a-mount-approach", "chunk6a-geometry-change", "chunk6a-obstruction", "chunk6a-stop-approach", "chunk6a-combat-end-approach", "chunk6a-disable-approach", "chunk6a-command-replacement", "chunk6a-repeated-mount-request", "chunk6a-ownership-change", "chunk6a-size-form-change", "chunk6a-lost-direct-control", "chunk6a-rider-incapacitated", "chunk6a-mount-incapacitated", "chunk6a-hotbar-approach", "chunk6a-allocation-rider-first-tb","chunk6a-allocation-mount-first-tb","chunk6a-dismount-feature-disabled-rt","chunk6a-dismount-policy-disabled-rt","chunk6a-auto-use-mount-rt","chunk6a-auto-use-dismount-rt",
            "chunk6a-adoption-compensation-rt", "chunk6a-adoption-compensation-tb",
            "chunk6a-left-area", "chunk6a-view-agent-lost", "chunk6a-loading-cutscene", "chunk6a-generation-change",
            "chunk6a-injected-exception", "chunk6a-rider-death-approach", "chunk6a-mount-death-approach",
            "chunk6a-turn-end-approach-tb", "chunk6a-mode-exit-tb",
            "chunk6a-mount-spent-move-tb", "chunk6a-mount-spent-standard-tb", "chunk6a-mount-spent-all-tb", "chunk6a-rider-without-move-tb", "chunk6a-rider-other-action-tb", "chunk6a-unrelated-candidate-between-tb", "chunk6a-dismount-after-rider-expenditure-tb", "chunk6a-dismount-after-mount-expenditure-tb", "chunk6a-dismount-immediately-after-mount-tb",
            "chunk6a-combat-mount-rt", "chunk6a-combat-mount-tb", "chunk4-rider-incapacitation-tb", "chunk4-rider-death-tb", "chunk4-mount-death-tb", "chunk4-targeting-rider-rt", "chunk4-targeting-mount-rt", "chunk4-ground-arrival-rt", "chunk4-horse-strike-comparison-rt", "chunk4-targeting-area-unmounted-rt", "chunk4-obstruction-ranged-rt", "chunk4-ranged-native-control-rt", "chunk4-interrupt-melee-rt", "chunk4-interrupt-ranged-rt", "chunk4-inspection-rt", "chunk4-session-rt", "chunk4-session-tb", "chunk4-sustained-melee-rt", "chunk4-sustained-ranged-rt", "chunk4-sustained-tb", "chunk4-charge-safety-rt", "chunk4-charge-safety-tb", "actor-allocation-rider-first-tb", "actor-allocation-mount-first-tb", "actor-allocation-rider-first-unmounted-tb", "actor-allocation-mount-first-unmounted-tb", "ordinary-attack-controls-tb", "unmounted-attack-controls-rt", "phase3h-combat-loop-rt", "phase3h-combat-loop-tb", "phase3g-native-controls-rt", "phase3g-native-controls-tb", "phase3d-unified-combat-rt-suite",
            "CM01-combat-mount-setup", "CM01-exploration-dismount-costs-nothing",
            "CM01-native-mammoth-fixture", "CM01-native-mammoth-continuity", "CM01-native-mammoth-child-cleanup",
            "CM01-native-mammoth-restoration", "CM01-native-mammoth-artifact", "CM01-native-mammoth-interrupted",
            "CM06-paused-queue", "CM06-combat-mount-requires-qualified-paired-policy","CM02-foreign-companion","CM02-wrong-creature-target","CM06-mount-selected","CM06-multiple-selection","CM06-foreign-selection", "CM01-exploration-free", "CM02-approach-arrival",
            "CM02-geometry-change", "CM02-obstruction", "CM04-stop-during-approach", "CM04-combat-end", "CM04-disable-unload", "CM04-command-replacement", "CM06-repeated-request", "CM02-ownership-change", "CM02-size-form-change", "CM02-lost-direct-control", "CM02-rider-incapacitated", "CM02-mount-incapacitated", "CM06-hotbar-path", "CM03-rider-before-mount-slot","CM03-mount-slot-before-rider","CM03-next-round-activation","CM05-dismount-survives-feature-policy-disable","CM06-ai-auto-use",
            "CM01-combat-mount-cancel-costs-nothing", "CM01-combat-mount-accepted",
            "CM03-combat-mount-conserves-debt", "CM03-combat-mount-adoption-preparations",
            "CM06-combat-mount-repeat-refused", "CM05-combat-dismount-accepted",
            "CM05-combat-dismount-conserves-debt", "CM05-no-duplicate-mount-turn", "CM05-forced-detach",
            "CM02-left-area", "CM02-view-agent-lost", "CM02-loading-cutscene", "CM02-generation-change",
            "CM04-injected-exception", "CM04-rider-death", "CM04-mount-death", "CM04-turn-end", "CM04-mode-exit",
            "CM03-mount-spent-move", "CM03-mount-spent-standard", "CM03-mount-spent-all", "CM03-rider-without-move", "CM03-rider-other-action", "CM03-unrelated-candidate-between", "CM05-after-rider-expenditure", "CM05-after-mount-expenditure", "CM05-immediately-after-mount",
            "T01-native-allocation-trace", "A05-native-preparation-callbacks", "T02-native-exhaustion-refresh-trace", "3g-rider-longbow-ordinary", "3g-rider-longbow-primary", "3g-rider-melee-ordinary", "3g-rider-melee-primary",
            "3g-horse-bite-ordinary", "3g-horse-bite-primary", "3g-paused-dismount", "3g-paused-mount-stop",
            "3g-paused-mount-execute", "3g-paused-control-failure",
            "phase3d-unified-combat-tb-suite",
            "phase3d-horse-presentation-suite",
            "Horse-small-portrait-close-up",
            "saddle-icon",
            "Horse-pose-final-idle-walk-run-turn-stop",
            "mounted-single-rider-turn-portrait",
            "rider-primary-target-cancel-does-not-dismount",
            "rider-primary-rejection-does-not-dismount",
            "rider-primary-does-not-dismount-rt",
            "rider-primary-does-not-dismount-tb",
            "rider-primary-after-movement-does-not-dismount",
            "rider-primary-after-shared-turn-transition-does-not-dismount",
            "mounted-stock-click-melee-adjacent-rt",
            "mounted-stock-click-melee-approach-rt",
            "mounted-stock-click-melee-auto-repeat-rt",
            "mounted-stock-click-melee-cancel-rt",
            "mounted-stock-click-melee-rider-only-explicit",
            "mounted-stock-click-melee-mount-only-explicit",
            "mounted-stock-click-invalid-target-feedback",
            "mounted-stock-click-melee-shared-turn-tb",
            "mounted-separate-action-ledgers",
            "mounted-bow-adjacent-rt",
            "mounted-bow-approach-to-range-rt",
            "mounted-bow-auto-fire-rt",
            "mounted-bow-cancel-rt",
            "mounted-bow-shared-turn-tb",
            "mounted-ranged-line-of-sight",
            "mounted-ranged-cover-concealment",
            "mounted-ranged-does-not-force-melee",
            "mounted-ranged-aao-native-control",
            "mounted-crossbow-or-reload-control",
            "mounted-sling-control",
            "unmounted-ranged-control",
            "unmounted-stock-attack-control",
            "RT-to-TB-shared-turn",
            "TB-to-RT-shared-turn",
            "mount-in-combat-before-either-acted",
            "mount-in-combat-rider-already-acted",
            "mount-in-combat-mount-already-acted",
            "mount-ability-in-combat",
            "mounted-combat-start-single-initiative-entry",
            "mounted-rider-initiative-bonus",
            "mounted-turn-rider-portrait",
            "mounted-shared-turn-action-order",
            "mounted-five-foot-step-no-aao",
            "mounted-five-foot-step-distance",
            "mounted-five-foot-step-resource",
            "mounted-five-foot-step-after-movement-rejected",
            "mounted-ordinary-move-aao-control",
            "unmounted-five-foot-step-control",
            "dismount-in-combat-no-extra-turn",
            "dismount-ability-in-combat",
            "phase3d-horse-tranche-cleanup",
            "phase3d-horse-scenario-deadline",
            "phase3d-horse-leaf-deadline",
            "phase3d-horse-runtime-exception",
            "phase3d-horse-host-interrupted",
            "player-action-availability",
            "mount-dismount-user-flow",
            "mounted-pair-create-and-clear",
            "mounted-pair-double-mount-rejected",
            "mounted-pair-invalid-pair-rejected",
            "mounted-pair-cleanup-idempotent",
            "mounted-pair-death-cleanup",
            "mounted-pair-combat-start-cleanup",
            "mounted-pair-area-unload-cleanup",
            "mounted-pair-mod-disable-cleanup",
            "mounted-pair-combat-start-retained",
            "mounted-pair-combat-end-retained",
            "mounted-pair-rider-death-cleanup",
            "mounted-pair-mount-death-cleanup",
            "mounted-pair-rider-incapacitated-cleanup",
            "mounted-pair-mount-incapacitated-cleanup",
            "mounted-pair-rider-native-incapacitated-cleanup",
            "mounted-pair-mount-native-incapacitated-cleanup",
            "mounted-pair-companion-removal-cleanup",
            "mounted-pair-view-destroyed-cleanup",
            "mounted-pair-exception-cleanup",
            "mounted-pair-slope", "mounted-pair-open-ground",
            "mounted-pair-stop-start",
            "mounted-pair-turns-and-corners",
            "mounted-pair-doorway",
            "mounted-distance-door-interaction",
            "mounted-pair-selection",
            "mounted-pair-party-formation",
            "mounted-pair-pause-unpause",
            "mounted-pair-destination-cancel",
            "mounted-pair-turn-based-entry-cleanup",
            "mounted-pair-realtime-entry-cleanup",
            "mounted-pair-save-safety",
            "mounted-pair-load-safety",
            "mounted-pair-area-transition-safety",
            "native-save-clean-dismount",
            "native-area-clean-dismount",
            "native-mode-transition-cleanup",
            "presentation-residue-and-uninstall-safety",
            "pose-idle",
            "pose-walk-run",
            "pose-turn-stop",
            "pose-doorway-formation",
            "pose-equipment-variants",
            "ui-selection-portrait-actionbar",
            "camera-follow-and-command-routing",
            "mounted-rider-melee-hit-rt",
            "mounted-rider-melee-hit-tb",
            "mounted-rider-melee-miss-rt",
            "mounted-mammoth-primary-hit-rt",
            "mounted-mammoth-primary-hit-tb",
            "mounted-rider-melee-move-to-attack-rt",
            "mounted-rider-melee-move-to-attack-tb",
            "mounted-rider-melee-command-cancel-rt",
            "mounted-rider-melee-command-cancel-tb",
            "mounted-rider-melee-command-interrupt-rt",
            "mounted-rider-melee-command-interrupt-tb",
            "mounted-rider-melee-combat-end-rt",
            "mounted-rider-melee-combat-end-tb",
            "mounted-rider-melee-human-play-path-rt",
            "mounted-rider-melee-human-play-path-tb"
        };

        public string Name { get; set; }

        public string Status { get; set; }

        public int AssertionPassCount { get; set; }

        public int AssertionFailCount { get; set; }

        public IReadOnlyList<string> Errors { get; set; }

        public static bool IsMissionScenario(string name)
        {
            return !string.IsNullOrEmpty(name) && MissionScenarios.Contains(name);
        }

        internal IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();
            if (!IsMissionScenario(Name))
            {
                errors.Add("subscenario name is outside the exact mission-row allowlist.");
            }

            if (!string.Equals(Status, "PASS", StringComparison.Ordinal) && !string.Equals(Status, "FAIL", StringComparison.Ordinal))
            {
                errors.Add("subscenario status must be PASS or FAIL.");
            }

            if (AssertionPassCount < 0 || AssertionFailCount < 0 || AssertionPassCount + AssertionFailCount == 0)
            {
                errors.Add("subscenario assertion totals must be nonnegative and nonempty.");
            }

            if (Errors == null)
            {
                errors.Add("subscenario errors is required.");
            }
            else if (string.Equals(Status, "PASS", StringComparison.Ordinal) && (AssertionFailCount != 0 || Errors.Count != 0))
            {
                errors.Add("PASS subscenario contains failures or errors.");
            }
            else if (string.Equals(Status, "FAIL", StringComparison.Ordinal) && AssertionFailCount == 0)
            {
                errors.Add("FAIL subscenario has no failed assertion.");
            }

            return errors;
        }
    }

    public sealed class RuntimeResult
    {
        public const int CurrentSchemaVersion = 1;
        public const int SaveBackedSchemaVersion = 2;

        public int SchemaVersion { get; set; }

        public string RunId { get; set; }

        public string Scenario { get; set; }

        public string Status { get; set; }

        public string Branch { get; set; }

        public string Commit { get; set; }

        public string ProductVersion { get; set; }

        public string DllSha256 { get; set; }

        public string DllMvid { get; set; }

        public string TransactionToken { get; set; }

        public string StartedAtUtc { get; set; }

        public string CompletedAtUtc { get; set; }

        public bool ModsRestored { get; set; }

        public bool SaveProtectionPassed { get; set; }

        public string GameResultSha256 { get; set; }

        public IReadOnlyList<string> Errors { get; set; }

        public RuntimeFixtureIdentity Fixture { get; set; }


        public bool BaselineImmutable { get; set; }

        public bool WorkingRestored { get; set; }

        public bool SaveWriteAllowlistPassed { get; set; }

        public string RestoredSaveInventoryDigest { get; set; }

        public int SubscenarioTotal { get; set; }

        public int SubscenarioPassCount { get; set; }

        public int SubscenarioFailCount { get; set; }

        public int AssertionPassCount { get; set; }

        public int AssertionFailCount { get; set; }

        public string EvidenceManifestSha256 { get; set; }

        public IReadOnlyList<RuntimeSubscenarioResult> SubscenarioResults { get; set; }

        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();
            if (SchemaVersion != CurrentSchemaVersion && SchemaVersion != SaveBackedSchemaVersion)
            {
                errors.Add("Unsupported result schemaVersion.");
            }

            RuntimeRequest.Require(errors, RunId, "runId");
            RuntimeRequest.Require(errors, Scenario, "scenario");
            RuntimeRequest.Require(errors, Status, "status");
            RuntimeRequest.Require(errors, Branch, "branch");
            RuntimeRequest.Require(errors, Commit, "commit");
            RuntimeRequest.Require(errors, ProductVersion, "productVersion");
            RuntimeRequest.RequireSha256(errors, DllSha256, "dllSha256");
            RuntimeRequest.RequireGuid(errors, DllMvid, "dllMvid");
            RuntimeRequest.RequireSha256(errors, TransactionToken, "transactionToken");

            DateTimeOffset started;
            DateTimeOffset completed;
            if (!DateTimeOffset.TryParse(StartedAtUtc, out started))
            {
                errors.Add("startedAtUtc must be an ISO-8601 timestamp.");
            }

            if (!DateTimeOffset.TryParse(CompletedAtUtc, out completed))
            {
                errors.Add("completedAtUtc must be an ISO-8601 timestamp.");
            }
            else if (DateTimeOffset.TryParse(StartedAtUtc, out started) && completed < started)
            {
                errors.Add("completedAtUtc precedes startedAtUtc.");
            }

            if (!string.Equals(Status, "PASS", StringComparison.Ordinal) && !string.Equals(Status, "FAIL", StringComparison.Ordinal))
            {
                errors.Add("status must be PASS or FAIL.");
            }

            if (Errors == null)
            {
                errors.Add("errors is required.");
            }

            if (SchemaVersion == SaveBackedSchemaVersion)
            {
                ValidateSaveBackedResult(errors);
            }
            else if (SchemaVersion == CurrentSchemaVersion && Fixture != null)
            {
                errors.Add("Schema-v1 result must not contain fixture identity.");
            }

            if (string.Equals(Status, "PASS", StringComparison.Ordinal))
            {
                RuntimeRequest.RequireSha256(errors, GameResultSha256, "gameResultSha256");
                if (!ModsRestored || !SaveProtectionPassed || (Errors != null && Errors.Count != 0))
                {
                    errors.Add("PASS requires restored Mods, protected saves, and no errors.");
                }
            }

            return errors;
        }

        private void ValidateSaveBackedResult(List<string> errors)
        {
            if (!RuntimeRequest.IsSaveBackedScenario(Scenario))
            {
                errors.Add("scenario is outside the exact save-backed mission allowlist.");
            }

            if (Fixture == null)
            {
                errors.Add("fixture is required for schema-v2 results.");
            }
            else
            {
                errors.AddRange(Fixture.Validate());
            }

            if (!RuntimeRequest.IsLowerHex(RestoredSaveInventoryDigest, 64))
            {
                errors.Add("restoredSaveInventoryDigest must be an exact SHA-256.");
            }

            RuntimeRequest.RequireSha256(errors, EvidenceManifestSha256, "evidenceManifestSha256");

            ValidateSubscenarioTotals(errors, Scenario, SubscenarioResults, SubscenarioTotal, SubscenarioPassCount,
                SubscenarioFailCount, AssertionPassCount, AssertionFailCount);

            if (string.Equals(Status, "PASS", StringComparison.Ordinal) &&
                (!BaselineImmutable || !WorkingRestored || !SaveWriteAllowlistPassed ||
                 SubscenarioFailCount != 0 || AssertionFailCount != 0))
            {
                errors.Add("Schema-v2 PASS requires immutable Baseline, restored Working, the write allowlist, and no subscenario failures.");
            }

            if (SaveProtectionPassed != (BaselineImmutable && WorkingRestored && SaveWriteAllowlistPassed))
            {
                errors.Add("saveProtectionPassed does not equal its three exact fixture safety proofs.");
            }
        }

        internal static void ValidateSubscenarioTotals(
            List<string> errors,
            string scenario,
            IReadOnlyList<RuntimeSubscenarioResult> results,
            int total,
            int passCount,
            int failCount,
            int assertionPassCount,
            int assertionFailCount)
        {
            if (results == null)
            {
                errors.Add("subscenarioResults is required.");
                return;
            }

            var names = new HashSet<string>(StringComparer.Ordinal);
            var observedPass = 0;
            var observedFail = 0;
            var observedAssertionPass = 0;
            var observedAssertionFail = 0;
            for (var index = 0; index < results.Count; index++)
            {
                var result = results[index];
                if (result == null)
                {
                    errors.Add("subscenarioResults contains null.");
                    continue;
                }

                errors.AddRange(result.Validate());
                if (!names.Add(result.Name ?? string.Empty))
                {
                    errors.Add("subscenarioResults contains a duplicate name.");
                }

                if (string.Equals(result.Status, "PASS", StringComparison.Ordinal))
                {
                    observedPass++;
                }
                else if (string.Equals(result.Status, "FAIL", StringComparison.Ordinal))
                {
                    observedFail++;
                }

                observedAssertionPass += result.AssertionPassCount;
                observedAssertionFail += result.AssertionFailCount;
            }

            if (results.Count == 0 || total != results.Count || passCount != observedPass || failCount != observedFail ||
                passCount + failCount != total || assertionPassCount != observedAssertionPass ||
                assertionFailCount != observedAssertionFail)
            {
                errors.Add("subscenario and assertion totals do not match the named results.");
            }

            if (scenario != "chunk7-owned-two-pair-campaign" && scenario != "chunk7-cumulative-acquisition" && scenario != "chunk7-acquired-manual-derivative" && scenario != "chunk7-manual-derived-acquisition" && scenario != "chunk7-bootstrap-manual-seed" && scenario != "chunk7-bootstrap-cold-inventory" && scenario != "chunk7-native-progression" && scenario != "chunk7-native-main-bootstrap" && scenario != "chunk7-assisted-fixture-preparation" && scenario != "chunk7-assisted-cold-inventory" && scenario != "chunk7-small-profile-calibration" && scenario != "chunk7-ordinary-field-admission" && scenario != "chunk7-ordinary-acquisition" && scenario != "chunk7-two-pair-exploration" && !RuntimeRequest.IsChunk7PersistenceScenario(scenario) && !RuntimeRequest.IsChunk7CombatScenario(scenario) && RuntimeSubscenarioResult.IsMissionScenario(scenario) && !names.Contains(scenario))
            {
                errors.Add("An individual mission scenario must report its own named subscenario result.");
            }
        }
    }
}
