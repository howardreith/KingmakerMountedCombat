# Additional gate only: the unchanged full order/command envelope remains required.
function Assert-KmcEarlyEndMount($E) {
 Assert-KmcMountOrder $E
 $allowance=$E.earlyEndAllowance
 if($null-eq$allowance-or$allowance.action-cne'Swift'-or$allowance.method-cne'Kingmaker.EntitySystem.Entities.UnitEntityData.HasSwiftAction'-or$allowance.token-cne'06008380'-or$allowance.moduleMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'-or$allowance.ilSha256-cne'65061d8c8cccff181626338782db42cc3504357af1bc2c3121fb9dc4a49a09a8'){throw 'Early End fixed native Swift allowance contract differs'}
 function N($x){if($x-isnot[int]-and$x-isnot[long]){throw 'Early End integer missing'};[long]$x}
 function B($x){if($x-isnot[bool]){throw 'Early End boolean missing'};[bool]$x}
 function EqualResources($a,$b){
  foreach($f in @('actor','actorObject','inCombat','standard','move','swift','reactions','reactionCooldown','initiativeCooldown','initiativeOrder','reactionsPerRound','grantSequence','hasSwift')){
   if($null-eq$a.$f-or$null-eq$b.$f-or(ConvertTo-Json -InputObject $a.$f -Compress)-cne(ConvertTo-Json -InputObject $b.$f -Compress)){throw ('Early End resource differs: '+$f)}
  }
 }
 $mounted=$E.mounted;$before=$E.beforeEndInput;$after=$E.afterEndInput
 foreach($endpoint in @($before,$after)){
  foreach($f in @('turnObject','round','pairedSequence','gameTicks')){if((N $endpoint.$f)-ne(N $mounted.$f)){throw 'Early End input left the adopted allocation'}}
  if($endpoint.currentActor-cne$E.riderId){throw 'Early End input changed actor'}
 }
 if($before.status-cne'Acting'-or$after.status-cne'Ending'-or(N $before.frame)-lt(N $mounted.frame)-or(N $after.frame)-ne(N $before.frame)-or(N $before.allocationSequence)-lt(N $mounted.allocationSequence)){throw 'Early End native input interval differs'}
 if(-not(B $before.riderResources.hasSwift)){throw 'Early End lacks actual native unused Swift allowance'}
 foreach($role in @('rider','mount')){EqualResources $mounted.($role+'Resources') $before.($role+'Resources')}
 $calls=@($E.completion.calls|Where-Object {$_.kind-ceq'force-to-end'-and$_.before.actorId-ceq$E.riderId})
 if($calls.Count-ne1-or-not(B $calls[0].setCooldowns)){throw 'Early End exact ForceToEnd(true) absent'}
 $call=$calls[0]
 if((N $call.before.turnObject)-ne(N $mounted.turnObject)-or(N $call.before.allocationSequence)-le(N $before.allocationSequence)-or(N $call.after.allocationSequence)-gt(N $after.allocationSequence)){throw 'Early End callback outside input'}
 foreach($f in @('frame','gameTicks')){if((N $call.before.$f)-ne(N $before.$f)-or(N $call.after.$f)-ne(N $after.$f)){throw 'Early End callback clock differs'}}
 EqualResources $call.before.resources $before.riderResources
 EqualResources $call.after.resources $after.riderResources
 if(-not(B $call.before.resources.hasSwift)){throw 'Early End callback lacks native Swift remainder'}
 if((B $call.after.resources.hasSwift)-or(B $after.riderResources.hasSwift)){throw 'Early End native callback did not relinquish Swift allowance'}
 $partner=@($E.completion.calls|Where-Object {$_.kind-ceq'force-to-end'-and$_.before.actorId-ceq$E.mountId})
 if(B $E.riderFirst){
  if($partner.Count-ne1-or(N $partner[0].enterOrdinal)-le(N $call.enterOrdinal)-or(N $partner[0].exitOrdinal)-ge(N $call.exitOrdinal)){throw 'Early End partner not nested in rider input'}
  EqualResources $partner[0].before.resources $before.mountResources
  EqualResources $partner[0].after.resources $after.mountResources
 }else{
  if($partner.Count-ne0){throw 'Early End completed spent partner again'}
  EqualResources $before.mountResources $after.mountResources
 }
}