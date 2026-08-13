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
            runner.Run("result accepts complete PASS", ResultAcceptsCompletePass);
            runner.Run("result rejects non-terminal status", ResultRejectsNonTerminalStatus);
            MountedRelationshipTests.Register(runner);
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
    }
}
