# Sprint 7 (CHARGE-01) external reader. The rows only record native observations; PASS is decided here.
function Get-KmcSprint7Cases([string]$Scenario){
 if($Scenario -ceq 'sprint7-charge-rt'){ return @('S7-rt-charge','S7-rt-cancel') }
 if($Scenario -ceq 'sprint7-charge-pounce-rt'){ return @('S7-pounce') }
 if($Scenario -ceq 'sprint7-charge-queue-rt'){ return @('S7-queue','S7-lane') }
 if($Scenario -ceq 'sprint7-charge-reforce-tb'){ return @('S7-reforce') }
 if($Scenario -cin @('sprint7-charge-tb','sprint7-charge-tb-mount-first')){ return @('S7-tb-charge') }
 throw ('Sprint 7 scenario has no case list: '+$Scenario)
}
function Get-KmcSprint7RiderAttacks($Measure,$Hostile){ @($Measure.attackRules | Where-Object { [string]$_.attacker -ceq [string]$Measure.rider -and [string]$_.target -ceq $Hostile }) }
function Assert-KmcSprint7Refused($Click,[string[]]$Codes,[string]$What){
 if($null -eq $Click -or $Click.clicked -ne $false -or $Click.policyAllowed -ne $false -or [int]$Click.castDelta -ne 0 -or $Click.shell -ne $false){ throw ('Sprint 7 '+$What+' was not refused before admission') }
 if($Codes.Count -gt 0 -and [string]$Click.policyCode -cnotin $Codes){ throw ('Sprint 7 '+$What+' was refused for another reason: '+$Click.policyReason) }
 if([string]::IsNullOrWhiteSpace([string]$Click.policyReason)){ throw ('Sprint 7 '+$What+' refused with no reason') }
 foreach($field in 'riderStandard','riderMove','riderSwift','mountStandard','mountMove','mountSwift'){ if([double]$Click.afterClick.$field -gt [double]$Click.before.$field+0.001){ throw ('Sprint 7 '+$What+' spent '+$field) } }
}
function Assert-KmcSprint7Admitted($Click,[string]$What){
 if($null -eq $Click -or $Click.abilityPresent -ne $true -or $Click.clicked -ne $true -or $Click.policyAllowed -ne $true -or [int]$Click.castDelta -ne 1 -or [int]$Click.refusalDelta -ne 0 -or $Click.shell -ne $true){ throw ('Sprint 7 '+$What+' did not enter through the ordinary ability click') }
}
function Assert-KmcSprint7Lease($Terminal,[string]$What){
 $l=$Terminal.lease
 if($null -eq $l -or $l.applied -ne $true -or $l.buffApplied -ne $true -or $l.forcedPathOutstanding -ne $false -or $l.riderAgentTouched -ne $false){ throw ('Sprint 7 '+$What+': the lease did not own the mover only') }
 if($l.restored -ne $true -or $l.chargingRestoredExactly -ne $true -or $l.speedOverrideRestoredExactly -ne $true -or $l.riderChargingRestoredExactly -ne $true){ throw ('Sprint 7 '+$What+': an owned charge effect was not returned') }
 $s=$Terminal.state
 if($s.chargeOwned -ne $false -or $s.riderCharging -ne $false -or $s.mountCharging -ne $false -or $s.mounted -ne $true){ throw ('Sprint 7 '+$What+': the pair did not settle free and mounted') }
}
function Assert-KmcSprint7Delivered($Facts,$Rules,$Click,[string]$What){
 $t=$Facts.terminal
 Assert-KmcSprint7Lease $t $What
 if($t.sequenceLawful -ne $true -or [string]$t.result -cne 'Success' -or $t.revalidationFailed -eq $true){ throw ('Sprint 7 '+$What+': the charge did not complete its lawful sequence: '+$t.revalidationReason) }
 if($Rules.Count -ne 1){ throw ('Sprint 7 '+$What+': one attack was expected, observed '+$Rules.Count) }
 if([int]$t.mountRules -ne 0){ throw ('Sprint 7 '+$What+': the mount attacked during the charge') }
 if($Rules[0].charge -ne $true -or $Rules[0].opportunity -eq $true){ throw ('Sprint 7 '+$What+': the attack was not a native charge attack') }
 if($Rules[0].resultAtEvent -cin @('Hit','CriticalHit') -and [int]$Rules[0].damageRules -lt 1){ throw ('Sprint 7 '+$What+': a hit reached no native damage') }
 # One travel on the mount: real displacement to the rider's reach, at the doubled native speed.
 $reach=[double]$t.mountCorpulence+[double]$t.targetCorpulence+[double]$t.riderWeaponRange
 if([double]$t.closestDistance -gt $reach+0.35){ throw ('Sprint 7 '+$What+': the pair stopped outside the rider''s reach') }
 if([double]$t.closestDistance -lt [double]$t.mountCorpulence+[double]$t.targetCorpulence-0.35){ throw ('Sprint 7 '+$What+': the pair overran into the target') }
 if([double]$Click.before.distance -le $reach -or [double]$t.state.distance -gt $reach+0.35){ throw ('Sprint 7 '+$What+': no real travel into reach') }
 if([double]$t.peakSpeedOverride -lt 2*[double]$t.mountCombatSpeed-0.05){ throw ('Sprint 7 '+$What+': the mount did not carry at the native charge speed') }
 # The native penalty: on during travel, kept after delivery, exactly one buff.
 $w=$Facts.whileCharging
 if($null -eq $w -or $w.buff -ne $true -or [int]$w.buffCount -ne 1 -or $w.riderCharging -ne $true -or [double]$w.riderAc -ge [double]$Click.before.riderAc){ throw ('Sprint 7 '+$What+': the native modifiers were not on the rider during travel') }
 if([double]$w.mountAc -ne [double]$Click.before.mountAc){ throw ('Sprint 7 '+$What+': the mount took the rider''s modifier') }
 if($t.lease.buffHandedToNative -ne $true -or $t.lease.buffOutstanding -ne $false -or $t.state.buff -ne $true -or [int]$t.state.buffCount -ne 1 -or [double]$t.state.riderAc -ge [double]$Click.before.riderAc){ throw ('Sprint 7 '+$What+': the delivered charge did not keep its native penalty for its round') }
}
function Assert-KmcSprint7Cancelled($Terminal,$Before,[string]$What){
 Assert-KmcSprint7Lease $Terminal $What
 if([string]$Terminal.result -cne 'Interrupt' -or [int]$Terminal.rules -gt 0 -and $Terminal.lease.buffHandedToNative -eq $true){ throw ('Sprint 7 '+$What+': an interrupted charge kept delivering') }
 if($Terminal.lease.buffHandedToNative -ne $false -or $Terminal.lease.buffOutstanding -ne $false -or $Terminal.state.buff -ne $false -or [double]$Terminal.state.riderAc -ne [double]$Before.riderAc){ throw ('Sprint 7 '+$What+': the undelivered charge left its effect') }
}
function Assert-KmcSprint7ChargeEvidence {
 param([Parameter(Mandatory=$true)]$Request,[Parameter(Mandatory=$true)]$Artifact,[AllowNull()][string]$Status,[string]$EvidenceRoot)
 if($Request.scenario -cnotin @('sprint7-charge-rt','sprint7-charge-tb','sprint7-charge-tb-mount-first','sprint7-charge-pounce-rt','sprint7-charge-queue-rt','sprint7-charge-reforce-tb') -or $Artifact.schemaVersion -ne 51){ throw 'Sprint 7 scenario/schema mismatch' }
 if($Artifact.status -ceq 'FAIL') {
  if($Status -ceq 'PASS' -or (@($Artifact.errors).Count -eq 0 -and $Artifact.subscenarioFailCount -lt 1)){ throw 'Sprint 7 incomplete observation cannot become PASS' }
  return # Preserve the producer's explicit failed prefix; grant no qualification.
 }
 if($Artifact.status -cne 'PASS' -or @($Artifact.errors).Count -ne 0 -or @($Artifact.rows | Where-Object status -CNE 'PASS').Count -ne 0 -or $Artifact.subscenarioFailCount -ne 0){ throw 'Sprint 7 envelope differs' }
 $o=$Artifact.observations.sprint7Charge
 if($null -eq $o -or $o.contract -cne 'native-mounted-charge-v1' -or $o.rider -cne $Artifact.observations.riderId -or $o.mount -cne $Artifact.observations.horseId){ throw 'Sprint 7 measurement identity differs' }
 if($o.defaultFeatureEnabled -ne $true){ throw 'Sprint 7 the rows did not run under the ordinary default' }
 $expected=@(Get-KmcSprint7Cases $Request.scenario)
 if(($o.cases | ConvertTo-Json -Compress) -cne ($expected | ConvertTo-Json -Compress)){ throw 'Sprint 7 registered row order differs' }
 $rows=@{}
 foreach($name in $expected){ $found=@($Artifact.rows | Where-Object name -CEQ $name); if($found.Count -ne 1){ throw ('Sprint 7 required row missing or duplicated: '+$name) }; $rows[$name]=$found[0] }
 if($Request.scenario -ceq 'sprint7-charge-queue-rt'){
  $q=$rows['S7-queue'].evidence
  Assert-KmcSprint7Admitted $q.steps.followCharge 'charge with a follow-up'
  foreach($n in 'afterRejected','afterMountEnqueue','afterMountStandardEnqueue','afterFollowUp'){ $s=$q.$n; if($s.sameCommand -ne $true -or $s.finished -ne $false -or [int]$s.cleanupAttempts -ne [int]$q.beforeRequests.cleanupAttempts -or [int]$s.forcedPaths -ne [int]$q.beforeRequests.forcedPaths -or [double]$s.riderStandard -gt [double]$q.beforeRequests.riderStandard){ throw ('Sprint 7 '+$n+' changed the live charge') } }
  if([int]$q.afterMountEnqueue.mountQueue -ne 0 -or [int]$q.afterMountStandardEnqueue.mountQueue -ne 0){ throw 'Sprint 7 the mount enqueue was accepted' }
  if($q.followUpPending -ne $true){ throw 'Sprint 7 the follow-up did not wait in the queue' }
  Assert-KmcSprint7Lease $q.followTerminal 'charge with a follow-up'
  if([string]$q.followTerminal.result -cne 'Success' -or [int]$q.followTerminal.rules -lt 1 -or $q.followTerminal.lease.buffHandedToNative -ne $true){ throw 'Sprint 7 the follow-up cost the charge its delivery' }
  if($q.followUpRan.promoted -ne $true -or $q.followUpRan.inQueue -ne $false){ throw 'Sprint 7 the follow-up did not become active after the charge' }
  Assert-KmcSprint7Admitted $q.steps.replaceCharge 'charge with a replacement'
  Assert-KmcSprint7Cancelled $q.replaceTerminal $q.steps.replaceCharge.before 'replaced charge'
  if([double]$q.replaceTerminal.state.riderStandard -le 0 -or [int]$q.replaceTerminal.rules -ne 0){ throw 'Sprint 7 the replacement refunded or delivered' }
  if([double]$q.successorMetres -lt 1.0){ throw 'Sprint 7 cleanup stopped the order that replaced the charge' }
  $l=$rows['S7-lane'].evidence
  Assert-KmcSprint7Lease $l.validTerminal 'charge with a moved target'
  if([string]$l.validTerminal.result -cne 'Success' -or [int]$l.validTerminal.rules -ne 1 -or [int]$l.validTerminal.lease.forcedPathCount -le [int]$l.validBefore.forcedPaths){ throw 'Sprint 7 a target moved to a clear lane was not followed' }
  Assert-KmcSprint7Admitted $l.steps.blockedCharge 'charge toward a changed lane'
  if($l.flankBlocked.none -ne $true){
   $b=$l.blockedTerminal
   Assert-KmcSprint7Cancelled $b $l.steps.blockedCharge.before 'flank-blocked lane'
   if([string]$b.revalidationReason -cnotmatch 'clears the mount' -or [int]$b.rules -ne 0 -or [double]$b.state.riderStandard -le 0 -or [int]$b.lease.forcedPathCount -ne [int]$l.flankBlocked.identity.forcedPaths){ throw 'Sprint 7 a flank-blocked lane was forced or delivered' }
  }
  return
 }
 if($Request.scenario -ceq 'sprint7-charge-reforce-tb'){
  $f=$rows['S7-reforce'].evidence
  Assert-KmcSprint7Admitted $f.steps.stoppedCharge 'stopped-path charge'
  $s=$f.stoppedTerminal
  Assert-KmcSprint7Lease $s 'stopped-path charge'
  if([string]$s.result -cne 'Success' -or [int]$s.rules -ne 1 -or $f.stoppedSameTurn -ne $true -or [int]$s.lease.forcedPathCount -le [int]$f.stoppedBefore.forcedPaths){ throw 'Sprint 7 a stopped lawful line was not forced again and delivered' }
  Assert-KmcSprint7Admitted $f.steps.blockedCharge 'turn-based changed lane'
  $b=$f.blockedTerminal
  if($f.flankBlocked.none -ne $true){
   Assert-KmcSprint7Cancelled $b $f.steps.blockedCharge.before 'turn-based flank-blocked lane'
   if([string]$b.revalidationReason -cnotmatch 'clears the mount' -or [int]$b.lease.forcedPathCount -ne [int]$f.flankBlocked.identity.forcedPaths){ throw 'Sprint 7 a turn-based flank-blocked lane was forced' }
  }
  if([int]$b.rules -ne 0 -or [double]$b.state.riderStandard -lt 5.9 -or [double]$b.state.riderMove -lt 2.9 -or $f.blockedSameTurn -ne $true){ throw 'Sprint 7 the cancelled turn-based charge delivered or refunded' }
  if([double]$f.steps.nextTurn.riderStandard -ne 0 -or $f.steps.nextTurn.turnUnit -cne $o.rider){ throw 'Sprint 7 the cancelled charge held the turn' }
  return
 }
 if($Request.scenario -ceq 'sprint7-charge-pounce-rt'){
  $f=$rows['S7-pounce'].evidence
  Assert-KmcSprint7Admitted $f.steps.mountPounce 'charge with a Pouncing mount'
  Assert-KmcSprint7Admitted $f.steps.riderPounce 'charge with a Pouncing rider'
  $m=$f.mountPounceAttack;$r=$f.riderPounceAttack
  if($m.mountPounce -ne $true -or $m.riderPounce -ne $false -or $r.riderPounce -ne $true -or $r.mountPounce -ne $false){ throw 'Sprint 7 the Pounce control did not hold its setup' }
  if($m.fullAttack -ne $false -or [int]$m.planned -ne 1 -or [int]$f.mountPounceTerminal.rules -ne 1 -or [string]$f.mountPounceTerminal.result -cne 'Success'){ throw 'Sprint 7 the mount''s Pounce transferred to the rider' }
  if($r.fullAttack -ne $true -or [int]$f.riderPounceTerminal.rules -ne [int]$r.planned -or [string]$f.riderPounceTerminal.result -cne 'Success'){ throw 'Sprint 7 the rider''s own Pounce was not honoured' }
  return
 }
 if($Request.scenario -ceq 'sprint7-charge-rt'){
  $f=$rows['S7-rt-charge'].evidence; $d=$f.steps; $hostile=[string]$f.steps.charge.shellTarget
  Assert-KmcSprint7Refused $d.tooNear @('OutsideSupportedRange') 'too-near charge'
  if($f.obstruction.none -ne $true){ Assert-KmcSprint7Refused $d.obstructed @('NoPath','TargetNotVisible') 'obstructed charge' }
  Assert-KmcSprint7Admitted $d.charge 'real-time charge'
  $rules=@(Get-KmcSprint7RiderAttacks $o $hostile | Where-Object { $_.charge -eq $true })
  Assert-KmcSprint7Delivered $f $rules $d.charge 'real-time charge'
  # The forced die, for the native critical branch: this attack alone.
  if($o.attackD20IsForced -ne $true -or [int]$f.forcedDice -lt 1 -or [int]$rules[0].d20 -ne 20 -or $rules[0].criticalAtEvent -ne $true -or [int]$rules[0].damageRules -lt 1){ throw 'Sprint 7 the natural 20 did not take the native critical branch' }
  if([double]$f.terminal.state.riderStandard -le 0 -or [double]$f.terminal.state.mountStandard -gt [double]$d.charge.before.mountStandard+0.001){ throw 'Sprint 7 real-time cost did not stay with the rider' }
  Assert-KmcSprint7Refused $d.spentRefused @() 'charge while spent'
  if($d.buffExpired.buff -ne $false -or [double]$d.buffExpired.riderAc -ne [double]$d.charge.before.riderAc){ throw 'Sprint 7 the native penalty did not end with its round' }
  $ordinary=$f.ordinaryRule
  if($null -eq $ordinary -or $ordinary.charge -ne $false -or [string]$ordinary.attacker -cne [string]$o.rider){ throw 'Sprint 7 the ordinary attack neighbour differs' }
  if(([int]$rules[0].bonus-[int]$ordinary.bonus) -ne 2){ throw ('Sprint 7 the charge bonus is not +2 against the ordinary attack: '+$rules[0].bonus+' versus '+$ordinary.bonus) }
  $c=$rows['S7-rt-cancel'].evidence; $s=$c.steps
  Assert-KmcSprint7Admitted $s.stopCharge 'Stop control'
  Assert-KmcSprint7Cancelled $c.stopTerminal $s.stopCharge.before 'Stop control'
  if([double]$c.stopTerminal.state.riderStandard -le 0){ throw 'Sprint 7 Stop refunded the committed action' }
  Assert-KmcSprint7Admitted $s.deathCharge 'target-loss control'
  # The native life controller settles the flags after the rule; the command's own terminal names the loss.
  if([string]$c.deathTerminal.outcomeRejection -cnotmatch 'target became invalid'){ throw 'Sprint 7 the target-loss control did not lose its target' }
  Assert-KmcSprint7Cancelled $c.deathTerminal $s.deathCharge.before 'target-loss control'
  if([double]$c.deathTerminal.state.riderStandard -le 0){ throw 'Sprint 7 target loss refunded the committed action' }
 } else {
  $f=$rows['S7-tb-charge'].evidence; $d=$f.steps; $hostile=[string]$d.charge.shellTarget
  $riderFirst=$Request.scenario -ceq 'sprint7-charge-tb'
  if($o.order -cne $(if($riderFirst){'rider-first'}else{'mount-first'})){ throw 'Sprint 7 initiative arrangement differs from the scenario' }
  if($riderFirst -ne ([double]$f.order.riderInitiative -gt [double]$f.order.mountInitiative)){ throw 'Sprint 7 native initiative did not produce the arrangement' }
  Assert-KmcSprint7Refused $d.tooNear @('OutsideSupportedRange') 'too-near charge on the turn'
  Assert-KmcSprint7Admitted $d.charge 'turn-based charge'
  $rules=@(Get-KmcSprint7RiderAttacks $o $hostile | Where-Object { $_.charge -eq $true })
  if($rules.Count -ne 2){ throw ('Sprint 7 two charges were expected to attack once each, observed '+$rules.Count) }
  Assert-KmcSprint7Delivered $f @($rules[0]) $d.charge 'turn-based charge'
  Assert-KmcSprint7Admitted $d.secondCharge 'next round''s charge'
  Assert-KmcSprint7Lease $f.secondTerminal 'next round''s charge'
  if([string]$f.secondTerminal.result -cne 'Success' -or [int]$f.secondTerminal.rules -ne 1 -or [int]$f.secondTerminal.state.buffCount -ne 1){ throw 'Sprint 7 the next round''s charge did not deliver once with one native penalty' }
  if($o.attackD20IsForced -eq $true){ throw 'Sprint 7 the turn-based charge used a forced die' }
  $b=$d.charge.before; $a=$f.terminal.state
  if($b.turnUnit -cne $o.rider -or $f.sameTurn -ne $true -or [string]$a.activation -cne [string]$b.activation -or [int]$a.activationSequence -ne [int]$b.activationSequence -or [int]$a.round -ne [int]$b.round){ throw 'Sprint 7 the charge minted another activation or left its turn' }
  if([double]$b.riderStandard -ne 0 -or [double]$b.riderMove -ne 0 -or [double]$b.mountMove -ne 0 -or [double]$b.mountStandard -ne 0){ throw 'Sprint 7 the charge did not start from unspent actions' }
  if([double]$a.riderStandard -lt 5.9 -or [double]$a.riderMove -lt 2.9){ throw 'Sprint 7 the rider did not pay the native full round' }
  if([double]$a.mountMove -le 0 -or [double]$a.mountMove -gt 3.05 -or [double]$a.partnerTimeForced -le 0){ throw 'Sprint 7 the mount did not pay its own travel' }
  if([double]$a.mountStandard -ne 0 -or $a.partnerCanMove -ne $false){ throw 'Sprint 7 the carried charge took the mount''s action or left it more movement' }
  Assert-KmcSprint7Refused $d.spentRefused @('WrongActionState') 'second charge on the turn'
  foreach($rule in @($f.mountOwnAttackRules)){ if($rule.charge -ne $false -or [string]$rule.attacker -cne [string]$o.mount){ throw 'Sprint 7 the mount''s own attack was not its own ordinary attack' } }
  $n=$d.nextTurn
  if([int]$n.activationSequence -ne [int]$b.activationSequence+1 -or $n.turnUnit -cne $o.rider){ throw 'Sprint 7 the next activation is not the next native paired turn' }
  if([double]$n.riderStandard -ne 0 -or [double]$n.riderMove -ne 0){ throw 'Sprint 7 the next native preparation did not renew the rider' }
  # Native buffs tick at turn start with game time frozen inside a turn, so the last moment of a round can reach this turn: never more.
  if($n.buff -eq $true){ if([double]$n.buffSecondsLeft -gt 1.0 -or [int]$n.buffCount -ne 1){ throw 'Sprint 7 the native penalty outlived its round' } }
  elseif([double]$n.riderAc -ne [double]$b.riderAc){ throw 'Sprint 7 the penalty outlived its buff' }
  Assert-KmcSprint7Refused $d.partnerSpentRefused @('WrongActionState') 'charge with a spent partner'
  if([double]$d.partnerSpentRefused.before.mountStandard -le 0 -or [double]$d.partnerSpentRefused.afterClick.mountStandard -lt [double]$d.partnerSpentRefused.before.mountStandard-0.05){ throw 'Sprint 7 the spent partner was reset' }
 }
}
# The cold half: the delivered charge's cost and native penalty survive, nothing resumes, and the next native turn renews.
function Assert-KmcSprint7ChargeContinuation($Value,$Ids){
 $noPrepare=@($Value.continuation|Where-Object{$_.PSObject.Properties['label']-and$_.label-ceq'saved-charge-no-prepare'})
 $before=@($Value.continuation|Where-Object{$_.PSObject.Properties['label']-and$_.label-ceq'continuation-before-input'})
 if($noPrepare.Count-ne1-or$before.Count-ne1){throw 'Sprint 7 the restored charge turn has no boundary observation'}
 if($noPrepare[0].currentActor-cne$Ids[0]){throw 'Sprint 7 the restored turn is not the rider''s'}
 foreach($id in $Ids){$a=@($before[0].grants|Where-Object actorId -CEQ $id);$b=@($noPrepare[0].grants|Where-Object actorId -CEQ $id);if($a.Count-ne1-or$b.Count-ne1-or$a[0].count-ne$b[0].count){throw 'Sprint 7 the restored charge manufactured a preparation'}}
}
# Sprint 9: the feat's half of the integrated save. The charge half is Assert-KmcSprint7ChargePersistence, unchanged.
function Assert-KmcSprint9IntegratedPersistence($Observations,[bool]$Cold){
 if(-not $Cold){
  $d=$Observations.featDebt; $s=$Observations.featAtSave
  if($null -eq $d -or $d.commitment -cne 'BorrowNextSwift' -or $d.state.spent -ne $true -or $d.state.owed -ne $true -or $d.riderTurn -ne $false){ throw 'Sprint 9 the charge did not follow a committed off-turn borrow' }
  if($null -eq $s -or $s.afterPreparation.spent -ne $false -or $s.afterPreparation.owed -ne $false -or $s.afterPreparation.hasSwift -ne $false -or [double]$s.afterPreparation.swift -le 0){ throw 'Sprint 9 the debt was not paid at the preparation before the charge' }
  return
 }
 $c=$Observations.featCold; $n=$Observations.featRenewal
 if($null -eq $c -or $c.hasFeat -ne $true -or $c.preferenceOn -ne $true -or $c.state.spent -ne $false -or $c.state.owed -ne $false -or [double]$c.swift -le 0){ throw 'Sprint 9 the cold load changed the paid debt or refunded the Swift action' }
 if($null -eq $n -or [int]$n.chargesAtPreparation -ne 0 -or $n.afterPreparation.owed -ne $false -or $n.afterPreparation.hasSwift -ne $true){ throw 'Sprint 9 the paid debt was charged again at the next preparation' }
 if($n.commitment -cne 'BorrowNextSwift' -or $n.attempted -ne $true -or $n.riderTurn -ne $false -or $n.stateAfter.owed -ne $true){ throw 'Sprint 9 no legal reaction followed the integrated load' }
}
function Assert-KmcSprint7ChargePersistence($Observations,$Archive,[bool]$Cold){
 if(-not $Cold){
  $c=$Observations.chargeCommit
  if($null -eq $c -or $c.clicked -ne $true -or [string]$c.result -cne 'Success' -or $c.sequenceLawful -ne $true -or $c.lease.buffHandedToNative -ne $true){ throw 'Sprint 7 the save did not follow a delivered ordinary charge' }
  $charges=@($c.rules | Where-Object { $_.charge -eq $true })
  if($charges.Count -ne 1){ throw ('Sprint 7 the save followed '+$charges.Count+' charge attacks') }
  $a=$c.after
  if([double]$a.riderStandard -lt 5.9 -or [double]$a.riderMove -lt 2.9 -or [double]$a.mountStandard -ne 0 -or [double]$a.mountMove -le 0 -or [int]$a.buffCount -ne 1 -or $a.chargeOwned -ne $false -or $a.riderCharging -ne $false){ throw 'Sprint 7 the saved state is not a committed charge' }
  return
 }
 $s=$Observations.chargeCold.state
 if($Observations.chargeCold.hasShell -ne $false -or $s.chargeOwned -ne $false -or $s.activeCommand -ne $false -or $s.riderCharging -ne $false -or $s.mountCharging -ne $false){ throw 'Sprint 7 the cold load resumed a charge' }
 if([int]$s.buffCount -ne 1){ throw 'Sprint 7 the cold load lost or duplicated the native penalty' }
 if([double]$s.riderStandard -lt 5.9 -or [double]$s.riderMove -lt 2.9 -or [double]$s.mountMove -le 0){ throw 'Sprint 7 the cold load refunded committed actions' }
 if($s.available -ne $false){ throw 'Sprint 7 the cold load offered a free charge' }
 $n=$Observations.chargeRenewal
 if($null -eq $n -or [int]$n.castRequests -ne [int]$Observations.chargeCold.castRequests -or @($n.rulesAfterLoad | Where-Object { $_.charge -eq $true }).Count -ne 0){ throw 'Sprint 7 the cold load delivered a duplicate charge' }
 if([double]$n.state.riderStandard -ne 0 -or [double]$n.state.riderMove -ne 0 -or $n.state.turnUnit -cne $s.turnUnit){ throw 'Sprint 7 the next native turn did not renew the rider' }
}
