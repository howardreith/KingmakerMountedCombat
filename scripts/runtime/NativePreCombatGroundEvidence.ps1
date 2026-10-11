Set-StrictMode -Version Latest
function Assert-KmcNativeGroundPlan($P) {
 function Number($v){if($v -isnot [int] -and $v -isnot [long] -and $v -isnot [double] -and $v -isnot [single] -and $v -isnot [decimal]){throw 'Ground plan number missing'};$n=[double]$v;if([double]::IsNaN($n)-or[double]::IsInfinity($n)){throw 'Ground plan nonfinite number'};$n}
 function Integer($v){if($v -isnot [int] -and $v -isnot [long]){throw 'Ground plan integer missing'};[int]$v}
 function Boolean($v){if($v -isnot [bool]){throw 'Ground plan boolean missing'};$v}
 function Point($p){if($p -is [array]){if($p.Count-ne3){throw 'Ground vector length'};@((Number $p[0]),(Number $p[1]),(Number $p[2]))}else{@((Number $p.x),(Number $p.y),(Number $p.z))}}
 function Distance($a,$b){[Math]::Sqrt([Math]::Pow($a[0]-$b[0],2)+[Math]::Pow($a[2]-$b[2],2))}
 function Near($a,$b){if([Math]::Abs((Number $a)-(Number $b))-gt0.00001){throw 'Ground plan numeric geometry differs'}}
 function SamePoint($a,$b){$x=Point $a;$y=Point $b;foreach($i in 0..2){Near $x[$i] $y[$i]}}
 $reciprocal=$P.contract-ceq'bounded-native-ground-plan-v2'
 if((-not$reciprocal-and$P.contract-cne'bounded-native-ground-plan')-or(Integer $P.maximumCandidates)-ne72){throw 'Ground plan contract or bound differs'}
 $actor=$P.actorId;$partner=$P.partnerId;$joint=Boolean $P.joint
 if([string]::IsNullOrWhiteSpace($actor)-or[string]::IsNullOrWhiteSpace($partner)-or$actor-ceq$partner){throw 'Ground plan pair identity missing'}
 $origin=Point $P.origin;$center=Point $P.center;$corp=Number $P.corpulence;$clearance=Number $P.clearanceRadius
 # Historical v1 evidence keeps its original forward-only route predicate.
 # A format change never grants a historical trace the new reciprocal proof.
 if($reciprocal){
  $originInside=Boolean $P.originInsideNavmesh;$originWalkable=Boolean $P.originNativeNearest.walkable
  SamePoint $P.originNativeNearest.requested $P.origin
  $nearestOriginResidual=Distance $origin (Point $P.originNativeNearest.clamped)
 }elseif($null-ne$P.PSObject.Properties['originInsideNavmesh']){throw 'Legacy ground plan carries a newer origin proof'}
 if($corp-lt0){throw 'Ground plan negative radius'}
 $extra=0.75;if($joint){$extra=0.0};Near $clearance ([Math]::Max(0.5,$corp)+$extra)
 $length=Distance $origin $center;if($length-lt0.1){throw 'Ground plan degenerate geometry'};$dx=($origin[0]-$center[0])/$length;$dz=($origin[2]-$center[2])/$length
 $occupants=@($P.occupants);$ids=@($occupants|ForEach-Object actorId)
 if(@($ids|Where-Object{[string]::IsNullOrWhiteSpace($_)}).Count-ne0-or@($ids|Select-Object -Unique).Count-ne$ids.Count-or$ids-cnotcontains$actor-or$ids-cnotcontains$partner){throw 'Ground plan occupancy identity differs'}
 foreach($u in $occupants){$null=Point $u.position;if((Number $u.corpulence)-lt0){throw 'Ground plan occupant radius invalid'}}
 $own=@($occupants|Where-Object actorId -CEQ $actor)[0];$other=@($occupants|Where-Object actorId -CEQ $partner)[0]
 SamePoint $own.position $P.origin;SamePoint $other.position $P.center;Near $own.corpulence $corp
 if($null-ne$P.partnerPositionOverride){if($joint){throw 'Joint plan partner override'};SamePoint $P.partnerPositionOverride $P.center}
 $candidates=@($P.candidates);if($candidates.Count-lt1-or$candidates.Count-gt72){throw 'Ground plan search size invalid'};$selected=-1
 for($n=0;$n-lt$candidates.Count;$n++){
  if($selected-ge0){throw 'Ground plan continued after selection'}
  $c=$candidates[$n];$radius=@(2.3,2.65,2.0)[[int][Math]::Floor($n/24)];$index=$n%24
  if(-not$reciprocal-and($null-ne$c.PSObject.Properties['reverseRouteEnd']-or$null-ne$c.PSObject.Properties['routeProof'])){throw 'Legacy ground plan carries a newer route proof'}
  $signed=$index;if($index%2-eq1){$signed=-$index};$angle=$signed*15*[Math]::PI/180
  $requested=Point $c.requested;$point=Point $c.point;Near $c.requestedSeparation $radius
  Near $requested[0] ($center[0]+([Math]::Cos($angle)*$dx+[Math]::Sin($angle)*$dz)*$radius)
  Near $requested[1] $center[1];Near $requested[2] ($center[2]+(-[Math]::Sin($angle)*$dx+[Math]::Cos($angle)*$dz)*$radius)
  $nestedProperty=$c.PSObject.Properties['riderSearch'];$nested=$null;if($null-ne$nestedProperty){$nested=$nestedProperty.Value}
  if(-not(Boolean $c.walkable)){if((Boolean $c.eligible)-or$null-ne$nested){throw 'Nonwalkable plan selected'};continue}
  $travel=Distance $origin $point;$separation=Distance $center $point;$route=Distance (Point $c.routeEnd) $point
  Near $c.travel $travel;Near $c.separation $separation;Near $c.routeResidual $route
  $measuredRoute=$route
  if($reciprocal){
   $reverse=Distance (Point $c.reverseRouteEnd) $origin;$clipped=Distance (Point $c.routeEnd) $origin
   $direction='rejected'
   if($route-lt0.001){$direction='forward'}
   elseif($originInside-and$originWalkable-and$nearestOriginResidual-lt0.001-and$clipped-lt0.001-and$reverse-lt0.001){
    $direction='reciprocal-origin-boundary';$measuredRoute=$reverse
   }
   if($c.routeProof-cne$direction){throw 'Ground plan native route direction differs'}
  }
  $f=$c.footprint;SamePoint $f.center $c.point;Near $f.corpulence $corp;Near $f.probeRadius $clearance
  $probes=@($f.probes);if($probes.Count-ne8){throw 'Ground footprint probes missing'};$maximum=0.0
  for($j=0;$j-lt8;$j++){
   $probe=$probes[$j];$to=Point $probe.requested;$a=$j*[Math]::PI/4
   Near $to[0] ($point[0]+[Math]::Sin($a)*$clearance);Near $to[1] $point[1];Near $to[2] ($point[2]+[Math]::Cos($a)*$clearance)
   $residual=Distance $to (Point $probe.endpoint);Near $probe.residual $residual;$maximum=[Math]::Max($maximum,(Number $probe.residual))
  }
  $blockers=@($occupants|Where-Object {$_.actorId-cne$actor-and(Distance $point (Point $_.position))-lt($corp+(Number $_.corpulence)+0.05)}|ForEach-Object actorId)
  if(($blockers|ConvertTo-Json -Compress)-cne(@($c.blockers)|ConvertTo-Json -Compress)){throw 'Ground plan occupancy differs'}
  $eligible=$blockers.Count-eq0-and$radius-ge2-and$radius-le2.65-and[Math]::Abs($c.separation-$radius)-le0.45-and$c.travel-ge0.25-and$c.travel-le4-and$measuredRoute-ge0-and$measuredRoute-lt0.001-and$maximum-lt0.001
  if((Boolean $c.eligible)-ne$eligible){throw 'Ground plan eligibility differs'}
  if(-not$eligible){if($null-ne$nested){throw 'Rejected mount has rider plan'};continue}
  if($joint){
   if($null-eq$nested-or$nested.contract-cne$P.contract-or(Boolean $nested.joint)-or$nested.actorId-cne$partner-or$nested.partnerId-cne$actor){throw 'Joint plan pair or format differs'}
   SamePoint $nested.center $c.point;SamePoint $nested.origin $P.center;SamePoint $nested.partnerPositionOverride $c.point
   if(@($nested.occupants).Count-ne$occupants.Count){throw 'Joint counterfactual inventory missing'}
   for($k=0;$k-lt$occupants.Count;$k++){
    $actual=$nested.occupants[$k];$original=$occupants[$k];if($actual.actorId-cne$original.actorId){throw 'Joint occupant differs'};Near $actual.corpulence $original.corpulence
    $expected=$original.position;if($original.actorId-ceq$actor){$expected=$c.point};SamePoint $actual.position $expected
   }
   if((Assert-KmcNativeGroundPlan $nested)-lt0){continue}
  }elseif($null-ne$nested){throw 'Unexpected nested ground plan'}
  $selected=$n
 }
 if((Integer $P.selectedIndex)-ne$selected-or($selected-lt0-and$candidates.Count-ne72)){throw 'Ground plan selection or search cap differs'}
 $selected
}

