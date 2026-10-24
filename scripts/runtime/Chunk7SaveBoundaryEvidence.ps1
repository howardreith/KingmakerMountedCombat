# Exact new save-boundary rows only. Historical P04/P07 readers are unchanged.
Set-StrictMode -Version Latest
function Test-KmcChunk7SaveBoundaryCase([string]$Case) { $Case-cin@('multi-nonselected-mount-unsettled-manual','multi-owned-worker-cancel-manual') }
function Assert-KmcChunk7SaveBoundaryObject($Value,[bool]$Nullable=$false) {
    if(-not(Test-KmcExactJsonInteger $Value)-or$Value-lt[int]::MinValue-or$Value-gt[int]::MaxValue-or(-not$Nullable-and$Value-eq0)){throw 'Save-boundary object label is not a signed nonzero int32.'}
}
function Assert-KmcChunk7SaveBoundarySource($Request,$Observation,$Archive) {
    $owned=$Request.scenario-ceq'chunk7-owned-two-pair-campaign'
    if($owned){Assert-KmcChunk7OwnedPairsSelection $Request;if($Request.ownedPairs.case-cnotin@('persistence-save','owner-seed-checkpoint-save')){throw 'Owned save boundaries are source-only.'}}
    if(((-not$owned)-and$Request.scenario-cne'chunk7-persistence-save')-or-not(Test-KmcChunk7SaveBoundaryCase $Request.persistenceCase)){throw 'Save-boundary evidence belongs to another exact request.'}
    $o=$Observation.saveBoundary;$worker=$Request.persistenceCase-ceq'multi-owned-worker-cancel-manual'
    if($o.case-isnot[string]-or$o.case-cne$Request.persistenceCase){throw 'Save-boundary case identity differs.'}
    $writeCount=if($worker){2}else{1}
    Assert-KmcChunk7Boolean $o.ordinaryNativeCallbackRequired (-not$worker)
    Assert-KmcChunk7Boolean $o.seedNativeCallbackRequired $worker
    Assert-KmcChunk7Boolean $o.actualWorkerFaultProof $false
    Assert-KmcChunk7Boolean $o.diagnosticsOffProof $false
    Assert-KmcChunk7NativeCounters $o @('manualResourceWrites')
    if($o.manualResourceWrites-ne0){throw 'The diagnostic manufactured native resources.'}
    $identity=$o.identities
    Assert-KmcExactProperties $identity @('capacity','faults','ids','released','retainedAfterRelease') 'Save-boundary retained identity receipt'
    Assert-KmcChunk7NativeCounters $identity @('capacity','faults','retainedAfterRelease')
    Assert-KmcChunk7Boolean $identity.released $true
    if($identity.capacity-ne128-or$identity.faults-ne0-or$identity.retainedAfterRelease-ne0-or$identity.ids-isnot[Array]-or$identity.ids.Count-lt9-or$identity.ids.Count-gt128){throw 'Retained native identity table did not settle within its bound.'}
    $labels=[Collections.Generic.HashSet[long]]::new()
    foreach($id in $identity.ids){Assert-KmcChunk7SaveBoundaryObject $id;if(-not$labels.Add($id)){throw 'Duplicate native object label.'}}
    $probe=$o.probe
    Assert-KmcExactProperties $probe @('contract','installed','expectedSnapshots','starts','stages','complete','events','hooks','errors','maximumEvents','prepareMetric') 'Save-boundary native probe'
    Assert-KmcChunk7Boolean $probe.installed $true;Assert-KmcChunk7Boolean $probe.complete $true
    Assert-KmcChunk7NativeCounters $probe @('expectedSnapshots','starts','stages','maximumEvents')
    if($probe.contract-isnot[string]-or$probe.prepareMetric-isnot[string]-or$probe.contract-cne'chunk7-selected-save-boundary-v1'-or $probe.expectedSnapshots-ne$writeCount-or$probe.starts-ne$writeCount-or$probe.stages-ne$writeCount-or$probe.maximumEvents-ne64-or
       $probe.prepareMetric-cne'native-method-entries-including-production-refused-attempts'-or$probe.errors-isnot[Array]-or$probe.errors.Count-ne0-or
       $probe.events-isnot[Array]-or $probe.events.Count-ne(2*$writeCount)-or$probe.hooks-isnot[Array]-or$probe.hooks.Count-ne2){throw 'Native preparation/header observation is incomplete, repeated or faulted.'}
    $expectedHooks=@(@('TurnBased.Controllers.TurnController','Prepare','06000C3C'),@('Kingmaker.Controllers.Combat.UnitCombatState','OnNewRound','0600939D'))
    # The exact method token/MVID is authority; retain its native method spelling as observed.
    for($i=0;$i-lt2;$i++) {
        $h=$probe.hooks[$i];Assert-KmcExactProperties $h @('type','method','token','moduleMvid') 'Save-boundary native hook'
        foreach($key in @('type','method','token','moduleMvid')){if($h.$key-isnot[string]){throw 'Save-boundary hook identity is not a scalar string.'}}
        if($h.type-cne$expectedHooks[$i][0]-or$h.token-cne$expectedHooks[$i][2]-or$h.moduleMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'-or$h.method-cne$expectedHooks[$i][1]){throw 'Save-boundary observer native contract changed.'}
    }
    $expected=if($worker){@('before-worker-save','worker-seed-complete','worker-replacement-request','worker-held-before-stop','worker-stop-once','worker-stop-twice','worker-held-stable','worker-held-after-ticks','worker-drain-settled')}else{@('pending-before-save','pending-save-deferred','pending-save-continuing','deferred-save-settled')}
    if($o.rows-isnot[Array]-or$o.rows.Count-ne$expected.Count){throw 'Selected save boundary omitted or duplicated a native stage.'}
    for($i=0;$i-lt$expected.Count;$i++){if($o.rows[$i].kind-isnot[string]-or$o.rows[$i].kind-cne$expected[$i]){throw 'Native save stages are out of order.'}}
    for($i=0;$i-lt$writeCount;$i++){if($probe.events[2*$i].kind-isnot[string]-or$probe.events[2*$i+1].kind-isnot[string]-or$probe.events[2*$i].kind-cne'header-starting'-or$probe.events[2*$i+1].kind-cne'header-staged'){throw 'The exact native header events are missing.'}}
    if($owned){$a=@($Archive.Pairs|Where-Object{$_.Rider.Id-ceq$Request.ownedPairs.riderAId-and$_.Mount.Id-ceq$Request.ownedPairs.mountAId});$b=@($Archive.Pairs|Where-Object{$_.Rider.Id-ceq$Request.ownedPairs.riderBId-and$_.Mount.Id-ceq$Request.ownedPairs.mountBId});if($a.Count-ne1-or$b.Count-ne1){throw 'Owned save boundary roles differ.'};$a=$a[0];$b=$b[0]}
    else{$a=@($Archive.Pairs|Where-Object ProfileId -CEQ 'medium-humanoid-mammoth-v1')[0];$b=@($Archive.Pairs|Where-Object ProfileId -CEQ 'medium-humanoid-horse-v1')[0]}
    $actors=@($Archive.Pairs|ForEach-Object{$_.Rider;$_.Mount})
    $all=@($o.rows)+@($probe.events)
    $first=$o.rows[0];$final=$o.rows[-1];$header=$probe.events[-2];$staged=$probe.events[-1]
    $objects=@('gameObject','playerObject','stateObject','saveManagerObject')
    $actorLabels=@{};$boundaryLabels=@{}
    foreach($row in $all) {
        Assert-KmcExactProperties $row @('kind','frame','gameTicks','gameObject','playerObject','stateObject','saveManagerObject','requestedSaveObject','authorityGeneration','selected','owners','boundaries','actors','shell','slots','snapshots','deferred','completed','failed','cancelDeferrals','drains','draining','activeScope','admissionFence','worldBusy','saveSuspended','controlsSuspended','nativeWaiting','callback','nativeLoading','manualLoadingScreen','mode','authorizedWrites','admittedWrites','workerId','workerRunning','workerHeld','workerLeaf','workerEntries','workerHolds','drainCommitted','drainOutcomeEstablished','drainPath','paused','pauseDepth','worldReleasePending') 'Save-boundary native state'
        Assert-KmcChunk7NativeCounters $row @('frame','gameTicks','authorityGeneration','snapshots','deferred','completed','failed','cancelDeferrals','drains','authorizedWrites','admittedWrites','workerEntries','workerHolds','pauseDepth')
        foreach($field in $objects){Assert-KmcChunk7SaveBoundaryObject $row.$field;if(-not$labels.Contains($row.$field)-or$row.$field-ne$first.$field){throw 'Save boundary substituted its exact world or native manager.'}}
        Assert-KmcChunk7SaveBoundaryObject $row.requestedSaveObject ($row.kind-cin@('before-worker-save','pending-before-save'))
        $requested=if($worker-and$row.authorizedWrites-le1-and$row.kind-cne'worker-replacement-request'){$probe.events[0].requestedSaveObject}else{$header.requestedSaveObject}
        if($row.requestedSaveObject-ne0-and(-not$labels.Contains($row.requestedSaveObject)-or$row.requestedSaveObject-ne$requested)){throw 'Native requested SaveInfo changed across the operation.'}
        if($row.authorityGeneration-le0-or$row.authorityGeneration-ne$first.authorityGeneration-or$row.gameTicks-lt$first.gameTicks-or$row.frame-lt$first.frame-or$row.frame-gt$final.frame){throw 'Native authority epoch or callback time changed.'}
        foreach($field in @('draining','activeScope','admissionFence','worldBusy','saveSuspended','controlsSuspended','nativeWaiting','callback','nativeLoading','manualLoadingScreen','workerRunning','workerHeld','drainCommitted','drainOutcomeEstablished','paused','worldReleasePending')){if($row.$field-isnot[bool]){throw ('Save-boundary flag is not boolean: '+$field)}}
        if(-not(Test-KmcExactJsonInteger $row.workerId)-or$row.workerId-lt-1-or$row.workerId-gt[int]::MaxValue){throw 'Native worker identity is not an exact task integer.'}
        if($row.selected-isnot[Array]-or$row.selected.Count-ne1-or$row.selected[0]-cne$a.Rider.Id-or$row.boundaries-isnot[Array]-or$row.boundaries.Count-ne2-or$row.actors-isnot[Array]-or$row.actors.Count-ne4){throw 'Native save was not bound to selected A and all four actors/boundaries.'}
        foreach($actor in $actors) {
            $x=@($row.actors|Where-Object id -CEQ $actor.Id)
            if($x.Count-ne1){throw 'Native save actor identity is absent or duplicated.'};$x=$x[0]
            Assert-KmcExactProperties $x @('id','object','native','prepared','position','commandsEmpty','moving') 'Save-boundary native actor'
            Assert-KmcChunk7SaveBoundaryObject $x.object
            if(-not$labels.Contains($x.object)){throw 'Native actor label is unregistered.'}
            if($actorLabels.ContainsKey($x.id)){if($actorLabels[$x.id]-ne$x.object){throw 'Native save substituted an actor object.'}}else{$actorLabels[$x.id]=$x.object}
            Assert-KmcChunk7PersistenceNativeActor $x.native
            if($x.native.Id-cne$x.id-or$x.prepared-isnot[bool]-or$x.commandsEmpty-isnot[bool]-or$x.moving-isnot[bool]){throw 'Native actor debt, preparation or command state differs.'}
            Assert-KmcExactProperties $x.position @('x','y','z') 'Native actor position'
            foreach($coord in @('x','y','z')){if(-not(Test-KmcChunk7FiniteNumber $x.position.$coord)){throw 'Native actor coordinate is not finite numeric JSON.'}}
        }
        if(@($actorLabels.Values|Sort-Object -Unique).Count-ne4){throw 'Two native actors share one object label.'}
        $pending=-not$worker-and$row.kind-cin@('pending-before-save','pending-save-deferred','pending-save-continuing')
        if($row.owners-isnot[Array]-or$row.owners.Count-ne$(if($pending){1}else{2})){throw 'Native owner count is wrong for the exact save phase.'}
        foreach($pair in @($a,$b)) {
            $owner=@($row.owners|Where-Object id -CEQ $pair.PairId)
            if($pending-and$pair.PairId-ceq$b.PairId){if($owner.Count-ne0){throw 'Unacted B already has an invented mounted owner.'};continue}
            if($owner.Count-ne1){throw 'A required native owner disappeared.'};$owner=$owner[0]
            Assert-KmcExactProperties $owner @('id','generation','riderId','mountId','boundaryObject','state') 'Native save owner'
            Assert-KmcChunk7NativeCounters $owner @('generation');Assert-KmcChunk7SaveBoundaryObject $owner.boundaryObject
            if($owner.generation-ne$pair.Generation-or-not(Test-KmcChunk7OwnerActorMatch $owner.riderId $pair.Rider.Id)-or-not(Test-KmcChunk7OwnerActorMatch $owner.mountId $pair.Mount.Id)-or$owner.state-cne'Active'-or-not$labels.Contains($owner.boundaryObject)){throw 'Native owner identity/generation/role differs from its committed archive.'}
            if($boundaryLabels.ContainsKey($owner.id)){if($boundaryLabels[$owner.id]-ne$owner.boundaryObject){throw 'Owner boundary object changed.'}}else{$boundaryLabels[$owner.id]=$owner.boundaryObject}
        }
        foreach($x in $row.boundaries) {
            Assert-KmcExactProperties $x @('object','ownerId','state','generation','shellUnsettled','saveSuspended','chargeFence') 'Native save boundary'
            Assert-KmcChunk7SaveBoundaryObject $x.object;Assert-KmcChunk7NativeCounters $x @('generation')
            if(-not$labels.Contains($x.object)){throw 'Boundary label is unregistered.'}
            foreach($field in @('shellUnsettled','saveSuspended','chargeFence')){if($x.$field-isnot[bool]){throw 'Boundary flags are not exact JSON booleans.'}}
        }
        if(@($row.boundaries.object|Sort-Object -Unique).Count-ne2){throw 'Native save has duplicate boundary labels.'}
        if($row.slots-isnot[Array]){throw 'Native slots are not an array.'}
        $suspended=$row.kind-cin@('header-staged','worker-held-before-stop','worker-stop-once','worker-stop-twice','worker-held-stable','worker-held-after-ticks')
        if($suspended){if($row.slots.Count-ne0){throw 'Native owned hotbar leases remained serialized.'}}
        elseif((ConvertTo-KmcChunk7Canonical $row.slots)-cne(ConvertTo-KmcChunk7Canonical $Archive.Slots)){throw 'Native owned slots changed outside serialization.'}
        if($row.failed-ne0-or$row.authorizedWrites-gt$writeCount-or$row.admittedWrites-gt$writeCount){throw 'Native save failed or admitted an extra write.'}
    }
    foreach($row in $all){foreach($x in $row.boundaries){
        $isB=$x.object-eq$boundaryLabels[$b.PairId]
        $pending=-not$worker-and$row.kind-cin@('pending-before-save','pending-save-deferred','pending-save-continuing')-and$isB
        if(-not$isB-and$x.object-ne$boundaryLabels[$a.PairId]){throw 'Native save introduced a third boundary object.'}
        if($pending){if($null-ne$x.ownerId-or$x.state-cne'Unmounted'-or$x.generation+1-ne$b.Generation-or-not$x.shellUnsettled){throw 'Pending B boundary lost its exact pre-activation generation.'}}
        else{if($x.ownerId-cne$(if($isB){$b.PairId}else{$a.PairId})-or$x.state-cne'Mounted'-or$x.generation-ne$(if($isB){$b.Generation}else{$a.Generation})-or$x.shellUnsettled){throw 'A settled native boundary has wrong owner/generation or unfinished shell.'}}
    }}
    foreach($row in @($header,$staged)) {
        if(($worker-and($row.frame-lt$o.rows[2].frame-or$row.frame-gt$o.rows[3].frame))-or(-not$worker-and$row.frame-lt$o.rows[2].frame)-or$row.snapshots-ne$(if($row.kind-ceq'header-starting'){$writeCount-1}else{$writeCount})-or$row.authorizedWrites-ne$writeCount-or$row.admittedWrites-ne$writeCount-or-not$row.activeScope-or-not$row.admissionFence){throw 'Native header was not bracketed by the exact admitted operation.'}
        foreach($x in $row.boundaries){Assert-KmcChunk7Boolean $x.chargeFence $true;Assert-KmcChunk7Boolean $x.saveSuspended ($row.kind-ceq'header-staged')}
        Assert-KmcChunk7Boolean $row.controlsSuspended ($row.kind-ceq'header-staged');Assert-KmcChunk7Boolean $row.saveSuspended ($row.kind-ceq'header-staged')
        foreach($actor in $actors){$x=@($row.actors|Where-Object id -CEQ $actor.Id)[0];if((ConvertTo-KmcChunk7Canonical $x.native)-cne(ConvertTo-KmcChunk7Canonical $actor)){throw 'Archive debt differs from actual native header values.'}}
    }
    if($header.gameTicks-ne$Archive.GameTimeTicks-or$staged.gameTicks-ne$header.gameTicks-or$staged.frame-ne$header.frame){throw 'The native immutable header clock differs.'}
    if($final.snapshots-ne$writeCount-or$final.authorizedWrites-ne$writeCount-or$final.admittedWrites-ne$writeCount-or$final.activeScope-or$final.admissionFence-or$final.worldBusy-or$final.controlsSuspended-or$final.saveSuspended-or$final.draining-or$final.workerRunning-or$final.workerHeld-or$final.worldReleasePending){throw 'Native operation retained serialization or worker custody at reported settlement.'}
    foreach($x in $final.boundaries){Assert-KmcChunk7Boolean $x.chargeFence $false;Assert-KmcChunk7Boolean $x.saveSuspended $false}
    if($worker) {
        if($o.workerCachedAtHold-isnot[bool]-or$final.completed-ne1-or$final.deferred-ne0-or$final.drains-ne1-or$final.cancelDeferrals-ne1-or-not$final.drainCommitted-or-not$final.drainOutcomeEstablished-or$final.callback-or$final.drainPath-cne$Observation.nativeWrite.path){throw 'Native abandoned iterator outcome was fabricated or released more than once.'}
        Assert-KmcChunk7WorkerSeedReplacement $Request $Observation $Archive $labels
        $held=$o.rows[3]
        if($held.workerId-lt0-or$held.workerLeaf-cne$o.workerReplacement.stagingLeaf-or$held.workerEntries-ne2-or$held.workerHolds-ne1){throw 'Cancellation was not bound to its own real worker.'}
        foreach($row in $o.rows[3..7]) {
            if($row.workerId-ne$held.workerId-or$row.workerLeaf-cne$held.workerLeaf-or-not$row.workerRunning-or-not$row.workerHeld-or-not$row.activeScope-or-not$row.admissionFence-or-not$row.worldBusy-or-not$row.controlsSuspended-or-not$row.saveSuspended-or$row.callback-or$row.drains-ne0-or$row.completed-ne1-or$row.snapshots-ne2-or$row.authorizedWrites-ne2-or$row.admittedWrites-ne2-or$row.workerEntries-ne2-or$row.workerHolds-ne1){throw 'Live worker cancellation released a pair or substituted a task.'}
            if($row.kind-cne'worker-held-before-stop'-and(-not$row.draining-or$row.cancelDeferrals-ne1)){throw 'Repeated StopAll did not retain exactly one drain.'}
            foreach($x in $row.boundaries){Assert-KmcChunk7Boolean $x.chargeFence $true;Assert-KmcChunk7Boolean $x.saveSuspended $true}
        }
        $stable=$o.rows[6];$after=$o.rows[7]
        if(-not$stable.paused-or-not$after.paused-or$after.frame-lt$stable.frame+20-or$after.gameTicks-ne$stable.gameTicks-or
           (ConvertTo-KmcChunk7Canonical $after.actors)-cne(ConvertTo-KmcChunk7Canonical $stable.actors)-or$final.pauseDepth-ne$held.pauseDepth-or$final.paused-ne$held.paused){throw 'Worker-held native clock/actors or prior pause state did not settle exactly.'}
        foreach($row in $all){if($null-ne$row.shell){throw 'Worker case invented a pending Mount shell.'}}
    } else {
        if($final.completed-ne1-or$final.deferred-ne1-or$final.drains-ne0-or$final.cancelDeferrals-ne0-or-not$final.callback){throw 'Deferred second-owner save did not complete once normally.'}
        $before=$o.rows[0];$waiting=$o.rows[1];$continuing=$o.rows[2]
        if($before.snapshots-ne0-or$before.deferred-ne0-or$waiting.snapshots-ne0-or$waiting.deferred-ne1-or-not$waiting.nativeWaiting-or-not$waiting.admissionFence-or$waiting.activeScope-or$waiting.controlsSuspended-or$waiting.saveSuspended){throw 'Nonselected unsettled owner failed to defer capture before the header.'}
        foreach($row in @($waiting,$continuing)) {
            if(-not$row.nativeWaiting-or$row.nativeLoading-or$row.manualLoadingScreen-or$row.mode-cnotin@('Default','Pause')-or$row.paused-or$row.snapshots-ne0-or$row.deferred-ne1-or
                -not$row.admissionFence-or$row.activeScope-or$row.controlsSuspended-or$row.saveSuspended-or$row.worldBusy-or$row.authorizedWrites-ne1-or$row.admittedWrites-ne1){throw 'The native deferred save did not leave the admitted world running before serialization.'}
            foreach($x in $row.boundaries){Assert-KmcChunk7Boolean $x.chargeFence $true;Assert-KmcChunk7Boolean $x.saveSuspended $false}
        }
        $priorActor=@($waiting.actors|Where-Object id -CEQ $b.Rider.Id)[0];$continuedActor=@($continuing.actors|Where-Object id -CEQ $b.Rider.Id)[0]
        $continuedDistance=[Math]::Sqrt([Math]::Pow($continuedActor.position.x-$priorActor.position.x,2)+[Math]::Pow($continuedActor.position.y-$priorActor.position.y,2)+[Math]::Pow($continuedActor.position.z-$priorActor.position.z,2))
        if($continuing.frame-le$waiting.frame-or$continuing.gameTicks-le$waiting.gameTicks-or$continuedDistance-le0.1-or-not$continuedActor.moving-or$continuedActor.commandsEmpty){throw 'B did not continue its exact native Mount approach after save admission.'}
        if(-not(Test-KmcChunk7FiniteNumber $o.pendingApproachDisplacement)-or$o.pendingApproachDisplacement-le0.25){throw 'No actual native Mount approach was observed.'}
        $pos=$o.positioning
        if($pos.actorId-cne$b.Rider.Id-or$pos.commandType-cne'Kingmaker.UnitLogic.Commands.UnitMoveTo'){throw 'Positioning was not the exact unmounted native ground command.'}
        Assert-KmcChunk7SaveBoundaryObject $pos.commandObject;if(-not$labels.Contains($pos.commandObject)){throw 'Native approach command label is unregistered.'}
        Assert-KmcChunk7Boolean $pos.otherCommandsUnchanged $true;Assert-KmcChunk7Boolean $pos.terminal $true
        foreach($field in @('distance','mountDistance')){if(-not(Test-KmcChunk7FiniteNumber $pos.$field)){throw 'Native approach distance is not numeric.'}}
        foreach($point in @($pos.start,$pos.goal,$pos.end,$pos.mountPosition)){Assert-KmcExactProperties $point @('x','y','z') 'Native approach point';foreach($coord in @('x','y','z')){if(-not(Test-KmcChunk7FiniteNumber $point.$coord)){throw 'Native approach point is not numeric.'}}}
        $distance=[Math]::Sqrt([Math]::Pow($pos.end.x-$pos.start.x,2)+[Math]::Pow($pos.end.y-$pos.start.y,2)+[Math]::Pow($pos.end.z-$pos.start.z,2))
        $mountDistance=[Math]::Sqrt([Math]::Pow($pos.end.x-$pos.mountPosition.x,2)+[Math]::Pow($pos.end.y-$pos.mountPosition.y,2)+[Math]::Pow($pos.end.z-$pos.mountPosition.z,2))
        $pendingActor=@($before.actors|Where-Object id -CEQ $b.Rider.Id)[0]
        $approach=[Math]::Sqrt([Math]::Pow($pendingActor.position.x-$pos.end.x,2)+[Math]::Pow($pendingActor.position.y-$pos.end.y,2)+[Math]::Pow($pendingActor.position.z-$pos.end.z,2))
        if([Math]::Abs($distance-$pos.distance)-gt0.0001-or[Math]::Abs($mountDistance-$pos.mountDistance)-gt0.0001-or[Math]::Abs($approach-$o.pendingApproachDisplacement)-gt0.0001-or
           -not$pendingActor.moving-or$pendingActor.commandsEmpty-or$distance-lt0.75-or$mountDistance-lt5.5){throw 'Positioning or pending approach differs from native raw actor coordinates/commands.'}
        $input=$Observation.inputs[1]
        foreach($row in $all) {
            $s=$row.shell;Assert-KmcExactProperties $s @('object','boundaryObject','control','casterId','targetId','generation','processObject','contextObject','acted','finished','ownedUnsettled','processEnded') 'Exact pending Mount shell'
            foreach($field in @('object','boundaryObject')){Assert-KmcChunk7SaveBoundaryObject $s.$field;if(-not$labels.Contains($s.$field)){throw 'Native shell identity is unregistered.'}}
            foreach($field in @('processObject','contextObject')){Assert-KmcChunk7SaveBoundaryObject $s.$field $true;if($s.$field-ne0-and-not$labels.Contains($s.$field)){throw 'Native process/context identity is unregistered.'}}
            Assert-KmcChunk7NativeCounters $s @('generation')
            foreach($field in @('acted','finished','ownedUnsettled','processEnded')){if($s.$field-isnot[bool]){throw 'Native shell flags are not exact booleans.'}}
            if($s.object-ne$before.shell.object-or$s.boundaryObject-ne$boundaryLabels[$b.PairId]-or$s.control-cne$input.control-or$s.casterId-cne$b.Rider.Id-or$s.targetId-cne$b.Mount.Id-or$s.generation-ne$input.generationAtInit-or$s.generation+1-ne$b.Generation){throw 'Pending native command/control/generation binding changed.'}
            if($row.kind-cin@('pending-before-save','pending-save-deferred','pending-save-continuing')){if($s.acted-or$s.finished-or-not$s.ownedUnsettled-or$s.processObject-ne0-or$s.contextObject-ne0){throw 'Save request did not observe the exact unacted native shell.'}}
            elseif(-not$s.finished-or$s.ownedUnsettled-or-not$s.processEnded){throw 'Native capture ran before the exact shell/process settled.'}
        }
    }
}

function Assert-KmcChunk7WorkerSeedReplacement($Request,$Observation,$Archive,$Labels) {
    $o=$Observation.saveBoundary;$seed=$o.workerSeed;$replace=$o.workerReplacement
    $owned=$Request.scenario-ceq'chunk7-owned-two-pair-campaign';$source=if($owned){$Request.persistenceLoad}else{$Request.fixture.working}
    $name=if($owned){Get-KmcChunk7OwnedManualOutputName $Request}else{'KMC_C7_WORLD'}
    $leaf=[IO.Path]::GetFileName($Observation.nativeWrite.path)
    if($leaf-cnotmatch('^Manual_([1-9][0-9]*)_'+$name+'[.]zks$')){throw 'Worker seed lacks its canonical native Manual target.'}
    $number=0;if(-not[int]::TryParse($Matches[1],[ref]$number)-or$number-eq[int]::MaxValue-or(-not$owned-and$number-ne300)){throw 'Worker seed number is not its selected native target.'}
    if($owned-and$leaf-cne(Get-KmcChunk7OwnedOutputLeaf $Request $Observation)){throw 'Worker seed is not the reserved inherited output.'}
    $staging='Manual_'+($number+1).ToString([Globalization.CultureInfo]::InvariantCulture)+'_'+$name+'.zks'
    Assert-KmcExactProperties $seed @('requested','prepared','path','sha256','length','callbackCount','completionSequence','snapshotCount','nativeWorkerSettled') 'completed native worker seed'
    Assert-KmcExactProperties $replace @('requested','stagingLeaf','landedLeaf','seedSha256','seedLength','number','callbackCount','prepared','settled','completionCount','snapshotCount','drainCount','cancelCount','committed','outcomeEstablished','path') 'native cancelled replacement'
    foreach($v in @($seed.path,$replace.path)){if($v-isnot[string]-or$v-cne$Observation.nativeWrite.path){throw 'Worker seed/replacement escaped the exact final path.'}}
    foreach($v in @($seed.sha256,$replace.seedSha256)){if($v-isnot[string]-or$v-cnotmatch'^[a-f0-9]{64}$'-or$v-cne$seed.sha256){throw 'Replacement is not bound to the completed seed bytes.'}}
    foreach($v in @($seed.length,$replace.seedLength)){if(-not(Test-KmcExactJsonInteger $v)-or$v-le0-or$v-ne$seed.length){throw 'Completed seed length is missing or changed.'}}
    Assert-KmcChunk7Boolean $seed.nativeWorkerSettled $true
    foreach($key in @('callbackCount','completionSequence','snapshotCount')){Assert-KmcChunk7NativeCounters $seed @($key);if($seed.$key-ne1){throw 'Seed did not finish its one ordinary callback/header/completion.'}}
    foreach($pair in @(@('stagingLeaf',$staging),@('landedLeaf',$leaf))){if($replace.($pair[0])-isnot[string]-or$replace.($pair[0])-cne$pair[1]){throw 'Native replacement staging/landed leaves differ.'}}
    Assert-KmcChunk7NativeCounters $replace @('number','callbackCount','completionCount','snapshotCount','drainCount','cancelCount')
    if($replace.number-ne$number+1-or$replace.callbackCount-ne0-or$replace.completionCount-ne1-or$replace.snapshotCount-ne2-or$replace.drainCount-ne1-or$replace.cancelCount-ne1){throw 'Native replacement relabeled seed completion or cancellation totals.'}
    Assert-KmcChunk7Boolean $replace.committed $true;Assert-KmcChunk7Boolean $replace.outcomeEstablished $true
    $used=[Collections.Generic.HashSet[long]]::new()
    foreach($id in @($o.rows[0].gameObject,$o.rows[0].playerObject,$o.rows[0].stateObject,$o.rows[0].saveManagerObject)+@($o.rows[0].actors.object)+@($o.rows[0].boundaries.object)){if(-not$used.Add($id)){throw 'Worker native object roles alias.'}}
    foreach($d in @($seed.requested,$seed.prepared,$replace.prepared)) {Assert-KmcChunk7SaveBoundaryObject $d.object;if(-not$Labels.Contains($d.object)-or-not$used.Add($d.object)){throw 'Worker requested/prepared descriptor aliases another retained object.'}}
    $descriptors=@($seed.requested,$seed.prepared,$replace.requested,$replace.prepared,$replace.settled)
    for($i=0;$i-lt$descriptors.Count;$i++){
        $d=$descriptors[$i];Assert-KmcExactProperties $d @('fileName','nativeName','nativeType','gameId','gameName','area','hasFileOnDisk','isActuallySaved','operation','autoLevelup','object') 'worker native descriptor'
        Assert-KmcChunk7SaveBoundaryObject $d.object;if(-not$Labels.Contains($d.object)){throw 'Worker descriptor is unretained.'}
        foreach($pair in @(@('nativeName',$name),@('nativeType','Manual'),@('gameName',$source.gameName),@('area',$Archive.AreaId))){if($d.($pair[0])-isnot[string]-or$d.($pair[0])-cne$pair[1]){throw 'Worker descriptor native identity differs.'}}
        foreach($key in @('hasFileOnDisk','isActuallySaved','autoLevelup')){if($d.$key-isnot[bool]){throw 'Worker descriptor flag is not native Boolean.'}}
        if($d.autoLevelup-or$d.operation-isnot[string]-or[string]::IsNullOrWhiteSpace($d.operation)){throw 'Worker descriptor is not an ordinary Manual operation.'}
        if($i-eq0){if($null-ne$d.fileName-or$null-ne$d.gameId-or$d.hasFileOnDisk-or$d.isActuallySaved-or$d.operation-cne'None'){throw 'Seed request was not the native fresh unsaved descriptor.'}}
        else{if($d.gameId-isnot[string]-or$d.gameId-cne$Archive.CampaignId-or$d.fileName-isnot[string]-or$d.fileName-cne$(if($i-eq3){$staging}else{$leaf})){throw 'Prepared native descriptor escaped seed/staging/landed identity.'}}
        # Native FileName is null unless FolderName is nonempty; both flags read that same field.
        # A prepared staging name therefore requires true flags, which do not establish worker completion.
        if($i-eq3){Assert-KmcChunk7Boolean $d.hasFileOnDisk $true;Assert-KmcChunk7Boolean $d.isActuallySaved $true}
        if($i-cin@(1,2,4)){Assert-KmcChunk7Boolean $d.hasFileOnDisk $true;Assert-KmcChunk7Boolean $d.isActuallySaved $true;if($d.operation-cne'None'){throw 'Completed native descriptor is still writing.'}}
    }
    if((ConvertTo-KmcChunk7Canonical $seed.prepared)-cne(ConvertTo-KmcChunk7Canonical $replace.requested)-or$replace.prepared.object-ne$replace.settled.object){throw 'Replacement did not reuse the seed and retain its own prepared object.'}
    if($o.probe.events[0].requestedSaveObject-ne$seed.requested.object-or$o.probe.events[2].requestedSaveObject-ne$seed.prepared.object){throw 'Seed/replacement header requested identities differ.'}
    $done=$o.rows[1];$request=$o.rows[2];$held=$o.rows[3];$final=$o.rows[-1]
    if($done.snapshots-ne1-or$done.completed-ne1-or-not$done.callback-or$done.authorizedWrites-ne1-or$done.admittedWrites-ne1-or$done.workerEntries-ne1-or$done.workerHolds-ne0-or$done.drains-ne0-or$done.cancelDeferrals-ne0){throw 'Replacement lacked one completed unheld native seed.'}
    foreach($key in @('activeScope','admissionFence','worldBusy','controlsSuspended','saveSuspended','draining','workerRunning','workerHeld','worldReleasePending','nativeLoading','manualLoadingScreen')){Assert-KmcChunk7Boolean $done.$key $false;Assert-KmcChunk7Boolean $request.$key $false}
    if($request.callback-or$request.snapshots-ne1-or$request.completed-ne1-or$request.authorizedWrites-ne1-or$request.admittedWrites-ne1-or$request.workerEntries-ne1-or$request.workerHolds-ne0){throw 'Second request was counted before actual native admission or seed settlement.'}
    if($final.workerEntries-ne2-or$final.workerHolds-ne1){throw 'Two native workers and exactly one held replacement were not observed.'}
    foreach($h in $o.probe.events[0..1]){
        if($h.frame-lt$o.rows[0].frame-or$h.frame-gt$done.frame-or$h.snapshots-ne$(if($h.kind-ceq'header-starting'){0}else{1})-or$h.completed-ne0-or$h.authorizedWrites-ne1-or$h.admittedWrites-ne1-or-not$h.activeScope-or-not$h.admissionFence){throw 'Seed header was not independently bracketed by its first operation.'}
        foreach($x in $h.boundaries){Assert-KmcChunk7Boolean $x.chargeFence $true;Assert-KmcChunk7Boolean $x.saveSuspended ($h.kind-ceq'header-staged')}
        Assert-KmcChunk7Boolean $h.controlsSuspended ($h.kind-ceq'header-staged');Assert-KmcChunk7Boolean $h.saveSuspended ($h.kind-ceq'header-staged')
    }
    if($o.probe.events[0].frame-ne$o.probe.events[1].frame-or$o.probe.events[0].gameTicks-ne$o.probe.events[1].gameTicks){throw 'Seed immutable header clock differs.'}
    $ordered=@($o.rows[0],$o.probe.events[0],$o.probe.events[1],$done,$request,$o.probe.events[2],$o.probe.events[3])+@($o.rows[3..8])
    for($i=1;$i-lt$ordered.Count;$i++){
        $before=$ordered[$i-1];$after=$ordered[$i];$elapsed=([double]$after.gameTicks-[double]$before.gameTicks)/10000000.0
        if($after.frame-lt$before.frame-or$elapsed-lt0-or$elapsed-gt30){throw 'Worker phase clock/order escaped its bounded observation window.'}
        foreach($old in $before.actors){$current=@($after.actors|Where-Object id -CEQ $old.id)[0]
            if($current.prepared-ne$old.prepared-or$current.moving-or-not$current.commandsEmpty-or(ConvertTo-KmcChunk7Canonical $current.position)-cne(ConvertTo-KmcChunk7Canonical $old.position)){throw 'Worker seed/replacement changed native actor preparation, commands or position.'}
            foreach($key in @('Standard','Move','Swift','Initiative','Reaction')){if([double]$current.native.$key-gt[double]$old.native.$key+0.001-or[double]$current.native.$key+$elapsed+0.001+($elapsed*0.1)-lt[double]$old.native.$key){throw 'Native seed/replacement cooldown was manufactured beyond real elapsed time.'}}
            foreach($key in @('ReactionsRemaining','LastSurpriseTicks')){if($current.native.$key-ne$old.native.$key){throw 'Seed/replacement changed reaction/surprise history.'}}
        }
    }
}
