# Additional independent reader for the closed two-pair AfterEntry autosave case.
# Main persistence validation still checks the immutable ZIP, full archive, source
# identity, two native Mounts, both continuation moves and exact settled cleanup.
Set-StrictMode -Version Latest
function Assert-KmcChunk7AreaAutosave($Request,$Observation,$Archive) {
    if($Request.persistenceCase-cne'multi-area-autosave'-or(Get-KmcChunk7SelectedScenario $Request)-cne'chunk7-persistence-save') {
        throw 'Area source evidence is only valid for its exact selected native case.'
    }
    $target=$Request.persistenceAreaTarget;$row=$Observation.areaTransfer
    $sourceArea=(Get-KmcChunk7SelectedFixture $Request).area
    if($target.autoSaveMode-cne'AfterEntry'-or$target.area-ceq$sourceArea-or
       $row.mode-cne'AfterEntry'-or$row.enterPoint-cne$target.enterPoint-or$row.sourceArea-cne$sourceArea-or
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
    if(Test-KmcChunk7OwnedAreaOverwrite $Request){Assert-KmcChunk7OwnedAreaOverwrite $Request $Observation}
    elseif($null-ne$Observation.PSObject.Properties['areaOverwrite']){throw 'A first-ever area Auto cannot carry an overwrite receipt.'}
    $base=$row.before;$seed=(Test-KmcChunk7OwnedPairsScenario $Request.scenario)-and(Test-KmcChunk7OwnedPairsSeedCheckpointSave $Request)
    if($seed){Assert-KmcChunk7OwnerSeedAreaEntryObservation $Request $Observation.areaEntryPrerequisite}
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
           $s.activeOwners-ne2-or$s.semanticRestores-ne$(if($seed){4}else{0})-or$s.presentationRestores-ne$(if($seed){2}else{0})-or$s.nativePostLoads-ne$(if($seed){4}else{0})-or
           $s.nativeCastRequests-ne$base.nativeCastRequests-or$s.owners-isnot[Array]-or$s.owners.Count-ne2-or$s.actors-isnot[Array]-or$s.actors.Count-ne4-or
           $s.views-isnot[Array]-or$s.views.Count-ne4-or$s.movementAuthorities-isnot[Array]-or$s.movementAuthorities.Count-ne2){
            throw 'Area header order, all-pair counts, view inventory or native replay counters differ.'
        }
        Assert-KmcChunk7Boolean $s.sameWorld $true
        if(-not(Test-KmcExactJsonInteger $s.frame)-or-not(Test-KmcExactJsonInteger $s.gameTicks)-or$s.frame-lt$priorFrame-or$s.gameTicks-lt$priorTicks){throw 'Native area snapshots are missing or out of causal order.'}
        $priorFrame=[long]$s.frame;$priorTicks=[long]$s.gameTicks
        Assert-KmcChunk7Boolean $s.activeSaveScope ($spec[1]-ceq'native-header-starting'-or$spec[1]-ceq'native-header-staged')
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
            Assert-KmcChunk7Boolean $m.saveSerializationSuspended ($spec[1]-ceq'native-header-staged')
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
    Assert-KmcChunk7AreaContinuationPlans $Observation
    if($null-ne$Observation.PSObject.Properties['areaDoorInventory']){Assert-KmcChunk7AreaDoorInventory $Observation}
    if(Test-KmcChunk7OwnedPairsScenario $Request.scenario){Assert-KmcChunk7OwnedAreaField $Request $Observation}
}

