# Sprint 8 (PAL-01) external reader. The rows only record native observations; PASS is decided here.
function Get-KmcSprint8Cases([string]$Scenario){
 switch -CaseSensitive ($Scenario){
  'sprint8-paladin-choice-rt' { return @('S8-inventory','S8-weapon','S8-existing','S8-retrain') }
  'sprint8-paladin-mount-rt' { return @('S8-mount-horse','S8-control') }
  'sprint8-paladin-dog-rt' { return @('S8-dog') }
  'sprint8-paladin-mixed-rt' { return @('S8-mixed','S8-mixed-reverse','S8-archetype') }
  'sprint8-paladin-celestial-rt' { return @('S8-tiers','S8-smite') }
 }
 throw ('Sprint 8 scenario has no case list: '+$Scenario)
}
$script:KmcS8Bond='6e0f3b7ac1d94f2e8a51c7d0294be163'; $script:KmcS8MountSel='b14c9d27e6a3480fa97e03d5c18f6a42'
$script:KmcS8Horse='d97c20e5a84b4f16b3e87a1604cd2f95'; $script:KmcS8Dog='47ea1f96b0c24d38a6f5d2c83e917b04'
$script:KmcS8WeaponGuid='1c7cdc1605554954f838d85bbdd22d90'; $script:KmcS8Empty='472091361cf118049a2b4339c4ea836a'
$script:KmcS8HorseUnit='4016c7db400ab721ff125aef9e65e202'; $script:KmcS8DogUnit='b7a14f25d3c94e0d8e6f1a2c5d90e431'
# Native AddPet's rank-to-level table.
$script:KmcS8RankLevel=@(0,2,3,3,4,5,6,6,7,8,9,9,10,11,12,12,13,14,15,15,16)
function Assert-KmcS8Weapon($f,[string]$What){
 if($f.weaponBond -ne $true -or $f.mountBond -ne $false -or $null -ne $f.pet -or [int]$f.addPetFacts -ne 0 -or [int]$f.ownedPets -ne 0 -or $f.callAbility -ne $false -or [int]$f.callUses -ne 0 -or [int]$f.rank -ne 0){ throw ('Sprint 8 '+$What+': the weapon path holds mount facts, a pet or no weapon bond') }
 if([int]$f.weaponAbilities -lt 1 -or [int]$f.weaponResources -lt 1){ throw ('Sprint 8 '+$What+': the weapon path has no usable ability or uses') }
}
# Owner-side facts alone: a hired owner's pet spawns when the owner enters the world, not during the creation chain.
function Assert-KmcS8MountOwnerFacts($f,[string]$What,[string]$Choice){
 if($f.weaponBond -ne $false -or [int]$f.weaponUpgradeRanks -ne 0 -or [int]$f.weaponAbilities -ne 0 -or [int]$f.weaponResources -ne 0){ throw ('Sprint 8 '+$What+': a weapon-bond benefit survives on the mount path') }
 if($f.mountBond -ne $true -or [string]$f.mountChoice -cne $Choice -or [int]$f.addPetFacts -ne 1 -or $f.callAbility -ne $true){ throw ('Sprint 8 '+$What+': the mount bond or its call is missing') }
}
function Assert-KmcS8MountFacts($f,[string]$What,[string]$Unit,[string]$Choice){
 Assert-KmcS8MountOwnerFacts $f $What $Choice
 if([int]$f.ownedPets -ne 1){ throw ('Sprint 8 '+$What+': the mount bond does not own exactly one pet') }
 $p=$f.pet
 if($null -eq $p -or [string]$p.blueprint -cne $Unit -or [string]$p.master -cne [string]$f.id){ throw ('Sprint 8 '+$What+': the companion is not the exact owned mount') }
 if([int]$f.rank -ne [int]$f.paladinLevel){ throw ('Sprint 8 '+$What+': effective rank '+$f.rank+' is not the paladin level '+$f.paladinLevel) }
 if([int]$p.intelligenceBond -ne 1 -or [int]$p.intelligence -lt 6){ throw ('Sprint 8 '+$What+': the bonded mount''s Intelligence is below 6') }
}
function Assert-KmcS8SelectionOffered($Record,[string[]]$Selectable,[string[]]$Refused,[string]$What){
 if($null -eq $Record -or [int]$Record.level -lt 1 -or $Record.completeBeforeChoice -ne $false){ throw ('Sprint 8 '+$What+': the level was not held open by the native choice') }
 foreach($g in $Selectable){ if(@($Record.items|Where-Object{[string]$_.guid -ceq $g -and $_.canSelect -eq $true}).Count -ne 1){ throw ('Sprint 8 '+$What+': option '+$g+' was not selectable') } }
 foreach($g in $Refused){ if(@($Record.items|Where-Object{[string]$_.guid -ceq $g -and $_.canSelect -eq $false}).Count -ne 1){ throw ('Sprint 8 '+$What+': option '+$g+' was not refused') } }
}
function Assert-KmcS8NativeRefusals($Record,[string]$Selection,[string]$What,[bool]$RequireIllegal){
 $illegal=$Record.PSObject.Properties[$Selection+'IllegalAttempts']
 if($null -eq $illegal -or ($RequireIllegal -and @($illegal.Value).Count -lt 1)){ throw ('Sprint 8 '+$What+': no illegal attempt was made') }
 if(@($illegal.Value|Where-Object{$_.accepted -ne $false}).Count -ne 0){ throw ('Sprint 8 '+$What+': the native controller accepted an illegal choice') }
 if($Record.PSObject.Properties[$Selection+'DuplicateAccepted'].Value -ne $false){ throw ('Sprint 8 '+$What+': a duplicate selection was accepted') }
}
function Get-KmcS8LevelRecord($Levels,[int]$Level){ @($Levels|Where-Object{[int]$_.nextLevel -eq $Level})[0] }
function Assert-KmcS8Inventory($e){
 if($e.bondApplied -ne $true -or $null -ne $e.failure){ throw ('Sprint 8 inventory: the bond was not applied: '+$e.failure) }
 if(@($e.leases) -cnotcontains 'PaladinProgression' -or @($e.levelFive) -cnotcontains 'DivineBondSelection' -or @($e.levelFive) -ccontains 'WeaponBondFeature'){ throw 'Sprint 8 inventory: the level-five entry is not the choice' }
 if((@($e.bondItems)-join',') -cne ($script:KmcS8WeaponGuid+','+$script:KmcS8MountSel)){ throw 'Sprint 8 inventory: the choice does not offer exactly the two bonds' }
 if(@($e.archetypes|Where-Object{@($_.removesAtFive) -ccontains 'WeaponBondFeature'}).Count -ne 0){ throw 'Sprint 8 inventory: an archetype still removes the stock bond the class no longer grants' }
 $d=$e.drift
 if($null -eq $d -or $d.applied -ne $false -or [string]::IsNullOrEmpty([string]$d.failure) -or $d.slotKept -ne $true -or $d.reapplied -ne $true -or $null -ne $d.failureAfter){ throw 'Sprint 8 inventory: another mod''s edit of the slot was overwritten or not reported' }
}
function Assert-KmcS8WeaponCase($e){
 $five=Get-KmcS8LevelRecord $e.levels 5
 Assert-KmcS8SelectionOffered $five.DivineBondSelection @($script:KmcS8WeaponGuid,$script:KmcS8MountSel) @() 'weapon level five'
 if($five.after.bondSelectionFact -ne $true -or $five.after.weaponBond -ne $true){ throw 'Sprint 8 weapon: the level-five commit did not choose the weapon' }
 Assert-KmcS8Weapon $five.after 'weapon at five'
 if([int]$five.after.weaponUpgradeRanks -ne 0){ throw 'Sprint 8 weapon: an upgrade arrived early' }
 Assert-KmcS8Weapon $e.final 'weapon at nine'
 if([int]$e.final.paladinLevel -lt 9 -or [int]$e.final.weaponUpgradeRanks -lt 2){ throw 'Sprint 8 weapon: the later weapon upgrades did not arrive' }
}
function Assert-KmcS8ExistingCase($e){
 if(@($e.stockLevelFive) -cnotcontains 'WeaponBondFeature' -or @($e.stockLevelFive) -ccontains 'DivineBondSelection'){ throw 'Sprint 8 existing: the stock class was not restored for the old-character input' }
 Assert-KmcS8Weapon $e.stockBuilt 'existing as built'
 Assert-KmcS8Weapon $e.afterBondEnabled 'existing after the bond was enabled'
 if([int]$e.afterBondEnabled.weaponUpgradeRanks -ne [int]$e.stockBuilt.weaponUpgradeRanks -or $e.afterBondEnabled.bondSelectionFact -ne $false){ throw 'Sprint 8 existing: enabling the bond rewrote the character' }
 Assert-KmcS8Weapon $e.final 'existing at the next level'
 if([int]$e.final.weaponUpgradeRanks -le [int]$e.stockBuilt.weaponUpgradeRanks){ throw 'Sprint 8 existing: its later upgrades stopped' }
 foreach($r in @($e.laterLevels)){ if($null -ne $r.PSObject.Properties['DivineBondSelection']){ throw 'Sprint 8 existing: a later level offered the bond again' } }
}
function Assert-KmcS8RetrainCase($e){
 if($e.respecAllowed -ne $true){ throw 'Sprint 8 retrain: the native route was not allowed' }
 Assert-KmcS8Weapon $e.before 'retrain input'
 $m=$e.mountPath
 Assert-KmcS8MountFacts $m 'retrained to mount' $script:KmcS8HorseUnit 'Horse'
 if([string]$m.id -cne [string]$e.before.id -or $e.experienceUnchanged -ne $true){ throw 'Sprint 8 retrain: identity or experience changed' }
 Assert-KmcS8Weapon $e.weaponPath 'retrained back to the weapon'
 if($e.firstPetDestroyed -ne $true){ throw 'Sprint 8 retrain: the former mount remains' }
}
function Assert-KmcS8MountCase($e){
 $five=Get-KmcS8LevelRecord $e.levels 5
 Assert-KmcS8SelectionOffered $five.DivineBondSelection @($script:KmcS8WeaponGuid,$script:KmcS8MountSel) @() 'mount level five'
 Assert-KmcS8SelectionOffered $five.DivineMountSelection @($script:KmcS8Horse) @($script:KmcS8Dog,$script:KmcS8Empty) 'Medium mount choice'
 Assert-KmcS8NativeRefusals $five 'DivineMountSelection' 'Medium mount choice' $true
 Assert-KmcS8NativeRefusals $five 'DivineBondSelection' 'bond choice' $false
 Assert-KmcS8MountOwnerFacts $five.after 'mount at five' 'Horse'
 if([int]$five.after.rank -ne 5 -or [int]$five.after.callMax -ne 1){ throw 'Sprint 8 mount: level five is not rank 5 with one call' }
 $petId=[string]$e.boundaries.'8'.after.pet.id
 $expected=@{'8'=@{uses=1;c=0;g=0;sr=0};'9'=@{uses=2;c=0;g=0;sr=0};'11'=@{uses=2;c=1;g=0;sr=0};'13'=@{uses=3;c=0;g=1;sr=0};'15'=@{uses=3;c=0;g=1;sr=26};'17'=@{uses=4;c=0;g=1;sr=28}}
 foreach($k in $expected.Keys){
  $b=$e.boundaries.$k; if($null -eq $b){ throw ('Sprint 8 mount: boundary '+$k+' is missing') }
  $f=$b.after; $x=$expected[$k]
  Assert-KmcS8MountFacts $f ('mount at '+$k) $script:KmcS8HorseUnit 'Horse'
  if([int]$f.paladinLevel -ne [int]$k -or [string]$f.pet.id -cne $petId){ throw ('Sprint 8 mount: level or pet identity changed at '+$k) }
  if([int]$f.callMax -ne $x.uses){ throw ('Sprint 8 mount: calls at '+$k+' were '+$f.callMax) }
  if([int]$f.pet.celestial -ne $x.c -or [int]$f.pet.greaterCelestial -ne $x.g){ throw ('Sprint 8 mount: celestial facts at '+$k+' were '+$f.pet.celestial+'/'+$f.pet.greaterCelestial) }
  if([int]$f.pet.spellResistance -ne $x.sr){ throw ('Sprint 8 mount: spell resistance at '+$k+' was '+$f.pet.spellResistance) }
  $level=$script:KmcS8RankLevel[[int]$f.rank]
  if([int]$f.pet.characterLevel -ne $level -or $f.pet.canLevelUp -ne $false){ throw ('Sprint 8 mount: at '+$k+' the pet stands at level '+$f.pet.characterLevel+', not the rank table''s '+$level) }
  if($f.pet.upgrade -ne $true){ throw ('Sprint 8 mount: the species advancement is missing at '+$k) }
  if([int]$k -ge 11 -and ([int]$f.pet.smiteFact -ne 1 -or $f.pet.smiteAbility -ne $true -or [int]$f.pet.smiteUses -lt 1)){ throw ('Sprint 8 mount: the mount has no usable smite at '+$k) }
 }
 if(@($e.petCommits|Where-Object{$_.committed -ne $true}).Count -ne 0 -or @($e.petCommits).Count -lt 8){ throw 'Sprint 8 mount: the pet levels were not native commits' }
 if($e.boundaries.'11'.after.pet.reductionSources -cnotcontains 'AddDamageResistancePhysical=5'){ throw 'Sprint 8 mount: DR 5/evil is not live at 11' }
 if(@($e.boundaries.'11'.after.pet.reductionSources|Where-Object{$_ -ceq 'AddDamageResistanceEnergy=10'}).Count -ne 3){ throw 'Sprint 8 mount: three resistances of 10 are not live at 11' }
 if(@($e.boundaries.'13'.after.pet.reductionSources|Where-Object{$_ -ceq 'AddDamageResistanceEnergy=15'}).Count -ne 3){ throw 'Sprint 8 mount: three resistances of 15 are not live at 13' }
 if($e.boundaries.'13'.after.pet.reductionSources -cnotcontains 'AddDamageResistancePhysical=10'-or $e.boundaries.'13'.after.pet.reductionSources -ccontains 'AddDamageResistancePhysical=5'){ throw 'Sprint 8 mount: DR did not step to 10/evil at 13' }
}
function Assert-KmcS8ControlCase($e){
 if($e.inField.owner -ne $true -or $e.inField.pet -ne $true -or [string]$e.inField.profile -cne 'medium-humanoid-horse-v1'){ throw 'Sprint 8 control: the pair is not a supported field pair' }
 $c=$e.call
 if($c.available -ne $true -or [double]$c.distanceBefore -lt 6 -or [double]$c.distanceAfter -gt 4 -or [int]$c.usesBefore-[int]$c.usesAfter -ne 1){ throw 'Sprint 8 control: the call did not bring the mount for one use' }
 if($e.mounted -ne $true -or [string]$e.mountedProfile -cne 'medium-humanoid-horse-v1' -or $e.callWhileMounted.available -ne $false){ throw 'Sprint 8 control: mounting or the mounted refusal of the call failed' }
 if([double]$e.movedMetres -lt 2 -or [double]$e.riderWithMount -gt 2){ throw 'Sprint 8 control: the mounted pair did not travel together' }
 if($e.attack.clicked -ne $true -or [int]$e.attack.riderAttacks -lt 1 -or $e.attack.mounted -ne $true){ throw 'Sprint 8 control: the mounted rider did not attack from the ordinary input' }
 if($e.dismounted -ne $true){ throw 'Sprint 8 control: the pair did not dismount' }
 $s=$e.smite
 $n=$e.outsideParty
 if($null -eq $n -or $n.playerFaction -ne $false -or (@($n.entry)-join',') -cne 'WeaponBondFeature'){ throw 'Sprint 8 control: a character outside the party did not keep the stock bond' }
 $x=$e.fallen
 if($null -eq $x -or $x.conscious -ne $false -or $x.callAvailable -ne $false -or $x.consciousAfter -ne $true -or $x.callAvailableAfter -ne $true -or [string]$x.petAfter -cne [string]$x.petBefore){ throw 'Sprint 8 control: a fallen mount was called, or did not return as the same companion' }
 if([int]$s.charismaBonus -lt 1 -or [int]$s.attackBonusMarked-[int]$s.attackBonusUnmarked -ne [int]$s.charismaBonus -or $s.markExpires -ne $true){ throw 'Sprint 8 control: the smite did not add the Charisma bonus, or its mark never expires' }
 if($e.whileDisabled.applied -ne $false -or @($e.whileDisabled.entry).Count -ne 0){ throw 'Sprint 8 control: with the choice off, a mount-bonded character would take weapon upgrades' }
 if($s.marked -ne $true -or $s.canTarget -ne $true -or [int]$s.usesBefore-[int]$s.usesAfter -ne 1 -or [int]$s.additionalDamageMarked-[int]$s.additionalDamageUnmarked -ne [int]$s.petHitDice){ throw 'Sprint 8 control: Celestial Smite did not mark or add the pet''s Hit Dice' }
}
function Assert-KmcS8DogCase($e){
 $five=Get-KmcS8LevelRecord $e.levels 5
 Assert-KmcS8SelectionOffered $five.DivineMountSelection @($script:KmcS8Horse,$script:KmcS8Dog) @($script:KmcS8Empty) 'Small mount choice'
 Assert-KmcS8MountFacts $e.final 'dog' $script:KmcS8DogUnit 'RidingDog'
 if([string]$e.final.size -cne 'Small' -or $null -eq $e.profile -or $e.mounted -ne $true -or [double]$e.movedMetres -lt 2 -or $e.dismounted -ne $true){ throw 'Sprint 8 dog: the Small rider could not ride, walk and dismount' }
 if([int]$e.final.pet.characterLevel -ne $script:KmcS8RankLevel[[int]$e.final.rank] -or @($e.petCommits).Count -lt 1){ throw 'Sprint 8 dog: the pet did not advance by native commits' }
 $d=$e.dismissal
 if($null -eq $d -or $d.ownerInParty -ne $false -or $d.petDestroyed -ne $true -or [int]$d.orphanPets -ne 0){ throw 'Sprint 8 dog: the dismissed owner left a mount behind' }
}
function Assert-KmcS8MixedCase($e){
 $b=$e.beforePaladin
 if($null -eq $b.pet -or [int]$b.rank -lt 1 -or [int]$b.addPetFacts -ne 1){ throw 'Sprint 8 mixed: the input has no other-class companion' }
 $five=@($e.levels|Where-Object{$null -ne $_.PSObject.Properties['DivineMountSelection']})[0]
 Assert-KmcS8SelectionOffered $five.DivineMountSelection @($script:KmcS8Empty) @($script:KmcS8Horse,$script:KmcS8Dog) 'mixed mount choice'
 $f=$e.final
 if($e.samePet -ne $true -or [string]$f.pet.id -cne [string]$b.pet.id -or [int]$f.addPetFacts -ne 1 -or [int]$f.ownedPets -ne 1){ throw 'Sprint 8 mixed: a second animal or a lost original' }
 $c=$e.afterCancel
 if($null -eq $c -or $e.cancelled.complete -ne $true -or $c.mountBond -ne $false -or [int]$c.pet.intelligenceBond -ne 0 -or [int]$c.rank -ne [int]$b.rank -or [int]$c.paladinLevel -ne 4){ throw 'Sprint 8 mixed: an abandoned level left the bond behind' }
 if($f.weaponBond -ne $false -or $f.mountBond -ne $true -or [int]$f.pet.intelligenceBond -ne 1){ throw 'Sprint 8 mixed: the bond did not attach to the existing companion' }
 if([int]$f.rank -ne [int]$b.rank+[int]$f.paladinLevel){ throw ('Sprint 8 mixed: rank '+$f.rank+' does not stack '+$b.rank+' with paladin '+$f.paladinLevel) }
}
function Assert-KmcS8MixedReverseCase($e){
 $b=$e.beforeRanger
 Assert-KmcS8MountFacts $b 'reverse before Ranger' $script:KmcS8HorseUnit 'Horse'
 $bond=@($e.levels|Where-Object{$null -ne $_.PSObject.Properties['AnimalCompanionSelectionRanger']})[0]
 Assert-KmcS8SelectionOffered $bond.AnimalCompanionSelectionRanger @($script:KmcS8Empty) @('7db7c50677e39f09feef56f3831fc723','5c8e2b9a417f4d63a1d07e3bf26c9184','f894e003d31461f48a02f5caec4e3359') 'reverse companion choice'
 $f=$e.final
 if($e.samePet -ne $true -or [string]$f.pet.id -cne [string]$b.pet.id -or [int]$f.addPetFacts -ne 1 -or [int]$f.ownedPets -ne 1){ throw 'Sprint 8 reverse: a second animal or a lost mount' }
 if([int]$f.rank -le [int]$f.paladinLevel){ throw ('Sprint 8 reverse: the later class did not stack: rank '+$f.rank) }
}
function Assert-KmcS8ArchetypeCase($e){
 if([string]::IsNullOrEmpty([string]$e.archetype)){ throw 'Sprint 8 archetype: no bond-replacing archetype was exercised' }
 $five=Get-KmcS8LevelRecord $e.levels 5
 if(@($e.levels|Where-Object{$null -ne $_.PSObject.Properties['archetypeAdded'] -and $_.archetypeAdded -eq $false}).Count -ne 0 -or @($e.final.archetypes) -cnotcontains [string]$e.archetype){ throw 'Sprint 8 archetype: the native archetype was refused' }
 if($null -ne $five.PSObject.Properties['DivineBondSelection'] -or $e.final.weaponBond -ne $false -or $e.final.mountBond -ne $false -or $null -ne $e.final.pet){ throw 'Sprint 8 archetype: a replaced bond was still offered or granted' }
}
# The Bestiary's table, against the pet's own committed level: exactly one tier, with its live values.
function Assert-KmcS8Tier($Pet,[string]$What){
 $hd=[int]$Pet.characterLevel
 $want=if($hd -ge 11){@{l=0;c=0;g=1;r=15;d=10}}elseif($hd -ge 5){@{l=0;c=1;g=0;r=10;d=5}}else{@{l=1;c=0;g=0;r=5;d=0}}
 if([int]$Pet.lesserCelestial -ne $want.l -or [int]$Pet.celestial -ne $want.c -or [int]$Pet.greaterCelestial -ne $want.g){ throw ('Sprint 8 '+$What+': pet at '+$hd+' Hit Dice holds tiers '+$Pet.lesserCelestial+'/'+$Pet.celestial+'/'+$Pet.greaterCelestial) }
 $sources=@($Pet.reductionSources)
 if(@($sources|Where-Object{$_ -ceq ('AddDamageResistanceEnergy='+$want.r)}).Count -ne 3 -or @($sources|Where-Object{$_ -clike 'AddDamageResistanceEnergy=*'}).Count -ne 3){ throw ('Sprint 8 '+$What+': live resistances differ at '+$hd+' Hit Dice: '+($sources-join';')) }
 $physical=@($sources|Where-Object{$_ -clike 'AddDamageResistancePhysical=*'})
 if($want.d -eq 0){ if($physical.Count -ne 0){ throw ('Sprint 8 '+$What+': DR at '+$hd+' Hit Dice') } }
 elseif($physical.Count -ne 1 -or $physical[0] -cne ('AddDamageResistancePhysical='+$want.d)){ throw ('Sprint 8 '+$What+': DR at '+$hd+' Hit Dice differs: '+($physical-join';')) }
}
function Assert-KmcS8NoTier($Pet,[string]$What){
 if([int]$Pet.lesserCelestial+[int]$Pet.celestial+[int]$Pet.greaterCelestial -ne 0 -or @($Pet.reductionSources).Count -ne 0){ throw ('Sprint 8 '+$What+': a celestial defense is present') }
}
function Assert-KmcS8TiersCase($e){
 if([int]$e.beforeUnlock.paladinLevel -ne 10){ throw 'Sprint 8 tiers: the input was not Paladin 10' }
 Assert-KmcS8NoTier $e.beforeUnlock.pet 'before the unlock'
 $u=$e.unlockUnlevelled
 if([int]$u.paladinLevel -ne 11 -or [int]$u.rank -ne 11 -or $u.pet.canLevelUp -ne $true -or [int]$u.pet.characterLevel -ge 5 -or [int]$u.pet.smiteFact -ne 1){ throw 'Sprint 8 tiers: the unlock was not a pending underlevelled pet with its smite' }
 Assert-KmcS8Tier $u.pet 'at the unlock with levels pending'
 if([int]$e.smiteSpentForTransitions -ne 0){ throw 'Sprint 8 tiers: the use was not spent before the transitions' }
 $steps=@($e.petSteps)
 foreach($s in $steps){ Assert-KmcS8Tier $s.pet ('pet step '+$s.pet.characterLevel); if([int]$s.pet.smiteFact -ne 1 -or [int]$s.pet.smiteUses -ne 0){ throw ('Sprint 8 tiers: a transition changed the smite at '+$s.pet.characterLevel) } }
 foreach($hd in 4,5,10,11){ if(@($steps|Where-Object{[int]$_.pet.characterLevel -eq $hd}).Count -ne 1){ throw ('Sprint 8 tiers: no native commit reached '+$hd+' Hit Dice') } }
 $a=$e.ownerAheadOfPet
 if([int]$a.paladinLevel -ne 13 -or $a.pet.canLevelUp -ne $true -or [int]$a.pet.characterLevel -ge 11){ throw 'Sprint 8 tiers: the owner was not ahead of its pet' }
 Assert-KmcS8Tier $a.pet 'owner ahead of its pet'
 $r=$e.resistanceAtFifteen
 if([int]$r.paladinLevel -ne 15 -or [int]$r.pet.spellResistance -ne 26 -or [string]$r.pet.id -cne [string]$e.petIdentity){ throw 'Sprint 8 tiers: spell resistance at 15 or the pet differs' }
 Assert-KmcS8Tier $r.pet 'at 15'
 $m=$e.mixedBeforeUnlock
 if([int]$m.paladinLevel -ne 10 -or [int]$m.pet.characterLevel -lt 11){ throw 'Sprint 8 tiers: the mixed pet was not above the boundary before the unlock' }
 Assert-KmcS8NoTier $m.pet 'mixed before the unlock'
 $n=$e.mixedAtUnlock
 if([int]$n.paladinLevel -ne 11 -or [int]$n.pet.greaterCelestial -ne 1 -or [string]$n.pet.id -cne [string]$m.pet.id){ throw 'Sprint 8 tiers: Paladin 11 with an 11+ HD pet did not take the upper tier at once' }
 Assert-KmcS8Tier $n.pet 'mixed at the unlock'
 Assert-KmcS8NoTier $e.mixedLinkRemoved.pet 'link removed'
 if([int]$e.mixedLinkRemoved.pet.smiteFact -ne 1 -or [int]$e.mixedLinkRemoved.pet.smiteUses -ne 0){ throw 'Sprint 8 tiers: removing the defenses touched the smite' }
 Assert-KmcS8Tier $e.mixedLinkRestored.pet 'link restored'
 if([int]$e.mixedLinkRestored.pet.smiteUses -ne 0){ throw 'Sprint 8 tiers: restoring the defenses refunded the smite' }
 $x=$e.replaced
 if($e.oldPetDestroyed -ne $true -or [int]$x.ownedPets -ne 1 -or [string]$x.pet.id -ceq [string]$e.petIdentity -or [int]$x.pet.characterLevel -ge 5 -or [int]$x.pet.spellResistance -ne 26){ throw 'Sprint 8 tiers: the replacement pet is not a new low-level mount' }
 Assert-KmcS8Tier $x.pet 'replacement pet'
}
function Assert-KmcS8SmiteCase($e){
 $a=[string]$e.mountA; $b=[string]$e.mountB; $x=[string]$e.targetX; $y=[string]$e.targetY
 $reads=@{}; foreach($r in @($e.reads)){ $reads[[string]$r.label]=$r }
 function Expect([string]$Label,[bool]$Bonus,[string[]]$Sources,[int]$Uses){
  $r=$reads[$Label]; if($null -eq $r){ throw ('Sprint 8 smite: no read '+$Label) }
  $want=if($Bonus){[int]$r.hitDice}else{0}
  if([int]$r.additionalDamage -ne $want){ throw ('Sprint 8 smite: '+$Label+' added '+$r.additionalDamage+', expected '+$want) }
  if((@($r.sources|Sort-Object)-join',') -cne (@($Sources|Sort-Object)-join',')){ throw ('Sprint 8 smite: '+$Label+' sources were '+(@($r.sources)-join',')) }
  if([int]$r.uses -ne $Uses){ throw ('Sprint 8 smite: '+$Label+' uses were '+$r.uses) }
 }
 if([int]$e.usesAtStart.a -ne 1 -or [int]$e.usesAtStart.b -ne 1){ throw 'Sprint 8 smite: the mounts did not start with a use each' }
 Expect 'a-x-unmarked' $false @() 1
 Expect 'a-x-own-mark' $true @($a) 0
 Expect 'b-x-a-mark-only' $false @($a) 1
 if($e.bMayMarkX.canTarget -ne $true){ throw 'Sprint 8 smite: another mount''s mark blocked this one' }
 if($e.aMayRemarkX.canTarget -ne $false){ throw 'Sprint 8 smite: a mount could spend a use re-marking its own current target' }
 Expect 'a-x-both-marks' $true @($a,$b) 0
 Expect 'b-x-both-marks' $true @($a,$b) 0
 if([int]$e.aRested.uses -ne 1 -or (@($e.aRested.sourcesOnX|Sort-Object)-join',') -cne (@($a,$b|Sort-Object)-join',')){ throw 'Sprint 8 smite: the rest did not return a use and leave the marks' }
 Expect 'a-x-after-rest' $true @($a,$b) 1
 Expect 'a-x-after-changing-target' $false @($b) 0
 Expect 'a-y-current' $true @($a) 0
 Expect 'b-x-keeps-its-mark' $true @($b) 0
 Expect 'b-y-a-mark-only' $false @($a) 0
 Expect 'b-x-removed' $false @() 0
 if([double]$e.aMarkHoursLeft -gt 24.01 -or [double]$e.aMarkHoursLeft -lt 23){ throw 'Sprint 8 smite: the mark does not last the declared day' }
 Expect 'a-y-expired' $false @() 0
 if($e.aMayMarkYAfterExpiry.canTarget -ne $true){ throw 'Sprint 8 smite: an expired mark still blocks a new one' }
 if([int]$e.usesAtEnd.a -ne 0 -or [int]$e.usesAtEnd.b -ne 0){ throw 'Sprint 8 smite: a use came back without a rest' }
}
function Assert-KmcSprint8PaladinEvidence {
 param([Parameter(Mandatory=$true)]$Request,[Parameter(Mandatory=$true)]$Artifact,[AllowNull()][string]$Status,[string]$EvidenceRoot)
 if($Request.scenario -cnotin @('sprint8-paladin-choice-rt','sprint8-paladin-mount-rt','sprint8-paladin-dog-rt','sprint8-paladin-mixed-rt','sprint8-paladin-celestial-rt') -or $Artifact.schemaVersion -ne 52){ throw 'Sprint 8 evidence belongs to another scenario or schema' }
 if($Artifact.status -ceq 'FAIL') {
  if($Status -ceq 'PASS' -or (@($Artifact.errors).Count -eq 0 -and $Artifact.subscenarioFailCount -lt 1)){ throw 'Sprint 8 incomplete observation cannot become PASS' }
  return # Preserve the producer's explicit failed prefix; grant no qualification.
 }
 if($Artifact.status -cne 'PASS' -or @($Artifact.errors).Count -ne 0 -or @($Artifact.rows | Where-Object status -CNE 'PASS').Count -ne 0 -or $Artifact.subscenarioFailCount -ne 0){ throw 'Sprint 8 envelope differs from a passing one' }
 $o=$Artifact.observations.sprint8Paladin
 if($null -eq $o -or [string]$o.contract -cne 'native-paladin-divine-mount-v1' -or $o.bondApplied -ne $true){ throw 'Sprint 8 measurement is missing or the bond was not applied' }
 $names=@($Artifact.rows|ForEach-Object{[string]$_.name}|Where-Object{$_ -clike 'S8-*'})
 $cases=Get-KmcSprint8Cases ([string]$Request.scenario)
 if(($names -join ',') -cne ($cases -join ',')){ throw ('Sprint 8 rows differ: '+($names -join ',')) }
 foreach($row in @($Artifact.rows|Where-Object{[string]$_.name -clike 'S8-*'})){
  $e=$row.evidence
  switch -CaseSensitive ([string]$row.name){
   'S8-inventory' { Assert-KmcS8Inventory $e }
   'S8-weapon' { Assert-KmcS8WeaponCase $e }
   'S8-existing' { Assert-KmcS8ExistingCase $e }
   'S8-retrain' { Assert-KmcS8RetrainCase $e }
   'S8-mount-horse' { Assert-KmcS8MountCase $e }
   'S8-control' { Assert-KmcS8ControlCase $e }
   'S8-dog' { Assert-KmcS8DogCase $e }
   'S8-mixed' { Assert-KmcS8MixedCase $e }
   'S8-mixed-reverse' { Assert-KmcS8MixedReverseCase $e }
   'S8-archetype' { Assert-KmcS8ArchetypeCase $e }
   'S8-tiers' { Assert-KmcS8TiersCase $e }
   'S8-smite' { Assert-KmcS8SmiteCase $e }
  }
 }
}
function Assert-KmcSprint8PaladinPersistence($o,[bool]$Cold){
 if(-not $Cold){
  $a=$o.acquisition
  if($null -eq $a -or $null -eq $a.owner -or $null -eq $a.oldWeapon){ throw 'The Paladin save producer recorded no bond facts' }
  Assert-KmcS8MountOwnerFacts $a.owner 'producer' 'Horse'
  if([string]$a.owner.pet.blueprint -cne $script:KmcS8HorseUnit -or [string]$a.owner.pet.master -cne [string]$a.owner.id -or [int]$a.owner.pet.intelligence -lt 6){ throw 'The Paladin producer has no exact owned mount' }
  Assert-KmcS8Weapon $a.oldWeapon 'producer old character'
  if(@($a.oldWeaponStockEntry) -cnotcontains 'WeaponBondFeature'){ throw 'The old character was not built on the stock class' }
  if([int]$a.highOwner.paladinLevel -ne 15 -or [int]$a.highOwner.pet.spellResistance -ne 26){ throw 'The high owner did not reach the save with its later benefits' }
  Assert-KmcS8Tier $a.highOwner.pet 'producer high owner'
  if($a.highOwner.pet.greaterCelestial -ne 1 -or (@($a.markSources)-join',') -cne [string]$a.highOwner.pet.id -or [int]$a.highOwner.pet.smiteUses -ne 0){ throw 'The producer has no spent smite with a mark from its own mount' }
  if([int]$a.owner.rank -ne 5 -or @($a.petCommits|Where-Object{$_.committed -ne $true}).Count -ne 0){ throw 'The Paladin producer did not reach rank 5 by native commits' }
  return
 }
 $d=$o.paladinBondCold
 if($null -eq $d -or $d.oldWeaponFound -ne $true){ throw 'The cold load did not read both Paladin characters' }
 Assert-KmcS8MountFacts $d.owner 'cold' $script:KmcS8HorseUnit 'Horse'
 if([int]$d.owner.rank -ne 5 -or [int]$d.owner.pet.characterLevel -ne $script:KmcS8RankLevel[5] -or $d.ownerPetIsPairMount -ne $true){ throw 'The cold mount lost its rank, level or pair binding' }
 if([string]$d.restoredProfile -cne 'medium-humanoid-horse-v1' -or [string]$d.resolvedProfile -cne 'medium-humanoid-horse-v1'){ throw 'The cold pair lost its profile' }
 $h=$d.highOwner
 Assert-KmcS8MountFacts $h 'cold high owner' $script:KmcS8HorseUnit 'Horse'
 if([int]$h.paladinLevel -ne 15 -or [int]$h.rank -ne 15 -or [int]$h.pet.greaterCelestial -ne 1 -or [int]$h.pet.smiteFact -ne 1 -or [int]$h.pet.spellResistance -ne 26){ throw 'The cold load lost a later bond benefit' }
 Assert-KmcS8Tier $h.pet 'cold high owner'
 $m=$d.smite
 if($null -eq $m -or @($m.markedUnits).Count -ne 1 -or (@($m.sources)-join',') -cne [string]$h.pet.id -or [int]$m.additionalDamage -ne [int]$m.petHitDice -or [int]$h.pet.smiteUses -ne 0){ throw 'The cold load lost the mark''s source or target, or refunded its use' }
 Assert-KmcS8Weapon $d.oldWeapon 'cold old character'
 Assert-KmcS8Weapon $d.oldWeaponAfter 'cold old character after a level'
 if($d.oldWeapon.bondSelectionFact -ne $false -or $null -ne $d.oldWeaponNextLevel.PSObject.Properties['DivineBondSelection']){ throw 'The old character was offered or given the new choice' }
 if([int]$d.oldWeaponAfter.paladinLevel -ne [int]$d.oldWeapon.paladinLevel+1 -or [int]$d.oldWeaponAfter.weaponUpgradeRanks -lt [int]$d.oldWeapon.weaponUpgradeRanks){ throw 'The old character did not level on its own path' }
}
