param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$labRoot=[IO.Path]::GetFullPath((Join-Path $repoRoot '../..'))
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'runtime/Chunk6aPausedQueueEvidence.ps1')
$tokens=$null;$parseErrors=$null
$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1'),[ref]$tokens,[ref]$parseErrors)
if($parseErrors.Count -ne 0){throw $parseErrors[0]}
foreach($name in @('Get-KmcPhase3dHorseRuntimeRows','Assert-KmcChunk6aCombatMountEvidence')){
 $d=@($ast.FindAll({param($n)$n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -ceq $name},$true))
 if($d.Count -ne 1){throw $name};. ([scriptblock]::Create($d[0].Extent.Text))
}
function Copy-PausedEnvelope($v){$v|ConvertTo-Json -Depth 100|ConvertFrom-Json}
$original=(Join-Path $labRoot 'runtime-evidence/c6a-fixtures-a-approach/phase3d-horse-scenario-evidence.json')
$originalHash=Get-KmcSha256 $original
$envelope=Read-KmcJson $original;$envelope.scenario='chunk6a-paused-queue'
$proof=@($envelope.observations.chunk6aCommandProofs|Where-Object window -CEQ 'positive-mount')[0]
$admission=@($proof.samples|Where-Object boundary -CEQ 'click-admission')[0]
$sample=[pscustomobject]@{frame=$admission.frame;gameTicks=$admission.gameTicks;paused=$true;turnBased=$false;allocationSequence=$admission.allocationSequence;traceComplete=$true;inMoveSlot=$true;createdByPlayer=$true;started=$false;acted=$false;finished=$false;state=(Copy-PausedEnvelope $admission.state)}
foreach($p in $admission.identity.PSObject.Properties){$sample|Add-Member -NotePropertyName $p.Name -NotePropertyValue $p.Value}
foreach($s in $proof.samples){if($s.boundary -cnotin @('init','click-admission','move-slot-installation')){$s.frame+=11}}
foreach($e in $proof.resourceWindow.events){if($e.sequence -gt $sample.allocationSequence){$e.frame+=11}}
$samples=@(for($i=0;$i -lt 11;$i++){$s=Copy-PausedEnvelope $sample;$s.frame+=$i;$s})
$released=Copy-PausedEnvelope $samples[-1];$released.paused=$false
$hold=[pscustomobject]@{contract='one-native-rt-mount-held-paused-then-unpaused-once';samples=$samples;events=@();beforeUnpause=(Copy-PausedEnvelope $samples[-1]);afterUnpause=$released;unpauseCount=1;complete=$true}
$envelope.observations|Add-Member -NotePropertyName chunk6aPausedQueue -NotePropertyValue $hold
$envelope.rows+=@([pscustomobject]@{name='CM06-paused-queue';status='PASS'})
$script:checks=0
function Check-PausedEnvelope($e){Assert-KmcChunk6aCombatMountEvidence ([pscustomobject]@{scenario='chunk6a-paused-queue'}) $e 'PASS'}
function Reject-PausedEnvelope([scriptblock]$mutate){
 $e=Copy-PausedEnvelope $envelope;& $mutate $e;$rejected=$false
 try{Check-PausedEnvelope $e}catch{$rejected=$true}
 if(-not $rejected){throw 'Corrupt paused envelope accepted'};$script:checks++
}
Check-PausedEnvelope $envelope;$script:checks++
Reject-PausedEnvelope {param($e)$e.rows=@($e.rows|Where-Object name -CNE 'CM06-paused-queue')}
Reject-PausedEnvelope {param($e)$e.rows+=@([pscustomobject]@{name='CM06-paused-queue';status='PASS'})}
Reject-PausedEnvelope {param($e)$e.observations.chunk6aPausedQueue.complete=$false}
Reject-PausedEnvelope {param($e)$e.observations.chunk6aPausedQueue.samples[5].state.mount.reactions++}
Reject-PausedEnvelope {param($e)$e.observations.chunk6aPausedQueue.samples[5].state.rider.reactionCooldown++}
Reject-PausedEnvelope {param($e)$e.observations.chunk6aPausedQueue.samples[5].commandObject++}
Reject-PausedEnvelope {param($e)$e.observations.chunk6aPausedQueue.unpauseCount=2}
Reject-PausedEnvelope {param($e)$e.observations.chunk6aCommandProofs[-1].resourceWindow.reactionResources.pass=$false}
foreach($row in @('CM02-adoption-plan-invalidated','CM02-geometry-change','CM02-obstruction','CM05-combat-dismount-accepted')){Reject-PausedEnvelope {param($e)$e.rows+=@([pscustomobject]@{name=$row;status='PASS'})}}
if((Get-KmcSha256 $original) -cne $originalHash){throw 'Original artifact changed'}
Write-Output ('PAUSED ENVELOPE PASS='+$script:checks+' FAIL=0; synthetic only, no native qualification')
