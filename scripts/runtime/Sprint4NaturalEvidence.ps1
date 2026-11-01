# Sprint 4 (ATTACK-01, AI-01) external reader. The rows only record native observations; PASS is decided here.
$script:KmcSprint4RetiredGuids = @('27364df661b3c121eabb97a31aa73a83','f88a50d6fdbebbd709c3e323d2f52f5e')
function Get-KmcSprint4NaturalCases([string]$Scenario) {
 if($Scenario -ceq 'sprint4-natural-tb'){ return @('S4-tb-ordinary-actors') }
 if($Scenario -ceq 'sprint4-natural-rt'){ return @('S4-ui-clean','S4-ranged-retarget','S4-controls','S4-melee-retarget','S4-hold-no-chase') }
 throw ('Sprint 4 scenario has no case list: '+$Scenario)
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
 if(@($Row.evidence.rules.events | Where-Object { [string]$_.kind -ceq 'damage' -and [string]$_.target -ceq $first }).Count -eq 0 -and @($Row.evidence.rules.events | Where-Object { [string]$_.target -ceq $first -and [string]$_.kind -clike '*damage*' }).Count -eq 0){ throw 'Sprint 4 first target died without a native damage rule' }
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
 $null=Assert-KmcSprint4Retarget $Row $Rider $Mount; $second=[string]$Row.evidence.secondTarget
 if($Row.evidence.riderWeapon.ranged -ne $false){ throw 'Sprint 4 melee row did not run with a melee weapon' }
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
 if([long]$done.dispatchesDuringCast -ne 0 -or [string]$done.state.controller.lastRetirement -cne 'newer player operation'){ throw 'Sprint 4 automatic attacks did not yield to the cast' }
 if([int]$done.recipientDamage -ge [int]$before.recipientDamage){ throw 'Sprint 4 cast never took native effect' }
 $null=Get-KmcSprint4Step $Row 'resumedAfterCast'
 if(@(Get-KmcSprint4Inputs $Row 'stop').Count -ne 2 -or @(Get-KmcSprint4Inputs $Row 'cast').Count -ne 1 -or @(Get-KmcSprint4Inputs $Row 'hostile-click').Count -ne 1){ throw 'Sprint 4 controls row input count differs' }
 Assert-KmcSprint4Delta $Row 'duplicateDispatches' 0
}
function Assert-KmcSprint4HoldRow($Row,[string]$Rider,[string]$Mount){
 $arrived=Get-KmcSprint4Step $Row 'afterMoveArrival'; $held=Get-KmcSprint4Step $Row 'heldAfterMove'
 if([double]$arrived.distanceToTarget -lt 6){ throw 'Sprint 4 hold fixture left the target in reach' }
 if([double]$held.mountDisplacement -gt 0.5 -or [long]$held.dispatchesDuring -ne 0){ throw 'Sprint 4 automatic attacks chased after a completed player move' }
 if([long]$held.refusedDuring -lt 1 -or [string]$held.state.controller.lastRefusal -cnotlike 'hold-out-of-reach*'){ throw 'Sprint 4 no-chase period was not the live brain being refused' }
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
function Assert-KmcSprint4NaturalEvidence {
 param([Parameter(Mandatory=$true)]$Request,[Parameter(Mandatory=$true)]$Artifact,[AllowNull()][string]$Status)
 if($Request.scenario -cnotin @('sprint4-natural-rt','sprint4-natural-tb') -or $Artifact.schemaVersion -ne 48){ throw 'Sprint 4 scenario/schema mismatch' }
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
   'S4-ranged-retarget' { Assert-KmcSprint4RangedRow $row $o.rider $o.mount }
   'S4-controls' { Assert-KmcSprint4ControlsRow $row $o.rider $o.mount }
   'S4-melee-retarget' { Assert-KmcSprint4MeleeRow $row $o.rider $o.mount }
   'S4-hold-no-chase' { Assert-KmcSprint4HoldRow $row $o.rider $o.mount }
   'S4-tb-ordinary-actors' { Assert-KmcSprint4TurnBasedRow $row $o.rider $o.mount }
  }
 }
 if($o.castTrace.faults -ne 0 -or $o.castTrace.dropped -ne 0 -or $o.castTrace.identityRegistry.faults -ne 0 -or $o.castTrace.identityRegistry.released -ne $true){ throw 'Sprint 4 casting trace faulted' }
 if($o.final.riderCommandsEmpty -ne $true -or $o.final.mountCommandsEmpty -ne $true -or $o.final.processesSettled -ne $true -or $o.final.nativeAbilitiesPending -ne $false -or $o.final.projectilesPending -ne $false){ throw 'Sprint 4 fixture did not settle' }
 foreach($item in @($o.items)){ if($item.disposed -ne $true -or $item.slotRestored -ne $true -or $item.noOwnedItemResident -ne $true){ throw 'Sprint 4 disposable native item ownership remains' } }
 if((Test-KmcSprint4Prop $o 'riderAiRestored') -and $o.riderAiRestored -ne $true){ throw 'Sprint 4 rider AI flag was not restored' }
 if($Status -ceq 'PASS' -and $Artifact.status -cne 'PASS'){ throw 'Sprint 4 failed native observation cannot be promoted' }
}
