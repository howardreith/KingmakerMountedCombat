using System;using System.Linq;using KingmakerMountedCombat;using KingmakerMountedCombat.Diagnostics;
internal static class Chunk7RepeatedWorldProtocolTests
{
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
    internal static int Main(){
        foreach(var area in new[]{false,true}){var r=Replay(area);Check(r.Validate().Count==0,"exact bounded "+r.PersistenceCase+": "+string.Join(";",r.Validate()));Check(r.ExpectedNativeLoadType=="Manual","exact native Manual input");}
        foreach(var mutation in new[]{"save-only","wrong-family","missing-input","foreign-source-area","protected-source","second-input","legacy-target","text-case"}){var r=Replay(false);switch(mutation){case "save-only":r.Scenario="chunk7-persistence-save";r.PersistenceLoad=null;break;case "wrong-family":r.Scenario="persistence-p05-load";break;case "missing-input":r.PersistenceLoad=null;break;case "foreign-source-area":r.PersistenceLoad.Area=new string('b',32);break;case "protected-source":r.PersistenceLoad.Sha256=r.Fixture.Baseline.Sha256;break;case "second-input":r.PersistenceAlternate=r.PersistenceLoad;break;case "legacy-target":r.PersistenceAreaTarget=new RuntimeAreaTransitionTarget{EnterPoint=new string('1',32),Area=new string('b',32),AutoSaveMode="AfterEntry"};break;case "text-case":r.PersistenceCase="Multi-exploration-three-loads-manual";break;}Check(r.Validate().Count!=0,"refuses "+mutation);}
        foreach(var mutation in new[]{"missing","short","long","null","zero-area","zero-entry","same-guid","same-source","same-leg","uppercase","save-mode","wrong-case"}){var r=Replay(true);switch(mutation){case "missing":r.PersistenceReplayTargets=null;break;case "short":r.PersistenceReplayTargets=r.PersistenceReplayTargets.Take(2).ToArray();break;case "long":r.PersistenceReplayTargets=r.PersistenceReplayTargets.Concat(new[]{r.PersistenceReplayTargets[0]}).ToArray();break;case "null":r.PersistenceReplayTargets[0]=null;break;case "zero-area":r.PersistenceReplayTargets[0].Area=new string('0',32);break;case "zero-entry":r.PersistenceReplayTargets[0].EnterPoint=new string('0',32);break;case "same-guid":r.PersistenceReplayTargets[0].EnterPoint=r.PersistenceReplayTargets[0].Area;break;case "same-source":r.PersistenceReplayTargets[0].Area=r.Fixture.Working.Area;break;case "same-leg":r.PersistenceReplayTargets[1].Area=r.PersistenceReplayTargets[0].Area;break;case "uppercase":r.PersistenceReplayTargets[0].Area=new string('B',32);break;case "save-mode":r.PersistenceReplayTargets[0].AutoSaveMode="AfterEntry";break;case "wrong-case":r.PersistenceCase="multi-exploration-manual";break;}Check(r.Validate().Count!=0,"refuses route "+mutation);}
        foreach(var row in new[]{"C7-persistence-three-native-loads","C7-persistence-three-native-area-transfers"})Check(RuntimeSubscenarioResult.IsMissionScenario(row),"closed result row "+row);
        Check(!RuntimeSubscenarioResult.IsMissionScenario("C7-persistence-three-native-worlds"),"unknown inferred result remains closed");
        Console.WriteLine("REPEATED WORLD PROTOCOL PASS="+passed+" FAIL=0");return 0;
    }
}
