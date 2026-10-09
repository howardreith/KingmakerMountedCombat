$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
$passed=0
function Check([bool]$condition,[string]$why){if(-not$condition){throw $why};$script:passed++;Write-Host ('PASS '+$why)}
function Copy-Staged($v){$v|ConvertTo-Json -Depth 50 -Compress|ConvertFrom-Json}
$Rider='rider';$Mount='mount';$Hostile='hostile'
$Scroll='cd635d5720937b044a354dba17abad8d';$Potion='d52566ae8cbe8dc4dae977ef51c27d91';$Clw='5590652e1c2225c4ca30c4a699ab3649';$Snowball='9f10909f0be1f5141bf1c102041f93d9'
$castingLeases=@([pscustomobject]@{blueprint=$Potion;equipped=[pscustomobject]@{item=61;count=1;exactSlot=$true}},[pscustomobject]@{blueprint=$Scroll;equipped=[pscustomobject]@{item=60;count=1;exactSlot=$true}})
function New-ReactionState([double]$riderSwift,[double]$riderStandard,[string]$turnActor,$slotAvailable,[int]$scrollCount=10){
 [pscustomobject]@{relationship='Mounted';generation=1;rider=[pscustomobject]@{actor=$Rider;standard=$riderStandard;move=0.0;swift=$riderSwift};mount=[pscustomobject]@{actor=$Mount;standard=0.0;move=0.0;swift=0.0}
  riderCommandsEmpty=$true;mountCommandsEmpty=$true;processesSettled=$true;nativeAbilitiesPending=$false;projectilesPending=$false;activePairCommand=$false;pairMovement=$false
  items=@([pscustomobject]@{blueprint=$Potion;item=61;count=2;charges=1;exactSlot=$true},[pscustomobject]@{blueprint=$Scroll;item=60;count=$scrollCount;charges=1;exactSlot=$true})
  slotAvailable=$slotAvailable;mountUsedOneMove=$false;mountUsedTwoMove=$false;mountHasMove=$true;mountMoveRestricted=$false;riderHasStandard=$true;riderHasSwift=($riderSwift -lt 6);riderHasMove=$true;riderCanAct=$true;turnActor=$turnActor;turnIsActing=$true;turnTimeMoved=0.0
  mountPosition=[pscustomobject]@{x=0.0;y=0.0;z=0.0};riderPosition=[pscustomobject]@{x=0.0;y=0.0;z=0.0};rejectionCodes=@()}
}
function New-Rod(){[pscustomobject]@{item=62;blueprint='55a059b32df920c4abe65b8ee8b56056';count=1;charges=3;exactSlot=$true;activatableOn=$true;activatableSourceItem=62}}
function New-SwiftRecord([string]$instrument,[string]$target,$before,[bool]$admitted,[string]$turnActor,[int]$shellIdentity=11,[int]$cost=101){
 $r=[pscustomobject]@{rod=(New-Rod);instrument=$instrument;ability=[pscustomobject]@{identity=6;blueprint=$instrument;caster=$Rider;runtimeActionType='Swift';fullRound=$false;sourceItem=0;sourceItemBlueprint=$null;available=$true;availableForCast=$true};target=$target;before=$before;turnActor=$turnActor;ignoreClick=(-not$admitted);riderCanAct=$admitted;selected=$true;resolvedTarget=$target;canTarget=$true;clicked=$admitted;inputCount=1;admittedShellCount=$(if($admitted){1}else{0});shell=$(if($admitted){[pscustomobject]@{identity=$shellIdentity;executor=$Rider}}else{$null});costShell=$(if($admitted){$cost}else{0});queued=$false;selectionAfter=@($Rider);afterInput=$before;groundDistanceToRider=12.0;groundDistanceToMount=12.0;groundDistanceToHostile=10.5}
 $r
}
function New-SwiftEvents([string]$instrument,[int]$shellIdentity,[int]$cost,$beforeRider,$afterRider){
 @{events=@([pscustomobject]@{kind='action-after';identity=$shellIdentity;actor=$Rider;shell=[pscustomobject]@{ignoreCooldown=$false;timeSinceStart=2.0}},[pscustomobject]@{kind='cast-after';actor=$Rider;ability=[pscustomobject]@{blueprint=$instrument;sourceItem=0};process=21;spellFailed=$false;arcaneFailed=$false},[pscustomobject]@{kind='spell-spend-after';actor=$Rider;ability=[pscustomobject]@{blueprint=$instrument}})
   costs=@([pscustomobject]@{boundary='cost-before';command=$cost;actionType='Swift';state=$beforeRider},[pscustomobject]@{boundary='cost-after';command=$cost;actionType='Swift';state=$afterRider})}
}
function New-Terminal([int]$id,[string]$type,[string]$executor,[bool]$finished=$true,[bool]$started=$true){[pscustomobject]@{identity=$id;costIdentity=$id*10;type=$type;executor=$executor;createdByPlayer=$true;started=$started;acted=$finished;finished=$finished;result=$(if($finished){'Success'}else{'None'});ignoreCooldown=$false;commandType=$(if($type-ceq'UnitAttack'){'Standard'}else{'Swift'});timeSinceStart=1.0}}
function New-RuleEvents([string]$case){
 [pscustomobject]@{dropped=0;events=@([pscustomobject]@{caseId=$case;kind='attack-before';identity=900;frame=1000;actor=$Hostile;target=$Mount},[pscustomobject]@{caseId=$case;kind='attack-roll';identity=901;frame=1001;actor=$Hostile;target=$Mount;result='Hit';autoHit=$false;autoMiss=$false},[pscustomobject]@{caseId=$case;kind='weapon-resolved';identity=902;frame=1002;actor=$Hostile;target=$Mount;attack=900},[pscustomobject]@{caseId=$case;kind='damage-after';identity=903;frame=1002;actor=$Hostile;target=$Mount;damage=4});attacks=@([pscustomobject]@{identity=900;actor=$Hostile;target=$Mount;projectile=$false;resolved=$true;result='Hit';opportunity=$false;weapon='w'})}
}
# Synthetic rows: the own-turn Swift cast is admitted; the out-of-turn and attack-window Swift inputs are
# refused natively (the expected law); admitted variants are exercised explicitly below.
function New-ReactionRow([string]$name,[bool]$tb,[bool]$admitOutOfTurn=$false){
 $before=New-ReactionState 0.0 0.0 $Rider $null
 $plan=@(Get-KmcChunk6eReactionPlan $name);$kind=$plan[0]
 $step=[pscustomobject]@{index=0;kind=$kind;frame=100;costEvents=@();events=@();terminal=$null;hostileAttackTerminal=$null;after=$null}
 $rules=[pscustomobject]@{dropped=0;events=@();attacks=@()}
 switch -CaseSensitive($kind){
  'cast-swift' {
   $after=New-ReactionState $(if($tb){6.0}else{4.0}) 0.0 $Rider $false
   $swift=New-SwiftRecord $Clw $Rider $before $true $Rider
   $ev=New-SwiftEvents $Clw 11 101 $before.rider $after.rider
   $step|Add-Member swift $swift;$step.events=$ev.events;$step.costEvents=$ev.costs;$step.after=$after;$step.terminal=New-Terminal 11 'UnitUseAbility' $Rider
  }
  'foreign-window-swift' {
   if($tb){
    $swift=New-SwiftRecord $Snowball $Hostile $before $admitOutOfTurn $Hostile
    $after=New-ReactionState $(if($admitOutOfTurn){6.0}else{0.0}) 0.0 $Hostile $(-not$admitOutOfTurn)
    $step|Add-Member foreignTurnActor $Hostile;$step|Add-Member swift $swift;$step.after=$after
    if($admitOutOfTurn){$ev=New-SwiftEvents $Snowball 11 101 $before.rider $after.rider;$step.events=$ev.events;$step.costEvents=$ev.costs;$step.terminal=New-Terminal 11 'UnitUseAbility' $Rider}
   } else {
    $primary=[pscustomobject]@{ability=[pscustomobject]@{identity=5;blueprint=$Clw;caster=$Rider;runtimeActionType='Standard';fullRound=$false;sourceItem=60;sourceItemBlueprint=$Scroll;available=$true;availableForCast=$true};target=$Rider;before=$before;turnActor=$null;ignoreClick=$null;riderCanAct=$true;selected=$true;resolvedTarget=$Rider;canTarget=$true;clicked=$true;inputCount=1;admittedShellCount=1;shell=[pscustomobject]@{identity=10;executor=$Rider};costShell=100;queued=$false;selectionAfter=@($Rider);afterInput=$before}
    $mid=New-ReactionState 0.0 4.0 $null $null 9
    $swift=New-SwiftRecord $Snowball $Hostile $mid $admitOutOfTurn $null
    $after=New-ReactionState $(if($admitOutOfTurn){4.0}else{0.0}) 4.0 $null $(-not$admitOutOfTurn) 9
    $step|Add-Member primaryInput $primary;$step|Add-Member primaryShellAtInput ([pscustomobject]@{identity=10;started=$true;acted=$false;finished=$false});$step|Add-Member swift $swift;$step.after=$after
    $step.events=@([pscustomobject]@{kind='cast-after';actor=$Rider;ability=[pscustomobject]@{blueprint=$Clw;sourceItem=60};process=20;spellFailed=$false;arcaneFailed=$false},[pscustomobject]@{kind='item-spend-after';identity=60;actor=$Rider;charges=1;count=9;result=$true})
    $step.costEvents=@([pscustomobject]@{boundary='cost-before';command=100;actionType='Standard';state=$before.rider},[pscustomobject]@{boundary='cost-after';command=100;actionType='Standard';state=$mid.rider})
    if($admitOutOfTurn){$ev=New-SwiftEvents $Snowball 11 101 $mid.rider $after.rider;$step.events+=$ev.events;$step.costEvents+=$ev.costs}
    $step.terminal=New-Terminal 10 'UnitUseAbility' $Rider
   }
  }
  'hostile-attack-mount' {
   $after=New-ReactionState 0.0 0.0 $Rider $null
   $step|Add-Member before $before;$step|Add-Member hostileAttackInputCount 1;$step|Add-Member hostileActor $Hostile;$step|Add-Member hostileAttack (New-Terminal 50 'UnitAttack' $Hostile $false $false);$step|Add-Member afterInput $before
   $step.after=$after;$step.hostileAttackTerminal=New-Terminal 50 'UnitAttack' $Hostile;$rules=New-RuleEvents $name
  }
  'hostile-attack-swift' {
   $swift=New-SwiftRecord $Snowball $Hostile $before $admitOutOfTurn $Hostile
   $after=New-ReactionState $(if($admitOutOfTurn){if($tb){6.0}else{4.0}}else{0.0}) 0.0 $Rider $(-not$admitOutOfTurn)
   $step|Add-Member before $before;$step|Add-Member hostileAttackInputCount 1;$step|Add-Member hostileActor $Hostile;$step|Add-Member hostileAttack (New-Terminal 50 'UnitAttack' $Hostile $false $false);$step|Add-Member afterInput $before
   $step|Add-Member hostileAttackAtInput (New-Terminal 50 'UnitAttack' $Hostile $false $true);$step|Add-Member foreignTurnActor $Hostile;$step|Add-Member swift $swift
   $step.after=$after;$step.hostileAttackTerminal=New-Terminal 50 'UnitAttack' $Hostile;$rules=New-RuleEvents $name
   if($admitOutOfTurn){$ev=New-SwiftEvents $Snowball 11 101 $before.rider $after.rider;$step.events=$ev.events;$step.costEvents=$ev.costs;$step.terminal=New-Terminal 11 'UnitUseAbility' $Rider}
  }
 }
 [pscustomobject]@{name=$name;status='PASS';evidence=[pscustomobject]@{case=$name;plan=$plan;before=$before;rowTurnActor=$Rider;hostileActor=$Hostile;steps=@($step);after=$step.after;events=@($step.events);costEvents=@($step.costEvents);ruleEvents=$rules;rulesComplete=$true}}
}
function Accept($row,[bool]$tb){Assert-KmcChunk6eReactionRow $row $Rider $Mount $tb $castingLeases}
function Reject($row,[string]$why,[bool]$tb=$true){$refused=$false;try{Accept $row $tb}catch{$refused=$true};Check $refused $why}
foreach($tb in @($true,$false)){foreach($name in Get-KmcChunk6eReactionCases){Accept (New-ReactionRow $name $tb) $tb;Check $true ('native fact row (refused out-of-turn Swift) '+$name+' TB='+$tb)}}
foreach($tb in @($true,$false)){foreach($name in @('C6E-swift-out-of-turn','C6E-reaction-window')){Accept (New-ReactionRow $name $tb $true) $tb;Check $true ('native fact row (admitted out-of-turn Swift, charged exactly once) '+$name+' TB='+$tb)}}
# Frozen 205 TB (Stage 12): a Swift input issued outside the rider's own turn is admitted into a shell that never
# starts (finished without a start, no cost pair, no cast, no spend; swift debt and slot unchanged) - the native
# refusal shape. The same never-started shell on the rider's own turn stays a refused own-turn cast.
function New-NeverStartedRow([string]$name,[bool]$interrupted=$false){
 $r=New-ReactionRow $name $true $true;$s=$r.evidence.steps[0]
 $s.events=@();$s.costEvents=@();$s.after=New-ReactionState 0.0 0.0 $(if($name-ceq'C6E-reaction-window'){$Rider}else{$Hostile}) $true
 $s.terminal=New-Terminal 11 'UnitUseAbility' $Rider $true $false
 if($interrupted){$s.terminal.acted=$false;$s.terminal.result='Interrupt'}
 $r.evidence.after=$s.after;$r.evidence.events=@();$r.evidence.costEvents=@()
 $r
}
$r=New-NeverStartedRow 'C6E-swift-out-of-turn';Accept $r $true;Check $true 'turn-based out-of-turn Swift shell that never started (finished without running) is the native refusal'
$r=New-NeverStartedRow 'C6E-reaction-window' $true;Accept $r $true;Check $true 'turn-based attack-window Swift shell interrupted before it started is the native refusal'
$r=New-NeverStartedRow 'C6E-swift-out-of-turn';$r.evidence.steps[0].after=New-ReactionState 6.0 0.0 $Hostile $true;$r.evidence.after=$r.evidence.steps[0].after;Reject $r 'a never-started Swift shell cannot change the Swift debt'
$r=New-NeverStartedRow 'C6E-swift-out-of-turn';$r.evidence.steps[0].after=New-ReactionState 0.0 0.0 $Hostile $false;$r.evidence.after=$r.evidence.steps[0].after;Reject $r 'a never-started Swift shell cannot spend the memorized slot'
$r=New-NeverStartedRow 'C6E-swift-out-of-turn';$r.evidence.steps[0].terminal.identity=12;Reject $r 'a never-started shell must be the exact admitted rider shell'
$r=New-NeverStartedRow 'C6E-swift-out-of-turn';$r.evidence.steps[0].terminal.started=$true;Reject $r 'a shell that started without its single Swift charge is refused'
$r=New-ReactionRow 'C6E-swift-on-own-turn' $true;$s=$r.evidence.steps[0];$s.events=@();$s.costEvents=@();$s.after=New-ReactionState 0.0 0.0 $Rider $true;$s.terminal=New-Terminal 11 'UnitUseAbility' $Rider $true $false;$r.evidence.after=$s.after;$r.evidence.events=@();$r.evidence.costEvents=@();Reject $r 'an own-turn Swift shell that never started is still a refused own-turn cast'
# Frozen 205 RT (Stage 11): the native Swift charge is read at the cost boundary (6 s less the shell's running time) and
# the settled state decays after it; a settled debt above the charge or a cost boundary that differs from the timing is refused.
$r=New-ReactionRow 'C6E-swift-on-own-turn' $false;$s=$r.evidence.steps[0];$s.after=New-ReactionState 3.7 0.0 $Rider $false;$r.evidence.after=$s.after;Accept $r $false;Check $true 'real-time own-turn Swift accepts the settled cooldown decaying below the native charge'
$r=New-ReactionRow 'C6E-swift-on-own-turn' $false;$s=$r.evidence.steps[0];$s.after=New-ReactionState 4.5 0.0 $Rider $false;$r.evidence.after=$s.after;Reject $r 'real-time settled Swift debt above the native charge is refused' $false
$r=New-ReactionRow 'C6E-swift-on-own-turn' $false;$r.evidence.steps[0].costEvents[1].state=New-ReactionState 3.0 0.0 $Rider $false;$r.evidence.steps[0].costEvents[1].state=$r.evidence.steps[0].costEvents[1].state.rider;Reject $r 'real-time Swift charge that differs from the shell timing is refused' $false
# Swift ownership.
$r=New-ReactionRow 'C6E-swift-on-own-turn' $true;$r.evidence.steps[0].swift.ability.runtimeActionType='Standard';Reject $r 'a quickened instrument that is not Swift-typed is refused'
# Frozen 203 RT: a ground instrument cast beside the pair made the rider and mount save at the attack seam.
$r=New-ReactionRow 'C6E-swift-on-own-turn' $true;$r.evidence.steps[0].swift.instrument='0fd00984a2c0e0a429cf1a911b4ec5ca';$r.evidence.steps[0].swift.ability.blueprint='0fd00984a2c0e0a429cf1a911b4ec5ca';$r.evidence.steps[0].swift.target=$null;$r.evidence.steps[0].swift.resolvedTarget=$null;$r.evidence.steps[0].events=@(New-SwiftEvents '0fd00984a2c0e0a429cf1a911b4ec5ca' 11 101 $r.evidence.before.rider $r.evidence.after.rider).events;Accept $r $true;Check $true 'a ground instrument cast away from the pair is accepted'
$r=New-ReactionRow 'C6E-swift-on-own-turn' $true;$r.evidence.steps[0].swift.instrument='0fd00984a2c0e0a429cf1a911b4ec5ca';$r.evidence.steps[0].swift.ability.blueprint='0fd00984a2c0e0a429cf1a911b4ec5ca';$r.evidence.steps[0].swift.target=$null;$r.evidence.steps[0].swift.resolvedTarget=$null;$r.evidence.steps[0].events=@(New-SwiftEvents '0fd00984a2c0e0a429cf1a911b4ec5ca' 11 101 $r.evidence.before.rider $r.evidence.after.rider).events;$r.evidence.steps[0].swift.groundDistanceToMount=3.0;Reject $r 'a ground instrument whose area reaches the pair is refused'
$r=New-ReactionRow 'C6E-swift-on-own-turn' $true;$r.evidence.steps[0].swift.rod.activatableOn=$false;Reject $r 'an inactive rod cannot own the Swift instrument'
$r=New-ReactionRow 'C6E-swift-on-own-turn' $true;$r.evidence.steps[0].swift.admittedShellCount=0;$r.evidence.steps[0].swift.shell=$null;Reject $r 'the own-turn Swift baseline must be admitted'
$r=New-ReactionRow 'C6E-swift-on-own-turn' $true;$r.evidence.steps[0].after.rider.swift=3.0;Reject $r 'incorrect additive Swift debt is refused'
$r=New-ReactionRow 'C6E-swift-on-own-turn' $false;$r.evidence.steps[0].after.rider.swift=5.0;Reject $r 'incorrect real-time Swift debt is refused' $false
$r=New-ReactionRow 'C6E-swift-on-own-turn' $true;$r.evidence.steps[0].costEvents[1].actionType='Standard';Reject $r 'Swift debt written by a non-Swift command is refused'
$r=New-ReactionRow 'C6E-swift-on-own-turn' $true;$r.evidence.steps[0].after.slotAvailable=$true;Reject $r 'an admitted quickened cast must spend its memorized slot'
$r=New-ReactionRow 'C6E-swift-on-own-turn' $true;$r.evidence.steps[0].swift.ability.sourceItem=60;Reject $r 'a Swift instrument served by an item is refused'
$r=New-ReactionRow 'C6E-swift-on-own-turn' $true;$r.evidence.steps[0].costEvents+=[pscustomobject]@{boundary='cost-after';command=999;actionType='Swift';state=$r.evidence.before.rider};$r.evidence.costEvents=$r.evidence.steps[0].costEvents;Reject $r 'rider debt outside a shell the row admitted is refused'
# Out-of-turn facts.
$r=New-ReactionRow 'C6E-swift-out-of-turn' $true;$r.evidence.steps[0].after.rider.swift=6.0;Reject $r 'a refused out-of-turn input that still changed Swift debt is refused'
$r=New-ReactionRow 'C6E-swift-out-of-turn' $true;$r.evidence.steps[0].foreignTurnActor=$Rider;$r.evidence.steps[0].swift.turnActor=$Rider;Reject $r 'an out-of-turn row issued on the rider''s own turn is refused'
$r=New-ReactionRow 'C6E-swift-out-of-turn' $true;$r.evidence.steps[0].after.slotAvailable=$false;Reject $r 'a refused Swift input that spent the slot is refused'
$r=New-ReactionRow 'C6E-swift-out-of-turn' $true $true;$r.evidence.steps[0].costEvents=@();$r.evidence.costEvents=@();Reject $r 'an admitted out-of-turn Swift command without its native charge is refused'
$r=New-ReactionRow 'C6E-swift-out-of-turn' $false;$r.evidence.steps[0].primaryShellAtInput.finished=$true;Reject $r 'an RT window input issued after the Standard cast finished is refused' $false
# Attack seam facts.
$r=New-ReactionRow 'C6E-attack-on-mount-observed' $true;$r.evidence.ruleEvents.events=@($r.evidence.ruleEvents.events|Where-Object kind -CNE 'attack-roll');Reject $r 'an attack seam without its native roll is refused'
$r=New-ReactionRow 'C6E-attack-on-mount-observed' $true;$r.evidence.steps[0].after.rider.swift=6.0;Reject $r 'Swift debt moving at the attack seam without a Swift command is refused'
$r=New-ReactionRow 'C6E-attack-on-mount-observed' $true;$r.evidence.ruleEvents.events+=[pscustomobject]@{caseId='C6E-attack-on-mount-observed';kind='saving-throw';identity=904;frame=1003;actor=$Rider;target=$Rider};Reject $r 'a rider check at the attack seam is refused (no feat effect exists)'
$r=New-ReactionRow 'C6E-attack-on-mount-observed' $true;$r.evidence.ruleEvents.events[1].autoMiss=$true;Reject $r 'a negated attack roll is refused (no feat effect exists)'
$r=New-ReactionRow 'C6E-attack-on-mount-observed' $true;$r.evidence.steps[0].hostileAttackTerminal.finished=$false;Reject $r 'an unfinished hostile attack is refused'
$r=New-ReactionRow 'C6E-reaction-window' $true;$r.evidence.steps[0].hostileAttackAtInput.started=$false;Reject $r 'a window input issued before the hostile attack started is refused'
$r=New-ReactionRow 'C6E-reaction-window' $true;$r.evidence.ruleEvents.events[0].target=$Rider;Reject $r 'an attack that did not target the mount is refused'
$r=New-ReactionRow 'C6E-swift-on-own-turn' $true;$r.evidence.rulesComplete=$false;Reject $r 'unresolved attack rules are refused'
$r=New-ReactionRow 'C6E-swift-on-own-turn' $true;$r.evidence.plan=@('cast-standard-scroll');Reject $r 'row plan differing from the registered plan is refused'
# Registration.
$launcher=Join-Path $PSScriptRoot 'runtime/Invoke-KingmakerRuntimeScenario.ps1'
$tokens=$null;$errors=$null;$launchAst=[Management.Automation.Language.Parser]::ParseFile($launcher,[ref]$tokens,[ref]$errors)
Check ($errors.Count-eq0) 'actual guarded launcher parses'
$metadata=Get-Command $launcher
$registered=@(($metadata.Parameters['Scenario'].Attributes|Where-Object {$_-is[Management.Automation.ValidateSetAttribute]}).ValidValues)
$bindOnly=[scriptblock]::Create($launchAst.ParamBlock.Extent.Text+"`n'bound-without-launch'")
foreach($scenario in @('chunk6e-reaction-rt','chunk6e-reaction-tb')){
 Check ($registered-ccontains$scenario) ('actual guarded launcher registers '+$scenario)
 Check ((& $bindOnly -Scenario $scenario -RunId 'c6e-registration-probe')-ceq'bound-without-launch') ('actual native parameter guard admits '+$scenario)
 Check ((Get-KmcSaveBackedRuntimeScenarios)-ccontains$scenario) ('request registry agrees '+$scenario)
 Check (Test-KmcPhase3dSchemaRegistration 46 $scenario) ('exact schema registered '+$scenario)
 Check (-not(Test-KmcPhase3dSchemaRegistration 45 $scenario)) ('staged schema rejected '+$scenario)
 Check ((Get-KmcPhase3dEvidenceDispatch 46 $scenario).validator-ceq'Assert-KmcChunk6eReactionEvidence') ('exact reader selected '+$scenario)
 Check (-not(Test-KmcChildEntryExpectsMounted $scenario)) ('native original-pair exploration handoff '+$scenario)
 Check (Test-KmcCompoundRuntimeScenario $scenario) ('compound root registered '+$scenario)
}
foreach($row in Get-KmcChunk6eReactionCases){Check ((Get-KmcPhase3dHorseRuntimeRows)-ccontains$row) ('shared row registry carries '+$row)}
# Full envelope through the real Phase 3D dispatch.
$tokens=$null;$parseErrors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'Test-ChildEntryPreamble.ps1'),[ref]$tokens,[ref]$parseErrors)
$definition=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq'New-Preamble'},$true));if($parseErrors.Count-ne0-or$definition.Count-ne1){throw 'Existing preamble fixture unavailable'};. ([scriptblock]::Create($definition[0].Extent.Text))
function New-ReactionEnvelopeItems($rows){
 $spends=@($rows|ForEach-Object {@($_.evidence.events)}|Where-Object {$_.kind-ceq'item-spend-after'-and$_.identity-eq60-and$_.result-eq$true}).Count
 @([pscustomobject]@{blueprint='55a059b32df920c4abe65b8ee8b56056';requestedCount=1;equipped=[pscustomobject]@{item=62;count=1;exactSlot=$true};beforeCleanup=[pscustomobject]@{item=62;count=1;exactSlot=$true};disposed=$true;slotRestored=$true;noOwnedItemResident=$true},
   [pscustomobject]@{blueprint=$Potion;requestedCount=2;equipped=[pscustomobject]@{item=61;count=1;exactSlot=$true};stacked=[pscustomobject]@{item=61;count=2;exactSlot=$true};beforeCleanup=[pscustomobject]@{item=61;count=2;exactSlot=$true};disposed=$true;slotRestored=$true;noOwnedItemResident=$true},
   [pscustomobject]@{blueprint=$Scroll;requestedCount=10;equipped=[pscustomobject]@{item=60;count=1;exactSlot=$true};stacked=[pscustomobject]@{item=60;count=10;exactSlot=$true};beforeCleanup=[pscustomobject]@{item=60;count=(10-$spends);exactSlot=$true};disposed=$true;slotRestored=$true;noOwnedItemResident=$true})
}
$root=Join-Path (Get-KmcRepositoryRoot) ('obj/chunk6e-envelope/'+[Guid]::NewGuid().ToString('N'))
foreach($scenario in @('chunk6e-reaction-rt','chunk6e-reaction-tb')){
 $tb=$scenario.EndsWith('-tb');$rows=@(Get-KmcChunk6eReactionCases|ForEach-Object {New-ReactionRow $_ $tb})
 $pre=New-Preamble $false;$pre.childScenario=$scenario;$pre.parentScenario=$scenario;$pre.parentEngine='KingmakerMountedCombat.Diagnostics.Chunk6aMammothScenarioEngine';$pre.runId='reaction-envelope'
 $trace=[pscustomobject]@{faults=0;dropped=0;observationErrors=0;identityRegistry=[pscustomobject]@{faults=0;released=$true;retainedCount=0}}
 $plans=[pscustomobject]@{};foreach($c in Get-KmcChunk6eReactionCases){$plans|Add-Member $c @(Get-KmcChunk6eReactionPlan $c)}
 $artifact=[pscustomobject]@{schemaVersion=46;evidenceKind='phase3d-horse-scenario-evidence';runId='reaction-envelope';scenario=$scenario;branch='codex/mounted-combat-phase3f-playable-core';commit=('a'*40);productVersion='0.1.0-chunk6d-preview.203';dllSha256=('b'*64);dllMvid='00000000-0000-0000-0000-000000000001';createdAtUtc='2026-10-09T00:00:00Z';status='PASS';rows=$rows;subscenarioPassCount=$rows.Count;subscenarioFailCount=0;errors=@();observations=[pscustomobject]@{riderId=$Rider;horseId=$Mount;childEntryPreamble=$pre;chunk6eReaction=[pscustomobject]@{contract='native-mounted-reaction-feasibility-v1';rider=$Rider;mount=$Mount;mountBlueprint='e7aa96d15a45238438ae4cfb476f6bb9';mode=$(if($tb){'TB'}else{'RT'});mounted=$true;cases=@(Get-KmcChunk6eReactionCases);plans=$plans;castTrace=$trace;costTrace=$trace;ruleTrace=[pscustomobject]@{dropped=0;events=@();attacks=@()};items=(New-ReactionEnvelopeItems $rows);summonCleanup=@();final=$rows[0].evidence.after}}}
 $request=@{runId=$artifact.runId;scenario=$scenario;branch=$artifact.branch;commit=$artifact.commit;productVersion=$artifact.productVersion;dllSha256=$artifact.dllSha256;dllMvid=$artifact.dllMvid;evidenceRoot=(Join-Path $root $scenario)};[void][IO.Directory]::CreateDirectory($request.evidenceRoot);$path=Join-Path $request.evidenceRoot 'phase3d-horse-scenario-evidence.json';$manifest=@{artifacts=@(@{relativePath='phase3d-horse-scenario-evidence.json';kind='phase3d-horse-scenario-evidence'})}
 function Envelope($value){$value|ConvertTo-Json -Depth 70|Set-Content -LiteralPath $path -Encoding UTF8;Assert-KmcPhase3dHorseScenarioEvidence $request $manifest 'PASS'}
 Envelope $artifact;Check $true ('complete current envelope '+$scenario)
 foreach($mutation in @({param($a)$a.schemaVersion=45},{param($a)$a.rows=@($a.rows|Select-Object -Skip 1);$a.subscenarioPassCount--},{param($a)$a.observations.chunk6eReaction.castTrace.faults=1},{param($a)$a.rows[0].evidence.steps[0].swift.rod.activatableOn=$false},{param($a)$a.observations.chunk6eReaction.ruleTrace.dropped=1},{param($a)$a.observations.chunk6eReaction.items[1].beforeCleanup.exactSlot=$false})){
  $changed=Copy-Staged $artifact;& $mutation $changed;$rejected=$false;try{Envelope $changed}catch{$rejected=$true};Check $rejected ('full envelope retains substantive refusal '+$scenario)
 }
 Envelope $artifact
}
Write-Host ('TOTAL PASS='+$passed+' FAIL=0')