# Optional bounded read-only fixture inventory added after the accepted
# preview.77 area route. Older immutable PASS artifacts have no such field.
function Assert-KmcChunk7AreaDoorInventory($Observation){
    $inventory=$Observation.areaDoorInventory
    Assert-KmcExactProperties $inventory @('boundary','candidateCount','rowLimit','radius','mounts','truncated','rows','interactionTested') 'area door inventory'
    Assert-KmcBootstrapInventoryString $inventory.boundary 'before-first-continuation'
    Assert-KmcOrdinaryInteger $inventory.candidateCount 0 100000
    Assert-KmcOrdinaryInteger $inventory.rowLimit 32 32
    if(-not(Test-KmcChunk7FiniteNumber $inventory.radius)-or[double]$inventory.radius-ne28.0){throw 'Native door scan radius differs.'}
    Assert-KmcChunk7Boolean $inventory.truncated ($inventory.candidateCount-gt32)
    Assert-KmcChunk7Boolean $inventory.interactionTested $false
    if($inventory.rows-isnot[Array]-or$inventory.rows.Count-ne[Math]::Min(32,[int]$inventory.candidateCount)){
        throw 'Area door rows do not match the bounded native candidate count.'
    }
    $moves=@($Observation.movements);$first=@($moves|Where-Object label -CEQ 'first-pair');$second=@($moves|Where-Object label -CEQ 'second-pair')
    if($inventory.mounts-isnot[Array]-or$inventory.mounts.Count-ne2-or$first.Count-ne1-or$second.Count-ne1){throw 'Area door inventory lacks both mounted actor positions.'}
    for($i=0;$i-lt2;$i++){
        Assert-KmcExactProperties $inventory.mounts[$i] @('id','position') 'area door mounted actor'
        Assert-KmcBootstrapInventoryString $inventory.mounts[$i].id $(if($i-eq0){$first[0].executorId}else{$second[0].executorId})
    }
    foreach($axis in @('x','y','z')){
        foreach($mount in $inventory.mounts){if(-not(Test-KmcChunk7FiniteNumber $mount.position.$axis)){throw 'Area door mount position is nonfinite.'}}
        if([Math]::Abs([double]$inventory.mounts[0].position.$axis-[double]$first[0].start.$axis)-gt0.001){throw 'Door scan was not taken at the first mounted continuation boundary.'}
    }
    # Eligibility is the probe's shared door predicate (a legacy row is admitted only when it could not qualify open or closed).
    Assert-KmcChunk7NativeDoorRows $inventory.rows $inventory.mounts ([int]$inventory.candidateCount) 'native area door'
}

