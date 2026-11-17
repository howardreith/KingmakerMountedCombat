[CmdletBinding()]
param([ValidateSet('Debug','Release')][string]$Configuration='Release',[string]$ProductDll)
# Known issues C04: Candlemere Tower, "wade through" while mounted.
#
# Four kinds of evidence, none of which launches anything. Each check's label names its kind:
#   ASSET             the installed Candlemere scene data (level348 and sharedassets348.assets, Unity 2018.4,
#                     serialized format 17, no type trees) is READ in place with a minimal reader: which component
#                     "Wade through" is, what it checks, what its passed and failed action lists hold, where its two
#                     enter points and its direction zone are. Nothing is copied or extracted.
#   NATIVE-IL         the installed Assembly-CSharp is READ: how zone membership is kept, how the condition reads it, how
#                     the interaction runs its actions and how the party is moved.
#   PRODUCT-IL        the compiled product is READ for its patch registration and what its hooks call.
#   PRODUCT-EXECUTED  the compiled product's recheck decision is RUN.
# -ProductDll reads another build (the Sprint 3 base e5b92474 must FAIL the PRODUCT groups; ASSET and NATIVE-IL are
# independent of the product). Every check runs; the script exits 1 if any failed. This is not native gameplay evidence.
$ErrorActionPreference='Stop'
$kmcRepo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$kmcLab=[IO.Path]::GetFullPath((Join-Path $kmcRepo '../..'))
$kmcLayout=(Get-Content -Raw (Join-Path $kmcLab 'environment-intake.json')|ConvertFrom-Json).requestedLayout
$kmcManaged=Join-Path $kmcLayout.kingmakerInstallDir 'Kingmaker_Data/Managed'
$kmcDataDir=Join-Path $kmcLayout.kingmakerInstallDir 'Kingmaker_Data'
$kmcDll=if($ProductDll){[IO.Path]::GetFullPath($ProductDll)}else{Join-Path $kmcRepo "bin/$Configuration/KingmakerMountedCombat.dll"}
if(-not(Test-Path -LiteralPath $kmcDll -PathType Leaf)){Write-Output "The product build is missing: $kmcDll";exit 1}
Add-Type -TypeDefinition @'
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

