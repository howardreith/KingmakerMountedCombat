$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
. (Join-Path $kmcRepo 'scripts/runtime/RuntimeHarness.Common.ps1')
. (Join-Path $repo 'scripts/runtime/Chunk7PersistenceEvidence.ps1')
. (Join-Path $repo 'scripts/runtime/Chunk7PersistenceSaveFixtures.ps1')
$passed=0
function Check([string]$Name,[scriptblock]$Body){& $Body;$script:passed++;Write-Host ('PASS '+$Name)}
function Reject([string]$Name,[scriptblock]$Body){$refused=$false;try{& $Body}catch{$refused=$true};if(-not$refused){throw ('Unexpected acceptance: '+$Name)};$script:passed++;Write-Host ('PASS refuses '+$Name)}
function Copy-Test($value){return ($value|ConvertTo-Json -Depth 90 -Compress|ConvertFrom-Json)}
$first='b6628a77-4962-47a4-a17c-88d9836fc9d5';$mammoth='d79a4f6c-b74e-4868-95bd-533899131acb';$second='d17c8fd0-6627-40c4-bf9a-024cf001c1f7';$horse='77777777-7777-4777-8777-777777777777'
$ids=@($first,$mammoth,$second,$horse)
$request=[pscustomobject]@{schemaVersion=2;scenario='chunk7-persistence-save';persistenceCase='multi-exploration-manual';runId='synthetic-c7-persistence';branch='codex/mounted-combat-chunk7-foundation';commit='1'*40;
    productVersion='synthetic';dllSha256='2'*64;dllMvid='33333333-3333-4333-8333-333333333333';fixture=[pscustomobject]@{
    working=[pscustomobject]@{sha256='5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6';gameId='11111111-1111-4111-8111-111111111111';gameName='owned';area='a'*32};baseline=[pscustomobject]@{sha256='4'*64}}}
function Native([string]$Id,[double]$Debt){[pscustomobject]@{Id=$Id;Standard=$Debt;Move=0.5;Swift=0.0;Initiative=0.0;Reaction=0.0;ReactionsRemaining=1;LastSurpriseTicks=0L}}
$native=@();for($i=0;$i-lt4;$i++){$native+=Native $ids[$i] ($i+1)}
$pairs=@([pscustomobject]@{PairId='11111111111141118111111111111111';Generation=1L;ProfileId='medium-humanoid-mammoth-v1';Rider=$native[0];Mount=$native[1]},
    [pscustomobject]@{PairId='22222222222242228222222222222222';Generation=4L;ProfileId='medium-humanoid-horse-v1';Rider=$native[2];Mount=$native[3]})
$slots=@();foreach($id in @($first,$second)){foreach($kind in @(2,3)){$slots+=[pscustomobject]@{ActorId=$id;Index=10+$kind;Kind=$kind}}}
$archive=[pscustomobject]@{SchemaVersion=3;CampaignId=$request.fixture.working.gameId;AreaId=$request.fixture.working.area;GameTimeTicks=10000000L;Policy='rider-principal-distinct-native-v1';RulesId='crpg-transport-v1';Pairs=$pairs;Participations=@();Slots=$slots;Combat=$null}
$owners=@($pairs|ForEach-Object{[pscustomobject]@{ownerId=$_.PairId;generation=$_.Generation;riderId=$_.Rider.Id;mountId=$_.Mount.Id;state='Active'}})
$snapshot=[pscustomobject]@{label='native-write-complete';owners=$owners;actors=@($native|ForEach-Object{[pscustomobject]@{native=$_;prepared=$false;inCombat=$false}});gameTicks=10000000L;
    controls=[pscustomobject]@{DuplicateFactCount=0;SerializationSuspended=$false;ManagedHotbarSlotCount=4};semanticRestores=0;presentationRestores=0;nativePostLoads=0;snapshots=1;nativeCastRequests=2;archivePhase='not-selected'}
$inputs=@();for($i=0;$i-lt2;$i++){$pair=$pairs[$i];$inputs+=[pscustomobject]@{label=@('mount-mammoth','mount-horse')[$i];casterId=$pair.Rider.Id;targetId=$pair.Mount.Id;
    abilityGuid='f053faad986631688defa003cd7bda0e';nativeCastDelta=1;nativeRefusalDelta=0;generationAtInit=0L;control='shell-'+$i;selectionAfterInput=$pairs[1-$i].Rider.Id;clicked=$true;terminal=$true}}
