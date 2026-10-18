$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Test-Chunk7PersistenceEvidence.ps1')
function New-SaveBoundaryFixture([bool]$Worker) {
    $r=Copy-Test $request;$a=Copy-Test $good;$v=$a.observations
    $r.persistenceCase=if($Worker){'multi-owned-worker-cancel-manual'}else{'multi-nonselected-mount-unsettled-manual'};$v.checkpoint=$r.persistenceCase
    $arc=$v.nativeWrite.archive
    if(-not$Worker){$arc.Slots=@($arc.Slots|Where-Object ActorId -CEQ $first);$v.snapshots[0].controls.ManagedHotbarSlotCount=2;$v.inputs[1].generationAtInit=$arc.Pairs[1].Generation-1}
    $v.nativeWrite.nativeCallback=-not$Worker;$v.nativeWrite|Add-Member NoteProperty path 'C:\private\Manual_300_KMC_C7_WORLD.zks'
    $actorRows=@();for($i=0;$i-lt4;$i++){$actorRows+=[pscustomobject]@{id=$ids[$i];object=10+$i;native=(Copy-Test $native[$i]);prepared=$false;position=[pscustomobject]@{x=0.0;y=0.0;z=0.0};commandsEmpty=$true;moving=$false}}
    $own=@();for($i=0;$i-lt2;$i++){$p=$arc.Pairs[$i];$own+=[pscustomobject]@{id=$p.PairId;generation=$p.Generation;riderId=$p.Rider.Id;mountId=$p.Mount.Id;boundaryObject=20+$i;state='Active'}}
    $bounds=@();for($i=0;$i-lt2;$i++){$p=$arc.Pairs[$i];$bounds+=[pscustomobject]@{object=20+$i;ownerId=$p.PairId;state='Mounted';generation=$p.Generation;shellUnsettled=$false;saveSuspended=$false;chargeFence=$false}}
    function Row([string]$Kind,[int]$Frame){
        $x=[pscustomobject]@{kind=$Kind;frame=$Frame;gameTicks=10000000L;gameObject=1;playerObject=2;stateObject=3;saveManagerObject=4;requestedSaveObject=30;authorityGeneration=1;
            selected=@($first);owners=(Copy-Test $own);boundaries=(Copy-Test $bounds);actors=(Copy-Test $actorRows);shell=$null;slots=(Copy-Test $arc.Slots);
            snapshots=0;deferred=0;completed=0;failed=0;cancelDeferrals=0;drains=0;draining=$false;activeScope=$false;admissionFence=$false;worldBusy=$false;saveSuspended=$false;controlsSuspended=$false;nativeWaiting=$false;callback=$false;nativeLoading=$false;manualLoadingScreen=$false;mode='Default';
            authorizedWrites=1;admittedWrites=1;workerId=-1;workerRunning=$false;workerHeld=$false;workerLeaf=$null;workerEntries=0;workerHolds=0;drainCommitted=$false;drainOutcomeEstablished=$true;drainPath=$null;paused=$false;pauseDepth=0;worldReleasePending=$false}
        if(-not$Worker){$x.shell=[pscustomobject]@{object=32;boundaryObject=21;control=$v.inputs[1].control;casterId=$second;targetId=$horse;generation=$arc.Pairs[1].Generation-1;processObject=0;contextObject=0;acted=$true;finished=$true;ownedUnsettled=$false;processEnded=$true}}
        $x
    }
    $header=Row 'header-starting' 104;$header.activeScope=$true;$header.admissionFence=$true;foreach($b in $header.boundaries){$b.chargeFence=$true}
    $staged=Copy-Test $header;$staged.kind='header-staged';$staged.snapshots=1;$staged.saveSuspended=$true;$staged.controlsSuspended=$true;$staged.worldBusy=$true;$staged.slots=@();foreach($b in $staged.boundaries){$b.saveSuspended=$true}
    if($Worker){
        $v.nativeWrite.snapshotCount=2;$v.snapshots[0].snapshots=2
        $seedHeader=Copy-Test $header;$seedStaged=Copy-Test $staged
        $header.snapshots=1;$header.authorizedWrites=2;$header.admittedWrites=2;$header.completed=1;$header.frame=108;$header.requestedSaveObject=31;$header.workerEntries=1
        $staged.snapshots=2;$staged.authorizedWrites=2;$staged.admittedWrites=2;$staged.completed=1;$staged.frame=108;$staged.requestedSaveObject=31;$staged.workerEntries=1
        $done=Row 'worker-seed-complete' 106;$done.snapshots=1;$done.completed=1;$done.callback=$true;$done.workerEntries=1
        $replacementRequest=Copy-Test $done;$replacementRequest.kind='worker-replacement-request';$replacementRequest.callback=$false;$replacementRequest.requestedSaveObject=31
        $before=Row 'before-worker-save' 100;$before.requestedSaveObject=0;$before.authorizedWrites=0;$before.admittedWrites=0
        $held=Copy-Test $staged;$held.kind='worker-held-before-stop';$held.frame=110;$held.workerId=42;$held.workerRunning=$true;$held.workerHeld=$true;$held.workerLeaf='Manual_301_KMC_C7_WORLD.zks';$held.workerEntries=2;$held.workerHolds=1
        $one=Copy-Test $held;$one.kind='worker-stop-once';$one.draining=$true;$one.cancelDeferrals=1
        $two=Copy-Test $one;$two.kind='worker-stop-twice'
        $stable=Copy-Test $two;$stable.kind='worker-held-stable';$stable.frame=111;$stable.paused=$true;$stable.pauseDepth=1
        $after=Copy-Test $stable;$after.kind='worker-held-after-ticks';$after.frame=131
        $final=Row 'worker-drain-settled' 135;$final.snapshots=2;$final.completed=1;$final.authorizedWrites=2;$final.admittedWrites=2;$final.requestedSaveObject=31;$final.drains=1;$final.cancelDeferrals=1;$final.drainCommitted=$true;$final.drainPath=$v.nativeWrite.path;$final.workerEntries=2;$final.workerHolds=1
        $rows=@($before,$done,$replacementRequest,$held,$one,$two,$stable,$after,$final)
    }else{
        $before=Row 'pending-before-save' 100;$before.requestedSaveObject=0;$before.authorizedWrites=0;$before.admittedWrites=0;$before.owners=@($before.owners[0]);$before.boundaries[1].ownerId=$null;$before.boundaries[1].state='Unmounted';$before.boundaries[1].generation--;$before.boundaries[1].shellUnsettled=$true
        $before.shell.acted=$false;$before.shell.finished=$false;$before.shell.ownedUnsettled=$true;$before.actors[2].commandsEmpty=$false;$before.actors[2].moving=$true;$before.actors[2].position.x=6.0
        $waiting=Copy-Test $before;$waiting.kind='pending-save-deferred';$waiting.frame=101;$waiting.requestedSaveObject=30;$waiting.authorizedWrites=1;$waiting.admittedWrites=1;$waiting.deferred=1;$waiting.nativeWaiting=$true;$waiting.admissionFence=$true;foreach($b in $waiting.boundaries){$b.chargeFence=$true}
        $final=Row 'deferred-save-settled' 110;$final.snapshots=1;$final.deferred=1;$final.completed=1;$final.callback=$true
        $header.deferred=1;$staged.deferred=1;$continuing=Copy-Test $waiting;$continuing.kind='pending-save-continuing';$continuing.frame=102;$continuing.gameTicks=10000001L;$continuing.actors[2].position.x=5.75;
        $header.gameTicks=10000002L;$staged.gameTicks=$header.gameTicks;$final.gameTicks=$header.gameTicks;$arc.GameTimeTicks=$header.gameTicks;$v.snapshots[0].gameTicks=$header.gameTicks;$rows=@($before,$waiting,$continuing,$final)
    }
    $sb=[pscustomobject]@{case=$r.persistenceCase;rows=$rows;ordinaryNativeCallbackRequired=(-not$Worker);seedNativeCallbackRequired=$Worker;actualWorkerFaultProof=$false;manualResourceWrites=0;diagnosticsOffProof=$false;
        probe=[pscustomobject]@{contract='chunk7-selected-save-boundary-v1';installed=$true;expectedSnapshots=$(if($Worker){2}else{1});starts=$(if($Worker){2}else{1});stages=$(if($Worker){2}else{1});complete=$true;events=$(if($Worker){@($seedHeader,$seedStaged,$header,$staged)}else{@($header,$staged)});errors=@();maximumEvents=64;prepareMetric='native-method-entries-including-production-refused-attempts';hooks=@(
            [pscustomobject]@{type='TurnBased.Controllers.TurnController';method='Prepare';token='06000C3C';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'},
            [pscustomobject]@{type='Kingmaker.Controllers.Combat.UnitCombatState';method='OnNewRound';token='0600939D';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'})};
        identities=[pscustomobject]@{capacity=128;faults=0;ids=@(1,2,3,4,10,11,12,13,20,21,30,31,32);released=$true;retainedAfterRelease=0}}
    if($Worker){
        $sb|Add-Member NoteProperty workerCachedAtHold $false
        function Descriptor([int]$Object,$Leaf,$GameId,[bool]$Complete){[pscustomobject]@{object=$Object;fileName=$Leaf;nativeName='KMC_C7_WORLD';nativeType='Manual';gameId=$GameId;gameName=$r.fixture.working.gameName;area=$arc.AreaId;hasFileOnDisk=$Complete;isActuallySaved=$Complete;operation='None';autoLevelup=$false}}
        $seed=[pscustomobject]@{requested=(Descriptor 30 $null $null $false);prepared=(Descriptor 31 'Manual_300_KMC_C7_WORLD.zks' $arc.CampaignId $true);path=$v.nativeWrite.path;sha256='6'*64;length=1200L;callbackCount=1;completionSequence=1;snapshotCount=1;nativeWorkerSettled=$true}
        $replacement=[pscustomobject]@{requested=(Copy-Test $seed.prepared);stagingLeaf='Manual_301_KMC_C7_WORLD.zks';landedLeaf='Manual_300_KMC_C7_WORLD.zks';seedSha256=$seed.sha256;seedLength=$seed.length;number=301;callbackCount=0;prepared=(Descriptor 32 'Manual_301_KMC_C7_WORLD.zks' $arc.CampaignId $true);settled=(Descriptor 32 'Manual_300_KMC_C7_WORLD.zks' $arc.CampaignId $true);completionCount=1;snapshotCount=2;drainCount=1;cancelCount=1;committed=$true;outcomeEstablished=$true;path=$v.nativeWrite.path}
        $sb|Add-Member NoteProperty workerSeed $seed;$sb|Add-Member NoteProperty workerReplacement $replacement
    }else{
        $sb|Add-Member NoteProperty pendingApproachDisplacement 0.5
        $sb|Add-Member NoteProperty positioning ([pscustomobject]@{actorId=$second;commandObject=31;commandType='Kingmaker.UnitLogic.Commands.UnitMoveTo';otherCommandsUnchanged=$true;terminal=$true;distance=6.5;mountDistance=6.5;
          start=[pscustomobject]@{x=0.0;y=0.0;z=0.0};goal=[pscustomobject]@{x=6.5;y=0.0;z=0.0};end=[pscustomobject]@{x=6.5;y=0.0;z=0.0};mountPosition=[pscustomobject]@{x=0.0;y=0.0;z=0.0}})
    }
    $v|Add-Member NoteProperty saveBoundary $sb
    [pscustomobject]@{request=$r;artifact=$a}
}
foreach($worker in @($false,$true)) {
    $f=New-SaveBoundaryFixture $worker
    Check ('selected source native save boundary '+$worker) {Assert-KmcChunk7PersistenceObservation $f.request $f.artifact 'PASS'}
    foreach($mutation in @('numeric-generation','boolean-text','swapped-actor','swapped-world','wrong-request','other-selected','missing-owner','released-neighbour','extra-header','native-prepare','early-header','wrong-debt','wrong-native-hook','identity-collision','identity-retained','extra-write','wrong-slots','invented-resource','fault-overclaim')) {
        Reject ('save-boundary-'+$worker+'-'+$mutation) {$bad=Copy-Test $f;$sb=$bad.artifact.observations.saveBoundary;$row=$sb.probe.events[0];switch($mutation){
            'numeric-generation' {$row.owners[0].generation=[string]$row.owners[0].generation}
            'boolean-text' {$row.activeScope='true'}
            'swapped-actor' {$row.actors[0].object=$row.actors[1].object}
            'swapped-world' {$row.gameObject=$row.stateObject}
            'wrong-request' {$row.requestedSaveObject=31}
            'other-selected' {$row.selected=@($second)}
            'missing-owner' {$row.owners=@($row.owners[0])}
            'released-neighbour' {$sb.probe.events[1].boundaries[1].saveSuspended=$false}
            'extra-header' {$sb.probe.events+=Copy-Test $row}
            'native-prepare' {$sb.probe.events+=[pscustomobject]@{kind='prepare-before';frame=104;actorId=$horse}}
            'early-header' {$row.frame=99}
            'wrong-debt' {$row.actors[1].native.Standard=0.0}
            'wrong-native-hook' {$sb.probe.hooks[0].moduleMvid=$bad.request.dllMvid}
            'identity-collision' {$sb.identities.faults=1}
            'identity-retained' {$sb.identities.retainedAfterRelease=1}
            'extra-write' {$row.admittedWrites=2}
            'wrong-slots' {$sb.rows[-1].slots=@()}
            'invented-resource' {$sb.manualResourceWrites=1}
            'fault-overclaim' {$sb.actualWorkerFaultProof=$true}
        };Assert-KmcChunk7PersistenceObservation $bad.request $bad.artifact 'PASS'}
    }
}
$pending=New-SaveBoundaryFixture $false
foreach($mutation in @('already-mounted','not-waiting','snapshot-before-settlement','no-approach','unregistered-shell','foreign-context','changed-control','unfinished-at-header','invented-hotbar','second-not-fenced','invented-distance','stationary-pending','native-loading-wait','manual-screen-wait','saving-mode-wait','paused-wait','no-post-admission-motion','no-post-admission-clock','foreign-continued-shell')){
    Reject $mutation {$bad=Copy-Test $pending;$sb=$bad.artifact.observations.saveBoundary;switch($mutation){
        'already-mounted' {$sb.rows[0].boundaries[1].ownerId=$bad.artifact.observations.nativeWrite.archive.Pairs[1].PairId}
        'not-waiting' {$sb.rows[1].nativeWaiting=$false}
        'snapshot-before-settlement' {$sb.rows[1].snapshots=1}
        'no-approach' {$sb.pendingApproachDisplacement=0.0}
        'unregistered-shell' {$sb.rows[0].shell.object=999}
        'foreign-context' {$sb.rows[1].shell.contextObject=12}
        'changed-control' {$sb.probe.events[0].shell.control='other-shell'}
        'unfinished-at-header' {$sb.probe.events[0].shell.finished=$false}
        'invented-hotbar' {$bad.artifact.observations.nativeWrite.archive.Slots+=[pscustomobject]@{ActorId=$second;Index=12;Kind=2}}
        'second-not-fenced' {$sb.rows[1].boundaries[1].chargeFence=$false}
        'invented-distance' {$sb.positioning.end.x=0.0}
        'stationary-pending' {$sb.rows[0].actors[2].moving=$false}
        'native-loading-wait' {$sb.rows[1].nativeLoading=$true}
        'manual-screen-wait' {$sb.rows[2].manualLoadingScreen=$true}
        'saving-mode-wait' {$sb.rows[1].mode='Saving'}
        'paused-wait' {$sb.rows[1].paused=$true}
        'no-post-admission-motion' {$sb.rows[2].actors[2].position=Copy-Test $sb.rows[1].actors[2].position}
        'no-post-admission-clock' {$sb.rows[2].gameTicks=$sb.rows[1].gameTicks}
        'foreign-continued-shell' {$sb.rows[2].shell.control='foreign-control'}
    };Assert-KmcChunk7PersistenceObservation $bad.request $bad.artifact 'PASS'}
}
$worker=New-SaveBoundaryFixture $true
foreach($mutation in @('different-worker','not-live','released-on-second-stop','false-callback','unconfirmed-outcome','other-output','early-hold-release','moving-world','clock-advanced','pause-not-restored','duplicate-drain','extra-preparation','no-real-ticks')){
    Reject $mutation {$bad=Copy-Test $worker;$sb=$bad.artifact.observations.saveBoundary;switch($mutation){
        'different-worker' {$sb.rows[5].workerId=43}
        'not-live' {$sb.rows[3].workerRunning=$false}
        'released-on-second-stop' {$sb.rows[5].boundaries[1].saveSuspended=$false}
        'false-callback' {$bad.artifact.observations.nativeWrite.nativeCallback=$true}
        'unconfirmed-outcome' {$sb.rows[-1].drainOutcomeEstablished=$false}
        'other-output' {$sb.rows[-1].drainPath='C:\other.zks'}
        'early-hold-release' {$sb.rows[7].workerHeld=$false}
        'moving-world' {$sb.rows[7].actors[3].position.x=1.0}
        'clock-advanced' {$sb.rows[7].gameTicks++}
        'pause-not-restored' {$sb.rows[-1].paused=$true}
        'duplicate-drain' {$sb.rows[-1].drains=2}
        'extra-preparation' {$sb.rows[7].actors[0].prepared=$true}
        'no-real-ticks' {$sb.rows[7].frame=115}
    };Assert-KmcChunk7PersistenceObservation $bad.request $bad.artifact 'PASS'}
}

foreach($mutation in @('one-write-old-shape','no-seed-callback','extra-seed-callback','replacement-callback','seed-fresh-request-alias','replacement-request-alias','prepared-world-alias','prepared-object-switch','bad-seed-hash','bad-stage','seed-resource-refund','seed-round','seed-owner-change','early-second-header','unfinished-seed','seed-type-text','extra-total-write')) {
 Reject ('two-native-operation-'+$mutation) {$bad=Copy-Test $worker;$sb=$bad.artifact.observations.saveBoundary;switch($mutation){
  'one-write-old-shape' {$bad.artifact.observations.nativeWrite.snapshotCount=1}
  'no-seed-callback' {$sb.workerSeed.callbackCount=0}
  'extra-seed-callback' {$sb.workerSeed.callbackCount=2}
  'replacement-callback' {$sb.workerReplacement.callbackCount=1}
  'seed-fresh-request-alias' {$sb.workerSeed.requested.object=$sb.workerSeed.prepared.object}
  'replacement-request-alias' {$sb.workerReplacement.requested.object=$sb.workerReplacement.prepared.object}
  'prepared-world-alias' {$sb.workerReplacement.prepared.object=$sb.rows[0].gameObject;$sb.workerReplacement.settled.object=$sb.rows[0].gameObject}
  'prepared-object-switch' {$sb.workerReplacement.settled.object=$sb.workerSeed.prepared.object}
  'bad-seed-hash' {$sb.workerReplacement.seedSha256='7'*64}
  'bad-stage' {$sb.workerReplacement.stagingLeaf=$sb.workerReplacement.landedLeaf}
  'seed-resource-refund' {$sb.rows[1].actors[0].native.Standard=99.0}
  'seed-round' {$sb.rows[1].actors[0].prepared=$true}
  'seed-owner-change' {$sb.rows[1].owners[1].generation++}
  'early-second-header' {$sb.probe.events[2].frame=$sb.rows[0].frame}
  'unfinished-seed' {$sb.rows[1].activeScope=$true}
  'seed-type-text' {$sb.workerSeed.callbackCount='1'}
  'extra-total-write' {$sb.rows[-1].authorizedWrites=3}
 };Assert-KmcChunk7PersistenceObservation $bad.request $bad.artifact 'PASS'}
}
Check 'elapsed native cooldown decay between seed and replacement remains legal' {$f=New-SaveBoundaryFixture $true;$sb=$f.artifact.observations.saveBoundary;foreach($row in @($sb.rows[0],$sb.probe.events[0],$sb.probe.events[1])){$row.gameTicks=0L;foreach($x in $row.actors){foreach($field in @('Standard','Move')){$x.native.$field+=1.0}}};Assert-KmcChunk7PersistenceObservation $f.request $f.artifact 'PASS'}
# The same exact committed output has an independent ordinary cold-load reader;
# cancellation is never promoted to native callback success.
foreach($source in @($pending,$worker)) {
    Check ('selected cold schema3 output '+$source.request.persistenceCase) {
        $q=Copy-Test $coldRequest;$q.persistenceCase=$source.request.persistenceCase;$a=Copy-Test $cold;$a.observations.checkpoint=$q.persistenceCase
        if($q.persistenceCase-ceq'multi-nonselected-mount-unsettled-manual'){$a.observations.loadedArchive.Slots=@($a.observations.loadedArchive.Slots|Where-Object ActorId -CEQ $first);$a.observations.snapshots[0].controls.ManagedHotbarSlotCount=2}
        Assert-KmcChunk7PersistenceObservation $q $a 'PASS'
    }
}
Write-Host ('CHUNK7 SAVE BOUNDARY READER PASS='+$passed+' FAIL=0; synthetic exact-boundary evidence only, no native qualification.')

# Exact owned source cases reuse the full inherited two-Horse shape; these remain JSON-only observations.
$ownedFixturePath=Join-Path $chunk7TestSource 'OwnedPairsEvidenceFixtures.json'
$ownedCore=@(([IO.File]::ReadAllText($ownedFixturePath)|ConvertFrom-Json).fixtures|Where-Object name -CEQ 'persistence-save-multi-exploration-manual')[0]
function New-OwnedSaveBoundaryFixture([bool]$Worker){
 $f=New-SaveBoundaryFixture $Worker;$r=Copy-Test $ownedCore.request;$a=Copy-Test $ownedCore.artifact
 $r.persistenceCase=$f.request.persistenceCase;$r.ownedPairs.expectedNativeWriteCount=if($Worker){2}else{1}
 $raw=$f.artifact.observations|ConvertTo-Json -Depth 100
 $newIds=@($r.ownedPairs.riderAId,$r.ownedPairs.mountAId,$r.ownedPairs.riderBId,$r.ownedPairs.mountBId)
 for($i=0;$i-lt4;$i++){$raw=$raw.Replace($ids[$i],$newIds[$i])}
 $raw=$raw.Replace($f.request.fixture.working.gameId,$r.persistenceLoad.gameId).Replace('Manual_300_KMC_C7_WORLD.zks','Manual_43_KMC_C7_WORLD.zks').Replace('Manual_301_KMC_C7_WORLD.zks','Manual_44_KMC_C7_WORLD.zks')
 $v=$raw|ConvertFrom-Json;$v.sourceWorkingSha256=$null;$v.nativeProgressionRequiresExternalWorkingRestoration=$false;$v.nativeWrite.archive.Pairs[0].ProfileId='medium-humanoid-horse-v1'
 foreach($key in @('acquisition','cleanup','inheritedFixture','inheritedCleanup','inheritedManualSha256','ownedOutputAdmission','ownedOutputCompletion')){$v|Add-Member NoteProperty $key (Copy-Test $ownedCore.artifact.observations.$key) -Force}
 $v.nativeWrite.path=$ownedCore.artifact.observations.nativeWrite.path;$v.nativeWrite|Add-Member NoteProperty nativeName 'KMC_C7_WORLD' -Force
 foreach($key in @('ownedOutputAdmission','ownedOutputCompletion')){$v.$key.authority.writeCap=$r.ownedPairs.expectedNativeWriteCount}
 $v.ownedOutputCompletion.authority.nativeWrites=$r.ownedPairs.expectedNativeWriteCount;$v.ownedOutputCompletion.authority.authorizedWrites=$r.ownedPairs.expectedNativeWriteCount
 if($Worker){$sb=$v.saveBoundary;$sb.workerSeed.path=$v.nativeWrite.path;$sb.workerReplacement.path=$v.nativeWrite.path;$sb.workerReplacement.number=44;$sb.rows[-1].drainPath=$v.nativeWrite.path
  $v.ownedOutputCompletion|Add-Member NoteProperty replacement ([pscustomobject]@{callbackCount=0;completedSaveCount=1;drainCount=1;committed=$true;outcomeEstablished=$true;path=$v.nativeWrite.path;stagingLeaf='Manual_44_KMC_C7_WORLD.zks'})
 }
 $a.observations=$v;[pscustomobject]@{request=$r;artifact=$a}
}
foreach($worker in @($false,$true)){
 $f=New-OwnedSaveBoundaryFixture $worker
 Check ('owned two-Horse save boundary '+$worker){Assert-KmcChunk7PersistenceObservation $f.request $f.artifact 'PASS'}
 foreach($mutation in @('source-quota','write-cap','foreign-actor','owned-fact-removal','prior-pet-changed','changed-source-hash','cold-alias','checkpoint-array')){
  Reject ('owned boundary '+$worker+' '+$mutation){$bad=Copy-Test $f;switch($mutation){
   'source-quota'{$bad.request.ownedPairs.expectedNativeWriteCount=3}
   'write-cap'{$bad.artifact.observations.ownedOutputCompletion.authority.writeCap=3}
   'foreign-actor'{$bad.artifact.observations.saveBoundary.probe.events[0].actors[0].id=$first}
   'owned-fact-removal'{$bad.artifact.observations.inheritedCleanup.ownedFactRemovalCount=1}
   'prior-pet-changed'{$bad.artifact.observations.inheritedCleanup.pairs[0].mountId=$horse}
   'changed-source-hash'{$bad.artifact.observations.inheritedManualSha256='9'*64}
   'cold-alias'{$bad.request.ownedPairs.case='persistence-load';$bad.request.ownedPairs.expectedNativeWriteCount=0;$bad.request.ownedPairs.checkpointSourceRunId='owned-checkpoint';$bad.request.ownedPairs.checkpointSourceArtifactSha256='8'*64}
   'checkpoint-array'{$bad.artifact.observations.checkpoint=@($bad.artifact.observations.checkpoint)}
  };Assert-KmcChunk7PersistenceObservation $bad.request $bad.artifact 'PASS'}
 }
 if($worker){foreach($mutation in @('seed-callback','replacement-callback','staging-leaf','completion-count')){Reject ('owned replacement '+$mutation){$bad=Copy-Test $f;$done=$bad.artifact.observations.ownedOutputCompletion;switch($mutation){'seed-callback'{$done.callbackCount=0};'replacement-callback'{$done.replacement.callbackCount=1};'staging-leaf'{$done.replacement.stagingLeaf=$bad.request.persistenceLoad.fileName};'completion-count'{$done.replacement.completedSaveCount=2}};Assert-KmcChunk7PersistenceObservation $bad.request $bad.artifact 'PASS'}}}
}
Write-Host ('OWNED SAVE BOUNDARY JSON READER PASS='+$passed+' FAIL=0; no native or archive execution')

foreach($owned in @($false,$true)){
 $f=if($owned){New-OwnedSaveBoundaryFixture $true}else{New-SaveBoundaryFixture $true}
 foreach($token in @('seed-kind','replacement-kind','case','contract','prepareMetric','type','method','token','moduleMvid','seed-header-kind','replacement-header-kind')){
  Reject ('exact scalar save-boundary token owned='+$owned+' '+$token){$bad=Copy-Test $f;$v=$bad.artifact.observations.saveBoundary;switch($token){
   'seed-kind'{$v.rows[1].kind=@($v.rows[1].kind)}
   'replacement-kind'{$v.rows[2].kind=@($v.rows[2].kind)}
   'case'{$v.case=@($v.case)}
   'contract'{$v.probe.contract=@($v.probe.contract)}
   'prepareMetric'{$v.probe.prepareMetric=@($v.probe.prepareMetric)}
   'seed-header-kind'{$v.probe.events[0].kind=@($v.probe.events[0].kind)}
   'replacement-header-kind'{$v.probe.events[2].kind=@($v.probe.events[2].kind)}
   default{$v.probe.hooks[0].$token=@($v.probe.hooks[0].$token)}
  };Assert-KmcChunk7PersistenceObservation $bad.request $bad.artifact 'PASS'}
 }
}
Write-Host ('SAVE BOUNDARY FINAL READER PASS='+$passed+' FAIL=0; JSON-only native observations, no native/archive calls.')

foreach($owned in @($false,$true)){
 $f=if($owned){New-OwnedSaveBoundaryFixture $true}else{New-SaveBoundaryFixture $true}
 foreach($pair in @(@($true,$false),@($false,$true),@($false,$false))){Reject ('native prepared filename/flags owned='+$owned+' '+$pair[0]+'/'+$pair[1]){$bad=Copy-Test $f;$d=$bad.artifact.observations.saveBoundary.workerReplacement.prepared;$d.hasFileOnDisk=$pair[0];$d.isActuallySaved=$pair[1];Assert-KmcChunk7PersistenceObservation $bad.request $bad.artifact 'PASS'}}
}
Write-Host ('SAVE BOUNDARY FINAL NATIVE SHAPE READER PASS='+$passed+' FAIL=0; JSON-only native observations, no native/archive calls.')

# Owner-seed connection: reuse the existing generated boundary observations and
# the established restored-start template, preserving the measured worker/shell
# invariants. This remains generated JSON, never a native recording.
$fixtures=([IO.File]::ReadAllText($ownedFixturePath)|ConvertFrom-Json).fixtures
function Copy-Value($v){Copy-Test $v}
$seedFixtureTokens=$null;$seedFixtureErrors=$null;$seedFixtureAst=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'Test-Chunk7OwnedPairsEvidence.ps1'),[ref]$seedFixtureTokens,[ref]$seedFixtureErrors)
foreach($name in @('New-KmcOwnerSeedCheckpointSelection','Set-KmcOwnerSeedDerivedLabels','New-KmcOwnerSeedCheckpointSaveFixture')){$definition=$seedFixtureAst.Find({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq$name},$true);if($null-eq$definition){throw 'Missing established owner-seed fixture helper.'};. ([scriptblock]::Create($definition.Extent.Text))}
. (Join-Path $chunk7TestSource 'OwnerSeedCheckpointPreparationFixtures.ps1')
function New-OwnerSeedSaveBoundaryFixture([bool]$Worker){
 $f=New-OwnedSaveBoundaryFixture $Worker;$r=$f.request;$o=$f.artifact.observations;$oldInput=$r.persistenceLoad.fileName
 $seed=New-KmcOwnerSeedCheckpointSaveFixture;$r.ownedPairs=Copy-Test $seed.request.ownedPairs;$r.ownedPairs.sourceArchiveSha256=$r.persistenceLoad.sha256;$r.ownedPairs.expectedNativeWriteCount=if($Worker){2}else{1}
 $r.persistenceLoad.internalName='KMC_C7_SEED_WORLD';$r.persistenceLoad.fileName='Manual_303_KMC_C7_SEED_WORLD.zks'
 $raw=$o|ConvertTo-Json -Depth 100;$raw=$raw.Replace('Manual_43_KMC_C7_WORLD.zks','Manual_304_KMC_C7_SEED_CHECKPOINT.zks').Replace('Manual_44_KMC_C7_WORLD.zks','Manual_305_KMC_C7_SEED_CHECKPOINT.zks').Replace($oldInput,$r.persistenceLoad.fileName).Replace('"KMC_C7_WORLD"','"KMC_C7_SEED_CHECKPOINT"');$o=$raw|ConvertFrom-Json;$f.artifact.observations=$o
 Set-KmcOwnerSeedDerivedLabels $o $r.ownedPairs;$o.PSObject.Properties.Remove('acquisition')
 foreach($row in @($o.ownedOutputAdmission.nativeInventory)+@($o.ownedOutputCompletion.nativeInventory)){if($row.fileName-ceq$r.persistenceLoad.fileName){$row.nativeName=$r.persistenceLoad.internalName}}
 $o.ownedOutputAdmission.number=304;foreach($auth in @($o.ownedOutputAdmission.authority,$o.ownedOutputCompletion.authority)){$auth.number=304}
 $o|Add-Member mountedStart (Copy-Test $seed.artifact.observations.mountedStart) -Force
 $o|Add-Member ownerSeedCheckpointSource (Copy-Test $seed.artifact.observations.ownerSeedCheckpointSource) -Force;$o.ownerSeedCheckpointSource.checkpoint=$r.persistenceCase;$o.ownerSeedCheckpointSource.manualSha256=$r.ownedPairs.sourceArchiveSha256
 if($Worker){
  $o.inputs=@();$o.saveBoundary.workerReplacement.number=305
  $before=Copy-Test $o.snapshots[0];$before.label='before-native-save';$before.snapshots=0;$before.nativeCastRequests=0;$o.snapshots=@($before)+@($o.snapshots)
 }else{
  $o.inputs=@($o.inputs[-1]);Set-KmcOwnerSeedNativePreparationFixture $f $true
 }
 $f
}
foreach($worker in @($false,$true)){
 $f=New-OwnerSeedSaveBoundaryFixture $worker
 Check ('owner-seed actual boundary reader '+$worker){Assert-KmcChunk7PersistenceObservation $f.request $f.artifact 'PASS'}
 Check ('owner-seed boundary exact landed inventory '+$worker){Assert-KmcChunk7OwnedFinalSaveRoot $f.request @($f.request.persistenceLoad.fileName,'Manual_304_KMC_C7_SEED_CHECKPOINT.zks') $f.artifact.observations}
 foreach($mutation in @('extra-staging','missing-output','missing-input','unexpected-file','duplicate-input')){
  Reject ('owner-seed final inventory '+$worker+' '+$mutation){$leaves=@($f.request.persistenceLoad.fileName,'Manual_304_KMC_C7_SEED_CHECKPOINT.zks');switch($mutation){'extra-staging'{$leaves+='Manual_305_KMC_C7_SEED_CHECKPOINT.zks'};'missing-output'{$leaves=@($leaves[0])};'missing-input'{$leaves=@($leaves[1])};'unexpected-file'{$leaves+='stray.tmp'};'duplicate-input'{$leaves=@($leaves[0],$leaves[0])}};Assert-KmcChunk7OwnedFinalSaveRoot $f.request $leaves $f.artifact.observations}
 }
 foreach($mutation in @('wrong-quota','cold-source','wrong-output-name','old-input-name')){
  Reject ('owner-seed boundary source guards '+$worker+' '+$mutation){$bad=Copy-Test $f;switch($mutation){'wrong-quota'{$bad.request.ownedPairs.expectedNativeWriteCount=3};'cold-source'{$bad.request.ownedPairs.case='owner-seed-checkpoint-load';$bad.request.ownedPairs.expectedNativeWriteCount=0};'wrong-output-name'{$bad.artifact.observations.nativeWrite.nativeName='KMC_C7_WORLD'};'old-input-name'{$bad.request.persistenceLoad.internalName='KMC_C7_ACQUIRED_FIELD'}};Assert-KmcChunk7PersistenceObservation $bad.request $bad.artifact 'PASS'}
 }
 if($worker){foreach($mutation in @('replacement-callback','third-request','wrong-staging-name')){Reject ('owner-seed worker exact replacement '+$mutation){$bad=Copy-Test $f;switch($mutation){'replacement-callback'{$bad.artifact.observations.ownedOutputCompletion.replacement.callbackCount=1};'third-request'{$bad.artifact.observations.ownedOutputCompletion.authority.nativeWrites=3};'wrong-staging-name'{$bad.artifact.observations.saveBoundary.workerReplacement.stagingLeaf='Manual_305_KMC_C7_WORLD.zks'}};Assert-KmcChunk7PersistenceObservation $bad.request $bad.artifact 'PASS'}}}
 else{foreach($mutation in @('retained-B','omitted-preparation','wrong-remounted-owner','wrong-remounted-generation','remounted-role-array','slot-kind-string','extra-B-control','command-still-pending')){Reject ('owner-seed pending preparation '+$mutation){$bad=Copy-Test $f;$p=$bad.artifact.observations.ownerSeedCheckpointPreparation;switch($mutation){'retained-B'{$p.after.secondMountOwnerAbsent=$false};'omitted-preparation'{$bad.artifact.observations.PSObject.Properties.Remove('ownerSeedCheckpointPreparation')};'wrong-remounted-owner'{$p.remountedSecondOwner.ownerId=$p.before.owners[1].ownerId};'wrong-remounted-generation'{$p.remountedSecondOwner.generation++};'remounted-role-array'{$p.remountedSecondOwner.riderId=@($p.remountedSecondOwner.riderId)};'slot-kind-string'{$p.slotsAfter[0].Kind=[string]$p.slotsAfter[0].Kind};'extra-B-control'{$p.slotsAfter[0].ActorId=$bad.request.ownedPairs.riderBId};'command-still-pending'{$p.after.secondPairCommandsRetained=$true}};Assert-KmcChunk7PersistenceObservation $bad.request $bad.artifact 'PASS'}}}
}
Write-Host ('OWNER-SEED SAVE BOUNDARY CONNECTION PASS='+$passed+' FAIL=0; generated JSON only; final replacement inventory has two archives after two native requests.')
