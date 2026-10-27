[CmdletBinding()]
param([string]$RepositoryRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
foreach($name in @('Get-KmcChunk7ProgressionColdSource','Get-KmcChunk7OwnedReceiptSet')){
    if(-not(Get-Command $name -ErrorAction SilentlyContinue)){throw ('Standalone result reader lacks '+$name)}
}
$lab=Get-KmcLabRoot
$producerRun='20261021-c7b-world111-b99s3-small-native-retrain'
$inventoryRun='20261021-c7b-world111-b99s2-small-mounted-cold'
$producerPath=Join-Path $lab ('runtime-evidence/'+$producerRun+'/chunk7-progression-evidence.json')
$inventoryPath=Join-Path $lab ('runtime-evidence/'+$inventoryRun+'/chunk7-persistence-evidence.json')
if((Get-KmcSha256 $producerPath)-cne'bc5ab7d3ede7d591dc56f96fc704fd1cb01ce9983f1eb65ed8d80cff1730a7ea'-or
   (Get-KmcSha256 $inventoryPath)-cne'cb8815227733b861af987e0ebe8ced50422624802a7efadfece7694cf8570c8e'){
    throw 'Historical batch99 inputs changed; read-only source regression cannot transfer a verdict.'
}
$request=Read-KmcJson (Join-Path $lab ('runtime-evidence/'+$producerRun+'/runtime-request.json'))
$candidate=[pscustomobject]@{commit=$request.commit;dllSha256=$request.dllSha256;dllMvid=$request.dllMvid}
function New-ColdSelection {
    [pscustomobject]@{case='small-horse-native-retrain-cold';sourceRunId=$producerRun;
        sourceArtifactSha256='bc5ab7d3ede7d591dc56f96fc704fd1cb01ce9983f1eb65ed8d80cff1730a7ea';
        sourceArchiveSha256='6d20fea4532be5a0e7f9eaaf01da22b6c2f498c19fe53a696d081d2bc36a0444';
        inventoryRunId=$inventoryRun;inventoryArtifactSha256='cb8815227733b861af987e0ebe8ced50422624802a7efadfece7694cf8570c8e';
        actorId='c4e2c853-4b72-4904-ad84-d0b7bfba7470';petId='c862fa24-d1c1-4f95-9f50-009d7f864264';
        formerPetId='0e0c7d2d-06b4-4510-9f81-d847340d1424';controlActorId='73681ef2-b281-4bc6-bc05-2de68c1616cb';
        controlPetId='30b79228-3fe5-48ac-aa88-2de9085837b7';raceGuid=$null;presetGuid=$null;skeletonName=$null;
        gender=$null;leftHanded=$null;bodySize=$null;minimumRangerLevel=0;expectedNativeWriteCount=0;nativeWriteLeaf=$null}
}
$selection=New-ColdSelection
$source=Get-KmcChunk7ProgressionSource $selection $request.fixture $candidate
if($source.descriptor.sha256-cne$selection.sourceArchiveSha256-or
   $source.descriptor.fileName-cne'Manual_306_KMC_C7_PROGRESSION.zks'-or
   $source.sourceProcessId-ne(Read-KmcJson $producerPath).observations.processId){throw 'Genuine retrain Manual did not resolve from its accepted producer.'}
Write-Host 'PASS batch99 genuine retrain Manual resolves with complete same-candidate producer receipts'
foreach($name in @('changed output pin','retired horse aliases new pet','foreign candidate','inventory aliases producer')){
    $selection=New-ColdSelection;$current=$candidate
    switch($name){
        'changed output pin'{$selection.sourceArchiveSha256='0'*64}
        'retired horse aliases new pet'{$selection.formerPetId=$selection.petId}
        'foreign candidate'{$current=[pscustomobject]@{commit=$candidate.commit;dllSha256='0'*64;dllMvid=$candidate.dllMvid}}
        'inventory aliases producer'{$selection.inventoryRunId=$producerRun}
    }
    $refused=$false;try{[void](Get-KmcChunk7ProgressionSource $selection $request.fixture $current)}catch{$refused=$true}
    if(-not$refused){throw ('Cold retrain source accepted '+$name)}
    Write-Host ('PASS refuses '+$name)
}
Write-Host 'TOTAL PASS 5 FAIL 0; historical source admission only, no cold native qualification'
