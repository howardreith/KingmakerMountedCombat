Set-StrictMode -Version Latest
function Get-KmcChunk7OwnerSeedAcquisitionProjection($Observations){
 $world=$Observations.loadedWorld
 $actors=@($world.party|Sort-Object -Property id|ForEach-Object{
  $actor=$_
  [pscustomobject]@{actorId=$actor.id;blueprint=$actor.blueprint;experience=$actor.experience;characterLevel=$actor.characterLevel;
   classes=@($actor.classes|Sort-Object -Property guid|ForEach-Object{[pscustomobject]@{classGuid=$_.guid;level=$_.level}})}
 })
 [pscustomobject]@{fixtureReadiness=$Observations.fixtureReadiness;nativeProfileInventory=$Observations.nativeProfileInventory;
  afterSaveArrangement=[pscustomobject]@{money=$world.nativeMoney;experienceRatePercent=$Observations.fixtureReadiness.experienceRatePercent;actors=$actors}}
}
function Test-KmcChunk7ManualDerivedScenario([string]$Scenario){$Scenario-ceq'chunk7-manual-derived-acquisition'}
function Get-KmcChunk7ManualDerivedRows {@('chunk7-manual-derived-acquisition','C7-manual-derived-source','C7-manual-derived-native-load','C7-manual-derived-eligibility','C7-manual-derived-native-choices','C7-manual-derived-native-field','C7-manual-derived-native-manual-save','C7-manual-derived-cleanup','C7-manual-derived-runtime','C7-manual-derived-interrupted')}
function Test-KmcManualDerivedPaidCase([string]$Case){$Case-ceq'paid-small-ranger-horse'-or$Case-ceq'paid-medium-ranger-horse'}
function Assert-KmcMediumHireBodyPins($Pins,$Readiness){
 Assert-KmcExactProperties $Pins @('templateActorId','raceGuid','presetGuid','skeletonName','gender','leftHanded') 'Medium native body pins'
 Assert-KmcOrdinaryGuid $Pins.templateActorId
 if($Pins.templateActorId-cnotin$Readiness.partyCharacterIds){throw 'Medium body template must be an observed native field character.'}
 foreach($key in @('raceGuid','presetGuid')){if($Pins.$key-isnot[string]-or$Pins.$key-cnotmatch'^[a-f0-9]{32}$'){throw 'Medium body blueprint must be exact.'}}
 if($Pins.skeletonName-isnot[string]-or[string]::IsNullOrWhiteSpace($Pins.skeletonName)-or$Pins.skeletonName.Length-gt160-or$Pins.skeletonName-match'[\x00-\x1f\x7f]'-or$Pins.gender-isnot[string]-or$Pins.gender-cnotin@('Male','Female')-or$Pins.leftHanded-isnot[bool]){throw 'Medium body selection is malformed.'}
}
function Assert-KmcMediumHireBodySource($Inventory,$Pins){
 if($Inventory.party-isnot[Array]-or$Inventory.party.Count-lt1-or$Inventory.party.Count-gt24){throw 'Medium native body source needs a bounded observed party.'}
 foreach($actor in $Inventory.party){if($actor-isnot[pscustomobject]){throw 'Medium native body source contains a non-object actor.'}}
 $rows=@($Inventory.party|Where-Object id -CEQ $Pins.templateActorId);if($rows.Count-ne1){throw 'Medium template must identify exactly one observed actor.'};$row=$rows[0]
 foreach($key in @('originalSize','effectiveSize')){Assert-KmcBootstrapInventoryString $row.$key 'Medium'}
 Assert-KmcChunk7Boolean $row.polymorphed $false;Assert-KmcChunk7Boolean $row.dollPresent $true
 foreach($pair in @(@('raceGuid','raceGuid'),@('dollPresetGuid','presetGuid'),@('dollGender','gender'))){Assert-KmcBootstrapInventoryString $row.($pair[0]) $Pins.($pair[1])}
 Assert-KmcChunk7Boolean $row.dollLeftHanded $Pins.leftHanded
 if($row.raceId-isnot[string]-or[string]::IsNullOrWhiteSpace($row.raceId)){throw 'Medium native race identity is missing.'};Assert-KmcBootstrapInventoryString $row.dollRaceId $row.raceId
 if($row.character-isnot[pscustomobject]-or$row.character.skeleton-isnot[pscustomobject]){throw 'Medium native rig observation is malformed.'}
 Assert-KmcChunk7Boolean $row.character.present $true;Assert-KmcChunk7Boolean $row.character.skeleton.present $true;Assert-KmcBootstrapInventoryString $row.character.skeleton.name $Pins.skeletonName
}
function Get-KmcManualDerivedPaidBody($Selection){
 if($Selection.case-ceq'paid-medium-ranger-horse'){return $Selection.mediumBody}
 if($Selection.case-cne'paid-small-ranger-horse'){throw 'Body selection belongs to a closed paid-hire case.'}
 [pscustomobject]@{raceGuid=$Selection.smallRaceGuid;presetGuid=$Selection.smallPresetGuid;skeletonName=$Selection.skeletonName;gender=$Selection.gender;leftHanded=$Selection.leftHanded}
}
function Assert-KmcChunk7ManualDerivedSelection($Request){
 if($Request-is[Collections.IDictionary]){$Request=[pscustomobject]$Request}
 $has=$null-ne$Request.PSObject.Properties['manualDerived']
 if(-not(Test-KmcChunk7ManualDerivedScenario $Request.scenario)){if($has){throw 'Manual-derived selection is forbidden under another family.'};return}
 if(-not$has){throw 'Manual-derived acquisition requires exact explicit selection.'};$s=$Request.manualDerived
 $selectionKeys=@('case','sourceRunId','sourceArtifactSha256','sourceArchiveSha256','baseGameOnly','expectedNativeLoadCount','expectedNativeWriteCount','nativeWriteLeaf','readiness','existingActorId','existingLevel','existingExperience','existingRangerLevel','smallRaceGuid','smallPresetGuid','skeletonName','gender','leftHanded');if($s.case-ceq'paid-medium-ranger-horse'){$selectionKeys+='mediumBody'}
 Assert-KmcExactProperties $s $selectionKeys 'Manual-derived selection'
 if($s.case-isnot[string]-or$s.case-cnotin@('existing-ranger-horse','paid-small-ranger-horse','paid-medium-ranger-horse')-or$s.sourceRunId-isnot[string]-or$s.sourceRunId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'-or$s.sourceRunId-ceq$Request.runId){throw 'Manual-derived case/source run differs.'}
 foreach($key in @('sourceArtifactSha256','sourceArchiveSha256')){if($s.$key-isnot[string]-or$s.$key-cnotmatch'^[a-f0-9]{64}$'){throw 'Manual-derived source requires exact SHA256 pins.'}}
 Assert-KmcChunk7Boolean $s.baseGameOnly $true
 if($s.nativeWriteLeaf-cnotin@('Manual_2_KMC_C7_MANUAL_DERIVED_FIELD.zks','Manual_304_KMC_C7_MANUAL_DERIVED_FIELD.zks')){throw 'Manual-derived output is outside the two closed source modes.'}
 foreach($key in @('expectedNativeLoadCount','expectedNativeWriteCount')){Assert-KmcOrdinaryInteger $s.$key 1 1}
 $p=$s.readiness;Assert-KmcExactProperties $p @('mainActorId','mainLevel','mainExperience','money','experienceRatePercent','nativeHireCost','defaultHireExperience','partyCharacterIds') 'native readiness pins'
 Assert-KmcOrdinaryGuid $p.mainActorId;Assert-KmcOrdinaryInteger $p.mainLevel 1 20;Assert-KmcOrdinaryInteger $p.mainExperience 0 ([int]::MaxValue)
 Assert-KmcOrdinaryInteger $p.money 0;Assert-KmcOrdinaryInteger $p.experienceRatePercent 100 100;Assert-KmcOrdinaryInteger $p.nativeHireCost 1 ([int]::MaxValue);Assert-KmcOrdinaryInteger $p.defaultHireExperience 0 ([int]::MaxValue)
 if($p.partyCharacterIds-isnot[Array]-or$p.partyCharacterIds.Count-lt1-or$p.partyCharacterIds.Count-gt6){throw 'Readiness needs bounded native field characters.'}
 $ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);$last=''
 foreach($id in $p.partyCharacterIds){Assert-KmcOrdinaryGuid $id;if(-not$ids.Add($id)-or[StringComparer]::Ordinal.Compare($last,$id)-ge0){throw 'Readiness party IDs must be unique and ordinally sorted.'};$last=$id}
 if(-not$ids.Contains($p.mainActorId)){throw 'Readiness main character is absent from the native party.'}
 if($s.case-ceq'existing-ranger-horse'){
  Assert-KmcOrdinaryGuid $s.existingActorId;if(-not$ids.Contains($s.existingActorId)){throw 'Existing actor is absent from observed field party.'}
  Assert-KmcOrdinaryInteger $s.existingLevel 1 19;Assert-KmcOrdinaryInteger $s.existingExperience 0 ([int]::MaxValue);Assert-KmcOrdinaryInteger $s.existingRangerLevel 0 3
  if($s.existingRangerLevel-gt$s.existingLevel){throw 'Existing Ranger ranks exceed character level.'}
  foreach($key in @('smallRaceGuid','smallPresetGuid','skeletonName','gender','leftHanded')){if($null-ne$s.$key){throw 'Existing actor selection cannot alter its body.'}}
 }elseif($s.case-ceq'paid-medium-ranger-horse'){
  foreach($key in @('existingActorId','existingLevel','existingExperience','existingRangerLevel','smallRaceGuid','smallPresetGuid','skeletonName','gender','leftHanded')){if($null-ne$s.$key){throw 'Medium hire cannot borrow legacy body or actor pins.'}}
  Assert-KmcMediumHireBodyPins $s.mediumBody $p
 }else{
  foreach($key in @('existingActorId','existingLevel','existingExperience','existingRangerLevel')){if($null-ne$s.$key){throw 'Paid hire cannot borrow an existing actor.'}}
  foreach($key in @('smallRaceGuid','smallPresetGuid')){if($s.$key-isnot[string]-or$s.$key-cnotmatch'^[a-f0-9]{32}$'){throw 'Paid body blueprint must be exact.'}}
  if($s.skeletonName-isnot[string]-or[string]::IsNullOrWhiteSpace($s.skeletonName)-or$s.skeletonName.Length-gt160-or$s.skeletonName-match'[\x00-\x1f\x7f]'-or$s.gender-isnot[string]-or$s.gender-cnotin@('Male','Female')-or$s.leftHanded-isnot[bool]){throw 'Paid body selection is malformed.'}
 }
}
function Assert-KmcChunk7ManualDerivedDescriptor($Request){
 Assert-KmcChunk7ManualDerivedSelection $Request
 if(-not(Test-KmcChunk7ManualDerivedScenario $Request.scenario)){throw 'Manual-derived descriptor belongs to its closed family only.'}
 $d=$Request.persistenceLoad;Assert-KmcExactProperties $d @('internalName','fileName','sha256','length','lastWriteTimeUtcTicks','gameId','gameName','area') 'Manual-derived immutable descriptor'
 $owner=$Request.manualDerived.nativeWriteLeaf-ceq'Manual_304_KMC_C7_MANUAL_DERIVED_FIELD.zks'
 Assert-KmcBootstrapInventoryString $d.internalName $(if($owner){'KMC C7 NO MODS OWNER SEED'}else{'KMC_C7_NATIVE_SEED'})
 Assert-KmcBootstrapInventoryString $d.fileName $(if($owner){'Manual_303_KMC_C7_NO_MODS_OWNER_SEED.zks'}else{'Manual_1_KMC_C7_NATIVE_SEED.zks'})
 Assert-KmcBootstrapInventoryString $d.sha256 $Request.manualDerived.sourceArchiveSha256
 Assert-KmcOrdinaryGuid $d.gameId
 if($d.gameName-isnot[string]-or[string]::IsNullOrWhiteSpace($d.gameName)-or$d.gameName.Length-gt256-or$d.gameName-match'[\x00-\x1f\x7f]'-or$d.area-isnot[string]-or$d.area-cnotmatch'^[a-f0-9]{32}$'-or
  $d.gameId-ceq$Request.fixture.working.gameId-or$d.sha256-ceq$Request.fixture.working.sha256-or$d.sha256-ceq$Request.fixture.baseline.sha256){throw 'Manual-derived source must preserve its separately minted native campaign.'}
 Assert-KmcOrdinaryInteger $d.length 1 268435456;Assert-KmcOrdinaryInteger $d.lastWriteTimeUtcTicks 1 ([DateTime]::MaxValue.Ticks)
}
function Assert-KmcManualDerivedSourcePins($Selection,$Observations){
 $p=$Selection.readiness;$i=$Observations.nativeProfileInventory.ordinaryAcquisitionPrerequisites
 foreach($pair in @(@('mainActorId','mainCharacterId'),@('mainLevel','mainCharacterLevel'),@('mainExperience','mainCharacterExperience'),@('money','nativeMoney'),@('nativeHireCost','nativeHireCostNow'),@('defaultHireExperience','nativeDefaultHireExperienceNow'))){
  if((ConvertTo-KmcChunk7Canonical $p.($pair[0]))-cne(ConvertTo-KmcChunk7Canonical $i.($pair[1]))){throw 'Selected readiness differs from its exact Manual native observation.'}
 }
 if((ConvertTo-KmcChunk7Canonical $p.experienceRatePercent)-cne(ConvertTo-KmcChunk7Canonical $Observations.fixtureReadiness.experienceRatePercent)){throw 'Selected native XP rate differs.'}
 [string[]]$party=@($i.nativePartyCharacterIds);[Array]::Sort($party,[StringComparer]::Ordinal)
 if((ConvertTo-KmcChunk7Canonical $p.partyCharacterIds)-cne(ConvertTo-KmcChunk7Canonical $party)){throw 'Selected original native party differs.'}
 Assert-KmcChunk7Boolean $Observations.fixtureReadiness.saveSettings.onlyOneSave $false
 Assert-KmcChunk7Boolean $Observations.fixtureReadiness.saveSettings.projectionEnabled $false
 if($Selection.case-ceq'existing-ranger-horse'){
  $a=@($Observations.afterSaveArrangement.actors|Where-Object actorId -CEQ $Selection.existingActorId);$body=@($Observations.nativeProfileInventory.party|Where-Object id -CEQ $Selection.existingActorId)
  if($a.Count-ne1-or$body.Count-ne1-or$null-ne$body[0].petId){throw 'Existing actor lacks exact pet-free Manual source observation.'}
  Assert-KmcOrdinaryInteger $a[0].characterLevel $Selection.existingLevel $Selection.existingLevel;Assert-KmcOrdinaryInteger $a[0].experience $Selection.existingExperience $Selection.existingExperience
  $r=@($a[0].classes|Where-Object classGuid -CEQ 'cda0615668a6df14eb36ba19ee881af6');$rank=if($r.Count-eq0){0}elseif($r.Count-eq1){$r[0].level}else{throw 'Duplicate Ranger class in native source.'}
  Assert-KmcOrdinaryInteger $rank $Selection.existingRangerLevel $Selection.existingRangerLevel
 }elseif($Selection.case-ceq'paid-medium-ranger-horse'){
  Assert-KmcMediumHireBodySource $Observations.nativeProfileInventory $Selection.mediumBody
 }else{
  $race=@($Observations.nativeProfileInventory.smallRaces|Where-Object raceGuid -CEQ $Selection.smallRaceGuid)
  if($race.Count-ne1){throw 'Selected Small race absent from native source.'};Assert-KmcBootstrapInventoryString $race[0].size 'Small'
  $preset=@($race[0].presets|Where-Object presetGuid -CEQ $Selection.smallPresetGuid);if($preset.Count-ne1){throw 'Selected native body preset is absent.'}
  Assert-KmcBootstrapInventoryString $preset[0].raceId $race[0].raceId;Assert-KmcChunk7Boolean $preset[0].present $true
  $skeleton=$preset[0].$(if($Selection.gender-ceq'Male'){'maleSkeleton'}else{'femaleSkeleton'});Assert-KmcChunk7Boolean $skeleton.present $true;Assert-KmcBootstrapInventoryString $skeleton.name $Selection.skeletonName
 }
}

