Set-StrictMode -Version Latest
function Test-KmcChunk7ManualSeedScenario([string]$Scenario){$Scenario-ceq'chunk7-bootstrap-manual-seed'}
function Get-KmcChunk7ManualSeedRows {@('chunk7-bootstrap-manual-seed','C7-manual-seed-source','C7-manual-seed-native-load','C7-manual-seed-native-save','C7-manual-seed-no-arrangement','C7-manual-seed-cleanup','C7-manual-seed-runtime','C7-manual-seed-interrupted')}
function Assert-KmcChunk7ManualSeedSelection($Request){
 if($Request-is[Collections.IDictionary]){$Request=[pscustomobject]$Request}
 $has=$null-ne$Request.PSObject.Properties['manualSeed']
 if(-not(Test-KmcChunk7ManualSeedScenario $Request.scenario)){if($has){throw 'Manual seed selection is forbidden under another family.'};return}
 if(-not$has){throw 'Manual seed requires its exact explicit selection.'};$s=$Request.manualSeed
 Assert-KmcExactProperties $s @('case','sourceRunId','sourceArtifactSha256','sourceArchiveSha256','inventoryRunId','inventoryArtifactSha256','baseGameOnly','expectedNativeLoadCount','expectedNativeWriteCount','expectedManualNumber') 'Manual seed selection'
 Assert-KmcBootstrapInventoryString $s.case 'native-save-only'
 $ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);[void]$ids.Add($Request.runId)
 foreach($key in @('sourceRunId','inventoryRunId')){if($s.$key-isnot[string]-or$s.$key-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'-or-not$ids.Add($s.$key)){throw 'Manual seed requires distinct completed bootstrap and cold-inventory runs.'}}
 foreach($key in @('sourceArtifactSha256','sourceArchiveSha256','inventoryArtifactSha256')){if($s.$key-isnot[string]-or$s.$key-cnotmatch'^[a-f0-9]{64}$'){throw 'Manual seed source pins must be exact SHA256 strings.'}}
 Assert-KmcChunk7Boolean $s.baseGameOnly $true
 foreach($key in @('expectedNativeLoadCount','expectedNativeWriteCount','expectedManualNumber')){Assert-KmcOrdinaryInteger $s.$key 1 1}
}
function Assert-KmcChunk7ManualSeedDescriptor($Request){
 Assert-KmcChunk7ManualSeedSelection $Request
 if(-not(Test-KmcChunk7ManualSeedScenario $Request.scenario)){throw 'Manual seed descriptor belongs only to its closed family.'}
 $d=$Request.persistenceLoad
 Assert-KmcExactProperties $d @('internalName','fileName','sha256','length','lastWriteTimeUtcTicks','gameId','gameName','area') 'Manual seed immutable descriptor'
 Assert-KmcBootstrapInventoryString $d.fileName 'Auto_1.zks';Assert-KmcBootstrapInventoryString $d.sha256 $Request.manualSeed.sourceArchiveSha256
 foreach($key in @('internalName','gameName')){if($d.$key-isnot[string]-or[string]::IsNullOrWhiteSpace($d.$key)-or$d.$key.Length-gt256-or$d.$key-match'[\x00-\x1f\x7f]'){throw 'Manual seed source native name is malformed.'}}
 Assert-KmcOrdinaryGuid $d.gameId
 if($d.gameId-ceq$Request.fixture.working.gameId-or$d.sha256-ceq$Request.fixture.working.sha256-or$d.sha256-ceq$Request.fixture.baseline.sha256-or$d.area-isnot[string]-or$d.area-cnotmatch'^[a-f0-9]{32}$'){throw 'Manual seed source must be the distinct native-minted campaign.'}
 Assert-KmcOrdinaryInteger $d.length 1 268435456;Assert-KmcOrdinaryInteger $d.lastWriteTimeUtcTicks 1
}
function Assert-KmcChunk7ManualSeedAuthority($Row,[string]$Root,[long]$Generation,[long]$Loads,[long]$Writes){
 Assert-KmcBootstrapInventoryString $Row.root $Root;Assert-KmcOrdinaryInteger $Row.generation 1 ([int]::MaxValue)
 if($Row.generation-ne$Generation){throw 'Manual seed authority generation changed.'}
 foreach($key in @('maximumNativeLoads','maximumNativeWrites')){Assert-KmcOrdinaryInteger $Row.$key 1 1}
 foreach($key in @('admittedNativeLoads','authorizedLoads')){Assert-KmcOrdinaryInteger $Row.$key $Loads $Loads}
 foreach($key in @('admittedNativeWrites','authorizedWrites')){Assert-KmcOrdinaryInteger $Row.$key $Writes $Writes}
 foreach($key in @('unauthorizedLoads','unauthorizedWrites','fatalViolations','suppressedWrites')){Assert-KmcOrdinaryInteger $Row.$key 0 0}
}
function Assert-KmcManualSeedObjectIdentity($Value){Assert-KmcOrdinaryInteger $Value ([int]::MinValue) ([int]::MaxValue);if($Value-eq0){throw 'A retained native object requires a nonzero signed Int32 label.'}}
function Assert-KmcManualSeedWorld($Row,$Request,$Labels,$SourceWorld,[long]$MinimumFrame,[long]$Writes){
 Assert-KmcOrdinaryInteger $Row.frame $MinimumFrame ([int]::MaxValue)
 $objects=[Collections.Generic.HashSet[int]]::new()
 foreach($key in @('game','saveManager','player','state','areaState','mainState')){Assert-KmcManualSeedObjectIdentity $Row.$key;if(-not$Labels.Contains([int]$Row.$key)-or-not$objects.Add([int]$Row.$key)){throw 'Cold world label was not retained or aliased another native role.'}}
 foreach($key in @('gameId','gameName','area')){Assert-KmcBootstrapInventoryString $Row.$key $Request.persistenceLoad.$key}
 Assert-KmcBootstrapInventoryString $Row.mainActorId $SourceWorld.mainActorId;Assert-KmcBootstrapInventoryString $Row.mode 'Default'
 foreach($key in @('cutscene','inCombat','nativeLoading')){Assert-KmcChunk7Boolean $Row.$key $false}
 foreach($key in @('owners','slots','casts','semanticRestores','presentationRestores')){Assert-KmcOrdinaryInteger $Row.$key 0 0}
 foreach($key in @('snapshots','completedSaves')){Assert-KmcOrdinaryInteger $Row.$key $Writes $Writes}
 if($Row.party-isnot[Array]-or$SourceWorld.party-isnot[Array]-or$Row.party.Count-ne$SourceWorld.party.Count-or$Row.party.Count-lt1-or$Row.party.Count-gt24){throw 'Cold world party differs from the exact source inventory.'}
 $ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
 foreach($actor in $Row.party){
  Assert-KmcOrdinaryGuid $actor.id;if(-not$ids.Add($actor.id)){throw 'Cold native actor ID repeated.'}
  $original=@($SourceWorld.party|Where-Object id -CEQ $actor.id);if($original.Count-ne1){throw 'Cold native actor differs from exact source.'}
  Assert-KmcBootstrapInventoryString $actor.blueprint $original[0].blueprint
  foreach($key in @('object','descriptor','view')){Assert-KmcManualSeedObjectIdentity $actor.$key;if(-not$Labels.Contains([int]$actor.$key)-or-not$objects.Add([int]$actor.$key)){throw 'Cold native object label is foreign or aliased.'}}
  foreach($key in @('isInGame','isInState','commandsEmpty')){Assert-KmcChunk7Boolean $actor.$key $true}
 }
 if(-not$ids.Contains($Row.mainActorId)){throw 'Cold main actor is absent from its exact party.'}
}

function Assert-KmcChunk7ManualSeedEvidence($Request,$Artifact,$SourceArtifact,$InventoryArtifact,[string]$Status='PASS'){
 Assert-KmcChunk7ManualSeedDescriptor $Request;Assert-KmcChunk7ArtifactEnvelope $Request $Artifact 'chunk7-manual-seed-evidence'
 if($Status-ceq'PASS'-and$Artifact.status-cne'PASS'){throw 'PASS cold inventory requires its PASS native artifact.'}
 if($Artifact.status-cne'PASS'){return}
 $o=$Artifact.observations;$s=$Request.manualSeed;$source=$SourceArtifact
 foreach($key in @('commit','dllSha256','dllMvid')){Assert-KmcBootstrapInventoryString $source.$key $Request.$key}
 Assert-KmcBootstrapInventoryString $source.runId $s.sourceRunId;Assert-KmcBootstrapInventoryString $source.scenario 'chunk7-native-main-bootstrap';Assert-KmcBootstrapInventoryString $source.status 'PASS'
 if($source.errors-isnot[Array]-or$source.errors.Count-ne0){throw 'Cold inventory source was not an error-free bootstrap.'}
 foreach($key in @('gameId','gameName','area')){Assert-KmcBootstrapInventoryString $source.observations.nativeWrite.$key $Request.persistenceLoad.$key}
 Assert-KmcBootstrapInventoryString $source.observations.nativeWrite.sha256 $s.sourceArchiveSha256
 if((ConvertTo-KmcChunk7Canonical $o.sourceNativeWorld)-cne(ConvertTo-KmcChunk7Canonical $source.observations.nativeWorld)){throw 'Cold inventory changed its qualified source native world.'}
 if((ConvertTo-KmcChunk7Canonical $o.selection)-cne(ConvertTo-KmcChunk7Canonical $s)){throw 'Cold inventory selection changed.'}
 Assert-KmcOrdinaryInteger $o.processId 1 ([int]::MaxValue);Assert-KmcOrdinaryInteger $source.observations.processId 1 ([int]::MaxValue)
 if($o.processId-eq$source.observations.processId){throw 'Cold inventory reused the native bootstrap process.'}
 foreach($key in @('resourceWrites','xpWrites','moneyWrites')){Assert-KmcOrdinaryInteger $o.$key 0 0}
 foreach($key in @('profileAdmissionChanged','ordinaryAcquisitionProven','sixPairFixtureQualified')){Assert-KmcChunk7Boolean $o.$key $false}
 foreach($key in @('runId','artifactSha256','archiveSha256')){$selectionKey=if($key-ceq'runId'){'sourceRunId'}elseif($key-ceq'artifactSha256'){'sourceArtifactSha256'}else{'sourceArchiveSha256'};Assert-KmcBootstrapInventoryString $o.source.$key $s.$selectionKey}
 Assert-KmcBootstrapInventoryString $o.source.archiveLeaf 'Auto_1.zks';Assert-KmcChunk7Boolean $o.source.sameCandidate $true;Assert-KmcChunk7Boolean $o.source.diagnosticArchiveContentRead $false
 Assert-KmcManualSeedColdLineage $Request $o $InventoryArtifact
 $b=$o.beforeLoad;$w=$o.loadedWorld;$v=$o.savedWorld;$f=$o.settledWorld;$load=$o.nativeLoad;$registry=$load.identities
 Assert-KmcBootstrapInventoryString $load.contract 'bootstrap-native-postload-v1';foreach($key in @('installed','installationAttempted','complete')){Assert-KmcChunk7Boolean $load.$key $true}
 foreach($key in @('entries','exits')){Assert-KmcOrdinaryInteger $load.$key 1 1}
 if($load.errors-isnot[Array]-or$load.errors.Count-ne0-or$load.events-isnot[Array]-or$load.events.Count-ne2-or$load.hooks-isnot[Array]-or$load.hooks.Count-ne1){throw 'Cold native PostLoad observer is incomplete.'}
 $h=$load.hooks[0];foreach($pair in @(@('declaringType','Kingmaker.Player'),@('method','PostLoad'),@('token','06000DC7'),@('moduleMvid','07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'))){Assert-KmcBootstrapInventoryString $h.($pair[0]) $pair[1]}
 Assert-KmcBootstrapInventoryString $registry.contract 'retained-native-object-identity-v1';Assert-KmcOrdinaryInteger $registry.capacity 128 128;Assert-KmcOrdinaryInteger $registry.faults 0 0;Assert-KmcChunk7Boolean $registry.released $false
 if($registry.ids-isnot[Array]-or$registry.ids.Count-lt1-or$registry.ids.Count-gt128){throw 'Cold native registry is missing or unbounded.'}
 Assert-KmcOrdinaryInteger $registry.retainedCount $registry.ids.Count $registry.ids.Count;$labels=[Collections.Generic.HashSet[int]]::new()
 foreach($id in $registry.ids){Assert-KmcManualSeedObjectIdentity $id;if(-not$labels.Add($id)){throw 'Cold native registry repeats a label.'}}
 $root=Join-Path (Join-Path (Join-Path (Split-Path (Split-Path $Request.evidenceRoot -Parent) -Parent) 'runtime-staging') ('persistence-'+$Request.runId)) 'Saved Games'
 Assert-KmcOrdinaryInteger $b.frame 0 ([int]::MaxValue)
 $beforeRoles=[Collections.Generic.HashSet[int]]::new()
 foreach($key in @('game','saveManager','menuPlayer')){Assert-KmcManualSeedObjectIdentity $b.$key;if(-not$labels.Contains([int]$b.$key)-or-not$beforeRoles.Add([int]$b.$key)){throw 'Before-load native owner was not retained or aliased another role.'}}
 foreach($key in @('loadedAreaPresent','nativeLoading')){Assert-KmcChunk7Boolean $b.$key $false}
 Assert-KmcChunk7ManualSeedAuthority $b.authority $root $b.authority.generation 0 0
 Assert-KmcManualSeedWorld $w $Request $labels $o.sourceNativeWorld $b.frame 0
 Assert-KmcManualSeedWorld $v $Request $labels $o.sourceNativeWorld $w.frame 1
  Assert-KmcManualSeedWorld $f $Request $labels $o.sourceNativeWorld $v.frame 1
 Assert-KmcChunk7ManualSeedAuthority $w.authority $root $b.authority.generation 1 0
 foreach($row in @($v,$f)){Assert-KmcChunk7ManualSeedAuthority $row.authority $root $b.authority.generation 1 1}
 foreach($row in @($w,$v,$f)){if($row.game-ne$b.game-or$row.saveManager-ne$b.saveManager){throw 'Manual seed replaced the exact Game or SaveManager.'}}
 foreach($key in @('game','saveManager','player','state','areaState','mainState','mainActorId','party')){if((ConvertTo-KmcChunk7Canonical $v.$key)-cne(ConvertTo-KmcChunk7Canonical $w.$key)){throw 'Manual write replaced its exact native world or actors.'}}
 foreach($key in @('game','saveManager','player','state','areaState','mainState','mainActorId','party')){if((ConvertTo-KmcChunk7Canonical $f.$key)-cne(ConvertTo-KmcChunk7Canonical $w.$key)){throw 'Final cold world or actors changed before cleanup.'}}
 $prior=$b.frame
 for($i=0;$i-lt2;$i++){$e=$load.events[$i];$kind=if($i-eq0){'native-player-postload-before'}else{'native-player-postload-after'};Assert-KmcBootstrapInventoryString $e.kind $kind;Assert-KmcOrdinaryInteger $e.frame $prior $w.frame;$prior=$e.frame
  foreach($key in @('game','saveManager','player')){Assert-KmcManualSeedObjectIdentity $e.$key;if($e.$key-ne$w.$key){throw 'Native PostLoad callback owner was substituted.'}}
  Assert-KmcBootstrapInventoryString $e.gameId $Request.persistenceLoad.gameId
  if($i-eq1){Assert-KmcBootstrapInventoryString $e.mainActorId $w.mainActorId}else{if($null-ne$e.mainActorId){Assert-KmcBootstrapInventoryString $e.mainActorId $w.mainActorId}}
 }
 if($load.events[0].frame-ne$load.events[1].frame){throw 'Native PostLoad did not return in its exact synchronous callback.'}
 $p=$o.noPreparation;Assert-KmcBootstrapInventoryString $p.contract 'chunk7-native-restoration-entry-observer-v1';Assert-KmcBootstrapInventoryString $p.prepareMetric 'native-method-entries-including-production-refused-attempts'
 foreach($key in @('complete','installedBeforeWorldLoad')){Assert-KmcChunk7Boolean $p.$key $true};Assert-KmcOrdinaryInteger $p.maximumEvents 2048 2048
 foreach($key in @('dropped','errors')){Assert-KmcOrdinaryInteger $p.$key 0 0}
 if($p.events-isnot[Array]-or$p.events.Count-ne0-or$p.hooks-isnot[Array]-or$p.hooks.Count-ne4){throw 'Cold inventory observed native preparation/round or actor/debt restoration.'}
 $tokens=@('06000C3C','0600939D');$methods=@('Prepare','NewRound','RestoreActor','RestoreDebt');$prefixes=@('PrepareBefore','RoundBefore','RestoreActorBefore','RestoreDebtBefore');$postfixes=@('PrepareAfter','RoundAfter','RestoreActorAfter','RestoreDebtAfter')
 for($i=0;$i-lt4;$i++){$h=$p.hooks[$i];Assert-KmcBootstrapInventoryString $h.method $methods[$i];Assert-KmcBootstrapInventoryString $h.prefix $prefixes[$i];Assert-KmcBootstrapInventoryString $h.postfix $postfixes[$i]
  if($i-lt2){$type=if($i-eq0){'TurnBased.Controllers.TurnController'}else{'Kingmaker.UnitLogic.UnitCombatState'};Assert-KmcBootstrapInventoryString $h.declaringType $type;Assert-KmcBootstrapInventoryString $h.token $tokens[$i];Assert-KmcBootstrapInventoryString $h.moduleMvid '07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'}else{Assert-KmcBootstrapInventoryString $h.moduleMvid $Request.dllMvid;Assert-KmcBootstrapInventoryString $h.declaringType 'KingmakerMountedCombat.Integration.NativeCombatActorPersistence';if($h.token-isnot[string]-or$h.token-cnotmatch'^06[0-9A-F]{6}$'){throw 'Product restore hook token invalid.'}}
 }
 $a=$o.archive;Assert-KmcOrdinaryInteger $a.schemaVersion 3 3;foreach($pair in @(@('campaignId','gameId'),@('areaId','area'))){Assert-KmcBootstrapInventoryString $a.($pair[0]) $Request.persistenceLoad.($pair[1])}
 foreach($key in @('pairs','participations','slots')){Assert-KmcOrdinaryInteger $a.$key 0 0};foreach($key in @('combatPresent','nativeRestorationGrantedByDiagnostic')){Assert-KmcChunk7Boolean $a.$key $false}
 $inventory=$o.nativeProfileInventory;Assert-KmcOrdinaryInteger $inventory.schemaVersion 1 1;Assert-KmcBootstrapInventoryString $inventory.captureStatus 'PASS';Assert-KmcBootstrapInventoryString $inventory.captureKind 'installed-native-small-body-catalogue';Assert-KmcBootstrapInventoryString $inventory.gameplayAssemblyMvid '07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'
 foreach($key in @('profileAdmissionChanged','nativeFixtureCreated','poseQualified','ordinaryAcquisitionProven')){Assert-KmcChunk7Boolean $inventory.$key $false}
 if($inventory.party-isnot[Array]-or$inventory.party.Count-ne$w.party.Count){throw 'Read-only native profile inventory lost its exact cold actors.'}
 foreach($actor in $w.party){$row=@($inventory.party|Where-Object id -CEQ $actor.id);if($row.Count-ne1){throw 'Profile inventory actor changed.'};Assert-KmcBootstrapInventoryString $row[0].id $actor.id;Assert-KmcBootstrapInventoryString $row[0].blueprintGuid $actor.blueprint
  $l=$row[0].nativeLevelUpReadiness;Assert-KmcOrdinaryInteger $l.characterLevel 0 20;Assert-KmcOrdinaryInteger $l.experience 0 ([int]::MaxValue);if($null-ne$l.nextLevelExperience){Assert-KmcOrdinaryInteger $l.nextLevelExperience 0 ([int]::MaxValue)}
  foreach($key in @('canLevelUp','isDead')){if($l.$key-isnot[bool]){throw 'Native readiness flag invalid.'}};foreach($key in @('partyInCombat','controllerCreated','completeChoicesObserved','ordinaryAcquisitionProven')){Assert-KmcChunk7Boolean $l.$key $false}
 }
 $r=$inventory.ordinaryAcquisitionPrerequisites;Assert-KmcChunk7Boolean $r.observationOnly $true;foreach($key in @('purchaseAuthorized','sixPairFixtureQualified','defaultHireMatchesPlayerExperienceExactly')){Assert-KmcChunk7Boolean $r.$key $false}
 foreach($key in @('nativeMoney','nativeHireCostNow')){Assert-KmcOrdinaryInteger $r.$key 0 ([long]::MaxValue)}
 foreach($key in @('mainCharacterExperience','nativeDefaultHireExperienceNow')){Assert-KmcOrdinaryInteger $r.$key 0 ([int]::MaxValue)}
 foreach($key in @('nativePartyLevel','mainCharacterLevel')){Assert-KmcOrdinaryInteger $r.$key 0 20};Assert-KmcOrdinaryInteger $r.nativeFieldCharacterLimit 6 6;Assert-KmcBootstrapInventoryString $r.mainCharacterId $w.mainActorId;Assert-KmcChunk7Boolean $r.nativeAffordableNow ($r.nativeMoney-ge$r.nativeHireCostNow)
 if($r.nativePartyCharacterIds-isnot[Array]-or$r.nativePartyCharacterIds.Count-lt1-or$r.nativePartyCharacterIds.Count-gt6){throw 'Native field character capacity observation invalid.'}
 $characters=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);foreach($id in $r.nativePartyCharacterIds){Assert-KmcOrdinaryGuid $id;if(-not$characters.Add($id)-or$id-cnotin@($w.party.id)){throw 'Native character inventory is duplicate or foreign.'}}
 $main=@($inventory.party|Where-Object id -CEQ $w.mainActorId)[0].nativeLevelUpReadiness
 if($r.mainCharacterLevel-ne$main.characterLevel-or$r.mainCharacterExperience-ne$main.experience){throw 'Native main readiness observations disagree.'}
 Assert-KmcBootstrapFixtureReadiness $o.fixtureReadiness $Request $w $f $inventory $root
 foreach($key in @('custodySettled','nativeWorkerSettled','observersReleased','worldAdmitted','sourcePinned','sourceUnchanged')){Assert-KmcChunk7Boolean $o.cleanup.$key $true}
 Assert-KmcChunk7ManualSeedAuthority $o.cleanup.authority $root $b.authority.generation 1 1
  Assert-KmcManualSeedNativeSave $Request $o $labels $root
}

