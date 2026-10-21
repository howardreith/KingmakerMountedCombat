Set-StrictMode -Version Latest
function Test-KmcChunk7SmallScenario([string]$Scenario){$Scenario-ceq'chunk7-small-profile-calibration'}
function Get-KmcChunk7SmallRows {@('chunk7-small-profile-calibration','C7-small-readonly-source','C7-small-native-party-placement','C7-small-production-mount','C7-small-native-ground','C7-small-rider-primary','C7-small-mount-primary','C7-small-scroll-presentation','C7-small-potion-presentation','C7-small-native-dismount','C7-small-native-pose-observation','C7-small-cleanup','C7-small-runtime','C7-small-interrupted')}
function Assert-KmcChunk7SmallSelection($Request){
 if($Request-is[Collections.IDictionary]){$Request=[pscustomobject]$Request}
 $has=$null-ne$Request.PSObject.Properties['smallCalibration']
 if(-not(Test-KmcChunk7SmallScenario $Request.scenario)){if($has){throw 'Small calibration selection is forbidden under another scenario.'};return}
 if(-not$has){throw 'Exact Small calibration selection is missing.'};$s=$Request.smallCalibration
 $manual=Test-KmcChunk7SmallManualCase $s;$f09=Test-KmcChunk7SmallF09Case $s
 if($f09){Assert-KmcExactProperties $s @('case','fixtureSource','profileId','nativeInventorySha256','nativeCalibrationSha256') 'F09 Small calibration selection'}
 elseif($manual){Assert-KmcExactProperties $s @('case','manualSource','profileId','nativeInventorySha256','nativeCalibrationSha256') 'Small Manual calibration selection'}
 else{Assert-KmcExactProperties $s @('case','source','profileId','nativeInventorySha256','nativeCalibrationSha256') 'Small calibration selection'}
 if($s.case-isnot[string]-or($s.case-cne'compiled-small-horse-rt'-and-not$manual-and-not$f09)-or$s.profileId-isnot[string]-or$s.profileId-cnotmatch'^small-[a-z0-9]+(?:-[a-z0-9]+)*-horse-v[1-9][0-9]*$'){throw 'Exact compiled Small profile/case is required.'}
 foreach($key in @('nativeInventorySha256','nativeCalibrationSha256')){if($s.$key-isnot[string]-or$s.$key-cnotmatch'^[a-f0-9]{64}$'){throw 'Immutable native Small declaration provenance is required.'}}
 if($f09){Assert-KmcChunk7SmallF09SourceSelection $s.fixtureSource $Request.runId}
 elseif($manual){Assert-KmcChunk7SmallManualSourceSelection $s.manualSource $Request.runId}
 else{Assert-KmcChunk7FieldSelection ([pscustomobject]@{scenario='chunk7-ordinary-field-admission';runId=$Request.runId;ordinaryField=$s.source})}
}
function Assert-KmcChunk7SmallLoadDescriptor($Request){
 Assert-KmcChunk7SmallSelection $Request
 if(-not(Test-KmcChunk7SmallScenario $Request.scenario)){throw 'Small descriptor requires its exact closed family.'}
 if(Test-KmcChunk7SmallF09Case $Request.smallCalibration){Assert-KmcChunk7SmallF09Descriptor $Request}
 elseif(Test-KmcChunk7SmallManualCase $Request.smallCalibration){Assert-KmcChunk7SmallManualDescriptor $Request}
 else{Assert-KmcChunk7FieldLoadDescriptor ([pscustomobject]@{scenario='chunk7-ordinary-field-admission';runId=$Request.runId;ordinaryField=$Request.smallCalibration.source;fixture=$Request.fixture;persistenceLoad=$Request.persistenceLoad})}
}
function Assert-KmcChunk7SmallNumber($Value,[double]$Minimum=0,[double]$Maximum=[double]::MaxValue){if(-not(Test-KmcChunk7FiniteNumber $Value)-or$Value-lt$Minimum-or$Value-gt$Maximum){throw 'Native Small measurement is missing, nonnumeric or out of bounds.'}}
function Assert-KmcChunk7SmallVector($Value){Assert-KmcExactProperties $Value @('x','y','z') 'native vector';foreach($k in @('x','y','z')){Assert-KmcChunk7SmallNumber $Value.$k (-[double]::MaxValue)}}
function Assert-KmcChunk7SmallState($State,$ActorIds){
 Assert-KmcOrdinaryInteger $State.frame 0 ([int]::MaxValue);Assert-KmcOrdinaryInteger $State.gameTicks 0
 Assert-KmcChunk7ExactIds $State.actors $ActorIds
 foreach($a in $State.actors){Assert-KmcOrdinaryInteger $a.object ([int]::MinValue) ([int]::MaxValue);if($a.object-eq0){throw 'Native Small actor label absent.'}
  foreach($k in @('standard','move','swift','reactionCooldown')){Assert-KmcChunk7SmallNumber $a.$k}
  foreach($k in @('reactions','reactionAllowance')){Assert-KmcOrdinaryInteger $a.$k 0 ([int]::MaxValue)}
  if($a.position-isnot[Array]-or$a.position.Count-ne3){throw 'Native position missing.'};foreach($v in $a.position){Assert-KmcChunk7SmallNumber $v (-[double]::MaxValue)}
 }
 if($State.owners-isnot[Array]-or$State.owners.Count-gt1){throw 'Small calibration admits one exact owner only.'}
 foreach($owner in $State.owners){if($null-eq$owner-or$owner.GetType().FullName-cne'System.Management.Automation.PSCustomObject'-or$owner.state-isnot[string]){throw 'Native Small owner must be an object with scalar state.'};Assert-KmcOrdinaryGuid $owner.id;Assert-KmcOrdinaryInteger $owner.generation 1}
}
function Assert-KmcChunk7SmallResourceEqual($Left,$Right){
 foreach($a in $Left.actors){$b=@($Right.actors|Where-Object id -CEQ $a.id);if($b.Count-ne1){throw 'Actor identity disappeared.'}
  foreach($k in @('object','standard','move','swift','reactionCooldown','reactions','reactionAllowance')){if((ConvertTo-KmcChunk7Canonical $a.$k)-cne(ConvertTo-KmcChunk7Canonical $b[0].$k)){throw ('Exploration action changed native expenditure: '+$k)}}
 }
}
function Assert-KmcChunk7SmallTrace($Request,$Observation){
 $trace=$Observation.allocationTrace;$registry=$trace.identityRegistry
 Assert-KmcOrdinaryInteger $trace.dropped 0 0;Assert-KmcOrdinaryInteger $trace.observationErrors 0 0
 if($registry.contract-isnot[string]-or$registry.contract-cne'retained-native-object-identity-v1'){throw 'Native trace identity contract differs.'}
 Assert-KmcOrdinaryInteger $registry.capacity 16000 16000;Assert-KmcOrdinaryInteger $registry.faults 0 0
 Assert-KmcChunk7Boolean $registry.released $false;Assert-KmcOrdinaryInteger $registry.retainedCount 1 16000
 if($registry.ids-isnot[Array]-or$registry.ids.Count-ne$registry.retainedCount){throw 'Native trace retained identity count differs.'}
 $labels=[Collections.Generic.HashSet[int]]::new()
 foreach($id in $registry.ids){Assert-KmcOrdinaryInteger $id ([int]::MinValue) ([int]::MaxValue);if($id-eq0-or-not$labels.Add($id)){throw 'Native trace labels must be distinct signed nonzero native object identities.'}}
 if($trace.observerHooks-isnot[Array]-or$trace.observerHooks.Count-eq0-or$trace.observerHooks.Count-gt64){throw 'Bounded native trace hook inventory missing.'}
 foreach($hook in $trace.observerHooks){
  if($hook.method-isnot[string]-or[string]::IsNullOrWhiteSpace($hook.method)-or$hook.method.Length-gt256-or$hook.token-isnot[string]-or$hook.token-cnotmatch'^060[0-9A-F]{5}$'){throw 'Native trace hook metadata shape differs.'}
  Assert-KmcOrdinaryGuid $hook.moduleMvid
 }
 if($trace.events-isnot[Array]-or$trace.events.Count-eq0-or$trace.events.Count-gt16000){throw 'Bounded native trace events missing.'}
 $target=$Observation.target
 Assert-KmcOrdinaryGuid $Observation.targetId;Assert-KmcOrdinaryGuid $target.id
 Assert-KmcOrdinaryInteger $target.object ([int]::MinValue) ([int]::MaxValue)
 if($target.id-cne$Observation.targetId-or-not$labels.Contains($target.object)-or
    $target.blueprint-isnot[string]-or$target.blueprint-cne'e7aa96d15a45238438ae4cfb476f6bb9'-or
    $target.groupId-isnot[string]-or$target.groupId-cne('KMC.RuntimeHostile.'+$Request.runId+':small-profile-target')){throw 'Exact owned diagnostic target provenance differs.'}
 Assert-KmcChunk7Boolean $target.nativeDiagnosticFixture $true
 foreach($key in @('playerResourceWrites','initiativeWrites')){Assert-KmcOrdinaryInteger $target.$key 0 0}
 $observedActors=@($Observation.inputs[0].before.actors)
 if(@($observedActors|Where-Object{$_.id-ceq$target.id-or$_.object-eq$target.object}).Count){throw 'Owned diagnostic target conflicts with a controlled actor.'}
 $observedActors+=,[pscustomobject]@{id=$target.id;object=$target.object}
 $sequence=0
 foreach($event in $trace.events){
  $sequence++;Assert-KmcOrdinaryInteger $event.sequence $sequence $sequence;Assert-KmcOrdinaryInteger $event.frame 0 ([int]::MaxValue)
  if($event.boundary-isnot[string]-or[string]::IsNullOrWhiteSpace($event.boundary)){throw 'Native trace boundary name differs.'}
  Assert-KmcOrdinaryGuid $event.state.actor;Assert-KmcOrdinaryInteger $event.state.actorObject ([int]::MinValue) ([int]::MaxValue)
  $actor=@($observedActors|Where-Object id -CEQ $event.state.actor)
  if(-not$labels.Contains($event.state.actorObject)-or$actor.Count-ne1-or$actor[0].object-ne$event.state.actorObject){throw 'Native trace actor label differs from the retained action actor.'}
  Assert-KmcOrdinaryInteger $event.command ([int]::MinValue) ([int]::MaxValue)
  if($event.command-ne0-and-not$labels.Contains($event.command)){throw 'Native trace command is not retained by its identity registry.'}
 }
 foreach($input in $Observation.inputs){Assert-KmcOrdinaryInteger $input.shell ([int]::MinValue) ([int]::MaxValue);if(-not$labels.Contains($input.shell)){throw 'Native ability shell is absent from its retained trace registry.'}}
 Assert-KmcOrdinaryInteger $Observation.movement.command ([int]::MinValue) ([int]::MaxValue)
 if(-not$labels.Contains($Observation.movement.command)){throw 'Native movement command is absent from its retained trace registry.'}
 foreach($state in @($Observation.inputs|ForEach-Object{$_.before;$_.after})+@($Observation.movement.before,$Observation.movement.after)){
  foreach($actor in $state.actors){if(-not$labels.Contains($actor.object)){throw 'Native action actor is absent from its retained trace registry.'}}
 }
 Assert-KmcChunk7CheckpointTrace $trace $Request
}
function Assert-KmcChunk7SmallMountedEvidence($Request,$o){
 $s=$Request.smallCalibration;$source=Get-KmcChunk7SmallSelectedSource $s;$d=$o.declaration
 if($d.profileId-cne$s.profileId-or$d.nativeInventorySha256-cne$s.nativeInventorySha256-or$d.nativeCalibrationSha256-cne$s.nativeCalibrationSha256-or$d.qualificationStatus-cne'IN PROGRESS'-or$d.pose.Id-cne$s.profileId){throw 'Observed compiled declaration differs or claims prior acceptance.'}
 foreach($f in @('diagnosticsOffProven','humanVisualAcceptance','nativePreparationManufactured','resourceFieldsWritten','poseQualified')){Assert-KmcChunk7Boolean $o.$f $false}
 Assert-KmcChunk7Boolean $o.representativeNativeActionsObserved $true
 if($o.breadthQualification-cne'IN PROGRESS'){throw 'Bounded calibration cannot auto-promote breadth qualification.'}
 if($o.inputs-isnot[Array]-or$o.inputs.Count-ne4){throw 'Exactly Mount, two primaries and Dismount are required.'}
 $kinds=@('native-mount','rider-primary','mount-primary','native-dismount')
 $ability=@('f053faad986631688defa003cd7bda0e','27364df661b3c121eabb97a31aa73a83','f88a50d6fdbebbd709c3e323d2f52f5e','3af2b81f4d72bbb30501fa730fcdf36e')
 if((Test-KmcChunk7SmallManualCase $s)-or(Test-KmcChunk7SmallF09Case $s)){$ids=@($o.manualNativeRoster.mainActorId)+@($o.manualNativeRoster.actors.id|Where-Object{$_-cne$o.manualNativeRoster.mainActorId-and$_-cne$source.actorId-and$_-cne$source.petId}|Sort-Object -CaseSensitive)+@($source.actorId,$source.petId)}
 else{$ids=@('b6628a77-4962-47a4-a17c-88d9836fc9d5','d79a4f6c-b74e-4868-95bd-533899131acb','d17c8fd0-6627-40c4-bf9a-024cf001c1f7',$source.actorId,$source.petId)}
 $prior=-1
 $ownerRiderId=if(Test-KmcChunk7SmallF09Case $s){$source.actorId.Replace('-','')}else{$source.actorId}
 $ownerMountId=if(Test-KmcChunk7SmallF09Case $s){$source.petId.Replace('-','')}else{$source.petId}
 $mountedOwner=$o.inputs[0].after.owners
 if($mountedOwner-isnot[Array]-or$mountedOwner.Count-ne1){throw 'Native Mount has no exact owner.'}
 $mountedOwner=$mountedOwner[0]
 if($null-eq$mountedOwner-or$mountedOwner.GetType().FullName-cne'System.Management.Automation.PSCustomObject'-or$mountedOwner.state-isnot[string]-or$mountedOwner.riderId-isnot[string]-or$mountedOwner.mountId-isnot[string]){throw 'Native mounted owner role/state is not a scalar object field.'}
 if($mountedOwner.riderId-cne$ownerRiderId-or$mountedOwner.mountId-cne$ownerMountId-or$mountedOwner.state-cne'Active'){throw 'Native mounted owner identity/state differs.'}
 $states=@($o.inputs|ForEach-Object{$_.before;$_.after})+@($o.movement.before,$o.movement.after)
 foreach($state in $states){
  foreach($a in $state.actors){$first=@($o.inputs[0].before.actors|Where-Object id -CEQ $a.id);if($first.Count-ne1-or$a.object-ne$first[0].object){throw 'Native actor object changed between action observations.'}}
  foreach($owner in $state.owners){if($null-eq$owner-or$owner.GetType().FullName-cne'System.Management.Automation.PSCustomObject'-or$owner.state-isnot[string]-or$owner.riderId-isnot[string]-or$owner.mountId-isnot[string]-or$owner.id-cne$mountedOwner.id-or$owner.riderId-cne$ownerRiderId-or$owner.mountId-cne$ownerMountId-or$owner.generation-ne$mountedOwner.generation){throw 'Native pair owner changed between action observations.'}}
 }
 if($o.inputs[0].before.owners.Count-ne0){throw 'Mount input began with existing ownership.'}
 foreach($state in @($o.inputs[0].after,$o.inputs[1].before,$o.inputs[1].after,$o.inputs[2].before,$o.inputs[2].after,$o.inputs[3].before,$o.movement.before,$o.movement.after)){if($state.owners.Count-ne1-or$state.owners[0].state-cne'Active'){throw 'Mounted action lost its exact active owner.'}}
 if($o.inputs[3].after.owners.Count-ne0-and$o.inputs[3].after.owners[0].state-cne'Detached'){throw 'Native Dismount left active ownership.'}
 for($i=0;$i-lt4;$i++){
  $row=$o.inputs[$i];if($row.kind-cne$kinds[$i]-or$row.casterId-cne$source.actorId-or$row.abilityGuid-cne$ability[$i]-or$row.selectionAfterInput-cne$ids[0]){throw 'Native calibration input kind/caster/ability/selection differs.'}
  Assert-KmcChunk7Boolean $row.clicked $true;Assert-KmcChunk7Boolean $row.terminal $true;Assert-KmcChunk7Boolean $row.processTerminal $true
  Assert-KmcOrdinaryInteger $row.castDelta 1 1;Assert-KmcOrdinaryInteger $row.refusalDelta 0 0;Assert-KmcOrdinaryInteger $row.shell ([int]::MinValue) ([int]::MaxValue);if($row.shell-eq0){throw 'Exact native ability shell absent.'}
  Assert-KmcChunk7SmallState $row.before $ids;Assert-KmcChunk7SmallState $row.after $ids
  if($row.before.frame-lt$prior-or$row.after.frame-lt$row.before.frame){throw 'Native input observation ordering differs.'};$prior=$row.after.frame
  if($i-eq0-or$i-eq3){
   if($i-eq0-and(Test-KmcChunk7SmallF09Case $s)){
    Assert-KmcOrdinaryInteger $row.generation 0 0;Assert-KmcOrdinaryInteger $mountedOwner.generation 1 1
   }else{
    Assert-KmcOrdinaryInteger $row.generation 1;if($row.generation-ne$mountedOwner.generation){throw 'Native relationship command generation differs from its owner.'}
   };Assert-KmcChunk7SmallResourceEqual $row.before $row.after
   if($row.targetId-cne$(if($i-eq0){$source.petId}else{$source.actorId})){throw 'Native relationship target differs.'}
  }else{
   $owner=if($i-eq1){$source.actorId}else{$source.petId};$other=if($i-eq1){$source.petId}else{$source.actorId}
   if($row.targetId-cne$o.targetId-or$row.resourceOwnerId-cne$owner){throw 'Primary native target or resource owner differs.'}
   $out=$row.outcome;if($out.actorId-cne$owner-or$out.resourceOwnerId-cne$owner-or$out.targetId-cne$o.targetId-or$out.result-cne'Success'){throw 'Primary outcome identity differs.'}
   Assert-KmcChunk7Boolean $out.nativeAttackRule $true;Assert-KmcChunk7Boolean $out.singleAttack $true;Assert-KmcOrdinaryInteger $out.completedAttacks 1 1
   if($row.nativeEvents-isnot[Array]-or$row.nativeEvents.Count-eq0-or$row.nativeEvents.Count-gt16000){throw 'Raw native primary trace is absent or oversized.'}
   Assert-KmcOrdinaryInteger $row.traceOffset 0 16000
   if($o.allocationTrace.events-isnot[Array]-or($row.traceOffset+$row.nativeEvents.Count)-gt$o.allocationTrace.events.Count-or (ConvertTo-KmcChunk7Canonical @($o.allocationTrace.events|Select-Object -Skip $row.traceOffset -First $row.nativeEvents.Count))-cne(ConvertTo-KmcChunk7Canonical $row.nativeEvents)){throw 'Primary receipt is not its exact bounded raw trace slice.'}
   $costs=@($row.nativeEvents|Where-Object{$_.boundary-ceq'cost-after'-and$_.commandType-ceq'KingmakerMountedCombat.Integration.MountedPairAttackCommand'});$beforeCost=@($row.nativeEvents|Where-Object{$_.boundary-ceq'cost-before'-and$_.commandType-ceq'KingmakerMountedCombat.Integration.MountedPairAttackCommand'})
   if($costs.Count-ne1-or$beforeCost.Count-ne1){throw 'Exactly one concrete native actor cost pair required.'}
   $a=$costs[0];$b=$beforeCost[0]
   foreach($event in @($a,$b)){Assert-KmcChunk7Boolean $event.acted $true;Assert-KmcChunk7Boolean $event.nativeTurnBased $false;if($event.actionType-cne'Standard'){throw 'Native RT primary action type differs.'};foreach($key in @('move','swift','reactionCooldown')){Assert-KmcChunk7SmallNumber $event.state.$key};Assert-KmcOrdinaryInteger $event.state.reactions 0}
   foreach($key in @('move','swift','reactionCooldown','reactions')){if((ConvertTo-KmcChunk7Canonical $a.state.$key)-cne(ConvertTo-KmcChunk7Canonical $b.state.$key)){throw 'Native cost changed another resource.'}}
   foreach($event in @($a,$b)){if($event.state.actor-cne$owner-or$event.commandActor-cne$owner-or$event.commandType-cne'KingmakerMountedCombat.Integration.MountedPairAttackCommand'){throw 'Native actor expenditure identity differs.'};Assert-KmcOrdinaryInteger $event.command ([int]::MinValue) ([int]::MaxValue);if($event.command-eq0){throw 'Native cost command absent.'};Assert-KmcChunk7SmallNumber $event.state.standard;Assert-KmcOrdinaryInteger $event.sequence 1;Assert-KmcOrdinaryInteger $event.frame $row.before.frame $row.after.frame}
   if($a.command-ne$b.command-or$a.sequence-le$b.sequence-or$a.state.standard-le$b.state.standard){throw 'Concrete native standard expenditure was not observed.'}
  }
 }
 $m=$o.movement;Assert-KmcChunk7SmallState $m.before $ids;Assert-KmcChunk7SmallState $m.after $ids
 Assert-KmcChunk7SmallResourceEqual $m.before $m.after;Assert-KmcChunk7Boolean $m.clicked $true;Assert-KmcChunk7Boolean $m.terminal $true
 Assert-KmcChunk7SmallNumber $m.actualDistance .020000001 100
 $start=@($m.before.actors|Where-Object id -CEQ $source.petId)[0].position;$finish=@($m.after.actors|Where-Object id -CEQ $source.petId)[0].position
 $sum=0.0;for($axis=0;$axis-lt3;$axis++){$sum+=[Math]::Pow(($finish[$axis]-$start[$axis]),2)}
 if([Math]::Abs([Math]::Sqrt($sum)-$m.actualDistance)-gt0.00001){throw 'Native displacement differs from raw positions.'}
 if($m.casterId-cne$source.actorId-or$m.executorId-cne$source.petId-or$m.selectionAfterInput-cne$ids[0]-or$m.before.frame-lt$o.inputs[0].after.frame-or$m.after.frame-gt$o.inputs[1].before.frame){throw 'Native mount movement identity/window differs.'}
 Assert-KmcOrdinaryInteger $m.command ([int]::MinValue) ([int]::MaxValue);if($m.command-eq0){throw 'Native transport command absent.'}
 $phases=@('unmounted-field','mounted-idle','mounted-moving','mounted-arrived','rider-primary-complete','mount-primary-complete','dismounted-field')
 if($o.poses-isnot[Array]-or$o.poses.Count-ne$phases.Count){throw 'Exact native pose phases required.'};$last=-1
 for($i=0;$i-lt$phases.Count;$i++){
  $row=$o.poses[$i];$p=$row.observation
  if($row.phase-cne$phases[$i]-or$p.riderId-cne$source.actorId-or$p.mountId-cne$source.petId-or$p.profileId-cne$s.profileId-or$p.inventorySha256-cne$s.nativeInventorySha256-or$p.declarationSha256-cne$s.nativeCalibrationSha256-or$p.coordinateConvention-cne'native-Unity'-or$p.qualificationStatus-cne'IN PROGRESS'){throw 'Pose phase/body/declaration provenance differs.'}
  if((ConvertTo-KmcChunk7Canonical $p.body)-cne(ConvertTo-KmcChunk7Canonical $o.bodyObservation)){throw 'Mounted phase changed the exact observed native Small body.'}
  Assert-KmcChunk7Boolean $p.visualAcceptanceProven $false;Assert-KmcOrdinaryInteger $p.frame $last ([int]::MaxValue);$last=$p.frame
  $windows=@(@(0,$o.inputs[0].before.frame),@($o.inputs[0].after.frame,$m.before.frame),@($m.before.frame,$m.after.frame),@($m.after.frame,$m.after.frame),@($o.inputs[1].after.frame,$o.inputs[1].after.frame),@($o.inputs[2].after.frame,$o.inputs[2].after.frame),@($o.inputs[3].after.frame,$o.inputs[3].after.frame))
  if($p.frame-lt$windows[$i][0]-or$p.frame-gt$windows[$i][1]){throw 'Pose was not observed in its exact native action window.'}
  foreach($rootName in @('riderRoot','horseRoot')){
   $rootNode=$p.$rootName;Assert-KmcOrdinaryInteger $rootNode.instanceId ([int]::MinValue) ([int]::MaxValue);if($rootNode.instanceId-eq0-or$rootNode.instanceId-ne$o.poses[0].observation.$rootName.instanceId-or$rootNode.name-cne$o.poses[0].observation.$rootName.name){throw 'Native rig root identity changed.'}
   Assert-KmcOrdinaryInteger $rootNode.parentId ([int]::MinValue) ([int]::MaxValue)
   foreach($v in @('localPosition','localEuler','localScale','worldPosition','worldScale')){Assert-KmcChunk7SmallVector $rootNode.$v}
  }
  $names=@($d.pose.PelvisBoneName,$d.pose.LeftLeg.ThighBoneName,$d.pose.LeftLeg.LowerLegBoneName,$d.pose.LeftLeg.FootBoneName,$d.pose.RightLeg.ThighBoneName,$d.pose.RightLeg.LowerLegBoneName,$d.pose.RightLeg.FootBoneName)
  foreach($nodes in @(@{Rows=$p.riderBones;Names=$names},@{Rows=$p.horseAnchors;Names=@('Chest','L_Stirrup','R_Stirrup')})){
   if($nodes.Rows-isnot[Array]-or$nodes.Rows.Count-ne$nodes.Names.Count){throw 'Exact native bone/anchor inventory missing.'}
   for($j=0;$j-lt$nodes.Names.Count;$j++){$node=$nodes.Rows[$j];if($node.name-isnot[string]-or$node.name-cne$nodes.Names[$j]){throw 'Native pose transform identity differs.'};Assert-KmcOrdinaryInteger $node.instanceId ([int]::MinValue) ([int]::MaxValue);if($node.instanceId-eq0){throw 'Native transform absent.'};Assert-KmcOrdinaryInteger $node.parentId ([int]::MinValue) ([int]::MaxValue);foreach($v in @('localPosition','localEuler','localScale','worldPosition','worldScale')){Assert-KmcChunk7SmallVector $node.$v}}
  }
  foreach($collection in @('riderBones','horseAnchors')){for($j=0;$j-lt$p.$collection.Count;$j++){$first=$o.poses[0].observation.$collection[$j];$node=$p.$collection[$j];if($node.instanceId-ne$first.instanceId-or$node.parentId-ne$first.parentId){throw 'Native pose bone/anchor object changed.'}}}
  $distances=@(
   @{Name='leftFootToLeftStirrup';A=$p.riderBones[3];B=$p.horseAnchors[1]},@{Name='leftFootToRightStirrup';A=$p.riderBones[3];B=$p.horseAnchors[2]},
   @{Name='rightFootToLeftStirrup';A=$p.riderBones[6];B=$p.horseAnchors[1]},@{Name='rightFootToRightStirrup';A=$p.riderBones[6];B=$p.horseAnchors[2]},
   @{Name='leftUpperLength';A=$p.riderBones[1];B=$p.riderBones[2]},@{Name='leftLowerLength';A=$p.riderBones[2];B=$p.riderBones[3]},
   @{Name='rightUpperLength';A=$p.riderBones[4];B=$p.riderBones[5]},@{Name='rightLowerLength';A=$p.riderBones[5];B=$p.riderBones[6]})
  foreach($distance in $distances){$squared=0.0;foreach($axis in @('x','y','z')){$squared+=[Math]::Pow(($distance.A.worldPosition.$axis-$distance.B.worldPosition.$axis),2)};Assert-KmcChunk7SmallNumber $p.($distance.Name);if([Math]::Abs([Math]::Sqrt($squared)-$p.($distance.Name))-gt0.00001){throw 'Native pose distance differs from its raw world transforms.'}}
  foreach($n in @('leftFootToLeftStirrup','leftFootToRightStirrup','rightFootToLeftStirrup','rightFootToRightStirrup','leftUpperLength','leftLowerLength','rightUpperLength','rightLowerLength')){Assert-KmcChunk7SmallNumber $p.$n}
  Assert-KmcChunk7SmallVector $p.pelvisFromChestMountLocal
  if($i-gt0-and$i-lt6){$r=$p.runtime;if($r.profileId-cne$s.profileId-or$r.poseProfileId-cne$s.profileId){throw 'Native pose was not the compiled production profile.'};foreach($f in @('configured','healthy')){Assert-KmcChunk7Boolean $r.$f $true};if($r.frameApplied-isnot[bool]){throw 'Pose frame flag type differs.'};Assert-KmcOrdinaryInteger $r.applications 1;Assert-KmcOrdinaryInteger $r.footClamps 0;Assert-KmcOrdinaryInteger $r.componentCount 1 1;Assert-KmcOrdinaryInteger $r.boneCount 7 7;foreach($n in @('maximumFootResidual','maximumKneeResidual','maximumSegmentResidual')){Assert-KmcChunk7SmallNumber $r.$n};if($r.failure){throw 'Production pose recorded a failure.'}}
 }
 Assert-KmcChunk7SmallTrace $Request $o
 foreach($f in @('nativeCommandsSettled','poseResidueAbsent','targetRemoved','observerReleased','modeRestored','pairAiRestored')){Assert-KmcChunk7Boolean $o.cleanup.$f $true}
 Assert-KmcOrdinaryInteger $o.cleanup.ownerCount 0 0
}
function Assert-KmcChunk7SmallEvidence($Request,$Artifact,[string]$Status='PASS'){
    Assert-KmcChunk7SmallLoadDescriptor $Request
    Assert-KmcChunk7ArtifactEnvelope $Request $Artifact 'chunk7-small-calibration-evidence'
    Assert-KmcOrdinaryInteger $Artifact.schemaVersion 1 1
    if($Status-ceq'PASS'-and$Artifact.status-cne'PASS'){throw 'PASS field result requires a PASS artifact.'}
    if($Artifact.status-cne'PASS'){return}
    if(Test-KmcChunk7SmallF09Case $Request.smallCalibration){Assert-KmcChunk7SmallF09Evidence $Request $Artifact;return}
    if(Test-KmcChunk7SmallManualCase $Request.smallCalibration){Assert-KmcChunk7SmallManualEvidence $Request $Artifact;return}
    $o=$Artifact.observations;$s=$Request.smallCalibration.source
    if((ConvertTo-KmcChunk7Canonical $o.selection)-cne(ConvertTo-KmcChunk7Canonical $Request.smallCalibration)){throw 'Field observed explicit selection differs.'}
    Assert-KmcOrdinaryInteger $o.processId 1 ([int]::MaxValue)
    if(-not(Test-KmcChunk7FiniteNumber $o.elapsedSeconds)-or$o.elapsedSeconds-lt0-or$o.elapsedSeconds-gt420){throw 'Bounded native field elapsed observation invalid.'}
    foreach($flag in @('sourceWasRemoteAutosave','sourceArchiveStillRemote')){Assert-KmcChunk7Boolean $o.$flag $true}
    foreach($flag in @('smallPoseQualified','mountedCombatProven','freshMainCampaignProven','manualPartyMutation')){Assert-KmcChunk7Boolean $o.$flag $false}
    $source=$o.source
    if($source.runId-cne$s.sourceRunId-or$source.artifactSha256-cne$s.sourceArtifactSha256-or$source.archiveSha256-cne$s.sourceArchiveSha256-or
       $source.archiveLeaf-cne'Auto_1.zks'-or$source.archiveName-cne$Request.persistenceLoad.internalName){throw 'Field source receipt differs from the explicitly qualified source.'}
    Assert-KmcOrdinaryInteger $source.archiveLength $Request.persistenceLoad.length $Request.persistenceLoad.length
    Assert-KmcChunk7Boolean $source.sameCandidate $true;Assert-KmcChunk7Boolean $source.unchangedArchive $true
    Assert-KmcChunk7OrdinarySelection ([pscustomobject]@{scenario='chunk7-ordinary-acquisition';runId=$s.sourceRunId;ordinaryAcquisition=$source.bodySelection})
    if($source.bodySelection.case-cne'paid-small-ranger-horse'){throw 'Field source was not a paid Small acquisition.'}
    $root=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($Request.evidenceRoot))) ('runtime-staging\persistence-'+$Request.runId+'\Saved Games')
    Assert-KmcChunk7FieldAuthority $o.authority $root $o.authority.generation
    Assert-KmcChunk7FieldAuthority $o.cleanup.authority $root $o.authority.generation
    Assert-KmcChunk7FieldSnapshot $o.before $s;Assert-KmcChunk7FieldSnapshot $o.field $s
    if($o.before.riderInGame-cne$false-or$s.actorId-cin$o.before.partyCharacters-or$s.actorId-cnotin$o.before.remoteCompanions){throw 'The cold acquisition archive was not an exact remote companion.'}
    foreach($flag in @('riderInGame','petInGame','riderInState','petInState','riderAgentReady','petAgentReady')){Assert-KmcChunk7Boolean $o.field.$flag $true}
    foreach($key in @('actorId','actorObject','descriptorObject','petId','petObject','petDescriptorObject','money','xp','level','petLevel','rank','featureSourceGuid','reciprocal')){
        if((ConvertTo-KmcChunk7Canonical $o.before.$key)-cne(ConvertTo-KmcChunk7Canonical $o.field.$key)){throw ('Field admission changed native identity/progression: '+$key)}
    }
    if($o.field.partyCharacters.Count-ne($o.before.partyCharacters.Count+1)-or$s.actorId-cnotin$o.field.partyCharacters-or$s.actorId-cin$o.field.remoteCompanions){throw 'Native party placement not observed.'}
    foreach($id in $o.before.partyCharacters){if($id-cnotin$o.field.partyCharacters){throw 'Native selector removed an original party character.'}}
    foreach($id in @('b6628a77-4962-47a4-a17c-88d9836fc9d5','d17c8fd0-6627-40c4-bf9a-024cf001c1f7')){if($id-cnotin$o.before.partyCharacters){throw 'Qualified original party actor missing.'}}
    $ui=$o.selector
    if($ui.action-cne'native-ShowPartySelection'-or$ui.actorId-cne$s.actorId){throw 'Native selector identity differs.'}
    Assert-KmcOrdinaryInteger $ui.nativeCap 6 6
    foreach($key in @('openCount','switchCount','goCount')){Assert-KmcOrdinaryInteger $ui.$key 1 1}
    foreach($flag in @('openActiveSelf','readyActiveInHierarchy','closedExactInstance')){Assert-KmcChunk7Boolean $ui.$flag $true}
    foreach($flag in @('closedActiveSelf','manualPartyMutation')){Assert-KmcChunk7Boolean $ui.$flag $false}
    if(-not(Test-KmcChunk7FiniteNumber $ui.readyAlpha)-or$ui.readyAlpha-ne1){throw 'Native selector was not ready before ordinary input.'}
    $b=$o.bodyObservation;$pins=$source.bodySelection
    foreach($flag in @('NativeRaceAndPresetIdentityExact','NativeSkeletonIsExactPreset','NativeRigAvailable')){Assert-KmcChunk7Boolean $b.$flag $true}
    foreach($flag in @('RiderPolymorphed','MountPolymorphed')){Assert-KmcChunk7Boolean $b.$flag $false}
    Assert-KmcOrdinaryInteger $b.OriginalSizeOrdinal 3 3;Assert-KmcOrdinaryInteger $b.EffectiveSizeOrdinal 3 3
    if($b.RaceGuid-cne$pins.smallRaceGuid-or$b.PresetGuid-cne$pins.smallPresetGuid-or$b.SkeletonName-cne$pins.skeletonName-or$b.Gender-cne$pins.gender){throw 'Observed field body differs from its native source selection.'}
    Assert-KmcChunk7Boolean $b.LeftHanded $pins.leftHanded
    if($o.profileInventory.captureStatus-cne'PASS'-or$o.profileInventory.gameplayAssemblyMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'){throw 'Exact native profile observation missing.'}
    Assert-KmcOrdinaryInteger $o.profileInventory.schemaVersion 1 1
    Assert-KmcChunk7Boolean $o.profileInventory.profileAdmissionChanged $false
    if($o.profileInventory.party-isnot[Array]-or$o.profileInventory.party.Count-ne2){throw 'Field rig inventory must bind the exact rider and pet.'}
    Assert-KmcChunk7ExactIds $o.profileInventory.party @($s.actorId,$s.petId)
    $nativeRider=@($o.profileInventory.party|Where-Object id -CEQ $s.actorId)[0]
    Assert-KmcChunk7Boolean $nativeRider.character.present $true
    foreach($flag in @('custodySettled','nativeWorkerSettled','selectorClosed','selectionRestored','pauseRestored','acquiredNativeStateRetained','sourceUnchanged')){Assert-KmcChunk7Boolean $o.cleanup.$flag $true}
    foreach($key in @('nativeWrites','manualPartyMutations','manualRefunds','manualFactRemovals','manualCompanionRemovals')){Assert-KmcOrdinaryInteger $o.cleanup.$key 0 0}
    Assert-KmcChunk7SmallMountedEvidence $Request $o

}