Set-StrictMode -Version Latest
function Assert-KmcNativeGroundTransaction($P,[string]$Rider,[string]$Mount) {
 function Number($v){if($v -isnot [int]-and$v -isnot [long]-and$v -isnot [double]-and$v -isnot [single]-and$v -isnot [decimal]){throw 'Ground transaction number missing'};$n=[double]$v;if([double]::IsNaN($n)-or[double]::IsInfinity($n)){throw 'Ground transaction nonfinite number'};$n}
 function Integer($v){if($v -isnot [int]-and$v -isnot [long]){throw 'Ground transaction integer missing'};[int]$v}
 function Boolean($v){if($v -isnot [bool]){throw 'Ground transaction boolean missing'};$v}
 function Near($a,$b){if([Math]::Abs((Number $a)-(Number $b))-gt0.00001){throw 'Ground transaction numeric binding differs'}}
 function Point($p){if($p -is [array]){if($p.Count-ne3){throw 'Ground transaction vector length'};@((Number $p[0]),(Number $p[1]),(Number $p[2]))}else{@((Number $p.x),(Number $p.y),(Number $p.z))}}
 function SamePoint($a,$b){$x=Point $a;$y=Point $b;foreach($i in 0..2){Near $x[$i] $y[$i]}}
 function Distance($a,$b){$x=Point $a;$y=Point $b;[Math]::Sqrt([Math]::Pow($x[0]-$y[0],2)+[Math]::Pow($x[2]-$y[2],2))}
 function SameJson($a,$b){if(($a|ConvertTo-Json -Depth 85 -Compress)-cne($b|ConvertTo-Json -Depth 85 -Compress)){throw 'Ground transaction evidence copies differ'}}
 if([string]::IsNullOrWhiteSpace($Rider)-or[string]::IsNullOrWhiteSpace($Mount)-or$Rider-ceq$Mount){throw 'Ground transaction pair missing'}
 $mover=$P.moverId;if($mover-cne$Rider-and$mover-cne$Mount){throw 'Ground transaction mover differs'}
 $selected=Assert-KmcNativeGroundPlan $P.plan;$partner=$Mount;$contract='native-ground-positioning-before-fresh-encounter'
 if($mover-ceq$Mount){$partner=$Rider;$contract='native-mount-ground-positioning-before-rider-staging'}
 if($selected-lt0-or$P.plan.actorId-cne$mover-or$P.plan.partnerId-cne$partner-or$P.plan.joint-ne($mover-ceq$Mount)-or$P.contract-cne$contract){throw 'Ground transaction plan or contract differs'}
 if((Boolean $P.beforeInCombat)-or(Boolean $P.afterInCombat)-or-not(Boolean $P.traceComplete)){throw 'Ground transaction combat/trace differs'}
 Near $P.minimumTravel 0.25;Near $P.maximumTravel 4;Near $P.clearanceRadius $P.plan.clearanceRadius
 SameJson $P.candidates $P.plan.candidates;SamePoint $P.destination $P.plan.candidates[$selected].point
 $b=$P.before;$a=$P.after;$selection=$P.selection
 if(-not(Boolean $selection.exact)-or$selection.actorId-cne$mover-or@($selection.selectedIds).Count-ne1-or$selection.selectedIds[0]-cne$mover){throw 'Ground transaction exact selection missing'}
 if((Integer $selection.frame)-gt(Integer $P.frameBefore)-or(Number $selection.gameTicks)-gt(Number $P.gameTicksBefore)-or(Integer $P.frameAfter)-le(Integer $P.frameBefore)-or(Number $P.gameTicksAfter)-lt(Number $P.gameTicksBefore)){throw 'Ground transaction selection or clock order differs'}
 foreach($state in @($b,$a)){
  if($state.rider.actor-cne$Rider-or$state.mount.actor-cne$Mount-or$state.relationshipState-cne'Unmounted'){throw 'Ground transaction pair/relationship differs'}
  SameJson $state.selectedIds $selection.selectedIds
 }
 if($null-eq$b.ledger-or$null-eq$b.generation){throw 'Ground transaction ledger missing'};SameJson $b.ledger $a.ledger;SameJson $b.generation $a.generation
 $role='riderPosition';$other='horsePosition';if($mover-ceq$Mount){$role='horsePosition';$other='riderPosition'}
 SamePoint $b.geometry.$role $P.plan.origin;SamePoint $b.geometry.$other $P.plan.center;SamePoint $b.geometry.$other $a.geometry.$other
 $residual=Distance $a.geometry.$role $P.destination;Near $P.residual $residual;if($residual-gt0.06){throw 'Ground transaction arrival exceeds unchanged tolerance'}
 SamePoint $P.originNavigation.position $b.geometry.$role;SamePoint $P.terminalNavigation.position $a.geometry.$role
 $command=$P.admittedCommand;$terminal=$P.terminalCommand;$id=Integer $command.id
 if($id-eq0-or(Integer $terminal.id)-ne$id-or$command.executor-cne$mover-or$terminal.executor-cne$mover-or$command.type-cne'Kingmaker.UnitLogic.Commands.UnitMoveTo'-or$terminal.type-cne$command.type-or(Boolean $command.acted)-or(Boolean $command.finished)-or-not(Boolean $terminal.acted)-or-not(Boolean $terminal.finished)-or$terminal.result-cne'Success'-or-not(Boolean $P.createdByPlayer)){throw 'Ground transaction exact successful command differs'}
 $resources=$P.resourceWindow;Assert-KmcNativeGroundResources $resources
 if($resources.riderId-cne$Rider-or$resources.mountId-cne$Mount-or(Integer $resources.groundCommand.commandObject)-ne$id-or$resources.groundCommand.casterId-cne$mover-or$resources.groundCommand.type-cne$command.type-or-not(Boolean $resources.groundCommand.createdByPlayer)-or-not(Boolean $resources.groundCommand.finished)-or$resources.groundCommand.nativeResult-cne'Success'){throw 'Ground resource command binding differs'}
 SameJson $P.events $resources.events
 foreach($boundary in @('before','after')){
  $state=$P.$boundary;$snapshot=$resources.$boundary;$suffix='Before';if($boundary-ceq'after'){$suffix='After'}
  if((Integer $snapshot.frame)-ne(Integer $P.('frame'+$suffix))-or(Number $snapshot.gameTicks)-ne(Number $P.('gameTicks'+$suffix))){throw 'Ground resource clock binding differs'}
  foreach($actor in @('rider','mount')){
   if($state.$actor.actor-cne$snapshot.$actor.actor-or(Integer $state.$actor.nativePrepareCount)-ne(Integer $snapshot.$actor.grantSequence)){throw 'Ground resource actor/allocation binding differs'}
   foreach($field in @('standard','move','swift','reactions','reactionCooldown','initiativeCooldown','initiativeOrder','reactionsPerRound')){Near $state.$actor.$field $snapshot.$actor.$field}
  }
 }
 $path=$P.path;Assert-KmcChunk6aPathEvidence $path $id $mover $false
 $time=Number $P.gameTicksBefore;$end=Number $P.gameTicksAfter;$pending=$false;$requests=0;$completed=0;$ended=0;$pathObject=0
 foreach($e in $path.events){
  $ticks=Number $e.gameTicks;if($ticks-lt$time-or$ticks-gt$end-or$ended-gt0){throw 'Ground path clock/terminal order differs'};$time=$ticks
  if($e.boundary-ceq'command-bound'){if($e.sequence-ne1){throw 'Ground path bound order'};continue}
  if($e.boundary-ceq'path-request'){
   if($pending-or$e.requestSequence-ne(++$requests)){throw 'Ground path request order'};$pending=$true;$pathObject=$e.pathObject;SamePoint $e.destination $P.destination;continue
  }
  if($e.boundary-cin@('path-complete-before','path-complete-after')){
   if(-not$pending-or$e.pathObject-ne$pathObject-or$e.requestSequence-ne$requests-or(Boolean $e.pathError)-or$e.pathState-cne'Complete'-or@($e.points).Count-lt1){throw 'Ground path completion identity/state differs'}
   if((Distance $e.points[-1] $P.destination)-gt0.06){throw 'Ground path endpoint differs'}
   if($e.boundary-ceq'path-complete-before'){if($completed-ne2*($requests-1)){throw 'Ground path prefix order'}}else{if($completed-ne(2*$requests-1)){throw 'Ground path postfix order'};$pending=$false}
   $completed++;continue
  }
  if($e.boundary-cne'command-ended'-or$pending-or$requests-lt1-or$completed-ne2*$requests-or-not(Boolean $e.acted)-or-not(Boolean $e.finished)-or$e.result-cne'Success'){throw 'Ground path successful terminal differs'};$ended++
 }
 if($ended-ne1-or$requests-lt1){throw 'Ground path lifecycle incomplete'}
}

