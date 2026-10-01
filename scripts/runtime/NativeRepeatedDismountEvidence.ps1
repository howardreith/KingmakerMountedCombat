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
 function Same-Window($before,$after,[bool]$selection) {
  foreach($field in @('frame','gameTicks','allocationSequence','shellCount','processBindings')){
   if((Int $before.$field)-ne(Int $after.$field)){throw ('Repeated Dismount synchronous window changed '+$field)}
  }
  Same $before.riderPosition $after.riderPosition 'rider position'
  Same $before.mountPosition $after.mountPosition 'mount position'
  Same $before.state.ledger $after.state.ledger 'transition ledger'
  Same-State $before.state $after.state
  Same-Resources $before.riderResources $after.riderResources 'rider'
  Same-Resources $before.mountResources $after.mountResources 'mount'
  foreach($field in @('nativeCastRequest','nativeRefusal','dispatchAccepted','dispatchRejected','activationCount')){
   if((Int $before.controls.$field)-ne(Int $after.controls.$field)){throw ('Repeated Dismount control count changed '+$field)}
  }
  $expected=if($selection){1L}else{0L}
  if((Int $after.controls.targetSelectionStart)-(Int $before.controls.targetSelectionStart)-ne$expected-or
     (Int $after.controls.targetSelectionEnd)-(Int $before.controls.targetSelectionEnd)-ne$expected){
   throw 'Repeated Dismount target-selection callback count differs'
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
 Same-Window $cancel.before $cancel.after $true
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
 Same-Window $repeat.before $repeat.after $false
 if([string]$repeat.inputBaseline.availabilityReason-cne'Dismount is available only to the exact mounted rider.'-or
    $null-ne$repeat.inputBaseline.abilityAvailableForCast){
  throw 'Repeated Dismount unavailable baseline differs'
 }
}
