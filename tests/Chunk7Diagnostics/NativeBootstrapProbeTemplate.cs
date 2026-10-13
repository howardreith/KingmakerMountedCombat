using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Newtonsoft.Json.Linq;
namespace KingmakerMountedCombat.Diagnostics {
static class Time { internal static int frameCount = 10; }
class Actor { internal string UniqueId = "main-native"; }
class Holder { internal Actor Value; }
class Player { internal Holder MainCharacter = new Holder(); public string GameId { get; set; } }
class NativeState { public Player PlayerState; public Player OtherPlayer; }
class Game {
 internal static Game Instance; internal static object NewGameUnit;
 public NativeState State = new NativeState(); public NativeState OtherState;
 public Player Player => State.PlayerState; public SaveManager SaveManager = new SaveManager();
 internal Game(){State.PlayerState=new Player();}
 public void LoadNewGame(BlueprintAreaPreset preset,SaveInfo importFrom){}
 public void Other(BlueprintAreaPreset preset,SaveInfo importFrom){}
}
class Area { public string AssetGuidThreadSafe = "area"; }
class BlueprintAreaPreset { public string AssetGuid = "preset"; }
class SaveInfo {
 public enum SaveType { Auto, Manual }
 public SaveType Type=SaveType.Auto;public bool IsAutoLevelupSave;public string FileName="Auto_1.zks",Name="Auto",FolderName="owned/Auto_1.zks",GameId,GameName="Native";public Area Area=new Area();
}
class SaveManager { public void SaveRoutine(SaveInfo saveInfo,bool singleThread){} public void PrepareSave(SaveInfo save){} }
static class Chunk7NativeBootstrapSelection { internal const string AutoLeaf="Auto_1.zks"; }
class CodeInstruction {
 public OpCode opcode;public object operand;public readonly List<Label> labels=new List<Label>();public readonly List<object> blocks=new List<object>();
 public CodeInstruction(OpCode opcode,object operand=null){this.opcode=opcode;this.operand=operand;}
}
class HarmonyMethod { internal readonly MethodInfo Method;public int prioritiy;public HarmonyMethod(MethodInfo method){Method=method;} }
static class Priority { internal const int First=800,Last=0; }
class HarmonyInstance {
 internal static readonly Dictionary<string,Tuple<HarmonyMethod,HarmonyMethod>> Hooks=new Dictionary<string,Tuple<HarmonyMethod,HarmonyMethod>>();
 internal static int UnpatchFailures,UnpatchCalls;internal static bool ForeignOwner;
 internal static HarmonyInstance Create(string id)=>new HarmonyInstance();
 public void Patch(MethodInfo original,HarmonyMethod prefix,HarmonyMethod postfix,HarmonyMethod transpiler=null){
  Hooks[original.Name]=Tuple.Create(prefix,postfix);
  if(original.Name=="LoadNewGame"){
   NativeHarness.Build();NativeHarness.Mutate?.Invoke(NativeHarness.Code);
   if(transpiler!=null){var owner=ForeignOwner?typeof(Game).GetMethod("Other"):original;NativeHarness.Code=((IEnumerable<CodeInstruction>)transpiler.Method.Invoke(null,new object[]{NativeHarness.Code,owner})).ToList();}
  }
 }
 public void UnpatchAll(string id){UnpatchCalls++;if(UnpatchFailures-->0)throw new InvalidOperationException("injected unpatch refusal");Hooks.Clear();}
}
static class MetadataBoundary { internal static void Require(){} }
static class NativeHarness {
 internal static List<CodeInstruction> Code,Original;internal static Action<List<CodeInstruction>> Mutate;internal static DynamicMethod Dynamic;internal static ILGenerator Generator;
 internal static void Reset(){HarmonyInstance.Hooks.Clear();HarmonyInstance.ForeignOwner=false;HarmonyInstance.UnpatchCalls=HarmonyInstance.UnpatchFailures=0;Time.frameCount=10;Mutate=null;Game.Instance=new Game();Game.NewGameUnit=null;}
 internal static void Build(){
  Dynamic=new DynamicMethod("NativeIdentitySequence",typeof(void),new[]{typeof(Game)},typeof(NativeHarness),true);Generator=Dynamic.GetILGenerator();
  for(int i=0;i<5;i++)Generator.DeclareLocal(typeof(object));var local=Generator.DeclareLocal(typeof(Guid));
  Code=new List<CodeInstruction>{new CodeInstruction(OpCodes.Ldarg_0),new CodeInstruction(OpCodes.Ldfld,typeof(Game).GetField("State")),
   new CodeInstruction(OpCodes.Ldfld,typeof(NativeState).GetField("PlayerState")),new CodeInstruction(OpCodes.Call,typeof(Guid).GetMethod("NewGuid")),
   new CodeInstruction(OpCodes.Stloc_S,local),new CodeInstruction(OpCodes.Ldloca_S,local),new CodeInstruction(OpCodes.Constrained,typeof(Guid)),
   new CodeInstruction(OpCodes.Callvirt,typeof(object).GetMethod("ToString")),new CodeInstruction(OpCodes.Callvirt,typeof(Player).GetProperty("GameId").GetSetMethod()),new CodeInstruction(OpCodes.Ret)};
  Original=Code.ToList();
 }
 internal static void Emit(){foreach(var i in Code){
  foreach(var label in i.labels)Generator.MarkLabel(label);
  if(i.operand==null)Generator.Emit(i.opcode);else if(i.operand is MethodInfo)Generator.Emit(i.opcode,(MethodInfo)i.operand);
  else if(i.operand is FieldInfo)Generator.Emit(i.opcode,(FieldInfo)i.operand);else if(i.operand is LocalBuilder)Generator.Emit(i.opcode,(LocalBuilder)i.operand);
  else if(i.operand is Type)Generator.Emit(i.opcode,(Type)i.operand);else throw new InvalidOperationException("unexpected emitted operand");
 }((Action<Game>)Dynamic.CreateDelegate(typeof(Action<Game>)))(Game.Instance);}
 internal static void Call(string nativeName,bool after,params object[] args){Tuple<HarmonyMethod,HarmonyMethod> h;if(HarmonyInstance.Hooks.TryGetValue(nativeName,out h)){var m=after?h.Item2:h.Item1;m?.Method.Invoke(null,args);}}
 internal static void Hook(string hook,params object[] args){typeof(Chunk7NativeBootstrapProbe).GetNestedType("Hooks",BindingFlags.NonPublic).GetMethod(hook,BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,args);}
 internal static SaveInfo Sequence(Chunk7NativeBootstrapProbe probe,Game g,BlueprintAreaPreset preset,bool legacySetterCallback=false,Action<Game> beforeIdentity=null,Action<Game> afterIdentity=null,SaveManager requestManager=null,SaveManager prepareManager=null,Action<SaveInfo> changeSave=null){
  Call("LoadNewGame",false,g,preset,null);g.Player.MainCharacter.Value=new Actor();beforeIdentity?.Invoke(g);Emit();
  if(legacySetterCallback)Call("set_GameId",true,g.Player,g.Player.GameId);afterIdentity?.Invoke(g);
  var save=new SaveInfo{GameId=g.Player.GameId};changeSave?.Invoke(save);Call("SaveRoutine",false,requestManager??g.SaveManager,save);Call("LoadNewGame",true,g);Call("PrepareSave",true,prepareManager??g.SaveManager,save);return save;
 }
}
static class BootstrapProbeTests {
 static int pass,fail;static void Check(bool c,string name){if(c){pass++;Console.WriteLine("PASS "+name);}else{fail++;Console.WriteLine("FAIL "+name);}}
 static bool Refuses(Action a){try{a();return false;}catch{return true;}}
 static Chunk7NativeBootstrapProbe Create(out Game g,out BlueprintAreaPreset preset){NativeHarness.Reset();g=Game.Instance;preset=new BlueprintAreaPreset();var p=new Chunk7NativeBootstrapProbe(g,preset);p.Install();return p;}
 static void Mutation(string name,Action<List<CodeInstruction>> change,bool foreign=false){NativeHarness.Reset();NativeHarness.Mutate=change;HarmonyInstance.ForeignOwner=foreign;var p=new Chunk7NativeBootstrapProbe(Game.Instance,new BlueprintAreaPreset());Check(Refuses(p.Install)&&!p.Complete,"callsite refusal: "+name);p.Dispose();}
 static void BadBinding(string name,Action<Game> before=null,Action<Game> after=null,bool foreignSave=false,bool foreignPrepared=false,Action<SaveInfo> saveChange=null){Game g;BlueprintAreaPreset preset;var p=Create(out g,out preset);NativeHarness.Sequence(p,g,preset,false,before,after,foreignSave?new SaveManager():null,foreignPrepared?new SaveManager():null,saveChange);Check(!p.Complete&&((JArray)p.Capture()["errors"]).Count>0,"identity/save refusal: "+name);p.Dispose();}
 public static int Main(string[] args){
  bool all=args.Contains("--all");Game g;BlueprintAreaPreset preset;var p=Create(out g,out preset);
  var saved=NativeHarness.Sequence(p,g,preset,true);Check(p.Complete,"historical delivered-setter callback neighbor remains complete");p.Dispose();
  p=Create(out g,out preset);saved=NativeHarness.Sequence(p,g,preset,false);
  Check(p.Complete,"native identity sequence without a setter postfix completes exact observation");
  Check(((JArray)p.Capture()["errors"]).Count==0&&((JArray)p.Capture()["events"]).Count==5,"exact callsite yields five ordered events and no inferred-save fallback");
  Guid id;Check(Guid.TryParse(g.Player.GameId,out id)&&id!=Guid.Empty&&saved.GameId==g.Player.GameId,"original native Guid assignment and save identity remain unchanged");
  Check(NativeHarness.Code.Count(i=>Equals(i.operand,typeof(Player).GetProperty("GameId").GetSetMethod()))==1,"original identity setter remains exactly once");p.Dispose();
  if(all){
   Mutation("missing setter",c=>c.RemoveAt(8));Mutation("duplicate setter",c=>c.Insert(9,c[8]));Mutation("wrong setter opcode",c=>c[8].opcode=OpCodes.Call);
   Mutation("different Guid source",c=>c[3].operand=typeof(Guid).GetMethod("Empty",BindingFlags.Static|BindingFlags.Public));
   Mutation("duplicate Guid source",c=>c.Insert(0,new CodeInstruction(OpCodes.Call,typeof(Guid).GetMethod("NewGuid"))));
   Mutation("changed State receiver",c=>c[1].operand=typeof(Game).GetField("OtherState"));Mutation("changed Player receiver",c=>c[2].operand=typeof(NativeState).GetField("OtherPlayer"));
   Mutation("changed owner argument",c=>c[0].opcode=OpCodes.Ldarg_1);Mutation("changed local index",c=>c[4].operand=NativeHarness.Generator.DeclareLocal(typeof(Guid)));
   Mutation("changed address local",c=>c[5].operand=NativeHarness.Generator.DeclareLocal(typeof(Guid)));Mutation("changed constrained type",c=>c[6].operand=typeof(long));
   Mutation("changed ToString call",c=>c[7].operand=typeof(Guid).GetMethod("ToString",Type.EmptyTypes));Mutation("interior branch label",c=>c[6].labels.Add(NativeHarness.Generator.DefineLabel()));
   Mutation("interior exception boundary",c=>c[6].blocks.Add(new object()));Mutation("foreign original method",null,true);
   Mutation("observer already injected",c=>c.Insert(9,new CodeInstruction(OpCodes.Call,typeof(Chunk7NativeBootstrapProbe).GetNestedType("Hooks",BindingFlags.NonPublic).GetMethod("IdentityAssigned",BindingFlags.NonPublic|BindingFlags.Static))));
   p=Create(out g,out preset);var output=NativeHarness.Code;Check(output.Count==NativeHarness.Original.Count+2&&NativeHarness.Original.All(i=>output.Contains(i))&&
    output[9].opcode==OpCodes.Ldarg_0&&output[10].opcode==OpCodes.Call&&((MethodInfo)output[10].operand).Name=="IdentityAssigned","rewriter preserves all native instructions and inserts only two read-only instructions after setter");
   Check((bool)p.Capture()["identityCallsite"]["installed"]&&!p.Complete,"installed callsite alone grants no completed observation");p.Dispose();
   BadBinding("foreign Game",g0=>Game.Instance=new Game());BadBinding("foreign Player",g0=>g0.State.PlayerState=new Player{MainCharacter=new Holder{Value=new Actor()}});
   BadBinding("foreign SaveManager",g0=>g0.SaveManager=new SaveManager());BadBinding("late frame",g0=>Time.frameCount++);
   BadBinding("duplicate identity delivery",null,g0=>NativeHarness.Hook("IdentityAssigned",g0));BadBinding("changed identity after mint",null,g0=>g0.Player.GameId=Guid.NewGuid().ToString("D"));
   BadBinding("foreign save request",foreignSave:true);BadBinding("foreign prepared manager",foreignPrepared:true);BadBinding("manual save category",saveChange:s=>s.Type=SaveInfo.SaveType.Manual);
   BadBinding("auto level-up save",saveChange:s=>s.IsAutoLevelupSave=true);BadBinding("wrong prepared leaf",saveChange:s=>s.FileName="Auto_2.zks");BadBinding("wrong descriptor game id",saveChange:s=>s.GameId=Guid.NewGuid().ToString("D"));
   p=Create(out g,out preset);g.Player.GameId=Guid.NewGuid().ToString("D");NativeHarness.Sequence(p,g,preset);Check(!p.Complete,"preexisting campaign identity refuses new-game entry");p.Dispose();
   p=Create(out g,out preset);NativeHarness.Hook("IdentityAssigned",g);Check(!p.Complete&&((JArray)p.Capture()["errors"]).Count==1,"identity callback outside admitted native call refuses");p.Dispose();
   NativeHarness.Reset();NativeHarness.Mutate=c=>c.RemoveAt(8);p=new Chunk7NativeBootstrapProbe(Game.Instance,new BlueprintAreaPreset());Check(Refuses(p.Install),"failed rewrite remains an incomplete installation");HarmonyInstance.UnpatchFailures=1;Check(Refuses(p.Dispose)&&!p.IdentityReleased,"failed rewrite plus unpatch failure retains identity custody");p.Dispose();Check(p.IdentityReleased,"failed rewrite disposal retry releases identity custody");
  }
  Console.WriteLine("NATIVE BOOTSTRAP PROBE PASS="+pass+" FAIL="+fail+"; actual production probe/transpiler; native metadata/Harmony/game are substitutes; no game or archive operations.");return fail==0?0:1;
 }
}
}
