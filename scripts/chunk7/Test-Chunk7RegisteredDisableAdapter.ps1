[CmdletBinding()]
param([string]$DraftRoot,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$DraftRoot){$DraftRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
$analysis='C:/Dev/KingmakerMountedCombatLab/analysis-cache'
if(-not$OutputDirectory){$OutputDirectory=Join-Path $DraftRoot ('checks/adapter-'+[Guid]::NewGuid().ToString('N'))}
$OutputDirectory=[IO.Path]::GetFullPath($OutputDirectory)
if(-not$OutputDirectory.StartsWith([IO.Path]::GetFullPath($analysis)+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)-or(Test-Path -LiteralPath $OutputDirectory)){throw 'Fresh private analysis-cache check directory required.'}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
function Method([string]$Text,[string]$Signature){$s=$Text.IndexOf($Signature,[StringComparison]::Ordinal);if($s-lt0-or$s-ne$Text.LastIndexOf($Signature,[StringComparison]::Ordinal)){throw 'Actual private diagnostic method absent/ambiguous.'};$o=$Text.IndexOf('{',$s);$d=1;$e=$o+1;while($d-gt0-and$e-lt$Text.Length){if($Text[$e]-ceq'{'){$d++}elseif($Text[$e]-ceq'}'){$d--};$e++};if($d-ne0){throw 'Unbalanced actual private method.'};$Text.Substring($s,$e-$s)}
$source=Join-Path $DraftRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk7CombatScenario.Disable.cs';$text=[IO.File]::ReadAllText($source)
$methods=@('private void RequireRegisteredDisableScope()','private void ObserveDisableVisit(','private JObject CaptureDisableState(','private JObject CaptureDisablePairState(','private void ExercisePermanentHorseRemovalRefusal()','private static void RequireDisableDebt(','private static void RequireNativeDecay(','private static double NativeNumber(','private void RequireNativeDisableProgression(','private void RequireDisableVisits(','private void RequireDisableRetainedOwners()','private void BeginRegisteredDisableFailure()','private bool RegisteredDisableCombatBoundary()','private void PollRegisteredDisabledFrame()','private void EndRegisteredDisableObservation()')|ForEach-Object{Method $text $_}
$template=Join-Path $DraftRoot 'tests/Chunk7Diagnostics/RegisteredDisableAdapterTemplate.cs';$actual=Join-Path $OutputDirectory 'ActualRegisteredDisableAdapter.cs'
[IO.File]::WriteAllText($actual,([IO.File]::ReadAllText($template).Replace('__METHODS__',($methods-join[Environment]::NewLine))),[Text.UTF8Encoding]::new($false))
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$json='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll'
$exe=Join-Path $OutputDirectory 'PrivateRegisteredDisable.exe';$timer=[Diagnostics.Stopwatch]::StartNew()
$policy='C:/Dev/KingmakerMountedCombatLab/repo/KingmakerMountedCombat/src/KingmakerMountedCombat/Domain/RemovalReadinessPolicy.cs'
$identity='C:/Dev/KingmakerMountedCombatLab/repo/KingmakerMountedCombat/src/KingmakerMountedCombat/Diagnostics/RetainedObjectIdentity.cs'
$compile=@(&$compiler /nologo /target:exe /langversion:7.3 '/nowarn:0649,0414' "/reference:$json" "/out:$exe" $actual $policy $identity 2>&1);$compileExit=$LASTEXITCODE;$compile|Set-Content -LiteralPath (Join-Path $OutputDirectory 'compile.log')
$test=@();$testExit=$null
if($compileExit-eq0){
 # Resolve the installed read-only JSON reference in this private PowerShell process;
 # no native game assemblies or dependency payloads are copied into the draft.
 [void][Reflection.Assembly]::LoadFrom($json)
 $assembly=[Reflection.Assembly]::LoadFrom($exe)
 $capture=New-Object IO.StringWriter;$previous=[Console]::Out
 try{[Console]::SetOut($capture);$testExit=[int]$assembly.EntryPoint.Invoke($null,@());}finally{[Console]::SetOut($previous)}
 $test=@($capture.ToString()-split'\r?\n'|Where-Object{$_-ne''});$test|Set-Content -LiteralPath (Join-Path $OutputDirectory 'test.log');$test|ForEach-Object{Write-Output $_}
}
$passed=@($test|Where-Object{$_-cmatch'^PASS '}).Count;$failed=@($test|Where-Object{$_-cmatch'^FAIL '}).Count
$ok=$compileExit-eq0-and$testExit-eq0-and$passed-gt0-and$failed-eq0
[ordered]@{status=$(if($ok){'PASS'}else{'FAIL'});passed=$passed;failed=$failed;compileExit=$compileExit;testExit=$testExit;elapsedSeconds=$timer.Elapsed.TotalSeconds;sourceSha256=(Get-FileHash -LiteralPath $source).Hash.ToLowerInvariant();templateSha256=(Get-FileHash -LiteralPath $template).Hash.ToLowerInvariant();actualSha256=(Get-FileHash -LiteralPath $actual).Hash.ToLowerInvariant();jsonSha256=(Get-FileHash -LiteralPath $json).Hash.ToLowerInvariant();policySha256=(Get-FileHash -LiteralPath $policy).Hash.ToLowerInvariant();identitySha256=(Get-FileHash -LiteralPath $identity).Hash.ToLowerInvariant();nativeInvoked=$false;trackedSourceEdited=$false;limits='Twelve exact original diagnostic methods and the real unchanged RemovalReadinessPolicy and RetainedObjectIdentity. Registered callback, removal service, native world, relationship delivery and authority are typed facades. No native cleanup/disable/removal behavior, full product compilation, package or execution-source application is claimed.'}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json')
if(-not$ok){$compile|Write-Output;throw 'Private registered-disable adapter check failed; preserve outputs.'}
