# Read-only substantive acceptance for the bounded stock 6C baseline.
# Native C# emits observations; this module owns their interpretation.
Set-StrictMode -Version Latest
function Get-KmcChunk6cCastingCases {
 @('C6C-quickened-self','C6C-standard-self','C6C-standard-friendly','C6C-standard-hostile',
   'C6C-prepared-interrupt-after','C6C-invalid-target','C6C-cancel-before','C6C-interrupt-before',
   'C6C-potion-self','C6C-scroll-friendly','C6C-full-round','C6C-movement-policy','C6C-rider-incapacity','C6C-mount-incapacity','C6C-under-threat')
}
function Assert-KmcChunk6cCastingRow($Row,[string]$Rider,[string]$Mount,[bool]$Tb,[bool]$Mounted) {
 function Need([bool]$ok,[string]$why){if(-not$ok){throw ('6C '+$Row.name+': '+$why)}}
 function Number($v){if($v-isnot[int]-and$v-isnot[long]-and$v-isnot[single]-and$v-isnot[double]-and$v-isnot[decimal]){throw '6C number missing'};$x=[double]$v;if([double]::IsNaN($x)-or[double]::IsInfinity($x)){throw '6C nonfinite number'};$x}
 function Close($a,$b){[Math]::Abs((Number $a)-(Number $b))-le.001}
 $e=$Row.evidence;$before=$e.before;$after=$e.after
 Need ($Row.status-ceq'PASS'-and$e.case-ceq$Row.name) 'row structure/status differs'
 $life=$Row.name-cin@('C6C-rider-incapacity','C6C-mount-incapacity')
 $motion=$Row.name-ceq'C6C-movement-policy'
 if($Row.name-ceq'C6C-full-round'-and$e.PSObject.Properties.Name-ccontains'availability') {
  Need ($e.availability-ceq'none-native'-and$e.inputCount-eq0-and@($e.conversions|Where-Object {$_.ability.availableForCast-eq$true}).Count-eq0) 'available native full-round case was dropped'
  Need (@($e.events).Count-eq0-and@($e.costEvents).Count-eq0-and$after.rider.actor-ceq$Rider-and$after.mount.actor-ceq$Mount-and$after.riderCommandsEmpty-eq$true-and$after.mountCommandsEmpty-eq$true-and$after.nativeAbilitiesPending-eq$false-and$after.projectilesPending-eq$false) 'unavailable full-round observation spent or left residue'
  return
 }

 foreach($state in @($before,$e.afterInput,$after)) {
  Need ($state.rider.actor-ceq$Rider-and$state.mount.actor-ceq$Mount) 'actor identity differs'
  if(-not$life){Need ($state.generation-eq$before.generation-and$state.relationship-ceq$(if($Mounted){'Mounted'}else{'Unmounted'})) 'relationship generation changed during settled baseline'}
 }
 Need ($before.ability.caster-ceq$Rider) 'native caster/item user is not the rider'
 $expectedBlueprint=switch -CaseSensitive($Row.name){'C6C-standard-hostile'{'9f10909f0be1f5141bf1c102041f93d9'} 'C6C-prepared-interrupt-after'{'5590652e1c2225c4ca30c4a699ab3649'} 'C6C-potion-self'{'5590652e1c2225c4ca30c4a699ab3649'} 'C6C-scroll-friendly'{'5590652e1c2225c4ca30c4a699ab3649'} 'C6C-full-round'{'c6147854641924442a3bb736080cfeb6'} default{'c3a8f31778c3980498d8f00c980be5f5'}}
 Need ($before.ability.blueprint-ceq$expectedBlueprint) 'native fixture ability substituted'
 Need ($after.riderCommandsEmpty-eq$true-and$after.mountCommandsEmpty-eq$true-and$after.processesSettled-eq$true-and$after.nativeAbilitiesPending-eq$false-and$after.projectilesPending-eq$false-and$after.activePairCommand-eq$false) 'command/process/pair residue remains'
 Need ($e.selectionAfter-is[Array]-and$e.selectionAfter.Count-eq1-and$e.selectionAfter[0]-ceq$Rider) 'normal input lacks exact single rider selection'
 $events=@($e.events);$costs=@($e.costEvents)
 Need (@($events|Where-Object actor -CEQ $Mount).Count-eq0) 'mount cast/spend/delivery observed'
 $moveOwner=if($Mounted){$Mount}else{$Rider}
 $carrier=if($motion){$e.boundary.costCarrier}else{0}
 Need (@($costs|Where-Object {$_.state.actor-ceq$Mount-and$_.boundary-cin@('cost-before','cost-after','actor-cost-before','actor-cost-after')-and(-not$motion-or$_.command-ne$carrier)}).Count-eq0) 'mount charged for rider casting'
 Need (@($costs|Where-Object {$_.state.actor-cin@($Rider,$Mount)-and$_.boundary-cin@('prepare-before','prepare-after','clear-before','clear-after')}).Count-eq0) 'pair preparation replayed in cast window'
 foreach($field in @('standard','move','swift')) {
  if($Tb-and-not$motion){Need (Close $before.mount.$field $after.mount.$field) 'mount native debt changed during rider action'}
  elseif(-not$motion){Need ((Number $after.mount.$field)-le(Number $before.mount.$field)+.001) 'mount native debt increased during rider action'}
 }
 $costBefore=@($costs|Where-Object {$_.boundary-ceq'cost-before'-and$_.state.actor-ceq$Rider})
 $costAfter=@($costs|Where-Object {$_.boundary-ceq'cost-after'-and$_.state.actor-ceq$Rider})
 $cast=@($events|Where-Object {$_.kind-ceq'cast-after'-and$_.ability.blueprint-ceq$before.ability.blueprint-and$_.actor-ceq$Rider})
 $refusal=$Row.name-cin@('C6C-invalid-target','C6C-cancel-before','C6C-interrupt-before','C6C-rider-incapacity')-or($Row.name-ceq'C6C-mount-incapacity'-and$cast.Count-eq0)
 $nativeConcentrationFailure=$Row.name-ceq'C6C-under-threat'-and@($events|Where-Object {$_.kind-ceq'concentration-rule-after'-and$_.success-eq$false}).Count-gt0
 if($refusal) {
  Need ($cast.Count-eq0-and$costBefore.Count-eq0-and$costAfter.Count-eq0) 'precommit refusal/cancellation spent or delivered'
  if($Row.name-ceq'C6C-invalid-target'){Need ($e.canTarget-eq$false-and$e.admittedShellCount-eq0-and$e.inputCount-eq1) 'invalid target was not genuinely refused'}
  if($Row.name-ceq'C6C-cancel-before'){Need ($e.inputCount-eq0-and$e.cancelledSelection-eq$true) 'selection cancellation sent a cast'}
  if($Row.name-ceq'C6C-interrupt-before'){Need ($e.interrupted-eq$true-and$e.interruptionBefore.shell.started-eq$true-and$e.interruptionBefore.shell.acted-eq$false) 'exact running precommit shell was not interrupted'}
 } elseif(-not$nativeConcentrationFailure) {
  Need ($e.inputCount-eq1-and$e.clicked-eq$true-and$e.canTarget-eq$true-and($e.resolvedTarget-ceq$e.target-or($Row.name-ceq'C6C-full-round'-and($e.resolvedPoint|ConvertTo-Json -Compress)-ceq($e.targetPoint|ConvertTo-Json -Compress)))-and$e.admittedShellCount-eq1) 'normal native input/target/shell not admitted exactly once'
  Need ($cast.Count-eq1-and$cast[0].process-ne0-and$cast[0].spellFailed-eq$false-and$cast[0].arcaneFailed-eq$false) 'native source cast/process failed or duplicated'
  $root=$e.costShell;$castRoot=$e.afterInput.shell.identity
  $beforeCost=@($costBefore|Where-Object command -EQ $root);$afterCost=@($costAfter|Where-Object command -EQ $root)
  Need ($beforeCost.Count-eq1-and$afterCost.Count-eq1) 'source native action commitment not exactly once'
  $b=$beforeCost[0].state;$a=$afterCost[0].state
  $action=$before.ability.runtimeActionType
  Need ($action-cin@('Standard','Swift','Move')) 'baseline action type unsupported or unobserved'
  if($Row.name-ceq'C6C-quickened-self'){Need ($action-ceq'Swift') 'rod did not produce genuine native Swift casting'}
  $field=if($action-ceq'Standard'){'standard'}elseif($action-ceq'Swift'){'swift'}else{'move'}
  $nominal=if($action-ceq'Move'){3.0}else{6.0}
  if($Tb){Need (Close ((Number $a.$field)-(Number $b.$field)) $nominal) 'native additive action debt differs'}
  else {
   $acted=@($events|Where-Object {$_.kind-ceq'action-after'-and$_.identity-eq$castRoot})
   Need ($acted.Count-eq1-and$acted[0].shell.ignoreCooldown-eq$false) 'source native action timing absent'
   Need (Close $a.$field ($nominal-(Number $acted[0].shell.timeSinceStart))) 'native real-time cost differs from observed start duration'
  }
  $full=$before.ability.fullRound-eq$true
  if($full) {
   Need ($Row.name-ceq'C6C-full-round'-and$action-ceq'Standard') 'unexpected full-round native action'
   $moveExpected=if($Tb){(Number $b.move)+3.0}else{Number $b.move}
   Need (Close $a.move $moveExpected) 'full-round native Move debt differs'
  }
  foreach($other in @('standard','move','swift')|Where-Object {$_-cne$field-and(-not$full-or$_-cne'move')}){Need (Close $a.$other $b.$other) 'extra rider action cost'}
  foreach($extra in @($costAfter|Where-Object command -NE $root)) {
   Need ($extra.ignoreCooldown-eq$true-or($motion-and$extra.command-eq$carrier-and$extra.state.actor-ceq$moveOwner)) 'another rider command cost was charged'
  }
  if($Row.name-ceq'C6C-prepared-interrupt-after') {
   Need ($e.interrupted-eq$true-and$e.interruptionBefore.shell.acted-eq$true-and$e.interruptionBefore.shell.finished-eq$false) 'postcommit exact-shell interruption not observed'
   Need ($before.slotAvailable-eq$true-and$after.slotAvailable-eq$false) 'native prepared slot not spent once'
  }
  if($Row.name-ceq'C6C-full-round') {
   Need ($before.ability.fullRound-eq$true-and$before.slotAvailable-eq$true-and$after.slotAvailable-eq$false) 'available native converted full-round slot not spent'
   $summons=@($events|Where-Object kind -CEQ 'summon')
   Need ($summons.Count-gt0-and@($summons|Where-Object {$_.actor-cne$Rider-or$_.unitObject-eq0-or$_.context-eq0}).Count-eq0) 'native converted summon effect not observed'
  }
  if($Row.name-ceq'C6C-standard-hostile'){Need ($before.slotAvailable-eq$true-and$after.slotAvailable-eq$false) 'native hostile prepared slot not spent'}
 }
 if($life) {
  $b=$e.boundary;$subject=if($Row.name-ceq'C6C-rider-incapacity'){$Rider}else{$Mount}
  Need ($b.subject-ceq$subject-and$b.nativeDamage-gt0-and$b.damageAfter-ge$b.hitPoints-and$b.damageAfter-lt$b.deathThreshold-and$b.unconsciousObserved-eq$true-and$b.deadObserved-eq$false) 'real safe native incapacity not observed'
  Need ($b.healthRestored-eq$true-and$b.damageRestored-eq$b.damageBefore-and$b.settledBeforeHealthRestore.riderCommandsEmpty-eq$true-and$b.settledBeforeHealthRestore.mountCommandsEmpty-eq$true-and$b.settledBeforeHealthRestore.processesSettled-eq$true) 'life stimulus restored before native command settlement'
  Need ($after.relationship-ceq'Unmounted'-and$after.riderLife.conscious-eq$true-and$after.mountLife.conscious-eq$true-and$after.riderLife.dead-eq$false-and$after.mountLife.dead-eq$false) 'life cleanup relationship or restored pair differs'
  if($Mounted){Need ($after.generation-eq$before.generation-and$before.relationship-ceq'Mounted'-and$after.relationshipRider-eq$null-and$after.relationshipMount-eq$null) 'life cleanup retained a pair or fabricated a new generation'}
 }
 if($motion) {
  $b=$e.boundary;$samples=@($e.motionSamples)
  Need ($b.nativeGroundInputCount-eq1-and$b.carrier-ne0-and$b.costCarrier-ne0-and$b.carrierExecutor-ceq$moveOwner-and$samples.Count-gt0) 'native movement ownership unobserved'
  Need ($samples[-1].carrierFinished-eq$true-and$samples[-1].moverMoveSlotOwnsCarrier-eq$false-and$after.activePairCommand-eq$false-and$after.pairMovement-eq$false) 'movement/cast replacement left its carrier live'
  $moved=@(@($b.setupCostEvents)+$costs|Where-Object {$_.boundary-ceq'cost-after'-and$_.command-eq$carrier-and$_.state.actor-ceq$moveOwner})
  Need ($moved.Count-le1) 'transport command cost was replayed'
 }
 if($Row.name-ceq'C6C-under-threat') {
  Need ($e.boundary.riderEngaged-eq$true-and$e.boundary.nativeHostileAttackInputCount-eq1-and$e.boundary.nativeAttackTerminalBeforeCast-eq$true) 'real native threat not established'
  $def=@($events|Where-Object kind -CEQ 'defensive-rule-after')
  Need ($def.Count-eq1-and$def[0].actor-ceq$Rider-and$def[0].dc-gt0-and$def[0].roll-is[int]-and$def[0].success-is[bool]) 'native defensive check outcome unobserved'
  if($nativeConcentrationFailure) {
   Need ($cast.Count-eq0-and@($events|Where-Object kind -CEQ 'spell-spend-after').Count-eq1-and$after.shell.finished-eq$true) 'native concentration failure lost or duplicated commitment'
   Need ($costAfter.Count-eq$(if($Tb){1}else{0})) 'concentration-failure native mode cost differs'
  }
 }
 if($Row.name-ceq'C6C-quickened-self') {
  $rodBefore=@($before.items|Where-Object blueprint -CEQ '55a059b32df920c4abe65b8ee8b56056')
  $rodAfter=@($after.items|Where-Object blueprint -CEQ '55a059b32df920c4abe65b8ee8b56056')
  Need ($rodBefore.Count-eq1-and$rodAfter.Count-eq1-and$rodBefore[0].item-eq$rodAfter[0].item-and$rodBefore[0].activatableSourceItem-eq$rodBefore[0].item-and$rodBefore[0].activatableOn-eq$true-and$rodBefore[0].charges-eq3-and$rodAfter[0].charges-eq2) 'exact native rod did not spend one factory-created charge'
 }
 if($Row.name-cin@('C6C-potion-self','C6C-scroll-friendly')) {
  $id=$before.ability.sourceItem
  Need ($id-ne0-and$before.ability.sourceItemBlueprint-cin@('d52566ae8cbe8dc4dae977ef51c27d91','cd635d5720937b044a354dba17abad8d')) 'item source identity missing'
  $spent=@($events|Where-Object {$_.kind-ceq'item-spend-after'-and$_.identity-eq$id})
  $pre=@($events|Where-Object {$_.kind-ceq'item-spend-before'-and$_.identity-eq$id})
  Need ($pre.Count-eq1-and$spent.Count-eq1-and(($spent[0].charges-eq$pre[0].charges-1)-or($spent[0].count-eq$pre[0].count-1))) 'native consumable state did not decrease exactly once'
  Need ($spent.Count-eq1) 'native item charge/consumption not exactly once'
  $heal=@($events|Where-Object {$_.kind-ceq'heal'-and$_.actor-ceq$Rider-and$_.target-ceq$e.target})
  Need ($heal.Count-eq1-and(Number $heal[0].value)-gt0) 'item healing missing or duplicated'
 }
}
function Assert-KmcChunk6cCastingEvidence {
 param([Parameter(Mandatory=$true)]$Request,[Parameter(Mandatory=$true)]$Artifact,[AllowNull()][string]$Status)
 if($Request.scenario-cnotin@('chunk6c-casting-rt','chunk6c-casting-tb','chunk6c-casting-unmounted-rt','chunk6c-casting-unmounted-tb')-or$Artifact.schemaVersion-ne44){throw '6C scenario/schema mismatch'}
 if($Artifact.status-ceq'FAIL') {
  if($Status-ceq'PASS'-or@($Artifact.errors).Count-eq0-or$Artifact.subscenarioFailCount-lt1){throw '6C incomplete observation cannot become PASS'}
  return # Preserve the producer's explicit failed prefix; grant no qualification.
 }
 if($Artifact.status-cne'PASS'-or@($Artifact.errors).Count-ne0-or@($Artifact.rows|Where-Object status -CNE 'PASS').Count-ne0-or$Artifact.subscenarioFailCount-ne0-or$Artifact.subscenarioPassCount-ne@($Artifact.rows).Count){throw '6C failed/incomplete native envelope'}
 $o=$Artifact.observations.chunk6cCasting
 if($null-eq$o-or$o.contract-cne'native-mounted-casting-items-v1'-or$o.rider-cne$Artifact.observations.riderId-or$o.mount-cne$Artifact.observations.horseId-or$o.mountBlueprint-cne'e7aa96d15a45238438ae4cfb476f6bb9'){throw '6C exact original-pair observation missing'}
 $tb=$Request.scenario.EndsWith('-tb');$mounted=-not$Request.scenario.Contains('-unmounted-')
 if($o.mode-cne$(if($tb){'TB'}else{'RT'})-or$o.mounted-ne$mounted){throw '6C mode/surface differs'}
 $expected=@(Get-KmcChunk6cCastingCases)
 if(($o.cases|ConvertTo-Json -Compress)-cne($expected|ConvertTo-Json -Compress)){throw '6C registered row order differs'}
 foreach($name in $expected){$rows=@($Artifact.rows|Where-Object name -CEQ $name);if($rows.Count-ne1){throw ('6C required row missing or duplicated: '+$name)};Assert-KmcChunk6cCastingRow $rows[0] $o.rider $o.mount $tb $mounted}
 if($o.castTrace.faults-ne0-or$o.castTrace.dropped-ne0-or$o.castTrace.identityRegistry.faults-ne0-or$o.castTrace.identityRegistry.released-ne$true-or$o.castTrace.identityRegistry.retainedCount-ne0-or$o.costTrace.observationErrors-ne0-or$o.costTrace.dropped-ne0-or$o.costTrace.identityRegistry.faults-ne0-or$o.costTrace.identityRegistry.released-ne$true-or$o.costTrace.identityRegistry.retainedCount-ne0){throw '6C observer incomplete or ownership not retired'}
 if($o.final.riderCommandsEmpty-ne$true-or$o.final.mountCommandsEmpty-ne$true-or$o.final.processesSettled-ne$true-or$o.final.nativeAbilitiesPending-ne$false-or$o.final.projectilesPending-ne$false){throw '6C final native residue remains'}
 foreach($item in @($o.items)){if($item.disposed-ne$true-or$item.slotRestored-ne$true-or$item.noOwnedItemResident-ne$true){throw '6C disposable native item ownership remains'}}
 if(@($o.summonCleanup|Where-Object {$_.inState-ne$false-or$_.worldContains-ne$false}).Count-ne0){throw '6C fixture summoned actor remains'}
 if($Status-ceq'PASS'-and$Artifact.status-cne'PASS'){throw '6C failed native observation cannot be promoted'}
}