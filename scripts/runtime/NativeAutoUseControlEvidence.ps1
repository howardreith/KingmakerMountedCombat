Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'NativePassiveResourceEvidence.ps1')
function Assert-KmcAutoUseControl($E){
 function Need([bool]$Pass,[string]$Why){if(-not$Pass){throw ('Native auto-use: '+$Why)}}
 function I($v){Need ($v-is[int]-or$v-is[long]) 'Integer absent';[long]$v}
 function B($v){Need ($v-is[bool]) 'Boolean absent';[bool]$v}
 function Arr($v){Need ($v-is[array]) 'Array absent';,$v}
 function Same($a,$b,[string]$why){Need (($a|ConvertTo-Json -Depth 100 -Compress)-ceq($b|ConvertTo-Json -Depth 100 -Compress)) $why}
 function N($v){Need ($v-is[int]-or$v-is[long]-or$v-is[double]-or$v-is[decimal]-or$v-is[single]) 'Number absent';$n=[double]$v;Need (-not[double]::IsNaN($n)-and-not[double]::IsInfinity($n)) 'Nonfinite number';$n}
 function StableState($state){$s=$state|ConvertTo-Json -Depth 100|ConvertFrom-Json;$s.geometry.PSObject.Properties.Remove('seconds');$s}
 function Unadmitted($c){Need ((I $c.object)-ne0-and$null-eq$c.executorId-and$c.targetId-ceq$target-and-not(B $c.started)-and-not(B $c.acted)-and-not(B $c.finished)) 'Command admitted or target changed'}
 $mountGuid='f053faad986631688defa003cd7bda0e';$dismountGuid='3af2b81f4d72bbb30501fa730fcdf36e';$mvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'
 Need ($E.contract-ceq'native-ui-auto-use-and-ai-revalidation-of-explicit-stale-reference'-and@($E.PSObject.Properties|Where-Object {$_.Name-ceq'failure'-and$null-ne$_.Value}).Count-eq0-and(Arr $E.errors).Count-eq0) 'Contract/failure differs'
 $rider=$E.riderId;$mount=$E.mountId;$target=$E.targetId;$guid=$E.abilityGuid
 Need (-not[string]::IsNullOrWhiteSpace($rider)-and-not[string]::IsNullOrWhiteSpace($mount)-and-not[string]::IsNullOrWhiteSpace($target)-and@(@($rider,$mount,$target)|Select-Object -Unique).Count-eq3-and$guid-cin@($mountGuid,$dismountGuid)) 'Pair/control differs'
 foreach($k in @('riderObject','mountObject','targetObject','mechanicObject','aiObject','abilityObject','ordinaryAbilityObject','contextObject')){Need ((I $E.$k)-ne0) ('Object absent '+$k)}
 Need ((I $E.riderObject)-ne(I $E.mountObject)-and(I $E.ordinaryAbilityObject)-ne(I $E.abilityObject)) 'Actor/ability aliases'
 $ordinary=$E.ordinaryAbilityGuid
 Need (-not[string]::IsNullOrWhiteSpace($ordinary)-and$ordinary-cnotin@($mountGuid,$dismountGuid)-and(B $E.ordinarySuitable)-and-not(B $E.suitable)-and(B $E.staleReferenceFixtureDeclared)-and(B $E.constructorObserverExecuted)) 'Ordinary control/refusal absent'
 $reg=$E.slotRegistration
 Need ($reg.casterId-ceq$rider-and(I $reg.occurrences)-eq1-and(B $reg.active)-and$reg.route-cin@('main','group')) 'Live slot registration differs'
 foreach($k in @('managerObject','ownerObject','slotObject')){Need ((I $reg.$k)-ne0) 'Registered object absent'}
 $before=$E.before;$frame=I $before.frame;$ticks=I $before.gameTicks;$sequence=I $before.allocationSequence;$original=I $before.autoUseObject
 Need ($original-ne(I $E.abilityObject)-and$before.autoUseGuid-cnotin@($mountGuid,$dismountGuid)) 'Preexisting relationship auto-use'
 foreach($name in @('before','afterUi','afterLease','afterAi','afterRestoration')){
  $s=$E.$name
  Need ((I $s.frame)-eq$frame-and(I $s.gameTicks)-eq$ticks-and(I $s.allocationSequence)-eq$sequence-and(B $s.paused)-and-not(B $s.turnBased)-and(I $s.selectedAbilityObject)-eq0-and(B $s.riderCommandsEmpty)-and(B $s.mountCommandsEmpty)) ('Synchronous paused command-free window differs '+$name)
  foreach($k in @('shellCount','processBindings','controls')){Same $s.$k $before.$k ('Transition/selection/resources changed '+$name+' '+$k)}
  Same (StableState $s.state) (StableState $before.state) ('Causal state changed '+$name)
  Need ((I $s.state.geometry.frame)-eq$frame-and(N $s.state.geometry.seconds)-ge(N $before.state.geometry.seconds)) 'Geometry observation clock differs'
  $leased=$name-cin@('afterLease','afterAi');$expected=if($leased){I $E.abilityObject}else{$original}
  Need ((I $s.autoUseObject)-eq$expected) 'Lease/reference restoration differs'
  if($leased){Need ($s.autoUseGuid-ceq$guid) 'Leased blueprint differs'}else{Same $s.autoUseGuid $before.autoUseGuid 'Restored blueprint differs'}
 }
 Need ((Arr $before.state.selectedIds).Count-eq1-and$before.state.selectedIds[0]-ceq$rider-and$before.state.relationshipState-ceq$(if($guid-ceq$mountGuid){'Unmounted'}else{'Mounted'})) 'Selected rider/relationship differs'
 $resource=$E.resources;Assert-KmcNativePassiveResources $resource
 Need (-not(B $resource.turnBased)-and$resource.riderId-ceq$rider-and$resource.mountId-ceq$mount-and(Arr $resource.events).Count-eq0) 'Passive window admitted work'
 foreach($end in @('before','after')){
  Need ((I $resource.$end.frame)-eq$frame-and(I $resource.$end.gameTicks)-eq$ticks-and(I $resource.$end.allocationSequence)-eq$sequence) 'Passive clock differs'
  foreach($role in @('rider','mount')){
   $r=$resource.$end.$role;Need ((B $r.inCombat)-and(I $r.actorObject)-eq(I $E.($role+'Object'))) 'Passive actor differs'
   foreach($k in @('actor','standard','move','swift','reactions','reactionCooldown','initiativeCooldown','initiativeOrder','reactionsPerRound')){Same $r.$k $before.state.$role.$k ('Passive resource copy differs '+$role+' '+$k)}
   Need ((I $r.grantSequence)-eq(I $before.state.$role.nativePrepareCount)) 'Passive preparation differs'
  }
 }
 $specs=@(@('06002B30','Kingmaker.UnitLogic.Abilities.AbilityData.get_IsSuitableForAutoUse','SuitableBefore','SuitableAfter'),@('06002F5F','Kingmaker.UI.UnitSettings.MechanicActionBarSlotAbility.OnAutoUseToggle','ToggleBefore','ToggleAfter'),@('06008370','Kingmaker.EntitySystem.Entities.UnitEntityData.GetAvailableAutoUseAbility','AvailableBefore','AvailableAfter'),@('06009434','Kingmaker.Controllers.Brain.Blueprints.BlueprintAiAttack.CreateCommand','AiBefore','AiAfter'),@('06002726','Kingmaker.UnitLogic.Commands.UnitUseAbility..ctor',$null,'Constructed'),@('06002727','Kingmaker.UnitLogic.Commands.UnitUseAbility..ctor',$null,'Constructed'))
 $hooks=Arr $E.observerHooks;Need ($hooks.Count-eq6) 'Hook count differs'
 foreach($spec in $specs){$h=@($hooks|Where-Object token -CEQ $spec[0]);Need ($h.Count-eq1-and$h[0].method-ceq$spec[1]-and$h[0].prefix-ceq$spec[2]-and$h[0].postfix-ceq$spec[3]-and$h[0].moduleMvid-ceq$mvid) 'Exact native hook absent'}
 $events=Arr $E.events;Need ($events.Count-gt0-and$events.Count-le128) 'Events absent/bound exceeded'
 $ordinals=[Collections.Generic.List[long]]::new();$phases=@('constructor-control','baseline-ai','ui-toggle','stale-reference');$lastPhase=0;$lastEnter=0L
 foreach($c in $events){
  $phase=[Array]::IndexOf($phases,$c.phase);$enter=I $c.enterOrdinal;$exit=I $c.exitOrdinal
  Need ($phase-ge$lastPhase-and$enter-gt$lastEnter-and$exit-gt$enter-and(B $c.complete)-and(I $c.frame)-eq$frame-and(I $c.exitFrame)-eq$frame-and(I $c.gameTicks)-eq$ticks-and(I $c.exitGameTicks)-eq$ticks) 'Callback order/clock/completion differs'
  $lastPhase=$phase;$lastEnter=$enter;$ordinals.Add($enter);$ordinals.Add($exit)
  $kind=$c.kind;Need ($kind-cin@('suitability','toggle','available','ai-create')) 'Unknown callback'
  if($kind-ceq'ai-create'){
   Need ((I $c.receiverObject)-eq(I $E.aiObject)-and(I $c.contextObject)-eq(I $E.contextObject)-and(I $c.targetObject)-eq(I $E.targetObject)-and$c.targetId-ceq$target-and$c.casterId-ceq$rider-and(I $c.abilityObject)-eq0) 'AI receiver/context/target differs'
  }else{
   Need ($c.casterId-ceq$rider-or($kind-ceq'available'-and(I $c.abilityObject)-eq0)) 'Callback caster differs'
   $receiver=if($kind-ceq'suitability'){I $c.abilityObject}elseif($kind-ceq'toggle'){I $E.mechanicObject}else{I $E.riderObject}
   Need ((I $c.receiverObject)-eq$receiver) 'Native receiver differs'
  }
  foreach($parent in @($events|Where-Object {$_.enterOrdinal-lt$enter-and$_.exitOrdinal-gt$enter})){Need ($exit-lt(I $parent.exitOrdinal)-and$parent.phase-ceq$c.phase) 'Crossed callback nesting'}
 }
 function Calls([string]$Phase,[string]$Kind){,@($events|Where-Object {$_.phase-ceq$Phase-and$_.kind-ceq$Kind})}
 function Nested($child,$parent){Need ((I $child.enterOrdinal)-gt(I $parent.enterOrdinal)-and(I $child.exitOrdinal)-lt(I $parent.exitOrdinal)) 'Missing nested native revalidation'}
 $control=Calls 'constructor-control' 'suitability'
 Need ($control.Count-eq1-and@($events|Where-Object phase -CEQ 'constructor-control').Count-eq1-and(I $control[0].abilityObject)-eq(I $E.ordinaryAbilityObject)-and$control[0].abilityGuid-ceq$ordinary-and(B $control[0].result)) 'Ordinary suitability control absent'
 foreach($phase in @('baseline-ai','stale-reference')){
  $ai=Calls $phase 'ai-create';Need ($ai.Count-eq1) 'Native AI call missing';$key=if($phase-ceq'baseline-ai'){'baselineAi'}else{'aiWithStaleReference'};Same $ai[0].result $E.$key 'AI result differs'
  $available=Calls $phase 'available';Need ($available.Count-eq$(if($phase-ceq'baseline-ai'){1}else{2})) 'Native availability count differs'
  Need (@($available|Where-Object {$_.enterOrdinal-gt$ai[0].enterOrdinal-and$_.exitOrdinal-lt$ai[0].exitOrdinal}).Count-eq1) 'AI did not revalidate availability'
  if($phase-ceq'stale-reference'){foreach($call in $available){
   Need ((I $call.abilityObject)-eq(I $E.abilityObject)-and$call.abilityGuid-ceq$guid-and(I $call.result.object)-eq0-and$null-eq$call.result.guid) 'Stale reference remained available'
   $suitable=@((Calls $phase 'suitability')|Where-Object {$_.enterOrdinal-gt$call.enterOrdinal-and$_.exitOrdinal-lt$call.exitOrdinal});Need ($suitable.Count-eq1) 'Availability did not query eligibility'
  }}
 }
 $baseAvailable=(Calls 'baseline-ai' 'available')[0];$baseSuitable=Calls 'baseline-ai' 'suitability'
 Need ((I $baseAvailable.abilityObject)-eq$original-and$baseAvailable.abilityGuid-ceq$before.autoUseGuid) 'Baseline availability input differs'
 Need ($baseSuitable.Count-eq$(if($original-eq0){0}else{1})-and@($events|Where-Object phase -CEQ 'baseline-ai').Count-eq2+$baseSuitable.Count) 'Baseline callback chain differs'
 $availableOriginal=I $baseAvailable.result.object;Need ($availableOriginal-eq0-or$availableOriginal-eq$original) 'Baseline availability result differs'
 Need ($baseAvailable.result.guid-ceq$(if($availableOriginal-eq0){$null}else{$before.autoUseGuid})) 'Baseline available blueprint differs'
 if($original-ne0){Nested $baseSuitable[0] $baseAvailable;Need ((I $baseSuitable[0].abilityObject)-eq$original-and$baseSuitable[0].abilityGuid-ceq$before.autoUseGuid) 'Baseline eligibility identity differs';if($availableOriginal-ne0){Need (B $baseSuitable[0].result) 'Baseline ignored eligibility'}else{$null=B $baseSuitable[0].result}}
 $toggle=Calls 'ui-toggle' 'toggle';$ui=Calls 'ui-toggle' 'suitability'
 Need ($toggle.Count-eq1-and$ui.Count-eq2-and@($events|Where-Object phase -CEQ 'ui-toggle').Count-eq3-and$null-eq$toggle[0].result) 'UI toggle callbacks differ'
 Nested $ui[1] $toggle[0];Need ((I $ui[0].exitOrdinal)-lt(I $toggle[0].enterOrdinal)) 'Explicit suitability query did not precede UI'
 Need ((Calls 'stale-reference' 'suitability').Count-eq2-and@($events|Where-Object phase -CEQ 'stale-reference').Count-eq5) 'Stale callback chain differs'
 foreach($c in @($events|Where-Object {$_.phase-cin@('ui-toggle','stale-reference')-and$_.kind-cin@('suitability','toggle')})){Need ((I $c.abilityObject)-eq(I $E.abilityObject)-and$c.abilityGuid-ceq$guid-and($c.kind-cne'suitability'-or-not(B $c.result))) 'Relationship eligibility not refused'}
 Need ((I $E.availableObject)-eq0-and$null-eq$E.availableGuid) 'Explicit stale availability differs'
 $ctor=$E.constructorControl;Unadmitted $ctor;Need ($ctor.type-ceq'Kingmaker.UnitLogic.Commands.UnitUseAbility'-and$ctor.abilityGuid-ceq$ordinary) 'Constructor control differs'
 $baseline=$E.baselineAi;$stale=$E.aiWithStaleReference;Unadmitted $baseline;Unadmitted $stale
 Need ((I $baseline.object)-ne(I $stale.object)-and(I $ctor.object)-ne(I $stale.object)-and(I $ctor.object)-ne(I $baseline.object)) 'Commands alias'
 Need ($stale.type-ceq'Kingmaker.UnitLogic.Commands.UnitAttack'-and$null-eq$stale.abilityGuid) 'AI ordinary attack fallback absent'
 $cast=$baseline.type-ceq'Kingmaker.UnitLogic.Commands.UnitUseAbility';Need ($cast-or$baseline.type-ceq'Kingmaker.UnitLogic.Commands.UnitAttack') 'Unexpected baseline command'
 if($cast){Need ($original-ne0-and$baseline.abilityGuid-ceq$before.autoUseGuid-and$baseline.abilityGuid-cnotin@($mountGuid,$dismountGuid)) 'Baseline cast differs'}else{Need ($null-eq$baseline.abilityGuid) 'Ordinary baseline acquired ability'}
 $constructed=Arr $E.constructedAbilityCommands;Need ($constructed.Count-eq$(if($cast){2}else{1})) 'Unexpected ability command constructed'
 Same $constructed[0].command $ctor 'Constructor control differs';Need ($constructed[0].phase-ceq'constructor-control') 'Constructor phase differs'
 if($cast){Same $constructed[1].command $baseline 'Baseline construction differs';Need ($constructed[1].phase-ceq'baseline-ai') 'Baseline construction phase differs'}
 $callbacks=Arr $E.constructorCallbacks;Need ($callbacks.Count-eq$constructed.Count*2) 'Constructor execution control incomplete'
 foreach($c in $constructed){
  $pair=@($callbacks|Where-Object {$_.command.object-eq$c.command.object});Need ($pair.Count-eq2-and$pair[0].token-ceq'06002727'-and$pair[1].token-ceq'06002726'-and(I $pair[0].ordinal)-lt(I $pair[1].ordinal)) 'Exact constructor chain absent'
  foreach($cb in $pair){Same $cb.command $c.command 'Constructor identity differs';Need ($cb.phase-ceq$c.phase-and(I $cb.frame)-eq$frame-and(I $cb.gameTicks)-eq$ticks) 'Constructor phase/clock differs';$ordinals.Add((I $cb.ordinal))}
 }
 $sorted=@($ordinals|Sort-Object);for($n=0;$n-lt$sorted.Count;$n++){Need ($sorted[$n]-eq$n+1L) 'Observer sequence gap/duplicate'}
}
