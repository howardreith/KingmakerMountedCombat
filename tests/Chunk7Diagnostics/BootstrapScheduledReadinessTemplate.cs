using System;
// Only the exact Update entry/readiness decision and its catch are extracted.
// Native reads, the original authority/probe/world boundaries and cleanup are
// controlled external facades. No game, artifact, archive or save is invoked.
namespace KingmakerMountedCombat.Diagnostics {
 enum GameModeType { None, Default, Dialog }
 sealed class NativeStart { internal bool IsScheduled;internal object Dialog; }
 sealed class NativeDialogState { internal NativeStart Scheduled; }
 sealed class NativeView { internal object AgentASP=new object(); }
 sealed class NativeUnit { internal object Blueprint;internal NativeView View=new NativeView(); }
 sealed class UnitReference { internal NativeUnit Value; }
 sealed class Player { internal string GameId="native-game";internal UnitReference MainCharacter=new UnitReference();internal NativeDialogState State=new NativeDialogState();internal bool ThrowDialog;internal int DialogReads;internal NativeDialogState Dialog {get{DialogReads++;if(ThrowDialog)throw new InvalidOperationException("native dialog read failed");return State;}} }
 sealed class NativeGame { internal object CurrentlyLoadedArea;internal GameModeType CurrentMode=GameModeType.Default;internal bool CutsceneLock,DialogActive;internal bool IsModeActive(GameModeType mode){return mode==GameModeType.Dialog&&DialogActive;} }
 sealed class Preset {internal object Area,PlayerCharacter;}
 sealed class Root {internal object DefaultPlayerCharacter;}
 sealed class Probe {internal bool Complete=true;internal Player NativePlayer;internal string NativeGameId;}
 sealed class Clock {internal double Seconds;internal TimeSpan Elapsed {get{return TimeSpan.FromSeconds(Seconds);}}}
 sealed class DialogMessageBox {internal static DialogMessageBox Instance;internal bool IsShown;}
 sealed class Scenario {
  internal bool completed,disposed,cleanup,callbackReturned=true,NativeBusy;
  internal int ReadyReached,Failures,CleanupBegins,CleanupPolls,AuthorityReads;
  internal string LastError;
  internal Clock clock=new Clock();internal Player player=new Player();internal NativeGame game=new NativeGame();internal Preset preset=new Preset();internal Root blueprintRoot=new Root();internal Probe probe=new Probe();
  internal Scenario(){preset.Area=new object();preset.PlayerCharacter=new object();blueprintRoot.DefaultPlayerCharacter=new object();game.CurrentlyLoadedArea=preset.Area;player.MainCharacter.Value=new NativeUnit{Blueprint=preset.PlayerCharacter};probe.NativePlayer=player;probe.NativeGameId=player.GameId;}
  private void RequireAuthority(bool cleanupOnly){AuthorityReads++;}
  private bool Busy(){return NativeBusy;}
  private void PollCleanup(){CleanupPolls++;}
  private void BeginCleanup(){CleanupBegins++;cleanup=true;}
  private void Fail(string kind,string error){Failures++;LastError=kind+": "+error;}
  private static void Require(bool value,string reason){if(!value)throw new InvalidOperationException(reason);}
/*ACTUAL_UPDATE_READINESS*/
 }
 static class Tests {
  static int passed,failed;
  static void Check(string name,bool value){Console.WriteLine((value?"PASS ":"FAIL ")+name);if(value)passed++;else failed++;}
  static Scenario Fresh(){DialogMessageBox.Instance=null;return new Scenario();}
  static NativeStart Pending(Scenario value,bool marked=true){var start=new NativeStart{IsScheduled=marked,Dialog=new object()};value.player.State.Scheduled=start;return start;}
  static bool Waited(Scenario s){return s.ReadyReached==0&&s.Failures==0&&s.CleanupBegins==0;}
  internal static int Main(){
   var s=Fresh();s.Update();Check("settled native Default passes exact readiness prefix",s.ReadyReached==1&&s.Failures==0);Check("ready prefix does not begin cleanup or alter native mode",s.CleanupBegins==0&&s.game.CurrentMode==GameModeType.Default);
   s=Fresh();s.callbackReturned=false;s.Update();Check("unfinished owned callback waits before dialog read",Waited(s)&&s.player.DialogReads==0);
   s=Fresh();s.NativeBusy=true;s.Update();Check("native worker waits before dialog read",Waited(s)&&s.player.DialogReads==0);
   s=Fresh();DialogMessageBox.Instance=new DialogMessageBox{IsShown=true};s.Update();Check("unexpected popup fails before any readiness read",s.Failures==1&&s.CleanupBegins==1&&s.player.DialogReads==0&&s.LastError.Contains("unexpected dialog"));
   s=Fresh();s.clock.Seconds=301;s.Update();Check("original settlement deadline remains a failure",s.Failures==1&&s.CleanupBegins==1&&s.player.DialogReads==0);
   s=Fresh();Pending(s);s.probe.Complete=false;s.Update();Check("pending start cannot excuse incomplete native observer",s.Failures==1&&s.player.DialogReads==0);
   s=Fresh();s.probe.NativePlayer=new Player();s.Update();Check("foreign native player still refuses",s.Failures==1);
   s=Fresh();s.probe.NativeGameId="foreign";s.Update();Check("foreign campaign identity still refuses",s.Failures==1);
   s=Fresh();Pending(s);s.game.CurrentlyLoadedArea=new object();s.Update();Check("pending start cannot excuse foreign area",s.Failures==1&&s.player.DialogReads==0);
   s=Fresh();s.player.MainCharacter.Value=null;s.Update();Check("missing native main still refuses",s.Failures==1);
   s=Fresh();s.player.MainCharacter.Value.Blueprint=new object();s.Update();Check("foreign main blueprint still refuses",s.Failures==1);
   s=Fresh();s.game.CurrentMode=GameModeType.None;s.Update();Check("non-Default top mode still waits",Waited(s));
   s=Fresh();s.game.DialogActive=true;s.Update();Check("Dialog active beneath Default still waits",Waited(s));
   s=Fresh();s.game.CutsceneLock=true;s.Update();Check("native cutscene still waits",Waited(s));
   s=Fresh();s.player.MainCharacter.Value.View=null;s.Update();Check("missing native view still waits",Waited(s));
   s=Fresh();s.player.MainCharacter.Value.View.AgentASP=null;s.Update();Check("missing native agent still waits",Waited(s));
   s=Fresh();var pending=Pending(s);s.Update();Check("Default with scheduled native start waits",Waited(s));Check("pending start keeps exact object reference",ReferenceEquals(s.player.State.Scheduled,pending));Check("pending start native scheduling marker is unchanged",pending.IsScheduled);Check("pending start does not force mode or active dialog",s.game.CurrentMode==GameModeType.Default&&!s.game.DialogActive);
   s=Fresh();pending=Pending(s,false);s.Update();Check("unmarked scheduled data still waits",Waited(s));Check("unmarked scheduling flag remains unchanged",!pending.IsScheduled);
   s=Fresh();pending=Pending(s);pending.Dialog=null;s.Update();Check("scheduled data without blueprint is never quiescence",Waited(s));
   s=Fresh();s.player.State=null;s.Update();Check("missing native dialog state refuses with cleanup custody",s.ReadyReached==0&&s.Failures==1&&s.CleanupBegins==1&&s.LastError.Contains("dialog state"));
   s=Fresh();s.player.ThrowDialog=true;s.Update();Check("native dialog getter error refuses with original cause",s.ReadyReached==0&&s.Failures==1&&s.CleanupBegins==1&&s.LastError.Contains("native dialog read failed"));
   s=Fresh();pending=Pending(s);s.Update();s.player.State.Scheduled=null;s.Update();Check("naturally consumed start admits only later quiescent readiness",s.ReadyReached==1&&s.Failures==0&&s.CleanupBegins==0);
   s=Fresh();pending=Pending(s);s.Update();s.player.State.Scheduled=null;s.game.CurrentMode=GameModeType.Dialog;s.game.DialogActive=true;s.Update();Check("consumed start with active authored dialogue still waits",Waited(s));s.game.CurrentMode=GameModeType.Default;s.game.DialogActive=false;s.Update();Check("later native dialogue completion can reach readiness",s.ReadyReached==1&&s.Failures==0);
   s=Fresh();Pending(s);s.Update();s.clock.Seconds=301;s.Update();Check("pending debt cannot remove original timeout",s.ReadyReached==0&&s.Failures==1&&s.CleanupBegins==1);
   s=Fresh();s.cleanup=true;s.Update();Check("existing cleanup path is independent of readiness",s.CleanupPolls==1&&s.ReadyReached==0&&s.AuthorityReads==0);
   s=Fresh();s.completed=true;s.Update();Check("completed scenario remains inert",s.ReadyReached==0&&s.AuthorityReads==0);
   s=Fresh();s.disposed=true;s.Update();Check("disposed scenario remains inert",s.ReadyReached==0&&s.AuthorityReads==0);
   Console.WriteLine("TOTAL PASS="+passed+" FAIL="+failed);return failed==0?0:1;
  }
 }
}
