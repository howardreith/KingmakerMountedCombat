# Additional independent reader for the closed two-pair AfterEntry autosave case.
# Main persistence validation still checks the immutable ZIP, full archive, source
# identity, two native Mounts, both continuation moves and exact settled cleanup.
Set-StrictMode -Version Latest
function Assert-KmcChunk7AreaAutosave($Request,$Observation,$Archive) {
    if($Request.persistenceCase-cne'multi-area-autosave'-or$Request.scenario-cne'chunk7-persistence-save') {
        throw 'Area source evidence is only valid for its exact selected native case.'
    }
    $target=$Request.persistenceAreaTarget;$row=$Observation.areaTransfer
    if($target.autoSaveMode-cne'AfterEntry'-or$target.area-ceq$Request.fixture.working.area-or
       $row.mode-cne'AfterEntry'-or$row.enterPoint-cne$target.enterPoint-or$row.sourceArea-cne$Request.fixture.working.area-or
       $row.destinationArea-cne$target.area-or$Archive.AreaId-cne$target.area-or
       $row.explicitSaveGameDispatches-ne0-or$null-ne$row.observerError-or$row.nativeArchiveSha256-cne$Observation.nativeWrite.sha256) {
        throw 'Native area autosave destination, engine dispatch, observer or immutable archive identity differs.'
    }
    if(-not(Test-KmcExactJsonInteger $row.explicitSaveGameDispatches)){throw 'Area native dispatch count is not an integer.'}
    Assert-KmcChunk7Boolean $row.nativeSaveCallbackExpected $false
    Assert-KmcChunk7Boolean $Observation.nativeWrite.nativeCallback $false
    Assert-KmcChunk7Boolean $row.suspensionObserved $true
    Assert-KmcChunk7Boolean $row.duplicateReadyNoOp $true
    if(-not(Test-KmcExactJsonInteger $row.loadingTicks)-or$row.loadingTicks-le0-or$row.loadingTicks-gt100000){throw 'Native asynchronous area interval was not observed.'}
    if($Observation.nativeWrite.nativeType-cne'Auto'-or$Observation.nativeWrite.operation-cne'None'-or$Observation.nativeWrite.snapshotCount-ne1){throw 'Area case lacks one completed actual Auto save.'}
    $base=$row.before
    $phases=@(
        @($base,'before-native-transfer',$row.sourceArea,0,0,0),
        @($row.headerStarting,'native-header-starting',$row.destinationArea,1,2,0),
        @($row.headerStaged,'native-header-staged',$row.destinationArea,1,2,1),
        @($row.after,'after-native-transfer',$row.destinationArea,1,2,1))
    $priorFrame=-1L;$priorTicks=-1L
    $restoredMovers=[Collections.Generic.Dictionary[string,int]]::new([StringComparer]::Ordinal)
    $allNative=@($Archive.Pairs|ForEach-Object{$_.Rider;$_.Mount})
    foreach($spec in $phases) {
        $s=$spec[0]
        foreach($counter in @('suspensions','resumes','snapshots','activeOwners','semanticRestores','presentationRestores','nativePostLoads','nativeCastRequests')) {
            if(-not(Test-KmcExactJsonInteger $s.$counter)-or$s.$counter-lt0){throw 'Area counters must be observed nonnegative integers.'}
        }
        $viewSet=[Collections.Generic.HashSet[int]]::new();$movementSet=[Collections.Generic.HashSet[int]]::new()
        if($s.label-cne$spec[1]-or$s.area-cne$spec[2]-or$s.suspensions-ne$spec[3]-or$s.resumes-ne$spec[4]-or$s.snapshots-ne$spec[5]-or
           $s.activeOwners-ne2-or$s.semanticRestores-ne0-or$s.presentationRestores-ne0-or$s.nativePostLoads-ne0-or
           $s.nativeCastRequests-ne$base.nativeCastRequests-or$s.owners-isnot[Array]-or$s.owners.Count-ne2-or$s.actors-isnot[Array]-or$s.actors.Count-ne4-or
           $s.views-isnot[Array]-or$s.views.Count-ne4-or$s.movementAuthorities-isnot[Array]-or$s.movementAuthorities.Count-ne2){
            throw 'Area header order, all-pair counts, view inventory or native replay counters differ.'
        }
        Assert-KmcChunk7Boolean $s.sameWorld $true
        if(-not(Test-KmcExactJsonInteger $s.frame)-or-not(Test-KmcExactJsonInteger $s.gameTicks)-or$s.frame-lt$priorFrame-or$s.gameTicks-lt$priorTicks){throw 'Native area snapshots are missing or out of causal order.'}
        $priorFrame=[long]$s.frame;$priorTicks=[long]$s.gameTicks
        $elapsed=([double]$s.gameTicks-[double]$base.gameTicks)/10000000.0
        if($elapsed-lt0-or$elapsed-gt180){throw 'Native area debt escaped the bounded transfer clock.'}
        foreach($pair in $Archive.Pairs) {
            $owner=@($s.owners|Where-Object{([Guid]$_.ownerId)-eq([Guid]$pair.PairId)})
            if($owner.Count-ne1-or-not(Test-KmcExactJsonInteger $owner[0].generation)-or$owner[0].generation-ne$pair.Generation-or$owner[0].riderId-cne$pair.Rider.Id-or$owner[0].mountId-cne$pair.Mount.Id-or$owner[0].state-cne'Active'){
                throw 'Area transfer changed exact owner, generation or actor roles.'
            }
            $movement=@($s.movementAuthorities|Where-Object riderId -CEQ $pair.Rider.Id)
            $original=@($base.movementAuthorities|Where-Object riderId -CEQ $pair.Rider.Id)
            if($movement.Count-ne1-or$original.Count-ne1){throw 'Area transfer omitted an independent movement authority.'}
            $m=$movement[0];$initial=$original[0]
            if(-not(Test-KmcExactJsonInteger $m.runtimeOverride)-or$m.runtimeOverride-eq0-or-not$movementSet.Add([int]$m.runtimeOverride)-or
               -not(Test-KmcExactJsonInteger $m.baselineOverride)-or-not(Test-KmcExactJsonInteger $m.viewOverride)-or-not(Test-KmcExactJsonInteger $m.mountOverride)-or
               $m.baselineOverride-ne$initial.baselineOverride-or$m.viewOverride-ne$m.runtimeOverride-or
               $m.overrideComponents-isnot[Array]-or$m.overrideComponents.Count-ne1-or
               -not(Test-KmcExactJsonInteger $m.overrideComponents[0])-or$m.overrideComponents[0]-ne$m.runtimeOverride-or
               $m.mountId-cne$pair.Mount.Id-or$m.mountOverride-ne0-or$m.mountOverrideComponents-isnot[Array]-or$m.mountOverrideComponents.Count-ne0-or$null-ne$m.invariant){
                throw 'Area movement/view ownership is absent, duplicated or mismatched.'
            }
            if($spec[1]-ceq'before-native-transfer') {
                if($m.runtimeOverride-ne$m.baselineOverride){throw 'Area baseline does not identify its original native override.'}
            } elseif($m.runtimeOverride-eq$m.baselineOverride) {throw 'Area restoration retained the retired movement override.'}
            if($spec[1]-ceq'native-header-starting') {
                $restoredMovers.Add($pair.Rider.Id,[int]$m.runtimeOverride)
            } elseif($spec[1]-cne'before-native-transfer') {
                if(-not$restoredMovers.ContainsKey($pair.Rider.Id)-or$m.runtimeOverride-ne$restoredMovers[$pair.Rider.Id]) {
                    throw 'The restored native mover changed across the autosave header or settled transfer.'
                }
            }
            Assert-KmcChunk7Boolean $m.stockEnabled $false
            foreach($field in @('mountStockEnabled','riderViewBoundToOwner','mountViewBoundToOwner')) {Assert-KmcChunk7Boolean $m.$field $true}
        }
        foreach($actor in $allNative) {
            $view=@($s.views|Where-Object id -CEQ $actor.Id);$baseView=@($base.views|Where-Object id -CEQ $actor.Id)
            if($view.Count-ne1-or$baseView.Count-ne1-or-not(Test-KmcExactJsonInteger $view[0].viewId)-or$view[0].viewId-eq0-or-not$viewSet.Add([int]$view[0].viewId)-or
               -not(Test-KmcExactJsonInteger $view[0].baselineViewId)-or-not(Test-KmcExactJsonInteger $view[0].nativeMatches)-or
               $view[0].nativeMatches-ne1-or$view[0].viewId-ne$view[0].baselineViewId-or$view[0].viewId-ne$baseView[0].viewId){throw 'Area actor/view did not remain uniquely bound and retained.'}
            Assert-KmcChunk7Boolean $view[0].viewBound $true;Assert-KmcChunk7Boolean $view[0].commandsEmpty $true
            $before=@($base.actors|Where-Object{$_.native.Id-ceq$actor.Id});$after=@($s.actors|Where-Object{$_.native.Id-ceq$actor.Id})
            if($before.Count-ne1-or$after.Count-ne1){throw 'Area native debt snapshot omitted an actor.'}
            $a=$before[0].native;$b=$after[0].native
            Assert-KmcChunk7PersistenceNativeActor $a;Assert-KmcChunk7PersistenceNativeActor $b
            Assert-KmcChunk7Boolean $after[0].inCombat $false
            if($before[0].prepared-isnot[bool]-or$after[0].prepared-isnot[bool]-or$before[0].prepared-ne$after[0].prepared){throw 'Area transfer changed actor preparation.'}
            foreach($field in @('Standard','Move','Swift','Initiative','Reaction')) {
                if([double]$b.$field-gt[double]$a.$field+0.001-or[double]$b.$field+$elapsed+0.001+($elapsed*0.1)-lt[double]$a.$field){throw 'Area transfer erased, granted or replayed native expenditure.'}
            }
            if($a.ReactionsRemaining-ne$b.ReactionsRemaining-or$a.LastSurpriseTicks-ne$b.LastSurpriseTicks){throw 'Area transfer changed reaction/surprise history.'}
        }
    }
    Assert-KmcChunk7Boolean $row.before.pending $false
    Assert-KmcChunk7Boolean $row.after.pending $false
}
