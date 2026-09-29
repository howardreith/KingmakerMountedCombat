Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'RefusedMountEvidence.ps1')
. (Join-Path $PSScriptRoot 'ForeignCompanionEvidence.ps1')
function Assert-KmcRefusalCase($E) {
 $id=$E.identity;$legal=$E.legal;$n=$E.condition;$p=$E.input
 if($E.contract -cne 'one-native-refused-mount-in-fresh-rt-allocation'){throw 'Wrong refusal case contract'}
 $actorIds=@($id.riderId,$id.mountId,$id.unrelatedId)
 if(@($actorIds|Where-Object {[string]::IsNullOrWhiteSpace($_)}).Count -ne 0 -or @($actorIds|Select-Object -Unique).Count -ne 3){throw 'Refusal requires three exact actors'}
 $actorObjects=@($id.riderObject,$id.mountObject,$id.unrelatedObject)
 if(@($actorObjects|Where-Object {($_ -isnot [int] -and $_ -isnot [long]) -or $_ -eq 0}).Count -ne 0 -or @($actorObjects|Select-Object -Unique).Count -ne 3){throw 'Refusal native object identities missing'}
 foreach($field in @('reciprocalPair','unrelatedLivePartyActor')){if($id.$field -isnot [bool] -or $id.$field -ne $true){throw 'Native refusal fixture unavailable'}}
 if($E.case -ceq 'foreign-companion'){Assert-KmcForeignCompanion $E}
 else {foreach($field in @('unrelatedIsPet','unrelatedSupportedMount')){if($id.$field -isnot [bool] -or $id.$field -ne $false){throw 'Unrelated actor is not a plain unrelated creature'}}}
 if($id.mountProfile -cne 'Horse'){throw 'Refusal profile differs'}
 $reason='Select the exact prospective rider.';$target=$id.mountId;$targetObject=$id.mountObject
 switch -CaseSensitive ($E.case) {
  'foreign-companion' {$selection=@($id.riderId);$objects=@($id.riderObject);$target=$id.unrelatedId;$targetObject=$id.unrelatedObject;$row='CM02-foreign-companion';$reason="Mount target rejected: click the selected rider's exact active Horse."}
  'wrong-creature-target' {$selection=@($id.riderId);$objects=@($id.riderObject);$target=$id.unrelatedId;$targetObject=$id.unrelatedObject;$row='CM02-wrong-creature-target';$reason="Mount target rejected: click the selected rider's exact active Horse."}
  'mount-selected' {$selection=@($id.mountId);$objects=@($id.mountObject);$row='CM06-mount-selected'}
  'multiple-selection' {$selection=@($id.riderId,$id.mountId);$objects=@($id.riderObject,$id.mountObject);$row='CM06-multiple-selection'}
  'foreign-selection' {$selection=@($id.unrelatedId);$objects=@($id.unrelatedObject);$row='CM06-foreign-selection'}
  default {throw 'Unknown refusal case'}
 }
 if($E.row -cne $row -or $E.scenario -cne ('chunk6a-refused-'+$E.case)){throw 'Refusal case scenario/row mapping differs'}
 foreach($field in @('inCombat','visible','enabled','canTargetOwnedMount','exactRiderSelected')){if($legal.$field -isnot [bool] -or $legal.$field -ne $true){throw 'Legal Mount baseline unavailable'}}
 if($legal.turnBased -isnot [bool] -or $legal.turnBased -ne $false -or $legal.state.selectedIds -isnot [Array] -or $legal.state.selectedIds.Count -ne 1 -or $legal.state.selectedIds[0] -cne $id.riderId){throw 'Legal baseline requires the exact RT rider'}
 foreach($field in @('selectionVerified','inCombat','visible')){if($n.$field -isnot [bool] -or $n.$field -ne $true){throw 'Negative fixture unavailable'}}
 foreach($field in @('turnBased','canTargetRequested')){if($n.$field -isnot [bool] -or $n.$field -ne $false){throw 'Negative fixture did not refuse'}}
 $wrong=$E.case -cin @('wrong-creature-target','foreign-companion')
 foreach($field in @('enabled','canTargetOwnedMount')){if($n.$field -isnot [bool] -or $n.$field -ne $wrong){throw 'Exact refusal cause differs'}}
 if($n.selectedIds -isnot [Array] -or $n.selectedObjects -isnot [Array] -or ($n.selectedIds -join ',') -cne ($selection -join ',') -or ($n.selectedObjects -join ',') -cne ($objects -join ',')){throw 'Negative exact selection differs'}
 foreach($v in $n.selectedObjects){if($v -isnot [int] -and $v -isnot [long]){throw 'Negative selection objects malformed'}}
 if(($n.targetObject -isnot [int] -and $n.targetObject -isnot [long]) -or $n.targetId -cne $target -or $n.targetObject -ne $targetObject -or $n.expectedReason -cne $reason){throw 'Exact target or expected reason differs'}
 foreach($field in @('frame','gameTicks')){foreach($sample in @($legal,$n,$p.before)){if(($sample.$field -isnot [int] -and $sample.$field -isnot [long]) -or $sample.$field -ne $legal.$field){throw 'Legal/negative/input clock differs'}}}
 if($legal.state.relationshipState -cne 'Unmounted' -or $legal.state.generation -ne $p.before.state.generation -or ($legal.state.ledger|ConvertTo-Json -Depth 30 -Compress) -cne ($p.before.state.ledger|ConvertTo-Json -Depth 30 -Compress)){throw 'Selection setup changed relationship'}
 foreach($actor in @('rider','mount')){foreach($field in @('standard','move','swift','reactions','reactionCooldown','initiativeCooldown','initiativeOrder','reactionsPerRound','nativePrepareCount')){
  $a=$legal.state.$actor.$field;$b=$p.before.state.$actor.$field
  if($null -eq $a -or $null -eq $b -or [double]::IsNaN([double]$a) -or [double]::IsInfinity([double]$a) -or $a -ne $b){throw 'Selection setup changed action/reaction resources'}
 }}
 foreach($actor in @('riderPosition','horsePosition')){foreach($axis in @('x','y','z')){if($null -eq $legal.state.geometry.$actor.$axis -or $legal.state.geometry.$actor.$axis -ne $p.before.state.geometry.$actor.$axis){throw 'Selection setup moved actors'}}}
 if(($n.state|ConvertTo-Json -Depth 40 -Compress) -cne ($p.before.state|ConvertTo-Json -Depth 40 -Compress)){throw 'Negative baseline is not exact click baseline'}
 Assert-KmcRefusedMountInput $p $id.riderId $id.mountId $target $selection
 if($p.activations[1].reason -cne $reason){throw 'Native refusal did not name the exact tested cause'}
}
function Assert-KmcRefusalExploration($E,$Proofs) {
 foreach($proof in $Proofs) {
  if($proof.identity.casterId -cne $E.identity.riderId -or $proof.mountId -cne $E.identity.mountId){throw 'Refusal borrowed another pair exploration window.'}
 }
 $end=@($Proofs|Where-Object window -CEQ 'exploration-dismount')[0].samples[-1]
 foreach($state in @($E.legal.state,$E.input.before.state)) {
  if($state.generation -ne $end.state.generation -or ($state.ledger|ConvertTo-Json -Depth 30 -Compress) -cne ($end.state.ledger|ConvertTo-Json -Depth 30 -Compress)){throw 'Combat refusal inherited another relationship transition.'}
 }
 if($E.input.before.frame -le $end.frame -or $E.input.before.gameTicks -lt $end.gameTicks -or $E.input.before.allocationSequence -lt $end.allocationSequence){throw 'Refusal does not follow its own exploration windows.'}
}
