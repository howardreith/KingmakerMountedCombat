using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Diagnostics
{
    // Only the not-yet-wired common request and storage boundary are facades.
    // The closed selection, preflight, new authority partial and body methods
    // below are the exact implementation files/methods under review.
    public sealed class RuntimeSaveDescriptor
    {
        public string InternalName { get; set; } public string FileName { get; set; } public string Sha256 { get; set; }
        public long Length { get; set; } public long LastWriteTimeUtcTicks { get; set; }
        public string GameId { get; set; } public string GameName { get; set; } public string Area { get; set; }
    }
    public sealed class RuntimeFixtureIdentity { public RuntimeSaveDescriptor Baseline { get; set; } public RuntimeSaveDescriptor Working { get; set; } public RuntimeSaveWriteAuthorization WriteAuthorization { get; set; } }
    public sealed class RuntimeSaveWriteAuthorization { public string Mode { get; set; } }
    public sealed class RuntimeQualificationSuiteIdentity { public string SuiteId { get; set; } }
    public sealed class RuntimeRequest
    {
        public string Scenario { get; set; } public string RunId { get; set; } public string TransactionToken { get; set; }
        public string EvidenceRoot { get; set; } public string UnselectedMarker { get; set; }
        public RuntimeFixtureIdentity Fixture { get; set; } public object ForeignFamily { get; set; }
        public RuntimeSaveDescriptor PersistenceLoad { get; set; } public Chunk7SmallFixtureSelection SmallFixture { get; set; }
        public Chunk7StockMammothFixtureSelection StockMammothFixture { get; set; }
        public IReadOnlyList<string> Validate() => SmallFixture != null && StockMammothFixture != null ? new[] { "mixed fixture" } :
            StockMammothFixture != null ? StockMammothFixture.Validate(Scenario, RunId) :
            SmallFixture == null ? new[] { "missing" } : SmallFixture.Validate(Scenario, RunId);
    }
    internal enum RuntimeSaveOperation { Load, Write, Delete }
    internal static class Chunk7EvidenceDto
    {
        internal static JObject Request(object value) => JObject.FromObject(value,
            new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() });
    }
    internal sealed partial class PersistenceSaveAuthorization
    {
        private sealed class Entry
        { internal string Name, Type, Area, Hash, AdmissionArea, ForeignGameId, ForeignGameName; internal bool Writable, Writing, Bootstrap, AdmitsBeforeArea; }
        private readonly object sync = new object();
        private readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
        private string gameId, gameName;
        private int? maximumNativeLoadRequests = 1, maximumNativeWriteRequests = 1;
        private int admittedNativeLoadRequests, admittedNativeWriteRequests;
        private bool declaresBootstrap, bootstrapWindowOpen;
        private object acquiredManualRequest = null, ownedPairsRequest = null, assistedRequest = null, assistedColdRequest = null, progressionManualRequest = null;
        internal string Root { get; private set; }
        internal int Verifications;
        internal PersistenceSaveAuthorization(string root, RuntimeSaveDescriptor source)
        {
            Root = root; gameId = source.GameId; gameName = source.GameName;
            entries.Add(source.FileName, new Entry { Name = source.InternalName, Type = "Manual", Area = source.Area, Hash = source.Sha256 });
        }
        // Facade for the external common storage check: exact native output must
        // still be absent; production's reparse/hash/allowlist checks are not claimed.
        private void VerifyFile(string leaf, Entry output)
        { Verifications++; if (File.Exists(Path.Combine(Root, leaf)) || Directory.Exists(Path.Combine(Root, leaf))) throw new InvalidOperationException("output exists"); }
        internal void Loaded() { admittedNativeLoadRequests = 1; }
        internal void ExtraLoad() { admittedNativeLoadRequests = 2; }
        internal void ExtraWrite() { admittedNativeWriteRequests = 2; }
        internal void SetCaps(int loads, int writes) { maximumNativeLoadRequests = loads; maximumNativeWriteRequests = writes; }
        internal void ForeignEntry() { entries.Add("foreign.zks", new Entry()); }
        internal string Admission(RuntimeSaveOperation operation) => stockMammothFixtureRequest == null ? SmallFixtureAdmission(operation) : StockMammothFixtureAdmission(operation);
    }
    internal sealed partial class Chunk7SmallFixtureScenario
    {
        __BODY_METHODS__
        private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
    }
    internal static class SmallFixtureAdapterTests
    {
        private static int pass, fail;
        private static string root;
        private static void Good(string name, Action test)
        { try { test(); pass++; Console.WriteLine("PASS " + name); } catch (Exception e) { fail++; Console.WriteLine("FAIL " + name + ": " + e); } }
        private static void Bad(string name, Action test)
        { try { test(); fail++; Console.WriteLine("FAIL accepted " + name); } catch { pass++; Console.WriteLine("PASS refuses " + name); } }
        private static void Assert(bool value) { if (!value) throw new InvalidOperationException("assertion"); }
        private static RuntimeRequest Fresh() => new RuntimeRequest {
            RunId = "synthetic-f09-adapter", Scenario = Chunk7SmallFixtureSelection.Scenario, TransactionToken = new string('a',64), EvidenceRoot = root,
            SmallFixture = new Chunk7SmallFixtureSelection { Case = Chunk7SmallFixtureSelection.CaseName, SeedId = Chunk7SmallFixtureSelection.SeedIdentity,
                SeedSha256 = Chunk7SmallFixtureSelection.SeedHash, SeedQualificationRunId = Chunk7SmallFixtureSelection.QualificationRun,
                SeedQualificationArtifactSha256 = Chunk7SmallFixtureSelection.QualificationHash, NativeInventoryRunId = Chunk7SmallFixtureSelection.InventoryRun,
                NativeInventoryArtifactSha256 = Chunk7SmallFixtureSelection.InventoryHash, BaseGameOnly = true,
                ExpectedNativeLoadCount = 1, ExpectedNativeWriteCount = 1, TargetRangerLevel = 4 },
            PersistenceLoad = new RuntimeSaveDescriptor { InternalName = Chunk7SmallFixtureSelection.SeedInternalName,
                FileName = Chunk7SmallFixtureSelection.SeedFileName, Sha256 = Chunk7SmallFixtureSelection.SeedHash, Length = 1239025,
                LastWriteTimeUtcTicks = 1, GameId = Chunk7SmallFixtureSelection.CampaignId, GameName = Chunk7SmallFixtureSelection.CampaignName,
                Area = Chunk7SmallFixtureSelection.AreaId } };
        private static RuntimeRequest FreshStock()
        {
            var r = Fresh(); r.RunId = "synthetic-stock-mammoth-adapter";
            r.Scenario = Chunk7StockMammothFixtureSelection.Scenario; r.SmallFixture = null;
            r.StockMammothFixture = new Chunk7StockMammothFixtureSelection {
                Case = Chunk7StockMammothFixtureSelection.CaseName,
                SourceSeedId = Chunk7StockMammothFixtureSelection.SeedId,
                SourceSeedSha256 = Chunk7StockMammothFixtureSelection.SeedSha256,
                SourceQualificationRunId = Chunk7StockMammothFixtureSelection.SeedQualificationRunId,
                SourceQualificationSha256 = Chunk7StockMammothFixtureSelection.SeedQualificationSha256,
                SourceInventoryRunId = Chunk7StockMammothFixtureSelection.NativeInventoryRunId,
                SourceInventorySha256 = Chunk7StockMammothFixtureSelection.NativeInventorySha256,
                BaseGameOnly = true, ExpectedNativeLoadCount = 1, ExpectedNativeWriteCount = 1,
                TargetRangerLevel = 10, NativeXpAssistance = 96000 };
            return r;
        }
        private static SmallRiderBodyObservation Body() => new SmallRiderBodyObservation {
            OriginalSizeOrdinal=3,EffectiveSizeOrdinal=3,MountBlueprintGuid=DeclaredSmallRiderBody.HorseBlueprintGuid,MountSizeOrdinal=5,
            RaceGuid=Chunk7SmallFixtureSelection.RaceGuid,PresetGuid=Chunk7SmallFixtureSelection.PresetGuid,Gender="Male",SkeletonName="MaleHalfling",
            NativeRaceAndPresetIdentityExact=true,NativeSkeletonIsExactPreset=true,NativeRigAvailable=true,AvatarLocalScale=new PoseVector3(1,1,1),
            DollEquipmentEntityIds=new[]{new string('a',32)},CurrentEquipmentBlueprints=new string[12],ActiveVisualEquipmentNames=new[]{"native-clothes"},AdditionalLimbCount=0 };
        private static void ConfigureMutation(string name, Action<RuntimeRequest> mutation)
        { Good(name,()=>{var r=Fresh();var a=new PersistenceSaveAuthorization(root,r.PersistenceLoad);var g=new object();var m=new object();a.ConfigureSmallFixture(r,g,m);mutation(r);Assert(!a.MatchesSmallFixturePlan(r,g,m));Assert(a.Admission(RuntimeSaveOperation.Load)!=null);}); }
        public static int Main(string[] args)
        {
            root=args[0]; Directory.CreateDirectory(root);
            // A source sentinel exercises exact root inventory without a game save.
            File.WriteAllText(Path.Combine(root,Chunk7SmallFixtureSelection.SeedFileName),"synthetic storage sentinel, not a save");
            Good("stock Mammoth exact selection and clean input",()=>{var r=FreshStock();Assert(r.Validate().Count==0&&Chunk7StockMammothFixtureSelection.IsSource(r.PersistenceLoad));Assert(typeof(Chunk7StockMammothFixtureSelection).GetProperties().Length==12);});
            foreach(var p in typeof(Chunk7StockMammothFixtureSelection).GetProperties())
                Good("stock Mammoth selection rejects mutation "+p.Name,()=>{var r=FreshStock();var value=p.GetValue(r.StockMammothFixture,null);p.SetValue(r.StockMammothFixture,p.PropertyType==typeof(string)?(object)((string)value+"x"):p.PropertyType==typeof(bool)?(object)!(bool)value:(int)value+1,null);Assert(r.Validate().Count>0);});
            Good("stock Mammoth exact load then native Manual reservation",()=>{var r=FreshStock();var g=new object();var m=new object();var a=new PersistenceSaveAuthorization(root,r.PersistenceLoad);a.ConfigureStockMammothFixture(r,g,m);Assert(a.MatchesStockMammothFixturePlan(r,g,m)&&a.Admission(RuntimeSaveOperation.Write)!=null);a.Loaded();a.ReserveStockMammothFixtureOutput(r,g,m,304);Assert(a.StockMammothFixtureReservations==1&&a.StockMammothFixtureNumber==304&&a.StockMammothFixtureOutputLeaf==Chunk7StockMammothFixtureSelection.ManualLeaf(304)&&a.Admission(RuntimeSaveOperation.Write)==null&&a.Admission(RuntimeSaveOperation.Delete)!=null);});
            Bad("stock Mammoth reservation before load",()=>{var r=FreshStock();var g=new object();var m=new object();var a=new PersistenceSaveAuthorization(root,r.PersistenceLoad);a.ConfigureStockMammothFixture(r,g,m);a.ReserveStockMammothFixtureOutput(r,g,m,304);});
            Bad("stock Mammoth second reservation",()=>{var r=FreshStock();var g=new object();var m=new object();var a=new PersistenceSaveAuthorization(root,r.PersistenceLoad);a.ConfigureStockMammothFixture(r,g,m);a.Loaded();a.ReserveStockMammothFixtureOutput(r,g,m,304);a.ReserveStockMammothFixtureOutput(r,g,m,305);});
            Bad("stock Mammoth foreign manager reservation",()=>{var r=FreshStock();var g=new object();var m=new object();var a=new PersistenceSaveAuthorization(root,r.PersistenceLoad);a.ConfigureStockMammothFixture(r,g,m);a.Loaded();a.ReserveStockMammothFixtureOutput(r,g,new object(),304);});
            Good("stock Mammoth extra load and write revoke plan",()=>{var r=FreshStock();var g=new object();var m=new object();var a=new PersistenceSaveAuthorization(root,r.PersistenceLoad);a.ConfigureStockMammothFixture(r,g,m);a.ExtraLoad();Assert(!a.MatchesStockMammothFixturePlan(r,g,m));var b=new PersistenceSaveAuthorization(root,r.PersistenceLoad);b.ConfigureStockMammothFixture(r,g,m);b.Loaded();b.ReserveStockMammothFixtureOutput(r,g,m,304);b.ExtraWrite();Assert(!b.MatchesStockMammothFixturePlan(r,g,m));});
            Good("stock Mammoth captured request drift refuses write",()=>{var r=FreshStock();var g=new object();var m=new object();var a=new PersistenceSaveAuthorization(root,r.PersistenceLoad);a.ConfigureStockMammothFixture(r,g,m);a.Loaded();a.ReserveStockMammothFixtureOutput(r,g,m,304);r.StockMammothFixture.NativeXpAssistance++;Assert(!a.MatchesStockMammothFixturePlan(r,g,m)&&a.Admission(RuntimeSaveOperation.Write)!=null);});
            Good("closed eleven-property DTO and exact source",()=>{var r=Fresh();Assert(r.Validate().Count==0&&Chunk7SmallFixtureSelection.IsSource(r.PersistenceLoad));Assert(typeof(Chunk7SmallFixtureSelection).GetProperties().Length==11);});
            foreach(var p in typeof(Chunk7SmallFixtureSelection).GetProperties())
            {
                Good("selection rejects mutation "+p.Name,()=>{var r=Fresh();var value=p.GetValue(r.SmallFixture,null);p.SetValue(r.SmallFixture,p.PropertyType==typeof(string)?(object)((string)value+"x"):p.PropertyType==typeof(bool)?(object)!(bool)value:(int)value+1,null);Assert(r.Validate().Count>0);});
            }
            Good("historical F10 family cannot select F09",()=>{var r=Fresh();r.Scenario="chunk7-native-progression";Assert(r.Validate().Count>0);});
            foreach(var n in new[]{0,-1,int.MaxValue}){Bad("invalid Manual number "+n,()=>Chunk7SmallFixtureSelection.ManualLeaf(n));}
            Good("canonical native Manual leaf",()=>Assert(Chunk7SmallFixtureSelection.IsManualLeaf("Manual_304_KMC_C7_SMALL_FIXTURE.zks")));
            foreach(var leaf in new[]{"Manual_0304_KMC_C7_SMALL_FIXTURE.zks","Manual_0_KMC_C7_SMALL_FIXTURE.zks","Manual_2147483647_KMC_C7_SMALL_FIXTURE.zks","Auto_1.zks","Manual_1_KMC_C7_ACQUIRED_FIELD.zks"}){Good("leaf refuses "+leaf,()=>Assert(!Chunk7SmallFixtureSelection.IsManualLeaf(leaf)));}
            Good("unassisted native main preflight",()=>Chunk7SmallFixtureAdmission.RequireMain(3,3,9000,4,true,false,400057,8000,4));
            Bad("main insufficient XP",()=>Chunk7SmallFixtureAdmission.RequireMain(3,3,5000,3,true,false,400057,8000,4));
            Bad("main additional native levels",()=>Chunk7SmallFixtureAdmission.RequireMain(3,3,15000,5,true,false,400057,8000,4));
            Bad("main existing pet",()=>Chunk7SmallFixtureAdmission.RequireMain(3,3,9000,4,true,true,400057,8000,4));
            Bad("main native not eligible",()=>Chunk7SmallFixtureAdmission.RequireMain(3,3,9000,4,false,false,400057,8000,4));
            Bad("main hire shortfall",()=>Chunk7SmallFixtureAdmission.RequireMain(3,3,9000,4,true,false,7999,8000,4));
            Good("native paid hire preflight",()=>Chunk7SmallFixtureAdmission.RequireHire(4,4,9000,false,400057,8000,9000,4));
            Bad("hire inherits level three",()=>Chunk7SmallFixtureAdmission.RequireHire(4,4,9000,false,400057,8000,5000,3));
            Bad("hire inherits extra levels",()=>Chunk7SmallFixtureAdmission.RequireHire(4,4,9000,false,400057,8000,15000,5));
            Bad("hire insufficient money",()=>Chunk7SmallFixtureAdmission.RequireHire(4,4,9000,false,7999,8000,9000,4));
            Bad("hire free",()=>Chunk7SmallFixtureAdmission.RequireHire(4,4,9000,false,400057,0,9000,4));
            Good("native paid balance retains starting gold after field admission",()=>Chunk7SmallFixtureAdmission.RequirePaidBalance(400057,8000,411,392468));
            Bad("final field silently drops native starting gold",()=>Chunk7SmallFixtureAdmission.RequirePaidBalance(400057,8000,411,392057));
            Bad("final field charges hire twice",()=>Chunk7SmallFixtureAdmission.RequirePaidBalance(400057,8000,411,384468));
            Bad("final field invents another grant",()=>Chunk7SmallFixtureAdmission.RequirePaidBalance(400057,8000,411,392879));
            Good("authority input-only then real post-load reservation",()=>{var r=Fresh();var g=new object();var m=new object();var a=new PersistenceSaveAuthorization(root,r.PersistenceLoad);a.ConfigureSmallFixture(r,g,m);Assert(a.MatchesSmallFixturePlan(r,g,m)&&a.SmallFixtureReservations==0&&a.Admission(RuntimeSaveOperation.Write)!=null);a.Loaded();a.ReserveSmallFixtureOutput(r,g,m,304);Assert(a.SmallFixtureReservations==1&&a.SmallFixtureNumber==304&&a.SmallFixtureOutputLeaf==Chunk7SmallFixtureSelection.ManualLeaf(304)&&a.Admission(RuntimeSaveOperation.Write)==null&&a.Admission(RuntimeSaveOperation.Delete)!=null);});
            Bad("authority reservation before load",()=>{var r=Fresh();var g=new object();var m=new object();var a=new PersistenceSaveAuthorization(root,r.PersistenceLoad);a.ConfigureSmallFixture(r,g,m);a.ReserveSmallFixtureOutput(r,g,m,304);});
            Bad("authority configure with extra write cap",()=>{var r=Fresh();var a=new PersistenceSaveAuthorization(root,r.PersistenceLoad);a.SetCaps(1,2);a.ConfigureSmallFixture(r,new object(),new object());});
            Bad("authority foreign native game",()=>{var r=Fresh();var g=new object();var m=new object();var a=new PersistenceSaveAuthorization(root,r.PersistenceLoad);a.ConfigureSmallFixture(r,g,m);a.Loaded();a.ReserveSmallFixtureOutput(r,new object(),m,304);});
            Bad("authority second reservation",()=>{var r=Fresh();var g=new object();var m=new object();var a=new PersistenceSaveAuthorization(root,r.PersistenceLoad);a.ConfigureSmallFixture(r,g,m);a.Loaded();a.ReserveSmallFixtureOutput(r,g,m,304);a.ReserveSmallFixtureOutput(r,g,m,305);});
            ConfigureMutation("captured full request token",r=>r.TransactionToken+="x");
            ConfigureMutation("captured full request additional scalar",r=>r.UnselectedMarker="changed");
            ConfigureMutation("captured nested authorized descriptor",r=>r.PersistenceLoad.GameName+="x");
            ConfigureMutation("captured nested protected fixture",r=>r.Fixture=new RuntimeFixtureIdentity{Working=new RuntimeSaveDescriptor{Sha256="changed"}});
            ConfigureMutation("captured foreign-family object",r=>r.ForeignFamily=new object());
            ConfigureMutation("bounded request string",r=>r.UnselectedMarker=new string('x',16385));
            ConfigureMutation("captured exact source timestamp",r=>r.PersistenceLoad.LastWriteTimeUtcTicks++);
            ConfigureMutation("captured catalogue pin",r=>r.SmallFixture.NativeInventoryArtifactSha256=new string('0',64));
            Good("body capture is bounded vector-only wire JSON",()=>{var b=Body();Chunk7SmallFixtureScenario.RequireSmallBody(b);var o=Chunk7SmallFixtureScenario.CaptureBody(b);Assert(((JObject)o["avatarLocalScale"]).Properties().Count()==3&&o["currentEquipmentBlueprints"] is JArray slots&&slots.Count==12&&slots.All(v=>v.Type==JTokenType.Null));Assert(o.ToString(Formatting.None).Length<4096);});
            var changes=new Dictionary<string,Action<SmallRiderBodyObservation>> {
                {"female",b=>b.Gender="Female"},{"fat preset",b=>b.PresetGuid="4031e194544440c47a50ed30073bcf09"},{"left handed",b=>b.LeftHanded=true},
                {"Medium",b=>b.EffectiveSizeOrdinal=4},{"polymorph",b=>b.RiderPolymorphed=true},{"foreign Horse",b=>b.MountBlueprintGuid=new string('0',32)},
                {"unresolved native preset",b=>b.NativeRaceAndPresetIdentityExact=false},{"skeleton substituted",b=>b.NativeSkeletonIsExactPreset=false},
                {"nonfinite scale",b=>b.AvatarLocalScale=new PoseVector3(float.NaN,1,1)},{"missing slot",b=>b.CurrentEquipmentBlueprints=new string[11]},
                {"duplicate Doll ID",b=>b.DollEquipmentEntityIds=new[]{new string('a',32),new string('a',32)}},{"extra limb",b=>b.AdditionalLimbCount=1},
                {"empty live visual equipment",b=>b.ActiveVisualEquipmentNames=new string[0]} };
            foreach(var c in changes){Bad("exact live body "+c.Key,()=>{var b=Body();c.Value(b);Chunk7SmallFixtureScenario.RequireSmallBody(b);});}
            Console.WriteLine("TOTAL PASS="+pass+" FAIL="+fail);return fail==0?0:1;
        }
    }
}
