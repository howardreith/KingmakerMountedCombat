param([string]$SourceDirectory,[string]$OutputDirectory,[string]$OrdinarySourceDirectory)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$initializer=Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1'
if(Test-Path -LiteralPath $initializer){
 . $initializer
 if([string]::IsNullOrWhiteSpace($SourceDirectory)){$SourceDirectory=Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics'}
 if([string]::IsNullOrWhiteSpace($OutputDirectory)){$OutputDirectory=Join-Path $chunk7AnalysisRoot ('observer-acquisition-'+[Guid]::NewGuid().ToString('N'))}
}elseif([string]::IsNullOrWhiteSpace($SourceDirectory)-or[string]::IsNullOrWhiteSpace($OutputDirectory)){throw 'A private adapter invocation requires explicit source and output directories.'}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
function Get-ExactMethod([string]$Text,[string]$Signature){
 $start=$Text.IndexOf($Signature,[StringComparison]::Ordinal);if($start-lt0){throw "Missing exact method: $Signature"}
 $open=$Text.IndexOf('{',$start);$depth=1;$end=$open+1
 while($depth-gt0-and$end-lt$Text.Length){if($Text[$end]-ceq'{'){$depth++}elseif($Text[$end]-ceq'}'){$depth--};$end++}
 if($depth-ne0){throw 'Unbalanced actual-method extraction.'}
 return $Text.Substring($start,$end-$start)
}
$body=@'
using System;using System.Reflection;
class HarmonyMethod{public int prioritiy;public HarmonyMethod(object m){}}
static class Priority{public const int First=0,Last=0;}
class HarmonyInstance{public static int PatchFailureAt,UnpatchFailures,PatchCalls,UnpatchCalls,LiveHooks;public static HarmonyInstance Create(string id){return new HarmonyInstance();}public void Patch(params object[] args){PatchCalls++;LiveHooks++;if(PatchFailureAt==PatchCalls)throw new InvalidOperationException("installed hook then Patch failed");}public void UnpatchAll(string id){UnpatchCalls++;if(UnpatchFailures>0){UnpatchFailures--;throw new InvalidOperationException("Unpatch failed");}LiveHooks=0;}public static void Reset(){PatchFailureAt=1;UnpatchFailures=1;PatchCalls=UnpatchCalls=LiveHooks=0;}}
class IdentityBoundary{public int Calls;public int FaultCount=>0;public void Dispose(){Calls++;}}
class JArray:System.Collections.Generic.List<object>{public object DeepClone(){var clone=new JArray();clone.AddRange(this);return clone;}}
class UnitEntityData{}class TurnController{}class CombatController{}class UnitCombatState{public class Cooldowns{}}class UnitCombatCooldownsController{}class BaseUnitCombatController{}class CombatAiData{}class UnitCommands{}class UnitActionController{}class UnitCommand{}class UnitConfusionController{}class UnitMovementAgent{}class UnitMovementAgentBase{}class UnitCombatPrepareController{}class PairedConfusionPreparation{public static void Prepare(){}}
class NativeCombatActorPersistence{public static void RestoreActor(){}public static void RestoreDebt(){}}
class Game{public static Game Instance=new Game();}class LevelUpAutoSaveController{}class SaveInfo{}class SaveManager{}
class MountedPersistenceService{public void TryRestoreArchiveCombat(){}public void RestoreArchivePresentation(){}}
'@
$names=@('NativeArchiveRestorationProbe','NativeRoundBoundaryProbe','NativeLifeDeliveryProbe','NativeTurnCompletionProbe','NativeActorAllocationTrace');$receipts=@()
if([string]::IsNullOrWhiteSpace($OrdinarySourceDirectory)){$OrdinarySourceDirectory=$SourceDirectory}
if(Test-Path -LiteralPath (Join-Path $OrdinarySourceDirectory 'NativeOrdinaryAcquisitionProbe.cs')){$names+=@('NativeOrdinaryAcquisitionProbe')}
foreach($name in $names){
 $path=Join-Path $(if($name-ceq'NativeOrdinaryAcquisitionProbe'){$OrdinarySourceDirectory}else{$SourceDirectory}) ($name+'.cs');$text=[IO.File]::ReadAllText($path)
 $method=if($text.Contains('internal void Install()')){Get-ExactMethod $text '        internal void Install()'}else{Get-ExactMethod $text ('        internal '+$name+'(')}
 $start=$method.IndexOf('harmony = HarmonyInstance.Create(',[StringComparison]::Ordinal);if($start-lt0){throw 'Missing actual Harmony ownership boundary.'}
 $tail=$method.Substring($start,$method.LastIndexOf('}')-$start)
 $deferred=$text.Contains('internal void Install()');$prefix=''
 if($deferred){
  $marker=$method.IndexOf('// Native contract validation;', [StringComparison]::Ordinal);$open=$method.IndexOf('{')
  if($marker-le$open-or$marker-ge$start){throw 'Explicit Install has no exact contract-boundary marker.'}
  $prefix=$method.Substring($open+1,$marker-$open-1)
 }
 $dispose=Get-ExactMethod $text '        public void Dispose()'
 $receipts+=@{file=$name+'.cs';sha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant();explicitInstall=$deferred}
 $body+='class '+$name+' { const string HarmonyId="probe",Id="probe";const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;static '+$name+' active;HarmonyInstance harmony;bool disposed,installed,installationStarted;int tickDepth,setDepth;Action AfterNativeRound;object world=new object();MethodInfo method=null,tick=null,set=null,callback=null,deathCallback=null;bool observeDeath;IdentityBoundary identities=new IdentityBoundary();int dropped,observationErrors,completionEntries,completionExits,saveRequests=1,preparedWrites=1;bool InSynchronousCallback=>false;'+$(if($name-ceq'NativeArchiveRestorationProbe'){'int errors;object debtRequest;Game debtGame;int debtErrors,debtDropped;System.Collections.Generic.HashSet<int> debtOpenCalls=new System.Collections.Generic.HashSet<int>();bool DebtIdentityCurrent()=>debtRequest!=null&&ReferenceEquals(Game.Instance,debtGame);'}else{'JArray errors=new JArray();'})+$(if($name-ceq'NativeActorAllocationTrace'){'int disablePrepareDepth;'}else{''})+''+[Environment]::NewLine
 $body+='void Patch(Type type,int token,string before,string after){harmony.Patch(type,token,before,after);}void Patch(Type type,string name,int token,Type[] args,string before,string after){harmony.Patch(type,name,token,args,before,after);}void Patch(MethodInfo method,string before,string after){harmony.Patch(method,before,after);}void PatchProduct(string name,string before,string after){harmony.Patch(name,before,after);}void Install(MethodInfo method,string before,string after){harmony.Patch(method,before,after);}static void After(){}static void TickBefore(){}static void TickAfter(){}static void SetBefore(){}static void SetAfter(){}static void DeliveryBefore(){}static void DeliveryAfter(){}static void DeathBefore(){}static void DeathAfter(){}'+[Environment]::NewLine
 if($deferred){$body+='public '+$name+'(){}internal void Install(){'+$prefix+$tail+'}'}else{$body+='public '+$name+'(){'+$tail+'}'}
 $body+=$dispose+[Environment]::NewLine
 if($text.Contains('        private JArray CaptureErrors()')){
  $body+=(Get-ExactMethod $text '        private JArray CaptureErrors()')+'bool Incomplete()=>CaptureErrors().Count!=0;'
 }else{
  $complete=[regex]::Match($text,'internal bool Complete => [^;]+;').Value
  if([string]::IsNullOrEmpty($complete)){throw 'Missing exact completion predicate.'}
  $body+=$complete+'bool Incomplete()=>!Complete;'
 }
 $body+='public static int Verify(){int passed=0;HarmonyInstance.Reset();'+$name+' caller=null;bool failed=false;try{caller=new '+$name+'();'+$(if($deferred){'if(!caller.Incomplete())throw new Exception("Capture-only observer reported complete");passed++;caller.Install();'})+'}catch{failed=true;}if(!failed)throw new Exception("Patch fault was not exercised");if(caller==null||active!=caller){if(active!=null){HarmonyInstance.UnpatchFailures=0;active.Dispose();}throw new Exception("'+$name+': caller lost partial-install custody after failed patch and rollback");}if(!caller.Incomplete())throw new Exception("Partial observer reported complete");passed++;if(HarmonyInstance.UnpatchCalls!=0||HarmonyInstance.LiveHooks==0)throw new Exception("Failed Install attempted hidden rollback or lost hook evidence");passed++;'
 if($deferred){$body+='bool installRefused=false;try{caller.Install();}catch(InvalidOperationException){installRefused=true;}if(!installRefused||HarmonyInstance.PatchCalls!=1)throw new Exception("Partial install was replayed");passed++;'}
 $body+='bool cleanupRefused=false;try{caller.Dispose();}catch(InvalidOperationException){cleanupRefused=true;}if(!cleanupRefused||active!=caller||caller.disposed||HarmonyInstance.LiveHooks==0)throw new Exception("Failed explicit rollback lost owner");passed++;caller.Dispose();if(active!=null||!caller.disposed||HarmonyInstance.LiveHooks!=0||HarmonyInstance.UnpatchCalls!=2)throw new Exception("Retry did not settle retained observer");passed++;caller.Dispose();if(HarmonyInstance.UnpatchCalls!=2)throw new Exception("Settled cleanup repeated native unpatch");passed++;'
 if($deferred){$body+='bool disposedRefused=false;try{caller.Install();}catch(ObjectDisposedException){disposedRefused=true;}if(!disposedRefused)throw new Exception("Disposed observer reacquired native hooks");passed++;HarmonyInstance.Reset();HarmonyInstance.PatchFailureAt=0;HarmonyInstance.UnpatchFailures=0;caller=new '+$name+'();caller.Install();int writes=HarmonyInstance.PatchCalls;if(caller.Incomplete())throw new Exception("Completed observer kept installation failure");passed++;caller.Install();if(HarmonyInstance.PatchCalls!=writes||active!=caller||!caller.installed)throw new Exception("Successful Install was not idempotent");passed++;var neighbour=new '+$name+'();bool foreignRefused=false;try{neighbour.Install();}catch(InvalidOperationException){foreignRefused=true;}neighbour.Dispose();if(!foreignRefused||active!=caller||HarmonyInstance.UnpatchCalls!=0)throw new Exception("Foreign observer released an existing owner");passed++;caller.Dispose();HarmonyInstance.Reset();caller=new '+$name+'();caller.Dispose();if(HarmonyInstance.UnpatchCalls!=0||!caller.disposed)throw new Exception("Never-installed observer required native cleanup");passed++;'}
 $body+='Console.WriteLine("PASS '+$name+' actual installation/disposal ownership="+passed);return passed;}}'+[Environment]::NewLine
}
$body+='static class Program{static int Main(){int total=0,fail=0;'+(($names|ForEach-Object{'try{total+='+$_+'.Verify();}catch(Exception e){fail++;Console.WriteLine("FAIL "+e.Message);}'})-join'')+'Console.WriteLine("CHUNK7 OBSERVER ACQUISITION PASS="+total+" FAIL="+fail+"; exact installation tail and Dispose; native metadata/patch provider are external boundary stubs");return fail==0?0:1;}}'
$cs=Join-Path $OutputDirectory 'ObserverAcquisitionAdapter.cs';[IO.File]::WriteAllText($cs,$body)
$receipts|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $OutputDirectory 'source-hashes.json') -Encoding UTF8
$vs=Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Microsoft Visual Studio/Installer/vswhere.exe';$compiler=@(& $vs -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild/**/Bin/Roslyn/csc.exe')|Select-Object -First 1
$exe=Join-Path $OutputDirectory 'ObserverAcquisitionAdapter.exe';& $compiler /nologo /target:exe /langversion:7.3 /nowarn:0649,0169,0414 "/out:$exe" $cs
if($LASTEXITCODE-ne0){throw 'Observer actual ownership adapter compilation failed.'};& $exe
if($LASTEXITCODE-ne0){throw 'Observer partial-install custody regression failed.'}

