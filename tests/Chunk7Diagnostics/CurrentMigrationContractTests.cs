using System;
using System.IO;
using System.Linq;
using System.Reflection;
using KingmakerMountedCombat.Diagnostics;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

// Request and profile boundaries are facades; the parent guard, current/historical
// codecs, archive validators and both continuity guards are actual source methods.
namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed class RuntimeRequest
    {
        internal Chunk7OwnedPairsSelection OwnedPairs = new Chunk7OwnedPairsSelection();
        internal string PersistenceCase, Schema1DerivationSha256;
        internal RuntimeSaveDescriptor PersistenceLoad = new RuntimeSaveDescriptor();
    }
    internal sealed class Chunk7OwnedPairsSelection
    {
        internal const string Scenario = "chunk7-owned-two-pair-campaign";
        internal bool IsOwnerSeedCheckpointLoad = true;
        internal int ExpectedNativeLoadCount = 1, ExpectedNativeWriteCount;
        internal string CheckpointSourceRunId = "accepted-historical-parent", SourceArchiveSha256 = new string('a', 64), RiderAId, MountAId;
        internal static bool CurrentMigrationCheckpoint(string value) => value == "owned-schema1-current-manual" || value == "owned-schema2-pending-current-manual";
    }
    internal sealed class RuntimeSaveDescriptor
    {
        internal string InternalName, FileName, Sha256 = new string('b', 64), GameId, Area;
        internal long Length = 1234;
    }
    internal static class SupportedMountedProfiles
    {
        internal static readonly Profile Horse = new Profile();
        internal sealed class Profile { internal string Id => "medium-humanoid-horse-v1"; }
    }
}

