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
    private static string Hash(string path)
    {
        using(var algorithm=System.Security.Cryptography.SHA256.Create())
        using(var stream=File.OpenRead(path))
            return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
    }
    private static System.Collections.Generic.IEnumerator<object> WriteHeader(Type type,object saver)
    {
        type.GetMethod("SaveJson").Invoke(saver,new object[]{"header","{\"LoadedTimes\":99}"});
        type.GetMethod("Save").Invoke(saver,null);
        yield break;
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
            foreach (var adapterName in new[]{"NativeCombatActorPersistence","NativeTurnPersistence","NativeCombatTurnPersistence"})
            {
                var adapter=candidate.GetType("KingmakerMountedCombat.Integration."+adapterName,true);
                System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(adapter.TypeHandle);
                Check(true,"exact native field/signature bindings: "+adapterName);
            }
            var saveGate=native.ManifestModule.ResolveMethod(0x06008028);
            var saveInstructions=(System.Collections.IEnumerable)read.Invoke(null,new object[]{saveGate,null});
            var saveLegacy=(System.Collections.IList)Activator.CreateInstance(listType);
            var originalOps=new System.Collections.Generic.List<object>();
            var originalOperands=new System.Collections.Generic.List<object>();
            foreach(var instruction in saveInstructions)
            {
                var type=instruction.GetType();
                var op=type.GetField("opcode").GetValue(instruction);
                var operand=type.GetField("operand").GetValue(instruction);
                originalOps.Add(op); originalOperands.Add(operand);
                saveLegacy.Add(Activator.CreateInstance(legacyInstruction,new[]{op,operand}));
            }
            var persistencePatches=candidate.GetType("KingmakerMountedCombat.Integration.MountedPatchController",true)
                .GetNestedType("PatchMethods",BindingFlags.NonPublic);
            var gateResult=(System.Collections.IEnumerable)persistencePatches.GetMethod("CombatSaveAdmissionTranspiler",
                BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{saveLegacy});
            int gateCount=0,gateChanges=0;
            foreach(var instruction in gateResult)
            {
                var op=legacyInstruction.GetField("opcode").GetValue(instruction);
                var operand=legacyInstruction.GetField("operand").GetValue(instruction);
                var oldMethod=originalOperands[gateCount] as MethodInfo;
                if(oldMethod!=null && oldMethod.Module==native.ManifestModule && oldMethod.MetadataToken==0x06000DBF)
                {
                    var changed=operand as MethodInfo;
                    Check(changed!=null && changed.Name=="CombatBlocksSave" &&
                        op.Equals(System.Reflection.Emit.OpCodes.Call),"only native combat predicate delegates to persistence");
                    gateChanges++;
                }
                else if(!Equals(op,originalOps[gateCount]) || !Equals(operand,originalOperands[gateCount]))
                    throw new InvalidOperationException("An unrelated native save predicate changed.");
                gateCount++;
            }
            Check(gateChanges==1 && gateCount==originalOps.Count,
                "native area/game-over/dialog/cutscene/encounter/dual-companion gates and branches retained");
            var settingsRefresh=native.GetType("Kingmaker.UI.SettingsUI.SettingsRoot",true)
                .GetMethod("HandleSettingsUpdated",BindingFlags.Public|BindingFlags.Static);
            Check(settingsRefresh!=null && settingsRefresh.MetadataToken==0x0600346B &&
                settingsRefresh.GetParameters().Length==0,"exact native boolean cache refresh seam");
            isolation.GetMethod("SettingsRefreshPostfix",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
            Check(true,"unbound persistence cache callback leaves ordinary configuration untouched");
            var nativeSaver=native.GetType("Kingmaker.EntitySystem.Persistence.ZipSaver",true);
            patch.Invoke(null,new object[]{harmony,nativeSaver,"SaveJson",0x06008063,new[]{typeof(string),typeof(string)},"LoadHeaderJsonPrefix",null});
            patch.Invoke(null,new object[]{harmony,nativeSaver,"Save",0x06008068,Type.EmptyTypes,"LoadHeaderCommitPrefix",null});
            patch.Invoke(null,new object[]{harmony,nativeSaver,"Clear",0x06008067,Type.EmptyTypes,"ClearPrefix",null});
            patch.Invoke(null,new object[]{harmony,nativeSaver,"RenameFile",0x0600806D,new[]{typeof(string)},"RenamePrefix",null});
            Check(true,"narrow native header, commit, clear and rename guards construct without iterator rewriting");
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
            var loadArgs=new object[]{null,null,false,null,false};
            Check(!(bool)callbacks.GetMethod("LoadPrefix",flags).Invoke(null,loadArgs) && !(bool)loadArgs[4],
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
        const string member="kmc-mounted-state";
        const string metadata="{\"schemaVersion\":1,\"campaignId\":\"fixture\",\"riderId\":\"native-actor\"}";
        var loaderType=native.GetType("Kingmaker.EntitySystem.Persistence.ThreadedGameLoader",true);
        var loader=Activator.CreateInstance(loaderType,new object[]{null,false});
        var inventory=loaderType.GetMethod("CreateStateData",BindingFlags.NonPublic|BindingFlags.Instance);
        Check(inventory.MetadataToken==0x06008055,"exact native archive area-inventory contract");
        inventory.Invoke(loader,new object[]{new System.Collections.Generic.List<string>{"header.json",member}});
        Check(true,"extensionless metadata does not create a native area record");
        object saver=Activator.CreateInstance(saverType,new object[]{path});
        try
        {
            saverType.GetMethod("SaveJson").Invoke(saver,new object[]{"header","{\"Name\":\"owned storage probe\"}"});
            saverType.GetMethod("SaveBytes").Invoke(saver,new object[]{member,System.Text.Encoding.UTF8.GetBytes(metadata)});
            Check(!File.Exists(path),"native metadata addition does not write before Save");
            saverType.GetMethod("Save").Invoke(saver,null);
            Check(File.Exists(path),"native archive Save writes an owned archive");
            Check(System.Text.Encoding.UTF8.GetString((byte[])saverType.GetMethod("ReadBytes").Invoke(saver,new object[]{member}))==metadata,
                "owned metadata survives native archive commit");
            ((IDisposable)saver).Dispose();
            var clone=saverType.GetMethod("Clone").Invoke(saver,null);
            try
            {
                Check(System.Text.Encoding.UTF8.GetString((byte[])saverType.GetMethod("ReadBytes").Invoke(clone,new object[]{member}))==metadata,
                    "metadata reads through a fresh native saver clone");
                saverType.GetMethod("SaveJson").Invoke(clone,new object[]{"header","{\"LoadedTimes\":1}"});
                saverType.GetMethod("Save").Invoke(clone,null);
            }
            finally { ((IDisposable)clone).Dispose(); }
            Check(System.Text.Encoding.UTF8.GetString((byte[])saverType.GetMethod("ReadBytes").Invoke(saver,new object[]{member}))==metadata,
                "native header update preserves unknown archive members");
            ((IDisposable)saver).Dispose();
            saverType.GetMethod("RenameFile").Invoke(saver,new object[]{renamed});
            Check(!File.Exists(path) && File.Exists(renamed),"native rename moves the whole owned archive");
            Check(System.Text.Encoding.UTF8.GetString((byte[])saverType.GetMethod("ReadBytes").Invoke(saver,new object[]{member}))==metadata,
                "metadata survives rename without original filename or sidecar");
        }
        finally
        {
            ((IDisposable)saver).Dispose();
            foreach(var file in new[]{path,renamed}) if(File.Exists(file)) File.Delete(file);
        }
        var isolated=Path.Combine(owned,"Saved Games");
        Directory.CreateDirectory(isolated);
        var readPath=Path.Combine(isolated,"owned-read.zks");
        var readSaver=Activator.CreateInstance(saverType,new object[]{readPath});
        var binding=BindingFlags.NonPublic|BindingFlags.Static;
        try
        {
            saverType.GetMethod("SaveJson").Invoke(readSaver,new object[]{"header","{\"LoadedTimes\":0}"});
            saverType.GetMethod("Save").Invoke(readSaver,null);
            var before=Hash(readPath);
            var entryType=candidate.GetType("KingmakerMountedCombat.Diagnostics.PersistenceSaveEntry",true);
            var entry=Activator.CreateInstance(entryType,true);
            foreach(var pair in new[]{new[]{"FileName","owned-read.zks"},new[]{"InternalName","owned"},
                new[]{"SaveType","Manual"},new[]{"Area",new string('a',32)},new[]{"InitialSha256",before}})
                entryType.GetProperty(pair[0]).SetValue(entry,pair[1],null);
            var writable=Activator.CreateInstance(entryType,true);
            foreach(var pair in new[]{new[]{"FileName","owned-write.zks"},new[]{"InternalName","owned-write"},
                new[]{"SaveType","Manual"},new[]{"Area",new string('a',32)}})
                entryType.GetProperty(pair[0]).SetValue(writable,pair[1],null);
            entryType.GetProperty("Writable").SetValue(writable,true,null);
            var entries=Array.CreateInstance(entryType,2);entries.SetValue(entry,0);entries.SetValue(writable,1);
            var authorityType=candidate.GetType("KingmakerMountedCombat.Diagnostics.PersistenceSaveAuthorization",true);
            var authority=authorityType.GetConstructors(BindingFlags.NonPublic|BindingFlags.Instance)[0].Invoke(
                new object[]{owned,"owned-campaign","owned",new string('b',64),entries});
            isolation.GetMethod("Bind",binding).Invoke(null,new[]{authority});
            var patch=isolation.GetMethod("Patch",binding);
            patch.Invoke(null,new object[]{harmony,saverType,"SaveJson",0x06008063,new[]{typeof(string),typeof(string)},"LoadHeaderJsonPrefix",null});
            patch.Invoke(null,new object[]{harmony,saverType,"Save",0x06008068,Type.EmptyTypes,"LoadHeaderCommitPrefix",null});
            var routine=(System.Collections.Generic.IEnumerator<object>)isolation.GetMethod("WrapReadOnlyLoad",binding).Invoke(
                null,new object[]{WriteHeader(saverType,readSaver),readPath});
            using(routine){while(routine.MoveNext()){}}
            Check(Hash(readPath)==before,"enumerated isolated load preserves actual native archive bytes");
            bool unleasedDenied=false;
            try { using(var ordinary=WriteHeader(saverType,readSaver)){while(ordinary.MoveNext()){}} }
            catch(TargetInvocationException e) { unleasedDenied=e.InnerException is InvalidOperationException; }
            Check(unleasedDenied && Hash(readPath)==before,"outside-load commit requires an explicit write lease");
            var writePath=Path.Combine(isolated,"owned-write.zks");
            var writeSaver=Activator.CreateInstance(saverType,new object[]{writePath});
            var targetType=candidate.GetType("KingmakerMountedCombat.Diagnostics.RuntimeSaveTarget",true);
            var target=Activator.CreateInstance(targetType,true);
            foreach(var pair in new[]{new[]{"InternalName","owned-write"},new[]{"FileName","owned-write.zks"},
                new[]{"FullPath",writePath},new[]{"SaveType","Manual"},new[]{"GameId","owned-campaign"},
                new[]{"GameName","owned"},new[]{"Area",new string('a',32)}})
                targetType.GetProperty(pair[0]).SetValue(target,pair[1],null);
            var writeLease=authorityType.GetMethod("BeginWrite",BindingFlags.NonPublic|BindingFlags.Instance)
                .Invoke(authority,new[]{target,isolated});
            var pending=(System.Collections.IDictionary)isolation.GetField("writes",binding).GetValue(null);
            pending.Add(writeSaver,writeLease);
            try
            {
                saverType.GetMethod("SaveJson").Invoke(writeSaver,new object[]{"header","{}"});
                Check(!File.Exists(writePath),"authorized native metadata stage does not claim a disk write");
                saverType.GetMethod("Save").Invoke(writeSaver,null);
                Check(File.Exists(writePath) && !pending.Contains(writeSaver),"real native commit completes and releases its exact write lease");
                authorityType.GetMethod("AssertReadableArchive",BindingFlags.NonPublic|BindingFlags.Instance)
                    .Invoke(authority,new object[]{writePath});
                Check(true,"completed native archive becomes readable through the same strict authority");
            }
            finally { ((IDisposable)writeSaver).Dispose(); ((IDisposable)writeLease).Dispose(); pending.Remove(writeSaver); if(File.Exists(writePath)) File.Delete(writePath); }
            isolation.GetField("authority",binding).SetValue(null,null);
            using(var ordinary=WriteHeader(saverType,readSaver)){while(ordinary.MoveNext()){}}
            Check(Hash(readPath)!=before,"unbound ordinary native writer retains normal behavior");
        }
        finally
        {
            harmonyType.GetMethod("UnpatchAll").Invoke(harmony,new object[]{id});
            isolation.GetField("authority",binding).SetValue(null,null);
            ((IDisposable)readSaver).Dispose();
            if(File.Exists(readPath)) File.Delete(readPath);
            Directory.Delete(isolated);
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
