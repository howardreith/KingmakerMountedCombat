Set-StrictMode -Version Latest
function Assert-KmcTurnCompletion($E){
 function Int($x){if($x -isnot [int] -and $x -isnot [long]){throw 'Completion integer absent'};[long]$x}
 function Num($x){if($x -isnot [int]-and$x -isnot [long]-and$x -isnot [double]-and$x -isnot [single]-and$x -isnot [decimal]){throw 'Completion number absent'};$n=[double]$x;if([double]::IsNaN($n)-or[double]::IsInfinity($n)){throw 'Completion nonfinite'};$n}
 function Bool($x){if($x -isnot [bool]){throw 'Completion boolean absent'};[bool]$x}
 function EqNum($a,[double]$b){if([Math]::Abs((Num $a)-$b)-gt0.0001){throw 'Completion endpoint differs'}}
 function Call($c){
  if(-not(Bool $c.complete)-or$c.kind -cnotin @('end','force-to-end')){throw 'Incomplete completion callback'}
  $set=$false;if($c.kind -ceq 'force-to-end'){$set=Bool $c.setCooldowns}elseif($null -ne $c.setCooldowns){throw 'End has no bool argument'}
  $before=$c.before;$after=$c.after;$a=$before.resources;$b=$after.resources
  foreach($f in @('frame','gameTicks','turnObject','round','pairedSequence')){if((Int $before.$f)-ne(Int $after.$f)){throw 'Completion identity/clock changed'}}
  if((Int $before.turnObject)-eq0-or(Int $after.allocationSequence)-lt(Int $before.allocationSequence)){throw 'Completion turn/sequence missing'}
  if(-not(Bool $before.turnBased)-or-not(Bool $after.turnBased)-or-not(Bool $a.inCombat)-or-not(Bool $b.inCombat)){throw 'Completion requires live TB allocation'}
  if((Bool $before.conditionForfeitContext)-or(Bool $after.conditionForfeitContext)){throw 'Condition forfeit outside ordinary contract'}
  $owned=Bool $before.completionDebtOwned;if($owned-ne(Bool $after.completionDebtOwned)-or($owned-and(Int $before.pairedSequence)-le0)){throw 'Completion ownership differs'}
  if($owned){foreach($endpoint in @($before,$after)){$s=$endpoint.forfeitState;if(-not(Bool $s.granted)-or-not(Bool $s.prepared)-or(Bool $s.forfeitRecorded)-or(Bool $s.forfeitSettled)-or(Num $s.forfeitStandardAdded)-ne0){throw 'Ordinary completion has prior condition-forfeit settlement'}}}
  if([string]::IsNullOrEmpty($before.actorId)-or$before.actorId-cne$after.actorId-or$a.actor-cne$before.actorId-or$b.actor-cne$before.actorId-or(Int $a.actorObject)-eq0-or(Int $a.actorObject)-ne(Int $b.actorObject)){throw 'Completion actor differs'}
  if((Int $c.enterOrdinal)-le0-or(Int $c.exitOrdinal)-le(Int $c.enterOrdinal)){throw 'Completion call order invalid'}
  foreach($f in @('reactions','reactionsPerRound','initiativeOrder','grantSequence')){if((Int $a.$f)-ne(Int $b.$f)){throw 'Completion discrete resource changed'}}
  foreach($f in @('reactionCooldown','initiativeCooldown')){EqNum $b.$f (Num $a.$f)}
  $standard=Num $a.standard;$move=Num $a.move;$swift=Num $a.swift;$step=Num $before.stepMetres;$limit=Num $before.stepLimit
  if($limit-le0){throw 'Completion step limit absent'};EqNum $after.stepLimit $limit
  if($c.kind-ceq'end'){
   $standard=if($owned){[Math]::Max($standard,6.0)}else{6.0};$move=if($owned){[Math]::Max($move,[Math]::Min(6.0,$move))}else{[Math]::Min(6.0,$move)}
   if($after.status-cne'Ended'){throw 'Native End status differs'}
  }else{
   if($set){$standard=if($owned){[Math]::Max($standard,6.0)}else{6.0};$move=if($owned){[Math]::Max($move,3.0)}else{3.0};$swift=if($owned){[Math]::Max($swift,6.0)}else{6.0};$step=$limit}
   if($after.status-cne'Ending'){throw 'Native ForceToEnd status differs'}
  }
  EqNum $b.standard $standard;EqNum $b.move $move;EqNum $b.swift $swift;EqNum $after.stepMetres $step
 }
 if($E.contract-cne'observed-native-turn-completion-writes'-or-not(Bool $E.traceComplete)-or$E.calls-isnot[array]-or$E.errors-isnot[array]-or$E.errors.Count-ne0){throw 'Completion evidence incomplete'}
 if([string]::IsNullOrEmpty($E.riderId)-or[string]::IsNullOrEmpty($E.mountId)-or$E.riderId-ceq$E.mountId){throw 'Completion pair absent'}
 foreach($token in @('06000C46','06000C47')){$hook=@($E.observerHooks|Where-Object token -CEQ $token);if($hook.Count-ne1-or$hook[0].moduleMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'){throw 'Completion native hook absent'}}
 if($E.calls.Count-lt1-or$E.calls.Count-gt64){throw 'Completion call bound differs'}
 $ordinals=@();$priorStart=0L
 foreach($c in $E.calls){
  Call $c;if($c.before.actorId-cne$E.riderId-and$c.before.actorId-cne$E.mountId){throw 'Completion foreign actor'}
  $start=Int $c.enterOrdinal;if($start-le$priorStart){throw 'Completion call starts unordered'};$priorStart=$start;$ordinals+=@($start,(Int $c.exitOrdinal))
 }
 $sorted=@($ordinals|Sort-Object);for($i=0;$i-lt$sorted.Count;$i++){if($sorted[$i]-ne$i+1L){throw 'Completion boundary gap/duplicate'}}
 foreach($c in $E.calls){foreach($d in $E.calls){if($c.enterOrdinal-lt$d.enterOrdinal-and$d.enterOrdinal-lt$c.exitOrdinal-and$c.exitOrdinal-lt$d.exitOrdinal){throw 'Crossed completion callbacks'}}}
}
