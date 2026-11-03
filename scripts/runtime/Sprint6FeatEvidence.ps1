# Sprint 6 (FEAT-01) external reader. The rows only record native observations; PASS is decided here.
function Get-KmcSprint6Cases([string]$Scenario){
 if($Scenario -ceq 'sprint6-feat-rt'){ return @('S6-selection','S6-rt-defense','S6-rt-isolation') }
 if($Scenario -ceq 'sprint6-feat-tb'){ return @('S6-rider-feat','S6-tb-economy') }
 throw ('Sprint 6 scenario has no case list: '+$Scenario)
}
function Get-KmcSprint6Record($Measure,[string]$Case,[int]$Step){
 $found=@($Measure.records | Where-Object { [string]$_.case -ceq $Case -and [int]$_.step -eq $Step })
 if($found.Count -lt 1){ throw ('Sprint 6 record missing: '+$Case+' step '+$Step) }
 $found[0]
}
function Get-KmcSprint6Rule($Measure,$Record){
 $found=@($Measure.attackRules | Where-Object { [string]$_.identity -ceq [string]$Record.ruleIdentity })
 if($found.Count -ne 1){ throw ('Sprint 6 record has no single observed attack rule: '+$Record.case+' step '+$Record.step) }
 $found[0]
}
function Assert-KmcSprint6Negated($Measure,$R,[string]$What){
 if($R.attempted -ne $true -or $R.negated -ne $true -or [string]$R.refusal -cne 'None' -or [int]$R.checkResult -le [int]$R.attackResult){ throw ('Sprint 6 '+$What+' did not negate by a greater check') }
 if($R.resultBefore -cnotin @('Hit','CriticalHit') -or $R.resultAfter -cne 'Parried' -or $R.criticalAfter -ne $false){ throw ('Sprint 6 '+$What+' did not become the native negated result') }
 $rule=Get-KmcSprint6Rule $Measure $R
 if($rule.resultAtEvent -cne 'Parried' -or $rule.isHitAtEvent -ne $false -or $rule.resultAfterRule -cne 'Parried' -or $rule.sneakAtEvent -ne $false -or [int]$rule.damageRules -ne 0 -or [int]$rule.damage -ne 0){ throw ('Sprint 6 '+$What+': a later consumer saw a hit or damage') }
}
function Assert-KmcSprint6Stands($Measure,$R,[string]$What){
 if($R.negated -ne $false -or $R.resultAfter -cne $R.resultBefore -or $R.criticalAfter -ne $R.criticalBefore){ throw ('Sprint 6 '+$What+' changed the attack it should leave intact') }
 $rule=Get-KmcSprint6Rule $Measure $R
 if($rule.isHitAtEvent -ne $true -or [int]$rule.damageRules -lt 1){ throw ('Sprint 6 '+$What+': the standing hit did not reach native damage') }
}
function Assert-KmcSprint6Refused($Measure,$R,[string]$Refusal,[string]$What){
 if([string]$R.refusal -cne $Refusal -or $R.attempted -ne $false -or [int]$R.checkResult -ne 0){ throw ('Sprint 6 '+$What+' expected refusal '+$Refusal+', observed '+$R.refusal) }
 Assert-KmcSprint6Stands $Measure $R $What
 if(($R.stateBefore | ConvertTo-Json -Compress) -cne ($R.stateAfter | ConvertTo-Json -Compress) -or [double]$R.swiftAfter -ne [double]$R.swiftBefore){ throw ('Sprint 6 '+$What+': a refusal changed the rider''s state') }
}
function Assert-KmcSprint6Selection($Facts,[string]$Who){
 if($Facts.committed -ne $true -or $Facts.fact -ne $true -or $Facts.eligibleAfterRank -ne $true -or $Facts.offeredLast -ne $true -or [int]$Facts.after.ranks -lt 1){ throw ('Sprint 6 '+$Who+' choice did not commit through the native controller') }
}
function Assert-KmcSprint6FeatEvidence {
 param([Parameter(Mandatory=$true)]$Request,[Parameter(Mandatory=$true)]$Artifact,[AllowNull()][string]$Status,[string]$EvidenceRoot)
 if($Request.scenario -cnotin @('sprint6-feat-rt','sprint6-feat-tb') -or $Artifact.schemaVersion -ne 50){ throw 'Sprint 6 scenario/schema mismatch' }
 if($Artifact.status -ceq 'FAIL') {
  if($Status -ceq 'PASS' -or (@($Artifact.errors).Count -eq 0 -and $Artifact.subscenarioFailCount -lt 1)){ throw 'Sprint 6 incomplete observation cannot become PASS' }
  return # Preserve the producer's explicit failed prefix; grant no qualification.
 }
 if($Artifact.status -cne 'PASS' -or @($Artifact.errors).Count -ne 0 -or @($Artifact.rows | Where-Object status -CNE 'PASS').Count -ne 0 -or $Artifact.subscenarioFailCount -ne 0){ throw 'Sprint 6 envelope differs' }
 $o=$Artifact.observations.sprint6Feat
 if($null -eq $o -or $o.contract -cne 'native-mounted-combat-feat-v1' -or $o.rider -cne $Artifact.observations.riderId -or $o.mount -cne $Artifact.observations.horseId){ throw 'Sprint 6 measurement identity differs' }
 if($o.checkD20IsControlled -ne $true -or $o.experienceIsDisclosedSetup -ne $true){ throw 'Sprint 6 setup is not disclosed' }
 $expected=@(Get-KmcSprint6Cases $Request.scenario)
 if(($o.cases | ConvertTo-Json -Compress) -cne ($expected | ConvertTo-Json -Compress)){ throw 'Sprint 6 registered row order differs' }
 $rows=@{}
 foreach($name in $expected){ $found=@($Artifact.rows | Where-Object name -CEQ $name); if($found.Count -ne 1){ throw ('Sprint 6 required row missing or duplicated: '+$name) }; $rows[$name]=$found[0] }
 # Every row: one check per attempt, one attempt per rule, and nothing done to a miss or by the mount's resources.
 $records=@($o.records); $rules=@($o.attackRules); $checks=@($o.skillChecks)
 $identities=@($records | ForEach-Object { [string]$_.ruleIdentity })
 if(@($identities | Select-Object -Unique).Count -ne $identities.Count){ throw 'Sprint 6 one attack rule was evaluated twice' }
 $attempted=@($records | Where-Object attempted -EQ $true)
 if($checks.Count -ne $attempted.Count){ throw ('Sprint 6 Mobility checks differ from attempts: '+$checks.Count+' versus '+$attempted.Count) }
 for($i=0;$i -lt $checks.Count;$i++){
  $c=$checks[$i];$a=$attempted[$i]
  if([string]$c.actor -cne [string]$a.rider -or [int]$c.result -ne [int]$a.checkResult -or [int]$c.dc -ne ([int]$a.attackResult+1) -or [bool]$c.passed -ne [bool]$a.negated){ throw 'Sprint 6 native check and recorded attempt disagree' }
  if([int]$a.attackResult -ne ([int]$a.attackD20+[int]$a.attackBonus)){ throw 'Sprint 6 compared something other than the complete attack result' }
 }
 foreach($rule in $rules){
  if($rule.autoHit -eq $true -or $rule.resultAtEvent -cin @('Hit','CriticalHit','Parried')){ continue }
  if([string]$rule.identity -cin $identities){ throw 'Sprint 6 a natural miss was evaluated' }
  if([int]$rule.damageRules -ne 0){ throw 'Sprint 6 a miss reached damage' }
 }
 foreach($r in $records){ if([double]$r.mountSwiftAfter -ne [double]$r.mountSwiftBefore){ throw 'Sprint 6 the mount paid the rider''s Swift action' } }
 if(@($records | Where-Object { [string]$_.mountId -ceq [string]$_.rider }).Count -ne 0){ throw 'Sprint 6 an actor rode itself' }

 if($Request.scenario -ceq 'sprint6-feat-rt'){
  $f=$rows['S6-selection'].evidence
  if($null -ne $f.registration.failure -or [int]$f.registration.ranks -ne 1 -or 'Feat' -cnotin @($f.registration.groups) -or 'CombatFeat' -cnotin @($f.registration.groups) -or $f.registration.prerequisite -cne 'SkillMobility=1'){ throw 'Sprint 6 feat registration differs' }
  $names=@($f.selections | ForEach-Object name)
  foreach($needed in 'BasicFeatSelection','FighterFeatSelection'){ if($needed -cnotin $names){ throw ('Sprint 6 selection missing: '+$needed) } }
  foreach($s in @($f.selections)){ if($s.lastIsFeat -ne $true -or [int]$s.occurrences -ne 1){ throw ('Sprint 6 selection order or duplicate: '+$s.name) } }
  $ordinary=$f.ordinary
  if($ordinary.selection -cne 'BasicFeatSelection'){ throw 'Sprint 6 ordinary selection differs' }
  if([int]$ordinary.preview.ranks -ne 0 -or [int]$ordinary.preview.total -lt 1 -or $ordinary.eligibleBeforeRank -ne $false -or $ordinary.selectAcceptedBeforeRank -ne $false){ throw 'Sprint 6 zero ranks with a bonus was not refused by the native controller' }
  if([int]$ordinary.previewAfterRank.ranks -ne 1){ throw 'Sprint 6 trained control is not one rank' }
  if($f.noCompanionAtChoice -ne $true){ throw 'Sprint 6 ordinary choice was made with a companion' }
  Assert-KmcSprint6Selection $ordinary 'ordinary'
  if($f.riderCombatChoice.selection -cne 'FighterFeatSelection'){ throw 'Sprint 6 combat choice differs' }
  Assert-KmcSprint6Selection $f.riderCombatChoice 'combat-bonus'
  if($f.riderToggle.present -ne $true -or $f.riderToggle.on -ne $true){ throw 'Sprint 6 preference toggle is not available and on' }

  $d=$rows['S6-rt-defense'].evidence.steps; $c='S6-rt-defense'
  $r=Get-KmcSprint6Record $o $c 2
  if([string]$r.mode -cne 'RT' -or [string]$r.commitment -cne 'RealTimeWindow' -or $r.forced -ne $true -or [int]$r.checkResult -ne [int]$r.attackResult+1 -or $r.stateAfter.window -ne $true -or [double]$r.swiftAfter -lt 5.5){ throw 'Sprint 6 real-time success differs' }
  if((Get-KmcSprint6Rule $o $r).opportunity -ne $false){ throw 'Sprint 6 real-time success was not an ordinary attack' }
  Assert-KmcSprint6Negated $o $r 'real-time success'
  Assert-KmcSprint6Refused $o (Get-KmcSprint6Record $o $c 3) 'UsageSpent' 'second real-time attack'
  if($d.renewedByNativeWindow.rider.window -ne $false -or $d.renewedByNativeWindow.rider.hasSwift -ne $true){ throw 'Sprint 6 real-time renewal differs' }
  $r=Get-KmcSprint6Record $o $c 5
  if([int]$r.checkResult -ne [int]$r.attackResult -or $r.attempted -ne $true -or $r.stateAfter.window -ne $true){ throw 'Sprint 6 equality was not exact or did not consume the use' }
  Assert-KmcSprint6Stands $o $r 'equality'
  $r=Get-KmcSprint6Record $o $c 7
  if([int]$r.checkResult -ne [int]$r.attackResult-1 -or $r.attempted -ne $true){ throw 'Sprint 6 failure was not exact' }
  Assert-KmcSprint6Stands $o $r 'failure'
  $r=Get-KmcSprint6Record $o $c 9
  if($r.forced -ne $false -or [int]$r.checkD20 -lt 1 -or [int]$r.checkD20 -gt 20 -or [bool]$r.negated -ne ([int]$r.checkResult -gt [int]$r.attackResult)){ throw 'Sprint 6 natural die disagrees with the strict rule' }
  if($r.negated -eq $true){ Assert-KmcSprint6Negated $o $r 'natural die' } else { Assert-KmcSprint6Stands $o $r 'natural die' }
  if($d.preferenceKeptUse.rider.window -ne $true -or $d.preferenceKeptUse.rider.windowIdentity -ne $d.naturalDie.rider.windowIdentity -or $d.preferenceKeptUse.rider.windowEndTicks -ne $d.naturalDie.rider.windowEndTicks -or $d.preferenceKeptUse.rider.charges -ne $d.naturalDie.rider.charges){ throw 'Sprint 6 the preference touched a running use' }
  Assert-KmcSprint6Refused $o (Get-KmcSprint6Record $o $c 11) 'PreferenceOff' 'preference off'
  Assert-KmcSprint6Negated $o (Get-KmcSprint6Record $o $c 12) 'preference on'

  $i=$rows['S6-rt-isolation'].evidence; $c='S6-rt-isolation'
  if([int]$i.attemptsAfterUnrelated -ne [int]$i.attemptsBeforeUnrelated -or @($rules | Where-Object { [string]$_.target -ceq [string]$o.rider }).Count -lt 1 -or @($rules | Where-Object { [string]$_.target -ceq [string]$i.unrelated }).Count -lt 1){ throw 'Sprint 6 attacks on the rider or an unrelated unit were evaluated, or never made' }
  if($i.stunApplied -ne $true){ throw 'Sprint 6 stun setup did not stop the rider' }
  Assert-KmcSprint6Refused $o (Get-KmcSprint6Record $o $c 3) 'RiderCannotAct' 'incapacitated rider'
  Assert-KmcSprint6Refused $o (Get-KmcSprint6Record $o $c 5) 'RiderFlatFooted' 'flat-footed rider'
  $first=Get-KmcSprint6Record $o $c 8; Assert-KmcSprint6Negated $o $first 'first rider'
  if([string]$first.rider -cne [string]$o.rider){ throw 'Sprint 6 first rider differs' }
  if($i.ownerAfterFirstRider.window -ne $false -or $i.ownerAfterFirstRider.spent -ne $false){ throw 'Sprint 6 one rider''s use reached another' }
  $second=Get-KmcSprint6Record $o $c 9
  if([string]$second.rider -cne [string]$i.secondPair.rider -or [string]$second.mountId -cne [string]$i.secondPair.mount -or [string]$second.commitment -cne 'RealTimeWindow'){ throw 'Sprint 6 second pair did not react independently' }
  Assert-KmcSprint6Negated $o $second 'second rider'
  if($i.ownerAfterOwnUse.window -ne $true){ throw 'Sprint 6 second rider kept no use' }
  if([int]$i.beforeDismount.windowIdentity -eq 0 -or $i.afterDismount.windowIdentity -ne $i.beforeDismount.windowIdentity -or $i.afterDismount.windowEndTicks -ne $i.beforeDismount.windowEndTicks){ throw 'Sprint 6 a dismount changed the running use' }
  if([int]$i.attemptsAfterDismountedStrike -ne [int]$i.attemptsAtDismount){ throw 'Sprint 6 a former mount was defended after the dismount' }
  if([int]$i.afterRemount.windowIdentity -ne 0 -and ($i.afterRemount.windowIdentity -ne $i.beforeDismount.windowIdentity -or $i.afterRemount.windowEndTicks -ne $i.beforeDismount.windowEndTicks)){ throw 'Sprint 6 a remount minted a fresh use' }
 } else {
  $f=$rows['S6-rider-feat'].evidence
  if($f.riderCombatChoice.selection -cne 'FighterFeatSelection'){ throw 'Sprint 6 turn-based rider choice differs' }
  Assert-KmcSprint6Selection $f.riderCombatChoice 'turn-based rider'
  $d=$rows['S6-tb-economy'].evidence.steps; $c='S6-tb-economy'
  $r=Get-KmcSprint6Record $o $c 2
  if([string]$r.mode -cne 'TB' -or $r.riderTurn -ne $true -or [string]$r.commitment -cne 'SpendCurrentSwift' -or $r.stateAfter.spent -ne $true -or $r.stateAfter.owed -ne $false -or [double]$r.swiftAfter -lt [double]$r.swiftBefore+5.9){ throw 'Sprint 6 own-turn reaction is not that turn''s Swift action' }
  if($r.opportunity -ne $true){ throw 'Sprint 6 own-turn strike is not an opportunity attack' }
  Assert-KmcSprint6Negated $o $r 'own-turn success'
  Assert-KmcSprint6Refused $o (Get-KmcSprint6Record $o $c 3) 'UsageSpent' 'second own-turn attack'
  $r=Get-KmcSprint6Record $o $c 5
  if($r.riderTurn -ne $false){ throw 'Sprint 6 still-spent strike was on the rider''s turn' }
  Assert-KmcSprint6Refused $o $r 'UsageSpent' 'off-turn attack in the same window'
  $n=$d.renewedNoDebt.rider
  if($n.spent -ne $false -or $n.owed -ne $false -or $n.hasSwift -ne $true -or [double]$n.swift -ne 0){ throw 'Sprint 6 preparation with no debt did not renew and free the Swift action' }
  if($d.otherSwiftSpent.rider.hasSwift -ne $false){ throw 'Sprint 6 the other Swift action did not spend the Swift opportunity' }
  $r=Get-KmcSprint6Record $o $c 9
  if($r.riderTurn -ne $false -or [string]$r.refusal -cne 'None' -or [string]$r.commitment -cne 'BorrowNextSwift' -or [int]$r.checkResult -ne [int]$r.attackResult -or $r.attempted -ne $true -or $r.stateAfter.spent -ne $true -or $r.stateAfter.owed -ne $true -or [double]$r.swiftAfter -ne [double]$r.swiftBefore){ throw 'Sprint 6 off-turn borrow after a spent Swift differs' }
  Assert-KmcSprint6Stands $o $r 'off-turn equality'
  Assert-KmcSprint6Refused $o (Get-KmcSprint6Record $o $c 10) 'UsageSpent' 'second off-turn attack'
  $p=$d.debtPaidAtPreparation.rider
  if($p.spent -ne $false -or $p.owed -ne $false -or $p.hasSwift -ne $false -or $p.swiftGateBlocked -ne $true -or [double]$p.swift -lt 5.9 -or [int]$p.charges -ne [int]$d.offTurnEqual.rider.charges+1){ throw 'Sprint 6 the debt was not charged exactly once at the next preparation' }
  if([double]$d.debtPaidAtPreparation.mountSwift -ne 0){ throw 'Sprint 6 the mount donated the Swift action' }
  $r=Get-KmcSprint6Record $o $c 12
  if($r.riderTurn -ne $true){ throw 'Sprint 6 paying-turn strike was off-turn' }
  Assert-KmcSprint6Refused $o $r 'SwiftUnavailable' 'attack on the paying turn'
  $mv=$rows['S6-tb-economy'].evidence.payingTurnMove
  if($mv.sameTurn -ne $true -or $mv.mounted -ne $true -or [double]$mv.metres -lt 0.5){ throw 'Sprint 6 the paying turn could not move by the ordinary ground click' }
  $r=Get-KmcSprint6Record $o $c 14
  if($r.riderTurn -ne $false -or [string]$r.commitment -cne 'BorrowNextSwift' -or $r.stateAfter.owed -ne $true){ throw 'Sprint 6 renewal after a debt did not permit a further reaction' }
  Assert-KmcSprint6Negated $o $r 'renewal after the debt'
  if(@($rules | Where-Object { $_.opportunity -eq $false -and [string]$_.target -ceq [string]$o.mount }).Count -lt 1){ throw 'Sprint 6 no ordinary hostile attack reached the mount in turn-based combat' }
 }
}
# The cold half of the persistence case: a save with a borrowed Swift and a spent use, restored without refund, then renewed.
function Assert-KmcSprint6FeatPersistence($Request,$Observations,[bool]$Cold){
 if(-not $Cold){
  $save=$Observations.featDebt
  if($null -eq $save -or $save.commitment -cne 'BorrowNextSwift' -or $save.state.spent -ne $true -or $save.state.owed -ne $true -or $save.riderTurn -ne $false){ throw 'Sprint 6 the save did not follow a committed off-turn use' }
  return
 }
 $c=$Observations.featCold; $n=$Observations.featRenewal
 if($null -eq $c -or $c.hasFeat -ne $true -or $c.state.spent -ne $true -or $c.state.owed -ne $true -or $c.preferenceOn -ne $true){ throw 'Sprint 6 the cold load refunded the used round or the debt' }
 if($null -eq $n -or $n.afterPreparation.spent -ne $false -or $n.afterPreparation.owed -ne $false -or $n.afterPreparation.hasSwift -ne $false -or [double]$n.afterPreparation.swift -lt 5.9 -or [int]$n.chargesAtPreparation -ne 1){ throw 'Sprint 6 the restored debt was not paid exactly once at the next preparation' }
 if($n.commitment -cne 'BorrowNextSwift' -or $n.attempted -ne $true -or $n.riderTurn -ne $false -or $n.stateAfter.owed -ne $true){ throw 'Sprint 6 no legal renewal followed the load' }
}
