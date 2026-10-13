using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text.RegularExpressions;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class PersistenceSaveAuthorization
    {
        private readonly RuntimeRequest ownedPairsRequest;
        private readonly object ownedPairsGame, ownedPairsManager;
        private readonly string ownedPairsKey, ownedPairsExpectedNativeName;
        private object ownedPairsSourceDescriptor;
        private int ownedPairsNumber, ownedPairsReservations;
        private string ownedPairsOutputLeaf, ownedPairsOutputName, ownedPairsOutputType;
        private string ownedPairsReplacementLeaf;
        private object ownedPairsSeedDescriptor;
        internal string OwnedPairsReplacementLeaf { get { lock (sync) return ownedPairsReplacementLeaf; } }
        private bool OwnedPairsWorkerReplacement => ownedPairsRequest?.OwnedPairs?.Case == "persistence-save" &&
            ownedPairsRequest.PersistenceCase == "multi-owned-worker-cancel-manual" && maximumNativeWriteRequests == 2;
        private bool OwnedPairsReplacementReady(RuntimeSaveTarget target) => OwnedPairsWorkerReplacement &&
            ownedPairsReplacementLeaf != null && ownedPairsSeedDescriptor != null && target?.FileName == ownedPairsOutputLeaf &&
            MatchesOwnedPairsPlan(true, false);
        internal int OwnedPairsReservationCount { get { lock (sync) return ownedPairsReservations; } }
        internal int OwnedPairsOutputNumber { get { lock (sync) return ownedPairsNumber; } }
        internal string OwnedPairsOutputLeaf { get { lock (sync) return ownedPairsOutputLeaf; } }
        internal string OwnedPairsOutputName { get { lock (sync) return ownedPairsOutputName; } }
        internal string OwnedPairsOutputType { get { lock (sync) return ownedPairsOutputType; } }
        internal object OwnedPairsSourceDescriptor { get { lock (sync) return ownedPairsSourceDescriptor; } }
        private static string OwnedPairsKey(RuntimeRequest value)
        {
            var selected = value?.OwnedPairs; var source = value?.PersistenceLoad;
            return string.Join("\n", value?.RunId, value?.Scenario, value?.EvidenceRoot, value?.Commit, value?.DllSha256,
                value?.DllMvid, value?.TransactionToken, value?.PersistenceCase, selected?.Case, selected?.SourceRunId,
                selected?.SourceArtifactSha256, selected?.SourceArchiveSha256, selected?.CheckpointSourceRunId,
                selected?.CheckpointSourceArtifactSha256, selected?.RiderAId, selected?.MountAId, selected?.RiderBId, selected?.MountBId,
                selected?.PartyIds == null ? null : string.Join(",", selected.PartyIds), selected?.BaseGameOnly.ToString(),
                selected?.ExpectedNativeLoadCount.ToString(), selected?.ExpectedNativeWriteCount.ToString(), source?.FileName,
                source?.InternalName, source?.GameId, source?.GameName, source?.Area, source?.Sha256, source?.Length.ToString(),
                source?.LastWriteTimeUtcTicks.ToString(), value?.PersistenceReplayTargets == null ? null :
                string.Join(";", value.PersistenceReplayTargets.Select(target => target == null ? null :
                    string.Join(",", target.EnterPoint, target.Area, target.AutoSaveMode))));
        }
        private bool OwnedPairsUnchanged() => ownedPairsRequest == null ||
            OwnedPairsKey(ownedPairsRequest) == ownedPairsKey && ownedPairsRequest.Validate().Count == 0;
        private bool OwnedPairsWriteReady => ownedPairsRequest != null && ownedPairsReservations == 1 && admittedNativeLoadRequests == 1;
        internal bool MatchesOwnedPairsPlan(bool requireReserved, bool requireUnwritten)
        {
            lock (sync)
            {
                var source = ownedPairsRequest?.PersistenceLoad;
                Entry input;
                if (source == null || !OwnedPairsUnchanged() || maximumNativeLoadRequests != ownedPairsRequest.OwnedPairs.LoadCount(ownedPairsRequest.PersistenceCase) ||
                    maximumNativeWriteRequests != ownedPairsRequest.OwnedPairs.WriteCount(ownedPairsRequest.PersistenceCase) || declaresBootstrap || bootstrapWindowOpen ||
                    gameId != source.GameId || gameName != source.GameName || !entries.TryGetValue(source.FileName, out input) ||
                    input.Name != source.InternalName || input.Type != ownedPairsRequest.ExpectedNativeLoadType || input.Area != source.Area ||
                    input.Hash != source.Sha256 || input.Writable || input.Writing || input.Bootstrap || input.AdmitsBeforeArea ||
                    input.AdmissionArea != null || input.ForeignGameId != null || input.ForeignGameName != null ||
                    admittedNativeLoadRequests > maximumNativeLoadRequests.Value || admittedNativeWriteRequests > maximumNativeWriteRequests.Value) return false;
                if (maximumNativeWriteRequests == 0)
                    return ownedPairsReservations == 0 && entries.Count == 1 && admittedNativeWriteRequests == 0;
                if (ownedPairsReservations == 0)
                    return !requireReserved && entries.Count == 1 && admittedNativeLoadRequests == 0 && admittedNativeWriteRequests == 0;
                Entry output;
                if (ownedPairsReservations != 1 || ownedPairsSourceDescriptor == null || ownedPairsNumber <= 0 || entries.Count != (ownedPairsReplacementLeaf == null ? 2 : 3) ||
                    !entries.TryGetValue(ownedPairsOutputLeaf, out output) || output.Name != ownedPairsOutputName || output.Type != ownedPairsOutputType ||
                    output.Area != source.Area || !output.Writable || output.Bootstrap || output.AdmitsBeforeArea || output.AdmissionArea != null ||
                    output.ForeignGameId != null || output.ForeignGameName != null) return false;
                if (ownedPairsReplacementLeaf != null)
                {
                    Entry stage;
                    if (!OwnedPairsWorkerReplacement || ownedPairsSeedDescriptor == null || requireUnwritten ||
                        admittedNativeWriteRequests < 1 || !entries.TryGetValue(ownedPairsReplacementLeaf, out stage) ||
                        stage.Name != output.Name || stage.Type != output.Type || stage.Area != output.Area || !stage.Writable ||
                        stage.Bootstrap || stage.AdmitsBeforeArea || stage.AdmissionArea != null || stage.ForeignGameId != null || stage.ForeignGameName != null || output.Hash == null) return false;
                }
                return requireUnwritten ? output.Hash == null && !output.Writing && admittedNativeWriteRequests == 0 :
                    output.Hash == null && !output.Writing || admittedNativeWriteRequests >= 1;
            }
        }
        internal void ReserveOwnedPairsReplacement(RuntimeRequest request, object nativeGame, object nativeManager,
            object nativeSeed, RuntimeSaveTarget seed, int number)
        {
            lock (sync)
            {
                if (!OwnedPairsWorkerReplacement || !ReferenceEquals(request, ownedPairsRequest) ||
                    !ReferenceEquals(nativeGame, ownedPairsGame) || !ReferenceEquals(nativeManager, ownedPairsManager) ||
                    nativeSeed == null || ownedPairsReplacementLeaf != null || admittedNativeLoadRequests != 1 || admittedNativeWriteRequests != 1 ||
                    !MatchesOwnedPairsPlan(true, false) || seed?.FileName != ownedPairsOutputLeaf || ValidateArchiveIdentity(seed, Root) != null ||
                    ownedPairsNumber == int.MaxValue || number != ownedPairsNumber + 1)
                    throw new InvalidOperationException("Owned-pair replacement requires the exact completed seed and next native Manual number.");
                var output = entries[ownedPairsOutputLeaf];
                if (output.Hash == null || output.Writing || output.Type != "Manual")
                    throw new InvalidOperationException("The native seed is not a completed run-owned Manual archive.");
                var expected = new[] { Path.Combine(Root, request.PersistenceLoad.FileName), Path.Combine(Root, ownedPairsOutputLeaf) };
                var paths = Directory.EnumerateFileSystemEntries(Root).ToArray();
                if (paths.Length != 2 || paths.Any(p => !expected.Contains(p, StringComparer.Ordinal) || Directory.Exists(p)))
                    throw new InvalidOperationException("Isolated native inventory changed before replacement reservation.");
                VerifyFile(request.PersistenceLoad.FileName, entries[request.PersistenceLoad.FileName]);
                VerifyFile(ownedPairsOutputLeaf, output);
                var leaf = "Manual_" + number.ToString(CultureInfo.InvariantCulture) + "_" + ownedPairsOutputName + ".zks";
                var stage = new Entry { Name = output.Name, Type = output.Type, Area = output.Area, Writable = true };
                VerifyFile(leaf, stage);
                entries.Add(leaf, stage); ownedPairsSeedDescriptor = nativeSeed; ownedPairsReplacementLeaf = leaf;
            }
        }
        internal void ReserveOwnedPairsOutput(RuntimeRequest request, object nativeGame, object nativeManager, object nativeDescriptor,
            RuntimeSaveTarget source, int number, string type, string nativeName)
        {
            lock (sync)
            {
                if (!ReferenceEquals(request, ownedPairsRequest) || !ReferenceEquals(nativeGame, ownedPairsGame) ||
                    !ReferenceEquals(nativeManager, ownedPairsManager) || nativeDescriptor == null || (maximumNativeWriteRequests != 1 && !OwnedPairsWorkerReplacement) ||
                    ownedPairsReservations != 0 || !MatchesOwnedPairsPlan(false, true) || ValidateArchiveIdentity(source, Root) != null)
                    throw new InvalidOperationException("Owned-pair output reservation lacks exact unchanged request/world/native source custody.");
                var selectedType = request.OwnedPairs.Cold ? "Manual" : request.PersistenceCase == "multi-exploration-quick" ? "Quick" :
                    request.PersistenceCase == "multi-exploration-auto" ? "Auto" : "Manual";
                var selectedName = request.OwnedPairs.Cold ? "KMC_C7_SCHEMA2" : "KMC_C7_WORLD";
                if (number <= 0 || type != selectedType || nativeName != ownedPairsExpectedNativeName || string.IsNullOrWhiteSpace(nativeName) || nativeName.Length > 256 || nativeName.Any(char.IsControl) ||
                    type == "Manual" && nativeName != selectedName || type != "Manual" && number != 1)
                    throw new InvalidOperationException("Owned-pair native category/name/number differs from its closed selected case.");
                if (type == "Manual")
                {
                    var match = Regex.Match(request.PersistenceLoad.FileName, "^Manual_([1-9][0-9]*)_[A-Za-z0-9_]+\\.zks$");
                    if (!match.Success || !int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var prior) ||
                        prior == int.MaxValue || number != prior + 1)
                        throw new InvalidOperationException("Observed isolated native Manual numbering differs from the sole source descriptor.");
                }
                var paths = Directory.EnumerateFileSystemEntries(Root).ToArray();
                if (paths.Length != 1 || paths[0] != Path.Combine(Root, request.PersistenceLoad.FileName) || Directory.Exists(paths[0]))
                    throw new InvalidOperationException("Owned-pair isolated native inventory changed before reservation.");
                var leaf = type + "_" + number.ToString(CultureInfo.InvariantCulture) + (type == "Manual" ? "_" + nativeName : "") + ".zks";
                var output = new Entry { Name = nativeName, Type = type, Area = request.PersistenceLoad.Area, Writable = true };
                VerifyFile(leaf, output);
                entries.Add(leaf, output); ownedPairsSourceDescriptor = nativeDescriptor; ownedPairsNumber = number;
                ownedPairsOutputLeaf = leaf; ownedPairsOutputName = nativeName; ownedPairsOutputType = type; ownedPairsReservations = 1;
            }
        }
    }
}
