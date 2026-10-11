# Synthetic raw observations for isolated reader tests; no game or external state.
function New-ChargeFixtureReturn([string]$Case='case',[bool]$Moved=$false){
 $before=[ordered]@{riderId='rider';mountId='mount';mountPosition=@{x=0.0;y=0.0;z=0.0};frame=100;gameTicks=1000000L;
  generation=1;relationship='Mounted';riderCommandsEmpty=$true;mountCommandsEmpty=$true;inCombat=$false;nativeTurnBased=$false;
  controllerInitialized=$false;chargeOwned=$false;commandActive=$false;mountMoving=$false;mountPathPresent=$false;mountCharging=$false;speedOverride=$null}
 $p=[ordered]@{contract='native-return-to-original-charge-fixture-origin';case=$Case;destination=@{x=0.0;y=0.0;z=0.0};moved=$Moved;
  before=$before;after=($before|ConvertTo-Json -Depth 12|ConvertFrom-Json)}
 if($Moved){
  $p.before.mountPosition.x=6.0;$p.after.frame=101;$p.after.gameTicks=2000000L
  $p.selectedIds=@('rider');$p.createdByPlayer=$true
  $p.admittedCommand=@{id=333;executor='mount'}
  $p.terminalCommand=@{id=333;executor='mount';type='Kingmaker.UnitLogic.Commands.UnitMoveTo';finished=$true;result='Success'}
  $p.resourceWindow=New-GroundResources 'mount'
  $p.resourceWindow.groundCommand|Add-Member -NotePropertyName createdByPlayer -NotePropertyValue $true
  $p.resourceWindow.groundCommand|Add-Member -NotePropertyName finished -NotePropertyValue $true
  $p.resourceWindow.groundCommand|Add-Member -NotePropertyName nativeResult -NotePropertyValue 'Success'
  $p.path=@{complete=$true;errors=@();actorId='mount';commandObject=333;events=@(
   @{sequence=1;actorId='mount';commandObject=333;frame=100;boundary='path-request'},
   @{sequence=2;actorId='mount';commandObject=333;frame=101;boundary='path-complete-after'},
   @{sequence=3;actorId='mount';commandObject=333;frame=101;boundary='command-ended'})}
 }
 $p|ConvertTo-Json -Depth 60|ConvertFrom-Json
}
