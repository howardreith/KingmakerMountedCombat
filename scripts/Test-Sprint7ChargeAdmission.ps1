# Sprint 7 ownership boundary: the compiled production controller and carrier, on real native container and command types.
# Only the inaccessible engine boundary is absent; no game is launched. This is not native evidence.
[CmdletBinding()]
param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$lab=[IO.Path]::GetFullPath((Join-Path $repo '../..'))
$layout=(Get-Content -Raw (Join-Path $lab 'environment-intake.json') | ConvertFrom-Json).requestedLayout
$managed=Join-Path $layout.kingmakerInstallDir 'Kingmaker_Data/Managed'
$dll=Join-Path $repo "bin/$Configuration/KingmakerMountedCombat.dll"
Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
public static class Sprint7AdmissionProbe {
 const BindingFlags F=BindingFlags.Instance|BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public;
 public static int Passed,Failed;
 static void Check(bool ok,string text){if(ok){Passed++;Console.WriteLine("    PASS "+text);}else{Failed++;Console.WriteLine("    FAIL "+text);}}
 static object Blank(Type t){return FormatterServices.GetUninitializedObject(t);}
 static FieldInfo Field(Type t,string name){for(;t!=null;t=t.BaseType){var f=t.GetField(name,F);if(f!=null)return f;}throw new MissingFieldException(name);}
 static void Set(object o,string name,object v){Field(o.GetType(),name).SetValue(o,v);}
 static object Get(object o,string name){return Field(o.GetType(),name).GetValue(o);}
 static object Call(object o,string name,params object[] args){
  var m=o.GetType().GetMethods(F).First(x=>x.Name==name&&x.GetParameters().Length==args.Length);
  try{return m.Invoke(o,args);}catch(TargetInvocationException e){throw e.InnerException;}}
 sealed class LoggerProxy:RealProxy{public LoggerProxy(Type t):base(t){} public override IMessage Invoke(IMessage m){var c=(IMethodCallMessage)m;return new ReturnMessage(null,null,0,c.LogicalCallContext,c);}}
 static Assembly mod,native; static Type commandType,commandsType,pairType,routeType;
 static object Container(){var c=Blank(commandsType);Set(c,"m_Commands",Array.CreateInstance(commandType,4));Set(c,"m_Queue",Activator.CreateInstance(Field(commandsType,"m_Queue").FieldType));return c;}
 static object Move(){return Blank(native.GetType("Kingmaker.UnitLogic.Commands.UnitMoveTo",true));}
 sealed class World{public object Controller,Owner,Rider,Mount,Command,Shell,Carrier;}
 static World Live(bool started,bool commandResident=true){
  var w=new World{Rider=Container(),Mount=Container()};
  w.Command=Blank(pairType);Set(w.Command,"chargeMode",true);
  var carriers=Activator.CreateInstance(Field(pairType,"chargeCarriers").FieldType);Set(w.Command,"chargeCarriers",carriers);
  Set(w.Command,"transaction",Activator.CreateInstance(mod.GetType("KingmakerMountedCombat.Domain.MountedCombatTransaction",true)));
  Set(w.Command,"<IsStarted>k__BackingField",started);
  w.Carrier=Move();
  carriers.GetType().GetMethod("Add").Invoke(carriers,new[]{Activator.CreateInstance(carriers.GetType().GetGenericArguments()[0],w.Mount,w.Carrier)});
  ((Array)Get(w.Mount,"m_Commands")).SetValue(w.Carrier,3);
  if(started)((Array)Get(w.Rider,"m_Commands")).SetValue(w.Command,1);
  else if(commandResident){var q=Get(w.Rider,"m_Queue");q.GetType().GetMethod("AddLast",new[]{commandType}).Invoke(q,new[]{w.Command});}
  w.Controller=Blank(mod.GetType("KingmakerMountedCombat.Integration.MountedCombatController",true));
  Set(w.Controller,"logger",new LoggerProxy(mod.GetType("KingmakerMountedCombat.Logging.IModLogger",true)).GetTransparentProxy());
  Set(w.Controller,"pairedCommandScheduler",Blank(mod.GetType("KingmakerMountedCombat.Integration.MountedPairCommandScheduler",true)));
  w.Shell=Blank(native.GetType("Kingmaker.UnitLogic.Commands.UnitUseAbility",true));
  Set(w.Shell,"<IsFinished>k__BackingField",true);
  w.Owner=Call(w.Controller,"CreateChargeOwner",null,null,null,w.Rider,w.Mount,w.Shell,17L,0);
  Set(w.Owner,"Command",w.Command);Set(w.Controller,"chargeOwner",w.Owner);Set(w.Controller,"activeCommand",w.Command);
  return w;
 }
 static bool Admit(World w,object container,object command,string route){
  var m=w.Controller.GetType().GetMethods(F).First(x=>x.Name=="AdmitChargeCommand");
  var args=m.GetParameters().Length==2?new[]{container,command}:new[]{container,command,Enum.Parse(routeType,route)};
  try{return (bool)m.Invoke(w.Controller,args);}catch(TargetInvocationException e){throw e.InnerException;}
 }
 static bool Unchanged(World w){
  var o=Get(w.Owner,"Ownership");
  return ReferenceEquals(Get(w.Controller,"chargeOwner"),w.Owner)&&!(bool)o.GetType().GetProperty("CleanupRequested").GetValue(o,null)&&
   ReferenceEquals(Get(w.Controller,"activeCommand"),w.Command)&&(int)o.GetType().GetProperty("AttemptCount").GetValue(o,null)==0;
 }
 static bool QueueHas(object c,object command){var q=(System.Collections.IEnumerable)Get(c,"m_Queue");return q.Cast<object>().Any(x=>ReferenceEquals(x,command));}
 static int Attempt(string name,Action body){try{body();return 0;}catch(Exception e){Failed++;Console.WriteLine("    FAIL "+name+" threw "+e.GetType().Name+": "+e.Message);return 1;}}
 public static void Run(string managed,string product){
  AppDomain.CurrentDomain.AssemblyResolve+=(s,a)=>{string leaf=new AssemblyName(a.Name).Name+".dll";foreach(var root in new[]{managed,Path.Combine(managed,"UnityModManager")}){var p=Path.Combine(root,leaf);if(File.Exists(p))return Assembly.LoadFrom(p);}return null;};
  native=Assembly.LoadFrom(Path.Combine(managed,"Assembly-CSharp.dll"));mod=Assembly.LoadFrom(product);
  commandType=native.GetType("Kingmaker.UnitLogic.Commands.Base.UnitCommand",true);commandsType=native.GetType("Kingmaker.UnitLogic.Commands.UnitCommands",true);
  pairType=mod.GetType("KingmakerMountedCombat.Integration.MountedPairAttackCommand",true);
  routeType=mod.GetType("KingmakerMountedCombat.Domain.MountedChargeRequestRoute",false);
  Attempt("pending follow-up",()=>{var w=Live(true);var follow=Move();
   Check(Admit(w,w.Rider,follow,"Enqueue")&&Unchanged(w),"a player's follow-up enqueued on the rider leaves the exact current owner untouched");});
  Attempt("rejected run",()=>{var w=Live(true);var request=Move();
   var admitted=Admit(w,w.Rider,request,"Run");
   Check(Unchanged(w),"a Run request is decided without draining: a later gate's refusal leaves the charge unchanged (admitted="+admitted+")");});
  Attempt("mount enqueue",()=>{var w=Live(true);var follow=Move();
   Check(!Admit(w,w.Mount,follow,"Enqueue")&&Unchanged(w),"an enqueue on the carrying mount is refused clearly, with no mutation");});
  Attempt("own carrier",()=>{var w=Live(true);
   Check(Admit(w,w.Mount,w.Carrier,"Run")&&Unchanged(w),"the charge's own carrier stays admissible");
   Check(Admit(w,w.Rider,w.Command,"Enqueue")&&Unchanged(w),"the charge's own reentrant enqueue stays admissible");});
  // A brain proposal's AiAction is a Unity object, whose null test needs the engine: the decision is in the policy tests, the behaviour in the native rows with a live brain.
  Attempt("starting window",()=>{var w=Live(false);var request=Move();
   Check(!Admit(w,w.Rider,request,"Run")&&Unchanged(w)&&QueueHas(w.Rider,w.Command),"while the attack waits in the queue a request is refused, because native Run would clear it");});
  if(routeType==null){Check(false,"the request route is a separate production concept");return;}
  Attempt("promotion",()=>{var w=Live(true);var follow=Move();
   Check(!Admit(w,w.Rider,follow,"QueuePromotion")&&Unchanged(w)&&QueueHas(w.Rider,follow),"a promotion during a live charge goes back to the queue, neither dropped nor displacing");});
  Attempt("terminal",()=>{var w=Live(true);Set(w.Command,"<IsFinished>k__BackingField",true);Set(w.Command,"<Result>k__BackingField",Enum.Parse(commandType.GetProperty("Result",F).PropertyType,"Interrupt"));
   ((Array)Get(w.Mount,"m_Commands")).SetValue(null,3);Set(w.Carrier,"<IsFinished>k__BackingField",true);
   ((Array)Get(w.Rider,"m_Commands")).SetValue(null,1);
   var successor=Move();((Array)Get(w.Mount,"m_Commands")).SetValue(successor,3);
   var removed=false;try{Call(w.Command,"TryDrainChargeCarrier");}catch(Exception){}
   removed=!ReferenceEquals(((Array)Get(w.Mount,"m_Commands")).GetValue(3),successor)||(bool)successor.GetType().GetProperty("IsFinished").GetValue(successor,null);
   Check(!removed,"carrier cleanup leaves the active successor in the mount's slot, unfinished");
   Check((bool)Call(w.Command,"ForeignMountCommandActive"),"carrier cleanup recognises a live successor, so it stops no one else's path");});
 }
}
'@
[Sprint7AdmissionProbe]::Run($managed,$dll)
Write-Output ('Sprint 7 admission: PASS='+[Sprint7AdmissionProbe]::Passed+' FAIL='+[Sprint7AdmissionProbe]::Failed)
if([Sprint7AdmissionProbe]::Failed -ne 0){ exit 1 }
