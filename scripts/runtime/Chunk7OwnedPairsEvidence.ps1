Set-StrictMode -Version Latest
function Test-KmcChunk7OwnedPairsScenario([string]$Scenario){$Scenario-ceq'chunk7-owned-two-pair-campaign'}
function Get-KmcChunk7SelectedScenario($Request){
 if(-not(Test-KmcChunk7OwnedPairsScenario $Request.scenario)){return [string]$Request.scenario}
 Assert-KmcChunk7OwnedPairsSelection $Request
 switch -CaseSensitive ($Request.ownedPairs.case){
  'exploration'{'chunk7-two-pair-exploration'}
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
 if($Request.ownedPairs.case-ceq'persistence-save'-and$Request.persistenceCase-ceq'multi-owned-worker-cancel-manual'){return 2}
 if($Request.ownedPairs.case-ceq'persistence-save'-or($Request.ownedPairs.case-ceq'persistence-load'-and$Request.persistenceCase-ceq'legacy-mounted-pending-tb-manual')){return 1};return 0
}
function Get-KmcChunk7OwnedLoadCount($Request){
 if($Request.ownedPairs.case-ceq'persistence-load'-and$Request.persistenceCase-ceq'multi-exploration-three-loads-manual'){return 3};return 1
}
function Get-KmcChunk7OwnedPairIds($Request){@($Request.ownedPairs.riderAId,$Request.ownedPairs.mountAId,$Request.ownedPairs.riderBId,$Request.ownedPairs.mountBId)}
function Get-KmcChunk7SelectedFixture($Request){if(Test-KmcChunk7OwnedPairsScenario $Request.scenario){return $Request.persistenceLoad};return $Request.fixture.working}
function Assert-KmcChunk7OwnedPairsSelection($Request){
 $has=$null-ne$Request.PSObject.Properties['ownedPairs']
 if(-not(Test-KmcChunk7OwnedPairsScenario $Request.scenario)){if($has){throw 'Inherited two-owner selection is forbidden under another family.'};return}
 if(-not$has){throw 'Inherited two-owner campaign requires its explicit selection.'};$s=$Request.ownedPairs
 Assert-KmcExactProperties $s @('case','sourceRunId','sourceArtifactSha256','sourceArchiveSha256','checkpointSourceRunId','checkpointSourceArtifactSha256','riderAId','mountAId','riderBId','mountBId','partyIds','baseGameOnly','expectedNativeLoadCount','expectedNativeWriteCount') 'inherited native pair selection'
 if($s.case-isnot[string]-or$s.case-cnotin@('exploration','combat-rider-first','combat-mount-first','realtime-core','realtime-casting','realtime-charge','native-life-cleanup','persistence-save','persistence-load')){throw 'Inherited pair case is not closed.'}
 if($s.sourceRunId-isnot[string]-or$s.sourceRunId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'-or$s.sourceRunId-ceq$Request.runId){throw 'Inherited pair source run must be distinct and exact.'}
 foreach($key in @('sourceArtifactSha256','sourceArchiveSha256')){if($s.$key-isnot[string]-or$s.$key-cnotmatch'^[a-f0-9]{64}$'){throw 'Inherited source requires exact SHA256 pins.'}}
 $ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);foreach($key in @('riderAId','mountAId','riderBId','mountBId')){Assert-KmcOrdinaryGuid $s.$key;if(-not$ids.Add($s.$key)){throw 'Inherited role IDs collide.'}}
 if($s.partyIds-isnot[Array]-or$s.partyIds.Count-lt4-or$s.partyIds.Count-gt12){throw 'Complete inherited field roster must be bounded.'}
 $party=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);foreach($id in $s.partyIds){Assert-KmcOrdinaryGuid $id;if(-not$party.Add($id)){throw 'Inherited roster repeats an actor.'}}
 foreach($id in $ids){if(-not$party.Contains($id)){throw 'Inherited roster omits a selected pair role.'}}
 $checkpoint=$null;if($null-ne$Request.PSObject.Properties['persistenceCase']){$checkpoint=$Request.persistenceCase}
 if($s.case-cin@('persistence-save','persistence-load')){if($checkpoint-isnot[string]-or($checkpoint-cnotin@('multi-exploration-manual','multi-exploration-quick','multi-exploration-auto','multi-rt-debt-manual','multi-tb-debt-manual','zero-mounted-tb-retained-manual','legacy-mounted-pending-tb-manual')-and($s.case-cne'persistence-load'-or$checkpoint-cnotin@('multi-exploration-three-loads-manual','multi-exploration-three-area-transfers-manual'))-and($s.case-cne'persistence-save'-or$checkpoint-cnotin@('multi-nonselected-mount-unsettled-manual','multi-owned-worker-cancel-manual')))){throw 'Inherited persistence retains the seven selected core checkpoints; save boundaries are source-only.'}}
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
 $name=if($Request.ownedPairs.case-ceq'persistence-load'){'KMC_C7_SCHEMA2'}else{'KMC_C7_WORLD'}
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
 $properties=@('completionSequence','callbackCount','nativeWorkerSettled','nativeInventory','authority');if($worker){$properties+='replacement'}
 Assert-KmcExactProperties $b $properties 'inherited native save settlement'
 if($worker){$replacement=$b.replacement;Assert-KmcExactProperties $replacement @('callbackCount','completedSaveCount','drainCount','committed','outcomeEstablished','path','stagingLeaf') 'inherited cancelled replacement';Assert-KmcOrdinaryInteger $replacement.callbackCount 0 0;foreach($key in @('completedSaveCount','drainCount')){Assert-KmcOrdinaryInteger $replacement.$key 1 1};foreach($key in @('committed','outcomeEstablished')){Assert-KmcChunk7Boolean $replacement.$key $true};Assert-KmcBootstrapInventoryString $replacement.path $Observations.nativeWrite.path;Assert-KmcBootstrapInventoryString $replacement.stagingLeaf ('Manual_'+($a.number+1).ToString([Globalization.CultureInfo]::InvariantCulture)+'_KMC_C7_WORLD.zks')}
 Assert-KmcOrdinaryInteger $b.completionSequence 1 1;Assert-KmcOrdinaryInteger $b.callbackCount 1 1;Assert-KmcChunk7Boolean $b.nativeWorkerSettled $true
 $type=Get-KmcChunk7PersistenceType $Request.persistenceCase;$name=if($type-ceq'Manual'){if($Request.ownedPairs.case-ceq'persistence-load'){'KMC_C7_SCHEMA2'}else{'KMC_C7_WORLD'}}else{$Observations.nativeWrite.nativeName}
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
 $descriptor=[pscustomobject]@{fileName=$leaf;internalName=$name;gameId=$Request.persistenceLoad.gameId;gameName=$Request.persistenceLoad.gameName;area=$Request.persistenceLoad.area}
 Assert-KmcChunk7OwnedNativeDescriptor $output[0] $descriptor $type
 Assert-KmcBootstrapInventoryString $Observations.nativeWrite.path (Join-Path $root $leaf)
 Assert-KmcBootstrapInventoryString $Observations.nativeWrite.nativeType $type
 Assert-KmcBootstrapInventoryString $Observations.nativeWrite.nativeName $name
}
