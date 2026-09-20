$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')

# Parser fixtures exercise the actual evidence reader; no gameplay claim is made.
function New-GroundEnvelope {
    $rows=@()
    foreach($mounted in @($true,$false)){
        $id=if($mounted){'C4-GROUND-mounted-arrival'}else{'C4-GROUND-unmounted-arrival'}
        $relationship=if($mounted){'Mounted'}else{'Unmounted'}
        $state=@{relationship=$relationship;rider=@{id='rider';move=0;raw=@();queue=@()};mount=@{id='mount';move=0;raw=@();queue=@()}}
        $selected=if($mounted){'rider'}else{'mount'}
        $command=@{id=1;type='Kingmaker.UnitLogic.Commands.UnitMoveTo';executor='mount';started=$true;acted=$true;finished=$true;result='Success'}
        $footprint=@{corpulence=.9;probeRadius=.9;probes=@(1..8|ForEach-Object {@{residual=0}})}
        $e=@{level='NATIVE INTEGRATION';caseId=$id;mode='RT';mounted=$mounted;area='9d1278a2f599b2a4daab53abdfe88d2e';
            inputKind='native-ordinary-pointer-ground';before=$state;after=$state;selected=$selected;
            input=@{beforePrediction=$state;afterPrediction=$state;clicked=$true;selected=$selected};
            riderCanAct=$true;mountCanAct=$true;createdByPlayer=$true;forcedD20=0;command=$command;
            setup=@{command=$command;residual=.02};travel=2.5;endpointDistance=.02;approachRadius=.3;originMatchDistance=.01;originTolerance=.06;
            originFootprint=$footprint;destinationFootprint=$footprint;
            samples=@(1..2|ForEach-Object {@{frame=$_;gameSeconds=$_;movement=@{avoidanceDisabled=$false;corpulence=.9}}});
            nativeTrace=@(@{boundary='private-run-after';command=1;actor='mount';commandType=$command.type;caseId=$id})}
        $rows+=@{name=$id;status='PASS';evidence=$e}
    }
    return (@{schemaVersion=22;status='PASS';rows=$rows;errors=@();subscenarioPassCount=2;subscenarioFailCount=0;
        observations=@{phase3fActualConfiguration=@{enablePairedActivation=$true;enableUnifiedMountedTurn=$false;
            enablePairedCommandScheduler=$false;enableDiagnosticOverlay=$false;overlayPresent=$false};
            ordinaryAttackTrace=@{dropped=0;events=@()}}}|ConvertTo-Json -Depth 30|ConvertFrom-Json)
}
$groundPass=0;$groundRequest=@{scenario='chunk4-ground-arrival-rt'}
Assert-KmcChunk4CoreEvidence $groundRequest (New-GroundEnvelope) 'PASS';$groundPass++
$groundMutations=@(
    {param($e) $e.command.result='Interrupt'}, {param($e) $e.command.acted=$false},
    {param($e) $e.command.executor='rider'}, {param($e) $e.command.finished=$false},
    {param($e) $e.setup.command.result='Interrupt'}, {param($e) $e.setup.residual=.07},
    {param($e) $e.originMatchDistance=.07}, {param($e) $e.originTolerance=.3},
    {param($e) $e.approachRadius=.5}, {param($e) $e.endpointDistance=.31},
    {param($e) $e.travel=.1}, {param($e) $e.travel=[double]::NaN},
    {param($e) $e.riderCanAct=$false}, {param($e) $e.mountCanAct=$false},
    {param($e) $e.createdByPlayer=$false}, {param($e) $e.forcedD20=1},
    {param($e) $e.input.afterPrediction.rider.move=1}, {param($e) $e.input.clicked=$false},
    {param($e) $e.after.relationship='Mounting'}, {param($e) $e.after.mount.id='other'},
    {param($e) $e.after.mount.queue=@(1)}, {param($e) $e.originFootprint.probeRadius=.2},
    {param($e) $e.destinationFootprint.probes=@()}, {param($e) $e.samples=@()},
    {param($e) $e.samples[1].frame=1}, {param($e) $e.samples[1].movement.avoidanceDisabled=$true},
    {param($e) $e.samples[1].movement.corpulence=.1}, {param($e) $e.nativeTrace=@()},
    {param($e) $e.nativeTrace+=@($e.nativeTrace[0])}
)
foreach($row in @(0,1)){
    foreach($mutate in $groundMutations){
        $changed=New-GroundEnvelope;& $mutate $changed.rows[$row].evidence;$rejected=$false
        try{Assert-KmcChunk4CoreEvidence $groundRequest $changed 'PASS'}catch{$rejected=$true}
        if(!$rejected){throw "Invalid ground evidence accepted for row $row : $mutate"};$groundPass++
    }
}
# Keep failed native movement visible while accepting a separately completed control.
$failed=New-GroundEnvelope;$failed.status='FAIL';$failed.rows[0].status='FAIL'
$failed.rows[0].evidence.command.result='Interrupt';$failed.subscenarioPassCount=1;$failed.subscenarioFailCount=1
Assert-KmcChunk4CoreEvidence $groundRequest $failed 'FAIL';$groundPass++
$rejected=$false
try{Assert-KmcChunk4CoreEvidence $groundRequest $failed 'PASS'}catch{$rejected=$true}
if(!$rejected){throw 'Ground failure was promoted by the outer status.'};$groundPass++
Write-Host "COMPONENT ground arrival protocol TOTAL PASS=$groundPass FAIL=0"
