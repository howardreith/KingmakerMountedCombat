$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
$passed=0
function New-ChargePauseProof {
    param([string]$ActorId='rider')
    $actors=@{rider=@{id='rider';standard=0;move=0;swift=0;position=@(1,2,3)}
        mount=@{id='mount';standard=0;move=0;swift=0;position=@(3,2,1)}
        actor=@{id=$ActorId;standard=0;move=0;swift=0;position=@(1,2,3)}}
    return @{kind='native-real-time-pause';requestFrame=5;inputFrame=6;afterInputFrame=6;heldFrame=7;beforePaused=$true;afterInputPaused=$true;heldPaused=$true
        gameTimeBefore=100;gameTimeAfterInput=100;gameTimeAfterHold=100
        actorsBefore=$actors;actorsAfterInput=$actors;actorsAfterHold=$actors}
}
function New-ChargeInputWindow {
    param([string]$Mode,[string]$ActorId='rider')
    $proof=New-ChargePauseProof $ActorId
    if($Mode -ceq 'RT'){return $proof}
    foreach($key in @('requestFrame','heldFrame','heldPaused','gameTimeAfterHold','actorsAfterHold')){$proof.Remove($key)}
    $proof.kind='native-turn-based-input';$proof.beforePaused=$false;$proof.afterInputPaused=$false
    $proof.turnBefore=if($ActorId -ceq 'mount'){'rider'}else{$ActorId};$proof.turnAfter=$proof.turnBefore
    $proof.turnStatusBefore='Preparing';$proof.turnStatusAfter='Acting'
    return $proof
}
function New-ChargeEnvelope {
    param([string]$Mode,[int]$Schema=26)
    # Synthetic envelopes exercise evidence validation only, never gameplay.
    $rows=@()
    foreach($mounted in @($true,$false)) {
        $id=if($mounted){'C4-CHARGE-mounted-rider'}else{'C4-CHARGE-unmounted-rider'}
        $rows+=@{name=$id;status='PASS';evidence=@{
            level='NATIVE INTEGRATION';inputKind='scripted-native-handler-integration';mode=$Mode;mounted=$mounted;hoverPure=$true
            identity=@(@{logic='Kingmaker.UnitLogic.Abilities.Components.AbilityCustomCharge';assemblyMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';blueprint='c78506dd0e14f7c45a599990e4e65038'})
            samples=@(@{nativeSeconds=1});rules=@{pairForcedD20=0;mountAttackRules=0;riderAttackRules=$(if($mounted){0}else{1});riderResolved=$(if($mounted){0}else{1})}
            actorId='rider';actorIsRider=$true;actorIsMount=$false;pairMounted=$mounted;warningDelta=$(if($mounted){1}else{0})
            actorDistance=$(if($mounted){0}else{5});maximumActorStandard=$(if($mounted){0}else{6})
            safeRejected=$mounted;observedCharging=(!$mounted);riderDistance=$(if($mounted){0}else{5});mountDistance=0
            maximumRiderStandard=$(if($mounted){0}else{6});maximumRiderMove=0;nativeChargeCompleted=(!$mounted)
            maximumMountStandard=0;maximumMountMove=0
            before=@{actor=@{id='rider'};rider=@{id='rider'};mount=@{id='mount'};nativeCanTarget=(!$mounted);nativeAvailable=(!$mounted);nativeReason='Charge is not yet supported while mounted.';nativeGeometry=@{customCanTarget=$true}}
            feedback='Charge is not yet supported while mounted.'
            nativeWarnings=@(1..4|ForEach-Object {@{text='Charge is not yet supported while mounted.';addToLog=$true}})
            pausedInput=(New-ChargePauseProof)
            inputWindow=(New-ChargeInputWindow $Mode)
            recovery=@{completed=$true;moveDistance=2;afterMove=@{rider=@{id='rider';move=0};mount=@{id='mount'}};nativeEndInput=$true;nextUnrelatedActor='unrelated'
                liveQueue=@{inputWindow=(New-ChargeInputWindow $Mode);pausedInput=(New-ChargePauseProof);queuedRejectedBeforeInit=$true;queuePure=$true;executionPure=$true;clickPure=$true;preparedStartRejected=$true;clickRejected=$true;sameLiveAttack=$true;warnings=3}
                ordinaryAttack=@{complete=$true;isCharge=$false;planned=2;completed=2;maximumRiderStandard=6;rules=@{riderResolved=2;pairForcedD20=0}}}
        }}
    }
    if($Schema -ge 19) {
        $mount=($rows[0]|ConvertTo-Json -Depth 20|ConvertFrom-Json)
        $mount.name='C4-CHARGE-mounted-mount';$mount.evidence.actorId='mount';$mount.evidence.actorIsRider=$false
        $mount.evidence.actorIsMount=$true;$mount.evidence.before.actor.id='mount';$mount.evidence.pausedInput=New-ChargePauseProof 'mount';$rows+=@($mount)
        $mount.evidence.inputWindow=New-ChargeInputWindow $Mode 'mount'
        $other=($rows[1]|ConvertTo-Json -Depth 20|ConvertFrom-Json)
        $other.name='C4-CHARGE-unrelated-actor';$other.evidence.actorId='unrelated';$other.evidence.actorIsRider=$false
        $other.evidence.before.actor.id='unrelated';$other.evidence.pairMounted=$true;$other.evidence.pausedInput=New-ChargePauseProof 'unrelated';$rows+=@($other)
        $other.evidence.inputWindow=New-ChargeInputWindow $Mode 'unrelated'
        $rows+=@(@{name='C4-CHARGE-queued-state-change';status='PASS';evidence=@{
            level='NATIVE INTEGRATION';mode=$Mode;inputKind='native-mount-handler-and-native-queue-promotion'
            blueprint='c78506dd0e14f7c45a599990e4e65038';availableWhileUnmounted=$true;canTargetWhileUnmounted=$true
            queuedWhileUnmounted=$true;pausedQueueCostsPure=$true;nativeQueueApi='UnitCommands.AddToQueueInternal060026B8'
            mountSucceeded=$true;combatBeforeMount=$false;rejectedBeforeStart=$true;admissionCostsAndPositionPure=$true
            chargingObserved=$false;warningDelta=1;rulesBeforeRecovery=@{pairForcedD20=0;riderAttackRules=0;mountAttackRules=0}
            admission=@(@{boundary='private-run-before';command=42;started=$false;acted=$false;finished=$false;standard=0;move=0;actorPosition=@(1,2,3)},
                @{boundary='private-run-after';command=42;started=$false;acted=$false;finished=$true;standard=0;move=0;actorPosition=@(1,2,3)})
            recovery=$rows[0].evidence.recovery
        }})
        if($Schema -in @(20,24,25,26)) {
            $queued=$rows[4].evidence
            foreach($boundary in $queued.admission) {
                $boundary.relationship='Unmounted';$boundary.charging=$false;$boundary.frame=10
                $boundary.mountStandard=0;$boundary.mountMove=0;$boundary.mountPosition=@(3,2,1)
            }
            $queued.admission[1].finished=$false
            $queued.executionRejectedWhileMounted=$true
            $queued.charge=@{started=$false;acted=$false;finished=$true;queued=$false;contained=$false}
            $queued.approachExecution=@(
                @{boundary='charge-approach-before';relationship='Mounted';command=42;frame=11;started=$false;acted=$false;finished=$false;charging=$false;standard=0;move=0;mountStandard=0;mountMove=0;actorPosition=@(1,3,3);mountPosition=@(3,2,1)},
                @{boundary='charge-approach-after';relationship='Mounted';command=42;frame=11;started=$false;acted=$false;finished=$true;charging=$false;standard=0;move=0;mountStandard=0;mountMove=0;actorPosition=@(1,3,3);mountPosition=@(3,2,1)})
        }
        if($Schema -in @(24,25,26)) {
            $queued=$rows[4].evidence
            $queued.inputKind='native-mount-delivery-and-native-queue-promotion'
            $queued.queueBoundary='NativeMountedControlService.TryDispatch:MountCompanion:before'
            $queued.queueFrame=9;$queued.queuePaused=($Schema -eq 24);$queued.before=@{relationship='Unmounted'}
            if($Schema -ge 25){$queued.Remove('pausedQueueCostsPure');$queued.queueCostsAndPositionsPure=$true}
            $queued.mountAtQueue=@{present=$true;abilityGuid='f053faad986631688defa003cd7bda0e';executorId='rider';targetId='mount';started=$true;finished=$false;contained=$true;acted=$false;result='None'}
            $queued.mountDelivery=@{frame=9;state=@{relationship='Unmounted'};activeShell=$true;completedShell=$false
                riderInCombat=$false;mountInCombat=$false;playerInCombat=$false;targetAbsent=$true
                executionPresent=$true;executionEnded=$false;engagesUnit=$false;sameAbility=$true
                blueprint='f053faad986631688defa003cd7bda0e';casterId='rider';targetId='mount'}
            $queued.pairBeforeQueue=@{relationship='Unmounted';rider=@{id='rider';standard=0;move=0;swift=0;position=@(1,2,3)};mount=@{id='mount';standard=0;move=0;swift=0;position=@(3,2,1)}}
            $queued.pairAfterQueue=($queued.pairBeforeQueue|ConvertTo-Json -Depth 10|ConvertFrom-Json)
            $state=($queued.pairBeforeQueue|ConvertTo-Json -Depth 10|ConvertFrom-Json);$state.relationship='Mounted'
            $queued.afterMountDispatch=@{frame=9;accepted=$true;playerInCombat=$false;state=$state}
        }
    }
    return (@{schemaVersion=$Schema;status='PASS';subscenarioPassCount=$rows.Count;subscenarioFailCount=0;errors=@();rows=$rows
        observations=@{phase3fActualConfiguration=@{enableUnifiedMountedTurn=$false;enablePairedCommandScheduler=$false;enablePairedActivation=$true;enableDiagnosticOverlay=$false;overlayPresent=$false}}} | ConvertTo-Json -Depth 20 | ConvertFrom-Json)
}
foreach($mode in @('RT','TB')) {
    $request=@{scenario='chunk4-charge-safety-'+$mode.ToLowerInvariant()}
    if(@(Get-KmcSaveBackedRuntimeScenarios | Where-Object {$_ -ceq $request.scenario}).Count -ne 1) {throw 'Charge scenario registration missing or duplicate.'}
    $artifact=New-ChargeEnvelope $mode
    Assert-KmcChunk4ChargeEvidence $request $artifact 'PASS';$passed++
    Assert-KmcChunk4ChargeEvidence $request (New-ChargeEnvelope $mode 18) 'PASS';$passed++
    Assert-KmcChunk4ChargeEvidence $request (New-ChargeEnvelope $mode 19) 'PASS';$passed++
    Assert-KmcChunk4ChargeEvidence $request (New-ChargeEnvelope $mode 20) 'PASS';$passed++
    Assert-KmcChunk4ChargeEvidence $request (New-ChargeEnvelope $mode 24) 'PASS';$passed++
    Assert-KmcChunk4ChargeEvidence $request (New-ChargeEnvelope $mode 25) 'PASS';$passed++
    $legacyPause=New-ChargeEnvelope $mode 25;$legacyPause.rows[0].evidence.pausedInput.beforePaused=$false
    $rejected=$false;try{Assert-KmcChunk4ChargeEvidence $request $legacyPause 'PASS'}catch{$rejected=$true}
    if(!$rejected){throw 'Legacy schema25 unpaused input was retroactively accepted.'};$passed++
    $legacy=New-ChargeEnvelope $mode 24;$legacy.rows[4].evidence.queuePaused=$false
    $rejected=$false;try{Assert-KmcChunk4ChargeEvidence $request $legacy 'PASS'}catch{$rejected=$true}
    if(!$rejected){throw 'Legacy schema24 unpaused delivery was retroactively accepted.'};$passed++
    $completed=New-ChargeEnvelope $mode
    $completed.rows[4].evidence.mountAtQueue.finished=$true;$completed.rows[4].evidence.mountAtQueue.acted=$true
    $completed.rows[4].evidence.mountAtQueue.result='Success';$completed.rows[4].evidence.mountAtQueue.contained=$false
    $completed.rows[4].evidence.mountDelivery.activeShell=$false;$completed.rows[4].evidence.mountDelivery.completedShell=$true
    Assert-KmcChunk4ChargeEvidence $request $completed 'PASS';$passed++
    foreach($mutation in @(
        {$args[0].rows[4].evidence.mountAtQueue.acted=$false},
        {$args[0].rows[4].evidence.mountAtQueue.result='Fail'},
        {$args[0].rows[4].evidence.mountAtQueue.started=$false}
    )) {
        $changed=$completed|ConvertTo-Json -Depth 30|ConvertFrom-Json;& $mutation $changed
        $rejected=$false;try{Assert-KmcChunk4ChargeEvidence $request $changed 'PASS'}catch{$rejected=$true}
        if(!$rejected){throw 'Unexecuted or failed Mount shell accepted as pending native delivery.'};$passed++
    }
    foreach($mutation in @(
        {$args[0].observations.phase3fActualConfiguration.enablePairedActivation=$false},
        {$args[0].rows[0].evidence.maximumRiderMove=0.01},
        {$args[0].rows[0].evidence.before.nativeCanTarget=$true},
        {$args[0].rows[0].evidence.maximumMountStandard=0.01},
        {$args[0].rows[0].evidence.maximumMountMove=-1},
        {$args[0].rows[0].evidence.identity[0].blueprint=('a'*32)},
        {$args[0].rows[0].evidence.nativeWarnings=@()},
        {$args[0].rows[0].evidence.recovery.liveQueue.sameLiveAttack=$false},
        {$args[0].rows[0].evidence.recovery.liveQueue.queuePure=$false},
        {$args[0].rows[0].evidence.recovery.liveQueue.preparedStartRejected=$false},
        {$args[0].rows[0].evidence.recovery.ordinaryAttack.complete=$false},
        {$args[0].rows[0].evidence.recovery.afterMove.rider.move=3},
        {$args[0].rows[0].evidence.riderDistance=0.1},
        {$args[0].rows[0].evidence.observedCharging=$true},
        {$args[0].rows[0].evidence.hoverPure=$false},
        {$args[0].rows[0].evidence.identity[0].logic='OtherAbility'},
        {$args[0].rows[1].evidence.nativeChargeCompleted=$false},
        {$args[0].rows[1].evidence.rules.riderResolved=0},
        {$args[0].rows[1].evidence.rules.pairForcedD20=1},
        {$args[0].rows[2].evidence.actorIsMount=$false},
        {$args[0].rows[2].evidence.rules.mountAttackRules=1},
        {$args[0].rows[3].evidence.actorId='rider'},
        {$args[0].rows[3].evidence.nativeChargeCompleted=$false},
        {$args[0].rows[4].evidence.mountSucceeded=$false},
        {$args[0].rows[4].evidence.combatBeforeMount=$true},
        {$args[0].rows[4].evidence.queuedWhileUnmounted=$false},
        {$args[0].rows[4].evidence.admission[1].acted=$true},
        {$args[0].rows[4].evidence.admission[1].move=3},
        {$args[0].rows[4].evidence.admission[1].actorPosition[0]=2},
        {$args[0].rows[4].evidence.recovery.ordinaryAttack.complete=$false},
        {$args[0].rows[4].evidence.approachExecution=@()},
        {$args[0].rows[4].evidence.approachExecution[0].relationship='Unmounted'},
        {$args[0].rows[4].evidence.approachExecution[1].charging=$true},
        {$args[0].rows[4].evidence.approachExecution[1].mountMove=3},
        {$args[0].rows[4].evidence.approachExecution[1].actorPosition[0]=2},
        {$args[0].rows[4].evidence.approachExecution[1].started=$true},
        {$args[0].rows[4].evidence.approachExecution[1].finished=$false},
        {$args[0].rows[4].evidence.charge.contained=$true},
        {$args[0].rows[4].evidence.queueBoundary='synthetic-mount'},
        {$args[0].rows[4].evidence.queuePaused=$true},
        {$args[0].rows[4].evidence.queueCostsAndPositionsPure=$false},
        {$args[0].rows[4].evidence.queueFrame=0},
        {$args[0].rows[4].evidence.afterMountDispatch.frame++},
        {$args[0].rows[4].evidence.afterMountDispatch.accepted=$false},
        {$args[0].rows[4].evidence.afterMountDispatch.playerInCombat=$true},
        {$args[0].rows[4].evidence.afterMountDispatch.state.relationship='Unmounted'},
        {$args[0].rows[4].evidence.before.relationship='Mounted'},
        {$args[0].rows[4].evidence.mountAtQueue.started=$false},
        {$args[0].rows[4].evidence.mountAtQueue.finished=$true},
        {$args[0].rows[4].evidence.mountAtQueue.contained=$false},
        {$args[0].rows[4].evidence.mountAtQueue.abilityGuid='another-ability'},
        {$args[0].rows[4].evidence.mountAtQueue.executorId='unrelated'},
        {$args[0].rows[4].evidence.mountAtQueue.targetId='unrelated'},
        {$args[0].rows[4].evidence.mountDelivery.frame++},
        {$args[0].rows[4].evidence.mountDelivery.state.relationship='Mounted'},
        {$args[0].rows[4].evidence.mountDelivery.activeShell=$false},
        {$args[0].rows[4].evidence.mountDelivery.completedShell=$true},
        {$args[0].rows[4].evidence.mountDelivery.riderInCombat=$true},
        {$args[0].rows[4].evidence.mountDelivery.mountInCombat=$true},
        {$args[0].rows[4].evidence.mountDelivery.playerInCombat=$true},
        {$args[0].rows[4].evidence.mountDelivery.targetAbsent=$false},
        {$args[0].rows[4].evidence.mountDelivery.executionPresent=$false},
        {$args[0].rows[4].evidence.mountDelivery.executionEnded=$true},
        {$args[0].rows[4].evidence.mountDelivery.engagesUnit=$true},
        {$args[0].rows[4].evidence.mountDelivery.sameAbility=$false},
        {$args[0].rows[4].evidence.mountDelivery.blueprint='another'},
        {$args[0].rows[4].evidence.mountDelivery.casterId='unrelated'},
        {$args[0].rows[4].evidence.mountDelivery.targetId='unrelated'},
        {$args[0].rows[4].evidence.pairAfterQueue.rider.move=3},
        {$args[0].rows[4].evidence.pairAfterQueue.mount.standard=6},
        {$args[0].rows[4].evidence.pairAfterQueue.mount.swift=6},
        {$args[0].rows[4].evidence.pairAfterQueue.rider.position[0]++},
        {$args[0].rows[4].evidence.pairAfterQueue.mount.position[0]++},
        {$args[0].rows[4].evidence.pairAfterQueue.relationship='Mounted'},
        {$args[0].rows[4].evidence.pairAfterQueue.mount.id='unrelated'},
        {$args[0].rows=@($args[0].rows[0..3]);$args[0].subscenarioPassCount=4},
        {$args[0].rows=@($args[0].rows[0]);$args[0].subscenarioPassCount=1}
    )) {
        $changed=New-ChargeEnvelope $mode;& $mutation $changed
        $rejected=$false;try{Assert-KmcChunk4ChargeEvidence $request $changed 'PASS'}catch{$rejected=$true}
        if(!$rejected){throw 'Unsafe or incomplete Charge evidence accepted.'};$passed++
    }
    foreach($rowIndex in @(0,1,2,3,4)) {
        $locations=if($rowIndex -eq 0){@('direct','recovery')}elseif($rowIndex -eq 4){@('recovery')}else{@('direct')}
        foreach($location in $locations) {
            $mutations=if($mode -ceq 'RT'){@(
                {$args[0].beforePaused=$false}, {$args[0].afterInputPaused=$false}, {$args[0].heldPaused=$false},
                {$args[0].requestFrame=-1}, {$args[0].inputFrame=$args[0].requestFrame}, {$args[0].heldFrame=$args[0].inputFrame},
                {$args[0].gameTimeAfterInput++}, {$args[0].gameTimeAfterHold++}, {$args[0].gameTimeBefore=0.5},
                {$args[0].actorsBefore.rider.standard=-1}, {$args[0].actorsAfterInput.rider.standard=6},
                {$args[0].actorsAfterHold.mount.move=3}, {$args[0].actorsAfterInput.actor.swift=6},
                {$args[0].actorsAfterHold.actor.position[0]++}, {$args[0].actorsAfterHold.rider.position[1]++},
                {$args[0].actorsAfterInput.mount.position[2]++}, {$args[0].actorsAfterHold.mount.position=@(1,2)},
                {$args[0].actorsAfterInput.actor.id='wrong'}, {$args[0].actorsBefore.mount.id='wrong'},
                {$args[0].actorsAfterHold.rider.id='wrong'}, {$args[0].PSObject.Properties.Remove('heldPaused')}
            )}else{@(
                {$args[0].beforePaused=$true}, {$args[0].afterInputPaused=$true}, {$args[0].kind='native-real-time-pause'},
                {$args[0].turnBefore='wrong'}, {$args[0].turnAfter='wrong'}, {$args[0].turnStatusBefore='Ended'}, {$args[0].turnStatusAfter='Ended'},
                {$args[0].inputFrame=-1}, {$args[0].gameTimeAfterInput++}, {$args[0].gameTimeBefore=0.5},
                {$args[0].actorsBefore.rider.standard=-1}, {$args[0].actorsAfterInput.rider.standard=6},
                {$args[0].actorsAfterInput.mount.move=3}, {$args[0].actorsAfterInput.actor.swift=6},
                {$args[0].actorsAfterInput.actor.position[0]++}, {$args[0].actorsAfterInput.rider.position[1]++},
                {$args[0].actorsAfterInput.mount.position[2]++}, {$args[0].actorsAfterInput.mount.position=@(1,2)},
                {$args[0].actorsAfterInput.actor.id='wrong'}, {$args[0].actorsBefore.mount.id='wrong'},
                {$args[0].actorsAfterInput.rider.id='wrong'}, {$args[0].PSObject.Properties.Remove('turnStatusBefore')}
            )}
            $mutations+=@({$args[0].afterInputFrame++}, {$args[0].kind='unknown-input-window'})
            foreach($mutation in $mutations) {
                $changed=New-ChargeEnvelope $mode
                $proof=if($location -ceq 'direct'){$changed.rows[$rowIndex].evidence.inputWindow}else{$changed.rows[$rowIndex].evidence.recovery.liveQueue.inputWindow}
                & $mutation $proof
                $rejected=$false;try{Assert-KmcChunk4ChargeEvidence $request $changed 'PASS'}catch{$rejected=$true}
                if(!$rejected){throw "Invalid native pause observation accepted: row=$rowIndex location=$location"};$passed++
            }
        }
    }
}
Write-Host "CHUNK 4 CHARGE PROTOCOL PASS=$passed FAIL=0"
