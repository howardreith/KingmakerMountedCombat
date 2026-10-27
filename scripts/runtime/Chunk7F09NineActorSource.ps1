Set-StrictMode -Version Latest

# Read-only F09 input to a possible fifth/sixth supported-pair derivative. The
# batch178 Manual has a preserved original outer FAIL; its distinct reader-only
# recovery and batch179 original cold PASS are both required here.
function Get-KmcChunk7F09NineActorSource($Fixture){
 $producer='20261022-c7b-world178-b178s1-fourthhorse-manual'
 $cold='20261022-c7b-world179-b179s1-fourthhorse-cold'
 $artifactHash='453c3c590a444807f9fba42e9e0eea6e44d8db42b6e3194437c71c8c9547ea02'
 $archiveHash='e82f7bb0775535ac2d9b782c6e53fa92a0893ab71bdf375a1e7cc06d50188759'
 $coldArtifactHash='439cd665da2861388d75f1fb6eafd33dfdc3b4de904b08fe7371ae4aeb6f45d3'
 $coldGameHash='ec4097074b7d89c2b8ad800ece7fa6f2e0c3058d1ae90a03cbebca484dbee58c'
 $coldOuterHash='114161c1eabe46cea71dcedc86caded489fc4563c15bb136bd3183ce05731c64'
 $selection=[pscustomobject][ordered]@{case='fourth-horse-manual-readonly-inventory';sourceRunId=$producer
  sourceArtifactSha256=$artifactHash;sourceArchiveSha256=$archiveHash;baseGameOnly=$true
  expectedNativeLoadCount=1;expectedNativeWriteCount=0}
 $set=Get-KmcChunk7OwnedReceiptSet $cold 'chunk7-assisted-cold-inventory-evidence.json';$r=$set.request
 $recorded=Get-KmcChunk7RecordedCandidate $r
 $source=Get-KmcChunk7FourthHorseColdSource $selection $Fixture $recorded
 Assert-KmcAssistedColdSourcePins $source $true $true
 Assert-KmcBootstrapInventoryString $r.scenario 'chunk7-assisted-cold-inventory'
 Assert-KmcChunk7AssistedColdInventoryDescriptor $r
 if((ConvertTo-KmcChunk7Canonical $r.assistedColdInventory)-cne(ConvertTo-KmcChunk7Canonical $selection)-or
    (ConvertTo-KmcChunk7Canonical $r.persistenceLoad)-cne(ConvertTo-KmcChunk7Canonical $source.descriptor)){
  throw 'Nine-actor cold consumer substituted its declared producer or native Manual.'
 }
 Assert-KmcChunk7OwnedSourceCompletion $set $cold 'chunk7-assisted-cold-inventory' $Fixture $recorded $archiveHash 0
 foreach($pair in @(@('runtime-result.json',$coldOuterHash),@('runtime-game-result.json',$coldGameHash),
   @('chunk7-assisted-cold-inventory-evidence.json',$coldArtifactHash))){
  Assert-KmcBootstrapInventoryString $set.pins[(Join-Path $set.root $pair[0])] $pair[1]
 }
 Assert-KmcChunk7AssistedColdInventoryArtifact $r $set.manifest 'PASS'
 $path=Join-Path $set.root 'chunk7-assisted-cold-inventory-evidence.json'
 $a=Read-KmcChunk7ManifestArtifact $r $set.manifest 'chunk7-assisted-cold-inventory-evidence.json' 'chunk7-assisted-cold-inventory-evidence';$o=$a.observations
 foreach($pair in @(@('runId',$producer),@('artifactSha256',$artifactHash),@('archiveSha256',$archiveHash),
   @('archiveLeaf','Manual_306_KMC_C7_FOUR_HORSE_WORLD.zks'),@('kind','reader-reevaluated-assisted-fourth-horse-manual'))){
  Assert-KmcBootstrapInventoryString $o.source.($pair[0]) $pair[1]
 }
 Assert-KmcChunk7Boolean $o.source.assistedSetupOnly $true
 Assert-KmcChunk7Boolean $o.source.sameCandidate $false
 Assert-KmcChunk7Boolean $o.noPreparation.complete $true
 foreach($key in @('dropped','errors')){Assert-KmcOrdinaryInteger $o.noPreparation.$key 0 0}
 if(@($o.noPreparation.events).Count-ne0){throw 'Accepted cold receipt observed a native preparation or restoration event.'}
 foreach($key in @('resourceWrites','xpWrites','moneyWrites','mountRequests','choiceRequests')){Assert-KmcOrdinaryInteger $o.$key 0 0}
 $party=@($o.loadedWorld.party);$facts=@($o.fourthHorseFacts);$characters=@($o.loadedWorld.partyCharacterIds)
 if($party.Count-ne9-or$facts.Count-ne4-or$characters.Count-ne5-or
    @($party.id|Sort-Object -CaseSensitive -Unique).Count-ne9-or
    @($facts.riderId|Sort-Object -CaseSensitive -Unique).Count-ne4-or
    @($facts.mountId|Sort-Object -CaseSensitive -Unique).Count-ne4-or
    @($characters|Sort-Object -CaseSensitive -Unique).Count-ne5){
  throw 'Accepted cold receipt lost its nine actors, five characters or four distinct Horses.'
 }
 $linzi=@($o.nativeProfileInventory.party|Where-Object{$_.id-ceq'8265f8cf-0969-4ec7-a266-2448de91c8c4'})
 if($linzi.Count-ne1-or$linzi[0].effectiveSize-cne'Small'-or$null-ne$linzi[0].petId-or
   $linzi[0].dollPresent-ne$false-or$linzi[0].character.skeleton.name-cne'FemaleHalfling'-or
   $linzi[0].nativeLevelUpReadiness.characterLevel-ne4-or
   $linzi[0].nativeLevelUpReadiness.experience-ne9000-or
   $linzi[0].nativeLevelUpReadiness.canLevelUp-ne$false){
  throw 'The remaining stock Small rider is not the observed unsupported special-body slot.'
 }
 $ready=$o.nativeProfileInventory.ordinaryAcquisitionPrerequisites
 Assert-KmcOrdinaryInteger $ready.nativeFieldCharacterLimit 6 6
 Assert-KmcOrdinaryInteger $ready.nativeMoney 392468 392468
 Assert-KmcOrdinaryInteger $ready.nativeHireCostNow 18000 18000
 Assert-KmcChunk7Boolean $ready.nativeAffordableNow $true
 Assert-KmcAssistedSame $ready.nativePartyCharacterIds $characters 'nine-actor native party and hire readiness'
 if(@($o.loadedWorld.remoteCompanionIds).Count-ne0){throw 'Nine-actor source unexpectedly retains a remote companion.'}
 Assert-KmcChunk7OwnedReceiptPins $set;Assert-KmcAssistedColdSourcePins $source $true $true
 [pscustomobject][ordered]@{path=$source.path;descriptor=$source.descriptor
  producerRunId=$producer;producerArtifactPath=$source.artifactPath;producerArtifactSha256=$artifactHash
  producerProcessId=$source.sourceProcessId;coldRunId=$cold;coldArtifactPath=$path
  coldArtifactSha256=$coldArtifactHash;coldGameSha256=$coldGameHash;coldOuterSha256=$coldOuterHash
  rosterIds=@($party.id);characterIds=$characters;horseFacts=$facts
  petlessSpecialBodyActorId=$linzi[0].id;nativeFreeFieldCharacterSlots=1
  nativeHireCostNow=18000;fixtureSetupOnly=$true;nativeAuthorityGranted=$false}
}

