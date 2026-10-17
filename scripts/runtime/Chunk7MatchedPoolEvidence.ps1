Set-StrictMode -Version Latest
function Assert-KmcMatchedString($Value,[string]$Expected) {
    if($Value-isnot[string]-or$Value-cne$Expected){throw 'Matched-pool string is missing, mistyped or differs.'}
}
function Assert-KmcMatchedInt64($Value) {
    if(-not(Test-KmcExactJsonInteger $Value)-or$Value-lt[int64]::MinValue-or$Value-gt[int64]::MaxValue){throw 'Matched native Int64 observation is mistyped or outside its bound.'}
}
function Get-KmcMatchedSubscriptions($Snapshot) {
    Assert-KmcExactProperties $Snapshot @('assemblyMvid','executing','uniqueOwnedSubscribers','ownedInterfaceEntries','entries') 'Matched native subscriptions'
    Assert-KmcMatchedString $Snapshot.assemblyMvid '07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'
    if($Snapshot.entries-isnot[Array]){throw 'Matched subscribers must be an array.'}
    foreach($name in @('uniqueOwnedSubscribers','ownedInterfaceEntries')){Assert-KmcChunk7WorldInt32 $Snapshot.$name}
    foreach($entry in $Snapshot.entries){
        Assert-KmcExactProperties $entry @('interface','type','identity') 'Matched subscriber'
        if($entry.interface-isnot[string]-or$entry.type-isnot[string]-or[string]::IsNullOrEmpty($entry.interface)-or[string]::IsNullOrEmpty($entry.type)){throw 'Subscriber identity strings are mistyped.'}
        Assert-KmcChunk7WorldInt32 $entry.identity;if($entry.identity-eq0){throw 'Native subscriber identity is null.'}
    }
    return Get-KmcChunk7Subscriptions $Snapshot
}
function Assert-KmcMatchedResources($Before,$After,[string[]]$ActorIds) {
    foreach($resources in @($Before,$After)){
        Assert-KmcExactProperties $resources @('gameTicks','actors') 'Matched native resources'
        Assert-KmcMatchedInt64 $resources.gameTicks
        if($resources.actors-isnot[Array]-or$resources.actors.Count-ne4){throw 'Matched native resource actors differ.'}
        foreach($actor in $resources.actors){
            Assert-KmcExactProperties $actor @('id','move','standard','swift','prepared','reactionAllowance','initiative','reactions') 'Matched native actor resources'
            if($actor.id-isnot[string]-or$actor.id-cnotin$ActorIds){throw 'Matched resource actor identity is mistyped or foreign.'}
            foreach($name in @('reactions','reactionAllowance','initiative')){Assert-KmcChunk7WorldInt32 $actor.$name}
            if($actor.reactions-lt0-or$actor.reactionAllowance-lt0-or$actor.prepared-isnot[bool]){throw 'Native reaction/preparation observation differs.'}
        }
        Assert-KmcChunk7ExactIds $resources.actors $ActorIds
    }
    $elapsed=([double]$After.gameTicks-[double]$Before.gameTicks)/10000000.0
    if($elapsed-lt0-or$elapsed-gt30){throw 'Matched resource observation time differs.'}
    foreach($a in $Before.actors){
        $b=@($After.actors|Where-Object id -CEQ $a.id)[0]
        foreach($name in @('move','standard','swift')){
            if(-not(Test-KmcChunk7FiniteNumber $a.$name)-or-not(Test-KmcChunk7FiniteNumber $b.$name)-or$a.$name-lt0-or$b.$name-lt0-or
                [Math]::Abs([double]$b.$name-[Math]::Max([double]0,([double]$a.$name-$elapsed)))-gt0.03){throw 'Matched idle window changed native action spending.'}
        }
        foreach($name in @('initiative','prepared','reactions','reactionAllowance')){if($a.$name-ne$b.$name){throw 'Matched idle window changed native preparation, initiative or reactions.'}}
    }
}
function Assert-KmcMatchedPoolSample($Sample,[string]$Label,[int]$Active) {
    Assert-KmcExactProperties $Sample @('label','warmupFrames','sampleFrames','startFrame','endFrame','stopwatchFrequency','serviceTicks','frameSeconds','serviceMeanMs','serviceP50Ms','serviceP95Ms','frameP50Ms','frameP95Ms','managedHeapBefore','managedHeapAfter','managedHeapDelta','forcedCollection','collectionsBefore','collectionsAfter','kmcLogMessages','kmcLogCharacters','ownersBefore','ownersAfter','subscriptionsBefore','subscriptionsAfter','limits') 'Matched-pool raw sample'
    Assert-KmcMatchedString $Sample.label $Label
    foreach($field in @('warmupFrames','sampleFrames','startFrame','endFrame','stopwatchFrequency','managedHeapBefore','managedHeapAfter','kmcLogMessages','kmcLogCharacters')) {
        Assert-KmcMatchedInt64 $Sample.$field;if($Sample.$field-lt0){throw 'Matched-pool sample counter differs.'}
    }
    Assert-KmcChunk7WorldInt32 $Sample.startFrame;Assert-KmcChunk7WorldInt32 $Sample.endFrame
    if($Sample.warmupFrames-ne30-or$Sample.sampleFrames-ne120-or$Sample.endFrame-$Sample.startFrame-lt119-or$Sample.stopwatchFrequency-le0-or
       $Sample.serviceTicks-isnot[Array]-or$Sample.serviceTicks.Count-ne120-or$Sample.frameSeconds-isnot[Array]-or$Sample.frameSeconds.Count-ne120){throw 'Matched-pool sample interval differs.'}
    foreach($value in $Sample.serviceTicks){Assert-KmcMatchedInt64 $value;if($value-lt0){throw 'Matched-pool service ticks are not native integers.'}}
    foreach($value in $Sample.frameSeconds){if(-not(Test-KmcChunk7FiniteNumber $value)-or$value-le0){throw 'Matched-pool frame seconds are not finite positive numbers.'}}
    $ticks=@($Sample.serviceTicks|Sort-Object);$frames=@($Sample.frameSeconds|Sort-Object)
    $summaries=@{serviceMeanMs=([double]($ticks|Measure-Object -Average).Average*1000/$Sample.stopwatchFrequency);serviceP50Ms=([double]$ticks[59]*1000/$Sample.stopwatchFrequency);serviceP95Ms=([double]$ticks[113]*1000/$Sample.stopwatchFrequency);frameP50Ms=([double]$frames[59]*1000);frameP95Ms=([double]$frames[113]*1000)}
    foreach($name in $summaries.Keys){if(-not(Test-KmcChunk7FiniteNumber $Sample.$name)-or[Math]::Abs([double]$Sample.$name-$summaries[$name])-gt0.001){throw 'Matched-pool summary differs from raw observations.'}}
    Assert-KmcMatchedInt64 $Sample.managedHeapDelta
    if($Sample.managedHeapDelta-ne($Sample.managedHeapAfter-$Sample.managedHeapBefore)){throw 'Managed heap occupancy arithmetic differs.'}
    Assert-KmcChunk7Boolean $Sample.forcedCollection $false
    foreach($values in @($Sample.collectionsBefore,$Sample.collectionsAfter)){if($values-isnot[Array]-or$values.Count-ne3){throw 'Native GC observation cardinality differs.'};foreach($value in $values){Assert-KmcChunk7WorldInt32 $value;if($value-lt0){throw 'Native GC counter is negative.'}}}
    for($i=0;$i-lt3;$i++){if($Sample.collectionsAfter[$i]-lt$Sample.collectionsBefore[$i]){throw 'Native GC counter went backwards.'}}
    foreach($state in @($Sample.ownersBefore,$Sample.ownersAfter)){
        Assert-KmcExactProperties $state @('boundaries','failedConstructions','owners','active','allocationActors','participants','preparedOrViewResidue','liveControlBoundaries') 'Matched resident sample ownership'
        foreach($name in @('boundaries','failedConstructions','owners','active','allocationActors','participants','preparedOrViewResidue','liveControlBoundaries')){Assert-KmcChunk7WorldInt32 $state.$name;if($state.$name-lt0){throw 'Negative matched-pool ownership observation.'}}
        if($state.boundaries-ne2-or$state.participants-ne2-or$state.failedConstructions-ne0-or$state.active-ne$Active-or$state.owners-ne$Active-or$state.preparedOrViewResidue-ne$Active-or$state.liveControlBoundaries-ne0){throw 'Matched resident pool contains unfinished or changed custody.'}
    }
    if($Sample.ownersBefore.allocationActors-ne$Sample.ownersAfter.allocationActors){throw 'Idle sampling changed retained actor allocations.'}
    if($Sample.limits-isnot[string]-or[string]::IsNullOrWhiteSpace($Sample.limits)){throw 'Raw sample limits missing.'}
}
function Assert-KmcMatchedPoolEvidence($Request,$Artifact,$WorldArtifact,[string]$WorldSha256,[string]$Status) {
    Assert-KmcChunk7ArtifactEnvelope $Request $Artifact 'chunk7-matched-pool-measurements'
    Assert-KmcMatchedString $Artifact.evidenceKind 'chunk7-matched-pool-measurements'
    if($Artifact.status-isnot[string]-or$Artifact.status-cnotin@('PASS','FAIL')){throw 'Matched-pool status token differs.'}
    if($Artifact.status-ceq'FAIL'){
        if($Status-ceq'PASS'-or$Artifact.errors.Count-eq0){throw 'Failed matched-pool observation cannot qualify a successful run.'}
        return
    }
    Assert-KmcChunk7ArtifactEnvelope $Request $WorldArtifact 'chunk7-world-evidence'
    Assert-KmcMatchedString $WorldArtifact.status 'PASS'
    $o=$Artifact.observations;$world=$WorldArtifact.observations
    Assert-KmcExactProperties $o @('contract','complete','observerReleased','residentBoundaries','residentParticipants','warmupNativeCycle','measuredNativeCycle','additionalNativeInputs','inclusiveUpdateIncludesObserverWork','managedAllocationRateMeasured','fullyInertDiagnostics','performanceAccepted','rows','abortedWindow','limits','worldEvidenceSha256') 'Matched-pool observations'
    Assert-KmcMatchedString $o.contract 'chunk7-matched-resident-pool-v1'
    Assert-KmcMatchedString $o.worldEvidenceSha256 $WorldSha256
    if($WorldSha256-cnotmatch'^[0-9a-f]{64}$'){throw 'Original world artifact hash is missing.'}
    foreach($flag in @('complete','observerReleased','inclusiveUpdateIncludesObserverWork')){Assert-KmcChunk7Boolean $o.$flag $true}
    foreach($flag in @('managedAllocationRateMeasured','fullyInertDiagnostics','performanceAccepted')){Assert-KmcChunk7Boolean $o.$flag $false}
    foreach($name in @('residentBoundaries','residentParticipants','warmupNativeCycle','measuredNativeCycle','additionalNativeInputs')){Assert-KmcChunk7WorldInt32 $o.$name}
    if($o.residentBoundaries-ne2-or$o.residentParticipants-ne2-or$o.warmupNativeCycle-ne1-or$o.measuredNativeCycle-ne2-or$o.additionalNativeInputs-ne0-or$null-ne$o.abortedWindow-or
        $o.rows-isnot[Array]-or$o.rows.Count-ne4-or$o.limits-isnot[string]-or[string]::IsNullOrWhiteSpace($o.limits)){throw 'Matched-pool sequence or evidence limits differ.'}
    if($world.cycles-isnot[Array]-or$world.cycles.Count-ne6-or$world.cycleInputs-isnot[Array]-or$world.cycleInputs.Count-ne12){throw 'Matched observation changed the original native cycle/input counts.'}
    if(Test-KmcChunk7OwnedPairsOwnerSeed $Request){$actorIds=@(Assert-KmcChunk7OwnerSeedWorldSetup $Request $world)}
    elseif(Test-KmcChunk7OwnedPairsScenario $Request.scenario){Assert-KmcChunk7InheritedEvidence $Request $world;$actorIds=@(Get-KmcChunk7OwnedPairIds $Request)}else{
    $actorIds=@('b6628a77-4962-47a4-a17c-88d9836fc9d5','d79a4f6c-b74e-4868-95bd-533899131acb','d17c8fd0-6627-40c4-bf9a-024cf001c1f7')
    $horse=@($world.partyWithHorse|Where-Object { $_.masterId-ceq$actorIds[2] })
    if($horse.Count-ne1-or$horse[0].id-isnot[string]-or[string]::IsNullOrEmpty($horse[0].id)){throw 'Exact native Horse identity unavailable.'};$actorIds+=$horse[0].id
    }
    $labels=@('resident-0-after-cycle-1','resident-1-cycle-2','resident-2-cycle-2','resident-0-after-cycle-2');$active=@(0,1,2,0)
    $firstMount=@($world.snapshots|Where-Object label -CEQ 'cycle-2-mount-mammoth-settled')
    if($firstMount.Count-ne1){throw 'Original cycle2 first native mount snapshot unavailable.'}
    $anchors=@($world.cycles[1],$firstMount[0],$world.cycles[2],$world.cycles[3])
    $pins=$null;$subscriptions=$null;$priorFrame=-1;$priorTicks=-1L;$actorAllocationCount=$null
    for($i=0;$i-lt4;$i++){
        $row=$o.rows[$i]
        Assert-KmcExactProperties $row @('index','label','activePairs','before','after','sample') 'Matched-pool row'
        Assert-KmcChunk7WorldInt32 $row.index;Assert-KmcChunk7WorldInt32 $row.activePairs
        Assert-KmcMatchedString $row.label $labels[$i]
        if($row.index-ne$i-or$row.activePairs-ne$active[$i]){throw 'Matched-pool row order/count differs.'}
        Assert-KmcMatchedPoolSample $row.sample $labels[$i] $active[$i]
        foreach($boundary in @($row.before,$row.after)){
            Assert-KmcExactProperties $boundary @('identityPins','actorIds','expectedActivePairs','world','commandsSettled','preparationPending','relationshipTransition','serializationBusy','pendingNativeWrites','allocationActors') 'Matched native boundary'
            if($boundary.identityPins-isnot[Array]-or$boundary.identityPins.Count-ne22-or$boundary.actorIds-isnot[Array]-or$boundary.actorIds.Count-ne4){throw 'Matched native identity cardinality differs.'}
            foreach($value in $boundary.identityPins){Assert-KmcChunk7WorldInt32 $value;if($value-eq0){throw 'Matched native identity cannot be null.'}}
            if(@($boundary.identityPins|Select-Object -Unique).Count-ne22){throw 'Matched native scope aliases distinct ownership roles.'}
            for($a=0;$a-lt4;$a++){
                Assert-KmcMatchedString $boundary.actorIds[$a] $actorIds[$a]
                $original=@($world.partyWithHorse|Where-Object id -CEQ $actorIds[$a]);if($original.Count-ne1){throw 'Original native actor ambiguous.'}
                Assert-KmcChunk7WorldInt32 $original[0].object
                if($boundary.identityPins[10+3*$a]-ne$original[0].object){throw 'Matched actor object differs from the original fixture.'}
            }
            $observedPins=$boundary.identityPins-join'|';if($null-eq$pins){$pins=$observedPins}elseif($pins-cne$observedPins){throw 'Matched native world/pool/descriptor/view changed.'}
            Assert-KmcChunk7WorldInt32 $boundary.expectedActivePairs;Assert-KmcChunk7WorldInt32 $boundary.allocationActors
            if($boundary.expectedActivePairs-ne$active[$i]-or$boundary.allocationActors-lt0){throw 'Matched native ownership differs.'}
            if($null-eq$actorAllocationCount){$actorAllocationCount=$boundary.allocationActors}elseif($actorAllocationCount-ne$boundary.allocationActors){throw 'Matched sequence changed retained allocation actor cardinality.'}
            Assert-KmcChunk7Boolean $boundary.commandsSettled $true
            foreach($flag in @('preparationPending','relationshipTransition','serializationBusy','pendingNativeWrites')){Assert-KmcChunk7Boolean $boundary.$flag $false}
            $w=$boundary.world
            Assert-KmcExactProperties $w @('label','frame','selection','resources','boundaryCount','participantCount','subscriptions','owners') 'Matched world snapshot'
            Assert-KmcMatchedString $w.label 'matched-pool-boundary'
            foreach($name in @('frame','boundaryCount','participantCount')){Assert-KmcChunk7WorldInt32 $w.$name}
            if($w.frame-lt$priorFrame-or$w.boundaryCount-ne2-or$w.participantCount-ne2-or$w.owners-isnot[Array]-or$w.owners.Count-ne$active[$i]-or
                $w.selection-isnot[Array]-or$w.selection.Count-ne1-or$w.selection[0]-isnot[string]-or$w.selection[0]-cnotin$actorIds){throw 'Matched world cardinality/clock/selection differs.'}
            Assert-KmcMatchedInt64 $w.resources.gameTicks
            if($w.resources.gameTicks-lt$priorTicks){throw 'Matched native game time went backwards.'}
            $priorFrame=$w.frame;$priorTicks=$w.resources.gameTicks
            $sub=Get-KmcMatchedSubscriptions $w.subscriptions
            if($null-eq$subscriptions){$subscriptions=$sub}elseif($subscriptions-cne$sub){throw 'Matched native subscriptions changed.'}
            foreach($owner in $w.owners){
                Assert-KmcExactProperties $owner @('ownerId','riderId','mountId','generation','state','relationshipState','retainsObligations','saveReady','teardownReady') 'Matched native pair owner'
                foreach($name in @('ownerId','riderId','mountId','state','relationshipState')){if($owner.$name-isnot[string]){throw 'Matched owner string is mistyped.'}}
                Assert-KmcMatchedInt64 $owner.generation
                if($owner.generation-lt0-or$owner.state-cne'Active'-or$owner.relationshipState-cne'Mounted'){throw 'Matched owner is not an admitted active pair.'}
                foreach($flag in @('retainsObligations','saveReady','teardownReady')){if($owner.$flag-isnot[bool]){throw 'Matched owner custody flag is mistyped.'}}
                $pinned=@($anchors[$i].owners|Where-Object riderId -CEQ $owner.riderId)
                if($pinned.Count-ne1-or$owner.ownerId-cne$pinned[0].ownerId-or$owner.mountId-cne$pinned[0].mountId-or$owner.generation-ne$pinned[0].generation){throw 'Matched owner/generation differs from its real native cycle.'}
            }
            $expectedRiders=@();if($active[$i]-ge1){$expectedRiders+=$actorIds[0]};if($active[$i]-eq2){$expectedRiders+=$actorIds[2]}
            Assert-KmcChunk7ExactIds $w.owners $expectedRiders 'riderId'
        }
        Assert-KmcChunk7WorldInt32 $anchors[$i].frame
        if((Get-KmcMatchedSubscriptions $anchors[$i].subscriptions)-cne$subscriptions){throw 'Matched subscribers differ from the actual native lifecycle anchor.'}
        if($row.before.world.frame-lt$anchors[$i].frame-or$row.sample.startFrame-lt$row.before.world.frame-or$row.sample.endFrame-ne$row.after.world.frame){throw 'Matched sample is not inside its captured native interval.'}
        if(($row.before.world.selection-join'|')-cne($row.after.world.selection-join'|')){throw 'Idle sampling changed native selection.'}
        Assert-KmcMatchedResources $row.before.world.resources $row.after.world.resources $actorIds
        foreach($sampleSide in @($row.sample.subscriptionsBefore,$row.sample.subscriptionsAfter)){if((Get-KmcMatchedSubscriptions $sampleSide)-cne$subscriptions){throw 'Timer subscriber snapshot differs from native boundaries.'}}
        if($row.sample.ownersBefore.allocationActors-ne$actorAllocationCount){throw 'Timer allocation actor count differs from native scope.'}
    }
    # Deliberately no numeric performance acceptance threshold.
}
function Assert-KmcMatchedPoolArtifact($Request,$Manifest,[string]$Status) {
    $leaf='chunk7-matched-pool-measurements.json';$kind='chunk7-matched-pool-measurements'
    $present=@($Manifest.artifacts|Where-Object{$_.relativePath-ceq$leaf-or$_.kind-ceq$kind})
    if((Get-KmcChunk7SelectedScenario $Request)-cne'chunk7-two-pair-exploration'){if($present.Count-ne0){throw 'Another scenario supplied matched-pool observations.'};return}
    if($present.Count-eq0-and$Status-cne'PASS'){return}
    $artifact=Read-KmcChunk7ManifestArtifact $Request $Manifest $leaf $kind
    $world=Read-KmcChunk7ManifestArtifact $Request $Manifest 'chunk7-world-evidence.json' 'chunk7-world-evidence'
    $worldRow=@($Manifest.artifacts|Where-Object relativePath -CEQ 'chunk7-world-evidence.json')
    Assert-KmcMatchedPoolEvidence $Request $artifact $world $worldRow[0].sha256 $Status
}
