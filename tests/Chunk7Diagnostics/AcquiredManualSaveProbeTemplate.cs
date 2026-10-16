using System;using System.Collections.Generic;
namespace KingmakerMountedCombat.Diagnostics {
internal static class Chunk7ManualSeedSelection {public const string OutputName="KMC_C7_NATIVE_SEED";}
internal static class Chunk7AcquiredManualSelection {public const string OutputName="KMC_C7_ACQUIRED_FIELD"; internal static string NativeOutputLeaf(int number) { if(number<=0)throw new ArgumentOutOfRangeException(nameof(number));return "Manual_"+number.ToString(System.Globalization.CultureInfo.InvariantCulture)+"_"+OutputName+".zks"; }}
internal static class Chunk7AssistedFixtureSelection {public const string OutputName="KMC_C7_ASSISTED_FIXTURE"; internal static string ManualLeaf(int number) { if(number<=0)throw new ArgumentOutOfRangeException(nameof(number));return "Manual_"+number.ToString(System.Globalization.CultureInfo.InvariantCulture)+"_"+OutputName+".zks"; }}
internal sealed class MountedPersistenceService {}
internal sealed class Area {public string AssetGuidThreadSafe="area";}
internal sealed class Player {public string GameId="campaign";}
internal sealed class SaveManager {}
internal sealed class SaveInfo {public enum SaveType{Manual,Auto} public SaveType Type=SaveType.Manual;public bool IsActuallySaved=false;public string Name,FileName,GameId="campaign";public Area Area;}
internal sealed class Game {public static Game Instance;public SaveManager SaveManager=new SaveManager();public Player Player=new Player();public Area CurrentlyLoadedArea=new Area();}
internal sealed class Chunk7ManualSeedSaveProbe {
private static Chunk7ManualSeedSaveProbe active;
private readonly Game game;private readonly SaveManager manager;private readonly string leaf,nativeName;private readonly MountedPersistenceService persistence;private readonly Func<object,int> identity;private readonly Action assertWorld;
private bool installed=true;private SaveInfo expected,prepared;private int inside,requests,prepares;
private readonly List<string> errors=new List<string>(),events=new List<string>();
internal Chunk7ManualSeedSaveProbe(string name) : this(NewGame(),new MountedPersistenceService(),x=>1,()=>{},"Manual_1_"+name+".zks",name==Chunk7ManualSeedSelection.OutputName?null:name){active=this;}
private static Game NewGame(){Game.Instance=new Game();return Game.Instance;}
internal bool TestDependenciesPresent {get{return persistence!=null&&identity!=null&&assertWorld!=null;}}
//__CONSTRUCTOR__
internal SaveInfo Descriptor(string name){return new SaveInfo{Name=name,FileName="Manual_1_"+name+".zks",Area=game.CurrentlyLoadedArea};}
internal bool HasExpected{get{return expected!=null;}}
internal bool PreparedExact(SaveInfo v){return ReferenceEquals(prepared,v)&&prepares==1&&errors.Count==0&&events.Count==1&&inside==0;}
internal bool RefusedPrepared{get{return prepared==null&&errors.Count>0&&inside==0;}}
internal void Prepare(SaveInfo v,bool foreign=false){requests=1;Hooks.PreparedAfter(foreign?new SaveManager():manager,v);}
internal void Disable(){installed=false;}
private void Record(string kind,SaveInfo value){events.Add(kind);}
//__EXPECT__
//__OBSERVE__
private static void Require(bool v,string m){if(!v)throw new InvalidOperationException(m);}
private static class Hooks {
//__PREPARED__
}}
internal static class Program {
private static int pass,fail;
private static bool Throws(Action a){try{a();return false;}catch{return true;}}
private static void Check(bool v,string n){Console.WriteLine((v?"PASS ":"FAIL ")+n);if(v)pass++;else fail++;}
private static void Named(string name,string label){
var p=new Chunk7ManualSeedSaveProbe(name);var d=p.Descriptor(name);Check(!Throws(()=>p.Expect(d))&&p.HasExpected,label+" exact requested Manual admitted");
p=new Chunk7ManualSeedSaveProbe(name);d=p.Descriptor(name);p.Prepare(d);Check(p.PreparedExact(d),label+" exact prepared Manual observed");
p=new Chunk7ManualSeedSaveProbe(name);d=p.Descriptor("FOREIGN");Check(Throws(()=>p.Expect(d)),label+" foreign name refused");
p=new Chunk7ManualSeedSaveProbe(name);d=p.Descriptor(name);d.FileName="Manual_999_FOREIGN.zks";p.Prepare(d);Check(p.RefusedPrepared,label+" foreign leaf refused");
p=new Chunk7ManualSeedSaveProbe(name);d=p.Descriptor(name);p.Prepare(d,true);Check(p.RefusedPrepared,label+" foreign manager refused");
p=new Chunk7ManualSeedSaveProbe(name);d=p.Descriptor(name);d.IsActuallySaved=true;Check(Throws(()=>p.Expect(d)),label+" saved descriptor refused");
}
public static int Main(){
Named(Chunk7ManualSeedSelection.OutputName,"historical seed");Named("KMC_C7_ACQUIRED_FIELD","new acquired derivative");
var p=new Chunk7ManualSeedSaveProbe(Chunk7ManualSeedSelection.OutputName);var d=p.Descriptor(Chunk7ManualSeedSelection.OutputName);p.Expect(d);Check(Throws(()=>p.Expect(d)),"duplicate Expect refused");
p=new Chunk7ManualSeedSaveProbe(Chunk7ManualSeedSelection.OutputName);d=p.Descriptor(Chunk7ManualSeedSelection.OutputName);p.Disable();Check(Throws(()=>p.Expect(d)),"uninstalled Expect refused");
var g=new Game();var persistence=new MountedPersistenceService();Func<object,int> identity=x=>1;Action guard=()=>{};
Func<string,string,bool> admits=(name,leaf)=>!Throws(()=>new Chunk7ManualSeedSaveProbe(g,persistence,identity,guard,leaf,name));
Check(admits(null,"Manual_1_KMC_C7_NATIVE_SEED.zks"),"legacy optional-null constructor preserved");
Check(admits(Chunk7AcquiredManualSelection.OutputName,"Manual_1_KMC_C7_ACQUIRED_FIELD.zks"),"acquired canonical first positive slot");
Check(admits(Chunk7AcquiredManualSelection.OutputName,"Manual_2147483647_KMC_C7_ACQUIRED_FIELD.zks"),"acquired native Int32 maximum positive slot");
foreach(var leaf in new[]{(string)null,"","Manual_0_KMC_C7_ACQUIRED_FIELD.zks","Manual_-1_KMC_C7_ACQUIRED_FIELD.zks","Manual_+1_KMC_C7_ACQUIRED_FIELD.zks","Manual_01_KMC_C7_ACQUIRED_FIELD.zks","Manual_2147483648_KMC_C7_ACQUIRED_FIELD.zks","Manual_1.0_KMC_C7_ACQUIRED_FIELD.zks","Manual_ 1_KMC_C7_ACQUIRED_FIELD.zks","Manual_1_KMC_C7_ACQUIRED_FIELD.ZKS","manual_1_KMC_C7_ACQUIRED_FIELD.zks","Manual_1_KMC_C7_NATIVE_SEED.zks","C:/owned/Manual_1_KMC_C7_ACQUIRED_FIELD.zks","Manual_1_KMC_C7_ACQUIRED_FIELD.zks/extra"})
 Check(!admits(Chunk7AcquiredManualSelection.OutputName,leaf),"acquired noncanonical/foreign leaf refused: "+leaf);
foreach(var name in new[]{"","FOREIGN",Chunk7ManualSeedSelection.OutputName,"kmc_c7_acquired_field"})Check(!admits(name,"Manual_1_KMC_C7_ACQUIRED_FIELD.zks"),"explicit alias name refused: "+name);
Check(Throws(()=>new Chunk7ManualSeedSaveProbe(null,persistence,identity,guard,"leaf")),"null Game refused");
Check(Throws(()=>new Chunk7ManualSeedSaveProbe(g,null,identity,guard,"leaf")),"null persistence refused");
Check(Throws(()=>new Chunk7ManualSeedSaveProbe(g,persistence,null,guard,"leaf")),"null identity registry refused");
Check(Throws(()=>new Chunk7ManualSeedSaveProbe(g,persistence,identity,null,"leaf")),"null world guard refused");
Console.WriteLine("Actual name boundary "+pass+" passed, "+fail+" failed.");return fail==0?0:1;}
}}