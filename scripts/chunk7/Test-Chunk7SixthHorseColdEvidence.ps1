[CmdletBinding()]
param([string]$RepositoryRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
$path=Join-Path (Get-KmcLabRoot) 'runtime-evidence/20261022-c7b-world184-b184s1-sixthhorse-manual/chunk7-manual-derived-evidence.json'
if((Get-KmcSha256 $path)-cne'0f6aabe42d0aeaf63f60c6912c1a029c0175ebe87806768f1191df23a913fa13'){throw 'Preserve exact original producer bytes.'}
$artifact=Read-KmcJson $path;$expected=Get-KmcChunk7SixthHorseColdExpected $artifact
if($expected.world.party.Count-ne12-or$expected.readiness.staticPartyFacts.characters.Count-ne6-or$expected.owners.Count-ne6-or$expected.money-ne357290-or$expected.remote.experience-ne9000){throw 'Sixth-Horse projection lost the field, paid pet, money or remote progression.'}
$count=1
foreach($mutation in @('lost new reciprocal pet','changed inherited Horse','missing saved actor','remote XP','remote class','remote missing','extra remote','remote also field','manual mutation','missing original progression')){
 $copy=$artifact|ConvertTo-Json -Depth 90|ConvertFrom-Json
 switch($mutation){
  'lost new reciprocal pet'{$copy.observations.field.profileInventory.party[0].petId=$null}
  'changed inherited Horse'{$copy.observations.f09PriorHorsesAfter[0].mountId='00000000-0000-0000-0000-000000000001'}
  'missing saved actor'{$copy.observations.savedFieldWorld.party=@($copy.observations.savedFieldWorld.party|Select-Object -Skip 1)}
  'remote XP'{($copy.observations.afterSaveArrangement.actors|Where-Object actorId -CEQ $expected.remote.id).experience++}
  'remote class'{($copy.observations.afterSaveArrangement.actors|Where-Object actorId -CEQ $expected.remote.id).classes[0].level++}
  'remote missing'{$copy.observations.savedSixthRoster.remoteCompanionIds=@()}
  'extra remote'{$copy.observations.savedSixthRoster.remoteCompanionIds+=,'00000000-0000-0000-0000-000000000001'}
  'remote also field'{$copy.observations.savedFieldWorld.party[0]=($copy.observations.sourceNativeWorld.party|Where-Object id -CEQ $expected.remote.id)}
  'manual mutation'{$copy.observations.savedSixthRoster.manualPartyMutation=$true}
  'missing original progression'{$copy.observations.afterSaveArrangement.actors=@($copy.observations.afterSaveArrangement.actors|Where-Object actorId -CNE $expected.remote.id)}
 }
 $refused=$false;try{[void](Get-KmcChunk7SixthHorseColdExpected $copy)}catch{$refused=$true}
 if(-not$refused){throw ('Sixth-Horse projection admitted '+$mutation)};$count++
}
# Native object labels here are controlled external-boundary values. This tests
# the real strict retention reader, not a native cold load.
$labels=[Collections.Generic.HashSet[int]]::new();foreach($label in 1..5){[void]$labels.Add($label)}
$remote=[pscustomobject]@{semantics=$expected.remote;object=1;descriptor=2}
$o=[pscustomobject]@{loadedWorld=[pscustomobject]@{remoteCompanionIds=@($expected.remote.id);party=@([pscustomobject]@{object=3;descriptor=4;view=5})};
 settledWorld=[pscustomobject]@{remoteCompanionIds=@($expected.remote.id)};remoteAtLoad=$remote;remoteSettled=$remote}
Assert-KmcChunk7SixthHorseColdRemote $o $expected.remote $labels;$count++
foreach($mutation in @('XP changed','descriptor changed','field alias','missing remote','extra remote')){
 $copy=$o|ConvertTo-Json -Depth 25|ConvertFrom-Json
 switch($mutation){
  'XP changed'{$copy.remoteSettled.semantics.experience++};'descriptor changed'{$copy.remoteSettled.descriptor=1}
  'field alias'{$copy.remoteSettled.object=3};'missing remote'{$copy.settledWorld.remoteCompanionIds=@()}
  'extra remote'{$copy.settledWorld.remoteCompanionIds+=,'00000000-0000-0000-0000-000000000001'}
 }
 $refused=$false;try{Assert-KmcChunk7SixthHorseColdRemote $copy $expected.remote $labels}catch{$refused=$true}
 if(-not$refused){throw ('Sixth-Horse cold retention admitted '+$mutation)};$count++
}
Write-Host ('SIXTH-HORSE COLD PROJECTION/RETENTION PASS='+$count+' FAIL=0 (nativeInvoked=false)')
