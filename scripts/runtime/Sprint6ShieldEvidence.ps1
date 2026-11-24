# Mounted Shield rows (docs/missions/mounted-shield/ACCEPTANCE.md) in the Sprint 6 feat family. The driver
# (src/KingmakerMountedCombat/Diagnostics/Sprint6ShieldScenario.cs) only records native observations; PASS is decided here.
# Every expected amount is derived from the mission's rule (shield base + Shield Focus + Greater Shield Focus once each,
# never enhancement) and compared with the production amount, the mount's native AC stat and its modifiers, native
# RuleCalculateAC results and the production Mounted Combat check record.
$script:KmcShieldFeatGuid='671a301bd1774677a8bfa2873d33b191'
$script:KmcShieldFocusGuid='ac57069b6bf8c904086171683992a92a'
$script:KmcMountedCombatGuid='a7d2e94c6f1b4830b5c9e28d17f3a604'
$script:KmcMountedArcheryGuid='b81926bca1d24a3997cdc8471a84014b'
$script:KmcShieldStockBase=@{buckler=1;light=1;heavy=2;tower=4}
# Verified stock items (installed 2.1.7b sharedassets1.assets; supplemental review PN6-F4): key, item GUID, armor type, enhancement.
$script:KmcShieldStock=[ordered]@{
 buckler=@('78aef04821150bd479314bc974ea73e2','26fcc43f7d20374498d2e1643381d345',0); light=@('a85d51d0fb905f940b951eec60388bac','d38e8ea23ce653c4582eb3e002555483',0)
 heavy=@('f4cef3ba1a15b0f4fa7fd66b602ff32b','d1b05b901bab9524388ebfa0435902a6',0); tower=@('6a1a429f023e5134fb16a4962cb79257','5f0f4b6e480e7054b8592b5a8b55854a',0)
 heavyPlus1=@('5c7b898a1bfb6cb4f8c14a0ebc143abe','d1b05b901bab9524388ebfa0435902a6',1); heavyPlus3=@('7e23918977b18524591204c93fae0a37','d1b05b901bab9524388ebfa0435902a6',3)
 heavyPlus5=@('0b09d902ff100524dbec6e3d3d93dc3b','d1b05b901bab9524388ebfa0435902a6',5)}
