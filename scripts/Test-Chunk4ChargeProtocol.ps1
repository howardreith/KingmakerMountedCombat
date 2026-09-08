$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
$passed=0
function New-ChargeEnvelope {
    param([string]$Mode)
    # Synthetic envelopes exercise evidence validation only, never gameplay.
    $rows=@()
    foreach($mounted in @($true,$false)) {
        $id=if($mounted){'C4-CHARGE-mounted-rider'}else{'C4-CHARGE-unmounted-rider'}
        $rows+=@{name=$id;status='PASS';evidence=@{
            level='NATIVE INTEGRATION';inputKind='scripted-native-handler-integration';mode=$Mode;mounted=$mounted;hoverPure=$true
            identity=@(@{logic='Kingmaker.UnitLogic.Abilities.Components.AbilityCustomCharge';assemblyMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';blueprint=('a'*32)})
            samples=@(@{nativeSeconds=1});rules=@{pairForcedD20=0;riderAttackRules=$(if($mounted){0}else{1});riderResolved=$(if($mounted){0}else{1})}
            safeRejected=$mounted;observedCharging=(!$mounted);riderDistance=$(if($mounted){0}else{5});mountDistance=0
            maximumRiderStandard=$(if($mounted){0}else{6});maximumRiderMove=0;nativeChargeCompleted=(!$mounted)
        }}
    }
    return (@{schemaVersion=18;status='PASS';subscenarioPassCount=2;subscenarioFailCount=0;errors=@();rows=$rows
        observations=@{phase3fActualConfiguration=@{enableUnifiedMountedTurn=$false;enablePairedCommandScheduler=$false;enablePairedActivation=$true;enableDiagnosticOverlay=$false;overlayPresent=$false}}} | ConvertTo-Json -Depth 20 | ConvertFrom-Json)
}
foreach($mode in @('RT','TB')) {
    $request=@{scenario='chunk4-charge-safety-'+$mode.ToLowerInvariant()}
    if(@(Get-KmcSaveBackedRuntimeScenarios | Where-Object {$_ -ceq $request.scenario}).Count -ne 1) {throw 'Charge scenario registration missing or duplicate.'}
    $artifact=New-ChargeEnvelope $mode
    Assert-KmcChunk4ChargeEvidence $request $artifact 'PASS';$passed++
    foreach($mutation in @(
        {$args[0].observations.phase3fActualConfiguration.enablePairedActivation=$false},
        {$args[0].rows[0].evidence.maximumRiderMove=0.01},
        {$args[0].rows[0].evidence.riderDistance=0.1},
        {$args[0].rows[0].evidence.observedCharging=$true},
        {$args[0].rows[0].evidence.hoverPure=$false},
        {$args[0].rows[0].evidence.identity[0].logic='OtherAbility'},
        {$args[0].rows[1].evidence.nativeChargeCompleted=$false},
        {$args[0].rows[1].evidence.rules.riderResolved=0},
        {$args[0].rows[1].evidence.rules.pairForcedD20=1},
        {$args[0].rows=@($args[0].rows[0]);$args[0].subscenarioPassCount=1}
    )) {
        $changed=New-ChargeEnvelope $mode;& $mutation $changed
        $rejected=$false;try{Assert-KmcChunk4ChargeEvidence $request $changed 'PASS'}catch{$rejected=$true}
        if(!$rejected){throw 'Unsafe or incomplete Charge evidence accepted.'};$passed++
    }
}
Write-Host "CHUNK 4 CHARGE PROTOCOL PASS=$passed FAIL=0"