# Batch180 observes an opened selector on the same immutable four-Horse Manual.
# It proves only that Linzi is optional in that exact world. No roster operation
# or paid-hire authority is inferred from a read-only observation.
function Get-KmcChunk7F09SelectorOptionalitySource($Fixture){
 $run='20261022-c7b-world180-b180s1-fourthhorse-selector'
 $artifactHash='985101587995147c4a548b6f81ed6c88837cc54ac39f3ca22b8352a32afb2e96'
 $gameHash='d5f89642eb0660e494b42c3534412c52352da85d7631d1a856f63e62b3e83dd3'
 $outerHash='48e44ca5a4386abded6611eea786137dec601bd97512b93c662cd16d364881f0'
 $nine=Get-KmcChunk7F09NineActorSource $Fixture
 $set=Get-KmcChunk7OwnedReceiptSet $run 'chunk7-assisted-cold-inventory-evidence.json';$r=$set.request
 $recorded=Get-KmcChunk7RecordedCandidate $r
 Assert-KmcBootstrapInventoryString $r.scenario 'chunk7-assisted-cold-inventory'
 Assert-KmcBootstrapInventoryString $r.assistedColdInventory.case 'fourth-horse-open-selector-inventory'
 Assert-KmcChunk7AssistedColdInventoryDescriptor $r
 if((ConvertTo-KmcChunk7Canonical $r.persistenceLoad)-cne(ConvertTo-KmcChunk7Canonical $nine.descriptor)){
  throw 'Selector observation substituted the four-Horse native Manual.'
 }
 foreach($key in @('sourceRunId','sourceArtifactSha256','sourceArchiveSha256')){
  $expected=switch($key){
   'sourceRunId'{$nine.producerRunId}
   'sourceArtifactSha256'{$nine.producerArtifactSha256}
   'sourceArchiveSha256'{$nine.descriptor.sha256}
  }
  Assert-KmcBootstrapInventoryString $r.assistedColdInventory.$key $expected
 }
 Assert-KmcChunk7Boolean $r.assistedColdInventory.baseGameOnly $true
 Assert-KmcOrdinaryInteger $r.assistedColdInventory.expectedNativeLoadCount 1 1
 Assert-KmcOrdinaryInteger $r.assistedColdInventory.expectedNativeWriteCount 0 0
 Assert-KmcChunk7OwnedSourceCompletion $set $run 'chunk7-assisted-cold-inventory' $Fixture $recorded $nine.descriptor.sha256 0
 foreach($pair in @(@('runtime-result.json',$outerHash),@('runtime-game-result.json',$gameHash),
   @('chunk7-assisted-cold-inventory-evidence.json',$artifactHash))){
  Assert-KmcBootstrapInventoryString $set.pins[(Join-Path $set.root $pair[0])] $pair[1]
 }
 Assert-KmcChunk7AssistedColdInventoryArtifact $r $set.manifest 'PASS'
 $a=Read-KmcChunk7ManifestArtifact $r $set.manifest 'chunk7-assisted-cold-inventory-evidence.json' 'chunk7-assisted-cold-inventory-evidence'
 $o=$a.observations;$p=$o.selectorProbe
 Assert-KmcBootstrapInventoryString $o.source.archiveSha256 $nine.descriptor.sha256
 Assert-KmcBootstrapInventoryString $o.source.runId $nine.producerRunId
 Assert-KmcAssistedSame @($o.loadedWorld.party.id) $nine.rosterIds 'selector observed nine-actor roster'
 Assert-KmcBootstrapInventoryString $p.contract 'installed-native-open-selector-optionality-v1'
 Assert-KmcBootstrapInventoryString $p.actorId $nine.petlessSpecialBodyActorId
 Assert-KmcBootstrapInventoryString $p.action 'native-ShowPartySelection'
 foreach($key in @('openCount','closeCount')){Assert-KmcOrdinaryInteger $p.$key 1 1}
 foreach($key in @('switchCount','goCount')){Assert-KmcOrdinaryInteger $p.$key 0 0}
 foreach($key in @('activeInHierarchy','closed','eligibleForNativeRemoval')){Assert-KmcChunk7Boolean $p.$key $true}
 foreach($key in @('mainCharacter','lockedCompanion','mustBeInParty','requiredByOpenedSelector')){Assert-KmcChunk7Boolean $p.$key $false}
 if([double]$p.readyAlpha-ne1){throw 'Native selector was not fully ready at its optionality observation.'}
 Assert-KmcAssistedSame @($p.selectedActorIds) $nine.characterIds 'selector preserved selected character roster'
 Assert-KmcOrdinaryInteger $set.game.loadRequestCount 1 1
 Assert-KmcOrdinaryInteger $set.game.saveRequestCount 0 0
 Assert-KmcChunk7OwnedReceiptPins $set
 [pscustomobject][ordered]@{nineActorSource=$nine;selectorRunId=$run;selectorArtifactSha256=$artifactHash
  selectorGameSha256=$gameHash;selectorOuterSha256=$outerHash;optionalActorId=$p.actorId
  eligibleForNativeRemoval=$true;nativeRosterTransitionProven=$false
  fixtureSetupOnly=$true;nativeAuthorityGranted=$false}
}
