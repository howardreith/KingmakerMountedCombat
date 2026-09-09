$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
$passed=0

# Synthetic envelopes exercise the evidence parser, never native gameplay.
function New-PlayState {
    param([double]$RiderStandard=0,[double]$RiderMove=0,[double]$MountStandard=0,[double]$MountMove=0)
    return @{relationship='Mounted';rider=@{id='rider';standard=$RiderStandard;move=$RiderMove;raw=@();queue=@()};
        mount=@{id='mount';standard=$MountStandard;move=$MountMove;raw=@();queue=@()}}
}
function New-PlayEnvelope {
    param([string]$Root)
    $rows=@()
    if($Root -ne 'chunk4-sustained-tb') {
        $weapon=if($Root -eq 'chunk4-sustained-ranged-rt'){'ranged'}else{'melee'}
        foreach($condition in @('adjacent-held','adjacent-repeat','approach-held','approach-repeat')) {
            $id='C4-SUSTAINED-'+$weapon+'-'+$condition
            $repeat=$condition.EndsWith('-repeat');$approach=$condition.StartsWith('approach-')
            $samples=@(1..16|ForEach-Object {@{time=($_*0.25);riderStandard=2;riderMove=0;mountStandard=3;mountMove=0;targetTemporaryHp=(4096-$_)}})
            $routines=@();$events=@();$commandId=1
            foreach($actor in @('rider','mount')) {
                foreach($index in 1..3) {
                    $routines+=@{actor=$actor;command=@{id=$commandId;executor=$actor;started=$true;finished=$true;acted=$true;result='Success'};
                        planned=2;completed=2;completedObserved=$true}
                    $events+=@{command=$commandId;boundary='start-after';actor=$actor;standard=0}
                    $events+=@{command=$commandId;boundary='cost-before';actor=$actor;standard=0;frame=$index}
                    $events+=@{command=$commandId;boundary='cost-after';actor=$actor;standard=6;frame=$index}
                    $commandId++
                }
            }
            $clicks=@()
            if($repeat){$clicks=@(1..16|ForEach-Object {@{pure=$true;before=(New-PlayState 3 0 2 0);after=(New-PlayState 3 0 2 0);clocksBefore=@(1.5);clocksAfter=@(1.5)}})}
            $rows+=@{name=$id;status='PASS';evidence=@{level='NATIVE INTEGRATION';caseId=$id;mode='RT';repeat=$repeat;approach=$approach;ranged=($weapon -eq 'ranged');
                inputKind='scripted-native-pointer-prediction-and-click';nativeEnoughCloseBefore=(!$approach);intentStarts=1;duplicateDispatches=0;
                rules=@{pairForcedD20=0;riderNonOpportunityAttackRules=6;riderResolved=6;mountNonOpportunityAttackRules=6;mountResolved=6};
                samples=$samples;riderStartPeriods=@(6.02,6.03);before=(New-PlayState);afterStop=(New-PlayState);
                target='target';targetProvisioning=@{targetId='target';durabilityLeaseAmount=4096;temporaryHitPointsAfterProvisioning=4096};
                clicks=$clicks;routines=$routines;nativeTrace=$events;completeRiderRoutines=3;nativeFullRiderRoutines=3;nativeRangedTailRiderRoutines=0;beforeStop=(New-PlayState 0.1 0 0.2 0);
                afterStopInput=(New-PlayState 0.1 0 0.2 0);mountDistance=3;weapon=$weapon;maximumNativeFrameStep=0.02;
                cadenceComparison=@{observedFrameTolerance=0.06}}}
        }
    }else{
        $names=@('rider-first','mount-first','rider-exhausted','mount-exhausted','early-end','after-early-end')
        foreach($name in $names) {
            $id='C4-SUSTAINED-TB-'+$name;$operations=@();$state=New-PlayState
            $early=$name -eq 'early-end'
            if($name -notin @('early-end','after-early-end')) {
                $mountFirst=$name -in @('mount-first','mount-exhausted')
                foreach($index in 0..1) {
                    $isMount=$mountFirst -eq ($index -eq 0);$actor=if($isMount){'mount'}else{'rider'}
                    $full=$index -eq 0 -and $name.EndsWith('-exhausted');$kind=if($full){'ordinary-Full'}else{'explicit-Primary'}
                    $count=if($full){3}else{1};$after=New-PlayState $state.rider.standard $state.rider.move $state.mount.standard $state.mount.move
                    $after[$actor].standard=6;$after[$actor].move=if($full){3}else{0}
                    $operations+=@{kind=$kind;actor=$actor;beforeIdentity=$id;before=$state;after=$after;
                        command=@{executor=$actor;started=$true;acted=$true;finished=$true;result='Success'};
                        nativeFull=$full;nativePrimary=(!$full);completed=$count;planned=$count;resolved=$count;rulesAfter=@{pairForcedD20=0}}
                    $state=$after
                }
                if($name -eq 'rider-exhausted'){
                    $operations+=@{kind='partner-move-after-rider-exhaustion';command=@{executor='mount';finished=$true;result='Success'};
                        before=$state;after=(New-PlayState 6 3 6 1)}
                }
            }
            $rows+=@{name=$id;status='PASS';evidence=@{level='NATIVE INTEGRATION';caseId=$id;mode='TB';identity=$id;
                before=(New-PlayState);principal='rider';partner='mount';riderPrepared=$true;mountPrepared=$true;
                operations=$operations;earlyEnd=$early;nextUnrelatedActor='unrelated';beforeEnd=(New-PlayState)}}
        }
    }
    return (@{schemaVersion=21;status='PASS';rows=$rows;subscenarioPassCount=$rows.Count;subscenarioFailCount=0;errors=@();
        observations=@{phase3fActualConfiguration=@{enablePairedActivation=$true;enableUnifiedMountedTurn=$false;enablePairedCommandScheduler=$false;enableDiagnosticOverlay=$false;overlayPresent=$false};
            ordinaryTrace=@{dropped=0;events=@()}}}|ConvertTo-Json -Depth 40|ConvertFrom-Json)
}