$script:KmcMageShieldBuff='9c0fa9b438ada3f43864be8dd8b3e741'

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
 # Each native rule chose flat-footedness itself (outside combat it can), so it is compared with the matching stat (review PN8-F3).
 if($null -eq $S.ruleMeleeFlatFooted -or $null -eq $S.ruleTouchFlatFooted){ throw ('Mounted Shield '+$What+': the native AC rules did not record their flat-footed choice') }
 $melee=$(if($S.ruleMeleeFlatFooted -eq $true){[int]$S.flatFooted}else{[int]$S.ac}); $touch=$(if($S.ruleTouchFlatFooted -eq $true){[int]$S.flatFootedTouch}else{[int]$S.touch})
 if([int]$S.ruleMelee -ne $melee -or [int]$S.ruleTouch -ne $touch){ throw ('Mounted Shield '+$What+': the native AC rule disagrees with the stat') }
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
  # The positive three-feat observation (MS-20, review PN8-F5): exactly one of each feat, last, in the order Mounted Combat,
  # Mounted Archery, Mounted Shield. An Archery fault is MS-22's isolation case, not this one.
  if([int]$s.combat -ne 1 -or [int]$s.shield -ne 1 -or [int]$s.archery -ne 1 -or $s.kmcTailExact -ne $true -or
    ((@($s.kmcTail) | ForEach-Object { [string]$_ }) -join ',') -cne (@($script:KmcMountedCombatGuid,$script:KmcMountedArcheryGuid,$script:KmcShieldFeatGuid) -join ',')){ throw 'Mounted Shield three-feat publication differs (each KMC feat once in the exact tail)' }
 }
 # Each three-feat row is one distinct list of the producer's own selection inventory, the ordinary and fighter lists among
 # them (review PN9-F5).
 $inventory=@{}; foreach($i in @($E.selections)){ $inventory[[string]$i.guid]=[string]$i.name }
 $tailGuids=@($E.threeFeatSelections | ForEach-Object { [string]$_.guid })
 $inventoryGuids=@($E.selections | ForEach-Object { [string]$_.guid })
 # Exactly one row per inventoried list (review PN10-F5): the same unique GUID set on both sides.
 if(@($inventoryGuids | Select-Object -Unique).Count -ne $inventoryGuids.Count -or (($inventoryGuids | Sort-Object) -join ',') -cne (($tailGuids | Sort-Object) -join ',')){ throw 'Mounted Shield a supported selection supplied no three-feat observation' }
 if(@($E.threeFeatSelections).Count -lt 2 -or @($tailGuids | Select-Object -Unique).Count -ne $tailGuids.Count -or @($E.threeFeatSelections | Where-Object { -not $inventory.ContainsKey([string]$_.guid) -or $inventory[[string]$_.guid] -cne [string]$_.name }).Count -ne 0 -or
   @($E.threeFeatSelections | Where-Object { [string]$_.name -ceq 'BasicFeatSelection' }).Count -ne 1 -or @($E.threeFeatSelections | Where-Object { [string]$_.name -ceq 'FighterFeatSelection' }).Count -ne 1){ throw 'Mounted Shield is not published in the ordinary and fighter lists' }
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
 # A positive native eligibility observation on an available (unselected) ordinary choice and an available fighter choice,
 # with both prerequisites in the preview (review PN9-F5).
 foreach($list in 'BasicFeatSelection','FighterFeatSelection'){
  if(@($rows | Where-Object { [string]$_.selection -ceq $list -and $_.offered -eq $true -and $_.selected -ne $true -and $_.mountedCombat -eq $true -and $_.shieldFocus -eq $true -and $_.eligible -eq $true }).Count -lt 1){ throw ('Mounted Shield was never natively eligible on an available '+$list+' choice with both prerequisites') }
 }
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
  if([string]$s.shield.blueprint -cne $script:KmcShieldStock[$k][0] -or [int]$s.shield.armorEnhancement -ne 0){ throw ('Mounted Shield '+$k+': the shield is not the verified mundane stock item') }
  if([int]$s.facts.base -ne $script:KmcShieldStockBase[$k] -or $s.facts.focus -ne $true -or $s.facts.greater -ne $false -or $s.facts.effectiveShield -ne $true){ throw ('Mounted Shield '+$k+': the base or focus terms differ from the stock data') }
  Assert-KmcShieldAc $s $none $expected[$k] $k
 }
 # Each verified stock heavy shield +1, +3 and +5 by its native armor enhancement (review PN6-F4).
 $exercised=@()
 foreach($p in @($d.PSObject.Properties | Where-Object { $_.Name -clike 'heavyEnhanced*' })){
  $s=$p.Value; Assert-KmcShieldSnapshot $s 3 $p.Name
  $want=[int]$s.requestedEnhancement; $stock=$script:KmcShieldStock['heavyPlus'+$want]
  if($null -eq $stock -or [string]$s.shield.blueprint -cne $stock[0] -or [int]$s.shield.armorEnhancement -ne $want -or [int]$s.facts.base -ne 2){ throw ('Mounted Shield '+$p.Name+' is not the verified stock heavy shield +'+$want+' by its native armor enhancement') }
  Assert-KmcShieldAc $s $none 3 $p.Name
  $exercised+=$want
 }
 if((($exercised | Sort-Object) -join ',') -cne '1,3,5'){ throw 'Mounted Shield the +1, +3 and +5 stock heavy shields were not each observed once' }
 Assert-KmcShieldSnapshot $d.heavyAgain 3 'heavy again'
 Assert-KmcShieldSnapshot $d.greater 4 'Greater Shield Focus'; Assert-KmcShieldAc $d.greater $none 4 'Greater Shield Focus'
 if($d.greater.facts.greater -ne $true){ throw 'Mounted Shield Greater Shield Focus was not read' }
 Assert-KmcShieldSnapshot $d.greaterRemoved 3 'Greater Shield Focus removed'
 foreach($n in 2,3,4){
  $s=Get-KmcShieldProperty $d ('competing'+$n); Assert-KmcShieldSnapshot $s 3 ('competing '+$n)
  if(@($s.shieldModifiers | Where-Object { $_.kmc -ne $true -and [int]$_.value -eq $n }).Count -ne 1){ throw ('Mounted Shield the competing +'+$n+' shield source is missing') }
  Assert-KmcShieldAc $s $none ([Math]::Max(3,$n)) ('competing '+$n)
 }
 # The stock Shield spell itself (review PN6-F3): its own modifier on the mount, or an explicit unavailable observation.
 $ms=$E.mountSpell
 if($null -eq $ms -or [string]$ms.guid -cne $script:KmcMageShieldBuff){ throw 'Mounted Shield the mount Shield spell observation is missing' }
 if($null -eq (Get-KmcShieldProperty $ms 'unavailable')){
  $spellMods=@($d.mountSpell.shieldModifiers | Where-Object { [string]$_.sourceGuid -ceq $script:KmcMageShieldBuff -and [int]$_.value -eq [int]$ms.value })
  if($ms.applied -ne $true -or $spellMods.Count -ne 1 -or [int]$ms.value -lt 1){ throw 'Mounted Shield the stock Shield spell is not the source of the mount''s competing bonus' }
  Assert-KmcShieldSnapshot $d.mountSpell 3 'Shield spell on the mount'; Assert-KmcShieldAc $d.mountSpell $none ([Math]::Max(3,[int]$ms.value)) 'Shield spell on the mount'
 }
 Assert-KmcShieldSnapshot $d.otherSet 0 'other weapon set'
 if([int]$d.otherSet.riderSet -eq [int]$d.heavyAgain.riderSet){ throw 'Mounted Shield the weapon set did not change' }
 Assert-KmcShieldAc $d.otherSet $none 0 'other weapon set'
 Assert-KmcShieldSnapshot $d.setRestored 3 'weapon set restored'
 Assert-KmcShieldSnapshot $d.unequipped 0 'unequipped'
 if($d.unequipped.shieldInInventory -ne $true){ throw 'Mounted Shield the unequipped shield did not stay in the inventory' }
 Assert-KmcShieldSnapshot $d.riderSpellOnly 0 'rider Shield effect only'
 $rs=$E.riderSpell
 if($null -eq $rs -or [string]$rs.guid -cne $script:KmcMageShieldBuff){ throw 'Mounted Shield the rider Shield spell observation is missing' }
 if($null -eq (Get-KmcShieldProperty $rs 'unavailable') -and ($rs.applied -ne $true -or @($d.riderSpellOnly.riderShieldModifiers | Where-Object { [string]$_.sourceGuid -ceq $script:KmcMageShieldBuff -and $_.item -ne $true }).Count -ne 1)){ throw 'Mounted Shield the rider''s own stock Shield spell was not on the rider' }
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
 # The selection switch itself, observed through the native single selection (review PN9-F6).
 if([string]$E.selectedBeforeSwitch -cne [string]$d.bothFirst.rider -or [string]$E.selectedAtSecondSnapshots -cne [string]$d.bothSecond.rider -or [string]$E.selectedAfterReturn -cne [string]$d.bothFirst.rider){ throw 'Mounted Shield the UI selection switch between the riders was not observed' }
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
# The mount's shield terms at an attack's own native AC calculation (review PN7-F4): bound to that attack, its result the
# attack's TargetAC, a melee weapon AC (not touch).
function Get-KmcShieldAttackContext($Measure,[string]$Identity,[string]$What){
 $row=@($Measure.attackRules | Where-Object { [string]$_.identity -ceq $Identity })
 if($row.Count -ne 1){ throw ('Mounted Shield '+$What+' has no single observed attack') }
 $context=Get-KmcShieldProperty $row[0] 'acContext'
 if($null -eq $context -or [int]$context.acResult -ne [int]$row[0].ac -or [string]$context.acAttackType -cnotmatch '^(Melee|Ranged)$' -or [string]$context.attackType -cne [string]$context.acAttackType){ throw ('Mounted Shield '+$What+' carries no shield terms from its own AC calculation') }
 [pscustomobject]@{row=$row[0];context=$context}
}
# The record that completed a step (reviews PN9-F3, PN10-F3): the one the driver recorded as completing it, of the measured
# rider and mount, its attack rule by the same attacker on the same mount; for a planned step also the attempt that consumed
# the plan. Further records of the same step (a legitimate follow-up strike) are validated by the all-attempt checks.
function Get-KmcShieldRecord($Measure,[string]$Case,[int]$Step,$E){
 $identity=[string](Get-KmcShieldProperty (Get-KmcShieldProperty $E 'stepRecords') ([string]$Step))
 $found=@($Measure.records | Where-Object { [string]$_.case -ceq $Case -and [int]$_.step -eq $Step -and [string]$_.ruleIdentity -ceq $identity -and [string]$_.rider -ceq [string]$Measure.rider -and [string]$_.mountId -ceq [string]$Measure.mount })
 if([string]::IsNullOrEmpty($identity) -or $found.Count -ne 1){ throw ('Mounted Shield '+$Case+' step '+$Step+': the record that completed the step is not one record of the measured pair') }
 Assert-KmcShieldRecordRule $Measure $found[0]
 if($null -ne $script:KmcShieldPlans[$Case] -and $script:KmcShieldPlans[$Case].Contains([string]$Step)){
  $planned=Get-KmcShieldProperty (Get-KmcShieldProperty $E 'plannedAttempts') ([string]$Step)
  if($null -eq $planned -or [string]$planned.identity -cne $identity -or [string]$planned.attacker -cne [string]$found[0].attacker -or [string]$planned.rider -cne [string]$Measure.rider -or [string]$planned.mount -cne [string]$Measure.mount){ throw ('Mounted Shield '+$Case+' step '+$Step+': the planned attempt is not the record that completed the step') }
 }
 $found[0]
}
# A defense record and its native attack rule name the same attacker and the same mount (review PN10-F3), and the attack total
# the record compares against is the native attack's own d20 and bonus (review PN12-F4).
function Assert-KmcShieldRecordRule($Measure,$R){
 $rule=@($Measure.attackRules | Where-Object { [string]$_.identity -ceq [string]$R.ruleIdentity })
 if($rule.Count -ne 1 -or [string]$rule[0].attacker -cne [string]$R.attacker -or [string]$rule[0].target -cne [string]$R.mountId){ throw 'Mounted Shield a defense record''s attack rule names another attacker or mount' }
 if([int]$rule[0].d20 -ne [int]$R.attackD20 -or [int]$rule[0].bonus -ne [int]$R.attackBonus -or [int]$R.attackResult -ne [int]$rule[0].d20+[int]$rule[0].bonus){ throw 'Mounted Shield a defense record''s attack total is not the native attack''s own' }
}
function Assert-KmcShieldOtherPairsNatural($Measure,[string]$Case){
 foreach($x in @($Measure.records | Where-Object { [string]$_.case -ceq $Case -and ([string]$_.rider -cne [string]$Measure.rider -or [string]$_.mountId -cne [string]$Measure.mount) })){
  if($x.forced -eq $true){ throw ('Mounted Shield '+$Case+': another pair''s defense received a planned roll') }
 }
}
# The planned steps of each row, from the scenario (review PN11-F5): step -> plan.
$script:KmcShieldPlans=@{ 'MS-check-rt'=[ordered]@{'2'='equal';'4'='success';'9'='success'}; 'MS-check-tb'=[ordered]@{'2'='success';'6'='equal'} }
# Every defense record of a row (reviews PN10-F3, PN11-F4, PN11-F5): its attack rule on its own attacker and mount; the planned
# attempts exactly the scenario's planned steps, each the record that completed its step, made by the row's designated hostile;
# the forced records exactly those attempts; and every refusal leaves the hit standing and the rider's Swift, window, use and
# debt as they were.
function Assert-KmcShieldEstablishedRider($Measure,$Pairs,$X){
 $known=@{ ([string]$Measure.mount)=[string]$Measure.rider }
 if($null -ne $Pairs){ $known[[string]$Pairs.steps.bothSecond.mount]=[string]$Pairs.steps.bothSecond.rider }
 if($known.ContainsKey([string]$X.mountId) -and $known[[string]$X.mountId] -cne [string]$X.rider){ throw 'Mounted Shield a rider defended a mount established for another rider' }
}
function Assert-KmcShieldRowRecords($Measure,$E,[string]$Case){
 $plans=$script:KmcShieldPlans[$Case]; $planned=Get-KmcShieldProperty $E 'plannedAttempts'; $steps=Get-KmcShieldProperty $E 'stepRecords'
 $hostile=[string](Get-KmcShieldProperty $E 'hostile')
 if([string]::IsNullOrWhiteSpace($hostile)){ throw ('Mounted Shield '+$Case+': the row''s designated hostile was not recorded') }
 $keys=@(); if($null -ne $planned){ $keys=@($planned.PSObject.Properties | ForEach-Object { $_.Name }) }
 if((($keys | Sort-Object) -join ',') -cne ((@($plans.Keys) | Sort-Object) -join ',')){ throw ('Mounted Shield '+$Case+': the planned attempts are not exactly the scenario''s planned steps') }
 $consumers=@{}
 foreach($k in $plans.Keys){
  $p=Get-KmcShieldProperty $planned $k
  if([string]$p.plan -cne [string]$plans[$k] -or [string]$p.attacker -cne $hostile -or [string](Get-KmcShieldProperty $steps $k) -cne [string]$p.identity){ throw ('Mounted Shield '+$Case+' step '+$k+': the planned attempt is not the designated hostile''s completing attack') }
  $consumers[[string]$p.identity]=$true
 }
 foreach($x in @($Measure.records | Where-Object { [string]$_.case -ceq $Case })){
  Assert-KmcShieldRecordRule $Measure $x
  if(($x.forced -eq $true) -ne $consumers.ContainsKey([string]$x.ruleIdentity)){ throw ('Mounted Shield '+$Case+': a forced roll outside the planned attempts, or a planned attempt not forced') }
  if($x.attempted -ne $true){
   if([string]$x.resultAfter -cne [string]$x.resultBefore -or $x.negated -eq $true -or [double]$x.swiftAfter -ne [double]$x.swiftBefore -or
     ($x.stateAfter | ConvertTo-Json -Compress) -cne ($x.stateBefore | ConvertTo-Json -Compress)){ throw ('Mounted Shield '+$Case+': a refused defense changed the hit or the rider''s Swift, window, use or debt') }
   # The bound native attack itself still hits, keeps its critical state and deals its damage (review PN12-F2).
   $rule=@($Measure.attackRules | Where-Object { [string]$_.identity -ceq [string]$x.ruleIdentity })[0]
   if($rule.isHitAtEvent -ne $true -or $rule.isHitAfterRule -ne $true -or [string]$rule.resultAfterRule -cne [string]$rule.resultAtEvent -or [string]$rule.resultAtEvent -cmatch '^(Parried|Miss)$' -or
     [bool]$x.criticalAfter -ne [bool]$x.criticalBefore -or [int]$rule.damageRules -lt 1){ throw ('Mounted Shield '+$Case+': a refused defense''s native hit did not stand') }
  }
 }
}
function Get-KmcShieldCheckRow($Measure,$R){
 # The record's rider's attempts paired in order with that rider's own native rolls (other riders' are set aside).
 $checks=@($Measure.skillChecks | Where-Object { [string]$_.actor -ceq [string]$R.rider })
 if($script:KmcShieldUnrelated -and [string]$R.rider -ceq [string]$Measure.rider){ $checks=@(Remove-KmcSprint6ShieldUnrelatedCheck $checks $script:KmcShieldUnrelated $Measure.rider) }
 $attempted=@($Measure.records | Where-Object { $_.attempted -eq $true -and [string]$_.rider -ceq [string]$R.rider })
 $index=-1; for($i=0;$i -lt $attempted.Count;$i++){ if([string]$attempted[$i].ruleIdentity -ceq [string]$R.ruleIdentity){ $index=$i } }
 if($index -lt 0 -or $index -ge $checks.Count){ throw 'Mounted Shield a recorded check has no observed native roll' }
 $row=$checks[$index]
 if([int]$row.result -ne [int]$R.checkResult -or [int]$row.d20 -ne [int]$R.checkD20 -or [int]$row.dc -ne [int]$R.checkDc){ throw 'Mounted Shield the paired native roll is not this attempt''s roll' }
 $row
}
# The use was actually committed (supplemental review PN5-F8), not only labeled: the Sprint 6 state and cooldown evidence.
function Assert-KmcShieldCommitted($R,[string]$What){
 switch -CaseSensitive ([string]$R.commitment){
  # From an eligible native state (no window, use or debt; for a Swift spent now, the native HasSwiftAction state, cooldown 0),
  # exactly one native Swift charge (six seconds) or exactly one newly incurred debt, and the complete expected state after
  # (reviews PN6-F7, PN7-F5). An off-turn borrow may follow a Swift legitimately spent on the rider's own turn (cooldown kept).
  'RealTimeWindow' { if([string]$R.mode -cne 'RT' -or $R.stateBefore.window -ne $false -or $R.stateBefore.spent -ne $false -or $R.stateBefore.owed -ne $false -or [double]$R.swiftBefore -ne 0 -or
    $R.stateAfter.window -ne $true -or $R.stateAfter.spent -ne $false -or $R.stateAfter.owed -ne $false -or [double]$R.swiftAfter -lt 5.5 -or [double]$R.swiftAfter -gt 6.05){ throw ('Mounted Shield '+$What+': the real-time use did not open exactly one window with exactly one Swift charge') } }
  'SpendCurrentSwift' { if([string]$R.mode -cne 'TB' -or $R.stateBefore.window -ne $false -or $R.stateBefore.spent -ne $false -or $R.stateBefore.owed -ne $false -or [double]$R.swiftBefore -ne 0 -or
    $R.stateAfter.window -ne $false -or $R.stateAfter.spent -ne $true -or $R.stateAfter.owed -ne $false -or [double]$R.swiftAfter -lt 5.9 -or [double]$R.swiftAfter -gt 6.05){ throw ('Mounted Shield '+$What+': the own-turn use did not spend exactly this turn''s available Swift action') } }
  'BorrowNextSwift' { if([string]$R.mode -cne 'TB' -or $R.stateBefore.window -ne $false -or $R.stateBefore.spent -ne $false -or $R.stateBefore.owed -ne $false -or [double]$R.swiftBefore -lt 0 -or
    $R.stateAfter.window -ne $false -or $R.stateAfter.spent -ne $true -or $R.stateAfter.owed -ne $true -or [double]$R.swiftAfter -ne [double]$R.swiftBefore){ throw ('Mounted Shield '+$What+': the off-turn use did not incur exactly one new debt') } }
  default { throw ('Mounted Shield '+$What+': no commitment for an attempted check') }
 }
}
function Assert-KmcShieldRecord($R,[string]$What,$Measure){
 Assert-KmcShieldCommitted $R $What
 $row=Get-KmcShieldCheckRow $Measure $R
 if([int]$row.kmcShieldModifiers -ne 1 -or [int]$row.kmcShieldTotal -ne 3){ throw ('Mounted Shield '+$What+': the native roll did not hold exactly one +3 Mounted Shield modifier') }
 # KMC's own +3 is judged by its identified modifier in the roll above; the aggregate bonus may hold native penalties (PN12-F1).
 if([int]$R.shieldBonus -ne 3 -or [int]$R.checkResult -ne [int]$R.checkD20+[int]$R.checkStat+[int]$R.checkBonus -or [int]$R.checkDc -ne [int]$R.attackResult+1){ throw ('Mounted Shield '+$What+': the native check did not carry the +3 addition exactly once') }
 if((Get-KmcShieldProperty $R 'plannedUnattainable') -eq $true){ throw ('Mounted Shield '+$What+': the planned total was unattainable at the native roll; the measurement failed') }
 if($R.forced -ne $true){ throw ('Mounted Shield '+$What+': the boundary row is not a labeled planned total') }
 if([int]$R.checkD20 -lt 1 -or [int]$R.checkD20 -gt 20){ throw ('Mounted Shield '+$What+': the planned total needed a die outside 1..20') }
}