function Assert-KmcChunk7SmallArtifact($Request,$Manifest,[string]$Status='PASS'){
 $records=@($Manifest.artifacts|Where-Object{$_.kind-ceq'chunk7-small-calibration-evidence'-or$_.relativePath-ceq'chunk7-small-calibration-evidence.json'})
 if(-not(Test-KmcChunk7SmallScenario $Request.scenario)){if($records.Count){throw 'Small calibration artifact under another scenario.'};return}
 if($records.Count-eq0-and$Status-ceq'FAIL'){return}
 $artifact=Read-KmcChunk7ManifestArtifact $Request $Manifest 'chunk7-small-calibration-evidence.json' 'chunk7-small-calibration-evidence'
 Assert-KmcChunk7SmallEvidence $Request $artifact $Status
}

# The new case is a distinct field-durable source. No legacy Working alias is admitted.
function Test-KmcChunk7SmallAssistedCase($Selection){$null-ne$Selection-and$Selection.case-is[string]-and$Selection.case-ceq'compiled-assisted-two-owner-small-horse-rt'}
function Test-KmcChunk7SmallF09Case($Selection){$null-ne$Selection-and$Selection.case-is[string]-and$Selection.case-cin@('compiled-owner-seed-fixture-small-horse-rt','compiled-owner-seed-fixture-small-horse-presentation-rt')}
function Test-KmcChunk7SmallPresentationCase($Selection){$null-ne$Selection-and$Selection.case-is[string]-and$Selection.case-ceq'compiled-owner-seed-fixture-small-horse-presentation-rt'}
function Test-KmcChunk7SmallManualCase($Selection){$null-ne$Selection-and$Selection.case-is[string]-and$Selection.case-cin@('compiled-manual-derived-small-horse-rt','compiled-assisted-two-owner-small-horse-rt')}
function Get-KmcChunk7SmallSelectedSource($Selection){if(Test-KmcChunk7SmallF09Case $Selection){$Selection.fixtureSource}elseif(Test-KmcChunk7SmallManualCase $Selection){$Selection.manualSource}else{$Selection.source}}
function Assert-KmcChunk7SmallF09SourceSelection($S,[string]$RunId){
 Assert-KmcExactProperties $S @('sourceRunId','sourceArtifactSha256','sourceArchiveSha256','coldReceiptSha256','actorId','petId') 'F09 Small Manual source'
 if($S.sourceRunId-isnot[string]-or$S.sourceRunId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'-or$S.sourceRunId-ceq$RunId){throw 'F09 Small source requires a distinct completed producer.'}
 foreach($key in @('sourceArtifactSha256','sourceArchiveSha256','coldReceiptSha256')){if($S.$key-isnot[string]-or$S.$key-cnotmatch'^[a-f0-9]{64}$'){throw 'F09 Small source lacks an exact immutable hash.'}}
 Assert-KmcOrdinaryGuid $S.actorId;Assert-KmcOrdinaryGuid $S.petId
 if($S.actorId-ceq$S.petId){throw 'F09 Small owner and Horse alias.'}
}
function Assert-KmcChunk7SmallF09Descriptor($Request){
 $s=$Request.smallCalibration.fixtureSource;Assert-KmcChunk7SmallF09SourceSelection $s $Request.runId
 $d=$Request.persistenceLoad;Assert-KmcExactProperties $d @('internalName','fileName','sha256','length','lastWriteTimeUtcTicks','gameId','gameName','area') 'F09 immutable Manual descriptor'
 Assert-KmcBootstrapInventoryString $d.internalName 'KMC_C7_SMALL_FIXTURE';Assert-KmcBootstrapInventoryString $d.sha256 $s.sourceArchiveSha256
 if($d.fileName-isnot[string]-or$d.fileName-cnotmatch'^Manual_([1-9][0-9]*)_KMC_C7_SMALL_FIXTURE\.zks$'){throw 'F09 source must retain its genuine Manual leaf.'}
 $number=0;if(-not[int]::TryParse($Matches[1],[Globalization.NumberStyles]::None,[Globalization.CultureInfo]::InvariantCulture,[ref]$number)-or$number-le0-or$d.fileName-cne('Manual_'+$number.ToString([Globalization.CultureInfo]::InvariantCulture)+'_KMC_C7_SMALL_FIXTURE.zks')){throw 'F09 native Manual number is malformed.'}
 Assert-KmcOrdinaryGuid $d.gameId
 if($d.gameName-isnot[string]-or[string]::IsNullOrWhiteSpace($d.gameName)-or$d.gameName.Length-gt256-or$d.gameName-match'[\x00-\x1f\x7f]'-or$d.area-isnot[string]-or$d.area-cnotmatch'^[a-f0-9]{32}$'-or$d.gameId-ceq$Request.fixture.working.gameId-or$d.sha256-ceq$Request.fixture.working.sha256-or$d.sha256-ceq$Request.fixture.baseline.sha256){throw 'F09 must retain its separately minted owner-seed campaign.'}
 Assert-KmcOrdinaryInteger $d.length 1 268435456;Assert-KmcOrdinaryInteger $d.lastWriteTimeUtcTicks 1 ([DateTime]::MaxValue.Ticks)
}
function Assert-KmcChunk7SmallManualBodyInventory($O,$S,$Source){
 $b=$O.bodyObservation;$pins=$Source.bodySelection
 foreach($flag in @('NativeRaceAndPresetIdentityExact','NativeSkeletonIsExactPreset','NativeRigAvailable')){Assert-KmcChunk7Boolean $b.$flag $true}
 foreach($flag in @('RiderPolymorphed','MountPolymorphed')){Assert-KmcChunk7Boolean $b.$flag $false}
 Assert-KmcOrdinaryInteger $b.OriginalSizeOrdinal 3 3;Assert-KmcOrdinaryInteger $b.EffectiveSizeOrdinal 3 3
 foreach($pair in @(@('RaceGuid','smallRaceGuid'),@('PresetGuid','smallPresetGuid'),@('SkeletonName','skeletonName'),@('Gender','gender'),@('LeftHanded','leftHanded'))){if((ConvertTo-KmcChunk7Canonical $b.($pair[0]))-cne(ConvertTo-KmcChunk7Canonical $pins.($pair[1]))){throw 'F09 observed Small body differs from its exact native source.'}}
 if($O.profileInventory.captureStatus-isnot[string]-or$O.profileInventory.gameplayAssemblyMvid-isnot[string]-or$O.profileInventory.captureStatus-cne'PASS'-or$O.profileInventory.gameplayAssemblyMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'){throw 'F09 native Small rig inventory is missing.'}
 Assert-KmcOrdinaryInteger $O.profileInventory.schemaVersion 1 1
 Assert-KmcChunk7Boolean $O.profileInventory.profileAdmissionChanged $false
 if($O.profileInventory.party-isnot[Array]-or$O.profileInventory.party.Count-ne2){throw 'F09 rig inventory must bind rider and Horse.'}
 foreach($actor in $O.profileInventory.party){Assert-KmcOrdinaryGuid $actor.id}
 Assert-KmcChunk7ExactIds $O.profileInventory.party @($S.actorId,$S.petId)
 $rider=@($O.profileInventory.party|Where-Object id -CEQ $S.actorId)
 if($rider.Count-ne1){throw 'F09 native Small rider inventory is absent.'}
 Assert-KmcChunk7Boolean $rider[0].character.present $true
}
function Assert-KmcChunk7SmallPresentationEvidence($Request,$O){
 $p=$O.presentation;$s=$Request.smallCalibration.fixtureSource
 Assert-KmcExactProperties $p @('contract','fixtureSetupOnly','spellSlotWrites','nativePreparationManufactured','inputs','originalSlots','items','castTrace','released') 'Small native item presentation'
 Assert-KmcBootstrapInventoryString $p.contract 'small-native-item-presentation-v1'
 foreach($key in @('fixtureSetupOnly','released')){Assert-KmcChunk7Boolean $p.$key $true}
 Assert-KmcChunk7Boolean $p.nativePreparationManufactured $false;Assert-KmcOrdinaryInteger $p.spellSlotWrites 0 0
 if($p.inputs-isnot[Array]-or$p.inputs.Count-ne2-or$p.items-isnot[Array]-or$p.items.Count-ne2){throw 'Small presentation requires exactly one scroll and one potion native input.'}
 if($p.originalSlots.restored-isnot[bool]-or-not$p.originalSlots.restored-or$p.originalSlots.before-isnot[Array]-or$p.originalSlots.after-isnot[Array]-or$p.originalSlots.before.Count-lt5-or
  (ConvertTo-KmcChunk7Canonical $p.originalSlots.before)-cne(ConvertTo-KmcChunk7Canonical $p.originalSlots.after)){throw 'Exact original Small quick-slot owner/item placement was not restored.'}
 foreach($item in $p.items){
  foreach($key in @('ownedItemReleased','disposed','slotRestored','noOwnedItemResident','slotOriginallyEmpty')){Assert-KmcChunk7Boolean $item.$key $true}
  if((ConvertTo-KmcChunk7Canonical $item.quickSlotsBefore)-cne(ConvertTo-KmcChunk7Canonical $p.originalSlots.before)-or
   (ConvertTo-KmcChunk7Canonical $item.quickSlotsAfter)-cne(ConvertTo-KmcChunk7Canonical $p.originalSlots.after)){throw 'Small item lease does not retain exact original quick-slot snapshots.'}
 }
 foreach($pair in @(@('scroll-friendly','cd635d5720937b044a354dba17abad8d',$s.petId),@('potion-self','d52566ae8cbe8dc4dae977ef51c27d91',$s.actorId))){
  $i=[array]::IndexOf(@('scroll-friendly','potion-self'),$pair[0]);$row=$p.inputs[$i]
  foreach($key in @('clicked','terminal','processTerminal')){Assert-KmcChunk7Boolean $row.$key $true}
  foreach($key in @('woundBefore','woundAtInput','woundAfter')){Assert-KmcOrdinaryInteger $row.$key 0 ([int]::MaxValue)}
  if($row.woundAtInput-le0-or$row.woundAfter-ge$row.woundAtInput-or$row.nativeResult-cne'Success'-or$row.kind-cne$pair[0]-or$row.actorId-cne$s.actorId-or$row.targetId-cne$pair[2]-or$row.itemBlueprint-cne$pair[1]-or$row.selectionAfterInput-cne$O.manualNativeRoster.mainActorId){throw 'Native Small item input role, healing or terminal result differs.'}
  if($row.abilityGuid-isnot[string]-or$row.abilityGuid-cnotmatch'^[a-f0-9]{32}$'){throw 'Native Small item ability is missing.'}
  Assert-KmcOrdinaryInteger $row.shell ([int]::MinValue) ([int]::MaxValue);if($row.shell-eq0){throw 'Native Small item shell identity is absent.'}
  Assert-KmcOrdinaryInteger $row.beforeItem.count 2 2;Assert-KmcOrdinaryInteger $row.afterItem.count 1 1
  if($row.beforeItem.item-ne$row.afterItem.item-or$row.beforeItem.blueprint-cne$pair[1]-or$row.afterItem.blueprint-cne$pair[1]-or$row.beforeItem.exactSlot-isnot[bool]-or-not$row.beforeItem.exactSlot-or$row.afterItem.exactSlot-isnot[bool]-or-not$row.afterItem.exactSlot){throw 'Exact native item identity, source or in-slot one-unit spend differs.'}
  $lease=$p.items[$i]
  if($lease.blueprint-cne$pair[1]-or$lease.rider-cne$s.actorId-or$lease.requestedCount-ne2-or$lease.slot-ne$row.beforeItem.holdingSlot-or
   $lease.stacked.item-ne$row.beforeItem.item-or$lease.stacked.count-ne2-or$lease.beforeCleanup.item-ne$row.beforeItem.item-or
   $lease.beforeCleanup.count-ne1-or$lease.removalPostconditions.item-ne$row.beforeItem.item-or
   $lease.removalPostconditions.collection-ne0-or$lease.removalPostconditions.holdingSlot-ne0-or
   $lease.removalPostconditions.exactInventoryResident-ne$false-or$lease.afterCleanup.item-ne$row.beforeItem.item-or
   $lease.afterCleanup.exactInventoryResident-ne$false){throw 'Small leased item cleanup is not bound to its exact acquired identity.'}
  if($row.beforePose.profileId-cne$Request.smallCalibration.profileId-or$row.afterPose.profileId-cne$Request.smallCalibration.profileId-or$row.beforePose.riderId-cne$s.actorId-or$row.afterPose.riderId-cne$s.actorId-or$row.beforePose.mountId-cne$s.petId-or$row.afterPose.mountId-cne$s.petId){throw 'Small item input lost its compiled mounted pose.'}
  foreach($pose in @($row.beforePose,$row.afterPose)){
   Assert-KmcChunk7Boolean $pose.runtime.configured $true;Assert-KmcChunk7Boolean $pose.runtime.healthy $true
   Assert-KmcOrdinaryInteger $pose.runtime.componentCount 1 1
   if($pose.qualificationStatus-cne'IN PROGRESS'-or$pose.runtime.poseProfileId-cne$Request.smallCalibration.profileId-or$pose.riderRoot.instanceId-ne$O.poses[0].observation.riderRoot.instanceId-or$pose.horseRoot.instanceId-ne$O.poses[0].observation.horseRoot.instanceId){throw 'Small item pose lost the compiled rig or runtime ownership.'}
  }
  Assert-KmcOrdinaryInteger $row.beforePose.frame $O.poses[5].observation.frame $O.inputs[3].before.frame
  Assert-KmcOrdinaryInteger $row.afterPose.frame $row.beforePose.frame $O.inputs[3].before.frame
  if($i-eq1-and$row.beforePose.frame-lt$p.inputs[0].afterPose.frame){throw 'Potion input preceded the exact scroll terminal.'}
  if($null-eq$row.PSObject.Properties['activePose']-or$row.activePose.profileId-cne$Request.smallCalibration.profileId-or$row.activePose.riderId-cne$s.actorId-or$row.activePose.mountId-cne$s.petId-or$row.activePose.frame-lt$row.beforePose.frame-or$row.activePose.frame-gt$row.afterPose.frame){throw 'Active Small item pose differs or was not observed.'}
  foreach($key in @('configured','healthy')){Assert-KmcChunk7Boolean $row.activePose.runtime.$key $true}
  Assert-KmcOrdinaryInteger $row.activePose.runtime.componentCount 1 1
  if($row.activePose.qualificationStatus-cne'IN PROGRESS'-or$row.activePose.runtime.poseProfileId-cne$Request.smallCalibration.profileId-or
   $row.activePose.riderRoot.instanceId-ne$O.poses[0].observation.riderRoot.instanceId-or
   $row.activePose.horseRoot.instanceId-ne$O.poses[0].observation.horseRoot.instanceId){throw 'Active Small item pose lost the compiled rig or runtime ownership.'}
  if($row.nativeEvents-isnot[Array]-or$row.nativeEvents.Count-eq0-or$row.nativeEvents.Count-gt2048-or$row.nativeCosts-isnot[Array]-or$row.nativeCosts.Count-eq0-or$row.nativeCosts.Count-gt16000){throw 'Bounded native Small item/cost event slices are missing.'}
  Assert-KmcOrdinaryInteger $row.eventOffset 0 2048;Assert-KmcOrdinaryInteger $row.costOffset 0 16000
  if($row.eventOffset+$row.nativeEvents.Count-gt$p.castTrace.events.Count-or
   (ConvertTo-KmcChunk7Canonical @($p.castTrace.events|Select-Object -Skip $row.eventOffset -First $row.nativeEvents.Count))-cne(ConvertTo-KmcChunk7Canonical $row.nativeEvents)-or
   $row.costOffset+$row.nativeCosts.Count-gt$O.allocationTrace.events.Count-or
   (ConvertTo-KmcChunk7Canonical @($O.allocationTrace.events|Select-Object -Skip $row.costOffset -First $row.nativeCosts.Count))-cne(ConvertTo-KmcChunk7Canonical $row.nativeCosts)){throw 'Small item event/cost evidence is not its exact raw observer slice.'}
  $spends=@($row.nativeEvents|Where-Object{$_.kind-cin@('item-spend-before','item-spend-after')})
  $beforeSpend=@($spends|Where-Object kind -CEQ 'item-spend-before');$afterSpend=@($spends|Where-Object kind -CEQ 'item-spend-after')
  if($spends.Count-ne2-or$beforeSpend.Count-ne1-or$afterSpend.Count-ne1-or$beforeSpend[0].identity-ne$row.beforeItem.item-or
   $afterSpend[0].identity-ne$row.afterItem.item-or$beforeSpend[0].actor-cne$s.actorId-or$afterSpend[0].actor-cne$s.actorId-or
   $beforeSpend[0].blueprint-cne$pair[1]-or$afterSpend[0].blueprint-cne$pair[1]-or
   $beforeSpend[0].count-ne2-or$afterSpend[0].count-ne1-or$afterSpend[0].result-isnot[bool]-or-not$afterSpend[0].result){throw 'Small item input lacks one exact leased-item native spend.'}
  $pairCosts=@($row.nativeCosts|Where-Object{$_.boundary-cin@('cost-before','cost-after')-and$_.state.actor-cin@($s.actorId,$s.petId)})
  $beforeCost=@($pairCosts|Where-Object boundary -CEQ 'cost-before');$afterCost=@($pairCosts|Where-Object boundary -CEQ 'cost-after')
  if($pairCosts.Count-ne2-or$beforeCost.Count-ne1-or$afterCost.Count-ne1){throw 'Small item input lacks exactly one pair-owned native action cost.'}
  $b=$beforeCost[0];$a=$afterCost[0]
  foreach($event in @($b,$a)){
   Assert-KmcChunk7Boolean $event.acted $true;Assert-KmcChunk7Boolean $event.nativeTurnBased $false
   if($event.command-ne$row.shell-or$event.commandActor-cne$s.actorId-or$event.state.actor-cne$s.actorId-or
    $event.commandType-cne'Kingmaker.UnitLogic.Commands.UnitUseAbility'-or$event.actionType-cnotin@('Standard','Move','Swift')){throw 'Small item cost command, actor or native action type differs.'}
   foreach($key in @('standard','move','swift','reactionCooldown')){Assert-KmcChunk7SmallNumber $event.state.$key};Assert-KmcOrdinaryInteger $event.state.reactions 0
  }
  if($a.sequence-le$b.sequence-or$a.actionType-cne$b.actionType){throw 'Small item native action cost order/type differs.'}
  $resource=$a.actionType.ToLowerInvariant()
  if($a.state.$resource-le$b.state.$resource){throw 'Native Small item action did not consume its selected resource.'}
  foreach($key in @('standard','move','swift','reactionCooldown','reactions')){if($key-cne$resource-and
   (ConvertTo-KmcChunk7Canonical $a.state.$key)-cne(ConvertTo-KmcChunk7Canonical $b.state.$key)){throw 'Native Small item cost changed another resource.'}}
 }
 foreach($key in @('dropped','faults')){Assert-KmcOrdinaryInteger $p.castTrace.$key 0 0}
 if($p.castTrace.events-isnot[Array]-or$p.castTrace.events.Count-eq0-or$p.castTrace.events.Count-gt2048){throw 'Installed native Small item observer has no bounded events.'}
}
function Assert-KmcChunk7SmallF09Evidence($Request,$Artifact){
 $o=$Artifact.observations;$s=$Request.smallCalibration.fixtureSource;$source=$o.source;$saved=$source.fixtureManual
 if((ConvertTo-KmcChunk7Canonical $o.selection)-cne(ConvertTo-KmcChunk7Canonical $Request.smallCalibration)){throw 'F09 observed selection differs.'}
 Assert-KmcOrdinaryInteger $o.processId 1 ([int]::MaxValue)
 if(-not(Test-KmcChunk7FiniteNumber $o.elapsedSeconds)-or$o.elapsedSeconds-lt0-or$o.elapsedSeconds-gt420){throw 'F09 calibration elapsed time invalid.'}
 foreach($flag in @('sourceWasRemoteAutosave','sourceArchiveStillRemote','smallPoseQualified','mountedCombatProven','freshMainCampaignProven','manualPartyMutation')){Assert-KmcChunk7Boolean $o.$flag $false}
 Assert-KmcBootstrapInventoryString $source.kind 'qualified-owner-seed-f09-small-fixture-manual'
 foreach($pair in @(@('runId',$s.sourceRunId),@('artifactSha256',$s.sourceArtifactSha256),@('archiveSha256',$s.sourceArchiveSha256),@('coldReceiptSha256',$s.coldReceiptSha256),@('archiveLeaf',$Request.persistenceLoad.fileName),@('archiveName',$Request.persistenceLoad.internalName))){Assert-KmcBootstrapInventoryString $source.($pair[0]) $pair[1]}
 Assert-KmcOrdinaryInteger $source.archiveLength $Request.persistenceLoad.length $Request.persistenceLoad.length
 Assert-KmcChunk7Boolean $source.sameCandidate $false;Assert-KmcChunk7Boolean $source.fixtureSetupOnly $true;Assert-KmcChunk7Boolean $source.unchangedArchive $true
 foreach($pair in @(@('producerCommit','784a6296f9e0d91c44ad25dda826db57f609e929'),@('producerDllSha256','8f7f89769c82e0ac15f5d972441d29e68d6f67ff4ad942fe55f25ece12047d89'),@('producerDllMvid','bf98be50-898a-41b3-88f0-78959a0ea1a3'))){Assert-KmcBootstrapInventoryString $source.($pair[0]) $pair[1]}
 Assert-KmcBootstrapInventoryString $saved.contract 'owner-seed-unmounted-small-fixture-v1'
 $f=$saved
 foreach($key in @('sha256','fileName','internalName','gameId','gameName','area')){$expected=if($key-ceq'sha256'){$s.sourceArchiveSha256}else{$Request.persistenceLoad.$key};Assert-KmcBootstrapInventoryString $f.nativeWrite.$key $expected}
 Assert-KmcBootstrapInventoryString $f.nativeWrite.type 'Manual';Assert-KmcChunk7Boolean $f.nativeWrite.autoLevelup $false
 foreach($key in @('custodySettled','sourceUnchanged')){Assert-KmcChunk7Boolean $f.cleanup.$key $true}
 if((ConvertTo-KmcChunk7Canonical $f.beforeSaveArrangement)-cne(ConvertTo-KmcChunk7Canonical $f.afterSaveArrangement)-or(ConvertTo-KmcChunk7Canonical $f.bodyObservation)-cne(ConvertTo-KmcChunk7Canonical $f.savedBodyObservation)){throw 'F09 native save changed its field arrangement or body.'}
 if($f.acquiredOwners-isnot[Array]-or$f.acquiredOwners.Count-ne2-or$f.savedWorld.party-isnot[Array]-or$f.savedWorld.party.Count-ne7-or$f.afterSaveArrangement.characters.Count-ne5-or$f.afterSaveArrangement.remote.Count-ne0){throw 'F09 saved pair or seven-actor roster differs.'}
 if($f.acquiredOwners[0].riderId-cne$f.savedWorld.mainActorId-or$f.acquiredOwners[1].riderId-cne$s.actorId-or$f.acquiredOwners[1].horseId-cne$s.petId){throw 'F09 saved pair roles differ.'}
 $root=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($Request.evidenceRoot))) ('runtime-staging\persistence-'+$Request.runId+'\Saved Games')
 Assert-KmcChunk7FieldAuthority $o.authority $root $o.authority.generation;Assert-KmcChunk7FieldAuthority $o.cleanup.authority $root $o.authority.generation
 foreach($row in @($o.before,$o.field)){Assert-KmcChunk7FieldSnapshot $row $s;foreach($key in @('riderInGame','petInGame','riderInState','petInState','riderAgentReady','petAgentReady')){Assert-KmcChunk7Boolean $row.$key $true}}
 if((ConvertTo-KmcChunk7Canonical $o.before)-cne(ConvertTo-KmcChunk7Canonical $o.field)){throw 'F09 field load unexpectedly used a party selector.'}
 foreach($pair in @(@('money',$f.afterSaveArrangement.money),@('xp',@($f.afterSaveArrangement.actors|Where-Object id -CEQ $s.actorId)[0].experience),@('level',@($f.afterSaveArrangement.actors|Where-Object id -CEQ $s.actorId)[0].level),@('petLevel',@($f.afterSaveArrangement.actors|Where-Object id -CEQ $s.petId)[0].level),@('rank',$f.acquiredOwners[1].rank))){Assert-KmcOrdinaryInteger $o.before.($pair[0]) $pair[1] $pair[1]}
 Assert-KmcBootstrapInventoryString $o.before.featureSourceGuid $f.acquiredOwners[1].featureSourceGuid
 if((ConvertTo-KmcChunk7Canonical $o.before.partyCharacters)-cne(ConvertTo-KmcChunk7Canonical $f.afterSaveArrangement.characters)-or$o.before.remoteCompanions.Count-ne0){throw 'F09 cold character membership differs.'}
 Assert-KmcExactProperties $o.selector @('required','openCount','manualPartyMutation','fieldRestoredFromManual') 'F09 selector refusal'
 foreach($key in @('required','manualPartyMutation')){Assert-KmcChunk7Boolean $o.selector.$key $false};Assert-KmcOrdinaryInteger $o.selector.openCount 0 0;Assert-KmcChunk7Boolean $o.selector.fieldRestoredFromManual $true
 Assert-KmcOrdinaryInteger $o.beforeLoad.frame 0 ([int]::MaxValue);Assert-KmcChunk7Boolean $o.beforeLoad.installedBeforeWorldLoad $true;Assert-KmcChunk7Boolean $o.beforeLoad.sourceReadOnly $true
 foreach($key in @('nativeLoads','nativeWrites')){Assert-KmcOrdinaryInteger $o.beforeLoad.$key 0 0}
 Assert-KmcAssistedColdNoPreparation $o.coldNoPreparation $Request
 $n=$o.manualNativeRoster;Assert-KmcExactProperties $n @('mainActorId','arrangement','resources','actors') 'F09 native full roster'
 Assert-KmcBootstrapInventoryString $n.mainActorId $f.savedWorld.mainActorId
 if($n.actors-isnot[Array]-or$n.actors.Count-ne7){throw 'F09 cold roster must contain seven actors.'}
 Assert-KmcChunk7ExactIds $n.actors @($f.savedWorld.party.id)
 $labels=[Collections.Generic.HashSet[int]]::new()
 foreach($a in $n.actors){Assert-KmcOrdinaryGuid $a.id;$sourceActor=@($f.savedWorld.party|Where-Object id -CEQ $a.id);if($sourceActor.Count-ne1){throw 'F09 cold actor is foreign.'};Assert-KmcBootstrapInventoryString $a.blueprint $sourceActor[0].blueprint;foreach($key in @('object','descriptor','view')){Assert-KmcAcquiredManualObjectIdentity $a.$key;if(-not$labels.Add([int]$a.$key)){throw 'F09 native actor object roles alias.'}};$state=@($o.inputs[0].before.actors|Where-Object id -CEQ $a.id);if($state.Count-ne1){throw 'F09 action trace lacks a source actor.'};Assert-KmcOrdinaryInteger $state[0].object $a.object $a.object}
 foreach($role in @(@('actor',$s.actorId),@('pet',$s.petId))){$a=@($n.actors|Where-Object id -CEQ $role[1]);if($a.Count-ne1){throw 'F09 Small pair role missing.'};Assert-KmcOrdinaryInteger $o.before.($role[0]+'Object') $a[0].object $a[0].object;Assert-KmcOrdinaryInteger $o.before.($(if($role[0]-ceq'actor'){'descriptorObject'}else{'petDescriptorObject'})) $a[0].descriptor $a[0].descriptor}
 if($n.arrangement.actors-isnot[Array]-or$n.arrangement.actors.Count-ne7){throw 'F09 cold progression roster differs.'};Assert-KmcChunk7ExactIds $n.arrangement.actors @($f.afterSaveArrangement.actors.id) 'actorId'
 Assert-KmcOrdinaryInteger $n.arrangement.experienceRatePercent $f.afterSaveArrangement.experienceRatePercent $f.afterSaveArrangement.experienceRatePercent
 foreach($a in $n.arrangement.actors){Assert-KmcOrdinaryGuid $a.actorId;$sourceActor=@($f.afterSaveArrangement.actors|Where-Object id -CEQ $a.actorId);if($sourceActor.Count-ne1){throw 'F09 cold progression actor is foreign.'};foreach($pair in @(@('blueprint','blueprint'),@('experience','experience'),@('characterLevel','level'))){if((ConvertTo-KmcChunk7Canonical $a.($pair[0]))-cne(ConvertTo-KmcChunk7Canonical $sourceActor[0].($pair[1]))){throw 'F09 cold native progression changed.'}};if($a.classes-isnot[Array]-or$a.classes.Count-ne$sourceActor[0].classes.Count){throw 'F09 cold classes differ.'};$classes=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);foreach($c in $a.classes){$sourceClass=@($sourceActor[0].classes|Where-Object guid -CEQ $c.classGuid);if($sourceClass.Count-ne1){throw 'F09 cold native class is foreign.'};Assert-KmcBootstrapInventoryString $c.classGuid $sourceClass[0].guid;if(-not$classes.Add($c.classGuid)){throw 'F09 cold native class repeats.'};Assert-KmcOrdinaryInteger $c.level $sourceClass[0].level $sourceClass[0].level}}
 Assert-KmcOrdinaryInteger $n.arrangement.money $f.afterSaveArrangement.money $f.afterSaveArrangement.money
 if($n.resources.actors-isnot[Array]-or$n.resources.actors.Count-ne7){throw 'F09 cold resource roster differs.'};Assert-KmcChunk7ExactIds $n.resources.actors @($f.savedWorld.party.id) 'actorId'
 foreach($a in $n.resources.actors){Assert-KmcOrdinaryGuid $a.actorId;$sourceActor=@($f.savedWorld.party|Where-Object id -CEQ $a.actorId);if($sourceActor.Count-ne1){throw 'F09 resource actor is foreign.'};$native=$sourceActor[0].native;foreach($pair in @(@('standard','Standard'),@('move','Move'),@('swift','Swift'),@('initiative','Initiative'),@('reactions','ReactionsRemaining'))){$observed=$a.($pair[0]);$saved=$native.($pair[1]);if(-not(Test-KmcFiniteJsonNumber $observed)-or-not(Test-KmcFiniteJsonNumber $saved)){throw 'F09 cold native resources changed.'};$observedCanonical=ConvertTo-KmcChunk7Canonical $observed;$savedCanonical=ConvertTo-KmcChunk7Canonical $saved;if($observedCanonical-cne$savedCanonical-and-not($observedCanonical-cmatch'^-?0(?:\.0+)?$'-and$savedCanonical-cmatch'^-?0(?:\.0+)?$')){throw 'F09 cold native resources changed.'}};Assert-KmcOrdinaryInteger $a.reactionAllowance 1;Assert-KmcChunk7Boolean $a.prepared $false}
 if(@($o.bodyObservation.PSObject.Properties).Count-ne20-or@($f.savedBodyObservation.PSObject.Properties).Count-ne20){throw 'F09 cold Small body field count changed.'};foreach($p in $f.savedBodyObservation.PSObject.Properties){if((ConvertTo-KmcChunk7Canonical $o.bodyObservation.($p.Name))-cne(ConvertTo-KmcChunk7Canonical $p.Value)){throw 'F09 cold Small body changed.'}}
 Assert-KmcChunk7SmallManualBodyInventory $o $s $source
 $prior=$f.acquiredOwners[0]
 $pairKeys=@('actorId','petId','actorObject','petObject','actorDescriptor','petDescriptor','actorView','petView','featureObject','sourceObject','rankObject','actorBlueprint','petBlueprint','featureGuid','sourceGuid','rank','reciprocal','resources')
 foreach($row in @($o.inheritedFixturePairBefore,$o.inheritedFixturePairAfter)){Assert-KmcAssistedObjectValue $row $pairKeys 'F09 inherited native pair';foreach($pair in @(@('actorId','riderId'),@('petId','horseId'),@('petBlueprint','horseBlueprint'),@('featureGuid','featureGuid'),@('sourceGuid','featureSourceGuid'),@('rank','rank'))){if((ConvertTo-KmcChunk7Canonical $row.($pair[0]))-cne(ConvertTo-KmcChunk7Canonical $prior.($pair[1]))){throw 'F09 neighboring native pair changed.'}};Assert-KmcChunk7Boolean $row.reciprocal $true}
 $admission=$o.inheritedFixturePairNativeCombatAdmission
 Assert-KmcExactProperties $admission @('stage','frame','gameTicks','targetId','targetInCombat','ownerInCombat','horseInCombat','ownerCommandsEmpty','horseCommandsEmpty','before','after') 'F09 neighboring native combat admission'
 Assert-KmcOrdinaryInteger $admission.stage 3 3
 $primary=@($o.inputs|Where-Object kind -CEQ 'rider-primary')
 if($primary.Count-ne1){throw 'F09 native combat entry lacks its first native primary window.'}
 Assert-KmcOrdinaryInteger $admission.frame ($o.movement.after.frame+1) $primary[0].before.frame
 Assert-KmcOrdinaryInteger $admission.gameTicks $o.movement.after.gameTicks $primary[0].before.gameTicks
 Assert-KmcBootstrapInventoryString $admission.targetId $o.targetId
 foreach($flag in @('targetInCombat','ownerInCombat','horseInCombat','ownerCommandsEmpty','horseCommandsEmpty')){Assert-KmcChunk7Boolean $admission.$flag $true}
 if((ConvertTo-KmcChunk7Canonical $admission.before)-cne(ConvertTo-KmcChunk7Canonical $o.inheritedFixturePairBefore)){throw 'F09 native combat admission lacks its exact cold neighbor.'}
 foreach($row in @($admission.before,$admission.after,$o.inheritedFixturePairAfter)){
  Assert-KmcAssistedObjectValue $row $pairKeys 'F09 native combat neighbor'
  foreach($key in @($pairKeys|Where-Object{$_-cne'resources'})){if((ConvertTo-KmcChunk7Canonical $row.$key)-cne(ConvertTo-KmcChunk7Canonical $o.inheritedFixturePairBefore.$key)){throw 'F09 neighboring pair identity changed during native combat.'}}
 }
 if($null-ne$o.PSObject.Properties['inheritedFixturePairDrift']){throw 'F09 native observer recorded unadmitted neighboring drift.'}
 $admittedResources=@($admission.after.resources)
 if($admittedResources.Count-ne2){throw 'F09 native combat admission must retain two neighbor resources.'}
 Assert-KmcChunk7ExactIds $admittedResources @($prior.riderId,$prior.horseId) 'actorId'
 foreach($saved in @($admission.before.resources)){
  $current=@($admittedResources|Where-Object actorId -CEQ $saved.actorId)
  if($current.Count-ne1){throw 'F09 native combat admission actor changed.'}
  Assert-KmcExactProperties $current[0] @('actorId','move','standard','swift','prepared','reactions','reactionAllowance','initiative') 'F09 native combat resource'
  Assert-KmcChunk7Boolean $saved.prepared $false;Assert-KmcOrdinaryInteger $saved.initiative 0 0
  Assert-KmcChunk7Boolean $current[0].prepared $true;Assert-KmcOrdinaryInteger $current[0].initiative 1 1000
  foreach($key in @('actorId','move','standard','swift','reactions','reactionAllowance')){if((ConvertTo-KmcChunk7Canonical $saved.$key)-cne(ConvertTo-KmcChunk7Canonical $current[0].$key)){throw 'F09 native combat admission changed a neighboring action budget.'}}
 }
 if((ConvertTo-KmcChunk7Canonical $o.inheritedFixturePairAfter.resources)-cne(ConvertTo-KmcChunk7Canonical $admission.before.resources)){throw 'F09 neighboring combat resources did not return to their cold state after target disposal.'}
 $beforePair=$o.inheritedFixturePairBefore
 $mainActor=@($f.savedWorld.party|Where-Object id -CEQ $prior.riderId);if($mainActor.Count-ne1){throw 'F09 inherited main actor missing.'};Assert-KmcBootstrapInventoryString $beforePair.actorBlueprint $mainActor[0].blueprint
 foreach($role in @('actor','pet')){$nativeActor=@($n.actors|Where-Object id -CEQ $beforePair.($role+'Id'));if($nativeActor.Count-ne1){throw 'F09 inherited actor absent from cold roster.'};foreach($pair in @(@('Object','object'),@('Descriptor','descriptor'),@('View','view'))){Assert-KmcOrdinaryInteger $beforePair.($role+$pair[0]) $nativeActor[0].($pair[1]) $nativeActor[0].($pair[1])}}
 foreach($key in @('featureObject','sourceObject','rankObject')){Assert-KmcAcquiredManualObjectIdentity $beforePair.$key;if(-not$labels.Add([int]$beforePair.$key)){throw 'F09 inherited fact identities alias native actors or each other.'}}
 Assert-KmcCumulativeResources ([pscustomobject]@{actors=$beforePair.resources}) @($prior.riderId,$prior.horseId)
 $coldPrior=@($n.resources.actors|Where-Object{$_.actorId-cin@($prior.riderId,$prior.horseId)})
 if((ConvertTo-KmcChunk7Canonical $beforePair.resources)-cne(ConvertTo-KmcChunk7Canonical $coldPrior)){throw 'F09 inherited native resources differ from cold roster.'}
 foreach($flag in @('custodySettled','nativeWorkerSettled','selectorClosed','selectionRestored','pauseRestored','acquiredNativeStateRetained','sourceUnchanged')){Assert-KmcChunk7Boolean $o.cleanup.$flag $true}
 foreach($key in @('nativeWrites','manualPartyMutations','manualRefunds','manualFactRemovals','manualCompanionRemovals')){Assert-KmcOrdinaryInteger $o.cleanup.$key 0 0}
 Assert-KmcChunk7SmallMountedEvidence $Request $o
 if(Test-KmcChunk7SmallPresentationCase $Request.smallCalibration){Assert-KmcChunk7SmallPresentationEvidence $Request $o}
 elseif($null-ne$o.PSObject.Properties['presentation']){throw 'Historical F09 Small calibration cannot claim the separate presentation case.'}
}
function Assert-KmcChunk7SmallManualSourceSelection($S,[string]$RunId){
 Assert-KmcExactProperties $S @('sourceRunId','sourceArtifactSha256','sourceArchiveSha256','actorId','petId') 'Small acquired Manual source'
 if($S.sourceRunId-isnot[string]-or$S.sourceRunId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'-or$S.sourceRunId-ceq$RunId){throw 'Small Manual source needs a distinct exact run.'}
 foreach($key in @('sourceArtifactSha256','sourceArchiveSha256')){if($S.$key-isnot[string]-or$S.$key-cnotmatch'^[a-f0-9]{64}$'){throw 'Small Manual source needs exact immutable SHA256 pins.'}}
 Assert-KmcOrdinaryGuid $S.actorId;Assert-KmcOrdinaryGuid $S.petId;if($S.actorId-ceq$S.petId){throw 'Small owner and Horse identities must differ.'}
}
function Assert-KmcChunk7SmallManualDescriptor($Request){
 $s=$Request.smallCalibration.manualSource;Assert-KmcChunk7SmallManualSourceSelection $s $Request.runId
 $d=$Request.persistenceLoad;Assert-KmcExactProperties $d @('internalName','fileName','sha256','length','lastWriteTimeUtcTicks','gameId','gameName','area') 'Small immutable Manual descriptor'
 Assert-KmcBootstrapInventoryString $d.internalName 'KMC_C7_ACQUIRED_FIELD';Assert-KmcBootstrapInventoryString $d.sha256 $s.sourceArchiveSha256
 if($d.fileName-isnot[string]-or$d.fileName-cnotmatch'^Manual_([1-9][0-9]*)_KMC_C7_ACQUIRED_FIELD\.zks$'){throw 'Small source must retain its real acquired Manual leaf.'}
 $number=0;if(-not[int]::TryParse($Matches[1],[Globalization.NumberStyles]::None,[Globalization.CultureInfo]::InvariantCulture,[ref]$number)-or$number-le0-or$d.fileName-cne('Manual_'+$number.ToString([Globalization.CultureInfo]::InvariantCulture)+'_KMC_C7_ACQUIRED_FIELD.zks')){throw 'Small source native Manual number is malformed.'}
 Assert-KmcOrdinaryGuid $d.gameId
 if($d.gameName-isnot[string]-or[string]::IsNullOrWhiteSpace($d.gameName)-or$d.gameName.Length-gt256-or$d.gameName-match'[\x00-\x1f\x7f]'-or$d.area-isnot[string]-or$d.area-cnotmatch'^[a-f0-9]{32}$'-or$d.gameId-ceq$Request.fixture.working.gameId-or$d.sha256-ceq$Request.fixture.working.sha256-or$d.sha256-ceq$Request.fixture.baseline.sha256){throw 'Small Manual source must retain its separately minted campaign.'}
 Assert-KmcOrdinaryInteger $d.length 1 268435456;Assert-KmcOrdinaryInteger $d.lastWriteTimeUtcTicks 1 ([DateTime]::MaxValue.Ticks)
}
function Assert-KmcChunk7SmallManualSourceObservation($S,$O,[switch]$Assisted){
 Assert-KmcChunk7AcquiredManualSelection ([pscustomobject]@{scenario='chunk7-acquired-manual-derivative';runId=$S.sourceRunId;acquiredManual=$O.selection})
 if($Assisted){Assert-KmcAssistedSmallManualSource $S $O}else{
 Assert-KmcBootstrapInventoryString $O.selection.case 'preserve-acquired-field';Assert-KmcBootstrapInventoryString $O.selection.actorId $S.actorId;Assert-KmcBootstrapInventoryString $O.selection.petId $S.petId
 if($null-ne$O.PSObject.Properties['twoOwners']){throw 'Small calibration does not admit a cumulative/two-owner source.'}
 $choice=$O.sourceAcquisition.selection
 Assert-KmcChunk7ManualDerivedSelection ([pscustomobject]@{scenario='chunk7-manual-derived-acquisition';runId=$O.selection.sourceRunId;manualDerived=$choice})
 Assert-KmcBootstrapInventoryString $choice.case 'paid-small-ranger-horse'
 Assert-KmcChunk7Boolean $O.sourceAcquisition.nativeWrite.saveBeforeFieldSelection $true
 foreach($key in @('actorId','petId')){Assert-KmcBootstrapInventoryString $O.sourceAcquisition.field.$key $S.$key}
 # This existing function validates one reciprocal observed graph only; it grants no request authority.
 Assert-KmcCumulativePriorSource ([pscustomobject]@{priorOwnerId=$S.actorId;priorPetId=$S.petId}) $O
 }
 Assert-KmcAcquiredManualArrangement $O.beforeSaveArrangement $O.savedWorld $O.nativeProfileInventory
 Assert-KmcAcquiredManualArrangement $O.afterSaveArrangement $O.savedWorld $O.nativeProfileInventory
 if((ConvertTo-KmcChunk7Canonical $O.beforeSaveArrangement)-cne(ConvertTo-KmcChunk7Canonical $O.afterSaveArrangement)){throw 'Small Manual changed native progression while saving.'}
 Assert-KmcOrdinaryGuid $O.savedWorld.mainActorId
 if($O.savedWorld.mainActorId-ceq$S.actorId-or$O.savedWorld.mainActorId-ceq$S.petId-or@($O.savedWorld.party|Where-Object id -CEQ $O.savedWorld.mainActorId).Count-ne1){throw 'Paid Small source cannot replace the original main actor.'}
 foreach($key in @('custodySettled','sourceUnchanged')){Assert-KmcChunk7Boolean $O.cleanup.$key $true}
 foreach($key in @('schemaVersion')){Assert-KmcOrdinaryInteger $O.archive.$key 3 3}
 foreach($key in @('pairs','participations','slots')){Assert-KmcOrdinaryInteger $O.archive.$key 0 0};Assert-KmcChunk7Boolean $O.archive.combatPresent $false
 Assert-KmcChunk7Boolean $O.archive.nativeRestorationGrantedByDiagnostic $false
 foreach($key in @('owners','slots','semanticRestores','presentationRestores','casts')){Assert-KmcOrdinaryInteger $O.savedWorld.$key 0 0}
 foreach($key in @('snapshots','completedSaves')){Assert-KmcOrdinaryInteger $O.savedWorld.$key 1 1}
 Assert-KmcBootstrapInventoryString $O.nativeWrite.sha256 $S.sourceArchiveSha256
 Assert-KmcBootstrapInventoryString $O.nativeWrite.type 'Manual';Assert-KmcBootstrapInventoryString $O.nativeWrite.internalName 'KMC_C7_ACQUIRED_FIELD';Assert-KmcChunk7Boolean $O.nativeWrite.autoLevelup $false
}
function Assert-KmcChunk7SmallManualEvidence($Request,$Artifact){
 $o=$Artifact.observations;$s=$Request.smallCalibration.manualSource
 if((ConvertTo-KmcChunk7Canonical $o.selection)-cne(ConvertTo-KmcChunk7Canonical $Request.smallCalibration)){throw 'Observed Small Manual selection differs.'}
 Assert-KmcOrdinaryInteger $o.processId 1 ([int]::MaxValue)
 if(-not(Test-KmcChunk7FiniteNumber $o.elapsedSeconds)-or$o.elapsedSeconds-lt0-or$o.elapsedSeconds-gt420){throw 'Bounded native calibration elapsed time invalid.'}
 foreach($key in @('sourceWasRemoteAutosave','sourceArchiveStillRemote','smallPoseQualified','mountedCombatProven','freshMainCampaignProven','manualPartyMutation')){Assert-KmcChunk7Boolean $o.$key $false}
 $source=$o.source;$assisted=Test-KmcChunk7SmallAssistedCase $Request.smallCalibration
 Assert-KmcBootstrapInventoryString $source.kind $(if($assisted){'qualified-assisted-two-owner-acquired-manual'}else{'qualified-first-owner-acquired-manual'})
 foreach($pair in @(@('runId',$s.sourceRunId),@('artifactSha256',$s.sourceArtifactSha256),@('archiveSha256',$s.sourceArchiveSha256),@('archiveLeaf',$Request.persistenceLoad.fileName),@('archiveName',$Request.persistenceLoad.internalName))){Assert-KmcBootstrapInventoryString $source.($pair[0]) $pair[1]}
 Assert-KmcOrdinaryInteger $source.archiveLength $Request.persistenceLoad.length $Request.persistenceLoad.length
 Assert-KmcChunk7Boolean $source.sameCandidate $true;Assert-KmcChunk7Boolean $source.unchangedArchive $true
 $saved=$source.acquiredManual;Assert-KmcChunk7SmallManualSourceObservation $s $saved -Assisted:$assisted
 foreach($key in @('gameId','gameName','area')){Assert-KmcBootstrapInventoryString $saved.nativeWrite.$key $Request.persistenceLoad.$key}
 if((ConvertTo-KmcChunk7Canonical $source.bodySelection)-cne(ConvertTo-KmcChunk7Canonical $saved.sourceAcquisition.selection)){throw 'Small body choices differ from the qualified paid source.'}
 $root=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($Request.evidenceRoot))) ('runtime-staging\persistence-'+$Request.runId+'\Saved Games')
 Assert-KmcChunk7FieldAuthority $o.authority $root $o.authority.generation;Assert-KmcChunk7FieldAuthority $o.cleanup.authority $root $o.authority.generation
 foreach($row in @($o.before,$o.field)){Assert-KmcChunk7FieldSnapshot $row $s;foreach($key in @('riderInGame','petInGame','riderInState','petInState','riderAgentReady','petAgentReady')){Assert-KmcChunk7Boolean $row.$key $true}}
 if((ConvertTo-KmcChunk7Canonical $o.before)-cne(ConvertTo-KmcChunk7Canonical $o.field)){throw 'The field-durable Small Manual cannot acquire another selector phase.'}
 foreach($key in @('partyCharacters','remoteCompanions')){if((ConvertTo-KmcChunk7Canonical $o.before.$key)-cne(ConvertTo-KmcChunk7Canonical $saved.savedOwner.$key)){throw 'Cold Small Manual character membership differs.'}}
 $ownerProgress=@($saved.afterSaveArrangement.actors|Where-Object actorId -CEQ $s.actorId);$petProgress=@($saved.afterSaveArrangement.actors|Where-Object actorId -CEQ $s.petId)
 if($ownerProgress.Count-ne1-or$petProgress.Count-ne1){throw 'Source Small progression roles missing.'}
 foreach($p in @(@('money',$saved.afterSaveArrangement.money),@('xp',$ownerProgress[0].experience),@('level',$ownerProgress[0].characterLevel),@('petLevel',$petProgress[0].characterLevel),@('rank',$saved.savedOwner.rank))){Assert-KmcOrdinaryInteger $o.before.($p[0]) $p[1] $p[1]}
 Assert-KmcBootstrapInventoryString $o.before.featureSourceGuid $saved.savedOwner.sourceGuid
 Assert-KmcExactProperties $o.selector @('required','openCount','manualPartyMutation','fieldRestoredFromManual') 'Small Manual selector refusal'
 foreach($key in @('required','manualPartyMutation')){Assert-KmcChunk7Boolean $o.selector.$key $false};Assert-KmcOrdinaryInteger $o.selector.openCount 0 0;Assert-KmcChunk7Boolean $o.selector.fieldRestoredFromManual $true
 Assert-KmcOrdinaryInteger $o.beforeLoad.frame 0 ([int]::MaxValue);Assert-KmcChunk7Boolean $o.beforeLoad.installedBeforeWorldLoad $true;Assert-KmcChunk7Boolean $o.beforeLoad.sourceReadOnly $true
 foreach($key in @('nativeLoads','nativeWrites')){Assert-KmcOrdinaryInteger $o.beforeLoad.$key 0 0}
 Assert-KmcAssistedColdNoPreparation $o.coldNoPreparation $Request
 $n=$o.manualNativeRoster;Assert-KmcExactProperties $n @('mainActorId','arrangement','resources','actors') 'Small native full roster'
 Assert-KmcBootstrapInventoryString $n.mainActorId $saved.savedWorld.mainActorId
 if($n.actors-isnot[Array]-or$n.actors.Count-ne$saved.savedWorld.party.Count-or$n.actors.Count-lt3-or$n.actors.Count-gt24){throw 'Cold Small native roster is missing or unbounded.'}
 $ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);$objects=[Collections.Generic.HashSet[int]]::new()
 foreach($a in $n.actors){Assert-KmcExactProperties $a @('id','blueprint','object','descriptor','view') 'Small native actor';Assert-KmcOrdinaryGuid $a.id;if(-not$ids.Add($a.id)){throw 'Small native actor repeats.'};$row=@($saved.savedWorld.party|Where-Object id -CEQ $a.id);if($row.Count-ne1){throw 'Small native actor is foreign.'};Assert-KmcBootstrapInventoryString $a.blueprint $row[0].blueprint
  foreach($key in @('object','descriptor','view')){Assert-KmcAcquiredManualObjectIdentity $a.$key;if(-not$objects.Add([int]$a.$key)){throw 'Small native roles alias.'}}
  $state=@($o.inputs[0].before.actors|Where-Object id -CEQ $a.id);if($state.Count-ne1){throw 'Native calibration lacks a source actor.'};Assert-KmcOrdinaryInteger $state[0].object $a.object $a.object
 }
 foreach($role in @(@('actor',$s.actorId),@('pet',$s.petId))){$a=@($n.actors|Where-Object id -CEQ $role[1]);if($a.Count-ne1){throw 'Small native pair role missing.'};Assert-KmcOrdinaryInteger $o.before.($role[0]+'Object') $a[0].object $a[0].object;Assert-KmcOrdinaryInteger $o.before.($(if($role[0]-ceq'actor'){'descriptorObject'}else{'petDescriptorObject'})) $a[0].descriptor $a[0].descriptor}
 Assert-KmcAcquiredManualArrangement $n.arrangement $saved.savedWorld $saved.nativeProfileInventory;Assert-KmcCumulativeResources $n.resources @($saved.savedWorld.party.id)
 if((ConvertTo-KmcChunk7Canonical $n.arrangement)-cne(ConvertTo-KmcChunk7Canonical $saved.afterSaveArrangement)-or(ConvertTo-KmcChunk7Canonical $n.resources)-cne(ConvertTo-KmcChunk7Canonical $saved.resourcesAfterSave)){throw 'Native cold Small load changed progression/resources.'}
 if((ConvertTo-KmcChunk7Canonical $o.bodyObservation)-cne(ConvertTo-KmcChunk7Canonical $saved.bodyObservation)){throw 'Cold Small body/rig/equipment differs from its saved field body.'}
    $b=$o.bodyObservation;$pins=$source.bodySelection
    foreach($flag in @('NativeRaceAndPresetIdentityExact','NativeSkeletonIsExactPreset','NativeRigAvailable')){Assert-KmcChunk7Boolean $b.$flag $true}
    foreach($flag in @('RiderPolymorphed','MountPolymorphed')){Assert-KmcChunk7Boolean $b.$flag $false}
    Assert-KmcOrdinaryInteger $b.OriginalSizeOrdinal 3 3;Assert-KmcOrdinaryInteger $b.EffectiveSizeOrdinal 3 3
    if($b.RaceGuid-cne$pins.smallRaceGuid-or$b.PresetGuid-cne$pins.smallPresetGuid-or$b.SkeletonName-cne$pins.skeletonName-or$b.Gender-cne$pins.gender){throw 'Observed field body differs from its native source selection.'}
    Assert-KmcChunk7Boolean $b.LeftHanded $pins.leftHanded
    if($o.profileInventory.captureStatus-isnot[string]-or$o.profileInventory.gameplayAssemblyMvid-isnot[string]-or
       $o.profileInventory.captureStatus-cne'PASS'-or$o.profileInventory.gameplayAssemblyMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'){throw 'Exact native profile observation missing.'}
    Assert-KmcOrdinaryInteger $o.profileInventory.schemaVersion 1 1
    Assert-KmcChunk7Boolean $o.profileInventory.profileAdmissionChanged $false
    if($o.profileInventory.party-isnot[Array]-or$o.profileInventory.party.Count-ne2){throw 'Field rig inventory must bind the exact rider and pet.'}
    Assert-KmcChunk7ExactIds $o.profileInventory.party @($s.actorId,$s.petId)
    $nativeRider=@($o.profileInventory.party|Where-Object id -CEQ $s.actorId)[0]
    Assert-KmcChunk7Boolean $nativeRider.character.present $true
    foreach($flag in @('custodySettled','nativeWorkerSettled','selectorClosed','selectionRestored','pauseRestored','acquiredNativeStateRetained','sourceUnchanged')){Assert-KmcChunk7Boolean $o.cleanup.$flag $true}
    foreach($key in @('nativeWrites','manualPartyMutations','manualRefunds','manualFactRemovals','manualCompanionRemovals')){Assert-KmcOrdinaryInteger $o.cleanup.$key 0 0}
    if($assisted){Assert-KmcAssistedSmallInheritedPair $o $saved}
    Assert-KmcChunk7SmallMountedEvidence $Request $o

}

