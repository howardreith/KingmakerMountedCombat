[CmdletBinding()]
param([string]$RepositoryRoot,[string]$DiagnosticSourceDirectory,[string]$TestSource,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$DiagnosticSourceDirectory){$DiagnosticSourceDirectory=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics'}
if(-not$TestSource){$TestSource=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/CombatDebtDiagnosticTests.cs'}
$analysis=[IO.Path]::GetFullPath((Join-Path $RepositoryRoot '../../analysis-cache')).TrimEnd('\')+'\'
if(-not$OutputDirectory){$OutputDirectory=Join-Path $analysis ('chunk7-continuation/combat-debt-observer-'+[Guid]::NewGuid().ToString('N'))}
$output=[IO.Path]::GetFullPath($OutputDirectory)
if(-not$output.StartsWith($analysis,[StringComparison]::OrdinalIgnoreCase)-or(Test-Path -LiteralPath $output)){throw 'Fresh private analysis-cache output required.'}
[void][IO.Directory]::CreateDirectory($output)
function Method([string]$Text,[string]$Signature){$s=$Text.IndexOf($Signature,[StringComparison]::Ordinal);if($s-lt0-or$s-ne$Text.LastIndexOf($Signature,[StringComparison]::Ordinal)){throw 'Exact source method absent or ambiguous.'};$o=$Text.IndexOf('{',$s);$d=1;$e=$o+1;while($d-gt0-and$e-lt$Text.Length){if($Text[$e]-ceq'{'){$d++}elseif($Text[$e]-ceq'}'){$d--};$e++};if($d-ne0){throw 'Unbalanced source method.'};$Text.Substring($s,$e-$s)}
$combat=Join-Path $DiagnosticSourceDirectory 'Chunk7PersistenceScenario.Combat.cs'
$probe=Join-Path $DiagnosticSourceDirectory 'NativeArchiveRestorationProbe.cs'
$text=[IO.File]::ReadAllText($combat)
$methods=@('private void AssertCombatDebt(','private SavedNativeActor[] RecordCombatDebtObservation(','internal static void AppendCombatDebtObservation(','internal static JObject CaptureCombatDebtComparison(')|ForEach-Object{Method $text $_}
$actual=Join-Path $output 'ActualCombatDebtDiagnosticTests.cs'
[IO.File]::WriteAllText($actual,[IO.File]::ReadAllText($TestSource).Replace('__DEBT_METHODS__',($methods-join[Environment]::NewLine)),[Text.UTF8Encoding]::new($false))
$relative=@('src/KingmakerMountedCombat/Integration/MountedSaveData.cs','src/KingmakerMountedCombat/Integration/SavedCombatData.cs','src/KingmakerMountedCombat/Domain/PairedActivationSnapshot.cs','src/KingmakerMountedCombat/Integration/MountedLoadAdmissionPolicy.cs','src/KingmakerMountedCombat/Integration/MountedArchiveData.cs','src/KingmakerMountedCombat/Integration/MountedArchiveCodec.cs','src/KingmakerMountedCombat/Integration/NativeMountedSaveFormat.cs','src/KingmakerMountedCombat/Domain/DeclaredSmallRiderBody.cs','src/KingmakerMountedCombat/Domain/MountedRiderProfileCatalog.cs','src/KingmakerMountedCombat/Domain/MountedRiderPoseProfile.cs','src/KingmakerMountedCombat/Domain/PoseVector3.cs')
$paths=@($relative|ForEach-Object{Join-Path $RepositoryRoot $_})+@($actual,$probe)
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$managed='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed'
$json=Join-Path $managed 'Newtonsoft.Json.dll';$unity=Join-Path $managed 'UnityEngine.dll'
$framework='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7'
$exe=Join-Path $output 'CombatDebtTests.exe';$response=Join-Path $output 'compile.rsp'
$arguments=@('/nologo','/target:exe','/langversion:7.3','/optimize+','/deterministic+','/nostdlib+','/nowarn:0649,0414',('/out:'+$exe),('/reference:'+$json))+@('mscorlib.dll','System.dll','System.Core.dll'|ForEach-Object{'/reference:'+(Join-Path $framework $_)})
[IO.File]::WriteAllLines($response,@(($arguments+$paths)|ForEach-Object{'"'+$_+'"'}),[Text.UTF8Encoding]::new($false))
$timer=[Diagnostics.Stopwatch]::StartNew()
$compile=@(&$compiler /noconfig ('@'+$response) 2>&1);$compileExit=$LASTEXITCODE
[IO.File]::WriteAllLines((Join-Path $output 'compile.log'),[string[]]$compile,[Text.UTF8Encoding]::new($false))
$test=@();$testExit=$null
if($compileExit-eq0){Copy-Item -LiteralPath $json -Destination $output;$test=@(&$exe $unity 2>&1);$testExit=$LASTEXITCODE;[IO.File]::WriteAllLines((Join-Path $output 'test.log'),[string[]]$test,[Text.UTF8Encoding]::new($false));$test|ForEach-Object{Write-Output $_}}
$passed=@($test|Where-Object{$_-cmatch'^PASS '}).Count;$failed=@($test|Where-Object{$_-cmatch'^FAIL '}).Count
$ok=$compileExit-eq0-and$testExit-eq0-and$passed-gt0-and$failed-eq0
$receipt=[ordered]@{status=$(if($ok){'PASS'}else{'FAIL'});passed=$passed;failed=$failed;compileExit=$compileExit;testExit=$testExit;elapsedSeconds=$timer.Elapsed.TotalSeconds;inputs=@(($paths+@($combat,$TestSource,$json,$unity))|ForEach-Object{[ordered]@{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash.ToLowerInvariant()}});nativeInvoked=$false;nativePatchInstalled=$false;actualSourceProbe=$true;actualDebtPredicate=$true;limits='Entire actual native restoration probe plus4 exact combat debt methods, actual DTO/codec validation classes. Runtime request, Game/native actors/Harmony/clock and native storage containers are explicit facades. Native tokens are separately reflection-only verified. No native call, timing guarantee, shared product compile, gameplay repair or restored-state acceptance is claimed.'}
[IO.File]::WriteAllText((Join-Path $output 'receipt.json'),($receipt|ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false))
if(-not$ok){$compile|ForEach-Object{Write-Output $_};throw 'Combat debt diagnostic source regression failed; retain output.'}
