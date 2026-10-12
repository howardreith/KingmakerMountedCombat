[CmdletBinding()]
param([string]$RepositoryRoot,[string]$SourceOverlayRoot,[string]$OutputRoot)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
if(-not $PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not $PSBoundParameters.ContainsKey('OutputRoot')){$OutputRoot=Join-Path ([IO.Path]::GetFullPath((Join-Path $RepositoryRoot '../..'))) ('analysis-cache/chunk7-diagnostics-off-'+[Guid]::NewGuid().ToString('N'))}
$RepositoryRoot=[IO.Path]::GetFullPath($RepositoryRoot)
$OutputRoot=[IO.Path]::GetFullPath($OutputRoot)
if(Test-Path -LiteralPath $OutputRoot){throw 'Diagnostics-off test output already exists; evidence is immutable.'}
[void][IO.Directory]::CreateDirectory($OutputRoot)
function Source([string]$relative){
    if(-not [string]::IsNullOrWhiteSpace($SourceOverlayRoot)){$candidate=Join-Path $SourceOverlayRoot $relative;if(Test-Path -LiteralPath $candidate -PathType Leaf){return $candidate}}
    return Join-Path $RepositoryRoot $relative
}
function Member([string]$text,[string]$signature,[bool]$optional=$false){
    $start=$text.IndexOf($signature,[StringComparison]::Ordinal)
    if($start-lt0){if($optional){return ''};throw ('Missing exact member: '+$signature)}
    if($text.IndexOf($signature,$start+$signature.Length,[StringComparison]::Ordinal)-ge0){throw ('Ambiguous exact member: '+$signature)}
    $open=$text.IndexOf('{',$start);$depth=0
    for($i=$open;$i-lt$text.Length;$i++){
        if($text[$i]-eq'{'){$depth++}elseif($text[$i]-eq'}'){$depth--;if($depth-eq0){return $text.Substring($start,$i-$start+1)}}
    }
    throw ('Unclosed exact member: '+$signature)
}
$patchPath=Source 'src/KingmakerMountedCombat/Integration/MountedPatchController.cs'
$hostPath=Source 'src/KingmakerMountedCombat/Diagnostics/RuntimeAutomationHost.cs'
$enginePath=Source 'src/KingmakerMountedCombat/Diagnostics/RuntimeCombatScenarioEngine.cs'
$templatePath=Source 'tests/Chunk7Diagnostics/DiagnosticsOffCooldownTemplate.cs'
$patch=[IO.File]::ReadAllText($patchPath);$hostText=[IO.File]::ReadAllText($hostPath);$engine=[IO.File]::ReadAllText($enginePath)
$patchMembers=(Member $patch 'internal readonly struct CombatCooldownObservation' $true)+[Environment]::NewLine+
    (Member $patch 'internal static bool CombatCooldownPrefix(')+[Environment]::NewLine+
    (Member $patch 'internal static void CombatCooldownPostfix(')
$hostMembers=(Member $hostText 'internal static RuntimeCombatScenarioEngine GetCombatCooldownConsumer(' $true)+[Environment]::NewLine+
    (Member $hostText 'internal static bool IsCurrentCombatCooldownConsumer(' $true)+[Environment]::NewLine+
    (Member $hostText 'internal static void ObserveCombatCooldownTick(')
$engineMembers=(Member $engine 'internal bool CanObserveCombatCooldownTick(' $true)+[Environment]::NewLine+
    (Member $engine 'internal void ObserveCombatCooldownTick(')
$code=[IO.File]::ReadAllText($templatePath).Replace('__PATCH_METHODS__',$patchMembers).Replace('__HOST_METHODS__',$hostMembers).Replace('__ENGINE_METHODS__',$engineMembers)
$codePath=Join-Path $OutputRoot 'DiagnosticsOffCooldownActual.cs';[IO.File]::WriteAllText($codePath,$code)
$compiler='C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe'
$exe=Join-Path $OutputRoot 'DiagnosticsOffCooldownActual.exe';$clock=[Diagnostics.Stopwatch]::StartNew()
& $compiler /nologo /target:exe /langversion:7.3 /nowarn:0649 "/out:$exe" $codePath
if($LASTEXITCODE-ne0){throw 'Diagnostics-off actual method adapter did not compile.'}
& $exe
$exitCode=$LASTEXITCODE
$inputs=@($patchPath,$hostPath,$enginePath,$templatePath)|ForEach-Object{[ordered]@{path=$_;sha256=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant()}}
[ordered]@{status=$(if($exitCode-eq0){'PASS'}else{'FAIL'});exitCode=$exitCode;durationSeconds=$clock.Elapsed.TotalSeconds;inputs=$inputs;actualSourceMethods=$true;externalNativeBoundariesStubbed=$true;nativeProof=$false;measuredPerformanceProof=$false}|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $OutputRoot 'receipt.json') -Encoding UTF8
exit $exitCode
