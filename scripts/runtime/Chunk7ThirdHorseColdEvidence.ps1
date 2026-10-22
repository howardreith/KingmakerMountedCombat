Set-StrictMode -Version Latest

function Get-KmcChunk7ThirdHorseColdExpected($Producer,[bool]$Fourth=$false){
 $o=$Producer.observations;$manual=$o.thirdHorseManual;$boundary=$manual.boundary;$field=$o.cleanup.partyAfter
 $actorCount=if($Fourth){9}else{8};$horseCount=if($Fourth){4}else{3}
 Assert-KmcAssistedArray $field $actorCount $actorCount 'Horse-world saved field'
 Assert-KmcAssistedArray $boundary.actors $actorCount $actorCount 'Horse-world saved progression'
 Assert-KmcAssistedSame $boundary $manual.arrangementAfterSave 'third-Horse native Manual boundary'
 $rows=@(foreach($actor in $field){
  $progress=@($boundary.actors|Where-Object actorId -CEQ $actor.id)
  if($progress.Count-ne1-or$progress[0].blueprint-cne$actor.blueprint){throw 'Third-Horse saved field and progression disagree.'}
  [pscustomobject]@{id=$actor.id;blueprint=$actor.blueprint;petId=$actor.petId;masterId=$actor.masterId;
   characterLevel=$progress[0].characterLevel;experience=$progress[0].experience;
   classes=@($progress[0].classes|ForEach-Object{[pscustomobject]@{guid=$_.classGuid;level=$_.level}})}
 })
 if(@($rows.id|Select-Object -Unique).Count-ne$actorCount){throw 'Horse-world producer roster repeats an actor.'}
 $main='73681ef2-b281-4bc6-bc05-2de68c1616cb'
 $characters=@($rows|Where-Object{$null-eq$_.masterId})
 if($characters.Count-ne5-or@($characters|Where-Object id -CEQ $main).Count-ne1){throw 'Horse-world source does not retain five native characters and its main.'}
 $sourceOwners=@($o.inheritedFixture.pairs|ForEach-Object{[pscustomobject]@{riderId=$_.riderId;mountId=$_.mountId;
   featureGuid='7db7c50677e39f09feef56f3831fc723';blueprint='4016c7db400ab721ff125aef9e65e202';
   sourceGuid=$_.sourceGuid;rank=$_.rank;reciprocal=$true}})
 $horse=$o.thirdHorseSetup.nativeHorse
 $sourceOwners+=,[pscustomobject]@{riderId=$horse.mountMasterId;mountId=$horse.mountId;featureGuid=$horse.featureGuid;
  blueprint=$horse.blueprint;sourceGuid=$horse.sourceGuid;rank=$horse.rank;reciprocal=$true}
 if($sourceOwners.Count-ne$horseCount-or@($sourceOwners.riderId|Select-Object -Unique).Count-ne$horseCount-or
  @($sourceOwners.mountId|Select-Object -Unique).Count-ne$horseCount){throw 'Horse-world producer did not record distinct sourced Horses.'}
 foreach($owner in $sourceOwners){
  $r=@($rows|Where-Object id -CEQ $owner.riderId);$m=@($rows|Where-Object id -CEQ $owner.mountId)
  if($r.Count-ne1-or$m.Count-ne1-or$r[0].petId-cne$owner.mountId-or$m[0].masterId-cne$owner.riderId-or
   $m[0].blueprint-cne$owner.blueprint){throw 'Third-Horse source lacks saved reciprocal Horse roles.'}
 }
 [pscustomobject]@{world=[pscustomobject]@{mainActorId=$main;party=$rows};
  readiness=[pscustomobject]@{staticPartyFacts=[pscustomobject]@{characters=@($characters|ForEach-Object{[pscustomobject]@{actorId=$_.id}})};
   mainProgression=[pscustomobject]@{experience=@($rows|Where-Object id -CEQ $main)[0].experience}};
  money=[long]$boundary.money;owners=$sourceOwners}
}

