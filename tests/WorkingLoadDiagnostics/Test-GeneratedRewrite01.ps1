[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$out=[IO.Path]::GetFullPath($OutputDirectory);if(-not$out.StartsWith($PSScriptRoot+'\',[StringComparison]::OrdinalIgnoreCase)-or(Test-Path -LiteralPath $out)){throw 'Fresh private matcher output required.'}
. (Join-Path $PSScriptRoot 'NativeCompileInputs.ps1')
$exe=Join-Path $out 'GeneratedWorkingLoadRewriteTests.exe';$test=Join-Path $PSScriptRoot 'GeneratedWorkingLoadRewriteTests.cs'
$arguments=@('/nologo','/noconfig','/nostdlib+','/target:exe','/langversion:7.3','/warnaserror+',('/out:'+$exe))+@($references|ForEach-Object{'/reference:'+$_})+$sources+@($test)
$paths=@($PSCommandPath,(Join-Path $PSScriptRoot 'NativeCompileInputs.ps1'),$test,$compiler)+$references+$sources
$pins=@(foreach($path in $paths){[ordered]@{path=$path;sha256=(Get-FileHash -LiteralPath $path).Hash.ToLowerInvariant();bytes=(Get-Item -LiteralPath $path).Length}})
[void][IO.Directory]::CreateDirectory($out);$snap=Join-Path $out 'source-inputs';[void][IO.Directory]::CreateDirectory($snap)
foreach($p in $paths|Where-Object{$_.EndsWith('.cs')-or$_.EndsWith('.ps1')}){Copy-Item -LiteralPath $p -Destination (Join-Path $snap ([IO.Path]::GetFileName($p)))}
function Run([string]$File,[string[]]$ChildArguments,[string]$Label){
 $i=[Diagnostics.ProcessStartInfo]::new();$i.FileName=$File;$i.Arguments=($ChildArguments|ForEach-Object{'"'+$_+'"'})-join' ';$i.WorkingDirectory=$out;$i.UseShellExecute=$false;$i.CreateNoWindow=$true;$i.RedirectStandardOutput=$true;$i.RedirectStandardError=$true;$i.EnvironmentVariables['TEMP']=$out;$i.EnvironmentVariables['TMP']=$out
 $p=[Diagnostics.Process]::new();$p.StartInfo=$i;$w=[Diagnostics.Stopwatch]::StartNew();try{if(-not$p.Start()){throw 'Child did not start.'};$id=$p.Id;$so=$p.StandardOutput.ReadToEndAsync();$se=$p.StandardError.ReadToEndAsync();$p.WaitForExit();$w.Stop();$exit=$p.ExitCode;$text=$so.Result;$errorText=$se.Result}finally{$p.Dispose()}
 $stdout=Join-Path $out ($Label+'.stdout.log');$stderr=Join-Path $out ($Label+'.stderr.log');[IO.File]::WriteAllText($stdout,$text);[IO.File]::WriteAllText($stderr,$errorText)
 [ordered]@{executable=$File;requestedArguments=$ChildArguments;cwd=$out;processId=$id;exitCode=$exit;seconds=$w.Elapsed.TotalSeconds;stdout=$stdout;stdoutSha256=(Get-FileHash -LiteralPath $stdout).Hash.ToLowerInvariant();stderr=$stderr;stderrSha256=(Get-FileHash -LiteralPath $stderr).Hash.ToLowerInvariant()}
}
$compile=Run -File $compiler -ChildArguments $arguments -Label 'compile';$run=$null;$passed=$null;$failed=$null
if($compile.exitCode-eq0){$run=Run -File $exe -ChildArguments @('C:\Program Files (x86)\Steam\steamapps\common\Pathfinder Kingmaker\Kingmaker_Data\Managed') -Label 'matcher';$raw=[IO.File]::ReadAllText($run.stdout);$m=[regex]::Matches($raw,'(?m)^WORKING LOAD GENERATED REWRITE PASS=(\d+) FAIL=(\d+)\r?$');if($m.Count-eq1){$passed=[int]$m[0].Groups[1].Value;$failed=[int]$m[0].Groups[2].Value}}
$same=$true;foreach($pin in $pins){if((Get-FileHash -LiteralPath $pin.path).Hash.ToLowerInvariant()-cne$pin.sha256){$same=$false}}
$ok=$compile.exitCode-eq0-and$null-ne$run-and$run.exitCode-eq0-and$null-ne$passed-and$failed-eq0-and$same
$receipt=[ordered]@{schemaVersion=1;kind='actual-private-generated-working-load-rewrite';status=$(if($ok){'PASS'}else{'FAIL'});passed=$passed;failed=$failed;compile=$compile;test=$run;inputPins=$pins;inputsUnchanged=$same;boundary='Actual rewrite emits only generated external boundary methods/fields after an explicit native-operand mapping refuses every remaining native member. Native metadata binds the matcher; no native method, Harmony patch, game state, serializer or archive is invoked. Generated notification failure is an external substitute; adapter no-throw behavior is tested separately.';nativeInvocation=$false;archiveReads=0;candidateBuild=$false}
[IO.File]::WriteAllText((Join-Path $out 'receipt.json'),($receipt|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
[IO.File]::ReadAllText($compile.stdout);if($null-ne$run){[IO.File]::ReadAllText($run.stdout)}
if(-not$ok){throw 'Private installed metadata matcher failed.'}
