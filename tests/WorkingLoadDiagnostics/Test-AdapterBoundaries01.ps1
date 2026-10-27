[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$lane=$PSScriptRoot
$out=[IO.Path]::GetFullPath($OutputDirectory).TrimEnd('\')
if([string]::IsNullOrWhiteSpace($OutputDirectory)-or$out.Contains('"')-or$out.Contains("`r")-or$out.Contains("`n")-or-not$out.StartsWith($lane+'\',[StringComparison]::OrdinalIgnoreCase)-or(Test-Path -LiteralPath $out)){throw 'A fresh private observer test output directory is required.'}
$ancestor=Split-Path -Parent $out
while($ancestor.Length-ge$lane.Length){if(Test-Path -LiteralPath $ancestor){if((Get-Item -LiteralPath $ancestor).Attributes-band[IO.FileAttributes]::ReparsePoint){throw 'Output ancestor is a reparse point.'}};if($ancestor-ieq$lane){break};$ancestor=Split-Path -Parent $ancestor}
$compiler=Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe'
$referenceRoot=Join-Path ${env:ProgramFiles(x86)} 'Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7'
$references=@('mscorlib.dll','System.dll','System.Core.dll')|ForEach-Object{Join-Path $referenceRoot $_}
$sources=@((Join-Path $lane 'src/WorkingLoadObservation.cs'),(Join-Path $lane 'src/NativeWorkingLoadObserver.cs'),(Join-Path $PSScriptRoot 'WorkingLoadAdapterBoundaryTests.cs'))
$pins=@(foreach($path in @($PSCommandPath,$compiler)+$references+$sources){$file=Get-Item -LiteralPath $path;if($file.PSIsContainer-or($file.Attributes-band[IO.FileAttributes]::ReparsePoint)){throw 'Test input must be a regular file.'};[pscustomobject]@{path=$file.FullName;sha256=(Get-FileHash -LiteralPath $path).Hash.ToLowerInvariant();bytes=$file.Length}})
[void][IO.Directory]::CreateDirectory($out)
$frozen=Join-Path $out 'source-inputs';[void][IO.Directory]::CreateDirectory($frozen)
foreach($path in $sources){Copy-Item -LiteralPath $path -Destination (Join-Path $frozen ([IO.Path]::GetFileName($path)))}
Copy-Item -LiteralPath $PSCommandPath -Destination (Join-Path $frozen ([IO.Path]::GetFileName($PSCommandPath)))
function Run-Child([string]$Exe,[string[]]$Arguments,[string]$Label){
 $info=[Diagnostics.ProcessStartInfo]::new();$info.FileName=$Exe;$info.Arguments=($Arguments|ForEach-Object{'"'+$_+'"'})-join' ';$info.WorkingDirectory=$out;$info.UseShellExecute=$false;$info.CreateNoWindow=$true;$info.RedirectStandardOutput=$true;$info.RedirectStandardError=$true
 $info.EnvironmentVariables['TEMP']=$out;$info.EnvironmentVariables['TMP']=$out
 $process=[Diagnostics.Process]::new();$process.StartInfo=$info;$watch=[Diagnostics.Stopwatch]::StartNew()
 try{if(-not$process.Start()){throw 'Focused child did not start.'};$pidObserved=$process.Id;$started=$process.StartTime.ToUniversalTime().ToString('o');$stdout=$process.StandardOutput.ReadToEndAsync();$stderr=$process.StandardError.ReadToEndAsync();$process.WaitForExit();$watch.Stop();$code=$process.ExitCode;$text=$stdout.Result;$errorText=$stderr.Result}
 finally{$process.Dispose()}
 $stdoutPath=Join-Path $out ($Label+'.stdout.log');$stderrPath=Join-Path $out ($Label+'.stderr.log');[IO.File]::WriteAllText($stdoutPath,$text);[IO.File]::WriteAllText($stderrPath,$errorText)
 [pscustomobject]@{executable=$Exe;arguments=$Arguments;requestedRawArguments=$info.Arguments;workingDirectory=$out;temp=$out;tmp=$out;processId=$pidObserved;startedAtUtc=$started;exitCode=$code;seconds=$watch.Elapsed.TotalSeconds;stdout=$stdoutPath;stdoutSha256=(Get-FileHash -LiteralPath $stdoutPath).Hash.ToLowerInvariant();stderr=$stderrPath;stderrSha256=(Get-FileHash -LiteralPath $stderrPath).Hash.ToLowerInvariant()}
}
$exe=Join-Path $out 'WorkingLoadAdapterBoundaryTests.exe'
$compileArgs=@('/nologo','/noconfig','/nostdlib+','/target:exe','/langversion:7.3','/warnaserror+',('/out:'+$exe))+@($references|ForEach-Object{'/reference:'+$_})+$sources
$compile=Run-Child $compiler $compileArgs 'compile';$run=$null;$failure=$null;$passed=$null;$failed=$null
if($compile.exitCode-ne0){$failure='Compilation failed.'}else{
 $run=Run-Child -Exe $exe -Arguments @() -Label 'test'
 $raw=[IO.File]::ReadAllText($run.stdout)
 $terminals=@([regex]::Matches($raw,'(?m)^WORKING LOAD ADAPTER BOUNDARY PASS=(\d+) FAIL=(\d+)\r?$'))
 if($terminals.Count-ne1){$failure='Unique focused-test terminal absent.'}else{$passed=[int]$terminals[0].Groups[1].Value;$failed=[int]$terminals[0].Groups[2].Value}
 if($run.exitCode-ne0-or$failed-ne0){$failure='Actual observer checks failed.'}
}
$unchanged=$true;foreach($pin in $pins){if((Get-FileHash -LiteralPath $pin.path).Hash.ToLowerInvariant()-cne$pin.sha256){$unchanged=$false}}
if(-not$unchanged){$failure='Focused test input changed.'}
$receipt=[ordered]@{schemaVersion=1;kind='actual-private-working-load-adapter-boundaries';status=$(if($null-eq$failure){'PASS'}else{'FAIL'});failure=$failure;passed=$passed;failed=$failed;inputPins=$pins;inputsUnchanged=$unchanged;compile=$compile;test=$run;externalBoundary='Actual original observer and recorder compiled unchanged. Explicit native/Harmony/field/contract stand-ins exercise provenance, no-throw and custody; actual installed matcher and generated IL have separate checks. No game assembly, native/Harmony installation or archive.';nativeInvocations=0;archiveReads=0;archiveWrites=0;candidateBuild=$false}
[IO.File]::WriteAllText((Join-Path $out 'receipt.json'),($receipt|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
if($null-ne$run){[IO.File]::ReadAllText($run.stdout)}else{[IO.File]::ReadAllText($compile.stdout)}
if($null-ne$failure){throw $failure}
