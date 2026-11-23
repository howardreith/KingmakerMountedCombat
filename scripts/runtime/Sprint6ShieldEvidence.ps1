# Mounted Shield rows (docs/missions/mounted-shield/ACCEPTANCE.md) in the Sprint 6 feat family. The driver
# (src/KingmakerMountedCombat/Diagnostics/Sprint6ShieldScenario.cs) only records native observations; PASS is decided here.
# Every expected amount is derived from the mission's rule (shield base + Shield Focus + Greater Shield Focus once each,
# never enhancement) and compared with the production amount, the mount's native AC stat and its modifiers, native
# RuleCalculateAC results and the production Mounted Combat check record.
$script:KmcShieldFeatGuid='671a301bd1774677a8bfa2873d33b191'
$script:KmcShieldFocusGuid='ac57069b6bf8c904086171683992a92a'
$script:KmcMountedCombatGuid='a7d2e94c6f1b4830b5c9e28d17f3a604'
$script:KmcShieldStockBase=@{buckler=1;light=1;heavy=2;tower=4}

function Get-KmcShieldProperty($Value,[string]$Name){ if($null -eq $Value){ return $null }; $p=$Value.PSObject.Properties[$Name]; if($null -eq $p){ return $null }; $p.Value }

function Remove-KmcSprint6ShieldUnrelatedCheck($Checks,$Unrelated,[string]$Rider){
 if($null -eq $Unrelated){ throw 'Mounted Shield unrelated Mobility check was not recorded' }
 $kept=New-Object System.Collections.Generic.List[object]; $removed=0
 foreach($c in @($Checks)){
  if($removed -eq 0 -and [string]$c.actor -ceq $Rider -and [int]$c.dc -eq [int]$Unrelated.dc -and [int]$c.result -eq [int]$Unrelated.result -and [int]$c.bonus -eq [int]$Unrelated.bonus -and [int]$c.frame -eq [int]$Unrelated.frame){ $removed++; continue }
  $kept.Add($c)
 }
 if($removed -ne 1){ throw 'Mounted Shield unrelated Mobility check is not exactly one observed native check' }
 $kept.ToArray()
}

# One snapshot against its expected amount: production amount, exactly one KMC Shield modifier owned by the rider's own fact
# (none when zero), the ledger, and no Mounted Shield modifier on the rider's Mobility.
function Assert-KmcShieldSnapshot($S,[int]$Expected,[string]$What){
 if($null -eq $S){ throw ('Mounted Shield step missing: '+$What) }
 if([int]$S.amount -ne $Expected){ throw ('Mounted Shield '+$What+': production amount '+$S.amount+', expected '+$Expected) }
 $owned=@($S.owned)
 if($Expected -eq 0){ if($owned.Count -ne 0 -or $null -ne $S.ledger){ throw ('Mounted Shield '+$What+': a contribution remained with no eligible amount') } }
 else {
  if($owned.Count -ne 1){ throw ('Mounted Shield '+$What+': '+$owned.Count+' contributions on the mount, expected one') }
  $m=$owned[0]
  if([int]$m.value -ne $Expected -or [string]$m.descriptor -cne 'Shield' -or [string]$m.sourceGuid -cne $script:KmcShieldFeatGuid -or $m.sourceIsRiderFact -ne $true){ throw ('Mounted Shield '+$What+': the contribution is not the rider''s own Shield bonus of '+$Expected) }
  if($null -eq $S.ledger -or [string]$S.ledger.mount -cne [string]$S.mount){ throw ('Mounted Shield '+$What+': the ledger does not name this exact mount') }
 }
 if([int]$S.riderMobilityKmc -ne 0){ throw ('Mounted Shield '+$What+': a Mounted Shield modifier reached the rider''s Mobility stat') }
}

# Native AC arithmetic against the shieldless reference: shield bonuses do not stack, so the ordinary and flat-footed AC rise
# by the highest Shield bonus; touch AC never includes it; the native rules agree with the stat.
function Assert-KmcShieldAc($S,$Reference,[int]$HighestShield,[string]$What){
 if([int]$S.ac -ne [int]$Reference.ac+$HighestShield -or [int]$S.flatFooted -ne [int]$Reference.flatFooted+$HighestShield){ throw ('Mounted Shield '+$What+': native AC '+$S.ac+'/'+$S.flatFooted+' differs from reference + '+$HighestShield) }
 if([int]$S.touch -ne [int]$Reference.touch -or [int]$S.flatFootedTouch -ne [int]$Reference.flatFootedTouch){ throw ('Mounted Shield '+$What+': a shield bonus reached touch AC') }
 if([int]$S.ruleMelee -ne [int]$S.ac -or [int]$S.ruleTouch -ne [int]$S.touch){ throw ('Mounted Shield '+$What+': the native AC rule disagrees with the stat') }
}

