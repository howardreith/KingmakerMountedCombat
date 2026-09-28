param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$labRoot=[IO.Path]::GetFullPath((Join-Path $repoRoot '../..'))
# Lab-only future hotbar dispatch proof; original frozen122 artifact is an immutable shape fixture.
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Test-Chunk6aSyntheticPrediction.ps1')
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'runtime/Chunk6aHotbarEvidence.ps1')
$tokens=$null;$parseErrors=$null
$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1'),[ref]$tokens,[ref]$parseErrors)
if($parseErrors.Count -ne 0){throw $parseErrors[0]}
foreach($name in @('Get-KmcPhase3dHorseRuntimeRows','Assert-KmcChunk6aCombatMountEvidence')){
    $definitions=@($ast.FindAll({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -ceq $name},$true))
    if($definitions.Count -ne 1){throw ('Missing exact draft function '+$name)}
    . ([scriptblock]::Create($definitions[0].Extent.Text))
}
function Copy-Hotbar($v){$v|ConvertTo-Json -Depth 100|ConvertFrom-Json}
$original=(Join-Path $labRoot 'runtime-evidence/c6a-fixtures-a-approach/phase3d-horse-scenario-evidence.json')
$originalHash=(Get-FileHash $original).Hash
$envelope=Get-Content -Raw $original|ConvertFrom-Json
Add-KmcSyntheticPredictionFields $envelope
$envelope.scenario='chunk6a-hotbar-approach'
$proof=@($envelope.observations.chunk6aCommandProofs|Where-Object window -CEQ 'positive-mount')[0]
$admission=@($proof.samples|Where-Object boundary -CEQ 'click-admission')[0]
$fixtures=Get-Content -Raw (Join-Path $repoRoot 'tests/Fixtures/Chunk6aHotbarSynthetic.json')|ConvertFrom-Json
$p=Copy-Hotbar @($fixtures|Where-Object expectPass -EQ $true|Select-Object -First 1)[0].input
$p.baseline=Copy-Hotbar $proof.preClick.state
$p.registration.casterId=$proof.identity.casterId
foreach($event in $p.events){
    $event.casterId=$proof.identity.casterId;$event.targetId=$proof.identity.targetId;$event.abilityGuid=$proof.identity.abilityGuid
    $event.frame=$admission.frame;$event.gameTicks=$admission.gameTicks;$event.state=Copy-Hotbar $proof.preClick.state
    if($event.boundary -ceq 'click-after'){$event.commandObject=$proof.identity.commandObject;$event.commandTargetId=$proof.identity.targetId}
}
$group=if($p.registration.route -ceq 'group'){$p.registration.ownerObject}else{999}
$before=@([pscustomobject]@{index=0;groupObject=$group;type='ActivatableAbility';toggle=$false})
$opened=@([pscustomobject]@{index=0;groupObject=$group;type='ActivatableAbility';toggle=$true})
$ui=[pscustomobject]@{contract='native-action-bar-group-input-and-exact-restoration';restored=$true;casterId=$proof.identity.casterId;managerObject=$p.registration.managerObject;abilityGroupObject=$group;before=$before;opened=$opened;after=(Copy-Hotbar $before)}
$envelope.observations|Add-Member -NotePropertyName chunk6aHotbarInput -NotePropertyValue $p
$envelope.observations|Add-Member -NotePropertyName chunk6aHotbarUi -NotePropertyValue $ui
$envelope.rows+=@([pscustomobject]@{name='CM06-hotbar-path';status='PASS'})
$script:checks=0
function Check-HotbarEnvelope($e){Assert-KmcChunk6aCombatMountEvidence ([pscustomobject]@{scenario='chunk6a-hotbar-approach'}) $e 'PASS'}
function Reject-HotbarEnvelope([scriptblock]$mutate){
    $e=Copy-Hotbar $envelope;& $mutate $e;$rejected=$false
    try{Check-HotbarEnvelope $e}catch{$rejected=$true}
    if(-not $rejected){throw 'Corrupt hotbar envelope accepted'}
    $script:checks++
}
Check-HotbarEnvelope $envelope;$script:checks++
Reject-HotbarEnvelope {param($e) $e.rows=@($e.rows|Where-Object name -CNE 'CM06-hotbar-path')}
Reject-HotbarEnvelope {param($e) $e.rows+=@([pscustomobject]@{name='CM06-hotbar-path';status='PASS'})}
Reject-HotbarEnvelope {param($e) $e.observations.chunk6aHotbarInput.events[-1].commandObject++}
Reject-HotbarEnvelope {param($e) $e.observations.chunk6aHotbarInput.events[0].slotObject++}
Reject-HotbarEnvelope {param($e) $e.observations.chunk6aHotbarInput.events=@($e.observations.chunk6aHotbarInput.events|Select-Object -Skip 1)}
Reject-HotbarEnvelope {param($e) $e.observations.chunk6aHotbarUi.after[0].toggle=$true}
Reject-HotbarEnvelope {param($e) $e.observations.chunk6aHotbarUi.restored=$false}
Reject-HotbarEnvelope {param($e) $e.observations.chunk6aCommandProofs[-1].resourceWindow.reactionResources.pass=$false}
Reject-HotbarEnvelope {param($e) $e.observations.chunk6aCommandProofs=@($e.observations.chunk6aCommandProofs|Select-Object -First 2)}
Reject-HotbarEnvelope {param($e) $e.rows+=@([pscustomobject]@{name='CM02-adoption-plan-invalidated';status='PASS'})}
if((Get-FileHash $original).Hash -cne $originalHash){throw 'Frozen122 source fixture changed'}
Write-Output ('HOTBAR DRAFT ENVELOPE PASS='+$script:checks+' FAIL=0; synthetic, no native qualification')