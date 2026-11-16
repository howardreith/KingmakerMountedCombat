[CmdletBinding()]
param([ValidateSet('Debug','Release')][string]$Configuration='Release',[string]$ProductDll)
# Known issues S02: premature post-load dismount (view activation timing).
#
# Two groups of contracts, read from IL without launching anything:
#   NATIVE   the installed Assembly-CSharp contracts the recorded ordering rests on: a "sleeping" unit has an inactive
#            view; world-map, kingdom and settlement modes put every unit to sleep; only the Default-family mode
#            controller wakes one; Game.Tick runs no game mode while a loading process is active; OnAreaLoaded neither
#            ticks a mode nor writes the sleeping flag. Together: a party arriving from the world map has inactive
#            views from OnAreaLoaded until the first Default tick after the loader is idle.
#   PRODUCT  the compiled KingmakerMountedCombat.dll: the arrival and in-area restoration read the exact views through
#            one read-only adapter BEFORE their decision and before any attachment, the compiled decisions defer rather
#            than attach, an explicit dismount releases the retained row, and the mounted invariant itself is unchanged.
# -ProductDll reads another build (for example the build of the commit before this repair, which must FAIL the
# PRODUCT group). Every check runs; the script exits 1 if any failed. This is not native gameplay evidence.
$ErrorActionPreference='Stop'
$kmcRepo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$kmcLab=[IO.Path]::GetFullPath((Join-Path $kmcRepo '../..'))
$kmcLayout=(Get-Content -Raw (Join-Path $kmcLab 'environment-intake.json')|ConvertFrom-Json).requestedLayout
$kmcManaged=Join-Path $kmcLayout.kingmakerInstallDir 'Kingmaker_Data/Managed'
$kmcDll=if($ProductDll){[IO.Path]::GetFullPath($ProductDll)}else{Join-Path $kmcRepo "bin/$Configuration/KingmakerMountedCombat.dll"}
if(-not(Test-Path -LiteralPath $kmcDll -PathType Leaf)){Write-Output "The product build is missing: $kmcDll";exit 1}
Add-Type -TypeDefinition @'
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
public static class KmcPostLoadViewReadinessProbe
{
    private static int passes, failures;
    private static MethodInfo read;
    private const BindingFlags All=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly;

    private static void Check(string label, Func<bool> body)
    {
        string detail=null; var ok=false;
        try { ok=body(); }
        catch(Exception error)
        {
            var inner=error is TargetInvocationException && error.InnerException!=null ? error.InnerException : error;
            detail=inner.GetType().Name+": "+inner.Message;
        }
        if(ok){ passes++; Console.WriteLine("PASS "+label); }
        else { failures++; Console.WriteLine("FAIL "+label+(detail!=null?" ["+detail+"]":"")); }
    }

    private sealed class Ins { public OpCode Op; public object Operand; public MethodBase Call; public FieldInfo Field; }

    // A stand-in IModLogger: counts every call and, when asked to, throws from every call.
    private sealed class ProbeLogger : System.Runtime.Remoting.Proxies.RealProxy
    {
        internal int Calls; internal bool Throw;
        internal ProbeLogger(Type type) : base(type) {}
        public override System.Runtime.Remoting.Messaging.IMessage Invoke(System.Runtime.Remoting.Messaging.IMessage message)
        {
            Calls++;
            var call=(System.Runtime.Remoting.Messaging.IMethodCallMessage)message;
            if(Throw) return new System.Runtime.Remoting.Messaging.ReturnMessage(new InvalidOperationException("logger failed"),call);
            return new System.Runtime.Remoting.Messaging.ReturnMessage(null,null,0,call.LogicalCallContext,call);
        }
    }

    private static object Row(Type pairType, string rider, string mount)
    {
        var row=Activator.CreateInstance(pairType,true);
        pairType.GetField("RiderId",All).SetValue(row,rider); pairType.GetField("MountId",All).SetValue(row,mount);
        return row;
    }

    private static object Slot(Type slotType, string actor)
    {
        var slot=Activator.CreateInstance(slotType,true);
        var field=slotType.GetField("ActorId",All);
        if(field!=null) field.SetValue(slot,actor); else slotType.GetProperty("ActorId",All).SetValue(slot,actor,null);
        return slot;
    }

