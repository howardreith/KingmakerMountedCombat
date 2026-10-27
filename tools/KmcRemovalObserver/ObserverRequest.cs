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

    // Schema 3 only: the request-free normal-player control. KMC is present and
    // receives no automation request; these are the exact roles the observer
    // drives through ordinary native input on the declared disposable fixture.
    public sealed class ObserverNormalPlayer
    {
        [JsonProperty("riderId")] public string RiderId { get; set; }
        [JsonProperty("mountId")] public string MountId { get; set; }
        [JsonProperty("neighborRiderId")] public string NeighborRiderId { get; set; }
        [JsonProperty("neighborMountId")] public string NeighborMountId { get; set; }
        [JsonProperty("mountAbilityGuid")] public string MountAbilityGuid { get; set; }
        [JsonProperty("dismountAbilityGuid")] public string DismountAbilityGuid { get; set; }
        [JsonProperty("kmcPackageSha256")] public string KmcPackageSha256 { get; set; }
        [JsonProperty("idleWindowFrames")] public int IdleWindowFrames { get; set; }
        // "controls" drives the rider on the six-Horse fixture; "previous-package-load"
        // (F12 upgrade rehearsal) only loads the reference-free owner seed under the
        // released previous package and observes a quiet ordinary start.
        [JsonProperty("mode")] public string Mode { get; set; }
        internal bool PreviousPackageLoad => Mode == "previous-package-load";
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
        [JsonProperty("normalPlayer")] public ObserverNormalPlayer NormalPlayer { get; set; }

        private static readonly Regex Sha = new Regex("^[0-9a-f]{64}$");
        private static readonly Regex BlueprintGuid = new Regex("^[0-9a-f]{32}$");
        private static readonly Regex RunIdShape = new Regex("^[A-Za-z0-9._-]{1,120}$");

        internal bool IsNormalPlayer => SchemaVersion == 3;

        public List<string> Validate()
        {
            if (SchemaVersion == 3 || NormalPlayer != null) return ValidateNormalPlayer();
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
                var ownerStockCleanup = Archive.FileName == "Manual_306_KMC_CLEANUP.zks";
                if (Archive.InternalName != "KMC_CLEANUP" ||
                    (!ownerStockCleanup && Archive.FileName != "Manual_301_KMC_CLEANUP.zks"))
                    errors.Add("The archive is not the prepared cleanup save.");
                // batch65's exact cleanup Manual, or (7E run plan gate G7 (a)) the same candidate's settled final r31
                // cleanup Manual, whose provenance the runner's source gate proves; the campaign stays exact.
                var batch65Cleanup = Archive.Sha256 == "2c46c2700f117b3f0d50f688d2c89bd879de79d4b0b40eaf3b2502c1015bdfc2";
                if (ownerStockCleanup && ((batch65Cleanup ? Archive.Length != 1263594 : Archive.Length > 256L * 1024 * 1024) ||
                    Archive.Sha256 == "e475d49c3590927c7f216d301aa8535f6dbbdc7af2066f4c01f58fff335866fa" ||
                    Archive.GameId != "3f7381f7-aba6-43bf-bae6-39c6a66a7180" ||
                    Archive.GameName != "Testy the Modless" || Archive.Area != "b10e86516b6437d45a629931c30936fe"))
                    errors.Add("The owner stock cleanup archive differs from the accepted native Manual.");
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

        // Closed: only the cold-qualified unmounted six-Horse fixture Manual308, its
        // exact roles, the native Mount/Dismount blueprints and both package identities.
        private List<string> ValidateNormalPlayer()
        {
            var errors = new List<string>();
            if (SchemaVersion != 3 || NormalPlayer == null || ChargeSource != null) errors.Add("Normal-player requests are exactly schema 3 with their own section and no charge source.");
            if (RunId == null || !RunIdShape.IsMatch(RunId)) errors.Add("Run id is invalid.");
            if (TransactionToken == null || !Sha.IsMatch(TransactionToken)) errors.Add("Transaction token is invalid.");
            foreach (var pair in new[] { new KeyValuePair<string, string>("evidenceRoot", EvidenceRoot),
                new KeyValuePair<string, string>("profileRoot", ProfileRoot), new KeyValuePair<string, string>("modsRoot", ModsRoot) })
                if (string.IsNullOrWhiteSpace(pair.Value) || !System.IO.Path.IsPathRooted(pair.Value)) errors.Add(pair.Key + " must be an absolute path.");
            if (KmcModId != "KingmakerMountedCombat") errors.Add("The observed mod id is not the KMC mod id.");
            if (ObserverModId != "KmcRemovalObserver") errors.Add("The observer mod id is not this mod.");
            if (KmcHarmonyIds == null || KmcHarmonyIds.Length == 0 || KmcHarmonyIds.Any(string.IsNullOrWhiteSpace)) errors.Add("KMC Harmony ids are missing.");
            var previous = NormalPlayer != null && NormalPlayer.PreviousPackageLoad;
            if (NormalPlayer != null && NormalPlayer.Mode != "controls" && !previous) errors.Add("The normal-player mode is not one of the closed modes.");
            if (previous)
            {
                if (Archive == null || Archive.InternalName != "KMC C7 NO MODS OWNER SEED" || Archive.FileName != "Manual_303_KMC_C7_NO_MODS_OWNER_SEED.zks" ||
                    Archive.Sha256 != "3405d7c813668ad646788efd746a62c1b16e1438fad1bc4bcbebc961821b924f" || Archive.Length != 1239025 ||
                    Archive.GameId != "3f7381f7-aba6-43bf-bae6-39c6a66a7180" || Archive.GameName != "Testy the Modless" ||
                    Archive.Area != "b10e86516b6437d45a629931c30936fe")
                    errors.Add("The previous-package load is not the reference-free owner seed.");
                if (Candidate == null || Candidate.DllSha256 != "11e993093d2042f615cfc9d6aecc33d630d2e41e8125dd680656b6a1057a48ea" ||
                    Candidate.DllMvid != "0aa68ba0-f293-480e-8ecb-d7bdaeca73d0" || NormalPlayer.KmcPackageSha256 != "e89f431ae1303b217e987b2ff12171a05bac434d97809962c3596e2942874039")
                    errors.Add("The previous-package load is not bound to the released preview.211 package.");
            }
            else if (Archive == null || Archive.InternalName != "KMC_C7_SIX_HORSE_WORLD" || Archive.FileName != "Manual_308_KMC_C7_SIX_HORSE_WORLD.zks" ||
                Archive.Sha256 != "b1e817a60c8bbe35b5947b4eab3b3f35060493ca20873d5de5fdf8a81efbc291" || Archive.Length != 1332308 ||
                Archive.GameId != "3f7381f7-aba6-43bf-bae6-39c6a66a7180" || Archive.GameName != "Testy the Modless" ||
                Archive.Area != "b10e86516b6437d45a629931c30936fe")
                errors.Add("The archive is not the cold-qualified six-Horse fixture Manual308.");
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
            var n = NormalPlayer;
            if (n != null)
            {
                var roles = new[] { n.RiderId, n.MountId, n.NeighborRiderId, n.NeighborMountId };
                if (n.PreviousPackageLoad ? roles.Any(r => r != null) :
                    roles.Any(r => string.IsNullOrWhiteSpace(r) || r.Length > 120) || roles.Distinct(StringComparer.Ordinal).Count() != 4)
                    errors.Add("The four normal-player roles are missing, oversized or not distinct, or a load-only request names roles.");
                if (n.MountAbilityGuid != "f053faad986631688defa003cd7bda0e" || n.DismountAbilityGuid != "3af2b81f4d72bbb30501fa730fcdf36e")
                    errors.Add("Only the native KMC Mount and Dismount ability blueprints are driven.");
                if (n.KmcPackageSha256 == null || !Sha.IsMatch(n.KmcPackageSha256)) errors.Add("The KMC package identity is invalid.");
                if (n.IdleWindowFrames < 60 || n.IdleWindowFrames > 600) errors.Add("Idle windows are outside 60..600 frames.");
            }
            if (TimeoutSeconds < 60 || TimeoutSeconds > 900) errors.Add("Timeout is outside 60..900 seconds.");
            return errors;
        }
    }
}
