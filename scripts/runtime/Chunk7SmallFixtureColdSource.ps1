Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Chunk7SmallFixtureFixtures.ps1')

# F09's paid Halfling Manual is a new native producer lineage. This read-only
# gate proves that lineage before a separate cold process may copy the archive;
# it does not grant a load, write, profile or mounted-action authority.
function Get-KmcChunk7SmallFixtureColdSource {
 param([string]$SourceRunId,[string]$ArtifactSha256,[string]$ArchiveSha256,$Fixture,$Candidate)
 if($SourceRunId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'-or
    $ArtifactSha256-cnotmatch'^[a-f0-9]{64}$'-or$ArchiveSha256-cnotmatch'^[a-f0-9]{64}$'){
  throw 'F09 cold source requires one canonical producer run and exact artifact/archive hashes.'
 }
 $producer=Get-KmcSmallFixtureHistoricalArtifact $SourceRunId 'chunk7-small-fixture-evidence.json' 'chunk7-small-fixture-evidence' $ArtifactSha256 1
 $request=$producer.request;$artifact=$producer.artifact
 Assert-KmcBootstrapInventoryString $request.scenario 'chunk7-small-fixture-preparation'
 Assert-KmcChunk7SmallFixtureDescriptor $request
 foreach($key in @('commit','dllSha256','dllMvid')){Assert-KmcBootstrapInventoryString $request.$key $Candidate.$key}
 # The package manifest calls this value `version`; the in-game request calls
 # it `productVersion`. Both must bind the same exact compiled candidate.
 $version=if($null-ne$Candidate.PSObject.Properties['version']){$Candidate.version}else{$Candidate.productVersion}
 Assert-KmcBootstrapInventoryString $request.productVersion $version
 foreach($key in @('working','baseline')){Assert-KmcBootstrapInventoryString $request.fixture.$key.sha256 $Fixture.$key.sha256}
 Assert-KmcChunk7SmallFixtureEvidence $request $artifact
 Assert-KmcChunk7SmallFixtureOutput $request $artifact
 $write=$artifact.observations.nativeWrite
 Assert-KmcBootstrapInventoryString $write.sha256 $ArchiveSha256
 foreach($key in @('working','baseline')){
  if($write.sha256-ceq$Fixture.$key.sha256-or$write.gameId-ceq$Fixture.$key.gameId){throw 'F09 native Manual aliases a protected fixture.'}
 }
 $saveRoot=Get-KmcAssistedSaveRoot $request
 $archive=Assert-KmcChildPath $write.path $saveRoot 'F09 native Manual source'
 Assert-KmcBootstrapInventoryString $archive (Join-Path $saveRoot $write.fileName)
 Assert-KmcAssistedSettledFile $archive $write 'F09 immutable native Manual source'
 $ownerPath=Join-Path (Split-Path $saveRoot) 'owner.json'
 $owner=Read-KmcSmallFixturePinnedJson $ownerPath $null 16384
 Assert-KmcAssistedObjectValue $owner.value @('runId','transactionToken','scenario','persistenceCase','sourceSha256','saveRoot') 'F09 producer owner'
 foreach($pair in @(@('runId',$request.runId),@('transactionToken',$request.transactionToken),@('scenario',$request.scenario),@('sourceSha256',$request.persistenceLoad.sha256),@('saveRoot',$saveRoot))){
  Assert-KmcBootstrapInventoryString $owner.value.($pair[0]) $pair[1]
 }
 if($null-ne$owner.value.persistenceCase-and$owner.value.persistenceCase-cne''){throw 'F09 producer owner has a foreign persistence case.'}
 $evidenceRoot=Join-Path (Join-Path (Get-KmcLabRoot) 'runtime-evidence') $SourceRunId
 $isolation=Read-KmcSmallFixturePinnedJson (Join-Path $evidenceRoot 'base-game-isolation-result.json') $null 16384
 $profile=Read-KmcSmallFixturePinnedJson (Join-Path $evidenceRoot 'profile-cache-changes.json') $null 65536
 $loaderInventory=Read-KmcSmallFixturePinnedJson (Join-Path $evidenceRoot 'runtime-loader-inventory.json') $null 1048576
 Assert-KmcBootstrapInventoryString $isolation.value.runId $SourceRunId
 Assert-KmcBootstrapInventoryString $isolation.value.stagingMode 'base-game-only-kmc'
 foreach($key in @('stageVerified','dependenciesUnchanged','processExited','modsRestored','saveProtectionPassed','gameReportedPass','nativeInventoryObserved','nativeCompatibilityQualified')){
  Assert-KmcChunk7Boolean $isolation.value.$key $true
 }
 Assert-KmcBootstrapInventoryString $isolation.value.runtimeLoaderInventorySha256 $loaderInventory.sha256
 Assert-KmcOrdinaryInteger $profile.value.schemaVersion 1 1
 Assert-KmcBootstrapInventoryString $profile.value.runId $SourceRunId
 Assert-KmcBootstrapInventoryString $profile.value.policy 'native-achievement-cache-settled-size'
 if($profile.value.acceptedNativeCacheChanges-isnot[Array]-or$profile.value.profileBytesRestoredExactly-isnot[bool]-or
    $profile.value.profileBytesRestoredExactly-ne($profile.value.acceptedNativeCacheChanges.Count-eq0)){
  throw 'F09 source profile receipt does not distinguish exact restoration from approved cache churn.'
 }
 foreach($pin in $producer.receiptPins){Assert-KmcAssistedSettledFile $pin.path $pin 'F09 producer receipt'}
 Assert-KmcAssistedSettledFile $owner.path $owner 'F09 producer owner'
 foreach($pin in @($isolation,$profile,$loaderInventory)){Assert-KmcAssistedSettledFile $pin.path $pin 'F09 base-only/profile receipt'}
 [pscustomobject]@{path=$archive;descriptor=[pscustomobject][ordered]@{
   internalName=$write.internalName;fileName=$write.fileName;sha256=$write.sha256;
   length=[long]$write.length;lastWriteTimeUtcTicks=[long]$write.lastWriteTimeUtcTicks;
   gameId=$write.gameId;gameName=$write.gameName;area=$write.area};
  artifactPath=$producer.artifactPath;artifactSha256=$producer.artifactSha256;
  artifactLength=$producer.artifactLength;sourceRunId=$SourceRunId;
  sourceProcessId=[int]$artifact.observations.processId;
  receiptPins=@($producer.receiptPins)+@($owner,$isolation,$profile,$loaderInventory);
  fixtureSetupOnly=$true;producerAncestry=$false;nativeAuthorityGranted=$false}
}
