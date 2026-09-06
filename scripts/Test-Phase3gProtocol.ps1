$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime\RuntimeHarness.Common.ps1')
$passed=0
function New-ControlsEvidence {
    param([bool]$TurnBased)
    $rows=@()
    foreach($name in @('3g-rider-longbow-ordinary','3g-rider-longbow-primary','3g-rider-melee-ordinary','3g-rider-melee-primary','3g-horse-bite-ordinary','3g-horse-bite-primary')) {
        $mount=$name.StartsWith('3g-horse-');$actor=if($mount){'mount'}else{'rider'}
        $rows+=@{name=$name;status='PASS';evidence=@{
            inputKind='scripted-native-handler-integration';intentStarts=1;turnActor=$actor
            actorBefore=@{standard=0};otherBefore=@{standard=0}
            ledger=@{relationshipState='Mounted';rider=@{standard=if($mount){0}else{6}};mount=@{standard=if($mount){6}else{0}}}
            rules=@{riderResolved=if($mount){0}else{2};mountResolved=if($mount){1}else{0};pairForcedD20=0}
            lastOutcome=@{result='Success';actorId=$actor;commandOwnerId=$actor;resourceOwnerId=$actor;childAttackStartCount=1}
        }}
    }
    return (@{schemaVersion=8;status='PASS';subscenarioPassCount=6;subscenarioFailCount=0;errors=@();rows=$rows;observations=@{
        riderId='rider';horseId='mount';phase3fActualConfiguration=@{enableUnifiedMountedTurn=$false;enablePairedCommandScheduler=$false;enableDiagnosticOverlay=$false;overlayPresent=$false}
    }}|ConvertTo-Json -Depth 15|ConvertFrom-Json)
}
foreach($mode in @('rt','tb')){
    $request=[pscustomobject]@{scenario="phase3g-native-controls-$mode"}
    $artifact=New-ControlsEvidence ($mode -eq 'tb')
    Assert-KmcPhase3gControlsEvidence $request $artifact PASS
    $passed++
    foreach($mutation in @('effect','owner','configuration','generation','missing')){
        $artifact=New-ControlsEvidence ($mode -eq 'tb')
        switch($mutation){
            effect {$artifact.rows[0].evidence.rules.riderResolved=0}
            owner {$artifact.rows[0].evidence.lastOutcome.resourceOwnerId='mount'}
            configuration {$artifact.observations.phase3fActualConfiguration.enableUnifiedMountedTurn=$true}
            generation {$artifact.rows[0].evidence.intentStarts=2}
            missing {$artifact.rows=@($artifact.rows|Select-Object -Skip 1);$artifact.subscenarioPassCount=5}
        }
        $rejected=$false
        try{Assert-KmcPhase3gControlsEvidence $request $artifact PASS}catch{$rejected=$true}
        if(-not $rejected){throw "Accepted corrupt $mode $mutation evidence."}
        $passed++
    }
}
Write-Host "TOTAL Phase3G protocol PASS=$passed FAIL=0"