function Assert-KmcShieldEligibility($Rows,[bool]$RequireMissingCombat,[string]$Who){
 $rows=@($Rows | Where-Object { $_.offered -eq $true })
 if($rows.Count -eq 0){ throw ('Mounted Shield '+$Who+': the feat was never offered') }
 if(@($rows | Where-Object { $_.eligible -eq $true -and ($_.mountedCombat -ne $true -or $_.shieldFocus -ne $true) }).Count -ne 0){ throw ('Mounted Shield '+$Who+': eligible while a prerequisite was missing') }
 if(@($rows | Where-Object { $_.mountedCombat -eq $true -and $_.shieldFocus -ne $true -and $_.eligible -eq $false }).Count -lt 1){ throw ('Mounted Shield '+$Who+': no native refusal without Shield Focus') }
 if($RequireMissingCombat -and @($rows | Where-Object { $_.mountedCombat -ne $true -and $_.shieldFocus -eq $true -and $_.eligible -eq $false }).Count -lt 1){ throw ('Mounted Shield '+$Who+': no native refusal without Mounted Combat') }
 if(@($rows | Where-Object { $_.mountedCombat -eq $true -and $_.shieldFocus -eq $true -and $_.eligible -eq $true }).Count -lt 1){ throw ('Mounted Shield '+$Who+': never eligible with both prerequisites') }
 if(@($rows | Where-Object { $_.iconIsShieldIcon -ne $true }).Count -ne 0){ throw ('Mounted Shield '+$Who+': an offered row did not carry the Mounted Shield icon') }
}

