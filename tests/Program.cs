using System;
using KingmakerMountedCombat.Diagnostics;

namespace KingmakerMountedCombat.Tests
{
    internal static class Program
    {
        private const string Sha = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
        private const string Mvid = "07fa1e4d-8618-41b3-9b8d-faa17d3b26f7";

        private static int Main()
        {
            var runner = new TestRunner();
            runner.Run("diagnostic settings defaults are safe", DiagnosticSettingsDefaultsAreSafe);
            runner.Run("request accepts no-save smoke", RequestAcceptsNoSaveSmoke);
            runner.Run("request rejects valued save", RequestRejectsValuedSave);
            runner.Run("request rejects save name in no-save mode", RequestRejectsSaveNameInNoSaveMode);
            runner.Run("request requires exact hash and MVID formats", RequestRequiresBuildIdentity);
            runner.Run("request accepts exact save-backed fixture", RequestAcceptsExactSaveBackedFixture);
            runner.Run("request rejects mismatched fixture identity", RequestRejectsMismatchedFixtureIdentity);
            runner.Run("request rejects non-Working write authorization", RequestRejectsNonWorkingAuthorization);
            runner.Run("result accepts complete PASS", ResultAcceptsCompletePass);
            runner.Run("result rejects non-terminal status", ResultRejectsNonTerminalStatus);
            runner.Run("result accepts restored save-backed PASS", ResultAcceptsRestoredSaveBackedPass);
            runner.Run("result rejects incomplete fixture restoration", ResultRejectsIncompleteFixtureRestoration);
            runner.Run("result rejects inconsistent subscenario totals", ResultRejectsInconsistentSubscenarioTotals);
            runner.Run("result requires lowercase evidence manifest SHA-256", ResultRequiresEvidenceManifestSha256);
            MountedRelationshipTests.Register(runner);
            RuntimeSaveAuthorizationTests.Register(runner);
            WorkingFixtureLoadWatchdogTests.Register(runner);
            return runner.Complete();
        }

        private static void DiagnosticSettingsDefaultsAreSafe()
        {
            var settings = new DiagnosticSettings();
            TestRunner.Equal(false, settings.EnableUnsafeMovementExperiment, "Unsafe movement experiment must default off.");
            TestRunner.Equal(0.10d, settings.MaximumAnchorResidualWorldUnits, "Residual threshold changed.");
            TestRunner.Equal(null, settings.Validate(), "Default settings must validate.");
        }

        private static void RequestAcceptsNoSaveSmoke()
        {
            var request = ValidRequest();
            TestRunner.Equal(0, request.Validate().Count, "Valid no-save request was rejected.");
        }

        private static void RequestRejectsValuedSave()
        {
            var request = ValidRequest();
            request.SaveAccessAllowed = true;
            request.SaveName = "VALUED_CAMPAIGN";
            TestRunner.True(request.Validate().Count > 0, "Valued save was accepted.");
        }

        private static void RequestRejectsSaveNameInNoSaveMode()
        {
            var request = ValidRequest();
            request.SaveName = "KMC_AUTOMATION_WORKING";
            TestRunner.True(request.Validate().Count > 0, "No-save request accepted a save name.");
        }

        private static void RequestRequiresBuildIdentity()
        {
            var request = ValidRequest();
            request.DllSha256 = "bad";
            request.DllMvid = "bad";
            TestRunner.True(request.Validate().Count >= 2, "Invalid build identity was accepted.");
        }

        private static void ResultAcceptsCompletePass()
        {
            var result = ValidResult();
            TestRunner.Equal(0, result.Validate().Count, "Valid runtime result was rejected.");
        }

        private static void ResultRejectsNonTerminalStatus()
        {
            var result = ValidResult();
            result.Status = "IN PROGRESS";
            TestRunner.True(result.Validate().Count > 0, "Non-terminal result status was accepted.");
        }

        private static void RequestAcceptsExactSaveBackedFixture()
        {
            var request = ValidSaveBackedRequest();
            TestRunner.Equal(0, request.Validate().Count, "Valid save-backed fixture request was rejected.");
        }

        private static void RequestRejectsMismatchedFixtureIdentity()
        {
            var request = ValidSaveBackedRequest();
            request.Fixture.Working.Area = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            TestRunner.True(request.Validate().Count > 0, "Mismatched fixture campaign identity was accepted.");
        }

        private static void RequestRejectsNonWorkingAuthorization()
        {
            var request = ValidSaveBackedRequest();
            request.Fixture.WriteAuthorization.AllowedInternalName = "KMC_AUTOMATION_BASELINE";
            TestRunner.True(request.Validate().Count > 0, "Baseline write authorization was accepted.");
        }

        private static void ResultAcceptsRestoredSaveBackedPass()
        {
            var result = ValidSaveBackedResult();
            TestRunner.Equal(0, result.Validate().Count, "Valid restored save-backed result was rejected.");
        }

        private static void ResultRejectsIncompleteFixtureRestoration()
        {
            var result = ValidSaveBackedResult();
            result.WorkingRestored = false;
            TestRunner.True(result.Validate().Count > 0, "Incomplete Working restoration was accepted.");
        }

        private static void ResultRejectsInconsistentSubscenarioTotals()
        {
            var result = ValidSaveBackedResult();
            result.AssertionPassCount++;
            TestRunner.True(result.Validate().Count > 0, "Inconsistent subscenario totals were accepted.");
        }

