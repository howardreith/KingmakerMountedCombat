$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
$passed=0
function Check([bool]$condition,[string]$why){if(-not$condition){throw $why};$script:passed++;Write-Host ('PASS '+$why)}
function Copy-S3($v){$v|ConvertTo-Json -Depth 40 -Compress|ConvertFrom-Json}
$rider='b6628a77-4962-47a4-a17c-88d9836fc9d5';$mount='d79a4f6c-b74e-4868-95bd-533899131acb';$recipient='d17c8fd0-6627-40c4-bf9a-024cf001c1f7';$hostile='11111111-2222-3333-4444-555555555555'
$clw='5590652e1c2225c4ca30c4a699ab3649';$snowball='9f10909f0be1f5141bf1c102041f93d9'
function New-S3State([int]$recipientDamage,[bool]$heldTouch,[int]$clwAvailable,[string]$turnActor=$null){
 [pscustomobject]@{relationship='Mounted';riderCommandsEmpty=$true;mountCommandsEmpty=$true;processesSettled=$true;nativeAbilitiesPending=$false;projectilesPending=$false
  heldTouch=$(if($heldTouch){[pscustomobject]@{blueprint='touch';castedThisRound=$true}}else{$null});carriedLive=$false;turnActor=$turnActor;riderHasStandard=$true
  prepared=@(if($clwAvailable -gt 0){1..$clwAvailable|ForEach-Object{[pscustomobject]@{available=$true;ability=[pscustomobject]@{blueprint=$clw}}}});recipient=[pscustomobject]@{id=$recipient;damage=$recipientDamage};hostile=[pscustomobject]@{id=$hostile;damage=0}}
}
function New-S3Carried([string]$kind,[double]$distance){[pscustomobject]@{kind=$kind;rider=$rider;mount=$mount;moveStarts=1;replans=0;withoutProgress=0;satisfied=$true;nativeApproachEndSignalled=$false;trapSafePointApplied=($kind -ceq 'TrapInteraction');distanceAtSatisfaction=$distance;lineOfSightAtSatisfaction=$true;operationalSeconds=3.2;slotRestored=$true;terminalReason=$null;result='Success';serial=7}}
function New-S3Cast([string]$blueprint,[string]$target,[double]$distance,[string]$turnActor=$null){[pscustomobject]@{inputPath='ClickWithSelectedAbilityHandler.OnClick';clicked=$true;canTarget=$true;resolvedTarget=$target;distanceMountToTarget=$distance;ability=[pscustomobject]@{blueprint=$blueprint;caster=$rider};shell=[pscustomobject]@{executor=$rider;type='UnitUseAbility'};before=[pscustomobject]@{turnActor=$turnActor;riderHasStandard=$true}}}
function New-S3Common([string]$name,$before,$after,$carried){[pscustomobject]@{name=$name;status='PASS';evidence=[pscustomobject]@{case=$name;before=$before;after=$after;carriedSeen=$true;carriedReleasedSeen=$true;mountTravelledMetres=8.5;mountDisplacement=8.1;riderDisplacement=8.0;carriedOutcomes=@($carried);costEvents=@();rules=[pscustomobject]@{events=@()};trapEvents=[pscustomobject]@{events=@()};events=@();inputCount=1}}}
function New-S3TrapRow{
 $row=New-S3Common 'S3-trap-disarm' (New-S3State 0 $false 1) (New-S3State 0 $false 1) (New-S3Carried 'TrapInteraction' 0.01)
 $e=$row.evidence
 $e|Add-Member clicked $true;$e|Add-Member inputPath 'ClickMapObjectHandler.OnClick'
 $e|Add-Member admitted ([pscustomobject]@{type='UnitInteractWithObject';executor=$rider;interaction='DisableTrapInteractionComponent';approachRadius=0.3;needLoS=$true})
 $e|Add-Member trap ([pscustomobject]@{id='trap-1';distanceAtClick=40.2;disableDc=20});$e|Add-Member routeProbe ([pscustomobject]@{error=$false;lengthMetres=52.1})
 $e|Add-Member command ([pscustomobject]@{type='UnitInteractWithObject';acted=$true;result='Success'})
 $e|Add-Member thievery ([pscustomobject]@{baseBefore=0;baseGranted=4;granted=$true;mountBase=0})
 $e|Add-Member trapAfter ([pscustomobject]@{active=$false;distanceToRider=0.9})
 $e|Add-Member trapAtClick ([pscustomobject]@{active=$true;perceptionPassed=$true;revealed=$true;distance=9.8})
 $e|Add-Member explorationLegs @([pscustomobject]@{ordered=$true;destinationToTrap=6.1;perceptionBefore=$false;outcome='discovered';perceptionChecks=@([pscustomobject]@{actor=$rider;trap='trap-1';dc=15;d20=14;passed=$true})})
 $e.trapEvents=[pscustomobject]@{events=@([pscustomobject]@{kind='skill-check';actor=$rider;stat='SkillThievery';dc=20;d20=19;rollResult=25;passed=$true},[pscustomobject]@{kind='disarm-success';actor=$rider;trap='trap-1'})}
 $row
}
function New-S3FriendlyRow([bool]$held){
 $name=$(if($held){'S3-touch-held-charge'}else{'S3-touch-friendly-distant'})
 $row=New-S3Common $name (New-S3State 3 $false 1) (New-S3State 0 $false $(if($held){1}else{0})) (New-S3Carried 'Ability' 2.1)
 $e=$row.evidence
 $e|Add-Member recipientWounded ([pscustomobject]@{damage=3});$e|Add-Member instrument $(if($held){'scroll-stack'}else{'memorized-slot'})
 $e|Add-Member cast (New-S3Cast $clw $recipient $(if($held){6.2}else{12.3}))
 $events=@([pscustomobject]@{kind='cast-after';actor=$rider;spellFailed=$false},[pscustomobject]@{kind='cast-after';actor=$rider;spellFailed=$false},[pscustomobject]@{kind='heal';actor=$rider;target=$recipient;value=5})
 if($held){$events+=[pscustomobject]@{kind='item-spend-after';actor=$rider;result=$true};$e.inputCount=3
  $e|Add-Member stop ([pscustomobject]@{chargeHeldAfterStop=$true;settled=[pscustomobject]@{heldTouch=[pscustomobject]@{blueprint='touch'}}})
  $e|Add-Member redeliver ([pscustomobject]@{resolvedToHeldTouch=$true;clicked=$true})}
 $e.events=$events
 $row
}
function New-S3HostileRow{
 $row=New-S3Common 'S3-touch-hostile' (New-S3State 0 $false 0) (New-S3State 0 $false 0) (New-S3Carried 'Ability' 2.4)
 $e=$row.evidence;$e|Add-Member cast (New-S3Cast $clw $hostile 8.1)
 $e.events=@([pscustomobject]@{kind='cast-after';actor=$rider;spellFailed=$false},[pscustomobject]@{kind='cast-after';actor=$rider;spellFailed=$false},[pscustomobject]@{kind='item-spend-after';actor=$rider;result=$true},[pscustomobject]@{kind='heal';actor=$rider;target=$hostile;value=4})
 $e.rules=[pscustomobject]@{events=@([pscustomobject]@{kind='attack-roll';actor=$rider;target=$hostile;attackType='Touch';result='Hit'})}
 $row
}
function New-S3RangedRow{
 $row=New-S3Common 'S3-ranged-distant' (New-S3State 0 $false 0) (New-S3State 0 $false 0) (New-S3Carried 'Ability' 9.8)
 $e=$row.evidence;$e|Add-Member cast (New-S3Cast $snowball $hostile 18.4);$e|Add-Member hostileRetreat ([pscustomobject]@{distanceBefore=8.1;distanceAfter=18.4});$e|Add-Member rangeMetres 10.6
 $e.events=@([pscustomobject]@{kind='cast-after';actor=$rider;spellFailed=$false})
 $e.rules=[pscustomobject]@{events=@([pscustomobject]@{kind='attack-roll';actor=$rider;target=$hostile;attackType='RangedTouch';result='Miss'})}
 $row
}
function New-S3TbRow([bool]$insufficient){
 $name=$(if($insufficient){'S3-tb-insufficient-movement'}else{'S3-tb-touch-friendly'})
 $row=New-S3Common $name (New-S3State 3 $false 1 $rider) (New-S3State 0 $false $(if($insufficient){1}else{0}) $rider) (New-S3Carried 'Ability' 2.0)
 $e=$row.evidence
 $e|Add-Member recipientWounded ([pscustomobject]@{damage=3});$e|Add-Member instrument $(if($insufficient){'scroll-stack'}else{'memorized-slot'})
 $e|Add-Member cast (New-S3Cast $clw $recipient 9.1 $rider);$e|Add-Member rowTurnActor $rider
 $e.costEvents=@([pscustomobject]@{boundary='cost-after';state=[pscustomobject]@{actor=$rider}})
 $events=@([pscustomobject]@{kind='cast-after';actor=$rider;spellFailed=$false},[pscustomobject]@{kind='cast-after';actor=$rider;spellFailed=$false},[pscustomobject]@{kind='heal';actor=$rider;target=$recipient;value=6})
 if($insufficient){
  $events+=[pscustomobject]@{kind='item-spend-after';actor=$rider;result=$true}
  $e.carriedOutcomes=@((New-S3Carried 'Ability' 0),(New-S3Carried 'Ability' 2.0));$e.carriedOutcomes[0].nativeApproachEndSignalled=$true;$e.carriedOutcomes[0].satisfied=$false
  $e|Add-Member summary ([pscustomobject]@{nativeApproachEndSignalled=$true;secondInput=$true});$e|Add-Member secondTurn ([pscustomobject]@{heldCharge=$true})
  $e|Add-Member redeliver ([pscustomobject]@{resolvedToHeldTouch=$true;clicked=$true});$e|Add-Member recipientPlacement ([pscustomobject]@{distanceAfter=36.0});$e|Add-Member allowanceMetres 27.4
  $e|Add-Member firstTurnEnd ([pscustomobject]@{heldTouch=[pscustomobject]@{blueprint='touch'}});$e.inputCount=2
 }
 $e.events=$events
 $row
}
function New-S3Artifact([string]$scenario,$rows){
 [pscustomobject]@{schemaVersion=47;status='PASS';errors=@();rows=$rows;subscenarioPassCount=@($rows).Count;subscenarioFailCount=0
  observations=[pscustomobject]@{riderId=$rider;horseId=$mount;sprint3Approach=[pscustomobject]@{contract='native-mounted-carried-approach-v1';mode=$(if($scenario.EndsWith('-tb')){'TB'}else{'RT'});mounted=$true;rider=$rider;mount=$mount;mountBlueprint='e7aa96d15a45238438ae4cfb476f6bb9'
   cases=@(Get-KmcSprint3ApproachCases $scenario);riderLevel=4;recipient=[pscustomobject]@{id=$recipient};riderThieveryBaseBefore=0;riderThieveryBaseAfter=0
   castTrace=[pscustomobject]@{faults=0;dropped=0;identityRegistry=[pscustomobject]@{faults=0;released=$true}}
   final=[pscustomobject]@{riderCommandsEmpty=$true;mountCommandsEmpty=$true;processesSettled=$true;nativeAbilitiesPending=$false;projectilesPending=$false}
   items=@([pscustomobject]@{disposed=$true;slotRestored=$true;noOwnedItemResident=$true})}}}
}
$rt=New-S3Artifact 'sprint3-approach-rt' @((New-S3TrapRow),(New-S3FriendlyRow $false),(New-S3FriendlyRow $true),(New-S3HostileRow),(New-S3RangedRow))
$tb=New-S3Artifact 'sprint3-approach-tb' @((New-S3TbRow $true),(New-S3TbRow $false))
function Accept($artifact,[string]$scenario){Assert-KmcSprint3ApproachEvidence -Request ([pscustomobject]@{scenario=$scenario}) -Artifact $artifact -Status 'PASS'}
function Reject($artifact,[string]$scenario,[string]$why){$refused=$false;try{Accept $artifact $scenario}catch{$refused=$true};Check $refused $why}
Accept $rt 'sprint3-approach-rt';Check $true 'complete real-time Sprint 3 envelope accepted'
Accept $tb 'sprint3-approach-tb';Check $true 'complete turn-based Sprint 3 envelope accepted'
Check ((Test-KmcPhase3dSchemaRegistration -SchemaVersion 47 -Scenario 'sprint3-approach-rt') -and -not (Test-KmcPhase3dSchemaRegistration -SchemaVersion 44 -Scenario 'sprint3-approach-rt') -and -not (Test-KmcPhase3dSchemaRegistration -SchemaVersion 47 -Scenario 'chunk6c-casting-rt')) 'schema 47 binds exactly the Sprint 3 scenarios'
Check (((Get-KmcPhase3dEvidenceDispatch -SchemaVersion 47 -Scenario 'sprint3-approach-tb').validator) -ceq 'Assert-KmcSprint3ApproachEvidence') 'dispatch selects the Sprint 3 reader'
# Exercise the actual launcher's parameter binder without executing its body.
$launcher=Join-Path $PSScriptRoot 'runtime/Invoke-KingmakerRuntimeScenario.ps1'
$tokens=$null;$errors=$null;$launchAst=[Management.Automation.Language.Parser]::ParseFile($launcher,[ref]$tokens,[ref]$errors)
Check ($errors.Count-eq0) 'actual guarded launcher parses'
$bindOnly=[scriptblock]::Create($launchAst.ParamBlock.Extent.Text+"`n'bound-without-launch'")
foreach($scenario in @('sprint3-approach-rt','sprint3-approach-tb')){
 Check ((& $bindOnly -Scenario $scenario -RunId 's3-registration-probe')-ceq'bound-without-launch') ('actual native parameter guard admits '+$scenario)
 Check ((Get-KmcSaveBackedRuntimeScenarios)-ccontains$scenario) ('request registry agrees '+$scenario)
 Check ((Test-KmcCompoundRuntimeScenario $scenario)) ('compound registry agrees '+$scenario)
}
# Negatives: every refusal names the exact native fact it protects.
$r=Copy-S3 $rt;$r.rows[0].evidence.carriedSeen=$false;Reject $r 'sprint3-approach-rt' 'trap row without the product carrying the command refused'
$r=Copy-S3 $rt;$r.rows[0].evidence.trapEvents.events=@($r.rows[0].evidence.trapEvents.events[1]);Reject $r 'sprint3-approach-rt' 'trap row without the rider Trickery check refused'
$r=Copy-S3 $rt;$r.rows[0].evidence.thievery.mountBase=3;Reject $r 'sprint3-approach-rt' 'mount Trickery ranks refused'
$r=Copy-S3 $rt;$r.rows[0].evidence.trapEvents.events[0].passed=$false;Reject $r 'sprint3-approach-rt' 'disarm success after a failed native check refused'
$r=Copy-S3 $rt;$r.rows[0].evidence.trap.distanceAtClick=1.0;Reject $r 'sprint3-approach-rt' 'trap click already in range refused'
$r=Copy-S3 $rt;$r.rows[0].evidence.admitted.approachRadius=4.0;Reject $r 'sprint3-approach-rt' 'changed native trap approach radius refused'
$r=Copy-S3 $rt;$r.rows[1].evidence.events+=Copy-S3 $r.rows[1].evidence.events[2];Reject $r 'sprint3-approach-rt' 'double heal refused'
$r=Copy-S3 $rt;$r.rows[1].evidence.after.prepared=@();$r.rows[1].evidence.after.prepared=$r.rows[1].evidence.before.prepared;Reject $r 'sprint3-approach-rt' 'unspent memorized slot after a cast refused'
$r=Copy-S3 $rt;$r.rows[1].evidence.after.heldTouch=[pscustomobject]@{blueprint='touch'};Reject $r 'sprint3-approach-rt' 'held charge left after delivery refused'
$r=Copy-S3 $rt;$r.rows[1].evidence.cast.distanceMountToTarget=4.0;Reject $r 'sprint3-approach-rt' 'distant row with the recipient inside the cast radius refused'
$r=Copy-S3 $rt;$r.rows[2].evidence.stop.chargeHeldAfterStop=$false;Reject $r 'sprint3-approach-rt' 'Stop that lost the held charge refused'
$r=Copy-S3 $rt;$r.rows[2].evidence.redeliver.resolvedToHeldTouch=$false;Reject $r 'sprint3-approach-rt' 're-click that recast instead of delivering the held charge refused'
$r=Copy-S3 $rt;$r.rows[2].evidence.events+=[pscustomobject]@{kind='item-spend-after';actor=$rider;result=$true};Reject $r 'sprint3-approach-rt' 'second scroll charge for one held delivery refused'
$r=Copy-S3 $rt;$r.rows[3].evidence.rules.events[0].attackType='Melee';Reject $r 'sprint3-approach-rt' 'hostile delivery without a native touch attack roll refused'
$r=Copy-S3 $rt;$r.rows[3].evidence.rules.events[0].result='Miss';Reject $r 'sprint3-approach-rt' 'effect delivered on a native miss refused'
$r=Copy-S3 $rt;$r.rows[3].evidence.rules.events[0].result='Miss';$r.rows[3].evidence.events=@($r.rows[3].evidence.events|Where-Object kind -cne 'heal');$r.rows[3].evidence.after.heldTouch=[pscustomobject]@{blueprint='touch'};Accept $r 'sprint3-approach-rt';Check $true 'a legal native miss with the charge still held is accepted'
$r=Copy-S3 $rt;$r.rows[3].evidence.rules.events[0].result='Miss';$r.rows[3].evidence.events=@($r.rows[3].evidence.events|Where-Object kind -cne 'heal');Reject $r 'sprint3-approach-rt' 'a miss that lost the held charge refused'
$r=Copy-S3 $rt;$r.rows[0].evidence.trapAtClick.perceptionPassed=$false;Reject $r 'sprint3-approach-rt' 'trap click without native discovery refused'
$r=Copy-S3 $rt;$r.rows[0].evidence.explorationLegs[0].destinationToTrap=1.0;Reject $r 'sprint3-approach-rt' 'exploration leg ordered onto the trap refused'
$r=Copy-S3 $rt;$r.rows[0].evidence.explorationLegs[0].perceptionChecks=@();Reject $r 'sprint3-approach-rt' 'discovery without an observed native Perception roll refused'
$r=Copy-S3 $rt;$r.rows[4].evidence.carriedOutcomes[0].distanceAtSatisfaction=2.5;Reject $r 'sprint3-approach-rt' 'ranged spell forced into melee refused'
$r=Copy-S3 $rt;$r.rows[4].evidence.carriedOutcomes[0].distanceAtSatisfaction=12.0;Reject $r 'sprint3-approach-rt' 'ranged cast beyond its own range refused'
$r=Copy-S3 $rt;$r.rows[4].evidence.hostileRetreat.distanceAfter=6.0;Reject $r 'sprint3-approach-rt' 'ranged row with the hostile inside range refused'
$r=Copy-S3 $rt;$r.rows[0].evidence.carriedOutcomes[0].slotRestored=$false;Reject $r 'sprint3-approach-rt' 'mount Move slot not restored refused'
$r=Copy-S3 $rt;$r.rows[0].evidence.carriedOutcomes[0].terminalReason='exhausted';Reject $r 'sprint3-approach-rt' 'carried approach fault refused'
$r=Copy-S3 $rt;$r.observations.sprint3Approach.riderThieveryBaseAfter=4;Reject $r 'sprint3-approach-rt' 'unrestored rider Trickery fixture rank refused'
$r=Copy-S3 $rt;$r.observations.sprint3Approach.castTrace.faults=1;Reject $r 'sprint3-approach-rt' 'faulted casting trace refused'
$r=Copy-S3 $rt;$r.rows=@($r.rows[0],$r.rows[1],$r.rows[2],$r.rows[3]);$r.subscenarioPassCount=4;Reject $r 'sprint3-approach-rt' 'missing registered row refused'
$r=Copy-S3 $rt;$r.status='FAIL';$r.errors=@('x');$r.subscenarioFailCount=1;Reject $r 'sprint3-approach-rt' 'failed producer envelope cannot become PASS'
$r=Copy-S3 $tb;$r.rows[0].evidence.summary.nativeApproachEndSignalled=$false;Reject $r 'sprint3-approach-tb' 'insufficient-movement row without the native approach-end rule refused'
$r=Copy-S3 $tb;$r.rows[0].evidence.secondTurn.heldCharge=$false;Reject $r 'sprint3-approach-tb' 'charge lost across the turn boundary refused'
$r=Copy-S3 $tb;$r.rows[0].evidence.recipientPlacement.distanceAfter=10.0;Reject $r 'sprint3-approach-tb' 'insufficient-movement row with the recipient within one turn refused'
$r=Copy-S3 $tb;$r.rows[1].evidence.rowTurnActor=$mount;Reject $r 'sprint3-approach-tb' 'cast issued off the rider''s turn refused'
$r=Copy-S3 $tb;$r.rows[1].evidence.costEvents=@();Reject $r 'sprint3-approach-tb' 'turn-based row without the rider''s native Standard cost refused'
$r=Copy-S3 $tb;$r.rows[1].evidence.inputCount=2;Reject $r 'sprint3-approach-tb' 'turn-based friendly row needing two inputs refused'
Reject $rt 'sprint3-approach-tb' 'real-time envelope under the turn-based scenario refused'
Write-Host ('Sprint 3 reader checks passed: '+$passed)
