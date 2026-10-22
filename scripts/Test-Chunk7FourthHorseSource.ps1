$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'runtime/Chunk7WorldEvidence.ps1')
$zero=[pscustomobject]@{cleanup=[pscustomobject]@{nativeSaveWritten=$false}}
Assert-KmcChunk7FourthHorseZeroWrite $zero
Write-Host 'PASS zero-write world observations without optional save members'
foreach($key in @('nativeWrite','thirdHorseManual','saveRequested')){
 $changed=[pscustomobject]@{cleanup=[pscustomobject]@{nativeSaveWritten=$false}}
 $changed|Add-Member -NotePropertyName $key -NotePropertyValue $null
 $refused=$false;try{Assert-KmcChunk7FourthHorseZeroWrite $changed}catch{$refused=$true}
 if(-not$refused){throw ('Present '+$key+' escaped the zero-write reader.')}
}
Write-Host 'PASS unexpected save observations refused under strict mode'
$root=Get-KmcLabRoot
$cold='20261021-c7b-world174-b174s1-thirdhorse-cold'
$retained=Read-KmcJson (Join-Path $root ('runtime-evidence/'+$cold+'/runtime-request.json'))
$selection=[pscustomobject][ordered]@{
 case='third-horse-fixture-fourth-horse-setup'
 sourceRunId='20261021-c7b-world171-b171s1-thirdhorse-manual'
 sourceArtifactSha256='57ab380ae5c4c95fbed1647446b3ca264f7e140c91a3b7ea4bb41037085450b1'
 sourceArchiveSha256='f70a54865c86a599c3b25f6f2574abde2f663cd07b82472eac4bcbe245ea8c84'
 checkpointSourceRunId=$null;checkpointSourceArtifactSha256=$null
 seedId='KMC_C7_NO_MODS_OWNER_SEED'
 seedSha256='3405d7c813668ad646788efd746a62c1b16e1438fad1bc4bcbebc961821b924f'
 coldReceiptSha256='4439fbea3608dad76634174b555f9d4ea243f2b4d0480029b03b7de66f9c8766'
 riderAId='73681ef2-b281-4bc6-bc05-2de68c1616cb';mountAId='30b79228-3fe5-48ac-aa88-2de9085837b7'
 riderBId='c4e2c853-4b72-4904-ad84-d0b7bfba7470';mountBId='0e0c7d2d-06b4-4510-9f81-d847340d1424'
 partyIds=@('73681ef2-b281-4bc6-bc05-2de68c1616cb','30b79228-3fe5-48ac-aa88-2de9085837b7',
  'c4e2c853-4b72-4904-ad84-d0b7bfba7470','0e0c7d2d-06b4-4510-9f81-d847340d1424',
  'fd213210-71cd-41b0-b35b-68e1b18e9b43','da28f0a7-6eb0-4392-b2e0-f401a889b3aa',
  '5dd7bd1d-ae4c-4df7-9687-50fceb027344','8265f8cf-0969-4ec7-a266-2448de91c8c4')
 baseGameOnly=$true;expectedNativeLoadCount=1;expectedNativeWriteCount=0
}
$request=[pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';runId='test-fourth-source';
 ownedPairs=$selection;persistenceCase=$null;fixture=$retained.fixture}
Assert-KmcChunk7OwnedPairsSelection $request
$source=Get-KmcChunk7OwnedPairsSource $selection $null $retained.fixture $null
$request|Add-Member -NotePropertyName persistenceLoad -NotePropertyValue $source.descriptor
Assert-KmcChunk7OwnedPairsDescriptor $request
if($source.nativeAuthorityGranted-ne$false-or$source.fixtureSetupOnly-ne$true-or
 $source.artifactSha256-cne$selection.sourceArtifactSha256-or
 $source.descriptor.sha256-cne$selection.sourceArchiveSha256-or
 $source.coldReaderReceiptSha256-cne$selection.coldReceiptSha256-or
 (Get-KmcSha256 $source.path)-cne$selection.sourceArchiveSha256){throw 'Fourth-Horse source gate lost its read-only three-Horse input.'}
Write-Host 'PASS accepted eight-actor producer and independent cold ancestry; no native authority'
$selection.coldReceiptSha256='0'*64
$refused=$false
try{[void](Get-KmcChunk7OwnedPairsSource $selection $null $retained.fixture $null)}catch{$refused=$true}
if(-not$refused){throw 'Changed cold receipt was accepted.'}
Write-Host 'PASS changed cold receipt refused before native launch'
$selection.coldReceiptSha256='4439fbea3608dad76634174b555f9d4ea243f2b4d0480029b03b7de66f9c8766'
$selection.partyIds=@($selection.partyIds|Select-Object -First 7)
$refused=$false
try{[void](Get-KmcChunk7OwnedPairsSource $selection $null $retained.fixture $null)}catch{$refused=$true}
if(-not$refused){throw 'Seven-actor roster was accepted.'}
Write-Host 'PASS seven-actor roster refused before native launch'
