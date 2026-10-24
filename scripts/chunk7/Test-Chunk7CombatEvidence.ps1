$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
. (Join-Path $repo 'scripts/runtime/RuntimeHarness.Common.ps1')
. (Join-Path $repo 'scripts/runtime/Chunk7PersistenceEvidence.ps1')
. (Join-Path $repo 'scripts/runtime/Chunk7CombatEvidence.ps1')
. (Join-Path $PSScriptRoot 'Chunk7NativeForfeitFixture.ps1')
$passed=0
function Check([string]$Name,[scriptblock]$Body){& $Body;$script:passed++;Write-Host ('PASS '+$Name)}
function Reject([string]$Name,[scriptblock]$Body){$caught=$false;try{& $Body}catch{$caught=$true};if(-not$caught){throw ('Unexpected acceptance: '+$Name)};$script:passed++;Write-Host ('PASS refuses '+$Name)}
function Copy-Value($Value){$Value|ConvertTo-Json -Depth 90 -Compress|ConvertFrom-Json}
$ids=@('b6628a77-4962-47a4-a17c-88d9836fc9d5','d79a4f6c-b74e-4868-95bd-533899131acb','d17c8fd0-6627-40c4-bf9a-024cf001c1f7','77777777-7777-4777-8777-777777777777')
$target='88888888-8888-4888-8888-888888888888'
$request=[pscustomobject]@{scenario='chunk7-two-pair-combat-rider-first';runId='synthetic-c7-combat';branch='codex/test';commit='1'*40;productVersion='synthetic';dllSha256='2'*64;dllMvid='33333333-3333-4333-8333-333333333333';fixture=[pscustomobject]@{working=[pscustomobject]@{sha256='5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6'}}}
$owners=@([pscustomobject]@{id='11111111-1111-4111-8111-111111111111';riderId=$ids[0].Replace('-','');mountId=$ids[1].Replace('-','');generation=1;state='Active';retainsObligations=$true},[pscustomobject]@{id='22222222-2222-4222-8222-222222222222';riderId=$ids[2].Replace('-','');mountId=$ids[3].Replace('-','');generation=1;state='Active';retainsObligations=$true})
function Snap([int]$Frame,[string]$Label='window'){
    [pscustomobject]@{label=$Label;frame=$Frame;gameTicks=[long]$Frame*100000;round=1;currentActor=$ids[0];owners=@($owners|ForEach-Object{Copy-Value $_});
        actors=@($ids|ForEach-Object{[pscustomobject]@{id=$_;standard=0.0;move=0.0;swift=0.0;reactions=1;reactionCooldown=0.0;prepared=$true;position=[pscustomobject]@{x=0.0;y=0.0;z=0.0};native=[pscustomobject]@{actor=$_;grantSequence=1}}})}
}
$traceEvents=@();$attackOrdinal=0
function Attack([int]$Pair,[int]$Member,[int]$Frame,[string]$Label){
    $script:attackOrdinal++;$id=$ids[$Pair*2+$Member];$caster=$ids[$Pair*2];$before=Snap $Frame;$after=Snap ($Frame+4);$before.currentActor=$caster;$after.currentActor=$caster
    $after.actors[$Pair*2+$Member].standard=3.0
    $command=600+$script:attackOrdinal
    foreach($kind in @('cost-before','actor-cost-before','actor-cost-after','cost-after')){
        $state=[pscustomobject]@{actor=$id;standard=$(if($kind-clike'*-after'){3.0}else{0.0});move=0.0;swift=0.0;reactions=1;reactionCooldown=0.0}
        $script:traceEvents+=[pscustomobject]@{sequence=$script:traceEvents.Count+1;boundary=$kind;state=$state;frame=$Frame+1;command=$command;commandActor=$id;commandType='KingmakerMountedCombat.Integration.MountedPairAttackCommand';actionType='Standard'}
    }
    [pscustomobject]@{label=$Label;pair=$Pair;casterId=$caster;executorId=$caster;selectionAfterInput=$ids[2-2*$Pair];nativeCastDelta=1;nativeRefusalDelta=0;command=100+$script:attackOrdinal;clicked=$true;before=$before;after=$after;
        outcome=[pscustomobject]@{actorId=$id;resourceOwnerId=$id;targetId=$target;result='Success';completedAttacks=1;nativeAttackRule=$true;singleAttack=$true}}
}
$inputs=@();foreach($i in 0..2){$caster=if($i-eq1){$ids[2]}else{$ids[0]};$targetActor=if($i-eq0){$ids[1]}elseif($i-eq1){$ids[3]}else{$ids[0]};$ability=if($i-eq2){'3af2b81f4d72bbb30501fa730fcdf36e'}else{'f053faad986631688defa003cd7bda0e'}
    $inputs+=[pscustomobject]@{label=@('mount-a','mount-b','r1-native-dismount-a')[$i];casterId=$caster;targetId=$targetActor;abilityGuid=$ability;nativeCastDelta=1;nativeRefusalDelta=0;control=('control-'+$i);generationAtInit=1;selectionAfterInput=$(if($i-eq1){$ids[0]}else{$ids[2]});clicked=$true;terminal=$true;processTerminal=$true}}