function Assert-KmcChunk7ThirdHorseColdEvidence($Request,$Artifact,$SourceArtifact){
 $s=$Request.assistedColdInventory;$o=$Artifact.observations
 $fourth=Test-KmcChunk7FourthHorseColdSelection $s
 $qualified=if($fourth){Get-KmcChunk7FourthHorseColdSource $s $Request.fixture $Request}else{Get-KmcChunk7ThirdHorseColdSource $s $Request.fixture $Request}
 Assert-KmcAssistedSame $SourceArtifact (Read-KmcJson $qualified.artifactPath) 'third-Horse immutable producer artifact'
 $expected=Get-KmcChunk7ThirdHorseColdExpected $SourceArtifact $fourth
 $factKey=if($fourth){'fourthHorseFacts'}else{'thirdHorseFacts'}
 $sourceKind=if($fourth){'reader-reevaluated-assisted-fourth-horse-manual'}else{'qualified-assisted-third-horse-manual'}
 Assert-KmcAssistedObjectValue $o @('processId','selection','source','sourceNativeWorld','sourceNativeMoney','beforeLoad',
  'resourceWrites','xpWrites','moneyWrites','profileAdmissionChanged','ordinaryAcquisitionProven',
  'sixPairFixtureQualified','mountRequests','choiceRequests','ordinaryProgressionProven','loadedWorld','loader',
  'nativeProfileInventory','fixtureReadiness','archive','settledWorld','settledLoader','nativeLoad',
  'noPreparation',$factKey,'cleanup') 'Horse-world separate cold observations'
 Assert-KmcAssistedSame $o.selection $s 'third-Horse exact cold selection'
 Assert-KmcAssistedSame $o.sourceNativeWorld ([pscustomobject]@{gameId=$Request.persistenceLoad.gameId;
  gameName=$Request.persistenceLoad.gameName;area=$Request.persistenceLoad.area;
  mainActorId=$expected.world.mainActorId;party=$expected.world.party}) 'third-Horse saved world projection'
 Assert-KmcOrdinaryInteger $o.processId 1 ([int]::MaxValue)
 if($o.processId-eq$qualified.sourceProcessId){throw 'Third-Horse cold reused its native producer process.'}
 foreach($key in @('resourceWrites','xpWrites','moneyWrites','mountRequests','choiceRequests')){Assert-KmcOrdinaryInteger $o.$key 0 0}
 foreach($key in @('profileAdmissionChanged','ordinaryAcquisitionProven','sixPairFixtureQualified','ordinaryProgressionProven')){Assert-KmcChunk7Boolean $o.$key $false}
 $root=Get-KmcAssistedSaveRoot $Request
 Assert-KmcAssistedObjectValue $o.source @('runId','artifactSha256','archiveSha256','archiveLeaf','sameCandidate',
  'assistedSetupOnly','copiedArtifactPath','diagnosticArchiveContentRead','kind') 'third-Horse cold source provenance'
 foreach($pair in @(@('runId',$s.sourceRunId),@('artifactSha256',$s.sourceArtifactSha256),
  @('archiveSha256',$s.sourceArchiveSha256),@('archiveLeaf',$Request.persistenceLoad.fileName),
  @('copiedArtifactPath',(Join-Path (Split-Path $root -Parent) 'assisted-cold-source.json')),
  @('kind',$sourceKind))){Assert-KmcBootstrapInventoryString $o.source.($pair[0]) $pair[1]}
 Assert-KmcChunk7Boolean $o.source.sameCandidate $false
 Assert-KmcChunk7Boolean $o.source.assistedSetupOnly $true
 Assert-KmcChunk7Boolean $o.source.diagnosticArchiveContentRead $false
 Assert-KmcOrdinaryInteger $o.sourceNativeMoney $expected.money $expected.money
 $b=$o.beforeLoad;$w=$o.loadedWorld;$f=$o.settledWorld;$labels=Get-KmcAssistedColdLabels $o.nativeLoad
 Assert-KmcAssistedObjectValue $b @('frame','game','saveManager','menuPlayer','loadedAreaPresent','nativeLoading','authority') 'third-Horse before-load owner'
 Assert-KmcOrdinaryInteger $b.frame 0 ([int]::MaxValue)
 foreach($key in @('loadedAreaPresent','nativeLoading')){Assert-KmcChunk7Boolean $b.$key $false}
 foreach($key in @('game','saveManager','menuPlayer')){Assert-KmcAssistedLabel $b.$key $labels}
 Assert-KmcAssistedColdAuthority $b.authority $root $b.authority.generation 0
 Assert-KmcAssistedColdWorld $w $Request $labels $expected.world $expected.readiness $expected.money $b.frame
 Assert-KmcAssistedColdWorld $f $Request $labels $expected.world $expected.readiness $expected.money $w.frame
 foreach($world in @($w,$f)){
  Assert-KmcAssistedColdAuthority $world.authority $root $b.authority.generation 1
  foreach($key in @('game','saveManager')){Assert-KmcOrdinaryInteger $world.$key $b.$key $b.$key}
 }
 foreach($property in $w.PSObject.Properties){if($property.Name-cne'frame'){Assert-KmcAssistedSame $f.($property.Name) $property.Value ('third-Horse settled cold world '+$property.Name)}}
 Assert-KmcAssistedColdLoader $o.loader $Request $w $labels $root $b.authority.generation
 Assert-KmcAssistedColdLoader $o.settledLoader $Request $f $labels $root $b.authority.generation
 Assert-KmcAssistedSame $o.settledLoader $o.loader 'third-Horse settled cold descriptor'
 $prior=$b.frame
 for($i=0;$i-lt2;$i++){
  $entry=$o.nativeLoad.events[$i]
  Assert-KmcAssistedObjectValue $entry @('kind','frame','game','saveManager','player','gameId','mainActorId') 'third-Horse native PostLoad event'
  Assert-KmcBootstrapInventoryString $entry.kind $(if($i-eq0){'native-player-postload-before'}else{'native-player-postload-after'})
  Assert-KmcOrdinaryInteger $entry.frame $prior $w.frame;$prior=$entry.frame
  foreach($key in @('game','saveManager','player')){Assert-KmcAssistedLabel $entry.$key $labels;Assert-KmcOrdinaryInteger $entry.$key $w.$key $w.$key}
  if($null-ne$entry.gameId){Assert-KmcBootstrapInventoryString $entry.gameId $Request.persistenceLoad.gameId}
  if($i-eq1-or$null-ne$entry.mainActorId){Assert-KmcBootstrapInventoryString $entry.mainActorId $w.mainActorId}
 }
 if($o.nativeLoad.events[0].frame-ne$o.nativeLoad.events[1].frame){throw 'Third-Horse native PostLoad did not return synchronously.'}
 Assert-KmcAssistedColdNoPreparation $o.noPreparation $Request
 $archive=$o.archive;Assert-KmcAssistedObjectValue $archive @('schemaVersion','campaignId','areaId','pairs','participations','slots','combatPresent','nativeRestorationGrantedByDiagnostic') 'third-Horse empty mounted metadata'
 Assert-KmcOrdinaryInteger $archive.schemaVersion 3 3
 Assert-KmcBootstrapInventoryString $archive.campaignId $Request.persistenceLoad.gameId
 Assert-KmcBootstrapInventoryString $archive.areaId $Request.persistenceLoad.area
 foreach($key in @('pairs','participations','slots')){Assert-KmcOrdinaryInteger $archive.$key 0 0}
 foreach($key in @('combatPresent','nativeRestorationGrantedByDiagnostic')){Assert-KmcChunk7Boolean $archive.$key $false}
 Assert-KmcAssistedColdProfile $o.nativeProfileInventory $w
 Assert-KmcOrdinaryInteger $o.nativeProfileInventory.ordinaryAcquisitionPrerequisites.nativeMoney $expected.money $expected.money
 Assert-KmcAssistedColdFixtureReadiness $o.fixtureReadiness $Request $w $f $o.nativeProfileInventory $root
 Assert-KmcAssistedSame $o.$factKey $expected.owners 'sourced native Horse facts/ranks survived cold load'
 Assert-KmcAssistedObjectValue $o.cleanup @('custodySettled','nativeWorkerSettled','observersReleased','worldAdmitted','sourcePinned','sourceUnchanged','authority') 'third-Horse cold cleanup'
 foreach($key in @('custodySettled','nativeWorkerSettled','observersReleased','worldAdmitted','sourcePinned','sourceUnchanged')){Assert-KmcChunk7Boolean $o.cleanup.$key $true}
 Assert-KmcAssistedColdAuthority $o.cleanup.authority $root $b.authority.generation 1
}
