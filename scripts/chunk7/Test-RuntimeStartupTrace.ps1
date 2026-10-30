[CmdletBinding()]
param([string]$RepositoryRoot,[string]$SourceOverlayRoot,[string]$OutputRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
$RepositoryRoot=[IO.Path]::GetFullPath($RepositoryRoot)
$cacheRoot=[IO.Path]::GetFullPath((Join-Path $RepositoryRoot '../../analysis-cache'))
if(-not$PSBoundParameters.ContainsKey('OutputRoot')){$OutputRoot=Join-Path $cacheRoot ('runtime-startup-'+[Guid]::NewGuid().ToString('N'))}
$OutputRoot=[IO.Path]::GetFullPath($OutputRoot)
if(-not$OutputRoot.StartsWith($cacheRoot.TrimEnd('\')+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Startup test output must be a private analysis-cache child.'}
if(Test-Path -LiteralPath $OutputRoot){throw 'Startup output exists; preserve prior evidence.'}
[void][IO.Directory]::CreateDirectory($OutputRoot)
$inputs=@{}
function Pin-Startup([string]$p){$inputs[$p]=(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant();return $p}
function Source-Startup([string]$relative){if(-not[string]::IsNullOrWhiteSpace($SourceOverlayRoot)){$p=Join-Path $SourceOverlayRoot $relative;if(Test-Path -LiteralPath $p -PathType Leaf){return (Pin-Startup $p)}};return (Pin-Startup (Join-Path $RepositoryRoot $relative))}
function Member-Startup([string]$text,[string]$signature){
 $start=$text.IndexOf($signature,[StringComparison]::Ordinal)
 if($start-lt0-or$text.IndexOf($signature,$start+$signature.Length,[StringComparison]::Ordinal)-ge0){throw ('Missing/ambiguous exact member '+$signature)}
 $open=$text.IndexOf('{',$start);$depth=0
 for($i=$open;$i-lt$text.Length;$i++){if($text[$i]-eq'{'){$depth++}elseif($text[$i]-eq'}'){$depth--;if($depth-eq0){return $text.Substring($start,$i-$start+1)}}}
 throw ('Unclosed exact member '+$signature)
}
function Expression-Startup([string]$text,[string]$signature){$start=$text.IndexOf($signature,[StringComparison]::Ordinal);if($start-lt0-or$text.IndexOf($signature,$start+$signature.Length,[StringComparison]::Ordinal)-ge0){throw ('Missing expression '+$signature)};$end=$text.IndexOf(';',$start);if($end-lt0){throw 'Unclosed expression'};return $text.Substring($start,$end-$start+1)}
function Write-StartupFile([string]$name,[string]$text){$p=Join-Path $OutputRoot $name;[IO.File]::WriteAllText($p,$text,(New-Object Text.UTF8Encoding($false)));return $p}
[void](Pin-Startup $PSCommandPath)
$main=Source-Startup 'src/KingmakerMountedCombat/Main.cs';$composition=Source-Startup 'src/KingmakerMountedCombat/CompositionRoot.cs';$hostSource=Source-Startup 'src/KingmakerMountedCombat/Diagnostics/RuntimeAutomationHost.cs';$trace=Source-Startup 'src/KingmakerMountedCombat/Diagnostics/RuntimeStartupTrace.cs';$templatePath=Source-Startup 'tests/Chunk7Diagnostics/RuntimeStartupTraceTemplate.cs';$readerTest=Source-Startup 'scripts/chunk7/Test-RuntimeStartupEvidence.ps1'
[void](Source-Startup 'scripts/runtime/RuntimeStartupEvidence.ps1');[void](Source-Startup 'scripts/runtime/Test-RuntimeGameResult.ps1');[void](Source-Startup 'scripts/runtime/RuntimeHarness.Common.ps1')
$mainText=[IO.File]::ReadAllText($main);$compositionText=[IO.File]::ReadAllText($composition);$hostText=[IO.File]::ReadAllText($hostSource)
$hostUpdate=Member-Startup $hostText 'public void Update(float deltaTime)';$marker='elapsedSeconds = runtimeClock.Elapsed.TotalSeconds;';$end=$hostUpdate.IndexOf($marker,[StringComparison]::Ordinal);if($end-lt0){throw 'Host prefix marker missing'};$hostPrefix=$hostUpdate.Substring(0,$end+$marker.Length)
$hostTrace=(@('internal void ObserveStartupCallbackEntry()','internal void ObserveStartupCallbackExit()','internal void ObserveStartupPreHostStart()','internal void ObserveStartupPreHostEnd()')|ForEach-Object{Expression-Startup $hostText $_})-join[Environment]::NewLine
$compTrace=(@('internal void ObserveStartupCallbackEntry()','internal void ObserveStartupCallbackExit()')|ForEach-Object{Expression-Startup $compositionText $_})-join[Environment]::NewLine
$actual=[IO.File]::ReadAllText($templatePath).Replace('__MAIN_METHOD__',(Member-Startup $mainText 'private static void OnUpdate(')).Replace('__COMP_UPDATE__',(Member-Startup $compositionText 'public void Update(float deltaTime)')).Replace('__COMP_CORE__',(Member-Startup $compositionText 'private void UpdateCore(float deltaTime)')).Replace('__COMP_TRACE__',$compTrace).Replace('__HOST_TRACE__',$hostTrace).Replace('__HOST_UPDATE_PREFIX__',$hostPrefix).Replace('__JSON_SETTINGS__',((Member-Startup $hostText 'private static readonly JsonSerializerSettings JsonSettings')+';')).Replace('__RESULT1__',(Member-Startup $hostText ([regex]::Match($hostText,'private sealed class RuntimeGameResult\s*\{').Value.TrimEnd('{')))).Replace('__RESULT2__',(Member-Startup $hostText 'private sealed class RuntimeGameResultV2'))
if($actual-match'__[A-Z_]+__'){throw 'Unexpanded startup test source marker.'}
$actualPath=Write-StartupFile 'actual.cs' $actual
$traceCopy=Write-StartupFile 'RuntimeStartupTrace.cs' ([IO.File]::ReadAllText($trace))
$compiler=Pin-Startup 'C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$framework='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7'
$refs=@('mscorlib','System','System.Core','System.Runtime.Serialization'|ForEach-Object{Pin-Startup (Join-Path $framework ($_+'.dll'))})
$json=Pin-Startup 'C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll';$refs+=@($json)
$managed=Split-Path -Parent $json;$unity=Pin-Startup (Join-Path $managed 'UnityEngine.dll');$unityCore=Pin-Startup (Join-Path $managed 'UnityEngine.CoreModule.dll')
$exe=Join-Path $OutputRoot 'StartupActual.exe'
$compileArguments=@('/nologo','/target:exe','/langversion:7.3','/nowarn:0649','/nostdlib+','/deterministic+',('/out:'+$exe))+@($refs|ForEach-Object{'/reference:'+$_})+@($actualPath,$traceCopy)
$rsp=Write-StartupFile 'compile.rsp' (($compileArguments|ForEach-Object{[char]34+$_+[char]34})-join[Environment]::NewLine)
$loaderCode='using System;using System.Reflection;using System.Collections.Generic;static class Loader {static int Main(string[] args){if(args.Length!=2)throw new Exception("Expected generated executable and fixture directory");var paths=new[]{@"'+$json+'",@"'+$unity+'",@"'+$unityCore+'"};var map=new Dictionary<string,string>(StringComparer.Ordinal);foreach(var p in paths)map.Add(AssemblyName.GetAssemblyName(p).FullName,p);AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>{string p;return map.TryGetValue(e.Name,out p)?Assembly.LoadFrom(p):null;};var a=Assembly.LoadFrom(args[0]);return (int)a.EntryPoint.Invoke(null,new object[]{new string[]{args[1]}});}}'
$loaderSource=Write-StartupFile 'loader.cs' $loaderCode;$loaderExe=Join-Path $OutputRoot 'Loader.exe'
$loaderArgs=@('/nologo','/target:exe','/langversion:7.3','/nostdlib+',('/out:'+$loaderExe),('/reference:'+$refs[0]),$loaderSource)
$loaderRsp=Write-StartupFile 'loader.rsp' (($loaderArgs|ForEach-Object{[char]34+$_+[char]34})-join[Environment]::NewLine)
$clock=[Diagnostics.Stopwatch]::StartNew();$global:LASTEXITCODE=$null;$savedPreference=$ErrorActionPreference
try{$ErrorActionPreference='Continue';$compile=@(& $compiler '/noconfig' ('@'+$rsp) 2>&1);$compileExit=$global:LASTEXITCODE}finally{$ErrorActionPreference=$savedPreference}
[void](Write-StartupFile 'compile.log' (($compile|ForEach-Object{$_.ToString()})-join[Environment]::NewLine))
$global:LASTEXITCODE=$null
try{$ErrorActionPreference='Continue';$loaderCompile=@(& $compiler '/noconfig' ('@'+$loaderRsp) 2>&1);$loaderCompileExit=$global:LASTEXITCODE}finally{$ErrorActionPreference=$savedPreference}
[void](Write-StartupFile 'loader-compile.log' (($loaderCompile|ForEach-Object{$_.ToString()})-join[Environment]::NewLine))
$adapterExit=$null;$readerExit=$null;$adapterPass=0;$adapterFail=0;$readerPass=0;$readerFail=0;$run=@();$readerRun=@()
if($compileExit-is[int]-and$compileExit-eq0-and$loaderCompileExit-is[int]-and$loaderCompileExit-eq0){
 $global:LASTEXITCODE=$null;$fixtureRoot=Join-Path $OutputRoot 'fixtures'
 try{$ErrorActionPreference='Continue';$run=@(& $loaderExe $exe $fixtureRoot 2>&1);$adapterExit=$global:LASTEXITCODE}finally{$ErrorActionPreference=$savedPreference}
 $lines=@($run|ForEach-Object{$_.ToString()});$summary=@($lines|Where-Object{$_-cmatch'^STARTUP TRACE PASS=([0-9]+) FAIL=([0-9]+)$'})
 if($summary.Count-eq1-and$summary[0]-cmatch'^STARTUP TRACE PASS=([0-9]+) FAIL=([0-9]+)$'){$adapterPass=[int]$Matches[1];$adapterFail=[int]$Matches[2]}
 if($adapterExit-is[int]-and$adapterExit-eq0-and$adapterPass-eq45-and$adapterFail-eq0){
  $powershell=Pin-Startup (Join-Path $PSHOME 'powershell.exe');$readerArgs=@('-NoProfile','-NonInteractive','-File',$readerTest,'-RepositoryRoot',$RepositoryRoot,'-FixtureRoot',$fixtureRoot,'-OutputRoot',$OutputRoot)
  if(-not[string]::IsNullOrWhiteSpace($SourceOverlayRoot)){$readerArgs+=@('-SourceOverlayRoot',$SourceOverlayRoot)}
  $global:LASTEXITCODE=$null
  try{$ErrorActionPreference='Continue';$readerRun=@(& $powershell @readerArgs 2>&1);$readerExit=$global:LASTEXITCODE}finally{$ErrorActionPreference=$savedPreference}
  $readerLines=@($readerRun|ForEach-Object{$_.ToString()});$readerSummary=@($readerLines|Where-Object{$_-cmatch'^STARTUP READER PASS=([0-9]+) FAIL=([0-9]+)$'})
  if($readerSummary.Count-eq1-and$readerSummary[0]-cmatch'^STARTUP READER PASS=([0-9]+) FAIL=([0-9]+)$'){$readerPass=[int]$Matches[1];$readerFail=[int]$Matches[2]}
 }
}
[void](Write-StartupFile 'adapter.log' (($run|ForEach-Object{$_.ToString()})-join[Environment]::NewLine));[void](Write-StartupFile 'reader.log' (($readerRun|ForEach-Object{$_.ToString()})-join[Environment]::NewLine))
$clock.Stop();$run|ForEach-Object{Write-Output $_};$readerRun|ForEach-Object{Write-Output $_}
$changed=@($inputs.Keys|Where-Object{(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant()-cne$inputs[$_]})
$ok=$compileExit-is[int]-and$compileExit-eq0-and$loaderCompileExit-is[int]-and$loaderCompileExit-eq0-and$adapterExit-is[int]-and$adapterExit-eq0-and$adapterPass-eq45-and$adapterFail-eq0-and$readerExit-is[int]-and$readerExit-eq0-and$readerPass-eq61-and$readerFail-eq0-and$changed.Count-eq0
$outputs=@(Get-ChildItem -LiteralPath $OutputRoot -File -Recurse|Where-Object{$_.Extension-notin@('.exe','.dll')}|Sort-Object FullName|ForEach-Object{[ordered]@{path=$_.FullName;sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}})
$inputRows=@($inputs.Keys|Sort-Object|ForEach-Object{[ordered]@{path=$_;sha256=$inputs[$_]}})
$receipt=[ordered]@{schemaVersion=1;kind='runtime-startup-actual-method-and-reader-tests';status=$(if($ok){'PASS'}else{'FAIL'});compileExit=$compileExit;loaderCompileExit=$loaderCompileExit;serializerDependenciesResolvedInPlace=$true;adapterExit=$adapterExit;readerExit=$readerExit;adapterPass=$adapterPass;adapterFail=$adapterFail;readerPass=$readerPass;readerFail=$readerFail;durationSeconds=$clock.Elapsed.TotalSeconds;inputs=$inputRows;outputs=$outputs;changedInputs=$changed;compilerNoConfigOnArgv=$true;actualMainAndCompositionMethods=$true;actualHostEntryPrefixAndResultDtos=$true;actualRecorder=$true;actualJsonSettings=$true;externalNativeServicesUnityClockAndRestOfHostFacaded=$true;fullGameResultQualificationExecuted=$false;nativeInvoked=$false;archiveOperations=0}
[void](Write-StartupFile 'receipt.json' ($receipt|ConvertTo-Json -Depth 9))
if(-not$ok){exit 1};exit 0
