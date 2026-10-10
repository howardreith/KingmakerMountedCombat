# Read-only substantive acceptance for the Chunk 6D staged move-cast-move and double-move families.
# Native C# emits raw per-step facts; this module owns their interpretation. The mount's native Move
# budget carries the pair (accepted CRPG transport preset); the rider owns only its own actions.
Set-StrictMode -Version Latest
function Get-KmcChunk6dStagedCases {
 @('C6D-move-cast-move','C6D-cast-then-move','C6D-double-move-ranged','C6D-movement-exhausted','C6D-auto-stop-boundary')
}
function Get-KmcChunk6dStagedPlan([string]$Case) {
 switch -CaseSensitive($Case) {
  'C6D-move-cast-move' { @('move-short','cast-standard-scroll','move-short') }
  'C6D-cast-then-move' { @('cast-standard-scroll','move-long') }
  'C6D-double-move-ranged' { @('move-two-moves','attack-ranged','cast-standard-scroll') }
  'C6D-movement-exhausted' { @('move-exhaust','move-probe') }
  'C6D-auto-stop-boundary' { @('move-extended','cast-standard-scroll','move-short') }
  default { throw ('6D unknown case '+$Case) }
 }
}
# Shared staged-family helpers (6D and 6E).
function Get-KmcStagedProp($Object,[string]$Name) { if($null-ne$Object-and$Object.PSObject.Properties[$Name]){$Object.$Name}else{$null} }
function Get-KmcStagedNumber($Value,[string]$Why) {
 if($Value-isnot[int]-and$Value-isnot[long]-and$Value-isnot[single]-and$Value-isnot[double]-and$Value-isnot[decimal]){throw ('staged number missing: '+$Why)}
 $x=[double]$Value;if([double]::IsNaN($x)-or[double]::IsInfinity($x)){throw ('staged nonfinite number: '+$Why)};$x
}
function Test-KmcStagedClose($A,$B,[string]$Why) { [Math]::Abs((Get-KmcStagedNumber $A $Why)-(Get-KmcStagedNumber $B $Why))-le.001 }
function Get-KmcStagedDisplacement($From,$To) {
 if($null-eq$From-or$null-eq$To){throw 'staged position missing'}
 $dx=(Get-KmcStagedNumber $From.x 'x')-(Get-KmcStagedNumber $To.x 'x');$dz=(Get-KmcStagedNumber $From.z 'z')-(Get-KmcStagedNumber $To.z 'z')
 [Math]::Sqrt($dx*$dx+$dz*$dz)
}
function Get-KmcStagedCostPairs($Costs,[string]$Actor,$CommandIdentity) {
 $before=@($Costs|Where-Object {$_.boundary-ceq'cost-before'-and$_.state.actor-ceq$Actor-and$_.command-eq$CommandIdentity})
 $after=@($Costs|Where-Object {$_.boundary-ceq'cost-after'-and$_.state.actor-ceq$Actor-and$_.command-eq$CommandIdentity})
 [pscustomobject]@{before=$before;after=$after}
}
# Every terminal state of a staged step or row: exact actors, mounted pair, settled native surfaces.
function Assert-KmcStagedSettledState($State,[string]$Rider,[string]$Mount,[string]$Why) {
 if($null-eq$State){throw ($Why+': state missing')}
 if($State.rider.actor-cne$Rider-or$State.mount.actor-cne$Mount){throw ($Why+': actor identity differs')}
 if($State.relationship-cne'Mounted'){throw ($Why+': pair is not mounted')}
 if($State.riderCommandsEmpty-ne$true-or$State.mountCommandsEmpty-ne$true-or$State.processesSettled-ne$true-or$State.nativeAbilitiesPending-ne$false-or$State.projectilesPending-ne$false-or$State.activePairCommand-ne$false){throw ($Why+': command/process/pair residue remains')}
}
# One native rider cast step (scroll CLW): exact instrument, one shell, one native commitment, exact cost owner.
function Assert-KmcStagedStandardScrollCast($Step,[string]$Rider,[string]$Mount,[bool]$Tb,$Items,[string]$Why) {
 function Need([bool]$ok,[string]$what){if(-not$ok){throw ($Why+': '+$what)}}
 $before=$Step.before;$after=$Step.after;$events=@($Step.events);$costs=@($Step.costEvents)
 Need ($Step.ability.caster-ceq$Rider-and$Step.ability.blueprint-ceq'5590652e1c2225c4ca30c4a699ab3649'-and$Step.ability.runtimeActionType-ceq'Standard'-and$Step.ability.available-eq$true) 'scroll cast instrument differs'
 $scroll=@($Items|Where-Object blueprint -CEQ 'cd635d5720937b044a354dba17abad8d')
 Need ($scroll.Count-eq1-and$Step.ability.sourceItem-eq$scroll[0].equipped.item-and$Step.ability.sourceItemBlueprint-ceq'cd635d5720937b044a354dba17abad8d') 'scroll cast did not source the exact equipped disposable stack'
 Need ($Step.target-ceq$Rider-and$Step.selected-eq$true-and$Step.canTarget-eq$true-and$Step.clicked-eq$true-and$Step.inputCount-eq1-and$Step.admittedShellCount-eq1-and$null-ne$Step.shell) 'normal cast input/shell not admitted exactly once'
 Need ($Step.terminal.finished-eq$true-and$Step.terminal.executor-ceq$Rider-and$Step.terminal.type-ceq'UnitUseAbility') 'cast shell did not finish on the rider'
 $cast=@($events|Where-Object {$_.kind-ceq'cast-after'-and$_.actor-ceq$Rider-and$_.ability.blueprint-ceq$Step.ability.blueprint})
 Need ($cast.Count-eq1-and$cast[0].process-ne0-and$cast[0].spellFailed-eq$false-and$cast[0].arcaneFailed-eq$false) 'native cast/process failed or duplicated'
 $pairs=Get-KmcStagedCostPairs $costs $Rider $Step.costShell
 Need ($pairs.before.Count-eq1-and$pairs.after.Count-eq1) 'rider Standard commitment not exactly once'
 Need (@($costs|Where-Object {$_.state.actor-ceq$Mount-and$_.boundary-cin@('cost-before','cost-after','actor-cost-before','actor-cost-after')}).Count-eq0) 'mount charged for the rider cast'
 # The native charge is read at the cost boundary itself (frozen 203 RT: the settled state has already
 # decayed by the time the step settles); the settled debt never exceeds that charge.
 $charged=$pairs.after[0].state;$chargedBefore=$pairs.before[0].state
 if($Tb){Need (Test-KmcStagedClose ((Get-KmcStagedNumber $charged.standard 'standard')-(Get-KmcStagedNumber $chargedBefore.standard 'standard')) 6.0 'standard') 'native additive Standard debt differs'}
 else {
  $acted=@($events|Where-Object {$_.kind-ceq'action-after'-and$_.identity-eq$Step.shell.identity})
  Need ($acted.Count-eq1-and$acted[0].shell.ignoreCooldown-eq$false) 'native action timing absent'
  Need (Test-KmcStagedClose $charged.standard (6.0-(Get-KmcStagedNumber $acted[0].shell.timeSinceStart 'timeSinceStart')) 'standard') 'native real-time Standard cost differs'
 }
 Need ((Get-KmcStagedNumber $after.rider.standard 'standard')-le(Get-KmcStagedNumber $charged.standard 'standard')+.001) 'settled Standard debt above its native charge'
 foreach($field in @('move','swift')){ if($Tb){Need (Test-KmcStagedClose $after.rider.$field $before.rider.$field $field) ('rider '+$field+' changed during the cast')} else {Need ((Get-KmcStagedNumber $after.rider.$field $field)-le(Get-KmcStagedNumber $before.rider.$field $field)+.001) ('rider '+$field+' rose during the cast')} }
 foreach($field in @('standard','move','swift')){ if($Tb){Need (Test-KmcStagedClose $after.mount.$field $before.mount.$field $field) ('mount '+$field+' changed during the rider cast')} else {Need ((Get-KmcStagedNumber $after.mount.$field $field)-le(Get-KmcStagedNumber $before.mount.$field $field)+.001) ('mount '+$field+' rose during the rider cast')} }
 $spent=@($events|Where-Object {$_.kind-ceq'item-spend-after'-and$_.identity-eq$Step.ability.sourceItem})
 $stackBefore=@($before.items|Where-Object {$_.item-eq$Step.ability.sourceItem});$stackAfter=@($after.items|Where-Object {$_.item-eq$Step.ability.sourceItem})
 Need ($spent.Count-eq1-and$stackBefore.Count-eq1-and$stackAfter.Count-eq1-and$stackBefore[0].exactSlot-eq$true-and$stackAfter[0].exactSlot-eq$true-and$stackBefore[0].count-ge2-and$stackAfter[0].count-eq$stackBefore[0].count-1) 'exact scroll stack did not decrement in place exactly once'
}
# One native ground order leg carried by the mount. In TB the mount's own native movement state
# decides admission: movement is expected while the mount has Move left and refused cost-free when
# it has none. RT has no movement budget and keeps only ownership and conservation rules.
function Assert-KmcStagedMoveLeg($Leg,[string]$Rider,[string]$Mount,[bool]$Tb,[string]$Why) {
 function Need([bool]$ok,[string]$what){if(-not$ok){throw ($Why+': '+$what)}}
 $before=$Leg.before;$after=$Leg.after;$costs=@($Leg.costEvents);$events=@($Leg.events)
 Need ($Leg.clicked-eq$true-and$Leg.inputCount-eq1-and$Leg.inputPath-cin@('pointer','ground-handler')) 'ground order input not issued exactly once'
 Need ($null-ne$before-and$null-ne$after-and$null-ne$Leg.afterInput) 'leg states missing'
 Need (@($events|Where-Object {$_.kind-clike'cast-*'-or$_.kind-clike'*spend*'}).Count-eq0) 'movement leg cast or spent something'
 Need (@($costs|Where-Object {$_.state.actor-ceq$Rider-and$_.boundary-cin@('cost-before','cost-after','actor-cost-before','actor-cost-after')}).Count-eq0) 'rider charged for mount movement'
 # Real-time cooldowns decay while the mount moves (frozen 203 RT); turn-based debt is exact.
 foreach($field in @('standard','move','swift')){ if($Tb){Need (Test-KmcStagedClose $after.rider.$field $before.rider.$field $field) ('rider '+$field+' changed during movement')} else {Need ((Get-KmcStagedNumber $after.rider.$field $field)-le(Get-KmcStagedNumber $before.rider.$field $field)+.001) ('rider '+$field+' rose during movement')} }
 if($Tb){Need (Test-KmcStagedClose $after.mount.standard $before.mount.standard 'standard') 'mount Standard changed during movement'} else {Need ((Get-KmcStagedNumber $after.mount.standard 'standard')-le(Get-KmcStagedNumber $before.mount.standard 'standard')+.001) 'mount Standard rose during movement'}
 $moved=Get-KmcStagedDisplacement $before.mountPosition $after.mountPosition
 # Native TB movement ends when the mount's move cooldown budget (two Move actions, 6 s) is spent, not
 # when HasMoveAction first reports false: frozen 203 TB kept the Mammoth moving from 3.68 to 5.32 after
 # UsedTwoMoveAction. Movement is therefore expected while the budget is open and refused once spent.
 $expectMovement=-not$Tb-or(Get-KmcStagedNumber $before.mount.move 'move')-lt5.95
 if($expectMovement) {
  $carrier=$Leg.carrier
  Need ($null-ne$carrier-and$carrier.type-ceq'UnitMoveTo'-and$carrier.executor-ceq$Mount-and$carrier.createdByPlayer-eq$true) 'exact mount carrier not admitted'
  Need ($Leg.terminal.finished-eq$true-and$Leg.terminal.executor-ceq$Mount) 'mount carrier did not finish'
  Need ($moved-gt0.5) 'mount did not move'
  if($Tb){Need ((Get-KmcStagedNumber $after.mount.move 'move')-gt(Get-KmcStagedNumber $before.mount.move 'move')+.001) 'mount native Move debt did not increase for its own movement'}
 } else {
  Need ($null-eq$Leg.carrier-or($Leg.terminal.finished-eq$true-and$moved-le0.3)) 'exhausted mount still moved'
  Need (@($costs|Where-Object {$_.boundary-cin@('cost-before','cost-after')}).Count-eq0) 'refused movement committed a cost'
  Need ($moved-le0.3) 'exhausted mount displaced'
 }
}
function Assert-KmcStagedMoveStep($Step,[string]$Rider,[string]$Mount,[bool]$Tb,[string]$Why) {
 function Need([bool]$ok,[string]$what){if(-not$ok){throw ($Why+': '+$what)}}
 $legs=@($Step.legs)
 $kind=[string]$Step.kind
 if($kind-cin@('move-two-moves','move-exhaust')){ if($Tb){Need ($legs.Count-ge1-and$legs.Count-le8) 'bounded leg count differs'}else{Need ($legs.Count-eq2) 'RT budget rows run exactly two legs'} }
 else {Need ($legs.Count-eq1) 'single-leg step ran more than one leg'}
 for($i=0;$i-lt$legs.Count;$i++){Assert-KmcStagedMoveLeg $legs[$i] $Rider $Mount $Tb ($Why+' leg '+$i)}
 $last=$legs[-1]
 if($Tb){
  if($kind-ceq'move-two-moves'){Need ($last.after.mountUsedTwoMove-eq$true-and(Get-KmcStagedNumber $last.after.mount.move 'move')-gt3.001) 'mount did not reach its native second Move'}
  if($kind-ceq'move-exhaust'){Need ((Get-KmcStagedNumber $last.after.mount.move 'move')-ge5.95) 'mount movement not natively exhausted'}
  if($kind-ceq'move-probe'){Need ((Get-KmcStagedNumber $legs[0].before.mount.move 'move')-ge5.95) 'probe did not start from native exhaustion'}
 }
 Assert-KmcStagedSettledState $Step.after $Rider $Mount ($Why+' step end')
}
function Assert-KmcStagedRangedAttack($Step,[string]$Rider,[string]$Mount,[bool]$Tb,[string]$Why) {
 function Need([bool]$ok,[string]$what){if(-not$ok){throw ($Why+': '+$what)}}
 $before=$Step.before;$after=$Step.after;$costs=@($Step.costEvents)
 Need ($Step.weapon.ranged-eq$true-and$Step.inputCount-eq1-and$Step.clicked-is[bool]) 'ranged input not recorded'
 # The product routes the mounted rider's stock ranged click into its own MountedPairAttackCommand
 # (action RiderRanged; frozen 203 RT and TB: "Mounted pair command accepted: RiderRanged."), charges the
 # rider's Standard once at the native cost boundary and resolves the attack natively; the mount is
 # never charged. The stock refusal code MountedRangedUnsupported belongs to the bypassing UnitAttack
 # path only and never appears here.
 Need ($null-ne$Step.admitted-and$Step.admitted.type-ceq'MountedPairAttackCommand'-and$Step.admitted.executor-ceq$Rider-and$Step.admitted.createdByPlayer-eq$true-and$Step.admitted.commandType-ceq'Standard'-and@($Step.rejectionCodes).Count-eq0-and[string]$Step.rejectionFeedback-ceq'Mounted pair command accepted: RiderRanged.') 'mounted ranged attack was not admitted as the product''s RiderRanged pair command'
 Need ($null-ne$Step.terminal-and$Step.terminal.identity-eq$Step.admitted.identity-and$Step.terminal.finished-eq$true-and$Step.terminal.acted-eq$true-and$Step.terminal.result-ceq'Success'-and$Step.terminal.executor-ceq$Rider) 'admitted ranged pair command did not finish successfully on the rider'
 $pairs=Get-KmcStagedCostPairs $costs $Rider $Step.admitted.costIdentity
 Need ($pairs.before.Count-eq1-and$pairs.after.Count-eq1) 'rider Standard commitment not exactly once'
 $charged=$pairs.after[0].state;$chargedBefore=$pairs.before[0].state
 if($Tb){Need (Test-KmcStagedClose ((Get-KmcStagedNumber $charged.standard 'standard')-(Get-KmcStagedNumber $chargedBefore.standard 'standard')) 6.0 'standard') 'native additive Standard debt differs'}
 else {Need ((Get-KmcStagedNumber $charged.standard 'standard')-gt(Get-KmcStagedNumber $chargedBefore.standard 'standard')+.001-and(Get-KmcStagedNumber $charged.standard 'standard')-le6.001) 'native real-time Standard charge missing or above the nominal action'}
 Need ((Get-KmcStagedNumber $after.rider.standard 'standard')-le(Get-KmcStagedNumber $charged.standard 'standard')+.001) 'settled Standard debt above its native charge'
 Need (@($costs|Where-Object {$_.boundary-cin@('cost-before','cost-after','actor-cost-before','actor-cost-after')-and$_.state.actor-ceq$Mount}).Count-eq0) 'mount charged for the rider ranged attack'
 foreach($field in @('move','swift')){ if($Tb){Need (Test-KmcStagedClose $after.rider.$field $before.rider.$field $field) ('rider '+$field+' changed during the ranged attack')} else {Need ((Get-KmcStagedNumber $after.rider.$field $field)-le(Get-KmcStagedNumber $before.rider.$field $field)+.001) ('rider '+$field+' rose during the ranged attack')} }
 foreach($field in @('standard','move','swift')){ if($Tb){Need (Test-KmcStagedClose $after.mount.$field $before.mount.$field $field) ('mount '+$field+' changed during the rider ranged attack')} else {Need ((Get-KmcStagedNumber $after.mount.$field $field)-le(Get-KmcStagedNumber $before.mount.$field $field)+.001) ('mount '+$field+' rose during the rider ranged attack')} }
 Assert-KmcStagedSettledState $after $Rider $Mount ($Why+' step end')
}
# Native boundaries that open, refresh or close a turn or round. Any of them inside a claimed activation
# means a later step drew on budget the activation itself did not carry.
function Get-KmcStagedTurnTransitionBoundaries {
 @('prepare-before','prepare-after','clear-before','clear-after','combat-clear-before','combat-clear-after',
   'turn-end-before','turn-end-after','casting-end-turn-input-before','casting-end-turn-input-after',
   'round-state-before','round-state-after','round-handler-before','round-handler-after','ai-round-before','ai-round-after')
}
function Get-KmcStagedEventSequence($Event,[string]$Why) {
 $p=$Event.PSObject.Properties['sequence']
 if($null-eq$p-or$null-eq$p.Value){throw ($Why+': cost boundary without its native trace sequence')}
 [long](Get-KmcStagedNumber $p.Value 'sequence')
}
# One native activation across the named steps: every cost boundary of those steps is stamped by the same
# rider turn identity and round (turn-based), none of them sits outside the Preparing/Acting window of the
# rider's own turn, and no turn-transition boundary (preparation, cooldown clear, turn end, round state,
# round handler, AI round) is traced anywhere between the first and the last of them in the row's own
# trace. Frozen 205 TB double-move-ranged: the two mount Moves and the ranged attack share turn
# -2051895680 in round 3 while the later scroll cast followed three native turn ends and a round rollover;
# that cast is a separate next-turn control and never evidence for the activation it followed.
function Assert-KmcStagedSameActivation($Row,[int[]]$StepIndexes,[string]$Rider,[string]$Mount,[bool]$Tb,[string]$Why) {
 function Need([bool]$ok,[string]$what){if(-not$ok){throw ($Why+': '+$what)}}
 $steps=@($Row.evidence.steps);$window=@()
 foreach($i in $StepIndexes){ Need ($i-ge0-and$i-lt$steps.Count) 'activation step index outside the row'; $window+=@($steps[$i].costEvents) }
 Need ($window.Count-ge2) 'activation carries no native cost boundaries'
 $sequences=@($window|ForEach-Object { Get-KmcStagedEventSequence $_ $Why })
 $first=($sequences|Measure-Object -Minimum).Minimum;$last=($sequences|Measure-Object -Maximum).Maximum
 $transitions=@(Get-KmcStagedTurnTransitionBoundaries)
 $inside=@(@($Row.evidence.costEvents)|Where-Object { $s=Get-KmcStagedEventSequence $_ $Why; $s-ge$first-and$s-le$last })
 Need ($inside.Count-ge$window.Count) 'row trace does not carry the activation''s own boundaries'
 Need (@($inside|Where-Object {[string]$_.boundary-cin$transitions}).Count-eq0) 'a native turn transition or round refresh sits inside the claimed activation'
 if($Tb){
  $turns=@($window|ForEach-Object { Get-KmcChunk6cEventTurn $_ }|Select-Object -Unique)
  Need ($turns.Count-eq1-and$turns[0]-cne'0') 'activation spans more than one native turn identity'
  $rounds=@($window|ForEach-Object { [string](Get-KmcStagedNumber $_.round 'round') }|Select-Object -Unique)
  Need ($rounds.Count-eq1) 'activation spans more than one native round'
  Need (@($window|Where-Object {[string]$_.currentActor-cne$Rider}).Count-eq0) 'activation boundary traced outside the rider''s own native turn'
  Need (@($window|Where-Object {[string]$_.turnStatus-cnotin@('Preparing','Acting')}).Count-eq0) 'activation boundary traced outside the Preparing/Acting window'
  Need ([string]$Row.evidence.rowTurnActor-ceq$Rider) 'row did not open on the rider''s native turn'
 }
 Need (@($window|Where-Object {[string]$_.boundary-ceq'cost-after'-and[string]$_.state.actor-ceq$Rider}).Count-eq1) 'the activation did not carry exactly one rider cost commitment'
}
function Assert-KmcChunk6dStagedRow($Row,[string]$Rider,[string]$Mount,[bool]$Tb,$Items,$AutoStop) {
 function Need([bool]$ok,[string]$why){if(-not$ok){throw ('6D '+$Row.name+': '+$why)}}
 $e=$Row.evidence
 Need ($Row.status-ceq'PASS'-and$e.case-ceq$Row.name) 'row structure/status differs'
 $plan=@(Get-KmcChunk6dStagedPlan $Row.name);$steps=@($e.steps)
 Need ((@($e.plan)-join',')-ceq($plan-join',')-and$steps.Count-eq$plan.Count) 'registered step plan differs'
 for($i=0;$i-lt$plan.Count;$i++){Need ($steps[$i].kind-ceq$plan[$i]-and$steps[$i].index-eq$i) ('step '+$i+' kind differs')}
 Need ($e.before.rider.actor-ceq$Rider-and$e.before.mount.actor-ceq$Mount-and$e.before.relationship-ceq'Mounted') 'row did not start on the exact mounted pair'
 Assert-KmcStagedSettledState $e.after $Rider $Mount ('6D '+$Row.name+' row end')
 Need ($e.after.generation-eq$e.before.generation) 'relationship generation changed'
 Need ((Get-KmcChunk6cPairReplayCount @($e.costEvents) $Rider $Mount)-eq0) 'pair preparation replayed inside the row'
 Need (@(@($e.events)|Where-Object actor -CEQ $Mount).Count-eq0) 'mount cast/spend observed'
 for($i=0;$i-lt$steps.Count;$i++){
  $step=$steps[$i];$why='6D '+$Row.name+' step '+$i+' '+$step.kind
  switch -CaseSensitive([string]$step.kind) {
   'cast-standard-scroll' { Assert-KmcStagedStandardScrollCast $step $Rider $Mount $Tb $Items $why }
   'attack-ranged' { Assert-KmcStagedRangedAttack $step $Rider $Mount $Tb $why }
   default { Assert-KmcStagedMoveStep $step $Rider $Mount $Tb $why }
  }
 }
 $why6d='6D '+$Row.name
 switch -CaseSensitive($Row.name) {
  'C6D-move-cast-move' {
   # Both legs and the cast are one activation: no refreshed turn between them, one rider cost (the cast).
   Assert-KmcStagedSameActivation $Row @(0,1,2) $Rider $Mount $Tb ($why6d+' same activation')
  }
  'C6D-double-move-ranged' {
   # The mount's two Moves and the rider's ranged attack are one activation; the rider's Standard is
   # available immediately before the attack and the attack is the activation's single rider cost. The
   # later scroll cast is a separate next-turn control (frozen 205 TB: three native turn ends and a round
   # rollover before it) and proves nothing about retention during the movement.
   Assert-KmcStagedSameActivation $Row @(0,1) $Rider $Mount $Tb ($why6d+' same activation')
   Need ((Get-KmcStagedNumber $steps[1].before.rider.standard 'standard')-le.001-and$steps[1].before.riderHasStandard-eq$true) 'rider Standard was not available immediately before the ranged attack'
   if($Tb){Need ($steps[1].before.mountUsedTwoMove-eq$true-and(Get-KmcStagedNumber $steps[1].before.mount.move 'move')-gt3.001) 'the ranged attack did not follow the mount''s two native Moves inside the same activation'}
  }
 }
 if($Tb) {
  switch -CaseSensitive($Row.name) {
   'C6D-move-cast-move' {
    # Both short legs fit the mount's first Move; the cast between them changed nothing for the mount.
    Need ((Get-KmcStagedNumber $e.after.mount.move 'move')-le3.001-and$e.after.mountUsedTwoMove-ne$true) 'two short legs exceeded one native Move'
    Need ((Get-KmcStagedNumber $steps[2].legs[0].after.mount.move 'move')-gt(Get-KmcStagedNumber $steps[0].legs[0].after.mount.move 'move')+.001) 'second leg did not continue the same native Move budget'
    # The second leg starts from the first leg's retained allocation (no refresh) and consumes the mount's
    # remaining native movement time of that same allocation.
    $second=$steps[2].legs[0]
    Need ($second.before.mountUsedOneMove-eq$true-and(Test-KmcStagedClose $second.before.mount.move $steps[0].legs[0].after.mount.move 'move')) 'second leg did not start from the first leg''s retained native Move allocation'
    Need ((Get-KmcStagedNumber $second.after.mount.remainingNativeTime 'remainingNativeTime')-lt(Get-KmcStagedNumber $second.before.mount.remainingNativeTime 'remainingNativeTime')-.001-and(Get-KmcStagedNumber $second.before.mount.remainingNativeTime 'remainingNativeTime')-lt5.999) 'second leg did not consume the retained native movement time'
   }
   'C6D-cast-then-move' {
    # The mount's remaining movement follows the mount's own unused Standard, not the rider's spent one.
    Need ($steps[1].legs[0].before.mountHasMove-eq$true-and(Test-KmcStagedClose $steps[1].legs[0].before.mount.standard 0.0 'standard')-and(Get-KmcStagedNumber $steps[1].legs[0].before.rider.standard 'standard')-ge5.999) 'movement after the cast was not governed by the mount''s own actions'
   }
   'C6D-auto-stop-boundary' {
    $leg=$steps[0].legs[0]
    if($AutoStop-eq$true){Need ((Test-KmcStagedClose $leg.after.mount.move 3.0 'move')-and$leg.after.mountUsedTwoMove-ne$true) 'auto-stop did not stop the extended leg at the one-Move boundary'}
    else {Need ((Get-KmcStagedNumber $leg.after.mount.move 'move')-gt3.001) 'extended leg did not continue into the second Move without auto-stop'}
    Need ($steps[1].admittedShellCount-eq1-and$steps[1].turnActor-ceq$Rider) 'rider-led boundary ended before the cast'
   }
  }
 }
}
function Assert-KmcChunk6dStagedEvidence {
 param([Parameter(Mandatory=$true)]$Request,[Parameter(Mandatory=$true)]$Artifact,[AllowNull()][string]$Status)
 if($Request.scenario-cnotin@('chunk6d-staged-rt','chunk6d-staged-tb')-or$Artifact.schemaVersion-ne45){throw '6D scenario/schema mismatch'}
 if($Artifact.status-ceq'FAIL') {
  if($Status-ceq'PASS'-or@($Artifact.errors).Count-eq0-or$Artifact.subscenarioFailCount-lt1){throw '6D incomplete observation cannot become PASS'}
  return
 }
 if($Artifact.status-cne'PASS'-or@($Artifact.errors).Count-ne0-or@($Artifact.rows|Where-Object status -CNE 'PASS').Count-ne0-or$Artifact.subscenarioFailCount-ne0-or$Artifact.subscenarioPassCount-ne@($Artifact.rows).Count){throw '6D failed/incomplete native envelope'}
 $o=$Artifact.observations.chunk6dStaged
 if($null-eq$o-or$o.contract-cne'native-mounted-staged-actions-v1'-or$o.rider-cne$Artifact.observations.riderId-or$o.mount-cne$Artifact.observations.horseId-or$o.mountBlueprint-cne'e7aa96d15a45238438ae4cfb476f6bb9'){throw '6D exact original-pair observation missing'}
 $tb=$Request.scenario.EndsWith('-tb')
 if($o.mode-cne$(if($tb){'TB'}else{'RT'})-or$o.mounted-ne$true){throw '6D mode/surface differs'}
 $expected=@(Get-KmcChunk6dStagedCases)
 if(($o.cases|ConvertTo-Json -Compress)-cne($expected|ConvertTo-Json -Compress)){throw '6D registered row order differs'}
 foreach($name in $expected){if((@($o.plans.$name)-join',')-cne(@(Get-KmcChunk6dStagedPlan $name)-join',')){throw ('6D registered plan differs: '+$name)}}
 if($o.autoStopAfterFirstMoveAction-isnot[bool]){throw '6D native auto-stop setting unobserved'}
 if($o.rangedWeapon.ranged-ne$true-or$o.rangedWeaponReleased-ne$true){throw '6D stock ranged weapon lease missing or retained'}
 foreach($name in $expected){$rows=@($Artifact.rows|Where-Object name -CEQ $name);if($rows.Count-ne1){throw ('6D required row missing or duplicated: '+$name)};Assert-KmcChunk6dStagedRow $rows[0] $o.rider $o.mount $tb @($o.items) $o.autoStopAfterFirstMoveAction}
 Assert-KmcCastingFixtureClosure $o $Artifact '6D'
 if($Status-ceq'PASS'-and$Artifact.status-cne'PASS'){throw '6D failed native observation cannot be promoted'}
}
# Shared disposable-fixture closure rules (traces retired, no residue, items released with their
# stacks conserved, summons gone), identical in wording to the 6C validator's envelope rules.
function Assert-KmcCastingFixtureClosure($o,$Artifact,[string]$Tag) {
 if($o.castTrace.faults-ne0-or$o.castTrace.dropped-ne0-or$o.castTrace.identityRegistry.faults-ne0-or$o.castTrace.identityRegistry.released-ne$true-or$o.castTrace.identityRegistry.retainedCount-ne0-or$o.costTrace.observationErrors-ne0-or$o.costTrace.dropped-ne0-or$o.costTrace.identityRegistry.faults-ne0-or$o.costTrace.identityRegistry.released-ne$true-or$o.costTrace.identityRegistry.retainedCount-ne0){throw ($Tag+' observer incomplete or ownership not retired')}
 if($o.final.riderCommandsEmpty-ne$true-or$o.final.mountCommandsEmpty-ne$true-or$o.final.processesSettled-ne$true-or$o.final.nativeAbilitiesPending-ne$false-or$o.final.projectilesPending-ne$false){throw ($Tag+' final native residue remains')}
 foreach($item in @($o.items)){if($item.disposed-ne$true-or$item.slotRestored-ne$true-or$item.noOwnedItemResident-ne$true){throw ($Tag+' disposable native item ownership remains')}}
 foreach($item in @($o.items)) {
  if($item.requestedCount-isnot[int]-and$item.requestedCount-isnot[long]){throw ($Tag+' disposable item lacks its requested count')}
  if($item.equipped.exactSlot-ne$true-or$item.equipped.count-ne1-or$item.equipped.item-eq0){throw ($Tag+' disposable item was not equipped as one exact unit')}
  if($item.requestedCount-gt1-and($item.stacked.exactSlot-ne$true-or$item.stacked.count-ne$item.requestedCount-or$item.stacked.item-ne$item.equipped.item)){throw ($Tag+' disposable stack was not built on the equipped entity')}
  if($item.blueprint-cin@('d52566ae8cbe8dc4dae977ef51c27d91','cd635d5720937b044a354dba17abad8d')) {
   $spends=@($Artifact.rows|ForEach-Object {if($_.evidence.PSObject.Properties['events']){@($_.evidence.events)}}|Where-Object {$_.kind-ceq'item-spend-after'-and$_.identity-eq$item.equipped.item-and$_.result-eq$true}).Count
   if($item.beforeCleanup.exactSlot-ne$true-or$item.beforeCleanup.count-lt1-or$item.beforeCleanup.count-ne($item.requestedCount-$spends)){throw ($Tag+' disposable stack left its slot or its count does not conserve the observed native spends')}
  }
 }
 if(@($o.summonCleanup|Where-Object {$_.inState-ne$false-or$_.worldContains-ne$false}).Count-ne0){throw ($Tag+' fixture summoned actor remains')}
}
