[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderPath,[string]$TestSourcePath,[Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=Split-Path -Parent $PSScriptRoot}
if(-not$PSBoundParameters.ContainsKey('ReaderPath')){$ReaderPath=Join-Path $RepositoryRoot 'scripts/runtime/WorkingActorGraphReader.cs'}
if(-not$PSBoundParameters.ContainsKey('TestSourcePath')){$TestSourcePath=Join-Path $RepositoryRoot 'tests/WorkingActorGraphPartialTests.cs'}
foreach($value in @($RepositoryRoot,$ReaderPath,$TestSourcePath,$OutputDirectory)){if([string]::IsNullOrWhiteSpace($value)-or$value.Contains('"')-or$value.Contains("`n")-or$value.Contains("`r")){throw 'Exact nonempty test paths are required.'}}
$analysis='C:\Dev\KingmakerMountedCombatLab\analysis-cache'
$out=[IO.Path]::GetFullPath($OutputDirectory).TrimEnd('\')
if(-not$out.StartsWith($analysis+'\',[StringComparison]::OrdinalIgnoreCase)-or(Test-Path -LiteralPath $out)){throw 'Focused structure tests require a fresh analysis-cache output directory.'}
$ancestor=Split-Path -Parent $out
while($ancestor.Length-ge$analysis.Length){if(Test-Path -LiteralPath $ancestor){if((Get-Item -LiteralPath $ancestor).Attributes-band[IO.FileAttributes]::ReparsePoint){throw 'Test output ancestry is a reparse point.'}};if($ancestor-ieq$analysis){break};$ancestor=Split-Path -Parent $ancestor}
$compiler=Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe'
$referenceRoot=Join-Path ${env:ProgramFiles(x86)} 'Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7'
$references=@('mscorlib.dll','System.dll','System.Core.dll','System.Xml.dll','System.Runtime.Serialization.dll')|ForEach-Object{Join-Path $referenceRoot $_}
$inputPaths=@($ReaderPath,$TestSourcePath,$PSCommandPath,$compiler)+$references
$pins=@(foreach($path in $inputPaths){$file=Get-Item -LiteralPath $path;if($file.PSIsContainer-or($file.Attributes-band[IO.FileAttributes]::ReparsePoint)){throw 'Focused test input must be a regular file.'};[pscustomobject]@{path=$file.FullName;sha256=(Get-FileHash -LiteralPath $path).Hash.ToLowerInvariant();bytes=$file.Length}})
[void][IO.Directory]::CreateDirectory($out)
function Run-Child([string]$Exe,[string[]]$Arguments,[string]$Label){
 $info=[Diagnostics.ProcessStartInfo]::new();$info.FileName=$Exe;$info.Arguments=($Arguments|ForEach-Object{'"'+$_+'"'})-join' ';$info.WorkingDirectory=$out;$info.UseShellExecute=$false;$info.CreateNoWindow=$true;$info.RedirectStandardOutput=$true;$info.RedirectStandardError=$true
 $info.EnvironmentVariables['TEMP']=$out;$info.EnvironmentVariables['TMP']=$out
 $process=[Diagnostics.Process]::new();$process.StartInfo=$info;$watch=[Diagnostics.Stopwatch]::StartNew()
 try{if(-not$process.Start()){throw 'Focused test process did not start.'};$stdout=$process.StandardOutput.ReadToEndAsync();$stderr=$process.StandardError.ReadToEndAsync();$process.WaitForExit();$watch.Stop();$code=$process.ExitCode;$text=$stdout.Result;$errorText=$stderr.Result}
 finally{$process.Dispose()}
 $stdoutPath=Join-Path $out ($Label+'.stdout.log');$stderrPath=Join-Path $out ($Label+'.stderr.log');[IO.File]::WriteAllText($stdoutPath,$text);[IO.File]::WriteAllText($stderrPath,$errorText)
 [pscustomobject]@{exitCode=$code;seconds=$watch.Elapsed.TotalSeconds;stdout=$stdoutPath;stdoutSha256=(Get-FileHash -LiteralPath $stdoutPath).Hash.ToLowerInvariant();stderr=$stderrPath;stderrSha256=(Get-FileHash -LiteralPath $stderrPath).Hash.ToLowerInvariant()}
}
$exe=Join-Path $out 'WorkingActorGraphPartialTests.exe'
$compileArgs=@('/nologo','/noconfig','/nostdlib+','/target:exe','/langversion:7.3','/warnaserror+',('/out:'+$exe))+@($references|ForEach-Object{'/reference:'+$_})+@($ReaderPath,$TestSourcePath)
$compile=Run-Child $compiler $compileArgs 'compile';$run=$null;$failure=$null
if($compile.exitCode-ne0){$failure='Compilation failed.'}else{$run=Run-Child -Exe $exe -Arguments @() -Label 'test';if($run.exitCode-ne0){$failure='Actual in-memory structure checks failed.'}}
$unchanged=$true;foreach($pin in $pins){if((Get-FileHash -LiteralPath $pin.path).Hash.ToLowerInvariant()-cne$pin.sha256){$unchanged=$false}}
if(-not$unchanged){$failure='Focused test input changed.'}
$receipt=[ordered]@{schemaVersion=1;kind='actual-working-actor-graph-partial-checks';status=$(if($null-eq$failure){'PASS'}else{'FAIL'});failure=$failure;inputPins=$pins;inputsUnchanged=$unchanged;compile=$compile;test=$run;externalBoundary='Generated in-memory JSON only; actual complete Analyze source; no game or archive objects';nativeInvocations=0;archiveReads=0;archiveWrites=0}
[IO.File]::WriteAllText((Join-Path $out 'receipt.json'),($receipt|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
if($null-ne$run){[IO.File]::ReadAllText($run.stdout)}
if($null-ne$failure){throw $failure}
