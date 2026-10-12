[CmdletBinding()]
param([string]$RepositoryRoot,[string]$SourceOverlayRoot,[string]$OutputRoot)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
if(-not $PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not $PSBoundParameters.ContainsKey('OutputRoot')){$OutputRoot=Join-Path ([IO.Path]::GetFullPath((Join-Path $RepositoryRoot '../..'))) ('analysis-cache/chunk7-forensic-off-'+[Guid]::NewGuid().ToString('N'))}
$RepositoryRoot=[IO.Path]::GetFullPath($RepositoryRoot)
$OutputRoot=[IO.Path]::GetFullPath($OutputRoot)
if(Test-Path -LiteralPath $OutputRoot){throw 'Forensic-off output exists; preserve prior evidence.'}
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
function ExpressionMember([string]$text,[string]$signature){
    $start=$text.IndexOf($signature,[StringComparison]::Ordinal)
    if($start-lt0){throw ('Missing exact expression member: '+$signature)}
    $end=$text.IndexOf(';',$start)
    if($end-lt0){throw ('Unclosed expression member: '+$signature)}
    return $text.Substring($start,$end-$start+1)
}
$discardPath=Source 'src/KingmakerMountedCombat/Integration/PairedActivationPersistence.cs'
$attackPath=Source 'src/KingmakerMountedCombat/Integration/MountedPairSingleAttack.cs'
$hostPath=Source 'src/KingmakerMountedCombat/Diagnostics/RuntimeAutomationHost.cs'
$enginePath=Source 'src/KingmakerMountedCombat/Diagnostics/RuntimePersistenceFoundationScenario.cs'
$protocolPath=Source 'src/KingmakerMountedCombat/Diagnostics/RuntimeProtocol.cs'
$validationPath=Source 'src/KingmakerMountedCombat/Diagnostics/RuntimePersistenceInvalidCombatScenario.cs'
$templatePath=Source 'tests/Chunk7Diagnostics/ForensicDiagnosticsOffTemplate.cs'
$discard=[IO.File]::ReadAllText($discardPath);$attack=[IO.File]::ReadAllText($attackPath)
$hostText=[IO.File]::ReadAllText($hostPath);$engine=[IO.File]::ReadAllText($enginePath)
$discardMembers=(Member $discard 'private static string DescribeManagedCallers()')+[Environment]::NewLine+(Member $discard 'internal void DiscardParticipation()')
$attackMembers=(Member $attack 'internal string TerminalLifecycle' $true)+[Environment]::NewLine+
    (Member $attack 'protected override void OnEnded(' $true)+[Environment]::NewLine+(Member $attack 'private string DescribeTerminalLifecycle()' $true)
$hostMembers=Member $hostText 'internal static bool ShouldCapturePersistenceWorldDiscard(' $true
$engineMembers=(Member $engine 'internal static bool ConsumesPersistenceWorldDiscard(' $true)+[Environment]::NewLine+
    (Member $engine 'internal bool CanObservePersistenceWorldDiscard(' $true)
$chargeCase=ExpressionMember ([IO.File]::ReadAllText($protocolPath)) 'internal static bool IsChargeLifecycleCase('
$validationCase=ExpressionMember ([IO.File]::ReadAllText($validationPath)) 'internal static bool IsCombatValidation('
$code=[IO.File]::ReadAllText($templatePath).Replace('__DISCARD_METHODS__',$discardMembers).Replace('__ATTACK_METHODS__',$attackMembers).Replace('__HOST_METHODS__',$hostMembers).Replace('__ENGINE_METHODS__',$engineMembers).Replace('__CHARGE_CASE__',$chargeCase).Replace('__VALIDATION_CASE__',$validationCase)
$codePath=Join-Path $OutputRoot 'ForensicDiagnosticsOffActual.cs';[IO.File]::WriteAllText($codePath,$code)
$compiler='C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe'
$exe=Join-Path $OutputRoot 'ForensicDiagnosticsOffActual.exe';$clock=[Diagnostics.Stopwatch]::StartNew()
& $compiler /nologo /target:exe /langversion:7.3 /nowarn:0649,0436 "/out:$exe" $codePath
if($LASTEXITCODE-ne0){throw 'Forensic-off actual method adapter did not compile.'}
& $exe
$exitCode=$LASTEXITCODE
$inputs=@($discardPath,$attackPath,$hostPath,$enginePath,$protocolPath,$validationPath,$templatePath)|ForEach-Object{[ordered]@{path=$_;sha256=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant()}}
[ordered]@{status=$(if($exitCode-eq0){'PASS'}else{'FAIL'});exitCode=$exitCode;durationSeconds=$clock.Elapsed.TotalSeconds;inputs=$inputs;actualSourceMethods=$true;externalNativeAndStackBoundariesStubbed=$true;nativeProof=$false;measuredPerformanceProof=$false}|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $OutputRoot 'receipt.json') -Encoding UTF8
exit $exitCode