function Assert-KmcNativeJointGround($P,$RiderStep,[string]$Rider,[string]$Mount) {
 function Same($a,$b){if(($a|ConvertTo-Json -Depth 90 -Compress)-cne($b|ConvertTo-Json -Depth 90 -Compress)){throw 'Joint ground envelope binding differs'}}
 if($P.contract-cne'bounded-joint-plan-before-two-separate-native-ground-inputs'-or$P.maximumMountCandidates-ne72-or$P.maximumRiderCandidatesPerMount-ne72-or$P.maximumJointCandidates-ne5256){throw 'Joint ground search contract/bounds differ'}
 $original=$P.initialRiderFailure;$joint=$P.jointPlan
 if((Assert-KmcNativeGroundPlan $original)-ne-1-or$original.joint-ne$false-or$original.actorId-cne$Rider-or$original.partnerId-cne$Mount){throw 'Original 72 failed rider proposals missing'}
 if((Assert-KmcNativeGroundPlan $joint)-lt0-or$joint.joint-ne$true-or$joint.actorId-cne$Mount-or$joint.partnerId-cne$Rider){throw 'Joint ground feasibility missing'}
 Same $original.origin $joint.center;Same $original.center $joint.origin;Same $original.occupants $joint.occupants
 $initial=$P.initialState;if($initial.relationshipState-cne'Unmounted'){throw 'Joint ground initial baseline missing'}
 Same $initial.geometry.riderPosition $original.origin;Same $initial.geometry.horsePosition $original.center
 $horse=$P.ground;Assert-KmcNativeGroundTransaction $horse $Rider $Mount;Assert-KmcNativeGroundTransaction $RiderStep $Rider $Mount
 if($horse.moverId-cne$Mount-or$RiderStep.moverId-cne$Rider-or$horse.admittedCommand.id-eq$RiderStep.admittedCommand.id){throw 'Joint ground input identity differs'}
 Same $horse.plan $joint
 foreach($field in @('rider','mount','ledger','generation','relationshipState')){Same $initial.$field $horse.before.$field}
 foreach($field in @('riderPosition','horsePosition')){Same $initial.geometry.$field $horse.before.geometry.$field;Same $horse.after.geometry.$field $RiderStep.before.geometry.$field}
 Same $horse.resourceWindow.after $RiderStep.resourceWindow.before
 foreach($field in @('rider','mount','ledger','generation','relationshipState')){Same $horse.after.$field $RiderStep.before.$field}
 if($null-ne$RiderStep.plan.partnerPositionOverride-or$RiderStep.plan.joint-ne$false){throw 'Actual rider search reused counterfactual plan'}
 Same $RiderStep.plan.origin $horse.after.geometry.riderPosition;Same $RiderStep.plan.center $horse.after.geometry.horsePosition
}

