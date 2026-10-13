Set-StrictMode -Version Latest
function Test-KmcChunk7AcquiredManualScenario([string]$Scenario){$Scenario-ceq'chunk7-acquired-manual-derivative'}
function Get-KmcChunk7AcquiredManualRows {@('chunk7-acquired-manual-derivative','C7-acquired-manual-source','C7-acquired-manual-native-load','C7-acquired-manual-restored-owner','C7-acquired-manual-native-field','C7-acquired-manual-numbering','C7-acquired-manual-native-save','C7-acquired-manual-no-arrangement','C7-acquired-manual-cleanup','C7-acquired-manual-runtime','C7-acquired-manual-interrupted')}
function Assert-KmcChunk7AcquiredManualSelection($Request){
 $has=$null-ne$Request.PSObject.Properties['acquiredManual']
 if(-not(Test-KmcChunk7AcquiredManualScenario $Request.scenario)){if($has){throw 'Manual seed selection is forbidden under another family.'};return}
 if(-not$has){throw 'Manual seed requires its exact explicit selection.'};$s=$Request.acquiredManual
 Assert-KmcExactProperties $s @('case','sourceRunId','sourceArtifactSha256','sourceArchiveSha256','actorId','petId','baseGameOnly','expectedNativeLoadCount','expectedNativeWriteCount') 'Manual seed selection'
 Assert-KmcBootstrapInventoryString $s.case 'preserve-acquired-field'
 $ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);[void]$ids.Add($Request.runId)
 foreach($key in @('sourceRunId')){if($s.$key-isnot[string]-or$s.$key-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'-or-not$ids.Add($s.$key)){throw 'Manual seed requires distinct completed bootstrap and cold-inventory runs.'}}
 foreach($key in @('sourceArtifactSha256','sourceArchiveSha256')){if($s.$key-isnot[string]-or$s.$key-cnotmatch'^[a-f0-9]{64}$'){throw 'Manual seed source pins must be exact SHA256 strings.'}}
 foreach($key in @('actorId','petId')){Assert-KmcOrdinaryGuid $s.$key};if($s.actorId-ceq$s.petId){throw 'Acquired owner and pet must have distinct native identities.'}
 Assert-KmcChunk7Boolean $s.baseGameOnly $true
 foreach($key in @('expectedNativeLoadCount','expectedNativeWriteCount')){Assert-KmcOrdinaryInteger $s.$key 1 1}
}
function Assert-KmcChunk7AcquiredManualDescriptor($Request){
 Assert-KmcChunk7AcquiredManualSelection $Request
 if(-not(Test-KmcChunk7AcquiredManualScenario $Request.scenario)){throw 'Manual seed descriptor belongs only to its closed family.'}
 $d=$Request.persistenceLoad
 Assert-KmcExactProperties $d @('internalName','fileName','sha256','length','lastWriteTimeUtcTicks','gameId','gameName','area') 'Manual seed immutable descriptor'
 Assert-KmcBootstrapInventoryString $d.fileName 'Auto_1.zks';Assert-KmcBootstrapInventoryString $d.sha256 $Request.acquiredManual.sourceArchiveSha256
 foreach($key in @('internalName','gameName')){if($d.$key-isnot[string]-or[string]::IsNullOrWhiteSpace($d.$key)-or$d.$key.Length-gt256-or$d.$key-match'[\x00-\x1f\x7f]'){throw 'Manual seed source native name is malformed.'}}
 Assert-KmcOrdinaryGuid $d.gameId
 if($d.gameId-ceq$Request.fixture.working.gameId-or$d.sha256-ceq$Request.fixture.working.sha256-or$d.sha256-ceq$Request.fixture.baseline.sha256-or$d.area-isnot[string]-or$d.area-cnotmatch'^[a-f0-9]{32}$'){throw 'Manual seed source must be the distinct native-minted campaign.'}
 Assert-KmcOrdinaryInteger $d.length 1 268435456;Assert-KmcOrdinaryInteger $d.lastWriteTimeUtcTicks 1
}
function Assert-KmcChunk7AcquiredManualAuthority($Row,[string]$Root,[long]$Generation,[long]$Loads,[long]$Writes){
 Assert-KmcBootstrapInventoryString $Row.root $Root;Assert-KmcOrdinaryInteger $Row.generation 1 ([int]::MaxValue)
 if($Row.generation-ne$Generation){throw 'Manual seed authority generation changed.'}
 foreach($key in @('maximumNativeLoads','maximumNativeWrites')){Assert-KmcOrdinaryInteger $Row.$key 1 1}
 foreach($key in @('admittedNativeLoads','authorizedLoads')){Assert-KmcOrdinaryInteger $Row.$key $Loads $Loads}
 foreach($key in @('admittedNativeWrites','authorizedWrites')){Assert-KmcOrdinaryInteger $Row.$key $Writes $Writes}
 foreach($key in @('unauthorizedLoads','unauthorizedWrites','fatalViolations','suppressedWrites')){Assert-KmcOrdinaryInteger $Row.$key 0 0}
 Assert-KmcOrdinaryInteger $Row.reservationCount 1 1;Assert-KmcOrdinaryInteger $Row.manualNumber 1 ([int]::MaxValue)
 Assert-KmcBootstrapInventoryString $Row.outputLeaf ('Manual_'+([int]$Row.manualNumber).ToString([Globalization.CultureInfo]::InvariantCulture)+'_KMC_C7_ACQUIRED_FIELD.zks')
}
function Assert-KmcAcquiredManualObjectIdentity($Value){Assert-KmcOrdinaryInteger $Value ([int]::MinValue) ([int]::MaxValue);if($Value-eq0){throw 'A retained native object requires a nonzero signed Int32 label.'}}
function Assert-KmcAcquiredManualWorld($Row,$Request,$Labels,$SourceWorld,[long]$MinimumFrame,[long]$Writes){
 Assert-KmcOrdinaryInteger $Row.frame $MinimumFrame ([int]::MaxValue)
 $objects=[Collections.Generic.HashSet[int]]::new()
 foreach($key in @('game','saveManager','player','state','areaState','mainState')){Assert-KmcAcquiredManualObjectIdentity $Row.$key;if(-not$Labels.Contains([int]$Row.$key)-or-not$objects.Add([int]$Row.$key)){throw 'Cold world label was not retained or aliased another native role.'}}
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
  foreach($key in @('object','descriptor','view')){Assert-KmcAcquiredManualObjectIdentity $actor.$key;if(-not$Labels.Contains([int]$actor.$key)-or-not$objects.Add([int]$actor.$key)){throw 'Cold native object label is foreign or aliased.'}}
  foreach($key in @('isInGame','isInState','commandsEmpty')){Assert-KmcChunk7Boolean $actor.$key $true}
 }
 if(-not$ids.Contains($Row.mainActorId)){throw 'Cold main actor is absent from its exact party.'}
}

