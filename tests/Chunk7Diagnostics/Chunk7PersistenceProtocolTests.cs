using System;
using System.Linq;
using KingmakerMountedCombat;
using KingmakerMountedCombat.Diagnostics;
internal static class Chunk7PersistenceProtocolTests
{
    private static int passed;
    private static readonly string[] Cases = { "multi-exploration-manual", "multi-exploration-quick", "multi-exploration-auto", "multi-rt-debt-manual", "multi-tb-debt-manual", "zero-mounted-tb-retained-manual", "legacy-mounted-pending-tb-manual", "multi-area-autosave" };
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
    private static void Check(bool value, string name) { if (!value) throw new Exception(name); passed++; Console.WriteLine("PASS " + name); }
    internal static int Main()
    {
        foreach (var checkpoint in Cases) foreach (var cold in new[] { false, true })
        {
            var request = Request(checkpoint, cold); var errors = request.Validate();
            Check(errors.Count == 0, "exact " + (cold ? "cold" : "save") + " " + checkpoint + ": " + string.Join("; ", errors));
            Check(request.ExpectedNativeLoadType == (cold && checkpoint.EndsWith("-quick") ? "Quick" : cold && (checkpoint.EndsWith("-auto") || checkpoint == "multi-area-autosave") ? "Auto" : "Manual"), "exact source Working or cold archive category " + checkpoint);
        }
        Action<string, Action<RuntimeRequest>> refuse = (name, change) => { var request = Request(Cases[0], true); change(request); Check(request.Validate().Count != 0, "refuses " + name); };
        refuse("undeclared case", r => r.PersistenceCase = "manual");
        refuse("missing case", r => r.PersistenceCase = null);
        refuse("wrong case casing", r => r.PersistenceCase = "MULTI-exploration-manual");
        refuse("missing archive", r => r.PersistenceLoad = null);
        refuse("legacy P01 leaf", r => r.PersistenceLoad.FileName = "Manual_300_KMC_P01.zks");
        refuse("renamed native category", r => r.PersistenceLoad.FileName = "Quick_1.zks");
        refuse("source archive on save request", r => r.Scenario = "chunk7-persistence-save");
        refuse("foreign campaign", r => r.PersistenceLoad.GameId = "99999999-9999-4999-8999-999999999999");
        refuse("foreign area", r => r.PersistenceLoad.Area = new string('b', 32));
        refuse("protected baseline bytes", r => r.PersistenceLoad.Sha256 = r.Fixture.Baseline.Sha256);
        refuse("second archive", r => r.PersistenceAlternate = r.PersistenceLoad);
        refuse("area mutation", r => r.PersistenceAreaTarget = new RuntimeAreaTransitionTarget { EnterPoint = new string('c', 32), Area = new string('d', 32), AutoSaveMode = "AfterEntry" });
        refuse("old-family broadened case", r => r.Scenario = "persistence-p02-load");
        refuse("schema1 archive", r => r.SchemaVersion = 1);
        foreach (var mutation in new[] { "target", "mode", "same-area", "load-area", "category" })
        {
            var area = Request("multi-area-autosave", true);
            switch (mutation)
            {
                case "target": area.PersistenceAreaTarget = null; break;
                case "mode": area.PersistenceAreaTarget.AutoSaveMode = "BeforeExit"; break;
                case "same-area": area.PersistenceAreaTarget.Area = area.Fixture.Working.Area; break;
                case "load-area": area.PersistenceLoad.Area = area.Fixture.Working.Area; break;
                case "category": area.PersistenceLoad.FileName = "Manual_300_KMC_C7_WORLD.zks"; break;
            }
            Check(area.Validate().Count != 0, "refuses exact area " + mutation);
        }
        foreach (var name in new[] { "C7-persistence-all-pair-area-before-autosave", "C7-persistence-two-native-mounts", "C7-persistence-one-native-mount-legacy-source", "C7-persistence-native-category-all-pair-archive", "C7-persistence-cold-all-pair-identity-and-no-prepare", "C7-persistence-native-continuation", "C7-persistence-native-combat-continuation", "C7-persistence-cleanup", "C7-persistence-runtime", "C7-persistence-interrupted" })
            Check(RuntimeSubscenarioResult.IsMissionScenario(name), "closed result row " + name);
        foreach (var scenario in new[] { "chunk7-persistence-save", "chunk7-persistence-load", "chunk7-two-pair-combat-rider-first", "chunk7-two-pair-combat-mount-first", "chunk7-two-pair-realtime-core", "chunk7-two-pair-realtime-casting", "chunk7-two-pair-realtime-charge", "chunk7-two-pair-native-life-cleanup" })
        {
            var cold = scenario == "chunk7-persistence-load";
            var combat = RuntimeRequest.IsChunk7CombatScenario(scenario);
            var request = Request(Cases[0], cold);
            request.Scenario = scenario;
            if (combat) request.PersistenceCase = null;
            Check(request.Validate().Count == 0, "exact selected native family " + scenario);
            var names = cold ? new[] { "C7-persistence-cold-all-pair-identity-and-no-prepare", "C7-persistence-native-continuation", "C7-persistence-cleanup" } :
                new[] { "C7-persistence-two-native-mounts", "C7-persistence-native-category-all-pair-archive", "C7-persistence-native-continuation", "C7-persistence-cleanup" };
            if (combat) names = new[] { "C7-combat-fixture", "C7-combat-native-mounts", "C7-combat-native-orders", "C7-combat-pair-a-full-activation", "C7-combat-pair-b-full-activation", "C7-combat-tb-charge-refusal", "C7-combat-refresh", "C7-combat-r1-round-release", "C7-combat-native-pending-survivor", "C7-combat-local-cleanup-failure", "C7-combat-cleanup" };
            if (RuntimeRequest.IsChunk7RealtimeScenario(scenario)) names = new[] { "C7-combat-fixture", "C7-combat-native-mounts", "C7-combat-cleanup" }.Concat(
                scenario == "chunk7-two-pair-realtime-core" ? new[] { "C7-rt-charge-default-off", "C7-rt-concurrent-primary", "C7-rt-local-cancel", "C7-rt-unmounted-native-control", "C7-combat-local-cleanup-failure" } :
                scenario == "chunk7-two-pair-realtime-casting" ? new[] { "C7-rt-native-casting", "C7-rt-casting-neighbour" } :
                new[] { "C7-rt-charge-native-success", "C7-rt-charge-neighbour-movement", "C7-rt-charge-native-cancel" }).ToArray();
            if (RuntimeRequest.IsChunk7NativeLifeScenario(scenario)) names = new[] { "C7-combat-fixture", "C7-combat-native-mounts", "C7-combat-native-orders", "C7-life-safe-native-damage", "C7-life-native-callback", "C7-life-healthy-neighbour", "C7-life-independent-survivor", "C7-combat-cleanup" };
            var rows = names.Select(name => new RuntimeSubscenarioResult { Name = name, Status = "PASS", AssertionPassCount = 1, Errors = new string[0] }).ToArray();
            var result = new RuntimeResult { SchemaVersion = 2, RunId = request.RunId, Scenario = request.Scenario, Status = "PASS", Branch = request.Branch, Commit = request.Commit,
                ProductVersion = request.ProductVersion, DllSha256 = request.DllSha256, DllMvid = request.DllMvid, TransactionToken = request.TransactionToken,
                StartedAtUtc = "2026-10-12T00:00:00Z", CompletedAtUtc = "2026-10-12T00:00:01Z", ModsRestored = true, SaveProtectionPassed = true, GameResultSha256 = new string('9', 64),
                Errors = new string[0], Fixture = request.Fixture, BaselineImmutable = true, WorkingRestored = true, SaveWriteAllowlistPassed = true, RestoredSaveInventoryDigest = new string('a', 64),
                EvidenceManifestSha256 = new string('b', 64), SubscenarioTotal = rows.Length, SubscenarioPassCount = rows.Length, AssertionPassCount = rows.Length, SubscenarioResults = rows };
            Check(result.Validate().Count == 0, "complete exact persistence result " + request.Scenario + ": " + string.Join("; ", result.Validate()));
            rows[0].Name = "C7-persistence-unknown"; Check(result.Validate().Count != 0, "unknown completion row rejected " + request.Scenario);
        }
        var unselected = Request(null, false); unselected.Scenario = "chunk7-two-pair-combat-rt";
        Check(unselected.Validate().Count != 0, "unselected RT standalone family stays closed");
        Console.WriteLine("CHUNK7 PERSISTENCE PROTOCOL PASS=" + passed + " FAIL=0; offline protocol only"); return 0;
    }
}
