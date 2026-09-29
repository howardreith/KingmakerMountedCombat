param()
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
$checks=0
foreach($name in @('Assert-KmcStopInput','Assert-KmcHotbarInput','Assert-KmcHotbarUi','Assert-KmcNativeMammothArtifact','Assert-KmcPausedQueue','Assert-KmcChunk6aPreCombatPositioning')) {
    if($null -eq (Get-Command -Name $name -CommandType Function -ErrorAction SilentlyContinue)){throw ('Harness omitted isolated validator '+$name)}
    $checks++
}
$rows=@(Get-KmcPhase3dHorseRuntimeRows)
foreach($name in @('CM04-stop-during-approach','CM06-hotbar-path','CM06-paused-queue','CM01-native-mammoth-fixture')) {
    if(@($rows|Where-Object {$_ -ceq $name}).Count -ne 1){throw ('Harness omitted exact isolated row '+$name)};$checks++
}
. (Join-Path $PSScriptRoot 'runtime/Chunk6aSupportingEvidence.ps1')
foreach($case in @(
    @('CM01-mammoth-rt','chunk6a-mammoth-mount-rt',@('CM01-combat-mount-accepted','CM02-approach-arrival')),
    @('CM01-mammoth-tb','chunk6a-mammoth-mount-tb',@('CM01-combat-mount-accepted','CM02-approach-arrival','CM01-combat-mount-preparing-refused')),
    @('CM04-stop-during-approach','chunk6a-stop-approach',@('CM04-stop-during-approach')),
    @('CM02-ownership-change','chunk6a-ownership-change',@('CM02-ownership-change')),
    @('CM06-hotbar-path','chunk6a-hotbar-approach',@('CM06-hotbar-path')),
    @('CM06-paused-queue','chunk6a-paused-queue',@('CM06-paused-queue')))) {
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
