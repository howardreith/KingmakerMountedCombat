Set-StrictMode -Version Latest
function Assert-KmcChunk7Schema1Request($Request) {
    if($Request-is[Collections.IDictionary]){$Request=[pscustomobject]$Request}
    if($Request.scenario-cne'chunk7-persistence-load'-or$Request.persistenceCase-cne'legacy-schema1-resave-manual'-or
       -not$Request.PSObject.Properties['schema1DerivationSha256']-or$Request.schema1DerivationSha256-isnot[string]-or
       $Request.schema1DerivationSha256-cnotmatch'^[0-9a-f]{64}$'){throw 'Schema-1 native resave requires its exact cold request and derivative receipt hash.'}
}
function Get-KmcChunk7Schema1Owner($Metadata) {
    $sha=[Security.Cryptography.SHA256]::Create()
    try{$bytes=$sha.ComputeHash([Text.Encoding]::UTF8.GetBytes('kmc-schema3|'+$Metadata.CampaignId+'|'+$Metadata.Rider.Id+'|'+$Metadata.Mount.Id))}
    finally{$sha.Dispose()}
    return ([Guid]::new([byte[]]$bytes[0..15])).ToString('N')
}
function Assert-KmcChunk7Schema1Archive($Archive,$Request) {
    Assert-KmcExactProperties $Archive @('SchemaVersion','CampaignId','AreaId','GameTimeTicks','Policy','RulesId','Pairs','Participations','Slots','Combat') 'schema-1 migrated native archive'
    if(-not(Test-KmcExactJsonInteger $Archive.SchemaVersion)-or$Archive.SchemaVersion-ne3-or
       $Archive.CampaignId-cne$Request.fixture.working.gameId-or$Archive.AreaId-cne$Request.fixture.working.area-or
       -not(Test-KmcExactJsonInteger $Archive.GameTimeTicks)-or$Archive.GameTimeTicks-lt0-or
       $Archive.Policy-cne'rider-principal-distinct-native-v1'-or$Archive.RulesId-cne'crpg-transport-v1'-or
       $Archive.Pairs-isnot[Array]-or$Archive.Pairs.Count-ne1-or$Archive.Participations-isnot[Array]-or$Archive.Participations.Count-ne0-or
       $null-ne$Archive.Combat-or$Archive.Slots-isnot[Array]-or$Archive.Slots.Count-ne2){throw 'Schema-1 archive changed its exact exploration graph.'}
    $pair=$Archive.Pairs[0]
    Assert-KmcExactProperties $pair @('PairId','Generation','ProfileId','Rider','Mount') 'schema-1 stable owner'
    if($pair.PairId-isnot[string]-or$pair.PairId-cnotmatch'^[0-9a-f]{32}$'-or-not(Test-KmcExactJsonInteger $pair.Generation)-or$pair.Generation-ne1-or
       $pair.ProfileId-cne'medium-humanoid-mammoth-v1'-or$pair.Rider.Id-cne'b6628a77-4962-47a4-a17c-88d9836fc9d5'-or
       $pair.Mount.Id-cne'd79a4f6c-b74e-4868-95bd-533899131acb'){throw 'Schema-1 restored another pair, generation or profile.'}
    Assert-KmcChunk7PersistenceNativeActor $pair.Rider;Assert-KmcChunk7PersistenceNativeActor $pair.Mount
    $indices=@();$kinds=@()
    foreach($slot in $Archive.Slots){
        Assert-KmcExactProperties $slot @('ActorId','Index','Kind') 'schema-1 saved control'
        if($slot.ActorId-cne$pair.Rider.Id-or-not(Test-KmcExactJsonInteger $slot.Index)-or$slot.Index-lt0-or$slot.Index-gt127-or
           -not(Test-KmcExactJsonInteger $slot.Kind)-or$slot.Kind-cnotin@(2,3)-or$slot.Index-cin$indices-or$slot.Kind-cin$kinds){throw 'Schema-1 control bindings changed or duplicate a slot.'}
        $indices+=@($slot.Index);$kinds+=@($slot.Kind)
    }
}
function Assert-KmcChunk7Schema1Owners($Owners,$Archive) {
    $p=$Archive.Pairs[0]
    if($Owners-isnot[Array]-or$Owners.Count-ne1){throw 'Schema-1 world lost or invented an owner.'}
    $o=$Owners[0];Assert-KmcChunk7NativeCounters $o @('generation')
    if($o.ownerId-cne$p.PairId-or$o.generation-ne$p.Generation-or$o.riderId-cne$p.Rider.Id-or$o.mountId-cne$p.Mount.Id-or$o.state-cne'Active'){throw 'Schema-1 stable runtime custody differs.'}
}
function Assert-KmcChunk7Schema1Debt($Current,$Saved,[long]$Ticks,[long]$SavedTicks) {
    Assert-KmcChunk7PersistenceNativeActor $Current
    $elapsed=([double]$Ticks-[double]$SavedTicks)/10000000.0
    if($Current.Id-cne$Saved.Id-or$elapsed-lt0-or$elapsed-gt30){throw 'Schema-1 native debt observation escaped its exact actor/clock window.'}
    foreach($field in @('Standard','Move','Swift','Initiative','Reaction')){
        if($Current.$field-gt$Saved.$field+0.001-or$Current.$field+$elapsed+0.001+($elapsed*0.1)-lt$Saved.$field){throw 'Schema-1 debt changed beyond native elapsed clock progress.'}
    }
    if($Current.ReactionsRemaining-ne$Saved.ReactionsRemaining-or$Current.LastSurpriseTicks-ne$Saved.LastSurpriseTicks){throw 'Schema-1 reaction history changed.'}
}
function Assert-KmcChunk7Schema1World($World,$Archive) {
    Assert-KmcChunk7NativeCounters $World @('gameTicks')
    Assert-KmcChunk7Boolean $World.playerInCombat $false;Assert-KmcChunk7Boolean $World.turnBased $false
    Assert-KmcChunk7Schema1Owners $World.owners $Archive
    if($null-ne$World.combat-or$World.actors-isnot[Array]-or$World.actors.Count-ne2-or
       $World.participations-isnot[Array]-or$World.participations.Count-gt1-or
       (ConvertTo-KmcChunk7Canonical $World.nativeControls)-cne(ConvertTo-KmcChunk7Canonical $Archive.Slots)){throw 'Schema-1 world invented combat or lost exact controls/actors.'}
    foreach($saved in @($Archive.Pairs[0].Rider,$Archive.Pairs[0].Mount)){
        $actor=@($World.actors|Where-Object{$_.Native.Id-ceq$saved.Id})
        if($actor.Count-ne1){throw 'Schema-1 world actor is missing or duplicated.'}
        Assert-KmcChunk7Boolean $actor[0].InCombat $false
        if($actor[0].Prepared-isnot[bool]){throw 'Schema-1 native prepared flag is not a boolean.'}
        Assert-KmcChunk7Schema1Debt $actor[0].Native $saved $World.gameTicks $Archive.GameTimeTicks
    }
    foreach($entry in $World.participations){
        $p=$Archive.Pairs[0];$state=$entry.State
        Assert-KmcExactProperties $entry @('OwnerId','Generation','MountedPairId','State') 'schema-1 armed participation'
        Assert-KmcExactProperties $state @('RiderId','MountId','Activation','BoundaryIsCurrent','Boundary','Partner','SplitReleaseRound','PendingSplitId','PendingSplitRound','RenewalNotBeforeTicks') 'schema-1 armed state'
        if($entry.OwnerId-cne$p.PairId-or$entry.MountedPairId-cne$p.PairId-or-not(Test-KmcExactJsonInteger $entry.Generation)-or$entry.Generation-ne1-or
           $state.RiderId-cne$p.Rider.Id-or$state.MountId-cne$p.Mount.Id-or$null-ne$state.Activation-or$null-ne$state.Boundary-or$null-ne$state.Partner-or$null-ne$state.PendingSplitId-or
           -not(Test-KmcExactJsonInteger $state.SplitReleaseRound)-or$state.SplitReleaseRound-ne-1-or
           -not(Test-KmcExactJsonInteger $state.PendingSplitRound)-or$state.PendingSplitRound-ne-1-or
           -not(Test-KmcExactJsonInteger $state.RenewalNotBeforeTicks)-or$state.RenewalNotBeforeTicks-ne0){throw 'Schema-1 exploration binding invented native turn participation.'}
        Assert-KmcChunk7Boolean $state.BoundaryIsCurrent $false
    }
}
function Assert-KmcChunk7Schema1Probe($Probe,$Archive,$Request) {
    Assert-KmcChunk7Boolean $Probe.complete $true;Assert-KmcChunk7Boolean $Probe.installedBeforeWorldLoad $true
    Assert-KmcChunk7NativeCounters $Probe @('maximumEvents','dropped','errors')
    if($Probe.contract-cne'chunk7-native-restoration-entry-observer-v1'-or$Probe.maximumEvents-ne2048-or$Probe.dropped-ne0-or$Probe.errors-ne0-or
       $Probe.prepareMetric-cne'native-method-entries-including-production-refused-attempts'-or$Probe.hooks-isnot[Array]-or$Probe.hooks.Count-ne6-or
       $Probe.events-isnot[Array]-or$Probe.events.Count-ne5){throw 'Schema-1 observer is incomplete, partial or contains extra native callbacks.'}
    foreach($spec in @(@('06000C3C','TurnBased.Controllers.TurnController','Prepare'),@('0600939D','Kingmaker.Controllers.Combat.UnitCombatState','OnNewRound'))){
        $h=@($Probe.hooks|Where-Object{$_.token-ceq$spec[0]})
        if($h.Count-ne1-or$h[0].moduleMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'-or$h[0].declaringType-cne$spec[1]-or$h[0].method-cne$spec[2]){throw 'Schema-1 observer native hook identity differs.'}
    }
    foreach($method in @('RestoreActor','RestoreDebt','TryRestoreArchiveCombat','RestoreArchivePresentation')){
        $h=@($Probe.hooks|Where-Object method -CEQ $method)
        $declaring=if($method-cin@('RestoreActor','RestoreDebt')){'KingmakerMountedCombat.Integration.NativeCombatActorPersistence'}else{'KingmakerMountedCombat.Integration.MountedPersistenceService'}
        if($h.Count-ne1-or$h[0].declaringType-cne$declaring-or$h[0].moduleMvid-cne$Request.dllMvid-or$h[0].token-cnotmatch'^06[0-9A-F]{6}$'){throw 'Schema-1 observer product hook differs from candidate.'}
    }
    if(@($Probe.events|Where-Object{$_.kind-cnotin@('restore-debt-before','restore-debt-after','all-owner-presentation-restored')}).Count-ne0){throw 'Schema-1 restore/resave entered Prepare/round/full combat restore.'}
    foreach($actor in @($Archive.Pairs[0].Rider,$Archive.Pairs[0].Mount)){
        $before=@($Probe.events|Where-Object{$_.kind-ceq'restore-debt-before'-and$_.actorId-ceq$actor.Id});$after=@($Probe.events|Where-Object{$_.kind-ceq'restore-debt-after'-and$_.actorId-ceq$actor.Id})
        if($before.Count-ne1-or$after.Count-ne1-or[Array]::IndexOf($Probe.events,$before[0])-ge[Array]::IndexOf($Probe.events,$after[0])-or
           $after[0].restorePhase-cne'validated-before-native-mutation'-or
           (ConvertTo-KmcChunk7Canonical $after[0].debt)-cne(ConvertTo-KmcChunk7Canonical $actor)){throw 'Schema-1 exact native debt restore differs or occurs out of order.'}
        foreach($row in @($before[0],$after[0])){Assert-KmcChunk7Boolean $row.inCombat $false;if($row.prepared-isnot[bool]){throw 'Native prepared state missing.'}}
        if($before[0].prepared-ne$after[0].prepared){throw 'Schema-1 supplemental debt restore manufactured prepared state.'}
    }
    $presentation=@($Probe.events|Where-Object kind -CEQ 'all-owner-presentation-restored')
    if($presentation.Count-ne1-or[Array]::IndexOf($Probe.events,$presentation[0])-ne4-or$presentation[0].restorePhase-cne'all-pair-presentation-restored'){throw 'Schema-1 attachment preceded full actor restoration.'}
    Assert-KmcChunk7Schema1World $presentation[0].snapshot $Archive
}
function Assert-KmcChunk7Schema1Observation($Request,$Artifact) {
    Assert-KmcChunk7Schema1Request $Request
    $o=$Artifact.observations;$migration=$o.schema1Migration;$old=$migration.nativeInputMetadata;$imported=$migration.migratedArchive
    if($Artifact.status-cne'PASS'-or$o.checkpoint-cne$Request.persistenceCase-or$o.sourceWorkingSha256-cne$Request.fixture.working.sha256-or
       $o.sourceWorkingSha256-cne'5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6'){throw 'Schema-1 native case/fixture identity differs.'}
    Assert-KmcChunk7NativeCounters $o @('nativeSchema','nativeCapacity','processId')
    if($o.nativeSchema-ne3-or$o.nativeCapacity-ne2-or$o.processId-le0){throw 'Schema-1 deliberate native migration capability differs.'}
    foreach($field in @('cold','nativeProgressionRequiresExternalWorkingRestoration')){Assert-KmcChunk7Boolean $o.$field $true}
    foreach($field in @('combatDebtProof','ordinaryRespecProven')){Assert-KmcChunk7Boolean $o.$field $false}
    Assert-KmcExactProperties $old @('SchemaVersion','CampaignId','AreaId','GameTimeTicks','Policy','RulesId','Mounted','ProfileId','Rider','Mount','Slots') 'raw native schema-1 metadata'
    Assert-KmcChunk7NativeCounters $old @('SchemaVersion','GameTimeTicks');Assert-KmcChunk7Boolean $old.Mounted $true
    Assert-KmcChunk7NativeCounters $migration @('inputSchema','inputLength')
    if($old.SchemaVersion-ne1-or$migration.inputSchema-ne1-or$migration.inputSha256-cne$Request.persistenceLoad.sha256-or
       $migration.inputLength-ne$Request.persistenceLoad.length-or$migration.derivationReceiptSha256-cne$Request.schema1DerivationSha256-or
       (ConvertTo-KmcChunk7Canonical $o.loadedDescriptor)-cne(ConvertTo-KmcChunk7Canonical $Request.persistenceLoad)-or
       (ConvertTo-KmcChunk7Canonical $o.loadedArchive)-cne(ConvertTo-KmcChunk7Canonical $imported)){throw 'Schema-1 input provenance or migrated graph differs.'}
    Assert-KmcChunk7Schema1Archive $imported $Request
    foreach($field in @('CampaignId','AreaId','GameTimeTicks','Policy','RulesId','Slots')){if((ConvertTo-KmcChunk7Canonical $old.$field)-cne(ConvertTo-KmcChunk7Canonical $imported.$field)){throw 'Schema-1 migration lost native metadata.'}}
    $pair=$imported.Pairs[0]
    foreach($field in @('ProfileId','Rider','Mount')){if((ConvertTo-KmcChunk7Canonical $old.$field)-cne(ConvertTo-KmcChunk7Canonical $pair.$field)){throw 'Schema-1 migration changed an actor or profile.'}}
    if($pair.PairId-cne(Get-KmcChunk7Schema1Owner $old)){throw 'Schema-1 stable synthetic owner identity differs.'}
    Assert-KmcChunk7Schema1World $migration.restoredWorld $imported
    Assert-KmcChunk7Schema1Probe $o.coldRestoration $imported $Request
    if($o.inputs-isnot[Array]-or$o.inputs.Count-ne0-or$o.movements-isnot[Array]-or$o.movements.Count-ne0){throw 'Schema-1 restoration replayed a native command.'}
    $write=$o.nativeWrite;$resaved=$write.archive
    Assert-KmcChunk7Schema1Archive $resaved $Request
    foreach($field in @('CampaignId','AreaId','Policy','RulesId','Slots')){if((ConvertTo-KmcChunk7Canonical $imported.$field)-cne(ConvertTo-KmcChunk7Canonical $resaved.$field)){throw 'Schema-1 resave changed persistent identity/policy/slots.'}}
    foreach($field in @('PairId','Generation','ProfileId')){if($resaved.Pairs[0].$field-cne$pair.$field){throw 'Schema-1 resave replaced its owner.'}}
    foreach($role in @('Rider','Mount')){Assert-KmcChunk7Schema1Debt $resaved.Pairs[0].$role $pair.$role $resaved.GameTimeTicks $imported.GameTimeTicks}
    Assert-KmcChunk7Schema1World $migration.resavedWorld $resaved
    Assert-KmcChunk7NativeCounters $write @('snapshotCount','sourceSchema','length')
    Assert-KmcChunk7NativeCounters $o.saveRequested @('snapshotCount','gameTicks')
    foreach($field in @('nativeSaveAllowed','newSlot')){Assert-KmcChunk7Boolean $o.saveRequested.$field $true}
    Assert-KmcChunk7Boolean $write.nativeCallback $true
    if($write.nativeType-cne'Manual'-or$write.nativeName-cne'KMC_C7_SCHEMA1'-or$write.operation-cne'None'-or$write.sourceSchema-ne3-or$write.snapshotCount-ne1-or
       $write.sha256-cnotmatch'^[0-9a-f]{64}$'-or$write.length-le0-or$write.length-gt256MB-or$o.saveRequested.nativeType-cne'Manual'-or
       $o.saveRequested.nativeName-cne'KMC_C7_SCHEMA1'-or$o.saveRequested.snapshotCount-ne0){throw 'Schema-1 resave was not one completed native schema-3 Manual write.'}
    foreach($spec in @(@('schema1-native-restored-before-resave',0),@('schema1-native-resave-complete',1))){
        $rows=@($o.snapshots|Where-Object label -CEQ $spec[0]);if($rows.Count-ne1){throw 'Schema-1 native checkpoint missing.'};$s=$rows[0]
        Assert-KmcChunk7NativeCounters $s @('semanticRestores','presentationRestores','nativePostLoads','snapshots','nativeCastRequests','gameTicks')
        Assert-KmcChunk7NativeCounters $s.controls @('DuplicateFactCount','ManagedHotbarSlotCount');Assert-KmcChunk7Boolean $s.controls.SerializationSuspended $false
        if($s.semanticRestores-ne2-or$s.presentationRestores-ne1-or$s.nativePostLoads-ne2-or$s.snapshots-ne$spec[1]-or$s.nativeCastRequests-ne0-or
           $s.archivePhase-cne'all-pair-presentation-restored'-or$s.controls.DuplicateFactCount-ne0-or$s.controls.ManagedHotbarSlotCount-ne2-or$s.actors-isnot[Array]-or$s.actors.Count-ne2){throw 'Schema-1 native restore/capture counters or actor controls differ.'}
        Assert-KmcChunk7Schema1Owners $s.owners $imported
        foreach($a in @($pair.Rider,$pair.Mount)){$row=@($s.actors|Where-Object{$_.native.Id-ceq$a.Id});if($row.Count-ne1){throw 'Schema-1 checkpoint lost exact actor.'};Assert-KmcChunk7Boolean $row[0].inCombat $false;Assert-KmcChunk7Schema1Debt $row[0].native $a $s.gameTicks $imported.GameTimeTicks}
    }
    foreach($field in @('pairsClear','sourceHorseRemoved','controlsSettled','saveCustodyClear','selectionRestored','nativeProgressionRequiresExternalWorkingRestoration')){Assert-KmcChunk7Boolean $o.cleanup.$field $true}
    Assert-KmcChunk7Boolean $o.cleanup.ordinaryRespecProven $false
    if($o.cleanup.world.owners-isnot[Array]-or$o.cleanup.world.owners.Count-ne0){throw 'Schema-1 cleanup retained owner custody.'}
}
