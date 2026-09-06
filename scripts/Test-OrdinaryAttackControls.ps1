$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime\RuntimeHarness.Common.ps1')
$passed=0
foreach($name in @('ordinary-attack-controls-tb','C01-B','C01-C','C01-D')) {
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
    $rows=@()
    foreach($id in @('C01-B','C01-C','C01-D')) {
        $primary=$id -ceq 'C01-D';$mounted=$id -cne 'C01-B';$count=if($primary){1}else{4}
        $rows+=@{name=$id;status='PASS';evidence=@{
            level='NATIVE INTEGRATION';parameters=@{mode='TB';mounted=$mounted;primary=$primary;rapidShot=$true;weapon='Longbow'}
            prediction=@{count=3;pure=$true;fullEnabled=$true;before=@{rider=@{standard=0;move=0}};after=@{rider=@{standard=0;move=0}}};continuity=$true;repeatedRequests=3;intentStarts=1
            nativeCommand=@{result='Success';finished=$true};completed=$count;planned=$count;nativeFull=(!$primary);nativeSingle=$primary
            rules=@{riderResolved=$count;mountNonOpportunityAttackRules=0;pairForcedD20=0}
            before=@{rider=@{standard=0;move=0};mount=@{standard=0;move=0}}
            after=@{rider=@{standard=6;move=if($primary){0}else{3}};mount=@{standard=0;move=0}}
        }}
    }
    return (@{schemaVersion=1;status='PASS';rows=$rows;subscenarioPassCount=3;subscenarioFailCount=0;errors=@();observations=@{
        phase3fActualConfiguration=@{enableUnifiedMountedTurn=$false;enablePairedCommandScheduler=$false;enableDiagnosticOverlay=$false;overlayPresent=$false}
        ordinaryAttackTrace=@{dropped=0;events=@('simulation-before','plan-after','start-after','delivery-after','ended','cost-after')|ForEach-Object {@{boundary=$_}}}
    }} | ConvertTo-Json -Depth 16 | ConvertFrom-Json)
}
$request=[pscustomobject]@{scenario='ordinary-attack-controls-tb'}
Assert-KmcOrdinaryAttackControlsEvidence $request (New-OrdinaryControlArtifact) PASS
$passed++
foreach($mutation in @('mode','configuration','owner-cost','count','primary','prediction','prediction-data','wrong-envelope','continuity','missing','trace','native-cost','forced-roll','unmatched')) {
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
        unmatched {$a.rows[1].evidence.planned=5;$a.rows[1].evidence.completed=5;$a.rows[1].evidence.rules.riderResolved=5}
    }
    $rejected=$false
    try {Assert-KmcOrdinaryAttackControlsEvidence $request $a PASS} catch {$rejected=$true}
    if(-not $rejected){throw "Accepted corrupt ordinary controls: $mutation"}
    $passed++
}
Write-Host "ORDINARY CONTROL PROTOCOL PASS=$passed FAIL=0"
