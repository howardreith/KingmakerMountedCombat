Set-StrictMode -Version Latest
function Test-KmcChunk7SixPairStress($Request){(Get-KmcChunk7OwnedPairsCase $Request)-ceq'six-horse-cold-fixture-lifecycle-stress'}
function Get-KmcChunk7SixPairRoles {
 @('73681ef2-b281-4bc6-bc05-2de68c1616cb','30b79228-3fe5-48ac-aa88-2de9085837b7',
   'c4e2c853-4b72-4904-ad84-d0b7bfba7470','0e0c7d2d-06b4-4510-9f81-d847340d1424',
   'fd213210-71cd-41b0-b35b-68e1b18e9b43','da28f0a7-6eb0-4392-b2e0-f401a889b3aa',
   '5dd7bd1d-ae4c-4df7-9687-50fceb027344','e486e6e9-aa36-423e-9d9e-9dfbe4a94a07',
   '82deb3d9-d734-4843-81c6-384becf082d9','1ed01148-1646-469c-a3d5-153df01192b8',
   'd8c2f840-9d59-4b5e-9250-c628a36c5589','130fd1b5-c9f4-4b9f-80de-6980e0d21556')
}
function Assert-KmcChunk7SixPairStressSelection($Request,$Selection,$Checkpoint){
 Assert-KmcExactProperties $Selection @('case','sourceRunId','sourceArtifactSha256','sourceArchiveSha256',
  'checkpointSourceRunId','checkpointSourceArtifactSha256','riderAId','mountAId','riderBId','mountBId',
  'partyIds','baseGameOnly','expectedNativeLoadCount','expectedNativeWriteCount') 'six-pair cold-fixture stress selection'
 $roles=@(Get-KmcChunk7SixPairRoles)
 if($null-ne$Checkpoint){throw 'Six-pair zero-write stress selects no persistence checkpoint.'}
 foreach($pair in @(@('case','six-horse-cold-fixture-lifecycle-stress'),@('sourceRunId','20261022-c7b-world185-b185s1-sixthhorse-cold'),
  @('sourceArtifactSha256','012d1caff175d34614fcd255f0b6ad897e0ef1a9650791cb3c0d55ce13b47241'),
  @('sourceArchiveSha256','b1e817a60c8bbe35b5947b4eab3b3f35060493ca20873d5de5fdf8a81efbc291'),
  @('riderAId',$roles[0]),@('mountAId',$roles[1]),@('riderBId',$roles[2]),@('mountBId',$roles[3]))){Assert-KmcBootstrapInventoryString $Selection.($pair[0]) $pair[1]}
 foreach($key in @('checkpointSourceRunId','checkpointSourceArtifactSha256')){if($null-ne$Selection.$key){throw 'Six-pair stress admits no substitute checkpoint.'}}
 if($Request.runId-isnot[string]-or$Request.runId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'-or$Request.runId-ceq$Selection.sourceRunId){throw 'Six-pair stress requires a distinct bounded run identity.'}
 Assert-KmcAssistedArray $Selection.partyIds 12 12 'six-pair complete field roster'
 Assert-KmcAssistedSame @($Selection.partyIds|Sort-Object -CaseSensitive) @($roles|Sort-Object -CaseSensitive) 'six-pair exact role set'
 Assert-KmcChunk7Boolean $Selection.baseGameOnly $true;Assert-KmcOrdinaryInteger $Selection.expectedNativeLoadCount 1 1
 Assert-KmcOrdinaryInteger $Selection.expectedNativeWriteCount 0 0
 if($null-ne$Request.PSObject.Properties['f11AllocationProbe']-and$null-ne$Request.f11AllocationProbe){throw 'This six-pair frame/lifecycle observer does not select native allocation instrumentation.'}
}
function Assert-KmcChunk7SixPairStressDescriptor($Request){
 $d=$Request.persistenceLoad
 foreach($pair in @(@('internalName','KMC_C7_SIX_HORSE_WORLD'),@('fileName','Manual_308_KMC_C7_SIX_HORSE_WORLD.zks'),
  @('sha256','b1e817a60c8bbe35b5947b4eab3b3f35060493ca20873d5de5fdf8a81efbc291'),
  @('gameId','3f7381f7-aba6-43bf-bae6-39c6a66a7180'),@('gameName','Testy the Modless'),@('area','b10e86516b6437d45a629931c30936fe'))){Assert-KmcBootstrapInventoryString $d.($pair[0]) $pair[1]}
 Assert-KmcOrdinaryInteger $d.length 1332308 1332308;Assert-KmcOrdinaryInteger $d.lastWriteTimeUtcTicks 639282712706531806L 639282712706531806L
 if($d.sha256-ceq$Request.fixture.working.sha256-or$d.sha256-ceq$Request.fixture.baseline.sha256){throw 'Six-pair stress aliases a protected archive.'}
}
function Assert-KmcChunk7SixPairInheritedSnapshot($Request,$Snapshot,[string]$Phase){
 Assert-KmcExactProperties $Snapshot @('contract','phase','sourceRunId','sourceArtifactSha256','sourceArchiveSha256','sourceCase',
  'nativeLoadVerifiedByHost','decodedReceiptAuthorizesRestoration','inheritedPetsPreserved','ownedFactRemovalCount',
  'partyIds','currentPartyIds','currentCharacterIds','remoteCapturedIds','arrangement','pairs','identities','remote') 'six-pair inherited custody'
 foreach($pair in @(@('contract','cold-qualified-six-horse-native-field-v1'),@('phase',$Phase),@('sourceCase','sixth-horse-manual-readonly-inventory'))){Assert-KmcBootstrapInventoryString $Snapshot.($pair[0]) $pair[1]}
 foreach($key in @('sourceRunId','sourceArtifactSha256','sourceArchiveSha256')){Assert-KmcBootstrapInventoryString $Snapshot.$key $Request.ownedPairs.$key}
 foreach($key in @('nativeLoadVerifiedByHost','inheritedPetsPreserved')){Assert-KmcChunk7Boolean $Snapshot.$key $true}
 Assert-KmcChunk7Boolean $Snapshot.decodedReceiptAuthorizesRestoration $false;Assert-KmcOrdinaryInteger $Snapshot.ownedFactRemovalCount 0 0
 $roles=@(Get-KmcChunk7SixPairRoles);$riders=@(for($i=0;$i-lt6;$i++){$roles[$i*2]})
 foreach($key in @('partyIds','currentPartyIds')){Assert-KmcAssistedArray $Snapshot.$key 12 12 'six-pair exact field';Assert-KmcAssistedSame @($Snapshot.$key|Sort-Object -CaseSensitive) @($roles|Sort-Object -CaseSensitive) 'six-pair field membership'}
 Assert-KmcAssistedSame @($Snapshot.currentCharacterIds|Sort-Object -CaseSensitive) @($riders|Sort-Object -CaseSensitive) 'six-pair character membership'
 Assert-KmcAssistedArray $Snapshot.remoteCapturedIds 0 0 'six-pair field actors stay in the field'
 Assert-KmcExactProperties $Snapshot.identities @('capacity','ids','faults') 'six-pair retained identity registry'
 Assert-KmcOrdinaryInteger $Snapshot.identities.capacity 128 128;Assert-KmcOrdinaryInteger $Snapshot.identities.faults 0 0
 Assert-KmcAssistedArray $Snapshot.identities.ids 26 26 'six-pair actor/fact and remote identities'
 $labels=[Collections.Generic.HashSet[int]]::new();foreach($id in $Snapshot.identities.ids){Assert-KmcManualSeedObjectIdentity $id;if(-not$labels.Add([int]$id)){throw 'Six-pair registry repeats a native label.'}}
 $used=[Collections.Generic.HashSet[int]]::new();Assert-KmcAssistedArray $Snapshot.pairs 6 6 'six inherited native Horses'
 for($i=0;$i-lt6;$i++){
  $p=$Snapshot.pairs[$i];Assert-KmcExactProperties $p @('riderId','mountId','rider','mount','feature','rankFact','sourceGuid','rank','profileId') 'six-pair inherited role'
  Assert-KmcBootstrapInventoryString $p.riderId $roles[$i*2];Assert-KmcBootstrapInventoryString $p.mountId $roles[$i*2+1]
  Assert-KmcBootstrapInventoryString $p.sourceGuid '97261d609529d834eba4fd4da1bc44dc';Assert-KmcOrdinaryInteger $p.rank 1 1
  Assert-KmcBootstrapInventoryString $p.profileId $(if($i-cin@(1,4,5)){'small-default-halfling-horse-v1'}else{'medium-humanoid-horse-v1'})
  foreach($key in @('rider','mount','feature','rankFact')){Assert-KmcManualSeedObjectIdentity $p.$key;if(-not$labels.Contains([int]$p.$key)-or-not$used.Add([int]$p.$key)){throw 'Six-pair native role aliases or lacks retained custody.'}}
 }
 $remote=$Snapshot.remote
 Assert-KmcExactProperties $remote @('actorId','object','descriptor','petId','masterId','arrangement') 'six-pair exact remote Linzi'
 Assert-KmcBootstrapInventoryString $remote.actorId '8265f8cf-0969-4ec7-a266-2448de91c8c4'
 foreach($key in @('petId','masterId')){if($null-ne$remote.$key){throw 'Remote Linzi acquired an undeclared pet/master.'}}
 foreach($key in @('object','descriptor')){Assert-KmcManualSeedObjectIdentity $remote.$key;if(-not$labels.Contains([int]$remote.$key)-or-not$used.Add([int]$remote.$key)){throw 'Remote Linzi aliases a six-pair native role.'}}
 $expectedRemote=[pscustomobject]@{money=357290;experienceRatePercent=100;actors=@([pscustomobject]@{
  actorId=$remote.actorId;blueprint='77c11edb92ce0fd408ad96b40fd27121';experience=9000;characterLevel=4;
  classes=@([pscustomobject]@{classGuid='772c83a25e2268e448e841dcd548235f';level=4})})}
 Assert-KmcAssistedSame $remote.arrangement $expectedRemote 'remote Linzi original progression'
 # Consume only the pinned accepted cold observation, without recursively
 # qualifying acquisition ancestry or borrowing native authority from it.
 $source=Get-KmcChunk7SixPairCheckpointSource $Request.fixture
 $cold=(Read-KmcJson $source.artifactPath).observations.settledWorld
 $arrangement=[pscustomobject]@{money=$cold.nativeMoney;experienceRatePercent=100;actors=@($cold.party|Sort-Object id -CaseSensitive|ForEach-Object{
  [pscustomobject]@{actorId=$_.id;blueprint=$_.blueprint;experience=$_.experience;characterLevel=$_.characterLevel;
   classes=@($_.classes|Sort-Object guid -CaseSensitive|ForEach-Object{[pscustomobject]@{classGuid=$_.guid;level=$_.level}})}})}
 Assert-KmcAssistedSame $Snapshot.arrangement $arrangement 'six-pair exact sourced field progression and funds'
}
function Assert-KmcChunk7SixPairWorld($Snapshot,[int]$Active,[string[]]$Roles){
 Assert-KmcChunk7WorldCounters $Snapshot @('frame','boundaryCount','participantCount')
 Assert-KmcOrdinaryInteger $Snapshot.boundaryCount 6 6;Assert-KmcOrdinaryInteger $Snapshot.participantCount 6 6
 Assert-KmcAssistedArray $Snapshot.owners $Active $Active 'six-pair exact active custody'
 $expected=@(for($i=0;$i-lt$Active;$i++){$Roles[$i*2]})
 Assert-KmcChunk7ExactIds $Snapshot.owners $expected 'riderId' -GuidValue
 $seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
 foreach($owner in $Snapshot.owners){
  [void](ConvertTo-KmcChunk7PairIdN $owner.ownerId);Assert-KmcOrdinaryInteger $owner.generation 1 ([long]::MaxValue)
  Assert-KmcBootstrapInventoryString $owner.state 'Active';Assert-KmcBootstrapInventoryString $owner.relationshipState 'Mounted'
  if(-not$seen.Add($owner.ownerId)){throw 'Six-pair world duplicates an owner.'}
  $index=-1;for($i=0;$i-lt6;$i++){if(Test-KmcChunk7OwnerActorMatch $owner.riderId $Roles[$i*2]){$index=$i;break}}
  if($index-lt0-or-not(Test-KmcChunk7OwnerActorMatch $owner.mountId $Roles[$index*2+1])){throw 'Six-pair world substitutes a native actor role.'}
 }
 Assert-KmcChunk7ExactIds $Snapshot.resources.actors $Roles
 foreach($row in $Snapshot.resources.actors){foreach($key in @('move','standard','swift')){if(-not(Test-KmcChunk7FiniteNumber $row.$key)-or$row.$key-lt0){throw 'Six-pair native resource is not finite nonnegative debt.'}}}
}
function Assert-KmcChunk7SixPairGroundSnapshot($Snapshot,[string[]]$Roles,[string]$Selected){
 Assert-KmcChunk7ExactIds $Snapshot.actors $Roles
 foreach($key in @('commandsComplete','paused')){Assert-KmcChunk7Boolean $Snapshot.$key $true}
 foreach($key in @('turnBased','gamepad','simulatingClick','serializationBusy','saveWorkerRunning','restorationPending','selectionTruncated')){Assert-KmcChunk7Boolean $Snapshot.$key $false}
 Assert-KmcOrdinaryInteger $Snapshot.selectionCount 1 1
 Assert-KmcAssistedArray $Snapshot.selection 1 1 'six-pair ground native selection'
 Assert-KmcBootstrapInventoryString $Snapshot.selection[0].id $Selected
 Assert-KmcManualSeedObjectIdentity $Snapshot.selection[0].object
 foreach($actor in $Snapshot.actors){
  foreach($key in @('object','commandsObject')){Assert-KmcManualSeedObjectIdentity $actor.$key}
  foreach($key in @('rawTruncated','queueTruncated')){Assert-KmcChunk7Boolean $actor.$key $false}
  Assert-KmcOrdinaryInteger $actor.rawCount 0 8;Assert-KmcOrdinaryInteger $actor.queueCount 0 16
  Assert-KmcAssistedArray $actor.raw $actor.rawCount $actor.rawCount 'six-pair bounded native raw commands'
  Assert-KmcAssistedArray $actor.queue $actor.queueCount $actor.queueCount 'six-pair bounded native queued commands'
  foreach($command in @($actor.raw)+@($actor.queue)+@($actor.previous)){
   Assert-KmcChunk7WorldInt32 $command.object
   if($command.object-ne0){
    Assert-KmcBootstrapInventoryString $command.executorId $actor.id
    Assert-KmcChunk7WorldInt32 $command.executorObject
    foreach($key in @('readError')){if($null-ne$command.PSObject.Properties[$key]){throw 'Six-pair command custody contains a read failure.'}}
   }
  }
  foreach($key in @('publicMoveObject','rawMoveObject')){Assert-KmcChunk7WorldInt32 $actor.$key}
  if($null-ne$actor.PSObject.Properties['readError']){throw 'Six-pair actor command custody contains a read failure.'}
 }
 if($null-ne$Snapshot.PSObject.Properties['environmentError']){throw 'Six-pair ground environment observation failed.'}
}
# b210 r39: the before-stress native vision clearance (2.1.7b VisionRangeMeters plus a 3 m margin, 2D distance) of every
# conscious non-player-faction unit from the twelve pair actors and six declared waypoints. A PASS run must record it clear.
function Assert-KmcChunk7SixPairHostileClearance($C){
 Assert-KmcExactProperties $C @('contract','phase','frame','gameTicks','combatVisionMeters','marginMeters','points','units','breaches','clear') 'six-pair native vision clearance'
 if($C.contract-isnot[string]-or$C.contract-cne'six-pair-native-vision-clearance-v1'-or$C.phase-isnot[string]-or$C.phase-cne'before-stress'){throw 'Six-pair native vision clearance contract differs.'}
 Assert-KmcOrdinaryInteger $C.frame 0 ([int]::MaxValue);Assert-KmcOrdinaryInteger $C.gameTicks 0 ([long]::MaxValue);Assert-KmcOrdinaryInteger $C.points 18 18
 if(-not(Test-KmcChunk7FiniteNumber $C.marginMeters)-or[double]$C.marginMeters-ne3-or-not(Test-KmcChunk7FiniteNumber $C.combatVisionMeters)-or[Math]::Abs([double]$C.combatVisionMeters-11.7)-gt0.0001){throw 'Six-pair native vision clearance constants differ.'}
 Assert-KmcChunk7Boolean $C.clear $true
 if($C.breaches-isnot[Array]-or@($C.breaches).Count-ne0-or$C.units-isnot[Array]){throw 'Six-pair native vision clearance was breached or malformed.'}
 $seen=@{}
 foreach($u in @($C.units)){
  Assert-KmcExactProperties $u @('id','blueprint','faction','peacefulFaction','groupId','awake','inFog','position','visionRangeMeters','requiredClearanceMeters','nearestPoint','nearestDistance2D','extendedVisionCoversPoint','clear') 'six-pair clearance unit'
  if($u.id-isnot[string]-or$u.id.Length-eq0-or$seen.ContainsKey($u.id)){throw 'Six-pair clearance unit identity is missing or repeated.'};$seen[$u.id]=$true
  foreach($k in @('peacefulFaction','awake','inFog')){if($u.$k-isnot[bool]){throw 'Six-pair clearance unit flag is not boolean.'}}
  Assert-KmcChunk7Boolean $u.extendedVisionCoversPoint $false;Assert-KmcChunk7Boolean $u.clear $true
  foreach($k in @('visionRangeMeters','requiredClearanceMeters','nearestDistance2D')){if(-not(Test-KmcChunk7FiniteNumber $u.$k)){throw 'Six-pair clearance distance is not finite.'}}
  if([double]$u.visionRangeMeters-le0-or[Math]::Abs([double]$u.requiredClearanceMeters-([double]$u.visionRangeMeters+3))-gt0.0001-or[double]$u.nearestDistance2D-lt[double]$u.requiredClearanceMeters){throw 'Six-pair clearance unit is inside native vision plus margin.'}
  if($u.nearestPoint-isnot[string]-or$u.nearestPoint-cnotmatch'^(actor|waypoint):.+'){throw 'Six-pair clearance nearest point differs.'}
 }
}
# F3 r39: a random roamer passed the before-stress clearance at 19.35 m and roamed natively into vision of an eastern actor.
# Producers from then on choose each cycle-1 ground goal (the declared +x 2 m route first) so that native vision clearance
# plus margin and the 4 m dismount reach holds from every hostile's current position and native wander envelope
# (2.1.7b UnitRoamingController: OriginalPoint + Settings.Radius, or the waypoint graph, plus the 1 m roaming approach).
# Lifecycle v1/v2 evidence keeps its original verdict: a v1 record binds each routed move to its recorded goal and, without
# the record, the move must be the declared +x 2 m route. Lifecycle v3 requires the v2 record and recomputes it below.
function Assert-KmcChunk7SixPairGroundGoals($O,[string[]]$Roles){
 $moves=@($O.movements)
 if($null-eq$O.PSObject.Properties['sixPairGroundGoals']){
  foreach($m in $moves){if([Math]::Abs([double]$m.requestedGoal.x-[double]$m.start.x-2)-gt0.001-or[Math]::Abs([double]$m.requestedGoal.y-[double]$m.start.y)-gt0.001-or[Math]::Abs([double]$m.requestedGoal.z-[double]$m.start.z)-gt0.001){throw 'Six-pair ground move left the declared route without its recorded wander-clear goal.'}}
  return
 }
 $G=$O.sixPairGroundGoals
 Assert-KmcExactProperties $G @('contract','roamingApproachMeters','marginMeters','dismountReachMeters','spacingMeters','envelopes','pairs','clear') 'six-pair wander-clear ground goals'
 Assert-KmcBootstrapInventoryString $G.contract 'six-pair-wander-clear-ground-goals-v1';Assert-KmcChunk7Boolean $G.clear $true
 foreach($pair in @(@('roamingApproachMeters',1.0),@('marginMeters',3.0),@('dismountReachMeters',4.0),@('spacingMeters',1.75))){
  if(-not(Test-KmcChunk7FiniteNumber $G.($pair[0]))-or[Math]::Abs([double]$G.($pair[0])-$pair[1])-gt0.0001){throw 'Six-pair ground goal constants differ.'}}
 if($G.envelopes-isnot[Array]-or$G.pairs-isnot[Array]){throw 'Six-pair ground goal record is malformed.'}
 foreach($e in @($G.envelopes)){if($e.kind-isnot[string]-or$e.kind-cnotin@('stationary','roaming-disabled','random','waypoints')){throw 'Six-pair ground goals carry an unreadable or unknown native wander envelope.'}
  if($e.kind-ceq'random'-and(-not(Test-KmcChunk7FiniteNumber $e.radius)-or[double]$e.radius-lt0)){throw 'Six-pair random wander envelope lacks its native radius.'}}
 Assert-KmcAssistedArray $G.pairs 6 6 'six wander-clear ground goals'
 for($i=0;$i-lt6;$i++){
  $p=$G.pairs[$i];Assert-KmcOrdinaryInteger $p.index $i $i;Assert-KmcBootstrapInventoryString $p.riderId $Roles[$i*2];Assert-KmcBootstrapInventoryString $p.mountId $Roles[$i*2+1]
  $c=$p.chosen;if($null-eq$c){throw 'Six-pair ground goals lack a chosen goal.'}
  foreach($k in @('distance','bearing','minimumEnvelopeClearanceMeters')){if(-not(Test-KmcChunk7FiniteNumber $c.$k)){throw 'Six-pair ground goal values are not finite.'}}
  if([double]$c.distance-lt2-or[double]$c.distance-gt10-or[double]$c.minimumEnvelopeClearanceMeters-lt3){throw 'Six-pair ground goal is outside its bounded search or its wander-envelope clearance.'}
  foreach($axis in @('x','y','z')){if(-not(Test-KmcChunk7FiniteNumber $c.goal.$axis)-or[Math]::Abs([double]$c.goal.$axis-[double]$moves[$i].requestedGoal.$axis)-gt0.0001){throw 'Six-pair ground move did not target its recorded wander-clear goal.'}}
 }
}
function Get-KmcChunk7SixDistance2D([double]$Ax,[double]$Az,[double]$Bx,[double]$Bz){[Math]::Sqrt(($Ax-$Bx)*($Ax-$Bx)+($Az-$Bz)*($Az-$Bz))}
# Chunk7SixPairCombatEntry.RoamingLegDistance2D: the 2D segment distance less the 1 m roaming approach, never negative.
function Get-KmcChunk7SixLegDistance2D([double]$Px,[double]$Pz,[double]$Ax,[double]$Az,[double]$Bx,[double]$Bz){
 $dx=$Bx-$Ax;$dz=$Bz-$Az;$length=$dx*$dx+$dz*$dz;$t=0.0
 if($length-gt0){$t=[Math]::Max(0.0,[Math]::Min(1.0,(($Px-$Ax)*$dx+($Pz-$Az)*$dz)/$length))}
 [Math]::Max(0.0,(Get-KmcChunk7SixDistance2D $Px $Pz ($Ax+$t*$dx) ($Az+$t*$dz))-1.0)
}
function Assert-KmcChunk7SixPoint($Point,[string]$Description){
 Assert-KmcAssistedObjectValue $Point @('x','y','z') $Description
 foreach($axis in @('x','y','z')){if(-not(Test-KmcChunk7FiniteNumber $Point.$axis)){throw ($Description+' lacks finite coordinates.')}}
}
# The recorded v2 native wander envelope, validated by kind (SixWanderEnvelope): its 2D distance from a point.
function Assert-KmcChunk7SixWanderEnvelope($E){
 $kind=$E.kind;if($kind-isnot[string]){throw 'Six-pair wander envelope kind is missing.'}
 $fields=switch -CaseSensitive ($kind){
  'stationary'{@('id','groupId','position','kind')};'roaming-disabled'{@('id','groupId','position','kind')}
  'random'{@('id','groupId','position','nextPoint','kind','originalPoint','radius')}
  'waypoints'{@('id','groupId','position','nextPoint','kind','waypoints','legs','randomFallback','originalPoint','radius')}
  default{throw 'Six-pair ground goals carry an unreadable or unknown native wander envelope.'}}
 Assert-KmcExactProperties $E $fields 'six-pair native wander envelope'
 if($E.id-isnot[string]-or$E.id.Length-eq0){throw 'Six-pair wander envelope identity is missing.'}
 Assert-KmcChunk7SixPoint $E.position 'six-pair wander envelope position'
 if($kind-cin@('stationary','roaming-disabled')){return}
 if($null-ne$E.nextPoint){Assert-KmcChunk7SixPoint $E.nextPoint 'six-pair wander envelope next point'}elseif($kind-ceq'waypoints'){throw 'Six-pair waypoint envelope lacks its native next waypoint.'}
 Assert-KmcChunk7SixPoint $E.originalPoint 'six-pair wander envelope original point'
 $disk=$kind-ceq'random'
 if($kind-ceq'waypoints'){
  if($E.randomFallback-isnot[bool]){throw 'Six-pair waypoint envelope fallback is not boolean.'};$disk=$E.randomFallback
  if($E.waypoints-isnot[Array]-or@($E.waypoints).Count-lt1-or@($E.waypoints).Count-gt257-or$E.legs-isnot[Array]){throw 'Six-pair waypoint envelope graph is malformed.'}
  foreach($w in @($E.waypoints)){Assert-KmcChunk7SixPoint $w 'six-pair waypoint'}
  foreach($l in @($E.legs)){Assert-KmcExactProperties $l @('from','to') 'six-pair waypoint leg';Assert-KmcChunk7SixPoint $l.from 'six-pair waypoint leg';Assert-KmcChunk7SixPoint $l.to 'six-pair waypoint leg'}
 }
 if($disk){if(-not(Test-KmcChunk7FiniteNumber $E.radius)-or[double]$E.radius-lt0){throw 'Six-pair wander envelope lacks its native radius.'}}
 elseif(-not(Test-KmcChunk7FiniteNumber $E.radius)){throw 'Six-pair wander envelope radius is not finite.'}
}
function Get-KmcChunk7SixWanderDistance2D($E,[double]$Px,[double]$Pz){
 $hx=[double]$E.position.x;$hz=[double]$E.position.z
 if($E.kind-cin@('stationary','roaming-disabled')){return Get-KmcChunk7SixDistance2D $Px $Pz $hx $hz}
 $nx=$hx;$nz=$hz;if($null-ne$E.nextPoint){$nx=[double]$E.nextPoint.x;$nz=[double]$E.nextPoint.z}
 $result=Get-KmcChunk7SixLegDistance2D $Px $Pz $hx $hz $nx $nz
 if($E.kind-ceq'random'-or$E.randomFallback-eq$true){
  $result=[Math]::Min($result,[Math]::Max(0.0,(Get-KmcChunk7SixDistance2D $Px $Pz ([double]$E.originalPoint.x) ([double]$E.originalPoint.z))-[double]$E.radius-1.0))}
 if($E.kind-ceq'waypoints'){
  foreach($w in @($E.waypoints)){$result=[Math]::Min($result,[Math]::Max(0.0,(Get-KmcChunk7SixDistance2D $Px $Pz ([double]$w.x) ([double]$w.z))-1.0))}
  foreach($l in @($E.legs)){$result=[Math]::Min($result,(Get-KmcChunk7SixLegDistance2D $Px $Pz ([double]$l.from.x) ([double]$l.from.z) ([double]$l.to.x) ([double]$l.to.z)))}
 }
 $result
}
# Chunk7SixPairCombatEntry.GroundGoalDistances x GroundGoalBearings: the nearest-first bounded ring, the declared +x 2 m route first.
function Get-KmcChunk7SixGoalBearings{$b=@(0.0);for($i=1;$i-lt16;$i++){$b+=$(if($i%2-eq1){1.0}else{-1.0})*22.5*[Math]::Floor(($i+1)/2)};$b[15]=180.0;$b}
function Get-KmcChunk7SixGoalOrdinal($Distance,$Bearing){
 if(-not(Test-KmcChunk7FiniteNumber $Distance)-or-not(Test-KmcChunk7FiniteNumber $Bearing)){throw 'Six-pair ground goal candidate is not finite.'}
 $d=[double]$Distance;$bearings=@(Get-KmcChunk7SixGoalBearings);$di=-1;$bi=-1
 for($k=0;$k-lt9;$k++){if($d-eq(2.0+$k)){$di=$k}};for($k=0;$k-lt16;$k++){if([double]$Bearing-eq$bearings[$k]){$bi=$k}}
 if($di-lt0-or$bi-lt0){throw 'Six-pair ground goal candidate is outside the declared bounded ring.'}
 $di*16+$bi+1
}
# v2 (lifecycle v3): every chosen goal is recomputed from the record. Its candidate is the first admitted in the declared
# order after the recorded first refusals; it lies within the 0.1 m navmesh projection of its ring point from the recorded
# start (Unity Quaternion.Euler(0, bearing, 0) * Vector3.right); it keeps the declared spacing from earlier goals and later
# mounts; and every recorded hostile (exactly the before-stress clearance units, at the same positions) keeps native vision
# plus the 3 m margin from the nearer of its current position and its recorded wander envelope less the 4 m dismount reach.
function Assert-KmcChunk7SixPairWanderClearGoals($O,[string[]]$Roles){
 if($null-eq$O.PSObject.Properties['sixPairGroundGoals']){throw 'Six-pair v3 evidence lacks its wander-clear ground goals.'}
 if($null-eq$O.PSObject.Properties['sixPairHostileClearance']){throw 'Six-pair v3 evidence lacks its before-stress native vision clearance.'}
 $G=$O.sixPairGroundGoals;$moves=@($O.movements);$tolerance=0.0001
 Assert-KmcExactProperties $G @('contract','roamingApproachMeters','marginMeters','dismountReachMeters','spacingMeters','envelopes','pairs','clear') 'six-pair wander-clear ground goals'
 Assert-KmcBootstrapInventoryString $G.contract 'six-pair-wander-clear-ground-goals-v2';Assert-KmcChunk7Boolean $G.clear $true
 foreach($pair in @(@('roamingApproachMeters',1.0),@('marginMeters',3.0),@('dismountReachMeters',4.0),@('spacingMeters',1.75))){
  if(-not(Test-KmcChunk7FiniteNumber $G.($pair[0]))-or[Math]::Abs([double]$G.($pair[0])-$pair[1])-gt$tolerance){throw 'Six-pair ground goal constants differ.'}}
 if($G.envelopes-isnot[Array]-or$G.pairs-isnot[Array]){throw 'Six-pair ground goal record is malformed.'}
 $units=@{};foreach($u in @($O.sixPairHostileClearance.units)){$units[$u.id]=$u}
 $envelopes=@($G.envelopes);$ids=@{}
 foreach($e in $envelopes){
  Assert-KmcChunk7SixWanderEnvelope $e
  if($ids.ContainsKey($e.id)){throw 'Six-pair wander envelopes repeat a unit.'};$ids[$e.id]=$true
  if(-not$units.ContainsKey($e.id)){throw 'Six-pair wander envelope names a unit outside the before-stress clearance.'}
  foreach($axis in @('x','y','z')){if([Math]::Abs([double]$e.position.$axis-[double]$units[$e.id].position.$axis)-gt$tolerance){throw 'Six-pair wander envelope position differs from the before-stress clearance.'}}
 }
 if($ids.Count-ne$units.Count){throw 'Six-pair wander envelopes omit a before-stress hostile.'}
 Assert-KmcAssistedArray $G.pairs 6 6 'six wander-clear ground goals'
 $reasons=@('not-walkable','navmesh-projection','route-not-straight-on-navmesh','pair-spacing','unit-footprint','hostile-wander-vision','hostile-extended-vision')
 for($i=0;$i-lt6;$i++){
  $p=$G.pairs[$i];Assert-KmcExactProperties $p @('index','mountId','riderId','chosen','firstRefusals') 'six-pair wander-clear ground goal'
  Assert-KmcOrdinaryInteger $p.index $i $i;Assert-KmcBootstrapInventoryString $p.riderId $Roles[$i*2];Assert-KmcBootstrapInventoryString $p.mountId $Roles[$i*2+1]
  $c=$p.chosen;if($null-eq$c){throw 'Six-pair ground goals lack a chosen goal.'}
  Assert-KmcExactProperties $c @('distance','bearing','goal','start','candidatesTried','minimumEnvelopeClearanceMeters') 'six-pair chosen ground goal'
  Assert-KmcChunk7SixPoint $c.goal 'six-pair chosen ground goal';Assert-KmcChunk7SixPoint $c.start 'six-pair chosen ground goal start'
  $ordinal=Get-KmcChunk7SixGoalOrdinal $c.distance $c.bearing;Assert-KmcOrdinaryInteger $c.candidatesTried $ordinal $ordinal
  if($null-eq$p.firstRefusals-or$p.firstRefusals.GetType().FullName-cne'System.Management.Automation.PSCustomObject'){throw 'Six-pair ground goal refusals are malformed.'}
  $last=0;foreach($r in $p.firstRefusals.PSObject.Properties){
   if($r.Name-cnotin$reasons-or$r.Value-isnot[Array]-or@($r.Value).Count-ne2){throw 'Six-pair ground goal refusal is unknown or malformed.'}
   $n=Get-KmcChunk7SixGoalOrdinal $r.Value[0] $r.Value[1];if($n-le$last-or$n-ge$ordinal){throw 'Six-pair ground goal refusals are out of search order or follow the chosen goal.'};$last=$n}
  if(($ordinal-gt1)-ne($last-gt0)-or($last-gt0-and(Get-KmcChunk7SixGoalOrdinal @($p.firstRefusals.PSObject.Properties)[0].Value[0] @($p.firstRefusals.PSObject.Properties)[0].Value[1])-ne1)){throw 'Six-pair ground goal search does not record the refusal of every earlier candidate.'}
  $gx=[double]$c.goal.x;$gz=[double]$c.goal.z;$sx=[double]$c.start.x;$sz=[double]$c.start.z;$radians=[double]$c.bearing*[Math]::PI/180.0
  if((Get-KmcChunk7SixDistance2D $gx $gz ($sx+[double]$c.distance*[Math]::Cos($radians)) ($sz-[double]$c.distance*[Math]::Sin($radians)))-gt0.1+$tolerance){throw 'Six-pair ground goal is not its ring candidate on the native navmesh.'}
  # The move starts from the mount after the cycle-1 Mount; the mount stands, so its start stays within 0.5 m.
  if((Get-KmcChunk7SixDistance2D $sx $sz ([double]$moves[$i].start.x) ([double]$moves[$i].start.z))-gt0.5){throw 'Six-pair ground goal start is not the routed move''s mount.'}
  foreach($axis in @('x','y','z')){if([Math]::Abs([double]$c.goal.$axis-[double]$moves[$i].requestedGoal.$axis)-gt$tolerance){throw 'Six-pair ground move did not target its recorded wander-clear goal.'}}
  for($j=0;$j-lt6;$j++){if($j-eq$i){continue};$other=if($j-lt$i){$G.pairs[$j].chosen.goal}else{$G.pairs[$j].chosen.start}
   if($null-eq$other){throw 'Six-pair ground goal spacing lacks another pair.'}
   if((Get-KmcChunk7SixDistance2D $gx $gz ([double]$other.x) ([double]$other.z))-lt1.75-$tolerance){throw 'Six-pair ground goal breaks the declared pair spacing.'}}
  $minimum=[double]::MaxValue
  foreach($e in $envelopes){
   $vision=$units[$e.id].visionRangeMeters;if(-not(Test-KmcChunk7FiniteNumber $vision)-or[double]$vision-le0){throw 'Six-pair hostile vision range is not finite and positive.'}
   $current=Get-KmcChunk7SixDistance2D $gx $gz ([double]$e.position.x) ([double]$e.position.z)
   $reach=[Math]::Min((Get-KmcChunk7SixWanderDistance2D $e $gx $gz),$current)-4.0;$minimum=[Math]::Min($minimum,$reach)
   if($reach-lt[double]$vision+3.0-$tolerance){throw 'Six-pair ground goal is inside a hostile''s native vision plus margin from its wander envelope and dismount reach.'}
  }
  if(-not(Test-KmcChunk7FiniteNumber $c.minimumEnvelopeClearanceMeters)-or[Math]::Abs([double]$c.minimumEnvelopeClearanceMeters-$minimum)-gt0.001){throw 'Six-pair ground goal recorded clearance differs from its recomputed wander-envelope clearance.'}
 }
}
function Assert-KmcChunk7SixPairStressEvidence($Request,$O){
 Assert-KmcChunk7OwnedPairsDescriptor $Request
 Assert-KmcChunk7InheritedEvidence $Request $O
 Assert-KmcAssistedSame $O.inheritedFixture.remote $O.inheritedCleanup.remote 'six-pair remote native identity/progression retention'
 $roles=@(Get-KmcChunk7SixPairRoles);$s=$O.sixPairStress
 # F2 r39: the guard records a combat entry only on its refused path; PASS evidence never carries one.
 foreach($key in @('sixPairCombatEntry','sixPairCombatEntryError')){if($null-ne$O.PSObject.Properties[$key]){throw ('Six-pair evidence carries its refused-path record '+$key+'.')}}
 # v1 (batch186-188) verified full inherited custody on every measured frame; v2 verifies it
 # at sampling-window boundaries and declares that its measured frames carry only the guard.
 # v3 (F3 r39 producers) keeps v2's custody and requires the before-stress clearance and the
 # v2 wander-clear ground goals, recomputed from the record.
 $v3=$null-ne$s.PSObject.Properties['contract']-and$s.contract-is[string]-and$s.contract-ceq'cold-qualified-six-pair-lifecycle-v3'
 # Producers from b210 on record the before-stress clearance; earlier accepted evidence predates it.
 if($v3-and$null-eq$O.PSObject.Properties['sixPairHostileClearance']){throw 'Six-pair v3 evidence lacks its before-stress native vision clearance.'}
 if($null-ne$O.PSObject.Properties['sixPairHostileClearance']){Assert-KmcChunk7SixPairHostileClearance $O.sixPairHostileClearance}
 $fields=@('contract','archiveProducerRunId','coldQualificationRunId','fixtureSetupWrites','nativeSaveRequests','nativeLoadRequests',
  'requiredCycles','requiredGroundInputs','requiredAbilityInputs','requiredWindows','windowsPerState','performanceAccepted','requestFreeControlsProven',
  'identityScopeReleased','cycles','samples','nativeCastRequests','nativeRefusals','saveSnapshots','semanticRestores','presentationRestores',
  'riderParents','riderParentChildren','siblingOrderVerifiedCycles')
 $v2=$v3-or($null-ne$s.PSObject.Properties['contract']-and$s.contract-is[string]-and$s.contract-ceq'cold-qualified-six-pair-lifecycle-v2')
 if($v2){$fields+='measuredFramesCarryFullCustody'}
 Assert-KmcExactProperties $s $fields 'six-pair native lifecycle observation'
 foreach($pair in @(@('contract',$(if($v3){'cold-qualified-six-pair-lifecycle-v3'}elseif($v2){'cold-qualified-six-pair-lifecycle-v2'}else{'cold-qualified-six-pair-lifecycle-v1'})),@('archiveProducerRunId','20261022-c7b-world184-b184s1-sixthhorse-manual'),@('coldQualificationRunId',$Request.ownedPairs.sourceRunId))){Assert-KmcBootstrapInventoryString $s.($pair[0]) $pair[1]}
 if($v2){Assert-KmcChunk7Boolean $s.measuredFramesCarryFullCustody $false}
 foreach($key in @('fixtureSetupWrites','nativeSaveRequests','nativeRefusals','saveSnapshots','semanticRestores','presentationRestores')){Assert-KmcOrdinaryInteger $s.$key 0 0}
 foreach($pair in @(@('nativeLoadRequests',1),@('requiredCycles',3),@('requiredGroundInputs',6),@('requiredAbilityInputs',36),@('requiredWindows',15),@('windowsPerState',3),@('nativeCastRequests',36))){Assert-KmcOrdinaryInteger $s.($pair[0]) $pair[1] $pair[1]}
 foreach($key in @('performanceAccepted','requestFreeControlsProven')){Assert-KmcChunk7Boolean $s.$key $false};Assert-KmcChunk7Boolean $s.identityScopeReleased $true
 # Each settled cycle reproduced every native rider parent's exact child order.
 Assert-KmcOrdinaryInteger $s.riderParents 1 6;Assert-KmcOrdinaryInteger $s.riderParentChildren 6 ([int]::MaxValue);Assert-KmcOrdinaryInteger $s.siblingOrderVerifiedCycles 3 3
 Assert-KmcChunk7Boolean $O.capability.archiveNativeEnabled $true;Assert-KmcOrdinaryInteger $O.capability.nativeWriterSchema 3 3;Assert-KmcOrdinaryInteger $O.capability.nativePairCapacity 6 6
 Assert-KmcBootstrapInventoryString $O.capability.inheritedManualSha256 $Request.ownedPairs.sourceArchiveSha256
 if($null-ne$O.capability.workingSha256){throw 'Six-pair fixture cannot claim legacy Working ancestry.'}
 $specs=@(for($cycle=1;$cycle-le3;$cycle++){foreach($mount in @($true,$false)){for($i=0;$i-lt6;$i++){
  $verb=if($mount){'mount'}else{'dismount'};$target=if($mount){$roles[$i*2+1]}else{$roles[$i*2]}
  ,@(('six-cycle-'+$cycle+'-'+$verb+'-'+$i),$roles[$i*2],$target,$(if($mount){'f053faad986631688defa003cd7bda0e'}else{'3af2b81f4d72bbb30501fa730fcdf36e'}))
 }}})
 $commands=[Collections.Generic.HashSet[long]]::new();$shells=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
 Assert-KmcChunk7InputSequence $O.inputs $specs $roles $commands $shells
 for($i=0;$i-lt36;$i++){
  $row=$O.inputs[$i];Assert-KmcChunk7Boolean $row.explorationResourcesConserved $true
  Assert-KmcBootstrapInventoryString $row.selectionAfterInput $roles[(($i+1)%6)*2]
 }
 Assert-KmcAssistedArray $O.cycleInputs 0 0 'six-pair has one explicit input ledger'
 Assert-KmcAssistedArray $O.cycles 0 0 'six-pair uses its distinct cycle ledger'
 Assert-KmcAssistedArray $O.measurements 0 0 'six-pair uses its distinct fifteen-window measurements'
 Assert-KmcAssistedArray $s.cycles 3 3 'three complete six-pair native lifecycles'
 $subscriptions=$null
 for($i=0;$i-lt3;$i++){
  $v=$s.cycles[$i];Assert-KmcBootstrapInventoryString $v.label ('six-cycle-'+($i+1)+'-settled');Assert-KmcChunk7SixPairWorld $v 0 $roles
  $count=Get-KmcChunk7Subscriptions $v.subscriptions
  if($null-ne$subscriptions-and$count-cne$subscriptions){throw 'Six-pair lifecycle grows or loses native subscriptions.'};$subscriptions=$count
 }
 $mounted=@($O.snapshots|Where-Object label -CEQ 'six-first-native-mounts')
 Assert-KmcAssistedArray $mounted 1 1 'six native mounted boundary';Assert-KmcChunk7SixPairWorld $mounted[0] 6 $roles
 Assert-KmcAssistedArray $O.movements 6 6 'six native routed ground moves';Assert-KmcAssistedArray $O.groundInputs 6 6 'six entered/returned native ground calls'
 for($i=0;$i-lt6;$i++){
  $m=$O.movements[$i];$g=$O.groundInputs[$i]
  foreach($row in @($m,$g)){
   Assert-KmcBootstrapInventoryString $row.label ('six-ground-'+$i);Assert-KmcBootstrapInventoryString $row.selectedId $roles[$i*2]
   Assert-KmcBootstrapInventoryString $row.executorId $roles[$i*2+1]
  }
  foreach($key in @('nativeCallEntered','nativeCallReturned','commandValidationPassed')){Assert-KmcChunk7Boolean $g.$key $true}
  Assert-KmcBootstrapInventoryString $g.contract 'chunk7-world-ground-input/v1'
  foreach($phase in @('before','after')){Assert-KmcChunk7SixPairGroundSnapshot $g.$phase $roles $g.selectedId}
  foreach($a in $g.before.actors){
   $b=@($g.after.actors|Where-Object id -CEQ $a.id)[0]
   foreach($key in @('object','commandsObject')){Assert-KmcOrdinaryInteger $b.$key $a.$key $a.$key}
   $expected=$(if($a.id-ceq$g.executorId){$m.commandObject}else{$a.publicMoveObject})
   Assert-KmcOrdinaryInteger $b.publicMoveObject $expected $expected
   if($a.id-ceq$g.executorId){
    $issued=@(@($b.raw)+@($b.queue)|Where-Object object -EQ $m.commandObject)
    if($issued.Count-lt1){throw 'Six-pair raw custody omitted the issued movement command.'}
    foreach($command in $issued){Assert-KmcChunk7Boolean $command.createdByPlayer $true}
   }
  }
  Assert-KmcChunk7Boolean $m.commandTerminal $true;Assert-KmcChunk7Boolean $m.otherCommandsUnchangedAtInput $true
  Assert-KmcChunk7WorldInt32 $m.commandObject;if($m.commandObject-eq0-or-not$commands.Add([long]$m.commandObject)){throw 'Six-pair movement reuses a native command identity.'}
  Assert-KmcBootstrapInventoryString $m.selectionAfterInput $roles[(($i+1)%6)*2]
  foreach($set in @($m.commandsBefore,$m.commandsAfter)){Assert-KmcChunk7ExactIds $set $roles;foreach($c in $set){Assert-KmcChunk7WorldInt32 $c.moveObject}}
  foreach($before in $m.commandsBefore){$after=@($m.commandsAfter|Where-Object id -CEQ $before.id)[0]
   if($before.id-ceq$m.executorId){if($after.moveObject-ne$m.commandObject-or$before.moveObject-eq$after.moveObject){throw 'Six-pair executor lacks its exact new movement command.'}}
   elseif($after.moveObject-ne$before.moveObject){throw 'Six-pair movement changed a neighboring command.'}}
  $distance=0.0;$residual=0.0;foreach($point in @($m.start,$m.end,$m.requestedGoal)){Assert-KmcAssistedObjectValue $point @('x','y','z') 'six-pair native point';foreach($axis in @('x','y','z')){if(-not(Test-KmcChunk7FiniteNumber $point.$axis)){throw 'Six-pair movement lacks finite coordinates.'}}}
  foreach($axis in @('x','y','z')){$distance+=[Math]::Pow(([double]$m.end.$axis-$m.start.$axis),2);$residual+=[Math]::Pow(([double]$m.end.$axis-$m.requestedGoal.$axis),2)}
  if([Math]::Sqrt($distance)-lt.75-or[Math]::Sqrt($residual)-gt1.0){throw 'Six-pair ground movement missed its fixed endpoint bound.'}
  Assert-KmcChunk7QuietWindow $m.optionalCaptureWindow $m @($mounted[0].owners) $roles 6
 }
 if($v3){Assert-KmcChunk7SixPairWanderClearGoals $O $roles}else{Assert-KmcChunk7SixPairGroundGoals $O $roles}
 Assert-KmcAssistedArray $s.samples 15 15 'six-pair repeated native measurement windows'
 $pinVector=$null;$lastFrame=-1;$retainedOwners=@{}
 for($i=0;$i-lt15;$i++){
  $row=$s.samples[$i];$group=[int][Math]::Floor($i/3);$window=$i%3;$active=@(0,1,2,6,0)[$group]
  Assert-KmcExactProperties $row @('group','window','activePairs','before','after','sample') 'six-pair measured row'
  foreach($pair in @(@('group',$group),@('window',$window),@('activePairs',$active))){Assert-KmcOrdinaryInteger $row.($pair[0]) $pair[1] $pair[1]}
  Assert-KmcMatchedPoolSample $row.sample ('six-state-'+$group+'-window-'+$window) $active $true $false 6
  if($row.sample.startFrame-le$lastFrame){throw 'Six-pair native measurement windows overlap or replay.'};$lastFrame=$row.sample.endFrame
  foreach($key in @('kmcLogMessages','kmcLogCharacters')){Assert-KmcOrdinaryInteger $row.sample.$key 0 0}
  foreach($phase in @('before','after')){
   $b=$row.$phase;Assert-KmcExactProperties $b @('world','identityPins','activePairs','allocationActors') 'six-pair measured native boundary'
   Assert-KmcOrdinaryInteger $b.activePairs $active $active;Assert-KmcOrdinaryInteger $b.allocationActors 0 64
   Assert-KmcAssistedArray $b.identityPins 54 54 'six-pair exact world/resident/actor pins'
   $pins=[Collections.Generic.HashSet[int]]::new();foreach($pin in $b.identityPins){Assert-KmcManualSeedObjectIdentity $pin;if(-not$pins.Add([int]$pin)){throw 'Six-pair measurement aliases native identity pins.'}}
   if($null-eq$pinVector){$pinVector=$b.identityPins}else{Assert-KmcAssistedSame $b.identityPins $pinVector 'six-pair retained resident/native identities'}
   Assert-KmcChunk7SixPairWorld $b.world $active $roles
   if((Get-KmcChunk7Subscriptions $b.world.subscriptions)-cne$subscriptions){throw 'Six-pair measurement changed the warmed native subscription population.'}
   $wanted=@(for($j=0;$j-lt$active;$j++){$roles[$j*2]})
   Assert-KmcChunk7ExactIds $b.world.owners $wanted 'riderId' -GuidValue
   if($active-gt0){foreach($owner in $b.world.owners){$key=[string]$owner.riderId
    if($retainedOwners.ContainsKey($key)){Assert-KmcAssistedSame $owner $retainedOwners[$key] 'six-pair neighboring owner remains exact across partial mounts'}else{$retainedOwners[$key]=$owner}}}
  }
  Assert-KmcAssistedSame $row.before.world.owners $row.after.world.owners 'six-pair sample stable owners'
  Assert-KmcAssistedSame $row.before.world.selection $row.after.world.selection 'six-pair sample stable selection'
  Assert-KmcChunk7Resources $row.before.world.resources $row.after.world.resources $roles
  foreach($a in $row.before.world.resources.actors){$b=@($row.after.world.resources.actors|Where-Object id -CEQ $a.id)[0];Assert-KmcOrdinaryInteger $a.reactions 0 ([int]::MaxValue);Assert-KmcOrdinaryInteger $b.reactions $a.reactions $a.reactions}
 }
 foreach($key in @('pairResidueAbsent','inheritedPetsPreserved','controlsSettled','settingsAndSelectionRestored','nativeProcessesSettled','cleanupCustodySettled')){Assert-KmcChunk7Boolean $O.cleanup.$key $true}
 foreach($key in @('horseRemoved','ordinaryRespecProven','nativeSaveWritten','nativeProgressionRequiresExternalWorkingRestoration')){Assert-KmcChunk7Boolean $O.cleanup.$key $false}
 Assert-KmcChunk7SixPairWorld $O.cleanup.world 0 $roles
 Assert-KmcAssistedSame $O.cleanup.selectionAfter $O.cleanup.selectionBefore 'six-pair restores original native selection'
 foreach($key in @('nativeWrite','saveRequested','thirdHorseSetup','ownerSeedSetup','ownerSeedManual','door','screenshots')){if($null-ne$O.PSObject.Properties[$key]){throw 'Six-pair stress carries an undeclared setup, persistence or door observation.'}}
}