function Assert-KmcShieldSelection($E,[bool]$RealTime){
 $g=$E.shieldRegistration
 if($null -ne $g.failure -or $g.withdrawn -ne $false -or [string]$g.guid -cne $script:KmcShieldFeatGuid -or [int]$g.ranks -ne 1 -or $g.isClassFeature -ne $true -or $g.hideInUi -ne $false){ throw 'Mounted Shield registration differs from the single-rank visible donor contract' }
 if(@($g.groups) -cnotcontains 'Feat' -or @($g.groups) -cnotcontains 'CombatFeat'){ throw 'Mounted Shield is not a Feat and CombatFeat' }
 $visible=@($g.prerequisites | Where-Object { $_.hidden -ne $true }); $hidden=@($g.prerequisites | Where-Object { $_.hidden -eq $true })
 $features=@($visible | ForEach-Object { [string]$_.feature } | Sort-Object)
 if($visible.Count -ne 2 -or @($visible | Where-Object { [string]$_.type -cne 'PrerequisiteFeature' -or [string]$_.group -cne 'All' }).Count -ne 0 -or
   (($features -join ',') -cne ((@($script:KmcMountedCombatGuid,$script:KmcShieldFocusGuid) | Sort-Object) -join ','))){ throw 'Mounted Shield visible prerequisites are not exactly Mounted Combat and Shield Focus (ALL)' }
 if($hidden.Count -ne 1){ throw 'Mounted Shield has not exactly one hidden availability prerequisite' }
 if($g.iconIsShieldIcon -ne $true -or [int]$g.iconWidth -ne 64 -or [int]$g.iconHeight -ne 64){ throw 'Mounted Shield blueprint icon is not the 64x64 Mounted Shield sprite' }
 if([string]::IsNullOrWhiteSpace([string]$g.name) -or [string]::IsNullOrWhiteSpace([string]$g.description)){ throw 'Mounted Shield name or description is empty' }
 foreach($s in @($E.threeFeatSelections)){
  if([int]$s.combat -ne 1 -or [int]$s.shield -ne 1 -or [int]$s.archery -gt 1 -or $s.kmcTailExact -ne $true){ throw 'Mounted Shield three-feat publication differs (each KMC feat once in the exact tail)' }
 }
 if(@($E.threeFeatSelections).Count -lt 2){ throw 'Mounted Shield is not published in the ordinary and fighter lists' }
 $levels=@($E.levels)
 if($levels.Count -lt 2){ throw 'Mounted Shield acquisition took fewer native levels than its two-step plan' }
 $rows=@(); $offeredIn=@{}
 foreach($l in $levels){
  if($l.committed -ne $true){ throw 'Mounted Shield a native level did not commit' }
  $c=Get-KmcShieldProperty $l 'cancelled'
  if($null -eq $c){ throw 'Mounted Shield a level was not first chosen and cancelled' }
  if(($l.factsAfterCancel | ConvertTo-Json -Compress) -cne ($l.factsBefore | ConvertTo-Json -Compress) -or [int]$l.characterLevelAfterCancel -ne [int]$l.characterLevelBefore){ throw 'Mounted Shield a cancelled level changed the unit' }
  if([int]$l.characterLevelAfter -ne [int]$l.characterLevelBefore+1){ throw 'Mounted Shield a committed level did not add exactly one level' }
  $rows+=@($c.eligibility)+@($l.pass.eligibility)
  foreach($r in @($c.eligibility)+@($l.pass.eligibility)){ if($r.offered -eq $true){ $offeredIn[[string]$r.selection]=$true } }
 }
 if(-not $offeredIn.ContainsKey('BasicFeatSelection') -or -not $offeredIn.ContainsKey('FighterFeatSelection')){ throw 'Mounted Shield was not offered in both the ordinary and fighter lists' }
 Assert-KmcShieldEligibility $rows $true 'rider'
 # Mounted Combat and Shield Focus at one level, then a Back that the preview follows.
 $pair=@($levels | Where-Object { $ch=@($_.pass.choices); ($ch -match '=MountedCombat').Count -ge 1 -and ($ch -match '=ShieldFocus$').Count -ge 1 })
 if($pair.Count -ne 1){ throw 'Mounted Combat and Shield Focus were not chosen at one native level' }
 $back=Get-KmcShieldProperty $pair[0].pass 'backOut'
 if($null -eq $back -or $back.previewHasWithdrawn -ne $false){ throw 'Mounted Shield Back did not withdraw the first choice from the preview' }
 $afterBack=Get-KmcShieldProperty $pair[0].pass 'afterBack'
 if($null -eq $afterBack -or $afterBack.bothChosen -ne $true -or $afterBack.mountedCombat -ne $true -or $afterBack.shieldFocus -ne $true){ throw 'Mounted Shield the choices withdrawn by Back were not both chosen again on the same level' }
 if([int]$pair[0].factsAfter.mountedCombat -ne 1 -or [int]$pair[0].factsAfter.shieldFocus -ne 1){ throw 'Mounted Shield the level with Back did not commit both prerequisites' }
 $backRows=@($pair[0].pass.eligibility | Where-Object { [string]$_.moment -clike 'back-*' -and $_.offered -eq $true })
 if($backRows.Count -lt 1 -or @($backRows | Where-Object eligible -EQ $true).Count -ne 0){ throw 'Mounted Shield stayed eligible after Back withdrew a prerequisite' }
 $cancelledShield=@($levels | Where-Object { $_.cancelled.previewFacts.mountedShield -eq $true })
 if($cancelledShield.Count -lt 1){ throw 'No cancelled level held Mounted Shield in its preview before rolling back' }
 $f=$E.riderFacts
 if([int]$f.mountedCombat -ne 1 -or [int]$f.shieldFocus -ne 1 -or [int]$f.mountedShield -ne 1 -or [int]$f.mountedShieldRank -ne 1){ throw 'Mounted Shield the rider does not own each feat exactly once' }
 if($RealTime){
  $owner=@($E.ownerLevels); if($owner.Count -lt 1){ throw 'Mounted Shield the second rider took no native level' }
  $ownerRows=@($owner | ForEach-Object { @($_.pass.eligibility) })
  if(@($ownerRows | Where-Object { $_.offered -eq $true -and $_.shieldFocus -eq $true -and $_.mountedCombat -ne $true -and $_.eligible -eq $false }).Count -lt 1){ throw 'Mounted Shield was not refused natively without Mounted Combat' }
  if(@($ownerRows | Where-Object { $_.offered -eq $true -and $_.eligible -eq $true }).Count -ne 0){ throw 'Mounted Shield was eligible for the second rider without Mounted Combat' }
  if($E.ownerCompanion.acquired -ne $true){ throw 'Mounted Shield the second pair has no native companion' }
  if($null -eq (Get-KmcShieldProperty $E.ownerCompanion 'experienceSetup') -or [long]$E.ownerCompanion.experienceSetup -lt [long]$E.ownerCompanion.experienceBefore){ throw 'Mounted Shield the second rider''s Ranger experience setup is not disclosed' }
  $s=$E.ownerSetupFacts; if([int]$s.mountedCombat -ne 1 -or [int]$s.shieldFocus -ne 1 -or [int]$s.mountedShield -ne 1){ throw 'Mounted Shield second-rider setup facts differ' }
 }
}

