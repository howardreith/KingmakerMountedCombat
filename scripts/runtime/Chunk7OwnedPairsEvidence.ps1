Set-StrictMode -Version Latest
function Test-KmcChunk7OwnedPairsScenario([string]$Scenario){$Scenario-ceq'chunk7-owned-two-pair-campaign'}
function Get-KmcChunk7SelectedScenario($Request){
 if(-not(Test-KmcChunk7OwnedPairsScenario $Request.scenario)){return [string]$Request.scenario}
 Assert-KmcChunk7OwnedPairsSelection $Request
 switch -CaseSensitive ($Request.ownedPairs.case){
  'exploration'{'chunk7-two-pair-exploration'}
  'owner-seed-exploration'{'chunk7-two-pair-exploration'}
  'combat-rider-first'{'chunk7-two-pair-combat-rider-first'}
  'combat-mount-first'{'chunk7-two-pair-combat-mount-first'}
  'realtime-core'{'chunk7-two-pair-realtime-core'}
  'realtime-casting'{'chunk7-two-pair-realtime-casting'}
  'realtime-charge'{'chunk7-two-pair-realtime-charge'}
  'native-life-cleanup'{'chunk7-two-pair-native-life-cleanup'}
  'persistence-save'{'chunk7-persistence-save'}
  'persistence-load'{'chunk7-persistence-load'}
 }
}
function Get-KmcChunk7OwnedWriteCount($Request){
 # Owner seed: exactly its declared setup level-up Autos (validated 0..2 and bound to the native observation).
 if(Test-KmcChunk7OwnedPairsOwnerSeed $Request){return [int]$Request.ownedPairs.expectedNativeWriteCount}
 if($Request.ownedPairs.case-ceq'persistence-save'-and$Request.persistenceCase-ceq'multi-owned-worker-cancel-manual'){return 2}
 if($Request.ownedPairs.case-ceq'persistence-save'-or($Request.ownedPairs.case-ceq'persistence-load'-and$Request.persistenceCase-cin@('legacy-mounted-pending-tb-manual','owned-schema1-resave-manual'))){return 1};return 0
}
function Get-KmcChunk7OwnedLoadCount($Request){
 if($Request.ownedPairs.case-ceq'persistence-load'-and$Request.persistenceCase-ceq'multi-exploration-three-loads-manual'){return 3};return 1
}
function Get-KmcChunk7OwnedPairIds($Request){@($Request.ownedPairs.riderAId,$Request.ownedPairs.mountAId,$Request.ownedPairs.riderBId,$Request.ownedPairs.mountBId)}
function Get-KmcChunk7SelectedFixture($Request){if(Test-KmcChunk7OwnedPairsScenario $Request.scenario){return $Request.persistenceLoad};return $Request.fixture.working}
# The owner-supplied seed kind of the closed family: seed pins, two selected riders,
# the full party and the exact rider-B native XP setup; no producer or pet pins.
function Test-KmcChunk7OwnedPairsOwnerSeed($Request){
 if($Request-is[Collections.IDictionary]){$Request=[pscustomobject]$Request}
 if($null-eq$Request-or$null-eq$Request.PSObject.Properties['ownedPairs']){return $false};$s=$Request.ownedPairs
 if($s-is[Collections.IDictionary]){$s=[pscustomobject]$s}
 $null-ne$s-and$null-ne$s.PSObject.Properties['case']-and$s.case-is[string]-and$s.case-ceq'owner-seed-exploration'
}
function Assert-KmcChunk7OwnerSeedPairsSelection($Request,$Selection,$Checkpoint){
 $s=$Selection
 Assert-KmcExactProperties $s @('case','seedId','seedSha256','riderAId','riderBId','partyIds','xpAssistance','baseGameOnly','expectedNativeLoadCount','expectedNativeWriteCount') 'owner-seed two-pair selection'
 if($Request.runId-isnot[string]-or$Request.runId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'){throw 'Owner-seed consumer run identity is malformed.'}
 if($s.seedId-isnot[string]-or$s.seedId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'-or$s.seedId-ceq$Request.runId){throw 'Owner-seed two-pair exploration requires an exact registered seed identity.'}
 if($s.seedSha256-isnot[string]-or$s.seedSha256-cnotmatch'^[a-f0-9]{64}$'){throw 'Owner-seed two-pair exploration requires its exact seed SHA256.'}
 Assert-KmcOrdinaryGuid $s.riderAId;Assert-KmcOrdinaryGuid $s.riderBId
 if($s.riderAId-ceq$s.riderBId){throw 'Owner-seed rider A and rider B must be distinct actors.'}
 if($s.partyIds-isnot[Array]-or$s.partyIds.Count-lt2-or$s.partyIds.Count-gt6){throw 'Owner-seed full party must be bounded.'}
 $party=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);foreach($id in $s.partyIds){Assert-KmcOrdinaryGuid $id;if(-not$party.Add($id)){throw 'Owner-seed party repeats an actor.'}}
 foreach($id in @($s.riderAId,$s.riderBId)){if(-not$party.Contains($id)){throw 'Owner-seed riders must be members of the selected party.'}}
 if($null-ne$s.xpAssistance){
  Assert-KmcExactProperties $s.xpAssistance @('unitId','targetCharacterLevel') 'owner-seed XP assistance'
  if($s.xpAssistance.unitId-isnot[string]-or$s.xpAssistance.unitId-cne$s.riderBId){throw 'Owner-seed XP assistance may target only rider B.'}
  Assert-KmcOrdinaryInteger $s.xpAssistance.targetCharacterLevel 2 8
 }
 if($null-ne$Checkpoint){throw 'Owner-seed exploration cannot select a persistence checkpoint.'}
 Assert-KmcChunk7Boolean $s.baseGameOnly $true;Assert-KmcOrdinaryInteger $s.expectedNativeLoadCount 1 1;Assert-KmcOrdinaryInteger $s.expectedNativeWriteCount 0 2
}
function Assert-KmcChunk7OwnedPairsSelection($Request){
 if($Request-is[Collections.IDictionary]){$Request=[pscustomobject]$Request}
 $has=$null-ne$Request.PSObject.Properties['ownedPairs']
 if(-not(Test-KmcChunk7OwnedPairsScenario $Request.scenario)){if($has){throw 'Inherited two-owner selection is forbidden under another family.'};return}
 if(-not$has){throw 'Inherited two-owner campaign requires its explicit selection.'};$s=$Request.ownedPairs
 $checkpoint=$null;if($null-ne$Request.PSObject.Properties['persistenceCase']){$checkpoint=$Request.persistenceCase}
 if(Test-KmcChunk7OwnedPairsOwnerSeed $Request){Assert-KmcChunk7OwnerSeedPairsSelection $Request $s $checkpoint;return}
 $area=$checkpoint-is[string]-and$checkpoint-ceq'multi-area-autosave'
 $fields=@('case','sourceRunId','sourceArtifactSha256','sourceArchiveSha256','checkpointSourceRunId','checkpointSourceArtifactSha256','riderAId','mountAId','riderBId','mountBId','partyIds','baseGameOnly','expectedNativeLoadCount','expectedNativeWriteCount');if($area){$fields+='sourceArea'}
 Assert-KmcExactProperties $s $fields 'inherited native pair selection'
 if($area-and($s.sourceArea-isnot[string]-or$s.sourceArea-cnotmatch'^[a-f0-9]{32}$'-or$s.sourceArea-ceq('0'*32))){throw 'Owned area checkpoint requires the exact original acquired Manual area.'}
 if($s.case-isnot[string]-or$s.case-cnotin@('exploration','combat-rider-first','combat-mount-first','realtime-core','realtime-casting','realtime-charge','native-life-cleanup','persistence-save','persistence-load')){throw 'Inherited pair case is not closed.'}
 if($s.sourceRunId-isnot[string]-or$s.sourceRunId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'-or$s.sourceRunId-ceq$Request.runId){throw 'Inherited pair source run must be distinct and exact.'}
 foreach($key in @('sourceArtifactSha256','sourceArchiveSha256')){if($s.$key-isnot[string]-or$s.$key-cnotmatch'^[a-f0-9]{64}$'){throw 'Inherited source requires exact SHA256 pins.'}}
 $ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);foreach($key in @('riderAId','mountAId','riderBId','mountBId')){Assert-KmcOrdinaryGuid $s.$key;if(-not$ids.Add($s.$key)){throw 'Inherited role IDs collide.'}}
 if($s.partyIds-isnot[Array]-or$s.partyIds.Count-lt4-or$s.partyIds.Count-gt12){throw 'Complete inherited field roster must be bounded.'}
 $party=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);foreach($id in $s.partyIds){Assert-KmcOrdinaryGuid $id;if(-not$party.Add($id)){throw 'Inherited roster repeats an actor.'}}
 foreach($id in $ids){if(-not$party.Contains($id)){throw 'Inherited roster omits a selected pair role.'}}
 if($s.case-cin@('persistence-save','persistence-load')){if($checkpoint-isnot[string]-or($checkpoint-cnotin@('multi-exploration-manual','multi-exploration-quick','multi-exploration-auto','multi-rt-debt-manual','multi-tb-debt-manual','zero-mounted-tb-retained-manual','legacy-mounted-pending-tb-manual','multi-area-autosave')-and($s.case-cne'persistence-load'-or$checkpoint-cnotin@('multi-exploration-three-loads-manual','multi-exploration-three-area-transfers-manual','owned-schema1-resave-manual'))-and($s.case-cne'persistence-save'-or$checkpoint-cnotin@('multi-nonselected-mount-unsettled-manual','multi-owned-worker-cancel-manual','owned-schema1-manual')))){throw 'Inherited persistence retains its closed checkpoints; save boundaries are source-only.'}}
 elseif($null-ne$checkpoint){throw 'Non-persistence inherited campaign cannot select a checkpoint.'}
 if($s.case-ceq'persistence-load'){
  if($s.checkpointSourceRunId-isnot[string]-or$s.checkpointSourceRunId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'-or$s.checkpointSourceRunId-cin@($Request.runId,$s.sourceRunId)-or$s.checkpointSourceArtifactSha256-isnot[string]-or$s.checkpointSourceArtifactSha256-cnotmatch'^[a-f0-9]{64}$'){throw 'Cold inherited checkpoint requires its exact separate successful source.'}
 }elseif($null-ne$s.checkpointSourceRunId-or$null-ne$s.checkpointSourceArtifactSha256){throw 'Only cold inherited persistence consumes a checkpoint parent.'}
 Assert-KmcChunk7Boolean $s.baseGameOnly $true;$loads=Get-KmcChunk7OwnedLoadCount $Request;Assert-KmcOrdinaryInteger $s.expectedNativeLoadCount $loads $loads
 $writes=Get-KmcChunk7OwnedWriteCount $Request;Assert-KmcOrdinaryInteger $s.expectedNativeWriteCount $writes $writes
}
function Assert-KmcChunk7OwnedPairsDescriptor($Request){
 Assert-KmcChunk7OwnedPairsSelection $Request
 if(-not(Test-KmcChunk7OwnedPairsScenario $Request.scenario)){throw 'Inherited descriptor belongs to the exact separate family.'}
 $d=$Request.persistenceLoad;Assert-KmcExactProperties $d @('internalName','fileName','sha256','length','lastWriteTimeUtcTicks','gameId','gameName','area') 'inherited native descriptor'
 if(Test-KmcChunk7OwnedPairsOwnerSeed $Request){
  # Registry-supplied exact names, admitted generically as the cold owner-seed kind does.
  if($d.internalName-isnot[string]-or$d.internalName-cnotmatch(Get-KmcChunk7OwnerSeedNamePattern)-or$d.internalName-ceq'KMC_C7_ASSISTED_FIXTURE'){throw 'Owner seed internal name is malformed or impersonates the producer fixture.'}
  if($d.fileName-isnot[string]-or$d.fileName-cnotmatch'^[A-Za-z0-9][A-Za-z0-9_.-]{0,95}\.zks$'-or$d.fileName.EndsWith('_KMC_C7_ASSISTED_FIXTURE.zks',[StringComparison]::Ordinal)){throw 'Owner seed file name is malformed or impersonates the producer fixture.'}
  if($d.sha256-isnot[string]-or$d.sha256-cnotmatch'^[a-f0-9]{64}$'){throw 'Owner seed archive hash is malformed.'};Assert-KmcBootstrapInventoryString $d.sha256 $Request.ownedPairs.seedSha256
  Assert-KmcOrdinaryGuid $d.gameId
  if($d.gameName-isnot[string]-or[string]::IsNullOrWhiteSpace($d.gameName)-or$d.gameName.Length-gt256-or$d.gameName-match'[\x00-\x1f\x7f]'-or$d.area-isnot[string]-or$d.area-cnotmatch'^[a-f0-9]{32}$'-or$d.gameId-ceq$Request.fixture.working.gameId-or$d.sha256-ceq$Request.fixture.working.sha256-or$d.sha256-ceq$Request.fixture.baseline.sha256){throw 'Owner seed campaign must remain distinct from protected Working/Baseline.'}
  Assert-KmcOrdinaryInteger $d.length 1 268435456;Assert-KmcOrdinaryInteger $d.lastWriteTimeUtcTicks 1 ([DateTime]::MaxValue.Ticks);return
 }
 $cold=$Request.ownedPairs.case-ceq'persistence-load';$type=if($cold){Get-KmcChunk7PersistenceType $Request.persistenceCase}else{'Manual'}
 if($d.sha256-isnot[string]-or$d.sha256-cnotmatch'^[a-f0-9]{64}$'){throw 'Inherited archive hash is malformed.'};if(-not$cold){Assert-KmcBootstrapInventoryString $d.sha256 $Request.ownedPairs.sourceArchiveSha256}
 if($type-ceq'Manual'){
  $name=if($cold){'KMC_C7_WORLD'}else{'KMC_C7_ACQUIRED_FIELD'};Assert-KmcBootstrapInventoryString $d.internalName $name
  if($d.fileName-isnot[string]-or$d.fileName-cnotmatch('^Manual_([1-9][0-9]*)_'+$name+'\.zks$')){throw 'Inherited native Manual name/number is not canonical.'}
  $number=0;if(-not[int]::TryParse($Matches[1],[Globalization.NumberStyles]::None,[Globalization.CultureInfo]::InvariantCulture,[ref]$number)-or$number-le0-or$d.fileName-cne('Manual_'+$number.ToString([Globalization.CultureInfo]::InvariantCulture)+'_'+$name+'.zks')){throw 'Inherited native number is out of range.'}
 }else{
  Assert-KmcBootstrapInventoryString $d.fileName ($type+'_1.zks')
  if($d.internalName-isnot[string]-or[string]::IsNullOrWhiteSpace($d.internalName)-or$d.internalName.Length-gt256-or$d.internalName-match'[\x00-\x1f\x7f]'){throw 'Inherited native localized slot name is invalid.'}
 }
 Assert-KmcOrdinaryGuid $d.gameId
 if($d.gameName-isnot[string]-or[string]::IsNullOrWhiteSpace($d.gameName)-or$d.gameName.Length-gt256-or$d.gameName-match'[\x00-\x1f\x7f]'-or$d.area-isnot[string]-or$d.area-cnotmatch'^[a-f0-9]{32}$'-or$d.gameId-ceq$Request.fixture.working.gameId-or$d.sha256-ceq$Request.fixture.working.sha256-or$d.sha256-ceq$Request.fixture.baseline.sha256){throw 'Inherited native campaign must remain distinct from protected Working/Baseline.'}
 if($Request.PSObject.Properties['persistenceCase']-and$Request.persistenceCase-ceq'multi-area-autosave'){Assert-KmcChunk7AreaTarget $Request;$expectedArea=if($cold){$Request.persistenceAreaTarget.area}else{$Request.ownedPairs.sourceArea};Assert-KmcBootstrapInventoryString $d.area $expectedArea}
 Assert-KmcOrdinaryInteger $d.length 1 268435456;Assert-KmcOrdinaryInteger $d.lastWriteTimeUtcTicks 1 ([DateTime]::MaxValue.Ticks)
}
function Assert-KmcChunk7InheritedSnapshot($Request,$Snapshot,[string]$Phase){
 Assert-KmcExactProperties $Snapshot @('contract','phase','sourceRunId','sourceArtifactSha256','sourceArchiveSha256','sourceCase','nativeLoadVerifiedByHost','decodedReceiptAuthorizesRestoration','inheritedPetsPreserved','ownedFactRemovalCount','partyIds','currentPartyIds','currentCharacterIds','remoteCapturedIds','arrangement','pairs','identities') 'inherited native custody'
 foreach($pair in @(@('contract','inherited-two-horse-native-field-v1'),@('phase',$Phase),@('sourceCase','preserve-two-acquired-field'))){Assert-KmcBootstrapInventoryString $Snapshot.($pair[0]) $pair[1]}
 foreach($key in @('sourceRunId','sourceArtifactSha256','sourceArchiveSha256')){Assert-KmcBootstrapInventoryString $Snapshot.$key $Request.ownedPairs.$key}
 foreach($key in @('nativeLoadVerifiedByHost','inheritedPetsPreserved')){Assert-KmcChunk7Boolean $Snapshot.$key $true};Assert-KmcChunk7Boolean $Snapshot.decodedReceiptAuthorizesRestoration $false;Assert-KmcOrdinaryInteger $Snapshot.ownedFactRemovalCount 0 0
 if($Snapshot.partyIds-isnot[Array]-or(ConvertTo-KmcChunk7Canonical @($Snapshot.partyIds|Sort-Object -CaseSensitive))-cne(ConvertTo-KmcChunk7Canonical @($Request.ownedPairs.partyIds|Sort-Object -CaseSensitive))){throw 'Inherited full native roster differs.'}
 foreach($name in @('currentPartyIds','currentCharacterIds','remoteCapturedIds')){if($Snapshot.$name-isnot[Array]){throw 'Current native roster observations must be arrays.'};$seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);foreach($id in $Snapshot.$name){Assert-KmcOrdinaryGuid $id;if(-not$seen.Add($id)){throw 'Current native roster repeats an actor.'}}}
 if((ConvertTo-KmcChunk7Canonical @($Snapshot.currentPartyIds|Sort-Object -CaseSensitive))-cne(ConvertTo-KmcChunk7Canonical @($Snapshot.partyIds|Sort-Object -CaseSensitive))-or$Snapshot.remoteCapturedIds.Count-ne0-or$Snapshot.currentCharacterIds.Count-lt2-or$Snapshot.currentCharacterIds.Count-gt6){throw 'Current field/remote membership differs from inherited custody.'}
 foreach($id in $Snapshot.currentCharacterIds){if($id-cnotin$Snapshot.partyIds){throw 'Native field character is outside the admitted roster.'}}
 foreach($id in @($Request.ownedPairs.riderAId,$Request.ownedPairs.riderBId)){if($id-cnotin$Snapshot.currentCharacterIds){throw 'Inherited rider left the native character roster.'}}
 Assert-KmcExactProperties $Snapshot.identities @('capacity','ids','faults') 'inherited identity registry';Assert-KmcOrdinaryInteger $Snapshot.identities.capacity 128 128;Assert-KmcOrdinaryInteger $Snapshot.identities.faults 0 0
 if($Snapshot.identities.ids-isnot[Array]-or$Snapshot.identities.ids.Count-ne8){throw 'Inherited identity registry requires its exact eight retained objects.'}
 $labels=[Collections.Generic.HashSet[int]]::new();foreach($id in $Snapshot.identities.ids){Assert-KmcManualSeedObjectIdentity $id;if(-not$labels.Add([int]$id)){throw 'Inherited registry label repeats.'}}
 if($Snapshot.pairs-isnot[Array]-or$Snapshot.pairs.Count-ne2){throw 'Inherited snapshot requires prior/new two-pair order.'};$used=[Collections.Generic.HashSet[int]]::new();$ids=Get-KmcChunk7OwnedPairIds $Request
 for($i=0;$i-lt2;$i++){$p=$Snapshot.pairs[$i];Assert-KmcExactProperties $p @('riderId','mountId','rider','mount','feature','rankFact','sourceGuid','rank','profileId') 'inherited pair custody'
  Assert-KmcBootstrapInventoryString $p.riderId $ids[$i*2];Assert-KmcBootstrapInventoryString $p.mountId $ids[$i*2+1]
  foreach($key in @('rider','mount','feature','rankFact')){Assert-KmcManualSeedObjectIdentity $p.$key;if(-not$labels.Contains([int]$p.$key)-or-not$used.Add([int]$p.$key)){throw 'Inherited native roles alias or are unretained.'}}
  if($p.sourceGuid-isnot[string]-or$p.sourceGuid-cnotmatch'^[0-9a-f]{32}$'-or$p.profileId-isnot[string]-or$p.profileId-cnotmatch'^[a-z0-9][a-z0-9.-]{0,127}$'){throw 'Inherited native source/profile token is malformed.'};Assert-KmcOrdinaryInteger $p.rank 1 20
 }
 Assert-KmcExactProperties $Snapshot.arrangement @('money','experienceRatePercent','actors') 'inherited no-arrangement state';Assert-KmcOrdinaryInteger $Snapshot.arrangement.money 0;Assert-KmcOrdinaryInteger $Snapshot.arrangement.experienceRatePercent 100 100
 if($Snapshot.arrangement.actors-isnot[Array]-or$Snapshot.arrangement.actors.Count-ne$Request.ownedPairs.partyIds.Count){throw 'Inherited progression roster differs.'}
 $actors=[Collections.Generic.HashSet[string]]::new();foreach($a in $Snapshot.arrangement.actors){Assert-KmcExactProperties $a @('actorId','blueprint','experience','characterLevel','classes') 'inherited native progression';Assert-KmcOrdinaryGuid $a.actorId;if(-not$actors.Add($a.actorId)-or$a.actorId-cnotin$Request.ownedPairs.partyIds){throw 'Inherited progression actor repeats or is foreign.'}
  if($a.blueprint-isnot[string]-or$a.blueprint-cnotmatch'^[0-9a-f]{32}$'){throw 'Inherited actor blueprint invalid.'};Assert-KmcOrdinaryInteger $a.experience 0 ([int]::MaxValue);Assert-KmcOrdinaryInteger $a.characterLevel 1 20
  if($a.classes-isnot[Array]-or$a.classes.Count-lt1-or$a.classes.Count-gt20){throw 'Inherited native class rows are unbounded.'};$classes=[Collections.Generic.HashSet[string]]::new();$sum=0
  foreach($c in $a.classes){Assert-KmcExactProperties $c @('classGuid','level') 'inherited class';if($c.classGuid-isnot[string]-or$c.classGuid-cnotmatch'^[0-9a-f]{32}$'-or-not$classes.Add($c.classGuid)){throw 'Inherited class repeats or is malformed.'};Assert-KmcOrdinaryInteger $c.level 1 20;$sum+=$c.level};if($sum-ne$a.characterLevel){throw 'Inherited class levels disagree.'}
 }
}
function Assert-KmcChunk7InheritedEvidence($Request,$Observations){
 Assert-KmcChunk7OwnedPairsDescriptor $Request
 Assert-KmcChunk7InheritedSnapshot $Request $Observations.inheritedFixture 'admitted';Assert-KmcChunk7InheritedSnapshot $Request $Observations.inheritedCleanup 'cleanup'
 foreach($key in @('partyIds','currentPartyIds','currentCharacterIds','remoteCapturedIds','arrangement','pairs','identities')){if((ConvertTo-KmcChunk7Canonical $Observations.inheritedFixture.$key)-cne(ConvertTo-KmcChunk7Canonical $Observations.inheritedCleanup.$key)){throw 'Inherited native roster/facts/progression changed across execution and cleanup.'}}
 Assert-KmcChunk7Boolean $Observations.cleanup.inheritedPetsPreserved $true;Assert-KmcChunk7Boolean $Observations.cleanup.nativeProgressionRequiresExternalWorkingRestoration $false
}