function Assert-KmcShieldCheckRealTime($Measure,$E,$Pairs){
 $c='MS-check-rt'
 $r=Get-KmcShieldRecord $Measure $c 2 $E
 if([int]$r.checkResult -ne [int]$r.attackResult -or $r.attempted -ne $true -or [string]$r.commitment -cne 'RealTimeWindow'){ throw 'Mounted Shield equality was not the exact shield-modified total' }
 Assert-KmcShieldRecord $r 'equality' $Measure; Assert-KmcSprint6Stands $Measure $r 'Mounted Shield equality'
 $r=Get-KmcShieldRecord $Measure $c 4 $E
 if([int]$r.checkResult -ne [int]$r.attackResult+1){ throw 'Mounted Shield success was not one above the attack result' }
 Assert-KmcShieldRecord $r 'success' $Measure; Assert-KmcSprint6Negated $Measure $r 'Mounted Shield success'
 $u=$E.equipmentDuringUse
 # The same-frame samples' Swift cooldown and window expiry too (review PN12-F6): no refund, no restarted or extended window.
 foreach($k in 'spent','owed','window','windowIdentity','swift','windowEndTicks'){
  if($null -eq $u.before.$k -or [string]$u.before.$k -cne [string]$u.unequipped.$k -or [string]$u.before.$k -cne [string]$u.reequipped.$k){ throw 'Mounted Shield an equipment change refunded or restarted the running use' }
 }
 Assert-KmcSprint6Refused $Measure (Get-KmcShieldRecord $Measure $c 5 $E) 'UsageSpent' 'Mounted Shield spent use'
 Assert-KmcShieldSnapshot $E.steps.spentRefused 3 'spent use'
 Assert-KmcSprint6Refused $Measure (Get-KmcShieldRecord $Measure $c 7 $E) 'PreferenceOff' 'Mounted Shield preference off'
 Assert-KmcShieldSnapshot $E.steps.preferenceOffRefused 3 'preference off'
 # A stronger shield source on the mount covers its AC, and the production check made under it still carries +3.
 $s=$E.steps.competingBeforeCheck; Assert-KmcShieldSnapshot $s 3 'competing +4 before the check'
 if(@($s.shieldModifiers | Where-Object { $_.kmc -ne $true -and [int]$_.value -eq 4 }).Count -ne 1){ throw 'Mounted Shield the competing +4 was not on the mount at the check' }
 $r=Get-KmcShieldRecord $Measure $c 9 $E
 if([int]$r.checkResult -ne [int]$r.attackResult+1){ throw 'Mounted Shield the check under the stronger shield was not the planned success' }
 # The +4 covered the AC of that very attack (review PN7-F4): at its own AC calculation the mount held Mounted Shield's +3 and
 # the competing +4, so Mounted Shield changed no AC there.
 $cc=(Get-KmcShieldAttackContext $Measure ([string]$r.ruleIdentity) 'the check under a stronger shield').context
 if([int]$cc.ownedCount -ne 1 -or [int]$cc.owned -ne 3 -or [int]$cc.otherShield -ne 4 -or [int]$cc.effective -ne 0){ throw 'Mounted Shield the competing +4 did not cover the AC of the checked attack' }
 Assert-KmcShieldRecord $r 'check under a stronger shield' $Measure; Assert-KmcSprint6Negated $Measure $r 'Mounted Shield check under a stronger shield'
 if([int]$E.unrelatedMobility.bonus -ne 0 -or [int]$E.unrelatedMobility.stat -ne [int]$E.unrelatedMobility.displayed){ throw 'Mounted Shield an unrelated Mobility check received an addition' }
 $unrelatedRow=@($Measure.skillChecks | Where-Object { [int]$_.frame -eq [int]$E.unrelatedMobility.frame -and [int]$_.dc -eq [int]$E.unrelatedMobility.dc })
 if($unrelatedRow.Count -ne 1 -or [int]$unrelatedRow[0].kmcShieldModifiers -ne 0){ throw 'Mounted Shield an unrelated Mobility roll held a Mounted Shield modifier' }
 # Every attempted real-time use on this row, planned or natural, is a real-time window with exactly one Swift charge from an
 # eligible state (review PN8-F4); the forced-total assertions stay with the planned attacks.
 Assert-KmcShieldOtherPairsNatural $Measure $c
 # Every attempted defense on this row, of every pair (review PN10-F4): a real-time window with exactly one Swift charge from an
 # eligible state, its attack rule on its own mount, and its own roll holding its pair's Mounted Shield amount (the measured
 # pair +3, forced or natural, review PN9-F4).
 Assert-KmcShieldRowRecords $Measure $E $c
 foreach($x in @($Measure.records | Where-Object { [string]$_.case -ceq $c -and $_.attempted -eq $true })){
  $measured=[string]$x.rider -ceq [string]$Measure.rider -and [string]$x.mountId -ceq [string]$Measure.mount
  # A known mount is defended only by the rider established for it (review PN12-F5).
  Assert-KmcShieldEstablishedRider $Measure $Pairs $x
  # Each pair's own amount (review PN11-F6): the measured pair +3; the identified second pair its observed amount; anyone else 0.
  $expected=$(if($measured){3}elseif($null -ne $Pairs -and [string]$x.rider -ceq [string]$Pairs.steps.bothSecond.rider -and [string]$x.mountId -ceq [string]$Pairs.steps.bothSecond.mount){[int]$Pairs.steps.bothSecond.amount}else{0})
  if([int]$x.shieldBonus -ne $expected){ throw 'Mounted Shield a check carried another pair''s Mounted Shield amount' }
  if([string]$x.mode -cne 'RT' -or [string]$x.commitment -cne 'RealTimeWindow'){ throw 'Mounted Shield a real-time row attempt was not a real-time window' }
  Assert-KmcShieldCommitted $x ('real-time attempt '+$x.step)
  Assert-KmcShieldRecordRule $Measure $x
  $row=Get-KmcShieldCheckRow $Measure $x
  if([int]$row.kmcShieldModifiers -ne $(if([int]$x.shieldBonus -gt 0){1}else{0}) -or [int]$row.kmcShieldTotal -ne [int]$x.shieldBonus){ throw 'Mounted Shield a real-time attempt''s native roll does not hold its pair''s Mounted Shield amount' }
 }
 $b=$E.band; $miss=Get-KmcShieldProperty $E 'bandMiss'
 # The qualifying miss may arrive before the driver's first strike (review PN11-F7): 0..120 driver strikes.
 if([int]$b.amount -ne 3 -or $null -eq $miss -or [int]$E.bandAttempts -lt 0 -or [int]$E.bandAttempts -gt 120){ throw 'Mounted Shield no miss caused only by its AC was identified within the strike bound' }
 # The AC change Mounted Shield made at that very attack (review PN5-F7, PN6-F6): its own modifier above the strongest other
 # Shield modifier, both observed with the attack.
 $context=Get-KmcShieldProperty $miss 'acContext'
 if($null -eq $context){ throw 'Mounted Shield the band miss carries no AC context of its own' }
 [void](Get-KmcShieldAttackContext $Measure ([string]$miss.identity) 'the band miss')
 $effective=[int]$context.effective
 if($effective -lt 1 -or [int]$context.ownedCount -ne 1 -or [int]$context.owned -ne [int]$b.amount -or $effective -ne [Math]::Max(0,[int]$context.owned-[Math]::Max(0,[int]$context.otherShield))){ throw 'Mounted Shield the band miss''s AC change is not derived from the modifiers at that attack' }
 # The row's designated hostile, recorded when the row starts (review PN11-F7); other hostiles' attacks do not matter here.
 $hostile=@([string]$E.hostile)
 $row=@($Measure.attackRules | Where-Object { [string]$_.identity -ceq [string]$miss.identity })
 if($row.Count -ne 1 -or $hostile.Count -ne 1 -or [string]$row[0].attacker -cne $hostile[0] -or [string]$row[0].target -cne [string]$Measure.mount -or $row[0].isHitAtEvent -eq $true -or [int]$row[0].d20 -le 1 -or
   ($row[0].acContext | ConvertTo-Json -Compress) -cne ($context | ConvertTo-Json -Compress) -or
   ([int]$row[0].d20+[int]$row[0].bonus) -ge [int]$row[0].ac -or ([int]$row[0].d20+[int]$row[0].bonus) -lt [int]$row[0].ac-$effective){ throw 'Mounted Shield the identified band miss is not a miss that reached the AC without Mounted Shield' }
 if([string]$miss.identity -cin @($Measure.records | ForEach-Object { [string]$_.ruleIdentity })){ throw 'Mounted Shield a miss caused by its AC was evaluated by Mounted Combat' }
}