    private static string SlotActor(object slot)
    {
        var field=slot.GetType().GetField("ActorId",All);
        return (string)(field!=null?field.GetValue(slot):slot.GetType().GetProperty("ActorId",All).GetValue(slot,null));
    }

    private static List<Ins> Body(MethodBase method)
    {
        if(method==null) throw new InvalidOperationException("the method does not exist in this build");
        var found=new List<Ins>();
        foreach(var raw in (IEnumerable)read.Invoke(null,new object[]{method,null}))
        {
            var ins=new Ins();
            ins.Op=(OpCode)raw.GetType().GetField("opcode").GetValue(raw);
            ins.Operand=raw.GetType().GetField("operand").GetValue(raw);
            ins.Call=ins.Operand as MethodBase; ins.Field=ins.Operand as FieldInfo;
            found.Add(ins);
        }
        return found;
    }

    private static bool Is(Ins ins, string type, string name)
    {
        return ins.Call!=null && ins.Call.Name==name && (type==null || ins.Call.DeclaringType.Name==type || ins.Call.DeclaringType.FullName==type);
    }

    private static List<int> Find(List<Ins> body, string type, string name)
    {
        var found=new List<int>();
        for(var i=0;i<body.Count;i++) if(Is(body[i],type,name)) found.Add(i);
        return found;
    }

    private static int One(List<Ins> body, string type, string name)
    {
        var found=Find(body,type,name);
        if(found.Count!=1) throw new InvalidOperationException(name+" is referenced "+found.Count+" times, expected once");
        return found[0];
    }

    private static MethodBase Method(Type type, string name, int parameters)
    {
        foreach(var member in type.GetMember(name,MemberTypes.Method|MemberTypes.Constructor,All))
        {
            var method=(MethodBase)member;
            if(parameters<0 || method.GetParameters().Length==parameters) return method;
        }
        return null;
    }

    // The game modes one controller is registered for, read from GameModesFactory.Initialize.
    private static string RegisteredModes(List<Ins> initialize, Type controller, Type modeType)
    {
        for(var i=0;i<initialize.Count;i++)
        {
            if(initialize[i].Op!=OpCodes.Newobj || initialize[i].Call==null || initialize[i].Call.DeclaringType!=controller) continue;
            for(var j=i+1;j<initialize.Count && j<i+12;j++)
            {
                if(Is(initialize[j],"GameModesFactory","Register")) break;
                if(initialize[j].Op!=OpCodes.Ldtoken || initialize[j].Field==null || !Is(initialize[j+1],"RuntimeHelpers","InitializeArray")) continue;
                var count=0;
                for(var k=j-1;k>i;k--) if(initialize[k].Op==OpCodes.Newarr){ count=Constant(initialize[k-1]); break; }
                var values=new int[count];
                System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(values,initialize[j].Field.FieldHandle);
                var names=new List<string>();
                foreach(var value in values) names.Add(Enum.GetName(modeType,value));
                names.Sort(StringComparer.Ordinal);
                return string.Join(",",names.ToArray());
            }
            throw new InvalidOperationException("the registration of "+controller.Name+" is not a constant mode array");
        }
        throw new InvalidOperationException(controller.Name+" is not registered");
    }

    private static int Constant(Ins ins)
    {
        if(ins.Op==OpCodes.Ldc_I4_0) return 0; if(ins.Op==OpCodes.Ldc_I4_1) return 1; if(ins.Op==OpCodes.Ldc_I4_2) return 2;
        if(ins.Op==OpCodes.Ldc_I4_3) return 3; if(ins.Op==OpCodes.Ldc_I4_4) return 4; if(ins.Op==OpCodes.Ldc_I4_5) return 5;
        if(ins.Op==OpCodes.Ldc_I4_6) return 6; if(ins.Op==OpCodes.Ldc_I4_7) return 7; if(ins.Op==OpCodes.Ldc_I4_8) return 8;
        if(ins.Op==OpCodes.Ldc_I4_S || ins.Op==OpCodes.Ldc_I4) return Convert.ToInt32(ins.Operand);
        throw new InvalidOperationException("the array length is not a constant");
    }

