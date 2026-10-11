Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'NativeAllocationContinuationEvidence.ps1')
. (Join-Path $PSScriptRoot 'NativePassiveResourceEvidence.ps1')
function Assert-KmcDismountGroundSetup($P,[string]$Rider,[string]$Mount) {
 function I($v){if($v-isnot[int]-and$v-isnot[long]){throw 'Dismount ground integer absent'};[long]$v}
 function N($v){if($v-isnot[int]-and$v-isnot[long]-and$v-isnot[double]-and$v-isnot[single]-and$v-isnot[decimal]){throw 'Dismount ground number absent'};$n=[double]$v;if([double]::IsNaN($n)-or[double]::IsInfinity($n)){throw 'Dismount ground nonfinite'};$n}
 function Eq($a,$b){(ConvertTo-Json -InputObject $a -Depth 90 -Compress)-ceq(ConvertTo-Json -InputObject $b -Depth 90 -Compress)}
 if([string]::IsNullOrEmpty($Rider)-or[string]::IsNullOrEmpty($Mount)-or$Rider-ceq$Mount-or$P.traceComplete-isnot[bool]-or-not$P.traceComplete){throw 'Dismount ground pair/trace absent'}
 $before=$P.before;$after=$P.after;$admitted=$P.admittedCommand;$terminal=$P.terminalCommand
 $id=I $admitted.id;$turn=I $before.turnObject;$round=I $before.round
 if($id-eq0-or$turn-eq0-or(I $terminal.id)-ne$id-or$admitted.executor-cne$Mount-or$terminal.executor-cne$Mount-or$admitted.type-cne'Kingmaker.UnitLogic.Commands.UnitMoveTo'-or$terminal.type-cne$admitted.type-or$terminal.acted-isnot[bool]-or-not$terminal.acted-or$terminal.finished-isnot[bool]-or-not$terminal.finished-or$terminal.result-cne'Success'){throw 'Dismount ground exact command did not succeed'}
 if($before.status-cne'Preparing'-or$after.status-cne'Acting'-or(I $after.turnObject)-ne$turn-or(I $after.round)-ne$round-or$before.currentActor-cne$Rider-or$after.currentActor-cne$Rider){throw 'Dismount ground same Preparing-to-Acting rider absent'}
 foreach($b in @($before,$after)){
  if($b.turnBased-isnot[bool]-or-not$b.turnBased-or(I $b.pairedSequence)-ne2-or$b.pairedSplit-isnot[bool]-or$b.pairedSplit-or$b.partnerActor-cne$Mount-or(I $b.partnerContextObject)-eq0-or$b.state.relationshipState-cne'Mounted'-or$b.state.selectedIds-isnot[array]-or$b.state.selectedIds.Count-ne1-or$b.state.selectedIds[0]-cne$Rider){throw 'Dismount ground pair/selection/allocation changed'}
  foreach($key in @('generation','ledger')){if(-not(Eq $before.state.$key $b.state.$key)){throw 'Dismount ground relationship changed'}}
  foreach($key in @('controllerObject','sessionObject','partnerContextObject','adoptionCount')){if(-not(Eq $before.$key $b.$key)){throw 'Dismount ground encounter identity changed'}}
 }
 if($P.events-isnot[array]-or$P.observerHooks-isnot[array]){throw 'Dismount ground native evidence absent'}
 foreach($token in @('060026B2','06009120','0600838F','060093A1','0600934A','06000C3C','0600C3BE','060018A9','06000C5E')){
  if(@($P.observerHooks|Where-Object {$_.token-ceq$token-and$_.moduleMvid-ceq'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'}).Count-ne1){throw 'Dismount ground observer absent'}
 }
 $seq=I $before.allocationSequence;$frame=I $before.frame;$ticks=I $before.gameTicks;$move=N $before.mountResources.move
 $costs=@();$admissions=@();$ended=0
 foreach($e in $P.events){
  $seq++;if((I $e.sequence)-ne$seq-or(I $e.frame)-lt$frame-or(I $e.frame)-gt(I $after.frame)-or(I $e.gameTicks)-lt$ticks-or(I $e.gameTicks)-gt(I $after.gameTicks)){throw 'Dismount ground event clock/sequence gap'}
  $frame=I $e.frame;$ticks=I $e.gameTicks;$actor=$e.state.actor;if($actor-cne$Rider-and$actor-cne$Mount){continue}
  $role=if($actor-ceq$Rider){'riderResources'}else{'mountResources'};$s=$e.state;$baseline=$before.$role
  if($e.nativeTurnBased-isnot[bool]-or-not$e.nativeTurnBased-or$e.nativePassing-isnot[bool]-or$e.nativePassing-or(I $e.round)-ne$round-or(I $e.turn)-ne$turn-or$e.currentActor-cne$Rider){throw 'Dismount ground native allocation changed'}
  foreach($f in @('actor','actorObject','inCombat','grantSequence','standard','swift','reactions','reactionsPerRound','reactionCooldown','initiativeCooldown','initiativeOrder')){
   if($null-eq$baseline.$f-or-not(Eq $s.$f $baseline.$f)){throw ('Dismount ground undeclared actor change '+$actor+' '+$f)}
  }
  if($actor-ceq$Rider){if((N $s.move)-ne(N $baseline.move)){throw 'Dismount ground carried rider charged'}}
  else{if((N $s.move)+0.0001-lt$move){throw 'Dismount ground mount refund'};$move=N $s.move}
  $boundary=[string]$e.boundary
  foreach($part in @('prepare','clear','opportunity','round-','turn-end','completion-','remove-unit')){if($boundary.Contains($part)){throw ('Dismount ground undeclared native event '+$boundary)}}
  if($boundary.Contains('cost')-or$boundary.Contains('admission')-or$boundary.StartsWith('command-end')){
   if((I $e.command)-ne$id-or$actor-cne$Mount-or$e.commandType-cne$admitted.type-or$e.actionType-cne'Move'-or$e.simulatingClick-isnot[bool]-or$e.simulatingClick){throw 'Dismount ground another command/action'}
   if($boundary.Contains('cost')){$costs+=@($e)}
   if($boundary.Contains('admission')){$admissions+=@($e)}
   if($boundary-ceq'command-end-after'-and$e.finished-is[bool]-and$e.finished-and$e.result-ceq'Success'){$ended++}
  }
 }
 if($seq-ne(I $after.allocationSequence)-or$ended-lt1){throw 'Dismount ground terminal event absent'}
 if($admissions.Count-ne2-or$admissions[0].boundary-cne'admission-before'-or$admissions[1].boundary-cne'admission-after'){throw 'Dismount ground one admission absent'}
 $costBoundaries=@('cost-before','actor-cost-before','actor-cost-after','cost-after')
 if($costs.Count-ne4){throw 'Dismount ground native TB cost callback count differs'}
 for($i=0;$i-lt4;$i++){$c=$costs[$i];if($c.boundary-cne$costBoundaries[$i]-or(I $c.sequence)-ne(I $costs[0].sequence)+$i-or$c.ignoreCooldown-isnot[bool]-or-not$c.ignoreCooldown-or$c.acted-isnot[bool]-or-not$c.acted){throw 'Dismount ground native TB no-write boundary absent'}
  foreach($f in @('standard','move','swift')){if(-not(Eq $costs[0].state.$f $c.state.$f)){throw 'Dismount ground acted cost changed debt'}}
 }
 foreach($role in @('riderResources','mountResources')){foreach($f in @('actor','actorObject','inCombat','grantSequence','standard','swift','reactions','reactionsPerRound','reactionCooldown','initiativeCooldown','initiativeOrder')){if(-not(Eq $before.$role.$f $after.$role.$f)){throw 'Dismount ground terminal unrelated resource changed'}}}
 if((N $after.riderResources.move)-ne(N $before.riderResources.move)){throw 'Dismount ground terminal rider charged'}
 $delta=(N $after.mountResources.move)-(N $before.mountResources.move)
 $allowed=(N $after.mountResources.measuredAllowedTime)-(N $before.mountResources.measuredAllowedTime)
 if($delta-le0-or$allowed-le0-or[Math]::Abs($delta-$allowed)-gt0.0001-or[Math]::Abs($move-(N $after.mountResources.move))-gt0.0001){throw 'Dismount ground debt differs from native allowed movement'}
}
function Assert-KmcFullTbDismountTurn($Artifact) {
 $o=$Artifact.observations
 $mp=@($o.chunk6aCommandProofs|Where-Object window -CEQ 'positive-mount')
 $dp=@($o.chunk6aCommandProofs|Where-Object window -CEQ 'combat-dismount')
 if($mp.Count-ne1-or$dp.Count-ne1){throw 'Full TB requires exact Mount/Dismount windows'}
 $terminal=@($mp[0].samples|Where-Object boundary -CEQ 'terminal')[0];$pre=$dp[0].preClick
 $rider=$mp[0].identity.casterId;$mount=$mp[0].identity.targetId
 if(-not$o.PSObject.Properties['chunk6aDismountTurn']){
  if($pre.state.rider.nativeTurnObject-ne$terminal.state.rider.nativeTurnObject-or$pre.state.rider.nativePrepareCount-ne$terminal.state.rider.nativePrepareCount-or$pre.state.mount.nativePrepareCount-ne$terminal.state.mount.nativePrepareCount){throw 'Full TB changed allocation without native continuation evidence'}
  return # Same-turn case still requires the unchanged exact Dismount resource proof.
 }
 $e=$o.chunk6aDismountTurn;$b=$e.beforeEndInput;$n=$e.nextRound;$g=$e.groundSetup;$ready=$e.ready
 if($e.contract-cne'full-tb-later-native-turn-dismount'-or$e.riderId-cne$rider-or$e.mountId-cne$mount-or$e.reason-cne'The rider has no Move action available to dismount.'){throw 'Full TB later-turn declaration differs'}
 if($e.endInput.method-cne'Kingmaker.Game.PauseBind'-or$e.endInput.token-cne'06000CB7'-or$e.endInput.moduleMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'-or$e.endInput.count-isnot[int]-or$e.endInput.count-ne1){throw 'Full TB one ordinary End input absent'}
 if($b.currentActor-cne$rider-or$b.status-cne'Acting'-or$b.turnObject-ne$terminal.state.rider.nativeTurnObject-or$b.riderResources.move-le3-or$e.afterEndInput.turnObject-ne$b.turnObject-or$e.afterEndInput.round-ne$b.round-or$n.round-ne$b.round+1-or$n.turnObject-eq$b.turnObject-or$n.currentActor-cne$rider-or$n.pairedSequence-ne2-or$n.partnerActor-cne$mount){throw 'Full TB exact spent/next rider allocation differs'}
 foreach($x in @($b,$e.afterEndInput,$n,$g.before,$g.after,$ready)){
  if($x.controllerObject-ne$b.controllerObject-or$x.sessionObject-ne$b.sessionObject-or$x.state.generation-ne$terminal.state.generation-or$x.state.relationshipState-cne'Mounted'-or$x.adoptionCount-ne1-or$x.pairedSplit){throw 'Full TB encounter/relationship changed'}
  Assert-KmcSameEvidence $x.state.ledger $terminal.state.ledger 'full TB relationship ledger'
 }
 if($b.state.selectedIds.Count-ne1-or$b.state.selectedIds[0]-cne$rider){throw 'Full TB End selection differs'}
 $c=$e.continuation;Assert-KmcAllocationContinuation $c
 if($c.riderId-cne$rider-or$c.mountId-cne$mount){throw 'Full TB continuation pair differs'}
 foreach($role in @('rider','mount')){
  Assert-KmcSameEvidence $c.before.$role $b.($role+'Resources') 'full TB before completion'
  Assert-KmcSameEvidence $c.after.$role $n.($role+'Resources') 'full TB after preparation'
  if($n.($role+'Resources').grantSequence-ne$b.($role+'Resources').grantSequence+1){throw 'Full TB preparation repeated/missing'}
 }
 foreach($call in $c.completion.calls){if($call.before.actorId-ceq$rider-and$call.before.turnObject-ne$b.turnObject){throw 'Full TB End affected another rider turn'}}
 $riderForce=@($c.completion.calls|Where-Object {$_.kind-ceq'force-to-end'-and$_.before.actorId-ceq$rider})
 if($riderForce.Count-ne1-or$null-eq$riderForce[0].setCooldowns-or-not$riderForce[0].setCooldowns){throw 'Full TB ordinary End force count differs'}
 Assert-KmcDismountGroundSetup $g $rider $mount
 if($g.before.turnObject-ne$n.turnObject-or$ready.turnObject-ne$n.turnObject-or$ready.status-cne'Acting'){throw 'Full TB ground/readiness context differs'}
 $trace=$o.actorAllocationTrace
 if($trace.dropped-ne0-or$trace.observationErrors-ne0){throw 'Full TB allocation trace incomplete'}
 foreach($window in @($e.mountTerminalBridge,$c,$e.groundStartBridge,$g,$e.readyBridge)){
  Assert-KmcSameEvidence @($window.observerHooks) @($trace.observerHooks) 'full TB native hooks'
  $slice=@($trace.events|Where-Object {$_.sequence-gt$window.before.allocationSequence-and$_.sequence-le$window.after.allocationSequence})
  Assert-KmcSameEvidence @($window.events) $slice 'full TB native event slice'
 }
 foreach($bridge in @($e.mountTerminalBridge,$e.groundStartBridge,$e.readyBridge)){Assert-KmcNativePassiveResources $bridge}
 foreach($pair in @(@($e.mountTerminalBridge.before,$terminal),@($e.mountTerminalBridge.after,$b),@($c.before,$b),@($c.after,$n),@($e.groundStartBridge.before,$n),@($e.groundStartBridge.after,$g.before),@($e.readyBridge.before,$g.after),@($e.readyBridge.after,$ready))){
  foreach($key in @('frame','gameTicks','allocationSequence')){if($pair[0].$key-ne$pair[1].$key){throw 'Full TB window boundary differs'}}
 }
 foreach($role in @('rider','mount')){
  Assert-KmcSameEvidence $e.mountTerminalBridge.before.$role $terminal.nativeAllocation.$role 'full TB terminal resource binding'
  foreach($pair in @(@($e.mountTerminalBridge.after,$b),@($e.groundStartBridge.before,$n),@($e.groundStartBridge.after,$g.before),@($e.readyBridge.before,$g.after),@($e.readyBridge.after,$ready))){Assert-KmcSameEvidence $pair[0].$role $pair[1].($role+'Resources') 'full TB passive actor binding'}
  foreach($field in @('actor','standard','move','swift','reactions','reactionsPerRound','reactionCooldown','initiativeCooldown','initiativeOrder')){if($pre.state.$role.$field-cne$ready.($role+'Resources').$field){throw ('Full TB Dismount baseline did not carry '+$role+' '+$field)}}
  if($pre.state.$role.nativePrepareCount-ne$ready.($role+'Resources').grantSequence){throw 'Full TB Dismount baseline preparation differs'}
 }
 if($pre.allocationSequence-ne$ready.allocationSequence-or$pre.frame-ne$ready.frame-or$pre.gameTicks-ne$ready.gameTicks-or$pre.state.rider.nativeTurnObject-ne$ready.turnObject-or$pre.state.selectedIds.Count-ne1-or$pre.state.selectedIds[0]-cne$rider){throw 'Full TB Dismount baseline preceded exact selected readiness'}
}
