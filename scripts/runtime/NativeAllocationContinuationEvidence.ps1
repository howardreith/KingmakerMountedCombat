Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'NativeTurnCompletionEvidence.ps1')
function Assert-KmcAllocationContinuation($P){
 function Int($x){if($x-isnot[int]-and$x-isnot[long]){throw 'Continuation integer absent'};[long]$x}
 function Bool($x){if($x-isnot[bool]){throw 'Continuation boolean absent'};[bool]$x}
 function Num($x){if($x-isnot[int]-and$x-isnot[long]-and$x-isnot[double]-and$x-isnot[single]-and$x-isnot[decimal]){throw 'Continuation number absent'};$n=[double]$x;if([double]::IsNaN($n)-or[double]::IsInfinity($n)){throw 'Continuation nonfinite'};$n}
 function Clone-ContinuationState($x){$x|ConvertTo-Json -Depth 60|ConvertFrom-Json}
 function Same($a,$b){
  if([string]::IsNullOrEmpty($a.actor)-or$a.actor-cne$b.actor-or(Int $a.actorObject)-eq0-or(Int $a.actorObject)-ne(Int $b.actorObject)-or-not(Bool $a.inCombat)-or-not(Bool $b.inCombat)){throw 'Continuation actor identity differs'}
  foreach($f in @('grantSequence','reactionsPerRound','reactions','initiativeOrder')){if((Int $a.$f)-ne(Int $b.$f)){throw ('Continuation discrete resource '+$f)}}
  foreach($f in @('standard','move','swift','reactionCooldown','initiativeCooldown')){if([Math]::Abs((Num $a.$f)-(Num $b.$f))-gt0.0001){throw ('Continuation resource '+$f)}}
 }
 function Matches($a,$b){try{Same $a $b;return $true}catch{return $false}}
 function Cleared($s,[bool]$Prepare){$v=Clone-ContinuationState $s;foreach($f in @('standard','move','swift','reactionCooldown','initiativeCooldown')){$v.$f=0.0};if($Prepare-and$v.reactionsPerRound-gt0-and$v.reactions-le$v.reactionsPerRound){$v.reactions=$v.reactionsPerRound};$v}
 function Tick($e){
  $v=Clone-ContinuationState $e.state;$delta=Num $e.gameDeltaTime;if($delta-lt0-or-not(Bool $e.nativeTurnBased)){throw 'Continuation tick differs'}
  $passing=Bool $e.nativePassing;$surprised=Bool $e.nativeSurprised;$null=Bool $e.state.waitingInitiative
  if(-not$passing){return $v}
  if($v.initiativeCooldown-gt0-and-not$surprised){$used=[Math]::Min($delta,[double]$v.initiativeCooldown);$v.initiativeCooldown-=$used;$delta-=$used}
  if($delta-gt0){foreach($f in @('standard','move','swift','reactionCooldown')){$v.$f=[Math]::Max(0.0,[double]$v.$f-$delta)}}
  $v
 }
 if($P.contract-cne'ordinary-paired-end-through-one-native-next-preparation'-or-not(Bool $P.traceComplete)){throw 'Continuation contract/trace missing'}
 $before=$P.before;$after=$P.after;$rider=[string]$P.riderId;$mount=[string]$P.mountId
 if([string]::IsNullOrEmpty($rider)-or[string]::IsNullOrEmpty($mount)-or$rider-ceq$mount){throw 'Continuation pair missing'}
 $start=Int $before.allocationSequence;$end=Int $after.allocationSequence;$round=Int $before.round
 if($start-lt0-or$end-le$start-or(Int $after.round)-ne$round+1L-or(Int $after.frame)-le(Int $before.frame)-or(Int $after.gameTicks)-lt(Int $before.gameTicks)){throw 'Continuation next-round interval differs'}
 Assert-KmcTurnCompletion $P.completion
 if($P.completion.riderId-cne$rider-or$P.completion.mountId-cne$mount-or$P.events-isnot[array]-or$P.observerHooks-isnot[array]){throw 'Continuation binding missing'}
 foreach($token in @('06000C3C','0600C3BE','0600934A','060093A1','060093A4','06009120','0600838F','060026B2','06000C37')){$h=@($P.observerHooks|Where-Object token -CEQ $token);if($h.Count-ne1-or$h[0].moduleMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'){throw 'Continuation native hook absent'}}
 $completions=@{};foreach($c in $P.completion.calls){foreach($phase in @('before','after')){$seq=Int $c.$phase.allocationSequence;if($seq-le$start-or$seq-gt$end-or$completions.ContainsKey($seq)){throw 'Continuation completion sequence differs'};$completions[$seq]=$c}}
 $current=@{};$stacks=@{};$prepares=@{};$clears=@{}
 foreach($role in @('rider','mount')){$id=if($role-ceq'rider'){$rider}else{$mount};$s=Clone-ContinuationState $before.$role;Same $s $s;if($s.actor-cne$id){throw 'Continuation baseline role differs'};$current[$id]=$s;$stacks[$id]=[Collections.Generic.Stack[object]]::new();$prepares[$id]=0;$clears[$id]=0}
 if($current[$rider].actorObject-eq$current[$mount].actorObject){throw 'Continuation actor alias'}
 $sequence=$start;$ticks=Int $before.gameTicks;$frame=Int $before.frame;$nativeRound=$round;$observed=@{}
 foreach($e in $P.events){
  $sequence++;if((Int $e.sequence)-ne$sequence-or(Int $e.gameTicks)-lt$ticks-or(Int $e.gameTicks)-gt(Int $after.gameTicks)){throw 'Continuation trace gap/clock differs'};$ticks=Int $e.gameTicks
  if((Int $e.frame)-lt$frame-or(Int $e.frame)-gt(Int $after.frame)-or(Int $e.round)-lt$nativeRound-or(Int $e.round)-gt$round+1L){throw 'Continuation frame/round mismatch'};$frame=Int $e.frame;$nativeRound=Int $e.round
  $actor=[string]$e.state.actor;if([string]::IsNullOrEmpty($actor)){throw 'Continuation event actor absent'};if(-not$current.ContainsKey($actor)){continue}
  if(-not(Bool $e.nativeTurnBased)){throw 'Continuation native mode changed'}
  $actual=$e.state;$boundary=[string]$e.boundary;if([string]::IsNullOrEmpty($boundary)){throw 'Continuation boundary absent'}
  foreach($forbidden in @('cost','admission','opportunity','combat-clear','approach-movement','native-movement-displacement','remove-unit')){if($boundary.Contains($forbidden)){throw 'Continuation undeclared event'}}
  $stack=$stacks[$actor];$isBefore=$boundary.EndsWith('-before',[StringComparison]::Ordinal);$isAfter=$boundary.EndsWith('-after',[StringComparison]::Ordinal)
  $kind=if($isBefore){$boundary.Substring(0,$boundary.Length-7)}elseif($isAfter){$boundary.Substring(0,$boundary.Length-6)}else{$boundary}
  $owned=$kind-cin@('cooldown-tick','prepare','clear','completion-force-to-end','completion-end');$call=$null
  if($kind.StartsWith('completion-',[StringComparison]::Ordinal)){
   if(-not$completions.ContainsKey($sequence)){throw 'Continuation call not bound'};$call=$completions[$sequence];$phase=if($isBefore){'before'}else{'after'};$s=$call.$phase
   if($kind-cne('completion-'+$call.kind)-or(Int $s.allocationSequence)-ne$sequence-or(Int $s.turnObject)-ne(Int $e.callbackObject)-or$s.actorId-cne$actor){throw 'Continuation call identity differs'}
   if((Int $s.frame)-ne(Int $e.frame)-or(Int $s.gameTicks)-ne(Int $e.gameTicks)-or(Int $s.round)-ne(Int $e.round)){throw 'Continuation completion clock/round binding differs'}
   Same $s.resources $actual;$observed[$sequence]=$true
  }
  if($kind-ceq'prepare'-and$isBefore){$prepares[$actor]++;if($stack.Count-ne0-or(Int $e.round)-ne$round+1L-or(Int $e.preparingTurn)-eq0-or$prepares[$actor]-ne1){throw 'Continuation preparation is not exactly one next-round grant'};$v=Clone-ContinuationState $current[$actor];$v.grantSequence++;$current[$actor]=$v}
  if($owned-and$isAfter){if($stack.Count-eq0-or$stack.Peek().kind-cne$kind){throw 'Continuation native callback nesting differs'};$expected=$stack.Pop().expected;Same $expected $actual;$current[$actor]=Clone-ContinuationState $actual;continue}
  if(-not(Matches $current[$actor] $actual)-and-not($stack.Count-gt0-and(Matches $stack.Peek().expected $actual))){throw ('Continuation unexplained resource at '+$boundary)}
  $current[$actor]=Clone-ContinuationState $actual;if(-not$owned){continue};if(-not$isBefore){throw 'Continuation native suffix differs'};$expected=Clone-ContinuationState $actual
  if($kind-ceq'cooldown-tick'){$expected=Tick $e}
  elseif($kind-ceq'prepare'){$expected=Cleared $actual $true}
  elseif($kind-ceq'clear'){$clears[$actor]++;if(@($stack.ToArray()|Where-Object kind -CEQ 'prepare').Count-eq0-or$clears[$actor]-ne1){throw 'Continuation clear outside preparation'};$expected=Cleared $actual $false}
  else{if($null-eq$call-or(Int $call.before.round)-ne$round){throw 'Continuation completion outside original allocation'};$expected=Clone-ContinuationState $call.after.resources}
  $stack.Push([pscustomobject]@{kind=$kind;expected=$expected})
 }
 if($sequence-ne$end-or$observed.Count-ne$completions.Count){throw 'Continuation interval did not close'}
 foreach($role in @('rider','mount')){$id=if($role-ceq'rider'){$rider}else{$mount};if($stacks[$id].Count-ne0-or$prepares[$id]-ne1-or$clears[$id]-ne1){throw 'Continuation missing exact preparation/clear'};Same $current[$id] $after.$role}
}