    public static int Run(string managed, string productPath)
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
        var product=Assembly.LoadFrom(productPath);
        Console.WriteLine("INFO product "+productPath+" MVID "+product.ManifestModule.ModuleVersionId);
        var modern=Assembly.LoadFrom(Path.Combine(managed,"UnityModManager/0Harmony.dll"));
        foreach(var method in modern.GetType("HarmonyLib.PatchProcessor",true).GetMethods(BindingFlags.Public|BindingFlags.Static))
            if(method.Name=="GetOriginalInstructions" && method.GetParameters().Length==2 && !method.GetParameters()[1].ParameterType.IsByRef) read=method;
        if(read==null) throw new InvalidOperationException("The installed Harmony instruction reader is unavailable.");

        // ---- NATIVE: the installed engine contracts behind the recorded ordering ----
        Check("NATIVE exact Kingmaker Assembly-CSharp MVID",
            ()=>native.ManifestModule.ModuleVersionId==new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"));
        var unit=native.GetType("Kingmaker.EntitySystem.Entities.UnitEntityData",true);
        var viewBase=native.GetType("Kingmaker.View.EntityViewBase",true);
        var allSleeping=native.GetType("Kingmaker.Controllers.AllUnitsSleepingController",true);
        var sleeping=native.GetType("Kingmaker.Controllers.SleepingUnitsController",true);
        var factory=native.GetType("Kingmaker.GameModes.GameModesFactory",true);
        var modeType=native.GetType("Kingmaker.GameModes.GameModeType",true);
        var game=native.GetType("Kingmaker.Game",true);
        Check("NATIVE UnitEntityData.IsViewActive (060082F4) is false for a unit that is out of the game or sleeping",
            ()=>{
                var getter=unit.GetProperty("IsViewActive",All).GetGetMethod(true);
                var body=Body(getter);
                return getter.MetadataToken==0x060082F4 && One(body,"EntityDataBase","get_IsInGame")<One(body,"UnitEntityData","get_IsSleeping") &&
                    One(body,"UnitEntityData","get_IsSleeping")<One(body,"UnitEntityData","get_IsDeathRevealed");
            });
        Check("NATIVE EntityViewBase.UpdateViewActive (0600176F) sets the view object active exactly as IsViewActive says",
            ()=>{
                var update=Method(viewBase,"UpdateViewActive",0);
                var body=Body(update);
                return update.MetadataToken==0x0600176F && One(body,"EntityDataBase","get_IsViewActive")<One(body,"GameObject","get_activeSelf") &&
                    One(body,"GameObject","get_activeSelf")<One(body,"GameObject","SetActive");
            });
        Check("NATIVE the IsSleeping setter updates the view's activity",
            ()=>Find(Body(unit.GetProperty("IsSleeping",All).GetSetMethod(true)),"EntityViewBase","UpdateViewActive").Count==1);
        Check("NATIVE AllUnitsSleepingController.Tick (06008FE3) puts every unit of the loaded state to sleep",
            ()=>{
                var tick=Method(allSleeping,"Tick",0);
                var body=Body(tick);
                var set=One(body,"UnitEntityData","set_IsSleeping");
                return tick.MetadataToken==0x06008FE3 && body[set-1].Op==OpCodes.Ldc_I4_1 && One(body,null,"get_All")<set;
            });
        Check("NATIVE that controller runs in exactly the GlobalMap, Kingdom and KingdomSettlement game modes",
            ()=>{
                var modes=RegisteredModes(Body(Method(factory,"Initialize",0)),allSleeping,modeType);
                if(modes!="GlobalMap,Kingdom,KingdomSettlement") throw new InvalidOperationException("registered for "+modes);
                return true;
            });
        Check("NATIVE the controller that wakes a unit (SleepingUnitsController) runs in exactly Cutscene, Default, Dialog, Pause and Rest",
            ()=>{
                var modes=RegisteredModes(Body(Method(factory,"Initialize",0)),sleeping,modeType);
                if(modes!="Cutscene,Default,Dialog,Pause,Rest") throw new InvalidOperationException("registered for "+modes);
                var body=Body(Method(sleeping,"Tick",0));
                return One(body,"SleepingUnitsController","ShouldBeSleeping")<One(body,"UnitEntityData","set_IsSleeping");
            });
        Check("NATIVE that controller wakes nothing when its mode starts: Activate is empty, only Tick writes the flag",
            ()=>{
                var activate=Body(Method(sleeping,"Activate",0));
                return activate.Count==1 && activate[0].Op==OpCodes.Ret;
            });
        Check("NATIVE Game.Tick (06000CBA) returns before any game-mode tick while a loading process is active",
            ()=>{
                var tick=Method(game,"Tick",0);
                var body=Body(tick);
                var loading=Find(body,"LoadingProcess","get_IsLoadingInProcess")[0];
                var firstReturn=-1;
                for(var i=0;i<body.Count;i++) if(body[i].Op==OpCodes.Ret){ firstReturn=i; break; }
                var modeTick=One(body,"GameMode","Tick");
                return tick.MetadataToken==0x06000CBA && loading==1 && (body[2].Op==OpCodes.Brfalse_S || body[2].Op==OpCodes.Brfalse) &&
                    loading<firstReturn && firstReturn<modeTick;
            });
        Check("NATIVE Game.OnAreaLoaded (06000CD7) starts the arrival mode but ticks no mode and never writes the sleeping flag",
            ()=>{
                var loaded=native.ManifestModule.ResolveMethod(0x06000CD7);
                var body=Body(loaded);
                return loaded.Name=="OnAreaLoaded" && Find(body,"Game","StartMode").Count==1 && Find(body,"GameMode","Tick").Count==0 &&
                    Find(body,"UnitEntityData","set_IsSleeping").Count==0 && Find(body,null,"UpdateViewActive").Count==0;
            });

        // ---- PRODUCT: the compiled mod ----
        var runtime=product.GetType("KingmakerMountedCombat.Integration.KingmakerMountedPairRuntime",true);
        var persistence=product.GetType("KingmakerMountedCombat.Integration.MountedPersistenceService",true);
        var relationship=product.GetType("KingmakerMountedCombat.Integration.GameMountedRelationshipService",true);
        var intent=product.GetType("KingmakerMountedCombat.Domain.MountedRetainedIntentPolicy",true);
        const string Adapter="RestoreViewReadinessRefusal";
        Check("PRODUCT the restore view adapter only reads: the exact views' activity and the native party selector, as the invariant does, into the readiness policy",
            ()=>{
                var body=Body(Method(runtime,Adapter,3));
                var allowed=new HashSet<string>(StringComparer.Ordinal){"get_View","op_Inequality","op_Equality","op_Implicit","get_gameObject",
                    "get_activeSelf","get_activeInHierarchy","Concat","ToString","Refusal","get_Instance","get_UI","get_Canvas","get_GroupManager"};
                foreach(var ins in body)
                {
                    if(ins.Call!=null && !allowed.Contains(ins.Call.Name)) throw new InvalidOperationException("unexpected call "+ins.Call.DeclaringType.Name+"."+ins.Call.Name);
                    if(ins.Op==OpCodes.Stfld || ins.Op==OpCodes.Stsfld) throw new InvalidOperationException("the adapter writes a field");
                }
                // The selector is a member of the native UI canvas (a field or a property, whichever the build has).
                var selector=0;
                foreach(var ins in body) if((ins.Field!=null && ins.Field.Name=="GroupManager") || (ins.Call!=null && ins.Call.Name=="get_GroupManager")) selector++;
                // Two unit views and the selector object, each read for activeSelf and activeInHierarchy.
                return Find(body,"GameObject","get_activeSelf").Count==3 && Find(body,"GameObject","get_activeInHierarchy").Count==3 &&
                    Find(body,"GameObject","SetActive").Count==0 && selector==1 &&
                    One(body,"MountedRestoreViewReadinessPolicy","Refusal")>Find(body,"GameObject","get_activeInHierarchy")[2];
            });        Check("PRODUCT the arrival restoration reads the views before each arrival decision, and decides before any ownership import or attachment",
            ()=>{
                var body=Body(Method(persistence,"RestoreAreaPair",0));
                var reads=Find(body,"KingmakerMountedPairRuntime",Adapter);
                if(reads.Count!=2) throw new InvalidOperationException("the views are read "+reads.Count+" times, expected once per arrival decision");
                var carried=One(body,"MountedRetainedIntentPolicy","DecideCarriedArrival");
                var departing=One(body,"MountedRetainedIntentPolicy","DecideDepartingArrival");
                if(!(reads[0]<carried && carried<reads[1] && reads[1]<departing)) throw new InvalidOperationException("the views are not read before their decisions");
                var attachments=0;
                foreach(var name in new[]{"RestoreRetainedPair","RestoreSavedPair","ImportSavedOwnership","RestoreParticipation"})
                    foreach(var index in Find(body,null,name))
                    {
                        attachments++;
                        if(index<departing) throw new InvalidOperationException(name+" precedes the arrival decisions");
                    }
                // Each deferral records the arrival placement (two call sites: carried and departing).
                return attachments>=4 && Find(body,"MountedPersistenceService","MarkArrivalDeferred").Count==2;
            });
        Check("PRODUCT the in-area retry waits for settled play, reads the views at every evaluation, then decides, then attaches",
            ()=>{
                var body=Body(Method(persistence,"RestoreRetainedIntentCore",0));
                var decide=One(body,"MountedRetainedIntentPolicy","Decide");
                var settled=One(body,"MountedRetainedIntentPolicy","Settled");
                var views=One(body,"KingmakerMountedPairRuntime",Adapter);
                var patience=One(body,"MountedRetainedIntentPolicy","PatienceExpired");
                var pair=One(body,"MountedRetainedIntentPolicy","DecidePair");
                var attach=One(body,"MountedPersistenceService","RestoreRetainedPair");
                var placement=One(body,"MountedRetainedIntentPolicy","ArrivalPlacementHeld");
                return decide<settled && settled<views && views<pair && patience<pair && placement<pair && pair<attach;
            });
        Check("PRODUCT an explicit dismount is delivered to the retained custody: the world constructor subscribes the pair-dismount notification",
            ()=>{
                MethodBase constructor=null;
                foreach(var candidate in persistence.GetConstructors(All))
                    if(candidate.GetParameters().Length==3 && candidate.GetParameters()[0].ParameterType.Name=="MountedWorldService") constructor=candidate;
                var body=Body(constructor);
                return One(body,"MountedPersistenceService","ObservePairDismounting")<One(body,"MountedWorldService","add_PairDismounting");
            });
        Check("PRODUCT that notification consults the explicit-dismount policy before it releases anything",
            ()=>{
                var body=Body(Method(persistence,"ObservePairDismounting",2));
                return One(body,"MountedRetainedIntentPolicy","ReleasedByDismount")<One(body,"MountedPersistenceService","ReleaseRetainedPair");
            });
        // The actual compiled release and notification, driven on a detached service with real custody rows.
        var transferType=persistence.GetNestedType("AreaTransfer",BindingFlags.NonPublic);
        var pairType=persistence.GetNestedType("AreaPair",BindingFlags.NonPublic);
        var loggerType=product.GetType("KingmakerMountedCombat.Logging.IModLogger",true);
        var triggerType=product.GetType("KingmakerMountedCombat.Domain.CleanupTrigger",true);
        var handler=Method(persistence,"ObservePairDismounting",2);
        var release=Method(persistence,"ReleaseRetainedPair",3);
        Check("PRODUCT the compiled release drops exactly the rows and control rows that share an actor with the dismounted pair, and ends an emptied custody",
            ()=>{
                var slotType=transferType.GetField("Slots",All).FieldType.GetElementType();
                var service=System.Runtime.Serialization.FormatterServices.GetUninitializedObject(persistence);
                var log=new ProbeLogger(loggerType);
                persistence.GetField("logger",All).SetValue(service,log.GetTransparentProxy());
                var custody=Activator.CreateInstance(transferType,true);
                var rows=Array.CreateInstance(pairType,3);
                rows.SetValue(Row(pairType,"r1","m1"),0); rows.SetValue(Row(pairType,"r2","m2"),1); rows.SetValue(Row(pairType,"r3","m3"),2);
                var slots=Array.CreateInstance(slotType,3);
                slots.SetValue(Slot(slotType,"r1"),0); slots.SetValue(Slot(slotType,"m2"),1); slots.SetValue(Slot(slotType,"r3"),2);
                transferType.GetField("Pairs",All).SetValue(custody,rows); transferType.GetField("Slots",All).SetValue(custody,slots);
                var held=persistence.GetField("retainedIntent",All);
                held.SetValue(service,custody);
                Func<string> remaining=()=>{
                    var list=new List<string>();
                    foreach(var row in (Array)transferType.GetField("Pairs",All).GetValue(custody)) list.Add((string)pairType.GetField("RiderId",All).GetValue(row));
                    foreach(var slot in (Array)transferType.GetField("Slots",All).GetValue(custody)) list.Add("slot:"+SlotActor(slot));
                    return string.Join(",",list.ToArray());
                };
                // A pair that shares nothing, and an unknown pair, release nothing.
                release.Invoke(service,new object[]{"r9","m9","probe"}); release.Invoke(service,new object[]{null,null,"probe"});
                if(remaining()!="r1,r2,r3,slot:r1,slot:m2,slot:r3" || log.Calls!=0) throw new InvalidOperationException("an unrelated pair released rows: "+remaining());
                // The rider of one row.
                release.Invoke(service,new object[]{"r1","m1","probe"});
                if(remaining()!="r2,r3,slot:m2,slot:r3" || held.GetValue(service)!=custody) throw new InvalidOperationException("after r1/m1: "+remaining());
                // Only the mount is shared (the rider now rides something else): still that pair's row.
                release.Invoke(service,new object[]{"x","m2","probe"});
                if(remaining()!="r3,slot:r3" || held.GetValue(service)!=custody) throw new InvalidOperationException("after x/m2: "+remaining());
                // The notification itself: a non-explicit trigger releases nothing; nothing is released for an owner without a pair.
                handler.Invoke(service,new object[]{null,Enum.Parse(triggerType,"AreaSuspension")});
                handler.Invoke(service,new object[]{null,Enum.Parse(triggerType,"Manual")});
                if(remaining()!="r3,slot:r3") throw new InvalidOperationException("the notification released a row without a matching pair: "+remaining());
                release.Invoke(service,new object[]{"r3","m3","probe"});
                return held.GetValue(service)==null && (int)persistence.GetProperty("RetainedExplicitReleaseCount",All).GetValue(service,null)==3 &&
                    (int)persistence.GetProperty("RetainedDropCount",All).GetValue(service,null)==3 && log.Calls==3;
            });
        Check("PRODUCT a failure inside that release, with a logger that also throws, never escapes the dismount notification",
            ()=>{
                var service=System.Runtime.Serialization.FormatterServices.GetUninitializedObject(persistence);
                var log=new ProbeLogger(loggerType); log.Throw=true;
                persistence.GetField("logger",All).SetValue(service,log.GetTransparentProxy());
                // Custody whose rows cannot be read: the release throws inside the notification.
                var broken=Activator.CreateInstance(transferType,true);
                transferType.GetField("Pairs",All).SetValue(broken,null);
                persistence.GetField("retainedIntent",All).SetValue(service,broken);
                handler.Invoke(service,new object[]{null,Enum.Parse(triggerType,"Manual")});
                if(log.Calls!=1) throw new InvalidOperationException("the failure did not reach the throwing logger ("+log.Calls+" calls)");
                // And a release whose own log line throws is still a completed release.
                var custody=Activator.CreateInstance(transferType,true);
                var rows=Array.CreateInstance(pairType,1); rows.SetValue(Row(pairType,"r1","m1"),0);
                transferType.GetField("Pairs",All).SetValue(custody,rows);
                transferType.GetField("Slots",All).SetValue(custody,Array.CreateInstance(transferType.GetField("Slots",All).FieldType.GetElementType(),0));
                persistence.GetField("retainedIntent",All).SetValue(service,custody);
                try { release.Invoke(service,new object[]{"r1","m1","probe"}); } catch(TargetInvocationException) { }
                return persistence.GetField("retainedIntent",All).GetValue(service)==null;
            });        Check("PRODUCT the mounted invariant is unchanged: it still judges view activity through MountedViewActivityPolicy and has no readiness allowance",
            ()=>{
                var invariant=Body(Method(runtime,"ValidateMountedInvariants",1));
                var validate=Body(Method(relationship,"ValidateActivePair",0));
                foreach(var body in new[]{invariant,validate})
                    foreach(var ins in body)
                        if(ins.Call!=null && (ins.Call.Name==Adapter || ins.Call.DeclaringType.Name=="MountedRestoreViewReadinessPolicy" ||
                            ins.Call.DeclaringType.Name=="MountedRetainedIntentPolicy"))
                            throw new InvalidOperationException("the invariant consults the restoration readiness");
                return One(invariant,"MountedViewActivityPolicy","IsAdmissible")>Find(invariant,"GameObject","get_activeInHierarchy")[1] &&
                    Find(invariant,"GameObject","get_activeSelf").Count>=2 &&
                    One(validate,"KingmakerMountedPairRuntime","ValidateMountedInvariants")<Find(validate,"GameMountedRelationshipService","Dismount")[0];
            });
        Check("PRODUCT compiled decisions: inactive views are not ready, and an eligible pair with such views is retained or deferred, never restored",
            ()=>{
                var readiness=product.GetType("KingmakerMountedCombat.Domain.MountedRestoreViewReadinessPolicy",true);
                var notReady=(string)readiness.GetField("NotReady").GetValue(null);
                var refusal=readiness.GetMethod("Refusal");
                if(refusal.Invoke(null,new object[]{true,true,true,true,false})!=null) throw new InvalidOperationException("active views are refused");
                foreach(var observation in new[]{new object[]{false,false,false,false,false},new object[]{false,true,true,true,false},new object[]{true,false,true,true,false},
                    new object[]{true,true,false,true,false},new object[]{true,true,true,false,false}})
                    if((string)refusal.Invoke(null,observation)!=notReady) throw new InvalidOperationException("an inactive view is ready");
                // The invariant's own Default-mode allowance: under the native party selector the invariant keeps the pair.
                if(refusal.Invoke(null,new object[]{false,false,false,false,true})!=null) throw new InvalidOperationException("readiness differs from the invariant under the party selector");
                var carried=intent.GetMethod("DecideCarriedArrival");
                var departing=intent.GetMethod("DecideDepartingArrival");
                var pair=intent.GetMethod("DecidePair");
                var regroup=Enum.Parse(pair.GetParameters()[10].ParameterType,"Unavailable");
                Func<bool,string,float,bool,string> decidePair=(expired,views,separation,arrival)=>
                    pair.Invoke(null,new object[]{true,true,false,false,null,separation,6f,true,expired,false,regroup,false,views,arrival}).ToString();
                var placement=intent.GetMethod("ArrivalPlacementHeld");
                return carried.Invoke(null,new object[]{false,true,true,false,null,false,notReady}).ToString()=="Retain" &&
                    carried.Invoke(null,new object[]{false,true,true,false,null,false,null}).ToString()=="Restore" &&
                    departing.Invoke(null,new object[]{null,false,notReady}).ToString()=="Defer" &&
                    departing.Invoke(null,new object[]{null,false,null}).ToString()=="Restore" &&
                    departing.Invoke(null,new object[]{"refused",false,notReady}).ToString()=="Fail" &&
                    decidePair(false,notReady,1f,false)=="Retry" && decidePair(true,notReady,1f,false)=="Drop" && decidePair(false,null,1f,false)=="Restore" &&
                    // The untouched arrival placement keeps its own geometry; a relocated pair does not.
                    decidePair(false,null,9f,true)=="Restore" && decidePair(false,null,9f,false)=="Drop" && decidePair(false,notReady,9f,true)=="Retry" &&
                    (bool)placement.Invoke(null,new object[]{true,0.1f,0f}) && !(bool)placement.Invoke(null,new object[]{true,0.2f,0f}) &&
                    !(bool)placement.Invoke(null,new object[]{false,0f,0f});
            });        Check("PRODUCT compiled bounds: five settled frames and 0.3 s before a restoration, ten seconds of patience, and only a Manual dismount releases",
            ()=>{
                var trigger=product.GetType("KingmakerMountedCombat.Domain.CleanupTrigger",true);
                var released=intent.GetMethod("ReleasedByDismount");
                foreach(var value in Enum.GetValues(trigger))
                    if((bool)released.Invoke(null,new object[]{value})!=(value.ToString()=="Manual")) throw new InvalidOperationException(value+" is misjudged");
                var settled=intent.GetMethod("Settled"); var patience=intent.GetMethod("PatienceExpired");
                return (bool)settled.Invoke(null,new object[]{5,0.3f}) && !(bool)settled.Invoke(null,new object[]{4,9f}) && !(bool)settled.Invoke(null,new object[]{9,0.29f}) &&
                    (bool)patience.Invoke(null,new object[]{0f,10.01f}) && !(bool)patience.Invoke(null,new object[]{0f,10f}) && !(bool)patience.Invoke(null,new object[]{-1f,999f});
            });
        Console.WriteLine("POST-LOAD VIEW READINESS CONTRACT PASS="+passes+" FAIL="+failures+"; installed-assembly and compiled-product contracts, not native gameplay evidence");
        return failures==0?0:1;
    }
}
'@
try { $kmcCode=[KmcPostLoadViewReadinessProbe]::Run($kmcManaged,$kmcDll) }
catch { Write-Output $_.Exception.ToString(); exit 1 }
exit $kmcCode