// A minimal, read-only reader of Unity 2018.4 serialized files (format 17, no type trees).
public sealed class KmcUnityFile
{
    public readonly string Name; public readonly byte[] B; public readonly List<string> Externals=new List<string>();
    public readonly Dictionary<long,int[]> Objects=new Dictionary<long,int[]>(); private readonly List<int> types=new List<int>();
    public int O;
    private static readonly Dictionary<string,KmcUnityFile> loaded=new Dictionary<string,KmcUnityFile>(StringComparer.OrdinalIgnoreCase);
    public static string DataDir;
    public static KmcUnityFile Load(string name)
    {
        KmcUnityFile file;
        if(!loaded.TryGetValue(name,out file)) { file=new KmcUnityFile(name); loaded[name]=file; }
        return file;
    }
    private KmcUnityFile(string name)
    {
        Name=name; B=File.ReadAllBytes(Path.Combine(DataDir,name));
        var version=BigEndian(8); var dataOffset=BigEndian(12);
        if(version!=17) throw new InvalidDataException(name+" is serialized format "+version+", not 17");
        O=20; CString(); I32(); if(U8()!=0) throw new InvalidDataException(name+" has type trees");
        var typeCount=I32();
        for(var i=0;i<typeCount;i++){ var cid=I32(); U8(); O+=2; if(cid==114||cid<0) O+=16; O+=16; types.Add(cid); }
        var objectCount=I32();
        for(var i=0;i<objectCount;i++){ Align(); var pid=I64(); var start=(int)U32(); var size=(int)U32(); var t=I32(); Objects[pid]=new[]{start+dataOffset,size,types[t]}; }
        var scripts=I32(); for(var i=0;i<scripts;i++){ I32(); Align(); I64(); }
        var externals=I32(); for(var i=0;i<externals;i++){ CString(); O+=16; I32(); Externals.Add(CString()); }
    }
    private int BigEndian(int at){ return (B[at]<<24)|(B[at+1]<<16)|(B[at+2]<<8)|B[at+3]; }
    public void Align(){ O=(O+3)&~3; }
    public int U8(){ return B[O++]; }
    public int I32(){ var v=BitConverter.ToInt32(B,O); O+=4; return v; }
    public uint U32(){ var v=BitConverter.ToUInt32(B,O); O+=4; return v; }
    public long I64(){ var v=BitConverter.ToInt64(B,O); O+=8; return v; }
    public float F32(){ var v=BitConverter.ToSingle(B,O); O+=4; return v; }
    public bool Bool(){ var v=U8()!=0; Align(); return v; }
    public string CString(){ var e=Array.IndexOf(B,(byte)0,O); var s=Encoding.UTF8.GetString(B,O,e-O); O=e+1; return s; }
    public string AString(){ var n=I32(); var s=Encoding.UTF8.GetString(B,O,n); O+=n; Align(); return s; }
    public long[] PPtr(){ return new long[]{ I32(), I64() }; }
    public KmcUnityFile Resolve(long fileId){ return fileId==0?this:Load(Path.GetFileName(Externals[(int)fileId-1])); }
    public int Class(long pid){ return Objects[pid][2]; }
    public int End(long pid){ return Objects[pid][0]+Objects[pid][1]; }
    // A MonoBehaviour's script class name and object name; leaves O at its first own field.
    public string Behaviour(long pid, out string name)
    {
        O=Objects[pid][0]; PPtr(); Bool(); var script=PPtr(); name=AString();
        var at=O; var file=Resolve(script[0]); var className=file.ScriptName(script[1]); O=at; return className;
    }
    private string ScriptName(long pid)
    {
        var at=O; O=Objects[pid][0]; AString(); I32(); O+=16; var cls=AString(); var ns=AString(); O=at;
        return ns.Length==0?cls:ns+"."+cls;
    }
    public long GameObject(long pid){ var at=O; O=Objects[pid][0]; var go=PPtr(); O=at; return go[1]; }
    public string GameObjectName(long goPid)
    {
        var at=O; O=Objects[goPid][0]; var n=I32(); for(var i=0;i<n;i++) PPtr(); U32(); var s=AString(); O=at; return s;
    }
    public long TransformOf(long goPid)
    {
        var at=O; O=Objects[goPid][0]; var n=I32(); long found=0;
        for(var i=0;i<n;i++){ var c=PPtr(); if(c[0]==0 && Class(c[1])==4) found=c[1]; }
        O=at; return found;
    }
    // The world position of a point local to a transform, through its parents.
    public float[] World(long tpid, float[] local)
    {
        var v=(float[])local.Clone();
        while(tpid!=0)
        {
            var at=O; O=Objects[tpid][0]; PPtr();
            float qx=F32(),qy=F32(),qz=F32(),qw=F32(),px=F32(),py=F32(),pz=F32(),sx=F32(),sy=F32(),sz=F32();
            var kids=I32(); for(var i=0;i<kids;i++) PPtr(); var father=PPtr(); O=at;
            float vx=v[0]*sx,vy=v[1]*sy,vz=v[2]*sz;
            float tx=2*(qy*vz-qz*vy),ty=2*(qz*vx-qx*vz),tz=2*(qx*vy-qy*vx);
            v=new[]{ vx+qw*tx+(qy*tz-qz*ty)+px, vy+qw*ty+(qz*tx-qx*tz)+py, vz+qw*tz+(qx*ty-qy*tx)+pz };
            tpid=father[0]==0?father[1]:0;
        }
        return v;
    }
}

public static class KmcCandlemereProbe
{
    private static int passes, failures;
    private static MethodInfo read;
    private const BindingFlags All=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly;
    private const BindingFlags Any=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;

    private static void Check(string label, Func<bool> body)
    {
        string detail=null; var ok=false;
        try { ok=body(); }
        catch(Exception error) { var inner=error; while(inner is TargetInvocationException && inner.InnerException!=null) inner=inner.InnerException; detail=inner.GetType().Name+": "+inner.Message; }
        if(ok){ passes++; Console.WriteLine("PASS "+label); }
        else { failures++; Console.WriteLine("FAIL "+label+(detail!=null?" ["+detail+"]":"")); }
    }

