# Read-only substantive acceptance for the bounded stock 6C baseline.
# Native C# emits observations; this module owns their interpretation.
Set-StrictMode -Version Latest
function Get-KmcChunk6cCastingCases {
 @('C6C-quickened-self','C6C-standard-self','C6C-standard-friendly','C6C-standard-hostile',
   'C6C-scroll-interrupt-after','C6C-invalid-target','C6C-cancel-before','C6C-interrupt-before',
   'C6C-potion-self','C6C-scroll-friendly','C6C-full-round','C6C-movement-policy','C6C-rider-incapacity','C6C-mount-incapacity','C6C-under-threat')
}
# Native preparation is never replayed inside a cast window. A Cooldowns.Clear nested inside the
# same actor's native combat-exit window (combat-clear-before .. combat-clear-after) is the engine's
# own leave-combat reset of an incapacitated actor, not a replay; every other clear or prepare is.
function Get-KmcChunk6cEventTurn($Event) {
 $p=$Event.PSObject.Properties['turn']
 if($null-eq$p-or$null-eq$p.Value){return '0'}
 [string]$p.Value
}
function Get-KmcChunk6cPairReplayCount($Costs,[string]$Rider,[string]$Mount) {
 # A pair preparation inside the row's own turn is a replay. A row that legitimately waits for the
 # rider's next native turn (frozen 204 TB 6D double-move-ranged: the retained-Standard cast after the
 # ranged attack) records that turn's single native preparation per actor (prepare-before, the cooldown
 # clear it nests, prepare-after) under a new turn identity; only a further preparation there is a replay.
 $inside=@{};$replays=0;$rowTurn=$null;$seenTurn=$false;$nativePrepare=@{}
 foreach($c in @($Costs)) {
  $actor=[string]$c.state.actor
  if($actor-cnotin@($Rider,$Mount)){continue}
  $turn=Get-KmcChunk6cEventTurn $c
  if(-not$seenTurn){$rowTurn=$turn;$seenTurn=$true}
  $laterTurn=$turn-cne$rowTurn
  $key=$actor+'|'+$turn
  $nativeOpen=$laterTurn-and$nativePrepare[$key]-ceq'open'
  switch -CaseSensitive([string]$c.boundary) {
   'combat-clear-before' {$inside[$actor]=$true}
   'combat-clear-after' {$inside[$actor]=$false}
   'prepare-before' {if($laterTurn-and-not$nativePrepare.ContainsKey($key)){$nativePrepare[$key]='open'}else{$replays++}}
   'prepare-after' {if($nativeOpen){$nativePrepare[$key]='closed'}else{$replays++}}
   'clear-before' {if(-not$inside[$actor]-and-not$nativeOpen){$replays++}}
   'clear-after' {if(-not$inside[$actor]-and-not$nativeOpen){$replays++}}
  }
 }
 $replays
}
function Assert-KmcChunk6cCastingRow($Row,[string]$Rider,[string]$Mount,[bool]$Tb,[bool]$Mounted,$Items=$null) {
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
 $expectedBlueprint=switch -CaseSensitive($Row.name){'C6C-standard-hostile'{'9f10909f0be1f5141bf1c102041f93d9'} 'C6C-full-round'{'c6147854641924442a3bb736080cfeb6'} default{'5590652e1c2225c4ca30c4a699ab3649'}}
 Need ($before.ability.blueprint-ceq$expectedBlueprint) 'native fixture ability substituted'
 # Instrument identity. The fixture Druid has no memorized orison, so the native IsAvailable
 # predicate (not IsAvailableForCast) must already be true for every spellbook row; the other
 # rows cast CLW from the exact native scroll stack and the potion row drinks the exact potion.
 $spellbookRow=$Row.name-cin@('C6C-quickened-self','C6C-standard-hostile','C6C-full-round')
 $scrollRow=-not$spellbookRow-and$Row.name-cne'C6C-potion-self'
 if($spellbookRow){Need ($before.ability.sourceItem-eq0-and$null-eq$before.ability.sourceItemBlueprint-and$before.ability.available-eq$true) 'spellbook row lacks an available memorized or converted native slot'}
 if($scrollRow){Need ($before.ability.sourceItem-ne0-and$before.ability.sourceItemBlueprint-ceq'cd635d5720937b044a354dba17abad8d'-and$before.ability.available-eq$true) 'scroll row did not cast from the exact native scroll stack'}
 if($Row.name-ceq'C6C-potion-self'){Need ($before.ability.sourceItem-ne0-and$before.ability.sourceItemBlueprint-ceq'd52566ae8cbe8dc4dae977ef51c27d91') 'potion row did not drink the exact native potion'}
 # The row's source item is the exact equipped disposable entity recorded by the lease of the same
 # identity registry, never an original or a foreign quick-slot refill of one.
 if($null-ne$Items) {
  $leaseBlueprint=if($Row.name-ceq'C6C-potion-self'){'d52566ae8cbe8dc4dae977ef51c27d91'}elseif($scrollRow){'cd635d5720937b044a354dba17abad8d'}else{$null}
  if($leaseBlueprint) {
   $lease=@($Items|Where-Object blueprint -CEQ $leaseBlueprint)
   Need ($lease.Count-eq1-and$lease[0].equipped.exactSlot-eq$true-and$before.ability.sourceItem-eq$lease[0].equipped.item) 'item row did not source the exact equipped disposable entity'
  }
 }
 Need ($after.riderCommandsEmpty-eq$true-and$after.mountCommandsEmpty-eq$true-and$after.processesSettled-eq$true-and$after.nativeAbilitiesPending-eq$false-and$after.projectilesPending-eq$false-and$after.activePairCommand-eq$false) 'command/process/pair residue remains'
 Need ($e.selectionAfter-is[Array]-and$e.selectionAfter.Count-eq1-and$e.selectionAfter[0]-ceq$Rider) 'normal input lacks exact single rider selection'
 $events=@($e.events);$costs=@($e.costEvents)
 Need (@($events|Where-Object actor -CEQ $Mount).Count-eq0) 'mount cast/spend/delivery observed'
 $moveOwner=if($Mounted){$Mount}else{$Rider}
 $carrier=if($motion){$e.boundary.costCarrier}else{0}
 Need (@($costs|Where-Object {$_.state.actor-ceq$Mount-and$_.boundary-cin@('cost-before','cost-after','actor-cost-before','actor-cost-after')-and(-not$motion-or$_.command-ne$carrier)}).Count-eq0) 'mount charged for rider casting'
 Need ((Get-KmcChunk6cPairReplayCount $costs $Rider $Mount)-eq0) 'pair preparation replayed in cast window'
 if($life) {
  $partner=if($Row.name-ceq'C6C-rider-incapacity'){$Mount}else{$Rider}
  Need (@($costs|Where-Object {$_.state.actor-ceq$partner-and$_.boundary-like'*clear*'}).Count-eq0) 'partner cooldowns cleared during the subject incapacity'
 } else {
  Need (@($costs|Where-Object {$_.state.actor-cin@($Rider,$Mount)-and$_.boundary-like'*clear*'}).Count-eq0) 'pair cooldowns cleared in a settled cast window'
 }
 # Turn-based life rows: the subject's native incapacity ends the pair's shared turn and the product's
 # paired turn end marks the mount's remaining actions spent (the shared paired turn-end event; frozen
 # 203 mounted TB: mount standard 0 -> 6 at turn-end-after, decaying afterwards). That is the only
 # admitted mount debt change: at turn-end-before the mount still carries the row's baseline (nothing
 # was charged during the cast) and the settled debt never exceeds the turn-end value.
 $mountTurnEndBefore=@($costs|Where-Object {$_.state.actor-ceq$Mount-and$_.boundary-ceq'turn-end-before'})
 $mountTurnEndAfter=@($costs|Where-Object {$_.state.actor-ceq$Mount-and$_.boundary-ceq'turn-end-after'})
 foreach($field in @('standard','move','swift')) {
  if($Tb-and$life-and($mountTurnEndBefore.Count-gt0-or$mountTurnEndAfter.Count-gt0)) {
   Need ($mountTurnEndBefore.Count-eq1-and$mountTurnEndAfter.Count-eq1-and(Close $mountTurnEndBefore[0].state.$field $before.mount.$field)-and(Number $after.mount.$field)-le(Number $mountTurnEndAfter[0].state.$field)+.001) 'mount native debt changed outside the native paired turn end'
  }
  # Without a paired turn end the mount's debt may only decay: the native cooldown controller ticks every
  # unit's cooldowns while game time flows in both modes (frozen 204 unmounted TB rider-incapacity: the
  # separately-turned mount's Standard 4.00 -> 3.76 during the rider's lost cast, the rider's own delta).
  elseif(-not$motion){Need ((Number $after.mount.$field)-le(Number $before.mount.$field)+.001) 'mount native debt increased during rider action'}
 }
 $costBefore=@($costs|Where-Object {$_.boundary-ceq'cost-before'-and$_.state.actor-ceq$Rider})
 $costAfter=@($costs|Where-Object {$_.boundary-ceq'cost-after'-and$_.state.actor-ceq$Rider})
 $cast=@($events|Where-Object {$_.kind-ceq'cast-after'-and$_.ability.blueprint-ceq$before.ability.blueprint-and$_.actor-ceq$Rider})
 $refusal=$Row.name-cin@('C6C-invalid-target','C6C-cancel-before','C6C-interrupt-before','C6C-rider-incapacity')-or($Row.name-ceq'C6C-mount-incapacity'-and$cast.Count-eq0)
 # Native concentration (pinned IL, Assembly-CSharp MVID 07fa1e4d): damage to a unit whose running
 # standard command is a UnitUseAbility makes UnitConcentrationController.Tick (0x060090F7) call
 # MakeConcentrationCheck (0x06002739; wand sources excluded, scrolls included); a failed check makes
 # FailIfConcentrationCheckFailed (0x06002736) force-finish the shell before it acts and spend the
 # spell through AbilityData.Spend (0x06002B60: the scroll charge, then the spellbook slot), so no
 # cast and no action cost exist in either mode (TB charges at the action frame, frozen 202 TB).
 # Only the two rows whose stimulus can land inside the running shell may show that check; the
 # rider incapacity row must, because its damage is applied to the running precommit shell.
 $concentration=@($events|Where-Object {$_.kind-ceq'concentration-rule-after'-and$_.actor-ceq$Rider})
 Need ($concentration.Count-le1-and($concentration.Count-eq0-or$Row.name-cin@('C6C-under-threat','C6C-rider-incapacity'))) 'unexpected native concentration check'
 if($Row.name-ceq'C6C-rider-incapacity'){Need ($concentration.Count-eq1-and$concentration[0].dc-gt0-and$concentration[0].roll-is[int]-and$concentration[0].success-is[bool]) 'incapacity damage did not reach the native concentration owner inside the running shell'}
 $nativeConcentrationFailure=$concentration.Count-eq1-and$concentration[0].success-eq$false
 # A turn-based lost spell is the one refusal that carries a native charge (see the lost-spell rules).
 $refusalCharges=$(if($Tb-and$nativeConcentrationFailure){1}else{0})
 if($refusal) {
  Need ($cast.Count-eq0-and$costBefore.Count-eq$refusalCharges-and$costAfter.Count-eq$refusalCharges) 'precommit refusal/cancellation spent or delivered'
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
  if($Row.name-ceq'C6C-quickened-self'){Need ($action-ceq'Swift') 'rod did not produce genuine native Swift casting';Need ($before.slotAvailable-eq$true-and$after.slotAvailable-eq$false) 'quickened memorized slot not spent once'}
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
  if($Row.name-ceq'C6C-scroll-interrupt-after') {
   Need ($e.interrupted-eq$true-and$e.interruptionBefore.shell.acted-eq$true-and$e.interruptionBefore.shell.finished-eq$false) 'postcommit exact-shell interruption not observed'
  }
  if($Row.name-ceq'C6C-full-round') {
   Need ($before.ability.fullRound-eq$true-and$before.slotAvailable-eq$true-and$after.slotAvailable-eq$false) 'available native converted full-round slot not spent'
   $summons=@($events|Where-Object kind -CEQ 'summon')
   Need ($summons.Count-gt0-and@($summons|Where-Object {$_.actor-cne$Rider-or$_.unitObject-eq0-or$_.context-eq0}).Count-eq0) 'native converted summon effect not observed'
   # The row releases its exact native summons before the next row (frozen 202 TB stages 2/4: a live
   # summon owned a foreign turn and changed the leased party membership); every summoned unit is named.
   $summonUnits=@($summons|ForEach-Object {[string]$_.unit}|Sort-Object -Unique)
   Need ($null-ne$e.PSObject.Properties['summonCleanup']) 'row-end summon release unobserved'
   $cleanup=@($e.summonCleanup)
   Need ($cleanup.Count-eq$summonUnits.Count-and@($cleanup|Where-Object {$_.inState-ne$false-or$_.worldContains-ne$false-or[string]$_.unit-cnotin$summonUnits}).Count-eq0) 'exact native summons were not released before the next row'
  }
  if($Row.name-ceq'C6C-standard-hostile'){Need ($before.slotAvailable-eq$true-and$after.slotAvailable-eq$false) 'native hostile prepared slot not spent'}
 }
 if($nativeConcentrationFailure) {
  # The lost spell: the shell force-finished before acting. In real time nothing is charged (frozen
  # 202/203 RT); in turn-based mode the native action controller charges the finished, not interrupted,
  # shell its Standard action at the finish frame (frozen 203 mounted TB: cost-before/after with the
  # command end, standard 0 -> 6). Exactly one native spell spend, and for a scroll row exactly one
  # charge of the exact leased entity decremented in place; a memorized slot is spent once.
  $lostCharges=$(if($Tb){1}else{0})
  # (In turn-based mode the charged shell reports acted while it never ran a cast process: frozen 203 TB.)
  Need ($cast.Count-eq0-and$costBefore.Count-eq$lostCharges-and$costAfter.Count-eq$lostCharges-and$after.shell.finished-eq$true-and$after.shell.process-eq0) 'native concentration failure cast, charged outside its native mode or left its shell live'
  if($Tb) {
   $lostRoot=$e.costShell;$lostBefore=@($costBefore|Where-Object command -EQ $lostRoot);$lostAfter=@($costAfter|Where-Object command -EQ $lostRoot)
   Need ($lostBefore.Count-eq1-and$lostAfter.Count-eq1-and(Close ((Number $lostAfter[0].state.standard)-(Number $lostBefore[0].state.standard)) 6.0)) 'turn-based lost spell did not charge exactly one native Standard action on its own shell'
  }
  Need (@($events|Where-Object kind -CEQ 'spell-spend-after').Count-eq1) 'native concentration failure lost or duplicated commitment'
  if($scrollRow) {
   $id=$before.ability.sourceItem;$spent=@($events|Where-Object {$_.kind-ceq'item-spend-after'-and$_.identity-eq$id})
   Need ($id-ne0-and$spent.Count-eq1-and@($events|Where-Object kind -CEQ 'item-spend-after').Count-eq1) 'native concentration failure did not spend exactly one charge of the exact scroll'
   $stackBefore=@($before.items|Where-Object {$_.item-eq$id});$stackAfter=@($after.items|Where-Object {$_.item-eq$id})
   Need ($stackBefore.Count-eq1-and$stackAfter.Count-eq1-and$stackBefore[0].exactSlot-eq$true-and$stackAfter[0].exactSlot-eq$true-and$stackBefore[0].count-ge2-and$stackAfter[0].count-eq$stackBefore[0].count-1-and$stackAfter[0].charges-eq1) 'lost scroll charge did not decrement the exact disposable stack in place'
  } else {Need ($before.slotAvailable-eq$true-and$after.slotAvailable-eq$false) 'lost memorized spell did not spend its slot once'}
 }
 if($life) {
  $b=$e.boundary;$subject=if($Row.name-ceq'C6C-rider-incapacity'){$Rider}else{$Mount}
  $nativeBefore=if($Row.name-ceq'C6C-rider-incapacity'){$b.beforeIncapacity.riderLife}else{$b.beforeIncapacity.mountLife}
  Need ($b.mainCharacter-is[bool]-and$b.mainCharacter-eq$nativeBefore.mainCharacter-and$b.nativeRuleIsFake-eq$false-and(Close $b.difficulty $b.nativeRuleDifficulty)) 'native life stimulus identity/difficulty differs'
  Need ($nativeBefore.conscious-eq$true-and$nativeBefore.dead-eq$false-and$nativeBefore.allowDyingCondition-eq$true-and$nativeBefore.immortal-eq$false-and$nativeBefore.essential-eq$false) 'life stimulus lacks its native safe subject'
  if($b.mainCharacter) {
   Need ($nativeBefore.temporaryHitPoints-eq0-and$b.nativeDamageCap-is[int]-and$b.nativeRuleDamageCap-eq$b.nativeDamageCap-and$b.nativeDamageBeforeDifficulty-le$b.requested-and$b.nativeDamageCap-eq$b.hitPoints-$b.requested) 'main-character native damage cap absent or exceeded'
  } else {Need ($null-eq$b.nativeDamageCap-and$null-eq$b.nativeRuleDamageCap) 'non-main life fixture cap identity differs'}
  Need ($b.subject-ceq$subject-and$b.nativeDamage-gt0-and$b.damageAfter-ge$b.hitPoints-and$b.damageAfter-lt$b.deathThreshold-and$b.unconsciousObserved-eq$true-and$b.deadObserved-eq$false) 'real safe native incapacity not observed'
  Need ($b.healthRestored-eq$true-and$b.damageRestored-eq$b.damageBefore-and$b.settledBeforeHealthRestore.riderCommandsEmpty-eq$true-and$b.settledBeforeHealthRestore.mountCommandsEmpty-eq$true-and$b.settledBeforeHealthRestore.processesSettled-eq$true) 'life stimulus restored before native command settlement'
  Need ($after.relationship-ceq'Unmounted'-and$after.riderLife.conscious-eq$true-and$after.mountLife.conscious-eq$true-and$after.riderLife.dead-eq$false-and$after.mountLife.dead-eq$false) 'life cleanup relationship or restored pair differs'
  if($Mounted){Need ($after.generation-eq$before.generation-and$before.relationship-ceq'Mounted'-and$after.relationshipRider-eq$null-and$after.relationshipMount-eq$null) 'life cleanup retained a pair or fabricated a new generation'}
 }
 if($motion) {
  $b=$e.boundary;$samples=@($e.motionSamples)
  Need ($b.nativeGroundInputCount-eq1-and$b.nativeMovingBeforeCast-eq$true-and$b.carrier-ne0-and$b.costCarrier-ne0-and$b.carrierExecutor-ceq$moveOwner-and$samples.Count-gt0) 'native movement ownership unobserved'
  Need ($samples[-1].carrierFinished-eq$true-and$samples[-1].moverMoveSlotOwnsCarrier-eq$false-and$after.activePairCommand-eq$false-and$after.pairMovement-eq$false) 'movement/cast replacement left its carrier live'
  $moved=@(@($b.setupCostEvents)+$costs|Where-Object {$_.boundary-ceq'cost-after'-and$_.command-eq$carrier-and$_.state.actor-ceq$moveOwner})
  Need ($moved.Count-le1) 'transport command cost was replayed'
 }
 if($Row.name-ceq'C6C-under-threat') {
  Need ($e.boundary.riderEngaged-eq$true-and$e.boundary.nativeHostileAttackInputCount-eq1-and$e.boundary.nativeAttackTerminalBeforeCast-eq$true) 'real native threat not established'
  # The native casting-defensively window (UnitUseAbility.OnTick, 0x06002734) opens only for a Standard
  # shell still running after one second while engaged in combat; TryCastingDefensively (0x0600273B)
  # exempts wand sources only, so the scroll is checked. A self-targeted CLW acts after ~0.55 s and is
  # never checked (frozen 202 RT stages 1/3); the threatened row therefore targets the mount (~1.5 s)
  # and the check must be observed exactly once. A failed check provokes the native attack of
  # opportunity, whose damage may fail concentration: that is the lost spell handled above.
  Need ($e.target-ceq$Mount-and$e.resolvedTarget-ceq$Mount) 'threatened cast did not target the exact mount'
  $def=@($events|Where-Object kind -CEQ 'defensive-rule-after')
  Need ($def.Count-eq1-and$def[0].actor-ceq$Rider-and$def[0].dc-gt0-and$def[0].roll-is[int]-and$def[0].success-is[bool]) 'native defensive check outcome unobserved'
  if(-not$nativeConcentrationFailure) {
   $acted=@($events|Where-Object {$_.kind-ceq'action-after'-and$_.identity-eq$e.afterInput.shell.identity})
   Need ($acted.Count-eq1-and(Number $acted[0].shell.timeSinceStart)-gt1.0) 'threatened cast acted before the native one-second defensive window'
  }
 }
 if($Row.name-ceq'C6C-quickened-self') {
  $rodBefore=@($before.items|Where-Object blueprint -CEQ '55a059b32df920c4abe65b8ee8b56056')
  $rodAfter=@($after.items|Where-Object blueprint -CEQ '55a059b32df920c4abe65b8ee8b56056')
  Need ($rodBefore.Count-eq1-and$rodAfter.Count-eq1-and$rodBefore[0].item-eq$rodAfter[0].item-and$rodBefore[0].activatableSourceItem-eq$rodBefore[0].item-and$rodBefore[0].activatableOn-eq$true-and$rodBefore[0].charges-eq3-and$rodAfter[0].charges-eq2) 'exact native rod did not spend one factory-created charge'
 }
 # Every completed item-sourced cast spends exactly one native charge/count; the two wounded
 # heal rows also deliver exactly one native heal. Refused or precommit-interrupted casts spend nothing.
 if($Row.name-cin@('C6C-potion-self','C6C-scroll-friendly')-or($scrollRow-and-not$refusal-and-not$nativeConcentrationFailure)) {
  $id=$before.ability.sourceItem
  Need ($id-ne0-and$before.ability.sourceItemBlueprint-cin@('d52566ae8cbe8dc4dae977ef51c27d91','cd635d5720937b044a354dba17abad8d')) 'item source identity missing'
  $spent=@($events|Where-Object {$_.kind-ceq'item-spend-after'-and$_.identity-eq$id})
  $pre=@($events|Where-Object {$_.kind-ceq'item-spend-before'-and$_.identity-eq$id})
  Need ($pre.Count-eq1-and$spent.Count-eq1-and(($spent[0].charges-eq$pre[0].charges-1)-or($spent[0].count-eq$pre[0].count-1))) 'native consumable state did not decrease exactly once'
  Need ($spent.Count-eq1) 'native item charge/consumption not exactly once'
  # The exact disposable stack stays equipped and decrements in place (count-1, charges back to 1);
  # a removed single unit would invoke the foreign quick-slot refill of an original item.
  $stackBefore=@($before.items|Where-Object {$_.item-eq$id});$stackAfter=@($after.items|Where-Object {$_.item-eq$id})
  Need ($stackBefore.Count-eq1-and$stackAfter.Count-eq1-and$stackBefore[0].exactSlot-eq$true-and$stackAfter[0].exactSlot-eq$true-and$stackBefore[0].count-ge2-and$stackAfter[0].count-eq$stackBefore[0].count-1-and$stackAfter[0].charges-eq1) 'exact disposable stack did not decrement in place'
  if($Row.name-cin@('C6C-potion-self','C6C-scroll-friendly')) {
   $heal=@($events|Where-Object {$_.kind-ceq'heal'-and$_.actor-ceq$Rider-and$_.target-ceq$e.target})
   Need ($heal.Count-eq1-and(Number $heal[0].value)-gt0) 'item healing missing or duplicated'
  }
 }
 if($scrollRow-and$refusal-and-not$nativeConcentrationFailure) {
  Need (@($events|Where-Object {$_.kind-ceq'item-spend-after'}).Count-eq0) 'refused or precommit-interrupted scroll cast spent a charge'
  $id=$before.ability.sourceItem;$stackBefore=@($before.items|Where-Object {$_.item-eq$id});$stackAfter=@($after.items|Where-Object {$_.item-eq$id})
  Need ($stackBefore.Count-eq1-and$stackAfter.Count-eq1-and$stackAfter[0].exactSlot-eq$true-and$stackAfter[0].count-eq$stackBefore[0].count) 'refused scroll cast changed the exact disposable stack'
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
 foreach($name in $expected){$rows=@($Artifact.rows|Where-Object name -CEQ $name);if($rows.Count-ne1){throw ('6C required row missing or duplicated: '+$name)};Assert-KmcChunk6cCastingRow $rows[0] $o.rider $o.mount $tb $mounted @($o.items)}
 if($o.castTrace.faults-ne0-or$o.castTrace.dropped-ne0-or$o.castTrace.identityRegistry.faults-ne0-or$o.castTrace.identityRegistry.released-ne$true-or$o.castTrace.identityRegistry.retainedCount-ne0-or$o.costTrace.observationErrors-ne0-or$o.costTrace.dropped-ne0-or$o.costTrace.identityRegistry.faults-ne0-or$o.costTrace.identityRegistry.released-ne$true-or$o.costTrace.identityRegistry.retainedCount-ne0){throw '6C observer incomplete or ownership not retired'}
 if($o.final.riderCommandsEmpty-ne$true-or$o.final.mountCommandsEmpty-ne$true-or$o.final.processesSettled-ne$true-or$o.final.nativeAbilitiesPending-ne$false-or$o.final.projectilesPending-ne$false){throw '6C final native residue remains'}
 foreach($item in @($o.items)){if($item.disposed-ne$true-or$item.slotRestored-ne$true-or$item.noOwnedItemResident-ne$true){throw '6C disposable native item ownership remains'}}
 # Disposable stacks: one exact equipped unit first, the bounded stack built on that entity, and the
 # final count equal to the requested count minus every observed native spend of that exact entity.
 foreach($item in @($o.items)) {
  if($item.requestedCount-isnot[int]-and$item.requestedCount-isnot[long]){throw '6C disposable item lacks its requested count'}
  if($item.equipped.exactSlot-ne$true-or$item.equipped.count-ne1-or$item.equipped.item-eq0){throw '6C disposable item was not equipped as one exact unit'}
  if($item.requestedCount-gt1-and($item.stacked.exactSlot-ne$true-or$item.stacked.count-ne$item.requestedCount-or$item.stacked.item-ne$item.equipped.item)){throw '6C disposable stack was not built on the equipped entity'}
  if($item.blueprint-cin@('d52566ae8cbe8dc4dae977ef51c27d91','cd635d5720937b044a354dba17abad8d')) {
   $spends=@($Artifact.rows|ForEach-Object {if($_.evidence.PSObject.Properties['events']){@($_.evidence.events)}}|Where-Object {$_.kind-ceq'item-spend-after'-and$_.identity-eq$item.equipped.item-and$_.result-eq$true}).Count
   if($item.beforeCleanup.exactSlot-ne$true-or$item.beforeCleanup.count-lt1-or$item.beforeCleanup.count-ne($item.requestedCount-$spends)){throw '6C disposable stack left its slot or its count does not conserve the observed native spends'}
  }
 }
 if(@($o.summonCleanup|Where-Object {$_.inState-ne$false-or$_.worldContains-ne$false}).Count-ne0){throw '6C fixture summoned actor remains'}
 if($Status-ceq'PASS'-and$Artifact.status-cne'PASS'){throw '6C failed native observation cannot be promoted'}
}