param([string]$SourceDirectory,[string]$OverlaySourceDirectory,[string]$TestSourceDirectory,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$initializer=Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1'
if(Test-Path -LiteralPath $initializer){
 . $initializer
 if(-not $PSBoundParameters.ContainsKey('SourceDirectory')){$SourceDirectory=Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics'}
 if(-not $PSBoundParameters.ContainsKey('TestSourceDirectory')){$TestSourceDirectory=$chunk7TestSource}
 if(-not $PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path $chunk7AnalysisRoot ('progression-private-'+[Guid]::NewGuid().ToString('N'))}
}
if(-not $PSBoundParameters.ContainsKey('OverlaySourceDirectory')){$OverlaySourceDirectory=$SourceDirectory}
foreach($path in @($SourceDirectory,$OverlaySourceDirectory,$TestSourceDirectory,$OutputDirectory)){if([string]::IsNullOrWhiteSpace($path)){throw 'Exact progression source, template and private output paths are required.'}}
$privateRoot=$SourceDirectory
[void][IO.Directory]::CreateDirectory($OutputDirectory)
function Get-ExactMethod([string]$Text,[string]$Signature){
 $start=$Text.IndexOf($Signature,[StringComparison]::Ordinal);if($start-lt0){throw "Missing exact method: $Signature"}
 $open=$Text.IndexOf('{',$start);$depth=1;$end=$open+1
 while($depth-gt0-and$end-lt$Text.Length){if($Text[$end]-ceq'{'){$depth++}elseif($Text[$end]-ceq'}'){$depth--};$end++}
 if($depth-ne0){throw 'Unbalanced actual-method extraction.'};$Text.Substring($start,$end-$start)
}
$body=@'
using System;using System.Reflection;using System.Collections.Generic;
class HarmonyMethod{public int prioritiy;public HarmonyMethod(object value){}}
static class Priority{public const int First=0,Last=0;}
class HarmonyInstance{public static int PatchFailureAt,UnpatchFailures,PatchCalls,UnpatchCalls,LiveHooks;public static HarmonyInstance Create(string id){return new HarmonyInstance();}public void Patch(params object[] args){PatchCalls++;LiveHooks++;if(PatchFailureAt==PatchCalls)throw new InvalidOperationException("installed hook then Patch failed");}public void UnpatchAll(string id){UnpatchCalls++;if(UnpatchFailures>0){UnpatchFailures--;throw new InvalidOperationException("Unpatch failed");}LiveHooks=0;}public static void Reset(){PatchFailureAt=1;UnpatchFailures=1;PatchCalls=UnpatchCalls=LiveHooks=0;}}
class AddPet{}class Player{}class CharacterBuildController{}class RespecCompanion{}class Game{}class RestController{}class UnitDescriptor{}class UnitEntityData{}class JToken{}class LevelUpState{public enum CharBuildMode{Respec}}
'@
$names=@('NativeProgressionRespecProbe','NativeProgressionDismissProbe');$inputs=@()
foreach($name in $names){
 $path=Join-Path $privateRoot ($name+'.cs');$source=[IO.File]::ReadAllText($path);$install=Get-ExactMethod $source '        internal void Install()';$dispose=Get-ExactMethod $source '        public void Dispose()'
 $start=$install.IndexOf('harmony = HarmonyInstance.Create(',[StringComparison]::Ordinal);$marker=$install.IndexOf('// Native contract validation;', [StringComparison]::Ordinal);$open=$install.IndexOf('{')
 if($start-le$marker-or$marker-le$open){throw 'Missing explicit Install ownership boundary.'}
 $prefix=$install.Substring($open+1,$marker-$open-1);$tail=$install.Substring($start,$install.LastIndexOf('}')-$start)
 $complete=[regex]::Match($source,'internal bool Complete => [^;]+;').Value;if([string]::IsNullOrEmpty($complete)){throw 'Missing exact completion predicate.'}
 $body+='class '+$name+' { const string HarmonyId="probe"; const BindingFlags Flags=BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public;static '+$name+' active;HarmonyInstance harmony;bool disposed,installed,installationStarted,creditPending;AddPet petUpdateSource;bool InSynchronousCallback;MethodInfo method=null;List<string> errors=new List<string>();int entries=1,exits=1,previews=1,finishes=1,finishExits=1,advances=1,rebinds=1,moneyCredits=1,outsideFinishRestEntries,outsideFinishRestExits;HashSet<UnitDescriptor> expectedRest=new HashSet<UnitDescriptor>(),restEntered=new HashSet<UnitDescriptor>(),restExited=new HashSet<UnitDescriptor>();class Hooks{public static void Before(){}public static void After(){}}void Patch(Type type,string name,int token,Type[] arguments,string before,string after){harmony.Patch(type,name,token,arguments,before,after);}'
 $body+='internal void Install(){'+$prefix+$tail+'}'+$dispose+$complete
 $body+='public static int Verify(){int passed=0;HarmonyInstance.Reset();var owner=new '+$name+'();if(owner.Complete)throw new Exception("Capture-only observer completed");passed++;bool refused=false;try{owner.Install();}catch(InvalidOperationException){refused=true;}if(!refused||active!=owner||owner.Complete||HarmonyInstance.LiveHooks!=1||HarmonyInstance.UnpatchCalls!=0)throw new Exception("Partial installation lost caller custody");passed++;refused=false;try{owner.Install();}catch(InvalidOperationException){refused=true;}if(!refused||HarmonyInstance.PatchCalls!=1)throw new Exception("Partial install replayed");passed++;refused=false;try{owner.Dispose();}catch(InvalidOperationException){refused=true;}if(!refused||active!=owner||owner.disposed||HarmonyInstance.LiveHooks!=1)throw new Exception("Failed unpatch dropped owner");passed++;owner.Dispose();if(active!=null||!owner.disposed||HarmonyInstance.LiveHooks!=0)throw new Exception("Retry did not settle");passed++;owner.Dispose();if(HarmonyInstance.UnpatchCalls!=2)throw new Exception("Settled cleanup repeated");passed++;refused=false;try{owner.Install();}catch(ObjectDisposedException){refused=true;}if(!refused)throw new Exception("Disposed observer reacquired");passed++;HarmonyInstance.Reset();HarmonyInstance.PatchFailureAt=0;HarmonyInstance.UnpatchFailures=0;owner=new '+$name+'();owner.Install();int patchCount=HarmonyInstance.PatchCalls;if(!owner.Complete)throw new Exception("Completed observed fixture remains incomplete");passed++;owner.Install();if(HarmonyInstance.PatchCalls!=patchCount)throw new Exception("Installed observer repeated patches");passed++;var neighbour=new '+$name+'();refused=false;try{neighbour.Install();}catch(InvalidOperationException){refused=true;}neighbour.Dispose();if(!refused||active!=owner||HarmonyInstance.UnpatchCalls!=0)throw new Exception("Foreign observer released owner");passed++;owner.InSynchronousCallback=true;refused=false;try{owner.Dispose();}catch(InvalidOperationException){refused=true;}if(!refused||owner.disposed||HarmonyInstance.UnpatchCalls!=0)throw new Exception("Active native callback lost custody");passed++;owner.InSynchronousCallback=false;owner.Dispose();HarmonyInstance.Reset();owner=new '+$name+'();owner.Dispose();if(HarmonyInstance.UnpatchCalls!=0||!owner.disposed)throw new Exception("Never-installed observer mutated patch state");passed++;Console.WriteLine("PASS '+$name+' actual installation/disposal="+passed);return passed;}}'
 $inputs+=@{path=$path;sha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()}
}
$body+='class Program{static int Main(){int pass=0,fail=0;'+(($names|ForEach-Object{'try{pass+='+$_+'.Verify();}catch(Exception error){fail++;Console.WriteLine("FAIL "+error);}'})-join'')+'Console.WriteLine("PROGRESSION ACTUAL PROBE CUSTODY PASS="+pass+" FAIL="+fail);return fail==0?0:1;}}'
$cs=Join-Path $OutputDirectory 'ActualProbe.cs';[IO.File]::WriteAllText($cs,$body,[Text.UTF8Encoding]::new($false))
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe';$exe=Join-Path $OutputDirectory 'check.exe';$log=Join-Path $OutputDirectory 'result.log'
$timer=[Diagnostics.Stopwatch]::StartNew();& $compiler /nologo /target:exe /langversion:7.3 /nowarn:0649,0169,0414 "/out:$exe" $cs 2>&1|Tee-Object -FilePath $log;$compile=$LASTEXITCODE;$test=-1
if($compile-eq0){& $exe 2>&1|Tee-Object -FilePath $log -Append;$test=$LASTEXITCODE};$timer.Stop()
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'receipt.json'),(@{compile=$compile;test=$test;elapsedSeconds=$timer.Elapsed.TotalSeconds;inputs=$inputs;nativeProof=$false;boundary='Actual Install guards/tail, Complete and Dispose; native reflection and patch provider are external stubs.'}|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
if($compile-ne0-or$test-ne0){throw ('Private observer custody regression failed: '+$OutputDirectory)}
[ordered]@{status='PASS';elapsedSeconds=$timer.Elapsed.TotalSeconds;output=$OutputDirectory;nativeProof=$false}|ConvertTo-Json
