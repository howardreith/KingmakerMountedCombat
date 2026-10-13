Set-StrictMode -Version Latest
function Test-KmcChunk7CombatPersistenceCase([string]$Case) { $Case-cin@('multi-rt-debt-manual','multi-tb-debt-manual','zero-mounted-tb-retained-manual','legacy-mounted-pending-tb-manual') }
function Assert-KmcChunk7CombatArchive($Archive,$Request) {
    $legacy=$Request.persistenceCase-ceq'legacy-mounted-pending-tb-manual';$zero=$Request.persistenceCase-ceq'zero-mounted-tb-retained-manual';$tb=$Request.persistenceCase-cne'multi-rt-debt-manual'
    $pairCount=if($zero){0}elseif($legacy){1}else{2};$ownerCount=if($legacy){1}else{2};$slotCount=$pairCount*2
    Assert-KmcExactProperties $Archive @('SchemaVersion','CampaignId','AreaId','GameTimeTicks','Policy','RulesId','Pairs','Participations','Slots','Combat') 'Chunk 7 combat archive'
    if($Archive.SchemaVersion-ne3-or$Archive.CampaignId-cne$Request.fixture.working.gameId-or$Archive.AreaId-cne$Request.fixture.working.area-or
       -not(Test-KmcExactJsonInteger $Archive.GameTimeTicks)-or$Archive.GameTimeTicks-lt0-or$Archive.Policy-cne'rider-principal-distinct-native-v1'-or$Archive.RulesId-cne'crpg-transport-v1'-or
       $Archive.Pairs-isnot[Array]-or$Archive.Pairs.Count-ne$pairCount-or$Archive.Participations-isnot[Array]-or$Archive.Participations.Count-ne$ownerCount-or
       $Archive.Slots-isnot[Array]-or$Archive.Slots.Count-ne$slotCount-or$null-eq$Archive.Combat){throw 'Native combat archive has the wrong complete ownership shape.'}
    $combat=$Archive.Combat
    Assert-KmcExactProperties $combat @('TurnBased','Round','StartTicks','RoundStartTicks','TurnStartTicks','TimeSinceStart','TimeToNextRound','HasSurpriseRound','HasEnemy','HadEnemy','NextActor','BeforeCombatSelected','Actors','Roster','Engagements','Current','Paired','Allocations') 'Chunk 7 shared combat'
    Assert-KmcChunk7Boolean $combat.TurnBased $tb
    if($null-ne$combat.Paired-or$combat.Actors-isnot[Array]-or$combat.Actors.Count-lt5-or$combat.Actors.Count-gt64-or$combat.Roster-isnot[Array]-or$combat.Roster.Count-gt64-or
       $combat.Allocations-isnot[Array]-or$combat.Allocations.Count-gt64-or$combat.Engagements-isnot[Array]-or$combat.Engagements.Count-gt128){throw 'Shared combat duplicated singular participation or escaped its native bounds.'}
    $native=[Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal)
    foreach($actor in $combat.Actors) {
        Assert-KmcExactProperties $actor @('Native','InCombat','Prepared','HitThisRound','InitiativeRoll','CachedInitiative','InitiativeRandom','ExecutedAttacks','LastMoveTicks','LastDeflectTicks','StoryImmunity','EnergyDrainImmunity','PreventNextReaction','ReturnPoint','ReturnYaw','DisengageTargets','AiActions','AiDelayTicks') 'Chunk 7 shared actor'
        Assert-KmcChunk7PersistenceNativeActor $actor.Native
        $id=([Guid]$actor.Native.Id).ToString('N');if($native.ContainsKey($id)){throw 'Shared native actor duplicated.'};$native.Add($id,$actor)
        foreach($field in @('InCombat','Prepared','HitThisRound','EnergyDrainImmunity','PreventNextReaction')){if($actor.$field-isnot[bool]){throw 'Native actor boolean is mistyped.'}}
    }
    $first='b6628a77-4962-47a4-a17c-88d9836fc9d5';$mount='d79a4f6c-b74e-4868-95bd-533899131acb';$second='d17c8fd0-6627-40c4-bf9a-024cf001c1f7'
    foreach($id in @($first,$mount,$second)){if(-not$native.ContainsKey(([Guid]$id).ToString('N'))){throw 'Known native fixture actor missing from shared combat.'}}
    $active=[Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal);$members=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach($pair in $Archive.Pairs) {
        Assert-KmcExactProperties $pair @('PairId','Generation','ProfileId','Rider','Mount') 'Chunk 7 pair'
        $key=([Guid]$pair.PairId).ToString('N')
        if($key-ceq('0'*32)-or$active.ContainsKey($key)-or-not(Test-KmcExactJsonInteger $pair.Generation)-or$pair.Generation-le0){throw 'Stable owner identity or generation invalid.'}
        $active.Add($key,$pair)
        foreach($actor in @($pair.Rider,$pair.Mount)) {
            Assert-KmcChunk7PersistenceNativeActor $actor;$id=([Guid]$actor.Id).ToString('N')
            if(-not$members.Add($id)-or-not$native.ContainsKey($id)-or(ConvertTo-KmcChunk7Canonical $actor)-cne(ConvertTo-KmcChunk7Canonical $native[$id].Native)){throw 'Mounted actor has conflicting custody or shared debt.'}
        }
        if($pair.Rider.Id-ceq$first){if($pair.Mount.Id-cne$mount-or$pair.ProfileId-cne'medium-humanoid-mammoth-v1'){throw 'Mammoth native role/profile differs.'}}
        elseif($pair.Rider.Id-ceq$second){if($legacy-or$pair.ProfileId-cne'medium-humanoid-horse-v1'){throw 'Horse native role/profile differs.'}}
        else{throw 'Archive has a foreign mounted rider.'}
    }
    $ownerKeys=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);$retained=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);$current=0
    foreach($row in $Archive.Participations) {
        Assert-KmcExactProperties $row @('OwnerId','Generation','MountedPairId','State') 'Chunk 7 participation owner'
        $key=([Guid]$row.OwnerId).ToString('N')
        if($key-ceq('0'*32)-or-not$ownerKeys.Add($key)-or-not(Test-KmcExactJsonInteger $row.Generation)-or$row.Generation-le0){throw 'Retained stable owner is duplicate or invalid.'}
        $s=$row.State
        Assert-KmcExactProperties $s @('RiderId','MountId','Activation','BoundaryIsCurrent','Boundary','Partner','SplitReleaseRound','PendingSplitId','PendingSplitRound','RenewalNotBeforeTicks') 'Chunk 7 paired state'
        if($s.BoundaryIsCurrent-isnot[bool]){throw 'Boundary claim must be a JSON boolean.'};if($s.BoundaryIsCurrent){$current++}
        foreach($id in @($s.RiderId,$s.MountId,$s.PendingSplitId)|Where-Object{$null-ne$_}|Select-Object -Unique) {
            $actorKey=([Guid]$id).ToString('N');if(-not$native.ContainsKey($actorKey)-or-not$retained.Add($actorKey)){throw 'Participation drops an actor or admits a conflicting owner.'}
            if($members.Contains($actorKey)-and($null-eq$row.MountedPairId-or-not$active.ContainsKey($key))){throw 'Detached participation aliases active custody.'}
        }
        if($zero){if($null-ne$row.MountedPairId-or$null-eq$s.Activation-or$s.Activation.Split-ne$true-or$s.SplitReleaseRound-ne$combat.Round){throw 'Zero-mounted save lost original detached release obligations.'}}
        else {
            if($row.MountedPairId-cne$row.OwnerId-or-not$active.ContainsKey($key)-or$row.Generation-ne$active[$key].Generation){throw 'Participation owner does not belong to its exact mounted pair.'}
            if(-not$legacy-and($s.RiderId-cne$active[$key].Rider.Id-or$s.MountId-cne$active[$key].Mount.Id)){throw 'Participation actor role differs.'}
        }
        if($legacy) {
            if($s.RiderId-ne$null-or$s.MountId-ne$null-or$s.Activation-ne$null-or$s.Boundary-ne$null-or$s.Partner-ne$null-or$s.BoundaryIsCurrent-ne$false-or
               $s.PendingSplitId-cne$mount-or$s.PendingSplitRound-ne$combat.Round-or$native[([Guid]$first).ToString('N')].InCombat-ne$false-or
               $native[([Guid]$mount).ToString('N')].InCombat-ne$true-or$native[([Guid]$mount).ToString('N')].Native.Standard-le0.1){throw 'Mounted pending-only migration invented an activation, lost debt or changed its native release round.'}
        } elseif(-not$tb) {if($null-ne$s.Activation-or$s.BoundaryIsCurrent-or$null-ne$s.Boundary-or$null-ne$s.Partner){throw 'RT archive invented a turn-based context.'}}
        elseif($null-eq$s.Activation-or$s.Activation.Sequence-le0){throw 'Native TB archive lost its actor grant identity.'}
    }
    if($current-gt1){throw 'Two owners claim the single native turn.'}
    if($tb) {
        if($combat.Round-lt1-or$combat.Roster.Count-eq0){throw 'Native TB roster/round absent.'}
        if(-not$zero-and-not$legacy-and($combat.Current.ActorId-cne$second-or$current-ne1)){throw 'Two-pair TB checkpoint lost its saved second-rider turn.'}
    } elseif($combat.Roster.Count-ne0-or$null-ne$combat.Current-or$null-ne$combat.NextActor){throw 'RT save contains a synthetic roster or current turn.'}
    if(-not$legacy) {
        $firstDebt=$native[([Guid]$first).ToString('N')].Native;$secondDebt=$native[([Guid]$second).ToString('N')].Native
        $a=@($Archive.Participations|Where-Object{$_.State.RiderId-ceq$first});$b=@($Archive.Participations|Where-Object{$_.State.RiderId-ceq$second})
        if($a.Count-ne1-or$b.Count-ne1){throw 'Two-pair checkpoint lost either independent owner.'}
        $mountB=$native[([Guid]$b[0].State.MountId).ToString('N')].Native
        if($tb-and-not$zero-and($firstDebt.Standard-lt5.9-or$secondDebt.Standard-le0.1-or$mountB.Standard-ne0)){throw 'TB archive omitted the distinct completed/current native actor expenditures.'}
        if(-not$tb-and($firstDebt.Standard-le0.1-or$mountB.Standard-le0.1-or$secondDebt.Standard-ne0)){throw 'RT archive omitted the distinct rider/other-pair mount expenditures.'}
    }
    $allocations=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach($allocation in $combat.Allocations) {if(-not$native.ContainsKey(([Guid]$allocation.ActorId).ToString('N'))-or-not$allocations.Add($allocation.ActorId)){throw 'World movement allocation omitted or duplicated an actor.'}}
    $slots=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach($slot in $Archive.Slots){if($slot.ActorId-cnotin@($Archive.Pairs|ForEach-Object{$_.Rider.Id})-or$slot.Kind-cnotin@(2,3)-or$slot.Index-lt0-or$slot.Index-gt127-or-not$slots.Add($slot.ActorId+':'+$slot.Index)){throw 'Native owned slot differs.'}}
    foreach($pair in $Archive.Pairs){if((@($Archive.Slots|Where-Object ActorId -CEQ $pair.Rider.Id|ForEach-Object{$_.Kind}|Sort-Object)-join',')-cne'2,3'){throw 'Mounted owner lost its declared slots.'}}
}
function Assert-KmcChunk7LegacyPendingMigration($NativeMetadata,$Archive) {
    Assert-KmcExactProperties $NativeMetadata @('SchemaVersion','CampaignId','AreaId','GameTimeTicks','Policy','RulesId','Mounted','ProfileId','Rider','Mount','Slots','Combat') 'Native schema-2 source'
    if($NativeMetadata.SchemaVersion-ne2-or$NativeMetadata.Mounted-ne$true-or$Archive.Pairs.Count-ne1-or$Archive.Participations.Count-ne1){throw 'Legacy source is not the declared native mounted checkpoint.'}
    foreach($field in @('CampaignId','AreaId','GameTimeTicks','Policy','RulesId','Slots')){if((ConvertTo-KmcChunk7Canonical $NativeMetadata.$field)-cne(ConvertTo-KmcChunk7Canonical $Archive.$field)){throw 'Migration changed native identity, clock, rules or slots.'}}
    $pair=$Archive.Pairs[0];$row=$Archive.Participations[0]
    foreach($field in @('ProfileId','Rider','Mount')){if((ConvertTo-KmcChunk7Canonical $NativeMetadata.$field)-cne(ConvertTo-KmcChunk7Canonical $pair.$field)){throw 'Migration changed native pair or debt.'}}
    if((ConvertTo-KmcChunk7Canonical $NativeMetadata.Combat.Paired)-cne(ConvertTo-KmcChunk7Canonical $row.State)){throw 'Migration changed exact pending-only state or release round.'}
    $combat=$NativeMetadata.Combat|ConvertTo-Json -Depth 90 -Compress|ConvertFrom-Json;$combat.Paired=$null
    if((ConvertTo-KmcChunk7Canonical $combat)-cne(ConvertTo-KmcChunk7Canonical $Archive.Combat)){throw 'Migration changed the shared native world.'}
    $sha=[Security.Cryptography.SHA256]::Create()
    try{$bytes=$sha.ComputeHash([Text.Encoding]::UTF8.GetBytes('kmc-schema3|'+$Archive.CampaignId+'|'+$pair.Rider.Id+'|'+$pair.Mount.Id));$owner=[Guid]::new([byte[]]$bytes[0..15]).ToString('N')}finally{$sha.Dispose()}
    if($pair.PairId-cne$owner-or$row.OwnerId-cne$owner-or$row.MountedPairId-cne$owner-or$pair.Generation-ne1-or$row.Generation-ne1){throw 'Migration changed deterministic owner custody or admitted a competing owner.'}
}
function Assert-KmcChunk7NativeWorldRestore($Snapshot,$Archive,[bool]$Presentation) {
    if($Snapshot.turnBased-ne$Archive.Combat.TurnBased-or$Snapshot.playerInCombat-ne$true-or$Snapshot.gameTicks-lt$Archive.GameTimeTicks){throw 'Native world mode, presence or clock differs.'}
    foreach($field in @('TurnBased','Round','StartTicks','RoundStartTicks','TurnStartTicks','TimeSinceStart','TimeToNextRound','HasSurpriseRound','HasEnemy','HadEnemy','NextActor','BeforeCombatSelected','Current','Paired')){
        if((ConvertTo-KmcChunk7Canonical $Snapshot.combat.$field)-cne(ConvertTo-KmcChunk7Canonical $Archive.Combat.$field)){throw ('Immediate shared restoration changed '+$field)}}
    foreach($field in @('Roster','Allocations','Engagements')){
        $keys=if($field-ceq'Engagements'){@('From','To')}else{@('ActorId')}
        if((ConvertTo-KmcChunk7Canonical @($Snapshot.combat.$field|Sort-Object -Property $keys))-cne(ConvertTo-KmcChunk7Canonical @($Archive.Combat.$field|Sort-Object -Property $keys))){throw ('Immediate shared restoration changed '+$field)}}
    if(@($Snapshot.actors).Count-ne@($Archive.Combat.Actors).Count){throw 'Immediate native restoration omitted an actor.'}
    foreach($actor in $Archive.Combat.Actors){$actual=@($Snapshot.actors|Where-Object{$_.Native.Id-ceq$actor.Native.Id});if($actual.Count-ne1-or(ConvertTo-KmcChunk7Canonical $actual[0])-cne(ConvertTo-KmcChunk7Canonical $actor)){throw 'Immediate native restoration changed an actor remainder, initiative or preparation.'}}
    if((ConvertTo-KmcChunk7Canonical @($Snapshot.participations|Sort-Object OwnerId))-cne(ConvertTo-KmcChunk7Canonical @($Archive.Participations|Sort-Object OwnerId))){throw 'Immediate restoration changed a retained obligation, grant/context or release round.'}
    $ids=@($Archive.Pairs|ForEach-Object{$_.PairId})+@($Archive.Participations|ForEach-Object{$_.OwnerId})|Select-Object -Unique
    if(@($Snapshot.owners).Count-ne@($ids).Count){throw 'Immediate restoration omitted retained ownership.'}
    foreach($id in $ids){$actual=@($Snapshot.owners|Where-Object{([Guid]$_.ownerId)-eq([Guid]$id)});$pair=@($Archive.Pairs|Where-Object PairId -CEQ $id);$part=@($Archive.Participations|Where-Object OwnerId -CEQ $id)
        if($actual.Count-ne1){throw 'Exact native owner identity is missing or duplicated.'}
        Assert-KmcChunk7NativeCounters $actual[0] @('generation')
        $generation=if($pair.Count){$pair[0].Generation}else{$part[0].Generation};$state=if($pair.Count){if($Presentation){'Active'}else{'Reserved'}}else{'Detached'}
        if($actual.Count-ne1-or$actual[0].generation-ne$generation-or$actual[0].state-cne$state){throw 'Immediate restoration changed exact owner identity/generation/state.'}
        $rider=if($pair.Count){$pair[0].Rider.Id}else{$part[0].State.RiderId};$mount=if($pair.Count){$pair[0].Mount.Id}elseif($null-ne$part[0].State.MountId){$part[0].State.MountId}else{$part[0].State.PendingSplitId}
        if($actual[0].riderId-cne$rider-or$actual[0].mountId-cne$mount){throw 'Immediate restoration changed stable owner actor roles.'}}
    if($Presentation-and(ConvertTo-KmcChunk7Canonical @($Snapshot.nativeControls|Sort-Object ActorId,Index))-cne(ConvertTo-KmcChunk7Canonical @($Archive.Slots|Sort-Object ActorId,Index))){throw 'Native presentation restore omitted another owner control slot.'}
}

