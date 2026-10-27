using System;
using System.Collections.Generic;
using System.Linq;
namespace UnityModManagerNet { internal static class UnityModManager { internal sealed class UI {internal static UI Instance;internal bool Opened;} } }
namespace KingmakerMountedCombat.Diagnostics {
 internal sealed class JsonNode { internal JsonNode this[string key] { get { return this; } set { } } public static implicit operator JsonNode(bool value){return new JsonNode();} public static implicit operator JsonNode(int value){return new JsonNode();} }
 internal sealed class Actor { internal readonly object View = new object(); }
 internal sealed class Player { internal bool IsInCombat; }
 internal enum GameModeType { Dialog }
 internal sealed class Game { internal Player Player = new Player(); internal bool CutsceneLock, Dialog; private bool pause; internal int PauseWrites; internal bool IsPaused { get { return pause; } set { PauseWrites++;pause=value;} } internal bool IsModeActive(GameModeType mode){return Dialog;} internal void SetPause(bool v){pause=v;} }
 internal sealed class DialogMessageBox { internal static DialogMessageBox Instance; internal bool IsShown; }
 internal static class NativePersistenceIsolation { internal static bool NativeSlotRotationEnabled; internal static int Disables; internal static void DisableNativeSlotRotation(){Disables++;NativeSlotRotationEnabled=false;} }
 internal sealed class SelectionManager { internal static SelectionManager Instance;internal Actor[] All;internal Actor[] SelectedUnits;internal int Writes;internal void MultiSelect(IEnumerable<object> views,bool additive){Writes++;SelectedUnits=views.Select(v=>All.Single(a=>ReferenceEquals(a.View,v))).ToArray();} }
 internal sealed class Lease:IDisposable { internal int Calls;internal bool FailOnce;public void Dispose(){Calls++;if(FailOnce){FailOnce=false;throw new InvalidOperationException("lease-unpatch-failed");}} }
 internal sealed class Actual {
  private Game game=new Game();private bool presentationCaptured,originalPause,nativeBusy,foreignWorld,owners;
  private UnityModManagerNet.UnityModManager.UI ummInstance;private bool originalUmmOpened;
  private Actor[] originalSelection;private IDisposable ummLease;private JsonNode evidence=new JsonNode();
  private bool Busy(){return nativeBusy;} private void RequireAuthority(bool cleanup){Require(!foreignWorld,"foreign-world");}private void RequireNoOwners(){Require(!owners,"owned-native-work");}
  private static void Require(bool v,string r){if(!v)throw new InvalidOperationException(r);}
@@CLEANUP_GUARD@@
@@PRESENTATION@@
  private static int pass,fail;
  private static Actual Fresh(){NativePersistenceIsolation.NativeSlotRotationEnabled=false;NativePersistenceIsolation.Disables=0;DialogMessageBox.Instance=new DialogMessageBox();var a=new Actual{presentationCaptured=true,originalPause=false,originalSelection=new[]{new Actor()},ummLease=new Lease()};a.game.SetPause(true);a.ummInstance=UnityModManagerNet.UnityModManager.UI.Instance=new UnityModManagerNet.UnityModManager.UI();a.originalUmmOpened=false;
   SelectionManager.Instance=new SelectionManager{All=a.originalSelection,SelectedUnits=new Actor[0]};return a;}
  private static void Check(bool ok,string n){if(ok){pass++;Console.WriteLine("PASS "+n);}else{fail++;Console.WriteLine("FAIL "+n);}}
  private static bool Refuses(Action a){try{a();return false;}catch(InvalidOperationException){return true;}}
  internal static int Run(){
   var x=Fresh();DialogMessageBox.Instance.IsShown=true;var l=(Lease)x.ummLease;
   Check(Refuses(x.RestoreAcquisitionPresentation),"foreign popup refuses final presentation");
   Check(NativePersistenceIsolation.Disables==0&&!NativePersistenceIsolation.NativeSlotRotationEnabled&&SelectionManager.Instance.Writes==0&&x.game.PauseWrites==0&&l.Calls==0,"foreign popup preserves absent projection and retains selection pause and lease");
   DialogMessageBox.Instance.IsShown=false;x.RestoreAcquisitionPresentation();
   Check(NativePersistenceIsolation.Disables==0&&!NativePersistenceIsolation.NativeSlotRotationEnabled&&SelectionManager.Instance.Writes==1&&x.game.PauseWrites==1&&!x.game.IsPaused&&l.Calls==1,"exact popup settlement resumes owned Manual cleanup once without projection mutation");
   foreach(var kind in new[]{"worker","world","owner","combat","cutscene","dialog"}){x=Fresh();if(kind=="worker")x.nativeBusy=true;if(kind=="world")x.foreignWorld=true;if(kind=="owner")x.owners=true;if(kind=="combat")x.game.Player.IsInCombat=true;if(kind=="cutscene")x.game.CutsceneLock=true;if(kind=="dialog")x.game.Dialog=true;Check(Refuses(x.RestoreAcquisitionPresentation)&&NativePersistenceIsolation.Disables==0&&SelectionManager.Instance.Writes==0&&x.game.PauseWrites==0,"refuses before presentation "+kind);}
   x=Fresh();x.presentationCaptured=false;DialogMessageBox.Instance.IsShown=true;x.RestoreAcquisitionPresentation();Check(NativePersistenceIsolation.Disables==0&&SelectionManager.Instance.Writes==0&&x.game.PauseWrites==0,"unacquired presentation has no effects");
   x=Fresh();l=(Lease)x.ummLease;l.FailOnce=true;Check(Refuses(x.RestoreAcquisitionPresentation)&&ReferenceEquals(x.ummLease,l)&&x.game.PauseWrites==0,"failed lease retains exact field before unpause");x.RestoreAcquisitionPresentation();Check(l.Calls==2&&x.ummLease==null&&x.game.PauseWrites==1,"failed lease retries exact retained owner");
   x=Fresh();UnityModManagerNet.UnityModManager.UI.Instance=new UnityModManagerNet.UnityModManager.UI();Check(Refuses(x.RestoreAcquisitionPresentation)&&NativePersistenceIsolation.Disables==0&&SelectionManager.Instance.Writes==0&&x.game.PauseWrites==0,"replaced UMM refuses before presentation mutations");
   x=Fresh();x.ummInstance.Opened=true;Check(Refuses(x.RestoreAcquisitionPresentation)&&x.ummLease!=null&&x.game.PauseWrites==0,"disposed lease does not prove changed UMM open state");Check(Refuses(x.RestoreAcquisitionPresentation)&&x.ummLease!=null&&x.game.PauseWrites==0,"no-op lease retry retains unrestored UMM state");x.ummInstance.Opened=false;x.RestoreAcquisitionPresentation();Check(x.ummLease==null&&x.game.PauseWrites==1,"exact observed UMM restoration permits final settlement");
   Console.WriteLine("TOTAL PASS="+pass+" FAIL="+fail);return fail==0?0:1;
  }
 }
 internal static class Program { private static int Main(){return Actual.Run();} }
}
