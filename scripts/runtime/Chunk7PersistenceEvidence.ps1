# Distinct schema-3 native evidence family. Historical one-pair readers are unchanged.
Set-StrictMode -Version Latest
# Historical receipts used capacity two; the selected all-pair candidate uses
# six. This is a capability field, not the number of pairs exercised by a case.
# Existing case-specific graph, budget and input assertions remain unchanged.
function Test-KmcChunk7NativePairCapacity($Value){
    (Test-KmcExactJsonInteger $Value)-and($Value-eq2-or$Value-eq6)
}
# JSON value comparison for Chunk 7 evidence only. PowerShell may report wrapped
# JSON strings/arrays as PSCustomObject, so inspect scalar/array values first.
function ConvertTo-KmcChunk7Canonical($Value,[int]$Depth=0) {
    if($Depth-gt64){throw 'Chunk 7 evidence comparison exceeded its JSON depth bound.'}
    if($null-eq$Value){return 'null'}
    $type=$Value.GetType()
    if($Value-is[string]-or$Value-is[bool]){return (ConvertTo-Json -InputObject $Value -Compress)}
    if($type.IsPrimitive-or$Value-is[decimal]) {
        if($Value-is[char]-or$Value-is[IntPtr]-or$Value-is[UIntPtr]){throw 'Chunk 7 evidence contains a non-JSON scalar.'}
        if(($Value-is[double]-or$Value-is[single])-and([double]::IsNaN([double]$Value)-or[double]::IsInfinity([double]$Value))){throw 'Chunk 7 evidence contains a nonfinite number.'}
        return (ConvertTo-Json -InputObject $Value -Compress)
    }
    if($Value-is[Array]) {
        if($Value.Rank-ne1){throw 'Chunk 7 evidence contains a non-JSON array.'}
        $items=@(foreach($item in $Value){ConvertTo-KmcChunk7Canonical $item ($Depth+1)})
        return ('['+($items-join',')+']')
    }
    if($Value-is[Collections.IDictionary]) {
        foreach($key in $Value.Keys){if($key-isnot[string]){throw 'Chunk 7 evidence object has a non-string key.'}}
        [string[]]$keys=@($Value.Keys);[Array]::Sort($keys,[StringComparer]::Ordinal)
        $members=@(foreach($key in $keys){(ConvertTo-Json -InputObject $key -Compress)+':'+(ConvertTo-KmcChunk7Canonical $Value[$key] ($Depth+1))})
        return ('{'+($members-join',')+'}')
    }
    if($type.FullName-ceq'System.Management.Automation.PSCustomObject') {
        [string[]]$keys=@($Value.PSObject.Properties|ForEach-Object{$_.Name});[Array]::Sort($keys,[StringComparer]::Ordinal)
        $members=@(foreach($key in $keys){(ConvertTo-Json -InputObject $key -Compress)+':'+(ConvertTo-KmcChunk7Canonical $Value.PSObject.Properties[$key].Value ($Depth+1))})
        return ('{'+($members-join',')+'}')
    }
    throw ('Chunk 7 evidence contains an unsupported JSON value: '+$type.FullName)
}
function Assert-KmcChunk7NativeCounters($Value,[string[]]$Fields) {
    foreach($field in $Fields) {
        if(-not(Test-KmcExactJsonInteger $Value.$field)-or$Value.$field-lt0){throw ('Native identity/counter must be a nonnegative JSON integer: '+$field)}
    }
}
. (Join-Path $PSScriptRoot 'Chunk7CombatPersistenceEvidence.ps1')
. (Join-Path $PSScriptRoot 'Chunk7AreaPersistenceEvidence.ps1')
. (Join-Path $PSScriptRoot 'Chunk7OwnerSeedAreaEvidence.ps1')
. (Join-Path $PSScriptRoot 'Chunk7ManticoreFixtureEvidence.ps1')
. (Join-Path $PSScriptRoot 'Chunk7Schema1PersistenceEvidence.ps1')
. (Join-Path $PSScriptRoot 'Chunk7OwnedSchema1Evidence.ps1')
. (Join-Path $PSScriptRoot 'Chunk7OwnedSchema2Evidence.ps1')
. (Join-Path $PSScriptRoot 'Chunk7CurrentMigrationEvidence.ps1')
. (Join-Path $PSScriptRoot 'Chunk7RepeatedWorldEvidence.ps1')
. (Join-Path $PSScriptRoot 'Chunk7CapitalArrivalEvidence.ps1')
. (Join-Path $PSScriptRoot 'Chunk7SaveBoundaryEvidence.ps1')
function Get-KmcChunk7PersistenceCases { @('multi-exploration-manual','multi-exploration-quick','multi-exploration-auto','multi-rt-debt-manual','multi-tb-debt-manual','zero-mounted-tb-retained-manual','legacy-mounted-pending-tb-manual','pending-schema3-control-manual','multi-area-autosave','legacy-schema1-resave-manual','multi-exploration-three-loads-manual','multi-exploration-three-area-transfers-manual','multi-exploration-capital-arrival-manual','multi-exploration-capital-arrival-cold-manual','multi-nonselected-mount-unsettled-manual','multi-owned-worker-cancel-manual','owned-schema1-manual','owned-schema1-resave-manual','owned-schema1-current-manual','owned-schema2-pending-current-manual','multi-malformed-refusal-manual','multi-future-refusal-manual','owner-seed-manticore-fixture-manual','owner-seed-tomb-door-fixture-manual','owner-seed-thorn-river-door-fixture-manual','manticore-fixture-cold-manual') }
function Test-KmcChunk7PersistenceScenario([string]$Scenario) { $Scenario -cin @('chunk7-persistence-save','chunk7-persistence-load') }
function Get-KmcChunk7PersistenceRows {
    @('chunk7-persistence-save','chunk7-persistence-load','C7-persistence-two-native-mounts','C7-persistence-native-category-all-pair-archive',
      'C7-persistence-cold-all-pair-identity-and-no-prepare','C7-persistence-nonselected-mount-save-deferral','C7-persistence-owned-worker-cancel-drain','C7-persistence-three-native-loads','C7-persistence-three-native-area-transfers','C7-persistence-capital-arrival-retirement','C7-persistence-capital-arrival-native-manual','C7-persistence-capital-arrival-cold','C7-persistence-owned-schema1-source','C7-persistence-owner-seed-restored-start','C7-persistence-schema1-native-restoration','C7-persistence-schema1-native-resave','C7-persistence-schema2-native-resave','C7-persistence-native-continuation','C7-persistence-all-pair-area-before-autosave','C7-persistence-one-native-mount-legacy-source','C7-persistence-native-combat-continuation','C7-persistence-cleanup','C7-persistence-runtime','C7-persistence-interrupted','C7-persistence-owner-seed-derived-cold-identity','C7-persistence-current-migration-native-load','C7-persistence-current-migration-continuation','C7-persistence-real-entry-load-refusal','C7-persistence-manticore-fixture-native-manual','C7-persistence-tomb-door-fixture-native-manual','C7-persistence-thorn-river-door-fixture-native-manual','C7-persistence-manticore-manual-cold-identity',
      'C7-persistence-six-native-mounts','C7-persistence-six-mounted-archive','C7-persistence-six-native-continuation')
}
function Get-KmcChunk7PersistenceType([string]$Case) {
    if($Case-cnotin(Get-KmcChunk7PersistenceCases)){throw 'Undeclared Chunk 7 native persistence case.'}
    if($Case.EndsWith('-quick',[StringComparison]::Ordinal)){return 'Quick'}
    if($Case-ceq'multi-area-autosave'-or$Case.EndsWith('-auto',[StringComparison]::Ordinal)){return 'Auto'}
    return 'Manual'
}
function Get-KmcChunk7PersistenceLeaf([string]$Case) {
    $type=Get-KmcChunk7PersistenceType $Case
    if($Case-ceq'legacy-schema1-resave-manual'){return 'Manual_813_KMC_C7_SCHEMA1.zks'}
    if($type-ceq'Manual'){return 'Manual_300_KMC_C7_WORLD.zks'}
    return ($type+'_1.zks')
}
function Get-KmcChunk7ExpectedArchiveArea($Request) {
    if($Request.persistenceCase-ceq'owner-seed-tomb-door-fixture-manual'){Assert-KmcChunk7TombSelection $Request;return '81b13ce8765469045864ffb878b0944b'}
    if($Request.persistenceCase-ceq'owner-seed-thorn-river-door-fixture-manual'){Assert-KmcChunk7ThornRiverSelection $Request;return '7d72372ade2f14448a3fec6d61e06bd8'}
    if($Request.persistenceCase-cin@('owner-seed-manticore-fixture-manual','manticore-fixture-cold-manual')){if($Request.persistenceCase-ceq'owner-seed-manticore-fixture-manual'){Assert-KmcChunk7ManticoreSelection $Request}else{Assert-KmcChunk7OwnedPairsDescriptor $Request};return '314e8c3b15b896a4f847c85f9250f02b'}
    if($Request.persistenceCase-ceq'multi-area-autosave'){Assert-KmcChunk7AreaTarget $Request;return [string]$Request.persistenceAreaTarget.area}
    if(Test-KmcChunk7CapitalCase $Request.persistenceCase){return Get-KmcChunk7CapitalArrivalArea}
    return [string](Get-KmcChunk7SelectedFixture $Request).area
}
function Assert-KmcChunk7AreaTarget($Request) {
    if($Request.persistenceCase-cne'multi-area-autosave'){return}
    $t=$Request.persistenceAreaTarget
    $sourceArea=if(Test-KmcChunk7OwnedPairsScenario $Request.scenario){$Request.ownedPairs.sourceArea}else{$Request.fixture.working.area}
    Assert-KmcExactProperties $t @('enterPoint','area','autoSaveMode') 'Chunk 7 area target'
    if($t.enterPoint-isnot[string]-or$t.area-isnot[string]-or$t.enterPoint-cnotmatch'^[0-9a-f]{32}$'-or$t.area-cnotmatch'^[0-9a-f]{32}$'-or
       $t.enterPoint-ceq$t.area-or$t.enterPoint-ceq('0'*32)-or$t.area-ceq('0'*32)-or$t.area-ceq$sourceArea-or$t.autoSaveMode-isnot[string]-or$t.autoSaveMode-cne'AfterEntry'){throw 'Chunk 7 area autosave requires an exact distinct AfterEntry destination.'}
}
function Assert-KmcChunk7PersistenceNativeActor($Actor) {
    Assert-KmcExactProperties $Actor @('Id','Standard','Move','Swift','Initiative','Reaction','ReactionsRemaining','LastSurpriseTicks') 'Chunk 7 native actor debt'
    $id=[Guid]::Empty
    if($Actor.Id-isnot[string]-or-not[Guid]::TryParse($Actor.Id,[ref]$id)-or$id-eq[Guid]::Empty){throw 'Native actor ID invalid.'}
    foreach($field in @('Standard','Move','Swift','Initiative','Reaction')) {
        if(-not(Test-KmcChunk7FiniteNumber $Actor.$field)-or$Actor.$field-lt0-or$Actor.$field-gt86400){throw 'Native debt is missing, nonnumeric, nonfinite or out of bounds.'}
    }
    if(-not(Test-KmcExactJsonInteger $Actor.ReactionsRemaining)-or$Actor.ReactionsRemaining-lt0-or$Actor.ReactionsRemaining-gt1024-or
       -not(Test-KmcExactJsonInteger $Actor.LastSurpriseTicks)-or$Actor.LastSurpriseTicks-lt0){throw 'Native reaction or surprise debt differs.'}
}
function Assert-KmcChunk7PersistenceArchive($Archive,$Request) {
    $partialSlots=$Request.persistenceCase-ceq'multi-nonselected-mount-unsettled-manual'
    $slotCount=if($partialSlots){2}else{4}
    Assert-KmcExactProperties $Archive @('SchemaVersion','CampaignId','AreaId','GameTimeTicks','Policy','RulesId','Pairs','Participations','Slots','Combat') 'Chunk 7 native archive'
    if(-not(Test-KmcExactJsonInteger $Archive.SchemaVersion)-or$Archive.SchemaVersion-ne3-or$Archive.CampaignId-cne(Get-KmcChunk7SelectedFixture $Request).gameId-or
       $Archive.AreaId-cne(Get-KmcChunk7ExpectedArchiveArea $Request)-or-not(Test-KmcExactJsonInteger $Archive.GameTimeTicks)-or$Archive.GameTimeTicks-lt0-or
       $Archive.Policy-cne'rider-principal-distinct-native-v1'-or$Archive.RulesId-cne'crpg-transport-v1'-or$Archive.Pairs-isnot[Array]-or$Archive.Pairs.Count-ne2-or
       $Archive.Participations-isnot[Array]-or$Archive.Participations.Count-ne0-or$null-ne$Archive.Combat-or$Archive.Slots-isnot[Array]-or$Archive.Slots.Count-ne$slotCount){
        throw 'Exploration native archive omitted owners, invented combat/retained state or changed version/policy/slots.'
    }
    $ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $owners=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach($pair in $Archive.Pairs) {
        Assert-KmcExactProperties $pair @('PairId','Generation','ProfileId','Rider','Mount') 'Chunk 7 native pair'
        $owner=[Guid]::Empty
        if(-not[Guid]::TryParse([string]$pair.PairId,[ref]$owner)-or$owner-eq[Guid]::Empty-or-not$owners.Add($owner.ToString('N'))-or
           -not(Test-KmcExactJsonInteger $pair.Generation)-or$pair.Generation-le0){throw 'Stable native owner identity or generation invalid.'}
        foreach($actor in @($pair.Rider,$pair.Mount)) {
            Assert-KmcChunk7PersistenceNativeActor $actor
            if(-not$ids.Add(([Guid]$actor.Id).ToString('N'))){throw 'Actor has duplicate or cross-role mounted custody.'}
        }
    }
    if(Test-KmcChunk7OwnedPairsScenario $Request.scenario){
        Assert-KmcChunk7OwnedPairsDescriptor $Request;$roles=Get-KmcChunk7OwnedPairIds $Request
        $first=@($Archive.Pairs|Where-Object{$_.Rider.Id-ceq$roles[0]});$second=@($Archive.Pairs|Where-Object{$_.Rider.Id-ceq$roles[2]})
        if($first.Count-ne1-or$second.Count-ne1-or$first[0].Mount.Id-cne$roles[1]-or$second[0].Mount.Id-cne$roles[3]){throw 'Native archive substituted inherited rider/mount roles.'}
        foreach($pair in @($first[0],$second[0])){if($pair.ProfileId-isnot[string]-or$pair.ProfileId-cnotmatch'^[a-z0-9][a-z0-9.-]{0,127}$'){throw 'Inherited archive profile token invalid.'}}
    }else{
    $first=@($Archive.Pairs|Where-Object ProfileId -CEQ 'medium-humanoid-mammoth-v1')
    $second=@($Archive.Pairs|Where-Object ProfileId -CEQ 'medium-humanoid-horse-v1')
    if($first.Count-ne1-or$second.Count-ne1-or$first[0].Rider.Id-cne'b6628a77-4962-47a4-a17c-88d9836fc9d5'-or
       $first[0].Mount.Id-cne'd79a4f6c-b74e-4868-95bd-533899131acb'-or$second[0].Rider.Id-cne'd17c8fd0-6627-40c4-bf9a-024cf001c1f7'){
        throw 'Native archive actor/profile roles differ from the exact qualified fixture.'
    }
    }
    $slotIds=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach($slot in $Archive.Slots) {
        Assert-KmcExactProperties $slot @('ActorId','Index','Kind') 'Chunk 7 native slot'
        if($slot.ActorId-cnotin@($first[0].Rider.Id,$second[0].Rider.Id)-or-not(Test-KmcExactJsonInteger $slot.Index)-or$slot.Index-lt0-or$slot.Index-gt127-or
           -not(Test-KmcExactJsonInteger $slot.Kind)-or$slot.Kind-cnotin@(2,3)-or-not$slotIds.Add($slot.ActorId+':'+$slot.Index)){throw 'Owned native control slot invalid or duplicated.'}
    }
    foreach($rider in @($first[0].Rider.Id,$second[0].Rider.Id)) {
        $kinds=@($Archive.Slots|Where-Object ActorId -CEQ $rider|ForEach-Object{$_.Kind}|Sort-Object)
        $expectedKinds=if($partialSlots-and$rider-ceq$second[0].Rider.Id){''}else{'2,3'}
        if(($kinds-join',')-cne$expectedKinds){throw 'Each pair must retain its exact declared native controls.'}
    }
}
function Assert-KmcChunk7PersistenceSnapshot($Snapshot,$Archive,[string]$Label) {
    if($Snapshot.label-cne$Label-or$Snapshot.owners-isnot[Array]-or$Snapshot.owners.Count-ne2-or$Snapshot.actors-isnot[Array]-or$Snapshot.actors.Count-ne4){throw 'All-pair native snapshot incomplete.'}
    Assert-KmcChunk7NativeCounters $Snapshot @('semanticRestores','presentationRestores','nativePostLoads','snapshots','nativeCastRequests')
    Assert-KmcChunk7NativeCounters $Snapshot.controls @('DuplicateFactCount','ManagedHotbarSlotCount')
    Assert-KmcChunk7Boolean $Snapshot.controls.SerializationSuspended $false
    foreach($owner in $Snapshot.owners){Assert-KmcChunk7NativeCounters $owner @('generation')}
    $native=@($Archive.Pairs|ForEach-Object{$_.Rider;$_.Mount})
    foreach($pair in $Archive.Pairs) {
        $row=@($Snapshot.owners|Where-Object{([Guid]$_.ownerId)-eq([Guid]$pair.PairId)})
        if($row.Count-ne1-or$row[0].generation-ne$pair.Generation-or$row[0].riderId-cne$pair.Rider.Id-or$row[0].mountId-cne$pair.Mount.Id-or$row[0].state-cne'Active'){
            throw 'Stable owner, generation or native actor mapping changed across the archive boundary.'
        }
    }
    if(-not(Test-KmcExactJsonInteger $Snapshot.gameTicks)-or$Snapshot.gameTicks-lt$Archive.GameTimeTicks){throw 'Native restored clock precedes the saved snapshot.'}
    $elapsed=([double]$Snapshot.gameTicks-[double]$Archive.GameTimeTicks)/10000000.0
    if($elapsed-gt30){throw 'Native debt comparison escaped its bounded observation window.'}
    foreach($a in $native) {
        $row=@($Snapshot.actors|Where-Object{$_.native.Id-ceq$a.Id})
        if($row.Count-ne1){throw 'Native actor debt snapshot omitted or duplicated an actor.'}
        $b=$row[0].native;Assert-KmcChunk7PersistenceNativeActor $b
        Assert-KmcChunk7Boolean $row[0].inCombat $false
        foreach($field in @('Standard','Move','Swift','Initiative','Reaction')) {
            if([double]$b.$field-gt[double]$a.$field+0.001-or[double]$b.$field+$elapsed+0.001+($elapsed*0.1)-lt[double]$a.$field){throw 'Native cooldown changed beyond elapsed native clock progress.'}
        }
        if($a.ReactionsRemaining-ne$b.ReactionsRemaining-or$a.LastSurpriseTicks-ne$b.LastSurpriseTicks){throw 'Native reaction/surprise history changed across restoration.'}
    }
    if($Snapshot.controls.DuplicateFactCount-ne0-or$Snapshot.controls.SerializationSuspended-ne$false-or$Snapshot.controls.ManagedHotbarSlotCount-ne$Archive.Slots.Count){throw 'Native control restoration differs.'}
}
function Assert-KmcChunk7PersistenceObservation($Request,$Artifact,[string]$Status) {
    Assert-KmcChunk7ArtifactEnvelope $Request $Artifact 'chunk7-persistence-evidence'
    Assert-KmcChunk7NativeCounters $Artifact @('schemaVersion')
    if($Status-cne'PASS'){return}
    if($null-ne$Artifact.observations.PSObject.Properties['manticoreFixturePreparation']-and-not(Test-KmcChunk7ManticoreFixture $Request)){throw 'Another persistence case supplied Manticore fixture preparation.'}
    if($null-ne$Artifact.observations.PSObject.Properties['tombDoorFixturePreparation']-and-not(Test-KmcChunk7TombFixture $Request)){throw 'Another persistence case supplied Tomb fixture preparation.'}
    if($null-ne$Artifact.observations.PSObject.Properties['thornRiverDoorFixturePreparation']-and-not(Test-KmcChunk7ThornRiverFixture $Request)){throw 'Another persistence case supplied Thorn River fixture preparation.'}
    if($null-ne$Artifact.observations.PSObject.Properties['manticoreManualCold']-and-not(Test-KmcChunk7ManticoreManualCold $Request)){throw 'Another persistence case supplied Manticore Manual cold proof.'}
    $doorInventoryCase=$Request.persistenceCase-ceq'multi-area-autosave'-or(Test-KmcChunk7ManticoreManualCold $Request)
    if($null-ne$Artifact.observations.PSObject.Properties['areaDoorInventory']-and-not$doorInventoryCase){throw 'Native door inventory belongs only to an admitted area continuation.'}
    if(Test-KmcChunk7OwnerSeedCurrentCheckpoint $Request.persistenceCase){Assert-KmcChunk7CurrentMigrationObservation $Request $Artifact;return}
    if(Test-KmcOwnedSchema1Case $Request.persistenceCase){Assert-KmcOwnedSchema1Observation $Request $Artifact;return}
    if(Test-KmcChunk7RepeatedWorldCase $Request.persistenceCase){Assert-KmcChunk7RepeatedWorldObservation $Request $Artifact;return}
    if(Test-KmcChunk7CapitalArrivalCase $Request.persistenceCase){Assert-KmcChunk7CapitalArrivalObservation $Request $Artifact;return}
    if(Test-KmcChunk7CapitalColdCase $Request.persistenceCase){Assert-KmcChunk7CapitalColdObservation $Request $Artifact;return}
    if($Request.persistenceCase-ceq'legacy-schema1-resave-manual'){Assert-KmcChunk7Schema1Observation $Request $Artifact;return}
    if(Test-KmcChunk7CombatPersistenceCase $Request.persistenceCase){Assert-KmcChunk7CombatPersistenceObservation $Request $Artifact;return}
    if((Test-KmcChunk7OwnedPairsScenario $Request.scenario)-and(Test-KmcChunk7SixPairPersistence $Request)){Assert-KmcChunk7SixPairPersistenceObservation $Request $Artifact;return}
    if($Artifact.status-cne'PASS'){throw 'Native persistence artifact failed.'}
    $o=$Artifact.observations;$cold=(Get-KmcChunk7SelectedScenario $Request)-ceq'chunk7-persistence-load'
    if(-not(Test-KmcChunk7LoadRefusal $Request)-and$null-ne$o.PSObject.Properties['loadRefusal']){throw 'Another persistence case supplied native refusal evidence.'}
    Assert-KmcChunk7NativeCounters $o @('nativeSchema','nativeCapacity','processId')
    $owned=Test-KmcChunk7OwnedPairsScenario $Request.scenario
    if($owned){
        Assert-KmcChunk7InheritedEvidence $Request $o
        if(-not(Test-KmcChunk7PersistenceScenario (Get-KmcChunk7SelectedScenario $Request))-or$o.checkpoint-isnot[string]-or$o.checkpoint-cne$Request.persistenceCase-or$null-ne$o.sourceWorkingSha256-or$o.nativeSchema-ne3-or-not(Test-KmcChunk7NativePairCapacity $o.nativeCapacity)-or-not(Test-KmcExactJsonInteger $o.processId)-or$o.processId-le0){throw 'Inherited native persistence case or capability differs.'}
        Assert-KmcBootstrapInventoryString $o.inheritedManualSha256 $Request.ownedPairs.sourceArchiveSha256
    }else{
    if(-not(Test-KmcChunk7PersistenceScenario (Get-KmcChunk7SelectedScenario $Request))-or$Request.persistenceCase-cnotin(Get-KmcChunk7PersistenceCases)-or
       $o.checkpoint-cne$Request.persistenceCase-or$o.sourceWorkingSha256-cne$Request.fixture.working.sha256-or
       $o.sourceWorkingSha256-cne'5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6'-or
       $o.nativeSchema-ne3-or-not(Test-KmcChunk7NativePairCapacity $o.nativeCapacity)-or-not(Test-KmcExactJsonInteger $o.processId)-or$o.processId-le0){throw 'Native persistence family/fixture/capability identity differs.'}
    }
    Assert-KmcChunk7Boolean $o.cold $cold
    Assert-KmcChunk7Boolean $o.nativeProgressionRequiresExternalWorkingRestoration (-not$owned)
    Assert-KmcChunk7Boolean $o.ordinaryRespecProven $false
    Assert-KmcChunk7Boolean $o.combatDebtProof $false
    $archive=if($cold){$o.loadedArchive}else{$o.nativeWrite.archive}
    Assert-KmcChunk7PersistenceArchive $archive $Request
    if($owned){Assert-KmcChunk7OwnedArchiveProfiles $Request $o $archive;if(-not$cold){Assert-KmcChunk7OwnedOutputEvidence $Request $o}}
    $seedCheckpointSave=$owned-and-not$cold-and(Test-KmcChunk7OwnedPairsSeedCheckpointSave $Request)
    if(-not$seedCheckpointSave-and($null-ne$o.PSObject.Properties['mountedStart']-or$null-ne$o.PSObject.Properties['ownerSeedCheckpointSource']-or$null-ne$o.PSObject.Properties['ownerSeedCheckpointPreparation'])){throw 'Only the owner-seed checkpoint producer may carry a restored mounted start or native preparation.'}
    # Owner-seed-derived cold load: the restored world compared with its pinned producer runtime.
    $derivedCold=$owned-and$cold-and((Test-KmcChunk7OwnedPairsSeedDerivedLoad $Request)-or(Test-KmcChunk7LoadRefusal $Request))
    if(Test-KmcChunk7ManticoreManualCold $Request){Assert-KmcChunk7ManticoreManualColdObservation $Request $o $archive
    }elseif($derivedCold){Assert-KmcChunk7OwnerSeedDerivedColdObservation $Request $o $archive}elseif($null-ne$o.PSObject.Properties['ownerSeedDerivedCold']){throw 'Only the owner-seed-derived consumer may carry its cold proof.'}
    $label=if($cold){'cold-restored-before-input'}else{'native-write-complete'}
    $snapshot=@($o.snapshots|Where-Object label -CEQ $label)
    if($snapshot.Count-ne1){throw 'Exact archive completion snapshot is missing or duplicated.'}
    Assert-KmcChunk7PersistenceSnapshot $snapshot[0] $archive $label
    if($cold) {
        if((ConvertTo-KmcChunk7Canonical $o.loadedDescriptor)-cne(ConvertTo-KmcChunk7Canonical $Request.persistenceLoad)-or
           $snapshot[0].semanticRestores-ne4-or$snapshot[0].presentationRestores-ne2-or$snapshot[0].nativePostLoads-ne4-or$snapshot[0].snapshots-ne0-or$snapshot[0].nativeCastRequests-ne0-or
           $snapshot[0].archivePhase-cne'all-pair-presentation-restored'-or@($o.inputs).Count-ne0){throw 'Cold load substituted its source or replayed a Mount.'}
        $probe=$o.coldRestoration
        Assert-KmcChunk7NativeCounters $probe @('maximumEvents','dropped','errors')
        Assert-KmcChunk7Boolean $probe.complete $true;Assert-KmcChunk7Boolean $probe.installedBeforeWorldLoad $true
        if($probe.contract-cne'chunk7-native-restoration-entry-observer-v1'-or$probe.maximumEvents-ne2048-or$probe.dropped-ne0-or$probe.errors-ne0-or
           $probe.prepareMetric-cne'native-method-entries-including-production-refused-attempts'-or@($probe.hooks).Count-ne4){throw 'Cold native preparation observer incomplete or wrong.'}
        $ids=@($archive.Pairs|ForEach-Object{$_.Rider.Id;$_.Mount.Id})
        if(@($probe.events|Where-Object{$_.actorId-cin$ids-and$_.kind-cin@('prepare-before','round-before')}).Count-ne0){throw 'Cold load entered native preparation or a new round.'}
        foreach($id in $ids) {foreach($kind in @('restore-debt-before','restore-debt-after')) {
            if(@($probe.events|Where-Object{$_.actorId-ceq$id-and$_.kind-ceq$kind}).Count-ne1){throw 'Native debt restore was not observed once for each actor.'}
        }}
        $prepare=@($probe.hooks|Where-Object{$_.declaringType-ceq'TurnBased.Controllers.TurnController'-and$_.method-ceq'Prepare'-and$_.token-ceq'06000C3C'})
        if($prepare.Count-ne1-or$prepare[0].moduleMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'){throw 'Preparation hook differs from installed native contract.'}
        $round=@($probe.hooks|Where-Object{$_.token-ceq'0600939D'-and$_.moduleMvid-ceq'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'})
        if($round.Count-ne1){throw 'Native round observer token differs.'}
        foreach($method in @('RestoreActor','RestoreDebt')) {
            $hook=@($probe.hooks|Where-Object{$_.declaringType-ceq'KingmakerMountedCombat.Integration.NativeCombatActorPersistence'-and$_.method-ceq$method-and$_.moduleMvid-ceq$Request.dllMvid})
            if($hook.Count-ne1){throw 'Supplemental restore observer is not bound to the exact candidate method.'}
        }
        foreach($savedActor in @($archive.Pairs|ForEach-Object{$_.Rider;$_.Mount})) {
            $after=@($probe.events|Where-Object{$_.actorId-ceq$savedActor.Id-and$_.kind-ceq'restore-debt-after'})[0]
            if($after.restorePhase-cne'validated-before-native-mutation'-or(ConvertTo-KmcChunk7Canonical $after.debt)-cne(ConvertTo-KmcChunk7Canonical $savedActor)){
                throw 'Exact post-restore native debt differs from the whole-graph validated source.'
            }
        }
    } else {
        $type=Get-KmcChunk7PersistenceType $Request.persistenceCase
        Assert-KmcChunk7NativeCounters $o.nativeWrite @('snapshotCount')
        if($o.nativeWrite.nativeType-cne$type-or$o.nativeWrite.operation-cne'None'-or$o.nativeWrite.snapshotCount-ne$(if($Request.persistenceCase-ceq'multi-owned-worker-cancel-manual'){2}else{1})-or
           $o.nativeWrite.sha256-cnotmatch'^[0-9a-f]{64}$'-or-not(Test-KmcExactJsonInteger $o.nativeWrite.length)-or
           $o.nativeWrite.length-le0-or$o.nativeWrite.length-gt256MB){throw 'Native save category/completion identity differs.'}
        if($Request.persistenceCase-ceq'multi-area-autosave'){
            if(@($o.PSObject.Properties.Name)-ccontains'saveRequested'){throw 'Native AfterEntry autosave was replaced with an explicit save request.'}
            Assert-KmcChunk7Boolean $o.nativeWrite.nativeCallback $false
            Assert-KmcChunk7AreaAutosave $Request $o $archive
        }else{
            Assert-KmcChunk7NativeCounters $o.saveRequested @('snapshotCount')
            if($o.saveRequested.nativeType-cne$type-or$o.saveRequested.snapshotCount-ne0){throw 'Explicit native save request differs.'}
            Assert-KmcChunk7Boolean $o.nativeWrite.nativeCallback ($Request.persistenceCase-cne'multi-owned-worker-cancel-manual')
            Assert-KmcChunk7Boolean $o.saveRequested.nativeSaveAllowed $true
            Assert-KmcChunk7Boolean $o.saveRequested.newSlot $true
        }
        $inputs=@($o.inputs)
        # Owner-seed checkpoint producer: the verified restored mounted start replaces both native Mount inputs.
        if($seedCheckpointSave){Assert-KmcChunk7OwnerSeedCheckpointStart $Request $o $archive}else{
        if($inputs.Count-ne2){throw 'Two actual native Mount inputs are required.'}
        $pairs=@($archive.Pairs|Sort-Object ProfileId -Descending)
        if($owned){$roles=Get-KmcChunk7OwnedPairIds $Request;$expected=@(@('mount-mammoth',$roles[0],$roles[1]),@('mount-horse',$roles[2],$roles[3]))}else{
        $expected=@(@('mount-mammoth','b6628a77-4962-47a4-a17c-88d9836fc9d5','d79a4f6c-b74e-4868-95bd-533899131acb'),
          @('mount-horse','d17c8fd0-6627-40c4-bf9a-024cf001c1f7',@($archive.Pairs|Where-Object ProfileId -CEQ 'medium-humanoid-horse-v1')[0].Mount.Id))
        }
        for($i=0;$i-lt2;$i++) {
            $row=$inputs[$i];$spec=$expected[$i]
            Assert-KmcChunk7NativeCounters $row @('nativeCastDelta','nativeRefusalDelta','generationAtInit')
            if($row.label-cne$spec[0]-or$row.casterId-cne$spec[1]-or$row.targetId-cne$spec[2]-or$row.abilityGuid-cne'f053faad986631688defa003cd7bda0e'-or
               $row.nativeCastDelta-ne1-or$row.nativeRefusalDelta-ne0-or-not(Test-KmcExactJsonInteger $row.generationAtInit)-or$row.generationAtInit-lt0-or
               [string]::IsNullOrEmpty($row.control)-or$row.selectionAfterInput-ceq$row.casterId){throw 'Native Mount input lost exact actor/control/generation/selection binding.'}
            Assert-KmcChunk7Boolean $row.clicked $true;Assert-KmcChunk7Boolean $row.terminal $true
        }
        }
        if(-not$owned){
        Assert-KmcChunk7NativeCounters $o.acquisition @('nativeRangerCommitCount','huntersBondSelectionLevel','rangerCompanionSelectionLevel','horseFactRankAtCommit')
        Assert-KmcChunk7Boolean $o.acquisition.horsePresentAtNativeCommit $true
        if($o.acquisition.nativeRangerCommitCount-ne4-or$o.acquisition.huntersBondSelectionLevel-ne4-or$o.acquisition.rangerCompanionSelectionLevel-ne4-or
           $o.acquisition.horseFactRankAtCommit-ne1-or$o.acquisition.horsePresentAtNativeCommit-ne$true){throw 'Native Horse fixture acquisition evidence differs.'}        }

    }
    if(-not$cold-and(Test-KmcChunk7SaveBoundaryCase $Request.persistenceCase)){Assert-KmcChunk7SaveBoundarySource $Request $o $archive}
    $moves=@($o.movements)
    if($moves.Count-ne2){throw 'Both restored/live pairs require native continuation.'}
    for($i=0;$i-lt2;$i++) {
        $move=$moves[$i];$pair=@($archive.Pairs|Where-Object{$_.Rider.Id-ceq$move.selectedId})
        if($pair.Count-ne1-or$move.executorId-cne$pair[0].Mount.Id-or$move.label-cne@('first-pair','second-pair')[$i]){throw 'Native continuation lost actor routing.'}
        Assert-KmcChunk7Boolean $move.terminal $true;Assert-KmcChunk7Boolean $move.otherCommandsUnchanged $true
        $distance=[Math]::Sqrt([Math]::Pow([double]$move.end.x-$move.start.x,2)+[Math]::Pow([double]$move.end.y-$move.start.y,2)+[Math]::Pow([double]$move.end.z-$move.start.z,2))
        $residual=[Math]::Sqrt([Math]::Pow([double]$move.end.x-$move.goal.x,2)+[Math]::Pow([double]$move.end.y-$move.goal.y,2)+[Math]::Pow([double]$move.end.z-$move.goal.z,2))
        if(-not(Test-KmcChunk7FiniteNumber $move.distance)-or-not(Test-KmcChunk7FiniteNumber $move.residual)-or
           [Math]::Abs($distance-[double]$move.distance)-gt0.0001-or[Math]::Abs($residual-[double]$move.residual)-gt0.0001-or$distance-lt0.75-or$residual-gt1){throw 'Native movement missed the unchanged measured endpoint.'}
    }
    if($cold-and$Request.persistenceCase-ceq'multi-area-autosave'){
        Assert-KmcChunk7AreaContinuationPlans $o
        if($null-ne$o.PSObject.Properties['areaDoorInventory']){Assert-KmcChunk7AreaDoorInventory $o}
    }
    if($cold-and(Test-KmcChunk7ManticoreManualCold $Request)-and$null-ne$o.PSObject.Properties['areaDoorInventory']){
        Assert-KmcChunk7AreaDoorInventory $o
    }
    if($owned){foreach($field in @('pairsClear','controlsSettled','saveCustodyClear','selectionRestored','inheritedPetsPreserved')){Assert-KmcChunk7Boolean $o.cleanup.$field $true};Assert-KmcChunk7Boolean $o.cleanup.sourceHorseRemoved $false;Assert-KmcChunk7Boolean $o.cleanup.nativeProgressionRequiresExternalWorkingRestoration $false}else{
    foreach($field in @('pairsClear','sourceHorseRemoved','controlsSettled','saveCustodyClear','selectionRestored','nativeProgressionRequiresExternalWorkingRestoration')) {Assert-KmcChunk7Boolean $o.cleanup.$field $true}    }
    Assert-KmcChunk7Boolean $o.cleanup.ordinaryRespecProven $false
    if(@($o.cleanup.world.owners).Count-ne0){throw 'Cleanup retained native custody.'}
    if(Test-KmcChunk7LoadRefusal $Request){Assert-KmcChunk7LoadRefusalObservation $Request $Artifact}
}
function Assert-KmcChunk7PersistenceArtifact($Request,$Manifest,[string]$Status='PASS') {
    $present=@($Manifest.artifacts|Where-Object{$_.relativePath-ceq'chunk7-persistence-evidence.json'-or$_.kind-ceq'chunk7-persistence-evidence'})
    if(-not(Test-KmcChunk7PersistenceScenario (Get-KmcChunk7SelectedScenario $Request))){if($present.Count-ne0){throw 'Another scenario supplied Chunk 7 persistence evidence.'};return}
    if($present.Count-eq0-and$Status-cne'PASS'){return}
    $artifact=Read-KmcChunk7ManifestArtifact $Request $Manifest 'chunk7-persistence-evidence.json' 'chunk7-persistence-evidence'
    Assert-KmcChunk7PersistenceObservation $Request $artifact $Status
    if($Status-ceq'PASS'-and((Get-KmcChunk7SelectedScenario $Request)-ceq'chunk7-persistence-save'-or((Get-KmcChunk7SelectedScenario $Request)-ceq'chunk7-persistence-load'-and$Request.persistenceCase-cin@('legacy-schema1-resave-manual','legacy-mounted-pending-tb-manual','owned-schema1-resave-manual','multi-exploration-capital-arrival-manual')))) {
        $w=$artifact.observations.nativeWrite
        $leaf=if(Test-KmcChunk7OwnedPairsScenario $Request.scenario){Get-KmcChunk7OwnedOutputLeaf $Request $artifact.observations}else{if((Get-KmcChunk7SelectedScenario $Request)-ceq'chunk7-persistence-load'-and$Request.persistenceCase-ceq'legacy-mounted-pending-tb-manual'){'Manual_301_KMC_C7_SCHEMA2.zks'}else{Get-KmcChunk7PersistenceLeaf $Request.persistenceCase}}
        $path=Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$Request.runId+'/Saved Games/'+$leaf)
        if($w.path-isnot[string]-or$w.path-cne$path){throw 'Native save escaped its declared isolated profile leaf.'}
        Assert-KmcNotReparsePoint $path 'Chunk 7 native archive';Assert-KmcNotHardLink $path 'Chunk 7 native archive'
        $metadata=if((Get-KmcChunk7SelectedScenario $Request)-ceq'chunk7-persistence-save'-and$Request.persistenceCase-ceq'legacy-mounted-pending-tb-manual'){$w.nativeMetadata}else{$w.archive}
        Assert-KmcChargeArchiveSnapshot $path $w.sha256 $w.length $metadata
    }
}

