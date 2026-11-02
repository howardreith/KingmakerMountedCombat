# Sprint 5 (POSE-01, SIZE-01, DOG-01) external reader. The rows only record native observations; PASS is decided here.
# Pixels are not judged here: every named view must exist, hash and belong to its row, and a person inspects the images.
$script:KmcSprint5Rigs=@('male-halfling','female-halfling','male-gnome','female-gnome')
function Get-KmcSprint5Cases { @('S5-inventory','S5-medium-control','S5-horse-rigs','S5-dog-acquire','S5-dog-rigs') }
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
function Assert-KmcSprint5SmallEvidence {
 param([Parameter(Mandatory=$true)]$Request,[Parameter(Mandatory=$true)]$Artifact,[AllowNull()][string]$Status,[string]$EvidenceRoot)
 if($Request.scenario -cne 'sprint5-small-rt' -or $Artifact.schemaVersion -ne 49){ throw 'Sprint 5 scenario/schema mismatch' }
 if(-not $EvidenceRoot){ $EvidenceRoot=[string]$Request.evidenceRoot }
 if($Artifact.status -ceq 'FAIL') {
  if($Status -ceq 'PASS' -or (@($Artifact.errors).Count -eq 0 -and $Artifact.subscenarioFailCount -lt 1)){ throw 'Sprint 5 incomplete observation cannot become PASS' }
  return # Preserve the producer's explicit failed prefix; grant no qualification.
 }
 if($Artifact.status -cne 'PASS' -or @($Artifact.errors).Count -ne 0 -or @($Artifact.rows | Where-Object status -CNE 'PASS').Count -ne 0 -or $Artifact.subscenarioFailCount -ne 0){ throw 'Sprint 5 envelope differs' }
 $o=$Artifact.observations.sprint5Small
 if($null -eq $o -or $o.contract -cne 'native-small-riders-riding-dog-v1' -or $o.controlRider -cne $Artifact.observations.riderId -or $o.controlMount -cne $Artifact.observations.horseId){ throw 'Sprint 5 measurement identity differs' }
 if($o.bodiesAreDisclosedSetup -ne $true){ throw 'Sprint 5 body setup is not disclosed' }
 $expected=@(Get-KmcSprint5Cases)
 if(($o.cases | ConvertTo-Json -Compress) -cne ($expected | ConvertTo-Json -Compress)){ throw 'Sprint 5 registered row order differs' }
 $rows=@{}
 foreach($name in $expected){ $found=@($Artifact.rows | Where-Object name -CEQ $name); if($found.Count -ne 1){ throw ('Sprint 5 required row missing or duplicated: '+$name) }; $rows[$name]=$found[0] }
 $shots=@($o.shots)
 $inventory=$rows['S5-inventory'].evidence
 if([string]$inventory.stockDog.size -cne 'Small' -or [string]$inventory.ridingDog.guid -cne 'b7a14f25d3c94e0d8e6f1a2c5d90e431'){ throw 'Sprint 5 stock and Riding Dog identities differ' }
 if(@($inventory.dogSelections | Where-Object { $_.ridingDogOffered -ne $true }).Count -ne 0 -or @($inventory.dogSelections).Count -lt 1){ throw 'Sprint 5 a selection that offers the stock Dog does not offer the Riding Dog' }
 $control=$rows['S5-medium-control'].evidence
 if([string]$control.profile -cne 'medium-humanoid-mammoth-v1'){ throw 'Sprint 5 Medium control profile differs' }
 foreach($view in @('side','front','rear')){ Assert-KmcSprint5Shot $shots ('medium-mammoth-idle-'+$view) $EvidenceRoot }
 $horse=$rows['S5-horse-rigs'].evidence
 if($horse.acquisition.acquired -ne $true -or [string]$horse.acquisition.petBlueprint -cne '4016c7db400ab721ff125aef9e65e202'){ throw 'Sprint 5 Horse was not chosen through the native selection' }
 Assert-KmcSprint5Rigs $rows['S5-horse-rigs'] $shots 'horse' $EvidenceRoot 'Large'
 $dog=$rows['S5-dog-acquire'].evidence
 if($dog.acquisition.acquired -ne $true -or [string]$dog.acquisition.petBlueprint -cne 'b7a14f25d3c94e0d8e6f1a2c5d90e431' -or [int]$dog.acquisition.rank -ne 1){ throw 'Sprint 5 Riding Dog was not chosen through the native selection' }
 if(@($dog.acquisition.optionsOffered) -cnotcontains '5c8e2b9a417f4d63a1d07e3bf26c9184'){ throw 'Sprint 5 Ranger selection did not offer the Riding Dog' }
 if([string]$dog.pet.effectiveSize -cne 'Medium' -or [string]$dog.pet.originalSize -cne 'Small' -or [string]$dog.pet.master -cne [string]$dog.acquisition.owner){ throw 'Sprint 5 Riding Dog size or owner differs' }
 Assert-KmcSprint5Rigs $rows['S5-dog-rigs'] $shots 'dog' $EvidenceRoot 'Medium'
 if($Status -ceq 'PASS' -and $Artifact.status -cne 'PASS'){ throw 'Sprint 5 failed native observation cannot be promoted' }
}
