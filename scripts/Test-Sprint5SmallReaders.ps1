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
 foreach($rig in 'male-halfling','female-halfling','male-gnome','female-gnome'){ foreach($v in 'idle-side','idle-front','idle-rear','move-1','move-2'){ [void]$names.Add($Mount+'-'+$rig+'-'+$v) } }
 foreach($v in 'gear-removed','gear-restored','cast','turn','attack-1','attack-2','attack-3','mount-attack','dismounted'){ [void]$names.Add($Mount+'-action-'+$v) }
 @($names | ForEach-Object { [pscustomobject]@{name=$_;file='movement-visuals/sprint5-'+$_+'.png';bytes=10;sha256='a'} })
}
function New-S5Rigs([string]$Mount,[string]$Size){
 $p=@{'male-halfling'='small-default-halfling-horse-v1';'female-halfling'='small-halfling-female-horse-v1';'male-gnome'='small-gnome-male-horse-v1';'female-gnome'='small-gnome-female-horse-v1'}
 if($Mount -ceq 'dog'){$p=@{'male-halfling'='small-halfling-male-ridingdog-v1';'female-halfling'='small-halfling-female-ridingdog-v1';'male-gnome'='small-gnome-male-ridingdog-v1';'female-gnome'='small-gnome-female-ridingdog-v1'}}
 @('male-halfling','female-halfling','male-gnome','female-gnome' | ForEach-Object { [pscustomobject]@{rig=$_;expectedProfile=$p[$_];resolvedProfile=$p[$_];refusal=$null;mounted=$true
   rigFacts=[pscustomobject]@{originalSize='Small';effectiveSize='Small';worldScale=[pscustomobject]@{y=1.0}};sizes=[pscustomobject]@{mountEffective=$Size;riderWorldScale=[pscustomobject]@{y=1.0}};afterDismount=[pscustomobject]@{riderEffective='Small'}} })
}
function New-S5Artifact([string]$Scenario){
 $dog=$Scenario -ceq 'sprint5-dog-rt'; $mount=$(if($dog){'dog'}else{'horse'}); $size=$(if($dog){'Medium'}else{'Large'})
 $unit=$(if($dog){'b7a14f25d3c94e0d8e6f1a2c5d90e431'}else{'4016c7db400ab721ff125aef9e65e202'}); $feature=$(if($dog){'5c8e2b9a417f4d63a1d07e3bf26c9184'}else{'7db7c50677e39f09feef56f3831fc723'})
 $profile=$(if($dog){'small-halfling-male-ridingdog-v1'}else{'small-default-halfling-horse-v1'})
 $boundary=[pscustomobject]@{state='Mounted';resolved=$profile}
 $rows=New-Object System.Collections.ArrayList
 function Row([string]$Name,$Evidence){ [void]$rows.Add([pscustomobject]@{name=$Name;status='PASS';evidence=$Evidence}) }
 Row 'S5-inventory' ([pscustomobject]@{ridingDog=[pscustomobject]@{guid='b7a14f25d3c94e0d8e6f1a2c5d90e431'};registration=[pscustomobject]@{ridingDogFailure=$null};dogSelections=@([pscustomobject]@{name='a';ridingDogOffered=$true})})
 Row 'S5-medium-control' ([pscustomobject]@{profile='medium-humanoid-mammoth-v1'})
 Row 'S5-acquire' ([pscustomobject]@{acquisition=[pscustomobject]@{acquired=$true;petBlueprint=$unit;rank=1;owner=$owner;optionsOffered=@($feature)};pet=[pscustomobject]@{effectiveSize=$size;master=$owner}})
 if($dog){ Row 'S5-medium-refused' ([pscustomobject]@{rigFacts=[pscustomobject]@{effectiveSize='Medium'};resolved=$null;refusal='The mount must be at least one size category larger than its rider.';stateAfter='none'}) }
 Row 'S5-rigs' ([pscustomobject]@{rigs=@(New-S5Rigs $mount $size)})
 Row 'S5-actions' ([pscustomobject]@{remounted=$true;profileBefore=$profile;gearRemoved=$boundary;gearRestored=$boundary;duringCombat=$boundary;attackClicked=$true;mountAttackClicked=$true;riderAttacks=2;mountAttacks=1
   cooldownsAfterRider=[pscustomobject]@{riderStandard=4.0;mountStandard=0.0};cooldownsDuring=[pscustomobject]@{riderStandard=1.0;mountStandard=5.0};afterDismount=[pscustomobject]@{worldScale=[pscustomobject]@{y=1.0}};unmountedRig=[pscustomobject]@{worldScale=[pscustomobject]@{y=1.0}}})
 if($dog){
  Row 'S5-size-change' ([pscustomobject]@{detached=$true;after=[pscustomobject]@{state=$null;resolved=$null};ownership=$owner;restored=[pscustomobject]@{sizes=[pscustomobject]@{riderEffective='Small'}}})
  Row 'S5-growth' ([pscustomobject]@{upgrade=$true;rankAfter=4;before=[pscustomobject]@{strength=15;constitution=15};after=[pscustomobject]@{strength=17;constitution=17;effectiveSize='Medium'};grownMounted=[pscustomobject]@{state='Mounted'}})
  Row 'S5-stock-dog' ([pscustomobject]@{mountable=$false})
  Row 'S5-replacement' ([pscustomobject]@{ownerPet=$null;orphans=0})
 }
 [pscustomobject]@{schemaVersion=49;status='PASS';errors=@();subscenarioFailCount=0;rows=@($rows)
  observations=[pscustomobject]@{riderId='r';horseId='m';sprint5Small=[pscustomobject]@{contract='native-small-riders-riding-dog-v1';mount=$mount;controlRider='r';controlMount='m';bodiesAreDisclosedSetup=$true;cases=@(Get-KmcSprint5Cases $Scenario);shots=@(New-S5Shots $mount $dog)}}}
}
function Accept($Artifact,[string]$Scenario){ try { Assert-KmcSprint5SmallEvidence -Request ([pscustomobject]@{scenario=$Scenario;evidenceRoot=''}) -Artifact $Artifact -Status 'PASS'; $true } catch { Write-Output ('      refused: '+$_.Exception.Message); $false } }
function Reject($Artifact,[string]$Scenario,[string]$Name){ $threw=$false; try { Assert-KmcSprint5SmallEvidence -Request ([pscustomobject]@{scenario=$Scenario;evidenceRoot=''}) -Artifact $Artifact -Status 'PASS' } catch { $threw=$true }; Check $threw $Name }
function RowOf($Artifact,[string]$Name){ @($Artifact.rows | Where-Object name -CEQ $Name)[0].evidence }
$dog=New-S5Artifact 'sprint5-dog-rt'; $horse=New-S5Artifact 'sprint5-small-rt'
Check ((Accept $dog 'sprint5-dog-rt')[-1] -eq $true) 'complete Dog evidence accepted'
Check ((Accept $horse 'sprint5-small-rt')[-1] -eq $true) 'complete Horse evidence accepted'
Check ((Get-KmcPhase3dEvidenceDispatch 49 'sprint5-dog-rt').validator -ceq 'Assert-KmcSprint5SmallEvidence') 'schema 49 dispatches to the Sprint 5 reader'
Check (-not (Test-KmcPhase3dSchemaRegistration 48 'sprint5-dog-rt')) 'a Sprint 4 schema is refused for a Sprint 5 scenario'
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
Write-Output ('TOTAL PASS='+$script:pass+' FAIL='+$script:fail)
if($script:fail -ne 0){ exit 1 }
