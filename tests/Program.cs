using System;
using KingmakerMountedCombat.Diagnostics;

namespace KingmakerMountedCombat.Tests
{
    internal static class Program
    {
        private const string Sha = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
        private const string Mvid = "07fa1e4d-8618-41b3-9b8d-faa17d3b26f7";
        private const string ProductVersion = BuildIdentity.ProductVersion;

        private static int Main()
        {
            var runner = new TestRunner();
            runner.Run("diagnostic settings defaults are safe", DiagnosticSettingsDefaultsAreSafe);
            runner.Run("request accepts no-save smoke", RequestAcceptsNoSaveSmoke);
            runner.Run("request rejects valued save", RequestRejectsValuedSave);
            runner.Run("request rejects save name in no-save mode", RequestRejectsSaveNameInNoSaveMode);
            runner.Run("request requires exact hash and MVID formats", RequestRequiresBuildIdentity);
            runner.Run("request accepts exact save-backed fixture", RequestAcceptsExactSaveBackedFixture);
            runner.Run("request accepts combat core control suite", RequestAcceptsCombatCoreControlSuite);
            runner.Run("request accepts observation-only horse native asset audit", RequestAcceptsHorseNativeAssetAudit);
            runner.Run("request accepts horse companion blueprint registration audit", RequestAcceptsHorseCompanionBlueprintRegistration);
            runner.Run("request accepts horse companion unmounted suite", RequestAcceptsHorseCompanionUnmountedSuite);
            runner.Run("request accepts horse mounted alpha suite", RequestAcceptsHorseMountedAlphaSuite);
            runner.Run("request accepts horse native-controls UX suite", RequestAcceptsHorseNativeControlsUxSuite);
            runner.Run("request accepts Phase 3D Horse suites", RequestAcceptsPhase3dHorseSuites);
            runner.Run("request accepts private-alpha human-play combat rows", RequestAcceptsHumanPlayCombatRows);
            runner.Run("request requires exact qualification-suite identity", RequestRequiresQualificationSuiteIdentity);
            runner.Run("request accepts read-only manual visual review", RequestAcceptsReadOnlyManualReview);
            runner.Run("request rejects writable manual visual review", RequestRejectsWritableManualReview);
            runner.Run("request rejects read-only automated scenario", RequestRejectsReadOnlyAutomatedScenario);
            runner.Run("request rejects mismatched fixture identity", RequestRejectsMismatchedFixtureIdentity);
            runner.Run("request rejects non-Working write authorization", RequestRejectsNonWorkingAuthorization);
            runner.Run("result accepts complete PASS", ResultAcceptsCompletePass);
            runner.Run("result rejects non-terminal status", ResultRejectsNonTerminalStatus);
            runner.Run("result accepts restored save-backed PASS", ResultAcceptsRestoredSaveBackedPass);
            runner.Run("result rejects incomplete fixture restoration", ResultRejectsIncompleteFixtureRestoration);
            runner.Run("result rejects inconsistent subscenario totals", ResultRejectsInconsistentSubscenarioTotals);
            runner.Run("result requires lowercase evidence manifest SHA-256", ResultRequiresEvidenceManifestSha256);
            MountedRelationshipTests.Register(runner);
            MountedPlayerActionTests.Register(runner);
            MountedCombatDomainTests.Register(runner);
            ManualReviewBoundaryGuardTests.Register(runner);
            RuntimeSaveAuthorizationTests.Register(runner);
            WorkingFixtureLoadWatchdogTests.Register(runner);
            BoundaryFailureDrainTests.Register(runner);
            BoundaryScenarioEvidenceContractTests.Register(runner);
            MovementScreenshotCaptureTests.Register(runner);
            MovementNavigationBoundaryPolicyTests.Register(runner);
            MovementRadialDistanceOrderTests.Register(runner);
            NavigationEndpointDistanceTrackerTests.Register(runner);
            NativeLifecycleDeliveryLedgerTests.Register(runner);
            NativeMountedAbilityActivationLedgerTests.Register(runner);
            NativeAreaBoundaryProgressTests.Register(runner);
            MountedRiderPoseTests.Register(runner);
            MountedRiderGroundingPolicyTests.Register(runner);
            MountedStabilizationPolicyTests.Register(runner);
            NativeMountedControlPolicyTests.Register(runner);
            UnifiedMountedTurnPolicyTests.Register(runner);
            MountedStockAttackPolicyTests.Register(runner);
            ReactiveBooleanValueReaderTests.Register(runner);
            StopEarlyCaptureBoundaryTests.Register(runner);
            PresentationOverlayEvidenceTests.Register(runner);
            ScopedDiagnosticAiLeaseTests.Register(runner);
            ExpectedAttackDispatchLedgerTests.Register(runner);
            ExactAppendOnlyArrayLeaseTests.Register(runner);
            HorseCompanionLifeTransitionPolicyTests.Register(runner);
            HorseCompanionProgressionPolicyTests.Register(runner);
            HorseCompanionScenarioDeadlinePolicyTests.Register(runner);
            return runner.Complete();
        }