function Assert-KmcChunk7AcquiredManualEvidence($Request,$Artifact,$SourceArtifact,[string]$Status='PASS'){
 Assert-KmcChunk7AcquiredManualDescriptor $Request;Assert-KmcChunk7ArtifactEnvelope $Request $Artifact 'chunk7-acquired-manual-evidence'
 if($Status-ceq'PASS'-and$Artifact.status-cne'PASS'){throw 'PASS cold inventory requires its PASS native artifact.'}
 if($Artifact.status-cne'PASS'){return}
 $o=$Artifact.observations;$s=$Request.acquiredManual;$source=$SourceArtifact
 foreach($key in @('commit','dllSha256','dllMvid')){Assert-KmcBootstrapInventoryString $source.$key $Request.$key}
 Assert-KmcBootstrapInventoryString $source.runId $s.sourceRunId;Assert-KmcBootstrapInventoryString $source.scenario 'chunk7-manual-derived-acquisition';Assert-KmcBootstrapInventoryString $source.status 'PASS'
 if($source.errors-isnot[Array]-or$source.errors.Count-ne0){throw 'Cold inventory source was not an error-free bootstrap.'}
 foreach($key in @('gameId','gameName','area')){Assert-KmcBootstrapInventoryString $source.observations.nativeWrite.$key $Request.persistenceLoad.$key}
 Assert-KmcBootstrapInventoryString $source.observations.nativeWrite.sha256 $s.sourceArchiveSha256
 if((ConvertTo-KmcChunk7Canonical $o.sourceNativeWorld)-cne(ConvertTo-KmcChunk7Canonical $source.observations.loadedWorld)){throw 'Cold inventory changed its qualified source native world.'}
 if((ConvertTo-KmcChunk7Canonical $o.selection)-cne(ConvertTo-KmcChunk7Canonical $s)){throw 'Cold inventory selection changed.'}
 Assert-KmcOrdinaryInteger $o.processId 1 ([int]::MaxValue);Assert-KmcOrdinaryInteger $source.observations.processId 1 ([int]::MaxValue)
 if($o.processId-eq$source.observations.processId){throw 'Cold inventory reused the native bootstrap process.'}
 foreach($key in @('resourceWrites','xpWrites','moneyWrites')){Assert-KmcOrdinaryInteger $o.$key 0 0}
 foreach($key in @('profileAdmissionChanged','ordinaryAcquisitionProven','sixPairFixtureQualified')){Assert-KmcChunk7Boolean $o.$key $false}
 foreach($key in @('runId','artifactSha256','archiveSha256')){$selectionKey=if($key-ceq'runId'){'sourceRunId'}elseif($key-ceq'artifactSha256'){'sourceArtifactSha256'}else{'sourceArchiveSha256'};Assert-KmcBootstrapInventoryString $o.source.$key $s.$selectionKey}
 Assert-KmcBootstrapInventoryString $o.source.archiveLeaf 'Auto_1.zks';Assert-KmcChunk7Boolean $o.source.sameCandidate $true;Assert-KmcChunk7Boolean $o.source.diagnosticArchiveContentRead $false
 if((ConvertTo-KmcChunk7Canonical $o.sourceAcquisition)-cne(ConvertTo-KmcChunk7Canonical $source.observations)){throw 'Derivative substituted its complete source acquisition observations.'}
 $paid=$source.observations.selection.case-ceq'paid-small-ranger-horse';if($source.observations.selection.case-isnot[string]-or$source.observations.selection.case-cnotin@('existing-ranger-horse','paid-small-ranger-horse')){throw 'Acquired Manual source case differs.'}
 Assert-KmcBootstrapInventoryString $source.observations.field.actorId $s.actorId;Assert-KmcBootstrapInventoryString $source.observations.field.petId $s.petId
 Assert-KmcChunk7Boolean $source.observations.nativeWrite.saveBeforeFieldSelection $paid;Assert-KmcChunk7Boolean $o.sourceSavedBeforeSelector $paid
 $loadedExpected=[pscustomobject]@{mainActorId=$o.sourceNativeWorld.mainActorId;party=@($o.sourceNativeWorld.party)}
 if(-not$paid){$loadedExpected.party+=,[pscustomobject]@{id=$s.petId;blueprint=$source.observations.field.horseBlueprint}}
 $fieldExpected=[pscustomobject]@{mainActorId=$o.sourceNativeWorld.mainActorId;party=@($loadedExpected.party)}
 if($paid){$actorProfile=@($source.observations.field.profileInventory.party|Where-Object id -CEQ $s.actorId);if($actorProfile.Count-ne1){throw 'Qualified source acquired actor profile is missing.'};$fieldExpected.party+=,[pscustomobject]@{id=$s.actorId;blueprint=$actorProfile[0].blueprintGuid};$fieldExpected.party+=,[pscustomobject]@{id=$s.petId;blueprint=$source.observations.field.horseBlueprint}}
 $b=$o.beforeLoad;$w=$o.loadedWorld;$d=$o.fieldWorld;$v=$o.savedWorld;$f=$o.settledWorld;$load=$o.nativeLoad;$registry=$load.identities
 Assert-KmcBootstrapInventoryString $load.contract 'bootstrap-native-postload-v1';foreach($key in @('installed','installationAttempted','complete')){Assert-KmcChunk7Boolean $load.$key $true}
 foreach($key in @('entries','exits')){Assert-KmcOrdinaryInteger $load.$key 1 1}
 if($load.errors-isnot[Array]-or$load.errors.Count-ne0-or$load.events-isnot[Array]-or$load.events.Count-ne2-or$load.hooks-isnot[Array]-or$load.hooks.Count-ne1){throw 'Cold native PostLoad observer is incomplete.'}
 $h=$load.hooks[0];foreach($pair in @(@('declaringType','Kingmaker.Player'),@('method','PostLoad'),@('token','06000DC7'),@('moduleMvid','07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'))){Assert-KmcBootstrapInventoryString $h.($pair[0]) $pair[1]}
 Assert-KmcBootstrapInventoryString $registry.contract 'retained-native-object-identity-v1';Assert-KmcOrdinaryInteger $registry.capacity 128 128;Assert-KmcOrdinaryInteger $registry.faults 0 0;Assert-KmcChunk7Boolean $registry.released $false
 if($registry.ids-isnot[Array]-or$registry.ids.Count-lt1-or$registry.ids.Count-gt128){throw 'Cold native registry is missing or unbounded.'}
 Assert-KmcOrdinaryInteger $registry.retainedCount $registry.ids.Count $registry.ids.Count;$labels=[Collections.Generic.HashSet[int]]::new()
 foreach($id in $registry.ids){Assert-KmcAcquiredManualObjectIdentity $id;if(-not$labels.Add($id)){throw 'Cold native registry repeats a label.'}}
 $root=Join-Path (Join-Path (Join-Path (Split-Path (Split-Path $Request.evidenceRoot -Parent) -Parent) 'runtime-staging') ('persistence-'+$Request.runId)) 'Saved Games'
 Assert-KmcOrdinaryInteger $b.frame 0 ([int]::MaxValue)
 $beforeRoles=[Collections.Generic.HashSet[int]]::new()
 foreach($key in @('game','saveManager','menuPlayer')){Assert-KmcAcquiredManualObjectIdentity $b.$key;if(-not$labels.Contains([int]$b.$key)-or-not$beforeRoles.Add([int]$b.$key)){throw 'Before-load native owner was not retained or aliased another role.'}}
 foreach($key in @('loadedAreaPresent','nativeLoading')){Assert-KmcChunk7Boolean $b.$key $false}
 Assert-KmcChunk7AcquiredManualAuthority $b.authority $root $b.authority.generation 0 0
 Assert-KmcAcquiredManualWorld $w $Request $labels $loadedExpected $b.frame 0
 Assert-KmcAcquiredManualWorld $d $Request $labels $fieldExpected $w.frame 0
 Assert-KmcChunk7AcquiredManualAuthority $d.authority $root $b.authority.generation 1 0
 Assert-KmcAcquiredManualWorld $v $Request $labels $fieldExpected $d.frame 1
  Assert-KmcAcquiredManualWorld $f $Request $labels $fieldExpected $v.frame 1
 Assert-KmcChunk7AcquiredManualAuthority $w.authority $root $b.authority.generation 1 0
 foreach($row in @($v,$f)){Assert-KmcChunk7AcquiredManualAuthority $row.authority $root $b.authority.generation 1 1}
 foreach($row in @($w,$d,$v,$f)){if($row.game-ne$b.game-or$row.saveManager-ne$b.saveManager){throw 'Manual seed replaced the exact Game or SaveManager.'}}
 foreach($key in @('game','saveManager','player','state','areaState','mainState','mainActorId')){if((ConvertTo-KmcChunk7Canonical $v.$key)-cne(ConvertTo-KmcChunk7Canonical $w.$key)){throw 'Manual write replaced its exact native world or actors.'}}
 foreach($key in @('game','saveManager','player','state','areaState','mainState','mainActorId','party')){if((ConvertTo-KmcChunk7Canonical $f.$key)-cne(ConvertTo-KmcChunk7Canonical $v.$key)){throw 'Final cold world or actors changed before cleanup.'}}
 $prior=$b.frame
 for($i=0;$i-lt2;$i++){$e=$load.events[$i];$kind=if($i-eq0){'native-player-postload-before'}else{'native-player-postload-after'};Assert-KmcBootstrapInventoryString $e.kind $kind;Assert-KmcOrdinaryInteger $e.frame $prior $w.frame;$prior=$e.frame
  foreach($key in @('game','saveManager','player')){Assert-KmcAcquiredManualObjectIdentity $e.$key;if($e.$key-ne$w.$key){throw 'Native PostLoad callback owner was substituted.'}}
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
 if($inventory.party-isnot[Array]-or$inventory.party.Count-ne$v.party.Count){throw 'Read-only native profile inventory lost its exact cold actors.'}
 foreach($actor in $v.party){$row=@($inventory.party|Where-Object id -CEQ $actor.id);if($row.Count-ne1){throw 'Profile inventory actor changed.'};Assert-KmcBootstrapInventoryString $row[0].id $actor.id;Assert-KmcBootstrapInventoryString $row[0].blueprintGuid $actor.blueprint
  $l=$row[0].nativeLevelUpReadiness;Assert-KmcOrdinaryInteger $l.characterLevel 0 20;Assert-KmcOrdinaryInteger $l.experience 0 ([int]::MaxValue);if($null-ne$l.nextLevelExperience){Assert-KmcOrdinaryInteger $l.nextLevelExperience 0 ([int]::MaxValue)}
  foreach($key in @('canLevelUp','isDead')){if($l.$key-isnot[bool]){throw 'Native readiness flag invalid.'}};foreach($key in @('partyInCombat','controllerCreated','completeChoicesObserved','ordinaryAcquisitionProven')){Assert-KmcChunk7Boolean $l.$key $false}
 }
 $r=$inventory.ordinaryAcquisitionPrerequisites;Assert-KmcChunk7Boolean $r.observationOnly $true;foreach($key in @('purchaseAuthorized','sixPairFixtureQualified','defaultHireMatchesPlayerExperienceExactly')){Assert-KmcChunk7Boolean $r.$key $false}
 Assert-KmcOrdinaryInteger $r.nativeMoney 0 ([long]::MaxValue);Assert-KmcOrdinaryInteger $r.nativeHireCostNow 0 ([int]::MaxValue)
 foreach($key in @('mainCharacterExperience','nativeDefaultHireExperienceNow')){Assert-KmcOrdinaryInteger $r.$key 0 ([int]::MaxValue)}
 foreach($key in @('nativePartyLevel','mainCharacterLevel')){Assert-KmcOrdinaryInteger $r.$key 0 20};Assert-KmcOrdinaryInteger $r.nativeFieldCharacterLimit 6 6;Assert-KmcBootstrapInventoryString $r.mainCharacterId $w.mainActorId;Assert-KmcChunk7Boolean $r.nativeAffordableNow ($r.nativeMoney-ge$r.nativeHireCostNow)
 if($r.nativePartyCharacterIds-isnot[Array]-or$r.nativePartyCharacterIds.Count-lt1-or$r.nativePartyCharacterIds.Count-gt6){throw 'Native field character capacity observation invalid.'}
 $characters=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);foreach($id in $r.nativePartyCharacterIds){Assert-KmcOrdinaryGuid $id;if(-not$characters.Add($id)-or$id-cnotin@($v.party.id)){throw 'Native character inventory is duplicate or foreign.'}}
 $main=@($inventory.party|Where-Object id -CEQ $w.mainActorId)[0].nativeLevelUpReadiness
 if($r.mainCharacterLevel-ne$main.characterLevel-or$r.mainCharacterExperience-ne$main.experience){throw 'Native main readiness observations disagree.'}
 Assert-KmcAcquiredFixtureReadiness $o.fixtureReadiness $Request $d $v $inventory $root
 foreach($key in @('custodySettled','nativeWorkerSettled','observersReleased','worldAdmitted','sourcePinned','sourceUnchanged')){Assert-KmcChunk7Boolean $o.cleanup.$key $true}
 Assert-KmcChunk7AcquiredManualAuthority $o.cleanup.authority $root $b.authority.generation 1 1
  Assert-KmcAcquiredManualField $Request $o $source.observations $labels $root $paid
  Assert-KmcAcquiredManualNativeSave $Request $o $labels $root
}

