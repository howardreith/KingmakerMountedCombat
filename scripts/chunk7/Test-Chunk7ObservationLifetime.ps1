[CmdletBinding()]
param([string]$RepositoryRoot,[string]$SourceOverlayRoot,[string]$OutputRoot)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
if(-not $PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not $PSBoundParameters.ContainsKey('OutputRoot')){$OutputRoot=Join-Path ([IO.Path]::GetFullPath((Join-Path $RepositoryRoot '../..'))) ('analysis-cache/chunk7-observation-lifetime-'+[Guid]::NewGuid().ToString('N'))}
$RepositoryRoot=[IO.Path]::GetFullPath($RepositoryRoot)
$OutputRoot=[IO.Path]::GetFullPath($OutputRoot)
if(Test-Path -LiteralPath $OutputRoot){throw 'Observation-lifetime output exists; preserve prior evidence.'}
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
    if($start-lt0-or$text.IndexOf($signature,$start+$signature.Length,[StringComparison]::Ordinal)-ge0){throw ('Missing or ambiguous exact expression member: '+$signature)}
    $end=$text.IndexOf(';',$start)
    if($end-lt0){throw ('Unclosed expression member: '+$signature)}
    return $text.Substring($start,$end-$start+1)
}
$relative=@('src/KingmakerMountedCombat/Integration/HorseCompanionBlueprintService.cs','src/KingmakerMountedCombat/Integration/MountedPairAttackCommand.cs','src/KingmakerMountedCombat/Diagnostics/RuntimeAutomationHost.cs','src/KingmakerMountedCombat/Integration/MountedCombatController.cs','src/KingmakerMountedCombat/Integration/MountedWorldService.cs','tests/Chunk7Diagnostics/ObservationLifetimeTemplate.cs')
$paths=@($relative|ForEach-Object{Source $_})
$inputs=@($paths|ForEach-Object{[ordered]@{path=$_;sha256=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant()}})
$horse=[IO.File]::ReadAllText($paths[0]);$attack=[IO.File]::ReadAllText($paths[1]);$hostText=[IO.File]::ReadAllText($paths[2])
$controller=[IO.File]::ReadAllText($paths[3]);$world=[IO.File]::ReadAllText($paths[4]);$template=[IO.File]::ReadAllText($paths[5])
$fieldsStart=$horse.IndexOf('private readonly Dictionary<string, HorseActivationObservation> observedActivationByOwner',[StringComparison]::Ordinal)
if($fieldsStart-lt0){$fieldsStart=$horse.IndexOf('private readonly Dictionary<string, string> observedActivationByOwner',[StringComparison]::Ordinal)}
$fieldsEnd=$horse.IndexOf('private HorseCompanionBlueprintState state;',[StringComparison]::Ordinal)
if($fieldsStart-lt0-or$fieldsEnd-le$fieldsStart){throw 'Horse observation-cache field boundary changed.'}
$code=$template.Replace('__OBSERVATION_VALUE__',(Member $horse 'private struct HorseActivationObservation' $true)).Replace('__CACHE_FIELDS__',$horse.Substring($fieldsStart,$fieldsEnd-$fieldsStart)).Replace('__SYNC__',(Member $horse 'private void SynchronizeLiveHorseCompanionProgression()')).Replace('__DISPOSE__',(Member $horse 'public void Dispose()')).Replace('__WORLD_FIND__',(ExpressionMember $world 'internal MountedPairServices Find(GameMountedRelationshipService relationship)')).Replace('__HOST_GATE__',(Member $hostText 'internal static bool ShouldCaptureMountedPreStartInterruption(' $true)).Replace('__INTERRUPT__',(Member $attack 'internal void ObservePreStartInterrupt()')).Replace('__CONTROLLER__',(Member $controller 'internal void ObserveCommandInterrupt(UnitCommand command)'))
if($code-match'__[A-Z_]+__'){throw 'Unexpanded exact-method template marker.'}
$codePath=Join-Path $OutputRoot 'ObservationLifetimeActual.cs'
[IO.File]::WriteAllText($codePath,$code,(New-Object Text.UTF8Encoding($false)))
[IO.File]::WriteAllText((Join-Path $OutputRoot 'ObservationLifetimeTemplate.cs'),$template,(New-Object Text.UTF8Encoding($false)))
$compiler='C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe'
$exe=Join-Path $OutputRoot 'ObservationLifetimeActual.exe'
$clock=[Diagnostics.Stopwatch]::StartNew()
$compileOutput=@(& $compiler /nologo /target:exe /langversion:7.3 /nowarn:0649,0436 "/out:$exe" $codePath 2>&1)
$compileExitCode=$LASTEXITCODE
$compileOutput|ForEach-Object{Write-Output $_}
[IO.File]::WriteAllLines((Join-Path $OutputRoot 'compile.log'),[string[]]$compileOutput)
$exitCode=1;$passed=0;$failed=1;$runOutput=@()
if($compileExitCode-eq0){
    $runOutput=@(& $exe 2>&1);$exitCode=$LASTEXITCODE
    $runOutput|ForEach-Object{Write-Output $_}
    $summary=@($runOutput|Where-Object{$_-is[string]-and$_-match'^OBSERVATION LIFETIME PASS=([0-9]+) FAIL=([0-9]+)$'})
    if($summary.Count-eq1-and$summary[0]-match'^OBSERVATION LIFETIME PASS=([0-9]+) FAIL=([0-9]+)$'){
        $passed=[int]$Matches[1];$failed=[int]$Matches[2]
        if($passed+$failed-ne71-or$failed-ne0){$exitCode=1}
    }else{$exitCode=1}
}
[IO.File]::WriteAllLines((Join-Path $OutputRoot 'run.log'),[string[]]$runOutput)
$clock.Stop()
foreach($inputPin in $inputs){if((Get-FileHash -LiteralPath $inputPin.path -Algorithm SHA256).Hash.ToLowerInvariant()-cne$inputPin.sha256){throw ('Executed input changed: '+$inputPin.path)}}
[ordered]@{kind='chunk7-observation-lifetime-actual';status=$(if($compileExitCode-eq0-and$exitCode-eq0-and$failed-eq0){'PASS'}else{'FAIL'});compileExitCode=$compileExitCode;exitCode=$exitCode;passed=$passed;failed=$failed;durationSeconds=$clock.Elapsed.TotalSeconds;durationBasis='Stopwatch around compiler and actual adapter child processes';inputs=$inputs;actual=[ordered]@{path=$codePath;sha256=(Get-FileHash -LiteralPath $codePath -Algorithm SHA256).Hash.ToLowerInvariant()};actualSourceMethods=$true;externalNativeAndStackBoundariesStubbed=$true;nativeProof=$false;measuredPerformanceProof=$false}|ConvertTo-Json -Depth 7|Set-Content -LiteralPath (Join-Path $OutputRoot 'receipt.json') -Encoding UTF8
exit $exitCode
