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
            runner.Run("registration audit accepts exact Horse parent scenarios", RegistrationAuditAcceptsExactHorseParentScenarios);
            foreach (var scenario in new[] { "chunk4-targeting-area-unmounted-rt", "chunk4-obstruction-ranged-rt" })
            {
                var exactScenario = scenario;
                runner.Run("Chunk 4 focused registration: " + exactScenario, () =>
                {
                    var request = ValidSaveBackedRequest(); request.Scenario = exactScenario;
                    TestRunner.Equal(0, request.Validate().Count, "Focused request rejected: " + exactScenario);
                    TestRunner.True(HorseCompanionRegistrationScenarioPolicy.SupportsScenario(exactScenario),
                        "Focused Horse registration missing: " + exactScenario);
                });
            }
            runner.Run("Chunk 4 Charge envelopes retain every native row", () =>
            {
                foreach (var scenario in new[] { "chunk4-charge-safety-rt", "chunk4-charge-safety-tb" })
                {
                    var request = ValidSaveBackedRequest();
                    request.Scenario = scenario;
                    TestRunner.Equal(0, request.Validate().Count, "Charge request rejected.");
                    TestRunner.True(HorseCompanionRegistrationScenarioPolicy.SupportsScenario(scenario), "Charge registration rejected.");
                }
                foreach (var name in new[] { "C4-CHARGE-mounted-rider", "C4-CHARGE-unmounted-rider", "C4-CHARGE-mounted-mount", "C4-CHARGE-unrelated-actor", "C4-CHARGE-queued-state-change" })
                {
                    var result = new RuntimeSubscenarioResult { Name = name, Status = "PASS", AssertionPassCount = 1, Errors = new string[0] };
                    TestRunner.Equal(0, result.Validate().Count, "Native Charge result was lost at serialization: " + name);
                }
            });
            runner.Run("Chunk 4 sustained native envelopes retain each parameterized case", () =>
            {
                foreach (var scenario in new[] { "chunk4-sustained-melee-rt", "chunk4-sustained-ranged-rt", "chunk4-sustained-tb" })
                {
                    var request = ValidSaveBackedRequest(); request.Scenario = scenario;
                    TestRunner.Equal(0, request.Validate().Count, "Sustained request rejected.");
                    TestRunner.True(HorseCompanionRegistrationScenarioPolicy.SupportsScenario(scenario), "Sustained Horse registration rejected.");
                }
                foreach (var weapon in new[] { "melee", "ranged" })
                    foreach (var input in new[] { "adjacent-held", "adjacent-repeat", "approach-held", "approach-repeat" })
                    {
                        var result = new RuntimeSubscenarioResult { Name = "C4-SUSTAINED-" + weapon + "-" + input,
                            Status = "PASS", AssertionPassCount = 1, Errors = new string[0] };
                        TestRunner.Equal(0, result.Validate().Count, "Sustained RT native leaf was lost at serialization.");
                    }
                foreach (var input in new[] { "rider-first", "mount-first", "rider-exhausted", "mount-exhausted", "early-end", "after-early-end" })
                {
                    var result = new RuntimeSubscenarioResult { Name = "C4-SUSTAINED-TB-" + input,
                        Status = "PASS", AssertionPassCount = 1, Errors = new string[0] };
                    TestRunner.Equal(0, result.Validate().Count, "Sustained TB native leaf was lost at serialization.");
                }
            });
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
            ActorAllocationLifetimeTests.Register(runner);
            PairedActivationTests.Register(runner);
            Chunk6aCombatMountTests.Register(runner);
            ManualReviewBoundaryGuardTests.Register(runner);
            runner.Run("persistence isolation accepts only the qualified disposable fixture", () =>
            {
                var request = ValidSaveBackedRequest();
                request.Scenario = "persistence-isolation";
                TestRunner.Equal(0, request.Validate().Count, "Isolated bootstrap request rejected.");
                request.Fixture.Working.InternalName = "KMC_arbitrary";
                TestRunner.True(request.Validate().Count > 0, "Display prefix authorized an arbitrary fixture.");
            });
            runner.Run("P02 accepts only bounded checkpoint parameters", () =>
            {
                foreach (var name in new[] { "partial-movement", "rider-spent", "between-partner-orders", "exhausted", "explicit-end" })
                {
                    var request = ValidSaveBackedRequest(); request.Scenario = "persistence-p02-save"; request.PersistenceCase = name;
                    TestRunner.Equal(0, request.Validate().Count, "P02 checkpoint rejected.");
                    request.Scenario = "mounted-pair-create-and-clear";
                    TestRunner.True(request.Validate().Count > 0, "Checkpoint leaked to an unrelated scenario.");
                }
                var invalid = ValidSaveBackedRequest(); invalid.Scenario = "persistence-p02-save"; invalid.PersistenceCase = "../human";
                TestRunner.True(invalid.Validate().Count > 0, "Unrecognized checkpoint accepted.");
                invalid.PersistenceCase = "";
                TestRunner.True(invalid.Validate().Count > 0, "Empty checkpoint accepted.");
                invalid.PersistenceCase = "rider-spent"; invalid.Fixture = null;
                TestRunner.True(invalid.Validate().Count > 0, "Checkpoint bypassed fixture authority.");
            });
            runner.Run("P03 preserves disjoint native commitment and save authority", () =>
            {
                foreach (var name in new[] { "step", "conversion", "round-effect", "reaction", "condition", "condition-preparing", "suspended" })
                {
                    var request = ValidSaveBackedRequest(); request.Scenario = "persistence-p03-save";
                    request.PersistenceCase = name;
                    TestRunner.Equal(0, request.Validate().Count, "P03 commitment rejected.");
                    request.Scenario = "persistence-p02-save";
                    TestRunner.True(request.Validate().Count > 0, "P03 case leaked into P02.");
                    request.Scenario = "persistence-p03-load";
                    TestRunner.True(request.Validate().Count > 0, "Cold P03 bypassed its archive identity.");
                }
                var invalid = ValidSaveBackedRequest(); invalid.Scenario = "persistence-p03-save";
                TestRunner.True(invalid.Validate().Count > 0, "P03 inferred an undeclared commitment.");
                invalid.PersistenceCase = "partial-movement";
                TestRunner.True(invalid.Validate().Count > 0, "P02 case leaked into P03.");
                invalid.PersistenceCase = "step"; invalid.Fixture = null;
                TestRunner.True(invalid.Validate().Count > 0, "P03 bypassed fixture authority.");
            });
            runner.Run("P04 requires a declared RT boundary and actual cold archive", () =>
            {
                foreach (var name in new[] { "unmounted-spent", "mounted-spent", "unmounted-attack", "mounted-attack", "unmounted-projectile", "mounted-projectile", "unmounted-approach", "mounted-approach", "unmounted-casting", "mounted-casting" })
                {
                    var request = ValidSaveBackedRequest(); var fixture = request.Fixture.Working;
                    request.Scenario = "persistence-p04-save"; request.PersistenceCase = name;
                    TestRunner.Equal(0, request.Validate().Count, "Exact RT source rejected.");
                    request.Scenario = "persistence-p04-load";
                    TestRunner.True(request.Validate().Count > 0, "RT cold load accepted missing archive.");
                    request.PersistenceLoad = new RuntimeSaveDescriptor {
                        InternalName = "KMC_P01", FileName = "Manual_300_KMC_P01.zks",
                        GameId = fixture.GameId, GameName = fixture.GameName, Area = fixture.Area,
                        Sha256 = new string('c', 64), Length = 1024, LastWriteTimeUtcTicks = fixture.LastWriteTimeUtcTicks };
                    TestRunner.Equal(0, request.Validate().Count, "Exact RT cold archive rejected.");
                    request.PersistenceLoad.GameId = "00000000-0000-0000-0000-000000000001";
                    TestRunner.True(request.Validate().Count > 0, "RT admitted a foreign campaign.");
                    request.PersistenceLoad.GameId = fixture.GameId;
                    request.Scenario = "persistence-p03-load";
                    TestRunner.True(request.Validate().Count > 0, "RT case escaped into TB qualification.");
                }
            });
            runner.Run("P05 requires exact native categories and isolated cold identity", () =>
            {
                foreach (var name in new[] { "manual", "quick", "auto" })
                {
                    var request = ValidSaveBackedRequest(); request.Scenario = "persistence-p05-save";
                    request.PersistenceCase = name;
                    TestRunner.Equal(0, request.Validate().Count, "P05 category rejected.");
                    request.Scenario = "persistence-p03-save";
                    TestRunner.True(request.Validate().Count > 0, "P05 category leaked into combat fixture.");
                    request.Scenario = "persistence-p05-load";
                    TestRunner.True(request.Validate().Count > 0, "P05 cold bypassed archive identity.");
                }
                var invalid = ValidSaveBackedRequest(); invalid.Scenario = "persistence-p05-save";
                TestRunner.True(invalid.Validate().Count > 0, "P05 inferred an undeclared category.");
                invalid.PersistenceCase = "reaction";
                TestRunner.True(invalid.Validate().Count > 0, "P03 checkpoint leaked into P05.");
            });
            runner.Run("cold category and renamed archive cannot broaden Working descriptor types", () =>
            {
                foreach (var name in new[] { "manual", "quick", "auto", "manual-renamed", "queued" })
                {
                    var request = ValidSaveBackedRequest(); var fixture = request.Fixture.Working;
                    request.Scenario = "persistence-p05-load"; request.PersistenceCase = name;
                    request.PersistenceLoad = new RuntimeSaveDescriptor {
                        InternalName = name == "quick" || name == "auto" ? "Native slot 1" : "KMC_P01",
                        FileName = name == "quick" ? "Quick_1.zks" : name == "auto" ? "Auto_1.zks" :
                            name == "manual-renamed" ? "Manual_811_KMC_RENAMED.zks" : name == "queued" ? "Manual_302_KMC_P01.zks" : "Manual_300_KMC_P01.zks",
                        GameId = fixture.GameId, GameName = fixture.GameName, Area = fixture.Area,
                        Sha256 = new string('c', 64), Length = 1024, LastWriteTimeUtcTicks = fixture.LastWriteTimeUtcTicks };
                    TestRunner.Equal(0, request.Validate().Count, "Exact native cold category rejected.");
                    TestRunner.Equal(name == "quick" ? "Quick" : name == "auto" ? "Auto" : "Manual",
                        request.ExpectedNativeLoadType, "Declared native category lost before loader admission.");
                    request.PersistenceLoad.FileName = "../Manual_811_KMC_RENAMED.zks";
                    TestRunner.True(request.Validate().Count > 0, "Renamed archive traversal accepted.");
                    request.Scenario = "persistence-p01-load";
                    TestRunner.Equal("Manual", request.ExpectedNativeLoadType, "Native category escaped P05.");
                    TestRunner.True(request.Validate().Count > 0, "P05 case escaped its scenario.");
                }
            });
            runner.Run("alternating cold loads require two distinct exact native archives", () =>
            {
                var request = ValidSaveBackedRequest(); var f = request.Fixture.Working;
                request.Scenario = "persistence-p05-save"; request.PersistenceCase = "alternating";
                TestRunner.Equal(0, request.Validate().Count, "Alternating source rejected.");
                request.Scenario = "persistence-p05-load";
                request.PersistenceLoad = new RuntimeSaveDescriptor {
                    InternalName = "KMC_P01", FileName = "Manual_300_KMC_P01.zks", Sha256 = new string('c', 64),
                    GameId = f.GameId, GameName = f.GameName, Area = f.Area, Length = 1024, LastWriteTimeUtcTicks = f.LastWriteTimeUtcTicks };
                TestRunner.True(request.Validate().Count > 0, "Missing alternate archive was accepted.");
                request.PersistenceAlternate = new RuntimeSaveDescriptor {
                    InternalName = "KMC_P05_UNMOUNTED", FileName = "Manual_301_KMC_P05_UNMOUNTED.zks", Sha256 = new string('d', 64),
                    GameId = f.GameId, GameName = f.GameName, Area = f.Area, Length = 1024, LastWriteTimeUtcTicks = f.LastWriteTimeUtcTicks };
                TestRunner.Equal(0, request.Validate().Count, "Exact distinct A/B archives rejected.");
                request.PersistenceAlternate.Sha256 = request.PersistenceLoad.Sha256;
                TestRunner.True(request.Validate().Count > 0, "A/B aliased identical archive bytes.");
                request.PersistenceAlternate.Sha256 = new string('d', 64);
                request.PersistenceAlternate.FileName = "../Manual_301_KMC_P05_UNMOUNTED.zks";
                TestRunner.True(request.Validate().Count > 0, "Alternate traversal accepted.");
                request.PersistenceAlternate.FileName = "Manual_301_KMC_P05_UNMOUNTED.zks";
                request.PersistenceCase = "manual";
                TestRunner.True(request.Validate().Count > 0, "Alternate archive leaked into single-load scenario.");
                request.PersistenceCase = "alternating"; request.Scenario = "persistence-p05-save";
                TestRunner.True(request.Validate().Count > 0, "Source creation accepted cold archive authority.");
            });
            runner.Run("P06 owns exactly two read-only archive identities and a bounded validation case", () =>
            {
                foreach (var name in new[] { "legacy", "schema1", "future", "malformed", "profile", "campaign",
                    "missing-rider", "missing-mount", "mismatched-profile", "policy", "combat-missing", "combat-ai",
                    "failed-area-load" })
                {
                    // The failed-load derivative edits a native member, so it owns
                    // its own leaf and can never be byte-identical to its source.
                    var failedLoad = RuntimeRequest.IsFailedLoad(name);
                    var leaf = failedLoad ? "Manual_813_KMC_P06_AREA.zks" : "Manual_812_KMC_P06.zks";
                    var request = ValidSaveBackedRequest(); var f = request.Fixture.Working;
                    request.Scenario = "persistence-p06-load"; request.PersistenceCase = name;
                    request.PersistenceLoad = new RuntimeSaveDescriptor {
                        InternalName = "KMC_P01", FileName = "Manual_300_KMC_P01.zks", Sha256 = new string('c', 64),
                        GameId = f.GameId, GameName = f.GameName, Area = f.Area, Length = 1024, LastWriteTimeUtcTicks = f.LastWriteTimeUtcTicks };
                    TestRunner.True(request.Validate().Count > 0, "P06 accepted a missing validation copy.");
                    request.PersistenceAlternate = new RuntimeSaveDescriptor {
                        InternalName = "KMC_P01", FileName = leaf, Sha256 = new string('d', 64),
                        GameId = f.GameId, GameName = f.GameName, Area = f.Area, Length = 1024, LastWriteTimeUtcTicks = f.LastWriteTimeUtcTicks };
                    TestRunner.Equal(0, request.Validate().Count, "P06 exact archive pair rejected.");
                    request.PersistenceAlternate.FileName = failedLoad ? "Manual_812_KMC_P06.zks" : "Manual_813_KMC_P06_AREA.zks";
                    TestRunner.True(request.Validate().Count > 0, "P06 accepted the other variant's leaf.");
                    request.PersistenceAlternate.FileName = leaf;
                    if (failedLoad)
                    {
                        request.PersistenceAlternate.Sha256 = request.PersistenceLoad.Sha256;
                        TestRunner.True(request.Validate().Count > 0, "P06 failed-load accepted an unchanged derivative.");
                        request.PersistenceAlternate.Sha256 = new string('d', 64);
                    }
                    request.PersistenceAlternate.GameId = "00000000-0000-0000-0000-000000000001";
                    TestRunner.True(request.Validate().Count > 0, "P06 allowed a foreign native campaign.");
                    request.PersistenceAlternate.GameId = f.GameId;
                    request.PersistenceAlternate.FileName = "../" + leaf;
                    TestRunner.True(request.Validate().Count > 0, "P06 variant escaped its direct-child leaf.");
                    request.PersistenceAlternate.FileName = leaf;
                    request.Scenario = "persistence-p01-load";
                    TestRunner.True(request.Validate().Count > 0, "P06 variant leaked into an old scenario.");
                }
            });
            runner.Run("P06 foreign-header derivative carries B's own native identity on the alternate only", () =>
            {
                // Campaign B's own archive whose KMC member claims A: the alternate
                // is B's native header and bytes; the primary stays A's mounted save.
                const string minted = "bf673e4e-5e19-4ec3-b5a5-54d59ea73357";
                var request = ValidSaveBackedRequest(); var f = request.Fixture.Working;
                request.Scenario = "persistence-p06-load"; request.PersistenceCase = "foreign-header-campaign";
                request.PersistenceLoad = new RuntimeSaveDescriptor {
                    InternalName = "KMC_P01", FileName = "Manual_300_KMC_P01.zks", Sha256 = new string('c', 64),
                    GameId = f.GameId, GameName = f.GameName, Area = f.Area, Length = 1024, LastWriteTimeUtcTicks = f.LastWriteTimeUtcTicks };
                TestRunner.True(request.Validate().Count > 0, "Foreign-header load accepted a missing derivative.");
                request.PersistenceAlternate = new RuntimeSaveDescriptor {
                    InternalName = "KMC_B", FileName = "Manual_812_KMC_P06.zks", Sha256 = new string('d', 64),
                    GameId = minted, GameName = "Baron", Area = new string('e', 32),
                    Length = 1024, LastWriteTimeUtcTicks = f.LastWriteTimeUtcTicks };
                TestRunner.Equal(0, request.Validate().Count, "Exact foreign-header pair rejected.");
                request.PersistenceAlternate.GameId = f.GameId;
                TestRunner.True(request.Validate().Count > 0, "Foreign-header derivative accepted A's own campaign.");
                request.PersistenceAlternate.GameId = Guid.Empty.ToString();
                TestRunner.True(request.Validate().Count > 0, "Foreign-header derivative accepted an empty campaign identity.");
                request.PersistenceAlternate.GameId = minted; request.PersistenceAlternate.Area = f.Area;
                TestRunner.True(request.Validate().Count > 0, "Foreign-header derivative accepted A's own area.");
                request.PersistenceAlternate.Area = new string('e', 32); request.PersistenceAlternate.InternalName = "KMC_P01";
                TestRunner.True(request.Validate().Count > 0, "Foreign-header derivative accepted A's own save name.");
                request.PersistenceAlternate.InternalName = "KMC_B"; request.PersistenceAlternate.Sha256 = request.PersistenceLoad.Sha256;
                TestRunner.True(request.Validate().Count > 0, "Foreign-header derivative aliased A's bytes.");
                request.PersistenceAlternate.Sha256 = new string('d', 64); request.PersistenceAlternate.FileName = "Manual_813_KMC_P06_AREA.zks";
                TestRunner.True(request.Validate().Count > 0, "Foreign-header derivative accepted the failed-load leaf.");
                request.PersistenceAlternate.FileName = "Manual_812_KMC_P06.zks"; request.PersistenceCase = "campaign";
                TestRunner.True(request.Validate().Count > 0, "The metadata-only campaign variant accepted a foreign native header.");
                request.PersistenceCase = "foreign-header-campaign"; request.PersistenceLoad.GameId = minted;
                TestRunner.True(request.Validate().Count > 0, "Foreign-header primary accepted B's identity.");
                request.PersistenceLoad.GameId = f.GameId; request.Scenario = "persistence-p07-load";
                TestRunner.True(request.Validate().Count > 0, "Foreign-header derivative leaked into a recovery scenario.");
            });
            runner.Run("P07 recovery requests retain exact source and cold archive authority", () =>
            {
                // The transition autosave each cross-area source produced is a
                // distinct cold artifact from its destination manual archive:
                // AfterEntry committed in the destination, BeforeExit in the
                // departure area, and neither may be swapped for the other.
                foreach (var entry in new[] { true, false })
                {
                    var target = new string('e', 32);
                    var request = ValidSaveBackedRequest(); var f = request.Fixture.Working;
                    request.Scenario = "persistence-p07-load";
                    request.PersistenceCase = entry ? "area-cross-entry-auto" : "area-cross-exit-auto";
                    request.PersistenceAreaTarget = new RuntimeAreaTransitionTarget {
                        EnterPoint = new string('d', 32), Area = target,
                        AutoSaveMode = entry ? "AfterEntry" : "BeforeExit" };
                    var committed = entry ? target : f.Area;
                    request.PersistenceLoad = new RuntimeSaveDescriptor {
                        InternalName = "Auto 1", FileName = "Auto_1.zks", Sha256 = new string('c', 64),
                        GameId = f.GameId, GameName = f.GameName, Area = committed,
                        Length = 1024, LastWriteTimeUtcTicks = f.LastWriteTimeUtcTicks };
                    TestRunner.Equal(0, request.Validate().Count, "Exact transition autosave cold request rejected.");
                    request.PersistenceLoad.Area = entry ? f.Area : target;
                    TestRunner.True(request.Validate().Count > 0, "Transition autosave accepted the other leg's area.");
                    request.PersistenceLoad.Area = committed;
                    request.PersistenceLoad.FileName = "Manual_300_KMC_P01.zks";
                    TestRunner.True(request.Validate().Count > 0, "Transition autosave accepted a manual leaf.");
                    request.PersistenceLoad.FileName = "Auto_1.zks";
                    request.PersistenceAreaTarget.AutoSaveMode = entry ? "BeforeExit" : "AfterEntry";
                    TestRunner.True(request.Validate().Count > 0, "Transition autosave accepted the other authored mode.");
                    request.PersistenceAreaTarget.AutoSaveMode = entry ? "AfterEntry" : "BeforeExit";
                    request.PersistenceLoad.GameId = "00000000-0000-0000-0000-000000000001";
                    TestRunner.True(request.Validate().Count > 0, "Transition autosave accepted a foreign campaign.");
                    request.PersistenceLoad.GameId = f.GameId;
                    request.Scenario = "persistence-p07-save";
                    TestRunner.True(request.Validate().Count > 0, "Transition autosave accepted a writing scenario.");
                }
                foreach (var name in new[] { "timeout", "cancel-wait", "locked-replace", "serialization-cancel", "serialization-cancel-output", "disable-reenable", "area-reload",
                    "area-cross-entry", "area-cross-exit" })
                {
                    var cross = name == "area-cross-entry" || name == "area-cross-exit";
                    var target = new string('e', 32);
                    var request = ValidSaveBackedRequest(); var f = request.Fixture.Working;
                    request.Scenario = "persistence-p07-save"; request.PersistenceCase = name;
                    if (cross)
                    {
                        TestRunner.True(request.Validate().Count > 0, "Cross-area case accepted no declared destination.");
                        request.PersistenceAreaTarget = new RuntimeAreaTransitionTarget {
                            EnterPoint = new string('d', 32), Area = target,
                            AutoSaveMode = name == "area-cross-entry" ? "AfterEntry" : "BeforeExit" };
                    }
                    TestRunner.Equal(0, request.Validate().Count, "Exact recovery save rejected.");
                    if (cross)
                    {
                        var declared = request.PersistenceAreaTarget;
                        request.PersistenceAreaTarget = new RuntimeAreaTransitionTarget {
                            EnterPoint = declared.EnterPoint, Area = f.Area, AutoSaveMode = declared.AutoSaveMode };
                        TestRunner.True(request.Validate().Count > 0, "Cross-area accepted its own loaded area as the destination.");
                        request.PersistenceAreaTarget = new RuntimeAreaTransitionTarget {
                            EnterPoint = declared.EnterPoint, Area = target, AutoSaveMode = "None" };
                        TestRunner.True(request.Validate().Count > 0, "Cross-area accepted an unauthored transition mode.");
                        request.PersistenceAreaTarget = declared;
                    }
                    request.Scenario = "persistence-p07-load";
                    TestRunner.True(request.Validate().Count > 0, "Recovery cold load accepted no archive.");
                    request.PersistenceLoad = new RuntimeSaveDescriptor {
                        InternalName = "KMC_P01", FileName = "Manual_300_KMC_P01.zks", Sha256 = new string('c', 64),
                        GameId = f.GameId, GameName = f.GameName, Area = cross ? target : f.Area,
                        Length = 1024, LastWriteTimeUtcTicks = f.LastWriteTimeUtcTicks };
                    TestRunner.Equal(0, request.Validate().Count, "Exact recovery cold archive rejected.");
                    if (cross)
                    {
                        request.PersistenceLoad.Area = f.Area;
                        TestRunner.True(request.Validate().Count > 0, "Cross-area cold archive accepted the departure area.");
                        request.PersistenceLoad.Area = target;
                    }
                    request.PersistenceLoad.GameId = "00000000-0000-0000-0000-000000000001";
                    TestRunner.True(request.Validate().Count > 0, "Recovery cold request allowed a foreign campaign.");
                    request.PersistenceLoad = null; request.Scenario = "persistence-p01-save";
                    TestRunner.True(request.Validate().Count > 0, "Recovery fault leaked into an old scenario.");
                    request.Scenario = "persistence-p07-save"; request.PersistenceCase = "area-reload";
                    request.PersistenceAreaTarget = new RuntimeAreaTransitionTarget {
                        EnterPoint = new string('d', 32), Area = target, AutoSaveMode = "AfterEntry" };
                    TestRunner.True(request.Validate().Count > 0, "A same-area case accepted a cross-area destination.");
                }
                // Save-only removal cases, and the cold-only integration-absent
                // case that opens the cleanup archive under its own name.
                foreach (var name in new[] { "prepare-removal", "disable-during-load" })
                {
                    var request = ValidSaveBackedRequest(); var f = request.Fixture.Working;
                    request.Scenario = "persistence-p07-save"; request.PersistenceCase = name;
                    TestRunner.Equal(0, request.Validate().Count, "Exact removal save case rejected.");
                    request.Scenario = "persistence-p07-load";
                    request.PersistenceLoad = new RuntimeSaveDescriptor {
                        InternalName = "KMC_P01", FileName = "Manual_300_KMC_P01.zks", Sha256 = new string('c', 64),
                        GameId = f.GameId, GameName = f.GameName, Area = f.Area, Length = 1024, LastWriteTimeUtcTicks = f.LastWriteTimeUtcTicks };
                    TestRunner.True(request.Validate().Count > 0, "A save-only removal case accepted a cold load.");
                }
                {
                    var request = ValidSaveBackedRequest(); var f = request.Fixture.Working;
                    request.Scenario = "persistence-p07-save"; request.PersistenceCase = "absent-kmc";
                    TestRunner.True(request.Validate().Count > 0, "The integration-absent case accepted a writing scenario.");
                    request.Scenario = "persistence-p07-load";
                    request.PersistenceLoad = new RuntimeSaveDescriptor {
                        InternalName = "KMC_CLEANUP", FileName = "Manual_301_KMC_CLEANUP.zks", Sha256 = new string('c', 64),
                        GameId = f.GameId, GameName = f.GameName, Area = f.Area, Length = 1024, LastWriteTimeUtcTicks = f.LastWriteTimeUtcTicks };
                    TestRunner.Equal(0, request.Validate().Count, "Exact integration-absent cold request rejected.");
                    request.PersistenceLoad.FileName = "Manual_300_KMC_P01.zks"; request.PersistenceLoad.InternalName = "KMC_P01";
                    TestRunner.True(request.Validate().Count > 0, "The integration-absent case accepted a mounted archive leaf.");
                    request.PersistenceLoad.FileName = "Manual_301_KMC_CLEANUP.zks"; request.PersistenceLoad.InternalName = "KMC_CLEANUP";
                    request.PersistenceLoad.GameId = "00000000-0000-0000-0000-000000000001";
                    TestRunner.True(request.Validate().Count > 0, "The integration-absent case accepted a foreign campaign.");
                }
                // The genuine no-DLL cleanup-save observation is cold-load only and
                // opens the prepared cleanup archive under its own name.
                {
                    var request = ValidSaveBackedRequest(); var f = request.Fixture.Working;
                    request.Scenario = "persistence-p07-save"; request.PersistenceCase = "removal-no-dll";
                    TestRunner.True(request.Validate().Count > 0, "The no-DLL case accepted a writing scenario.");
                    request.Scenario = "persistence-p07-load";
                    request.PersistenceLoad = new RuntimeSaveDescriptor {
                        InternalName = "KMC_CLEANUP", FileName = "Manual_301_KMC_CLEANUP.zks", Sha256 = new string('c', 64),
                        GameId = f.GameId, GameName = f.GameName, Area = f.Area, Length = 1024, LastWriteTimeUtcTicks = f.LastWriteTimeUtcTicks };
                    TestRunner.Equal(0, request.Validate().Count, "Exact no-DLL cold request rejected.");
                    request.PersistenceLoad.FileName = "Manual_300_KMC_P01.zks"; request.PersistenceLoad.InternalName = "KMC_P01";
                    TestRunner.True(request.Validate().Count > 0, "The no-DLL case accepted a mounted archive leaf.");
                }
                // Death boundaries: a writing run and a cold load of the exact
                // no-pair archive that run wrote, under its own name.
                foreach (var name in new[] { "rider-death", "mount-death" })
                {
                    var request = ValidSaveBackedRequest(); var f = request.Fixture.Working;
                    request.Scenario = "persistence-p07-save"; request.PersistenceCase = name;
                    TestRunner.Equal(0, request.Validate().Count, "Exact death save case rejected.");
                    request.Scenario = "persistence-p07-load";
                    request.PersistenceLoad = new RuntimeSaveDescriptor {
                        InternalName = "KMC_DEATH", FileName = "Manual_301_KMC_DEATH.zks", Sha256 = new string('c', 64),
                        GameId = f.GameId, GameName = f.GameName, Area = f.Area, Length = 1024, LastWriteTimeUtcTicks = f.LastWriteTimeUtcTicks };
                    TestRunner.Equal(0, request.Validate().Count, "Exact death cold request rejected.");
                    request.PersistenceLoad.FileName = "Manual_300_KMC_P01.zks"; request.PersistenceLoad.InternalName = "KMC_P01";
                    TestRunner.True(request.Validate().Count > 0, "A death cold load accepted the mounted archive leaf.");
                    request.PersistenceLoad.FileName = "Manual_301_KMC_DEATH.zks"; request.PersistenceLoad.InternalName = "KMC_DEATH";
                    request.PersistenceLoad.GameId = "00000000-0000-0000-0000-000000000001";
                    TestRunner.True(request.Validate().Count > 0, "A death cold load accepted a foreign campaign.");
                }
                // The live eligibility change: a writing run and a cold load of
                // the exact no-pair archive that run wrote, under its own name.
                {
                    var request = ValidSaveBackedRequest(); var f = request.Fixture.Working;
                    request.Scenario = "persistence-p07-save"; request.PersistenceCase = "rider-size-change";
                    TestRunner.Equal(0, request.Validate().Count, "Exact eligibility save case rejected.");
                    request.Scenario = "persistence-p07-load";
                    request.PersistenceLoad = new RuntimeSaveDescriptor {
                        InternalName = "KMC_SIZE", FileName = "Manual_301_KMC_SIZE.zks", Sha256 = new string('c', 64),
                        GameId = f.GameId, GameName = f.GameName, Area = f.Area, Length = 1024, LastWriteTimeUtcTicks = f.LastWriteTimeUtcTicks };
                    TestRunner.Equal(0, request.Validate().Count, "Exact eligibility cold request rejected.");
                    request.PersistenceLoad.FileName = "Manual_300_KMC_P01.zks"; request.PersistenceLoad.InternalName = "KMC_P01";
                    TestRunner.True(request.Validate().Count > 0, "An eligibility cold load accepted the mounted archive leaf.");
                    request.PersistenceLoad.FileName = "Manual_301_KMC_DEATH.zks"; request.PersistenceLoad.InternalName = "KMC_DEATH";
                    TestRunner.True(request.Validate().Count > 0, "An eligibility cold load accepted a death archive leaf.");
                    request.PersistenceLoad.FileName = "Manual_301_KMC_SIZE.zks"; request.PersistenceLoad.InternalName = "KMC_SIZE";
                    request.PersistenceLoad.GameId = "00000000-0000-0000-0000-000000000001";
                    TestRunner.True(request.Validate().Count > 0, "An eligibility cold load accepted a foreign campaign.");
                }
                // Campaign B's own manual archive opened cold: B's minted identity
                // and area, which are exactly NOT the fixture's.
                {
                    var request = ValidSaveBackedRequest(); var f = request.Fixture.Working;
                    const string minted = "bf673e4e-5e19-4ec3-b5a5-54d59ea73357";
                    request.Scenario = "persistence-p07-save"; request.PersistenceCase = "campaign-b";
                    TestRunner.Equal(0, request.Validate().Count, "Exact campaign-B save case rejected.");
                    request.Scenario = "persistence-p07-load";
                    TestRunner.True(request.Validate().Count > 0, "A campaign-B cold load accepted no archive.");
                    request.PersistenceLoad = new RuntimeSaveDescriptor {
                        InternalName = "KMC_P01", FileName = "Manual_300_KMC_P01.zks", Sha256 = new string('c', 64),
                        GameId = f.GameId, GameName = f.GameName, Area = f.Area, Length = 1024, LastWriteTimeUtcTicks = f.LastWriteTimeUtcTicks };
                    TestRunner.True(request.Validate().Count > 0, "A campaign-B cold load accepted the fixture's own mounted archive.");
                    request.PersistenceLoad = new RuntimeSaveDescriptor {
                        InternalName = "KMC_B", FileName = "Manual_302_KMC_B.zks", Sha256 = new string('c', 64),
                        GameId = minted, GameName = "Baron", Area = new string('c', 32),
                        Length = 1024, LastWriteTimeUtcTicks = f.LastWriteTimeUtcTicks };
                    TestRunner.Equal(0, request.Validate().Count, "Exact campaign-B cold request rejected.");
                    request.PersistenceLoad.GameId = f.GameId;
                    TestRunner.True(request.Validate().Count > 0, "A campaign-B cold load accepted the fixture campaign.");
                    request.PersistenceLoad.GameId = Guid.Empty.ToString();
                    TestRunner.True(request.Validate().Count > 0, "A campaign-B cold load accepted an empty campaign identity.");
                    request.PersistenceLoad.GameId = minted; request.PersistenceLoad.Area = f.Area;
                    TestRunner.True(request.Validate().Count > 0, "A campaign-B cold load accepted the fixture area.");
                    request.PersistenceLoad.Area = new string('c', 32); request.PersistenceLoad.GameName = "";
                    TestRunner.True(request.Validate().Count > 0, "A campaign-B cold load accepted an empty campaign name.");
                    request.PersistenceLoad.GameName = "Baron";
                    request.PersistenceLoad.FileName = "Manual_300_KMC_P01.zks"; request.PersistenceLoad.InternalName = "KMC_P01";
                    TestRunner.True(request.Validate().Count > 0, "A campaign-B cold load accepted the mounted archive leaf.");
                    request.PersistenceLoad.FileName = "Manual_302_KMC_B.zks"; request.PersistenceLoad.InternalName = "KMC_B";
                    request.Scenario = "persistence-p01-load";
                    TestRunner.True(request.Validate().Count > 0, "Campaign B's archive leaked into an old scenario.");
                }
            });
            RuntimeSaveAuthorizationTests.Register(runner);
            ScopedEnumeratorTests.Register(runner);
            DeferredSaveEnumeratorTests.Register(runner);
            OwnedWorkerTeardownPolicyTests.Register(runner);
            NativeSaveCommitOutcomeTests.Register(runner);
            RemovalReadinessPolicyTests.Register(runner);
            NativeLoadWorldTests.Register(runner);
            PersistenceSaveAuthorizationTests.Register(runner);
            WorkingFixtureLoadWatchdogTests.Register(runner);
            SustainedRoutineProgressTests.Register(runner);
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
            PairedCommandSchedulerTests.Register(runner);
            MountedStockAttackPolicyTests.Register(runner);
            runner.Run("Chunk 4 core native requests and leaves remain serializable", () =>
            {
                foreach (var scenario in new[] { "chunk4-rider-incapacitation-tb", "chunk4-rider-death-tb", "chunk4-mount-death-tb",
                    "chunk4-targeting-rider-rt", "chunk4-targeting-mount-rt", "chunk4-ground-arrival-rt", "chunk4-horse-strike-comparison-rt", "chunk4-ranged-native-control-rt", "chunk4-interrupt-melee-rt", "chunk4-interrupt-ranged-rt", "chunk4-inspection-rt", "chunk4-session-rt", "chunk4-session-tb" })
                {
                    var request = ValidSaveBackedRequest(); request.Scenario = scenario;
                    TestRunner.Equal(0, request.Validate().Count, "Core request rejected: " + scenario);
                    TestRunner.True(HorseCompanionRegistrationScenarioPolicy.SupportsScenario(scenario), "Core registration missing.");
                }
                foreach (var name in new[] { "C4-LIFE-rider-incapacitation", "C4-LIFE-rider-death-live-command", "C4-LIFE-mount-death-live-command",
                    "C4-TARGETING-rider-heal", "C4-TARGETING-rider-hostile", "C4-TARGETING-mount-heal", "C4-TARGETING-mount-hostile", "C4-TARGETING-area-both",
                    "C4-GROUND-mounted-arrival", "C4-GROUND-unmounted-arrival",
                    "C4-HORSE-mounted-three-primaries", "C4-HORSE-unmounted-strike-recovery", "C4-RANGED-native-mixed-range", "C4-INTERRUPT-melee-pause-resume", "C4-INTERRUPT-melee-pause-stop-recover", "C4-INTERRUPT-melee-moving-target", "C4-INTERRUPT-melee-retarget-windup", "C4-INTERRUPT-melee-target-death-windup", "C4-INTERRUPT-melee-target-death-midroutine", "C4-INTERRUPT-ranged-pause-resume", "C4-INTERRUPT-ranged-pause-stop-recover", "C4-INTERRUPT-ranged-moving-target", "C4-INTERRUPT-ranged-retarget-windup", "C4-INTERRUPT-ranged-retarget-inflight", "C4-INTERRUPT-ranged-target-death-windup", "C4-INTERRUPT-ranged-target-death-inflight", "C4-INSPECTION-rider", "C4-INSPECTION-mount", "C4-SESSION-RT-1", "C4-SESSION-RT-2", "C4-SESSION-RT-3", "C4-SESSION-TB-1", "C4-SESSION-TB-2", "C4-SESSION-TB-3" })
                {
                    var result = new RuntimeSubscenarioResult { Name = name, Status = "PASS", AssertionPassCount = 1, Errors = new string[0] };
                    TestRunner.Equal(0, result.Validate().Count, "Core native leaf missing: " + name);
                }
            });
            MountedChargeSafetyTests.Register(runner);
            MountedRangedRoutineCompletionTests.Register(runner);
            OptionalPublicPropertyReaderTests.Register(runner);
            ReactiveBooleanValueReaderTests.Register(runner);
            StopEarlyCaptureBoundaryTests.Register(runner);
            PresentationOverlayEvidenceTests.Register(runner);
            ScopedDiagnosticAiLeaseTests.Register(runner);
            ExpectedAttackDispatchLedgerTests.Register(runner);
            ExactAppendOnlyArrayLeaseTests.Register(runner);
            HorseCompanionLifeTransitionPolicyTests.Register(runner);
            HorseCompanionProgressionPolicyTests.Register(runner);
            HorseCompanionScenarioDeadlinePolicyTests.Register(runner);
            DiagnosticTurnTraversalPolicyTests.Register(runner);
            return runner.Complete();
        }

        private static void DiagnosticSettingsDefaultsAreSafe()
        {
            var settings = new DiagnosticSettings();
            TestRunner.Equal(true, settings.EnableUnsafeMovementExperiment, "Native mounted controls must default on for the enabled private alpha.");
            TestRunner.Equal(false, settings.EnableUnifiedMountedTurn, "The bounded Phase 3E fallback must default to accepted Phase 3C separate turns.");
            TestRunner.Equal(false, settings.EnablePairedCommandScheduler, "The unqualified paired-command scheduler must default off.");
            TestRunner.Equal(false, settings.EnablePairedActivation, "The paired prototype requires explicit developer opt-in.");
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
                "phase3d-horse-presentation-suite",
                "phase3h-combat-loop-rt", "phase3h-combat-loop-tb"
            })
            {
                var request = ValidSaveBackedRequest();
                request.Scenario = scenario;
                TestRunner.Equal(0, request.Validate().Count, scenario + " request was rejected.");
            }
        }

        private static void RegistrationAuditAcceptsExactHorseParentScenarios()
        {
            foreach (var scenario in new[]
            {
                "horse-companion-blueprint-registration",
                "horse-companion-unmounted-suite",
                "horse-mounted-alpha-suite",
                "horse-native-controls-ux-suite",
                "phase3d-unified-combat-rt-suite",
                "phase3d-unified-combat-tb-suite",
                "phase3d-horse-presentation-suite"
            })
            {
                TestRunner.Equal(
                    true,
                    HorseCompanionRegistrationScenarioPolicy.SupportsScenario(scenario),
                    scenario + " was rejected by the registration prerequisite.");
            }

            TestRunner.Equal(
                false,
                HorseCompanionRegistrationScenarioPolicy.SupportsScenario("foreign-horse-scenario"),
                "The registration prerequisite accepted an unknown scenario.");
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
