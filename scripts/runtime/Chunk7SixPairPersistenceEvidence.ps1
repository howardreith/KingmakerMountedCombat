Set-StrictMode -Version Latest
# The six-mounted native Manual producer and its separate-process cold consumer. This
# closed branch sits beside the historical two-pair persistence reader, whose graph,
# budget, input and continuation assertions stay unchanged. Source* always pins the
# batch185 cold qualification of the unmounted Manual308; only the consumer's
# Checkpoint* names the mounted producer run and its persistence evidence.
function Test-KmcChunk7SixPairSave($Request){(Get-KmcChunk7OwnedPairsCase $Request)-ceq'six-horse-mounted-persistence-save'}
function Test-KmcChunk7SixPairLoad($Request){(Get-KmcChunk7OwnedPairsCase $Request)-ceq'six-horse-mounted-persistence-load'}
function Test-KmcChunk7SixPairPersistence($Request){(Test-KmcChunk7SixPairSave $Request)-or(Test-KmcChunk7SixPairLoad $Request)}
# Every kind whose fixture is the twelve-actor cold-qualified six-Horse field.
function Test-KmcChunk7SixPairLineage($Request){(Test-KmcChunk7SixPairStress $Request)-or(Test-KmcChunk7SixPairPersistence $Request)}
function Get-KmcChunk7SixPairManualName {'KMC_C7_SIX_MOUNTED_WORLD'}
function Get-KmcChunk7SixPairManualLeaf {'Manual_309_KMC_C7_SIX_MOUNTED_WORLD.zks'}
function Get-KmcChunk7SixPairProfile([int]$Index){if($Index-cin@(1,4,5)){'small-default-halfling-horse-v1'}else{'medium-humanoid-horse-v1'}}
function Assert-KmcChunk7SixPairPersistenceSelection($Request,$Selection,$Checkpoint){
 Assert-KmcExactProperties $Selection @('case','sourceRunId','sourceArtifactSha256','sourceArchiveSha256',
  'checkpointSourceRunId','checkpointSourceArtifactSha256','riderAId','mountAId','riderBId','mountBId',
  'partyIds','baseGameOnly','expectedNativeLoadCount','expectedNativeWriteCount') 'six-pair mounted persistence selection'
 $roles=@(Get-KmcChunk7SixPairRoles);$cold=Test-KmcChunk7SixPairLoad $Request
 Assert-KmcBootstrapInventoryString $Checkpoint 'multi-exploration-manual'
 foreach($pair in @(@('sourceRunId','20261022-c7b-world185-b185s1-sixthhorse-cold'),
  @('sourceArtifactSha256','012d1caff175d34614fcd255f0b6ad897e0ef1a9650791cb3c0d55ce13b47241'),
  @('sourceArchiveSha256','b1e817a60c8bbe35b5947b4eab3b3f35060493ca20873d5de5fdf8a81efbc291'),
  @('riderAId',$roles[0]),@('mountAId',$roles[1]),@('riderBId',$roles[2]),@('mountBId',$roles[3]))){Assert-KmcBootstrapInventoryString $Selection.($pair[0]) $pair[1]}
 if($Request.runId-isnot[string]-or$Request.runId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'-or$Request.runId-ceq$Selection.sourceRunId){throw 'Six-pair persistence requires a distinct bounded run identity.'}
 Assert-KmcAssistedArray $Selection.partyIds 12 12 'six-pair complete field roster'
 Assert-KmcAssistedSame @($Selection.partyIds|Sort-Object -CaseSensitive) @($roles|Sort-Object -CaseSensitive) 'six-pair exact role set'
 if($cold){
  if($Selection.checkpointSourceRunId-isnot[string]-or$Selection.checkpointSourceRunId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'-or
     $Selection.checkpointSourceRunId-cin@($Request.runId,$Selection.sourceRunId)-or
     $Selection.checkpointSourceArtifactSha256-isnot[string]-or$Selection.checkpointSourceArtifactSha256-cnotmatch'^[a-f0-9]{64}$'-or
     $Selection.checkpointSourceArtifactSha256-cin@($Selection.sourceArtifactSha256,$Selection.sourceArchiveSha256)){throw 'Six-pair cold consumer requires a distinct exact six-mounted producer pin.'}
 }elseif($null-ne$Selection.checkpointSourceRunId-or$null-ne$Selection.checkpointSourceArtifactSha256){throw 'Six-pair Manual producer cannot borrow a checkpoint source.'}
 Assert-KmcChunk7Boolean $Selection.baseGameOnly $true;Assert-KmcOrdinaryInteger $Selection.expectedNativeLoadCount 1 1
 $writes=if($cold){0}else{1};Assert-KmcOrdinaryInteger $Selection.expectedNativeWriteCount $writes $writes
 if($null-ne$Request.PSObject.Properties['f11AllocationProbe']-and$null-ne$Request.f11AllocationProbe){throw 'Six-pair persistence does not select native allocation instrumentation.'}
}
function Assert-KmcChunk7SixPairPersistenceDescriptor($Request){
 if(Test-KmcChunk7SixPairSave $Request){Assert-KmcChunk7SixPairStressDescriptor $Request;return}
 if(-not(Test-KmcChunk7SixPairLoad $Request)){throw 'Six-pair persistence descriptor belongs only to its declared cases.'}
 $d=$Request.persistenceLoad;$s=$Request.ownedPairs
 foreach($pair in @(@('internalName',(Get-KmcChunk7SixPairManualName)),@('fileName',(Get-KmcChunk7SixPairManualLeaf)),
  @('gameId','3f7381f7-aba6-43bf-bae6-39c6a66a7180'),@('gameName','Testy the Modless'),@('area','b10e86516b6437d45a629931c30936fe'))){Assert-KmcBootstrapInventoryString $d.($pair[0]) $pair[1]}
 if($d.sha256-isnot[string]-or$d.sha256-cnotmatch'^[a-f0-9]{64}$'-or
    $d.sha256-cin@($s.sourceArchiveSha256,$s.sourceArtifactSha256,$s.checkpointSourceArtifactSha256,$Request.fixture.working.sha256,$Request.fixture.baseline.sha256)){
  throw 'Six-pair cold input aliases its fixture, producer receipt or a protected archive.'}
 Assert-KmcOrdinaryInteger $d.length 1 268435456;Assert-KmcOrdinaryInteger $d.lastWriteTimeUtcTicks 1 ([DateTime]::MaxValue.Ticks)
}
function Assert-KmcChunk7SixPairArchive($Archive,$Request){
 Assert-KmcExactProperties $Archive @('SchemaVersion','CampaignId','AreaId','GameTimeTicks','Policy','RulesId','Pairs','Participations','Slots','Combat') 'six-pair native archive'
 if(-not(Test-KmcExactJsonInteger $Archive.SchemaVersion)-or$Archive.SchemaVersion-ne3-or$Archive.CampaignId-cne$Request.persistenceLoad.gameId-or
    $Archive.AreaId-cne$Request.persistenceLoad.area-or-not(Test-KmcExactJsonInteger $Archive.GameTimeTicks)-or$Archive.GameTimeTicks-lt0-or
    $Archive.Policy-cne'rider-principal-distinct-native-v1'-or$Archive.RulesId-cne'crpg-transport-v1'-or
    $Archive.Pairs-isnot[Array]-or$Archive.Pairs.Count-ne6-or$Archive.Participations-isnot[Array]-or$Archive.Participations.Count-ne0-or
    $null-ne$Archive.Combat-or$Archive.Slots-isnot[Array]-or$Archive.Slots.Count-ne12){
  throw 'Six-pair exploration archive omitted owners, invented combat/retained state or changed version/policy/slots.'}
 $roles=@(Get-KmcChunk7SixPairRoles);$owners=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
 $actors=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
 foreach($pair in $Archive.Pairs){
  Assert-KmcExactProperties $pair @('PairId','Generation','ProfileId','Rider','Mount') 'six-pair native pair'
  $owner=[Guid]::Empty
  if(-not[Guid]::TryParse([string]$pair.PairId,[ref]$owner)-or$owner-eq[Guid]::Empty-or-not$owners.Add($owner.ToString('N'))-or
     -not(Test-KmcExactJsonInteger $pair.Generation)-or$pair.Generation-le0){throw 'Six-pair stable native owner identity or generation invalid.'}
  foreach($actor in @($pair.Rider,$pair.Mount)){Assert-KmcChunk7PersistenceNativeActor $actor;if(-not$actors.Add(([Guid]$actor.Id).ToString('N'))){throw 'Six-pair actor has duplicate or cross-role mounted custody.'}}
 }
 for($i=0;$i-lt6;$i++){
  $rows=@($Archive.Pairs|Where-Object{$_.Rider.Id-ceq$roles[$i*2]})
  if($rows.Count-ne1-or$rows[0].Mount.Id-cne$roles[$i*2+1]){throw 'Six-pair archive substituted a sourced rider/mount role.'}
  Assert-KmcBootstrapInventoryString $rows[0].ProfileId (Get-KmcChunk7SixPairProfile $i)
 }
 $riders=@(for($i=0;$i-lt6;$i++){$roles[$i*2]});$slotIds=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
 foreach($slot in $Archive.Slots){
  Assert-KmcExactProperties $slot @('ActorId','Index','Kind') 'six-pair native slot'
  if($slot.ActorId-cnotin$riders-or-not(Test-KmcExactJsonInteger $slot.Index)-or$slot.Index-lt0-or$slot.Index-gt127-or
     -not(Test-KmcExactJsonInteger $slot.Kind)-or$slot.Kind-cnotin@(2,3)-or-not$slotIds.Add($slot.ActorId+':'+$slot.Index)){throw 'Six-pair owned native control slot invalid or duplicated.'}
 }
 foreach($rider in $riders){
  $kinds=@($Archive.Slots|Where-Object ActorId -CEQ $rider|ForEach-Object{$_.Kind}|Sort-Object)
  if(($kinds-join',')-cne'2,3'){throw 'Each six-pair rider must retain exactly its own Dismount and Rider Primary.'}
 }
}
# Owners, generations and actor mapping equal the archive. Debt is compared only where
# the label is the archive boundary itself.
function Assert-KmcChunk7SixPairSnapshot($Snapshot,$Archive,[string]$Label,[bool]$Debt){
 if($Snapshot.label-cne$Label-or$Snapshot.owners-isnot[Array]-or$Snapshot.owners.Count-ne6-or$Snapshot.actors-isnot[Array]-or$Snapshot.actors.Count-ne12){throw 'Six-pair native snapshot incomplete.'}
 Assert-KmcChunk7NativeCounters $Snapshot @('semanticRestores','presentationRestores','nativePostLoads','snapshots','nativeCastRequests')
 Assert-KmcChunk7NativeCounters $Snapshot.controls @('DuplicateFactCount','ManagedHotbarSlotCount')
 Assert-KmcChunk7Boolean $Snapshot.controls.SerializationSuspended $false
 foreach($owner in $Snapshot.owners){Assert-KmcChunk7NativeCounters $owner @('generation')}
 foreach($pair in $Archive.Pairs){
  $row=@($Snapshot.owners|Where-Object{([Guid]$_.ownerId)-eq([Guid]$pair.PairId)})
  if($row.Count-ne1-or$row[0].generation-ne$pair.Generation-or$row[0].riderId-cne$pair.Rider.Id-or$row[0].mountId-cne$pair.Mount.Id-or$row[0].state-cne'Active'){
   throw 'Six-pair stable owner, generation or native actor mapping changed across the archive boundary.'}
 }
 if($Snapshot.controls.DuplicateFactCount-ne0-or$Snapshot.controls.ManagedHotbarSlotCount-ne$Archive.Slots.Count){throw 'Six-pair native control restoration differs.'}
 foreach($row in $Snapshot.actors){Assert-KmcChunk7Boolean $row.inCombat $false}
 if(-not$Debt){return}
 if(-not(Test-KmcExactJsonInteger $Snapshot.gameTicks)-or$Snapshot.gameTicks-lt$Archive.GameTimeTicks){throw 'Six-pair native clock precedes the saved snapshot.'}
 $elapsed=([double]$Snapshot.gameTicks-[double]$Archive.GameTimeTicks)/10000000.0
 if($elapsed-gt30){throw 'Six-pair debt comparison escaped its bounded observation window.'}
 foreach($a in @($Archive.Pairs|ForEach-Object{$_.Rider;$_.Mount})){
  $row=@($Snapshot.actors|Where-Object{$_.native.Id-ceq$a.Id})
  if($row.Count-ne1){throw 'Six-pair native actor debt snapshot omitted or duplicated an actor.'}
  $b=$row[0].native;Assert-KmcChunk7PersistenceNativeActor $b
  foreach($field in @('Standard','Move','Swift','Initiative','Reaction')){
   if([double]$b.$field-gt[double]$a.$field+0.001-or[double]$b.$field+$elapsed+0.001+($elapsed*0.1)-lt[double]$a.$field){throw 'Six-pair native cooldown changed beyond elapsed native clock progress.'}
  }
  if($a.ReactionsRemaining-ne$b.ReactionsRemaining-or$a.LastSurpriseTicks-ne$b.LastSurpriseTicks){throw 'Six-pair native reaction/surprise history changed across the boundary.'}
 }
}
function Get-KmcChunk7SixPairOwnerRows($Archive){
 $roles=@(Get-KmcChunk7SixPairRoles)
 @(for($i=0;$i-lt6;$i++){
  $pair=@($Archive.Pairs|Where-Object{$_.Rider.Id-ceq$roles[$i*2]})[0]
  [pscustomobject][ordered]@{index=$i;riderId=$pair.Rider.Id;mountId=$pair.Mount.Id;ownerId=([Guid]$pair.PairId).ToString('N');generation=$pair.Generation;profileId=$pair.ProfileId}
 })
}
function Assert-KmcChunk7SixPairOwnerRows($Rows,$Archive){
 Assert-KmcAssistedArray $Rows 6 6 'six-pair recorded owners'
 foreach($row in $Rows){Assert-KmcExactProperties $row @('index','riderId','mountId','ownerId','generation','profileId') 'six-pair recorded owner'}
 Assert-KmcAssistedSame $Rows @(Get-KmcChunk7SixPairOwnerRows $Archive) 'six-pair recorded owner roles, generations and profiles'
}
function Assert-KmcChunk7SixPairMovements($Observations){
 $roles=@(Get-KmcChunk7SixPairRoles);$moves=@($Observations.movements)
 if($moves.Count-ne6){throw 'Every six-pair restored/live pair requires one native ground continuation.'}
 for($i=0;$i-lt6;$i++){
  $move=$moves[$i]
  Assert-KmcExactProperties $move @('label','selectedId','executorId','otherCommandsUnchanged','start','goal','end','terminal','distance','residual') 'six-pair native continuation'
  Assert-KmcBootstrapInventoryString $move.label ('six-pair-'+$i);Assert-KmcBootstrapInventoryString $move.selectedId $roles[$i*2];Assert-KmcBootstrapInventoryString $move.executorId $roles[$i*2+1]
  Assert-KmcChunk7Boolean $move.terminal $true;Assert-KmcChunk7Boolean $move.otherCommandsUnchanged $true
  foreach($point in @($move.start,$move.goal,$move.end)){Assert-KmcExactProperties $point @('x','y','z') 'six-pair continuation point';foreach($axis in @('x','y','z')){if(-not(Test-KmcChunk7FiniteNumber $point.$axis)){throw 'Six-pair continuation point is not finite.'}}}
  if([Math]::Abs([double]$move.goal.x-[double]$move.start.x-2)-gt0.001-or[Math]::Abs([double]$move.goal.y-[double]$move.start.y)-gt0.001-or[Math]::Abs([double]$move.goal.z-[double]$move.start.z)-gt0.001){throw 'Six-pair continuation goal is not its fixed two-metre offset.'}
  $distance=[Math]::Sqrt([Math]::Pow([double]$move.end.x-$move.start.x,2)+[Math]::Pow([double]$move.end.y-$move.start.y,2)+[Math]::Pow([double]$move.end.z-$move.start.z,2))
  $residual=[Math]::Sqrt([Math]::Pow([double]$move.end.x-$move.goal.x,2)+[Math]::Pow([double]$move.end.y-$move.goal.y,2)+[Math]::Pow([double]$move.end.z-$move.goal.z,2))
  if(-not(Test-KmcChunk7FiniteNumber $move.distance)-or-not(Test-KmcChunk7FiniteNumber $move.residual)-or
     [Math]::Abs($distance-[double]$move.distance)-gt0.0001-or[Math]::Abs($residual-[double]$move.residual)-gt0.0001-or$distance-lt0.75-or$residual-gt1){throw 'Six-pair native movement missed the unchanged measured endpoint.'}
 }
}
function Assert-KmcChunk7SixPairMountInputs($Observations,$Archive){
 $roles=@(Get-KmcChunk7SixPairRoles);$inputs=@($Observations.inputs)
 if($inputs.Count-ne6){throw 'Six actual native Mount inputs are required.'}
 for($i=0;$i-lt6;$i++){
  $row=$inputs[$i]
  Assert-KmcExactProperties $row @('label','before','casterId','targetId','abilityGuid','clicked','nativeCastDelta','nativeRefusalDelta','generationAtInit','control','selectionAfterInput','terminal','after') 'six-pair native Mount input'
  Assert-KmcChunk7NativeCounters $row @('nativeCastDelta','nativeRefusalDelta','generationAtInit')
  if($row.label-cne('six-mount-'+$i)-or$row.casterId-cne$roles[$i*2]-or$row.targetId-cne$roles[$i*2+1]-or$row.abilityGuid-cne'f053faad986631688defa003cd7bda0e'-or
     $row.nativeCastDelta-ne1-or$row.nativeRefusalDelta-ne0-or$row.control-isnot[string]-or[string]::IsNullOrEmpty($row.control)-or
     $row.selectionAfterInput-cne$roles[(($i+1)%6)*2]){throw 'Six-pair native Mount input lost exact actor/control/generation/selection binding.'}
  Assert-KmcChunk7Boolean $row.clicked $true;Assert-KmcChunk7Boolean $row.terminal $true
  # Each input mints exactly one owner in sourced role order; none is reminted later.
  if($row.before.label-cne'input-before'-or$row.after.label-cne'input-after'-or@($row.before.owners).Count-ne$i-or@($row.after.owners).Count-ne($i+1)){throw 'Six-pair Mount did not add exactly one owner in sourced order.'}
  $minted=@($row.after.owners|Where-Object riderId -CEQ $roles[$i*2]);$saved=@($Archive.Pairs|Where-Object{$_.Rider.Id-ceq$roles[$i*2]})
  if($minted.Count-ne1-or$minted[0].mountId-cne$roles[$i*2+1]-or$minted[0].state-cne'Active'-or
     ([Guid]$minted[0].ownerId)-ne([Guid]$saved[0].PairId)-or$minted[0].generation-ne$saved[0].Generation){throw 'Six-pair Mount owner was reminted before the save.'}
 }
}
function Assert-KmcChunk7SixPairColdRestoration($Observations,$Archive,$Request){
 $probe=$Observations.coldRestoration
 Assert-KmcChunk7NativeCounters $probe @('maximumEvents','dropped','errors')
 Assert-KmcChunk7Boolean $probe.complete $true;Assert-KmcChunk7Boolean $probe.installedBeforeWorldLoad $true
 if($probe.contract-cne'chunk7-native-restoration-entry-observer-v1'-or$probe.maximumEvents-ne2048-or$probe.dropped-ne0-or$probe.errors-ne0-or
    $probe.prepareMetric-cne'native-method-entries-including-production-refused-attempts'-or@($probe.hooks).Count-ne4){throw 'Six-pair cold native preparation observer incomplete or wrong.'}
 $ids=@($Archive.Pairs|ForEach-Object{$_.Rider.Id;$_.Mount.Id})
 if(@($probe.events|Where-Object{$_.actorId-cin$ids-and$_.kind-cin@('prepare-before','round-before','restore-actor-before','restore-actor-after')}).Count-ne0){throw 'Six-pair cold load entered native preparation, a new round or combat actor restoration.'}
 foreach($id in $ids){foreach($kind in @('restore-debt-before','restore-debt-after')){
  if(@($probe.events|Where-Object{$_.actorId-ceq$id-and$_.kind-ceq$kind}).Count-ne1){throw 'Six-pair native debt restore was not observed once for each actor.'}
 }}
 $prepare=@($probe.hooks|Where-Object{$_.declaringType-ceq'TurnBased.Controllers.TurnController'-and$_.method-ceq'Prepare'-and$_.token-ceq'06000C3C'})
 if($prepare.Count-ne1-or$prepare[0].moduleMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'){throw 'Six-pair preparation hook differs from the installed native contract.'}
 if(@($probe.hooks|Where-Object{$_.token-ceq'0600939D'-and$_.moduleMvid-ceq'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'}).Count-ne1){throw 'Six-pair native round observer token differs.'}
 foreach($method in @('RestoreActor','RestoreDebt')){
  if(@($probe.hooks|Where-Object{$_.declaringType-ceq'KingmakerMountedCombat.Integration.NativeCombatActorPersistence'-and$_.method-ceq$method-and$_.moduleMvid-ceq$Request.dllMvid}).Count-ne1){throw 'Six-pair supplemental restore observer is not bound to the exact candidate method.'}
 }
 foreach($saved in @($Archive.Pairs|ForEach-Object{$_.Rider;$_.Mount})){
  $after=@($probe.events|Where-Object{$_.actorId-ceq$saved.Id-and$_.kind-ceq'restore-debt-after'})[0]
  if($after.restorePhase-cne'validated-before-native-mutation'-or(ConvertTo-KmcChunk7Canonical $after.debt)-cne(ConvertTo-KmcChunk7Canonical $saved)){throw 'Six-pair post-restore native debt differs from the whole-graph validated source.'}
 }
}
function Assert-KmcChunk7SixPairPersistenceObservation($Request,$Artifact){
 if(-not(Test-KmcChunk7SixPairPersistence $Request)){throw 'Six-pair persistence evidence belongs only to its declared cases.'}
 if($Artifact.status-cne'PASS'){throw 'Native six-pair persistence artifact failed.'}
 $o=$Artifact.observations;$cold=Test-KmcChunk7SixPairLoad $Request
 $common=@('checkpoint','cold','sourceWorkingSha256','inheritedManualSha256','processId','snapshots','inputs','movements',
  'nativeProgressionRequiresExternalWorkingRestoration','ordinaryRespecProven','combatDebtProof','nativeSchema','nativeCapacity',
  'inheritedFixture','sixPairPersistence','inheritedCleanup','cleanup','elapsedSeconds','nativeShellLifecycle')
 $fields=if($cold){$common+@('coldRestoration','loadedArchive','loadedDescriptor')}else{$common+@('ownedOutputAdmission','saveRequested','ownedOutputCompletion','nativeWrite')}
 Assert-KmcExactProperties $o $fields 'six-pair persistence observations'
 $expectedScenario=if($cold){'chunk7-persistence-load'}else{'chunk7-persistence-save'}
 Assert-KmcBootstrapInventoryString (Get-KmcChunk7SelectedScenario $Request) $expectedScenario
 Assert-KmcBootstrapInventoryString $o.checkpoint 'multi-exploration-manual';Assert-KmcChunk7Boolean $o.cold $cold
 if($null-ne$o.sourceWorkingSha256){throw 'Six-pair persistence cannot claim historical Working fixture identity.'}
 Assert-KmcBootstrapInventoryString $o.inheritedManualSha256 $Request.ownedPairs.sourceArchiveSha256
 Assert-KmcOrdinaryInteger $o.processId 1 ([int]::MaxValue);Assert-KmcOrdinaryInteger $o.nativeSchema 3 3;Assert-KmcOrdinaryInteger $o.nativeCapacity 6 6
 foreach($key in @('nativeProgressionRequiresExternalWorkingRestoration','ordinaryRespecProven','combatDebtProof')){Assert-KmcChunk7Boolean $o.$key $false}
 # The engine bounds its scenario at 300 s; a settled cleanup adds at most its 45 s retention window.
 if(-not(Test-KmcChunk7FiniteNumber $o.elapsedSeconds)-or$o.elapsedSeconds-lt0-or$o.elapsedSeconds-gt360){throw 'Six-pair persistence exceeded its bounded scenario window.'}
 Assert-KmcChunk7InheritedEvidence $Request $o
 $archive=if($cold){$o.loadedArchive}else{$o.nativeWrite.archive}
 Assert-KmcChunk7SixPairArchive $archive $Request
 $s=$o.sixPairPersistence
 if($cold){
  Assert-KmcExactProperties $s @('contract','fixtureRunId','producerRunId','producerArtifactSha256','requiredPairs','requiredSlots','requiredGroundInputs',
   'nativeLoadRequests','nativeSaveRequests','semanticRestores','presentationRestores','nativePostLoads','owners','performanceAccepted',
   'requestFreeControlsProven','nativeCastRequests','nativeRefusals','groundInputs') 'six-pair mounted Manual cold consumer'
  Assert-KmcBootstrapInventoryString $s.contract 'six-horse-mounted-manual-cold-v1'
  Assert-KmcBootstrapInventoryString $s.producerRunId $Request.ownedPairs.checkpointSourceRunId
  Assert-KmcBootstrapInventoryString $s.producerArtifactSha256 $Request.ownedPairs.checkpointSourceArtifactSha256
  foreach($pair in @(@('nativeSaveRequests',0),@('semanticRestores',12),@('presentationRestores',6),@('nativePostLoads',12),@('nativeCastRequests',0))){Assert-KmcOrdinaryInteger $s.($pair[0]) $pair[1] $pair[1]}
 }else{
  Assert-KmcExactProperties $s @('contract','fixtureRunId','requiredPairs','requiredSlots','requiredMountInputs','requiredGroundInputs',
   'nativeLoadRequests','nativeSaveRequests','fixtureSetupWrites','performanceAccepted','requestFreeControlsProven','managedHotbarSlotsBeforeBinding',
   'savedPairs','savedSlots','owners','nativeCastRequestsBeforeSave','nativeRefusalsBeforeSave','nativeCastRequests','nativeRefusals','groundInputs') 'six-pair mounted Manual producer'
  Assert-KmcBootstrapInventoryString $s.contract 'six-horse-mounted-manual-producer-v1'
  foreach($pair in @(@('requiredMountInputs',6),@('nativeSaveRequests',1),@('fixtureSetupWrites',0),@('managedHotbarSlotsBeforeBinding',0),@('savedPairs',6),@('savedSlots',12),
   @('nativeCastRequestsBeforeSave',6),@('nativeRefusalsBeforeSave',0),@('nativeCastRequests',6))){Assert-KmcOrdinaryInteger $s.($pair[0]) $pair[1] $pair[1]}
 }
 Assert-KmcBootstrapInventoryString $s.fixtureRunId $Request.ownedPairs.sourceRunId
 foreach($pair in @(@('requiredPairs',6),@('requiredSlots',12),@('requiredGroundInputs',6),@('nativeLoadRequests',1),@('nativeRefusals',0),@('groundInputs',6))){Assert-KmcOrdinaryInteger $s.($pair[0]) $pair[1] $pair[1]}
 foreach($key in @('performanceAccepted','requestFreeControlsProven')){Assert-KmcChunk7Boolean $s.$key $false}
 Assert-KmcChunk7SixPairOwnerRows $s.owners $archive
 $snapshots=@($o.snapshots);$labels=if($cold){@('cold-restored-before-input','continuation-complete')}else{@('before-native-save','native-write-complete','continuation-complete')}
 if($snapshots.Count-ne$labels.Count-or(($snapshots|ForEach-Object{$_.label})-join',')-cne($labels-join',')){throw 'Six-pair persistence snapshots differ from their declared boundaries.'}
 $boundary=if($cold){'cold-restored-before-input'}else{'native-write-complete'}
 foreach($snapshot in $snapshots){Assert-KmcChunk7SixPairSnapshot $snapshot $archive $snapshot.label ($snapshot.label-ceq$boundary)}
 if($cold){
  $c=$snapshots[0]
  if((ConvertTo-KmcChunk7Canonical $o.loadedDescriptor)-cne(ConvertTo-KmcChunk7Canonical $Request.persistenceLoad)-or
     $c.semanticRestores-ne12-or$c.presentationRestores-ne6-or$c.nativePostLoads-ne12-or$c.snapshots-ne0-or$c.nativeCastRequests-ne0-or
     $c.archivePhase-cne'all-pair-presentation-restored'-or@($o.inputs).Count-ne0){throw 'Six-pair cold load substituted its source or replayed a Mount.'}
  Assert-KmcChunk7SixPairColdRestoration $o $archive $Request
 }else{
  Assert-KmcChunk7OwnedOutputEvidence $Request $o
  Assert-KmcExactProperties $o.saveRequested @('nativeType','nativeName','gameTicks','snapshotCount','nativeSaveAllowed','newSlot') 'six-pair explicit native save request'
  Assert-KmcBootstrapInventoryString $o.saveRequested.nativeType 'Manual';Assert-KmcBootstrapInventoryString $o.saveRequested.nativeName (Get-KmcChunk7SixPairManualName)
  Assert-KmcOrdinaryInteger $o.saveRequested.snapshotCount 0 0;Assert-KmcOrdinaryInteger $o.saveRequested.gameTicks 0
  foreach($key in @('nativeSaveAllowed','newSlot')){Assert-KmcChunk7Boolean $o.saveRequested.$key $true}
  $w=$o.nativeWrite
  Assert-KmcExactProperties $w @('path','sha256','length','nativeType','nativeName','nativeCallback','operation','snapshotCount','archive') 'six-pair native Manual write'
  Assert-KmcBootstrapInventoryString $w.nativeType 'Manual';Assert-KmcBootstrapInventoryString $w.nativeName (Get-KmcChunk7SixPairManualName);Assert-KmcBootstrapInventoryString $w.operation 'None'
  Assert-KmcChunk7Boolean $w.nativeCallback $true;Assert-KmcOrdinaryInteger $w.snapshotCount 1 1;Assert-KmcOrdinaryInteger $w.length 1 268435456
  if($w.sha256-isnot[string]-or$w.sha256-cnotmatch'^[0-9a-f]{64}$'-or$w.sha256-cin@($Request.ownedPairs.sourceArchiveSha256,$Request.ownedPairs.sourceArtifactSha256)){throw 'Six-pair native Manual hash is malformed or aliases its fixture.'}
  Assert-KmcBootstrapInventoryString (Get-KmcChunk7OwnedOutputLeaf $Request $o) (Get-KmcChunk7SixPairManualLeaf)
  $before=$snapshots[0];$written=$snapshots[1]
  if($before.nativeCastRequests-ne6-or$before.snapshots-ne0-or$written.nativeCastRequests-ne6-or$written.snapshots-ne1-or
     $before.semanticRestores-ne0-or$before.presentationRestores-ne0){throw 'Six-pair producer replayed input, restored a graph or saved early.'}
  Assert-KmcChunk7SixPairMountInputs $o $archive
 }
 $end=$snapshots[$snapshots.Count-1]
 if($end.nativeCastRequests-ne$(if($cold){0}else{6})-or$end.snapshots-ne$(if($cold){0}else{1})){throw 'Six-pair continuation performed undeclared native input or saves.'}
 Assert-KmcChunk7SixPairMovements $o
 foreach($field in @('pairsClear','controlsSettled','saveCustodyClear','selectionRestored','inheritedPetsPreserved')){Assert-KmcChunk7Boolean $o.cleanup.$field $true}
 foreach($field in @('sourceHorseRemoved','nativeProgressionRequiresExternalWorkingRestoration','ordinaryRespecProven')){Assert-KmcChunk7Boolean $o.cleanup.$field $false}
 if(@($o.cleanup.world.owners).Count-ne0){throw 'Six-pair cleanup retained native custody.'}
}
# Separate-process cold proof: the consumer restored exactly the producer's read-back
# graph under the producer's owners, generations and slots, on the same candidate,
# with the same field roster, progression and remote companion.
function Assert-KmcChunk7SixPairMountedColdSource($Request,$ProducerArtifact,$ColdArtifact){
 Assert-KmcChunk7OwnedPairsDescriptor $Request
 if(-not(Test-KmcChunk7SixPairLoad $Request)){throw 'Six-pair mounted cold proof belongs only to its declared consumer.'}
 $s=$Request.ownedPairs;$p=$ProducerArtifact.observations;$c=$ColdArtifact.observations
 Assert-KmcBootstrapInventoryString $ProducerArtifact.runId $s.checkpointSourceRunId
 foreach($artifact in @($ProducerArtifact,$ColdArtifact)){Assert-KmcBootstrapInventoryString $artifact.status 'PASS'}
 foreach($key in @('commit','dllSha256','dllMvid')){Assert-KmcBootstrapInventoryString $ProducerArtifact.$key $Request.$key}
 Assert-KmcBootstrapInventoryString $p.checkpoint 'multi-exploration-manual';Assert-KmcBootstrapInventoryString $c.checkpoint 'multi-exploration-manual'
 Assert-KmcChunk7Boolean $p.cold $false;Assert-KmcChunk7Boolean $c.cold $true
 Assert-KmcBootstrapInventoryString $p.sixPairPersistence.contract 'six-horse-mounted-manual-producer-v1'
 Assert-KmcBootstrapInventoryString $c.sixPairPersistence.contract 'six-horse-mounted-manual-cold-v1'
 Assert-KmcBootstrapInventoryString $p.nativeWrite.sha256 $Request.persistenceLoad.sha256
 $producerRoot=Join-Path (Join-Path (Join-Path (Get-KmcLabRoot) 'runtime-staging') ('persistence-'+$s.checkpointSourceRunId)) 'Saved Games'
 Assert-KmcBootstrapInventoryString $p.nativeWrite.path (Join-Path $producerRoot (Get-KmcChunk7SixPairManualLeaf))
 foreach($fixture in @($p.inheritedFixture,$c.inheritedFixture)){Assert-KmcBootstrapInventoryString $fixture.sourceCase 'sixth-horse-manual-readonly-inventory'}
 Assert-KmcOrdinaryInteger $p.processId 1 ([int]::MaxValue);Assert-KmcOrdinaryInteger $c.processId 1 ([int]::MaxValue)
 if($p.processId-eq$c.processId-or(ConvertTo-KmcChunk7Canonical $p.nativeWrite.archive)-cne(ConvertTo-KmcChunk7Canonical $c.loadedArchive)){
  throw 'Six-pair mounted cold process did not restore the exact producer read-back graph.'}
 Assert-KmcAssistedSame $c.sixPairPersistence.owners $p.sixPairPersistence.owners 'six-pair restored owner identities and generations'
 foreach($key in @('partyIds','currentCharacterIds','remoteCapturedIds','arrangement')){
  Assert-KmcAssistedSame $c.inheritedFixture.$key $p.inheritedCleanup.$key 'six-pair cold field roster or progression'
 }
 Assert-KmcAssistedSame $c.inheritedFixture.remote.arrangement $p.inheritedCleanup.remote.arrangement 'six-pair cold remote Linzi'
 for($i=0;$i-lt6;$i++){foreach($key in @('riderId','mountId','sourceGuid','rank','profileId')){
  Assert-KmcAssistedSame $c.inheritedFixture.pairs[$i].$key $p.inheritedCleanup.pairs[$i].$key 'six-pair inherited pair provenance'
 }}
}
# Producer: the batch185 cold-qualified Manual308 gate itself. Consumer: the same gate
# for fixture provenance, plus the completed producer's receipts, re-read evidence and
# exact written Manual309 bytes.
function Get-KmcChunk7SixPairMountedSource($Selection,[AllowNull()][string]$PersistenceCase,$Fixture,$Candidate,$AreaTarget=$null){
 $run='unassigned-six-pair-persistence'
 $r=[pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';runId=$run;ownedPairs=$Selection;persistenceCase=$(if([string]::IsNullOrEmpty($PersistenceCase)){$null}else{$PersistenceCase})}
 Assert-KmcChunk7OwnedPairsSelection $r
 if(-not(Test-KmcChunk7SixPairPersistence $r)){throw 'Six-pair persistence source belongs only to its declared cases.'}
 if($null-ne$AreaTarget){throw 'Six-pair persistence admits no Areas source or destination.'}
 $fixtureSource=Get-KmcChunk7SixPairCheckpointSource $Fixture
 if(Test-KmcChunk7SixPairSave $r){
  $r|Add-Member persistenceLoad $fixtureSource.descriptor;$r|Add-Member fixture $Fixture;Assert-KmcChunk7OwnedPairsDescriptor $r
  $fixtureSource|Add-Member sixPairCheckpoint $true;$fixtureSource|Add-Member sourceRunId $fixtureSource.qualificationRunId
  return $fixtureSource
 }
 $set=Get-KmcChunk7OwnedReceiptSet $Selection.checkpointSourceRunId 'chunk7-persistence-evidence.json';$request=$set.request
 Assert-KmcBootstrapInventoryString $request.scenario 'chunk7-owned-two-pair-campaign'
 Assert-KmcBootstrapInventoryString $request.ownedPairs.case 'six-horse-mounted-persistence-save'
 Assert-KmcBootstrapInventoryString $request.persistenceCase 'multi-exploration-manual'
 Assert-KmcChunk7OwnedPairsDescriptor $request
 foreach($key in @('sourceRunId','sourceArtifactSha256','sourceArchiveSha256','riderAId','mountAId','riderBId','mountBId')){Assert-KmcBootstrapInventoryString $request.ownedPairs.$key $Selection.$key}
 if((ConvertTo-KmcChunk7Canonical $request.ownedPairs.partyIds)-cne(ConvertTo-KmcChunk7Canonical $Selection.partyIds)-or
    (ConvertTo-KmcChunk7Canonical $request.persistenceLoad)-cne(ConvertTo-KmcChunk7Canonical $fixtureSource.descriptor)){throw 'Six-pair Manual producer substituted its qualified Manual308 input.'}
 Assert-KmcChunk7OwnedSourceCompletion $set $Selection.checkpointSourceRunId 'chunk7-owned-two-pair-campaign' $Fixture $Candidate $Selection.sourceArchiveSha256
 Assert-KmcBootstrapInventoryString $set.owner.persistenceCase $PersistenceCase
 Assert-KmcChunk7PersistenceArtifact $request $set.manifest 'PASS'
 $checkpoint=Read-KmcChunk7ManifestArtifact $request $set.manifest 'chunk7-persistence-evidence.json' 'chunk7-persistence-evidence'
 $checkpointPath=Join-Path $set.root 'chunk7-persistence-evidence.json';$checkpointFile=Get-Item -LiteralPath $checkpointPath
 if($checkpointFile.Length-le0-or$checkpointFile.Length-gt16MB-or$set.pins[$checkpointPath]-cne$Selection.checkpointSourceArtifactSha256){throw 'Six-pair Manual producer receipt differs from its exact pin.'}
 $w=$checkpoint.observations.nativeWrite;$leaf=Get-KmcChunk7OwnedOutputLeaf $request $checkpoint.observations
 Assert-KmcBootstrapInventoryString $leaf (Get-KmcChunk7SixPairManualLeaf);Assert-KmcBootstrapInventoryString $w.nativeName (Get-KmcChunk7SixPairManualName)
 Assert-KmcBootstrapInventoryString $w.path (Join-Path (Join-Path $set.profile 'Saved Games') $leaf)
 Assert-KmcNotReparsePoint $w.path 'six-pair mounted Manual';Assert-KmcNotHardLink $w.path 'six-pair mounted Manual'
 $file=Get-Item -LiteralPath $w.path
 if($file.Length-ne$w.length-or(Get-KmcSha256 $w.path)-cne$w.sha256){throw 'Six-pair mounted Manual bytes differ from the completed native write.'}
 $descriptor=[pscustomobject][ordered]@{internalName=$w.nativeName;fileName=$leaf;sha256=$w.sha256;length=$w.length;
  lastWriteTimeUtcTicks=[long]$file.LastWriteTimeUtc.Ticks;gameId=$fixtureSource.descriptor.gameId;gameName=$fixtureSource.descriptor.gameName;area=$w.archive.AreaId}
 Assert-KmcChunk7OwnedPairsDescriptor ([pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';runId=$run;ownedPairs=$Selection;
  persistenceCase=$PersistenceCase;persistenceLoad=$descriptor;fixture=$Fixture})
 Assert-KmcChunk7OwnedReceiptPins $set
 [pscustomobject][ordered]@{path=$w.path;descriptor=$descriptor;artifactPath=$fixtureSource.artifactPath;
  artifactSha256=$fixtureSource.artifactSha256;artifactLength=$fixtureSource.artifactLength;sourceRunId=$fixtureSource.qualificationRunId;
  checkpointArtifactPath=$checkpointPath;checkpointArtifactSha256=$Selection.checkpointSourceArtifactSha256;
  checkpointArtifactLength=[long]$checkpointFile.Length;checkpointCandidate=[pscustomobject]@{commit=$request.commit;dllSha256=$request.dllSha256;dllMvid=$request.dllMvid};
  fixtureSetupOnly=$true;nativeAuthorityGranted=$false}
}
