Set-StrictMode -Version Latest

# A closed, already accepted cold checkpoint, not a cache of arbitrary readers.
# Manual308 was produced by b184. B185 saved nothing and qualifies only its
# exact copied fixture; subsequent candidates must prove their own behavior.
function Get-KmcChunk7SixPairCheckpointPins {
 $lab=Get-KmcLabRoot;$run='20261022-c7b-world185-b185s1-sixthhorse-cold'
 $e='runtime-evidence/'+$run+'/';$p='runtime-staging/persistence-'+$run+'/'
 $specs=@(
  @(($e+'runtime-request.json'),'cdcb0df4cb994796c54ced7e69339b8cb227757f0859e7454486f0bcecc26da3',4244,639282746484398907L),
  @(($e+'runtime-result.json'),'7ee502ec15d343740ebd4d3136e21c7284e397a22bbcc029a05bd4606e786ffa',5050,639282753446982710L),
  @(($e+'runtime-game-result.json'),'2c2035b102632698b1322d074a3cc5a2ed0c71ab0fe595ae1f9a87a666683ecd',14294,639282747085782228L),
  @(($e+'runtime-artifacts.json'),'f0708bce5d908481e02e3b5d343a1c9ed4e661dae38e7bfa0c92987d65d08acc',689,639282747084237162L),
  @(($e+'chunk7-assisted-cold-inventory-evidence.json'),'012d1caff175d34614fcd255f0b6ad897e0ef1a9650791cb3c0d55ce13b47241',183439,639282747079013536L),
  @(($e+'runtime-loader-inventory.json'),'878e4ef60f044c83975ed99642a3acfb58807f6a119c9ee5ea1d37d3487f321d',46592,639282747084097144L),
  @(($e+'base-game-isolation-admission.json'),'fdd44aefa1ab7b73aa379607fd5cb77fb553cd61971ae7e7cf6f044562282a11',3723,639282746484378903L),
  @(($e+'base-game-isolation-result.json'),'b63eecce3f1c0d8d6f914094e04dfd4b512d35d35b93faa69f819bca05345bf8',1043,639282753446812694L),
  @(($e+'profile-cache-changes.json'),'3818e3de90071691a0bee8b0a198e2f9bf3e14583c31079cf979af91f50f4c6b',466,639282753415517225L),
  @(($e+'orchestration.json'),'0b57887a8ad69ea2c87ded9aa68257f690f67354ae5fb818a7ecc99a2da757e3',1247,639282753447032716L),
  @(($p+'owner.json'),'67b6af1a60d956724af22a72dfe49d62ac3a0cc5ce4bdb983311b7e6a99ea433',475,639282744519459091L),
  @(($p+'Saved Games/Manual_308_KMC_C7_SIX_HORSE_WORLD.zks'),'b1e817a60c8bbe35b5947b4eab3b3f35060493ca20873d5de5fdf8a81efbc291',1332308,639282712706531806L),
  @(('runtime-state/run-transactions/'+$run+'.json'),'2a23057588f1e21fa8c2acf557a741d6df8d86ca63adbe235d64d52b84f7d106',1623,639282753347150577L),
  @(('runtime-state/transactions/'+$run+'.json'),'9142fbc2984718d3dc08ebf3b7e13007f369315f98f10db6322a1998f9694eac',6471,639282753306685265L),
  @('analysis-cache/chunk7-continuation/b184-reader-only-reevaluation.json','6a625108153175d6004b7cd8258eb47d9d37a2efa8f057eff1e428773a3969c0',9590,639282723519944934L),
  @('analysis-cache/chunk7-continuation/b184-base-only-reader-reevaluation.json','ed4ac7708fa0ad35aaba711c1765dc3d2424ea003d71bd391442b347c8fb1648',5415,639282721395361015L)
 )
 foreach($s in $specs){[pscustomobject][ordered]@{path=[IO.Path]::GetFullPath((Join-Path $lab $s[0]));sha256=$s[1];length=[long]$s[2];lastWriteTimeUtcTicks=[long]$s[3]}}
}
function Assert-KmcChunk7SixPairCheckpointPins($Pins){
 $expected=@(Get-KmcChunk7SixPairCheckpointPins)
 Assert-KmcAssistedArray $Pins 16 16 'six-pair accepted checkpoint closure'
 for($i=0;$i-lt$expected.Count;$i++){
  Assert-KmcAssistedSame $Pins[$i] $expected[$i] 'six-pair literal checkpoint pin'
  Assert-KmcAssistedSettledFile $Pins[$i].path $Pins[$i] 'six-pair accepted checkpoint'
 }
}
function Assert-KmcChunk7SixPairCheckpointEvidence($Request,$Artifact,$Profile,$Combined,$Transaction){
 Assert-KmcAssistedColdArtifactEnvelope $Request $Artifact 'chunk7-assisted-cold-inventory-evidence'
 Assert-KmcBootstrapInventoryString $Artifact.status 'PASS';Assert-KmcAssistedArray $Artifact.errors 0 0 'six-pair cold errors'
 $o=$Artifact.observations
 foreach($pair in @(@('runId','20261022-c7b-world184-b184s1-sixthhorse-manual'),
  @('artifactSha256','0f6aabe42d0aeaf63f60c6912c1a029c0175ebe87806768f1191df23a913fa13'),
  @('archiveSha256',$Request.persistenceLoad.sha256),@('archiveLeaf',$Request.persistenceLoad.fileName),
  @('kind','reader-reevaluated-paid-sixth-horse-manual'))){Assert-KmcBootstrapInventoryString $o.source.($pair[0]) $pair[1]}
 Assert-KmcChunk7Boolean $o.source.assistedSetupOnly $true;Assert-KmcChunk7Boolean $o.source.sameCandidate $false
 foreach($key in @('resourceWrites','xpWrites','moneyWrites','mountRequests','choiceRequests')){Assert-KmcOrdinaryInteger $o.$key 0 0}
 foreach($key in @('ordinaryAcquisitionProven','ordinaryProgressionProven','sixPairFixtureQualified','profileAdmissionChanged')){Assert-KmcChunk7Boolean $o.$key $false}
 Assert-KmcAssistedColdNoPreparation $o.noPreparation $Request
 Assert-KmcOrdinaryInteger $o.archive.schemaVersion 3 3
 foreach($key in @('pairs','participations','slots')){Assert-KmcOrdinaryInteger $o.archive.$key 0 0}
 foreach($key in @('combatPresent','nativeRestorationGrantedByDiagnostic')){Assert-KmcChunk7Boolean $o.archive.$key $false}
 Assert-KmcBootstrapInventoryString $o.archive.campaignId $Request.persistenceLoad.gameId
 Assert-KmcBootstrapInventoryString $o.archive.areaId $Request.persistenceLoad.area
 $facts=@($o.sixthHorseFacts);Assert-KmcAssistedArray $facts 6 6 'six native sourced Horses'
 $ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
 foreach($f in $facts){
  foreach($id in @($f.riderId,$f.mountId)){Assert-KmcOrdinaryGuid $id;if(-not$ids.Add($id)){throw 'Six-pair checkpoint aliases an actor role.'}}
  foreach($pair in @(@('featureGuid','7db7c50677e39f09feef56f3831fc723'),@('blueprint','4016c7db400ab721ff125aef9e65e202'),@('sourceGuid','97261d609529d834eba4fd4da1bc44dc'))){Assert-KmcBootstrapInventoryString $f.($pair[0]) $pair[1]}
  Assert-KmcOrdinaryInteger $f.rank 1 1;Assert-KmcChunk7Boolean $f.reciprocal $true
 }
 foreach($w in @($o.loadedWorld,$o.settledWorld)){
  Assert-KmcAssistedArray $w.party 12 12 'six-pair checkpoint field';Assert-KmcAssistedArray $w.partyCharacterIds 6 6 'six-pair checkpoint characters'
  Assert-KmcAssistedSame @($w.party.id|Sort-Object -CaseSensitive) @($ids|Sort-Object -CaseSensitive) 'six-pair checkpoint role membership'
  Assert-KmcAssistedSame @($w.partyCharacterIds|Sort-Object -CaseSensitive) @($facts.riderId|Sort-Object -CaseSensitive) 'six-pair checkpoint rider membership'
  foreach($f in $facts){
   $r=@($w.party|Where-Object id -CEQ $f.riderId);$m=@($w.party|Where-Object id -CEQ $f.mountId)
   if($r.Count-ne1-or$m.Count-ne1){throw 'Six-pair checkpoint lost an exact actor.'}
   Assert-KmcBootstrapInventoryString $r[0].petId $f.mountId;Assert-KmcBootstrapInventoryString $m[0].masterId $f.riderId
  }
 }
 Assert-KmcAssistedSame $o.loadedWorld.party $o.settledWorld.party 'six-pair checkpoint stable native actors'
 $labels=Get-KmcAssistedColdLabels $o.nativeLoad
 $remote=[pscustomobject]@{id='8265f8cf-0969-4ec7-a266-2448de91c8c4';blueprint='77c11edb92ce0fd408ad96b40fd27121';
  characterLevel=4;experience=9000;petId=$null;masterId=$null;
  classes=@([pscustomobject]@{guid='772c83a25e2268e448e841dcd548235f';level=4})}
 Assert-KmcChunk7SixthHorseColdRemote $o $remote $labels
 foreach($role in @('game','saveManager','player','state','areaState','mainState','loadedDescriptor')){
  if($o.remoteAtLoad.object-eq$o.loadedWorld.$role-or$o.remoteAtLoad.descriptor-eq$o.loadedWorld.$role){throw 'Remote actor aliases a native world owner.'}
 }
 foreach($key in @('custodySettled','nativeWorkerSettled','observersReleased','worldAdmitted','sourcePinned','sourceUnchanged')){Assert-KmcChunk7Boolean $o.cleanup.$key $true}
 Assert-KmcChunk7Boolean $Profile.profileBytesRestoredExactly $true;Assert-KmcAssistedArray $Profile.acceptedNativeCacheChanges 0 0 'exact checkpoint profile'
 foreach($s in @($Combined,$Transaction)){
  Assert-KmcBootstrapInventoryString $s.runId $Request.runId;Assert-KmcBootstrapInventoryString $s.token $Request.transactionToken
  Assert-KmcBootstrapInventoryString $s.phase 'restored'
 }
 foreach($key in @('modsRestored','saveProtectionPassed','baselineImmutable','workingRestored','saveWriteAllowlistPassed')){Assert-KmcChunk7Boolean $Combined.$key $true}
 Assert-KmcAssistedArray $Combined.restorationErrors 0 0 'checkpoint transaction restoration errors'
 Assert-KmcBootstrapInventoryString $Combined.restoredModsDigest $Combined.modsDigestBefore
 Assert-KmcBootstrapInventoryString $Combined.restoredSaveInventoryDigest $Combined.saveInventoryDigestBefore
}
function Assert-KmcChunk7SixPairHistoricalProof($Proof,[bool]$BaseOnly){
 Assert-KmcBootstrapInventoryString $Proof.runId '20261022-c7b-world184-b184s1-sixthhorse-manual'
 Assert-KmcBootstrapInventoryString $Proof.kind $(if($BaseOnly){'sixth-hire-b184-immutable-base-only-reevaluation'}else{'sixth-hire-b184-immutable-reader-only-reevaluation'})
 Assert-KmcBootstrapInventoryString $Proof.status 'PASS';Assert-KmcChunk7Boolean $Proof.nativeInvoked $false
 Assert-KmcBootstrapInventoryString $Proof.commit 'b6d3b965a1de0653bdd2030ab7684f14201b2fca'
 Assert-KmcBootstrapInventoryString $Proof.dllSha256 '64175c597c938decb095677cad170a78e680231c0ae7eb55c36d2216753a64be'
 if(-not$BaseOnly){
  Assert-KmcBootstrapInventoryString $Proof.originalOuterStatus 'FAIL'
  Assert-KmcBootstrapInventoryString $Proof.readerCommit 'fdf580d4a28f1df6edb5c0423f24972a20791040'
  Assert-KmcOrdinaryInteger $Proof.assertionPassCount 7 7;Assert-KmcOrdinaryInteger $Proof.assertionFailCount 0 0
 }
}
function Get-KmcChunk7SixPairCheckpointSource($Fixture){
 $pins=@(Get-KmcChunk7SixPairCheckpointPins);Assert-KmcChunk7SixPairCheckpointPins $pins
 $run='20261022-c7b-world185-b185s1-sixthhorse-cold';$lab=Get-KmcLabRoot
 $set=Get-KmcChunk7OwnedReceiptSet $run 'chunk7-assisted-cold-inventory-evidence.json';$r=$set.request
 Assert-KmcBootstrapInventoryString $r.commit '29f1d9c58009dcf640e196fe33169716a7ae11d2'
 Assert-KmcChunk7AssistedColdInventoryDescriptor $r
 Assert-KmcChunk7OwnedSourceCompletion $set $run 'chunk7-assisted-cold-inventory' $Fixture (Get-KmcChunk7RecordedCandidate $r) $r.persistenceLoad.sha256 0
 $a=Read-KmcChunk7ManifestArtifact $r $set.manifest 'chunk7-assisted-cold-inventory-evidence.json' 'chunk7-assisted-cold-inventory-evidence'
 $profile=Read-KmcJson (Join-Path $set.root 'profile-cache-changes.json')
 $combinedPath=Join-Path $lab ('runtime-state/run-transactions/'+$run+'.json')
 $transactionPath=Join-Path $lab ('runtime-state/transactions/'+$run+'.json')
 $combined=Read-KmcJson $combinedPath;$transaction=Read-KmcJson $transactionPath
 Assert-KmcChunk7SixPairCheckpointEvidence $r $a $profile $combined $transaction
 Assert-KmcChunk7SixPairHistoricalProof (Read-KmcJson $pins[14].path) $false
 Assert-KmcChunk7SixPairHistoricalProof (Read-KmcJson $pins[15].path) $true
 Assert-KmcBootstrapInventoryString $set.isolation.transactionStatePath $transactionPath
 Assert-KmcBootstrapInventoryString $set.isolation.transactionStateSha256 (Get-KmcSha256 $transactionPath)
 $admission=Read-KmcJson (Join-Path $set.root 'base-game-isolation-admission.json')
 Assert-KmcBootstrapInventoryString $admission.runId $run;Assert-KmcBootstrapInventoryString $admission.transactionToken $r.transactionToken
 Assert-KmcBaseGameDependencyReceipt $admission.dependencies -VerifyFiles
 $inventory=Read-KmcChunk7ManifestArtifact $r $set.manifest 'runtime-loader-inventory.json' 'runtime-loader-inventory'
 Assert-KmcBaseGameNativeInventory $r $inventory $admission.dependencies
 Assert-KmcChunk7SixPairCheckpointPins $pins
 [pscustomobject][ordered]@{path=(Join-Path $set.profile ('Saved Games/'+$r.persistenceLoad.fileName));descriptor=$r.persistenceLoad
  archiveProducerRunId=$a.observations.source.runId;archiveProducerArtifactSha256=$a.observations.source.artifactSha256
  qualificationRunId=$run;qualificationProcessId=[int]$set.game.processId
  artifactPath=(Join-Path $set.root 'chunk7-assisted-cold-inventory-evidence.json');artifactSha256='012d1caff175d34614fcd255f0b6ad897e0ef1a9650791cb3c0d55ce13b47241';artifactLength=183439L
  rosterIds=@($a.observations.settledWorld.party.id);characterIds=@($a.observations.settledWorld.partyCharacterIds)
  horseFacts=$a.observations.sixthHorseFacts;remote=$a.observations.remoteAtLoad.semantics;receiptPins=$pins
  fixtureSetupOnly=$true;nativeAuthorityGranted=$false}
}
