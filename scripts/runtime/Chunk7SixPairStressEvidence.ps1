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
function Assert-KmcChunk7SixPairStressEvidence($Request,$O){
 Assert-KmcChunk7OwnedPairsDescriptor $Request
 Assert-KmcChunk7InheritedEvidence $Request $O
 Assert-KmcAssistedSame $O.inheritedFixture.remote $O.inheritedCleanup.remote 'six-pair remote native identity/progression retention'
 $roles=@(Get-KmcChunk7SixPairRoles);$s=$O.sixPairStress
 # F2 r39: the guard records a combat entry only on its refused path; PASS evidence never carries one.
 foreach($key in @('sixPairCombatEntry','sixPairCombatEntryError')){if($null-ne$O.PSObject.Properties[$key]){throw ('Six-pair evidence carries its refused-path record '+$key+'.')}}
 # Producers from b210 on record the before-stress clearance; earlier accepted evidence predates it.
 if($null-ne$O.PSObject.Properties['sixPairHostileClearance']){Assert-KmcChunk7SixPairHostileClearance $O.sixPairHostileClearance}
 # v1 (batch186-188) verified full inherited custody on every measured frame; v2 verifies it
 # at sampling-window boundaries and declares that its measured frames carry only the guard.
 $fields=@('contract','archiveProducerRunId','coldQualificationRunId','fixtureSetupWrites','nativeSaveRequests','nativeLoadRequests',
  'requiredCycles','requiredGroundInputs','requiredAbilityInputs','requiredWindows','windowsPerState','performanceAccepted','requestFreeControlsProven',
  'identityScopeReleased','cycles','samples','nativeCastRequests','nativeRefusals','saveSnapshots','semanticRestores','presentationRestores',
  'riderParents','riderParentChildren','siblingOrderVerifiedCycles')
 $v2=$null-ne$s.PSObject.Properties['contract']-and$s.contract-is[string]-and$s.contract-ceq'cold-qualified-six-pair-lifecycle-v2'
 if($v2){$fields+='measuredFramesCarryFullCustody'}
 Assert-KmcExactProperties $s $fields 'six-pair native lifecycle observation'
 foreach($pair in @(@('contract',$(if($v2){'cold-qualified-six-pair-lifecycle-v2'}else{'cold-qualified-six-pair-lifecycle-v1'})),@('archiveProducerRunId','20261022-c7b-world184-b184s1-sixthhorse-manual'),@('coldQualificationRunId',$Request.ownedPairs.sourceRunId))){Assert-KmcBootstrapInventoryString $s.($pair[0]) $pair[1]}
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