function Assert-KmcShieldEquipment($E){
 $d=$E.steps; $none=$d.none
 Assert-KmcShieldSnapshot $none 0 'no shield'
 $expected=[ordered]@{buckler=2;light=2;heavy=3}
 $tower=Get-KmcShieldProperty $E 'towerRefused'
 if($null -eq $tower){ $expected['tower']=5 } elseif(-not ([string]$tower).StartsWith('native slot refused ')){ throw 'Mounted Shield the tower observation was skipped for something other than a native slot refusal' }
 foreach($k in $expected.Keys){
  $s=Get-KmcShieldProperty $d $k
  Assert-KmcShieldSnapshot $s $expected[$k] $k
  if([int]$s.facts.base -ne $script:KmcShieldStockBase[$k] -or $s.facts.focus -ne $true -or $s.facts.greater -ne $false -or $s.facts.effectiveShield -ne $true){ throw ('Mounted Shield '+$k+': the base or focus terms differ from the stock data') }
  Assert-KmcShieldAc $s $none $expected[$k] $k
 }
 $enhanced=@($d.PSObject.Properties | Where-Object { $_.Name -clike 'heavyEnhanced*' })
 if($enhanced.Count -lt 1){ throw 'Mounted Shield no enhanced stock heavy shield was observed' }
 foreach($p in $enhanced){
  $s=$p.Value; Assert-KmcShieldSnapshot $s 3 $p.Name
  if([int]$s.shield.enhancement -lt 1 -or [int]$s.facts.base -ne 2){ throw ('Mounted Shield '+$p.Name+' is not an enhanced shield of the heavy base') }
  Assert-KmcShieldAc $s $none 3 $p.Name
 }
 Assert-KmcShieldSnapshot $d.heavyAgain 3 'heavy again'
 Assert-KmcShieldSnapshot $d.greater 4 'Greater Shield Focus'; Assert-KmcShieldAc $d.greater $none 4 'Greater Shield Focus'
 if($d.greater.facts.greater -ne $true){ throw 'Mounted Shield Greater Shield Focus was not read' }
 Assert-KmcShieldSnapshot $d.greaterRemoved 3 'Greater Shield Focus removed'
 foreach($n in 2,3,4){
  $s=Get-KmcShieldProperty $d ('competing'+$n); Assert-KmcShieldSnapshot $s 3 ('competing '+$n)
  if(@($s.shieldModifiers | Where-Object { $_.kmc -ne $true -and [int]$_.value -eq $n }).Count -ne 1){ throw ('Mounted Shield the competing +'+$n+' shield source is missing') }
  Assert-KmcShieldAc $s $none ([Math]::Max(3,$n)) ('competing '+$n)
 }
 if($null -ne $E.shieldSpell){ Assert-KmcShieldSnapshot $d.mountSpell 3 'Shield spell on the mount'; Assert-KmcShieldAc $d.mountSpell $none 4 'Shield spell on the mount' }
 Assert-KmcShieldSnapshot $d.otherSet 0 'other weapon set'
 if([int]$d.otherSet.riderSet -eq [int]$d.heavyAgain.riderSet){ throw 'Mounted Shield the weapon set did not change' }
 Assert-KmcShieldAc $d.otherSet $none 0 'other weapon set'
 Assert-KmcShieldSnapshot $d.setRestored 3 'weapon set restored'
 Assert-KmcShieldSnapshot $d.unequipped 0 'unequipped'
 if($d.unequipped.shieldInInventory -ne $true){ throw 'Mounted Shield the unequipped shield did not stay in the inventory' }
 Assert-KmcShieldSnapshot $d.riderSpellOnly 0 'rider Shield effect only'
 if(@($d.riderSpellOnly.riderShieldModifiers | Where-Object { $_.item -ne $true -and [int]$_.value -gt 0 }).Count -lt 1){ throw 'Mounted Shield the rider''s own Shield effect was not on the rider' }
 Assert-KmcShieldAc $d.riderSpellOnly $none 0 'rider Shield effect only'
 Assert-KmcShieldSnapshot $d.final 3 'final'
 foreach($p in $d.PSObject.Properties){ if($p.Value.mounted -ne $true){ throw ('Mounted Shield '+$p.Name+' was not observed mounted') } }
}

function Assert-KmcShieldPairs($E){
 $d=$E.steps
 foreach($n in 'bothFirst','uiSelectedSecondFirst','remounted','featReacquired','serviceEnabledFirst'){ Assert-KmcShieldSnapshot (Get-KmcShieldProperty $d $n) 3 $n }
 foreach($n in 'bothSecond','uiSelectedSecondSecond','dismountedSecond','featRemovedSecond','serviceEnabledSecond'){
  $s=Get-KmcShieldProperty $d $n; Assert-KmcShieldSnapshot $s 2 $n
  if([string]$s.rider -ceq [string]$d.bothFirst.rider -or [string]$s.mount -ceq [string]$d.bothFirst.mount){ throw ('Mounted Shield '+$n+' is not the second pair') }
 }
 foreach($cross in $E.crossFirstToSecondMount,$E.crossSecondToFirstMount){
  if([int]$cross.amount -ne 0 -or @($cross.owned).Count -ne 1 -or $cross.owned[0].sourceIsRiderFact -ne $false){ throw 'Mounted Shield a rider''s contribution reached another pair''s mount' }
 }
 Assert-KmcShieldSnapshot $d.dismountedFirst 0 'dismounted'
 if($d.dismountedFirst.mounted -ne $false){ throw 'Mounted Shield the first pair did not dismount' }
 Assert-KmcShieldSnapshot $d.featRemoved 0 'feat removed'
 if($d.featRemoved.facts.ownsFeat -ne $false){ throw 'Mounted Shield the feat removal was not observed' }
 if($E.serviceDisableAccepted -ne $true){ throw 'Mounted Shield the service refused to disable' }
 foreach($n in 'serviceDisabledFirst','serviceDisabledSecond'){ $s=Get-KmcShieldProperty $d $n; Assert-KmcShieldSnapshot $s 0 $n; if($s.serviceEnabled -ne $false){ throw ('Mounted Shield '+$n+' observed an enabled service') } }
}