        private static void ResultRequiresEvidenceManifestSha256()
        {
            var result = ValidSaveBackedResult();
            result.EvidenceManifestSha256 = null;
            TestRunner.True(result.Validate().Count > 0, "Missing evidence manifest SHA-256 was accepted.");

            result.EvidenceManifestSha256 = Sha.ToUpperInvariant();
            TestRunner.True(result.Validate().Count > 0, "Uppercase evidence manifest SHA-256 was accepted.");
        }

        private static RuntimeRequest ValidRequest()
        {
            return new RuntimeRequest
            {
                SchemaVersion = RuntimeRequest.CurrentSchemaVersion,
                RunId = "kmc-smoke-001",
                Scenario = "mod-load-smoke",
                Branch = "codex/mounted-combat-feasibility",
                Commit = "3801345720241eeab75f2944d91948f182ca26aa",
                ProductVersion = "0.0.1-feasibility",
                DllSha256 = Sha,
                DllMvid = Mvid,
                EvidenceRoot = "runtime-evidence/kmc-smoke-001",
                TransactionToken = Sha,
                SaveAccessAllowed = false,
                SaveName = null
            };
        }

        private static RuntimeResult ValidResult()
        {
            return new RuntimeResult
            {
                SchemaVersion = RuntimeResult.CurrentSchemaVersion,
                RunId = "kmc-smoke-001",
                Scenario = "mod-load-smoke",
                Status = "PASS",
                Branch = "codex/mounted-combat-feasibility",
                Commit = "3801345720241eeab75f2944d91948f182ca26aa",
                ProductVersion = "0.0.1-feasibility",
                DllSha256 = Sha,
                DllMvid = Mvid,
                TransactionToken = Sha,
                StartedAtUtc = "2026-08-13T16:00:00Z",
                CompletedAtUtc = "2026-08-13T16:00:01Z",
                ModsRestored = true,
                SaveProtectionPassed = true,
                GameResultSha256 = Sha,
                Errors = new string[0]
            };
        }

        private static RuntimeRequest ValidSaveBackedRequest()
        {
            return new RuntimeRequest
            {
                SchemaVersion = RuntimeRequest.SaveBackedSchemaVersion,
                RunId = "kmc-fixture-001",
                Scenario = "fixture-intake",
                Branch = "codex/mounted-combat-feasibility",
                Commit = "3801345720241eeab75f2944d91948f182ca26aa",
                ProductVersion = "0.0.1-feasibility",
                DllSha256 = Sha,
                DllMvid = Mvid,
                EvidenceRoot = "runtime-evidence/kmc-fixture-001",
                TransactionToken = Sha,
                Fixture = ValidFixture()
            };
        }

        private static RuntimeResult ValidSaveBackedResult()
        {
            return new RuntimeResult
            {
                SchemaVersion = RuntimeResult.SaveBackedSchemaVersion,
                RunId = "kmc-fixture-001",
                Scenario = "fixture-intake",
                Status = "PASS",
                Branch = "codex/mounted-combat-feasibility",
                Commit = "3801345720241eeab75f2944d91948f182ca26aa",
                ProductVersion = "0.0.1-feasibility",
                DllSha256 = Sha,
                DllMvid = Mvid,
                TransactionToken = Sha,
                StartedAtUtc = "2026-08-13T16:00:00Z",
                CompletedAtUtc = "2026-08-13T16:00:01Z",
                ModsRestored = true,
                SaveProtectionPassed = true,
                GameResultSha256 = Sha,
                Errors = new string[0],
                Fixture = ValidFixture(),
                BaselineImmutable = true,
                WorkingRestored = true,
                SaveWriteAllowlistPassed = true,
                RestoredSaveInventoryDigest = Sha,
                SubscenarioTotal = 1,
                SubscenarioPassCount = 1,
                SubscenarioFailCount = 0,
                AssertionPassCount = 3,
                AssertionFailCount = 0,
                EvidenceManifestSha256 = Sha,
                SubscenarioResults = new[]
                {
                    new RuntimeSubscenarioResult
                    {
                        Name = "observe-mount-diagnostic-availability",
                        Status = "PASS",
                        AssertionPassCount = 3,
                        AssertionFailCount = 0,
                        Errors = new string[0]
                    }
                }
            };
        }

        private static RuntimeFixtureIdentity ValidFixture()
        {
            return new RuntimeFixtureIdentity
            {
                Baseline = new RuntimeSaveDescriptor
                {
                    InternalName = "KMC_AUTOMATION_BASELINE",
                    FileName = "Manual_298_KMC_AUTOMATION_BASELINE.zks",
                    Sha256 = Sha,
                    Length = 686605,
                    LastWriteTimeUtcTicks = 638907120000000000L,
                    GameId = Mvid,
                    GameName = "KMC Fixture",
                    Area = "0123456789abcdef0123456789abcdef"
                },
                Working = new RuntimeSaveDescriptor
                {
                    InternalName = "KMC_AUTOMATION_WORKING",
                    FileName = "Manual_299_KMC_AUTOMATION_WORKING.zks",
                    Sha256 = "abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789",
                    Length = 684085,
                    LastWriteTimeUtcTicks = 638907120010000000L,
                    GameId = Mvid,
                    GameName = "KMC Fixture",
                    Area = "0123456789abcdef0123456789abcdef"
                },
                WriteAuthorization = new RuntimeSaveWriteAuthorization
                {
                    Mode = "working-only",
                    AllowedInternalName = "KMC_AUTOMATION_WORKING",
                    AllowedFileName = "Manual_299_KMC_AUTOMATION_WORKING.zks",
                    BaselineImmutable = true
                }
            };
        }
    }
}
