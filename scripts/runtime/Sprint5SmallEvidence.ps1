# Sprint 5 (POSE-01, SIZE-01, DOG-01) external reader. The rows only record native observations; PASS is decided here.
# Pixels are not judged here: every named view must exist, hash and belong to its row, and a person inspects the images.
$script:KmcSprint5Rigs=@('male-halfling','female-halfling','male-gnome','female-gnome','male-halfling-fat','male-halfling-thin','female-halfling-fat','female-halfling-thin','male-gnome-fat','male-gnome-thin','female-gnome-fat','female-gnome-thin')
$script:KmcSprint5ExtremeRigs=@('female-halfling-thin','male-gnome-fat')
function Get-KmcSprint5Cases([string]$Scenario){
 if($Scenario -ceq 'sprint5-dog-rt'){ return @('S5-inventory','S5-medium-control','S5-acquire','S5-medium-refused','S5-boot','S5-rigs','S5-actions','S5-body-actions','S5-left-handed','S5-size-change','S5-growth','S5-pet-level','S5-stock-dog','S5-replacement') }
 if($Scenario -ceq 'sprint5-small-rt'){ return @('S5-inventory','S5-medium-control','S5-acquire','S5-boot','S5-rigs','S5-actions','S5-body-actions','S5-left-handed') }
 throw ('Sprint 5 scenario has no case list: '+$Scenario)
}
function Assert-KmcSprint5Shot($Shots,[string]$Name,[string]$Root){
 $found=@($Shots | Where-Object { [string]$_.name -ceq $Name })
 if($found.Count -ne 1){ throw ('Sprint 5 view missing or duplicated: '+$Name) }
 $shot=$found[0]
 if($null -ne $shot.PSObject.Properties['error'] -or [string]$shot.file -cnotmatch '^movement-visuals/sprint5-[a-z0-9-]+\.png$' -or [long]$shot.bytes -le 0){ throw ('Sprint 5 view failed: '+$Name) }
 if($Root){
  $path=Join-Path $Root ([string]$shot.file).Replace('/','\')
  if(-not (Test-Path -LiteralPath $path -PathType Leaf)){ throw ('Sprint 5 view file is absent: '+$Name) }
  if((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -cne [string]$shot.sha256){ throw ('Sprint 5 view hash differs: '+$Name) }
 }
}
function Assert-KmcSprint5Rigs($Row,$Shots,[string]$Mount,[string]$Root,[string]$ExpectedMountEffective){
 $rigs=@($Row.evidence.rigs)
 if($rigs.Count -ne $script:KmcSprint5Rigs.Count -or (($rigs | ForEach-Object { [string]$_.rig }) -join ',') -cne ($script:KmcSprint5Rigs -join ',')){ throw ('Sprint 5 '+$Mount+' row does not cover each declared rig') }
 $seen=New-Object System.Collections.Generic.HashSet[string]
 foreach($rig in $rigs){
  if([string]$rig.resolvedProfile -cne [string]$rig.expectedProfile -or $rig.mounted -ne $true){ throw ('Sprint 5 rig '+$rig.rig+' on '+$Mount+' was not admitted to its own profile: '+$rig.refusal) }
  if(-not $seen.Add([string]$rig.expectedProfile)){ throw 'Sprint 5 rigs share one profile' }
  $f=$rig.rigFacts
  if([string]$f.originalSize -cne 'Small' -or [string]$f.effectiveSize -cne 'Small'){ throw ('Sprint 5 rig '+$rig.rig+' is not natively Small') }
  $s=$rig.sizes
  if([string]$s.mountEffective -cne $ExpectedMountEffective){ throw ('Sprint 5 '+$Mount+' effective size differs: '+$s.mountEffective) }
  if([math]::Abs([double]$s.riderWorldScale.y-[double]$f.worldScale.y) -gt 0.02){ throw ('Sprint 5 rider '+$rig.rig+' changed size on the mount: '+$s.riderWorldScale.y+' from '+$f.worldScale.y) }
  foreach($view in @('side','front','rear')){ Assert-KmcSprint5Shot $Shots ($Mount+'-'+$rig.rig+'-idle-'+$view) $Root }
  foreach($index in 1,2){ Assert-KmcSprint5Shot $Shots ($Mount+'-'+$rig.rig+'-move-'+$index) $Root; Assert-KmcSprint5Shot $Shots ($Mount+'-'+$rig.rig+'-move-close-'+$index) $Root }; Assert-KmcSprint5Shot $Shots ($Mount+'-'+$rig.rig+'-ordinary-camera') $Root
  if($null -eq $rig.afterDismount -or $null -eq $rig.afterDismountRig){ throw ('Sprint 5 rig '+$rig.rig+' never dismounted') }
  if([string]$rig.afterDismountRig.skeleton -cne [string]$f.skeleton){ throw ('Sprint 5 rig '+$rig.rig+' changed skeleton across the ride') }
  if([math]::Abs([double]$rig.afterDismountRig.worldScale.y-[double]$f.worldScale.y) -gt 0.02){ throw ('Sprint 5 rig '+$rig.rig+' kept a scale residue after dismounting') }
  if([math]::Abs([double]$rig.afterDismountRig.pelvisHeight-[double]$f.pelvisHeight) -gt 0.08){ throw ('Sprint 5 rig '+$rig.rig+' kept a pose residue after dismounting') }
  foreach($view in @('close-side','close-other','close-front')){ Assert-KmcSprint5Shot $Shots ($Mount+'-'+$rig.rig+'-idle-'+$view) $Root }
  Assert-KmcSprint5Shot $Shots ($Mount+'-'+$rig.rig+'-dismounted') $Root
  if(@($Shots | Where-Object { [string]$_.name -cmatch ('^'+$Mount+'-'+$rig.rig+'-idle-close-(side|other)$') -and $_.clear -eq $true }).Count -lt 1){ throw ('Sprint 5 rig '+$rig.rig+' has no unobstructed close view') }
 }
}
function Assert-KmcSprint5Actions($Evidence,$Shots,[string]$Mount,[string]$Root,[string]$Rig=''){
 $e=$Evidence
 $prefix=$Mount+'-action-'+$(if($Rig){$Rig+'-'}else{''})
 if($e.remounted -ne $true){ throw 'Sprint 5 remount was not observed' }
 foreach($boundary in @($e.gearRemoved,$e.gearRestored,$e.duringCombat)){
  if([string]$boundary.state -cne 'Mounted' -or [string]$boundary.resolved -cne [string]$e.profileBefore){ throw 'Sprint 5 gear or combat changed the rig profile or the mounted state' }
 }
 foreach($view in @('gear-removed','gear-restored','cast','cast-close','turn','turn-close','attack-close','attack-1','attack-2','attack-3','mount-attack','dismounted')){ Assert-KmcSprint5Shot $Shots ($prefix+$view) $Root }
 if($e.attackClicked -ne $true -or $e.mountAttackClicked -ne $true -or [int]$e.riderAttacks -lt 1 -or [int]$e.mountAttacks -lt 1){ throw 'Sprint 5 ordinary clicks did not produce a native attack by each actor' }
 if([double]$e.cooldownsAfterRider.riderStandard -le 0 -or [double]$e.cooldownsAfterRider.mountStandard -ne 0 -or [double]$e.cooldownsDuring.mountStandard -le 0){ throw 'Sprint 5 each actor must spend its own Standard action' }
 if([math]::Abs([double]$e.afterDismount.worldScale.y-[double]$e.unmountedRig.worldScale.y) -gt 0.02){ throw 'Sprint 5 dismount did not restore the rider scale' }
}
# The native view mirrors from the override the setup sets, so an unmirrored row is not left-handed evidence. Its seat must be complete and leave no residue.
function Assert-KmcSprint5LeftHanded($Evidence,$Shots,[string]$Mount,[string]$Root){
 $e=$Evidence
 if($null -eq $e.restoredRig -or $e.restoredRig.mirror -ne $false){ throw 'Sprint 5 the left-handed setup was not undone' }
 if($e.rigFacts.mirror -ne $true){ throw 'Sprint 5 the left-handed setup did not mirror the native avatar' }
 if([double]$e.rigFacts.animatorScaleX -ge 0 -or $e.rigFacts.leftHanded -ne $true){ throw 'Sprint 5 a mirrored rig has no mirrored native scale' }
 if($e.mounted -ne $true -or $null -eq $e.resolved -or $e.mountedRig.mirror -ne $true){ throw 'Sprint 5 the mirrored rig was not seated' }
 foreach($view in @('side','front','rear','close-side','close-other','close-front')){ Assert-KmcSprint5Shot $Shots ($Mount+'-left-handed-'+$view) $Root }
}
# The near boot comparison: an affected rig, a control and a mirror, each seated, geared, dismounted and remounted on its own profile, with a matched geometry observation.
function Assert-KmcSprint5Boot($Evidence,$Shots,[string]$Mount,[string]$Root){
 $cases=@($Evidence.cases)
 if((@($cases | ForEach-Object { [string]$_.label }) -join ',') -cne 'female-halfling,male-halfling,female-halfling-left-handed'){ throw 'Sprint 5 the near boot cases differ' }
 foreach($c in $cases){
  if($c.mounted -ne $true -or $c.remounted -ne $true -or $null -eq $c.resolved -or [string]$c.remountedResolved -cne [string]$c.resolved){ throw ('Sprint 5 near boot case '+$c.label+' did not keep its profile through remounting') }
  if([string]$c.armorOff.state -cne 'Mounted' -or [string]$c.armorOff.resolved -cne [string]$c.resolved -or [string]$c.armorOn.state -cne 'Mounted' -or [string]$c.armorOn.resolved -cne [string]$c.resolved){ throw ('Sprint 5 near boot case '+$c.label+' lost its seat across the gear change') }
  if(($c.mountedRig.mirror -eq $true) -ne ($c.leftHanded -eq $true)){ throw ('Sprint 5 near boot case '+$c.label+' has the wrong mirror') }
  if($null -eq $c.afterDismountRig -or [math]::Abs([double]$c.afterDismountRig.pelvisHeight-[double]$c.rigFacts.pelvisHeight) -gt 0.08){ throw ('Sprint 5 near boot case '+$c.label+' kept a pose residue') }
  $prefix=$Mount+'-boot-'+$c.label+'-'
  foreach($view in @('standing-near','standing-far','standing-front','standing-near-ordinary-light','standing-walk-1','standing-walk-2','mounted-near','mounted-near-nomount','mounted-far','mounted-front','mounted-front-nomount','mounted-three-quarter','mounted-near-ordinary-light','mounted-ordinary-camera','armor-off-near','armor-off-near-nomount','armor-off-far','armor-restored-near','dismounted-near','dismounted-walk','remounted-near')){ Assert-KmcSprint5Shot $Shots ($prefix+$view) $Root }
  foreach($view in @('standing-near','mounted-near','mounted-far','mounted-front','armor-off-near','remounted-near')){
   $shot=@($Shots | Where-Object { [string]$_.name -ceq ($prefix+$view) })[0]
   if($null -eq $shot.boot -or $null -ne $shot.PSObject.Properties['bootError']){ throw ('Sprint 5 near boot view has no geometry observation: '+$prefix+$view) }
   if([int]$shot.boot.frame -ne [int]$shot.frame){ throw ('Sprint 5 near boot geometry is not from the frame of its pixels: '+$prefix+$view) }
   # A seated rider's rig is whole: the native merge mapped every bone of every part, so both legs follow their own bones.
   if($view -cne 'standing-near'){
    if(@($shot.boot.parts | Where-Object { $null -ne $_.PSObject.Properties['mappingStoppedAt'] }).Count -ne 0){ throw ('Sprint 5 a seated rig still stops the native merge: '+$prefix+$view) }
    if([int]$shot.boot.rightBoot.count -le 0 -or [int]$shot.boot.rightBoot.count -ne [int]$shot.boot.leftBoot.count){ throw ('Sprint 5 a seated rig has no matching lower legs: '+$prefix+$view) }
   }
  }
 }
}
# The pet's own class levels: each commit is one native level with class-table base statistics, the same identity, and no second upgrade.
function Assert-KmcSprint5PetLevel($Evidence,$Shots,[string]$Root,[string]$OwnerId,[string]$PetId){
 $e=$Evidence
 $commits=@($e.commits)
 if($commits.Count -ne 3){ throw 'Sprint 5 the pet level sequence is incomplete' }
 $expected=[int]$e.levelBefore.characterLevel
 foreach($c in $commits){
  $expected++
  if($c.canLevelUp -ne $true -or $c.committed -ne $true -or $c.complete -ne $true){ throw 'Sprint 5 a pet level was not a complete native commit' }
  $b=$c.before; $a=$c.after
  if([int]$a.characterLevel -ne $expected -or [int]$a.classLevel -ne ([int]$b.classLevel+1)){ throw 'Sprint 5 a commit did not add one pet class level' }
  if([int]$a.experience -ne [int]$b.experience){ throw 'Sprint 5 a pet level changed experience' }
  foreach($pair in @(@('babBase','classBab'),@('fortitudeBase','classFortitude'),@('reflexBase','classReflex'),@('willBase','classWill'))){
   if(([int]$a.($pair[0])-[int]$b.($pair[0])) -ne ([int]$a.($pair[1])-[int]$b.($pair[1]))){ throw ('Sprint 5 '+$pair[0]+' did not follow the native class table') }
  }
  if([int]$a.hitPointsBase -le [int]$b.hitPointsBase -or [int]$a.maxHp -le [int]$b.maxHp){ throw 'Sprint 5 a pet level added no hit points' }
  foreach($state in @($b,$a)){ if([string]$state.id -cne $PetId -or [string]$state.master -cne $OwnerId -or [string]$state.size -cne 'Medium'){ throw 'Sprint 5 a pet level changed the pet, its owner or its size' } }
  if([int]$a.upgradeCount -ne 1 -or [int]$a.upgradeRank -ne 1){ throw 'Sprint 5 the rank-four upgrade did not stay single' }
 }
 if([string]$e.afterMountedCommit.state -cne 'Mounted' -or $null -eq $e.afterMountedCommit.resolved){ throw 'Sprint 5 a pet level lost the mounted pair or its seat' }
 if([int]$e.ownerLevelCommits -ne 1 -or [int]$e.ownerRankAfter -le [int]$e.ownerRank){ throw 'Sprint 5 the owner level that re-runs the pet update did not happen' }
 foreach($state in @($e.petAfterOwnerLevel,$e.petFinal)){
  if([string]$state.id -cne $PetId -or [int]$state.upgradeCount -ne 1 -or [int]$state.upgradeRank -ne 1){ throw 'Sprint 5 reconciliation re-granted the upgrade or replaced the pet' }
  if([int]$state.strength -ne [int]$e.petAfterCommits.strength -or [int]$state.constitution -ne [int]$e.petAfterCommits.constitution){ throw 'Sprint 5 reconciliation or remounting changed the upgraded scores' }
 }
 if([string]$e.petIdentityAfterOwnerLevel -cne $PetId -or [int]$e.ridingDogUnits -ne 1){ throw 'Sprint 5 a duplicate or orphan companion exists after leveling' }
 if([string]$e.leveledMounted.state -cne 'Mounted' -or $null -eq $e.leveledMounted.resolved){ throw 'Sprint 5 the leveled pet could not carry its rider' }
 foreach($view in @('side','front','rear','close-side','close-other','close-front')){ Assert-KmcSprint5Shot $Shots ('dog-leveled-'+$view) $Root }
}
function Assert-KmcSprint5SmallEvidence {
 param([Parameter(Mandatory=$true)]$Request,[Parameter(Mandatory=$true)]$Artifact,[AllowNull()][string]$Status,[string]$EvidenceRoot)
 if($Request.scenario -cnotin @('sprint5-small-rt','sprint5-dog-rt') -or $Artifact.schemaVersion -ne 49){ throw 'Sprint 5 scenario/schema mismatch' }
 if(-not $EvidenceRoot){ $EvidenceRoot=[string]$Request.evidenceRoot }
 if($Artifact.status -ceq 'FAIL') {
  if($Status -ceq 'PASS' -or (@($Artifact.errors).Count -eq 0 -and $Artifact.subscenarioFailCount -lt 1)){ throw 'Sprint 5 incomplete observation cannot become PASS' }
  return # Preserve the producer's explicit failed prefix; grant no qualification.
 }
 if($Artifact.status -cne 'PASS' -or @($Artifact.errors).Count -ne 0 -or @($Artifact.rows | Where-Object status -CNE 'PASS').Count -ne 0 -or $Artifact.subscenarioFailCount -ne 0){ throw 'Sprint 5 envelope differs' }
 $o=$Artifact.observations.sprint5Small
 if($null -eq $o -or $o.contract -cne 'native-small-riders-riding-dog-v3' -or $o.controlRider -cne $Artifact.observations.riderId -or $o.controlMount -cne $Artifact.observations.horseId){ throw 'Sprint 5 measurement identity differs' }
 if($o.bodiesAreDisclosedSetup -ne $true){ throw 'Sprint 5 body setup is not disclosed' }
 $dogRun=$Request.scenario -ceq 'sprint5-dog-rt'
 $mount=$(if($dogRun){'dog'}else{'horse'})
 if([string]$o.mount -cne $mount){ throw 'Sprint 5 mount differs from its scenario' }
 $expected=@(Get-KmcSprint5Cases $Request.scenario)
 if(($o.cases | ConvertTo-Json -Compress) -cne ($expected | ConvertTo-Json -Compress)){ throw 'Sprint 5 registered row order differs' }
 $rows=@{}
 foreach($name in $expected){ $found=@($Artifact.rows | Where-Object name -CEQ $name); if($found.Count -ne 1){ throw ('Sprint 5 required row missing or duplicated: '+$name) }; $rows[$name]=$found[0] }
 $shots=@($o.shots)
 $inventory=$rows['S5-inventory'].evidence
 if([string]$inventory.ridingDog.guid -cne 'b7a14f25d3c94e0d8e6f1a2c5d90e431' -or $null -ne $inventory.registration.ridingDogFailure){ throw 'Sprint 5 Riding Dog identity or registration differs' }
 if(@($inventory.dogSelections | Where-Object { $_.ridingDogOffered -ne $true }).Count -ne 0 -or @($inventory.dogSelections).Count -lt 1){ throw 'Sprint 5 a selection that offers the stock Dog does not offer the Riding Dog' }
 $control=$rows['S5-medium-control'].evidence
 if([string]$control.profile -cne 'medium-humanoid-mammoth-v1'){ throw 'Sprint 5 Medium control profile differs' }
 foreach($view in @('side','front','rear')){ Assert-KmcSprint5Shot $shots ('medium-mammoth-idle-'+$view) $EvidenceRoot }
 $acquire=$rows['S5-acquire'].evidence
 $wantedUnit=$(if($dogRun){'b7a14f25d3c94e0d8e6f1a2c5d90e431'}else{'4016c7db400ab721ff125aef9e65e202'})
 $wantedFeature=$(if($dogRun){'5c8e2b9a417f4d63a1d07e3bf26c9184'}else{'7db7c50677e39f09feef56f3831fc723'})
 $wantedSize=$(if($dogRun){'Medium'}else{'Large'})
 if($acquire.acquisition.acquired -ne $true -or [string]$acquire.acquisition.petBlueprint -cne $wantedUnit -or [int]$acquire.acquisition.rank -ne 1){ throw 'Sprint 5 companion was not chosen through the native selection' }
 if(@($acquire.acquisition.optionsOffered) -cnotcontains $wantedFeature){ throw 'Sprint 5 Ranger selection did not offer the companion' }
 if([string]$acquire.pet.effectiveSize -cne $wantedSize -or [string]$acquire.pet.master -cne [string]$acquire.acquisition.owner){ throw 'Sprint 5 companion size or owner differs' }
 Assert-KmcSprint5Boot $rows['S5-boot'].evidence $shots $mount $EvidenceRoot
 Assert-KmcSprint5Rigs $rows['S5-rigs'] $shots $mount $EvidenceRoot $wantedSize
 Assert-KmcSprint5Actions $rows['S5-actions'].evidence $shots $mount $EvidenceRoot
 $bodies=@($rows['S5-body-actions'].evidence.bodies)
 if((@($bodies | ForEach-Object { [string]$_.rig }) -join ',') -cne ($script:KmcSprint5ExtremeRigs -join ',')){ throw 'Sprint 5 the action extremes were not all run' }
 foreach($body in $bodies){
  $own=@($rows['S5-rigs'].evidence.rigs | Where-Object { [string]$_.rig -ceq [string]$body.rig })
  if($own.Count -ne 1 -or [string]$body.profileBefore -cne [string]$own[0].expectedProfile -or [string]$body.unmountedRig.skeleton -cne [string]$own[0].rigFacts.skeleton){ throw ('Sprint 5 action extreme '+$body.rig+' did not run on its own body') }
  Assert-KmcSprint5Actions $body $shots $mount $EvidenceRoot ([string]$body.rig)
 }
 Assert-KmcSprint5LeftHanded $rows['S5-left-handed'].evidence $shots $mount $EvidenceRoot
 if($dogRun){
  $refused=$rows['S5-medium-refused'].evidence
  if([string]$refused.rigFacts.effectiveSize -cne 'Medium' -or $null -ne $refused.resolved -or [string]$refused.refusal -cnotmatch 'larger' -or [string]$refused.stateAfter -ceq 'Mounted'){ throw 'Sprint 5 a Medium rider was not refused on the Medium dog' }
  $size=$rows['S5-size-change'].evidence
  if($size.detached -ne $true -or $null -ne $size.after.resolved -or [string]$size.ownership -cne [string]$acquire.acquisition.owner -or [string]$size.restored.sizes.riderEffective -cne 'Small'){ throw 'Sprint 5 native size change did not detach lawfully' }
  $growth=$rows['S5-growth'].evidence
  if($growth.upgrade -ne $true -or [int]$growth.rankAfter -lt 4 -or [string]$growth.after.effectiveSize -cne 'Medium' -or [string]$growth.grownMounted.state -cne 'Mounted'){ throw 'Sprint 5 rank-4 growth or grown seat differs' }
  if(([int]$growth.after.strength-[int]$growth.before.strength) -lt 2 -or ([int]$growth.after.constitution-[int]$growth.before.constitution) -lt 2){ throw 'Sprint 5 Riding Dog advancement did not apply' }
  foreach($view in @('side','front','rear')){ Assert-KmcSprint5Shot $shots ('dog-grown-'+$view) $EvidenceRoot }
  Assert-KmcSprint5PetLevel $rows['S5-pet-level'].evidence $shots $EvidenceRoot ([string]$acquire.acquisition.owner) ([string]$acquire.pet.id)
  $stock=$rows['S5-stock-dog'].evidence
  if($stock.mountable -ne $false){ throw 'Sprint 5 the ordinary Dog became a mount' }
  $replace=$rows['S5-replacement'].evidence
  if($null -ne $replace.ownerPet -or [int]$replace.orphans -ne 0){ throw 'Sprint 5 replacing the Riding Dog left a pet or an orphan' }
 }
 if($Status -ceq 'PASS' -and $Artifact.status -cne 'PASS'){ throw 'Sprint 5 failed native observation cannot be promoted' }
}