$activations=@();$snapshots=@();$calls=@()
for($i=0;$i-lt2;$i++){
    $frame=10+50*$i;$before=Snap $frame;$before.currentActor=$ids[2*$i];$moveBefore=Snap ($frame+1);$moveAfter=Snap ($frame+4);$moveAfter.actors[$i*2+1].position.z=0.5;$moveAfter.actors[$i*2+1].move=0.25
    $end=Snap ($frame+35);$end.actors[$i*2].standard=6.0;$end.actors[$i*2+1].standard=6.0
    $charge=[pscustomobject]@{featureEnabled=$true;available=$false;castDelta=0;reason='TB unsupported';before=(Snap ($frame+29));after=(Snap ($frame+29))}
    $activations+=[pscustomobject]@{pair=$i;sequence=1;before=$before;afterEnd=$end;tbChargeRefusal=$charge;movement=[pscustomobject]@{clicked=$true;terminal=$true;selectedId=$ids[2*$i];executorId=$ids[2*$i+1];command=200+$i;distance=0.5;before=$moveBefore;after=$moveAfter}}
    $inputs+=Attack $i 0 ($frame+10) 'rider-primary';$inputs+=Attack $i 1 ($frame+20) 'mount-primary'
    foreach($actor in @($ids[$i*2],$ids[$i*2+1])){$calls+=[pscustomobject]@{kind='end';complete=$true;before=[pscustomobject]@{actorId=$actor;frame=$frame+31};after=[pscustomobject]@{frame=$frame+32}}}
    $refresh=Snap (130+$i) ('pair-'+$i+'-native-refresh');$refresh.actors[$i*2].native.grantSequence=2;$refresh.actors[$i*2+1].native.grantSequence=2;$snapshots+=$refresh
}
$inputs+=Attack 1 1 180 'pending-survivor-mount-expenditure'
$afterDismount=Snap 150;$afterDismount.round=3;$afterDismount.actors[1].native.grantSequence=2
$duplicateBefore=Snap 160;$duplicateBefore.round=4;$duplicateBefore.owners[0].state='Detached';$duplicateAfter=Copy-Value $duplicateBefore
$r1=[pscustomobject]@{stimulus='diagnostic cleanup callback at actual native StartRound return; no health/round/resource mutation';splitRound=3;callbackRound=4;releaseBefore=3;releaseAfter=3;independentRound=4;
    afterDismount=$afterDismount;independentMountTurn=[pscustomobject]@{actor=$ids[1];grantSequence=3};duplicateBefore=$duplicateBefore;duplicateAfter=$duplicateAfter}
