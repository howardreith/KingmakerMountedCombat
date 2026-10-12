# Exact new-case readers only. Repeated loads do not qualify native area transfers.
Set-StrictMode -Version Latest
function Test-KmcChunk7RepeatedWorldCase([string]$Case) { $Case-cin@('multi-exploration-three-loads-manual','multi-exploration-three-area-transfers-manual') }
function Get-KmcChunk7PersistenceSourceCase([string]$Case) { if(Test-KmcChunk7RepeatedWorldCase $Case){return 'multi-exploration-manual'};return $Case }
function Assert-KmcChunk7RepeatedWorldRequest($Request) {
    $repeated=Test-KmcChunk7RepeatedWorldCase $Request.persistenceCase
    $areas=$Request.persistenceCase-ceq'multi-exploration-three-area-transfers-manual'
    $hasTargets=@($Request.PSObject.Properties.Name)-ccontains'persistenceReplayTargets'
    if(-not$repeated){if($hasTargets){throw 'Repeated area route belongs only to its exact cold case.'};return}
    if($Request.scenario-cne'chunk7-persistence-load'){throw 'Repeated worlds require the selected cold-load family.'}
    if(-not$areas){if($hasTargets){throw 'Repeated native loads cannot claim area-transfer coverage.'};return}
    if(-not$hasTargets-or$Request.persistenceReplayTargets-isnot[Array]-or$Request.persistenceReplayTargets.Count-ne3){throw 'Three exact native area destinations are mandatory.'}
    $prior=$Request.fixture.working.area
    foreach($target in $Request.persistenceReplayTargets){
        Assert-KmcExactProperties $target @('enterPoint','area','autoSaveMode') 'Repeated native area target'
        foreach($field in @('enterPoint','area')){if($target.$field-isnot[string]-or$target.$field-cnotmatch'^[0-9a-f]{32}$'-or$target.$field-ceq('0'*32)){throw 'Native area target must be an exact nonzero GUID pin.'}}
        if($target.area-ceq$prior-or$target.enterPoint-ceq$target.area-or$target.autoSaveMode-cne'None'){throw 'Repeated areas forbid same-area teleport and unselected autosave.'}
        $prior=$target.area
    }
}
function Assert-KmcChunk7ReplayObject($Value,[bool]$AllowZero=$false) {
    if(-not(Test-KmcExactJsonInteger $Value)-or$Value-lt[int]::MinValue-or$Value-gt[int]::MaxValue-or(-not$AllowZero-and$Value-eq0)){throw 'Native object identity must be an exact nonzero signed integer.'}
}
function Assert-KmcChunk7ReplaySubscriptions($Value,[bool]$InNativeCallback=$false) {
    Assert-KmcExactProperties $Value @('assemblyMvid','executing','uniqueOwnedSubscribers','ownedInterfaceEntries','entries') 'Native KMC subscription inventory'
    Assert-KmcChunk7Boolean $Value.executing $InNativeCallback
    Assert-KmcChunk7NativeCounters $Value @('uniqueOwnedSubscribers','ownedInterfaceEntries')
    if($Value.assemblyMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'-or$Value.entries-isnot[Array]-or$Value.ownedInterfaceEntries-ne$Value.entries.Count-or$Value.entries.Count-gt256){throw 'Native subscription inventory is incomplete or unbounded.'}
    $rows=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $identities=[Collections.Generic.HashSet[int]]::new()
    foreach($row in $Value.entries){
        Assert-KmcExactProperties $row @('interface','type','identity') 'Native subscription row'
        Assert-KmcChunk7ReplayObject $row.identity
        if($row.interface-isnot[string]-or$row.type-isnot[string]-or$row.interface.Length-eq0-or$row.interface.Length-gt256-or$row.type-cnotmatch'^KingmakerMountedCombat\.'-or$row.type.Length-gt256-or-not$rows.Add($row.interface+'|'+$row.type+'|'+$row.identity)){throw 'Subscription row is invalid or duplicated.'}
        $null=$identities.Add($row.identity)
    }
    if($identities.Count-ne$Value.uniqueOwnedSubscribers){throw 'Subscription unique-owner count differs from raw identities.'}
}
function Assert-KmcChunk7ReplayOwners($Owners,$Archive,[bool]$Mounted) {
    if($Owners-isnot[Array]-or$Owners.Count-ne$(if($Mounted){2}else{0})){throw 'Mounted or settled native ownership differs.'}
    if(-not$Mounted){return}
    foreach($pair in $Archive.Pairs){
        $actual=@($Owners|Where-Object ownerId -CEQ $pair.PairId)
        if($actual.Count-ne1){throw 'Stable native owner was lost or duplicated.'}
        $v=$actual[0];Assert-KmcChunk7NativeCounters $v @('generation')
        if($v.generation-ne$pair.Generation-or$v.riderId-cne$pair.Rider.Id-or$v.mountId-cne$pair.Mount.Id-or$v.state-cne'Active'){throw 'Stable generation/actor roles changed.'}
        if(@($v.PSObject.Properties.Name)-ccontains'retainsObligations'-and$v.retainsObligations-isnot[bool]){throw 'Retained obligation predicate is not a JSON boolean.'}
    }
}
function Assert-KmcChunk7ReplayWorld($World,$Archive,[string]$Area,[bool]$Mounted,[bool]$RequireActors=$true,[bool]$InNativeCallback=$false,[bool]$PendingViewDestroy=$false) {
    Assert-KmcChunk7NativeCounters $World @('gameTicks','boundaries','participants','failedConstructions','allocationActors','active','preparedOrViewResidue','liveControlBoundaries','commandOwners','nativeDisposals','areaSuspensions','areaResumes','semanticRestores','presentationRestores','nativePostLoads','nativeCastRequests','snapshotCount','duplicateFacts')
    Assert-KmcChunk7ReplayObject $World.frame $true
    foreach($field in @('gameObject','playerObject','stateObject','areaStateObject','mainStateObject')){Assert-KmcChunk7ReplayObject $World.$field}
    foreach($field in @('playerInCombat','unsettledPreparation','relationshipTransition','serializationBusy','pendingWrites','saveScope','saveFence')){Assert-KmcChunk7Boolean $World.$field $false}
    if($World.area-cne$Area-or$World.boundaries-ne2-or$World.participants-ne2-or$World.failedConstructions-ne0-or$World.allocationActors-ne0-or
       $World.nativeCastRequests-ne0-or$World.snapshotCount-ne0-or$World.duplicateFacts-ne0-or$World.active-ne$(if($Mounted){2}else{0})-or$World.preparedOrViewResidue-ne$(if($Mounted){2}else{0})-or$World.liveControlBoundaries-ne0-or$World.commandOwners-ne0){throw 'Repeated world grew boundary/custody/ledger residue or changed area.'}
    Assert-KmcChunk7ReplayOwners $World.owners $Archive $Mounted
    if($Mounted-and(ConvertTo-KmcChunk7Canonical @($World.nativeSlots|Sort-Object ActorId,Index))-cne(ConvertTo-KmcChunk7Canonical @($Archive.Slots|Sort-Object ActorId,Index))){throw 'Repeated world lost exact all-pair native control slots.'}
    Assert-KmcChunk7ReplaySubscriptions $World.subscriptions $InNativeCallback
    if(-not$RequireActors){return}
    if($World.actors-isnot[Array]-or$World.actors.Count-ne4){throw 'Repeated world must observe all four exact actors.'}
    foreach($savedActor in @($Archive.Pairs|ForEach-Object{$_.Rider;$_.Mount})){
        $rows=@($World.actors|Where-Object id -CEQ $savedActor.Id)
        if($rows.Count-ne1){throw 'Repeated world substituted or duplicated native actor.'}
        $a=$rows[0];foreach($f in @('object','descriptor','view','viewObject')){Assert-KmcChunk7ReplayObject $a.$f}
        Assert-KmcChunk7ReplayObject $a.agentOverride $true
        Assert-KmcChunk7Boolean $a.inState $true;Assert-KmcChunk7Boolean $a.commandsEmpty $true
        Assert-KmcChunk7Schema1Debt $a.native $savedActor $World.gameTicks $Archive.GameTimeTicks
        $rider=$savedActor.Id-cin@($Archive.Pairs|ForEach-Object{$_.Rider.Id})
        if($a.overrides-isnot[Array]-or($Mounted-and$rider-and$a.overrides.Count-ne1)-or((-not$Mounted-or-not$rider)-and$a.overrides.Count-ne0-and(-not$PendingViewDestroy-or-not$rider-or$a.overrides.Count-ne1))){throw 'Native movement override is missing, duplicated or retained outside its native destruction phase.'}
        foreach($v in $a.overrides){Assert-KmcChunk7ReplayObject $v}
        if($Mounted-and$rider){if($a.agentOverride-ne$a.overrides[0]){throw 'Mounted rider override is not its exact live component.'}}
        elseif($a.agentOverride-ne0){throw 'Unmounted/mount actor retained a rider movement override.'}
    }
}
function Assert-KmcChunk7ReplayPresentation($World,$Archive) {
    Assert-KmcChunk7NativeCounters $World @('gameTicks')
    Assert-KmcChunk7Boolean $World.playerInCombat $false;Assert-KmcChunk7Boolean $World.turnBased $false
    Assert-KmcChunk7ReplayOwners $World.owners $Archive $true
    if($World.actors-isnot[Array]-or$World.actors.Count-ne4-or$null-ne$World.combat-or$World.participations-isnot[Array]-or$World.participations.Count-gt2-or
       (ConvertTo-KmcChunk7Canonical @($World.nativeControls|Sort-Object ActorId,Index))-cne(ConvertTo-KmcChunk7Canonical @($Archive.Slots|Sort-Object ActorId,Index))){throw 'Native repeated presentation omitted actors/controls or invented combat.'}
    foreach($savedActor in @($Archive.Pairs|ForEach-Object{$_.Rider;$_.Mount})){
        $rows=@($World.actors|Where-Object{$_.Native.Id-ceq$savedActor.Id});if($rows.Count-ne1){throw 'Native presentation has a missing or duplicate actor.'}
        Assert-KmcChunk7Boolean $rows[0].InCombat $false
        if($rows[0].Prepared-isnot[bool]){throw 'Native prepared state is not boolean.'}
        Assert-KmcChunk7Schema1Debt $rows[0].Native $savedActor $World.gameTicks $Archive.GameTimeTicks
    }
    $seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach($p in $World.participations){
        $pair=@($Archive.Pairs|Where-Object PairId -CEQ $p.OwnerId)
        if($pair.Count-ne1-or-not$seen.Add($p.OwnerId)-or$p.MountedPairId-cne$p.OwnerId-or-not(Test-KmcExactJsonInteger $p.Generation)-or$p.Generation-ne$pair[0].Generation){throw 'Armed-only participation lost native owner identity.'}
        $s=$p.State
        Assert-KmcExactProperties $s @('RiderId','MountId','Activation','BoundaryIsCurrent','Boundary','Partner','SplitReleaseRound','PendingSplitId','PendingSplitRound','RenewalNotBeforeTicks') 'Repeated exploration participation'
        if($s.RiderId-cne$pair[0].Rider.Id-or$s.MountId-cne$pair[0].Mount.Id-or$null-ne$s.Activation-or$null-ne$s.Boundary-or$null-ne$s.Partner-or$null-ne$s.PendingSplitId-or
           -not(Test-KmcExactJsonInteger $s.SplitReleaseRound)-or$s.SplitReleaseRound-ne-1-or-not(Test-KmcExactJsonInteger $s.PendingSplitRound)-or$s.PendingSplitRound-ne-1-or
           -not(Test-KmcExactJsonInteger $s.RenewalNotBeforeTicks)-or$s.RenewalNotBeforeTicks-ne0){throw 'Repeated exploration invented native turn participation.'}
        Assert-KmcChunk7Boolean $s.BoundaryIsCurrent $false
    }
}
function Assert-KmcChunk7ReplayRestoration($Probe,$Archive,$Request,[int]$Loads) {
    Assert-KmcChunk7Boolean $Probe.complete $true;Assert-KmcChunk7Boolean $Probe.installedBeforeWorldLoad $true
    Assert-KmcChunk7NativeCounters $Probe @('maximumEvents','dropped','errors')
    if($Probe.contract-cne'chunk7-native-restoration-entry-observer-v1'-or$Probe.maximumEvents-ne2048-or$Probe.dropped-ne0-or$Probe.errors-ne0-or
       $Probe.prepareMetric-cne'native-method-entries-including-production-refused-attempts'-or$Probe.hooks-isnot[Array]-or$Probe.hooks.Count-ne6-or$Probe.events-isnot[Array]-or$Probe.events.Count-ne(9*$Loads)){throw 'Repeated-load observer is incomplete, overrun or contains extra native entries.'}
    foreach($spec in @(@('06000C3C','TurnBased.Controllers.TurnController','Prepare'),@('0600939D','Kingmaker.Controllers.Combat.UnitCombatState','OnNewRound'))){
        $h=@($Probe.hooks|Where-Object token -CEQ $spec[0]);if($h.Count-ne1-or$h[0].method-cne$spec[2]-or$h[0].declaringType-cne$spec[1]-or$h[0].moduleMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'){throw 'Repeated-load native hook identity differs.'}
    }
    foreach($method in @('RestoreActor','RestoreDebt','TryRestoreArchiveCombat','RestoreArchivePresentation')){
        $h=@($Probe.hooks|Where-Object method -CEQ $method);$type=if($method-cin@('RestoreActor','RestoreDebt')){'KingmakerMountedCombat.Integration.NativeCombatActorPersistence'}else{'KingmakerMountedCombat.Integration.MountedPersistenceService'}
        if($h.Count-ne1-or$h[0].moduleMvid-cne$Request.dllMvid-or$h[0].declaringType-cne$type-or$h[0].token-cnotmatch'^06[0-9A-F]{6}$'){throw 'Repeated-load candidate hook identity differs.'}
    }
    for($epoch=0;$epoch-lt$Loads;$epoch++){
        $events=@($Probe.events[($epoch*9)..($epoch*9+8)])
        if(@($events|Where-Object{$_.kind-cnotin@('restore-debt-before','restore-debt-after','all-owner-presentation-restored')}).Count-ne0-or
           $events[8].kind-cne'all-owner-presentation-restored'-or$events[8].restorePhase-cne'all-pair-presentation-restored'){throw 'Repeated epoch invented Prepare/round/combat restoration or attached before debt.'}
        foreach($actor in @($Archive.Pairs|ForEach-Object{$_.Rider;$_.Mount})){
            $b=@($events|Where-Object{$_.kind-ceq'restore-debt-before'-and$_.actorId-ceq$actor.Id});$a=@($events|Where-Object{$_.kind-ceq'restore-debt-after'-and$_.actorId-ceq$actor.Id})
            if($b.Count-ne1-or$a.Count-ne1-or[Array]::IndexOf($events,$b[0])-ge[Array]::IndexOf($events,$a[0])-or$a[0].restorePhase-cne'validated-before-native-mutation'-or
               (ConvertTo-KmcChunk7Canonical $a[0].debt)-cne(ConvertTo-KmcChunk7Canonical $actor)){throw 'Repeated epoch lost exactly-once native actor debt.'}
            foreach($row in @($b[0],$a[0])){Assert-KmcChunk7Boolean $row.inCombat $false;if($row.prepared-isnot[bool]){throw 'Repeated prepared state is not boolean.'}}
            if($b[0].prepared-ne$a[0].prepared){throw 'Repeated debt restoration manufactured preparation.'}
        }
        Assert-KmcChunk7ReplayPresentation $events[8].snapshot $Archive
    }
}
function Assert-KmcChunk7RepeatedWorldObservation($Request,$Artifact) {
    Assert-KmcChunk7RepeatedWorldRequest $Request
    if(-not(Test-KmcChunk7RepeatedWorldCase $Request.persistenceCase)-or$Artifact.status-cne'PASS'){throw 'Repeated-world PASS requires its exact declared case.'}
    $o=$Artifact.observations;$r=$o.repeatedWorlds;$areas=$Request.persistenceCase-ceq'multi-exploration-three-area-transfers-manual';$loads=if($areas){1}else{3};$transfers=if($areas){3}else{0}
    Assert-KmcChunk7NativeCounters $o @('nativeSchema','nativeCapacity','processId')
    if($o.checkpoint-cne$Request.persistenceCase-or$o.sourceWorkingSha256-cne$Request.fixture.working.sha256-or$o.sourceWorkingSha256-cne'5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6'-or
       $o.nativeSchema-ne3-or$o.nativeCapacity-ne2-or$o.processId-le0-or@($o.inputs).Count-ne0-or@($o.movements).Count-ne0-or
       (ConvertTo-KmcChunk7Canonical $o.loadedDescriptor)-cne(ConvertTo-KmcChunk7Canonical $Request.persistenceLoad)){throw 'Repeated-world fixture/candidate/source differs or replayed native input.'}
    foreach($f in @('cold','nativeProgressionRequiresExternalWorkingRestoration')){Assert-KmcChunk7Boolean $o.$f $true}
    foreach($f in @('ordinaryRespecProven','combatDebtProof')){Assert-KmcChunk7Boolean $o.$f $false}
    $archive=$o.loadedArchive;Assert-KmcChunk7PersistenceArchive $archive $Request
    foreach($f in @('requiredNativeLoads','requiredAreaTransfers','nativeLoadRequests','nativeWriteRequests')){Assert-KmcChunk7NativeCounters $r @($f)}
    foreach($f in @('performanceProof','diagnosticsOffProof','allocationRateProof')){Assert-KmcChunk7Boolean $r.$f $false}
    Assert-KmcChunk7Boolean $r.inputUnchanged $true;Assert-KmcChunk7Boolean $r.observersReleased $true
    if($r.contract-cne'chunk7-repeated-worlds-v1'-or$r.kind-cne$(if($areas){'area-transfer'}else{'native-load'})-or$r.requiredNativeLoads-ne$loads-or$r.requiredAreaTransfers-ne$transfers-or
       $r.nativeLoadRequests-ne$loads-or$r.nativeWriteRequests-ne0-or$r.inputSha256-cne$Request.persistenceLoad.sha256-or$r.worlds-isnot[Array]-or$r.worlds.Count-ne($loads+$transfers)-or
       $r.requests-isnot[Array]-or$r.requests.Count-ne$(if($areas){3}else{2})){throw 'Repeated native admissions/counts differ from the finite selected campaign.'}
    $identity=$r.identityRetention
    Assert-KmcChunk7NativeCounters $identity @('capacity','faults','retainedAfterRelease');Assert-KmcChunk7Boolean $identity.released $true
    if($identity.capacity-ne256-or$identity.faults-ne0-or$identity.retainedAfterRelease-ne0-or$identity.ids-isnot[Array]-or$identity.ids.Count-eq0-or$identity.ids.Count-gt256){throw 'Bounded retained native identity registry did not settle.'}
    $nativeIds=[Collections.Generic.HashSet[int]]::new()
    foreach($id in $identity.ids){Assert-KmcChunk7ReplayObject $id;if(-not$nativeIds.Add($id)){throw 'Retained native identity labels duplicated.'}}
    foreach($state in @($r.worlds)+@($r.requests|ForEach-Object{$_.before})+@($r.retention.events|ForEach-Object{$_.state})+@($r.settled)){
        foreach($field in @('gameObject','playerObject','stateObject','areaStateObject','mainStateObject')){if(-not$nativeIds.Contains($state.$field)){throw 'Native world label lacks retained exact-reference registration.'}}
        foreach($actor in $state.actors){foreach($field in @('object','descriptor','viewObject')){if(-not$nativeIds.Contains($actor.$field)){throw 'Native actor label lacks retained exact-reference registration.'}}}
    }
    $initial=$r.worlds[0];$previous=$null
    for($index=0;$index-lt$r.worlds.Count;$index++){
        $w=$r.worlds[$index];$nativeLoads=if($areas){1}else{$index+1};$area=if($areas-and$index-gt0){$Request.persistenceReplayTargets[$index-1].area}else{$Request.persistenceLoad.area}
        $label=if($areas-and$index-gt0){'area-'+$index}else{'load-'+($index+1)}
        Assert-KmcChunk7ReplayWorld $w $archive $area $true
        if($w.label-cne$label-or$w.nativeDisposals-ne($nativeLoads-1)-or$w.semanticRestores-ne(4*$nativeLoads)-or$w.presentationRestores-ne(2*$nativeLoads)-or$w.nativePostLoads-ne4-or
           $w.areaSuspensions-ne$(if($areas){$index}else{0})-or$w.areaResumes-ne$(if($areas){2*$index}else{0})-or
           (ConvertTo-KmcChunk7Canonical $w.subscriptions)-cne(ConvertTo-KmcChunk7Canonical $initial.subscriptions)){throw 'Repeated world counters/subscriptions differ or accumulate.'}
        if($w.selectionRebinding-isnot[Array]){throw 'Repeated world omitted selection migration intent.'}
        foreach($selected in $w.selectionRebinding){Assert-KmcChunk7ReplayObject $selected.object;if($selected.id-isnot[string]-or$selected.id-cnotmatch'^[0-9a-f-]{36}$'){throw 'Selection migration has an invalid actor identity.'}}
        if($previous){
            if($w.gameObject-ne$previous.gameObject-or$w.frame-le$previous.frame){throw 'Repeated world replaced Game or observation ordering.'}
            foreach($f in @('playerObject','stateObject')){if(($w.$f-eq$previous.$f)-ne$areas){throw 'Native load must replace and area must retain original world objects.'}}
            if($w.areaStateObject-eq$previous.areaStateObject-or$w.mainStateObject-eq$previous.mainStateObject){throw 'Repeated boundary did not replace native area state.'}
            foreach($a in $w.actors){$old=@($previous.actors|Where-Object id -CEQ $a.id)[0]
                foreach($f in @('object','descriptor','view','viewObject')){if(($a.$f-eq$old.$f)-ne$areas){throw 'Repeated world actor/view identity violates its selected native boundary.'}}
                if($a.id-cin@($archive.Pairs|ForEach-Object{$_.Rider.Id})-and$a.agentOverride-eq$old.agentOverride){throw 'Repeated boundary retained stale movement authority.'}
            }
            if((ConvertTo-KmcChunk7Canonical @($w.selectionRebinding|ForEach-Object{$_.id}))-cne(ConvertTo-KmcChunk7Canonical @($initial.selectionRebinding|ForEach-Object{$_.id}))){throw 'Repeated load changed original selection intent.'}
            foreach($selected in $w.selectionRebinding){$old=@($previous.selectionRebinding|Where-Object id -CEQ $selected.id)[0];if(($selected.object-eq$old.object)-ne$areas){throw 'Selection retained old native actor objects across a load.'}}
            $q=$r.requests[$index-1];Assert-KmcChunk7NativeCounters $q @('ordinal')
            Assert-KmcChunk7ReplayWorld $q.before $archive $previous.area $true
            foreach($f in @('gameObject','playerObject','stateObject','areaStateObject','mainStateObject')){if($q.before.$f-ne$previous.$f){throw 'Dispatch lost its admitted prior native world.'}}
            foreach($actor in $q.before.actors){$old=@($previous.actors|Where-Object id -CEQ $actor.id)[0];foreach($f in @('object','descriptor','view','viewObject','agentOverride')){if($actor.$f-ne$old.$f){throw 'Dispatch observed a substituted prior actor/view/control.'}}}
            if($q.kind-cne$r.kind-or$q.ordinal-ne$(if($areas){$index}else{$index+1})){throw 'Native replay dispatch ordinal differs.'}
            if($areas){$t=$Request.persistenceReplayTargets[$index-1];if($q.enterPoint-cne$t.enterPoint-or$q.area-cne$t.area-or$q.autoSaveMode-cne'None'){throw 'Native replay replaced the selected area route.'}}
            else{Assert-KmcChunk7Boolean $q.callbackObserved $true;Assert-KmcChunk7NativeCounters $q @('callbackNativeLoads');Assert-KmcChunk7ReplayObject $q.callbackFrame $true
                if($q.callbackNativeLoads-ne($index+1)-or$q.callbackFrame-gt$w.frame-or$q.callbackFrame-lt$q.before.frame-or$q.fileName-cne$Request.persistenceLoad.fileName-or$q.sha256-cne$Request.persistenceLoad.sha256){throw 'Native load callback is missing, stale or bound to another immutable input.'}}
        }
        $previous=$w
    }
    Assert-KmcChunk7ReplayRestoration $r.restoration $archive $Request $loads
    if((ConvertTo-KmcChunk7Canonical $r.restoration)-cne(ConvertTo-KmcChunk7Canonical $o.coldRestoration)){throw 'Final no-Prepare observer changed after sequence completion.'}
    $probe=$r.retention;Assert-KmcChunk7Boolean $probe.complete $true;Assert-KmcChunk7Boolean $probe.installedBeforeWorldLoad $true
    Assert-KmcChunk7NativeCounters $probe @('maximumEvents','errors','dropped')
    if($probe.contract-cne'chunk7-repeated-world-retention-v1'-or$probe.maximumEvents-ne24-or$probe.errors-ne0-or$probe.dropped-ne0-or$probe.hooks-isnot[Array]-or$probe.hooks.Count-ne3-or
       $probe.events-isnot[Array]-or$probe.events.Count-ne$(if($areas){6}else{2})-or(ConvertTo-KmcChunk7Canonical $probe)-cne(ConvertTo-KmcChunk7Canonical $r.finalRetention)){throw 'Retained-owner observer is partial or changed during cleanup.'}
    foreach($method in @('PrepareForNativeWorldDisposal','SuspendAreaPair','RestoreAreaPair')){$h=@($probe.hooks|Where-Object method -CEQ $method);if($h.Count-ne1-or$h[0].declaringType-cne'KingmakerMountedCombat.Integration.MountedPersistenceService'-or$h[0].moduleMvid-cne$Request.dllMvid-or$h[0].token-cnotmatch'^06[0-9A-F]{6}$'){throw 'Retention hook differs from exact candidate.'}}
    for($i=0;$i-lt$probe.events.Count;$i++){
        $e=$probe.events[$i];Assert-KmcChunk7NativeCounters $e @('before','after');$leg=if($areas){[int][Math]::Floor($i/2)+1}else{$i+1};$resume=$areas-and($i%2)-eq1
        $kind=if(-not$areas){'native-world-cleanup-before-disposal'}elseif($resume){'area-restored'}else{'area-suspended'}
        $before=if($resume){($leg-1)*2}else{$leg-1};$after=if($resume){$leg*2}else{$leg}
        if($e.kind-cne$kind-or$e.before-ne$before-or$e.after-ne$after){throw 'Independent native cleanup/restore boundary count differs.'}
        $area=if($areas-and$resume){$Request.persistenceReplayTargets[$leg-1].area}else{$r.worlds[$leg-1].area}
        Assert-KmcChunk7ReplayWorld $e.state $archive $area $resume $true ($areas-and-not$resume) (-not$resume)
        $bound=if($resume){$r.worlds[$leg]}else{$r.worlds[$leg-1]}
        foreach($field in @('gameObject','playerObject','stateObject','areaStateObject','mainStateObject')){if($e.state.$field-ne$bound.$field){throw 'Native callback snapshot is not bound to its admitted world phase.'}}
        foreach($actor in $e.state.actors){$known=@($bound.actors|Where-Object id -CEQ $actor.id)[0];foreach($field in @('object','descriptor','view','viewObject')){if($actor.$field-ne$known.$field){throw 'Native callback actor belongs to another admitted world.'}}}
        if($e.state.frame-lt$r.worlds[$leg-1].frame-or$e.state.frame-gt$r.worlds[$leg].frame){throw 'Native callback escaped its selected boundary frame window.'}
        if(-not$resume){
            $prior=$r.worlds[$leg-1]
            foreach($actor in $e.state.actors){$old=@($prior.actors|Where-Object id -CEQ $actor.id)[0]
                foreach($id in $actor.overrides){if($id-cnotin$old.overrides){throw 'Pending native Destroy enumerated an unowned or new component.'}}
            }
        }
    }
    Assert-KmcChunk7ReplayWorld $r.settled $archive $previous.area $false
    foreach($f in @('gameObject','playerObject','stateObject','areaStateObject','mainStateObject')){if($r.settled.$f-ne$previous.$f){throw 'Cleanup touched an unadmitted final world.'}}
    foreach($actor in $r.settled.actors){$old=@($previous.actors|Where-Object id -CEQ $actor.id)[0];foreach($f in @('object','descriptor','view','viewObject')){if($actor.$f-ne$old.$f){throw 'Cleanup touched a substituted final native actor.'}}}
    if((ConvertTo-KmcChunk7Canonical $r.settled.subscriptions)-cne(ConvertTo-KmcChunk7Canonical $initial.subscriptions)){throw 'Final cleanup grew or lost KMC subscriptions.'}
    foreach($f in @('pairsClear','sourceHorseRemoved','controlsSettled','saveCustodyClear','selectionRestored','nativeProgressionRequiresExternalWorkingRestoration')){Assert-KmcChunk7Boolean $o.cleanup.$f $true}
    Assert-KmcChunk7Boolean $o.cleanup.ordinaryRespecProven $false
    if(@($o.cleanup.world.owners).Count-ne0){throw 'Repeated-world cleanup retained an owner.'}
}
