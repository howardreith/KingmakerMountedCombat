# Sprint 5 reader: synthetic positive and negative checks. No game, no build. A reader-only assertion is never native evidence.
param()
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'runtime\RuntimeHarness.Common.ps1')
$script:pass=0;$script:fail=0
function Check([bool]$Condition,[string]$Name){ if($Condition){$script:pass++;Write-Output ('    PASS '+$Name)}else{$script:fail++;Write-Output ('    FAIL '+$Name)} }
function Copy-S5($Value){ $Value | ConvertTo-Json -Depth 30 | ConvertFrom-Json }
$owner='d17c8fd0-6627-40c4-bf9a-024cf001c1f7'
function New-S5Shots([string]$Mount,[bool]$Dog){
 $names=New-Object System.Collections.ArrayList
 foreach($v in 'side','front','rear'){ [void]$names.Add('medium-mammoth-idle-'+$v); if($Dog){[void]$names.Add('dog-grown-'+$v)} }
 foreach($rig in $script:KmcSprint5Rigs){ foreach($v in 'idle-side','idle-front','idle-rear','idle-close-side','idle-close-other','idle-close-front','move-1','move-2','move-close-1','move-close-2','ordinary-camera','dismounted'){ [void]$names.Add($Mount+'-'+$rig+'-'+$v) } }
 foreach($v in 'gear-removed','gear-restored','cast','cast-close','turn','turn-close','attack-close','attack-1','attack-2','attack-3','mount-attack','dismounted'){ [void]$names.Add($Mount+'-action-'+$v); foreach($rig in $script:KmcSprint5ExtremeRigs){ [void]$names.Add($Mount+'-action-'+$rig+'-'+$v) } }
 foreach($v in 'side','front','rear','close-side','close-other','close-front'){ [void]$names.Add($Mount+'-left-handed-'+$v); if($Dog){ [void]$names.Add('dog-leveled-'+$v) } }
 foreach($label in 'female-halfling','male-halfling','female-halfling-left-handed'){ foreach($v in 'standing-near','standing-far','standing-front','standing-near-ordinary-light','standing-walk-1','standing-walk-2','mounted-near','mounted-near-nomount','mounted-far','mounted-front','mounted-front-nomount','mounted-three-quarter','mounted-near-ordinary-light','mounted-ordinary-camera','armor-off-near','armor-off-near-nomount','armor-off-far','armor-restored-near','dismounted-near','dismounted-walk','remounted-near'){ [void]$names.Add($Mount+'-boot-'+$label+'-'+$v) } }
 @($names | ForEach-Object { $shot=[pscustomobject]@{name=$_;frame=7;file='movement-visuals/sprint5-'+$_+'.png';bytes=10;sha256='a';clear=$true}; if($_ -cmatch '-boot-.*-(standing-near|mounted-near|mounted-far|mounted-front|armor-off-near|remounted-near)$'){ $shot | Add-Member -NotePropertyName boot -NotePropertyValue ([pscustomobject]@{frame=7;parts=@([pscustomobject]@{entity='e'});leftBoot=[pscustomobject]@{count=312};rightBoot=[pscustomobject]@{count=312}}) }; $shot })
}
function New-S5Rigs([string]$Mount,[string]$Size){
 @($script:KmcSprint5Rigs | ForEach-Object {
  $parts=$_.Split('-'); $build=$(if($parts.Count -eq 3){'-'+$parts[2]}else{''})
  $id=$(if($_ -ceq 'male-halfling' -and $Mount -ceq 'horse'){'small-default-halfling-horse-v1'}else{'small-'+$parts[1]+'-'+$parts[0]+$build+'-'+$(if($Mount -ceq 'dog'){'ridingdog'}else{'horse'})+'-v1'})
  [pscustomobject]@{rig=$_;expectedProfile=$id;resolvedProfile=$id;refusal=$null;mounted=$true
   rigFacts=[pscustomobject]@{originalSize='Small';effectiveSize='Small';skeleton=$_;pelvisHeight=0.4;worldScale=[pscustomobject]@{y=1.0}};sizes=[pscustomobject]@{mountEffective=$Size;riderWorldScale=[pscustomobject]@{y=1.0}}
   afterDismount=[pscustomobject]@{riderEffective='Small'};afterDismountRig=[pscustomobject]@{skeleton=$_;pelvisHeight=0.41;worldScale=[pscustomobject]@{y=1.0}}} })
}
function New-S5Artifact([string]$Scenario){
 $dog=$Scenario -ceq 'sprint5-dog-rt'; $mount=$(if($dog){'dog'}else{'horse'}); $size=$(if($dog){'Medium'}else{'Large'})
 $unit=$(if($dog){'b7a14f25d3c94e0d8e6f1a2c5d90e431'}else{'4016c7db400ab721ff125aef9e65e202'}); $feature=$(if($dog){'5c8e2b9a417f4d63a1d07e3bf26c9184'}else{'7db7c50677e39f09feef56f3831fc723'})
 $profile=$(if($dog){'small-halfling-male-ridingdog-v1'}else{'small-default-halfling-horse-v1'})
 $boundary=[pscustomobject]@{state='Mounted';resolved=$profile}
 $action={ param($rig) $body=[pscustomobject]@{remounted=$true;profileBefore=$profile;gearRemoved=$boundary;gearRestored=$boundary;duringCombat=$boundary;attackClicked=$true;mountAttackClicked=$true;riderAttacks=2;mountAttacks=1
   cooldownsAfterRider=[pscustomobject]@{riderStandard=4.0;mountStandard=0.0};cooldownsDuring=[pscustomobject]@{riderStandard=1.0;mountStandard=5.0};afterDismount=[pscustomobject]@{worldScale=[pscustomobject]@{y=1.0}};unmountedRig=[pscustomobject]@{skeleton=$(if($rig){$rig}else{'male-halfling'});worldScale=[pscustomobject]@{y=1.0}}}; if($rig){ $parts=$rig.Split('-'); $body.profileBefore='small-'+$parts[1]+'-'+$parts[0]+'-'+$parts[2]+'-'+$(if($dog){'ridingdog'}else{'horse'})+'-v1'; $own=[pscustomobject]@{state='Mounted';resolved=$body.profileBefore}; $body.gearRemoved=$own; $body.gearRestored=$own; $body.duringCombat=$own; $body | Add-Member -NotePropertyName rig -NotePropertyValue $rig }; $body }
 $pet='pet-1'
 $state={ param($level,$xp) [pscustomobject]@{id=$pet;master=$owner;size='Medium';characterLevel=$level;classLevel=$level;experience=$xp;hitPointsBase=10*$level;maxHp=11*$level;babBase=$level;fortitudeBase=$level;reflexBase=$level;willBase=$level;classBab=$level;classFortitude=$level;classReflex=$level;classWill=$level;strength=17;constitution=17;upgradeCount=1;upgradeRank=1} }
 $commits=@(1,2,3 | ForEach-Object { [pscustomobject]@{canLevelUp=$true;committed=$true;complete=$true;before=(& $state $_ 9000);after=(& $state ($_+1) 9000)} })
 $rows=New-Object System.Collections.ArrayList
 function Row([string]$Name,$Evidence){ [void]$rows.Add([pscustomobject]@{name=$Name;status='PASS';evidence=$Evidence}) }
 Row 'S5-inventory' ([pscustomobject]@{ridingDog=[pscustomobject]@{guid='b7a14f25d3c94e0d8e6f1a2c5d90e431'};registration=[pscustomobject]@{ridingDogFailure=$null};dogSelections=@([pscustomobject]@{name='a';ridingDogOffered=$true})})
 Row 'S5-medium-control' ([pscustomobject]@{profile='medium-humanoid-mammoth-v1'})
 Row 'S5-acquire' ([pscustomobject]@{acquisition=[pscustomobject]@{acquired=$true;petBlueprint=$unit;rank=1;owner=$owner;optionsOffered=@($feature)};pet=[pscustomobject]@{id=$pet;effectiveSize=$size;master=$owner}})
 if($dog){ Row 'S5-medium-refused' ([pscustomobject]@{rigFacts=[pscustomobject]@{effectiveSize='Medium'};resolved=$null;refusal='The mount must be at least one size category larger than its rider.';stateAfter='none'}) }
 Row 'S5-boot' ([pscustomobject]@{cases=@('female-halfling','male-halfling','female-halfling-left-handed' | ForEach-Object { $left=$_ -cmatch 'left'; [pscustomobject]@{label=$_;leftHanded=$left;mounted=$true;remounted=$true;resolved='p';remountedResolved='p';armorOff=[pscustomobject]@{state='Mounted';resolved='p'};armorOn=[pscustomobject]@{state='Mounted';resolved='p'};mountedRig=[pscustomobject]@{mirror=$left};rigFacts=[pscustomobject]@{pelvisHeight=0.4};afterDismountRig=[pscustomobject]@{pelvisHeight=0.41}} })})
 Row 'S5-rigs' ([pscustomobject]@{rigs=@(New-S5Rigs $mount $size)})
 Row 'S5-actions' (& $action $null)
 Row 'S5-body-actions' ([pscustomobject]@{bodies=@($script:KmcSprint5ExtremeRigs | ForEach-Object { & $action $_ })})
 Row 'S5-left-handed' ([pscustomobject]@{resolved=$profile;mounted=$true;rigFacts=[pscustomobject]@{mirror=$true;leftHanded=$true;animatorScaleX=-1.0};mountedRig=[pscustomobject]@{mirror=$true};restoredRig=[pscustomobject]@{mirror=$false}})
 if($dog){
  Row 'S5-size-change' ([pscustomobject]@{detached=$true;after=[pscustomobject]@{state=$null;resolved=$null};ownership=$owner;restored=[pscustomobject]@{sizes=[pscustomobject]@{riderEffective='Small'}}})
  Row 'S5-growth' ([pscustomobject]@{upgrade=$true;rankAfter=4;before=[pscustomobject]@{strength=15;constitution=15};after=[pscustomobject]@{strength=17;constitution=17;effectiveSize='Medium'};grownMounted=[pscustomobject]@{state='Mounted'}})
  Row 'S5-pet-level' ([pscustomobject]@{commits=$commits;levelBefore=(& $state 1 9000);afterMountedCommit=$boundary;ownerLevelCommits=1;ownerRank=4;ownerRankAfter=5;petAfterCommits=(& $state 4 9000);petAfterOwnerLevel=(& $state 4 12000);petFinal=(& $state 4 12000);petIdentityAfterOwnerLevel=$pet;ridingDogUnits=1;leveledMounted=$boundary})
  Row 'S5-stock-dog' ([pscustomobject]@{mountable=$false})
  Row 'S5-replacement' ([pscustomobject]@{ownerPet=$null;orphans=0})
 }
 [pscustomobject]@{schemaVersion=49;status='PASS';errors=@();subscenarioFailCount=0;rows=@($rows)
  observations=[pscustomobject]@{riderId='r';horseId='m';sprint5Small=[pscustomobject]@{contract='native-small-riders-riding-dog-v3';mount=$mount;controlRider='r';controlMount='m';bodiesAreDisclosedSetup=$true;cases=@(Get-KmcSprint5Cases $Scenario);shots=@(New-S5Shots $mount $dog)}}}
}
function Accept($Artifact,[string]$Scenario){ try { Assert-KmcSprint5SmallEvidence -Request ([pscustomobject]@{scenario=$Scenario;evidenceRoot=''}) -Artifact $Artifact -Status 'PASS'; $true } catch { Write-Output ('      refused: '+$_.Exception.Message); $false } }
function Reject($Artifact,[string]$Scenario,[string]$Name){ $threw=$false; try { Assert-KmcSprint5SmallEvidence -Request ([pscustomobject]@{scenario=$Scenario;evidenceRoot=''}) -Artifact $Artifact -Status 'PASS' } catch { $threw=$true }; Check $threw $Name }
function RowOf($Artifact,[string]$Name){ @($Artifact.rows | Where-Object name -CEQ $Name)[0].evidence }
$dog=New-S5Artifact 'sprint5-dog-rt'; $horse=New-S5Artifact 'sprint5-small-rt'
Check ((Accept $dog 'sprint5-dog-rt')[-1] -eq $true) 'complete Dog evidence accepted'
Check ((Accept $horse 'sprint5-small-rt')[-1] -eq $true) 'complete Horse evidence accepted'
Check ((Get-KmcPhase3dEvidenceDispatch 49 'sprint5-dog-rt').validator -ceq 'Assert-KmcSprint5SmallEvidence') 'schema 49 dispatches to the Sprint 5 reader'
Check (Test-KmcPhase3dSchemaRegistration 49 'sprint5-dog-rt') 'schema 49 is registered for the Dog scenario'
Check (Test-KmcPhase3dSchemaRegistration 49 'sprint5-small-rt') 'schema 49 is registered for the Horse scenario'
Check (-not (Test-KmcPhase3dSchemaRegistration 49 'sprint4-natural-rt')) 'schema 49 is refused for another scenario'
Check (-not (Test-KmcPhase3dSchemaRegistration 48 'sprint5-dog-rt')) 'a Sprint 4 schema is refused for a Sprint 5 scenario'
Check (-not (Test-KmcChildEntryExpectsMounted 'sprint5-dog-rt')) 'a Sprint 5 child starts from its unmounted fixture handoff'
Check (Test-KmcChildEntryRequiresIdleParty 'sprint5-small-rt') 'a Sprint 5 child requires the idle party handoff'
Check ('riding-dog-exploration-manual' -cin (Get-KmcChunk7PersistenceCases)) 'the Riding Dog save case is registered'
Check ((Get-KmcChunk7SecondPairProfile ([pscustomobject]@{persistenceCase='riding-dog-exploration-manual'})) -ceq 'small-halfling-female-ridingdog-v1') 'the Riding Dog save case expects its own profile'
Check ((Get-KmcChunk7SecondPairProfile ([pscustomobject]@{persistenceCase='multi-exploration-manual'})) -ceq 'medium-humanoid-horse-v1') 'every other save case keeps the Medium Horse profile'
$r=Copy-S5 $horse; (RowOf $r 'S5-rigs').rigs[1].resolvedProfile='small-default-halfling-horse-v1'; Reject $r 'sprint5-small-rt' 'a rig admitted to another rig''s profile refuses'
$r=Copy-S5 $horse; (RowOf $r 'S5-rigs').rigs[2].mounted=$false; Reject $r 'sprint5-small-rt' 'a rig that never mounted refuses'
$r=Copy-S5 $horse; $x=(RowOf $r 'S5-rigs'); $x.rigs=@($x.rigs[0..2]); Reject $r 'sprint5-small-rt' 'a missing rig refuses'
$r=Copy-S5 $horse; (RowOf $r 'S5-rigs').rigs[0].sizes.riderWorldScale.y=1.5; Reject $r 'sprint5-small-rt' 'a rider that inherited the mount scale refuses'
$r=Copy-S5 $horse; (RowOf $r 'S5-rigs').rigs[0].rigFacts.effectiveSize='Medium'; Reject $r 'sprint5-small-rt' 'a resized rider refuses'
$r=Copy-S5 $horse; $o=$r.observations.sprint5Small; $o.shots=@($o.shots | Where-Object name -CNE 'horse-male-gnome-idle-side'); Reject $r 'sprint5-small-rt' 'a missing view refuses'
$r=Copy-S5 $horse; @($r.observations.sprint5Small.shots)[0] | Add-Member -NotePropertyName error -NotePropertyValue 'x'; Reject $r 'sprint5-small-rt' 'a failed capture refuses'
$r=Copy-S5 $horse; (RowOf $r 'S5-actions').gearRemoved.resolved=$null; Reject $r 'sprint5-small-rt' 'gear changing the rig profile refuses'
$r=Copy-S5 $horse; (RowOf $r 'S5-actions').mountAttacks=0; Reject $r 'sprint5-small-rt' 'no attack by the mount refuses'
$r=Copy-S5 $horse; (RowOf $r 'S5-actions').cooldownsAfterRider.mountStandard=2.0; Reject $r 'sprint5-small-rt' 'a rider attack spending the mount''s Standard refuses'
$r=Copy-S5 $dog; (RowOf $r 'S5-acquire').pet.effectiveSize='Small'; Reject $r 'sprint5-dog-rt' 'a Riding Dog that is not Medium refuses'
$r=Copy-S5 $dog; (RowOf $r 'S5-acquire').acquisition.optionsOffered=@(); Reject $r 'sprint5-dog-rt' 'a selection that never offered the dog refuses'
$r=Copy-S5 $dog; (RowOf $r 'S5-medium-refused').resolved='small-halfling-male-ridingdog-v1'; Reject $r 'sprint5-dog-rt' 'a Medium rider admitted on a Medium dog refuses'
$r=Copy-S5 $dog; (RowOf $r 'S5-size-change').detached=$false; Reject $r 'sprint5-dog-rt' 'an enlarged rider left mounted refuses'
$r=Copy-S5 $dog; (RowOf $r 'S5-size-change').ownership='other'; Reject $r 'sprint5-dog-rt' 'a detach that lost ownership refuses'
$r=Copy-S5 $dog; (RowOf $r 'S5-growth').upgrade=$false; Reject $r 'sprint5-dog-rt' 'a missing rank-4 advancement refuses'
$r=Copy-S5 $dog; (RowOf $r 'S5-growth').after.effectiveSize='Large'; Reject $r 'sprint5-dog-rt' 'a dog grown past its declared seat refuses'
$r=Copy-S5 $dog; (RowOf $r 'S5-stock-dog').mountable=$true; Reject $r 'sprint5-dog-rt' 'a mountable ordinary Dog refuses'
$r=Copy-S5 $dog; (RowOf $r 'S5-replacement').orphans=1; Reject $r 'sprint5-dog-rt' 'an orphaned Riding Dog refuses'
$r=Copy-S5 $dog; $r.observations.sprint5Small.bodiesAreDisclosedSetup=$false; Reject $r 'sprint5-dog-rt' 'undisclosed body setup refuses'
$r=Copy-S5 $dog; (RowOf $r 'S5-inventory').dogSelections[0].ridingDogOffered=$false; Reject $r 'sprint5-dog-rt' 'a stock-Dog selection without the Riding Dog refuses'
$threw=$false; $r=Copy-S5 $dog; $r.status='FAIL'; $r.subscenarioFailCount=1; try { Assert-KmcSprint5SmallEvidence -Request ([pscustomobject]@{scenario='sprint5-dog-rt';evidenceRoot=''}) -Artifact $r -Status 'PASS' } catch { $threw=$true }; Check $threw 'a failed run cannot be promoted'
$r=Copy-S5 $horse; $o=$r.observations.sprint5Small; foreach($s in @($o.shots)){ if($s.name -cmatch '^horse-male-gnome-fat-idle-close-'){ $s.clear=$false } }; Reject $r 'sprint5-small-rt' 'a fat rig with no unobstructed close view refuses'
$r=Copy-S5 $horse; $x=(RowOf $r 'S5-rigs'); $x.rigs=@($x.rigs[0..3]); Reject $r 'sprint5-small-rt' 'the default rigs alone do not cover the matrix'
$r=Copy-S5 $horse; (RowOf $r 'S5-rigs').rigs[6].afterDismountRig.worldScale.y=1.4; Reject $r 'sprint5-small-rt' 'a scale residue after dismounting refuses'
$r=Copy-S5 $horse; (RowOf $r 'S5-rigs').rigs[6].afterDismountRig.pelvisHeight=0.8; Reject $r 'sprint5-small-rt' 'a pose residue after dismounting refuses'
$r=Copy-S5 $horse; $x=(RowOf $r 'S5-body-actions'); $x.bodies=@($x.bodies[0]); Reject $r 'sprint5-small-rt' 'a missing action extreme refuses'
$r=Copy-S5 $horse; (RowOf $r 'S5-body-actions').bodies[1].mountAttacks=0; Reject $r 'sprint5-small-rt' 'an action extreme without a mount attack refuses'
$r=Copy-S5 $horse; (RowOf $r 'S5-left-handed').rigFacts.animatorScaleX=1.0; Reject $r 'sprint5-small-rt' 'a mirror claim without a mirrored scale refuses'
$r=Copy-S5 $horse; (RowOf $r 'S5-left-handed').mounted=$false; Reject $r 'sprint5-small-rt' 'a mirrored rig that never sat refuses'
$r=Copy-S5 $horse; (RowOf $r 'S5-left-handed').restoredRig.mirror=$true; Reject $r 'sprint5-small-rt' 'a mirror left behind refuses'
$r=Copy-S5 $horse; (RowOf $r 'S5-left-handed').rigFacts.mirror=$false; Reject $r 'sprint5-small-rt' 'an unmirrored setup is not left-handed evidence'
$r=Copy-S5 $horse; (RowOf $r 'S5-body-actions').bodies[0].profileBefore='small-gnome-male-horse-v1'; Reject $r 'sprint5-small-rt' 'an action extreme on another body refuses'
foreach($scenario in 'sprint5-dog-rt','sprint5-small-rt'){ foreach($row in (Get-KmcSprint5Cases $scenario)){ Check (@(Get-KmcPhase3dHorseRuntimeRows $scenario | Where-Object { $_ -ceq $row }).Count -eq 1) ($scenario+' registers row '+$row) } }
$r=Copy-S5 $horse; (RowOf $r 'S5-boot').cases[0].remounted=$false; Reject $r 'sprint5-small-rt' 'a near boot case that never remounted refuses'
$r=Copy-S5 $horse; (RowOf $r 'S5-boot').cases[1].armorOff.state='Unmounted'; Reject $r 'sprint5-small-rt' 'a near boot case that lost its seat across gear refuses'
$r=Copy-S5 $horse; (RowOf $r 'S5-boot').cases[2].mountedRig.mirror=$false; Reject $r 'sprint5-small-rt' 'an unmirrored near boot mirror case refuses'
$r=Copy-S5 $dog; (@($r.observations.sprint5Small.shots | Where-Object name -CEQ 'dog-boot-female-halfling-mounted-near')[0]).boot.frame=8; Reject $r 'sprint5-dog-rt' 'geometry from another frame than its pixels refuses'
$r=Copy-S5 $dog; $r.observations.sprint5Small.shots=@($r.observations.sprint5Small.shots | Where-Object name -CNE 'dog-boot-male-halfling-mounted-near-nomount'); Reject $r 'sprint5-dog-rt' 'a missing near boot view refuses'
$r=Copy-S5 $horse; (@($r.observations.sprint5Small.shots | Where-Object name -CEQ 'horse-boot-female-halfling-mounted-near')[0]).boot.parts[0] | Add-Member -NotePropertyName mappingStoppedAt -NotePropertyValue 103; Reject $r 'sprint5-small-rt' 'a seated rig that still stops the native merge refuses'
$r=Copy-S5 $horse; (@($r.observations.sprint5Small.shots | Where-Object name -CEQ 'horse-boot-female-halfling-mounted-near')[0]).boot.rightBoot.count=0; Reject $r 'sprint5-small-rt' 'a seated rig with no right lower leg refuses'
$r=Copy-S5 $horse; $r.observations.sprint5Small.shots=@($r.observations.sprint5Small.shots | Where-Object name -CNE 'horse-action-cast-close'); Reject $r 'sprint5-small-rt' 'a missing close cast view refuses'
$r=Copy-S5 $horse; $r.observations.sprint5Small.shots=@($r.observations.sprint5Small.shots | Where-Object name -CNE 'horse-female-gnome-thin-move-close-2'); Reject $r 'sprint5-small-rt' 'a missing close motion view refuses'
$r=Copy-S5 $dog; $x=(RowOf $r 'S5-pet-level'); $x.commits=@($x.commits[0..1]); Reject $r 'sprint5-dog-rt' 'an incomplete pet level sequence refuses'
$r=Copy-S5 $dog; (RowOf $r 'S5-pet-level').commits[0].complete=$false; Reject $r 'sprint5-dog-rt' 'a commit with unmade choices refuses'
$r=Copy-S5 $dog; (RowOf $r 'S5-pet-level').commits[1].after.experience=99; Reject $r 'sprint5-dog-rt' 'a pet level that moved experience refuses'
$r=Copy-S5 $dog; (RowOf $r 'S5-pet-level').commits[1].after.characterLevel=9; Reject $r 'sprint5-dog-rt' 'a level jump refuses'
$r=Copy-S5 $dog; (RowOf $r 'S5-pet-level').commits[1].after.babBase=9; Reject $r 'sprint5-dog-rt' 'a BAB off the class table refuses'
$r=Copy-S5 $dog; (RowOf $r 'S5-pet-level').commits[2].after.hitPointsBase=0; Reject $r 'sprint5-dog-rt' 'a level without hit points refuses'
$r=Copy-S5 $dog; (RowOf $r 'S5-pet-level').commits[2].after.upgradeRank=2; Reject $r 'sprint5-dog-rt' 'a second upgrade rank refuses'
$r=Copy-S5 $dog; (RowOf $r 'S5-pet-level').petAfterOwnerLevel.upgradeCount=2; Reject $r 'sprint5-dog-rt' 'a re-granted upgrade after the owner level refuses'
$r=Copy-S5 $dog; (RowOf $r 'S5-pet-level').petFinal.strength=19; Reject $r 'sprint5-dog-rt' 'changed scores after remounting refuse'
$r=Copy-S5 $dog; (RowOf $r 'S5-pet-level').commits[1].after.id='other'; Reject $r 'sprint5-dog-rt' 'a replaced pet refuses'
$r=Copy-S5 $dog; (RowOf $r 'S5-pet-level').commits[1].after.size='Large'; Reject $r 'sprint5-dog-rt' 'an unexplained size change refuses'
$r=Copy-S5 $dog; (RowOf $r 'S5-pet-level').ridingDogUnits=2; Reject $r 'sprint5-dog-rt' 'a duplicate companion refuses'
$r=Copy-S5 $dog; (RowOf $r 'S5-pet-level').afterMountedCommit.state='Unmounted'; Reject $r 'sprint5-dog-rt' 'a level that broke the mounted pair refuses'
Write-Output ('TOTAL PASS='+$script:pass+' FAIL='+$script:fail)
if($script:fail -ne 0){ exit 1 }
