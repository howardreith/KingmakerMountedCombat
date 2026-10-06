using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace KmcRemovalObserver
{
    public sealed class ObserverArchive
    {
        [JsonProperty("internalName")] public string InternalName { get; set; }
        [JsonProperty("fileName")] public string FileName { get; set; }
        [JsonProperty("sha256")] public string Sha256 { get; set; }
        [JsonProperty("length")] public long Length { get; set; }
        [JsonProperty("lastWriteTimeUtcTicks")] public long LastWriteTimeUtcTicks { get; set; }
        [JsonProperty("gameId")] public string GameId { get; set; }
        [JsonProperty("gameName")] public string GameName { get; set; }
        [JsonProperty("area")] public string Area { get; set; }
    }

    public sealed class ObserverCandidate
    {
        [JsonProperty("commit")] public string Commit { get; set; }
        [JsonProperty("productVersion")] public string ProductVersion { get; set; }
        [JsonProperty("dllSha256")] public string DllSha256 { get; set; }
        [JsonProperty("dllMvid")] public string DllMvid { get; set; }
    }

    public sealed class ObserverPackage
    {
        [JsonProperty("version")] public string Version { get; set; }
        [JsonProperty("packageSha256")] public string PackageSha256 { get; set; }
        [JsonProperty("dllSha256")] public string DllSha256 { get; set; }
        [JsonProperty("dllMvid")] public string DllMvid { get; set; }
    }

    public sealed class ObserverChargeSource
    {
        [JsonProperty("runId")] public string RunId { get; set; }
        [JsonProperty("observationsSha256")] public string ObservationsSha256 { get; set; }
        [JsonProperty("riderId")] public string RiderId { get; set; }
        [JsonProperty("mountId")] public string MountId { get; set; }
        [JsonProperty("chargeBuffGuid")] public string ChargeBuffGuid { get; set; }

        internal Newtonsoft.Json.Linq.JObject CaptureEvidence() => new Newtonsoft.Json.Linq.JObject
        {
            ["runId"] = RunId, ["observationsSha256"] = ObservationsSha256,
            ["riderId"] = RiderId, ["mountId"] = MountId, ["chargeBuffGuid"] = ChargeBuffGuid
        };
    }

    // Written by the launcher next to the run's evidence; bound to the process
    // by the command-line token and SHA-256 of its exact bytes.
    public sealed class ObserverRequest
    {
        [JsonProperty("schemaVersion")] public int SchemaVersion { get; set; }
        [JsonProperty("runId")] public string RunId { get; set; }
        [JsonProperty("transactionToken")] public string TransactionToken { get; set; }
        [JsonProperty("evidenceRoot")] public string EvidenceRoot { get; set; }
        [JsonProperty("profileRoot")] public string ProfileRoot { get; set; }
        [JsonProperty("modsRoot")] public string ModsRoot { get; set; }
        [JsonProperty("kmcModId")] public string KmcModId { get; set; }
        [JsonProperty("observerModId")] public string ObserverModId { get; set; }
        [JsonProperty("kmcHarmonyIds")] public string[] KmcHarmonyIds { get; set; }
        [JsonProperty("archive")] public ObserverArchive Archive { get; set; }
        [JsonProperty("kmcBlueprintGuids")] public string[] KmcBlueprintGuids { get; set; }
        [JsonProperty("mammothBlueprintGuid")] public string MammothBlueprintGuid { get; set; }
        [JsonProperty("candidate")] public ObserverCandidate Candidate { get; set; }
        [JsonProperty("observer")] public ObserverPackage Observer { get; set; }
        [JsonProperty("timeoutSeconds")] public int TimeoutSeconds { get; set; }
        [JsonProperty("chargeSource")] public ObserverChargeSource ChargeSource { get; set; }

        private static readonly Regex Sha = new Regex("^[0-9a-f]{64}$");
        private static readonly Regex BlueprintGuid = new Regex("^[0-9a-f]{32}$");
        private static readonly Regex RunIdShape = new Regex("^[A-Za-z0-9._-]{1,120}$");

        public List<string> Validate()
        {
            var errors = new List<string>();
            if (SchemaVersion != (ChargeSource == null ? 1 : 2)) errors.Add("Unknown observer request schema or charge-source binding.");
            if (ChargeSource != null && (ChargeSource.RunId == null || !RunIdShape.IsMatch(ChargeSource.RunId) ||
                ChargeSource.ObservationsSha256 == null || !Sha.IsMatch(ChargeSource.ObservationsSha256) ||
                string.IsNullOrWhiteSpace(ChargeSource.RiderId) || ChargeSource.RiderId.Length > 120 ||
                string.IsNullOrWhiteSpace(ChargeSource.MountId) || ChargeSource.MountId.Length > 120 ||
                ChargeSource.RiderId == ChargeSource.MountId || ChargeSource.ChargeBuffGuid == null ||
                !BlueprintGuid.IsMatch(ChargeSource.ChargeBuffGuid))) errors.Add("Charge source identity is incomplete.");
            if (RunId == null || !RunIdShape.IsMatch(RunId)) errors.Add("Run id is invalid.");
            if (TransactionToken == null || !Sha.IsMatch(TransactionToken)) errors.Add("Transaction token is invalid.");
            foreach (var pair in new[] { new KeyValuePair<string, string>("evidenceRoot", EvidenceRoot),
                new KeyValuePair<string, string>("profileRoot", ProfileRoot), new KeyValuePair<string, string>("modsRoot", ModsRoot) })
                if (string.IsNullOrWhiteSpace(pair.Value) || !System.IO.Path.IsPathRooted(pair.Value)) errors.Add(pair.Key + " must be an absolute path.");
            if (KmcModId != "KingmakerMountedCombat") errors.Add("The observed mod id is not the KMC mod id.");
            if (ObserverModId != "KmcRemovalObserver") errors.Add("The observer mod id is not this mod.");
            if (KmcHarmonyIds == null || KmcHarmonyIds.Length == 0 || KmcHarmonyIds.Any(string.IsNullOrWhiteSpace)) errors.Add("KMC Harmony ids are missing.");
            if (Archive == null) errors.Add("The cleanup archive descriptor is missing.");
            else
            {
                if (Archive.InternalName != "KMC_CLEANUP" || Archive.FileName != "Manual_301_KMC_CLEANUP.zks") errors.Add("The archive is not the prepared cleanup save.");
                if (Archive.Sha256 == null || !Sha.IsMatch(Archive.Sha256) || Archive.Length <= 0) errors.Add("The archive identity is invalid.");
                Guid parsed;
                if (Archive.GameId == null || !Guid.TryParse(Archive.GameId, out parsed) || parsed == Guid.Empty || string.IsNullOrWhiteSpace(Archive.GameName))
                    errors.Add("The archive campaign identity is invalid.");
                if (Archive.Area == null || !BlueprintGuid.IsMatch(Archive.Area)) errors.Add("The archive area is invalid.");
            }
            if (KmcBlueprintGuids == null || KmcBlueprintGuids.Length == 0 || KmcBlueprintGuids.Any(g => g == null || !BlueprintGuid.IsMatch(g)))
                errors.Add("KMC blueprint guids are missing or malformed.");
            if (MammothBlueprintGuid == null || !BlueprintGuid.IsMatch(MammothBlueprintGuid)) errors.Add("Mammoth blueprint guid is malformed.");
            if (Candidate == null || Candidate.Commit == null || !Regex.IsMatch(Candidate.Commit, "^[0-9a-f]{40}$") ||
                string.IsNullOrWhiteSpace(Candidate.ProductVersion) || Candidate.DllSha256 == null || !Sha.IsMatch(Candidate.DllSha256) ||
                string.IsNullOrWhiteSpace(Candidate.DllMvid))
                errors.Add("The candidate identity is incomplete.");
            if (Observer == null || Observer.Version != ObserverIdentity.ProductVersion || Observer.PackageSha256 == null || !Sha.IsMatch(Observer.PackageSha256) ||
                Observer.DllSha256 == null || !Sha.IsMatch(Observer.DllSha256) || string.IsNullOrWhiteSpace(Observer.DllMvid))
                errors.Add("The observer package identity is incomplete or is not this observer version.");
            if (TimeoutSeconds < 60 || TimeoutSeconds > 900) errors.Add("Timeout is outside 60..900 seconds.");
            return errors;
        }
    }
}
