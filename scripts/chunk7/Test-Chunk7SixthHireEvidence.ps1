[CmdletBinding()]
param([string]$RepositoryRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
$lab=Get-KmcLabRoot
function Clone($value){ConvertFrom-Json -InputObject (ConvertTo-Json -InputObject $value -Depth 90)}
$source=Read-KmcJson (Join-Path $lab 'runtime-evidence/20261022-c7b-world183-b183s1-linzi-departure/chunk7-assisted-cold-inventory-evidence.json')
$id='8265f8cf-0969-4ec7-a266-2448de91c8c4'
function Fixture {
 $prior=$source.observations;$loaded=Clone $prior.loadedWorld;$loaded.frame=100
 $before=Clone $loaded;$before.frame=101;$after=Clone $loaded;$after.frame=110;$after.party=@($after.party|Where-Object id -CNE $id)
 $characters=@($prior.loadedWorld.partyCharacterIds|Sort-Object);$retained=@($characters|Where-Object{$_-cne$id})
 $selection=[pscustomobject]@{nativeWriteLeaf='Manual_308_KMC_C7_SIX_HORSE_WORLD.zks';readiness=[pscustomobject]@{partyCharacterIds=$characters;nativeHireCost=12500}}
 $newActor='99999999-0000-0000-0000-000000000001';$newHorse='99999999-0000-0000-0000-000000000002'
 $arrangement=(Get-KmcChunk7OwnerSeedAcquisitionProjection $prior).afterSaveArrangement
 $target=@($loaded.party|Where-Object id -CEQ $id)[0]
 $roster=[pscustomobject]@{partyCharacterIds=$retained;remoteCompanionIds=@($id);departureActorId=$id;departureObject=$target.object;departureDescriptor=$target.descriptor;manualPartyMutation=$false}
 $saved=Clone $roster;$saved.partyCharacterIds=@($retained)+@($newActor)
 $o=[pscustomobject]@{nativeDeparture=(Clone $prior.nativeDeparture);originalPartyCharacters=$retained;
  departureBeforeArrangement=(Clone $arrangement);departureAfterArrangement=(Clone $arrangement);beforeChoices=(Clone $arrangement);
  departureBeforeWorld=$before;departureAfterWorld=$after;loadedWorld=$loaded;sourceNativeWorld=(Clone $prior.loadedWorld);departureRoster=$roster;savedSixthRoster=$saved;
  departureReadiness=[pscustomobject]@{contract='installed-native-post-departure-hire-readiness-v1';frame=110;nativePartyLevel=6;nativeHireBaseCost=500;nativeHireCost=18000;money=$arrangement.money;observationOnly=$true};
  field=[pscustomobject]@{frame=200;partyCharacters=$saved.partyCharacterIds};
  savedFieldWorld=[pscustomobject]@{party=@($after.party)+@([pscustomobject]@{id=$newActor},[pscustomobject]@{id=$newHorse})}}
 [pscustomobject]@{selection=$selection;observations=$o}
}
$pass=0;$f=Fixture;Assert-KmcSixthHireDeparture $f.selection $f.observations;$pass++
foreach($mutation in @('claimed completion','extra input','wrong target','not optional','not ready','manual roster edit','no remote','foreign remote','retained alias','other field removal','progression write','money write','input projected','early choices','Linzi readded','saved remote alias','source price replaced','post price changed','wrong native level','early price','price grants authority')){
 $f=Fixture;$o=$f.observations
 switch($mutation){
  'claimed completion'{$o.nativeDeparture.nativeDepartureProven=$false}
  'extra input'{$o.nativeDeparture.goCount=2}
  'wrong target'{$o.nativeDeparture.actorId=$o.loadedWorld.mainActorId}
  'not optional'{$o.nativeDeparture.mustBeInParty=$true}
  'not ready'{$o.nativeDeparture.readyAlpha=0.5}
  'manual roster edit'{$o.departureRoster.manualPartyMutation=$true}
  'no remote'{$o.savedSixthRoster.remoteCompanionIds=@()}
  'foreign remote'{$o.departureRoster.remoteCompanionIds+=@('99999999-0000-0000-0000-000000000003')}
  'retained alias'{$o.departureAfterWorld.party[0].object++}
  'other field removal'{$o.departureAfterWorld.party=@($o.loadedWorld.party|Where-Object id -CNE $o.loadedWorld.mainActorId)}
  'progression write'{$o.departureAfterArrangement.actors[0].experience++}
  'money write'{$o.beforeChoices.money++}
  'input projected'{$o.departureBeforeWorld.party=@($o.departureBeforeWorld.party|Where-Object id -CNE $id)}
  'early choices'{$o.field.frame=102}
  'Linzi readded'{$o.field.partyCharacters+=@($id)}
  'saved remote alias'{$o.savedSixthRoster.departureDescriptor++}
  'source price replaced'{$f.selection.readiness.nativeHireCost=18000}
  'post price changed'{$o.departureReadiness.nativeHireCost=12500}
  'wrong native level'{$o.departureReadiness.nativePartyLevel=5}
  'early price'{$o.departureReadiness.frame=100}
  'price grants authority'{$o.departureReadiness.observationOnly=$false}
 }
 $rejected=$false;try{Assert-KmcSixthHireDeparture $f.selection $o}catch{$rejected=$true}
 if(-not$rejected){throw ('Sixth-hire native departure reader admitted '+$mutation)};$pass++
}
Write-Host ('SIXTH-HIRE DEPARTURE READER PASS='+$pass+' FAIL=0 (synthetic post-departure/acquisition observations; nativeInvoked=false)')