        private static void DiagnosticSettingsDefaultsAreSafe()
        {
            var settings = new DiagnosticSettings();
            TestRunner.Equal(true, settings.EnableUnsafeMovementExperiment, "Native mounted controls must default on for the enabled private alpha.");
            TestRunner.Equal(false, settings.EnableDiagnosticOverlay, "The legacy diagnostic overlay must default hidden.");
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

        private static void RequestAcceptsCombatCoreControlSuite()
        {
            var request = ValidSaveBackedRequest();
            request.Scenario = "combat-core-control-suite";
            TestRunner.Equal(0, request.Validate().Count, "Combat core control suite request was rejected.");
        }

        private static void RequestAcceptsHorseNativeAssetAudit()
        {
            var request = ValidSaveBackedRequest();
            request.Scenario = "horse-native-asset-audit";
            TestRunner.Equal(0, request.Validate().Count, "Horse native-asset audit request was rejected.");
        }

        private static void RequestAcceptsHorseCompanionBlueprintRegistration()
        {
            var request = ValidSaveBackedRequest();
            request.Scenario = "horse-companion-blueprint-registration";
            TestRunner.Equal(0, request.Validate().Count, "Horse companion blueprint registration request was rejected.");
        }

        private static void RequestAcceptsHorseCompanionUnmountedSuite()
        {
            var request = ValidSaveBackedRequest();
            request.Scenario = "horse-companion-unmounted-suite";
            TestRunner.Equal(0, request.Validate().Count, "Horse companion unmounted suite request was rejected.");
        }

        private static void RequestAcceptsHorseMountedAlphaSuite()
        {
            var request = ValidSaveBackedRequest();
            request.Scenario = "horse-mounted-alpha-suite";
            TestRunner.Equal(0, request.Validate().Count, "Horse mounted alpha suite request was rejected.");
        }

        private static void RequestAcceptsHorseNativeControlsUxSuite()
        {
            var request = ValidSaveBackedRequest();
            request.Scenario = "horse-native-controls-ux-suite";
            TestRunner.Equal(0, request.Validate().Count, "Horse native-controls UX suite request was rejected.");
        }

        private static void RequestAcceptsPhase3dHorseSuites()
        {
            foreach (var scenario in new[]
            {
                "phase3d-unified-combat-rt-suite",
                "phase3d-unified-combat-tb-suite",
                "phase3d-horse-presentation-suite"
            })
            {
                var request = ValidSaveBackedRequest();
                request.Scenario = scenario;
                TestRunner.Equal(0, request.Validate().Count, scenario + " request was rejected.");
            }
        }

        private static void RequestAcceptsHumanPlayCombatRows()
        {
            foreach (var scenario in new[]
            {
                "mounted-rider-melee-human-play-path-rt",
                "mounted-rider-melee-human-play-path-tb"
            })
            {
                var request = ValidSaveBackedRequest();
                request.Scenario = scenario;
                TestRunner.Equal(0, request.Validate().Count, scenario + " request was rejected.");
            }
        }

        private static void RequestRequiresQualificationSuiteIdentity()
        {
            var request = ValidSaveBackedRequest();
            request.QualificationSuite = null;
            TestRunner.True(request.Validate().Count > 0, "Missing qualification-suite identity was accepted.");

            request = ValidSaveBackedRequest();
            request.QualificationSuite.SuiteId = "bad suite";
            request.QualificationSuite.SnapshotSha256 = Sha.ToUpperInvariant();
            TestRunner.True(request.Validate().Count >= 2, "Malformed qualification-suite identity was accepted.");
        }

        private static void RequestAcceptsReadOnlyManualReview()
        {
            var request = ValidReadOnlyManualReviewRequest();
            TestRunner.Equal(0, request.Validate().Count, "Valid read-only manual review request was rejected.");
        }

        private static void RequestRejectsWritableManualReview()
        {
            var request = ValidSaveBackedRequest();
            request.Scenario = RuntimeRequest.ManualReviewScenario;
            TestRunner.True(request.Validate().Count > 0, "Manual review accepted Working write authorization.");
        }

        private static void RequestRejectsReadOnlyAutomatedScenario()
        {
            var request = ValidReadOnlyManualReviewRequest();
            request.Scenario = "presentation-suite";
            TestRunner.True(request.Validate().Count > 0, "Automated presentation suite accepted read-only authorization identity.");
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
                ProductVersion = ProductVersion,
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
                ProductVersion = ProductVersion,
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
                ProductVersion = ProductVersion,
                DllSha256 = Sha,
                DllMvid = Mvid,
                EvidenceRoot = "runtime-evidence/kmc-fixture-001",
                TransactionToken = Sha,
                QualificationSuite = new RuntimeQualificationSuiteIdentity
                {
                    SuiteId = "suite-001",
                    SnapshotSha256 = Sha
                },
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
                ProductVersion = ProductVersion,
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

        private static RuntimeRequest ValidReadOnlyManualReviewRequest()
        {
            var request = ValidSaveBackedRequest();
            request.Scenario = RuntimeRequest.ManualReviewScenario;
            request.Fixture.WriteAuthorization.Mode = "read-only";
            request.Fixture.WriteAuthorization.AllowedInternalName = null;
            request.Fixture.WriteAuthorization.AllowedFileName = null;
            return request;
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
