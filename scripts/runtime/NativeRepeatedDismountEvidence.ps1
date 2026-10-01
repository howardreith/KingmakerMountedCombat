Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'NativeTerminalBridgeEvidence.ps1')

function Assert-KmcRepeatedDismountInput($Evidence) {
 function Int($value) {
  if($value-isnot[int]-and$value-isnot[long]){throw 'Repeated Dismount integer missing'}
  [long]$value
 }
 function Number($value) {
  if($value-isnot[int]-and$value-isnot[long]-and$value-isnot[double]-and$value-isnot[single]-and$value-isnot[decimal]){throw 'Repeated Dismount number missing'}
  $number=[double]$value
  if([double]::IsNaN($number)-or[double]::IsInfinity($number)){throw 'Repeated Dismount nonfinite number'}
  $number
 }
 function Yes($value){$value-is[bool]-and$value}
 function No($value){$value-is[bool]-and-not$value}
 function Same($before,$after,[string]$label) {
  if($null-eq$before-or$null-eq$after-or
     ($before|ConvertTo-Json -Depth 100 -Compress)-cne($after|ConvertTo-Json -Depth 100 -Compress)){
   throw ('Repeated Dismount changed '+$label)
  }
 }
 function Same-State($before,$after) {
  $left=$before|ConvertTo-Json -Depth 100|ConvertFrom-Json
  $right=$after|ConvertTo-Json -Depth 100|ConvertFrom-Json
  foreach($state in @($left,$right)){
   if($null-ne$state.PSObject.Properties['geometry']){$state.geometry.PSObject.Properties.Remove('seconds')}
  }
  Same $left $right 'causal state'
 }
 function Same-Resources($before,$after,[string]$role) {
  if([string]$before.actor-cne[string]$after.actor-or(Int $before.actorObject)-ne(Int $after.actorObject)-or
     (Int $before.actorObject)-eq0-or-not(Yes $before.inCombat)-or-not(Yes $after.inCombat)){
   throw ('Repeated Dismount changed '+$role+' resource identity')
  }
  foreach($field in @('standard','move','swift','reactionCooldown','initiativeCooldown','reactions','reactionsPerRound','initiativeOrder','grantSequence')){
   if([Math]::Abs((Number $before.$field)-(Number $after.$field))-gt0.0001){throw ('Repeated Dismount changed '+$role+' '+$field)}
  }
 }
 function Boundary($boundary,[string]$rider,[string]$mount,[long]$generation,[string]$state) {
  if(-not(No $boundary.turnBased)-or-not(Yes $boundary.inCombat)-or-not(Yes $boundary.pairIdle)-or-not(Yes $boundary.traceComplete)){
   throw 'Repeated Dismount boundary is not complete idle RT combat'
  }
  if([string]$boundary.riderId-cne$rider-or[string]$boundary.mountId-cne$mount-or-not(Yes $boundary.exactSingleRider)-or
     $boundary.selectedIds-isnot[array]-or$boundary.selectedIds.Count-ne1-or[string]$boundary.selectedIds[0]-cne$rider){
   throw 'Repeated Dismount pair or exact selection differs'
  }
  if(-not(Yes $boundary.riderCommandsEmpty)-or-not(Yes $boundary.mountCommandsEmpty)-or(Int $boundary.selectedAbilityObject)-ne0){
   throw 'Repeated Dismount boundary retained a command or selected ability'
  }
  if([string]$boundary.state.relationshipState-cne$state-or(Int $boundary.state.generation)-ne$generation-or
     [string]$boundary.state.rider.actor-cne$rider-or[string]$boundary.state.mount.actor-cne$mount){
   throw 'Repeated Dismount relationship identity differs'
  }
  Same-Resources $boundary.riderResources $boundary.riderResources 'rider'
  Same-Resources $boundary.mountResources $boundary.mountResources 'mount'
  foreach($role in @('rider','mount')){
   $native=$boundary.($role+'Resources');$causal=$boundary.state.$role
   if([string]$native.actor-cne[string]$causal.actor-or(Int $native.grantSequence)-ne(Int $causal.nativePrepareCount)){
    throw ('Repeated Dismount changed '+$role+' native/causal allocation binding')
   }
   foreach($field in @('standard','move','swift','reactionCooldown','initiativeCooldown','reactions','reactionsPerRound','initiativeOrder')){
    if([Math]::Abs((Number $native.$field)-(Number $causal.$field))-gt0.0001){throw ('Repeated Dismount changed '+$role+' native/causal '+$field)}
   }
  }
  if($state-ceq'Mounted'){
   if(-not(Yes $boundary.abilityPresent)-or(Int $boundary.abilityObject)-eq0-or(Int $boundary.abilityDataObject)-eq0-or
      -not(Yes $boundary.availabilityVisible)-or-not(Yes $boundary.availabilityEnabled)){throw 'Repeated Dismount mounted surface unavailable'}
  } else {
   if(-not(No $boundary.abilityPresent)-or-not(No $boundary.availabilityVisible)-or-not(No $boundary.availabilityEnabled)-or
      [string]$boundary.availabilityReason-cne'Dismount is available only to the exact mounted rider.'){
    throw 'Repeated Dismount post-terminal surface differs'
   }
  }
 }
 function Same-Window($window,[string]$rider,[string]$mount,[bool]$selection) {
  $before=$window.before;$after=$window.after
  if($null-eq$before-or$null-eq$after){throw 'Repeated Dismount synchronous window boundaries missing'}
  foreach($field in @('frame','gameTicks','allocationSequence','shellCount','processBindings')){
   if((Int $before.$field)-ne(Int $after.$field)){throw ('Repeated Dismount synchronous window changed '+$field)}
  }
  Same $before.riderPosition $after.riderPosition 'rider position'
  Same $before.mountPosition $after.mountPosition 'mount position'
  Same $before.state.ledger $after.state.ledger 'transition ledger'
  Same-State $before.state $after.state
  Same-Resources $before.riderResources $after.riderResources 'rider'
  Same-Resources $before.mountResources $after.mountResources 'mount'
  foreach($field in @('nativeCastRequest','nativeRefusal','dispatchAccepted','dispatchRejected')){
   if((Int $before.controls.$field)-ne(Int $after.controls.$field)){throw ('Repeated Dismount control count changed '+$field)}
  }
  $expected=if($selection){1L}else{0L}
  if((Int $after.controls.targetSelectionStart)-(Int $before.controls.targetSelectionStart)-ne$expected-or
     (Int $after.controls.targetSelectionEnd)-(Int $before.controls.targetSelectionEnd)-ne$expected){
   throw 'Repeated Dismount target-selection callback count differs'
  }
  Passive-Selection-Records $window $before $after $rider $mount $selection
 }
 # Evidence arrives either as parsed JSON objects or as the offline fixture's dictionaries.
 function Field($object,[string]$name) {
  if($object-is[Collections.IDictionary]){
   if(-not$object.Contains($name)){return [pscustomobject]@{present=$false;value=$null}}
   return [pscustomobject]@{present=$true;value=$object[$name]}
  }
  $property=$object.PSObject.Properties[$name]
  if($null-eq$property){return [pscustomobject]@{present=$false;value=$null}}
  [pscustomobject]@{present=$true;value=$property.Value}
 }
 function Null-Field($record,[string]$name) {
  $field=Field $record $name
  if(-not$field.present-or$null-ne$field.value){throw ('Repeated Dismount activation record '+$name+' is not exactly null')}
 }
 # The native control service appends one activation ledger record per observed
 # control phase, so a SetAbility/DropAbility cancellation lawfully appends exactly
 # TargetSelectionStarted and TargetSelectionEnded for one Dismount activation and a
 # refused post-terminal input appends none. The window proves passivity by exposing
 # those records; a flat count that contradicts the required selection callbacks is
 # not evidence (preview.144 c6a-repeated-dismount144-j-unmounted-controls).
 function Passive-Selection-Records($window,$before,$after,[string]$rider,[string]$mount,[bool]$selection) {
  $field=Field $window 'activationRecords'
  if(-not$field.present-or$field.value-isnot[array]){throw 'Repeated Dismount activation records missing'}
  $records=@($field.value)
  $expected=if($selection){2}else{0}
  if($records.Count-ne$expected){throw 'Repeated Dismount activation record count differs'}
  if((Int $after.controls.activationCount)-(Int $before.controls.activationCount)-ne$records.Count){throw 'Repeated Dismount activation count differs from the appended records'}
  if(-not$selection){return}
  $started=$records[0];$ended=$records[1]
  if([string]$started.phase-cne'TargetSelectionStarted'-or[string]$ended.phase-cne'TargetSelectionEnded'){throw 'Repeated Dismount activation phases are not one passive target selection'}
  if([string]$started.terminalResult-cne'target-selection-started'-or[string]$ended.terminalResult-cne'target-selection-cancelled'){throw 'Repeated Dismount activation terminal results are not one cancelled target selection'}
  if((Int $started.activationId)-le0-or(Int $ended.activationId)-ne(Int $started.activationId)-or(Int $ended.sequence)-ne((Int $started.sequence)+1)){throw 'Repeated Dismount activation records are not one consecutive activation'}
  foreach($record in @($started,$ended)){
   if([string]$record.kind-cne'Dismount'-or[string]$record.abilityGuid-cne'3af2b81f4d72bbb30501fa730fcdf36e'-or[string]$record.casterId-cne$rider-or
      [string]$record.activeSelectedUnitIds-cne$rider-or[string]$record.targetId-cne'<none>'-or-not(Yes $record.targetSelectionMode)){throw 'Repeated Dismount activation record identity differs'}
   if((Int $record.frame)-ne(Int $before.frame)){throw 'Repeated Dismount activation record left the synchronous frame'}
   if([string]$record.relationshipStateAtStart-cne'Mounted'-or[string]$record.relationshipStateObserved-cne'Mounted'-or
      [string]$record.riderIdAtStart-cne$rider-or[string]$record.mountIdAtStart-cne$mount-or
      -not(No $record.riderViewChanged)-or-not(No $record.mountViewChanged)-or-not(No $record.relationshipEnded)-or-not(No $record.relationshipTransitionChanged)){throw 'Repeated Dismount activation record observed a relationship or view change'}
   if(-not(Yes $record.inCombat)-or-not(No $record.turnBased)){throw 'Repeated Dismount activation record is not RT combat'}
   Null-Field $record 'dispatchAccepted';Null-Field $record 'cleanupTrigger'
   if([string]$record.lifecycleDeliveries-cne'<none>'-or(Int $record.lifecycleSequenceObserved)-ne(Int $record.lifecycleSequenceAtStart)){throw 'Repeated Dismount activation record observed a dispatch, cleanup or lifecycle delivery'}
  }
 }
 if([string]$Evidence.contract-cne'cancel-once-one-native-dismount-then-repeat-refused'-or
    [string]$Evidence.scenario-cne'unmounted-attack-controls-rt'-or
    [string]$Evidence.abilityGuid-cne'3af2b81f4d72bbb30501fa730fcdf36e'){
  throw 'Repeated Dismount case identity differs'
 }
 $rider=[string]$Evidence.riderId
 $mount=[string]$Evidence.mountId
 if([string]::IsNullOrWhiteSpace($rider)-or[string]::IsNullOrWhiteSpace($mount)-or$rider-ceq$mount){throw 'Repeated Dismount pair missing'}
 $cancel=$Evidence.cancellation
 if(-not(Yes $cancel.setAbilityInvoked)-or-not(No $cancel.onClickInvoked)-or-not(Yes $cancel.dropAbilityInvoked)-or
    $cancel.allocationEvents-isnot[array]-or$cancel.allocationEvents.Count-ne0){throw 'Repeated Dismount cancellation created a click or allocation event'}
 $generation=Int $cancel.before.state.generation
 Boundary $cancel.before $rider $mount $generation 'Mounted'
 Boundary $cancel.after $rider $mount $generation 'Mounted'
 Same-Window $cancel $rider $mount $true
 if((Int $cancel.selected.handlerObject)-eq0-or
    (Int $cancel.selected.abilityDataObject)-ne(Int $cancel.before.abilityDataObject)-or
    (Int $cancel.selected.selectedAbilityObject)-ne(Int $cancel.before.abilityDataObject)-or
    [string]$cancel.selected.abilityGuid-cne[string]$Evidence.abilityGuid-or
    [string]$cancel.selected.casterId-cne$rider){throw 'Repeated Dismount cancellation selected a different ability'}
 $proof=$Evidence.positiveProof
 Assert-KmcRelationshipCommandProof $proof $true $false 0 $false 0 $true
 if([string]$proof.identity.abilityGuid-cne[string]$Evidence.abilityGuid-or
    [string]$proof.identity.casterId-cne$rider-or[string]$proof.identity.targetId-cne$rider-or
    [string]$proof.mountId-cne$mount-or(Int $proof.identity.generationAtInit)-ne$generation){
  throw 'Repeated Dismount positive identity differs'
 }
 foreach($field in @('admittedMount','acceptedMount','admittedDismount','acceptedDismount','refusedVoluntary','forcedDetach','duplicateSuppressed','concurrentSuppressed')){
  $expected=if($field-cin@('admittedDismount','acceptedDismount')){1L}else{0L}
  if((Int $proof.ledgerDelta.$field)-ne$expected){throw ('Repeated Dismount positive ledger delta differs '+$field)}
 }
 if(-not(Yes $Evidence.positiveClicked)-or-not(Yes $Evidence.positiveInput.clicked)-or
    [string]$Evidence.positiveInput.abilityGuid-cne[string]$Evidence.abilityGuid-or
    [string]$Evidence.positiveInput.clickedTargetId-cne$rider-or[string]$Evidence.positiveInput.resolvedTargetId-cne$rider){
  throw 'Repeated Dismount positive selected-ability input differs'
 }
 foreach($field in @('frame','gameTicks','allocationSequence')){
  if((Int $cancel.after.$field)-ne(Int $proof.preClick.$field)){throw ('Repeated Dismount pre-click gap '+$field)}
 }
 Same-State $cancel.after.state $proof.preClick.state
 Boundary $Evidence.afterPositive $rider $mount $generation 'Unmounted'
 Assert-KmcNativeTerminalBridge $proof $Evidence.afterPositive $Evidence.terminalBridge $false 'Unmounted'
 $repeat=$Evidence.repeat
 if(-not(No $repeat.clicked)-or-not(No $repeat.input.clicked)-or-not(No $repeat.input.abilityPresent)-or
    -not(Yes $repeat.input.handlerPresent)-or-not(Yes $repeat.input.targetViewPresent)-or
    $repeat.allocationEvents-isnot[array]-or$repeat.allocationEvents.Count-ne0){
  throw 'Repeated Dismount post-terminal input was not refused before command or allocation creation'
 }
 Boundary $repeat.before $rider $mount $generation 'Unmounted'
 Boundary $repeat.after $rider $mount $generation 'Unmounted'
 Same $Evidence.afterPositive $repeat.before 'positive terminal to repeat baseline'
 Same-Window $repeat $rider $mount $false
 if([string]$repeat.inputBaseline.availabilityReason-cne'Dismount is available only to the exact mounted rider.'-or
    $null-ne$repeat.inputBaseline.abilityAvailableForCast){
  throw 'Repeated Dismount unavailable baseline differs'
 }
}
