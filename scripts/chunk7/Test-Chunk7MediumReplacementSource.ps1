[CmdletBinding()]
param([string]$RepositoryRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
. (Join-Path $RepositoryRoot 'scripts/runtime/Chunk7ProgressionFixtures.ps1')

$lab=Get-KmcLabRoot
$producerRun='20261021-c7b-world119-b115s1-medium-mounted'
$coldRun='20261021-c7b-world119-b115s2-medium-mounted-cold'
$producer=Read-KmcJson (Join-Path $lab ('runtime-evidence/'+$producerRun+'/chunk7-persistence-evidence.json'))
$cold=Read-KmcJson (Join-Path $lab ('runtime-evidence/'+$coldRun+'/chunk7-persistence-evidence.json'))
$coldRequest=Read-KmcJson (Join-Path $lab ('runtime-evidence/'+$coldRun+'/runtime-request.json'))
$manifest=Read-KmcJson (Join-Path $lab 'artifacts/KingmakerMountedCombat-0.1.0-chunk7b-preview.82-c7b-world119-diagnostic.zip.manifest.json')
$body=(Read-KmcJson (Join-Path $lab 'runtime-evidence/20261021-c7b-world118-b114s1-medium-paid/chunk7-small-fixture-evidence.json')).observations.savedBodyObservation
$selection=[pscustomobject][ordered]@{
 case='medium-horse-native-retrain-mammoth';sourceRunId=$producerRun
 sourceArtifactSha256=(Get-KmcSha256 (Join-Path $lab ('runtime-evidence/'+$producerRun+'/chunk7-persistence-evidence.json')))
 sourceArchiveSha256=$producer.observations.nativeWrite.sha256
 inventoryRunId=$coldRun
 inventoryArtifactSha256=(Get-KmcSha256 (Join-Path $lab ('runtime-evidence/'+$coldRun+'/chunk7-persistence-evidence.json')))
 actorId=$coldRequest.ownedPairs.riderBId;petId=$coldRequest.ownedPairs.mountBId
 controlActorId=$coldRequest.ownedPairs.riderAId;controlPetId=$coldRequest.ownedPairs.mountAId
 raceGuid=$body.raceGuid;presetGuid=$body.presetGuid;skeletonName=$body.skeletonName
 gender=$body.gender;leftHanded=$body.leftHanded;bodySize='Medium'
 minimumRangerLevel=10;fixtureExperienceGrant=96000
 expectedNativeWriteCount=1;nativeWriteLeaf=$null;ownedSource=$coldRequest.ownedPairs
}
$fixture=$coldRequest.fixture
$request=[pscustomobject]@{scenario='chunk7-native-progression';runId='test-medium-replacement-source';progression=$selection;
 persistenceLoad=$coldRequest.persistenceLoad;fixture=$fixture}
$pass=0;$fail=0
function Check([string]$Name,[bool]$Expected,[scriptblock]$Action){
 $actual=$false;$detail=$null
 try{&$Action;$actual=$true}catch{$detail=$_.Exception.Message}
 if($actual-eq$Expected){$script:pass++;Write-Host ('PASS '+$Name)}
 else{$script:fail++;Write-Host ('FAIL '+$Name+': '+$detail)}
}
function Copy-Value($Value){$Value|ConvertTo-Json -Depth 30|ConvertFrom-Json}
Check 'exact paid Medium source and separate mounted cold receipt admit native replacement fixture' $true {
 Assert-KmcChunk7ProgressionLoadDescriptor $request
 $source=Get-KmcChunk7ProgressionSource $selection $fixture $manifest
 Assert-KmcBootstrapInventoryString $source.descriptor.sha256 $selection.sourceArchiveSha256
 Assert-KmcBootstrapInventoryString $source.artifactSha256 $selection.sourceArtifactSha256
 Assert-KmcBootstrapInventoryString $source.inventory.sha256 $selection.inventoryArtifactSha256
}
Check 'Small paid fixture cannot impersonate Medium Mammoth replacement' $false {
 $bad=Copy-Value $selection;$bad.ownedSource.case='small-fixture-persistence-load'
 Assert-KmcChunk7ProgressionSelection ([pscustomobject]@{scenario=$request.scenario;runId=$request.runId;progression=$bad})
}
Check 'replacement refuses a lower XP assistance amount' $false {
 $bad=Copy-Value $selection;$bad.fixtureExperienceGrant=95999
 Assert-KmcChunk7ProgressionSelection ([pscustomobject]@{scenario=$request.scenario;runId=$request.runId;progression=$bad})
}
Check 'replacement refuses the main rider as native target' $false {
 $bad=Copy-Value $selection;$bad.actorId=$selection.controlActorId
 Assert-KmcChunk7ProgressionSelection ([pscustomobject]@{scenario=$request.scenario;runId=$request.runId;progression=$bad})
}
Check 'replacement refuses altered mounted producer receipt' $false {
 $bad=Copy-Value $selection;$bad.sourceArtifactSha256='0'*64
 [void](Get-KmcChunk7ProgressionSource $bad $fixture $manifest)
}
Write-Host ('TOTAL PASS='+$pass+' FAIL='+$fail)
if($fail-ne0){exit 1}