    private sealed class Ins { public OpCode Op; public object Operand; public MethodBase Call; public FieldInfo Field; }
    private static List<Ins> Body(MethodBase method)
    {
        if(method==null) throw new InvalidOperationException("the method does not exist in this build");
        var found=new List<Ins>();
        foreach(var raw in (IEnumerable)read.Invoke(null,new object[]{method,null}))
        {
            var ins=new Ins(); ins.Op=(OpCode)raw.GetType().GetField("opcode").GetValue(raw); ins.Operand=raw.GetType().GetField("operand").GetValue(raw);
            ins.Call=ins.Operand as MethodBase; ins.Field=ins.Operand as FieldInfo; found.Add(ins);
        }
        return found;
    }
    private static List<int> Find(List<Ins> body, string type, string name)
    {
        var found=new List<int>();
        for(var i=0;i<body.Count;i++) if(body[i].Call!=null && body[i].Call.Name==name && (type==null || body[i].Call.DeclaringType.Name==type)) found.Add(i);
        return found;
    }
    private static int One(List<Ins> body, string type, string name)
    {
        var f=Find(body,type,name); if(f.Count!=1) throw new InvalidOperationException(name+" is referenced "+f.Count+" times, expected once"); return f[0];
    }
    private static MethodBase Token(Type type, string name, int token)
    {
        foreach(var member in type.GetMember(name,MemberTypes.Method,All)) if(((MethodBase)member).MetadataToken==token) return (MethodBase)member;
        throw new InvalidOperationException(type.Name+"."+name+" with token "+token.ToString("X8")+" does not exist");
    }

    // Unity's Vector3 == in XZ is not needed here: containment uses the native even-odd rule on the polygon's XZ points.
    private static bool InsideXZ(List<float[]> poly, float[] p)
    {
        var c=false;
        for(int i=0,j=poly.Count-1;i<poly.Count;j=i++)
            if((poly[i][2]>p[2])!=(poly[j][2]>p[2]) && p[0]<(poly[j][0]-poly[i][0])*(p[2]-poly[i][2])/(poly[j][2]-poly[i][2])+poly[i][0]) c=!c;
        return c;
    }