foreach($root in @('chunk4-sustained-melee-rt','chunk4-sustained-ranged-rt','chunk4-sustained-tb')) {
    $request=@{scenario=$root}
    if(@(Get-KmcSaveBackedRuntimeScenarios|Where-Object {$_ -ceq $root}).Count -ne 1){throw 'Sustained root registration missing or duplicate.'}
    Assert-KmcChunk4PlayEvidence $request (New-PlayEnvelope $root) 'PASS';$passed++
    $mutations=@(
        {$args[0].schemaVersion=20},
        {$args[0].observations.phase3fActualConfiguration.enablePairedActivation=$false},
        {$args[0].observations.phase3fActualConfiguration.enablePairedCommandScheduler=$true},
        {$args[0].observations.phase3fActualConfiguration.overlayPresent=$true},
        {$args[0].rows[0].evidence.level='COMPONENT'},
        {$args[0].rows[0].evidence.before.relationship='Unmounted'},
        {$args[0].rows[0].evidence.before.rider.standard=1},
        {$args[0].rows[0].name='unregistered'},
        {$args[0].rows=$args[0].rows[1..($args[0].rows.Count-1)];$args[0].subscenarioPassCount--},
        {$args[0].observations.ordinaryTrace.dropped=1},
        {$args[0].observations.ordinaryTrace.events=@([pscustomobject]@{observationError='missing native event'})}
    )
    if($root -ne 'chunk4-sustained-tb') {
        $mutations+=@(
            {$args[0].rows[0].evidence.rules.pairForcedD20=1},
            {$args[0].rows[0].evidence.rules.riderResolved=5},
            {$args[0].rows[0].evidence.rules.mountNonOpportunityAttackRules=7},
            {$args[0].rows[0].evidence.routines[0].completed=1},
            {$args[0].rows[0].evidence.routines[0].command.result='Interrupt'},
            {$args[0].rows[0].evidence.routines[1].command.id=1},
            {$args[0].rows[0].evidence.nativeTrace[0].standard=2},
            {$args[0].rows[0].evidence.nativeTrace[2].standard=0},
            {$args[0].rows[0].evidence.nativeTrace[2].frame=99},
            {$args[0].rows[0].evidence.samples[2].riderMove=0.1},
            {$args[0].rows[0].evidence.samples[2].targetTemporaryHp=4096},
            {$args[0].rows[0].evidence.afterStop.rider.raw=@('stuck')},
            {$args[0].rows[0].evidence.afterStopInput.mount.standard=0},
            {$args[0].rows[1].evidence.clicks[0].pure=$false},
            {$args[0].rows[1].evidence.clicks[0].after.rider.standard=0},
            {$args[0].rows[1].evidence.clicks[0].clocksAfter=@(0)},
            {$args[0].rows[1].evidence.riderStartPeriods=@(5,5)},
            {$args[0].rows[1].evidence.maximumNativeFrameStep=1},
            {$args[0].rows[2].evidence.nativeEnoughCloseBefore=$true},
            {$args[0].rows[2].evidence.mountDistance=0}
        )
    }else{
        $mutations+=@(
            {$args[0].rows[0].evidence.mountPrepared=$false},
            {$args[0].rows[0].evidence.partner='rider'},
            {$args[0].rows[0].evidence.operations[0].command.executor='mount'},
            {$args[0].rows[0].evidence.operations[0].nativePrimary=$false},
            {$args[0].rows[0].evidence.operations[0].completed=2},
            {$args[0].rows[0].evidence.operations[0].after.mount.standard=6},
            {$args[0].rows[0].evidence.operations[1].before.mount.standard=6},
            {$args[0].rows[2].evidence.operations[0].nativeFull=$false},
            {$args[0].rows[2].evidence.operations[2].after.rider.move=0},
            {$args[0].rows[2].evidence.operations[2].after.mount.move=4},
            {$args[0].rows[4].evidence.beforeEnd.rider.standard=6},
            {$args[0].rows[4].evidence.nextUnrelatedActor='mount'},
            {$args[0].rows[5].evidence.identity=$args[0].rows[4].evidence.identity},
            {$args[0].rows[5].evidence.operations=@('unexpected')}
        )
    }
    $index=0
    foreach($mutation in $mutations) {
        $changed=New-PlayEnvelope $root;& $mutation $changed;$rejected=$false
        try{Assert-KmcChunk4PlayEvidence $request $changed 'PASS'}catch{$rejected=$true}
        if(!$rejected){throw "Invalid sustained evidence accepted: $root mutation $index."}
        $passed++;$index++
    }
}
# Native ranged plans keep their ineligible melee tail; only the observed native
# range terminal can complete the ranged portion for repetition accounting.
function New-RangedTailEnvelope {
    $e=New-PlayEnvelope 'chunk4-sustained-ranged-rt'
    foreach($row in @($e.rows|Where-Object {$_.name.Contains('-approach-')})){
        $row.evidence.nativeFullRiderRoutines=0;$row.evidence.nativeRangedTailRiderRoutines=3
        foreach($routine in @($row.evidence.routines|Where-Object {$_.actor -ceq 'rider'})){
            $routine.planned=3;$routine.command.result='Interrupt'
            $range=@{boundary='target-invalid';command=$routine.command.id;completed=2;targetDead=$false;targetUnconscious=$false;targetInState=$true;
                rangeOriginDistance=10;pairApproachRadius=2;mountCorpulence=1;targetCorpulence=1;nativeActorLoS=$true;
                plan=@(@{ranged=$true;weaponRange=15},@{ranged=$true;weaponRange=15},@{ranged=$false;weaponRange=.6})}
            $routine|Add-Member -NotePropertyName nativeRangedTailTermination -NotePropertyValue $true
            $routine|Add-Member -NotePropertyName nativeRangeRejection -NotePropertyValue ($range|ConvertTo-Json -Depth 8|ConvertFrom-Json)
            $row.evidence.nativeTrace+=@($routine.nativeRangeRejection)
        }
    }
    return $e
}
$tailRequest=@{scenario='chunk4-sustained-ranged-rt'}
Assert-KmcChunk4PlayEvidence $tailRequest (New-RangedTailEnvelope) 'PASS';$passed++
foreach($mutate in @(
    {param($e) $e.rows[2].evidence.routines[0].nativeRangeRejection.targetDead=$true},
    {param($e) $e.rows[2].evidence.routines[0].nativeRangeRejection.targetInState=$false},
    {param($e) $e.rows[2].evidence.routines[0].nativeRangeRejection.rangeOriginDistance=1},
    {param($e) $e.rows[2].evidence.routines[0].nativeRangeRejection.plan[2].ranged=$true},
    {param($e) $e.rows[2].evidence.routines[0].nativeRangeRejection.plan[0].ranged=$false},
    {param($e) $e.rows[2].evidence.routines[0].nativeRangeRejection.command=-1},
    {param($e) $e.rows[2].evidence.nativeFullRiderRoutines=3},
    {param($e) $e.rows[2].evidence.routines[0].nativeRangeRejection.nativeActorLoS=$false},
    {param($e) $e.rows[2].evidence.routines[0].nativeRangeRejection.plan[2].weaponRange=15},
    {param($e) $e.rows[2].evidence.routines[0].nativeRangeRejection.plan[0].weaponRange=1},
    {param($e) $e.rows[2].evidence.nativeTrace=@($e.rows[2].evidence.nativeTrace|Where-Object {$_.boundary -cne 'target-invalid'})},
    {param($e) $e.rows[2].evidence.routines[0].command.result='Fail'},
    {param($e) $e.rows[2].evidence.routines[0].nativeRangedTailTermination=$false}
)){
    $e=New-RangedTailEnvelope;& $mutate $e;$rejected=$false
    try{Assert-KmcChunk4PlayEvidence $tailRequest $e 'PASS'}catch{$rejected=$true}
    if(!$rejected){throw 'Invalid ranged native-tail evidence accepted.'};$passed++
}
Write-Host "CHUNK 4 PLAY PROTOCOL PASS=$passed FAIL=0"
