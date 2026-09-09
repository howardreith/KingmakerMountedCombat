$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Test-Chunk4CoreProtocol.ps1')
. (Join-Path $PSScriptRoot 'Test-Chunk4ObstructionProtocol.ps1')

$kmcOuterPass=0
$kmcRoot=Join-Path (Get-KmcRepositoryRoot) ('obj/chunk4-artifact-protocol/'+[Guid]::NewGuid().ToString('N'))
foreach($root in @('chunk4-rider-incapacitation-tb','chunk4-rider-death-tb','chunk4-mount-death-tb','chunk4-targeting-rider-rt',
    'chunk4-targeting-mount-rt','chunk4-targeting-area-unmounted-rt','chunk4-horse-strike-comparison-rt','chunk4-ranged-native-control-rt',
    'chunk4-obstruction-ranged-rt')){
    $outer=if($root -ceq 'chunk4-obstruction-ranged-rt'){New-ObstructionEnvelope $true}else{New-CoreEnvelope $root}
    $identity=@{evidenceKind='phase3d-horse-scenario-evidence';runId='parser-only';scenario=$root;branch='codex/mounted-combat-phase3f-playable-core';
        commit=('a'*40);productVersion='0.1.0-parser-only';dllSha256=('b'*64);dllMvid='00000000-0000-0000-0000-000000000001';createdAtUtc=[DateTime]::UtcNow.ToString('o')}
    foreach($key in $identity.Keys){$outer|Add-Member -NotePropertyName $key -NotePropertyValue $identity[$key]}
    $request=@{};foreach($key in $identity.Keys){$request[$key]=$identity[$key]}
    $request.evidenceRoot=Join-Path $kmcRoot $root
    $null=[IO.Directory]::CreateDirectory($request.evidenceRoot)
    $path=Join-Path $request.evidenceRoot 'phase3d-horse-scenario-evidence.json'
    $manifest=@{artifacts=@(@{relativePath='phase3d-horse-scenario-evidence.json';kind='phase3d-horse-scenario-evidence'})}
    $outer|ConvertTo-Json -Depth 40|Set-Content -LiteralPath $path -Encoding UTF8
    Assert-KmcPhase3dHorseScenarioEvidence -Request $request -Manifest $manifest -Status 'PASS';$kmcOuterPass++
    foreach($mutate in @({param($a) $a.schemaVersion=21},{param($a) $a.commit=('c'*40)},
        {param($a) $a.rows=@()},{param($a) $a.observations.phase3fActualConfiguration.enablePairedActivation=$false})){
        $changed=$outer|ConvertTo-Json -Depth 40|ConvertFrom-Json;& $mutate $changed
        $changed|ConvertTo-Json -Depth 40|Set-Content -LiteralPath $path -Encoding UTF8
        $rejected=$false
        try{Assert-KmcPhase3dHorseScenarioEvidence -Request $request -Manifest $manifest -Status 'PASS'}catch{$rejected=$true}
        if(!$rejected){throw ('Invalid outer artifact accepted: '+$root)};$kmcOuterPass++
    }
}
Write-Output "COMPONENT outer artifact dispatch TOTAL PASS=$kmcOuterPass FAIL=0"
