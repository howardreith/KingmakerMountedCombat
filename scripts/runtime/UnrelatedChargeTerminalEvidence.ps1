# Exact unrelated native Charge evidence. Supplements the complete original Charge validator.
function Assert-KmcUnrelatedChargeTerminal($Artifact,[string]$ExpectedMode) {
 function Need-Unrelated([bool]$Value,[string]$Reason) { if(-not $Value){throw ('Unrelated native Charge: '+$Reason)} }

 function Bool-Unrelated($Value,[bool]$Expected) { return ($Value-is[bool]-and$Value-eq$Expected) }
 function Integer-Unrelated($Value) { return ($Value -is [int] -or $Value -is [long]) }
 function Number-Unrelated($Value) {
  Need-Unrelated ($null-ne$Value-and($Value-is[double]-or$Value-is[float]-or$Value-is[decimal]-or(Integer-Unrelated $Value))) 'numeric evidence absent'
  $n=[double]$Value;Need-Unrelated (-not[double]::IsNaN($n)-and-not[double]::IsInfinity($n)) 'nonfinite numeric evidence';return $n
 }
 Need-Unrelated ($ExpectedMode-cin@('RT','TB')) 'mode absent'
 $rows=@($Artifact.rows|Where-Object name -CEQ 'C4-CHARGE-unrelated-actor')
 Need-Unrelated ($rows.Count-eq1-and$rows[0].status-ceq'PASS') 'exact unrelated row absent'
 $e=$rows[0].evidence;$actor=[string]$e.actorId
 Need-Unrelated ($e.mode-ceq$ExpectedMode-and-not[string]::IsNullOrEmpty($actor)-and$actor-ceq$e.before.actor.id-and
  $actor-cne$e.before.rider.id-and$actor-cne$e.before.mount.id-and$e.before.rider.id-cne$e.before.mount.id) 'distinct actual actor identity differs'
 Need-Unrelated ((Bool-Unrelated $e.actorIsRider $false)-and(Bool-Unrelated $e.actorIsMount $false)-and(Bool-Unrelated $e.pairMounted $true)-and
  $e.before.relationship-ceq'Mounted'-and$e.after.relationship-ceq'Mounted'-and(Bool-Unrelated $e.nativeChargeCompleted $true)-and
  (Bool-Unrelated $e.clicked $true)-and(Bool-Unrelated $e.hoverPure $true)-and(Integer-Unrelated $e.warningDelta)-and$e.warningDelta-eq0) 'native input or unchanged pair evidence absent'
 $input=$e.inputWindow
 Need-Unrelated ($input.actorsAfterInput.actor.id-ceq$actor-and$input.actorsBefore.actor.id-ceq$actor-and
  $input.actorsAfterInput.actor.raw-is[Array]-and$input.actorsAfterInput.actor.queue-is[Array]) 'input actor or command lists absent'
 $admitted=@(@($input.actorsAfterInput.actor.raw)+@($input.actorsAfterInput.actor.queue)|Where-Object {$null-ne$_})
 Need-Unrelated ($admitted.Count-eq1) 'input did not own exactly one command'
 $shell=$admitted[0];$chargeCommandId=$shell.id
 Need-Unrelated ((Integer-Unrelated $chargeCommandId)-and$chargeCommandId-ne0-and$shell.type-ceq'Kingmaker.UnitLogic.Commands.UnitUseAbility'-and
  $shell.executor-ceq$actor-and(Bool-Unrelated $shell.started $false)-and(Bool-Unrelated $shell.acted $false)-and(Bool-Unrelated $shell.finished $false)) 'exact pending input shell absent'
 $trace=$Artifact.observations.'chargeTraceBefore-C4-CHARGE-queued-state-change'
 Need-Unrelated ($null-ne$trace-and(Integer-Unrelated $trace.dropped)-and$trace.dropped-eq0-and$trace.events-is[Array]) 'complete exact retained trace absent'
 $events=@($trace.events|Where-Object caseId -CEQ 'C4-CHARGE-unrelated-actor')
 Need-Unrelated ($events.Count-gt0) 'exact case trace absent'
 $previous=-1;$frame=-1;$ticks=-1
 foreach($v in $events){
  Need-Unrelated ((Integer-Unrelated $v.index)-and$v.index-gt$previous-and(Integer-Unrelated $v.frame)-and$v.frame-ge$frame-and
   (Integer-Unrelated $v.gameTime)-and$v.gameTime-ge$ticks-and$v.actor-ceq$actor-and$v.relationship-ceq'Mounted') 'trace sequence, native time, actor or pair differs'
  $previous=$v.index;$frame=$v.frame;$ticks=$v.gameTime
 }
 $shellEvents=@($events|Where-Object command -EQ $chargeCommandId)
 Need-Unrelated ($shellEvents.Count-gt0) 'input shell has no native observations'
 foreach($v in $shellEvents){Need-Unrelated ($v.commandType-ceq$shell.type-and($v.executor-ceq$actor-or($null-eq$v.executor-and$v.boundary-cin@('run-before','private-run-before')))-and$v.spellBlueprint-ceq'c78506dd0e14f7c45a599990e4e65038') 'shell identity changed'}
 function One-Unrelated($Items,[string]$Boundary) {
  $found=@($Items|Where-Object boundary -CEQ $Boundary);Need-Unrelated ($found.Count-eq1) ('missing or repeated '+$Boundary);return $found[0]
 }
 $admission=One-Unrelated $shellEvents 'run-after'
 Need-Unrelated ($admission.frame-eq$input.inputFrame-and$admission.gameTime-eq$input.gameTimeBefore-and
  (Bool-Unrelated $admission.started $false)-and(Bool-Unrelated $admission.acted $false)-and(Bool-Unrelated $admission.finished $false)) 'observed input admission differs'
 $actionBefore=One-Unrelated $shellEvents 'charge-action-before';$actionAfter=One-Unrelated $shellEvents 'charge-action-after'
 $shellCostBefore=One-Unrelated $shellEvents 'cost-before';$shellCostAfter=One-Unrelated $shellEvents 'cost-after'
 $shellEnd=One-Unrelated $shellEvents 'ended'
 Need-Unrelated ($admission.index-lt$actionBefore.index-and$actionBefore.index-lt$actionAfter.index-and$actionAfter.index-lt$shellCostBefore.index-and
  $shellCostBefore.index-lt$shellCostAfter.index-and$shellCostAfter.index-lt$shellEnd.index-and(Bool-Unrelated $actionBefore.acted $false)-and
  (Bool-Unrelated $shellCostBefore.acted $true)-and(Bool-Unrelated $shellCostAfter.acted $true)-and(Bool-Unrelated $shellEnd.acted $true)-and(Bool-Unrelated $shellEnd.finished $true)-and$shellEnd.result-ceq'Success') 'shell acted/cost/terminal ordering differs'
 foreach($v in @($shellCostBefore,$shellCostAfter)){Need-Unrelated ($v.frame-eq$shellCostBefore.frame-and$v.gameTime-eq$shellCostBefore.gameTime) 'cost crossed a native boundary'}
 $standardBefore=Number-Unrelated $shellCostBefore.standard;$standardAfter=Number-Unrelated $shellCostAfter.standard
 $moveBefore=Number-Unrelated $shellCostBefore.move;$moveAfter=Number-Unrelated $shellCostAfter.move
 $expectedStandard=if($ExpectedMode-ceq'TB'){$standardBefore+6}else{6-(Number-Unrelated $shellCostAfter.timeSinceStart)}
 $expectedMove=if($ExpectedMode-ceq'TB'){$moveBefore+3}else{$moveBefore}
 Need-Unrelated ([Math]::Abs($standardAfter-$expectedStandard)-le0.00001-and[Math]::Abs($moveAfter-$expectedMove)-le0.00001) 'exact native Charge cost differs'
 $attacks=@($events|Where-Object {$_.boundary-ceq'start-after'-and$_.commandType-ceq'Kingmaker.UnitLogic.Commands.UnitAttack'})
 Need-Unrelated ($attacks.Count-eq1-and(Integer-Unrelated $attacks[0].command)-and$attacks[0].command-ne0-and$attacks[0].command-ne$chargeCommandId) 'unique native attack absent'
 $attackId=$attacks[0].command;$target=$attacks[0].target
 Need-Unrelated (-not[string]::IsNullOrEmpty($target)-and$target-ceq$e.samples[0].shell.targetId) 'exact native target differs'
 $attackEvents=@($events|Where-Object command -EQ $attackId)
 foreach($v in $attackEvents){Need-Unrelated ($v.commandType-ceq'Kingmaker.UnitLogic.Commands.UnitAttack'-and$v.executor-ceq$actor-and$v.target-ceq$target) 'attack identity changed'}
 $delivery=One-Unrelated $attackEvents 'delivery-before';$delivered=One-Unrelated $attackEvents 'delivery-after'
 $costBefore=One-Unrelated $attackEvents 'cost-before';$costAfter=One-Unrelated $attackEvents 'cost-after';$end=One-Unrelated $attackEvents 'ended'
 Need-Unrelated ($attacks[0].index-lt$delivery.index-and$delivery.index-lt$delivered.index-and$delivered.index-lt$costBefore.index-and
  $costBefore.index-lt$costAfter.index-and$costAfter.index-lt$end.index-and(Bool-Unrelated $costBefore.acted $true)-and(Bool-Unrelated $end.acted $true)-and(Bool-Unrelated $end.finished $true)-and
  (Integer-Unrelated $end.planned)-and$end.planned-eq1-and(Integer-Unrelated $end.completed)-and$end.completed-eq1) 'attack completion absent'
 foreach($field in @('standard','move','frame','gameTime')){Need-Unrelated ((Number-Unrelated $costBefore.$field)-eq(Number-Unrelated $costAfter.$field)) 'charge attack added a second cost'}
 if($end.result-ceq'Interrupt'){
  $recovery=One-Unrelated $attackEvents 'native-recovery-interrupt'
  Need-Unrelated ($recovery.index-gt$costAfter.index-and$recovery.index-lt$end.index-and(Bool-Unrelated $recovery.acted $true)-and$recovery.completed-eq1-and
   $recovery.detail.Contains('Kingmaker.UnitLogic.Commands.UnitAttack.OnTick')) 'interrupt lacks completed native recovery'
 }else{Need-Unrelated ($end.result-ceq'Success') 'attack terminal result differs'}
 $rules=@($e.rules.attackRuleEvents)
 Need-Unrelated ($rules.Count-eq1-and$rules[0].actorId-ceq$actor-and$rules[0].targetId-ceq$target-and(Bool-Unrelated $rules[0].charge $true)-and
  (Bool-Unrelated $rules[0].attackOfOpportunity $false)-and$rules[0].frame-ge$delivery.frame-and$rules[0].frame-le$delivered.frame-and
  (Integer-Unrelated $e.rules.riderResolved)-and$e.rules.riderResolved-eq1-and(Integer-Unrelated $e.rules.riderNonOpportunityAttackRules)-and$e.rules.riderNonOpportunityAttackRules-eq1-and(Integer-Unrelated $e.rules.pairForcedD20)-and$e.rules.pairForcedD20-eq0) 'exact native charge rule differs'
 $foreign=@($events|Where-Object {$_.command-ne0-and$_.command-ne$chargeCommandId-and$_.command-ne$attackId})
 Need-Unrelated ($foreign.Count-eq0) 'another command entered the case'
}
