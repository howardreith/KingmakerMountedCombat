using System;
using System.IO;
using System.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class PersistenceSaveAuthorization
    {
        private RuntimeRequest progressionManualRequest;
        private object progressionManualGame, progressionManualManager;
        private string progressionManualKey;
        private int progressionManualNumber, progressionManualReservations;

        internal int ProgressionManualNumber { get { lock (sync) return progressionManualNumber; } }
        internal int ProgressionManualReservations { get { lock (sync) return progressionManualReservations; } }
        internal string ProgressionManualLeaf { get { lock (sync) return progressionManualReservations == 1
            ? Chunk7ProgressionSelection.ManualLeaf(progressionManualNumber) : null; } }

        private static string ProgressionManualKey(RuntimeRequest request)
        {
            if (request?.Scenario != Chunk7ProgressionSelection.Scenario || request.Progression?.WritesManual != true ||
                request.PersistenceLoad == null || request.Validate().Count != 0) return null;
            var source = request.PersistenceLoad;
            var selected = request.Progression;
            var owned = selected.OwnedSource;
            return string.Join("\n", request.RunId, request.Scenario, request.Commit, request.DllSha256,
                request.DllMvid, request.TransactionToken, request.EvidenceRoot, source.FileName,
                source.InternalName, source.GameId, source.GameName, source.Area, source.Sha256,
                source.Length.ToString(System.Globalization.CultureInfo.InvariantCulture),
                source.LastWriteTimeUtcTicks.ToString(System.Globalization.CultureInfo.InvariantCulture),
                selected.Case, selected.SourceRunId, selected.SourceArtifactSha256, selected.SourceArchiveSha256,
                selected.InventoryRunId, selected.InventoryArtifactSha256, selected.ActorId, selected.PetId,
                selected.ControlActorId, selected.ControlPetId, selected.RaceGuid, selected.PresetGuid,
                selected.SkeletonName, selected.Gender, selected.LeftHanded?.ToString(), selected.BodySize,
                selected.MinimumRangerLevel.ToString(System.Globalization.CultureInfo.InvariantCulture),
                selected.ExpectedNativeWriteCount.ToString(System.Globalization.CultureInfo.InvariantCulture), selected.NativeWriteLeaf,
                owned?.Case, owned?.SourceRunId, owned?.SourceArtifactSha256, owned?.SourceArchiveSha256,
                owned?.CheckpointSourceRunId, owned?.CheckpointSourceArtifactSha256,
                owned?.RiderAId, owned?.MountAId, owned?.RiderBId, owned?.MountBId,
                owned?.PartyIds == null ? null : string.Join(",", owned.PartyIds), owned?.BaseGameOnly.ToString(),
                owned?.ExpectedNativeLoadCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                owned?.ExpectedNativeWriteCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        private bool ProgressionManualUnchanged() => progressionManualRequest == null ||
            progressionManualKey != null && progressionManualKey == ProgressionManualKey(progressionManualRequest);

        internal void ConfigureProgressionManual(RuntimeRequest request, object game, object manager)
        {
            lock (sync)
            {
                if (progressionManualRequest != null || game == null || manager == null ||
                    ProgressionManualKey(request) == null || maximumNativeLoadRequests != 1 || maximumNativeWriteRequests != 1 ||
                    admittedNativeLoadRequests != 0 || admittedNativeWriteRequests != 0 || entries.Count != 1 ||
                    declaresBootstrap || bootstrapWindowOpen || acquiredManualRequest != null || ownedPairsRequest != null)
                    throw new InvalidOperationException("Owned progression Manual requires one immutable source and an empty one-write reservation.");
                var source = request.PersistenceLoad;
                if (!entries.TryGetValue(source.FileName, out var input) || input.Name != source.InternalName ||
                    input.Type != "Manual" || input.Area != source.Area || input.Hash != source.Sha256 ||
                    input.Writable || input.Writing || input.Bootstrap || input.AdmitsBeforeArea ||
                    input.AdmissionArea != null || input.ForeignGameId != null || input.ForeignGameName != null ||
                    source.GameId != gameId || source.GameName != gameName)
                    throw new InvalidOperationException("Owned progression Manual source differs from its immutable allowlist entry.");
                progressionManualRequest = request; progressionManualGame = game; progressionManualManager = manager;
                progressionManualKey = ProgressionManualKey(request);
            }
        }

        internal bool MatchesProgressionManualPlan(RuntimeRequest request, object game, object manager)
        {
            lock (sync)
            {
                if (!ReferenceEquals(request, progressionManualRequest) || !ReferenceEquals(game, progressionManualGame) ||
                    !ReferenceEquals(manager, progressionManualManager) || !ProgressionManualUnchanged() ||
                    maximumNativeLoadRequests != 1 || maximumNativeWriteRequests != 1 ||
                    admittedNativeLoadRequests > 1 || admittedNativeWriteRequests > 1 ||
                    entries.Count != 1 + progressionManualReservations || progressionManualReservations > 1)
                    return false;
                var source = request.PersistenceLoad;
                if (!entries.TryGetValue(source.FileName, out var input) || input.Name != source.InternalName ||
                    input.Type != "Manual" || input.Area != source.Area || input.Hash != source.Sha256 ||
                    input.Writable || input.Writing || input.Bootstrap || input.AdmitsBeforeArea ||
                    input.AdmissionArea != null || input.ForeignGameId != null || input.ForeignGameName != null ||
                    source.GameId != gameId || source.GameName != gameName) return false;
                if (progressionManualReservations == 0) return admittedNativeWriteRequests == 0;
                return entries.TryGetValue(Chunk7ProgressionSelection.ManualLeaf(progressionManualNumber), out var output) &&
                    output.Name == Chunk7ProgressionSelection.ManualOutputName && output.Type == "Manual" &&
                    output.Area == source.Area && output.Writable && !output.Bootstrap && !output.AdmitsBeforeArea &&
                    output.AdmissionArea == null && output.ForeignGameId == null && output.ForeignGameName == null &&
                    (output.Hash == null && !output.Writing || admittedNativeWriteRequests == 1);
            }
        }

        internal void ReserveProgressionManual(RuntimeRequest request, object game, object manager, int observedNumber)
        {
            lock (sync)
            {
                if (!MatchesProgressionManualPlan(request, game, manager) || progressionManualReservations != 0 ||
                    admittedNativeLoadRequests != 1 || admittedNativeWriteRequests != 0 || observedNumber <= 0 ||
                    observedNumber == int.MaxValue)
                    throw new InvalidOperationException("Owned progression Manual reservation lacks its exact loaded source and unused write quota.");
                var paths = Directory.EnumerateFileSystemEntries(Root).ToArray();
                if (paths.Length != 1 || paths[0] != Path.Combine(Root, request.PersistenceLoad.FileName) || Directory.Exists(paths[0]))
                    throw new InvalidOperationException("Owned progression Manual isolated source inventory changed.");
                var leaf = Chunk7ProgressionSelection.ManualLeaf(observedNumber);
                var output = new Entry { Name = Chunk7ProgressionSelection.ManualOutputName,
                    Type = "Manual", Area = request.PersistenceLoad.Area, Writable = true };
                VerifyFile(leaf, output);
                entries.Add(leaf, output); progressionManualNumber = observedNumber; progressionManualReservations = 1;
            }
        }
    }
}
