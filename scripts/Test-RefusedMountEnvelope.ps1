param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
. (Join-Path $repo 'scripts/runtime/RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'Test-RefusedMountCase.ps1') -Configuration $Configuration
$tokens=$null;$errors=$null
$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1'),[ref]$tokens,[ref]$errors)
if($errors.Count -ne 0){throw $errors[0]}
foreach($name in @('Get-KmcPhase3dHorseRuntimeRows','Assert-KmcChunk6aCombatMountEvidence')){
 $defs=@($ast.FindAll({param($n)$n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -ceq $name},$true))
 if($defs.Count -ne 1){throw 'Envelope function missing'}
 . ([scriptblock]::Create($defs[0].Extent.Text))
}
$original='C:/Dev/KingmakerMountedCombatLab/runtime-evidence/c6a-path-a-approach/phase3d-horse-scenario-evidence.json'
$originalHash=(Get-FileHash $original).Hash
$shape=Get-Content -Raw $original|ConvertFrom-Json
$windows=@($shape.observations.chunk6aCommandProofs|Where-Object window -CLike 'exploration-*')
# In-memory synthetic current-schema envelope; the hashed native artifacts are unchanged.
foreach($proof in $windows){
 $proof|Add-Member -NotePropertyName predictionCommands -NotePropertyValue ([pscustomobject]@{contract='native-speculative-init-separated-from-one-committed-request';commands=@();pass=$true})
 foreach($sample in $proof.samples){$sample|Add-Member -NotePropertyName simulatingClick -NotePropertyValue $false}
}
$end=$windows[1].samples[-1]
$riderId=$windows[0].identity.casterId;$mountId=$windows[0].mountId
function Copy-Envelope($e){$e|ConvertTo-Json -Depth 100|ConvertFrom-Json}
$script:envelopeChecks=0
foreach($caseName in @('wrong-creature-target','mount-selected','multiple-selection','foreign-selection','foreign-companion','policy-disabled')) {
 $fixture=New-RefusalCase $caseName
 $fixture.identity.riderId=$riderId;$fixture.identity.mountId=$mountId
 $fixture.input.riderId=$riderId;$fixture.input.mountId=$mountId
 $fixture.legal.state.selectedIds=@($riderId)
 switch($caseName){
  'policy-disabled'{$selection=@($riderId);$target=$mountId}
  'foreign-companion'{$selection=@($riderId);$target='other'}
  'wrong-creature-target'{$selection=@($riderId);$target='other'}
  'mount-selected'{$selection=@($mountId);$target=$mountId}
  'multiple-selection'{$selection=@($riderId,$mountId);$target=$mountId}
  'foreign-selection'{$selection=@('other');$target=$mountId}
 }
 $fixture.condition.selectedIds=$selection;$fixture.condition.targetId=$target;$fixture.input.targetId=$target
 foreach($s in @($fixture.input.before,$fixture.input.after,$fixture.legal,$fixture.condition)){$s.frame=[int]$end.frame+1;$s.gameTicks=[long]$end.gameTicks+1000000L;$s.state.generation=$end.state.generation;$s.state.ledger=Copy-Envelope $end.state.ledger}
 foreach($s in @($fixture.input.before,$fixture.input.after)){$s.state.selectedIds=$selection;$s.allocationSequence=[int]$end.allocationSequence+1}
 foreach($e in $fixture.input.events){$e.casterId=$riderId;$e.targetId=$target;$e.frame=$fixture.input.before.frame;$e.gameTicks=$fixture.input.before.gameTicks}
 $refused=$fixture.input.activations[1];$refused.casterId=$riderId;$refused.targetId=$target;$refused.selectedIds=$selection -join ',';$refused.frame=$fixture.input.before.frame
 $fixture.condition.state=Copy-Envelope $fixture.input.before.state

 if($caseName-ceq'policy-disabled'){
  foreach($name in @('before','disabled','restored')){
   $b=$fixture.policy.$name
   $b.state=Copy-Envelope $fixture.input.before.state
   foreach($field in @('frame','gameTicks','allocationSequence')){$b.$field=$fixture.input.before.$field}
  }
  $p=$fixture.policy.resources;$p.riderId=$riderId;$p.mountId=$mountId
  foreach($endName in @('before','after')){
   $b=$p.$endName
   foreach($field in @('frame','gameTicks','allocationSequence')){$b.$field=$fixture.input.before.$field}
   $b.rider.actor=$riderId;$b.mount.actor=$mountId
  }
 }

 $envelope=Copy-Envelope $shape
 $envelope.scenario=$fixture.scenario
 $envelope.rows=@($envelope.rows|Where-Object name -CNotLike 'CM*')+@(foreach($name in @('CM01-exploration-dismount-costs-nothing','CM01-exploration-free','CM01-combat-mount-cancel-costs-nothing',$fixture.row)){[pscustomobject]@{name=$name;status='PASS'}})
 $envelope.observations.chunk6aCommandProofs=Copy-Envelope $windows
 $envelope.observations|Add-Member -NotePropertyName chunk6aRefusedMount -NotePropertyValue $fixture
 function Assert-Envelope($e){Assert-KmcChunk6aCombatMountEvidence ([pscustomobject]@{scenario=$fixture.scenario}) $e 'PASS'}
 function Reject-Envelope([scriptblock]$mutate){$e=Copy-Envelope $envelope;& $mutate $e;$rejected=$false;try{Assert-Envelope $e}catch{$rejected=$true};if(-not $rejected){throw ('Refusal envelope accepted '+$mutate.ToString())};$script:envelopeChecks++}
 Assert-Envelope $envelope;$script:envelopeChecks++
 Reject-Envelope {param($e)$e.rows=@($e.rows|Where-Object name -CNE $fixture.row)}
 Reject-Envelope {param($e)$e.rows+=@([pscustomobject]@{name=$fixture.row;status='PASS'})}
 foreach($row in @('CM01-combat-mount-accepted','CM02-approach-arrival','CM02-adoption-plan-invalidated','CM02-obstruction','CM02-geometry-change','CM04-stop-during-approach','CM05-combat-dismount-accepted','CM06-paused-queue')){
  Reject-Envelope {param($e)$e.rows+=@([pscustomobject]@{name=$row;status='PASS'})}
 }
 Reject-Envelope {param($e)$e.observations.chunk6aRefusedMount.scenario='chunk6a-mount-approach'}
 Reject-Envelope {param($e)$e.observations.chunk6aCommandProofs=@($e.observations.chunk6aCommandProofs|Select-Object -First 1)}
 Reject-Envelope {param($e)$e.observations.chunk6aRefusedMount.input.after.state.rider.reactions--}
 Reject-Envelope {param($e)$e.observations.chunk6aRefusedMount.input.activations[1].reason='unrelated failure'}
 Reject-Envelope {param($e)$e.observations.chunk6aRefusedMount.input.constructedCommands=@([pscustomobject]@{commandObject=999})}
 Reject-Envelope {param($e)$e.observations.chunk6aRefusedMount.legal.state.generation++}
}
if((Get-FileHash $original).Hash -cne $originalHash){throw 'Historical native evidence mutated'}
Write-Output ('REFUSAL ENVELOPE PASS='+$script:envelopeChecks+' FAIL=0; historical shape read unchanged; synthetic only')
