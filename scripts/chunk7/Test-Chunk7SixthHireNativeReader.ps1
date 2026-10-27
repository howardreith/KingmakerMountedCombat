[CmdletBinding()]
param([string]$RepositoryRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
$root=Join-Path (Get-KmcLabRoot) 'runtime-evidence/20261022-c7b-world184-b184s1-sixthhorse-manual'
$sourcePath=Join-Path (Get-KmcLabRoot) 'runtime-evidence/20261022-c7b-world183-b183s1-linzi-departure/chunk7-assisted-cold-inventory-evidence.json'
$paths=@((Join-Path $root 'runtime-request.json'),(Join-Path $root 'chunk7-manual-derived-evidence.json'),(Join-Path $root 'runtime-result.json'),$sourcePath)
$pins=@{};foreach($path in $paths){$pins[$path]=Get-KmcSha256 $path}
$request=Read-KmcJson $paths[0];$artifact=Read-KmcJson $paths[1];$original=Read-KmcJson $paths[2];$source=Read-KmcJson $sourcePath
Assert-KmcBootstrapInventoryString $original.status 'FAIL'
Assert-KmcAssistedArray $original.errors 1 1 'preserved batch184 original outer failure'
Assert-KmcBootstrapInventoryString $original.errors[0] 'Ordinary native counter is missing, mistyped or out of range.'
Assert-KmcChunk7ManualDerivedEvidence $request $artifact $source
$pass=1
function Clone($value){ConvertFrom-Json -InputObject (ConvertTo-Json -InputObject $value -Depth 100)}
foreach($mutation in @('native price replaced','source price replaced','captured price replaced','capture removed','paid money changed','extra native spend','lost remote','sixth Horse replaced')){
 $wrong=Clone $artifact;$q=Clone $request;$o=$wrong.observations
 switch($mutation){
  'native price replaced'{$o.choices[0].nativeCost=12500}
  'source price replaced'{$q.manualDerived.readiness.nativeHireCost=18000;$o.selection.readiness.nativeHireCost=18000}
  'captured price replaced'{$o.departureReadiness.nativeHireCost=12500}
  'capture removed'{$o.PSObject.Properties.Remove('departureReadiness')}
  'paid money changed'{$o.afterChoices.money++}
  'extra native spend'{$spend=@($o.choices|Where-Object event -CEQ 'native-hire-spent')[0];$o.choices+=@($spend)}
  'lost remote'{$o.savedSixthRoster.remoteCompanionIds=@()}
  'sixth Horse replaced'{$o.field.petId=$o.field.actorId}
 }
 $refused=$false;try{Assert-KmcChunk7ManualDerivedEvidence $q $wrong $source}catch{$refused=$true}
 if(-not$refused){throw ('Sixth native reader admitted '+$mutation)};$pass++
}
foreach($path in $pins.Keys){if((Get-KmcSha256 $path)-cne$pins[$path]){throw 'Read-only sixth-hire regression changed original evidence.'}}
Write-Host ('SIXTH-HIRE NATIVE ARTIFACT READER PASS='+$pass+' FAIL=0 (immutable raw game evidence; original outer FAIL preserved; nativeInvoked=false)')