# The native roll of exactly this attempt (supplemental review PN5-F6): attempts and native Mobility rolls are paired in order,
# as the Sprint 6 reader pairs them, after the one unrelated check of the real-time row is set aside.
function Get-KmcShieldCheckRow($Measure,$R){
 $checks=@($Measure.skillChecks)
 if($script:KmcShieldUnrelated){ $checks=@(Remove-KmcSprint6ShieldUnrelatedCheck $checks $script:KmcShieldUnrelated $Measure.rider) }
 $attempted=@($Measure.records | Where-Object attempted -EQ $true)
 $index=-1; for($i=0;$i -lt $attempted.Count;$i++){ if([string]$attempted[$i].ruleIdentity -ceq [string]$R.ruleIdentity){ $index=$i } }
 if($index -lt 0 -or $index -ge $checks.Count){ throw 'Mounted Shield a recorded check has no observed native roll' }
 $row=$checks[$index]
 if([int]$row.result -ne [int]$R.checkResult -or [int]$row.d20 -ne [int]$R.checkD20 -or [int]$row.dc -ne [int]$R.checkDc){ throw 'Mounted Shield the paired native roll is not this attempt''s roll' }
 $row
}
# The use was actually committed (supplemental review PN5-F8), not only labeled: the Sprint 6 state and cooldown evidence.
function Assert-KmcShieldCommitted($R,[string]$What){
 switch -CaseSensitive ([string]$R.commitment){
  'RealTimeWindow' { if($R.stateAfter.window -ne $true -or [double]$R.swiftAfter -lt 5.5){ throw ('Mounted Shield '+$What+': the real-time use opened no window or charged no Swift action') } }
  'SpendCurrentSwift' { if($R.stateAfter.spent -ne $true -or $R.stateAfter.owed -ne $false -or [double]$R.swiftAfter -lt [double]$R.swiftBefore+5.9){ throw ('Mounted Shield '+$What+': the own-turn use did not spend this turn''s Swift action') } }
  'BorrowNextSwift' { if($R.stateAfter.spent -ne $true -or $R.stateAfter.owed -ne $true -or [double]$R.swiftAfter -ne [double]$R.swiftBefore){ throw ('Mounted Shield '+$What+': the off-turn use did not borrow the next Swift action') } }
  default { throw ('Mounted Shield '+$What+': no commitment for an attempted check') }
 }
}
function Assert-KmcShieldRecord($R,[string]$What,$Measure){
 Assert-KmcShieldCommitted $R $What
 $row=Get-KmcShieldCheckRow $Measure $R
 if([int]$row.kmcShieldModifiers -ne 1 -or [int]$row.kmcShieldTotal -ne 3){ throw ('Mounted Shield '+$What+': the native roll did not hold exactly one +3 Mounted Shield modifier') }
 if([int]$R.shieldBonus -ne 3 -or [int]$R.checkBonus -lt 3 -or [int]$R.checkResult -ne [int]$R.checkD20+[int]$R.checkStat+[int]$R.checkBonus -or [int]$R.checkDc -ne [int]$R.attackResult+1){ throw ('Mounted Shield '+$What+': the native check did not carry the +3 addition exactly once') }
 if($R.forced -ne $true){ throw ('Mounted Shield '+$What+': the boundary row is not a labeled planned total') }
 if([int]$R.checkD20 -lt 1 -or [int]$R.checkD20 -gt 20){ throw ('Mounted Shield '+$What+': the planned total needed a die outside 1..20') }
}

