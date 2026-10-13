using System;
using System.Linq;
using System.Collections.Generic;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using KingmakerMountedCombat.Diagnostics;
namespace Kingmaker { internal sealed class Game { internal Player Player; } internal sealed class Player { internal UnitEntityData[] Party; internal List<UnitReference> PartyCharacters=new List<UnitReference>(); internal List<UnitReference> RemoteCompanions=new List<UnitReference>(); } }
namespace Kingmaker.EntitySystem.Entities
{
 internal sealed class UnitReference { internal UnitEntityData Value; }
 internal sealed class NativeDescriptor { internal UnitEntityData Pet; internal UnitReference Master=new UnitReference(); }
 internal sealed class NativeCommands { internal bool Empty=true; }
 internal sealed class UnitEntityData { internal string UniqueId; internal NativeDescriptor Descriptor=new NativeDescriptor(); internal NativeCommands Commands=new NativeCommands(); internal bool IsInCombat=false, IsDirectlyControllable=true; internal bool Alive=true, ViewPresent=true; }
}
namespace KingmakerMountedCombat.Tests
{
 internal sealed class FieldSubject
 {
  internal Game game; internal UnitEntityData acquiredActor; internal string[] originalCharacters; internal bool sourcePaid; internal UnitEntityData[] owned;
  private UnitEntityData[] OwnedActors()=>owned;
  private void RequireAcquiredOwner(){} // External owner/fact binding is exercised by existing adapter tests.
  private static void Require(bool valid,string reason){if(!valid)throw new InvalidOperationException(reason);}
  internal void Check(){
__ACTUAL_FIELD_PREDICATE__
  }
 }
 internal static class NativeFieldRosterTests
 {
  private static int pass,fail;
  private static void Check(string name,Action body,bool reject=false){try{body();if(reject)throw new Exception("unexpected acceptance");pass++;Console.WriteLine("PASS "+name);}catch(Exception error){if(reject&&error.Message!="unexpected acceptance"){pass++;Console.WriteLine("PASS refuses "+name);}else{fail++;Console.WriteLine("FAIL "+name+": "+error.Message);}}}
  private static UnitEntityData Actor(int id)=>new UnitEntityData{UniqueId=id.ToString("D4")};
  private static FieldSubject Fixture(int characters,bool paid)
  {
   var raw=Enumerable.Range(1,characters).Select(Actor).ToArray();var all=new List<UnitEntityData>(raw);
   foreach(var actor in raw){var companion=Actor(100+int.Parse(actor.UniqueId));actor.Descriptor.Pet=companion;companion.Descriptor.Master.Value=actor;all.Add(companion);}
   var player=new Player{Party=raw};player.PartyCharacters.AddRange(raw.Select(value=>new UnitReference{Value=value}));
   return new FieldSubject{game=new Game{Player=player},acquiredActor=raw.Last(),originalCharacters=raw.Take(characters-(paid?1:0)).Select(value=>value.UniqueId).ToArray(),sourcePaid=paid,owned=all.ToArray()};
  }
  private static void Scope(FieldSubject f){var scope=Chunk7NativeFieldRoster.Capture(f.game.Player);if(scope.Length!=f.owned.Length||!f.owned.All(value=>scope.Count(other=>ReferenceEquals(other,value))==1))throw new Exception("actor scope changed");}
  public static int Main()
  {
   Check("actual existing acquired field accepts character Party with separate pet",()=>Fixture(1,false).Check());
   Check("actual paid cumulative field accepts two character owners and separate pets",()=>Fixture(2,true).Check());
   Check("actual field refuses unrelated raw character",()=>{var f=Fixture(2,true);f.game.Player.Party=f.game.Player.Party.Concat(new[]{Actor(9)}).ToArray();f.Check();},true);
   Check("actual field refuses pet contaminating raw Party",()=>{var f=Fixture(2,true);f.game.Player.Party=f.owned;f.Check();},true);
   Check("actual field keeps acquired owner out of remote roster",()=>{var f=Fixture(2,true);f.game.Player.RemoteCompanions.Add(new UnitReference{Value=f.acquiredActor});f.Check();},true);
   Check("actual field refuses changed original character",()=>{var f=Fixture(2,true);f.originalCharacters=new[]{"foreign"};f.Check();},true);
   Check("actual field refuses busy pet",()=>{var f=Fixture(2,true);f.acquiredActor.Descriptor.Pet.Commands.Empty=false;f.Check();},true);
   Check("actual field refuses pet combat",()=>{var f=Fixture(2,true);f.acquiredActor.Descriptor.Pet.IsInCombat=true;f.Check();},true);
   Check("helper captures all two owners and reciprocal pets",()=>Scope(Fixture(2,false)));
   Check("helper handles bounded six characters and twelve actors offline",()=>Scope(Fixture(6,false)));
   Check("helper retains life/view readiness responsibility with caller",()=>{var f=Fixture(2,false);var p=f.acquiredActor.Descriptor.Pet;p.Alive=false;p.ViewPresent=false;Scope(f);});
   Check("helper permits native character with no pet",()=>{var f=Fixture(2,false);var p=f.acquiredActor.Descriptor.Pet;f.acquiredActor.Descriptor.Pet=null;f.owned=f.owned.Where(v=>!ReferenceEquals(v,p)).ToArray();Scope(f);});
   Check("helper permits raw character order change",()=>{var f=Fixture(2,false);Array.Reverse(f.game.Player.Party);Scope(f);});
   Check("helper refuses duplicate raw character",()=>{var f=Fixture(2,false);f.game.Player.Party=new[]{f.game.Player.Party[0],f.game.Player.Party[0]};Scope(f);},true);
   Check("helper refuses duplicate PartyCharacters",()=>{var f=Fixture(2,false);f.game.Player.PartyCharacters[1]=f.game.Player.PartyCharacters[0];Scope(f);},true);
   Check("helper refuses raw same-ID replacement",()=>{var f=Fixture(2,false);f.game.Player.Party[1]=Actor(2);Scope(f);},true);
   Check("helper refuses declared same-ID replacement",()=>{var f=Fixture(2,false);f.game.Player.PartyCharacters[1]=new UnitReference{Value=Actor(2)};Scope(f);},true);
   Check("helper refuses null raw character",()=>{var f=Fixture(2,false);f.game.Player.Party[1]=null;Scope(f);},true);
   Check("helper refuses foreign master",()=>{var f=Fixture(2,false);f.acquiredActor.Descriptor.Pet.Descriptor.Master.Value=Actor(9);Scope(f);},true);
   Check("helper refuses missing master",()=>{var f=Fixture(2,false);f.acquiredActor.Descriptor.Pet.Descriptor.Master.Value=null;Scope(f);},true);
   Check("helper refuses pet ID alias",()=>{var f=Fixture(2,false);f.acquiredActor.Descriptor.Pet.UniqueId=f.acquiredActor.UniqueId;Scope(f);},true);
   Check("helper refuses shared pet",()=>{var f=Fixture(2,false);f.acquiredActor.Descriptor.Pet=f.game.Player.Party[0].Descriptor.Pet;Scope(f);},true);
   Check("helper refuses character acting as pet",()=>{var f=Fixture(2,false);f.acquiredActor.Descriptor.Master.Value=f.game.Player.Party[0];Scope(f);},true);
   Check("helper refuses empty field",()=>{var p=new Player{Party=new UnitEntityData[0]};Chunk7NativeFieldRoster.Capture(p);},true);
   Check("helper refuses seventh character",()=>Scope(Fixture(7,false)),true);
   Check("helper refuses null player",()=>Chunk7NativeFieldRoster.Capture((Player)null),true);
   Console.WriteLine("RESULT "+pass+" PASS / "+fail+" FAIL");return fail==0?0:1;
  }
 }
}
