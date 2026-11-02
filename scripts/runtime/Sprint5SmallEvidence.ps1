# Sprint 5 (POSE-01, SIZE-01, DOG-01) external reader. The rows only record native observations; PASS is decided here.
# Pixels are not judged here: every named view must exist, hash and belong to its row, and a person inspects the images.
$script:KmcSprint5Rigs=@('male-halfling','female-halfling','male-gnome','female-gnome')
function Get-KmcSprint5Cases([string]$Scenario){
 if($Scenario -ceq 'sprint5-dog-rt'){ return @('S5-inventory','S5-medium-control','S5-acquire','S5-medium-refused','S5-rigs','S5-actions','S5-size-change','S5-growth','S5-stock-dog','S5-replacement') }
 if($Scenario -ceq 'sprint5-small-rt'){ return @('S5-inventory','S5-medium-control','S5-acquire','S5-rigs','S5-actions') }
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
 if($rigs.Count -ne 4 -or (($rigs | ForEach-Object { [string]$_.rig }) -join ',') -cne ($script:KmcSprint5Rigs -join ',')){ throw ('Sprint 5 '+$Mount+' row does not cover each declared rig') }
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
  foreach($index in 1,2){ Assert-KmcSprint5Shot $Shots ($Mount+'-'+$rig.rig+'-move-'+$index) $Root }
  if($null -eq $rig.afterDismount){ throw ('Sprint 5 rig '+$rig.rig+' never dismounted') }
 }
}
function Assert-KmcSprint5Actions($Row,$Shots,[string]$Mount,[string]$Root){
 $e=$Row.evidence
 if($e.remounted -ne $true){ throw 'Sprint 5 remount was not observed' }
 foreach($boundary in @($e.gearRemoved,$e.gearRestored,$e.duringCombat)){
  if([string]$boundary.state -cne 'Mounted' -or [string]$boundary.resolved -cne [string]$e.profileBefore){ throw 'Sprint 5 gear or combat changed the rig profile or the mounted state' }
 }
 foreach($view in @('gear-removed','gear-restored','cast','turn','attack-1','attack-2','attack-3','mount-attack','dismounted')){ Assert-KmcSprint5Shot $Shots ($Mount+'-action-'+$view) $Root }
 if($e.attackClicked -ne $true -or $e.mountAttackClicked -ne $true -or [int]$e.riderAttacks -lt 1 -or [int]$e.mountAttacks -lt 1){ throw 'Sprint 5 ordinary clicks did not produce a native attack by each actor' }
 if([double]$e.cooldownsAfterRider.riderStandard -le 0 -or [double]$e.cooldownsAfterRider.mountStandard -ne 0 -or [double]$e.cooldownsDuring.mountStandard -le 0){ throw 'Sprint 5 each actor must spend its own Standard action' }
 if([math]::Abs([double]$e.afterDismount.worldScale.y-[double]$e.unmountedRig.worldScale.y) -gt 0.02){ throw 'Sprint 5 dismount did not restore the rider scale' }
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
 if($null -eq $o -or $o.contract -cne 'native-small-riders-riding-dog-v1' -or $o.controlRider -cne $Artifact.observations.riderId -or $o.controlMount -cne $Artifact.observations.horseId){ throw 'Sprint 5 measurement identity differs' }
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
 Assert-KmcSprint5Rigs $rows['S5-rigs'] $shots $mount $EvidenceRoot $wantedSize
 Assert-KmcSprint5Actions $rows['S5-actions'] $shots $mount $EvidenceRoot
 if($dogRun){
  $refused=$rows['S5-medium-refused'].evidence
  if([string]$refused.rigFacts.effectiveSize -cne 'Medium' -or $null -ne $refused.resolved -or [string]$refused.refusal -cnotmatch 'larger' -or [string]$refused.stateAfter -ceq 'Mounted'){ throw 'Sprint 5 a Medium rider was not refused on the Medium dog' }
  $size=$rows['S5-size-change'].evidence
  if($size.detached -ne $true -or $null -ne $size.after.resolved -or [string]$size.ownership -cne [string]$acquire.acquisition.owner -or [string]$size.restored.sizes.riderEffective -cne 'Small'){ throw 'Sprint 5 native size change did not detach lawfully' }
  $growth=$rows['S5-growth'].evidence
  if($growth.upgrade -ne $true -or [int]$growth.rankAfter -lt 4 -or [string]$growth.after.effectiveSize -cne 'Medium' -or [string]$growth.grownMounted.state -cne 'Mounted'){ throw 'Sprint 5 rank-4 growth or grown seat differs' }
  if(([int]$growth.after.strength-[int]$growth.before.strength) -lt 2 -or ([int]$growth.after.constitution-[int]$growth.before.constitution) -lt 2){ throw 'Sprint 5 Riding Dog advancement did not apply' }
  foreach($view in @('side','front','rear')){ Assert-KmcSprint5Shot $shots ('dog-grown-'+$view) $EvidenceRoot }
  $stock=$rows['S5-stock-dog'].evidence
  if($stock.mountable -ne $false){ throw 'Sprint 5 the ordinary Dog became a mount' }
  $replace=$rows['S5-replacement'].evidence
  if($null -ne $replace.ownerPet -or [int]$replace.orphans -ne 0){ throw 'Sprint 5 replacing the Riding Dog left a pet or an orphan' }
 }
 if($Status -ceq 'PASS' -and $Artifact.status -cne 'PASS'){ throw 'Sprint 5 failed native observation cannot be promoted' }
}
