. (Join-Path $PSScriptRoot 'NativeAutoUseControlEvidence.ps1')
. (Join-Path $PSScriptRoot 'NativeTerminalBridgeEvidence.ps1')
function Assert-KmcAutoUseCase($E){
 function Need([bool]$b,[string]$why){if(-not$b){throw ('Auto-use case: '+$why)}}
 function I($v){Need ($v-is[int]-or$v-is[long]) 'Integer absent';[long]$v}
 function B($v){Need ($v-is[bool]) 'Boolean absent';[bool]$v}
 function Same($a,$b,[string]$why){Need ($null-ne$a-and$null-ne$b-and($a|ConvertTo-Json -Depth 100 -Compress)-ceq($b|ConvertTo-Json -Depth 100 -Compress)) $why}
 function Clock($a,$b){foreach($k in @('frame','gameTicks','allocationSequence')){Need ((I $a.$k)-eq(I $b.$k)) 'Window clock/sequence differs'}}
 function StableState($state){$s=$state|ConvertTo-Json -Depth 100|ConvertFrom-Json;$s.geometry.PSObject.Properties.Remove('seconds');$s}
 function Resources($native,$boundary){foreach($role in @('rider','mount')){Same $native.$role $boundary.($role+'Resources') 'Resource endpoint differs'}}
 function Slice($window,$trace){Assert-KmcNativePassiveResources $window;Same @($window.observerHooks) @($trace.observerHooks) 'Window hooks differ from full trace';$slice=@($trace.events|Where-Object {$_.sequence-gt$window.before.allocationSequence-and$_.sequence-le$window.after.allocationSequence});Same @($window.events) $slice 'Window differs from complete native trace'}
 $kind=$E.case;Need ($E.contract-ceq'independent-fresh-rt-native-auto-use-control-window'-and$kind-cin@('mount','dismount')-and$E.scenario-ceq('chunk6a-auto-use-'+$kind+'-rt')) 'Case identity differs'
 $controlEvidence=$E.input;Assert-KmcAutoUseControl $controlEvidence;Need (B $controlEvidence.pass) 'Input producer did not PASS'
 $first=$E.beforeSetup;$before=$E.beforeInput;$after=$E.afterInput;$restored=$E.afterRestoration
 $rider=$controlEvidence.riderId;$mount=$controlEvidence.mountId;$relation=if($kind-ceq'mount'){'Unmounted'}else{'Mounted'}
 Need ($controlEvidence.abilityGuid-ceq$(if($kind-ceq'mount'){'f053faad986631688defa003cd7bda0e'}else{'3af2b81f4d72bbb30501fa730fcdf36e'})) 'Case/control mismatch'
 foreach($b in @($first,$before,$after,$restored)){
  Need (-not(B $b.turnBased)-and(B $b.pairIdle)-and(B $b.targetInCombat)-and(B $b.hostileTarget)-and$b.state.relationshipState-ceq$relation) 'Case allocation/target illegal'
  foreach($k in @('riderObject','mountObject','targetObject')){Need ((I $b.$k)-eq(I $controlEvidence.$k)) 'Case/input object differs'};Need ($b.targetId-ceq$controlEvidence.targetId) 'Hostile target changed'
  foreach($k in @('ledger','generation','selectedIds')){Same $b.state.$k $controlEvidence.before.state.$k 'Case relationship/selection changed'}
  foreach($role in @('rider','mount')){Need ((B $b.($role+'Resources').inCombat)-and$b.($role+'Resources').actor-ceq$(if($role-ceq'rider'){$rider}else{$mount})) 'Actor allocation differs'
   foreach($k in @('actor','standard','move','swift','reactions','reactionCooldown','initiativeCooldown','initiativeOrder','reactionsPerRound')){Same $b.($role+'Resources').$k $b.state.$role.$k 'Case resource copies differ'}
   Need ((I $b.($role+'Resources').grantSequence)-eq(I $b.state.$role.nativePrepareCount)) 'Case preparation copies differ'
  }
 }
 Need ((B $first.paused)-eq(B $E.originalPause)-and(B $before.paused)-and(B $after.paused)-and(B $restored.paused)-eq(B $E.originalPause)-and(B $E.pauseRestored)) 'Pause restoration differs'
 Clock $before $controlEvidence.before;Clock $after $controlEvidence.afterRestoration;Clock $after $restored
 Same (StableState $before.state) (StableState $controlEvidence.before.state) 'Case/input baseline state differs'
 Same (StableState $after.state) (StableState $controlEvidence.afterRestoration.state) 'Case/input terminal state differs'
 $trace=$E.allocationTrace;Need ((I $trace.dropped)-eq0-and(I $trace.observationErrors)-eq0-and$trace.events-is[array]) 'Complete allocation trace absent'
 $sequence=0L;foreach($event in $trace.events){Need ((I $event.sequence)-eq(++$sequence)) 'Full trace gap'};Need ($sequence-eq(I $restored.allocationSequence)) 'Trace endpoint differs'
 $setup=$E.setupResources;Need ($setup.riderId-ceq$rider-and$setup.mountId-ceq$mount-and-not(B $setup.turnBased)) 'Setup pair/mode differs'
 Slice $setup $trace;Clock $setup.before $first;Clock $setup.after $before;Resources $setup.before $first;Resources $setup.after $before
 $resource=$controlEvidence.resources;Slice $resource $trace;Clock $resource.before $before;Clock $resource.after $after;Resources $resource.before $before;Resources $resource.after $after
 foreach($role in @('rider','mount')){Same $restored.($role+'Resources') $after.($role+'Resources') 'Restoration changed resources';Same $restored.state.$role $after.state.$role 'Restoration changed causal resources'}
 $ui=$E.ui;Need ($ui.contract-ceq'native-action-bar-group-input-and-exact-restoration'-and$ui.casterId-ceq$rider-and(B $ui.restored)-and(I $ui.managerObject)-eq(I $controlEvidence.slotRegistration.managerObject)) 'UI registration/restoration differs'
 Need ($ui.before-is[array]-and$ui.after-is[array]-and$ui.opened-is[array]-and$ui.matchingSlotObjects-is[array]) 'UI observations absent';Same @($ui.before) @($ui.after) 'UI groups not restored'
 Need ($ui.matchingSlotObjects.Count-eq1-and(I $ui.matchingSlotObjects[0])-eq(I $controlEvidence.slotRegistration.slotObject)) 'Slot lookup differs'
 Need ((I $controlEvidence.slotRegistration.ownerObject)-eq(I $ui.$(if($controlEvidence.slotRegistration.route-ceq'group'){'abilityGroupObject'}else{'mainOwnerObject'}))) 'Live slot owner differs'
 Same @($ui.current) @($ui.opened) 'Opened group changed before input'
 Need ($ui.before.Count-eq$ui.opened.Count) 'Native groups changed'
 for($g=0;$g-lt$ui.before.Count;$g++){foreach($k in @('index','groupObject','type')){Same $ui.before[$g].$k $ui.opened[$g].$k 'Native group registration changed'}}
 $groups=@($ui.opened|Where-Object {$_.groupObject-eq$ui.abilityGroupObject});Need ($groups.Count-eq1-and(B $groups[0].toggle)-and$groups[0].type-ceq'ActivatableAbility') 'Native ability group not open'
 Need ($E.ordinaryCandidates-is[array]-and$E.ordinaryCandidates.Count-gt0-and$E.ordinaryCandidates.Count-le256) 'Ordinary inventory absent/bounded'
 $ordinary=@($E.ordinaryCandidates|Where-Object {$_.object-eq$controlEvidence.ordinaryAbilityObject});Need ($ordinary.Count-eq1-and$ordinary[0].guid-ceq$controlEvidence.ordinaryAbilityGuid-and(B $ordinary[0].suitable)) 'Ordinary control not owned by inventory'
 if($kind-ceq'mount'){Need ($null-eq$E.mountProof) 'Mount auto-use followed another combat Mount'}else{
  $p=$E.mountProof;Need ((B $p.pass)-and(B $p.resourceWindow.pass)-and(B $p.resourceWindow.reactionResources.pass)) 'Dismount lacks its own positive Mount'
  Assert-KmcNativeTerminalBridge $p $first $E.mountTerminalBridge $false 'Mounted';Slice $E.mountTerminalBridge $trace
 }
}
function Assert-KmcAutoUseEnvelope($Request,$Artifact){
 $e=$Artifact.observations.chunk6aAutoUse
 if($e.scenario-cne$Request.scenario-or$Artifact.scenario-cne$Request.scenario){throw 'Auto-use request differs'}
 Assert-KmcAutoUseCase $e
 $trace=$Artifact.observations.actorAllocationTrace
 if($trace.dropped-ne0-or$trace.observationErrors-ne0){throw 'Auto-use complete trace absent'}
 Assert-KmcSameEvidence @($e.allocationTrace.observerHooks) @($trace.observerHooks) 'auto-use full trace hooks'
 Assert-KmcSameEvidence @($e.allocationTrace.events) @($trace.events|Where-Object {$_.sequence-le$e.afterRestoration.allocationSequence}) 'auto-use full trace prefix'
 foreach($window in @($e.setupResources,$e.input.resources)){
  Assert-KmcSameEvidence @($window.observerHooks) @($trace.observerHooks) 'auto-use native observer hooks'
  Assert-KmcSameEvidence @($window.events) @($trace.events|Where-Object {$_.sequence-gt$window.before.allocationSequence-and$_.sequence-le$window.after.allocationSequence}) 'auto-use exact window full trace'
 }
 foreach($proof in @($Artifact.observations.chunk6aCommandProofs)){
  $terminal=@($proof.samples|Where-Object boundary -CEQ 'terminal');if($terminal.Count-ne1){throw 'Auto-use preceding command terminal absent'}
  Assert-KmcSameEvidence @($proof.resourceWindow.events) @($trace.events|Where-Object {$_.sequence-gt$proof.preClick.allocationSequence-and$_.sequence-le$terminal[0].allocationSequence}) 'auto-use preceding exact command trace'
 }
 if($e.case-ceq'dismount'){
  $p=Get-KmcExactWindow $Artifact 'positive-mount';Assert-KmcSameEvidence $e.mountProof $p 'auto-use owned positive Mount'
  Assert-KmcRelationshipCommandProof $p $true $false 0 $true 0 $true
  Assert-KmcSameEvidence @($e.mountTerminalBridge.observerHooks) @($trace.observerHooks) 'auto-use bridge observers'
 }elseif(@($Artifact.observations.chunk6aCommandProofs|Where-Object window -CEQ 'positive-mount').Count-ne0){throw 'Mount auto-use inherited a positive Mount'}
 $row=@($Artifact.rows|Where-Object name -CEQ 'CM06-ai-auto-use');if($row.Count-ne1-or$row[0].status-cne'PASS'){throw 'Auto-use mandatory row absent/failed'}
 Assert-KmcSameEvidence $row[0].evidence $e 'auto-use row'
 foreach($name in @('CM02-adoption-plan-invalidated','CM02-adoption-compensation-releases','CM02-geometry-change','CM02-obstruction','CM05-combat-dismount-accepted')){if(@($Artifact.rows|Where-Object name -CEQ $name).Count-ne0){throw 'Auto-use inherited another scenario'}}
}