function Assert-KmcShieldCheckTurnBased($Measure,$E){
 $c='MS-check-tb'
 # Every attempted turn-based use on this row is a turn-based Swift spend or borrow with its exact transition (review PN8-F4).
 Assert-KmcShieldOtherPairsNatural $Measure $c
 Assert-KmcShieldRowRecords $Measure $E $c
 foreach($x in @($Measure.records | Where-Object { [string]$_.case -ceq $c -and $_.attempted -eq $true })){
  $measured=[string]$x.rider -ceq [string]$Measure.rider -and [string]$x.mountId -ceq [string]$Measure.mount
  Assert-KmcShieldEstablishedRider $Measure $null $x
  if(-not $measured -and [int]$x.shieldBonus -ne 0){ throw 'Mounted Shield a check carried another pair''s Mounted Shield amount' }
  if([string]$x.mode -cne 'TB' -or [string]$x.commitment -cnotin @('SpendCurrentSwift','BorrowNextSwift')){ throw 'Mounted Shield a turn-based row attempt was not a turn-based commitment' }
  # The rider's own turn spends this turn's Swift; any other moment borrows the next (review PN9-F4).
  if(($x.riderTurn -eq $true -and [string]$x.commitment -cne 'SpendCurrentSwift') -or ($x.riderTurn -ne $true -and [string]$x.commitment -cne 'BorrowNextSwift')){ throw 'Mounted Shield a turn-based commitment does not match the rider''s turn' }
  Assert-KmcShieldCommitted $x ('turn-based attempt '+$x.step)
  $row=Get-KmcShieldCheckRow $Measure $x
  if(($measured -and [int]$x.shieldBonus -ne 3) -or [int]$row.kmcShieldModifiers -ne $(if([int]$x.shieldBonus -gt 0){1}else{0}) -or [int]$row.kmcShieldTotal -ne [int]$x.shieldBonus){ throw 'Mounted Shield a turn-based attempt''s native roll does not hold its pair''s Mounted Shield amount' }
 }
 $r=Get-KmcShieldRecord $Measure $c 2 $E
 if([string]$r.mode -cne 'TB' -or $r.riderTurn -ne $true -or [string]$r.commitment -cne 'SpendCurrentSwift' -or [int]$r.checkResult -ne [int]$r.attackResult+1){ throw 'Mounted Shield own-turn success differs' }
 Assert-KmcShieldRecord $r 'own-turn success' $Measure; Assert-KmcSprint6Negated $Measure $r 'Mounted Shield own-turn success'
 Assert-KmcSprint6Refused $Measure (Get-KmcShieldRecord $Measure $c 3 $E) 'UsageSpent' 'Mounted Shield second own-turn attack'
 Assert-KmcShieldSnapshot $E.steps.ownTurnSecondRefused 3 'turn-based spent use'
 $r=Get-KmcShieldRecord $Measure $c 6 $E
 if($r.riderTurn -ne $false -or [string]$r.commitment -cne 'BorrowNextSwift' -or [int]$r.checkResult -ne [int]$r.attackResult){ throw 'Mounted Shield off-turn equality differs' }
 Assert-KmcShieldRecord $r 'off-turn equality' $Measure; Assert-KmcSprint6Stands $Measure $r 'Mounted Shield off-turn equality'
}

