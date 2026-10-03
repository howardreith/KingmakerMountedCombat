[CmdletBinding()]
param()
# Synthetic acceptance/refusal regression for the Chunk 6A foundation readers
# (scripts/runtime/Chunk6aFoundationEvidence.ps1): the six persistence foundation processes
# (settled RT combat Mount/Dismount and settled TB combat Mount, save and cold load), the two
# lifecycle boundaries during the exact Mount approach, the ledger row projection, the
# id-to-checkpoint mapping and the cold-load source pairing. Never launches the game; every
# fixture lives in an owned test lab under obj/ and is retained for inspection.
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'runtime/PersistenceSaveFixtures.ps1')
. (Join-Path $PSScriptRoot 'runtime/Chunk6aSupportingEvidence.ps1')
. (Join-Path $PSScriptRoot 'runtime/Chunk6aFoundationEvidence.ps1')
$script:ownedTestLab=Join-Path ([IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))) ('obj/foundation-reader-tests/'+[Guid]::NewGuid().ToString('N'))
function Get-KmcLabRoot { return $script:ownedTestLab }
[void][IO.Directory]::CreateDirectory($script:ownedTestLab)
$script:checks=0
function Accept([string]$Name,[scriptblock]$Action){ & $Action | Out-Null; $script:checks++ }
function Reject([string]$Name,[scriptblock]$Action){
 $refused=$false;$message=''
 try{ & $Action | Out-Null }catch{ $refused=$true; $message=$_.Exception.Message }
 if(-not$refused){throw ('Unexpected acceptance: '+$Name)}
 if($message -cnotlike 'Chunk 6A foundation:*'){throw ('Refusal of '+$Name+' came from outside the foundation reader: '+$message)}
 $script:checks++
}
# PowerShell 5.1 returns a JSON array as one object; unroll it so a cloned row list stays a list.
function Clone($Value){ $parsed=$Value | ConvertTo-Json -Depth 100 | ConvertFrom-Json; if($parsed -is [Array]){ $parsed | ForEach-Object {$_} } else { $parsed } }
$commit=('c'*40);$dll=('d'*64);$gameId='89c49a86-a171-4ea9-871a-f6b3b53d19b9';$area=('a'*32)
$riderId='b6628a77-4962-47a4-a17c-88d9836fc9d5';$mountId='d79a4f6c-b74e-4868-95bd-533899131acb';$targetId='c8fa7500-1ec8-4b71-8642-b620d5cc8034'
$mountGuid='f053faad986631688defa003cd7bda0e';$dismountGuid='3af2b81f4d72bbb30501fa730fcdf36e'
function New-Actor([string]$Id,[double]$Standard,[double]$Move){ [ordered]@{Id=$Id;Standard=$Standard;Move=$Move;Swift=0;Initiative=0;Reaction=0;ReactionsRemaining=1;LastSurpriseTicks=0} }
function New-Controls([int]$Facts,[long]$Casts,[long]$Accepted){ [ordered]@{Registered=$true;Enabled=$true;SerializationSuspended=$false;ExactFactCount=$Facts;DuplicateFactCount=0;ManagedHotbarSlotCount=2;TargetSelectionStartCount=$Casts;TargetSelectionEndCount=$Casts;NativeCastRequestCount=$Casts;NativeRefusalCount=0;DispatchAcceptedCount=$Accepted;DispatchRejectedCount=0;NativePrimaryShellPrepareCount=0;LastNativePrimaryShellObservation='not-observed';ActivationRecordCount=0;RiderPrimaryRelationshipEndCount=0} }
function New-Native([bool]$Tb,[string]$Current){ [ordered]@{paused=$false;mode='Default';partyCombat=$true;tbSetting=$Tb;tbInitialized=$Tb;tbCurrent=$Current;tbStatus=$(if($Tb){'Acting'}else{$null});tbWaitingUi=$false;tbRoster=@();riderCombat=$true;mountCombat=$true;riderCanAct=$true;targetCombat=$true;targetId=$targetId;targetLife=$null} }
function New-Foundation([string]$Relationship,[long]$Generation,[long]$Casts,[long]$Accepted,[long]$Mounts,[long]$Dismounts,[long]$Adoptions,[string]$Paired,[string]$Partner){
 [ordered]@{relationship=$Relationship;generation=$Generation;transitionInFlight=$false;transitionSettlement='ledgerInFlight=False';relationshipShells=0;castRequests=$Casts;dispatchAccepted=$Accepted;dispatchRejected=0;
  transitionCounters=[ordered]@{admittedMount=$Mounts;acceptedMount=$Mounts;admittedDismount=$Dismounts;acceptedDismount=$Dismounts;refusedVoluntary=0;forcedDetach=0;duplicateSuppressed=0;concurrentSuppressed=0};
  pairedIdentity=$Paired;pairedSequence=1;pairedSplit=$false;pairedFinalized=$false;partnerContextActor=$Partner;adoptionCount=$Adoptions;adoptionObservation=$null;initiativeObservation='not-observed';persistenceWorldDiscards=0;lastPersistenceWorldDiscard='none';
  riderInCombat=$true;mountInCombat=$true;riderHasMove=$true;riderHasStandard=$true;riderCommandsEmpty=$true;mountCommandsEmpty=$true;riderReallyMoving=$false;mountReallyMoving=$false;
  riderPosition=@(0.0,0.0,0.0);mountPosition=@(1.0,0.0,0.0);pairDistance=1.0;riderMoveSlot=$null}
}
function New-Debt([double]$RiderStandard,[double]$RiderMove,[double]$MountStandard,[double]$MountMove){ [ordered]@{rider=(New-Actor $riderId $RiderStandard $RiderMove);mount=(New-Actor $mountId $MountStandard $MountMove)} }
function New-Snapshot([bool]$Mounted,[bool]$Tb,[double]$RiderMove,[string]$Paired){
 $rider=New-Actor $riderId 0 $RiderMove;$mount=New-Actor $mountId 0 0
 $activation=if($Paired){[ordered]@{EncounterId=$Paired;Sequence=1;Ending=$false;Finalized=$false;Split=$false;Suspended=$false;Rider=[ordered]@{Granted=$true;Prepared=$true;Ended=$false;StandardObserved=0;MoveObserved=$RiderMove;SwiftObserved=0;ForfeitRecorded=$false;ForfeitSettled=$false;ForfeitAdded=0};Mount=$null}}else{$null}
 [ordered]@{SchemaVersion=2;CampaignId=$gameId;AreaId=$area;GameTimeTicks=434303080000;Policy='rider-principal-distinct-native-v1';RulesId='crpg-transport-v1';Mounted=$Mounted;
  ProfileId=$(if($Mounted){'medium-humanoid-mammoth-v1'}else{$null});Rider=$(if($Mounted){$rider}else{$null});Mount=$(if($Mounted){$mount}else{$null});
  Slots=@([ordered]@{ActorId=$riderId;Index=14;Kind=2},[ordered]@{ActorId=$riderId;Index=15;Kind=3});
  Combat=[ordered]@{TurnBased=$Tb;Round=$(if($Tb){1}else{0});Current=$(if($Tb){[ordered]@{ActorId=$riderId;Status=2}}else{$null});Roster=$(if($Tb){,@([ordered]@{ActorId=$riderId;Sequence=0})}else{,@()});
   Actors=@([ordered]@{Native=$rider;InCombat=$true;Prepared=$true;ExecutedAttacks=0},[ordered]@{Native=$mount;InCombat=$true;Prepared=$true;ExecutedAttacks=0},[ordered]@{Native=(New-Actor $targetId 0 0);InCombat=$true;Prepared=$true;ExecutedAttacks=0});
   Paired=$(if($Paired){[ordered]@{RiderId=$riderId;MountId=$mountId;Activation=$activation;BoundaryIsCurrent=$Tb;Boundary=$null;Partner=$null}}else{[ordered]@{RiderId=$null;MountId=$null;Activation=$null}});Allocations=@()}}
}
function New-Archive([string]$RunId){
 $root=Join-Path $script:ownedTestLab ('runtime-staging/persistence-'+$RunId+'/Saved Games');[void][IO.Directory]::CreateDirectory($root)
 $path=Join-Path $root 'Manual_300_KMC_P01.zks'
 [IO.File]::WriteAllText($path,('synthetic archive '+$RunId),[Text.UTF8Encoding]::new($false))
 [pscustomobject]@{path=$path;sha256=(Get-KmcSha256 $path);length=(Get-Item -LiteralPath $path).Length}
}
function New-Request([string]$RunId,[string]$Scenario,[string]$Case,$Load){
 $r=[ordered]@{schemaVersion=2;runId=$RunId;scenario=$Scenario;branch='b';commit=$commit;productVersion='0.1.0-chunk6a-preview.152';dllSha256=$dll;dllMvid='m';transactionToken=('t'*64);
  evidenceRoot=(Join-Path $script:ownedTestLab ('runtime-evidence/'+$RunId));persistenceCase=$Case;
  fixture=[ordered]@{baseline=[ordered]@{sha256=('b'*64)};working=[ordered]@{internalName='KMC_AUTOMATION_WORKING';fileName='Manual_299_KMC_AUTOMATION_WORKING.zks';sha256=('e'*64);length=1;lastWriteTimeUtcTicks=1;gameId=$gameId;gameName='cvb';area=$area}};
  qualificationSuite=[ordered]@{suiteId='s';snapshotSha256=('f'*64)}}
 if($null-ne$Load){$r['persistenceLoad']=$Load}
 [pscustomobject]$r
}
# One synthetic process, built the way the producer writes it.
function New-FoundationProcess([string]$Case,[string]$Scenario,[string]$RunId,[int]$ProcessId,$Archive){
 $rt=$Scenario.StartsWith('persistence-p04-');$tb=-not$rt;$save=$Scenario.EndsWith('-save');$mountCase=$Case-cne'combat-dismount-rt'
 $savedState=if($mountCase){'Mounted'}else{'Unmounted'};$sourceState=if($mountCase){'Unmounted'}else{'Mounted'}
 $paired='e0d6fd5f6a8a4f6c9d0b4d2d6b5e3b11:1'
 $ticks=[long]434200000000;$rows=New-Object System.Collections.ArrayList
 function Add-Row([string]$Kind,[string]$Relationship,$Rider,$Mount,$Controls,$Native,$Persistence,$Detail){
  $script:ticks+=10000000
  [void]$rows.Add([ordered]@{runId=$RunId;scenario=$Scenario;processId=$ProcessId;kind=$Kind;checkpoint=$Case;stage=0;time='2026-10-03T00:00:00+00:00';gameTicks=$script:ticks;source=$commit;dll=$dll;relationship=$Relationship;rider=$Rider;mount=$Mount;controls=$Controls;native=$Native;persistence=$Persistence;detail=$Detail})
 }
 $script:ticks=$ticks
 $native=New-Native $tb $(if($tb){$riderId}else{$null})
 $facts=if($mountCase){0}else{3};$factsAfter=if($mountCase){3}else{1}
 $gen0=if($save){$(if($mountCase){0}else{1})}else{1}
 $beforeFoundation=New-Foundation $sourceState $gen0 0 0 0 0 0 $(if($mountCase){$null}else{$paired}) $null
 $beforeDebt=if($tb){New-Debt 0 0.4 0 0}else{New-Debt 0 0 0 0}
 $afterFoundation=New-Foundation $savedState $(if($mountCase){$gen0+1}else{$gen0}) 1 1 $(if($mountCase){1}else{0}) $(if($mountCase){0}else{1}) $(if($mountCase){1}else{0}) $(if($mountCase){$paired}else{$null}) $null
 $afterDebt=if($tb){New-Debt 0 3.4 0 0}else{New-Debt 0 2.9 0 0}
 $actual=[ordered]@{target=$targetId;targetDamage=0;resolved=0;ordinaryAttacks=0;inputRequests=0;riderRounds=0;forcedD20=0;unresolvedProjectiles=$false;deferredSaves=0;snapshotCount=0;readyTicks=0;foundation=(Clone $beforeFoundation)}
 $snapshot=New-Snapshot $mountCase $tb $(if($tb){3.4}else{2.9}) $(if($mountCase){$paired}else{$null})
 $riderBefore=New-Actor $riderId 0 $(if($tb){0.4}else{0});$mountBefore=New-Actor $mountId 0 0
 $riderAfter=New-Actor $riderId 0 $(if($tb){3.4}else{2.9})
 $persistence=[ordered]@{semantics=0;presentation=0;feedback='none'}
 if($save){
  Add-Row 'initial' $sourceState $riderBefore $mountBefore (New-Controls $facts 0 0) $native $persistence $(if($rt){Clone $actual}else{[ordered]@{checkpoint=$Case;foundation=(Clone $beforeFoundation)}})
  if($rt){ Add-Row 'rt-foundation-combat-ready' $sourceState $riderBefore $mountBefore (New-Controls $facts 0 0) $native $persistence (Clone $actual) }
  else {
   Add-Row 'combat-mount-acting-entry-dispatched' $sourceState (New-Actor $riderId 0 0) $mountBefore (New-Controls 0 0 0) $native $persistence ([ordered]@{checkpoint=$Case;foundation=(Clone $beforeFoundation);entry=[ordered]@{origin=@(0,0,0);destination=@(0.75,0,0)}})
   Add-Row 'combat-mount-acting-entry-completed' $sourceState $riderBefore $mountBefore (New-Controls 0 0 0) $native $persistence ([ordered]@{checkpoint=$Case;foundation=(Clone $beforeFoundation);entry=[ordered]@{displacement=0.7;acting=$true;status='Acting';debt=(Clone $beforeDebt)}})
  }
  $before=Clone $beforeFoundation;$before|Add-Member -NotePropertyName debt -NotePropertyValue (Clone $beforeDebt)
  $click=[ordered]@{abilityGuid=$(if($mountCase){$mountGuid}else{$dismountGuid});abilityPresent=$true;availableForCast=$true;handlerPresent=$true;targetViewPresent=$true;clickedTargetId=$(if($mountCase){$mountId}else{$riderId});frame=100;gameTicks=$script:ticks;clicked=$true;selectedIds=@($riderId);targetSelectionStartDelta=1;targetSelectionEndDelta=1;nativeCastRequestDelta=1;nativeRefusalDelta=0;dispatchAcceptedDelta=0;dispatchRejectedDelta=0;shell=[ordered]@{type='UnitUseAbility';executor=$riderId;target=$(if($mountCase){$mountId}else{$riderId});started=$false;acted=$false;finished=$false;result='None';createdByPlayer=$false};shellInMoveSlot=$true}
  $availability=[ordered]@{visible=$true;enabled=$true;transitionReady=$true;reason=$null}
  $clickKind=if($rt){if($mountCase){'rt-combat-mount-click'}else{'rt-combat-dismount-click'}}else{'combat-mount-click'}
  $clickDetail=if($rt){[ordered]@{availability=$availability;before=$before;click=$click;actual=(Clone $actual)}}else{[ordered]@{checkpoint=$Case;foundation=(Clone $beforeFoundation);availability=$availability;before=$before;click=$click}}
  Add-Row $clickKind $sourceState $riderBefore $mountBefore (New-Controls $facts 1 0) $native $persistence $clickDetail
  $settledKind=if($rt){if($mountCase){'rt-combat-mount-settled'}else{'rt-combat-dismount-settled'}}else{'combat-mount-settled'}
  $settledDetail=if($rt){$settledActual=Clone $actual;$settledActual.foundation=(Clone $afterFoundation);$settledActual}else{[pscustomobject]([ordered]@{checkpoint=$Case;foundation=(Clone $afterFoundation)})}
  $settledDetail|Add-Member -NotePropertyName settledFrames -NotePropertyValue 10
  $settledDetail|Add-Member -NotePropertyName framesSinceClick -NotePropertyValue 40
  $settledDetail|Add-Member -NotePropertyName before -NotePropertyValue $before
  $settledDetail|Add-Member -NotePropertyName debt -NotePropertyValue (Clone $afterDebt)
  Add-Row $settledKind $savedState $riderAfter $mountBefore (New-Controls $factsAfter 1 1) $native $persistence $settledDetail
  if($rt){
   $beforeSave=Clone $actual;$beforeSave.foundation=(Clone $afterFoundation)
   Add-Row 'rt-before-save' $savedState $riderAfter $mountBefore (New-Controls $factsAfter 1 1) $native $persistence (Clone $beforeSave)
   Add-Row 'rt-native-save-requested' $savedState $riderAfter $mountBefore (New-Controls $factsAfter 1 1) $native $persistence (Clone $beforeSave)
  }
  $writeActual=Clone $actual;$writeActual.foundation=(Clone $afterFoundation);$writeActual.snapshotCount=1
  Add-Row 'native-write-complete' $savedState $riderAfter $mountBefore (New-Controls $factsAfter 1 1) $native $persistence ([ordered]@{path=$Archive.path;sha256=$Archive.sha256;length=$Archive.length;nativeType='Manual';nativeCallback=$true;operation='None';snapshot=$snapshot;actual=$writeActual})
 } else {
  $persistence=[ordered]@{semantics=3;presentation=$(if($mountCase){1}else{0});feedback='restored'}
  $coldFoundation=New-Foundation $savedState 1 0 0 0 0 0 $(if($mountCase){$paired}else{$null}) $null
  $coldActual=Clone $actual;$coldActual.foundation=(Clone $coldFoundation)
  if($rt){
   Add-Row 'initial' $savedState $riderAfter $mountBefore (New-Controls $factsAfter 0 0) $native $persistence (Clone $coldActual)
   Add-Row 'rt-cold-debt-restored' $savedState $riderAfter $mountBefore (New-Controls $factsAfter 0 0) $native $persistence ([ordered]@{snapshot=$snapshot;actual=(Clone $coldActual)})
  } else {
   Add-Row 'initial' $savedState $riderAfter $mountBefore (New-Controls $factsAfter 0 0) $native $persistence ([ordered]@{checkpoint=$Case;foundation=(Clone $coldFoundation);snapshot=$snapshot})
  }
  $afterFoundation=$coldFoundation
 }
 $endFoundation=Clone $afterFoundation
 if($rt){
  $cont=Clone $actual;$cont.foundation=(Clone $endFoundation);$cont.inputRequests=1
  Add-Row 'rt-foundation-continuation' $savedState $riderAfter $mountBefore (New-Controls $factsAfter $(if($save){1}else{0}) $(if($save){1}else{0})) $native $persistence $cont
  $first=Clone $cont;$first.resolved=1;$first.ordinaryAttacks=1
  Add-Row 'rt-approach-first-delivery' $savedState $riderAfter $mountBefore (New-Controls $factsAfter $(if($save){1}else{0}) $(if($save){1}else{0})) $native $persistence $first
  $ready=$script:ticks+70000000
  $queued=Clone $first;$queued.readyTicks=$ready
  Add-Row 'rt-spent-attack-queued' $savedState (New-Actor $riderId 6 $(if($tb){3.4}else{2.9})) $mountBefore (New-Controls $factsAfter $(if($save){1}else{0}) $(if($save){1}else{0})) $native $persistence $queued
  Add-Row 'rt-native-debt-wait' $savedState (New-Actor $riderId 5.5 2) $mountBefore (New-Controls $factsAfter $(if($save){1}else{0}) $(if($save){1}else{0})) $native $persistence (Clone $queued)
  $script:ticks=$ready
  for($k=1;$k-le2;$k++){ $later=Clone $queued;$later.resolved=1+$k;$later.riderRounds=$k;Add-Row 'rt-later-attack' $savedState (New-Actor $riderId 6 0) $mountBefore (New-Controls $factsAfter $(if($save){1}else{0}) $(if($save){1}else{0})) $native $persistence $later }
  $final=Clone $queued;$final.resolved=3;$final.riderRounds=2
  Add-Row 'usable-continuation-complete' $savedState (New-Actor $riderId 6 0) $mountBefore (New-Controls $factsAfter $(if($save){1}else{0}) $(if($save){1}else{0})) $native $persistence $final
 } else {
  $c=if($save){1}else{0}
  Add-Row 'attack-dispatched' $savedState $riderAfter $mountBefore (New-Controls 3 $c $c) $native $persistence ([ordered]@{checkpoint=$Case;foundation=(Clone $endFoundation)})
  Add-Row 'attack-delivered' $savedState (New-Actor $riderId 6 3.4) $mountBefore (New-Controls 3 $c $c) $native $persistence ([ordered]@{rules=1;rolls=1;actor=$riderId;damage=5;hit=$true;combat=[ordered]@{checkpoint=$Case;foundation=(Clone $endFoundation)}})
  for($k=2;$k-le3;$k++){ Add-Row 'next-paired-activation' $savedState (New-Actor $riderId 0 0) $mountBefore (New-Controls 3 $c $c) $native $persistence ([ordered]@{checkpoint=$Case;foundation=(Clone $endFoundation);sequence=$k}) }
  Add-Row 'usable-continuation-complete' $savedState (New-Actor $riderId 0 0) $mountBefore (New-Controls 3 $c $c) $native $persistence ([ordered]@{checkpoint=$Case;foundation=(Clone $endFoundation);turnVisits=@('2|a','2|b','2|c','2|d')})
 }
 foreach($row in $rows){ [pscustomobject]$row }
}
$processIndex=0
$processes=@(
 @('combat-mount-rt','persistence-p04-save','f-mount-save-rt'),@('combat-mount-rt','persistence-p04-load','f-mount-load-rt'),
 @('combat-dismount-rt','persistence-p04-save','f-dismount-save'),@('combat-dismount-rt','persistence-p04-load','f-dismount-load'),
 @('combat-mount-tb','persistence-p02-save','f-mount-save-tb'),@('combat-mount-tb','persistence-p02-load','f-mount-load-tb'))
