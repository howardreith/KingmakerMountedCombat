$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
$passed=0
$kmcProtocolRoot=Join-Path ([IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))) ('analysis-cache/chunk4-protocol/'+[Guid]::NewGuid().ToString('N'))
# Parser envelopes only; none of these objects is a native gameplay result.
function New-ExtendedLive {
    return @{relationship='Mounted';rider=@{id='rider';standard=6;move=0;swift=0;position=@(1,2,3);raw=@();queue=@()};
        mount=@{id='mount';standard=6;move=0;swift=0;position=@(1,1,3);raw=@();queue=@()}}
}
function New-ExtendedCommand($id,$actor,[bool]$finished=$true){return @{id=$id;executor=$actor;started=$true;acted=$finished;finished=$finished;result='Success'}}
function New-ExtendedTrace($id,$actor,$target){
    return @(@{command=$id;actor=$actor;target=$target;boundary='start-after'},@{command=$id;actor=$actor;boundary='cost-after'},
        @{command=$id;actor=$actor;boundary='delivery-after'},@{command=$id;actor=$actor;boundary='delivery-after'})
}
function New-ExtendedRules {
    return @{dropped=0;attacks=@(@{identity=100;actor='rider';target='target';resolved=$true},@{identity=101;actor='mount';target='target';resolved=$true});
        events=@(@{kind='weapon-resolved';attack=100;actor='rider';target='target';firstResolution=$true},
            @{kind='weapon-resolved';attack=101;actor='mount';target='target';firstResolution=$true})}
}
function New-ExtendedState {
    return @{live=(New-ExtendedLive);gameTicks=1;target=@{id='target';dead=$false};firstCommand=(New-ExtendedCommand 1 'rider' $false);
        firstClock=.1;firstIndex=0;nativeProjectiles=@(@{attack=100;target='target';hit=$false;destroyed=$false})}
}
function New-ExtendedSubscriptions {return @{executing=$false;entries=@(@{interface='native';type='owned';identity=5})}}
function New-ExtendedSessionState([bool]$exit,[bool]$dismounted) {
    $s=@{live=(New-ExtendedLive);playerCombat=(!$exit);riderCombat=(!$exit);mountCombat=(!$exit);tbActive=(!$exit);tbInitialized=(!$exit);
        targetLife=@{id='target';dead=$exit};records=0;privatePartner=$null;attachmentResidue=(!$dismounted);attachmentRestored=$dismounted;poseRestored=$dismounted}
    if($dismounted){$s.live.relationship='Unmounted'};return $s
}
function New-ExtendedEnvelope([string]$root) {
    $rows=@()
    foreach($id in @(Get-KmcChunk4ExtendedLeaves $root)){
        $e=@{level='NATIVE INTEGRATION';caseId=$id}
        if($id.StartsWith('C4-INSPECTION-')){
            $actor=if($id.EndsWith('-rider')){'rider'}else{'mount'}
            $e+=@{inputKind='native-character-hotkey-and-group-selection';actor=$actor;boundActor=$actor;groupActor=$actor;
                shown=$true;active=$true;screenIndex=1;displayedName='Native actor';before=(New-ExtendedLive);after=(New-ExtendedLive)}
        }elseif($id.StartsWith('C4-SESSION-')){
            $mode=if($root.EndsWith('-tb')){'TB'}else{'RT'};$cycle=[int]$id.Substring($id.Length-1)
            $e+=@{mode=$mode;cycle=$cycle;inputKind='native-mount-ordinary-pointer-combat-exit-dismount';mountedBeforeCombat=$true;
                beforeOrdinary=(New-ExtendedSessionState $false $false);beforeDeath=(New-ExtendedSessionState $false $false);
                selectedFirst=$(if($cycle -eq 2){'mount'}else{'rider'});riderRoutine=(New-ExtendedCommand 1 'rider');mountRoutine=(New-ExtendedCommand 2 'mount');
                riderPlan=2;riderCompleted=2;mountPlan=2;mountCompleted=2;beforeStop=@{live=(New-ExtendedLive)};afterStopInput=@{live=(New-ExtendedLive)};
                nativeTrace=@((New-ExtendedTrace 1 'rider' 'target')+(New-ExtendedTrace 2 'mount' 'target'));rulesAfter=(New-ExtendedRules);
                damageDispatches=1;nativeDamage=50;nativeLifeTransitions=1;nativeEncounterExit=(New-ExtendedSessionState $true $false);
                afterDismount=(New-ExtendedSessionState $true $true);settledAfter=(New-ExtendedSessionState $true $true);
                subscriptionsAfter=(New-ExtendedSubscriptions);recordsAfter=0}
        }else{
            $ranged=$root -ceq 'chunk4-interrupt-ranged-rt';$weapon=if($ranged){'ranged'}else{'melee'};$kind=$id.Substring(('C4-INTERRUPT-'+$weapon+'-').Length)
            $firstId=if($kind -ceq 'pause-resume'){1}else{2};$other=$kind.StartsWith('retarget-') -or $kind.StartsWith('target-death-')
            $e+=@{mode='RT';inputKind='native-pointer-prediction-and-click';ranged=$ranged;kind=$kind;target='target';otherTarget='other';
                before=(New-ExtendedState);after=(New-ExtendedState);beforeStimulus=(New-ExtendedState);afterStimulusInput=(New-ExtendedState);
                beforeStop=(New-ExtendedState);afterStopInput=(New-ExtendedState);rulesAfter=(New-ExtendedRules);
                legalCompletion=(New-ExtendedCommand $firstId 'rider');legalCompletedAttacks=2;legalNativePlan=2;legalNativeRangedTail=$false;
                nativeTrace=(New-ExtendedTrace $firstId 'rider' $(if($other){'other'}else{'target'}));damageDispatches=0;
                pauseDuration=.5;pausedBegin=(New-ExtendedState);pausedEnd=(New-ExtendedState);pausedAfterStop=(New-ExtendedState);
                targetMoved=3;targetMove=(New-ExtendedCommand 3 'target');
                targetPath=@{accepted=$true;error=$false;direct=3;length=3;endpointError=0;radius=16;
                    before=@{rider=@{id='rider'};mount=@{id='mount'};target=@{id='target'}};
                    after=@{rider=@{id='rider'};mount=@{id='mount'};target=@{id='target'}};
                    samples=@(@{blocked=$false;distance=12},@{blocked=$false;distance=13})};
                inFlightAtStimulus=@(@{identity=100;resolved=$false});targetLifeTransitions=0;nativeDamage=0}
            if($kind.StartsWith('target-death-')){$e.damageDispatches=1;$e.nativeDamage=50;$e.after.target.dead=$true;$e.targetLifeTransitions=1}
            if($kind.EndsWith('midroutine')){$e.beforeStimulus.firstIndex=1}
        }
        $rows+=@{name=$id;status='PASS';evidence=$e}
    }
    return (@{schemaVersion=23;status='PASS';rows=$rows;errors=@();subscenarioPassCount=$rows.Count;subscenarioFailCount=0;
        observations=@{phase3fActualConfiguration=@{enablePairedActivation=$true;enableUnifiedMountedTurn=$false;enablePairedCommandScheduler=$false;enableDiagnosticOverlay=$false;overlayPresent=$false};
            ordinaryAttackTrace=@{dropped=0;events=@()};chunk4InspectionClosed=$true;chunk4SessionSubscriptionsBefore=(New-ExtendedSubscriptions)}}|ConvertTo-Json -Depth 30|ConvertFrom-Json)
}
function Assert-ExtendedMutationRejected($original,$request,[scriptblock]$mutate,[int]$index) {
    $candidate=$original|ConvertTo-Json -Depth 30|ConvertFrom-Json
    & $mutate $candidate $index
    $rejected=$false
    try {Assert-KmcChunk4ExtendedEvidence $request $candidate 'PASS'}catch{$rejected=$true}
    if(!$rejected){throw "Extended parser accepted mutation $mutate at row $index for $($request.scenario)."}
    $script:passed++
}
foreach($root in @('chunk4-interrupt-melee-rt','chunk4-interrupt-ranged-rt','chunk4-inspection-rt','chunk4-session-rt','chunk4-session-tb')){
    $request=@{scenario=$root};$native=New-ExtendedEnvelope $root
    Assert-KmcChunk4ExtendedEvidence $request $native 'PASS';$passed++
    # Exercise the real outer artifact dispatcher as well as the leaf validator.
    $outer=New-ExtendedEnvelope $root
    $identity=@{evidenceKind='phase3d-horse-scenario-evidence';runId='parser-only';scenario=$root;branch='codex/mounted-combat-phase3f-playable-core';
        commit=('a'*40);productVersion='0.1.0-parser-only';dllSha256=('b'*64);dllMvid='00000000-0000-0000-0000-000000000001';createdAtUtc=[DateTime]::UtcNow.ToString('o')}
    foreach($key in $identity.Keys){$outer|Add-Member -NotePropertyName $key -NotePropertyValue $identity[$key]}
    $outerRequest=@{};foreach($key in $identity.Keys){$outerRequest[$key]=$identity[$key]}
    $outerRequest.evidenceRoot=Join-Path $kmcProtocolRoot $root
    $null=New-Item -ItemType Directory -Path $outerRequest.evidenceRoot -Force
    $outer|ConvertTo-Json -Depth 30|Set-Content -LiteralPath (Join-Path $outerRequest.evidenceRoot 'phase3d-horse-scenario-evidence.json') -Encoding UTF8
    Assert-KmcPhase3dHorseScenarioEvidence -Request $outerRequest -Manifest @{artifacts=@(@{relativePath='phase3d-horse-scenario-evidence.json';kind='phase3d-horse-scenario-evidence'})} -Status 'PASS'
    $passed++
    foreach($mutation in @(
        {param($e,$i) $e.schemaVersion=22}, {param($e,$i) $e.rows=@()},
        {param($e,$i) $e.rows=@($e.rows)+@($e.rows[0]);$e.subscenarioPassCount++},
        {param($e,$i) $e.subscenarioPassCount++}, {param($e,$i) $e.rows[0].evidence.level='COMPONENT'},
        {param($e,$i) $e.rows[0].evidence.caseId='wrong'},
        {param($e,$i) $e.observations.phase3fActualConfiguration.enablePairedActivation=$false},
        {param($e,$i) $e.observations.phase3fActualConfiguration.enablePairedCommandScheduler=$true},
        {param($e,$i) $e.observations.ordinaryAttackTrace.dropped=1}
    )){Assert-ExtendedMutationRejected $native $request $mutation 0}
    for($i=0;$i -lt $native.rows.Count;$i++){
        $id=$native.rows[$i].name
        $mutations=@()
        if($id.StartsWith('C4-INSPECTION-')){
            $mutations+=@({param($e,$i) $e.rows[$i].evidence.boundActor='wrong'}, {param($e,$i) $e.rows[$i].evidence.groupActor='wrong'},
                {param($e,$i) $e.rows[$i].evidence.shown=$false}, {param($e,$i) $e.rows[$i].evidence.screenIndex=0},
                {param($e,$i) $e.rows[$i].evidence.after.rider.move=1}, {param($e,$i) $e.observations.chunk4InspectionClosed=$false})
        }elseif($id.StartsWith('C4-SESSION-')){
            $mutations+=@({param($e,$i) $e.rows[$i].evidence.mountedBeforeCombat=$false}, {param($e,$i) $e.rows[$i].evidence.selectedFirst='wrong'},
                {param($e,$i) $e.rows[$i].evidence.riderCompleted=1}, {param($e,$i) $e.rows[$i].evidence.mountCompleted=0},
                {param($e,$i) $e.rows[$i].evidence.nativeDamage=0}, {param($e,$i) $e.rows[$i].evidence.nativeEncounterExit.playerCombat=$true},
                {param($e,$i) $e.rows[$i].evidence.afterDismount.privatePartner='stale'}, {param($e,$i) $e.rows[$i].evidence.settledAfter.records=1},
                {param($e,$i) $e.rows[$i].evidence.subscriptionsAfter.entries[0].identity=6}, {param($e,$i) $e.rows[$i].evidence.settledAfter.attachmentResidue=$true},
                {param($e,$i) $e.rows[$i].evidence.afterStopInput.live.mount.standard=0})
        }else{
            $mutations+=@({param($e,$i) $e.rows[$i].evidence.legalCompletedAttacks=0}, {param($e,$i) $e.rows[$i].evidence.legalCompletion.executor='mount'},
                {param($e,$i) $e.rows[$i].evidence.afterStopInput.live.rider.standard=0},
                {param($e,$i) $e.rows[$i].evidence.nativeTrace=@($e.rows[$i].evidence.nativeTrace|Where-Object {$_.boundary -cne 'cost-after'})})
            if($id.Contains('-pause-')){$mutations+=@({param($e,$i) $e.rows[$i].evidence.pauseDuration=0},{param($e,$i) $e.rows[$i].evidence.pausedEnd.firstClock=.2})}
            if($id.EndsWith('moving-target')){$mutations+=@({param($e,$i) $e.rows[$i].evidence.targetMoved=0})}
            if($id -ceq 'C4-INTERRUPT-ranged-moving-target'){$mutations+=@(
                {param($e,$i) $e.rows[$i].evidence.targetPath.accepted=$false},
                {param($e,$i) $e.rows[$i].evidence.targetPath.length=10},
                {param($e,$i) $e.rows[$i].evidence.targetPath.samples[0].blocked=$true},
                {param($e,$i) $e.rows[$i].evidence.targetPath.samples[0].distance=17},
                {param($e,$i) $e.rows[$i].evidence.targetPath.after.target.id='changed'},
                {param($e,$i) $e.rows[$i].evidence.targetPath.samples=@()})}
            if($id.Contains('target-death')){$mutations+=@({param($e,$i) $e.rows[$i].evidence.nativeDamage=0},{param($e,$i) $e.rows[$i].evidence.after.target.dead=$false})}
            if($id.EndsWith('inflight')){$mutations+=@({param($e,$i) $e.rows[$i].evidence.inFlightAtStimulus=@()}, {param($e,$i) $e.rows[$i].evidence.beforeStimulus.nativeProjectiles[0].destroyed=$true})}
            if($id.Contains('retarget') -or $id.Contains('target-death')){$mutations+=@({param($e,$i) $e.rows[$i].evidence.otherTarget='target'})}
        }
        if(!$id.StartsWith('C4-INSPECTION-')){
            $mutations+=@({param($e,$i) $e.rows[$i].evidence.rulesAfter.attacks[0].resolved=$false},
                {param($e,$i) $e.rows[$i].evidence.rulesAfter.events+=@($e.rows[$i].evidence.rulesAfter.events[0])},
                {param($e,$i) $e.rows[$i].evidence.rulesAfter.events[0].target='wrong'})
        }
        foreach($mutation in $mutations){Assert-ExtendedMutationRejected $native $request $mutation $i}
    }
}
Write-Host "CHUNK 4 EXTENDED PROTOCOL PASS=$passed FAIL=0"
