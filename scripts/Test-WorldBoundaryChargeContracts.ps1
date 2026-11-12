[CmdletBinding()]
param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$lab=[IO.Path]::GetFullPath((Join-Path $repo '../..'))
$layout=(Get-Content -Raw (Join-Path $lab 'environment-intake.json') | ConvertFrom-Json).requestedLayout
$managed=Join-Path $layout.kingmakerInstallDir 'Kingmaker_Data/Managed'
$dll=Join-Path $repo "bin/$Configuration/KingmakerMountedCombat.dll"
if((Get-FileHash (Join-Path $managed 'Assembly-CSharp.dll')).Hash.ToLowerInvariant() -cne '3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb'){throw 'Installed native identity changed.'}
# Review of PR #31 (P2): a saved-world replacement (Game.LoadGame -> full Game.LoadArea with the selected SaveInfo)
# must not let the native unload begin while ANY mounted pair retains charge cleanup, including a secondary pair that
# holds no charge fence. Detached production-adapter probe over two real compiled MountedCombatController owners and
# the real compiled MountedPersistenceService admission decision. Native containers and commands are real types; only
# the inaccessible logging boundary is substituted. No game is launched and no save is read or written.
Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
public static class WorldBoundaryChargeProbe {
 const BindingFlags F=BindingFlags.Instance|BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public;
 static int passed;
 static void Check(bool ok,string text){if(!ok)throw new InvalidOperationException(text);passed++;Console.WriteLine("PASS "+text);}
 static object Blank(Type t){return FormatterServices.GetUninitializedObject(t);}
 static FieldInfo Field(Type t,string name){for(;t!=null;t=t.BaseType){var f=t.GetField(name,F);if(f!=null)return f;}throw new MissingFieldException(name);}
 static void Set(object o,string n,object v){Field(o.GetType(),n).SetValue(o,v);}
 static object Get(object o,string n){return Field(o.GetType(),n).GetValue(o);}
 static object Call(object o,string n,params object[] args){return o.GetType().GetMethod(n,F).Invoke(o,args);}
 sealed class LoggerProxy:RealProxy {
  internal LoggerProxy(Type t):base(t){}
  public override IMessage Invoke(IMessage m){return new ReturnMessage(null,null,0,null,(IMethodCallMessage)m);}
 }
 public static void Run(string managed,string dll){
  AppDomain.CurrentDomain.AssemblyResolve+=(s,a)=>{
   var leaf=new AssemblyName(a.Name).Name+".dll";
   foreach(var root in new[]{managed,Path.Combine(managed,"UnityModManager")}){var path=Path.Combine(root,leaf);if(File.Exists(path))return Assembly.LoadFrom(path);}return null;
  };
  var native=Assembly.LoadFrom(Path.Combine(managed,"Assembly-CSharp.dll"));var mod=Assembly.LoadFrom(dll);
  Check(native.ManifestModule.ModuleVersionId.ToString()=="07fa1e4d-8618-41b3-9b8d-faa17d3b26f7","pinned native MVID");
  var loggerType=mod.GetType("KingmakerMountedCombat.Logging.IModLogger",true);
  var commandType=native.GetType("Kingmaker.UnitLogic.Commands.Base.UnitCommand",true);
  var commandsType=native.GetType("Kingmaker.UnitLogic.Commands.UnitCommands",true);
  Func<object> container=()=>{var c=Blank(commandsType);Set(c,"m_Commands",Array.CreateInstance(commandType,4));
   Set(c,"m_Queue",Activator.CreateInstance(Field(commandsType,"m_Queue").FieldType));return c;};
  var riderCommands=container();var mountCommands=container();
  // Pair B's retained charge: native Interrupt's partial terminal (result Interrupt, terminal false). Its cleanup cannot
  // finish until the native terminal is observed; nothing here commits, finishes or fabricates an action.
  var pairType=mod.GetType("KingmakerMountedCombat.Integration.MountedPairAttackCommand",true);
  var command=Blank(pairType);Set(command,"chargeMode",true);
  Set(command,"chargeCarriers",Activator.CreateInstance(Field(pairType,"chargeCarriers").FieldType));
  Set(command,"transaction",Activator.CreateInstance(mod.GetType("KingmakerMountedCombat.Domain.MountedCombatTransaction",true)));
  Set(command,"<Result>k__BackingField",Enum.Parse(commandType.GetProperty("Result",F).PropertyType,"Interrupt"));
  Set(command,"<IsFinished>k__BackingField",false);
  ((Array)Get(riderCommands,"m_Commands")).SetValue(command,1);
  var controllerType=mod.GetType("KingmakerMountedCombat.Integration.MountedCombatController",true);
  Func<object> controller=()=>{var c=Blank(controllerType);Set(c,"logger",new LoggerProxy(loggerType).GetTransparentProxy());
   Set(c,"pairedCommandScheduler",Blank(mod.GetType("KingmakerMountedCombat.Integration.MountedPairCommandScheduler",true)));return c;};
  var primary=controller();var secondary=controller();
  var shell=Blank(native.GetType("Kingmaker.UnitLogic.Commands.UnitUseAbility",true));
  Set(shell,"<IsFinished>k__BackingField",true);
  var owner=Call(secondary,"CreateChargeOwner",null,null,null,riderCommands,mountCommands,shell,17L,0);
  Set(owner,"Command",command);Set(secondary,"chargeOwner",owner);Set(secondary,"activeCommand",command);

  // The persistence service sees both pairs through its mounted world, exactly as WorldCombats enumerates them. Its
  // primary-only field is pair A. No unified-turn coordinator is bound: consulting ordinary participation would throw.
  var worldType=mod.GetType("KingmakerMountedCombat.Integration.MountedWorldService",true);
  var pairServicesType=mod.GetType("KingmakerMountedCombat.Integration.MountedPairServices",true);
  var world=Blank(worldType);var pairs=Activator.CreateInstance(Field(worldType,"pairs").FieldType);Set(world,"pairs",pairs);
  foreach(var combat in new[]{primary,secondary}){
   var services=Activator.CreateInstance(pairServicesType,true);Set(services,"Combat",combat);
   pairs.GetType().GetMethod("Add").Invoke(pairs,new[]{services});
  }
  var persistence=Blank(mod.GetType("KingmakerMountedCombat.Integration.MountedPersistenceService",true));
  Set(persistence,"logger",new LoggerProxy(loggerType).GetTransparentProxy());
  Set(persistence,"combat",primary);Set(persistence,"mountedWorld",world);

  Check(!(bool)Call(persistence,"get_AnyChargeFence")&&!(bool)Call(secondary,"get_ChargeAdmissionFenced")&&
   (bool)Call(secondary,"get_HasChargeOwnership")&&!(bool)Call(primary,"get_HasChargeOwnership")&&
   (bool)Call(primary,"TryDrainChargeOwnership","primary-only load admission"),
   "fixture: pair B retains charge ownership with no charge fence, and pair A (the primary-only admission) is clean");
  // The save guards refuse first: an active save is decided before any pair's charge cleanup is attempted.
  var scopeType=persistence.GetType().GetNestedType("SaveScope",BindingFlags.NonPublic);
  Set(persistence,"activeSave",Activator.CreateInstance(scopeType,true));
  var attempts=(int)Call(secondary,"get_ChargeCleanupAttemptCount");
  Check(Call(persistence,"DecideSavedWorldReplacement",null,true).ToString()=="SaveScopeActive"&&
   attempts==(int)Call(secondary,"get_ChargeCleanupAttemptCount"),
   "an active save refuses the replacement before pair B's retained charge cleanup is attempted");
  Set(persistence,"activeSave",null);
  // bound=true: Game.LoadGame's own admission already accepted this exact descriptor (the in-session load route).
  var decision=Call(persistence,"DecideSavedWorldReplacement",null,true);
  Check(decision.ToString()=="ChargeCleanupUnresolved",
   "a saved-world replacement is refused before the native unload while a secondary pair retains charge cleanup: "+decision);
  Check(Object.ReferenceEquals(Get(secondary,"chargeOwner"),owner)&&Object.ReferenceEquals(Get(secondary,"activeCommand"),command)&&
   !(bool)commandType.GetProperty("IsActed").GetValue(command,null),
   "the refused replacement retains pair B's exact owner and command and commits nothing");
  decision=Call(persistence,"DecideSavedWorldReplacement",null,true);
  Check(decision.ToString()=="ChargeCleanupUnresolved","a repeated load attempt stays refused while the native terminal is unobserved");
  // Native terminal completion is an external observation in this detached probe.
  Set(command,"<IsFinished>k__BackingField",true);
  decision=Call(persistence,"DecideSavedWorldReplacement",null,true);
  Check(decision.ToString()=="None"&&Get(secondary,"chargeOwner")==null&&Get(secondary,"activeCommand")==null,
   "the same replacement is admitted once pair B's cleanup settles, and only through B's own full drain: "+decision);
  // Control: ordinary participation is never consulted (no coordinator is bound), so a paired turn without charge
  // custody is replaceable.
  Check(Call(persistence,"DecideSavedWorldReplacement",null,true).ToString()=="None",
   "an ordinary mounted world with no charge custody is replaced by a valid load without consulting participation");
  Console.WriteLine("WORLD BOUNDARY CHARGE ADAPTER PASS="+passed+" FAIL=0; detached, not native gameplay qualification");
 }
}
'@
try { [WorldBoundaryChargeProbe]::Run($managed,$dll) }
catch { Write-Output $_.Exception.ToString(); exit 1 }