function Assert-KmcChunk7OwnedActionFixture($Request,$Observations){
 Assert-KmcChunk7InheritedEvidence $Request $Observations
 Assert-KmcBootstrapInventoryString $Observations.scenario $Request.scenario
 if($null-ne$Observations.workingSha256){throw 'Inherited action proof cannot claim historical Working fixture identity.'}
 if($Observations.actorIds-isnot[Array]-or(ConvertTo-KmcChunk7Canonical $Observations.actorIds)-cne(ConvertTo-KmcChunk7Canonical @(Get-KmcChunk7OwnedPairIds $Request))){throw 'Native action roles differ from qualified inherited pair order.'}
 Assert-KmcOrdinaryGuid $Observations.targetId
 if($Observations.targetId-cin$Request.ownedPairs.partyIds){throw 'Owned hostile target aliases an inherited native party actor.'}
 Assert-KmcBootstrapInventoryString $Observations.targetGroupId ('KMC.RuntimeHostile.'+$Request.runId+':two-pair-target')
}

function Assert-KmcChunk7OwnedArchiveProfiles($Request,$Observations,$Archive){
 foreach($pair in $Archive.Pairs){$rows=@($Observations.inheritedFixture.pairs|Where-Object riderId -CEQ $pair.Rider.Id)
  if($rows.Count-ne1){throw 'Mounted archive rider is outside inherited ownership.'};Assert-KmcBootstrapInventoryString $pair.Mount.Id $rows[0].mountId;Assert-KmcBootstrapInventoryString $pair.ProfileId $rows[0].profileId
 }
}
function Get-KmcChunk7OwnedOutputLeaf($Request,$Observations){
 Assert-KmcChunk7OwnedPairsDescriptor $Request
 if((Get-KmcChunk7OwnedWriteCount $Request)-le0){throw 'Read-only inherited case has no output leaf.'}
 $a=$Observations.ownedOutputAdmission;Assert-KmcOrdinaryInteger $a.number 1 ([int]::MaxValue)
 $type=Get-KmcChunk7PersistenceType $Request.persistenceCase
 $name=if($Request.ownedPairs.case-ceq'persistence-load'){if($Request.persistenceCase-ceq'owned-schema1-resave-manual'){'KMC_C7_SCHEMA1'}else{'KMC_C7_SCHEMA2'}}else{'KMC_C7_WORLD'}
 if($type-ceq'Manual'){
  if($Request.persistenceLoad.fileName-cnotmatch'^Manual_([1-9][0-9]*)_KMC_C7_(ACQUIRED_FIELD|WORLD)[.]zks$'){throw 'Inherited Manual source numbering is not canonical.'}
  $sourceNumber=0;if(-not[int]::TryParse($Matches[1],[ref]$sourceNumber)-or$sourceNumber-eq[int]::MaxValue-or$a.number-ne$sourceNumber+1){throw 'Reserved output is not the exact native next Manual number.'}
  $leaf='Manual_'+([int]$a.number).ToString([Globalization.CultureInfo]::InvariantCulture)+'_'+$name+'.zks'
 }else{if($a.number-ne1){throw 'Fresh native Quick/Auto inventory must select number one.'};$leaf=$type+'_1.zks'}
 Assert-KmcBootstrapInventoryString $a.leaf $leaf;return $leaf
}
function Assert-KmcChunk7OwnedNativeDescriptor($Value,$Descriptor,[string]$Type){
 Assert-KmcExactProperties $Value @('fileName','nativeName','nativeType','gameId','gameName','area','hasFileOnDisk','isActuallySaved','operation','autoLevelup') 'settled inherited native descriptor'
 foreach($pair in @(@('fileName','fileName'),@('nativeName','internalName'),@('gameId','gameId'),@('gameName','gameName'),@('area','area'))){Assert-KmcBootstrapInventoryString $Value.($pair[0]) $Descriptor.($pair[1])}
 Assert-KmcBootstrapInventoryString $Value.nativeType $Type;Assert-KmcBootstrapInventoryString $Value.operation 'None'
 Assert-KmcChunk7Boolean $Value.hasFileOnDisk $true;Assert-KmcChunk7Boolean $Value.isActuallySaved $true;Assert-KmcChunk7Boolean $Value.autoLevelup $false
}
function Assert-KmcChunk7OwnedOutputEvidence($Request,$Observations){
 $leaf=Get-KmcChunk7OwnedOutputLeaf $Request $Observations;$a=$Observations.ownedOutputAdmission;$b=$Observations.ownedOutputCompletion
 Assert-KmcExactProperties $a @('method','token','moduleMvid','number','leaf','nativeInventory','authority') 'inherited native save admission'
 Assert-KmcBootstrapInventoryString $a.method 'FindUnusedSaveNumber';Assert-KmcBootstrapInventoryString $a.token '06008024';Assert-KmcBootstrapInventoryString $a.moduleMvid '07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'
 $worker=$Request.ownedPairs.case-ceq'persistence-save'-and$Request.persistenceCase-ceq'multi-owned-worker-cancel-manual'
 $writeCount=Get-KmcChunk7OwnedWriteCount $Request
 $area=$Request.ownedPairs.case-ceq'persistence-save'-and$Request.persistenceCase-ceq'multi-area-autosave'
 $properties=@('completionSequence','callbackCount','nativeWorkerSettled','nativeInventory','authority');if($worker){$properties+='replacement'};if($area){$properties+='areaSaveIdentity'}
 Assert-KmcExactProperties $b $properties 'inherited native save settlement'
 if($worker){$replacement=$b.replacement;Assert-KmcExactProperties $replacement @('callbackCount','completedSaveCount','drainCount','committed','outcomeEstablished','path','stagingLeaf') 'inherited cancelled replacement';Assert-KmcOrdinaryInteger $replacement.callbackCount 0 0;foreach($key in @('completedSaveCount','drainCount')){Assert-KmcOrdinaryInteger $replacement.$key 1 1};foreach($key in @('committed','outcomeEstablished')){Assert-KmcChunk7Boolean $replacement.$key $true};Assert-KmcBootstrapInventoryString $replacement.path $Observations.nativeWrite.path;Assert-KmcBootstrapInventoryString $replacement.stagingLeaf ('Manual_'+($a.number+1).ToString([Globalization.CultureInfo]::InvariantCulture)+'_KMC_C7_WORLD.zks')}
 Assert-KmcOrdinaryInteger $b.completionSequence 1 1;Assert-KmcOrdinaryInteger $b.callbackCount $(if($area){0}else{1}) $(if($area){0}else{1});Assert-KmcChunk7Boolean $b.nativeWorkerSettled $true
 $type=Get-KmcChunk7PersistenceType $Request.persistenceCase;$name=if($type-ceq'Manual'){if($Request.ownedPairs.case-ceq'persistence-load'){if($Request.persistenceCase-ceq'owned-schema1-resave-manual'){'KMC_C7_SCHEMA1'}else{'KMC_C7_SCHEMA2'}}else{'KMC_C7_WORLD'}}else{$Observations.nativeWrite.nativeName}
 if($name-isnot[string]-or[string]::IsNullOrWhiteSpace($name)-or$name.Length-gt256-or$name-match'[\x00-\x1f\x7f]'){throw 'Native output name must be an exact bounded scalar.'}
 $root=Join-Path (Join-Path (Join-Path (Split-Path (Split-Path $Request.evidenceRoot -Parent) -Parent) 'runtime-staging') ('persistence-'+$Request.runId)) 'Saved Games'
 for($i=0;$i-lt2;$i++){$v=if($i-eq0){$a.authority}else{$b.authority}
  Assert-KmcExactProperties $v @('root','loadCap','writeCap','nativeLoads','nativeWrites','authorizedLoads','authorizedWrites','reservationCount','number','leaf','nativeName','nativeType') 'inherited exact output authority'
  Assert-KmcBootstrapInventoryString $v.root $root;foreach($key in @('loadCap','nativeLoads','authorizedLoads','reservationCount')){Assert-KmcOrdinaryInteger $v.$key 1 1}
  Assert-KmcOrdinaryInteger $v.writeCap $writeCount $writeCount
  $writes=if($i-eq0){0}else{$writeCount};foreach($key in @('nativeWrites','authorizedWrites')){Assert-KmcOrdinaryInteger $v.$key $writes $writes};Assert-KmcOrdinaryInteger $v.number $a.number $a.number
  Assert-KmcBootstrapInventoryString $v.leaf $leaf;Assert-KmcBootstrapInventoryString $v.nativeType $type;Assert-KmcBootstrapInventoryString $v.nativeName $name
 }
 if($a.nativeInventory-isnot[Array]-or$a.nativeInventory.Count-ne1-or$b.nativeInventory-isnot[Array]-or$b.nativeInventory.Count-ne2){throw 'Inherited native inventory did not transition from one immutable input to one fresh output.'}
 $inputType=if($Request.ownedPairs.case-ceq'persistence-load'){Get-KmcChunk7PersistenceType $Request.persistenceCase}else{'Manual'}
 Assert-KmcChunk7OwnedNativeDescriptor $a.nativeInventory[0] $Request.persistenceLoad $inputType
 $input=@($b.nativeInventory|Where-Object fileName -CEQ $Request.persistenceLoad.fileName);$output=@($b.nativeInventory|Where-Object fileName -CEQ $leaf)
 if($input.Count-ne1-or$output.Count-ne1-or$leaf-ceq$Request.persistenceLoad.fileName){throw 'Settled inherited inventory aliases or omits the immutable input/fresh output.'}
 Assert-KmcChunk7OwnedNativeDescriptor $input[0] $Request.persistenceLoad $inputType
 $descriptor=[pscustomobject]@{fileName=$leaf;internalName=$name;gameId=$Request.persistenceLoad.gameId;gameName=$Request.persistenceLoad.gameName;area=(Get-KmcChunk7ExpectedArchiveArea $Request)}
 Assert-KmcChunk7OwnedNativeDescriptor $output[0] $descriptor $type
 Assert-KmcBootstrapInventoryString $Observations.nativeWrite.path (Join-Path $root $leaf)
 Assert-KmcBootstrapInventoryString $Observations.nativeWrite.nativeType $type
 Assert-KmcBootstrapInventoryString $Observations.nativeWrite.nativeName $name
 if($area){Assert-KmcChunk7OwnedAreaSaveIdentity $Request $Observations}
}