function Assert-KmcManualDerivedFixtureReadiness($Row,$Request,$World,$Settled,$Inventory,[string]$Root,[switch]$Cumulative){
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
 $p=$Row.staticPartyFacts;$factKeys=@('selectorOpened','liveSelectorRequiredAuthorityAvailable','membershipOrRemovalQualified','characters')
 $observedScope=$Cumulative-or($Request.scenario-ceq'chunk7-manual-derived-acquisition'-and$Request.manualDerived.nativeWriteLeaf-ceq'Manual_304_KMC_C7_MANUAL_DERIVED_FIELD.zks')
 if($observedScope){$factKeys+='observationScope'};Assert-KmcExactProperties $p $factKeys 'static native party facts'
 if($observedScope){Assert-KmcBootstrapInventoryString $p.observationScope 'static-getters-only'}
 foreach($key in @('selectorOpened','liveSelectorRequiredAuthorityAvailable','membershipOrRemovalQualified')){Assert-KmcChunk7Boolean $p.$key $false}
 if($p.characters-isnot[Array]-or$p.characters.Count-lt1-or$p.characters.Count-gt6){throw 'Static native party-character facts are unbounded.'}
 $ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);$ordered=@()
 foreach($c in $p.characters){Assert-KmcExactProperties $c @('actorId','blueprintGuid','isMain','lockedCompanionComponent') 'static character fact';Assert-KmcOrdinaryGuid $c.actorId
  if(-not$ids.Add($c.actorId)){throw 'Static native actor repeated.'};$ordered+=,$c.actorId;$actor=@($World.party|Where-Object id -CEQ $c.actorId);if($actor.Count-ne1){throw 'Static native actor is foreign.'};Assert-KmcBootstrapInventoryString $c.blueprintGuid $actor[0].blueprint
  Assert-KmcChunk7Boolean $c.isMain ($c.actorId-ceq$World.mainActorId);if($c.lockedCompanionComponent-isnot[bool]){throw 'Locked-companion observation must remain a boolean fact.'}
 }
 if(-not$ids.Contains($World.mainActorId)-or(ConvertTo-KmcChunk7Canonical $ordered)-cne(ConvertTo-KmcChunk7Canonical $Inventory.ordinaryAcquisitionPrerequisites.nativePartyCharacterIds)){throw 'Static party facts differ from the actual native character roster.'}
 $d=$Row.isolatedSaveDescriptors;Assert-KmcExactProperties $d @('root','metadataOnly','maximumObservedDescriptors','descriptors') 'isolated descriptor observation';Assert-KmcBootstrapInventoryString $d.root $Root;Assert-KmcChunk7Boolean $d.metadataOnly $true;Assert-KmcOrdinaryInteger $d.maximumObservedDescriptors 32 32
 if($d.descriptors-isnot[Array]-or$d.descriptors.Count-ne1){throw 'Read-only isolated inventory must contain exactly its admitted Manual seed descriptor.'};$v=$d.descriptors[0]
 Assert-KmcExactProperties $v @('fileName','internalName','gameId','gameName','area','type','isAutoLevelupSave','isActuallySaved','hasFileOnDisk','operationState','systemSaveTimeTicks','systemSaveTimeKind') 'isolated native descriptor'
 foreach($key in @('fileName','internalName','gameId','gameName','area')){Assert-KmcBootstrapInventoryString $v.$key $Request.persistenceLoad.$key}
 Assert-KmcBootstrapInventoryString $v.type 'Manual';Assert-KmcBootstrapInventoryString $v.operationState 'None';Assert-KmcChunk7Boolean $v.hasFileOnDisk $true;Assert-KmcChunk7Boolean $v.isAutoLevelupSave $false
 # Installed getters06007FE1/06007FE2 both return nonempty FolderName; a disk-backed settled descriptor has both true.
 Assert-KmcChunk7Boolean $v.isActuallySaved $true;Assert-KmcOrdinaryInteger $v.systemSaveTimeTicks 0 ([DateTime]::MaxValue.Ticks)
 if($v.systemSaveTimeKind-isnot[string]-or$v.systemSaveTimeKind-cnotin@('Unspecified','Utc','Local')){throw 'Native save timestamp kind is malformed.'}
}
function Assert-KmcManualDerivedSnapshot($Row,$Identity,[long]$Xp,[long]$Level,[long]$Money){
 Assert-KmcOrdinaryGuid $Row.actorId
 foreach($key in @('nativeMode','phase')){if($null-ne$Row.$key-and$Row.$key-isnot[string]){throw 'Native progression phase must be a scalar token or null.'}}
 foreach($key in @('petId')){if($null-ne$Row.$key){Assert-KmcOrdinaryGuid $Row.$key}}
 foreach($key in @('selectedClass','petBlueprint')){if($null-ne$Row.PSObject.Properties[$key]-and$null-ne$Row.$key-and($Row.$key-isnot[string]-or$Row.$key-cnotmatch'^[a-f0-9]{32}$')){throw 'Native snapshot blueprint token is malformed.'}}
 Assert-KmcOrdinarySnapshot $Row $Identity $Xp $Level $Money
}
function Assert-KmcManualDerivedNativeChoices($Request,$O,$Selection=$null){
 if($null-eq$Selection){$Selection=$Request.manualDerived};$s=$Selection;$paid=Test-KmcManualDerivedPaidCase $s.case;$body=if($paid){Get-KmcManualDerivedPaidBody $s}else{$null};$original=@($O.loadedWorld.party.id)
    if($o.choices-isnot[Array]-or$o.choices.Count-lt4-or$o.choices.Count-gt1400){throw 'Bounded native ordinary events are missing.'}
    $rows=@($o.choices);$admission=$rows[0]
    foreach($row in $rows){Assert-KmcBootstrapInventoryString $row.runId $Request.runId;if($row.event-isnot[string]){throw 'Native choice event must be a scalar token.'}}
    if($paid){
        if($admission.event-cne'native-paid-hire-admission'-or$rows[1].event-cne'native-new-actor-bound'){throw 'Paid native admission/binding is missing.'}
        $explicitSpend=$admission.PSObject.Properties.Name-ccontains'spendObservationContract'
        $startingGoldSpend=$Request.productVersion-cmatch'^0\.1\.0-chunk7b-preview\.(\d+)$'-and[int]$Matches[1]-ge65
        $spendContract=if($startingGoldSpend){'native-hire-spent-v2'}else{'native-hire-spent-v1'}
        if($explicitSpend-and$admission.spendObservationContract-cne$spendContract){throw 'Native hire spend observation contract differs.'}
        $legacySpendVersion=$Request.productVersion-cin@('0.1.0-test','synthetic')-or
            ($Request.productVersion-cmatch'^0\.1\.0-chunk7b-preview\.(\d+)$'-and[int]$Matches[1]-lt63)
        if(-not$explicitSpend-and-not$legacySpendVersion){throw 'Current paid native evidence omitted its required spend observation.'}
        Assert-KmcOrdinaryInteger $admission.moneyBefore
        Assert-KmcOrdinaryInteger $admission.nativeInheritedXp 0 ([int]::MaxValue)
        Assert-KmcOrdinaryInteger $admission.nativeCost 1 ([int]::MaxValue);Assert-KmcOrdinaryInteger $admission.nativeExpectedFinalLevel 4 20
        Assert-KmcBootstrapInventoryString $admission.nativeInventorySha256 $s.sourceArtifactSha256
        if($admission.moneyBefore-lt$admission.nativeCost-or$admission.nativeInventorySha256-cne$s.sourceArtifactSha256){throw 'Native paid admission is unaffordable or unbound.'}
        $identity=$rows[1];$xp=$admission.nativeInheritedXp;$level=0;$money=$admission.moneyBefore;$expectedMoney=$money-$admission.nativeCost
        $first=2
        if($identity.actorId-cin$original-or$identity.nativeMode-cne'CharGen'){throw 'Native paid hire reused an actor or skipped CharGen.'}
    }else{
        if($admission.event-cne'native-existing-level-admission'){throw 'Existing native readiness admission missing.'}
        $identity=$admission;$xp=$identity.xp;$level=$identity.level;$money=$identity.money;$expectedMoney=$money;$first=1
        if($identity.actorId-cne$s.existingActorId-or$null-ne$identity.petId){throw 'Existing native actor or prior companion differs.'}
    }
    Assert-KmcOrdinaryGuid $identity.actorId;Assert-KmcManualDerivedSnapshot $identity $identity $xp $level $money
    Assert-KmcChunk7Boolean $identity.nativeCanLevelUp $true
    Assert-KmcOrdinaryInteger $o.beforeChoices.money $money $money
    $accepted=0;$commits=0;$pendingChoice=$null;$pendingCommit=$null;$finished=$false;$lastFrame=-1;$lastTime=-1
    $horseSelected=$false;$bondSelected=$false
    for($i=0;$i-lt$rows.Count;$i++){
        $row=$rows[$i]
        if($row.event-cin@('native-choice-requested','native-choice')){
            foreach($key in @('kind','choice')){if($row.$key-isnot[string]-or[string]::IsNullOrEmpty($row.$key)){throw 'Native requested/accepted choice token must be a nonempty scalar string.'}}
            if($null-ne$row.nativeSelectionGuid-and($row.nativeSelectionGuid-isnot[string]-or$row.nativeSelectionGuid-cnotmatch'^[a-f0-9]{32}$')){throw 'Native requested/accepted selection GUID must be an exact scalar blueprint or null.'}
        }
        Assert-KmcOrdinaryInteger $row.frame $o.loadedWorld.frame $o.settledWorld.frame
        if($row.runId-cne$Request.runId-or$row.frame-lt$lastFrame-or-not(Test-KmcChunk7FiniteNumber $row.elapsedSeconds)-or
           $row.elapsedSeconds-lt$lastTime-or$row.elapsedSeconds-gt720){throw 'Native ordinary event run/frame/time order differs.'}
        $lastFrame=$row.frame;$lastTime=$row.elapsedSeconds
        if($i-lt$first){continue};if($finished){throw 'Native ordinary choices continued after completion.'}
        switch -CaseSensitive ($row.event){
            'native-level-open' {
                if($null-ne$pendingChoice-or$null-ne$pendingCommit){throw 'Native ordinary level opened before its preceding operation settled.'}
                Assert-KmcManualDerivedSnapshot $row $identity $xp $level $money
                Assert-KmcChunk7Boolean $row.nativeCanLevelUp $true
                Assert-KmcOrdinaryInteger $row.nextLevel ($level+1) ($level+1)
            }
            'native-choice-requested' {
                if($null-ne$pendingChoice-or$null-ne$pendingCommit){throw 'Overlapping ordinary choice requests.'}
                if($row.kind-isnot[string]-or$row.kind-cnotin@('portrait','race','gender','native-race-preset','native-handedness','class','attribute-purchase','racial-attribute','level-attribute','feature','skill','name','alignment','voice','native-total-phase')-or
                   $row.choice-isnot[string]-or[string]::IsNullOrEmpty($row.choice)){throw 'Unknown native ordinary choice.'}
                Assert-KmcManualDerivedSnapshot $row.before $identity $xp $level $money;$pendingChoice=$row
            }
            'native-choice' {
                if($null-eq$pendingChoice-or$row.kind-cne$pendingChoice.kind-or$row.choice-cne$pendingChoice.choice-or$row.nativeSelectionGuid-cne$pendingChoice.nativeSelectionGuid-or
                   (ConvertTo-KmcChunk7Canonical $row.before)-cne(ConvertTo-KmcChunk7Canonical $pendingChoice.before)){throw 'Native ordinary choice lost its exact requested binding.'}
                Assert-KmcChunk7Boolean $row.nativeChoiceAccepted $true
                Assert-KmcManualDerivedSnapshot $row.before $identity $xp $level $money;Assert-KmcManualDerivedSnapshot $row.after $identity $xp $level $money
                if($row.kind-ceq'feature'){
                    if($row.nativeSelectionGuid-isnot[string]-or$row.nativeSelectionGuid-cnotmatch'^[a-f0-9]{32}$'){throw 'Native feature selection identity missing.'}
                    if($row.nativeSelectionGuid-ceq'b705c5184a96a84428eeb35ae2517a14'-and$row.choice.StartsWith('ee63330662126374e8785cc901941ac7/',[StringComparison]::Ordinal)){$bondSelected=$true}
                    if($row.nativeSelectionGuid-ceq'ee63330662126374e8785cc901941ac7'-and$row.choice.StartsWith('7db7c50677e39f09feef56f3831fc723/',[StringComparison]::Ordinal)){$horseSelected=$true}
                }elseif($null-ne$row.nativeSelectionGuid){throw 'Non-feature choice contains feature selection identity.'}
                if($row.kind-ceq'class'-and$row.choice-cne'cda0615668a6df14eb36ba19ee881af6'){throw 'Native ordinary class differs.'}
                if($paid-and$row.kind-ceq'race'-and$row.choice-cne$body.raceGuid){throw 'Native selected paid race differs.'}
                if($paid-and$row.kind-ceq'native-race-preset'-and$row.choice-cne$body.presetGuid){throw 'Native selected paid preset differs.'}
                $pendingChoice=$null;$accepted++
            }
            'native-total-next-requested' {
                if($null-ne$pendingChoice-or$null-ne$pendingCommit){throw 'Native Total overlapped unfinished choices.'}
                Assert-KmcManualDerivedSnapshot $row $identity $xp $level $money
                Assert-KmcChunk7Boolean $row.nativeCanLevelUp $true;Assert-KmcChunk7Boolean $row.nativeIsComplete $true
                if($row.phase-cne'Total'-or$row.selectedClass-cne'cda0615668a6df14eb36ba19ee881af6'){throw 'Native completion bypassed its Total/selected class gate.'}
                foreach($field in @('attributePoints','distributionPoints','skillRemaining','pendingFeatures','pendingSpellGroups')){Assert-KmcOrdinaryInteger $row.$field 0 0}
                Assert-KmcOrdinaryInteger $row.skillTotal 0 ([int]::MaxValue);Assert-KmcOrdinaryInteger $row.skillSpent $row.skillTotal $row.skillTotal
                Assert-KmcOrdinaryInteger $row.nextLevel ($level+1) ($level+1);$pendingCommit=$row
            }
            'native-level-committed' {
                if($null-eq$pendingCommit-or(ConvertTo-KmcChunk7Canonical $row.before)-cne(ConvertTo-KmcChunk7Canonical $pendingCommit)){throw 'Native level commit lacks its exact prior complete Total gate.'}
                $commits++;Assert-KmcOrdinaryInteger $row.commitCount $commits $commits
                if($paid-and-not$explicitSpend-and$money-eq$admission.moneyBefore-and$row.after.money-eq$expectedMoney){$money=$expectedMoney}
                Assert-KmcOrdinaryInteger $row.moneyExpected $money $money
                $level++;Assert-KmcManualDerivedSnapshot $row.after $identity $xp $level $money
                $pendingCommit=$null
            }
            'native-hire-spent' {
                if(-not$paid-or-not$explicitSpend-or$money-ne$admission.moneyBefore){throw 'Native hire spent more than once or outside the declared paid observation.'}
                Assert-KmcOrdinaryInteger $row.moneyBefore $admission.moneyBefore $admission.moneyBefore
                Assert-KmcOrdinaryInteger $row.nativeCost $admission.nativeCost $admission.nativeCost
                if($startingGoldSpend){
                    Assert-KmcOrdinaryInteger $row.nativeStartingGold 0 ([int]::MaxValue)
                    $expectedMoney=[long](([decimal]$admission.moneyBefore)+[decimal]$row.nativeStartingGold-[decimal]$admission.nativeCost)
                }elseif($null-ne$row.PSObject.Properties['nativeStartingGold']){throw 'Historical hire spend cannot claim a new native gold grant.'}
                Assert-KmcOrdinaryInteger $row.moneyAfter $expectedMoney $expectedMoney
                $money=$expectedMoney
            }
            'native-builder-complete' {
                if($null-ne$pendingChoice-or$null-ne$pendingCommit-or$commits-lt1){throw 'Ordinary builder completion precedes committed choices.'}
                if($paid-and$money-ne$expectedMoney){throw 'Paid native builder completed before exact hire expenditure.'}
                Assert-KmcManualDerivedSnapshot $row.actor $identity $xp $level $money
                Assert-KmcChunk7Boolean $row.actor.nativeCanLevelUp $false;Assert-KmcChunk7Boolean $row.ordinaryChoicesComplete $true
                foreach($field in @('nativeFieldAdmissionProven','nativeSaveSettlementProven','poseQualified')){Assert-KmcChunk7Boolean $row.$field $false}
                if((ConvertTo-KmcChunk7Canonical $row.actor)-cne(ConvertTo-KmcChunk7Canonical $o.builderComplete)){throw 'Ordinary terminal actor snapshot differs.'}
                if($paid-and$level-ne$admission.nativeExpectedFinalLevel){throw 'Native hire reached a level not supported by its inherited XP.'}
                Assert-KmcOrdinaryInteger $row.actor.rangerLevel 4 20
                Assert-KmcOrdinaryGuid $row.actor.petId
                if($row.actor.petBlueprint-cne'4016c7db400ab721ff125aef9e65e202') {throw 'Ordinary native pet differs.'}
                $finished=$true
            }
            default {throw 'Unexpected ordinary native event.'}
        }
    }
    if(-not$finished-or-not$horseSelected-or-not$bondSelected){throw 'Native ordinary completion/companion selection chain is incomplete.'}
    Assert-KmcOrdinaryInteger $o.acceptedChoiceCount $accepted $accepted;Assert-KmcOrdinaryInteger $o.nativeCommitCount $commits $commits
    if($accepted-lt2-or$accepted-gt512-or$commits-gt20){throw 'Ordinary native choice bounds differ.'}
    if($Request.scenario-ceq'chunk7-manual-derived-acquisition'-or$Request.scenario-ceq'chunk7-cumulative-acquisition'){
     if($null-ne$o.PSObject.Properties['nativeAcquisition']){throw 'Manual acquisition cannot claim an unregistered level-up Auto observer.'}
     if($paid){Assert-KmcOrdinaryInteger $admission.moneyBefore $s.readiness.money $s.readiness.money;Assert-KmcOrdinaryInteger $admission.nativeCost $s.readiness.nativeHireCost $s.readiness.nativeHireCost;Assert-KmcOrdinaryInteger $xp $s.readiness.defaultHireExperience $s.readiness.defaultHireExperience}
     else{Assert-KmcOrdinaryInteger $identity.level $s.existingLevel $s.existingLevel;Assert-KmcOrdinaryInteger $xp $s.existingExperience $s.existingExperience;Assert-KmcOrdinaryInteger $identity.rangerLevel $s.existingRangerLevel $s.existingRangerLevel;Assert-KmcOrdinaryInteger $expectedMoney $s.readiness.money $s.readiness.money}
     return [pscustomobject]@{actorId=$identity.actorId;actorObject=$identity.actorObject;descriptorObject=$identity.descriptorObject;petId=$O.builderComplete.petId;level=$level;xp=$xp;money=$expectedMoney;commits=$commits}
    }
    $probe=$o.nativeAcquisition
    foreach($field in @('installationAttempted','installed','complete')){Assert-KmcChunk7Boolean $probe.$field $true}
    if($probe.errors-isnot[Array]-or$probe.errors.Count-ne0-or$probe.hooks-isnot[Array]-or$probe.hooks.Count-ne3-or$probe.events-isnot[Array]-or$probe.events.Count-ne(2*$commits+2)){throw 'Native ordinary observer is incomplete.'}
    $contracts=@(@('Kingmaker.Controllers.LevelUpAutoSaveController','HandleLevelUpComplete','0600906B'),@('Kingmaker.Game','SaveGame','06000CE4'),@('Kingmaker.EntitySystem.Persistence.SaveManager','PrepareSave','06008025'))
    for($i=0;$i-lt3;$i++){$h=$probe.hooks[$i];foreach($key in @('type','method','token','moduleMvid')){if($h.$key-isnot[string]){throw 'Native hook identity must be scalar.'}};if($h.type-cne$contracts[$i][0]-or$h.method-cne$contracts[$i][1]-or$h.token-cne$contracts[$i][2]-or$h.moduleMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'){throw 'Exact native ordinary hook contract differs.'}}
    Assert-KmcOrdinaryInteger $probe.completionEntries $commits $commits;Assert-KmcOrdinaryInteger $probe.completionExits $commits $commits
    Assert-KmcOrdinaryInteger $probe.saveRequests 1 1;Assert-KmcOrdinaryInteger $probe.preparedWrites 1 1
    $inside=$null;$entries=0;$exits=0;$requests=0;$writes=0;$saveObject=0;$lastFrame=-1
    foreach($row in $probe.events){
        Assert-KmcOrdinaryGuid $row.actorId;if($row.kind-isnot[string]){throw 'Native callback kind must be a scalar token.'}
        foreach($key in @('completionActorId','saveType','saveLeaf')){if($null-ne$row.PSObject.Properties[$key]-and$null-ne$row.$key-and$row.$key-isnot[string]){throw 'Native callback identity must be scalar.'}}
        Assert-KmcOrdinaryInteger $row.frame 0 ([int]::MaxValue)
        Assert-KmcOrdinaryInteger $row.actorObject ([int]::MinValue) ([int]::MaxValue)
        if($row.actorId-cne$identity.actorId-or$row.actorObject-ne$identity.actorObject-or$row.frame-lt$lastFrame){throw 'Native autosave observer actor/frame binding differs.'};$lastFrame=$row.frame
        switch -CaseSensitive ($row.kind){
            'native-level-completion-enter' {
                if($null-ne$inside){throw 'Overlapping native completion callbacks.'};$entries++;$inside=$row
                Assert-KmcChunk7Boolean $row.insideObservedCompletion $true
                Assert-KmcChunk7Boolean $row.isChargen ($entries-lt$commits)
                Assert-KmcOrdinaryInteger $row.characterLevel ($identity.level+$entries) ($identity.level+$entries)
            }
            'native-level-autosave-request' {
                if($null-eq$inside-or$entries-ne$commits-or$row.completionActorId-cne$identity.actorId-or$row.frame-ne$inside.frame){throw 'Native autosave did not originate inside the exact terminal completion.'}
                Assert-KmcChunk7Boolean $row.insideObservedCompletion $true;Assert-KmcChunk7Boolean $row.isChargen $false;Assert-KmcChunk7Boolean $row.inState $true
                Assert-KmcChunk7Boolean $row.autoLevelup $true;if($row.saveType-cne'Auto'){throw 'Native ordinary completion requested a different save category.'}
                $requests++;Assert-KmcOrdinaryInteger $row.saveObject ([int]::MinValue) ([int]::MaxValue)
                if($row.saveObject-eq0){throw 'Native autosave object identity missing.'};$saveObject=$row.saveObject
            }
            'native-level-autosave-prepared' {
                Assert-KmcOrdinaryInteger $row.saveObject ([int]::MinValue) ([int]::MaxValue)
                if($row.saveObject-eq0){throw 'Native prepared object identity missing.'}
                $writes++;if($requests-ne1-or$row.saveObject-eq$saveObject-or$row.saveLeaf-cne'Auto_1.zks'){throw 'Native PrepareSave lost exact descriptor identity.'}
                Assert-KmcChunk7Boolean $row.autoLevelup $true;if($row.saveType-cne'Auto'){throw 'Native prepared category differs.'}
            }
            'native-level-completion-exit' {
                if($null-eq$inside-or$row.frame-ne$inside.frame-or$row.completionActorId-cne$identity.actorId){throw 'Native completion exit lost exact callback custody.'}
                Assert-KmcChunk7Boolean $row.isChargen $inside.isChargen
                Assert-KmcChunk7Boolean $row.insideObservedCompletion $true;$inside=$null;$exits++
            }
            default {throw 'Unknown native autosave observer event.'}
        }
    }
    if($null-ne$inside-or$entries-ne$commits-or$exits-ne$commits-or$requests-ne1-or$writes-ne1){throw 'Native completion/autosave events do not settle exactly once.'}
 Assert-KmcOrdinarySaveDescriptorBinding $probe $o.nativeWrite
 Assert-KmcOrdinaryInteger $o.nativeWrite.completedWritesBefore 0 0
 Assert-KmcOrdinaryInteger $o.nativeWrite.completionSequence 1 1
 if($paid){Assert-KmcOrdinaryInteger $admission.moneyBefore $s.readiness.money $s.readiness.money;Assert-KmcOrdinaryInteger $admission.nativeCost $s.readiness.nativeHireCost $s.readiness.nativeHireCost;Assert-KmcOrdinaryInteger $xp $s.readiness.defaultHireExperience $s.readiness.defaultHireExperience}
 else{Assert-KmcOrdinaryInteger $identity.level $s.existingLevel $s.existingLevel;Assert-KmcOrdinaryInteger $xp $s.existingExperience $s.existingExperience;Assert-KmcOrdinaryInteger $identity.rangerLevel $s.existingRangerLevel $s.existingRangerLevel;Assert-KmcOrdinaryInteger $expectedMoney $s.readiness.money $s.readiness.money}
 [pscustomobject]@{actorId=$identity.actorId;actorObject=$identity.actorObject;descriptorObject=$identity.descriptorObject;petId=$O.builderComplete.petId;level=$level;xp=$xp;money=$expectedMoney;commits=$commits}
}
function Assert-KmcChunk7ManualDerivedEvidence($Request,$Artifact,$SourceArtifact,[string]$Status='PASS'){
 Assert-KmcChunk7ManualDerivedDescriptor $Request;Assert-KmcChunk7ArtifactEnvelope $Request $Artifact 'chunk7-manual-derived-evidence'
 if($Status-ceq'PASS'-and$Artifact.status-cne'PASS'){throw 'PASS Manual-derived result needs its PASS native artifact.'};if($Artifact.status-cne'PASS'){return}
 if($Request.manualDerived.nativeWriteLeaf-ceq'Manual_304_KMC_C7_MANUAL_DERIVED_FIELD.zks'){
  Assert-KmcBootstrapInventoryString $SourceArtifact.scenario 'chunk7-assisted-cold-inventory';Assert-KmcBootstrapInventoryString $SourceArtifact.evidenceKind 'chunk7-assisted-cold-inventory-evidence'
 }else{Assert-KmcBootstrapInventoryString $SourceArtifact.scenario 'chunk7-bootstrap-manual-seed';Assert-KmcBootstrapInventoryString $SourceArtifact.evidenceKind 'chunk7-manual-seed-evidence'}
 Assert-KmcNativeAcquisitionProof $Request $Artifact $SourceArtifact $Request.manualDerived
}
# Shared raw native observation proof only. Each closed caller validates its own
# exact descriptor, artifact kind and distinct source family before entering.
function Assert-KmcNativeAcquisitionProof($Request,$Artifact,$SourceArtifact,$Selection,[switch]$Cumulative,$AssistedProjection=$null){
 $o=$Artifact.observations;$s=$Selection;$source=$SourceArtifact;$paid=Test-KmcManualDerivedPaidCase $s.case
 $sourceValues=$source.observations;$sourceWrite=$null;$sourceWorld=$null;$ownerSeed=$s.nativeWriteLeaf-ceq'Manual_304_KMC_C7_MANUAL_DERIVED_FIELD.zks'
 if($ownerSeed){
  if($Cumulative-or$null-ne$AssistedProjection){throw 'Owner-seed cold provenance is not cumulative.'}
  Assert-KmcBootstrapInventoryString $sourceValues.selection.case 'owner-seed-readonly-inventory'
  Assert-KmcBootstrapInventoryString $sourceValues.selection.seedId 'KMC_C7_NO_MODS_OWNER_SEED'
  Assert-KmcBootstrapInventoryString $sourceValues.selection.seedSha256 $s.sourceArchiveSha256
  Assert-KmcBootstrapInventoryString $sourceValues.seedSource.archiveLeaf $Request.persistenceLoad.fileName
  Assert-KmcChunk7Boolean $sourceValues.seedSource.ownerAuthorized $true;Assert-KmcChunk7Boolean $sourceValues.seedSource.producerAncestry $false
  Assert-KmcChunk7Boolean $sourceValues.ownerSeedQualification.qualified $true
  Assert-KmcBootstrapInventoryString $sourceValues.kmcMetadata.kind 'Missing';Assert-KmcChunk7Boolean $sourceValues.kmcMetadata.loadedArchivePresent $false
  $sourceWorld=$sourceValues.loadedWorld;$sourceWrite=$Request.persistenceLoad
  $sourceValues=Get-KmcChunk7OwnerSeedAcquisitionProjection $source.observations
 }elseif($null-eq$AssistedProjection){$sourceWrite=$sourceValues.nativeWrite;$sourceWorld=$sourceValues.savedWorld}
 if($null-ne$AssistedProjection){
  if(-not$Cumulative-or-not(Test-KmcAssistedCumulativeMode $s)){throw 'An assisted projection belongs only to the closed cumulative source mode.'}
  $sourceValues=$AssistedProjection;$sourceWrite=$source.observations.finalManual;$sourceWorld=$source.observations.settledWorld
 }
 if(-not(Test-KmcFiniteJsonNumber $o.elapsedSeconds)-or$o.elapsedSeconds-lt0-or$o.elapsedSeconds-gt1050){throw 'Manual-derived elapsed observation exceeds its declared native bound.'}
 foreach($key in @('commit','dllSha256','dllMvid')){Assert-KmcBootstrapInventoryString $source.$key $Request.$key}
 Assert-KmcBootstrapInventoryString $source.runId $s.sourceRunId;Assert-KmcBootstrapInventoryString $source.status 'PASS'
 if($source.errors-isnot[Array]-or$source.errors.Count-ne0){throw 'Manual source failed.'}
 foreach($key in @('gameId','gameName','area')){Assert-KmcBootstrapInventoryString $sourceWrite.$key $Request.persistenceLoad.$key}
 Assert-KmcBootstrapInventoryString $sourceWrite.sha256 $s.sourceArchiveSha256
 if((ConvertTo-KmcChunk7Canonical $o.sourceManualObservations)-cne(ConvertTo-KmcChunk7Canonical $source.observations)-or(ConvertTo-KmcChunk7Canonical $o.sourceNativeWorld)-cne(ConvertTo-KmcChunk7Canonical $sourceWorld)){throw 'Manual-derived copied source observations differ.'}
 if((ConvertTo-KmcChunk7Canonical $o.selection)-cne(ConvertTo-KmcChunk7Canonical $s)){throw 'Manual-derived observed selection changed.'}
 Assert-KmcManualDerivedSourcePins $s $sourceValues
 Assert-KmcOrdinaryInteger $o.processId 1 ([int]::MaxValue);Assert-KmcOrdinaryInteger $source.observations.processId 1 ([int]::MaxValue);if($o.processId-eq$source.observations.processId){throw 'Manual-derived native process was not cold.'}
 foreach($key in @('resourceWrites','xpWrites','moneyWrites')){Assert-KmcOrdinaryInteger $o.$key 0 0}
 foreach($key in @('profileAdmissionChanged','sixPairFixtureQualified','vendorDialogueProven','mountedCombatProven','smallPoseQualified')){Assert-KmcChunk7Boolean $o.$key $false};Assert-KmcChunk7Boolean $o.ordinaryAcquisitionProven $true
 foreach($key in @('runId','artifactSha256','archiveSha256')){$selectionKey=if($key-ceq'runId'){'sourceRunId'}elseif($key-ceq'artifactSha256'){'sourceArtifactSha256'}else{'sourceArchiveSha256'};Assert-KmcBootstrapInventoryString $o.source.$key $s.$selectionKey}
 Assert-KmcBootstrapInventoryString $o.source.archiveLeaf $Request.persistenceLoad.fileName;Assert-KmcChunk7Boolean $o.source.sameCandidate $true;Assert-KmcChunk7Boolean $o.source.diagnosticArchiveContentRead $false
 $b=$o.beforeLoad;$w=$o.loadedWorld;$f=$o.settledWorld;$load=$o.nativeLoad;$registry=$load.identities
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
 Assert-KmcManualSeedWorld $f $Request $labels $o.sourceNativeWorld $w.frame 1
 Assert-KmcChunk7ManualSeedAuthority $w.authority $root $b.authority.generation 1 0
 foreach($row in @($f)){Assert-KmcChunk7ManualSeedAuthority $row.authority $root $b.authority.generation 1 1}
 foreach($row in @($w,$f)){if($row.game-ne$b.game-or$row.saveManager-ne$b.saveManager){throw 'Manual seed replaced the exact Game or SaveManager.'}}
 foreach($key in @('game','saveManager','player','state','areaState','mainState','mainActorId','party')){if((ConvertTo-KmcChunk7Canonical $f.$key)-cne(ConvertTo-KmcChunk7Canonical $w.$key)){throw 'Final cold world or actors changed before cleanup.'}}
 $prior=$b.frame
 for($i=0;$i-lt2;$i++){$e=$load.events[$i];$kind=if($i-eq0){'native-player-postload-before'}else{'native-player-postload-after'};Assert-KmcBootstrapInventoryString $e.kind $kind;Assert-KmcOrdinaryInteger $e.frame $prior $w.frame;$prior=$e.frame
  foreach($key in @('game','saveManager','player')){Assert-KmcManualSeedObjectIdentity $e.$key;if($e.$key-ne$w.$key){throw 'Native PostLoad callback owner was substituted.'}}
  # The admitted owner seed reports a null Player.GameId in the synchronous
  # PostLoad callbacks, then the exact archive GameId in the settled world.
  # A populated callback ID must still match the loaded archive.
  if($null-ne$e.gameId){Assert-KmcBootstrapInventoryString $e.gameId $Request.persistenceLoad.gameId}
  elseif(-not$ownerSeed-and$null-eq$AssistedProjection){throw 'Bootstrap Manual PostLoad lost its required GameId.'}
  if($i-eq1){Assert-KmcBootstrapInventoryString $e.mainActorId $w.mainActorId}else{if($null-ne$e.mainActorId){Assert-KmcBootstrapInventoryString $e.mainActorId $w.mainActorId}}
 }
 if($load.events[0].frame-ne$load.events[1].frame){throw 'Native PostLoad did not return in its exact synchronous callback.'}
 $p=$o.noPreparation;Assert-KmcBootstrapInventoryString $p.contract 'chunk7-native-restoration-entry-observer-v1';Assert-KmcBootstrapInventoryString $p.prepareMetric 'native-method-entries-including-production-refused-attempts'
 foreach($key in @('complete','installedBeforeWorldLoad')){Assert-KmcChunk7Boolean $p.$key $true};Assert-KmcOrdinaryInteger $p.maximumEvents 2048 2048
 foreach($key in @('dropped','errors')){Assert-KmcOrdinaryInteger $p.$key 0 0}
 if($p.events-isnot[Array]-or$p.events.Count-ne0-or$p.hooks-isnot[Array]-or$p.hooks.Count-ne4){throw 'Cold inventory observed native preparation/round or actor/debt restoration.'}
 $tokens=@('06000C3C','0600939D');$methods=@('Prepare','OnNewRound','RestoreActor','RestoreDebt');$prefixes=@('PrepareBefore','RoundBefore','RestoreActorBefore','RestoreDebtBefore');$postfixes=@('PrepareAfter','RoundAfter','RestoreActorAfter','RestoreDebtAfter')
 for($i=0;$i-lt4;$i++){$h=$p.hooks[$i];Assert-KmcBootstrapInventoryString $h.method $methods[$i];Assert-KmcBootstrapInventoryString $h.prefix $prefixes[$i];Assert-KmcBootstrapInventoryString $h.postfix $postfixes[$i]
   if($i-lt2){$type=if($i-eq0){'TurnBased.Controllers.TurnController'}else{'Kingmaker.Controllers.Combat.UnitCombatState'};Assert-KmcBootstrapInventoryString $h.declaringType $type;Assert-KmcBootstrapInventoryString $h.token $tokens[$i];Assert-KmcBootstrapInventoryString $h.moduleMvid '07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'}else{Assert-KmcBootstrapInventoryString $h.moduleMvid $Request.dllMvid;Assert-KmcBootstrapInventoryString $h.declaringType 'KingmakerMountedCombat.Integration.NativeCombatActorPersistence';if($h.token-isnot[string]-or$h.token-cnotmatch'^06[0-9A-F]{6}$'){throw 'Product restore hook token invalid.'}}
 }
 $a=$o.archive
 if($ownerSeed){Assert-KmcBootstrapInventoryString $a.kind 'Missing';foreach($key in @('loadedArchivePresent','nativeRestorationGrantedByDiagnostic')){Assert-KmcChunk7Boolean $a.$key $false}}
 else{Assert-KmcOrdinaryInteger $a.schemaVersion 3 3;foreach($pair in @(@('campaignId','gameId'),@('areaId','area'))){Assert-KmcBootstrapInventoryString $a.($pair[0]) $Request.persistenceLoad.($pair[1])}
  foreach($key in @('pairs','participations','slots')){Assert-KmcOrdinaryInteger $a.$key 0 0};foreach($key in @('combatPresent','nativeRestorationGrantedByDiagnostic')){Assert-KmcChunk7Boolean $a.$key $false}}
 $inventory=$o.nativeProfileInventory;Assert-KmcOrdinaryInteger $inventory.schemaVersion 1 1;Assert-KmcBootstrapInventoryString $inventory.captureStatus 'PASS';Assert-KmcBootstrapInventoryString $inventory.captureKind 'installed-native-small-body-catalogue';Assert-KmcBootstrapInventoryString $inventory.gameplayAssemblyMvid '07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'
 foreach($key in @('profileAdmissionChanged','nativeFixtureCreated','poseQualified','ordinaryAcquisitionProven')){Assert-KmcChunk7Boolean $inventory.$key $false}
 if($inventory.party-isnot[Array]-or$inventory.party.Count-ne$w.party.Count){throw 'Read-only native profile inventory lost its exact cold actors.'}
 foreach($actor in $w.party){$row=@($inventory.party|Where-Object id -CEQ $actor.id);if($row.Count-ne1){throw 'Profile inventory actor changed.'};Assert-KmcBootstrapInventoryString $row[0].id $actor.id;Assert-KmcBootstrapInventoryString $row[0].blueprintGuid $actor.blueprint
  $l=$row[0].nativeLevelUpReadiness;Assert-KmcOrdinaryInteger $l.characterLevel 0 20;Assert-KmcOrdinaryInteger $l.experience 0 ([int]::MaxValue);if($null-ne$l.nextLevelExperience){Assert-KmcOrdinaryInteger $l.nextLevelExperience 0 ([int]::MaxValue)}
  foreach($key in @('canLevelUp','isDead')){if($l.$key-isnot[bool]){throw 'Native readiness flag invalid.'}};foreach($key in @('partyInCombat','controllerCreated','completeChoicesObserved','ordinaryAcquisitionProven')){Assert-KmcChunk7Boolean $l.$key $false}
 }
 $r=$inventory.ordinaryAcquisitionPrerequisites;Assert-KmcChunk7Boolean $r.observationOnly $true;foreach($key in @('purchaseAuthorized','sixPairFixtureQualified','defaultHireMatchesPlayerExperienceExactly')){Assert-KmcChunk7Boolean $r.$key $false}
 Assert-KmcOrdinaryInteger $r.nativeMoney 0 ([long]::MaxValue);Assert-KmcOrdinaryInteger $r.nativeHireCostNow 1 ([int]::MaxValue)
 foreach($key in @('mainCharacterExperience','nativeDefaultHireExperienceNow')){Assert-KmcOrdinaryInteger $r.$key 0 ([int]::MaxValue)}
 foreach($key in @('nativePartyLevel','mainCharacterLevel')){Assert-KmcOrdinaryInteger $r.$key 0 20};Assert-KmcOrdinaryInteger $r.nativeFieldCharacterLimit 6 6;Assert-KmcBootstrapInventoryString $r.mainCharacterId $w.mainActorId;Assert-KmcChunk7Boolean $r.nativeAffordableNow ($r.nativeMoney-ge$r.nativeHireCostNow)
 if($r.nativePartyCharacterIds-isnot[Array]-or$r.nativePartyCharacterIds.Count-lt1-or$r.nativePartyCharacterIds.Count-gt6){throw 'Native field character capacity observation invalid.'}
 $characters=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);foreach($id in $r.nativePartyCharacterIds){Assert-KmcOrdinaryGuid $id;if(-not$characters.Add($id)-or$id-cnotin@($w.party.id)){throw 'Native character inventory is duplicate or foreign.'}}
 $main=@($inventory.party|Where-Object id -CEQ $w.mainActorId)[0].nativeLevelUpReadiness
 if($r.mainCharacterLevel-ne$main.characterLevel-or$r.mainCharacterExperience-ne$main.experience){throw 'Native main readiness observations disagree.'}
 Assert-KmcManualDerivedFixtureReadiness $o.fixtureReadiness $Request $w $f $inventory $root -Cumulative:$Cumulative
 foreach($key in @('mainProgression','staticPartyFacts','saveSettings','experienceRatePercent')){if((ConvertTo-KmcChunk7Canonical $o.fixtureReadiness.$key)-cne(ConvertTo-KmcChunk7Canonical $sourceValues.fixtureReadiness.$key)){throw 'Cold readiness differs from Manual source.'}}
 if((ConvertTo-KmcChunk7Canonical $o.nativeProfileInventory.ordinaryAcquisitionPrerequisites)-cne(ConvertTo-KmcChunk7Canonical $sourceValues.nativeProfileInventory.ordinaryAcquisitionPrerequisites)){throw 'Native readiness differs from Manual source.'}
 if((ConvertTo-KmcChunk7Canonical $o.beforeChoices)-cne(ConvertTo-KmcChunk7Canonical $sourceValues.afterSaveArrangement)){throw 'Native arrangement changed before choices.'}
 if($s.case-ceq'paid-medium-ranger-horse'){Assert-KmcMediumHireBodySource $o.nativeProfileInventory $s.mediumBody}
 Assert-KmcManualDerivedEligibility $s $o.liveEligibility
 $choice=Assert-KmcManualDerivedNativeChoices $Request $o $s
 Assert-KmcManualDerivedOutcome $Request $o $choice $labels $root $s
 foreach($key in @('custodySettled','nativeWorkerSettled','observersReleased','worldAdmitted','sourcePinned','sourceUnchanged','selectionRestored','pauseRestored','slotProjectionRestored','acquiredNativeStateRetained','ummInstanceRestored','ummOpenStateRestored')){Assert-KmcChunk7Boolean $o.cleanup.$key $true}
 foreach($key in @('manualRefunds','manualFactRemovals','manualCompanionRemovals')){Assert-KmcOrdinaryInteger $o.cleanup.$key 0 0}
 Assert-KmcChunk7ManualSeedAuthority $o.cleanup.authority $root $b.authority.generation 1 1
}
function Assert-KmcManualDerivedEligibility($Selection,$E){
 $s=$Selection;$p=$s.readiness
 foreach($key in @('mainActorId','mainLevel','mainExperience','money','experienceRatePercent','nativeHireCost','defaultHireExperience')){if((ConvertTo-KmcChunk7Canonical $E.$key)-cne(ConvertTo-KmcChunk7Canonical $p.$key)){throw 'Live native eligibility differs from exact selected source pin.'}}
 if($E.partyCharacterIds-isnot[Array]){throw 'Live party eligibility is missing.'};$ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
 foreach($id in $E.partyCharacterIds){Assert-KmcOrdinaryGuid $id;if(-not$ids.Add($id)){throw 'Live party eligibility repeats actor.'}}
 [string[]]$ordered=@($E.partyCharacterIds);[Array]::Sort($ordered,[StringComparer]::Ordinal)
 if((ConvertTo-KmcChunk7Canonical $ordered)-cne(ConvertTo-KmcChunk7Canonical $p.partyCharacterIds)){throw 'Live eligibility party differs.'}
 $table=$E.nativeXpThresholds;if($table-isnot[Array]-or$table.Count-ne21){throw 'Native XP table observation must contain exactly bounded indices zero to twenty.'};$last=0
 foreach($xp in $table){Assert-KmcOrdinaryInteger $xp $last ([int]::MaxValue);$last=$xp}
 Assert-KmcOrdinaryInteger $E.defaultHireExperience $table[$p.mainLevel] $table[$p.mainLevel]
 Assert-KmcChunk7Boolean $E.arrangementGranted $false
 $ready=0;$xp=if(Test-KmcManualDerivedPaidCase $s.case){$p.defaultHireExperience}else{$s.existingExperience}
 while($ready-lt20-and$table[$ready+1]-le$xp){$ready++}
 if(Test-KmcManualDerivedPaidCase $s.case){
  if($s.case-ceq'paid-medium-ranger-horse'){Assert-KmcChunk7Boolean $E.exactMediumBodyMember $true}else{Assert-KmcChunk7Boolean $E.exactSmallCatalogueMember $true}
  if($p.mainLevel-lt4-or$ready-lt4-or$p.money-lt$p.nativeHireCost-or$p.partyCharacterIds.Count-ge6){throw 'Native paid readiness lacks attained level/funds/slot.'}
  if($null-ne$E.existingActorId){throw 'Paid eligibility borrows an existing actor.'}
 }else{
  foreach($key in @('existingActorId','existingLevel','existingExperience','existingRangerLevel')){if((ConvertTo-KmcChunk7Canonical $E.$key)-cne(ConvertTo-KmcChunk7Canonical $s.$key)){throw 'Existing native eligibility changed.'}}
  foreach($key in @('targetLiving','targetInField','targetIdle','targetCanLevelUp')){Assert-KmcChunk7Boolean $E.$key $true}
  foreach($key in @('targetIsPet','targetHasPet','targetHasDualCompanion')){Assert-KmcChunk7Boolean $E.$key $false}
  if($s.existingRangerLevel+$ready-$s.existingLevel-lt4){throw 'Actual native XP cannot reach Ranger four through ordinary choices.'}
 }
}
function Assert-KmcManualDerivedNativeSave($Request,$O,$Labels,[string]$Root){
 $selection=if($Request.scenario-ceq'chunk7-cumulative-acquisition'){$Request.cumulativeAcquisition}else{$Request.manualDerived}
 $leaf=$selection.nativeWriteLeaf;$name='KMC_C7_MANUAL_DERIVED_FIELD';$path=Join-Path $Root $leaf
 $n=$O.nativeNumbering;$r=$O.saveRequested;$p=$O.nativeSave;$w=$O.nativeWrite;$field=$O.field
 foreach($pair in @(@('method','FindUnusedSaveNumber'),@('token','06008024'),@('moduleMvid','07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'),@('nativeSaveType','Manual'),@('fileName',$leaf))){Assert-KmcBootstrapInventoryString $n.($pair[0]) $pair[1]}
 $expectedNumber=if($leaf-ceq'Manual_301_KMC_C7_MANUAL_DERIVED_FIELD.zks'){301}elseif($leaf-ceq'Manual_303_KMC_C7_MANUAL_DERIVED_FIELD.zks'){303}elseif($leaf-ceq'Manual_304_KMC_C7_MANUAL_DERIVED_FIELD.zks'){304}elseif($leaf-ceq'Manual_306_KMC_C7_MANUAL_DERIVED_FIELD.zks'){306}else{2}
 Assert-KmcOrdinaryInteger $n.number $expectedNumber $expectedNumber;Assert-KmcOrdinaryInteger $n.frame $field.frame $O.settledWorld.frame
 foreach($key in @('outputInitiallyAbsent')){Assert-KmcChunk7Boolean $n.$key $true};Assert-KmcChunk7Boolean $n.slotProjectionEnabled $false
 if((ConvertTo-KmcChunk7Canonical $n.sourceDescriptors)-cne(ConvertTo-KmcChunk7Canonical $O.fixtureReadiness.isolatedSaveDescriptors.descriptors)){throw 'Manual numbering changed its immutable source inventory.'}
 Assert-KmcOrdinaryInteger $r.frame $n.frame $O.settledWorld.frame;Assert-KmcManualSeedObjectIdentity $r.descriptor
 Assert-KmcBootstrapInventoryString $r.name $name;Assert-KmcBootstrapInventoryString $r.type 'Manual'
 foreach($key in @('nativeSaveAllowed','newSlot')){Assert-KmcChunk7Boolean $r.$key $true};Assert-KmcOrdinaryInteger $r.snapshotCount 0 0
 if($r.authority.root-isnot[string]-or$r.authority.root-cne$Root){throw ('Manual request authority root differs: '+($r.authority.root|ConvertTo-Json -Compress)+' versus '+($Root|ConvertTo-Json -Compress))}
 Assert-KmcChunk7ManualSeedAuthority $r.authority $Root $O.beforeLoad.authority.generation 1 0
 Assert-KmcBootstrapInventoryString $p.contract 'manual-seed-native-save-v1'
 foreach($key in @('installed','installationAttempted','complete')){Assert-KmcChunk7Boolean $p.$key $true}
 foreach($key in @('requests','prepares','starts','stages','completions','callbacks')){Assert-KmcOrdinaryInteger $p.$key 1 1}
 if($p.errors-isnot[Array]-or$p.errors.Count-ne0-or$p.events-isnot[Array]-or$p.events.Count-ne6-or$p.hooks-isnot[Array]-or$p.hooks.Count-ne3){throw 'Manual save observer is incomplete.'}
 $methods=@('SaveRoutine','PrepareSave','RecordCompletedSave');$tokens=@('06008029','06008025')
 for($i=0;$i-lt3;$i++){$h=$p.hooks[$i];Assert-KmcBootstrapInventoryString $h.method $methods[$i]
  if($i-lt2){Assert-KmcBootstrapInventoryString $h.type 'Kingmaker.EntitySystem.Persistence.SaveManager';Assert-KmcBootstrapInventoryString $h.token $tokens[$i];Assert-KmcBootstrapInventoryString $h.moduleMvid '07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'}
  else{Assert-KmcBootstrapInventoryString $h.type 'KingmakerMountedCombat.Integration.MountedPersistenceService';Assert-KmcBootstrapInventoryString $h.moduleMvid $Request.dllMvid;if($h.token-isnot[string]-or$h.token-cnotmatch'^06[0-9A-F]{6}$'){throw 'Manual completion hook token invalid.'}}
 }
 $kinds=@('native-save-request','native-save-prepared','snapshot-starting','snapshot-staged','native-worker-completed','native-save-callback');$prior=$r.frame
 for($i=0;$i-lt6;$i++){$e=$p.events[$i];Assert-KmcBootstrapInventoryString $e.kind $kinds[$i];Assert-KmcOrdinaryInteger $e.sequence ($i+1) ($i+1);Assert-KmcOrdinaryInteger $e.frame $prior $O.settledWorld.frame;$prior=$e.frame
  foreach($key in @('game','saveManager','player')){Assert-KmcOrdinaryInteger $e.$key $O.loadedWorld.$key $O.loadedWorld.$key}
  Assert-KmcOrdinaryInteger $e.requested $r.descriptor $r.descriptor
  $descriptor=if($i-eq0){$r.descriptor}else{$w.writtenDescriptor};Assert-KmcOrdinaryInteger $e.descriptor $descriptor $descriptor
  Assert-KmcBootstrapInventoryString $e.internalName $name;Assert-KmcBootstrapInventoryString $e.type 'Manual';Assert-KmcBootstrapInventoryString $e.gameName $Request.persistenceLoad.gameName;Assert-KmcBootstrapInventoryString $e.area $Request.persistenceLoad.area
  if($i-eq0){foreach($key in @('fileName','gameId','path')){if($null-ne$e.$key){throw 'Fresh Manual request was already prepared.'}}}
  else{Assert-KmcBootstrapInventoryString $e.fileName $leaf;Assert-KmcBootstrapInventoryString $e.path $path;Assert-KmcBootstrapInventoryString $e.gameId $Request.persistenceLoad.gameId}
  $snapshots=if($i-lt3){0}else{1};$completed=if($i-lt5){0}else{1};Assert-KmcOrdinaryInteger $e.snapshotCount $snapshots $snapshots;Assert-KmcOrdinaryInteger $e.completedSaveCount $completed $completed
 }
 $worker=$p.events[4];Assert-KmcBootstrapInventoryString $worker.iteratorType 'Kingmaker.EntitySystem.Persistence.SaveManager+<SaveRoutine>d__46';Assert-KmcBootstrapInventoryString $worker.workerStatus 'RanToCompletion';Assert-KmcOrdinaryInteger $worker.workerId 1 ([int]::MaxValue)
 foreach($key in @('nativeWorkerMatches','workerCompleted')){Assert-KmcChunk7Boolean $worker.$key $true};foreach($key in @('workerFaulted','workerCanceled')){Assert-KmcChunk7Boolean $worker.$key $false}
 $roles=[Collections.Generic.HashSet[int]]::new();[void]$roles.Add([int]$O.beforeLoad.menuPlayer)
 foreach($key in @('game','saveManager','player','state','areaState','mainState')){[void]$roles.Add([int]$O.loadedWorld.$key)}
 foreach($actor in $O.loadedWorld.party){foreach($key in @('object','descriptor','view')){[void]$roles.Add([int]$actor.$key)}}
 foreach($key in @('actorObject','actorDescriptor','actorView','petObject','petDescriptor','petView')){[void]$roles.Add([int]$O.field.$key)}
 foreach($value in @($O.sourceDescriptorObject,$r.descriptor,$w.writtenDescriptor,$worker.scope,$worker.iterator,$worker.worker)){
  Assert-KmcManualSeedObjectIdentity $value;if(-not$Labels.Contains([int]$value)-or-not$roles.Add([int]$value)){throw 'Manual save object identity is unretained or aliases a world/actor/save role.'}
 }
 foreach($pair in @(@('path',$path),@('leaf',$leaf),@('fileName',$leaf),@('name',$name),@('internalName',$name),@('type','Manual'),@('operation','None'),@('operationState','None'))){Assert-KmcBootstrapInventoryString $w.($pair[0]) $pair[1]}
 foreach($key in @('gameId','gameName','area')){Assert-KmcBootstrapInventoryString $w.$key $Request.persistenceLoad.$key}
 Assert-KmcManualSeedObjectIdentity $w.requestedObject;Assert-KmcManualSeedObjectIdentity $w.writtenObject
 if($w.requestedObject-ne$r.descriptor-or$w.writtenObject-ne$w.writtenDescriptor){throw 'Manual completion object labels differ from native request/preparation.'}
 if($null-ne$w.PSObject.Properties['completedWritesBefore']-or$null-ne$w.PSObject.Properties['nativeCallbackSupplied']-or$null-ne$w.PSObject.Properties['saveBeforeFieldSelection']){throw 'Manual checkpoint borrowed obsolete Auto completion fields.'}
 Assert-KmcOrdinaryInteger $w.requestedDescriptor $r.descriptor $r.descriptor;Assert-KmcOrdinaryInteger $w.callbackCount 1 1;Assert-KmcOrdinaryInteger $w.completionSequence 1 1
 foreach($key in @('authorizedWrites','admittedWrites','completedWrites','snapshotCount')){Assert-KmcOrdinaryInteger $w.$key 1 1}
 foreach($key in @('nativeWorkerSettled','hasFileOnDisk','isActuallySaved','saveAfterFieldSelection','emptyMountedArchive')){Assert-KmcChunk7Boolean $w.$key $true}
 foreach($key in @('autoLevelup','slotProjectionEnabled')){Assert-KmcChunk7Boolean $w.$key $false}
 Assert-KmcOrdinaryInteger $w.schema 3 3;Assert-KmcOrdinaryInteger $w.descriptorCount 2 2
 if($w.sha256-isnot[string]-or$w.sha256-cnotmatch'^[a-f0-9]{64}$'-or$w.sha256-ceq$Request.persistenceLoad.sha256-or$w.sha256-ceq$Request.fixture.working.sha256-or$w.sha256-ceq$Request.fixture.baseline.sha256){throw 'Manual output lacks fresh archive identity.'}
 Assert-KmcOrdinaryInteger $w.length 1 268435456;Assert-KmcOrdinaryInteger $w.lastWriteTimeUtcTicks 1 ([DateTime]::MaxValue.Ticks)
 Assert-KmcChunk7Boolean $O.slotProjection.initiallyEnabled $false;Assert-KmcChunk7Boolean $O.slotProjection.enabledForTerminalAuto $false;Assert-KmcOrdinaryInteger $O.slotProjection.maximumNativeWrites 1 1
 if($O.settledDescriptors-isnot[Array]-or$O.settledDescriptors.Count-ne2){throw 'Native Manual source/output descriptor inventory differs.'}
 $names=@($O.settledDescriptors|ForEach-Object{$_.fileName});if($names-cnotcontains$Request.persistenceLoad.fileName-or$names-cnotcontains$leaf-or@($names|Select-Object -Unique).Count-ne2){throw 'Native settled Manual leaves differ.'}
 foreach($d in $O.settledDescriptors){$input=$d.fileName-ceq$Request.persistenceLoad.fileName;Assert-KmcBootstrapInventoryString $d.internalName $(if($input){$Request.persistenceLoad.internalName}else{$name});Assert-KmcBootstrapInventoryString $d.type 'Manual'
  foreach($key in @('gameId','gameName','area')){Assert-KmcBootstrapInventoryString $d.$key $Request.persistenceLoad.$key};Assert-KmcBootstrapInventoryString $d.operationState 'None'
  foreach($key in @('isActuallySaved','hasFileOnDisk')){Assert-KmcChunk7Boolean $d.$key $true};Assert-KmcChunk7Boolean $d.autoLevelup $false
  if($input){Assert-KmcOrdinaryInteger $d.descriptor $O.sourceDescriptorObject $O.sourceDescriptorObject}
  else{Assert-KmcOrdinaryInteger $d.descriptor $w.writtenDescriptor $w.writtenDescriptor}
 }
 if((ConvertTo-KmcChunk7Canonical $O.beforeSaveArrangement)-cne(ConvertTo-KmcChunk7Canonical $O.afterSaveArrangement)){throw 'Native Manual save changed the settled acquired arrangement.'}
 $saved=$O.afterSaveArrangement
 if($saved.acquired-isnot[Array]-or$saved.acquired.Count-ne2){throw 'Manual save omitted the acquired owner or Horse progression.'}
 $savedIds=@($saved.acquired|ForEach-Object{$_.actorId});if($savedIds-cnotcontains$O.field.actorId-or$savedIds-cnotcontains$O.field.petId-or@($savedIds|Select-Object -Unique).Count-ne2){throw 'Saved acquired progression IDs differ from the field pair.'}
 $owner=@($saved.acquired|Where-Object actorId -CEQ $O.field.actorId)[0];$horse=@($saved.acquired|Where-Object actorId -CEQ $O.field.petId)[0]
 Assert-KmcBootstrapInventoryString $horse.blueprint $O.field.horseBlueprint
 Assert-KmcOrdinaryInteger $owner.experience $O.builderComplete.xp $O.builderComplete.xp
 Assert-KmcOrdinaryInteger $owner.characterLevel $O.builderComplete.level $O.builderComplete.level
 Assert-KmcOrdinaryInteger $horse.experience $O.builderComplete.petExperience $O.builderComplete.petExperience
 Assert-KmcOrdinaryInteger $horse.characterLevel $O.builderComplete.petLevel $O.builderComplete.petLevel
 foreach($row in $saved.acquired){Assert-KmcOrdinaryGuid $row.actorId;Assert-KmcOrdinaryInteger $row.experience 0 ([int]::MaxValue);Assert-KmcOrdinaryInteger $row.characterLevel 1 20
  if($row.classes-isnot[Array]-or$row.classes.Count-lt1-or$row.classes.Count-gt20){throw 'Acquired save progression class inventory is absent or unbounded.'}
  foreach($class in $row.classes){if($class.classGuid-isnot[string]-or$class.classGuid-cnotmatch'^[a-f0-9]{32}$'){throw 'Saved acquired native class is malformed.'};Assert-KmcOrdinaryInteger $class.level 1 20}
 }
 $original=$O.afterSaveArrangement|ConvertTo-Json -Depth 64|ConvertFrom-Json;$original.PSObject.Properties.Remove('acquired')
 if((ConvertTo-KmcChunk7Canonical $original)-cne(ConvertTo-KmcChunk7Canonical $O.afterChoices)){throw 'Save arrangement changed the original actor progression.'}
 $world=$O.savedFieldWorld;$old=$O.savedWorld;$paid=Test-KmcManualDerivedPaidCase $selection.case
 if((@($world.PSObject.Properties.Name)-join'|')-cne(@($old.PSObject.Properties.Name)-join'|')){throw 'Saved field world changed its native world shape.'}
 foreach($key in $old.PSObject.Properties.Name|Where-Object{$_-cne'party'}){if((ConvertTo-KmcChunk7Canonical $world.$key)-cne(ConvertTo-KmcChunk7Canonical $old.$key)){throw 'Saved field world changed native world identity or authority.'}}
 if($world.party-isnot[Array]-or$world.party.Count-ne($O.loadedWorld.party.Count+$(if($paid){2}else{1}))){throw 'Saved field roster omits or adds an actor.'}
 $seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);$seenObjects=[Collections.Generic.HashSet[int]]::new()
 foreach($actor in $world.party){Assert-KmcOrdinaryGuid $actor.id;if(-not$seen.Add($actor.id)){throw 'Saved field roster repeats an actor.'}
  if($actor.blueprint-isnot[string]-or$actor.blueprint-cnotmatch'^[a-f0-9]{32}$'){throw 'Saved field actor blueprint is malformed.'}
  foreach($key in @('object','descriptor','view')){Assert-KmcManualSeedObjectIdentity $actor.$key;if(-not$Labels.Contains([int]$actor.$key)-or-not$seenObjects.Add([int]$actor.$key)){throw 'Saved field actor identity is unretained or aliased.'}}
  foreach($key in @('isInGame','isInState','commandsEmpty')){Assert-KmcChunk7Boolean $actor.$key $true}
 }
 foreach($actor in $O.loadedWorld.party){$match=@($world.party|Where-Object id -CEQ $actor.id);if($match.Count-ne1-or(ConvertTo-KmcChunk7Canonical $match[0])-cne(ConvertTo-KmcChunk7Canonical $actor)){throw 'Saved field world changed an original native actor.'}}
 $owner=@($world.party|Where-Object id -CEQ $O.field.actorId);$pet=@($world.party|Where-Object id -CEQ $O.field.petId)
 if($owner.Count-ne1-or$pet.Count-ne1){throw 'Saved field roster omitted its acquired owner or Horse.'}
 foreach($pair in @(@($owner[0],'actorObject','actorDescriptor','actorView'),@($pet[0],'petObject','petDescriptor','petView'))){$row=$pair[0];$keys=@('object','descriptor','view');for($i=0;$i-lt3;$i++){Assert-KmcOrdinaryInteger $row.($keys[$i]) $O.field.($pair[$i+1]) $O.field.($pair[$i+1])}}
 Assert-KmcBootstrapInventoryString $pet[0].blueprint $O.field.horseBlueprint
}
function Assert-KmcManualDerivedOutcome($Request,$O,$Choice,$Labels,[string]$Root,$Selection=$null){
 if($null-eq$Selection){$Selection=$Request.manualDerived};$s=$Selection;$paid=Test-KmcManualDerivedPaidCase $s.case;$w=$O.nativeWrite
 Assert-KmcManualDerivedNativeSave $Request $O $Labels $Root
 $f=$O.field;Assert-KmcOrdinaryInteger $f.frame $O.loadedWorld.frame $O.settledWorld.frame
 foreach($pair in @(@('actorId',$Choice.actorId),@('petId',$Choice.petId),@('horseBlueprint','4016c7db400ab721ff125aef9e65e202'),@('featureBlueprint','7db7c50677e39f09feef56f3831fc723'))){Assert-KmcBootstrapInventoryString $f.($pair[0]) $pair[1]}
 if($f.featureSourceGuid-isnot[string]-or$f.featureSourceGuid-cnotmatch'^[a-f0-9]{32}$'){throw 'Native acquired feature lacks its exact source GUID.'};Assert-KmcOrdinaryInteger $f.rank 1 20
 Assert-KmcChunk7Boolean $f.reciprocal $true;Assert-KmcChunk7Boolean $f.ordinaryFieldAdmissionProven $true;Assert-KmcChunk7Boolean $f.mountedOrPoseAcceptanceProven $false
 $used=[Collections.Generic.HashSet[int]]::new();foreach($key in @('game','saveManager','player','state','areaState','mainState')){[void]$used.Add([int]$O.loadedWorld.$key)}
 foreach($a in $O.loadedWorld.party){foreach($key in @('object','descriptor','view')){[void]$used.Add([int]$a.$key)}}
 foreach($key in @('actorObject','actorDescriptor','actorView','petObject','petDescriptor','petView')){Assert-KmcManualSeedObjectIdentity $f.$key;if(-not$Labels.Contains([int]$f.$key)){throw 'Field label was not retained.'}}
 Assert-KmcOrdinaryInteger $f.actorObject $Choice.actorObject $Choice.actorObject;Assert-KmcOrdinaryInteger $f.actorDescriptor $Choice.descriptorObject $Choice.descriptorObject
 if(-not$paid){$a=@($O.loadedWorld.party|Where-Object id -CEQ $Choice.actorId);if($a.Count-ne1){throw 'Existing native field actor is absent.'};foreach($pair in @(@('actorObject','object'),@('actorDescriptor','descriptor'),@('actorView','view'))){Assert-KmcOrdinaryInteger $f.($pair[0]) $a[0].($pair[1]) $a[0].($pair[1])}}
 else{foreach($key in @('actorObject','actorDescriptor','actorView')){if(-not$used.Add([int]$f.$key)){throw 'New acquired actor aliases an original native role.'}}}
 foreach($key in @('petObject','petDescriptor','petView')){if(-not$used.Add([int]$f.$key)){throw 'New pet aliases another native role.'}}
 Assert-KmcBootstrapInventoryString $f.profileInventory.captureStatus 'PASS';Assert-KmcBootstrapInventoryString $f.profileInventory.gameplayAssemblyMvid '07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';Assert-KmcOrdinaryInteger $f.profileInventory.schemaVersion 1 1;Assert-KmcChunk7Boolean $f.profileInventory.profileAdmissionChanged $false
 if($f.profileInventory.party-isnot[Array]-or$f.profileInventory.party.Count-ne2){throw 'Field inventory must contain the exact acquired actor and Horse.'}
 $inv=@($f.profileInventory.party|Where-Object id -CEQ $Choice.actorId);$pet=@($f.profileInventory.party|Where-Object id -CEQ $Choice.petId);if($inv.Count-ne1-or$pet.Count-ne1){throw 'Field inventory pair identity differs.'}
 Assert-KmcOrdinaryInteger $inv[0].nativeLevelUpReadiness.experience $Choice.xp $Choice.xp;Assert-KmcOrdinaryInteger $inv[0].nativeLevelUpReadiness.characterLevel $Choice.level $Choice.level
 Assert-KmcBootstrapInventoryString $inv[0].petId $Choice.petId;Assert-KmcBootstrapInventoryString $pet[0].blueprintGuid $f.horseBlueprint
 if($paid){
  $body=Get-KmcManualDerivedPaidBody $s;$size=if($s.case-ceq'paid-medium-ranger-horse'){4}else{3}
  $b=$f.bodyObservation;Assert-KmcOrdinaryInteger $b.OriginalSizeOrdinal $size $size;Assert-KmcOrdinaryInteger $b.EffectiveSizeOrdinal $size $size
  foreach($key in @('NativeRaceAndPresetIdentityExact','NativeSkeletonIsExactPreset','NativeRigAvailable')){Assert-KmcChunk7Boolean $b.$key $true}
  foreach($key in @('RiderPolymorphed','MountPolymorphed')){Assert-KmcChunk7Boolean $b.$key $false}
  foreach($pair in @(@('RaceGuid','raceGuid'),@('PresetGuid','presetGuid'),@('Gender','gender'),@('SkeletonName','skeletonName'))){Assert-KmcBootstrapInventoryString $b.($pair[0]) $body.($pair[1])};Assert-KmcChunk7Boolean $b.LeftHanded $body.leftHanded
  $g=$O.fieldSelection;Assert-KmcBootstrapInventoryString $g.action 'native-ShowPartySelection';Assert-KmcBootstrapInventoryString $g.actorId $Choice.actorId
  Assert-KmcOrdinaryInteger $g.nativeCap 6 6;foreach($key in @('openCount','switchCount','goCount')){Assert-KmcOrdinaryInteger $g.$key 1 1}
  foreach($key in @('openActiveSelf','readyActiveInHierarchy','closedExactInstance')){Assert-KmcChunk7Boolean $g.$key $true};foreach($key in @('closedActiveSelf','manualPartyMutation')){Assert-KmcChunk7Boolean $g.$key $false}
  if(-not(Test-KmcChunk7FiniteNumber $g.readyAlpha)-or$g.readyAlpha-ne1){throw 'Native selector did not reach its actual input-ready state.'}
 }elseif($null-ne$O.PSObject.Properties['fieldSelection']){throw 'Existing acquisition cannot open a party selector.'}
 if($f.partyCharacters-isnot[Array]-or$f.partyCharacters.Count-ne($s.readiness.partyCharacterIds.Count+$(if($paid){1}else{0}))){throw 'Native final field-party count differs.'}
 $ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);foreach($id in $f.partyCharacters){Assert-KmcOrdinaryGuid $id;if(-not$ids.Add($id)){throw 'Native field repeats a character.'}}
 foreach($id in $s.readiness.partyCharacterIds){if(-not$ids.Contains($id)){throw 'Original native character was removed.'}};if(-not$ids.Contains($Choice.actorId)){throw 'Acquired native actor was not admitted to field.'}
 $before=$O.beforeChoices;$after=$O.afterChoices;Assert-KmcOrdinaryInteger $after.money $Choice.money $Choice.money;Assert-KmcOrdinaryInteger $after.experienceRatePercent 100 100
 if($before.actors-isnot[Array]-or$after.actors-isnot[Array]-or$before.actors.Count-ne$after.actors.Count){throw 'Original actor progression inventory differs.'}
 foreach($a in $before.actors){$matches=@($after.actors|Where-Object actorId -CEQ $a.actorId);if($matches.Count-ne1){throw 'Original progression actor is absent/duplicated.'};$b=$matches[0]
  foreach($key in @('actorId','blueprint')){Assert-KmcBootstrapInventoryString $b.$key $a.$key};Assert-KmcOrdinaryInteger $b.experience $a.experience $a.experience
  if($paid-or$a.actorId-cne$Choice.actorId){if((ConvertTo-KmcChunk7Canonical $a)-cne(ConvertTo-KmcChunk7Canonical $b)){throw 'Unrelated actor progression changed.'};continue}
  Assert-KmcOrdinaryInteger $b.characterLevel $Choice.level $Choice.level
  $old=@{};foreach($c in $a.classes){$old[$c.classGuid]=$c.level};$old['cda0615668a6df14eb36ba19ee881af6']=$s.existingRangerLevel+$Choice.commits
  if($b.classes-isnot[Array]-or$b.classes.Count-ne$old.Count){throw 'Native existing class count differs.'};$seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
  foreach($c in $b.classes){if($c.classGuid-isnot[string]-or-not$seen.Add($c.classGuid)-or-not$old.ContainsKey($c.classGuid)){throw 'Unexpected existing class mutation.'};Assert-KmcOrdinaryInteger $c.level $old[$c.classGuid] $old[$c.classGuid]}
 }
}
function Assert-KmcChunk7ManualDerivedArtifact($Request,$Manifest,[string]$Status='PASS'){
 $records=@($Manifest.artifacts|Where-Object{$_.kind-ceq'chunk7-manual-derived-evidence'-or$_.relativePath-ceq'chunk7-manual-derived-evidence.json'})
 if(-not(Test-KmcChunk7ManualDerivedScenario $Request.scenario)){if($records.Count){throw 'Foreign request supplied Manual-derived evidence.'};return}
 if($Status-ceq'FAIL'-and$records.Count-eq0){return}
 $artifact=Read-KmcChunk7ManifestArtifact $Request $Manifest 'chunk7-manual-derived-evidence.json' 'chunk7-manual-derived-evidence';$source=$null
 if($artifact.status-ceq'PASS'){
  $sourceLeaf=if($Request.manualDerived.nativeWriteLeaf-ceq'Manual_304_KMC_C7_MANUAL_DERIVED_FIELD.zks'){'chunk7-assisted-cold-inventory-evidence.json'}else{'chunk7-manual-seed-evidence.json'}
  $path=Join-Path (Join-Path (Split-Path $Request.evidenceRoot -Parent) $Request.manualDerived.sourceRunId) $sourceLeaf
  Assert-KmcNotReparsePoint $path 'qualified Manual lineage';Assert-KmcNotHardLink $path 'qualified Manual lineage';$file=Get-Item -LiteralPath $path
  if($file.Length-le0-or$file.Length-gt16MB-or(Get-KmcSha256 $path)-cne$Request.manualDerived.sourceArtifactSha256){throw 'Qualified Manual lineage is absent or changed.'}
  Assert-KmcManifestJsonMembersUnique $path 'qualified Manual lineage';$source=Read-KmcJson $path
  if((Get-KmcSha256 $path)-cne$Request.manualDerived.sourceArtifactSha256){throw 'Manual lineage changed while reading.'}
 }
 Assert-KmcChunk7ManualDerivedEvidence $Request $artifact $source $Status
}
