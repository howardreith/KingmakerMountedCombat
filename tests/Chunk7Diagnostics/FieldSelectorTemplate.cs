using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
class UnitEntityData {public string UniqueId;}
class UnitReference {public UnitEntityData Value;}
class Player {public List<UnitReference> RemoteCompanions=new List<UnitReference>(),PartyCharacters=new List<UnitReference>();}
class Game {public static Game Instance;public Player Player=new Player();public Ui UI=new Ui();}
class Ui {public Canvas Canvas=new Canvas();} class Canvas {public GroupManager GroupManager=new GroupManager();}
class GameObject {public bool activeSelf,activeInHierarchy=true;}
class GroupManager {
 public GameObject gameObject=new GameObject();public float alpha;public int switches,goes,closes;public bool waitGo,throwOpen,throwClose,replaceGo;
 public List<UnitReference> selected=new List<UnitReference>();
 public void SwitchCharacter(UnitReference value){switches++;selected.Add(value);}
 public void Go(){goes++;if(replaceGo)Game.Instance.UI.Canvas.GroupManager=new GroupManager();if(!waitGo)gameObject.activeSelf=false;}
 public void Close(){closes++;if(throwClose)throw new InvalidOperationException("native Close failure");gameObject.activeSelf=false;}
}
class ShowPartySelection {public static int opens;public void RunAction(){opens++;var g=Game.Instance.UI.Canvas.GroupManager;g.gameObject.activeSelf=true;g.alpha=0;if(g.throwOpen)throw new InvalidOperationException("native partial open");}}
public sealed class FieldSelectorAdapter {
 private readonly UnitEntityData actor=new UnitEntityData{UniqueId="new"};private readonly string[] originalParty=new[]{"old"};private readonly Action guard;
 private GroupManager group;private bool started,owned,goRequested,completed,blocked;private readonly JObject evidence=new JObject();
 public FieldSelectorAdapter(){Game.Instance=new Game();Game.Instance.Player.RemoteCompanions.Add(new UnitReference{Value=actor});
  Game.Instance.UI.Canvas.GroupManager.selected.Add(new UnitReference{Value=new UnitEntityData{UniqueId="old"}});guard=()=>{if(blocked)throw new InvalidOperationException("exact world/worker authority lost");};ShowPartySelection.opens=0;}
 private bool Current()=>ReferenceEquals(Game.Instance.UI.Canvas.GroupManager,group);private bool Active()=>group!=null&&group.gameObject.activeSelf;
 private List<UnitReference> Selection()=>group.selected;private float Alpha()=>group.alpha;
 private static void Require(bool value,string reason){if(!value)throw new InvalidOperationException(reason);}
 /*OPEN*/
 /*TICK*/
 /*DRAIN*/
 private static int pass,fail;
 private static bool Refuses(Action a){try{a();return false;}catch(InvalidOperationException){return true;}}
 private static void Check(string name,Func<bool> test){try{if(!test())throw new Exception("predicate false");pass++;Console.WriteLine("PASS "+name);}catch(Exception e){fail++;Console.WriteLine("FAIL "+name+": "+e.Message);}}
 public static int Verify(){
  Check("open at native alpha zero owns one selector",()=>{var a=new FieldSelectorAdapter();a.Open();return a.owned&&a.group.gameObject.activeSelf&&a.group.alpha==0&&ShowPartySelection.opens==1;});
  Check("repeat open never retries native action",()=>{var a=new FieldSelectorAdapter();a.Open();return Refuses(a.Open)&&ShowPartySelection.opens==1&&a.owned;});
  Check("pre-existing alpha-zero selector cannot be borrowed",()=>{var a=new FieldSelectorAdapter();Game.Instance.UI.Canvas.GroupManager.gameObject.activeSelf=true;return Refuses(a.Open)&&!a.owned&&ShowPartySelection.opens==0;});
  Check("partial open failure retains assigned owner",()=>{var a=new FieldSelectorAdapter();Game.Instance.UI.Canvas.GroupManager.throwOpen=true;return Refuses(a.Open)&&a.owned&&a.TryDrain()&&a.group.closes==1;});
  Check("fade wait performs no switch Go or reopen",()=>{var a=new FieldSelectorAdapter();a.Open();var first=a.Tick();a.group.alpha=.5f;var second=a.Tick();return !first&&!second&&a.owned&&a.group.switches==0&&a.group.goes==0&&ShowPartySelection.opens==1;});
  Check("hierarchy readiness is mandatory",()=>{var a=new FieldSelectorAdapter();a.Open();a.group.alpha=1;a.group.gameObject.activeInHierarchy=false;return !a.Tick()&&a.group.switches==0;});
  Check("NaN alpha refuses before native choice",()=>{var a=new FieldSelectorAdapter();a.Open();a.group.alpha=float.NaN;return Refuses(()=>a.Tick())&&a.owned&&a.group.switches==0;});
  Check("native ready selector issues one switch Go",()=>{var a=new FieldSelectorAdapter();a.Open();a.group.alpha=1;return a.Tick()&&!a.owned&&a.completed&&a.group.switches==1&&a.group.goes==1&&a.TryDrain();});
  Check("native Go asynchronous closure never repeats input",()=>{var a=new FieldSelectorAdapter();a.Open();a.group.alpha=1;a.group.waitGo=true;var x=a.Tick();var y=a.Tick();a.group.gameObject.activeSelf=false;return !x&&!y&&a.Tick()&&a.group.goes==1&&a.group.switches==1;});
  Check("native cap refuses before switch",()=>{var a=new FieldSelectorAdapter();a.Open();a.group.alpha=1;for(int i=0;i<5;i++)a.group.selected.Add(new UnitReference{Value=new UnitEntityData{UniqueId="other"+i}});return Refuses(()=>a.Tick())&&a.group.switches==0&&a.owned;});
  Check("original party replacement refuses before switch",()=>{var a=new FieldSelectorAdapter();a.Open();a.group.alpha=1;a.group.selected.Clear();return Refuses(()=>a.Tick())&&a.group.switches==0;});
  Check("remote actor replacement refuses before switch",()=>{var a=new FieldSelectorAdapter();a.Open();a.group.alpha=1;Game.Instance.Player.RemoteCompanions.Clear();return Refuses(()=>a.Tick())&&a.group.switches==0;});
  Check("selector replacement before ready retains custody",()=>{var a=new FieldSelectorAdapter();a.Open();Game.Instance.UI.Canvas.GroupManager=new GroupManager();return Refuses(()=>a.Tick())&&a.owned&&a.group.switches==0;});
  Check("replacement by native Go cannot counterfeit closure",()=>{var a=new FieldSelectorAdapter();a.Open();a.group.alpha=1;a.group.replaceGo=true;return Refuses(()=>a.Tick())&&a.owned&&a.group.goes==1;});
  Check("replaced settled selector fences later cleanup",()=>{var a=new FieldSelectorAdapter();a.Open();a.group.alpha=1;a.Tick();Game.Instance.UI.Canvas.GroupManager=new GroupManager();return Refuses(()=>a.TryDrain());});
  Check("reopened settled selector fences later cleanup",()=>{var a=new FieldSelectorAdapter();a.Open();a.group.alpha=1;a.Tick();a.group.gameObject.activeSelf=true;return Refuses(()=>a.TryDrain())&&a.group.closes==0;});
  Check("world or worker guard fences Close",()=>{var a=new FieldSelectorAdapter();a.Open();a.blocked=true;return Refuses(()=>a.TryDrain())&&a.owned&&a.group.closes==0;});
  Check("foreign group fences Close",()=>{var a=new FieldSelectorAdapter();a.Open();Game.Instance.UI.Canvas.GroupManager=new GroupManager();return Refuses(()=>a.TryDrain())&&a.owned&&a.group.closes==0;});
  Check("failed Close retains owner then retry settles",()=>{var a=new FieldSelectorAdapter();a.Open();a.group.throwClose=true;var refused=Refuses(()=>a.TryDrain());var retained=a.owned;a.group.throwClose=false;return refused&&retained&&a.TryDrain()&&!a.owned&&a.group.closes==2;});
  Check("never opened selector drains without mutation",()=>{var a=new FieldSelectorAdapter();return a.TryDrain()&&ShowPartySelection.opens==0;});
  Console.WriteLine("PASS "+pass+" FAIL "+fail);return fail;
 }
}
