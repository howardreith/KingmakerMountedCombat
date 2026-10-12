using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerMountedCombat.Domain;
struct Vector3 { }
namespace Kingmaker.Items { internal sealed class ItemEntity { } }
internal sealed class TargetWrapper { }
internal sealed class UnitEntityData { internal readonly UnitCommands Commands = new UnitCommands(); internal readonly CombatState CombatState = new CombatState(); internal readonly string UniqueId; internal UnitEntityData(string id) { UniqueId = id; } }
internal sealed class CombatState { internal UnitEntityData ManualTarget; internal readonly int[] Resources = { 3, 2, 1, 4 }; }
internal class UnitCommand { internal bool IsFinished, ChargeMode; internal void Interrupt(bool silent = true) { IsFinished = true; } }
internal sealed class UnitMoveTo : UnitCommand { }
// External native command boundary: an admitted move is visible until native interruption;
// native ground dispatch raises OnMoveRequested only after publishing that command.
internal sealed class UnitCommands { internal UnitMoveTo Slot; internal UnitMoveTo Move => Slot != null && !Slot.IsFinished ? Slot : null; internal void Admit(UnitMoveTo command) { Slot = command; } internal void InterruptMove() { Slot?.Interrupt(); Slot = null; } }
internal sealed class UnitView { internal int Stops; internal void StopMoving() { Stops++; } }
internal sealed class SelectionManager { internal static SelectionManager Instance = new SelectionManager(); internal UnitEntityData[] SelectedUnits = new UnitEntityData[0]; }
internal sealed class Runtime {
 private readonly UnitEntityData mount; private readonly UnitView mountView = new UnitView();
 internal Runtime(UnitEntityData value) { mount = value; }
 /* RUNTIME_CANCEL */
}
internal sealed class Relationship { internal readonly UnitEntityData Rider, Mount; internal readonly Runtime Runtime; internal World World; internal readonly object Owner = new object(); internal readonly long Generation = 7; internal Relationship(string name) { Rider=new UnitEntityData(name+"R"); Mount=new UnitEntityData(name+"M"); Runtime=new Runtime(Mount); } }
internal sealed class Pair { internal readonly MountedCombatController Combat; internal Pair(MountedCombatController combat) { Combat=combat; } }
internal sealed class World { internal readonly Pair[] Pairs; internal World(params MountedCombatController[] owners) { Pairs=owners.Select(x=>new Pair(x)).ToArray(); } internal Exception[] Visit(Action<Pair> action) { var errors=new List<Exception>(); foreach(var pair in Pairs) try { action(pair); } catch(Exception e) { errors.Add(e); } return errors.ToArray(); } }
internal enum MountedCombatActionKind { None, RiderPrimary, MountPrimary }
internal sealed class Scheduler { internal int Interrupts; internal void ObserveExternalInterrupt(UnitCommand command,string reason) { Interrupts++; } }
internal sealed class OverlayGuard { internal void Clear() { } }
internal sealed class MountedCombatController {
 internal readonly Relationship relationship;
 internal MountedCombatController(Relationship value) { relationship=value; }
 internal bool disposed, chargeHeld; private object chargeOwner;
 internal string ChargeCleanupStatus => "retained";
 private bool TryDrainChargeOwnership(string reason) { if(chargeHeld) return false; chargeOwner=null; return true; }
 internal void RetainCharge() { chargeOwner=new object(); chargeHeld=true; }
 private readonly MountedAttackIntent<UnitEntityData,object> stockIntent = new MountedAttackIntent<UnitEntityData,object>();
 private UnitEntityData stockIntentTarget => stockIntent.Target;
 internal bool HasStockAttackIntent => stockIntent.Target != null;
 internal int StockAttackIntentCancelCount; internal string LastStockAttackObservation, LastFeedback;
 private UnitCommand stockIntentCommand;
 private UnitEntityData observedStockRequestUnit, observedStockRequestTarget; private int observedStockRequestFrame;
 internal MountedCombatActionKind ArmedAction;
 internal UnitCommand activeCommand, activeDoorInteraction, activeRiderTurnGroundMove;
 private object groundMoveOwner, projectedNativeMountTurnGroundMove; private int projectedNativeMountTurnGroundMoveFrame;
 private bool riderTurnGroundMoveAdmissionPending;
 private readonly Scheduler pairedCommandScheduler = new Scheduler(); private readonly OverlayGuard overlayWorldInputGuard = new OverlayGuard();
 private void SweepFinishedCommand() { }
 internal void Intent(UnitEntityData target) { stockIntent.Begin(target,new object(),false,false); relationship.Rider.CombatState.ManualTarget=target; }
 /* COMBAT_CLEAR_INTENT */
 /* COMBAT_CLEAR_OBSERVED */
 /* COMBAT_CLEAR_GROUND */
 /* COMBAT_CLEAR_PROJECTED */
 /* COMBAT_CANCEL_SELECTED */
 /* COMBAT_CANCEL */
}
internal sealed class NativeMountedControlService {
 private readonly Relationship relationship; private readonly MountedCombatController combat;
 internal NativeMountedControlService(Relationship boundary,MountedCombatController owner) { relationship=boundary;combat=owner; }
 /* VISIT_INPUT_OWNERS */
 /* ON_MOVE */
 /* ON_ITEM */
}
internal static class Program {
 static int pass, fail;
 static void Check(string name,Func<bool> check) { try { if(!check()) throw new Exception("predicate false");pass++;Console.WriteLine("PASS "+name); } catch(Exception e) { fail++;Console.WriteLine("FAIL "+name+": "+e.Message); } }
 sealed class Fixture {
 internal readonly Relationship A=new Relationship("A"), B=new Relationship("B"); internal readonly MountedCombatController CA, CB; internal readonly NativeMountedControlService Controls; internal readonly UnitEntityData Target=new UnitEntityData("target");
 internal Fixture(bool legacy=false) { CA=new MountedCombatController(A); CB=new MountedCombatController(B); if(!legacy){var w=new World(CA,CB);A.World=B.World=w;} Controls=new NativeMountedControlService(A,CA); SelectionManager.Instance.SelectedUnits=new[]{A.Rider}; }
 internal UnitMoveTo Ground(MountedCombatController c) { c.Cancel("ground command"); var command=new UnitMoveTo();c.relationship.Mount.Commands.Admit(command);Controls.OnMoveRequested(new Vector3());return command; }
 internal bool ResourcesUnchanged => new[]{A.Rider,A.Mount,B.Rider,B.Mount}.All(x=>x.CombatState.Resources.SequenceEqual(new[]{3,2,1,4}));
 }
 static int Main() {
 Check("selected A fresh native ground command survives its post-input callback",()=>{var f=new Fixture();var command=f.Ground(f.CA);return ReferenceEquals(f.A.Mount.Commands.Move,command)&&!command.IsFinished;});
 Check("selected B fresh ground command survives exact other-owner routing",()=>{var f=new Fixture();SelectionManager.Instance.SelectedUnits=new[]{f.B.Rider};var command=f.Ground(f.CB);return ReferenceEquals(f.B.Mount.Commands.Move,command)&&!command.IsFinished;});
 Check("ground pre-admission cancels old attack intent before fresh command publication",()=>{var f=new Fixture();f.CA.Intent(f.Target);f.CA.activeCommand=new UnitCommand();var old=f.CA.activeCommand;var command=f.Ground(f.CA);return old.IsFinished&&!f.CA.HasStockAttackIntent&&f.CA.StockAttackIntentCancelCount==1&&!command.IsFinished;});
 Check("ground pre-admission cancels old door before fresh command publication",()=>{var f=new Fixture();f.CA.activeDoorInteraction=new UnitCommand();var old=f.CA.activeDoorInteraction;var command=f.Ground(f.CA);return old.IsFinished&&f.CA.activeDoorInteraction==null&&!command.IsFinished;});
 Check("post-input does not cancel nonselected owner targeting or move",()=>{var f=new Fixture();f.CB.Intent(f.Target);var other=new UnitMoveTo();f.B.Mount.Commands.Admit(other);var command=f.Ground(f.CA);return f.CB.HasStockAttackIntent&&ReferenceEquals(f.B.Mount.Commands.Move,other)&&!other.IsFinished&&!command.IsFinished;});
 Check("selected stock intent still cancels on native move event",()=>{var f=new Fixture();f.CA.Intent(f.Target);f.CA.activeCommand=new UnitCommand();var old=f.CA.activeCommand;f.Controls.OnMoveRequested(new Vector3());return !f.CA.HasStockAttackIntent&&old.IsFinished&&f.CA.StockAttackIntentCancelCount==1;});
 Check("item event without stock intent preserves existing native move",()=>{var f=new Fixture();var command=new UnitMoveTo();f.A.Mount.Commands.Admit(command);f.Controls.OnItemUseRequested(null,null);return ReferenceEquals(f.A.Mount.Commands.Move,command)&&!command.IsFinished;});
 Check("item event cancels exact selected stock intent only",()=>{var f=new Fixture();f.CA.Intent(f.Target);f.CB.Intent(f.Target);f.Controls.OnItemUseRequested(null,null);return !f.CA.HasStockAttackIntent&&f.CB.HasStockAttackIntent&&f.CA.StockAttackIntentCancelCount==1&&f.CB.StockAttackIntentCancelCount==0;});
 Check("no selection retains both independent targeting intents",()=>{var f=new Fixture();SelectionManager.Instance.SelectedUnits=new UnitEntityData[0];f.CA.Intent(f.Target);f.CB.Intent(f.Target);f.Controls.OnMoveRequested(new Vector3());return f.CA.HasStockAttackIntent&&f.CB.HasStockAttackIntent;});
 Check("same-ID replacement selection has no authority",()=>{var f=new Fixture();SelectionManager.Instance.SelectedUnits=new[]{new UnitEntityData(f.A.Rider.UniqueId)};f.CA.Intent(f.Target);f.Controls.OnMoveRequested(new Vector3());return f.CA.HasStockAttackIntent;});
 Check("selected mount exact identity can replace its stock intent",()=>{var f=new Fixture();SelectionManager.Instance.SelectedUnits=new[]{f.A.Mount};f.CA.Intent(f.Target);f.Controls.OnMoveRequested(new Vector3());return !f.CA.HasStockAttackIntent;});
 Check("explicit Stop still cancels selected move without stock intent",()=>{var f=new Fixture();var command=new UnitMoveTo();f.A.Mount.Commands.Admit(command);f.CA.CancelSelectedInput("stop or hold");return command.IsFinished&&f.A.Mount.Commands.Move==null;});
 Check("explicit Stop still cancels door and primary custody",()=>{var f=new Fixture();f.CA.activeDoorInteraction=new UnitCommand();f.CA.activeCommand=new UnitCommand();var door=f.CA.activeDoorInteraction;var attack=f.CA.activeCommand;f.CA.CancelSelectedInput("stop or hold");return door.IsFinished&&attack.IsFinished;});
 Check("nonselected Stop does not cancel another owner's native move",()=>{var f=new Fixture();var command=new UnitMoveTo();f.B.Mount.Commands.Admit(command);f.CB.CancelSelectedInput("stop or hold");return !command.IsFinished&&ReferenceEquals(f.B.Mount.Commands.Move,command);});
 Check("failed charge cleanup retains selected intent and command custody",()=>{var f=new Fixture();f.CA.Intent(f.Target);f.CA.RetainCharge();var command=new UnitMoveTo();f.A.Mount.Commands.Admit(command);bool refused=false;try{f.CA.CancelSelectedInput("stop or hold");}catch(InvalidOperationException){refused=true;}return refused&&f.CA.HasStockAttackIntent&&f.CA.chargeHeld&&!command.IsFinished;});
 Check("multi-selected stock intents each cancel independently",()=>{var f=new Fixture();SelectionManager.Instance.SelectedUnits=new[]{f.A.Rider,f.B.Rider};f.CA.Intent(f.Target);f.CB.Intent(f.Target);f.Controls.OnMoveRequested(new Vector3());return !f.CA.HasStockAttackIntent&&!f.CB.HasStockAttackIntent;});
 Check("failed owner cleanup does not skip independently selected owner",()=>{var f=new Fixture();SelectionManager.Instance.SelectedUnits=new[]{f.A.Rider,f.B.Rider};f.CA.Intent(f.Target);f.CB.Intent(f.Target);f.CA.RetainCharge();f.Controls.OnMoveRequested(new Vector3());return f.CA.HasStockAttackIntent&&f.CA.chargeHeld&&!f.CB.HasStockAttackIntent;});
 Check("legacy one-pair path preserves fresh native ground command",()=>{var f=new Fixture(true);var command=f.Ground(f.CA);return !command.IsFinished&&ReferenceEquals(f.A.Mount.Commands.Move,command);});
 Check("ground/item/Stop preserve actor expenditure and owner identity",()=>{var f=new Fixture();var owner=f.A.Owner;f.Ground(f.CA);f.Controls.OnItemUseRequested(null,null);f.CA.CancelSelectedInput("stop or hold");return f.ResourcesUnchanged&&ReferenceEquals(owner,f.A.Owner)&&f.A.Generation==7&&f.B.Generation==7;});
 Console.WriteLine("TOTAL PASS "+pass+" FAIL "+fail);return fail==0?0:1;
 }
}
