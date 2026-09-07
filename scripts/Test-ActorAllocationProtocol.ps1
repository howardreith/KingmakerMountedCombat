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
function New-CallbackEnvelope {
    $e=New-AllocationEnvelope
    $events=@(); $sequence=0L; $measurements=@(); $facts=@(); $restored=@()
    foreach($actor in @('rider','mount','unrelated')) {
        $facts+=@{actor=$actor;nativeComponent='Kingmaker.UnitLogic.Buffs.Components.AddEffectFastHealing'}
        $restored+=@{actor=$actor;restored=$true;originalDamage=0;currentDamage=0}
        for($round=1;$round-le3;$round++) {
            $measurements+=@{actor=$actor;round=$round}
            foreach($boundary in @('prepare-before','clear-before','clear-after','round-state-before','round-state-after',
                'round-handler-before','round-handler-after','ai-round-before','ai-round-after','fact-before','fact-after',
                'ready-handler-before','ready-handler-after','prepare-after','end-turn-input-before','turn-end-before','turn-end-after','end-turn-input-after')) {
                $sequence++
                $events+=@{sequence=$sequence;round=$round;boundary=$boundary;callbackObject=42
                    detail='Kingmaker.UnitLogic.Buffs.Components.AddEffectFastHealing'
                    state=@{actor=$actor;damage=$(if($boundary-ceq'fact-after'){3}else{4});move=0.4;standard=0
                        retained=@{Round=$round;MoveUsed=0.4;StandardUsed=$false}}}
            }
        }
    }
    $e.observations.actorAllocationTrace.events=$events
    $e.observations | Add-Member allocationNativeRoundFacts $facts
    $e.observations | Add-Member allocationNativeRoundFactsRestored $restored
    $e.rows+=@{name='A05-native-preparation-callbacks';status='PASS';evidence=@{level='NATIVE INTEGRATION';passed=$true;errors=@();measurements=$measurements}}
    $e.subscenarioPassCount=2
    return ($e|ConvertTo-Json -Depth 25|ConvertFrom-Json)
}
$valid=New-CallbackEnvelope
Assert-KmcActorAllocationEvidence $request $valid 'PASS'
$passes++
$feature=New-CallbackEnvelope
foreach($fact in $feature.observations.allocationNativeRoundFacts) {$fact.nativeComponent='Kingmaker.UnitLogic.Mechanics.Components.AddFactContextActions'}
foreach($event in $feature.observations.actorAllocationTrace.events) {$event.detail='Kingmaker.UnitLogic.Mechanics.Components.AddFactContextActions'}
Assert-KmcActorAllocationEvidence $request $feature 'PASS'
$passes++
foreach($mutation in @(
    {param($e) $e.observations.actorAllocationTrace.events=@($e.observations.actorAllocationTrace.events|Where-Object boundary -CNE 'fact-after')},
    {param($e) $e.observations.actorAllocationTrace.events+=@($e.observations.actorAllocationTrace.events|Where-Object boundary -CEQ 'fact-before'|Select-Object -First 1)},
    {param($e) ($e.observations.actorAllocationTrace.events|Where-Object boundary -CEQ 'fact-after'|Select-Object -First 1).state.damage=4},
    {param($e) ($e.observations.actorAllocationTrace.events|Where-Object boundary -CEQ 'fact-before'|Select-Object -First 1).state.move=0},
    {param($e) ($e.observations.actorAllocationTrace.events|Where-Object boundary -CEQ 'ready-handler-before'|Select-Object -First 1).state.retained.StandardUsed=$true},
    {param($e) ($e.observations.actorAllocationTrace.events|Where-Object boundary -CEQ 'round-handler-after'|Select-Object -First 1).callbackObject=99},
    {param($e) $e.observations.allocationNativeRoundFactsRestored[0].restored=$false},
    {param($e) $e.observations.allocationNativeRoundFactsRestored[0].actor='another-actor'}
)) {
    $e=New-CallbackEnvelope; & $mutation $e
    $rejected=$false
    try {Assert-KmcActorAllocationEvidence $request $e 'PASS'} catch {$rejected=$true}
    if(!$rejected){throw 'Malformed native callback evidence was accepted.'}
    $passes++
}
function New-ConservationEnvelope {
    $e=New-CallbackEnvelope
    $samples=@()
    foreach($round in 4..5) {
        foreach($actor in @('rider','mount')) {
            foreach($boundary in @('prepare-before','prepare-after','turn-end-before','turn-end-after')) {
                $e.observations.actorAllocationTrace.events+=@{round=$round;boundary=$boundary;state=@{actor=$actor}}
            }
            $samples+=@{round=$round;actor=$actor;mover='mount';before=@{actor='mount'};after=@{actor='mount'}
                attempts=@(@{before=@{actor='mount'};after=@{actor='mount'};clicked=$true;admitted=$true;distance=3.0
                    riderBefore=@{actor='rider'};riderAfter=@{actor='rider'};mountBefore=@{actor='mount'};mountAfter=@{actor='mount'}})}
        }
    }
    $e.rows+=@{name='T02-native-exhaustion-refresh-trace';status='PASS';evidence=@{level='NATIVE INTEGRATION';gameplayQualified=$false
        coverageComplete=$true;inputKind='scripted-native-handler-integration';firstRound=4;endRound=6;samples=$samples}}
    $e.subscenarioPassCount=3
    return ($e|ConvertTo-Json -Depth 25|ConvertFrom-Json)
}
Assert-KmcActorAllocationEvidence $request (New-ConservationEnvelope) 'PASS'
$passes++
foreach($mutation in @(
    {param($e) $e.rows[2].evidence.gameplayQualified=$true},
    {param($e) $e.rows[2].evidence.endRound=5},
    {param($e) $e.rows[2].evidence.samples[0].mover='rider'},
    {param($e) $e.rows[2].evidence.samples[0].attempts[0].riderAfter.actor='mount'},
    {param($e) $e.observations.actorAllocationTrace.events=@($e.observations.actorAllocationTrace.events|Where-Object {!($_.round -eq 5 -and $_.boundary -ceq 'turn-end-after')})}
)) {
    $e=New-ConservationEnvelope; & $mutation $e
    $rejected=$false
    try {Assert-KmcActorAllocationEvidence $request $e 'PASS'} catch {$rejected=$true}
    if(!$rejected){throw 'Malformed conservation trace was accepted.'}
    $passes++
}
# The preparation contract is shared; schema 11 binds its coverage to the new
# paired loop row without relaxing callback cardinality or effect delivery.
$pairedCallbacks=New-CallbackEnvelope
$pairedCallbacks.schemaVersion=11
$pairedCallbacks.rows[0].name='P01-three-paired-activations'
Assert-KmcAllocationCallbackEvidence $pairedCallbacks $pairedCallbacks.rows[1].evidence
$passes++
$pairedCallbacks.observations.actorAllocationTrace.events+=@($pairedCallbacks.observations.actorAllocationTrace.events|Where-Object boundary -CEQ 'fact-before'|Select-Object -First 1)
$rejected=$false
try {Assert-KmcAllocationCallbackEvidence $pairedCallbacks $pairedCallbacks.rows[1].evidence} catch {$rejected=$true}
if(!$rejected){throw 'Paired callback schema accepted a duplicated effect.'}
$passes++
# A native turn may curve away from its endpoint while consuming real action
# time. Require the physical path and the native shifts, with unchanged cost
# conservation and displacement tolerance; net displacement is not path length.
$curved=[pscustomobject]@{purpose='conversion';admitted=$true;distance=3.0;travelledDistance=3.8
    nativeShiftDistance=3.8;nativeMoveCost=0.79;nativeAllowedTime=0.79;before=@{speedMps=5.08}}
Assert-KmcPairedMovementEvidence $curved
$passes++
foreach($mutation in @(
    {param($e) $e.nativeMoveCost=0.4},
    {param($e) $e.travelledDistance=2.5},
    {param($e) $e.nativeShiftDistance=3.0},
    {param($e) $e.travelledDistance=4.7;$e.nativeShiftDistance=4.7},
    {param($e) $e.travelledDistance=$null},
    {param($e) $e.purpose='exhausted-rejection'}
)) {
    $e=$curved|ConvertTo-Json -Depth 5|ConvertFrom-Json; & $mutation $e
    $rejected=$false
    try {Assert-KmcPairedMovementEvidence $e} catch {$rejected=$true}
    if(!$rejected){throw 'Malformed paired movement measurement was accepted.'}
    $passes++
}
Write-Host "ALLOCATION PROTOCOL PASS=$passes FAIL=0 (envelope validation only)"
