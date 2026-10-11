Set-StrictMode -Version Latest
function Assert-KmcNativePassiveResources($P,[switch]$Ground) {
 function Int($v){if($v -isnot [int] -and $v -isnot [long]){throw 'Passive integer missing'};[long]$v}
 function Bool($v){if($v -isnot [bool]){throw 'Passive bool missing'};[bool]$v}
 function Number($v){if($v -isnot [int] -and $v -isnot [long] -and $v -isnot [double] -and $v -isnot [single] -and $v -isnot [decimal]){throw 'Passive number missing'};$d=[double]$v;if([double]::IsNaN($d) -or [double]::IsInfinity($d)){throw 'Passive nonfinite value'};$d}
 function Equal($a,$b){
  foreach($f in @('actor','actorObject','inCombat','grantSequence','reactionsPerRound','initiativeOrder','reactions')){
   if($null -eq $a.$f -or $null -eq $b.$f -or $a.$f -cne $b.$f){throw ('Passive resource identity/discrete change '+$f)}
  }
  foreach($s in @($a,$b)){
   if([string]::IsNullOrWhiteSpace($s.actor) -or (Int $s.actorObject) -eq 0){throw 'Passive actor identity missing'}
   foreach($f in @('grantSequence','reactionsPerRound','initiativeOrder','reactions')){$null=Int $s.$f};$null=Bool $s.inCombat
  }
  foreach($f in @('standard','move','swift','reactionCooldown','initiativeCooldown')){if([Math]::Abs((Number $a.$f)-(Number $b.$f)) -gt 0.0001){throw ('Passive unexplained resource '+$f)}}
 }
 function Expected($e){
  $s=$e.state|ConvertTo-Json -Depth 20|ConvertFrom-Json
  $delta=Number $e.gameDeltaTime;if($delta -lt 0){throw 'Negative native delta'}
  $tb=Bool $e.nativeTurnBased;$passing=Bool $e.nativePassing;$surprised=Bool $e.nativeSurprised;$waiting=Bool $e.state.waitingInitiative
  $inCombat=Bool $s.inCombat
  $decay=$delta
  if($tb -and $inCombat){
   if(-not $passing){$decay=0.0}
   elseif($s.initiativeCooldown -gt 0 -and -not $surprised){$used=[Math]::Min($decay,[double]$s.initiativeCooldown);$s.initiativeCooldown-=$used;$decay-=$used}
   if($decay -gt 0){$s.reactionCooldown=[Math]::Max(0.0,[double]$s.reactionCooldown-$decay)}
  } elseif($tb -and -not $passing){$decay=0.0}
  elseif($waiting){$s.initiativeCooldown=[Math]::Max(0.0,[double]$s.initiativeCooldown-$delta);$decay=0.0}
  else {
   $s.reactionCooldown=[Math]::Max(0.0,[double]$s.reactionCooldown-$delta)
   if($s.reactionCooldown -le 0 -and $s.reactionsPerRound -gt 0 -and $s.reactions -le $s.reactionsPerRound){$s.reactions=$s.reactionsPerRound}
  }
  if($decay -gt 0){foreach($f in @('standard','move','swift')){$s.$f=[Math]::Max(0.0,[double]$s.$f-$decay)}}
  $s
 }
 $contract=if($Ground){'one-native-ground-command-outside-combat-with-observed-time-only'}else{'same-allocation-no-command-cost-with-observed-native-time-only'}
 if($P.contract -cne $contract -or -not (Bool $P.traceComplete)){throw 'Passive contract or trace incomplete'}
 $tb=Bool $P.turnBased;$b=$P.before;$a=$P.after
 if([string]::IsNullOrWhiteSpace($P.riderId) -or [string]::IsNullOrWhiteSpace($P.mountId) -or $P.riderId -ceq $P.mountId){throw 'Passive pair identity missing'}
 if($P.events -isnot [Array] -or $P.observerHooks -isnot [Array]){throw 'Passive evidence arrays missing'}
 foreach($token in @('0600934A','060093A1','060093A4','06000C3C','0600C3BE','06009120','0600838F','060026B2','06000C37')){
  $h=@($P.observerHooks|Where-Object token -CEQ $token)
  if($h.Count -ne 1 -or $h[0].moduleMvid -cne '07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'){throw 'Passive required native hook missing'}
 }
 if($Ground){
  if($P.cooldownEligibilityContract -cne 'observed-native-outside-combat-skip'){throw 'Ground native eligibility contract missing'}
  $hooks=@($P.observerHooks|Where-Object token -CEQ '06009343')
  if($hooks.Count-ne1-or$hooks[0].moduleMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'-or$hooks[0].method-cne'Kingmaker.Controllers.Combat.BaseUnitCombatController.ShouldTickOnUnit'-or$hooks[0].prefix-cne'CooldownEligibilityBefore'-or$hooks[0].postfix-cne'CooldownEligibilityAfter'){throw 'Ground eligibility hook differs'}
  $command=Int $P.groundCommand.commandObject;$mover=[string]$P.groundCommand.casterId
  if($tb-or$command-eq0-or$mover-cnotin@($P.riderId,$P.mountId)){throw 'Ground input identity/mode differs'}
  foreach($role in @('rider','mount')){foreach($s in @($b.$role,$a.$role)){if((Bool $s.inCombat)-or(Int $s.grantSequence)-ne0){throw 'Ground setup reused an encounter allocation'}}}
  $controls=@($P.events|Where-Object {$_.state.actor-cin@($P.riderId,$P.mountId)-and$_.boundary-cmatch'admission|cost'})
  $expected=@('admission-before','admission-after','cost-before','cost-after')
  if($controls.Count-ne4){throw 'Ground admission/cost callback count differs'}
  for($i=0;$i-lt4;$i++){
   $e=$controls[$i]
   if($e.boundary-cne$expected[$i]-or(Int $e.command)-ne$command-or$e.state.actor-cne$mover-or$e.commandType-cne'Kingmaker.UnitLogic.Commands.UnitMoveTo'-or$e.actionType-cne'Move'-or(Bool $e.state.inCombat)-or(Bool $e.simulatingClick)-or(Bool $e.acted)-ne($i-ge2)-or($i-eq0-and$null-ne$e.commandActor)-or($i-gt0-and$e.commandActor-cne$mover)){throw 'Ground exact callback identity differs'}
  }
 }
 $sequence=Int $b.allocationSequence;$end=Int $a.allocationSequence;$time=Int $b.gameTicks;$endTime=Int $a.gameTicks
 if($sequence -lt 0 -or $end -lt $sequence -or $time -lt 0 -or $endTime -lt $time -or (Int $a.frame) -lt (Int $b.frame)){throw 'Passive window order differs'}
 $current=@{};$pending=@{};$ticks=@{};$eligibility=@{};$skips=@{};$cooldownController=0L
 foreach($pair in @(@('rider',$P.riderId),@('mount',$P.mountId))){
  $s=$b.($pair[0]);Equal $s $s
  if($s.actor -cne $pair[1]){throw 'Passive baseline actor differs'}
  $current[$pair[1]]=$s;$ticks[$pair[1]]=0;$skips[$pair[1]]=0
 }
 if($current[$P.riderId].actorObject -eq $current[$P.mountId].actorObject){throw 'Passive native pair objects alias'}
 foreach($e in $P.events){
  if((Int $e.sequence) -ne ++$sequence -or (Int $e.gameTicks) -lt $time -or (Int $e.gameTicks) -gt $endTime){throw 'Passive event sequence/time differs'}
  $time=Int $e.gameTicks;$actor=[string]$e.state.actor
  if([string]::IsNullOrWhiteSpace($actor)){throw 'Passive event actor missing'}
  if(-not $current.ContainsKey($actor)){continue}
  if((Bool $e.nativeTurnBased) -ne $tb){throw 'Passive native mode changed'}
  $boundary=[string]$e.boundary
  $groundBoundary=$Ground-and$boundary-cin@('admission-before','admission-after','cost-before','cost-after')
  if([string]::IsNullOrWhiteSpace($boundary) -or (-not$groundBoundary-and$boundary -cmatch 'cost|prepare|clear|opportunity|admission|movement|turn-end')){throw 'Passive native cost/grant/reaction/movement or allocation change occurred'}
  if($Ground-and$boundary.StartsWith('cooldown-tick-',[StringComparison]::Ordinal)){throw 'Outside-combat ground actor unexpectedly received a native cooldown tick'}
  if($Ground-and$boundary-cin@('cooldown-eligibility-before','cooldown-eligibility-after')){
   $callback=Int $e.callbackObject
   if($callback-eq0-or($cooldownController-ne0-and$callback-ne$cooldownController)-or(Bool $e.state.inCombat)-or(Bool $e.simulatingClick)-or(Int $e.command)-ne0-or(Int $e.frame)-lt(Int $b.frame)-or(Int $e.frame)-gt(Int $a.frame)){throw 'Ground eligibility context differs'}
   $cooldownController=$callback;Equal $current[$actor] $e.state
   if($boundary-ceq'cooldown-eligibility-before'){
    if($eligibility.ContainsKey($actor)-or$e.detail-cne'Kingmaker.Controllers.Combat.UnitCombatCooldownsController'){throw 'Ground eligibility entry differs'}
    $eligibility[$actor]=$e
   }else{
    if(-not$eligibility.ContainsKey($actor)-or(Int $eligibility[$actor].callbackObject)-ne$callback-or(Int $eligibility[$actor].frame)-ne(Int $e.frame)-or(Int $eligibility[$actor].gameTicks)-ne(Int $e.gameTicks)-or$e.detail-cne'eligible=False'){throw 'Ground native skip result missing'}
    $eligibility.Remove($actor);$skips[$actor]++
   }
   continue
  }
  if(-not$Ground-and$boundary.StartsWith('cooldown-eligibility-',[StringComparison]::Ordinal)){throw 'Ground eligibility leaked into passive window'}
  if($boundary -ceq 'cooldown-tick-after'){
   if(-not $pending.ContainsKey($actor)){throw 'Passive native tick end without entry'}
   Equal $pending[$actor] $e.state;$pending.Remove($actor);$current[$actor]=$e.state;$ticks[$actor]++;continue
  }
  if($pending.ContainsKey($actor)){Equal $pending[$actor] $e.state}else{Equal $current[$actor] $e.state}
  if($boundary -ceq 'cooldown-tick-before'){
   if($pending.ContainsKey($actor)){throw 'Passive nested native tick'}
   $pending[$actor]=Expected $e
  }
 }
 if($sequence -ne $end -or $pending.Count -ne 0 -or $eligibility.Count -ne 0){throw 'Passive trace did not close'}
 foreach($pair in @(@('rider',$P.riderId),@('mount',$P.mountId))){
  Equal $current[$pair[1]] $a.($pair[0])
  if($endTime -ne (Int $b.gameTicks)){
   if($Ground){if($skips[$pair[1]]-eq0){throw 'Elapsed ground window lacks exact native skip for an actor'}}
   elseif($ticks[$pair[1]]-eq0){throw 'Elapsed passive window lacks native tick for an actor'}
  }
 }
}

function Assert-KmcNativeGroundResources($P){Assert-KmcNativePassiveResources $P -Ground}
