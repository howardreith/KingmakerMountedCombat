$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
$passed=0
function Check([bool]$condition,[string]$why){if(-not$condition){throw $why};$script:passed++;Write-Host ('PASS '+$why)}
function Copy-CastingFixture($v){$v|ConvertTo-Json -Depth 40 -Compress|ConvertFrom-Json}
# ConvertFrom-Json returns a JSON array as one wrapped object; enumerate it back into a plain array of records.
function Copy-CastingItems($v){@((Copy-CastingFixture $v)|ForEach-Object {$_})}
# Lease snapshots of the two disposable stacks (potion 61 x2, scroll 60 x10) as CastingState records
# them, and the lease evidence the envelope carries (equipped exact single unit of each entity).
function New-CastingLeaseSnapshots{@([pscustomobject]@{blueprint='d52566ae8cbe8dc4dae977ef51c27d91';item=61;count=2;charges=1;exactSlot=$true},[pscustomobject]@{blueprint='cd635d5720937b044a354dba17abad8d';item=60;count=10;charges=1;exactSlot=$true})}
$castingLeases=@([pscustomobject]@{blueprint='d52566ae8cbe8dc4dae977ef51c27d91';equipped=[pscustomobject]@{item=61;count=1;exactSlot=$true}},[pscustomobject]@{blueprint='cd635d5720937b044a354dba17abad8d';equipped=[pscustomobject]@{item=60;count=1;exactSlot=$true}})
function New-CastingFixtureState {
 [pscustomobject]@{relationship='Mounted';generation=1;rider=[pscustomobject]@{actor='rider';standard=0.0;move=0.0;swift=0.0};mount=[pscustomobject]@{actor='mount';standard=0.0;move=0.0;swift=0.0};riderCommandsEmpty=$true;mountCommandsEmpty=$true;processesSettled=$true;nativeAbilitiesPending=$false;projectilesPending=$false;activePairCommand=$false;items=@(New-CastingLeaseSnapshots);ability=[pscustomobject]@{caster='rider';blueprint='5590652e1c2225c4ca30c4a699ab3649';runtimeActionType='Standard';fullRound=$false;sourceItem=60;sourceItemBlueprint='cd635d5720937b044a354dba17abad8d';available=$true};slotAvailable=$null}
}
function Positive([bool]$tb){
 $before=New-CastingFixtureState;$after=Copy-CastingFixture $before;$after.rider.standard=if($tb){6.0}else{4.0};$after.items[1].count=9
 $post=Copy-CastingFixture $before;$post|Add-Member shell ([pscustomobject]@{identity=10})
 [pscustomobject]@{name='C6C-standard-self';status='PASS';evidence=[pscustomobject]@{case='C6C-standard-self';before=$before;afterInput=$post;after=$after;selectionAfter=@('rider');target='rider';resolvedTarget='rider';selected=$true;canTarget=$true;clicked=$true;inputCount=1;admittedShellCount=1;costShell=100;events=@([pscustomobject]@{kind='action-after';identity=10;actor='rider';shell=[pscustomobject]@{ignoreCooldown=$false;timeSinceStart=2.0}},[pscustomobject]@{kind='cast-after';actor='rider';ability=[pscustomobject]@{blueprint='5590652e1c2225c4ca30c4a699ab3649'};process=20;spellFailed=$false;arcaneFailed=$false},[pscustomobject]@{kind='item-spend-before';identity=60;actor='rider';charges=1;count=10},[pscustomobject]@{kind='item-spend-after';identity=60;actor='rider';charges=1;count=9;result=$true});costEvents=@([pscustomobject]@{boundary='cost-before';command=100;state=$before.rider},[pscustomobject]@{boundary='cost-after';command=100;state=$after.rider})}}
}
function Accept($row,[bool]$tb=$false,[bool]$mounted=$true){Assert-KmcChunk6cCastingRow $row 'rider' 'mount' $tb $mounted $castingLeases}
function Reject($row,[string]$why,[bool]$tb=$false){$refused=$false;try{Accept $row $tb}catch{$refused=$true};Check $refused $why}
foreach($tb in @($false,$true)){Accept (Positive $tb) $tb;Check $true ('native single Standard cost accepted '+$tb)}
# Exercise the actual launcher's parameter binder without executing its body.
$launcher=Join-Path $PSScriptRoot 'runtime/Invoke-KingmakerRuntimeScenario.ps1'
$tokens=$null;$errors=$null;$launchAst=[Management.Automation.Language.Parser]::ParseFile($launcher,[ref]$tokens,[ref]$errors)
Check ($errors.Count-eq0) 'actual guarded launcher parses'
$metadata=Get-Command $launcher
$registered=@(($metadata.Parameters['Scenario'].Attributes|Where-Object {$_-is[Management.Automation.ValidateSetAttribute]}).ValidValues)
$bindOnly=[scriptblock]::Create($launchAst.ParamBlock.Extent.Text+"`n'bound-without-launch'")
foreach($scenario in @('chunk6c-casting-rt','chunk6c-casting-tb','chunk6c-casting-unmounted-rt','chunk6c-casting-unmounted-tb')){
 Check ($registered-ccontains$scenario) ('actual guarded launcher registers '+$scenario)
 Check ((& $bindOnly -Scenario $scenario -RunId 'c6c-registration-probe')-ceq'bound-without-launch') ('actual native parameter guard admits '+$scenario)
 Check ((Get-KmcSaveBackedRuntimeScenarios)-ccontains$scenario) ('request registry agrees '+$scenario)
}
$base=Positive $false
$r=Copy-CastingFixture $base;$r.evidence.before.ability.caster='mount';Reject $r 'mount caster refused'
$r=Copy-CastingFixture $base;$r.evidence.inputCount=2;Reject $r 'duplicate input refused'
$r=Copy-CastingFixture $base;$r.evidence.admittedShellCount=2;Reject $r 'duplicate source shell refused'
$r=Copy-CastingFixture $base;$r.evidence.events+=Copy-CastingFixture $r.evidence.events[1];Reject $r 'duplicate source delivery refused'
$r=Copy-CastingFixture $base;$r.evidence.after.riderCommandsEmpty=$false;Reject $r 'rider command residue refused'
$r=Copy-CastingFixture $base;$r.evidence.after.mountCommandsEmpty=$false;Reject $r 'mount command residue refused'
$r=Copy-CastingFixture $base;$r.evidence.after.processesSettled=$false;Reject $r 'pending native process refused'
$r=Copy-CastingFixture $base;$r.evidence.after.nativeAbilitiesPending=$true;Reject $r 'native execution controller residue refused'
$r=Copy-CastingFixture $base;$r.evidence.after.generation=2;Reject $r 'relationship generation substitution refused'
$r=Copy-CastingFixture $base;$r.evidence.after.mount.standard=1.0;Reject $r 'duplicate mount action cost refused'
$r=Copy-CastingFixture $base;$r.evidence.costEvents[1].state.standard=3.5;Reject $r 'incorrect native realtime debt refused'
$r=Copy-CastingFixture $base;$r.evidence.costEvents[1].state.swift=1.0;Reject $r 'extra rider Swift cost refused'
$r=Positive $true;$r.evidence.costEvents[1].state.standard=3.0;Reject $r 'incorrect additive TB debt refused' $true
$r=Copy-CastingFixture $base;$r.evidence.costEvents+=[pscustomobject]@{boundary='prepare-before';state=$r.evidence.before.rider};Reject $r 'pair preparation replay refused'
$r=Copy-CastingFixture $base;foreach($s in @($r.evidence.before,$r.evidence.afterInput,$r.evidence.after)){$s.relationship='Unmounted'};Accept $r $false $false;Check $true 'normal unmounted cost owner preserved'
foreach($name in @('C6C-invalid-target','C6C-cancel-before')){
 $r=Copy-CastingFixture $base;$r.name=$name;$r.evidence.case=$name;$r.evidence.events=@();$r.evidence.costEvents=@();$r.evidence.admittedShellCount=0;$r.evidence.inputCount=if($name-ceq'C6C-cancel-before'){0}else{1};$r.evidence.canTarget=$false;$r.evidence|Add-Member cancelledSelection $true;$r.evidence.after.items=(Copy-CastingItems $r.evidence.before.items)
 Accept $r;Check $true ('cost-free native refusal '+$name)
 $r.evidence.costEvents=@($base.evidence.costEvents[0]);Reject $r ('cost cannot masquerade as refusal '+$name)
}
foreach($scenario in @('chunk6c-casting-rt','chunk6c-casting-tb','chunk6c-casting-unmounted-rt','chunk6c-casting-unmounted-tb')){
 Check (Test-KmcPhase3dSchemaRegistration 44 $scenario) ('exact schema registered '+$scenario)
 Check (-not(Test-KmcPhase3dSchemaRegistration 43 $scenario)) ('stale schema rejected '+$scenario)
 Check ((Get-KmcPhase3dEvidenceDispatch 44 $scenario).validator-ceq'Assert-KmcChunk6cCastingEvidence') ('exact reader selected '+$scenario)
 Check (-not(Test-KmcChildEntryExpectsMounted $scenario)) ('native original-pair exploration handoff '+$scenario)
}
foreach($v in @('0.1.0-chunk6c-preview.189','0.1.0-chunk6d-preview.200','0.1.0-chunk6e-preview.201','0.1.0-chunk6f-preview.202')){Check (Test-KmcChildEntryPreambleRequired $v) ('later phase retains preamble guard '+$v)}
foreach($v in @('0.1.0-chunk6g-preview.189','0.1.0-chunk6c-preview.bad','0.1.0-chunk6c-preview.149')){Check (-not(Test-KmcChildEntryPreambleRequired $v)) ('malformed/unregistered product refused '+$v)}
function Set-CastingAction($row,[bool]$tb,[string]$action){
 $e=$row.evidence;$e.before.ability.runtimeActionType=$action
 foreach($f in @('standard','move','swift')){$e.after.rider.$f=0.0}
 $field=if($action-ceq'Swift'){'swift'}elseif($action-ceq'Move'){'move'}else{'standard'}
 $nominal=if($action-ceq'Move'){3.0}else{6.0};$e.after.rider.$field=if($tb){$nominal}else{$nominal-2.0}
 $e.costEvents[0].state=Copy-CastingFixture $e.before.rider;$e.costEvents[1].state=Copy-CastingFixture $e.after.rider
}
function New-CastingRow([string]$name,[bool]$tb,[bool]$mounted){
 $r=Positive $tb;$r.name=$name;$e=$r.evidence;$e.case=$name
 if(-not$mounted){foreach($v in @($e.before,$e.afterInput,$e.after)){$v.relationship='Unmounted'}}
 $blueprint=switch -CaseSensitive($name){'C6C-standard-hostile'{'9f10909f0be1f5141bf1c102041f93d9'} 'C6C-full-round'{'c6147854641924442a3bb736080cfeb6'} default{'5590652e1c2225c4ca30c4a699ab3649'}}
 $e.before.ability.blueprint=$blueprint;$e.events[1].ability.blueprint=$blueprint
 # Spellbook rows cast an available memorized or converted slot, never an item; every other row casts from the scroll stack.
 if($name-cin@('C6C-quickened-self','C6C-standard-hostile','C6C-full-round')){$e.before.ability.sourceItem=0;$e.before.ability.sourceItemBlueprint=$null;$e.events=@($e.events|Where-Object {$_.kind-notlike'item-spend-*'});$e.after.items=(Copy-CastingItems $e.before.items)}
 if($name-ceq'C6C-quickened-self'){
  Set-CastingAction $r $tb 'Swift'
  $e.before.items=@([pscustomobject]@{blueprint='55a059b32df920c4abe65b8ee8b56056';item=60;activatableSourceItem=60;activatableOn=$true;charges=3});$e.after.items=@([pscustomobject]@{blueprint='55a059b32df920c4abe65b8ee8b56056';item=60;charges=2})
 }
 if($name-cin@('C6C-quickened-self','C6C-standard-hostile','C6C-full-round')){$e.before.slotAvailable=$true;$e.after.slotAvailable=$false}
 if($name-ceq'C6C-scroll-interrupt-after'){$e|Add-Member interrupted $true;$e|Add-Member interruptionBefore ([pscustomobject]@{shell=[pscustomobject]@{acted=$true;finished=$false}})}
 if($name-cin@('C6C-invalid-target','C6C-cancel-before','C6C-interrupt-before','C6C-rider-incapacity')){
  $e.events=@();$e.costEvents=@();$e.after.rider.standard=0.0;$e.after.items=(Copy-CastingItems $e.before.items)
  if($name-ceq'C6C-invalid-target'){$e.canTarget=$false;$e.admittedShellCount=0}
  if($name-ceq'C6C-cancel-before'){$e.inputCount=0;$e|Add-Member cancelledSelection $true}
  if($name-ceq'C6C-interrupt-before'){$e|Add-Member interrupted $true;$e|Add-Member interruptionBefore ([pscustomobject]@{shell=[pscustomobject]@{started=$true;acted=$false}})}
 }
 if($name-cin@('C6C-potion-self','C6C-scroll-friendly')){
  $itemBlueprint=if($name-ceq'C6C-potion-self'){'d52566ae8cbe8dc4dae977ef51c27d91'}else{'cd635d5720937b044a354dba17abad8d'}
  $e.before.ability.sourceItem=60;$e.before.ability.sourceItemBlueprint=$itemBlueprint
  if($name-ceq'C6C-potion-self'){Set-CastingAction $r $tb 'Move';$e.before.ability.sourceItem=61;$e.after.items=(Copy-CastingItems $e.before.items);$e.after.items[0].count=1;$e.events=@($e.events|Where-Object {$_.kind-notlike'item-spend-*'})+@([pscustomobject]@{kind='item-spend-before';identity=61;actor='rider';charges=1;count=2},[pscustomobject]@{kind='item-spend-after';identity=61;actor='rider';charges=1;count=1;result=$true})}
  $e.events+=@([pscustomobject]@{kind='heal';actor='rider';target=$e.target;value=3})
 }
 if($name-ceq'C6C-full-round'){
  $e.before.ability.fullRound=$true
  if($tb){$e.after.rider.move=3.0;$e.costEvents[1].state.move=3.0}
  $e.events+=[pscustomobject]@{kind='summon';actor='rider';unitObject=70;context=20}
 }
 if($name-cin@('C6C-rider-incapacity','C6C-mount-incapacity')){
  $e.after.relationship='Unmounted';$e.after|Add-Member relationshipRider $null;$e.after|Add-Member relationshipMount $null
  $e.after|Add-Member riderLife ([pscustomobject]@{conscious=$true;dead=$false});$e.after|Add-Member mountLife ([pscustomobject]@{conscious=$true;dead=$false})
  $main=$name-ceq'C6C-rider-incapacity';$nativeCap=if($main){-1}else{$null}
  # The incapacitated subject leaves combat natively: its Cooldowns.Clear is nested in its own combat-exit window.
  $subjectState=if($main){$e.before.rider}else{$e.before.mount}
  $e.costEvents=@($e.costEvents)+@([pscustomobject]@{boundary='combat-clear-before';command=0;state=$subjectState},[pscustomobject]@{boundary='clear-before';command=0;state=$subjectState},[pscustomobject]@{boundary='clear-after';command=0;state=$subjectState},[pscustomobject]@{boundary='combat-clear-after';command=0;state=$subjectState})
  $riderLife=[pscustomobject]@{mainCharacter=$true;conscious=$true;dead=$false;allowDyingCondition=$true;immortal=$false;essential=$false;temporaryHitPoints=0}
  $mountLife=Copy-CastingFixture $riderLife;$mountLife.mainCharacter=$false
  $e|Add-Member boundary ([pscustomobject]@{subject=$(if($main){'rider'}else{'mount'});mainCharacter=$main;nativeDamageCap=$nativeCap;nativeRuleDamageCap=$nativeCap;nativeDamageBeforeDifficulty=11;requested=11;difficulty=1.0;nativeRuleDifficulty=1.0;nativeRuleIsFake=$false;beforeIncapacity=[pscustomobject]@{riderLife=$riderLife;mountLife=$mountLife};nativeDamage=11;damageAfter=11;hitPoints=10;deathThreshold=20;unconsciousObserved=$true;deadObserved=$false;healthRestored=$true;damageRestored=0;damageBefore=0;settledBeforeHealthRestore=[pscustomobject]@{riderCommandsEmpty=$true;mountCommandsEmpty=$true;processesSettled=$true}})
 }
 if($name-ceq'C6C-movement-policy'){
  $e.after|Add-Member pairMovement $false
  $e|Add-Member boundary ([pscustomobject]@{nativeGroundInputCount=1;nativeMovingBeforeCast=$true;carrier=40;costCarrier=400;carrierExecutor=$(if($mounted){'mount'}else{'rider'});setupCostEvents=@()})
  $e|Add-Member motionSamples @([pscustomobject]@{carrierFinished=$true;moverMoveSlotOwnsCarrier=$false})
 }
 if($name-ceq'C6C-under-threat'){
  $e|Add-Member boundary ([pscustomobject]@{riderEngaged=$true;nativeHostileAttackInputCount=1;nativeAttackTerminalBeforeCast=$true})
  $e.events+=[pscustomobject]@{kind='defensive-rule-after';actor='rider';dc=17;roll=19;success=$true}
 }
 $r
}
foreach($tb in @($false,$true)){foreach($mounted in @($false,$true)){foreach($name in Get-KmcChunk6cCastingCases){Accept (New-CastingRow $name $tb $mounted) $tb $mounted;Check $true ('native fact row '+$name+' TB='+$tb+' mounted='+$mounted)}}}
$r=New-CastingRow 'C6C-standard-self' $false $true;$r.evidence.costShell=10;Reject $r 'cost trace may not reuse another registry identity'
$r=New-CastingRow 'C6C-full-round' $true $true;$r.evidence.costEvents[1].state.move=0.0;Reject $r 'TB full-round Move commitment not dropped' $true
$r=New-CastingRow 'C6C-rider-incapacity' $false $true;$r.evidence.boundary.healthRestored=$false;Reject $r 'unrestored fixture incapacity cannot qualify'
$r=New-CastingRow 'C6C-rider-incapacity' $false $true;$r.evidence.boundary.nativeRuleDamageCap=$null;Reject $r 'main-character life stimulus cannot lose its native cap'
$r=New-CastingRow 'C6C-rider-incapacity' $false $true;$r.evidence.boundary.nativeDamageBeforeDifficulty=12;Reject $r 'native main-character damage cannot exceed the applied cap'
$r=New-CastingRow 'C6C-rider-incapacity' $false $true;$r.evidence.boundary.nativeRuleIsFake=$true;Reject $r 'fake damage cannot prove native incapacity'
$r=New-CastingRow 'C6C-rider-incapacity' $false $true;$r.evidence.boundary.nativeRuleDifficulty=2.0;Reject $r 'changed native difficulty cannot qualify the safe window'
$r=New-CastingRow 'C6C-rider-incapacity' $false $true;$r.evidence.boundary.beforeIncapacity.riderLife.mainCharacter=$false;Reject $r 'native main-character subject cannot be relabeled'
$r=New-CastingRow 'C6C-movement-policy' $false $true;$r.evidence.motionSamples[0].carrierFinished=$false;Reject $r 'live delegated movement cannot disappear'
$r=New-CastingRow 'C6C-movement-policy' $false $true;$r.evidence.boundary.nativeMovingBeforeCast=$false;Reject $r 'idle carrier is not casting during native movement'
$r=New-CastingRow 'C6C-movement-policy' $false $true;$r.evidence.boundary.PSObject.Properties.Remove('nativeMovingBeforeCast');Reject $r 'missing native movement observation cannot pass'
$r=New-CastingRow 'C6C-under-threat' $false $true;$r.evidence.boundary.riderEngaged=$false;Reject $r 'mere nearby enemy cannot stand for native threat'
$r=New-CastingRow 'C6C-potion-self' $false $true;$r.evidence.events+=$r.evidence.events[3];Reject $r 'duplicate potion spending rejected'
# Instrument and native-fact rules introduced after the preview.200 Stage 1 observation.
$r=New-CastingRow 'C6C-standard-self' $false $true;$r.evidence.before.ability.sourceItem=0;$r.evidence.before.ability.sourceItemBlueprint=$null;Reject $r 'scroll row must cast from the exact native scroll stack'
$r=New-CastingRow 'C6C-standard-self' $false $true;$r.evidence.before.ability.blueprint='c3a8f31778c3980498d8f00c980be5f5';$r.evidence.events[1].ability.blueprint='c3a8f31778c3980498d8f00c980be5f5';Reject $r 'unavailable Guidance orison cannot stand in for the scroll cast'
$r=New-CastingRow 'C6C-quickened-self' $false $true;$r.evidence.before.ability.available=$false;Reject $r 'spellbook row with an unavailable native slot refused'
$r=New-CastingRow 'C6C-quickened-self' $false $true;$r.evidence.after.slotAvailable=$true;Reject $r 'quickened memorized slot must be spent exactly once'
$r=New-CastingRow 'C6C-quickened-self' $false $true;$r.evidence.before.ability.sourceItem=60;$r.evidence.before.ability.sourceItemBlueprint='cd635d5720937b044a354dba17abad8d';Reject $r 'spellbook row cannot be served by an item'
$r=New-CastingRow 'C6C-standard-self' $false $true;$r.evidence.events=@($r.evidence.events|Where-Object {$_.kind-cne'item-spend-after'});Reject $r 'scroll cast without one native charge spend refused'
$r=New-CastingRow 'C6C-scroll-interrupt-after' $false $true;Accept $r;Check $true 'post-commit scroll interruption spends once and keeps its cost'
$r=New-CastingRow 'C6C-scroll-interrupt-after' $false $true;$r.evidence.interruptionBefore.shell.acted=$false;Reject $r 'precommit interruption cannot pass as the post-commit row'
$r=New-CastingRow 'C6C-invalid-target' $false $true;$r.evidence.events=@([pscustomobject]@{kind='item-spend-after';identity=60;actor='rider';charges=1;count=9});Reject $r 'refused scroll cast cannot spend a charge'
$r=New-CastingRow 'C6C-rider-incapacity' $false $true;$r.evidence.costEvents+=@([pscustomobject]@{boundary='clear-before';command=0;state=$r.evidence.before.rider});Reject $r 'bare pair cooldown clear outside the native combat-exit window is a replay'
$r=New-CastingRow 'C6C-rider-incapacity' $false $true;$r.evidence.costEvents=@($r.evidence.costEvents[0],[pscustomobject]@{boundary='prepare-before';command=0;state=$r.evidence.before.rider})+@($r.evidence.costEvents|Select-Object -Skip 1);Reject $r 'preparation inside the combat-exit window is still a replay'
$r=New-CastingRow 'C6C-rider-incapacity' $false $true;$r.evidence.costEvents+=@([pscustomobject]@{boundary='combat-clear-before';command=0;state=$r.evidence.before.mount},[pscustomobject]@{boundary='clear-before';command=0;state=$r.evidence.before.mount},[pscustomobject]@{boundary='clear-after';command=0;state=$r.evidence.before.mount},[pscustomobject]@{boundary='combat-clear-after';command=0;state=$r.evidence.before.mount});Reject $r 'the partner must not leave combat during the subject incapacity'
$r=New-CastingRow 'C6C-mount-incapacity' $false $true;Accept $r;Check $true 'mount incapacity accepts the native mount combat exit around a completed rider cast'
$r=New-CastingRow 'C6C-standard-self' $false $true;$r.evidence.costEvents+=@([pscustomobject]@{boundary='combat-clear-before';command=0;state=$r.evidence.before.rider},[pscustomobject]@{boundary='clear-before';command=0;state=$r.evidence.before.rider},[pscustomobject]@{boundary='clear-after';command=0;state=$r.evidence.before.rider},[pscustomobject]@{boundary='combat-clear-after';command=0;state=$r.evidence.before.rider});Reject $r 'a settled cast window tolerates no pair combat exit'
Check ((Get-KmcChunk6cPairReplayCount @() 'rider' 'mount')-eq0) 'empty cost window has no replay'
Check ((Get-KmcChunk6cPairReplayCount @([pscustomobject]@{boundary='clear-before';state=[pscustomobject]@{actor='other'}}) 'rider' 'mount')-eq0) 'foreign actor clears are not pair replays'
# Disposable stack rules introduced after the preview.201 native fact (ItemSlot.InsertItem splits stacks;
# a consumed single unit is removed through the one-bool removal the foreign refill patch intercepts).
$r=New-CastingRow 'C6C-standard-self' $false $true;$r.evidence.before.ability.sourceItem=59;Reject $r 'scroll row sourcing another same-blueprint item is refused'
$r=New-CastingRow 'C6C-standard-self' $false $true;$r.evidence.after.items[1].count=10;Reject $r 'completed scroll cast must decrement the exact stack in place'
$r=New-CastingRow 'C6C-standard-self' $false $true;$r.evidence.after.items[1].exactSlot=$false;Reject $r 'the scroll stack may not leave its slot during a cast'
$r=New-CastingRow 'C6C-standard-self' $false $true;$r.evidence.before.items[1].count=1;$r.evidence.after.items[1].count=0;$r.evidence.events[2].count=1;$r.evidence.events[3].count=0;Reject $r 'a single remaining unit cannot be the measured spend'
$r=New-CastingRow 'C6C-potion-self' $false $true;$r.evidence.before.ability.sourceItem=60;Reject $r 'potion row sourcing the scroll entity is refused'
$r=New-CastingRow 'C6C-invalid-target' $false $true;$r.evidence.after.items[1].count=9;Reject $r 'refused scroll cast cannot change the stack'
$r=New-CastingRow 'C6C-rider-incapacity' $false $true;$r.evidence.after.items[1].exactSlot=$false;Reject $r 'refused life-row scroll stack may not leave its slot'
$r=New-CastingRow 'C6C-standard-self' $false $true;Assert-KmcChunk6cCastingRow $r 'rider' 'mount' $false $true;Check $true 'row validation without lease evidence keeps the structural stack rules'
# Same file-backed envelope and original-pair preamble used by the real runtime gate.
$tokens=$null;$parseErrors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'Test-ChildEntryPreamble.ps1'),[ref]$tokens,[ref]$parseErrors)
$definition=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq'New-Preamble'},$true));if($parseErrors.Count-ne0-or$definition.Count-ne1){throw 'Existing preamble fixture unavailable'};. ([scriptblock]::Create($definition[0].Extent.Text))
$root=Join-Path (Get-KmcRepositoryRoot) ('obj/chunk6c-envelope/'+[Guid]::NewGuid().ToString('N'))
# Lease evidence of the three disposable items; the stack counts before cleanup conserve the rows' spends.
function New-CastingEnvelopeItems($rows){
 $spends=@{};foreach($id in @(60,61)){$spends[$id]=@($rows|ForEach-Object {@($_.evidence.events)}|Where-Object {$_.kind-ceq'item-spend-after'-and$_.identity-eq$id-and$_.result-eq$true}).Count}
 @([pscustomobject]@{blueprint='55a059b32df920c4abe65b8ee8b56056';requestedCount=1;equipped=[pscustomobject]@{item=62;count=1;exactSlot=$true};beforeCleanup=[pscustomobject]@{item=62;count=1;exactSlot=$false};disposed=$true;slotRestored=$true;noOwnedItemResident=$true},
   [pscustomobject]@{blueprint='d52566ae8cbe8dc4dae977ef51c27d91';requestedCount=2;equipped=[pscustomobject]@{item=61;count=1;exactSlot=$true};stacked=[pscustomobject]@{item=61;count=2;exactSlot=$true};beforeCleanup=[pscustomobject]@{item=61;count=(2-$spends[61]);exactSlot=$true};disposed=$true;slotRestored=$true;noOwnedItemResident=$true},
   [pscustomobject]@{blueprint='cd635d5720937b044a354dba17abad8d';requestedCount=10;equipped=[pscustomobject]@{item=60;count=1;exactSlot=$true};stacked=[pscustomobject]@{item=60;count=10;exactSlot=$true};beforeCleanup=[pscustomobject]@{item=60;count=(10-$spends[60]);exactSlot=$true};disposed=$true;slotRestored=$true;noOwnedItemResident=$true})
}
foreach($scenario in @('chunk6c-casting-rt','chunk6c-casting-tb','chunk6c-casting-unmounted-rt','chunk6c-casting-unmounted-tb')){
 $tb=$scenario.EndsWith('-tb');$mounted=-not$scenario.Contains('-unmounted-');$rows=@(Get-KmcChunk6cCastingCases|ForEach-Object {New-CastingRow $_ $tb $mounted})
 $pre=New-Preamble $false;$pre.childScenario=$scenario;$pre.parentScenario=$scenario;$pre.parentEngine='KingmakerMountedCombat.Diagnostics.Chunk6aMammothScenarioEngine';$pre.runId='casting-envelope'
 $trace=[pscustomobject]@{faults=0;dropped=0;observationErrors=0;identityRegistry=[pscustomobject]@{faults=0;released=$true;retainedCount=0}}
 $artifact=[pscustomobject]@{schemaVersion=44;evidenceKind='phase3d-horse-scenario-evidence';runId='casting-envelope';scenario=$scenario;branch='codex/mounted-combat-phase3f-playable-core';commit=('a'*40);productVersion='0.1.0-chunk6c-preview.189';dllSha256=('b'*64);dllMvid='00000000-0000-0000-0000-000000000001';createdAtUtc='2026-10-07T00:00:00Z';status='PASS';rows=$rows;subscenarioPassCount=$rows.Count;subscenarioFailCount=0;errors=@();observations=[pscustomobject]@{riderId='rider';horseId='mount';childEntryPreamble=$pre;chunk6cCasting=[pscustomobject]@{contract='native-mounted-casting-items-v1';rider='rider';mount='mount';mountBlueprint='e7aa96d15a45238438ae4cfb476f6bb9';mode=$(if($tb){'TB'}else{'RT'});mounted=$mounted;cases=@(Get-KmcChunk6cCastingCases);castTrace=$trace;costTrace=$trace;items=(New-CastingEnvelopeItems $rows);summonCleanup=@();final=$rows[0].evidence.after}}}
 $request=@{runId=$artifact.runId;scenario=$scenario;branch=$artifact.branch;commit=$artifact.commit;productVersion=$artifact.productVersion;dllSha256=$artifact.dllSha256;dllMvid=$artifact.dllMvid;evidenceRoot=(Join-Path $root $scenario)};[void][IO.Directory]::CreateDirectory($request.evidenceRoot);$path=Join-Path $request.evidenceRoot 'phase3d-horse-scenario-evidence.json';$manifest=@{artifacts=@(@{relativePath='phase3d-horse-scenario-evidence.json';kind='phase3d-horse-scenario-evidence'})}
 function Envelope($value){$value|ConvertTo-Json -Depth 60|Set-Content -LiteralPath $path -Encoding UTF8;Assert-KmcPhase3dHorseScenarioEvidence $request $manifest 'PASS'}
 Envelope $artifact;Check $true ('complete current envelope '+$scenario)
 foreach($mutation in @({param($a)$a.schemaVersion=43},{param($a)$a.commit=('c'*40)},{param($a)$a.rows=@($a.rows|Select-Object -Skip 1);$a.subscenarioPassCount--},{param($a)$a.observations.childEntryPreamble.parentEngine='KingmakerMountedCombat.Diagnostics.HorseCompanionUnmountedScenarioEngine'},{param($a)$a.observations.chunk6cCasting.castTrace.faults=1},{param($a)$a.rows[1].evidence.after.mount.standard=1.0},{param($a)$a.observations.chunk6cCasting.items[2].beforeCleanup.count=9},{param($a)$a.observations.chunk6cCasting.items[2].equipped.count=10},{param($a)$a.observations.chunk6cCasting.items[1].beforeCleanup.exactSlot=$false},{param($a)$a.observations.chunk6cCasting.items[1].stacked.item=59})){
  $changed=Copy-CastingFixture $artifact;& $mutation $changed;$rejected=$false;try{Envelope $changed}catch{$rejected=$true};Check $rejected ('full envelope retains substantive refusal '+$scenario)
 }
 Envelope $artifact
}
# Compound 6C envelopes: one registered request root, child case rows only, no self-named
# aggregate. The generic readers accept that structure and keep the individual rule intact.
function New-CompoundRow([string]$name,[string]$status='PASS'){
 [pscustomobject]@{name=$name;status=$status;assertionPassCount=$(if($status-ceq'PASS'){1}else{0});assertionFailCount=$(if($status-ceq'PASS'){0}else{1});errors=@($(if($status-ceq'PASS'){@()}else{@('synthetic native failure')}))}
}
function New-CompoundEnvelope([string]$scenario,$rows){
 $rows=@($rows);$pass=@($rows|Where-Object status -CEQ 'PASS').Count;$fail=$rows.Count-$pass
 [pscustomobject]@{scenario=$scenario;status=$(if($fail-eq0){'PASS'}else{'FAIL'});subscenarioTotal=$rows.Count;subscenarioPassCount=$pass;subscenarioFailCount=$fail;assertionPassCount=$pass;assertionFailCount=$fail;subscenarioResults=$rows}
}
function RejectEnvelope($envelope,[string]$expected,[string]$why){
 $message=$null;try{Assert-SubscenarioResults $envelope}catch{$message=$_.Exception.Message}
 Check ($null-ne$message-and$message.IndexOf($expected,[StringComparison]::Ordinal)-ge0) ($why+' ['+$message+']')
}
$frozenRoot='C:\Dev\KingmakerMountedCombatLab\runtime-evidence\c6c-casting199-k-mounted-rt'
$frozenGame=Read-KmcJson (Join-Path $frozenRoot 'runtime-game-result.json')
Check ($frozenGame.commit-ceq'85186ae3af3fac0a3aa466625188fa53f64d6f71'-and$frozenGame.status-ceq'FAIL'-and@($frozenGame.subscenarioResults).Count-eq15-and@($frozenGame.subscenarioResults|Where-Object name -CEQ 'chunk6c-casting-rt').Count-eq0) 'frozen preview.199 Stage 1 compound envelope is the immutable child-rows-only fixture'
foreach($leaf in @('Test-RuntimeGameResult.ps1','Test-RuntimeResult.ps1')){
 $tokens=$null;$parseErrors=$null
 $readerAst=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot ('runtime/'+$leaf)),[ref]$tokens,[ref]$parseErrors)
 if($parseErrors.Count-ne0){throw ('Reader has syntax errors: '+$leaf)}
 $definition=@($readerAst.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq'Assert-SubscenarioResults'},$true))
 if($definition.Count-ne1){throw ('Reader lacks one subscenario assertion: '+$leaf)}
 . ([scriptblock]::Create($definition[0].Extent.Text))
 Assert-SubscenarioResults $frozenGame;Check $true ('immutable compound FAIL envelope is read structurally by '+$leaf)
 foreach($scenario in @('chunk6c-casting-rt','chunk6c-casting-tb','chunk6c-casting-unmounted-rt','chunk6c-casting-unmounted-tb')){
  $rows=@(New-CompoundRow 'CM01-native-mammoth-fixture')+@(Get-KmcChunk6cCastingCases|ForEach-Object{New-CompoundRow $_})+@(New-CompoundRow 'CM01-native-mammoth-restoration')
  Assert-SubscenarioResults (New-CompoundEnvelope $scenario $rows);Check $true ('complete compound PASS envelope accepted by '+$leaf+' '+$scenario)
  $partial=@(New-CompoundRow 'CM01-native-mammoth-fixture')+@(New-CompoundRow 'C6C-quickened-self')+@(New-CompoundRow 'phase3d-horse-leaf-deadline' 'FAIL')+@(New-CompoundRow 'CM01-native-mammoth-restoration')
  Assert-SubscenarioResults (New-CompoundEnvelope $scenario $partial);Check $true ('partial compound FAIL envelope keeps its raw native failure in '+$leaf+' '+$scenario)
  RejectEnvelope (New-CompoundEnvelope $scenario ($rows+@(New-CompoundRow 'C6C-not-registered'))) 'unknown subscenario' ('compound envelope cannot carry an unregistered row '+$leaf+' '+$scenario)
  RejectEnvelope (New-CompoundEnvelope $scenario ($rows+@(New-CompoundRow 'C6C-quickened-self'))) 'duplicate subscenario' ('compound envelope cannot duplicate a case row '+$leaf+' '+$scenario)
  $mismatch=New-CompoundEnvelope $scenario $rows;$mismatch.subscenarioPassCount=0
  RejectEnvelope $mismatch 'totals do not match' ('compound envelope totals remain exact '+$leaf+' '+$scenario)
 }
 RejectEnvelope (New-CompoundEnvelope 'chunk6b-charge-core-rt' @(New-CompoundRow 'C6B-CHARGE-positive')) 'Individual runtime scenario did not report its own named result.' ('individual scenario still requires its own named row '+$leaf)
 Assert-SubscenarioResults (New-CompoundEnvelope 'chunk6b-charge-core-rt' @((New-CompoundRow 'C6B-CHARGE-positive'),(New-CompoundRow 'chunk6b-charge-core-rt')));Check $true ('individual scenario with its own named row accepted '+$leaf)
 RejectEnvelope (New-CompoundEnvelope 'persistence-p04-save' @(New-CompoundRow 'CM01-native-mammoth-fixture')) 'Individual runtime scenario did not report its own named result.' ('individual persistence scenario still requires its own named row '+$leaf)
}
# The corrected reader chain over the immutable Stage 1 artifacts: artifact bytes bound, the
# compound structure read, the dedicated 6C validator reached with the raw native FAIL, no PASS
# promotion and no byte change. The live request guard (current product version) is a launch-time
# gate and is deliberately not replayed against a frozen earlier candidate.
$frozenLeaves=@('runtime-request.json','runtime-game-result.json','runtime-result.json','runtime-artifacts.json','phase3d-horse-scenario-evidence.json')
$frozenHashes=@(foreach($leaf in $frozenLeaves){(Get-FileHash -LiteralPath (Join-Path $frozenRoot $leaf) -Algorithm SHA256).Hash})
$frozenRequest=Read-KmcJson (Join-Path $frozenRoot 'runtime-request.json')
Check ($frozenRequest.productVersion-ceq'0.1.0-chunk6c-preview.199'-and$frozenRequest.scenario-ceq'chunk6c-casting-rt'-and([IO.Path]::GetFullPath([string]$frozenRequest.evidenceRoot).TrimEnd('\')-ceq$frozenRoot)) 'frozen Stage 1 request binds the exact preview.199 compound scenario and evidence root'
Assert-KmcManifestJsonMembersUnique (Join-Path $frozenRoot 'runtime-game-result.json') 'frozen runtime game result'
Assert-KmcReadOnlyArtifactManifest $frozenRequest $frozenGame.evidenceManifestSha256 -GameResult
Check $true 'immutable Stage 1 artifact manifest still binds every artifact byte'
$frozenManifest=Read-KmcJson (Join-Path $frozenRoot 'runtime-artifacts.json')
Assert-KmcPhase3dHorseScenarioEvidence -Request $frozenRequest -Manifest $frozenManifest -Status ([string]$frozenGame.status) -SubscenarioResults $frozenGame.subscenarioResults
Check ($frozenGame.status-ceq'FAIL'-and@($frozenGame.errors|Where-Object {$_-like'*leaf exceeded 30 seconds*'}).Count-ge1) 'corrected reader chain reaches the dedicated 6C validator with the raw native leaf-deadline FAIL preserved'
$promoted=$false;try{Assert-KmcPhase3dHorseScenarioEvidence -Request $frozenRequest -Manifest $frozenManifest -Status 'PASS' -SubscenarioResults $frozenGame.subscenarioResults;$promoted=$true}catch{}
Check (-not$promoted) 'dedicated 6C validator never promotes the immutable native fixture timeout to PASS'
$afterHashes=@(foreach($leaf in $frozenLeaves){(Get-FileHash -LiteralPath (Join-Path $frozenRoot $leaf) -Algorithm SHA256).Hash})
Check (($frozenHashes-join',')-ceq($afterHashes-join',')) 'immutable Stage 1 artifacts are byte-identical after re-evaluation'
Write-Host ('TOTAL PASS='+$passed+' FAIL=0')