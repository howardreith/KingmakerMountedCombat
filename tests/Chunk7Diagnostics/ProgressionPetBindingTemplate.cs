using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
internal struct UnitReference { internal UnitEntityData Value; }
internal sealed class Features { internal readonly List<AddPet> Items=new List<AddPet>(); internal IEnumerable<T> SelectFactComponents<T>() { return Items.OfType<T>(); } }
internal sealed class Progression { internal readonly Features Features=new Features(); }
internal sealed class UnitDescriptor { internal UnitEntityData Unit,Pet; internal UnitReference Master; internal readonly Progression Progression=new Progression(); }
internal sealed class NativeCooldown { internal float MoveAction=.25f, StandardAction=3, SwiftAction=.5f; }
internal sealed class NativeCombatState { internal readonly NativeCooldown Cooldown=new NativeCooldown(); internal bool Prepared=true; internal int AttackOfOpportunityCount=2,AttackOfOpportunityPerRound=5; internal float Initiative=7; }
internal sealed class UnitEntityData { internal readonly NativeCombatState CombatState=new NativeCombatState(); internal string UniqueId=Guid.NewGuid().ToString(); internal UnitDescriptor Descriptor; internal object View=new object(); internal bool ShouldBeDestroyed,Destroyed; internal UnitEntityData() { Descriptor=new UnitDescriptor { Unit=this }; } }
internal sealed class AddPet { internal UnitDescriptor Owner; internal UnitEntityData SpawnedPet; }
internal sealed class JObject:Dictionary<string,object> { }
internal static class Time { internal static int frameCount=100; }
internal sealed class NativeUi { internal object CharacterBuildController=new object(); }
internal sealed class Game { internal static Game Instance=new Game(); internal NativeUi UI=new NativeUi(); }
internal enum Chunk7NativeProgressionCase { ExistingHorseLevel, RespecHorse, RespecMammoth, DismissCustomCompanion }
internal sealed class Probe {
 internal bool Complete; internal int RebindCalls,PetCalls; internal Action PetCheck;
 internal bool TryRequireNativeRebind(object builder) { RebindCalls++; return Complete; }
 internal void RequireObservedPetBinding(UnitEntityData pet,object descriptor,object view) { PetCalls++; PetCheck?.Invoke(); }
}
internal sealed class UiOwner { internal Probe RespecProbe; internal bool NativeDismissalObserved; }
internal sealed class Subject {
 internal readonly UnitEntityData rider=new UnitEntityData(),oldMount=new UnitEntityData();
 internal object originalDescriptor,originalView,originalMountDescriptor,originalMountView;
 internal UiOwner nativeUi; internal Chunk7NativeProgressionCase selectedCase; internal int WorldChecks,InputCalls;internal bool started=true,Busy;internal object requestedManual;
 private readonly Stopwatch elapsed=Stopwatch.StartNew();
 internal bool IsRespec=>selectedCase==Chunk7NativeProgressionCase.RespecHorse||selectedCase==Chunk7NativeProgressionCase.RespecMammoth;
 internal Subject(Chunk7NativeProgressionCase value) {selectedCase=value;rider.Descriptor.Pet=oldMount;oldMount.Descriptor.Master=new UnitReference { Value=rider };originalDescriptor=rider.Descriptor;originalView=rider.View;originalMountDescriptor=oldMount.Descriptor;originalMountView=oldMount.View;}
 internal void Check()=>AssertTargetBinding();
 private static void Require(bool value,string error){if(!value)throw new InvalidOperationException(error);}
 private void AssertWorld(){WorldChecks++;} private bool NativeBusy()=>Busy;
 internal void TickAdmission() { @TICK@ InputCalls++; }
 @BINDING@
}
internal sealed class PetSubject {
 internal readonly UnitEntityData original,originalPet;
 internal readonly UnitDescriptor originalPetDescriptor;
 internal readonly object originalPetView;
 internal readonly string originalPetId;
 internal UnitEntityData observedPet,reboundPet;
 internal UnitDescriptor observedPetDescriptor,reboundPetDescriptor;
 internal object observedPetView,reboundPetView;
 internal string observedPetId,reboundPetId;
 internal AddPet observedPetSource,petUpdateSource;
 internal UnitDescriptor petUpdateOwner;
 internal bool originalPetRemoved,originalPetViewReleased;
 internal int petUpdates,callbackFrame,entries=1,exits=1,previews=1,rebinds;
 internal readonly List<string> errors=new List<string>();
 internal readonly AddPet source;
 internal readonly List<string> records=new List<string>();
 internal PetSubject() {original=new UnitEntityData();originalPet=new UnitEntityData();original.Descriptor.Pet=originalPet;originalPet.Descriptor.Master=new UnitReference {Value=original};originalPetDescriptor=originalPet.Descriptor;originalPetView=originalPet.View;originalPetId=originalPet.UniqueId;source=new AddPet {Owner=original.Descriptor};original.Descriptor.Progression.Features.Items.Add(source);}
 internal void Begin()=>BeginPetUpdate(source);
 internal void End()=>CompletePetUpdate(source);
 internal void Bind(){PinReboundPet();rebinds=1;}
 internal void Check()=>RequireCurrentPetBinding();
 internal UnitEntityData NativeResult() {var pet=new UnitEntityData();pet.Descriptor.Master=new UnitReference {Value=original};source.SpawnedPet=pet;original.Descriptor.Pet=pet;return pet;}
 internal void NativeRemoveOld(){originalPet.Descriptor.Master=new UnitReference();originalPet.ShouldBeDestroyed=true;}
 internal UnitEntityData Replace(){Begin();var pet=NativeResult();End();NativeRemoveOld();Bind();return pet;}
 private void Record(string kind,JObject detail){records.Add(kind);}
 private static int Id(object value)=>value==null?0:value.GetHashCode();
 private static void Require(bool value,string error){if(!value)throw new InvalidOperationException(error);}
 @PROBE@
 @RESOURCES@
 internal JObject ResourcesNow()=>Resources(original.Descriptor);
}
internal static class Test {
 static int pass,fail;
 static void Check(bool value,string label){if(value){pass++;Console.WriteLine("PASS "+label);}else{fail++;Console.WriteLine("FAIL "+label);}}
 static bool Refuses(Action action){try{action();return false;}catch(InvalidOperationException){return true;}}
 static PetSubject Bound(){var s=new PetSubject();s.Replace();return s;}
 static int Main(){
 foreach(var kind in new[]{Chunk7NativeProgressionCase.RespecHorse,Chunk7NativeProgressionCase.RespecMammoth}) {
 var baseline=new Subject(kind);Check(!Refuses(baseline.Check),kind+" accepts original target");
 var mountDescriptor=new Subject(kind);mountDescriptor.oldMount.Descriptor=new UnitDescriptor();Check(Refuses(mountDescriptor.Check),kind+" refuses unobserved original-mount descriptor replacement");
 var mountView=new Subject(kind);mountView.oldMount.View=new object();Check(Refuses(mountView.Check),kind+" refuses unobserved original-mount view replacement");
 var riderDescriptor=new Subject(kind);riderDescriptor.rider.Descriptor=new UnitDescriptor();Check(Refuses(riderDescriptor.Check),kind+" refuses unobserved rider descriptor replacement");
 var lostPet=new Subject(kind);lostPet.rider.Descriptor.Pet=null;Check(Refuses(lostPet.Check),kind+" refuses early pet detachment");
 var lostMaster=new Subject(kind);lostMaster.oldMount.Descriptor.Master=new UnitReference();Check(Refuses(lostMaster.Check),kind+" refuses early master detachment");
 var tick=new Subject(kind);tick.oldMount.View=new object();Check(Refuses(tick.TickAdmission)&&tick.WorldChecks==1&&tick.InputCalls==0,kind+" actual Tick admission refuses before UI input");
 var observed=new Subject(kind);var probe=new Probe{Complete=true};observed.nativeUi=new UiOwner{RespecProbe=probe};Check(!Refuses(observed.Check)&&probe.RebindCalls==1&&probe.PetCalls==1,kind+" complete observer still validates pet when rider descriptor was retained");
 probe.PetCheck=()=>{throw new InvalidOperationException();};Check(Refuses(observed.Check),kind+" observer pet refusal cannot authorize input");
 }
 var level=new Subject(Chunk7NativeProgressionCase.ExistingHorseLevel);level.oldMount.View=new object();Check(Refuses(level.Check),"existing-level path rejects same mount view substitution");
 var pending=new Subject(Chunk7NativeProgressionCase.RespecHorse){Busy=true};pending.TickAdmission();Check(pending.InputCalls==0,"busy native boundary still prevents input");
 var p=Bound();Check(!Refuses(p.Check)&&p.petUpdates==1&&p.records.Count==1,"actual native source exit permits exact replacement and reciprocal binding");
 var descriptor=Bound();descriptor.originalPet.Descriptor=new UnitDescriptor();Check(Refuses(descriptor.Check),"old pet descriptor remains pinned after native replacement");
 var view=Bound();view.originalPet.View=new object();Check(Refuses(view.Check),"old pet cannot acquire another view after native replacement");
 var drain=Bound();drain.originalPet.View=null;Check(!Refuses(drain.Check)&&drain.originalPetViewReleased,"observed detached native destruction permits view-to-null drain");drain.originalPet.View=drain.originalPetView;Check(Refuses(drain.Check),"released old pet view cannot revive");
 var master=Bound();master.originalPet.Descriptor.Master=new UnitReference{Value=master.original};Check(Refuses(master.Check),"removed old pet cannot regain master");
 var nativeFlag=Bound();nativeFlag.originalPet.ShouldBeDestroyed=false;Check(Refuses(nativeFlag.Check),"old pet removal requires retained native destruction state");
 var destroyed=Bound();destroyed.originalPet.ShouldBeDestroyed=false;destroyed.originalPet.Destroyed=true;destroyed.originalPet.View=null;Check(!Refuses(destroyed.Check),"settled native Destroyed permits final null view");
 var newDescriptor=Bound();newDescriptor.reboundPet.Descriptor=new UnitDescriptor();Check(Refuses(newDescriptor.Check),"new pet descriptor remains pinned");
 var newView=Bound();newView.reboundPet.View=new object();Check(Refuses(newView.Check),"new pet view remains pinned");
 var newId=Bound();newId.reboundPet.UniqueId="changed";Check(Refuses(newId.Check),"new pet stable ID remains pinned");
 var newMaster=Bound();newMaster.reboundPet.Descriptor.Master=new UnitReference();Check(Refuses(newMaster.Check),"new pet reciprocal master remains pinned");
 var swapped=Bound();swapped.original.Descriptor.Pet=new UnitEntityData();Check(Refuses(swapped.Check),"unobserved pet object substitution is refused");
 var removedSource=Bound();removedSource.original.Descriptor.Progression.Features.Items.Clear();Check(Refuses(removedSource.Check),"new pet must retain exact native source fact component");
 var foreign=new PetSubject();foreign.source.Owner=new UnitDescriptor();Check(Refuses(foreign.Begin)&&foreign.petUpdateSource==null,"foreign AddPet owner rejected before admission");
 var unknown=new PetSubject();unknown.original.Descriptor.Pet=new UnitEntityData();Check(Refuses(unknown.Begin)&&unknown.petUpdateSource==null,"unobserved pet before native source call rejected");
 var orphan=new PetSubject();orphan.original.Descriptor.Progression.Features.Items.Clear();Check(Refuses(orphan.Begin),"unowned AddPet fact rejected before admission");
 var mismatch=new PetSubject();mismatch.Begin();mismatch.NativeResult();mismatch.source.Owner=new UnitDescriptor();Check(Refuses(mismatch.End)&&mismatch.petUpdateSource!=null,"changed native source owner retains unfinished observation");
 var wrongResult=new PetSubject();wrongResult.Begin();wrongResult.NativeResult();wrongResult.original.Descriptor.Pet=null;Check(Refuses(wrongResult.End),"source result must equal actual actor pet");
 var noObserver=new PetSubject();noObserver.NativeResult();noObserver.NativeRemoveOld();Check(Refuses(noObserver.Bind)&&noObserver.rebinds==0,"rebind cannot admit a new pet without observed native source");
 var noDestroy=new PetSubject();noDestroy.original.Descriptor.Pet=null;noDestroy.originalPet.Descriptor.Master=new UnitReference();Check(Refuses(noDestroy.Bind),"rebind cannot treat arbitrary detach as native removal");
 var nullResult=new PetSubject();nullResult.original.Descriptor.Pet=null;nullResult.NativeRemoveOld();nullResult.Bind();Check(!Refuses(nullResult.Check),"observed rebind may pin an exact null pet after native removal");
 nullResult.Begin();var latePet=nullResult.NativeResult();nullResult.End();Check(!Refuses(nullResult.Check)&&ReferenceEquals(nullResult.reboundPet,latePet),"later actual native AddPet may fill a pinned null result");
 var noOp=new PetSubject();noOp.Begin();noOp.End();Check(noOp.observedPet==null&&ReferenceEquals(noOp.original.Descriptor.Pet,noOp.originalPet),"native no-op cannot grant a pet but preserves original binding");
 var reboundNoOp=Bound();reboundNoOp.Begin();reboundNoOp.source.SpawnedPet=null;Check(Refuses(reboundNoOp.End),"lost non-null source cannot be disguised as a native no-op");
 var emptyNoOp=new PetSubject();emptyNoOp.original.Descriptor.Pet=null;emptyNoOp.NativeRemoveOld();emptyNoOp.Bind();emptyNoOp.Begin();emptyNoOp.End();Check(!Refuses(emptyNoOp.Check)&&emptyNoOp.observedPet==null,"native no-op preserves an observed null result");
 var repeat=Bound();repeat.Begin();repeat.End();Check(!Refuses(repeat.Check)&&repeat.petUpdates==2,"repeat native update preserves exact sourced result");
 var repeatSubstitution=Bound();repeatSubstitution.Begin();repeatSubstitution.NativeResult();Check(Refuses(repeatSubstitution.End),"repeat native update cannot replace an observed pet");
 var kept=new PetSubject();kept.Bind();Check(!Refuses(kept.Check)&&!kept.originalPetRemoved,"native rebind may retain original reciprocal pet");
 var expected=Bound();Check(Refuses(()=>expected.RequireObservedPetBinding(new UnitEntityData(),expected.originalPetDescriptor,expected.originalPetView)),"session cannot borrow another probe pet binding");
 var resource=new PetSubject();var receipt=resource.ResourcesNow();Check((int)receipt["reactionAllowance"]==5,"native reaction allowance uses PerRound instead of Count");Check((int)receipt["reactions"]==2,"spent native reaction count remains independently observed");Check(resource.original.CombatState.AttackOfOpportunityCount==2&&resource.original.CombatState.AttackOfOpportunityPerRound==5&&resource.original.CombatState.Cooldown.StandardAction==3,"resource observation writes no native state");
 Console.WriteLine("ACTUAL RESPEC BINDING PASS "+pass+" FAIL "+fail);return fail==0?0:1;
 }
}
