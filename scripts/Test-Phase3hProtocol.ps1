$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime\RuntimeHarness.Common.ps1')
$passed=0
function New-ControlsEvidence {
    param([bool]$TurnBased)
    $rows=@()
    foreach($name in @('3h-rider-longbow-ordinary','3h-rider-longbow-primary','3h-rider-melee-ordinary','3h-rider-melee-primary','3h-horse-bite-ordinary','3h-horse-bite-primary')) {
        $mount=$name.StartsWith('3h-horse-');$actor=if($mount){'mount'}else{'rider'}
        $rows+=@{name=$name;status='PASS';evidence=@{
            inputKind='scripted-native-handler-integration';intentStarts=1;turnActor=$actor;mountDisplacement=if($name -cin @('3h-rider-melee-ordinary','3h-horse-bite-ordinary')){1}else{0}
            actorBefore=@{standard=0};otherBefore=@{standard=0}
            ledger=@{relationshipState='Mounted';rider=@{standard=if($mount){0}else{6};move=3};mount=@{standard=if($mount){6}else{0};move=0}}
            rules=@{riderResolved=if($mount){0}elseif($name.EndsWith('-ordinary')){2}else{1};mountResolved=if(-not $mount){0}elseif($name.EndsWith('-ordinary')){2}else{1};pairForcedD20=0}
            lastOutcome=@{result='Success';actorId=$actor;commandOwnerId=$actor;resourceOwnerId=$actor;childAttackStartCount=1;singleAttackMode=$name.EndsWith('-primary');nativeFullAttack=$name.EndsWith('-ordinary');nativePlannedAttackCount=if($name.EndsWith('-ordinary')){2}else{1};nativeCompletedAttackCount=if($name.EndsWith('-ordinary')){2}else{1}}
        }}
    }
    if(-not $TurnBased){foreach($name in @('3h-paused-dismount','3h-paused-mount-stop','3h-paused-mount-execute')){
        $stop=$name -ceq '3h-paused-mount-stop'
        $rows+=@{name=$name;status='PASS';evidence=@{inputKind='scripted-native-handler-integration';queuedBeforeExecution=$true;finished=$true;acted=(!$stop);dispatchDelta=if($stop){0}else{1};result=if($stop){'Interrupt'}else{'Success'}}}
    }}
    return (@{schemaVersion=9;status='PASS';subscenarioPassCount=$rows.Count;subscenarioFailCount=0;errors=@();rows=$rows;observations=@{
        riderId='rider';horseId='mount';phase3fActualConfiguration=@{enableUnifiedMountedTurn=$false;enablePairedCommandScheduler=$false;enableDiagnosticOverlay=$false;overlayPresent=$false}
    }}|ConvertTo-Json -Depth 15|ConvertFrom-Json)
}
foreach($mode in @('rt','tb')){
    $request=[pscustomobject]@{scenario="phase3h-combat-loop-$mode"}
    $artifact=New-ControlsEvidence ($mode -eq 'tb')
    Assert-KmcPhase3hLoopEvidence $request $artifact PASS
    $passed++
    foreach($mutation in @('effect','owner','configuration','generation','missing','truncated','single','fullcost','pending-effect')){
        $artifact=New-ControlsEvidence ($mode -eq 'tb')
        switch($mutation){
            effect {$artifact.rows[0].evidence.rules.riderResolved=0}
            owner {$artifact.rows[0].evidence.lastOutcome.resourceOwnerId='mount'}
            configuration {$artifact.observations.phase3fActualConfiguration.enableUnifiedMountedTurn=$true}
            generation {$artifact.rows[0].evidence.intentStarts=2}
            missing {$artifact.rows=@($artifact.rows|Select-Object -Skip 1);$artifact.subscenarioPassCount--}
            truncated {$artifact.rows[0].evidence.lastOutcome.nativeCompletedAttackCount=1}
            single {$artifact.rows[1].evidence.lastOutcome.nativePlannedAttackCount=2}
            fullcost {if($mode -eq 'tb'){$artifact.rows[0].evidence.ledger.rider.move=0}else{$artifact.rows[0].evidence.lastOutcome.nativeFullAttack=$false}}
            pending-effect {$artifact.rows[4].evidence.rules.mountResolved=1}
        }
        $rejected=$false
        try{Assert-KmcPhase3hLoopEvidence $request $artifact PASS}catch{$rejected=$true}
        if(-not $rejected){throw "Accepted corrupt $mode $mutation evidence."}
        $passed++
    }
    if($mode -eq 'tb'){
        $artifact=New-ControlsEvidence $true
        $artifact.rows[0].evidence.mountDisplacement=0.2
        $rejected=$false
        try{Assert-KmcPhase3hLoopEvidence $request $artifact PASS}catch{$rejected=$true}
        if(-not $rejected){throw 'Accepted moving attack as stationary TB evidence.'}
        $passed++
        $artifact=New-ControlsEvidence $true
        $artifact.rows[0].evidence.actorBefore=[pscustomobject]@{'$id'='1'}
        $rejected=$false
        try{Assert-KmcPhase3hLoopEvidence $request $artifact PASS}catch{$rejected=$true}
        if(-not $rejected){throw 'Accepted a missing native before ledger.'}
        $passed++
    }
}
Write-Host "TOTAL Phase3H protocol PASS=$passed FAIL=0"
