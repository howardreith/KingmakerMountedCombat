[CmdletBinding()]
param([string]$RepositoryRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
. (Join-Path $RepositoryRoot 'scripts/runtime/Chunk7OwnedPairsFixtures.ps1')

$lab=Get-KmcLabRoot
$producerRoot=Join-Path $lab 'runtime-evidence/20261020-c7b-world71-b53s1-small'
$producer=Read-KmcJson (Join-Path $producerRoot 'chunk7-small-fixture-evidence.json')
$consumer=Read-KmcJson (Join-Path $lab 'runtime-evidence/20261020-c7b-world86-b72s1-small-ordinary/runtime-request.json')
$sourceArtifact=Join-Path $producerRoot 'chunk7-small-fixture-evidence.json'
$selection=[pscustomobject][ordered]@{
 case='small-fixture-persistence-save';sourceRunId='20261020-c7b-world71-b53s1-small'
 sourceArtifactSha256=(Get-KmcSha256 $sourceArtifact)
 sourceArchiveSha256=$producer.observations.nativeWrite.sha256
 checkpointSourceRunId=$null;checkpointSourceArtifactSha256=$null
 seedId='KMC_C7_NO_MODS_OWNER_SEED';seedSha256='3405d7c813668ad646788efd746a62c1b16e1438fad1bc4bcbebc961821b924f'
 coldReceiptSha256='d188012fb1f9a19410a572b67381bfb15be5e813a215a79196ed734b49c59bfc'
 riderAId='73681ef2-b281-4bc6-bc05-2de68c1616cb';mountAId='30b79228-3fe5-48ac-aa88-2de9085837b7'
 riderBId='c4e2c853-4b72-4904-ad84-d0b7bfba7470';mountBId='0e0c7d2d-06b4-4510-9f81-d847340d1424'
 partyIds=@($producer.observations.fieldWorld.party|ForEach-Object{$_.id})
 baseGameOnly=$true;expectedNativeLoadCount=1;expectedNativeWriteCount=1
}
$request=[pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';runId='test-small-mounted-source';ownedPairs=$selection;
 persistenceCase='multi-exploration-manual';fixture=$consumer.fixture}
$pass=0;$fail=0
function Check([string]$Name,[bool]$Expected,[scriptblock]$Body){
 $actual=$false;$detail=$null
 try{&$Body;$actual=$true}catch{$detail=$_.Exception.Message}
 if($actual-eq$Expected){$script:pass++;Write-Host ('PASS '+$Name)}
 else{$script:fail++;Write-Host ('FAIL '+$Name+': '+$detail)}
}
function Copy-Selection($Value){$Value|ConvertTo-Json -Depth 20|ConvertFrom-Json}
$thirdSelection=Copy-Selection $selection;$thirdSelection.case='small-fixture-third-horse-setup';$thirdSelection.expectedNativeWriteCount=0
$thirdRequest=[pscustomobject]@{scenario=$request.scenario;runId='test-small-third-horse';ownedPairs=$thirdSelection;persistenceCase=$null;fixture=$request.fixture}
Check 'third-Horse setup accepts no matched-pool artifact' $true {
 Assert-KmcMatchedPoolArtifact $thirdRequest ([pscustomobject]@{artifacts=@()}) 'PASS'
}
Check 'third-Horse setup refuses forged matched-pool artifact' $false {
 Assert-KmcMatchedPoolArtifact $thirdRequest ([pscustomobject]@{artifacts=@([pscustomobject]@{relativePath='chunk7-matched-pool-measurements.json';kind='chunk7-matched-pool-measurements'})}) 'PASS'
}
Check 'qualified b53 source and accepted cold receipt' $true {
 $source=Get-KmcChunk7OwnedPairsSmallSource $selection 'multi-exploration-manual' $consumer.fixture $consumer
 Assert-KmcBootstrapInventoryString $source.descriptor.sha256 $selection.sourceArchiveSha256
 Assert-KmcBootstrapInventoryString $source.artifactSha256 $selection.sourceArtifactSha256
 Assert-KmcChunk7Boolean $source.fixtureSetupOnly $true;Assert-KmcChunk7Boolean $source.nativeAuthorityGranted $false
}
Check 'source hash substitution refused' $false {
 $bad=Copy-Selection $selection;$bad.sourceArtifactSha256='0'*64
 Assert-KmcChunk7OwnedPairsSelection ([pscustomobject]@{scenario=$request.scenario;runId=$request.runId;ownedPairs=$bad;persistenceCase=$request.persistenceCase})
}
Check 'cold receipt substitution refused' $false {
 $bad=Copy-Selection $selection;$bad.coldReceiptSha256='0'*64
 Assert-KmcChunk7OwnedPairsSelection ([pscustomobject]@{scenario=$request.scenario;runId=$request.runId;ownedPairs=$bad;persistenceCase=$request.persistenceCase})
}
Check 'foreign native actor refused' $false {
 $bad=Copy-Selection $selection;$bad.partyIds[0]='00000000-0000-4000-8000-000000000000'
 [void](Get-KmcChunk7OwnedPairsSmallSource $bad 'multi-exploration-manual' $consumer.fixture $consumer)
}
Check 'producer cannot borrow a cold checkpoint' $false {
 $bad=Copy-Selection $selection;$bad.checkpointSourceRunId='foreign-producer';$bad.checkpointSourceArtifactSha256='0'*64
 Assert-KmcChunk7OwnedPairsSelection ([pscustomobject]@{scenario=$request.scenario;runId=$request.runId;ownedPairs=$bad;persistenceCase=$request.persistenceCase})
}
Check 'cold case requires a distinct mounted producer' $false {
 $bad=Copy-Selection $selection;$bad.case='small-fixture-persistence-load';$bad.expectedNativeWriteCount=0
 Assert-KmcChunk7OwnedPairsSelection ([pscustomobject]@{scenario=$request.scenario;runId=$request.runId;ownedPairs=$bad;persistenceCase=$request.persistenceCase})
}
$sourceDescriptor=[pscustomobject]@{internalName=$producer.observations.nativeWrite.internalName;
 fileName=$producer.observations.nativeWrite.fileName;sha256=$producer.observations.nativeWrite.sha256;
 length=$producer.observations.nativeWrite.length;lastWriteTimeUtcTicks=$producer.observations.nativeWrite.lastWriteTimeUtcTicks;
 gameId=$producer.observations.nativeWrite.gameId;gameName=$producer.observations.nativeWrite.gameName;area=$producer.observations.nativeWrite.area}
$request|Add-Member -NotePropertyName persistenceLoad -NotePropertyValue $sourceDescriptor
Check 'Small producer reserves the next genuine Manual number' $true {
 $o=[pscustomobject]@{ownedOutputAdmission=[pscustomobject]@{number=305;leaf='Manual_305_KMC_C7_SMALL_WORLD.zks'}}
 Assert-KmcBootstrapInventoryString (Get-KmcChunk7OwnedOutputLeaf $request $o) 'Manual_305_KMC_C7_SMALL_WORLD.zks'
}
Check 'Small output cannot skip a native Manual number' $false {
 $o=[pscustomobject]@{ownedOutputAdmission=[pscustomobject]@{number=306;leaf='Manual_306_KMC_C7_SMALL_WORLD.zks'}}
 [void](Get-KmcChunk7OwnedOutputLeaf $request $o)
}
$coldSelection=Copy-Selection $selection;$coldSelection.case='small-fixture-persistence-load';
$coldSelection.expectedNativeWriteCount=0;$coldSelection.checkpointSourceRunId='test-small-mounted-producer';
$coldSelection.checkpointSourceArtifactSha256='2'*64
$coldRequest=[pscustomobject]@{scenario=$request.scenario;runId='test-small-mounted-consumer';ownedPairs=$coldSelection;
 persistenceCase=$request.persistenceCase;fixture=$consumer.fixture;
 persistenceLoad=[pscustomobject]@{internalName='KMC_C7_SMALL_WORLD';fileName='Manual_305_KMC_C7_SMALL_WORLD.zks';
 sha256=('1'*64);length=100;lastWriteTimeUtcTicks=[long]639000000000000000;
 gameId=$sourceDescriptor.gameId;gameName=$sourceDescriptor.gameName;area=$sourceDescriptor.area}}
Check 'distinct mounted Small Manual admits a closed cold descriptor' $true {Assert-KmcChunk7OwnedPairsDescriptor $coldRequest}
Check 'Small cold descriptor cannot alias the unmounted F09 source' $false {
 $bad=Copy-Selection $coldRequest;$bad.persistenceLoad.sha256=$selection.sourceArchiveSha256
 Assert-KmcChunk7OwnedPairsDescriptor $bad
}
Check 'Small cold descriptor refuses exhausted Manual number' $false {
 $bad=Copy-Selection $coldRequest;$bad.persistenceLoad.fileName='Manual_2147483647_KMC_C7_SMALL_WORLD.zks'
 Assert-KmcChunk7OwnedPairsDescriptor $bad
}
$ownerRows=@([pscustomobject]@{riderId=$selection.riderAId;mountId=$selection.mountAId;sourceGuid='a'*32;rank=1;profileId='medium-humanoid-horse-v1'},
 [pscustomobject]@{riderId=$selection.riderBId;mountId=$selection.mountBId;sourceGuid='a'*32;rank=1;profileId='small-default-halfling-horse-v1'})
$custody=[pscustomobject]@{sourceCase='owner-seed-default-halfling-horse-manual';partyIds=@($selection.partyIds);
 currentCharacterIds=@($selection.riderAId,$selection.riderBId);remoteCapturedIds=@();arrangement=[pscustomobject]@{money=1};pairs=$ownerRows}
$archive=[pscustomobject]@{SchemaVersion=3;Pairs=@([pscustomobject]@{Rider=[pscustomobject]@{Id=$selection.riderAId}},
 [pscustomobject]@{Rider=[pscustomobject]@{Id=$selection.riderBId}})}
$producerArtifact=[pscustomobject]@{runId=$coldSelection.checkpointSourceRunId;status='PASS';commit=$consumer.commit;
 dllSha256=$consumer.dllSha256;dllMvid=$consumer.dllMvid;observations=[pscustomobject]@{checkpoint='multi-exploration-manual';
 cold=$false;processId=111;nativeWrite=[pscustomobject]@{sha256=('1'*64);archive=$archive};inheritedFixture=$custody;inheritedCleanup=$custody}}
$coldRequest|Add-Member -NotePropertyName commit -NotePropertyValue $consumer.commit
$coldRequest|Add-Member -NotePropertyName dllSha256 -NotePropertyValue $consumer.dllSha256
$coldRequest|Add-Member -NotePropertyName dllMvid -NotePropertyValue $consumer.dllMvid
$coldArtifact=[pscustomobject]@{status='PASS';observations=[pscustomobject]@{checkpoint='multi-exploration-manual';cold=$true;
 processId=222;loadedArchive=$archive;inheritedFixture=$custody}}
Check 'separate cold readback matches the mounted producer' $true {
 Assert-KmcChunk7SmallMountedColdSource $coldRequest $producerArtifact $coldArtifact
}
Check 'different cold archive is refused' $false {
 $bad=Copy-Selection $coldArtifact;$bad.observations.loadedArchive.SchemaVersion=2
 Assert-KmcChunk7SmallMountedColdSource $coldRequest $producerArtifact $bad
}
Check 'same-process cold claim is refused' $false {
 $bad=Copy-Selection $coldArtifact;$bad.observations.processId=111
 Assert-KmcChunk7SmallMountedColdSource $coldRequest $producerArtifact $bad
}
Write-Host ('Small mounted source checks: '+$pass+' PASS, '+$fail+' FAIL')
if($fail-ne0){exit 1}
