# Future qualification contract: a true PASS row must prove the claimed behavior.
# This supplements all payload, suite, artifact, restoration, native and composite gates.
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Chunk6aRegressionEvidence.ps1')
function Get-KmcChunk6aPrimaryContracts {
 $mount=@('CM01-combat-mount-accepted','CM03-combat-mount-conserves-debt','CM03-combat-mount-adoption-preparations')
 $dismount=@('CM05-combat-dismount-accepted','CM05-combat-dismount-conserves-debt','CM05-no-duplicate-mount-turn')
 $specs=@(
  @('CM01-horse-rt','chunk6a-mount-approach',$mount),
  @('CM01-horse-tb','chunk6a-combat-mount-tb',$mount),
  @('CM01-exploration-free','chunk6a-mount-approach',@('CM01-exploration-free')),
  @('CM02-approach-arrival','chunk6a-mount-approach',@('CM02-approach-arrival')),
  @('CM02-target-selection-cancelled','chunk6a-mount-approach',@('CM01-combat-mount-cancel-costs-nothing')),
  @('CM01-combat-mount-preparing-refused','chunk6a-adoption-compensation-tb',@('CM01-combat-mount-preparing-refused')),
  @('CM02-adoption-plan-invalidated','chunk6a-adoption-compensation-tb',@('CM02-adoption-plan-invalidated')),
  @('CM02-adoption-compensation-releases','chunk6a-adoption-compensation-tb',@('CM02-adoption-compensation-releases')),
  @('CM02-geometry-change','chunk6a-geometry-change',@('CM02-geometry-change')),
  @('CM02-obstruction','chunk6a-obstruction',@('CM02-obstruction')),
  @('CM05-rt','chunk6a-combat-mount-rt',$dismount),
  @('CM05-tb','chunk6a-combat-mount-tb',$dismount),
  @('CM01-mammoth-rt','chunk6a-mammoth-mount-rt',@('CM01-combat-mount-accepted','CM02-approach-arrival')),
  @('CM01-mammoth-tb','chunk6a-mammoth-mount-tb',@('CM01-combat-mount-accepted','CM02-approach-arrival','CM01-combat-mount-preparing-refused')),
  @('CM04-command-replacement','chunk6a-command-replacement',@('CM04-command-replacement')),
  @('CM04-stop-during-approach','chunk6a-stop-approach',@('CM04-stop-during-approach')),
  @('CM06-hotbar-path','chunk6a-hotbar-approach',@('CM06-hotbar-path')),
  @('CM06-pointer-target','chunk6a-combat-mount-tb',@('CM02-approach-arrival','CM01-combat-mount-accepted','CM03-combat-mount-conserves-debt')),
  @('CM06-paused-queue','chunk6a-paused-queue',@('CM06-paused-queue')),
  @('CM06-combat-mount-requires-qualified-paired-policy','chunk6a-refused-policy-disabled',@('CM06-combat-mount-requires-qualified-paired-policy')),
  @('CM02-foreign-companion','chunk6a-refused-foreign-companion',@('CM02-foreign-companion')),
  @('CM02-wrong-creature-target','chunk6a-refused-wrong-creature-target',@('CM02-wrong-creature-target')),
  @('CM06-mount-selected','chunk6a-refused-mount-selected',@('CM06-mount-selected')),
  @('CM06-multiple-selection','chunk6a-refused-multiple-selection',@('CM06-multiple-selection')),
  @('CM03-rider-before-mount-slot','chunk6a-allocation-rider-first-tb',@('CM03-rider-before-mount-slot','CM03-next-round-activation')),
  @('CM03-mount-slot-before-rider','chunk6a-allocation-mount-first-tb',@('CM03-mount-slot-before-rider','CM03-next-round-activation')),
  @('CM03-next-round-activation','chunk6a-allocation-mount-first-tb',@('CM03-next-round-activation','CM03-mount-slot-before-rider')),
  @('CM05-dismount-survives-feature-policy-disable','chunk6a-dismount-policy-disabled-rt',@('CM05-dismount-survives-feature-policy-disable')),
  @('CM06-ai-auto-use','chunk6a-auto-use-dismount-rt',@('CM06-ai-auto-use')),
  @('CM06-ai-auto-use','chunk6a-auto-use-mount-rt',@('CM06-ai-auto-use')),
  @('CM03-next-round-activation','chunk6a-allocation-rider-first-tb',@('CM03-next-round-activation','CM03-rider-before-mount-slot')),
  @('CM05-dismount-survives-feature-policy-disable','chunk6a-dismount-feature-disabled-rt',@('CM05-dismount-survives-feature-policy-disable')),
  @('CM02-adoption-plan-invalidated','chunk6a-adoption-compensation-rt',@('CM02-adoption-plan-invalidated')),
  @('CM02-adoption-compensation-releases','chunk6a-adoption-compensation-rt',@('CM02-adoption-compensation-releases')),
  @('CM08-area-restoration','chunk4-area-cleanup',@('native-area-clean-dismount')),
  @('CM08-mounted-mammoth-primary-hit-tb','mounted-mammoth-primary-hit-tb',@('mounted-mammoth-primary-hit-tb'))
 )
 foreach($spec in $specs) {
  [pscustomobject]@{id=$spec[0];scenario=$spec[1];rows=@($spec[2]);evidenceLeaf=$(if($spec[0]-ceq'CM08-area-restoration'){'boundary-scenario-evidence.jsonl'}elseif($spec[0]-ceq'CM08-mounted-mammoth-primary-hit-tb'){'combat-scenario-evidence.jsonl'}else{'phase3d-horse-scenario-evidence.json'})}
 }
 foreach($claimId in @(Get-KmcChunk6aRegressionClaims)) {
  foreach($role in @(Get-KmcChunk6aRegressionRoles $claimId)) {
   [pscustomobject]@{id=$claimId;scenario=$role.scenario;rows=@($role.rows);evidenceLeaf=$role.evidenceLeaf}
  }
 }
}
function Assert-KmcChunk6aPrimaryClaim([string]$Id,$Binding) {
 $matches=@(Get-KmcChunk6aPrimaryContracts|Where-Object id -CEQ $Id)
 if($matches.Count-eq0){throw "No implemented native evidence contract qualifies mandatory claim: $Id"}
 $matches=@($matches|Where-Object scenario -CEQ $Binding.scenario)
 if($matches.Count-ne1){throw 'Primary claim requires its exact scenario'}
 $contract=$matches[0]
 if($null-eq$Binding){throw 'Primary claim lacks a binding'}
 if([string]$Binding.scenario-cne$contract.scenario){throw 'Primary claim requires its exact scenario'}
 if([string]$Binding.evidenceLeaf-cne$contract.evidenceLeaf){throw 'Primary claim requires its exact artifact leaf'}
 $rows=@($Binding.rows)
 if($rows.Count-lt1-or@($rows|Select-Object -Unique).Count-ne$rows.Count){throw 'Primary claim requires nonempty unique rows'}
 foreach($row in $contract.rows){
  if(@($rows|Where-Object {$_-ceq$row}).Count-ne1){throw "Primary claim omitted its required row: $row"}
 }
}
