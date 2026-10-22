using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using KingmakerMountedCombat.Diagnostics;
using Unit = Kingmaker.EntitySystem.Entities.UnitEntityData;
using Player = Kingmaker.Player;
using FileInfo = OwnedPairsCustodyCheck.FakeFileInfo;
namespace OwnedPairsCustodyCheck {
 internal static class Program {
  private static int pass, fail;
  private static void Test(string name, Action test) { try { test(); pass++; Console.WriteLine("PASS " + name); } catch (Exception error) { fail++; Console.WriteLine("FAIL " + name + ": " + error.Message); } }
  private static void Assert(bool value) { if (!value) throw new Exception("Expected predicate was false."); }
  private static void Refuses(Action action) { try { action(); } catch (InvalidOperationException) { return; } throw new Exception("Expected exact boundary refusal."); }
  private static int Main() {
   foreach(var checkpoint in new[]{"multi-exploration-manual","multi-exploration-quick","multi-exploration-auto","multi-area-autosave","multi-exploration-three-loads-manual","multi-exploration-three-area-transfers-manual","owned-schema1-resave-manual","owned-schema1-current-manual"}){var selected=checkpoint;Test("actual inherited exploration cold admission "+selected,()=>{var m=new ModeFixture(selected,true);m.Verify(false);Assert(m.Continued==1);Refuses(()=>new ModeFixture(selected,true).Verify(true));});}
   foreach(var checkpoint in new[]{"multi-rt-debt-manual","multi-tb-debt-manual","zero-mounted-tb-retained-manual","legacy-mounted-pending-tb-manual","owned-schema2-pending-current-manual"}){var selected=checkpoint;Test("actual inherited combat cold admission "+selected,()=>{var m=new ModeFixture(selected,true);m.Verify(true);Assert(m.Continued==1);Refuses(()=>new ModeFixture(selected,true).Verify(false));});}
   Test("actual inherited source admission retains exploration mode",()=>{var m=new ModeFixture("multi-tb-debt-manual",false);m.Verify(false);Refuses(()=>new ModeFixture("multi-tb-debt-manual",false).Verify(true));});
   foreach(var marker in new[]{"bound","released"}){var selected=marker;Test("actual inherited admission rejects "+selected+" before continuation",()=>{var m=new ModeFixture("owned-schema1-current-manual",true);m.bound=selected=="bound";m.released=selected=="released";Refuses(()=>m.Verify(false));Assert(m.Continued==0);});}
   Test("historical Working non-transfer area is unchanged", () => Assert(new AreaBoundary(false,false).Area == "working"));
   Test("owned source uses qualified acquired area", () => Assert(new AreaBoundary(true,false).Area == "acquired"));
   Test("owned cold scope uses qualified acquired area", () => { var a=new AreaBoundary(true,false); a.Cold=true; Assert(a.StartScopeArea == "acquired"); });
   Test("explicit native area autosave keeps declared destination", () => Assert(new AreaBoundary(false,true).Area == "destination"));
   Test("native area autosave missing target stays refused/null", () => { var a=new AreaBoundary(false,true); a.request.PersistenceAreaTarget=null; Assert(a.Area==null); });
   Test("unchanged five-actor field and inherited facts pass", () => new Fixture().Verify());
   Test("field enumeration reordering preserves exact membership", () => { var f=new Fixture(); f.game.Player.Party.Reverse(); f.game.Player.PartyCharacters.Reverse(); f.Verify(); });
   Test("unrelated actor removed from live field refuses", () => { var f=new Fixture(); f.game.Player.Party.RemoveAt(2); Refuses(f.Verify); });
   Test("new unrelated field actor refuses", () => { var f=new Fixture(); f.game.Player.Party.Add(Fixture.Actor("foreign")); Refuses(f.Verify); });
   Test("same-ID unrelated replacement refuses", () => { var f=new Fixture(); f.game.Player.Party[2]=Fixture.Actor("unrelated"); Refuses(f.Verify); });
   Test("duplicate native field actor refuses", () => { var f=new Fixture(); f.game.Player.Party[2]=f.game.Player.Party[0]; Refuses(f.Verify); });
   Test("unrelated actor moved Remote refuses", () => { var f=new Fixture(); f.game.Player.RemoteCompanions.Add(new Ref(f.Party[4])); Refuses(f.Verify); });
   Test("inherited rider moved Remote refuses", () => { var f=new Fixture(); f.game.Player.RemoteCompanions.Add(new Ref(f.Actors[0])); Refuses(f.Verify); });
   Test("unrelated native PartyCharacters removal refuses", () => { var f=new Fixture(); f.game.Player.PartyCharacters.RemoveAt(2); Refuses(f.Verify); });
   Test("same-ID PartyCharacters replacement refuses", () => { var f=new Fixture(); f.game.Player.PartyCharacters[2]=new Ref(Fixture.Actor("unrelated")); Refuses(f.Verify); });
   Test("foreign remote companion does not authorize or disturb field", () => { var f=new Fixture(); f.game.Player.RemoteCompanions.Add(new Ref(Fixture.Actor("remote"))); f.Verify(); });
   Test("native death state alone does not broaden roster predicate", () => { var f=new Fixture(); f.Party[4].IsInState=false; f.Party[4].IsInGame=false; f.Verify(); });
   Test("scope and request checks still precede custody", () => { var f=new Fixture(); f.Scope.Refuse=true; Refuses(f.Verify); Assert(f.Scope.Calls==1); });
   Test("request change still refuses", () => { var f=new Fixture(); f.RequestChanged=true; Refuses(f.Verify); });
   Test("authority loss still refuses", () => { var f=new Fixture(); f.AuthorityChanged=true; Refuses(f.Verify); });
   Test("reciprocal pet substitution refuses", () => { var f=new Fixture(); f.Actors[0].Descriptor.Pet=Fixture.Actor("replacement"); Refuses(f.Verify); });
   Test("inherited feature replacement refuses", () => { var f=new Fixture(); f.Actors[0].Descriptor.Facts[f.horses.HorseFeature]=new Feature(); Refuses(f.Verify); });
   Test("native rank mutation refuses", () => { var f=new Fixture(); f.Actors[0].Descriptor.GetFact(f.horses.LevelRank).Rank=5; Refuses(f.Verify); });
   Test("native XP change refuses", () => { var f=new Fixture(); f.Party[4].Descriptor.Progression.Experience++; Refuses(f.Verify); });
   Test("native money change refuses", () => { var f=new Fixture(); f.game.Player.Money++; Refuses(f.Verify); });
   Test("source receipt hash change refuses", () => { var f=new Fixture(); f.CurrentHash="changed"; Refuses(f.Verify); });
   Test("refusal performs no removal or native mutation", () => { var f=new Fixture(); f.game.Player.Party.RemoveAt(2); Refuses(f.Verify); Assert(f.Actors[0].Descriptor.Facts.Count==2 && f.Actors[2].Descriptor.Facts.Count==2 && f.game.Player.Money==10 && f.Actors[0].Descriptor.Progression.Experience==0); });
   Test("native character containers exclude both reciprocal pets", () => { var f=new Fixture(); Assert(f.game.Player.Party.Count==3 && f.game.Player.PartyCharacters.Count==3 && f.Party.Length==5 && f.game.Player.Party.All(actor=>actor.Descriptor.Master.Value==null)); f.Verify(); });
   Test("pet in raw Party refuses even when full actor scope is unchanged", () => { var f=new Fixture(); f.game.Player.Party.Add(f.Actors[1]); Refuses(f.Verify); });
   Test("pet in both native character containers still refuses", () => { var f=new Fixture(); f.game.Player.Party.Add(f.Actors[1]); f.game.Player.PartyCharacters.Add(new Ref(f.Actors[1])); Refuses(f.Verify); });
   Test("missing reciprocal native pet master refuses", () => { var f=new Fixture(); f.Actors[1].Descriptor.Master=new Ref(null); Refuses(f.Verify); });
   Test("same-ID reciprocal pet replacement refuses exact old custody", () => { var f=new Fixture(); var replacement=Fixture.Actor(f.Actors[1].UniqueId); replacement.Descriptor.Master=new Ref(f.Actors[0]); f.Actors[0].Descriptor.Pet=replacement; Refuses(f.Verify); });
   Test("actual eight-actor three-Horse custody passes",()=>new Fixture(true).Verify());
   Test("third inherited Horse replacement refuses",()=>{var f=new Fixture(true);f.Actors[4].Descriptor.Pet=Fixture.Actor("replacement");Refuses(f.Verify);});
   Test("third inherited Horse rank drift refuses",()=>{var f=new Fixture(true);f.Actors[4].Descriptor.GetFact(f.horses.LevelRank).Rank++;Refuses(f.Verify);});
   Test("second unrelated eight-actor progression drift refuses",()=>{var f=new Fixture(true);f.Party[7].Descriptor.Progression.Experience++;Refuses(f.Verify);});
   Console.WriteLine("RESULT: "+pass+" PASS / "+fail+" FAIL"); return fail==0?0:1;
  }
 }
 internal sealed class AreaBoundary {
  internal Request request; internal bool Cold;
  internal AreaBoundary(bool owned,bool area) { request=new Request {Owned=owned,PersistenceCase=area?"multi-area-autosave":"multi-exploration-manual",Fixture=new Fixtures {Working=new Descriptor {Area="working"}},PersistenceLoad=new Descriptor {Area="acquired"},PersistenceAreaTarget=new Descriptor {Area="destination"}}; }
  /*AREA_PROPERTIES*/
  internal string Area => ExpectedArchiveArea;
  // Exact selection expression used by StartNativeWorld; external scope is not constructed.
  internal string StartScopeArea => Cold ? ExpectedArchiveArea : request.SelectedChunk7Fixture.Area;
 }
 internal sealed class ModeFixture {
  internal bool bound,released;internal int Continued;private Selection selection;private Request request;
  internal ModeFixture(string checkpoint,bool cold){selection=new Selection{Cold=cold};request=new Request{PersistenceCase=checkpoint};}
  private static void Require(bool value,string reason){if(!value)throw new InvalidOperationException(reason);}
  internal void Verify(bool combatCold){RequireBindMode(combatCold);Continued++;}
  /*ACTUAL_BIND_MODE*/
 }
 internal sealed class Request { internal bool Owned; internal string PersistenceCase; internal Fixtures Fixture; internal Descriptor PersistenceLoad,PersistenceAreaTarget; internal Descriptor SelectedChunk7Fixture=>Owned?PersistenceLoad:Fixture.Working; internal string ExpectedChunk7ArchiveArea=>PersistenceCase=="multi-area-autosave"?PersistenceAreaTarget?.Area:SelectedChunk7Fixture?.Area; }
 internal sealed class Fixtures { internal Descriptor Working; }
 internal sealed class Descriptor { internal string Area; }
 internal sealed class Fixture {
  private bool bound=true,released=false; internal bool RequestChanged,AuthorityChanged;
  internal readonly Game game; internal Unit[] Party,Actors; private Unit[] partyCharacters;
  private object[] descriptors,views,facts,ranks; private JObject[] ownerRows; private JObject sourceArrangement;
  internal Horses horses=new Horses(); internal Scope Scope=new Scope(); private string sourcePath="receipt"; private long sourceLength=10,sourceTicks=20;
  // This legacy facade selects only two/three inherited pairs. The six-case
  // actual decoder/remote method has its separate pinned-input regression.
  private void RequireSixPairRemote() { }
  private Selection selection=new Selection(); private int InheritedPairCount=>selection.IsFourthHorse?3:2; internal string CurrentHash="exact";
  internal Fixture(bool fourth=false) {
   game=new Game(); Game.Instance=game;
   selection.IsFourthHorse=fourth;
   Party=fourth?new[] {Actor("rider-a"),Actor("mount-a"),Actor("rider-b"),Actor("mount-b"),Actor("rider-c"),Actor("mount-c"),Actor("unrelated"),Actor("other")}:new[] {Actor("rider-a"),Actor("mount-a"),Actor("rider-b"),Actor("mount-b"),Actor("unrelated")}; Actors=Party.Take(fourth?6:4).ToArray();
   partyCharacters=fourth?new[]{Party[0],Party[2],Party[4],Party[6],Party[7]}:new[]{Party[0],Party[2],Party[4]}; game.Player.Party=partyCharacters.ToList(); game.Player.PartyCharacters=partyCharacters.Select(a=>new Ref(a)).ToList();
   descriptors=Actors.Select(a=>(object)a.Descriptor).ToArray();views=Actors.Select(a=>a.View).ToArray(); facts=new object[InheritedPairCount];ranks=new object[InheritedPairCount];ownerRows=new JObject[InheritedPairCount];
   for(var i=0;i<InheritedPairCount;i++){var rider=Actors[i*2];var mount=Actors[i*2+1];mount.Blueprint=horses.HorseUnit;rider.Descriptor.Pet=mount;mount.Descriptor.Master=new Ref(rider);var fact=new Feature {Source=new Blueprint {AssetGuid="source"}};var rank=new Fact {Rank=4};rider.Descriptor.Facts.Add(horses.HorseFeature,fact);rider.Descriptor.Facts.Add(horses.LevelRank,rank);facts[i]=fact;ranks[i]=rank;ownerRows[i]=new JObject{["sourceGuid"]="source",["rank"]=4};}
   sourceArrangement=Arrangement();
  }
  internal static Unit Actor(string id)=>new Unit {UniqueId=id,Blueprint=new Blueprint {AssetGuid="unit"}};
  internal void Verify()=>RequirePreserved();
  private void RequireRequest(){Require(!RequestChanged,"request changed");} private void RequireAuthority(){Require(!AuthorityChanged,"authority changed");}
  private string Hash(string path)=>CurrentHash;
  private static void Require(bool value,string reason){if(!value)throw new InvalidOperationException(reason);}
  /*ACTUAL_REQUIRE*/
  /*ACTUAL_ARRANGEMENT*/
 }
 internal sealed class Selection {internal string SourceArtifactSha256="exact";internal bool Cold,IsFourthHorse;internal bool RequiresCombatColdBind(string checkpoint)=>Cold&&checkpoint!="multi-exploration-manual"&&checkpoint!="multi-exploration-quick"&&checkpoint!="multi-exploration-auto"&&checkpoint!="multi-area-autosave"&&checkpoint!="multi-exploration-three-loads-manual"&&checkpoint!="multi-exploration-three-area-transfers-manual"&&checkpoint!="owned-schema1-resave-manual"&&checkpoint!="owned-schema1-current-manual";}
 internal sealed class Scope {internal int Calls;internal bool Refuse;internal void RequireCurrent(){Calls++;if(Refuse)throw new InvalidOperationException("world changed");}}
 internal sealed class Game {internal static Game Instance;internal Player Player=new Player();}
 internal sealed class Ref {internal Unit Value;internal Ref(Unit value){Value=value;}}
 internal sealed class UnitDescriptor {internal Unit Pet;internal Ref Master=new Ref(null);internal Dictionary<object,Fact> Facts=new Dictionary<object,Fact>();internal Progression Progression=new Progression();internal Fact GetFact(object key){Fact value;return Facts.TryGetValue(key,out value)?value:null;}}
 internal class Fact {internal int Rank;internal int GetRank()=>Rank;}
 internal sealed class Feature:Fact {internal Blueprint Source;}
 internal sealed class Progression {internal int Experience,CharacterLevel=4;internal List<ClassRow> Classes=new List<ClassRow>{new ClassRow {CharacterClass=new Blueprint {AssetGuid="ranger"},Level=4}};}
 internal sealed class ClassRow {internal Blueprint CharacterClass;internal int Level;}
 internal sealed class Blueprint {internal string AssetGuid;}
 internal sealed class Horses {internal object HorseFeature=new object(),LevelRank=new object();internal Blueprint HorseUnit=new Blueprint {AssetGuid="horse"};}
 internal sealed class FakeFileInfo {internal FakeFileInfo(string path){} internal bool Exists=>true;internal long Length=>10;internal DateTime LastWriteTimeUtc=>new DateTime(20);}
}
namespace KingmakerMountedCombat.Diagnostics {
 internal static class Chunk7OwnedPairsSelection {
  internal static bool RepeatedCheckpoint(string value)=>value=="multi-exploration-three-loads-manual"||value=="multi-exploration-three-area-transfers-manual";
  internal static bool OwnedSchema1Cold(string value)=>value=="owned-schema1-resave-manual";
 }
}

// Only native data containers are substituted. The production roster helper is compiled unchanged.
namespace Kingmaker { internal sealed class Player {internal long Money=10;internal int ExperienceRatePercent=100;internal List<Unit> Party=new List<Unit>();internal List<OwnedPairsCustodyCheck.Ref> PartyCharacters=new List<OwnedPairsCustodyCheck.Ref>(),RemoteCompanions=new List<OwnedPairsCustodyCheck.Ref>();} }
namespace Kingmaker.EntitySystem.Entities { internal sealed class UnitEntityData {internal string UniqueId;internal OwnedPairsCustodyCheck.Blueprint Blueprint;internal OwnedPairsCustodyCheck.UnitDescriptor Descriptor=new OwnedPairsCustodyCheck.UnitDescriptor();internal object View=new object();internal bool IsInState=true,IsInGame=true;} }
