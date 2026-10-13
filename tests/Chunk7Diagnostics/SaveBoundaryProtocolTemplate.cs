using System;using System.Linq;using KingmakerMountedCombat;using KingmakerMountedCombat.Diagnostics;
namespace KingmakerMountedCombat.Diagnostics {internal static class Chunk7PersistenceScenario{internal const string SaveScenario="chunk7-persistence-save",LoadScenario="chunk7-persistence-load";internal const string PendingMountSaveCase="multi-nonselected-mount-unsettled-manual",WorkerCancelSaveCase="multi-owned-worker-cancel-manual";__CASE_METHOD__}}
internal static class Chunk7SaveBoundaryProtocolTests{
    private static RuntimeSaveDescriptor Save(string name, string leaf, char hash) => new RuntimeSaveDescriptor {
        InternalName = name, FileName = leaf, Sha256 = new string(hash, 64), Length = 1234, LastWriteTimeUtcTicks = 100,
        GameId = "11111111-1111-4111-8111-111111111111", GameName = "SyntheticOwned", Area = new string('a', 32) };
    private static RuntimeRequest Request(string checkpoint, bool cold)
    {
        var working = Save(RuntimeRequest.WorkingSaveName, "Manual_299_KMC_AUTOMATION_WORKING.zks", '1');
        var baseline = Save(RuntimeRequest.BaselineSaveName, "Manual_1_KMC_AUTOMATION_BASELINE.zks", '2');
        var result = new RuntimeRequest { SchemaVersion = 2, RunId = "synthetic-c7-protocol", Scenario = cold ? "chunk7-persistence-load" : "chunk7-persistence-save",
            Branch = "codex/mounted-combat-chunk7-foundation", Commit = new string('3', 40), ProductVersion = BuildIdentity.ProductVersion,
            DllSha256 = new string('4', 64), DllMvid = "55555555-5555-4555-8555-555555555555", TransactionToken = new string('6', 64), EvidenceRoot = @"C:\Dev\KingmakerMountedCombatLab\analysis-cache\synthetic-c7-protocol",
            PersistenceCase = checkpoint, QualificationSuite = new RuntimeQualificationSuiteIdentity { SuiteId = "synthetic-c7", SnapshotSha256 = new string('7', 64) },
            Fixture = new RuntimeFixtureIdentity { Baseline = baseline, Working = working,
                WriteAuthorization = new RuntimeSaveWriteAuthorization { Mode = "working-only", AllowedInternalName = working.InternalName, AllowedFileName = working.FileName, BaselineImmutable = true } } };
        if (cold) result.PersistenceLoad = checkpoint == "multi-exploration-quick" ? Save("Localized quick", "Quick_1.zks", '8') :
            checkpoint == "multi-exploration-auto" || checkpoint == "multi-area-autosave" ? Save("Localized auto", "Auto_1.zks", '8') : Save("KMC_C7_WORLD", "Manual_300_KMC_C7_WORLD.zks", '8');
        if (checkpoint == "multi-area-autosave")
        {
            result.PersistenceAreaTarget = new RuntimeAreaTransitionTarget { EnterPoint = new string('c', 32), Area = new string('b', 32), AutoSaveMode = "AfterEntry" };
            if (cold) result.PersistenceLoad.Area = result.PersistenceAreaTarget.Area;
        }
        return result;
    }


    private static int passed;private static void Check(bool c,string m){if(!c)throw new Exception(m);passed++;Console.WriteLine("PASS "+m);}
    private static RuntimeRequest Replay(bool area){var r=Request(area?"multi-exploration-three-area-transfers-manual":"multi-exploration-three-loads-manual",true);if(area)r.PersistenceReplayTargets=Enumerable.Range(0,3).Select(i=>new RuntimeAreaTransitionTarget{EnterPoint=new string((char)('1'+i),32),Area=new string((char)('b'+i),32),AutoSaveMode="None"}).ToArray();return r;}