function Assert-KmcChunk7PersistenceLoadDescriptor($Request) {
    if(Test-KmcChunk7OwnedPairsScenario $Request.scenario){Assert-KmcChunk7OwnedPairsDescriptor $Request;return}
    $d=$Request.persistenceLoad
    Assert-KmcExactProperties $d @('internalName','fileName','sha256','length','lastWriteTimeUtcTicks','gameId','gameName','area') 'Chunk 7 cold native archive descriptor'
    $type=Get-KmcChunk7PersistenceType $Request.persistenceCase;$leaf=Get-KmcChunk7PersistenceLeaf $Request.persistenceCase
    $schema1=$Request.persistenceCase-ceq'legacy-schema1-resave-manual'
    if($schema1){Assert-KmcChunk7Schema1Request $Request;$leaf='Manual_812_KMC_P06.zks'}
    $nameOk=if($schema1){$d.internalName-ceq'KMC_P01'}elseif($type-ceq'Manual'){$d.internalName-ceq'KMC_C7_WORLD'}else{$d.internalName-is[string]-and$d.internalName.Length-gt0-and$d.internalName.Length-le256-and$d.internalName-cnotmatch'[\x00-\x1f\x7f]'}
    if(-not$nameOk-or$d.fileName-cne$leaf-or$d.sha256-cnotmatch'^[0-9a-f]{64}$'-or$d.sha256-ceq$Request.fixture.baseline.sha256-or
       -not(Test-KmcExactJsonInteger $d.length)-or$d.length-le0-or$d.length-gt256MB-or-not(Test-KmcExactJsonInteger $d.lastWriteTimeUtcTicks)-or
       $d.lastWriteTimeUtcTicks-le0-or$d.lastWriteTimeUtcTicks-gt[DateTime]::MaxValue.Ticks){throw 'Chunk 7 cold archive identity is invalid.'}
    foreach($field in @('gameId','gameName')){if($d.$field-cne$Request.fixture.working.$field){throw 'Chunk 7 cold native archive does not belong to its exact Working campaign.'}}
    if($d.area-cne(Get-KmcChunk7ExpectedArchiveArea $Request)){throw 'Chunk 7 cold native archive differs from its exact declared area.'}
}
