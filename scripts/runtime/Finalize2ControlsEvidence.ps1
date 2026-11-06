# F2 (ART-02, CTRL-01/02/03) external reader for finalize-f2-controls-rt. The rows only record native observations;
# PASS is decided here, strictly: a missing observation is a failure, never a pass.
function Get-KmcFinalize2Cases { @('F2-placement','F2-strings','F2-mount-cycle','F2-alias-and-removal','F2-charge-mounted','F2-refusals','F2-charge-unmounted') }
function Get-KmcFinalize2ShotNames { @('public-control-unmounted','public-control-mounted','charge-target-selection-mounted') }
function Get-KmcFinalize2Identities {
 [ordered]@{
  publicControl='26151090ad4744ea96dbb7e0537a4a7f'; mountShell='f053faad986631688defa003cd7bda0e'; dismountShell='3af2b81f4d72bbb30501fa730fcdf36e'
  mountedChargeShell='d79eaec224a7a832e738eb81baef9d49'; stockCharge='c78506dd0e14f7c45a599990e4e65038'
  expectedFeedback="Charge was cancelled because the rider's mount state changed. Choose Charge again."
 }
}
function Assert-KmcFinalize2ControlsEvidence {
 param([Parameter(Mandatory=$true)]$Request,[Parameter(Mandatory=$true)]$Artifact,[AllowNull()][string]$Status,[string]$EvidenceRoot)
 function Fail([string]$Why){ throw ('F2 '+$Why) }
 function P($O,[string]$N){ if($null -ne $O -and $O -isnot [string] -and $O -isnot [ValueType] -and $null -ne $O.PSObject.Properties[$N]){ $O.$N } else { $null } }
 function Num($V,[string]$What){ if($V -isnot [int] -and $V -isnot [long] -and $V -isnot [double] -and $V -isnot [single] -and $V -isnot [decimal]){ Fail ($What+' is missing or not a number') }; [double]$V }
 function Int($V,[string]$What){ if($V -isnot [int] -and $V -isnot [long]){ Fail ($What+' is missing or not an integer') }; [long]$V }
 function Yes($V,[string]$What){ if($V -isnot [bool] -or $V -ne $true){ Fail $What } }
 function No($V,[string]$What){ if($V -isnot [bool] -or $V -ne $false){ Fail $What } }
 function Text($V){ if($V -is [string]){ $V } else { $null } }
 function Same([double]$A,[double]$B){ [math]::Abs($A-$B) -le 0.001 }
 function Relation($State,[string]$Expected,[string]$What){ if((Text (P $State 'relationship')) -cne $Expected){ Fail ($What+': the relationship is not '+$Expected) } }
 # A click on the rider's own bar slot: the slot keeps its own binding after the native body ran on the delivery shell.
 function SlotClick($Click,[string]$SlotGuid,[string]$What){
  if($null -eq $Click){ Fail ($What+': the slot click was not recorded') }
  Yes (P $Click 'slotFound') ($What+': no slot at the recorded index')
  if((Text (P $Click 'slotAbilityBefore')) -cne $SlotGuid -or (Text (P $Click 'slotAbilityAfter')) -cne $SlotGuid){ Fail ($What+': the clicked slot does not hold its public identity') }
  Yes (P $Click 'slotDataRestored') ($What+': the slot did not get its own AbilityData back')
  Yes (P $Click 'slotStillBound') ($What+': the slot object was replaced during the click')
 }
 function Terminal($T,[string]$What){
  if($null -eq $T){ Fail ($What+': no charge terminal') }
  try { Assert-KmcSprint7Lease $T ('F2 '+$What) } catch { Fail ($What+': '+$_.Exception.Message) }
  if((P $T 'sequenceLawful') -ne $true -or (Text (P $T 'result')) -cne 'Success' -or (P $T 'revalidationFailed') -eq $true){ Fail ($What+': the charge did not complete its lawful sequence') }
  if((Int (P $T 'rules') ($What+' rider attack count')) -lt 1){ Fail ($What+': no rider attack after the click') }
  if((Int (P $T 'mountRules') ($What+' mount attack count')) -ne 0){ Fail ($What+': the mount attacked during the charge') }
 }

 if([string]$Request.scenario -cne 'finalize-f2-controls-rt' -or $Artifact.schemaVersion -ne 53){ Fail 'scenario/schema mismatch' }
 if($Artifact.status -ceq 'FAIL'){
  if($Status -ceq 'PASS' -or (@($Artifact.errors).Count -eq 0 -and $Artifact.subscenarioFailCount -lt 1)){ Fail 'incomplete observation cannot become PASS' }
  return # Preserve the producer's explicit failed prefix; grant no qualification.
 }
 if($Artifact.status -cne 'PASS' -or @($Artifact.errors).Count -ne 0 -or @($Artifact.rows | Where-Object status -CNE 'PASS').Count -ne 0 -or $Artifact.subscenarioFailCount -ne 0){ Fail 'envelope differs' }
 $o=P (P $Artifact 'observations') 'finalize2Controls'
 if($null -eq $o -or (Text (P $o 'contract')) -cne 'finalize-f2-canonical-controls-v1' -or (Text (P $o 'mode')) -cne 'RT'){ Fail 'measurement contract differs' }
 $riderId=Text (P $o 'rider'); $mountId=Text (P $o 'mount')
 if([string]::IsNullOrEmpty($riderId) -or [string]::IsNullOrEmpty($mountId) -or $riderId -ceq $mountId -or $riderId -cne [string](P $Artifact.observations 'riderId') -or $mountId -cne [string](P $Artifact.observations 'horseId')){ Fail 'measurement identity differs' }
 if((P $o 'defaultFeatureEnabled') -ne $true){ Fail 'the rows did not run under the ordinary default' }
 if((Text (P $o 'startRelationship')) -cne 'Unmounted'){ Fail 'the child did not start from its unmounted handoff' }
 $id=Get-KmcFinalize2Identities; $ids=P $o 'identities'
 foreach($key in $id.Keys){ if((Text (P $ids $key)) -cne [string]$id[$key]){ Fail ('identity differs: '+$key) } }
 if((Int (P $ids 'mainBarSlots') 'main-bar size') -ne 14){ Fail 'main-bar size differs' }
 $expected=@(Get-KmcFinalize2Cases)
 if((@(P $o 'cases') | ConvertTo-Json -Compress) -cne ($expected | ConvertTo-Json -Compress)){ Fail 'registered row order differs' }
 if(@($Artifact.rows).Count -ne $expected.Count){ Fail 'the artifact holds rows other than the registered F2 rows' }
 $rows=@{}
 foreach($name in $expected){
  $found=@($Artifact.rows | Where-Object name -CEQ $name)
  if($found.Count -ne 1){ Fail ('required row missing or duplicated: '+$name) }
  if((Text (P $found[0].evidence 'case')) -cne $name){ Fail ('row evidence names another case: '+$name) }
  $rows[$name]=$found[0].evidence
 }
 $setup=@(P $o 'disclosedSetup')
 if($setup.Count -lt 6 -or @($setup | Where-Object { [string]::IsNullOrWhiteSpace((Text (P $_ 'action'))) }).Count -ne 0){ Fail 'the setup actions were not disclosed' }

 # ---- F2-placement --------------------------------------------------------------------------------------------------
 $p=$rows['F2-placement']
 if((P $p 'autofill') -ne $true){ Fail 'the native autofill setting was not on in this profile, so the placement cannot qualify' }
 $controlIndices=@(P $p 'controlIndices')
 if($controlIndices.Count -ne 1){ Fail ('the rider holds '+$controlIndices.Count+' public controls, not exactly one') }
 $index=Int $controlIndices[0] 'public control index'
 $records=@(@(P $p 'placements') | Where-Object { (Text (P $_ 'unitId')) -ceq $riderId })
 if($records.Count -ne 1){ Fail ('the rider has '+$records.Count+' initial-placement records, not exactly one') }
 if((Int (P $records[0] 'index') 'placement index') -ne $index -or $index -lt 0 -or $index -gt 13){ Fail 'the recorded placement is not the control''s main-bar slot' }
 $before=@(P $records[0] 'mainBarBefore'); $now=@(P $p 'mainBarNow')
 if($before.Count -ne 14 -or $now.Count -ne 14){ Fail 'the main-bar snapshots are not fourteen slots' }
 if($null -eq $before[$index] -or [string]$before[$index] -cne ''){ Fail 'the placement went over an occupied slot' }
 for($j=0;$j -lt $index;$j++){ if([string]$before[$j] -ceq ''){ Fail 'the placement was not the first empty main-bar slot' } }
 for($j=0;$j -lt 14;$j++){
  if($j -eq $index){ if([string]$now[$j] -cne $id.publicControl){ Fail 'the placed slot does not hold the public control' } }
  elseif($null -eq $now[$j] -or [string]$now[$j] -cne [string]$before[$j]){ Fail ('unrelated main-bar slot '+$j+' changed') }
 }
 Yes (P $p 'placementRecorded') 'the placement is not recorded for the rider'
 # A full bar at load: the load's own placement wrote nothing; the first placement then took the one slot the player freed.
 $freed=P $p 'freedSlot'
 if($null -ne $freed){
  $load=P $p 'loadObservation'
  $loadRecords=@(P $load 'placements')
  if($loadRecords.Count -ne 1 -or (Int (P $loadRecords[0] 'index') 'load placement index') -ne -1){ Fail 'the full-bar load did not record exactly one placement attempt with nothing placed' }
  $loadBefore=@(P $loadRecords[0] 'mainBarBefore'); $loadNow=@(P $load 'mainBarNow')
  if($loadBefore.Count -ne 14 -or $loadNow.Count -ne 14 -or @($loadBefore | Where-Object { [string]$_ -ceq '' }).Count -ne 0){ Fail 'the load-time main bar was not full' }
  for($j=0;$j -lt 14;$j++){ if([string]$loadNow[$j] -cne [string]$loadBefore[$j]){ Fail ('the full main bar changed at slot '+$j) } }
  if(@(P $load 'controlIndices').Count -ne 0){ Fail 'a public control appeared on the full bar' }
  if((Int $freed 'freed slot') -ne $index){ Fail 'the first placement did not take the freed slot' }
  for($j=0;$j -lt 14;$j++){ if($j -ne $index -and [string]$before[$j] -cne [string]$loadBefore[$j]){ Fail ('slot '+$j+' changed between the load and the first placement') } }
 }
 Yes (P $p 'factActive') 'the public control fact is not active'
 Yes (P $p 'slotHoldsLiveFact') 'the placed slot is not bound to the live fact'
 if((Int (P (P $p 'countersAtRow') 'initialPlacement') 'placement count') -lt 1){ Fail 'no initial placement was counted' }

 # ---- F2-strings ----------------------------------------------------------------------------------------------------
 $s=$rows['F2-strings']
 Yes (P $s 'tableFound') 'the owned key table was not read'
 Yes (P $s 'packPresent') 'the current localization pack was not read'
 $keys=@(P $s 'keys')
 $wanted=[ordered]@{'KMC.Native.MountState.Name'='Mount / Dismount';'KMC.Native.MountedCharge.Name'='Charge';'KMC.Native.MountState.Description'=$null;'KMC.Native.MountedCharge.Description'=$null;'KMC.Native.Mount.Name'=$null;'KMC.Native.Dismount.Name'=$null}
 foreach($key in $wanted.Keys){
  $entry=@($keys | Where-Object { (Text (P $_ 'key')) -ceq $key })
  if($entry.Count -ne 1){ Fail ('owned key missing: '+$key) }
  if($null -ne $wanted[$key] -and (Text (P $entry[0] 'expected')) -cne $wanted[$key]){ Fail ('owned key holds another text: '+$key) }
 }
 foreach($entry in $keys){
  $key=Text (P $entry 'key')
  if([string]::IsNullOrEmpty($key) -or -not $key.StartsWith('KMC.Native.',[StringComparison]::Ordinal)){ Fail ('a foreign key is in the owned table: '+$key) }
  if((P $entry 'present') -ne $true -or (P $entry 'actual') -isnot [string] -or (Text (P $entry 'actual')) -cne (Text (P $entry 'expected'))){ Fail ('owned key does not resolve to its value: '+$key) }
 }
 $controls=@(P $s 'controls')
 $kinds=[ordered]@{MountState=@($id.publicControl,$false);MountCompanion=@($id.mountShell,$true);Dismount=@($id.dismountShell,$true);MountedCharge=@($id.mountedChargeShell,$true);RiderPrimary=@($null,$null);MountPrimary=@($null,$null)}
 foreach($kind in $kinds.Keys){
  $c=@($controls | Where-Object { (Text (P $_ 'kind')) -ceq $kind })
  if($c.Count -ne 1){ Fail ('control missing from the string row: '+$kind) }
  $name=Text (P $c[0] 'name'); $description=Text (P $c[0] 'description')
  if([string]::IsNullOrWhiteSpace($name) -or $name -match '\[unknown key' -or [string]$description -match '\[unknown key' -or $name -cmatch '^KMC\.' -or [string]$description -cmatch '^KMC\.'){ Fail ('control text is unresolved: '+$kind) }
  if($null -ne $kinds[$kind][0] -and ((Text (P $c[0] 'guid')) -cne $kinds[$kind][0] -or (P $c[0] 'hidden') -ne $kinds[$kind][1])){ Fail ('control identity or visibility differs: '+$kind) }
 }
 if((Text (P @($controls | Where-Object { (Text (P $_ 'kind')) -ceq 'MountState' })[0] 'name')) -cne 'Mount / Dismount'){ Fail 'the public control is not named Mount / Dismount' }
 if((Text (P @($controls | Where-Object { (Text (P $_ 'kind')) -ceq 'MountedCharge' })[0] 'name')) -cne 'Charge'){ Fail 'the mounted Charge shell is not named Charge' }

 # ---- F2-mount-cycle ------------------------------------------------------------------------------------------------
 $m=$rows['F2-mount-cycle']; $ms=P $m 'steps'
 $cycleIndex=Int (P (P $m 'start') 'index') 'cycle control index'
 if($cycleIndex -lt 0){ Fail 'the cycle found no single public control' }
 No (P $m 'controlPlacedBySetup') 'the mount cycle ran on a control placed by setup, not the automatic placement'
 if($cycleIndex -ne $index){ Fail 'the mount cycle did not start from the automatically placed slot' }
 No (P $m 'fallbackUsed') 'a mount-cycle transition fell back to the hidden shell'
 foreach($leg in @(@{n='mount1';mount=$true;from='Unmounted';to='Mounted';end='mounted1'},@{n='dismount';mount=$false;from='Mounted';to='Unmounted';end='dismounted'},@{n='mount2';mount=$true;from='Unmounted';to='Mounted';end='mounted2'})){
  $what='mount cycle '+$leg.n; $click=P $ms ($leg.n+'Click')
  SlotClick $click $id.publicControl $what
  if((Int (P $click 'index') ($what+' index')) -ne $cycleIndex){ Fail ($what+': clicked another slot') }
  Relation (P $click 'before') $leg.from $what
  if((P (P $click 'before') 'riderInCombat') -ne $false){ Fail ($what+': the cycle was not in exploration') }
  $shell=if($leg.mount){$id.mountShell}else{$id.dismountShell}
  if($leg.mount){
   if((Text (P $click 'handlerAfter')) -cne $id.mountShell -or (Text (P $click 'handlerCaster')) -cne $riderId){ Fail ($what+': the slot did not select the Mount shell for the rider') }
   $target=P $ms ($leg.n+'Target')
   if($null -eq $target -or (P $target 'clicked') -ne $true -or (Text (P $target 'clickedId')) -cne $mountId -or (Text (P $target 'handlerAbility')) -cne $id.mountShell){ Fail ($what+': the companion click did not take the Mount') }
  } else {
   if((Text (P $click 'handlerAfter')) -ceq $id.dismountShell -or (P $click 'handlerIsShell') -ne $false){ Fail ($what+': Dismount waited for a target instead of running') }
  }
  $command=P $ms ($leg.n+'Command')
  if($null -eq $command -or (P $command 'found') -ne $true -or (Text (P $command 'blueprint')) -cne $shell -or (P $command 'fallback') -ne $false){ Fail ($what+': the slot produced no '+$leg.n+' shell command') }
  $end=P $ms $leg.end
  Relation (P $end 'state') $leg.to $what
  if((Text (P $end 'shellResult')) -cne 'Success' -or (Text (P $end 'shellBlueprint')) -cne $shell){ Fail ($what+': the shell command did not succeed') }
  foreach($field in 'riderMove','mountMove'){ if((Num (P (P $end 'state') $field) ($what+' '+$field)) -gt (Num (P (P $click 'before') $field) ($what+' '+$field))+0.001){ Fail ($what+': exploration spent '+$field) } }
 }
 $watch=P $m 'frameWatch'
 if((Int (P $watch 'index') 'watched index') -ne $cycleIndex -or (Int (P $watch 'frames') 'watched frames') -lt 1){ Fail 'the control was not watched through the cycle' }
 foreach($field in 'absentFrames','factReplacedFrames','countNotOneFrames','slotDataNotFactFrames','indexMovedFrames'){ if((Int (P $watch $field) $field) -ne 0){ Fail ('the public control changed during the cycle: '+$field) } }

 # ---- F2-alias-and-removal ------------------------------------------------------------------------------------------
 $a=$rows['F2-alias-and-removal']
 $removal=P $a 'removal'
 $removedIndex=Int (P $removal 'index') 'removed index'
 if($removedIndex -lt 0){ Fail 'the public control was not removed' }
 if(@(P (P $removal 'barAfterRemoval') 'keys') -ccontains $id.publicControl){ Fail 'the public control stayed on the bar after its removal' }
 $placementBefore=Int (P (P $removal 'countersBefore') 'initialPlacement') 'placement count before removal'
 if((Int (P (P $a 'after60') 'frames') 'frames after removal') -lt 60){ Fail 'the removal was not held sixty frames' }
 foreach($n in 'after60','removalResult'){
  $x=P $a $n
  if($null -eq $x -or @(P $x 'indices').Count -ne 0){ Fail ('the removed control came back ('+$n+')') }
  if((Int (P (P $x 'counters') 'initialPlacement') ($n+' placement count')) -ne $placementBefore){ Fail ('a removed control was placed again ('+$n+')') }
 }
 if(@(P (P (P $a 'removalResult') 'bar') 'keys') -ccontains $id.publicControl){ Fail 'the removed control was reinserted elsewhere' }
 Yes (P (P $a 'removalResult') 'placementRecorded') 'the removal is not remembered as the actor''s placement'
 Yes (P $a 'removalDismountClicked') 'the removal cycle''s Dismount was not clicked'
 Yes (P $a 'removalMountClicked') 'the removal cycle''s Mount was not clicked'
 Relation (P $a 'removalDismounted') 'Unmounted' 'removal cycle'
 Relation (P $a 'removalMounted') 'Mounted' 'removal cycle'
 $injection=P $a 'injection'
 $k1=Int (P $injection 'k1') 'k1'; $k2=Int (P $injection 'k2') 'k2'
 if($k1 -lt 0 -or $k2 -lt 0 -or $k1 -eq $k2){ Fail 'the old bindings were not injected into two slots' }
 # Reader correction after run 20261105-finalize-f2-r02: an exact old slot is repaired in place wherever it is. With the
 # fixture's full main bar the injection rightly used empty slots of the additional panel; only the persisted range matters.
 if($k1 -gt 127 -or $k2 -gt 127){ Fail 'the old bindings were injected outside the persisted slot range' }
 if((Text (P $injection 'k1Guid')) -cnotin @($id.mountShell,$id.dismountShell) -or (Text (P $injection 'k2Guid')) -cne $id.mountedChargeShell){ Fail 'the injected bindings are not the exact old identities' }
 $barBefore=P $injection 'barBefore'; $keysBefore=@(P $barBefore 'keys'); $typesBefore=@(P $barBefore 'types')
 $repaired=P $a 'repaired'; $keysAfter=@(P (P $repaired 'bar') 'keys'); $typesAfter=@(P (P $repaired 'bar') 'types')
 if($keysBefore.Count -lt 14 -or $keysBefore.Count -ne $keysAfter.Count -or $typesBefore.Count -ne $keysBefore.Count -or $typesAfter.Count -ne $keysAfter.Count){ Fail 'the full-bar snapshots differ in shape' }
 if([string]$keysBefore[$k1] -cne '' -or [string]$keysBefore[$k2] -cne ''){ Fail 'the injection went over an occupied slot' }
 if((Text (P $repaired 'k1Key')) -cne $id.publicControl -or (P $repaired 'k1IsLiveControl') -ne $true){ Fail 'the old Mount/Dismount slot was not repaired to the public control in place' }
 if((P $injection 'stockChargeOnBarBefore') -eq $true){
  if((Text (P $repaired 'k2Key')) -cne ''){ Fail 'the old Mounted Charge slot became a second Charge' }
 } elseif((P $injection 'stockChargeOnBarBefore') -eq $false){
  if((Text (P $repaired 'k2Key')) -cne $id.stockCharge -or (P $repaired 'k2IsLiveStock') -ne $true){ Fail 'the old Mounted Charge slot was not repaired to the rider''s stock Charge' }
 } else { Fail 'whether the stock Charge was already on the bar was not recorded' }
 $countersBefore=P $injection 'countersBefore'; $countersAfter=P $repaired 'countersAfter'
 if((Int (P $countersAfter 'legacySlotRepair') 'repairs after')-(Int (P $countersBefore 'legacySlotRepair') 'repairs before') -ne 2){ Fail 'the repair count did not rise by exactly two' }
 if((Int (P $countersAfter 'initialPlacement') 'placements after') -ne $placementBefore){ Fail 'the repair counted as a new placement' }
 for($j=0;$j -lt $keysBefore.Count;$j++){
  if($j -eq $k1 -or $j -eq $k2){ continue }
  if([string]$keysAfter[$j] -cne [string]$keysBefore[$j] -or [string]$typesAfter[$j] -cne [string]$typesBefore[$j]){ Fail ('unrelated slot '+$j+' changed during the repair') }
 }
 if((@(P $repaired 'controlIndices') | ConvertTo-Json -Compress) -cne (@($k1) | ConvertTo-Json -Compress)){ Fail 'the public control is not exactly at the repaired slot' }

 # ---- F2-charge-mounted ---------------------------------------------------------------------------------------------
 $c=$rows['F2-charge-mounted']; $cs=P $c 'steps'
 $slot=P $c 'chargeSlot'; $chargeIndex=Int (P $slot 'index') 'Charge slot index'
 if($chargeIndex -lt 0 -or (Text (P $slot 'source')) -cnotin @('pre-existing','repaired-old-binding','placed-setup') -or (P $slot 'preexistingAtScenarioStart') -isnot [bool]){ Fail 'the stock Charge slot was not recorded' }
 Relation (P $c 'combatEntered') 'Mounted' 'mounted Charge combat entry'
 Yes (P (P $c 'combatEntered') 'riderInCombat') 'the mounted Charge row did not enter combat'
 $tooNear=P $cs 'tooNear'
 if($null -eq $tooNear -or (P $tooNear 'canTarget') -ne $false -or (P $tooNear 'policyAllowed') -ne $false){ Fail 'a too-near hostile was targetable by the ordinary Charge' }
 $reason=[string](Text (P $tooNear 'reason'))
 if($reason -match 'Mounted Charge' -or $reason -cmatch 'KMC\.'){ Fail ('the ordinary Charge shows an internal reason: '+$reason) }
 foreach($n in 'selectionClick','chargeClick'){
  $click=P $cs $n
  SlotClick $click $id.stockCharge ('mounted '+$n)
  if((Int (P $click 'index') ($n+' index')) -ne $chargeIndex){ Fail ('mounted '+$n+': clicked another slot') }
  Relation (P $click 'before') 'Mounted' ('mounted '+$n)
  if((Text (P $click 'handlerAfter')) -cne $id.mountedChargeShell -or (Text (P $click 'handlerCaster')) -cne $riderId){ Fail ('mounted '+$n+': the ordinary Charge slot did not select the mounted Charge') }
 }
 $probe=P $cs 'chargeProbe'
 if($null -eq $probe -or (P $probe 'available') -ne $true -or (P $probe 'canTarget') -ne $true){ Fail 'the mounted rider''s ordinary Charge was not available and targetable on a clear lane' }
 $chargeTarget=P $cs 'chargeTarget'
 if($null -eq $chargeTarget -or (P $chargeTarget 'clicked') -ne $true -or (Text (P $chargeTarget 'handlerAbility')) -cne $id.mountedChargeShell){ Fail 'the hostile click did not take the mounted Charge' }
 # Reader correction after run 20261105-finalize-f2-r02: the click creates the one shell command; the pair transaction is
 # admitted when that shell delivers, a few frames later. Count admissions from before the selection to the terminal.
 $castDelta=Int (P $chargeTarget 'castDelta') 'cast delta'
 $shellCommands=@(@(P $chargeTarget 'commands') | Where-Object { (Text (P $_ 'blueprint')) -ceq $id.mountedChargeShell })
 $admittedBefore=Int (P (P $cs 'afterSelectionDrop') 'admitted') 'admitted before the charge'
 $admittedAfter=Int (P (P $c 'terminal') 'state' | ForEach-Object { P $_ 'admitted' }) 'admitted at the terminal'
 if((P $chargeTarget 'shell') -ne $true -or $castDelta -ne 1 -or $shellCommands.Count -ne 1 -or ($admittedAfter-$admittedBefore) -ne 1){ Fail 'the mounted Charge was not admitted exactly once' }
 $hostile=Text (P $chargeTarget 'shellTarget')
 if([string]::IsNullOrEmpty($hostile) -or $hostile -cne (Text (P $chargeTarget 'clickedId'))){ Fail 'the mounted Charge shell names another target' }
 $terminal=P $c 'terminal'
 Terminal $terminal 'mounted Charge'
 # The ordinary Charge's own refusal while the rider's Standard is spent: truthful, in the player's terms.
 $spent=P $c 'spentRefusal'
 if($null -eq $spent -or (P $spent 'available') -ne $false){ Fail 'the ordinary Charge stayed available with the rider''s Standard spent' }
 $spentReason=[string](Text (P $spent 'reason'))
 if(-not $spentReason.StartsWith('Charge ') -or $spentReason.Contains('Mounted Charge') -or $spentReason.Contains('KMC.')){ Fail ('the ordinary Charge refusal is not in the player''s terms: '+$spentReason) }
 if((Int (P $c 'chargeRules') 'mounted charge rules') -ne 1){ Fail 'the mounted Charge did not make exactly one charge attack' }
 $chargeRules=@(@(P $o 'attackRules') | Where-Object { (Text (P $_ 'attacker')) -ceq $riderId -and (Text (P $_ 'target')) -ceq $hostile -and (P $_ 'charge') -eq $true })
 if($chargeRules.Count -lt 1){ Fail 'no rider charge attack rule was observed' }
 if((Num (P (P $terminal 'state') 'riderStandard') 'rider Standard') -le 0){ Fail 'the rider''s Standard action was not spent' }
 if((Num (P (P $terminal 'state') 'mountStandard') 'mount Standard') -gt (Num (P (P (P $cs 'chargeClick') 'before') 'mountStandard') 'mount Standard before')+0.001){ Fail 'the mount paid the rider''s Charge' }
 if((Int (P $c 'stockChargeWatchedFrames') 'watched frames') -lt 1 -or (Int (P $c 'stockChargeCommandFrames') 'stock Charge frames') -ne 0){ Fail 'a stock Charge command reached the mounted rider' }

 # ---- F2-refusals ---------------------------------------------------------------------------------------------------
 $r=$rows['F2-refusals']
 $public=P $r 'publicCommand'
 if($null -eq $public -or (Int (P $public 'refusedDelta') 'public refusal delta') -ne 1 -or (P $public 'started') -ne $false -or (P $public 'inCommands') -ne $false){ Fail 'a direct public-control command was not refused at admission' }
 $pb=P $public 'before'; $pa=P $public 'after'
 Yes (P $pb 'riderInCombat') 'the refusal probes did not run in combat'
 foreach($field in 'riderMove','mountMove','riderStandard'){ if(-not (Same (Num (P $pa $field) $field) (Num (P $pb $field) $field))){ Fail ('the refused public-control command changed '+$field) } }
 if((Text (P $pa 'relationship')) -cne (Text (P $pb 'relationship')) -or (Int (P $pa 'generation') 'generation') -ne (Int (P $pb 'generation') 'generation')){ Fail 'the refused public-control command changed the relationship' }
 $stock=P $r 'stockCommand'
 $sb=P $stock 'before'; $sa=P $stock 'after'
 Relation $sb 'Mounted' 'stock Charge probe'; Relation $sa 'Mounted' 'stock Charge probe'
 if((Text (P $stock 'feedbackAfter')) -cne $id.expectedFeedback){ Fail 'the refused stock Charge did not give the cancellation feedback' }
 if((Int (P $stock 'warningDelta') 'warning delta') -lt 1 -or @(@(P $r 'warnings') | Where-Object { (Text (P $_ 'text')) -ceq $id.expectedFeedback }).Count -lt 1){ Fail 'the cancellation was not shown as a warning' }
 if((Int (P $stock 'admittedDelta') 'admitted delta') -ne 0 -or (P $stock 'started') -ne $false -or (P $stock 'inCommands') -ne $false){ Fail 'the mounted rider''s stock Charge command was not refused' }
 if(-not (Same (Num (P $sa 'riderStandard') 'rider Standard') (Num (P $sb 'riderStandard') 'rider Standard'))){ Fail 'the refused stock Charge changed the rider''s Standard action' }

 # ---- F2-charge-unmounted -------------------------------------------------------------------------------------------
 $u=$rows['F2-charge-unmounted']; $us=P $u 'steps'
 $dismount=P $us 'dismountClick'
 SlotClick $dismount $id.publicControl 'combat Dismount'
 Relation (P $dismount 'before') 'Mounted' 'combat Dismount'
 Yes (P (P $dismount 'before') 'riderInCombat') 'the public Dismount did not run in combat'
 if((P $dismount 'handlerIsShell') -ne $false -or (Text (P $dismount 'shellCommand')) -cne $id.dismountShell){ Fail 'the public control did not run Dismount at once in combat' }
 $dismounted=P $us 'dismounted'
 Relation (P $dismounted 'state') 'Unmounted' 'combat Dismount'
 if((Text (P $dismounted 'shellResult')) -cne 'Success'){ Fail 'the combat Dismount did not succeed' }
 if((Num (P $dismounted 'peakRiderMove') 'peak rider Move') -le 0){ Fail 'the combat Dismount did not cost the rider''s Move action' }
 $stockClick=P $us 'stockClick'
 SlotClick $stockClick $id.stockCharge 'unmounted Charge'
 if((Int (P $stockClick 'index') 'unmounted Charge index') -ne $chargeIndex){ Fail 'unmounted Charge: clicked another slot' }
 Relation (P $stockClick 'before') 'Unmounted' 'unmounted Charge'
 if((Text (P $stockClick 'handlerAfter')) -cne $id.stockCharge -or (P $stockClick 'handlerIsStock') -ne $true -or (P $stockClick 'handlerIsShell') -ne $false){ Fail 'the unmounted Charge slot did not select the stock Charge' }
 $stockTarget=P $us 'stockTarget'
 if($null -eq $stockTarget -or (P $stockTarget 'clicked') -ne $true -or (P $stockTarget 'commandFound') -ne $true -or (Text (P $stockTarget 'commandBlueprint')) -cne $id.stockCharge){ Fail 'the unmounted Charge did not run the native stock command' }
 if((Int (P $stockTarget 'admittedDelta') 'unmounted admitted delta') -ne 0 -or (Int (P (P $u 'stockResult') 'admittedDelta') 'unmounted admitted delta') -ne 0){ Fail 'the unmounted Charge was converted into the mounted Charge' }
 if($null -eq (P $u 'buffSeen')){ Fail 'the native charge buff was not observed on the unmounted rider' }
 $rule=P $u 'chargeRule'
 if($null -eq $rule -or (P $rule 'charge') -ne $true -or (Text (P $rule 'attacker')) -cne $riderId){ Fail 'the unmounted rider''s native charge attack was not observed' }
 if((Int (P (P $u 'stockResult') 'chargeRules') 'unmounted charge rules') -lt 1){ Fail 'the unmounted Charge made no charge attack' }

 # ---- screenshots with the game UI ----------------------------------------------------------------------------------
 $shots=@(P $o 'screenshots')
 if($shots.Count -ne 3){ Fail ('three screenshots were expected, recorded '+$shots.Count) }
 $root=if(-not [string]::IsNullOrEmpty($EvidenceRoot)){ $EvidenceRoot } else { Text (P $Request 'evidenceRoot') }
 $shotIndex=@{'public-control-unmounted'=@('Unmounted',$cycleIndex);'public-control-mounted'=@('Mounted',$cycleIndex);'charge-target-selection-mounted'=@('Mounted',-1)}
 foreach($name in Get-KmcFinalize2ShotNames){
  $shot=@($shots | Where-Object { (Text (P $_ 'name')) -ceq $name })
  if($shot.Count -ne 1){ Fail ('screenshot missing or duplicated: '+$name) }
  $shot=$shot[0]
  if($null -ne (P $shot 'error')){ Fail ('screenshot failed: '+$name+': '+(P $shot 'error')) }
  if((Text (P $shot 'file')) -cne ('movement-visuals/finalize-f2-'+$name+'.png') -or (Text (P $shot 'sha256')) -cnotmatch '^[0-9a-f]{64}$' -or (Int (P $shot 'bytes') ($name+' bytes')) -le 0){ Fail ('screenshot file record differs: '+$name) }
  $ui=P $shot 'ui'
  if((P $ui 'manager') -ne $true -or (P $ui 'selectedIsRider') -ne $true -or $null -ne (P $ui 'error')){ Fail ('the toolbar was not showing the rider: '+$name) }
  Relation (P $shot 'state') $shotIndex[$name][0] ('screenshot '+$name)
  if($shotIndex[$name][1] -ge 0){
   if((@(P (P $shot 'state') 'controlIndices') | ConvertTo-Json -Compress) -cne (@($cycleIndex) | ConvertTo-Json -Compress)){ Fail ('the public control is not in its slot in '+$name) }
   if(@(@(P $ui 'mainBar') | Where-Object { (P $_ 'active') -eq $true -and (Text (P $_ 'key')) -ceq $id.publicControl -and (P $_ 'settingsIndex') -eq $cycleIndex }).Count -ne 1){ Fail ('the toolbar did not show the public control in '+$name) }
  } elseif((Text (P (P $shot 'state') 'selectedAbility')) -cne $id.mountedChargeShell){ Fail 'the Charge target-selection frame did not hold the mounted Charge' }
  if(-not [string]::IsNullOrEmpty($root)){
   $path=Join-Path $root ((Text (P $shot 'file')) -replace '/','\')
   if(-not (Test-Path -LiteralPath $path -PathType Leaf)){ Fail ('screenshot file missing: '+$name) }
   if((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -cne (Text (P $shot 'sha256'))){ Fail ('screenshot file hash differs: '+$name) }
  }
 }
}
