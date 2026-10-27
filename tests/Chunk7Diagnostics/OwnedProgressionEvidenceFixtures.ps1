Set-StrictMode -Version Latest
function New-OwnedProgressionFixture([string]$Case='owned-horse-native-level') {
 $op=Get-KmcChunk7ProgressionOperation $Case
 if(-not(Test-KmcChunk7OwnedProgressionCase $Case)){throw 'Owned synthetic fixture requires one exact owned case.'}
 $f=New-ProgressionFixture $op;$r=$f.request;$o=$f.artifact.observations;$s=$r.progression;$s.case=$Case
 $main='88888888-1111-4111-8111-111111111111';$campaign='99999999-1111-4111-8111-111111111111';$area='c'*32
 $nested=[pscustomobject]@{case='persistence-load';sourceRunId=$s.inventoryRunId;sourceArtifactSha256=$s.inventoryArtifactSha256;sourceArchiveSha256='c'*64;checkpointSourceRunId=$s.sourceRunId;checkpointSourceArtifactSha256=$s.sourceArtifactSha256;riderAId=$s.controlActorId;mountAId=$s.controlPetId;riderBId=$s.actorId;mountBId=$s.petId;partyIds=@($main,$s.controlActorId,$s.controlPetId,$s.actorId,$s.petId);baseGameOnly=$true;expectedNativeLoadCount=1;expectedNativeWriteCount=0}
 $s|Add-Member NoteProperty ownedSource $nested
 $s.nativeWriteLeaf=$null
 $r.persistenceLoad.internalName='KMC_C7_WORLD';$r.persistenceLoad.fileName='Manual_404_KMC_C7_WORLD.zks';$r.persistenceLoad.gameId=$campaign;$r.persistenceLoad.gameName='Observed isolated campaign';$r.persistenceLoad.area=$area
 $o.selection=Copy-O $s;$o.loadedDescriptor=Copy-O $r.persistenceLoad
 $o.loadedArchive.CampaignId=$campaign;$o.loadedArchive.AreaId=$area;$o.loadedArchive.Pairs[0].ProfileId='medium-humanoid-horse-v1'
 if($null-ne$o.session.nativeWrite){$o.session.nativeWrite.gameId=$campaign;$o.session.nativeWrite.gameName=$r.persistenceLoad.gameName;$o.session.nativeWrite.area=$area;$o.session.nativeWrite.archive.CampaignId=$campaign;$o.session.nativeWrite.archive.AreaId=$area;$o.session.nativeWrite.archive.Pairs[0].ProfileId='medium-humanoid-horse-v1'}
 if($null-ne$o.session.nativeWrite){
  $w=$o.session.nativeWrite;$w|Add-Member NoteProperty nativeManualNumber 1;$w|Add-Member NoteProperty manualCallbackCount 1;$w|Add-Member NoteProperty snapshotBefore 0
  $w.leaf='Manual_1_KMC_C7_PROGRESSION.zks';$w.path=Join-Path ([IO.Path]::GetDirectoryName($w.path)) $w.leaf
  $w.type='Manual';$w.name='KMC_C7_PROGRESSION';$w.nativeAutoLevelup=$false;$w.nativeCallbackSupplied=$true
  $o.session.nativeCompletion=$null
  $kinds=@('native-save-request','native-save-prepared','snapshot-starting','snapshot-staged','native-worker-completed','native-save-callback')
  $events=@();for($i=0;$i-lt6;$i++){$events+=,[pscustomobject]@{sequence=$i+1;kind=$kinds[$i];frame=$i+1;requested=$w.requestedObject;descriptor=$(if($i-eq0){$w.requestedObject}else{$w.writtenObject});internalName=$w.name;type='Manual';fileName=$(if($i-eq0){$null}else{$w.leaf});path=$(if($i-eq0){$null}else{$w.path});snapshotCount=$(if($i-lt3){0}else{1});completedSaveCount=$(if($i-lt5){0}else{1})}}
  $events[4]|Add-Member NoteProperty iteratorType 'Kingmaker.EntitySystem.Persistence.SaveManager+<SaveRoutine>d__46';$events[4]|Add-Member NoteProperty workerStatus 'RanToCompletion';$events[4]|Add-Member NoteProperty workerId 4;$events[4]|Add-Member NoteProperty nativeWorkerMatches $true;$events[4]|Add-Member NoteProperty workerCompleted $true;$events[4]|Add-Member NoteProperty workerFaulted $false;$events[4]|Add-Member NoteProperty workerCanceled $false
  $hooks=@([pscustomobject]@{type='Kingmaker.EntitySystem.Persistence.SaveManager';method='SaveRoutine';token='06008029';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'},[pscustomobject]@{type='Kingmaker.EntitySystem.Persistence.SaveManager';method='PrepareSave';token='06008025';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'},[pscustomobject]@{type='KingmakerMountedCombat.Integration.MountedPersistenceService';method='RecordCompletedSave';token='06001000';moduleMvid=$r.dllMvid})
  $o.session|Add-Member NoteProperty manualSave ([pscustomobject]@{contract='manual-seed-native-save-v1';installed=$true;installationAttempted=$true;complete=$true;requests=1;prepares=1;starts=1;stages=1;completions=1;callbacks=1;events=$events;hooks=$hooks;errors=@()})
 }
 $o.source|Add-Member NoteProperty ownedSource (Copy-O $nested)
 $o.source.inventoryActor|Add-Member NoteProperty originalSize 'Medium';$o.source.inventoryActor|Add-Member NoteProperty effectiveSize 'Medium'
 $o.source.inventoryActor|Add-Member NoteProperty nativeLevelUpReadiness ([pscustomobject]@{experience=20000;characterLevel=4})
 $worldParty=@([pscustomobject]@{id=$main;blueprint='1'*32},[pscustomobject]@{id=$s.controlActorId;blueprint='2'*32},[pscustomobject]@{id=$s.controlPetId;blueprint='4016c7db400ab721ff125aef9e65e202'},[pscustomobject]@{id=$s.actorId;blueprint='3'*32},[pscustomobject]@{id=$s.petId;blueprint='4016c7db400ab721ff125aef9e65e202'})
 $o.source|Add-Member NoteProperty inventoryWorld ([pscustomobject]@{mainActorId=$main;party=$worldParty})
 function ActorRow($Id,[int]$Object,[int]$Descriptor,[int]$View){[pscustomobject]@{id=$Id;object=$Object;descriptor=$Descriptor;view=$View}}
 $party=@((ActorRow $main 21 121 221),(ActorRow $s.controlActorId 11 111 211),(ActorRow $s.controlPetId 12 112 212),(ActorRow $s.actorId 3 103 203),(ActorRow $s.petId 4 104 204))
 $old=[pscustomobject]@{id=$s.petId;object=4;descriptor=104;view=204;isInState=$true;isInGame=$true;shouldBeDestroyed=$false;destroyed=$false;presentInNativeState=$true;masterId=$s.actorId}
 $before=[pscustomobject]@{contract='owned-progression-roster-v1';phase='loaded';nativeTargetTransitionObserved=$false;targetRemoved=$false;currentPetId=$s.petId;originalPartyIds=(Copy-O $nested.partyIds);originalCharacterIds=@($main,$s.controlActorId,$s.actorId);party=$party;characterIds=@($main,$s.controlActorId,$s.actorId);remoteCapturedIds=@();controlOwnerId=$o.loadedArchive.Pairs[0].PairId;controlGeneration=1L;controlFeature=1001;controlRankFact=1002;controlSource=1003;controlRank=4;resourceEqualityAcrossNativeRestClaimed=$false;originalPet=$old}
 $after=Copy-O $before;$after.phase='settled'
 $dismiss=$op-ceq'horse-native-dismiss';$respec=$op-cin@('horse-native-retrain-horse','horse-native-retrain-mammoth')
 if($dismiss){$after.nativeTargetTransitionObserved=$true;$after.targetRemoved=$true;$after.currentPetId=$null;$after.party=@($after.party|Where-Object{$_.id-cne$s.actorId-and$_.id-cne$s.petId});$after.characterIds=@($after.characterIds|Where-Object{$_-cne$s.actorId});$after.originalPet.isInGame=$false;$after.originalPet.masterId=$null}
 if($respec){
  $terminal=@($o.session.events|Where-Object{$_.PSObject.Properties['kind']-and$_.kind-ceq'native-progression-builder-complete'})[0]
  $after.nativeTargetTransitionObserved=$true;$after.currentPetId=$terminal.petId;$after.party=@($after.party|Where-Object{$_.id-cne$s.petId})+@((ActorRow $terminal.petId 703 803 903))
  $target=@($after.party|Where-Object{$_.id-ceq$s.actorId})[0];$target.descriptor=303;$target.view=403
  $after.originalPet.destroyed=$true;$after.originalPet.shouldBeDestroyed=$true;$after.originalPet.isInState=$false;$after.originalPet.presentInNativeState=$false;$after.originalPet.view=$null;$after.originalPet.masterId=$null
  $probe=$o.session.nativeUi.nativeRespec;$update=@($probe.events|Where-Object kind -CEQ 'native-respec-pet-update')[0];$update.petObject=703;$update.petDescriptorObject=803;$update.petViewObject=903
  @($probe.events|Where-Object kind -CEQ 'native-respec-finish-enter')[0].nativeRestTargets[0].descriptorObject=111
  foreach($rest in @($probe.events|Where-Object{$_.kind-cin@('native-respec-rest-before','native-respec-rest-after')})){$rest.descriptorObject=111}
 }
 function CorrectPetSnapshot($value){
  if($null-eq$value-or$value-is[string]-or$value.GetType().IsValueType){return}
  if($value-is[Array]){foreach($item in $value){CorrectPetSnapshot $item};return}
  if($value.PSObject.Properties['petObject']-and$value.PSObject.Properties['petId']-and$null-ne$value.petId){if($value.petId-ceq$s.petId){$value.petObject=4}elseif($respec-and$value.petId-ceq$after.currentPetId){$value.petObject=703}}
  foreach($property in $value.PSObject.Properties){CorrectPetSnapshot $property.Value}
 }
 CorrectPetSnapshot $o.session.events
 $o|Add-Member NoteProperty ownedRosterBefore $before;$o|Add-Member NoteProperty ownedRosterAfter $after
 return $f
}