function Assert-KmcChunk6aGroundSetupEnvelope($Observations,$Proof) {
 $setup=$Observations.chunk6aPreCombatPositioning;$rider=$Proof.identity.casterId;$mount=$Proof.mountId
 if($null-eq$setup-or$null-eq$setup.PSObject.Properties['plan']){throw 'Current TB setup omitted the bounded native ground/resource proof'}
 Assert-KmcNativeGroundTransaction $setup $rider $mount
 if($setup.moverId-cne$rider-or$setup.before.generation-ne$Proof.identity.generationAtInit){throw 'Ground setup belongs to another relationship request'}
 $windows=@($setup.resourceWindow)
 $jointProperty=$Observations.PSObject.Properties['chunk6aPreCombatJointPositioning']
 if($Observations -is [System.Collections.IDictionary]){$jointProperty=$null;if($Observations.Contains('chunk6aPreCombatJointPositioning')){$jointProperty=[pscustomobject]@{Value=$Observations['chunk6aPreCombatJointPositioning']}}}
 if($null-ne$jointProperty){
  $joint=$jointProperty.Value;Assert-KmcNativeJointGround $joint $setup $rider $mount
  $windows=@($joint.ground.resourceWindow)+$windows
 }
 $trace=$Observations.actorAllocationTrace
 if(($trace.dropped-isnot[int]-and$trace.dropped-isnot[long])-or$trace.dropped-ne0-or($trace.observationErrors-isnot[int]-and$trace.observationErrors-isnot[long])-or$trace.observationErrors-ne0){throw 'Ground setup full allocation trace incomplete'}
 foreach($window in $windows){
  if(($window.observerHooks|ConvertTo-Json -Depth 30 -Compress)-cne($trace.observerHooks|ConvertTo-Json -Depth 30 -Compress)){throw 'Ground setup observer hooks differ from full trace'}
  $events=@($trace.events|Where-Object {$_.sequence-gt$window.before.allocationSequence-and$_.sequence-le$window.after.allocationSequence})
  if(($events|ConvertTo-Json -Depth 80 -Compress)-cne(@($window.events)|ConvertTo-Json -Depth 80 -Compress)){throw 'Ground setup events differ from full allocation trace'}
 }
}

