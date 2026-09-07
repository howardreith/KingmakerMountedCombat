[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/ActorAllocationEvidence.ps1')
$passes=0
function New-AllocationEnvelope {
    $events=@(); $samples=@(); $rounds=[ordered]@{}
    for($round=1;$round-le3;$round++) {
        $rounds[[string]$round]=@('rider','mount')
        foreach($actor in @('rider','mount')) {
            $samples+=@{round=$round;surface=$actor}
            foreach($boundary in @('prepare-before','clear-before','clear-after','round-state-before','round-state-after',
                'round-handler-before','round-handler-after','ready-handler-before','ready-handler-after','prepare-after',
                'end-turn-input-before','end-turn-input-after','turn-end-before','turn-end-after')) {
                $events+=@{round=$round;state=@{actor=$actor};boundary=$boundary}
            }
        }
    }
    return ([ordered]@{schemaVersion=7;subscenarioPassCount=1;subscenarioFailCount=0;errors=@()
        rows=@(@{name='T01-native-allocation-trace';status='PASS';evidence=@{level='NATIVE INTEGRATION';gameplayQualified=$false
            firstRound=1;endRound=4;samples=$samples;order=@('rider','mount');rounds=$rounds}})
        observations=@{riderId='rider';horseId='mount';allocationInitiativeRestored=$true;allocationPartyCombatRestored=$true
            allocationFixture=@{outsideCombat=$true;endTurnInput='Game.PauseBind'}
            phase3fActualConfiguration=@{enableUnifiedMountedTurn=$false;enablePairedCommandScheduler=$false;enableDiagnosticOverlay=$false;overlayPresent=$false}
            actorAllocationTrace=@{dropped=0;observationErrors=0;events=$events}}
    } | ConvertTo-Json -Depth 20 | ConvertFrom-Json)
}
$request=[pscustomobject]@{scenario='actor-allocation-rider-first-tb'}
$valid=New-AllocationEnvelope
Assert-KmcActorAllocationEvidence $request $valid 'PASS'
$passes++
foreach($mutation in @(
    {param($e) $e.rows[0].evidence.gameplayQualified=$true},
    {param($e) $e.observations.actorAllocationTrace.dropped=1},
    {param($e) $e.observations.actorAllocationTrace.observationErrors=1},
    {param($e) $e.rows[0].evidence.endRound=3},
    {param($e) $e.rows[0].evidence.rounds.'2'=@('mount','rider')},
    {param($e) $e.observations.allocationInitiativeRestored=$false},
    {param($e) $e.observations.allocationPartyCombatRestored=$false},
    {param($e) $e.observations.allocationFixture.outsideCombat=$false},
    {param($e) $e.observations.actorAllocationTrace.events=@($e.observations.actorAllocationTrace.events | Where-Object boundary -CNE 'clear-after')},
    {param($e) $e.observations.actorAllocationTrace.events+=@($e.observations.actorAllocationTrace.events[0])},
    {param($e) $e.observations.phase3fActualConfiguration.enableUnifiedMountedTurn=$true}
)) {
    $e=New-AllocationEnvelope; & $mutation $e
    $rejected=$false
    try {Assert-KmcActorAllocationEvidence $request $e 'PASS'} catch {$rejected=$true}
    if(!$rejected){throw 'Malformed allocation envelope was accepted.'}
    $passes++
}
Write-Host "ALLOCATION PROTOCOL PASS=$passes FAIL=0 (envelope validation only)"
