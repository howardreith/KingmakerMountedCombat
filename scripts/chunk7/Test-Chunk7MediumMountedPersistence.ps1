[CmdletBinding()]
param([string]$RepositoryRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
. (Join-Path $RepositoryRoot 'scripts/runtime/Chunk7OwnedPairsFixtures.ps1')

$producer=Read-KmcJson (Join-Path (Get-KmcLabRoot) 'runtime-evidence/20261021-c7b-world118-b114s1-medium-paid/chunk7-small-fixture-evidence.json')
$coldRequest=Read-KmcJson (Join-Path (Get-KmcLabRoot) 'runtime-evidence/20261021-c7b-world118-b114s2-medium-cold/runtime-request.json')
$candidate=[pscustomobject]@{commit='cd1f3714483f7c1b36a6fea26bfb6adcf27afae9';dllSha256=$coldRequest.dllSha256;dllMvid=$coldRequest.dllMvid}
$selection=[pscustomobject][ordered]@{
 case='medium-fixture-persistence-save';sourceRunId='20261021-c7b-world118-b114s1-medium-paid'
 sourceArtifactSha256='c890e8b2e5aa196407dda420d6b69deb5173ade26afe74e456a827ce1148cd4b'
 sourceArchiveSha256='d25aa931174027b1f1f661db8f5a0db3054fcbe89e930ada1747f327e41a65e3'
 checkpointSourceRunId=$null;checkpointSourceArtifactSha256=$null
 seedId='KMC_C7_NO_MODS_OWNER_SEED';seedSha256='3405d7c813668ad646788efd746a62c1b16e1438fad1bc4bcbebc961821b924f'
 coldReceiptSha256='9183cd19a9df95678eda235a632701a429b57d95bfd8fdb7eb6a5eeef95b6b17'
 riderAId='73681ef2-b281-4bc6-bc05-2de68c1616cb';mountAId='a3e58763-2386-4ee7-a96d-8ccc21123b22'
 riderBId='93f5f59d-403a-4ab2-a5e2-f91ff4d57f5f';mountBId='af4e8966-4d8d-4573-a16c-c2b402a71693'
 partyIds=@($producer.observations.fieldWorld.party|ForEach-Object{$_.id})
 baseGameOnly=$true;expectedNativeLoadCount=1;expectedNativeWriteCount=1
}
$request=[pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';runId='test-medium-mounted-source';ownedPairs=$selection;
 persistenceCase='multi-exploration-manual';fixture=$coldRequest.fixture}
$pass=0;$fail=0
function Check([string]$Name,[bool]$Expected,[scriptblock]$Action){
 $actual=$false;$detail=$null
 try{&$Action;$actual=$true}catch{$detail=$_.Exception.Message}
 if($actual-eq$Expected){$script:pass++;Write-Host ('PASS '+$Name)}
 else{$script:fail++;Write-Host ('FAIL '+$Name+': '+$detail)}
}
function Clone($Value){$Value|ConvertTo-Json -Depth 20|ConvertFrom-Json}
Check 'distinct Medium selection and real source admitted for mounted Manual' $true {
 Assert-KmcChunk7OwnedPairsSelection $request
 $source=Get-KmcChunk7OwnedPairsMediumSource $selection 'multi-exploration-manual' $request.fixture $candidate
 Assert-KmcBootstrapInventoryString $source.descriptor.sha256 $selection.sourceArchiveSha256
 Assert-KmcBootstrapInventoryString $source.artifactSha256 $selection.sourceArtifactSha256
 Assert-KmcChunk7Boolean $source.fixtureSetupOnly $true
 Assert-KmcChunk7Boolean $source.nativeAuthorityGranted $false
 $request|Add-Member -NotePropertyName persistenceLoad -NotePropertyValue $source.descriptor
 $admission=[pscustomobject]@{ownedOutputAdmission=[pscustomobject]@{number=305;leaf='Manual_305_KMC_C7_MEDIUM_WORLD.zks'}}
 Assert-KmcBootstrapInventoryString (Get-KmcChunk7OwnedOutputLeaf $request $admission) 'Manual_305_KMC_C7_MEDIUM_WORLD.zks'
}
Check 'Medium selection cannot impersonate Small case' $false {
 $bad=Clone $selection;$bad.case='small-fixture-persistence-save'
 Assert-KmcChunk7OwnedPairsSelection ([pscustomobject]@{scenario=$request.scenario;runId=$request.runId;ownedPairs=$bad;persistenceCase=$request.persistenceCase})
}
Check 'altered separate cold receipt refused' $false {
 $bad=Clone $selection;$bad.coldReceiptSha256='0'*64
 Assert-KmcChunk7OwnedPairsSelection ([pscustomobject]@{scenario=$request.scenario;runId=$request.runId;ownedPairs=$bad;persistenceCase=$request.persistenceCase})
}
Check 'foreign paid rider refused' $false {
 $bad=Clone $selection;$bad.riderBId='fd213210-71cd-41b0-b35b-68e1b18e9b43'
 Assert-KmcChunk7OwnedPairsSelection ([pscustomobject]@{scenario=$request.scenario;runId=$request.runId;ownedPairs=$bad;persistenceCase=$request.persistenceCase})
}
Check 'source archive substitution refused' $false {
 $bad=Clone $selection;$bad.sourceArchiveSha256='0'*64
 [void](Get-KmcChunk7OwnedPairsMediumSource $bad 'multi-exploration-manual' $request.fixture $candidate)
}
Check 'cold Medium case requires distinct mounted producer' $false {
 $bad=Clone $selection;$bad.case='medium-fixture-persistence-load';$bad.expectedNativeWriteCount=0
 Assert-KmcChunk7OwnedPairsSelection ([pscustomobject]@{scenario=$request.scenario;runId=$request.runId;ownedPairs=$bad;persistenceCase=$request.persistenceCase})
}
Check 'output cannot skip reserved native Manual number' $false {
 $admission=[pscustomobject]@{ownedOutputAdmission=[pscustomobject]@{number=306;leaf='Manual_306_KMC_C7_MEDIUM_WORLD.zks'}}
 [void](Get-KmcChunk7OwnedOutputLeaf $request $admission)
}
$coldSelection=Clone $selection
$coldSelection.case='medium-fixture-persistence-load';$coldSelection.expectedNativeWriteCount=0
$coldSelection.checkpointSourceRunId='synthetic-medium-mounted-producer'
$coldSelection.checkpointSourceArtifactSha256='5'*64
$coldRequest=Clone $request;$coldRequest.runId='synthetic-medium-mounted-consumer'
$coldRequest.ownedPairs=$coldSelection
$coldRequest.persistenceLoad=[pscustomobject]@{internalName='KMC_C7_MEDIUM_WORLD';fileName='Manual_305_KMC_C7_MEDIUM_WORLD.zks';
 sha256=('1'*64);length=1234;lastWriteTimeUtcTicks=1;gameId=$request.persistenceLoad.gameId;
 gameName=$request.persistenceLoad.gameName;area=$request.persistenceLoad.area}
Check 'distinct Medium mounted Manual admits a closed cold descriptor' $true {
 Assert-KmcChunk7OwnedPairsDescriptor $coldRequest
}
$pairs=@([pscustomobject]@{riderId=$selection.riderAId;mountId=$selection.mountAId;sourceGuid='a'*32;rank=1;profileId='medium-humanoid-horse-v1'},
 [pscustomobject]@{riderId=$selection.riderBId;mountId=$selection.mountBId;sourceGuid='b'*32;rank=1;profileId='medium-humanoid-horse-v1'})
$custody=[pscustomobject]@{sourceCase='owner-seed-default-human-horse-manual';partyIds=@($selection.partyIds);
 currentCharacterIds=@($selection.riderAId,$selection.riderBId);remoteCapturedIds=@();arrangement=[pscustomobject]@{money=1};pairs=$pairs}
$archive=[pscustomobject]@{SchemaVersion=3;Pairs=@([pscustomobject]@{Rider=[pscustomobject]@{Id=$selection.riderAId}},
 [pscustomobject]@{Rider=[pscustomobject]@{Id=$selection.riderBId}})}
$producerArtifact=[pscustomobject]@{runId=$coldSelection.checkpointSourceRunId;status='PASS';commit=$candidate.commit;
 dllSha256=$candidate.dllSha256;dllMvid=$candidate.dllMvid;observations=[pscustomobject]@{checkpoint='multi-exploration-manual';
 cold=$false;processId=111;nativeWrite=[pscustomobject]@{sha256=$coldRequest.persistenceLoad.sha256;archive=$archive};
 inheritedFixture=$custody;inheritedCleanup=$custody}}
$coldRequest|Add-Member -NotePropertyName commit -NotePropertyValue $candidate.commit
$coldRequest|Add-Member -NotePropertyName dllSha256 -NotePropertyValue $candidate.dllSha256
$coldRequest|Add-Member -NotePropertyName dllMvid -NotePropertyValue $candidate.dllMvid
$coldArtifact=[pscustomobject]@{status='PASS';observations=[pscustomobject]@{checkpoint='multi-exploration-manual';cold=$true;
 processId=222;loadedArchive=$archive;inheritedFixture=$custody}}
Check 'Medium separate cold readback matches the mounted producer' $true {
 Assert-KmcChunk7PaidMountedColdSource $coldRequest $producerArtifact $coldArtifact
}
Check 'Medium cold readback refuses another process identity claim' $false {
 $bad=Clone $coldArtifact;$bad.observations.processId=111
 Assert-KmcChunk7PaidMountedColdSource $coldRequest $producerArtifact $bad
}
Write-Host ('TOTAL PASS='+$pass+' FAIL='+$fail)
if($fail-ne0){exit 1}
