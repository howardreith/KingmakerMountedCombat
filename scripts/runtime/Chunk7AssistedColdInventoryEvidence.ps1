Set-StrictMode -Version Latest
function Test-KmcChunk7AssistedColdInventoryScenario([string]$Scenario){$Scenario-ceq'chunk7-assisted-cold-inventory'}
function Get-KmcChunk7AssistedColdInventoryRows {@('chunk7-assisted-cold-inventory','C7-assisted-cold-source','C7-assisted-cold-native-load','C7-assisted-cold-readonly','C7-assisted-cold-cleanup','C7-assisted-cold-runtime','C7-assisted-cold-interrupted')}
function Assert-KmcChunk7AssistedColdInventorySelection($Request){
 if($null-eq$Request-or$Request.scenario-isnot[string]){throw 'Assisted cold request scenario must be a scalar string.'}
 $has=$null-ne$Request.PSObject.Properties['assistedColdInventory']
 if(-not(Test-KmcChunk7AssistedColdInventoryScenario $Request.scenario)){if($has){throw 'Assisted cold selection is forbidden under another scenario.'};return}
 if(-not$has){throw 'Assisted cold inventory requires its explicit closed selection.'}
 $s=$Request.assistedColdInventory
 Assert-KmcAssistedObjectValue $s @('case','sourceRunId','sourceArtifactSha256','sourceArchiveSha256','baseGameOnly','expectedNativeLoadCount','expectedNativeWriteCount') 'assisted cold selection'
 Assert-KmcBootstrapInventoryString $s.case 'assisted-manual-readonly-inventory'
 if($Request.runId-isnot[string]-or$Request.runId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'){throw 'Assisted cold consumer run identity is malformed.'}
 if($s.sourceRunId-isnot[string]-or$s.sourceRunId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'-or$s.sourceRunId-ceq$Request.runId){throw 'Assisted cold inventory requires a distinct completed source run.'}
 foreach($key in @('sourceArtifactSha256','sourceArchiveSha256')){if($s.$key-isnot[string]-or$s.$key-cnotmatch'^[a-f0-9]{64}$'){throw 'Assisted cold immutable source hashes are required.'}}
 Assert-KmcChunk7Boolean $s.baseGameOnly $true
 Assert-KmcOrdinaryInteger $s.expectedNativeLoadCount 1 1;Assert-KmcOrdinaryInteger $s.expectedNativeWriteCount 0 0
}
function Assert-KmcChunk7AssistedColdInventoryDescriptor($Request){
 Assert-KmcChunk7AssistedColdInventorySelection $Request
 if(-not(Test-KmcChunk7AssistedColdInventoryScenario $Request.scenario)){throw 'Assisted cold descriptor is exclusive to its exact family.'}
 $d=$Request.persistenceLoad
 Assert-KmcAssistedObjectValue $d @('internalName','fileName','sha256','length','lastWriteTimeUtcTicks','gameId','gameName','area') 'assisted cold descriptor'
 Assert-KmcBootstrapInventoryString $d.internalName 'KMC_C7_ASSISTED_FIXTURE'
 if($d.fileName-isnot[string]-or$d.fileName-cnotmatch'^Manual_([1-9][0-9]*)_KMC_C7_ASSISTED_FIXTURE\.zks$'){throw 'Assisted cold source must retain its observed native Manual leaf.'}
 $number=0;$leafMatch=[regex]::Match($d.fileName,'^Manual_([1-9][0-9]*)_KMC_C7_ASSISTED_FIXTURE\.zks$');if(-not[int]::TryParse($leafMatch.Groups[1].Value,[ref]$number)-or$number-le0-or$d.fileName-cne('Manual_'+$number+'_KMC_C7_ASSISTED_FIXTURE.zks')){throw 'Assisted cold Manual number is noncanonical or outside native Int32.'}
 Assert-KmcBootstrapInventoryString $d.sha256 $Request.assistedColdInventory.sourceArchiveSha256
 Assert-KmcOrdinaryGuid $d.gameId;Assert-KmcAssistedGuid32 $d.area
 if($d.gameName-isnot[string]-or[string]::IsNullOrWhiteSpace($d.gameName)-or$d.gameName.Length-gt256-or$d.gameName-match'[\x00-\x1f\x7f]'){throw 'Assisted cold native game name is malformed.'}
 foreach($name in @('baseline','working')){
  if($d.sha256-ceq$Request.fixture.$name.sha256-or$d.gameId-ceq$Request.fixture.$name.gameId){throw 'Assisted cold source cannot alias a protected fixture campaign or bytes.'}
 }
 Assert-KmcOrdinaryInteger $d.length 1 268435456;Assert-KmcOrdinaryInteger $d.lastWriteTimeUtcTicks 1 ([DateTime]::MaxValue.Ticks)
}

# Native readonly readiness uses the unchanged installed getters and class/static
# facts contract. Only this new family's sole observed Manual descriptor differs
# from the older bootstrap Auto tail; the legacy function remains unchanged.
function Assert-KmcAssistedColdFixtureReadiness($Row,$Request,$World,$Settled,$Inventory,[string]$Root){
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
 $p=$Row.staticPartyFacts;Assert-KmcExactProperties $p @('selectorOpened','liveSelectorRequiredAuthorityAvailable','membershipOrRemovalQualified','observationScope','characters') 'static native party facts';Assert-KmcBootstrapInventoryString $p.observationScope 'static-getters-only'
 foreach($key in @('selectorOpened','liveSelectorRequiredAuthorityAvailable','membershipOrRemovalQualified')){Assert-KmcChunk7Boolean $p.$key $false}
 if($p.characters-isnot[Array]-or$p.characters.Count-lt1-or$p.characters.Count-gt6){throw 'Static native party-character facts are unbounded.'}
 $ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);$ordered=@()
 foreach($c in $p.characters){Assert-KmcExactProperties $c @('actorId','blueprintGuid','isMain','lockedCompanionComponent') 'static character fact';Assert-KmcOrdinaryGuid $c.actorId
  if(-not$ids.Add($c.actorId)){throw 'Static native actor repeated.'};$ordered+=,$c.actorId;$actor=@($World.party|Where-Object id -CEQ $c.actorId);if($actor.Count-ne1){throw 'Static native actor is foreign.'};Assert-KmcBootstrapInventoryString $c.blueprintGuid $actor[0].blueprint
  Assert-KmcChunk7Boolean $c.isMain ($c.actorId-ceq$World.mainActorId);if($c.lockedCompanionComponent-isnot[bool]){throw 'Locked-companion observation must remain a boolean fact.'}
 }
 if(-not$ids.Contains($World.mainActorId)-or(ConvertTo-KmcChunk7Canonical $ordered)-cne(ConvertTo-KmcChunk7Canonical $Inventory.ordinaryAcquisitionPrerequisites.nativePartyCharacterIds)){throw 'Static party facts differ from the actual native character roster.'}
 $d=$Row.isolatedSaveDescriptors;Assert-KmcExactProperties $d @('root','metadataOnly','maximumObservedDescriptors','descriptors') 'isolated descriptor observation';Assert-KmcBootstrapInventoryString $d.root $Root;Assert-KmcChunk7Boolean $d.metadataOnly $true;Assert-KmcOrdinaryInteger $d.maximumObservedDescriptors 32 32
 if($d.descriptors-isnot[Array]-or$d.descriptors.Count-ne1){throw 'Assisted cold inventory must contain exactly its admitted native Manual descriptor.'};$v=$d.descriptors[0]
 Assert-KmcExactProperties $v @('fileName','internalName','gameId','gameName','area','type','isAutoLevelupSave','isActuallySaved','hasFileOnDisk','operationState','systemSaveTimeTicks','systemSaveTimeKind') 'isolated native descriptor'
 foreach($key in @('fileName','internalName','gameId','gameName','area')){Assert-KmcBootstrapInventoryString $v.$key $Request.persistenceLoad.$key}
 Assert-KmcBootstrapInventoryString $v.type 'Manual';Assert-KmcBootstrapInventoryString $v.operationState 'None';Assert-KmcChunk7Boolean $v.hasFileOnDisk $true;Assert-KmcChunk7Boolean $v.isAutoLevelupSave $false
 # Installed getters06007FE1/06007FE2 both return nonempty FolderName; a disk-backed settled descriptor has both true.
 Assert-KmcChunk7Boolean $v.isActuallySaved $true;Assert-KmcOrdinaryInteger $v.systemSaveTimeTicks 0 ([DateTime]::MaxValue.Ticks)
 if($v.systemSaveTimeKind-isnot[string]-or$v.systemSaveTimeKind-cnotin@('Unspecified','Utc','Local')){throw 'Native save timestamp kind is malformed.'}
}

function Assert-KmcAssistedColdArtifactEnvelope($Request,$Artifact,[string]$Kind){
 if($null-eq$Artifact-or$Artifact.GetType().FullName-cne'System.Management.Automation.PSCustomObject'){throw 'Assisted cold native artifact must be one object.'}
 foreach($key in @('evidenceKind','status','createdAtUtc')){if($Artifact.$key-isnot[string]){throw 'Assisted cold artifact kind/status/time must be scalar strings.'}}
 Assert-KmcChunk7ArtifactEnvelope $Request $Artifact $Kind
}

function Assert-KmcAssistedColdAuthority($Row,[string]$Root,[long]$Generation,[long]$Loads){
 Assert-KmcAssistedObjectValue $Row @('root','generation','maximumNativeLoads','maximumNativeWrites','writableEntries','entryCount','admittedNativeLoads','admittedNativeWrites','authorizedLoads','authorizedWrites','unauthorizedLoads','unauthorizedWrites','fatalViolations','suppressedWrites') 'assisted cold authority'
 Assert-KmcChunk7BootstrapInventoryAuthority $Row $Root $Generation $Loads
 Assert-KmcOrdinaryInteger $Row.writableEntries 0 0;Assert-KmcOrdinaryInteger $Row.entryCount 1 1
}
function Assert-KmcAssistedColdNullableId($Value,$Expected,[string]$Description){
 if($null-eq$Expected){if($null-ne$Value){throw ($Description+' acquired an unexpected native ID.')}}
 else{Assert-KmcOrdinaryGuid $Value;Assert-KmcBootstrapInventoryString $Value $Expected}
}
function Assert-KmcAssistedColdIds($Rows,[int]$Minimum,[int]$Maximum,[string]$Description){
 Assert-KmcAssistedArray $Rows $Minimum $Maximum $Description
 $ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
 foreach($id in $Rows){Assert-KmcOrdinaryGuid $id;if(-not$ids.Add($id)){throw ($Description+' repeats an identity.')}}
 return ,$ids
}
function Assert-KmcAssistedColdWorld($Row,$Request,$Labels,$SourceWorld,$SourceReadiness,[long]$SourceMoney,[long]$MinimumFrame){
 $keys=@('frame','game','saveManager','player','state','areaState','mainState','gameId','gameName','mainActorId','area','mode','cutscene','inCombat','nativeLoading','nativeMoney','authority','loadedDescriptor','party','partyCharacterIds','remoteCompanionIds','dialogScheduled','owners','slots','casts','snapshots','completedSaves','semanticRestores','presentationRestores')
 Assert-KmcAssistedObjectValue $Row $keys 'assisted cold native world'
 Assert-KmcOrdinaryInteger $Row.frame $MinimumFrame ([int]::MaxValue)
 $objects=[Collections.Generic.HashSet[int]]::new()
 foreach($key in @('game','saveManager','player','state','areaState','mainState','loadedDescriptor')){Assert-KmcAssistedLabel $Row.$key $Labels;if(-not$objects.Add([int]$Row.$key)){throw 'Assisted cold native world/descriptor labels alias.'}}
 foreach($key in @('gameId','gameName','area')){Assert-KmcBootstrapInventoryString $Row.$key $Request.persistenceLoad.$key}
 Assert-KmcBootstrapInventoryString $Row.mainActorId $SourceWorld.mainActorId;Assert-KmcOrdinaryGuid $Row.mainActorId
 Assert-KmcOrdinaryInteger $Row.nativeMoney $SourceMoney $SourceMoney
 Assert-KmcBootstrapInventoryString $Row.mode 'Default'
 foreach($key in @('cutscene','inCombat','nativeLoading','dialogScheduled')){Assert-KmcChunk7Boolean $Row.$key $false}
 foreach($key in @('owners','slots','casts','snapshots','completedSaves','semanticRestores','presentationRestores')){Assert-KmcOrdinaryInteger $Row.$key 0 0}
 Assert-KmcAssistedArray $SourceWorld.party 1 12 'qualified assisted source field'
 Assert-KmcAssistedArray $Row.party $SourceWorld.party.Count $SourceWorld.party.Count 'assisted cold full field'
 $ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
 foreach($actor in $Row.party){
  Assert-KmcAssistedObjectValue $actor @('id','blueprint','object','descriptor','view','isInGame','isInState','commandsEmpty','queuedCommands','conscious','dead','inCombat','petId','masterId','characterLevel','experience','classes') 'assisted cold field actor'
  Assert-KmcOrdinaryGuid $actor.id;if(-not$ids.Add($actor.id)){throw 'Assisted cold field repeats an actor ID.'}
  $old=@($SourceWorld.party|Where-Object id -CEQ $actor.id);if($old.Count-ne1){throw 'Assisted cold actor is foreign to the qualified full field.'};$old=$old[0]
  Assert-KmcAssistedGuid32 $actor.blueprint;Assert-KmcBootstrapInventoryString $actor.blueprint $old.blueprint
  foreach($key in @('object','descriptor','view')){Assert-KmcAssistedLabel $actor.$key $Labels;if(-not$objects.Add([int]$actor.$key)){throw 'Assisted cold actor/descriptor/view label aliases another native role.'}}
  foreach($key in @('isInGame','isInState','commandsEmpty','conscious')){Assert-KmcChunk7Boolean $actor.$key $true}
  foreach($key in @('dead','inCombat')){Assert-KmcChunk7Boolean $actor.$key $false};Assert-KmcOrdinaryInteger $actor.queuedCommands 0 0
  foreach($key in @('petId','masterId')){Assert-KmcAssistedColdNullableId $actor.$key $old.$key ('assisted cold '+$key)}
  Assert-KmcOrdinaryInteger $actor.characterLevel 0 20;Assert-KmcOrdinaryInteger $actor.characterLevel $old.characterLevel $old.characterLevel
  $xp=if($actor.id-ceq$Row.mainActorId){$SourceReadiness.mainProgression.experience}else{$old.experience}
  Assert-KmcOrdinaryInteger $actor.experience 0 ([int]::MaxValue);Assert-KmcOrdinaryInteger $actor.experience $xp $xp
  Assert-KmcAssistedArray $actor.classes 0 20 'assisted cold class ranks';$sum=0;$classes=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
  foreach($c in $actor.classes){Assert-KmcAssistedObjectValue $c @('guid','level') 'assisted cold class';Assert-KmcAssistedGuid32 $c.guid;if(-not$classes.Add($c.guid)){throw 'Assisted cold class repeats.'};Assert-KmcOrdinaryInteger $c.level 1 20;$sum+=$c.level}
  if($sum-ne$actor.characterLevel){throw 'Assisted cold class ranks differ from the actor level.'}
  Assert-KmcAssistedSame $actor.classes $old.classes 'assisted cold exact source class ranks'
 }
 if(-not$ids.Contains($Row.mainActorId)){throw 'Assisted cold full field lost its exact main actor.'}
 $characters=Assert-KmcAssistedColdIds $Row.partyCharacterIds 1 6 'assisted cold native characters'
 $remote=Assert-KmcAssistedColdIds $Row.remoteCompanionIds 0 32 'assisted cold native remote characters'
 $sourceCharacters=$SourceReadiness.staticPartyFacts.characters
 Assert-KmcAssistedArray $sourceCharacters 1 6 'qualified source static native characters'
 if($characters.Count-ne$sourceCharacters.Count){throw 'Assisted cold native character roster count changed.'}
 foreach($character in $sourceCharacters){if(-not$characters.Contains($character.actorId)){throw 'Assisted cold native character membership changed.'}}
 if(-not$characters.Contains($Row.mainActorId)){throw 'Assisted cold main is absent from native character membership.'}
 foreach($id in $Row.partyCharacterIds){if(-not$ids.Contains($id)){throw 'Assisted cold native character is absent from full field.'}}
 foreach($id in $Row.remoteCompanionIds){if($ids.Contains($id)){throw 'Assisted cold remote roster overlaps the full field.'}}
 foreach($actor in $Row.party){
  if($characters.Contains($actor.id)){if($null-ne$actor.masterId){throw 'Assisted cold native character is also a pet.'}}
  else{if($null-eq$actor.masterId-or-not$characters.Contains($actor.masterId)){throw 'Assisted cold non-character actor lacks its exact character owner.'}}
  if($null-ne$actor.petId){$pet=@($Row.party|Where-Object id -CEQ $actor.petId);if($pet.Count-ne1-or$pet[0].masterId-cne$actor.id-or$actor.petId-ceq$actor.id-or$characters.Contains($actor.petId)){throw 'Assisted cold pet link is nonreciprocal or enters native character membership.'}}
  if($null-ne$actor.masterId){$master=@($Row.party|Where-Object id -CEQ $actor.masterId);if($master.Count-ne1-or$master[0].petId-cne$actor.id-or$actor.masterId-ceq$actor.id){throw 'Assisted cold master link is nonreciprocal.'}}
 }
}
function Assert-KmcAssistedColdLoader($Row,$Request,$World,$Labels,[string]$Root,[long]$Generation){
 Assert-KmcAssistedObjectValue $Row @('state','callbackObserved','errors','path','descriptor','fileName','internalName','gameId','gameName','area','type','operation','autoLevelup','isActuallySaved','hasFileOnDisk','authority') 'assisted cold native loader'
 Assert-KmcBootstrapInventoryString $Row.state 'LoadedAndVerified';Assert-KmcChunk7Boolean $Row.callbackObserved $true;Assert-KmcAssistedArray $Row.errors 0 0 'assisted cold loader errors'
 Assert-KmcBootstrapInventoryString $Row.path (Join-Path $Root $Request.persistenceLoad.fileName)
 Assert-KmcAssistedLabel $Row.descriptor $Labels;Assert-KmcOrdinaryInteger $Row.descriptor $World.loadedDescriptor $World.loadedDescriptor
 foreach($key in @('fileName','internalName','gameId','gameName','area')){Assert-KmcBootstrapInventoryString $Row.$key $Request.persistenceLoad.$key}
 Assert-KmcBootstrapInventoryString $Row.type 'Manual';Assert-KmcBootstrapInventoryString $Row.operation 'None';Assert-KmcChunk7Boolean $Row.autoLevelup $false
 foreach($key in @('isActuallySaved','hasFileOnDisk')){Assert-KmcChunk7Boolean $Row.$key $true}
 Assert-KmcAssistedColdAuthority $Row.authority $Root $Generation 1
}
function Assert-KmcAssistedColdJsonObject($Value,[string]$Description){
 if($null-eq$Value-or$Value.GetType().FullName-cne'System.Management.Automation.PSCustomObject'){throw ($Description+' must be one JSON object.')}
}
function Assert-KmcAssistedColdProfile($Inventory,$World){
 Assert-KmcAssistedColdJsonObject $Inventory 'assisted cold native profile inventory'
 Assert-KmcOrdinaryInteger $Inventory.schemaVersion 1 1
 foreach($pair in @(@('captureStatus','PASS'),@('captureKind','installed-native-small-body-catalogue'),@('gameplayAssemblyMvid','07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'))){Assert-KmcBootstrapInventoryString $Inventory.($pair[0]) $pair[1]}
 foreach($key in @('profileAdmissionChanged','nativeFixtureCreated','poseQualified','ordinaryAcquisitionProven')){Assert-KmcChunk7Boolean $Inventory.$key $false}
 Assert-KmcAssistedArray $Inventory.party $World.party.Count $World.party.Count 'assisted cold native body inventory'
 foreach($actor in $World.party){
  $profile=@($Inventory.party|Where-Object id -CEQ $actor.id);if($profile.Count-ne1){throw 'Assisted cold native body inventory changed actors.'};$profile=$profile[0]
  Assert-KmcBootstrapInventoryString $profile.id $actor.id;Assert-KmcBootstrapInventoryString $profile.blueprintGuid $actor.blueprint
  $level=$profile.nativeLevelUpReadiness;Assert-KmcAssistedColdJsonObject $level 'assisted cold native level readiness'
  Assert-KmcOrdinaryInteger $level.characterLevel $actor.characterLevel $actor.characterLevel;Assert-KmcOrdinaryInteger $level.experience $actor.experience $actor.experience
  if($null-ne$level.nextLevelExperience){Assert-KmcOrdinaryInteger $level.nextLevelExperience 0 ([int]::MaxValue)}
  foreach($key in @('canLevelUp','isDead')){if($level.$key-isnot[bool]){throw 'Assisted cold native readiness flag is not boolean.'}}
  Assert-KmcChunk7Boolean $level.isDead $false
  foreach($key in @('partyInCombat','controllerCreated','completeChoicesObserved','ordinaryAcquisitionProven')){Assert-KmcChunk7Boolean $level.$key $false}
 }
 $r=$Inventory.ordinaryAcquisitionPrerequisites;Assert-KmcAssistedColdJsonObject $r 'assisted cold native prerequisites'
 Assert-KmcChunk7Boolean $r.observationOnly $true
 foreach($key in @('purchaseAuthorized','sixPairFixtureQualified','defaultHireMatchesPlayerExperienceExactly')){Assert-KmcChunk7Boolean $r.$key $false}
 foreach($key in @('nativeMoney','nativeHireCostNow')){Assert-KmcOrdinaryInteger $r.$key 0 ([long]::MaxValue)}
 foreach($key in @('mainCharacterExperience','nativeDefaultHireExperienceNow')){Assert-KmcOrdinaryInteger $r.$key 0 ([int]::MaxValue)}
 foreach($key in @('nativePartyLevel','mainCharacterLevel')){Assert-KmcOrdinaryInteger $r.$key 0 20}
 Assert-KmcOrdinaryInteger $r.nativeFieldCharacterLimit 6 6;Assert-KmcBootstrapInventoryString $r.mainCharacterId $World.mainActorId
 $main=@($World.party|Where-Object id -CEQ $World.mainActorId)[0]
 Assert-KmcOrdinaryInteger $r.mainCharacterLevel $main.characterLevel $main.characterLevel;Assert-KmcOrdinaryInteger $r.mainCharacterExperience $main.experience $main.experience
 Assert-KmcChunk7Boolean $r.nativeAffordableNow ($r.nativeMoney-ge$r.nativeHireCostNow)
 [void](Assert-KmcAssistedColdIds $r.nativePartyCharacterIds 1 6 'assisted cold inventory native characters')
 Assert-KmcAssistedSame $r.nativePartyCharacterIds $World.partyCharacterIds 'assisted cold observed native character roster'
}

function Get-KmcAssistedColdLabels($Load){
 Assert-KmcAssistedObjectValue $Load @('contract','installed','installationAttempted','entries','exits','complete','events','hooks','errors','identities') 'assisted cold native PostLoad observer'
 Assert-KmcBootstrapInventoryString $Load.contract 'bootstrap-native-postload-v1'
 foreach($key in @('installed','installationAttempted','complete')){Assert-KmcChunk7Boolean $Load.$key $true}
 foreach($key in @('entries','exits')){Assert-KmcOrdinaryInteger $Load.$key 1 1}
 Assert-KmcAssistedArray $Load.errors 0 0 'assisted cold native PostLoad errors';Assert-KmcAssistedArray $Load.events 2 2 'assisted cold native PostLoad events';Assert-KmcAssistedArray $Load.hooks 1 1 'assisted cold native PostLoad hooks'
 $h=$Load.hooks[0];Assert-KmcAssistedObjectValue $h @('declaringType','method','token','moduleMvid') 'assisted cold PostLoad hook'
 foreach($pair in @(@('declaringType','Kingmaker.Player'),@('method','PostLoad'),@('token','06000DC7'),@('moduleMvid','07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'))){Assert-KmcBootstrapInventoryString $h.($pair[0]) $pair[1]}
 $r=$Load.identities;Assert-KmcAssistedObjectValue $r @('contract','capacity','ids','retainedCount','faults','released') 'assisted cold native identity registry'
 Assert-KmcBootstrapInventoryString $r.contract 'retained-native-object-identity-v1';Assert-KmcOrdinaryInteger $r.capacity 128 128;Assert-KmcOrdinaryInteger $r.faults 0 0;Assert-KmcChunk7Boolean $r.released $false
 Assert-KmcAssistedArray $r.ids 1 128 'assisted cold retained identities';Assert-KmcOrdinaryInteger $r.retainedCount $r.ids.Count $r.ids.Count
 $labels=[Collections.Generic.HashSet[int]]::new();foreach($id in $r.ids){Assert-KmcBootstrapInventoryObject $id;if(-not$labels.Add([int]$id)){throw 'Assisted cold registry repeats a retained label.'}}
 return ,$labels
}
function Assert-KmcAssistedColdNoPreparation($Value,$Request){
 Assert-KmcAssistedObjectValue $Value @('contract','events','hooks','maximumEvents','dropped','errors','complete','installedBeforeWorldLoad','prepareMetric') 'assisted cold restoration-entry observer'
 Assert-KmcBootstrapInventoryString $Value.contract 'chunk7-native-restoration-entry-observer-v1';Assert-KmcBootstrapInventoryString $Value.prepareMetric 'native-method-entries-including-production-refused-attempts'
 foreach($key in @('complete','installedBeforeWorldLoad')){Assert-KmcChunk7Boolean $Value.$key $true}
 Assert-KmcOrdinaryInteger $Value.maximumEvents 2048 2048;foreach($key in @('dropped','errors')){Assert-KmcOrdinaryInteger $Value.$key 0 0}
 Assert-KmcAssistedArray $Value.events 0 0 'assisted cold native restoration/preparation entries';Assert-KmcAssistedArray $Value.hooks 4 4 'assisted cold restoration hooks'
 $tokens=@('06000C3C','0600939D');$methods=@('Prepare','NewRound','RestoreActor','RestoreDebt');$prefixes=@('PrepareBefore','RoundBefore','RestoreActorBefore','RestoreDebtBefore');$postfixes=@('PrepareAfter','RoundAfter','RestoreActorAfter','RestoreDebtAfter')
 for($i=0;$i-lt4;$i++){
  $h=$Value.hooks[$i];Assert-KmcAssistedObjectValue $h @('declaringType','method','token','moduleMvid','prefix','postfix') 'assisted cold restoration hook'
  Assert-KmcBootstrapInventoryString $h.method $methods[$i];Assert-KmcBootstrapInventoryString $h.prefix $prefixes[$i];Assert-KmcBootstrapInventoryString $h.postfix $postfixes[$i]
  if($i-lt2){Assert-KmcBootstrapInventoryString $h.declaringType $(if($i-eq0){'TurnBased.Controllers.TurnController'}else{'Kingmaker.UnitLogic.UnitCombatState'});Assert-KmcBootstrapInventoryString $h.token $tokens[$i];Assert-KmcBootstrapInventoryString $h.moduleMvid '07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'}
  else{Assert-KmcBootstrapInventoryString $h.moduleMvid $Request.dllMvid;Assert-KmcBootstrapInventoryString $h.declaringType 'KingmakerMountedCombat.Integration.NativeCombatActorPersistence';if($h.token-isnot[string]-or$h.token-cnotmatch'^06[0-9A-F]{6}$'){throw 'Assisted cold product restore token is malformed.'}}
 }
}
function Assert-KmcChunk7AssistedColdInventoryEvidence($Request,$Artifact,$SourceArtifact,[string]$Status='PASS'){
 if($Status-cnotin@('PASS','FAIL')){throw 'Assisted cold completion status is outside the closed contract.'}
 Assert-KmcChunk7AssistedColdInventoryDescriptor $Request
 Assert-KmcAssistedColdArtifactEnvelope $Request $Artifact 'chunk7-assisted-cold-inventory-evidence'
 if($Status-ceq'PASS'-and$Artifact.status-cne'PASS'){throw 'PASS assisted cold result requires its PASS native artifact.'}
 if($Artifact.status-cne'PASS'){return}
 $o=$Artifact.observations;$s=$Request.assistedColdInventory;$source=$SourceArtifact
 if($null-eq$source-or$source.GetType().FullName-cne'System.Management.Automation.PSCustomObject'){throw 'Assisted cold native proof requires its exact qualified producer artifact.'}
 Assert-KmcOrdinaryInteger $source.schemaVersion 1 1
 foreach($pair in @(@('runId',$s.sourceRunId),@('scenario','chunk7-assisted-fixture-preparation'),@('evidenceKind','chunk7-assisted-fixture-evidence'),@('status','PASS'))){Assert-KmcBootstrapInventoryString $source.($pair[0]) $pair[1]}
 foreach($key in @('commit','dllSha256','dllMvid')){Assert-KmcBootstrapInventoryString $source.$key $Request.$key}
 Assert-KmcAssistedArray $source.errors 0 0 'assisted cold producer errors'
 $so=$source.observations;Assert-KmcAssistedColdJsonObject $so 'assisted cold producer observations'
 Assert-KmcAssistedColdJsonObject $so.selection 'assisted cold producer selection'
 Assert-KmcAssistedColdJsonObject $so.nativeProfileInventory 'assisted cold source native profile'
 Assert-KmcAssistedColdJsonObject $so.nativeProfileInventory.ordinaryAcquisitionPrerequisites 'assisted cold source native prerequisites'
 Assert-KmcBootstrapInventoryString $so.selection.case 'authored-main-ranger4-manual';Assert-KmcChunk7Boolean $so.selection.baseGameOnly $true;Assert-KmcChunk7Boolean $so.selection.assistedSetupOnly $true
 Assert-KmcChunk7Boolean $so.assistanceOnly $true
 foreach($key in @('ordinaryAcquisitionProven','ordinaryProgressionProven','mountedCombatProven','smallPoseQualified')){Assert-KmcChunk7Boolean $so.$key $false}
 foreach($key in @('fileName','internalName','gameId','gameName','area','sha256')){Assert-KmcBootstrapInventoryString $so.finalManual.$key $Request.persistenceLoad.$key}
 foreach($key in @('length','lastWriteTimeUtcTicks')){Assert-KmcOrdinaryInteger $so.finalManual.$key $Request.persistenceLoad.$key $Request.persistenceLoad.$key}
 Assert-KmcBootstrapInventoryString $so.finalManual.type 'Manual';Assert-KmcBootstrapInventoryString $so.finalManual.operation 'None';Assert-KmcChunk7Boolean $so.finalManual.autoLevelup $false
 $keys=@('processId','selection','source','sourceNativeWorld','sourceFixtureReadiness','sourceNativeMoney','beforeLoad','resourceWrites','xpWrites','moneyWrites','profileAdmissionChanged','ordinaryAcquisitionProven','sixPairFixtureQualified','mountRequests','choiceRequests','ordinaryProgressionProven','loadedWorld','loader','nativeProfileInventory','fixtureReadiness','archive','settledWorld','settledLoader','nativeLoad','noPreparation','cleanup')
 Assert-KmcAssistedObjectValue $o $keys 'assisted cold successful native observations'
 Assert-KmcAssistedSame $o.selection $s 'assisted cold request selection'
 Assert-KmcAssistedSame $o.sourceNativeWorld $so.settledWorld 'assisted cold qualified source native world'
 Assert-KmcAssistedSame $o.sourceFixtureReadiness $so.fixtureReadiness 'assisted cold qualified source readiness'
 Assert-KmcOrdinaryInteger $o.processId 1 ([int]::MaxValue);Assert-KmcOrdinaryInteger $so.processId 1 ([int]::MaxValue)
 if($o.processId-eq$so.processId){throw 'Assisted cold inventory reused the producer native process.'}
 foreach($key in @('resourceWrites','xpWrites','moneyWrites','mountRequests','choiceRequests')){Assert-KmcOrdinaryInteger $o.$key 0 0}
 foreach($key in @('profileAdmissionChanged','ordinaryAcquisitionProven','sixPairFixtureQualified','ordinaryProgressionProven')){Assert-KmcChunk7Boolean $o.$key $false}
 $root=Get-KmcAssistedSaveRoot $Request
 Assert-KmcAssistedObjectValue $o.source @('runId','artifactSha256','archiveSha256','archiveLeaf','sameCandidate','assistedSetupOnly','copiedArtifactPath','diagnosticArchiveContentRead') 'assisted cold source provenance'
 foreach($pair in @(@('runId',$s.sourceRunId),@('artifactSha256',$s.sourceArtifactSha256),@('archiveSha256',$s.sourceArchiveSha256),@('archiveLeaf',$Request.persistenceLoad.fileName),@('copiedArtifactPath',(Join-Path (Split-Path $root -Parent) 'assisted-cold-source.json')))){Assert-KmcBootstrapInventoryString $o.source.($pair[0]) $pair[1]}
 foreach($key in @('sameCandidate','assistedSetupOnly')){Assert-KmcChunk7Boolean $o.source.$key $true};Assert-KmcChunk7Boolean $o.source.diagnosticArchiveContentRead $false
 $money=$so.nativeProfileInventory.ordinaryAcquisitionPrerequisites.nativeMoney
 Assert-KmcOrdinaryInteger $money 0 ([long]::MaxValue);Assert-KmcOrdinaryInteger $o.sourceNativeMoney $money $money
 $b=$o.beforeLoad;$w=$o.loadedWorld;$f=$o.settledWorld;$load=$o.nativeLoad;$labels=Get-KmcAssistedColdLabels $load
 Assert-KmcAssistedObjectValue $b @('frame','game','saveManager','menuPlayer','loadedAreaPresent','nativeLoading','authority') 'assisted cold before-load owner'
 Assert-KmcOrdinaryInteger $b.frame 0 ([int]::MaxValue);$before=[Collections.Generic.HashSet[int]]::new()
 foreach($key in @('game','saveManager','menuPlayer')){Assert-KmcAssistedLabel $b.$key $labels;if(-not$before.Add([int]$b.$key)){throw 'Assisted cold before-load native owner labels alias.'}}
 foreach($key in @('loadedAreaPresent','nativeLoading')){Assert-KmcChunk7Boolean $b.$key $false}
 Assert-KmcAssistedColdAuthority $b.authority $root $b.authority.generation 0
 Assert-KmcAssistedColdWorld $w $Request $labels $o.sourceNativeWorld $o.sourceFixtureReadiness $money $b.frame
 Assert-KmcAssistedColdWorld $f $Request $labels $o.sourceNativeWorld $o.sourceFixtureReadiness $money $w.frame
 foreach($world in @($w,$f)){
  Assert-KmcAssistedColdAuthority $world.authority $root $b.authority.generation 1
  foreach($key in @('game','saveManager')){Assert-KmcOrdinaryInteger $world.$key $b.$key $b.$key}
 }
 foreach($property in $w.PSObject.Properties){if($property.Name-cne'frame'){Assert-KmcAssistedSame $f.($property.Name) $property.Value ('assisted cold settled world '+$property.Name)}}
 Assert-KmcAssistedColdLoader $o.loader $Request $w $labels $root $b.authority.generation
 Assert-KmcAssistedColdLoader $o.settledLoader $Request $f $labels $root $b.authority.generation
 Assert-KmcAssistedSame $o.settledLoader $o.loader 'assisted cold settled loader/descriptor/authority'
 $prior=$b.frame
 for($i=0;$i-lt2;$i++){
  $e=$load.events[$i];Assert-KmcAssistedObjectValue $e @('kind','frame','game','saveManager','player','gameId','mainActorId') 'assisted cold native PostLoad event'
  Assert-KmcBootstrapInventoryString $e.kind $(if($i-eq0){'native-player-postload-before'}else{'native-player-postload-after'})
  Assert-KmcOrdinaryInteger $e.frame $prior $w.frame;$prior=$e.frame
  foreach($key in @('game','saveManager','player')){Assert-KmcAssistedLabel $e.$key $labels;Assert-KmcOrdinaryInteger $e.$key $w.$key $w.$key}
  Assert-KmcBootstrapInventoryString $e.gameId $Request.persistenceLoad.gameId
  if($i-eq1-or$null-ne$e.mainActorId){Assert-KmcBootstrapInventoryString $e.mainActorId $w.mainActorId}
 }
 if($load.events[0].frame-ne$load.events[1].frame){throw 'Assisted cold PostLoad callback did not return synchronously in the observed frame.'}
 Assert-KmcAssistedColdNoPreparation $o.noPreparation $Request
 $archive=$o.archive;Assert-KmcAssistedObjectValue $archive @('schemaVersion','campaignId','areaId','pairs','participations','slots','combatPresent','nativeRestorationGrantedByDiagnostic') 'assisted cold empty mounted archive'
 Assert-KmcOrdinaryInteger $archive.schemaVersion 3 3;Assert-KmcBootstrapInventoryString $archive.campaignId $Request.persistenceLoad.gameId;Assert-KmcBootstrapInventoryString $archive.areaId $Request.persistenceLoad.area
 foreach($key in @('pairs','participations','slots')){Assert-KmcOrdinaryInteger $archive.$key 0 0};foreach($key in @('combatPresent','nativeRestorationGrantedByDiagnostic')){Assert-KmcChunk7Boolean $archive.$key $false}
 Assert-KmcAssistedColdProfile $o.nativeProfileInventory $w
 Assert-KmcOrdinaryInteger $o.nativeProfileInventory.ordinaryAcquisitionPrerequisites.nativeMoney $money $money
 Assert-KmcAssistedColdFixtureReadiness $o.fixtureReadiness $Request $w $f $o.nativeProfileInventory $root
 Assert-KmcAssistedSame $o.fixtureReadiness.mainProgression $o.sourceFixtureReadiness.mainProgression 'assisted cold native main classes/XP/archetypes'
 Assert-KmcAssistedObjectValue $o.cleanup @('custodySettled','nativeWorkerSettled','observersReleased','worldAdmitted','sourcePinned','sourceUnchanged','authority') 'assisted cold completed cleanup'
 foreach($key in @('custodySettled','nativeWorkerSettled','observersReleased','worldAdmitted','sourcePinned','sourceUnchanged')){Assert-KmcChunk7Boolean $o.cleanup.$key $true}
 Assert-KmcAssistedColdAuthority $o.cleanup.authority $root $b.authority.generation 1
}

function Assert-KmcAssistedColdPending($Request,$Artifact){
 Assert-KmcChunk7AssistedColdInventoryDescriptor $Request
 Assert-KmcAssistedColdArtifactEnvelope $Request $Artifact 'chunk7-assisted-cold-inventory-cleanup-pending'
 Assert-KmcBootstrapInventoryString $Artifact.status 'FAIL'
 if($Artifact.errors.Count-eq0){throw 'Assisted cold retained cleanup must preserve its failure.'}
 $o=$Artifact.observations
 Assert-KmcAssistedObjectValue $o @('cleanupPendingAtHostBoundary','cleanupCustodyRetained','reason','authority') 'assisted cold retained cleanup'
 foreach($key in @('cleanupPendingAtHostBoundary','cleanupCustodyRetained')){Assert-KmcChunk7Boolean $o.$key $true}
 if($o.reason-isnot[string]-or[string]::IsNullOrWhiteSpace($o.reason)-or$o.reason.Length-gt16384){throw 'Assisted cold pending reason is missing or unbounded.'}
 $a=$o.authority
 Assert-KmcAssistedObjectValue $a @('root','generation','maximumNativeLoads','maximumNativeWrites','writableEntries','entryCount','admittedNativeLoads','admittedNativeWrites','authorizedLoads','authorizedWrites','unauthorizedLoads','unauthorizedWrites','fatalViolations','suppressedWrites') 'assisted cold pending authority'
 Assert-KmcBootstrapInventoryString $a.root (Get-KmcAssistedSaveRoot $Request)
 Assert-KmcOrdinaryInteger $a.generation 1 ([int]::MaxValue);Assert-KmcOrdinaryInteger $a.maximumNativeLoads 1 1
 if($null-ne$a.maximumNativeWrites){throw 'Assisted cold pending authority gained a write quota.'}
 Assert-KmcOrdinaryInteger $a.writableEntries 0 0;Assert-KmcOrdinaryInteger $a.entryCount 1 1
 foreach($key in @('admittedNativeLoads','authorizedLoads')){Assert-KmcOrdinaryInteger $a.$key 0 1}
 foreach($key in @('admittedNativeWrites','authorizedWrites')){Assert-KmcOrdinaryInteger $a.$key 0 0}
 foreach($key in @('unauthorizedLoads','unauthorizedWrites','fatalViolations','suppressedWrites')){Assert-KmcOrdinaryInteger $a.$key 0 ([int]::MaxValue)}
}
function Read-KmcAssistedColdManifestArtifact($Request,$Manifest,[string]$Leaf,[string]$Kind){
 $rows=@($Manifest.artifacts|Where-Object{$_.relativePath-ceq$Leaf-or$_.kind-ceq$Kind})
 if($rows.Count-ne1){throw 'Assisted cold requires one exact manifest row per observed artifact.'}
 Assert-KmcBootstrapInventoryString $rows[0].relativePath $Leaf;Assert-KmcBootstrapInventoryString $rows[0].kind $Kind
 Assert-KmcOrdinaryInteger $rows[0].length 1 16777216
 if($rows[0].sha256-isnot[string]-or$rows[0].sha256-cnotmatch'^[a-f0-9]{64}$'){throw 'Assisted cold manifest artifact hash is malformed.'}
 Read-KmcChunk7ManifestArtifact $Request $Manifest $Leaf $Kind
}
function Assert-KmcChunk7AssistedColdInventoryArtifact($Request,$Manifest,[string]$Status='PASS'){
 if($Status-cnotin@('PASS','FAIL')){throw 'Assisted cold result status is outside the closed contract.'}
 $leaf='chunk7-assisted-cold-inventory-evidence.json';$kind='chunk7-assisted-cold-inventory-evidence'
 $pendingLeaf='chunk7-assisted-cold-inventory-cleanup-pending.json';$pendingKind='chunk7-assisted-cold-inventory-cleanup-pending'
 $records=@($Manifest.artifacts|Where-Object{$_.relativePath-ceq$leaf-or$_.kind-ceq$kind})
 $pendingRecords=@($Manifest.artifacts|Where-Object{$_.relativePath-ceq$pendingLeaf-or$_.kind-ceq$pendingKind})
 Assert-KmcChunk7AssistedColdInventorySelection $Request
 if(-not(Test-KmcChunk7AssistedColdInventoryScenario $Request.scenario)){if($records.Count-ne0-or$pendingRecords.Count-ne0){throw 'Foreign request supplied assisted cold evidence.'};return}
 if($Status-ceq'PASS'-and$pendingRecords.Count-ne0){throw 'PASS assisted cold result cannot retain a cleanup-pending artifact.'}
 if($pendingRecords.Count-ne0){$pendingArtifact=Read-KmcAssistedColdManifestArtifact $Request $Manifest $pendingLeaf $pendingKind;Assert-KmcAssistedColdPending $Request $pendingArtifact}
 if($Status-ceq'FAIL'-and$records.Count-eq0){return}
 $artifact=Read-KmcAssistedColdManifestArtifact $Request $Manifest $leaf $kind;$source=$null
 if($artifact.status-is[string]-and$artifact.status-ceq'PASS'){$source=Read-KmcAssistedColdSourceArtifact $Request}
 Assert-KmcChunk7AssistedColdInventoryEvidence $Request $artifact $source $Status
}
