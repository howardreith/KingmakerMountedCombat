Set-StrictMode -Version Latest
# Acceptance lives here. Native code records exact callbacks and makes no clock-based
# readiness decision. Float subtraction and native cost callbacks explain every change.
function Assert-KmcChargeContinuation($P,$Queued,$Done) {
 function N($v){if($v-isnot[int]-and$v-isnot[long]-and$v-isnot[single]-and$v-isnot[double]-and$v-isnot[decimal]){throw 'Charge continuation number absent'};$n=[double]$v;if([double]::IsNaN($n)-or[double]::IsInfinity($n)){throw 'Charge continuation nonfinite'};$n}
 function I($v){if($v-isnot[int]-and$v-isnot[long]){throw 'Charge continuation integer absent'};[long]$v}
 function B($v){if($v-isnot[bool]){throw 'Charge continuation boolean absent'};[bool]$v}
 function CopyState($s){$s|ConvertTo-Json -Depth 30 -Compress|ConvertFrom-Json}
 function Same($a,$b){
  if([string]::IsNullOrEmpty($a.actor)-or$a.actor-cne$b.actor-or(I $a.actorObject)-eq0-or(I $a.actorObject)-ne(I $b.actorObject)-or
    -not(B $b.inCombat)-or-not(B $b.prepared)-or(B $b.waitingInitiative)-or(I $b.grantSequence)-ne0){throw 'Charge continuation actor/preparation changed'}
  foreach($f in @('standard','move','swift')){if([Math]::Abs((N $a.$f)-(N $b.$f))-gt0.0001){throw ('Charge continuation unexplained '+$f)}}
 }
 function Decayed($e){
  $s=CopyState $e.state;$delta=[single](N $e.gameDeltaTime)
  if($delta-lt0-or(B $e.nativeTurnBased)-or(B $e.state.waitingInitiative)){throw 'Charge continuation native RT tick differs'}
  foreach($f in @('standard','move','swift')){$s.$f=[single][Math]::Max([single]0,[single]([single]$s.$f-$delta))}
  $s
 }
 if($P.contract-cne'native-rt-charge-continuation-v1'-or-not(B $P.closed)-or@($P.errors).Count-ne0-or
    (I $P.trace.dropped)-ne0-or(I $P.trace.observationErrors)-ne0){throw 'Charge continuation trace incomplete'}
 $rider=[string]$P.riderId;$mount=[string]$P.mountId
 if([string]::IsNullOrEmpty($rider)-or[string]::IsNullOrEmpty($mount)-or$rider-ceq$mount-or
    $rider-cne$Queued.rider.Id-or$mount-cne$Queued.mount.Id-or$P.targetId-cne$Queued.detail.target){throw 'Charge continuation identity differs'}
 foreach($token in @('06000C3C','0600C3BE','0600934A','0600939D','060093A4','06009120','0600838F','060026B2')){
  $hooks=@($P.trace.observerHooks|Where-Object token -CEQ $token)
  if($hooks.Count-ne1-or$hooks[0].moduleMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'){throw 'Charge continuation native observer hook missing'}
 }
 $before=$P.before;$after=$P.after;$current=@{};$ticks=@{};$costs=@{};$roundPending=@{};$rounds=@{};$tickCounts=@{};$standardCosts=0
 foreach($role in @('rider','mount')){
  $id=if($role-ceq'rider'){$rider}else{$mount};$s=CopyState $before.$role;Same $s $s
  if($s.actor-cne$id){throw 'Charge continuation role changed'}
  foreach($field in @('standard','move','swift')){if([Math]::Abs((N $s.$field)-(N $Queued.$role.$field))-gt0.0001){throw 'Charge continuation queued baseline differs'}}
  $current[$id]=$s;$rounds[$id]=0;$tickCounts[$id]=0
 }
 if($current[$rider].actorObject-eq$current[$mount].actorObject-or(N $current[$rider].standard)-le0){throw 'Charge continuation has no exact spent rider'}
 if((I $before.resolved)-ne(I $Queued.detail.resolved)-or(I $before.rounds)-ne(I $Queued.detail.riderRounds)-or
    (I $after.resolved)-ne(I $Done.detail.resolved)-or(I $after.resolved)-(I $before.resolved)-ne2-or
    (I $after.rounds)-(I $before.rounds)-ne2){throw 'Charge continuation endpoint counts differ'}
 $attackFacts=@{};$attackRules=@{};$begun=0;$resolved=0
 foreach($a in @($P.attacks)){
  $seq=I $a.allocationSequence
  if($attackFacts.ContainsKey($seq)-or$a.actor-cne$rider-or$a.target-cne$P.targetId-or(B $a.charge)-or(B $a.opportunity)-or
     (I $a.rule)-eq0-or$a.boundary-cnotin@('attack-before','attack-after','attack-resolved')){throw 'Charge continuation attack identity differs'}
  $attackFacts[$seq]=$a
 }
 if($attackFacts.Count-ne6){throw 'Charge continuation requires exactly two complete native attack observations'}
 $sequence=I $before.allocationSequence;$time=I $before.gameTicks;$frame=I $before.frame;$seenAttacks=0
 if($sequence-ne0-or$P.trace.events-isnot[array]-or@($P.trace.events).Count-gt16000){throw 'Charge continuation trace bounds differ'}
 foreach($e in $P.trace.events){
  if((I $e.sequence)-ne++$sequence-or(I $e.gameTicks)-lt$time-or(I $e.frame)-lt$frame-or(I $e.frame)-gt(I $after.frame)-or(I $e.gameTicks)-gt(I $after.gameTicks)){throw 'Charge continuation event order differs'}
  $time=I $e.gameTicks;$frame=I $e.frame;$id=[string]$e.state.actor
  if(-not$current.ContainsKey($id)){continue}
  if(B $e.nativeTurnBased){throw 'Charge continuation changed mode'}
  $boundary=[string]$e.boundary;$expected=$current[$id]
  if($boundary-ceq'round-state-before'){
   if(-not$ticks.ContainsKey($id)-or$roundPending.ContainsKey($id)-or$ticks[$id].applied-or
      (N $ticks[$id].before.state.standard)-le0-or(N $ticks[$id].expected.standard)-ne0){throw 'Charge continuation round without native expiry'}
   $expected=$ticks[$id].expected;$ticks[$id].applied=$true;$roundPending[$id]=$true
  }elseif($boundary-ceq'cooldown-tick-after'){
   if(-not$ticks.ContainsKey($id)-or$roundPending.ContainsKey($id)){throw 'Charge continuation unmatched tick end'}
   $expected=$ticks[$id].expected
   $crossed=(N $ticks[$id].before.state.standard)-gt0-and(N $expected.standard)-eq0
   if($crossed-ne$ticks[$id].applied){throw 'Charge continuation native expiry/round callback differs'}
  }elseif($boundary-ceq'cost-after'){
   if(-not$costs.ContainsKey($id)){throw 'Charge continuation cost end has no entry'}
   $entry=$costs[$id]
   if((I $entry.command)-ne(I $e.command)-or$entry.actionType-cne$e.actionType-or(B $entry.ignoreCooldown)-ne(B $e.ignoreCooldown)){throw 'Charge continuation cost command changed'}
   $expected=CopyState $current[$id]
   if(-not(B $entry.ignoreCooldown)){
    $field=if($entry.actionType-ceq'Standard'){'standard'}else{'move'}
    $base=if($field-ceq'standard'){[single]6}else{[single]3}
    $expected.$field=[single]($base-[single](N $entry.timeSinceStart))
   }
  }
  Same $expected $e.state
  $current[$id]=CopyState $e.state
  switch -CaseSensitive ($boundary){
   'cooldown-tick-before' {
    if($ticks.ContainsKey($id)-or$costs.ContainsKey($id)){throw 'Charge continuation overlapping tick'}
    $ticks[$id]=@{before=$e;expected=(Decayed $e);applied=$false};$tickCounts[$id]++
   }
   'cooldown-tick-after' {$ticks.Remove($id)}
   'round-state-after' {
    if(-not$roundPending.ContainsKey($id)){throw 'Charge continuation unmatched round end'}
    $roundPending.Remove($id);$rounds[$id]++
   }
   'cost-before' {
    if($costs.ContainsKey($id)-or$ticks.ContainsKey($id)-or(I $e.command)-eq0-or$e.commandActor-cne$id-or-not(B $e.acted)-or(N $e.timeSinceStart)-lt0){throw 'Charge continuation invalid native cost entry'}
    if($e.actionType-cnotin@('Standard','Move')){throw 'Charge continuation unexpected action cost'}
    if(-not(B $e.ignoreCooldown)){
     if($id-ceq$rider){
      if($e.actionType-cne'Standard'-or(N $e.state.standard)-gt0-or$rounds[$id]-ne$standardCosts+1-or
        $e.commandType-cnotin@('Kingmaker.UnitLogic.Commands.UnitAttack','KingmakerMountedCombat.Integration.MountedPairAttackCommand','KingmakerMountedCombat.Integration.MountedPairSingleAttack')){throw 'Charge continuation premature or wrong rider cost'}
      $standardCosts++
     }elseif($e.actionType-cne'Move'-or$e.commandType-cne'Kingmaker.UnitLogic.Commands.UnitMoveTo'){throw 'Charge continuation mount paid a rider action'}
    }
    $costs[$id]=$e
   }
   'cost-after' {$costs.Remove($id)}
   {$_-cmatch'^(prepare-|clear-|combat-clear-|actor-cost-|turn-end-)'} {throw 'Charge continuation preparation, direct cost or lifecycle replay'}
  }
  if($boundary.StartsWith('continuation-attack-', [StringComparison]::Ordinal)){
   if(-not$attackFacts.ContainsKey($sequence)){throw 'Charge continuation attack event lacks raw rule identity'}
   $a=$attackFacts[$sequence];$seenAttacks++
   if($boundary-cne('continuation-'+$a.boundary)-or(I $e.callbackObject)-ne(I $a.rule)-or$id-cne$rider){throw 'Charge continuation rule callback attribution differs'}
   $key=[string]$a.rule
   if($a.boundary-ceq'attack-before'){
    if($attackRules.ContainsKey($key)-or$rounds[$rider]-ne$begun+1){throw 'Charge continuation attack before debt expiry or duplicate attack'}
    $begun++;$attackRules[$key]=@{after=$false;resolved=$false;round=$rounds[$rider]}
   }else{
    if(-not$attackRules.ContainsKey($key)){throw 'Charge continuation orphan attack completion'}
    $part=if($a.boundary-ceq'attack-after'){'after'}else{'resolved'}
    if($attackRules[$key][$part]-or$attackRules[$key].round-ne$rounds[$rider]){throw 'Charge continuation repeated or cross-round delivery'}
    $attackRules[$key][$part]=$true;if($part-ceq'resolved'){$resolved++}
   }
  }
 }
 if($ticks.Count-or$costs.Count-or$roundPending.Count-or$standardCosts-ne2-or$rounds[$rider]-ne2-or$rounds[$mount]-ne0-or
    $tickCounts[$rider]-eq0-or$tickCounts[$mount]-eq0-or$begun-ne2-or$resolved-ne2-or$seenAttacks-ne6-or
    (I $after.allocationSequence)-ne$sequence-or@($attackRules.Values|Where-Object {-not$_.after-or-not$_.resolved}).Count){throw 'Charge continuation incomplete native boundaries'}
 foreach($role in @('rider','mount')){$id=if($role-ceq'rider'){$rider}else{$mount};Same $current[$id] $after.$role}
}