 private static int? WriteQuota(RuntimeRequest request){__SCHEMA2_SELECTOR__ return __WRITE_QUOTA__;}
 internal static int Main(){
  foreach(var checkpoint in new[]{"multi-nonselected-mount-unsettled-manual","multi-owned-worker-cancel-manual"}){
   foreach(var cold in new[]{false,true}){var r=Request(checkpoint,cold);Check(r.Validate().Count==0,"closed source/cold "+checkpoint+" "+cold+": "+string.Join(";",r.Validate()));Check(r.ExpectedNativeLoadType=="Manual","exact native Manual load route");Check(WriteQuota(r)==(cold?(int?)null:1),"exact one-write quota only for selected source");}
   foreach(var mutation in new[]{"wrong-family","wrong-case","protected-source","foreign-area","extra-area","extra-replay","extra-alternate","wrong-leaf","missing-load"}){
    var r=Request(checkpoint,true);switch(mutation){case "wrong-family":r.Scenario="persistence-p04-load";break;case "wrong-case":r.PersistenceCase=checkpoint.ToUpperInvariant();break;case "protected-source":r.PersistenceLoad.Sha256=r.Fixture.Baseline.Sha256;break;case "foreign-area":r.PersistenceLoad.Area=new string('b',32);break;case "extra-area":r.PersistenceAreaTarget=new RuntimeAreaTransitionTarget{EnterPoint=new string('c',32),Area=new string('b',32),AutoSaveMode="AfterEntry"};break;case "extra-replay":r.PersistenceReplayTargets=new[]{new RuntimeAreaTransitionTarget{EnterPoint=new string('c',32),Area=new string('b',32),AutoSaveMode="None"}};break;case "extra-alternate":r.PersistenceAlternate=r.PersistenceLoad;break;case "wrong-leaf":r.PersistenceLoad.FileName="Quick_1.zks";break;case "missing-load":r.PersistenceLoad=null;break;}Check(r.Validate().Count!=0,"rejects "+checkpoint+" "+mutation);
   }
  }
  foreach(var row in new[]{"C7-persistence-nonselected-mount-save-deferral","C7-persistence-owned-worker-cancel-drain"})Check(RuntimeSubscenarioResult.IsMissionScenario(row),"closed evidence row "+row);
  Check(!RuntimeSubscenarioResult.IsMissionScenario("C7-persistence-owned-worker-fault"),"unselected actual fault proof remains closed");
  foreach(var item in new[]{new[]{"persistence-p04-save","combat-mount-unsettled-save"},new[]{"persistence-p07-save","worker-drain"},new[]{"chunk7-persistence-save","multi-exploration-manual"},new[]{"chunk7-persistence-load","multi-exploration-three-loads-manual"}}){var r=new RuntimeRequest{Scenario=item[0],PersistenceCase=item[1]};Check(WriteQuota(r)==null,"historical quota remains unchanged "+item[0]+" "+item[1]);}
  Check(WriteQuota(new RuntimeRequest{Scenario=Chunk7OrdinaryAcquisitionSelection.Scenario})==1,"ordinary acquisition exact one-write cap preserved");
  var resultRows=new[]{new RuntimeSubscenarioResult{Name="C7-persistence-schema2-native-resave",Status="PASS",AssertionPassCount=1,Errors=new string[0]}};
  var envelopeErrors=new System.Collections.Generic.List<string>();RuntimeResult.ValidateSubscenarioTotals(envelopeErrors,"chunk7-persistence-load",resultRows,1,1,0,1,0);
  Check(envelopeErrors.Count==0,"schema2 native resave actual result envelope: "+string.Join(";",envelopeErrors));
  resultRows[0].Name="C7-persistence-schema2-native-resave-extra";envelopeErrors.Clear();RuntimeResult.ValidateSubscenarioTotals(envelopeErrors,"chunk7-persistence-load",resultRows,1,1,0,1,0);
  Check(envelopeErrors.Count!=0,"unregistered schema2 result neighbor remains refused");
  var migrated=Request("legacy-mounted-pending-tb-manual",true);Check(migrated.Validate().Count==0,"exact schema2 pending-only cold request remains admitted");
  Check(WriteQuota(migrated)==1,"schema2 cold migration allows one native resave");
  Check(WriteQuota(Request("legacy-mounted-pending-tb-manual",false))==null,"historical schema2 source quota remains unchanged");
  Console.WriteLine("SAVE BOUNDARY PROTOCOL/QUOTA PASS="+passed+" FAIL=0; source contracts only");return 0;
 }
}