function Assert-KmcChunk7AreaContinuationPlans($Observation) {
    foreach($label in @('first-pair','second-pair')) {
        $plan=$Observation.areaContinuationPlans.$label
        $move=@($Observation.movements|Where-Object label -CEQ $label)
        if($null-eq$plan-or$move.Count-ne1-or$plan.moverId-cne$move[0].executorId-or
           $plan.proposals-isnot[Array]-or$plan.proposals.Count-lt1-or$plan.proposals.Count-gt8){
            throw 'Area continuation lacks its bounded exact native mount plan.'
        }
        foreach($axis in @('x','y','z')) {
            if(-not(Test-KmcChunk7FiniteNumber $plan.start.$axis)-or-not(Test-KmcChunk7FiniteNumber $plan.selectedGoal.$axis)-or
               [Math]::Abs([double]$plan.start.$axis-[double]$move[0].start.$axis)-gt0.0001-or
               [Math]::Abs([double]$plan.selectedGoal.$axis-[double]$move[0].goal.$axis)-gt0.0001){
                throw 'Area continuation plan changed its actual native click.'
            }
        }
        $angles=@(90,270,0,180,45,135,225,315)
        for($i=0;$i-lt$plan.proposals.Count;$i++){
            if(-not(Test-KmcChunk7FiniteNumber $plan.proposals[$i].angle)-or$plan.proposals[$i].angle-ne$angles[$i]){
                throw 'Area continuation proposal order or angle differs from the bounded native search.'
            }
            Assert-KmcChunk7Boolean $plan.proposals[$i].eligible ($i-eq$plan.proposals.Count-1)
        }
        $selected=$plan.proposals[-1]
        Assert-KmcChunk7Boolean $selected.walkable $true
        Assert-KmcChunk7Boolean $selected.occupied $false
        foreach($axis in @('x','y','z')) {
            foreach($point in @($selected.requested,$selected.goal,$selected.traceEnd)) {
                if(-not(Test-KmcChunk7FiniteNumber $point.$axis)){throw 'Area continuation proposal has a nonfinite native point.'}
            }
            if([Math]::Abs([double]$selected.goal.$axis-[double]$plan.selectedGoal.$axis)-gt0.0001){
                throw 'Area continuation selected a different proposal endpoint.'
            }
        }
        $travel=[Math]::Sqrt([Math]::Pow([double]$plan.start.x-[double]$selected.goal.x,2)+[Math]::Pow([double]$plan.start.z-[double]$selected.goal.z,2))
        $clamp=[Math]::Sqrt([Math]::Pow([double]$selected.requested.x-[double]$selected.goal.x,2)+[Math]::Pow([double]$selected.requested.z-[double]$selected.goal.z,2))
        $route=[Math]::Sqrt([Math]::Pow([double]$selected.traceEnd.x-[double]$selected.goal.x,2)+[Math]::Pow([double]$selected.traceEnd.z-[double]$selected.goal.z,2))
        $radians=[double]$selected.angle*[Math]::PI/180.0
        if(-not(Test-KmcChunk7FiniteNumber $plan.selectedAngle)-or$selected.angle-ne$plan.selectedAngle-or
           [Math]::Abs([double]$selected.requested.x-([double]$plan.start.x+2.0*[Math]::Sin($radians)))-gt0.0001-or
           [Math]::Abs([double]$selected.requested.y-[double]$plan.start.y)-gt0.0001-or
           [Math]::Abs([double]$selected.requested.z-([double]$plan.start.z+2.0*[Math]::Cos($radians)))-gt0.0001-or
           -not(Test-KmcChunk7FiniteNumber $selected.travel)-or-not(Test-KmcChunk7FiniteNumber $selected.clampDistance)-or
           -not(Test-KmcChunk7FiniteNumber $selected.routeResidual)-or
           [Math]::Abs($travel-[double]$selected.travel)-gt0.0001-or
           [Math]::Abs($clamp-[double]$selected.clampDistance)-gt0.0001-or
           [Math]::Abs($route-[double]$selected.routeResidual)-gt0.0001-or
           $selected.travel-lt1.8-or$selected.travel-gt2.2-or
           $selected.clampDistance-gt0.15-or$selected.routeResidual-gt0.05){
            throw 'Area continuation selected a blocked, clamped or short native ray.'
        }
    }
}

