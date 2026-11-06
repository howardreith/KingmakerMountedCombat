# Sprint 4 (ATTACK-01, AI-01) external reader. The rows only record native observations; PASS is decided here.
$script:KmcSprint4RetiredGuids = @('27364df661b3c121eabb97a31aa73a83','f88a50d6fdbebbd709c3e323d2f52f5e')
function Get-KmcSprint4NaturalCases([string]$Scenario) {
 if($Scenario -ceq 'sprint4-natural-tb'){ return @('S4-tb-ordinary-actors') }
 if($Scenario -ceq 'sprint4-natural-rt'){ return @('S4-ui-clean','S4-ranged-retarget','S4-controls','S4-melee-retarget','S4-hold-no-chase') }
 if($Scenario -ceq 'sprint4-intent-rt'){ return @('S4-move-into-combat','S4-queued-hold','S4-intent-neighbors') }
 if($Scenario -ceq 'finalize4-ai-rt'){ return @('F4-melee-engage','S4-ranged-retarget','S4-controls','S4-melee-retarget','S4-hold-no-chase') }
 throw ('Sprint 4 scenario has no case list: '+$Scenario)
}
# F4 (AI-02): a fresh engagement with rider AI on and no click. The native brain's automatic pair command approaches a
# hostile that walks toward the pair and attacks it; a chase it is closing never exhausts the bounded repath allowance.
function Assert-KmcFinalize4EngageRow($Row,[string]$Rider,[string]$Mount){
 $walker=[string]$Row.evidence.firstTarget
 if([string]::IsNullOrEmpty($walker)){ throw 'F4 engagement row lacks its hostile' }
 if(@(Get-KmcSprint4Inputs $Row 'hostile-click').Count -ne 0 -or @(Get-KmcSprint4Inputs $Row 'ground-click').Count -ne 0){ throw 'F4 engagement row was given a player order' }
 $toggles=@(Get-KmcSprint4Inputs $Row 'ai-toggle')
 if($toggles.Count -lt 1 -or [string]$toggles[-1].name -cne 'engagement-no-click' -or $toggles[-1].value -ne $true){ throw 'F4 engagement row did not run with the rider AI on' }
 $walk=Get-KmcSprint4Step $Row 'walkOrdered'; $dispatch=Get-KmcSprint4Step $Row 'automaticDispatch'; $dead=Get-KmcSprint4Step $Row 'walkerDead'; $null=Get-KmcSprint4Step $Row 'settled'
 if([double]$walk.gapToMount -lt 10){ throw 'F4 engagement hostile did not start out of reach' }
 if([double]$dead.walkerDisplacement -lt 2){ throw 'F4 engagement hostile did not move toward the pair' }
 if([double]$dead.mountDisplacement -lt 1){ throw 'F4 engagement mount did not approach' }
 if($dead.riderUnreachable -ne $false){ throw 'F4 engagement target was marked unreachable for the rider' }
 $commands=@($Row.evidence.pairCommands | Where-Object { [string]$_.actor -ceq $Rider -and [string]$_.target -ceq $walker })
 if($commands.Count -lt 1){ throw 'F4 engagement no rider pair command addressed the hostile' }
 foreach($command in $commands){
  if($command.automaticIntent -ne $true -or $command.createdByPlayer -ne $false){ throw 'F4 engagement command is not the automatic path' }
  if((Test-KmcSprint4Prop $command 'repaths') -and [int]$command.repaths -gt 4){ throw 'F4 engagement command exceeded the bounded repath allowance' }
  if((Test-KmcSprint4Prop $command 'terminalReason') -and [string]$command.terminalReason -cmatch 'bounded (repath allowance|execution time)'){ throw 'F4 engagement approach faulted on its bound' }
 }
 $approach=@($commands | Where-Object { $_.approachRequired -eq $true -and (Test-KmcSprint4Prop $_ 'endFrame') })
 if($approach.Count -lt 1){ throw 'F4 engagement no completed rider command approached the hostile' }
 if((@($approach | ForEach-Object { [int]$_.progressingRepaths }) | Measure-Object -Sum).Sum -lt 1){ throw 'F4 engagement never followed the moving hostile' }
 $strikes=@(Get-KmcSprint4Attacks $Row $Rider $walker)
 if($strikes.Count -lt 1){ throw 'F4 engagement rider never attacked the hostile' }
 if([int]$strikes[0].frame -lt [int]$approach[0].frame){ throw 'F4 engagement strike precedes the approaching command' }
 if(($Row.evidence.costEvents | Measure-Object).Count -eq 0){ throw 'F4 engagement records no native expenditure' }
}
# F4 (AI-01): the clicked routine continues natively on the next remembered enemy once its first target is down; the
# continuation spends nothing new, so the routine's strikes never exceed its own native plan.
function Assert-KmcFinalize4ContinuationRow($Row,[string]$Rider){
 $first=[string]$Row.evidence.firstTarget; $second=[string]$Row.evidence.secondTarget
 $clicked=@($Row.evidence.pairCommands | Where-Object { [string]$_.actor -ceq $Rider -and [string]$_.target -ceq $first -and (Test-KmcSprint4Prop $_ 'endFrame') } | Sort-Object { [int]$_.frame })
 if($clicked.Count -lt 1){ throw 'F4 continuation the clicked routine has no terminal' }
 $routine=$clicked[0]
 $inWindow={ param($target) @($Row.evidence.rules.events | Where-Object { [string]$_.kind -ceq 'attack-before' -and [string]$_.actor -ceq $Rider -and [string]$_.target -ceq $target -and [int]$_.frame -ge [int]$routine.frame -and [int]$_.frame -le [int]$routine.endFrame }) }
 $onFirst=@(& $inWindow $first); $onSecond=@(& $inWindow $second); $planned=[int]$routine.plannedAttacks
 if($onFirst.Count -lt 1 -or ($onFirst.Count + $onSecond.Count) -gt $planned){ throw ('F4 continuation routine struck '+($onFirst.Count+$onSecond.Count)+' times against a plan of '+$planned) }
 if($onFirst.Count -lt $planned){
  if([int]$routine.continuations -ne 1 -or [string]$routine.continuationTargets -cne $second -or [string]$routine.endTarget -cne $second){ throw 'F4 continuation the routine ended at the kill instead of continuing on the next target' }
  if($onSecond.Count -lt 1){ throw 'F4 continuation the remaining planned attacks never reached the next target' }
 } elseif([int]$routine.continuations -ne 0){ throw 'F4 continuation recorded with no remaining planned attack' }
}
function Test-KmcSprint4Prop($Object,[string]$Name){ $null -ne $Object -and $null -ne $Object.PSObject.Properties[$Name] -and $null -ne $Object.$Name }
function Get-KmcSprint4Attacks($Row,[string]$Actor,[string]$Target){ @($Row.evidence.rules.events | Where-Object { [string]$_.kind -ceq 'attack-before' -and [string]$_.actor -ceq $Actor -and [string]$_.target -ceq $Target }) }
function Get-KmcSprint4Step($Row,[string]$Name){ if(-not (Test-KmcSprint4Prop $Row.evidence.steps $Name)){ throw ('Sprint 4 row '+$Row.name+' never reached step '+$Name) }; $Row.evidence.steps.$Name }
function Get-KmcSprint4Inputs($Row,[string]$Kind){ @($Row.evidence.inputs | Where-Object { [string]$_.kind -ceq $Kind }) }
function Assert-KmcSprint4Delta($Row,[string]$Name,[long]$Expected){ $delta=[long]$Row.evidence.after.controller.$Name-[long]$Row.evidence.before.controller.$Name; if($delta -ne $Expected){ throw ('Sprint 4 row '+$Row.name+' '+$Name+' changed by '+$delta+', expected '+$Expected) } }
function Assert-KmcSprint4UiRow($Row,[string]$Rider,[string]$Mount){
 $surface=Get-KmcSprint4Step $Row 'surface'
 foreach($unit in @($surface.rider,$surface.mount)){
  if([int]$unit.retiredFacts -ne 0 -or @($unit.listed | Where-Object { $script:KmcSprint4RetiredGuids -ccontains [string]$_ }).Count -ne 0){ throw 'Sprint 4 retired helper remains on the ordinary ability surface' }
  if(@($unit.slots | Where-Object { $script:KmcSprint4RetiredGuids -ccontains [string]$_.blueprint }).Count -ne 0){ throw 'Sprint 4 retired helper remains on a hotbar' }
 }
 if([int]$surface.rider.dismountFacts -ne 1 -or [int]$surface.mount.dismountFacts -ne 0 -or [int]$surface.mount.mountFacts -ne 0){ throw 'Sprint 4 Mount/Dismount surface differs for a mounted pair' }
 if($surface.blueprintsResolve.riderPrimary -ne $true -or $surface.blueprintsResolve.mountPrimary -ne $true){ throw 'Sprint 4 retired blueprint identity no longer resolves' }
 $done=Get-KmcSprint4Step $Row 'reconciled'
 if($done.staleInjected -ne $true){ throw 'Sprint 4 stale reference was never presented' }
 if(@($surface.staleSlotsAtInjection | Where-Object { [int]$_.index -eq [int]$done.staleIndex -and $script:KmcSprint4RetiredGuids -ccontains [string]$_.blueprint }).Count -ne 1){ throw 'Sprint 4 stale hotbar entry was not present at injection' }
 if(([long]$done.slotsCleared-[long]$surface.slotsClearedBefore) -ne 1 -or ([long]$done.autoUseCleared-[long]$surface.autoUseClearedBefore) -ne 1){ throw 'Sprint 4 stale references were not each cleared exactly once' }
 if(Test-KmcSprint4Prop $done.rider 'autoUse'){ throw 'Sprint 4 stale auto-use selection survived' }
 if(($done.rider.slots | ConvertTo-Json -Depth 6 -Compress) -cne ($surface.unrelatedSlotsBefore | ConvertTo-Json -Depth 6 -Compress)){ throw 'Sprint 4 reconciliation changed an unrelated hotbar slot' }
 if([int]$done.rider.retiredFacts -ne 0 -or [int]$done.mount.retiredFacts -ne 0 -or [int]$done.rider.dismountFacts -ne 1){ throw 'Sprint 4 repeated refresh changed the leased facts' }
}
function Assert-KmcSprint4Retarget($Row,[string]$Rider,[string]$Mount){
 $first=[string]$Row.evidence.firstTarget; $second=[string]$Row.evidence.secondTarget
 if([string]::IsNullOrEmpty($first) -or [string]::IsNullOrEmpty($second) -or $first -ceq $second){ throw 'Sprint 4 retarget row lacks two distinct targets' }
 $clicks=@(Get-KmcSprint4Inputs $Row 'hostile-click')
 if($clicks.Count -ne 1 -or [string]$clicks[0].target -cne $first -or $clicks[0].clicked -ne $true -or [string]$clicks[0].inputPath -cne 'ClickUnitHandler.OnClick'){ throw 'Sprint 4 retarget row did not use exactly one ordinary click on the first target' }
 Assert-KmcSprint4Delta $Row 'nativeRequests' 1
 Assert-KmcSprint4Delta $Row 'duplicateDispatches' 0
 $dead=Get-KmcSprint4Step $Row 'firstTargetDead'
 if(@($dead.state.targets | Where-Object { [string]$_.id -ceq $first -and $_.dead -eq $true }).Count -ne 1){ throw 'Sprint 4 first target did not die' }
 if((@(Get-KmcSprint4Attacks $Row $Rider $first).Count + @(Get-KmcSprint4Attacks $Row $Mount $first).Count) -eq 0){ throw 'Sprint 4 first target died without the pair''s own native attack' }
 # The death's cause in the native rules: a Hit by a pair actor, whose resolution raised the target's damage, before it died.
 $hits=@($Row.evidence.rules.events | Where-Object { [string]$_.kind -ceq 'attack-roll' -and [string]$_.target -ceq $first -and [string]$_.result -cin @('Hit','CriticalHit') -and ([string]$_.actor -ceq $Rider -or [string]$_.actor -ceq $Mount) })
 $before=@($Row.evidence.rules.events | Where-Object { [string]$_.kind -ceq 'attack-before' -and [string]$_.target -ceq $first } | Sort-Object { [int]$_.frame })
 $resolved=@($Row.evidence.rules.events | Where-Object { [string]$_.kind -ceq 'weapon-resolved' -and [string]$_.target -ceq $first -and [int]$_.frame -le [int]$dead.frame -and $before.Count -gt 0 -and [int]$_.targetDamage -gt [int]$before[0].targetDamage })
 if($hits.Count -eq 0 -or $resolved.Count -eq 0){ throw 'Sprint 4 first target died without a native hit raising its damage' }
 if(@(Get-KmcSprint4Attacks $Row $Rider $second).Count -lt 1){ throw 'Sprint 4 rider never attacked the unclicked next target' }
 $next=@($Row.evidence.pairCommands | Where-Object { [string]$_.target -ceq $second })
 if($next.Count -eq 0){ throw 'Sprint 4 no pair command addressed the next target' }
 foreach($command in $next){
  if($command.automaticIntent -ne $true -or $command.createdByPlayer -ne $false -or $command.aiAction -ne $false){ throw 'Sprint 4 next-target command is not the distinct automatic path' }
  if((Test-KmcSprint4Prop $command 'manualTargetAtDispatch') -and [string]$command.manualTargetAtDispatch -ceq $second){ throw 'Sprint 4 automatic dispatch forged a manual target' }
  if([string]$command.actor -ceq $Rider -and [double]$command.riderStandardCooldown -gt 0.001){ throw 'Sprint 4 automatic dispatch started before the rider''s native cooldown' }
  if([string]$command.actor -ceq $Mount -and [double]$command.mountStandardCooldown -gt 0.001){ throw 'Sprint 4 automatic dispatch started before the mount''s native cooldown' }
 }
 $controller=$Row.evidence.after.controller
 if(([long]$controller.admitted-[long]$Row.evidence.before.controller.admitted) -lt 1){ throw 'Sprint 4 no native proposal was admitted' }
 if(($Row.evidence.costEvents | Measure-Object).Count -eq 0){ throw 'Sprint 4 retarget row records no native expenditure' }
 # Routines on one actor never overlap: a new target buys no second routine inside the first.
 foreach($actor in @($Rider,$Mount)){
  $own=@($next | Where-Object { [string]$_.actor -ceq $actor } | Sort-Object { [int]$_.frame })
  for($index=1;$index -lt $own.Count;$index++){ if(-not (Test-KmcSprint4Prop $own[$index-1] 'endFrame') -or [int]$own[$index].frame -lt [int]$own[$index-1].endFrame){ throw 'Sprint 4 automatic routines overlap on one actor' } }
 }
 $next
}
function Assert-KmcSprint4RangedRow($Row,[string]$Rider,[string]$Mount){
 $next=@(Assert-KmcSprint4Retarget $Row $Rider $Mount); $second=[string]$Row.evidence.secondTarget
 $plan=$Row.evidence.nativePlan
 if($plan.enoughClose -ne $true){ throw 'Sprint 4 ranged fixture did not start in native bow range' }
 if([int]$plan.ranged -lt 3){ throw 'Sprint 4 Haste and Rapid Shot did not enlarge the native ranged plan' }
 if([double]$Row.evidence.mountDisplacement -gt 0.5){ throw 'Sprint 4 ranged rider dragged the mount away from its firing position' }
 if(@(Get-KmcSprint4Attacks $Row $Mount $second).Count -ne 0){ throw 'Sprint 4 ranged retarget gave the out-of-reach mount a natural attack' }
 $riders=@($next | Where-Object { [string]$_.actor -ceq $Rider })
 if($riders.Count -lt 2){ throw 'Sprint 4 fewer than two rider routines on the next target' }
 foreach($command in @($riders | Where-Object { Test-KmcSprint4Prop $_ 'endFrame' })){
  if([string]$command.action -cne 'RiderRanged' -or $command.fullAttack -ne $true -or [int]$command.plannedAttacks -ne [int]$plan.count){ throw 'Sprint 4 automatic routine differs from the native full-attack plan' }
  $strikes=@($Row.evidence.rules.events | Where-Object { [string]$_.kind -ceq 'attack-before' -and [string]$_.actor -ceq $Rider -and [string]$_.target -ceq $second -and [int]$_.frame -ge [int]$command.frame -and [int]$_.frame -le [int]$command.endFrame })
  if($strikes.Count -ne [int]$plan.ranged){ throw ('Sprint 4 routine fired '+$strikes.Count+' native strikes against a plan of '+[int]$plan.ranged) }
  if(@($strikes | Select-Object -ExpandProperty identity -Unique).Count -ne $strikes.Count){ throw 'Sprint 4 strike identity repeated inside a routine' }
 }
 if(@($riders | Where-Object { Test-KmcSprint4Prop $_ 'endFrame' }).Count -lt 1){ throw 'Sprint 4 no automatic routine reached its terminal' }
}
function Assert-KmcSprint4MeleeRow($Row,[string]$Rider,[string]$Mount){
 $next=@(Assert-KmcSprint4Retarget $Row $Rider $Mount); $second=[string]$Row.evidence.secondTarget
 if($Row.evidence.riderWeapon.ranged -ne $false){ throw 'Sprint 4 melee row did not run with a melee weapon' }
 foreach($command in @($next | Where-Object { [string]$_.actor -ceq $Rider -and (Test-KmcSprint4Prop $_ 'endFrame') })){
  if([int]$command.plannedAttacks -gt [int]$Row.evidence.nativePlan.count){ throw 'Sprint 4 melee routine exceeds the native full-attack plan' }
  $strikes=@($Row.evidence.rules.events | Where-Object { [string]$_.kind -ceq 'attack-before' -and [string]$_.actor -ceq $Rider -and [string]$_.target -ceq $second -and [int]$_.frame -ge [int]$command.frame -and [int]$_.frame -le [int]$command.endFrame })
  if($strikes.Count -gt [int]$command.plannedAttacks){ throw 'Sprint 4 melee routine struck more than its plan' }
 }
 if(@(Get-KmcSprint4Attacks $Row $Mount $second).Count -lt 1){ throw 'Sprint 4 mount lost its legitimate attack on the next target' }
}
function Assert-KmcSprint4ControlsRow($Row,[string]$Rider,[string]$Mount){
 $stop=Get-KmcSprint4Step $Row 'afterStop'
 if([long]$stop.dispatchesDuring -ne 0){ throw 'Sprint 4 Stop was followed by an automatic restart' }
 if([long]$stop.refusedDuring -lt 2 -or $stop.state.controller.stopSuppressed -ne $true -or [string]$stop.state.controller.lastRefusal -cnotlike 'stop-suppressed*'){ throw 'Sprint 4 Stop quiet period was not the live brain being refused' }
 if($stop.state.controller.hasIntent -ne $false -or [string]$stop.state.riderAiEnabled -cne 'True'){ throw 'Sprint 4 Stop left an intent or switched the AI off' }
 $again=Get-KmcSprint4Step $Row 'afterReEnable'
 if($again.state.controller.stopSuppressed -ne $false -or $again.state.controller.intentAutomatic -ne $true){ throw 'Sprint 4 explicit AI re-enable did not resume automatic attacks' }
 $manual=Get-KmcSprint4Step $Row 'manualAttacked'
 if($manual.intentOwnedByPlayer -ne $true){ throw 'Sprint 4 manual click did not own the replaced intent' }
 $held=Get-KmcSprint4Step $Row 'manualHeld'
 if([long]$held.attacksOnOldTargetSinceManual -ne 0 -or $held.manualTargetStillOwned -ne $true){ throw 'Sprint 4 automatic proposal displaced the manual target' }
 $paused=Get-KmcSprint4Step $Row 'paused'
 if($paused.pausedObserved -ne $true -or [long]$paused.proposalsDuring -ne 0 -or [long]$paused.dispatchesDuring -ne 0){ throw 'Sprint 4 automatic attacks advanced during native pause' }
 $before=Get-KmcSprint4Step $Row 'beforeCast'; $done=Get-KmcSprint4Step $Row 'castDone'
 if($before.state.controller.intentAutomatic -ne $true){ throw 'Sprint 4 cast precedence row had no live automatic intent' }
 if($Row.evidence.carriedSeen -ne $true){ throw 'Sprint 4 cast was not carried by the mount' }
 # A cast replacing the running routine ends the intent through that command's terminal; a cast beside it retires the intent. Either way nothing dispatches during it.
 if([long]$done.dispatchesDuringCast -ne 0){ throw 'Sprint 4 automatic attacks did not yield to the cast' }
 if([int]$done.recipientDamage -ge [int]$before.recipientDamage){ throw 'Sprint 4 cast never took native effect' }
 $null=Get-KmcSprint4Step $Row 'resumedAfterCast'
 $bow=Get-KmcSprint4Step $Row 'heldInBowReach'
 if([long]$bow.dispatchesDuring -lt 1 -or $bow.holdKept -ne $true -or [double]$bow.mountDisplacement -gt 0.5){ throw 'Sprint 4 automatic dispatch under native Hold moved the pair or cleared Hold' }
 $off=Get-KmcSprint4Step $Row 'aiOffRetired'
 if([long]$off.retiredDuring -lt 1 -or [long]$off.dispatchesDuring -ne 0 -or [string]$off.state.controller.lastRetirement -cne 'ai off' -or $off.state.controller.hasIntent -ne $false){ throw 'Sprint 4 AI off did not end the live automatic intent' }
 if(@(Get-KmcSprint4Inputs $Row 'stop').Count -ne 2 -or @(Get-KmcSprint4Inputs $Row 'cast').Count -ne 1 -or @(Get-KmcSprint4Inputs $Row 'hostile-click').Count -ne 1){ throw 'Sprint 4 controls row input count differs' }
 Assert-KmcSprint4Delta $Row 'duplicateDispatches' 0
}
function Assert-KmcSprint4HoldRow($Row,[string]$Rider,[string]$Mount){
 $arrived=Get-KmcSprint4Step $Row 'afterMoveArrival'; $held=Get-KmcSprint4Step $Row 'heldAfterMove'
 if([double]$arrived.distanceToTarget -lt 6){ throw 'Sprint 4 hold fixture left the target in reach' }
 if([double]$held.mountDisplacement -gt 0.5 -or [long]$held.dispatchesDuring -ne 0){ throw 'Sprint 4 automatic attacks chased after a completed player move' }
 if([long]$held.refusedDuring -lt 1 -or [string]$held.state.controller.lastRefusal -cnotlike 'hold-out-of-reach*'){ throw 'Sprint 4 no-chase period was not the live brain being refused' }
 if($held.state.controller.intentAutomatic -eq $true){ throw 'Sprint 4 automatic intent stayed live beyond a player move hold' }
 $null=Get-KmcSprint4Step $Row 'attackAfterMove'
 $hold=Get-KmcSprint4Step $Row 'afterHold'; $reach=Get-KmcSprint4Step $Row 'heldInReach'
 if($hold.state.riderHold -ne $true -or $reach.holdKept -ne $true){ throw 'Sprint 4 automatic path cleared a native Hold' }
 if([double]$reach.mountDisplacement -gt 0.5){ throw 'Sprint 4 Hold allowed the pair to move' }
 Assert-KmcSprint4Delta $Row 'duplicateDispatches' 0
}
function Assert-KmcSprint4TurnBasedRow($Row,[string]$Rider,[string]$Mount){
 $clicks=@(Get-KmcSprint4Inputs $Row 'hostile-click')
 if($clicks.Count -ne 2 -or @($clicks | Where-Object { $_.clicked -ne $true }).Count -ne 0){ throw 'Sprint 4 turn-based row did not use one ordinary click per actor' }
 $hostile=[string]$clicks[0].target
 if(@(Get-KmcSprint4Attacks $Row $Rider $hostile).Count -lt 1 -or @(Get-KmcSprint4Attacks $Row $Mount $hostile).Count -lt 1){ throw 'Sprint 4 stock attack by an actor is missing' }
 Assert-KmcSprint4Delta $Row 'admitted' 0
 Assert-KmcSprint4Delta $Row 'automaticDispatches' 0
 Assert-KmcSprint4Delta $Row 'duplicateDispatches' 0
 if((Get-KmcSprint4Step $Row 'settled').sameTurn -ne $true){ throw 'Sprint 4 turn ended automatically' }
 $null=Get-KmcSprint4Step $Row 'noAutomaticAfterRider'
 if([double]$Row.evidence.riderAfter.standard -le [double]$Row.evidence.riderBefore.standard){ throw 'Sprint 4 rider attack did not spend its own native Standard' }
 if([double]$Row.evidence.mountAfterRider.standard -ne [double]$Row.evidence.mountBefore.standard){ throw 'Sprint 4 rider attack spent the mount''s Standard' }
 if([double]$Row.evidence.mountAfter.standard -le [double]$Row.evidence.mountAfterRider.standard){ throw 'Sprint 4 mount attack did not spend its own native Standard' }
}
function Assert-KmcSprint4MoveRow($Row,[string]$Rider,[string]$Mount){
 $clicks=@(Get-KmcSprint4Inputs $Row 'ground-click')
 if($clicks.Count -ne 2 -or @($clicks | Where-Object { $_.clicked -ne $true }).Count -ne 0 -or @(Get-KmcSprint4Inputs $Row 'native-interrupt-move').Count -ne 1 -or @(Get-KmcSprint4Inputs $Row 'hold').Count -ne 1){ throw 'Sprint 4 move row did not use the ordinary ground input, one native Hold and one interruption' }
 if($Row.evidence.continueMovementOption.leased -ne $true){ throw 'Sprint 4 move row did not record the native engagement option' }
 $issued=Get-KmcSprint4Step $Row 'moveIssued'; $joined=Get-KmcSprint4Step $Row 'combatWhileMoving'; $arrival=Get-KmcSprint4Step $Row 'arrival'
 if($issued.riderInCombat -ne $false -or $issued.mountInCombat -ne $false -or $issued.finished -ne $false){ throw 'Sprint 4 move did not start before combat' }
 if([string]$issued.moveType -cne 'UnitMoveTo' -or $issued.createdByPlayer -ne $true -or [string]$issued.executor -cne $Mount){ throw 'Sprint 4 watched move is not the explicit player move on the mount' }
 if($joined.riderInCombat -ne $true -or $joined.mountInCombat -ne $true -or $joined.finished -ne $false){ throw 'Sprint 4 combat did not begin while the move was under way' }
 if([string]$joined.moveIdentity -cne [string]$issued.moveIdentity -or [string]$arrival.moveIdentity -cne [string]$issued.moveIdentity){ throw 'Sprint 4 arrival is not the exact issued move' }
 if($arrival.finished -ne $true -or $arrival.acted -ne $true -or [string]$arrival.result -cne 'Success' -or $arrival.riderInCombat -ne $true -or $arrival.turnBased -ne $false){ throw 'Sprint 4 move did not complete natively in real-time combat' }
 if($arrival.state.mountHold -ne $true -or $arrival.state.controller.playerMoveHold -ne $true -or [string]$arrival.state.controller.lastMoveOutcome -cne 'CompletedHeld'){ throw 'Sprint 4 arrival in combat did not hold the pair' }
 if([double]$arrival.distanceToTarget -lt 13){ throw 'Sprint 4 arrival left the target in reach' }
 $held=Get-KmcSprint4Step $Row 'heldAfterArrival'
 if([double]$held.mountDisplacement -gt 0.5 -or [long]$held.dispatchesDuring -ne 0){ throw 'Sprint 4 automatic attacks chased from the ordered destination' }
 if([long]$held.refusedDuring -lt 1 -or [string]$held.state.controller.lastRefusal -cnotlike 'hold-out-of-reach*' -or $held.state.controller.hasIntent -ne $false){ throw 'Sprint 4 no-chase period was not the live brain being refused' }
 $second=Get-KmcSprint4Step $Row 'secondMoveIssued'; $cut=Get-KmcSprint4Step $Row 'interrupted'
 $kinds=@($Row.evidence.inputs | Where-Object { [string]$_.kind -cin @('ground-click','hold') } | ForEach-Object { [string]$_.kind })
 if(($kinds -join ',') -cne 'ground-click,hold,ground-click'){ throw 'Sprint 4 native Hold did not come between the two moves' }
 if([string]$second.moveIdentity -ceq [string]$issued.moveIdentity -or [string]$cut.moveIdentity -cne [string]$second.moveIdentity){ throw 'Sprint 4 interruption control is not a distinct exact move' }
 if($second.state.controller.playerMoveHold -ne $false -or $second.state.riderHold -ne $false){ throw 'Sprint 4 issuing a move kept the earlier hold or the rider''s native Hold' }
 if($cut.finished -ne $true -or $cut.acted -ne $false -or [string]$cut.result -cne 'Interrupt'){ throw 'Sprint 4 interruption control did not end by a native interrupt' }
 if($cut.state.controller.playerMoveHold -ne $false -or $cut.state.mountHold -ne $false -or [string]$cut.state.controller.lastMoveOutcome -cne 'Interrupted'){ throw 'Sprint 4 interrupted move manufactured an arrival hold' }
 if([double]$cut.distanceToTarget -lt 13){ throw 'Sprint 4 interruption control left the target in reach, which proves nothing' }
 $resumed=Get-KmcSprint4Step $Row 'resumedAfterInterruption'
 if([long]$resumed.dispatchesDuring -lt 1 -or [double]$resumed.mountDisplacement -lt 1 -or $resumed.state.controller.intentAutomatic -ne $true){ throw 'Sprint 4 pair stayed restricted after an interrupted move' }
}
function Assert-KmcSprint4QueuedRow($Row,[string]$Rider,[string]$Mount){
 $before=Get-KmcSprint4Step $Row 'heldBeforeAutomatic'; if($before.state.riderHold -ne $true){ throw 'Sprint 4 native Hold was not in place' }
 $queued=Get-KmcSprint4Step $Row 'queued'
 if($queued.automaticOrigin -ne $true -or $queued.createdByPlayer -ne $false -or $queued.aiAction -ne $false -or [string]$queued.actor -cne $Mount){ throw 'Sprint 4 queued command is not the mount''s distinct automatic command' }
 if($queued.inStandardSlot -ne $false -or $queued.started -ne $false -or [int]$queued.state.mountQueue -lt 1 -or $queued.busyUninterruptible -ne $true){ throw 'Sprint 4 command was not waiting in the native queue behind an uninterruptible command' }
 if($queued.state.mountHold -ne $true -or $queued.state.riderHold -ne $true){ throw 'Sprint 4 Hold was lost before the later run' }
 $ran=Get-KmcSprint4Step $Row 'ranFromQueue'
 if([string]$ran.identity -cne [string]$queued.identity -or [long]$ran.queuedRuns -ne 1 -or [long]$ran.queuedHoldKept -ne 1 -or $ran.busyFinished -ne $true){ throw 'Sprint 4 later run is not the exact queued command through queue advancement' }
 if($ran.state.mountHold -ne $true -or $ran.state.riderHold -ne $true){ throw 'Sprint 4 later native run cleared Hold' }
 $done=Get-KmcSprint4Step $Row 'queuedDone'
 if([string]$done.result -cne 'Success'){ throw 'Sprint 4 queued command did not complete natively' }
 if([string]$done.identity -cne [string]$queued.identity -or $done.state.mountHold -ne $true -or $done.state.riderHold -ne $true){ throw 'Sprint 4 Hold did not survive the queued command' }
 if([double]$done.mountDisplacement -gt 0.5){ throw 'Sprint 4 queued command moved the held pair' }
 if([double]$done.mountStandardCooldown -le 0){ throw 'Sprint 4 queued command spent no native Standard of the mount' }
 if(@($Row.evidence.rules.events | Where-Object { [string]$_.kind -ceq 'attack-before' -and [string]$_.actor -ceq $Mount -and [int]$_.frame -ge [int]$ran.frame }).Count -lt 1){ throw 'Sprint 4 queued command never struck' }
 $held=Get-KmcSprint4Step $Row 'farTargetHeld'
 # A chase has to be admitted first, so this holds whether or not the rider's cooldown is running during the window.
 if([long]$held.state.controller.admitted -ne [long]$done.state.controller.admitted){ throw 'Sprint 4 a later out-of-reach target was admitted under Hold' }
 if([double]$held.distanceToTarget -lt 13 -or [double]$held.mountDisplacement -gt 0.5 -or [long]$held.dispatchesDuring -ne 0){ throw 'Sprint 4 a later out-of-reach target provoked movement' }
 # Under a native Hold the brain itself filters targets out of reach (AiMovementForbidden), so there is no proposal to refuse: r08 saw none. The next row's Stop shows the same brain proposing that target once Hold is gone.
 if($held.state.riderHold -ne $true -or $held.state.mountHold -ne $true -or $held.state.riderAiEnabled -ne $true -or $held.state.riderBrainActive -ne $true -or $held.state.controller.hasIntent -ne $false){ throw 'Sprint 4 later target was not held off with Hold kept and the AI live' }
 Assert-KmcSprint4Delta $Row 'duplicateDispatches' 0
}
function Assert-KmcSprint4NeighborRow($Row,[string]$Rider,[string]$Mount,[string]$FarTarget){
 $stop=Get-KmcSprint4Step $Row 'afterStop'
 if([long]$stop.dispatchesDuring -ne 0 -or [double]$stop.mountDisplacement -gt 0.5){ throw 'Sprint 4 Stop was followed by an automatic restart' }
 if([long]$stop.refusedDuring -lt 1 -or $stop.state.controller.stopSuppressed -ne $true -or [string]$stop.state.controller.lastRefusal -cnotlike 'stop-suppressed*' -or $stop.state.riderHold -ne $false){ throw 'Sprint 4 Stop quiet period was not the live brain being refused' }
 if(-not [string]::IsNullOrEmpty($FarTarget) -and [string]$stop.state.controller.lastRefusal -cne ('stop-suppressed;target='+$FarTarget)){ throw 'Sprint 4 brain refused after Stop was not proposing the far target' }
 $again=Get-KmcSprint4Step $Row 'afterReEnable'
 if($again.state.controller.stopSuppressed -ne $false -or $again.state.controller.intentAutomatic -ne $true){ throw 'Sprint 4 explicit AI re-enable did not resume automatic attacks' }
 if((Get-KmcSprint4Step $Row 'manualAttacked').intentOwnedByPlayer -ne $true){ throw 'Sprint 4 manual click did not own the replaced intent' }
 $manual=Get-KmcSprint4Step $Row 'manualHeld'
 if([long]$manual.attacksOnOldTargetSinceManual -ne 0 -or $manual.manualTargetStillOwned -ne $true){ throw 'Sprint 4 automatic proposal displaced the manual target' }
 Assert-KmcSprint4Delta $Row 'duplicateDispatches' 0
}
function Assert-KmcSprint4NaturalEvidence {
 param([Parameter(Mandatory=$true)]$Request,[Parameter(Mandatory=$true)]$Artifact,[AllowNull()][string]$Status)
 if($Request.scenario -cnotin @('sprint4-natural-rt','finalize4-ai-rt','sprint4-natural-tb','sprint4-intent-rt') -or $Artifact.schemaVersion -ne 48){ throw 'Sprint 4 scenario/schema mismatch' }
 if($Artifact.status -ceq 'FAIL') {
  if($Status -ceq 'PASS' -or (@($Artifact.errors).Count -eq 0 -and $Artifact.subscenarioFailCount -lt 1)){ throw 'Sprint 4 incomplete observation cannot become PASS' }
  return # Preserve the producer's explicit failed prefix; grant no qualification.
 }
 if($Artifact.status -cne 'PASS' -or @($Artifact.errors).Count -ne 0 -or @($Artifact.rows | Where-Object status -CNE 'PASS').Count -ne 0 -or $Artifact.subscenarioFailCount -ne 0){ throw 'Sprint 4 envelope status differs' }
 $o=$Artifact.observations.sprint4Natural
 if($null -eq $o -or $o.contract -cne 'native-natural-attacks-retargeting-v1' -or $o.rider -cne $Artifact.observations.riderId -or $o.mount -cne $Artifact.observations.horseId){ throw 'Sprint 4 measurement identity differs' }
 if($o.retiredHelpersSelected -ne $false){ throw 'Sprint 4 rows ran with the retired helpers selected' }
 $tb=$Request.scenario.EndsWith('-tb')
 if($o.mode -cne $(if($tb){'TB'}else{'RT'}) -or $o.mounted -ne $true){ throw 'Sprint 4 mode/surface differs' }
 $expected=@(Get-KmcSprint4NaturalCases $Request.scenario)
 if(($o.cases | ConvertTo-Json -Compress) -cne ($expected | ConvertTo-Json -Compress)){ throw 'Sprint 4 registered row order differs' }
 foreach($name in $expected){
  $rows=@($Artifact.rows | Where-Object name -CEQ $name); if($rows.Count -ne 1){ throw ('Sprint 4 required row missing or duplicated: '+$name) }
  $row=$rows[0]
  switch -CaseSensitive ($name) {
   'S4-ui-clean' { Assert-KmcSprint4UiRow $row $o.rider $o.mount }
   'S4-ranged-retarget' { Assert-KmcSprint4RangedRow $row $o.rider $o.mount; if($Request.scenario -ceq 'finalize4-ai-rt'){ Assert-KmcFinalize4ContinuationRow $row $o.rider } }
   'F4-melee-engage' { Assert-KmcFinalize4EngageRow $row $o.rider $o.mount }
   'S4-controls' { Assert-KmcSprint4ControlsRow $row $o.rider $o.mount }
   'S4-melee-retarget' { Assert-KmcSprint4MeleeRow $row $o.rider $o.mount }
   'S4-hold-no-chase' { Assert-KmcSprint4HoldRow $row $o.rider $o.mount }
   'S4-tb-ordinary-actors' { Assert-KmcSprint4TurnBasedRow $row $o.rider $o.mount }
   'S4-move-into-combat' { Assert-KmcSprint4MoveRow $row $o.rider $o.mount }
   'S4-queued-hold' { Assert-KmcSprint4QueuedRow $row $o.rider $o.mount }
   'S4-intent-neighbors' { Assert-KmcSprint4NeighborRow $row $o.rider $o.mount ([string](@($Artifact.rows | Where-Object name -CEQ 'S4-move-into-combat')[0]).evidence.firstTarget) }
  }
 }
 if($o.castTrace.faults -ne 0 -or $o.castTrace.dropped -ne 0 -or $o.castTrace.identityRegistry.faults -ne 0 -or $o.castTrace.identityRegistry.released -ne $true){ throw 'Sprint 4 casting trace faulted' }
 if($o.final.riderCommandsEmpty -ne $true -or $o.final.mountCommandsEmpty -ne $true -or $o.final.processesSettled -ne $true -or $o.final.nativeAbilitiesPending -ne $false -or $o.final.projectilesPending -ne $false){ throw 'Sprint 4 fixture did not settle' }
 foreach($item in @($o.items)){ if($item.disposed -ne $true -or $item.slotRestored -ne $true -or $item.noOwnedItemResident -ne $true){ throw 'Sprint 4 disposable native item ownership remains' } }
 if((Test-KmcSprint4Prop $o 'riderAiRestored') -and $o.riderAiRestored -ne $true){ throw 'Sprint 4 rider AI flag was not restored' }
 if($Status -ceq 'PASS' -and $Artifact.status -cne 'PASS'){ throw 'Sprint 4 failed native observation cannot be promoted' }
}