# Owner-seed two-pair exploration evidence. The setup disclosure, every assistance
# item, the SETUP-COMPLETE boundary and the fixture-setup (not acquisition) outcome
# per rider are required exactly; returns the observed roles riderA, mountA, riderB, mountB.
function Get-KmcChunk7OwnerSeedDisclosure {'Fixture setup on a disposable run copy of an owner-supplied seed: each Horse was created by the native ordinary Ranger level-up (Hunter''s Bond -> Ranger companion -> KMC Horse) and rider B received only the exact native XP its Ranger 4 eligibility requires. This is not acquisition proof; the only native writes are the game''s own level-up Autos into the run-owned isolated save root during setup, none after SETUP-COMPLETE.'}
# Stock Kingmaker character XP table (BlueprintRoot.Progression.XPTable, the
# Pathfinder medium advancement track): index = character level, threshold = XP.
# Pinned here; the product emits the native table it used and must match exactly.
function Get-KmcChunk7StockXpTable {@(0,0,2000,5000,9000,15000,23000,35000,51000,75000,105000,155000,220000,315000,445000,635000,890000,1300000,1800000,2550000,3600000)}
function Get-KmcChunk7XpReadyLevel([long]$Experience){$t=Get-KmcChunk7StockXpTable;$level=0;while($level-lt20-and$t[$level+1]-le$Experience){$level++};$level}
function Assert-KmcChunk7OwnerSeedEconomy($Value,[string]$Name,$Selection){
 Assert-KmcExactProperties $Value @('money','itemRows','inventorySha256','characterIds') $Name
 Assert-KmcOrdinaryInteger $Value.money 0 ([long]::MaxValue);Assert-KmcOrdinaryInteger $Value.itemRows 0 100000
 if($Value.inventorySha256-isnot[string]-or$Value.inventorySha256-cnotmatch'^[a-f0-9]{64}$'-or$Value.characterIds-isnot[Array]-or$Value.characterIds.Count-lt1){throw ('Owner-seed '+$Name+' is malformed.')}
 # The digested characters are selected party members and include both riders.
 $ids=@($Value.characterIds);foreach($id in $ids){if($id-isnot[string]-or$id-cnotin@($Selection.partyIds)){throw ('Owner-seed '+$Name+' digests a character outside the selected party.')}}
 if(@($ids|Select-Object -Unique).Count-ne$ids.Count-or$Selection.riderAId-cnotin$ids-or$Selection.riderBId-cnotin$ids){throw ('Owner-seed '+$Name+' must digest both riders exactly once.')}
}
# Disclosure only, never compared: each setup Horse's items read from the Horse itself. No
# minimum count and no pinned blueprint/slot: the native layout (incl. any StartingInventory)
# is not yet observed natively. Each listed item must be held by that Horse.
function Assert-KmcChunk7OwnerSeedHorseItems($Value,[string[]]$HorseIds){
 Assert-KmcExactProperties $Value @('horses','note') 'owner-seed setup Horse item disclosure'
 Assert-KmcBootstrapInventoryString $Value.note 'native Horse body/inventory disclosure; not compared; layout not yet observed natively'
 if($Value.horses-isnot[Array]-or$Value.horses.Count-ne$HorseIds.Count){throw 'Owner-seed setup Horse item disclosure must cover each setup Horse.'}
 for($i=0;$i-lt$HorseIds.Count;$i++){
  $h=$Value.horses[$i];Assert-KmcExactProperties $h @('horseId','sharedCollection','items') 'owner-seed setup Horse items';Assert-KmcBootstrapInventoryString $h.horseId $HorseIds[$i]
  if($h.sharedCollection-isnot[bool]-or$h.items-isnot[Array]-or$h.items.Count-gt256){throw 'Owner-seed setup Horse item disclosure is malformed.'}
  foreach($item in $h.items){
   Assert-KmcExactProperties $item @('blueprint','slot','count','heldBy') 'owner-seed setup Horse item'
   if($item.blueprint-isnot[string]-or[string]::IsNullOrWhiteSpace($item.blueprint)-or$item.slot-isnot[string]-or[string]::IsNullOrWhiteSpace($item.slot)){throw 'Owner-seed setup Horse item identity is malformed.'}
   Assert-KmcOrdinaryInteger $item.count 1 100000;Assert-KmcBootstrapInventoryString $item.heldBy $HorseIds[$i]
  }
 }
}
function Assert-KmcChunk7OwnerSeedWorldSetup($Request,$Observations){
 Assert-KmcChunk7OwnedPairsDescriptor $Request
 if(-not(Test-KmcChunk7OwnedPairsOwnerSeed $Request)){throw 'Owner-seed setup evidence belongs only to the owner-seed exploration kind.'}
 $s=$Request.ownedPairs;$o=$Observations
 foreach($key in @('inheritedFixture','inheritedCleanup','acquisition','failurePhase')){if($null-ne$o.PSObject.Properties[$key]){throw ('Owner-seed evidence cannot carry '+$key+'.')}}
 $seed=$o.ownerSeed;Assert-KmcExactProperties $seed @('contract','seedId','seedSha256','riderAId','riderBId','nativeLoadVerifiedByHost','fixtureSetupOnly','acquisitionProof') 'owner-seed identity'
 foreach($pair in @(@('contract','owner-seed-two-pair-exploration-v1'),@('seedId',$s.seedId),@('seedSha256',$s.seedSha256),@('riderAId',$s.riderAId),@('riderBId',$s.riderBId))){Assert-KmcBootstrapInventoryString $seed.($pair[0]) $pair[1]}
 Assert-KmcChunk7Boolean $seed.nativeLoadVerifiedByHost $true;Assert-KmcChunk7Boolean $seed.fixtureSetupOnly $true;Assert-KmcChunk7Boolean $seed.acquisitionProof $false
 $c=$o.capability;Assert-KmcExactProperties $c @('archiveNativeEnabled','nativeWriterSchema','nativePairCapacity','workingSha256','inheritedManualSha256','ownerSeedSha256') 'owner-seed capability'
 Assert-KmcChunk7Boolean $c.archiveNativeEnabled $true;Assert-KmcChunk7WorldCounters $c @('nativeWriterSchema','nativePairCapacity')
 if($c.nativeWriterSchema-ne3-or$c.nativePairCapacity-lt2-or$null-ne$c.workingSha256-or$null-ne$c.inheritedManualSha256){throw 'Owner-seed capability differs or claims Working/inherited provenance.'}
 Assert-KmcBootstrapInventoryString $c.ownerSeedSha256 $s.seedSha256
 $u=$o.ownerSeedSetup
 Assert-KmcExactProperties $u @('contract','disclosure','seedId','seedSha256','fixtureSetupOnly','acquisitionProof','producerAncestry','loadedArchiveKind','loadedArchiveCanRestore','riderFacts','nativeLevelUpAutosave','riders','events','setupAutosaves','economyBefore','nativeXpTable','admissionRefusals','eventCount','eventsTruncated','riderBExperience','assistance') 'owner-seed fixture setup disclosure'
 $table=Get-KmcChunk7StockXpTable
 if($u.nativeXpTable-isnot[Array]-or$u.nativeXpTable.Count-ne20){throw 'Owner-seed native XP table observation is missing.'}
 for($level=1;$level-le20;$level++){Assert-KmcOrdinaryInteger $u.nativeXpTable[$level-1] $table[$level] $table[$level]}
 Assert-KmcChunk7OwnerSeedEconomy $u.economyBefore 'economy before setup' $s
 Assert-KmcBootstrapInventoryString $u.contract 'owner-seed-fixture-setup-v1';Assert-KmcBootstrapInventoryString $u.disclosure (Get-KmcChunk7OwnerSeedDisclosure)
 Assert-KmcBootstrapInventoryString $u.seedId $s.seedId;Assert-KmcBootstrapInventoryString $u.seedSha256 $s.seedSha256;Assert-KmcBootstrapInventoryString $u.loadedArchiveKind 'Missing'
 Assert-KmcChunk7Boolean $u.fixtureSetupOnly $true;foreach($key in @('acquisitionProof','producerAncestry','loadedArchiveCanRestore','eventsTruncated')){Assert-KmcChunk7Boolean $u.$key $false}
 if($u.admissionRefusals-isnot[Array]-or$u.admissionRefusals.Count-ne0){throw 'Owner-seed setup recorded an admission refusal.'}
 $a=$u.nativeLevelUpAutosave;Assert-KmcExactProperties $a @('autosaveEnabled','onlyOneSave','autosaveSlots','settingsWritten','case','riderBuilds','expectedWrites','selectedWrites','slotProjectionDuringBuild','mountedPersistenceProof') 'native level-up autosave observation'
 if($a.autosaveEnabled-isnot[bool]){throw 'Native autosave setting must be an observed boolean.'}
 foreach($key in @('onlyOneSave','settingsWritten','mountedPersistenceProof')){Assert-KmcChunk7Boolean $a.$key $false}
 if(-not(Test-KmcChunk7FiniteNumber $a.autosaveSlots)){throw 'Native autosave slot setting must be a finite observation.'}
 Assert-KmcOrdinaryInteger $a.riderBuilds 2 2;$writesExpected=if($a.autosaveEnabled){[int]$a.riderBuilds}else{0}
 Assert-KmcBootstrapInventoryString $a.case $(if($a.autosaveEnabled){'autosave-on'}else{'autosave-off'})
 Assert-KmcOrdinaryInteger $a.expectedWrites $writesExpected $writesExpected;Assert-KmcOrdinaryInteger $a.selectedWrites $writesExpected $writesExpected
 if([int]$s.expectedNativeWriteCount-ne$writesExpected){throw 'Selected native write count differs from the observed native level-up autosave count.'}
 Assert-KmcChunk7Boolean $a.slotProjectionDuringBuild ($writesExpected-gt0)
 $autos=@($u.setupAutosaves);if($u.setupAutosaves-isnot[Array]-or$autos.Count-ne$a.riderBuilds){throw 'Owner-seed setup must observe exactly one native level-up autosave request per rider build.'}
 $saveRoot=Get-KmcAssistedSaveRoot $Request;$autoPath=Join-Path $saveRoot 'Auto_1.zks'
 for($i=0;$i-lt$autos.Count;$i++){
  $w=$autos[$i];$rid=if($i-eq0){$s.riderAId}else{$s.riderBId}
  if($a.autosaveEnabled){
   Assert-KmcExactProperties $w @('sequence','riderId','case','phase','type','autoLevelup','internalName','fileName','preparedLeaf','path','insideRunRoot','length','sha256','lastWriteTimeUtcTicks','completionSequence','requestedObject','writtenObject','admittedWrites','authorizedWrites','snapshots','completedSaves','frame','completedBeforeBoundary','mountedPersistenceProof','probe') 'owner-seed setup level-up Auto'
   foreach($pair in @(@('riderId',$rid),@('case','autosave-on'),@('phase','setup-levelup-auto'),@('type','Auto'),@('fileName','Auto_1.zks'),@('preparedLeaf',@('Auto_1.zks','Auto_2.zks')[$i]),@('path',$autoPath))){Assert-KmcBootstrapInventoryString $w.($pair[0]) $pair[1]}
   if([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($w.path))-cne[IO.Path]::GetFullPath($saveRoot)){throw 'Setup level-up Auto escaped the run-owned isolated save root.'}
   foreach($flag in @('autoLevelup','insideRunRoot','completedBeforeBoundary')){Assert-KmcChunk7Boolean $w.$flag $true};Assert-KmcChunk7Boolean $w.mountedPersistenceProof $false
   if($w.sha256-isnot[string]-or$w.sha256-cnotmatch'^[a-f0-9]{64}$'-or$w.internalName-isnot[string]-or[string]::IsNullOrWhiteSpace($w.internalName)){throw 'Setup level-up Auto identity is malformed.'}
   foreach($key in @('sequence','completionSequence','admittedWrites','authorizedWrites','snapshots','completedSaves')){Assert-KmcOrdinaryInteger $w.$key ($i+1) ($i+1)}
   Assert-KmcOrdinaryInteger $w.length 1 268435456;Assert-KmcOrdinaryInteger $w.frame 0 ([int]::MaxValue)
   if($i-gt0-and$w.internalName-cne$autos[0].internalName){throw 'Setup level-up Autos changed their native slot name.'}
  }else{
   Assert-KmcExactProperties $w @('sequence','riderId','case','requested','written','probe') 'owner-seed suppressed level-up autosave'
   Assert-KmcOrdinaryInteger $w.sequence ($i+1) ($i+1);Assert-KmcBootstrapInventoryString $w.riderId $rid;Assert-KmcBootstrapInventoryString $w.case 'autosave-off'
   Assert-KmcChunk7Boolean $w.requested $true;Assert-KmcChunk7Boolean $w.written $false
  }
 }
 if($u.events-isnot[Array]-or$u.events.Count-lt2-or$u.events.Count-gt768){throw 'Owner-seed native build events are missing or unbounded.'};Assert-KmcOrdinaryInteger $u.eventCount $u.events.Count $u.events.Count
 if($u.riderFacts-isnot[Array]-or$u.riderFacts.Count-ne2-or$u.riders-isnot[Array]-or$u.riders.Count-ne2){throw 'Owner-seed setup must disclose exactly two riders.'}
 $party=@($s.partyIds);$horses=@();$roles=@()
 for($i=0;$i-lt2;$i++){
  $role=@('A','B')[$i];$id=if($i-eq0){$s.riderAId}else{$s.riderBId};$f=$u.riderFacts[$i];$r=$u.riders[$i]
  Assert-KmcExactProperties $f @('role','riderId','size','originalSize','polymorphed','controllable','petless','characterLevel','rangerLevel','experience','readyLevel','eligibleWithoutAssistance','minimalTargetLevel') 'owner-seed rider facts'
  Assert-KmcBootstrapInventoryString $f.role $role;Assert-KmcBootstrapInventoryString $f.riderId $id;Assert-KmcBootstrapInventoryString $f.size 'Medium';Assert-KmcBootstrapInventoryString $f.originalSize 'Medium'
  Assert-KmcChunk7Boolean $f.polymorphed $false;Assert-KmcChunk7Boolean $f.controllable $true;Assert-KmcChunk7Boolean $f.petless $true
  foreach($key in @('characterLevel','rangerLevel','experience','readyLevel','minimalTargetLevel')){Assert-KmcOrdinaryInteger $f.$key 0 1000000000}
  if($f.eligibleWithoutAssistance-isnot[bool]-or$f.eligibleWithoutAssistance-ne(($f.rangerLevel+$f.readyLevel-$f.characterLevel)-ge4)-or$f.minimalTargetLevel-ne($f.characterLevel+4-$f.rangerLevel)){throw 'Owner-seed eligibility arithmetic differs from the ordinary driver rule.'}
  if($i-eq0-and-not$f.eligibleWithoutAssistance){throw 'Rider A received setup without its own native eligibility.'}
  Assert-KmcExactProperties $r @('role','riderId','before','after','nativeCommits','nativeChoices','nativeAutoLevelupOverrides','horseId','horseBlueprint','horseMasterId','horseFeatureSourceGuid','presentation','acquisitionProof','profileId','rigValidated') 'owner-seed rider setup outcome'
  Assert-KmcBootstrapInventoryString $r.role $role;Assert-KmcBootstrapInventoryString $r.riderId $id;Assert-KmcBootstrapInventoryString $r.horseMasterId $id
  Assert-KmcBootstrapInventoryString $r.horseBlueprint '4016c7db400ab721ff125aef9e65e202';Assert-KmcBootstrapInventoryString $r.profileId 'medium-humanoid-horse-v1'
  Assert-KmcBootstrapInventoryString $r.presentation 'fixture-setup';Assert-KmcChunk7Boolean $r.acquisitionProof $false;Assert-KmcChunk7Boolean $r.rigValidated $true
  Assert-KmcOrdinaryGuid $r.horseId;if($r.horseId-cin$party-or$r.horseId-cin$horses){throw 'Owner-seed setup Horse aliases a party actor or the other Horse.'};$horses+=$r.horseId
  if($r.horseFeatureSourceGuid-isnot[string]-or$r.horseFeatureSourceGuid-cnotmatch'^[a-f0-9]{32}$'){throw 'Owner-seed Horse feature lacks its native progression source.'}
  Assert-KmcOrdinaryInteger $r.nativeCommits 1 20;Assert-KmcOrdinaryInteger $r.nativeChoices 1 512;Assert-KmcOrdinaryInteger $r.nativeAutoLevelupOverrides 0 20
  foreach($state in @($r.before,$r.after)){Assert-KmcExactProperties $state @('characterLevel','experience','rangerLevel','classes') 'owner-seed rider progression';foreach($key in @('characterLevel','experience','rangerLevel')){Assert-KmcOrdinaryInteger $state.$key 0 1000000000}}
  if($r.after.rangerLevel-ne4-or$r.after.characterLevel-ne($r.before.characterLevel+$r.nativeCommits)-or$r.before.characterLevel-ne$f.characterLevel){throw 'Owner-seed rider did not reach exactly Ranger 4 through exactly its native commits.'}
  # Pinned XP table: reported ready levels and the final level follow the stock thresholds; no extra levels.
  if($f.readyLevel-ne(Get-KmcChunk7XpReadyLevel $f.experience)-or$r.after.characterLevel-ne(Get-KmcChunk7XpReadyLevel $r.after.experience)-or$r.after.experience-ne$r.before.experience){throw 'Owner-seed rider levels differ from the stock XP table or exceed the XP-ready level.'}
  $roles+=@($id,$r.horseId)
 }
 $x=$u.riderBExperience;$assist=$s.xpAssistance
 if($null-eq$assist){
  Assert-KmcExactProperties $x @('assisted','experience') 'rider B unassisted experience';Assert-KmcChunk7Boolean $x.assisted $false
  if(-not$u.riderFacts[1].eligibleWithoutAssistance){throw 'Rider B needed assistance that was not selected.'}
 }else{
  Assert-KmcExactProperties $x @('assisted','targetCharacterLevel','before','after','delta','threshold','characterLevelBefore','characterLevelAfter','levelReadyAfter','readyLevelAfter') 'rider B native XP assistance'
  Assert-KmcChunk7Boolean $x.assisted $true;Assert-KmcChunk7Boolean $x.levelReadyAfter $true
  foreach($key in @('before','after','delta','threshold','characterLevelBefore','characterLevelAfter','readyLevelAfter')){Assert-KmcOrdinaryInteger $x.$key 0 1000000000}
  if($u.riderFacts[1].eligibleWithoutAssistance-or$x.targetCharacterLevel-ne$assist.targetCharacterLevel-or$assist.targetCharacterLevel-ne$u.riderFacts[1].minimalTargetLevel-or
     $x.delta-le0-or$x.after-ne($x.before+$x.delta)-or$x.after-ne$x.threshold-or$x.characterLevelAfter-ne$x.characterLevelBefore-or$x.readyLevelAfter-ne$assist.targetCharacterLevel-or
     $x.before-ne$u.riderFacts[1].experience-or$u.riders[1].before.experience-ne$x.after){throw 'Rider B XP assistance exceeds or differs from the exact minimal eligibility it requires.'}
 # Pinned stock XP table: threshold is the target level's value, 'before' lies in the current level's band.
 $target=[int]$assist.targetCharacterLevel;$cl=[int]$x.characterLevelBefore
 if($cl-lt1-or$cl-ge20-or$x.threshold-ne$table[$target]-or$x.before-lt$table[$cl]-or$x.before-ge$table[$cl+1]-or$x.delta-ne($table[$target]-$x.before)-or
    $x.after-ne$table[$target]-or$u.riders[1].after.characterLevel-ne$target-or$u.riders[1].after.rangerLevel-ne4){throw 'Rider B XP assistance differs from the stock XP table minimum or ends beyond its exact target level.'}
 }
 # Ledger order: A build, [A level-up Auto], [B XP], B build, [B level-up Auto].
 $expected=@();$units=@();$riderOf=@()
 foreach($step in @(@('native-ordinary-ranger-levels',$s.riderAId,0),@('native-levelup-autosave',$s.riderAId,0),@('native-xp',$s.riderBId,1),@('native-ordinary-ranger-levels',$s.riderBId,1),@('native-levelup-autosave',$s.riderBId,1))){
  if($step[0]-ceq'native-levelup-autosave'-and$writesExpected-eq0){continue};if($step[0]-ceq'native-xp'-and$null-eq$assist){continue}
  $expected+=$step[0];$units+=$step[1];$riderOf+=$step[2]
 }
 $items=@($u.assistance)
 if($u.assistance-isnot[Array]-or$items.Count-ne$expected.Count){throw 'Owner-seed assistance disclosure is incomplete or contains an undeclared setup item.'}
 for($i=0;$i-lt$items.Count;$i++){
  $v=$items[$i];Assert-KmcExactProperties $v @('unitId','kind','nativeApi','before','after','amount','detail') 'owner-seed assistance item'
  Assert-KmcBootstrapInventoryString $v.kind $expected[$i];Assert-KmcBootstrapInventoryString $v.unitId $units[$i]
  if($v.nativeApi-isnot[string]-or[string]::IsNullOrWhiteSpace($v.nativeApi)-or$v.detail-isnot[string]){throw 'Owner-seed assistance must name its native API.'}
  foreach($key in @('before','after','amount')){Assert-KmcOrdinaryInteger $v.$key 0 1000000000}
  if($v.kind-ceq'native-xp'){if($v.nativeApi-cne'Kingmaker.UnitLogic.UnitProgressionData.GainExperience(int, log: false)'-or$v.before-ne$x.before-or$v.after-ne$x.after-or$v.amount-ne$x.delta){throw 'Owner-seed XP assistance item differs from its observation.'}}
  elseif($v.kind-ceq'native-levelup-autosave'){$w=$autos[$riderOf[$i]];if($v.nativeApi-cne'Kingmaker.Controllers.LevelUpAutoSaveController.HandleLevelUpComplete -> Game.SaveGame(Auto) into the run-owned isolated save root'-or$v.before-ne$riderOf[$i]-or$v.after-ne($riderOf[$i]+1)-or$v.amount-ne1-or$v.detail-cne$w.path){throw 'Owner-seed level-up Auto assistance item differs from its observed write.'}}
  else{$row=$u.riders[$riderOf[$i]];if($v.before-ne$row.before.characterLevel-or$v.after-ne$row.after.characterLevel-or$v.amount-ne$row.nativeCommits){throw 'Owner-seed build assistance item differs from its rider outcome.'}}
 }
 $b=$o.setupComplete;Assert-KmcExactProperties $b @('boundary','frame','gameTicks','assistanceCount','setupSeconds','measuredClockRestarted','state','writesAtBoundary','writeScopeClosed','economy','horseItems') 'SETUP-COMPLETE boundary'
 Assert-KmcChunk7OwnerSeedEconomy $b.economy 'economy at SETUP-COMPLETE' $s
 if($b.state-eq$null-or[long]$b.economy.money-ne[long]$b.state.money){throw 'Owner-seed boundary economy money differs from the boundary state.'}
 Assert-KmcChunk7OwnerSeedHorseItems $b.horseItems @($u.riders[0].horseId,$u.riders[1].horseId)
 if((ConvertTo-KmcChunk7Canonical $b.economy)-cne(ConvertTo-KmcChunk7Canonical $u.economyBefore)){throw 'Owner-seed setup changed party money or the party item inventory.'}
 # SETUP-COMPLETE precedes the first measured observation.
 $firstMeasured=@($o.measurements)[0];$firstInput=@($o.inputs)[0]
 if($null-eq$firstMeasured-or$null-eq$firstInput-or[long]$b.frame-gt[long]$firstMeasured.startFrame-or[long]$b.gameTicks-gt[long]$firstInput.resourcesBefore.gameTicks){throw 'SETUP-COMPLETE does not precede the measured phase.'}
 Assert-KmcOrdinaryInteger $b.writesAtBoundary $writesExpected $writesExpected;Assert-KmcChunk7Boolean $b.writeScopeClosed $true
 foreach($w in $autos){if($a.autosaveEnabled-and[long]$w.frame-gt[long]$b.frame){throw 'A setup level-up Auto completed after SETUP-COMPLETE.'}}
 Assert-KmcBootstrapInventoryString $b.boundary 'SETUP-COMPLETE';Assert-KmcOrdinaryInteger $b.frame 0 ([int]::MaxValue);Assert-KmcOrdinaryInteger $b.gameTicks 0 ([long]::MaxValue);Assert-KmcOrdinaryInteger $b.assistanceCount $items.Count $items.Count
 Assert-KmcChunk7Boolean $b.measuredClockRestarted $true;if(-not(Test-KmcChunk7FiniteNumber $b.setupSeconds)-or$b.setupSeconds-lt0){throw 'Owner-seed setup duration is not a finite observation.'}
 if($b.state-eq$null-or$b.state.pairs-isnot[Array]-or$b.state.pairs.Count-ne2){throw 'SETUP-COMPLETE lacks its captured setup state.'}
 for($i=0;$i-lt2;$i++){$p=$b.state.pairs[$i];if($p.riderId-cne$roles[$i*2]-or$p.petId-cne$roles[$i*2+1]-or$p.petMasterId-cne$roles[$i*2]){throw 'SETUP-COMPLETE state differs from the disclosed setup pairs.'}}
 Assert-KmcChunk7ExactIds @($o.partyBefore) $party
 foreach($id in @($s.riderAId,$s.riderBId)){$row=@($o.partyBefore|Where-Object id -CEQ $id);if($row.Count-ne1-or$null-ne$row[0].petId){throw 'Owner-seed rider was not petless before setup.'}}
 Assert-KmcChunk7ExactIds @($o.partyWithHorse) @($party+$horses)
 for($i=0;$i-lt2;$i++){$rider=@($o.partyWithHorse|Where-Object id -CEQ $roles[$i*2]);$horse=@($o.partyWithHorse|Where-Object id -CEQ $roles[$i*2+1]);if($rider.Count-ne1-or$horse.Count-ne1-or$rider[0].petId-cne$horse[0].id-or$horse[0].masterId-cne$rider[0].id-or$horse[0].blueprint-cne'4016c7db400ab721ff125aef9e65e202'){throw 'Owner-seed setup Horse reciprocity differs.'}}
 return ,@($roles)
}
function Assert-KmcChunk7DoorNotApplicable($Door){
 Assert-KmcExactProperties $Door @('applicable','reason','candidatesScanned','radius') 'not-applicable native door'
 Assert-KmcChunk7Boolean $Door.applicable $false
 if($Door.reason-isnot[string]-or[string]::IsNullOrWhiteSpace($Door.reason)-or$Door.reason.Length-gt512){throw 'Not-applicable door lacks its observed reason.'}
 Assert-KmcOrdinaryInteger $Door.candidatesScanned 0 100000
 if(-not(Test-KmcChunk7FiniteNumber $Door.radius)-or[double]$Door.radius-ne28.0){throw 'Not-applicable door scan radius differs from the exact probe radius.'}
}
function Assert-KmcChunk7OwnerSeedCleanup($Cleanup){
 foreach($field in @('pairResidueAbsent','controlsSettled','settingsAndSelectionRestored','nativeProcessesSettled','cleanupCustodySettled','setupCompleted','ownerSeedSetupPetsPreserved','setupStateUnchangedAfterBoundary','setupRetainedInMemoryOnly')){Assert-KmcChunk7Boolean $Cleanup.$field $true}
 foreach($field in @('horseRemoved','inheritedPetsPreserved','nativeProgressionRequiresExternalWorkingRestoration','nativeSaveWritten','ordinaryRespecProven')){Assert-KmcChunk7Boolean $Cleanup.$field $false}
 Assert-KmcOrdinaryInteger $Cleanup.setupMutationsAfterBoundary 0 0
 # Zero native writes after SETUP-COMPLETE; none unauthorized; the owned projection released.
 Assert-KmcOrdinaryInteger $Cleanup.writesAfterBoundary 0 0;Assert-KmcOrdinaryInteger $Cleanup.unauthorizedWrites 0 0;Assert-KmcChunk7Boolean $Cleanup.slotProjectionReleased $true
}
