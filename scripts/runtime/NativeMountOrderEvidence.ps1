. (Join-Path $PSScriptRoot 'NativePassiveResourceEvidence.ps1')
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'NativeAllocationContinuationEvidence.ps1')
function Assert-KmcMountOrder($E){
 function I($v){if($v-isnot[int]-and$v-isnot[long]){throw 'Mount order integer absent'};[long]$v}
 function B($v){if($v-isnot[bool]){throw 'Mount order boolean absent'};[bool]$v}
 function Equal($a,$b){($a|ConvertTo-Json -Depth 90 -Compress)-ceq($b|ConvertTo-Json -Depth 90 -Compress)}
 if($E.contract-cne'fresh-native-allocation-order-through-next-paired-round'){throw 'Mount order contract differs'}
 $first=B $E.riderFirst;$rider=[string]$E.riderId;$mount=[string]$E.mountId;$order=if($first){'rider-first'}else{'mount-first'}
 if([string]::IsNullOrEmpty($rider)-or[string]::IsNullOrEmpty($mount)-or$rider-ceq$mount-or$E.scenario-cne('chunk6a-allocation-'+$order+'-tb')){throw 'Mount order case/pair differs'}
 $fixture=$E.fixture;$proof=$E.positiveProof;$mounted=$E.mounted;$next=$E.nextRound;$before=$E.mountBefore
 if(-not(B $fixture.outsideCombat)-or-not(B $fixture.targetTurnBased)-or-not(B $fixture.modeLeaseCurrent)-or$fixture.relationshipState-cne'Unmounted'-or(I $fixture.riderInputBase)-ne$(if($first){40}else{-40})-or(I $fixture.mountInputBase)-ne$(if($first){-40}else{40})-or-not(Equal $fixture.beforeResources $fixture.afterResources)){throw 'Mount order fixture changed resources'}
 foreach($role in @('rider','mount')){if((I $fixture.beforeResources.$role.grantSequence)-ne0-or(B $fixture.beforeResources.$role.inCombat)){throw 'Mount order fixture reused allocation'}}
 foreach($flag in @('pass','identityComplete','sameCommandAtEveryBoundary','exactActedObserved','nativeTerminal')){if(-not(B $proof.$flag)){throw 'Mount order positive proof incomplete'}}
 if($proof.nativeResult-cne'Success'-or-not(B $proof.resourceWindow.pass)-or-not(B $proof.resourceWindow.reactionResources.pass)){throw 'Mount order native resource proof incomplete'}
 $id=$proof.identity;if($id.casterId-cne$rider-or$id.targetId-cne$mount-or$id.abilityGuid-cne'f053faad986631688defa003cd7bda0e'-or$id.commandType-cne'Move'-or(I $proof.initCount)-ne1){throw 'Mount order exact Mount differs'}
 $terminal=@($proof.samples|Where-Object boundary -CEQ 'terminal');if($terminal.Count-ne1){throw 'Order exact command terminal absent'};$terminal=$terminal[0]
 $bridge=$E.terminalBridge;Assert-KmcNativePassiveResources $bridge
 if($bridge.riderId-cne$rider-or$bridge.mountId-cne$mount-or-not(B $bridge.turnBased)){throw 'Terminal bridge pair/mode differs'}
 foreach($key in @('frame','gameTicks','allocationSequence')){if((I $bridge.before.$key)-ne(I $terminal.$key)-or(I $bridge.after.$key)-ne(I $mounted.$key)){throw 'Terminal bridge clock/sequence differs'}}
 if(-not(Equal $terminal.state.ledger $mounted.state.ledger)-or(I $terminal.state.generation)-ne(I $mounted.state.generation)-or$terminal.state.relationshipState-cne'Mounted'){throw 'Terminal bridge relationship differs'}
 foreach($role in @('rider','mount')){
  if(-not(Equal $bridge.before.$role $terminal.nativeAllocation.$role)-or-not(Equal $bridge.after.$role $mounted.($role+'Resources'))){throw 'Terminal bridge resource copies differ'}
  foreach($key in @('actor','standard','move','swift','reactions','reactionCooldown','initiativeCooldown','initiativeOrder','reactionsPerRound')){if($null-eq$terminal.state.$role.$key-or$terminal.state.$role.$key-cne$terminal.nativeAllocation.$role.$key){throw ('Terminal allocation differs from exact command '+$role+' '+$key)}}
  if((I $terminal.state.$role.nativePrepareCount)-ne(I $terminal.nativeAllocation.$role.grantSequence)){throw 'Terminal preparation count differs'}
 }
 $round=I $mounted.round;$turn=I $mounted.turnObject;$generation=(I $id.generationAtInit)+1L
 if($turn-eq0-or(I $terminal.state.rider.nativeTurnObject)-ne$turn-or(I $before.round)-ne$round-or$before.currentTurnActor-cne$rider-or$mounted.currentActor-cne$rider-or$mounted.status-cne'Acting'-or(I $before.rider.nativeTurnObject)-ne$turn){throw 'Mount order adopted rider context differs'}
 if($E.disposition-cne$(if($first){'PreparePartnerThisRound'}else{'RetainPartnerParticipation'})){throw 'Mount order disposition differs'}
 $roster=@($mounted.roster|ForEach-Object actor);$ri=[Array]::IndexOf($roster,$rider);$mi=[Array]::IndexOf($roster,$mount)
 if($ri-lt0-or$mi-lt0-or$ri-eq$mi-or(($ri-lt$mi)-ne$first)-or@($roster|Select-Object -Unique).Count-ne$roster.Count-or(I $before.riderRosterIndex)-ne$ri-or(I $before.mountRosterIndex)-ne$mi){throw 'Mount order native roster differs'}
 if((I $before.rider.nativePrepareCount)-ne1-or(I $before.mount.nativePrepareCount)-ne$(if($first){0}else{1})){throw 'Mount order pre-Mount participation differs'}
 if((((I $mounted.partnerContextObject)-ne0)-ne$first)-or($first-and$mounted.partnerActor-cne$mount)-or(-not$first-and$null-ne$mounted.partnerActor)){throw 'Mount order partner context differs'}
 foreach($b in @($mounted,$E.beforeEndInput,$E.afterEndInput,$next)){
  if((I $b.controllerObject)-ne(I $mounted.controllerObject)-or(I $b.sessionObject)-ne(I $mounted.sessionObject)-or-not(B $b.turnBased)-or$b.state.relationshipState-cne'Mounted'-or(I $b.state.generation)-ne$generation-or(I $b.adoptionCount)-ne1-or(B $b.pairedSplit)-or-not(Equal $b.state.ledger $mounted.state.ledger)){throw 'Mount order encounter/pair/ledger differs'}
 }
 if((I $mounted.pairedSequence)-ne1-or(I $next.pairedSequence)-ne2-or(I $next.round)-ne$round+1L-or$next.currentActor-cne$rider-or(I $next.turnObject)-eq$turn-or(I $next.turnObject)-eq0-or$next.partnerActor-cne$mount-or(I $next.partnerContextObject)-eq0-or(B $next.pairedFinalized)){throw 'Mount order next paired activation absent'}
 foreach($role in @('riderResources','mountResources')){if((I $mounted.$role.grantSequence)-ne1-or(I $next.$role.grantSequence)-ne2){throw 'Mount order preparation count differs'}}
 $input=$E.input;if($input.method-cne'Kingmaker.Game.PauseBind'-or$input.token-cne'06000CB7'-or$input.moduleMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'-or(I $input.count)-ne1-or(I $E.beforeEndInput.turnObject)-ne$turn-or(I $E.beforeEndInput.round)-ne$round-or$E.beforeEndInput.status-cne'Acting'){throw 'Mount order native End Turn differs'}
 $trace=$E.allocationTrace;if((I $trace.dropped)-ne0-or(I $trace.observationErrors)-ne0-or$trace.events-isnot[array]){throw 'Mount order trace incomplete'}
 $seq=0L;foreach($item in $trace.events){$seq++;if((I $item.sequence)-ne$seq){throw 'Mount order trace gap'}}
 if($seq-ne(I $next.allocationSequence)){throw 'Mount order trace endpoint differs'}
 $bridgeEvents=@($trace.events|Where-Object {$_.sequence-gt$bridge.before.allocationSequence-and$_.sequence-le$bridge.after.allocationSequence})
 if(-not(Equal @($bridge.events) $bridgeEvents)){throw 'Terminal bridge differs from complete trace'}
 foreach($actor in @($rider,$mount)){foreach($roundId in @($round,($round+1L))){foreach($phase in @('before','after')){
  if(@($trace.events|Where-Object {$_.boundary-ceq('prepare-'+$phase)-and$_.state.actor-ceq$actor-and(I $_.round)-eq$roundId}).Count-ne1){throw 'Mount order actor/round preparation differs'}
 }}}
 $rp=@($trace.events|Where-Object {$_.boundary-ceq'prepare-before'-and$_.state.actor-ceq$rider-and(I $_.round)-eq$round})[0]
 $ends=@($trace.events|Where-Object {$_.boundary-ceq'turn-end-after'-and$_.state.actor-ceq$mount-and(I $_.round)-eq$round-and(I $_.sequence)-lt(I $proof.preClick.allocationSequence)})
 if($ends.Count-ne$(if($first){0}else{1})-or(-not$first-and(I $ends[0].sequence)-ge(I $rp.sequence))){throw 'Mount order prior partner slot differs'}
 if($E.turns-isnot[array]-or$E.turns.Count-eq0-or$E.turns.Count-gt128){throw 'Mount order turns absent'}
 if(@($E.turns|Where-Object {$_.currentActor-ceq$mount-and(I $_.allocationSequence)-ge(I $mounted.allocationSequence)}).Count-ne0){throw 'Mount order independent mounted partner slot'}
 if((@($E.turns|Where-Object {$_.currentActor-ceq$mount-and(I $_.round)-eq$round}).Count-gt0)-ne(-not$first)){throw 'Mount order actual partner slot order missing'}
 if($E.completion.calls-isnot[array]){throw 'Mount order completion absent'}
 foreach($actor in @($rider,$mount)){foreach($kind in @('force-to-end','end')){
  $calls=@($E.completion.calls|Where-Object {$_.kind-ceq$kind-and$_.before.actorId-ceq$actor});$expected=if($actor-ceq$rider-or$first){1}else{0}
  if($calls.Count-ne$expected){throw 'Mount order adopted completion count differs'}
  foreach($c in $calls){if((I $c.before.turnObject)-ne$(if($actor-ceq$rider){$turn}else{I $mounted.partnerContextObject})){throw 'Mount order completion context differs'}}
 }}
 $continuation=$E.continuation;if(-not(Equal $E.completion $continuation.completion)){throw 'Mount order completion record differs'}
 Assert-KmcAllocationContinuation $continuation
 if($continuation.riderId-cne$rider-or$continuation.mountId-cne$mount-or(I $continuation.before.allocationSequence)-ne(I $mounted.allocationSequence)-or(I $continuation.after.allocationSequence)-ne(I $next.allocationSequence)){throw 'Mount order continuation interval differs'}
 foreach($pair in @(@('rider','riderResources'),@('mount','mountResources'))){if(-not(Equal $continuation.before.($pair[0]) $mounted.($pair[1]))-or-not(Equal $continuation.after.($pair[0]) $next.($pair[1]))){throw 'Mount order resource endpoints differ'}}
}
function Assert-KmcMountOrderRestoration($E){
 $r=$E.restoration;$f=$E.fixture
 if($r.exact-isnot[bool]-or-not$r.exact-or$r.outsideCombat-isnot[bool]-or-not$r.outsideCombat-or$r.riderBase-isnot[int]-or$r.mountBase-isnot[int]-or$r.riderBase-ne$f.riderOriginalBase-or$r.mountBase-ne$f.mountOriginalBase){throw 'Mount order pre-encounter input restoration differs'}
}
