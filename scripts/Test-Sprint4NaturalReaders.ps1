$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'runtime/PersistenceProfileAreasIsolation.ps1')
$passed=0
function Check([bool]$condition,[string]$why){if(-not$condition){throw $why};$script:passed++;Write-Host ('PASS '+$why)}
function Copy-S4($v){$v|ConvertTo-Json -Depth 40 -Compress|ConvertFrom-Json}
$rider='rider-1';$mount='mount-1';$low='low-1';$next='next-1';$third='third-1'
$bow='bow-guid'
function New-S4Controller($over){$c=[ordered]@{hasIntent=$false;intentAutomatic=$false;stopSuppressed=$false;nativeRequests=5;intentStarts=1;riderDispatches=1;mountDispatches=0;duplicateDispatches=0;proposals=10;admitted=2;continued=1;refused=3;automaticDispatches=0;retired=0;lastRefusal='none';lastRetirement='none';lastObservation='x'};foreach($k in $over.Keys){$c[$k]=$over[$k]};[pscustomobject]$c}
function New-S4State($over=@{},$targets=@()){[pscustomobject]@{controller=(New-S4Controller $over);riderAiEnabled=$true;riderHold=$false;targets=$targets}}
function New-S4Attack([string]$actor,[string]$target,[int]$frame,[int]$identity){[pscustomobject]@{kind='attack-before';actor=$actor;target=$target;frame=$frame;identity=$identity;targetDamage=0}}
function New-S4Hit([string]$target,[int]$frame){[pscustomobject]@{kind='attack-roll';actor=$rider;target=$target;frame=$frame;identity=$frame+7000;result='Hit'}}
function New-S4Resolved([string]$target,[int]$frame){[pscustomobject]@{kind='weapon-resolved';actor=$rider;target=$target;frame=$frame;identity=$frame+8000;targetDamage=3}}
function New-S4Command([string]$actor,[string]$target,[int]$frame,[string]$action='RiderRanged',[int]$planned=3,[bool]$full=$true){[pscustomobject]@{actor=$actor;target=$target;frame=$frame;endFrame=$frame+30;action=$action;automaticIntent=$true;createdByPlayer=$false;aiAction=$false;riderStandardCooldown=0;mountStandardCooldown=0;plannedAttacks=$planned;fullAttack=$full}}
function New-S4Row([string]$name,$before,$after,$steps,$inputs,$rules,$commands,$extra=@{}){
 $evidence=[ordered]@{case=$name;before=$before;after=$after;steps=$steps;inputs=$inputs;rules=[pscustomobject]@{events=$rules;dropped=0};pairCommands=$commands;costEvents=@([pscustomobject]@{kind='cost'})}
 foreach($k in $extra.Keys){$evidence[$k]=$extra[$k]}
 [pscustomobject]@{name=$name;status='PASS';evidence=[pscustomobject]$evidence}
}
function New-S4UiRow{
 $slots=@([pscustomobject]@{index=3;type='MechanicActionBarSlotAbility';blueprint='native-ability'})
 $stale=@($slots)+@([pscustomobject]@{index=7;type='MechanicActionBarSlotAbility';blueprint='27364df661b3c121eabb97a31aa73a83'})
 $riderSurface=[pscustomobject]@{unit=$rider;listed=@('3af2b81f4d72bbb30501fa730fcdf36e','native-ability');retiredFacts=0;dismountFacts=1;mountFacts=0;slots=$slots;autoUse=$null}
 $mountSurface=[pscustomobject]@{unit=$mount;listed=@('mount-native');retiredFacts=0;dismountFacts=0;mountFacts=0;slots=@();autoUse=$null}
 New-S4Row 'S4-ui-clean' (New-S4State) (New-S4State) ([pscustomobject]@{
  surface=[pscustomobject]@{rider=$riderSurface;mount=$mountSurface;blueprintsResolve=[pscustomobject]@{riderPrimary=$true;mountPrimary=$true};slotsClearedBefore=0;autoUseClearedBefore=0;unrelatedSlotsBefore=$slots;staleIndex=7;staleSlotsAtInjection=$stale}
  reconciled=[pscustomobject]@{rider=$riderSurface;mount=$mountSurface;slotsCleared=1;autoUseCleared=1;staleIndex=7;staleInjected=$true}}) @() @() @()
}
function New-S4RetargetSteps{[pscustomobject]@{firstTargetDead=[pscustomobject]@{frame=150;state=(New-S4State @{} @([pscustomobject]@{id=$low;dead=$true},[pscustomobject]@{id=$next;dead=$false}))}}}
function New-S4RangedRow{
 $rules=@((New-S4Attack $rider $low 100 1),(New-S4Hit $low 100),(New-S4Resolved $low 101),(New-S4Attack $rider $next 200 2),(New-S4Attack $rider $next 205 3),(New-S4Attack $rider $next 210 4),(New-S4Attack $rider $next 400 5),(New-S4Attack $rider $next 405 6),(New-S4Attack $rider $next 410 7))
 $commands=@((New-S4Command $rider $next 195),(New-S4Command $rider $next 395))
 $commands[1].PSObject.Properties.Remove('endFrame')
 New-S4Row 'S4-ranged-retarget' (New-S4State) (New-S4State @{nativeRequests=6;admitted=3;automaticDispatches=2}) (New-S4RetargetSteps) @([pscustomobject]@{kind='hostile-click';name='first';target=$low;clicked=$true;inputPath='ClickUnitHandler.OnClick'}) $rules $commands @{firstTarget=$low;secondTarget=$next;mountDisplacement=0.1;nativePlan=[pscustomobject]@{count=3;ranged=3;enoughClose=$true}}
}
function New-S4MeleeRow{
 $rules=@((New-S4Attack $rider $low 100 1),(New-S4Hit $low 100),(New-S4Resolved $low 101),(New-S4Attack $rider $next 200 2),(New-S4Attack $mount $next 260 3))
 New-S4Row 'S4-melee-retarget' (New-S4State) (New-S4State @{nativeRequests=6;admitted=3;automaticDispatches=2}) (New-S4RetargetSteps) @([pscustomobject]@{kind='hostile-click';name='first';target=$low;clicked=$true;inputPath='ClickUnitHandler.OnClick'}) $rules @((New-S4Command $rider $next 195 'RiderMelee' 1 $false),(New-S4Command $mount $next 255 'MountPrimaryNatural' 1 $false)) @{firstTarget=$low;secondTarget=$next;riderWeapon=[pscustomobject]@{ranged=$false};nativePlan=[pscustomobject]@{count=1}}
}
function New-S4ControlsRow{
 $steps=[pscustomobject]@{
  afterStop=[pscustomobject]@{state=(New-S4State @{stopSuppressed=$true;lastRefusal='stop-suppressed;target=next-1'});dispatchesDuring=0;refusedDuring=8}
  afterReEnable=[pscustomobject]@{state=(New-S4State @{intentAutomatic=$true;hasIntent=$true})}
  manualAttacked=[pscustomobject]@{intentOwnedByPlayer=$true}
  manualHeld=[pscustomobject]@{attacksOnOldTargetSinceManual=0;manualTargetStillOwned=$true}
  paused=[pscustomobject]@{pausedObserved=$true;proposalsDuring=0;dispatchesDuring=0}
  beforeCast=[pscustomobject]@{state=(New-S4State @{intentAutomatic=$true;hasIntent=$true});recipientDamage=3}
  castDone=[pscustomobject]@{state=(New-S4State @{lastRetirement='newer player operation'});recipientDamage=0;dispatchesDuringCast=0}
  resumedAfterCast=[pscustomobject]@{state=(New-S4State @{intentAutomatic=$true})}
  heldInBowReach=[pscustomobject]@{dispatchesDuring=1;holdKept=$true;mountDisplacement=0;state=(New-S4State @{intentAutomatic=$true})}
  aiOffRetired=[pscustomobject]@{retiredDuring=1;dispatchesDuring=0;state=(New-S4State @{lastRetirement='ai off'})}}
 New-S4Row 'S4-controls' (New-S4State) (New-S4State) $steps @([pscustomobject]@{kind='stop'},[pscustomobject]@{kind='stop'},[pscustomobject]@{kind='cast'},[pscustomobject]@{kind='hostile-click';target=$third;clicked=$true}) @() @() @{carriedSeen=$true}
}
function New-S4HoldRow{
 $steps=[pscustomobject]@{
  afterMoveArrival=[pscustomobject]@{distanceToTarget=16}
  heldAfterMove=[pscustomobject]@{mountDisplacement=0;dispatchesDuring=0;refusedDuring=4;state=(New-S4State @{lastRefusal='hold-out-of-reach;target=next-1'})}
  attackAfterMove=[pscustomobject]@{frame=1}
  afterHold=[pscustomobject]@{state=[pscustomobject]@{riderHold=$true}}
  heldInReach=[pscustomobject]@{mountDisplacement=0;holdKept=$true}}
 New-S4Row 'S4-hold-no-chase' (New-S4State) (New-S4State) $steps @() @() @()
}
function New-S4TbRow{
 $steps=[pscustomobject]@{settled=[pscustomobject]@{sameTurn=$true};noAutomaticAfterRider=[pscustomobject]@{frame=2}}
 New-S4Row 'S4-tb-ordinary-actors' (New-S4State) (New-S4State) $steps @([pscustomobject]@{kind='hostile-click';target=$next;clicked=$true},[pscustomobject]@{kind='hostile-click';target=$next;clicked=$true}) @((New-S4Attack $rider $next 10 1),(New-S4Attack $mount $next 20 2)) @() @{riderBefore=[pscustomobject]@{standard=0};riderAfter=[pscustomobject]@{standard=6};mountBefore=[pscustomobject]@{standard=0};mountAfterRider=[pscustomobject]@{standard=0};mountAfter=[pscustomobject]@{standard=6}}
}
function New-S4Artifact([string]$scenario,$rows){
 [pscustomobject]@{schemaVersion=48;status='PASS';errors=@();rows=$rows;subscenarioPassCount=@($rows).Count;subscenarioFailCount=0
  observations=[pscustomobject]@{riderId=$rider;horseId=$mount;sprint4Natural=[pscustomobject]@{contract='native-natural-attacks-retargeting-v1';mode=$(if($scenario.EndsWith('-tb')){'TB'}else{'RT'});mounted=$true;rider=$rider;mount=$mount;retiredHelpersSelected=$false;cases=@(Get-KmcSprint4NaturalCases $scenario);riderAiRestored=$true
   castTrace=[pscustomobject]@{faults=0;dropped=0;identityRegistry=[pscustomobject]@{faults=0;released=$true}}
   final=[pscustomobject]@{riderCommandsEmpty=$true;mountCommandsEmpty=$true;processesSettled=$true;nativeAbilitiesPending=$false;projectilesPending=$false}
   items=@([pscustomobject]@{disposed=$true;slotRestored=$true;noOwnedItemResident=$true})}}}
}
$rt=New-S4Artifact 'sprint4-natural-rt' @((New-S4UiRow),(New-S4RangedRow),(New-S4ControlsRow),(New-S4MeleeRow),(New-S4HoldRow))
$tb=New-S4Artifact 'sprint4-natural-tb' @((New-S4TbRow))
function Accept($artifact,[string]$scenario){Assert-KmcSprint4NaturalEvidence -Request ([pscustomobject]@{scenario=$scenario}) -Artifact $artifact -Status 'PASS'}
function Reject($artifact,[string]$scenario,[string]$why){$threw=$false;try{Accept $artifact $scenario}catch{$threw=$true};Check $threw $why}
Accept $rt 'sprint4-natural-rt';Check $true 'complete real-time evidence accepted'
Accept $tb 'sprint4-natural-tb';Check $true 'complete turn-based evidence accepted'
Check ((Get-KmcPhase3dEvidenceDispatch 48 'sprint4-natural-rt').validator -ceq 'Assert-KmcSprint4NaturalEvidence') 'schema 48 dispatches to the Sprint 4 reader'
Check (Test-KmcPhase3dSchemaRegistration 48 'sprint4-natural-tb') 'schema 48 is registered for the Sprint 4 scenarios'
Check (-not (Test-KmcPhase3dSchemaRegistration 48 'sprint3-approach-rt')) 'schema 48 is refused for another scenario'
Check (-not (Test-KmcPhase3dSchemaRegistration 47 'sprint4-natural-rt')) 'a Sprint 3 schema is refused for a Sprint 4 scenario'
Check (Test-KmcPersistenceAreasIsolationSelection -Scenario 'sprint4-natural-rt' -PersistenceCase $null -ObserveWorkingDeserialization $false) 'Sprint 4 rows select the profile Areas isolation'
# ATTACK-01
$r=Copy-S4 $rt;$r.rows[0].evidence.steps.surface.rider.retiredFacts=1;Reject $r 'sprint4-natural-rt' 'a leased retired fact refused'
$r=Copy-S4 $rt;$r.rows[0].evidence.steps.surface.rider.listed=@('f88a50d6fdbebbd709c3e323d2f52f5e');Reject $r 'sprint4-natural-rt' 'a retired helper in the ordinary list refused'
$r=Copy-S4 $rt;$r.rows[0].evidence.steps.surface.rider.dismountFacts=0;Reject $r 'sprint4-natural-rt' 'a missing Dismount refused'
$r=Copy-S4 $rt;$r.rows[0].evidence.steps.surface.blueprintsResolve.riderPrimary=$false;Reject $r 'sprint4-natural-rt' 'an unresolvable retired blueprint refused'
$r=Copy-S4 $rt;$r.rows[0].evidence.steps.reconciled.slotsCleared=0;Reject $r 'sprint4-natural-rt' 'an uncleared stale hotbar entry refused'
$r=Copy-S4 $rt;$r.rows[0].evidence.steps.reconciled.autoUseCleared=2;Reject $r 'sprint4-natural-rt' 'a repeated auto-use clear refused'
$r=Copy-S4 $rt;$r.rows[0].evidence.steps.reconciled.rider.slots=@();Reject $r 'sprint4-natural-rt' 'a cleared unrelated slot refused'
$r=Copy-S4 $rt;$r.rows[0].evidence.steps.reconciled.rider.autoUse='27364df661b3c121eabb97a31aa73a83';Reject $r 'sprint4-natural-rt' 'a surviving auto-use selection refused'
$r=Copy-S4 $rt;$r.observations.sprint4Natural.retiredHelpersSelected=$true;Reject $r 'sprint4-natural-rt' 'a run with the helpers selected refused'
# AI-01 retarget
$r=Copy-S4 $rt;$r.rows[1].evidence.inputs+=[pscustomobject]@{kind='hostile-click';target=$next;clicked=$true;inputPath='ClickUnitHandler.OnClick'};Reject $r 'sprint4-natural-rt' 'a second hostile click refused'
$r=Copy-S4 $rt;$r.rows[1].evidence.after.controller.nativeRequests=7;Reject $r 'sprint4-natural-rt' 'an extra native request refused'
$r=Copy-S4 $rt;$r.rows[1].evidence.pairCommands[0].createdByPlayer=$true;Reject $r 'sprint4-natural-rt' 'a command forged as a player order refused'
$r=Copy-S4 $rt;$r.rows[1].evidence.pairCommands[0].automaticIntent=$false;Reject $r 'sprint4-natural-rt' 'a next-target command outside the automatic path refused'
$r=Copy-S4 $rt;$r.rows[1].evidence.pairCommands[0]|Add-Member manualTargetAtDispatch $next;Reject $r 'sprint4-natural-rt' 'a forged manual target refused'
$r=Copy-S4 $rt;$r.rows[1].evidence.pairCommands[0].riderStandardCooldown=3.0;Reject $r 'sprint4-natural-rt' 'a dispatch inside the native cooldown refused'
$r=Copy-S4 $rt;$r.rows[1].evidence.after.controller.duplicateDispatches=6;Reject $r 'sprint4-natural-rt' 'a duplicate dispatch refused'
$r=Copy-S4 $rt;$r.rows[1].evidence.rules.events=@($r.rows[1].evidence.rules.events|Where-Object{$_.target -cne $low});Reject $r 'sprint4-natural-rt' 'a first target dying without the pair''s attack refused'
$r=Copy-S4 $rt;$r.rows[1].evidence.rules.events=@($r.rows[1].evidence.rules.events|Where-Object{$_.kind -cne 'attack-roll'});Reject $r 'sprint4-natural-rt' 'a first target dying without a native hit refused'
$r=Copy-S4 $rt;$r.rows[1].evidence.rules.events=@($r.rows[1].evidence.rules.events|Where-Object{$_.kind -cne 'weapon-resolved'});Reject $r 'sprint4-natural-rt' 'a first target dying without a resolved attack refused'
$r=Copy-S4 $rt;$r.rows[1].evidence.steps.firstTargetDead.frame=50;Reject $r 'sprint4-natural-rt' 'a death recorded before its hit resolved refused'
$r=Copy-S4 $rt;$r.rows[1].evidence.steps.firstTargetDead.state.targets[0].dead=$false;Reject $r 'sprint4-natural-rt' 'a first target still alive refused'
# Attack integrity
$r=Copy-S4 $rt;$r.rows[1].evidence.pairCommands[0].plannedAttacks=4;Reject $r 'sprint4-natural-rt' 'a routine plan differing from the native plan refused'
$r=Copy-S4 $rt;$r.rows[1].evidence.pairCommands[0].fullAttack=$false;Reject $r 'sprint4-natural-rt' 'a single attack in place of the native full attack refused'
$r=Copy-S4 $rt;$r.rows[1].evidence.nativePlan.ranged=2;Reject $r 'sprint4-natural-rt' 'a plan without Haste and Rapid Shot refused'
$r=Copy-S4 $rt;$r.rows[1].evidence.rules.events+=(New-S4Attack $rider $next 212 9);Reject $r 'sprint4-natural-rt' 'an extra strike in a routine refused'
$r=Copy-S4 $rt;$r.rows[1].evidence.rules.events=@($r.rows[1].evidence.rules.events|Where-Object{$_.identity -ne 3});Reject $r 'sprint4-natural-rt' 'a missing strike in a routine refused'
$r=Copy-S4 $rt;$r.rows[1].evidence.rules.events[4].identity=2;Reject $r 'sprint4-natural-rt' 'a repeated strike identity refused'
$r=Copy-S4 $rt;$r.rows[1].evidence.mountDisplacement=4.0;Reject $r 'sprint4-natural-rt' 'a ranged rider dragging the mount refused'
$r=Copy-S4 $rt;$r.rows[1].evidence.rules.events+=(New-S4Attack $mount $next 300 8);Reject $r 'sprint4-natural-rt' 'an out-of-reach mount attack in a ranged row refused'
$r=Copy-S4 $rt;$r.rows[3].evidence.rules.events=@($r.rows[3].evidence.rules.events|Where-Object{$_.actor -cne $mount});Reject $r 'sprint4-natural-rt' 'a melee retarget without the mount''s attack refused'
# Controls
$r=Copy-S4 $rt;$r.rows[2].evidence.steps.afterStop.dispatchesDuring=1;Reject $r 'sprint4-natural-rt' 'an automatic restart after Stop refused'
$r=Copy-S4 $rt;$r.rows[2].evidence.steps.afterStop.refusedDuring=0;Reject $r 'sprint4-natural-rt' 'a quiet period without a live brain refused'
$r=Copy-S4 $rt;$r.rows[2].evidence.steps.afterStop.state.riderAiEnabled=$false;Reject $r 'sprint4-natural-rt' 'Stop turning the AI off refused'
$r=Copy-S4 $rt;$r.rows[2].evidence.steps.afterReEnable.state.controller.intentAutomatic=$false;Reject $r 'sprint4-natural-rt' 'an AI re-enable that resumed nothing refused'
$r=Copy-S4 $rt;$r.rows[2].evidence.steps.manualHeld.attacksOnOldTargetSinceManual=1;Reject $r 'sprint4-natural-rt' 'an automatic attack over a manual target refused'
$r=Copy-S4 $rt;$r.rows[2].evidence.steps.manualAttacked.intentOwnedByPlayer=$false;Reject $r 'sprint4-natural-rt' 'a manual click not owning its intent refused'
$r=Copy-S4 $rt;$r.rows[2].evidence.steps.paused.dispatchesDuring=1;Reject $r 'sprint4-natural-rt' 'a dispatch during pause refused'
$r=Copy-S4 $rt;$r.rows[2].evidence.steps.castDone.dispatchesDuringCast=1;Reject $r 'sprint4-natural-rt' 'an automatic dispatch during a cast refused'
$r=Copy-S4 $rt;$r.rows[2].evidence.carriedSeen=$false;Reject $r 'sprint4-natural-rt' 'a cast not carried by the mount refused'
$r=Copy-S4 $rt;$r.rows[2].evidence.steps.castDone.recipientDamage=3;Reject $r 'sprint4-natural-rt' 'a cast without native effect refused'
$r=Copy-S4 $rt;$r.rows[2].evidence.steps.PSObject.Properties.Remove('resumedAfterCast');Reject $r 'sprint4-natural-rt' 'no resumption after the cast refused'
$r=Copy-S4 $rt;$r.rows[2].evidence.steps.heldInBowReach.dispatchesDuring=0;Reject $r 'sprint4-natural-rt' 'a Hold row that never dispatched refused'
$r=Copy-S4 $rt;$r.rows[2].evidence.steps.heldInBowReach.holdKept=$false;Reject $r 'sprint4-natural-rt' 'a dispatch clearing native Hold refused'
$r=Copy-S4 $rt;$r.rows[2].evidence.steps.aiOffRetired.retiredDuring=0;Reject $r 'sprint4-natural-rt' 'AI off leaving the automatic intent live refused'
$r=Copy-S4 $rt;$r.rows[2].evidence.steps.aiOffRetired.state.controller.lastRetirement='stop';Reject $r 'sprint4-natural-rt' 'an AI-off row ended by another cause refused'
$r=Copy-S4 $rt;$r.rows[3].evidence.pairCommands[0].plannedAttacks=5;Reject $r 'sprint4-natural-rt' 'a melee routine above the native plan refused'
$r=Copy-S4 $rt;$r.rows[1].evidence.pairCommands[1].frame=200;$r.rows[1].evidence.pairCommands[1]|Add-Member endFrame 230;Reject $r 'sprint4-natural-rt' 'overlapping routines on one actor refused'
# Hold
$r=Copy-S4 $rt;$r.rows[4].evidence.steps.heldAfterMove.mountDisplacement=3;Reject $r 'sprint4-natural-rt' 'a chase after a player move refused'
$r=Copy-S4 $rt;$r.rows[4].evidence.steps.heldAfterMove.dispatchesDuring=1;Reject $r 'sprint4-natural-rt' 'a dispatch out of reach under the move hold refused'
$r=Copy-S4 $rt;$r.rows[4].evidence.steps.heldInReach.holdKept=$false;Reject $r 'sprint4-natural-rt' 'an automatic path clearing native Hold on the melee row refused'
$r=Copy-S4 $rt;$r.rows[4].evidence.steps.heldInReach.mountDisplacement=2;Reject $r 'sprint4-natural-rt' 'movement under native Hold refused'
# Turn-based neighbor
$r=Copy-S4 $tb;$r.rows[0].evidence.after.controller.admitted=3;Reject $r 'sprint4-natural-tb' 'an automatic admission in turn-based combat refused'
$r=Copy-S4 $tb;$r.rows[0].evidence.steps.settled.sameTurn=$false;Reject $r 'sprint4-natural-tb' 'an automatic End Turn refused'
$r=Copy-S4 $tb;$r.rows[0].evidence.mountAfterRider.standard=6;Reject $r 'sprint4-natural-tb' 'a rider attack spending the mount refused'
$r=Copy-S4 $tb;$r.rows[0].evidence.riderAfter.standard=0;Reject $r 'sprint4-natural-tb' 'a rider attack without its own native cost refused'
$r=Copy-S4 $tb;$r.rows[0].evidence.mountAfter.standard=0;Reject $r 'sprint4-natural-tb' 'a mount attack without its own native cost refused'
$r=Copy-S4 $tb;$r.rows[0].evidence.rules.events=@($r.rows[0].evidence.rules.events|Where-Object{$_.actor -cne $mount});Reject $r 'sprint4-natural-tb' 'a mount without a stock attack refused'
# Envelope
Reject $rt 'sprint4-natural-tb' 'a real-time envelope under the turn-based scenario refused'
$r=Copy-S4 $rt;$r.status='FAIL';$r.errors=@('x');$threw=$false;try{Accept $r 'sprint4-natural-rt'}catch{$threw=$true};Check $threw 'a failed run is never promoted to PASS'
$r=Copy-S4 $rt;$r.rows=@($r.rows[0]);Reject $r 'sprint4-natural-rt' 'a missing required row refused'
Write-Host ('Sprint 4 reader checks passed: '+$passed)
