using System;
using System.Linq;
using System.Text;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class PersistenceSaveAuthorization
    {
        private RuntimeRequest assistedColdRequest;
        private object assistedColdGame, assistedColdManager;
        private string assistedColdKey;
        internal int AssistedColdEntryCount { get { lock(sync) return entries.Count; } }
        internal int AssistedColdWritableEntries { get { lock(sync) return entries.Values.Count(value => value.Writable); } }
        private static string AssistedColdKey(RuntimeRequest value)
        {
            if (value == null || value.Scenario != Chunk7AssistedColdInventorySelection.Scenario || value.AssistedColdInventory == null || value.Validate().Count != 0) return null;
            var key = new StringBuilder("assisted-cold-request-key-v1;");
            AssistedScalar(key,value.SchemaVersion); AssistedScalar(key,value.RunId); AssistedScalar(key,value.Scenario);
            AssistedScalar(key,value.Branch); AssistedScalar(key,value.Commit); AssistedScalar(key,value.ProductVersion);
            AssistedScalar(key,value.DllSha256); AssistedScalar(key,value.DllMvid); AssistedScalar(key,value.TransactionToken); AssistedScalar(key,value.EvidenceRoot);
            AssistedScalar(key,value.SaveAccessAllowed); AssistedScalar(key,value.SaveName); AssistedScalar(key,value.ObserveWorkingDeserialization);
            AssistedScalar(key,value.Schema1DerivationSha256); AssistedScalar(key,value.PersistenceCase);
            AssistedDescriptor(key,value.Fixture.Baseline); AssistedDescriptor(key,value.Fixture.Working); AssistedDescriptor(key,value.PersistenceLoad);
            var write=value.Fixture.WriteAuthorization;
            AssistedScalar(key,write.Mode); AssistedScalar(key,write.AllowedInternalName); AssistedScalar(key,write.AllowedFileName); AssistedScalar(key,write.BaselineImmutable);
            AssistedScalar(key,value.QualificationSuite.SuiteId); AssistedScalar(key,value.QualificationSuite.SnapshotSha256);
            var selected=value.AssistedColdInventory;
            AssistedScalar(key,selected.Case); AssistedScalar(key,selected.SourceRunId); AssistedScalar(key,selected.SourceArtifactSha256); AssistedScalar(key,selected.SourceArchiveSha256);
            AssistedScalar(key,selected.BaseGameOnly); AssistedScalar(key,selected.ExpectedNativeLoadCount); AssistedScalar(key,selected.ExpectedNativeWriteCount);
            if (selected.IsOwnerSeed) { AssistedScalar(key,selected.SeedId); AssistedScalar(key,selected.SeedSha256); }
            return key.ToString();
        }
        private bool AssistedColdUnchanged() => assistedColdRequest == null || assistedColdKey == AssistedColdKey(assistedColdRequest) && assistedColdRequest.Validate().Count == 0;
        private bool AssistedColdEntry(RuntimeSaveDescriptor source, Chunk7AssistedColdInventorySelection selected)
        {
            Entry entry;
            // Owner seeds admit their registry-supplied exact names; the producer leaf/name test is unchanged.
            var named = selected?.IsOwnerSeed == true
                ? Chunk7AssistedColdInventorySelection.IsOwnerSeedLeaf(source?.FileName) && Chunk7AssistedColdInventorySelection.IsOwnerSeedName(source?.InternalName)
                : selected?.IsSmallFixture == true
                    ? Chunk7SmallFixtureSelection.IsManualLeaf(source?.FileName) && source.InternalName == Chunk7SmallFixtureSelection.OutputName
                    : selected?.IsThirdHorse == true
                        ? source?.FileName == "Manual_305_KMC_C7_SMALL_WORLD.zks" && source.InternalName == Chunk7OwnedPairsSelection.SmallFixtureManualName
                    : selected?.IsFourthHorseSource == true
                        ? source?.FileName == "Manual_306_KMC_C7_FOUR_HORSE_WORLD.zks" && source.InternalName == Chunk7OwnedPairsSelection.FourthHorseManualName
                    : selected?.IsMediumFixture == true
                        ? Chunk7SmallFixtureSelection.IsMediumManualLeaf(source?.FileName) && source.InternalName == Chunk7SmallFixtureSelection.MediumOutputName
                    : Chunk7AssistedColdInventorySelection.IsManualLeaf(source?.FileName) && source.InternalName == Chunk7AssistedColdInventorySelection.ManualName;
            return source != null && named &&
                source.GameId == gameId && source.GameName == gameName && maximumNativeLoadRequests == 1 && !maximumNativeWriteRequests.HasValue &&
                !declaresBootstrap && !bootstrapWindowOpen && entries.Count == 1 && entries.TryGetValue(source.FileName,out entry) &&
                entry.Name == source.InternalName && entry.Type == "Manual" && entry.Area == source.Area && entry.Hash == source.Sha256 &&
                !entry.Writable && !entry.Writing && !entry.Bootstrap && !entry.AdmitsBeforeArea && entry.AdmissionArea == null && entry.ForeignGameId == null && entry.ForeignGameName == null &&
                admittedNativeLoadRequests <= 1 && admittedNativeWriteRequests == 0;
        }
        internal void ConfigureAssistedColdInventory(RuntimeRequest request, object game, object manager)
        {
            lock(sync)
            {
                var key=AssistedColdKey(request);
                if (assistedColdRequest != null || key == null || game == null || manager == null || assistedRequest != null || acquiredManualRequest != null || ownedPairsRequest != null ||
                    admittedNativeLoadRequests != 0 || admittedNativeWriteRequests != 0 || !AssistedColdEntry(request.PersistenceLoad, request.AssistedColdInventory))
                    throw new InvalidOperationException("Assisted cold inventory requires its unused exact one-Manual one-load/no-write plan.");
                assistedColdRequest=request; assistedColdGame=game; assistedColdManager=manager; assistedColdKey=key;
            }
        }
        internal bool MatchesAssistedColdInventoryPlan(RuntimeRequest request, object game, object manager)
        {
            lock(sync) return assistedColdRequest != null && ReferenceEquals(assistedColdRequest,request) && ReferenceEquals(assistedColdGame,game) &&
                ReferenceEquals(assistedColdManager,manager) && AssistedColdUnchanged() && AssistedColdEntry(request.PersistenceLoad, request.AssistedColdInventory);
        }
        private string AssistedColdAdmission(RuntimeSaveOperation operation)
        {
            if (assistedColdRequest == null) return null;
            if (!AssistedColdUnchanged()) return "Assisted cold captured request changed.";
            return operation == RuntimeSaveOperation.Load ? null : "Assisted cold inventory permits no write or deletion.";
        }
    }
}
