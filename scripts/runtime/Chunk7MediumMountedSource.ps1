Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Chunk7SmallFixtureColdSource.ps1')

# Batch114 is fixture ancestry, not a claim that its preview.82 DLL qualified
# a later consumer. Recheck both original native processes before admitting an
# independent copy of the unmounted paid Human/Horse Manual.
function Get-KmcChunk7MediumMountedSource($Fixture,$Consumer){
 $producerRun='20261021-c7b-world118-b114s1-medium-paid'
 $producerArtifact='c890e8b2e5aa196407dda420d6b69deb5173ade26afe74e456a827ce1148cd4b'
 $archiveSha='d25aa931174027b1f1f661db8f5a0db3054fcbe89e930ada1747f327e41a65e3'
 $coldRun='20261021-c7b-world118-b114s2-medium-cold'
 $coldArtifactSha='9183cd19a9df95678eda235a632701a429b57d95bfd8fdb7eb6a5eeef95b6b17'
 $producerCandidate=[pscustomobject]@{commit='fcc9a51646b7adddcff9a9887fd341348bd21a63';
  dllSha256='d8ae19aceef341da0b54e6cc5cbeaa670b91a67309f847a95d436ed9c196a93b';
  dllMvid='35a888c7-15f1-431c-9775-17dd7669d5b8';version='0.1.0-chunk7b-preview.82'}
 foreach($key in @('commit','dllSha256','dllMvid')){
  if($Consumer.$key-isnot[string]-or$Consumer.$key-cnotmatch $(if($key-ceq'commit'){'^[a-f0-9]{40}$'}elseif($key-ceq'dllSha256'){'^[a-f0-9]{64}$'}else{'^[a-f0-9-]{36}$'})){
   throw ('Medium mounted consumer lacks canonical '+$key+'.')
  }
 }
 $source=Get-KmcChunk7SmallFixtureColdSource $producerRun $producerArtifact $archiveSha $Fixture $producerCandidate 'owner-seed-default-human-horse-manual'
 $cold=Get-KmcSmallFixtureHistoricalArtifact $coldRun 'chunk7-assisted-cold-inventory-evidence.json' 'chunk7-assisted-cold-inventory-evidence' $coldArtifactSha 0
 $request=$cold.request;$artifact=$cold.artifact
 foreach($key in @('commit','dllSha256','dllMvid')){Assert-KmcBootstrapInventoryString $request.$key $producerCandidate.$key}
 Assert-KmcBootstrapInventoryString $request.scenario 'chunk7-assisted-cold-inventory'
 Assert-KmcBootstrapInventoryString $request.assistedColdInventory.case 'medium-fixture-manual-readonly-inventory'
 foreach($pair in @(@('sourceRunId',$producerRun),@('sourceArtifactSha256',$producerArtifact),@('sourceArchiveSha256',$archiveSha))){
  Assert-KmcBootstrapInventoryString $request.assistedColdInventory.($pair[0]) $pair[1]
 }
 Assert-KmcChunk7Boolean $request.assistedColdInventory.baseGameOnly $true
 Assert-KmcOrdinaryInteger $request.assistedColdInventory.expectedNativeLoadCount 1 1
 Assert-KmcOrdinaryInteger $request.assistedColdInventory.expectedNativeWriteCount 0 0
 foreach($key in @('internalName','fileName','sha256','length','lastWriteTimeUtcTicks','gameId','gameName','area')){
  Assert-KmcAssistedSame $request.persistenceLoad.$key $source.descriptor.$key ('Medium cold input '+$key)
 }
 $producer=Read-KmcSmallFixturePinnedJson $source.artifactPath $producerArtifact
 Assert-KmcChunk7AssistedColdInventoryEvidence $request $artifact $producer.value 'PASS'
 $o=$artifact.observations
 Assert-KmcBootstrapInventoryString $o.source.kind 'qualified-owner-seed-derived-medium-fixture-manual'
 foreach($pair in @(@('runId',$producerRun),@('artifactSha256',$producerArtifact),@('archiveSha256',$archiveSha))){
  Assert-KmcBootstrapInventoryString $o.source.($pair[0]) $pair[1]
 }
 Assert-KmcChunk7Boolean $o.source.assistedSetupOnly $true
 Assert-KmcChunk7Boolean $o.source.sameCandidate $true
 Assert-KmcOrdinaryInteger $o.loadedWorld.owners 0 0
 Assert-KmcOrdinaryInteger $o.loadedWorld.slots 0 0
 Assert-KmcOrdinaryInteger $o.cleanup.authority.authorizedLoads 1 1
 Assert-KmcOrdinaryInteger $o.cleanup.authority.authorizedWrites 0 0
 $party=@($o.loadedWorld.party)
 if($party.Count-ne7){throw 'Medium cold roster is not the seven-actor native field.'}
 $riders=@('73681ef2-b281-4bc6-bc05-2de68c1616cb','93f5f59d-403a-4ab2-a5e2-f91ff4d57f5f')
 $horses=@('a3e58763-2386-4ee7-a96d-8ccc21123b22','af4e8966-4d8d-4573-a16c-c2b402a71693')
 for($i=0;$i-lt2;$i++){
  $r=@($party|Where-Object id -CEQ $riders[$i]);$h=@($party|Where-Object id -CEQ $horses[$i])
  if($r.Count-ne1-or$h.Count-ne1-or$r[0].petId-cne$horses[$i]-or$h[0].masterId-cne$riders[$i]-or
   $h[0].blueprint-cne'4016c7db400ab721ff125aef9e65e202'){
   throw 'Medium cold source lost an exact reciprocal Horse.'
  }
 }
 $root=Assert-KmcChildPath (Join-Path (Get-KmcLabRoot) ('runtime-evidence/'+$coldRun)) (Join-Path (Get-KmcLabRoot) 'runtime-evidence') 'Medium cold receipt root'
 $profile=Read-KmcSmallFixturePinnedJson (Join-Path $root 'profile-cache-changes.json') $null 65536
 $isolation=Read-KmcSmallFixturePinnedJson (Join-Path $root 'base-game-isolation-result.json') $null 16384
 $transaction=Read-KmcSmallFixturePinnedJson (Join-Path (Get-KmcLabRoot) ('runtime-state/transactions/'+$coldRun+'.json')) $null 65536
 Assert-KmcBootstrapInventoryString $profile.value.runId $coldRun
 Assert-KmcChunk7Boolean $profile.value.profileBytesRestoredExactly $true
 Assert-KmcAssistedArray $profile.value.acceptedNativeCacheChanges 0 0 'Medium cold profile exceptions'
 Assert-KmcBootstrapInventoryString $isolation.value.runId $coldRun
 Assert-KmcBootstrapInventoryString $isolation.value.stagingMode 'base-game-only-kmc'
 foreach($key in @('stageVerified','dependenciesUnchanged','processExited','modsRestored','saveProtectionPassed','gameReportedPass','nativeInventoryObserved','nativeCompatibilityQualified')){
  Assert-KmcChunk7Boolean $isolation.value.$key $true
 }
 Assert-KmcBootstrapInventoryString $transaction.value.phase 'restored'
 Assert-KmcBootstrapInventoryString $transaction.value.runId $coldRun
 foreach($pin in @($profile,$isolation,$transaction,$producer)){Assert-KmcAssistedSettledFile $pin.path $pin 'Medium paid fixture lineage receipt'}
 foreach($pin in $source.receiptPins){Assert-KmcAssistedSettledFile $pin.path $pin 'Medium producer lineage receipt'}
 [pscustomobject]@{path=$source.path;descriptor=$source.descriptor;artifactPath=$source.artifactPath;
  artifactSha256=$producerArtifact;artifactLength=$source.artifactLength;sourceRunId=$producerRun;
  producerCandidate=$producerCandidate;coldRunId=$coldRun;coldArtifactPath=$cold.artifactPath;
  coldArtifactSha256=$coldArtifactSha;coldReceiptPins=@($cold.receiptPins)+@($profile,$isolation,$transaction);
  fixtureSetupOnly=$true;producerAncestry=$false;sameCandidate=$false;nativeAuthorityGranted=$false}
}