internal static class CurrentMigrationContractTests
{
    private static int passed, failed;
    private static void Check(string name, Action action)
    { try { action(); passed++; Console.WriteLine("PASS " + name); } catch (Exception exception) { failed++; Console.WriteLine("FAIL " + name + ": " + exception); } }
    private static void True(bool value, string text) { if (!value) throw new Exception(text); }
    private static void Refuse(Action action)
    { var refused = false; try { action(); } catch (InvalidOperationException) { refused = true; } catch (InvalidDataException) { refused = true; } True(refused, "Invalid parent admitted."); }
    private static MountedArchiveData Archive(bool schema1)
    {
        if (schema1)
        {
            var rider = new SavedNativeActor { Id = "11111111-1111-4111-8111-111111111111", ReactionsRemaining = 2 };
            var mount = new SavedNativeActor { Id = "22222222-2222-4222-8222-222222222222", ReactionsRemaining = 2 };
            return new MountedArchiveData { SchemaVersion = 3, CampaignId = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa", AreaId = new string('a', 32),
                GameTimeTicks = 10000000, Policy = MountedSaveData.PairedPolicy, RulesId = MountedSaveData.Rules,
                Pairs = new[] { new SavedMountedPair { PairId = new string('c', 32), Generation = 1, ProfileId = SupportedMountedProfiles.Horse.Id, Rider = rider, Mount = mount } },
                Participations = new SavedPairParticipation[0], Slots = new[] { new SavedMountedSlot { ActorId = rider.Id, Index = 12, Kind = 2 }, new SavedMountedSlot { ActorId = rider.Id, Index = 13, Kind = 3 } } };
        }
        var old = (MountedSaveData)typeof(Chunk7PersistenceTests).GetMethod("Historical", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        old.ProfileId = SupportedMountedProfiles.Horse.Id;
        old.Combat.Actors[0].InCombat = false; old.Combat.Roster = new[] { old.Combat.Roster[1] };
        old.Combat.Current = (SavedTurnContext)typeof(Chunk7PersistenceTests).GetMethod("Turn", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { old.Mount.Id });
        old.Combat.Paired = new SavedPairedState { PendingSplitId = old.Mount.Id, PendingSplitRound = old.Combat.Round, SplitReleaseRound = -1 };
        return MountedArchiveCodec.Decode(MountedSaveCodec.Encode(old)).Data;
    }
    private static RuntimeRequest Child(MountedArchiveData archive, bool schema1)
    {
        var child = new RuntimeRequest { PersistenceCase = schema1 ? "owned-schema1-current-manual" : "owned-schema2-pending-current-manual" };
        child.OwnedPairs.RiderAId = archive.Pairs[0].Rider.Id; child.OwnedPairs.MountAId = archive.Pairs[0].Mount.Id;
        child.PersistenceLoad.InternalName = schema1 ? "KMC_C7_SCHEMA1" : "KMC_C7_SCHEMA2";
        child.PersistenceLoad.FileName = "Manual_305_" + child.PersistenceLoad.InternalName + ".zks";
        child.PersistenceLoad.GameId = archive.CampaignId; child.PersistenceLoad.Area = archive.AreaId;
        return child;
    }
    private static JObject Parent(RuntimeRequest child, MountedArchiveData archive, bool schema1)
    {
        return new JObject { ["scenario"] = Chunk7OwnedPairsSelection.Scenario, ["evidenceKind"] = "chunk7-persistence-evidence",
            ["runId"] = child.OwnedPairs.CheckpointSourceRunId, ["status"] = "PASS", ["errors"] = new JArray(),
            ["commit"] = new string('1', 40), ["dllSha256"] = new string('2', 64), ["dllMvid"] = "33333333-3333-4333-8333-333333333333",
            ["observations"] = new JObject { ["checkpoint"] = schema1 ? "owned-schema1-resave-manual" : "legacy-mounted-pending-tb-manual",
                ["cold"] = true, ["processId"] = 99, ["inheritedManualSha256"] = child.OwnedPairs.SourceArchiveSha256,
                [schema1 ? "schema1Migration" : "schema2Migration"] = new JObject { ["inputSchema"] = schema1 ? 1 : 2, ["resavedWorld"] = new JObject() },
                ["nativeWrite"] = new JObject { ["path"] = Path.Combine(@"C:\KmcCurrentVirtual\Saved Games", child.PersistenceLoad.FileName),
                    ["sha256"] = child.PersistenceLoad.Sha256, ["length"] = child.PersistenceLoad.Length, ["nativeName"] = child.PersistenceLoad.InternalName,
                    ["nativeType"] = "Manual", ["sourceSchema"] = 3, ["nativeCallback"] = true, ["operation"] = "None", ["snapshotCount"] = 1,
                    ["archive"] = JObject.FromObject(archive, MountedSaveCodec.CreateSerializer()) } } };
    }
    public static int Main(string[] arguments)
    {
        if (arguments.Length != 1) throw new ArgumentException("Exact installed Unity dependency path required.");
        AppDomain.CurrentDomain.AssemblyResolve += (sender, request) => new AssemblyName(request.Name).Name == "UnityEngine" ? Assembly.LoadFrom(arguments[0]) : null;
        foreach (var schema in new[] { true, false })
        {
            var schema1 = schema; var label = schema1 ? "schema1" : "schema2";
            Check(label + " actual native current output parent is accepted without mutating its graph", () => {
                var archive = Archive(schema1); var child = Child(archive, schema1); var parent = Parent(child, archive, schema1); var before = parent.ToString();
                Chunk7PersistenceScenario.ValidateCurrentMigrationParent(parent, child, archive, 100);
                True(before == parent.ToString(), "Parent mutated.");
            });
            foreach (var problem in new[] { "save-parent", "wrong-parent-case", "wrong-run", "failed-parent", "parent-errors", "missing-migration", "wrong-historical-schema", "wrong-format", "wrong-hash", "wrong-length", "wrong-name", "wrong-leaf", "no-callback", "unfinished-write", "extra-snapshot", "same-process", "wrong-manual", "write-quota", "load-quota", "derivative-authority", "source-direction", "graph-change", "role-change", "campaign-change", "missing-resaved-world", "other-migration", "bad-candidate" })
            {
                var fault = problem;
                Check(label + " current parent refuses " + fault, () => {
                    var archive = Archive(schema1); var child = Child(archive, schema1); var parent = Parent(child, archive, schema1);
                    var o = parent["observations"]; var write = o["nativeWrite"]; var migration = o[schema1 ? "schema1Migration" : "schema2Migration"];
                    switch (fault)
                    {
                        case "save-parent": o["cold"] = false; break;
                        case "wrong-parent-case": o["checkpoint"] = "owned-schema1-manual"; break;
                        case "wrong-run": parent["runId"] = "foreign"; break;
                        case "failed-parent": parent["status"] = "FAIL"; break;
                        case "parent-errors": ((JArray)parent["errors"]).Add("retained failure"); break;
                        case "missing-migration": ((JObject)o).Remove(schema1 ? "schema1Migration" : "schema2Migration"); break;
                        case "wrong-historical-schema": migration["inputSchema"] = 3; break;
                        case "wrong-format": write["sourceSchema"] = 2; break;
                        case "wrong-hash": write["sha256"] = new string('d', 64); break;
                        case "wrong-length": write["length"] = 1235; break;
                        case "wrong-name": write["nativeName"] = "KMC_C7_SEED_CHECKPOINT"; break;
                        case "wrong-leaf": write["path"] = @"C:\KmcCurrentVirtual\Saved Games\Manual_999_other.zks"; break;
                        case "no-callback": write["nativeCallback"] = false; break;
                        case "unfinished-write": write["operation"] = "Saving"; break;
                        case "extra-snapshot": write["snapshotCount"] = 2; break;
                        case "same-process": o["processId"] = 100; break;
                        case "wrong-manual": o["inheritedManualSha256"] = new string('d', 64); break;
                        case "write-quota": child.OwnedPairs.ExpectedNativeWriteCount = 1; break;
                        case "load-quota": child.OwnedPairs.ExpectedNativeLoadCount = 2; break;
                        case "derivative-authority": child.Schema1DerivationSha256 = new string('e', 64); break;
                        case "source-direction": child.OwnedPairs.IsOwnerSeedCheckpointLoad = false; break;
                        case "graph-change": write["archive"]["Pairs"][0]["Generation"] = 12; break;
                        case "role-change": child.OwnedPairs.RiderAId = "99999999-9999-4999-8999-999999999999"; break;
                        case "campaign-change": child.PersistenceLoad.GameId = "99999999-9999-4999-8999-999999999999"; break;
                        case "missing-resaved-world": ((JObject)migration).Remove("resavedWorld"); break;
                        case "other-migration": o[schema1 ? "schema2Migration" : "schema1Migration"] = new JObject(); break;
                        case "bad-candidate": parent["commit"] = "wrong"; break;
                    }
                    Refuse(() => Chunk7PersistenceScenario.ValidateCurrentMigrationParent(parent, child, archive, 100));
                });
            }
        }
        Console.WriteLine("CURRENT MIGRATION ACTUAL PARENT GUARDS PASS=" + passed + " FAIL=" + failed + "; native delivery is not exercised.");
        return failed == 0 ? 0 : 1;
    }
}
