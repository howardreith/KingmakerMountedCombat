using System;
using System.Collections.Generic;
namespace KingmakerMountedCombat.Diagnostics {
internal interface ICallerProbe { void Capture(); void Cleanup(); bool HasCustody {get;} }
internal sealed class NativeOverlayBoundary : IMovementCaptureOverlay {
 internal bool Available=true, State=true, ThrowAfterHide;
 internal int FailedRestores, Writes;
 public bool IsAvailable {get{return Available;}}
 public bool Opened {get{return State;}}
 public void SetOpened(bool opened) { Writes++; if(opened&&FailedRestores>0){FailedRestores--;throw new InvalidOperationException("native overlay restore fault");} State=opened;if(!opened&&ThrowAfterHide)throw new InvalidOperationException("native hide mutated then faulted"); }
}
internal static class MovementScreenshotCaptureCoordinator {
 internal static NativeOverlayBoundary Native;
 internal static MovementCaptureOverlayLease AcquireClosedUmmLease(){return MovementCaptureOverlayLease.Acquire(Native);}
 internal static void AcquireClosedUmmLeaseRetained(out MovementCaptureOverlayLease custody){MovementCaptureOverlayLease.AcquireRetained(Native,out custody);}
}
//__ACTUAL_CALLERS__
internal static class Program {
 private static int pass, fail;
 private static void Check(bool value,string name){Console.WriteLine((value?"PASS ":"FAIL ")+name);if(value)pass++;else fail++;}
 private static bool Throws(Action action){try{action();return false;}catch{ return true; }}
 private static void Run(string name,Func<ICallerProbe> make){
  var clean=new NativeOverlayBoundary();MovementScreenshotCaptureCoordinator.Native=clean;var owner=make();owner.Capture();var hidden=owner.HasCustody&&!clean.Opened;owner.Cleanup();Check(hidden&&!owner.HasCustody&&clean.Opened,name+" normal acquisition and exact restoration");
  var fault=new NativeOverlayBoundary{ThrowAfterHide=true,FailedRestores=2};MovementScreenshotCaptureCoordinator.Native=fault;owner=make();var failed=Throws(owner.Capture);Check(failed&&owner.HasCustody&&!fault.Opened,name+" double acquisition fault keeps mutated overlay owner");
  var retryFailed=Throws(owner.Cleanup);Check(retryFailed&&owner.HasCustody&&!fault.Opened,name+" release failure preserves caller field for retry");
  owner.Cleanup();Check(!owner.HasCustody&&fault.Opened,name+" later successful cleanup restores original state then releases");
  var writes=fault.Writes;owner.Cleanup();Check(fault.Writes==writes&&!owner.HasCustody,name+" duplicate completed cleanup is inert");
  var absent=new NativeOverlayBoundary{Available=false};MovementScreenshotCaptureCoordinator.Native=absent;owner=make();Check(Throws(owner.Capture)&&!owner.HasCustody&&absent.Writes==0,name+" unavailable overlay refuses before mutation");
 }
 public static int Main(){
 //__CALL_TESTS__
 Console.WriteLine("Caller custody "+pass+" passed, "+fail+" failed.");return fail==0?0:1;
 }
}}
