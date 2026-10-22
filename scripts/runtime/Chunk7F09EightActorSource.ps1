Set-StrictMode -Version Latest

# Read-only admission for the next F09 derivative. This binds the genuine
# producer Manual AND the separately completed cold consumer; neither receipt
# grants authority to create a Horse or to write into the producer's save root.
function Get-KmcChunk7F09EightActorSource($Fixture){
 $producer='20261021-c7b-world171-b171s1-thirdhorse-manual'
 $cold='20261021-c7b-world174-b174s1-thirdhorse-cold'
 $artifactHash='57ab380ae5c4c95fbed1647446b3ca264f7e140c91a3b7ea4bb41037085450b1'
 $archiveHash='f70a54865c86a599c3b25f6f2574abde2f663cd07b82472eac4bcbe245ea8c84'
 $coldArtifactHash='4439fbea3608dad76634174b555f9d4ea243f2b4d0480029b03b7de66f9c8766'
 $coldGameHash='56f234f784dc62b5199a1002670c1650cf5a71f545868613af482b83d94f2e13'
 $coldOuterHash='6321b9b4ead6071c264f25a37b4ceb2dbccdb57a6c3468e76bcb6d066dffa395'
 $selection=[pscustomobject][ordered]@{case='third-horse-manual-readonly-inventory';sourceRunId=$producer
  sourceArtifactSha256=$artifactHash;sourceArchiveSha256=$archiveHash;baseGameOnly=$true
  expectedNativeLoadCount=1;expectedNativeWriteCount=0}
 $set=Get-KmcChunk7OwnedReceiptSet $cold 'chunk7-assisted-cold-inventory-evidence.json'
 $r=$set.request
 # The recorded cold candidate proves its own receipt. This source gate does
 # not certify an unbuilt future consumer or grant cross-candidate native use.
 $recorded=Get-KmcChunk7RecordedCandidate $r
 $source=Get-KmcChunk7ThirdHorseColdSource $selection $Fixture $recorded
 Assert-KmcAssistedColdSourcePins $source $true
 Assert-KmcBootstrapInventoryString $r.scenario 'chunk7-assisted-cold-inventory'
 Assert-KmcChunk7AssistedColdInventoryDescriptor $r
 if((ConvertTo-KmcChunk7Canonical $r.assistedColdInventory)-cne(ConvertTo-KmcChunk7Canonical $selection)-or
    (ConvertTo-KmcChunk7Canonical $r.persistenceLoad)-cne(ConvertTo-KmcChunk7Canonical $source.descriptor)){
  throw 'Eight-actor cold consumer substituted its declared producer or native Manual.'
 }
 Assert-KmcChunk7OwnedSourceCompletion $set $cold 'chunk7-assisted-cold-inventory' $Fixture $recorded $archiveHash 0
 foreach($pair in @(@('runtime-result.json',$coldOuterHash),@('runtime-game-result.json',$coldGameHash),
   @('chunk7-assisted-cold-inventory-evidence.json',$coldArtifactHash))){
  Assert-KmcBootstrapInventoryString $set.pins[(Join-Path $set.root $pair[0])] $pair[1]
 }
 Assert-KmcChunk7AssistedColdInventoryArtifact $r $set.manifest 'PASS'
 $path=Join-Path $set.root 'chunk7-assisted-cold-inventory-evidence.json'
 $a=Read-KmcChunk7ManifestArtifact $r $set.manifest 'chunk7-assisted-cold-inventory-evidence.json' 'chunk7-assisted-cold-inventory-evidence'
 $o=$a.observations
 Assert-KmcBootstrapInventoryString $o.source.runId $producer
 Assert-KmcBootstrapInventoryString $o.source.artifactSha256 $artifactHash
 Assert-KmcBootstrapInventoryString $o.source.archiveSha256 $archiveHash
 Assert-KmcBootstrapInventoryString $o.source.archiveLeaf 'Manual_305_KMC_C7_SMALL_WORLD.zks'
 Assert-KmcBootstrapInventoryString $o.source.kind 'qualified-assisted-third-horse-manual'
 Assert-KmcChunk7Boolean $o.source.assistedSetupOnly $true
 Assert-KmcChunk7Boolean $o.noPreparation.complete $true
 Assert-KmcOrdinaryInteger $o.noPreparation.dropped 0 0
 Assert-KmcOrdinaryInteger $o.noPreparation.errors 0 0
 if(@($o.noPreparation.events).Count-ne0){throw 'Accepted cold receipt observed a native preparation or restoration event.'}
 foreach($key in @('resourceWrites','xpWrites','moneyWrites','mountRequests','choiceRequests')){
  Assert-KmcOrdinaryInteger $o.$key 0 0
 }
 $party=@($o.loadedWorld.party);$facts=@($o.thirdHorseFacts)
 if($party.Count-ne8-or$facts.Count-ne3-or
    @($party.id|Sort-Object -CaseSensitive -Unique).Count-ne8-or
    @($facts.riderId|Sort-Object -CaseSensitive -Unique).Count-ne3-or
    @($facts.mountId|Sort-Object -CaseSensitive -Unique).Count-ne3){
  throw 'Accepted cold receipt lost its eight distinct actors or three distinct Horses.'
 }
 foreach($pair in @(@('5dd7bd1d-ae4c-4df7-9687-50fceb027344','Medium'),
   @('8265f8cf-0969-4ec7-a266-2448de91c8c4','Small'))){
  $row=@($o.nativeProfileInventory.party|Where-Object{$_.id-ceq$pair[0]})
  if($row.Count-ne1-or$row[0].effectiveSize-cne$pair[1]-or$row[0].petId-or
     $row[0].nativeLevelUpReadiness.characterLevel-ne4-or
     $row[0].nativeLevelUpReadiness.experience-ne9000-or
     $row[0].nativeLevelUpReadiness.canLevelUp-ne$false){
   throw 'The accepted eight-actor cold inventory no longer has its exact petless stock fixture lead.'
  }
 }
 Assert-KmcChunk7OwnedReceiptPins $set
 Assert-KmcAssistedColdSourcePins $source $true
 [pscustomobject][ordered]@{path=$source.path;descriptor=$source.descriptor
  producerRunId=$producer;producerArtifactPath=$source.artifactPath;producerArtifactSha256=$artifactHash
  producerProcessId=$source.sourceProcessId;coldRunId=$cold;coldArtifactPath=$path
  coldArtifactSha256=$coldArtifactHash;coldGameSha256=$coldGameHash;coldOuterSha256=$coldOuterHash
  rosterIds=@($party.id);horseFacts=$facts;fixtureSetupOnly=$true;nativeAuthorityGranted=$false}
}