    public static int Run(string managed, string dataDir, string productPath)
    {
        AppDomain.CurrentDomain.AssemblyResolve+=(s,a)=>{
            var leaf=new AssemblyName(a.Name).Name+".dll";
            foreach(var folder in new[]{managed,Path.Combine(managed,"UnityModManager")}){ var p=Path.Combine(folder,leaf); if(File.Exists(p)) return Assembly.LoadFrom(p); }
            return null;
        };
        KmcUnityFile.DataDir=dataDir;
        var native=Assembly.LoadFrom(Path.Combine(managed,"Assembly-CSharp.dll"));
        var product=Assembly.LoadFrom(productPath);
        Console.WriteLine("INFO product "+productPath+" MVID "+product.ManifestModule.ModuleVersionId);
        var modern=Assembly.LoadFrom(Path.Combine(managed,"UnityModManager/0Harmony.dll"));
        foreach(var method in modern.GetType("HarmonyLib.PatchProcessor",true).GetMethods(BindingFlags.Public|BindingFlags.Static))
            if(method.Name=="GetOriginalInstructions" && method.GetParameters().Length==2 && !method.GetParameters()[1].ParameterType.IsByRef) read=method;

        // ---- ASSET: the authored crossing ----
        var scene=KmcUnityFile.Load("level348");
        var shared=KmcUnityFile.Load("sharedassets348.assets");
        var behaviours=new Dictionary<string,List<long>>();
        foreach(var pair in scene.Objects)
        {
            if(pair.Value[2]!=114) continue;
            string ignored; var cls=scene.Behaviour(pair.Key,out ignored);
            var go=scene.GameObjectName(scene.GameObject(pair.Key));
            var key=go+"|"+cls; List<long> list;
            if(!behaviours.TryGetValue(key,out list)) behaviours[key]=list=new List<long>();
            list.Add(pair.Key);
        }
        Func<string,string,long> find=(go,cls)=>{ List<long> l; if(!behaviours.TryGetValue(go+"|"+cls,out l) || l.Count!=1) throw new InvalidDataException("no single "+cls+" on "+go); return l[0]; };
        long[] passed=null, failed=null;
        Check("ASSET the Candlemere scene (level348) holds the crossing: \"Wade through\" is the InteractionSkillCheck of map object FindWay, its display name is Interaction_CandlemereTower_FindWay_DisplayName, it checks Lore (Nature) against DC 25 (not exact), approach type within 4 m, checked once and run on every use, with no component-level teleport",
            ()=>{
                var pid=find("FindWay","Kingmaker.View.MapObjects.InteractionSkillCheck");
                string ignored; scene.Behaviour(pid,out ignored);
                var calls=scene.I32(); if(calls!=0) return false;
                var uiType=scene.I32(); var notInCombat=scene.Bool(); var overtip=scene.Bool(); var radius=scene.F32();
                scene.Bool(); scene.Bool(); var type=scene.I32(); scene.F32(); scene.I32(); scene.AString(); scene.AString(); scene.AString(); scene.AString(); scene.PPtr();
                var skill=scene.I32(); var dc=scene.I32(); var exact=scene.Bool(); var fake=scene.I32();
                var displayName=scene.PPtr(); scene.PPtr(); scene.PPtr(); scene.PPtr();
                scene.Bool(); var disableAfterUse=scene.Bool(); var once=scene.Bool(); var every=scene.Bool();
                scene.PPtr(); scene.PPtr(); scene.PPtr(); scene.PPtr();
                passed=scene.PPtr(); var onSuccess=scene.AString(); var onFail=scene.AString(); scene.PPtr(); failed=scene.PPtr(); scene.Bool();
                scene.I32(); scene.F32(); scene.Bool(); scene.I32();
                if(scene.O!=scene.End(pid)) throw new InvalidDataException("the component did not decode to its exact end");
                string nameOfDisplay; scene.Resolve(displayName[0]).Behaviour(displayName[1],out nameOfDisplay);
                Console.WriteLine("INFO FindWay: skill="+skill+" DC="+dc+" exact="+exact+" fake="+fake+" type="+type+" radius="+radius+" once="+once+" everyClick="+every+" disableAfterUse="+disableAfterUse+" teleports=["+onSuccess+"|"+onFail+"]");
                return nameOfDisplay=="Interaction_CandlemereTower_FindWay_DisplayName" && skill==35 && dc==25 && !exact && fake==0 && type==1 && radius==4f &&
                    once && every && !disableAfterUse && onSuccess.Length==0 && onFail.Length==0 && passed[0]==3 && failed[0]==3;
            });
        Check("ASSET its failed actions only disable the interaction; its passed actions unlock two flags, then the Conditional \"which side of the passage we are on\" (UnitInScriptZone of the interacting unit in zone SortcutBefore) teleports the party to CandlemereTowerShortcut if true and to CandlemereTowerShortcutBack if false, then disable the Slash interaction (CutWay)",
            ()=>{
                string holderName; var holder=shared.Behaviour(failed[1],out holderName); shared.AString();
                var failedActions=new List<long[]>(); for(var n=shared.I32();n>0;n--) failedActions.Add(shared.PPtr());
                string ignored;
                if(failedActions.Count!=1 || shared.Behaviour(failedActions[0][1],out ignored)!="Kingmaker.Designers.EventConditionActionSystem.Actions.DisableMapObjectInteraction") return false;
                shared.Behaviour(passed[1],out holderName); shared.AString();
                var actions=new List<long[]>(); for(var n=shared.I32();n>0;n--) actions.Add(shared.PPtr());
                var kinds=new List<string>(); foreach(var a in actions) kinds.Add(shared.Behaviour(a[1],out ignored).Split('.')[4]);
                if(string.Join(",",kinds.ToArray())!="UnlockFlag,Conditional,UnlockFlag,DisableMapObjectInteraction") throw new InvalidDataException(string.Join(",",kinds.ToArray()));
                shared.Behaviour(actions[1][1],out ignored);
                var comment=shared.AString(); shared.I32();
                var conditions=new List<long[]>(); for(var n=shared.I32();n>0;n--) conditions.Add(shared.PPtr());
                var ifTrue=new List<long[]>(); for(var n=shared.I32();n>0;n--) ifTrue.Add(shared.PPtr());
                var ifFalse=new List<long[]>(); for(var n=shared.I32();n>0;n--) ifFalse.Add(shared.PPtr());
                if(conditions.Count!=1 || ifTrue.Count!=1 || ifFalse.Count!=1) return false;
                if(shared.Behaviour(conditions[0][1],out ignored)!="Kingmaker.Designers.EventConditionActionSystem.Conditions.UnitInScriptZone") return false;
                var not=shared.Bool(); var unit=shared.PPtr(); var zone=shared.PPtr();
                var unitKind=shared.Behaviour(unit[1],out ignored);
                shared.Behaviour(zone[1],out ignored); shared.O+=12; var zoneName=shared.AString();
                Func<long[],string> exit=(a)=>{ string n; if(shared.Behaviour(a[1],out n)!="Kingmaker.Designers.EventConditionActionSystem.Actions.TeleportParty") return null;
                    var p=shared.PPtr(); string bp; var file=shared.Resolve(p[0]); file.Behaviour(p[1],out bp); return bp; };
                var whenTrue=exit(ifTrue[0]); var whenFalse=exit(ifFalse[0]);
                shared.Behaviour(actions[3][1],out ignored); var slashed=shared.PPtr(); shared.Behaviour(slashed[1],out ignored); shared.O+=12; var slashName=shared.AString();
                Console.WriteLine("INFO passed actions: "+string.Join(",",kinds.ToArray())+"; condition Not="+not+" unit="+unitKind.Split('.')[4]+" zone="+zoneName+"; true -> "+whenTrue+"; false -> "+whenFalse+"; then disables "+slashName);
                return comment=="\u0441 \u043a\u0430\u043a\u043e\u0439 \u0441\u0442\u043e\u0440\u043e\u043d\u044b \u043e\u0442 \u043f\u0440\u043e\u0445\u043e\u0434\u0430 \u043d\u0430\u0445\u043e\u0434\u0438\u043c\u0441\u044f" && !not && unitKind=="Kingmaker.Designers.EventConditionActionSystem.Evaluators.InteractingUnit" &&
                    zoneName=="SortcutBefore" && whenTrue=="CandlemereTowerShortcut" && whenFalse=="CandlemereTowerShortcutBack" && slashName=="CutWay";
            });
        Check("ASSET the geometry: the interaction and the enter point Shortcut_Back are inside the polygon zone SortcutBefore and the enter point Shortcut is outside it, 8.26 m from Shortcut_Back, so the false branch places the party on the side it is already on",
            ()=>{
                var polygonPid=find("SortcutBefore","Kingmaker.View.MapObjects.SriptZones.ScriptZonePolygon");
                string ignored; scene.Behaviour(polygonPid,out ignored);
                var points=new List<float[]>(); for(var n=scene.I32();n>0;n--) points.Add(new[]{scene.F32(),scene.F32(),scene.F32()});
                var zoneTransform=scene.TransformOf(scene.GameObject(polygonPid));
                var world=new List<float[]>(); foreach(var p in points) world.Add(scene.World(zoneTransform,p));
                Func<string,string,float[]> at=(go,cls)=>scene.World(scene.TransformOf(scene.GameObject(find(go,cls))),new float[3]);
                var shortcut=at("Shortcut","Kingmaker.View.AreaEnterPoint");
                var back=at("Shortcut_Back","Kingmaker.View.AreaEnterPoint");
                var wade=at("FindWay","Kingmaker.View.MapObjects.InteractionSkillCheck");
                var distance=Math.Sqrt((shortcut[0]-back[0])*(shortcut[0]-back[0])+(shortcut[2]-back[2])*(shortcut[2]-back[2]));
                Console.WriteLine("INFO geometry: zone points="+world.Count+"; FindWay inside="+InsideXZ(world,wade)+"; Shortcut_Back inside="+InsideXZ(world,back)+"; Shortcut inside="+InsideXZ(world,shortcut)+"; crossing="+distance.ToString("0.00")+" m");
                return world.Count==7 && InsideXZ(world,wade) && InsideXZ(world,back) && !InsideXZ(world,shortcut) && Math.Abs(distance-8.26)<0.01;
            });

        // ---- NATIVE-IL ----
        var zoneController=native.GetType("Kingmaker.Controllers.MapObjects.ScriptZoneController",true);
        var moveController=native.GetType("Kingmaker.Controllers.Units.UnitMoveController",true);
        var unitType=native.GetType("Kingmaker.EntitySystem.Entities.UnitEntityData",true);
        var scriptZone=native.GetType("Kingmaker.View.MapObjects.SriptZones.ScriptZone",true);
        Check("NATIVE-IL ScriptZoneController.Tick (060092A0) checks a unit's membership only when it HasMotionThisTick (or the zone's active state changed); HasMotionThisTick (0600830F) is Position != PreviousPosition; UnitMoveController.Tick (06009183) stamps PreviousPosition before it moves any unit",
            ()=>{
                var tick=Body(Token(zoneController,"Tick",0x060092A0));
                var motion=Body(Token(unitType,"get_HasMotionThisTick",0x0600830F));
                var move=Body(Token(moveController,"Tick",0x06009183));
                return One(tick,"UnitEntityData","get_HasMotionThisTick")<One(tick,"ScriptZone","CheckUnit") &&
                    Find(motion,"UnitEntityData","get_Position").Count==1 && Find(motion,"UnitEntityData","get_PreviousPosition").Count==1 &&
                    One(move,"UnitEntityData","set_PreviousPosition")<Find(move,"UnitMovementAgentBase","TickMovement")[0];
            });
        Check("NATIVE-IL the crossing's chain: InteractionSkillCheck.OnInteract (06001A0A) runs the chosen action list inside InteractingUnitData of its user; UnitInScriptZone.CheckCondition (06008D42) answers from ScriptZone.ContainsUnit (06001AE9); TeleportParty.RunAction (06008F8F) calls Game.Teleport (06000CCB) for an enter point of the loaded area, which places the party through Player.MoveCharacters (06000DCC)",
            ()=>{
                var interact=Body(Token(native.GetType("Kingmaker.View.MapObjects.InteractionSkillCheck",true),"OnInteract",0x06001A0A));
                var condition=Body(Token(native.GetType("Kingmaker.Designers.EventConditionActionSystem.Conditions.UnitInScriptZone",true),"CheckCondition",0x06008D42));
                var teleport=Body(Token(native.GetType("Kingmaker.Designers.EventConditionActionSystem.Actions.TeleportParty",true),"RunAction",0x06008F8F));
                var game=Body(Token(native.GetType("Kingmaker.Game",true),"Teleport",0x06000CCB));
                var interactingCtor=false; foreach(var ins in interact) if(ins.Op==OpCodes.Newobj && ins.Call!=null && ins.Call.DeclaringType.Name=="InteractingUnitData") interactingCtor=true;
                return interactingCtor && Find(interact,"ActionList","Run").Count>=1 && One(condition,"ScriptZone","ContainsUnit")>=0 &&
                    One(teleport,"Game","Teleport")>=0 && Find(game,"LoadingProcess","StartLoadingProcess").Count==1;
            });

        // ---- PRODUCT ----
        var controller=product.GetType("KingmakerMountedCombat.Integration.MountedPatchController",true);
        var methods=controller.GetNestedType("PatchMethods",Any);
        Check("PRODUCT-IL the product patches ScriptZoneController.Tick by its exact token with a postfix; the postfix hands each pair's relationship to the zone adapter and contains a fault in a bounded report",
            ()=>{
                var found=false;
                foreach(var member in controller.GetMembers(All))
                {
                    var method=member as MethodBase; if(method==null || method.GetMethodBody()==null) continue;
                    var body=Body(method);
                    for(var i=0;i<body.Count;i++)
                    {
                        if(body[i].Op!=OpCodes.Ldstr || (string)body[i].Operand!="Kingmaker.Controllers.MapObjects.ScriptZoneController") continue;
                        for(var j=i+1;j<body.Count && j<i+16;j++)
                            if(body[j].Op==OpCodes.Ldc_I4 && (int)body[j].Operand==0x060092A0)
                                for(var k=j;k<body.Count && k<j+12;k++) if(body[k].Op==OpCodes.Ldstr && (string)body[k].Operand=="ScriptZoneTickPostfix") found=true;
                    }
                }
                var postfix=methods.GetMethod("ScriptZoneTickPostfix",Any);
                if(postfix==null) throw new InvalidOperationException("ScriptZoneTickPostfix does not exist in this build");
                var adapter=product.GetType("KingmakerMountedCombat.Integration.MountedScriptZoneAdapter",true);
                var lambdas=0; foreach(var t in new[]{methods}) foreach(var nested in t.GetNestedTypes(Any)) foreach(var m in nested.GetMethods(All))
                    if(m.Name.Contains("ScriptZoneTickPostfix") && m.GetMethodBody()!=null && Find(Body(m),"MountedScriptZoneAdapter","Recheck").Count==1) lambdas++;
                var body2=Body(postfix);
                return found && (Find(body2,"MountedScriptZoneAdapter","Recheck").Count+lambdas)>=2 && Find(body2,null,"ReportScriptZoneFault").Count==1;
            });
        Check("PRODUCT-IL the adapter reads the relationship's mounting instance and asks the instance-scoped observations (with the rider's native motion) before it checks with the native ScriptZone.CheckUnit, then records under that instance; zones are selected as native selects them (active, on a map object in game); the pair's synchronization of the rider still runs in the UnitMoveController prefix, before the native stamp",
            ()=>{
                var adapter=product.GetType("KingmakerMountedCombat.Integration.MountedScriptZoneAdapter",true);
                var recheck=Body(adapter.GetMethod("Recheck",Any));
                var zones=Body(adapter.GetMethod("ActiveZones",Any));
                var prefix=Body(methods.GetMethod("NativeMovementUpdatePrefix",Any));
                var visits=0; foreach(var nested in methods.GetNestedTypes(Any)) foreach(var m in nested.GetMethods(All))
                    if(m.Name.Contains("NativeMovementUpdatePrefix") && m.GetMethodBody()!=null && Find(Body(m),"GameMountedRelationshipService","BeginNativeMovementUpdate").Count==1) visits++;
                var ask=One(recheck,"MountedScriptZoneObservations`1","NeedsRecheck");
                return One(recheck,"GameMountedRelationshipService","get_MountingInstance")<ask &&
                    One(recheck,"UnitEntityData","get_HasMotionThisTick")<ask && ask<One(recheck,"ScriptZone","CheckUnit") &&
                    One(recheck,"ScriptZone","CheckUnit")<One(recheck,"MountedScriptZoneObservations`1","Record") &&
                    One(zones,"ScriptZone","get_IsActive")>=0 && One(zones,"EntityDataBase","get_IsInGame")>=0 &&
                    (Find(prefix,"GameMountedRelationshipService","BeginNativeMovementUpdate").Count+visits)>=1;
            });
        Check("PRODUCT-EXECUTED (review R1) the compiled observations belong to their mounting instance: a stationary rider is not checked again within one, a later instance at the same place is checked, another unit object is checked",
            ()=>{
                var open=product.GetType("KingmakerMountedCombat.Domain.MountedScriptZoneObservations`1",true);
                var observations=Activator.CreateInstance(open.MakeGenericType(typeof(object)));
                var pairType=product.GetType("KingmakerMountedCombat.Domain.MountedPair",true);
                var first=Activator.CreateInstance(pairType,"rider-a","horse-a"); var later=Activator.CreateInstance(pairType,"rider-a","horse-a");
                var needs=observations.GetType().GetMethod("NeedsRecheck"); var record=observations.GetType().GetMethod("Record");
                Func<object,object,bool> ask=(unit,instance)=>(bool)needs.Invoke(observations,new object[]{unit,instance,false,1f,0f,1f});
                var rider=new object();
                var firstAsk=ask(rider,first);
                record.Invoke(observations,new object[]{rider,first,1f,0f,1f});
                return firstAsk && !ask(rider,first) && ask(rider,later) && ask(new object(),first);
            });
        Check("PRODUCT-EXECUTED the compiled decision checks a seated rider only when the pair is mounted, native did not check it this tick, and it moved since its last check (or was never checked while mounted); position equality follows Unity's Vector3 equality",
            ()=>{
                var policy=product.GetType("KingmakerMountedCombat.Domain.MountedScriptZonePolicy",true);
                var decide=policy.GetMethod("RiderNeedsZoneRecheck");
                Func<bool,bool,bool,float,bool> d=(m,n,c,dx)=>(bool)decide.Invoke(null,new object[]{m,n,c,dx,0f,0f});
                var changed=policy.GetMethod("PositionChanged");
                return d(true,false,false,0f) && d(true,false,true,0.5f) && !d(true,false,true,0f) && !d(true,true,true,3f) && !d(false,false,false,3f) &&
                    !(bool)changed.Invoke(null,new object[]{0.000001f,0f,0f}) && (bool)changed.Invoke(null,new object[]{0.0001f,0f,0f});
            });

        Console.WriteLine("CANDLEMERE CROSSING CONTRACT PASS="+passes+" FAIL="+failures+"; installed-asset, installed-assembly and compiled-product contracts, not native gameplay evidence");
        return failures==0?0:1;
    }
}
'@
try { $kmcCode=[KmcCandlemereProbe]::Run($kmcManaged,$kmcDataDir,$kmcDll) }
catch { Write-Output $_.Exception.ToString(); exit 1 }
exit $kmcCode