# The owned area route reuses the strict full-field replay reader without changing
# any legacy/repeated route. Its raw snapshot retains the same replay-field shape.
function Assert-KmcChunk7OwnedAreaField($Request,$Observation){
 $r=$Observation.areaTransfer;$retained=$r.identityRetention
 Assert-KmcExactProperties $retained @('capacity','ids','faults','released','retainedAfterRelease') 'owned area retention'
 Assert-KmcOrdinaryInteger $retained.capacity 128 128;Assert-KmcOrdinaryInteger $retained.faults 0 0;Assert-KmcOrdinaryInteger $retained.retainedAfterRelease 0 0;Assert-KmcChunk7Boolean $retained.released $true
 if($retained.ids-isnot[Array]-or$retained.ids.Count-lt1-or$retained.ids.Count-gt128){throw 'Owned area retention is missing or unbounded.'}
 $registry=[Collections.Generic.HashSet[int]]::new();foreach($id in $retained.ids){Assert-KmcManualSeedObjectIdentity $id;if(-not$registry.Add([int]$id)){throw 'Owned area retention repeats a label.'}}
 $used=[Collections.Generic.HashSet[int]]::new();$phases=@($r.before,$r.headerStarting,$r.headerStaged,$r.after)
 for($i=0;$i-lt4;$i++){
  $p=$phases[$i];$w=$p.ownedWorld;$f=$p.ownedField
  Assert-KmcExactProperties $w @('game','player','manager','state','areaState','mainState','worldScope','authorityGeneration') 'owned area world'
  Assert-KmcOrdinaryInteger $w.authorityGeneration 1 ([long]::MaxValue)
  $worldLabels=[Collections.Generic.HashSet[int]]::new()
  foreach($key in @('game','player','manager','state','areaState','mainState','worldScope')){Assert-KmcManualSeedObjectIdentity $w.$key;if(-not$registry.Contains([int]$w.$key)-or-not$worldLabels.Add([int]$w.$key)){throw 'Owned area world aliases or escapes retained custody.'};[void]$used.Add([int]$w.$key)}
  $replay=[pscustomobject]@{ownedField=$f;area=$p.area;gameObject=$w.game;playerObject=$w.player;stateObject=$w.state;areaStateObject=$w.areaState;mainStateObject=$w.mainState;actors=@()}
  Assert-KmcChunk7OwnedReplayField $Request $replay $registry 1 0
  foreach($key in @('partyIds','currentPartyIds','currentCharacterIds','remoteCapturedIds','arrangement','pairs','identities')){if((ConvertTo-KmcChunk7Canonical $f.custody.$key)-cne(ConvertTo-KmcChunk7Canonical $Observation.inheritedFixture.$key)){throw 'Owned area transfer changed inherited roster, facts, progression or money.'}}
  foreach($a in $f.actors){foreach($key in @('object','descriptor','viewObject')){if($worldLabels.Contains([int]$a.$key)){throw 'Owned area actor aliases a world owner.'};[void]$used.Add([int]$a.$key)}
   $view=@($p.views|Where-Object id -CEQ $a.id);if($view.Count-gt1-or($a.id-cin(Get-KmcChunk7OwnedPairIds $Request)-and($view.Count-ne1-or$view[0].viewId-ne$a.view))){throw 'Owned area full field differs from the independently observed mounted views.'}
  }
  foreach($pair in $f.custody.pairs){foreach($key in @('feature','rankFact')){if($worldLabels.Contains([int]$pair.$key)){throw 'Owned area fact aliases a world owner.'};[void]$used.Add([int]$pair.$key)}}
  if($i-gt0){
   foreach($key in @('game','player','manager','state','authorityGeneration')){if($w.$key-ne$r.before.ownedWorld.$key){throw 'Owned area transferred a different native world or authority.'}}
   if($w.worldScope-eq$r.before.ownedWorld.worldScope){throw 'Owned area did not adopt a distinct destination scope.'}
   foreach($key in @('characters','actors')){if((ConvertTo-KmcChunk7Canonical $f.$key)-cne(ConvertTo-KmcChunk7Canonical $r.before.ownedField.$key)){throw 'Owned area replaced a field actor, view, relationship or ready command boundary.'}}
   if($i-gt1-and(ConvertTo-KmcChunk7Canonical $w)-cne(ConvertTo-KmcChunk7Canonical $r.headerStarting.ownedWorld)){throw 'Owned area destination authority changed during native saving.'}
  }
 }
 $s=$Observation.ownedOutputCompletion.areaSaveIdentity
 foreach($key in @('operation','requested','prepared','completion','sourceDescriptor')){if($used.Contains([int]$s.$key)){throw 'Owned area native save identity aliases a field/world owner.'};[void]$used.Add([int]$s.$key)}
 if($used.Count-ne$registry.Count-or@($registry|Where-Object{-not$used.Contains($_)}).Count-ne0){throw 'Owned area retention contains an unobserved object.'}
}
# Engine descriptor strings may mix separators under the engine-shaped isolated root; file identity, not spelling, is compared.
function Assert-KmcChunk7SameArchivePath($Observed,$Expected,[string]$Description){
 if($Observed-isnot[string]-or[string]::IsNullOrWhiteSpace($Observed)-or$Expected-isnot[string]-or[string]::IsNullOrWhiteSpace($Expected)-or[IO.Path]::GetFullPath($Observed)-cne[IO.Path]::GetFullPath($Expected)){throw ('Native path identity differs: '+$Description+'.')}
}
# Sprint 1 (SAVE-02): the genuine AfterEntry autosave overwrote the admitted pre-existing slot. The receipt
# must pin the slot the fixture pre-seeded, prove the engine's own selector chose it under the one-slot
# capacity, and show exactly one replacement commit landing new bytes at that leaf with the staging leaf gone.
function Assert-KmcChunk7OwnedAreaOverwrite($Request,$Observation){
 if($null-eq$Observation.PSObject.Properties['areaOverwrite']){throw 'Area overwrite producer lacks its overwrite receipt.'}
 $o=$Observation.areaOverwrite;$s=$Request.ownedPairs;$w=$Observation.nativeWrite;$leaf=Get-KmcChunk7OwnedOutputLeaf $Request $Observation
 Assert-KmcExactProperties $o @('contract','eventDispatch','existingRunId','existingArtifactSha256','existingSha256','existingLeaf','existingName','existingArea','existingFolderName','existingLength','previousSha256','stagingLeaf','stagingNumber','commitCountBefore','replacementFailuresBefore','replacementSiteInvokedDirectly','explicitSaveGameDispatches','preseededByFixture','existingLoaded','engineShapedRoot','nativeSlotCapacity','savedOfCategory','nextAutoslotIsExisting','requestedIsExistingSlot','commitCountAfter','commitRecorded','committedDestination','path','landedFolderName','sha256','length','bytesChanged','stagingAbsent','writtenIsNewDescriptor','requestedDescriptorRetired','holdsReleased','failedSaves','replacementFailures','nativeWrites') 'area overwrite receipt'
 Assert-KmcBootstrapInventoryString $o.contract 'sprint1-area-auto-overwrite-v1'
 if($o.eventDispatch-isnot[string]-or$o.eventDispatch-cnotlike'Game.LoadArea(enterPoint, AutoSaveMode.AfterEntry)*'){throw 'Area overwrite was not requested by the engine''s own after-entry autosave dispatch.'}
 foreach($pair in @(@('existingRunId',$s.existingAutoRunId),@('existingArtifactSha256',$s.existingAutoArtifactSha256),@('existingSha256',$s.existingAutoSha256),@('previousSha256',$s.existingAutoSha256),@('existingLeaf',$leaf),@('existingLeaf','Auto_1.zks'),@('existingName',$w.nativeName),@('existingArea',$Request.persistenceAreaTarget.area),@('stagingLeaf','Auto_2.zks'),@('sha256',$w.sha256))){Assert-KmcBootstrapInventoryString $o.($pair[0]) $pair[1]}
 if($o.sha256-ceq$o.previousSha256){throw 'Area overwrite left the pre-existing bytes at the slot.'}
 foreach($flag in @('preseededByFixture','nextAutoslotIsExisting','requestedIsExistingSlot','commitRecorded','bytesChanged','stagingAbsent','writtenIsNewDescriptor','requestedDescriptorRetired','holdsReleased')){Assert-KmcChunk7Boolean $o.$flag $true}
 foreach($flag in @('replacementSiteInvokedDirectly','existingLoaded')){Assert-KmcChunk7Boolean $o.$flag $false}
 Assert-KmcChunk7Boolean $o.engineShapedRoot ($null-ne$Request.PSObject.Properties['engineShapedSaveRoot']-and$Request.engineShapedSaveRoot-eq$true)
 Assert-KmcOrdinaryInteger $o.stagingNumber 2 2;Assert-KmcOrdinaryInteger $o.savedOfCategory 1 1;Assert-KmcOrdinaryInteger $o.explicitSaveGameDispatches 0 0;Assert-KmcOrdinaryInteger $o.nativeWrites 1 1
 foreach($key in @('replacementFailuresBefore','replacementFailures','failedSaves')){Assert-KmcOrdinaryInteger $o.$key 0 0}
 Assert-KmcOrdinaryInteger $o.commitCountBefore 0 ([int]::MaxValue);Assert-KmcOrdinaryInteger $o.commitCountAfter ([int]$o.commitCountBefore+1) ([int]$o.commitCountBefore+1)
 if(-not(Test-KmcChunk7FiniteNumber $o.nativeSlotCapacity)-or[double]$o.nativeSlotCapacity-ne1.0){throw 'Area overwrite ran outside the disposable one-slot Auto capacity.'}
 if(-not(Test-KmcExactJsonInteger $o.existingLength)-or$o.existingLength-le0-or-not(Test-KmcExactJsonInteger $o.length)-or$o.length-ne$w.length){throw 'Area overwrite archive lengths are invalid.'}
 foreach($key in @('committedDestination','path','landedFolderName','existingFolderName')){Assert-KmcChunk7SameArchivePath $o.$key $w.path ('area overwrite '+$key)}
}
function Assert-KmcChunk7OwnedAreaSaveIdentity($Request,$Observation){
 $r=$Observation.areaTransfer;$a=$r.headerStarting.nativeSaveBoundary;$b=$r.headerStaged.nativeSaveBoundary;$c=$Observation.ownedOutputCompletion.areaSaveIdentity
 $properties=@('operation','requested','prepared','sourceDescriptor','requestedType','requestedName','preparedType','preparedName','preparedLeaf','preparedArea','preparedPath','completionCount','callbackCount','snapshotCount')
 foreach($v in @($a,$b)){Assert-KmcExactProperties $v $properties 'owned area native header';$ids=[Collections.Generic.HashSet[int]]::new();foreach($key in @('operation','requested','prepared','sourceDescriptor')){Assert-KmcManualSeedObjectIdentity $v.$key;if(-not$ids.Add([int]$v.$key)){throw 'Owned area save operation/request/copy/source identities alias.'}}
  foreach($key in @('requestedType','preparedType')){Assert-KmcBootstrapInventoryString $v.$key 'Auto'}
  foreach($key in @('requestedName','preparedName')){Assert-KmcBootstrapInventoryString $v.$key $Observation.nativeWrite.nativeName}
  # An overwrite prepares its copy at the reserved staging leaf, which the engine's replacement then moves onto the output leaf.
  $outputLeaf=Get-KmcChunk7OwnedOutputLeaf $Request $Observation;$overwrite=Test-KmcChunk7OwnedAreaOverwrite $Request
  if($v.preparedLeaf-isnot[string]-or($v.preparedLeaf-cne$outputLeaf-and-not($overwrite-and$v.preparedLeaf-ceq'Auto_2.zks'))){throw 'Owned area prepared copy is neither the output leaf nor, for an overwrite, its reserved staging leaf.'}
  Assert-KmcBootstrapInventoryString $v.preparedArea $Request.persistenceAreaTarget.area
  Assert-KmcChunk7SameArchivePath $v.preparedPath (Join-Path (Split-Path -Parent $Observation.nativeWrite.path) $v.preparedLeaf) 'owned area prepared copy'
  foreach($key in @('completionCount','callbackCount')){Assert-KmcOrdinaryInteger $v.$key 0 0}
 }
 Assert-KmcOrdinaryInteger $a.snapshotCount 0 0;Assert-KmcOrdinaryInteger $b.snapshotCount 1 1
 foreach($key in $properties|Where-Object{$_-cne'snapshotCount'}){if((ConvertTo-KmcChunk7Canonical $a.$key)-cne(ConvertTo-KmcChunk7Canonical $b.$key)){throw 'Owned area staging replaced the captured native request or prepared copy.'}}
 Assert-KmcExactProperties $c @('operation','requested','prepared','completion','path','sourceDescriptor') 'owned area completed native save'
 $ids=[Collections.Generic.HashSet[int]]::new();foreach($key in @('operation','requested','prepared','completion','sourceDescriptor')){Assert-KmcManualSeedObjectIdentity $c.$key;if(-not$ids.Add([int]$c.$key)){throw 'Owned area completion identities alias.'};if($key-cne'completion'-and$c.$key-ne$a.$key){throw 'Owned area completion is not the captured native request/copy.'}}
 Assert-KmcChunk7SameArchivePath $c.path $Observation.nativeWrite.path 'owned area completion'
 if($r.identityRetention.ids-isnot[Array]){throw 'Owned area completion has no retained observer.'};foreach($id in $ids){if($id-cnotin$r.identityRetention.ids){throw 'Owned area completion was not retained through worker settlement.'}}
}
