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
$clock.Stop();[void][IO.Directory]::CreateDirectory($OutputDirectory);$passed=@($results|Where-Object pass).Count;$failed=$results.Count-$passed
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'receipt.json'),([pscustomobject]@{passed=$passed;failed=$failed;elapsedSeconds=$clock.Elapsed.TotalSeconds;nativeInvoked=$false;archiveRead=$false;results=@($results)}|ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false))
Write-Output ('Owned two-pair reader: '+$passed+' PASS / '+$failed+' FAIL');if($failed){exit 1}
