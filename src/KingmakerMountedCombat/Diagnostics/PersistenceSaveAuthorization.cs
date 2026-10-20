using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed class PersistenceSaveEntry
    {
        public string FileName { get; set; }
        public string InternalName { get; set; }
        public string SaveType { get; set; }
        public string Area { get; set; }
        // Verified installed behaviour: SaveRoutine 06008029 is an iterator, so
        // its admission boundary runs when LoadArea 06000CD5 synchronously
        // constructs the enumerator while the departure area is still loaded.
        // Execution, PrepareSave 06008025 and the committed header then follow
        // in the destination, so an authored after-entry autosave legitimately
        // spans two areas. Leave this null unless the run declares a transition;
        // the authority then refuses any pair that is not its exact endpoints.
        public string AdmissionArea { get; set; }
        public string InitialSha256 { get; set; }
        public bool Writable { get; set; }
        // null/"A": the validated fixture campaign. "B": a leaf of the single
        // disposable native new game this run may start; its identity is minted
        // by the engine at run time and frozen on the first admitted write.
        public string Campaign { get; set; }
        // Verified installed behaviour: Game.LoadNewGame 06000CDC constructs the
        // new game's autosave SaveRoutine (IL_0525) right after queueing the
        // area load, while no area is loaded at all, so its admission boundary
        // observes no current area. Only a bootstrap leaf may admit that, and it
        // still commits in the exact declared area.
        public bool AdmitsBeforeArea { get; set; }
        // A read-only leaf of ANOTHER native campaign whose identity the validated
        // request declares for it (the foreign-header derivative: campaign B's own
        // archive, loaded only to be refused by the KMC member it carries). Never
        // assigned here, never writable, never the bootstrap campaign.
        public string ForeignGameId { get; set; }
        public string ForeignGameName { get; set; }
    }

    // Test-only authority. The caller supplies the already authorized run directory,
    // never an arbitrary SavePath from an archive. All leaves are explicit; names
    // beginning with KMC confer no authority. Native integration must also guard
    // temporary archive creation, rotation/rename and cloud paths before activation.
    internal sealed partial class PersistenceSaveAuthorization
    {
        private sealed class Entry
        {
            internal string Name;
            internal string Type;
            internal string Area;
            internal string AdmissionArea;
            internal bool Writing;
            internal string Hash;
            internal bool Writable;
            // A leaf of the bootstrap campaign: the one native new game this run
            // may start, whose identity the engine mints.
            internal bool Bootstrap;
            internal bool AdmitsBeforeArea;
            // A declared read-only foreign campaign identity, or null for the fixture's.
            internal string ForeignGameId;
            internal string ForeignGameName;
            internal bool Admits(string area) =>
                area == Area || (AdmissionArea != null && area == AdmissionArea) ||
                (area == null && AdmitsBeforeArea);
        }

        private readonly object sync = new object();
        private readonly Dictionary<string, Entry> entries =
            new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
        private readonly string gameId;
        private readonly string gameName;
        private readonly string baselineHash;
        internal string Root { get; }
        private readonly int? maximumNativeWriteRequests;
        private readonly int? maximumNativeLoadRequests;
        private int admittedNativeWriteRequests, admittedNativeLoadRequests;
        internal int? MaximumNativeWriteRequests => maximumNativeWriteRequests;
        internal int? MaximumNativeLoadRequests => maximumNativeLoadRequests;
        internal int AdmittedNativeLoadRequests { get { lock (sync) return admittedNativeLoadRequests; } }
        internal int AdmittedNativeWriteRequests { get { lock (sync) return admittedNativeWriteRequests; } }

        private readonly RuntimeRequest acquiredManualRequest;
        private readonly object acquiredManualGame, acquiredManualManager;
        private readonly string acquiredManualRequestKey;
        private object acquiredManualSourceDescriptor;
        private int acquiredManualNumber, acquiredManualReservations;
        private int acquiredManualInitialEntries, acquiredManualInitialWritable, acquiredManualReservedEntries, acquiredManualReservedWritable;
        internal int AcquiredManualInitialEntries { get { lock (sync) return acquiredManualInitialEntries; } }
        internal int AcquiredManualInitialWritable { get { lock (sync) return acquiredManualInitialWritable; } }
        internal int AcquiredManualReservedEntries { get { lock (sync) return acquiredManualReservedEntries; } }
        internal int AcquiredManualReservedWritable { get { lock (sync) return acquiredManualReservedWritable; } }
        internal int AcquiredManualReservationCount { get { lock (sync) return acquiredManualReservations; } }
        internal int AcquiredManualNumber { get { lock (sync) return acquiredManualNumber; } }
        internal string AcquiredManualOutputLeaf { get { lock (sync) return acquiredManualReservations == 1 ? Chunk7AcquiredManualSelection.NativeOutputLeaf(acquiredManualNumber) : null; } }
        internal object AcquiredManualSourceDescriptor { get { lock (sync) return acquiredManualSourceDescriptor; } }
        private static string AcquiredManualRequestKey(RuntimeRequest request)
        {
            var selected = request?.AcquiredManual; var source = request?.PersistenceLoad;
            return string.Join("\n", request?.RunId, request?.Scenario, request?.EvidenceRoot, request?.Commit, request?.DllSha256,
                request?.DllMvid, request?.TransactionToken, selected?.Case, selected?.SourceRunId, selected?.SourceArtifactSha256,
                selected?.SourceArchiveSha256, selected?.ActorId, selected?.PetId, selected?.BaseGameOnly.ToString(),
                selected?.ExpectedNativeLoadCount.ToString(), selected?.ExpectedNativeWriteCount.ToString(), source?.FileName,
                source?.InternalName, source?.GameId, source?.GameName, source?.Area, source?.Sha256, source?.Length.ToString(),
                source?.LastWriteTimeUtcTicks.ToString());
        }
        private bool AcquiredManualRequestUnchanged() => acquiredManualRequest == null ||
            AcquiredManualRequestKey(acquiredManualRequest) == acquiredManualRequestKey && acquiredManualRequest.Validate().Count == 0;

        internal bool MatchesAcquiredManualPlan(RuntimeSaveDescriptor source, bool requireReserved, bool requireUnwritten)
        {
            lock (sync)
            {
                Entry input;
                if (acquiredManualRequest == null || !AcquiredManualRequestUnchanged() ||
                    source == null || source.FileName != "Auto_1.zks" || source.GameId != gameId || source.GameName != gameName ||
                    maximumNativeLoadRequests != 1 || maximumNativeWriteRequests != 1 || declaresBootstrap || bootstrapWindowOpen ||
                    !entries.TryGetValue(source.FileName, out input) || input.Name != source.InternalName || input.Type != "Auto" ||
                    input.Area != source.Area || input.Hash != source.Sha256 || input.Writable || input.Writing || input.Bootstrap ||
                    input.AdmitsBeforeArea || input.AdmissionArea != null || input.ForeignGameId != null || input.ForeignGameName != null ||
                    admittedNativeLoadRequests > 1 || admittedNativeWriteRequests > 1) return false;
                if (acquiredManualReservations == 0)
                    return !requireReserved && entries.Count == 1 && admittedNativeLoadRequests == 0 && admittedNativeWriteRequests == 0;
                Entry output;
                if (acquiredManualReservations != 1 || acquiredManualNumber <= 0 || acquiredManualSourceDescriptor == null || entries.Count != 2 ||
                    !entries.TryGetValue(Chunk7AcquiredManualSelection.NativeOutputLeaf(acquiredManualNumber), out output) ||
                    output.Name != Chunk7AcquiredManualSelection.OutputName || output.Type != "Manual" || output.Area != source.Area ||
                    !output.Writable || output.Bootstrap || output.AdmitsBeforeArea || output.AdmissionArea != null ||
                    output.ForeignGameId != null || output.ForeignGameName != null) return false;
                return requireUnwritten ? output.Hash == null && !output.Writing && admittedNativeWriteRequests == 0 :
                    (output.Hash == null && !output.Writing || admittedNativeWriteRequests == 1);
            }
        }
        // Called once after native enumeration of the isolated Auto-only root,
        // before its sole native load. No identity/counter/leaf changes on refusal.
        internal void ReserveAcquiredManualOutput(RuntimeRequest request, object nativeGame, object nativeManager,
            object nativeDescriptor, RuntimeSaveTarget observedSource, int observedNumber)
        {
            lock (sync)
            {
                if (!ReferenceEquals(request, acquiredManualRequest) || !ReferenceEquals(nativeGame, acquiredManualGame) ||
                    !ReferenceEquals(nativeManager, acquiredManualManager) || nativeDescriptor == null ||
                    acquiredManualReservations != 0 || !MatchesAcquiredManualPlan(request?.PersistenceLoad, false, true) ||
                    acquiredManualRequestKey != AcquiredManualRequestKey(request) || observedNumber <= 0 ||
                    ValidateArchiveIdentity(observedSource, Root) != null)
                    throw new InvalidOperationException("Acquired Manual reservation lacks its exact unconsumed request/world/native inventory.");
                var paths = Directory.EnumerateFileSystemEntries(Root).ToArray();
                if (paths.Length != 1 || paths[0] != Path.Combine(Root, request.PersistenceLoad.FileName) || Directory.Exists(paths[0]))
                    throw new InvalidOperationException("Acquired Manual isolated inventory changed before reservation.");
                var leaf = Chunk7AcquiredManualSelection.NativeOutputLeaf(observedNumber);
                var output = new Entry { Name = Chunk7AcquiredManualSelection.OutputName, Type = "Manual", Area = request.PersistenceLoad.Area, Writable = true };
                VerifyFile(leaf, output);
                // Publish the complete second entry and receipt only after all
                // fallible validation. The one native write cap is unchanged.
                entries.Add(leaf, output); acquiredManualSourceDescriptor = nativeDescriptor;
                acquiredManualNumber = observedNumber; acquiredManualReservedEntries = entries.Count;
                acquiredManualReservedWritable = entries.Values.Count(value => value.Writable); acquiredManualReservations = 1;
            }
        }

        // Read-only admission proof for the closed progression family.
        // It cannot add leaves, consume admission, rename a save or authorize a
        // request. The guarded bootstrap must have declared exactly this plan.
        internal bool MatchesProgressionPlan(RuntimeSaveDescriptor source, bool terminalAutosave, string nativeAutoName)
        {
            if (source == null) return false;
            lock (sync)
            {
                Entry input;
                if (gameId != source.GameId || gameName != source.GameName || declaresBootstrap || bootstrapWindowOpen ||
                    entries.Count != (terminalAutosave ? 2 : 1) || !entries.TryGetValue(source.FileName, out input) ||
                    input.Name != source.InternalName || input.Type != "Manual" || input.Area != source.Area ||
                    input.Hash != source.Sha256 || input.Writable || input.Writing || input.AdmissionArea != null ||
                    input.Bootstrap || input.AdmitsBeforeArea || input.ForeignGameId != null || input.ForeignGameName != null)
                    return false;
                if (!terminalAutosave) return maximumNativeWriteRequests == null && admittedNativeWriteRequests == 0;
                Entry output;
                return maximumNativeWriteRequests == 1 && admittedNativeWriteRequests <= 1 &&
                    entries.TryGetValue("Auto_1.zks", out output) && output.Name == nativeAutoName && output.Type == "Auto" &&
                    output.Area == source.Area && output.Writable && output.AdmissionArea == null &&
                    !output.Bootstrap && !output.AdmitsBeforeArea && output.ForeignGameId == null && output.ForeignGameName == null;
            }
        }

        // Called only by the real RuntimeSaveAuthorization native entry, never
        // by read-only eligibility/descriptor probes. Historical scopes remain
        // uncapped. A selected ordinary acquisition scope consumes its one
        // admission even if native serialization subsequently fails.
        internal string AuthorizeNativeRequest(RuntimeSaveOperation operation, RuntimeSaveTarget target, string observedRoot)
        {
            lock (sync)
            {
                var smallFixtureRejection = SmallFixtureAdmission(operation);
                if (smallFixtureRejection != null) return smallFixtureRejection;
                if (!AssistedCumulativeUnchanged()) return "Assisted cumulative captured request changed before native/storage admission.";
                if (!ProgressionManualUnchanged()) return "Owned progression Manual request changed before native admission.";
                var assistedColdRejection = AssistedColdAdmission(operation);
                if (assistedColdRejection != null) return assistedColdRejection;
                var assistedRejection = AssistedNativeAdmission(operation, target);
                if (assistedRejection != null) return assistedRejection;
                var rejection = Validate(operation, target, observedRoot);
                if (rejection != null) return rejection;
                if (operation == RuntimeSaveOperation.Load)
                {
                    // Validate also checks exhaustion at the earlier read-only
                    // Game.LoadGame boundary, before the old world is disposed.
                    // Only this real LoadRoutine admission consumes the count.
                    admittedNativeLoadRequests++;
                    return null;
                }
                if (operation != RuntimeSaveOperation.Write) return null;
                if (maximumNativeWriteRequests.HasValue && admittedNativeWriteRequests >= maximumNativeWriteRequests.Value)
                    return "The exact isolated scope exhausted its selected native write admission count.";
                if (OwnerSeedPlan && (ownerSeedPhase != "levelup" || admittedNativeWriteRequests != ownerSeedConfirmedWrites))
                    return "Owner-seed native write is outside its one reserved setup level-up Auto.";
                if (ownedPairsRequest != null && !OwnerSeedPlan && admittedNativeWriteRequests == 1 && !OwnedPairsReplacementReady(target))
                    return "Owned-pair replacement lacks its exact completed native seed and reserved staging leaf.";
                admittedNativeWriteRequests++;
                return null;
            }
        }

        // Exact read-only field bridge: one immutable Auto_1 source, one load,
        // no writes or alternate/foreign/transition/bootstrap leaves. Reading
        // this predicate never spends or replenishes a native admission.
        internal bool MatchesReadOnlyFieldPlan(RuntimeSaveDescriptor source)
        {
            lock (sync)
            {
                Entry entry;
                return source != null && source.FileName == "Auto_1.zks" && source.GameId == gameId && source.GameName == gameName &&
                    maximumNativeLoadRequests == 1 && !maximumNativeWriteRequests.HasValue && !declaresBootstrap && entries.Count == 1 &&
                    entries.TryGetValue(source.FileName, out entry) && entry.Name == source.InternalName && entry.Type == "Auto" &&
                    entry.Area == source.Area && entry.Hash == source.Sha256 && !entry.Writable && !entry.Writing &&
                    !entry.Bootstrap && !entry.AdmitsBeforeArea && entry.AdmissionArea == null && entry.ForeignGameId == null && entry.ForeignGameName == null;
            }
        }

        // Only the separate Small Manual calibration case consumes this one-entry
        // field-durable source. No writable/alternate/bootstrap leaf is admitted.
        internal bool MatchesReadOnlySmallManualPlan(RuntimeSaveDescriptor source)
        {
            lock (sync)
            {
                Entry entry;
                return source != null && Chunk7SmallManualSourceSelection.IsNativeManualLeaf(source.FileName) &&
                    source.InternalName == Chunk7AcquiredManualSelection.OutputName && source.GameId == gameId && source.GameName == gameName &&
                    maximumNativeLoadRequests == 1 && !maximumNativeWriteRequests.HasValue && !declaresBootstrap && entries.Count == 1 &&
                    entries.TryGetValue(source.FileName, out entry) && entry.Name == source.InternalName && entry.Type == "Manual" &&
                    entry.Area == source.Area && entry.Hash == source.Sha256 && !entry.Writable && !entry.Writing &&
                    !entry.Bootstrap && !entry.AdmitsBeforeArea && entry.AdmissionArea == null && entry.ForeignGameId == null && entry.ForeignGameName == null;
            }
        }

        // The closed native-bootstrap output uses the same neutral read-only
        // Auto_1 authority shape; provenance is admitted by its request/source
        // qualifier and observed native load, never by this predicate or decode.
        internal bool MatchesReadOnlyBootstrapInventoryPlan(RuntimeSaveDescriptor source) => MatchesReadOnlyFieldPlan(source);

        // A separate save-only plan. Older one-entry read-only predicates remain
        // unchanged. Inspection never grants or consumes a native operation.
        internal bool MatchesManualDerivedPlan(RuntimeSaveDescriptor source, string outputLeaf, string outputName, bool requireUnwritten)
        {
            lock (sync)
            {
                Entry input, output;
                if (source == null || source.FileName != "Manual_1_KMC_C7_NATIVE_SEED.zks" || source.InternalName != Chunk7ManualSeedSelection.OutputName ||
                    source.GameId != gameId || source.GameName != gameName || outputLeaf != Chunk7ManualDerivedSelection.OutputLeaf ||
                    string.IsNullOrWhiteSpace(outputName) || outputName.Length > 256 || outputName.Any(char.IsControl) ||
                    maximumNativeLoadRequests != 1 || maximumNativeWriteRequests != 1 || declaresBootstrap || bootstrapWindowOpen || entries.Count != 2 ||
                    !entries.TryGetValue(source.FileName, out input) || input.Name != source.InternalName || input.Type != "Manual" ||
                    input.Area != source.Area || input.Hash != source.Sha256 || input.Writable || input.Writing || input.Bootstrap ||
                    input.AdmitsBeforeArea || input.AdmissionArea != null || input.ForeignGameId != null || input.ForeignGameName != null ||
                    !entries.TryGetValue(outputLeaf, out output) || output.Name != outputName || output.Type != "Auto" || output.Area != source.Area ||
                    !output.Writable || output.Bootstrap || output.AdmitsBeforeArea || output.AdmissionArea != null ||
                    output.ForeignGameId != null || output.ForeignGameName != null || admittedNativeLoadRequests > 1 || admittedNativeWriteRequests > 1)
                    return false;
                return requireUnwritten ? output.Hash == null && !output.Writing && admittedNativeWriteRequests == 0 :
                    (output.Hash == null && !output.Writing || admittedNativeWriteRequests == 1);
            }
        }

        private RuntimeRequest assistedCumulativeRequest;
        private Chunk7CumulativeAcquisitionSelection assistedCumulativeSelection;
        private RuntimeSaveDescriptor assistedCumulativeDescriptor;
        private string assistedCumulativeKey;
        private static void CumulativeOptionalInt(System.Text.StringBuilder key, int? value)
        { if (value.HasValue) { key.Append('?'); AssistedScalar(key, value.Value); } else key.Append("N;"); }
        private static string AssistedCumulativeKey(RuntimeRequest value)
        {
            if (value?.Scenario != Chunk7CumulativeAcquisitionSelection.Scenario || value.CumulativeAcquisition?.UsesAssistedCold != true || value.Validate().Count != 0) return null;
            var key = new System.Text.StringBuilder("assisted-cumulative-request-v1;");
            AssistedScalar(key, value.SchemaVersion); AssistedScalar(key, value.RunId); AssistedScalar(key, value.Scenario);
            AssistedScalar(key, value.Branch); AssistedScalar(key, value.Commit); AssistedScalar(key, value.ProductVersion);
            AssistedScalar(key, value.DllSha256); AssistedScalar(key, value.DllMvid); AssistedScalar(key, value.TransactionToken); AssistedScalar(key, value.EvidenceRoot);
            AssistedScalar(key, value.SaveAccessAllowed); AssistedScalar(key, value.SaveName); AssistedScalar(key, value.ObserveWorkingDeserialization);
            AssistedScalar(key, value.Schema1DerivationSha256); AssistedScalar(key, value.PersistenceCase);
            AssistedDescriptor(key, value.Fixture.Baseline); AssistedDescriptor(key, value.Fixture.Working); AssistedDescriptor(key, value.PersistenceLoad);
            var write = value.Fixture.WriteAuthorization;
            AssistedScalar(key, write.Mode); AssistedScalar(key, write.AllowedInternalName); AssistedScalar(key, write.AllowedFileName); AssistedScalar(key, write.BaselineImmutable);
            AssistedScalar(key, value.QualificationSuite.SuiteId); AssistedScalar(key, value.QualificationSuite.SnapshotSha256);
            var s = value.CumulativeAcquisition;
            AssistedScalar(key, s.Case); AssistedScalar(key, s.SourceMode); AssistedScalar(key, s.SourceRunId);
            AssistedScalar(key, s.SourceArtifactSha256); AssistedScalar(key, s.SourceArchiveSha256); AssistedScalar(key, s.AssistedColdRunId); AssistedScalar(key, s.AssistedColdArtifactSha256);
            AssistedScalar(key, s.BaseGameOnly); AssistedScalar(key, s.ExpectedNativeLoadCount); AssistedScalar(key, s.ExpectedNativeWriteCount); AssistedScalar(key, s.NativeWriteLeaf);
            AssistedScalar(key, s.PriorOwnerId); AssistedScalar(key, s.PriorPetId); AssistedScalar(key, s.ExistingActorId);
            CumulativeOptionalInt(key, s.ExistingLevel); CumulativeOptionalInt(key, s.ExistingExperience); CumulativeOptionalInt(key, s.ExistingRangerLevel);
            AssistedScalar(key, s.SmallRaceGuid); AssistedScalar(key, s.SmallPresetGuid); AssistedScalar(key, s.SkeletonName); AssistedScalar(key, s.Gender); AssistedScalar(key, s.LeftHanded);
            var p = s.Readiness;
            AssistedScalar(key, p.MainActorId); AssistedScalar(key, p.MainLevel); AssistedScalar(key, p.MainExperience); AssistedScalar(key, p.Money);
            AssistedScalar(key, p.ExperienceRatePercent); AssistedScalar(key, p.NativeHireCost); AssistedScalar(key, p.DefaultHireExperience);
            AssistedScalar(key, p.PartyCharacterIds.Length); foreach (var id in p.PartyCharacterIds) AssistedScalar(key, id);
            AssistedScalar(key, s.MediumBody != null);
            if (s.MediumBody != null)
            {
                var b = s.MediumBody;
                AssistedScalar(key, b.TemplateActorId); AssistedScalar(key, b.RaceGuid); AssistedScalar(key, b.PresetGuid);
                AssistedScalar(key, b.SkeletonName); AssistedScalar(key, b.Gender); AssistedScalar(key, b.LeftHanded);
            }
            return key.ToString();
        }
        private bool AssistedCumulativeUnchanged() => assistedCumulativeRequest == null ||
            ReferenceEquals(assistedCumulativeRequest.CumulativeAcquisition, assistedCumulativeSelection) &&
            ReferenceEquals(assistedCumulativeRequest.PersistenceLoad, assistedCumulativeDescriptor) &&
            assistedCumulativeKey == AssistedCumulativeKey(assistedCumulativeRequest);
        internal void ConfigureAssistedCumulative(RuntimeRequest request)
        {
            lock (sync)
            {
                var key = AssistedCumulativeKey(request);
                Entry output;
                if (key == null || assistedCumulativeRequest != null || assistedRequest != null || assistedColdRequest != null ||
                    acquiredManualRequest != null || ownedPairsRequest != null || admittedNativeLoadRequests != 0 || admittedNativeWriteRequests != 0 ||
                    !entries.TryGetValue(Chunk7CumulativeAcquisitionSelection.OutputLeaf, out output) ||
                    !MatchesAssistedCumulativeEntries(request.PersistenceLoad, output.Name, true))
                    throw new InvalidOperationException("Assisted cumulative acquisition requires its unused closed one-load/one-write authority.");
                assistedCumulativeRequest = request; assistedCumulativeSelection = request.CumulativeAcquisition;
                assistedCumulativeDescriptor = request.PersistenceLoad; assistedCumulativeKey = key;
            }
        }
        internal bool MatchesAssistedCumulativePlan(RuntimeRequest request, string outputLeaf, string outputName, bool requireUnwritten)
        {
            lock (sync) return assistedCumulativeRequest != null && ReferenceEquals(assistedCumulativeRequest, request) &&
                AssistedCumulativeUnchanged() && outputLeaf == Chunk7CumulativeAcquisitionSelection.OutputLeaf &&
                MatchesAssistedCumulativeEntries(request.PersistenceLoad, outputName, requireUnwritten);
        }
        private bool MatchesAssistedCumulativeEntries(RuntimeSaveDescriptor source, string outputName, bool requireUnwritten)
        {
            Entry input, output;
            if (source == null || !Chunk7AssistedColdInventorySelection.IsManualLeaf(source.FileName) || source.InternalName != Chunk7AssistedFixtureSelection.OutputName ||
                source.GameId != gameId || source.GameName != gameName || string.IsNullOrWhiteSpace(outputName) || outputName.Length > 256 || outputName.Any(char.IsControl) ||
                maximumNativeLoadRequests != 1 || maximumNativeWriteRequests != 1 || declaresBootstrap || bootstrapWindowOpen || entries.Count != 2 ||
                !entries.TryGetValue(source.FileName, out input) || input.Name != source.InternalName || input.Type != "Manual" || input.Area != source.Area ||
                input.Hash != source.Sha256 || input.Writable || input.Writing || input.Bootstrap || input.AdmitsBeforeArea || input.AdmissionArea != null ||
                input.ForeignGameId != null || input.ForeignGameName != null ||
                !entries.TryGetValue(Chunk7CumulativeAcquisitionSelection.OutputLeaf, out output) || output.Name != outputName || output.Type != "Auto" || output.Area != source.Area ||
                !output.Writable || output.Bootstrap || output.AdmitsBeforeArea || output.AdmissionArea != null || output.ForeignGameId != null || output.ForeignGameName != null ||
                admittedNativeLoadRequests > 1 || admittedNativeWriteRequests > 1) return false;
            return requireUnwritten ? output.Hash == null && !output.Writing && admittedNativeWriteRequests == 0 :
                (output.Hash == null && !output.Writing || admittedNativeWriteRequests == 1);
        }

        // Independent closed owner1-to-owner2 authority; the original seed matcher above is unchanged.
        internal bool MatchesCumulativeAcquisitionPlan(RuntimeSaveDescriptor source, string outputLeaf, string outputName, bool requireUnwritten)
        {
            lock (sync)
            {
                Entry input, output;
                if (source == null || !Chunk7CumulativeAcquisitionSelection.IsAcquiredManualLeaf(source.FileName) || source.InternalName != Chunk7AcquiredManualSelection.OutputName ||
                    source.GameId != gameId || source.GameName != gameName || outputLeaf != Chunk7CumulativeAcquisitionSelection.OutputLeaf ||
                    string.IsNullOrWhiteSpace(outputName) || outputName.Length > 256 || outputName.Any(char.IsControl) ||
                    maximumNativeLoadRequests != 1 || maximumNativeWriteRequests != 1 || declaresBootstrap || bootstrapWindowOpen || entries.Count != 2 ||
                    !entries.TryGetValue(source.FileName, out input) || input.Name != source.InternalName || input.Type != "Manual" ||
                    input.Area != source.Area || input.Hash != source.Sha256 || input.Writable || input.Writing || input.Bootstrap ||
                    input.AdmitsBeforeArea || input.AdmissionArea != null || input.ForeignGameId != null || input.ForeignGameName != null ||
                    !entries.TryGetValue(outputLeaf, out output) || output.Name != outputName || output.Type != "Auto" || output.Area != source.Area ||
                    !output.Writable || output.Bootstrap || output.AdmitsBeforeArea || output.AdmissionArea != null ||
                    output.ForeignGameId != null || output.ForeignGameName != null || admittedNativeLoadRequests > 1 || admittedNativeWriteRequests > 1)
                    return false;
                return requireUnwritten ? output.Hash == null && !output.Writing && admittedNativeWriteRequests == 0 :
                    (output.Hash == null && !output.Writing || admittedNativeWriteRequests == 1);
            }
        }

        internal bool MatchesManualSeedPlan(RuntimeSaveDescriptor source, string outputLeaf, bool requireUnwritten)
        {
            lock (sync)
            {
                Entry input, output;
                if (source == null || source.FileName != "Auto_1.zks" || source.GameId != gameId || source.GameName != gameName ||
                    outputLeaf != "Manual_1_" + Chunk7ManualSeedSelection.OutputName + ".zks" ||
                    maximumNativeLoadRequests != 1 || maximumNativeWriteRequests != 1 || declaresBootstrap || bootstrapWindowOpen || entries.Count != 2 ||
                    !entries.TryGetValue(source.FileName, out input) || input.Name != source.InternalName || input.Type != "Auto" ||
                    input.Area != source.Area || input.Hash != source.Sha256 || input.Writable || input.Writing || input.Bootstrap ||
                    input.AdmitsBeforeArea || input.AdmissionArea != null || input.ForeignGameId != null || input.ForeignGameName != null ||
                    !entries.TryGetValue(outputLeaf, out output) || output.Name != Chunk7ManualSeedSelection.OutputName || output.Type != "Manual" ||
                    output.Area != source.Area || !output.Writable || output.Bootstrap || output.AdmitsBeforeArea || output.AdmissionArea != null ||
                    output.ForeignGameId != null || output.ForeignGameName != null || admittedNativeLoadRequests > 1 || admittedNativeWriteRequests > 1)
                    return false;
                return requireUnwritten ? output.Hash == null && !output.Writing && admittedNativeWriteRequests == 0 :
                    (output.Hash == null && !output.Writing || admittedNativeWriteRequests == 1);
            }
        }

        // Closed pre-fixture bootstrap: Working is only an immutable authorization
        // anchor. No LoadGame/LoadRoutine is eligible; only the authored first
        // autosave may create a new native campaign in this exact run root.
        internal bool MatchesNativeBootstrapPlan(RuntimeSaveDescriptor source, string area)
        {
            lock (sync)
            {
                Entry anchor, output;
                return source != null && source.GameId == gameId && source.GameName == gameName &&
                    maximumNativeLoadRequests == 0 && maximumNativeWriteRequests == 1 && declaresBootstrap && entries.Count == 2 &&
                    entries.TryGetValue(source.FileName, out anchor) && anchor.Name == source.InternalName && anchor.Type == "Manual" &&
                    anchor.Area == source.Area && anchor.Hash == source.Sha256 && !anchor.Writable && !anchor.Writing && !anchor.Bootstrap &&
                    !anchor.AdmitsBeforeArea && anchor.AdmissionArea == null && anchor.ForeignGameId == null && anchor.ForeignGameName == null &&
                    entries.TryGetValue(Chunk7NativeBootstrapSelection.AutoLeaf, out output) && output.Type == "Auto" && output.Area == area &&
                    output.Writable && output.Bootstrap && output.AdmitsBeforeArea && output.AdmissionArea == null &&
                    output.ForeignGameId == null && output.ForeignGameName == null;
            }
        }

        internal void CloseUnusedBootstrapWindow()
        {
            lock (sync)
            {
                if (bootstrapGameId == null)
                {
                    if (admittedNativeWriteRequests != 0) throw new InvalidOperationException("An admitted bootstrap write still owns its identity window.");
                    bootstrapWindowOpen = false;
                }
            }
        }

        // ---- Bootstrap campaign ----
        //
        // Campaign A is the validated fixture identity every ordinary leaf is
        // bound to. A run that declares bootstrap leaves may start exactly ONE
        // native new game: the scenario opens the window immediately before
        // Game.LoadNewGame, the engine mints the identity, and the first native
        // write that reaches a bootstrap leaf inside that window freezes the
        // exact GameId/GameName it carries -- nothing is assigned by KMC. Before
        // the freeze, bootstrap leaves admit nothing; after it, only that exact
        // identity, and the window is closed. Ordinary leaves never admit it.
        private bool declaresBootstrap;
        private bool bootstrapWindowOpen;
        private string bootstrapGameId;
        private string bootstrapGameName;
        internal bool DeclaresBootstrapCampaign => declaresBootstrap;
        internal bool BootstrapWindowOpen { get { lock (sync) return bootstrapWindowOpen; } }
        internal string BootstrapGameId { get { lock (sync) return bootstrapGameId; } }
        internal string BootstrapGameName { get { lock (sync) return bootstrapGameName; } }
        internal int BootstrapFreezeCount { get; private set; }

        internal void OpenBootstrapWindow()
        {
            lock (sync)
            {
                if (!declaresBootstrap) throw new InvalidOperationException("This run declares no bootstrap campaign leaf.");
                if (bootstrapGameId != null) throw new InvalidOperationException("The bootstrap campaign identity is already frozen.");
                if (bootstrapWindowOpen) throw new InvalidOperationException("The bootstrap window is already open.");
                bootstrapWindowOpen = true;
            }
        }

        // The engine's own first write for the new game mints the identity.
        private string TryFreezeBootstrapLocked(string observedGameId, string observedGameName)
        {
            if (bootstrapGameId != null) return null;
            if (!bootstrapWindowOpen) return "Bootstrap campaign identity is not minted: no native new game was opened.";
            Guid parsed;
            if (string.IsNullOrEmpty(observedGameId) || !Guid.TryParse(observedGameId, out parsed) ||
                string.Equals(observedGameId, gameId, StringComparison.Ordinal))
                return "Bootstrap campaign identity is not a fresh native GameId.";
            if (string.IsNullOrEmpty(observedGameName)) return "Bootstrap campaign has no native GameName.";
            bootstrapGameId = observedGameId;
            bootstrapGameName = observedGameName;
            bootstrapWindowOpen = false;
            BootstrapFreezeCount++;
            return null;
        }

        private string ExpectedGameId(Entry entry) => entry.Bootstrap ? bootstrapGameId : entry.ForeignGameId ?? gameId;
        private string ExpectedGameName(Entry entry) => entry.Bootstrap ? bootstrapGameName : entry.ForeignGameName ?? gameName;

        internal PersistenceSaveAuthorization(string authorizedRunRoot, string campaignId,
            string campaignName, string protectedBaselineHash, IEnumerable<PersistenceSaveEntry> allowlist)
            : this(authorizedRunRoot, campaignId, campaignName, protectedBaselineHash, allowlist, null, null)
        {
        }

        internal PersistenceSaveAuthorization(string authorizedRunRoot, string campaignId,
            string campaignName, string protectedBaselineHash, IEnumerable<PersistenceSaveEntry> allowlist,
            string declaredTransitionSource, string declaredTransitionTarget, int? maximumWriteRequests = null,
            int? maximumLoadRequests = null, RuntimeRequest acquiredManualReservationRequest = null,
            object acquiredManualNativeGame = null, object acquiredManualNativeManager = null,
            RuntimeRequest ownedPairsSelectionRequest = null, object ownedPairsNativeGame = null, object ownedPairsNativeManager = null,
            string ownedPairsNativeOutputName = null)
        {
            if (maximumWriteRequests.HasValue && (maximumWriteRequests.Value < (ownedPairsSelectionRequest != null ? 0 : 1) || maximumWriteRequests.Value > 128))
                throw new ArgumentException("A selected isolated write count must be positive and bounded.");
            if (maximumLoadRequests.HasValue && (maximumLoadRequests.Value < 0 || maximumLoadRequests.Value > 128))
                throw new ArgumentException("A selected isolated load count must be nonnegative and bounded.");
            maximumNativeWriteRequests = maximumWriteRequests;
            maximumNativeLoadRequests = maximumLoadRequests;
            var transitionDeclared = declaredTransitionSource != null || declaredTransitionTarget != null;
            if (transitionDeclared && (declaredTransitionSource == null || declaredTransitionTarget == null ||
                declaredTransitionSource == declaredTransitionTarget))
                throw new ArgumentException("A declared area transition needs two distinct exact endpoints.");
            var runRoot = Canonical(authorizedRunRoot);
            RequireDirectory(runRoot);
            Root = Path.Combine(runRoot, "Saved Games");
            RequireDirectory(Root);
            gameId = Required(campaignId, 128);
            gameName = Required(campaignName, 256);
            baselineHash = HashText(protectedBaselineHash);
            if (allowlist == null) throw new ArgumentNullException(nameof(allowlist));
            foreach (var item in allowlist)
            {
                if (item == null || entries.Count >= 128) throw new ArgumentException("Invalid bounded save allowlist.");
                var name = Required(item.InternalName, 256);
                var file = Required(item.FileName, 180);
                if (file != Path.GetFileName(file) || file.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                    !file.EndsWith(".zks", StringComparison.Ordinal) || file.EndsWith(" ", StringComparison.Ordinal) ||
                    file.IndexOf("KMC_AUTOMATION_BASELINE", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    string.Equals(name, RuntimeRequest.BaselineSaveName, StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("Save leaf or protected baseline alias is invalid.");
                if (item.SaveType != "Manual" && item.SaveType != "Quick" && item.SaveType != "Auto")
                    throw new ArgumentException("Only actual Manual, Quick and Auto save types are supported.");
                if (entries.ContainsKey(file)) throw new ArgumentException("Ambiguous duplicate save leaf.");
                var hash = string.IsNullOrEmpty(item.InitialSha256) ? null : HashText(item.InitialSha256);
                if (hash == baselineHash) throw new ArgumentException("A protected baseline copy is not an authorized fixture.");
                var area = Required(item.Area, 32);
                string admission = null;
                if (!string.IsNullOrEmpty(item.AdmissionArea) && item.AdmissionArea != area)
                {
                    admission = Required(item.AdmissionArea, 32);
                    // Only the two declared endpoints of a declared transfer may
                    // span two areas, and only a writable leaf may do so.
                    if (!transitionDeclared || !item.Writable ||
                        !(admission == declaredTransitionSource && area == declaredTransitionTarget ||
                          admission == declaredTransitionTarget && area == declaredTransitionSource))
                        throw new ArgumentException("A two-area save leaf must be a writable declared transition endpoint.");
                }
                if (item.Campaign != null && item.Campaign != "A" && item.Campaign != "B")
                    throw new ArgumentException("A save leaf belongs to campaign A or the bootstrap campaign B.");
                var bootstrap = item.Campaign == "B";
                // The bootstrap campaign does not exist before the run starts it:
                // its leaves are single-area, writable and initially empty.
                if (bootstrap && (!item.Writable || hash != null || admission != null))
                    throw new ArgumentException("A bootstrap campaign leaf must be an empty single-area writable leaf.");
                if (item.AdmitsBeforeArea && (!bootstrap || item.SaveType != "Auto"))
                    throw new ArgumentException("Only a bootstrap autosave leaf is admitted before any area is loaded.");
                declaresBootstrap |= bootstrap;
                var foreignDeclared = item.ForeignGameId != null || item.ForeignGameName != null;
                if (foreignDeclared)
                {
                    // Only a completed read-only fixture-campaign-shaped leaf may carry
                    // a foreign identity, and that identity must be a distinct real one.
                    Guid foreignGuid;
                    if (item.Writable || hash == null || admission != null || bootstrap || item.AdmitsBeforeArea ||
                        item.ForeignGameId == null || !Guid.TryParse(item.ForeignGameId, out foreignGuid) || foreignGuid == Guid.Empty ||
                        string.Equals(item.ForeignGameId, gameId, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(item.ForeignGameName))
                        throw new ArgumentException("A foreign-campaign leaf must be a completed read-only leaf with a distinct real identity.");
                }
                entries.Add(file, new Entry { Name = name, Type = item.SaveType, Area = area,
                    AdmissionArea = admission, Hash = hash, Writable = item.Writable, Bootstrap = bootstrap,
                    AdmitsBeforeArea = item.AdmitsBeforeArea,
                    ForeignGameId = foreignDeclared ? Required(item.ForeignGameId, 128) : null,
                    ForeignGameName = foreignDeclared ? Required(item.ForeignGameName, 256) : null });
            }
            if (entries.Count == 0) throw new ArgumentException("An exact save allowlist is required.");
            if (declaresBootstrap && entries.Values.All(entry => entry.Bootstrap))
                throw new ArgumentException("A bootstrap campaign needs the validated fixture campaign beside it.");
            foreach (var path in Directory.EnumerateFileSystemEntries(Root))
            {
                if (!entries.ContainsKey(Path.GetFileName(path)) || Directory.Exists(path))
                    throw new ArgumentException("Isolated root contains an unowned or ambiguous entry.");
            }
            foreach (var item in entries) VerifyFile(item.Key, item.Value);
            if (acquiredManualReservationRequest != null || acquiredManualNativeGame != null || acquiredManualNativeManager != null)
            {
                if (acquiredManualReservationRequest?.Scenario != Chunk7AcquiredManualSelection.Scenario ||
                    acquiredManualReservationRequest.AcquiredManual == null || acquiredManualNativeGame == null || acquiredManualNativeManager == null ||
                    acquiredManualReservationRequest.Validate().Count != 0 || transitionDeclared)
                    throw new ArgumentException("Only an exact closed acquired Manual request may reserve an observed native output.");
                acquiredManualRequest = acquiredManualReservationRequest; acquiredManualGame = acquiredManualNativeGame;
                acquiredManualManager = acquiredManualNativeManager; acquiredManualRequestKey = AcquiredManualRequestKey(acquiredManualReservationRequest);
                if (!MatchesAcquiredManualPlan(acquiredManualRequest.PersistenceLoad, false, true))
                    throw new ArgumentException("Acquired Manual reservation must start with one immutable Auto and no writable leaf.");
                acquiredManualInitialEntries = entries.Count; acquiredManualInitialWritable = entries.Values.Count(value => value.Writable);
            }
            if (ownedPairsSelectionRequest != null || ownedPairsNativeGame != null || ownedPairsNativeManager != null)
            {
                if (ownedPairsSelectionRequest?.Scenario != Chunk7OwnedPairsSelection.Scenario || ownedPairsSelectionRequest.OwnedPairs == null ||
                    ownedPairsSelectionRequest.Validate().Count != 0 || ownedPairsNativeGame == null || ownedPairsNativeManager == null || transitionDeclared || acquiredManualRequest != null)
                    throw new ArgumentException("Only the exact closed owned-pair family may select this native reservation.");
                ownedPairsRequest = ownedPairsSelectionRequest; ownedPairsGame = ownedPairsNativeGame; ownedPairsManager = ownedPairsNativeManager;
                ownedPairsKey = OwnedPairsKey(ownedPairsSelectionRequest); ownedPairsExpectedNativeName = ownedPairsNativeOutputName;
                // Owner-seed setup writes are native level-up Autos reserved one at a time; no output name is pre-observed.
                if (maximumNativeWriteRequests > 0 && ownedPairsSelectionRequest.OwnedPairs.IsOwnerSeed != true ? string.IsNullOrWhiteSpace(ownedPairsNativeOutputName) || ownedPairsNativeOutputName.Length > 256 || ownedPairsNativeOutputName.Any(char.IsControl) : ownedPairsNativeOutputName != null)
                    throw new ArgumentException("Owned-pair native output name must be observed only for its selected one-write case.");
                if (!MatchesOwnedPairsPlan(false, true)) throw new ArgumentException("Owned pairs must start with one immutable native input and no writable entry.");
            }
        }

        internal void AssertReadableArchive(string path)
        {
            lock (sync)
            {
                if (!SmallFixtureUnchanged()) throw new InvalidOperationException("F09 captured request changed before archive access.");
                if (!AssistedCumulativeUnchanged()) throw new InvalidOperationException("Assisted cumulative captured request changed before native archive access.");
                if (!AssistedColdUnchanged()) throw new InvalidOperationException("Assisted cold captured request changed before native archive access.");
                if (!AssistedUnchanged()) throw new InvalidOperationException("Assisted fixture captured request changed before native archive access.");
                if (!OwnedPairsUnchanged()) throw new InvalidOperationException("Owned-pair captured request changed before native archive access.");
                if (!AcquiredManualRequestUnchanged()) throw new InvalidOperationException("Acquired Manual captured request changed before native archive access.");
                if (!ProgressionManualUnchanged()) throw new InvalidOperationException("Owned progression Manual request changed before native archive access.");
                if (Canonical(path) != path || Path.GetDirectoryName(path) != Root ||
                    !entries.TryGetValue(Path.GetFileName(path), out var entry) || entry.Writing || entry.Hash == null)
                    throw new InvalidOperationException("Native load archive is not a completed exact owned entry.");
                VerifyFile(Path.GetFileName(path), entry);
            }
        }

        // Request creation precedes native queue ordering and slot allocation.
        // A unique, still-empty declared destination can identify the request;
        // BeginWrite still validates the actual PrepareSave path before storage.
        internal RuntimeSaveTarget ProjectNewRequest(RuntimeSaveTarget predicted, string observedRoot)
        {
            lock (sync)
            {
                if (!SmallFixtureUnchanged() || smallFixtureRequest != null &&
                    (smallFixtureReservations != 1 || admittedNativeLoadRequests != 1))
                    throw new InvalidOperationException("F09 projection requires its unchanged loaded request and native Manual reservation.");
                if (!AssistedCumulativeUnchanged()) throw new InvalidOperationException("Assisted cumulative captured request changed before projection.");
                if (!AssistedColdUnchanged()) throw new InvalidOperationException("Assisted cold captured request changed before projection.");
                if (!AssistedUnchanged()) throw new InvalidOperationException("Assisted fixture captured request changed before projection.");
                if (!OwnedPairsUnchanged() || ownedPairsRequest != null && !OwnedPairsWriteReady)
                    throw new InvalidOperationException("Owned-pair write requires unchanged request, native reservation and prior load.");
                if (!AcquiredManualRequestUnchanged() || acquiredManualRequest != null &&
                    (acquiredManualReservations != 1 || admittedNativeLoadRequests != 1))
                    throw new InvalidOperationException("Acquired Manual projection requires its unchanged request and admitted cold load.");
                if (!ProgressionManualUnchanged() || progressionManualRequest != null &&
                    (progressionManualReservations != 1 || admittedNativeLoadRequests != 1))
                    throw new InvalidOperationException("Owned progression Manual projection requires its exact reservation and admitted cold load.");
                if (predicted == null || Canonical(observedRoot) != Root ||
                    Canonical(predicted.FullPath) != Path.Combine(Root, predicted.FileName) ||
                    predicted.FileName != Path.GetFileName(predicted.FullPath))
                    throw new InvalidOperationException("New request projection escaped its exact isolated root.");
                RequireDirectory(Root);
                // A request carrying the engine-minted identity of the one open
                // (or already frozen) bootstrap game is projected onto bootstrap
                // leaves only; the fixture campaign never reaches them.
                var fixtureCampaign = predicted.GameId == gameId && predicted.GameName == gameName;
                var bootstrapCampaign = !fixtureCampaign && declaresBootstrap && (bootstrapGameId == null
                    ? bootstrapWindowOpen && !string.Equals(predicted.GameId, gameId, StringComparison.Ordinal)
                    : predicted.GameId == bootstrapGameId && predicted.GameName == bootstrapGameName);
                if (!fixtureCampaign && !bootstrapCampaign)
                    throw new InvalidOperationException("New request campaign differs from its run.");
                var candidates = entries.Where(p => p.Value.Name == predicted.InternalName &&
                    p.Value.Type == predicted.SaveType && p.Value.Admits(predicted.Area) &&
                    p.Value.Bootstrap == bootstrapCampaign &&
                    p.Value.Writable && !p.Value.Writing && p.Value.Hash == null).ToArray();
                // Existing rotation contracts may deliberately declare two leaves.
                // Keep their native prediction and let normal validation decide.
                if (candidates.Length != 1) return predicted;
                var candidate = candidates[0];
                VerifyFile(candidate.Key, candidate.Value);
                return new RuntimeSaveTarget
                {
                    InternalName = predicted.InternalName, FileName = candidate.Key,
                    FullPath = Path.Combine(Root, candidate.Key), SaveType = predicted.SaveType,
                    GameId = predicted.GameId, GameName = predicted.GameName, Area = predicted.Area
                };
            }
        }

        internal string Validate(RuntimeSaveOperation operation, RuntimeSaveTarget target, string observedRoot)
        {
            lock (sync) return ValidateLocked(operation, target, observedRoot);
        }

        // Inspect the identity of bytes already admitted by a native load. This
        // is not eligibility for another Game.LoadGame dispatch and consumes no
        // count; native pre-disposal and LoadRoutine paths still use Validate.
        internal string ValidateArchiveIdentity(RuntimeSaveTarget target, string observedRoot)
        {
            lock (sync) return ValidateLocked(RuntimeSaveOperation.Load, target, observedRoot, false);
        }

        private string ValidateLocked(RuntimeSaveOperation operation, RuntimeSaveTarget target, string observedRoot,
            bool checkLoadAdmission = true)
        {
            try
            {
                var smallFixtureRejection = SmallFixtureAdmission(operation);
                if (smallFixtureRejection != null) return smallFixtureRejection;
                if (!AssistedCumulativeUnchanged()) return "Assisted cumulative captured request changed before native/storage admission.";
                var assistedColdRejection = AssistedColdAdmission(operation);
                if (assistedColdRejection != null) return assistedColdRejection;
                var assistedRejection = AssistedStorageAdmission(operation, target, checkLoadAdmission);
                if (assistedRejection != null) return assistedRejection;
                if (!OwnedPairsUnchanged()) return "Owned-pair captured request changed after authority creation.";
                if (ownedPairsRequest != null && (operation == RuntimeSaveOperation.Write && !OwnedPairsWriteReady ||
                    operation == RuntimeSaveOperation.Load && checkLoadAdmission && maximumNativeWriteRequests > 0 && !OwnerSeedPlan && ownedPairsReservations != 1))
                    return "Owned-pair native admission requires its exact output reservation and load-before-write order.";
                if (!AcquiredManualRequestUnchanged()) return "Acquired Manual captured request changed after authority creation.";
                if (!ProgressionManualUnchanged()) return "Owned progression Manual request changed after authority creation.";
                if (progressionManualRequest != null && operation == RuntimeSaveOperation.Write &&
                    (progressionManualReservations != 1 || admittedNativeLoadRequests != 1))
                    return "Owned progression Manual write requires exact reservation and admitted cold load.";
                if (acquiredManualRequest != null && operation == RuntimeSaveOperation.Write &&
                    (acquiredManualReservations != 1 || admittedNativeLoadRequests != 1))
                    return "Acquired Manual write requires its exact reservation and admitted cold load.";
                if (Canonical(observedRoot) != Root) return "Observed save root differs from the isolated run root.";
                RequireDirectory(Root);
                if (target == null || string.IsNullOrEmpty(target.FileName) ||
                    !entries.TryGetValue(target.FileName, out var entry))
                    return "Save is not an explicit run-owned allowlist entry.";
                if (Canonical(target.FullPath) != Path.Combine(Root, target.FileName) ||
                    target.FileName != Path.GetFileName(target.FullPath))
                    return "Save is not the exact canonical direct child.";
                if (operation != RuntimeSaveOperation.Load && operation != RuntimeSaveOperation.Write &&
                    operation != RuntimeSaveOperation.Delete) return "Unknown save operation.";
                if (entry.Bootstrap && bootstrapGameId == null)
                {
                    // The first write the engine admits to a bootstrap leaf inside
                    // the open window freezes the identity it minted. Nothing can
                    // be loaded or rotated from that campaign before that.
                    if (operation != RuntimeSaveOperation.Write ||
                        target.InternalName != entry.Name || target.SaveType != entry.Type || !entry.Admits(target.Area))
                        return "Bootstrap campaign identity is not minted: no native new game write reached its leaf.";
                    var minting = TryFreezeBootstrapLocked(target.GameId, target.GameName);
                    if (minting != null) return minting;
                }
                if (target.InternalName != entry.Name || target.SaveType != entry.Type ||
                    target.GameId != ExpectedGameId(entry) || target.GameName != ExpectedGameName(entry) ||
                    !entry.Admits(target.Area))
                    // Naming the observed and declared identity keeps a rejected
                    // boundary self-diagnosing instead of costing another run.
                    return "Native save name, type, campaign or area differs from the run contract" +
                        " (observed name=" + target.InternalName + " type=" + target.SaveType +
                        " area=" + target.Area + " campaign=" + (entry.Bootstrap ? "B" : entry.ForeignGameId != null ? "foreign" : "A") +
                        "; declared name=" + entry.Name + " type=" + entry.Type +
                        " area=" + entry.Area + (entry.AdmissionArea == null ? string.Empty :
                        " admission=" + entry.AdmissionArea) + ").";
                if (entry.Writing) return "This save already belongs to an unfinished write.";
                if (operation != RuntimeSaveOperation.Load && !entry.Writable)
                    return "This run-owned entry is read-only.";
                VerifyFile(target.FileName, entry);
                if (operation != RuntimeSaveOperation.Write && entry.Hash == null)
                    return "Load/rotation requires a completed owned archive.";
                if (checkLoadAdmission && operation == RuntimeSaveOperation.Load && acquiredManualRequest != null &&
                    acquiredManualReservations != 1)
                    return "Acquired Manual load awaits its exact observed native destination reservation.";
                if (checkLoadAdmission && operation == RuntimeSaveOperation.Load && maximumNativeLoadRequests.HasValue &&
                    admittedNativeLoadRequests >= maximumNativeLoadRequests.Value)
                    return "The exact isolated scope exhausted its selected native load admission count.";
                return null;
            }
            catch (Exception exception)
            {
                return "Isolated save identity check failed (" + exception.GetType().Name + ").";
            }
        }

        // Storage integration must acquire after native ordering and complete only
        // after the real archive commit. Disposing without completion preserves the
        // previous identity; changed partial files remain unauthorized.
        internal WriteLease BeginWrite(RuntimeSaveTarget target, string observedRoot)
        {
            lock (sync)
            {
                if (acquiredManualRequest != null && admittedNativeWriteRequests != 1)
                    throw new InvalidOperationException("Acquired Manual storage requires its exact native write admission.");
                var assistedRejection = AssistedWriteLeaseAdmission(target);
                if (assistedRejection != null) throw new InvalidOperationException(assistedRejection);
                if (ownedPairsRequest?.PersistenceCase == "multi-area-autosave" &&
                    target?.Area != ownedPairsRequest.ExpectedChunk7ArchiveArea)
                    throw new InvalidOperationException("Owned area Auto storage requires the declared destination, not its departure admission area.");
                var rejection = ValidateLocked(RuntimeSaveOperation.Write, target, observedRoot);
                if (rejection != null) throw new InvalidOperationException(rejection);
                var entry = entries[target.FileName];
                entry.Writing = true;
                return new WriteLease(this, target.FileName, entry.Hash);
            }
        }

        // Replacement is admitted only for two completed archives already owned
        // by this run, with the same native slot identity. The native worker owns
        // ordering; this lease does not authorize arbitrary rename destinations.
        internal ReplacementLease BeginReplacement(string source, string destination)
        {
            lock (sync)
            {
                if (assistedRequest != null && (!AssistedUnchanged() || assistedPhase != "levelup-auto" || admittedNativeWriteRequests != 2 ||
                    source != Path.Combine(Root, Chunk7AssistedFixtureSelection.LevelupStagingLeaf) || destination != Path.Combine(Root, Chunk7AssistedFixtureSelection.InitialAutoLeaf)))
                    throw new InvalidOperationException("Assisted replacement is outside its exact native level-up storage phase.");
                if (Canonical(source) != source || Canonical(destination) != destination ||
                    Path.GetDirectoryName(source) != Root || Path.GetDirectoryName(destination) != Root ||
                    string.Equals(source, destination, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Replacement must use two distinct exact run-owned leaves.");
                var from = Path.GetFileName(source);
                var to = Path.GetFileName(destination);
                if (!entries.TryGetValue(from, out var staged) || !entries.TryGetValue(to, out var original) ||
                    !staged.Writable || !original.Writable || staged.Hash == null || original.Hash == null ||
                    staged.Writing || original.Writing || staged.Name != original.Name ||
                    staged.Type != original.Type || staged.Area != original.Area)
                    throw new InvalidOperationException("Replacement is outside the exact completed native slot contract.");
                VerifyFile(from, staged); VerifyFile(to, original);
                staged.Writing = true; original.Writing = true;
                return new ReplacementLease(this, from, to, staged.Hash);
            }
        }

        internal sealed class ReplacementLease : IDisposable
        {
            private PersistenceSaveAuthorization owner;
            private readonly string source;
            private readonly string destination;
            private readonly string committedHash;
            internal ReplacementLease(PersistenceSaveAuthorization owner, string source, string destination, string hash)
            { this.owner = owner; this.source = source; this.destination = destination; committedHash = hash; }

            internal void Complete()
            {
                var current = owner;
                if (current == null) throw new InvalidOperationException("Replacement scope is no longer active.");
                lock (current.sync)
                {
                    if (owner == null) throw new InvalidOperationException("Replacement scope is no longer active.");
                    RequireDirectory(current.Root);
                    var from = Path.Combine(current.Root, source);
                    if (File.Exists(from) || Directory.Exists(from) ||
                        ReadOwnedHash(Path.Combine(current.Root, destination)) != committedHash)
                        throw new IOException("Native replacement did not move the exact completed archive.");
                    current.entries[source].Hash = null;
                    current.entries[destination].Hash = committedHash;
                    Dispose();
                }
            }

            public void Dispose()
            {
                var current = owner;
                if (current == null) return;
                lock (current.sync)
                {
                    if (owner == null) return;
                    owner = null;
                    current.entries[source].Writing = false;
                    current.entries[destination].Writing = false;
                }
            }
        }

        // Used only by the still-active native worker for its own newly
        // allocated staging path; replacement destinations never enter this path.
        internal void DeleteCompletedStaging(string path)
        {
            lock (sync)
            {
                if (Canonical(path) != path || Path.GetDirectoryName(path) != Root ||
                    !entries.TryGetValue(Path.GetFileName(path), out var entry) ||
                    !entry.Writable || entry.Writing || entry.Hash == null)
                    throw new InvalidOperationException("Cleanup is not a completed owned staging archive.");
                VerifyFile(Path.GetFileName(path), entry);
                File.Delete(path);
                if (File.Exists(path) || Directory.Exists(path)) throw new IOException("Owned staging cleanup failed.");
                entry.Hash = null;
            }
        }

        internal sealed class WriteLease : IDisposable
        {
            private PersistenceSaveAuthorization owner;
            private readonly string leaf;
            private readonly string initialHash;
            internal WriteLease(PersistenceSaveAuthorization owner, string leaf, string initialHash)
            { this.owner = owner; this.leaf = leaf; this.initialHash = initialHash; }

            internal void ClearStaging()
            {
                var current = owner;
                if (current == null) throw new InvalidOperationException("Staging write scope is no longer active.");
                lock (current.sync)
                {
                    if (owner == null || initialHash != null || !current.entries[leaf].Writing ||
                        current.entries[leaf].Hash != null)
                        throw new InvalidOperationException("Cleanup cannot delete an existing completed save.");
                    RequireDirectory(current.Root);
                    var path = Path.Combine(current.Root, leaf);
                    if (Directory.Exists(path)) throw new IOException("Staging path became a directory.");
                    if (!File.Exists(path)) return;
                    if (ReadOwnedHash(path) == current.baselineHash)
                        throw new InvalidOperationException("Staging bytes alias a protected baseline.");
                    File.Delete(path);
                    if (File.Exists(path)) throw new IOException("Owned staging cleanup failed.");
                }
            }

            internal void Complete()
            {
                var current = owner;
                if (current == null) throw new InvalidOperationException("Write scope is no longer active.");
                lock (current.sync)
                {
                    if (owner == null) throw new InvalidOperationException("Write scope is no longer active.");
                    RequireDirectory(current.Root);
                    var hash = ReadOwnedHash(Path.Combine(current.Root, leaf));
                    if (hash == current.baselineHash)
                        throw new InvalidOperationException("Completed write aliases protected baseline bytes.");
                    current.entries[leaf].Hash = hash;
                    Dispose();
                }
            }

            public void Dispose()
            {
                var current = owner;
                if (current == null) return;
                lock (current.sync)
                {
                    if (owner == null) return;
                    owner = null;
                    current.entries[leaf].Writing = false;
                }
            }
        }

        private void VerifyFile(string leaf, Entry entry)
        {
            var path = Path.Combine(Root, leaf);
            if (entry.Hash == null)
            {
                if (File.Exists(path) || Directory.Exists(path))
                    throw new IOException("An uncommitted allowlist leaf already exists.");
                return;
            }
            if (ReadOwnedHash(path) != entry.Hash) throw new IOException("Owned save bytes changed outside a completed operation.");
        }

        private static string Required(string value, int max)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > max || value.Any(char.IsControl))
                throw new ArgumentException("Missing or oversized save identity.");
            return value;
        }

        private static string HashText(string value)
        {
            if (value == null || value.Length != 64 || value.Any(c => !(c >= '0' && c <= '9' || c >= 'a' && c <= 'f')))
                throw new ArgumentException("An exact lowercase SHA256 is required.");
            return value;
        }

        private static string Canonical(string path)
        {
            Required(path, 240);
            if (!Path.IsPathRooted(path) || path.StartsWith(@"\\", StringComparison.Ordinal) ||
                path.IndexOf(':', 2) >= 0)
                throw new ArgumentException("A local absolute path without device/stream syntax is required.");
            var full = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
            if (!string.Equals(path.TrimEnd(Path.DirectorySeparatorChar), full, StringComparison.Ordinal) ||
                full == Path.GetPathRoot(full).TrimEnd(Path.DirectorySeparatorChar))
                throw new ArgumentException("Noncanonical or filesystem-root save path.");
            return full;
        }

        private static void RequireDirectory(string path)
        {
            if (!Directory.Exists(path)) throw new DirectoryNotFoundException("Owned save directory must already exist.");
            for (var current = new DirectoryInfo(path); current != null; current = current.Parent)
                if ((current.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Reparse ancestry is not an owned save root.");
        }

        private static string ReadOwnedHash(string path)
        {
            RequireDirectory(Path.GetDirectoryName(path));
            if ((File.GetAttributes(path) & (FileAttributes.ReparsePoint | FileAttributes.Directory)) != 0)
                throw new IOException("Save leaf must be a regular file.");
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (!GetFileInformationByHandle(stream.SafeFileHandle, out var identity) ||
                    identity.NumberOfLinks != 1 || (identity.Attributes & (uint)FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Save file identity is unavailable or multiply linked.");
                using (var sha = SHA256.Create())
                    return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FileIdentity
        {
            public uint Attributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME CreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWriteTime;
            public uint VolumeSerialNumber;
            public uint FileSizeHigh;
            public uint FileSizeLow;
            public uint NumberOfLinks;
            public uint FileIndexHigh;
            public uint FileIndexLow;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetFileInformationByHandle(SafeFileHandle file, out FileIdentity information);
    }
}
