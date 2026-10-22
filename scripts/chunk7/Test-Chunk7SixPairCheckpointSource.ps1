[CmdletBinding()]
param([string]$RepositoryRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
$lab=Get-KmcLabRoot;$run='20261022-c7b-world185-b185s1-sixthhorse-cold'
$root=Join-Path $lab ('runtime-evidence/'+$run)
$r=Read-KmcJson (Join-Path $root 'runtime-request.json')
$a=Read-KmcJson (Join-Path $root 'chunk7-assisted-cold-inventory-evidence.json')
$p=Read-KmcJson (Join-Path $root 'profile-cache-changes.json')
$c=Read-KmcJson (Join-Path $lab ('runtime-state/run-transactions/'+$run+'.json'))
$t=Read-KmcJson (Join-Path $lab ('runtime-state/transactions/'+$run+'.json'))
function Clone($Value){$Value|ConvertTo-Json -Depth 90|ConvertFrom-Json}
$count=0
function Refuse([string]$Name,[scriptblock]$Action){
 $denied=$false;try{&$Action|Out-Null}catch{$denied=$true}
 if(-not$denied){throw ('Six-pair checkpoint admitted '+$Name)};$script:count++
}
$watch=[Diagnostics.Stopwatch]::StartNew();$source=Get-KmcChunk7SixPairCheckpointSource $r.fixture
if($source.archiveProducerRunId-cne'20261022-c7b-world184-b184s1-sixthhorse-manual'-or
 $source.qualificationRunId-cne$run-or$source.qualificationProcessId-ne63464-or
 $source.nativeAuthorityGranted-ne$false-or$source.fixtureSetupOnly-ne$true-or
 $source.horseFacts.Count-ne6-or$source.rosterIds.Count-ne12-or$source.characterIds.Count-ne6-or
 $source.descriptor.fileName-cne'Manual_308_KMC_C7_SIX_HORSE_WORLD.zks'-or
 $source.PSObject.Properties['areasPath']){throw 'Checkpoint lost its exact provenance or granted native/Areas authority.'}
$count++
$wrong=Clone $r.fixture;$wrong.working.sha256='f'*64
Refuse 'changed protected fixture anchor' {Get-KmcChunk7SixPairCheckpointSource $wrong}
foreach($mutation in @('missing pin','duplicate pin','changed hash','changed length','changed ticks','foreign path','extra Areas')){
 $pins=Clone $source.receiptPins
 switch($mutation){
  'missing pin'{$pins=@($pins|Select-Object -Skip 1)};'duplicate pin'{$pins[1]=$pins[0]}
  'changed hash'{$pins[0].sha256='f'*64};'changed length'{$pins[0].length++}
  'changed ticks'{$pins[0].lastWriteTimeUtcTicks++};'foreign path'{$pins[0].path=$source.path}
  'extra Areas'{$pins+=Clone $pins[0]}
 }
 Refuse $mutation {Assert-KmcChunk7SixPairCheckpointPins $pins}
}
Assert-KmcChunk7SixPairCheckpointEvidence $r $a $p $c $t;$count++
foreach($mutation in @('wrong producer','wrong archive','native write','XP write','mounted qualification claim',
 'ordinary claim','native prepare','lost actor','duplicate Horse','changed owner','nonreciprocal','rank',
 'lost character','field semantic drift','remote descriptor drift','remote missing','remote world alias','paired remote XP drift','paired remote class drift','paired remote ID drift',
 'unreleased observer','combat metadata','extra metadata pair')){
 $v=Clone $a;$o=$v.observations
 switch($mutation){
  'wrong producer'{$o.source.runId=$run};'wrong archive'{$o.source.archiveSha256='f'*64}
  'native write'{$o.resourceWrites=1};'XP write'{$o.xpWrites=1};'mounted qualification claim'{$o.sixPairFixtureQualified=$true}
  'ordinary claim'{$o.ordinaryAcquisitionProven=$true};'native prepare'{$o.noPreparation.events+=,[pscustomobject]@{kind='prepare'}}
  'lost actor'{$o.loadedWorld.party=@($o.loadedWorld.party|Select-Object -Skip 1)}
  'duplicate Horse'{$o.sixthHorseFacts[1].mountId=$o.sixthHorseFacts[0].mountId}
  'changed owner'{$o.loadedWorld.party[0].petId='00000000-0000-0000-0000-000000000001'}
  'nonreciprocal'{$o.sixthHorseFacts[0].reciprocal=$false};'rank'{$o.sixthHorseFacts[0].rank=2}
  'lost character'{$o.settledWorld.partyCharacterIds=@($o.settledWorld.partyCharacterIds|Select-Object -Skip 1)}
  'field semantic drift'{$o.settledWorld.party[0].experience++}
  'remote descriptor drift'{$o.remoteSettled.descriptor=$o.remoteSettled.object}
  'remote missing'{$o.settledWorld.remoteCompanionIds=@()}
  'remote world alias'{$o.remoteAtLoad.object=$o.loadedWorld.game;$o.remoteSettled.object=$o.loadedWorld.game}
  'paired remote XP drift'{$o.remoteAtLoad.semantics.experience++;$o.remoteSettled.semantics.experience++}
  'paired remote class drift'{$o.remoteAtLoad.semantics.classes[0].level++;$o.remoteSettled.semantics.classes[0].level++}
  'paired remote ID drift'{$o.remoteAtLoad.semantics.id=$o.sixthHorseFacts[0].riderId;$o.remoteSettled.semantics.id=$o.sixthHorseFacts[0].riderId}
  'unreleased observer'{$o.cleanup.observersReleased=$false};'combat metadata'{$o.archive.combatPresent=$true}
  'extra metadata pair'{$o.archive.pairs=1}
 }
 Refuse $mutation {Assert-KmcChunk7SixPairCheckpointEvidence $r $v $p $c $t}
}
foreach($mutation in @('profile not exact','cache exception','unsettled combined','unsettled Mods','restoration error','changed Mods digest','changed save digest','wrong transaction token')){
 $vp=Clone $p;$vc=Clone $c;$vt=Clone $t
 switch($mutation){
  'profile not exact'{$vp.profileBytesRestoredExactly=$false};'cache exception'{$vp.acceptedNativeCacheChanges+=,'arbitrary'}
  'unsettled combined'{$vc.phase='staged'};'unsettled Mods'{$vt.phase='staged'}
  'restoration error'{$vc.restorationErrors+=,'failed'};'changed Mods digest'{$vc.restoredModsDigest='f'*64}
  'changed save digest'{$vc.restoredSaveInventoryDigest='f'*64};'wrong transaction token'{$vt.token='f'*64}
 }
 Refuse $mutation {Assert-KmcChunk7SixPairCheckpointEvidence $r $a $vp $vc $vt}
}
Assert-KmcChunk7SixPairCheckpointPins $source.receiptPins
foreach($baseOnly in @($false,$true)){
 $proof=Read-KmcJson $source.receiptPins[$(if($baseOnly){15}else{14})].path
 Assert-KmcChunk7SixPairHistoricalProof $proof $baseOnly;$count++
 foreach($mutation in @('FAIL','native invoked','wrong kind','wrong source','wrong DLL','original verdict')){
  if($baseOnly-and$mutation-ceq'original verdict'){continue}
  $v=Clone $proof
  switch($mutation){'FAIL'{$v.status='FAIL'};'native invoked'{$v.nativeInvoked=$true};'wrong kind'{$v.kind='other'}
   'wrong source'{$v.runId=$run};'wrong DLL'{$v.dllSha256='f'*64};'original verdict'{$v.originalOuterStatus='PASS'}}
  Refuse ('historical proof '+$mutation) {Assert-KmcChunk7SixPairHistoricalProof $v $baseOnly}
 }
}
Write-Host ('SIX-PAIR CHECKPOINT SOURCE PASS='+$count+' FAIL=0 seconds='+$watch.Elapsed.TotalSeconds+' (nativeInvoked=false; exact b185 fixture only)')
