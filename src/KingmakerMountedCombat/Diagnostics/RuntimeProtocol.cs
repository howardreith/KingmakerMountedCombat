using System;
using System.Collections.Generic;
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
            "export-mounted-contracts",
            "export-candidate-mount-rigs",
            "observe-mount-diagnostic-availability",
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
            "mounted-pair-companion-removal-cleanup",
            "mounted-pair-view-destroyed-cleanup",
            "mounted-pair-exception-cleanup",
            "mounted-pair-open-ground",
            "mounted-pair-stop-start",
            "mounted-pair-turns-and-corners",
            "mounted-pair-doorway",
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
            "lifecycle-suite",
            "combat-lifecycle-suite",
            "movement-suite",
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

        public RuntimeQualificationSuiteIdentity QualificationSuite { get; set; }

        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();
            if (SchemaVersion != CurrentSchemaVersion && SchemaVersion != SaveBackedSchemaVersion)
            {
                errors.Add("Unsupported request schemaVersion.");
            }

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

            if (!string.Equals(ProductVersion, "0.1.0-phase2b-dev.1", StringComparison.Ordinal))
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
            "mod-load-smoke",
            "export-mounted-contracts",
            "export-candidate-mount-rigs",
            "observe-mount-diagnostic-availability",
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
            "mounted-pair-companion-removal-cleanup",
            "mounted-pair-view-destroyed-cleanup",
            "mounted-pair-exception-cleanup",
            "mounted-pair-open-ground",
            "mounted-pair-stop-start",
            "mounted-pair-turns-and-corners",
            "mounted-pair-doorway",
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
            "mounted-rider-melee-combat-end-tb"
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

            if (RuntimeSubscenarioResult.IsMissionScenario(scenario) && !names.Contains(scenario))
            {
                errors.Add("An individual mission scenario must report its own named subscenario result.");
            }
        }
    }
}
