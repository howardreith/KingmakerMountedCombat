[CmdletBinding()]
param([string]$RepositoryRoot,[string]$SourceOverlayRoot,[string]$SourceCompositionPath,[string]$OutputRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if([string]::IsNullOrWhiteSpace($RepositoryRoot)){throw 'RepositoryRoot must not be blank.'}
$RepositoryRoot=[IO.Path]::GetFullPath($RepositoryRoot)
$analysisRoot=[IO.Path]::GetFullPath((Join-Path $RepositoryRoot '../../analysis-cache'))
if(-not$PSBoundParameters.ContainsKey('OutputRoot')){$OutputRoot=Join-Path $analysisRoot ('chunk7-assisted-emitter-'+[Guid]::NewGuid().ToString('N'))}
if([string]::IsNullOrWhiteSpace($OutputRoot)){throw 'OutputRoot must not be blank.'}
$OutputRoot=[IO.Path]::GetFullPath($OutputRoot)
if(-not$OutputRoot.StartsWith($analysisRoot.TrimEnd('\')+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Focused generated output must remain under this lab analysis-cache.'}
if(Test-Path -LiteralPath $OutputRoot){throw 'Preserve existing assisted core output.'}
[void][IO.Directory]::CreateDirectory($OutputRoot)
$inputs=@{};$compositionEntries=@{}
function Pin([string]$path){$full=[IO.Path]::GetFullPath($path);$inputs[$full]=(Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash.ToLowerInvariant();return $full}
[void](Pin $PSCommandPath)
if(-not[string]::IsNullOrWhiteSpace($SourceCompositionPath)){
 [void](Pin $SourceCompositionPath)
 foreach($entry in @((Get-Content -LiteralPath $SourceCompositionPath -Raw|ConvertFrom-Json).files)){
  if($compositionEntries.ContainsKey($entry.path)){throw ('Duplicate composition path '+$entry.path)};$compositionEntries[$entry.path]=$entry
 }
}
function Source([string]$relative){
 $path=$null
 if(-not[string]::IsNullOrWhiteSpace($SourceOverlayRoot)){$p=Join-Path $SourceOverlayRoot $relative;if(Test-Path -LiteralPath $p -PathType Leaf){$path=$p}}
 if($null-eq$path-and$compositionEntries.ContainsKey($relative)){$e=$compositionEntries[$relative];$path=$e.sourcePath;if((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()-cne$e.sha256){throw ('Composition input changed '+$relative)}}
 if($null-eq$path){$path=Join-Path $RepositoryRoot $relative};return (Pin $path)
}
function Run-Child([string]$exe,[string[]]$arguments,[string]$stem){
 # All executable inputs here are the exact compiler or generated bounded tests.
 # There is no runtime/game/launcher entry and no archive operation.
 foreach($a in $arguments){if($a.Contains('"')){throw 'Unexpected quote in focused argument.'}}
 $psi=[Diagnostics.ProcessStartInfo]::new();$psi.FileName=$exe;$psi.Arguments=(@($arguments|ForEach-Object{'"'+$_+'"'})-join' ');$psi.WorkingDirectory=$OutputRoot
 $psi.UseShellExecute=$false;$psi.CreateNoWindow=$true;$psi.WindowStyle=[Diagnostics.ProcessWindowStyle]::Hidden;$psi.RedirectStandardOutput=$true;$psi.RedirectStandardError=$true
 $psi.EnvironmentVariables['TEMP']=$OutputRoot;$psi.EnvironmentVariables['TMP']=$OutputRoot
 $p=[Diagnostics.Process]::new();$p.StartInfo=$psi;$clock=[Diagnostics.Stopwatch]::StartNew()
 try{
  if(-not$p.Start()){throw 'Focused child failed to start.'};$id=$p.Id;$started=$p.StartTime.ToUniversalTime().ToString('o')
  $out=$p.StandardOutput.ReadToEndAsync();$err=$p.StandardError.ReadToEndAsync();$p.WaitForExit()
  $stdout=$out.GetAwaiter().GetResult();$stderr=$err.GetAwaiter().GetResult();$exitCode=$p.ExitCode;$seconds=$clock.Elapsed.TotalSeconds
  [IO.File]::WriteAllText((Join-Path $OutputRoot ($stem+'.stdout.log')),$stdout,[Text.UTF8Encoding]::new($false));[IO.File]::WriteAllText((Join-Path $OutputRoot ($stem+'.stderr.log')),$stderr,[Text.UTF8Encoding]::new($false))
  return [pscustomobject]@{executable=$exe;argumentList=$arguments;cwd=$OutputRoot;processId=$id;startedUtc=$started;exitCode=$exitCode;elapsedSeconds=$seconds;stdout=$stdout;stderr=$stderr}
 }finally{$p.Dispose()}
}

$names=@('Chunk7AssistedFixtureBinding.cs','Chunk7AssistedStoryDriver.cs','Chunk7AssistedShortfall.cs','Chunk7AssistedFixtureEvidence.cs')
$sourcePaths=@{};foreach($n in $names){$sourcePaths[$n]=Source ('src/KingmakerMountedCombat/Diagnostics/'+$n)}
$test=Source 'tests/Chunk7AssistedFixtureEvidenceEmitterTests.cs'
$compiler=Pin 'C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$refRoot='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7';$references=@('mscorlib','System','System.Core'|ForEach-Object{Pin (Join-Path $refRoot ($_+'.dll'))})
$managed='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed'
$json=Pin (Join-Path $managed 'Newtonsoft.Json.dll');$unity=Pin (Join-Path $managed 'UnityEngine.dll');$unityCore=Pin (Join-Path $managed 'UnityEngine.CoreModule.dll')
# Exact managed dependencies are resolved in place for the installed serializer's
# Boolean converter initialization. No game/Unity native method is invoked.
$loaderText='using System;using System.Reflection;using System.Collections.Generic;static class Loader {static int Main(string[] args){if(args.Length!=1)throw new Exception("Expected generated test executable");var paths=new[]{@"'+$json+'",@"'+$unity+'",@"'+$unityCore+'"};var map=new Dictionary<string,string>(StringComparer.Ordinal);foreach(var p in paths)map.Add(AssemblyName.GetAssemblyName(p).FullName,p);AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>{string p;return map.TryGetValue(e.Name,out p)?Assembly.LoadFrom(p):null;};var a=Assembly.LoadFrom(args[0]);return (int)a.EntryPoint.Invoke(null,null);}}'
$loaderPath=Join-Path $OutputRoot 'Loader.cs';[IO.File]::WriteAllText($loaderPath,$loaderText,[Text.UTF8Encoding]::new($false));$loader=Join-Path $OutputRoot 'Loader.exe'
$baseArgs=@('/nologo','/nostdlib+','/target:exe','/optimize+','/langversion:7.3')
$loaderArgs=$baseArgs+@(('/out:'+$loader),('/reference:'+$references[0]),$loaderPath);$loaderRsp=Join-Path $OutputRoot 'loader.rsp';[IO.File]::WriteAllLines($loaderRsp,@($loaderArgs|ForEach-Object{'"'+$_+'"'}),[Text.UTF8Encoding]::new($false))

$stages=[Collections.Generic.List[object]]::new();$stages.Add((Run-Child $compiler @('/noconfig',('@'+$loaderRsp)) 'loader-compile'))
$exe=Join-Path $OutputRoot 'emitter.exe';$rsp=Join-Path $OutputRoot 'emitter.rsp'
$compileArguments=$baseArgs+@(('/out:'+$exe),('/reference:'+$json))+@($references|ForEach-Object{'/reference:'+$_})+@($names|ForEach-Object{$sourcePaths[$_]})+@($test)
[IO.File]::WriteAllLines($rsp,@($compileArguments|ForEach-Object{'"'+$_+'"'}),[Text.UTF8Encoding]::new($false))
$passed=0;$failed=0;$outputs=@();$ran=$false
if($stages[0].exitCode-eq0){
 $c=Run-Child $compiler @('/noconfig',('@'+$rsp)) 'emitter-compile';$stages.Add($c)
 if($c.exitCode-eq0){
  $r=Run-Child $loader @($exe) 'emitter-run';$stages.Add($r);$ran=$true
  $lines=@($r.stdout-split'\r?\n'|Where-Object{$_-ne''});$expected='ASSISTED ACTUAL EMITTER: 7 PASS / 0 FAIL'
  $shape=$r.exitCode-eq0-and$r.stderr-ceq''-and$lines.Count-gt0-and$lines[-1]-ceq$expected-and@($lines|Where-Object{$_-ceq$expected}).Count-eq1-and@($lines|Where-Object{$_-clike'PASS *'}).Count-eq7-and@($lines|Where-Object{$_-clike'FAIL *'}).Count-eq0
  if($shape){$passed=7;Write-Host $expected}else{$failed=1;Write-Host 'FAIL actual assisted emitter child'}
 }
}
$leaves=@('ready-noop.json','click-xp-funds.json','xp-funds-sufficient.json','skip-disabled-first.json','system-button.json','partial-native-failure.json','same-cue-reentry.json')
foreach($leaf in $leaves){$p=Join-Path $OutputRoot $leaf;if(Test-Path -LiteralPath $p -PathType Leaf){$outputs+= [ordered]@{path=$p;leaf=$leaf;sha256=(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant();length=([IO.FileInfo]$p).Length;expectedReaderStatus=$(if($leaf-ceq'partial-native-failure.json'){'FAIL'}else{'PASS'})}}}
$changed=@($inputs.Keys|Where-Object{(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant()-cne$inputs[$_]})
$all=$passed-eq7-and$failed-eq0-and$ran-and$stages.Count-eq3-and$outputs.Count-eq7-and@($stages|Where-Object{$_.exitCode-ne0-or$_.stdout-match'CS2023'}).Count-eq0-and$changed.Count-eq0
$rows=@($inputs.Keys|Sort-Object|ForEach-Object{[ordered]@{path=$_;sha256=$inputs[$_]}})
[ordered]@{schemaVersion=1;kind='actual-assisted-setup-portable-emitter';status=$(if($all){'PASS'}else{'FAIL'});passed=$passed;failed=$failed;stages=@($stages);inputs=$rows;changedInputs=$changed;fixtures=$outputs;expected=[ordered]@{runId='assisted-emitter-generated';gameId='11111111-2222-3333-4444-555555555555';gameName='KMC_C7_ASSISTED_NATIVE';mainActorId='aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee';authorityGeneration=1};nativeEngineInvoked=$false;archiveOperations=0;limits=@('Exact four current production sources emit all JSON; no hand-authored positive fixtures','External native/UI and between-effect native level progression are generated Rig boundaries only','Canonical IDs are fixed synthetic data, not source/native bindings','Six intended reader positives (four cached-row, one system-button and one two-click same-cue re-entry) and one retained partial-failure negative; producer success does not validate reader or native qualification')}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $OutputRoot 'manifest.json') -Encoding UTF8
Write-Host ('ASSISTED EMITTER PORTABLE: '+$passed+' PASS / '+$failed+' FAIL')
if(-not$all){exit 1};exit 0
