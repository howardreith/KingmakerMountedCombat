[CmdletBinding()]
param([string]$RepositoryRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
$path=Join-Path (Get-KmcLabRoot) 'runtime-evidence/20261022-c7b-world181-b181s1-fifthhorse-manual/chunk7-manual-derived-evidence.json'
$artifact=Read-KmcJson $path
$expected=Get-KmcChunk7FifthHorseColdExpected $artifact
if($expected.world.party.Count-ne11-or$expected.readiness.staticPartyFacts.characters.Count-ne6-or$expected.owners.Count-ne5-or$expected.money-ne374879){
 throw 'Fifth-Horse cold projection lost the source roster, paid Horse or money.'
}
function Refuse([string]$label,[scriptblock]$change){
 $copy=ConvertFrom-Json -InputObject (ConvertTo-Json -InputObject $artifact -Depth 90 -Compress)
 & $change $copy
 try{[void](Get-KmcChunk7FifthHorseColdExpected $copy)}catch{Write-Host ('PASS '+$label);return}
 throw ('Fifth-Horse cold projection admitted '+$label)
}
Refuse 'lost new reciprocal pet' {param($a)$a.observations.field.profileInventory.party[0].petId=$null}
Refuse 'changed inherited Horse' {param($a)$a.observations.f09PriorHorsesAfter[0].mountId='00000000-0000-0000-0000-000000000001'}
Refuse 'missing saved actor' {param($a)$a.observations.savedFieldWorld.party=@($a.observations.savedFieldWorld.party|Select-Object -Skip 1)}
Write-Host 'FIFTH-HORSE COLD PROJECTION PASS=4 FAIL=0 (nativeInvoked=false; synthetic refusals are not cold results)'