function Assert-KmcAcquiredManualArrangement($Row,$World,$Inventory){
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
function Assert-KmcAcquiredManualNativeSave($Request,$O,$Labels,[string]$Root){
 $leaf='Manual_'+([int]$O.reservation.number).ToString([Globalization.CultureInfo]::InvariantCulture)+'_KMC_C7_ACQUIRED_FIELD.zks';$path=Join-Path $Root $leaf;$w=$O.loadedWorld;$f=$O.savedWorld;$n=$O.nativeNumbering;$r=$O.saveRequested;$p=$O.nativeSave;$write=$O.nativeWrite
 foreach($pair in @(@('method','FindUnusedSaveNumber'),@('token','06008024'),@('moduleMvid','07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'),@('nativeSaveType','Manual'),@('fileName',$leaf))){Assert-KmcBootstrapInventoryString $n.($pair[0]) $pair[1]}
 Assert-KmcOrdinaryInteger $n.number $O.reservation.number $O.reservation.number;Assert-KmcOrdinaryInteger $n.frame $w.frame $f.frame
 Assert-KmcChunk7Boolean $n.outputInitiallyAbsent $true;Assert-KmcChunk7Boolean $n.slotProjectionEnabled $false
 if((ConvertTo-KmcChunk7Canonical $n.sourceDescriptors)-cne(ConvertTo-KmcChunk7Canonical $O.fixtureReadiness.isolatedSaveDescriptors.descriptors)){throw 'Native numbering did not use the exact sole Auto descriptor inventory.'}
 Assert-KmcOrdinaryInteger $r.frame $n.frame $f.frame;Assert-KmcAcquiredManualObjectIdentity $r.descriptor
 Assert-KmcBootstrapInventoryString $r.name 'KMC_C7_ACQUIRED_FIELD';Assert-KmcBootstrapInventoryString $r.type 'Manual'
 foreach($key in @('nativeSaveAllowed','newSlot')){Assert-KmcChunk7Boolean $r.$key $true};Assert-KmcOrdinaryInteger $r.snapshotCount 0 0
 Assert-KmcChunk7AcquiredManualAuthority $r.authority $Root $O.beforeLoad.authority.generation 1 0
 Assert-KmcBootstrapInventoryString $p.contract 'manual-seed-native-save-v1'
 foreach($key in @('installed','installationAttempted','complete')){Assert-KmcChunk7Boolean $p.$key $true}
 foreach($key in @('requests','prepares','starts','stages','completions','callbacks')){Assert-KmcOrdinaryInteger $p.$key 1 1}
 if($p.errors-isnot[Array]-or$p.errors.Count-ne0-or$p.events-isnot[Array]-or$p.events.Count-ne6-or$p.hooks-isnot[Array]-or$p.hooks.Count-ne3){throw 'Manual seed native observer is incomplete or unbounded.'}
 $names=@('SaveRoutine','PrepareSave','RecordCompletedSave');$tokens=@('06008029','06008025')
 for($i=0;$i-lt3;$i++){$h=$p.hooks[$i];Assert-KmcBootstrapInventoryString $h.method $names[$i]
  if($i-lt2){Assert-KmcBootstrapInventoryString $h.type 'Kingmaker.EntitySystem.Persistence.SaveManager';Assert-KmcBootstrapInventoryString $h.token $tokens[$i];Assert-KmcBootstrapInventoryString $h.moduleMvid '07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'}else{Assert-KmcBootstrapInventoryString $h.type 'KingmakerMountedCombat.Integration.MountedPersistenceService';Assert-KmcBootstrapInventoryString $h.moduleMvid $Request.dllMvid;if($h.token-isnot[string]-or$h.token-cnotmatch'^06[0-9A-F]{6}$'){throw 'Manual completion hook token invalid.'}}
 }
 $roles=[Collections.Generic.HashSet[int]]::new();[void]$roles.Add([int]$O.beforeLoad.menuPlayer);foreach($key in @('game','saveManager','player','state','areaState','mainState')){[void]$roles.Add([int]$w.$key)}
 foreach($actor in $O.savedWorld.party){foreach($key in @('object','descriptor','view')){[void]$roles.Add([int]$actor.$key)}}
 foreach($value in @($r.descriptor,$write.writtenDescriptor,$p.events[4].scope,$p.events[4].iterator,$p.events[4].worker)){
  Assert-KmcAcquiredManualObjectIdentity $value;if(-not$Labels.Contains([int]$value)-or-not$roles.Add([int]$value)){throw 'Native save object identity is unretained or aliased.'}
 }
 $kinds=@('native-save-request','native-save-prepared','snapshot-starting','snapshot-staged','native-worker-completed','native-save-callback');$prior=$r.frame
 for($i=0;$i-lt6;$i++){$e=$p.events[$i];Assert-KmcBootstrapInventoryString $e.kind $kinds[$i];Assert-KmcOrdinaryInteger $e.sequence ($i+1) ($i+1);Assert-KmcOrdinaryInteger $e.frame $prior $f.frame;$prior=$e.frame
  foreach($key in @('game','saveManager','player')){Assert-KmcOrdinaryInteger $e.$key $w.$key $w.$key}
  Assert-KmcOrdinaryInteger $e.requested $r.descriptor $r.descriptor
  $descriptor=if($i-eq0){$r.descriptor}else{$write.writtenDescriptor};Assert-KmcOrdinaryInteger $e.descriptor $descriptor $descriptor
  Assert-KmcBootstrapInventoryString $e.internalName 'KMC_C7_ACQUIRED_FIELD';Assert-KmcBootstrapInventoryString $e.type 'Manual';Assert-KmcBootstrapInventoryString $e.gameName $Request.persistenceLoad.gameName;Assert-KmcBootstrapInventoryString $e.area $Request.persistenceLoad.area
  if($i-eq0){foreach($key in @('fileName','gameId','path')){if($null-ne$e.$key){throw 'Fresh requested descriptor was already prepared or saved.'}}}else{Assert-KmcBootstrapInventoryString $e.fileName $leaf;Assert-KmcBootstrapInventoryString $e.path $path;Assert-KmcBootstrapInventoryString $e.gameId $Request.persistenceLoad.gameId}
  $snapshots=if($i-lt3){0}else{1};$completed=if($i-lt5){0}else{1};Assert-KmcOrdinaryInteger $e.snapshotCount $snapshots $snapshots;Assert-KmcOrdinaryInteger $e.completedSaveCount $completed $completed
 }
 $worker=$p.events[4];Assert-KmcBootstrapInventoryString $worker.iteratorType 'Kingmaker.EntitySystem.Persistence.SaveManager+<SaveRoutine>d__46';Assert-KmcBootstrapInventoryString $worker.workerStatus 'RanToCompletion';Assert-KmcOrdinaryInteger $worker.workerId 1 ([int]::MaxValue)
 foreach($key in @('nativeWorkerMatches','workerCompleted')){Assert-KmcChunk7Boolean $worker.$key $true};foreach($key in @('workerFaulted','workerCanceled')){Assert-KmcChunk7Boolean $worker.$key $false}
 foreach($pair in @(@('path',$path),@('fileName',$leaf),@('internalName','KMC_C7_ACQUIRED_FIELD'),@('type','Manual'),@('operationState','None'))){Assert-KmcBootstrapInventoryString $write.($pair[0]) $pair[1]}
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
  Assert-KmcAcquiredManualObjectIdentity $d.descriptor;if(-not$Labels.Contains([int]$d.descriptor)){throw 'Settled descriptor label was not retained.'}
  if($source){if(-not$roles.Add([int]$d.descriptor)){throw 'Source descriptor aliases a native owner.'};Assert-KmcBootstrapInventoryString $d.internalName $Request.persistenceLoad.internalName;Assert-KmcBootstrapInventoryString $d.type 'Auto'}else{Assert-KmcOrdinaryInteger $d.descriptor $write.writtenDescriptor $write.writtenDescriptor;Assert-KmcBootstrapInventoryString $d.internalName 'KMC_C7_ACQUIRED_FIELD';Assert-KmcBootstrapInventoryString $d.type 'Manual'}
  foreach($key in @('gameId','gameName','area')){Assert-KmcBootstrapInventoryString $d.$key $Request.persistenceLoad.$key};Assert-KmcBootstrapInventoryString $d.operationState 'None'
  foreach($key in @('isActuallySaved','hasFileOnDisk')){Assert-KmcChunk7Boolean $d.$key $true};Assert-KmcChunk7Boolean $d.autoLevelup $source
 }
 if(-not$leaves.Contains('Auto_1.zks')-or-not$leaves.Contains($leaf)){throw 'Native settled two-entry inventory differs.'}
 foreach($row in @($O.beforeSaveArrangement,$O.afterSaveArrangement)){Assert-KmcAcquiredManualArrangement $row $f $O.nativeProfileInventory;Assert-KmcOrdinaryInteger $row.experienceRatePercent $O.fixtureReadiness.experienceRatePercent $O.fixtureReadiness.experienceRatePercent}
 if((ConvertTo-KmcChunk7Canonical $O.beforeSaveArrangement)-cne(ConvertTo-KmcChunk7Canonical $O.afterSaveArrangement)){throw 'Save-only derivative changed native money, XP, class ranks or actor identity.'}
}
function Assert-KmcChunk7AcquiredManualArtifact($Request,$Manifest,[string]$Status='PASS'){
 $records=@($Manifest.artifacts|Where-Object{$_.kind-ceq'chunk7-acquired-manual-evidence'-or$_.relativePath-ceq'chunk7-acquired-manual-evidence.json'})
 if(-not(Test-KmcChunk7AcquiredManualScenario $Request.scenario)){if($records.Count){throw 'Foreign request supplied acquired Manual evidence.'};return}
 if($Status-ceq'FAIL'-and$records.Count-eq0){return}
 $artifact=Read-KmcChunk7ManifestArtifact $Request $Manifest 'chunk7-acquired-manual-evidence.json' 'chunk7-acquired-manual-evidence';$source=$null
 if($artifact.status-ceq'PASS'){
  $path=Join-Path (Join-Path (Split-Path $Request.evidenceRoot -Parent) $Request.acquiredManual.sourceRunId) 'chunk7-manual-derived-evidence.json'
  Assert-KmcNotReparsePoint $path 'qualified acquisition lineage';Assert-KmcNotHardLink $path 'qualified acquisition lineage';$file=Get-Item -LiteralPath $path
  if($file.Length-le0-or$file.Length-gt16MB-or(Get-KmcSha256 $path)-cne$Request.acquiredManual.sourceArtifactSha256){throw 'Qualified acquisition lineage is absent or changed.'}
  Assert-KmcManifestJsonMembersUnique $path 'qualified acquisition lineage';$source=Read-KmcJson $path
  if((Get-KmcSha256 $path)-cne$Request.acquiredManual.sourceArtifactSha256){throw 'Qualified acquisition lineage changed while reading.'}
 }
 Assert-KmcChunk7AcquiredManualEvidence $Request $artifact $source $Status
}

