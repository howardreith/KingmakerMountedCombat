using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
class LockedCompanionComponent {}
class Blueprint { public bool locked; public T GetComponent<T>() where T:class => locked ? new LockedCompanionComponent() as T : null; }
class Descriptor { public UnitEntityData Pet; public UnitReference Master=new UnitReference(); }
class UnitEntityData { public string UniqueId; public Blueprint Blueprint=new Blueprint(); public Descriptor Descriptor=new Descriptor(); }
class UnitReference { public UnitEntityData Value; }
class Player { public List<UnitReference> RemoteCompanions=new List<UnitReference>(),PartyCharacters=new List<UnitReference>(); public UnitReference MainCharacter=new UnitReference(); }
class Game { public static Game Instance; public Player Player=new Player(); public Ui UI=new Ui(); }
class Ui { public Canvas Canvas=new Canvas(); } class Canvas { public GroupManager GroupManager=new GroupManager(); }
class GameObject { public bool activeSelf,activeInHierarchy=true; }
class GroupManager {
 public GameObject gameObject=new GameObject(); public float alpha; public int switches,goes,closes,requiredReads;
 public bool waitGo,throwOpen,throwSwitch,throwGo,throwClose,required,keepTarget,dropNeighbor,replaceGo,failRemote;
 public List<UnitReference> selected=new List<UnitReference>();
 public bool MustBeInParty(UnitReference value){requiredReads++;return required;}
 public void SwitchCharacter(UnitReference value){switches++;if(!keepTarget)selected.Remove(value);if(dropNeighbor)selected.RemoveAt(0);if(throwSwitch)throw new InvalidOperationException("native switch failure");}
 public void Go(){goes++;var p=Game.Instance.Player;var removed=p.PartyCharacters.Where(v=>!selected.Contains(v)).ToArray();p.PartyCharacters=selected.ToList();if(!failRemote)p.RemoteCompanions.AddRange(removed);if(replaceGo)Game.Instance.UI.Canvas.GroupManager=new GroupManager();if(throwGo)throw new InvalidOperationException("native Go failure");if(!waitGo)gameObject.activeSelf=false;}
 public void Close(){closes++;if(throwClose)throw new InvalidOperationException("native Close failure");gameObject.activeSelf=false;}
}
class ShowPartySelection { public static int opens; public void RunAction(){opens++;var g=Game.Instance.UI.Canvas.GroupManager;g.selected=Game.Instance.Player.PartyCharacters.ToList();g.gameObject.activeSelf=true;g.alpha=0;if(g.throwOpen)throw new InvalidOperationException("native partial open");} }
public sealed class FieldDepartureAdapter {
 private readonly UnitEntityData actor=new UnitEntityData{UniqueId="linzi"};private readonly string[] originalParty=new[]{"main","a","b","c","d","linzi"};
 private readonly Action guard,cleanupGuard;private GroupManager group;private bool started,owned,switchAttempted,goRequested,completed,blocked,cleanupBlocked;private readonly JObject evidence=new JObject();
 public FieldDepartureAdapter(){Game.Instance=new Game();foreach(var id in originalParty)Game.Instance.Player.PartyCharacters.Add(new UnitReference{Value=id=="linzi"?actor:new UnitEntityData{UniqueId=id}});Game.Instance.Player.MainCharacter=Game.Instance.Player.PartyCharacters[0];guard=()=>{if(blocked)throw new InvalidOperationException("progress custody lost");};cleanupGuard=()=>{if(cleanupBlocked)throw new InvalidOperationException("cleanup custody lost");};ShowPartySelection.opens=0;}
 private bool Current()=>ReferenceEquals(Game.Instance.UI.Canvas.GroupManager,group);private bool Active()=>group!=null&&group.gameObject.activeSelf;
 private List<UnitReference> Selection()=>group.selected;private float Alpha()=>group.alpha;
 private static void Require(bool value,string reason){if(!value)throw new InvalidOperationException(reason);}
 /*IDS*/
 /*OPEN*/
 /*TICK*/
 /*DRAIN*/
 private static int pass,fail;
 private static bool Refuses(Action a){try{a();return false;}catch(InvalidOperationException){return true;}}
 private static void Check(string name,Func<bool> test){try{if(!test())throw new Exception("predicate false");pass++;Console.WriteLine("PASS "+name);}catch(Exception e){fail++;Console.WriteLine("FAIL "+name+": "+e.Message);}}
 public static int Verify(){
  Check("ordinary removal commits once and retains five neighbors",()=>{var a=new FieldDepartureAdapter();a.Open();a.group.alpha=1;return a.Tick()&&a.completed&&!a.owned&&a.group.switches==1&&a.group.goes==1&&a.group.requiredReads==1&&Game.Instance.Player.PartyCharacters.Count==5&&ReferenceEquals(Game.Instance.Player.RemoteCompanions.Single().Value,a.actor)&&a.TryDrain();});
  Check("fade and hierarchy wait issue no input",()=>{var a=new FieldDepartureAdapter();a.Open();var first=a.Tick();a.group.alpha=1;a.group.gameObject.activeInHierarchy=false;return !first&&!a.Tick()&&a.group.switches==0&&a.group.goes==0;});
  Check("invalid alpha refuses before input",()=>{var a=new FieldDepartureAdapter();a.Open();a.group.alpha=float.NaN;return Refuses(()=>a.Tick())&&a.group.switches==0&&a.owned;});
  Check("native required character is never switched",()=>{var a=new FieldDepartureAdapter();a.Open();a.group.alpha=1;a.group.required=true;return Refuses(()=>a.Tick())&&a.group.switches==0&&a.group.goes==0&&a.TryDrain();});
  Check("main character refused before selector open",()=>{var a=new FieldDepartureAdapter();Game.Instance.Player.MainCharacter.Value=a.actor;return Refuses(a.Open)&&ShowPartySelection.opens==0;});
  Check("locked character refused before selector open",()=>{var a=new FieldDepartureAdapter();a.actor.Blueprint.locked=true;return Refuses(a.Open)&&ShowPartySelection.opens==0;});
  Check("pet owner refused before selector open",()=>{var a=new FieldDepartureAdapter();a.actor.Descriptor.Pet=new UnitEntityData();return Refuses(a.Open)&&ShowPartySelection.opens==0;});
  Check("pet actor refused before selector open",()=>{var a=new FieldDepartureAdapter();a.actor.Descriptor.Master.Value=new UnitEntityData();return Refuses(a.Open)&&ShowPartySelection.opens==0;});
  Check("remote alias refused before selector open",()=>{var a=new FieldDepartureAdapter();Game.Instance.Player.RemoteCompanions.Add(new UnitReference{Value=a.actor});return Refuses(a.Open)&&ShowPartySelection.opens==0;});
  Check("source party replacement refused before open",()=>{var a=new FieldDepartureAdapter();Game.Instance.Player.PartyCharacters[1].Value.UniqueId="foreign";return Refuses(a.Open)&&ShowPartySelection.opens==0;});
  Check("existing selector cannot be borrowed",()=>{var a=new FieldDepartureAdapter();Game.Instance.UI.Canvas.GroupManager.gameObject.activeSelf=true;return Refuses(a.Open)&&!a.owned&&ShowPartySelection.opens==0;});
  Check("repeat open does not repeat native input",()=>{var a=new FieldDepartureAdapter();a.Open();return Refuses(a.Open)&&ShowPartySelection.opens==1;});
  Check("partial native open retains custody and drains",()=>{var a=new FieldDepartureAdapter();Game.Instance.UI.Canvas.GroupManager.throwOpen=true;return Refuses(a.Open)&&a.owned&&a.TryDrain()&&!a.owned&&a.group.closes==1;});
  Check("selected party substitution refuses before switch",()=>{var a=new FieldDepartureAdapter();a.Open();a.group.alpha=1;a.group.selected[1]=new UnitReference{Value=new UnitEntityData{UniqueId="foreign"}};return Refuses(()=>a.Tick())&&a.group.switches==0;});
  Check("live party drift refuses before switch",()=>{var a=new FieldDepartureAdapter();a.Open();a.group.alpha=1;Game.Instance.Player.PartyCharacters.RemoveAt(1);return Refuses(()=>a.Tick())&&a.group.switches==0;});
  Check("partial switch is never retried and never commits",()=>{var a=new FieldDepartureAdapter();a.Open();a.group.alpha=1;a.group.throwSwitch=true;var first=Refuses(()=>a.Tick());return first&&Refuses(()=>a.Tick())&&a.group.switches==1&&a.group.goes==0&&a.TryDrain();});
  Check("native refusal to remove blocks Go",()=>{var a=new FieldDepartureAdapter();a.Open();a.group.alpha=1;a.group.keepTarget=true;return Refuses(()=>a.Tick())&&a.group.goes==0&&a.TryDrain();});
  Check("neighbor lost in switch blocks Go",()=>{var a=new FieldDepartureAdapter();a.Open();a.group.alpha=1;a.group.dropNeighbor=true;return Refuses(()=>a.Tick())&&a.group.goes==0&&a.TryDrain();});
  Check("asynchronous Go closure issues no repeated input",()=>{var a=new FieldDepartureAdapter();a.Open();a.group.alpha=1;a.group.waitGo=true;var first=a.Tick();var second=a.Tick();a.group.gameObject.activeSelf=false;return !first&&!second&&a.Tick()&&a.group.switches==1&&a.group.goes==1;});
  Check("partial Go failure retains cleanup without replay",()=>{var a=new FieldDepartureAdapter();a.Open();a.group.alpha=1;a.group.throwGo=true;return Refuses(()=>a.Tick())&&a.owned&&a.TryDrain()&&a.group.goes==1;});
  Check("missing remote actor cannot prove native completion",()=>{var a=new FieldDepartureAdapter();a.Open();a.group.alpha=1;a.group.failRemote=true;return Refuses(()=>a.Tick())&&!a.completed&&a.TryDrain();});
  Check("selector replaced by Go cannot counterfeit completion",()=>{var a=new FieldDepartureAdapter();a.Open();a.group.alpha=1;a.group.replaceGo=true;return Refuses(()=>a.Tick())&&a.owned&&Refuses(()=>a.TryDrain());});
  Check("progress failure still permits owned cleanup guard",()=>{var a=new FieldDepartureAdapter();a.Open();a.blocked=true;return Refuses(()=>a.Tick())&&a.TryDrain()&&a.group.closes==1;});
  Check("cleanup guard blocks native Close",()=>{var a=new FieldDepartureAdapter();a.Open();a.cleanupBlocked=true;return Refuses(()=>a.TryDrain())&&a.owned&&a.group.closes==0;});
  Check("failed Close retains custody for safe retry",()=>{var a=new FieldDepartureAdapter();a.Open();a.group.throwClose=true;var first=Refuses(()=>a.TryDrain());var retained=a.owned;a.group.throwClose=false;return first&&retained&&a.TryDrain()&&a.group.closes==2;});
  Check("completed selector replacement remains fenced",()=>{var a=new FieldDepartureAdapter();a.Open();a.group.alpha=1;a.Tick();Game.Instance.UI.Canvas.GroupManager=new GroupManager();return Refuses(()=>a.TryDrain());});
  Check("reopened completed selector remains fenced",()=>{var a=new FieldDepartureAdapter();a.Open();a.group.alpha=1;a.Tick();a.group.gameObject.activeSelf=true;return Refuses(()=>a.TryDrain())&&a.group.closes==0;});
  Check("unused helper drains without native action",()=>{var a=new FieldDepartureAdapter();return a.TryDrain()&&ShowPartySelection.opens==0;});
  Console.WriteLine("PASS "+pass+" FAIL "+fail);return fail;
 }
}