$built=@{}
foreach($p in $processes){
 $case=$p[0];$scenario=$p[1];$runId=$p[2]
 # Every synthetic process is its own native process, as a save and its cold load really are.
 $processIndex++;$game=[pscustomobject]@{processId=4242+$processIndex}
 # A cold load opens the archive its source save process wrote; a save writes its own.
 $archive=if($scenario.EndsWith('-load')){ $built[@($processes|Where-Object {$_[0]-ceq$case-and$_[1]-ceq($scenario -replace '-load$','-save')})[0][2]].archive } else { New-Archive $runId }
 $rows=@(New-FoundationProcess $case $scenario $runId $game.processId $archive)
 $rows=@(Clone $rows)
 if($rows.Count -lt 6){throw ('synthetic process '+$runId+' has '+$rows.Count+' rows')}
 $request=New-Request $runId $scenario $case $(if($scenario.EndsWith('-load')){[ordered]@{internalName='KMC_P01';fileName='Manual_300_KMC_P01.zks';sha256=$archive.sha256;length=$archive.length;lastWriteTimeUtcTicks=1;gameId=$gameId;gameName='cvb';area=$area}}else{$null})
 $built[$runId]=[pscustomobject]@{rows=$rows;request=$request;archive=$archive;case=$case;scenario=$scenario;game=$game}
 Accept ($runId+' accepted') { Assert-KmcChunk6aFoundationPersistenceEvidence $request $rows $game }
 Accept ($runId+' dispatched through the persistence validator entry') {
  $manifest=[pscustomobject]@{artifacts=@()}
  # The shared entry refuses the manifest-less call only after the foundation branch has run; the branch itself is reached first.
  $evidenceDir=[string]$request.evidenceRoot;[void][IO.Directory]::CreateDirectory($evidenceDir)
  [IO.File]::WriteAllLines((Join-Path $evidenceDir 'persistence-observations.jsonl'),@($rows|ForEach-Object {$_|ConvertTo-Json -Depth 100 -Compress}),[Text.UTF8Encoding]::new($false))
  $sha=Get-KmcSha256 (Join-Path $evidenceDir 'persistence-observations.jsonl')
  $manifest=[pscustomobject]@{artifacts=@([pscustomobject]@{relativePath='persistence-observations.jsonl';kind='persistence-evidence';sha256=$sha})}
  Assert-KmcPersistenceScenarioEvidence -Request $request -Manifest $manifest -Status 'PASS' -GameResult $game
 }
 Accept ($runId+' row projection') {
  $row=Get-KmcChunk6aFoundationRows (Join-Path ([string]$request.evidenceRoot) 'persistence-observations.jsonl') $scenario ([pscustomobject]@{runId=$runId;scenario=$scenario;commit=$commit;dllSha256=$dll;status='PASS';assertionPassCount=20;assertionFailCount=0;errors=@()})
  if($row.name-cne(Get-KmcChunk6aFoundationRowName $scenario $case)-or$row.status-cne'PASS'-or$row.assertionPassCount-ne20){throw 'projection row differs'}
 }
}
if((Get-KmcChunk6aFoundationRowName 'persistence-p04-save' 'combat-mount-rt')-cne'P04-save-combat-mount-rt'-or(Get-KmcChunk6aFoundationRowName 'persistence-p02-load' 'combat-mount-tb')-cne'P02-load-combat-mount-tb'){throw 'row names differ'};$script:checks++
Reject 'row name outside the foundation' { Get-KmcChunk6aFoundationRowName 'persistence-p04-save' 'mounted-spent' }
# Mutations on each process: the reader must refuse exactly these.
function Mutate([string]$RunId,[string]$Name,[scriptblock]$Change){
 $b=$built[$RunId];$rows=@(Clone $b.rows)
 if($rows.Count -ne @($b.rows).Count){throw 'clone changed the row count'}
 & $Change $rows
 Reject ($RunId+': '+$Name) { Assert-KmcChunk6aFoundationPersistenceEvidence $b.request $rows $b.game }
}
function RowOf($Rows,[string]$Kind){ @($Rows|Where-Object kind -CEQ $Kind)[0] }
Mutate 'f-mount-save-rt' 'relationship before the click' {param($r) (RowOf $r 'initial').relationship='Mounted'}
Mutate 'f-mount-save-rt' 'relationship after settlement' {param($r) (RowOf $r 'usable-continuation-complete').relationship='Unmounted'}
Mutate 'f-mount-save-rt' 'click not admitted' {param($r) (RowOf $r 'rt-combat-mount-click').detail.click.clicked=$false}
Mutate 'f-mount-save-rt' 'two cast requests' {param($r) (RowOf $r 'rt-combat-mount-click').detail.click.nativeCastRequestDelta=2}
Mutate 'f-mount-save-rt' 'click on the rider' {param($r) (RowOf $r 'rt-combat-mount-click').detail.click.clickedTargetId=$riderId}
Mutate 'f-mount-save-rt' 'generation unchanged by the Mount' {param($r) (RowOf $r 'rt-combat-mount-settled').detail.foundation.generation=0}
Mutate 'f-mount-save-rt' 'two accepted mounts' {param($r) (RowOf $r 'rt-combat-mount-settled').detail.foundation.transitionCounters.acceptedMount=2}
Mutate 'f-mount-save-rt' 'a forced detach' {param($r) (RowOf $r 'rt-combat-mount-settled').detail.foundation.transitionCounters.forcedDetach=1}
Mutate 'f-mount-save-rt' 'no adoption' {param($r) (RowOf $r 'rt-combat-mount-settled').detail.foundation.adoptionCount=0}
Mutate 'f-mount-save-rt' 'transition still in flight' {param($r) (RowOf $r 'rt-combat-mount-settled').detail.foundation.transitionInFlight=$true}
Mutate 'f-mount-save-rt' 'rider Move not charged' {param($r) (RowOf $r 'rt-combat-mount-settled').detail.debt.rider.Move=0}
Mutate 'f-mount-save-rt' 'rider Standard raised' {param($r) (RowOf $r 'rt-combat-mount-settled').detail.debt.rider.Standard=6}
Mutate 'f-mount-save-rt' 'mount Move raised' {param($r) (RowOf $r 'rt-combat-mount-settled').detail.debt.mount.Move=3}
Mutate 'f-mount-save-rt' 'settled for one frame' {param($r) (RowOf $r 'rt-combat-mount-settled').detail.settledFrames=1}
Mutate 'f-mount-save-rt' 'archive unmounted' {param($r) (RowOf $r 'native-write-complete').detail.snapshot.Mounted=$false}
Mutate 'f-mount-save-rt' 'archive invented a turn' {param($r) (RowOf $r 'native-write-complete').detail.snapshot.Combat.Current=[pscustomobject]@{ActorId=$riderId}}
Mutate 'f-mount-save-rt' 'archive bytes differ' {param($r) (RowOf $r 'native-write-complete').detail.sha256=('0'*64)}
Mutate 'f-mount-save-rt' 'archive lost the rider Move' {param($r) (RowOf $r 'native-write-complete').detail.snapshot.Combat.Actors[0].Native.Move=0}
Mutate 'f-mount-save-rt' 'missing debt wait' {param($r) (RowOf $r 'rt-native-debt-wait').kind='rt-other'}
Mutate 'f-mount-save-rt' 'later attack early' {param($r) (RowOf $r 'rt-later-attack').gameTicks=1}
Mutate 'f-mount-save-rt' 'first delivery replayed' {param($r) (RowOf $r 'rt-approach-first-delivery').detail.resolved=2}
Mutate 'f-mount-save-rt' 'duplicate control' {param($r) (RowOf $r 'rt-before-save').controls.DuplicateFactCount=1}
Mutate 'f-mount-save-rt' 'turn-based row' {param($r) (RowOf $r 'rt-before-save').native.tbSetting=$true}
Mutate 'f-mount-save-rt' 'foreign checkpoint' {param($r) (RowOf $r 'rt-before-save').checkpoint='mounted-spent'}
Mutate 'f-mount-save-rt' 'other process' {param($r) (RowOf $r 'rt-before-save').processId=1}
Mutate 'f-mount-load-rt' 'cold cast request' {param($r) (RowOf $r 'rt-later-attack').controls.NativeCastRequestCount=1}
Mutate 'f-mount-load-rt' 'cold adoption replay' {param($r) (RowOf $r 'rt-cold-debt-restored').detail.actual.foundation.adoptionCount=1}
Mutate 'f-mount-load-rt' 'cold accepted mount' {param($r) (RowOf $r 'usable-continuation-complete').detail.foundation.transitionCounters.acceptedMount=1}
Mutate 'f-mount-load-rt' 'cold forced detach after the initial observation' {param($r) (RowOf $r 'usable-continuation-complete').detail.foundation.transitionCounters.forcedDetach=1}
Mutate 'f-mount-load-rt' 'cold voluntary transition before the initial observation' {param($r) foreach($row in $r){ $f=Get-KmcChunk6aFoundationOf $row; if($null-ne$f){ $f.transitionCounters.acceptedMount=1 } }}
Mutate 'f-mount-load-rt' 'cold generation changed' {param($r) (RowOf $r 'usable-continuation-complete').detail.foundation.generation=2}
Mutate 'f-mount-load-rt' 'cold presentation duplicated' {param($r) (RowOf $r 'initial').persistence.presentation=2}
Mutate 'f-mount-load-rt' 'cold pair lost its paired ownership' {param($r) (RowOf $r 'rt-cold-debt-restored').detail.actual.foundation.pairedIdentity=$null}
Mutate 'f-mount-load-rt' 'cold relationship' {param($r) (RowOf $r 'initial').relationship='Unmounted'}
Mutate 'f-dismount-save' 'dismount advanced the generation' {param($r) (RowOf $r 'rt-combat-dismount-settled').detail.foundation.generation=2}
Mutate 'f-dismount-save' 'dismount archive retains a pair' {param($r) (RowOf $r 'native-write-complete').detail.snapshot.Rider=[pscustomobject]@{Id=$riderId}}
Mutate 'f-dismount-save' 'dismount counted as a mount' {param($r) $s=(RowOf $r 'rt-combat-dismount-settled').detail.foundation.transitionCounters;$s.acceptedDismount=0;$s.admittedDismount=0;$s.acceptedMount=1;$s.admittedMount=1}
Mutate 'f-dismount-load' 'cold unmounted actors retain a partner context' {param($r) (RowOf $r 'rt-cold-debt-restored').detail.actual.foundation.partnerContextActor=$mountId}
Mutate 'f-dismount-load' 'cold presentation of a pair' {param($r) (RowOf $r 'initial').persistence.presentation=1}
Mutate 'f-mount-save-tb' 'turn-based cost differs' {param($r) (RowOf $r 'combat-mount-settled').detail.debt.rider.Move=2.4}
Mutate 'f-mount-save-tb' 'turn-based Standard charged' {param($r) (RowOf $r 'combat-mount-settled').detail.debt.rider.Standard=6}
Mutate 'f-mount-save-tb' 'settled off the rider turn' {param($r) (RowOf $r 'combat-mount-settled').native.tbCurrent=$mountId}
Mutate 'f-mount-save-tb' 'archive current actor differs' {param($r) (RowOf $r 'native-write-complete').detail.snapshot.Combat.Current.ActorId=$mountId}
Mutate 'f-mount-save-tb' 'archive without paired activation' {param($r) (RowOf $r 'native-write-complete').detail.snapshot.Combat.Paired.Activation=$null}
Mutate 'f-mount-save-tb' 'archive debt differs from the settled debt' {param($r) (RowOf $r 'native-write-complete').detail.snapshot.Combat.Actors[0].Native.Move=2}
Mutate 'f-mount-save-tb' 'acting entry missing' {param($r) (RowOf $r 'combat-mount-acting-entry-completed').kind='entry-other'}
Mutate 'f-mount-save-tb' 'single paired activation' {param($r) (RowOf $r 'next-paired-activation').kind='other'}
Mutate 'f-mount-save-tb' 'attack by the mount' {param($r) (RowOf $r 'attack-delivered').detail.actor=$mountId}
Mutate 'f-mount-save-tb' 'real-time setting' {param($r) (RowOf $r 'initial').native.tbSetting=$false}
Mutate 'f-mount-load-tb' 'cold turn is not the rider turn' {param($r) (RowOf $r 'initial').native.tbCurrent=$mountId}
Mutate 'f-mount-load-tb' 'cold activations not successive' {param($r) @($r|Where-Object kind -CEQ 'next-paired-activation')[1].detail.sequence=9}
Mutate 'f-mount-load-tb' 'too little unrelated participation' {param($r) (RowOf $r 'usable-continuation-complete').detail.turnVisits=@('2|a')}
# Checkpoint and scenario mismatch at the request.
Reject 'turn-based checkpoint on the real-time scenario' { $b=$built['f-mount-save-rt'];$q=Clone $b.request;$q.persistenceCase='combat-mount-tb';Assert-KmcChunk6aFoundationPersistenceEvidence $q $b.rows $b.game }
Reject 'real-time checkpoint on the turn-based scenario' { $b=$built['f-mount-save-tb'];$q=Clone $b.request;$q.persistenceCase='combat-mount-rt';Assert-KmcChunk6aFoundationPersistenceEvidence $q $b.rows $b.game }
# Cold outcome comparison against the source.
Accept 'cold outcome matches its source' { Assert-KmcChunk6aFoundationColdOutcome $built['f-mount-save-rt'].rows $built['f-mount-load-rt'].rows }
Reject 'cold outcome from another checkpoint' { Assert-KmcChunk6aFoundationColdOutcome $built['f-dismount-save'].rows $built['f-mount-load-rt'].rows }
Reject 'cold load detached beyond the source baseline' { $c=@(Clone $built['f-mount-load-rt'].rows); foreach($row in $c){ $f=Get-KmcChunk6aFoundationOf $row; if($null-ne$f){ $f.transitionCounters.forcedDetach=1 } }; Assert-KmcChunk6aFoundationColdOutcome $built['f-mount-save-rt'].rows $c }
Reject 'cold outcome replayed delivery' { $c=@(Clone $built['f-mount-load-rt'].rows);(RowOf $c 'rt-cold-debt-restored').detail.actual.resolved=1;Assert-KmcChunk6aFoundationColdOutcome $built['f-mount-save-rt'].rows $c }
# Id mapping, isolated binding and the cold-load pairing over the owned lab.
$map=Get-KmcChunk6aFoundationIdMap
if(@($map.Keys).Count-ne6){throw 'id map size differs'};$script:checks++
foreach($id in @($map.Keys)){
 $scenario=$map[$id][0];$case=$map[$id][1];$runId=@($processes|Where-Object {$_[0]-ceq$case-and$_[1]-ceq$scenario})[0][2]
 $b=$built[$runId];$dir=[string]$b.request.evidenceRoot;[void][IO.Directory]::CreateDirectory($dir)
 $requestPath=Join-Path $dir 'runtime-request.json'
 [IO.File]::WriteAllText($requestPath,($b.request|ConvertTo-Json -Depth 100),[Text.UTF8Encoding]::new($false))
 $binding=[pscustomobject]@{runId=$runId;scenario=$scenario;rows=@((Get-KmcChunk6aFoundationRowName $scenario $case));requestSha256=(Get-KmcSha256 $requestPath)}
 Accept ($id+' isolated mapping') { Assert-KmcChunk6aFoundationIsolated $id $binding $script:ownedTestLab }
 $wrong=Clone $binding;$wrong.rows=@('P04-save-mounted-spent')
 Reject ($id+' isolated row differs') { Assert-KmcChunk6aFoundationIsolated $id $wrong $script:ownedTestLab }
 $other=@($map.Keys|Where-Object {$_-cne$id-and$map[$_][0]-ceq$scenario})
 if($other.Count-eq1){ Reject ($id+' isolated checkpoint borrowed by '+$other[0]) { Assert-KmcChunk6aFoundationIsolated $other[0] $binding $script:ownedTestLab } }
}
$ids=@{}
foreach($runId in @('f-mount-save-rt','f-mount-load-rt','f-dismount-save','f-dismount-load','f-mount-save-tb','f-mount-load-tb')){
 $b=$built[$runId];$id=@($map.Keys|Where-Object {$map[$_][0]-ceq$b.scenario-and$map[$_][1]-ceq$b.case})[0]
 $ids[$id]=[pscustomobject]@{id=$id;status='PASS';runId=$runId}
}
$sources=Get-KmcChunk6aFoundationLoadSources
foreach($loadId in @($sources.Keys)){
 $entry=[pscustomobject]@{id=$loadId;status='PASS';runId=$ids[$loadId].runId;sourceEntry=$sources[$loadId]}
 Accept ($loadId+' pairs with its source') { Assert-KmcChunk6aFoundationLedgerPairing $loadId $entry $ids $script:ownedTestLab }
 $unnamed=Clone $entry;$unnamed.PSObject.Properties.Remove('sourceEntry')
 Reject ($loadId+' without a source entry') { Assert-KmcChunk6aFoundationLedgerPairing $loadId $unnamed $ids $script:ownedTestLab }
 $foreign=Clone $entry;$foreign.sourceEntry=@($sources.Values|Where-Object {$_-cne$sources[$loadId]})[0]
 Reject ($loadId+' names another source') { Assert-KmcChunk6aFoundationLedgerPairing $loadId $foreign $ids $script:ownedTestLab }
 $blocked=@{};foreach($k in $ids.Keys){$blocked[$k]=$ids[$k]};$blocked[$sources[$loadId]]=[pscustomobject]@{id=$sources[$loadId];status='BLOCKED';runId=$ids[$sources[$loadId]].runId}
 Reject ($loadId+' source not PASS') { Assert-KmcChunk6aFoundationLedgerPairing $loadId $entry $blocked $script:ownedTestLab }
}
Accept 'a save entry names no source' { Assert-KmcChunk6aFoundationLedgerPairing 'CM07-mount-save-rt' ([pscustomobject]@{id='CM07-mount-save-rt';status='PASS';runId='f-mount-save-rt'}) $ids $script:ownedTestLab }
Reject 'a save entry naming a source' { Assert-KmcChunk6aFoundationLedgerPairing 'CM07-mount-save-rt' ([pscustomobject]@{id='CM07-mount-save-rt';status='PASS';runId='f-mount-save-rt';sourceEntry='CM07-mount-load-rt'}) $ids $script:ownedTestLab }
# The cold-load request must name the exact source write: re-point the staged archive and refuse.
$altered=Clone $built['f-mount-load-rt'].request;$altered.persistenceLoad.sha256=('1'*64)
[IO.File]::WriteAllText((Join-Path ([string]$altered.evidenceRoot) 'runtime-request.json'),($altered|ConvertTo-Json -Depth 100),[Text.UTF8Encoding]::new($false))
Reject 'cold archive is not the source write' { Assert-KmcChunk6aFoundationLedgerPairing 'CM07-mount-load-rt' ([pscustomobject]@{id='CM07-mount-load-rt';status='PASS';runId='f-mount-load-rt';sourceEntry='CM07-mount-save-rt'}) $ids $script:ownedTestLab }
[IO.File]::WriteAllText((Join-Path ([string]$built['f-mount-load-rt'].request.evidenceRoot) 'runtime-request.json'),($built['f-mount-load-rt'].request|ConvertTo-Json -Depth 100),[Text.UTF8Encoding]::new($false))