function Assert-KmcSprint6ShieldRows($Measure,$Rows,[string]$Scenario){
 $m=$Measure.mountedShield
 if($null -eq $m -or [string]$m.contract -cne 'native-mounted-shield-v1' -or [string]$m.featGuid -cne $script:KmcShieldFeatGuid -or [string]::IsNullOrWhiteSpace([string]$m.setupDisclosure)){ throw 'Mounted Shield measurement identity or setup disclosure differs' }
 # Stock items only (supplemental review PN5-F9): every shield the driver used is a stock blueprint loaded from the game's assets.
 $stock=Get-KmcShieldProperty $m 'shieldBlueprints'
 if($null -eq $stock){ throw 'Mounted Shield the stock shield identities were not recorded' }
 foreach($key in $script:KmcShieldStock.Keys){
  $item=Get-KmcShieldProperty $stock $key; $want=$script:KmcShieldStock[$key]
  if($null -eq $item -or $item.stockVerified -ne $true -or [string]$item.guid -cne $want[0] -or [string]$item.armorType -cne $want[1] -or [int]$item.requestedEnhancement -ne $want[2] -or [string]$item.name -cne [string]$item.expectedName){ throw ('Mounted Shield the stock shield '+$key+' is not the verified stock item') }
 }
 $script:KmcShieldUnrelated=$(if($Scenario -ceq 'sprint6-shield-rt'){ $Rows['MS-check-rt'].evidence.unrelatedMobility }else{ $null })
 if($Scenario -ceq 'sprint6-shield-rt'){
  Assert-KmcShieldSelection $Rows['MS-selection'].evidence $true
  Assert-KmcShieldEquipment $Rows['MS-equipment'].evidence
  Assert-KmcShieldPairs $Rows['MS-pairs'].evidence
  Assert-KmcShieldCheckRealTime $Measure $Rows['MS-check-rt'].evidence $Rows['MS-pairs'].evidence
 } else {
  Assert-KmcShieldSelection $Rows['MS-rider-feat'].evidence $false
  Assert-KmcShieldCheckTurnBased $Measure $Rows['MS-check-tb'].evidence
 }
 # Every forced defense of the whole measurement (acquisition to cleanup, any case or none) is one of the check rows' planned
 # attempts, in its own case (review PN12-F3).
 $authorized=@{}
 foreach($case in @($script:KmcShieldPlans.Keys)){
  $row=$Rows[$case]; if($null -eq $row){ continue }
  $planned=Get-KmcShieldProperty $row.evidence 'plannedAttempts'
  if($null -eq $planned){ continue }
  foreach($p in $planned.PSObject.Properties){ $authorized[$case+'|'+[string]$p.Value.identity+'|'+[string]$p.Value.rider+'|'+[string]$p.Value.mount+'|'+[string]$p.Value.attacker+'|'+$p.Name]=$true }
 }
 foreach($x in @($Measure.records | Where-Object { $_.forced -eq $true })){
  if(-not $authorized.ContainsKey([string]$x.case+'|'+[string]$x.ruleIdentity+'|'+[string]$x.rider+'|'+[string]$x.mountId+'|'+[string]$x.attacker+'|'+[string]$x.step)){ throw 'Mounted Shield a forced defense outside the authorized planned attempts' }
 }
 # The fixture actors' hands and every setup modifier must be verifiably restored (supplemental review PN4-F4).
 $drain=Get-KmcShieldProperty $m 'drain'
 if($null -eq $drain){ throw 'Mounted Shield fixture drain evidence is missing' }
 if(@($drain.PSObject.Properties | Where-Object { $_.Name -clike '*Error' }).Count -ne 0){ throw 'Mounted Shield a setup modifier or effect could not be removed at the drain' }
 # Every setup obligation released and verified (supplemental review PN5-F3).
 # Every setup removal fault of the row, kept across retries (review PN7-F3): one fault fails the row.
 if(@($drain.setup.faults).Count -ne 0 -or @($drain.setup.allFaults).Count -ne 0){ throw 'Mounted Shield a setup modifier removal faulted during the row' }
 if($drain.clean -ne $true -or @($drain.setup.remaining).Count -ne 0 -or @($drain.buffsLeft).Count -ne 0){ throw 'Mounted Shield setup modifiers, effects or equipment were not verifiably released at the drain' }
 $actors=@('rider'); if($Scenario -ceq 'sprint6-shield-rt'){ $actors+='owner' }
 foreach($who in $actors){ $x=Get-KmcShieldProperty $drain $who; if($null -eq $x -or @($x.errors).Count -ne 0 -or [int]$x.createdLeft -ne 0 -or [int]$x.handsChecked -lt 2 -or [int]$x.set -ne [int]$x.originalSet){ throw ('Mounted Shield fixture equipment was not verifiably restored for the '+$who) } }
}