# These focused call-site checks accompany the behavioral fault-injection test:
# a correct probe alone cannot protect an owner whose assignment/Install is reversed.
$bindings=@(
 @('Chunk7CombatScenario.cs','trace','NativeActorAllocationTrace'),@('Chunk7CombatScenario.cs','completion','NativeTurnCompletionProbe'),@('Chunk7CombatScenario.cs','rounds','NativeRoundBoundaryProbe'),
 @('Chunk7CombatCheckpointDriver.cs','trace','NativeActorAllocationTrace'),@('Chunk7CombatCheckpointDriver.cs','completion','NativeTurnCompletionProbe'),@('Chunk7CombatCheckpointDriver.cs','rounds','NativeRoundBoundaryProbe'),
 @('Chunk7CombatNativeLife.cs','lifeDelivery','NativeLifeDeliveryProbe'),@('Chunk7PersistenceScenario.cs','restoration','NativeArchiveRestorationProbe'),
 @('Chunk6aDismountTurnScenario.cs','chunk6aDismountCompletion','NativeTurnCompletionProbe'),@('Chunk6aMountOrderScenario.cs','chunk6aOrderCompletion','NativeTurnCompletionProbe'))
$bindingCount=0
foreach($binding in $bindings){
 $text=[IO.File]::ReadAllText((Join-Path $SourceDirectory $binding[0]));$field=[regex]::Escape($binding[1]);$type=[regex]::Escape($binding[2])
 if($text-cnotmatch ('\b'+$field+'\s*=\s*new\s+'+$type+'\b[^;]*;\s*(?:ownsObservers\s*=\s*true;\s*)?'+$field+'\.Install\(\);')){throw ('Observer field is not retained before Install: '+($binding-join'/'))}
 if($binding[2]-ceq'NativeActorAllocationTrace'-and$text-cnotmatch ('\b'+$field+'\s*=\s*new\s+'+$type+'\([^;]*deferInstallation:\s*true')){throw 'A selected Chunk 7 trace used historical eager construction.'}
 $bindingCount++
}
$cold=[IO.File]::ReadAllText((Join-Path $SourceDirectory 'Chunk7CombatCheckpointDriver.cs'))
if($cold-cnotmatch 'trace\s*=\s*new\s+NativeActorAllocationTrace\([^;]*;\s*ownsObservers\s*=\s*true;\s*trace\.Install\(\);'){throw 'Cold checkpoint ownsObservers is established after its fallible installation.'}
if([IO.File]::ReadAllText((Join-Path $SourceDirectory 'NativeActorAllocationTrace.cs'))-cnotmatch 'bool deferInstallation = false'){throw 'Historical allocation trace constructor default changed.'}
$bindingCount+=2
if($names-ccontains'NativeOrdinaryAcquisitionProbe'){
 $ordinary=[IO.File]::ReadAllText((Join-Path $OrdinarySourceDirectory 'Chunk7OrdinaryAcquisitionScenario.cs'))
 if($ordinary-cnotmatch '\bprobe\s*=\s*new\s+NativeOrdinaryAcquisitionProbe\b[^;]*;\s*probe\.Install\(\);'){throw 'Ordinary scenario lost retained probe assignment before Install.'}
 $bindingCount++
}
Write-Host ('CHUNK7 OBSERVER CALLER BINDINGS PASS='+$bindingCount+' FAIL=0')
