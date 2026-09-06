$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime\RuntimeHarness.Common.ps1')
$passed=0
foreach($name in @('ordinary-attack-controls-tb') + @(Get-KmcOrdinaryAttackControlCases | ForEach-Object {$_.id})) {
    if(@(Get-KmcPhase3dHorseRuntimeRows|Where-Object {$_ -ceq $name}).Count -ne 1){throw "Missing exact ordinary control row: $name"}
}
if(@(Get-KmcSaveBackedRuntimeScenarios|Where-Object {$_ -ceq 'ordinary-attack-controls-tb'}).Count -ne 1){throw 'Ordinary controls lack guarded fixture registration.'}
$passed++
$kmcGuardPath=Join-Path (Get-KmcLabRoot) 'codex-policy\Manage-KingmakerMountedCombatDeployment.ps1'
$kmcGuardCommand=Get-Command $kmcGuardPath
$allowed=@($kmcGuardCommand.Parameters['RuntimeScenario'].Attributes | Where-Object {$_ -is [Management.Automation.ValidateSetAttribute]} | ForEach-Object ValidValues)
if(@($allowed|Where-Object {$_ -ceq 'ordinary-attack-controls-tb'}).Count -ne 1 -or $allowed -contains 'ordinary-attack-unsafe') {throw 'Exact host scenario registration differs.'}
$passed++
function New-OrdinaryControlArtifact {
    # Synthetic envelopes test validation integrity; they are not gameplay evidence.
    $rows=@()
    foreach($case in @(Get-KmcOrdinaryAttackControlCases)) {
        $full=(!$case.primary -and $case.preparation -cin @('fresh','carried-move','mixed-range'))
        $mixed=$case.preparation -ceq 'mixed-range';$ownMove=$case.preparation -ceq 'rider-move';$carried=$case.preparation -ceq 'carried-move'
        $count=if(!$full){1}elseif($case.bab -eq 6){if($case.haste){3}else{2}}elseif($case.rapid){5}else{4}
        $completed=if($mixed){$count-1}else{$count};$commandId=$rows.Count+100
        $bonus=$(if($case.bab -eq 6){10}else{24})+$(if($case.rapid){-2}else{0})+$(if($case.haste){1}else{0})
        $rows+=@{name=$case.id;status='PASS';evidence=@{
            level='NATIVE INTEGRATION';parameters=@{mode='TB';mounted=$case.mounted;primary=$case.primary;rapidShot=$case.rapid;weapon='Longbow';bab=$case.bab;haste=$case.haste;preparation=$case.preparation}
            variation=@{rapidShot=$case.rapid;babBase=$case.bab;haste=$(if($case.haste){'03464790f40c3c24aa684b57155f3280'}else{$null});staggered=$case.preparation -ceq 'stale-staggered'}
            prediction=@{count=3;pure=$true;fullEnabled=$true;nativeEstimate=$count;nativeCursorCycles=1;fullAfterChoice=$false;before=@{rider=@{standard=0;move=0}};after=@{rider=@{standard=0;move=0}}};continuity=$true;repeatedRequests=3;intentStarts=1
            nativeCommand=@{id=$commandId;result=$(if($mixed){'Interrupt'}else{'Success'});finished=$true;executor='rider'};completed=$completed;planned=$count;nativeFull=$full;nativeSingle=$case.primary
            nativeRecovery=$null
            nativeRangeRejection=@{command=$commandId;caseId=$case.id;targetDead=$false;targetUnconscious=$false;targetUntargetable=$false;plannedWeapon='native-bite';rangeOriginDistance=6;pairApproachRadius=2;approachRadius=1.8}
            nativePlan=@(1..$count|ForEach-Object {$bite=$mixed -and $_ -eq $count;@{weapon=$(if($bite){'native-bite'}else{'native-longbow'});penalty=0;range=$(if($bite){1.8}else{16});ranged=(!$bite);natural=$bite}})
            rules=@{riderResolved=$completed;mountNonOpportunityAttackRules=0;pairForcedD20=0;attackRollEvents=@(1..$completed|ForEach-Object {@{
                actor='rider';attackBonus=$bonus;iterativePenalty=0;weapon='native-longbow';ranged=$true;statModifier=2;secondaryBonus=0
                nativeCalculation=@{innerResult=$bonus;result=$bonus;concealment=0;flanking=0;shootIntoCombat=0;bab=6;playerFaction=$true;trueDeath=$false}
            }});attackRuleEvents=@(1..$completed|ForEach-Object {@{weaponGuid='native-longbow'}})}
            spentStandard=@{sameNativeTurn=$true;secondAttackStarted=$false;rulesBefore=@{riderResolved=1};rulesAfter=@{riderResolved=1};before=@{rider=@{standard=6}};after=@{rider=@{standard=6;move=0}}}
            before=@{rider=@{id='rider';standard=0;move=$(if($ownMove){0.5}else{0})};mount=@{standard=0;move=$(if($carried){0.5}else{0})}}
            after=@{rider=@{standard=6;move=if($full -or $ownMove){3}else{0}};mount=@{standard=0;move=$(if($carried){0.5}else{0})}}
            movement=@{passed=$true;sameNativeTurn=$true;displacement=3;command=@{result='Success';executor=$(if($carried){'mount'}else{'rider'})}
                before=@{rider=@{id='rider';standard=0;move=0};mount=@{id='mount';standard=0;move=0}}
                after=@{rider=@{standard=0;move=$(if($carried){0}else{0.5})};mount=@{standard=0;move=$(if($carried){0.5}else{0})}}}
        }}
    }
    return (@{schemaVersion=1;status='PASS';rows=$rows;subscenarioPassCount=$rows.Count;subscenarioFailCount=0;errors=@();observations=@{
        phase3fActualConfiguration=@{enableUnifiedMountedTurn=$false;enablePairedCommandScheduler=$false;enableDiagnosticOverlay=$false;overlayPresent=$false}
        ordinaryAttackTrace=@{dropped=0;events=@('simulation-before','plan-after','start-after','delivery-after','ended','cost-after')|ForEach-Object {@{boundary=$_}}}
    }} | ConvertTo-Json -Depth 16 | ConvertFrom-Json)
}
$request=[pscustomobject]@{scenario='ordinary-attack-controls-tb'}
Assert-KmcOrdinaryAttackControlsEvidence $request (New-OrdinaryControlArtifact) PASS
$passed++
foreach($case in @(
    @{raw=11;shot=-4;flank=0;bab=11;death=$false;result=9},
    @{raw=11;shot=-4;flank=0;bab=11;death=$true;result=7},
    @{raw=8;shot=0;flank=2;bab=11;death=$false;result=10})) {
    Assert-KmcNativeAttackBonusEvidence ([pscustomobject]@{attackBonus=$case.result;iterativePenalty=0;nativeCalculation=[pscustomobject]@{
        innerResult=$case.raw;result=$case.result;concealment=0;flanking=$case.flank;shootIntoCombat=$case.shot;bab=$case.bab;playerFaction=$true;trueDeath=$case.death}})
    $passed++
}
function Set-NativeRecoveryControl($artifact) {
    $e=$artifact.rows[0].evidence;$e.nativeCommand.result='Interrupt'
    $e.nativeRecovery=[pscustomobject]@{boundary='native-recovery-interrupt';commandType='Kingmaker.UnitLogic.Commands.UnitAttack';command=$e.nativeCommand.id
        caseId='C01-B';result='Success';planned=$e.planned;completed=$e.completed;plannedWeapon=$null;detail='Kingmaker.UnitLogic.Commands.UnitAttack.OnTick'}
}
$nativeRecovery=New-OrdinaryControlArtifact;Set-NativeRecoveryControl $nativeRecovery
Assert-KmcOrdinaryAttackControlsEvidence $request $nativeRecovery PASS
$passed++
foreach($mutation in @('mode','configuration','owner-cost','count','primary','prediction','prediction-data','wrong-envelope','continuity','missing','trace','native-cost','forced-roll','unmatched','bonus','restricted','spent-standard','executor','native-plan','premature-recovery','external-stop','ranged-bite','dead-target','carried-tax','no-movement','haste-bonus','carried-plan','inner-result','native-floor','roll-weapon','roll-penalty','golem-haste')) {
    $a=New-OrdinaryControlArtifact
    switch($mutation) {
        mode {$a.rows[1].evidence.nativeFull=$false}
        configuration {$a.observations.phase3fActualConfiguration.enableUnifiedMountedTurn=$true}
        owner-cost {$a.rows[1].evidence.after.mount.standard=6}
        count {$a.rows[0].evidence.completed=1}
        primary {$a.rows[2].evidence.planned=2}
        prediction {$a.rows[1].evidence.prediction.pure=$false}
        prediction-data {$a.rows[1].evidence.prediction.after.rider.move=3}
        wrong-envelope {$a.schemaVersion=9}
        continuity {$a.rows[1].evidence.intentStarts=2}
        missing {$a.rows=@($a.rows|Select-Object -Skip 1);$a.subscenarioPassCount--}
        trace {$a.observations.ordinaryAttackTrace.dropped=1}
        native-cost {$a.rows[1].evidence.after.rider.move=0}
        forced-roll {$a.rows[1].evidence.rules.pairForcedD20=1}
        unmatched {$a.rows[1].evidence.nativePlan[0].penalty=5}
        bonus {$a.rows[1].evidence.rules.attackRollEvents[0].attackBonus=99}
        restricted {$a.rows[9].evidence.variation.staggered=$false}
        spent-standard {$a.rows[14].evidence.spentStandard.secondAttackStarted=$true}
        executor {$a.rows[1].evidence.nativeCommand.executor='mount'}
        native-plan {$a.rows[1].evidence.nativePlan=@()}
        premature-recovery {Set-NativeRecoveryControl $a;$a.rows[0].evidence.nativeRecovery.completed--}
        external-stop {Set-NativeRecoveryControl $a;$a.rows[0].evidence.nativeRecovery.detail='SelectionManager.Stop'}
        ranged-bite {$a.rows[18].evidence.rules.attackRuleEvents[0].weaponGuid='native-bite'}
        dead-target {$a.rows[18].evidence.nativeRangeRejection.targetDead=$true}
        carried-tax {$a.rows[16].evidence.movement.after.rider.move=1}
        no-movement {$a.rows[15].evidence.movement.displacement=0}
        haste-bonus {foreach($index in @(7,8)){foreach($roll in $a.rows[$index].evidence.rules.attackRollEvents){$roll.attackBonus--;$roll.nativeCalculation.innerResult--;$roll.nativeCalculation.result--}}}
        carried-plan {$a.rows[16].evidence.nativePlan[0].penalty=5}
        inner-result {$roll=$a.rows[1].evidence.rules.attackRollEvents[0];$roll.nativeCalculation.innerResult++;$roll.nativeCalculation.result++;$roll.attackBonus++}
        native-floor {$roll=$a.rows[1].evidence.rules.attackRollEvents[0];$roll.nativeCalculation.innerResult=11;$roll.nativeCalculation.bab=11;$roll.nativeCalculation.shootIntoCombat=-4;$roll.nativeCalculation.result=7;$roll.attackBonus=7}
        roll-weapon {$a.rows[1].evidence.rules.attackRollEvents[0].weapon='native-bite'}
        roll-penalty {$a.rows[1].evidence.rules.attackRollEvents[0].iterativePenalty=5}
        golem-haste {$a.rows[7].evidence.variation.haste='golem-haste'}
    }
    $rejected=$false
    try {Assert-KmcOrdinaryAttackControlsEvidence $request $a PASS} catch {$rejected=$true}
    if(-not $rejected){throw "Accepted corrupt ordinary controls: $mutation"}
    $passed++
}
Write-Host "ORDINARY CONTROL PROTOCOL PASS=$passed FAIL=0"