# The persistence case (supplemental review PN4-F8). The producer (warm) artifact holds the state before the save, the archive
# it wrote and its continuation; the consumer (cold) artifact holds the restored state, the loaded archive and its
# continuation. Each is read in its own shape. Neither archive may hold a Mounted Shield source tag (the native save turns
# the fact off before it serializes); the cold rider and mount are bound to the authenticated continuation's actors.
function Assert-KmcShieldArchive($Scan,[string]$What,$Archive,[string]$FileName){
 if($null -eq $Scan -or $null -ne $Scan.error -or [string]$Scan.area -cnotmatch '^[0-9a-f]{32}$'){ throw ('Mounted Shield '+$What+' archive was not read') }
 if($null -ne $Archive -and ([string]$Scan.area -cne [string]$Archive.AreaId -or [string]$Scan.file -cne $FileName)){ throw ('Mounted Shield '+$What+' scan is not of the validated archive') }
 foreach($entry in @('party.json',([string]$Scan.area+'.json'))){
  if(@($Scan.entries | Where-Object { [string]$_.entry -ceq $entry -and $_.present -eq $true -and [int]$_.length -gt 0 }).Count -ne 1){ throw ('Mounted Shield '+$What+' archive lacks its nonempty '+$entry) }
 }
 if([int]$Scan.mountTags -ne 0 -or [int]$Scan.checkTags -ne 0){ throw ('Mounted Shield '+$What+' archive serialized a Mounted Shield modifier') }
}
# The shield the rider holds, as observed at that moment (review PN7-F6): the verified mundane stock heavy shield.
function Assert-KmcShieldObservedHeavy($S,[string]$What){
 if([string]$S.observedShieldBlueprint -cne $script:KmcShieldStock['heavy'][0] -or $null -eq $S.observedShieldItem -or $null -eq $S.observedShieldEnhancement -or [int]$S.observedShieldEnhancement -ne 0){ throw ('Mounted Shield '+$What+' is not observed holding the verified stock heavy shield') }
}
function Assert-KmcShieldOneCurrent($S,[string]$What){
 if($null -eq $S -or $S.mounted -ne $true -or $S.hasFeat -ne $true -or [int]$S.shieldBase -ne 2 -or [int]$S.amount -ne 3 -or [int]$S.ownedCount -ne 1 -or [int]@($S.ownedValues)[0] -ne 3 -or
   @($S.ownedByRiderFact)[0] -ne $true -or [string]$S.ledgerMount -cne [string]$S.mount -or [int]$S.otherMountOwned -ne 0){ throw ('Mounted Shield '+$What+' does not hold exactly one current contribution') }
}
function Assert-KmcSprint6ShieldPersistence($Request,$Observations,[bool]$Cold,$Archive){
 $o=$Observations
 if($null -eq $Archive){ throw 'Mounted Shield persistence has no validated archive to bind to' }
 $script:KmcShieldArchive=$Archive
 if(-not $Cold){
  $save=Get-KmcShieldProperty $o 'shieldAtSave'
  if($null -eq $save -or $save.hasShieldFocus -ne $true){ throw 'Mounted Shield source state before the save is missing' }
  Assert-KmcShieldOneCurrent $save 'the source before the save'
  Assert-KmcShieldArchive (Get-KmcShieldProperty $o 'shieldSavedArchive') 'the written' $Archive ([IO.Path]::GetFileName([string]$o.nativeWrite.path))
  $after=Get-KmcShieldProperty $o 'shieldWarmContinuation'; Assert-KmcShieldOneCurrent $after 'the warm continuation'
  Assert-KmcShieldFirstPair $o $save 'the source before the save'; Assert-KmcShieldFirstPair $o $after 'the warm continuation'
  $want=$script:KmcShieldStock['heavy']
  if($save.shieldIdentity.stockVerified -ne $true -or [string]$save.shieldIdentity.guid -cne $want[0] -or [string]$save.shieldIdentity.armorType -cne $want[1] -or [string]$save.requestedShieldBlueprint -cne $want[0]){ throw 'Mounted Shield the persistence shield is not the verified stock heavy shield' }
  Assert-KmcShieldObservedHeavy $save 'the source before the save'; Assert-KmcShieldObservedHeavy $after 'the warm continuation'
  if([string]$after.observedShieldItem -cne [string]$save.observedShieldItem){ throw 'Mounted Shield the warm continuation holds another item than the one saved' }
  # The producer's fixture equipment is restored after the measured continuation, verified (review PN7-F2).
  $eq=Get-KmcShieldProperty $o 'shieldEquipmentCleanup'
  if($null -eq $eq -or @($eq.errors).Count -ne 0 -or [int]$eq.createdLeft -ne 0 -or [int]$eq.handsChecked -lt 2 -or [int]$eq.set -ne [int]$eq.originalSet){ throw 'Mounted Shield the producer''s fixture equipment was not verifiably restored' }
  return
 }
 if($null -ne (Get-KmcShieldProperty $o 'shieldAtSave')){ throw 'Mounted Shield a cold artifact carries producer state' }
 $restore=Get-KmcShieldProperty $o 'shieldColdAtRestore'
 if($null -eq $restore -or $restore.hasFeat -ne $true -or [int]$restore.shieldBase -ne 2 -or [int]$restore.ownedCount -gt 1 -or [int]$restore.otherMountOwned -ne 0){ throw 'Mounted Shield the cold load restored a stale or doubled contribution' }
 # A contribution already present at restoration must itself be the current one (supplemental review PN5-F5).
 if([int]$restore.ownedCount -eq 1 -and ([int]@($restore.ownedValues)[0] -ne 3 -or @($restore.ownedByRiderFact)[0] -ne $true -or [string]$restore.ledgerMount -cne [string]$restore.mount -or
   [int]$restore.amount -ne 3 -or $restore.mounted -ne $true -or $restore.hasShieldFocus -ne $true)){ throw 'Mounted Shield the cold load restored a contribution that disagrees with the state at restoration' }
 Assert-KmcShieldArchive $restore.archive 'the loaded' $Archive ([string]$o.loadedDescriptor.fileName)
 $after=Get-KmcShieldProperty $o 'shieldColdContinuation'; Assert-KmcShieldOneCurrent $after 'the cold continuation'
 Assert-KmcShieldFirstPair $o $restore 'the cold restore'; Assert-KmcShieldFirstPair $o $after 'the cold continuation'
 Assert-KmcShieldObservedHeavy $restore 'the cold restore'; Assert-KmcShieldObservedHeavy $after 'the cold continuation'
 if([string]$after.observedShieldItem -cne [string]$restore.observedShieldItem){ throw 'Mounted Shield the cold continuation holds another item than the one restored' }
}
# The authenticated combat checkpoint orders its actors first rider, first mount, second rider, second mount
# (Chunk7CombatCheckpointDriver); the shield pair is the first pair in those exact roles.
function Assert-KmcShieldFirstPair($Observations,$S,[string]$What){
 $ids=@($Observations.combatContinuation.actorIds | ForEach-Object { [string]$_ })
 # The first pair as the validated archive records it (the Mammoth profile), so the roles are not taken from the observation.
 $pair=@($script:KmcShieldArchive.Pairs | Where-Object { [string]$_.ProfileId -ceq 'medium-humanoid-mammoth-v1' })
 if($ids.Count -ne 4 -or $pair.Count -ne 1 -or [string]$S.rider -cne [string]$pair[0].Rider.Id -or [string]$S.mount -cne [string]$pair[0].Mount.Id -or [string]$S.rider -cne $ids[0] -or [string]$S.mount -cne $ids[1]){ throw ('Mounted Shield '+$What+' is not the validated archive''s first rider on its first mount') }
}