function Assert-KmcAcquiredFixtureReadiness($Row,$Request,$World,$Settled,$Inventory,[string]$Root){
 Assert-KmcExactProperties $Row @('schemaVersion','contract','captureStatus','frame','gameplayAssemblyMvid','observationOnly','saveSettings','experienceRatePercent','mainProgression','staticPartyFacts','isolatedSaveDescriptors') 'cold fixture readiness'
 Assert-KmcOrdinaryInteger $Row.schemaVersion 1 1;Assert-KmcOrdinaryInteger $Row.frame $World.frame $Settled.frame
 Assert-KmcBootstrapInventoryString $Row.contract 'installed-native-fixture-readiness-v1';Assert-KmcBootstrapInventoryString $Row.captureStatus 'PASS';Assert-KmcBootstrapInventoryString $Row.gameplayAssemblyMvid '07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';Assert-KmcChunk7Boolean $Row.observationOnly $true
 Assert-KmcOrdinaryInteger $Row.experienceRatePercent ([int]::MinValue) ([int]::MaxValue)
 $s=$Row.saveSettings;Assert-KmcExactProperties $s @('observationKind','projectionEnabled','autosaveSlotsNativeType','autosaveCapacityInferred','sliderFallbackCallbackPresent','settingsWritten','onlyOneSave','autosaveEnabled','autosaveSlots') 'observed native save settings'
 Assert-KmcBootstrapInventoryString $s.observationKind 'native-effective-getters';Assert-KmcBootstrapInventoryString $s.autosaveSlotsNativeType 'System.Single'
 foreach($key in @('projectionEnabled','autosaveCapacityInferred','sliderFallbackCallbackPresent','settingsWritten')){Assert-KmcChunk7Boolean $s.$key $false}
 Assert-KmcChunk7Boolean $s.onlyOneSave $false;if($s.autosaveEnabled-isnot[bool]){throw 'Native save setting must remain a boolean observation.'}
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
 $p=$Row.staticPartyFacts;Assert-KmcExactProperties $p @('observationScope','selectorOpened','liveSelectorRequiredAuthorityAvailable','membershipOrRemovalQualified','characters') 'static native party facts';Assert-KmcBootstrapInventoryString $p.observationScope 'static-getters-only'
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
 Assert-KmcBootstrapInventoryString $v.type 'Auto';Assert-KmcBootstrapInventoryString $v.operationState 'None';Assert-KmcChunk7Boolean $v.hasFileOnDisk $true;Assert-KmcChunk7Boolean $v.isAutoLevelupSave $true
 # Installed getters06007FE1/06007FE2 both return nonempty FolderName; a disk-backed settled descriptor has both true.
 Assert-KmcChunk7Boolean $v.isActuallySaved $true;Assert-KmcOrdinaryInteger $v.systemSaveTimeTicks 0 ([DateTime]::MaxValue.Ticks)
 if($v.systemSaveTimeKind-isnot[string]-or$v.systemSaveTimeKind-cnotin@('Unspecified','Utc','Local')){throw 'Native save timestamp kind is malformed.'}
}

