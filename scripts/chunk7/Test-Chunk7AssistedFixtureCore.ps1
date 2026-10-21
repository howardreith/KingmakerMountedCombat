[CmdletBinding()]
param([string]$RepositoryRoot,[string]$SourceOverlayRoot,[string]$SourceCompositionPath,[string]$OutputRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if([string]::IsNullOrWhiteSpace($RepositoryRoot)){throw 'RepositoryRoot must not be blank.'}
$RepositoryRoot=[IO.Path]::GetFullPath($RepositoryRoot)
$analysisRoot=[IO.Path]::GetFullPath((Join-Path $RepositoryRoot '../../analysis-cache'))
if(-not$PSBoundParameters.ContainsKey('OutputRoot')){$OutputRoot=Join-Path $analysisRoot ('chunk7-assisted-core-'+[Guid]::NewGuid().ToString('N'))}
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
$names=@('Chunk7AssistedFixtureBinding.cs','Chunk7AssistedStoryDriver.cs','Chunk7AssistedShortfall.cs','Chunk7AssistedFixtureEvidence.cs','Chunk7AssistedFixtureNativeBoundary.cs')
$sourcePaths=@{};foreach($n in $names){$sourcePaths[$n]=Source ('src/KingmakerMountedCombat/Diagnostics/'+$n)}
$core=Source 'tests/Chunk7Diagnostics/AssistedFixtureCoreTests.cs';$affected=Source 'tests/Chunk7Diagnostics/AssistedFixtureEvidenceReadinessTests.cs';$nativeFacade=Source 'tests/Chunk7Diagnostics/AssistedFixtureNativeFacade.cs';$nativeTests=Source 'tests/Chunk7Diagnostics/AssistedFixtureNativeFacadeTests.cs'
$nativeText=[IO.File]::ReadAllText($sourcePaths['Chunk7AssistedFixtureNativeBoundary.cs']);$marker='        private static T ReadField<T>';$split=$nativeText.IndexOf($marker,[StringComparison]::Ordinal)
if($split-lt0-or$nativeText.IndexOf($marker,$split+$marker.Length,[StringComparison]::Ordinal)-ge0){throw 'Exact adapter external metadata helper boundary changed.'}
# Only the installed reflection token/MVID helpers are substituted. Every
# constructor/read/call method before this seam remains byte-identical.
$nativeActual=$nativeText.Substring(0,$split)+@'
        private static T ReadField<T>(Type type,object owner,string name,int token){return Fixture.Field<T>(type,owner,name,token);}
        private static void RequireMethod(Type type,string name,int token,Type returns,Type[] parameters){Fixture.Method(type,name,token,returns,parameters);}
    }
}
'@
$nativePath=Join-Path $OutputRoot 'NativeBoundary.cs';[IO.File]::WriteAllText($nativePath,$nativeActual,[Text.UTF8Encoding]::new($false))
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
$jobs=@(
 [pscustomobject]@{name='core';test=$core;count=124;prefix='ASSISTED FIXTURE CORE';facade=$false},
 [pscustomobject]@{name='affected';test=$affected;count=19;prefix='ASSISTED AFFECTED';facade=$false},
 [pscustomobject]@{name='native-facade';test=$nativeTests;count=106;prefix='ASSISTED NATIVE FACADE';facade=$true}
)
$passed=0;$failed=0
if($stages[0].exitCode-eq0){foreach($job in $jobs){
 $sources=@($sourcePaths['Chunk7AssistedFixtureBinding.cs'],$sourcePaths['Chunk7AssistedStoryDriver.cs'],$sourcePaths['Chunk7AssistedShortfall.cs'],$job.test)
 $refs=$references;if($job.facade){$sources+=@($nativePath,$nativeFacade)}else{$sources+=@($sourcePaths['Chunk7AssistedFixtureEvidence.cs']);$refs+=@($json)}
 $exe=Join-Path $OutputRoot ($job.name+'.exe');$rsp=Join-Path $OutputRoot ($job.name+'.rsp');$compileArguments=$baseArgs+@(('/out:'+$exe))+@($refs|ForEach-Object{'/reference:'+$_})+$sources
 [IO.File]::WriteAllLines($rsp,@($compileArguments|ForEach-Object{'"'+$_+'"'}),[Text.UTF8Encoding]::new($false));$c=Run-Child $compiler @('/noconfig',('@'+$rsp)) ($job.name+'-compile');$stages.Add($c)
 if($c.exitCode-ne0){continue}
 $r=Run-Child $loader @($exe) ($job.name+'-run');$stages.Add($r);$lines=@($r.stdout-split'\r?\n'|Where-Object{$_-ne''});$expected=$job.prefix+': '+$job.count+' PASS / 0 FAIL'
 $ok=$r.exitCode-eq0-and$r.stderr-ceq''-and$lines.Count-gt0-and$lines[-1]-ceq$expected-and@($lines|Where-Object{$_-ceq$expected}).Count-eq1-and@($lines|Where-Object{$_-clike'PASS *'}).Count-eq$job.count-and@($lines|Where-Object{$_-clike'FAIL *'}).Count-eq0
 if($ok){$passed+=$job.count;Write-Host $expected}else{$failed++;Write-Host ('FAIL focused stage '+$job.name)}
}}
$changed=@($inputs.Keys|Where-Object{(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant()-cne$inputs[$_]})
$all=$passed-eq249-and$failed-eq0-and$stages.Count-eq7-and@($stages|Where-Object{$_.exitCode-ne0}).Count-eq0-and$changed.Count-eq0
$rows=@($inputs.Keys|Sort-Object|ForEach-Object{[ordered]@{path=$_;sha256=$inputs[$_]}})
[ordered]@{schemaVersion=1;kind='actual-assisted-core-portable';status=$(if($all){'PASS'}else{'FAIL'});passed=$passed;failed=$failed;stages=@($stages);inputs=$rows;changedInputs=$changed;nativeEngineInvoked=$false;archiveOperations=0;limits=@('Actual services and formatter; generated external game/UI/effect objects','Exact NativeBoundary constructor/read/call prefix, only metadata helper tail substituted','Installed serializer managed dependencies read in place; no engine calls or proprietary copies','No connected host, fixture qualification or native assistance proof')}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $OutputRoot 'receipt.json') -Encoding UTF8
Write-Host ('ASSISTED CORE PORTABLE: '+$passed+' PASS / '+$failed+' FAIL')
if(-not$all){exit 1};exit 0
