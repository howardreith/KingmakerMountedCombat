[CmdletBinding()]
param([string]$RepositoryRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
. (Join-Path $RepositoryRoot 'scripts/runtime/Chunk7OwnedPairsFixtures.ps1')
. (Join-Path $RepositoryRoot 'scripts/runtime/Chunk7ProgressionFixtures.ps1')
$lab=Get-KmcLabRoot
$producerRun='20261020-c7b-world90-b77s1-small-mounted-save'
$coldRun='20261020-c7b-world90-b77s2-small-mounted-cold'
$producerPath=Join-Path $lab ('runtime-evidence/'+$producerRun+'/chunk7-persistence-evidence.json')
$coldPath=Join-Path $lab ('runtime-evidence/'+$coldRun+'/chunk7-persistence-evidence.json')
$coldRequest=Read-KmcJson (Join-Path $lab ('runtime-evidence/'+$coldRun+'/runtime-request.json'))
if((Get-KmcSha256 $producerPath)-cne'8f42b418fee656549cbcf218a41880748944b6c33ca299ef11c8276533ed4882'-or
   (Get-KmcSha256 $coldPath)-cne'7620c298c91f33f5bc456a91c4beef2a7ccb66ef4758a13605d58cc682e16f3f'){
    throw 'Historical batch77 diagnostic input changed; this read-only regression cannot transfer a verdict.'
}
$producer=Read-KmcJson $producerPath
function New-SmallSelection {
    [pscustomobject]@{case='small-horse-native-dismiss';sourceRunId=$producerRun;sourceArtifactSha256=(Get-KmcSha256 $producerPath);
        sourceArchiveSha256=$producer.observations.nativeWrite.sha256;inventoryRunId=$coldRun;inventoryArtifactSha256=(Get-KmcSha256 $coldPath);
        actorId=$coldRequest.ownedPairs.riderBId;petId=$coldRequest.ownedPairs.mountBId;
        controlActorId=$coldRequest.ownedPairs.riderAId;controlPetId=$coldRequest.ownedPairs.mountAId;
        raceGuid=$null;presetGuid=$null;skeletonName=$null;gender=$null;leftHanded=$null;bodySize=$null;
        minimumRangerLevel=0;expectedNativeWriteCount=0;nativeWriteLeaf=$null;ownedSource=$coldRequest.ownedPairs}
}
$candidate=[pscustomobject]@{commit=$coldRequest.commit;dllSha256=$coldRequest.dllSha256;dllMvid=$coldRequest.dllMvid}
$selection=New-SmallSelection
$result=Get-KmcChunk7ProgressionSource $selection $coldRequest.fixture $candidate
if($result.descriptor.fileName-cne'Manual_305_KMC_C7_SMALL_WORLD.zks'-or
   $result.artifactSha256-cne$selection.sourceArtifactSha256-or
   $result.inventory.sha256-cne$selection.inventoryArtifactSha256-or
   $result.sourceProcessId-ne$producer.observations.processId){throw 'Small producer/cold read-only projection changed.'}
Write-Host 'PASS immutable batch77 Small producer and separate cold consumer resolve with honest fixture lineage'
foreach($name in @('changed cold artifact pin','producer aliases cold run','foreign nested source case')){
    $selection=New-SmallSelection
    switch($name){
        'changed cold artifact pin'{$selection.inventoryArtifactSha256='0'*64}
        'producer aliases cold run'{$selection.inventoryRunId=$producerRun}
        'foreign nested source case'{$selection.ownedSource=($coldRequest.ownedPairs|ConvertTo-Json -Depth 10|ConvertFrom-Json);$selection.ownedSource.case='persistence-load'}
    }
    $refused=$false;try{[void](Get-KmcChunk7ProgressionSource $selection $coldRequest.fixture $candidate)}catch{$refused=$true}
    if(-not$refused){throw ('Small source accepted '+$name)}
    Write-Host ('PASS refuses '+$name)
}
Write-Host 'TOTAL PASS 4 FAIL 0; read-only historical input projection, no new candidate or native qualification'
