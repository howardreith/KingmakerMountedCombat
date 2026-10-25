param([string]$ReaderDirectory,[string]$FixturePath,[string]$OutputDirectory,[string]$RepositoryRoot,[string]$MigrationFixturePath)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$repo=if($RepositoryRoot){[IO.Path]::GetFullPath($RepositoryRoot)}else{[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$ReaderDirectory){$ReaderDirectory=Join-Path $repo 'scripts/runtime'}
if(-not$FixturePath){$FixturePath=Join-Path $repo 'tests/Chunk7Diagnostics/OwnedPairsEvidenceFixtures.json'}
. (Join-Path $ReaderDirectory 'RuntimeHarness.Common.ps1')
if(-not$OutputDirectory){$OutputDirectory=Join-Path (Get-KmcLabRoot) ('analysis-cache/chunk7-owned-readers-'+[Guid]::NewGuid().ToString('N'))}
$clock=[Diagnostics.Stopwatch]::StartNew();$results=[Collections.Generic.List[object]]::new()
$fixtures=(Get-Content -LiteralPath $FixturePath -Raw|ConvertFrom-Json).fixtures
# The stored RT Charge fixture predates rider A's observed equipped-weapon record (7E X2); add the melee shape here.
function New-KmcChargeWeaponState([long]$HandSet=0,[string]$Blueprint='35dfad6517f401145af54111be04d6cf'){[pscustomobject][ordered]@{handSet=$HandSet;primaryItem=4711;primaryBlueprint=$Blueprint;category='Longsword';melee=$true;ranged=$false;firstWeaponIsPrimary=$true}}
function New-KmcTestChargePlacement($Position){$g=@($Position|ForEach-Object{[double]$_});$m=@($g[0],$g[1],($g[2]-8.0));[pscustomobject][ordered]@{contract='chunk7-rt-charge-target-placement-v2';mountPosition=$m;riderPosition=@($g[0],($g[1]+1.4),($g[2]-8.0));mountCorpulence=0.9;minimumChargeRange=4.65;maximumChargeRange=30.48;units=@([pscustomobject][ordered]@{id='far-unit';position=@(($g[0]+10.0),$g[1],$g[2]);corpulence=0.5});candidates=@([pscustomobject][ordered]@{angle=0.0;distance=8.0;requested=@($g);walkable=$true;goal=@($g);traceEnd=@($g);traceExact=$true;laneBlocker=$null;eligible=$true});selected=0;position=@($g)}}
foreach($fx in @($fixtures|Where-Object name -CEQ 'realtime-charge')){$fx.artifact.observations|Add-Member chargeWeapon ([pscustomobject][ordered]@{before=(New-KmcChargeWeaponState);after=(New-KmcChargeWeaponState)}) -Force
 $fx.artifact.observations|Add-Member chargeTargetPlacement (New-KmcTestChargePlacement $(if($null-ne$fx.artifact.observations.PSObject.Properties['targetAuthorization']){$fx.artifact.observations.targetAuthorization.position}else{@(0.0,0.0,8.0)})) -Force}
function Copy-Value($v){$v|ConvertTo-Json -Depth 90|ConvertFrom-Json}
. (Join-Path $repo 'tests/Chunk7Diagnostics/OwnerSeedCheckpointPreparationFixtures.ps1')
if(-not$MigrationFixturePath){$MigrationFixturePath=Join-Path $repo 'tests/Chunk7Diagnostics/OwnerSeedMigrationFixtures.ps1'}
. $MigrationFixturePath
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
  $o.door=[pscustomobject][ordered]@{applicable=$false;reason='No open StandardDoor with an active-when-closed navmesh cut lies outside both pair interaction radii within the bounded scan radius.';candidatesScanned=0;radius=28.0}
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
# Current not-applicable shape: the deciding rows, taken where each pair's movement ended. Two
# doors: a clickable closed door too close to both mounts, and a far door behind a restriction.
function Set-ScannedNotApplicable($v){
 $o=$v.artifact.observations
 $mounts=@(for($i=0;$i-lt2;$i++){[pscustomobject][ordered]@{id=$o.movements[$i].executorId;position=[pscustomobject]@{x=[double]$o.movements[$i].end.x;y=[double]$o.movements[$i].end.y;z=[double]$o.movements[$i].end.z}}})
 function DoorRow($name,$dx,$open,$restrictions){
  $p=[pscustomobject]@{x=[double]$mounts[0].position.x+$dx;y=0.0;z=[double]$mounts[0].position.z}
  $d=@($mounts|ForEach-Object{[Math]::Sqrt([Math]::Pow([double]$_.position.x-$p.x,2)+[Math]::Pow([double]$_.position.z-$p.z,2))})
  $row=[pscustomobject][ordered]@{name=$name;position=$p;active=$true;open=$open;disableCutWhenOpen=$true;cutPresent=$true;proximityRadius=4.0;mountDistances=@([double]$d[0],[double]$d[1]);
   interactable=$true;availableInteractions=$true;revealed=$true;hasVisibleTrap=$false;trapActive=$false;alreadyUnlocked=$false;restrictionCount=$restrictions;hasDialog=$false;
   dataPresent=$true;viewTriggerCount=0;viewTriggerActions=$false;eligible=$false}
  $row.eligible=(Test-KmcChunk7DoorGate $row)-and($open-or(Test-KmcChunk7DoorClosedClickable $row))-and@($d|Where-Object{$_-le4.5-or$_-gt28.0}).Count-eq0
  $row
 }
 $rows=@((DoorRow 'close door' 1.0 $false 0),(DoorRow 'restricted door' 30.0 $false 1))
 $rows=@($rows|Sort-Object {[double]$_.mountDistances[0]+[double]$_.mountDistances[1]})
 $o.door=[pscustomobject][ordered]@{applicable=$false;reason='No open or player-clickable closed StandardDoor lies within bounds.';candidatesScanned=2;radius=28.0;mounts=$mounts;truncated=$false;rows=$rows}
}
Check 'owner-seed not-applicable scan distinguishes a too-close door from no door' {$v=New-KmcOwnerSeedWorldFixture $false;Set-ScannedNotApplicable $v;Assert-KmcChunk7WorldEvidence $v.request $v.artifact 'PASS'}
Check-OwnerSeed 'not-applicable scan with a qualifying door refused' {Set-ScannedNotApplicable $args[0];$r=@($args[0].artifact.observations.door.rows|Where-Object name -CEQ 'restricted door')[0];$r.restrictionCount=0;$r.position.x=[double]$args[0].artifact.observations.door.mounts[0].position.x+12.0;$m=$args[0].artifact.observations.door.mounts;$r.mountDistances=@(@($m|ForEach-Object{[Math]::Sqrt([Math]::Pow([double]$_.position.x-$r.position.x,2)+[Math]::Pow([double]$_.position.z-$r.position.z,2))}));$r.eligible=$true;$args[0].artifact.observations.door.rows=@($args[0].artifact.observations.door.rows|Sort-Object {[double]$_.mountDistances[0]+[double]$_.mountDistances[1]})} $false 'qualifying door'
Check-OwnerSeed 'not-applicable scan with an in-range static-cut door refused' {Set-ScannedNotApplicable $args[0];$r=@($args[0].artifact.observations.door.rows|Where-Object name -CEQ 'restricted door')[0];$r.restrictionCount=0;$r.disableCutWhenOpen=$false;$r.position.x=[double]$args[0].artifact.observations.door.mounts[0].position.x+12.0;$m=$args[0].artifact.observations.door.mounts;$r.mountDistances=@(@($m|ForEach-Object{[Math]::Sqrt([Math]::Pow([double]$_.position.x-$r.position.x,2)+[Math]::Pow([double]$_.position.z-$r.position.z,2))}));$r.eligible=$true;$args[0].artifact.observations.door.rows=@($args[0].artifact.observations.door.rows|Sort-Object {[double]$_.mountDistances[0]+[double]$_.mountDistances[1]})} $false 'qualifying door'
Check-OwnerSeed 'not-applicable scan claiming an in-range static-cut door ineligible refused' {Set-ScannedNotApplicable $args[0];$r=@($args[0].artifact.observations.door.rows|Where-Object name -CEQ 'restricted door')[0];$r.restrictionCount=0;$r.disableCutWhenOpen=$false;$r.position.x=[double]$args[0].artifact.observations.door.mounts[0].position.x+12.0;$m=$args[0].artifact.observations.door.mounts;$r.mountDistances=@(@($m|ForEach-Object{[Math]::Sqrt([Math]::Pow([double]$_.position.x-$r.position.x,2)+[Math]::Pow([double]$_.position.z-$r.position.z,2))}));$r.eligible=$false;$args[0].artifact.observations.door.rows=@($args[0].artifact.observations.door.rows|Sort-Object {[double]$_.mountDistances[0]+[double]$_.mountDistances[1]})} $false
Check-OwnerSeed 'not-applicable scan hiding a restriction gate result refused' {Set-ScannedNotApplicable $args[0];@($args[0].artifact.observations.door.rows|Where-Object name -CEQ 'close door')[0].eligible=$true} $false
Check-OwnerSeed 'not-applicable scan row without clickability flags refused' {Set-ScannedNotApplicable $args[0];$args[0].artifact.observations.door.rows[0].PSObject.Properties.Remove('revealed')} $false
Check-OwnerSeed 'not-applicable scan row without trigger flags refused' {Set-ScannedNotApplicable $args[0];$args[0].artifact.observations.door.rows[0].PSObject.Properties.Remove('viewTriggerActions')} $false
Check-OwnerSeed 'not-applicable scan missing a row refused' {Set-ScannedNotApplicable $args[0];$args[0].artifact.observations.door.rows=@($args[0].artifact.observations.door.rows[0])} $false
Check-OwnerSeed 'not-applicable scan distances off mount geometry refused' {Set-ScannedNotApplicable $args[0];$args[0].artifact.observations.door.rows[0].mountDistances[0]+=1.0} $false
Check-OwnerSeed 'not-applicable scan away from the movement end refused' {Set-ScannedNotApplicable $args[0];$d=$args[0].artifact.observations.door;foreach($r in $d.rows){$r.position.x+=2.0};$d.mounts[0].position.x+=2.0;$d.mounts[1].position.x+=2.0} $false 'movement ended'
Check-OwnerSeed 'not-applicable scan naming another mount refused' {Set-ScannedNotApplicable $args[0];$d=$args[0].artifact.observations.door;$t=$d.mounts[0].id;$d.mounts[0].id=$d.mounts[1].id;$d.mounts[1].id=$t} $false 'another mount'
Check-OwnerSeed 'not-applicable scan without mounts refused' {Set-ScannedNotApplicable $args[0];$args[0].artifact.observations.door.PSObject.Properties.Remove('mounts')} $false
Check-OwnerSeed 'not-applicable scan truncation flag inconsistent refused' {Set-ScannedNotApplicable $args[0];$args[0].artifact.observations.door.truncated=$true} $false
Check-OwnerSeed 'legacy not-applicable shape with scanned candidates refused' {$args[0].artifact.observations.door.candidatesScanned=3} $false 'requires its scan rows'
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
 # Native life (7E X1) has no R1 Dismount, so no first-action Mount either.
 if(-not$rt-and$Base-cne'native-life-cleanup'-and$Base-cne'realtime-charge'){Add-KmcSeedFirstActionMount $v $baseMountA ($Base-ceq'combat-mount-first')}
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
# 7E X1: the existing native life case on the same restored mounted start.
# 7E row 57 fold: the healed rider's native prone stand-up (owner-seed incapacitation case only).
$script:proneHealer='5dd7bd1d-ae4c-4df7-9687-50fceb027344'
function Add-KmcSeedProne($v){$o=$v.artifact.observations;$l=$o.nativeLife;$ids=@($o.actorIds);$t=$o.targetId;$h=$script:proneHealer;$d=[long]$l.damageRound
 foreach($row in @($l.unconsciousControl.candidates)){$row|Add-Member healer ($row.actorId-ceq$h) -Force}
 # M1 regression: the trace keeps recording fold rounds; the still-unconscious control prepares again in N+2.
 $tr=$o.allocationTrace;$max=[long](@($tr.events)|Measure-Object -Property sequence -Maximum).Maximum
 $l.unconsciousControl|Add-Member traceSequenceAtGrantAfter $max -Force
 $later=Copy-Value @($tr.events|Where-Object{$_.boundary-ceq'prepare-before'})[0];$later.sequence=$max+1;$later.round=$d+2;$later.state.actor=$l.unconsciousControl.actorId;$tr.events=@($tr.events)+@($later)
 $life={param([bool]$c,[long]$dmg)[pscustomobject][ordered]@{actorId=$ids[2];life=$(if($c){'Conscious'}else{'Unconscious'});conscious=$c;dead=$false;damage=$dmg;hp=97;constitution=16;temporaryHp=0;mainCharacter=$false;essential=$false;immortal=$false;allowDyingCondition=$true}}
 $rider={param([string]$label,[long]$round,[bool]$c,[long]$dmg,[bool]$pr)[pscustomobject][ordered]@{label=$label;round=$round;frame=500;life=(&$life $c $dmg);prone=$pr;proneRequested=$pr;position=@(0.0,0.0,1.5);targetDistance=1.8;commandsEmpty=$true;engagedByTarget=$true;remembersTarget=$true;pairState='Unmounted';registryActive=$false;pairCommandA=$false;pairCommandB=$false}}
 $tgt={param([string]$label,[long]$round,[long]$reactions)[pscustomobject][ordered]@{label=$label;id=$t;round=$round;frame=500;reactions=$reactions;reactionsPerRound=1;canAttackOfOpportunity=$true;canActInCombat=$true;waitingInitiative=$false;initiativeCooldown=0.0;surprised=$false;canAct=$true;threatHand=$true;riderRemembersTarget=$true;mountRemembersTarget=$true;standard=0.0;move=0.0;swift=0.0;prone=$false;proneRequested=$false;conscious=$true;commandsEmpty=$true;engagesRider=$false;engagesMount=$false;engagedUnits=@($ids[2]);position=@(0.0,0.0,3.3);mountDistance=4.0;corpulence=0.5;mountCorpulence=0.5;engagesSubject=$true;subjectRemembersTarget=$true}}
 $w={param([string]$label,[long]$round,[long]$ticks,[double]$move)$x=Copy-Value $o.snapshots[0];$x.label=$label;$x.round=$round;$x.gameTicks=$ticks;foreach($a in $x.actors){$a.standard=0.0;$a.move=0.0;$a.swift=0.0;if($a.id-ceq$ids[2]){$a.move=$move}};$x}
 $ability=[pscustomobject][ordered]@{identity=9202;blueprint='5590652e1c2225c4ca30c4a699ab3649';caster=$h;sourceItem=9201;sourceItemBlueprint='cd635d5720937b044a354dba17abad8d'}
 $item=[pscustomobject][ordered]@{item=9201;blueprint='cd635d5720937b044a354dba17abad8d';count=4;charges=1;exactSlot=$true};$after=Copy-Value $item;$after.count=3
 $attempt=[pscustomobject][ordered]@{round=($d+2);riderBefore=(&$rider 'heal-before' ($d+2) $false 99 $true);targetBefore=(&$tgt 'heal-before' ($d+2) 1);healerEngaged=$false;before=(&$w 'prone-heal-before' ($d+2) 2000000000 0.0);ruleCountBefore=0;traceSequenceBefore=900
  itemsBefore=$item;castEventsBefore=0;ability=$ability;target=$ids[2];selected=$true;canTarget=$true;clicked=$true;admittedShellCount=1;shell=[pscustomobject]@{identity=9203};shellIdentity=9203
  itemsAfter=$after;castEvents=@([pscustomobject]@{kind='item-spend-after';actor=$h;identity=9201;result=$true;count=3},[pscustomobject]@{kind='heal';actor=$h;identity=9204;target=$ids[2];value=6});terminal=[pscustomobject]@{identity=9203;finished=$true}
  riderAfter=(&$rider 'heal-after' ($d+2) $true 93 $true);targetAfter=(&$tgt 'heal-after' ($d+2) 1);after=(&$w 'prone-heal-after' ($d+2) 2015000000 0.0);rules=@();traceSequenceAfter=910;riderConsciousAfter=$true;declaredLeaseCommands=1}
 $aoo=[pscustomobject][ordered]@{sequence=1;kind='attack';identity=4343;frame=600;round=($d+2);actor=$t;target=$ids[2];opportunity=$true;fullAttack=$false;weapon='35dfad6517f401145af54111be04d6cf';ranged=$false;natural=$true;actorPosition=@(0.0,0.0,3.3);targetPosition=@(0.0,0.0,1.5)}
 $stand=[pscustomobject][ordered]@{round=($d+2);rider=(&$rider 'stand-precondition' ($d+2) $true 93 $true);gates=(&$tgt 'stand-precondition' ($d+2) 1);proposals=@();destination=@(0.0,0.0,-0.5)
  before=(&$w 'prone-stand-before' ($d+2) 2100000000 0.0);ruleCountBefore=0;traceSequenceBefore=920;fiveFootStep=$false;singleActionMove=$false;clicked=$true;cursorCycles=0;command=777;commandCreated=$true
  after=(&$w 'prone-stand-after' ($d+2) 2120000000 3.0);riderAfter=(&$rider 'stand-after' ($d+2) $true 95 $false);targetAfter=(&$tgt 'stand-after' ($d+2) 0);rules=@($aoo);traceSequenceAfter=930;displacement=0.0;commandResult='Interrupt';reIncapacitatedByAttack=$false}
 $o|Add-Member proneStandUp ([pscustomobject][ordered]@{contract='owner-seed-prone-stand-up-v1';riderId=$ids[2];healAttemptsAllowed=2
  healerCandidates=@([pscustomobject][ordered]@{actorId=$h;pet=$false;conscious=$true;inGame=$true;controllable=$true;needsUseMagicDevice=$false;commandsEmpty=$true;inCombat=$false;controlStaticallyIneligible=$false;eligible=$true});healerAtFold=[pscustomobject]@{inCombat=$true;conscious=$true;canAct=$true}
  healerId=$h;healerScroll=[pscustomobject][ordered]@{casterId=$h;scrollUnits=4;scrollBlueprint='cd635d5720937b044a354dba17abad8d';scrollAbility=$ability;scroll=[pscustomobject]@{ownedItemReleased=$true;disposed=$true};originalSlots=[pscustomobject]@{before=@()};castTrace=[pscustomobject]@{dropped=0;faults=0}}
  healAttempts=@($attempt);firstRound=($d+2);stage=15;healedRound=($d+2);standUp=$stand;rules=[pscustomobject][ordered]@{dropped=0;errors=@();events=@($aoo)};complete=$true}) -Force;$v}
$script:baseSeedDerived=${function:New-KmcOwnerSeedDerivedCombatFixture}
function New-KmcOwnerSeedDerivedCombatFixture([string]$Base){$v=& $script:baseSeedDerived $Base;if($Base-ceq'native-life-cleanup'){[void](Add-KmcSeedProne $v)};$v}
function Get-Prone($v){$v.artifact.observations.proneStandUp}
function Check-SeedProne([string]$Name,[scriptblock]$Mutation,[string]$Pattern){$v=New-KmcOwnerSeedDerivedCombatFixture 'native-life-cleanup';&$Mutation $v;Check ('owner-seed prone stand-up refuses '+$Name) {Invoke-OwnedReader $v} $true $Pattern}
Check 'owner-seed prone stand-up accepts a re-incapacitating attack of opportunity' {$v=New-KmcOwnerSeedDerivedCombatFixture 'native-life-cleanup';$s=(Get-Prone $v).standUp;$s.reIncapacitatedByAttack=$true;$s.riderAfter.prone=$true;$s.riderAfter.life.conscious=$false;$s.riderAfter.life.damage=98;Invoke-OwnedReader $v}
Check 'owner-seed prone stand-up accepts a second native heal attempt' {$v=New-KmcOwnerSeedDerivedCombatFixture 'native-life-cleanup';$P=Get-Prone $v;$a1=Copy-Value $P.healAttempts[0];$a1.round=[long]$a1.round;$a1.riderConsciousAfter=$false;$a1.riderAfter.life.conscious=$false;$a1.riderAfter.life.damage=98;$a1.castEvents=@($a1.castEvents|Where-Object kind -CNE 'heal')
 $a2=$P.healAttempts[0];$a2.round=[long]$a2.round+1;$a2.declaredLeaseCommands=2;$a2.itemsBefore.count=3;$a2.itemsAfter.count=2;$P.healAttempts=@($a1,$a2);$P.healedRound=$a2.round;$P.standUp.round=$a2.round;$P.standUp.rules[0].round=$a2.round;$P.rules.events[0].round=$a2.round;Invoke-OwnedReader $v}
Check-SeedProne 'a heal attempt that was not a declared lease command' {(Get-Prone $args[0]).healAttempts[0].PSObject.Properties.Remove('declaredLeaseCommands')} 'next sequential declared lease command'
Check-SeedProne 'a heal attempt out of declaration order' {(Get-Prone $args[0]).healAttempts[0].declaredLeaseCommands=2} 'next sequential declared lease command'
Check-SeedProne 'a control without its grant trace bound' {$args[0].artifact.observations.nativeLife.unconsciousControl.PSObject.Properties.Remove('traceSequenceAtGrantAfter')} 'grant trace bound|grants differ'
Check-SeedProne 'a healer whose recorded facts contradict its eligibility' {(Get-Prone $args[0]).healerCandidates[0].inCombat=$true} 'differs from its recorded native facts'
Check-SeedProne 'a healer that was not a combatant at the fold' {(Get-Prone $args[0]).healerAtFold.inCombat=$false} 'conscious acting combatant'
Check-SeedProne 'a missing fold' {$args[0].artifact.observations.PSObject.Properties.Remove('proneStandUp')} 'lacks its prone stand-up'
Check-SeedProne 'a healer not first eligible' {$P=Get-Prone $args[0];$P.healerCandidates=@([pscustomobject][ordered]@{actorId='7e5cf212-d860-45d5-ac9e-9dce3c5b0350';pet=$false;conscious=$true;inGame=$true;controllable=$true;needsUseMagicDevice=$false;commandsEmpty=$true;inCombat=$false;controlStaticallyIneligible=$false;eligible=$true})+@($P.healerCandidates)} 'first eligible'
Check-SeedProne 'a healer needing Use Magic Device' {(Get-Prone $args[0]).healerCandidates[0].needsUseMagicDevice=$true} 'differs from its recorded native facts'
Check-SeedProne 'candidates not ordered control-ineligible first' {$P=Get-Prone $args[0];$x=Copy-Value $P.healerCandidates[0];$x.actorId='7e5cf212-d860-45d5-ac9e-9dce3c5b0350';$x.controlStaticallyIneligible=$true;$x.eligible=$false;$x.inGame=$false;$P.healerCandidates=@($P.healerCandidates)+@($x)} 'ordered control-ineligible first'
Check-SeedProne 'the healer used as the unconscious control' {$c=$args[0].artifact.observations.nativeLife.unconsciousControl;$c.candidates[0].healer=$true} 'mark the prone healer|ineligible|differs'
Check-SeedProne 'a heal in round N+1' {(Get-Prone $args[0]).healAttempts[0].round=[long]$args[0].artifact.observations.nativeLife.damageRound+1} 'before round N\+2'
Check-SeedProne 'three heal attempts' {$P=Get-Prone $args[0];$P.healAttempts=@($P.healAttempts[0],$P.healAttempts[0],$P.healAttempts[0])} 'one or two native attempts'
Check-SeedProne 'a heal on another unit' {(Get-Prone $args[0]).healAttempts[0].target=$args[0].request.ownedPairs.riderAId} 'one native scroll click by the healer on rider B'
Check-SeedProne 'a heal with two spends' {$a=(Get-Prone $args[0]).healAttempts[0];$a.castEvents=@($a.castEvents)+@(Copy-Value $a.castEvents[0])} 'spend the exact stack exactly once'
Check-SeedProne 'a restoring heal with no native heal' {$a=(Get-Prone $args[0]).healAttempts[0];$a.castEvents=@($a.castEvents|Where-Object kind -CNE 'heal')} 'no native heal'
Check-SeedProne 'a heal attempt drawing a foreign attack' {$a=(Get-Prone $args[0]).healAttempts[0];$e=Copy-Value (Get-Prone $args[0]).standUp.rules[0];$a.rules=@($e)} 'attack other than'
Check-SeedProne 'a non-prone rider before the stand-up' {(Get-Prone $args[0]).standUp.rider.prone=$false} 'conscious, prone, threatened'
Check-SeedProne 'a re-paired rider before the stand-up' {(Get-Prone $args[0]).standUp.rider.pairState='Mounted'} 'conscious, prone, threatened'
Check-SeedProne 'a target without its attack of opportunity' {(Get-Prone $args[0]).standUp.gates.reactions=0} 'one available native attack of opportunity'
Check-SeedProne 'a target not threatening rider B' {(Get-Prone $args[0]).standUp.gates.engagesSubject=$false} 'one available native attack of opportunity'
Check-SeedProne 'no attack of opportunity on the stand-up' {$P=Get-Prone $args[0];$P.standUp.rules=@();$P.rules.events=@()} 'exactly one attack of opportunity'
Check-SeedProne 'two attacks on the stand-up' {$P=Get-Prone $args[0];$P.standUp.rules=@($P.standUp.rules[0],$P.standUp.rules[0])} 'exactly one attack of opportunity'
Check-SeedProne 'an attack on another unit' {(Get-Prone $args[0]).standUp.rules[0].target=$args[0].request.ownedPairs.riderAId} 'exactly one attack of opportunity'
Check-SeedProne 'a refunded target reaction' {(Get-Prone $args[0]).standUp.targetAfter.reactions=1} 'drop to zero'
Check-SeedProne 'a rider still prone after a non-incapacitating attack' {(Get-Prone $args[0]).standUp.riderAfter.prone=$true} 'did not stand up natively'
Check-SeedProne 'a rider healed by the attack' {(Get-Prone $args[0]).standUp.riderAfter.life.damage=80} 'did not stand up natively'
Check-SeedProne 'a stand-up that spent no Move' {@((Get-Prone $args[0]).standUp.after.actors|Where-Object id -CEQ $args[0].request.ownedPairs.riderBId)[0].move=0.0} 'spend rider B''s Move'
Check-SeedProne 'a stand-up that spent a Standard' {@((Get-Prone $args[0]).standUp.after.actors|Where-Object id -CEQ $args[0].request.ownedPairs.riderBId)[0].standard=6.0} 'another action'
Check-SeedProne 'pair A spending in the stand-up window' {@((Get-Prone $args[0]).standUp.after.actors|Where-Object id -CEQ $args[0].request.ownedPairs.riderAId)[0].move=3.0} 'expenditure changed'
Check-SeedProne 'pair command authority after the stand-up' {(Get-Prone $args[0]).standUp.riderAfter.pairCommandB=$true} 'pair command authority'
Check-SeedProne 'attacks outside the prone windows' {$P=Get-Prone $args[0];$P.rules.events=@($P.rules.events)+@(Copy-Value $P.rules.events[0])} 'outside its windows'
Check-SeedProne 'an unreleased healer stack' {(Get-Prone $args[0]).healerScroll.scroll.ownedItemReleased=$false} 'not released'
# b207 r57: the healed rider lay outside the target's threat; the target's own native turn approaches it (natively costed, no attack).
function Add-KmcProneTargetApproach($v,[bool]$Approached=$true){$P=Get-Prone $v;$st=$P.standUp;$round=[long]$P.healedRound
 $tg={param([string]$label,[bool]$engaged)$x=Copy-Value $st.gates;$x.label=$label;$x.engagesSubject=$engaged;$x.engagedUnits=@(if($engaged){$v.request.ownedPairs.riderBId});$x|Add-Member targetRemembersSubject $true;$x|Add-Member threatReachMeters 0.3048;$x|Add-Member subjectCorpulence 0.5;$x|Add-Member subjectMechanicsDistance 2.06;$x|Add-Member subjectLineOfSight $true;$x}
 $t=[pscustomobject][ordered]@{round=$round;targetBefore=(&$tg 'target-turn-before' (-not$Approached));engagedBefore=(-not$Approached);approached=$Approached;traceSequenceBefore=915}
 if($Approached){$wb=Copy-Value $st.before;$wb.label='prone-target-before';$wb.gameTicks=2050000000;$wa=Copy-Value $st.before;$wa.label='prone-target-after';$wa.gameTicks=2060000000
  $rider=Copy-Value $st.rider;$rider.label='target-turn-after';$rider.engagedByTarget=$true
  $proposal=[pscustomobject][ordered]@{angle=0.0;length=0.6;requested=@(0.0,0.0,2.7);goal=@(0.0,0.0,2.7);walkable=$true;traceResidual=0.0;geometryAccepted=$true;geometryRefusal=$null;refusal=$null;eligible=$true}
  $cost={param([double]$time,[double]$move)[pscustomobject][ordered]@{timeMoved=$time;metersFiveFootStep=0.0;fiveFootStep=$false;move=$move;standard=0.0;swift=0.0}}
  foreach($kv in ([ordered]@{geometry=[pscustomobject][ordered]@{corpulence=1.0;weaponReachMeters=0.3048;reach=1.3048;near=1.05;far=1.1548;gap=1.1024;engagers=@()};proposals=@($proposal);destination=@(0.0,0.0,2.7);before=$wb;ruleCountBefore=0;turnBefore=(&$cost 0.0 0.0);command=7788
   after=$wa;targetAfter=(&$tg 'target-turn-after' $true);riderAfter=$rider;turnAfter=(&$cost 0.4 0.4);turnEndedNatively=$false;rules=@();traceSequenceAfter=918;displacement=0.6;commandResult='Success';engagedAfter=$true}).GetEnumerator()){$t|Add-Member $kv.Key $kv.Value}}
 # One JSON round trip, so numbers carry the native reader shape (PowerShell 5.1 parses fractions as decimal).
 $P|Add-Member targetTurn (Copy-Value $t) -Force;$v}
function Check-ProneTarget([string]$Name,[scriptblock]$Mutation,[string]$Pattern){$v=Add-KmcProneTargetApproach (New-KmcOwnerSeedDerivedCombatFixture 'native-life-cleanup');&$Mutation $v;Check ('owner-seed prone target turn refuses '+$Name) {Invoke-OwnedReader $v} $true $Pattern}
Check 'owner-seed prone stand-up accepts the target''s native approach on its own turn' {Invoke-OwnedReader (Add-KmcProneTargetApproach (New-KmcOwnerSeedDerivedCombatFixture 'native-life-cleanup'))}
Check 'owner-seed prone stand-up accepts a target turn that already threatened rider B' {Invoke-OwnedReader (Add-KmcProneTargetApproach (New-KmcOwnerSeedDerivedCombatFixture 'native-life-cleanup') $false)}
Check-ProneTarget 'an approach that drew an attack' {$t=(Get-Prone $args[0]).targetTurn;$t.rules=@(Copy-Value (Get-Prone $args[0]).standUp.rules[0])} 'drew an attack'
Check-ProneTarget 'an approach without native movement cost' {(Get-Prone $args[0]).targetTurn.turnAfter.timeMoved=0.0} 'natively costed'
Check-ProneTarget 'an approach spending neither Move nor five-foot step' {(Get-Prone $args[0]).targetTurn.turnAfter.move=0.0} 'natively costed'
Check-ProneTarget 'an approach that left rider B unthreatened' {$t=(Get-Prone $args[0]).targetTurn;$t.engagedAfter=$false} 'boolean|Boolean'
Check-ProneTarget 'an approach whose target state does not threaten rider B' {(Get-Prone $args[0]).targetTurn.targetAfter.engagesSubject=$false} 'threatened area'
Check-ProneTarget 'an approach without the target''s memory of rider B' {(Get-Prone $args[0]).targetTurn.targetBefore.targetRemembersSubject=$false} 'engagement prerequisites'
Check-ProneTarget 'an approach although the target already threatened rider B' {$t=(Get-Prone $args[0]).targetTurn;$t.engagedBefore=$true} 'inconsistent'
Check-ProneTarget 'a target turn before the heal' {(Get-Prone $args[0]).targetTurn.round=[long](Get-Prone $args[0]).healedRound-1} 'between the heal'
Check-ProneTarget 'rider B moving before its stand-up' {(Get-Prone $args[0]).targetTurn.riderAfter.position=@(0.0,0.0,1.0)} 'moved before its stand-up'
Check-ProneTarget 'rider B no longer prone after the approach' {(Get-Prone $args[0]).targetTurn.riderAfter.prone=$false} 'conscious prone unpaired'
Check-ProneTarget 'pair A spending in the approach window' {@((Get-Prone $args[0]).targetTurn.after.actors|Where-Object id -CEQ $args[0].request.ownedPairs.riderAId)[0].move=3.0} 'expenditure changed'
Check-ProneTarget 'a destination that is not the accepted proposal' {(Get-Prone $args[0]).targetTurn.destination=@(0.0,0.0,2.2)} 'accepted native proposal'
Check-ProneTarget 'an extra unrecorded field' {(Get-Prone $args[0]).targetTurn|Add-Member teleported $true} 'prone target approach'
Check-ProneTarget 'a geometry that differs from the recorded native reach' {(Get-Prone $args[0]).targetTurn.geometry.weaponReachMeters=1.524} 'native engagement terms'
Check-ProneTarget 'an approach although the target already reached rider B in line of sight' {(Get-Prone $args[0]).targetTurn.targetBefore.subjectMechanicsDistance=1.2} 'no geometry gap'
Check-ProneTarget 'an approach recorded before the restoring heal' {$t=(Get-Prone $args[0]).targetTurn;$t.traceSequenceBefore=905} 'between the heal and rider B'
Check-ProneTarget 'an approach recorded after the stand-up began' {(Get-Prone $args[0]).targetTurn.traceSequenceAfter=925} 'between the heal and rider B'
Check-ProneTarget 'an interrupted approach' {(Get-Prone $args[0]).targetTurn.commandResult='Interrupt'} 'one native move'
Check-ProneTarget 'an approach without its native turn-end flag' {(Get-Prone $args[0]).targetTurn.PSObject.Properties.Remove('turnEndedNatively')} 'prone target approach'
Check-ProneTarget 'an unapproached turn carrying an approach' {$t=(Get-Prone $args[0]).targetTurn;$t.approached=$false;$t.engagedBefore=$true;$t.targetBefore.engagesSubject=$true} 'prone target turn'
# b208 r57: rider B's turn came before the target's; that one turn is forfeited through the native End Turn, then the target approaches.
function Add-KmcProneRiderSkip($v){$v=Add-KmcProneTargetApproach $v;$P=Get-Prone $v;$st=$P.standUp;$next=[long]$P.healedRound+1
 $P.targetTurn.round=$next;$st.round=$next;$st.rules[0].round=$next;$P.rules.events[0].round=$next
 $wb=Copy-Value $st.before;$wb.label='prone-skip-before';$wb.gameTicks=2030000000;$wa=Copy-Value $st.before;$wa.label='prone-skip-after';$wa.gameTicks=2030000000;foreach($x in @($wa.actors|Where-Object id -CEQ $v.request.ownedPairs.riderBId)){$x.standard=6.0;$x.move=3.0;$x.swift=6.0}
 $rbId=$v.request.ownedPairs.riderBId
 $r0=Copy-Value $st.rider;$r0.label='skip-before';$r0.round=[long]$P.healedRound;$r1=Copy-Value $r0;$r1.label='skip-after'
 $tb=Copy-Value $P.targetTurn.targetBefore;$tb.label='skip-before'
 $k=[pscustomobject][ordered]@{round=[long]$P.healedRound;rider=$r0;targetBefore=$tb;before=$wb;ruleCountBefore=0;traceSequenceBefore=911
  turnBefore=[pscustomobject][ordered]@{actorId=$rbId;currentActor=$rbId;status='Preparing';timeMoved=0.0;acted=$false;standard=0.0;move=0.0;swift=0.0;commandsEmpty=$true}
  turnAfter=[pscustomobject][ordered]@{actorId=$rbId;currentActor=$rbId;status='Ending';timeMoved=0.0;acted=$true;standard=6.0;move=3.0;swift=6.0;commandsEmpty=$true}
  riderAfter=$r1;after=$wa;rules=@();traceSequenceAfter=913;displacement=0.0}
 $P|Add-Member riderSkip (Copy-Value $k) -Force;$v}
function Check-ProneSkip([string]$Name,[scriptblock]$Mutation,[string]$Pattern){$v=Add-KmcProneRiderSkip (New-KmcOwnerSeedDerivedCombatFixture 'native-life-cleanup');&$Mutation $v;Check ('owner-seed prone rider skip refuses '+$Name) {Invoke-OwnedReader $v} $true $Pattern}
Check 'owner-seed prone stand-up accepts one forfeited rider-B turn before the target''s approach' {Invoke-OwnedReader (Add-KmcProneRiderSkip (New-KmcOwnerSeedDerivedCombatFixture 'native-life-cleanup'))}
Check 'owner-seed prone stand-up refuses a stand-up two rounds after the heal without a skipped turn' {$v=Add-KmcProneRiderSkip (New-KmcOwnerSeedDerivedCombatFixture 'native-life-cleanup');$P=Get-Prone $v;$P.PSObject.Properties.Remove('riderSkip');$P.standUp.round=[long]$P.healedRound+2;$P.standUp.rules[0].round=$P.standUp.round;$P.rules.events[0].round=$P.standUp.round;$P.targetTurn.round=$P.standUp.round;Invoke-OwnedReader $v} $true 'own native turn after the heal'
Check-ProneSkip 'a skip without the target''s turn after it' {(Get-Prone $args[0]).PSObject.Properties.Remove('targetTurn')} 'without the target'
Check-ProneSkip 'a skip although the target already threatened rider B' {(Get-Prone $args[0]).riderSkip.targetBefore.engagesSubject=$true} 'already threatened'
Check-ProneSkip 'a skip with movement' {(Get-Prone $args[0]).riderSkip.displacement=0.3} 'moved in its skipped turn'
Check-ProneSkip 'a skip that drew an attack' {$P=Get-Prone $args[0];$P.riderSkip.rules=@(Copy-Value $P.standUp.rules[0])} 'drew an attack'
Check-ProneSkip 'a skip that was not the native forfeit' {(Get-Prone $args[0]).riderSkip.turnAfter.move=2.0} 'native End Turn forfeit'
Check-ProneSkip 'a skip after movement in the turn' {(Get-Prone $args[0]).riderSkip.turnBefore.timeMoved=0.5} 'untouched'
Check-ProneSkip 'a skip with a pending command' {(Get-Prone $args[0]).riderSkip.riderAfter.commandsEmpty=$false} 'idle native actor'
Check-ProneSkip 'a rider no longer prone after the skip' {(Get-Prone $args[0]).riderSkip.riderAfter.prone=$false} 'idle native actor'
Check-ProneSkip 'a skip recorded before the heal' {(Get-Prone $args[0]).riderSkip.traceSequenceBefore=905} 'between the heal and its stand-up'
Check-ProneSkip 'a stand-up not on the turn after the skip' {(Get-Prone $args[0]).riderSkip.round=[long](Get-Prone $args[0]).healedRound+1} 'one turn between'
Check-ProneSkip 'pair A spending in the skip window' {@((Get-Prone $args[0]).riderSkip.after.actors|Where-Object id -CEQ $args[0].request.ownedPairs.riderAId)[0].move=3.0} 'expenditure changed'
Check-ProneSkip 'a forfeited turn of another unit' {(Get-Prone $args[0]).riderSkip.turnAfter.actorId=$args[0].request.ownedPairs.riderAId} 'rider B''s own current'
Check-ProneSkip 'a forfeit while another unit held the current turn' {(Get-Prone $args[0]).riderSkip.turnBefore.currentActor=$args[0].request.ownedPairs.riderAId} 'rider B''s own current'
Check-ProneSkip 'a world record without the forfeit' {@((Get-Prone $args[0]).riderSkip.after.actors|Where-Object id -CEQ $args[0].request.ownedPairs.riderBId)[0].move=0.0} 'native End Turn forfeit'
Check-ProneSkip 'a turn already acted before the skip' {(Get-Prone $args[0]).riderSkip.turnBefore.acted=$true} 'untouched'
Check-ProneSkip 'a nonzero cooldown before the skip' {(Get-Prone $args[0]).riderSkip.turnBefore.swift=1.0} 'untouched'
Check-ProneSkip 'a skip from a non-acting turn status' {(Get-Prone $args[0]).riderSkip.turnBefore.status='Delayed'} 'untouched'
Check-ProneSkip 'a forfeit that did not end the turn' {(Get-Prone $args[0]).riderSkip.turnAfter.status='Acting'} 'native End Turn forfeit'
Check-ProneSkip 'a target turn recorded before the skipped round' {(Get-Prone $args[0]).targetTurn.round=[long](Get-Prone $args[0]).healedRound-1} 'without the target|between the heal'
Check 'owner-seed prone stand-up accepts a skip followed by a target that already threatened rider B' {$v=Add-KmcProneRiderSkip (New-KmcOwnerSeedDerivedCombatFixture 'native-life-cleanup');$P=Get-Prone $v;$t=$P.targetTurn;$keep=[pscustomobject][ordered]@{round=$t.round;targetBefore=(Copy-Value $t.targetAfter);engagedBefore=$true;approached=$false;traceSequenceBefore=$t.traceSequenceBefore};$P|Add-Member targetTurn $keep -Force;Invoke-OwnedReader $v}
Check-ProneSkip 'an unrecorded field' {(Get-Prone $args[0]).riderSkip|Add-Member refunded $true} 'prone rider skipped turn'
Check 'prone fold refused outside the owner-seed incapacitation case' {$v=Copy-Value @($fixtures|Where-Object name -CEQ 'native-life-cleanup')[0];$v.artifact.observations|Add-Member proneStandUp ([pscustomobject]@{contract='owner-seed-prone-stand-up-v1'}) -Force;Invoke-OwnedReader $v} $true 'Only the owner-seed incapacitation case'Check 'owner-seed-derived native life evidence accepted' {Invoke-OwnedReader (New-KmcOwnerSeedDerivedCombatFixture 'native-life-cleanup')}
Check-SeedCombat 'owner-seed-derived native life refuses a native Mount input' 'native-life-cleanup' {$o=$args[0].artifact.observations;$o.inputs=@($o.inputs)+@((Copy-Value @($fixtures|Where-Object name -CEQ 'native-life-cleanup')[0]).artifact.observations.inputs|Where-Object label -CEQ 'mount-a')} 'native Mount input is not admitted'
Check-SeedCombat 'owner-seed-derived native life refuses a missing mounted start' 'native-life-cleanup' {$args[0].artifact.observations.PSObject.Properties.Remove('mountedStart')} 'mounted-start admission'
Check-SeedCombat 'owner-seed-derived native life refuses a first-action Mount record' 'native-life-cleanup' {$args[0].artifact.observations|Add-Member firstActionMount ([pscustomobject]@{}) -Force} 'admits no relationship input'
Check-SeedCombat 'owner-seed-derived native life refuses a regranted restored debt' 'native-life-cleanup' {$args[0].artifact.observations.mountedStart.debt[0].restored.Standard=0.0} 'lost or regranted'
Check-SeedCombat 'owner-seed-derived native life refuses a non-derived copy' 'native-life-cleanup' {$r=$args[0].request;$r.persistenceLoad.internalName='KMC_C7_WORLD';$r.persistenceLoad.fileName='Manual_303_KMC_C7_WORLD.zks'} 'exact scalar identity differs'
foreach($label in @('r1-native-dismount-a','first-action-mount-a')){
 Check-SeedCombat ('owner-seed-derived native life refuses an injected '+$label+' input') 'native-life-cleanup' {$o=$args[0].artifact.observations;$o.inputs=@($o.inputs)+@([pscustomobject]@{label=$label;casterId=$args[0].request.ownedPairs.riderAId})} 'admits no relationship input|native Mount input is not admitted'
}
Mutate 'acquired native life refuses a spoofed derived source alone' 'native-life-cleanup' {$args[0].artifact.observations|Add-Member ownerSeedDerivedSource ([pscustomobject]@{contract='owner-seed-derived-source-v1'}) -Force} 'Only the owner-seed-derived life case'
Mutate 'acquired native life refuses a restored mounted-start claim' 'native-life-cleanup' {$args[0].artifact.observations|Add-Member mountedStart ([pscustomobject]@{contract='owner-seed-derived-mounted-start-v1'}) -Force} 'Only the owner-seed-derived life case'
# 7E X1 death proper: the same restored mounted start, one native damage past HP+Constitution. The
# synthetic death chain follows the native 2.1.7b order: SetLifeState(Dead) leaves TB combat, calls
# OnUnitDeath (HandleUnitDeath: the one Mounted -> Unmounted cleanup), then raises the life-state
# event (an idempotent repeat from Unmounted). The dead rider and the dead control get no N+1 turn.
function ConvertTo-KmcSeedNativeDeathFixture($v){
 $r=$v.request;$o=$v.artifact.observations;$l=$o.nativeLife;$ids=@($o.actorIds)
 if($null-ne$o.PSObject.Properties['proneStandUp']){$o.PSObject.Properties.Remove('proneStandUp')};$uc=$l.unconsciousControl;if($null-ne$uc.PSObject.Properties['traceSequenceAtGrantAfter']){$bound=[long]$uc.traceSequenceAtGrantAfter;$o.allocationTrace.events=@($o.allocationTrace.events|Where-Object{$_.sequence-le$bound});$uc.PSObject.Properties.Remove('traceSequenceAtGrantAfter')};foreach($row in @($l.unconsciousControl.candidates)){if($null-ne$row.PSObject.Properties['healer']){$row.PSObject.Properties.Remove('healer')}}
 $r.ownedPairs.case='owner-seed-native-death-cleanup'
 $o.naturalUnconsciousStimulusProven=$false;$o|Add-Member naturalDeathStimulusProven $true -Force
 $l|Add-Member outcome 'death' -Force
 $l.stimulus='one labelled native RuleDealDamage past HP+Constitution; stock UnitLifeController tick kills natively and delivers existing lifecycle subscribers'
 $policy=[pscustomobject][ordered]@{trueDeath=$false;deathDoorCondition=$false;damageToParty=1.0}
 $l|Add-Member nativeDeathPolicy $policy -Force;$l|Add-Member nativeDeathPolicyAtDamage (Copy-Value $policy) -Force
 $l.requestedDamage=43;$l.nativeDamageBeforeDifficulty=43;$l.nativeDamage=43;$l.afterDamage.rider.damage=43
 foreach($e in @($l.nativeEffects.events)){if($e.kind-ceq'damage-after'){$e.targetDamage=43;$e.damage=43;$e.beforeDifficulty=43}}
 foreach($a in @($l.afterDelivery.rider,$l.independentSurvivor.rider)){$a.life='Dead';$a.conscious=$false;$a.dead=$true;$a.damage=43}
 $dra=Copy-Value $l.afterDelivery.rider;$dra|Add-Member finallyDead $false;$dra|Add-Member inCombat $false;$dra|Add-Member inState $true;$l|Add-Member deadRiderAfterDelivery $dra -Force
 $old=@($l.deliveryProbe.events)
 $mk={param($src,[string]$boundary,$previous,[string]$current,[int]$ledger)$e=Copy-Value $src;$e.boundary=$boundary;$e.previousLife=$previous;$e.currentLife=$current;$e.ledgerSequence=$ledger;$e.damage=43;$e}
 $events=@((&$mk $old[0] 'native-tick-before' $null 'Conscious' 5),(&$mk $old[1] 'native-set-before' $null 'Conscious' 5),(&$mk $old[2] 'production-death-callback-before' $null 'Dead' 5),
  (&$mk $old[3] 'production-death-callback-after' $null 'Dead' 6),(&$mk $old[3] 'production-callback-before' 'Conscious' 'Dead' 6),(&$mk $old[3] 'production-callback-after' 'Conscious' 'Dead' 7),
  (&$mk $old[4] 'native-set-after' $null 'Dead' 7),(&$mk $old[5] 'native-tick-after' $null 'Dead' 7))
 for($i=0;$i-lt8;$i++){$events[$i].sequence=$i+1}
 $l.deliveryProbe.events=$events
 $l.deliveryProbe|Add-Member deathCallbackToken '06000401' -Force;$l.deliveryProbe|Add-Member deathCallbackMvid $r.dllMvid -Force
 $l.deliveryProbe|Add-Member deathCallbackType 'KingmakerMountedCombat.Integration.MountedLifecycleSubscriber' -Force;$l.deliveryProbe|Add-Member deathCallbackName 'HandleUnitDeath' -Force
 $l.deliveries=@([pscustomobject][ordered]@{sequence=6;boundary='UnitDeath';source='IUnitHandler.HandleUnitDeath';stateBefore='Mounted';stateAfter='Unmounted';trigger='Death';attempted=$true;succeeded=$true;errors=@()},
  [pscustomobject][ordered]@{sequence=7;boundary='UnitIncapacitated';source='IUnitLifeStateChanged.HandleUnitLifeStateChanged';stateBefore='Unmounted';stateAfter='Unmounted';trigger='Incapacitated';attempted=$true;succeeded=$true;errors=@()})
 $l.lifeTransitions[0].currentLife='Dead'
 $l.riderSlotsBeforeRenewal=0;$l.expectedRiderSlots=0
 # Native death detaches through the stock RemoveUnit before OnUnitDeath (b201 d1 trace 646/659): the
 # activation is retired, the surviving mount keeps a pending exclusion for the damage round, no split.
 $at=[long]$l.traceSequenceBeforeDamage;$te=@($o.allocationTrace.events);$rm=Copy-Value @($te|Where-Object{$null-ne$_.PSObject.Properties['round']})[0]
 $rm.boundary='remove-unit-before';$rm.round=$l.damageRound;$rm.state.actor=$ids[2]
 $o.allocationTrace.events=@(@($te|Where-Object{$_.sequence-le$at})+@($rm)+@($te|Where-Object{$_.sequence-gt$at}))
 $i=0;foreach($e in $o.allocationTrace.events){$i++;$e.sequence=$i}
 $l.traceSequenceAfterDelivery=$at+1
 foreach($t in @($l.incapacitatedRiderTurn,$(if($null-ne$l.unconsciousControl.PSObject.Properties['turn']){$l.unconsciousControl.turn}))){if($null-ne$t-and$null-ne$t.PSObject.Properties['prepareSequence']-and$null-ne$t.prepareSequence){$t.prepareSequence=[long]$t.prepareSequence+1}}
 $a=$l.afterDelivery;$a.splitReleaseRound=-1;$a.partnerContextActor=$null;$a.activationIdentity=$null;$a.activationSplit=$false
 $grants=@(foreach($id in $ids){Get-KmcChunk7LifeGrant $l.beforeDamage $id})
 $l|Add-Member deathDetach ([pscustomobject][ordered]@{contract='native-death-actor-removal-detach-v1';observedRound=$l.damageRound;damageRound=$l.damageRound;pairState='Unmounted'
  activationIdentity=$null;activationSplit=$false;splitReleaseRound=-1;participationRecorded=$true;participationRiderId=$null;participationMountId=$null;participationActivation=$false
  pendingSplitMountId=$ids[3];pendingSplitRound=$l.damageRound;partnerContextActor=$null;noPreparedPairResidue=$true;grantsBefore=$grants;grantsAfter=@($grants)
  conditions=[pscustomobject][ordered]@{observedInDamageRound=$true;pairUnmounted=$true;activationRetiredByRemoval=$true;noSplitReleaseRound=$true;survivorPendingForDamageRound=$true;partnerContextReleased=$true;noPreparedPairResidue=$true;grantsUnchanged=$true}}) -Force
 $c=$l.unconsciousControl;$controlId=$c.actorId;$atDamage=[long]$l.incapacitatedRiderTurn.prepareSequence-1
 $o.allocationTrace.events=@($o.allocationTrace.events|Where-Object{-not($null-ne$_.PSObject.Properties['round']-and$_.round-eq3-and$null-ne$_.state-and$_.state.actor-cin@($ids[2],$controlId))})
 $i=0;foreach($e in $o.allocationTrace.events){$i++;$e.sequence=$i}
 foreach($g in @($l.independentSurvivor.grants|Where-Object actorId -CEQ $ids[2])){$g.count=2}
 $l|Add-Member riderDamageRoundForceToEnd 0 -Force
 $capture=[long]@($o.allocationTrace.events).Count
 $dead={param([string]$id)[pscustomobject][ordered]@{actorId=$id;round=3;traceSequenceAtCapture=$capture;events=0;prepareBefore=0;prepareAfter=0;ends=0;forcedEnds=0;forbidden=0;conscious=$false;dead=$true;finallyDead=$false;inCombat=$false}}
 $l.PSObject.Properties.Remove('incapacitatedRiderTurn');$l|Add-Member deadRiderTurn (&$dead $ids[2]) -Force
 $l.PSObject.Properties.Remove('unconsciousControl')
 # The death control reads the native order after the rider left combat (b202 d1): slots index that order.
 $after=@(@($l.nativeNextRoundOrder)|Where-Object{$_-cne$ids[2]});$c.survivorSlot=[Array]::IndexOf([string[]]$after,[string]$ids[3])
 foreach($row in @($c.candidates)){$row.slot=[Array]::IndexOf([string[]]$after,[string]$row.actorId)};$c.slot=[Array]::IndexOf([string[]]$after,[string]$c.actorId)
 $copy=[ordered]@{};foreach($p in $c.PSObject.Properties){$copy[$p.Name]=$p.Value;if($p.Name-ceq'survivorSlot'){$copy['nativeOrderAfterDeath']=[object[]]$after}};$c=[pscustomobject]$copy
 $c.contract='unmounted-dead-native-turn-control-v2';$c.requestedDamage=43;$c.nativeDamage=43;$c.afterDamage.damage=43;$c.turn=(&$dead $controlId);$c.grantAfter=$c.grantBefore;$c.traceSequenceAtDamage=$atDamage
 $l|Add-Member deathControl $c -Force
 $ext={param($a,[bool]$dead)$x=Copy-Value $a;if($dead){$x.life='Dead';$x.conscious=$false;$x.dead=$true;$x.damage=43};$x|Add-Member finallyDead $false;$x|Add-Member inCombat $false;$x|Add-Member inState $true;$x}
 $l|Add-Member deathCleanup ([pscustomobject][ordered]@{pairState='Unmounted';pairOwnerReleased=$true;noPreparedPairResidue=$true;presentationResidue=$false;shellUnsettled=$false;relationshipTransitionUnsettled=$false;preparationUnsettled=$false
  riderRegistryActive=$false;mountRegistryActive=$false;rider=(&$ext $l.beforeDamage.rider $true);mount=(&$ext $l.beforeDamage.mount $false);control=(&$ext $c.before $true)
  selectionBefore=@($ids[0],$ids[2]);selectableBefore=@($ids[0],$ids[2]);selectionAfter=@($ids[0])}) -Force
 $v
}
function New-KmcSeedNativeDeathFixture{ConvertTo-KmcSeedNativeDeathFixture (New-KmcOwnerSeedDerivedCombatFixture 'native-life-cleanup')}
function Check-SeedDeath([string]$Name,[scriptblock]$Mutation,[string]$Pattern){$v=New-KmcSeedNativeDeathFixture;&$Mutation $v;Check ('owner-seed native death refuses '+$Name) {Invoke-OwnedReader $v} $true $Pattern}
function Add-DeathTraceEvent($v,[string]$Actor,[string]$Boundary){$e=Copy-Value @($v.artifact.observations.allocationTrace.events|Where-Object{$null-ne$_.PSObject.Properties['round']-and$_.round-eq3})[0];$e.boundary=$Boundary;$e.state.actor=$Actor
 $e.sequence=@($v.artifact.observations.allocationTrace.events).Count+1;$v.artifact.observations.allocationTrace.events=@($v.artifact.observations.allocationTrace.events)+@($e)}
Check 'owner-seed native death evidence accepted (dead rider and dead control get no native N+1 turn)' {Invoke-OwnedReader (New-KmcSeedNativeDeathFixture)}
Check 'owner-seed native death accepts a dead rider still selected after cleanup' {$v=New-KmcSeedNativeDeathFixture;$c=$v.artifact.observations.nativeLife.deathCleanup;$c.selectionAfter=@($c.selectableBefore);Invoke-OwnedReader $v}
Check 'owner-seed native death accepts a rider natively raised after combat' {$v=New-KmcSeedNativeDeathFixture;$a=$v.artifact.observations.nativeLife.deathCleanup.rider;$a.dead=$false;$a.conscious=$true;$a.life='Conscious';$v.artifact.observations.nativeLife.deathCleanup.selectionAfter=@($v.artifact.observations.nativeLife.deathCleanup.selectableBefore);Invoke-OwnedReader $v}
Check 'owner-seed native death accepts the derivation-only proof when the control is declared not required' {$v=New-KmcSeedNativeDeathFixture;$l=$v.artifact.observations.nativeLife;$l.deathControl=[pscustomobject][ordered]@{contract='unmounted-dead-native-turn-control-v1';survivorSlot=$l.deathControl.survivorSlot;candidates=@();available=$false};$l.deathCleanup.control=$null;$v.request|Add-Member nativeLifeControlRequired $false -Force;Invoke-OwnedReader $v}
Check 'owner-seed native death refuses the incapacitation chain under the death case' {$v=New-KmcOwnerSeedDerivedCombatFixture 'native-life-cleanup';$v.request.ownedPairs.case='owner-seed-native-death-cleanup';Invoke-OwnedReader $v} $true 'Native evidence boolean'
Check 'owner-seed native life refuses a death outcome under the incapacitation case' {$v=New-KmcSeedNativeDeathFixture;$v.request.ownedPairs.case='owner-seed-native-life-cleanup';$v.artifact.observations.naturalUnconsciousStimulusProven=$true;Invoke-OwnedReader $v} $true 'claims a death outcome'
Check-SeedDeath 'a missing death outcome' {$args[0].artifact.observations.nativeLife.PSObject.Properties.Remove('outcome')} 'does not name its death outcome'
Check-SeedDeath 'damage below the death threshold' {$args[0].artifact.observations.nativeLife.afterDamage.rider.damage=41} 'bounded death stimulus'
Check-SeedDeath 'an oversized native request' {$l=$args[0].artifact.observations.nativeLife;$l.requestedDamage=60;$l.nativeDamageBeforeDifficulty=60} 'bounded death stimulus'
Check-SeedDeath 'an immortal subject' {$args[0].artifact.observations.nativeLife.beforeDamage.rider.immortal=$true} 'Native evidence boolean'
Check-SeedDeath 'an essential subject' {$args[0].artifact.observations.nativeLife.beforeDamage.rider.essential=$true} 'Native evidence boolean'
Check-SeedDeath 'a native death policy that changed before the stimulus' {$args[0].artifact.observations.nativeLife.nativeDeathPolicyAtDamage.trueDeath=$true} 'policy changed'
Check-SeedDeath 'a policy readout of another difficulty' {$l=$args[0].artifact.observations.nativeLife;$l.nativeDeathPolicy.damageToParty=0.5;$l.nativeDeathPolicyAtDamage.damageToParty=0.5} 'differs from the stimulus difficulty'
Check-SeedDeath 'a missing native death callback' {$p=$args[0].artifact.observations.nativeLife.deliveryProbe;$p.events=@($p.events|Where-Object boundary -CNE 'production-death-callback-before')} 'sequence/actor/round|No exact native Tick'
Check-SeedDeath 'a direct death callback outside the native tick' {$args[0].artifact.observations.nativeLife.deliveryProbe.events[2].nativeTickDepth=0} 'direct, reordered'
Check-SeedDeath 'a death callback that did not observe Dead' {$args[0].artifact.observations.nativeLife.deliveryProbe.events[3].currentLife='Unconscious'} 'actual native Dead state'
Check-SeedDeath 'a foreign death callback' {$args[0].artifact.observations.nativeLife.deliveryProbe.deathCallbackName='HandleUnitLifeStateChanged'} 'observer identity'
Check-SeedDeath 'a cleanup booked by the life-state callback instead of the death delivery' {$d=$args[0].artifact.observations.nativeLife.deliveries;$d[0].boundary='UnitIncapacitated';$d[0].source='IUnitLifeStateChanged.HandleUnitLifeStateChanged';$d[0].trigger='Incapacitated'} 'inside the native death callback'
Check-SeedDeath 'a death cleanup outside the death callback window' {$args[0].artifact.observations.nativeLife.deliveryProbe.events[3].ledgerSequence=5} 'inside the native death callback'
Check-SeedDeath 'a second cleanup from Mounted' {$args[0].artifact.observations.nativeLife.deliveries[1].stateBefore='Mounted'} 'inside the native death callback'
Check-SeedDeath 'a failed repeated delivery' {$args[0].artifact.observations.nativeLife.deliveries[1].succeeded=$false} 'idempotent success'
Check-SeedDeath 'a non-consecutive delivery window' {$args[0].artifact.observations.nativeLife.deliveries[1].sequence=9} 'consecutive window'
Check-SeedDeath 'an unconscious instead of dead transition' {$args[0].artifact.observations.nativeLife.lifeTransitions[0].currentLife='Unconscious'} 'one exact rider death'
Check-SeedDeath 'a living rider after delivery' {$a=$args[0].artifact.observations.nativeLife.afterDelivery.rider;$a.life='Unconscious';$a.dead=$false} 'Native evidence boolean'
Check-SeedDeath 'a dead rider still in combat after delivery' {$args[0].artifact.observations.nativeLife.deadRiderAfterDelivery.inCombat=$true} 'Native evidence boolean'
Check-SeedDeath 'the healthy owner changed by the death callback' {$args[0].artifact.observations.nativeLife.deliveryProbe.events[3].world.actors[0].standard=0.0} 'Unrelated native actor expenditure changed'
Check-SeedDeath 'healthy expenditure changed across the whole native tick' {$args[0].artifact.observations.nativeLife.deliveryProbe.events[7].world.actors[1].standard=0.0} 'Unrelated native actor expenditure changed'
Check-SeedDeath 'a recorded damage-round ForceToEnd absent from the trace' {$args[0].artifact.observations.nativeLife.riderDamageRoundForceToEnd=1} 'damage-round ForceToEnd'
Check-SeedDeath 'a dead-turn capture position beyond the trace' {$args[0].artifact.observations.nativeLife.deadRiderTurn.traceSequenceAtCapture=999999} 'capture position is outside'
Check-SeedDeath 'a native N+1 Prepare of the dead rider after the capture' {$v=$args[0];Add-DeathTraceEvent $v $v.artifact.observations.actorIds[2] 'turn-end-before'} 'native next-round activity'
Check-SeedDeath 'an empty captured selection' {$c=$args[0].artifact.observations.nativeLife.deathCleanup;$c.selectionBefore=@();$c.selectableBefore=@();$c.selectionAfter=@()} 'captured no selection'
Check-SeedDeath 'a captured unit that lost its selectability' {$c=$args[0].artifact.observations.nativeLife.deathCleanup;$c.selectableBefore=@($c.selectableBefore[0])} 'captured no selection or lost'
Check 'owner-seed native death accepts later native hooks of the dead rider after the capture' {$v=New-KmcSeedNativeDeathFixture;Add-DeathTraceEvent $v $v.artifact.observations.actorIds[2] 'remove-unit-before';Invoke-OwnedReader $v}
Check 'owner-seed native death accepts the native leave-combat observation and a finally-dead repeat' {$v=New-KmcSeedNativeDeathFixture;$l=$v.artifact.observations.nativeLife
 $l.deliveries=@([pscustomobject][ordered]@{sequence=6;boundary='CombatEnded';source='IUnitCombatHandler.HandleUnitLeaveCombat';stateBefore='Mounted';stateAfter='Mounted';trigger=$null;attempted=$false;succeeded=$true;errors=@()},
  [pscustomobject][ordered]@{sequence=7;boundary='UnitDeath';source='IUnitHandler.HandleUnitDeath';stateBefore='Mounted';stateAfter='Unmounted';trigger='Death';attempted=$true;succeeded=$true;errors=@()},
  [pscustomobject][ordered]@{sequence=8;boundary='UnitFinallyDead';source='IUnitFinallyDeadHandler.HandleUnitBecameFinallyDead';stateBefore='Unmounted';stateAfter='Unmounted';trigger='Death';attempted=$true;succeeded=$true;errors=@()},
  [pscustomobject][ordered]@{sequence=9;boundary='UnitIncapacitated';source='IUnitLifeStateChanged.HandleUnitLifeStateChanged';stateBefore='Unmounted';stateAfter='Unmounted';trigger='Incapacitated';attempted=$true;succeeded=$true;errors=@()})
 $e=$l.deliveryProbe.events;$e[2].ledgerSequence=6;$e[3].ledgerSequence=8;$e[4].ledgerSequence=8;foreach($i in 5..7){$e[$i].ledgerSequence=9};Invoke-OwnedReader $v}
Check 'owner-seed native death accepts a native difficulty that is not exact in binary (0.6)' {$v=New-KmcSeedNativeDeathFixture;$l=$v.artifact.observations.nativeLife;$l.difficulty=0.6;$l.nativeDeathPolicy.damageToParty=0.6;$l.nativeDeathPolicyAtDamage.damageToParty=0.6
 $l.requestedDamage=72;$l.nativeDamageBeforeDifficulty=72;foreach($e in @($l.nativeEffects.events)){if($e.kind-ceq'damage-after'){$e.beforeDifficulty=72}};$c=$l.deathControl;$c.requestedDamage=72;Invoke-OwnedReader $v}
Check-SeedDeath 'a native N+1 preparation of the dead rider' {Add-DeathTraceEvent $args[0] $args[0].artifact.observations.actorIds[2] 'prepare-before'} 'prepare the dead rider'
Check-SeedDeath 'a dead rider record that hides a trace event before its capture' {$v=$args[0];Add-DeathTraceEvent $v $v.artifact.observations.actorIds[2] 'round-state-before';$v.artifact.observations.nativeLife.deadRiderTurn.traceSequenceAtCapture=@($v.artifact.observations.allocationTrace.events).Count} 'events differs from its trace'
foreach($n in @('observedInDamageRound','pairUnmounted','activationRetiredByRemoval','noSplitReleaseRound','survivorPendingForDamageRound','partnerContextReleased','noPreparedPairResidue','grantsUnchanged')){
 Check-SeedDeath ('a failed death detach condition '+$n) ([scriptblock]::Create("`$args[0].artifact.observations.nativeLife.deathDetach.conditions.$n=`$false")) ("condition $n failed")}
Check-SeedDeath 'a death detach round recorded as text' {$args[0].artifact.observations.nativeLife.deathDetach.pendingSplitRound='2'} 'pendingSplitRound is missing or mistyped'
Check-SeedDeath 'missing death detach conditions' {$args[0].artifact.observations.nativeLife.PSObject.Properties.Remove('deathDetach')} 'detach conditions were not recorded'
Check-SeedDeath 'the incapacitation split shape claimed under death' {$d=$args[0].artifact.observations.nativeLife.deathDetach;$d.activationSplit=$true;$d.splitReleaseRound=2} 'actor-removal shape'
Check-SeedDeath 'a pending exclusion of the wrong actor' {$v=$args[0];$v.artifact.observations.nativeLife.deathDetach.pendingSplitMountId=$v.artifact.observations.actorIds[1]} 'actor-removal shape'
Check-SeedDeath 'a pending exclusion of another round' {$args[0].artifact.observations.nativeLife.deathDetach.pendingSplitRound=3} 'actor-removal shape'
Check-SeedDeath 'a retained partner context' {$v=$args[0];$v.artifact.observations.nativeLife.deathDetach.partnerContextActor=$v.artifact.observations.actorIds[3]} 'actor-removal shape'
Check-SeedDeath 'a detach observed in a later round' {$args[0].artifact.observations.nativeLife.deathDetach.observedRound=3} 'not observed in the damage round'
Check-SeedDeath 'a detach grant record that hides a preparation' {$d=$args[0].artifact.observations.nativeLife.deathDetach;$d.grantsAfter=@($d.grantsAfter[0],$d.grantsAfter[1],([long]$d.grantsAfter[2]+1),$d.grantsAfter[3])} 'grant counts'
Check-SeedDeath 'a death without the native RemoveUnit observation' {$v=$args[0];foreach($e in @($v.artifact.observations.allocationTrace.events|Where-Object boundary -CEQ 'remove-unit-before')){$e.boundary='clear-before'}} 'native RemoveUnit'
Check-SeedDeath 'a split activation after delivery' {$args[0].artifact.observations.nativeLife.afterDelivery.activationSplit=$true} 'Native evidence boolean'
Check-SeedDeath 'a native N+1 slot claimed for the dead rider' {$args[0].artifact.observations.nativeLife.riderSlotsBeforeRenewal=1} 'no native next-round slot'
Check-SeedDeath 'a granted dead rider' {@($args[0].artifact.observations.nativeLife.independentSurvivor.grants|Where-Object actorId -CEQ $args[0].artifact.observations.actorIds[2])[0].count=3} 'grants were duplicated'
Check-SeedDeath 'a dead rider turn claiming combat' {$args[0].artifact.observations.nativeLife.deadRiderTurn.inCombat=$true} 'Native evidence boolean'
Check-SeedDeath 'a dead control prepared natively' {$c=$args[0].artifact.observations.nativeLife.deathControl;Add-DeathTraceEvent $args[0] $c.actorId 'prepare-before'} 'native next-round activity'
Check-SeedDeath 'a dead control granted after its death' {$args[0].artifact.observations.nativeLife.deathControl.grantAfter=3} 'native preparation after its death'
Check-SeedDeath 'a control that did not die' {$args[0].artifact.observations.nativeLife.deathControl.turn.dead=$false} 'Native evidence boolean'
Check-SeedDeath 'control damage below its death threshold' {$args[0].artifact.observations.nativeLife.deathControl.afterDamage.damage=41} 'bounded death stimulus'
Check-SeedDeath 'a control that is not the first eligible companion' {$args[0].artifact.observations.nativeLife.deathControl.actorId='5dd7bd1d-ae4c-4df7-9687-50fceb027344'} 'first eligible companion'
Check-SeedDeath 'a required control declared unavailable' {$l=$args[0].artifact.observations.nativeLife;$l.deathControl=[pscustomobject][ordered]@{contract='unmounted-dead-native-turn-control-v1';survivorSlot=$l.deathControl.survivorSlot;candidates=@();available=$false};$l.deathCleanup.control=$null} 'Required unmounted death control is unavailable'
# b202 d1: the control is chosen after the rider left native combat, so its slots index the recorded
# damage-time order without the dead rider. Real b202 ids and values; v1 (no recorded order) and v2.
$b202Order=@('73681ef2-b281-4bc6-bc05-2de68c1616cb','fd213210-71cd-41b0-b35b-68e1b18e9b43','8265f8cf-0969-4ec7-a266-2448de91c8c4','29f3c260-78d1-4c19-9445-97a4fac3b524','5dd7bd1d-ae4c-4df7-9687-50fceb027344','e9cd259e-69d5-4c11-9fcb-f25cc381bc8e','e3a3be8b-b035-4d7d-80b3-6670f3dc0183')
Check 'b202 death control order is the recorded order without the dead rider (survivor 4, candidates 1 and 3)' {$o=Get-KmcChunk7DeathControlOrder $b202Order 'fd213210-71cd-41b0-b35b-68e1b18e9b43'
 if($o.Count-ne6-or[Array]::IndexOf($o,'e9cd259e-69d5-4c11-9fcb-f25cc381bc8e')-ne4-or[Array]::IndexOf($o,'8265f8cf-0969-4ec7-a266-2448de91c8c4')-ne1-or[Array]::IndexOf($o,'5dd7bd1d-ae4c-4df7-9687-50fceb027344')-ne3-or[Array]::IndexOf($o,'fd213210-71cd-41b0-b35b-68e1b18e9b43')-ne-1){throw 'b202 control order derivation differs.'}}
Check 'death control order refuses a dead rider absent from the damage-time order' {Get-KmcChunk7DeathControlOrder @($b202Order|Select-Object -Skip 2) 'fd213210-71cd-41b0-b35b-68e1b18e9b43'} $true 'lacks the dead rider'
function Set-KmcSeedDeathControlV1($v){$c=$v.artifact.observations.nativeLife.deathControl;$c.contract='unmounted-dead-native-turn-control-v1';$c.PSObject.Properties.Remove('nativeOrderAfterDeath')}
function Get-KmcSeedDeathDamageTimeIndex($v,[string]$Id){[Array]::IndexOf([string[]]@($v.artifact.observations.nativeLife.nativeNextRoundOrder),$Id)}
Check 'owner-seed native death accepts a v1 control without its recorded order (b202 shape)' {$v=New-KmcSeedNativeDeathFixture;Set-KmcSeedDeathControlV1 $v;Invoke-OwnedReader $v}
Check-SeedDeath 'a v1 control survivor slot at its damage-time index' {Set-KmcSeedDeathControlV1 $args[0];$l=$args[0].artifact.observations.nativeLife;$l.deathControl.survivorSlot=Get-KmcSeedDeathDamageTimeIndex $args[0] $args[0].artifact.observations.actorIds[3]} 'Unmounted death control contract differs'
Check-SeedDeath 'a v2 control survivor slot at its damage-time index' {$l=$args[0].artifact.observations.nativeLife;$l.deathControl.survivorSlot=Get-KmcSeedDeathDamageTimeIndex $args[0] $args[0].artifact.observations.actorIds[3]} 'Unmounted death control contract differs'
Check-SeedDeath 'a v2 control whose recorded order still lists the dead rider' {$l=$args[0].artifact.observations.nativeLife;$l.deathControl.nativeOrderAfterDeath=@($l.nativeNextRoundOrder)} 'native order without the dead rider'
Check-SeedDeath 'a v2 control whose recorded order is reordered' {$l=$args[0].artifact.observations.nativeLife;$r=@($l.deathControl.nativeOrderAfterDeath);[Array]::Reverse($r);$l.deathControl.nativeOrderAfterDeath=$r} 'native order without the dead rider'
Check-SeedDeath 'a v2 control without its recorded order' {$args[0].artifact.observations.nativeLife.deathControl.PSObject.Properties.Remove('nativeOrderAfterDeath')} 'native order without the dead rider'
Check-SeedDeath 'a v1 control carrying a recorded order' {$args[0].artifact.observations.nativeLife.deathControl.contract='unmounted-dead-native-turn-control-v1'} 'Unmounted death control contract differs'
Check-SeedDeath 'a control candidate slot at its damage-time index' {$v=$args[0];$c=$v.artifact.observations.nativeLife.deathControl;$row=@($c.candidates)[0];$row.slot=Get-KmcSeedDeathDamageTimeIndex $v $row.actorId;if($row.slot-eq[Array]::IndexOf([string[]]@($c.nativeOrderAfterDeath),[string]$row.actorId)){$row.slot++}} 'candidate identity or slot differs'
Check-SeedDeath 'unobserved cleanup postconditions' {$args[0].artifact.observations.nativeLife.PSObject.Properties.Remove('deathCleanup')} 'were not observed'
Check-SeedDeath 'a pair owner still bound at cleanup' {$args[0].artifact.observations.nativeLife.deathCleanup.pairOwnerReleased=$false} 'Native evidence boolean'
Check-SeedDeath 'an unsettled relationship transition at cleanup' {$args[0].artifact.observations.nativeLife.deathCleanup.relationshipTransitionUnsettled=$true} 'Native evidence boolean'
Check-SeedDeath 'a surviving mount still registered at cleanup' {$args[0].artifact.observations.nativeLife.deathCleanup.mountRegistryActive=$true} 'Native evidence boolean'
Check-SeedDeath 'a mount lost at cleanup' {$a=$args[0].artifact.observations.nativeLife.deathCleanup.mount;$a.dead=$true;$a.conscious=$false;$a.life='Dead'} 'lost the surviving mount'
Check-SeedDeath 'an inconsistent rider state at cleanup' {$args[0].artifact.observations.nativeLife.deathCleanup.rider.dead=$false} 'rider state is inconsistent'
Check-SeedDeath 'a living unit dropped from the restored selection' {$args[0].artifact.observations.nativeLife.deathCleanup.selectionAfter=@()} 'did not restore the captured selection'
Check-SeedDeath 'a selection outside the captured selection' {$args[0].artifact.observations.nativeLife.deathCleanup.selectionAfter=@($args[0].artifact.observations.actorIds[0],$args[0].artifact.observations.actorIds[1])} 'not a subset'
Check-SeedDeath 'a cleanup without the observed dead control' {$args[0].artifact.observations.nativeLife.deathCleanup.control=$null} 'observed death control'
# 7E X1 C03 attack modes: rider A's native Rapid Shot off/on mounted full attacks and an unmounted
# control (round N, N+1, Dismount in N+2, N+3). Synthetic shapes: BAB-4 ranged rider, one attack
# at +7 off; Rapid Shot -2 adds one attack, so +5/+5 on (mounted and unmounted alike).
function New-KmcSeedAttackModesFixture([bool]$Bab6=$false){
 # Bab6: rider A has BAB 6, so the Rapid Shot off leg is an iterative full attack (+7/+2) and each Rapid Shot leg adds one at the highest bonus.
 $v=New-KmcOwnerSeedDerivedCombatFixture 'combat-rider-first';$r=$v.request;$o=$v.artifact.observations;$roles=@(Get-KmcChunk7OwnedPairIds $r)
 $r.ownedPairs.case='owner-seed-attack-modes'
 foreach($key in @('r1','firstActionMount','localFailure','registeredDisable','removalRefusal','pendingSurvivor')){if($null-ne$o.PSObject.Properties[$key]){$o.PSObject.Properties.Remove($key)}}
 $o.activations=@()
 $dismount=Copy-Value @($v.artifact.observations.inputs|Where-Object label -CEQ 'r1-native-dismount-a')[0]
 if($null-eq$dismount){$dismount=[pscustomobject]@{}}
 $dismount=[pscustomobject][ordered]@{label='attack-modes-native-dismount-a';casterId=$roles[0];targetId=$roles[0];abilityGuid='3af2b81f4d72bbb30501fa730fcdf36e';nativeCastDelta=1;nativeRefusalDelta=0;clicked=$true;terminal=$true;processTerminal=$true}
 $o.inputs=@($dismount)
 $weapon='4de4658c0e9b0d146b9a08ed6f030f8a';$rapidBuff='0f310c1e709e15e4fa693db15a4baeb4'
 $trace=$o.allocationTrace;$base=[long](@($trace.events)|Measure-Object -Property sequence -Maximum).Maximum
 $snap={param([string]$label,[double]$riderStandard,[long]$round)$x=Copy-Value $o.snapshots[0];$x.label=$label;$x.round=$round;foreach($a in $x.actors){$a.standard=0.0;$a.move=0.0;if($a.id-ceq$roles[0]){$a.standard=$riderStandard}};$x}
 $rapid={param([bool]$on,[long]$round,[bool]$combat)[pscustomobject][ordered]@{label='x';toggle='90a77bfe25ec2e14caf8bd5cde9febf2';isOn=$on;isRunning=$on;buff=$on;inCombat=$combat;round=$round}}
 $ruleCount=0;$ms=@()
 $specs=if($Bab6){@(@('mounted-off',$true,$false,0,@(7,2)),@('mounted-on',$true,$true,1,@(5,5,0)),@('unmounted-on',$false,$true,3,@(5,5,0)))}else{@(@('mounted-off',$true,$false,0,@(7)),@('mounted-on',$true,$true,1,@(5,5)),@('unmounted-on',$false,$true,3,@(5,5)))}
 foreach($spec in $specs){
  $round=10+[long]$spec[3];$inner=@($spec[4]);$events=@();$seq=$ruleCount
  for($i=0;$i-lt$inner.Count;$i++){
   $seq++;$events+=[pscustomobject][ordered]@{sequence=$seq;kind='attack';identity=100+$seq;frame=1;round=$round;actor=$roles[0];target=$o.targetId;opportunity=$false;fullAttack=($inner.Count-gt1);firstAttack=($i-eq0);attackNumber=$i;attacksCount=$inner.Count;weapon=$weapon;ranged=$true;natural=$false}
   $bonuses=@([pscustomobject]@{bonus=4;source=$null;sourceName=$null});if($spec[2]){$bonuses+=[pscustomobject]@{bonus=-2;source=$rapidBuff;sourceName='RapidShotBuff'}}
   $seq++;$events+=[pscustomobject][ordered]@{sequence=$seq;kind='roll';identity=200+$seq;frame=1;round=$round;actor=$roles[0];target=$o.targetId;attack=100+$seq-1;opportunity=$false;attackBonus=$inner[$i];iterativePenalty=0;weapon=$weapon;ranged=$true
    nativeCalculation=[pscustomobject]@{innerResult=$inner[$i];result=$inner[$i];concealment=0;flanking=0;shootIntoCombat=0;bab=4;playerFaction=$true;trueDeath=$false};bonuses=$bonuses}
  }
  $plan=@(for($i=0;$i-lt$inner.Count;$i++){[pscustomobject][ordered]@{index=$i;weapon=$weapon;ranged=$true;natural=$false;owner=$roles[0];iterativePenalty=0}})
  $tb=$base;$base++;$trace.events=@($trace.events)+@([pscustomobject]@{sequence=$base;boundary='cost-after';frame=1;round=$round;state=[pscustomobject]@{actor=$roles[0]}});$ta=$base
  $ms+=[pscustomobject][ordered]@{label=$spec[0];mounted=$spec[1];round=$round;rapid=(&$rapid $spec[2] $round $true);before=(&$snap ($spec[0]+'-before') 0.0 $round);ruleCountBefore=$ruleCount;traceSequenceBefore=$tb;pairState=$(if($spec[1]){'Mounted'}else{'Unmounted'})
   clicked=$true;after=(&$snap ($spec[0]+'-after') 6.0 $round);nativeEstimate=[long]$inner.Count;expectedFull=($inner.Count-gt1);currentTurnEnabledFullAttack=($inner.Count-gt1);command=[pscustomobject][ordered]@{identity=9000+$ruleCount;executor=$roles[0];target=$o.targetId;fullAttack=($inner.Count-gt1);singleAttack=$false;result='Success';completedAttacks=$inner.Count;plan=$plan
    interruptWatch=[pscustomobject][ordered]@{watched=$true;watchStart=[pscustomobject][ordered]@{atClick=$true;frame=1;finished=$false;result='None';acted=$false};interrupts=@();interruptAsSoonAsPossible=$false;executorCanAct=$true;targetInState=$true;targetDead=$false}}
   rules=$events;traceSequenceAfter=$ta;rapidAfter=(&$rapid $spec[2] $round $true)}
  $ruleCount=$seq
 }
 $clickOff=[pscustomobject][ordered]@{label='pre-combat-off';before=(&$rapid $true 0 $false);possible=$true;after=(&$rapid $false 0 $false)}
 $clickOn=[pscustomobject][ordered]@{label='round-n-on';before=(&$rapid $false 10 $true);possible=$true;after=(&$rapid $true 10 $true)}
 $o|Add-Member attackModes ([pscustomobject][ordered]@{contract='owner-seed-attack-modes-v2';riderId=$roles[0];mountId=$roles[1];rapidShotFeature=1;rapidShotToggles=1;weapon=$weapon;weaponRanged=$true;weaponRangeMeters=15.24
  rapidBefore=(&$rapid $true 0 $false);rapidOff=(&$rapid $false 0 $false);toggleClicks=@($clickOff,$clickOn);measurements=$ms;firstRound=10;stage=7
  dismountBefore=(&$snap 'attack-modes-dismount-before' 0.0 12);dismountAfter=(&$snap 'attack-modes-dismount-after' 0.0 12);complete=$true
  rules=[pscustomobject][ordered]@{dropped=0;errors=@();events=@($ms|ForEach-Object{@($_.rules)})}}) -Force
 $v.name='combat-attack-modes';$v
}
function Check-SeedAttackModes([string]$Name,[scriptblock]$Mutation,[string]$Pattern){$v=New-KmcSeedAttackModesFixture;&$Mutation $v;Check ('owner-seed attack modes refuses '+$Name) {Invoke-OwnedReader $v} $true $Pattern}
function AM($v,[int]$i){$v.artifact.observations.attackModes.measurements[$i]}
# One recorded UnitCommand.Interrupt(bool) of the measured command (contract v2), native callers by default.
function New-KmcAttackModeInterrupt($Command,[string]$ResultBefore,[bool]$Acted,[long]$AttackIndex,[string]$Callers='callers=MonoMod.Utils.DynamicMethodDefinition.Kingmaker.UnitLogic.Commands.Base.UnitCommand.Interrupt_Patch3 < Kingmaker.Controllers.Units.UnitActionController.TickCommand < Kingmaker.Controllers.Units.UnitActionController.TickOnUnit'){
 [pscustomobject][ordered]@{frame=2;round=13;command=$Command.identity;resultBefore=$ResultBefore;actedBefore=$Acted;finishedBefore=$false;interruptAsSoonAsPossible=$false;timeSinceStart=3.1;attackIndex=$AttackIndex;plannedAttacks=@($Command.plan).Count;executorCanAct=$true;targetInState=$true;targetDead=$false;callers=$Callers;callersTruncated=$false}}
# b202 am unmounted control: both planned shots resolved (OnAction Success), then one native Interrupt(bool) during the last animation.
function Set-KmcAttackModeOverwrite($v,[int]$i=2){$c=(AM $v $i).command;$c.result='Interrupt';$c.interruptWatch.interrupts=@(New-KmcAttackModeInterrupt $c 'Success' $true @($c.plan).Count);$c}
Check 'owner-seed attack modes accept a native interrupt that overwrote the completed plan''s Success (b202 unmounted shape)' {$v=New-KmcSeedAttackModesFixture;$null=Set-KmcAttackModeOverwrite $v;Invoke-OwnedReader $v}
Check 'owner-seed attack modes accept an inert interrupt after the command finished beside a Success' {$v=New-KmcSeedAttackModesFixture;$c=(AM $v 1).command;$x=New-KmcAttackModeInterrupt $c 'Success' $true 2;$x.finishedBefore=$true;$c.interruptWatch.interrupts=@($x);Invoke-OwnedReader $v}
Check-SeedAttackModes 'a v1 plan-complete interrupt (b202 evidence shape)' {$m=$args[0].artifact.observations.attackModes;$m.contract='owner-seed-attack-modes-v1';foreach($x in @($m.measurements)){$x.command.PSObject.Properties.Remove('interruptWatch')};$m.measurements[2].command.result='Interrupt'} 'completed plan without its recorded native cause'
Check-SeedAttackModes 'a v1 measurement carrying an interrupt watch' {$args[0].artifact.observations.attackModes.contract='owner-seed-attack-modes-v1'} 'interrupt observation differs from its contract'
Check-SeedAttackModes 'a v2 measurement without its interrupt watch' {(AM $args[0] 0).command.PSObject.Properties.Remove('interruptWatch')} 'interrupt observation differs from its contract'
Check-SeedAttackModes 'an unwatched command' {$c=Set-KmcAttackModeOverwrite $args[0];$c.interruptWatch.watched=$false} 'Native evidence boolean'
Check-SeedAttackModes 'a watch opened on an already finished command' {$c=Set-KmcAttackModeOverwrite $args[0];$c.interruptWatch.watchStart.finished=$true} 'Native evidence boolean'
Check-SeedAttackModes 'a watch without its start' {$c=Set-KmcAttackModeOverwrite $args[0];$c.interruptWatch.watchStart=$null} 'lacks its start'
Check-SeedAttackModes 'an interrupt recorded before its watch opened' {$c=Set-KmcAttackModeOverwrite $args[0];$c.interruptWatch.watchStart.frame=3} 'not the measured native command'
Check 'owner-seed attack modes accept a watch opened when the command was first observed after the click' {$v=New-KmcSeedAttackModesFixture;$c=Set-KmcAttackModeOverwrite $v;$c.interruptWatch.watchStart.atClick=$false;Invoke-OwnedReader $v}
Check-SeedAttackModes 'a plan-complete interrupt from a KingmakerMountedCombat frame' {$c=Set-KmcAttackModeOverwrite $args[0];$c.interruptWatch.interrupts[0].callers='callers=Kingmaker.UnitLogic.Commands.Base.UnitCommand.Interrupt < KingmakerMountedCombat.Integration.MountedCombatController.Cancel'} 'interrupt claims a completed plan'
Check-SeedAttackModes 'a plan-complete interrupt before the last attack acted' {$c=Set-KmcAttackModeOverwrite $args[0];$c.interruptWatch.interrupts[0].resultBefore='None';$c.interruptWatch.interrupts[0].actedBefore=$false} 'interrupt claims a completed plan'
Check-SeedAttackModes 'a plan-complete interrupt before the last attack frame' {$c=Set-KmcAttackModeOverwrite $args[0];$c.interruptWatch.watchStart.frame=0;$c.interruptWatch.interrupts[0].frame=0} 'interrupt claims a completed plan'
Check-SeedAttackModes 'a plan-complete interrupt at another attack index' {$c=Set-KmcAttackModeOverwrite $args[0];$c.interruptWatch.interrupts[0].attackIndex=1} 'interrupt claims a completed plan'
Check-SeedAttackModes 'a plan-complete interrupt whose caller chain was truncated' {$c=Set-KmcAttackModeOverwrite $args[0];$c.interruptWatch.interrupts[0].callersTruncated=$true} 'interrupt claims a completed plan'
Check-SeedAttackModes 'a plan-complete interrupt by a queued-command overwrite' {$c=Set-KmcAttackModeOverwrite $args[0];$c.interruptWatch.interrupts[0].interruptAsSoonAsPossible=$true} 'interrupt claims a completed plan'
Check-SeedAttackModes 'an interrupt record without its truncation flag' {$c=Set-KmcAttackModeOverwrite $args[0];$c.interruptWatch.interrupts[0].PSObject.Properties.Remove('callersTruncated')} 'Exact|propert'
Check-SeedAttackModes 'two effective interrupts' {$c=Set-KmcAttackModeOverwrite $args[0];$c.interruptWatch.interrupts=@($c.interruptWatch.interrupts[0],(Copy-Value $c.interruptWatch.interrupts[0]))} 'lacks its one recorded native cause'
Check-SeedAttackModes 'an interrupt record of another command' {$c=Set-KmcAttackModeOverwrite $args[0];$c.interruptWatch.interrupts[0].command=1} 'not the measured native command'
Check-SeedAttackModes 'a Success that hides an effective interrupt' {$c=(AM $args[0] 1).command;$c.interruptWatch.interrupts=@(New-KmcAttackModeInterrupt $c 'Success' $true 2)} 'Success hides an effective native interrupt'
Check-SeedAttackModes 'a natural-tail interrupt without its recorded cause' {$v=$args[0];$c=(AM $v 1).command;$c.plan=@($c.plan)+@([pscustomobject][ordered]@{index=2;weapon='35dfad6517f401145af54111be04d6cf';ranged=$false;natural=$true;owner=$v.request.ownedPairs.mountAId;iterativePenalty=0});$c.result='Interrupt';$c.completedAttacks=2;(AM $v 1).nativeEstimate=3} 'lacks its one recorded native cause'
Check 'owner-seed attack modes evidence accepted (Rapid Shot off/on mounted, unmounted control)' {Invoke-OwnedReader (New-KmcSeedAttackModesFixture)}
Check 'owner-seed attack modes accept a save that left Rapid Shot off (no pre-combat click)' {$v=New-KmcSeedAttackModesFixture;$m=$v.artifact.observations.attackModes;$m.rapidBefore.isOn=$false;$m.toggleClicks=@($m.toggleClicks[1]);Invoke-OwnedReader $v}
Check 'owner-seed attack modes accept a native interrupt in the natural tail after the ranged plan' {$v=New-KmcSeedAttackModesFixture;$c=(AM $v 1).command;$c.plan=@($c.plan)+@([pscustomobject][ordered]@{index=2;weapon='35dfad6517f401145af54111be04d6cf';ranged=$false;natural=$true;owner=$v.request.ownedPairs.mountAId;iterativePenalty=0});$c.result='Interrupt';$c.completedAttacks=2;(AM $v 1).nativeEstimate=3;$c.interruptWatch.interrupts=@(New-KmcAttackModeInterrupt $c 'None' $false 2)
 $k=(AM $v 2).command;$k.plan=@($k.plan);Invoke-OwnedReader $v}
Check 'combat evidence refuses an attack-modes record outside its case' {$v=New-KmcOwnerSeedDerivedCombatFixture 'combat-rider-first';$v.artifact.observations|Add-Member attackModes ([pscustomobject]@{}) -Force;Invoke-OwnedReader $v} $true 'belongs only to its closed owner-seed selection'
Check-SeedAttackModes 'a missing Rapid Shot feat' {$args[0].artifact.observations.attackModes.rapidShotFeature=0} 'native Rapid Shot feat'
Check-SeedAttackModes 'a melee weapon' {$args[0].artifact.observations.attackModes.weaponRanged=$false} 'native Rapid Shot feat, toggle or ranged weapon'
Check-SeedAttackModes 'Rapid Shot still on before combat' {$args[0].artifact.observations.attackModes.rapidOff.isOn=$true} 'not off out of combat'
Check-SeedAttackModes 'a toggle switched other than by its native slot clicks' {$m=$args[0].artifact.observations.attackModes;$m.toggleClicks=@($m.toggleClicks[0])} 'toggle clicks differ'
Check-SeedAttackModes 'a slot click that did not switch' {$args[0].artifact.observations.attackModes.toggleClicks[1].after.isOn=$false} 'did not switch the native toggle'
Check-SeedAttackModes 'a missing measurement' {$m=$args[0].artifact.observations.attackModes;$m.measurements=@($m.measurements[0],$m.measurements[1])} 'exactly three native ordinary attacks'
Check-SeedAttackModes 'measurements out of order' {$m=$args[0].artifact.observations.attackModes;$m.measurements=@($m.measurements[1],$m.measurements[0],$m.measurements[2])} 'natively running exactly when on'
Check-SeedAttackModes 'a control still mounted' {(AM $args[0] 2).mounted=$true} 'differs in mode, round or toggle'
Check-SeedAttackModes 'a control in the dismount round' {(AM $args[0] 2).round=12} 'differs in mode, round or toggle'
Check-SeedAttackModes 'a single attack instead of a full attack' {(AM $args[0] 1).command.fullAttack=$false} 'not one native full attack'
Check-SeedAttackModes 'a full attack claimed for a one-attack estimate' {(AM $args[0] 0).command.fullAttack=$true} 'not one native single attack'
Check-SeedAttackModes 'an explicit single-attack command' {(AM $args[0] 0).command.singleAttack=$true} 'not one native single attack'
Check-SeedAttackModes 'a Rapid Shot leg with a one-attack estimate' {$m=AM $args[0] 1;$m.nativeEstimate=1;$m.expectedFull=$false} 'estimate is missing or inconsistent'
Check-SeedAttackModes 'an expectation that contradicts its native estimate' {(AM $args[0] 0).expectedFull=$true} 'estimate is missing or inconsistent'
Check-SeedAttackModes 'a full plan longer than its native estimate' {(AM $args[0] 2).nativeEstimate=3} 'plan differs from its native estimate'
Check-SeedAttackModes 'a native cursor contradicting its estimate' {(AM $args[0] 0).currentTurnEnabledFullAttack=$true} 'contradicts its native estimate'
Check 'owner-seed attack modes accept an unobserved native cursor' {$v=New-KmcSeedAttackModesFixture;(AM $v 0).currentTurnEnabledFullAttack=$null;Invoke-OwnedReader $v}
Check-SeedAttackModes 'a missing native estimate' {(AM $args[0] 0).PSObject.Properties.Remove('nativeEstimate')} 'estimate is missing or inconsistent'
Check-SeedAttackModes 'a single-attack swing labelled as a full attack' {@((AM $args[0] 0).rules|Where-Object kind -CEQ 'attack')[0].fullAttack=$true} 'declared mode'
Check-SeedAttackModes 'a full attack by another executor' {(AM $args[0] 1).command.executor=$args[0].request.ownedPairs.mountAId} 'not one native full attack'
Check-SeedAttackModes 'a roll missing from the ranged plan' {$m=AM $args[0] 1;$m.rules=@($m.rules|Select-Object -SkipLast 1)} 'rolls differ from its ranged plan'
Check-SeedAttackModes 'a mount attack inside the rider window' {$m=AM $args[0] 0;$e=Copy-Value $m.rules[1];$e.sequence=99;$e.actor=$args[0].request.ownedPairs.mountAId;$m.rules=@($m.rules)+@($e)} 'consecutive own attacks'
Check-SeedAttackModes 'an attack of opportunity counted as a plan attack' {$m=AM $args[0] 0;$m.rules[1].opportunity=$true} 'rolls differ from its ranged plan'
Check-SeedAttackModes 'a ranged entry after the natural tail' {$c=(AM $args[0] 1).command;$c.plan=@([pscustomobject][ordered]@{index=0;weapon='35dfad6517f401145af54111be04d6cf';ranged=$false;natural=$true;owner=$args[0].request.ownedPairs.mountAId;iterativePenalty=0})+@($c.plan|ForEach-Object{$x=Copy-Value $_;$x.index++;$x});(AM $args[0] 1).nativeEstimate=3} 'lead with rider A'
Check-SeedAttackModes 'an incomplete ranged plan' {(AM $args[0] 1).command.completedAttacks=1} 'did not complete its ranged plan'
Check-SeedAttackModes 'a Rapid Shot source while off' {$b=(AM $args[0] 0).rules[1];$b.bonuses=@($b.bonuses)+@([pscustomobject]@{bonus=-2;source='0f310c1e709e15e4fa693db15a4baeb4';sourceName='RapidShotBuff'})} 'does not carry exactly its Rapid Shot source'
Check-SeedAttackModes 'Rapid Shot without its extra attack' {$m=AM $args[0] 1;$m.rules=@($m.rules[0],$m.rules[1]);$m.command.plan=@($m.command.plan[0]);$m.command.completedAttacks=1} 'plan differs from its native estimate'
Check-SeedAttackModes 'Rapid Shot bonuses that differ from the off attack' {foreach($r in @((AM $args[0] 1).rules|Where-Object kind -CEQ 'roll')){$r.nativeCalculation.innerResult=4}} 'exactly one attack at the highest bonus'
Check-SeedAttackModes 'a mounted attack bonus different from the unmounted control' {$r=@((AM $args[0] 2).rules|Where-Object kind -CEQ 'roll')[1];$r.nativeCalculation.innerResult=6} 'differs from the unmounted control'
Check-SeedAttackModes 'a control without the extra Rapid Shot attack' {$m=AM $args[0] 2;$m.rules=@($m.rules[0],$m.rules[1]);$m.command.plan=@($m.command.plan[0]);$m.command.completedAttacks=1} 'plan differs from its native estimate'
Check-SeedAttackModes 'a mount spending inside the rider full attack' {$m=AM $args[0] 1;$x=@($m.after.actors|Where-Object id -CEQ $args[0].request.ownedPairs.mountAId)[0];$x.standard=6.0} 'expenditure changed'
Check-SeedAttackModes 'a full attack without rider A''s own native action' {$m=AM $args[0] 0;@($m.after.actors|Where-Object id -CEQ $args[0].request.ownedPairs.riderAId)[0].standard=0.0} 'did not spend rider A'
Check-SeedAttackModes 'a mount native cost inside the rider window' {$v=$args[0];$m=AM $v 1;$e=@($v.artifact.observations.allocationTrace.events|Where-Object{$_.sequence-eq$m.traceSequenceAfter})[0];$e.state.actor=$v.request.ownedPairs.mountAId} 'no native cost on rider A or spent the mount'
Check-SeedAttackModes 'a different native cost shape between modes' {$v=$args[0];$m=AM $v 2;$t=$v.artifact.observations.allocationTrace;$m.traceSequenceAfter++;$t.events=@($t.events)+@([pscustomobject]@{sequence=$m.traceSequenceAfter;boundary='cost-after';frame=1;round=13;state=[pscustomobject]@{actor=$v.request.ownedPairs.riderAId}})} 'cost shape'
Check-SeedAttackModes 'a second relationship input' {$o=$args[0].artifact.observations;$o.inputs=@($o.inputs)+@($o.inputs[0])} 'exactly the one native Dismount input'
Check-SeedAttackModes 'a carried primary activation' {$o=$args[0].artifact.observations;$o.activations=@([pscustomobject]@{pair=0})} 'record no primary pair activation'
Check-SeedAttackModes 'an incomplete run' {$args[0].artifact.observations.attackModes.complete=$false} 'Native evidence boolean is missing, mistyped or differs'
Check-SeedAttackModes 'a dropped rule observation' {$args[0].artifact.observations.attackModes.rules.dropped=1} 'dropped or failed'
Check-SeedAttackModes 'a failed rule observation' {$args[0].artifact.observations.attackModes.rules.errors=@('boom')} 'dropped or failed'
Check-SeedAttackModes 'a missing rule observation' {$args[0].artifact.observations.attackModes.PSObject.Properties.Remove('rules')} 'dropped or failed'
Check-SeedAttackModes 'a toggle that changed during the full attack' {(AM $args[0] 1).rapidAfter.isOn=$false} 'differs in mode, round or toggle'
Check-SeedAttackModes 'Rapid Shot not running while on' {(AM $args[0] 1).rapid.isRunning=$false} 'natively running exactly when on'
Check-SeedAttackModes 'a roll without its Rapid Shot source while on' {$r=@((AM $args[0] 2).rules|Where-Object kind -CEQ 'roll')[1];$r.bonuses=@($r.bonuses|Where-Object{$_.source-cne'0f310c1e709e15e4fa693db15a4baeb4'})} 'exactly its Rapid Shot source'
Check-SeedAttackModes 'a plan weapon other than the equipped ranged weapon' {$p=(AM $args[0] 0).command.plan[0];$p.weapon='35dfad6517f401145af54111be04d6cf'} 'lead with rider A'
Check-SeedAttackModes 'an interrupt that claims a completed plan without a recorded cause' {(AM $args[0] 1).command.result='Interrupt'} 'lacks its one recorded native cause'
Check-SeedAttackModes 'an on click outside the first measured round' {$args[0].artifact.observations.attackModes.toggleClicks[1].before.round=11} 'first measured round'
Check-SeedAttackModes 'a Dismount outside its round' {$args[0].artifact.observations.attackModes.dismountAfter.round=13} 'outside its native round'
# Pair B (and mount A once unmounted) never spends; finished turns' cooldowns decay natively for every unit.
function AMActor($v,[int]$i,[string]$when,[string]$role){@((AM $v $i).$when.actors|Where-Object id -CEQ $v.request.ownedPairs.$role)[0]}
Check 'owner-seed attack modes accept pair B cooldowns that only decayed natively (N+1 and N+3)' {$v=New-KmcSeedAttackModesFixture;foreach($i in 1,2){foreach($role in 'riderBId','mountBId'){(AMActor $v $i 'before' $role).standard=6.0;(AMActor $v $i 'after' $role).standard=4.5;(AMActor $v $i 'before' $role).reactionCooldown=6.0;(AMActor $v $i 'after' $role).reactionCooldown=4.5}};foreach($i in 1,2){(AM $v $i).after.gameTicks=[long](AM $v $i).before.gameTicks+20000000};Invoke-OwnedReader $v}
Check 'owner-seed attack modes accept mount A cooldowns that only decayed natively in the unmounted control' {$v=New-KmcSeedAttackModesFixture;(AMActor $v 2 'before' 'mountAId').move=6.0;(AMActor $v 2 'after' 'mountAId').move=3.0;(AM $v 2).after.gameTicks=[long](AM $v 2).before.gameTicks+30000000;Invoke-OwnedReader $v}
Check-SeedAttackModes 'pair B spending its move inside the mounted-on window' {(AMActor $args[0] 1 'after' 'mountBId').move=3.0} 'Unrelated native actor expenditure changed'
Check-SeedAttackModes 'pair B spending inside the unmounted control' {(AMActor $args[0] 2 'after' 'riderBId').standard=6.0} 'Unrelated native actor expenditure changed'
Check-SeedAttackModes 'pair B spending a reaction inside the control' {(AMActor $args[0] 2 'after' 'riderBId').reactions=0} 'Unrelated native actor expenditure changed'
Check-SeedAttackModes 'a pair B reaction cooldown that grew' {(AMActor $args[0] 0 'after' 'mountBId').reactionCooldown=6.0} 'Unrelated native actor expenditure changed'
Check-SeedAttackModes 'mount A cooldowns that changed while it carries rider A' {$v=$args[0];(AMActor $v 1 'before' 'mountAId').move=6.0;(AMActor $v 1 'after' 'mountAId').move=3.0} 'Unrelated native actor expenditure changed'
Check-SeedAttackModes 'mount A spending inside the unmounted control' {(AMActor $args[0] 2 'after' 'mountAId').move=3.0} 'Unrelated native actor expenditure changed'Check-SeedAttackModes 'a pair B cooldown refunded faster than native time' {$v=$args[0];(AMActor $v 1 'before' 'riderBId').standard=6.0;(AMActor $v 1 'after' 'riderBId').standard=0.0;(AM $v 1).after.gameTicks=[long](AM $v 1).before.gameTicks+5000000} 'faster than native time'
Check-SeedAttackModes 'a negative pair B cooldown' {(AMActor $args[0] 1 'after' 'mountBId').move=-1.0} 'resource observation is invalid'
Check-SeedAttackModes 'a pair B native cost boundary inside the control window' {$v=$args[0];$m=AM $v 2;$t=$v.artifact.observations.allocationTrace;$m.traceSequenceAfter++;$t.events=@($t.events)+@([pscustomobject]@{sequence=$m.traceSequenceAfter;boundary='cost-before';frame=1;round=13;state=[pscustomobject]@{actor=$v.request.ownedPairs.riderBId}})} 'must not spend'
# 7E X1 C11 reactions: pair A leaves the diagnostic target's threatened area by one ordinary native
# ground move on rider A's first prepared turn; the target's single native attack of opportunity
# hits pair A. Synthetic shapes: the pair starts threatened (no approach) in round 10.
function New-KmcSeedReactionFixture([bool]$Approach=$false){
 $v=New-KmcOwnerSeedDerivedCombatFixture 'combat-rider-first';$r=$v.request;$o=$v.artifact.observations;$roles=@(Get-KmcChunk7OwnedPairIds $r)
 $r.ownedPairs.case='owner-seed-combat-reaction';$script:rxTraceBase=[long](@($o.allocationTrace.events)|Measure-Object -Property sequence -Maximum).Maximum
 foreach($key in @('r1','firstActionMount','localFailure','registeredDisable','removalRefusal','pendingSurvivor')){if($null-ne$o.PSObject.Properties[$key]){$o.PSObject.Properties.Remove($key)}}
 $o.activations=@();$o.inputs=@()
 $snap={param([string]$label,[double]$mountMove)$x=Copy-Value $o.snapshots[0];$x.label=$label;$x.round=10;foreach($a in $x.actors){$a.standard=0.0;$a.move=0.0;$a.swift=0.0;if($a.id-ceq$roles[1]){$a.move=$mountMove}};$x}
 $state={param([string]$label,[long]$reactions,[bool]$engaged,[double]$distance)[pscustomobject][ordered]@{label=$label;id=$o.targetId;round=10;frame=100;reactions=$reactions;reactionsPerRound=1;canAttackOfOpportunity=$true;canActInCombat=$true;waitingInitiative=$false;initiativeCooldown=0.0;surprised=$false;canAct=$true;threatHand=$true;riderRemembersTarget=$true;mountRemembersTarget=$true;standard=0.0;move=0.0;swift=0.0;prone=$false;proneRequested=$false;conscious=$true;commandsEmpty=$true;engagesRider=$engaged;engagesMount=$engaged;engagedUnits=@($(if($engaged){$roles[0];$roles[1]}));position=@(0.0,0.0,3.5);mountDistance=$distance;corpulence=1.5;mountCorpulence=0.9}}
 $residue=[pscustomobject][ordered]@{activeCommand=$false;groundMovement=$false;riderCommandsEmpty=$true;mountCommandsEmpty=$true;state='Mounted';riderConscious=$true;mountConscious=$true;riderProne=$false;mountProne=$false;riderDamage=4;riderMaxHp=38;mountDamage=0;mountMaxHp=30}
 $leg={param([string]$label,[double]$moveBefore,[double]$moveAfter,[long]$rulesBefore,$rules,[bool]$engagedBefore,[bool]$engagedAfter,[long]$reactionsAfter,[double]$distanceBefore,[double]$distanceAfter,[long]$traceBefore)
  [pscustomobject][ordered]@{label=$label;round=10;destination=@(0.0,0.0,-4.5);requestedDistance=4.5;before=(&$snap ($label+'-before') $moveBefore);targetBefore=(&$state ($label+'-before') 1 $engagedBefore $distanceBefore);ruleCountBefore=$rulesBefore;traceSequenceBefore=($script:rxTraceBase+$traceBefore)
   fiveFootStep=$false;singleActionMove=$false;clicked=$true;command=(700+$rulesBefore);proposals=@();after=(&$snap ($label+'-after') $moveAfter);targetAfter=(&$state ($label+'-after') $reactionsAfter $engagedAfter $distanceAfter);rules=@($rules);traceSequenceAfter=($script:rxTraceBase+$traceBefore+5)
   distance=4.5;result='Success';residue=(Copy-Value $residue);pairBefore=(Copy-Value $residue);turnActor=$roles[0]}}
 $victim=$roles[1];$aoo=[pscustomobject][ordered]@{sequence=1;kind='attack';identity=4242;frame=101;round=10;actor=$o.targetId;target=$victim;opportunity=$true;fullAttack=$false;weapon='35dfad6517f401145af54111be04d6cf';ranged=$false;natural=$true;actorPosition=@(0.0,0.0,3.5);targetPosition=@(0.0,0.0,-1.0)}
 $x=[ordered]@{contract='owner-seed-combat-reaction-v2';riderId=$roles[0];mountId=$roles[1];targetId=$o.targetId;proneStandUp=[pscustomobject][ordered]@{covered=$false;reason='No deterministic native prone source exists for the diagnostic target on the owner-seed fixture.'};stage=3;firstRound=10}
 if($Approach){
  $x.precondition=&$state 'precondition' 1 $false 9.0
  $x.approach=&$leg 'approach' 0.0 1.5 0 @() $false $true 1 9.0 3.5 10
  $aoo.sequence=1;$d=&$leg 'disengage' 1.5 3.0 0 @($aoo) $true $false 0 3.5 8.5 20
 }else{
  $x.precondition=&$state 'precondition' 1 $true 3.5
  $d=&$leg 'disengage' 0.0 1.5 0 @($aoo) $true $false 0 3.5 8.5 10
 }
 $d|Add-Member clearDistance 8.0;$x.disengageGates=&$state 'disengage-gates' 1 $true 3.5;$x.disengage=$d
 $x.rules=[pscustomobject][ordered]@{dropped=0;errors=@();events=@($aoo)};$x.complete=$true
 # Round N+1 (11): the folded F04 defensive cast on mount A from inside the threatened area (success shape).
 $w11={param([string]$label,[long]$ticks)$s=&$snap $label 0.0;$s.round=11;$s.gameTicks=$ticks;$s}
 $item=[pscustomobject][ordered]@{item=9101;blueprint='cd635d5720937b044a354dba17abad8d';count=4;charges=1;exactSlot=$true}
 $ability=[pscustomobject][ordered]@{identity=9102;blueprint='5590652e1c2225c4ca30c4a699ab3649';caster=$roles[0];sourceItem=9101;sourceItemBlueprint='cd635d5720937b044a354dba17abad8d'}
 $cs=&$state 'defensive-cast-before' 1 $true 3.5;$cs.round=11;$ca=&$state 'defensive-cast-after' 1 $true 3.5;$ca.round=11;$pre=&$state 'defensive-precondition' 1 $true 3.5;$pre.round=11
 $after=Copy-Value $item;$after.count=3
 $events=@([pscustomobject]@{kind='defensive-before';actor=$roles[0];identity=9103},[pscustomobject]@{kind='defensive-rule-after';actor=$roles[0];identity=9104;dc=17;roll=19;success=$true},
  [pscustomobject]@{kind='cast-after';actor=$roles[0];identity=9105;ability=$ability;target=$roles[1]},[pscustomobject]@{kind='item-spend-after';actor=$roles[0];identity=9101;blueprint='cd635d5720937b044a354dba17abad8d';result=$true;count=3})
 $cast=[pscustomobject][ordered]@{round=11;before=(&$w11 'defensive-cast-before' 1000000000);targetBefore=$cs;pairBefore=(Copy-Value $residue);riderEngaged=$true;mountEngaged=$true;ruleCountBefore=1;traceSequenceBefore=($script:rxTraceBase+40)
  itemsBefore=$item;castEventsBefore=0;ability=$ability;target=$roles[1];selected=$true;canTarget=$true;clicked=$true;admittedShellCount=1;shell=[pscustomobject]@{identity=9106};shellIdentity=9106
  itemsAfter=$after;castEvents=$events;terminal=[pscustomobject]@{identity=9106;executor=$roles[0];finished=$true;acted=$true;result='Success';timeSinceStart=1.5}
  after=(&$w11 'defensive-cast-after' 1015000000);targetAfter=$ca;residue=(Copy-Value $residue);rules=@();traceSequenceAfter=($script:rxTraceBase+50);turnActor=$roles[0]}
 $rb=@($cast.after.actors|Where-Object id -CEQ $roles[0])[0];$rb.standard=6.0
 $x.defensiveCast=[pscustomobject][ordered]@{contract='reaction-defensive-cast-v1';riderId=$roles[0];mountId=$roles[1];casterId=$roles[0];scrollUnits=4;scrollBlueprint='cd635d5720937b044a354dba17abad8d';scrollAbility=$ability
  stage=8;round=11;precondition=$pre;cast=$cast;check=[pscustomobject][ordered]@{kind='casting-defensively';dc=17;roll=19;success=$true};outcome='success'
  rulesCapture=[pscustomobject][ordered]@{dropped=0;errors=@();events=@($aoo)};scroll=[pscustomobject]@{ownedItemReleased=$true;disposed=$true};originalSlots=[pscustomobject]@{before=@()};castTrace=[pscustomobject]@{dropped=0;faults=0};complete=$true}
 $o|Add-Member reaction ([pscustomobject]$x) -Force
 $v.name='combat-reaction';$v
}
function Check-SeedReaction([string]$Name,[scriptblock]$Mutation,[string]$Pattern){$v=New-KmcSeedReactionFixture;&$Mutation $v;Check ('owner-seed combat reaction refuses '+$Name) {Invoke-OwnedReader $v} $true $Pattern}
function Get-RxLeg($v){$v.artifact.observations.reaction.disengage}
function RActor($w,[string]$id){@($w.actors|Where-Object id -CEQ $id)[0]}
Check 'owner-seed combat reaction evidence accepted (threatened start, one native attack of opportunity)' {Invoke-OwnedReader (New-KmcSeedReactionFixture)}
Check 'owner-seed combat reaction accepts a native approach before the disengagement' {Invoke-OwnedReader (New-KmcSeedReactionFixture $true)}
function Set-RxDefenseFailure($v,[bool]$Aoo){$x=$v.artifact.observations.reaction;$d=$x.defensiveCast;$c=$d.cast;$roles=@(Get-KmcChunk7OwnedPairIds $v.request)
 $d.check.success=$false;$d.check.roll=3;$d.outcome='failure';@($c.castEvents|Where-Object kind -CEQ 'defensive-rule-after')[0].success=$false;@($c.castEvents|Where-Object kind -CEQ 'defensive-rule-after')[0].roll=3
 if($Aoo){$e=Copy-Value $x.rules.events[0];$e.sequence=2;$e.round=11;$e.target=$roles[0];$c.rules=@($e);$c.targetAfter.reactions=0;$d.rulesCapture.events=@($x.rules.events[0],$e)};$v}
function Get-RxDefense($v){$v.artifact.observations.reaction.defensiveCast}
Check 'owner-seed combat reaction accepts the N+1 defensive cast with a passed native check' {Invoke-OwnedReader (New-KmcSeedReactionFixture)}
Check 'owner-seed combat reaction accepts a failed native defensive check with its one attack of opportunity' {Invoke-OwnedReader (Set-RxDefenseFailure (New-KmcSeedReactionFixture) $true)}
Check 'owner-seed combat reaction accepts a failed native defensive check whose attack is not made' {Invoke-OwnedReader (Set-RxDefenseFailure (New-KmcSeedReactionFixture) $false)}
Check-SeedReaction 'the v1 contract without the defensive cast' {$x=$args[0].artifact.observations.reaction;$x.contract='owner-seed-combat-reaction-v1';$x.PSObject.Properties.Remove('defensiveCast')} 'exact pair A'
Check-SeedReaction 'a missing defensive cast' {$args[0].artifact.observations.reaction.PSObject.Properties.Remove('defensiveCast')} 'defensive casting measurement is missing'
Check-SeedReaction 'a defensive cast in round N' {(Get-RxDefense $args[0]).round=10} 'next native round'
Check-SeedReaction 'a cast from outside the threatened area' {$c=(Get-RxDefense $args[0]).cast;$c.riderEngaged=$false;$c.mountEngaged=$false} 'inside the target'
Check-SeedReaction 'a cast on rider A itself' {(Get-RxDefense $args[0]).cast.target=$args[0].request.ownedPairs.riderAId} 'one native scroll click by rider A on mount A'
Check-SeedReaction 'a skipped native defensive check' {$c=(Get-RxDefense $args[0]).cast;$c.castEvents=@($c.castEvents|Where-Object kind -CNotIn @('defensive-before','defensive-rule-after'))} 'did not run exactly once'
Check-SeedReaction 'a duplicated native defensive check' {$c=(Get-RxDefense $args[0]).cast;$c.castEvents=@($c.castEvents)+@(Copy-Value @($c.castEvents|Where-Object kind -CEQ 'defensive-rule-after')[0])} 'did not run exactly once'
Check-SeedReaction 'a recorded check differing from its rule' {(Get-RxDefense $args[0]).check.roll=2} 'differs from its native rule'
Check-SeedReaction 'no scroll spend' {$c=(Get-RxDefense $args[0]).cast;$c.castEvents=@($c.castEvents|Where-Object kind -CNE 'item-spend-after')} 'spend exactly once'
Check-SeedReaction 'two scroll spends' {$c=(Get-RxDefense $args[0]).cast;$c.castEvents=@($c.castEvents)+@(Copy-Value @($c.castEvents|Where-Object kind -CEQ 'item-spend-after')[0])} 'spend exactly once'
Check-SeedReaction 'a stack not decremented in place' {(Get-RxDefense $args[0]).cast.itemsAfter.count=4} 'decrement in place'
Check-SeedReaction 'an attack of opportunity after a passed check' {$x=$args[0].artifact.observations.reaction;$d=$x.defensiveCast;$e=Copy-Value $x.rules.events[0];$e.round=11;$d.cast.rules=@($e);$d.cast.targetAfter.reactions=0;$d.rulesCapture.events=@($x.rules.events[0],$e)} 'differ from its outcome'
Check-SeedReaction 'a target count not matching the attack' {(Get-RxDefense (Set-RxDefenseFailure $args[0] $true)).cast.targetAfter.reactions=1} 'count does not match'
Check-SeedReaction 'a passed check with no cast' {$c=(Get-RxDefense $args[0]).cast;$c.castEvents=@($c.castEvents|Where-Object kind -CNE 'cast-after')} 'neither acted|cast the scroll exactly once'
Check 'owner-seed combat reaction accepts a failed check whose hit lost the cast to a failed concentration check' {$v=Set-RxDefenseFailure (New-KmcSeedReactionFixture) $true;$c=(Get-RxDefense $v).cast;$rider=$v.request.ownedPairs.riderAId
 $c.castEvents=@($c.castEvents|Where-Object kind -CNE 'cast-after')+@([pscustomobject]@{kind='damage';actor=$v.artifact.observations.targetId;identity=9301;target=$rider;damage=2},[pscustomobject]@{kind='concentration-rule-after';actor=$rider;identity=9302;dc=12;roll=4;success=$false})
 @($c.after.actors|Where-Object id -CEQ $rider)[0].standard=0.0;Invoke-OwnedReader $v}
Check-SeedReaction 'an acted cast without rider A''s native Standard charge' {$c=(Get-RxDefense $args[0]).cast;@($c.after.actors|Where-Object id -CEQ $args[0].request.ownedPairs.riderAId)[0].standard=0.0} 'neither acted'
Check-SeedReaction 'a cast with only mount A engaged' {(Get-RxDefense $args[0]).cast.riderEngaged=$false} 'inside the target'
Check-SeedReaction 'a concentration check without damage' {$c=(Get-RxDefense $args[0]).cast;$c.castEvents=@($c.castEvents)+@([pscustomobject]@{kind='concentration-rule-after';actor=$args[0].request.ownedPairs.riderAId;dc=16;roll=10;success=$false})} 'without damage'
Check-SeedReaction 'mount A spending on the rider cast' {@((Get-RxDefense $args[0]).cast.after.actors|Where-Object id -CEQ $args[0].request.ownedPairs.mountAId)[0].standard=6.0} 'expenditure changed'
Check-SeedReaction 'pair B spending on the rider cast' {@((Get-RxDefense $args[0]).cast.after.actors|Where-Object id -CEQ $args[0].request.ownedPairs.riderBId)[0].move=3.0} 'expenditure changed'
Check-SeedReaction 'a refunded rider A Standard' {$c=(Get-RxDefense $args[0]).cast;@($c.before.actors|Where-Object id -CEQ $args[0].request.ownedPairs.riderAId)[0].standard=6.0;@($c.after.actors|Where-Object id -CEQ $args[0].request.ownedPairs.riderAId)[0].standard=0.0} 'refunded|neither acted'
Check-SeedReaction 'rider A moving on its cast' {@((Get-RxDefense $args[0]).cast.after.actors|Where-Object id -CEQ $args[0].request.ownedPairs.riderAId)[0].move=3.0} 'move or swift'
Check-SeedReaction 'attacks outside the windows' {$d=Get-RxDefense $args[0];$d.rulesCapture.events=@($d.rulesCapture.events)+@(Copy-Value $d.rulesCapture.events[0])} 'outside the reaction and defensive windows'
Check-SeedReaction 'an unreleased scroll stack' {(Get-RxDefense $args[0]).scroll.ownedItemReleased=$false} 'not released'
Check-SeedReaction 'another scroll item' {(Get-RxDefense $args[0]).cast.ability.sourceItemBlueprint='d52566ae8cbe8dc4dae977ef51c27d91'} 'one native scroll click'
Check-SeedReaction 'a target without its attack of opportunity before the cast' {(Get-RxDefense $args[0]).precondition.reactions=0} 'one native attack of opportunity before the defensive cast'Check 'owner-seed combat reaction accepts the attack of opportunity on rider A' {$v=New-KmcSeedReactionFixture;$e=(Get-RxLeg $v).rules[0];$e.target=$v.request.ownedPairs.riderAId;$v.artifact.observations.reaction.rules.events[0].target=$e.target;Invoke-OwnedReader $v}
function Add-RxDelay($v){$o=$v.artifact.observations;$x=$o.reaction;$roles=@(Get-KmcChunk7OwnedPairIds $v.request)
 $tb=Copy-Value $x.precondition;$tb.label='delay-before';$tb.canActInCombat=$false;$tb.waitingInitiative=$true;$tb.initiativeCooldown=3.0
 $b=Copy-Value $x.disengage.before;$b.label='reaction-delay-before';$b|Add-Member currentActor $roles[0] -Force;$rs=Copy-Value $b;$rs.label='reaction-delay-resumed';$rs.gameTicks=[long]$b.gameTicks+30000000
 $x|Add-Member initiativeDelay ([pscustomobject][ordered]@{round=10;targetBefore=$tb;canDelay=$true;riderSlot=0;targetSlot=3;targetWait=3.0;nextRoundWait=6.0;before=$b;traceSequenceBefore=([long]$script:rxTraceBase-1);statusAfter='Delayed';suspended=$true;resumed=$rs;traceSequenceResumed=([long]$script:rxTraceBase-1)}) -Force;$v}
Check 'owner-seed combat reaction accepts one native same-round Delay behind a target resolving its initiative' {Invoke-OwnedReader (Add-RxDelay (New-KmcSeedReactionFixture))}
Check-SeedReaction 'a Delay although the target had resolved its initiative' {$x=(Add-RxDelay $args[0]).artifact.observations.reaction.initiativeDelay;$x.targetBefore.waitingInitiative=$false;$x.targetBefore.canActInCombat=$true} 'had resolved its native initiative'
Check-SeedReaction 'a Delay behind an earlier unit' {(Add-RxDelay $args[0]).artifact.observations.reaction.initiativeDelay.targetSlot=0} 'same-round suspension'
Check-SeedReaction 'a Delay crossing the round' {(Add-RxDelay $args[0]).artifact.observations.reaction.initiativeDelay.targetWait=6.0} 'same-round suspension'
Check-SeedReaction 'a Delay that did not suspend the turn' {(Add-RxDelay $args[0]).artifact.observations.reaction.initiativeDelay.statusAfter='Acting'} 'same-round suspension'
Check-SeedReaction 'a resume in another round' {(Add-RxDelay $args[0]).artifact.observations.reaction.initiativeDelay.resumed.round=11} 'did not resume its delayed native turn'
Check-SeedReaction 'a resume on another actor' {$v=Add-RxDelay $args[0];$v.artifact.observations.reaction.initiativeDelay.resumed.currentActor=$v.request.ownedPairs.riderBId} 'did not resume its delayed native turn'
Check-SeedReaction 'a Delay that spent mount A' {$v=Add-RxDelay $args[0];@($v.artifact.observations.reaction.initiativeDelay.resumed.actors|Where-Object id -CEQ $v.request.ownedPairs.mountAId)[0].standard=6.0} 'expenditure changed'
Check-SeedReaction 'a Delay window after the first leg' {$r=(Add-RxDelay $args[0]).artifact.observations.reaction;$r.initiativeDelay.traceSequenceResumed=[long]$r.disengage.traceSequenceBefore+1} 'not before its first leg'
Check-SeedReaction 'a pair A cost inside the Delay window' {$v=Add-RxDelay $args[0];$dl=$v.artifact.observations.reaction.initiativeDelay;$t=$v.artifact.observations.allocationTrace;$e=@($t.events|Where-Object{$_.sequence-eq[long]$dl.traceSequenceBefore})[0];$dl.traceSequenceResumed=[long]$dl.traceSequenceBefore;$dl.traceSequenceBefore=[long]$dl.traceSequenceBefore-1;$e.boundary='cost-after';$e.state.actor=$v.request.ownedPairs.riderAId} 'native Delay has a native cost boundary'
Check-SeedReaction 'an incomplete Delay record' {(Add-RxDelay $args[0]).artifact.observations.reaction.initiativeDelay.PSObject.Properties.Remove('resumed')} 'reaction initiative delay'
Check 'combat evidence refuses a reaction record outside its case' {$v=New-KmcOwnerSeedDerivedCombatFixture 'combat-rider-first';$v.artifact.observations|Add-Member reaction ([pscustomobject]@{}) -Force;Invoke-OwnedReader $v} $true 'belongs only to its closed owner-seed selection'
# 7E X2 F02: owner-seed real-time Charge on the owner's KMC_C7_CHARGE_SEED lineage (melee weapon as saved).
function New-KmcSeedChargeFixture{
 $v=New-KmcOwnerSeedDerivedCombatFixture 'realtime-charge';$o=$v.artifact.observations;$c=Get-KmcChunk7ChargeSeed
 foreach($x in @($v.request.ownedPairs,$o.ownerSeedDerivedSource)){$x.seedId=$c.seedId;$x.seedSha256=$c.sha256};$o.targetAuthorization.seedSha256=$c.sha256
 foreach($k in 'before','after'){$o.chargeWeapon.$k=New-KmcChargeWeaponState $c.handSet $c.primaryWeapon}
 $o.chargeTargetPlacement=New-KmcTestChargePlacement $o.targetAuthorization.position;$v
}
function Check-SeedCharge([string]$Name,[scriptblock]$Mutation,[string]$Pattern){$v=New-KmcSeedChargeFixture;&$Mutation $v;Check ('owner-seed real-time Charge refuses '+$Name) {Invoke-OwnedReader $v} $true $Pattern}
Check 'owner-seed real-time Charge evidence accepted (charge seed lineage, saved melee hand set)' {Invoke-OwnedReader (New-KmcSeedChargeFixture)}
# Owner decision (d): KMC_C7_LEVEL6_SEED reaches no owned two-pair case (rider A owns a native animal companion).
foreach($kind in @('realtime-charge','combat-rider-first','combat-mount-first','native-life-cleanup')){$lk=$kind;Check ('level-6 seed refused for owner-seed '+$lk) {$v=New-KmcOwnerSeedDerivedCombatFixture $lk;$c=Get-KmcChunk7Level6Seed;$v.request.ownedPairs.seedId=$c.seedId;$v.request.ownedPairs.seedSha256=$c.sha256;Invoke-OwnedReader $v} $true 'KMC_C7_LEVEL6_SEED is refused'}
Check 'level-6 seed hash under another id refused' {$v=New-KmcOwnerSeedDerivedCombatFixture 'combat-rider-first';$v.request.ownedPairs.seedSha256=(Get-KmcChunk7Level6Seed).sha256;Invoke-OwnedReader $v} $true 'KMC_C7_LEVEL6_SEED is refused'
# Owner decision of October 24 ("I'll make a mod seed"): KMC_C7_MOUNTED6_SEED reaches only its own lineage.
$script:m6repo=(Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
Check 'mounted6 seed constants agree across the C# selection, the PS readers, the cold reader and the registry' {$c=Get-KmcChunk7Mounted6Seed;$cs=[IO.File]::ReadAllText((Join-Path $script:m6repo 'src\KingmakerMountedCombat\Diagnostics\Chunk7OwnedPairsSelection.cs'))
 $expected=[ordered]@{Mounted6SeedId=('"'+$c.seedId+'"');Mounted6SeedSha256=('"'+$c.sha256+'"');Mounted6RiderA=('"'+$c.riderAId+'"');Mounted6RiderAHorse=('"'+$c.horseId+'"');Mounted6MeleeWeapon=('"'+$c.meleeWeapon+'"');Mounted6RangedWeapon=('"'+$c.rangedWeapon+'"');Mounted6MinimumBaseAttackBonus=[string]$c.minimumBab}
 foreach($name in @($expected.Keys)){if($cs-notmatch([regex]::Escape($name)+'\s*=\s*'+[regex]::Escape($expected[$name])+'[;,]')){throw ('C# constant differs: '+$name)}}
 if($cs-notmatch('Mounted6MeleeHandSet = '+$c.meleeHandSet+', Mounted6RangedHandSet = '+$c.rangedHandSet+';')){throw 'C# hand-set constants differ.'}
 $e=@(((Get-Content -LiteralPath (Join-Path $script:m6repo 'scripts\runtime\Chunk7OwnerSeedRegistry.json') -Raw)|ConvertFrom-Json).seeds|Where-Object seedId -CEQ $c.seedId);if($e.Count-ne1-or$e[0].sha256-cne$c.sha256-or$e[0].fileName-cne'Manual_308_KMC_C7_MOUNTED6_SEED.zks'){throw 'Registry mounted6 seed entry differs.'}
 if(-not(Test-KmcChunk7OwnerMadeSeedEntry $e[0])){throw 'The cold reader does not admit the registered mounted6 entry.'}
 foreach($other in @('KMC_C7_CHARGE_SEED','KMC_C7_LEVEL6_SEED','KMC_C7_NO_MODS_OWNER_SEED')){$x=@(((Get-Content -LiteralPath (Join-Path $script:m6repo 'scripts\runtime\Chunk7OwnerSeedRegistry.json') -Raw)|ConvertFrom-Json).seeds|Where-Object seedId -CEQ $other)[0];if(Test-KmcChunk7OwnerMadeSeedEntry $x){throw ('Owner-made metadata admitted for '+$other)}}
 $forged=Copy-Value $e[0];$forged.sha256=('7'*64);if(Test-KmcChunk7OwnerMadeSeedEntry $forged){throw 'Owner-made metadata admitted for another master hash.'}}
Check 'mounted6 seed admitted only along its lineage' {$c=Get-KmcChunk7Mounted6Seed
 Assert-KmcChunk7Mounted6SeedBinding ([pscustomobject]@{case='owner-seed-persistence-save';seedId=$c.seedId;seedSha256=$c.sha256}) 'multi-exploration-manual'
 Assert-KmcChunk7Mounted6SeedBinding ([pscustomobject]@{case='owner-seed-persistence-load';seedId=$c.seedId;seedSha256=$c.sha256}) 'multi-exploration-manual'
 Assert-KmcChunk7Mounted6SeedBinding ([pscustomobject]@{case='owner-seed-prepared-casting';seedId=$c.seedId;seedSha256=$c.sha256}) $null
 Assert-KmcChunk7Mounted6SeedBinding ([pscustomobject]@{case='owner-seed-attack-modes';seedId=$c.seedId;seedSha256=$c.sha256}) $null
 foreach($bad in @(@('owner-seed-exploration',$null),@('owner-seed-persistence-load','multi-exploration-three-loads-manual'),@('owner-seed-checkpoint-save','multi-exploration-quick'),@('owner-seed-combat-rider-first',$null),@('owner-seed-realtime-charge',$null),@('owner-seed-native-life-cleanup',$null),@('owner-seed-combat-reaction',$null),@('owner-seed-staged',$null),@('owner-seed-load-refusal','multi-malformed-refusal-manual'))){
  $threw=$false;try{Assert-KmcChunk7Mounted6SeedBinding ([pscustomobject]@{case=$bad[0];seedId=$c.seedId;seedSha256=$c.sha256}) $bad[1]}catch{$threw=$_.Exception.Message-like'*only along its own lineage*'};if(-not$threw){throw ('Mounted6 seed admitted for '+$bad[0])}}
 foreach($forged in @(@($c.seedId,('7'*64)),@('KMC_C7_NO_MODS_OWNER_SEED',$c.sha256))){$threw=$false;try{Assert-KmcChunk7Mounted6SeedBinding ([pscustomobject]@{case='owner-seed-attack-modes';seedId=$forged[0];seedSha256=$forged[1]}) $null}catch{$threw=$true};if(-not$threw){throw 'A forged mounted6 identity was admitted.'}}
 foreach($seed in @(@('KMC_C7_CHARGE_SEED','1ec2f3219102d1967e1e1e7f860543550062ef07f6c270be050d26d7059aba41'),@('KMC_C7_NO_MODS_OWNER_SEED','3405d7c813668ad646788efd746a62c1b16e1438fad1bc4bcbebc961821b924f'))){$threw=$false;try{Assert-KmcChunk7Mounted6SeedBinding ([pscustomobject]@{case='owner-seed-prepared-casting';seedId=$seed[0];seedSha256=$seed[1]}) $null}catch{$threw=$_.Exception.Message-like'*requires exactly the registered KMC_C7_MOUNTED6_SEED*'};if(-not$threw){throw ('Prepared casting admitted '+$seed[0])}}
 Assert-KmcChunk7Mounted6SeedBinding ([pscustomobject]@{case='owner-seed-combat-rider-first';seedId='KMC_C7_NO_MODS_OWNER_SEED';seedSha256='3405d7c813668ad646788efd746a62c1b16e1438fad1bc4bcbebc961821b924f'}) $null}
foreach($kind in @('combat-rider-first','combat-mount-first','native-life-cleanup')){$lk=$kind;Check ('mounted6 seed refused for owner-seed '+$lk) {$v=New-KmcOwnerSeedDerivedCombatFixture $lk;$c=Get-KmcChunk7Mounted6Seed;$v.request.ownedPairs.seedId=$c.seedId;$v.request.ownedPairs.seedSha256=$c.sha256;Invoke-OwnedReader $v} $true 'only along its own lineage'}
# Windows PowerShell 5.1 serializes a PSObject-wrapped array as {value,Count}; restore the plain arrays after a JSON round trip.
function Repair-KmcJsonArrays($x){if($x-is[Array]){return ,@($x|ForEach-Object{Repair-KmcJsonArrays $_})};if($null-eq$x-or$x.PSObject.BaseObject-isnot[Management.Automation.PSCustomObject]){return $x};$n=@($x.PSObject.Properties.Name)
 if($n.Count-eq2-and$n-ccontains'value'-and$n-ccontains'Count'){return ,@(@($x.value)|ForEach-Object{Repair-KmcJsonArrays $_})}
 foreach($p in @($x.PSObject.Properties)){$x.($p.Name)=Repair-KmcJsonArrays $p.Value};$x}
# The mounted6 Manual producer: the owner-seed Manual fixture with rider A's pinned pre-owned Horse (verified, never built),
# the owner-made empty schema-3 archive and rider B's pending levels short of Ranger 4 (exact minimal XP to level 8).
function New-KmcMounted6ManualFixture{
 $v=New-KmcOwnerSeedManualFixture;$c=Get-KmcChunk7Mounted6Seed;$p=$v.request.ownedPairs;$oldA=$p.riderAId;$oldHorse=$v.artifact.observations.ownerSeedSetup.riders[0].horseId
 $json=$v|ConvertTo-Json -Depth 90
 foreach($pair in @(@($oldA,$c.riderAId),@($oldHorse,$c.horseId),@($oldA.Replace('-',''),$c.riderAId.Replace('-','')),@($oldHorse.Replace('-',''),$c.horseId.Replace('-','')),@('Manual_302_KMC_C7_OWNER_SEED.zks','Manual_308_KMC_C7_MOUNTED6_SEED.zks'),@('KMC_C7_OWNER_SEED',$c.seedId),@(('9'*64),$c.sha256),@('Manual_303_KMC_C7_SEED_WORLD','Manual_309_KMC_C7_SEED_WORLD'))){$json=$json.Replace($pair[0],$pair[1])}
 $v=Repair-KmcJsonArrays (ConvertFrom-Json $json);$r=$v.request;$o=$v.artifact.observations;$s=$r.ownedPairs;$u=$o.ownerSeedSetup;$a=$s.riderAId;$b=$s.riderBId
 # The seed is Manual 308, so the engine's next Manual number is 309.
 foreach($n in @($o.setupComplete.measuredOutputReservation,$o.ownedOutputAdmission,$o.ownedOutputAdmission.authority,$o.ownedOutputCompletion.authority)){$n.number=309}
 $s|Add-Member mountAId $c.horseId -Force;$s.partyIds=@(@($s.partyIds)+@($c.horseId))
 $o.partyBefore=@(@($o.partyBefore|ForEach-Object{if($_.id-ceq$a){$_.petId=$c.horseId};$_})+@([pscustomobject][ordered]@{id=$c.horseId;blueprint='4016c7db400ab721ff125aef9e65e202';petId=$null;masterId=$a}))
 $chars=@(@($s.partyIds|Where-Object{$_-cne$c.horseId})|Sort-Object -CaseSensitive);$u.economyBefore.characterIds=$chars;$o.setupComplete.economy.characterIds=$chars
 $u.disclosure=Get-KmcChunk7OwnerSeedPreOwnedManualDisclosure;$u.loadedArchiveKind='Current';$u.loadedArchiveCanRestore=$true
 $u|Add-Member ownerMadeEmptyArchive ([pscustomobject][ordered]@{schemaVersion=3;campaignId=$r.persistenceLoad.gameId;areaId=$r.persistenceLoad.area;pairs=0;participations=0;slots=0;combatPresent=$false;nativeRestorationGrantedByDiagnostic=$false}) -Force
 $u|Add-Member preOwnedRiderA ([pscustomobject][ordered]@{riderId=$a;horseId=$c.horseId;petIsKmcHorse=$true;petReciprocal=$true;petInField=$true;horseFeature=$true;horseRank=$true;characterLevel=6;rangerLevel=6;experience=35022;readyLevel=7;source='taken by the owner in ordinary play with KMC installed; verified only'}) -Force
 $fa=$u.riderFacts[0];$fa.petless=$false;$fa.characterLevel=6;$fa.rangerLevel=6;$fa.experience=35022;$fa.readyLevel=7;$fa.eligibleWithoutAssistance=$true;$fa.minimalTargetLevel=4
 $fb=$u.riderFacts[1];$fb.characterLevel=4;$fb.rangerLevel=0;$fb.experience=35022;$fb.readyLevel=7;$fb.eligibleWithoutAssistance=$false;$fb.minimalTargetLevel=8
 $ra=$u.riders[0];$ra.before=Prog 6 35022 6;$ra.after=Prog 6 35022 6;$ra.nativeCommits=0;$ra.nativeChoices=0;$ra.nativeAutoLevelupOverrides=0;$ra.presentation='owner-pre-owned'
 $u.riders[1].before=Prog 4 51000 0;$u.riders[1].after=Prog 8 51000 4
 $u.riderBExperience=[pscustomobject][ordered]@{assisted=$true;targetCharacterLevel=8;before=35022;after=51000;delta=15978;threshold=51000;characterLevelBefore=4;characterLevelAfter=4;levelReadyAfter=$true;readyLevelAfter=8}
 $u.nativeLevelUpAutosave.riderBuilds=1;$u.setupAutosaves=@($u.setupAutosaves[1]);$u.setupAutosaves[0].sequence=1 # the producer numbers autosave observations by observed builds (rider B is build 1)
 $items=@($u.assistance|Where-Object{-not($_.kind-ceq'native-ordinary-ranger-levels'-and$_.unitId-ceq$a)});foreach($item in $items){if($item.kind-ceq'native-xp'){$item.before=35022;$item.after=51000;$item.amount=15978}};$u.assistance=$items
 $o.setupComplete.assistanceCount=$items.Count;$o.ownerSeedManual.assistanceCount=$items.Count;$o.ownerSeedManual.twoOwners[0].presentation='owner-pre-owned'
 $v
}
function Check-Mounted6Manual([string]$Name,[scriptblock]$Mutation,[string]$Pattern=''){$v=New-KmcMounted6ManualFixture;&$Mutation $v;Check ('mounted6 Manual producer refuses '+$Name) {Assert-KmcChunk7WorldEvidence $v.request $v.artifact 'PASS'} $true $Pattern}
Check 'mounted6 Manual producer evidence accepted (pre-owned rider A, rider B built with pending levels)' {$v=New-KmcMounted6ManualFixture;Assert-KmcChunk7WorldEvidence $v.request $v.artifact 'PASS'}
Check-Mounted6Manual 'the Missing metadata path' {$args[0].artifact.observations.ownerSeedSetup.loadedArchiveKind='Missing'}
Check-Mounted6Manual 'a non-empty owner-made archive' {$args[0].artifact.observations.ownerSeedSetup.ownerMadeEmptyArchive.pairs=1}
Check-Mounted6Manual 'an archive of another campaign' {$args[0].artifact.observations.ownerSeedSetup.ownerMadeEmptyArchive.campaignId='00000000-0000-0000-0000-000000000009'}
Check-Mounted6Manual 'a missing pre-owned disclosure' {$args[0].artifact.observations.ownerSeedSetup.PSObject.Properties.Remove('preOwnedRiderA')}
Check-Mounted6Manual 'another pre-owned Horse' {$args[0].artifact.observations.ownerSeedSetup.preOwnedRiderA.horseId='7347ab0c-d284-44a1-8c5a-d3c322488404'}
Check-Mounted6Manual 'a pre-owned Horse that is not the KMC Horse' {$args[0].artifact.observations.ownerSeedSetup.preOwnedRiderA.petIsKmcHorse=$false}
Check-Mounted6Manual 'a pre-owned rider below Ranger 4' {$q=$args[0].artifact.observations.ownerSeedSetup.preOwnedRiderA;$q.rangerLevel=3}
Check-Mounted6Manual 'a built rider A (native commits)' {$args[0].artifact.observations.ownerSeedSetup.riders[0].nativeCommits=1}
Check-Mounted6Manual 'a changed rider A progression' {$args[0].artifact.observations.ownerSeedSetup.riders[0].after=Prog 7 35022 7}
Check-Mounted6Manual 'a rider A fixture-setup presentation' {$args[0].artifact.observations.ownerSeedSetup.riders[0].presentation='fixture-setup'}
Check-Mounted6Manual 'two rider builds' {$args[0].artifact.observations.ownerSeedSetup.nativeLevelUpAutosave.riderBuilds=2}
Check-Mounted6Manual 'a rider A build item' {$u=$args[0].artifact.observations.ownerSeedSetup;$u.assistance=@([pscustomobject][ordered]@{unitId=$args[0].request.ownedPairs.riderAId;kind='native-ordinary-ranger-levels';nativeApi='x';before=6;after=6;amount=0;detail='synthetic'})+@($u.assistance)}
Check-Mounted6Manual 'a petless rider A before setup' {foreach($row in @($args[0].artifact.observations.partyBefore)){if($row.id-ceq$args[0].request.ownedPairs.riderAId){$row.petId=$null}}}
Check-Mounted6Manual 'a selection without the pinned Horse' {$args[0].request.ownedPairs.PSObject.Properties.Remove('mountAId')}
Check-Mounted6Manual 'a selection pinning another Horse' {$args[0].request.ownedPairs.mountAId='7347ab0c-d284-44a1-8c5a-d3c322488404'}
Check-Mounted6Manual 'rider B XP beyond its minimal target' {$x=$args[0].artifact.observations.ownerSeedSetup.riderBExperience;$x.before=51000;$x.delta=0}
Check-Mounted6Manual 'a fixture-setup owner row for rider A' {$args[0].artifact.observations.ownerSeedManual.twoOwners[0].presentation='fixture-setup'}
Check 'the pre-owned shape is refused on any other owner seed' {$v=New-KmcMounted6ManualFixture;$v.request.ownedPairs.seedId='KMC_C7_NO_MODS_OWNER_SEED';$v.request.ownedPairs.seedSha256='3405d7c813668ad646788efd746a62c1b16e1438fad1bc4bcbebc961821b924f';Assert-KmcChunk7OwnedPairsSelection $v.request} $true 'owner-seed two-pair selection'
Check 'the original owner-seed Manual producer still refuses a carried Horse pin' {$v=New-KmcOwnerSeedManualFixture;$v.request.ownedPairs|Add-Member mountAId '6eb039a1-d500-405e-987c-5a823ee41626';Assert-KmcChunk7OwnedPairsSelection $v.request} $true 'owner-seed two-pair selection'
# Mounted6 attack modes (C03 BAB variation): BAB 6, the seed's saved ranged hand set selected out of combat, an iterative
# Rapid Shot off full attack. Other seeds carry no mounted6 block.
function New-KmcMounted6AttackModesFixture([bool]$Bab6=$true){$c=Get-KmcChunk7Mounted6Seed;$json=(New-KmcSeedAttackModesFixture $Bab6)|ConvertTo-Json -Depth 90
 $v=Repair-KmcJsonArrays (ConvertFrom-Json $json.Replace('KMC_C7_OWNER_SEED',$c.seedId).Replace(('9'*64),$c.sha256))
 $hs={param($set,$bp)[pscustomobject][ordered]@{handSet=$set;primaryBlueprint=$bp;firstWeapon=$bp;firstWeaponRanged=($bp-ceq$c.rangedWeapon);inCombat=$false}}
 $v.artifact.observations.attackModes|Add-Member mounted6 ([pscustomobject][ordered]@{baseAttackBonus=6;before=(&$hs 1 $c.meleeWeapon);savedSets=(New-KmcMounted6SavedSets 1);switched=$true;nativeApi='Kingmaker.Items.UnitBody.CurrentHandEquipmentSetIndex (public setter, out of combat)';after=(&$hs 0 $c.rangedWeapon)}) -Force;$v}
# b207 m5's native saved hand sets (current set 1): the inactive ranged set 0 stores the ranged weapon (MaybeItem) while its
# native MaybeWeapon answers the empty-hand weapon; only the current set's slot is Active; sets 2 and 3 are empty.
$script:m6EmptyHand='118fdd03e569a66459ab01a20af6811a'
function New-KmcMounted6SavedSets([int]$Current){$c=Get-KmcChunk7Mounted6Seed;$items=@($c.rangedWeapon,$c.meleeWeapon,$null,$null)
 $rows=@(0..3|ForEach-Object{$item=$items[$_];$active=$_-eq$Current;[pscustomobject][ordered]@{index=$_;hasItem=($null-ne$item);active=$active;disabled=$false;item=$item;weapon=$item;weaponRanged=($item-ceq$c.rangedWeapon);maybeWeapon=$(if($active-and$null-ne$item){$item}else{$script:m6EmptyHand})}})
 [pscustomobject][ordered]@{count=4;current=$Current;polymorphed=$false;emptyHandWeapon=$script:m6EmptyHand;sets=$rows}}
function Check-Mounted6Attack([string]$Name,[scriptblock]$Mutation,[string]$Pattern){$v=New-KmcMounted6AttackModesFixture;&$Mutation $v;Check ('mounted6 attack modes refuses '+$Name) {Invoke-OwnedReader $v} $true $Pattern}
Check 'mounted6 attack modes accepted (BAB 6, saved ranged set selected, iterative Rapid Shot off full attack)' {Invoke-OwnedReader (New-KmcMounted6AttackModesFixture)}
Check 'mounted6 attack modes accepted with the ranged set already active' {$v=New-KmcMounted6AttackModesFixture;$m=$v.artifact.observations.attackModes.mounted6;$m.switched=$false;$m.before=Copy-Value $m.after;$m.savedSets=New-KmcMounted6SavedSets 0;Invoke-OwnedReader $v}
Check-Mounted6Attack 'a missing saved hand-set record' {$args[0].artifact.observations.attackModes.mounted6.PSObject.Properties.Remove('savedSets')} 'mounted6 attack modes selection'
Check-Mounted6Attack 'a saved hand-set count below two' {$s=$args[0].artifact.observations.attackModes.mounted6.savedSets;$s.count=1;$s.sets=@($s.sets[0])} 'Integer|integer|range'
Check-Mounted6Attack 'a saved hand-set record missing a set' {$s=$args[0].artifact.observations.attackModes.mounted6.savedSets;$s.sets=@($s.sets[0..2])} 'every set'
Check-Mounted6Attack 'a saved current set other than the one before the switch' {$args[0].artifact.observations.attackModes.mounted6.savedSets.current=0} 'selected before the switch'
Check-Mounted6Attack 'an empty saved ranged set' {$r=$args[0].artifact.observations.attackModes.mounted6.savedSets.sets[0];$r.hasItem=$false;$r.item=$null;$r.weapon=$null;$r.weaponRanged=$false} 'ranged weapon'
Check-Mounted6Attack 'a saved ranged-set item that is not ranged' {$args[0].artifact.observations.attackModes.mounted6.savedSets.sets[0].weaponRanged=$false} 'ranged weapon'
Check-Mounted6Attack 'another saved longsword-set item' {$r=$args[0].artifact.observations.attackModes.mounted6.savedSets.sets[1];$r.item='35dfad6517f401145af54111be04d6cf';$r.weapon=$r.item;$r.maybeWeapon=$r.item} 'longsword'
Check-Mounted6Attack 'an inactive set answering its stored weapon' {$args[0].artifact.observations.attackModes.mounted6.savedSets.sets[0].maybeWeapon=(Get-KmcChunk7Mounted6Seed).rangedWeapon} 'empty-hand weapon'
Check-Mounted6Attack 'two active saved sets' {$args[0].artifact.observations.attackModes.mounted6.savedSets.sets[0].active=$true} 'current-set rule'
Check-Mounted6Attack 'a polymorphed rider' {$args[0].artifact.observations.attackModes.mounted6.savedSets.polymorphed=$true} 'boolean|Boolean'
Check-Mounted6Attack 'disagreeing saved item clauses' {$args[0].artifact.observations.attackModes.mounted6.savedSets.sets[2].hasItem=$true} 'item clauses disagree'
# The C# attack-modes admission reads an inactive set's saved weapon through ItemSlot.MaybeItem: Kingmaker 2.1.7b's
# WeaponSlot.MaybeWeapon answers the empty-hand weapon for an inactive slot (b207 m5's false refusal).
Check 'mounted6 attack modes admission reads saved hand sets through the stored item' {$cs=[IO.File]::ReadAllText((Join-Path $script:m6repo 'src\KingmakerMountedCombat\Diagnostics\Chunk7CombatScenario.AttackModes.cs'),[Text.Encoding]::UTF8)
 if($cs-match'HandsEquipmentSets\[[^\]]+\]\.PrimaryHand\.MaybeWeapon'-or$cs-match'sets\[[^\]]+\]\.PrimaryHand\.MaybeWeapon'){throw 'An inactive hand set is read through WeaponSlot.MaybeWeapon.'}
 if($cs-notmatch'HandsEquipmentSets\[index\]\.PrimaryHand\.MaybeItem as ItemEntityWeapon'-or$cs-notmatch'Chunk7OwnedPairsSelection\.Mounted6HandSetsAdmitted\('){throw 'The mounted6 hand-set admission no longer reads the stored item through the shared predicate.'}}
Check 'mounted6 prepared casting maps to the unchanged RT casting engine' {$pv=New-KmcMounted6AttackModesFixture;$pv.request.ownedPairs.case='owner-seed-prepared-casting';if((Get-KmcChunk7SelectedScenario $pv.request)-cne'chunk7-two-pair-realtime-casting'){throw 'Prepared casting scenario mapping differs.'}}
Check 'BAB 6 attack modes on the original seed accepted without a mounted6 block' {Invoke-OwnedReader (New-KmcSeedAttackModesFixture $true)}
Check-Mounted6Attack 'a missing mounted6 record' {$args[0].artifact.observations.attackModes.PSObject.Properties.Remove('mounted6')} 'lack their BAB'
Check-Mounted6Attack 'BAB 5' {$args[0].artifact.observations.attackModes.mounted6.baseAttackBonus=5} 'Integer|integer|range'
Check-Mounted6Attack 'a switch from another hand set' {$args[0].artifact.observations.attackModes.mounted6.before.handSet=2} 'saved longsword set'
Check-Mounted6Attack 'a skipped needed switch' {$args[0].artifact.observations.attackModes.mounted6.switched=$false} 'skipped a needed hand-set selection'
Check-Mounted6Attack 'another weapon after the switch' {$m=$args[0].artifact.observations.attackModes.mounted6;$m.after.primaryBlueprint='35dfad6517f401145af54111be04d6cf'} 'saved ranged weapon'
Check-Mounted6Attack 'a selection made in combat' {$args[0].artifact.observations.attackModes.mounted6.after.inCombat=$true} 'boolean|Boolean'
Check 'mounted6 attack modes refuses a single Rapid Shot off attack (BAB below 6 shape)' {Invoke-OwnedReader (New-KmcMounted6AttackModesFixture $false)} $true 'iterative second ranged attack'
Check 'attack modes on another seed refuse a mounted6 block' {$v=New-KmcSeedAttackModesFixture $true;$v.artifact.observations.attackModes|Add-Member mounted6 ([pscustomobject]@{baseAttackBonus=6}) -Force;Invoke-OwnedReader $v} $true 'Only the mounted6 lineage'
# Mounted6 prepared casting: rider A is the one caster of its one memorized prepared healing slot.
function New-KmcPreparedCastObservation([string]$Caster,[int]$Slots=1){$ids=@('a1','m1','b1','m2')
 [pscustomobject]@{casting=[pscustomobject]@{casterId=$Caster};castingInventory=@([pscustomobject]@{actorId='a1';preparedSpells=@(1..$Slots|ForEach-Object{[pscustomobject]@{blueprint='5590652e1c2225c4ca30c4a699ab3649';available=$true}})+@([pscustomobject]@{blueprint='3e9d1119d43d07c4c8ba9ebfd1671952';available=$true})},[pscustomobject]@{actorId='b1';preparedSpells=@()})}}
$script:pcReq=[pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';ownedPairs=[pscustomobject]@{case='owner-seed-prepared-casting'}}
Check 'mounted6 prepared casting by rider A accepted' {Assert-KmcChunk7OwnerSeedPreparedCaster $script:pcReq (New-KmcPreparedCastObservation 'a1') @('a1','m1','b1','m2')}
Check 'mounted6 prepared casting refuses rider B as caster' {Assert-KmcChunk7OwnerSeedPreparedCaster $script:pcReq (New-KmcPreparedCastObservation 'b1') @('a1','m1','b1','m2')} $true 'not cast by rider A'
Check 'mounted6 prepared casting refuses two prepared healing slots on rider A' {Assert-KmcChunk7OwnerSeedPreparedCaster $script:pcReq (New-KmcPreparedCastObservation 'a1' 2) @('a1','m1','b1','m2')} $true 'exactly one memorized'
Check 'mounted6 prepared casting refuses the healing spell also on rider B' {$x=New-KmcPreparedCastObservation 'a1';$x.castingInventory[1].preparedSpells=@([pscustomobject]@{blueprint='5590652e1c2225c4ca30c4a699ab3649';available=$true});Assert-KmcChunk7OwnerSeedPreparedCaster $script:pcReq $x @('a1','m1','b1','m2')} $true 'on another rider'
Check 'the generic acquired casting case keeps its either-rider rule' {Assert-KmcChunk7OwnerSeedPreparedCaster ([pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';ownedPairs=[pscustomobject]@{case='realtime-casting'}}) (New-KmcPreparedCastObservation 'b1') @('a1','m1','b1','m2')}
Check 'generic acquired RT Charge evidence accepted with a melee weapon' {Invoke-OwnedReader (Copy-Value @($fixtures|Where-Object name -CEQ 'realtime-charge')[0])}
Check-SeedCharge 'the original owner seed' {foreach($x in @($args[0].request.ownedPairs)){$x.seedId='KMC_C7_NO_MODS_OWNER_SEED';$x.seedSha256='3405d7c813668ad646788efd746a62c1b16e1438fad1bc4bcbebc961821b924f'}} 'exactly the registered KMC_C7_CHARGE_SEED'
Check-SeedCharge 'the charge seed id with another hash' {$args[0].request.ownedPairs.seedSha256=('9'*64)} 'exactly the registered KMC_C7_CHARGE_SEED'
Check-SeedCharge 'a ranged primary weapon' {foreach($k in 'before','after'){$s=$args[0].artifact.observations.chargeWeapon.$k;$s.melee=$false;$s.ranged=$true}} 'Native evidence boolean'
Check-SeedCharge 'another active hand set' {foreach($k in 'before','after'){$args[0].artifact.observations.chargeWeapon.$k.handSet=0}} 'saved hand set and weapon'
Check-SeedCharge 'another primary weapon' {foreach($k in 'before','after'){$args[0].artifact.observations.chargeWeapon.$k.primaryBlueprint='4de4658c0e9b0d146b9a08ed6f030f8a'}} 'saved hand set and weapon'
Check-SeedCharge 'a weapon switched during the case' {$args[0].artifact.observations.chargeWeapon.after.handSet=0} 'changed during the Charge case'
Check-SeedCharge 'a missing weapon record' {$args[0].artifact.observations.PSObject.Properties.Remove('chargeWeapon')} 'observed equipped weapon'
Check-SeedCharge 'a primary that is not the native first weapon' {$args[0].artifact.observations.chargeWeapon.before.firstWeaponIsPrimary=$false} 'Native evidence boolean'
Check-SeedCharge 'a missing target placement' {$args[0].artifact.observations.PSObject.Properties.Remove('chargeTargetPlacement')} 'recorded target placement'
Check-SeedCharge 'a target authorization at another point than the placement' {$args[0].artifact.observations.targetAuthorization.position=@(1.0,2.0,3.0)} 'did not place the selected candidate'
Check-SeedCharge 'an extra weapon field' {$args[0].artifact.observations.chargeWeapon.before|Add-Member extra 1} 'charge weapon state'
$script:chargeRepo=(Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
Check 'the charge seed constants agree across the C# selection, the PS reader and the registry' {$c=Get-KmcChunk7ChargeSeed;$cs=[IO.File]::ReadAllText((Join-Path $script:chargeRepo 'src\KingmakerMountedCombat\Diagnostics\Chunk7OwnedPairsSelection.cs'))
 $expected=[ordered]@{ChargeSeedId=('"'+$c.seedId+'"');ChargeSeedSha256=('"'+$c.sha256+'"');ChargeSeedHandSetIndex=[string]$c.handSet;ChargeSeedPrimaryWeapon=('"'+$c.primaryWeapon+'"')}
 foreach($name in @($expected.Keys)){if($cs-notmatch([regex]::Escape($name)+'\s*=\s*'+[regex]::Escape($expected[$name])+';')){throw ('C# constant differs: '+$name)}}
 $e=@(((Get-Content -LiteralPath (Join-Path $script:chargeRepo 'scripts\runtime\Chunk7OwnerSeedRegistry.json') -Raw)|ConvertFrom-Json).seeds|Where-Object seedId -CEQ $c.seedId);if($e.Count-ne1-or$e[0].sha256-cne$c.sha256){throw 'Registry charge seed entry differs.'}}
foreach($kind in @('combat-rider-first','combat-mount-first','realtime-core')){
 Check ('owner-seed '+$kind+' refuses the charge seed') {$v=New-KmcOwnerSeedDerivedCombatFixture $kind;$c=Get-KmcChunk7ChargeSeed;$v.request.ownedPairs.seedId=$c.seedId;$v.request.ownedPairs.seedSha256=$c.sha256;Invoke-OwnedReader $v} $true 'only along the owner-seed real-time Charge lineage'}
Check 'owner-seed selections admit the charge seed only along its lineage' {$c=Get-KmcChunk7ChargeSeed
 Assert-KmcChunk7ChargeSeedBinding ([pscustomobject]@{case='owner-seed-persistence-save';seedId=$c.seedId;seedSha256=$c.sha256}) 'multi-exploration-manual'
 Assert-KmcChunk7ChargeSeedBinding ([pscustomobject]@{case='owner-seed-persistence-load';seedId=$c.seedId;seedSha256=$c.sha256}) 'multi-exploration-manual'
 Assert-KmcChunk7ChargeSeedBinding ([pscustomobject]@{case='owner-seed-realtime-charge';seedId=$c.seedId;seedSha256=$c.sha256}) $null
 foreach($bad in @(@('owner-seed-exploration',$null),@('owner-seed-persistence-load','multi-exploration-three-loads-manual'),@('owner-seed-checkpoint-save','multi-exploration-quick'),@('owner-seed-staged',$null),@('owner-seed-load-refusal','multi-malformed-refusal-manual'))){
  $threw=$false;try{Assert-KmcChunk7ChargeSeedBinding ([pscustomobject]@{case=$bad[0];seedId=$c.seedId;seedSha256=$c.sha256}) $bad[1]}catch{$threw=$_.Exception.Message-like'*only along*'};if(-not$threw){throw ('Charge seed admitted for '+$bad[0])}}
 $threw=$false;try{Assert-KmcChunk7ChargeSeedBinding ([pscustomobject]@{case='owner-seed-persistence-save';seedId='KMC_C7_NO_MODS_OWNER_SEED';seedSha256=$c.sha256}) 'multi-exploration-manual'}catch{$threw=$true};if(-not$threw){throw 'A charge hash under another seed id was admitted.'}
 Assert-KmcChunk7ChargeSeedBinding ([pscustomobject]@{case='owner-seed-staged';seedId='KMC_C7_NO_MODS_OWNER_SEED';seedSha256='3405d7c813668ad646788efd746a62c1b16e1438fad1bc4bcbebc961821b924f'}) $null}
Check 'attack modes refuse a carried reaction record' {$v=New-KmcSeedAttackModesFixture;$v.artifact.observations|Add-Member reaction ([pscustomobject]@{}) -Force;Invoke-OwnedReader $v} $true 'cannot carry another combat case'
Check-SeedReaction 'no attack of opportunity' {$d=Get-RxLeg $args[0];$d.rules=@();$args[0].artifact.observations.reaction.rules.events=@()} 'other than exactly one attack'
Check-SeedReaction 'two attacks of opportunity' {$d=Get-RxLeg $args[0];$e=Copy-Value $d.rules[0];$e.sequence=2;$d.rules=@($d.rules)+@($e);$args[0].artifact.observations.reaction.rules.events=@($d.rules)} 'other than exactly one attack'
Check-SeedReaction 'an ordinary attack instead of an attack of opportunity' {$d=Get-RxLeg $args[0];$d.rules[0].opportunity=$false} 'not the target''s native attack of opportunity'
Check-SeedReaction 'an attack of opportunity by the pair' {$d=Get-RxLeg $args[0];$d.rules[0].actor=$args[0].request.ownedPairs.mountAId;$d.rules[0].target=$args[0].artifact.observations.targetId} 'not the target''s native attack of opportunity'
Check-SeedReaction 'an attack of opportunity on pair B' {$d=Get-RxLeg $args[0];$d.rules[0].target=$args[0].request.ownedPairs.riderBId} 'not the target''s native attack of opportunity'
Check-SeedReaction 'an attack outside the reaction windows' {$r=$args[0].artifact.observations.reaction.rules;$e=Copy-Value $r.events[0];$e.sequence=2;$r.events=@($r.events)+@($e)} 'outside the reaction windows'
Check-SeedReaction 'a rule window that is not consecutive' {(Get-RxLeg $args[0]).rules[0].sequence=3} 'consecutive record'
Check-SeedReaction 'a target counter that did not drop' {(Get-RxLeg $args[0]).targetAfter.reactions=1} 'did not drop by exactly one'
Check-SeedReaction 'a target with two reactions per round' {$args[0].artifact.observations.reaction.precondition.reactionsPerRound=2} 'exactly one available native attack of opportunity'
Check-SeedReaction 'a target unable to make attacks of opportunity' {$args[0].artifact.observations.reaction.precondition.canAttackOfOpportunity=$false} 'exactly one available native attack of opportunity'
Check-SeedReaction 'a start outside the threatened area without an approach' {$p=$args[0].artifact.observations.reaction.precondition;$p.engagesRider=$false;$p.engagesMount=$false} 'no native approach ran'
Check-SeedReaction 'a leave that never was threatened' {$t=(Get-RxLeg $args[0]).targetBefore;$t.engagesRider=$false;$t.engagesMount=$false} 'from inside the target''s threatened area'
Check-SeedReaction 'a pair still threatened after its move' {(Get-RxLeg $args[0]).targetAfter.engagesMount=$true} 'did not leave the target''s threatened area'
Check-SeedReaction 'a move short of its clear distance' {(Get-RxLeg $args[0]).targetAfter.mountDistance=5.0} 'clear distance'
Check-SeedReaction 'a five-foot step' {(Get-RxLeg $args[0]).fiveFootStep=$true} 'Native evidence boolean'
Check-SeedReaction 'a move without displacement' {(Get-RxLeg $args[0]).distance=0.2} 'completed native ground move'
Check-SeedReaction 'a failed move command' {(Get-RxLeg $args[0]).result='Fail'} 'completed native ground move'
Check-SeedReaction 'a move outside rider A''s turn' {(Get-RxLeg $args[0]).turnActor=$args[0].request.ownedPairs.riderBId} 'left rider A'
Check-SeedReaction 'a move in another round' {(Get-RxLeg $args[0]).round=11} 'outside its native round'
Check-SeedReaction 'a move not charged to the mount' {$d=Get-RxLeg $args[0];(RActor $d.after $args[0].request.ownedPairs.mountAId).move=0.0} 'not charged to mount A'
Check-SeedReaction 'a rider charged for the pair''s move' {$d=Get-RxLeg $args[0];(RActor $d.after $args[0].request.ownedPairs.riderAId).move=1.5} 'Rider A spent its own action'
Check-SeedReaction 'a mount Standard spent on the move' {$d=Get-RxLeg $args[0];(RActor $d.after $args[0].request.ownedPairs.mountAId).standard=6.0} 'non-movement action'
Check-SeedReaction 'pair B spending during the reaction' {$d=Get-RxLeg $args[0];(RActor $d.after $args[0].request.ownedPairs.riderBId).reactions=0} 'Unrelated native actor expenditure changed'
Check-SeedReaction 'a target that spent an action' {(Get-RxLeg $args[0]).targetAfter.standard=6.0} 'spent an action inside'
Check-SeedReaction 'a stale pair command' {(Get-RxLeg $args[0]).residue.activeCommand=$true} 'Native evidence boolean'
Check-SeedReaction 'an unconscious victim' {(Get-RxLeg $args[0]).residue.mountConscious=$false} 'Native evidence boolean'
Check-SeedReaction 'a pair dismounted by the reaction' {(Get-RxLeg $args[0]).residue.state='Unmounted'} 'did not stay mounted'
Check-SeedReaction 'a prone target in the window' {(Get-RxLeg $args[0]).targetBefore.prone=$true} 'prone or unconscious'
Check-SeedReaction 'a prone half claimed covered' {$args[0].artifact.observations.reaction.proneStandUp.covered=$true} 'recorded as uncovered'
Check-SeedReaction 'a prone half without its reason' {$args[0].artifact.observations.reaction.proneStandUp.reason='skipped'} 'recorded as uncovered'
Check-SeedReaction 'a dropped rule observation' {$args[0].artifact.observations.reaction.rules.dropped=1} 'dropped or failed'
Check-SeedReaction 'a missing rule observation' {$args[0].artifact.observations.reaction.PSObject.Properties.Remove('rules')} 'dropped or failed'
Check-SeedReaction 'an incomplete run' {$args[0].artifact.observations.reaction.complete=$false} 'Native evidence boolean'
Check-SeedReaction 'a relationship input' {$o=$args[0].artifact.observations;$o.inputs=@([pscustomobject]@{label='stray-dismount-a'})} 'no relationship input'
Check-SeedReaction 'a foreign target' {$args[0].artifact.observations.reaction.targetId=$args[0].request.ownedPairs.riderBId} 'exact pair A and diagnostic target'
Check-SeedReaction 'an approach that drew an attack' {$v=$args[0];$n=New-KmcSeedReactionFixture $true;$v.artifact.observations.reaction=$n.artifact.observations.reaction;$x=$v.artifact.observations.reaction;$x.approach.rules=@(Copy-Value $x.disengage.rules[0]);$x.disengage.ruleCountBefore=1;$x.disengage.rules[0].sequence=2;$e=Copy-Value $x.disengage.rules[0];$x.rules.events=@($x.approach.rules[0],$e)} 'approach drew an attack'
Check-SeedReaction 'an approach although already threatened' {$v=$args[0];$n=New-KmcSeedReactionFixture $true;$v.artifact.observations.reaction=$n.artifact.observations.reaction;$v.artifact.observations.reaction.precondition.engagesMount=$true} 'already threatened'
# 7E X1 F04 staged moving actions: C6D-move-cast-move (round 10), C6D-double-move-ranged (11, its scroll
# control in 12) and the ranged approach row (12, approach shot in 13). Synthetic geometry: the target
# at the origin, envelope 0.9 + 0.7 + 15.24 = 16.84 m, the pair starting 8 m away on the x axis. The up-front geometry
# plan and its in-combat confirmation are the C# planner's own record (tests/Chunk7Diagnostics/StagedGeometryPlanFixture.json,
# regenerated and compared by Test-Chunk7StagedGeometry.ps1): the ninth placement (the origin) is selected.
function New-KmcSeedStagedFixture{
 $v=New-KmcOwnerSeedDerivedCombatFixture 'combat-rider-first';$r=$v.request;$o=$v.artifact.observations;$roles=@(Get-KmcChunk7OwnedPairIds $r)
 $r.ownedPairs.case='owner-seed-staged'
 $sp=Get-Content -LiteralPath (Join-Path $repo 'tests/Chunk7Diagnostics/StagedGeometryPlanFixture.json') -Raw|ConvertFrom-Json
 $script:stEnvelope=[double]$sp.confirmation.envelope;$script:stSpeed=[double]$sp.confirmation.speed
 $o.targetAuthorization.position=@($sp.plan.candidates[[int]$sp.plan.selected].goal|ForEach-Object{[double]$_});$o.targetAuthorization.riderPosition=@($sp.plan.riderStart|ForEach-Object{[double]$_})
 foreach($key in @('r1','firstActionMount','localFailure','registeredDisable','removalRefusal','pendingSurvivor')){if($null-ne$o.PSObject.Properties[$key]){$o.PSObject.Properties.Remove($key)}}
 $o.activations=@();$o.inputs=@()
 $weapon='4de4658c0e9b0d146b9a08ed6f030f8a';$clw='5590652e1c2225c4ca30c4a699ab3649';$scrollBp='cd635d5720937b044a354dba17abad8d';$scrollItem=-4401
 $trace=$o.allocationTrace;$script:stSeq=[long](@($trace.events)|Measure-Object -Property sequence -Maximum).Maximum
 $ev={param([string]$boundary,[string]$actor,[long]$command,[double]$standard,[string]$commandActor=$null,[string]$commandType=$null)$script:stSeq++
  $trace.events=@($trace.events)+@([pscustomobject][ordered]@{sequence=$script:stSeq;boundary=$boundary;frame=1;round=$script:stRound;turn=$script:stTurn;currentActor=$roles[0];turnStatus='Acting';command=$command;commandActor=$commandActor;commandType=$commandType;state=[pscustomobject][ordered]@{actor=$actor;standard=$standard}})}
 $item={param([long]$count)@([pscustomobject][ordered]@{item=$scrollItem;blueprint=$scrollBp;count=$count;exactSlot=$true})}
 $st={param([string]$label,[long]$round,[long]$turn,[double]$rStd,[double]$mMove,[double[]]$pos,[bool]$used2,[long]$count)
  $x=Copy-Value $o.snapshots[0];$x.label=$label;$x.round=$round;$x|Add-Member currentActor $roles[0] -Force
  foreach($a in $x.actors){$a.standard=0.0;$a.move=0.0;$a.swift=0.0;if($a.id-ceq$roles[0]){$a.standard=$rStd};if($a.id-ceq$roles[1]){$a.move=$mMove;$a|Add-Member native ([pscustomobject]@{remainingNativeTime=(6.0-$mMove);speedMps=$script:stSpeed}) -Force}}
  $p=@([double]$pos[0],0.0,[double]$pos[1]);$d=[Math]::Sqrt($pos[0]*$pos[0]+$pos[1]*$pos[1])
  foreach($pair in @(@('turnStatus','Acting'),@('turn',$turn),@('relationship','Mounted'),@('mountUsedOneMove',($mMove-gt0)),@('mountUsedTwoMove',$used2),@('mountHasMove',($mMove-lt5.95)),@('riderHasStandard',($rStd-le0.001)),
   @('riderCommandsEmpty',$true),@('mountCommandsEmpty',$true),@('activePairCommand',$false),@('pairMovement',$false),@('processesSettled',$true),@('nativeAbilitiesPending',$false),@('projectilesPending',$false),
   @('riderPosition',$p),@('mountPosition',$p),@('targetDistance',$d),@('riderEngaged',$false),@('mountEngaged',$false),@('items',(&$item $count)))){$x|Add-Member $pair[0] $pair[1] -Force}
  $x}
 $carrier={param([long]$id,[bool]$finished)[pscustomobject][ordered]@{identity=$id;type='UnitMoveTo';executor=$roles[1];createdByPlayer=$true;started=$true;acted=$true;finished=$finished;result='Success';commandType='Move';timeSinceStart=1.0}}
 $leg={param([int]$index,[long]$round,[long]$turn,[double]$moveFrom,[double]$moveTo,[double[]]$from,[double[]]$to,[bool]$used2,[long]$count,[long]$id,[bool]$refused=$false)
  $b=$script:stSeq;if(-not$refused){&$ev 'admission-after' $roles[1] $id 0.0 $roles[1] 'Kingmaker.UnitLogic.Commands.UnitMoveTo'}
  [pscustomobject][ordered]@{index=$index;destination=@($to[0],0.0,$to[1]);plannedDistance=1.0;proposals=@();before=(&$st 'leg-before' $round $turn 0.0 $moveFrom $from $false $count);traceSequenceBefore=$b;castEventsBefore=0
   fiveFootStep=$false;singleActionMove=$false;inputPath=$(if($refused){'ground-handler'}else{'pointer'});cursorCycles=0;clicked=$true;carrier=$(if($refused){$null}else{&$carrier $id $false});riderMoveSlot=$false
   afterInput=(&$st 'leg-after-input' $round $turn 0.0 $moveFrom $from $false $count);after=(&$st 'leg-after' $round $turn 0.0 $moveTo $to $used2 $count);terminal=$(if($refused){$null}else{&$carrier $id $true});traceSequenceAfter=$script:stSeq;castEvents=@();displacement=1.0}}
 $steps=[Collections.Generic.List[object]]::new()
 $moveStep={param([int]$index,[string]$row,[string]$kind,[long]$round,$legs)$f=@($legs)[0];$l=@($legs)[-1]
  $steps.Add([pscustomobject][ordered]@{index=$index;row=$row;kind=$kind;round=$round;before=(Copy-Value $f.before);traceSequenceBefore=$f.traceSequenceBefore;castEventsBefore=0;ruleCountBefore=0;legs=@($legs)
   after=(Copy-Value $l.after);traceSequenceAfter=$l.traceSequenceAfter;castEvents=@();rules=@();endDistance=$l.after.targetDistance})}
 $castStep={param([int]$index,[string]$row,[long]$round,[long]$turn,[double]$mMove,[double[]]$pos,[bool]$used2,[long]$count,[long]$shell)
  $b=$script:stSeq;&$ev 'cost-before' $roles[0] $shell 0.0;&$ev 'cost-after' $roles[0] $shell 6.0
  $ability=[pscustomobject][ordered]@{identity=900+$shell;blueprint=$clw;caster=$roles[0];sourceItem=$scrollItem;sourceItemBlueprint=$scrollBp;runtimeActionType='Standard';available=$true;availableForCast=$true}
  # The native self-targeted scroll shape (b204): the sourced scroll cast and its spends, then the sticky-touch
  # RuleCastSpell of the sourceless Cure Light Wounds touch ability on rider A and its heal (0 at full health).
  $inner=[pscustomobject][ordered]@{identity=950+$shell;blueprint='47808d23c67033d4bbab86a1070fd62f';caster=$roles[0];sourceItem=0;sourceItemBlueprint=$null;runtimeActionType='Standard';available=$true;availableForCast=$true}
  $casts=@([pscustomobject][ordered]@{kind='action-before';actor=$roles[0];identity=$shell;shell=[pscustomobject]@{timeSinceStart=0.55}},
   [pscustomobject][ordered]@{kind='cast-before';actor=$roles[0];identity=2;ability=$ability;target=$roles[0]},
   [pscustomobject][ordered]@{kind='cast-after';actor=$roles[0];identity=2;ability=$ability;target=$roles[0];process=77;spellFailed=$false;arcaneFailed=$false},
   [pscustomobject][ordered]@{kind='spell-spend-before';actor=$roles[0];identity=$ability.identity;ability=$ability},
   [pscustomobject][ordered]@{kind='item-spend-before';actor=$roles[0];identity=$scrollItem;blueprint=$scrollBp;count=$count;charges=1;result=$null},
   [pscustomobject][ordered]@{kind='item-spend-after';actor=$roles[0];identity=$scrollItem;blueprint=$scrollBp;count=($count-1);charges=1;result=$true},
   [pscustomobject][ordered]@{kind='spell-spend-after';actor=$roles[0];identity=$ability.identity;ability=$ability},
   [pscustomobject][ordered]@{kind='cast-before';actor=$roles[0];identity=3;ability=$inner;target=$roles[0]},
   [pscustomobject][ordered]@{kind='cast-after';actor=$roles[0];identity=3;ability=$inner;target=$roles[0];process=78;spellFailed=$false;arcaneFailed=$false},
   [pscustomobject][ordered]@{kind='heal';actor=$roles[0];identity=4;target=$roles[0];value=0})
  $castBefore=&$st 'cast-before' $round $turn 0.0 $mMove $pos $used2 $count;$riderBefore=@($castBefore.actors|Where-Object id -CEQ $roles[0])[0]
  if($null-ne$riderBefore.PSObject.Properties['native']-and$null-ne$riderBefore.native){$riderBefore.native|Add-Member damage 0 -Force}else{$riderBefore|Add-Member native ([pscustomobject]@{damage=0}) -Force}
  $steps.Add([pscustomobject][ordered]@{index=$index;row=$row;kind='cast-standard-scroll';round=$round;before=$castBefore;traceSequenceBefore=$b;castEventsBefore=0;ruleCountBefore=0
   ability=$ability;target=$roles[0];selected=$true;canTarget=$true;clicked=$true;admittedShellCount=1;shell=[pscustomobject]@{identity=1};costShell=$shell;afterInput=(&$st 'cast-after-input' $round $turn 0.0 $mMove $pos $used2 $count)
   after=(&$st 'cast-after' $round $turn 6.0 $mMove $pos $used2 ($count-1));traceSequenceAfter=$script:stSeq;castEvents=$casts;rules=@();terminal=[pscustomobject][ordered]@{identity=$shell;type='UnitUseAbility';executor=$roles[0];createdByPlayer=$true;started=$true;acted=$true;finished=$true;result='Success';commandType='Standard';timeSinceStart=0.6};endDistance=8.0})}
 $shotStep={param([int]$index,[string]$row,[string]$kind,[long]$round,[long]$turn,[double]$moveFrom,[double]$moveTo,[double[]]$from,[double[]]$to,[bool]$used2,[long]$count,[long]$id)
  $b=$script:stSeq;if($kind-ceq'attack-ranged-approach'){&$ev 'admission-after' $roles[1] 990 0.0 $roles[1] 'Kingmaker.UnitLogic.Commands.UnitMoveTo'};&$ev 'cost-before' $roles[0] $id 0.0;&$ev 'cost-after' $roles[0] $id 6.0
  $cmd={param([bool]$done)[pscustomobject][ordered]@{identity=$id;type='MountedPairAttackCommand';executor=$roles[0];createdByPlayer=$true;started=$done;acted=$done;finished=$done;result=$(if($done){'Success'}else{'None'});commandType='Standard';timeSinceStart=0.0}}
  $before=&$st 'shot-before' $round $turn 0.0 $moveFrom $from $used2 $count;$after=&$st 'shot-after' $round $turn 6.0 $moveTo $to $used2 $count
  $steps.Add([pscustomobject][ordered]@{index=$index;row=$row;kind=$kind;round=$round;before=$before;traceSequenceBefore=$b;castEventsBefore=0;ruleCountBefore=$index;envelope=$script:stEnvelope;startDistance=$before.targetDistance
   fullAttackMode=$false;cursorCycles=0;clicked=$true;admitted=(&$cmd $false);rejectionCodes=@();rejectionFeedback='Mounted pair command accepted: RiderRanged.';afterInput=(Copy-Value $before);after=$after;traceSequenceAfter=$script:stSeq;castEvents=@()
   rules=@([pscustomobject][ordered]@{sequence=1;kind='attack';identity=7000+$index;frame=1;round=$round;actor=$roles[0];target=$o.targetId;opportunity=$false;fullAttack=$false;weapon=$weapon;ranged=$true;natural=$false;actorPosition=@($to[0],0.0,$to[1]);targetPosition=@(0.0,0.0,0.0)})
   terminal=(&$cmd $true);endDistance=$after.targetDistance})}
 # Round 10: C6D-move-cast-move.
 $script:stRound=10;$script:stTurn=501
 &$moveStep 0 'C6D-move-cast-move' 'move-short' 10 @(&$leg 0 10 501 0.0 0.5 @(8.0,0.0) @(8.0,2.0) $false 4 601)
 &$castStep 1 'C6D-move-cast-move' 10 501 0.5 @(8.0,2.0) $false 4 -801
 &$moveStep 2 'C6D-move-cast-move' 'move-short' 10 @(&$leg 0 10 501 0.5 1.0 @(8.0,2.0) @(8.0,0.0) $false 3 602)
 # Round 11: C6D-double-move-ranged (two native Moves, then the in-range shot).
 $script:stRound=11;$script:stTurn=502
 $l=@((&$leg 0 11 502 0.0 1.4 @(8.0,0.0) @(8.0,7.0) $false 3 603),(&$leg 1 11 502 1.4 2.8 @(8.0,7.0) @(8.0,0.0) $false 3 604),(&$leg 2 11 502 2.8 4.2 @(8.0,0.0) @(8.0,7.0) $true 3 605))
 &$moveStep 3 'C6D-double-move-ranged' 'move-two-moves' 11 $l
 &$shotStep 4 'C6D-double-move-ranged' 'attack-ranged' 11 502 4.2 4.2 @(8.0,7.0) @(8.0,7.0) $true 3 802
 # Round 12: beyond range, native exhaustion and its refusal, then the scroll control.
 $script:stRound=12;$script:stTurn=503
 &$moveStep 5 'C7-staged-ranged-approach' 'move-beyond-range' 12 @(&$leg 0 12 503 0.0 2.0 @(8.0,7.0) @(20.64,0.0) $false 3 606)
 &$moveStep 6 'C7-staged-ranged-approach' 'move-exhaust' 12 @((&$leg 0 12 503 2.0 4.0 @(20.64,0.0) @(20.64,7.0) $true 3 607),(&$leg 1 12 503 4.0 6.0 @(20.64,7.0) @(20.64,0.0) $true 3 608))
 &$moveStep 7 'C7-staged-ranged-approach' 'move-probe' 12 @(&$leg 0 12 503 6.0 6.0 @(20.64,0.0) @(20.64,0.0) $true 3 0 $true)
 &$castStep 8 'C6D-double-move-ranged' 12 503 6.0 @(20.64,0.0) $true 3 803
 # Round 13: the RiderRanged approach shot from beyond range.
 $script:stRound=13;$script:stTurn=504
 &$shotStep 9 'C7-staged-ranged-approach' 'attack-ranged-approach' 13 504 0.0 1.0 @(20.64,0.0) @(16.64,0.0) $false 2 804
 $plan=@(Get-KmcChunk7StagedPlan|ForEach-Object{[pscustomobject][ordered]@{row=$_[0];kind=$_[1];round=$_[2]}})
 $events=@($steps|ForEach-Object{@($_.rules)})
 $o|Add-Member staged ([pscustomobject][ordered]@{contract='owner-seed-staged-v1';riderId=$roles[0];mountId=$roles[1];weapon=$weapon;weaponRanged=$true;weaponRangeMeters=15.24;scrollUnits=4;plan=$plan
  defensiveCheck=[pscustomobject]@{covered=$false;reason='The pair never stands inside the diagnostic target''s threatened area during a cast.'}
  scrollAbility=[pscustomobject][ordered]@{blueprint=$clw;sourceItem=$scrollItem;sourceItemBlueprint=$scrollBp;caster=$roles[0];available=$true;availableForCast=$true}
  envelope=$script:stEnvelope;mountCorpulence=0.9;targetCorpulence=0.7;geometryPlan=$sp.plan;geometryConfirmation=$sp.confirmation;steps=@($steps);firstRound=10;progress=$null
  rules=[pscustomobject][ordered]@{dropped=0;errors=@();events=$events}
  scroll=[pscustomobject][ordered]@{blueprint=$scrollBp;requestedCount=4;equipped=[pscustomobject]@{item=$scrollItem;exactSlot=$true;count=1};stacked=[pscustomobject]@{item=$scrollItem;exactSlot=$true;count=4}
   beforeCleanup=[pscustomobject]@{item=$scrollItem;exactSlot=$true;count=2};ownedItemReleased=$true;disposed=$true;slotRestored=$true;noOwnedItemResident=$true}
  castTrace=[pscustomobject][ordered]@{dropped=0;faults=0;identityRegistry=[pscustomobject]@{retainedCount=0;released=$true;faults=0}};originalSlots=[pscustomobject]@{restored=$true};complete=$true}) -Force
 $v.name='combat-staged';$v
}
function Check-SeedStaged([string]$Name,[scriptblock]$Mutation,[string]$Pattern){$v=New-KmcSeedStagedFixture;&$Mutation $v;Check ('owner-seed staged refuses '+$Name) {Invoke-OwnedReader $v} $true $Pattern}
function Get-StStep($v,[int]$i){$v.artifact.observations.staged.steps[$i]}
function Get-StActor($w,[string]$id){@($w.actors|Where-Object id -CEQ $id)[0]}
function Get-StEvent($v,[long]$command,[string]$boundary){@($v.artifact.observations.allocationTrace.events|Where-Object{$_.command-eq$command-and$_.boundary-ceq$boundary})[0]}
Check 'owner-seed staged evidence accepted (move-cast-move, double-move-ranged, ranged approach)' {Invoke-OwnedReader (New-KmcSeedStagedFixture)}
Check 'combat evidence refuses a staged record outside its case' {$v=New-KmcOwnerSeedDerivedCombatFixture 'combat-rider-first';$v.artifact.observations|Add-Member staged ([pscustomobject]@{}) -Force;Invoke-OwnedReader $v} $true 'belongs only to its closed owner-seed selection'
Check 'owner-seed staged accepts the authorization''s native float point for the planned double point' {$v=New-KmcSeedStagedFixture;$t=$v.artifact.observations.targetAuthorization;$t.position=@($t.position|ForEach-Object{[double]$_+0.000004});Invoke-OwnedReader $v}
Check-SeedStaged 'a missing geometry plan' {$args[0].artifact.observations.staged.geometryPlan=$null} 'up-front geometry plan'
Check-SeedStaged 'a target authorization away from the selected placement' {$args[0].artifact.observations.targetAuthorization.position=@(1.0,0.0,0.0)} 'did not place the selected staged candidate'
Check-SeedStaged 'a planned envelope off its inputs' {$args[0].artifact.observations.staged.geometryPlan.mountCorpulence=0.5} 'planned envelope is not'
Check-SeedStaged 'a plan selection past its first feasible candidate' {$p=$args[0].artifact.observations.staged.geometryPlan;$p.selected=[int]$p.selected+1} 'first feasible'
Check-SeedStaged 'a tampered plan proposal' {$p=$args[0].artifact.observations.staged.geometryPlan;$l=@($p.candidates[[int]$p.selected].simulation.legs)[0];$l.proposals[$l.chosen][12]='below-band'} 'recomputes'
Check-SeedStaged 'a plan rider start off the authorized rider' {$args[0].artifact.observations.targetAuthorization.riderPosition=@(8.0,1.4,1.0)} 'authorized rider position'
Check-SeedStaged 'a missing in-combat confirmation' {$args[0].artifact.observations.staged.geometryConfirmation=$null} 'in-combat plan confirmation'
Check-SeedStaged 'a confirmation from another start' {$args[0].artifact.observations.staged.geometryConfirmation.mountStart=@(9.0,0.0,0.0)} 'did not start from mount A'
Check-SeedStaged 'a confirmation off the native mount speed' {$v=$args[0];(Get-StActor (Get-StStep $v 0).before $v.request.ownedPairs.mountAId).native.speedMps=6.0} 'native speed'
Check-SeedStaged 'a confirmation recorded infeasible' {$c=$args[0].artifact.observations.staged.geometryConfirmation;$c.simulation.refusal=[pscustomobject]@{step=5;kind='move-beyond-range';leg=0;term='no-destination'}} 'records a refusal'
Check-SeedStaged 'a tampered confirmation leg' {$c=$args[0].artifact.observations.staged.geometryConfirmation;$l=@($c.simulation.legs)[3];$l.end[2]=[double]$l.end[2]+0.5} 'end or movement time differs'
Check-SeedStaged 'a missing ranged weapon' {$args[0].artifact.observations.staged.weaponRanged=$false} 'ranged weapon or the declared scroll stack'
Check-SeedStaged 'a smaller scroll stack' {$args[0].artifact.observations.staged.scrollUnits=1} 'ranged weapon or the declared scroll stack'
Check-SeedStaged 'a reordered plan' {$p=$args[0].artifact.observations.staged.plan;$t=$p[0];$p[0]=$p[1];$p[1]=$t} 'Staged plan differs'
Check-SeedStaged 'a missing step' {$s=$args[0].artifact.observations.staged;$s.steps=@($s.steps|Select-Object -First 9)} 'every planned native step'
Check-SeedStaged 'a step in another round' {(Get-StStep $args[0] 2).round=11} 'plan position or native round'
Check-SeedStaged 'a step that left rider A''s turn' {(Get-StStep $args[0] 1).after.turn=999} 'left rider A''s native turn'
Check-SeedStaged 'a wrong envelope' {$args[0].artifact.observations.staged.envelope=20.0} 'footprint plus weapon range'
Check-SeedStaged 'a defensive check claimed covered' {$args[0].artifact.observations.staged.defensiveCheck.covered=$true} 'recorded as uncovered'
Check-SeedStaged 'a scroll that is not Cure Light Wounds' {$args[0].artifact.observations.staged.scrollAbility.blueprint='0'*32} 'native Cure Light Wounds'
Check-SeedStaged 'a rider charged for movement' {$v=$args[0];$l=(Get-StStep $v 0).legs[0];$e=Get-StEvent $v 601 'admission-after';$e.boundary='cost-after';$e.state.actor=$v.request.ownedPairs.riderAId} 'rider A was charged'
Check-SeedStaged 'a leg not charged to the mount' {$l=(Get-StStep $args[0] 0).legs[0];(Get-StActor $l.after $args[0].request.ownedPairs.mountAId).move=0.0} 'did not increase'
Check-SeedStaged 'a leg that did not move' {$l=(Get-StStep $args[0] 3).legs[1];$l.after.mountPosition=@($l.before.mountPosition)} 'did not move'
Check-SeedStaged 'a leg carried by the rider' {$l=(Get-StStep $args[0] 0).legs[0];$l.carrier.executor=$args[0].request.ownedPairs.riderAId} 'exact mount carrier'
Check-SeedStaged 'a five-foot step leg' {(Get-StStep $args[0] 0).legs[0].fiveFootStep=$true} 'Native evidence boolean'
Check-SeedStaged 'a mount Standard spent on movement' {$l=(Get-StStep $args[0] 5).legs[0];(Get-StActor $l.after $args[0].request.ownedPairs.mountAId).standard=6.0} 'non-movement action'
Check-SeedStaged 'pair B spending inside a step' {$s=Get-StStep $args[0] 4;(Get-StActor $s.after $args[0].request.ownedPairs.riderBId).standard=6.0} 'Unrelated native actor expenditure'Check-SeedStaged 'a pair B cooldown refunded faster than native time' {$s=Get-StStep $args[0] 4;(Get-StActor $s.before $args[0].request.ownedPairs.riderBId).standard=6.0;$s.after.gameTicks=[long]$s.before.gameTicks+5000000} 'faster than native time'
Check-SeedStaged 'a pair B native cost boundary inside the last activation' {$v=$args[0];$s=@($v.artifact.observations.staged.steps)[-1];$t=$v.artifact.observations.allocationTrace;$e=Copy-Value @($t.events)[-1];$e.sequence=[long]$e.sequence+1;$e.boundary='cost-after';$e.command=987654;$e.state.actor=$v.request.ownedPairs.mountBId;$t.events=@($t.events)+@($e);$s.traceSequenceAfter=$e.sequence} 'must not spend'
Check-SeedStaged 'two short legs beyond one Move' {$l=(Get-StStep $args[0] 2).legs[0];(Get-StActor $l.after $args[0].request.ownedPairs.mountAId).move=3.5;(Get-StActor (Get-StStep $args[0] 2).after $args[0].request.ownedPairs.mountAId).move=3.5} 'exceeded one native Move'
Check-SeedStaged 'a second leg that did not continue the allocation' {$l=(Get-StStep $args[0] 2).legs[0];(Get-StActor $l.before $args[0].request.ownedPairs.mountAId).move=0.0} 'same native Move allocation'
Check-SeedStaged 'a cast without its native commitment' {$e=Get-StEvent $args[0] -801 'cost-after';$e.command=1} 'committed exactly once'
Check-SeedStaged 'a cast that did not charge six seconds' {$e=Get-StEvent $args[0] -801 'cost-after';$e.state.standard=3.0} 'additive Standard debt'
Check-SeedStaged 'a mount charged for the rider cast' {$v=$args[0];$s=Get-StStep $v 1;$e=Get-StEvent $v -801 'cost-before';$e.state.actor=$v.request.ownedPairs.mountAId} 'committed exactly once'
Check-SeedStaged 'a cast that moved the pair' {$s=Get-StStep $args[0] 1;$s.after.mountPosition=@(8.0,0.0,2.5)} 'not stationary'
Check-SeedStaged 'a cast from another item' {(Get-StStep $args[0] 1).ability.sourceItem=1} 'exact equipped disposable scroll'
Check-SeedStaged 'a failed native cast' {@((Get-StStep $args[0] 1).castEvents|Where-Object kind -CEQ 'cast-after')[0].spellFailed=$true} 'failed or duplicated'
Check-SeedStaged 'a cast window charging rider A for another command' {$v=$args[0];$c=Get-StEvent $v -801 'cost-after';$k=[long]$c.sequence;$e=Copy-Value $c;$e.boundary='actor-cost-after';$e.command=4242;$t=$v.artifact.observations.allocationTrace
 foreach($x in @($t.events)){if([long]$x.sequence-gt$k){$x.sequence=[long]$x.sequence+1}};$e.sequence=$k+1;$t.events=@(@($t.events|Where-Object{[long]$_.sequence-le$k})+@($e)+@($t.events|Where-Object{[long]$_.sequence-gt$k+1}))
 foreach($s in @($v.artifact.observations.staged.steps)){foreach($w in @($s)+@(if($null-ne$s.PSObject.Properties['legs']){@($s.legs)})){foreach($n in 'traceSequenceBefore','traceSequenceAfter'){if([long]$w.$n-ge$k){$w.$n=[long]$w.$n+1}}}}} 'another command'
# b204 exact native scroll windows (tests/Chunk7Diagnostics/StagedB204NativeShapes.json, copied from the immutable b204 evidence):
# a self-targeted scroll triggers the pinned sticky-touch RuleCastSpell of the sourceless Cure Light Wounds touch ability.
$b204=Get-Content -LiteralPath (Join-Path $repo 'tests/Chunk7Diagnostics/StagedB204NativeShapes.json') -Raw|ConvertFrom-Json
function Invoke-B204Cast($c){Assert-KmcChunk7StagedScrollDelivery $c @($b204.riderId,$b204.mountId) 'b204 scroll window'}
function Get-B204Events($c,[string]$Kind,[string]$Blueprint){@($c.castEvents|Where-Object{$_.kind-ceq$Kind-and(-not$Blueprint-or($null-ne$_.PSObject.Properties['ability']-and$_.ability.blueprint-ceq$Blueprint))})}
function Check-B204Cast([string]$Name,[int]$Index,[scriptblock]$Mutation,[string]$Pattern=''){$c=Copy-Value $b204.casts[$Index];&$Mutation $c;Check ('b204 scroll window '+$Name) {Invoke-B204Cast $c} ([bool]$Pattern) $Pattern}
$b204Inner='47808d23c67033d4bbab86a1070fd62f';$b204Scroll='5590652e1c2225c4ca30c4a699ab3649'
Check 'b204 step 1 native scroll window accepted' {Invoke-B204Cast (Copy-Value $b204.casts[0])}
Check 'b204 step 8 native scroll window accepted' {Invoke-B204Cast (Copy-Value $b204.casts[1])}
Check-B204Cast 'refuses a self-targeted scroll without its touch delivery and heal' 0 {$c=$args[0];$c.castEvents=@($c.castEvents|Where-Object{-not(($_.kind-cin@('cast-before','cast-after')-and$_.ability.blueprint-ceq$b204Inner)-or$_.kind-ceq'heal')})} 'lacks its native touch delivery'
Check-B204Cast 'with a bounded heal of a damaged rider is accepted' 1 {$c=$args[0];$c.before.actors[0].native.damage=6;(Get-B204Events $c 'heal')[0].value=5}
Check-B204Cast 'refuses a delivery whose heal is missing' 0 {$c=$args[0];$c.castEvents=@($c.castEvents|Where-Object kind -CNE 'heal')} 'heals do not match'
Check-B204Cast 'refuses a heal without the delivery' 0 {$c=$args[0];$c.castEvents=@($c.castEvents|Where-Object{-not($_.kind-cin@('cast-before','cast-after')-and$_.ability.blueprint-ceq$b204Inner)})} 'lacks its native touch delivery'
Check-B204Cast 'refuses a scroll cast-before of a foreign ability' 0 {(Get-B204Events $args[0] 'cast-before' $b204Scroll)[0].ability.blueprint='0'*32} 'failed or duplicated'
Check-B204Cast 'refuses a touch delivery cast-before on the mount' 1 {(Get-B204Events $args[0] 'cast-before' $b204Inner)[0].target=$b204.mountId} 'exact self touch delivery'
Check-B204Cast 'refuses a second heal' 0 {$c=$args[0];$c.castEvents=@($c.castEvents)+@(Copy-Value (Get-B204Events $c 'heal')[0])} 'heals do not match'
Check-B204Cast 'refuses a heal beyond the native damage' 0 {(Get-B204Events $args[0] 'heal')[0].value=5} 'bounded native heal'
Check-B204Cast 'refuses a heal of the mount' 1 {(Get-B204Events $args[0] 'heal')[0].target=$b204.mountId} 'bounded native heal'
Check-B204Cast 'refuses a heal before the delivery' 0 {$c=$args[0];$h=(Get-B204Events $c 'heal')[0];$rest=@($c.castEvents|Where-Object kind -CNE 'heal');$c.castEvents=@($rest[0..11])+@($h)+@($rest[12..($rest.Count-1)])} 'bounded native heal'
Check-B204Cast 'refuses a second touch delivery' 0 {$c=$args[0];$b=Copy-Value (Get-B204Events $c 'cast-before' $b204Inner)[0];$a=Copy-Value (Get-B204Events $c 'cast-after' $b204Inner)[0];$h=Copy-Value (Get-B204Events $c 'heal')[0];$b.identity=1;$a.identity=1;$b.ability.identity=2;$a.ability.identity=2;$c.castEvents=@($c.castEvents)+@($b,$a,$h)} 'failed or duplicated'
Check-B204Cast 'refuses a second sourced scroll cast' 1 {$c=$args[0];$b=Copy-Value (Get-B204Events $c 'cast-before' $b204Scroll)[0];$a=Copy-Value (Get-B204Events $c 'cast-after' $b204Scroll)[0];$b.identity=1;$a.identity=1;$c.castEvents=@($c.castEvents)+@($b,$a)} 'failed or duplicated'
Check-B204Cast 'refuses a touch delivery without the scroll cast' 0 {$c=$args[0];$c.castEvents=@($c.castEvents|Where-Object{-not($_.kind-cin@('cast-before','cast-after')-and$_.ability.blueprint-ceq$b204Scroll)})} 'failed or duplicated'
Check-B204Cast 'refuses a second cast of an unknown blueprint' 0 {$c=$args[0];foreach($e in @(Get-B204Events $c 'cast-before' $b204Inner)+@(Get-B204Events $c 'cast-after' $b204Inner)){$e.ability.blueprint='0'*32}} 'failed or duplicated'
Check-B204Cast 'refuses a sourced touch delivery' 1 {$c=$args[0];foreach($e in @(Get-B204Events $c 'cast-after' $b204Inner)){$e.ability.sourceItem=$c.ability.sourceItem}} 'exact self touch delivery'
Check-B204Cast 'refuses a touch delivery on another target' 0 {(Get-B204Events $args[0] 'cast-after' $b204Inner)[0].target=$b204.mountId} 'exact self touch delivery'
Check-B204Cast 'refuses a failed touch delivery' 0 {(Get-B204Events $args[0] 'cast-after' $b204Inner)[0].spellFailed=$true} 'exact self touch delivery'
Check-B204Cast 'refuses a touch delivery that reuses the scroll spell' 1 {$c=$args[0];foreach($e in @(Get-B204Events $c 'cast-before' $b204Inner)+@(Get-B204Events $c 'cast-after' $b204Inner)){$e.ability.identity=$c.ability.identity}} 'exact self touch delivery'
Check-B204Cast 'refuses a touch delivery before the spend' 0 {$c=$args[0];$d=@($c.castEvents|Where-Object{$_.kind-cin@('cast-before','cast-after')-and$_.ability.blueprint-ceq$b204Inner});$rest=@($c.castEvents|Where-Object{$d-notcontains$_});$c.castEvents=@($rest[0..6])+$d+@($rest[7..($rest.Count-1)])} 'after its spend'
Check-B204Cast 'refuses a second item spend' 0 {$c=$args[0];$c.castEvents=@($c.castEvents)+@(Copy-Value (Get-B204Events $c 'item-spend-before')[0])+@(Copy-Value (Get-B204Events $c 'item-spend-after')[0])} 'spent exactly once'
Check-B204Cast 'refuses a second spell spend' 1 {$c=$args[0];$c.castEvents=@($c.castEvents)+@(Copy-Value (Get-B204Events $c 'spell-spend-before')[0])+@(Copy-Value (Get-B204Events $c 'spell-spend-after')[0])} 'spent exactly once'
Check-B204Cast 'refuses a spend of the delivery spell' 0 {$c=$args[0];foreach($e in @(Get-B204Events $c 'spell-spend-before')+@(Get-B204Events $c 'spell-spend-after')){$e.ability=Copy-Value (Get-B204Events $c 'cast-after' $b204Inner)[0].ability}} 'spent exactly once'
Check-B204Cast 'refuses a child touch command' 0 {$c=$args[0];$s=Copy-Value @($c.castEvents|Where-Object kind -CEQ 'start-before')[0];$s.identity=1234;$c.castEvents=@($c.castEvents)+@($s)} 'another native command'
Check-B204Cast 'refuses a cast by the mount' 1 {$c=$args[0];$e=Copy-Value (Get-B204Events $c 'cast-after' $b204Inner)[0];$e.actor=$b204.mountId;$c.castEvents=@($c.castEvents)+@($e)} 'failed or duplicated'
Check-B204Cast 'refuses a scroll ability without a pinned delivery' 0 {$args[0].ability.blueprint='0'*32} 'no pinned native touch delivery'
# b204 step 4: MountedPairAttackCommand turns the stationary mount to the target; the saddle point only rotates.
# Seat geometry from b204 step 4 (radius about the mount pivot and rider height), placed about the fixture's stationary
# mount: before at the given bearing offset from the mount-to-target direction, after at its own offset (0 faces the target).
function Set-StSeatShot($v,[double]$Radius,[double]$Height,[double]$BeforeDeg,[double]$AfterDeg){$s=Get-StStep $v 4;$m=@($s.before.mountPosition|ForEach-Object{[double]$_});$t=@($s.rules[0].targetPosition|ForEach-Object{[double]$_})
 $base=[Math]::Atan2($t[2]-$m[2],$t[0]-$m[0]);$at={param([double]$deg)$r=$base+$deg*[Math]::PI/180.0;@(($m[0]+$Radius*[Math]::Cos($r)),($m[1]+$Height),($m[2]+$Radius*[Math]::Sin($r)))}
 $s.before.riderPosition=&$at $BeforeDeg;$p=&$at $AfterDeg;$s.after.riderPosition=$p;$s.rules[0].actorPosition=$p}
$b204SeatRadius=[Math]::Sqrt([Math]::Pow([double]$b204.shot.after.riderPosition[0]-[double]$b204.shot.after.mountPosition[0],2)+[Math]::Pow([double]$b204.shot.after.riderPosition[2]-[double]$b204.shot.after.mountPosition[2],2));$b204SeatHeight=[double]$b204.shot.after.riderPosition[1]-[double]$b204.shot.after.mountPosition[1]
Check 'owner-seed staged accepts the b204 in-range seat rotation toward the target about the stationary mount' {$v=New-KmcSeedStagedFixture;Set-StSeatShot $v $b204SeatRadius $b204SeatHeight 84.0 0.0;Invoke-OwnedReader $v}
Check-SeedStaged 'an in-range rider displaced off its seat radius' {$s=Get-StStep $args[0] 4;Set-StSeatShot $args[0] $b204SeatRadius $b204SeatHeight 84.0 0.0;$m=@($s.before.mountPosition);$p=@(([double]$m[0]+2.0*([double]$s.after.riderPosition[0]-[double]$m[0])),[double]$s.after.riderPosition[1],([double]$m[2]+2.0*([double]$s.after.riderPosition[2]-[double]$m[2])));$s.after.riderPosition=$p;$s.rules[0].actorPosition=$p} 'in-range shot origin moved'
Check-SeedStaged 'an in-range seat rotated half a turn' {Set-StSeatShot $args[0] $b204SeatRadius $b204SeatHeight 0.0 180.0} 'in-range shot origin moved'
Check-SeedStaged 'an in-range seat rotated away from the target' {Set-StSeatShot $args[0] $b204SeatRadius $b204SeatHeight 0.0 90.0} 'in-range shot origin moved'
Check-SeedStaged 'an in-range seat beyond the saddle radius cap' {Set-StSeatShot $args[0] 1.2 $b204SeatHeight 84.0 0.0} 'in-range shot origin moved'
Check-SeedStaged 'an in-range rider raised off its seat' {$s=Get-StStep $args[0] 4;Set-StSeatShot $args[0] $b204SeatRadius $b204SeatHeight 84.0 0.0;$p=@($s.after.riderPosition);$p[1]=[double]$p[1]+0.3;$s.after.riderPosition=$p;$s.rules[0].actorPosition=$p} 'in-range shot origin moved'
Check-SeedStaged 'a stack that did not decrement' {$s=Get-StStep $args[0] 1;$s.after.items[0].count=4} 'decrement in place'
Check-SeedStaged 'a measured spend from the last unit' {$s=Get-StStep $args[0] 8;$s.before.items[0].count=1;$s.after.items[0].count=0} 'decrement in place'
Check-SeedStaged 'damage inside a cast without its concentration check' {$s=Get-StStep $args[0] 1;$s.castEvents=@($s.castEvents)+@([pscustomobject]@{kind='damage';actor=$args[0].artifact.observations.targetId;target=$args[0].request.ownedPairs.riderAId;damage=3})} 'concentration checks'
Check-SeedStaged 'a skipped defensive check while engaged' {$s=Get-StStep $args[0] 8;$s.before.riderEngaged=$true;@($s.castEvents|Where-Object kind -CEQ 'action-before')[0].shell.timeSinceStart=1.5} 'skipped its native defensive check'
Check-SeedStaged 'an engaged cast under the uncovered record' {$s=Get-StStep $args[0] 8;$s.before.riderEngaged=$true} 'contradicts the uncovered'
Check-SeedStaged 'a stock ranged refusal' {(Get-StStep $args[0] 4).rejectionCodes=@('MountedRangedUnsupported')} 'RiderRanged pair command'
Check-SeedStaged 'an interrupted shot' {(Get-StStep $args[0] 4).terminal.result='Interrupt'} 'did not finish successfully'
Check-SeedStaged 'a shot charged to the mount' {$v=$args[0];$e=Get-StEvent $v 802 'cost-after';$e.state.actor=$v.request.ownedPairs.mountAId} 'single native Standard commitment'
Check-SeedStaged 'an in-range shot that moved the mount' {$s=Get-StStep $args[0] 4;$s.after.mountPosition=@(8.0,0.0,8.0);$s.after.riderPosition=@(8.0,0.0,8.0);$s.rules[0].actorPosition=@(8.0,0.0,8.0)} 'moved or charged the mount'
Check-SeedStaged 'a shot origin away from the rider' {(Get-StStep $args[0] 4).rules[0].actorPosition=@(8.0,0.0,6.0)} 'shot origin'
Check-SeedStaged 'a second attack inside the shot' {$s=Get-StStep $args[0] 4;$s.rules=@($s.rules)+@(Copy-Value $s.rules[0])} 'one native ranged shot'
Check-SeedStaged 'an attack of opportunity inside a staged window' {$s=Get-StStep $args[0] 3;$s.rules=@([pscustomobject]@{sequence=1;kind='attack';actor=$args[0].artifact.observations.targetId;target=$args[0].request.ownedPairs.mountAId;opportunity=$true;ranged=$false})} 'attack of opportunity or a foreign attack'
Check-SeedStaged 'a shot without the two native Moves' {$s=Get-StStep $args[0] 4;$s.before.mountUsedTwoMove=$false} 'two native Moves'
Check-SeedStaged 'a shot without the rider''s Standard' {$s=Get-StStep $args[0] 4;$s.before.riderHasStandard=$false} 'Standard was not available'
Check-SeedStaged 'a scroll control without the renewed Standard' {$s=Get-StStep $args[0] 8;(Get-StActor $s.before $args[0].request.ownedPairs.riderAId).standard=6.0} 'renewed Standard'
Check-SeedStaged 'a move that stayed inside weapon range' {(Get-StStep $args[0] 5).after.targetDistance=18.0} 'beyond weapon range'
Check-SeedStaged 'movement that was not exhausted' {$l=@((Get-StStep $args[0] 6).legs)[-1];(Get-StActor $l.after $args[0].request.ownedPairs.mountAId).move=5.0} 'not exhausted'
Check-SeedStaged 'an exhausted mount that still moved' {$l=(Get-StStep $args[0] 7).legs[0];$l.after.mountPosition=@(21.5,0.0,0.0)} 'exhausted mount still moved'
Check-SeedStaged 'a refused move that committed a cost' {$v=$args[0];$l=(Get-StStep $v 7).legs[0];$e=Get-StEvent $v 803 'cost-before';$l.traceSequenceAfter=$e.sequence;(Get-StStep $v 7).traceSequenceAfter=$e.sequence} 'committed a cost|rider A was charged'
Check-SeedStaged 'an approach shot from inside range' {(Get-StStep $args[0] 9).startDistance=17.0} 'did not start beyond weapon range'
Check-SeedStaged 'an approach without the mount''s native move' {$v=$args[0];$e=Get-StEvent $v 990 'admission-after';$e.boundary='admission-before'} 'exactly one native mount move'
Check-SeedStaged 'an approach not charged to the mount' {$s=Get-StStep $args[0] 9;(Get-StActor $s.after $args[0].request.ownedPairs.mountAId).move=0.0} 'not charged to the mount'
Check-SeedStaged 'a shot fired before the approach' {(Get-StStep $args[0] 9).rules[0].actorPosition=@(20.64,0.0,0.0)} 'shot origin'
Check-SeedStaged 'an approach that stopped out of range' {(Get-StStep $args[0] 9).endDistance=18.5} 'outside weapon range'
Check-SeedStaged 'two rider costs in one activation' {$v=$args[0];$s=Get-StStep $v 2;$l=$s.legs[0];$script:stSeq=[long](@($v.artifact.observations.allocationTrace.events)|Measure-Object -Property sequence -Maximum).Maximum;$t=$v.artifact.observations.allocationTrace;$t.events=@($t.events)+@([pscustomobject][ordered]@{sequence=$script:stSeq+1;boundary='cost-after';frame=1;round=10;command=5;commandActor=$null;commandType=$null;state=[pscustomobject]@{actor=$v.request.ownedPairs.riderAId;standard=6.0}});foreach($i in 0..9){(Get-StStep $v $i).traceSequenceBefore+=0};$s.traceSequenceAfter=$script:stSeq+1;$l.traceSequenceAfter=$script:stSeq+1} 'rider A was charged|exactly one rider cost'
Check-SeedStaged 'a turn transition inside an activation' {$v=$args[0];$e=Get-StEvent $v 602 'admission-after';$e.boundary='prepare-after'} 'turn transition'
Check-SeedStaged 'a stack built off the equipped entity' {$args[0].artifact.observations.staged.scroll.stacked.item=1} 'built on its equipped entity'
Check-SeedStaged 'a stack that does not conserve its spends' {$args[0].artifact.observations.staged.scroll.beforeCleanup.count=3} 'conserve its two observed native spends'
Check-SeedStaged 'a retained disposable item' {$args[0].artifact.observations.staged.scroll.noOwnedItemResident=$false} 'Native evidence boolean'
Check-SeedStaged 'an unretired cast observer' {$args[0].artifact.observations.staged.castTrace.identityRegistry.released=$false} 'not retired'
Check-SeedStaged 'a dropped rule observation' {$args[0].artifact.observations.staged.rules.dropped=1} 'dropped or failed'
Check-SeedStaged 'an incomplete run' {$args[0].artifact.observations.staged.complete=$false} 'Native evidence boolean'
Check-SeedStaged 'a carried reaction record' {$args[0].artifact.observations|Add-Member reaction ([pscustomobject]@{}) -Force} 'cannot carry another combat case'
Check-SeedStaged 'a relationship input' {$args[0].artifact.observations.inputs=@([pscustomobject]@{label='stray'})} 'no relationship input'
Check-SeedStaged 'a turn end inside an activation' {$v=$args[0];$e=Get-StEvent $v 602 'admission-after';$e.boundary='turn-end-before'} 'turn transition'
Check-SeedStaged 'a cost boundary of another native turn' {$e=Get-StEvent $args[0] 802 'cost-before';$e.turn=999} 'outside rider A''s own native activation'
Check-SeedStaged 'a cost boundary after the turn ended' {$e=Get-StEvent $args[0] -801 'cost-after';$e.turnStatus='Ended'} 'outside rider A''s own native activation'
Check-SeedStaged 'one native turn identity across two rounds' {$v=$args[0];foreach($i in 3,4){$s=Get-StStep $v $i;$s.before.turn=501;$s.after.turn=501;if($null-ne$s.PSObject.Properties["legs"]){foreach($l in @($s.legs)){$l.before.turn=501;$l.after.turn=501}}};foreach($e in @($v.artifact.observations.allocationTrace.events|Where-Object{$_.PSObject.Properties['turn']-and$_.turn-eq502})){$e.turn=501}} 'reuse one native turn identity'
Check-SeedStaged 'a foreign attack of opportunity outside every window' {$r=$args[0].artifact.observations.staged.rules;$r.events=@($r.events)+@([pscustomobject]@{sequence=9;kind='attack';actor=$args[0].artifact.observations.targetId;target=$args[0].request.ownedPairs.mountAId;opportunity=$true;ranged=$false})} 'outside the step windows'
Check-SeedStaged 'a cast observer that retains identities' {$args[0].artifact.observations.staged.castTrace.identityRegistry.retainedCount=7} 'not retired'
Check-SeedStaged 'original quick slots not observed restored' {$args[0].artifact.observations.staged.originalSlots.restored=$false} 'quick slots were not observed restored'
Check-SeedStaged 'a second leg before any Move was used' {(Get-StStep $args[0] 2).legs[0].before.mountUsedOneMove=$false} 'retained native movement time'
Check-SeedStaged 'a second leg that consumed no native movement time' {$l=(Get-StStep $args[0] 2).legs[0];(Get-StActor $l.after $args[0].request.ownedPairs.mountAId).native.remainingNativeTime=5.5} 'retained native movement time'
Check-SeedStaged 'a full attack as the shot' {(Get-StStep $args[0] 4).rules[0].fullAttack=$true;$args[0].artifact.observations.staged.rules.events=@((Get-StStep $args[0] 4).rules[0],(Get-StStep $args[0] 9).rules[0])} 'one native ranged shot'
Check-SeedStaged 'a single-action move leg' {(Get-StStep $args[0] 0).legs[0].singleActionMove=$true} 'Native evidence boolean'
Check-SeedStaged 'an absent cast shell identity' {$s=Get-StStep $args[0] 8;$s.costShell=0;$s.terminal.identity=0} 'another native command'
Check 'owner-seed combat reaction accepts a negative native command identity' {$v=New-KmcSeedReactionFixture;(Get-RxLeg $v).command=-1234567;Invoke-OwnedReader $v}
Check 'owner-seed combat reaction accepts an interrupted move that left the threatened area short of its clear distance' {$v=New-KmcSeedReactionFixture;$d=Get-RxLeg $v;$d.result='Interrupt';$d.targetAfter.mountDistance=6.0;Invoke-OwnedReader $v}
Check 'owner-seed combat reaction accepts pair B cooldowns that only decayed natively' {$v=New-KmcSeedReactionFixture;$d=Get-RxLeg $v;(RActor $d.before $v.request.ownedPairs.riderBId).standard=6.0;(RActor $d.after $v.request.ownedPairs.riderBId).standard=5.5;$d.after.gameTicks=[long]$d.before.gameTicks+10000000;Invoke-OwnedReader $v}
Check-SeedReaction 'pair B spending its move inside the window' {$d=Get-RxLeg $args[0];(RActor $d.after $args[0].request.ownedPairs.mountBId).move=3.0} 'Unrelated native actor expenditure changed'Check-SeedReaction 'a pair B cooldown refunded faster than native time' {$d=Get-RxLeg $args[0];(RActor $d.before $args[0].request.ownedPairs.riderBId).standard=6.0;$d.after.gameTicks=[long]$d.before.gameTicks+5000000} 'faster than native time'
Check-SeedReaction 'a pair B native cost boundary inside the disengagement' {$v=$args[0];$d=Get-RxLeg $v;$t=$v.artifact.observations.allocationTrace;$e=Copy-Value @($t.events|Where-Object boundary -CEQ 'cost-after')[-1];$e.sequence=[long]$script:rxTraceBase+1;$d.traceSequenceBefore=$script:rxTraceBase;$e.state.actor=$v.request.ownedPairs.mountBId;$t.events=@($t.events)+@($e)} 'must not spend'
Check-SeedReaction 'a rider A native cost boundary inside the disengagement' {$v=$args[0];$d=Get-RxLeg $v;$t=$v.artifact.observations.allocationTrace;$e=Copy-Value @($t.events|Where-Object boundary -CEQ 'cost-before')[-1];$e.sequence=[long]$script:rxTraceBase+1;$d.traceSequenceBefore=$script:rxTraceBase;$e.state.actor=$v.request.ownedPairs.riderAId;$t.events=@($t.events)+@($e)} 'must not spend'
Check-SeedReaction 'missing disengagement gates' {$args[0].artifact.observations.reaction.PSObject.Properties.Remove('disengageGates')} 'could not natively make its attack of opportunity'
Check-SeedReaction 'gates lost before the disengagement' {$args[0].artifact.observations.reaction.disengageGates.threatHand=$false} 'could not natively make its attack of opportunity'
Check-SeedReaction 'a victim healed by the attack' {(Get-RxLeg $args[0]).pairBefore.mountDamage=5} 'healed its victim'
Check-SeedReaction 'the other pair actor damaged' {(Get-RxLeg $args[0]).residue.riderDamage=9} 'damaged the other pair actor'
Check-SeedReaction 'an unrecorded pair state before the leg' {(Get-RxLeg $args[0]).PSObject.Properties.Remove('pairBefore')} 'pair state before the leg'
Check-SeedReaction 'a completed move short of its clear distance' {(Get-RxLeg $args[0]).targetAfter.mountDistance=6.0} 'clear distance'
Check-SeedReaction 'an absent command identity' {(Get-RxLeg $args[0]).command=0} 'completed native ground move'
Check-SeedReaction 'a single-action move mode' {(Get-RxLeg $args[0]).singleActionMove=$true} 'Native evidence boolean'
Check-SeedReaction 'a target still waiting on initiative' {$args[0].artifact.observations.reaction.precondition.waitingInitiative=$true} 'could not natively make its attack of opportunity'
Check-SeedReaction 'a surprised target' {(Get-RxLeg $args[0]).targetBefore.surprised=$true} 'could not natively make its attack of opportunity'
Check-SeedReaction 'a target without a threat hand' {$args[0].artifact.observations.reaction.precondition.threatHand=$false} 'could not natively make its attack of opportunity'
Check-SeedReaction 'a mount that does not remember the target' {(Get-RxLeg $args[0]).targetBefore.mountRemembersTarget=$false} 'could not natively make its attack of opportunity'
Check-SeedReaction 'a prone victim' {(Get-RxLeg $args[0]).residue.mountProne=$true} 'Native evidence boolean'
Check-SeedReaction 'unrecorded victim hit points' {(Get-RxLeg $args[0]).residue.mountDamage=$null} 'hit points are not recorded'
Check-SeedReaction 'a capture swapped for another attack of the same count' {$r=$args[0].artifact.observations.reaction.rules;$e=Copy-Value $r.events[0];$e.target=$args[0].request.ownedPairs.riderBId;$r.events=@($e)} 'outside the reaction windows'
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
  Set-KmcOwnerSeedSchema2DebtDiagnosticFixture $v
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
 Check ('six-capacity current-format historical resave child '+$checkpoint){$v=New-KmcOwnerSeedCurrentFixture $checkpoint;$v.artifact.observations.nativeCapacity=6;Invoke-OwnedReader $v}
 if($checkpoint-ceq'owned-schema2-pending-current-manual'){
  Check 'current-format pending cold accepts bounded paired native clock progress' {
   $v=New-KmcOwnerSeedCurrentFixture $checkpoint;$w=$v.artifact.observations.currentFormatMigration.restoredWorld
   $w.gameTicks+=5000000L;$w.combat.TimeSinceStart+=.5;$w.combat.TimeToNextRound-=.5
   foreach($actor in $w.actors){foreach($field in @('Standard','Move','Swift','Initiative','Reaction')){if($actor.Native.$field-ge.5){$actor.Native.$field-=.5}}}
   Invoke-OwnedReader $v
  }
  foreach($fault in @('one-sided','backwards','excess','new-round','string-clock','string-ticks','fractional-ticks','refunded-debt','changed-prepared')){
   Check ('current-format pending cold refuses '+$fault+' clock or allocation change') {
    $v=New-KmcOwnerSeedCurrentFixture $checkpoint;$w=$v.artifact.observations.currentFormatMigration.restoredWorld
    $w.gameTicks+=5000000L;$w.combat.TimeSinceStart+=.5;$w.combat.TimeToNextRound-=.5
    switch($fault){'one-sided'{$w.combat.TimeToNextRound+=.5};'backwards'{$w.combat.TimeSinceStart-=1};'excess'{$w.gameTicks-=4000000L};'new-round'{$w.combat.Round++};'string-clock'{$w.combat.TimeSinceStart=[string]$w.combat.TimeSinceStart};'string-ticks'{$w.gameTicks=[string]$w.gameTicks};'fractional-ticks'{$w.gameTicks=[double]$w.gameTicks+.5};'refunded-debt'{$w.actors[0].Native.Standard+=.5};'changed-prepared'{$w.actors[0].Prepared=-not$w.actors[0].Prepared}}
    Invoke-OwnedReader $v
   } $true
  }
 }
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
