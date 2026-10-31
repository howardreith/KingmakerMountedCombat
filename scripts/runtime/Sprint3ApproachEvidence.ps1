# Read-only substantive acceptance for the Sprint 3 carried-approach rows (TRAP-01, SPELL-01).
# Native C# emits observations; this module owns their interpretation. No file access, staging or native writes.
Set-StrictMode -Version Latest
function Get-KmcSprint3ApproachCases([string]$Scenario) {
 if($Scenario -ceq 'sprint3-approach-tb'){ return @('S3-tb-touch-friendly','S3-tb-insufficient-movement') }
 @('S3-trap-disarm','S3-touch-friendly-distant','S3-touch-held-charge','S3-touch-hostile','S3-ranged-distant')
}
function Test-KmcSprint3Prop($Object,[string]$Name){ $null -ne $Object -and $null -ne $Object.PSObject.Properties[$Name] -and $null -ne $Object.$Name }
function Get-KmcSprint3Events($Row,[string]$Kind){ if(-not (Test-KmcSprint3Prop $Row.evidence 'events')){ return @() }; @($Row.evidence.events | Where-Object { [string]$_.kind -ceq $Kind }) }
# The row's last carried-approach outcome: the product's own record of the mount's delegated approach for the rider's command.
function Get-KmcSprint3LastCarried($Row){ if(-not (Test-KmcSprint3Prop $Row.evidence 'carriedOutcomes')){ return $null }; $all=@($Row.evidence.carriedOutcomes); if($all.Count -eq 0){ return $null }; $all[$all.Count-1] }
function Assert-KmcSprint3CarriedRow($Row,[string]$Rider,[string]$Mount) {
 $e=$Row.evidence
 if($e.carriedSeen -ne $true){ throw ($Row.name+': the product never carried the rider command (no live carried approach observed)') }
 if(-not (Test-KmcSprint3Prop $e 'mountTravelledMetres') -or [double]$e.mountTravelledMetres -lt 1){ throw ($Row.name+': the mount did not travel for the carried command') }
 if([double]$e.riderDisplacement -lt 0.5 -or [double]$e.mountDisplacement -lt 0.5){ throw ($Row.name+': the pair did not move together during the approach') }
 $c=Get-KmcSprint3LastCarried $Row
 if($null -eq $c){ throw ($Row.name+': no carried-approach outcome was recorded') }
 if([string]$c.rider -cne $Rider -or [string]$c.mount -cne $Mount){ throw ($Row.name+': carried approach identity is not the exact pair') }
 if([int]$c.moveStarts -lt 1){ throw ($Row.name+': the carried approach started no delegated mount move') }
 if($c.slotRestored -ne $true){ throw ($Row.name+': the mount Move slot was not restored after the carried approach') }
 if($null -ne $c.terminalReason -and [string]$c.terminalReason -ne ''){ throw ($Row.name+': the carried approach ended with a fault: '+$c.terminalReason) }
}
function Assert-KmcSprint3TrapRow($Row,[string]$Rider,[string]$Mount) {
 $e=$Row.evidence
 if($e.clicked -ne $true -or [int]$e.inputCount -ne 1 -or [string]$e.inputPath -cne 'ClickMapObjectHandler.OnClick'){ throw 'S3 trap: not one ordinary map-object click' }
 if([string]$e.admitted.type -cne 'UnitInteractWithObject' -or [string]$e.admitted.executor -cne $Rider -or [string]$e.admitted.interaction -cne 'DisableTrapInteractionComponent'){ throw 'S3 trap: the native trap interaction was not admitted on the rider' }
 if([double]$e.admitted.approachRadius -gt 0.31 -or $e.admitted.needLoS -ne $true){ throw 'S3 trap: the native trap approach radius/LOS was changed' }
 if([double]$e.trap.distanceAtClick -lt 3){ throw 'S3 trap: the click began inside immediate range (no approach to prove)' }
 # Discovery is native: the trap must have passed the party's own Perception roll and be revealed when it is clicked.
 if(-not (Test-KmcSprint3Prop $e 'trapAtClick') -or $e.trapAtClick.perceptionPassed -ne $true -or $e.trapAtClick.revealed -ne $true -or $e.trapAtClick.active -ne $true){ throw 'S3 trap: the clicked trap was not natively discovered, revealed and active' }
 if(Test-KmcSprint3Prop $e 'explorationLegs'){
  foreach($leg in @($e.explorationLegs)){ if($leg.ordered -eq $true -and [double]$leg.destinationToTrap -lt 3.5){ throw 'S3 trap: an exploration leg was ordered onto the trap' } }
  $last=@($e.explorationLegs)[-1]
  if([string]$last.outcome -cne 'discovered' -or $last.perceptionBefore -eq $true){ throw 'S3 trap: the last exploration leg did not end in a native discovery of a previously undiscovered trap' }
  if(@($last.perceptionChecks).Count -lt 1 -or @($last.perceptionChecks | Where-Object { $_.passed -eq $true }).Count -lt 1){ throw 'S3 trap: no passed native Perception roll was observed for the discovered trap' }
 }
 Assert-KmcSprint3CarriedRow $Row $Rider $Mount
 $c=Get-KmcSprint3LastCarried $Row
 if([string]$c.kind -cne 'TrapInteraction'){ throw 'S3 trap: the carried approach was not a trap interaction' }
 if($c.satisfied -ne $true){ throw 'S3 trap: the carried approach never satisfied the native range' }
 if([string]$e.command.type -cne 'UnitInteractWithObject' -or $e.command.acted -ne $true -or [string]$e.command.result -cne 'Success'){ throw 'S3 trap: the native interaction command did not act through its own OnAction' }
 $checks=@($e.trapEvents.events | Where-Object { [string]$_.kind -ceq 'skill-check' -and [string]$_.actor -ceq $Rider })
 if($checks.Count -ne 1 -or [string]$checks[0].stat -cne 'SkillThievery'){ throw 'S3 trap: exactly one native rider Trickery check was required' }
 if([int]$checks[0].dc -lt 1 -or [int]$checks[0].d20 -lt 1 -or [int]$checks[0].d20 -gt 20){ throw 'S3 trap: the native check lacks a real DC or d20' }
 $disarms=@($e.trapEvents.events | Where-Object { ([string]$_.kind).StartsWith('disarm-') })
 if($disarms.Count -ne 1 -or [string]$disarms[0].actor -cne $Rider){ throw 'S3 trap: exactly one native disarm outcome by the rider was required' }
 $passed=[bool]$checks[0].passed; $kind=[string]$disarms[0].kind
 if(($passed -and $kind -cne 'disarm-success') -or (-not $passed -and $kind -ceq 'disarm-success')){ throw 'S3 trap: the disarm outcome does not follow the native check' }
 if($passed -and $e.trapAfter.active -ne $false){ throw 'S3 trap: a passed check left the trap active' }
 if(-not $passed -and $kind -ceq 'disarm-fail' -and $e.trapAfter.active -ne $true){ throw 'S3 trap: a failed check deactivated the trap' }
 if([int]$e.thievery.mountBase -ne 0){ throw 'S3 trap: the mount carried Trickery ranks' }
 if([int]$e.thievery.baseGranted -lt 1 -or [int]$e.thievery.baseGranted -gt [int]$Row.riderLevel){ throw 'S3 trap: the disposable rider rank grant exceeds the native rank cap' }
 if(-not (Test-KmcSprint3Prop $e 'trapAfter') -or [double]$e.trapAfter.distanceToRider -gt 2.5){ throw 'S3 trap: the rider acted beyond the native interaction distance' }
}
function Assert-KmcSprint3TouchRow($Row,[string]$Rider,[string]$Mount,[string]$Recipient,[bool]$Held,[bool]$Tb) {
 $e=$Row.evidence
 if([string]$e.cast.inputPath -cne 'ClickWithSelectedAbilityHandler.OnClick' -or $e.cast.clicked -ne $true -or $e.cast.canTarget -ne $true -or [string]$e.cast.resolvedTarget -cne $Recipient){ throw ($Row.name+': not one ordinary ability click on the recipient') }
 if([string]$e.cast.shell.executor -cne $Rider -or [string]$e.cast.ability.caster -cne $Rider){ throw ($Row.name+': the native cast shell is not the rider''s own') }
 if($e.cast.ability.blueprint -cne '5590652e1c2225c4ca30c4a699ab3649'){ throw ($Row.name+': the instrument is not Cure Light Wounds') }
 if(-not $Tb -and -not $Held -and [double]$e.cast.distanceMountToTarget -lt 9){ throw ($Row.name+': the recipient was not beyond the real-time cast radius') }
 if([int]$e.recipientWounded.damage -lt 1){ throw ($Row.name+': the recipient was not wounded before the heal') }
 Assert-KmcSprint3CarriedRow $Row $Rider $Mount
 $heals=@(Get-KmcSprint3Events $Row 'heal' | Where-Object { [string]$_.target -ceq $Recipient -and [string]$_.actor -ceq $Rider })
 if($heals.Count -ne 1){ throw ($Row.name+': exactly one native heal on the recipient was required (observed '+$heals.Count+')') }
 $casts=@(Get-KmcSprint3Events $Row 'cast-after' | Where-Object { [string]$_.actor -ceq $Rider })
 if($casts.Count -lt 2){ throw ($Row.name+': the native cast and its touch delivery were not both observed') }
 if(@($casts | Where-Object { $_.spellFailed -eq $true }).Count -ne 0){ throw ($Row.name+': a native cast failed') }
 $itemSpends=@(Get-KmcSprint3Events $Row 'item-spend-after' | Where-Object { $_.result -eq $true }).Count
 $expectedSpends=if([string]$e.instrument -ceq 'scroll-stack'){1}else{0}
 if($itemSpends -ne $expectedSpends){ throw ($Row.name+': scroll charge expenditure differs from one per cast ('+$itemSpends+')') }
 if([string]$e.instrument -ceq 'memorized-slot'){
  if($e.before.prepared -isnot [Array] -or $e.after.prepared -isnot [Array]){ throw ($Row.name+': prepared inventory missing') }
  $beforeAvail=@($e.before.prepared | Where-Object { $_.available -eq $true -and [string]$_.ability.blueprint -ceq '5590652e1c2225c4ca30c4a699ab3649' }).Count
  $afterAvail=@($e.after.prepared | Where-Object { $_.available -eq $true -and [string]$_.ability.blueprint -ceq '5590652e1c2225c4ca30c4a699ab3649' }).Count
  if($beforeAvail -ne $afterAvail+1){ throw ($Row.name+': the memorized slot was not spent exactly once ('+$beforeAvail+' -> '+$afterAvail+')') }
 }
 if($null -ne $e.after.heldTouch){ throw ($Row.name+': a held touch charge remained after delivery') }
 if($Held){
  if($e.stop.chargeHeldAfterStop -ne $true -or $e.stop.settled.heldTouch -eq $null){ throw 'S3 held: the native Stop did not retain the held charge' }
  if($e.redeliver.resolvedToHeldTouch -ne $true -or $e.redeliver.clicked -ne $true){ throw 'S3 held: the second click did not resolve to the held touch delivery' }
  if([int]$e.inputCount -ne 3){ throw 'S3 held: inputs differ from cast, Stop, re-click' }
 } elseif(-not $Tb) { if([int]$e.inputCount -ne 1){ throw ($Row.name+': more than one ordinary input was needed') } }
 if([int]$e.after.recipient.damage -ge [int]$e.recipientWounded.damage){ throw ($Row.name+': the recipient was not healed') }
}
function Assert-KmcSprint3HostileRow($Row,[string]$Rider,[string]$Mount,[string]$Hostile) {
 $e=$Row.evidence
 if($e.cast.clicked -ne $true -or $e.cast.canTarget -ne $true -or [string]$e.cast.resolvedTarget -cne $Hostile -or [string]$e.cast.shell.executor -cne $Rider){ throw 'S3 hostile: not one ordinary ability click on the hostile' }
 Assert-KmcSprint3CarriedRow $Row $Rider $Mount
 $rolls=@($e.rules.events | Where-Object { [string]$_.kind -ceq 'attack-roll' -and [string]$_.actor -ceq $Rider -and [string]$_.target -ceq $Hostile })
 if($rolls.Count -ne 1 -or [string]$rolls[0].attackType -cne 'Touch'){ throw 'S3 hostile: exactly one native touch attack roll by the rider was required' }
 $itemSpends=@(Get-KmcSprint3Events $Row 'item-spend-after' | Where-Object { $_.result -eq $true }).Count
 if($itemSpends -ne 1){ throw 'S3 hostile: the scroll charge was not spent exactly once' }
 $hit=[string]$rolls[0].result -cin @('Hit','CriticalHit')
 $effects=@(Get-KmcSprint3Events $Row 'heal')+@(Get-KmcSprint3Events $Row 'damage') | Where-Object { [string]$_.target -ceq $Hostile }
 if($hit -and @($effects).Count -ne 1){ throw 'S3 hostile: a hit did not deliver exactly one native effect' }
 if(-not $hit -and @($effects).Count -ne 0){ throw 'S3 hostile: a miss delivered an effect' }
 # Native touch delivery: a hit consumes the held charge; a legal miss keeps it held (nothing is manufactured or refunded).
 if($hit -and $null -ne $e.after.heldTouch){ throw 'S3 hostile: a held touch charge remained after a delivered touch attack' }
 if(-not $hit -and $null -eq $e.after.heldTouch){ throw 'S3 hostile: the held charge was lost on a native miss' }
}
function Assert-KmcSprint3RangedRow($Row,[string]$Rider,[string]$Mount,[string]$Hostile) {
 $e=$Row.evidence
 if($e.cast.clicked -ne $true -or [string]$e.cast.resolvedTarget -cne $Hostile -or [string]$e.cast.ability.blueprint -cne '9f10909f0be1f5141bf1c102041f93d9'){ throw 'S3 ranged: not one ordinary Snowball click on the hostile' }
 if([double]$e.hostileRetreat.distanceAfter -le [double]$e.rangeMetres){ throw 'S3 ranged: the hostile was not beyond the spell range' }
 Assert-KmcSprint3CarriedRow $Row $Rider $Mount
 $c=Get-KmcSprint3LastCarried $Row
 if($c.satisfied -ne $true -or [double]$c.distanceAtSatisfaction -gt [double]$e.rangeMetres+0.05 -or [double]$c.distanceAtSatisfaction -le 3){ throw 'S3 ranged: the approach did not stop at the spell''s own range' }
 $rolls=@($e.rules.events | Where-Object { [string]$_.kind -ceq 'attack-roll' -and [string]$_.actor -ceq $Rider -and [string]$_.target -ceq $Hostile })
 if($rolls.Count -ne 1){ throw 'S3 ranged: exactly one native ranged attack roll was required' }
 $casts=@(Get-KmcSprint3Events $Row 'cast-after' | Where-Object { [string]$_.actor -ceq $Rider })
 if($casts.Count -ne 1){ throw 'S3 ranged: exactly one native cast was required' }
}
function Assert-KmcSprint3TurnBasedRow($Row,[string]$Rider,[string]$Mount,[string]$Recipient,[bool]$Insufficient) {
 Assert-KmcSprint3TouchRow $Row $Rider $Mount $Recipient $false $true
 $e=$Row.evidence
 if([string]$e.rowTurnActor -cne $Rider){ throw ($Row.name+': the cast was not issued on the rider''s own turn') }
 if([string]$e.cast.before.turnActor -cne $Rider -or $e.cast.before.riderHasStandard -ne $true){ throw ($Row.name+': the rider did not hold its Standard action at the input') }
 $costs=@($e.costEvents | Where-Object { [string]$_.boundary -ceq 'cost-after' -and [string]$_.state.actor -ceq $Rider })
 if($costs.Count -lt 1){ throw ($Row.name+': the rider''s native Standard cost was not observed') }
 if($Insufficient){
  if($e.summary.nativeApproachEndSignalled -ne $true -or $e.summary.secondInput -ne $true){ throw 'S3 tb insufficient: the native approach-end rule and the next-turn re-delivery were not both observed' }
  if($e.secondTurn.heldCharge -ne $true){ throw 'S3 tb insufficient: the held charge did not survive the turn boundary' }
  if($e.redeliver.resolvedToHeldTouch -ne $true){ throw 'S3 tb insufficient: the next-turn click did not resolve to the held touch' }
  if([double]$e.recipientPlacement.distanceAfter -le [double]$e.allowanceMetres){ throw 'S3 tb insufficient: the recipient was within one turn''s pair movement' }
  if(-not (Test-KmcSprint3Prop $e 'firstTurnEnd') -or $null -eq $e.firstTurnEnd.heldTouch){ throw 'S3 tb insufficient: the charge was not held at the first turn end' }
 } else {
  if([int]$e.inputCount -ne 1){ throw 'S3 tb friendly: more than one ordinary input was needed' }
  if([double]$e.cast.distanceMountToTarget -lt 5){ throw 'S3 tb friendly: the recipient was not beyond touch range' }
 }
}
function Assert-KmcSprint3ApproachEvidence {
 param([Parameter(Mandatory=$true)]$Request,[Parameter(Mandatory=$true)]$Artifact,[AllowNull()][string]$Status)
 if($Request.scenario -cnotin @('sprint3-approach-rt','sprint3-approach-tb') -or $Artifact.schemaVersion -ne 47){ throw 'Sprint 3 scenario/schema mismatch' }
 if($Artifact.status -ceq 'FAIL') {
  if($Status -ceq 'PASS' -or (@($Artifact.errors).Count -eq 0 -and $Artifact.subscenarioFailCount -lt 1)){ throw 'Sprint 3 incomplete observation cannot become PASS' }
  return # Preserve the producer's explicit failed prefix; grant no qualification.
 }
 if($Artifact.status -cne 'PASS' -or @($Artifact.errors).Count -ne 0 -or @($Artifact.rows | Where-Object status -CNE 'PASS').Count -ne 0 -or $Artifact.subscenarioFailCount -ne 0 -or $Artifact.subscenarioPassCount -ne @($Artifact.rows).Count){ throw 'Sprint 3 PASS envelope is not internally consistent' }
 $o=$Artifact.observations.sprint3Approach
 if($null -eq $o -or $o.contract -cne 'native-mounted-carried-approach-v1' -or $o.rider -cne $Artifact.observations.riderId -or $o.mount -cne $Artifact.observations.horseId -or $o.mountBlueprint -cne 'e7aa96d15a45238438ae4cfb476f6bb9'){ throw 'Sprint 3 observation identity differs' }
 $tb=$Request.scenario.EndsWith('-tb')
 if($o.mode -cne $(if($tb){'TB'}else{'RT'}) -or $o.mounted -ne $true){ throw 'Sprint 3 mode/surface differs' }
 $expected=@(Get-KmcSprint3ApproachCases $Request.scenario)
 if(($o.cases | ConvertTo-Json -Compress) -cne ($expected | ConvertTo-Json -Compress)){ throw 'Sprint 3 registered row order differs' }
 $recipient=if(Test-KmcSprint3Prop $o 'recipient'){ [string]$o.recipient.id } else { $null }
 foreach($name in $expected){
  $rows=@($Artifact.rows | Where-Object name -CEQ $name); if($rows.Count -ne 1){ throw ('Sprint 3 required row missing or duplicated: '+$name) }
  $row=$rows[0]; $row | Add-Member -NotePropertyName riderLevel -NotePropertyValue ([int]$o.riderLevel) -Force
  $hostile=if(Test-KmcSprint3Prop $row.evidence.after 'hostile'){ [string]$row.evidence.after.hostile.id } else { $null }
  switch -CaseSensitive ($name) {
   'S3-trap-disarm' { Assert-KmcSprint3TrapRow $row $o.rider $o.mount }
   'S3-touch-friendly-distant' { Assert-KmcSprint3TouchRow $row $o.rider $o.mount $recipient $false $false }
   'S3-touch-held-charge' { Assert-KmcSprint3TouchRow $row $o.rider $o.mount $recipient $true $false }
   'S3-touch-hostile' { Assert-KmcSprint3HostileRow $row $o.rider $o.mount $hostile }
   'S3-ranged-distant' { Assert-KmcSprint3RangedRow $row $o.rider $o.mount $hostile }
   'S3-tb-touch-friendly' { Assert-KmcSprint3TurnBasedRow $row $o.rider $o.mount $recipient $false }
   'S3-tb-insufficient-movement' { Assert-KmcSprint3TurnBasedRow $row $o.rider $o.mount $recipient $true }
  }
 }
 if($o.castTrace.faults -ne 0 -or $o.castTrace.dropped -ne 0 -or $o.castTrace.identityRegistry.faults -ne 0 -or $o.castTrace.identityRegistry.released -ne $true){ throw 'Sprint 3 casting trace is not clean' }
 if($o.final.riderCommandsEmpty -ne $true -or $o.final.mountCommandsEmpty -ne $true -or $o.final.processesSettled -ne $true -or $o.final.nativeAbilitiesPending -ne $false -or $o.final.projectilesPending -ne $false){ throw 'Sprint 3 final native state is not settled' }
 foreach($item in @($o.items)){ if($item.disposed -ne $true -or $item.slotRestored -ne $true -or $item.noOwnedItemResident -ne $true){ throw 'Sprint 3 disposable native item ownership remains' } }
 if([int]$o.riderThieveryBaseAfter -ne [int]$o.riderThieveryBaseBefore){ throw 'Sprint 3 rider Trickery fixture rank was not restored' }
 if($Status -ceq 'PASS' -and $Artifact.status -cne 'PASS'){ throw 'Sprint 3 failed native observation cannot be promoted' }
}