$moves=@();for($i=0;$i-lt2;$i++){$pair=$pairs[$i];$moves+=[pscustomobject]@{label=@('first-pair','second-pair')[$i];selectedId=$pair.Rider.Id;executorId=$pair.Mount.Id;terminal=$true;otherCommandsUnchanged=$true;
    start=[pscustomobject]@{x=0.0;y=0.0;z=0.0};goal=[pscustomobject]@{x=2.0;y=0.0;z=0.0};end=[pscustomobject]@{x=2.0;y=0.0;z=0.0};distance=2.0;residual=0.0}}
$o=[pscustomobject]@{checkpoint=$request.persistenceCase;cold=$false;sourceWorkingSha256=$request.fixture.working.sha256;nativeSchema=3;nativeCapacity=2;processId=123;
    nativeProgressionRequiresExternalWorkingRestoration=$true;ordinaryRespecProven=$false;combatDebtProof=$false;snapshots=@($snapshot);inputs=$inputs;movements=$moves;
    nativeWrite=[pscustomobject]@{archive=$archive;nativeType='Manual';operation='None';snapshotCount=1;sha256='5'*64;length=1234L;nativeCallback=$true};
    saveRequested=[pscustomobject]@{nativeType='Manual';snapshotCount=0;nativeSaveAllowed=$true;newSlot=$true};
    acquisition=[pscustomobject]@{nativeRangerCommitCount=4;huntersBondSelectionLevel=4;rangerCompanionSelectionLevel=4;horseFactRankAtCommit=1;horsePresentAtNativeCommit=$true};
    cleanup=[pscustomobject]@{pairsClear=$true;sourceHorseRemoved=$true;controlsSettled=$true;saveCustodyClear=$true;selectionRestored=$true;nativeProgressionRequiresExternalWorkingRestoration=$true;ordinaryRespecProven=$false;world=[pscustomobject]@{owners=@()}}}