function Assert-KmcManualSeedColdLineage($Request,$Observations,$InventoryArtifact){
 $s=$Request.manualSeed;$cold=$InventoryArtifact
 foreach($key in @('commit','dllSha256','dllMvid')){Assert-KmcBootstrapInventoryString $cold.$key $Request.$key}
 Assert-KmcBootstrapInventoryString $cold.runId $s.inventoryRunId;Assert-KmcBootstrapInventoryString $cold.scenario 'chunk7-bootstrap-cold-inventory';Assert-KmcBootstrapInventoryString $cold.evidenceKind 'chunk7-bootstrap-inventory-evidence';Assert-KmcBootstrapInventoryString $cold.status 'PASS'
 if($cold.errors-isnot[Array]-or$cold.errors.Count-ne0){throw 'Manual seed requires error-free cold inventory.'}
 if((ConvertTo-KmcChunk7Canonical $Observations.sourceColdInventory)-cne(ConvertTo-KmcChunk7Canonical $cold.observations)){throw 'Manual seed substituted its qualified cold observations.'}
 $p=$cold.observations.selection
 foreach($key in @('sourceRunId','sourceArtifactSha256','sourceArchiveSha256')){Assert-KmcBootstrapInventoryString $p.$key $s.$key}
 Assert-KmcBootstrapInventoryString $p.case 'authored-readonly-inventory';Assert-KmcChunk7Boolean $p.baseGameOnly $true
 Assert-KmcOrdinaryInteger $p.expectedNativeLoadCount 1 1;Assert-KmcOrdinaryInteger $p.expectedNativeWriteCount 0 0
 Assert-KmcOrdinaryInteger $cold.observations.processId 1 ([int]::MaxValue)
 if($cold.observations.processId-eq$Observations.processId){throw 'Manual seed reused its cold-inventory process.'}
 foreach($key in @('gameId','gameName','area')){Assert-KmcBootstrapInventoryString $cold.observations.loadedWorld.$key $Request.persistenceLoad.$key}
 Assert-KmcBootstrapInventoryString $cold.observations.loadedWorld.mainActorId $Observations.sourceNativeWorld.mainActorId
 foreach($key in @('mainProgression','staticPartyFacts','saveSettings','experienceRatePercent')){if((ConvertTo-KmcChunk7Canonical $Observations.fixtureReadiness.$key)-cne(ConvertTo-KmcChunk7Canonical $cold.observations.fixtureReadiness.$key)){throw 'Manual seed readiness changed from the actual qualified cold inventory.'}}
 if((ConvertTo-KmcChunk7Canonical $Observations.nativeProfileInventory.ordinaryAcquisitionPrerequisites)-cne(ConvertTo-KmcChunk7Canonical $cold.observations.nativeProfileInventory.ordinaryAcquisitionPrerequisites)){throw 'Manual seed eligibility observations changed.'}
 Assert-KmcChunk7Boolean $Observations.fixtureReadiness.saveSettings.onlyOneSave $false
 foreach($key in @('custodySettled','sourceUnchanged')){Assert-KmcChunk7Boolean $cold.observations.cleanup.$key $true}
}
function Assert-KmcManualSeedArrangement($Row,$World,$Inventory){
 Assert-KmcExactProperties $Row @('money','experienceRatePercent','actors') 'native save-only arrangement'
 Assert-KmcOrdinaryInteger $Row.money 0 ([long]::MaxValue);Assert-KmcOrdinaryInteger $Row.experienceRatePercent ([int]::MinValue) ([int]::MaxValue)
 if($Row.money-ne$Inventory.ordinaryAcquisitionPrerequisites.nativeMoney-or$Row.actors-isnot[Array]-or$Row.actors.Count-ne$World.party.Count){throw 'Save-only native arrangement differs from its admitted inventory.'}
 $ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
 foreach($actor in $Row.actors){
  Assert-KmcExactProperties $actor @('actorId','blueprint','experience','characterLevel','classes') 'save-only native actor'
  Assert-KmcOrdinaryGuid $actor.actorId;if(-not$ids.Add($actor.actorId)){throw 'Save-only actor repeated.'}
  $original=@($World.party|Where-Object id -CEQ $actor.actorId);$profile=@($Inventory.party|Where-Object id -CEQ $actor.actorId)
  if($original.Count-ne1-or$profile.Count-ne1){throw 'Save-only actor is foreign.'};Assert-KmcBootstrapInventoryString $actor.blueprint $original[0].blueprint
  Assert-KmcOrdinaryInteger $actor.experience 0 ([int]::MaxValue);Assert-KmcOrdinaryInteger $actor.characterLevel 0 20
  if($actor.experience-ne$profile[0].nativeLevelUpReadiness.experience-or$actor.characterLevel-ne$profile[0].nativeLevelUpReadiness.characterLevel-or$actor.classes-isnot[Array]-or$actor.classes.Count-gt20){throw 'Save-only progression changed.'}
  $classes=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);$total=0
  foreach($c in $actor.classes){Assert-KmcExactProperties $c @('classGuid','level') 'save-only class rank';if($c.classGuid-isnot[string]-or$c.classGuid-cnotmatch'^[a-f0-9]{32}$'-or-not$classes.Add($c.classGuid)){throw 'Save-only class identity invalid.'};Assert-KmcOrdinaryInteger $c.level 1 20;$total+=$c.level}
  if($total-ne$actor.characterLevel){throw 'Save-only class ranks differ from native level.'}
 }
}
function Assert-KmcManualSeedNativeSave($Request,$O,$Labels,[string]$Root){
 $leaf='Manual_1_KMC_C7_NATIVE_SEED.zks';$path=Join-Path $Root $leaf;$w=$O.loadedWorld;$f=$O.savedWorld;$n=$O.nativeNumbering;$r=$O.saveRequested;$p=$O.nativeSave;$write=$O.nativeWrite
 foreach($pair in @(@('method','FindUnusedSaveNumber'),@('token','06008024'),@('moduleMvid','07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'),@('nativeSaveType','Manual'),@('fileName',$leaf))){Assert-KmcBootstrapInventoryString $n.($pair[0]) $pair[1]}
 Assert-KmcOrdinaryInteger $n.number 1 1;Assert-KmcOrdinaryInteger $n.frame $w.frame $f.frame
 Assert-KmcChunk7Boolean $n.outputInitiallyAbsent $true;Assert-KmcChunk7Boolean $n.slotProjectionEnabled $false
 if((ConvertTo-KmcChunk7Canonical $n.sourceDescriptors)-cne(ConvertTo-KmcChunk7Canonical $O.fixtureReadiness.isolatedSaveDescriptors.descriptors)){throw 'Native numbering did not use the exact sole Auto descriptor inventory.'}
 Assert-KmcOrdinaryInteger $r.frame $n.frame $f.frame;Assert-KmcManualSeedObjectIdentity $r.descriptor
 Assert-KmcBootstrapInventoryString $r.name 'KMC_C7_NATIVE_SEED';Assert-KmcBootstrapInventoryString $r.type 'Manual'
 foreach($key in @('nativeSaveAllowed','newSlot')){Assert-KmcChunk7Boolean $r.$key $true};Assert-KmcOrdinaryInteger $r.snapshotCount 0 0
 Assert-KmcChunk7ManualSeedAuthority $r.authority $Root $O.beforeLoad.authority.generation 1 0
 Assert-KmcBootstrapInventoryString $p.contract 'manual-seed-native-save-v1'
 foreach($key in @('installed','installationAttempted','complete')){Assert-KmcChunk7Boolean $p.$key $true}
 foreach($key in @('requests','prepares','starts','stages','completions','callbacks')){Assert-KmcOrdinaryInteger $p.$key 1 1}
 if($p.errors-isnot[Array]-or$p.errors.Count-ne0-or$p.events-isnot[Array]-or$p.events.Count-ne6-or$p.hooks-isnot[Array]-or$p.hooks.Count-ne3){throw 'Manual seed native observer is incomplete or unbounded.'}
 $names=@('SaveRoutine','PrepareSave','RecordCompletedSave');$tokens=@('06008029','06008025')
 for($i=0;$i-lt3;$i++){$h=$p.hooks[$i];Assert-KmcBootstrapInventoryString $h.method $names[$i]
  if($i-lt2){Assert-KmcBootstrapInventoryString $h.type 'Kingmaker.EntitySystem.Persistence.SaveManager';Assert-KmcBootstrapInventoryString $h.token $tokens[$i];Assert-KmcBootstrapInventoryString $h.moduleMvid '07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'}else{Assert-KmcBootstrapInventoryString $h.type 'KingmakerMountedCombat.Integration.MountedPersistenceService';Assert-KmcBootstrapInventoryString $h.moduleMvid $Request.dllMvid;if($h.token-isnot[string]-or$h.token-cnotmatch'^06[0-9A-F]{6}$'){throw 'Manual completion hook token invalid.'}}
 }
 $roles=[Collections.Generic.HashSet[int]]::new();[void]$roles.Add([int]$O.beforeLoad.menuPlayer);foreach($key in @('game','saveManager','player','state','areaState','mainState')){[void]$roles.Add([int]$w.$key)}
 foreach($actor in $w.party){foreach($key in @('object','descriptor','view')){[void]$roles.Add([int]$actor.$key)}}
 foreach($value in @($r.descriptor,$write.writtenDescriptor,$p.events[4].scope,$p.events[4].iterator,$p.events[4].worker)){
  Assert-KmcManualSeedObjectIdentity $value;if(-not$Labels.Contains([int]$value)-or-not$roles.Add([int]$value)){throw 'Native save object identity is unretained or aliased.'}
 }
 $kinds=@('native-save-request','native-save-prepared','snapshot-starting','snapshot-staged','native-worker-completed','native-save-callback');$prior=$r.frame
 for($i=0;$i-lt6;$i++){$e=$p.events[$i];Assert-KmcBootstrapInventoryString $e.kind $kinds[$i];Assert-KmcOrdinaryInteger $e.sequence ($i+1) ($i+1);Assert-KmcOrdinaryInteger $e.frame $prior $f.frame;$prior=$e.frame
  foreach($key in @('game','saveManager','player')){Assert-KmcOrdinaryInteger $e.$key $w.$key $w.$key}
  Assert-KmcOrdinaryInteger $e.requested $r.descriptor $r.descriptor
  $descriptor=if($i-eq0){$r.descriptor}else{$write.writtenDescriptor};Assert-KmcOrdinaryInteger $e.descriptor $descriptor $descriptor
  Assert-KmcBootstrapInventoryString $e.internalName 'KMC_C7_NATIVE_SEED';Assert-KmcBootstrapInventoryString $e.type 'Manual';Assert-KmcBootstrapInventoryString $e.gameName $Request.persistenceLoad.gameName;Assert-KmcBootstrapInventoryString $e.area $Request.persistenceLoad.area
  if($i-eq0){foreach($key in @('fileName','gameId','path')){if($null-ne$e.$key){throw 'Fresh requested descriptor was already prepared or saved.'}}}else{Assert-KmcBootstrapInventoryString $e.fileName $leaf;Assert-KmcBootstrapInventoryString $e.path $path;Assert-KmcBootstrapInventoryString $e.gameId $Request.persistenceLoad.gameId}
  $snapshots=if($i-lt3){0}else{1};$completed=if($i-lt5){0}else{1};Assert-KmcOrdinaryInteger $e.snapshotCount $snapshots $snapshots;Assert-KmcOrdinaryInteger $e.completedSaveCount $completed $completed
 }
 $worker=$p.events[4];Assert-KmcBootstrapInventoryString $worker.iteratorType 'Kingmaker.EntitySystem.Persistence.SaveManager+<SaveRoutine>d__46';Assert-KmcBootstrapInventoryString $worker.workerStatus 'RanToCompletion';Assert-KmcOrdinaryInteger $worker.workerId 1 ([int]::MaxValue)
 foreach($key in @('nativeWorkerMatches','workerCompleted')){Assert-KmcChunk7Boolean $worker.$key $true};foreach($key in @('workerFaulted','workerCanceled')){Assert-KmcChunk7Boolean $worker.$key $false}
 foreach($pair in @(@('path',$path),@('fileName',$leaf),@('internalName','KMC_C7_NATIVE_SEED'),@('type','Manual'),@('operationState','None'))){Assert-KmcBootstrapInventoryString $write.($pair[0]) $pair[1]}
 foreach($key in @('gameId','gameName','area')){Assert-KmcBootstrapInventoryString $write.$key $Request.persistenceLoad.$key}
 Assert-KmcOrdinaryInteger $write.requestedDescriptor $r.descriptor $r.descriptor
 foreach($key in @('completionSequence','callbackCount','snapshotCount')){Assert-KmcOrdinaryInteger $write.$key 1 1}
 foreach($key in @('hasFileOnDisk','isActuallySaved','nativeWorkerSettled')){Assert-KmcChunk7Boolean $write.$key $true};foreach($key in @('autoLevelup','slotProjectionEnabled')){Assert-KmcChunk7Boolean $write.$key $false}
 if($write.sha256-isnot[string]-or$write.sha256-cnotmatch'^[a-f0-9]{64}$'-or$write.sha256-ceq$Request.persistenceLoad.sha256-or$write.sha256-ceq$Request.fixture.working.sha256-or$write.sha256-ceq$Request.fixture.baseline.sha256){throw 'Manual output must be a fresh native archive.'}
 Assert-KmcOrdinaryInteger $write.length 1 268435456;Assert-KmcOrdinaryInteger $write.lastWriteTimeUtcTicks 1 ([DateTime]::MaxValue.Ticks)
 if($O.settledDescriptors-isnot[Array]-or$O.settledDescriptors.Count-ne2){throw 'Settled Manual seed must retain exactly its immutable input and fresh output.'}
 $leaves=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
 foreach($d in $O.settledDescriptors){if($d.fileName-isnot[string]-or-not$leaves.Add($d.fileName)){throw 'Settled descriptor leaf is invalid or duplicate.'}
  $source=$d.fileName-ceq'Auto_1.zks';if(-not$source-and$d.fileName-cne$leaf){throw 'Foreign settled descriptor.'}
  Assert-KmcManualSeedObjectIdentity $d.descriptor;if(-not$Labels.Contains([int]$d.descriptor)){throw 'Settled descriptor label was not retained.'}
  if($source){if(-not$roles.Add([int]$d.descriptor)){throw 'Source descriptor aliases a native owner.'};Assert-KmcBootstrapInventoryString $d.internalName $Request.persistenceLoad.internalName;Assert-KmcBootstrapInventoryString $d.type 'Auto'}else{Assert-KmcOrdinaryInteger $d.descriptor $write.writtenDescriptor $write.writtenDescriptor;Assert-KmcBootstrapInventoryString $d.internalName 'KMC_C7_NATIVE_SEED';Assert-KmcBootstrapInventoryString $d.type 'Manual'}
  foreach($key in @('gameId','gameName','area')){Assert-KmcBootstrapInventoryString $d.$key $Request.persistenceLoad.$key};Assert-KmcBootstrapInventoryString $d.operationState 'None'
  foreach($key in @('isActuallySaved','hasFileOnDisk')){Assert-KmcChunk7Boolean $d.$key $true};Assert-KmcChunk7Boolean $d.autoLevelup $false
 }
 if(-not$leaves.Contains('Auto_1.zks')-or-not$leaves.Contains($leaf)){throw 'Native settled two-entry inventory differs.'}
 foreach($row in @($O.beforeSaveArrangement,$O.afterSaveArrangement)){Assert-KmcManualSeedArrangement $row $w $O.nativeProfileInventory;Assert-KmcOrdinaryInteger $row.experienceRatePercent $O.fixtureReadiness.experienceRatePercent $O.fixtureReadiness.experienceRatePercent}
 if((ConvertTo-KmcChunk7Canonical $O.beforeSaveArrangement)-cne(ConvertTo-KmcChunk7Canonical $O.afterSaveArrangement)){throw 'Save-only derivative changed native money, XP, class ranks or actor identity.'}
}
function Assert-KmcChunk7ManualSeedArtifact($Request,$Manifest,[string]$Status='PASS'){
 $records=@($Manifest.artifacts|Where-Object{$_.kind-ceq'chunk7-manual-seed-evidence'-or$_.relativePath-ceq'chunk7-manual-seed-evidence.json'})
 if(-not(Test-KmcChunk7ManualSeedScenario $Request.scenario)){if($records.Count){throw 'Foreign request supplied Manual seed evidence.'};return}
 if($Status-ceq'FAIL'-and$records.Count-eq0){return}
 $artifact=Read-KmcChunk7ManifestArtifact $Request $Manifest 'chunk7-manual-seed-evidence.json' 'chunk7-manual-seed-evidence';$source=$null;$cold=$null
 if($artifact.status-ceq'PASS'){
  foreach($spec in @(@($Request.manualSeed.sourceRunId,'chunk7-native-bootstrap-evidence.json',$Request.manualSeed.sourceArtifactSha256),@($Request.manualSeed.inventoryRunId,'chunk7-bootstrap-inventory-evidence.json',$Request.manualSeed.inventoryArtifactSha256))){
   $path=Join-Path (Join-Path (Split-Path $Request.evidenceRoot -Parent) $spec[0]) $spec[1]
   Assert-KmcNotReparsePoint $path 'qualified Manual seed lineage';Assert-KmcNotHardLink $path 'qualified Manual seed lineage';$file=Get-Item -LiteralPath $path
   if($file.Length-le0-or$file.Length-gt16MB-or(Get-KmcSha256 $path)-cne$spec[2]){throw 'Qualified Manual seed lineage is absent or changed.'}
   Assert-KmcManifestJsonMembersUnique $path 'qualified Manual seed lineage';$value=Read-KmcJson $path
   if((Get-KmcSha256 $path)-cne$spec[2]){throw 'Qualified Manual seed lineage changed while reading.'}
   if($spec[1]-ceq'chunk7-native-bootstrap-evidence.json'){$source=$value}else{$cold=$value}
  }
 }
 Assert-KmcChunk7ManualSeedEvidence $Request $artifact $source $cold $Status
}
