using System;
using System.IO;
using System.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class PersistenceSaveAuthorization
    {
        private RuntimeRequest assistedRequest;
        private object assistedGame, assistedManager, assistedGateOwner;
        private string assistedKey, assistedArea, assistedPhase;
        private Func<RuntimeSaveTarget, string> assistedNativeGate;
        private int assistedManualNumber, assistedReservations, assistedLevelupWrites;
        internal string AssistedPhase { get { lock (sync) return assistedPhase; } }
        internal int AssistedManualNumber { get { lock (sync) return assistedManualNumber; } }
        internal int AssistedReservationCount { get { lock (sync) return assistedReservations; } }
        internal int AssistedLevelupWrites { get { lock (sync) return assistedLevelupWrites; } }
        internal string AssistedManualLeaf { get { lock (sync) return assistedManualNumber == 0 ? null : Chunk7AssistedFixtureSelection.ManualLeaf(assistedManualNumber); } }
        // The closed family admits only these typed scalar DTO fields. The
        // property-shape regression fails if any admitted DTO gains a new member.
        // Length-prefixed strings preserve null/empty/delimiter distinctions;
        // fixed type tags and invariant numbers avoid coercion or culture aliases.
        private static string AssistedKey(RuntimeRequest value)
        {
            if(value==null||value.Scenario!=Chunk7AssistedFixtureSelection.Scenario||value.AssistedFixture==null||value.Validate().Count!=0)return null;
            var key=new System.Text.StringBuilder("assisted-request-key-v1;");
            AssistedScalar(key,value.SchemaVersion);AssistedScalar(key,value.RunId);AssistedScalar(key,value.Scenario);
            AssistedScalar(key,value.Branch);AssistedScalar(key,value.Commit);AssistedScalar(key,value.ProductVersion);
            AssistedScalar(key,value.DllSha256);AssistedScalar(key,value.DllMvid);AssistedScalar(key,value.TransactionToken);AssistedScalar(key,value.EvidenceRoot);
            AssistedScalar(key,value.SaveAccessAllowed);AssistedScalar(key,value.SaveName);AssistedScalar(key,value.ObserveWorkingDeserialization);
            AssistedScalar(key,value.Schema1DerivationSha256);AssistedScalar(key,value.PersistenceCase);
            AssistedDescriptor(key,value.Fixture.Baseline);AssistedDescriptor(key,value.Fixture.Working);
            var write=value.Fixture.WriteAuthorization;
            AssistedScalar(key,write.Mode);AssistedScalar(key,write.AllowedInternalName);AssistedScalar(key,write.AllowedFileName);AssistedScalar(key,write.BaselineImmutable);
            AssistedScalar(key,value.QualificationSuite.SuiteId);AssistedScalar(key,value.QualificationSuite.SnapshotSha256);
            var selected=value.AssistedFixture;
            AssistedScalar(key,selected.Case);AssistedScalar(key,selected.OwnerAuthorizationSha256);AssistedScalar(key,selected.BaseGameOnly);AssistedScalar(key,selected.AssistedSetupOnly);
            AssistedScalar(key,selected.StoryPolicy);AssistedScalar(key,selected.AssistancePolicy);AssistedScalar(key,selected.ExpectedNativeLoadCount);AssistedScalar(key,selected.MaximumNativeWriteCount);
            AssistedScalar(key,selected.TargetRangerLevel);AssistedScalar(key,selected.MaximumTargetLevel);AssistedScalar(key,selected.MaximumStoryChoices);AssistedScalar(key,selected.MaximumStorySeconds);
            AssistedScalar(key,selected.MaximumExperienceShortfall);AssistedScalar(key,selected.MaximumMoneyShortfall);AssistedScalar(key,selected.NativeInitialAutoLeaf);AssistedScalar(key,selected.NativeManualName);
            return key.ToString();
        }
        private static void AssistedDescriptor(System.Text.StringBuilder key,RuntimeSaveDescriptor value)
        {
            AssistedScalar(key,value.InternalName);AssistedScalar(key,value.FileName);AssistedScalar(key,value.Sha256);AssistedScalar(key,value.Length);
            AssistedScalar(key,value.LastWriteTimeUtcTicks);AssistedScalar(key,value.GameId);AssistedScalar(key,value.GameName);AssistedScalar(key,value.Area);
        }
        private static void AssistedScalar(System.Text.StringBuilder key,string value)
        {
            if(value==null){key.Append("S-1:");return;}
            key.Append('S').Append(value.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(':').Append(value);
        }
        private static void AssistedScalar(System.Text.StringBuilder key,int value)=>key.Append('I').Append(value.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(';');
        private static void AssistedScalar(System.Text.StringBuilder key,long value)=>key.Append('L').Append(value.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(';');
        private static void AssistedScalar(System.Text.StringBuilder key,bool value)=>key.Append(value?"B1;":"B0;");
        private static void AssistedScalar(System.Text.StringBuilder key,bool? value){if(value.HasValue){key.Append('?');AssistedScalar(key,value.Value);}else key.Append("N;");}
        private string AssistedWriteLeaseAdmission(RuntimeSaveTarget target)
        {
            if(assistedRequest==null)return null;
            if(!AssistedUnchanged())return "Assisted request changed before storage acquisition.";
            var required=assistedPhase=="initial-auto"?1:assistedPhase=="levelup-auto"?2:assistedPhase=="manual"?2+assistedLevelupWrites:-1;
            if(required<0||admittedNativeWriteRequests!=required)return "Assisted storage requires its preceding exact native request.";
            var leaf=assistedPhase=="initial-auto"?Chunk7AssistedFixtureSelection.InitialAutoLeaf:assistedPhase=="levelup-auto"?Chunk7AssistedFixtureSelection.LevelupStagingLeaf:AssistedManualLeaf;
            return target?.FileName==leaf?null:"Assisted prepared descriptor selected a foreign phase leaf.";
        }
        private bool AssistedUnchanged() => assistedRequest == null || assistedKey == AssistedKey(assistedRequest) && assistedRequest.Validate().Count == 0;
        internal void ConfigureAssistedFixture(RuntimeRequest request, object game, object manager, string area)
        {
            lock(sync)
            {
                if(assistedRequest!=null || request?.AssistedFixture==null || request.Validate().Count!=0 || game==null || manager==null ||
                    maximumNativeLoadRequests!=0 || maximumNativeWriteRequests!=3 || acquiredManualRequest!=null || ownedPairsRequest!=null ||
                    entries.Count!=2 || !declaresBootstrap || bootstrapWindowOpen || BootstrapFreezeCount!=0 ||
                    admittedNativeLoadRequests!=0 || admittedNativeWriteRequests!=0)
                    throw new InvalidOperationException("Assisted fixture requires its unused closed zero-load/three-write bootstrap scope.");
                Entry anchor,auto;var source=request.Fixture.Working;
                if(source.GameId!=gameId||source.GameName!=gameName||!entries.TryGetValue(source.FileName,out anchor)||anchor.Writable||anchor.Bootstrap||anchor.Hash!=source.Sha256||
                    anchor.Name!=source.InternalName||anchor.Area!=source.Area||anchor.Type!="Manual"||
                    !entries.TryGetValue(Chunk7AssistedFixtureSelection.InitialAutoLeaf,out auto)||!auto.Bootstrap||!auto.Writable||!auto.AdmitsBeforeArea||
                    auto.Type!="Auto"||auto.Area!=area||auto.Hash!=null||auto.AdmissionArea!=null)
                    throw new InvalidOperationException("Assisted initial Auto and immutable Working anchor differ.");
                assistedRequest=request;assistedGame=game;assistedManager=manager;assistedArea=area;assistedKey=AssistedKey(request);assistedPhase="initial-auto";
            }
        }
        internal void BindAssistedNativeGate(RuntimeRequest request,object game,object manager,object owner,Func<RuntimeSaveTarget,string> gate)
        {
            lock(sync)
            {
                RequireAssistedContext(request,game,manager);
                if(assistedPhase!="initial-auto"||assistedGateOwner!=null||owner==null||gate==null||admittedNativeWriteRequests!=0)
                    throw new InvalidOperationException("Assisted native admission gate must be retained once before initial dispatch.");
                assistedGateOwner=owner;assistedNativeGate=gate;
            }
        }
        internal bool MatchesAssistedFixturePlan(RuntimeRequest request,object game,object manager)
        {
            lock(sync) return assistedRequest!=null&&ReferenceEquals(assistedRequest,request)&&ReferenceEquals(assistedGame,game)&&ReferenceEquals(assistedManager,manager)&&
                AssistedUnchanged()&&maximumNativeLoadRequests==0&&maximumNativeWriteRequests==3&&admittedNativeLoadRequests==0&&admittedNativeWriteRequests<=3&&
                assistedPhase!=null&&entries.Count>=2&&entries.Count<=4;
        }
        private void RequireAssistedContext(RuntimeRequest request,object game,object manager)
        {if(!MatchesAssistedFixturePlan(request,game,manager))throw new InvalidOperationException("Assisted fixture lost its captured request and native authority objects.");}
        private string AssistedNativeAdmission(RuntimeSaveOperation operation,RuntimeSaveTarget target)
        {
            if(assistedRequest==null)return null;
            if(!AssistedUnchanged())return "Assisted fixture captured request changed.";
            if(operation!=RuntimeSaveOperation.Write)return "Assisted fixture never authorizes a native load or deletion request.";
            if(assistedNativeGate==null||assistedGateOwner==null)return "Assisted fixture has no retained native call-origin gate.";
            var expected=assistedPhase=="initial-auto"?0:assistedPhase=="levelup-auto"?1:assistedPhase=="manual"?1+assistedLevelupWrites:-1;
            if(expected<0||admittedNativeWriteRequests!=expected)return "Assisted native request is outside its one-shot phase quota.";
            var leaf=assistedPhase=="manual"?AssistedManualLeaf:Chunk7AssistedFixtureSelection.InitialAutoLeaf;
            if(target==null||target.FileName!=leaf)return "Assisted native request selected a foreign phase leaf.";
            return assistedNativeGate(target);
        }
        private string AssistedStorageAdmission(RuntimeSaveOperation operation,RuntimeSaveTarget target,bool checkLoadAdmission)
        {
            if(assistedRequest==null)return null;
            if(!AssistedUnchanged())return "Assisted fixture captured request changed.";
            if(operation==RuntimeSaveOperation.Load)return checkLoadAdmission?"Assisted fixture permits no native load.":null;
            if(operation!=RuntimeSaveOperation.Write)return "Assisted fixture does not expose deletion authority.";
            var file=target?.FileName;
            if(assistedPhase=="initial-auto"&&file==Chunk7AssistedFixtureSelection.InitialAutoLeaf&&admittedNativeWriteRequests<=1)return null;
            if(assistedPhase=="levelup-auto"&&(file==Chunk7AssistedFixtureSelection.InitialAutoLeaf||file==Chunk7AssistedFixtureSelection.LevelupStagingLeaf)&&admittedNativeWriteRequests<=2)return null;
            if(assistedPhase=="manual"&&file==AssistedManualLeaf&&admittedNativeWriteRequests<=2+assistedLevelupWrites)return null;
            return "Assisted storage target is not owned by the current native write phase.";
        }
        internal void ConfirmAssistedInitialAuto(RuntimeRequest request,object game,object manager,RuntimeSaveTarget target)
        {
            lock(sync)
            {
                RequireAssistedContext(request,game,manager);
                if(assistedPhase!="initial-auto"||admittedNativeWriteRequests!=1||BootstrapFreezeCount!=1||bootstrapWindowOpen||
                    target?.FileName!=Chunk7AssistedFixtureSelection.InitialAutoLeaf||ValidateArchiveIdentity(target,Root)!=null)
                    throw new InvalidOperationException("Initial assisted Auto lacks exact completed minted-campaign storage.");
                entries[Chunk7AssistedFixtureSelection.InitialAutoLeaf].Writable=false;assistedPhase="setup";
            }
        }
        internal void ReserveAssistedLevelup(RuntimeRequest request,object game,object manager,RuntimeSaveTarget initial)
        {
            lock(sync)
            {
                RequireAssistedContext(request,game,manager);
                if(assistedPhase!="setup"||assistedLevelupWrites!=0||assistedManualNumber!=0||admittedNativeWriteRequests!=1||entries.Count!=2||
                    initial?.FileName!=Chunk7AssistedFixtureSelection.InitialAutoLeaf||ValidateArchiveIdentity(initial,Root)!=null)
                    throw new InvalidOperationException("Assisted level-up reservation requires its completed initial Auto only.");
                var prior=entries[Chunk7AssistedFixtureSelection.InitialAutoLeaf];
                var next=new Entry{Name=prior.Name,Type="Auto",Area=assistedArea,Writable=true,Bootstrap=true};
                VerifyFile(Chunk7AssistedFixtureSelection.LevelupStagingLeaf,next);
                RequireAssistedInventory(false);
                entries.Add(Chunk7AssistedFixtureSelection.LevelupStagingLeaf,next);prior.Writable=true;assistedPhase="levelup-auto";assistedReservations++;
            }
        }
        internal void ConfirmAssistedLevelup(RuntimeRequest request,object game,object manager,RuntimeSaveTarget settled)
        {
            lock(sync)
            {
                RequireAssistedContext(request,game,manager);
                if(assistedPhase!="levelup-auto"||admittedNativeWriteRequests!=2||settled?.FileName!=Chunk7AssistedFixtureSelection.InitialAutoLeaf||
                    ValidateArchiveIdentity(settled,Root)!=null||entries[Chunk7AssistedFixtureSelection.LevelupStagingLeaf].Hash!=null||
                    entries[Chunk7AssistedFixtureSelection.LevelupStagingLeaf].Writing)
                    throw new InvalidOperationException("Assisted level-up Auto replacement has not completed exactly.");
                RequireAssistedInventory(false);entries[Chunk7AssistedFixtureSelection.InitialAutoLeaf].Writable=false;
                entries[Chunk7AssistedFixtureSelection.LevelupStagingLeaf].Writable=false;assistedLevelupWrites=1;assistedPhase="setup";
            }
        }
        internal void ReserveAssistedManual(RuntimeRequest request,object game,object manager,int observedNumber)
        {
            lock(sync)
            {
                RequireAssistedContext(request,game,manager);
                if(assistedPhase!="setup"||assistedManualNumber!=0||observedNumber<=0||admittedNativeWriteRequests!=1+assistedLevelupWrites)
                    throw new InvalidOperationException("Assisted final Manual reservation is stale, repeated or out of order.");
                RequireAssistedInventory(false);var leaf=Chunk7AssistedFixtureSelection.ManualLeaf(observedNumber);
                var output=new Entry{Name=Chunk7AssistedFixtureSelection.OutputName,Type="Manual",Area=assistedArea,Writable=true,Bootstrap=true};
                VerifyFile(leaf,output);if(entries.ContainsKey(leaf))throw new InvalidOperationException("Assisted final Manual aliases an admitted leaf.");
                entries.Add(leaf,output);assistedManualNumber=observedNumber;assistedReservations++;assistedPhase="manual";
            }
        }
        internal void ConfirmAssistedManual(RuntimeRequest request,object game,object manager,RuntimeSaveTarget target)
        {
            lock(sync)
            {
                RequireAssistedContext(request,game,manager);
                if(assistedPhase!="manual"||admittedNativeWriteRequests!=2+assistedLevelupWrites||target?.FileName!=AssistedManualLeaf||ValidateArchiveIdentity(target,Root)!=null)
                    throw new InvalidOperationException("Assisted final Manual lacks its exact native completion.");
                RequireAssistedInventory(true);entries[AssistedManualLeaf].Writable=false;assistedPhase="complete";
            }
        }
        private void RequireAssistedInventory(bool manual)
        {
            var expected=new[]{assistedRequest.Fixture.Working.FileName,Chunk7AssistedFixtureSelection.InitialAutoLeaf};
            if(manual)expected=expected.Concat(new[]{AssistedManualLeaf}).ToArray();
            var paths=Directory.EnumerateFileSystemEntries(Root).ToArray();
            if(paths.Length!=expected.Length||paths.Any(p=>Directory.Exists(p)||!expected.Contains(Path.GetFileName(p))))
                throw new InvalidOperationException("Assisted isolated inventory contains a foreign, pending or missing leaf.");
            foreach(var leaf in expected)VerifyFile(leaf,entries[leaf]);
        }
        internal void CloseAssistedNativeGate(object owner)
        {
            lock(sync)
            {
                if(assistedRequest==null||!ReferenceEquals(owner,assistedGateOwner))throw new InvalidOperationException("Assisted native gate belongs to another caller.");
                if(entries.Values.Any(e=>e.Writing))throw new InvalidOperationException("Assisted storage still retains its native writer.");
                assistedNativeGate=null;assistedGateOwner=null;assistedPhase="closed";
            }
        }
    }
}
