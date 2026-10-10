param()
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
$checks=0
foreach($name in @('Assert-KmcStopInput','Assert-KmcHotbarInput','Assert-KmcHotbarUi','Assert-KmcNativeMammothArtifact','Assert-KmcPausedQueue','Assert-KmcChunk6aPreCombatPositioning','Assert-KmcChunk6aLifecycleBoundary','Assert-KmcChunk6aFoundationPersistenceEvidence',
    'Assert-KmcChunk6aApproachInvalidation','Assert-KmcChunk6aTurnEnd','Assert-KmcChunk6aModeExit','Assert-KmcChunk6aClosurePersistenceEvidence','Assert-KmcChunk6aAreaRestoration','Assert-KmcChunk6aSchemaUnchanged')) {
    if($null -eq (Get-Command -Name $name -CommandType Function -ErrorAction SilentlyContinue)){throw ('Harness omitted isolated validator '+$name)}
    $checks++
}
$rows=@(Get-KmcPhase3dHorseRuntimeRows)
foreach($name in @('CM04-stop-during-approach','CM04-combat-end','CM04-disable-unload','CM06-hotbar-path','CM06-paused-queue','CM01-native-mammoth-fixture',
    'CM02-left-area','CM02-view-agent-lost','CM02-loading-cutscene','CM02-generation-change','CM04-injected-exception','CM04-rider-death','CM04-mount-death','CM04-turn-end','CM04-mode-exit')) {
    if(@($rows|Where-Object {$_ -ceq $name}).Count -ne 1){throw ('Harness omitted exact isolated row '+$name)};$checks++
}
. (Join-Path $PSScriptRoot 'runtime/Chunk6aSupportingEvidence.ps1')
foreach($case in @(
    @('CM01-mammoth-rt','chunk6a-mammoth-mount-rt',@('CM01-combat-mount-accepted','CM02-approach-arrival')),
    @('CM01-mammoth-tb','chunk6a-mammoth-mount-tb',@('CM01-combat-mount-accepted','CM02-approach-arrival','CM01-combat-mount-preparing-refused')),
    @('CM04-stop-during-approach','chunk6a-stop-approach',@('CM04-stop-during-approach')),
    @('CM04-combat-end','chunk6a-combat-end-approach',@('CM04-combat-end')),
    @('CM04-disable-unload','chunk6a-disable-approach',@('CM04-disable-unload')),
    @('CM07-mount-save-rt','persistence-p04-save',@('P04-save-combat-mount-rt')),
    @('CM07-mount-load-rt','persistence-p04-load',@('P04-load-combat-mount-rt')),
    @('CM07-dismount-save','persistence-p04-save',@('P04-save-combat-dismount-rt')),
    @('CM07-dismount-load','persistence-p04-load',@('P04-load-combat-dismount-rt')),
    @('CM07-mount-save-tb','persistence-p02-save',@('P02-save-combat-mount-tb')),
    @('CM07-mount-load-tb','persistence-p02-load',@('P02-load-combat-mount-tb')),
    @('CM02-ownership-change','chunk6a-ownership-change',@('CM02-ownership-change')),
    @('CM02-size-form-change','chunk6a-size-form-change',@('CM02-size-form-change')),
    @('CM02-lost-direct-control','chunk6a-lost-direct-control',@('CM02-lost-direct-control')),
    @('CM02-rider-incapacitated','chunk6a-rider-incapacitated',@('CM02-rider-incapacitated')),
    @('CM02-mount-incapacitated','chunk6a-mount-incapacitated',@('CM02-mount-incapacitated')),
    @('CM06-hotbar-path','chunk6a-hotbar-approach',@('CM06-hotbar-path')),
    @('CM06-paused-queue','chunk6a-paused-queue',@('CM06-paused-queue')),
    # Chunk 6 closeout: the fifteen previously unimplemented ids and the re-registered area restoration.
    @('CM07-save-slot-routes','persistence-p04-save',@('P04-save-combat-mount-routes')),
    @('CM07-unsettled-save-deferred','persistence-p04-save',@('P04-save-combat-mount-unsettled-save')),
    @('CM07-area-reload','persistence-p04-save',@('P04-save-combat-mount-area-reload')),
    @('CM04-area-session-transition','persistence-p04-save',@('P04-save-pending-mount-area')),
    @('CM08-area-restoration','persistence-p07-save',@('P07-save-area-reload')),
    @('CM07-schema-unchanged','persistence-p04-save',@('P04-save-combat-mount-rt')),
    @('CM02-left-area','chunk6a-left-area',@('CM02-left-area')),
    @('CM02-view-agent-lost','chunk6a-view-agent-lost',@('CM02-view-agent-lost')),
    @('CM02-loading-cutscene','chunk6a-loading-cutscene',@('CM02-loading-cutscene')),
    @('CM02-generation-change','chunk6a-generation-change',@('CM02-generation-change')),
    @('CM04-injected-exception','chunk6a-injected-exception',@('CM04-injected-exception')),
    @('CM04-rider-death','chunk6a-rider-death-approach',@('CM04-rider-death')),
    @('CM04-mount-death','chunk6a-mount-death-approach',@('CM04-mount-death')),
    @('CM04-turn-end','chunk6a-turn-end-approach-tb',@('CM04-turn-end')),
    @('CM04-mode-exit','chunk6a-mode-exit-tb',@('CM04-mode-exit','CM01-combat-mount-accepted','CM01-combat-mount-preparing-refused')))) {
    $binding=[pscustomobject]@{scenario=$case[1];rows=@($case[2])}
    if(-not (Assert-KmcIsolatedScenarioRows $case[0] $binding)){throw 'Missing isolated qualification mapping'};$checks++
    foreach($mutation in @('scenario','rows')) {
        $bad=[pscustomobject]@{scenario=$case[1];rows=@($case[2])}
        if($mutation -ceq 'scenario'){$bad.scenario='chunk6a-mount-approach'}else{$bad.rows=@()}
        $rejected=$false;try{$null=Assert-KmcIsolatedScenarioRows $case[0] $bad}catch{$rejected=$true}
        if(-not $rejected){throw 'Isolated qualification accepted a different scenario or missing row'};$checks++
    }
}
Write-Host "ISOLATED INPUT REGISTRATION PASS=$checks FAIL=0"
