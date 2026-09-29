param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$labRoot=[IO.Path]::GetFullPath((Join-Path $repoRoot '../..'))
# Synthetic envelope regression. Never launches the game or changes original evidence.
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $repoRoot 'scripts/Test-Chunk6aSyntheticPrediction.ps1')
. (Join-Path $PSScriptRoot 'Test-Chunk6aReplacementEvidence.ps1')
$tokens=$null;$parseErrors=$null
$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1'),[ref]$tokens,[ref]$parseErrors)
if($parseErrors.Count -ne 0){throw $parseErrors[0]}
foreach($name in @('Get-KmcPhase3dHorseRuntimeRows','Assert-KmcChunk6aCombatMountEvidence')){
    $definitions=@($ast.FindAll({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -ceq $name},$true))
    if($definitions.Count -ne 1){throw ('Missing exact draft function '+$name)}
    . ([scriptblock]::Create($definitions[0].Extent.Text))
}
$envelope=Get-Content -Raw $original|ConvertFrom-Json
Add-KmcSyntheticPredictionFields $envelope
$envelope.scenario='chunk6a-command-replacement'
$envelope.rows=@($envelope.rows|Where-Object name -CNE 'CM02-obstruction')+@([pscustomobject]@{name='CM04-command-replacement';status='PASS'})
$envelope.observations.PSObject.Properties.Remove('chunk6aObstruction')
$envelope.observations.PSObject.Properties.Remove('chunk6aDoorFixture')
$envelope.observations|Add-Member -NotePropertyName chunk6aReplacementApproach -NotePropertyValue (Copy-StopDraft $case)
$script:envelopeChecks=0
function Assert-StopEnvelope($value) {
    Assert-KmcChunk6aCombatMountEvidence ([pscustomobject]@{scenario='chunk6a-command-replacement'}) $value 'PASS'
}
function Reject-StopEnvelope([scriptblock]$mutate){
    $value=Copy-StopDraft $envelope;& $mutate $value
    $rejected=$false;try{Assert-StopEnvelope $value}catch{$rejected=$true}
    if(-not $rejected){throw 'Invalid Stop envelope accepted'}
    $script:envelopeChecks++
}
Assert-StopEnvelope $envelope;$script:envelopeChecks++
Reject-StopEnvelope {param($e) $e.rows=@($e.rows|Where-Object name -CNE 'CM04-command-replacement')}
Reject-StopEnvelope {param($e) $e.rows+=@([pscustomobject]@{name='CM04-command-replacement';status='PASS'})}
foreach($name in @('CM01-combat-mount-accepted','CM02-approach-arrival','CM02-geometry-change','CM02-obstruction','CM05-combat-dismount-accepted','CM02-adoption-plan-invalidated')){
    Reject-StopEnvelope {param($e) $e.rows+=@([pscustomobject]@{name=$name;status='PASS'})}
}
Reject-StopEnvelope {param($e) $e.observations.chunk6aReplacementApproach.input.events[1].commandObject++}
Reject-StopEnvelope {param($e) $e.observations.chunk6aReplacementApproach.commandProof.resourceWindow.reactionResources.pass=$false}
Reject-StopEnvelope {param($e) $e.observations.chunk6aCommandProofs=@($e.observations.chunk6aCommandProofs|Select-Object -First 1)}
Reject-StopEnvelope {param($e) $e.observations.chunk6aCommandProofs+=@($e.observations.chunk6aCommandProofs[0])}
Reject-StopEnvelope {param($e) $e.observations.chunk6aReplacementApproach.commandProof.contract='unacted-native-obstruction-no-cost-or-transition'}
if((Get-FileHash $original).Hash -cne $originalHash){throw 'Historical shape artifact mutated'}
Write-Output ('REPLACEMENT DRAFT ENVELOPE PASS='+$script:envelopeChecks+' FAIL=0; synthetic, no runtime qualification')