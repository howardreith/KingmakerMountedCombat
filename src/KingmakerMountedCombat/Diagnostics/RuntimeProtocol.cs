using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace KingmakerMountedCombat.Diagnostics
{
    public sealed class RuntimeRequest
    {
        public const int CurrentSchemaVersion = 1;

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

        public bool SaveAccessAllowed { get; set; }

        public string SaveName { get; set; }

        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();
            if (SchemaVersion != CurrentSchemaVersion)
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

            if (!string.Equals(ProductVersion, "0.0.1-feasibility", StringComparison.Ordinal))
            {
                errors.Add("productVersion does not match this diagnostic build.");
            }

            if (SaveAccessAllowed && !string.Equals(SaveName, "KMC_AUTOMATION_WORKING", StringComparison.Ordinal))
            {
                errors.Add("Save-backed requests may name only KMC_AUTOMATION_WORKING.");
            }

            if (!SaveAccessAllowed && !string.IsNullOrEmpty(SaveName))
            {
                errors.Add("No-save requests must not include saveName.");
            }

            if (!SaveAccessAllowed && !string.Equals(Scenario, "mod-load-smoke", StringComparison.Ordinal))
            {
                errors.Add("Only mod-load-smoke is implemented as a no-save runtime scenario.");
            }

            return errors;
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
            if (string.IsNullOrEmpty(value) || value.Length != 64)
            {
                errors.Add(name + " must be a 64-character hexadecimal SHA-256.");
                return;
            }

            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (!((character >= '0' && character <= '9') || (character >= 'a' && character <= 'f')))
                {
                    errors.Add(name + " must be hexadecimal.");
                    return;
                }
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

        private static bool IsLowerHex(string value, int length)
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
    }

    public sealed class RuntimeResult
    {
        public const int CurrentSchemaVersion = 1;

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

        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();
            if (SchemaVersion != CurrentSchemaVersion)
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

            DateTimeOffset timestamp;
            if (!DateTimeOffset.TryParse(StartedAtUtc, out timestamp))
            {
                errors.Add("startedAtUtc must be an ISO-8601 timestamp.");
            }

            if (!DateTimeOffset.TryParse(CompletedAtUtc, out timestamp))
            {
                errors.Add("completedAtUtc must be an ISO-8601 timestamp.");
            }

            if (!string.Equals(Status, "PASS", StringComparison.Ordinal) && !string.Equals(Status, "FAIL", StringComparison.Ordinal))
            {
                errors.Add("status must be PASS or FAIL.");
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
    }
}
