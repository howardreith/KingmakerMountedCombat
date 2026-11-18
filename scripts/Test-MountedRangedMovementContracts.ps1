[CmdletBinding()]
param([ValidateSet('Debug','Release')][string]$Configuration='Release',[string]$ProductDll)
# Known issues Sprint 5 (R01, R02): the mounted ranged-movement rule's product seams.
#   PRODUCT-IL        the compiled product is READ: the links that keep a mounted pair's mount from moving on its own
#                     initiative slot while it carries its rider (the dismount-only split, the paired candidate
#                     suppression, the production turn authority), the attack-time seam and its read-only travel reader.
#   PRODUCT-EXECUTED  the compiled policy constants, allocation currency, paired grant, adoption decision and production
#                     settings defaults are RUN.
# The selector transpiler's patch construction is pinned by Test-PairedActivationContracts.ps1 and the native attack-roll
# facts by Test-AssemblyContracts.ps1. Not native gameplay evidence.
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
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

public static class KmcRangedMovementProbe
{
    private static int passes, failures;
    private static MethodInfo read;
    private const BindingFlags All=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly;

    private static void Check(string label, Func<bool> body)
    {
        string detail=null; var ok=false;
        try { ok=body(); }
        catch(Exception error) { var inner=error; while(inner is TargetInvocationException && inner.InnerException!=null) inner=inner.InnerException; detail=inner.GetType().Name+": "+inner.Message; }
        if(ok){ passes++; Console.WriteLine("PASS "+label); }
        else { failures++; Console.WriteLine("FAIL "+label+(detail!=null?" ["+detail+"]":"")); }
    }
    private static bool Every(string tag, params bool[] parts)
    {
        var failed=new List<string>(); for(var i=0;i<parts.Length;i++) if(!parts[i]) failed.Add((i+1).ToString());
        if(failed.Count>0) Console.WriteLine("INFO "+tag+" failing parts: "+string.Join(",",failed.ToArray()));
        return failed.Count==0;
    }
    private sealed class Ins { public OpCode Op; public object Operand; public MethodBase Call; }
    private static List<Ins> Body(MethodBase method)
    {
        if(method==null) throw new InvalidOperationException("the method does not exist in this build");
        var found=new List<Ins>();
        foreach(var raw in (IEnumerable)read.Invoke(null,new object[]{method,null}))
        {
            var ins=new Ins(); ins.Op=(OpCode)raw.GetType().GetField("opcode").GetValue(raw); ins.Operand=raw.GetType().GetField("operand").GetValue(raw);
            ins.Call=ins.Operand as MethodBase; found.Add(ins);
        }
        return found;
    }
    private static List<int> Find(List<Ins> body, string type, string name)
    {
        var found=new List<int>();
        for(var i=0;i<body.Count;i++) if(body[i].Call!=null && body[i].Call.Name==name && (type==null || body[i].Call.DeclaringType.Name==type)) found.Add(i);
        return found;
    }
    private static MethodBase M(Type type, string name)
    {
        var found=type.GetMember(name,MemberTypes.Method,All).Cast<MethodBase>().ToArray();
        if(found.Length!=1) throw new InvalidOperationException(type.Name+"."+name+": "+found.Length+" methods");
        return found[0];
    }
    private static Type[] TypesOf(Assembly assembly) { try { return assembly.GetTypes(); } catch(ReflectionTypeLoadException partial) { return partial.Types.Where(t=>t!=null).ToArray(); } }

    // Every product method (outside the Diagnostics namespace unless asked) whose body calls a method matching the test.
    private static List<string> Callers(Assembly product, Func<MethodBase,bool> target, bool diagnostics)
    {
        var found=new List<string>();
        foreach(var type in TypesOf(product))
        {
            if(!diagnostics && (type.FullName??"").StartsWith("KingmakerMountedCombat.Diagnostics.",StringComparison.Ordinal)) continue;
            foreach(var member in type.GetMembers(All))
            {
                var method=member as MethodBase; if(method==null || method.GetMethodBody()==null) continue;
                List<Ins> body; try { body=Body(method); } catch { continue; }
                if(body.Any(i=>i.Call!=null && target(i.Call))) found.Add(type.Name+"."+method.Name);
            }
        }
        found.Sort(StringComparer.Ordinal);
        return found;
    }
    private static bool Named(MethodBase call, string type, string name)
    {
        if(call.Name!=name) return false;
        var declaring=call.DeclaringType; if(declaring.IsGenericType) declaring=declaring.GetGenericTypeDefinition();
        return declaring.Name==type;
    }