# ---- Lifecycle boundaries -------------------------------------------------------------------
function New-LifecycleState([string]$Kind,[string]$Relationship,[long]$Generation,[bool]$PartyInCombat,[int]$Facts,[bool]$Enabled,[long]$Casts,[long]$Accepted,[long]$Adoptions,[long]$Compensated,[double]$RiderMove){
 [ordered]@{kind=$Kind;frame=100;seconds=1.0;gameTicks=1000;partyInCombat=$PartyInCombat;riderInCombat=$PartyInCombat;horseInCombat=$PartyInCombat;targetPresent=$PartyInCombat;targetInCombat=$PartyInCombat;
  relationshipState=$Relationship;generation=$Generation;transitionInFlight=$false;transitionLedger='ledger';ledger=[ordered]@{admittedMount=1;acceptedMount=0;admittedDismount=0;acceptedDismount=0;refusedVoluntary=0;forcedDetach=0;duplicateSuppressed=0;concurrentSuppressed=0};
  relationshipShells=0;shellState='none';dispatchAccepted=$Accepted;dispatchRejected=0;castRequests=$Casts;pairedIdentity=$null;partnerContextActor=$null;adoptionCount=$Adoptions;adoptionRollbacks=0;compensatedMounts=$Compensated;
  riderCommandsEmpty=$true;horseCommandsEmpty=$true;riderReallyMoving=$false;controls=[ordered]@{registered=$true;enabled=$Enabled;serializationSuspended=$false;exactFactCount=$Facts;duplicateFactCount=0;managedHotbarSlotCount=0};
  rider=[ordered]@{actor=$riderId;standard=0.0;move=$RiderMove;swift=0.0;initiativeCooldown=0.0;initiativeOrder=10;reactionCooldown=0.0;reactions=1;reactionsPerRound=1;nativePrepareCount=0};
  mount=[ordered]@{actor=$mountId;standard=0.0;move=0.0;swift=0.0;initiativeCooldown=0.0;initiativeOrder=9;reactionCooldown=0.0;reactions=1;reactionsPerRound=1;nativePrepareCount=0};
  command=$null;allocationSequence=10;combatFeedback='idle';playerActionFeedback='idle'}
}
function New-LifecycleCase([string]$Scenario,[string]$Outcome){
 $combatEnd=$Scenario-ceq'chunk6a-combat-end-approach'
 $geometry=[ordered]@{isAdjacent=$false;relationshipState='Unmounted';riderPosition=[ordered]@{x=0.0;y=0.0;z=0.0};horsePosition=[ordered]@{x=5.0;y=0.0;z=0.0}}
 $after=switch($Outcome){
  'unacted-Interrupt' { New-LifecycleState 'after' 'Unmounted' 0 (-not$combatEnd) 1 $true 1 0 0 0 0.0 }
  'delivered' { New-LifecycleState 'after' 'Mounted' 1 $false 3 $true 1 1 0 0 0.0 }
  'acted-not-mounted' { New-LifecycleState 'after' 'Unmounted' 1 (-not$combatEnd) 1 $true 1 1 0 1 3.0 }
 }
 $ledgerDelta=switch($Outcome){
  'unacted-Interrupt' { [ordered]@{admittedMount=1;acceptedMount=0;admittedDismount=0;acceptedDismount=0;refusedVoluntary=0;forcedDetach=$(if($Scenario-ceq'chunk6a-disable-approach'){1}else{0});duplicateSuppressed=0;concurrentSuppressed=0} }
  'delivered' { [ordered]@{admittedMount=1;acceptedMount=1;admittedDismount=0;acceptedDismount=0;refusedVoluntary=0;forcedDetach=0;duplicateSuppressed=0;concurrentSuppressed=0} }
  'acted-not-mounted' { [ordered]@{admittedMount=1;acceptedMount=0;admittedDismount=0;acceptedDismount=0;refusedVoluntary=1;forcedDetach=1;duplicateSuppressed=0;concurrentSuppressed=0} }
 }
 $case=[ordered]@{
  contract=$(if($combatEnd){'native-combat-end-during-exact-mount-approach'}else{'registered-disable-during-exact-mount-approach'});boundary=$(if($combatEnd){'combat-end'}else{'mod-disable'})
  start=$geometry;before=(New-LifecycleState 'before-click' 'Unmounted' 0 $true 1 $true 0 0 0 0 0.0);samples=@()
  click=[ordered]@{abilityGuid=$mountGuid;clickedTargetId=$mountId;clicked=$true;targetSelectionStartDelta=1;targetSelectionEndDelta=1;nativeCastRequestDelta=1;nativeRefusalDelta=0;dispatchAcceptedDelta=0;dispatchRejectedDelta=0}
  afterClick=(New-LifecycleState 'after-click' 'Unmounted' 0 $true 1 $true 1 0 0 0 0.0)
  trigger=[ordered]@{frame=120;gameTicks=1200;approachObserved=$true;riderReallyMoving=$true;riderDisplacement=0.9;commandObject=77;moveSlotObject=77;started=$false;acted=$false;finished=$false;geometry=$geometry;state=(New-LifecycleState 'trigger' 'Unmounted' 0 $true 1 $true 1 0 0 0 0.0)}
  terminal=(New-LifecycleState 'terminal' $after.relationshipState $after.generation $after.partyInCombat 1 $true 1 $after.dispatchAccepted 0 $after.compensatedMounts 0.0)
  commandWindowKind=$(if($Outcome-ceq'delivered'){'positive'}else{'unacted'});commandWindow=[ordered]@{};terminalCommand=[ordered]@{finished=$true;acted=($Outcome-cne'unacted-Interrupt');result=$(if($Outcome-ceq'unacted-Interrupt'){'Interrupt'}else{'Success'})}
  after=$after;allocationEvents=@();allocationTraceComplete=$true;observerHooks=@();interrupts=@();pairCostCallbacks=$(if($Outcome-ceq'unacted-Interrupt'){0}else{4});pairPrepareCallbacks=0
  ledgerDelta=$ledgerDelta;generationDelta=$(if($Outcome-ceq'unacted-Interrupt'){0}else{1});dispatchAcceptedDelta=$(if($Outcome-ceq'unacted-Interrupt'){0}else{1});dispatchRejectedDelta=0;castRequestDelta=1;relationshipShellsDelta=1
  noResidue=$false;outcome=$Outcome;settledFrames=10
 }
 if($combatEnd){
  $case['combatEnd']=[ordered]@{frame=121;gameTicks=1210;targetId=$targetId;targetInCombatBefore=$true;destroyRequested=$true;destroyVerified=$true;destroyVerifiedFrame=125;partyLeftCombatFrame=200;partyLeftCombatGameTicks=2000;partyLeftCombat=(New-LifecycleState 'party-left-combat' 'Unmounted' 0 $false 1 $true 1 0 0 0 0.0)}
 } else {
  $case['disable']=[ordered]@{frame=121;gameTicks=1210;before=(New-LifecycleState 'disable-before' 'Unmounted' 0 $true 1 $true 1 0 0 0 0.0);disableResult=$true;disableError=$null;after=(New-LifecycleState 'disable-after' 'Unmounted' 0 $true 0 $false 1 0 0 0 0.0);unloadProbe='not-performed'}
  $case['reEnable']=[ordered]@{frame=150;gameTicks=1500;before=(New-LifecycleState 're-enable-before' 'Unmounted' 0 $true 0 $false 1 0 0 0 0.0);reEnableResult=$true;reEnableError=$null;after=(New-LifecycleState 're-enable-after' 'Unmounted' 0 $true 1 $true 1 0 0 0 0.0);mountAvailability=[ordered]@{visible=$true;enabled=$true;transitionReady=$false;reason=$null}}
 }
 $artifact=[ordered]@{scenario=$Scenario;rows=@([ordered]@{name=$(if($combatEnd){'CM04-combat-end'}else{'CM04-disable-unload'});status='PASS';evidence=$case});observations=[ordered]@{chunk6aLifecycleBoundary=$case}}
 $artifact|ConvertTo-Json -Depth 100|ConvertFrom-Json
}
foreach($scenario in @('chunk6a-combat-end-approach','chunk6a-disable-approach')){
 $combatEnd=$scenario-ceq'chunk6a-combat-end-approach'
 foreach($outcome in @('unacted-Interrupt','acted-not-mounted')+@($(if($combatEnd){'delivered'}else{@()}))){
  Accept ($scenario+' '+$outcome) { Assert-KmcChunk6aLifecycleBoundary $scenario (New-LifecycleCase $scenario $outcome) }
 }
 function MutateCase([string]$Name,[string]$Outcome,[scriptblock]$Change){
  $a=New-LifecycleCase $scenario $Outcome;& $Change $a.observations.chunk6aLifecycleBoundary;$a.rows[0].evidence=$a.observations.chunk6aLifecycleBoundary
  Reject ($scenario+': '+$Name) { Assert-KmcChunk6aLifecycleBoundary $scenario $a }
 }
 $a=New-LifecycleCase $scenario 'unacted-Interrupt';$a.rows[0].evidence.settledFrames=11
 Reject ($scenario+': row evidence detached') { Assert-KmcChunk6aLifecycleBoundary $scenario $a }
 MutateCase 'adjacent start' 'unacted-Interrupt' {param($c) $c.start.isAdjacent=$true}
 MutateCase 'click delivered' 'unacted-Interrupt' {param($c) $c.click.dispatchAcceptedDelta=1}
 MutateCase 'no measured approach' 'unacted-Interrupt' {param($c) $c.trigger.riderDisplacement=0.1}
 MutateCase 'command acted at the trigger' 'unacted-Interrupt' {param($c) $c.trigger.acted=$true}
 MutateCase 'paired identity residue' 'unacted-Interrupt' {param($c) $c.after.pairedIdentity='stale-activation'}
 MutateCase 'detach count differs from the boundary' 'unacted-Interrupt' {param($c) $c.ledgerDelta.forcedDetach=$(if($scenario-ceq'chunk6a-disable-approach'){0}else{1})}
 MutateCase 'partner context residue' 'delivered' {param($c) $c.after.partnerContextActor='stale-partner'}
 MutateCase 'second shell registered' 'unacted-Interrupt' {param($c) $c.relationshipShellsDelta=2}
 MutateCase 'no shell registered' 'delivered' {param($c) $c.relationshipShellsDelta=0}
 MutateCase 'preparation ran' 'unacted-Interrupt' {param($c) $c.pairPrepareCallbacks=1}
 MutateCase 'unacted but mounted' 'unacted-Interrupt' {param($c) $c.after.relationshipState='Mounted'}
 MutateCase 'unacted with a cost' 'unacted-Interrupt' {param($c) $c.pairCostCallbacks=2}
 MutateCase 'unacted raised rider move' 'unacted-Interrupt' {param($c) $c.after.rider.move=3.0}
 MutateCase 'unacted with a generation' 'unacted-Interrupt' {param($c) $c.generationDelta=1}
 MutateCase 'unlawful outcome' 'unacted-Interrupt' {param($c) $c.outcome='acted-twice'}
 MutateCase 'compensation without detach' 'acted-not-mounted' {param($c) $c.ledgerDelta.forcedDetach=0}
 MutateCase 'compensation not counted' 'acted-not-mounted' {param($c) $c.after.compensatedMounts=0}
 MutateCase 'settled briefly' 'unacted-Interrupt' {param($c) $c.settledFrames=3}
 MutateCase 'incomplete trace' 'unacted-Interrupt' {param($c) $c.allocationTraceComplete=$false}
 if($combatEnd){
  MutateCase 'combat never ended' 'unacted-Interrupt' {param($c) $c.combatEnd.PSObject.Properties.Remove('partyLeftCombatFrame')}
  MutateCase 'party still in combat' 'unacted-Interrupt' {param($c) $c.after.partyInCombat=$true}
  MutateCase 'delivered mount adopted an encounter' 'delivered' {param($c) $c.after.adoptionCount=1}
  MutateCase 'delivered with two transitions' 'delivered' {param($c) $c.ledgerDelta.acceptedMount=2}
  MutateCase 'combat ended before the trigger' 'unacted-Interrupt' {param($c) $c.combatEnd.partyLeftCombatFrame=10}
 } else {
  MutateCase 'disable refused' 'unacted-Interrupt' {param($c) $c.disable.disableResult=$false}
  MutateCase 'disable left the services enabled' 'unacted-Interrupt' {param($c) $c.disable.after.controls.enabled=$true}
  MutateCase 'disable kept the facts' 'unacted-Interrupt' {param($c) $c.disable.after.controls.exactFactCount=1}
  MutateCase 're-enable refused' 'unacted-Interrupt' {param($c) $c.reEnable.reEnableResult=$false}
  MutateCase 're-enable duplicated a control' 'unacted-Interrupt' {param($c) $c.reEnable.after.controls.duplicateFactCount=1}
  MutateCase 're-enable resurrected a pair' 'unacted-Interrupt' {param($c) $c.reEnable.after.relationshipState='Mounted'}
  MutateCase 're-enable cast a mount' 'unacted-Interrupt' {param($c) $c.reEnable.after.castRequests=2}
  MutateCase 're-enable left the control invisible' 'unacted-Interrupt' {param($c) $c.reEnable.mountAvailability.visible=$false}
  MutateCase 'encounter lost across the disable' 'unacted-Interrupt' {param($c) $c.after.partyInCombat=$false}
  $delivered=New-LifecycleCase $scenario 'delivered';$delivered.observations.chunk6aLifecycleBoundary.after.partyInCombat=$true;$delivered.rows[0].evidence=$delivered.observations.chunk6aLifecycleBoundary
  Reject ($scenario+': a disabled service delivered') { Assert-KmcChunk6aLifecycleBoundary $scenario $delivered }
 }
}
Write-Host ('Synthetic foundation fixtures retained: '+$script:ownedTestLab)
Write-Output ('FOUNDATION READER PASS='+$script:checks+' FAIL=0; synthetic acceptance and refusal only, no native qualification')
