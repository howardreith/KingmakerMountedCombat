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
. (Join-Path $repo 'tests/Chunk7Diagnostics/OwnerSeedCheckpointPreparationFixtures.ps1')
. (Join-Path $repo 'tests/Chunk7Diagnostics/OwnerSeedMigrationFixtures.ps1')
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
# The installed game has no level-up autosave (LevelUpAutoSaveController is never registered):
# whatever the observed autosave setting, each rider build shows an installed probe with no
# completion entry, save request or prepared write, and setup makes zero writes.
function Set-KmcOwnerSeedAutosaves($v,[bool]$On){
 $r=$v.request;$o=$v.artifact.observations;$s=$r.ownedPairs;$u=$o.ownerSeedSetup
 $r.evidenceRoot=Join-Path (Join-Path (Get-KmcLabRoot) 'runtime-evidence') $r.runId
 $writes=0;$s.expectedNativeWriteCount=$writes
 $u.nativeLevelUpAutosave=[pscustomobject][ordered]@{autosaveEnabled=$On;onlyOneSave=$false;autosaveSlots=3.0;settingsWritten=$false;case='no-native-levelup-autosave';riderBuilds=2;expectedWrites=$writes;selectedWrites=$writes;slotProjectionDuringBuild=$false;mountedPersistenceProof=$false}
 $riders=@($s.riderAId,$s.riderBId)
 $probe={[pscustomobject][ordered]@{hooks=@([pscustomobject]@{type='Kingmaker.Controllers.LevelUpAutoSaveController';method='HandleLevelUpComplete'});events=@();errors=@();completionEntries=0;completionExits=0;saveRequests=0;preparedWrites=0;installationAttempted=$true;installed=$true;complete=$false}}
 $autos=@(for($i=0;$i-lt2;$i++){[pscustomobject][ordered]@{sequence=($i+1);riderId=$riders[$i];case='no-native-levelup-autosave';requested=$false;written=$false;probe=(&$probe)}})
 $u|Add-Member setupAutosaves $autos -Force
 $build=@($u.assistance|Where-Object kind -CEQ 'native-ordinary-ranger-levels');$xp=@($u.assistance|Where-Object kind -CEQ 'native-xp')
 $items=@($build[0]);$items+=$xp;$items+=$build[1]
 $u.assistance=$items
 $o.setupComplete.assistanceCount=$items.Count;$o.setupComplete|Add-Member writesAtBoundary $writes -Force;$o.setupComplete|Add-Member writeScopeClosed $true -Force
 # Stock XP table, money/item economy and a boundary no later than the first measured observation.
 $u|Add-Member nativeXpTable @(2000,5000,9000,15000,23000,35000,51000,75000,105000,155000,220000,315000,445000,635000,890000,1300000,1800000,2550000,3600000) -Force
 $u.nativeXpTable=@(0)+@($u.nativeXpTable)
 # Honest native shape: the shared stash keeps its 4 non-party player-faction holders (rows
 # included in the digest, unchanged by setup); itemRows counts every shared row + the slots.
 $economy=[pscustomobject][ordered]@{money=30000;itemRows=46;inventorySha256=('c'*64);characterIds=@($s.partyIds|Sort-Object -CaseSensitive)}
 $u|Add-Member economyBefore $economy -Force
 # Honest native shape: a companion pet keeps its own inventory collection, so each Horse's
 # body/limb items are read from the Horse itself and never appear in the shared stash.
 $limbs={param($h)@(@('a1b2c3d4e5f60718293a4b5c6d7e8f90','WeaponSlot'),@('b1b2c3d4e5f60718293a4b5c6d7e8f90','WeaponSlot'),@('b1b2c3d4e5f60718293a4b5c6d7e8f90','inventory'))|ForEach-Object{[pscustomobject][ordered]@{blueprint=$_[0];slot=$_[1];count=1;heldBy=$h}}}
 $o.setupComplete|Add-Member horseItems ([pscustomobject][ordered]@{horses=@([pscustomobject][ordered]@{horseId=$u.riders[0].horseId;sharedCollection=$false;items=@(&$limbs $u.riders[0].horseId)},[pscustomobject][ordered]@{horseId=$u.riders[1].horseId;sharedCollection=$false;items=@(&$limbs $u.riders[1].horseId)});note='native Horse body/inventory disclosure; not compared; layout not yet observed natively'}) -Force
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
Check 'owner-seed exploration with the autosave setting on and zero setup writes' {$v=New-KmcOwnerSeedWorldFixture $true $true;Assert-KmcChunk7WorldEvidence $v.request $v.artifact 'PASS'}
Check 'owner-seed exploration with the autosave setting off and zero setup writes' {$v=New-KmcOwnerSeedWorldFixture $true $false;Assert-KmcChunk7WorldEvidence $v.request $v.artifact 'PASS'}
foreach($on in @($true,$false)){Check ('owner-seed selection with two setup writes refused (autosave setting '+$on+')') {$v=New-KmcOwnerSeedWorldFixture $true $on;$v.request.ownedPairs.expectedNativeWriteCount=2;Assert-KmcChunk7WorldEvidence $v.request $v.artifact 'PASS'} $true 'no level-up autosave'}
Check-OwnerSeed 'owner-seed selection with one setup write refused' {$args[0].request.ownedPairs.expectedNativeWriteCount=1} $true 'no level-up autosave'
Check-OwnerSeed 'owner-seed observed write count must be zero' {$a=$args[0].artifact.observations.ownerSeedSetup.nativeLevelUpAutosave;$a.expectedWrites=2;$a.selectedWrites=2}
Check-OwnerSeed 'owner-seed old autosave-on case refused' {$args[0].artifact.observations.ownerSeedSetup.nativeLevelUpAutosave.case='autosave-on'}
Check-OwnerSeed 'owner-seed enabled slot projection refused' {$args[0].artifact.observations.ownerSeedSetup.nativeLevelUpAutosave.slotProjectionDuringBuild=$true}
Check-OwnerSeed 'owner-seed missing per-build absence observation refused' {$u=$args[0].artifact.observations.ownerSeedSetup;$u.setupAutosaves=@($u.setupAutosaves[0])} $true 'absence of a native level-up autosave'
Check-OwnerSeed 'owner-seed row claiming requested:true refused' {$args[0].artifact.observations.ownerSeedSetup.setupAutosaves[1].requested=$true}
Check-OwnerSeed 'owner-seed row claiming written:true refused' {$args[0].artifact.observations.ownerSeedSetup.setupAutosaves[0].written=$true}
foreach($key in @('completionEntries','saveRequests','preparedWrites')){Check-OwnerSeed ('owner-seed observed '+$key+' during setup refused') {param($v)$v.artifact.observations.ownerSeedSetup.setupAutosaves[1].probe.$key=1} $true 'registers no LevelUpAutoSaveController'}
Check-OwnerSeed 'owner-seed probe that was never installed refused' {$args[0].artifact.observations.ownerSeedSetup.setupAutosaves[0].probe.installed=$false}
Check-OwnerSeed 'owner-seed probe with a native event refused' {$args[0].artifact.observations.ownerSeedSetup.setupAutosaves[0].probe.events=@([pscustomobject]@{kind='native-level-autosave-request'})}
Check-OwnerSeed 'owner-seed probe without its counters refused' {$args[0].artifact.observations.ownerSeedSetup.setupAutosaves[0].probe.PSObject.Properties.Remove('saveRequests')}
Check-OwnerSeed 'owner-seed old written-Auto row shape refused' {$w=$args[0].artifact.observations.ownerSeedSetup.setupAutosaves[0];$w|Add-Member fileName 'Auto_1.zks'}
Check-OwnerSeed 'owner-seed native write after SETUP-COMPLETE refused' {$args[0].artifact.observations.cleanup.writesAfterBoundary=1}
Check-OwnerSeed 'owner-seed unauthorized write refused' {$args[0].artifact.observations.cleanup.unauthorizedWrites=1}
Check-OwnerSeed 'owner-seed level-up autosave assistance item refused' {$u=$args[0].artifact.observations.ownerSeedSetup;$u.assistance=@($u.assistance)+@([pscustomobject][ordered]@{unitId=$args[0].request.ownedPairs.riderBId;kind='native-levelup-autosave';nativeApi='Game.SaveGame';before=0;after=1;amount=1;detail='Auto_1.zks'});$args[0].artifact.observations.setupComplete.assistanceCount=@($u.assistance).Count} $true 'assistance disclosure'
# Runner final-root check: only the seed copy may remain.
Check 'owner-seed final save root with only the seed copy accepted' {$v=New-KmcOwnerSeedWorldFixture;Assert-KmcChunk7OwnerSeedFinalSaveRoot $v.request @($v.request.persistenceLoad.fileName)}
Check 'owner-seed final save root with an Auto_1 left refused' {$v=New-KmcOwnerSeedWorldFixture;Assert-KmcChunk7OwnerSeedFinalSaveRoot $v.request @($v.request.persistenceLoad.fileName,'Auto_1.zks')} $true 'no Auto_1'
Check 'owner-seed final save root with only an Auto_1 refused' {$v=New-KmcOwnerSeedWorldFixture;Assert-KmcChunk7OwnerSeedFinalSaveRoot $v.request @('Auto_1.zks')} $true 'no Auto_1'
Check 'owner-seed final save root without the seed copy refused' {$v=New-KmcOwnerSeedWorldFixture;Assert-KmcChunk7OwnerSeedFinalSaveRoot $v.request @()} $true 'no Auto_1'
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
# The digest covers every shared row with its holder: a move between holders changes only the hash.
Check-OwnerSeed 'owner-seed shared item moved from a rider to another holder refused' {$args[0].artifact.observations.setupComplete.economy.inventorySha256='e'*64} $true 'money or the party item'
Check-OwnerSeed 'owner-seed shared item moved from a non-party holder to a Horse refused' {$args[0].artifact.observations.setupComplete.economy.inventorySha256='f'*64} $true 'money or the party item'
Check-OwnerSeed 'owner-seed new unheld shared item refused' {$e=$args[0].artifact.observations.setupComplete.economy;$e.itemRows=47;$e.inventorySha256='e'*64} $true 'money or the party item'
Check-OwnerSeed 'owner-seed removed shared item refused' {$e=$args[0].artifact.observations.setupComplete.economy;$e.itemRows=45;$e.inventorySha256='e'*64} $true 'money or the party item'
Check-OwnerSeed 'owner-seed missing Horse item disclosure refused' {$args[0].artifact.observations.setupComplete.PSObject.Properties.Remove('horseItems')}
Check-OwnerSeed 'owner-seed disclosure item held by the other Horse refused' {$h=$args[0].artifact.observations.setupComplete.horseItems.horses;$h[0].items[0].heldBy=$h[1].horseId}
Check-OwnerSeed 'owner-seed missing Horse disclosure entry refused' {$x=$args[0].artifact.observations.setupComplete.horseItems;$x.horses=@($x.horses[0])} $true 'each setup Horse'
Check-OwnerSeed 'owner-seed Horse disclosure out of rider order refused' {$x=$args[0].artifact.observations.setupComplete.horseItems;$x.horses=@($x.horses[1],$x.horses[0])}
Check-OwnerSeed 'owner-seed Horse disclosure note cannot be softened' {$args[0].artifact.observations.setupComplete.horseItems.note='native Horse items compared'}
Check-OwnerSeed 'owner-seed Horse disclosure cannot reintroduce foreign held rows' {$args[0].artifact.observations.setupComplete.horseItems|Add-Member foreignHeldRows 0}
Check 'owner-seed Horse with no disclosed items accepted (no minimum)' {$v=New-KmcOwnerSeedWorldFixture $true;$v.artifact.observations.setupComplete.horseItems.horses[1].items=@();Assert-KmcChunk7WorldEvidence $v.request $v.artifact 'PASS'}
Check 'owner-seed Horse items with any native blueprint and slot accepted (no pin)' {$v=New-KmcOwnerSeedWorldFixture $true;$i=$v.artifact.observations.setupComplete.horseItems.horses[0].items[0];$i.blueprint='Saddle_Blueprint';$i.slot='HandSlot';$i.count=3;Assert-KmcChunk7WorldEvidence $v.request $v.artifact 'PASS'}
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
# Owner-seed-derived persistence. Producer: the unchanged disclosed setup and two native Mounts,
# then one native Manual whose read-back graph matches the live runtime at its save boundary.
# The producer disclosure, pinned here independently of the reader under test.
function Get-TestSeedManualDisclosure {'Fixture setup on a disposable run copy of an owner-supplied seed: each Horse was created by the native ordinary Ranger level-up (Hunter''s Bond -> Ranger companion -> KMC Horse) and rider B received only the exact native XP its Ranger 4 eligibility requires. This is not acquisition proof. Setup makes no native write; after SETUP-COMPLETE the only native write is one measured Manual into the run-owned isolated save root, labelled owner-seed-derived with disclosed assistance.'}
function New-KmcOwnerSeedManualFixture{
 $v=New-KmcOwnerSeedWorldFixture $true $false;$r=$v.request;$o=$v.artifact.observations;$p=$r.ownedPairs
 $r.ownedPairs=[pscustomobject][ordered]@{case='owner-seed-persistence-save';seedId=$p.seedId;seedSha256=$p.seedSha256;riderAId=$p.riderAId;riderBId=$p.riderBId;partyIds=$p.partyIds;xpAssistance=$p.xpAssistance;baseGameOnly=$true;expectedNativeLoadCount=1;expectedNativeWriteCount=1}
 $r|Add-Member persistenceCase 'multi-exploration-manual' -Force
 $roles=@($p.riderAId,$o.ownerSeedSetup.riders[0].horseId,$p.riderBId,$o.ownerSeedSetup.riders[1].horseId);$original=@(@($p.partyIds)+@($roles[1],$roles[3]))
 $o.ownerSeed.contract='owner-seed-derived-manual-producer-v1';$o.ownerSeedSetup.disclosure=Get-TestSeedManualDisclosure
 $o.PSObject.Properties.Remove('door');$o.movements=@();$o|Add-Member groundInputs @() -Force;$o.cycleInputs=@();$o.cycles=@()
 $o.inputs=@($o.inputs[0],$o.inputs[1]);$o.measurements=@($o.measurements[0],$o.measurements[1])
 $both=@($o.snapshots|Where-Object label -CEQ 'both-mounted')[0];$complete=Copy-Value $both;$complete.label='native-manual-complete';$o.snapshots=@($both,$complete)
 $o.factRemovalAdmission.commands=@($o.factRemovalAdmission.commands|Where-Object{[long]$_.commandObject-in@([long]$o.inputs[0].commandObject,[long]$o.inputs[1].commandObject)})
 $root=Join-Path (Join-Path (Join-Path (Split-Path (Split-Path $r.evidenceRoot -Parent) -Parent) 'runtime-staging') ('persistence-'+$r.runId)) 'Saved Games'
 $d=$r.persistenceLoad;$name='KMC_C7_SEED_WORLD';$leaf='Manual_303_KMC_C7_SEED_WORLD.zks';$path=Join-Path $root $leaf
 $desc={param($file,$native)[pscustomobject][ordered]@{fileName=$file;nativeName=$native;nativeType='Manual';gameId=$d.gameId;gameName=$d.gameName;area=$d.area;hasFileOnDisk=$true;isActuallySaved=$true;operation='None';autoLevelup=$false}}
 $auth={param($w)[pscustomobject][ordered]@{root=$root;loadCap=1;writeCap=1;nativeLoads=1;nativeWrites=$w;authorizedLoads=1;authorizedWrites=$w;reservationCount=1;number=303;leaf=$leaf;nativeName=$name;nativeType='Manual'}}
 $seed=&$desc $d.fileName $d.internalName;$out=&$desc $leaf $name
 $o.setupComplete|Add-Member measuredOutputReservation ([pscustomobject][ordered]@{leaf=$leaf;nativeName=$name;nativeType='Manual';number=303;reservationCount=1;unwritten=$true}) -Force
 $o|Add-Member saveRequested ([pscustomobject][ordered]@{nativeType='Manual';nativeName=$name;frame=2000;gameTicks=10000000;snapshotCount=0;nativeSaveAllowed=$true;newSlot=$true;levelUpSave=$false;api='Game.SaveGame(SaveManager.CreateNewSave(name))'}) -Force
 $o|Add-Member ownedOutputAdmission ([pscustomobject][ordered]@{method='FindUnusedSaveNumber';token='06008024';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';number=303;leaf=$leaf;nativeInventory=@($seed);authority=(&$auth 0)}) -Force
 $o|Add-Member ownedOutputCompletion ([pscustomobject][ordered]@{completionSequence=1;callbackCount=1;nativeWorkerSettled=$true;nativeInventory=@($seed,$out);authority=(&$auth 1)}) -Force
 $owners=@($both.owners|Where-Object state -CEQ 'Active')
 $actor={param($id,$standard)[pscustomobject][ordered]@{Id=$id;Standard=$standard;Move=0.5;Swift=0.0;Initiative=0.0;Reaction=0.0;ReactionsRemaining=1;LastSurpriseTicks=0}}
 $pairs=@(for($i=0;$i-lt2;$i++){$owner=@($owners|Where-Object{Test-KmcChunk7OwnerActorMatch $_.riderId $roles[$i*2]})[0];[pscustomobject][ordered]@{PairId=(ConvertTo-KmcChunk7PairIdN $owner.ownerId);Generation=[long]$owner.generation;ProfileId='medium-humanoid-horse-v1';Rider=(&$actor $roles[$i*2] 1.0);Mount=(&$actor $roles[$i*2+1] 2.0)}})
 $slots=@(foreach($rider in @($roles[0],$roles[2])){[pscustomobject][ordered]@{ActorId=$rider;Index=12;Kind=2};[pscustomobject][ordered]@{ActorId=$rider;Index=13;Kind=3}})
 $archive=[pscustomobject][ordered]@{SchemaVersion=3;CampaignId=$d.gameId;AreaId=$d.area;GameTimeTicks=10000000;Policy='rider-principal-distinct-native-v1';RulesId='crpg-transport-v1';Pairs=$pairs;Participations=@();Slots=$slots;Combat=$null}
 $o|Add-Member nativeWrite ([pscustomobject][ordered]@{path=$path;sha256=('5'*64);length=2048;lastWriteTimeUtcTicks=2;fileName=$leaf;nativeType='Manual';nativeName=$name;gameId=$d.gameId;gameName=$d.gameName;area=$d.area;nativeCallback=$true;operation='None';snapshotCount=1;readKind='Current';sourceSchema=3;archive=$archive}) -Force
 $arrangement=[pscustomobject][ordered]@{money=30000;experienceRatePercent=100;actors=@($original|Sort-Object -CaseSensitive|ForEach-Object{[pscustomobject][ordered]@{actorId=$_;blueprint=('b'*32);experience=9000;characterLevel=4;classes=@([pscustomobject]@{classGuid=('c'*32);level=4})}})}
 $boundaryPairs=@(for($i=0;$i-lt2;$i++){[pscustomobject][ordered]@{ownerId=$pairs[$i].PairId;generation=$pairs[$i].Generation;riderId=$roles[$i*2];mountId=$roles[$i*2+1];profileId='medium-humanoid-horse-v1';state='Active';relationshipState='Mounted'}})
 $boundarySlots=@($slots|ForEach-Object{[pscustomobject][ordered]@{actorId=$_.ActorId;index=$_.Index;kind=$_.Kind}})
 $boundary=[pscustomobject][ordered]@{label='save-boundary';frame=1990;gameTicks=10000000;actorIds=$roles;pairs=$boundaryPairs;slots=$boundarySlots;actors=@($pairs|ForEach-Object{Copy-Value $_.Rider;Copy-Value $_.Mount});unrelated=@($original|Where-Object{$_-cnotin$roles}|Sort-Object -CaseSensitive|ForEach-Object{[pscustomobject][ordered]@{actorId=$_;native=(&$actor $_ 0.0)}});rosterIds=@($original|Sort-Object -CaseSensitive);arrangement=(Copy-Value $arrangement)}
 $header={param($label,$snaps)[pscustomobject][ordered]@{label=$label;frame=2001;requestedIsSelectedDescriptor=$true;preparedIsCopy=$true;preparedLeaf=$leaf;preparedName=$name;preparedType='Manual';preparedPath=$path;snapshots=$snaps;activeOwners=2;nativeWrites=0;serializationBusy=$false}}
 $o|Add-Member ownerSeedManual ([pscustomobject][ordered]@{contract='owner-seed-derived-manual-v1';ancestry='owner-seed-derived';disclosedAssistance=$true;assistanceCount=@($o.ownerSeedSetup.assistance).Count;acquisitionProof=$false;ordinaryProducerAncestry=$false;levelUpSave=$false;restorationProof=$false;setupCompletedBeforeSave=$true;setupMutationsAfterBoundary=0
  seedId=$p.seedId;seedSha256=$p.seedSha256;seedCopyUnchanged=$true;processId=4242;boundary=$boundary;hotbar=@($boundarySlots|ForEach-Object{[pscustomobject][ordered]@{actorId=$_.actorId;abilityGuid=$(if($_.kind-eq2){'3af2b81f4d72bbb30501fa730fcdf36e'}else{'d'*32});index=$_.index;nativeApi='UnitUISettings.SetSlot(MechanicActionBarSlotAbility)';afterSetupComplete=$true}})
  headerStarting=(&$header 'native-header-starting' 0);headerStaged=(&$header 'native-header-staged' 1);workerObservedRunning=$true;completionSequence=1
  fieldWorld=[pscustomobject][ordered]@{gameId=$d.gameId;gameName=$d.gameName;area=$d.area;mainActorId=$p.riderAId;party=@($original|ForEach-Object{[pscustomobject][ordered]@{id=$_;blueprint=('b'*32)}})}
  twoOwners=@(for($i=0;$i-lt2;$i++){[pscustomobject][ordered]@{actorId=$roles[$i*2];petId=$roles[$i*2+1];sourceGuid=$o.ownerSeedSetup.riders[$i].horseFeatureSourceGuid;featureGuid=('e'*32);rank=1;profileId='medium-humanoid-horse-v1';presentation='fixture-setup';acquisitionProof=$false}})
  arrangement=$arrangement;arrangementAfterSave=(Copy-Value $arrangement)}) -Force
 $o.cleanup.nativeSaveWritten=$true;$o.cleanup.writesAfterBoundary=1;$v.name='owner-seed-manual';$v
}
function Check-SeedManual([string]$Name,[scriptblock]$Mutation,[string]$Pattern=''){$v=New-KmcOwnerSeedManualFixture;&$Mutation $v;Check $Name {Assert-KmcChunk7WorldEvidence $v.request $v.artifact 'PASS'} $true $Pattern}
Check 'owner-seed Manual producer evidence accepted' {$v=New-KmcOwnerSeedManualFixture;Assert-KmcChunk7WorldEvidence $v.request $v.artifact 'PASS'}
Check 'owner-seed Manual producer final root with seed and its one Manual accepted' {$v=New-KmcOwnerSeedManualFixture;Assert-KmcChunk7OwnerSeedManualFinalSaveRoot $v.request @($v.request.persistenceLoad.fileName,'Manual_303_KMC_C7_SEED_WORLD.zks') 'Manual_303_KMC_C7_SEED_WORLD.zks'}
foreach($rootCase in @('seed-only','extra-auto','extra-second-manual','no-seed')){Check ('owner-seed Manual producer final root refuses '+$rootCase) {$v=New-KmcOwnerSeedManualFixture;$s=$v.request.persistenceLoad.fileName;$set=switch($rootCase){'seed-only'{@($s)}'extra-auto'{@($s,'Manual_303_KMC_C7_SEED_WORLD.zks','Auto_1.zks')}'extra-second-manual'{@($s,'Manual_303_KMC_C7_SEED_WORLD.zks','Manual_304_KMC_C7_SEED_WORLD.zks')}'no-seed'{@('Manual_303_KMC_C7_SEED_WORLD.zks')}};Assert-KmcChunk7OwnerSeedManualFinalSaveRoot $v.request $set 'Manual_303_KMC_C7_SEED_WORLD.zks'} $true 'one fresh owner-seed-derived Manual'}
# Write quota and descriptor: one Manual, never a second write, a non-Manual or an out-of-root write.
Check-SeedManual 'owner-seed Manual producer refuses a second native write' {$a=$args[0].artifact.observations.ownedOutputCompletion.authority;$a.nativeWrites=2;$a.authorizedWrites=2}
Check-SeedManual 'owner-seed Manual producer refuses a selection of two writes' {$args[0].request.ownedPairs.expectedNativeWriteCount=2}
Check-SeedManual 'owner-seed Manual producer refuses a non-Manual output' {$w=$args[0].artifact.observations.nativeWrite;$w.nativeType='Auto'}
Check-SeedManual 'owner-seed Manual producer refuses an Auto request' {$args[0].artifact.observations.saveRequested.nativeType='Auto'}
Check-SeedManual 'owner-seed Manual producer refuses an out-of-root write' {$args[0].artifact.observations.nativeWrite.path='C:\foreign\Manual_303_KMC_C7_SEED_WORLD.zks'}
Check-SeedManual 'owner-seed Manual producer refuses a save stamped before SETUP-COMPLETE' {$o=$args[0].artifact.observations;$f=[long]$o.setupComplete.frame-1;$o.ownerSeedManual.boundary.frame=$f;$o.saveRequested.frame=$f;$o.ownerSeedManual.headerStarting.frame=$f;$o.ownerSeedManual.headerStaged.frame=$f}
Check-SeedManual 'owner-seed Manual producer refuses a save request in game time before its boundary' {$o=$args[0].artifact.observations;$o.saveRequested.gameTicks=[long]$o.ownerSeedManual.boundary.gameTicks-1}
Check-SeedManual 'owner-seed Manual producer refuses an acquired-ancestry output name' {$o=$args[0].artifact.observations;$o.nativeWrite.nativeName='KMC_C7_WORLD';$o.ownedOutputAdmission.leaf='Manual_303_KMC_C7_WORLD.zks'}
Check-SeedManual 'owner-seed Manual producer refuses a level-up save label' {$args[0].artifact.observations.saveRequested.levelUpSave=$true}
Check-SeedManual 'owner-seed Manual producer refuses an auto-levelup output descriptor' {$args[0].artifact.observations.ownedOutputCompletion.nativeInventory[1].autoLevelup=$true}
Check-SeedManual 'owner-seed Manual producer refuses a missing callback' {$args[0].artifact.observations.ownedOutputCompletion.callbackCount=0}
Check-SeedManual 'owner-seed Manual producer refuses an unsettled worker' {$args[0].artifact.observations.ownedOutputCompletion.nativeWorkerSettled=$false}
Check-SeedManual 'owner-seed Manual producer refuses a missing KMC staging observation' {$args[0].artifact.observations.ownerSeedManual.PSObject.Properties.Remove('headerStaged')}
Check-SeedManual 'owner-seed Manual producer refuses staging that did not snapshot' {$args[0].artifact.observations.ownerSeedManual.headerStaged.snapshots=0}
Check-SeedManual 'owner-seed Manual producer refuses a write aliasing the seed bytes' {$args[0].artifact.observations.nativeWrite.sha256=$args[0].request.ownedPairs.seedSha256}
Check-SeedManual 'owner-seed Manual producer refuses a schema-2 read-back' {$args[0].artifact.observations.nativeWrite.sourceSchema=2}
Check-SeedManual 'owner-seed Manual producer refuses a reservation already written at SETUP-COMPLETE' {$args[0].artifact.observations.setupComplete.measuredOutputReservation.unwritten=$false}
Check-SeedManual 'owner-seed Manual producer refuses writes after the boundary other than one' {$args[0].artifact.observations.cleanup.writesAfterBoundary=2}
Check-SeedManual 'owner-seed Manual producer refuses an unwritten-save cleanup claim' {$args[0].artifact.observations.cleanup.nativeSaveWritten=$false}
# Ancestry labelling: owner-seed-derived with disclosed assistance only.
foreach($label in @('acquired','ordinary','owner-seed')){Check-SeedManual ('owner-seed Manual producer refuses ancestry '+$label) {param($v)$v.artifact.observations.ownerSeedManual.ancestry=$label}}
Check-SeedManual 'owner-seed Manual producer refuses hidden assistance' {$args[0].artifact.observations.ownerSeedManual.disclosedAssistance=$false}
Check-SeedManual 'owner-seed Manual producer refuses an acquisition-proof claim' {$args[0].artifact.observations.ownerSeedManual.acquisitionProof=$true}
Check-SeedManual 'owner-seed Manual producer refuses an ordinary-ancestry claim' {$args[0].artifact.observations.ownerSeedManual.ordinaryProducerAncestry=$true}
Check-SeedManual 'owner-seed Manual producer refuses a restoration-proof claim' {$args[0].artifact.observations.ownerSeedManual.restorationProof=$true}
Check-SeedManual 'owner-seed Manual producer refuses the exploration disclosure' {$args[0].artifact.observations.ownerSeedSetup.disclosure=Get-KmcChunk7OwnerSeedDisclosure}
Check-SeedManual 'owner-seed Manual producer refuses the exploration contract' {$args[0].artifact.observations.ownerSeed.contract='owner-seed-two-pair-exploration-v1'}
Check-SeedManual 'owner-seed Manual producer refuses an assistance count that hides setup' {$args[0].artifact.observations.ownerSeedManual.assistanceCount=0}
Check-SeedManual 'owner-seed Manual producer refuses a setup mutation after the boundary' {$args[0].artifact.observations.ownerSeedManual.setupMutationsAfterBoundary=1}
Check-SeedManual 'owner-seed Manual producer refuses a changed seed copy' {$args[0].artifact.observations.ownerSeedManual.seedCopyUnchanged=$false}
# The two pair records correspond to the actual runtime.
Check-SeedManual 'owner-seed Manual producer refuses a single read-back pair' {$a=$args[0].artifact.observations.nativeWrite.archive;$a.Pairs=@($a.Pairs[0])}
Check-SeedManual 'owner-seed Manual producer refuses a read-back generation differing from the runtime' {$args[0].artifact.observations.nativeWrite.archive.Pairs[1].Generation=9} 'differ from the live runtime'
Check-SeedManual 'owner-seed Manual producer refuses a read-back owner differing from the runtime' {$args[0].artifact.observations.nativeWrite.archive.Pairs[0].PairId=('9'*32)} 'differ from the live runtime'
Check-SeedManual 'owner-seed Manual producer refuses a boundary owner differing from the live registry' {$args[0].artifact.observations.ownerSeedManual.boundary.pairs[0].generation=7}
Check-SeedManual 'owner-seed Manual producer refuses swapped mount roles' {$a=$args[0].artifact.observations.nativeWrite.archive;$m=$a.Pairs[0].Mount.Id;$a.Pairs[0].Mount.Id=$a.Pairs[1].Mount.Id;$a.Pairs[1].Mount.Id=$m}
Check-SeedManual 'owner-seed Manual producer refuses a read-back slot differing from the hotbar' {$args[0].artifact.observations.nativeWrite.archive.Slots[0].Index=40}
Check-SeedManual 'owner-seed Manual producer refuses read-back expenditure lost from the runtime' {$args[0].artifact.observations.nativeWrite.archive.Pairs[0].Rider.Standard=0.0} 'expenditure differs'
Check-SeedManual 'owner-seed Manual producer refuses read-back expenditure added to the runtime' {$args[0].artifact.observations.nativeWrite.archive.Pairs[1].Mount.Standard=5.0} 'expenditure differs'
Check-SeedManual 'owner-seed Manual producer refuses an omitted unrelated actor' {$b=$args[0].artifact.observations.ownerSeedManual.boundary;$b.unrelated=@($b.unrelated[0])}
Check-SeedManual 'owner-seed Manual producer refuses a changed class/XP arrangement' {$args[0].artifact.observations.ownerSeedManual.arrangementAfterSave.money=1}
Check-SeedManual 'owner-seed Manual producer refuses a move before its save' {$args[0].artifact.observations.movements=@([pscustomobject]@{label='mammoth'})} 'undeclared movements'
Check-SeedManual 'owner-seed Manual producer refuses a matched-pool cycle' {$args[0].artifact.observations.cycles=@([pscustomobject]@{label='cycle-1-mounted'})} 'undeclared cycles'
Check-SeedManual 'owner-seed Manual producer refuses a door step' {$args[0].artifact.observations|Add-Member door ([pscustomobject]@{applicable=$false})} 'door step'
Check-SeedManual 'owner-seed Manual producer refuses a third input' {$o=$args[0].artifact.observations;$o.inputs=@($o.inputs)+@(Copy-Value $o.inputs[0])}
Check 'owner-seed Manual producer refuses a matched-pool artifact' {$v=New-KmcOwnerSeedManualFixture;Assert-KmcMatchedPoolArtifact $v.request ([pscustomobject]@{artifacts=@([pscustomobject]@{relativePath='chunk7-matched-pool-measurements.json';kind='chunk7-matched-pool-measurements'})}) 'PASS'} $true 'cannot supply matched-pool'
Check 'owner-seed exploration selection cannot become a writer' {$v=New-KmcOwnerSeedWorldFixture;$v.request|Add-Member persistenceCase 'multi-exploration-manual' -Force;Assert-KmcChunk7OwnedPairsSelection $v.request} $true 'cannot select a persistence checkpoint'
# Consumer: the separate-process cold load of exactly that Manual, pinned to its producer.
function New-KmcOwnerSeedDerivedColdFixture{
 $v=Copy-Value @($fixtures|Where-Object name -CEQ 'persistence-load-multi-exploration-manual')[0];$r=$v.request;$o=$v.artifact.observations;$p=$r.ownedPairs
 $r.ownedPairs=[pscustomobject][ordered]@{case='owner-seed-persistence-load';sourceRunId='synthetic-owner-seed-manual';sourceArtifactSha256=('6'*64);sourceArchiveSha256=$r.persistenceLoad.sha256;seedId='KMC_C7_OWNER_SEED';seedSha256=('9'*64);riderAId=$p.riderAId;mountAId=$p.mountAId;riderBId=$p.riderBId;mountBId=$p.mountBId;partyIds=$p.partyIds;baseGameOnly=$true;expectedNativeLoadCount=1;expectedNativeWriteCount=0}
 $r.persistenceLoad.internalName='KMC_C7_SEED_WORLD';$r.persistenceLoad.fileName='Manual_303_KMC_C7_SEED_WORLD.zks';$o.loadedDescriptor=Copy-Value $r.persistenceLoad
 foreach($s in @($o.inheritedFixture,$o.inheritedCleanup)){$s.contract='owner-seed-derived-two-horse-native-field-v1';$s.sourceCase='owner-seed-persistence-save';$s.sourceRunId=$r.ownedPairs.sourceRunId;$s.sourceArtifactSha256=$r.ownedPairs.sourceArtifactSha256;$s.sourceArchiveSha256=$r.ownedPairs.sourceArchiveSha256}
 $o.inheritedManualSha256=$r.ownedPairs.sourceArchiveSha256;$a=$o.loadedArchive
 $o|Add-Member ownerSeedDerivedCold ([pscustomobject][ordered]@{contract='owner-seed-derived-cold-v1';ancestry='owner-seed-derived';sourceRunId=$r.ownedPairs.sourceRunId;sourceArtifactSha256=$r.ownedPairs.sourceArtifactSha256;sourceArchiveSha256=$r.ownedPairs.sourceArchiveSha256;seedSha256=$r.ownedPairs.seedSha256;producerProcessId=4242;loadedEqualsProducerReadback=$true;elapsedSeconds=0.5
  owners=@($a.Pairs|ForEach-Object{[pscustomobject][ordered]@{ownerId=$_.PairId;generation=$_.Generation;riderId=$_.Rider.Id;mountId=$_.Mount.Id}});slots=@($a.Slots|ForEach-Object{[pscustomobject][ordered]@{actorId=$_.ActorId;index=$_.Index;kind=$_.Kind}});unrelated=@()}) -Force
 $v.name='persistence-load-owner-seed-derived';$v
}
# The minimal producer receipt fields the runner compares; derived from the consumer fixture.
function New-KmcOwnerSeedDerivedProducer($Cold){
 $r=$Cold.request;$o=$Cold.artifact.observations;$c=$o.ownerSeedDerivedCold
 [pscustomobject][ordered]@{runId=$r.ownedPairs.sourceRunId;status='PASS';commit=$r.commit;dllSha256=$r.dllSha256;dllMvid=$r.dllMvid;observations=[pscustomobject][ordered]@{
  nativeWrite=[pscustomobject][ordered]@{sha256=$r.ownedPairs.sourceArchiveSha256;archive=(Copy-Value $o.loadedArchive)}
  ownerSeedManual=[pscustomobject][ordered]@{ancestry='owner-seed-derived';seedId=$r.ownedPairs.seedId;seedSha256=$r.ownedPairs.seedSha256;processId=4242;arrangement=(Copy-Value $o.inheritedFixture.arrangement)
   boundary=[pscustomobject][ordered]@{pairs=@($c.owners|ForEach-Object{Copy-Value $_});slots=@($c.slots|ForEach-Object{Copy-Value $_});unrelated=@()}
   fieldWorld=[pscustomobject][ordered]@{party=@($r.ownedPairs.partyIds|ForEach-Object{[pscustomobject]@{id=$_}})}}}}
}
function Check-SeedCold([string]$Name,[scriptblock]$Mutation,[string]$Pattern=''){$v=New-KmcOwnerSeedDerivedColdFixture;&$Mutation $v;Check $Name {Invoke-OwnedReader $v} $true $Pattern}
Check 'owner-seed-derived cold evidence accepted' {Invoke-OwnedReader (New-KmcOwnerSeedDerivedColdFixture)}
Check 'owner-seed-derived cold source matches its exact producer' {$v=New-KmcOwnerSeedDerivedColdFixture;Assert-KmcChunk7OwnerSeedDerivedColdSource $v.request (New-KmcOwnerSeedDerivedProducer $v) $v.artifact}
Check-SeedCold 'owner-seed-derived cold refuses acquired custody labels' {$args[0].artifact.observations.inheritedFixture.contract='inherited-two-horse-native-field-v1'}
Check-SeedCold 'owner-seed-derived cold refuses an acquired source case' {foreach($s in @($args[0].artifact.observations.inheritedFixture,$args[0].artifact.observations.inheritedCleanup)){$s.sourceCase='preserve-two-acquired-field'}}
Check-SeedCold 'owner-seed-derived cold refuses its missing cold proof' {$args[0].artifact.observations.PSObject.Properties.Remove('ownerSeedDerivedCold')}
Check-SeedCold 'owner-seed-derived cold refuses a same-process world' {$args[0].artifact.observations.ownerSeedDerivedCold.producerProcessId=$args[0].artifact.observations.processId} 'same-process'
Check-SeedCold 'owner-seed-derived cold refuses decode-only restoration' {$args[0].artifact.observations.ownerSeedDerivedCold.loadedEqualsProducerReadback=$false}
Check-SeedCold 'owner-seed-derived cold refuses a missing restored pair' {$c=$args[0].artifact.observations.ownerSeedDerivedCold;$c.owners=@($c.owners[0])}
Check-SeedCold 'owner-seed-derived cold refuses a restored generation differing from the archive' {$args[0].artifact.observations.ownerSeedDerivedCold.owners[1].generation=9}
Check-SeedCold 'owner-seed-derived cold refuses a restored rider identity differing from the archive' {$args[0].artifact.observations.ownerSeedDerivedCold.owners[0].riderId=$args[0].request.ownedPairs.riderBId}
Check-SeedCold 'owner-seed-derived cold refuses lost control bindings' {$c=$args[0].artifact.observations.ownerSeedDerivedCold;$c.slots=@($c.slots|Select-Object -First 3)}
Check-SeedCold 'owner-seed-derived cold refuses a foreign source pin' {$args[0].artifact.observations.ownerSeedDerivedCold.sourceArtifactSha256=('e'*64)}
Check-SeedCold 'owner-seed-derived cold refuses an acquired-ancestry label' {$args[0].artifact.observations.ownerSeedDerivedCold.ancestry='acquired'}
Check-SeedCold 'owner-seed-derived cold refuses an acquired-ancestry Manual name' {$v=$args[0];$v.request.persistenceLoad.internalName='KMC_C7_WORLD';$v.request.persistenceLoad.fileName='Manual_303_KMC_C7_WORLD.zks';$v.artifact.observations.loadedDescriptor=Copy-Value $v.request.persistenceLoad}
Check-SeedCold 'owner-seed-derived cold refuses its immutable seed as input' {$v=$args[0];$v.request.ownedPairs.seedSha256=$v.request.ownedPairs.sourceArchiveSha256;$v.artifact.observations.ownerSeedDerivedCold.seedSha256=$v.request.ownedPairs.seedSha256}
Check-SeedCold 'owner-seed-derived cold refuses a written native output' {$args[0].request.ownedPairs.expectedNativeWriteCount=1}
Check-SeedCold 'owner-seed-derived cold refuses a second native load' {$args[0].request.ownedPairs.expectedNativeLoadCount=2}
Check-SeedCold 'owner-seed-derived cold refuses carried setup assistance' {$args[0].request.ownedPairs|Add-Member xpAssistance ([pscustomobject]@{unitId=$args[0].request.ownedPairs.riderBId;targetCharacterLevel=8})}
# No extra preparation or grant and no loss of retained debt, captured once.
Check-SeedCold 'owner-seed-derived cold refuses an invented native preparation' {$p=$args[0].artifact.observations.coldRestoration;$p.events=@($p.events)+@([pscustomobject]@{actorId=$args[0].request.ownedPairs.riderAId;kind='prepare-before'})} 'preparation or a new round'
Check-SeedCold 'owner-seed-derived cold refuses an invented native round' {$p=$args[0].artifact.observations.coldRestoration;$p.events=@($p.events)+@([pscustomobject]@{actorId=$args[0].request.ownedPairs.mountBId;kind='round-before'})} 'preparation or a new round'
Check-SeedCold 'owner-seed-derived cold refuses a doubled debt restoration' {$p=$args[0].artifact.observations.coldRestoration;$e=@($p.events|Where-Object{$_.kind-ceq'restore-debt-after'})[0];$p.events=@($p.events)+@(Copy-Value $e)} 'once for each actor'
Check-SeedCold 'owner-seed-derived cold refuses a replayed Mount' {$args[0].artifact.observations.snapshots[0].nativeCastRequests=1}
Check-SeedCold 'owner-seed-derived cold refuses a second semantic restoration' {$s=@($args[0].artifact.observations.snapshots|Where-Object label -CEQ 'cold-restored-before-input')[0];$s.semanticRestores=5}
Check-SeedCold 'owner-seed-derived cold refuses refunded restored debt' {$p=$args[0].artifact.observations.coldRestoration;$e=@($p.events|Where-Object{$_.kind-ceq'restore-debt-after'})[0];$e.debt.Standard=0.0}
Check 'owner-seed-derived cold refuses an unrelated actor granted expenditure' {$v=New-KmcOwnerSeedDerivedColdFixture;$actor={param($s)[pscustomobject][ordered]@{Id='50505050-5050-4050-8050-505050505050';Standard=$s;Move=0.0;Swift=0.0;Initiative=0.0;Reaction=0.0;ReactionsRemaining=1;LastSurpriseTicks=0}};$v.artifact.observations.ownerSeedDerivedCold.unrelated=@([pscustomobject][ordered]@{actorId='50505050-5050-4050-8050-505050505050';before=(&$actor 3.0);after=(&$actor 0.0)});$v.request.ownedPairs.partyIds=@($v.request.ownedPairs.partyIds)+@('50505050-5050-4050-8050-505050505050');Assert-KmcChunk7OwnerSeedDerivedColdObservation $v.request $v.artifact.observations $v.artifact.observations.loadedArchive} $true 'expenditure changed'
Check 'owner-seed-derived cold accepts unrelated expenditure decaying with the clock' {$v=New-KmcOwnerSeedDerivedColdFixture;$actor={param($s)[pscustomobject][ordered]@{Id='50505050-5050-4050-8050-505050505050';Standard=$s;Move=0.0;Swift=0.0;Initiative=0.0;Reaction=0.0;ReactionsRemaining=1;LastSurpriseTicks=0}};$v.artifact.observations.ownerSeedDerivedCold.unrelated=@([pscustomobject][ordered]@{actorId='50505050-5050-4050-8050-505050505050';before=(&$actor 3.0);after=(&$actor 2.6)});$v.request.ownedPairs.partyIds=@($v.request.ownedPairs.partyIds)+@('50505050-5050-4050-8050-505050505050');Assert-KmcChunk7OwnerSeedDerivedColdObservation $v.request $v.artifact.observations $v.artifact.observations.loadedArchive}
# Runner pin: a source without matching producer evidence or with a mismatched hash is refused.
foreach($sourceFault in @('producer-run','producer-failed','archive-hash','input-hash','same-process','graph','generation','slots','arrangement','roster','acquired-ancestry','seed-pin','candidate')){Check ('owner-seed-derived cold source refuses '+$sourceFault) {$v=New-KmcOwnerSeedDerivedColdFixture;$p=New-KmcOwnerSeedDerivedProducer $v;$m=$p.observations.ownerSeedManual
 switch($sourceFault){'producer-run'{$p.runId='another-producer'}'producer-failed'{$p.status='FAIL'}'archive-hash'{$p.observations.nativeWrite.sha256=('e'*64)}'input-hash'{$v.request.persistenceLoad.sha256=('e'*64)}'same-process'{$m.processId=$v.artifact.observations.processId}'graph'{$p.observations.nativeWrite.archive.GameTimeTicks=1}'generation'{$m.boundary.pairs[0].generation=9}'slots'{$m.boundary.slots=@($m.boundary.slots|Select-Object -First 3)}'arrangement'{$m.arrangement.money=1}'roster'{$m.fieldWorld.party=@($m.fieldWorld.party|Select-Object -First 3)}'acquired-ancestry'{$m.ancestry='acquired'}'seed-pin'{$m.seedSha256=('e'*64)}'candidate'{$p.commit=('9'*40)}}
 Assert-KmcChunk7OwnerSeedDerivedColdSource $v.request $p $v.artifact} $true}
# Owner-seed-derived combat (scenarios 3-5): the TB/RT readers on a disposable copy restored mounted.
# Built from the owned combat/RT fixtures: no native Mount inputs, the restored mounted start, the
# derived source and target authorization and, for RT only, the unrelated companion's one action.
$script:seedCompanion='c0c0c0c0-c0c0-40c0-80c0-c0c0c0c0c0c0'
function New-KmcSeedNativeActor([string]$Id,[double]$Standard){[pscustomobject][ordered]@{Id=$Id;Standard=$Standard;Move=0.5;Swift=0.0;Initiative=0.0;Reaction=0.0;ReactionsRemaining=1;LastSurpriseTicks=0}}
# Allocation-trace rows of the companion window, in the native trace's shape.
function New-KmcSeedTraceRow([string]$Boundary,[string]$Actor,[long]$Command,$CommandActor,[double]$Standard,[bool]$Started=$true,[string]$Result='None'){
 [pscustomobject][ordered]@{sequence=0;boundary=$Boundary;frame=200;command=$Command;commandActor=$CommandActor;commandType='Kingmaker.UnitLogic.Commands.UnitAttack';actionType='Standard';started=$Started;acted=$false;finished=$false;result=$Result
  state=[pscustomobject][ordered]@{actor=$Actor;standard=$Standard;move=0;swift=0;reactions=1;reactionCooldown=0}}
}
function New-KmcSeedCompanionRow([string]$Boundary,[long]$Command,$CommandActor,[double]$Standard){@(New-KmcSeedTraceRow $Boundary $script:seedCompanion $Command $CommandActor $Standard $true)}
# A native party-brain proposal KMC refused before native Run: a closed admission pair, never initialized.
function New-KmcSeedProposal([string]$Actor,[long]$Command){@((New-KmcSeedTraceRow 'admission-before' $Actor $Command $null 1.0 $false),(New-KmcSeedTraceRow 'admission-after' $Actor $Command $null 1.0 $false))}
# Rewrites the window: sequences after the pre-window trace, the trace, the accounted copy, the Stop at
# the end; -Producer recomputes the producer's refused proposals from the rows built as proposals.
function Set-KmcSeedCompanionRows($v,$Rows,[bool]$Producer=$false){
 $o=$v.artifact.observations;$c=$o.companionAction;$base=$c.traceSequenceBefore;$i=$base
 foreach($row in $Rows){$i++;$row.sequence=$i}
 $o.allocationTrace.events=@(@($o.allocationTrace.events|Where-Object{$_.sequence-le$base})+@($Rows|ForEach-Object{Copy-Value $_}))
 $c.accountedEvents=@($Rows|ForEach-Object{Copy-Value $_});$c.traceSequenceAfter=$i;$c.nativeStop.traceSequenceBefore=$i;$c.nativeStop.traceSequenceAfter=$i
 if($Producer){$c.pairRefusedProposals=@($Rows|Where-Object{$_.state.actor-cne$script:seedCompanion}|Group-Object command|ForEach-Object{$g=@($_.Group|Sort-Object sequence);[pscustomobject][ordered]@{actorId=$g[0].state.actor;command=$g[0].command;sequenceBefore=$g[0].sequence;sequenceAfter=$g[1].sequence}})}
 if($null-ne$o.PSObject.Properties['playerClick']){$o.PSObject.Properties.Remove('playerClick');Add-KmcSeedPlayerClick $v}
}
# Generated (not a native recording): the Directive 4 R3 first-action Mount on a derived TB fixture.
# Horse A moves on its released turn (round = r1.independentRound); rider A then mounts on its next
# prepared turn: the same round when mount-first (retain, no preparation), the next when rider-first
# (one native partner preparation, recounted from a prepare-after row inside the mount window).
function Add-KmcSeedFirstActionMount($v,$BaseMountInput,[bool]$Retain){
 $o=$v.artifact.observations;$p=$v.request.ownedPairs;$round=[int]$o.r1.independentRound
 $snap={param($label,$frame,$rnd,$horseMove)$x=Copy-Value $o.pendingSurvivor.before;$x.label=$label;$x.frame=$frame;$x.round=$rnd
  foreach($row in $x.actors){if($row.id-ceq$p.mountAId){$row.move=$horseMove}};$x}
 $input=Copy-Value $BaseMountInput;$input.label='first-action-mount-a'
 $at=[Array]::IndexOf(@($o.inputs|ForEach-Object label),'r1-native-dismount-a');$list=[Collections.Generic.List[object]]::new();$list.AddRange(@($o.inputs));$list.Insert($at+1,$input);$o.inputs=$list.ToArray()
 $mountRound=$round+$(if($Retain){0}else{1});$before=&$snap 'first-action-mount-before' 300 $mountRound 1.5;$after=&$snap 'first-action-mount-after' 304 $mountRound $(if($Retain){1.5}else{0.0})
 $neighbour=@($before.owners)[1]
 if(-not$Retain){$n=@($o.allocationTrace.events).Count+1
  $row=[pscustomobject][ordered]@{sequence=$n;boundary='prepare-after';frame=302;command=0;commandActor=$null;commandType=$null;state=[pscustomobject][ordered]@{actor=$p.mountAId;standard=0.0;move=0.0;swift=0.0;reactions=1;reactionCooldown=0.0}}
  $o.allocationTrace.events=@($o.allocationTrace.events)+@($row)}
 $disposition=if($Retain){'RetainPartnerParticipation'}else{'PreparePartnerThisRound'};$calls=if($Retain){0}else{1}
 $o|Add-Member firstActionMount ([pscustomobject][ordered]@{contract='owner-seed-tb-first-action-mount-v1';riderId=$p.riderAId;mountId=$p.mountAId;neighbourOwnerId=$neighbour.id;neighbourGeneration=$neighbour.generation
  horseMove=[pscustomobject][ordered]@{round=$round;before=(&$snap 'horse-independent-move-before' 290 $round 0.0);requestedDistance=0.5;clicked=$true;command=8201;after=(&$snap 'horse-independent-move-after' 294 $round 1.5);distance=0.5;terminal=$true}
  mountTurn=[pscustomobject][ordered]@{round=$mountRound;turnStatus='Preparing';expectedDisposition=$disposition;horseGrantsBefore=3;before=$before
   after=$after;adoptionObservation=('adopted-running-turn;identity=x;principal='+$p.riderAId+';partner='+$p.mountAId+';disposition='+$disposition+';round='+$mountRound+';principalPrepareCalls=0;partnerPrepareCalls='+$calls)
   transactionObservation='committed;synthetic';horseGrantsDuringMount=$calls;terminal=$true}}) -Force
}
# Generated (not a native recording): the Directive 4 player-click window after the companion rows.
# Native brains keep proposing on retained ManualTargets; one rider-A pair attack follows the click.
function Add-KmcSeedPlayerClick($v){
 $o=$v.artifact.observations;$p=$v.request.ownedPairs;$base=[int]$o.companionAction.traceSequenceAfter;$i=$base
 $attack={param($b,$st)$r=New-KmcSeedTraceRow $b $p.riderAId 8101 $p.riderAId $st;$r.commandType='KingmakerMountedCombat.Integration.MountedPairAttackCommand';$r|Add-Member commandTarget $o.targetId -Force;$r}
 $rows=@((New-KmcSeedProposal $p.riderAId 7101)+(&$attack 'admission-after' 0)+(New-KmcSeedProposal $p.mountBId 7102)+(&$attack 'cost-before' 0)+(&$attack 'cost-after' 6))
 foreach($row in $rows){$i++;$row.sequence=$i}
 $o.allocationTrace.events=@($o.allocationTrace.events)+@($rows)
 $snap={param($label,$aStarts,$aActive)$x=Copy-Value $o.companionAction.before;$x.label=$label
  $x|Add-Member intent ([pscustomobject][ordered]@{aStarts=$aStarts;aCancels=1;aRequests=$aStarts;aActive=$aActive;aObservation='synthetic';bStarts=2;bRequests=2;bActive=$false;bObservation='synthetic'}) -Force
  $x|Add-Member manualTargets ([pscustomobject][ordered]@{riderA=$o.targetId;mountB=$o.targetId}) -Force;$x}
 $cost=@($rows|Where-Object boundary -CEQ 'cost-after')[0]
 $o|Add-Member playerClick ([pscustomobject][ordered]@{contract='owner-seed-rt-player-click-integrity-v1';actorId=$p.riderAId;targetId=$o.targetId;traceSequenceBefore=$base
  before=(&$snap 'player-click-before' 4 $false);clicked=$true;admitted=(&$snap 'player-click-admitted' 5 $true)
  stop=[pscustomobject][ordered]@{selectedId=$p.riderAId;traceSequenceBefore=$i;intentAfterStop=$false;manualTargetAfterStop=$null}
  after=(&$snap 'player-click-after' 5 $false);traceSequenceAfter=$i;warnings=@()
  riderAttackCosts=@([pscustomobject][ordered]@{sequence=$cost.sequence;command=8101;commandTarget=$o.targetId});pairRefusedProposals=@();pairBKmcCommandRows=0;terminal=$true}) -Force
}
function Get-KmcSeedCompanionRows($v){@($v.artifact.observations.companionAction.accountedEvents|ForEach-Object{Copy-Value $_})}
function New-KmcOwnerSeedDerivedCombatFixture([string]$Base){
 $v=Copy-Value @($fixtures|Where-Object name -CEQ $Base)[0];$r=$v.request;$o=$v.artifact.observations;$p=$r.ownedPairs;$rt=$Base-ceq'realtime-core'
 $party=@($p.partyIds);if($rt){$party+=$script:seedCompanion}
 $s=[ordered]@{case=$(if($rt){'owner-seed-realtime-core'}else{'owner-seed-'+$Base});sourceRunId='synthetic-owner-seed-manual';sourceArtifactSha256=('6'*64);sourceArchiveSha256=$r.persistenceLoad.sha256;checkpointSourceRunId='synthetic-owner-seed-cold';checkpointSourceArtifactSha256=('8'*64);seedId='KMC_C7_OWNER_SEED';seedSha256=('9'*64);riderAId=$p.riderAId;mountAId=$p.mountAId;riderBId=$p.riderBId;mountBId=$p.mountBId;partyIds=$party}
 if($rt){$s['companionId']=$script:seedCompanion};$s['baseGameOnly']=$true;$s['expectedNativeLoadCount']=1;$s['expectedNativeWriteCount']=0;$r.ownedPairs=[pscustomobject]$s
 $r.persistenceLoad.internalName='KMC_C7_SEED_WORLD';$r.persistenceLoad.fileName='Manual_303_KMC_C7_SEED_WORLD.zks'
 foreach($c in @($o.inheritedFixture,$o.inheritedCleanup)){$c.contract='owner-seed-derived-two-horse-native-field-v1';$c.sourceCase='owner-seed-persistence-save';$c.sourceRunId=$s.sourceRunId;$c.sourceArtifactSha256=$s.sourceArtifactSha256;$c.sourceArchiveSha256=$s.sourceArchiveSha256
  if($rt){$c.partyIds=@($c.partyIds)+@($script:seedCompanion);$c.currentPartyIds=@($c.currentPartyIds)+@($script:seedCompanion);$c.currentCharacterIds=@($c.currentCharacterIds)+@($script:seedCompanion);$row=Copy-Value $c.arrangement.actors[0];$row.actorId=$script:seedCompanion;$c.arrangement.actors=@($c.arrangement.actors)+@($row)}}
 $baseMountA=@($o.inputs|Where-Object label -CEQ 'mount-a')|Select-Object -First 1
 $o.inputs=@($o.inputs|Where-Object{$_.label-cnotin@('mount-a','mount-b')})
 if(-not$rt){Add-KmcSeedFirstActionMount $v $baseMountA ($Base-ceq'combat-mount-first')}
 $roles=@($p.riderAId,$p.mountAId,$p.riderBId,$p.mountBId);$world=Copy-Value $o.snapshots[0];$world.label='mounted-start'
 $owners=@(for($i=0;$i-lt2;$i++){$w=$world.owners[$i];[pscustomobject][ordered]@{ownerId=(ConvertTo-KmcChunk7PairIdN $w.id);generation=$w.generation;riderId=$roles[$i*2];mountId=$roles[$i*2+1]}})
 $o|Add-Member mountedStart ([pscustomobject][ordered]@{contract='owner-seed-derived-mounted-start-v1';restoredFromColdLoad=$true;nativeMountInputs=0;archivePhase='all-pair-presentation-restored';schemaVersion=3;semanticRestores=4;presentationRestores=2;nativePostLoads=4;nativeCastRequests=0;snapshots=0
  profiles=@(0,1|ForEach-Object{[pscustomobject][ordered]@{riderId=$roles[$_*2];mountId=$roles[$_*2+1];profileId='medium-humanoid-horse-v1'}});owners=$owners
  slots=@($roles[0],$roles[2]|ForEach-Object{$id=$_;2,3|ForEach-Object{[pscustomobject][ordered]@{actorId=$id;index=(10+$_);kind=$_}}});loadedEqualsProducerReadback=$true;producerProcessId=4242;processId=5151;debtRetained=$true;elapsedSeconds=0.5
  debt=@($roles|ForEach-Object{[pscustomobject][ordered]@{actorId=$_;saved=(New-KmcSeedNativeActor $_ 3.0);restored=(New-KmcSeedNativeActor $_ 2.6)}});world=$world}) -Force
 $candidate={param($run)[pscustomobject][ordered]@{runId=$run;commit=$r.commit;dllSha256=$r.dllSha256;dllMvid=$r.dllMvid}}
 $o|Add-Member ownerSeedDerivedSource ([pscustomobject][ordered]@{contract='owner-seed-derived-combat-source-v1';ancestry='owner-seed-derived';acquisitionProof=$false;ordinaryProducerAncestry=$false;producerRunId=$s.sourceRunId;producerArtifactSha256=$s.sourceArtifactSha256;derivedManualSha256=$s.sourceArchiveSha256;consumerRunId=$s.checkpointSourceRunId;consumerArtifactSha256=$s.checkpointSourceArtifactSha256;seedId=$s.seedId;seedSha256=$s.seedSha256
  candidates=[pscustomobject][ordered]@{contract='owner-seed-derived-candidate-identity-v1';sameCandidate=$true;crossCandidateAllowed=$true;producer=(&$candidate $s.sourceRunId);current=[pscustomobject][ordered]@{commit=$r.commit;dllSha256=$r.dllSha256;dllMvid=$r.dllMvid};consumerRunId=$s.checkpointSourceRunId;consumerArtifactSha256=$s.checkpointSourceArtifactSha256;manualSchema=3;currentWriterSchema=3}}) -Force
 $o|Add-Member targetAuthorization ([pscustomobject][ordered]@{contract='chunk7-combat-target-authorization-v1';basis='owner-seed-derived-disposable-copy';ownerApproval='docs/CHUNK7-OWNER-DIRECTIVES-2026-10-16.md, Owner decisions on the combat slice (October 17, 2026): combat target approved';workingSha256=$null;seedSha256=$s.seedSha256;derivedManualSha256=$s.sourceArchiveSha256;producerRunId=$s.sourceRunId;producerArtifactSha256=$s.sourceArtifactSha256;consumerRunId=$s.checkpointSourceRunId;consumerArtifactSha256=$s.checkpointSourceArtifactSha256;area=$r.persistenceLoad.area;riderPosition=@(0.0,0.0,0.0);position=@(0.0,0.0,3.5);placementValidated=$true}) -Force
 if($rt){
  $script:seedTraceBase=[int](@($o.allocationTrace.events)|Measure-Object -Property sequence -Maximum).Maximum
  $snap={param($label)$x=Copy-Value $o.snapshots[0];$x.label=$label;$x|Add-Member companion ([pscustomobject]@{actor=$script:seedCompanion}) -Force;$x|Add-Member pairCommands ([pscustomobject][ordered]@{a=@();b=@()}) -Force;$x|Add-Member target ([pscustomobject][ordered]@{id=$o.targetId;prone=$false;proneRequested=$false;conscious=$true;commandsEmpty=$true;commandTypes=@();engagedBy=@()}) -Force;$x}
  $o|Add-Member companionAction ([pscustomobject][ordered]@{contract='owner-seed-unrelated-companion-action-v1';kind='ordinary-native-attack';justification='An ordinary native attack on the diagnostic target spends only her Standard action.';actorId=$script:seedCompanion;targetId=$o.targetId;traceSequenceBefore=$script:seedTraceBase
   before=(&$snap 'companion-before');clicked=$true;command=9001;commandType='Kingmaker.UnitLogic.Commands.UnitAttack';admitted=(&$snap 'companion-admitted');selectionAfterInput=$p.riderAId;after=(&$snap 'companion-after');traceSequenceAfter=0
   accountedEvents=@();companionCommandCount=1;companionCostEvents=2;pairRefusedProposals=@();pairAccountedEvents=0;foreignCompanionEvents=0
   nativeStop=[pscustomobject][ordered]@{selectedId=$script:seedCompanion;selectionAfterStop=$p.riderAId;traceSequenceBefore=0;traceSequenceAfter=0;companionManualTargetCleared=$true;companionCommandsEmpty=$true}
   companionInputs=@('ordinary-attack-click','native-stop');declaredLeaseCommands=1;terminal=$true}) -Force
  # The native shape: riderA and horseB keep ManualTarget after the concurrent primaries, so their
  # brains' proposals interleave with her click; KMC refuses each before native Run.
  $rows=@((New-KmcSeedProposal $p.riderAId 7001)+(New-KmcSeedCompanionRow 'admission-before' 9001 $null 0)+(New-KmcSeedCompanionRow 'admission-after' 9001 $script:seedCompanion 0)+
   (New-KmcSeedProposal $p.mountBId 7002)+(New-KmcSeedProposal $p.riderAId 7003)+(New-KmcSeedCompanionRow 'cost-before' 9001 $script:seedCompanion 0)+(New-KmcSeedCompanionRow 'cost-after' 9001 $script:seedCompanion 5.2)+(New-KmcSeedProposal $p.mountBId 7004))
  Set-KmcSeedCompanionRows $v $rows $true;Add-KmcSeedPlayerClick $v
 }
 $v.name=$Base;$v
}
function Check-SeedCombat([string]$Name,[string]$Base,[scriptblock]$Mutation,[string]$Pattern=''){$v=New-KmcOwnerSeedDerivedCombatFixture $Base;&$Mutation $v;Check $Name {Invoke-OwnedReader $v} $true $Pattern}
foreach($base in @('combat-rider-first','combat-mount-first','realtime-core')){
 Check ('owner-seed-derived combat evidence accepted '+$base) {Invoke-OwnedReader (New-KmcOwnerSeedDerivedCombatFixture $base)}
 Check-SeedCombat ('owner-seed-derived combat refuses a native Mount input '+$base) $base {$o=$args[0].artifact.observations;$o.inputs=@((Copy-Value @($fixtures|Where-Object name -CEQ $base)[0]).artifact.observations.inputs|Where-Object label -CEQ 'mount-a')+@($o.inputs)} 'native Mount input is not admitted'
 Check-SeedCombat ('owner-seed-derived combat refuses a missing mounted start '+$base) $base {$args[0].artifact.observations.PSObject.Properties.Remove('mountedStart')} 'mounted-start admission'
 Check-SeedCombat ('owner-seed-derived combat refuses a missing restored pair '+$base) $base {$m=$args[0].artifact.observations.mountedStart;$m.owners=@($m.owners[0])} 'both restored pairs'
 Check-SeedCombat ('owner-seed-derived combat refuses a wrong restored owner '+$base) $base {$m=$args[0].artifact.observations.mountedStart;$m.owners[1].ownerId=$m.owners[0].ownerId} 'one exact archive'
 Check-SeedCombat ('owner-seed-derived combat refuses an owner bound to another actor '+$base) $base {$m=$args[0].artifact.observations.mountedStart;$m.owners[0].mountId=$m.owners[1].mountId} 'bound to another actor'
 Check-SeedCombat ('owner-seed-derived combat refuses a restored generation differing from the live registry '+$base) $base {$args[0].artifact.observations.mountedStart.owners[1].generation=9} 'generation differs'
 Check-SeedCombat ('owner-seed-derived combat refuses a live owner on another actor '+$base) $base {$args[0].artifact.observations.mountedStart.world.owners[0].riderId=$args[0].request.ownedPairs.riderBId.Replace('-','')} 'differs from the live registry'
 Check-SeedCombat ('owner-seed-derived combat refuses a D-form live owner '+$base) $base {$args[0].artifact.observations.mountedStart.world.owners[0].riderId=$args[0].request.ownedPairs.riderAId} 'compact lowercase Guid'
 Check-SeedCombat ('owner-seed-derived combat refuses a lost Horse profile '+$base) $base {$args[0].artifact.observations.mountedStart.profiles[0].profileId='other'} 'Horse profile'
 Check-SeedCombat ('owner-seed-derived combat refuses lost controls '+$base) $base {$m=$args[0].artifact.observations.mountedStart;$m.slots=@($m.slots|Select-Object -First 3)} 'four restored controls'
 Check-SeedCombat ('owner-seed-derived combat refuses regranted debt '+$base) $base {$args[0].artifact.observations.mountedStart.debt[0].restored.Standard=0.0} 'lost or regranted'
 Check-SeedCombat ('owner-seed-derived combat refuses a same-process start '+$base) $base {$m=$args[0].artifact.observations.mountedStart;$m.producerProcessId=$m.processId} 'same-process'
 Check-SeedCombat ('owner-seed-derived combat refuses a replayed Mount '+$base) $base {$args[0].artifact.observations.mountedStart.nativeCastRequests=1}
 Check-SeedCombat ('owner-seed-derived combat refuses an acquired source label '+$base) $base {$args[0].artifact.observations.ownerSeedDerivedSource.ordinaryProducerAncestry=$true}
 Check-SeedCombat ('owner-seed-derived combat refuses a foreign consumer pin '+$base) $base {$args[0].artifact.observations.ownerSeedDerivedSource.consumerArtifactSha256=('e'*64)}
 Check-SeedCombat ('owner-seed-derived combat refuses a false same-candidate claim '+$base) $base {$args[0].artifact.observations.ownerSeedDerivedSource.candidates.producer.commit=('f'*40)} 'cross-candidate fact'
 Check ('owner-seed-derived combat accepts a disclosed later candidate '+$base) {$v=New-KmcOwnerSeedDerivedCombatFixture $base;$c=$v.artifact.observations.ownerSeedDerivedSource.candidates;$c.producer.commit=('f'*40);$c.sameCandidate=$false;Invoke-OwnedReader $v}
 Check-SeedCombat ('owner-seed-derived combat refuses a Working target basis '+$base) $base {$args[0].artifact.observations.targetAuthorization.basis='exact-working-fixture'}
 Check-SeedCombat ('owner-seed-derived combat refuses a target authorization claiming Working '+$base) $base {$args[0].artifact.observations.targetAuthorization.workingSha256=$args[0].request.fixture.working.sha256} 'never the Working identity'
 Check-SeedCombat ('owner-seed-derived combat refuses an unvalidated placement '+$base) $base {$args[0].artifact.observations.targetAuthorization.placementValidated=$false}
 Check-SeedCombat ('owner-seed-derived combat refuses a non-derived copy '+$base) $base {$r=$args[0].request;$r.persistenceLoad.internalName='KMC_C7_WORLD';$r.persistenceLoad.fileName='Manual_303_KMC_C7_WORLD.zks'}
}
Check-SeedCombat 'owner-seed-derived RT refuses a missing companion action' 'realtime-core' {$args[0].artifact.observations.PSObject.Properties.Remove('companionAction')} 'unrelated companion action'
Check-SeedCombat 'owner-seed-derived RT refuses a reassigned pair command' 'realtime-core' {$args[0].artifact.observations.companionAction.after.pairCommands.b=@([pscustomobject]@{identity=8888;actorId=$args[0].request.ownedPairs.riderBId;type='Kingmaker.UnitLogic.Commands.UnitAttack';finished=$false})} 'reassigned or cancelled'
Check-SeedCombat 'owner-seed-derived RT refuses a pair spending during the action' 'realtime-core' {$args[0].artifact.observations.companionAction.after.actors[2].standard=6} 'spent native resources'
Check-SeedCombat 'owner-seed-derived RT refuses a companion that is a pair actor' 'realtime-core' {$args[0].artifact.observations.companionAction.actorId=$args[0].request.ownedPairs.mountBId}
Check-SeedCombat 'owner-seed-derived RT refuses an unsettled AI lease' 'realtime-core' {$args[0].artifact.observations.companionAction.declaredLeaseCommands=2}
foreach($base in @('combat-rider-first','combat-mount-first')){
 Check-SeedCombat ('owner-seed-derived TB refuses a missing first-action Mount '+$base) $base {$args[0].artifact.observations.PSObject.Properties.Remove('firstActionMount')} 'lacks its first-action Mount'
 Check-SeedCombat ('owner-seed-derived TB refuses a first-action Mount outside the prepared turn '+$base) $base {$args[0].artifact.observations.firstActionMount.mountTurn.turnStatus='Scrolling'} 'prepared turn'
 Check-SeedCombat ('owner-seed-derived TB refuses the other adoption disposition '+$base) $base {$t=$args[0].artifact.observations.firstActionMount.mountTurn;$t.adoptionObservation=$t.adoptionObservation.Replace('Retain','X').Replace('Prepare','Retain').Replace('X','Prepare')} 'adoption disposition'
 Check-SeedCombat ('owner-seed-derived TB refuses a duplicate partner preparation '+$base) $base {$o=$args[0].artifact.observations;$t=$o.firstActionMount.mountTurn;$n=@($o.allocationTrace.events).Count+1;$o.allocationTrace.events=@($o.allocationTrace.events)+@([pscustomobject][ordered]@{sequence=$n;boundary='prepare-after';frame=303;command=0;commandActor=$null;commandType=$null;state=[pscustomobject][ordered]@{actor=$args[0].request.ownedPairs.mountAId;standard=0.0;move=0.0;swift=0.0;reactions=1;reactionCooldown=0.0}})} 'wrong number of times'
 Check-SeedCombat ('owner-seed-derived TB refuses an independent Horse move that spent nothing '+$base) $base {$m=$args[0].artifact.observations.firstActionMount.horseMove;foreach($row in $m.after.actors){if($row.id-ceq$args[0].request.ownedPairs.mountAId){$row.move=0.0}}} 'spent no movement'
 Check-SeedCombat ('owner-seed-derived TB refuses pair B ownership change during the Mount '+$base) $base {$t=$args[0].artifact.observations.firstActionMount.mountTurn;@($t.after.owners)[1].generation=99} 'ownership changed'
 Check-SeedCombat ('owner-seed-derived TB refuses pair B spending during the Mount '+$base) $base {$t=$args[0].artifact.observations.firstActionMount.mountTurn;foreach($row in $t.after.actors){if($row.id-ceq$args[0].request.ownedPairs.riderBId){$row.standard=6.0}}}
}
Check-SeedCombat 'owner-seed-derived TB refuses retained participation replenishing the Horse' 'combat-mount-first' {$t=$args[0].artifact.observations.firstActionMount.mountTurn;foreach($row in $t.after.actors){if($row.id-ceq$args[0].request.ownedPairs.mountAId){$row.move=0.0}}} 'replenished'
Check-SeedCombat 'owner-seed-derived TB refuses a cleared Horse between its move and a retained Mount' 'combat-mount-first' {$o=$args[0].artifact.observations;$n=@($o.allocationTrace.events).Count+1;$o.allocationTrace.events=@($o.allocationTrace.events)+@([pscustomobject][ordered]@{sequence=$n;boundary='clear-before';frame=296;command=0;commandActor=$null;commandType=$null;state=[pscustomobject][ordered]@{actor=$args[0].request.ownedPairs.mountAId;standard=0.0;move=1.5;swift=0.0;reactions=1;reactionCooldown=0.0}})} 'prepared or cleared the Horse'
Check-SeedCombat 'owner-seed-derived RT refuses a missing player-click window' 'realtime-core' {$args[0].artifact.observations.PSObject.Properties.Remove('playerClick')} 'player-click integrity window'
Check-SeedCombat 'owner-seed-derived RT refuses a click that started no pair A intent' 'realtime-core' {$args[0].artifact.observations.playerClick.admitted.intent.aStarts=4} 'exactly one pair A stock intent'
Check-SeedCombat 'owner-seed-derived RT refuses a click that started a pair B intent' 'realtime-core' {$args[0].artifact.observations.playerClick.after.intent.bStarts=3} 'exactly one pair A stock intent'
Check-SeedCombat 'owner-seed-derived RT refuses a second rider-A attack before Stop' 'realtime-core' {$o=$args[0].artifact.observations;$p=$o.playerClick;$c=Copy-Value @($o.allocationTrace.events|Where-Object{$_.boundary-ceq'cost-after'-and$_.command-eq8101})[0];$c.command=8102;$c.sequence=@($o.allocationTrace.events).Count+1;$o.allocationTrace.events=@($o.allocationTrace.events)+@($c);$p.traceSequenceAfter=$c.sequence;$p.stop.traceSequenceBefore=$c.sequence} 'exactly one rider-A native attack'
Check-SeedCombat 'owner-seed-derived RT refuses the click attack on another target' 'realtime-core' {$o=$args[0].artifact.observations;@($o.allocationTrace.events|Where-Object{$_.boundary-ceq'cost-after'-and$_.command-eq8101})[0].commandTarget=$args[0].request.ownedPairs.riderBId} 'exactly one rider-A native attack'
Check-SeedCombat 'owner-seed-derived RT refuses a KMC command on pair B' 'realtime-core' {$o=$args[0].artifact.observations;$r=@($o.allocationTrace.events|Where-Object{$_.command-eq7102})[0];$r.commandType='KingmakerMountedCombat.Integration.MountedPairAttackCommand'} 'Pair B received a KMC attack'
Check-SeedCombat 'owner-seed-derived RT refuses a warning during refused proposals' 'realtime-core' {$args[0].artifact.observations.playerClick.warnings=@([pscustomobject]@{text='Select the rider to attack, or use the mount on its own turn.';frame=200})} 'player-facing warning'
Check-SeedCombat 'owner-seed-derived RT refuses an intent surviving Stop' 'realtime-core' {$args[0].artifact.observations.playerClick.stop.intentAfterStop=$true}
Check-SeedCombat 'owner-seed-derived RT refuses pair B spending during the click' 'realtime-core' {$args[0].artifact.observations.playerClick.after.actors[3].standard=6} 'Pair B spent'
Check-SeedCombat 'owner-seed-derived RT refuses a window opened on a prone target' 'realtime-core' {$args[0].artifact.observations.companionAction.before.target.prone=$true}
Check-SeedCombat 'owner-seed-derived RT refuses a window without the target state' 'realtime-core' {$args[0].artifact.observations.companionAction.before.PSObject.Properties.Remove('target')} 'diagnostic target state'
Check 'owner-seed-derived RT accepts interleaved refused pair proposals' {$v=New-KmcOwnerSeedDerivedCombatFixture 'realtime-core';if(@($v.artifact.observations.companionAction.pairRefusedProposals).Count-ne4){throw 'fixture lost its proposals'};Invoke-OwnedReader $v}
Check 'owner-seed-derived RT accepts a window without proposals' {$v=New-KmcOwnerSeedDerivedCombatFixture 'realtime-core';Set-KmcSeedCompanionRows $v @(Get-KmcSeedCompanionRows $v|Where-Object{$_.state.actor-ceq$script:seedCompanion}) $true;Invoke-OwnedReader $v}
Check-SeedCombat 'owner-seed-derived RT refuses the native run shape: two costs and no admission' 'realtime-core' {Set-KmcSeedCompanionRows $args[0] @(Get-KmcSeedCompanionRows $args[0]|Where-Object{-not($_.state.actor-ceq$script:seedCompanion-and$_.boundary-like'admission-*')}) $true} 'exactly one native command'
Check-SeedCombat 'owner-seed-derived RT refuses an admission not executed by her' 'realtime-core' {$rows=Get-KmcSeedCompanionRows $args[0];foreach($r in $rows){if($r.state.actor-ceq$script:seedCompanion-and$r.boundary-ceq'admission-after'){$r.commandActor=$null}};Set-KmcSeedCompanionRows $args[0] $rows $true} 'exactly one native command'
Check-SeedCombat 'owner-seed-derived RT refuses an admitted pair attempt' 'realtime-core' {$rows=Get-KmcSeedCompanionRows $args[0];$rows[1].commandActor=$args[0].request.ownedPairs.riderAId;$rows[1].started=$true;Set-KmcSeedCompanionRows $args[0] $rows} 'pair actor was admitted'
Check-SeedCombat 'owner-seed-derived RT refuses a pair cost' 'realtime-core' {$p=$args[0].request.ownedPairs;$rows=@(Get-KmcSeedCompanionRows $args[0])+@((New-KmcSeedTraceRow 'cost-before' $p.mountBId 7100 $p.mountBId 0),(New-KmcSeedTraceRow 'cost-after' $p.mountBId 7100 $p.mountBId 3));Set-KmcSeedCompanionRows $args[0] $rows} 'pair actor was admitted'
Check-SeedCombat 'owner-seed-derived RT refuses a pair actor cost' 'realtime-core' {$p=$args[0].request.ownedPairs;$rows=@(Get-KmcSeedCompanionRows $args[0])+@(New-KmcSeedTraceRow 'actor-cost-before' $p.riderBId 7101 $p.riderBId 0);Set-KmcSeedCompanionRows $args[0] $rows} 'pair actor was admitted'
Check-SeedCombat 'owner-seed-derived RT refuses a pair command-interrupt' 'realtime-core' {$p=$args[0].request.ownedPairs;$rows=@(Get-KmcSeedCompanionRows $args[0])+@(New-KmcSeedTraceRow 'command-interrupt-before' $p.riderAId 7102 $p.riderAId 1);Set-KmcSeedCompanionRows $args[0] $rows} 'pair actor was admitted'
Check-SeedCombat 'owner-seed-derived RT refuses a refused identity reappearing in a cost row' 'realtime-core' {$p=$args[0].request.ownedPairs;$rows=@(Get-KmcSeedCompanionRows $args[0])+@(New-KmcSeedTraceRow 'cost-before' $p.riderAId 7001 $p.riderAId 0);Set-KmcSeedCompanionRows $args[0] $rows} 'pair actor was admitted'
Check-SeedCombat 'owner-seed-derived RT refuses a refused identity held in a pair command slot' 'realtime-core' {$args[0].artifact.observations.companionAction.admitted.pairCommands.a=@([pscustomobject]@{identity=7003;actorId=$args[0].request.ownedPairs.riderAId;type='Kingmaker.UnitLogic.Commands.UnitAttack';finished=$false})} 'pair actor was admitted|reassigned or cancelled'
Check-SeedCombat 'owner-seed-derived RT refuses an unpaired pair admission' 'realtime-core' {$rows=@(Get-KmcSeedCompanionRows $args[0]|Where-Object{-not($_.command-eq7004-and$_.boundary-ceq'admission-after')});Set-KmcSeedCompanionRows $args[0] $rows} 'pair actor was admitted'
Check-SeedCombat 'owner-seed-derived RT refuses two afters for one proposal' 'realtime-core' {$rows=@(Get-KmcSeedCompanionRows $args[0]);$extra=Copy-Value @($rows|Where-Object{$_.command-eq7002-and$_.boundary-ceq'admission-after'})[0];Set-KmcSeedCompanionRows $args[0] (@($rows)+@($extra))} 'pair actor was admitted'
Check-SeedCombat 'owner-seed-derived RT refuses a second companion identity' 'realtime-core' {$rows=@(Get-KmcSeedCompanionRows $args[0])+@(New-KmcSeedCompanionRow 'admission-before' 9002 $null 5.2)+@(New-KmcSeedCompanionRow 'admission-after' 9002 $script:seedCompanion 5.2);Set-KmcSeedCompanionRows $args[0] $rows $true} 'exactly one native command|second native command'
Check-SeedCombat 'owner-seed-derived RT refuses a second companion cost identity' 'realtime-core' {$rows=@(Get-KmcSeedCompanionRows $args[0])+@(New-KmcSeedCompanionRow 'cost-before' 9003 $script:seedCompanion 0);Set-KmcSeedCompanionRows $args[0] $rows $true} 'second native command'
Check-SeedCombat 'owner-seed-derived RT refuses a missing cost event' 'realtime-core' {Set-KmcSeedCompanionRows $args[0] @(Get-KmcSeedCompanionRows $args[0]|Where-Object{$_.boundary-cne'cost-after'}) $true} 'single native cost'
Check-SeedCombat 'owner-seed-derived RT refuses producer proposals differing from the trace' 'realtime-core' {$c=$args[0].artifact.observations.companionAction;$c.pairRefusedProposals=@($c.pairRefusedProposals|Select-Object -First 3)} 'refused pair proposals differ'
Check-SeedCombat 'owner-seed-derived RT refuses a producer proposal on another actor' 'realtime-core' {$args[0].artifact.observations.companionAction.pairRefusedProposals[0].actorId=$args[0].request.ownedPairs.riderBId} 'refused pair proposals differ'
Check-SeedCombat 'owner-seed-derived RT refuses a recorded pair touch count' 'realtime-core' {$args[0].artifact.observations.companionAction.pairAccountedEvents=1} 'pair actor was admitted'
Check-SeedCombat 'owner-seed-derived RT refuses an unaccounted trace window' 'realtime-core' {$c=$args[0].artifact.observations.companionAction;$c.accountedEvents=@($c.accountedEvents|Select-Object -First 3)} 'differ from the native trace'
Check-SeedCombat 'owner-seed-derived RT refuses a second Stop' 'realtime-core' {$args[0].artifact.observations.companionAction.companionInputs=@('ordinary-attack-click','native-stop','native-stop')} 'undeclared native input'
Check-SeedCombat 'owner-seed-derived RT refuses another companion input' 'realtime-core' {$args[0].artifact.observations.companionAction.companionInputs=@('ordinary-attack-click','native-stop','ordinary-move-click')} 'undeclared native input'
Check-SeedCombat 'owner-seed-derived RT refuses a missing Stop' 'realtime-core' {$args[0].artifact.observations.companionAction.companionInputs=@('ordinary-attack-click')} 'undeclared native input'
Check-SeedCombat 'owner-seed-derived RT refuses a Stop with another actor selected' 'realtime-core' {$args[0].artifact.observations.companionAction.nativeStop.selectedId=$args[0].request.ownedPairs.riderAId}
Check-SeedCombat 'owner-seed-derived RT refuses a Stop that left her ManualTarget' 'realtime-core' {$args[0].artifact.observations.companionAction.nativeStop.companionManualTargetCleared=$false}
Check-SeedCombat 'owner-seed-derived RT refuses a Stop before her cost' 'realtime-core' {$c=$args[0].artifact.observations.companionAction;$c.nativeStop.traceSequenceBefore=$c.traceSequenceBefore+1}
Check-SeedCombat 'owner-seed-derived RT refuses a companion row after the Stop' 'realtime-core' {$rows=@(Get-KmcSeedCompanionRows $args[0])+@(New-KmcSeedCompanionRow 'command-interrupt-before' 9001 $script:seedCompanion 5.2);Set-KmcSeedCompanionRows $args[0] $rows $true;$c=$args[0].artifact.observations.companionAction;$c.nativeStop.traceSequenceBefore=$c.traceSequenceAfter-1} 'native Stop admitted'
Check 'producer-backed RT refuses a companion action' {$v=New-KmcOwnerSeedDerivedCombatFixture 'realtime-core';$plain=Copy-Value @($fixtures|Where-Object name -CEQ 'realtime-core')[0];$plain.artifact.observations|Add-Member companionAction $v.artifact.observations.companionAction -Force;Invoke-OwnedReader $plain} $true 'companion'
Check 'producer-backed TB refuses a mounted start' {$v=New-KmcOwnerSeedDerivedCombatFixture 'combat-rider-first';$plain=Copy-Value @($fixtures|Where-Object name -CEQ 'combat-rider-first')[0];Assert-KmcChunk7OwnerSeedDerivedCombatStart $plain.request $v.artifact.observations} $true 'belongs only'
# Owner-seed checkpoint pair (Directive 4), built from the owned Quick/Auto fixtures. Producer: a
# disposable copy of the derived Manual restored mounted, no native Mount input, the verified
# restored start adopted exactly, one genuine native Quick/Auto. Consumer: that checkpoint cold,
# compared with the producer's own recorded read-back (never the Manual boundary).
function New-KmcOwnerSeedCheckpointSelection($p,[string]$Case,[string]$Archive,[string]$Parent){
 [pscustomobject][ordered]@{case=$Case;sourceRunId='synthetic-owner-seed-manual';sourceArtifactSha256=('6'*64);sourceArchiveSha256=$Archive;checkpointSourceRunId=$Parent;checkpointSourceArtifactSha256=('8'*64);seedId='KMC_C7_OWNER_SEED';seedSha256=('9'*64);riderAId=$p.riderAId;mountAId=$p.mountAId;riderBId=$p.riderBId;mountBId=$p.mountBId;partyIds=$p.partyIds;baseGameOnly=$true;expectedNativeLoadCount=1;expectedNativeWriteCount=$(if($Case-ceq'owner-seed-checkpoint-save'){1}else{0})}
}
function Set-KmcOwnerSeedDerivedLabels($o,$s){foreach($c in @($o.inheritedFixture,$o.inheritedCleanup)){$c.contract='owner-seed-derived-two-horse-native-field-v1';$c.sourceCase='owner-seed-persistence-save';$c.sourceRunId=$s.sourceRunId;$c.sourceArtifactSha256=$s.sourceArtifactSha256;$c.sourceArchiveSha256=$s.sourceArchiveSha256};$o.inheritedManualSha256=$s.sourceArchiveSha256}
function New-KmcOwnerSeedCheckpointSaveFixture([string]$Checkpoint='multi-exploration-quick'){
 $v=Copy-Value @($fixtures|Where-Object name -CEQ ('persistence-save-'+$Checkpoint))[0];$r=$v.request;$o=$v.artifact.observations;$p=$r.ownedPairs
 $old=$r.persistenceLoad.fileName;$s=New-KmcOwnerSeedCheckpointSelection $p 'owner-seed-checkpoint-save' $r.persistenceLoad.sha256 'synthetic-owner-seed-cold';$r.ownedPairs=$s
 $r.persistenceLoad.internalName='KMC_C7_SEED_WORLD';$r.persistenceLoad.fileName='Manual_303_KMC_C7_SEED_WORLD.zks'
 foreach($row in @($o.ownedOutputAdmission.nativeInventory)+@($o.ownedOutputCompletion.nativeInventory)){if($row.fileName-ceq$old){$row.fileName=$r.persistenceLoad.fileName;$row.nativeName=$r.persistenceLoad.internalName}}
 Set-KmcOwnerSeedDerivedLabels $o $s;$o.inputs=@();$o.PSObject.Properties.Remove('acquisition')
 $a=$o.nativeWrite.archive;$roles=@($p.riderAId,$p.mountAId,$p.riderBId,$p.mountBId)
 $before=Copy-Value @($o.snapshots|Where-Object label -CEQ 'native-write-complete')[0];$before.label='before-native-save';$o.snapshots=@($before)+@($o.snapshots)
 $o|Add-Member mountedStart ([pscustomobject][ordered]@{contract='owner-seed-checkpoint-restored-start-v1';restoredFromColdLoad=$true;nativeMountInputs=0;newSlotBindings=0;archivePhase='all-pair-presentation-restored';schemaVersion=3;semanticRestores=4;presentationRestores=2;nativePostLoads=4;nativeCastRequests=0;snapshots=0
  profiles=@(0,1|ForEach-Object{[pscustomobject][ordered]@{riderId=$roles[$_*2];mountId=$roles[$_*2+1];profileId='medium-humanoid-horse-v1'}})
  owners=@($a.Pairs|ForEach-Object{[pscustomobject][ordered]@{ownerId=(ConvertTo-KmcChunk7PairIdN $_.PairId);generation=$_.Generation;riderId=$_.Rider.Id;mountId=$_.Mount.Id}})
  slots=@($a.Slots|ForEach-Object{[pscustomobject][ordered]@{actorId=$_.ActorId;index=$_.Index;kind=$_.Kind}});loadedEqualsProducerReadback=$true;producerProcessId=4242;processId=$o.processId;debtRetained=$true;loadedArchive=(Copy-Value $a)}) -Force
 $o|Add-Member ownerSeedCheckpointSource ([pscustomobject][ordered]@{contract='owner-seed-checkpoint-source-v1';role='producer';ancestry='owner-seed-derived';acquisitionProof=$false;ordinaryProducerAncestry=$false;manualProducerRunId=$s.sourceRunId;manualProducerArtifactSha256=$s.sourceArtifactSha256;manualSha256=$s.sourceArchiveSha256;acceptedConsumerRunId=$s.checkpointSourceRunId;acceptedConsumerArtifactSha256=$s.checkpointSourceArtifactSha256;seedId=$s.seedId;seedSha256=$s.seedSha256;checkpoint=$Checkpoint
  candidates=[pscustomobject][ordered]@{contract='owner-seed-derived-candidate-identity-v1';sameCandidate=$true;crossCandidateAllowed=$true;producer=[pscustomobject][ordered]@{runId=$s.sourceRunId;commit=$r.commit;dllSha256=$r.dllSha256;dllMvid=$r.dllMvid};current=[pscustomobject][ordered]@{commit=$r.commit;dllSha256=$r.dllSha256;dllMvid=$r.dllMvid};consumerRunId=$s.checkpointSourceRunId;consumerArtifactSha256=$s.checkpointSourceArtifactSha256;manualSchema=3;currentWriterSchema=3}}) -Force
 $v.name='persistence-owner-seed-checkpoint-save-'+$Checkpoint;$v
}
function New-KmcOwnerSeedCheckpointLoadFixture([string]$Checkpoint='multi-exploration-quick'){
 $v=Copy-Value @($fixtures|Where-Object name -CEQ ('persistence-load-'+$Checkpoint))[0];$r=$v.request;$o=$v.artifact.observations
 $s=New-KmcOwnerSeedCheckpointSelection $r.ownedPairs 'owner-seed-checkpoint-load' ('7'*64) 'synthetic-owner-seed-checkpoint';$r.ownedPairs=$s;Set-KmcOwnerSeedDerivedLabels $o $s
 $v.name='persistence-owner-seed-checkpoint-load-'+$Checkpoint;$v
}
# The checkpoint producer receipt fields the runner compares; derived from the consumer fixture.
function New-KmcOwnerSeedCheckpointProducer($Cold){
 $r=$Cold.request;$o=$Cold.artifact.observations;$s=$r.ownedPairs;$a=$o.loadedArchive
 [pscustomobject][ordered]@{runId=$s.checkpointSourceRunId;status='PASS';observations=[pscustomobject][ordered]@{checkpoint=$r.persistenceCase;cold=$false;processId=4343
  nativeWrite=[pscustomobject][ordered]@{sha256=$r.persistenceLoad.sha256;length=$r.persistenceLoad.length;archive=(Copy-Value $a)}
  ownerSeedCheckpointSource=[pscustomobject][ordered]@{manualProducerRunId=$s.sourceRunId;manualProducerArtifactSha256=$s.sourceArtifactSha256;manualSha256=$s.sourceArchiveSha256;seedId=$s.seedId;seedSha256=$s.seedSha256}
  mountedStart=[pscustomobject][ordered]@{owners=@($a.Pairs|ForEach-Object{[pscustomobject][ordered]@{ownerId=(ConvertTo-KmcChunk7PairIdN $_.PairId);generation=$_.Generation;riderId=$_.Rider.Id;mountId=$_.Mount.Id}});slots=@($a.Slots|ForEach-Object{[pscustomobject][ordered]@{actorId=$_.ActorId;index=$_.Index;kind=$_.Kind}})}
  inheritedCleanup=(Copy-Value $o.inheritedFixture)}}
}
function Check-SeedCheckpointSave([string]$Name,[scriptblock]$Mutation,[string]$Pattern=''){$v=New-KmcOwnerSeedCheckpointSaveFixture;&$Mutation $v;Check $Name {Invoke-OwnedReader $v} $true $Pattern}
function Check-SeedCheckpointLoad([string]$Name,[scriptblock]$Mutation,[string]$Pattern=''){$v=New-KmcOwnerSeedCheckpointLoadFixture;&$Mutation $v;Check $Name {Invoke-OwnedReader $v} $true $Pattern}
foreach($checkpointCase in @('multi-exploration-quick','multi-exploration-auto')){
 Check ('owner-seed checkpoint producer evidence accepted '+$checkpointCase) {$v=New-KmcOwnerSeedCheckpointSaveFixture $checkpointCase;Invoke-OwnedReader $v;Assert-KmcChunk7OwnedOutputEvidence $v.request $v.artifact.observations}
 Check ('owner-seed checkpoint consumer evidence accepted '+$checkpointCase) {Invoke-OwnedReader (New-KmcOwnerSeedCheckpointLoadFixture $checkpointCase)}
 Check ('owner-seed checkpoint consumer matches its producer read-back '+$checkpointCase) {$v=New-KmcOwnerSeedCheckpointLoadFixture $checkpointCase;Assert-KmcChunk7OwnerSeedCheckpointColdSource $v.request (New-KmcOwnerSeedCheckpointProducer $v) $v.artifact}
}
Check 'owner-seed checkpoint producer maps to the generic save scenario' {if((Get-KmcChunk7SelectedScenario (New-KmcOwnerSeedCheckpointSaveFixture).request)-cne'chunk7-persistence-save'-or(Get-KmcChunk7OwnedWriteCount (New-KmcOwnerSeedCheckpointSaveFixture).request)-ne1){throw 'Checkpoint producer scenario or write quota differs'}}
Check 'owner-seed checkpoint consumer maps to the generic load scenario' {if((Get-KmcChunk7SelectedScenario (New-KmcOwnerSeedCheckpointLoadFixture).request)-cne'chunk7-persistence-load'-or(Get-KmcChunk7OwnedWriteCount (New-KmcOwnerSeedCheckpointLoadFixture).request)-ne0){throw 'Checkpoint consumer scenario or write quota differs'}}
# Refusals: Mount inputs on a derived start, restored-start mismatch, wrong ancestry, wrong quotas, a Manual for a Quick.
Check-SeedCheckpointSave 'owner-seed checkpoint producer refuses native Mount inputs on its derived start' {$plain=Copy-Value @($fixtures|Where-Object name -CEQ 'persistence-save-multi-exploration-quick')[0];$args[0].artifact.observations.inputs=$plain.artifact.observations.inputs} 'Mount input is not admitted'
Check-SeedCheckpointSave 'owner-seed checkpoint producer refuses a missing restored start' {$args[0].artifact.observations.PSObject.Properties.Remove('mountedStart')} 'restored mounted-start'
Check-SeedCheckpointSave 'owner-seed checkpoint producer refuses a restored start that re-mounted' {$args[0].artifact.observations.mountedStart.nativeMountInputs=2}
Check-SeedCheckpointSave 'owner-seed checkpoint producer refuses new slot bindings' {$args[0].artifact.observations.mountedStart.newSlotBindings=4}
Check-SeedCheckpointSave 'owner-seed checkpoint producer refuses a decode-only restored start' {$args[0].artifact.observations.mountedStart.loadedEqualsProducerReadback=$false}
Check-SeedCheckpointSave 'owner-seed checkpoint producer refuses a same-process restored start' {$m=$args[0].artifact.observations.mountedStart;$m.producerProcessId=$m.processId} 'same-process'
Check-SeedCheckpointSave 'owner-seed checkpoint producer refuses a restored-start generation the checkpoint changed' {$args[0].artifact.observations.mountedStart.owners[1].generation=9} 'changed a restored owner'
Check-SeedCheckpointSave 'owner-seed checkpoint producer refuses a restored-start owner bound to another actor' {$args[0].artifact.observations.mountedStart.owners[0].mountId=$args[0].request.ownedPairs.mountBId}
Check-SeedCheckpointSave 'owner-seed checkpoint producer refuses restored slots the checkpoint changed' {$args[0].artifact.observations.mountedStart.slots[0].index=99} 'control bindings'
Check-SeedCheckpointSave 'owner-seed checkpoint producer refuses a lost restored control' {$m=$args[0].artifact.observations.mountedStart;$m.slots=@($m.slots|Select-Object -First 3)}
Check-SeedCheckpointSave 'owner-seed checkpoint producer refuses a non-Horse restored profile' {$args[0].artifact.observations.mountedStart.profiles[0].profileId='large-humanoid-mammoth-v1'}
Check-SeedCheckpointSave 'owner-seed checkpoint producer refuses a restored start from another campaign' {$args[0].artifact.observations.mountedStart.loadedArchive.CampaignId='aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa'}
Check-SeedCheckpointSave 'owner-seed checkpoint producer refuses a restored combat graph' {$args[0].artifact.observations.mountedStart.loadedArchive|Add-Member Combat ([pscustomobject]@{TurnBased=$true}) -Force}
Check-SeedCheckpointSave 'owner-seed checkpoint producer refuses a missing before-save capture' {$o=$args[0].artifact.observations;$o.snapshots=@($o.snapshots|Where-Object label -CNE 'before-native-save')} 'before-native-save'
Check-SeedCheckpointSave 'owner-seed checkpoint producer refuses acquired custody labels' {$args[0].artifact.observations.inheritedFixture.contract='inherited-two-horse-native-field-v1'}
Check-SeedCheckpointSave 'owner-seed checkpoint producer refuses an acquired source label' {$args[0].artifact.observations.ownerSeedCheckpointSource.ancestry='acquired'}
Check-SeedCheckpointSave 'owner-seed checkpoint producer refuses a foreign accepted-consumer pin' {$args[0].artifact.observations.ownerSeedCheckpointSource.acceptedConsumerArtifactSha256='e'*64}
Check-SeedCheckpointSave 'owner-seed checkpoint producer refuses a relabelled checkpoint' {$args[0].artifact.observations.ownerSeedCheckpointSource.checkpoint='multi-exploration-manual'}
Check-SeedCheckpointSave 'owner-seed checkpoint producer refuses a dishonest cross-candidate fact' {$args[0].artifact.observations.ownerSeedCheckpointSource.candidates.sameCandidate=$false} 'cross-candidate'
Check-SeedCheckpointSave 'owner-seed checkpoint producer refuses a missing consumer receipt pin' {$args[0].request.ownedPairs.checkpointSourceRunId=$null}
Check-SeedCheckpointSave 'owner-seed checkpoint producer refuses a Manual written instead of a Quick' {$args[0].request.persistenceCase='multi-exploration-manual';$args[0].artifact.observations.checkpoint='multi-exploration-manual'} 'connected checkpoints'
Check-SeedCheckpointSave 'owner-seed checkpoint producer refuses a zero write quota' {$args[0].request.ownedPairs.expectedNativeWriteCount=0}
Check-SeedCheckpointSave 'owner-seed checkpoint producer refuses the immutable seed as input' {$v=$args[0];$v.request.ownedPairs.seedSha256=$v.request.persistenceLoad.sha256}
Check-SeedCheckpointSave 'owner-seed checkpoint producer refuses an acquired-ancestry Manual input' {$v=$args[0];$v.request.persistenceLoad.internalName='KMC_C7_WORLD';$v.request.persistenceLoad.fileName='Manual_303_KMC_C7_WORLD.zks'}
Check 'plain owned save refuses a restored mounted start' {$v=New-KmcOwnerSeedCheckpointSaveFixture;$plain=Copy-Value @($fixtures|Where-Object name -CEQ 'persistence-save-multi-exploration-quick')[0];$plain.artifact.observations|Add-Member mountedStart $v.artifact.observations.mountedStart -Force;Invoke-OwnedReader $plain} $true 'Only the owner-seed checkpoint producer'
Check 'restored start belongs only to the checkpoint producer' {$v=New-KmcOwnerSeedCheckpointSaveFixture;$plain=Copy-Value @($fixtures|Where-Object name -CEQ 'persistence-save-multi-exploration-quick')[0];Assert-KmcChunk7OwnerSeedCheckpointStart $plain.request $v.artifact.observations $v.artifact.observations.nativeWrite.archive} $true 'belongs only'
Check-SeedCheckpointLoad 'owner-seed checkpoint consumer refuses a write quota' {$args[0].request.ownedPairs.expectedNativeWriteCount=1}
Check-SeedCheckpointLoad 'owner-seed checkpoint consumer refuses the derived Manual as its input' {$v=$args[0];$v.request.ownedPairs.sourceArchiveSha256=$v.request.persistenceLoad.sha256;Set-KmcOwnerSeedDerivedLabels $v.artifact.observations $v.request.ownedPairs} 'aliases its Manual'
Check-SeedCheckpointLoad 'owner-seed checkpoint consumer refuses a Manual-named input' {$v=$args[0];$v.request.persistenceLoad.fileName='Manual_304_KMC_C7_SEED_WORLD.zks';$v.artifact.observations.loadedDescriptor=Copy-Value $v.request.persistenceLoad}
Check-SeedCheckpointLoad 'owner-seed checkpoint consumer refuses acquired custody labels' {$args[0].artifact.observations.inheritedCleanup.sourceCase='preserve-two-acquired-field'}
Check-SeedCheckpointLoad 'owner-seed checkpoint consumer refuses a replayed Mount' {$args[0].artifact.observations.snapshots[0].nativeCastRequests=1}
Check-SeedCheckpointLoad 'owner-seed checkpoint consumer refuses a restored mounted start' {$v=New-KmcOwnerSeedCheckpointSaveFixture;$args[0].artifact.observations|Add-Member mountedStart $v.artifact.observations.mountedStart -Force} 'Only the owner-seed checkpoint producer'
Check-SeedCheckpointLoad 'owner-seed checkpoint consumer refuses the derived-Manual cold proof' {$args[0].artifact.observations|Add-Member ownerSeedDerivedCold ([pscustomobject]@{contract='owner-seed-derived-cold-v1'}) -Force} 'Only the owner-seed-derived consumer'
foreach($sourceFault in @('producer-run','producer-failed','producer-cold','producer-checkpoint','archive-hash','length','same-process','graph','generation','slots','arrangement','manual-pin','seed-pin')){Check ('owner-seed checkpoint cold source refuses '+$sourceFault) {$v=New-KmcOwnerSeedCheckpointLoadFixture;$p=New-KmcOwnerSeedCheckpointProducer $v;$po=$p.observations
 switch($sourceFault){'producer-run'{$p.runId='another-checkpoint'}'producer-failed'{$p.status='FAIL'}'producer-cold'{$po.cold=$true}'producer-checkpoint'{$po.checkpoint='multi-exploration-manual'}'archive-hash'{$po.nativeWrite.sha256=('e'*64)}'length'{$po.nativeWrite.length=1}'same-process'{$po.processId=$v.artifact.observations.processId}'graph'{$po.nativeWrite.archive.GameTimeTicks=1}'generation'{$po.mountedStart.owners[0].generation=9}'slots'{$po.mountedStart.slots=@($po.mountedStart.slots|Select-Object -First 3)}'arrangement'{$po.inheritedCleanup.arrangement.money=1}'manual-pin'{$po.ownerSeedCheckpointSource.manualSha256=('e'*64)}'seed-pin'{$po.ownerSeedCheckpointSource.seedSha256=('e'*64)}}
 Assert-KmcChunk7OwnerSeedCheckpointColdSource $v.request $p $v.artifact} $true}
Check 'owner-seed checkpoint cold source belongs only to its consumer' {$v=New-KmcOwnerSeedDerivedColdFixture;Assert-KmcChunk7OwnerSeedCheckpointColdSource $v.request (New-KmcOwnerSeedDerivedProducer $v) $v.artifact} $true 'belongs only'
# P2 combat checkpoints, generated (labelled synthetic) from the owned combat fixtures: the restored two-pair start
# (the exploration fixture's graph, owners, generations and four slots) replaces the native Mounts, the debt is the
# fixture's native primary expenditures, the target carries the owner-seed-derived basis, and the one genuine
# native Manual is Manual_304_KMC_C7_SEED_CHECKPOINT after the derived Manual_303 input.
function Get-KmcSeedRestoredGraph{(Copy-Value @($fixtures|Where-Object name -CEQ 'persistence-save-multi-exploration-quick')[0]).artifact.observations.nativeWrite.archive}
function New-KmcOwnerSeedCombatCheckpointSaveFixture([string]$Checkpoint){
 $v=Copy-Value @($fixtures|Where-Object name -CEQ ('persistence-save-'+$Checkpoint))[0];$r=$v.request;$o=$v.artifact.observations;$p=$r.ownedPairs;$explore=Get-KmcSeedRestoredGraph
 $old=$r.persistenceLoad.fileName;$oldLeaf=$o.ownedOutputAdmission.leaf;$leaf='Manual_304_KMC_C7_SEED_CHECKPOINT.zks'
 $s=New-KmcOwnerSeedCheckpointSelection $p 'owner-seed-checkpoint-save' $r.persistenceLoad.sha256 'synthetic-owner-seed-cold';$r.ownedPairs=$s
 $r.persistenceLoad.internalName='KMC_C7_SEED_WORLD';$r.persistenceLoad.fileName='Manual_303_KMC_C7_SEED_WORLD.zks'
 foreach($row in @($o.ownedOutputAdmission.nativeInventory)+@($o.ownedOutputCompletion.nativeInventory)){if($row.fileName-ceq$old){$row.fileName=$r.persistenceLoad.fileName;$row.nativeName=$r.persistenceLoad.internalName}elseif($row.fileName-ceq$oldLeaf){$row.fileName=$leaf;$row.nativeName='KMC_C7_SEED_CHECKPOINT'}}
 $o.ownedOutputAdmission.number=304;$o.ownedOutputAdmission.leaf=$leaf;foreach($auth in @($o.ownedOutputAdmission.authority,$o.ownedOutputCompletion.authority)){$auth.number=304;$auth.leaf=$leaf;$auth.nativeName='KMC_C7_SEED_CHECKPOINT'}
 $o.nativeWrite.path=$o.nativeWrite.path.Replace($oldLeaf,$leaf);$o.nativeWrite.nativeName='KMC_C7_SEED_CHECKPOINT'
 Set-KmcOwnerSeedDerivedLabels $o $s;$o.inputs=@();$o.PSObject.Properties.Remove('acquisition')
 $o|Add-Member mountedStart ([pscustomobject][ordered]@{contract='owner-seed-checkpoint-restored-start-v1';restoredFromColdLoad=$true;nativeMountInputs=0;newSlotBindings=0;archivePhase='all-pair-presentation-restored';schemaVersion=3;semanticRestores=4;presentationRestores=2;nativePostLoads=4;nativeCastRequests=0;snapshots=0
  profiles=@(0,1|ForEach-Object{[pscustomobject][ordered]@{riderId=$s.partyIds[$_*2];mountId=$s.partyIds[$_*2+1];profileId='medium-humanoid-horse-v1'}})
  owners=@($explore.Pairs|ForEach-Object{[pscustomobject][ordered]@{ownerId=(ConvertTo-KmcChunk7PairIdN $_.PairId);generation=$_.Generation;riderId=$_.Rider.Id;mountId=$_.Mount.Id}})
  slots=@($explore.Slots|ForEach-Object{[pscustomobject][ordered]@{actorId=$_.ActorId;index=$_.Index;kind=$_.Kind}});loadedEqualsProducerReadback=$true;producerProcessId=4242;processId=$o.processId;debtRetained=$true;loadedArchive=$explore}) -Force
 $o|Add-Member ownerSeedCheckpointSource ([pscustomobject][ordered]@{contract='owner-seed-checkpoint-source-v1';role='producer';ancestry='owner-seed-derived';acquisitionProof=$false;ordinaryProducerAncestry=$false;manualProducerRunId=$s.sourceRunId;manualProducerArtifactSha256=$s.sourceArtifactSha256;manualSha256=$s.sourceArchiveSha256;acceptedConsumerRunId=$s.checkpointSourceRunId;acceptedConsumerArtifactSha256=$s.checkpointSourceArtifactSha256;seedId=$s.seedId;seedSha256=$s.seedSha256;checkpoint=$Checkpoint
  candidates=[pscustomobject][ordered]@{contract='owner-seed-derived-candidate-identity-v1';sameCandidate=$true;crossCandidateAllowed=$true;producer=[pscustomobject][ordered]@{runId=$s.sourceRunId;commit=$r.commit;dllSha256=$r.dllSha256;dllMvid=$r.dllMvid};current=[pscustomobject][ordered]@{commit=$r.commit;dllSha256=$r.dllSha256;dllMvid=$r.dllMvid};consumerRunId=$s.checkpointSourceRunId;consumerArtifactSha256=$s.checkpointSourceArtifactSha256;manualSchema=3;currentWriterSchema=3}}) -Force
 $o.combatCheckpoint.source|Add-Member targetAuthorization ([pscustomobject][ordered]@{contract='chunk7-combat-target-authorization-v1';basis='owner-seed-derived-disposable-copy';ownerApproval='docs/CHUNK7-OWNER-DIRECTIVES-2026-10-16.md, Owner decisions on the combat slice (October 17, 2026): combat target approved';workingSha256=$null;seedSha256=$s.seedSha256;derivedManualSha256=$s.sourceArchiveSha256;producerRunId=$s.sourceRunId;producerArtifactSha256=$s.sourceArtifactSha256;consumerRunId=$s.checkpointSourceRunId;consumerArtifactSha256=$s.checkpointSourceArtifactSha256;area=$r.persistenceLoad.area;riderPosition=@(0.0,0.0,0.0);position=@(0.0,0.0,3.5);placementValidated=$true}) -Force
 if($Checkpoint-ceq'legacy-mounted-pending-tb-manual'){
  # Generated genuine current boundary: historical projection happens only
  # later in the guarded derivative, so this native write preserves live A.
  $o.mountedStart.owners[0].generation=7;$o.mountedStart.loadedArchive.Pairs[0].Generation=7
  foreach($snapshot in $o.snapshots){$snapshot.owners[0].ownerId=$o.mountedStart.owners[0].ownerId;$snapshot.owners[0].generation=7}
  Set-KmcOwnerSeedNativePreparationFixture $v $false
  $owner=$o.mountedStart.owners[0]
  $o.nativeWrite.archive.Pairs[0].PairId=$owner.ownerId;$o.nativeWrite.archive.Pairs[0].Generation=$owner.generation
  $o.nativeWrite.archive.Participations[0].OwnerId=$owner.ownerId;$o.nativeWrite.archive.Participations[0].Generation=$owner.generation;$o.nativeWrite.archive.Participations[0].MountedPairId=$owner.ownerId
  $o.nativeWrite.sourceSchema=3;$o.nativeWrite.nativeMetadata=Copy-Value $o.nativeWrite.archive;$o.nativeWrite.sha256='c'*64
  $o.PSObject.Properties.Remove('legacyWriterScope');$o|Add-Member nativeSourceContract 'owner-seed-current-pending-source-v1' -Force
 }
 $v.name='persistence-owner-seed-checkpoint-save-'+$Checkpoint;$v
}
function New-KmcOwnerSeedCombatCheckpointLoadFixture([string]$Checkpoint){
 $v=Copy-Value @($fixtures|Where-Object name -CEQ ('persistence-load-'+$Checkpoint))[0];$r=$v.request;$o=$v.artifact.observations
 $s=New-KmcOwnerSeedCheckpointSelection $r.ownedPairs 'owner-seed-checkpoint-load' ('7'*64) 'synthetic-owner-seed-checkpoint';$r.ownedPairs=$s;Set-KmcOwnerSeedDerivedLabels $o $s
 foreach($d in @($r.persistenceLoad,$o.loadedDescriptor)){$d.internalName='KMC_C7_SEED_CHECKPOINT';$d.fileName='Manual_304_KMC_C7_SEED_CHECKPOINT.zks'}
 if($Checkpoint-ceq'legacy-mounted-pending-tb-manual'){
  $s.expectedNativeWriteCount=1
  $source=New-KmcOwnerSeedCombatCheckpointSaveFixture $Checkpoint
  $r.persistenceLoad.sha256='d'*64;$o.loadedDescriptor.sha256=$r.persistenceLoad.sha256;$r|Add-Member schema2DerivationSha256 ('a'*64) -Force
  $o.schema2Migration.inputSha256=$r.persistenceLoad.sha256
  $o.schema2Migration|Add-Member syntheticMetadata $true -Force
  $o.schema2Migration|Add-Member nativeInputMetadata (ConvertTo-KmcOwnedSchema2Metadata $source.artifact.observations.nativeWrite.archive $r) -Force
  $o.schema2Migration|Add-Member derivationReceiptSha256 $r.schema2DerivationSha256 -Force
  $o.schema2Migration|Add-Member originalNativeArchiveSha256 $source.artifact.observations.nativeWrite.sha256 -Force
  foreach($row in @($o.ownedOutputAdmission.nativeInventory)+@($o.ownedOutputCompletion.nativeInventory)){if($row.fileName-ceq'Manual_43_KMC_C7_WORLD.zks'){$row.fileName=$r.persistenceLoad.fileName;$row.nativeName=$r.persistenceLoad.internalName}else{$row.fileName='Manual_305_KMC_C7_SCHEMA2.zks'}}
  $o.ownedOutputAdmission.number=305;$o.ownedOutputAdmission.leaf='Manual_305_KMC_C7_SCHEMA2.zks'
  foreach($auth in @($o.ownedOutputAdmission.authority,$o.ownedOutputCompletion.authority)){$auth.number=305;$auth.leaf='Manual_305_KMC_C7_SCHEMA2.zks'}
  $o.nativeWrite.path=$o.nativeWrite.path.Replace('Manual_44_KMC_C7_SCHEMA2.zks','Manual_305_KMC_C7_SCHEMA2.zks')
 }
 $v.name='persistence-owner-seed-checkpoint-load-'+$Checkpoint;$v
}
# The combat checkpoint producer receipt fields the runner compares: the restored start's owners and slots,
# the written archive the cold process restored, the producer's saved target and its debt proof.
function New-KmcOwnerSeedCombatCheckpointProducer($Cold){
 $p=New-KmcOwnerSeedCheckpointProducer $Cold;$po=$p.observations;$explore=Get-KmcSeedRestoredGraph;$c=$Cold.artifact.observations
 if($Cold.request.persistenceCase-ceq'legacy-mounted-pending-tb-manual'){
  $full=New-KmcOwnerSeedCombatCheckpointSaveFixture 'legacy-mounted-pending-tb-manual';$fullO=$full.artifact.observations
  $fullO.nativeWrite.sha256=$c.schema2Migration.originalNativeArchiveSha256;$fullO.nativeWrite.length=$Cold.request.persistenceLoad.length+100;$fullO.nativeWrite.archive=Copy-Value $c.loadedArchive
  $owner=$fullO.mountedStart.owners[0];$fullO.nativeWrite.archive.Pairs[0].PairId=$owner.ownerId;$fullO.nativeWrite.archive.Pairs[0].Generation=$owner.generation
  $fullO.nativeWrite.archive.Participations[0].OwnerId=$owner.ownerId;$fullO.nativeWrite.archive.Participations[0].Generation=$owner.generation;$fullO.nativeWrite.archive.Participations[0].MountedPairId=$owner.ownerId
  $fullO.nativeWrite.nativeMetadata=Copy-Value $fullO.nativeWrite.archive
  $fullO.ownerSeedCheckpointSource=$po.ownerSeedCheckpointSource;$fullO.inheritedCleanup=Copy-Value $c.inheritedFixture;$fullO.processId=4343;$p.observations=$fullO;return $p
 }
 $po.mountedStart.owners=@($explore.Pairs|ForEach-Object{[pscustomobject][ordered]@{ownerId=(ConvertTo-KmcChunk7PairIdN $_.PairId);generation=$_.Generation;riderId=$_.Rider.Id;mountId=$_.Mount.Id}})
 $po.mountedStart.slots=@($explore.Slots|ForEach-Object{[pscustomobject][ordered]@{actorId=$_.ActorId;index=$_.Index;kind=$_.Kind}})
 $po|Add-Member combatDebtProof $true;$po|Add-Member combatCheckpoint ([pscustomobject][ordered]@{targetId=$c.combatContinuation.targetId;targetGroupId=$c.combatContinuation.targetGroupId});$p
}
foreach($checkpointCase in @('multi-rt-debt-manual','multi-tb-debt-manual','zero-mounted-tb-retained-manual')){
 Check ('owner-seed combat checkpoint producer evidence accepted '+$checkpointCase) {$v=New-KmcOwnerSeedCombatCheckpointSaveFixture $checkpointCase;Invoke-OwnedReader $v;if((Get-KmcChunk7OwnedOutputLeaf $v.request $v.artifact.observations)-cne'Manual_304_KMC_C7_SEED_CHECKPOINT.zks'){throw 'Combat checkpoint output leaf differs.'}}
 Check ('owner-seed combat checkpoint consumer evidence accepted '+$checkpointCase) {Invoke-OwnedReader (New-KmcOwnerSeedCombatCheckpointLoadFixture $checkpointCase)}
 Check ('owner-seed combat checkpoint consumer matches its producer read-back '+$checkpointCase) {$v=New-KmcOwnerSeedCombatCheckpointLoadFixture $checkpointCase;Assert-KmcChunk7OwnerSeedCheckpointColdSource $v.request (New-KmcOwnerSeedCombatCheckpointProducer $v) $v.artifact}
 $saveFaults=[ordered]@{
  'a native Mount input on its restored start'=@({param($v)$plain=Copy-Value @($fixtures|Where-Object name -CEQ ('persistence-save-'+$checkpointCase))[0];$v.artifact.observations.inputs=$plain.artifact.observations.inputs},'native Mount input')
  'a missing restored start'=@({param($v)$v.artifact.observations.PSObject.Properties.Remove('mountedStart')},'restored mounted-start')
  'debt written instead of acted (no native primary)'=@({param($v)$cc=$v.artifact.observations.combatCheckpoint;$cc.source.inputs=@($cc.source.inputs|Where-Object{$null-eq$_.PSObject.Properties['outcome']})},'primary expenditure')
  'debt without its native cost callback'=@({param($v)$t=$v.artifact.observations.combatCheckpoint.trace;$t.events=@($t.events|Where-Object boundary -CNE 'cost-after');$i=0;foreach($e in $t.events){$i++;$e.sequence=$i}},'')
  'an acquired-ancestry output name'=@({param($v)$o=$v.artifact.observations;$o.nativeWrite.nativeName='KMC_C7_WORLD';$o.ownedOutputAdmission.leaf='Manual_304_KMC_C7_WORLD.zks'},'')
  'the derived Manual output name'=@({param($v)$o=$v.artifact.observations;$o.nativeWrite.nativeName='KMC_C7_SEED_WORLD';foreach($a in @($o.ownedOutputAdmission.authority,$o.ownedOutputCompletion.authority)){$a.nativeName='KMC_C7_SEED_WORLD'}},'')
  'a working-fixture target basis'=@({param($v)$t=$v.artifact.observations.combatCheckpoint.source.targetAuthorization;$t.basis='exact-working-fixture'},'')
  'an acquired target basis'=@({param($v)$t=$v.artifact.observations.combatCheckpoint.source.targetAuthorization;$t.basis='owned-acquired-field-copy'},'')
  'a target without the owner approval'=@({param($v)$v.artifact.observations.combatCheckpoint.source.targetAuthorization.ownerApproval=$null},'owner approval')
  'a missing target authorization'=@({param($v)$v.artifact.observations.combatCheckpoint.source.PSObject.Properties.Remove('targetAuthorization')},'target authorization')
  'a restored generation the checkpoint changed'=@({param($v)$v.artifact.observations.mountedStart.owners[1].generation=9},'')
  'an invented participation'=@({param($v)$a=$v.artifact.observations.nativeWrite.archive;$a.Participations=@($a.Participations[0])},'')
  'cross-ancestry (an acquired Manual input)'=@({param($v)$v.request.persistenceLoad.internalName='KMC_C7_WORLD';$v.request.persistenceLoad.fileName='Manual_303_KMC_C7_WORLD.zks'},'')
 }
 foreach($fault in $saveFaults.Keys){$faultBody=$saveFaults[$fault];Check ('owner-seed combat checkpoint producer refuses '+$fault+' '+$checkpointCase) {$v=New-KmcOwnerSeedCombatCheckpointSaveFixture $checkpointCase;&$faultBody[0] $v;Invoke-OwnedReader $v} $true $faultBody[1]}
 $loadFaults=[ordered]@{
  'an acquired-ancestry Manual input'={param($v)foreach($d in @($v.request.persistenceLoad,$v.artifact.observations.loadedDescriptor)){$d.internalName='KMC_C7_WORLD';$d.fileName='Manual_304_KMC_C7_WORLD.zks'}}
  'the derived Manual name as input'={param($v)foreach($d in @($v.request.persistenceLoad,$v.artifact.observations.loadedDescriptor)){$d.internalName='KMC_C7_SEED_WORLD';$d.fileName='Manual_304_KMC_C7_SEED_WORLD.zks'}}
  'a restored mounted start'={param($v)$v.artifact.observations|Add-Member mountedStart ([pscustomobject]@{contract='owner-seed-checkpoint-restored-start-v1'})}
  'a replayed input'={param($v)$v.artifact.observations.snapshots[0].nativeCastRequests=1}
 }
 foreach($fault in $loadFaults.Keys){$faultBody=$loadFaults[$fault];Check ('owner-seed combat checkpoint consumer refuses '+$fault+' '+$checkpointCase) {$v=New-KmcOwnerSeedCombatCheckpointLoadFixture $checkpointCase;&$faultBody $v;Invoke-OwnedReader $v} $true}
 # The zero-mounted checkpoint released its controls natively, so its cold source has no restored slots to compare.
 foreach($sourceFault in @('producer-checkpoint','graph','participation-generation','target','debt-proof','slots','same-process')|Where-Object{$_-cne'slots'-or$checkpointCase-cne'zero-mounted-tb-retained-manual'}){Check ('owner-seed combat checkpoint cold source refuses '+$sourceFault+' '+$checkpointCase) {$v=New-KmcOwnerSeedCombatCheckpointLoadFixture $checkpointCase;$p=New-KmcOwnerSeedCombatCheckpointProducer $v;$po=$p.observations
  switch($sourceFault){'producer-checkpoint'{$po.checkpoint='multi-exploration-quick'}'graph'{$po.nativeWrite.archive.GameTimeTicks=1}'participation-generation'{$po.mountedStart.owners[0].generation=9}'target'{$po.combatCheckpoint.targetId='abababab-1111-4111-8111-111111111111'}'debt-proof'{$po.combatDebtProof=$false}'slots'{$po.mountedStart.slots[0].index=99}'same-process'{$po.processId=$v.artifact.observations.processId}}
  Assert-KmcChunk7OwnerSeedCheckpointColdSource $v.request $p $v.artifact} $true}
}
Check 'acquired-ancestry combat checkpoint keeps KMC_C7_WORLD and native Mounts' {foreach($case in @('multi-rt-debt-manual','multi-tb-debt-manual','zero-mounted-tb-retained-manual')){$v=Copy-Value @($fixtures|Where-Object name -CEQ ('persistence-save-'+$case))[0];Invoke-OwnedReader $v;if((Get-KmcChunk7OwnedManualOutputName $v.request)-cne'KMC_C7_WORLD'-or(Get-KmcChunk7OwnedOutputLeaf $v.request $v.artifact.observations)-cne'Manual_43_KMC_C7_WORLD.zks'){throw 'Acquired-ancestry combat output name changed.'}}}
Check 'acquired-ancestry combat checkpoint refuses a restored mounted start' {$v=New-KmcOwnerSeedCombatCheckpointSaveFixture 'multi-tb-debt-manual';$plain=Copy-Value @($fixtures|Where-Object name -CEQ 'persistence-save-multi-tb-debt-manual')[0];$plain.artifact.observations|Add-Member mountedStart $v.artifact.observations.mountedStart;Invoke-OwnedReader $plain} $true 'Only the owner-seed checkpoint producer'
Check 'acquired-ancestry combat checkpoint refuses the owner-seed checkpoint name' {$v=Copy-Value @($fixtures|Where-Object name -CEQ 'persistence-save-multi-tb-debt-manual')[0];$o=$v.artifact.observations;$o.nativeWrite.nativeName='KMC_C7_SEED_CHECKPOINT';$o.ownedOutputAdmission.leaf='Manual_43_KMC_C7_SEED_CHECKPOINT.zks';Invoke-OwnedReader $v} $true
Check 'acquired-ancestry combat cold load refuses an owner-seed checkpoint Manual' {$v=Copy-Value @($fixtures|Where-Object name -CEQ 'persistence-load-multi-tb-debt-manual')[0];foreach($d in @($v.request.persistenceLoad,$v.artifact.observations.loadedDescriptor)){$d.internalName='KMC_C7_SEED_CHECKPOINT';$d.fileName='Manual_43_KMC_C7_SEED_CHECKPOINT.zks'};Invoke-OwnedReader $v} $true
Check 'owner-seed genuine current pending producer preserves exact live A custody' {$v=New-KmcOwnerSeedCombatCheckpointSaveFixture 'legacy-mounted-pending-tb-manual';Invoke-OwnedReader $v;$o=$v.artifact.observations;if($o.mountedStart.owners[0].ownerId-cne$o.nativeWrite.archive.Pairs[0].PairId-or$o.mountedStart.owners[0].generation-ne7-or$o.nativeWrite.archive.Pairs[0].Generation-ne7-or$o.nativeWrite.sourceSchema-ne3){throw 'Generated native source lost its exact current custody.'}}
Check 'owner-seed schema2 cold loader writes one separate current-format Manual' {$v=New-KmcOwnerSeedCombatCheckpointLoadFixture 'legacy-mounted-pending-tb-manual';Invoke-OwnedReader $v;if((Get-KmcChunk7OwnedOutputLeaf $v.request $v.artifact.observations)-cne'Manual_305_KMC_C7_SCHEMA2.zks'-or(Get-KmcChunk7OwnedWriteCount $v.request)-ne1){throw 'Schema2 cold/resave output or quota differs.'}}
Check 'owner-seed schema2 cold graph matches the prepared one-owner producer' {$v=New-KmcOwnerSeedCombatCheckpointLoadFixture 'legacy-mounted-pending-tb-manual';Assert-KmcChunk7OwnerSeedCheckpointColdSource $v.request (New-KmcOwnerSeedCombatCheckpointProducer $v) $v.artifact}
foreach($fault in @('missing-preparation','direct-service-label','wrong-Dismount-caster','missing-input','extra-input','not-retired','retained-obligation','unsettled-command','unsettled-preparation','busy-save','changed-survivor','lost-actor','actor-refund','prepared-actor','second-control','slot-index-string','owner-role-array','invented-activation','wrong-migrated-owner','wrong-migrated-generation')){
 Check ('owner-seed legacy preparation refuses '+$fault){$v=New-KmcOwnerSeedCombatCheckpointSaveFixture 'legacy-mounted-pending-tb-manual';$o=$v.artifact.observations;$p=$o.ownerSeedCheckpointPreparation
  switch($fault){
   'missing-preparation'{$o.PSObject.Properties.Remove('ownerSeedCheckpointPreparation')}
   'direct-service-label'{$p.input.label='direct-service-cleanup';$o.inputs[0].label=$p.input.label}
   'wrong-Dismount-caster'{$p.input.casterId=$v.request.ownedPairs.riderAId;$o.inputs[0].casterId=$p.input.casterId}
   'missing-input'{$o.inputs=@()}
   'extra-input'{$o.inputs=@($o.inputs)+@($o.inputs)}
   'not-retired'{$p.after.secondRiderOwnerAbsent=$false}
   'retained-obligation'{$p.after.secondPairCanRelease=$false}
   'unsettled-command'{$p.after.secondPairCommandsRetained=$true}
   'unsettled-preparation'{$p.after.unsettledPreparation=$true}
   'busy-save'{$p.after.saveBusy=$true}
   'changed-survivor'{$p.after.owners[0].generation++}
   'lost-actor'{$p.after.actors=@($p.after.actors|Select-Object -First 3)}
   'actor-refund'{$p.after.actors[0].native.Standard=99.0}
   'prepared-actor'{$p.after.actors[0].prepared=$true}
   'second-control'{$p.slotsAfter[0].ActorId=$v.request.ownedPairs.riderBId}
   'slot-index-string'{$p.slotsAfter[0].Index=[string]$p.slotsAfter[0].Index}
   'owner-role-array'{$p.before.owners[1].mountId=@($p.before.owners[1].mountId)}
   'invented-activation'{$o.nativeWrite.archive.Participations[0].State.Activation=[pscustomobject]@{Sequence=1}}
   'wrong-migrated-owner'{$old=$o.nativeWrite.archive.Pairs[0].PairId;$new='abcdefabcdef4def8defabcdefabcdef';$raw=$o.nativeWrite.archive|ConvertTo-Json -Depth 90;$o.nativeWrite.archive=$raw.Replace($old,$new)|ConvertFrom-Json}
   'wrong-migrated-generation'{$o.nativeWrite.archive.Pairs[0].Generation=1;$o.nativeWrite.archive.Participations[0].Generation=1}
  };Invoke-OwnedReader $v
 } $true
}
foreach($fault in @('missing-native-preparation','changed-surviving-owner','two-owners-in-legacy-input','resave-zero-quota','schema3-input-name','both-readbacks-use-live-owner')){
 Check ('owner-seed schema2 cold source refuses '+$fault){$v=New-KmcOwnerSeedCombatCheckpointLoadFixture 'legacy-mounted-pending-tb-manual';$p=New-KmcOwnerSeedCombatCheckpointProducer $v
  switch($fault){
   'missing-native-preparation'{$p.observations.PSObject.Properties.Remove('ownerSeedCheckpointPreparation')}
   'changed-surviving-owner'{$p.observations.mountedStart.owners[0].generation++}
   'two-owners-in-legacy-input'{$v.artifact.observations.loadedArchive.Pairs=@($v.artifact.observations.loadedArchive.Pairs)+@($p.observations.mountedStart.loadedArchive.Pairs[1])}
   'resave-zero-quota'{$v.request.ownedPairs.expectedNativeWriteCount=0}
   'schema3-input-name'{$v.request.persistenceLoad.internalName='KMC_C7_SCHEMA2';$v.request.persistenceLoad.fileName='Manual_305_KMC_C7_SCHEMA2.zks'}
   'both-readbacks-use-live-owner'{$old=$v.artifact.observations.loadedArchive.Pairs[0].PairId;$new=$p.observations.mountedStart.owners[0].ownerId;foreach($object in @($p.observations.nativeWrite,$v.artifact.observations)){$key=if($object.PSObject.Properties['loadedArchive']){'loadedArchive'}else{'archive'};$raw=$object.$key|ConvertTo-Json -Depth 90;$object.$key=$raw.Replace($old,$new)|ConvertFrom-Json}}
  };Assert-KmcChunk7OwnerSeedCheckpointColdSource $v.request $p $v.artifact
 } $true
}
Check 'native-shaped stale managed templates preserve exact A live controls and B Mount only' {$v=New-KmcOwnerSeedCombatCheckpointSaveFixture 'legacy-mounted-pending-tb-manual';$p=$v.artifact.observations.ownerSeedCheckpointPreparation;Invoke-OwnedReader $v;if($p.after.controls.ManagedHotbarSlotCount-ne4-or$p.slotsAfter.Count-ne2-or$p.controlFactsAfter.totalFacts-ne4){throw 'Generated stale-template fixture omitted the native distinction.'}}
foreach($fault in @('missing-facts','missing-row','duplicate-row','foreign-actor','wrong-kind-guid','extra-property','string-count','array-flag','inactive-fact','missing-data','wrong-data-blueprint','wrong-caster','wrong-lookup','aggregate-drift','mount-fact','charge-fact','B-primary-leak-compensated-by-A-loss')){
 Check ('native single-owner preparation control facts refuse '+$fault){$v=New-KmcOwnerSeedCombatCheckpointSaveFixture 'legacy-mounted-pending-tb-manual';$p=$v.artifact.observations.ownerSeedCheckpointPreparation;$facts=$p.controlFactsAfter;$rows=$facts.rows;$live=@($rows|Where-Object{$_.actorId-ceq$v.request.ownedPairs.riderAId-and$_.kind-eq4})[0];$fact=$live.facts[0]
  switch($fault){
   'missing-facts'{$p.PSObject.Properties.Remove('controlFactsAfter')}
   'missing-row'{$facts.rows=@($rows|Select-Object -Skip 1)}
   'duplicate-row'{$facts.rows[0]=Copy-Value $facts.rows[1]}
   'foreign-actor'{$facts.rows[0].actorId='aaaaaaaa-1111-4111-8111-111111111111'}
   'wrong-kind-guid'{$live.blueprintGuid=$rows[0].blueprintGuid}
   'extra-property'{$fact|Add-Member unrelated $true}
   'string-count'{$live.totalFacts='1'}
   'array-flag'{$fact.active=@($true)}
   'inactive-fact'{$fact.active=$false;$live.activeFacts=0;$facts.activeFacts=3}
   'missing-data'{$fact.dataPresent=$false}
   'wrong-data-blueprint'{$fact.dataBlueprintExact=$false}
   'wrong-caster'{$fact.casterExact=$false}
   'wrong-lookup'{$fact.nativeLookupExact=$false}
   'aggregate-drift'{$facts.totalFacts=3}
   'mount-fact'{$row=@($rows|Where-Object{$_.actorId-ceq$v.request.ownedPairs.mountAId-and$_.kind-eq1})[0];$row.totalFacts=1;$row.activeFacts=1;$row.facts=@(Copy-Value $fact);$facts.totalFacts++;$facts.activeFacts++}
   'charge-fact'{$row=@($rows|Where-Object{$_.actorId-ceq$v.request.ownedPairs.riderBId-and$_.kind-eq5})[0];$row.totalFacts=1;$row.activeFacts=1;$row.facts=@(Copy-Value $fact);$facts.totalFacts++;$facts.activeFacts++}
   'B-primary-leak-compensated-by-A-loss'{$row=@($rows|Where-Object{$_.actorId-ceq$v.request.ownedPairs.riderBId-and$_.kind-eq3})[0];$row.totalFacts=1;$row.activeFacts=1;$row.facts=@(Copy-Value $fact);$live.totalFacts=0;$live.activeFacts=0;$live.facts=@()}
  };Invoke-OwnedReader $v
 } $true
}
Check 'owner-seed schema1 source retains native A after genuine B retirement' {Invoke-OwnedReader (New-KmcOwnerSeedSchema1SourceFixture)}
foreach($phaseCase in @('owned-schema1-manual','legacy-mounted-pending-tb-manual')){
 Check ('owner-seed preparation four templates and native completion two bindings '+$phaseCase){$v=if($phaseCase-ceq'owned-schema1-manual'){New-KmcOwnerSeedSchema1SourceFixture}else{New-KmcOwnerSeedCombatCheckpointSaveFixture $phaseCase};$o=$v.artifact.observations;Invoke-OwnedReader $v;if($o.ownerSeedCheckpointPreparation.after.controls.ManagedHotbarSlotCount-ne4-or@($o.snapshots|Where-Object label -CEQ 'before-native-save')[0].controls.ManagedHotbarSlotCount-ne4-or@($o.snapshots|Where-Object label -CEQ 'native-write-complete')[0].controls.ManagedHotbarSlotCount-ne2){throw 'Generated source mixed preparation and completed-write control phases.'}}
 Check ('owner-seed native completion rejects stale preparation templates '+$phaseCase){$v=if($phaseCase-ceq'owned-schema1-manual'){New-KmcOwnerSeedSchema1SourceFixture}else{New-KmcOwnerSeedCombatCheckpointSaveFixture $phaseCase};@($v.artifact.observations.snapshots|Where-Object label -CEQ 'native-write-complete')[0].controls.ManagedHotbarSlotCount=4;Invoke-OwnedReader $v} $true
}
foreach($fault in @('missing-preparation','active-B-leak-compensated-by-A-loss','wrong-active-count','lost-live-slot','postsave-stale-template-count')){
 Check ('owner-seed schema1 stale-template source refuses '+$fault){$v=New-KmcOwnerSeedSchema1SourceFixture;$o=$v.artifact.observations;$p=$o.ownerSeedCheckpointPreparation
  switch($fault){
   'missing-preparation'{$o.PSObject.Properties.Remove('ownerSeedCheckpointPreparation')}
   'active-B-leak-compensated-by-A-loss'{$a=@($p.controlFactsAfter.rows|Where-Object{$_.actorId-ceq$v.request.ownedPairs.riderAId-and$_.kind-eq4})[0];$b=@($p.controlFactsAfter.rows|Where-Object{$_.actorId-ceq$v.request.ownedPairs.riderBId-and$_.kind-eq3})[0];$b.totalFacts=1;$b.activeFacts=1;$b.facts=@(Copy-Value $a.facts[0]);$a.totalFacts=0;$a.activeFacts=0;$a.facts=@()}
   'wrong-active-count'{$p.controlFactsAfter.activeFacts=3}
   'lost-live-slot'{$p.slotsAfter=@($p.slotsAfter|Select-Object -First 1)}
   'postsave-stale-template-count'{@($o.snapshots|Where-Object label -CEQ 'native-write-complete')[0].controls.ManagedHotbarSlotCount=4}
  };Invoke-OwnedReader $v
 } $true
}
Check 'owner-seed schema1 historical cold writes one native current-format Manual' {Invoke-OwnedReader (New-KmcOwnerSeedSchema1ColdFixture)}
Check 'owner-seed schema1 historical cold matches its exact original native producer' {$v=New-KmcOwnerSeedSchema1ColdFixture;Assert-KmcChunk7OwnerSeedCheckpointColdSource $v.request (New-KmcOwnerSeedSchema1Producer $v) $v.artifact}
foreach($checkpoint in @('owned-schema1-current-manual','owned-schema2-pending-current-manual')){
 Check ('current-format load-only native continuation '+$checkpoint){$v=New-KmcOwnerSeedCurrentFixture $checkpoint;Invoke-OwnedReader $v;if((Get-KmcChunk7OwnedWriteCount $v.request)-ne0){throw 'Current child acquired native save entitlement.'}}
 Check ('current-format exact historical resave parent '+$checkpoint){$v=New-KmcOwnerSeedCurrentFixture $checkpoint;Assert-KmcChunk7OwnerSeedCheckpointColdSource $v.request (New-KmcOwnerSeedCurrentParent $v) $v.artifact}
 foreach($fault in @('save-direction','write-quota','wrong-native-name','same-process','wrong-parent-case','wrong-parent-pin','wrong-input-schema','wrong-input-hash','wrong-input-length','missing-loaded-schema','historical-loaded-schema','string-loaded-schema','missing-actual-equality','producer-output','historical-migration','derivative-request','restoration-prepare','movement-or-continuation')){
  Check ('current-format child refuses '+$fault+' '+$checkpoint){$v=New-KmcOwnerSeedCurrentFixture $checkpoint;$r=$v.request;$o=$v.artifact.observations;$m=$o.currentFormatMigration
   switch($fault){
    'save-direction'{$r.ownedPairs.case='owner-seed-checkpoint-save';$r.ownedPairs.expectedNativeWriteCount=1}
    'write-quota'{$r.ownedPairs.expectedNativeWriteCount=1}
    'wrong-native-name'{$r.persistenceLoad.internalName='KMC_C7_SEED_CHECKPOINT';$r.persistenceLoad.fileName='Manual_304_KMC_C7_SEED_CHECKPOINT.zks'}
    'same-process'{$m.parentProcessId=$o.processId}
    'wrong-parent-case'{$m.parentCheckpoint='multi-exploration-quick'}
    'wrong-parent-pin'{$m.parentArtifactSha256='e'*64}
    'wrong-input-schema'{$m.inputSchema=2}
    'wrong-input-hash'{$m.inputSha256='e'*64}
    'wrong-input-length'{$m.inputLength++}
    'missing-loaded-schema'{$o.PSObject.Properties.Remove('loadedSourceSchema')}
    'historical-loaded-schema'{$o.loadedSourceSchema=2}
    'string-loaded-schema'{$o.loadedSourceSchema='3'}
    'missing-actual-equality'{$m.loadedEqualsParentNativeWrite=$false}
    'producer-output'{$o|Add-Member nativeWrite ([pscustomobject]@{sourceSchema=3})}
    'historical-migration'{$o|Add-Member schema2Migration ([pscustomobject]@{})}
    'derivative-request'{$r|Add-Member schema1DerivationSha256 ('e'*64)}
    'restoration-prepare'{$o.coldRestoration.events+=@([pscustomobject]@{kind='prepare-before';actorId=$r.ownedPairs.mountAId})}
    'movement-or-continuation'{if($checkpoint-ceq'owned-schema1-current-manual'){$o.movements[0].executorId=$r.ownedPairs.mountBId}else{$o.combatContinuation.kind='ZeroMountedRetained'}}
   };Invoke-OwnedReader $v
  } $true
 }
 foreach($fault in @('wrong-parent-process','wrong-parent-graph','wrong-parent-input-format','wrong-parent-checkpoint','wrong-parent-output-hash')){
  Check ('current-format parent refuses '+$fault+' '+$checkpoint){$v=New-KmcOwnerSeedCurrentFixture $checkpoint;$parent=New-KmcOwnerSeedCurrentParent $v;$p=$parent.observations
   switch($fault){'wrong-parent-process'{$p.processId++}'wrong-parent-graph'{$p.nativeWrite.archive.GameTimeTicks++}'wrong-parent-input-format'{if($checkpoint-ceq'owned-schema1-current-manual'){$p.schema1Migration.inputSchema=3}else{$p.loadedSourceSchema=3}}'wrong-parent-checkpoint'{$p.checkpoint='multi-exploration-quick'}'wrong-parent-output-hash'{$p.nativeWrite.sha256='e'*64}}
   Assert-KmcChunk7OwnerSeedCheckpointColdSource $v.request $parent $v.artifact
  } $true
 }
}
foreach($case in @('owned-schema1-manual','owned-schema1-resave-manual','owned-schema1-current-manual','owned-schema2-pending-current-manual')){Check ('owner-seed migration exact directional selection '+$case){$v=if($case-ceq'owned-schema1-manual'){New-KmcOwnerSeedSchema1SourceFixture}elseif($case-ceq'owned-schema1-resave-manual'){New-KmcOwnerSeedSchema1ColdFixture}else{New-KmcOwnerSeedCurrentFixture $case};$v.request.ownedPairs.case=if($v.request.ownedPairs.case-ceq'owner-seed-checkpoint-save'){'owner-seed-checkpoint-load'}else{'owner-seed-checkpoint-save'};Assert-KmcChunk7OwnedPairsSelection $v.request} $true}
# Compose each new artifact reader with the actual external result/game-result
# subscenario validator, including their shared closed runtime-row inventory.
foreach($leaf in @('Test-RuntimeGameResult.ps1','Test-RuntimeResult.ps1')){
 $ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $ReaderDirectory $leaf),[ref]$null,[ref]$null)
 $method=@($ast.FindAll({param($node)$node-is[Management.Automation.Language.FunctionDefinitionAst]-and$node.Name-ceq'Assert-SubscenarioResults'},$true));if($method.Count-ne1){throw 'Exact external runtime row validator is absent or repeated.'};Invoke-Expression $method[0].Extent.Text
 foreach($case in @('owned-schema1-current-manual','owned-schema2-pending-current-manual')){
  $runtime=[pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';subscenarioTotal=3;subscenarioPassCount=3;subscenarioFailCount=0;assertionPassCount=3;assertionFailCount=0;subscenarioResults=@(@('chunk7-owned-two-pair-campaign','C7-persistence-current-migration-native-load','C7-persistence-current-migration-continuation')|ForEach-Object{[pscustomobject]@{name=$_;status='PASS';assertionPassCount=1;assertionFailCount=0;errors=@()}})}
  Check ('current migration artifact and external runtime rows '+$case+' '+$leaf){Invoke-OwnedReader (New-KmcOwnerSeedCurrentFixture $case);Assert-SubscenarioResults $runtime}
  Check ('current migration external runtime rejects unknown row '+$case+' '+$leaf){$bad=Copy-Value $runtime;$bad.subscenarioResults[2].name+='-unknown';Assert-SubscenarioResults $bad} $true
  Check ('current migration external runtime rejects duplicate row '+$case+' '+$leaf){$bad=Copy-Value $runtime;$bad.subscenarioResults[2].name=$bad.subscenarioResults[1].name;Assert-SubscenarioResults $bad} $true
 }
}
$clock.Stop();[void][IO.Directory]::CreateDirectory($OutputDirectory);$passed=@($results|Where-Object pass).Count;$failed=$results.Count-$passed
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'receipt.json'),([pscustomobject]@{passed=$passed;failed=$failed;elapsedSeconds=$clock.Elapsed.TotalSeconds;nativeInvoked=$false;archiveRead=$false;results=@($results)}|ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false))
Write-Output ('Owned two-pair reader: '+$passed+' PASS / '+$failed+' FAIL');if($failed){exit 1}