function Assert-KmcShieldCheckRealTime($Measure,$E){
 $c='MS-check-rt'
 $r=Get-KmcSprint6Record $Measure $c 2
 if([int]$r.checkResult -ne [int]$r.attackResult -or $r.attempted -ne $true -or [string]$r.commitment -cne 'RealTimeWindow'){ throw 'Mounted Shield equality was not the exact shield-modified total' }
 Assert-KmcShieldRecord $r 'equality' $Measure; Assert-KmcSprint6Stands $Measure $r 'Mounted Shield equality'
 $r=Get-KmcSprint6Record $Measure $c 4
 if([int]$r.checkResult -ne [int]$r.attackResult+1){ throw 'Mounted Shield success was not one above the attack result' }
 Assert-KmcShieldRecord $r 'success' $Measure; Assert-KmcSprint6Negated $Measure $r 'Mounted Shield success'
 $u=$E.equipmentDuringUse
 foreach($k in 'spent','owed','window','windowIdentity'){ if([string]$u.before.$k -cne [string]$u.unequipped.$k -or [string]$u.before.$k -cne [string]$u.reequipped.$k){ throw 'Mounted Shield an equipment change refunded or restarted the running use' } }
 Assert-KmcSprint6Refused $Measure (Get-KmcSprint6Record $Measure $c 5) 'UsageSpent' 'Mounted Shield spent use'
 Assert-KmcShieldSnapshot $E.steps.spentRefused 3 'spent use'
 Assert-KmcSprint6Refused $Measure (Get-KmcSprint6Record $Measure $c 7) 'PreferenceOff' 'Mounted Shield preference off'
 Assert-KmcShieldSnapshot $E.steps.preferenceOffRefused 3 'preference off'
 # A stronger shield source on the mount covers its AC, and the production check made under it still carries +3.
 $s=$E.steps.competingBeforeCheck; Assert-KmcShieldSnapshot $s 3 'competing +4 before the check'
 if(@($s.shieldModifiers | Where-Object { $_.kmc -ne $true -and [int]$_.value -eq 4 }).Count -ne 1){ throw 'Mounted Shield the competing +4 was not on the mount at the check' }
 $r=Get-KmcSprint6Record $Measure $c 9
 if([int]$r.checkResult -ne [int]$r.attackResult+1){ throw 'Mounted Shield the check under the stronger shield was not the planned success' }
 Assert-KmcShieldRecord $r 'check under a stronger shield' $Measure; Assert-KmcSprint6Negated $Measure $r 'Mounted Shield check under a stronger shield'
 if([int]$E.unrelatedMobility.bonus -ne 0 -or [int]$E.unrelatedMobility.stat -ne [int]$E.unrelatedMobility.displayed){ throw 'Mounted Shield an unrelated Mobility check received an addition' }
 $unrelatedRow=@($Measure.skillChecks | Where-Object { [int]$_.frame -eq [int]$E.unrelatedMobility.frame -and [int]$_.dc -eq [int]$E.unrelatedMobility.dc })
 if($unrelatedRow.Count -ne 1 -or [int]$unrelatedRow[0].kmcShieldModifiers -ne 0){ throw 'Mounted Shield an unrelated Mobility roll held a Mounted Shield modifier' }
 foreach($x in @($Measure.records | Where-Object { [string]$_.case -ceq $c -and $_.attempted -eq $true })){ if([int]$x.shieldBonus -ne 3){ throw 'Mounted Shield a check on this pair missed the full amount' } }
 $b=$E.band; $miss=Get-KmcShieldProperty $E 'bandMiss'
 if([int]$b.amount -ne 3 -or $null -eq $miss -or [int]$E.bandAttempts -lt 1 -or [int]$E.bandAttempts -gt 120){ throw 'Mounted Shield no miss caused only by its AC was identified within the strike bound' }
 # The AC change Mounted Shield made at the strike (supplemental review PN5-F7): its amount above the strongest other Shield.
 $effective=[int]$b.effectiveDelta
 if($effective -lt 1 -or $effective -ne [Math]::Max(0,[int]$b.amount-[Math]::Max(0,[int]$b.otherShield)) -or [int]$b.acWithoutShield -ne [int]$b.ac-$effective){ throw 'Mounted Shield the band''s effective AC change is not derived from the native modifiers' }
 $hostile=@($Measure.records | Where-Object { [string]$_.case -ceq $c } | ForEach-Object { [string]$_.attacker } | Select-Object -Unique)
 $row=@($Measure.attackRules | Where-Object { [string]$_.identity -ceq [string]$miss.identity })
 if($row.Count -ne 1 -or $hostile.Count -ne 1 -or [string]$row[0].attacker -cne $hostile[0] -or [string]$row[0].target -cne [string]$Measure.mount -or $row[0].isHitAtEvent -eq $true -or [int]$row[0].d20 -le 1 -or
   [int]$row[0].ac -ne [int]$b.ac -or ([int]$row[0].d20+[int]$row[0].bonus) -ge [int]$b.ac -or ([int]$row[0].d20+[int]$row[0].bonus) -lt [int]$b.acWithoutShield){ throw 'Mounted Shield the identified band miss is not a miss that reached the AC without Mounted Shield' }
 if([string]$miss.identity -cin @($Measure.records | ForEach-Object { [string]$_.ruleIdentity })){ throw 'Mounted Shield a miss caused by its AC was evaluated by Mounted Combat' }
}

