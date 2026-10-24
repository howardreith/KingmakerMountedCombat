# Closed, bounded native two-pair combat evidence. The shared trace validator is
# defined by Chunk7CombatPersistenceEvidence; neither reader performs mutations.
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Chunk7RegisteredDisableEvidence.ps1')
function Test-KmcChunk7CombatScenario([string]$Scenario) { $Scenario-cin@('chunk7-two-pair-combat-rider-first','chunk7-two-pair-combat-mount-first') }
function Get-KmcChunk7CombatRows([string]$Scenario) {
    if(-not(Test-KmcChunk7CombatScenario $Scenario)){throw 'Unknown native combat scenario.'}
    @($Scenario,'C7-combat-fixture','C7-combat-native-mounts','C7-combat-restored-mounts','C7-combat-native-orders','C7-combat-pair-a-full-activation',
      'C7-combat-pair-b-full-activation','C7-combat-tb-charge-refusal','C7-combat-refresh','C7-combat-r1-round-release','C7-combat-first-action-mount',
      'C7-combat-native-pending-survivor','C7-combat-local-cleanup-failure','C7-combat-permanent-horse-removal-refusal','C7-combat-registered-disable-local-failure','C7-combat-attack-modes-mounted-off','C7-combat-attack-modes-mounted-on','C7-combat-attack-modes-unmounted-control','C7-combat-cleanup','C7-combat-runtime','C7-combat-interrupted')
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
       $Attack.nativeCastDelta-ne1-or$Attack.nativeRefusalDelta-ne0-or$Attack.command-eq0-or$Attack.outcome.actorId-cne$Actor-or
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
    if($cost.command-eq0-or$cost.sequence-le$prior.sequence-or$cost.state.standard-le$prior.state.standard){throw 'Native action cost did not spend one actor remainder.'}
    foreach($field in @('move','swift','reactions','reactionCooldown')){if($cost.state.$field-ne$prior.state.$field){throw 'Native primary cost changed an unrelated resource.'}}
    $a=Get-KmcChunk7CombatActor $Attack.before $Actor;$b=Get-KmcChunk7CombatActor $Attack.after $Actor
    if($b.standard-le$a.standard){throw 'Primary result lacks native spent remainder.'}
    # Ids are rider A, mount A, rider B, mount B. The other pair is unrelated. The attacker's own partner
    # may only spend movement, and only when the trace shows it executing a native approach UnitMoveTo
    # inside the attack window (a melee rider carried into reach by its mount).
    $index=[Array]::IndexOf($Ids,$Actor);if($index-lt0){throw 'Primary actor is not a pair role.'};$partner=$Ids[$index-bxor1]
    Assert-KmcChunk7SameCombatBudget $Attack.before $Attack.after @($Ids|Where-Object{$_-cne$Actor-and$_-cne$partner})
    $p=Get-KmcChunk7CombatActor $Attack.before $partner;$q=Get-KmcChunk7CombatActor $Attack.after $partner
    foreach($field in @('standard','swift','reactions','reactionCooldown')){if($p.$field-ne$q.$field){throw 'The attacker''s partner spent a non-movement resource during the primary.'}}
    if($q.move-lt$p.move){throw 'The attacker''s partner movement went backwards during the primary.'}
    if([double]$q.move-gt[double]$p.move+6.0){throw 'The attacker''s partner spent more than one native move budget during the primary.'}
    if($q.move-gt$p.move){
        $approach=@($Trace.events|Where-Object{$_.boundary-ceq'admission-after'-and$_.commandActor-ceq$partner-and$_.commandType-ceq'Kingmaker.UnitLogic.Commands.UnitMoveTo'-and
            $_.frame-ge$Attack.before.frame-and$_.frame-le$Attack.after.frame})
        if($approach.Count-ne1){throw 'The attacker''s partner spent movement without one native approach move in the primary window.'}}
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
    if($owned-and(Test-KmcChunk7OwnedPairsRegisteredDisable $Request)){Assert-KmcChunk7RegisteredDisableCombat $Request $o;return}
    if($owned-and(Test-KmcChunk7OwnedPairsAttackModes $Request)){Assert-KmcChunk7AttackModesCombat $Request $o;return}
    if($null-ne$o.PSObject.Properties['attackModes']){throw 'Attack modes evidence belongs only to its closed owner-seed selection.'}
    foreach($field in @('registeredDisable','removalRefusal')){if($null-ne$o.PSObject.Properties[$field]){throw 'Registered disable/removal evidence belongs only to its closed owner-seed selection.'}}
    $commands=[Collections.Generic.HashSet[long]]::new()
    # Owner-seed-derived combat restored both pairs mounted: only the R1 native Dismount is an input.
    $derived=$owned-and(Test-KmcChunk7OwnedPairsSeedDerivedCombat $Request)
    if($derived){Assert-KmcChunk7OwnerSeedDerivedCombatStart $Request $o;Assert-KmcChunk7OwnerSeedFirstActionMount $Request $o}elseif($null-ne$o.PSObject.Properties['firstActionMount']){throw 'Only owner-seed-derived TB records a first-action Mount.'}
    # Owner-seed-derived combat starts mounted: its relationship inputs are rider A's first-action Dismount,
    # then (Directive 4 R3) rider A's first-action combat Mount on a later prepared turn. Slots: 0 Mount A, 1 Mount B, 2 Dismount A.
    $labels=@(if($derived){'r1-native-dismount-a','first-action-mount-a'}else{'mount-a','mount-b','r1-native-dismount-a'});$slots=@(if($derived){2,0}else{0,1,2})
    $relationshipInputs=@($o.inputs|Where-Object{$_.label-cin@('mount-a','mount-b','r1-native-dismount-a','first-action-mount-a')})
    if($relationshipInputs.Count-ne$labels.Count-or(@($relationshipInputs|ForEach-Object label)-join'|')-cne($labels-join'|')){throw 'Exact native mount/dismount inputs are missing.'}
    for($i=0;$i-lt$labels.Count;$i++){
        $input=$relationshipInputs[$i];$slot=$slots[$i];$caster=if($slot-eq1){$ids[2]}else{$ids[0]};$target=if($slot-eq0){$ids[1]}elseif($slot-eq1){$ids[3]}else{$ids[0]}
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
        if($move.selectedId-cne$rider-or$move.executorId-cne$mount-or$move.command-eq0){throw 'Mounted transport changed actor ownership.'}
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
    # Paired rider removal forfeits the live paired activation through native ForceToEnd(setCooldowns: true),
    # nested on the mount's private turn (tb-pending-move-diagnosis01). Native writes max(before, 6/3/6); KMC
    # never lowers or synthesizes. Pair A stays untouched; the mount's reactions are untouched.
    Assert-KmcChunk7SameCombatBudget $pending.before $pending.after @($ids[0],$ids[1])
    $m=Get-KmcChunk7CombatActor $pending.before $ids[3];$n=Get-KmcChunk7CombatActor $pending.after $ids[3]
    foreach($pairing in @(@('standard',6.0),@('move',3.0),@('swift',6.0))){if([Math]::Abs([double]$n.($pairing[0])-[Math]::Max([double]$m.($pairing[0]),$pairing[1]))-gt0.001){throw 'Pending mount differs from the native forfeit outcome.'}}
    if($m.reactions-ne$n.reactions-or$m.reactionCooldown-ne$n.reactionCooldown){throw 'Pending mount reaction changed at rider removal.'};Assert-KmcChunk7Boolean $n.prepared $true
    # Bounded to the pending row's own frames, so a later cleanup-time removal of rider B is not counted.
    $removal=@($o.allocationTrace.events|Where-Object{$_.state.actor-ceq$ids[2]-and$_.boundary-cin@('remove-unit-before','remove-unit-after')-and$_.frame-ge$pending.before.frame-and$_.frame-le$pending.after.frame}|Sort-Object sequence)
    if($removal.Count-ne2-or$removal[0].boundary-cne'remove-unit-before'-or$removal[1].boundary-cne'remove-unit-after'){throw 'Native rider removal window is missing or repeated.'}
    $window=@($o.allocationTrace.events|Where-Object{$_.sequence-gt$removal[0].sequence-and$_.sequence-lt$removal[1].sequence}|Sort-Object sequence)
    $forfeit=@($window|Where-Object{$_.boundary-cin@('completion-force-to-end-before','completion-force-to-end-after')})
    if($forfeit.Count-ne4-or@($forfeit|Where-Object{"$($_.detail)"-cne'setCooldowns=True'}).Count-ne0-or
       (@($forfeit|ForEach-Object{$_.boundary.Substring(24)+':'+$_.state.actor})-join'|')-cne('before:'+$ids[2]+'|before:'+$ids[3]+'|after:'+$ids[3]+'|after:'+$ids[2])){throw 'Rider removal did not nest exactly one native mount forfeit inside the rider forfeit.'}
    if(@($window|Where-Object{$_.state.actor-ceq$ids[3]-and$_.boundary-cin@('cost-before','cost-after','actor-cost-before','actor-cost-after','admission-before','admission-after')}).Count-ne0){throw 'Rider removal charged or admitted work on the pending mount.'}
    foreach($actor in @($ids[2],$ids[3])){if(@($window|Where-Object{$_.state.actor-ceq$actor-and$_.boundary-ceq'turn-end-after'}).Count-ne1){throw 'Rider removal did not end each forfeited native turn exactly once.'}}
    $fault=$o.localFailure;if($fault.failureCount-ne1-or$fault.injectedPair-cne'b'){throw 'Selected cleanup fault was not isolated.'};Assert-KmcChunk7Boolean $fault.healthyNeighbourVisited $true
    Assert-KmcChunk7SameCombatBudget $fault.before $fault.held @($ids[1])
    $held=@($fault.held.owners|Where-Object{Test-KmcChunk7OwnerActorMatch $_.riderId $ids[2]});if($held.Count-ne1-or$held[0].retainsObligations-ne$true){throw 'Failed cleanup lost exact pair B custody.'}
    if($owned){foreach($field in @('pairResidueAbsent','nativeCombatEnded','initiativeInputsRestored','firstPairAiRestored','controlsSettled','inheritedPetsPreserved')){Assert-KmcChunk7Boolean $o.cleanup.$field $true};Assert-KmcChunk7Boolean $o.cleanup.ownedHorseRemoved $false;Assert-KmcChunk7Boolean $o.cleanup.nativeProgressionRequiresExternalWorkingRestoration $false}else{foreach($field in @('pairResidueAbsent','ownedHorseRemoved','nativeCombatEnded','initiativeInputsRestored','firstPairAiRestored','controlsSettled','nativeProgressionRequiresExternalWorkingRestoration')){Assert-KmcChunk7Boolean $o.cleanup.$field $true}}
    foreach($field in @('nativeSaveWrittenByThisDriver','delegatedPersistenceCleanup')){Assert-KmcChunk7Boolean $o.cleanup.$field $false}
}

# ---- 7E X1 C03 attack modes (owner-seed-attack-modes) --------------------------------------------
# Rider A's native Rapid Shot off/on mounted full attacks and an unmounted control with it on. Every
# value is recomputed from the recorded native rule and allocation observations.
function Get-KmcChunk7AttackModeRolls($Measurement){@(@($Measurement.rules)|Where-Object{$_.kind-ceq'roll'-and$_.opportunity-eq$false})}
function Get-KmcChunk7AttackModeInner($Measurement){@(Get-KmcChunk7AttackModeRolls $Measurement|ForEach-Object{[long]$_.nativeCalculation.innerResult})}
function Get-KmcChunk7AttackModeRapidBonus($Measurement){
 $values=@(Get-KmcChunk7AttackModeRolls $Measurement|ForEach-Object{@($_.bonuses)|Where-Object{$null-ne$_-and$_.source-ceq'0f310c1e709e15e4fa693db15a4baeb4'}|ForEach-Object{[long]$_.bonus}}|Select-Object -Unique)
 if($values.Count-gt1){throw 'Rapid Shot bonus sources are not uniform within one full attack.'}
 if($values.Count-eq0){0}else{[long]$values[0]}
}
function Assert-KmcChunk7AttackModeMeasurement($m,[string]$Label,[bool]$Mounted,[bool]$Rapid,[long]$Round,$o,$roles,$modes){
 if($m.label-cne$Label-or$m.mounted-isnot[bool]-or$m.mounted-ne$Mounted-or$m.round-ne$Round-or$m.rapid.isOn-ne$Rapid-or$m.rapidAfter.isOn-ne$Rapid){throw ('Attack mode measurement '+$Label+' differs in mode, round or toggle.')}
 Assert-KmcChunk7Boolean $m.clicked $true
 $c=$m.command;if($c.executor-cne$roles[0]-or$c.target-cne$o.targetId-or$c.fullAttack-ne$true-or$c.result-cnotin@('Success','Interrupt')-or-not(Test-KmcExactJsonInteger $c.completedAttacks)){throw ('Attack mode '+$Label+' is not one native full attack by rider A on the target.')}
 $plan=@($c.plan);$ranged=@($plan|Where-Object{$_.ranged-eq$true-and$_.owner-ceq$roles[0]});$rangedCount=$ranged.Count
 for($i=0;$i-lt$plan.Count;$i++){if($plan[$i].index-ne$i-or$plan[$i].ranged-isnot[bool]-or$plan[$i].natural-isnot[bool]){throw 'Attack mode plan entries are not an ordered native plan.'}
  if($i-lt$rangedCount){if($plan[$i].ranged-ne$true-or$plan[$i].owner-cne$roles[0]-or$plan[$i].weapon-cne$modes.weapon){throw 'Attack mode plan does not lead with rider A''s own ranged weapon.'}}elseif($plan[$i].ranged-eq$true){throw 'Attack mode plan holds a ranged entry after its tail.'}}
 if($rangedCount-lt1-or$c.completedAttacks-lt$rangedCount){throw ('Attack mode '+$Label+' did not complete its ranged plan.')}
 if($c.result-ceq'Interrupt'-and$c.completedAttacks-ge$plan.Count){throw 'Attack mode interrupt claims a completed plan.'}
 $events=@($m.rules);$seq=[long]$m.ruleCountBefore
 foreach($e in $events){$seq++;if($e.sequence-ne$seq-or$e.target-cne$o.targetId-or$e.actor-cne$roles[0]){throw ('Attack mode '+$Label+' rule window is not rider A''s consecutive own attacks on the target.')}}
 $rolls=@(Get-KmcChunk7AttackModeRolls $m);$attacks=@($events|Where-Object kind -CEQ 'attack')
 if($rolls.Count-ne$rangedCount-or$attacks.Count-ne$rangedCount-or@($events|Where-Object{$_.kind-ceq'roll'-and$_.opportunity-ne$false}).Count-ne0){throw ('Attack mode '+$Label+' rolls differ from its ranged plan.')}
 foreach($r in $rolls){if($r.ranged-ne$true-or$r.weapon-cne$modes.weapon-or-not(Test-KmcExactJsonInteger $r.attackBonus)-or-not(Test-KmcExactJsonInteger $r.nativeCalculation.innerResult)-or$r.bonuses-isnot[Array]){throw 'Attack mode roll lacks its native weapon or bonus calculation.'}}
 foreach($a in $attacks){if($a.fullAttack-ne$true-or$a.opportunity-ne$false-or$a.weapon-cne$modes.weapon){throw 'Attack mode weapon attack is not a native full-attack swing of the ranged weapon.'}}
 $beforeRider=Get-KmcChunk7CombatActor $m.before $roles[0];$afterRider=Get-KmcChunk7CombatActor $m.after $roles[0]
 if($afterRider.standard-le$beforeRider.standard){throw ('Attack mode '+$Label+' did not spend rider A''s own native action.')}
 Assert-KmcChunk7SameCombatBudget $m.before $m.after @($roles[1],$roles[2],$roles[3])
 foreach($f in @('traceSequenceBefore','traceSequenceAfter','ruleCountBefore')){if(-not(Test-KmcExactJsonInteger $m.$f)-or$m.$f-lt0){throw 'Attack mode window bounds are invalid.'}}
 if($m.traceSequenceAfter-lt$m.traceSequenceBefore){throw 'Attack mode trace window runs backwards.'}
 $window=@($o.allocationTrace.events|Where-Object{$_.sequence-gt$m.traceSequenceBefore-and$_.sequence-le$m.traceSequenceAfter-and$_.boundary-ceq'cost-after'-and$null-ne$_.state})
 $riderCosts=@($window|Where-Object{$_.state.actor-ceq$roles[0]}).Count;$mountCosts=@($window|Where-Object{$_.state.actor-ceq$roles[1]}).Count
 if($riderCosts-lt1-or$mountCosts-ne0){throw ('Attack mode '+$Label+' has no native cost on rider A or spent the mount.')}
 [pscustomobject]@{rangedCount=$rangedCount;inner=@(Get-KmcChunk7AttackModeInner $m);rapid=(Get-KmcChunk7AttackModeRapidBonus $m);riderCosts=$riderCosts}
}
function Assert-KmcChunk7AttackModesCombat($Request,$Observations){
 if(-not(Test-KmcChunk7OwnedPairsAttackModes $Request)){throw 'Attack modes evidence requires its closed owner-seed selection.'}
 Assert-KmcChunk7OwnedPairsSelection $Request
 if((Get-KmcChunk7OwnedLoadCount $Request)-ne1-or(Get-KmcChunk7OwnedWriteCount $Request)-ne0){throw 'Attack modes retain one native load and zero writes.'}
 $o=$Observations;$roles=@(Get-KmcChunk7OwnedPairIds $Request)
 Assert-KmcChunk7OwnerSeedDerivedCombatStart $Request $o
 foreach($key in @('r1','firstActionMount','localFailure','registeredDisable','removalRefusal','pendingSurvivor','nativeLife')){if($null-ne$o.PSObject.Properties[$key]){throw 'Attack modes cannot carry another combat case''s evidence.'}}
 if(@($o.activations).Count-ne0){throw 'Attack modes record no primary pair activation.'}
 $inputs=@($o.inputs);if($inputs.Count-ne1-or$inputs[0].label-cne'attack-modes-native-dismount-a'){throw 'Attack modes admit exactly the one native Dismount input.'}
 $d=$inputs[0];if($d.casterId-cne$roles[0]-or$d.targetId-cne$roles[0]-or$d.abilityGuid-cne'3af2b81f4d72bbb30501fa730fcdf36e'-or$d.nativeCastDelta-ne1-or$d.nativeRefusalDelta-ne0){throw 'Attack modes Dismount escaped rider A''s own native input.'}
 Assert-KmcChunk7Boolean $d.clicked $true;Assert-KmcChunk7Boolean $d.terminal $true;Assert-KmcChunk7Boolean $d.processTerminal $true
 $modes=$o.attackModes
 if($modes.contract-cne'owner-seed-attack-modes-v1'-or$modes.riderId-cne$roles[0]-or$modes.mountId-cne$roles[1]-or$modes.rapidShotFeature-ne1-or$modes.rapidShotToggles-ne1-or
    $modes.weaponRanged-ne$true-or$modes.weapon-cnotmatch'^[0-9a-f]{32}$'-or-not(Test-KmcChunk7FiniteNumber $modes.weaponRangeMeters)-or$modes.weaponRangeMeters-le0){throw 'Attack modes lack rider A''s native Rapid Shot feat, toggle or ranged weapon.'}
 Assert-KmcChunk7Boolean $modes.complete $true
 if($modes.rapidOff.isOn-ne$false-or$modes.rapidOff.buff-ne$false-or$modes.rapidOff.inCombat-ne$false-or$modes.rapidOff.toggle-cne'90a77bfe25ec2e14caf8bd5cde9febf2'){throw 'Rapid Shot was not off out of combat before the encounter.'}
 $clicks=@($modes.toggleClicks);$labels=@($clicks|ForEach-Object{[string]$_.label})
 $expectedLabels=@(if($modes.rapidBefore.isOn-eq$true){'pre-combat-off'};'round-n-on')
 if(($labels-join'|')-cne($expectedLabels-join'|')){throw 'Rapid Shot toggle clicks differ from the native off/on inputs.'}
 foreach($k in $clicks){Assert-KmcChunk7Boolean $k.possible $true
  $on=$k.label-ceq'round-n-on';if($k.before.isOn-ne(-not$on)-or$k.after.isOn-ne$on){throw 'A Rapid Shot slot click did not switch the native toggle as declared.'}
  if(-not$on-and$k.before.inCombat-ne$false){throw 'The pre-combat Rapid Shot click happened in combat.'}}
 $ms=@($modes.measurements);if($ms.Count-ne3){throw 'Attack modes need exactly three native full attacks.'}
 if(-not(Test-KmcExactJsonInteger $modes.firstRound)){throw 'Attack modes lack their first native round.'};$n=[long]$modes.firstRound
 $off=Assert-KmcChunk7AttackModeMeasurement $ms[0] 'mounted-off' $true $false $n $o $roles $modes
 $on=Assert-KmcChunk7AttackModeMeasurement $ms[1] 'mounted-on' $true $true ($n+1) $o $roles $modes
 $control=Assert-KmcChunk7AttackModeMeasurement $ms[2] 'unmounted-on' $false $true ($n+3) $o $roles $modes
 if($off.rapid-ne0){throw 'A Rapid Shot bonus source applied while the native toggle was off.'}
 $adjusted=@($on.inner|ForEach-Object{$_-$on.rapid}|Sort-Object);$expected=@(@($off.inner)+@(($off.inner|Measure-Object -Maximum).Maximum)|Sort-Object)
 if($on.rapid-ge0-or$on.rangedCount-ne$off.rangedCount+1-or($adjusted-join',')-cne($expected-join',')){throw 'Native Rapid Shot did not add exactly one attack at the highest bonus with its own uniform bonus source.'}
 if($control.rangedCount-ne$on.rangedCount-or(@($control.inner)-join',')-cne(@($on.inner)-join',')-or$control.rapid-ne$on.rapid){throw 'Mounted native full attack differs from the unmounted control under the same Rapid Shot toggle.'}
 if($off.riderCosts-ne$on.riderCosts-or$on.riderCosts-ne$control.riderCosts){throw 'Native cost shape of the full attack differs between modes.'}
 foreach($w in @($modes.dismountBefore,$modes.dismountAfter)){if($null-eq$w-or$w.round-ne$n+2){throw 'Attack modes Dismount lies outside its native round.'}}
 foreach($field in @('pairResidueAbsent','nativeCombatEnded','initiativeInputsRestored','firstPairAiRestored','controlsSettled','inheritedPetsPreserved')){Assert-KmcChunk7Boolean $o.cleanup.$field $true}
 Assert-KmcChunk7Boolean $o.cleanup.ownedHorseRemoved $false;Assert-KmcChunk7Boolean $o.cleanup.nativeSaveWrittenByThisDriver $false;Assert-KmcChunk7Boolean $o.cleanup.delegatedPersistenceCleanup $false
}
