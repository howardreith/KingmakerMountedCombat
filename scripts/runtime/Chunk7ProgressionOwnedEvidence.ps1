Set-StrictMode -Version Latest
function Get-KmcChunk7OwnedProgressionCases {@('owned-horse-native-level','owned-horse-native-dismiss','owned-horse-native-retrain-horse','owned-horse-native-retrain-mammoth','small-horse-native-dismiss')}
function Test-KmcChunk7OwnedProgressionCase($Case){$Case-is[string]-and$Case-cin(Get-KmcChunk7OwnedProgressionCases)}
function Test-KmcChunk7SmallProgressionCase($Case){$Case-is[string]-and$Case-ceq'small-horse-native-dismiss'}
function Get-KmcChunk7ProgressionOperation($Case){
 if($Case-isnot[string]){throw 'Progression operation requires an exact scalar case.'}
 switch -CaseSensitive($Case){
  'horse-native-level'{'horse-native-level'} 'owned-horse-native-level'{'horse-native-level'}
  'horse-native-dismiss'{'horse-native-dismiss'} 'owned-horse-native-dismiss'{'horse-native-dismiss'} 'small-horse-native-dismiss'{'horse-native-dismiss'}
  'horse-native-retrain-horse'{'horse-native-retrain-horse'} 'owned-horse-native-retrain-horse'{'horse-native-retrain-horse'}
  'horse-native-retrain-mammoth'{'horse-native-retrain-mammoth'} 'owned-horse-native-retrain-mammoth'{'horse-native-retrain-mammoth'}
  default{throw 'Unknown exact progression operation.'}
 }
}
function Get-KmcChunk7ProgressionOwnedRequest($Request){
 # This is a read-only validator projection, never a runtime request mutation or restore authority.
 [pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';runId=$Request.runId;ownedPairs=$Request.progression.ownedSource;persistenceCase='multi-exploration-manual';fixture=$(if($Request.PSObject.Properties['fixture']){$Request.fixture}else{$null});persistenceLoad=$(if($Request.PSObject.Properties['persistenceLoad']){$Request.persistenceLoad}else{$null})}
}
function Assert-KmcChunk7OwnedProgressionSelection($Request){
 if($Request-is[Collections.IDictionary]){$Request=[pscustomobject]$Request}
 if($null-ne$Request.PSObject.Properties['ownedPairs']){throw 'Progression cannot acquire outer owned-pair dispatch authority.'}
 $s=$Request.progression;$p=Get-KmcChunk7ProgressionOwnedRequest $Request;Assert-KmcChunk7OwnedPairsSelection $p;$n=$s.ownedSource
 if(Test-KmcChunk7SmallProgressionCase $s.case){
  if($n.case-cne'small-fixture-persistence-load'-or$n.expectedNativeWriteCount-ne0-or$n.expectedNativeLoadCount-ne1){throw 'Small dismissal requires a read-only Small mounted Manual cold lineage.'}
  foreach($pair in @(@('sourceRunId','checkpointSourceRunId'),@('sourceArtifactSha256','checkpointSourceArtifactSha256'))){Assert-KmcBootstrapInventoryString $s.($pair[0]) $n.($pair[1])}
  if($s.inventoryRunId-ceq$s.sourceRunId-or$s.inventoryRunId-ceq$n.sourceRunId-or$s.inventoryArtifactSha256-ceq$s.sourceArtifactSha256-or
     $s.inventoryArtifactSha256-ceq$n.sourceArtifactSha256-or$s.actorId-cne$n.riderBId-or$s.petId-cne$n.mountBId-or
     $s.controlActorId-cne$n.riderAId-or$s.controlPetId-cne$n.mountAId){throw 'Small dismissal substituted producer/cold receipt or Medium/Small role.'}
  return
 }
 if($n.case-cne'persistence-load'-or$n.expectedNativeWriteCount-ne0-or$n.expectedNativeLoadCount-ne1){throw 'Owned progression requires a read-only exploration Manual checkpoint.'}
 foreach($pair in @(@('sourceRunId','checkpointSourceRunId'),@('sourceArtifactSha256','checkpointSourceArtifactSha256'),@('inventoryRunId','sourceRunId'),@('inventoryArtifactSha256','sourceArtifactSha256'))){Assert-KmcBootstrapInventoryString $s.($pair[0]) $n.($pair[1])}
 $a=$s.actorId-ceq$n.riderAId-and$s.petId-ceq$n.mountAId-and$s.controlActorId-ceq$n.riderBId-and$s.controlPetId-ceq$n.mountBId
 $b=$s.actorId-ceq$n.riderBId-and$s.petId-ceq$n.mountBId-and$s.controlActorId-ceq$n.riderAId-and$s.controlPetId-ceq$n.mountAId
 if(-not($a-or$b)){throw 'Progression target/control must preserve the two exact inherited pair roles.'}
}
function Get-KmcChunk7ProgressionFixture($Request){if(Test-KmcChunk7OwnedProgressionCase $Request.progression.case){return $Request.persistenceLoad};return $Request.fixture.working}
function Assert-KmcChunk7OwnedProgressionDescriptor($Request){
 Assert-KmcChunk7OwnedPairsDescriptor (Get-KmcChunk7ProgressionOwnedRequest $Request)
 Assert-KmcBootstrapInventoryString $Request.persistenceLoad.sha256 $Request.progression.sourceArchiveSha256
 if($Request.persistenceLoad.sha256-ceq$Request.progression.ownedSource.sourceArchiveSha256){throw 'Progression cannot load the unmounted acquired ancestry in place of its mounted checkpoint.'}
 if(Test-KmcChunk7SmallProgressionCase $Request.progression.case){
  if($Request.persistenceLoad.internalName-cne'KMC_C7_SMALL_WORLD'-or$Request.persistenceLoad.fileName-cnotmatch'^Manual_[1-9][0-9]*_KMC_C7_SMALL_WORLD[.]zks$'-or
     $Request.persistenceLoad.gameId-cne'3f7381f7-aba6-43bf-bae6-39c6a66a7180'-or$Request.persistenceLoad.gameName-cne'Testy the Modless'-or
     $Request.persistenceLoad.area-cne'b10e86516b6437d45a629931c30936fe'){throw 'Small dismissal requires its exact qualified campaign and native Manual.'}
 }
}
function Assert-KmcChunk7ProgressionIdSet($Value,$Expected,[string]$Description){
 if($Value-isnot[Array]){throw ($Description+' must be an actual JSON array.')};$seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
 foreach($id in $Value){Assert-KmcOrdinaryGuid $id;if(-not$seen.Add($id)){throw ($Description+' repeats an actor.')}}
 if($null-ne$Expected-and(ConvertTo-KmcChunk7Canonical @($Value|Sort-Object -CaseSensitive))-cne(ConvertTo-KmcChunk7Canonical @($Expected|Sort-Object -CaseSensitive))){throw ($Description+' differs from exact native roster custody.')}
}
function Assert-KmcChunk7OwnedProgressionRoster($Request,$Observations,$Before,$Terminal){
 $s=$Request.progression;$op=Get-KmcChunk7ProgressionOperation $s.case;$n=$s.ownedSource
 if((ConvertTo-KmcChunk7Canonical $Observations.source.ownedSource)-cne(ConvertTo-KmcChunk7Canonical $n)){throw 'Owned progression source evidence changed the declared checkpoint ancestry.'}
 $world=$Observations.source.inventoryWorld
 Assert-KmcOrdinaryGuid $world.mainActorId
 if($world.mainActorId-ceq$s.actorId){throw 'Progression cannot select the native main character.'}
 if($world.party-isnot[Array]-or$world.party.Count-ne$n.partyIds.Count){throw 'Acquired inventory omitted the complete original native roster.'}
 Assert-KmcChunk7ProgressionIdSet @($world.party|ForEach-Object{$_.id}) $n.partyIds 'acquired source roster'
 foreach($actor in $world.party){if($actor.blueprint-isnot[string]-or$actor.blueprint-cnotmatch'^[0-9a-f]{32}$'){throw 'Acquired source actor blueprint is not an exact scalar GUID.'}}
 $sourceActor=$Observations.source.inventoryActor
 if(Test-KmcChunk7SmallProgressionCase $s.case){
  Assert-KmcChunk7Boolean $Observations.source.smallFixtureSetupOnly $true
  $fixture=$Observations.source.smallColdFixture
  if($fixture-isnot[pscustomobject]-or$fixture.arrangement-isnot[pscustomobject]){throw 'Small cold fixture and arrangement must be exact JSON objects.'}
  Assert-KmcBootstrapInventoryString $fixture.sourceCase 'owner-seed-default-halfling-horse-manual'
  Assert-KmcBootstrapInventoryString $world.mainActorId $n.riderAId
  Assert-KmcChunk7ProgressionIdSet $fixture.partyIds $n.partyIds 'Small cold field actors'
  if($fixture.arrangement.actors-isnot[Array]-or$fixture.arrangement.actors.Count-ne$n.partyIds.Count){throw 'Small cold arrangement lost its seven exact native actors.'}
  Assert-KmcChunk7ProgressionIdSet @($fixture.arrangement.actors|ForEach-Object{$_.actorId}) $n.partyIds 'Small cold arrangement actors'
  foreach($row in $fixture.arrangement.actors){
   $projected=@($world.party|Where-Object id -CEQ $row.actorId)
   if($projected.Count-ne1-or$projected[0].blueprint-cne$row.blueprint){throw 'Small progression world projection differs from cold native arrangement.'}
  }
  $target=@($fixture.arrangement.actors|Where-Object actorId -CEQ $s.actorId)
  if($target.Count-ne1-or(ConvertTo-KmcChunk7Canonical $target[0])-cne(ConvertTo-KmcChunk7Canonical $sourceActor)){
   throw 'Small dismissal target inventory differs from the independent cold native observation.'
  }
  Assert-KmcBootstrapInventoryString $sourceActor.blueprint 'baaff53a675a84f4983f1e2113b24552'
  if($fixture.pairs-isnot[Array]-or$fixture.pairs.Count-ne2){throw 'Small cold fixture lost the exact two sourced Horse pairs.'}
  foreach($role in @(@($n.riderAId,$n.mountAId,'medium-humanoid-horse-v1'),@($n.riderBId,$n.mountBId,'small-default-halfling-horse-v1'))){
   $pair=@($fixture.pairs|Where-Object riderId -CEQ $role[0]);$archive=@($Observations.loadedArchive.Pairs|Where-Object{$_.Rider.Id-ceq$role[0]})
   if($pair.Count-ne1-or$archive.Count-ne1){throw 'Small cold fixture lost an exact mounted role.'}
   Assert-KmcBootstrapInventoryString $pair[0].mountId $role[1]
   Assert-KmcBootstrapInventoryString $pair[0].profileId $role[2]
   Assert-KmcBootstrapInventoryString $pair[0].sourceGuid '97261d609529d834eba4fd4da1bc44dc'
   Assert-KmcOrdinaryInteger $pair[0].rank 1 1
   Assert-KmcBootstrapInventoryString $archive[0].Mount.Id $role[1]
   Assert-KmcBootstrapInventoryString $archive[0].ProfileId $role[2]
  }
  Assert-KmcOrdinaryInteger $sourceActor.experience $Before.xp $Before.xp
  Assert-KmcOrdinaryInteger $sourceActor.characterLevel $Before.level $Before.level
 }else{
  foreach($field in @('originalSize','effectiveSize')){Assert-KmcBootstrapInventoryString $sourceActor.$field 'Medium'}
  Assert-KmcOrdinaryInteger $sourceActor.nativeLevelUpReadiness.experience $Before.xp $Before.xp
  Assert-KmcOrdinaryInteger $sourceActor.nativeLevelUpReadiness.characterLevel $Before.level $Before.level
 }
 $first=$Observations.ownedRosterBefore;$last=$Observations.ownedRosterAfter
 $control=@($Observations.loadedArchive.Pairs|Where-Object{$_.Rider.Id-ceq$s.controlActorId})[0]
 foreach($entry in @(@($first,'loaded'),@($last,'settled'))){
  $row=$entry[0]
  Assert-KmcExactProperties $row @('contract','phase','nativeTargetTransitionObserved','targetRemoved','currentPetId','originalPartyIds','originalCharacterIds','party','characterIds','remoteCapturedIds','controlOwnerId','controlGeneration','controlFeature','controlRankFact','controlSource','controlRank','resourceEqualityAcrossNativeRestClaimed','originalPet') 'owned progression roster'
  Assert-KmcBootstrapInventoryString $row.contract 'owned-progression-roster-v1';Assert-KmcBootstrapInventoryString $row.phase $entry[1]
  foreach($key in @('nativeTargetTransitionObserved','targetRemoved')){if($row.$key-isnot[bool]){throw 'Native target lifecycle flags must be JSON Booleans.'}}
  Assert-KmcChunk7Boolean $row.resourceEqualityAcrossNativeRestClaimed $false
  $old=$row.originalPet
  Assert-KmcExactProperties $old @('id','object','descriptor','view','isInState','isInGame','shouldBeDestroyed','destroyed','presentInNativeState','masterId') 'original native pet settlement'
  Assert-KmcBootstrapInventoryString $old.id $s.petId
  foreach($key in @('object','descriptor')){Assert-KmcProgressionObject $old.$key}
  if($null-ne$old.view){Assert-KmcProgressionObject $old.view}
  foreach($key in @('isInState','isInGame','shouldBeDestroyed','destroyed','presentInNativeState')){if($old.$key-isnot[bool]){throw 'Original pet state must use native JSON Booleans.'}}
  if($null-ne$old.masterId){Assert-KmcOrdinaryGuid $old.masterId}
  if($null-ne$row.currentPetId){Assert-KmcOrdinaryGuid $row.currentPetId}
  Assert-KmcChunk7ProgressionIdSet $row.originalPartyIds $n.partyIds 'original owned roster'
  Assert-KmcChunk7ProgressionIdSet $row.originalCharacterIds $null 'original field characters'
  if($row.originalCharacterIds.Count-lt2-or$row.originalCharacterIds.Count-gt6-or$s.actorId-cnotin$row.originalCharacterIds-or$s.controlActorId-cnotin$row.originalCharacterIds-or$world.mainActorId-cnotin$row.originalCharacterIds-or$s.petId-cin$row.originalCharacterIds-or$s.controlPetId-cin$row.originalCharacterIds){throw 'Original native character roster is incomplete or exceeds its cap.'}
  foreach($id in $row.originalCharacterIds){if($id-cnotin$n.partyIds){throw 'Original field character is outside admitted roster.'}}
  Assert-KmcChunk7ProgressionIdSet $row.characterIds $null 'current field characters'
  Assert-KmcChunk7ProgressionIdSet $row.remoteCapturedIds @() 'remote captured actors'
  Assert-KmcBootstrapInventoryString $row.controlOwnerId $control.PairId;Assert-KmcOrdinaryInteger $row.controlGeneration $control.Generation $control.Generation
  foreach($key in @('controlFeature','controlRankFact','controlSource')){Assert-KmcProgressionObject $row.$key}
  if(@($row.controlFeature,$row.controlRankFact,$row.controlSource|Select-Object -Unique).Count-ne3){throw 'Control fact/source native roles alias.'}
  Assert-KmcOrdinaryInteger $row.controlRank 1 20
  if($row.party-isnot[Array]-or$row.party.Count-gt12){throw 'Current native roster is unbounded.'}
  Assert-KmcChunk7ProgressionIdSet @($row.party|ForEach-Object{$_.id}) $null 'current native party'
  $labels=[Collections.Generic.HashSet[int]]::new()
  foreach($actor in $row.party){Assert-KmcExactProperties $actor @('id','object','descriptor','view') 'current native roster actor';foreach($key in @('object','descriptor','view')){Assert-KmcProgressionObject $actor.$key;if(-not$labels.Add([int]$actor.$key)){throw 'Native roster identities alias.'}}}
 }
 Assert-KmcChunk7Boolean $first.nativeTargetTransitionObserved $false;Assert-KmcChunk7Boolean $first.targetRemoved $false;Assert-KmcBootstrapInventoryString $first.currentPetId $s.petId
 Assert-KmcChunk7ProgressionIdSet @($first.party|ForEach-Object{$_.id}) $n.partyIds 'loaded native roster'
 Assert-KmcChunk7ProgressionIdSet $first.characterIds $first.originalCharacterIds 'loaded field characters'
 foreach($key in @('originalCharacterIds','controlFeature','controlRankFact','controlSource','controlRank')){if((ConvertTo-KmcChunk7Canonical $first.$key)-cne(ConvertTo-KmcChunk7Canonical $last.$key)){throw 'Progression changed unrelated native field/fact custody.'}}
 $dismiss=$op-ceq'horse-native-dismiss';$changed=$op-cne'horse-native-level'
 Assert-KmcChunk7Boolean $last.nativeTargetTransitionObserved $changed;Assert-KmcChunk7Boolean $last.targetRemoved $dismiss
 $expectedParty=@($n.partyIds);$expectedChars=@($first.originalCharacterIds)
 if($changed){$expectedParty=@($expectedParty|Where-Object{$_-cne$s.petId-and(-not$dismiss-or$_-cne$s.actorId)});if($dismiss){$expectedChars=@($expectedChars|Where-Object{$_-cne$s.actorId});if($null-ne$last.currentPetId){throw 'Native dismissal retained a current pet.'}}else{Assert-KmcBootstrapInventoryString $last.currentPetId $Terminal.petId;if($last.currentPetId-ceq$s.petId-or$last.currentPetId-cin$n.partyIds){throw 'Native retrain did not bind its exact new pet.'};$expectedParty+=@($last.currentPetId)}}else{Assert-KmcBootstrapInventoryString $last.currentPetId $s.petId}
 Assert-KmcChunk7ProgressionIdSet @($last.party|ForEach-Object{$_.id}) $expectedParty 'settled native roster'
 Assert-KmcChunk7ProgressionIdSet $last.characterIds $expectedChars 'settled native characters'
 foreach($actor in $first.party){
  if($actor.id-cnotin$expectedParty){continue};$after=@($last.party|Where-Object{$_.id-ceq$actor.id})[0]
  $keys=if($actor.id-ceq$s.actorId-and$changed-and-not$dismiss){@('object')}else{@('object','descriptor','view')}
  foreach($key in $keys){if($after.$key-ne$actor.$key){throw 'Progression replaced a retained actor/descriptor/view.'}}
 }
 $original=@($first.party|Where-Object{$_.id-ceq$s.petId})[0]
 foreach($key in @('object','descriptor','view')){if($first.originalPet.$key-ne$original.$key){throw 'Original pet observation lost its admitted native identity.'}}
 foreach($key in @('object','descriptor')){if($last.originalPet.$key-ne$first.originalPet.$key){throw 'Old pet identity changed during native settlement.'}}
 foreach($key in @('isInState','isInGame','presentInNativeState')){Assert-KmcChunk7Boolean $first.originalPet.$key $true}
 foreach($key in @('shouldBeDestroyed','destroyed')){Assert-KmcChunk7Boolean $first.originalPet.$key $false}
 Assert-KmcBootstrapInventoryString $first.originalPet.masterId $s.actorId
 if($changed-and-not$dismiss){
  Assert-KmcChunk7Boolean $last.originalPet.destroyed $true
  Assert-KmcChunk7Boolean $last.originalPet.isInState $false
  Assert-KmcChunk7Boolean $last.originalPet.presentInNativeState $false
  if($null-ne$last.originalPet.view-or$null-ne$last.originalPet.masterId){throw 'Final native respec still retains its original pet view or master.'}
  # IsInGame and ShouldBeDestroyed are recorded raw. Native Dispose does not prove an IsInGame=false assignment.
 }elseif(-not$changed){
  if((ConvertTo-KmcChunk7Canonical $first.originalPet)-cne(ConvertTo-KmcChunk7Canonical $last.originalPet)){throw 'Level-only progression changed the original pet lifecycle.'}
 }
 # Native dismissal has its own observed inactive/detached result; do not infer respec destruction semantics.
 $target=@($first.party|Where-Object{$_.id-ceq$s.actorId})[0];if($target.object-ne$Before.object){throw 'Initial native target object differs from session admission.'}
 $custody=$Observations.session.pairCustody.observations[0].unrelatedOwners[0]
 if(@($first.party|Where-Object{$_.id-ceq$s.controlActorId})[0].object-ne$custody.riderObject-or@($first.party|Where-Object{$_.id-ceq$s.controlPetId})[0].object-ne$custody.mountObject){throw 'Native roster lost its exact independent mounted control identities.'}
 if($changed-and-not$dismiss){$probe=$Observations.session.nativeUi.nativeRespec
  $entered=@($probe.events|Where-Object{$_.kind-ceq'native-respec-enter'})[0];$rebound=@($probe.events|Where-Object{$_.kind-ceq'native-respec-original-rebound'})[0]
  $afterTarget=@($last.party|Where-Object{$_.id-ceq$s.actorId})[0]
  if($target.descriptor-ne$entered.descriptorObject-or$target.view-ne$entered.viewObject-or$afterTarget.descriptor-ne$rebound.descriptorObject-or$afterTarget.view-ne$rebound.viewObject){throw 'Target descriptor/view changed outside the exact observed native rebind.'}
  $petUpdates=@($probe.events|Where-Object{$_.kind-ceq'native-respec-pet-update'-and$_.petId-ceq$last.currentPetId});if($petUpdates.Count-lt1){throw 'Settled pet has no observed native AddPet identity.'};$pet=$petUpdates[-1];$row=@($last.party|Where-Object{$_.id-ceq$last.currentPetId})[0];if($row.object-ne$pet.petObject-or$row.descriptor-ne$pet.petDescriptorObject-or$row.view-ne$pet.petViewObject){throw 'Settled pet substituted the exact observed native AddPet result.'}}
}
