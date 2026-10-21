[CmdletBinding()]
param([string]$RepositoryRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
$lab=Get-KmcLabRoot
$producerRun='20261021-c7b-world120-b116s3-medium-mammoth-retrain'
$inventoryRun='20261021-c7b-world120-b116s2-medium-mounted-cold'
$producerPath=Join-Path $lab ('runtime-evidence/'+$producerRun+'/chunk7-progression-evidence.json')
$inventoryPath=Join-Path $lab ('runtime-evidence/'+$inventoryRun+'/chunk7-persistence-evidence.json')
$producerSha='68e4b456d9601f97c79f34c8a523a90d716fdcfe5eebd524032af1a308c64336'
$inventorySha='75b454a3dafa45859c291ffc0f8f3be55882bb24f3c6aa43dcbb86326353d391'
if((Get-KmcSha256 $producerPath)-cne$producerSha-or(Get-KmcSha256 $inventoryPath)-cne$inventorySha){
    throw 'Immutable batch116 Mammoth producer or mounted inventory changed.'
}
$request=Read-KmcJson (Join-Path $lab ('runtime-evidence/'+$producerRun+'/runtime-request.json'))
$artifact=Read-KmcJson $producerPath
$candidate=[pscustomobject]@{commit=$request.commit;dllSha256=$request.dllSha256;dllMvid=$request.dllMvid}
function New-ColdSelection {
    [pscustomobject]@{case='medium-horse-native-retrain-cold';sourceRunId=$producerRun;
        sourceArtifactSha256=$producerSha;sourceArchiveSha256='15a5b4915f84d96df052c878f1c4360322db847314bb1ef8fdcb793f07c2f26c';
        inventoryRunId=$inventoryRun;inventoryArtifactSha256=$inventorySha;
        actorId=$request.progression.actorId;petId=$artifact.observations.ownedRosterAfter.currentPetId;
        formerPetId=$request.progression.petId;controlActorId=$request.progression.controlActorId;
        controlPetId=$request.progression.controlPetId;raceGuid=$null;presetGuid=$null;skeletonName=$null;
        gender=$null;leftHanded=$null;bodySize=$null;minimumRangerLevel=0;expectedNativeWriteCount=0;nativeWriteLeaf=$null}
}
$selection=New-ColdSelection
$source=Get-KmcChunk7ProgressionSource $selection $request.fixture $candidate
if($source.descriptor.sha256-cne$selection.sourceArchiveSha256-or
   $source.descriptor.fileName-cne'Manual_306_KMC_C7_PROGRESSION.zks'-or
   $source.sourceProcessId-ne$artifact.observations.processId){throw 'Genuine Mammoth Manual did not resolve from its accepted producer.'}
Write-Host 'PASS batch116 genuine Mammoth Manual resolves with same-candidate receipts'
foreach($name in @('changed output pin','retired Horse aliases new Mammoth','foreign candidate','inventory aliases producer')){
    $selection=New-ColdSelection;$current=$candidate
    switch($name){
        'changed output pin'{$selection.sourceArchiveSha256='0'*64}
        'retired Horse aliases new Mammoth'{$selection.formerPetId=$selection.petId}
        'foreign candidate'{$current=[pscustomobject]@{commit=$candidate.commit;dllSha256='0'*64;dllMvid=$candidate.dllMvid}}
        'inventory aliases producer'{$selection.inventoryRunId=$producerRun}
    }
    $refused=$false;try{[void](Get-KmcChunk7ProgressionSource $selection $request.fixture $current)}catch{$refused=$true}
    if(-not$refused){throw ('Mammoth cold source accepted '+$name)}
    Write-Host ('PASS refuses '+$name)
}
Write-Host 'TOTAL PASS 5 FAIL 0; immutable producer source admission only, no native cold qualification'
