param([string]$ReaderDirectory,[string]$FixturePath,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if(-not$ReaderDirectory){$ReaderDirectory=Join-Path $repo 'scripts/runtime'}
if(-not$FixturePath){$FixturePath=Join-Path $repo 'tests/Chunk7Diagnostics/OwnedPairsEvidenceFixtures.json'}
. (Join-Path $ReaderDirectory 'RuntimeHarness.Common.ps1')
if(-not$OutputDirectory){$OutputDirectory=Join-Path (Get-KmcLabRoot) ('analysis-cache/chunk7-owned-readers-'+[Guid]::NewGuid().ToString('N'))}
$clock=[Diagnostics.Stopwatch]::StartNew();$results=[Collections.Generic.List[object]]::new()
$fixtures=(Get-Content -LiteralPath $FixturePath -Raw|ConvertFrom-Json).fixtures
function Copy-Value($v){$v|ConvertTo-Json -Depth 90|ConvertFrom-Json}
function Invoke-OwnedReader($Value){$r=$Value.request;$v=$Value.artifact;switch -Regex ($Value.name){'^exploration$'{Assert-KmcChunk7WorldEvidence $r $v 'PASS';break}'^combat-'{Assert-KmcChunk7CombatEvidence $r $v 'PASS';break}'^realtime-'{Assert-KmcChunk7RealtimeEvidence $r $v 'PASS';break}'^native-life-'{Assert-KmcChunk7NativeLifeEvidence $r $v 'PASS';break}'^persistence-'{Assert-KmcChunk7PersistenceObservation $r $v 'PASS';break}default{throw 'Unknown fixture name'}}}
function Check([string]$Name,[scriptblock]$Body,[bool]$Refuse=$false,[string]$Pattern=''){$errorText=$null;$pass=$false;try{&$Body;$pass=-not$Refuse}catch{$errorText=$_.Exception.Message;$pass=$Refuse-and(-not$Pattern-or$errorText-match$Pattern)};$results.Add([pscustomobject]@{name=$Name;pass=$pass;expectedRefusal=$Refuse;error=$errorText});if(-not$pass){Write-Output ('FAIL '+$Name+': '+$errorText)}}
function Mutate([string]$Name,[string]$Case,[scriptblock]$Mutation,[string]$Pattern=''){$v=Copy-Value @($fixtures|Where-Object name -CEQ $Case)[0];&$Mutation $v;Check $Name {Invoke-OwnedReader $v} $true $Pattern}
foreach($f in $fixtures){Check ('complete '+$f.name) {Invoke-OwnedReader $f};$v=Copy-Value $f;$v.artifact.observations.inheritedCleanup.arrangement.money++;Check ('no hidden arrangement '+$f.name) {Invoke-OwnedReader $v} $true 'progression changed';$v=Copy-Value $f;$v.artifact.observations.inheritedCleanup.currentPartyIds=@($v.artifact.observations.inheritedCleanup.currentPartyIds|Select-Object -Skip 1);Check ('full live roster '+$f.name) {Invoke-OwnedReader $v} $true 'field/remote membership'}
Mutate 'role collision' 'exploration' {$args[0].request.ownedPairs.mountBId=$args[0].request.ownedPairs.mountAId} 'role IDs collide'
Mutate 'undeclared role absent' 'exploration' {$args[0].request.ownedPairs.partyIds=@($args[0].request.ownedPairs.partyIds|Select-Object -Skip 1)} 'roster must be bounded'
Mutate 'source artifact mismatch' 'exploration' {$args[0].request.ownedPairs.sourceArtifactSha256='f'*64}
Mutate 'inherited pet removal refused' 'exploration' {$args[0].artifact.observations.cleanup.horseRemoved=$true}
Mutate 'inherited fact removal refused' 'exploration' {$args[0].artifact.observations.inheritedCleanup.ownedFactRemovalCount=1}
Mutate 'source does not grant restoration' 'exploration' {$args[0].artifact.observations.inheritedFixture.decodedReceiptAuthorizesRestoration=$true}
Mutate 'remote captured actor refused' 'exploration' {$args[0].artifact.observations.inheritedCleanup.remoteCapturedIds=@($args[0].request.ownedPairs.riderAId)} 'field/remote membership'
Mutate 'character membership loss refused' 'exploration' {$args[0].artifact.observations.inheritedCleanup.currentCharacterIds=@($args[0].request.ownedPairs.riderAId)} 'field/remote membership'
Mutate 'registry zero refused' 'exploration' {$args[0].artifact.observations.inheritedFixture.identities.ids[0]=0}
Mutate 'registry overflow refused' 'exploration' {$args[0].artifact.observations.inheritedFixture.identities.ids[0]=2147483648}
Mutate 'registry string refused' 'exploration' {$args[0].artifact.observations.inheritedFixture.identities.ids[0]='1001'}
Mutate 'registry collision refused' 'exploration' {$args[0].artifact.observations.inheritedFixture.identities.ids[0]=1002}
Mutate 'fact aliases actor refused' 'exploration' {$args[0].artifact.observations.inheritedFixture.pairs[0].feature=1001} 'alias or are unretained'
Mutate 'rank array refused' 'exploration' {$args[0].artifact.observations.inheritedFixture.pairs[0].rank=@(1)}
Mutate 'profile array refused' 'exploration' {$args[0].artifact.observations.inheritedFixture.pairs[0].profileId=@('medium-humanoid-horse-v1')}
Mutate 'role token array refused' 'exploration' {$args[0].request.ownedPairs.riderAId=@($args[0].request.ownedPairs.riderAId)}
Mutate 'door missing remains a row blocker' 'exploration' {$args[0].artifact.observations.door=$null}
Mutate 'door cannot use historical rider role' 'exploration' {$args[0].artifact.observations.door.interactions[0].riderId='b6628a77-4962-47a4-a17c-88d9836fc9d5'}
Mutate 'target cannot alias inherited actor' 'combat-rider-first' {$args[0].artifact.observations.targetId=$args[0].request.ownedPairs.riderAId} 'aliases an inherited'
Mutate 'historical fixture claim refused' 'realtime-core' {$args[0].artifact.observations.workingSha256=$args[0].request.fixture.working.sha256} 'Working fixture identity'
Mutate 'historical role order refused' 'combat-mount-first' {$args[0].artifact.observations.actorIds[0]='b6628a77-4962-47a4-a17c-88d9836fc9d5'} 'role'
Mutate 'early output counter refused' 'persistence-save-multi-exploration-manual' {$args[0].artifact.observations.ownedOutputAdmission.authority.nativeWrites=1}
Mutate 'unconsumed sole load refused' 'persistence-save-multi-exploration-manual' {$args[0].artifact.observations.ownedOutputAdmission.authority.authorizedLoads=0}
Mutate 'invented next number refused' 'persistence-save-multi-exploration-manual' {$args[0].artifact.observations.ownedOutputAdmission.number=99} 'native next Manual number'
Mutate 'duplicate reservation refused' 'persistence-save-multi-exploration-manual' {$args[0].artifact.observations.ownedOutputAdmission.authority.reservationCount=2}
Mutate 'changed input inventory refused' 'persistence-save-multi-exploration-manual' {$args[0].artifact.observations.ownedOutputCompletion.nativeInventory[0].gameId='aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa'}
Mutate 'missing input descriptor refused' 'persistence-save-multi-exploration-manual' {$args[0].artifact.observations.ownedOutputCompletion.nativeInventory=@($args[0].artifact.observations.ownedOutputCompletion.nativeInventory[1])} 'one immutable input'
Mutate 'missing callback refused' 'persistence-save-multi-exploration-manual' {$args[0].artifact.observations.ownedOutputCompletion.callbackCount=0}
Mutate 'worker unsettled refused' 'persistence-save-multi-exploration-manual' {$args[0].artifact.observations.ownedOutputCompletion.nativeWorkerSettled=$false}
Mutate 'output redirected refused' 'persistence-save-multi-exploration-manual' {$args[0].artifact.observations.nativeWrite.path='C:\foreign\Manual_43_KMC_C7_WORLD.zks'}
Mutate 'cold parent missing refused' 'persistence-load-multi-exploration-manual' {$args[0].request.ownedPairs.checkpointSourceRunId=$null}
Mutate 'core cold write cannot widen' 'persistence-load-multi-tb-debt-manual' {$args[0].request.ownedPairs.expectedNativeWriteCount=1}
Mutate 'schema2 cold must reserve its resave' 'persistence-load-legacy-mounted-pending-tb-manual' {$args[0].request.ownedPairs.expectedNativeWriteCount=0}
Mutate 'schema2 output cannot reuse input' 'persistence-load-legacy-mounted-pending-tb-manual' {$args[0].artifact.observations.ownedOutputAdmission.leaf=$args[0].request.persistenceLoad.fileName}
foreach($case in @('exploration','persistence-load-zero-mounted-tb-retained-manual')){$v=Copy-Value @($fixtures|Where-Object name -CEQ $case)[0];foreach($s in @($v.artifact.observations.inheritedFixture,$v.artifact.observations.inheritedCleanup)){$s.identities.ids=@($s.identities.ids|ForEach-Object{-[int]$_});foreach($p in $s.pairs){foreach($key in @('rider','mount','feature','rankFact')){$p.$key=-[int]$p.$key}}};Check ('signed object labels '+$case) {Invoke-OwnedReader $v}}
foreach($case in @('multi-unknown-checkpoint','multi-exploration-three-loads-manual','owned-schema1-resave-manual','legacy-schema1-native-resave')){Mutate ('unselected checkpoint '+$case) 'persistence-save-multi-exploration-manual' {$args[0].request.persistenceCase=$case} 'closed checkpoints'}
foreach($party in @('partyBefore','partyWithHorse')){foreach($key in @('petId','masterId','blueprint')){Mutate ('owned party scalar '+$party+'/'+$key) 'exploration' {$row=if($key-ceq'petId'){$args[0].artifact.observations.$party[0]}else{$args[0].artifact.observations.$party[1]};$row.$key=@($row.$key)}}}
foreach($case in @('persistence-save-multi-exploration-manual','persistence-load-multi-tb-debt-manual')){Mutate ('owned checkpoint scalar '+$case) $case {$args[0].artifact.observations.checkpoint=@($args[0].request.persistenceCase)}}
foreach($boundary in @('multi-nonselected-mount-unsettled-manual','multi-owned-worker-cancel-manual')){$v=Copy-Value @($fixtures|Where-Object name -CEQ 'persistence-save-multi-exploration-manual')[0];$v.request.persistenceCase=$boundary;$v.request.ownedPairs.expectedNativeWriteCount=if($boundary-ceq'multi-owned-worker-cancel-manual'){2}else{1};Check ('source-only boundary selection '+$boundary){Assert-KmcChunk7OwnedPairsSelection $v.request};$v.request.ownedPairs.case='persistence-load';$v.request.ownedPairs.expectedNativeWriteCount=0;$v.request.ownedPairs.checkpointSourceRunId='boundary-parent';$v.request.ownedPairs.checkpointSourceArtifactSha256='9'*64;Check ('cold boundary remains unselected '+$boundary){Assert-KmcChunk7OwnedPairsSelection $v.request} $true 'source-only'}
function Prog($cl,$xp,$rl){[pscustomobject][ordered]@{characterLevel=$cl;experience=$xp;rangerLevel=$rl;classes=@()}}
# Owner-seed two-pair exploration: the same measured world shape with the disclosed
# native fixture setup in place of inherited custody; door may be not-applicable only here.
# Setup level-up Autos (assisted precedent): autosave on -> one completed run-owned Auto per
# rider build before SETUP-COMPLETE; autosave off -> each request natively writes nothing.
function Set-KmcOwnerSeedAutosaves($v,[bool]$On){
 $r=$v.request;$o=$v.artifact.observations;$s=$r.ownedPairs;$u=$o.ownerSeedSetup
 $r.evidenceRoot=Join-Path (Join-Path (Get-KmcLabRoot) 'runtime-evidence') $r.runId
 $path=Join-Path (Get-KmcAssistedSaveRoot $r) 'Auto_1.zks';$writes=if($On){2}else{0};$s.expectedNativeWriteCount=$writes
 $u.nativeLevelUpAutosave=[pscustomobject][ordered]@{autosaveEnabled=$On;onlyOneSave=$false;autosaveSlots=3.0;settingsWritten=$false;case=$(if($On){'autosave-on'}else{'autosave-off'});riderBuilds=2;expectedWrites=$writes;selectedWrites=$writes;slotProjectionDuringBuild=($writes-gt0);mountedPersistenceProof=$false}
 $riders=@($s.riderAId,$s.riderBId)
 $autos=@(for($i=0;$i-lt2;$i++){if($On){[pscustomobject][ordered]@{sequence=($i+1);riderId=$riders[$i];case='autosave-on';phase='setup-levelup-auto';type='Auto';autoLevelup=$true;internalName='Autosave 1';fileName='Auto_1.zks';preparedLeaf=@('Auto_1.zks','Auto_2.zks')[$i];path=$path;insideRunRoot=$true;length=1200000;sha256=([string]@('a','b')[$i]*64);lastWriteTimeUtcTicks=638960000000000000;completionSequence=($i+1);requestedObject=(500+$i);writtenObject=(600+$i);admittedWrites=($i+1);authorizedWrites=($i+1);snapshots=($i+1);completedSaves=($i+1);frame=$i;completedBeforeBoundary=$true;mountedPersistenceProof=$false;probe=[pscustomobject]@{complete=$true}}}else{[pscustomobject][ordered]@{sequence=($i+1);riderId=$riders[$i];case='autosave-off';requested=$true;written=$false;probe=[pscustomobject]@{complete=$false}}}})
 $u|Add-Member setupAutosaves $autos -Force
 $build=@($u.assistance|Where-Object kind -CEQ 'native-ordinary-ranger-levels');$xp=@($u.assistance|Where-Object kind -CEQ 'native-xp')
 $api='Kingmaker.Controllers.LevelUpAutoSaveController.HandleLevelUpComplete -> Game.SaveGame(Auto) into the run-owned isolated save root'
 function AutoItem($unit,$n){[pscustomobject][ordered]@{unitId=$unit;kind='native-levelup-autosave';nativeApi=$api;before=($n-1);after=$n;amount=1;detail=$path}}
 $items=@($build[0]);if($On){$items+=AutoItem $riders[0] 1};$items+=$xp;$items+=$build[1];if($On){$items+=AutoItem $riders[1] 2}
 $u.assistance=$items
 $o.setupComplete.assistanceCount=$items.Count;$o.setupComplete|Add-Member writesAtBoundary $writes -Force;$o.setupComplete|Add-Member writeScopeClosed $true -Force
 # Stock XP table, money/item economy and a boundary no later than the first measured observation.
 $u|Add-Member nativeXpTable @(2000,5000,9000,15000,23000,35000,51000,75000,105000,155000,220000,315000,445000,635000,890000,1300000,1800000,2550000,3600000) -Force
 $u.nativeXpTable=@(0)+@($u.nativeXpTable)
 $economy=[pscustomobject][ordered]@{money=30000;itemRows=42;inventorySha256=('c'*64);characterIds=@($s.partyIds|Sort-Object -CaseSensitive)}
 $u|Add-Member economyBefore $economy -Force
 # Each setup Horse's native bite/hoof/hoof limb items, held by that Horse (disclosed, not digested).
 $limbs={param($h)@(@('a1b2c3d4e5f60718293a4b5c6d7e8f90','WeaponSlot'),@('b1b2c3d4e5f60718293a4b5c6d7e8f90','WeaponSlot'),@('b1b2c3d4e5f60718293a4b5c6d7e8f90','WeaponSlot'))|ForEach-Object{[pscustomobject][ordered]@{blueprint=$_[0];slot=$_[1];count=1;heldBy=$h}}}
 $o.setupComplete|Add-Member horseItems ([pscustomobject][ordered]@{horses=@([pscustomobject][ordered]@{horseId=$u.riders[0].horseId;items=@(&$limbs $u.riders[0].horseId)},[pscustomobject][ordered]@{horseId=$u.riders[1].horseId;items=@(&$limbs $u.riders[1].horseId)});foreignHeldRows=0}) -Force
$o.setupComplete|Add-Member economy ($economy|ConvertTo-Json -Depth 5|ConvertFrom-Json) -Force
 $o.setupComplete.frame=[int]@($o.measurements)[0].startFrame;$o.setupComplete.gameTicks=[long]@($o.inputs)[0].resourcesBefore.gameTicks
 foreach($pair in @(@('writesAfterBoundary',0),@('unauthorizedWrites',0),@('slotProjectionReleased',$true))){$o.cleanup|Add-Member $pair[0] $pair[1] -Force}
 $v
}
function New-KmcOwnerSeedWorldFixture([bool]$DoorApplicable=$true,[bool]$Autosave=$true){
 $v=Copy-Value @($fixtures|Where-Object name -CEQ 'exploration')[0];$r=$v.request;$o=$v.artifact.observations;$p=$r.ownedPairs
 $a=$p.riderAId;$ma=$p.mountAId;$b=$p.riderBId;$mb=$p.mountBId;$party=@($a,$b,'50505050-5050-4050-8050-505050505050','60606060-6060-4060-8060-606060606060')
 $r.ownedPairs=[pscustomobject][ordered]@{case='owner-seed-exploration';seedId='KMC_C7_OWNER_SEED';seedSha256=('9'*64);riderAId=$a;riderBId=$b;partyIds=$party;xpAssistance=[pscustomobject]@{unitId=$b;targetCharacterLevel=8};baseGameOnly=$true;expectedNativeLoadCount=1;expectedNativeWriteCount=0}
 $r.persistenceLoad.internalName='KMC_C7_OWNER_SEED';$r.persistenceLoad.fileName='Manual_302_KMC_C7_OWNER_SEED.zks';$r.persistenceLoad.sha256='9'*64
 foreach($key in @('acquisition','inheritedFixture','inheritedCleanup')){if($o.PSObject.Properties[$key]){$o.PSObject.Properties.Remove($key)}}
 $o.capability=[pscustomobject][ordered]@{archiveNativeEnabled=$true;nativeWriterSchema=3;nativePairCapacity=2;workingSha256=$null;inheritedManualSha256=$null;ownerSeedSha256=('9'*64)}
 function Row($id,$bp,$pet,$master){[pscustomobject][ordered]@{id=$id;blueprint=$bp;petId=$pet;masterId=$master}}
 $o.partyBefore=@($party|ForEach-Object{Row $_ 'rider' $null $null})
 $with=@((Row $a 'rider' $ma $null),(Row $b 'rider' $mb $null),(Row $party[2] 'rider' $null $null),(Row $party[3] 'rider' $null $null),(Row $ma '4016c7db400ab721ff125aef9e65e202' $null $a),(Row $mb '4016c7db400ab721ff125aef9e65e202' $null $b))
 $o.partyWithHorse=$with;$o.cleanup.partyAfter=Copy-Value $with
 $o|Add-Member ownerSeed ([pscustomobject][ordered]@{contract='owner-seed-two-pair-exploration-v1';seedId='KMC_C7_OWNER_SEED';seedSha256=('9'*64);riderAId=$a;riderBId=$b;nativeLoadVerifiedByHost=$true;fixtureSetupOnly=$true;acquisitionProof=$false})
 function Facts($role,$id,$cl,$rl,$xp,$ready){[pscustomobject][ordered]@{role=$role;riderId=$id;size='Medium';originalSize='Medium';polymorphed=$false;controllable=$true;petless=$true;characterLevel=$cl;rangerLevel=$rl;experience=$xp;readyLevel=$ready;eligibleWithoutAssistance=(($rl+$ready-$cl)-ge4);minimalTargetLevel=($cl+4-$rl)}}
 function Rider($role,$id,$horse,$before,$after,$commits){[pscustomobject][ordered]@{role=$role;riderId=$id;before=$before;after=$after;nativeCommits=$commits;nativeChoices=(12*$commits);nativeAutoLevelupOverrides=$(if($role-ceq'B'){1}else{0});horseId=$horse;horseBlueprint='4016c7db400ab721ff125aef9e65e202';horseMasterId=$id;horseFeatureSourceGuid=('b705c5184a96a84428eeb35ae2517a14');presentation='fixture-setup';acquisitionProof=$false;profileId='medium-humanoid-horse-v1';rigValidated=$true}}
 function Item($unit,$kind,$api,$before,$after,$amount){[pscustomobject][ordered]@{unitId=$unit;kind=$kind;nativeApi=$api;before=$before;after=$after;amount=$amount;detail='synthetic'}}
 $build='Kingmaker.UI.LevelUp.CharacterBuildController (Chunk7OrdinaryBuildDriver ExistingRangerLevels)';$gain='Kingmaker.UnitLogic.UnitProgressionData.GainExperience(int, log: false)'
 $o|Add-Member ownerSeedSetup ([pscustomobject][ordered]@{contract='owner-seed-fixture-setup-v1';disclosure=(Get-KmcChunk7OwnerSeedDisclosure);seedId='KMC_C7_OWNER_SEED';seedSha256=('9'*64);fixtureSetupOnly=$true;acquisitionProof=$false;producerAncestry=$false;loadedArchiveKind='Missing';loadedArchiveCanRestore=$false
  riderFacts=@((Facts 'A' $a 3 3 9000 4),(Facts 'B' $b 4 0 9000 4));nativeLevelUpAutosave=[pscustomobject]@{autosaveEnabled=$false;onlyOneSave=$false;settingsWritten=$false}
  riders=@((Rider 'A' $a $ma (Prog 3 9000 3) (Prog 4 9000 4) 1),(Rider 'B' $b $mb (Prog 4 51000 0) (Prog 8 51000 4) 4));events=@([pscustomobject]@{event='native-level-open'},[pscustomobject]@{event='native-builder-complete'});admissionRefusals=@();eventCount=2;eventsTruncated=$false
  riderBExperience=[pscustomobject][ordered]@{assisted=$true;targetCharacterLevel=8;before=9000;after=51000;delta=42000;threshold=51000;characterLevelBefore=4;characterLevelAfter=4;levelReadyAfter=$true;readyLevelAfter=8}
  assistance=@((Item $a 'native-ordinary-ranger-levels' $build 3 4 1),(Item $b 'native-xp' $gain 9000 51000 42000),(Item $b 'native-ordinary-ranger-levels' $build 4 8 4))})
 $o|Add-Member setupComplete ([pscustomobject][ordered]@{boundary='SETUP-COMPLETE';frame=900;gameTicks=10000000;assistanceCount=3;setupSeconds=42.5;measuredClockRestarted=$true;state=[pscustomobject]@{money=30000;pairs=@([pscustomobject]@{riderId=$a;petId=$ma;petMasterId=$a},[pscustomobject]@{riderId=$b;petId=$mb;petMasterId=$b})}})
 foreach($key in @('setupCompleted','ownerSeedSetupPetsPreserved','setupStateUnchangedAfterBoundary','setupRetainedInMemoryOnly')){$o.cleanup|Add-Member $key $true -Force}
 $o.cleanup|Add-Member setupMutationsAfterBoundary 0 -Force;$o.cleanup|Add-Member inheritedPetsPreserved $false -Force;$o.cleanup.horseRemoved=$false;$o.cleanup.nativeProgressionRequiresExternalWorkingRestoration=$false
 if(-not$DoorApplicable){
  $doorIds=@(@($o.door.interactions|ForEach-Object{[long]$_.commandObject})+@([long]$o.door.firstPairReturn.command))
  $o.factRemovalAdmission.commands=@($o.factRemovalAdmission.commands|Where-Object{[long]$_.commandObject-notin$doorIds})
  $o.snapshots=@($o.snapshots|Where-Object label -CNE 'door-complete')
  $o.door=[pscustomobject][ordered]@{applicable=$false;reason='No open StandardDoor with an active-when-closed navmesh cut lies outside both pair interaction radii within the bounded scan radius.';candidatesScanned=3;radius=28.0}
 }
 $v.name='exploration';Set-KmcOwnerSeedAutosaves $v $Autosave
}
function Check-OwnerSeed([string]$Name,[scriptblock]$Mutation,[bool]$DoorApplicable=$true,[string]$Pattern=''){$v=New-KmcOwnerSeedWorldFixture $DoorApplicable;&$Mutation $v;Check $Name {Assert-KmcChunk7WorldEvidence $v.request $v.artifact 'PASS'} $true $Pattern}
Check 'owner-seed exploration with a tested door' {$v=New-KmcOwnerSeedWorldFixture $true;Assert-KmcChunk7WorldEvidence $v.request $v.artifact 'PASS'}
Check 'owner-seed exploration with a not-applicable door' {$v=New-KmcOwnerSeedWorldFixture $false;Assert-KmcChunk7WorldEvidence $v.request $v.artifact 'PASS'}
Check 'owner-seed exploration without XP assistance' {$v=New-KmcOwnerSeedWorldFixture $true;$v.request.ownedPairs.xpAssistance=$null;$s=$v.artifact.observations.ownerSeedSetup;$s.riderFacts[1].characterLevel=3;$s.riderFacts[1].rangerLevel=3;$s.riderFacts[1].eligibleWithoutAssistance=$true;$s.riderFacts[1].minimalTargetLevel=4;$s.riderBExperience=[pscustomobject]@{assisted=$false;experience=9000};$s.riders[1].before=Prog 3 9000 3;$s.riders[1].after=Prog 4 9000 4;$s.riders[1].nativeCommits=1;$b=@($s.assistance|Where-Object kind -CEQ 'native-ordinary-ranger-levels');$b[1].before=3;$b[1].after=4;$b[1].amount=1;$s.assistance=@($b);[void](Set-KmcOwnerSeedAutosaves $v $true);Assert-KmcChunk7WorldEvidence $v.request $v.artifact 'PASS'}
Check-OwnerSeed 'owner-seed evidence without its assistance disclosure refused' {$args[0].artifact.observations.PSObject.Properties.Remove('ownerSeedSetup')}
Check-OwnerSeed 'owner-seed evidence missing one assistance item refused' {$s=$args[0].artifact.observations.ownerSeedSetup;$s.assistance=@($s.assistance[0],$s.assistance[2])} $true 'assistance disclosure'
Check-OwnerSeed 'owner-seed evidence with an undeclared money assistance refused' {$s=$args[0].artifact.observations.ownerSeedSetup;$s.assistance=@($s.assistance)+@([pscustomobject]@{unitId=$args[0].request.ownedPairs.riderAId;kind='native-money';nativeApi='Player.GainMoney';before=0;after=1;amount=1;detail='x'})} $true 'assistance disclosure'
Check-OwnerSeed 'owner-seed disclosure text cannot be softened' {$args[0].artifact.observations.ownerSeedSetup.disclosure='Horse acquired natively.'}
Check-OwnerSeed 'owner-seed rider outcome cannot claim acquisition proof' {$args[0].artifact.observations.ownerSeedSetup.riders[1].acquisitionProof=$true}
Check-OwnerSeed 'owner-seed setup cannot claim acquisition proof' {$args[0].artifact.observations.ownerSeed.acquisitionProof=$true}
Check-OwnerSeed 'owner-seed XP beyond the exact minimal eligibility refused' {$x=$args[0].artifact.observations.ownerSeedSetup.riderBExperience;$x.delta=50000;$x.after=59000;$x.threshold=59000} $true 'exceeds or differs'
Check-OwnerSeed 'owner-seed XP for an eligible rider B refused' {$f=$args[0].artifact.observations.ownerSeedSetup.riderFacts[1];$f.rangerLevel=4;$f.eligibleWithoutAssistance=$true;$f.minimalTargetLevel=4}
Check-OwnerSeed 'owner-seed missing SETUP-COMPLETE boundary refused' {$args[0].artifact.observations.PSObject.Properties.Remove('setupComplete')}
Check-OwnerSeed 'owner-seed measured-phase setup mutation refused' {$args[0].artifact.observations.cleanup.setupMutationsAfterBoundary=1}
Check-OwnerSeed 'owner-seed setup state drift after boundary refused' {$args[0].artifact.observations.cleanup.setupStateUnchangedAfterBoundary=$false}
Check-OwnerSeed 'owner-seed setup failure cannot be a PASS' {$args[0].artifact.observations|Add-Member failurePhase 'SETUP'}
Check-OwnerSeed 'owner-seed OnlyOneSave/Ironman setup refused' {$args[0].artifact.observations.ownerSeedSetup.nativeLevelUpAutosave.onlyOneSave=$true}
Check 'owner-seed exploration with native autosave off and zero setup writes' {$v=New-KmcOwnerSeedWorldFixture $true $false;Assert-KmcChunk7WorldEvidence $v.request $v.artifact 'PASS'}
Check 'owner-seed autosave off cannot declare setup writes' {$v=New-KmcOwnerSeedWorldFixture $true $false;$v.request.ownedPairs.expectedNativeWriteCount=2;Assert-KmcChunk7WorldEvidence $v.request $v.artifact 'PASS'} $true
Check 'owner-seed autosave off cannot carry a written Auto' {$v=New-KmcOwnerSeedWorldFixture $true $false;$v.artifact.observations.ownerSeedSetup.setupAutosaves[0].written=$true;Assert-KmcChunk7WorldEvidence $v.request $v.artifact 'PASS'} $true
Check-OwnerSeed 'owner-seed selected write count must equal the native observation' {$args[0].request.ownedPairs.expectedNativeWriteCount=1} $true 'differs from the observed'
Check-OwnerSeed 'owner-seed observed write count must equal its native derivation' {$a=$args[0].artifact.observations.ownerSeedSetup.nativeLevelUpAutosave;$a.expectedWrites=1;$a.selectedWrites=1;$args[0].request.ownedPairs.expectedNativeWriteCount=1}
Check-OwnerSeed 'owner-seed missing setup Auto observation refused' {$u=$args[0].artifact.observations.ownerSeedSetup;$u.setupAutosaves=@($u.setupAutosaves[0])} $true 'one native level-up autosave'
Check-OwnerSeed 'owner-seed setup Auto after SETUP-COMPLETE refused' {$args[0].artifact.observations.ownerSeedSetup.setupAutosaves[1].frame=901} $true 'after SETUP-COMPLETE'
Check-OwnerSeed 'owner-seed native write after SETUP-COMPLETE refused' {$args[0].artifact.observations.cleanup.writesAfterBoundary=1}
Check-OwnerSeed 'owner-seed unauthorized write refused' {$args[0].artifact.observations.cleanup.unauthorizedWrites=1}
Check-OwnerSeed 'owner-seed setup Auto outside the run root refused' {$w=$args[0].artifact.observations.ownerSeedSetup.setupAutosaves[0];$w.path='C:\Users\Howie\AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\Saved Games\Auto_1.zks'}
Check-OwnerSeed 'owner-seed Manual setup write refused' {$w=$args[0].artifact.observations.ownerSeedSetup.setupAutosaves[1];$w.type='Manual'}
Check-OwnerSeed 'owner-seed Quick setup leaf refused' {$w=$args[0].artifact.observations.ownerSeedSetup.setupAutosaves[0];$w.fileName='Quick_1.zks'}
Check-OwnerSeed 'owner-seed setup Auto cannot claim mounted persistence' {$args[0].artifact.observations.ownerSeedSetup.setupAutosaves[0].mountedPersistenceProof=$true}
Check-OwnerSeed 'owner-seed setup Auto without its assistance item refused' {$u=$args[0].artifact.observations.ownerSeedSetup;$u.assistance=@($u.assistance|Where-Object{$_.kind-cne'native-levelup-autosave'-or$_.after-ne2});$args[0].artifact.observations.setupComplete.assistanceCount=@($u.assistance).Count} $true 'assistance disclosure'
Check-OwnerSeed 'owner-seed boundary must close the write scope' {$args[0].artifact.observations.setupComplete.writeScopeClosed=$false}
Check-OwnerSeed 'owner-seed boundary write count must equal the selection' {$args[0].artifact.observations.setupComplete.writesAtBoundary=1}
Check-OwnerSeed 'owner-seed slot projection must be released' {$args[0].artifact.observations.cleanup.slotProjectionReleased=$false}
# Reviewer counterexamples: self-consistent XP beyond the stock-table minimum is refused.
foreach($cx in @(@(59000,50000),@(75000,66000))){Check-OwnerSeed ('owner-seed self-consistent XP '+$cx[0]+'/'+$cx[1]+' beyond the stock minimum refused') {param($v)$t=$cx[0];$d=$cx[1];$x=$v.artifact.observations.ownerSeedSetup.riderBExperience;$x.after=$t;$x.threshold=$t;$x.delta=$d;$r=$v.artifact.observations.ownerSeedSetup.riders[1];$r.before.experience=$t;$r.after.experience=$t;$i=@($v.artifact.observations.ownerSeedSetup.assistance|Where-Object kind -CEQ 'native-xp')[0];$i.after=$t;$i.amount=$d}.GetNewClosure() $true 'stock XP table'}
Check-OwnerSeed 'owner-seed rider B ending beyond Ranger 4 refused' {$args[0].artifact.observations.ownerSeedSetup.riders[1].after.rangerLevel=5} $true 'exactly Ranger 4'
Check-OwnerSeed 'owner-seed rider A ending beyond Ranger 4 refused' {$args[0].artifact.observations.ownerSeedSetup.riders[0].after.rangerLevel=5} $true 'exactly Ranger 4'
Check-OwnerSeed 'owner-seed rider A level beyond its XP-ready level refused' {$r=$args[0].artifact.observations.ownerSeedSetup.riders[0];$r.after.characterLevel=5;$r.nativeCommits=2;$i=@($args[0].artifact.observations.ownerSeedSetup.assistance)[0];$i.after=5;$i.amount=2} $true 'stock XP table'
Check-OwnerSeed 'owner-seed rider B before outside its current level band refused' {$v=$args[0];$f=$v.artifact.observations.ownerSeedSetup.riderFacts[1];$f.experience=15500;$f.readyLevel=5;$f.minimalTargetLevel=8;$x=$v.artifact.observations.ownerSeedSetup.riderBExperience;$x.before=15500;$x.delta=35500;$i=@($v.artifact.observations.ownerSeedSetup.assistance|Where-Object kind -CEQ 'native-xp')[0];$i.before=15500;$i.amount=35500} $true 'stock XP table'
Check-OwnerSeed 'owner-seed native XP table must equal the stock table' {$args[0].artifact.observations.ownerSeedSetup.nativeXpTable[7]=50000}
Check-OwnerSeed 'owner-seed money change across setup refused' {$args[0].artifact.observations.setupComplete.economy.money=29999;$args[0].artifact.observations.setupComplete.state.money=29999} $true 'money or the party item'
Check-OwnerSeed 'owner-seed item change across setup refused' {$args[0].artifact.observations.setupComplete.economy.inventorySha256='d'*64} $true 'money or the party item'
Check-OwnerSeed 'owner-seed missing economy observation refused' {$args[0].artifact.observations.ownerSeedSetup.PSObject.Properties.Remove('economyBefore')}
Check-OwnerSeed 'owner-seed SETUP-COMPLETE after the first measured frame refused' {$args[0].artifact.observations.setupComplete.frame=2000000000} $true 'precede the measured'
Check-OwnerSeed 'owner-seed SETUP-COMPLETE after the first measured tick refused' {$args[0].artifact.observations.setupComplete.gameTicks=9000000000000000000} $true 'precede the measured'
Check-OwnerSeed 'owner-seed Horse limb row moved to a party character refused' {$args[0].artifact.observations.setupComplete.horseItems.horses[0].items[1].heldBy=$args[0].request.ownedPairs.riderAId}
Check-OwnerSeed 'owner-seed shared item held by another unit refused' {$args[0].artifact.observations.setupComplete.horseItems.foreignHeldRows=1}
Check-OwnerSeed 'owner-seed new unheld shared item refused' {$e=$args[0].artifact.observations.setupComplete.economy;$e.itemRows=43;$e.inventorySha256='e'*64} $true 'money or the party item'
Check-OwnerSeed 'owner-seed missing Horse item disclosure refused' {$args[0].artifact.observations.setupComplete.PSObject.Properties.Remove('horseItems')}
Check-OwnerSeed 'owner-seed Horse without disclosed limb items refused' {$args[0].artifact.observations.setupComplete.horseItems.horses[1].items=@()}
Check-OwnerSeed 'owner-seed economy characters outside the party refused' {foreach($e in @($args[0].artifact.observations.ownerSeedSetup.economyBefore,$args[0].artifact.observations.setupComplete.economy)){$e.characterIds=@('nobody')}} $true 'outside the selected party'
Check-OwnerSeed 'owner-seed economy without both riders refused' {foreach($e in @($args[0].artifact.observations.ownerSeedSetup.economyBefore,$args[0].artifact.observations.setupComplete.economy)){$e.characterIds=@($args[0].request.ownedPairs.riderAId)}} $true 'both riders'
Check-OwnerSeed 'owner-seed boundary economy money must equal the boundary state money' {$args[0].artifact.observations.setupComplete.state.money=1} $true 'boundary state'
Check 'owner-seed registry seed with a spaced internal name accepted' {$v=New-KmcOwnerSeedWorldFixture;$v.request.ownedPairs.seedId='KMC_C7_NO_MODS_OWNER_SEED';$v.request.persistenceLoad.internalName='KMC C7 NO MODS OWNER SEED';$v.request.persistenceLoad.fileName='Manual_303_KMC_C7_NO_MODS_OWNER_SEED.zks';foreach($n in @($v.artifact.observations.ownerSeed,$v.artifact.observations.ownerSeedSetup)){$n.seedId='KMC_C7_NO_MODS_OWNER_SEED'};Assert-KmcChunk7WorldEvidence $v.request $v.artifact 'PASS'}
Check 'owner-seed spaced name still refuses the producer fixture name' {$v=New-KmcOwnerSeedWorldFixture;$v.request.persistenceLoad.internalName='KMC_C7_ASSISTED_FIXTURE';Assert-KmcChunk7WorldEvidence $v.request $v.artifact 'PASS'} $true 'impersonates'
Check-OwnerSeed 'owner-seed setup cannot remove its Horse' {$args[0].artifact.observations.cleanup.horseRemoved=$true}
Check-OwnerSeed 'owner-seed cannot claim inherited provenance' {$args[0].artifact.observations.capability.inheritedManualSha256='7'*64}
Check-OwnerSeed 'owner-seed rider not petless before setup refused' {$args[0].artifact.observations.partyBefore[0].petId=$args[0].request.ownedPairs.riderBId}
Check-OwnerSeed 'owner-seed Small rider fact refused' {$args[0].artifact.observations.ownerSeedSetup.riderFacts[1].size='Small'}
Check-OwnerSeed 'owner-seed not-applicable door with a completion window refused' {$args[0].artifact.observations.snapshots=@($args[0].artifact.observations.snapshots)+@([pscustomobject]@{label='door-complete';owners=@()})} $false
Check-OwnerSeed 'owner-seed not-applicable door radius must be exact' {$args[0].artifact.observations.door.radius=30.0} $false
Check-OwnerSeed 'owner-seed not-applicable door cannot claim it was tested' {$args[0].artifact.observations.door.applicable=$true} $false
Check-OwnerSeed 'owner-seed door with neither proof nor scan refused' {$args[0].artifact.observations.door=$null}
Check 'not-applicable door refused for the producer exploration kind' {$v=Copy-Value @($fixtures|Where-Object name -CEQ 'exploration')[0];$v.artifact.observations.door=[pscustomobject][ordered]@{applicable=$false;reason='none';candidatesScanned=0;radius=28.0};Invoke-OwnedReader $v} $true 'only for owner-seed'
$clock.Stop();[void][IO.Directory]::CreateDirectory($OutputDirectory);$passed=@($results|Where-Object pass).Count;$failed=$results.Count-$passed
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'receipt.json'),([pscustomobject]@{passed=$passed;failed=$failed;elapsedSeconds=$clock.Elapsed.TotalSeconds;nativeInvoked=$false;archiveRead=$false;results=@($results)}|ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false))
Write-Output ('Owned two-pair reader: '+$passed+' PASS / '+$failed+' FAIL');if($failed){exit 1}
