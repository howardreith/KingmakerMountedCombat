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
    private static void SetMember(object instance, string name, object value)
    {
        var type=instance.GetType();var property=type.GetProperty(name);
        if(property!=null) property.SetValue(instance,value,null);
        else type.GetField(name).SetValue(instance,value);
    }

    private static void VerifyNativeColdDescriptor(Assembly native, Assembly candidate, string owned)
    {
        var infoType=native.GetType("Kingmaker.EntitySystem.Persistence.SaveInfo",true);
        var expectedType=candidate.GetType("KingmakerMountedCombat.Diagnostics.RuntimeSaveDescriptor",true);
        var verify=candidate.GetType("KingmakerMountedCombat.Diagnostics.WorkingFixtureLoader",true)
            .GetMethod("VerifyDescriptor",BindingFlags.Static|BindingFlags.NonPublic);
        foreach(var category in new[]{"Manual","Quick","Auto"})
        {
            var path=Path.Combine(owned,category+"_1.zks");
            var info=Activator.CreateInstance(infoType);var expected=Activator.CreateInstance(expectedType);
            foreach(var name in new[]{"Name","GameId","GameName"}) SetMember(info,name,"owned");
            SetMember(info,"FolderName",path);SetMember(info,"CompatibilityVersion",1);
            SetMember(info,"Type",Enum.Parse(infoType.GetNestedType("SaveType"),category));
            foreach(var pair in new[]{new[]{"InternalName","owned"},new[]{"FileName",Path.GetFileName(path)},
                new[]{"GameId","owned"},new[]{"GameName","owned"}}) SetMember(expected,pair[0],pair[1]);
            verify.Invoke(null,new object[]{info,expected,path,category});
            Check(true,"real native "+category+" descriptor admits only its declared category");
            bool rejected=false;
            try{verify.Invoke(null,new object[]{info,expected,path,category=="Manual"?"Quick":"Manual"});}
            catch(TargetInvocationException e){rejected=e.InnerException is InvalidOperationException;}
            Check(rejected,"real native "+category+" descriptor rejects category substitution");
            SetMember(expected,"GameId","foreign");rejected=false;
            try{verify.Invoke(null,new object[]{info,expected,path,category});}
            catch(TargetInvocationException e){rejected=e.InnerException is InvalidOperationException;}
            Check(rejected,"real native "+category+" descriptor retains campaign identity protection");
        }
    }

    private sealed class DisposeProbe : System.Collections.Generic.IEnumerator<object>
    {
        internal int Disposals, Moves;
        internal bool FailDispose, FailMove;
        public object Current { get { return null; } }
        object System.Collections.IEnumerator.Current { get { return Current; } }
        public bool MoveNext() { Moves++; if(FailMove) throw new InvalidOperationException("native move"); return true; }
        public void Reset() { throw new NotSupportedException(); }
        public void Dispose() { Disposals++; if(FailDispose) throw new InvalidOperationException("owned disposal"); }
    }
    private static void VerifyNativeTouchCommitment(Assembly native, Assembly candidate)
    {
        var flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        Func<Type,object> blank=t=>System.Runtime.Serialization.FormatterServices.GetUninitializedObject(t);
        var actorType=native.GetType("Kingmaker.EntitySystem.Entities.UnitEntityData",true);
        var descriptorType=native.GetType("Kingmaker.UnitLogic.UnitDescriptor",true);
        var partType=native.GetType("Kingmaker.UnitLogic.Parts.UnitPartTouch",true);
        var abilityType=native.GetType("Kingmaker.UnitLogic.Abilities.Ability",true);
        var dataType=native.GetType("Kingmaker.UnitLogic.Abilities.AbilityData",true);
        var commandType=native.GetType("Kingmaker.UnitLogic.Commands.UnitUseAbility",true);
        var baseCommand=native.GetType("Kingmaker.UnitLogic.Commands.Base.UnitCommand",true);
        var actor=blank(actorType);var descriptor=blank(descriptorType);
        actorType.GetField("<Descriptor>k__BackingField",flags).SetValue(actor,descriptor);
        var partsField=descriptorType.GetField("m_Parts",flags);
        var manager=Activator.CreateInstance(partsField.FieldType,new[]{descriptor});
        partsField.SetValue(descriptor,manager);
        var parts=(System.Collections.IDictionary)manager.GetType().GetField("m_Parts",flags).GetValue(manager);
        var command=blank(commandType);var ability=blank(abilityType);var data=blank(dataType);var part=blank(partType);
        var effect=candidate.GetType("KingmakerMountedCombat.Integration.NativeSaveEffectBoundary",true);
        var settle=effect.GetMethod("CommandNeedsSettlement",BindingFlags.Static|BindingFlags.NonPublic);
        var start=effect.GetMethod("MayStartDuringWait",BindingFlags.Static|BindingFlags.NonPublic);
        Check(!(bool)settle.Invoke(null,new[]{command}) && !(bool)start.Invoke(null,new[]{command}),
            "new unowned native cast cannot start during a save wait");
        baseCommand.GetField("<Executor>k__BackingField",flags).SetValue(command,actor);
        commandType.GetField("Spell").SetValue(command,data);
        abilityType.GetField("Data").SetValue(ability,data);
        partType.GetField("<Ability>k__BackingField",flags).SetValue(part,ability);
        parts.Add(partType,part);
        Check((bool)settle.Invoke(null,new[]{command}) && (bool)start.Invoke(null,new[]{command}),
            "exact native held-touch command starts and settles the already spent spell");
        commandType.GetField("Spell").SetValue(command,blank(dataType));
        Check(!(bool)settle.Invoke(null,new[]{command}) && !(bool)start.Invoke(null,new[]{command}),
            "another selected spell cannot use an existing touch part to bypass the wait");
        commandType.GetField("Spell").SetValue(command,data);parts.Remove(partType);
        Check(!(bool)settle.Invoke(null,new[]{command}) && !(bool)start.Invoke(null,new[]{command}),
            "removed native touch ownership cannot authorize stale delivery");
        parts.Add(partType,part);
        baseCommand.GetProperty("IsFinished").GetSetMethod(true).Invoke(command,new object[]{true});
        Check(!(bool)settle.Invoke(null,new[]{command}) && !(bool)start.Invoke(null,new[]{command}),
            "completed touch delivery cannot run again or delay the save");
    }

    private static void VerifyNativeAbilitySettlement(Assembly native, Assembly candidate)
    {
        var controllerType=native.GetType("Kingmaker.Controllers.AbilityExecutionController",true);
        var processType=native.GetType("Kingmaker.Controllers.AbilityExecutionProcess",true);
        var flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        var field=controllerType.GetField("m_Abilities",flags);
        var controller=Activator.CreateInstance(controllerType);
        var list=(System.Collections.IList)field.GetValue(controller);
        var adapter=candidate.GetType("KingmakerMountedCombat.Integration.NativeSaveEffectBoundary",true);
        var read=adapter.GetMethod("HasUnresolvedAbilities",BindingFlags.Static|BindingFlags.NonPublic,null,new[]{controllerType},null);
        Check(field.MetadataToken==0x04005D50 && !(bool)read.Invoke(null,new[]{controller}),
            "exact native empty ability controller is save-safe");
        var process=System.Runtime.Serialization.FormatterServices.GetUninitializedObject(processType);
        list.Add(process);
        Check((bool)read.Invoke(null,new[]{controller}),"native ability effect remains unsettled without any live command");
        Check(list.Count==1 && object.ReferenceEquals(list[0],process) && processType.GetField("m_Process",flags).GetValue(process)==null,
            "save admission does not enumerate or retire native ability effects");
        processType.GetProperty("IsEnded").GetSetMethod(true).Invoke(process,new object[]{true});
        Check(!(bool)read.Invoke(null,new[]{controller}),"completed native ability awaiting controller retirement permits a save");
        list.Add(System.Runtime.Serialization.FormatterServices.GetUninitializedObject(processType));
        Check((bool)read.Invoke(null,new[]{controller}),"a second unfinished native spell cannot hide behind a completed process");
    }

    private static void VerifyNativeDeferredOwner(Assembly native, Assembly candidate)
    {
        var serviceType=candidate.GetType("KingmakerMountedCombat.Integration.MountedPersistenceService",true);
        var faultType=serviceType.GetNestedType("SaveWaitFault",BindingFlags.NonPublic);
        var saveType=native.GetType("Kingmaker.EntitySystem.Persistence.SaveInfo",true);
        var faultSave=Activator.CreateInstance(saveType);
        var faultOwner=System.Runtime.Serialization.FormatterServices.GetUninitializedObject(serviceType);
        var fault=faultType.GetConstructors(BindingFlags.Instance|BindingFlags.NonPublic)[0].Invoke(new[]{faultOwner,faultSave});
        var claim=faultType.GetMethod("TryClaim",BindingFlags.Instance|BindingFlags.NonPublic);
        Check(!(bool)claim.Invoke(fault,new object[]{null}) && !(bool)claim.Invoke(fault,new[]{Activator.CreateInstance(saveType)}) &&
            (bool)claim.Invoke(fault,new[]{faultSave}) && !(bool)claim.Invoke(fault,new[]{faultSave}),
            "owned wait fault can affect only one exact native request");
        ((IDisposable)fault).Dispose();
        Check(!(bool)claim.Invoke(fault,new[]{faultSave}),"disposed wait fault cannot affect another save");
        var requireOwned=candidate.GetType("KingmakerMountedCombat.Integration.NativePersistenceIsolation",true)
            .GetMethod("RequireOwnedWrite",BindingFlags.Static|BindingFlags.NonPublic);
        var unboundRejected=false;
        try{requireOwned.Invoke(null,new[]{faultSave});}
        catch(TargetInvocationException e){unboundRejected=e.InnerException is InvalidOperationException;}
        Check(unboundRejected,"fault injection cannot arm outside isolated write authority");
        var ownerType=native.GetType("Kingmaker.EntitySystem.Persistence.LoadingProcess",true);
        var flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        var queuedType=ownerType.GetNestedType("QueuedProcess",BindingFlags.NonPublic);
        var process=queuedType.GetField("Process");
        var owner=System.Runtime.Serialization.FormatterServices.GetUninitializedObject(ownerType);
        var queue=Activator.CreateInstance(ownerType.GetField("m_Queue",flags).FieldType);
        ownerType.GetField("m_Queue",flags).SetValue(owner,queue);
        var wrapperType=candidate.GetType("KingmakerMountedCombat.Domain.DeferredSaveEnumerator`1",true).MakeGenericType(typeof(object));
        var constructor=wrapperType.GetConstructors(flags)[0];
        var currentInner=new DisposeProbe { FailDispose=true };
        var queuedInner=new DisposeProbe();
        var foreign=new DisposeProbe();
        var restored=0; var failures=0;
        Func<DisposeProbe,object> wrap=inner=>constructor.Invoke(new object[]{inner,
            new Func<bool>(()=>false),new Func<double>(()=>0),new Action(()=>{}),new Action(()=>{}),
            new Action(()=>restored++),30d});
        var current=wrap(currentInner);var next=wrap(queuedInner);
        wrapperType.GetMethod("Activate",flags).Invoke(current,new object[]{new Action(()=>{})});
        var record=Activator.CreateInstance(queuedType,true);process.SetValue(record,current);
        ownerType.GetField("m_CurrentProcess",flags).SetValue(owner,record);
        foreach(var item in new object[]{next,foreign})
        {
            var entry=Activator.CreateInstance(queuedType,true);process.SetValue(entry,item);
            queue.GetType().GetMethod("Enqueue").Invoke(queue,new[]{entry});
        }
        var adapter=candidate.GetType("KingmakerMountedCombat.Integration.NativeDeferredSave",true);
        var waiting=adapter.GetMethod("Waiting",BindingFlags.NonPublic|BindingFlags.Static);
        Check((bool)waiting.Invoke(null,new[]{owner}),"selected native queued save owns the live wait");
        var abandon=adapter.GetMethod("AbandonOwned",BindingFlags.NonPublic|BindingFlags.Static);
        var report=new Action<Exception>(e=>{failures++;});
        abandon.Invoke(null,new object[]{owner,report});abandon.Invoke(null,new object[]{owner,report});
        Check(!(bool)waiting.Invoke(null,new[]{owner}) && currentInner.Disposals==1 && queuedInner.Disposals==1 &&
            restored==1 && failures==1,"native owner cancellation releases current and queued owned iterators despite disposal failure");
        Check(foreign.Disposals==0 && (int)queue.GetType().GetProperty("Count").GetValue(queue,null)==2,
            "owned cleanup leaves foreign iterators and native queue mutation to native StopAll");
        ownerType.GetField("m_CurrentProcess",flags).SetValue(owner,null);
        Check(!(bool)waiting.Invoke(null,new[]{owner}),"no current save leaves the native loading predicate unchanged");

        var callbackField=queuedType.GetField("Callback");
        Check(callbackField.MetadataToken==0x04008CAF,"exact native completion callback signature");
        var tick=adapter.GetMethod("MoveNext",BindingFlags.Static|BindingFlags.NonPublic);
        var now=0d; var timeoutInner=new DisposeProbe(); var timeoutRestored=0;
        var timeout=constructor.Invoke(new object[]{timeoutInner,new Func<bool>(()=>false),
            new Func<double>(()=>now),new Action(()=>{}),new Action(()=>{}),new Action(()=>timeoutRestored++),30d});
        wrapperType.GetMethod("Activate",flags).Invoke(timeout,new object[]{new Action(()=>{})}); now=31;
        var failedRecord=Activator.CreateInstance(queuedType,true);process.SetValue(failedRecord,timeout);
        var callback=new Action(()=>{throw new Exception("Failed save callback must never run");});
        callbackField.SetValue(failedRecord,callback);ownerType.GetField("m_CurrentProcess",flags).SetValue(owner,failedRecord);
        Check(!(bool)tick.Invoke(null,new object[]{timeout,owner}) && callbackField.GetValue(failedRecord)==null &&
            timeoutInner.Disposals==1 && timeoutInner.Moves==0 && timeoutRestored==1 &&
            object.ReferenceEquals(ownerType.GetField("m_CurrentProcess",flags).GetValue(owner),failedRecord),
            "owned timeout retires only its success callback after full cleanup and retains native queue ownership");
        foreach(var phase in new[]{"foreign","serialized","cleanup","unselected"})
        {
            var inner=new DisposeProbe { FailMove=phase=="foreign"||phase=="serialized",FailDispose=phase=="cleanup" };
            now=0;object iterator=inner;
            if(phase!="foreign") {
                iterator=constructor.Invoke(new object[]{inner,new Func<bool>(()=>phase=="serialized"),
                    new Func<double>(()=>now),new Action(()=>{}),new Action(()=>{}),new Action(()=>{}),30d});
                wrapperType.GetMethod("Activate",flags).Invoke(iterator,new object[]{new Action(()=>{})});
                now=31;
            }
            var entry=Activator.CreateInstance(queuedType,true);
            process.SetValue(entry,phase=="unselected"?(object)new DisposeProbe():iterator);
            callbackField.SetValue(entry,callback);ownerType.GetField("m_CurrentProcess",flags).SetValue(owner,entry);
            var rejected=false;
            try { tick.Invoke(null,new object[]{iterator,owner}); }
            catch(TargetInvocationException) { rejected=true; }
            Check(rejected && object.ReferenceEquals(callbackField.GetValue(entry),callback),
                "save recovery propagates "+phase+" failures without clearing a native callback");
        }
        ownerType.GetField("m_CurrentProcess",flags).SetValue(owner,null);

        var commandType=native.GetType("Kingmaker.UnitLogic.Commands.Base.UnitCommand",true);
        var attack=System.Runtime.Serialization.FormatterServices.GetUninitializedObject(native.GetType("Kingmaker.UnitLogic.Commands.UnitAttack",true));
        var reaction=System.Runtime.Serialization.FormatterServices.GetUninitializedObject(native.GetType("Kingmaker.UnitLogic.Commands.UnitAttackOfOpportunity",true));
        var effect=candidate.GetType("KingmakerMountedCombat.Integration.NativeSaveEffectBoundary",true);
        var settle=effect.GetMethod("CommandNeedsSettlement",BindingFlags.Static|BindingFlags.NonPublic);
        var mayStart=effect.GetMethod("MayStartDuringWait",BindingFlags.Static|BindingFlags.NonPublic);
        Check(!(bool)settle.Invoke(null,new[]{attack}) && !(bool)mayStart.Invoke(null,new[]{attack}),
            "unstarted native ordinary intent can snapshot without starting a new attack");
        Check((bool)settle.Invoke(null,new[]{reaction}) && (bool)mayStart.Invoke(null,new[]{reaction}),
            "already charged native reaction must start and settle before the snapshot");
        commandType.GetProperty("IsStarted").GetSetMethod(true).Invoke(attack,new object[]{true});
        Check((bool)settle.Invoke(null,new[]{attack}),"started native action remains outside the snapshot barrier");
        var mountedType=candidate.GetType("KingmakerMountedCombat.Integration.MountedPairAttackCommand",true);
        var mounted=System.Runtime.Serialization.FormatterServices.GetUninitializedObject(mountedType);
        var transactionType=candidate.GetType("KingmakerMountedCombat.Domain.MountedCombatTransaction",true);
        var transaction=Activator.CreateInstance(transactionType);
        mountedType.GetField("transaction",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(mounted,transaction);
        var actionType=candidate.GetType("KingmakerMountedCombat.Domain.MountedCombatActionKind",true);
        transactionType.GetMethod("Arm").Invoke(transaction,new[]{Enum.Parse(actionType,"RiderMelee")});
        transactionType.GetMethod("AcceptTarget").Invoke(transaction,new object[]{"owned-target",true});
        commandType.GetProperty("IsStarted").GetSetMethod(true).Invoke(mounted,new object[]{true});
        Check(!(bool)settle.Invoke(null,new[]{mounted}),
            "running mounted approach wrapper can snapshot like unmounted native approach");
        transactionType.GetMethod("Arrive").Invoke(transaction,new object[]{"owned-target"});
        Check(!(bool)settle.Invoke(null,new[]{mounted}),
            "arrival alone is not the native attack start or a delivered effect");
        transactionType.GetMethod("TryStartSingleAttack").Invoke(transaction,new object[]{"owned-target"});
        Check((bool)settle.Invoke(null,new[]{mounted}),
            "started mounted native attack still waits for complete effects before saving");
        commandType.GetProperty("IsFinished").GetSetMethod(true).Invoke(mounted,new object[]{true});
        Check(!(bool)settle.Invoke(null,new[]{mounted}),"completed mounted attack does not delay the next native snapshot");
        commandType.GetProperty("IsFinished").GetSetMethod(true).Invoke(reaction,new object[]{true});
        Check(!(bool)settle.Invoke(null,new[]{reaction}),"completed native reaction does not delay its already delivered effect");
        var service=candidate.GetType("KingmakerMountedCombat.Integration.MountedPersistenceService",true);
        var saveScope=service.GetNestedType("SaveScope",BindingFlags.NonPublic);
        var scope=Activator.CreateInstance(saveScope,true);
        var track=service.GetMethod("TrackNativeSave",BindingFlags.Static|BindingFlags.NonPublic);
        var empty=new object[0];
        var tracked=(System.Collections.Generic.IEnumerator<object>)track.Invoke(null,new object[]{
            ((System.Collections.Generic.IEnumerable<object>)empty).GetEnumerator(),scope});
        var noSnapshotRejected=false;
        try{tracked.MoveNext();}catch(InvalidOperationException){noSnapshotRejected=true;}finally{tracked.Dispose();}
        Check(noSnapshotRejected,"empty native save enumeration cannot report a successful write");
        saveScope.GetField("Json",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(scope,"bounded snapshot");
        tracked=(System.Collections.Generic.IEnumerator<object>)track.Invoke(null,new object[]{
            ((System.Collections.Generic.IEnumerable<object>)empty).GetEnumerator(),scope});
        Check(!tracked.MoveNext(),"normal native completion retains its actual captured snapshot boundary");tracked.Dispose();

    }

    private static void VerifyNativeEffectBoundary(Assembly native, Assembly candidate)
    {
        var controllerType=native.GetType("Kingmaker.Controllers.Projectiles.ProjectileController",true);
        var projectileType=native.GetType("Kingmaker.Controllers.Projectiles.Projectile",true);
        var boundary=candidate.GetType("KingmakerMountedCombat.Integration.NativeSaveEffectBoundary",true);
        var flags=BindingFlags.Static|BindingFlags.NonPublic;
        var read=boundary.GetMethod("HasUnresolvedProjectiles",flags,null,new[]{controllerType},null);
        var mark=boundary.GetMethod("HitCompleted",flags);var clear=boundary.GetMethod("Clear",flags);
        var snapshot=boundary.GetMethod("CaptureUnresolvedProjectiles",flags);
        var controller=Activator.CreateInstance(controllerType);
        var pending=(System.Collections.IList)controllerType.GetField("m_NewProjectiles",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(controller);
        var active=controllerType.GetField("m_Projectiles",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(controller);
        var updating=controllerType.GetField("m_Updating",BindingFlags.NonPublic|BindingFlags.Instance);
        var p=System.Runtime.Serialization.FormatterServices.GetUninitializedObject(projectileType);
        var q=System.Runtime.Serialization.FormatterServices.GetUninitializedObject(projectileType);
        clear.Invoke(null,null);
        Check(!(bool)read.Invoke(null,new[]{controller}),"native empty projectile controller is save-safe");
        pending.Add(p);
        Check((bool)read.Invoke(null,new[]{controller}),"pending native projectile cannot escape the save barrier");
        active.GetType().GetMethod("Add").Invoke(active,new[]{q});
        projectileType.GetProperty("IsHit").GetSetMethod(true).Invoke(q,new object[]{true});
        SetMember(p,"Cleared",true);
        Check((bool)read.Invoke(null,new[]{controller}) && !(bool)updating.GetValue(controller),
            "arrival is not delivery and save admission leaves the native updating flag unchanged");
        var enumerable=(System.Collections.IEnumerable)controllerType.GetProperty("Projectiles").GetValue(controller,null);
        var outer=enumerable.GetEnumerator();
        try
        {
            Check(outer.MoveNext() && (bool)updating.GetValue(controller),"native outer projectile iteration is active");
            Check((bool)read.Invoke(null,new[]{controller}) && (bool)updating.GetValue(controller),
                "nested save admission must preserve the native outer projectile iteration");
            var captured=(Array)snapshot.Invoke(null,new[]{controller});
            Check(captured.Length==1 && ReferenceEquals(captured.GetValue(0),q) && (bool)updating.GetValue(controller),
                "native pending effect observation retains exact identity without changing outer iteration");
        }
        finally { while(outer.MoveNext()) { } var disposable=outer as IDisposable; if(disposable!=null) disposable.Dispose(); }
        mark.Invoke(null,new[]{q});mark.Invoke(null,new[]{q});
        Check(!(bool)read.Invoke(null,new[]{controller}) && !(bool)updating.GetValue(controller),
            "completed delivery is idempotent and need not wait for visual particle expiry");
        clear.Invoke(null,null);
        Check((bool)read.Invoke(null,new[]{controller}),"old-world completion cannot classify a new restoration");
        pending.Clear();active.GetType().GetMethod("Clear").Invoke(active,null);clear.Invoke(null,null);
        Check(native.GetType("Kingmaker.Controllers.Combat.UnitCombatPrepareController",true).GetMethod("Tick").MetadataToken==0x0600936F &&
            projectileType.GetMethod("OnHit").MetadataToken==0x06009270,
            "exact native RT preparation and completed projectile delivery seams");
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
        var hp=native.GetType("Kingmaker.EntitySystem.Stats.CharacterStats",true).GetField("HitPoints");
        var hpBase=native.GetType("Kingmaker.EntitySystem.Stats.ModifiableValue",true)
            .GetField("m_BaseValue",BindingFlags.Instance|BindingFlags.NonPublic);
        Check(hp!=null && hpBase!=null && hpBase.FieldType==typeof(int) &&
            Attribute.IsDefined(hp,Type.GetType("Newtonsoft.Json.JsonPropertyAttribute, Newtonsoft.Json",true)) &&
            Attribute.IsDefined(hpBase,Type.GetType("Newtonsoft.Json.JsonPropertyAttribute, Newtonsoft.Json",true)),
            "native fixture HP and base value are native JSON members, not supplemental mod state");
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
            var deferred=candidate.GetType("KingmakerMountedCombat.Integration.NativeDeferredSave",true);
            var startOriginal=native.ManifestModule.ResolveMethod(0x06007FC5);
            var startLegacy=(System.Collections.IList)Activator.CreateInstance(listType);
            var startOps=new System.Collections.Generic.List<object>();
            var startOperands=new System.Collections.Generic.List<object>();
            foreach(var instruction in (System.Collections.IEnumerable)read.Invoke(null,new object[]{startOriginal,null}))
            {
                var t=instruction.GetType();var op=t.GetField("opcode").GetValue(instruction);
                var operand=t.GetField("operand").GetValue(instruction);
                startOps.Add(op);startOperands.Add(operand);
                startLegacy.Add(Activator.CreateInstance(legacyInstruction,new[]{op,operand}));
            }
            var startResult=(System.Collections.IEnumerable)deferred.GetMethod("TransformStart",
                BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{startLegacy});
            int startIndex=0,startChanges=0;
            var gotInserted=false;
            foreach(var instruction in startResult)
            {
                var op=legacyInstruction.GetField("opcode").GetValue(instruction);
                var operand=legacyInstruction.GetField("operand").GetValue(instruction);
                var expected=startOperands[startIndex] as MethodInfo;
                if(expected!=null && expected.MetadataToken==0x06007FC9 && expected.Module==native.ManifestModule)
                {
                    if(!gotInserted)
                    {
                        if(!op.Equals(System.Reflection.Emit.OpCodes.Ldarg_1) || operand!=null)
                            throw new InvalidOperationException("Native queued operation argument was not preserved.");
                        gotInserted=true;continue;
                    }
                    var replacement=operand as MethodInfo;
                    if(replacement==null || replacement.DeclaringType!=deferred || replacement.Name!="StartScreen" ||
                        !op.Equals(System.Reflection.Emit.OpCodes.Call))
                        throw new InvalidOperationException("Unexpected native loading activation rewrite.");
                    startChanges++;
                }
                else if(!Equals(op,startOps[startIndex]) || !Equals(operand,startOperands[startIndex]))
                    throw new InvalidOperationException("Native loading queue/owner/timer instruction changed.");
                startIndex++;
            }
            Check(startChanges==1 && startIndex==startOps.Count && gotInserted,
                "real native activation rewrite changes only screen admission; queue owner, callbacks and timers retained");

            var tickOriginal=native.ManifestModule.ResolveMethod(0x06007FC2);
            var tickLegacy=(System.Collections.IList)Activator.CreateInstance(listType);
            var tickOps=new System.Collections.Generic.List<object>();
            var tickOperands=new System.Collections.Generic.List<object>();
            foreach(var instruction in (System.Collections.IEnumerable)read.Invoke(null,new object[]{tickOriginal,null})) {
                var t=instruction.GetType();var op=t.GetField("opcode").GetValue(instruction);
                var operand=t.GetField("operand").GetValue(instruction);
                tickOps.Add(op);tickOperands.Add(operand);
                tickLegacy.Add(Activator.CreateInstance(legacyInstruction,new[]{op,operand}));
            }
            var tickResult=(System.Collections.IEnumerable)deferred.GetMethod("TransformTick",
                BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{tickLegacy});
            var tickIndex=0;var tickChanges=0;var tickInserted=false;
            foreach(var instruction in tickResult) {
                var op=legacyInstruction.GetField("opcode").GetValue(instruction);
                var operand=legacyInstruction.GetField("operand").GetValue(instruction);
                var expected=tickOperands[tickIndex] as MethodInfo;
                if(expected!=null && expected.DeclaringType==typeof(System.Collections.IEnumerator) && expected.Name=="MoveNext") {
                    if(!tickInserted) {
                        if(!op.Equals(System.Reflection.Emit.OpCodes.Ldarg_0)||operand!=null) throw new Exception("Native owner argument differs");
                        tickInserted=true;continue;
                    }
                    var replacement=operand as MethodInfo;
                    if(replacement==null||replacement.DeclaringType!=deferred||replacement.Name!="MoveNext"||
                        !op.Equals(System.Reflection.Emit.OpCodes.Call)) throw new Exception("Unexpected native tick rewrite");
                    tickChanges++;
                } else if(!Equals(op,tickOps[tickIndex])||!Equals(operand,tickOperands[tickIndex])) throw new Exception("Native queue completion changed");
                tickIndex++;
            }
            Check(tickChanges==1 && tickIndex==tickOps.Count && tickInserted,
                "exact native tick rewrite preserves progress, queue, screen, timer and callback bookkeeping");

            VerifyNativeDeferredOwner(native,candidate);
            VerifyNativeAbilitySettlement(native,candidate);
            VerifyNativeTouchCommitment(native,candidate);
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
            var buffTick=native.ManifestModule.ResolveMethod(0x06002A02);
            var buffLegacy=(System.Collections.IList)Activator.CreateInstance(listType);
            var buffOps=new System.Collections.Generic.List<object>();
            var buffOperands=new System.Collections.Generic.List<object>();
            var currentSites=new System.Collections.Generic.List<int>();
            foreach(var instruction in (System.Collections.IEnumerable)read.Invoke(null,new object[]{buffTick,null}))
            {
                var type=instruction.GetType();
                var op=type.GetField("opcode").GetValue(instruction);
                var operand=type.GetField("operand").GetValue(instruction);
                var buffMember=operand as MethodInfo;
                if(buffMember!=null && buffMember.Module==native.ManifestModule && buffMember.MetadataToken==0x06000BFA)
                    currentSites.Add(buffOps.Count);
                buffOps.Add(op); buffOperands.Add(operand);
                buffLegacy.Add(Activator.CreateInstance(legacyInstruction,new[]{op,operand}));
            }
            Check(currentSites.Count==2,"exact native buff owner and caster comparisons");
            var changedBuff=new System.Collections.Generic.List<object>();
            foreach(var instruction in (System.Collections.IEnumerable)persistencePatches.GetMethod("PairedBuffTimerTranspiler",
                BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{buffLegacy})) changedBuff.Add(instruction);
            int cursor=0;
            for(int i=0;i<buffOps.Count;i++)
            {
                var op=legacyInstruction.GetField("opcode").GetValue(changedBuff[cursor]);
                var operand=legacyInstruction.GetField("operand").GetValue(changedBuff[cursor]);
                if(i==currentSites[0]+4 || i==currentSites[1]+5)
                {
                    var hook=operand as MethodInfo;
                    var branch=changedBuff[++cursor];
                    if(!op.Equals(System.Reflection.Emit.OpCodes.Call) || hook==null || hook.Name!="PairedBuffTimerActor" ||
                        !legacyInstruction.GetField("opcode").GetValue(branch).Equals(System.Reflection.Emit.OpCodes.Brtrue) ||
                        !Equals(legacyInstruction.GetField("operand").GetValue(branch),buffOperands[i]))
                        throw new InvalidOperationException("Native buff eligibility replacement changed its continuation.");
                }
                else if(!Equals(op,buffOps[i]) || !Equals(operand,buffOperands[i]))
                    throw new InvalidOperationException("Native timer or effect body changed.");
                cursor++;
            }
            Check(cursor==changedBuff.Count && cursor==buffOps.Count+2,
                "both buff eligibility comparisons extended; native timers, loops, death cleanup and delivery unchanged");
            var buffType=native.GetType("Kingmaker.UnitLogic.Buffs.Buff",true);
            foreach(var field in new[]{"RoundNumber","NextTickTime","TickTime"})
            {
                var property=buffType.GetProperty(field,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
                Check(property!=null && Attribute.IsDefined(property,
                    Type.GetType("Newtonsoft.Json.JsonPropertyAttribute, Newtonsoft.Json",true)),
                    "native serialized buff timer member "+field);
            }
            var commit=native.ManifestModule.ResolveMethod(0x0600802A);
            var commitLegacy=(System.Collections.IList)Activator.CreateInstance(listType);
            var commitOps=new System.Collections.Generic.List<object>();
            var commitOperands=new System.Collections.Generic.List<object>();
            foreach(var instruction in (System.Collections.IEnumerable)read.Invoke(null,new object[]{commit,null}))
            {
                var type=instruction.GetType();var op=type.GetField("opcode").GetValue(instruction);
                var operand=type.GetField("operand").GetValue(instruction);
                commitOps.Add(op);commitOperands.Add(operand);
                commitLegacy.Add(Activator.CreateInstance(legacyInstruction,new[]{op,operand}));
            }
            var atomic=candidate.GetType("KingmakerMountedCombat.Integration.NativeMountedArchiveCommit",true);
            var changedCommit=new System.Collections.Generic.List<object>();
            foreach(var instruction in (System.Collections.IEnumerable)atomic.GetMethod("Transform",
                BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{commitLegacy})) changedCommit.Add(instruction);
            int commitCursor=0, replacementCalls=0;
            for(int i=0;i<commitOps.Count;i++)
            {
                var nativeCall=commitOperands[i] as MethodInfo;
                var token=nativeCall==null || nativeCall.Module!=native.ManifestModule ? 0 : nativeCall.MetadataToken;
                var instruction=changedCommit[commitCursor];
                var op=legacyInstruction.GetField("opcode").GetValue(instruction);
                var operand=legacyInstruction.GetField("operand").GetValue(instruction);
                if(token==0x06007FB2 || token==0x0600806D)
                {
                    if(!op.Equals(token==0x06007FB2 ? System.Reflection.Emit.OpCodes.Ldarg_1 : System.Reflection.Emit.OpCodes.Ldarg_3))
                        throw new InvalidOperationException("Native commit argument identity changed.");
                    instruction=changedCommit[++commitCursor];
                    var hook=legacyInstruction.GetField("operand").GetValue(instruction) as MethodInfo;
                    if(!legacyInstruction.GetField("opcode").GetValue(instruction).Equals(System.Reflection.Emit.OpCodes.Call) ||
                        hook==null || hook.DeclaringType!=atomic || hook.Name!=(token==0x06007FB2 ? "PreservePrevious" : "Replace"))
                        throw new InvalidOperationException("Native commit replacement hook differs.");
                    replacementCalls++;
                }
                else if(!Equals(op,commitOps[i]) || !Equals(operand,commitOperands[i]))
                    throw new InvalidOperationException("Native serialization, callbacks or failure handling changed.");
                commitCursor++;
            }
            Check(replacementCalls==2 && commitCursor==changedCommit.Count && commitCursor==commitOps.Count+2,
                "native worker only replaces original-delete/ZIP-rename; serialization, folder path and failure handling unchanged");
            var settingsRefresh=native.GetType("Kingmaker.UI.SettingsUI.SettingsRoot",true)
                .GetMethod("HandleSettingsUpdated",BindingFlags.Public|BindingFlags.Static);
            Check(settingsRefresh!=null && settingsRefresh.MetadataToken==0x0600346B &&
                settingsRefresh.GetParameters().Length==0,"exact native boolean cache refresh seam");
            isolation.GetMethod("SettingsRefreshPostfix",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
            Check(true,"unbound persistence cache callback leaves ordinary configuration untouched");
            var slider=native.GetType("Kingmaker.UI.SettingsUI.SettingsEntitySlider",true);
            var boolean=native.GetType("Kingmaker.UI.SettingsUI.SettingsEntityBool",true);
            patch.Invoke(null,new object[]{harmony,slider,"get_CurrentValue",0x060033EE,Type.EmptyTypes,"NativeSlotCountPrefix",null});
            patch.Invoke(null,new object[]{harmony,boolean,"get_CurrentValue",0x06003364,Type.EmptyTypes,"NativeAutosaveEnabledPrefix",null});
            Check(true,"exact native slot count and autosave getters accept scoped test prefixes");
            var floatArgs=new object[]{null,7f};var boolArgs=new object[]{null,false};
            Check((bool)isolation.GetMethod("NativeSlotCountPrefix",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,floatArgs) &&
                (float)floatArgs[1]==7f &&
                (bool)isolation.GetMethod("NativeAutosaveEnabledPrefix",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,boolArgs) &&
                !(bool)boolArgs[1],"unbound native category getters preserve all settings");
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
        var atomicType=candidate.GetType("KingmakerMountedCombat.Integration.NativeMountedArchiveCommit",true);
        var originalPath=Path.Combine(owned,"atomic-original.zks");
        var stagedPath=Path.Combine(owned,"atomic-staged.zks");
        var originalSaver=Activator.CreateInstance(saverType,new object[]{originalPath});
        var stagedSaver=Activator.CreateInstance(saverType,new object[]{stagedPath});
        foreach(var entry in new[]{new[]{"LoadGame","06000CE0"},new[]{"LoadGameFromMainMenu","06000CE2"},new[]{"LoadGameForSmokeTest","06000CE1"}})
        {
            var method=native.GetType("Kingmaker.Game",true).GetMethod(entry[0],BindingFlags.Instance|BindingFlags.Public,
                null,new[]{native.GetType("Kingmaker.EntitySystem.Persistence.SaveInfo",true)},null);
            Check(method!=null && method.MetadataToken==Convert.ToInt32(entry[1],16),
                "exact native "+entry[0]+" admission precedes the queued load iterator");
        }
        VerifyNativeColdDescriptor(native,candidate,owned);
        VerifyNativeEffectBoundary(native,candidate);
        var saveInfoType=native.GetType("Kingmaker.EntitySystem.Persistence.SaveInfo",true);
        var originalInfo=Activator.CreateInstance(saveInfoType);
        var stagedInfo=Activator.CreateInstance(saveInfoType);
        saveInfoType.GetProperty("Saver").SetValue(originalInfo,originalSaver,null);
        saveInfoType.GetProperty("Saver").SetValue(stagedInfo,stagedSaver,null);
        try
        {
            saverType.GetMethod("SaveJson").Invoke(originalSaver,new object[]{"header","{\"Name\":\"old native slot\"}"});
            saverType.GetMethod("SaveBytes").Invoke(originalSaver,new object[]{member,System.Text.Encoding.UTF8.GetBytes("old metadata")});
            saverType.GetMethod("Save").Invoke(originalSaver,null);
            saverType.GetMethod("SaveJson").Invoke(stagedSaver,new object[]{"header","{\"Name\":\"new native snapshot\"}"});
            saverType.GetMethod("SaveBytes").Invoke(stagedSaver,new object[]{member,System.Text.Encoding.UTF8.GetBytes(metadata)});
            saverType.GetMethod("Save").Invoke(stagedSaver,null);
            var oldHash=Hash(originalPath);var newHash=Hash(stagedPath);
            atomicType.GetMethod("PreservePrevious",flags).Invoke(null,new[]{originalSaver,stagedInfo});
            Check(Hash(originalPath)==oldHash,"native replacement preparation retains last-good complete bytes");
            bool failed=false;
            using(var held=new FileStream(originalPath,FileMode.Open,FileAccess.Read,FileShare.Read))
            {
                try { atomicType.GetMethod("Replace",flags).Invoke(null,new[]{stagedSaver,originalPath,originalInfo}); }
                catch(TargetInvocationException error){failed=error.InnerException is IOException;}
            }
            Check(failed && Hash(originalPath)==oldHash && Hash(stagedPath)==newHash,
                "failed actual native-saver replacement leaves both complete archives intact");
            atomicType.GetMethod("Replace",flags).Invoke(null,new[]{stagedSaver,originalPath,originalInfo});
            Check(!File.Exists(stagedPath) && Hash(originalPath)==newHash &&
                (string)saverType.GetProperty("FolderName").GetValue(stagedSaver,null)==originalPath,
                "actual native-saver replacement atomically moves exact complete archive and rebinds path");
            Check(System.Text.Encoding.UTF8.GetString((byte[])saverType.GetMethod("ReadBytes").Invoke(stagedSaver,new object[]{member}))==metadata,
                "atomic replacement preserves archive-scoped metadata without rewriting contents");
        }
        finally
        {
            ((IDisposable)originalSaver).Dispose();((IDisposable)stagedSaver).Dispose();
            foreach(var file in new[]{originalPath,stagedPath}) if(File.Exists(file))File.Delete(file);
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
            var transactionType=isolation.GetNestedType("WriteTransaction",BindingFlags.NonPublic);
            var descriptor=Activator.CreateInstance(native.GetType("Kingmaker.EntitySystem.Persistence.SaveInfo",true));
            var transaction=Activator.CreateInstance(transactionType,BindingFlags.NonPublic|BindingFlags.Instance,null,
                new object[]{descriptor,writePath,writeLease},null);
            pending.Add(writeSaver,transaction);
            try
            {
                saverType.GetMethod("SaveJson").Invoke(writeSaver,new object[]{"header","{}"});
                Check(!File.Exists(writePath),"authorized native metadata stage does not claim a disk write");
                saverType.GetMethod("Save").Invoke(writeSaver,null);
                Check(File.Exists(writePath) && pending.Contains(writeSaver),"real native archive commit retains ownership through the worker boundary");
                authorityType.GetMethod("AssertReadableArchive",BindingFlags.NonPublic|BindingFlags.Instance)
                    .Invoke(authority,new object[]{writePath});
                Check(true,"completed native archive becomes readable through the same strict authority");
                var clear=isolation.GetMethod("ClearPrefix",binding);
                Check(!(bool)clear.Invoke(null,new[]{writeSaver}) && !File.Exists(writePath) && pending.Contains(writeSaver),
                    "failed native worker may remove only its own completed staging archive");
                bool readDenied=false;
                try { clear.Invoke(null,new[]{readSaver}); } catch(TargetInvocationException e){readDenied=e.InnerException is InvalidOperationException;}
                Check(readDenied && Hash(readPath)==before,"worker cleanup cannot delete the read-only loaded archive");
                isolation.GetMethod("ObserveWorkerComplete",binding).Invoke(null,new[]{descriptor});
                isolation.GetMethod("ObserveWorkerComplete",binding).Invoke(null,new[]{descriptor});
                Check(!pending.Contains(writeSaver),"native worker completion releases its transaction exactly once");
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
