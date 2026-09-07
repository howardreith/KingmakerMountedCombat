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
function New-PairedEnvelope {
    $e=New-CallbackEnvelope
    $e.schemaVersion=11
    $e.observations.phase3fActualConfiguration | Add-Member enablePairedActivation $true
    $activations=@();$visits=@()
    foreach($round in 1..3) {
        $visits+=@{actor='rider';round=$round;friendly=$true;principal=$true;mount=$false}
        $visits+=@{actor='unrelated';round=$round;friendly=$true;principal=$false;mount=$false}
        $visits+=@{actor='enemy';round=$round;friendly=$false;principal=$false;mount=$false}
        $operations=@(@{kind='movement';purpose='partial';samePrincipalTurn=$true;fiveFootStep=$false;singleMove=$false
            admitted=$true;distance=1.0;travelledDistance=1.0;nativeShiftDistance=1.0;nativeMoveCost=0.2;nativeAllowedTime=0.2
            before=@{speedMps=5.0};riderBefore=@{move=0.0};riderAfter=@{move=0.0}})
        if($round -eq 1) {
            foreach($actor in @('rider','mount')) {
                $operations+=@{kind='attack';actor=$actor;clicked=$true;result='Success';completedAttacks=1;nativeRule=$true;after=@{standard=6.0}}
            }
            $operations+=@{kind='movement';purpose='exhausted-rejection';samePrincipalTurn=$true;fiveFootStep=$false;singleMove=$false
                admitted=$false;distance=0.0;travelledDistance=0.0;nativeShiftDistance=0.0;nativeMoveCost=0.0;nativeAllowedTime=0.0
                riderBefore=@{move=0.0};riderAfter=@{move=0.0}}
        }
        $activations+=@{index=$round;identity=('11111111111111111111111111111111:'+ $round);round=$round
            riderBefore=@{standard=0.0;move=0.0;canAct=$true};mountBefore=@{standard=0.0;move=0.0;canAct=$true}
            operations=$operations;exhaustedMovementRejected=($round -eq 1);conversionAttackRejected=($round -eq 2);earlyEnd=($round -eq 3)}
    }
    $e.rows[0].name='P01-three-paired-activations'
    $e.rows[0].evidence=[pscustomobject]@{level='NATIVE INTEGRATION';gameplayQualified=$true;inputKind='scripted-native-handler-integration'
        principal='rider';firstRound=1;activations=$activations;turnVisits=$visits;errors=@()
        refresh=@{identity='11111111111111111111111111111111:4';rider=@{standard=0.0;move=0.0;canAct=$true};mount=@{standard=0.0;move=0.0;canAct=$true}}}
    return ($e|ConvertTo-Json -Depth 30|ConvertFrom-Json)
}
Assert-KmcActorAllocationEvidence $request (New-PairedEnvelope) 'PASS'
$passes++
foreach($mutation in @(
    {param($e) $e.observations.actorAllocationTrace.observationErrors=19},
    {param($e) $e.observations.actorAllocationTrace.dropped=1},
    {param($e) $e.rows[0].evidence.activations[1].identity=$e.rows[0].evidence.activations[0].identity},
    {param($e) $e.rows[0].evidence.turnVisits[1].actor='mount'},
    {param($e) $e.rows[0].evidence.activations[0].operations[0].riderAfter.move=0.2},
    {param($e) $e.rows[0].evidence.activations[1].mountBefore.standard=6.0},
    {param($e) $e.rows[0].evidence.refresh.mount.move=6.0},
    {param($e) $e.observations.actorAllocationTrace.events+=@($e.observations.actorAllocationTrace.events[0])}
)) {
    $e=New-PairedEnvelope; & $mutation $e
    $rejected=$false
    try {Assert-KmcActorAllocationEvidence $request $e 'PASS'} catch {$rejected=$true}
    if(!$rejected){throw 'Incomplete or resource-violating paired loop envelope was accepted.'}
    $passes++
}
function New-PairedReactionEnvelope {
    $e=New-PairedEnvelope
    $e.schemaVersion=12
    $e.observations | Add-Member reactionTargetCondition ([pscustomobject]@{actor='enemy';condition='ImmuneToCombatManeuvers';before=$false;applied=$true;restored=$true})
    $reaction=[pscustomobject]@{passed=$true;inputKind='scripted-native-AI-command';nativeTurnActor='enemy'
        activationIdentity='11111111111111111111111111111111:1';refreshIdentity='11111111111111111111111111111111:2'
        mountBefore=@{reactions=1};mountAfterConsumption=@{reactions=0};mountAfterRefresh=@{reactions=1;reactionCooldown=0;disengageTargets=0}
        operations=@()}
    foreach($leg in 1..4) {
        $reaction.operations+=@{leg=$leg;before=@{actor='enemy'};after=@{actor='enemy'};result='Success';distance=4.0
            nativeTime=1.0;nativeCost=1.0;mountOpportunityRules=1
            mountBefore=@{standard=6.0;move=3.0};mountAfter=@{standard=6.0;move=3.0;reactions=0}}
    }
    $e.rows[0].evidence | Add-Member reactions $reaction
    return ($e|ConvertTo-Json -Depth 30|ConvertFrom-Json)
}
Assert-KmcActorAllocationEvidence $request (New-PairedReactionEnvelope) 'PASS'
$passes++
foreach($mutation in @(
    {param($e) $e.rows[0].evidence.reactions=$null},
    {param($e) $e.rows[0].evidence.reactions.operations[0].nativeCost=0.0},
    {param($e) $e.rows[0].evidence.reactions.operations[2].mountOpportunityRules=2},
    {param($e) $e.rows[0].evidence.reactions.operations[0].mountAfter.standard=0.0},
    {param($e) $e.rows[0].evidence.reactions.mountAfterRefresh.reactions=0},
    {param($e) $e.rows[0].evidence.reactions.mountAfterRefresh.disengageTargets=1},
    {param($e) $e.observations.reactionTargetCondition.restored=$false}
)) {
    $e=New-PairedReactionEnvelope; & $mutation $e
    $rejected=$false
    try {Assert-KmcActorAllocationEvidence $request $e 'PASS'} catch {$rejected=$true}
    if(!$rejected){throw 'Incomplete native reaction lifecycle envelope was accepted.'}
    $passes++
}
function New-PairedTransitionEnvelope {
    $e=New-PairedReactionEnvelope
    $e.schemaVersion=13
    $ledger=New-Object Collections.ArrayList
    foreach($event in $e.observations.actorAllocationTrace.events){[void]$ledger.Add($event)}
    $samples=New-Object Collections.ArrayList
    function Add-TransitionSample([string]$kind,[long]$ticks,[int]$round,[string]$identity,[double]$standard,[double]$move,[bool]$riderGrant,[bool]$mountGrant) {
        foreach($actor in @('rider','mount')) {
            if($actor -ceq 'rider' -and $riderGrant -or $actor -ceq 'mount' -and $mountGrant) {
                foreach($boundary in @('clear-after','round-state-after')) {
                    [void]$ledger.Add(@{sequence=$ledger.Count+1;boundary=$boundary;round=$round;gameTicks=$ticks;state=@{actor=$actor}})
                }
            }
        }
        [void]$ledger.Add(@{sequence=$ledger.Count+1;boundary=$kind;round=$round;gameTicks=$ticks;state=@{actor='rider'}})
        $sample=@{kind=$kind;frame=$ledger.Count;traceSequence=$ledger.Count;round=$round;gameTicks=$ticks;identity=$identity;currentActor='rider'}
        foreach($actor in @('rider','mount')) {
            $sample[$actor]=@{actor=$actor;standard=$standard;move=$move;reactions=1}
            $sample[$actor+'Clears']=@($ledger|Where-Object {$_.state.actor -ceq $actor -and $_.boundary -ceq 'clear-after'}).Count
            $sample[$actor+'Effects']=@($ledger|Where-Object {$_.state.actor -ceq $actor -and $_.boundary -ceq 'round-state-after'}).Count
        }
        [void]$samples.Add($sample)
    }
    [void]$ledger.Add(@{sequence=$ledger.Count+1;boundary='first-gate-sealed';round=4;gameTicks=100;state=@{actor='rider'}})
    $e.rows[0].evidence | Add-Member traceEndSequence $ledger.Count
    $id4='11111111111111111111111111111111:4';$id5='11111111111111111111111111111111:5'
    Add-TransitionSample 'native-delay-input-before' 100 4 $id4 0 0 $true $true
    Add-TransitionSample 'native-delay-existing-grant-resumed' 100 4 $id4 0 0 $false $false
    Add-TransitionSample 'used-pair-delay-input-before' 100 4 $id4 0 0.1 $false $false
    Add-TransitionSample 'used-pair-delay-rejected' 100 4 $id4 0 0.1 $false $false
    Add-TransitionSample 'native-mode-exit-before' 100 4 $id4 0 0.1 $false $false
    Add-TransitionSample 'native-mode-exit-after' 100 0 $id4 6 3 $false $false
    Add-TransitionSample 'native-mode-next-paired-grant' 60000100 1 $id5 0 0 $true $true
    Add-TransitionSample 'native-dismount-input-before' 60000100 1 $id5 0 0 $false $false
    Add-TransitionSample 'native-dismount-after' 60000100 1 $id5 0 0 $false $false
    Add-TransitionSample 'native-split-next-independent-mount-grant' 120000100 2 $id5 0 0 $false $true
    $moves=@(
        @{purpose='partial-stop';admitted=$true;distance=0.5;travelledDistance=0.5;nativeShiftDistance=0.5;nativeMoveCost=0.1;nativeAllowedTime=0.1
            before=@{speedMps=5.0};nativeTbStopVerified=$true;stopInput=$true;pausedAtStop=$false;pausedAfterStop=$false
            stopBefore=@{move=0.1};stopAfter=@{move=0.1};riderBefore=@{move=0};riderAfter=@{move=0}},
        @{purpose='five-foot-step';admitted=$true;distance=1.0;travelledDistance=1.0;nativeShiftDistance=1.0;nativeMoveCost=0;nativeAllowedTime=0.2
            before=@{speedMps=5.0;stepRangeMetres=1.524};after=@{metresStepped=1.0;standard=0};fiveFootStep=$true;riderBefore=@{move=0};riderAfter=@{move=0}},
        @{purpose='ordinary-after-step-rejected';distance=0;travelledDistance=0;nativeShiftDistance=0;nativeMoveCost=0;nativeAllowedTime=0
            riderBefore=@{move=0};riderAfter=@{move=0}}
    )
    $e.rows+=@{name='P02-paired-native-transitions';status='PASS';evidence=@{level='NATIVE INTEGRATION';passed=$true
        inputKind='scripted-native-control-integration';modeSettingRestored=$true;events=@($samples);movements=$moves}}
    $e.observations.actorAllocationTrace.events=@($ledger)
    $e.subscenarioPassCount=3
    return ($e|ConvertTo-Json -Depth 35|ConvertFrom-Json)
}
Assert-KmcActorAllocationEvidence $request (New-PairedTransitionEnvelope) 'PASS'
$passes++
foreach($mutation in @(
    {param($e) $e.rows[0].evidence.traceEndSequence=0},
    {param($e) $e.rows[2].evidence.modeSettingRestored=$false},
    {param($e) $e.rows[2].evidence.events[1].identity='11111111111111111111111111111111:5'},
    {param($e) $e.rows[2].evidence.events[1].mountEffects++},
    {param($e) $e.rows[2].evidence.events[3].mount.move=0},
    {param($e) $e.rows[2].evidence.events[5].mount.standard=0},
    {param($e) $e.rows[2].evidence.events[6].gameTicks=101},
    {param($e) $e.rows[2].evidence.events[9].round=1},
    {param($e) $e.rows[2].evidence.movements[0].nativeTbStopVerified=$false},
    {param($e) $e.rows[2].evidence.movements[0].stopAfter.move=0},
    {param($e) $e.rows[2].evidence.movements[0].pausedAtStop=$true},
    {param($e) $e.rows[2].evidence.movements[1].nativeMoveCost=0.1},
    {param($e) $e.rows[2].evidence.movements[2].distance=0.5}
)) {
    $e=New-PairedTransitionEnvelope; & $mutation $e
    $rejected=$false
    try {Assert-KmcActorAllocationEvidence $request $e 'PASS'} catch {$rejected=$true}
    if(!$rejected){throw 'Resource-violating or unbound native transition evidence was accepted.'}
    $passes++
}
Write-Host "ALLOCATION PROTOCOL PASS=$passes FAIL=0 (envelope validation only)"
