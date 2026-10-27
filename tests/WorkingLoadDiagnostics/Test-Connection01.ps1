[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$out=[IO.Path]::GetFullPath($OutputDirectory)
if(-not$out.StartsWith($PSScriptRoot+'\',[StringComparison]::OrdinalIgnoreCase)-or(Test-Path -LiteralPath $out)){throw 'Fresh private connection-test output required.'}
$repo=Join-Path $PSScriptRoot 'source-tree'
$compiler=Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe'
$framework=Join-Path ${env:ProgramFiles(x86)} 'Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7'
$json='C:\Program Files (x86)\Steam\steamapps\common\Pathfinder Kingmaker\Kingmaker_Data\Managed\Newtonsoft.Json.dll'
$jsonDependencies=@('UnityEngine.dll','UnityEngine.CoreModule.dll')|ForEach-Object{Join-Path (Split-Path -Parent $json) $_}
$references=@('mscorlib.dll','System.dll','System.Core.dll')|ForEach-Object{Join-Path $framework $_};$references+=@($json)
$protocol=Join-Path $PSScriptRoot 'source-tree/src/KingmakerMountedCombat/Diagnostics/RuntimeProtocol.cs'
$loader=Join-Path $PSScriptRoot 'source-tree/src/KingmakerMountedCombat/Diagnostics/WorkingFixtureLoader.cs'
$writer=Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/RuntimeLoaderInventory.cs'
$deadline=Join-Path $repo 'src/KingmakerMountedCombat/Domain/HorseCompanionScenarioDeadlinePolicy.cs'
$template=Join-Path $PSScriptRoot 'WorkingLoadConnectionTests.cs.template'
$complete=@('RuntimeAutomationHost.WorkingLoadObservation.cs','WorkingLoadDiagnosticSession.cs','WorkingLoadObservation.cs')|ForEach-Object{Join-Path $PSScriptRoot ('src/'+$_)}
$inputs=@($PSCommandPath,$protocol,$loader,$writer,$deadline,$template)+$complete+@($compiler)+$references+$jsonDependencies
$pins=@($inputs|ForEach-Object{[ordered]@{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash.ToLowerInvariant()}})
function Between([string]$text,[string]$start,[string]$end){$a=$text.IndexOf($start,[StringComparison]::Ordinal);if($a-lt0-or$text.IndexOf($start,$a+1,[StringComparison]::Ordinal)-ge0){throw 'Extraction start not unique.'};$b=$text.IndexOf($end,$a+$start.Length,[StringComparison]::Ordinal);if($b-lt0){throw 'Extraction end absent.'};$text.Substring($a,$b-$a).TrimEnd()}
$p=[IO.File]::ReadAllText($protocol);$l=[IO.File]::ReadAllText($loader);$w=[IO.File]::ReadAllText($writer)
$parts=[ordered]@{
 '__OPTION_PROPERTY__'=(Between $p 'public bool? ObserveWorkingDeserialization' 'public RuntimeSaveDescriptor PersistenceLoad')
 '__ADMISSION_PREFIX__'=(Between $p 'if (ObserveWorkingDeserialization.HasValue' 'if (SchemaVersion != CurrentSchemaVersion')
 '__LOADER_START__'=(Between $l 'public void Start(Action<Game, SaveManager, SaveInfo>' 'public bool TryCompleteVerification()')
 '__ARTIFACT_WRITER__'=(Between $w 'internal static void WriteArtifact(' 'private static string Hash(')
 '__ARTIFACT_HASH__'=(Between $w 'private static string Hash(' "`n    }")
}
[void][IO.Directory]::CreateDirectory($out);$snap=Join-Path $out 'source-inputs';[void][IO.Directory]::CreateDirectory($snap)
foreach($file in @($protocol,$loader,$writer,$deadline,$template)+$complete){Copy-Item -LiteralPath $file -Destination (Join-Path $snap ([IO.Path]::GetFileName($file)))}
$generated=[IO.File]::ReadAllText($template);foreach($key in $parts.Keys){if(([regex]::Matches($generated,[regex]::Escape($key))).Count-ne1){throw 'Template marker not unique.'};$generated=$generated.Replace($key,$parts[$key])}
$source=Join-Path $out 'WorkingLoadConnectionTests.cs';[IO.File]::WriteAllText($source,$generated,[Text.UTF8Encoding]::new($false))
Copy-Item -LiteralPath $json -Destination (Join-Path $out 'Newtonsoft.Json.dll')
foreach($dependency in $jsonDependencies){Copy-Item -LiteralPath $dependency -Destination (Join-Path $out ([IO.Path]::GetFileName($dependency)))}
function Run([string]$exe,[string[]]$arguments,[string]$label){
 $i=[Diagnostics.ProcessStartInfo]::new();$i.FileName=$exe;$i.Arguments=($arguments|ForEach-Object{'"'+$_+'"'})-join' ';$i.WorkingDirectory=$out;$i.UseShellExecute=$false;$i.CreateNoWindow=$true;$i.RedirectStandardOutput=$true;$i.RedirectStandardError=$true;$i.EnvironmentVariables['TEMP']=$out;$i.EnvironmentVariables['TMP']=$out
 $p=[Diagnostics.Process]::new();$p.StartInfo=$i;$watch=[Diagnostics.Stopwatch]::StartNew();try{if(-not$p.Start()){throw 'Focused child did not start.'};$id=$p.Id;$start=$p.StartTime.ToUniversalTime().ToString('o');$so=$p.StandardOutput.ReadToEndAsync();$se=$p.StandardError.ReadToEndAsync();$p.WaitForExit();$watch.Stop();$code=$p.ExitCode;$stdout=$so.Result;$stderr=$se.Result}finally{$p.Dispose()}
 $a=Join-Path $out ($label+'.stdout.log');$b=Join-Path $out ($label+'.stderr.log');[IO.File]::WriteAllText($a,$stdout);[IO.File]::WriteAllText($b,$stderr)
 [ordered]@{executable=$exe;arguments=$arguments;requestedRawArguments=$i.Arguments;workingDirectory=$out;processId=$id;startedAtUtc=$start;exitCode=$code;seconds=$watch.Elapsed.TotalSeconds;stdout=$a;stdoutSha256=(Get-FileHash -LiteralPath $a).Hash.ToLowerInvariant();stderr=$b;stderrSha256=(Get-FileHash -LiteralPath $b).Hash.ToLowerInvariant()}
}
$exe=Join-Path $out 'WorkingLoadConnectionTests.exe'
$arguments=@('/nologo','/noconfig','/nostdlib+','/target:exe','/langversion:7.3','/warnaserror+',('/out:'+$exe))+@($references|ForEach-Object{'/reference:'+$_})+@($source,$deadline)+$complete
$compile=Run $compiler $arguments 'compile';$run=$null;$failure=$null;$passed=$null;$failed=$null
if($compile.exitCode-ne0){$failure='Connection adapter compilation failed.'}else{$run=Run $exe @($out) 'test';$raw=[IO.File]::ReadAllText($run.stdout);$matches=@([regex]::Matches($raw,'(?m)^WORKING LOAD CONNECTION PASS=(\d+) FAIL=(\d+)\r?$'));if($matches.Count-ne1){$failure='Unique connection terminal absent.'}else{$passed=[int]$matches[0].Groups[1].Value;$failed=[int]$matches[0].Groups[2].Value};if($run.exitCode-ne0-or$failed-ne0){$failure='Actual connection checks failed.'}}
$same=$true;foreach($pin in $pins){if((Get-FileHash -LiteralPath $pin.path).Hash.ToLowerInvariant()-cne$pin.sha256){$same=$false}};if(-not$same){$failure='Connection input changed.'}
$receipt=[ordered]@{schemaVersion=1;kind='actual-private-working-load-host-connection';status=$(if($null-eq$failure){'PASS'}else{'FAIL'});failure=$failure;passed=$passed;failed=$failed;inputPins=$pins;inputsUnchanged=$same;extractedBodies=@($parts.Keys);generatedSourceSha256=(Get-FileHash -LiteralPath $source).Hash.ToLowerInvariant();compile=$compile;test=$run;limits=@('Actual complete host partial, session, recorder and deadline sources. Exact extracted loader Start, new request predicate/property and existing artifact writer.','Native/Harmony adapter, descriptor read/verification, hash and unrelated host completion are explicit external substitutes. Loader uses only generated plain text fixture metadata; no archive operation.','Original unrelated request guards are not substituted proof or rerun; production compiler/source review remains separate.');nativeInvocation=$false;archiveOperation=$false;rootChanged=$false}
[IO.File]::WriteAllText((Join-Path $out 'receipt.json'),($receipt|ConvertTo-Json -Depth 9),[Text.UTF8Encoding]::new($false))
if($null-ne$run){[IO.File]::ReadAllText($run.stdout)}else{[IO.File]::ReadAllText($compile.stdout)}
if($null-ne$failure){throw $failure}