function Assert-KmcChunk7CheckpointTrace($Trace,$Request) {
    if($null-eq$Trace-or$Trace.dropped-ne0-or$Trace.observationErrors-ne0-or$Trace.events-isnot[Array]-or$Trace.events.Count-gt16000-or
       $Trace.identityRegistry.contract-cne'retained-native-object-identity-v1'-or$Trace.identityRegistry.capacity-ne16000-or$Trace.identityRegistry.faults-ne0){throw 'Checkpoint trace is incomplete or has invalid native identities.'}
    $sequence=0
    foreach($event in $Trace.events){$sequence++;if($event.sequence-ne$sequence-or[string]::IsNullOrEmpty($event.boundary)-or$null-eq$event.state-or[string]::IsNullOrEmpty($event.state.actor)){throw 'Native trace sequence or actor snapshot is missing.'}}
    foreach($token in @('06000C3C','06009120','0600838F','06000C46','0600939D')) {
        $hook=@($Trace.observerHooks|Where-Object token -CEQ $token)
        if($hook.Count-ne1-or$hook[0].moduleMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'){throw 'Native checkpoint observer is not pinned to the installed action/turn contract.'}
    }
}
function Assert-KmcChunk7CheckpointSource($Checkpoint,$Archive,$Request) {
    Assert-KmcChunk7Boolean $Checkpoint.ready $true
    if($Checkpoint.provenance-cne'source-native-setup'-or@($Checkpoint.errors).Count-ne0-or$null-eq$Checkpoint.checkpoint-or
       $Checkpoint.targetGroupId-cne('KMC.RuntimeHostile.'+$Request.runId+':two-pair-target')){throw 'Native combat source setup or durable target custody differs.'}
    $ids=@($Checkpoint.actorIds);if($ids.Count-ne4-or@($ids|Select-Object -Unique).Count-ne4-or
       $ids[0]-cne'b6628a77-4962-47a4-a17c-88d9836fc9d5'-or$ids[1]-cne'd79a4f6c-b74e-4868-95bd-533899131acb'-or$ids[2]-cne'd17c8fd0-6627-40c4-bf9a-024cf001c1f7') {throw 'Checkpoint source actor order differs.'}
    Assert-KmcChunk7CheckpointTrace $Checkpoint.trace $Request
    $legacy=$Request.persistenceCase-ceq'legacy-mounted-pending-tb-manual';$rt=$Request.persistenceCase-ceq'multi-rt-debt-manual';$zero=$Request.persistenceCase-ceq'zero-mounted-tb-retained-manual'
    $spenders=@(if($legacy){$ids[1]}elseif($rt){$ids[0];$ids[3]}elseif($zero){$ids}else{$ids[0];$ids[1];$ids[2]})
    $attacks=@($Checkpoint.source.inputs|Where-Object{$null-ne$_.PSObject.Properties['outcome']})
    if($attacks.Count-ne$spenders.Count){throw 'Checkpoint did not observe each exact native primary expenditure.'}
    foreach($id in $spenders) {
        $attack=@($attacks|Where-Object{$_.outcome.actorId-ceq$id})
        if($attack.Count-ne1){throw 'Native checkpoint attack actor missing or duplicated.'};$attack=$attack[0]
        $caster=if($id-cin@($ids[0],$ids[1])){$ids[0]}else{$ids[2]}
        if($attack.casterId-cne$caster-or$attack.executorId-cne$caster-or$attack.selectionAfterInput-ceq$caster-or
           $attack.nativeCastDelta-ne1-or$attack.nativeRefusalDelta-ne0-or$attack.command-le0-or$attack.outcome.resourceOwnerId-cne$id-or
           $attack.outcome.targetId-cne$Checkpoint.targetId-or$attack.outcome.result-cne'Success'-or$attack.outcome.completedAttacks-ne1){throw 'Native primary lost exact actor, target, resource owner or one-shot outcome.'}
        Assert-KmcChunk7Boolean $attack.clicked $true;Assert-KmcChunk7Boolean $attack.outcome.nativeAttackRule $true
        $cost=@($Checkpoint.trace.events|Where-Object{$_.state.actor-ceq$id-and$_.boundary-ceq'cost-after'-and$_.commandType-ceq'KingmakerMountedCombat.Integration.MountedPairAttackCommand'})
        if($cost.Count-ne1){throw 'Native primary spending lacks one exact native cost callback.'};$cost=$cost[0]
        $boundaries=@('cost-before','cost-after');if(-not$rt){$boundaries+=@('actor-cost-before','actor-cost-after')}
        foreach($boundary in $boundaries) {
            $row=@($Checkpoint.trace.events|Where-Object{$_.command-eq$cost.command-and$_.boundary-ceq$boundary})
            if($row.Count-ne1-or$row[0].commandActor-cne$id-or$row[0].state.actor-cne$id-or$row[0].acted-ne$true-or$row[0].actionType-cne'Standard'){throw 'A native cost callback has the wrong actor, action or command.'}
        }
        $before=@($Checkpoint.trace.events|Where-Object{$_.command-eq$cost.command-and$_.boundary-ceq'cost-before'})[0]
        if($cost.sequence-le$before.sequence-or[double]$cost.state.standard-le[double]$before.state.standard){throw 'Native primary did not increase only its actor standard expenditure.'}
        foreach($field in @('move','swift','reactions','reactionCooldown')){if($cost.state.$field-ne$before.state.$field){throw 'Native primary changed an unrelated actor resource at spending.'}}
        $prior=@($attack.before.actors|Where-Object id -CEQ $id);$after=@($attack.after.actors|Where-Object id -CEQ $id)
        if($prior.Count-ne1-or$after.Count-ne1-or[double]$after[0].standard-le[double]$prior[0].standard){throw 'Source attack snapshot has no distinct native expenditure.'}
        if(-not$rt){foreach($other in $ids|Where-Object{$_-cne$id}){$a=@($attack.before.actors|Where-Object id -CEQ $other);$b=@($attack.after.actors|Where-Object id -CEQ $other);foreach($field in @('standard','move','swift','reactions','reactionCooldown')){if($a.Count-ne1-or$b.Count-ne1-or$a[0].$field-ne$b[0].$field){throw 'Native primary transferred expenditure to another actor.'}}}}
    }
    if($legacy){$pending=$Checkpoint.source.pendingSurvivor;if($pending.stimulus-cne'UnitEntityData.LeaveCombat then native RemoveUnit if still registered'-or
       (ConvertTo-KmcChunk7Canonical $pending.participation)-cne(ConvertTo-KmcChunk7Canonical $Archive.Participations[0].State)){throw 'Legacy pending source lacks the actual native rider-removal stimulus and preserved participation.'}}
    if($zero){$dismounts=@($Checkpoint.source.inputs|Where-Object{$_.label-clike'checkpoint-native-dismount-*'});if($dismounts.Count-ne2){throw 'Zero-mounted checkpoint did not execute two native Dismount inputs.'};foreach($nativeInput in $dismounts){if($nativeInput.nativeCastDelta-ne1-or$nativeInput.nativeRefusalDelta-ne0-or$nativeInput.clicked-ne$true){throw 'Native detached checkpoint input was refused or duplicated.'}}}
}
function Assert-KmcChunk7CheckpointContinuation($Value,$Archive,$Request,[bool]$Cold) {
    $case=$Request.persistenceCase;$kind=switch($case){'multi-rt-debt-manual'{'RealTimeDebts'};'multi-tb-debt-manual'{'TurnBasedDebts'};'zero-mounted-tb-retained-manual'{'ZeroMountedRetained'};'legacy-mounted-pending-tb-manual'{'MountedPendingOnly'}}
    Assert-KmcChunk7Boolean $Value.ready $true;Assert-KmcChunk7Boolean $Value.continuationComplete $true
    $provenance=if($Cold){'cold-native-archive-binding'}else{'source-native-setup'}
    if($Value.kind-cne$kind-or$Value.provenance-cne$provenance-or$Value.actorIds-isnot[Array]-or$Value.actorIds.Count-ne4-or
       @($Value.actorIds|Select-Object -Unique).Count-ne4-or-not(Test-KmcExactJsonInteger $Value.continuationTraceOffset)-or$Value.continuationTraceOffset-lt0){throw 'Native checkpoint continuation identity differs.'}
    $ids=@($Value.actorIds);$foreign=@($Archive.Combat.Actors|Where-Object{$_.Native.Id-cnotin$ids})
    if($Archive.Combat.Actors.Count-ne5-or$foreign.Count-ne1-or$foreign[0].Native.Id-cne$Value.targetId-or
       $Value.targetGroupId-cnotmatch'^KMC[.]RuntimeHostile[.][A-Za-z0-9._-]{1,120}:two-pair-target$'){throw 'Native continuation substituted its saved target or actor set.'}
    Assert-KmcChunk7CheckpointTrace $Value.trace $Request
    if($Value.continuationTraceOffset-gt$Value.trace.events.Count-or$Value.driver.archiveValidated-ne$true-or$Value.driver.archiveRound-ne$Archive.Combat.Round){throw 'Native continuation lost its archive boundary.'}
    $before=@($Value.continuation|Where-Object{$_.PSObject.Properties['label']-and$_.label-ceq'continuation-before-input'});$after=@($Value.continuation|Where-Object{$_.PSObject.Properties['label']-and$_.label-ceq'continuation-complete'})
    if($before.Count-ne1-or$after.Count-ne1){throw 'Native continuation boundary snapshots are absent.'};$before=$before[0];$after=$after[0]
    foreach($state in @($before,$after)){if(@($state.nativeActors).Count-ne4-or@($state.grants).Count-ne4){throw 'Native continuation omitted actor debt or grant counts.'};foreach($id in $ids){if(@($state.nativeActors|Where-Object Id -CEQ $id).Count-ne1-or@($state.grants|Where-Object actorId -CEQ $id).Count-ne1){throw 'Native continuation duplicates an actor debt or grant identity.'}}}
    $events=@($Value.trace.events|Where-Object{$_.sequence-gt$Value.continuationTraceOffset})
    if($kind-cin@('RealTimeDebts','TurnBasedDebts')) {
        $m=$Value.driver.movement;Assert-KmcChunk7Boolean $m.terminal $true
        if($m.executorId-cne$ids[3]-or$m.command-le0-or$m.command-ne$Value.driver.movementCommand-or$Value.driver.movementExecutorId-cne$ids[3]){throw 'Continuation ground command routed to a different actor.'}
        $start=@($m.before.positions|Where-Object actorId -CEQ $ids[3]);$end=@($m.after.positions|Where-Object actorId -CEQ $ids[3])
        if($start.Count-ne1-or$end.Count-ne1){throw 'Native continuation omitted measured actor positions.'}
        $distance=[Math]::Sqrt([Math]::Pow([double]$end[0].x-$start[0].x,2)+[Math]::Pow([double]$end[0].y-$start[0].y,2)+[Math]::Pow([double]$end[0].z-$start[0].z,2))
        if(-not(Test-KmcChunk7FiniteNumber $m.distance)-or$distance-le0.02-or[Math]::Abs($distance-$m.distance)-gt0.0001){throw 'Native continuation distance differs from measured movement.'}
        if(@($events|Where-Object{$_.command-eq$m.command-and$_.commandActor-ceq$ids[3]-and$_.boundary-ceq'admission-after'}).Count-ne1){throw 'Saved eligible actor has no exact native movement admission.'}
        $noGrantEnd=@(if($kind-ceq'TurnBasedDebts'){$Value.continuation|Where-Object{$_.PSObject.Properties['label']-and$_.label-ceq'saved-partner-movement-no-prepare'}}else{$after})
        if($noGrantEnd.Count-ne1){throw 'Saved partner movement boundary missing.'};$noGrantEnd=$noGrantEnd[0]
        foreach($id in $ids){$a=@($before.grants|Where-Object actorId -CEQ $id);$b=@($noGrantEnd.grants|Where-Object actorId -CEQ $id);if($b.Count-ne1-or$a[0].count-ne$b[0].count){throw 'Saved continuation manufactured another native preparation.'}}
        if(@($events|Where-Object{$_.state.actor-cin$ids-and$_.boundary-ceq'prepare-before'-and$_.frame-le$noGrantEnd.frame}).Count-ne0){throw 'Native preparation entered during saved eligible continuation.'}
        if($kind-ceq'TurnBasedDebts'){
            if($noGrantEnd.currentActor-cne$ids[2]){throw 'Native continuation changed the current actor before completion.'}
            $native=@($noGrantEnd.nativeActors|Where-Object Id -CEQ $ids[2])[0];$saved=@($Archive.Combat.Actors|Where-Object{$_.Native.Id-ceq$ids[2]})[0].Native
            if($native.Standard-ne$saved.Standard-or@($noGrantEnd.nativeActors|Where-Object Id -CEQ $ids[3])[0].Standard-ne0){throw 'Saved partner movement changed another actor standard debt.'}
            if(@($Value.nativeCompletion.errors).Count-ne0){throw 'Native turn completion observer failed.'}
            foreach($id in @($ids[2],$ids[3])){if(@($Value.nativeCompletion.calls|Where-Object{$_.kind-ceq'end'-and$_.before.actorId-ceq$id-and$_.complete-eq$true-and$_.before.allocationSequence-ge$Value.continuationTraceOffset}).Count-ne1){throw 'Both restored native actor contexts did not complete exactly once.'}}
        } else {
            $ready=@($Value.continuation|Where-Object{$_.PSObject.Properties['label']-and$_.label-ceq'native-rt-readiness'})
            if($ready.Count-ne1-or$ready[0].gameTicks-le$before.gameTicks){throw 'RT continuation skipped native cooldown time.'}
            foreach($id in @($ids[0],$ids[3])){$saved=@($Archive.Combat.Actors|Where-Object{$_.Native.Id-ceq$id})[0].Native;$native=@($ready[0].nativeActors|Where-Object Id -CEQ $id)[0];$elapsed=([double]$ready[0].gameTicks-[double]$Archive.GameTimeTicks)/10000000.0;if($native.Standard-gt0.001-or$elapsed+0.01-lt$saved.Standard){throw 'RT actor debt disappeared before native elapsed time.'}}
        }
    } else {
        $mounts=if($kind-ceq'MountedPendingOnly'){@($ids[1])}else{@($ids[1],$ids[3])}
        foreach($id in $mounts){$row=@($Value.continuation|Where-Object{$_.PSObject.Properties['label']-and$_.label-ceq('independent-native-mount-'+$id)});if($row.Count-ne1-or$row[0].currentActor-cne$id-or$row[0].round-ne$Archive.Combat.Round+1){throw 'Retained mount was released before or after its first legitimate native round.'};$old=@($before.grants|Where-Object actorId -CEQ $id)[0];$new=@($row[0].grants|Where-Object actorId -CEQ $id)[0];if($new.count-ne$old.count+1-or@($events|Where-Object{$_.state.actor-ceq$id-and$_.boundary-ceq'prepare-before'}).Count-ne1){throw 'Independent native mount received a missing or duplicate preparation.'}}
        if($kind-ceq'ZeroMountedRetained'){
            $repeat=@($Value.continuation|Where-Object{$_.PSObject.Properties['stimulus']})
            if($repeat.Count-ne1-or$repeat[0].stimulus-cne'repeat cleanup at actual native StartRound return; no round/resource mutation'-or$repeat[0].before.round-ne$Archive.Combat.Round+1-or
               (ConvertTo-KmcChunk7Canonical $repeat[0].before.nativeActors)-cne(ConvertTo-KmcChunk7Canonical $repeat[0].after.nativeActors)){throw 'Repeated native detached cleanup changed debt or missed its real round boundary.'}
            foreach($row in @($repeat[0].before,$repeat[0].after)){if(@($row.participations).Count-ne2-or@($row.participations|Where-Object SplitReleaseRound -NE $Archive.Combat.Round).Count-ne0){throw 'Repeated cleanup restarted or dropped original detached release ownership.'}}
        }
    }
}

function Assert-KmcChunk7CombatRestorationProbe($Probe,$Archive,$Request) {
    Assert-KmcChunk7Boolean $Probe.complete $true;Assert-KmcChunk7Boolean $Probe.installedBeforeWorldLoad $true
    Assert-KmcChunk7NativeCounters $Probe @('maximumEvents','dropped','errors')
    if($Probe.contract-cne'chunk7-native-restoration-entry-observer-v1'-or$Probe.maximumEvents-ne2048-or$Probe.dropped-ne0-or$Probe.errors-ne0-or
       $Probe.prepareMetric-cne'native-method-entries-including-production-refused-attempts'-or@($Probe.hooks).Count-ne6){throw 'Whole-world native restoration observer is incomplete.'}
    foreach($token in @('06000C3C','0600939D')){$hook=@($Probe.hooks|Where-Object token -CEQ $token);if($hook.Count-ne1-or$hook[0].moduleMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'){throw 'Native restore observation hook differs.'}}
    foreach($method in @('RestoreActor','RestoreDebt','TryRestoreArchiveCombat','RestoreArchivePresentation')){$hook=@($Probe.hooks|Where-Object method -CEQ $method);if($hook.Count-ne1-or$hook[0].moduleMvid-cne$Request.dllMvid){throw 'Product restore observer is not bound to the selected candidate.'}}
    $ids=@($Archive.Combat.Actors|ForEach-Object{$_.Native.Id})
    if(@($Probe.events|Where-Object{$_.PSObject.Properties['actorId']-and$_.actorId-cin$ids-and$_.kind-cin@('prepare-before','round-before')}).Count-ne0){throw 'Native archive restoration entered preparation or a round.'}
    foreach($actor in $Archive.Combat.Actors){foreach($kind in @('restore-actor-before','restore-actor-after','restore-debt-before','restore-debt-after')){
        $row=@($Probe.events|Where-Object{$_.kind-ceq$kind-and$_.actorId-ceq$actor.Native.Id})
        if($row.Count-ne1){throw 'Whole native actor/debt state was not restored once.'}
        if($kind-ceq'restore-debt-after'-and((ConvertTo-KmcChunk7Canonical $row[0].debt)-cne(ConvertTo-KmcChunk7Canonical $actor.Native)-or$row[0].restorePhase-cne'validated-before-native-mutation')){throw 'Native restore changed exact spent actor remainder.'}
    }}
    foreach($kind in @('shared-world-restored','all-owner-presentation-restored')){$row=@($Probe.events|Where-Object kind -CEQ $kind);if($row.Count-ne1){throw 'Immediate native all-owner restoration boundary missing or duplicated.'};Assert-KmcChunk7NativeWorldRestore $row[0].snapshot $Archive ($kind-ceq'all-owner-presentation-restored')}
}
function Assert-KmcChunk7CombatPersistenceObservation($Request,$Artifact) {
    $o=$Artifact.observations;$cold=$Request.scenario-ceq'chunk7-persistence-load';$legacy=$Request.persistenceCase-ceq'legacy-mounted-pending-tb-manual'
    Assert-KmcChunk7NativeCounters $o @('nativeSchema','nativeCapacity','processId')
    if($Artifact.status-cne'PASS'-or-not(Test-KmcChunk7CombatPersistenceCase $Request.persistenceCase)-or$o.checkpoint-cne$Request.persistenceCase-or
       $o.sourceWorkingSha256-cne$Request.fixture.working.sha256-or$o.sourceWorkingSha256-cne'5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6'-or
       $o.nativeSchema-ne3-or$o.nativeCapacity-ne2-or-not(Test-KmcExactJsonInteger $o.processId)-or$o.processId-le0){throw 'Native combat persistence family, fixture or capability identity differs.'}
    Assert-KmcChunk7Boolean $o.cold $cold;Assert-KmcChunk7Boolean $o.combatDebtProof $true
    Assert-KmcChunk7Boolean $o.nativeProgressionRequiresExternalWorkingRestoration $true;Assert-KmcChunk7Boolean $o.ordinaryRespecProven $false
    $archive=if($cold){$o.loadedArchive}else{$o.nativeWrite.archive};Assert-KmcChunk7CombatArchive $archive $Request
    Assert-KmcChunk7CheckpointContinuation $o.combatContinuation $archive $Request $cold
    $continuation=$o.combatContinuation;$ids=@($continuation.actorIds)
    $label=if($cold){'cold-restored-before-input'}else{'native-write-complete'};$snapshot=@($o.snapshots|Where-Object label -CEQ $label)
    if($snapshot.Count-ne1){throw 'Exact native archive snapshot is missing or duplicated.'}
    Assert-KmcChunk7NativeCounters $snapshot[0] @('semanticRestores','presentationRestores','nativePostLoads','snapshots','nativeCastRequests')
    Assert-KmcChunk7NativeCounters $snapshot[0].controls @('DuplicateFactCount','ManagedHotbarSlotCount')
    Assert-KmcChunk7Boolean $snapshot[0].controls.SerializationSuspended $false
    foreach($owner in $snapshot[0].owners){Assert-KmcChunk7NativeCounters $owner @('generation')}
    if($snapshot.Count-ne1-or$snapshot[0].actors.Count-ne4-or$snapshot[0].owners.Count-ne$archive.Participations.Count-or
       $snapshot[0].controls.ManagedHotbarSlotCount-ne$archive.Slots.Count-or$snapshot[0].controls.DuplicateFactCount-ne0-or$snapshot[0].controls.SerializationSuspended-ne$false){throw 'Native archive boundary omitted actor, ownership or exact controls.'}
    if($cold) {
        Assert-KmcChunk7NativeCounters $o @('loadedSourceSchema')
        if((ConvertTo-KmcChunk7Canonical $o.loadedDescriptor)-cne(ConvertTo-KmcChunk7Canonical $Request.persistenceLoad)-or
           $o.loadedSourceSchema-ne$(if($legacy){2}else{3})-or@($o.inputs).Count-ne0-or$snapshot[0].semanticRestores-ne$archive.Combat.Actors.Count-or
           $snapshot[0].nativePostLoads-ne$archive.Combat.Actors.Count-or$snapshot[0].presentationRestores-ne$archive.Pairs.Count-or$snapshot[0].snapshots-ne0-or
           $snapshot[0].nativeCastRequests-ne0-or$snapshot[0].archivePhase-cne'all-pair-presentation-restored'){throw 'Cold native combat substituted a source, dropped PostLoads or replayed commands.'}
        Assert-KmcChunk7CombatRestorationProbe $o.coldRestoration $archive $Request
        if($legacy){Assert-KmcChunk7Schema2ResaveObservation $Request $o}
        $binding=$continuation.driver.coldBinding
        if($binding.label-cne'read-only-cold-bind'-or@($binding.nativeActors).Count-ne4){throw 'Cold continuation omitted its read-only native fixture binding.'}
    } else {
        Assert-KmcChunk7NativeCounters $o.nativeWrite @('snapshotCount','sourceSchema')
        Assert-KmcChunk7NativeCounters $o.saveRequested @('snapshotCount')
        if($o.nativeWrite.nativeType-cne'Manual'-or$o.saveRequested.nativeType-cne'Manual'-or$o.nativeWrite.operation-cne'None'-or
           $o.nativeWrite.snapshotCount-ne1-or$o.saveRequested.snapshotCount-ne0-or$o.nativeWrite.sha256-cnotmatch'^[0-9a-f]{64}$'-or
           -not(Test-KmcExactJsonInteger $o.nativeWrite.length)-or$o.nativeWrite.length-le0-or$o.nativeWrite.length-gt256MB-or
           $o.nativeWrite.sourceSchema-ne$(if($legacy){2}else{3})){throw 'Native combat save category, version or completion differs.'}
        foreach($field in @('nativeSaveAllowed','newSlot')){Assert-KmcChunk7Boolean $o.saveRequested.$field $true};Assert-KmcChunk7Boolean $o.nativeWrite.nativeCallback $true
        if($legacy){Assert-KmcChunk7LegacyPendingMigration $o.nativeWrite.nativeMetadata $archive
            Assert-KmcChunk7NativeCounters $o.legacyWriterScope @('sourceSchema','owners')
            if($o.legacyWriterScope.selectedOnlyForCase-cne$Request.persistenceCase-or$o.legacyWriterScope.sourceSchema-ne2-or$o.legacyWriterScope.owners-ne1){throw 'Legacy native write escaped its singular case scope.'}
            Assert-KmcChunk7Boolean $o.legacyWriterScope.selectedCapabilityRestored $true;Assert-KmcChunk7Boolean $o.legacyWriterScope.restorationEnabledOnColdProcess $true
        }elseif((ConvertTo-KmcChunk7Canonical $o.nativeWrite.nativeMetadata)-cne(ConvertTo-KmcChunk7Canonical $archive)){throw 'Native schema-3 archive differs from actual metadata readback.'}
        Assert-KmcChunk7CheckpointSource $o.combatCheckpoint $archive $Request
        $original=@($o.snapshots|Where-Object label -CEQ 'before-native-save')
        if($original.Count-ne1-or@($original[0].owners).Count-ne@($snapshot[0].owners).Count){throw 'Source native owner custody lacks its initial complete boundary.'}
        Assert-KmcChunk7NativeCounters $original[0] @('semanticRestores','presentationRestores','nativePostLoads','snapshots','nativeCastRequests')
        Assert-KmcChunk7NativeCounters $original[0].controls @('DuplicateFactCount','ManagedHotbarSlotCount')
        Assert-KmcChunk7Boolean $original[0].controls.SerializationSuspended $false
        foreach($owner in $original[0].owners){
            Assert-KmcChunk7NativeCounters $owner @('generation')
            $actual=@($snapshot[0].owners|Where-Object{([Guid]$_.ownerId)-eq([Guid]$owner.ownerId)})
            $state=if($Request.persistenceCase-ceq'zero-mounted-tb-retained-manual'){'Detached'}else{'Active'}
            if($actual.Count-ne1-or$actual[0].generation-ne$owner.generation-or$actual[0].riderId-cne$owner.riderId-or$actual[0].mountId-cne$owner.mountId-or$actual[0].state-cne$state){throw 'Native save changed live owner identity, generation, actor roles or required detached custody.'}
            if(-not$legacy){$saved=@($archive.Participations|Where-Object{([Guid]$_.OwnerId)-eq([Guid]$owner.ownerId)});if($saved.Count-ne1-or$saved[0].Generation-ne$owner.generation){throw 'Native schema-3 source changed stable saved custody.'}}
        }
        $mountCount=if($legacy){1}else{2};if(@($o.inputs).Count-ne$mountCount){throw 'Wrong number of native source Mount commands.'}
        for($i=0;$i-lt$mountCount;$i++){$nativeInput=$o.inputs[$i];$rider=$ids[$i*2];$mount=$ids[$i*2+1]
            Assert-KmcChunk7NativeCounters $nativeInput @('nativeCastDelta','nativeRefusalDelta','generationAtInit')
            if($nativeInput.casterId-cne$rider-or$nativeInput.targetId-cne$mount-or$nativeInput.abilityGuid-cne'f053faad986631688defa003cd7bda0e'-or$nativeInput.nativeCastDelta-ne1-or$nativeInput.nativeRefusalDelta-ne0-or
               [string]::IsNullOrEmpty($nativeInput.control)-or$nativeInput.generationAtInit-lt0-or$nativeInput.selectionAfterInput-ceq$rider){throw 'Native source Mount lost exact caster, target, generation or captured selection.'}
            Assert-KmcChunk7Boolean $nativeInput.clicked $true;Assert-KmcChunk7Boolean $nativeInput.terminal $true}
        Assert-KmcChunk7NativeCounters $o.acquisition @('nativeRangerCommitCount','horseFactRankAtCommit')
        Assert-KmcChunk7Boolean $o.acquisition.horsePresentAtNativeCommit $true
        if($o.acquisition.nativeRangerCommitCount-ne4-or$o.acquisition.horsePresentAtNativeCommit-ne$true-or$o.acquisition.horseFactRankAtCommit-ne1){throw 'Native Horse fixture acquisition proof differs.'}
    }
    $cleanup=$o.checkpointCleanup;Assert-KmcChunk7Boolean $cleanup.cleanupComplete $true
    if(@($cleanup.cleanupErrors).Count-ne0-or$cleanup.targetId-cne$continuation.targetId-or$cleanup.targetGroupId-cne$continuation.targetGroupId){throw 'Checkpoint cleanup retained errors or lost exact target custody.'}
    if($cold){$c=$cleanup.driver.coldCleanup;foreach($field in @('targetRemoved','groupRemoved','nativeCombatEnded')){Assert-KmcChunk7Boolean $c.$field $true};foreach($field in @('borrowedFactionDestroyed','resourceFieldsWritten')){Assert-KmcChunk7Boolean $c.$field $false};if($c.targetId-cne$continuation.targetId-or$c.targetGroupId-cne$continuation.targetGroupId){throw 'Cold cleanup disposed another target or group.'}}
    foreach($field in @('pairsClear','sourceHorseRemoved','controlsSettled','saveCustodyClear','selectionRestored','nativeProgressionRequiresExternalWorkingRestoration')){Assert-KmcChunk7Boolean $o.cleanup.$field $true}
    Assert-KmcChunk7Boolean $o.cleanup.ordinaryRespecProven $false
    if(@($o.cleanup.world.owners).Count-ne0-or@($o.movements).Count-ne0){throw 'Combat cleanup retained ownership or replayed exploration continuation.'}
}

# The exact pending-only cold case also writes a separate native schema-3 Manual
# before continuation. Source-case schema-2 writing and historical intake remain
# unchanged; no archived input is made writable.
function Assert-KmcChunk7Schema2ResaveObservation($Request,$Observations) {
    if($Request.scenario-cne'chunk7-persistence-load'-or$Request.persistenceCase-cne'legacy-mounted-pending-tb-manual'){
        throw 'Schema-2 resave belongs only to its exact pending-only cold case.'
    }
    $before=$Observations.loadedArchive;$migration=$Observations.schema2Migration;$write=$Observations.nativeWrite;$after=$write.archive
    Assert-KmcExactProperties $migration @('inputSchema','inputSha256','inputLength','migratedArchive','inputUnchanged','resavedWorld') 'schema-2 native resave migration'
    Assert-KmcChunk7NativeCounters $migration @('inputSchema','inputLength')
    Assert-KmcChunk7Boolean $migration.inputUnchanged $true
    if($migration.inputSha256-isnot[string]-or$migration.inputSha256-cnotmatch'^[0-9a-f]{64}$'){throw 'Schema-2 input hash must be one exact SHA256 string.'}
    foreach($field in @('nativeType','nativeName','operation','sha256')){if($write.$field-isnot[string]){throw ('Schema-2 native write '+$field+' must be a scalar string.')}}
    foreach($field in @('nativeType','nativeName')){if($Observations.saveRequested.$field-isnot[string]){throw ('Schema-2 save request '+$field+' must be a scalar string.')}}
    if($migration.inputSchema-ne2-or$migration.inputSha256-cne$Request.persistenceLoad.sha256-or$migration.inputLength-ne$Request.persistenceLoad.length-or
        (ConvertTo-KmcChunk7Canonical $migration.migratedArchive)-cne(ConvertTo-KmcChunk7Canonical $before)){
        throw 'Schema-2 resave lost its immutable source or migrated graph.'
    }
    Assert-KmcChunk7CombatArchive $after $Request
    Assert-KmcChunk7NativeCounters $write @('snapshotCount','sourceSchema','length')
    Assert-KmcChunk7NativeCounters $Observations.saveRequested @('snapshotCount','gameTicks')
    foreach($field in @('nativeSaveAllowed','newSlot')){Assert-KmcChunk7Boolean $Observations.saveRequested.$field $true}
    Assert-KmcChunk7Boolean $write.nativeCallback $true
    if($write.nativeType-cne'Manual'-or$write.nativeName-cne'KMC_C7_SCHEMA2'-or$write.operation-cne'None'-or$write.sourceSchema-ne3-or
        $write.snapshotCount-ne1-or$Observations.saveRequested.snapshotCount-ne0-or$Observations.saveRequested.nativeType-cne'Manual'-or
        $Observations.saveRequested.nativeName-cne'KMC_C7_SCHEMA2'-or$write.sha256-cnotmatch'^[0-9a-f]{64}$'-or
        $write.sha256-ceq$Request.persistenceLoad.sha256-or$write.length-le0-or$write.length-gt256MB-or
        (ConvertTo-KmcChunk7Canonical $write.nativeMetadata)-cne(ConvertTo-KmcChunk7Canonical $after)){
        throw 'Schema-2 resave is not one completed native schema-3 write into its separate Manual slot.'
    }
    $elapsed=([double]$after.GameTimeTicks-[double]$before.GameTimeTicks)/10000000.0
    if($elapsed-lt0-or$elapsed-gt30){throw 'Schema-2 native resave escaped its bounded clock.'}
    $normalizedBefore=$before|ConvertTo-Json -Depth 64|ConvertFrom-Json
    $normalizedAfter=$after|ConvertTo-Json -Depth 64|ConvertFrom-Json
    $normalizedAfter.GameTimeTicks=$normalizedBefore.GameTimeTicks
    foreach($actor in $after.Combat.Actors){
        $prior=@($before.Combat.Actors|Where-Object {$_.Native.Id-ceq$actor.Native.Id})
        if($prior.Count-ne1){throw 'Schema-2 resave substituted a native actor.'}
        foreach($field in @('Standard','Move','Swift','Initiative','Reaction')){
            $a=[double]$prior[0].Native.$field;$b=[double]$actor.Native.$field
            if($b-gt$a+0.001-or$b+$elapsed*1.1+0.001-lt$a){throw 'Schema-2 resave refunded or changed debt beyond native elapsed time.'}
        }
    }
    $debts=@($normalizedAfter.Pairs|ForEach-Object {$_.Rider;$_.Mount})+@($normalizedAfter.Combat.Actors|ForEach-Object {$_.Native})
    foreach($debt in $debts){
        $prior=@($before.Combat.Actors|Where-Object {$_.Native.Id-ceq$debt.Id})
        if($prior.Count-ne1){throw 'Schema-2 resave substituted a native debt identity.'}
        foreach($field in @('Standard','Move','Swift','Initiative','Reaction')){$debt.$field=$prior[0].Native.$field}
    }
    foreach($graph in @($normalizedBefore,$normalizedAfter)){
        $graph.Pairs=@($graph.Pairs|Sort-Object PairId);$graph.Participations=@($graph.Participations|Sort-Object OwnerId)
        $graph.Slots=@($graph.Slots|Sort-Object ActorId,Index,Kind);$graph.Combat.Actors=@($graph.Combat.Actors|Sort-Object {$_.Native.Id})
        $graph.Combat.Roster=@($graph.Combat.Roster|Sort-Object ActorId);$graph.Combat.Allocations=@($graph.Combat.Allocations|Sort-Object ActorId)
        $graph.Combat.Engagements=@($graph.Combat.Engagements|Sort-Object From,To)
    }
    if((ConvertTo-KmcChunk7Canonical $normalizedBefore)-cne(ConvertTo-KmcChunk7Canonical $normalizedAfter)){
        throw 'Schema-2 resave changed pending release, owner/generation, native context/allocation or another protected graph field.'
    }
    Assert-KmcChunk7NativeWorldRestore $migration.resavedWorld $after $true
    $boundary=@($Observations.snapshots|Where-Object label -CEQ 'schema2-native-resave-before-continuation')
    $loaded=@($Observations.snapshots|Where-Object label -CEQ 'cold-restored-before-input')
    $continued=@($Observations.combatContinuation.continuation|Where-Object { $_.PSObject.Properties['label']-and$_.label-ceq'continuation-before-input' })
    if($boundary.Count-ne1-or$loaded.Count-ne1-or$continued.Count-ne1){throw 'Schema-2 resave omitted its before-continuation boundary.'}
    foreach($row in @($boundary[0],$loaded[0],$continued[0])){Assert-KmcChunk7NativeCounters $row @('frame','gameTicks')}
    Assert-KmcChunk7NativeCounters $boundary[0] @('snapshots','nativeCastRequests','semanticRestores','presentationRestores','nativePostLoads')
    if($boundary[0].frame-lt$loaded[0].frame-or$boundary[0].frame-gt$continued[0].frame-or
        $boundary[0].gameTicks-lt$after.GameTimeTicks-or$boundary[0].gameTicks-gt$continued[0].gameTicks-or
        $boundary[0].snapshots-ne1-or$boundary[0].nativeCastRequests-ne0-or$boundary[0].semanticRestores-ne$before.Combat.Actors.Count-or
        $boundary[0].nativePostLoads-ne$before.Combat.Actors.Count-or$boundary[0].presentationRestores-ne1){
        throw 'Schema-2 resave completed after continuation, repeated restoration or replayed native input.'
    }
}