    public static int Run(string managed, string productPath)
    {
        AppDomain.CurrentDomain.AssemblyResolve+=(s,a)=>{
            var leaf=new AssemblyName(a.Name).Name+".dll";
            foreach(var folder in new[]{managed,Path.Combine(managed,"UnityModManager")}){ var p=Path.Combine(folder,leaf); if(File.Exists(p)) return Assembly.LoadFrom(p); }
            return null;
        };
        Assembly.LoadFrom(Path.Combine(managed,"Assembly-CSharp.dll"));
        var product=Assembly.LoadFrom(productPath);
        Console.WriteLine("INFO product "+productPath+" MVID "+product.ManifestModule.ModuleVersionId);
        foreach(var method in Assembly.LoadFrom(Path.Combine(managed,"UnityModManager/0Harmony.dll")).GetType("HarmonyLib.PatchProcessor",true).GetMethods(BindingFlags.Public|BindingFlags.Static))
            if(method.Name=="GetOriginalInstructions" && method.GetParameters().Length==2 && !method.GetParameters()[1].ParameterType.IsByRef) read=method;

        var participant=product.GetType("KingmakerMountedCombat.Integration.MountedPairTurnParticipant",true);
        var relationship=product.GetType("KingmakerMountedCombat.Integration.GameMountedRelationshipService",true);
        var policy=product.GetType("KingmakerMountedCombat.Domain.MountedRangedMovementPolicy",true);

        Check("PRODUCT-IL the only split of a live pair's activation is the dismount's: PairedActivation.Detach is called only by SplitPairedActivationAtRound, which only SplitPairedActivation calls, which only HandleDismounting calls; HandleDismounting is subscribed to the relationship's Dismounting event, which Dismount raises before the coordinator's own Dismount (which leaves the pair Unmounted or Faulted, never Mounted)",
            ()=>{
                var detach=Callers(product,c=>Named(c,"PairedActivation`2","Detach"),false);
                var atRound=Callers(product,c=>c.Name=="SplitPairedActivationAtRound",false);
                var split=Callers(product,c=>c.Name=="SplitPairedActivation",false);
                var dismounting=Callers(product,c=>c.Name=="HandleDismounting" && c.DeclaringType==participant,false);
                var subscribes=false;
                foreach(var ctor in participant.GetConstructors(All))
                {
                    var subscribe=Body(ctor);
                    for(var i=2;i<subscribe.Count;i++) if(subscribe[i].Call!=null && subscribe[i].Call.Name=="add_Dismounting" && subscribe[i-1].Op==OpCodes.Newobj && subscribe[i-2].Op==OpCodes.Ldftn && subscribe[i-2].Call.Name=="HandleDismounting") subscribes=true;
                }
                var dismount=Body(relationship.GetMethod("Dismount",All,null,new[]{product.GetType("KingmakerMountedCombat.Domain.CleanupTrigger",true)},null));
                var raised=dismount.FindIndex(i=>i.Operand is FieldInfo && ((FieldInfo)i.Operand).Name=="Dismounting");
                var coordinator=Find(dismount,"MountedRelationshipCoordinator","Dismount");
                Console.WriteLine("INFO Detach callers: "+string.Join(", ",detach.ToArray())+"; SplitPairedActivationAtRound callers: "+string.Join(", ",atRound.ToArray())+
                    "; SplitPairedActivation callers: "+string.Join(", ",split.ToArray())+"; HandleDismounting references (delegate creation only: subscribe and unsubscribe): "+string.Join(", ",dismounting.ToArray()));
                return Every("C1",detach.Count==1 && detach[0]=="MountedPairTurnParticipant.SplitPairedActivationAtRound",
                    atRound.Count==1 && atRound[0]=="MountedPairTurnParticipant.SplitPairedActivation",
                    split.Count==1 && split[0]=="MountedPairTurnParticipant.HandleDismounting",
                    dismounting.Count==2 && dismounting[0]=="MountedPairTurnParticipant..ctor" && dismounting[1]=="MountedPairTurnParticipant.Dispose" &&
                        Find(Body(M(participant,"Dispose")),null,"remove_Dismounting").Count==1, subscribes, raised>=0, coordinator.Count==1, raised<(coordinator.Count==1?coordinator[0]:-1));
            });
        Check("PRODUCT-IL a mounted pair's mount gets no initiative slot of its own: the selector transpiler's hook SkipPairedCandidate asks the participant's SuppressPairedCandidate, which reads the activation's Partner and Split; a mount's own native grant ('native:') is issued only when it holds no part of an unsplit activation, while the partner's preparation is stamped with the activation's identity",
            ()=>{
                var methods=product.GetType("KingmakerMountedCombat.Integration.MountedPatchController",true).GetNestedType("PatchMethods",All);
                var selector=Body(M(methods,"PairedSelectorTranspiler"));
                var skip=Body(M(methods,"SkipPairedCandidate"));
                var suppress=Body(M(participant,"SuppressPairedCandidate"));
                var preparing=Body(M(participant,"HandleTurnPreparing"));
                var begin=Body(M(participant,"BeginPairedPreparation"));
                var native=preparing.FindIndex(i=>i.Op==OpCodes.Ldstr && (string)i.Operand=="native:");
                return Every("C2",selector.Any(i=>i.Op==OpCodes.Ldstr && (string)i.Operand=="SkipPairedCandidate"), Find(skip,null,"SuppressPairedCandidate").Count==1,
                    suppress.Any(i=>i.Call!=null && i.Call.Name=="get_Partner"), suppress.Any(i=>i.Call!=null && i.Call.Name=="get_Split"),
                    native>0, Find(preparing,null,"get_Split").Count>=1, Find(preparing,null,"BeginPairedPreparation").Count==1,
                    Find(begin,null,"BeginGrantedPreparation").Count>=1 && Find(begin,null,"get_Identity").Count>=Find(begin,null,"BeginGrantedPreparation").Count);
            });
        Check("PRODUCT-IL the production turn authority is the qualified one: outside the Diagnostics namespace only the two TryConfigurePairedActivation methods (dismounted, outside combat) write EnablePairedActivation, and nothing outside Diagnostics calls them except their own forwarders; there is no player switch",
            ()=>{
                var writers=Callers(product,c=>c.Name=="set_EnablePairedActivation",false);
                var configure=Callers(product,c=>c.Name=="TryConfigurePairedActivation",false);
                Console.WriteLine("INFO EnablePairedActivation writers: "+string.Join(", ",writers.ToArray())+"; TryConfigurePairedActivation callers: "+string.Join(", ",configure.ToArray()));
                var forwarders=new[]{"MountedCombatController.TryConfigurePairedActivation","MountedPairTurnParticipant.TryConfigurePairedActivation"};
                return Every("C3",writers.Count==2 && writers.All(w=>w.EndsWith(".TryConfigurePairedActivation",StringComparison.Ordinal)),
                    configure.All(c=>forwarders.Contains(c)));
            });
        Check("PRODUCT-IL the attack-time seam reads travel once, through the read-only reader, and adds one source: the about-to-trigger handler runs Apply inside a catch; Apply asks CommittedRollCalculation, QualifyingRangedWeaponAttack, the turn-based test, TryReadCurrentMountTravel once and Evaluate, and adds one bonus after checking the rule's existing sources; the reader applies the allocation lifetime and the currency rule and writes no field and calls no setter or allocation writer",
            ()=>{
                var service=product.GetType("KingmakerMountedCombat.Integration.MountedRangedMovementService",true);
                var apply=Body(M(service,"Apply"));
                var trigger=M(service,"OnEventAboutToTrigger").GetMethodBody();
                var adapter=product.GetType("KingmakerMountedCombat.Integration.MountedMovementStateAdapter",true);
                var reader=Body(M(adapter,"TryReadCurrentMountTravel"));
                var sources=apply.FindIndex(i=>i.Call!=null && i.Call.Name=="get_BonusSources");
                var add=Find(apply,null,"AddBonus");
                var writes=reader.Where(i=>i.Op==OpCodes.Stfld || i.Op==OpCodes.Stsfld).Select(i=>((FieldInfo)i.Operand).Name).ToList();
                var setters=reader.Where(i=>i.Call!=null && (i.Call.Name.StartsWith("set_",StringComparison.Ordinal) || new[]{"Get","Prepared","BeginGrantedPreparation","ObserveNativeMovement","TickDelegated","Advance","Clear"}.Contains(i.Call.Name))).Select(i=>i.Call.Name).ToList();
                Console.WriteLine("INFO reader writes: "+string.Join(",",writes.ToArray())+"; reader setters/writers: "+string.Join(",",setters.ToArray()));
                return Every("C4",trigger.ExceptionHandlingClauses.Any(c=>c.Flags==ExceptionHandlingClauseOptions.Clause),
                    Find(apply,"MountedRangedMovementPolicy","CommittedRollCalculation").Count==1, Find(apply,"MountedRangedMovementPolicy","QualifyingRangedWeaponAttack").Count==1,
                    Find(apply,null,"IsInTurnBasedCombat").Count==1, Find(apply,null,"TryReadCurrentMountTravel").Count==1, Find(apply,"MountedRangedMovementPolicy","Evaluate").Count==1,
                    add.Count==1, sources>=0 && add.Count==1 && sources<add[0],
                    Find(reader,null,"MaintainLifetimes").Count==1, Find(reader,"MountedRangedMovementPolicy","IsAllocationCurrent").Count==1,
                    Find(reader,"MountedRangedMovementPolicy","NormalTravelSeconds").Count==1, writes.Count==0, setters.Count==0);
            });
        Check("PRODUCT-EXECUTED the compiled rule: a Move is 3 s, the second Move is entered past 3.25 s (one native 0.25 s borrowed tick), -4 base and -2 with Mounted Archery; a mount's own native grant, another activation, another controller or a split activation are never current; a combat mount is admitted only on the exact rider's turn; the production settings are the qualified authority",
            ()=>{
                Func<string,object> constant=n=>policy.GetField(n).GetRawConstantValue();
                var current=policy.GetMethod("IsAllocationCurrent");
                Func<bool,string,string,bool,bool,bool> c=(same,mount,pair,paired,round)=>(bool)current.Invoke(null,new object[]{same,mount,pair,paired,round});
                var entered=policy.GetMethod("EnteredSecondMove");
                var activationType=product.GetType("KingmakerMountedCombat.Domain.PairedActivation`2",true).MakeGenericType(typeof(object),typeof(object));
                object rider=new object(), horse=new object(), boundary=new object();
                var activation=Activator.CreateInstance(activationType,new[]{rider,horse});
                activationType.GetMethod("Begin").Invoke(activation,new[]{boundary});
                activationType.GetMethod("BeginActorPreparation").Invoke(activation,new[]{rider,boundary});
                activationType.GetMethod("BeginActorPreparation").Invoke(activation,new[]{horse,boundary});
                var grantMethod=policy.GetMethod("CurrentPairedGrant").MakeGenericMethod(typeof(object),typeof(object));
                var grant=(string)grantMethod.Invoke(null,new[]{activation,rider,horse});
                activationType.GetMethod("Detach").Invoke(activation,null);
                var afterSplit=(string)grantMethod.Invoke(null,new[]{activation,rider,horse});
                var adoption=product.GetType("KingmakerMountedCombat.Domain.MidEncounterAdoptionPolicy",true).GetMethod("Resolve");
                Func<bool,int,int,string> resolve=(riderTurn,r,m)=>adoption.Invoke(null,new object[]{true,riderTurn,r,m,false,false,true}).ToString();
                var settingsType=product.GetType("KingmakerMountedCombat.Diagnostics.DiagnosticSettings",true);
                var settings=Activator.CreateInstance(settingsType);
                Func<string,bool> flag=n=>(bool)settingsType.GetProperty(n).GetValue(settings,null);
                var qualified=(bool)product.GetType("KingmakerMountedCombat.Domain.MountedAuthorityPolicy",true).GetMethod("IsQualifiedForCombatMount")
                    .Invoke(null,new object[]{flag("EnablePairedActivation"),flag("EnableUnifiedMountedTurn"),flag("EnablePairedCommandScheduler")});
                return Every("C5",(float)constant("FirstMoveSeconds")==3f, (float)constant("SecondMoveThresholdSeconds")==3.25f,
                    (int)constant("DoubleMovePenalty")==-4, (int)constant("MountedArcheryDoubleMovePenalty")==-2,
                    !(bool)entered.Invoke(null,new object[]{3.25f}), (bool)entered.Invoke(null,new object[]{3.26f}),
                    grant!=null && c(true,grant,grant,true,true), !c(true,"native:"+new string('a',32),grant,true,true), !c(false,grant,grant,true,true),
                    afterSplit==null && !c(true,grant,afterSplit,true,true),
                    resolve(false,2,5)=="Unavailable" && resolve(false,5,2)=="Unavailable" && resolve(true,2,5)=="PreparePartnerThisRound" && resolve(true,5,2)=="RetainPartnerParticipation",
                    qualified);
            });

        Console.WriteLine("MOUNTED RANGED MOVEMENT CONTRACT PASS="+passes+" FAIL="+failures+"; compiled-product contracts, not native gameplay evidence");
        return failures==0?0:1;
    }
}
'@
try { $kmcCode=[KmcRangedMovementProbe]::Run($kmcManaged,$kmcDll) }
catch { Write-Output $_.Exception.ToString(); exit 1 }
exit $kmcCode
