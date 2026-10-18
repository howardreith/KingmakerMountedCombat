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
                value?.DllMvid, value?.TransactionToken, value?.PersistenceCase, value?.Schema1DerivationSha256, selected?.Case, selected?.SourceRunId,
                selected?.SourceArtifactSha256, selected?.SourceArchiveSha256, selected?.SourceArea,
                value?.PersistenceAreaTarget?.EnterPoint, value?.PersistenceAreaTarget?.Area, value?.PersistenceAreaTarget?.AutoSaveMode, selected?.CheckpointSourceRunId,
                selected?.CheckpointSourceArtifactSha256, selected?.RiderAId, selected?.MountAId, selected?.RiderBId, selected?.MountBId,
                selected?.PartyIds == null ? null : string.Join(",", selected.PartyIds), selected?.BaseGameOnly.ToString(),
                selected?.ExpectedNativeLoadCount.ToString(), selected?.ExpectedNativeWriteCount.ToString(), source?.FileName,
                source?.InternalName, source?.GameId, source?.GameName, source?.Area, source?.Sha256, source?.Length.ToString(),
                source?.LastWriteTimeUtcTicks.ToString(), value?.PersistenceReplayTargets == null ? null :
                string.Join(";", value.PersistenceReplayTargets.Select(target => target == null ? null :
                    string.Join(",", target.EnterPoint, target.Area, target.AutoSaveMode))))
                // Owner-seed pins extend the captured key only for the seed-pinned kinds; historical keys are unchanged.
                + (selected?.OwnerSeedSetup == true || selected?.OwnerSeedDerivedLineage == true ? "\n" + string.Join("\n", selected.SeedId, selected.SeedSha256,
                    selected.XpAssistance?.UnitId, selected.XpAssistance?.TargetCharacterLevel.ToString()) : "")
                + (selected?.IsOwnerSeedDerivedCombat == true ? "\ncompanion:" + selected.CompanionId : "");
        }
        private bool OwnedPairsUnchanged() => ownedPairsRequest == null ||
            OwnedPairsKey(ownedPairsRequest) == ownedPairsKey && ownedPairsRequest.Validate().Count == 0;
        private bool OwnedPairsWriteReady => OwnerSeedPlan ? ownerSeedPhase == "levelup" && admittedNativeLoadRequests == 1 :
            ownedPairsRequest != null && ownedPairsReservations == 1 && admittedNativeLoadRequests == 1;
        // Owner-seed SETUP: each native level-up Auto the ordinary build requests is
        // reserved one at a time into the run root (Auto_1, then an Auto_2 staging copy
        // that the native worker moves onto Auto_1), confirmed on completion, and the
        // whole write scope closes at SETUP-COMPLETE. No other write is ever admitted.
        internal const string OwnerSeedAutoLeaf = Chunk7OrdinaryAcquisitionSelection.AutoLeaf, OwnerSeedStagingLeaf = Chunk7AssistedFixtureSelection.LevelupStagingLeaf;
        private string ownerSeedPhase, ownerSeedAutoName;
        private int ownerSeedConfirmedWrites;
        private bool ownerSeedClosed;
        private bool OwnerSeedPlan => ownedPairsRequest?.OwnedPairs?.IsOwnerSeed == true;
        internal int OwnerSeedConfirmedWrites { get { lock (sync) return ownerSeedConfirmedWrites; } }
        internal bool OwnerSeedWritesClosed { get { lock (sync) return ownerSeedClosed; } }
        internal string OwnerSeedPhase { get { lock (sync) return ownerSeedPhase; } }
        private bool MatchesOwnerSeedWrites(bool requireUnwritten)
        {
            var source = ownedPairsRequest.PersistenceLoad;
            if (requireUnwritten) return ownerSeedPhase == null && ownerSeedConfirmedWrites == 0 && admittedNativeWriteRequests == 0 && entries.Count == 1;
            if (ownerSeedConfirmedWrites > maximumNativeWriteRequests.Value) return false;
            var pending = ownerSeedPhase == "levelup" ? 1 : 0;
            if (admittedNativeWriteRequests < ownerSeedConfirmedWrites || admittedNativeWriteRequests > ownerSeedConfirmedWrites + pending) return false;
            foreach (var pair in entries)
            {
                if (pair.Key == source.FileName) continue;
                var e = pair.Value;
                if (pair.Key != OwnerSeedAutoLeaf && pair.Key != OwnerSeedStagingLeaf || e.Type != "Auto" || e.Name != ownerSeedAutoName ||
                    e.Area != source.Area || e.Bootstrap || e.AdmitsBeforeArea || e.AdmissionArea != null || e.ForeignGameId != null || e.ForeignGameName != null) return false;
                if (ownerSeedClosed && (e.Writable || e.Writing)) return false;
            }
            return !ownerSeedClosed || ownerSeedPhase == "closed" && ownerSeedConfirmedWrites == maximumNativeWriteRequests.Value && admittedNativeWriteRequests == ownerSeedConfirmedWrites;
        }
        private void RequireOwnerSeedContext(RuntimeRequest request, object nativeGame, object nativeManager)
        {
            if (!OwnerSeedPlan || !ReferenceEquals(request, ownedPairsRequest) || !ReferenceEquals(nativeGame, ownedPairsGame) ||
                !ReferenceEquals(nativeManager, ownedPairsManager) || !OwnedPairsUnchanged() || admittedNativeLoadRequests != 1)
                throw new InvalidOperationException("Owner-seed setup write lost its exact captured request, native objects or completed load.");
        }
        private void RequireOwnerSeedInventory()
        {
            var expected = ownerSeedConfirmedWrites == 0 ? new[] { ownedPairsRequest.PersistenceLoad.FileName } : new[] { ownedPairsRequest.PersistenceLoad.FileName, OwnerSeedAutoLeaf };
            var paths = Directory.EnumerateFileSystemEntries(Root).ToArray();
            if (paths.Length != expected.Length || paths.Any(path => Directory.Exists(path) || !expected.Contains(Path.GetFileName(path))))
                throw new InvalidOperationException("Owner-seed isolated inventory contains a foreign, pending or missing leaf.");
            foreach (var leaf in expected) VerifyFile(leaf, entries[leaf]);
        }
        internal void ReserveOwnerSeedLevelup(RuntimeRequest request, object nativeGame, object nativeManager, string nativeAutoName)
        {
            lock (sync)
            {
                RequireOwnerSeedContext(request, nativeGame, nativeManager);
                Required(nativeAutoName, 256);
                if (ownerSeedClosed || ownerSeedPhase == "levelup" || ownerSeedConfirmedWrites >= maximumNativeWriteRequests.Value ||
                    admittedNativeWriteRequests != ownerSeedConfirmedWrites || ownerSeedConfirmedWrites > 0 && nativeAutoName != ownerSeedAutoName ||
                    entries.Values.Any(value => value.Writable || value.Writing))
                    throw new InvalidOperationException("Owner-seed level-up reservation is repeated, out of order or beyond its exact selected count.");
                RequireOwnerSeedInventory();
                var source = ownedPairsRequest.PersistenceLoad;
                if (ownerSeedConfirmedWrites == 0)
                {
                    if (entries.Count != 1 || entries.ContainsKey(OwnerSeedAutoLeaf) || entries.ContainsKey(OwnerSeedStagingLeaf))
                        throw new InvalidOperationException("Owner-seed first level-up Auto aliases an admitted leaf.");
                    var first = new Entry { Name = nativeAutoName, Type = "Auto", Area = source.Area, Writable = true };
                    VerifyFile(OwnerSeedAutoLeaf, first); VerifyFile(OwnerSeedStagingLeaf, new Entry());
                    entries.Add(OwnerSeedAutoLeaf, first);
                }
                else
                {
                    if (!entries.TryGetValue(OwnerSeedAutoLeaf, out var settled) || settled.Hash == null)
                        throw new InvalidOperationException("Owner-seed replacement requires the completed first level-up Auto.");
                    var staging = new Entry { Name = nativeAutoName, Type = "Auto", Area = source.Area, Writable = true };
                    VerifyFile(OwnerSeedStagingLeaf, staging);
                    entries[OwnerSeedStagingLeaf] = staging; settled.Writable = true;
                }
                ownerSeedAutoName = nativeAutoName; ownerSeedPhase = "levelup";
            }
        }
        internal void ConfirmOwnerSeedLevelup(RuntimeRequest request, object nativeGame, object nativeManager, RuntimeSaveTarget settled)
        {
            lock (sync)
            {
                RequireOwnerSeedContext(request, nativeGame, nativeManager);
                Entry staging;
                entries.TryGetValue(OwnerSeedStagingLeaf, out staging);
                if (ownerSeedPhase != "levelup" || admittedNativeWriteRequests != ownerSeedConfirmedWrites + 1 || settled?.FileName != OwnerSeedAutoLeaf ||
                    settled.SaveType != "Auto" || settled.InternalName != ownerSeedAutoName || ValidateArchiveIdentity(settled, Root) != null ||
                    staging != null && (staging.Hash != null || staging.Writing) || entries[OwnerSeedAutoLeaf].Writing || entries[OwnerSeedAutoLeaf].Hash == null)
                    throw new InvalidOperationException("Owner-seed level-up Auto has not completed exactly at its run-owned leaf.");
                entries[OwnerSeedAutoLeaf].Writable = false; if (staging != null) staging.Writable = false;
                ownerSeedConfirmedWrites++; ownerSeedPhase = "idle"; RequireOwnerSeedInventory();
            }
        }
        internal void CloseOwnerSeedSetupWrites(RuntimeRequest request, object nativeGame, object nativeManager)
        {
            lock (sync)
            {
                RequireOwnerSeedContext(request, nativeGame, nativeManager);
                if (ownerSeedClosed || ownerSeedPhase == "levelup" || ownerSeedConfirmedWrites != maximumNativeWriteRequests.Value ||
                    admittedNativeWriteRequests != ownerSeedConfirmedWrites || entries.Values.Any(value => value.Writable || value.Writing))
                    throw new InvalidOperationException("SETUP-COMPLETE requires exactly the selected completed setup level-up Autos and no open writer.");
                RequireOwnerSeedInventory(); ownerSeedClosed = true; ownerSeedPhase = "closed";
            }
        }
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
                if (OwnerSeedPlan) return MatchesOwnerSeedWrites(requireUnwritten);
                if (maximumNativeWriteRequests == 0)
                    return ownedPairsReservations == 0 && entries.Count == 1 && admittedNativeWriteRequests == 0;
                if (ownedPairsReservations == 0)
                    return !requireReserved && entries.Count == 1 && admittedNativeLoadRequests == 0 && admittedNativeWriteRequests == 0;
                Entry output;
                if (ownedPairsReservations != 1 || ownedPairsSourceDescriptor == null || ownedPairsNumber <= 0 || entries.Count != (ownedPairsReplacementLeaf == null ? 2 : 3) ||
                    !entries.TryGetValue(ownedPairsOutputLeaf, out output) || output.Name != ownedPairsOutputName || output.Type != ownedPairsOutputType ||
                    output.Area != ownedPairsRequest.ExpectedChunk7ArchiveArea || !output.Writable || output.Bootstrap || output.AdmitsBeforeArea || output.AdmissionArea != null ||
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
                    request.PersistenceCase == "multi-exploration-auto" || request.PersistenceCase == "multi-area-autosave" ? "Auto" : "Manual";
                var selectedName = request.OwnedPairs.IsOwnerSeedManual ? Chunk7OwnedPairsSelection.OwnerSeedManualName : request.OwnedPairs.Cold ? (Chunk7OwnedPairsSelection.OwnedSchema1Cold(request.PersistenceCase) ? "KMC_C7_SCHEMA1" : "KMC_C7_SCHEMA2") : "KMC_C7_WORLD";
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
                var output = new Entry { Name = nativeName, Type = type, Area = request.ExpectedChunk7ArchiveArea, Writable = true };
                VerifyFile(leaf, output);
                entries.Add(leaf, output); ownedPairsSourceDescriptor = nativeDescriptor; ownedPairsNumber = number;
                ownedPairsOutputLeaf = leaf; ownedPairsOutputName = nativeName; ownedPairsOutputType = type; ownedPairsReservations = 1;
            }
        }
    }
}
