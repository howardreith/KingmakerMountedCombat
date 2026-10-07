function New-ChargeContinuationFixture([single]$Initial=5,[single]$Delta=1,[int]$Resolved=0){
 function Clone($v){$v|ConvertTo-Json -Depth 40 -Compress|ConvertFrom-Json}
 $state=@{
  rider=@{actor='rider';actorObject=1;inCombat=$true;prepared=$true;waitingInitiative=$false;grantSequence=0;standard=$Initial;move=0.0;swift=0.0}
  mount=@{actor='mount';actorObject=2;inCombat=$true;prepared=$true;waitingInitiative=$false;grantSequence=0;standard=0.0;move=0.0;swift=0.0}
 }
 $ctx=@{frame=1;ticks=100L;round=0}
 $events=New-Object 'Collections.Generic.List[object]';$attacks=New-Object 'Collections.Generic.List[object]'
 function Point {Clone @{frame=$ctx.frame;gameTicks=$ctx.ticks;allocationSequence=$events.Count;rider=$state.rider;mount=$state.mount;resolved=$Resolved+$ctx.round;rounds=$ctx.round}}
 function Event([string]$Boundary,[string]$Actor,[int]$Command=0,[int]$Rule=0){
  $events.Add((Clone @{sequence=$events.Count+1;frame=$ctx.frame;gameTicks=$ctx.ticks;boundary=$Boundary;state=$state[$Actor];gameDeltaTime=$Delta;nativeTurnBased=$false;command=$Command;commandActor=$Actor;actionType='Standard';acted=$true;ignoreCooldown=$false;timeSinceStart=0.02;commandType='KingmakerMountedCombat.Integration.MountedPairAttackCommand';callbackObject=$Rule}))
 }
 $before=Point
 for($round=1;$round-le2;$round++){
  $bounded=0
  while($state.rider.standard-gt0){
   if(++$bounded-gt2048){throw 'Synthetic cooldown generation bound'}
   $ctx.frame++;$ctx.ticks += [long]([Math]::Round([double]$Delta*1000,0,[MidpointRounding]::AwayFromZero)*10000)
   foreach($role in @('rider','mount')){
    Event 'cooldown-tick-before' $role
    $prior=$state[$role].standard
    foreach($field in @('standard','move','swift')){$state[$role][$field]=[single][Math]::Max([single]0,[single]([single]$state[$role][$field]-$Delta))}
    if($prior-gt0-and$state[$role].standard-eq0){Event 'round-state-before' $role;Event 'round-state-after' $role}
    Event 'cooldown-tick-after' $role
   }
  }
  $ctx.round=$round
  Event 'cost-before' 'rider' (100+$round)
  $state.rider.standard=[single]([single]6-[single]0.02)
  Event 'cost-after' 'rider' (100+$round)
  foreach($boundary in @('attack-before','attack-resolved','attack-after')){
   Event ('continuation-'+$boundary) 'rider' (100+$round) (200+$round)
   $attacks.Add((Clone @{boundary=$boundary;allocationSequence=$events.Count;rule=200+$round;actor='rider';target='target';charge=$false;opportunity=$false;fullAttack=$false;attackNumber=0;attacksCount=1}))
  }
 }
 $after=Point
 $p=Clone @{contract='native-rt-charge-continuation-v1';closed=$true;riderId='rider';mountId='mount';targetId='target';before=$before;after=$after;
  trace=@{events=@($events.ToArray());dropped=0;observationErrors=0;observerHooks=@(@('06000C3C','0600C3BE','0600934A','0600939D','060093A4','06009120','0600838F','060026B2')|ForEach-Object {@{token=$_;moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'}})};
  attacks=@($attacks.ToArray());errors=@()}
 $queued=Clone @{rider=@{Id='rider';standard=$Initial;move=0;swift=0};mount=@{Id='mount';standard=0;move=0;swift=0};detail=@{target='target';resolved=$Resolved;riderRounds=0}}
 $done=Clone @{detail=@{resolved=$Resolved+2}}
 [pscustomobject]@{proof=$p;queued=$queued;done=$done}
}
