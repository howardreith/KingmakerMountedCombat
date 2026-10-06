Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'NativePassiveResourceEvidence.ps1')

function Assert-KmcChargeFixtureReturn($P,$Origin,[string]$Case) {
 function Fail([string]$Message){throw ('Charge fixture return: '+$Message)}
 function Distance($A,$B){
  foreach($point in @($A,$B)){
   foreach($axis in @('x','y','z')){
    if($null-eq$point-or$null-eq$point.$axis-or$point.$axis-is[string]-or$point.$axis-is[bool]){Fail 'position missing or nonnumeric'}
    $v=[double]$point.$axis;if([double]::IsNaN($v)-or[double]::IsInfinity($v)){Fail 'nonfinite position'}
   }
  }
  [Math]::Sqrt([Math]::Pow([double]$A.x-[double]$B.x,2)+[Math]::Pow([double]$A.z-[double]$B.z,2))
 }
 if($null-eq$P-or$P.contract-cne'native-return-to-original-charge-fixture-origin'-or$P.case-cne$Case-or
    $P.moved-isnot[bool]-or(Distance $P.destination $Origin)-gt0.0001-or[double]$P.destination.y-ne[double]$Origin.y){Fail 'case or original destination differs'}
 $b=$P.before;$a=$P.after
 foreach($state in @($b,$a)){
  if([string]::IsNullOrEmpty($state.riderId)-or[string]::IsNullOrEmpty($state.mountId)-or$state.riderId-ceq$state.mountId-or
     $state.relationship-cne'Mounted'-or$state.riderCommandsEmpty-ne$true-or$state.mountCommandsEmpty-ne$true){Fail 'pair not settled and mounted'}
  foreach($field in @('inCombat','nativeTurnBased','controllerInitialized','chargeOwned','commandActive','mountMoving','mountPathPresent','mountCharging')){
   if($state.$field-isnot[bool]-or$state.$field-ne$false){Fail ('unsettled native state '+$field)}
  }
  if($null-ne$state.speedOverride){Fail 'charge speed remains owned'}
 }
 if($b.riderId-cne$a.riderId-or$b.mountId-cne$a.mountId-or$b.generation-ne$a.generation-or
    $a.frame-lt$b.frame-or$a.gameTicks-lt$b.gameTicks-or(Distance $a.mountPosition $Origin)-gt0.06){Fail 'identity, generation, clock or arrival differs'}
 $distance=Distance $b.mountPosition $Origin
 if(-not$P.moved){
  if($distance-gt0.06-or(Distance $b.mountPosition $a.mountPosition)-gt0.0001-or$a.frame-ne$b.frame-or$a.gameTicks-ne$b.gameTicks){Fail 'no-input row moved or did not already occupy origin'}
  foreach($field in @('admittedCommand','terminalCommand','path','resourceWindow')){if($null-ne$P.PSObject.Properties[$field]){Fail 'no-input row carries a command'}}
  return
 }
 if($distance-le0.06-or@($P.selectedIds).Count-ne1-or$P.selectedIds[0]-cne$b.riderId-or$P.createdByPlayer-ne$true){Fail 'native return input or selection differs'}
 $command=$P.terminalCommand
 if($command.id-isnot[int]-or$command.id-eq0-or$command.executor-cne$b.mountId-or
    $command.type-cne'Kingmaker.UnitLogic.Commands.UnitMoveTo'-or$command.finished-ne$true-or$command.result-cne'Success'-or
    $P.admittedCommand.id-ne$command.id-or$P.admittedCommand.executor-cne$command.executor){Fail 'exact native mount command did not complete'}
 $window=$P.resourceWindow
 if($window.riderId-cne$b.riderId-or$window.mountId-cne$b.mountId-or$window.groundCommand.commandObject-ne$command.id-or
    $window.groundCommand.casterId-cne$b.mountId-or$window.groundCommand.createdByPlayer-ne$true-or
    $window.groundCommand.finished-ne$true-or$window.groundCommand.nativeResult-cne'Success'-or
    $window.before.frame-ne$b.frame-or$window.after.frame-ne$a.frame-or
    $window.before.gameTicks-ne$b.gameTicks-or$window.after.gameTicks-ne$a.gameTicks){Fail 'native resource observation is not this exact input window'}
 Assert-KmcNativeGroundResources $window
 $path=$P.path
 if($path.complete-ne$true-or@($path.errors).Count-ne0-or$path.actorId-cne$b.mountId-or$path.commandObject-ne$command.id){Fail 'native path trace incomplete or foreign'}
 $previous=0
 foreach($event in @($path.events)){
  $previous++
  if($event.sequence-ne$previous-or$event.actorId-cne$b.mountId-or$event.commandObject-ne$command.id-or
     $event.frame-lt$b.frame-or$event.frame-gt$a.frame){Fail 'path event identity/window differs'}
 }
 foreach($boundary in @('path-request','path-complete-after','command-ended')){
  if(@($path.events|Where-Object boundary -CEQ $boundary).Count-lt1){Fail ('missing native '+$boundary)}
 }
}
