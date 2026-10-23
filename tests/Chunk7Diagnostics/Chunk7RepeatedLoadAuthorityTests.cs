using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using KingmakerMountedCombat.Diagnostics;

namespace KingmakerMountedCombat.Tests
{
    internal static class Chunk7RepeatedLoadAuthorityTests
    {
        private const string Area = "0123456789abcdef0123456789abcdef";
        private const string GameId = "11111111-1111-4111-8111-111111111111";
        private static string fixtureRoot;
        private static int Main(string[] args)
        {
            if (args.Length != 1 || !Directory.Exists(args[0]) || Path.GetFullPath(args[0]) != args[0])
                throw new ArgumentException("An existing exact private fixture root is required.");
            fixtureRoot = args[0];
            var runner = new TestRunner();
            runner.Run("selected initial load and two repeats share exactly three native admissions", () => {
                var f = new Files(3); using (f.Activate()) {
                    for (var ordinal = 1; ordinal <= 3; ordinal++) {
                        TestRunner.True(f.Authorization.ValidateLoadBeforeWorldReplacement(f.Working, f.Root) == null, "Next declared load refused before disposal.");
                        TestRunner.True(f.Authorization.Authorize(RuntimeSaveOperation.Load, f.Working, f.Root).Allowed, "Selected native load refused.");
                        TestRunner.Equal(ordinal, f.Scope.AdmittedNativeLoadRequests, "Native load was not counted exactly once.");
                        TestRunner.Equal(ordinal, f.Authorization.AuthorizedLoadCount, "Native authority count differs.");
                    }
                    TestRunner.True(f.Authorization.ValidateLoadBeforeWorldReplacement(f.Working, f.Root) != null, "Fourth load reached old-world disposal.");
                    TestRunner.Equal(3, f.Scope.AdmittedNativeLoadRequests, "Read-only exhaustion probe changed admission count.");
                    TestRunner.Equal(0, f.Authorization.FatalViolationCount, "Read-only pre-disposal probe fabricated a native request.");
                    var rejected = f.Authorization.Authorize(RuntimeSaveOperation.Load, f.Working, f.Root);
                    TestRunner.True(!rejected.Allowed && rejected.FatalViolation, "Fourth real native request bypassed its limit.");
                    TestRunner.Equal(3, f.Authorization.AuthorizedLoadCount, "Fourth request increased authorized count.");
                    TestRunner.Equal(1, f.Authorization.UnauthorizedLoadCount, "Fourth request lost its refusal evidence.");
                }
            });
            runner.Run("eligibility descriptor probes never consume native load admission", () => {
                var f = new Files(3); using (f.Activate()) {
                    for (var i = 0; i < 20; i++) {
                        TestRunner.True(f.Scope.Validate(RuntimeSaveOperation.Load, f.Working, f.Root) == null, "Exact read-only eligibility refused.");
                        TestRunner.True(f.Authorization.ValidateLoadBeforeWorldReplacement(f.Working, f.Root) == null, "Exact pre-disposal eligibility refused.");
                    }
                    TestRunner.Equal(0, f.Scope.AdmittedNativeLoadRequests, "Eligibility spent native admission.");
                    TestRunner.Equal(0, f.Authorization.AuthorizedLoadCount, "Eligibility counted a LoadRoutine entry.");
                }
            });
            runner.Run("failed accepted load cannot refund its consumed native admission", () => {
                var f = new Files(1); using (f.Activate()) {
                    TestRunner.True(f.Authorization.Authorize(RuntimeSaveOperation.Load, f.Working, f.Root).Allowed, "Selected native entry refused.");
                    // Simulate no callback/completion. Admission is spent at entry,
                    // so a failure cannot cause an unselected recovery retry.
                    TestRunner.True(f.Authorization.ValidateLoadBeforeWorldReplacement(f.Working, f.Root) != null, "Missing completion refunded admission.");
                    TestRunner.True(!f.Authorization.Authorize(RuntimeSaveOperation.Load, f.Working, f.Root).Allowed, "A failed load allowed an automatic retry.");
                    TestRunner.Equal(1, f.Scope.AdmittedNativeLoadRequests, "Failure changed consumed count.");
                }
            });
            runner.Run("parallel actual native entries cannot exceed selected total", () => {
                var f = new Files(3); using (f.Activate()) {
                    var answers = new bool[16];
                    Parallel.For(0, answers.Length, i => answers[i] = f.Authorization.Authorize(RuntimeSaveOperation.Load, f.Working, f.Root).Allowed);
                    TestRunner.Equal(3, answers.Count(v => v), "Concurrent loads exceeded selected count.");
                    TestRunner.Equal(3, f.Scope.AdmittedNativeLoadRequests, "Concurrent scope count differs.");
                    TestRunner.Equal(3, f.Authorization.AuthorizedLoadCount, "Concurrent allowed entries were lost.");
                    TestRunner.Equal(13, f.Authorization.UnauthorizedLoadCount, "Concurrent refusal evidence was lost.");
                }
            });
            foreach (var name in new[] { "foreign campaign", "foreign name", "foreign area", "wrong native type", "foreign path", "protected baseline" })
                runner.Run("invalid target cannot consume repeated-load authority: " + name, () => {
                    var f = new Files(3); var target = f.Working;
                    switch (name) {
                        case "foreign campaign": target.GameId = "foreign"; break;
                        case "foreign name": target.GameName = "foreign"; break;
                        case "foreign area": target.Area = new string('a',32); break;
                        case "wrong native type": target.SaveType = "Quick"; break;
                        case "foreign path": target.FullPath = Path.Combine(f.RunRoot, target.FileName); break;
                        case "protected baseline": target.FileName = f.Fixture.Baseline.FileName; target.InternalName = RuntimeRequest.BaselineSaveName; target.FullPath = Path.Combine(f.Root,target.FileName); break;
                    }
                    using (f.Activate()) TestRunner.True(!f.Authorization.Authorize(RuntimeSaveOperation.Load,target,f.Root).Allowed, "Invalid target admitted.");
                    TestRunner.Equal(0,f.Scope.AdmittedNativeLoadRequests,"Refused identity consumed admission.");
                });
            runner.Run("exhaustion does not block inspection of already admitted immutable archive", () => {
                var f = new Files(1); var before = Hash(f.Working.FullPath); using (f.Activate()) {
                    TestRunner.True(f.Authorization.Authorize(RuntimeSaveOperation.Load,f.Working,f.Root).Allowed,"Native entry refused.");
                    f.Scope.AssertReadableArchive(f.Working.FullPath);
                    TestRunner.True(f.Scope.ValidateArchiveIdentity(f.Working,f.Root)==null,"Completed initial load lost its independent identity proof.");
                    TestRunner.Equal(1,f.Scope.AdmittedNativeLoadRequests,"Completed identity inspection consumed another load.");
                    TestRunner.Equal(before,Hash(f.Working.FullPath),"Admission or inspection changed the immutable input.");
                    TestRunner.True(f.Scope.Validate(RuntimeSaveOperation.Load,f.Working,f.Root)!=null,"Future load remained eligible.");
                }
            });
            runner.Run("post-load identity proof retains every selected immutable input guard", () => {
                var f=new Files(1);using(f.Activate()) {
                    TestRunner.True(f.Authorization.Authorize(RuntimeSaveOperation.Load,f.Working,f.Root).Allowed,"Initial native entry refused.");
                    var wrong=f.Working;wrong.GameId="foreign";
                    TestRunner.True(f.Scope.ValidateArchiveIdentity(wrong,f.Root)!=null,"Identity-only proof admitted a foreign campaign.");
                    TestRunner.True(f.Scope.ValidateArchiveIdentity(f.Working,f.RunRoot)!=null,"Identity-only proof admitted a foreign root.");
                    File.AppendAllText(f.Working.FullPath,"changed synthetic fixture bytes");
                    TestRunner.True(f.Scope.ValidateArchiveIdentity(f.Working,f.Root)!=null,"Identity-only proof ignored immutable hash substitution.");
                    TestRunner.Equal(1,f.Scope.AdmittedNativeLoadRequests,"Refused identity proof changed native allowance.");
                }
            });
            runner.Run("native write limit stays independent of selected load limit", () => {
                var f = new Files(3,1); using (f.Activate()) {
                    for(var i=0;i<3;i++) TestRunner.True(f.Authorization.Authorize(RuntimeSaveOperation.Load,f.Working,f.Root).Allowed,"Selected load refused.");
                    TestRunner.True(f.Authorization.Authorize(RuntimeSaveOperation.Write,f.Auto,f.Root).Allowed,"Load limit consumed ordinary write allowance.");
                    using(var writer=f.Scope.BeginWrite(f.Auto,f.Root)){File.WriteAllText(f.Auto.FullPath,"synthetic owned native write");writer.Complete();}
                    TestRunner.True(!f.Authorization.Authorize(RuntimeSaveOperation.Write,f.Auto,f.Root).Allowed,"Ordinary write limit was weakened.");
                    TestRunner.Equal(3,f.Scope.AdmittedNativeLoadRequests,"Write changed load admission count.");
                    TestRunner.Equal(1,f.Scope.AdmittedNativeWriteRequests,"Write count differs.");
                    f.Scope.AssertReadableArchive(f.Auto.FullPath);
                }
            });
            runner.Run("historical null load limit keeps prior repeated-load policy", () => {
                var f=new Files(null);using(f.Activate()) {
                    for(var i=0;i<5;i++)TestRunner.True(f.Authorization.Authorize(RuntimeSaveOperation.Load,f.Working,f.Root).Allowed,"Historical scope acquired an unsolicited load limit.");
                    TestRunner.True(f.Scope.MaximumNativeLoadRequests==null,"Historical limit differs.");
                }
            });
            runner.Run("releasing and reactivating same selected scope cannot reset load admission", () => {
                var f=new Files(1);using(f.Activate())TestRunner.True(f.Authorization.Authorize(RuntimeSaveOperation.Load,f.Working,f.Root).Allowed,"First entry refused.");
                using(f.Activate())TestRunner.True(f.Authorization.ValidateLoadBeforeWorldReplacement(f.Working,f.Root)!=null&&!f.Authorization.Authorize(RuntimeSaveOperation.Load,f.Working,f.Root).Allowed,"Scope reactivation reset selected allowance.");
            });
            // Zero is a valid selected load count since cba8262a (the native bootstrap makes no load).
            runner.Run("native load count admits an explicit zero-load selection",()=>{new Files(0);});
            foreach(var cap in new[]{-1,129})runner.Run("native load count refuses invalid bound "+cap,()=>{
                var refused=false;try{new Files(cap);}catch(ArgumentException){refused=true;}TestRunner.True(refused,"Invalid selected count accepted.");
            });
            return runner.Complete();
        }
        private static string Hash(string path)
        {
            using(var sha=SHA256.Create())using(var stream=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
        }
        private sealed class Files
        {
            internal readonly string RunRoot,Root;
            internal readonly PersistenceSaveAuthorization Scope;
            internal readonly RuntimeSaveAuthorization Authorization=new RuntimeSaveAuthorization();
            internal readonly RuntimeFixtureIdentity Fixture;
            internal Files(int? loadCap,int? writeCap=null)
            {
                RunRoot=Path.Combine(fixtureRoot,Guid.NewGuid().ToString("N"));Root=Path.Combine(RunRoot,"Saved Games");Directory.CreateDirectory(Root);
                var working=Path.Combine(Root,"Manual_299_KMC_AUTOMATION_WORKING.zks");File.WriteAllText(working,"synthetic owned fixture bytes");var hash=Hash(working);
                Fixture=new RuntimeFixtureIdentity {
                    Baseline=new RuntimeSaveDescriptor {InternalName=RuntimeRequest.BaselineSaveName,FileName="Manual_298_KMC_AUTOMATION_BASELINE.zks",Sha256=new string('a',64),Length=10,LastWriteTimeUtcTicks=638907120000000000L,GameId=GameId,GameName="KMC Fixture",Area=Area},
                    Working=new RuntimeSaveDescriptor {InternalName=RuntimeRequest.WorkingSaveName,FileName=Path.GetFileName(working),Sha256=hash,Length=new FileInfo(working).Length,LastWriteTimeUtcTicks=new FileInfo(working).LastWriteTimeUtc.Ticks,GameId=GameId,GameName="KMC Fixture",Area=Area},
                    WriteAuthorization=new RuntimeSaveWriteAuthorization {Mode="working-only",AllowedInternalName=RuntimeRequest.WorkingSaveName,AllowedFileName=Path.GetFileName(working),BaselineImmutable=true}};
                Scope=new PersistenceSaveAuthorization(RunRoot,GameId,"KMC Fixture",new string('a',64),new[] {
                    new PersistenceSaveEntry {FileName=Path.GetFileName(working),InternalName=RuntimeRequest.WorkingSaveName,SaveType="Manual",Area=Area,InitialSha256=hash,Writable=false},
                    new PersistenceSaveEntry {FileName="Auto_1.zks",InternalName="Autosave",SaveType="Auto",Area=Area,Writable=true}},null,null,writeCap,loadCap);
            }
            internal RuntimeSaveTarget Auto=>new RuntimeSaveTarget {FileName="Auto_1.zks",FullPath=Path.Combine(Root,"Auto_1.zks"),InternalName="Autosave",SaveType="Auto",GameId=GameId,GameName="KMC Fixture",Area=Area};
            internal RuntimeSaveTarget Working=>new RuntimeSaveTarget {FileName=Fixture.Working.FileName,FullPath=Path.Combine(Root,Fixture.Working.FileName),InternalName=RuntimeRequest.WorkingSaveName,SaveType="Manual",GameId=GameId,GameName="KMC Fixture",Area=Area};
            internal IDisposable Activate(){var lease=Authorization.Activate(Fixture,Root,false);Authorization.BindPersistenceScope(Scope);return lease;}
        }
    }
}