function Assert-KmcAcquiredManualField($Request,$O,$Source,$Labels,[string]$Root,[bool]$Paid){
 $s=$Request.acquiredManual;$b=$O.beforeLoad;$w=$O.loadedWorld;$d=$O.fieldWorld;$v=$O.savedWorld;$r=$O.reservation
 Assert-KmcOrdinaryInteger $r.number 1 ([int]::MaxValue);Assert-KmcOrdinaryInteger $r.count 1 1;Assert-KmcOrdinaryInteger $r.frame $b.frame $b.frame
 foreach($pair in @(@('initialEntries',1),@('reservedEntries',2),@('initialWritableEntries',0),@('reservedWritableEntries',1))){Assert-KmcOrdinaryInteger $r.($pair[0]) $pair[1] $pair[1]}
 Assert-KmcChunk7Boolean $r.beforeLoad $true;Assert-KmcChunk7Boolean $r.slotProjectionEnabled $false
 foreach($pair in @(@('nativeMethod','FindUnusedSaveNumber'),@('nativeMethodToken','06008024'),@('nativeMethodMvid','07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'),@('outputLeaf',$b.authority.outputLeaf))){Assert-KmcBootstrapInventoryString $r.($pair[0]) $pair[1]}
 Assert-KmcOrdinaryInteger $r.number $b.authority.manualNumber $b.authority.manualNumber
 foreach($a in @($r.authority,$w.authority,$d.authority,$v.authority,$O.settledWorld.authority,$O.cleanup.authority,$O.saveRequested.authority)){
  Assert-KmcOrdinaryInteger $a.manualNumber $r.number $r.number;Assert-KmcBootstrapInventoryString $a.outputLeaf $r.outputLeaf
 }
 Assert-KmcChunk7AcquiredManualAuthority $r.authority $Root $b.authority.generation 0 0
 if($r.sourceDescriptors-isnot[Array]-or$r.sourceDescriptors.Count-ne1){throw 'Reservation must observe the sole actual native Auto descriptor.'}
 $sourceDescriptor=$r.sourceDescriptors[0];Assert-KmcAcquiredManualObjectIdentity $r.sourceDescriptor
 Assert-KmcOrdinaryInteger $sourceDescriptor.descriptor $r.sourceDescriptor $r.sourceDescriptor
 if(-not$Labels.Contains([int]$r.sourceDescriptor)){throw 'Reserved native source descriptor was not retained.'}
 foreach($key in @('fileName','internalName','gameId','gameName','area')){Assert-KmcBootstrapInventoryString $sourceDescriptor.$key $Request.persistenceLoad.$key}
 foreach($key in @('isActuallySaved','hasFileOnDisk','autoLevelup')){Assert-KmcChunk7Boolean $sourceDescriptor.$key $true}
 Assert-KmcBootstrapInventoryString $sourceDescriptor.type 'Auto';Assert-KmcBootstrapInventoryString $sourceDescriptor.operationState 'None'
 $settledSource=@($O.settledDescriptors|Where-Object fileName -CEQ 'Auto_1.zks');if($settledSource.Count-ne1-or(ConvertTo-KmcChunk7Canonical $settledSource[0])-cne(ConvertTo-KmcChunk7Canonical $sourceDescriptor)){throw 'Native source descriptor changed across cold load or fresh save.'}
 foreach($actor in $w.party){$now=@($d.party|Where-Object id -CEQ $actor.id);if($now.Count-ne1-or(ConvertTo-KmcChunk7Canonical $now[0])-cne(ConvertTo-KmcChunk7Canonical $actor)){throw 'Owned field selector changed an original actor/view.'}}
 foreach($key in @('game','saveManager','player','state','areaState','mainState','mainActorId','party')){if((ConvertTo-KmcChunk7Canonical $v.$key)-cne(ConvertTo-KmcChunk7Canonical $d.$key)){throw 'Manual write changed the exact field world.'}}
 $ownerRows=@($O.loadedOwner,$O.fieldOwner,$O.savedOwner);$worldRows=@($w,$d,$v);$prior=$w.frame
 for($i=0;$i-lt3;$i++){
  $owner=$ownerRows[$i];$world=$worldRows[$i];Assert-KmcOrdinaryInteger $owner.frame $prior $world.frame;$prior=$owner.frame
  Assert-KmcBootstrapInventoryString $owner.actorId $s.actorId;Assert-KmcBootstrapInventoryString $owner.petId $s.petId
  $profile=@($Source.field.profileInventory.party|Where-Object id -CEQ $s.actorId);if($profile.Count-ne1){throw 'Acquired owner source profile is ambiguous.'}
  Assert-KmcBootstrapInventoryString $owner.actorBlueprint $profile[0].blueprintGuid;Assert-KmcBootstrapInventoryString $owner.petBlueprint $Source.field.horseBlueprint
  Assert-KmcBootstrapInventoryString $owner.featureGuid $Source.field.featureBlueprint;Assert-KmcBootstrapInventoryString $owner.sourceGuid $Source.field.featureSourceGuid
  Assert-KmcOrdinaryInteger $Source.field.rank 1 20;Assert-KmcOrdinaryInteger $owner.rank $Source.field.rank $Source.field.rank
  Assert-KmcChunk7Boolean $owner.reciprocal $true
  $roles=[Collections.Generic.HashSet[int]]::new();foreach($key in @('game','saveManager','player','state','areaState','mainState')){[void]$roles.Add([int]$world.$key)}
  foreach($a in $world.party){foreach($key in @('object','descriptor','view')){[void]$roles.Add([int]$a.$key)}}
  foreach($spec in @(@('actor',$s.actorId),@('pet',$s.petId))){$name=$spec[0];$id=$spec[1];$native=@($world.party|Where-Object id -CEQ $id)
   foreach($key in @('Object','Descriptor','View')){$label=$owner.($name+$key)
    if($key-ceq'View'-and$i-eq0-and$Paid-and$label-eq0){Assert-KmcOrdinaryInteger $label 0 0;continue}
    Assert-KmcAcquiredManualObjectIdentity $label;if(-not$Labels.Contains([int]$label)){throw 'Acquired owner label was not retained.'}
    if($native.Count-eq1){$nativeKey=$key.ToLowerInvariant();Assert-KmcOrdinaryInteger $label $native[0].$nativeKey $native[0].$nativeKey}elseif(-not$roles.Add([int]$label)){throw 'Remote acquired owner aliases another native role.'}
   }
   foreach($key in @('CommandsEmpty')){Assert-KmcChunk7Boolean $owner.($name+$key) $true}
   foreach($key in @('InGame','InState','AgentReady')){if($i-eq0-and$Paid){if($owner.($name+$key)-isnot[bool]){throw 'Cold remote native state requires boolean observations.'}}else{Assert-KmcChunk7Boolean $owner.($name+$key) $true}}
  }
  foreach($key in @('featureObject','rankObject')){Assert-KmcAcquiredManualObjectIdentity $owner.$key;if(-not$Labels.Contains([int]$owner.$key)-or-not$roles.Add([int]$owner.$key)){throw 'Native sourced feature/rank aliases another retained object.'}}
  if($owner.partyCharacters-isnot[Array]-or$owner.remoteCompanions-isnot[Array]-or$owner.partyCharacters.Count-lt1-or$owner.partyCharacters.Count-gt6-or$owner.remoteCompanions.Count-gt24){throw 'Acquired owner native memberships are unbounded.'}
  foreach($array in @($owner.partyCharacters,$owner.remoteCompanions)){$seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);foreach($id in $array){Assert-KmcOrdinaryGuid $id;if(-not$seen.Add($id)){throw 'Native character membership repeats an actor.'}}}
  if($i-eq0-and$Paid){Assert-KmcChunk7Boolean $owner.actorInGame $false;if($s.actorId-cin$owner.partyCharacters-or@($owner.remoteCompanions|Where-Object{$_-ceq$s.actorId}).Count-ne1){throw 'Paid Auto must restore the acquired actor remotely before selector.'}}
  elseif(@($owner.partyCharacters|Where-Object{$_-ceq$s.actorId}).Count-ne1-or$s.actorId-cin$owner.remoteCompanions){throw 'Field owner is missing or still remote.'}
  Assert-KmcAcquiredManualArrangement $owner.progression $v $O.nativeProfileInventory
  if((ConvertTo-KmcChunk7Canonical $owner.progression)-cne(ConvertTo-KmcChunk7Canonical $O.loadedArrangement)){throw 'Native acquired actor/class/XP/money changed during field/save.'}
 }
 foreach($key in @('actorObject','actorDescriptor','petObject','petDescriptor','featureObject','rankObject')){if($O.loadedOwner.$key-ne$O.fieldOwner.$key-or$O.loadedOwner.$key-ne$O.savedOwner.$key){throw 'Acquired exact object changed across field/save.'}}
 foreach($key in @('actorView','petView')){if(($O.loadedOwner.$key-ne0-and$O.loadedOwner.$key-ne$O.fieldOwner.$key)-or$O.fieldOwner.$key-ne$O.savedOwner.$key){throw 'Acquired view was replaced rather than first materialized.'}}
 $original=@($O.loadedOwner.partyCharacters);$expected=@($original);if($Paid){$expected+=,$s.actorId}
 foreach($row in @($O.fieldOwner,$O.savedOwner)){if((ConvertTo-KmcChunk7Canonical @($row.partyCharacters|Sort-Object -CaseSensitive))-cne(ConvertTo-KmcChunk7Canonical @($expected|Sort-Object -CaseSensitive))){throw 'Native selector changed unrelated field characters.'}}
 $selector=$O.selector;Assert-KmcChunk7Boolean $selector.required $Paid;Assert-KmcChunk7Boolean $selector.manualPartyMutation $false
 if($Paid){Assert-KmcBootstrapInventoryString $selector.action 'native-ShowPartySelection';Assert-KmcBootstrapInventoryString $selector.actorId $s.actorId;Assert-KmcOrdinaryInteger $selector.nativeCap 6 6
  foreach($key in @('openCount','switchCount','goCount')){Assert-KmcOrdinaryInteger $selector.$key 1 1};foreach($key in @('openActiveSelf','readyActiveInHierarchy','closedExactInstance')){Assert-KmcChunk7Boolean $selector.$key $true};Assert-KmcChunk7Boolean $selector.closedActiveSelf $false
  if(-not(Test-KmcFiniteJsonNumber $selector.readyAlpha)-or$selector.readyAlpha-ne1){throw 'Native selector never reached its exact input-ready alpha.'}
  foreach($key in @('selectionRestored','ummInstanceRestored','ummOpenedRestored','pauseRestored')){Assert-KmcChunk7Boolean $O.presentation.$key $true};Assert-KmcChunk7Boolean $O.presentation.slotProjectionEnabled $false
  $body=$O.bodyObservation;$pins=$Source.selection;foreach($key in @('OriginalSizeOrdinal','EffectiveSizeOrdinal')){Assert-KmcOrdinaryInteger $body.$key 3 3}
  foreach($key in @('NativeRaceAndPresetIdentityExact','NativeSkeletonIsExactPreset','NativeRigAvailable')){Assert-KmcChunk7Boolean $body.$key $true}
  foreach($key in @('RiderPolymorphed','MountPolymorphed')){Assert-KmcChunk7Boolean $body.$key $false}
  foreach($pair in @(@('RaceGuid','smallRaceGuid'),@('PresetGuid','smallPresetGuid'),@('SkeletonName','skeletonName'),@('Gender','gender'))){Assert-KmcBootstrapInventoryString $body.($pair[0]) $pins.($pair[1])};Assert-KmcChunk7Boolean $body.LeftHanded $pins.leftHanded
 }else{Assert-KmcOrdinaryInteger $selector.openCount 0 0}
 foreach($row in @($O.loadedArrangement,$O.beforeSaveArrangement,$O.afterSaveArrangement)){Assert-KmcAcquiredManualArrangement $row $v $O.nativeProfileInventory;if((ConvertTo-KmcChunk7Canonical $row)-cne(ConvertTo-KmcChunk7Canonical $O.loadedArrangement)){throw 'Cumulative derivative manufactured native progression/funds.'}}
 $originalProgression=[pscustomobject]@{money=$O.loadedArrangement.money;experienceRatePercent=$O.loadedArrangement.experienceRatePercent;actors=@($O.loadedArrangement.actors|Where-Object{$_.actorId-cin@($Source.afterChoices.actors.actorId)})}
 if((ConvertTo-KmcChunk7Canonical $originalProgression)-cne(ConvertTo-KmcChunk7Canonical $Source.afterChoices)){throw 'Cold original progression differs from the completed acquisition.'}
 foreach($spec in @(@($s.actorId,'level','xp'),@($s.petId,'petLevel','petExperience'))){$row=@($O.loadedArrangement.actors|Where-Object actorId -CEQ $spec[0]);if($row.Count-ne1){throw 'Cold acquired progression actor missing.'};Assert-KmcOrdinaryInteger $row[0].characterLevel $Source.builderComplete.($spec[1]) $Source.builderComplete.($spec[1]);Assert-KmcOrdinaryInteger $row[0].experience $Source.builderComplete.($spec[2]) $Source.builderComplete.($spec[2])}
 foreach($row in @($O.resourcesBeforeField,$O.resourcesAfterField,$O.resourcesAfterSave)){if($row.actors-isnot[Array]-or$row.actors.Count-ne$v.party.Count){throw 'Native resource graph is missing or unbounded.'};$ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
  foreach($a in $row.actors){Assert-KmcOrdinaryGuid $a.actorId;if(-not$ids.Add($a.actorId)-or$a.actorId-cnotin@($v.party.id)){throw 'Native resource actor is foreign.'};foreach($key in @('move','standard','swift','initiative')){if(-not(Test-KmcFiniteJsonNumber $a.$key)){throw 'Native resource observation is non-finite or not numeric.'}};foreach($key in @('reactions','reactionAllowance')){Assert-KmcOrdinaryInteger $a.$key 0 ([int]::MaxValue)};if($a.prepared-isnot[bool]){throw 'Native prepared state must remain an observed boolean.'}}
  if((ConvertTo-KmcChunk7Canonical $row)-cne(ConvertTo-KmcChunk7Canonical $O.resourcesBeforeField)){throw 'Cumulative derivative changed action resources.'}
 }
 foreach($key in @('selectorClosed','presentationRestored')){Assert-KmcChunk7Boolean $O.cleanup.$key $true};Assert-KmcChunk7Boolean $O.cleanup.slotProjectionEnabled $false;Assert-KmcOrdinaryInteger $O.cleanup.manualPartyMutations 0 0
}
