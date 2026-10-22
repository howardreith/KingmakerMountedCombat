[CmdletBinding()]
param([string]$RepositoryRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
$native=Read-KmcJson (Join-Path (Get-KmcLabRoot) 'runtime-evidence/20261022-c7b-world182-b182s1-fifthhorse-cold/chunk7-assisted-cold-inventory-evidence.json')
$before=$native.observations.loadedWorld;$id='8265f8cf-0969-4ec7-a266-2448de91c8c4'
$after=$before|ConvertTo-Json -Depth 40|ConvertFrom-Json
$after.party=@($before.party|Where-Object id -CNE $id)
$after.partyCharacterIds=@($before.partyCharacterIds|Where-Object{$_-cne$id})
$after.remoteCompanionIds=@($id)
$actor=@($before.party|Where-Object id -CEQ $id)[0]
$semantics=[pscustomobject]@{nativeMoney=$before.nativeMoney;actors=@($before.party|Sort-Object id|ForEach-Object{
 [pscustomobject]@{id=$_.id;blueprint=$_.blueprint;petId=$_.petId;masterId=$_.masterId;level=$_.characterLevel;experience=$_.experience;classes=$_.classes}})}
$o=[pscustomobject]@{nativeDeparture=[pscustomobject]@{contract='installed-native-single-field-departure-v1';
 action='native-ShowPartySelection';actorId=$id;originalPartyCharacterIds=$before.partyCharacterIds;openCount=1;
 switchCount=1;goCount=1;manualPartyMutation=$false;mustBeInParty=$false;readyAlpha=1.0;readyActiveInHierarchy=$true;
 selectedPartyCharacterIds=$after.partyCharacterIds;closedExactInstance=$true;closedActiveSelf=$false;remoteActorId=$id;
 settledPartyCharacterIds=$after.partyCharacterIds;nativeDepartureProven=$true};departureRetention=[pscustomobject]@{
 actorId=$id;blueprint=$actor.blueprint;object=$actor.object;descriptor=$actor.descriptor;semanticsBefore=$semantics;
 semanticsAfter=$semantics;horseFactsAfter=$native.observations.fifthHorseFacts;retainedFieldActorCount=10;nativeWrites=0;archiveUnchanged=$true}}
Assert-KmcChunk7NativeFieldDeparture $o $before $after $native.observations.fifthHorseFacts
$count=1
foreach($mutation in @('required','repeat switch','repeat Go','foreign actor','unready','manual mutation','missing neighbor','changed view','remote missing','extra remote','changed XP','changed horse','write','wrong identity','open selector')){
 $copy=$o|ConvertTo-Json -Depth 50|ConvertFrom-Json;$final=$after|ConvertTo-Json -Depth 40|ConvertFrom-Json
 switch($mutation){
  'required'{$copy.nativeDeparture.mustBeInParty=$true}
  'repeat switch'{$copy.nativeDeparture.switchCount=2}
  'repeat Go'{$copy.nativeDeparture.goCount=2}
  'foreign actor'{$copy.nativeDeparture.actorId='00000000-0000-0000-0000-000000000001'}
  'unready'{$copy.nativeDeparture.readyAlpha=0.5}
  'manual mutation'{$copy.nativeDeparture.manualPartyMutation=$true}
  'missing neighbor'{$final.party=@($final.party|Select-Object -Skip 1)}
  'changed view'{$final.party[0].view=10000}
  'remote missing'{$final.remoteCompanionIds=@()}
  'extra remote'{$final.remoteCompanionIds+=,'00000000-0000-0000-0000-000000000001'}
  'changed XP'{$copy.departureRetention.semanticsAfter.actors[0].experience++}
  'changed horse'{$copy.departureRetention.horseFactsAfter[0].rank=2}
  'write'{$copy.departureRetention.nativeWrites=1}
  'wrong identity'{$copy.departureRetention.descriptor=10000}
  'open selector'{$copy.nativeDeparture.closedActiveSelf=$true}
 }
 $refused=$false
 try{Assert-KmcChunk7NativeFieldDeparture $copy $before $final $native.observations.fifthHorseFacts}catch{$refused=$true}
 if(-not$refused){throw ('Native departure reader admitted '+$mutation)}
 $count++
}
Write-Host ('NATIVE DEPARTURE READER PASS='+$count+' FAIL=0 (synthetic transition controls; no native departure claimed)')