function Assert-KmcShieldCheckTurnBased($Measure,$E){
 $c='MS-check-tb'
 $r=Get-KmcSprint6Record $Measure $c 2
 if([string]$r.mode -cne 'TB' -or $r.riderTurn -ne $true -or [string]$r.commitment -cne 'SpendCurrentSwift' -or [int]$r.checkResult -ne [int]$r.attackResult+1){ throw 'Mounted Shield own-turn success differs' }
 Assert-KmcShieldRecord $r 'own-turn success' $Measure; Assert-KmcSprint6Negated $Measure $r 'Mounted Shield own-turn success'
 Assert-KmcSprint6Refused $Measure (Get-KmcSprint6Record $Measure $c 3) 'UsageSpent' 'Mounted Shield second own-turn attack'
 Assert-KmcShieldSnapshot $E.steps.ownTurnSecondRefused 3 'turn-based spent use'
 $r=Get-KmcSprint6Record $Measure $c 6
 if($r.riderTurn -ne $false -or [string]$r.commitment -cne 'BorrowNextSwift' -or [int]$r.checkResult -ne [int]$r.attackResult){ throw 'Mounted Shield off-turn equality differs' }
 Assert-KmcShieldRecord $r 'off-turn equality' $Measure; Assert-KmcSprint6Stands $Measure $r 'Mounted Shield off-turn equality'
}

function Assert-KmcSprint6ShieldRows($Measure,$Rows,[string]$Scenario){
 $m=$Measure.mountedShield
 if($null -eq $m -or [string]$m.contract -cne 'native-mounted-shield-v1' -or [string]$m.featGuid -cne $script:KmcShieldFeatGuid -or [string]::IsNullOrWhiteSpace([string]$m.setupDisclosure)){ throw 'Mounted Shield measurement identity or setup disclosure differs' }
 # Stock items only (supplemental review PN5-F9): every shield the driver used is a stock blueprint loaded from the game's assets.
 $stock=Get-KmcShieldProperty $m 'shieldBlueprints'
 if($null -eq $stock){ throw 'Mounted Shield the stock shield identities were not recorded' }
 foreach($item in @($stock.mundane.PSObject.Properties | ForEach-Object { $_.Value })+@($stock.enhancedCandidates)){ if($item.stockLoaded -ne $true){ throw 'Mounted Shield a shield that is not a loaded stock blueprint was used' } }
 $script:KmcShieldUnrelated=$(if($Scenario -ceq 'sprint6-shield-rt'){ $Rows['MS-check-rt'].evidence.unrelatedMobility }else{ $null })
 if($Scenario -ceq 'sprint6-shield-rt'){
  Assert-KmcShieldSelection $Rows['MS-selection'].evidence $true
  Assert-KmcShieldEquipment $Rows['MS-equipment'].evidence
  Assert-KmcShieldPairs $Rows['MS-pairs'].evidence
  Assert-KmcShieldCheckRealTime $Measure $Rows['MS-check-rt'].evidence
 } else {
  Assert-KmcShieldSelection $Rows['MS-rider-feat'].evidence $false
  Assert-KmcShieldCheckTurnBased $Measure $Rows['MS-check-tb'].evidence
 }
 # The fixture actors' hands and every setup modifier must be verifiably restored (supplemental review PN4-F4).
 $drain=Get-KmcShieldProperty $m 'drain'
 if($null -eq $drain){ throw 'Mounted Shield fixture drain evidence is missing' }
 if(@($drain.PSObject.Properties | Where-Object { $_.Name -clike '*Error' }).Count -ne 0){ throw 'Mounted Shield a setup modifier or effect could not be removed at the drain' }
 # Every setup obligation released and verified (supplemental review PN5-F3).
 if($drain.clean -ne $true -or @($drain.setup.remaining).Count -ne 0 -or @($drain.buffsLeft).Count -ne 0){ throw 'Mounted Shield setup modifiers, effects or equipment were not verifiably released at the drain' }
 $actors=@('rider'); if($Scenario -ceq 'sprint6-shield-rt'){ $actors+='owner' }
 foreach($who in $actors){ $x=Get-KmcShieldProperty $drain $who; if($null -eq $x -or @($x.errors).Count -ne 0 -or [int]$x.createdLeft -ne 0 -or [int]$x.handsChecked -lt 2 -or [int]$x.set -ne [int]$x.originalSet){ throw ('Mounted Shield fixture equipment was not verifiably restored for the '+$who) } }
}