function New-Artifact($Request,$Observation){$v=[ordered]@{schemaVersion=1;evidenceKind='chunk7-persistence-evidence';createdAtUtc='2026-10-12T00:00:00Z';status='PASS';errors=@();observations=$Observation};foreach($field in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid')){$v[$field]=$Request.$field};[pscustomobject]$v}
$good=New-Artifact $request $o
Check 'complete synthetic manual all-pair native observation' {Assert-KmcChunk7PersistenceObservation $request $good 'PASS'}
Check 'six-capacity candidate preserves the same two-pair Manual assertions' {$v=Copy-Test $good;$v.observations.nativeCapacity=6;Assert-KmcChunk7PersistenceObservation $request $v 'PASS'}
foreach($capacity in @($null,0,1,3,4,5,7,'6',6.0,$true,@(6))){Reject 'unsupported or mistyped native capacity' {$v=Copy-Test $good;$v.observations.nativeCapacity=$capacity;Assert-KmcChunk7PersistenceObservation $request $v 'PASS'}}
foreach($type in @('Quick','Auto')) {Check ('actual native category reader '+$type) {$q=Copy-Test $request;$a=Copy-Test $good;$q.persistenceCase='multi-exploration-'+$type.ToLowerInvariant();$a.observations.checkpoint=$q.persistenceCase;$a.observations.nativeWrite.nativeType=$type;$a.observations.saveRequested.nativeType=$type;Assert-KmcChunk7PersistenceObservation $q $a 'PASS'}}
foreach($mutation in @('missing-pair','duplicate-actor','owner-generation','foreign-fixture','dropped-slot','duplicate-slot','free-debt','replayed-input','native-callback','wrong-category','cleanup','fake-combat-proof','missed-movement')) {
    Reject $mutation {$a=Copy-Test $good;$v=$a.observations;switch($mutation){
      'missing-pair' {$v.nativeWrite.archive.Pairs=@($v.nativeWrite.archive.Pairs[0])}
      'duplicate-actor' {$v.nativeWrite.archive.Pairs[1].Mount.Id=$first}
      'owner-generation' {$v.snapshots[0].owners[0].generation=2}
      'foreign-fixture' {$v.sourceWorkingSha256='0'*64}
      'dropped-slot' {$v.nativeWrite.archive.Slots=@($v.nativeWrite.archive.Slots|Select-Object -First 3)}
      'duplicate-slot' {$v.nativeWrite.archive.Slots[1].Index=$v.nativeWrite.archive.Slots[0].Index}
      'free-debt' {$v.snapshots[0].actors[0].native.Standard=0.0}
      'replayed-input' {$v.inputs[1].nativeCastDelta=2}
      'native-callback' {$v.nativeWrite.nativeCallback=$false}
      'wrong-category' {$v.nativeWrite.nativeType='Quick'}
      'cleanup' {$v.cleanup.saveCustodyClear=$false}
      'fake-combat-proof' {$v.combatDebtProof=$true}
      'missed-movement' {$v.movements[0].end.x=0.2;$v.movements[0].distance=0.2;$v.movements[0].residual=1.8}
    };Assert-KmcChunk7PersistenceObservation $request $a 'PASS'}
}
$coldRequest=Copy-Test $request;$coldRequest.scenario='chunk7-persistence-load'
$coldRequest|Add-Member NoteProperty persistenceLoad ([pscustomobject]@{internalName='KMC_C7_WORLD';fileName='Manual_300_KMC_C7_WORLD.zks';sha256='5'*64;length=1234L;lastWriteTimeUtcTicks=1L;gameId=$request.fixture.working.gameId;gameName='owned';area=$request.fixture.working.area})
$co=Copy-Test $o;$co.cold=$true;$co.processId=456;$co.inputs=@();$co.snapshots[0].label='cold-restored-before-input';$co.snapshots[0].semanticRestores=4;$co.snapshots[0].presentationRestores=2;$co.snapshots[0].nativePostLoads=4;$co.snapshots[0].snapshots=0;$co.snapshots[0].nativeCastRequests=0;$co.snapshots[0].archivePhase='all-pair-presentation-restored'
$co|Add-Member NoteProperty loadedArchive (Copy-Test $archive);$co|Add-Member NoteProperty loadedDescriptor (Copy-Test $coldRequest.persistenceLoad)
$hooks=@([pscustomobject]@{declaringType='TurnBased.Controllers.TurnController';method='Prepare';token='06000C3C';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'},
    [pscustomobject]@{declaringType='Kingmaker.Controllers.Combat.UnitCombatState';method='NewRound';token='0600939D';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'})
foreach($name in @('RestoreActor','RestoreDebt')){$hooks+=[pscustomobject]@{declaringType='KingmakerMountedCombat.Integration.NativeCombatActorPersistence';method=$name;token='06001000';moduleMvid=$request.dllMvid}}
$events=@();foreach($actor in $native){foreach($kind in @('restore-debt-before','restore-debt-after')){$events+=[pscustomobject]@{actorId=$actor.Id;kind=$kind;restorePhase='validated-before-native-mutation';debt=(Copy-Test $actor)}}}
$co|Add-Member NoteProperty coldRestoration ([pscustomobject]@{complete=$true;installedBeforeWorldLoad=$true;contract='chunk7-native-restoration-entry-observer-v1';maximumEvents=2048;dropped=0;errors=0;prepareMetric='native-method-entries-including-production-refused-attempts';hooks=$hooks;events=$events})
$cold=New-Artifact $coldRequest $co
Check 'complete synthetic cold graph with exact observer and no native preparation' {Assert-KmcChunk7PersistenceObservation $coldRequest $cold 'PASS'}
foreach($mutation in @('late-observer','missing-hook','wrong-native-hook','wrong-product-hook','prepare-entry','round-entry','lost-debt-event','wrong-restored-debt','wrong-restore-phase','cold-cast','cold-write','cold-source')) {
    Reject $mutation {$a=Copy-Test $cold;$v=$a.observations;switch($mutation){
      'late-observer' {$v.coldRestoration.installedBeforeWorldLoad=$false}
      'missing-hook' {$v.coldRestoration.hooks=@($v.coldRestoration.hooks|Select-Object -First 3)}
      'wrong-native-hook' {$v.coldRestoration.hooks[0].token='06000C3D'}
      'wrong-product-hook' {$v.coldRestoration.hooks[3].moduleMvid='99999999-9999-4999-8999-999999999999'}
      'prepare-entry' {$v.coldRestoration.events+= [pscustomobject]@{actorId=$first;kind='prepare-before'}}
      'round-entry' {$v.coldRestoration.events+= [pscustomobject]@{actorId=$horse;kind='round-before'}}
      'lost-debt-event' {$v.coldRestoration.events=@($v.coldRestoration.events|Select-Object -Skip 1)}
      'wrong-restored-debt' {$v.coldRestoration.events[1].debt.Standard=0.0}
      'wrong-restore-phase' {$v.coldRestoration.events[1].restorePhase='not-selected'}
      'cold-cast' {$v.snapshots[0].nativeCastRequests=1}
      'cold-write' {$v.snapshots[0].snapshots=1}
      'cold-source' {$v.loadedDescriptor.sha256='6'*64}
    };Assert-KmcChunk7PersistenceObservation $coldRequest $a 'PASS'}
}
Write-Host ('CHUNK7 PERSISTENCE READER PASS='+$passed+' FAIL=0; synthetic validator fixtures, no native gameplay claim.')
