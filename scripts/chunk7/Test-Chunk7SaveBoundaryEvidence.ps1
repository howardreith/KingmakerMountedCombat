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
        $before=Row 'before-worker-save' 100;$before.requestedSaveObject=0;$before.authorizedWrites=0;$before.admittedWrites=0
        $held=Copy-Test $staged;$held.kind='worker-held-before-stop';$held.frame=110;$held.workerId=42;$held.workerRunning=$true;$held.workerHeld=$true;$held.workerLeaf='Manual_300_KMC_C7_WORLD.zks';$held.workerEntries=1;$held.workerHolds=1
        $one=Copy-Test $held;$one.kind='worker-stop-once';$one.draining=$true;$one.cancelDeferrals=1
        $two=Copy-Test $one;$two.kind='worker-stop-twice'
        $stable=Copy-Test $two;$stable.kind='worker-held-stable';$stable.frame=111;$stable.paused=$true;$stable.pauseDepth=1
        $after=Copy-Test $stable;$after.kind='worker-held-after-ticks';$after.frame=131
        $final=Row 'worker-drain-settled' 135;$final.snapshots=1;$final.drains=1;$final.cancelDeferrals=1;$final.drainCommitted=$true;$final.drainPath=$v.nativeWrite.path;$final.workerEntries=1;$final.workerHolds=1
        $rows=@($before,$held,$one,$two,$stable,$after,$final)
    }else{
        $before=Row 'pending-before-save' 100;$before.requestedSaveObject=0;$before.authorizedWrites=0;$before.admittedWrites=0;$before.owners=@($before.owners[0]);$before.boundaries[1].ownerId=$null;$before.boundaries[1].state='Unmounted';$before.boundaries[1].generation--;$before.boundaries[1].shellUnsettled=$true
        $before.shell.acted=$false;$before.shell.finished=$false;$before.shell.ownedUnsettled=$true;$before.actors[2].commandsEmpty=$false;$before.actors[2].moving=$true;$before.actors[2].position.x=6.0
        $waiting=Copy-Test $before;$waiting.kind='pending-save-deferred';$waiting.frame=101;$waiting.requestedSaveObject=30;$waiting.authorizedWrites=1;$waiting.admittedWrites=1;$waiting.deferred=1;$waiting.nativeWaiting=$true;$waiting.admissionFence=$true;foreach($b in $waiting.boundaries){$b.chargeFence=$true}
        $final=Row 'deferred-save-settled' 110;$final.snapshots=1;$final.deferred=1;$final.completed=1;$final.callback=$true
        $header.deferred=1;$staged.deferred=1;$continuing=Copy-Test $waiting;$continuing.kind='pending-save-continuing';$continuing.frame=102;$continuing.gameTicks=10000001L;$continuing.actors[2].position.x=5.75;
        $header.gameTicks=10000002L;$staged.gameTicks=$header.gameTicks;$final.gameTicks=$header.gameTicks;$arc.GameTimeTicks=$header.gameTicks;$v.snapshots[0].gameTicks=$header.gameTicks;$rows=@($before,$waiting,$continuing,$final)
    }
    $sb=[pscustomobject]@{case=$r.persistenceCase;rows=$rows;ordinaryNativeCallbackRequired=(-not$Worker);actualWorkerFaultProof=$false;manualResourceWrites=0;diagnosticsOffProof=$false;
        probe=[pscustomobject]@{contract='chunk7-selected-save-boundary-v1';installed=$true;starts=1;stages=1;complete=$true;events=@($header,$staged);errors=@();maximumEvents=64;prepareMetric='native-method-entries-including-production-refused-attempts';hooks=@(
            [pscustomobject]@{type='TurnBased.Controllers.TurnController';method='Prepare';token='06000C3C';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'},
            [pscustomobject]@{type='Kingmaker.Controllers.Combat.UnitCombatState';method='OnNewRound';token='0600939D';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'})};
        identities=[pscustomobject]@{capacity=128;faults=0;ids=@(1,2,3,4,10,11,12,13,20,21,30,31,32);released=$true;retainedAfterRelease=0}}
    if($Worker){$sb|Add-Member NoteProperty workerCachedAtHold $false}else{
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
        'different-worker' {$sb.rows[3].workerId=43}
        'not-live' {$sb.rows[1].workerRunning=$false}
        'released-on-second-stop' {$sb.rows[3].boundaries[1].saveSuspended=$false}
        'false-callback' {$bad.artifact.observations.nativeWrite.nativeCallback=$true}
        'unconfirmed-outcome' {$sb.rows[-1].drainOutcomeEstablished=$false}
        'other-output' {$sb.rows[-1].drainPath='C:\other.zks'}
        'early-hold-release' {$sb.rows[5].workerHeld=$false}
        'moving-world' {$sb.rows[5].actors[3].position.x=1.0}
        'clock-advanced' {$sb.rows[5].gameTicks++}
        'pause-not-restored' {$sb.rows[-1].paused=$true}
        'duplicate-drain' {$sb.rows[-1].drains=2}
        'extra-preparation' {$sb.rows[5].actors[0].prepared=$true}
        'no-real-ticks' {$sb.rows[5].frame=115}
    };Assert-KmcChunk7PersistenceObservation $bad.request $bad.artifact 'PASS'}
}
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
