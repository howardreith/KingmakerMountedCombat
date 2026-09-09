$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')

$passed=0
function New-ObstructionState([string]$Target='blocked'){
    return @{live=@{relationship='Mounted';rider=@{id='rider';standard=0;move=0;swift=0};mount=@{id='mount';standard=0;move=0;swift=0}};
        sight=@{actor='rider';target=$Target;needLoS=$true;blocked=($Target -ceq 'blocked');enoughClose=$false;weapon='d5947b9cb1500c040b026bf6b4b57fa7'};
        frame=10;gameSeconds=1;mountCorpulence=.7;mountAgentEnabled=$true;avoidanceDisabled=$false;
        intent=$false;activeCommand=$false;groundMovement=$false;target=@{id=$Target;conscious=$true;dead=$false}}
}
function New-ObstructionEnvelope([bool]$Tail){
    $completed=if($Tail){3}else{4};$result=if($Tail){'Interrupt'}else{'Success'}
    $e=@{caseId='C4-OBSTRUCTION-ranged-native-geometry';level='NATIVE INTEGRATION';mode='RT';inputKind='native-pointer-prediction-and-click';
        initialQueryPure=$true;inputAccepted=$true;before=(New-ObstructionState);afterInput=(New-ObstructionState);
        beforeStop=(New-ObstructionState);afterStopInput=(New-ObstructionState);samples=@((New-ObstructionState));
        recoveryBefore=(New-ObstructionState 'clear');recoveryBeforeStop=(New-ObstructionState 'clear');recoveryAfterStopInput=(New-ObstructionState 'clear');
        after=(New-ObstructionState 'clear');observationSeconds=1;blockedFrames=10;sampleDrops=0;candidates=@(@{blocked=$true;point=@(1,2,3)});
        blockedTrace=@(@{actor='rider';boundary='delivery-before';nativeCommandLoS=$true;target='blocked'});
        recoveryCommand=@{id=1;executor='rider';started=$true;acted=$true;finished=$true;result=$result};recoveryTarget='clear';
        recoveryCompleted=$completed;recoveryPlan=4;recoveryNativeRangeTail=$Tail;recoveryNativeRecovery=$null;
        recoveryTrace=@(@{actor='rider';command=1;boundary='start-after'},@{actor='rider';command=1;boundary='cost-after'})+
            @(1..$completed|ForEach-Object {@{actor='rider';command=1;boundary='delivery-after'}});
        rulesAfter=@{dropped=0;attacks=@(1..$completed|ForEach-Object {@{identity=$_;actor='rider';target='clear';resolved=$true}});
            events=@(1..$completed|ForEach-Object {@{kind='weapon-resolved';attack=$_;actor='rider';target='clear';firstResolution=$true}})};
        recoveryRangeRejection=@{command=1;nativeCommandLoS=$true;nativeSequenceTick=$true;nativeMeleeTailRangeRejected=$true;
            targetDead=$false;targetUnconscious=$false;targetInState=$true;completed=$completed;rangeOriginDistance=10;mountCorpulence=.7;targetCorpulence=.5;
            plan=@(@{ranged=$true;weaponRange=15.24},@{ranged=$true;weaponRange=15.24},@{ranged=$true;weaponRange=15.24},@{ranged=$false;weaponRange=.6096})}}
    return (@{schemaVersion=23;status='PASS';rows=@(@{name=$e.caseId;status='PASS';evidence=$e});errors=@();subscenarioPassCount=1;subscenarioFailCount=0;
        observations=@{phase3fActualConfiguration=@{enablePairedActivation=$true;enableUnifiedMountedTurn=$false;enablePairedCommandScheduler=$false;
            enableDiagnosticOverlay=$false;overlayPresent=$false};ordinaryAttackTrace=@{dropped=0;events=@()}}}|ConvertTo-Json -Depth 30|ConvertFrom-Json)
}
$request=@{scenario='chunk4-obstruction-ranged-rt'}
foreach($tail in @($false,$true)){
    Assert-KmcChunk4ExtendedEvidence $request (New-ObstructionEnvelope $tail) 'PASS';$passed++
    $mutations=@(
        {param($a) $a.initialQueryPure=$false},{param($a) $a.before.sight.blocked=$false},
        {param($a) $a.before.sight.needLoS=$false},{param($a) $a.before.sight.actor='mount'},
        {param($a) $a.before.sight.enoughClose=$true},{param($a) $a.before.sight.weapon='bite'},
        {param($a) $a.afterInput.live.rider.standard=6},{param($a) $a.afterStopInput.live.mount.move=3},
        {param($a) $a.recoveryAfterStopInput.live.rider.standard=6},{param($a) $a.sampleDrops=1},
        {param($a) $a.blockedFrames=0},{param($a) $a.samples=@()},{param($a) $a.candidates=@()},
        {param($a) $a.samples+=@($a.samples[0])},{param($a) $a.samples[0].sight.blocked=$false},
        {param($a) $a.samples[0].sight.target='clear'},{param($a) $a.samples[0].live.rider.move=3},
        {param($a) $a.samples[0].mountCorpulence=.1},{param($a) $a.samples[0].avoidanceDisabled=$true},
        {param($a) $a.samples[0].mountAgentEnabled=$false},{param($a) $a.samples[0].target.conscious=$false},
        {param($a) $a.blockedTrace[0].nativeCommandLoS=$false},{param($a) $a.blockedTrace[0].target='clear'},
        {param($a) $a.recoveryBefore.sight.blocked=$true},{param($a) $a.recoveryTarget='blocked'},
        {param($a) $a.recoveryCompleted=0},{param($a) $a.recoveryCommand.started=$false},
        {param($a) $a.recoveryCommand.executor='mount'},{param($a) $a.recoveryTrace=@()},
        {param($a) $a.rulesAfter.dropped=1},{param($a) $a.rulesAfter.attacks[0].resolved=$false},
        {param($a) $a.rulesAfter.events[0].target='blocked'},{param($a) $a.rulesAfter.events+=@($a.rulesAfter.events[0])},
        {param($a) $a.after.activeCommand=$true},{param($a) $a.after.intent=$true},{param($a) $a.after.groundMovement=$true})
    if($tail){$mutations+=@(
        {param($a) $a.recoveryRangeRejection.nativeSequenceTick=$false},
        {param($a) $a.recoveryRangeRejection.nativeCommandLoS=$false},
        {param($a) $a.recoveryRangeRejection.nativeMeleeTailRangeRejected=$false},
        {param($a) $a.recoveryRangeRejection.rangeOriginDistance=1},
        {param($a) $a.recoveryRangeRejection.plan[3].ranged=$true},
        {param($a) $a.recoveryRangeRejection.plan[0].weaponRange=1},
        {param($a) $a.recoveryRangeRejection.targetDead=$true},
        {param($a) $a.recoveryRangeRejection.completed=4})}
    foreach($mutation in $mutations){
        $changed=New-ObstructionEnvelope $tail;& $mutation $changed.rows[0].evidence;$rejected=$false
        try{Assert-KmcChunk4ExtendedEvidence $request $changed 'PASS'}catch{$rejected=$true}
        if(!$rejected){throw 'Invalid native obstruction evidence was accepted by parser.'};$passed++
    }
}
Write-Output "COMPONENT parser-only TOTAL PASS=$passed FAIL=0"
