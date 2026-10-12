Set-StrictMode -Version Latest
# RetainedObjectIdentity labels are RuntimeHelpers hash values; capacity bounds their count.
function Assert-KmcBootstrapInventoryObject($Value){Assert-KmcOrdinaryInteger $Value ([int]::MinValue) ([int]::MaxValue);if($Value-eq0){throw 'Cold inventory object identity is zero.'}}
function Test-KmcChunk7BootstrapInventoryScenario([string]$Scenario){$Scenario-ceq'chunk7-bootstrap-cold-inventory'}
function Get-KmcChunk7BootstrapInventoryRows {@('chunk7-bootstrap-cold-inventory','C7-bootstrap-inventory-source','C7-bootstrap-inventory-native-load','C7-bootstrap-inventory-readonly','C7-bootstrap-inventory-cleanup','C7-bootstrap-inventory-runtime','C7-bootstrap-inventory-interrupted')}
function Assert-KmcBootstrapInventoryString($Value,$Expected){if($Value-isnot[string]-or$Expected-isnot[string]-or$Value-cne$Expected){throw 'Bootstrap inventory exact scalar identity differs.'}}
function Assert-KmcChunk7BootstrapInventorySelection($Request){
 $has=$null-ne$Request.PSObject.Properties['bootstrapInventory']
 if(-not(Test-KmcChunk7BootstrapInventoryScenario $Request.scenario)){if($has){throw 'Bootstrap inventory selection is forbidden under another scenario.'};return}
 if(-not$has){throw 'Bootstrap inventory requires its explicit closed selection.'};$s=$Request.bootstrapInventory
 Assert-KmcExactProperties $s @('case','sourceRunId','sourceArtifactSha256','sourceArchiveSha256','baseGameOnly','expectedNativeLoadCount','expectedNativeWriteCount') 'bootstrap cold selection'
 Assert-KmcBootstrapInventoryString $s.case 'authored-readonly-inventory'
 if($s.sourceRunId-isnot[string]-or$s.sourceRunId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'-or$s.sourceRunId-ceq$Request.runId){throw 'A distinct completed native bootstrap source is required.'}
 foreach($key in @('sourceArtifactSha256','sourceArchiveSha256')){if($s.$key-isnot[string]-or$s.$key-cnotmatch'^[a-f0-9]{64}$'){throw 'Bootstrap immutable source hashes are required.'}}
 Assert-KmcChunk7Boolean $s.baseGameOnly $true;Assert-KmcOrdinaryInteger $s.expectedNativeLoadCount 1 1;Assert-KmcOrdinaryInteger $s.expectedNativeWriteCount 0 0
}
function Assert-KmcChunk7BootstrapInventoryDescriptor($Request){
 Assert-KmcChunk7BootstrapInventorySelection $Request
 if(-not(Test-KmcChunk7BootstrapInventoryScenario $Request.scenario)){throw 'Bootstrap cold descriptor is exclusive to its exact family.'}
 $d=$Request.persistenceLoad
 Assert-KmcExactProperties $d @('internalName','fileName','sha256','length','lastWriteTimeUtcTicks','gameId','gameName','area') 'bootstrap cold descriptor'
 Assert-KmcBootstrapInventoryString $d.fileName 'Auto_1.zks';Assert-KmcBootstrapInventoryString $d.sha256 $Request.bootstrapInventory.sourceArchiveSha256
 foreach($key in @('internalName','gameName')){if($d.$key-isnot[string]-or[string]::IsNullOrWhiteSpace($d.$key)-or$d.$key.Length-gt256-or$d.$key-match'[\x00-\x1f\x7f]'){throw 'Native bootstrap save name is malformed.'}}
 Assert-KmcOrdinaryGuid $d.gameId
 if($d.gameId-ceq$Request.fixture.working.gameId-or$d.sha256-ceq$Request.fixture.working.sha256-or$d.sha256-ceq$Request.fixture.baseline.sha256-or
    $d.area-isnot[string]-or$d.area-cnotmatch'^[a-f0-9]{32}$'){throw 'Bootstrap source must have its separately engine-minted campaign and exact area.'}
 Assert-KmcOrdinaryInteger $d.length 1 268435456;Assert-KmcOrdinaryInteger $d.lastWriteTimeUtcTicks 1
}
function Assert-KmcChunk7BootstrapInventoryAuthority($Row,[string]$Root,[long]$Generation,[long]$Loads){
 Assert-KmcBootstrapInventoryString $Row.root $Root;Assert-KmcOrdinaryInteger $Row.generation 1 ([int]::MaxValue)
 if($Row.generation-ne$Generation-or$null-ne$Row.maximumNativeWrites){throw 'Bootstrap cold authority or zero-writable plan changed.'}
 Assert-KmcOrdinaryInteger $Row.maximumNativeLoads 1 1
 foreach($key in @('admittedNativeLoads','authorizedLoads')){Assert-KmcOrdinaryInteger $Row.$key $Loads $Loads}
 foreach($key in @('admittedNativeWrites','authorizedWrites','unauthorizedLoads','unauthorizedWrites','fatalViolations','suppressedWrites')){Assert-KmcOrdinaryInteger $Row.$key 0 0}
}
function Assert-KmcBootstrapInventoryWorld($Row,$Request,$Labels,$SourceWorld,[long]$MinimumFrame){
 Assert-KmcOrdinaryInteger $Row.frame $MinimumFrame ([int]::MaxValue)
 $objects=[Collections.Generic.HashSet[int]]::new()
 foreach($key in @('game','saveManager','player','state','areaState','mainState')){Assert-KmcBootstrapInventoryObject $Row.$key;if(-not$Labels.Contains([int]$Row.$key)-or-not$objects.Add([int]$Row.$key)){throw 'Cold world label was not retained or aliased another native role.'}}
 foreach($key in @('gameId','gameName','area')){Assert-KmcBootstrapInventoryString $Row.$key $Request.persistenceLoad.$key}
 Assert-KmcBootstrapInventoryString $Row.mainActorId $SourceWorld.mainActorId;Assert-KmcBootstrapInventoryString $Row.mode 'Default'
 foreach($key in @('cutscene','inCombat','nativeLoading')){Assert-KmcChunk7Boolean $Row.$key $false}
 foreach($key in @('owners','slots','casts','snapshots','completedSaves','semanticRestores','presentationRestores')){Assert-KmcOrdinaryInteger $Row.$key 0 0}
 if($Row.party-isnot[Array]-or$SourceWorld.party-isnot[Array]-or$Row.party.Count-ne$SourceWorld.party.Count-or$Row.party.Count-lt1-or$Row.party.Count-gt24){throw 'Cold world party differs from the exact source inventory.'}
 $ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
 foreach($actor in $Row.party){
  Assert-KmcOrdinaryGuid $actor.id;if(-not$ids.Add($actor.id)){throw 'Cold native actor ID repeated.'}
  $original=@($SourceWorld.party|Where-Object id -CEQ $actor.id);if($original.Count-ne1){throw 'Cold native actor differs from exact source.'}
  Assert-KmcBootstrapInventoryString $actor.blueprint $original[0].blueprint
  foreach($key in @('object','descriptor','view')){Assert-KmcBootstrapInventoryObject $actor.$key;if(-not$Labels.Contains([int]$actor.$key)-or-not$objects.Add([int]$actor.$key)){throw 'Cold native object label is foreign or aliased.'}}
  foreach($key in @('isInGame','isInState','commandsEmpty')){Assert-KmcChunk7Boolean $actor.$key $true}
 }
 if(-not$ids.Contains($Row.mainActorId)){throw 'Cold main actor is absent from its exact party.'}
}
function Assert-KmcBootstrapFixtureReadiness($Row,$Request,$World,$Settled,$Inventory,[string]$Root){
 Assert-KmcExactProperties $Row @('schemaVersion','contract','captureStatus','frame','gameplayAssemblyMvid','observationOnly','saveSettings','experienceRatePercent','mainProgression','staticPartyFacts','isolatedSaveDescriptors') 'cold fixture readiness'
 Assert-KmcOrdinaryInteger $Row.schemaVersion 1 1;Assert-KmcOrdinaryInteger $Row.frame $World.frame $Settled.frame
 Assert-KmcBootstrapInventoryString $Row.contract 'installed-native-fixture-readiness-v1';Assert-KmcBootstrapInventoryString $Row.captureStatus 'PASS';Assert-KmcBootstrapInventoryString $Row.gameplayAssemblyMvid '07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';Assert-KmcChunk7Boolean $Row.observationOnly $true
 Assert-KmcOrdinaryInteger $Row.experienceRatePercent ([int]::MinValue) ([int]::MaxValue)
 $s=$Row.saveSettings;Assert-KmcExactProperties $s @('observationKind','projectionEnabled','autosaveSlotsNativeType','autosaveCapacityInferred','sliderFallbackCallbackPresent','settingsWritten','onlyOneSave','autosaveEnabled','autosaveSlots') 'observed native save settings'
 Assert-KmcBootstrapInventoryString $s.observationKind 'native-effective-getters';Assert-KmcBootstrapInventoryString $s.autosaveSlotsNativeType 'System.Single'
 foreach($key in @('projectionEnabled','autosaveCapacityInferred','sliderFallbackCallbackPresent','settingsWritten')){Assert-KmcChunk7Boolean $s.$key $false}
 foreach($key in @('onlyOneSave','autosaveEnabled')){if($s.$key-isnot[bool]){throw 'Native save setting must remain a boolean observation.'}}
 if(-not(Test-KmcFiniteJsonNumber $s.autosaveSlots)){throw 'Native slot setting must remain a finite numeric observation.'}
 try{$nativeSlots=[single]$s.autosaveSlots}catch{throw 'Native slot setting cannot convert to Single.'}
 if([single]::IsNaN($nativeSlots)-or[single]::IsInfinity($nativeSlots)-or([double]$s.autosaveSlots-ne0.0-and$nativeSlots-eq0.0)){throw 'Native slot setting overflows or underflows Single; no slot capacity is inferred.'}
 $m=$Row.mainProgression;Assert-KmcExactProperties $m @('actorId','characterLevel','experience','classes') 'main native classes';Assert-KmcBootstrapInventoryString $m.actorId $World.mainActorId
 Assert-KmcOrdinaryInteger $m.characterLevel 1 20;Assert-KmcOrdinaryInteger $m.experience 0 ([int]::MaxValue)
 if($m.characterLevel-ne$Inventory.ordinaryAcquisitionPrerequisites.mainCharacterLevel-or$m.experience-ne$Inventory.ordinaryAcquisitionPrerequisites.mainCharacterExperience-or$m.classes-isnot[Array]-or$m.classes.Count-lt1-or$m.classes.Count-gt20){throw 'Native main class observation differs from its readiness or is unbounded.'}
 $classes=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);$levels=0
 foreach($c in $m.classes){Assert-KmcExactProperties $c @('classGuid','level','archetypeGuids') 'native class rank'
  if($c.classGuid-isnot[string]-or$c.classGuid-cnotmatch'^[a-f0-9]{32}$'-or-not$classes.Add($c.classGuid)){throw 'Native class GUID is malformed or repeated.'};Assert-KmcOrdinaryInteger $c.level 1 20;$levels+=$c.level
  if($c.archetypeGuids-isnot[Array]-or$c.archetypeGuids.Count-gt16){throw 'Native archetypes are unbounded.'};$archetypes=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
  foreach($a in $c.archetypeGuids){if($a-isnot[string]-or$a-cnotmatch'^[a-f0-9]{32}$'-or-not$archetypes.Add($a)){throw 'Native archetype GUID is malformed or repeated.'}}
 }
 if($levels-ne$m.characterLevel){throw 'Native class ranks do not sum to the observed main level.'}
 $p=$Row.staticPartyFacts;Assert-KmcExactProperties $p @('selectorOpened','liveSelectorRequiredAuthorityAvailable','membershipOrRemovalQualified','characters') 'static native party facts'
 foreach($key in @('selectorOpened','liveSelectorRequiredAuthorityAvailable','membershipOrRemovalQualified')){Assert-KmcChunk7Boolean $p.$key $false}
 if($p.characters-isnot[Array]-or$p.characters.Count-lt1-or$p.characters.Count-gt6){throw 'Static native party-character facts are unbounded.'}
 $ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);$ordered=@()
 foreach($c in $p.characters){Assert-KmcExactProperties $c @('actorId','blueprintGuid','isMain','lockedCompanionComponent') 'static character fact';Assert-KmcOrdinaryGuid $c.actorId
  if(-not$ids.Add($c.actorId)){throw 'Static native actor repeated.'};$ordered+=,$c.actorId;$actor=@($World.party|Where-Object id -CEQ $c.actorId);if($actor.Count-ne1){throw 'Static native actor is foreign.'};Assert-KmcBootstrapInventoryString $c.blueprintGuid $actor[0].blueprint
  Assert-KmcChunk7Boolean $c.isMain ($c.actorId-ceq$World.mainActorId);if($c.lockedCompanionComponent-isnot[bool]){throw 'Locked-companion observation must remain a boolean fact.'}
 }
 if(-not$ids.Contains($World.mainActorId)-or(ConvertTo-KmcChunk7Canonical $ordered)-cne(ConvertTo-KmcChunk7Canonical $Inventory.ordinaryAcquisitionPrerequisites.nativePartyCharacterIds)){throw 'Static party facts differ from the actual native character roster.'}
 $d=$Row.isolatedSaveDescriptors;Assert-KmcExactProperties $d @('root','metadataOnly','maximumObservedDescriptors','descriptors') 'isolated descriptor observation';Assert-KmcBootstrapInventoryString $d.root $Root;Assert-KmcChunk7Boolean $d.metadataOnly $true;Assert-KmcOrdinaryInteger $d.maximumObservedDescriptors 32 32
 if($d.descriptors-isnot[Array]-or$d.descriptors.Count-ne1){throw 'Read-only isolated inventory must contain exactly its admitted Auto_1 descriptor.'};$v=$d.descriptors[0]
 Assert-KmcExactProperties $v @('fileName','internalName','gameId','gameName','area','type','isAutoLevelupSave','isActuallySaved','hasFileOnDisk','operationState','systemSaveTimeTicks','systemSaveTimeKind') 'isolated native descriptor'
 foreach($key in @('fileName','internalName','gameId','gameName','area')){Assert-KmcBootstrapInventoryString $v.$key $Request.persistenceLoad.$key}
 Assert-KmcBootstrapInventoryString $v.type 'Auto';Assert-KmcBootstrapInventoryString $v.operationState 'None';Assert-KmcChunk7Boolean $v.hasFileOnDisk $true;Assert-KmcChunk7Boolean $v.isAutoLevelupSave $false
 # Installed getters06007FE1/06007FE2 both return nonempty FolderName; a disk-backed settled descriptor has both true.
 Assert-KmcChunk7Boolean $v.isActuallySaved $true;Assert-KmcOrdinaryInteger $v.systemSaveTimeTicks 0 ([DateTime]::MaxValue.Ticks)
 if($v.systemSaveTimeKind-isnot[string]-or$v.systemSaveTimeKind-cnotin@('Unspecified','Utc','Local')){throw 'Native save timestamp kind is malformed.'}
}
function Assert-KmcChunk7BootstrapInventoryEvidence($Request,$Artifact,$SourceArtifact,[string]$Status='PASS'){
 Assert-KmcChunk7BootstrapInventoryDescriptor $Request;Assert-KmcChunk7ArtifactEnvelope $Request $Artifact 'chunk7-bootstrap-inventory-evidence'
 if($Status-ceq'PASS'-and$Artifact.status-cne'PASS'){throw 'PASS cold inventory requires its PASS native artifact.'}
 if($Artifact.status-cne'PASS'){return}
 $o=$Artifact.observations;$s=$Request.bootstrapInventory;$source=$SourceArtifact
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
 $b=$o.beforeLoad;$w=$o.loadedWorld;$f=$o.settledWorld;$load=$o.nativeLoad;$registry=$load.identities
 Assert-KmcBootstrapInventoryString $load.contract 'bootstrap-native-postload-v1';foreach($key in @('installed','installationAttempted','complete')){Assert-KmcChunk7Boolean $load.$key $true}
 foreach($key in @('entries','exits')){Assert-KmcOrdinaryInteger $load.$key 1 1}
 if($load.errors-isnot[Array]-or$load.errors.Count-ne0-or$load.events-isnot[Array]-or$load.events.Count-ne2-or$load.hooks-isnot[Array]-or$load.hooks.Count-ne1){throw 'Cold native PostLoad observer is incomplete.'}
 $h=$load.hooks[0];foreach($pair in @(@('declaringType','Kingmaker.Player'),@('method','PostLoad'),@('token','06000DC7'),@('moduleMvid','07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'))){Assert-KmcBootstrapInventoryString $h.($pair[0]) $pair[1]}
 Assert-KmcBootstrapInventoryString $registry.contract 'retained-native-object-identity-v1';Assert-KmcOrdinaryInteger $registry.capacity 128 128;Assert-KmcOrdinaryInteger $registry.faults 0 0;Assert-KmcChunk7Boolean $registry.released $false
 if($registry.ids-isnot[Array]-or$registry.ids.Count-lt1-or$registry.ids.Count-gt128){throw 'Cold native registry is missing or unbounded.'}
 Assert-KmcOrdinaryInteger $registry.retainedCount $registry.ids.Count $registry.ids.Count;$labels=[Collections.Generic.HashSet[int]]::new()
 foreach($id in $registry.ids){Assert-KmcBootstrapInventoryObject $id;if(-not$labels.Add($id)){throw 'Cold native registry repeats a label.'}}
 $root=Join-Path (Join-Path (Join-Path (Split-Path (Split-Path $Request.evidenceRoot -Parent) -Parent) 'runtime-staging') ('persistence-'+$Request.runId)) 'Saved Games'
 Assert-KmcOrdinaryInteger $b.frame 0 ([int]::MaxValue)
 $beforeRoles=[Collections.Generic.HashSet[int]]::new()
 foreach($key in @('game','saveManager','menuPlayer')){Assert-KmcBootstrapInventoryObject $b.$key;if(-not$labels.Contains([int]$b.$key)-or-not$beforeRoles.Add([int]$b.$key)){throw 'Before-load native owner was not retained or aliased another role.'}}
 foreach($key in @('loadedAreaPresent','nativeLoading')){Assert-KmcChunk7Boolean $b.$key $false}
 Assert-KmcChunk7BootstrapInventoryAuthority $b.authority $root $b.authority.generation 0
 Assert-KmcBootstrapInventoryWorld $w $Request $labels $o.sourceNativeWorld $b.frame
 Assert-KmcBootstrapInventoryWorld $f $Request $labels $o.sourceNativeWorld $w.frame
 foreach($row in @($w,$f)){Assert-KmcChunk7BootstrapInventoryAuthority $row.authority $root $b.authority.generation 1;if($row.game-ne$b.game-or$row.saveManager-ne$b.saveManager){throw 'Cold load replaced the exact Game or SaveManager.'}}
 foreach($key in @('game','saveManager','player','state','areaState','mainState','mainActorId','party')){if((ConvertTo-KmcChunk7Canonical $f.$key)-cne(ConvertTo-KmcChunk7Canonical $w.$key)){throw 'Final cold world or actors changed before cleanup.'}}
 $prior=$b.frame
 for($i=0;$i-lt2;$i++){$e=$load.events[$i];$kind=if($i-eq0){'native-player-postload-before'}else{'native-player-postload-after'};Assert-KmcBootstrapInventoryString $e.kind $kind;Assert-KmcOrdinaryInteger $e.frame $prior $w.frame;$prior=$e.frame
  foreach($key in @('game','saveManager','player')){Assert-KmcBootstrapInventoryObject $e.$key;if($e.$key-ne$w.$key){throw 'Native PostLoad callback owner was substituted.'}}
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
 Assert-KmcChunk7BootstrapInventoryAuthority $o.cleanup.authority $root $b.authority.generation 1
}
function Assert-KmcChunk7BootstrapInventoryArtifact($Request,$Manifest,[string]$Status='PASS'){
 $records=@($Manifest.artifacts|Where-Object{$_.kind-ceq'chunk7-bootstrap-inventory-evidence'-or$_.relativePath-ceq'chunk7-bootstrap-inventory-evidence.json'})
 if(-not(Test-KmcChunk7BootstrapInventoryScenario $Request.scenario)){if($records.Count){throw 'Foreign request supplied bootstrap cold inventory evidence.'};return}
 if($Status-ceq'FAIL'-and$records.Count-eq0){return}
 $artifact=Read-KmcChunk7ManifestArtifact $Request $Manifest 'chunk7-bootstrap-inventory-evidence.json' 'chunk7-bootstrap-inventory-evidence';$source=$null
 if($artifact.status-ceq'PASS'){
  $path=Join-Path (Join-Path (Split-Path $Request.evidenceRoot -Parent) $Request.bootstrapInventory.sourceRunId) 'chunk7-native-bootstrap-evidence.json'
  Assert-KmcNotReparsePoint $path 'qualified bootstrap source evidence';Assert-KmcNotHardLink $path 'qualified bootstrap source evidence'
  $file=Get-Item -LiteralPath $path;if($file.Length-le0-or$file.Length-gt16MB-or(Get-KmcSha256 $path)-cne$Request.bootstrapInventory.sourceArtifactSha256){throw 'Qualified bootstrap source evidence is absent or changed.'}
  Assert-KmcManifestJsonMembersUnique $path 'qualified bootstrap source evidence';$source=Read-KmcJson $path
  if((Get-KmcSha256 $path)-cne$Request.bootstrapInventory.sourceArtifactSha256){throw 'Qualified source evidence changed while reading.'}
 }
 Assert-KmcChunk7BootstrapInventoryEvidence $Request $artifact $source $Status
}
