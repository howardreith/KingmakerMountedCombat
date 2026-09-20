[CmdletBinding()]
param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
$kmcRepo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$kmcLab=[IO.Path]::GetFullPath((Join-Path $kmcRepo '../..'))
$kmcLayout=(Get-Content -Raw (Join-Path $kmcLab 'environment-intake.json')|ConvertFrom-Json).requestedLayout
$kmcManaged=Join-Path $kmcLayout.kingmakerInstallDir 'Kingmaker_Data/Managed'
$kmcDll=Join-Path $kmcRepo "bin/$Configuration/KingmakerMountedCombat.dll"
$kmcOwned=Join-Path $kmcLab ('analysis-cache/chunk5-contract-tests/'+[Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($kmcOwned)
Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.Reflection;
public static class KmcPersistenceContractProbe
{
    private static int passes;
    private static void Check(bool value, string label)
    {
        if(!value) throw new InvalidOperationException(label);
        passes++;
        Console.WriteLine("PASS "+label);
    }
    public static void Run(string managed, string candidatePath, string owned)
    {
        ResolveEventHandler resolver=(sender,args)=>{
            var leaf=new AssemblyName(args.Name).Name+".dll";
            foreach(var folder in new[]{managed,Path.Combine(managed,"UnityModManager")})
            {
                var dependency=Path.Combine(folder,leaf);
                if(File.Exists(dependency)) return Assembly.LoadFrom(dependency);
            }
            return null;
        };
        AppDomain.CurrentDomain.AssemblyResolve+=resolver;
        var native=Assembly.LoadFrom(Path.Combine(managed,"Assembly-CSharp.dll"));
        var candidate=Assembly.LoadFrom(candidatePath);
        Check(native.ManifestModule.ModuleVersionId==new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"),"exact Kingmaker persistence MVID");
        var harmonyAssembly=Assembly.LoadFrom(Path.Combine(managed,"UnityModManager/0Harmony12.dll"));
        var harmonyType=harmonyAssembly.GetType("Harmony12.HarmonyInstance",true);
        const string id="KingmakerMountedCombat.PersistenceDetachedTest";
        var harmony=harmonyType.GetMethod("Create").Invoke(null,new object[]{id});
        var isolation=candidate.GetType("KingmakerMountedCombat.Integration.NativePersistenceIsolation",true);
        try
        {
            var patch=isolation.GetMethod("Patch",BindingFlags.Static|BindingFlags.NonPublic);
            var manager=native.GetType("Kingmaker.EntitySystem.Persistence.SaveManager",true);
            patch.Invoke(null,new object[]{harmony,manager,"get_SavePath",0x0600800C,Type.EmptyTypes,"SavePathPrefix",null});
            patch.Invoke(null,new object[]{harmony,manager,"UpdateSaveListAsync",0x0600800E,Type.EmptyTypes,null,"SaveRootTranspiler"});
            Check(true,"native save-root getter and refresh patches construct");
            // PrepareSave directly references Unity ECalls. Decode its real IL and
            // exercise the production transformer; its JIT installation requires
            // the actual Unity process and receives no detached PASS claim.
            var modern=Assembly.LoadFrom(Path.Combine(managed,"UnityModManager/0Harmony.dll"));
            var processor=modern.GetType("HarmonyLib.PatchProcessor",true);
            MethodInfo read=null;
            foreach(var method in processor.GetMethods(BindingFlags.Public|BindingFlags.Static))
                if(method.Name=="GetOriginalInstructions" && method.GetParameters().Length==2 &&
                    !method.GetParameters()[1].ParameterType.IsByRef) read=method;
            var original=native.ManifestModule.ResolveMethod(0x06008025);
            var instructions=(System.Collections.IEnumerable)read.Invoke(null,new object[]{original,null});
            var legacyInstruction=harmonyAssembly.GetType("Harmony12.CodeInstruction",true);
            var listType=typeof(System.Collections.Generic.List<>).MakeGenericType(legacyInstruction);
            var legacy=(System.Collections.IList)Activator.CreateInstance(listType);
            foreach(var instruction in instructions)
            {
                var type=instruction.GetType();
                legacy.Add(Activator.CreateInstance(legacyInstruction,new[]{
                    type.GetField("opcode").GetValue(instruction),type.GetField("operand").GetValue(instruction)}));
            }
            var transformed=(System.Collections.IEnumerable)isolation.GetMethod("SaveRootTranspiler",
                BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{legacy,original});
            var replaced=0; var retained=0;
            foreach(var instruction in transformed)
            {
                var operandMember=legacyInstruction.GetField("operand").GetValue(instruction) as MethodInfo;
                if(operandMember==null) continue;
                if(operandMember.DeclaringType==isolation && operandMember.Name=="IsolatedDataParent") replaced++;
                if(operandMember.Module==native.ManifestModule && operandMember.MetadataToken==0x06001BC7) retained++;
            }
            Check(replaced==2 && retained==0,"both exact native descriptor path branches are rewritten");
            var loadOriginal=native.ManifestModule.ResolveMethod(0x0600BF00);
            var loadInstructions=(System.Collections.IEnumerable)read.Invoke(null,new object[]{loadOriginal,null});
            var loadLegacy=(System.Collections.IList)Activator.CreateInstance(listType);
            foreach(var instruction in loadInstructions)
            {
                var instructionType=instruction.GetType();
                loadLegacy.Add(Activator.CreateInstance(legacyInstruction,new[]{
                    instructionType.GetField("opcode").GetValue(instruction),instructionType.GetField("operand").GetValue(instruction)}));
            }
            var guardedLoad=(System.Collections.IEnumerable)isolation.GetMethod("LoadHeaderTranspiler",
                BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{loadLegacy});
            var loadReplaced=0; var loadWritesRetained=0;
            foreach(var instruction in guardedLoad)
            {
                var called=legacyInstruction.GetField("operand").GetValue(instruction) as MethodInfo;
                if(called==null) continue;
                if(called.DeclaringType==isolation && (called.Name=="LoadHeaderJson" || called.Name=="LoadHeaderCommit")) loadReplaced++;
                if(called.Module==native.ManifestModule && (called.MetadataToken==0x06007FAE || called.MetadataToken==0x06007FB3)) loadWritesRetained++;
            }
            Check(loadReplaced==2 && loadWritesRetained==0,"exact native load-header writes route through isolated guard");
            Check((bool)isolation.GetMethod("CloudPrefix",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null),
                "unbound isolation leaves ordinary cloud behavior unchanged");
            Console.WriteLine("TODO native Unity construction of PrepareSave/load/stash/cloud isolation; no native write authorized");
        }
        finally { harmonyType.GetMethod("UnpatchAll").Invoke(harmony,new object[]{id}); }
        // Use the real strict authorization service and a real qualified fixture
        // DTO. The unavailable game-world boundary is a sentinel: touching it
        // before rejecting the invalid request must fail this test.
        var testPath=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(candidatePath),"..","Tests",
            new DirectoryInfo(Path.GetDirectoryName(candidatePath)).Name,"KingmakerMountedCombat.Tests.exe"));
        var component=Assembly.LoadFrom(testPath);
        var fixtureSource=component.GetType("KingmakerMountedCombat.Tests.RuntimeSaveAuthorizationTests",true)
            .GetMethod("ValidFixture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        var json=Assembly.LoadFrom(Path.Combine(managed,"Newtonsoft.Json.dll")).GetType("Newtonsoft.Json.JsonConvert",true);
        var fixtureJson=(string)json.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{fixtureSource});
        var fixture=json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)}).Invoke(null,new object[]{
            fixtureJson,candidate.GetType("KingmakerMountedCombat.Diagnostics.RuntimeFixtureIdentity",true)});
        var authorizationType=candidate.GetType("KingmakerMountedCombat.Diagnostics.RuntimeSaveAuthorization",true);
        var authorization=Activator.CreateInstance(authorizationType,true);
        var lease=(IDisposable)authorizationType.GetMethod("Activate").Invoke(authorization,new object[]{fixture,owned,false});
        var controller=candidate.GetType("KingmakerMountedCombat.Integration.MountedPatchController",true);
        var bridge=controller.GetNestedType("PatchBridge",BindingFlags.NonPublic);
        var callbacks=controller.GetNestedType("PatchMethods",BindingFlags.NonPublic);
        var flags=BindingFlags.NonPublic|BindingFlags.Static;
        try
        {
            bridge.GetField("SaveAuthorization",flags).SetValue(null,authorization);
            bridge.GetField("Service",flags).SetValue(null,System.Runtime.Serialization.FormatterServices.GetUninitializedObject(
                candidate.GetType("KingmakerMountedCombat.Integration.GameMountedRelationshipService",true)));
            var saveArgs=new object[]{null,null,false,null,false};
            Check(!(bool)callbacks.GetMethod("SavePrefix",flags).Invoke(null,saveArgs) && !(bool)saveArgs[4],
                "denied native save returns before relationship/control cleanup");
            var loadArgs=new object[]{null,null,false,null};
            Check(!(bool)callbacks.GetMethod("LoadPrefix",flags).Invoke(null,loadArgs),
                "denied native load returns before relationship cleanup");
            Check((int)authorizationType.GetProperty("UnauthorizedWriteCount").GetValue(authorization,null)==1 &&
                (int)authorizationType.GetProperty("UnauthorizedLoadCount").GetValue(authorization,null)==1,
                "denied boundaries retain strict authorization telemetry");
        }
        finally
        {
            bridge.GetField("SaveAuthorization",flags).SetValue(null,null);
            bridge.GetField("Service",flags).SetValue(null,null);
            lease.Dispose();
        }
        var saverType=native.GetType("Kingmaker.EntitySystem.Persistence.ZipSaver",true);
        var path=Path.Combine(owned,"owned-native-archive.zks");
        var renamed=Path.Combine(owned,"owned-renamed.zks");
        const string member="KingmakerMountedCombat";
        const string metadata="{\"schemaVersion\":1,\"campaignId\":\"fixture\",\"riderId\":\"native-actor\"}";
        object saver=Activator.CreateInstance(saverType,new object[]{path});
        try
        {
            saverType.GetMethod("SaveJson").Invoke(saver,new object[]{"header","{\"Name\":\"owned storage probe\"}"});
            saverType.GetMethod("SaveJson").Invoke(saver,new object[]{member,metadata});
            Check(!File.Exists(path),"native metadata addition does not write before Save");
            saverType.GetMethod("Save").Invoke(saver,null);
            Check(File.Exists(path),"native archive Save writes an owned archive");
            Check((string)saverType.GetMethod("ReadJson").Invoke(saver,new object[]{member})==metadata,
                "owned metadata survives native archive commit");
            ((IDisposable)saver).Dispose();
            var clone=saverType.GetMethod("Clone").Invoke(saver,null);
            try
            {
                Check((string)saverType.GetMethod("ReadJson").Invoke(clone,new object[]{member})==metadata,
                    "metadata reads through a fresh native saver clone");
                saverType.GetMethod("SaveJson").Invoke(clone,new object[]{"header","{\"LoadedTimes\":1}"});
                saverType.GetMethod("Save").Invoke(clone,null);
            }
            finally { ((IDisposable)clone).Dispose(); }
            Check((string)saverType.GetMethod("ReadJson").Invoke(saver,new object[]{member})==metadata,
                "native header update preserves unknown archive members");
            ((IDisposable)saver).Dispose();
            saverType.GetMethod("RenameFile").Invoke(saver,new object[]{renamed});
            Check(!File.Exists(path) && File.Exists(renamed),"native rename moves the whole owned archive");
            Check((string)saverType.GetMethod("ReadJson").Invoke(saver,new object[]{member})==metadata,
                "metadata survives rename without original filename or sidecar");
        }
        finally
        {
            ((IDisposable)saver).Dispose();
            foreach(var file in new[]{path,renamed}) if(File.Exists(file)) File.Delete(file);
        }
        Console.WriteLine("PERSISTENCE ASSEMBLY/STORAGE CONTRACT PASS="+passes+" FAIL=0; not native gameplay qualification");
    }
}
'@
try { [KmcPersistenceContractProbe]::Run($kmcManaged,$kmcDll,$kmcOwned) }
catch { Write-Output $_.Exception.ToString(); exit 1 }
finally {
    # Remove only this exact empty probe directory, never recursively.
    if((Test-Path -LiteralPath $kmcOwned)-and @(Get-ChildItem -LiteralPath $kmcOwned -Force).Count-eq0){
        [IO.Directory]::Delete($kmcOwned)
    }
}