$pendingBefore=Snap 190;$pendingBefore.round=4;$pendingBefore.actors[3].standard=3.0;$pendingAfter=Copy-Value $pendingBefore
$pending=[pscustomobject]@{stimulus='UnitEntityData.LeaveCombat then native RemoveUnit if still registered';before=$pendingBefore;after=$pendingAfter;participation=[pscustomobject]@{Activation=$null;RiderId=$null;MountId=$null;PendingSplitId=$ids[3];PendingSplitRound=4;Boundary=$null;Partner=$null;BoundaryIsCurrent=$false}}
$fault=[pscustomobject]@{failureCount=1;injectedPair='b';healthyNeighbourVisited=$true;before=(Copy-Value $pendingAfter);held=(Copy-Value $pendingAfter)}
$trace=[pscustomobject]@{events=$traceEvents;dropped=0;observationErrors=0;identityRegistry=[pscustomobject]@{contract='retained-native-object-identity-v1';capacity=16000;faults=0};observerHooks=@(@('06000C3C','06009120','0600838F','06000C46','0600939D')|ForEach-Object{[pscustomobject]@{token=$_;moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'}})}
$o=[pscustomobject]@{scenario=$request.scenario;actorIds=$ids;workingSha256=$request.fixture.working.sha256;targetId=$target;targetGroupId=('KMC.RuntimeHostile.'+$request.runId+':two-pair-target');
    fullRealtimeCombatProof=$false;naturalUnconsciousStimulusProven=$false;acquisition=[pscustomobject]@{nativeRangerCommitCount=4;horseFactRankAtCommit=1;horsePresentAtNativeCommit=$true};allocationTrace=$trace;
    nativeCompletion=[pscustomobject]@{errors=@();traceComplete=$true;observedActorIds=$ids;calls=$calls};nativeRounds=[pscustomobject]@{errors=@();token='06000BD3';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';events=@([pscustomobject]@{round=4})};
    nativeInitialOrder=@($ids[0],$ids[2],$target,$ids[3],$ids[1]);inputs=$inputs;activations=$activations;snapshots=$snapshots;r1=$r1;pendingSurvivor=$pending;localFailure=$fault;
    cleanup=[pscustomobject]@{pairResidueAbsent=$true;ownedHorseRemoved=$true;nativeCombatEnded=$true;initiativeInputsRestored=$true;firstPairAiRestored=$true;controlsSettled=$true;nativeProgressionRequiresExternalWorkingRestoration=$true;nativeSaveWrittenByThisDriver=$false;delegatedPersistenceCleanup=$false}}
Set-KmcChunk7NativeForfeitFixture $o
$map=[ordered]@{schemaVersion=1;evidenceKind='chunk7-combat-evidence';createdAtUtc='2026-10-12T00:00:00Z';status='PASS';errors=@();observations=$o}
foreach($field in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid')){$map[$field]=$request.$field};$good=Copy-Value ([pscustomobject]$map)
Check 'complete synthetic rider-first native combat evidence' {Assert-KmcChunk7CombatEvidence $request $good 'PASS'}
Check 'own mount approach carrying its melee rider into reach' {$v=Copy-Value $good;$attack=$v.observations.inputs[3];$attack.after.actors[1].move=0.5
    $v.observations.allocationTrace.events+=[pscustomobject]@{sequence=@($v.observations.allocationTrace.events).Count+1;boundary='admission-after';frame=$attack.before.frame+1;command=901;commandActor=$ids[1];commandType='Kingmaker.UnitLogic.Commands.UnitMoveTo';state=[pscustomobject]@{actor=$ids[1];standard=0.0;move=0.0;swift=0.0;reactions=1;reactionCooldown=0.0}}
    Assert-KmcChunk7CombatEvidence $request $v 'PASS'}
Check 'complete synthetic mount-first native combat evidence' {$r=Copy-Value $request;$v=Copy-Value $good;$r.scenario='chunk7-two-pair-combat-mount-first';$v.scenario=$r.scenario;$v.observations.scenario=$r.scenario;$v.observations.nativeInitialOrder=@($ids[1],$ids[3],$target,$ids[2],$ids[0]);Assert-KmcChunk7CombatEvidence $r $v 'PASS'}
$mutations=@(
    @('foreign fixture',{$args[0].observations.workingSha256='0'*64}),@('wrong initiative arrangement',{$args[0].observations.nativeInitialOrder=@($ids[1],$ids[3],$target,$ids[2],$ids[0])}),
    @('missing native unrelated target',{$args[0].observations.nativeInitialOrder=$ids}),@('trace dropped event',{$args[0].observations.allocationTrace.dropped=1}),
    @('native observer MVID drift',{$args[0].observations.nativeRounds.moduleMvid='0'*36}),@('native observer error',{$args[0].observations.nativeCompletion.errors=@('missing context')}),
    @('wrong primary resource owner',{$args[0].observations.inputs[3].outcome.resourceOwnerId=$ids[3]}),@('selection retained caster',{$args[0].observations.inputs[3].selectionAfterInput=$ids[0]}),
    @('duplicate native cost callback',{$v=$args[0];$e=Copy-Value $v.observations.allocationTrace.events[3];$e.sequence=$v.observations.allocationTrace.events.Count+1;$v.observations.allocationTrace.events+=,$e}),
    @('native cost omitted',{$args[0].observations.allocationTrace.events[3].boundary='observation-only'}),@('cost transferred actor',{$args[0].observations.allocationTrace.events[3].commandActor=$ids[3]}),
    @('native command changed type',{$args[0].observations.allocationTrace.events[3].commandType='KingmakerMountedCombat.Integration.MountedPairSingleAttack'}),
    @('neighbour debt changed',{$args[0].observations.inputs[3].after.actors[3].standard=3.0}),@('no physical displacement',{$args[0].observations.activations[0].movement.after.actors[1].position.z=0.0}),
    @('rider pays transport',{$args[0].observations.activations[0].movement.after.actors[0].move=0.25}),@('native actor end omitted',{$args[0].observations.nativeCompletion.calls=@($args[0].observations.nativeCompletion.calls|Select-Object -Skip 1)}),
    @('duplicate native renewal',{$args[0].observations.snapshots[0].actors[1].native.grantSequence=3}),@('TB charge admitted',{$args[0].observations.activations[0].tbChargeRefusal.castDelta=1}),
    @('R1 cleanup restarts release',{$args[0].observations.r1.releaseAfter=4}),@('R1 mount delayed',{$args[0].observations.r1.independentRound=5}),@('R1 changes neighbour owner',{$args[0].observations.r1.duplicateAfter.owners[1].generation=2}),
    @('R1 same-length neighbour mount substitution',{$args[0].observations.r1.duplicateAfter.owners[1].mountId=$ids[1].Replace('-','')}),
    @('partner moves without a native approach',{$args[0].observations.inputs[3].after.actors[1].move=0.5}),@('partner spends standard in the primary',{$args[0].observations.inputs[3].after.actors[1].standard=3.0}),
    @('pending forfeit absent',{$args[0].observations.allocationTrace.events=@($args[0].observations.allocationTrace.events|Where-Object{$_.boundary-notlike'completion-force-to-end-*'})}),@('pending mount move unforfeited',{$args[0].observations.pendingSurvivor.after.actors[3].move=0.0}),
    @('pending forfeit on the wrong actor',{foreach($e in @($args[0].observations.allocationTrace.events|Where-Object{$_.boundary-clike'completion-force-to-end-*'-and$_.state.actor-ceq$ids[3]})){$e.state.actor=$ids[1]}}),
    @('pending forfeit with an extra event',{$e=@($args[0].observations.allocationTrace.events|Where-Object{$_.boundary-ceq'turn-end-after'-and$_.state.actor-ceq$ids[2]})[0];$e.boundary='completion-force-to-end-after';$e.detail='setCooldowns=True'}),
    @('pending forfeit off its removal window',{foreach($e in @($args[0].observations.allocationTrace.events|Where-Object{$_.boundary-clike'remove-unit-*'})){$e.frame=$args[0].observations.pendingSurvivor.before.frame-10}}),
    @('pending neighbour expenditure',{$args[0].observations.pendingSurvivor.after.actors[0].standard=3.0}),
    @('pending forfeit not nested',{$e=@($args[0].observations.allocationTrace.events|Where-Object{$_.boundary-clike'completion-force-to-end-*'});$s=$e[1].sequence;$e[1].sequence=$e[3].sequence;$e[3].sequence=$s}),
    @('pending activation invented',{$args[0].observations.pendingSurvivor.participation.Activation=[pscustomobject]@{Sequence=1}}),@('pending debt lost',{$args[0].observations.pendingSurvivor.after.actors[3].standard=0.0}),
    @('failed cleanup custody dropped',{$args[0].observations.localFailure.held.owners=@($args[0].observations.localFailure.held.owners[0])}),@('neighbour cleanup skipped',{$args[0].observations.localFailure.healthyNeighbourVisited=$false}),
    @('health delivery overclaimed',{$args[0].observations.naturalUnconsciousStimulusProven=$true}),@('cleanup incomplete',{$args[0].observations.cleanup.nativeCombatEnded=$false})
)
foreach($mutation in $mutations){$bad=Copy-Value $good;& $mutation[1] $bad;Reject $mutation[0] {Assert-KmcChunk7CombatEvidence $request $bad 'PASS'}}
Check 'failed native artifact keeps bounded partial evidence' {$v=Copy-Value $good;$v.status='FAIL';$v.errors=@('native fixture unavailable');$v.observations=[pscustomobject]@{};Assert-KmcChunk7CombatEvidence $request $v 'FAIL'}
Reject 'failed artifact cannot fulfill PASS' {$v=Copy-Value $good;$v.status='FAIL';$v.errors=@('native fixture unavailable');Assert-KmcChunk7CombatEvidence $request $v 'PASS'}
Check 'closed combat rows remain disjoint' {foreach($scenario in @('chunk7-two-pair-combat-rider-first','chunk7-two-pair-combat-mount-first')){if((Get-KmcChunk7CombatRows $scenario).Count-ne25){throw 'Wrong declared row set.'}}}
Reject 'unknown native case' {Get-KmcChunk7CombatRows 'chunk7-two-pair-combat-anything'}
Check 'runtime rows contain both exact cases once' {$rows=@(Get-KmcChunk7CombatRuntimeRows);if($rows.Count-ne26-or@($rows|Select-Object -Unique).Count-ne26-or$rows-cnotcontains'chunk7-two-pair-combat-rider-first'-or$rows-cnotcontains'chunk7-two-pair-combat-mount-first'){throw 'Runtime row union differs.'}}
$artifactRoot=Join-Path $chunk7AnalysisRoot ('reader-artifact-fixture-'+[Guid]::NewGuid().ToString('N'));[IO.Directory]::CreateDirectory($artifactRoot)|Out-Null
$artifactPath=Join-Path $artifactRoot 'chunk7-combat-evidence.json';$request|Add-Member NoteProperty evidenceRoot $artifactRoot
[IO.File]::WriteAllText($artifactPath,($good|ConvertTo-Json -Depth 90),[Text.UTF8Encoding]::new($false))
$manifest=[pscustomobject]@{artifacts=@([pscustomobject]@{relativePath='chunk7-combat-evidence.json';kind='chunk7-combat-evidence';length=(Get-Item -LiteralPath $artifactPath).Length;sha256=(Get-KmcSha256 $artifactPath)})}
Check 'exact native artifact manifest and reader' {Assert-KmcChunk7CombatArtifact $request $manifest}
Reject 'combat artifact hash mismatch' {$v=Copy-Value $manifest;$v.artifacts[0].sha256='0'*64;Assert-KmcChunk7CombatArtifact $request $v}
Reject 'combat duplicate artifact record' {$v=Copy-Value $manifest;$v.artifacts+=,(Copy-Value $v.artifacts[0]);Assert-KmcChunk7CombatArtifact $request $v}
Reject 'combat artifact mismatched manifest role' {$v=Copy-Value $manifest;$v.artifacts[0].kind='foreign';Assert-KmcChunk7CombatArtifact $request $v}
Reject 'combat artifact missing for PASS' {Assert-KmcChunk7CombatArtifact $request ([pscustomobject]@{artifacts=@()})}
Check 'failed interrupted combat may omit artifact' {Assert-KmcChunk7CombatArtifact $request ([pscustomobject]@{artifacts=@()}) 'FAIL'}
Reject 'foreign scenario cannot include combat artifact' {$r=Copy-Value $request;$r.scenario='chunk7-two-pair-exploration';Assert-KmcChunk7CombatArtifact $r $manifest}
Check 'foreign scenario with no combat artifact is separate' {$r=Copy-Value $request;$r.scenario='chunk7-two-pair-exploration';Assert-KmcChunk7CombatArtifact $r ([pscustomobject]@{artifacts=@()})}
Write-Host ('RESULT '+$passed+' passed; 0 failed (synthetic reader checks only)')