# The persistence case (supplemental review PN4-F8). The producer (warm) artifact holds the state before the save, the archive
# it wrote and its continuation; the consumer (cold) artifact holds the restored state, the loaded archive and its
# continuation. Each is read in its own shape. Neither archive may hold a Mounted Shield source tag (the native save turns
# the fact off before it serializes); the cold rider and mount are bound to the authenticated continuation's actors.
function Assert-KmcShieldArchive($Scan,[string]$What){
 if($null -eq $Scan -or $null -ne $Scan.error -or [string]::IsNullOrWhiteSpace([string]$Scan.area)){ throw ('Mounted Shield '+$What+' archive was not read') }
 foreach($entry in @('party.json',([string]$Scan.area+'.json'))){
  if(@($Scan.entries | Where-Object { [string]$_.entry -ceq $entry -and $_.present -eq $true -and [int]$_.length -gt 0 }).Count -ne 1){ throw ('Mounted Shield '+$What+' archive lacks its nonempty '+$entry) }
 }
 if([int]$Scan.mountTags -ne 0 -or [int]$Scan.checkTags -ne 0){ throw ('Mounted Shield '+$What+' archive serialized a Mounted Shield modifier') }
}
function Assert-KmcShieldOneCurrent($S,[string]$What){
 if($null -eq $S -or $S.mounted -ne $true -or $S.hasFeat -ne $true -or [int]$S.shieldBase -ne 2 -or [int]$S.amount -ne 3 -or [int]$S.ownedCount -ne 1 -or [int]@($S.ownedValues)[0] -ne 3 -or
   @($S.ownedByRiderFact)[0] -ne $true -or [string]$S.ledgerMount -cne [string]$S.mount -or [int]$S.otherMountOwned -ne 0){ throw ('Mounted Shield '+$What+' does not hold exactly one current contribution') }
}
function Assert-KmcSprint6ShieldPersistence($Request,$Observations,[bool]$Cold){
 $o=$Observations
 if(-not $Cold){
  $save=Get-KmcShieldProperty $o 'shieldAtSave'
  if($null -eq $save -or $save.hasShieldFocus -ne $true){ throw 'Mounted Shield source state before the save is missing' }
  Assert-KmcShieldOneCurrent $save 'the source before the save'
  Assert-KmcShieldArchive (Get-KmcShieldProperty $o 'shieldSavedArchive') 'the written'
  $after=Get-KmcShieldProperty $o 'shieldWarmContinuation'; Assert-KmcShieldOneCurrent $after 'the warm continuation'
  Assert-KmcShieldFirstPair $o $save 'the source before the save'; Assert-KmcShieldFirstPair $o $after 'the warm continuation'
  if($save.shieldIdentity.stockLoaded -ne $true){ throw 'Mounted Shield the persistence shield is not a loaded stock blueprint' }
  return
 }
 if($null -ne (Get-KmcShieldProperty $o 'shieldAtSave')){ throw 'Mounted Shield a cold artifact carries producer state' }
 $restore=Get-KmcShieldProperty $o 'shieldColdAtRestore'
 if($null -eq $restore -or $restore.hasFeat -ne $true -or [int]$restore.shieldBase -ne 2 -or [int]$restore.ownedCount -gt 1 -or [int]$restore.otherMountOwned -ne 0){ throw 'Mounted Shield the cold load restored a stale or doubled contribution' }
 # A contribution already present at restoration must itself be the current one (supplemental review PN5-F5).
 if([int]$restore.ownedCount -eq 1 -and ([int]@($restore.ownedValues)[0] -ne 3 -or @($restore.ownedByRiderFact)[0] -ne $true -or [string]$restore.ledgerMount -cne [string]$restore.mount)){ throw 'Mounted Shield the cold load restored a wrong contribution' }
 Assert-KmcShieldArchive $restore.archive 'the loaded'
 $after=Get-KmcShieldProperty $o 'shieldColdContinuation'; Assert-KmcShieldOneCurrent $after 'the cold continuation'
 Assert-KmcShieldFirstPair $o $restore 'the cold restore'; Assert-KmcShieldFirstPair $o $after 'the cold continuation'
}
# The authenticated combat checkpoint orders its actors first rider, first mount, second rider, second mount
# (Chunk7CombatCheckpointDriver); the shield pair is the first pair in those exact roles.
function Assert-KmcShieldFirstPair($Observations,$S,[string]$What){
 $ids=@($Observations.combatContinuation.actorIds | ForEach-Object { [string]$_ })
 if($ids.Count -ne 4 -or [string]$S.rider -cne $ids[0] -or [string]$S.mount -cne $ids[1]){ throw ('Mounted Shield '+$What+' is not the authenticated first rider on the first mount') }
}
