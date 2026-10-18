# Closed, bounded native two-pair combat evidence. The shared trace validator is
# defined by Chunk7CombatPersistenceEvidence; neither reader performs mutations.
Set-StrictMode -Version Latest
function Test-KmcChunk7CombatScenario([string]$Scenario) { $Scenario-cin@('chunk7-two-pair-combat-rider-first','chunk7-two-pair-combat-mount-first') }
function Get-KmcChunk7CombatRows([string]$Scenario) {
    if(-not(Test-KmcChunk7CombatScenario $Scenario)){throw 'Unknown native combat scenario.'}
    @($Scenario,'C7-combat-fixture','C7-combat-native-mounts','C7-combat-restored-mounts','C7-combat-native-orders','C7-combat-pair-a-full-activation',
      'C7-combat-pair-b-full-activation','C7-combat-tb-charge-refusal','C7-combat-refresh','C7-combat-r1-round-release',
      'C7-combat-native-pending-survivor','C7-combat-local-cleanup-failure','C7-combat-cleanup','C7-combat-runtime','C7-combat-interrupted')
}
function Get-KmcChunk7CombatRuntimeRows {
    @('chunk7-two-pair-combat-rider-first','chunk7-two-pair-combat-mount-first' | ForEach-Object { Get-KmcChunk7CombatRows $_ } | Select-Object -Unique)
}
function Assert-KmcChunk7CombatArtifact($Request,$Manifest,[string]$Status='PASS') {
    $present=@($Manifest.artifacts|Where-Object{$_.relativePath-ceq'chunk7-combat-evidence.json'-or$_.kind-ceq'chunk7-combat-evidence'})
    if(-not(Test-KmcChunk7CombatScenario (Get-KmcChunk7SelectedScenario $Request))){if($present.Count-ne0){throw 'Another scenario supplied Chunk 7 combat evidence.'};return}
    if($present.Count-eq0-and$Status-cne'PASS'){return}
    $artifact=Read-KmcChunk7ManifestArtifact $Request $Manifest 'chunk7-combat-evidence.json' 'chunk7-combat-evidence'
    Assert-KmcChunk7CombatEvidence $Request $artifact $Status
}
function Get-KmcChunk7CombatActor($Window,[string]$Id) {
    $rows=@($Window.actors|Where-Object id -CEQ $Id)
    if($rows.Count-ne1){throw 'Native combat actor snapshot is missing or duplicated.'}
    $row=$rows[0]
    foreach($field in @('standard','move','swift','reactions','reactionCooldown')){if(-not(Test-KmcChunk7FiniteNumber $row.$field)-or$row.$field-lt0){throw 'Native combat resource observation is invalid.'}}
    return $row
}
function Assert-KmcChunk7SameCombatBudget($Before,$After,[string[]]$Ids) {
    foreach($id in $Ids){$a=Get-KmcChunk7CombatActor $Before $id;$b=Get-KmcChunk7CombatActor $After $id
        foreach($field in @('standard','move','swift','reactions','reactionCooldown')){if($a.$field-ne$b.$field){throw 'Unrelated native actor expenditure changed.'}}}
}
function Assert-KmcChunk7CombatAttack($Attack,$Trace,[string]$Actor,[string]$Caster,[string]$Target,[string[]]$Ids) {
    if($Attack.casterId-cne$Caster-or$Attack.executorId-cne$Caster-or$Attack.selectionAfterInput-ceq$Caster-or$Attack.selectionAfterInput-cnotin$Ids-or
       $Attack.nativeCastDelta-ne1-or$Attack.nativeRefusalDelta-ne0-or$Attack.command-le0-or$Attack.outcome.actorId-cne$Actor-or
       $Attack.outcome.resourceOwnerId-cne$Actor-or$Attack.outcome.targetId-cne$Target-or$Attack.outcome.result-cne'Success'-or$Attack.outcome.completedAttacks-ne1){throw 'Native primary command/caster/action-owner outcome differs.'}
    Assert-KmcChunk7Boolean $Attack.clicked $true;Assert-KmcChunk7Boolean $Attack.outcome.nativeAttackRule $true;Assert-KmcChunk7Boolean $Attack.outcome.singleAttack $true
    $cost=@($Trace.events|Where-Object{$_.boundary-ceq'cost-after'-and$_.state.actor-ceq$Actor-and
        $_.commandType-ceq'KingmakerMountedCombat.Integration.MountedPairAttackCommand'-and$_.frame-ge$Attack.before.frame-and$_.frame-le$Attack.after.frame})
    if($cost.Count-ne1){throw 'Primary has no unique native cost call within its exact observed input window.'};$cost=$cost[0]
    foreach($kind in @('cost-before','cost-after','actor-cost-before','actor-cost-after')) {
        $events=@($Trace.events|Where-Object{$_.command-eq$cost.command-and$_.boundary-ceq$kind})
        if($events.Count-ne1-or$events[0].state.actor-cne$Actor-or$events[0].commandActor-cne$Actor-or$events[0].actionType-cne'Standard'){throw 'Native spending callback escaped exact actor/command custody.'}
    }
    $prior=@($Trace.events|Where-Object{$_.command-eq$cost.command-and$_.boundary-ceq'cost-before'})[0]
    if($cost.command-le0-or$cost.sequence-le$prior.sequence-or$cost.state.standard-le$prior.state.standard){throw 'Native action cost did not spend one actor remainder.'}
    foreach($field in @('move','swift','reactions','reactionCooldown')){if($cost.state.$field-ne$prior.state.$field){throw 'Native primary cost changed an unrelated resource.'}}
    $a=Get-KmcChunk7CombatActor $Attack.before $Actor;$b=Get-KmcChunk7CombatActor $Attack.after $Actor
    if($b.standard-le$a.standard){throw 'Primary result lacks native spent remainder.'}
    Assert-KmcChunk7SameCombatBudget $Attack.before $Attack.after @($Ids|Where-Object{$_-cne$Actor})
    return [long]$cost.command
}
function Assert-KmcChunk7CombatEvidence($Request,$Artifact,[string]$Status) {
    Assert-KmcChunk7ArtifactEnvelope $Request $Artifact 'chunk7-combat-evidence'
    if(-not(Test-KmcChunk7CombatScenario (Get-KmcChunk7SelectedScenario $Request))){throw 'Native combat evidence has a foreign scenario.'}
    if($Status-ceq'PASS'-and$Artifact.status-cne'PASS'){throw 'PASS result requires a PASS native combat artifact.'}
    if($Artifact.status-cne'PASS'){return}
    $o=$Artifact.observations;$ids=@($o.actorIds)
    $owned=Test-KmcChunk7OwnedPairsScenario $Request.scenario
    if($owned){Assert-KmcChunk7OwnedActionFixture $Request $o}else{
    if($o.scenario-cne$Request.scenario-or$o.workingSha256-cne$Request.fixture.working.sha256-or
       $o.workingSha256-cne'5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6'-or$ids.Count-ne4-or@($ids|Select-Object -Unique).Count-ne4-or
       $ids[0]-cne'b6628a77-4962-47a4-a17c-88d9836fc9d5'-or$ids[1]-cne'd79a4f6c-b74e-4868-95bd-533899131acb'-or$ids[2]-cne'd17c8fd0-6627-40c4-bf9a-024cf001c1f7'-or
       $o.targetGroupId-cne('KMC.RuntimeHostile.'+$Request.runId+':two-pair-target')-or$o.targetId-cin$ids){throw 'Exact native combat fixture identity differs.'}
        }
    [Guid]$id=[Guid]::Empty;if(-not[Guid]::TryParse($ids[3],[ref]$id)-or-not[Guid]::TryParse($o.targetId,[ref]$id)){throw 'Native Horse or diagnostic target ID invalid.'}
    Assert-KmcChunk7Boolean $o.fullRealtimeCombatProof $false;Assert-KmcChunk7Boolean $o.naturalUnconsciousStimulusProven $false
    if(-not$owned){if($o.acquisition.nativeRangerCommitCount-ne4-or$o.acquisition.horseFactRankAtCommit-ne1-or$o.acquisition.horsePresentAtNativeCommit-ne$true){throw 'Native Horse acquisition receipt incomplete.'}}
    Assert-KmcChunk7CheckpointTrace $o.allocationTrace $Request
    if(@($o.nativeCompletion.errors).Count-ne0-or$o.nativeCompletion.traceComplete-ne$true-or@($o.nativeRounds.errors).Count-ne0-or
       $o.nativeRounds.token-cne'06000BD3'-or$o.nativeRounds.moduleMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'){throw 'Exact native turn/round observation is incomplete.'}
    if((@($o.nativeCompletion.observedActorIds|Sort-Object -CaseSensitive)-join'|')-cne(@($ids|Sort-Object -CaseSensitive)-join'|')){throw 'Native completion observer did not bind all four exact actors.'}
    $order=@($o.nativeInitialOrder);if(@($order|Select-Object -Unique).Count-ne$order.Count-or$o.targetId-cnotin$order){throw 'Unrelated native target/initiative roster absent.'}
    $riderFirst=(Get-KmcChunk7SelectedScenario $Request)-ceq'chunk7-two-pair-combat-rider-first'
    for($i=0;$i-lt2;$i++) {
        $r=[Array]::IndexOf($order,$ids[2*$i]);$m=[Array]::IndexOf($order,$ids[2*$i+1])
        if($r-lt0-or$m-lt0-or(($r-lt$m)-ne$riderFirst)){throw 'Actual native initiative order does not match selected arrangement.'}
    }
    $commands=[Collections.Generic.HashSet[long]]::new()
    # Owner-seed-derived combat restored both pairs mounted: only the R1 native Dismount is an input.
    $derived=$owned-and(Test-KmcChunk7OwnedPairsSeedDerivedCombat $Request)
    if($derived){Assert-KmcChunk7OwnerSeedDerivedCombatStart $Request $o}
    $labels=@(if($derived){'r1-native-dismount-a'}else{'mount-a','mount-b','r1-native-dismount-a'});$relationshipInputs=@($o.inputs|Where-Object{$_.label-cin@('mount-a','mount-b','r1-native-dismount-a')})
    if($relationshipInputs.Count-ne$labels.Count-or(@($relationshipInputs|ForEach-Object label)-join'|')-cne($labels-join'|')){throw 'Exact native mount/dismount inputs are missing.'}
    for($i=0;$i-lt$labels.Count;$i++){
        $input=$relationshipInputs[$i];$slot=if($derived){2}else{$i};$caster=if($slot-eq1){$ids[2]}else{$ids[0]};$target=if($slot-eq0){$ids[1]}elseif($slot-eq1){$ids[3]}else{$ids[0]}
        $ability=if($slot-eq2){'3af2b81f4d72bbb30501fa730fcdf36e'}else{'f053faad986631688defa003cd7bda0e'}
        if($input.casterId-cne$caster-or$input.targetId-cne$target-or$input.abilityGuid-cne$ability-or$input.nativeCastDelta-ne1-or$input.nativeRefusalDelta-ne0-or
           [string]::IsNullOrEmpty($input.control)-or$input.generationAtInit-lt0-or$input.selectionAfterInput-ceq$caster){throw 'Native relationship input escaped captured caster/target/generation.'}
        Assert-KmcChunk7Boolean $input.clicked $true;Assert-KmcChunk7Boolean $input.terminal $true;Assert-KmcChunk7Boolean $input.processTerminal $true
    }
    if(@($o.activations).Count-ne2){throw 'Both full pair activations are required.'}
    for($i=0;$i-lt2;$i++) {
        $activation=@($o.activations|Where-Object pair -EQ $i);if($activation.Count-ne1){throw 'Full pair activation is missing or duplicated.'};$activation=$activation[0]
        $rider=$ids[2*$i];$mount=$ids[2*$i+1];$pairIds=@($rider,$mount)
        if($activation.sequence-le0-or$activation.before.currentActor-cne$rider){throw 'Mounted principal/grant identity differs.'}
        $active=@($activation.before.owners|Where-Object state -CEQ 'Active');if($active.Count-ne2-or@($active.id|Select-Object -Unique).Count-ne2){throw 'One shared world did not retain both independent pair owners.'}
        $move=$activation.movement;Assert-KmcChunk7Boolean $move.clicked $true;Assert-KmcChunk7Boolean $move.terminal $true
        if($move.selectedId-cne$rider-or$move.executorId-cne$mount-or$move.command-le0){throw 'Mounted transport changed actor ownership.'}
        $before=Get-KmcChunk7CombatActor $move.before $mount;$after=Get-KmcChunk7CombatActor $move.after $mount
        $distance=0.0;foreach($axis in @('x','y','z')){if(-not(Test-KmcChunk7FiniteNumber $before.position.$axis)-or-not(Test-KmcChunk7FiniteNumber $after.position.$axis)){throw 'Native movement positions must be finite.'};$distance+=[Math]::Pow([double]$after.position.$axis-[double]$before.position.$axis,2)}
        if([Math]::Sqrt($distance)-le0.02-or[Math]::Abs([Math]::Sqrt($distance)-$move.distance)-gt0.0001-or$after.move-le$before.move){throw 'Native transport did not move and spend mount movement.'}
        Assert-KmcChunk7SameCombatBudget $move.before $move.after @($ids|Where-Object{$_-cne$mount})
        foreach($actor in $pairIds) {
            $attacks=@($o.inputs|Where-Object{$_.PSObject.Properties['outcome']-and$_.outcome.actorId-ceq$actor-and$_.pair-eq$i-and$_.label-cin@('rider-primary','mount-primary')})
            if($attacks.Count-ne1){throw 'Pair is missing a native actor primary action.'}
            $command=Assert-KmcChunk7CombatAttack $attacks[0] $o.allocationTrace $actor $rider $o.targetId $ids
            if(-not$commands.Add($command)){throw 'Two primary outcomes reused a native action cost.'}
            $end=@($o.nativeCompletion.calls|Where-Object{$_.kind-ceq'end'-and$_.complete-eq$true-and$_.before.actorId-ceq$actor-and$_.before.frame-ge$activation.before.frame-and$_.after.frame-le$activation.afterEnd.frame})
            if($end.Count-ne1-or(Get-KmcChunk7CombatActor $activation.afterEnd $actor).standard-lt5.9){throw 'Each native actor context must complete exactly once with full end expenditure.'}
        }
        $charge=$activation.tbChargeRefusal
        Assert-KmcChunk7Boolean $charge.featureEnabled $true;Assert-KmcChunk7Boolean $charge.available $false
        if($charge.castDelta-ne0-or[string]::IsNullOrWhiteSpace($charge.reason)){throw 'Unsupported TB Charge admitted an action or has no reason.'}
        Assert-KmcChunk7SameCombatBudget $charge.before $charge.after $ids
        $refresh=@($o.snapshots|Where-Object label -CEQ ('pair-'+$i+'-native-refresh'));if($refresh.Count-ne1){throw 'Pair native renewal observation missing.'}
        foreach($actor in $pairIds){$before=Get-KmcChunk7CombatActor $activation.before $actor;$after=Get-KmcChunk7CombatActor $refresh[0] $actor
            if($after.native.grantSequence-ne$before.native.grantSequence+1-or$after.standard-ne0-or$after.move-ne0){throw 'Pair renewal did not grant each actor exactly once through native preparation.'}}
    }
    $r1=$o.r1
    if($r1.stimulus-cne'diagnostic cleanup callback at actual native StartRound return; no health/round/resource mutation'-or
       $r1.callbackRound-ne$r1.splitRound+1-or$r1.releaseBefore-ne$r1.splitRound-or$r1.releaseAfter-ne$r1.splitRound-or$r1.independentRound-ne$r1.splitRound+1-or
       $r1.independentMountTurn.actor-cne$ids[1]-or$r1.independentMountTurn.grantSequence-ne(Get-KmcChunk7CombatActor $r1.afterDismount $ids[1]).native.grantSequence+1){throw 'Detached cleanup restarted the original release round or delayed the native independent mount.'}
    Assert-KmcChunk7SameCombatBudget $r1.duplicateBefore $r1.duplicateAfter $ids
    $priorB=@($r1.duplicateBefore.owners|Where-Object{Test-KmcChunk7OwnerActorMatch $_.riderId $ids[2]});$afterB=@($r1.duplicateAfter.owners|Where-Object{Test-KmcChunk7OwnerActorMatch $_.riderId $ids[2]});
    if($priorB.Count-ne1-or$afterB.Count-ne1-or(ConvertTo-KmcChunk7Canonical $priorB[0])-cne(ConvertTo-KmcChunk7Canonical $afterB[0])){throw 'Duplicate cleanup changed the neighbouring owner.'}
    if(@($o.nativeRounds.events|Where-Object round -EQ $r1.callbackRound).Count-ne1){throw 'Duplicate cleanup lacks the actual native N+1 round event.'}
    $pending=$o.pendingSurvivor;$state=$pending.participation
    if($pending.stimulus-cne'UnitEntityData.LeaveCombat then native RemoveUnit if still registered'-or$state.Activation-ne$null-or$state.RiderId-ne$null-or$state.MountId-ne$null-or
       $state.PendingSplitId-cne$ids[3]-or$state.PendingSplitRound-ne$pending.after.round-or$state.Boundary-ne$null-or$state.Partner-ne$null-or$state.BoundaryIsCurrent-ne$false){throw 'Native removal lost mounted pending-only participation.'}
    $attack=@($o.inputs|Where-Object label -CEQ 'pending-survivor-mount-expenditure');if($attack.Count-ne1){throw 'Native pending survivor lacks preceding mount expenditure.'}
    $command=Assert-KmcChunk7CombatAttack $attack[0] $o.allocationTrace $ids[3] $ids[2] $o.targetId $ids;if(-not$commands.Add($command)){throw 'Pending survivor reused prior expenditure.'}
    Assert-KmcChunk7SameCombatBudget $pending.before $pending.after @($ids[3]);if((Get-KmcChunk7CombatActor $pending.after $ids[3]).standard-le0){throw 'Pending mount lost spent remainder.'}
    $fault=$o.localFailure;if($fault.failureCount-ne1-or$fault.injectedPair-cne'b'){throw 'Selected cleanup fault was not isolated.'};Assert-KmcChunk7Boolean $fault.healthyNeighbourVisited $true
    Assert-KmcChunk7SameCombatBudget $fault.before $fault.held @($ids[1])
    $held=@($fault.held.owners|Where-Object{Test-KmcChunk7OwnerActorMatch $_.riderId $ids[2]});if($held.Count-ne1-or$held[0].retainsObligations-ne$true){throw 'Failed cleanup lost exact pair B custody.'}
    if($owned){foreach($field in @('pairResidueAbsent','nativeCombatEnded','initiativeInputsRestored','firstPairAiRestored','controlsSettled','inheritedPetsPreserved')){Assert-KmcChunk7Boolean $o.cleanup.$field $true};Assert-KmcChunk7Boolean $o.cleanup.ownedHorseRemoved $false;Assert-KmcChunk7Boolean $o.cleanup.nativeProgressionRequiresExternalWorkingRestoration $false}else{foreach($field in @('pairResidueAbsent','ownedHorseRemoved','nativeCombatEnded','initiativeInputsRestored','firstPairAiRestored','controlsSettled','nativeProgressionRequiresExternalWorkingRestoration')){Assert-KmcChunk7Boolean $o.cleanup.$field $true}}
    foreach($field in @('nativeSaveWrittenByThisDriver','delegatedPersistenceCleanup')){Assert-KmcChunk7Boolean $o.cleanup.$field $false}
}
