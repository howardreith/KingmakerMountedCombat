using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using KingmakerMountedCombat.Diagnostics;

namespace KingmakerMountedCombat.Tests
{
    internal static class Chunk7OrdinaryAuthorityTests
    {
        private const string Area = "0123456789abcdef0123456789abcdef";
        private const string GameId = "11111111-1111-4111-8111-111111111111";
        private static string fixtureRoot;
        private static int Main(string[] args)
        {
            if (args.Length != 1 || !Directory.Exists(args[0]) || Path.GetFullPath(args[0]) != args[0]) throw new ArgumentException("An existing exact private fixture root is required.");
            fixtureRoot = args[0];
            var runner = new TestRunner();
            RuntimeSaveAuthorizationTests.Register(runner);
            runner.Run("ordinary complete request uses the exact new scenario and selection", () =>
                TestRunner.Equal(0, Request().Validate().Count, "Ordinary request refused."));
            foreach (var name in new[] { "missing selection", "historical scenario", "schema 1", "persistence archive", "persistence checkpoint" })
                runner.Run("ordinary protocol refuses " + name, () => {
                    var value = Request();
                    switch (name) {
                        case "missing selection": value.OrdinaryAcquisition = null; break;
                        case "historical scenario": value.Scenario = "persistence-p01-save"; break;
                        case "schema 1": value.SchemaVersion = 1; break;
                        case "persistence archive": value.PersistenceLoad = value.Fixture.Working; break;
                        case "persistence checkpoint": value.PersistenceCase = "multi-exploration-auto"; break;
                    }
                    TestRunner.True(value.Validate().Count != 0, "Foreign ordinary request admitted.");
                });
            runner.Run("ordinary exact compound result rows are registered", () => {
                foreach (var row in new[] { "C7-ordinary-fixture", "C7-ordinary-native-choices", "C7-ordinary-native-autosave", "C7-ordinary-native-field-inventory", "C7-ordinary-cleanup", "C7-ordinary-runtime", "C7-ordinary-interrupted" })
                    TestRunner.True(RuntimeSubscenarioResult.IsMissionScenario(row), "Ordinary row missing: " + row);
                TestRunner.True(!RuntimeSubscenarioResult.IsMissionScenario("C7-ordinary-unselected"), "Unknown ordinary result admitted.");
            });
            runner.Run("ordinary custody blocks final world while independent owners still poll", () => {
                var gate = new Chunk7HostCompletionGate(); var order = new List<string>(); gate.Request(0);
                var result = gate.Poll(1, false, () => true, () => { order.Add("persistence"); return false; },
                    () => { order.Add("combat"); return true; }, () => { order.Add("world"); return true; }, _ => { },
                    () => { order.Add("ordinary"); return false; });
                TestRunner.True(!result && string.Join(",",order) == "persistence,combat,ordinary", "Independent ordinary owner skipped or final world advanced.");
            });
            runner.Run("ordinary throwing custody retains ownership through deadline then settles", () => {
                var gate = new Chunk7HostCompletionGate(); var failures = new List<string>(); gate.Request(0); var settled = false;
                Func<bool> ordinary = () => { if (!settled) throw new InvalidOperationException("owned native callback"); return true; };
                TestRunner.True(!gate.Poll(46, false, () => true, () => true, () => true, () => true, failures.Add, ordinary) && failures.Count == 2 && !gate.Settled,
                    "Native ordinary timeout/exception abandoned its owner.");
                settled = true;
                TestRunner.True(gate.Poll(47, false, () => true, () => true, () => true, () => true, failures.Add, ordinary) && gate.DeadlineExceeded,
                    "Physical later settlement failed or erased deadline evidence.");
            });
            runner.Run("native worker blocks ordinary acquisition cleanup", () => {
                var gate = new Chunk7HostCompletionGate(); var calls = 0; gate.Request(0);
                TestRunner.True(!gate.Poll(1, false, () => false, () => true, () => true, () => true, _ => { }, () => { calls++; return true; }) && calls == 0,
                    "Ordinary owner mutated state while native worker remained busy.");
            });
            runner.Run("ordinary exact Small future inventory selection is closed", () =>
                TestRunner.Equal(0, Selection().Validate(Chunk7OrdinaryAcquisitionSelection.Scenario, "ordinary-test").Count, "Valid explicit selection refused."));
            Reject(runner, "unknown case", value => value.Case = "anything");
            Reject(runner, "self-reported inventory", value => value.InventoryRunId = "ordinary-test");
            Reject(runner, "invented unpinned inventory", value => value.InventoryArtifactSha256 = null);
            Reject(runner, "missing exact race", value => value.SmallRaceGuid = null);
            Reject(runner, "missing exact preset", value => value.SmallPresetGuid = null);
            Reject(runner, "missing exact skeleton", value => value.SkeletonName = "");
            Reject(runner, "unspecified handedness", value => value.LeftHanded = null);
            Reject(runner, "unknown native gender", value => value.Gender = "Other");
            Reject(runner, "substituted existing rider", value => value.ExistingActorId = GameId);
            Reject(runner, "synthetic underlevel claim", value => value.MinimumRangerLevel = 3);
            Reject(runner, "unbounded level claim", value => value.MinimumRangerLevel = 21);
            Reject(runner, "zero native completion write", value => value.ExpectedNativeWriteCount = 0);
            Reject(runner, "repeated native completion write", value => value.ExpectedNativeWriteCount = 2);
            Reject(runner, "arbitrary native leaf", value => value.NativeWriteLeaf = "Auto_2.zks");
            runner.Run("another scenario cannot borrow ordinary authority selection", () =>
                TestRunner.True(Selection().Validate("persistence-p05-save", "ordinary-test").Count != 0, "Historical family accepted ordinary selection."));
            runner.Run("existing native actor selection preserves its body", () => {
                var selection = Existing(); TestRunner.Equal(0, selection.Validate(Chunk7OrdinaryAcquisitionSelection.Scenario, "ordinary-test").Count, "Exact existing route refused.");
                selection.SmallRaceGuid = new string('b', 32); TestRunner.True(selection.Validate(Chunk7OrdinaryAcquisitionSelection.Scenario, "ordinary-test").Count != 0, "Existing route accepted body mutation.");
            });
            runner.Run("read-only eligibility does not spend the one actual native write", () => {
                var files = new Files(1); for (var i = 0; i < 5; i++) TestRunner.True(files.Scope.Validate(RuntimeSaveOperation.Write, files.Auto, files.Root) == null, "Read-only eligibility failed.");
                TestRunner.Equal(0, files.Scope.AdmittedNativeWriteRequests, "A descriptor probe consumed native admission.");
                using (files.Activate()) {
                    TestRunner.True(files.Authorization.Authorize(RuntimeSaveOperation.Load, files.Working, files.Root).Allowed, "Exact fixture load refused.");
                    TestRunner.True(files.Authorization.Authorize(RuntimeSaveOperation.Write, files.Auto, files.Root).Allowed, "First actual autosave refused.");
                    TestRunner.Equal(1, files.Scope.AdmittedNativeWriteRequests, "Actual native admission was not counted once.");
                }
            });
            runner.Run("admitted native staging can commit after its request cap is consumed", () => {
                var files = new Files(1);
                using (files.Activate()) {
                    TestRunner.True(files.Authorization.Authorize(RuntimeSaveOperation.Write, files.Auto, files.Root).Allowed, "Initial autosave refused.");
                    using (var writer = files.Scope.BeginWrite(files.Auto, files.Root)) { File.WriteAllText(files.Auto.FullPath, "owned ordinary native-shaped archive bytes"); writer.Complete(); }
                    TestRunner.True(files.Scope.Validate(RuntimeSaveOperation.Load, files.Auto, files.Root) == null, "Completed admitted bytes cannot be read.");
                    var second = files.Authorization.Authorize(RuntimeSaveOperation.Write, files.Auto, files.Root);
                    TestRunner.True(!second.Allowed && second.FatalViolation, "Repeated actual autosave bypassed the one-write boundary.");
                    TestRunner.Equal(1, files.Authorization.AuthorizedWriteCount, "Rejected repeat inflated authorized count.");
                    TestRunner.Equal(1, files.Authorization.UnauthorizedWriteCount, "Rejected repeat was not recorded.");
                }
            });
            runner.Run("failed native write cannot refund its selected admission", () => {
                var files = new Files(1);
                using (files.Activate()) {
                    files.Authorization.Authorize(RuntimeSaveOperation.Write, files.Auto, files.Root);
                    using (files.Scope.BeginWrite(files.Auto, files.Root)) { }
                    TestRunner.True(!files.Authorization.Authorize(RuntimeSaveOperation.Write, files.Auto, files.Root).Allowed, "Writer failure refunded admission.");
                }
            });
            runner.Run("parallel actual native admissions cannot exceed the exact cap", () => {
                var files = new Files(1); using (files.Activate()) {
                    var answers = new bool[16]; Parallel.For(0, answers.Length, index => answers[index] = files.Authorization.Authorize(RuntimeSaveOperation.Write, files.Auto, files.Root).Allowed);
                    TestRunner.Equal(1, answers.Count(value => value), "Parallel admission exceeded one.");
                    TestRunner.Equal(1, files.Scope.AdmittedNativeWriteRequests, "Scope count differs.");
                    TestRunner.Equal(15, files.Authorization.UnauthorizedWriteCount, "Concurrent refusals were lost.");
                }
            });
            runner.Run("wrong identity never consumes ordinary native admission", () => {
                var files = new Files(1); var wrong = files.Auto; wrong.GameId = "foreign";
                TestRunner.True(files.Scope.AuthorizeNativeRequest(RuntimeSaveOperation.Write, wrong, files.Root) != null, "Foreign identity admitted.");
                TestRunner.Equal(0, files.Scope.AdmittedNativeWriteRequests, "Rejected identity consumed allowance.");
            });
            runner.Run("historical isolated scopes keep their prior uncapped policy", () => {
                var files = new Files(null); using (files.Activate()) {
                    TestRunner.True(files.Authorization.Authorize(RuntimeSaveOperation.Write, files.Auto, files.Root).Allowed, "Historical first request refused.");
                    TestRunner.True(files.Authorization.Authorize(RuntimeSaveOperation.Write, files.Auto, files.Root).Allowed, "Historical second request gained an unsolicited cap.");
                }
            });
            runner.Run("exact bound scope identity and activation generation are retained", () => {
                var files = new Files(1); var other = new Files(1); var lease = files.Activate(); var generation = files.Authorization.AuthorityGeneration;
                TestRunner.True(files.Authorization.IsExactPersistenceScope(files.Scope) && !files.Authorization.IsExactPersistenceScope(other.Scope), "Different authority object admitted.");
                lease.Dispose(); TestRunner.True(!files.Authorization.IsExactPersistenceScope(files.Scope), "Released authority remained active.");
                using (files.Activate()) TestRunner.True(files.Authorization.AuthorityGeneration != generation, "Reactivation retained an old generation.");
            });
            runner.Run("zero and unbounded native write caps refuse", () => {
                foreach (var value in new[] { 0, -1, 129 }) { var failed = false; try { new Files(value); } catch (ArgumentException) { failed = true; }
                    TestRunner.True(failed, "Invalid cap accepted: " + value); }
            });
            return runner.Complete();
        }
        private static Chunk7OrdinaryAcquisitionSelection Selection() => new Chunk7OrdinaryAcquisitionSelection {
            Case = Chunk7OrdinaryAcquisitionSelection.PaidSmallCase, InventoryRunId = "native-world-observed", InventoryArtifactSha256 = new string('a',64),
            SmallRaceGuid = new string('b',32), SmallPresetGuid = new string('c',32), SkeletonName = "synthetic-test-only-skeleton", Gender = "Female", LeftHanded = false,
            MinimumRangerLevel = 4, ExpectedNativeWriteCount = 1, NativeWriteLeaf = "Auto_1.zks" };
        private static RuntimeRequest Request()
        {
            var files = new Files(1);
            return new RuntimeRequest { SchemaVersion = 2, RunId = "ordinary-test", Scenario = Chunk7OrdinaryAcquisitionSelection.Scenario,
                OrdinaryAcquisition = Selection(), Branch = "codex/mounted-combat-chunk7-foundation", Commit = new string('1',40),
                ProductVersion = BuildIdentity.ProductVersion, DllSha256 = new string('2',64), DllMvid = GameId,
                TransactionToken = new string('3',64), EvidenceRoot = fixtureRoot, Fixture = files.Fixture,
                QualificationSuite = new RuntimeQualificationSuiteIdentity { SuiteId = "ordinary-suite", SnapshotSha256 = new string('4',64) } };
        }
        private static Chunk7OrdinaryAcquisitionSelection Existing()
        {
            var value = Selection(); value.Case = Chunk7OrdinaryAcquisitionSelection.ExistingCase; value.ExistingActorId = GameId;
            value.SmallRaceGuid = null; value.SmallPresetGuid = null; value.SkeletonName = null; value.Gender = null; value.LeftHanded = null; return value;
        }
        private static void Reject(TestRunner runner, string label, Action<Chunk7OrdinaryAcquisitionSelection> mutate) =>
            runner.Run("ordinary refuses " + label, () => { var value = Selection(); mutate(value); TestRunner.True(value.Validate(Chunk7OrdinaryAcquisitionSelection.Scenario, "ordinary-test").Count != 0, "Invalid selection accepted."); });
        private sealed class Files
        {
            internal readonly string RunRoot, Root;
            internal readonly PersistenceSaveAuthorization Scope;
            internal readonly RuntimeSaveAuthorization Authorization = new RuntimeSaveAuthorization();
            private readonly RuntimeFixtureIdentity fixture;
            internal RuntimeFixtureIdentity Fixture => fixture;
            internal Files(int? cap)
            {
                RunRoot = Path.Combine(fixtureRoot, Guid.NewGuid().ToString("N"));
                Root = Path.Combine(RunRoot,"Saved Games"); Directory.CreateDirectory(Root);
                var working = Path.Combine(Root,"Manual_299_KMC_AUTOMATION_WORKING.zks"); File.WriteAllText(working,"synthetic owned fixture bytes");
                string hash; using (var sha = SHA256.Create()) using (var stream = File.OpenRead(working)) hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
                fixture = new RuntimeFixtureIdentity {
                    Baseline = new RuntimeSaveDescriptor {InternalName=RuntimeRequest.BaselineSaveName,FileName="Manual_298_KMC_AUTOMATION_BASELINE.zks",Sha256=new string('a',64),Length=10,LastWriteTimeUtcTicks=638907120000000000L,GameId=GameId,GameName="KMC Fixture",Area=Area},
                    Working = new RuntimeSaveDescriptor {InternalName=RuntimeRequest.WorkingSaveName,FileName=Path.GetFileName(working),Sha256=hash,Length=new FileInfo(working).Length,LastWriteTimeUtcTicks=new FileInfo(working).LastWriteTimeUtc.Ticks,GameId=GameId,GameName="KMC Fixture",Area=Area},
                    WriteAuthorization = new RuntimeSaveWriteAuthorization {Mode="working-only",AllowedInternalName=RuntimeRequest.WorkingSaveName,AllowedFileName=Path.GetFileName(working),BaselineImmutable=true}};
                Scope = new PersistenceSaveAuthorization(RunRoot,GameId,"KMC Fixture",new string('a',64),new[] {
                    new PersistenceSaveEntry {FileName=Path.GetFileName(working),InternalName=RuntimeRequest.WorkingSaveName,SaveType="Manual",Area=Area,InitialSha256=hash,Writable=false},
                    new PersistenceSaveEntry {FileName="Auto_1.zks",InternalName="Autosave",SaveType="Auto",Area=Area,Writable=true}},null,null,cap);
            }
            internal RuntimeSaveTarget Auto => new RuntimeSaveTarget {FileName="Auto_1.zks",FullPath=Path.Combine(Root,"Auto_1.zks"),InternalName="Autosave",SaveType="Auto",GameId=GameId,GameName="KMC Fixture",Area=Area};
            internal RuntimeSaveTarget Working => new RuntimeSaveTarget {FileName=fixture.Working.FileName,FullPath=Path.Combine(Root,fixture.Working.FileName),InternalName=RuntimeRequest.WorkingSaveName,SaveType="Manual",GameId=GameId,GameName="KMC Fixture",Area=Area};
            internal IDisposable Activate() { var lease=Authorization.Activate(fixture,Root,false); Authorization.BindPersistenceScope(Scope); return lease; }
        }
    }
}