function Assert-KmcAssistedSmallManualSource($S,$O){
 Assert-KmcBootstrapInventoryString $O.selection.case 'preserve-two-acquired-field'
 foreach($key in @('actorId','petId')){Assert-KmcBootstrapInventoryString $O.selection.$key $S.$key;Assert-KmcBootstrapInventoryString $O.sourceAcquisition.field.$key $S.$key}
 $choice=$O.sourceAcquisition.selection
 Assert-KmcChunk7CumulativeSelection ([pscustomobject]@{scenario='chunk7-cumulative-acquisition';runId=$O.selection.sourceRunId;cumulativeAcquisition=$choice})
 if(-not(Test-KmcAssistedCumulativeMode $choice)){throw 'Assisted Small source requires completed assisted-cold cumulative ancestry.'}
 Assert-KmcBootstrapInventoryString $choice.case 'paid-small-ranger-horse'
 $a=$O.sourceAcquisition.assistedSource;Assert-KmcBootstrapInventoryString $a.contract 'assisted-producer-cold-cumulative-v1';Assert-KmcChunk7Boolean $a.setupWasAssisted $true;Assert-KmcChunk7Boolean $a.setupIsOrdinaryAcquisitionProof $false
 Assert-KmcChunk7Boolean $O.sourceAcquisition.nativeWrite.saveBeforeFieldSelection $true
 $labels=[Collections.Generic.HashSet[int]]::new();foreach($id in $O.nativeLoad.identities.ids){Assert-KmcAcquiredManualObjectIdentity $id;if(-not$labels.Add([int]$id)){throw 'Assisted two-owner native labels repeat.'}}
 Assert-KmcTwoAcquiredOwners ([pscustomobject]@{acquiredManual=$O.selection}) $O $O.sourceAcquisition $labels
}
function Assert-KmcAssistedSmallInheritedPair($O,$Saved){
 $before=$O.inheritedAssistedPairBefore;$after=$O.inheritedAssistedPairAfter;$prior=$Saved.twoOwners[0]
 $keys=@('actorId','petId','actorObject','petObject','actorDescriptor','petDescriptor','actorView','petView','featureObject','sourceObject','rankObject','actorBlueprint','petBlueprint','featureGuid','sourceGuid','rank','reciprocal','resources')
 foreach($row in @($before,$after)){Assert-KmcAssistedObjectValue $row $keys 'Small inherited assisted pair'}
 Assert-KmcAssistedSame $before $after 'Small inherited pair before/after'
 foreach($key in @('actorId','petId','actorBlueprint','petBlueprint','featureGuid','sourceGuid')){Assert-KmcBootstrapInventoryString $before.$key $prior.$key}
 Assert-KmcOrdinaryInteger $before.rank $prior.rank $prior.rank;Assert-KmcChunk7Boolean $before.reciprocal $true
 $used=[Collections.Generic.HashSet[int]]::new();foreach($actor in $O.manualNativeRoster.actors){foreach($key in @('object','descriptor','view')){[void]$used.Add([int]$actor.$key)}}
 foreach($role in @('actor','pet')){$id=$before.($role+'Id');$actors=@($O.manualNativeRoster.actors|Where-Object id -CEQ $id);if($actors.Count-ne1){throw 'Small inherited actor absent from native scope.'}
  foreach($pair in @(@('Object','object'),@('Descriptor','descriptor'),@('View','view'))){Assert-KmcAcquiredManualObjectIdentity $before.($role+$pair[0]);Assert-KmcOrdinaryInteger $before.($role+$pair[0]) $actors[0].($pair[1]) $actors[0].($pair[1])}
 }
 foreach($key in @('featureObject','sourceObject','rankObject')){Assert-KmcAcquiredManualObjectIdentity $before.$key;if(-not$used.Add([int]$before.$key)){throw 'Small inherited feature/source/rank aliases another role.'}}
 $expected=@($Saved.resourcesAfterSave.actors|Where-Object{$_.actorId-cin@($prior.actorId,$prior.petId)})
 Assert-KmcCumulativeResources ([pscustomobject]@{actors=$before.resources}) @($prior.actorId,$prior.petId)
 Assert-KmcAssistedSame $before.resources $expected 'Small inherited native resources'
}
