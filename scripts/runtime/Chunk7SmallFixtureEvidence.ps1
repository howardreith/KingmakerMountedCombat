Set-StrictMode -Version Latest
# F09 first capture is an unmounted fixture producer. These pins grant no profile
# admission, mounted action, ordinary F10 ancestry or successful cold derivative.
function Get-KmcChunk7SmallFixturePins {
 [pscustomobject][ordered]@{case='owner-seed-default-halfling-horse-manual';seedId='KMC_C7_NO_MODS_OWNER_SEED';seedSha256='3405d7c813668ad646788efd746a62c1b16e1438fad1bc4bcbebc961821b924f';
  seedQualificationRunId='20261017-c7b-world18-ownerseed-s4-cold';seedQualificationArtifactSha256='c0a4bf3ed11f1bc8d80ffa6f999491562f1896d2f3b38f4518067ad87a9ff74a';
  nativeInventoryRunId='20261018-c7b-world26-ownerseed-b3s1d-save';nativeInventoryArtifactSha256='01438210fbc9cdf8f8bc228b3d973ffff7220eaa32449bf43f4462fb355723a8';
  baseGameOnly=$true;expectedNativeLoadCount=1;expectedNativeWriteCount=1;targetRangerLevel=4}
}
function Get-KmcChunk7SmallFixtureRows {@('chunk7-small-fixture-preparation','C7-small-fixture-source','C7-small-fixture-native-load','C7-small-fixture-native-choices','C7-small-fixture-unmounted-field','C7-small-fixture-native-manual','C7-small-fixture-cleanup','C7-small-fixture-runtime','C7-small-fixture-interrupted')}
function Test-KmcChunk7SmallFixtureScenario([string]$Scenario){$Scenario-ceq'chunk7-small-fixture-preparation'}
function Assert-KmcChunk7SmallFixtureSelection($Request){
 if($Request-is[Collections.IDictionary]){$Request=[pscustomobject]$Request}
 $has=$null-ne$Request.PSObject.Properties['smallFixture']
 if(-not(Test-KmcChunk7SmallFixtureScenario $Request.scenario)){if($has){throw 'F09 fixture selection is forbidden under another scenario.'};return}
 if(-not$has){throw 'F09 exact closed fixture selection is missing.'}
 Assert-KmcBootstrapInventoryString $Request.scenario 'chunk7-small-fixture-preparation'
 if($Request.runId-isnot[string]-or$Request.runId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'){throw 'F09 fixture run ID is not canonical.'}
 $expected=Get-KmcChunk7SmallFixturePins;$s=$Request.smallFixture
 Assert-KmcAssistedObjectValue $s @($expected.PSObject.Properties.Name) 'F09 closed unmounted fixture selection'
 foreach($key in @('case','seedId','seedSha256','seedQualificationRunId','seedQualificationArtifactSha256','nativeInventoryRunId','nativeInventoryArtifactSha256')){Assert-KmcBootstrapInventoryString $s.$key $expected.$key}
 Assert-KmcChunk7Boolean $s.baseGameOnly $true
 foreach($key in @('expectedNativeLoadCount','expectedNativeWriteCount','targetRangerLevel')){Assert-KmcOrdinaryInteger $s.$key $expected.$key $expected.$key}
 if($Request.runId-ceq$s.seedQualificationRunId-or$Request.runId-ceq$s.nativeInventoryRunId){throw 'F09 run cannot alias its historical qualification or catalogue.'}
}
function Assert-KmcChunk7SmallFixtureDescriptor($Request){
 Assert-KmcChunk7SmallFixtureSelection $Request
 $d=$Request.persistenceLoad
 Assert-KmcAssistedObjectValue $d @('internalName','fileName','sha256','length','lastWriteTimeUtcTicks','gameId','gameName','area') 'F09 exact owner seed descriptor'
 $expected=@{internalName='KMC C7 NO MODS OWNER SEED';fileName='Manual_303_KMC_C7_NO_MODS_OWNER_SEED.zks';sha256=$Request.smallFixture.seedSha256;gameId='3f7381f7-aba6-43bf-bae6-39c6a66a7180';gameName='Testy the Modless';area='b10e86516b6437d45a629931c30936fe'}
 foreach($key in $expected.Keys){Assert-KmcBootstrapInventoryString $d.$key $expected[$key]}
 Assert-KmcOrdinaryInteger $d.length 1239025 1239025;Assert-KmcOrdinaryInteger $d.lastWriteTimeUtcTicks 1 ([DateTime]::MaxValue.Ticks)
 foreach($anchor in @($Request.fixture.baseline,$Request.fixture.working)){
  if($d.sha256-ceq$anchor.sha256-or$d.gameId-ceq$anchor.gameId-or$d.fileName-ceq$anchor.fileName-or$d.internalName-ceq$anchor.internalName){throw 'F09 seed aliases a protected Working/Baseline anchor.'}
 }
}
function Assert-KmcSmallFixtureEmptyArchive($Archive,$Descriptor){
 Assert-KmcAssistedObjectValue $Archive @('SchemaVersion','CampaignId','AreaId','GameTimeTicks','Policy','RulesId','Pairs','Participations','Slots','Combat') 'F09 actual current archive'
 Assert-KmcOrdinaryInteger $Archive.SchemaVersion 3 3;Assert-KmcBootstrapInventoryString $Archive.CampaignId $Descriptor.gameId;Assert-KmcBootstrapInventoryString $Archive.AreaId $Descriptor.area
 Assert-KmcOrdinaryInteger $Archive.GameTimeTicks 0 ([long]::MaxValue)
 foreach($key in @('Pairs','Participations','Slots')){Assert-KmcAssistedArray $Archive.$key 0 0 ('F09 empty '+$key)}
 if($null-ne$Archive.Combat){throw 'F09 unmounted Manual retains combat.'}
 # Use the same archive envelope policy as current production; no legacy projection.
 Assert-KmcBootstrapInventoryString $Archive.Policy 'rider-principal-distinct-native-v1';Assert-KmcBootstrapInventoryString $Archive.RulesId 'crpg-transport-v1'
}
function Assert-KmcSmallFixtureAuthority($Row,$Request,[long]$Loads,[long]$Writes,[long]$Generation){
 Assert-KmcAssistedObjectValue $Row @('root','generation','maximumNativeLoads','maximumNativeWrites','admittedNativeLoads','admittedNativeWrites','authorizedLoads','authorizedWrites','unauthorizedLoads','unauthorizedWrites','fatalViolations','suppressedWrites','reservations','manualNumber','outputLeaf') 'F09 exact authority'
 Assert-KmcBootstrapInventoryString $Row.root (Get-KmcAssistedSaveRoot $Request)
 Assert-KmcOrdinaryInteger $Row.generation $Generation $Generation
 foreach($key in @('maximumNativeLoads','maximumNativeWrites')){Assert-KmcOrdinaryInteger $Row.$key 1 1}
 foreach($key in @('admittedNativeLoads','authorizedLoads')){Assert-KmcOrdinaryInteger $Row.$key $Loads $Loads}
 foreach($key in @('admittedNativeWrites','authorizedWrites')){Assert-KmcOrdinaryInteger $Row.$key $Writes $Writes}
 foreach($key in @('unauthorizedLoads','unauthorizedWrites','fatalViolations','suppressedWrites')){Assert-KmcOrdinaryInteger $Row.$key 0 0}
 if($Writes-eq0){Assert-KmcOrdinaryInteger $Row.reservations 0 0;Assert-KmcOrdinaryInteger $Row.manualNumber 0 0;if($null-ne$Row.outputLeaf){throw 'F09 builder opened a writable output early.'}}
 else{Assert-KmcOrdinaryInteger $Row.reservations 1 1;Assert-KmcOrdinaryInteger $Row.manualNumber 1 ([int]::MaxValue-1);Assert-KmcBootstrapInventoryString $Row.outputLeaf ('Manual_'+$Row.manualNumber.ToString([Globalization.CultureInfo]::InvariantCulture)+'_KMC_C7_SMALL_FIXTURE.zks')}
}
function Assert-KmcSmallFixtureBody($Body){
 Assert-KmcAssistedObjectValue $Body @('originalSizeOrdinal','effectiveSizeOrdinal','riderPolymorphed','mountPolymorphed','mountBlueprintGuid','mountSizeOrdinal','raceGuid','presetGuid','gender','skeletonName','leftHanded','avatarMirrored','nativeRaceAndPresetIdentityExact','nativeSkeletonIsExactPreset','nativeRigAvailable','avatarLocalScale','dollEquipmentEntityIds','currentEquipmentBlueprints','activeVisualEquipmentNames','additionalLimbCount') 'F09 unmounted native body'
 foreach($key in @('originalSizeOrdinal','effectiveSizeOrdinal')){Assert-KmcOrdinaryInteger $Body.$key 3 3}
 Assert-KmcOrdinaryInteger $Body.mountSizeOrdinal 4 8
 foreach($key in @('riderPolymorphed','mountPolymorphed','leftHanded')){Assert-KmcChunk7Boolean $Body.$key $false}
 foreach($key in @('nativeRaceAndPresetIdentityExact','nativeSkeletonIsExactPreset','nativeRigAvailable')){Assert-KmcChunk7Boolean $Body.$key $true}
 if($Body.avatarMirrored-isnot[bool]){throw 'F09 native mirroring must be observed as a boolean.'}
 foreach($p in @(@('mountBlueprintGuid','4016c7db400ab721ff125aef9e65e202'),@('raceGuid','b0c3ef2729c498f47970bb50fa1acd30'),@('presetGuid','639cebe8cbc098345af429d62fd79578'),@('gender','Male'),@('skeletonName','MaleHalfling'))){Assert-KmcBootstrapInventoryString $Body.($p[0]) $p[1]}
 Assert-KmcAssistedObjectValue $Body.avatarLocalScale @('x','y','z') 'F09 native avatar scale'
 foreach($key in @('x','y','z')){if(-not(Test-KmcChunk7FiniteNumber $Body.avatarLocalScale.$key)-or[Math]::Abs([double]$Body.avatarLocalScale.$key)-lt0.01-or[Math]::Abs([double]$Body.avatarLocalScale.$key)-gt10){throw 'F09 native avatar scale is invalid.'}}
 Assert-KmcAssistedArray $Body.dollEquipmentEntityIds 0 128 'F09 exact Doll equipment IDs';$seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
 foreach($id in $Body.dollEquipmentEntityIds){Assert-KmcAssistedGuid32 $id;if(-not$seen.Add($id)){throw 'F09 Doll IDs repeat.'}}
 Assert-KmcAssistedArray $Body.currentEquipmentBlueprints 12 12 'F09 twelve native current equipment slots';foreach($id in $Body.currentEquipmentBlueprints){if($null-ne$id){Assert-KmcAssistedGuid32 $id}}
 Assert-KmcAssistedArray $Body.activeVisualEquipmentNames 1 128 'F09 visual equipment multiset';foreach($name in $Body.activeVisualEquipmentNames){if($name-isnot[string]-or[string]::IsNullOrWhiteSpace($name)-or$name.Length-gt160){throw 'F09 visual equipment name is invalid.'}}
 Assert-KmcOrdinaryInteger $Body.additionalLimbCount 0 0
}
function Assert-KmcSmallFixtureLiveTransforms($Rows,[string[]]$Names){
 Assert-KmcAssistedArray $Rows $Names.Count $Names.Count 'F09 exact live transform groups'
 $seen=[Collections.Generic.HashSet[int]]::new()
 foreach($name in $Names){
  $group=@($Rows|Where-Object name -CEQ $name);if($group.Count-ne1){throw 'F09 required live transform name is absent or repeated.'}
  Assert-KmcAssistedArray $group[0].matches 1 1 'F09 unique live transform match';$node=$group[0].matches[0]
  Assert-KmcBootstrapInventoryString $node.name $name;Assert-KmcOrdinaryInteger $node.instanceId ([int]::MinValue) ([int]::MaxValue)
  if($node.instanceId-eq0-or-not$seen.Add($node.instanceId)){throw 'F09 live transform identity is absent or repeated.'}
  Assert-KmcOrdinaryInteger $node.rootInstanceId ([int]::MinValue) ([int]::MaxValue);if($node.rootInstanceId-eq0){throw 'F09 live root identity is absent.'}
  foreach($key in @('localPosition','localEulerDegrees','localScale','worldPosition','worldScale','positionInObservedRoot')){
   Assert-KmcAssistedObjectValue $node.$key @('x','y','z') 'F09 measured native vector';foreach($axis in @('x','y','z')){if(-not(Test-KmcChunk7FiniteNumber $node.$key.$axis)){throw 'F09 live geometry vector is non-finite.'}}
  }
 }
}
function Assert-KmcSmallFixtureLiveInventory($Inventory,$Body,[string]$RiderId,[string]$HorseId){
 Assert-KmcOrdinaryInteger $Inventory.schemaVersion 1 1
 foreach($p in @(@('captureStatus','PASS'),@('captureKind','installed-native-small-body-catalogue'),@('gameplayAssemblyMvid','07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'))){Assert-KmcBootstrapInventoryString $Inventory.($p[0]) $p[1]}
 foreach($key in @('profileAdmissionChanged','nativeFixtureCreated','poseQualified','ordinaryAcquisitionProven')){Assert-KmcChunk7Boolean $Inventory.$key $false}
 Assert-KmcAssistedArray $Inventory.party 2 2 'F09 observed unmounted rider and Horse'
 $r=@($Inventory.party|Where-Object id -CEQ $RiderId);$h=@($Inventory.party|Where-Object id -CEQ $HorseId)
 if($r.Count-ne1-or$h.Count-ne1-or$RiderId-ceq$HorseId){throw 'F09 live rig observation has foreign or repeated actors.'};$r=$r[0];$h=$h[0]
 Assert-KmcBootstrapInventoryString $r.raceGuid $Body.raceGuid;Assert-KmcBootstrapInventoryString $r.dollPresetGuid $Body.presetGuid
 Assert-KmcBootstrapInventoryString $r.dollGender 'Male';Assert-KmcChunk7Boolean $r.dollLeftHanded $false;Assert-KmcChunk7Boolean $r.polymorphed $false
 Assert-KmcBootstrapInventoryString $r.originalSize 'Small';Assert-KmcBootstrapInventoryString $r.effectiveSize 'Small'
 Assert-KmcBootstrapInventoryString $r.petId $HorseId;Assert-KmcBootstrapInventoryString $h.masterId $RiderId;Assert-KmcBootstrapInventoryString $h.blueprintGuid $Body.mountBlueprintGuid
 Assert-KmcChunk7Boolean $r.character.present $true;Assert-KmcChunk7Boolean $r.character.skeleton.present $true
 Assert-KmcBootstrapInventoryString $r.character.skeleton.name $Body.skeletonName
 Assert-KmcChunk7Boolean $r.character.mirrored $Body.avatarMirrored
 Assert-KmcAssistedSame $r.dollEquipmentEntityIds $Body.dollEquipmentEntityIds 'F09 live Doll equipment'
 Assert-KmcAssistedSame $r.character.root.localScale $Body.avatarLocalScale 'F09 live avatar scale'
 Assert-KmcSmallFixtureLiveTransforms $r.character.poseTransforms @('Pelvis','L_Up_leg','L_leg','L_foot','R_Up_leg','R_leg','R_foot')
 Assert-KmcSmallFixtureLiveTransforms $h.horseSeatAnchors @('Chest','L_Stirrup','R_Stirrup')
}
function Assert-KmcChunk7SmallFixtureEvidence($Request,$Artifact){
 Assert-KmcChunk7SmallFixtureDescriptor $Request;Assert-KmcAssistedColdArtifactEnvelope $Request $Artifact 'chunk7-small-fixture-evidence'
 Assert-KmcBootstrapInventoryString $Artifact.status 'PASS';$o=$Artifact.observations
 Assert-KmcBootstrapInventoryString $o.contract 'owner-seed-unmounted-small-fixture-v1';Assert-KmcAssistedSame $o.selection $Request.smallFixture 'F09 exact selection'
 foreach($key in @('fixtureSetupOnly')){Assert-KmcChunk7Boolean $o.$key $true}
 foreach($key in @('producerAncestry','profileAdmissionChanged','poseQualified','mountedBehaviorQualified','coldLoadQualified')){Assert-KmcChunk7Boolean $o.$key $false}
 foreach($key in @('xpWrites','moneyWrites','resourceWrites','mountRequests','manualPartyMutations')){Assert-KmcOrdinaryInteger $o.$key 0 0}
 Assert-KmcBootstrapInventoryString $o.loadedArchiveKind 'Missing';Assert-KmcChunk7Boolean $o.loadedArchiveCanRestore $false
 $generation=$o.beforeLoadAuthority.generation;Assert-KmcOrdinaryInteger $generation 1 ([int]::MaxValue)
 Assert-KmcSmallFixtureAuthority $o.beforeLoadAuthority $Request 0 0 $generation
 foreach($key in @('seedId','seedSha256','seedQualificationRunId','seedQualificationArtifactSha256','nativeInventoryRunId','nativeInventoryArtifactSha256')){Assert-KmcBootstrapInventoryString $o.source.$key $Request.smallFixture.$key}
 Assert-KmcAssistedSame $o.source.descriptor $Request.persistenceLoad 'F09 source descriptor';foreach($key in @('masterLoaded','producerAncestry')){Assert-KmcChunk7Boolean $o.source.$key $false}
 Assert-KmcAssistedSame $o.neighboursBefore $o.neighboursAfter 'F09 preserved original companions';Assert-KmcAssistedSame $o.beforeSaveArrangement $o.afterSaveArrangement 'F09 native save changed progression or membership'
 Assert-KmcSmallFixtureBody $o.bodyObservation;Assert-KmcSmallFixtureBody $o.savedBodyObservation;Assert-KmcAssistedSame $o.bodyObservation $o.savedBodyObservation 'F09 saved body/equipment'
 $hire=$o.paidHireAdmission
 foreach($key in @('forFree','matchPlayerXpExactly','importableInDungeon')){Assert-KmcChunk7Boolean $hire.$key $false};foreach($key in @('xpAssistance','moneyAssistance')){Assert-KmcOrdinaryInteger $hire.$key 0 0}
 Assert-KmcOrdinaryInteger $hire.nativeCost 1 ([long]::MaxValue);Assert-KmcOrdinaryInteger $hire.moneyBefore $hire.nativeCost ([long]::MaxValue)
 Assert-KmcOrdinaryInteger $hire.nativeReadyLevel 4 4;Assert-KmcOrdinaryInteger $hire.nativeInheritedXp 1 ([int]::MaxValue)
 $startingGold=0
 if($Request.productVersion-cmatch'^0\.1\.0-chunk7b-preview\.(\d+)$'-and[int]$Matches[1]-ge65){
  Assert-KmcOrdinaryInteger $o.nativeStartingGold 0 ([int]::MaxValue);$startingGold=[long]$o.nativeStartingGold
  $admissions=@($o.choices|Where-Object event -CEQ 'native-paid-hire-admission')
  $spends=@($o.choices|Where-Object event -CEQ 'native-hire-spent')
  if($admissions.Count-ne1-or$spends.Count-ne1-or$admissions[0].spendObservationContract-cne'native-hire-spent-v2'){
   throw 'F09 native paid hire lacks its single starting-gold-aware spend event.'
  }
  Assert-KmcOrdinaryInteger $spends[0].moneyBefore $hire.moneyBefore $hire.moneyBefore
  Assert-KmcOrdinaryInteger $spends[0].nativeCost $hire.nativeCost $hire.nativeCost
  Assert-KmcOrdinaryInteger $spends[0].nativeStartingGold $startingGold $startingGold
  Assert-KmcBootstrapInventoryString $spends[0].runId $Request.runId
 }
 $paidMoney=[long](([decimal]$hire.moneyBefore)+[decimal]$startingGold-[decimal]$hire.nativeCost)
 if($Request.productVersion-cmatch'^0\.1\.0-chunk7b-preview\.(\d+)$'-and[int]$Matches[1]-ge65){
  Assert-KmcOrdinaryInteger $spends[0].moneyAfter $paidMoney $paidMoney
 }
 Assert-KmcOrdinaryInteger $o.afterSaveArrangement.money $paidMoney $paidMoney
 $rows=@($o.afterSaveArrangement.actors);Assert-KmcAssistedArray $o.afterSaveArrangement.actors 7 7 'F09 exact field actor graph'
 $ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
 foreach($row in $rows){if($row.id-isnot[string]-or$row.id-cnotmatch'^[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}$'-or$row.id-ceq'00000000-0000-0000-0000-000000000000'-or-not$ids.Add($row.id)){throw 'F09 exact native actor identities are missing, duplicate or noncanonical.'};if($row.nativeCustomCompanion-isnot[bool]){throw 'F09 native custom-companion observation is not boolean.'}}
 $main=@($rows|Where-Object id -CEQ '73681ef2-b281-4bc6-bc05-2de68c1616cb');$small=@($rows|Where-Object nativeCustomCompanion -EQ $true)
 if($main.Count-ne1-or$small.Count-ne1-or$main[0].id-ceq$small[0].id){throw 'F09 exact original main/new native custom companion graph is missing.'}
 foreach($row in @($main[0],$small[0])){Assert-KmcOrdinaryInteger $row.level 4 4;Assert-KmcChunk7Boolean $row.nativeCanLevelUp $false;$ranger=@($row.classes|Where-Object guid -CEQ 'cda0615668a6df14eb36ba19ee881af6');if($ranger.Count-ne1){throw 'F09 native Ranger class is absent.'};Assert-KmcOrdinaryInteger $ranger[0].level 4 4}
 Assert-KmcOrdinaryInteger $main[0].experience 9000 9000;Assert-KmcOrdinaryInteger $small[0].experience $hire.nativeInheritedXp $hire.nativeInheritedXp
 Assert-KmcOrdinaryInteger $o.mainNativeCommitCount 1 1;Assert-KmcOrdinaryInteger $o.paidNativeCommitCount 4 4
 foreach($key in @('mainAutoLevelupOverrideCount','paidAutoLevelupOverrideCount')){Assert-KmcOrdinaryInteger $o.$key 0 0}
 foreach($tuple in @(@($o.mainBuild,$main[0],$hire.moneyBefore),@($o.paidBuild,$small[0],$paidMoney))){
  $build=$tuple[0];$row=$tuple[1];Assert-KmcBootstrapInventoryString $build.actorId $row.id
  foreach($key in @('level','rangerLevel')){Assert-KmcOrdinaryInteger $build.$key 4 4}
  Assert-KmcOrdinaryInteger $build.xp $row.experience $row.experience;Assert-KmcOrdinaryInteger $build.money $tuple[2] $tuple[2];Assert-KmcChunk7Boolean $build.nativeCanLevelUp $false
 }
 Assert-KmcAssistedArray $o.acquiredOwners 2 2 'F09 reciprocal Horse owners'
 $owners=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
 foreach($owner in $o.acquiredOwners){$r=@($rows|Where-Object id -CEQ $owner.riderId);$h=@($rows|Where-Object id -CEQ $owner.horseId);if($r.Count-ne1-or$h.Count-ne1-or-not$owners.Add($owner.riderId)-or$owner.riderId-cnotin@($main[0].id,$small[0].id)-or$r[0].petId-cne$owner.horseId-or$h[0].masterId-cne$owner.riderId){throw 'F09 reciprocal native owner graph differs.'};Assert-KmcBootstrapInventoryString $owner.horseBlueprint '4016c7db400ab721ff125aef9e65e202';Assert-KmcChunk7Boolean $owner.reciprocal $true;Assert-KmcBootstrapInventoryString $owner.featureGuid '7db7c50677e39f09feef56f3831fc723';Assert-KmcAssistedGuid32 $owner.featureSourceGuid;Assert-KmcOrdinaryInteger $owner.rank 1 20
  Assert-KmcAssistedArray $r[0].features 1 128 'F09 native sourced rider features';$fact=@($r[0].features|Where-Object guid -CEQ $owner.featureGuid);$rank=@($r[0].features|Where-Object guid -CEQ '1670990255e4fe948a863bafd5dbda5d')
  if($fact.Count-ne1-or$rank.Count-ne1){throw 'F09 registered Horse feature or native companion rank fact is absent/ambiguous.'};Assert-KmcBootstrapInventoryString $fact[0].source $owner.featureSourceGuid;Assert-KmcOrdinaryInteger $rank[0].rank $owner.rank $owner.rank
 }
 Assert-KmcSmallFixtureLiveInventory $o.unmountedProfileInventory $o.bodyObservation $small[0].id $small[0].petId
 Assert-KmcAssistedArray $o.afterSaveArrangement.characters 5 5 'F09 native field characters';Assert-KmcAssistedArray $o.afterSaveArrangement.remote 0 0 'F09 no remaining remote companions'
 $expectedCharacters=@('73681ef2-b281-4bc6-bc05-2de68c1616cb','5dd7bd1d-ae4c-4df7-9687-50fceb027344','8265f8cf-0969-4ec7-a266-2448de91c8c4','fd213210-71cd-41b0-b35b-68e1b18e9b43',$small[0].id)
 Assert-KmcAssistedSame @($o.afterSaveArrangement.characters|Sort-Object -CaseSensitive) @($expectedCharacters|Sort-Object -CaseSensitive) 'F09 exact original four plus paid character'
 $s=$o.selector;foreach($key in @('openCount','switchCount','goCount')){Assert-KmcOrdinaryInteger $s.$key 1 1};Assert-KmcBootstrapInventoryString $s.actorId $small[0].id;Assert-KmcChunk7Boolean $s.manualPartyMutation $false;Assert-KmcChunk7Boolean $s.closedExactInstance $true;Assert-KmcChunk7Boolean $s.closedActiveSelf $false
 $labels=Get-KmcAssistedColdLabels $o.nativeLoad
 foreach($phase in @(@('loadedWorld',4,0),@('fieldWorld',7,0),@('savedWorld',7,1),@('settledWorld',7,1))){$w=$o.($phase[0]);Assert-KmcAssistedArray $w.party $phase[1] $phase[1] 'F09 observed native field';foreach($key in @('owners','slots','casts','semanticRestores','presentationRestores')){Assert-KmcOrdinaryInteger $w.$key 0 0};foreach($key in @('snapshots','completedSaves')){Assert-KmcOrdinaryInteger $w.$key $phase[2] $phase[2]};foreach($key in @('gameId','gameName','area')){Assert-KmcBootstrapInventoryString $w.$key $Request.persistenceLoad.$key};Assert-KmcBootstrapInventoryString $w.mode 'Default';Assert-KmcChunk7Boolean $w.inCombat $false;Assert-KmcSmallFixtureAuthority $w.authority $Request 1 $phase[2] $generation
  foreach($key in @('game','saveManager','player','state','areaState','mainState')){Assert-KmcBootstrapInventoryObject $w.$key;if(-not$labels.Contains([int]$w.$key)){throw 'F09 world label is not owned by the installed native observer.'};Assert-KmcOrdinaryInteger $w.$key $o.loadedWorld.$key $o.loadedWorld.$key}
  foreach($row in $w.party){foreach($key in @('object','descriptor','view')){Assert-KmcBootstrapInventoryObject $row.$key;if(-not$labels.Contains([int]$row.$key)){throw 'F09 actor label is foreign to the retained native observation.'}};Assert-KmcChunk7Boolean $row.ready $true}
 }
 foreach($row in $o.nativeLoad.events){foreach($key in @('game','saveManager','player')){Assert-KmcOrdinaryInteger $row.$key $o.loadedWorld.$key $o.loadedWorld.$key}}
 Assert-KmcAssistedColdNoPreparation $o.noPreparation $Request
 $w=$o.nativeWrite;Assert-KmcBootstrapInventoryString $w.type 'Manual';Assert-KmcBootstrapInventoryString $w.internalName 'KMC_C7_SMALL_FIXTURE';Assert-KmcBootstrapInventoryString $w.operationState 'None';Assert-KmcBootstrapInventoryString $w.readKind 'Current';Assert-KmcOrdinaryInteger $w.sourceSchema 3 3
 foreach($key in @('callbackCount','completionSequence','snapshotCount')){Assert-KmcOrdinaryInteger $w.$key 1 1};foreach($key in @('isActuallySaved','hasFileOnDisk','nativeWorkerSettled')){Assert-KmcChunk7Boolean $w.$key $true};foreach($key in @('autoLevelup','slotProjectionEnabled')){Assert-KmcChunk7Boolean $w.$key $false}
 Assert-KmcSmallFixtureEmptyArchive $w.archive $Request.persistenceLoad
 Assert-KmcBootstrapInventoryString $w.fileName $o.savedWorld.authority.outputLeaf;Assert-KmcBootstrapInventoryString $w.path (Join-Path (Get-KmcAssistedSaveRoot $Request) $w.fileName)
 $p=$o.nativeSave;Assert-KmcBootstrapInventoryString $p.contract 'small-fixture-native-save-v1';foreach($key in @('installed','complete')){Assert-KmcChunk7Boolean $p.$key $true};foreach($key in @('requests','prepares','starts','stages','completions','callbacks')){Assert-KmcOrdinaryInteger $p.$key 1 1};Assert-KmcAssistedArray $p.errors 0 0 'F09 save observer errors'
 foreach($key in @('custodySettled','nativeWorkerSettled','observersReleased','worldAdmitted','sourceUnchanged','selectorClosed','selectionRestored','pauseRestored','ummLeaseReleased','acquiredNativeStateRetained')){Assert-KmcChunk7Boolean $o.cleanup.$key $true}
 foreach($key in @('manualRefunds','manualCompanionRemovals','manualFactRemovals','manualPartyMutations')){Assert-KmcOrdinaryInteger $o.cleanup.$key 0 0}
 Assert-KmcSmallFixtureAuthority $o.cleanup.authority $Request 1 1 $generation
}
function Assert-KmcChunk7SmallFixtureArtifact($Request,$Manifest,[string]$Status='PASS'){
 if(-not(Test-KmcChunk7SmallFixtureScenario $Request.scenario)){return}
 Assert-KmcChunk7SmallFixtureDescriptor $Request
 $a=Read-KmcChunk7ManifestArtifact $Request $Manifest 'chunk7-small-fixture-evidence.json' 'chunk7-small-fixture-evidence'
 Assert-KmcBootstrapInventoryString $a.status $Status
 if($Status-ceq'PASS'){Assert-KmcChunk7SmallFixtureEvidence $Request $a}
 elseif($Status-cne'FAIL'){throw 'F09 artifact status is not an admitted completion status.'}
}
