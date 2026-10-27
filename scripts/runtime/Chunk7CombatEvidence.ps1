# Closed, bounded native two-pair combat evidence. The shared trace validator is
# defined by Chunk7CombatPersistenceEvidence; neither reader performs mutations.
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Chunk7RegisteredDisableEvidence.ps1')
function Test-KmcChunk7CombatScenario([string]$Scenario) { $Scenario-cin@('chunk7-two-pair-combat-rider-first','chunk7-two-pair-combat-mount-first') }
function Get-KmcChunk7CombatRows([string]$Scenario) {
    if(-not(Test-KmcChunk7CombatScenario $Scenario)){throw 'Unknown native combat scenario.'}
    @($Scenario,'C7-combat-fixture','C7-combat-native-mounts','C7-combat-restored-mounts','C7-combat-native-orders','C7-combat-pair-a-full-activation',
      'C7-combat-pair-b-full-activation','C7-combat-tb-charge-refusal','C7-combat-refresh','C7-combat-r1-round-release','C7-combat-first-action-mount',
      'C7-combat-native-pending-survivor','C7-combat-local-cleanup-failure','C7-combat-permanent-horse-removal-refusal','C7-combat-registered-disable-local-failure','C7-combat-attack-modes-mounted-off','C7-combat-attack-modes-mounted-on','C7-combat-attack-modes-unmounted-control','C7-combat-reaction-disengage-opportunity','C7-combat-reaction-defensive-cast','C7-combat-staged-move-cast-move','C7-combat-staged-double-move-ranged','C7-combat-staged-ranged-approach','C7-combat-cleanup','C7-combat-runtime','C7-combat-interrupted')
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
# An actor that never spends inside a window: no cooldown grows, the reaction count is unchanged, and any
# decrease is bounded by the native game time between the two snapshots (TB cooldowns of every unit decay with
# time while another actor's command runs). A larger drop is a refund or a clear, not native decay.
function Assert-KmcChunk7NeverSpentCombatBudget($Before,$After,[string[]]$Ids) {
    foreach($w in @($Before,$After)){if(-not(Test-KmcExactJsonInteger $w.gameTicks)-or$w.gameTicks-lt0){throw 'Native window game time is missing.'}}
    $elapsed=([double]$After.gameTicks-[double]$Before.gameTicks)/10000000.0;if($elapsed-lt0){throw 'Native window game time runs backwards.'}
    foreach($id in $Ids){$a=Get-KmcChunk7CombatActor $Before $id;$b=Get-KmcChunk7CombatActor $After $id
        foreach($field in @('standard','move','swift','reactionCooldown')){if([double]$b.$field-gt[double]$a.$field+0.001){throw 'Unrelated native actor expenditure changed.'}
            if([double]$a.$field-[double]$b.$field-gt$elapsed*1.1+0.001){throw 'Unrelated native actor cooldown fell faster than native time (refunded).'}}
        if($b.reactions-ne$a.reactions){throw 'Unrelated native actor expenditure changed.'}}
}
# No native cost boundary of the given actors inside a trace window (sequence after Before, up to After).
function Assert-KmcChunk7NoCostBoundary($Trace,[long]$Before,[long]$After,[string[]]$Ids,[string]$Why){
    $hits=@($Trace.events|Where-Object{$_.sequence-gt$Before-and$_.sequence-le$After-and$_.boundary-cin@('cost-before','cost-after','actor-cost-before','actor-cost-after')-and$null-ne$_.state-and$_.state.actor-cin$Ids})
    if($hits.Count-ne0){throw ($Why+' has a native cost boundary of an actor that must not spend.')}
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
    if($owned-and(Test-KmcChunk7OwnedPairsCombatReaction $Request)){Assert-KmcChunk7ReactionCombat $Request $o;return}
    if($owned-and(Test-KmcChunk7OwnedPairsStaged $Request)){Assert-KmcChunk7StagedCombat $Request $o;return}
    if($null-ne$o.PSObject.Properties['attackModes']){throw 'Attack modes evidence belongs only to its closed owner-seed selection.'}
    if($null-ne$o.PSObject.Properties['reaction']){throw 'Reaction evidence belongs only to its closed owner-seed selection.'}
    if($null-ne$o.PSObject.Properties['staged']){throw 'Staged evidence belongs only to its closed owner-seed selection.'}
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
# Rider A's native Rapid Shot off/on mounted ordinary attacks and an unmounted control with it on. Every
# value is recomputed from the recorded native rule and allocation observations.
function Get-KmcChunk7AttackModeRolls($Measurement){@(@($Measurement.rules)|Where-Object{$_.kind-ceq'roll'-and$_.opportunity-eq$false})}
function Get-KmcChunk7AttackModeInner($Measurement){@(Get-KmcChunk7AttackModeRolls $Measurement|ForEach-Object{[long]$_.nativeCalculation.innerResult})}
function Get-KmcChunk7AttackModeRapidBonus($Measurement){
 $values=@(Get-KmcChunk7AttackModeRolls $Measurement|ForEach-Object{@($_.bonuses)|Where-Object{$null-ne$_-and$_.source-ceq'0f310c1e709e15e4fa693db15a4baeb4'}|ForEach-Object{[long]$_.bonus}}|Select-Object -Unique)
 if($values.Count-gt1){throw 'Rapid Shot bonus sources are not uniform within one full attack.'}
 if($values.Count-eq0){0}else{[long]$values[0]}
}
# Native UnitAttack.OnAction returns Success with the last planned attack and the command then waits for
# that attack's animation; UnitCommand.Interrupt(bool) overwrites the Success if it arrives meanwhile
# (b202 am unmounted control: Interrupt one frame after its second and last shot). Contract v2 records
# every Interrupt(bool) of the measured command with its prior terminal state and caller chain. A
# plan-complete Interrupt is admitted only as exactly one overwrite of an acted Success after the last planned
# attack, with no KingmakerMountedCombat frame on its whole (untruncated) stack and no queued-command overwrite
# (InterruptAsSoonAsPossible); this excludes a KMC caller on the stack, not every KMC state influence; a Success carries none; an
# interrupt before the plan completes (the natural melee tail) must name its one recorded cause.
function Assert-KmcChunk7AttackModeInterrupt($c,$plan,$attacks,[string]$Label,$modes){
 $planComplete=$c.completedAttacks-ge@($plan).Count;$recorded=$modes.contract-ceq'owner-seed-attack-modes-v2';$watch=$c.PSObject.Properties['interruptWatch']
 if($recorded-ne($null-ne$watch)){throw ('Attack mode '+$Label+' interrupt observation differs from its contract.')}
 if(-not$recorded){if($c.result-ceq'Interrupt'-and$planComplete){throw 'Attack mode interrupt claims a completed plan without its recorded native cause.'};return}
 $w=$watch.Value;Assert-KmcExactProperties $w @('watched','watchStart','interrupts','interruptAsSoonAsPossible','executorCanAct','targetInState','targetDead') 'attack mode interrupt watch'
 Assert-KmcChunk7Boolean $w.watched $true
 # The watch opens at the click or the first frame the command is observed; an effective native interrupt always
 # finishes the command, so an unfinished command at the watch start proves no interrupt preceded it.
 $start=$w.watchStart;if($null-eq$start){throw 'Attack mode interrupt watch lacks its start.'};Assert-KmcExactProperties $start @('atClick','frame','finished','result','acted') 'attack mode interrupt watch start'
 if($start.atClick-isnot[bool]-or-not(Test-KmcExactJsonInteger $start.frame)-or$start.acted-isnot[bool]-or$start.result-cnotin@('None','Fail','Interrupt','Success')){throw 'Attack mode interrupt watch start is missing or mistyped.'}
 Assert-KmcChunk7Boolean $start.finished $false
 foreach($f in @('interruptAsSoonAsPossible','executorCanAct','targetInState','targetDead')){if($w.$f-isnot[bool]){throw 'Attack mode interrupt watch state is missing or mistyped.'}}
 if($w.interrupts-isnot[Array]){throw 'Attack mode interrupt watch lacks its interrupt list.'}
 $rows=@($w.interrupts)
 foreach($row in $rows){Assert-KmcExactProperties $row @('frame','round','command','resultBefore','actedBefore','finishedBefore','interruptAsSoonAsPossible','timeSinceStart','attackIndex','plannedAttacks','executorCanAct','targetInState','targetDead','callers','callersTruncated') 'attack mode interrupt'
  if(-not(Test-KmcExactJsonInteger $row.frame)-or$row.frame-lt$start.frame-or$row.command-ne$c.identity-or$row.resultBefore-cnotin@('None','Fail','Interrupt','Success')-or$row.actedBefore-isnot[bool]-or$row.finishedBefore-isnot[bool]-or
     -not(Test-KmcExactJsonInteger $row.attackIndex)-or$row.plannedAttacks-ne@($plan).Count-or$row.callers-isnot[string]-or-not$row.callers.StartsWith('callers=')-or$row.callersTruncated-isnot[bool]-or$row.interruptAsSoonAsPossible-isnot[bool]){throw 'Attack mode interrupt record is not the measured native command.'}}
 $effective=@($rows|Where-Object{-not$_.finishedBefore-and$_.resultBefore-cne'Interrupt'})
 if($c.result-ceq'Success'){if($effective.Count-ne0){throw ('Attack mode '+$Label+' Success hides an effective native interrupt.')};return}
 if($effective.Count-ne1){throw ('Attack mode '+$Label+' interrupt lacks its one recorded native cause.')}
 if(-not$planComplete){return}
 $x=$effective[0];$lastFrame=[long](@($attacks|ForEach-Object{[long]$_.frame})|Measure-Object -Maximum).Maximum
 if($x.resultBefore-cne'Success'-or$x.actedBefore-ne$true-or$x.attackIndex-ne@($plan).Count-or$x.frame-lt$lastFrame-or$x.callersTruncated-or$x.interruptAsSoonAsPossible-or$x.callers.Contains('KingmakerMountedCombat')){throw 'Attack mode interrupt claims a completed plan.'}
}
function Assert-KmcChunk7AttackModeMeasurement($m,[string]$Label,[bool]$Mounted,[bool]$Rapid,[long]$Round,$o,$roles,$modes){
 if($m.rapid.buff-ne$Rapid-or$m.rapid.isRunning-ne$Rapid-or$m.rapid.inCombat-ne$true){throw ('Attack mode '+$Label+' Rapid Shot was not natively running exactly when on.')}
 if($m.label-cne$Label-or$m.mounted-isnot[bool]-or$m.mounted-ne$Mounted-or$m.round-ne$Round-or$m.rapid.isOn-ne$Rapid-or$m.rapidAfter.isOn-ne$Rapid){throw ('Attack mode measurement '+$Label+' differs in mode, round or toggle.')}
 Assert-KmcChunk7Boolean $m.clicked $true
 # Native TB offers its FullAttack cursor only for an estimate above one, so a one-attack routine is a native
 # single attack, mounted or not; Rapid Shot always adds one attack, so every Rapid Shot leg is a full attack.
 foreach($p in @('nativeEstimate','expectedFull','currentTurnEnabledFullAttack')){if($null-eq$m.PSObject.Properties[$p]){throw ('Attack mode '+$Label+' native full-attack estimate is missing or inconsistent.')}}
 if(-not(Test-KmcExactJsonInteger $m.nativeEstimate)-or$m.nativeEstimate-lt1-or$m.expectedFull-isnot[bool]-or$m.expectedFull-ne($m.nativeEstimate-gt1)-or($Rapid-and-not$m.expectedFull)){throw ('Attack mode '+$Label+' native full-attack estimate is missing or inconsistent.')}
 if($null-ne$m.currentTurnEnabledFullAttack-and($m.currentTurnEnabledFullAttack-isnot[bool]-or$m.currentTurnEnabledFullAttack-ne($m.nativeEstimate-gt1))){throw ('Attack mode '+$Label+' native cursor observation is mistyped or contradicts its native estimate.')}
 $full=[bool]$m.expectedFull
 $c=$m.command;if($c.executor-cne$roles[0]-or$c.target-cne$o.targetId-or$c.fullAttack-ne$full-or$c.singleAttack-ne$false-or$c.result-cnotin@('Success','Interrupt')-or-not(Test-KmcExactJsonInteger $c.completedAttacks)){throw ('Attack mode '+$Label+' is not one native '+$(if($full){'full'}else{'single'})+' attack by rider A on the target.')}
 $plan=@($c.plan);$ranged=@($plan|Where-Object{$_.ranged-eq$true-and$_.owner-ceq$roles[0]});$rangedCount=$ranged.Count
 if(($full-and$plan.Count-ne$m.nativeEstimate)-or(-not$full-and($plan.Count-ne1-or$rangedCount-ne1))){throw ('Attack mode '+$Label+' plan differs from its native estimate.')}
 for($i=0;$i-lt$plan.Count;$i++){if($plan[$i].index-ne$i-or$plan[$i].ranged-isnot[bool]-or$plan[$i].natural-isnot[bool]){throw 'Attack mode plan entries are not an ordered native plan.'}
  if($i-lt$rangedCount){if($plan[$i].ranged-ne$true-or$plan[$i].owner-cne$roles[0]-or$plan[$i].weapon-cne$modes.weapon){throw 'Attack mode plan does not lead with rider A''s own ranged weapon.'}}elseif($plan[$i].ranged-eq$true){throw 'Attack mode plan holds a ranged entry after its tail.'}}
 if($rangedCount-lt1-or$c.completedAttacks-lt$rangedCount){throw ('Attack mode '+$Label+' did not complete its ranged plan.')}
 $events=@($m.rules);$seq=[long]$m.ruleCountBefore
 foreach($e in $events){$seq++;if($e.sequence-ne$seq-or$e.target-cne$o.targetId-or$e.actor-cne$roles[0]){throw ('Attack mode '+$Label+' rule window is not rider A''s consecutive own attacks on the target.')}}
 $rolls=@(Get-KmcChunk7AttackModeRolls $m);$attacks=@($events|Where-Object kind -CEQ 'attack')
 if($rolls.Count-ne$rangedCount-or$attacks.Count-ne$rangedCount-or@($events|Where-Object{$_.kind-ceq'roll'-and$_.opportunity-ne$false}).Count-ne0){throw ('Attack mode '+$Label+' rolls differ from its ranged plan.')}
 foreach($r in $rolls){$sources=@(@($r.bonuses)|Where-Object{$null-ne$_-and$_.source-ceq'0f310c1e709e15e4fa693db15a4baeb4'}).Count;if($sources-ne$(if($Rapid){1}else{0})){throw ('Attack mode '+$Label+' roll does not carry exactly its Rapid Shot source.')}}
 foreach($r in $rolls){if($r.ranged-ne$true-or$r.weapon-cne$modes.weapon-or-not(Test-KmcExactJsonInteger $r.attackBonus)-or-not(Test-KmcExactJsonInteger $r.nativeCalculation.innerResult)-or$r.bonuses-isnot[Array]){throw 'Attack mode roll lacks its native weapon or bonus calculation.'}}
 foreach($a in $attacks){if($a.fullAttack-ne$full-or$a.opportunity-ne$false-or$a.weapon-cne$modes.weapon){throw 'Attack mode weapon attack is not the native swing of the ranged weapon in its declared mode.'}}
 Assert-KmcChunk7AttackModeInterrupt $c $plan $attacks $Label $modes
 $beforeRider=Get-KmcChunk7CombatActor $m.before $roles[0];$afterRider=Get-KmcChunk7CombatActor $m.after $roles[0]
 if($afterRider.standard-le$beforeRider.standard){throw ('Attack mode '+$Label+' did not spend rider A''s own native action.')}
 # Pair B never spends inside the window; its finished turns' cooldowns may only decay natively, as every
 # unit's TB cooldowns do while another actor's command runs. Mount A is compared exactly while it carries
 # rider A; in the unmounted control it is an independent unit whose finished paired turn (N+2) may still
 # decay, so it is compared as never spending, and its native cost calls stay refused below.
 Assert-KmcChunk7NeverSpentCombatBudget $m.before $m.after @($roles[2],$roles[3])
 if($Mounted){Assert-KmcChunk7SameCombatBudget $m.before $m.after @($roles[1])}else{Assert-KmcChunk7NeverSpentCombatBudget $m.before $m.after @($roles[1])}
 foreach($f in @('traceSequenceBefore','traceSequenceAfter','ruleCountBefore')){if(-not(Test-KmcExactJsonInteger $m.$f)-or$m.$f-lt0){throw 'Attack mode window bounds are invalid.'}}
 if($m.traceSequenceAfter-lt$m.traceSequenceBefore){throw 'Attack mode trace window runs backwards.'}
 $window=@($o.allocationTrace.events|Where-Object{$_.sequence-gt$m.traceSequenceBefore-and$_.sequence-le$m.traceSequenceAfter-and$_.boundary-ceq'cost-after'-and$null-ne$_.state})
 Assert-KmcChunk7NoCostBoundary $o.allocationTrace $m.traceSequenceBefore $m.traceSequenceAfter @($roles[2],$roles[3]) ('Attack mode '+$Label)
 $riderCosts=@($window|Where-Object{$_.state.actor-ceq$roles[0]}).Count;$mountCosts=@($window|Where-Object{$_.state.actor-ceq$roles[1]}).Count
 if($riderCosts-lt1-or$mountCosts-ne0){throw ('Attack mode '+$Label+' has no native cost on rider A or spent the mount.')}
 [pscustomobject]@{rangedCount=$rangedCount;inner=@(Get-KmcChunk7AttackModeInner $m);rapid=(Get-KmcChunk7AttackModeRapidBonus $m);riderCosts=$riderCosts}
}
# Every saved primary slot as the native body held it before the switch (b207 m5). Kingmaker 2.1.7b: the saved weapon is
# the slot's stored item (ItemSlot.MaybeItem); HandSlot.UpdateActive makes only the current set's slots Active and
# WeaponSlot.MaybeWeapon answers the empty-hand weapon for an inactive slot.
function Assert-KmcChunk7AttackModesMounted6SavedSets($M,$C){
 $s=$M.savedSets;if($null-eq$s){throw 'Mounted6 attack modes lack their saved hand-set record.'}
 Assert-KmcExactProperties $s @('count','current','polymorphed','emptyHandWeapon','sets') 'mounted6 saved hand sets'
 Assert-KmcOrdinaryInteger $s.count ([Math]::Max([long]$C.meleeHandSet,[long]$C.rangedHandSet)+1) 4
 if(-not(Test-KmcExactJsonInteger $s.current)-or$s.current-ne$M.before.handSet){throw 'Mounted6 saved hand sets do not record the hand set selected before the switch.'}
 Assert-KmcChunk7Boolean $s.polymorphed $false
 $guid={param($v)$null-eq$v-or($v-is[string]-and$v-cmatch'^[0-9a-f]{32}$')}
 if(-not(&$guid $s.emptyHandWeapon)){throw 'Mounted6 empty-hand weapon identity is mistyped.'}
 $rows=@($s.sets);if($rows.Count-ne$s.count){throw 'Mounted6 saved hand sets do not record every set.'}
 for($i=0;$i-lt$rows.Count;$i++){$r=$rows[$i]
  Assert-KmcExactProperties $r @('index','hasItem','active','disabled','item','weapon','weaponRanged','maybeWeapon') 'mounted6 saved hand set'
  if(-not(Test-KmcExactJsonInteger $r.index)-or$r.index-ne$i){throw 'Mounted6 saved hand set index differs.'}
  foreach($k in @('hasItem','active','disabled','weaponRanged')){if($r.$k-isnot[bool]){throw ('Mounted6 saved hand set flag is not boolean: '+$k)}}
  foreach($k in @('item','weapon','maybeWeapon')){if(-not(&$guid $r.$k)){throw ('Mounted6 saved hand set identity is mistyped: '+$k)}}
  if($r.hasItem-ne($null-ne$r.item)-or($null-ne$r.weapon-and$r.weapon-cne$r.item)-or($null-eq$r.weapon-and$r.weaponRanged)){throw 'Mounted6 saved hand set item clauses disagree.'}
  if($r.active-ne($i-eq$s.current-and-not$r.disabled)){throw 'Mounted6 saved hand set activity differs from the native current-set rule.'}
  if(-not$r.active-and$r.maybeWeapon-cne$s.emptyHandWeapon){throw 'Mounted6 inactive hand set does not answer the native empty-hand weapon.'}
  if($r.active-and$null-ne$r.weapon-and$r.maybeWeapon-cne$r.weapon){throw 'Mounted6 active hand set does not answer its stored weapon.'}}
 $ranged=$rows[[int]$C.rangedHandSet];if(-not$ranged.hasItem-or$ranged.item-cne$C.rangedWeapon-or$ranged.weapon-cne$C.rangedWeapon-or$ranged.weaponRanged-ne$true){throw 'Mounted6 saved ranged hand set does not hold the seed''s ranged weapon.'}
 if($M.switched){$melee=$rows[[int]$C.meleeHandSet];if(-not$melee.hasItem-or$melee.item-cne$C.meleeWeapon-or$melee.weapon-cne$C.meleeWeapon){throw 'Mounted6 saved longsword hand set does not hold the seed''s longsword.'}}
}
# Mounted6 lineage (C03 BAB variation): rider A's native BAB is at least 6, the seed's saved ranged hand set was
# selected out of combat (from its saved longsword set, or already active), and the mounted Rapid Shot off leg is a
# native full attack with an iterative second ranged attack. Every other seed records no mounted6 block.
function Assert-KmcChunk7AttackModesMounted6($Request,$Modes,$OffMeasurement,$Off){
 $m6seed=Test-KmcChunk7Mounted6Selection $Request.ownedPairs;$has=$null-ne$Modes.PSObject.Properties['mounted6']
 if(-not$m6seed){if($has){throw 'Only the mounted6 lineage records a BAB and hand-set selection block.'};return}
 if(-not$has){throw 'Mounted6 attack modes lack their BAB and hand-set selection record.'};$c=Get-KmcChunk7Mounted6Seed;$m=$Modes.mounted6
 Assert-KmcExactProperties $m @('baseAttackBonus','before','savedSets','switched','nativeApi','after') 'mounted6 attack modes selection'
 Assert-KmcOrdinaryInteger $m.baseAttackBonus $c.minimumBab 40;Assert-KmcBootstrapInventoryString $m.nativeApi 'Kingmaker.Items.UnitBody.CurrentHandEquipmentSetIndex (public setter, out of combat)'
 foreach($k in @('before','after')){Assert-KmcExactProperties $m.$k @('handSet','primaryBlueprint','firstWeapon','firstWeaponRanged','inCombat') ('mounted6 hand set '+$k);Assert-KmcChunk7Boolean $m.$k.inCombat $false}
 if($m.switched-isnot[bool]){throw 'Mounted6 hand-set switch flag is not boolean.'}
 if($m.switched){if($m.before.handSet-ne$c.meleeHandSet-or$m.before.primaryBlueprint-cne$c.meleeWeapon){throw 'Mounted6 hand-set switch did not start from the seed''s saved longsword set.'}}
 elseif($m.before.handSet-ne$c.rangedHandSet-or$m.before.firstWeapon-cne$c.rangedWeapon){throw 'Mounted6 attack modes skipped a needed hand-set selection.'}
 if($m.after.handSet-ne$c.rangedHandSet-or$m.after.primaryBlueprint-cne$c.rangedWeapon-or$m.after.firstWeapon-cne$c.rangedWeapon-or$m.after.firstWeaponRanged-ne$true-or$Modes.weapon-cne$c.rangedWeapon){throw 'Mounted6 attack modes did not use the seed''s saved ranged weapon.'}
 Assert-KmcChunk7AttackModesMounted6SavedSets $m $c
 if($OffMeasurement.expectedFull-ne$true-or$OffMeasurement.nativeEstimate-lt2-or$Off.rangedCount-lt2){throw 'Mounted Rapid Shot off attack at BAB >= 6 was not a native full attack with an iterative second ranged attack.'}
}
function Assert-KmcChunk7AttackModesCombat($Request,$Observations){
 if(-not(Test-KmcChunk7OwnedPairsAttackModes $Request)){throw 'Attack modes evidence requires its closed owner-seed selection.'}
 Assert-KmcChunk7OwnedPairsSelection $Request
 if((Get-KmcChunk7OwnedLoadCount $Request)-ne1-or(Get-KmcChunk7OwnedWriteCount $Request)-ne0){throw 'Attack modes retain one native load and zero writes.'}
 $o=$Observations;$roles=@(Get-KmcChunk7OwnedPairIds $Request)
 Assert-KmcChunk7OwnerSeedDerivedCombatStart $Request $o
 foreach($key in @('r1','firstActionMount','localFailure','registeredDisable','removalRefusal','pendingSurvivor','nativeLife','reaction','staged')){if($null-ne$o.PSObject.Properties[$key]){throw 'Attack modes cannot carry another combat case''s evidence.'}}
 if(@($o.activations).Count-ne0){throw 'Attack modes record no primary pair activation.'}
 $inputs=@($o.inputs);if($inputs.Count-ne1-or$inputs[0].label-cne'attack-modes-native-dismount-a'){throw 'Attack modes admit exactly the one native Dismount input.'}
 $d=$inputs[0];if($d.casterId-cne$roles[0]-or$d.targetId-cne$roles[0]-or$d.abilityGuid-cne'3af2b81f4d72bbb30501fa730fcdf36e'-or$d.nativeCastDelta-ne1-or$d.nativeRefusalDelta-ne0){throw 'Attack modes Dismount escaped rider A''s own native input.'}
 Assert-KmcChunk7Boolean $d.clicked $true;Assert-KmcChunk7Boolean $d.terminal $true;Assert-KmcChunk7Boolean $d.processTerminal $true
 $modes=$o.attackModes
 if($modes.contract-cnotin@('owner-seed-attack-modes-v1','owner-seed-attack-modes-v2')-or$modes.riderId-cne$roles[0]-or$modes.mountId-cne$roles[1]-or$modes.rapidShotFeature-ne1-or$modes.rapidShotToggles-ne1-or
    $modes.weaponRanged-ne$true-or$modes.weapon-cnotmatch'^[0-9a-f]{32}$'-or-not(Test-KmcChunk7FiniteNumber $modes.weaponRangeMeters)-or$modes.weaponRangeMeters-le0){throw 'Attack modes lack rider A''s native Rapid Shot feat, toggle or ranged weapon.'}
 Assert-KmcChunk7Boolean $modes.complete $true
 if($null-eq$modes.PSObject.Properties['rules']-or$modes.rules.dropped-ne0-or@($modes.rules.errors).Count-ne0){throw 'Attack modes rule observation dropped or failed.'}
 if($modes.rapidOff.isOn-ne$false-or$modes.rapidOff.buff-ne$false-or$modes.rapidOff.inCombat-ne$false-or$modes.rapidOff.toggle-cne'90a77bfe25ec2e14caf8bd5cde9febf2'){throw 'Rapid Shot was not off out of combat before the encounter.'}
 $clicks=@($modes.toggleClicks);$labels=@($clicks|ForEach-Object{[string]$_.label})
 $expectedLabels=@(if($modes.rapidBefore.isOn-eq$true){'pre-combat-off'};'round-n-on')
 if(($labels-join'|')-cne($expectedLabels-join'|')){throw 'Rapid Shot toggle clicks differ from the native off/on inputs.'}
 foreach($k in $clicks){Assert-KmcChunk7Boolean $k.possible $true
  $on=$k.label-ceq'round-n-on';if($k.before.isOn-ne(-not$on)-or$k.after.isOn-ne$on){throw 'A Rapid Shot slot click did not switch the native toggle as declared.'}
  if(-not$on-and$k.before.inCombat-ne$false){throw 'The pre-combat Rapid Shot click happened in combat.'}
  if($on-and($k.before.inCombat-ne$true-or$k.before.round-ne$modes.firstRound)){throw 'The Rapid Shot on click was not made in the first measured round.'}}
 $ms=@($modes.measurements);if($ms.Count-ne3){throw 'Attack modes need exactly three native ordinary attacks.'}
 if(-not(Test-KmcExactJsonInteger $modes.firstRound)){throw 'Attack modes lack their first native round.'};$n=[long]$modes.firstRound
 $off=Assert-KmcChunk7AttackModeMeasurement $ms[0] 'mounted-off' $true $false $n $o $roles $modes
 $on=Assert-KmcChunk7AttackModeMeasurement $ms[1] 'mounted-on' $true $true ($n+1) $o $roles $modes
 $control=Assert-KmcChunk7AttackModeMeasurement $ms[2] 'unmounted-on' $false $true ($n+3) $o $roles $modes
 if($off.rapid-ne0){throw 'A Rapid Shot bonus source applied while the native toggle was off.'}
 Assert-KmcChunk7AttackModesMounted6 $Request $modes $ms[0] $off
 $adjusted=@($on.inner|ForEach-Object{$_-$on.rapid}|Sort-Object);$expected=@(@($off.inner)+@(($off.inner|Measure-Object -Maximum).Maximum)|Sort-Object)
 if($on.rapid-ge0-or$on.rangedCount-ne$off.rangedCount+1-or($adjusted-join',')-cne($expected-join',')){throw 'Native Rapid Shot did not add exactly one attack at the highest bonus with its own uniform bonus source.'}
 if($control.rangedCount-ne$on.rangedCount-or(@($control.inner)-join',')-cne(@($on.inner)-join',')-or$control.rapid-ne$on.rapid){throw 'Mounted native full attack differs from the unmounted control under the same Rapid Shot toggle.'}
 if($off.riderCosts-ne$on.riderCosts-or$on.riderCosts-ne$control.riderCosts){throw 'Native cost shape of the full attack differs between modes.'}
 foreach($w in @($modes.dismountBefore,$modes.dismountAfter)){if($null-eq$w-or$w.round-ne$n+2){throw 'Attack modes Dismount lies outside its native round.'}}
 foreach($field in @('pairResidueAbsent','nativeCombatEnded','initiativeInputsRestored','firstPairAiRestored','controlsSettled','inheritedPetsPreserved')){Assert-KmcChunk7Boolean $o.cleanup.$field $true}
 Assert-KmcChunk7Boolean $o.cleanup.ownedHorseRemoved $false;Assert-KmcChunk7Boolean $o.cleanup.nativeSaveWrittenByThisDriver $false;Assert-KmcChunk7Boolean $o.cleanup.delegatedPersistenceCleanup $false
}

# ---- 7E X1 C11 reactions (owner-seed-combat-reaction) -------------------------------------------
# Pair A leaves the diagnostic target's threatened area by one ordinary native ground order on rider
# A's first prepared turn (after one native approach only if it was not threatened). The stock
# disengagement owns the target's single attack of opportunity; every value is read back from the
# recorded native rule, counter and allocation observations.
function Assert-KmcChunk7ReactionTargetState($State,[string]$TargetId){
 if($null-eq$State-or$State.id-cne$TargetId){throw 'Reaction target state is missing or names another unit.'}
 foreach($f in @('reactions','reactionsPerRound','round','frame')){if(-not(Test-KmcExactJsonInteger $State.$f)-or$State.$f-lt0){throw 'Reaction target counters are invalid.'}}
 foreach($f in @('standard','move','swift','mountDistance')){if(-not(Test-KmcChunk7FiniteNumber $State.$f)-or$State.$f-lt0){throw 'Reaction target resources or distance are invalid.'}}
 foreach($f in @('canAttackOfOpportunity','prone','proneRequested','conscious','commandsEmpty','engagesRider','engagesMount')){if($State.$f-isnot[bool]){throw 'Reaction target state flag is missing or mistyped.'}}
 if($State.prone-or$State.proneRequested-or-not$State.conscious){throw 'The diagnostic target was prone or unconscious inside the reaction window; a stand-up provocation cannot be told from disengagement.'}
}
function Assert-KmcChunk7ReactionLeg($Leg,[string]$Label,[long]$Round,$o,$roles){
 if($null-eq$Leg-or$Leg.label-cne$Label-or$Leg.round-ne$Round){throw ('Reaction '+$Label+' leg is missing or outside its native round.')}
 Assert-KmcChunk7Boolean $Leg.clicked $true;Assert-KmcChunk7Boolean $Leg.fiveFootStep $false;Assert-KmcChunk7Boolean $Leg.singleActionMove $false
 if($Leg.result-cnotin@('Success','Interrupt')-or-not(Test-KmcExactJsonInteger $Leg.command)-or$Leg.command-eq0-or-not(Test-KmcChunk7FiniteNumber $Leg.distance)-or$Leg.distance-le0.5){throw ('Reaction '+$Label+' is not one completed native ground move of mount A.')}
 if($Leg.turnActor-cne$roles[0]){throw ('Reaction '+$Label+' left rider A''s native turn.')}
 Assert-KmcChunk7ReactionTargetState $Leg.targetBefore $o.targetId;Assert-KmcChunk7ReactionTargetState $Leg.targetAfter $o.targetId
 $rb=Get-KmcChunk7CombatActor $Leg.before $roles[0];$ra=Get-KmcChunk7CombatActor $Leg.after $roles[0];$mb=Get-KmcChunk7CombatActor $Leg.before $roles[1];$ma=Get-KmcChunk7CombatActor $Leg.after $roles[1]
 foreach($f in @('standard','move','swift')){if($ra.$f-ne$rb.$f){throw 'Rider A spent its own action on the pair''s ground move.'}}
 if($ma.standard-ne$mb.standard-or$ma.swift-ne$mb.swift){throw 'Mount A spent a non-movement action on its ground move.'}
 if($ma.move-le$mb.move){throw 'The pair''s ground move was not charged to mount A''s native move, or was refunded.'}
 # Pair B never spends inside the window; a finished turn's cooldowns may only decay natively, bounded by native time.
 Assert-KmcChunk7NeverSpentCombatBudget $Leg.before $Leg.after @($roles[2],$roles[3])
 # Native TB cooldowns of every unit only decay while another actor's command runs: the target never spends.
 foreach($f in @('standard','move','swift')){if([double]$Leg.targetAfter.$f-gt[double]$Leg.targetBefore.$f+0.001){throw 'The diagnostic target spent an action inside the pair''s move.'}}
 $res=$Leg.residue;foreach($f in @('activeCommand','groundMovement')){Assert-KmcChunk7Boolean $res.$f $false}
 foreach($f in @('riderCommandsEmpty','mountCommandsEmpty','riderConscious','mountConscious')){Assert-KmcChunk7Boolean $res.$f $true}
 foreach($f in @('riderProne','mountProne')){Assert-KmcChunk7Boolean $res.$f $false}
 foreach($f in @('riderDamage','riderMaxHp','mountDamage','mountMaxHp')){if(-not(Test-KmcExactJsonInteger $res.$f)-or$res.$f-lt0){throw 'Reaction victim hit points are not recorded.'}}
 if($res.state-cne'Mounted'){throw 'Pair A did not stay mounted through the reaction.'}
 foreach($f in @('ruleCountBefore','traceSequenceBefore','traceSequenceAfter')){if(-not(Test-KmcExactJsonInteger $Leg.$f)-or$Leg.$f-lt0){throw 'Reaction window bounds are invalid.'}}
 if($Leg.traceSequenceAfter-lt$Leg.traceSequenceBefore){throw 'Reaction trace window runs backwards.'}
 # Only mount A's own move is charged: no native cost boundary of rider A or pair B inside the leg.
 Assert-KmcChunk7NoCostBoundary $o.allocationTrace $Leg.traceSequenceBefore $Leg.traceSequenceAfter @($roles[0],$roles[2],$roles[3]) ('Reaction '+$Label)
 $pb=if($null-ne$Leg.PSObject.Properties['pairBefore']){$Leg.pairBefore}else{$null};if($null-eq$pb){throw 'Reaction pair state before the leg is not recorded.'}
 foreach($f in @('riderDamage','mountDamage')){if(-not(Test-KmcExactJsonInteger $pb.$f)-or$pb.$f-lt0){throw 'Reaction victim hit points are not recorded.'}}
 $seq=[long]$Leg.ruleCountBefore;foreach($e in @($Leg.rules)){$seq++;if($e.sequence-ne$seq){throw 'Reaction rule window is not the observer''s consecutive record.'}}
 return @($Leg.rules)
}
function Assert-KmcChunk7ReactionCombat($Request,$Observations){
 if(-not(Test-KmcChunk7OwnedPairsCombatReaction $Request)){throw 'Reaction evidence requires its closed owner-seed selection.'}
 Assert-KmcChunk7OwnedPairsSelection $Request
 if((Get-KmcChunk7OwnedLoadCount $Request)-ne1-or(Get-KmcChunk7OwnedWriteCount $Request)-ne0){throw 'Reaction retains one native load and zero writes.'}
 $o=$Observations;$roles=@(Get-KmcChunk7OwnedPairIds $Request)
 Assert-KmcChunk7OwnerSeedDerivedCombatStart $Request $o
 foreach($key in @('r1','firstActionMount','localFailure','registeredDisable','removalRefusal','pendingSurvivor','nativeLife','attackModes','staged')){if($null-ne$o.PSObject.Properties[$key]){throw 'Reaction cannot carry another combat case''s evidence.'}}
 if(@($o.activations).Count-ne0-or@($o.inputs).Count-ne0){throw 'Reaction records no primary activation and no relationship input.'}
 $x=$o.reaction
 if($null-eq$x-or$x.contract-cne'owner-seed-combat-reaction-v2'-or$x.riderId-cne$roles[0]-or$x.mountId-cne$roles[1]-or$x.targetId-cne$o.targetId){throw 'Reaction evidence lacks its exact pair A and diagnostic target.'}
 Assert-KmcChunk7Boolean $x.complete $true
 if($null-eq$x.PSObject.Properties['rules']-or$null-eq$x.rules-or$x.rules.dropped-ne0-or@($x.rules.errors).Count-ne0){throw 'Reaction rule observation dropped or failed.'}
 if(-not(Test-KmcExactJsonInteger $x.firstRound)){throw 'Reaction lacks its first native round.'};$n=[long]$x.firstRound
 $p=$x.precondition;Assert-KmcChunk7ReactionTargetState $p $o.targetId
 # b201 cr: a target still resolving its native initiative cannot make an attack of opportunity. Rider A then
 # used one native same-round Delay behind it and resumed in the same round; its turn was never ended.
 if($null-ne$x.PSObject.Properties['initiativeDelay']){$dl=$x.initiativeDelay
  Assert-KmcExactProperties $dl @('round','targetBefore','canDelay','riderSlot','targetSlot','targetWait','nextRoundWait','before','traceSequenceBefore','statusAfter','suspended','resumed','traceSequenceResumed') 'reaction initiative delay'
  Assert-KmcChunk7ReactionTargetState $dl.targetBefore $o.targetId
  if($dl.round-ne$n-or$dl.targetBefore.round-ne$n-or-not($dl.targetBefore.waitingInitiative-eq$true-or$dl.targetBefore.surprised-eq$true)){throw 'Rider A delayed although the target had resolved its native initiative, or outside the measured round.'}
  if($dl.canDelay-ne$true-or$dl.statusAfter-cne'Delayed'-or$dl.suspended-ne$true-or-not(Test-KmcExactJsonInteger $dl.riderSlot)-or-not(Test-KmcExactJsonInteger $dl.targetSlot)-or$dl.targetSlot-le$dl.riderSlot-or
     -not(Test-KmcChunk7FiniteNumber $dl.targetWait)-or-not(Test-KmcChunk7FiniteNumber $dl.nextRoundWait)-or$dl.targetWait-ge$dl.nextRoundWait){throw 'Rider A''s native Delay was not one same-round suspension behind the later diagnostic target.'}
  if($null-eq$dl.before-or$null-eq$dl.resumed-or$dl.before.round-ne$n-or$dl.resumed.round-ne$n-or$dl.resumed.currentActor-cne$roles[0]){throw 'Rider A did not resume its delayed native turn in the same round.'}
  # The Delay window is bound to the trace and precedes the first leg; pair A issues no command inside it.
  foreach($f in @('traceSequenceBefore','traceSequenceResumed')){if(-not(Test-KmcExactJsonInteger $dl.$f)-or$dl.$f-lt0){throw 'Rider A''s Delay window bounds are invalid.'}}
  $firstLeg=if($null-ne$x.PSObject.Properties['approach']){$x.approach}else{$x.disengage}
  if($dl.traceSequenceResumed-lt$dl.traceSequenceBefore-or$dl.traceSequenceResumed-gt$firstLeg.traceSequenceBefore){throw 'Rider A''s Delay window is not before its first leg.'}
  Assert-KmcChunk7NoCostBoundary $o.allocationTrace $dl.traceSequenceBefore $dl.traceSequenceResumed @($roles[0],$roles[1]) 'Rider A''s native Delay'
  # Mount A's suspended paired grant is untouched by the Delay; pair B's own turns may end natively meanwhile.
  Assert-KmcChunk7NeverSpentCombatBudget $dl.before $dl.resumed @($roles[1])}
 if($p.round-ne$n-or$p.reactions-ne1-or$p.reactionsPerRound-ne1-or$p.canAttackOfOpportunity-ne$true){throw 'The diagnostic target did not hold exactly one available native attack of opportunity at the stimulus.'}
 # The native AttackOfOpportunity gates beyond the counter, at the stimulus and at the disengagement.
 $gates=if($null-ne$x.PSObject.Properties['disengageGates']){$x.disengageGates}else{$null}
 foreach($g in @($p,$gates,$x.disengage.targetBefore)){
  if($null-eq$g-or$g.canActInCombat-ne$true-or$g.waitingInitiative-ne$false-or$g.surprised-ne$false-or$g.canAct-ne$true-or$g.threatHand-ne$true-or$g.riderRemembersTarget-ne$true-or$g.mountRemembersTarget-ne$true){throw 'The diagnostic target could not natively make its attack of opportunity (initiative, surprise, action, threat hand or memory).'}}

 $approached=$null-ne$x.PSObject.Properties['approach'];$approachRules=@()
 if($approached){
  if($p.engagesRider-or$p.engagesMount){throw 'A native approach ran although pair A was already threatened.'}
  $approachRules=@(Assert-KmcChunk7ReactionLeg $x.approach 'approach' $n $o $roles)
  if($approachRules.Count-ne0){throw 'The native approach drew an attack.'}
  if(-not($x.approach.targetAfter.engagesRider-or$x.approach.targetAfter.engagesMount)){throw 'The native approach did not bring pair A inside the target''s threatened area.'}
 }elseif(-not($p.engagesRider-or$p.engagesMount)){throw 'Pair A was not inside the target''s threatened area and no native approach ran.'}
 $d=$x.disengage;$dr=@(Assert-KmcChunk7ReactionLeg $d 'disengage' $n $o $roles)
 if($approached-and$d.ruleCountBefore-ne$x.approach.ruleCountBefore+$approachRules.Count){throw 'Reaction rule windows are not consecutive.'}
 if(-not($d.targetBefore.engagesRider-or$d.targetBefore.engagesMount)-or$d.targetBefore.reactions-ne1-or$d.targetBefore.canAttackOfOpportunity-ne$true){throw 'Pair A did not leave from inside the target''s threatened area with its reaction available.'}
 if($d.targetAfter.engagesRider-or$d.targetAfter.engagesMount){throw 'Pair A did not leave the target''s threatened area.'}
 if($d.targetAfter.reactions-ne$d.targetBefore.reactions-1){throw 'The target''s native attack-of-opportunity count did not drop by exactly one.'}
 if($dr.Count-ne1){throw 'Native disengagement drew other than exactly one attack.'};$aoo=$dr[0]
 # The attack may miss: its victim's damage never falls, and the other pair actor's damage is unchanged.
 $victim=if($aoo.target-ceq$roles[0]){'riderDamage'}else{'mountDamage'};$other=if($victim-ceq'riderDamage'){'mountDamage'}else{'riderDamage'}
 if($d.residue.$victim-lt$d.pairBefore.$victim-or$d.residue.$other-ne$d.pairBefore.$other){throw 'The attack of opportunity healed its victim or damaged the other pair actor.'}
 if($aoo.kind-cne'attack'-or$aoo.actor-cne$o.targetId-or$aoo.opportunity-ne$true-or$aoo.target-cnotin@($roles[0],$roles[1])-or$aoo.ranged-ne$false-or$aoo.round-ne$n){throw 'The single attack is not the target''s native attack of opportunity against pair A.'}
 if((ConvertTo-Json -InputObject @($x.rules.events) -Depth 10 -Compress)-cne(ConvertTo-Json -InputObject @(@($approachRules)+@($dr)) -Depth 10 -Compress)){throw 'The rule observer recorded attacks outside the reaction windows.'}
 # An interrupted move must still have left the threatened area (checked above); a completed one reaches its clear distance.
 if(-not(Test-KmcChunk7FiniteNumber $d.clearDistance)-or($d.result-ceq'Success'-and$d.targetAfter.mountDistance-lt$d.clearDistance-0.6)){throw 'The disengagement did not reach its declared clear distance.'}
 # F04 defensive casting folded into this case (round N+1, the 7E row 69 measurement).
 Assert-KmcChunk7ReactionDefensiveCast $(if($null-ne$x.PSObject.Properties['defensiveCast']){$x.defensiveCast}else{$null}) $x $n $o $roles
 $prone=$x.proneStandUp
 if($null-eq$prone-or$prone.covered-ne$false-or$prone.reason-isnot[string]-or-not$prone.reason.StartsWith('No deterministic native prone source',[StringComparison]::Ordinal)){throw 'The prone stand-up half must be recorded as uncovered with its reason.'}
 foreach($field in @('pairResidueAbsent','nativeCombatEnded','initiativeInputsRestored','firstPairAiRestored','controlsSettled','inheritedPetsPreserved')){Assert-KmcChunk7Boolean $o.cleanup.$field $true}
 Assert-KmcChunk7Boolean $o.cleanup.ownedHorseRemoved $false;Assert-KmcChunk7Boolean $o.cleanup.nativeSaveWrittenByThisDriver $false;Assert-KmcChunk7Boolean $o.cleanup.delegatedPersistenceCleanup $false
}

# ---- 7E row 69 F04 defensive casting (round N+1 of owner-seed-combat-reaction) --------------------
# Rider A casts the disposable native Cure Light Wounds scroll at mount A from inside the target's threatened
# area. The native casting-defensively check must run exactly once with its DC, roll and outcome; the exact
# stack spends exactly once in place whatever the outcome; a failed check may draw at most one native attack of
# opportunity from the target (none on success); mount A and pair B never spend; rider A is never refunded.
$script:KmcReactionScrollAbility='5590652e1c2225c4ca30c4a699ab3649';$script:KmcReactionScrollItem='cd635d5720937b044a354dba17abad8d'
function Assert-KmcChunk7ReactionDefensiveCast($D,$X,[long]$N,$o,$roles){
 if($null-eq$D-or$D.contract-cne'reaction-defensive-cast-v1'-or$D.riderId-cne$roles[0]-or$D.mountId-cne$roles[1]-or$D.casterId-cne$roles[0]){throw 'The defensive casting measurement is missing or names another caster.'}
 Assert-KmcChunk7Boolean $D.complete $true
 if($D.round-ne$N+1){throw 'The defensive cast is not on rider A''s next native round.'}
 if($D.scrollUnits-ne4-or$D.scrollBlueprint-cne$script:KmcReactionScrollItem-or$null-eq$D.scrollAbility-or$D.scrollAbility.blueprint-cne$script:KmcReactionScrollAbility-or$D.scrollAbility.caster-cne$roles[0]){throw 'The defensive cast does not use the one disposable native scroll stack on rider A.'}
 $p=$D.precondition;Assert-KmcChunk7ReactionTargetState $p $o.targetId
 if($p.round-ne$N+1-or$p.reactions-ne1-or$p.canAttackOfOpportunity-ne$true-or$p.canActInCombat-ne$true-or$p.waitingInitiative-ne$false-or$p.surprised-ne$false-or$p.canAct-ne$true-or$p.threatHand-ne$true){throw 'The target did not hold its one native attack of opportunity before the defensive cast.'}
 $approachRules=@()
 if($null-ne$D.PSObject.Properties['approach']){
  if($p.engagesRider){throw 'A native approach ran although rider A was already threatened in N+1.'}
  $approachRules=@(Assert-KmcChunk7ReactionLeg $D.approach 'defensive-approach' ($N+1) $o $roles)
  if($approachRules.Count-ne0-or-not$D.approach.targetAfter.engagesRider){throw 'The N+1 approach drew an attack or did not bring rider A inside the threatened area.'}
 }elseif(-not$p.engagesRider){throw 'Rider A was not threatened in N+1 and no native approach ran.'}
 $c=$D.cast
 # The native casting-defensively window keys on the caster, so rider A itself must be engaged.
 if($null-eq$c-or$c.round-ne$N+1-or$c.riderEngaged-ne$true){throw 'Rider A did not cast from inside the target''s threatened area.'}
 Assert-KmcChunk7Boolean $c.clicked $true;Assert-KmcChunk7Boolean $c.selected $true;Assert-KmcChunk7Boolean $c.canTarget $true
 if($c.admittedShellCount-ne1-or$c.target-cne$roles[1]-or$null-eq$c.ability-or$c.ability.blueprint-cne$script:KmcReactionScrollAbility-or$c.ability.caster-cne$roles[0]-or$c.ability.sourceItemBlueprint-cne$script:KmcReactionScrollItem-or-not(Test-KmcExactJsonInteger $c.ability.sourceItem)-or$c.ability.sourceItem-eq0){throw 'The defensive cast is not one native scroll click by rider A on mount A.'}
 Assert-KmcChunk7ReactionTargetState $c.targetBefore $o.targetId;Assert-KmcChunk7ReactionTargetState $c.targetAfter $o.targetId
 $ev=@($c.castEvents);$hooks=@($ev|Where-Object{$_.kind-ceq'defensive-before'-and$_.actor-ceq$roles[0]});$rule=@($ev|Where-Object{$_.kind-ceq'defensive-rule-after'-and$_.actor-ceq$roles[0]})
 if($hooks.Count-ne1-or$rule.Count-ne1-or-not(Test-KmcExactJsonInteger $rule[0].dc)-or-not(Test-KmcExactJsonInteger $rule[0].roll)-or$rule[0].success-isnot[bool]){throw 'The native casting-defensively check did not run exactly once with its DC, roll and outcome.'}
 if(@($ev|Where-Object{$_.kind-cin@('defensive-before','defensive-rule-after')-and$_.actor-cne$roles[0]}).Count-ne0){throw 'A casting-defensively check ran on another unit.'}
 $k=$D.check
 if($null-eq$k-or$k.kind-cne'casting-defensively'-or$k.dc-ne$rule[0].dc-or$k.roll-ne$rule[0].roll-or$k.success-ne$rule[0].success-or$D.outcome-cne$(if($rule[0].success){'success'}else{'failure'})){throw 'The recorded defensive check differs from its native rule.'}
 $spends=@($ev|Where-Object{$_.kind-ceq'item-spend-after'-and$_.actor-ceq$roles[0]-and$_.identity-eq$c.ability.sourceItem})
 $foreign=@($ev|Where-Object{$_.kind-ceq'item-spend-after'-and$_.identity-ne$c.ability.sourceItem})
 if($spends.Count-ne1-or$spends[0].result-ne$true-or$foreign.Count-ne0){throw 'The exact scroll stack did not spend exactly once.'}
 $ib=$c.itemsBefore;$ia=$c.itemsAfter
 if($ib.exactSlot-ne$true-or$ia.exactSlot-ne$true-or$ib.item-ne$c.ability.sourceItem-or$ia.item-ne$ib.item-or$ib.count-lt2-or$ia.count-ne$ib.count-1){throw 'The exact equipped scroll stack did not decrement in place exactly once.'}
 $rules=@($c.rules);$aoo=@($rules|Where-Object{$_.kind-ceq'attack'-and$_.opportunity-eq$true-and$_.actor-ceq$o.targetId-and$_.target-cin@($roles[0],$roles[1])-and$_.round-eq$N+1})
 if($rules.Count-ne$aoo.Count-or$(if($rule[0].success){$aoo.Count-ne0}else{$aoo.Count-gt1})){throw 'The defensive check''s native attacks of opportunity differ from its outcome.'}
 if($c.targetAfter.reactions-ne$c.targetBefore.reactions-$aoo.Count){throw 'The target''s attack-of-opportunity count does not match the defensive outcome.'}
 # Damage inside the running cast shows its native concentration check; none without damage.
 $damage=@($ev|Where-Object{$_.kind-ceq'damage'-and$_.target-ceq$roles[0]-and$_.damage-gt0});$conc=@($ev|Where-Object{$_.kind-ceq'concentration-rule-after'-and$_.actor-ceq$roles[0]})
 if(($damage.Count-eq0-and$conc.Count-ne0)-or$conc.Count-gt$damage.Count){throw 'A concentration check ran without damage inside the cast.'}
 # Exactly one native outcome: the shell acted (one scroll cast, rider A's Standard charged) or a failed concentration check lost it (no cast, no cost).
 $acted=@($ev|Where-Object{$_.kind-ceq'cast-after'-and$_.actor-ceq$roles[0]-and$_.ability.blueprint-ceq$script:KmcReactionScrollAbility});$lost=@($conc|Where-Object{$_.success-eq$false}).Count-gt0
 $sb=Get-KmcChunk7CombatActor $c.before $roles[0];$sa=Get-KmcChunk7CombatActor $c.after $roles[0]
 if($(if($acted.Count-eq1){$lost-or[double]$sa.standard-le[double]$sb.standard+0.001}else{$acted.Count-ne0-or-not$lost})){throw 'The defensive scroll cast neither acted once with rider A''s native Standard charged nor was lost to a failed concentration check.'}
 if($rule[0].success-and$acted.Count-ne1){throw 'A successful defensive check did not cast the scroll exactly once.'}
 $t=$c.terminal;if($null-eq$t-or$t.finished-ne$true-or$t.executor-cne$roles[0]){throw 'The defensive cast shell did not finish on rider A.'}
 Assert-KmcChunk7NeverSpentCombatBudget $c.before $c.after @($roles[1],$roles[2],$roles[3])
 $elapsed=([double]$c.after.gameTicks-[double]$c.before.gameTicks)/10000000.0;$rb=Get-KmcChunk7CombatActor $c.before $roles[0];$ra=Get-KmcChunk7CombatActor $c.after $roles[0]
 foreach($f in @('move','swift')){if([double]$ra.$f-gt[double]$rb.$f+0.001){throw 'Rider A spent a move or swift action on its scroll cast.'}}
 if([double]$ra.standard-lt[double]$rb.standard-$elapsed*1.1-0.001){throw 'Rider A''s native Standard was refunded by the defensive cast.'}
 $res=$c.residue;foreach($f in @('activeCommand','groundMovement')){Assert-KmcChunk7Boolean $res.$f $false};if($res.state-cne'Mounted'){throw 'Pair A did not stay mounted through the defensive cast.'}
 # Consecutive rule windows: N disengagement, then the N+1 approach (if any) and the cast.
 $cap=$D.rulesCapture
 if($null-eq$cap-or$cap.dropped-ne0-or@($cap.errors).Count-ne0){throw 'Defensive cast rule observation dropped or failed.'}
 if((ConvertTo-Json -InputObject @($cap.events) -Depth 10 -Compress)-cne(ConvertTo-Json -InputObject @(@($X.rules.events)+@($approachRules)+@($rules)) -Depth 10 -Compress)){throw 'The rule observer recorded attacks outside the reaction and defensive windows.'}
 if($null-eq$D.scroll-or$D.scroll.ownedItemReleased-ne$true-or$D.scroll.disposed-ne$true-or$null-eq$D.originalSlots-or$null-eq$D.castTrace-or$D.castTrace.dropped-ne0-or$D.castTrace.faults-ne0){throw 'The defensive scroll stack was not released or its cast observer was incomplete.'}
}
# ---- 7E X1 F04 staged moving actions (owner-seed-staged) ----------------------------------------
# The Chunk 6D plans on the owner-seed-derived mounted start: C6D-move-cast-move (round N),
# C6D-double-move-ranged (N+1, its scroll cast the next-turn control in N+2) and the ranged approach
# row (beyond range, exhaustion and its native refusal in N+2, the RiderRanged approach shot in N+3).
# Costs are read from the shared allocation trace by each step's own sequence window.
function Get-KmcChunk7StagedPlan{
 @(@('C6D-move-cast-move','move-short',0),@('C6D-move-cast-move','cast-standard-scroll',0),@('C6D-move-cast-move','move-short',0),
   @('C6D-double-move-ranged','move-two-moves',1),@('C6D-double-move-ranged','attack-ranged',1),
   @('C7-staged-ranged-approach','move-beyond-range',2),@('C7-staged-ranged-approach','move-exhaust',2),@('C7-staged-ranged-approach','move-probe',2),
   @('C6D-double-move-ranged','cast-standard-scroll',2),@('C7-staged-ranged-approach','attack-ranged-approach',3))
}
function Test-KmcChunk7StagedClose($A,$B){[Math]::Abs([double]$A-[double]$B)-le0.001}
function Get-KmcChunk7StagedFlat($A,$B){$dx=[double]$A[0]-[double]$B[0];$dz=[double]$A[2]-[double]$B[2];[Math]::Sqrt($dx*$dx+$dz*$dz)}
function Get-KmcChunk7StagedTrace($o,$From,$To){
 foreach($f in @($From,$To)){if(-not(Test-KmcExactJsonInteger $f)-or$f-lt0){throw 'Staged trace window bounds are invalid.'}}
 if($To-lt$From){throw 'Staged trace window runs backwards.'}
 @($o.allocationTrace.events|Where-Object{$_.sequence-gt$From-and$_.sequence-le$To})
}
function Assert-KmcChunk7StagedState($State,[string]$Why){
 if($null-eq$State){throw ($Why+': state missing')}
 if($State.relationship-cne'Mounted'){throw ($Why+': pair A is not mounted.')}
 foreach($f in @('riderPosition','mountPosition')){if(@($State.$f).Count-ne3-or@($State.$f|Where-Object{-not(Test-KmcChunk7FiniteNumber $_)}).Count-ne0){throw ($Why+': position missing.')}}
 if(-not(Test-KmcChunk7FiniteNumber $State.targetDistance)){throw ($Why+': target distance missing.')}
 foreach($f in @('mountUsedOneMove','mountUsedTwoMove','mountHasMove','riderHasStandard','riderEngaged','mountEngaged')){if($State.$f-isnot[bool]){throw ($Why+': native action flag missing or mistyped.')}}
}
function Assert-KmcChunk7StagedSettled($State,[string]$Why){
 Assert-KmcChunk7StagedState $State $Why
 if($State.riderCommandsEmpty-ne$true-or$State.mountCommandsEmpty-ne$true-or$State.processesSettled-ne$true-or$State.nativeAbilitiesPending-ne$false-or$State.projectilesPending-ne$false-or$State.activePairCommand-ne$false-or$State.pairMovement-ne$false){throw ($Why+': command, process or pair residue remains.')}
}
# Pair B never spends inside a staged window; its finished turns' cooldowns may only decay natively.
function Assert-KmcChunk7StagedPairBIdle($Before,$After,$roles){Assert-KmcChunk7NeverSpentCombatBudget $Before $After @($roles[2],$roles[3])}
function Get-KmcChunk7StagedCosts($Events,[string]$Actor){@($Events|Where-Object{$_.boundary-cin@('cost-before','cost-after','actor-cost-before','actor-cost-after')-and$_.state.actor-ceq$Actor})}
function Assert-KmcChunk7StagedLeg($Leg,$o,$roles,[string]$Why){
 Assert-KmcChunk7Boolean $Leg.clicked $true;Assert-KmcChunk7Boolean $Leg.fiveFootStep $false;Assert-KmcChunk7Boolean $Leg.singleActionMove $false
 if($Leg.inputPath-cnotin@('pointer','ground-handler')){throw ($Why+': ground order input path differs.')}
 Assert-KmcChunk7StagedState $Leg.before ($Why+' before');Assert-KmcChunk7StagedSettled $Leg.after ($Why+' after')
 $events=@(Get-KmcChunk7StagedTrace $o $Leg.traceSequenceBefore $Leg.traceSequenceAfter)
 if(@($Leg.castEvents|Where-Object{$_.kind-clike'cast-*'-or$_.kind-clike'*spend*'}).Count-ne0){throw ($Why+': the movement leg cast or spent something.')}
 if(@(Get-KmcChunk7StagedCosts $events $roles[0]).Count-ne0){throw ($Why+': rider A was charged for the mount''s movement.')}
 $rb=Get-KmcChunk7CombatActor $Leg.before $roles[0];$ra=Get-KmcChunk7CombatActor $Leg.after $roles[0];$mb=Get-KmcChunk7CombatActor $Leg.before $roles[1];$ma=Get-KmcChunk7CombatActor $Leg.after $roles[1]
 foreach($f in @('standard','move','swift')){if(-not(Test-KmcChunk7StagedClose $ra.$f $rb.$f)){throw ($Why+': rider A '+$f+' changed during movement.')}}
 if(-not(Test-KmcChunk7StagedClose $ma.standard $mb.standard)-or-not(Test-KmcChunk7StagedClose $ma.swift $mb.swift)){throw ($Why+': the mount spent a non-movement action.')}
 Assert-KmcChunk7StagedPairBIdle $Leg.before $Leg.after $roles
 $moved=Get-KmcChunk7StagedFlat $Leg.before.mountPosition $Leg.after.mountPosition
 # TB movement continues until the mount's 6 s move cooldown budget is spent (frozen 203 TB).
 if([double]$mb.move-lt5.95){
  $c=$Leg.carrier
  if($null-eq$c-or$c.type-cne'UnitMoveTo'-or$c.executor-cne$roles[1]-or$c.createdByPlayer-ne$true){throw ($Why+': the exact mount carrier was not admitted.')}
  if($null-eq$Leg.terminal-or$Leg.terminal.finished-ne$true-or$Leg.terminal.executor-cne$roles[1]-or$Leg.terminal.identity-ne$c.identity){throw ($Why+': the mount carrier did not finish.')}
  if($moved-le0.5){throw ($Why+': the mount did not move.')}
  if([double]$ma.move-le[double]$mb.move+0.001){throw ($Why+': the mount''s native move debt did not increase for its own movement.')}
 }else{
  if($null-ne$Leg.carrier-and($null-eq$Leg.terminal-or$Leg.terminal.finished-ne$true)){throw ($Why+': the exhausted mount kept a movement command.')}
  if($moved-gt0.3){throw ($Why+': the exhausted mount still moved.')}
  if(@($events|Where-Object{$_.boundary-cin@('cost-before','cost-after')}).Count-ne0){throw ($Why+': the refused movement committed a cost.')}
  if(-not(Test-KmcChunk7StagedClose $ma.move $mb.move)){throw ($Why+': the refused movement changed the mount''s move debt.')}
 }
 return $moved
}
# The scroll's ability (CureLightWoundsCast) carries AbilityEffectStickyTouch whose TouchDeliveryAbility is
# CureLightWounds. The pinned AbilityEffectStickyTouch.Apply initializes UnitPartTouch and, when the caster is the
# target, triggers RuleCastSpell on the touch ability's data at once (no child command, no spend); otherwise it queues
# a touch-delivery UnitUseAbility. The installed relation was recorded natively by b136 (F09 presentation:
# nativeFollowupAbilityGuid read from the scroll ability's TouchDeliveryAbility) and in every 6D staged scroll window.
$script:KmcStagedScrollTouchDelivery=@{'5590652e1c2225c4ca30c4a699ab3649'='47808d23c67033d4bbab86a1070fd62f'}
function Get-KmcChunk7StagedField($Event,[string]$Name){if($null-eq$Event){return $null};$p=$Event.PSObject.Properties[$Name];if($null-eq$p){$null}else{$p.Value}}
# One self-targeted scroll use in its cast window: exactly one sourced cast of the scroll ability from the equipped
# stack, its one spell spend and one item spend, then exactly one sourceless cast of the exact pinned touch-delivery
# ability by and on rider A after both spends, with exactly one heal (by and on rider A, after it, no larger
# than rider A's native damage before the step). Every other rider cast, spend, heal or child command refuses.
function Assert-KmcChunk7StagedScrollDelivery($Step,$roles,[string]$Why){
 $a=$Step.ability;$rider=$roles[0];$events=@($Step.castEvents);$delivery=$script:KmcStagedScrollTouchDelivery[[string]$a.blueprint]
 if($null-eq$delivery){throw ($Why+': the scroll ability has no pinned native touch delivery.')}
 $at={param($e)[array]::IndexOf($events,$e)}
 $castAfters=@($events|Where-Object{$_.kind-ceq'cast-after'-and$_.actor-ceq$rider})
 $scroll=@($castAfters|Where-Object{$_.ability.blueprint-ceq$a.blueprint-and$_.ability.sourceItem-eq$a.sourceItem-and$_.ability.identity-eq$a.identity})
 $inner=@($castAfters|Where-Object{$_.ability.blueprint-ceq$delivery})
 if($scroll.Count-ne1-or$scroll[0].process-eq0-or$scroll[0].spellFailed-ne$false-or$scroll[0].arcaneFailed-ne$false-or$scroll[0].target-cne$Step.target){throw ($Why+': the native cast failed or duplicated.')}
 if($castAfters.Count-ne$scroll.Count+$inner.Count-or$inner.Count-gt1){throw ($Why+': the native cast failed or duplicated.')}
 # The caster is the target, so the pinned Apply always triggers the delivery: its absence is not the native shape.
 if($inner.Count-ne1-or$Step.target-cne$rider){throw ($Why+': the self-targeted scroll lacks its native touch delivery.')}
 $sb=@($events|Where-Object{$_.kind-ceq'cast-before'-and$_.actor-ceq$rider-and$_.ability.identity-eq$a.identity-and$_.ability.blueprint-ceq$a.blueprint-and$_.target-ceq$rider})
 if(@($events|Where-Object{$_.kind-ceq'cast-before'}).Count-ne$castAfters.Count-or@($events|Where-Object{$_.kind-clike'cast-*'-and$_.actor-cne$rider}).Count-ne0-or
    $sb.Count-ne1-or(&$at $sb[0])-ge(&$at $scroll[0])){throw ($Why+': the native cast failed or duplicated.')}
 $spellSpends=@($events|Where-Object{$_.kind-ceq'spell-spend-after'});$itemSpends=@($events|Where-Object{$_.kind-ceq'item-spend-after'})
 if($spellSpends.Count-ne1-or@($events|Where-Object{$_.kind-ceq'spell-spend-before'}).Count-ne1-or$spellSpends[0].ability.identity-ne$a.identity-or$itemSpends.Count-ne1-or@($events|Where-Object{$_.kind-ceq'item-spend-before'}).Count-ne1-or$itemSpends[0].identity-ne$a.sourceItem-or
    (&$at $scroll[0])-ge(&$at $spellSpends[0])){throw ($Why+': the scroll was not spent exactly once after its cast.')}
 if(@($events|Where-Object{$_.kind-cin@('start-before','start-after','action-before','action-after')-and$_.identity-ne$Step.costShell}).Count-ne0){throw ($Why+': the cast window holds another native command.')}
 $heals=@($events|Where-Object{$_.kind-ceq'heal'})
 if($heals.Count-ne$inner.Count){throw ($Why+': the heals do not match the native touch delivery.')}
 if($inner.Count-eq1){$i=$inner[0];$ib=@($events|Where-Object{$_.kind-ceq'cast-before'-and$_.identity-eq$i.identity})
  if($i.ability.caster-cne$rider-or$i.target-cne$rider-or$Step.target-cne$rider-or$i.ability.sourceItem-ne0-or$null-ne(Get-KmcChunk7StagedField $i.ability 'sourceItemBlueprint')-or$i.ability.identity-eq$a.identity-or
     $i.process-eq0-or$i.spellFailed-ne$false-or$i.arcaneFailed-ne$false-or$ib.Count-ne1-or$ib[0].ability.identity-ne$i.ability.identity-or$ib[0].ability.blueprint-cne$delivery-or$ib[0].actor-cne$rider-or$ib[0].target-cne$rider-or
     (&$at $ib[0])-le(&$at $spellSpends[0])-or(&$at $ib[0])-le(&$at $itemSpends[0])-or(&$at $ib[0])-ge(&$at $i)){throw ($Why+': the second cast is not the scroll''s exact self touch delivery after its spend.')}
  $h=$heals[0];$damage=Get-KmcChunk7StagedField (Get-KmcChunk7StagedField (Get-KmcChunk7CombatActor $Step.before $rider) 'native') 'damage'
  if($h.actor-cne$rider-or$h.target-cne$rider-or-not(Test-KmcExactJsonInteger $h.value)-or$h.value-lt0-or(&$at $h)-le(&$at $i)-or-not(Test-KmcExactJsonInteger $damage)-or$h.value-gt$damage){throw ($Why+': the touch delivery heal is not rider A''s one bounded native heal.')}
 }
}
function Assert-KmcChunk7StagedCast($Step,$o,$s,$roles,[string]$Why){
 $a=$Step.ability;$scrollItem=$s.scroll.equipped.item
 if($a.caster-cne$roles[0]-or$a.blueprint-cne'5590652e1c2225c4ca30c4a699ab3649'-or$a.runtimeActionType-cne'Standard'-or$a.available-ne$true-or$a.sourceItem-ne$scrollItem-or$a.sourceItemBlueprint-cne'cd635d5720937b044a354dba17abad8d'){throw ($Why+': the cast did not source the exact equipped disposable scroll stack.')}
 if($Step.target-cne$roles[0]-or$Step.selected-ne$true-or$Step.canTarget-ne$true-or$Step.clicked-ne$true-or$Step.admittedShellCount-ne1-or$null-eq$Step.shell){throw ($Why+': the normal cast input was not admitted exactly once.')}
 if($null-eq$Step.terminal-or$Step.terminal.finished-ne$true-or$Step.terminal.executor-cne$roles[0]-or$Step.terminal.type-cne'UnitUseAbility'-or$Step.terminal.identity-ne$Step.costShell){throw ($Why+': the cast shell did not finish on rider A.')}
 Assert-KmcChunk7StagedScrollDelivery $Step $roles $Why
 if(@($Step.castEvents|Where-Object{$_.actor-ceq$roles[1]}).Count-ne0){throw ($Why+': the mount cast or spent something.')}
 $events=@(Get-KmcChunk7StagedTrace $o $Step.traceSequenceBefore $Step.traceSequenceAfter)
 $before=@($events|Where-Object{$_.boundary-ceq'cost-before'-and$_.state.actor-ceq$roles[0]-and$_.command-eq$Step.costShell});$after=@($events|Where-Object{$_.boundary-ceq'cost-after'-and$_.state.actor-ceq$roles[0]-and$_.command-eq$Step.costShell})
 if($before.Count-ne1-or$after.Count-ne1-or$Step.costShell-eq0){throw ($Why+': rider A''s Standard was not committed exactly once.')}
 if(@(Get-KmcChunk7StagedCosts $events $roles[0]|Where-Object{$_.command-ne$Step.costShell}).Count-ne0){throw ($Why+': the cast window charged rider A for another command.')}
 if(-not(Test-KmcChunk7StagedClose ([double]$after[0].state.standard-[double]$before[0].state.standard) 6.0)){throw ($Why+': the native additive Standard debt differs.')}
 if(@(Get-KmcChunk7StagedCosts $events $roles[1]).Count-ne0){throw ($Why+': the mount was charged for the rider cast.')}
 $rb=Get-KmcChunk7CombatActor $Step.before $roles[0];$ra=Get-KmcChunk7CombatActor $Step.after $roles[0];$mb=Get-KmcChunk7CombatActor $Step.before $roles[1];$ma=Get-KmcChunk7CombatActor $Step.after $roles[1]
 foreach($f in @('move','swift')){if(-not(Test-KmcChunk7StagedClose $ra.$f $rb.$f)){throw ($Why+': rider A '+$f+' changed during the cast.')}}
 foreach($f in @('standard','move','swift')){if(-not(Test-KmcChunk7StagedClose $ma.$f $mb.$f)){throw ($Why+': mount '+$f+' changed during the rider cast.')}}
 if([double]$ra.standard-gt[double]$after[0].state.standard+0.001){throw ($Why+': the settled Standard debt exceeds its native charge.')}
 foreach($f in @('riderPosition','mountPosition')){if((Get-KmcChunk7StagedFlat $Step.before.$f $Step.after.$f)-gt0.05){throw ($Why+': the cast was not stationary at its real location.')}}
 $spent=@($Step.castEvents|Where-Object{$_.kind-ceq'item-spend-after'-and$_.identity-eq$a.sourceItem-and$_.result-eq$true})
 $stackBefore=@($Step.before.items|Where-Object{$_.item-eq$a.sourceItem});$stackAfter=@($Step.after.items|Where-Object{$_.item-eq$a.sourceItem})
 if($spent.Count-ne1-or$stackBefore.Count-ne1-or$stackAfter.Count-ne1-or$stackBefore[0].exactSlot-ne$true-or$stackAfter[0].exactSlot-ne$true-or$stackBefore[0].count-lt2-or$stackAfter[0].count-ne$stackBefore[0].count-1){throw ($Why+': the exact scroll stack did not decrement in place exactly once.')}
 # Native checks are never skipped after the fact: a running Standard cast after one second while
 # engaged must show its defensive check; damage inside the cast must show its concentration check.
 $engaged=$Step.before.riderEngaged-or$Step.before.mountEngaged
 $acted=@($Step.castEvents|Where-Object{$_.kind-ceq'action-before'-and$_.actor-ceq$roles[0]})
 if($acted.Count-ne1-or-not(Test-KmcChunk7FiniteNumber $acted[0].shell.timeSinceStart)){throw ($Why+': the native action boundary of the cast is missing.')}
 $defensive=@($Step.castEvents|Where-Object{$_.kind-cin@('defensive-before','defensive-after','defensive-rule-before','defensive-rule-after')})
 if($engaged-and[double]$acted[0].shell.timeSinceStart-gt1.0-and@($defensive|Where-Object kind -CEQ 'defensive-before').Count-ne1){throw ($Why+': an engaged running cast skipped its native defensive check.')}
 if(-not$engaged-and$defensive.Count-ne0){throw ($Why+': a defensive check ran although the pair was not engaged.')}
 $damage=@($Step.castEvents|Where-Object{$_.kind-ceq'damage'-and$_.target-ceq$roles[0]})
 $concentration=@($Step.castEvents|Where-Object{$_.kind-ceq'concentration-rule-after'})
 if(($damage.Count-gt0-and$concentration.Count-lt1)-or($damage.Count-eq0-and$concentration.Count-ne0)){throw ($Why+': the concentration checks do not match the damage inside the cast.')}
 if($s.defensiveCheck.covered-eq$false-and$engaged){throw ($Why+': an engaged cast contradicts the uncovered defensive-check record.')}
 Assert-KmcChunk7StagedSettled $Step.after ($Why+' after')
}
function Assert-KmcChunk7StagedRanged($Step,$o,$s,$roles,[bool]$Approach,[string]$Why){
 Assert-KmcChunk7Boolean $Step.clicked $true;Assert-KmcChunk7Boolean $Step.fullAttackMode $false
 $c=$Step.admitted
 if($null-eq$c-or$c.identity-eq0-or$c.type-cne'MountedPairAttackCommand'-or$c.executor-cne$roles[0]-or$c.createdByPlayer-ne$true-or$c.commandType-cne'Standard'-or@($Step.rejectionCodes).Count-ne0-or[string]$Step.rejectionFeedback-cne'Mounted pair command accepted: RiderRanged.'){throw ($Why+': the mounted ranged click was not admitted as the RiderRanged pair command.')}
 $t=$Step.terminal
 if($null-eq$t-or$t.identity-ne$c.identity-or$t.finished-ne$true-or$t.acted-ne$true-or$t.result-cne'Success'-or$t.executor-cne$roles[0]){throw ($Why+': the RiderRanged pair command did not finish successfully on rider A.')}
 $events=@(Get-KmcChunk7StagedTrace $o $Step.traceSequenceBefore $Step.traceSequenceAfter)
 $cost=@($events|Where-Object{$_.command-eq$c.identity-and$_.boundary-cin@('cost-before','cost-after','actor-cost-before','actor-cost-after')})
 $before=@($cost|Where-Object boundary -CEQ 'cost-before');$after=@($cost|Where-Object boundary -CEQ 'cost-after')
 if($before.Count-ne1-or$after.Count-ne1-or@($cost|Where-Object{$_.state.actor-cne$roles[0]}).Count-ne0){throw ($Why+': the shot was not rider A''s single native Standard commitment.')}
 if(-not(Test-KmcChunk7StagedClose ([double]$after[0].state.standard-[double]$before[0].state.standard) 6.0)){throw ($Why+': the native additive Standard debt differs.')}
 $rb=Get-KmcChunk7CombatActor $Step.before $roles[0];$ra=Get-KmcChunk7CombatActor $Step.after $roles[0];$mb=Get-KmcChunk7CombatActor $Step.before $roles[1];$ma=Get-KmcChunk7CombatActor $Step.after $roles[1]
 foreach($f in @('move','swift')){if(-not(Test-KmcChunk7StagedClose $ra.$f $rb.$f)){throw ($Why+': rider A '+$f+' changed during the shot.')}}
 if(-not(Test-KmcChunk7StagedClose $ma.standard $mb.standard)-or-not(Test-KmcChunk7StagedClose $ma.swift $mb.swift)){throw ($Why+': the mount spent a non-movement action during the rider''s shot.')}
 Assert-KmcChunk7StagedPairBIdle $Step.before $Step.after $roles
 $shots=@($Step.rules)
 if($shots.Count-ne1-or$shots[0].actor-cne$roles[0]-or$shots[0].target-cne$o.targetId-or$shots[0].opportunity-ne$false-or$shots[0].fullAttack-ne$false-or$shots[0].ranged-ne$true-or$shots[0].weapon-cne$s.weapon){throw ($Why+': the window holds other than rider A''s one native ranged shot.')}
 $origin=@($shots[0].actorPosition);$moved=Get-KmcChunk7StagedFlat $Step.before.mountPosition $Step.after.mountPosition
 if(-not(Test-KmcChunk7FiniteNumber $Step.startDistance)-or-not(Test-KmcChunk7FiniteNumber $Step.endDistance)-or-not(Test-KmcChunk7StagedClose $Step.envelope $s.envelope)){throw ($Why+': the shot geometry is missing.')}
 if((Get-KmcChunk7StagedFlat $origin $Step.after.riderPosition)-gt0.15){throw ($Why+': the shot origin is not the rider''s position after the pair''s movement.')}
 $mountMoves=@($events|Where-Object{$_.boundary-ceq'admission-after'-and$_.commandActor-ceq$roles[1]-and$_.commandType-ceq'Kingmaker.UnitLogic.Commands.UnitMoveTo'})
 if($Approach){
  if([double]$Step.startDistance-le[double]$s.envelope+0.5){throw ($Why+': the approach shot did not start beyond weapon range.')}
  if($moved-le0.5-or$mountMoves.Count-ne1){throw ($Why+': the pair did not approach through exactly one native mount move.')}
  if([double]$ma.move-le[double]$mb.move+0.001){throw ($Why+': the approach was not charged to the mount''s native move.')}
  if((Get-KmcChunk7StagedFlat $origin $Step.before.riderPosition)-le0.5){throw ($Why+': the shot was fired from the pre-approach position.')}
  if($null-ne$Step.PSObject.Properties['nativeRangeAtStop']){Assert-KmcChunk7StagedNativeRange $Step $s $Why}
  if([double]$Step.endDistance-gt[double]$s.envelope+0.1){throw ($Why+': the approach ended outside weapon range.')}
 }else{
  if([double]$Step.startDistance-gt[double]$s.envelope){throw ($Why+': the stationary shot started beyond weapon range.')}
  if($moved-gt0.05-or$mountMoves.Count-ne0-or-not(Test-KmcChunk7StagedClose $ma.move $mb.move)){throw ($Why+': the in-range shot moved or charged the mount.')}
  # MountedPairAttackCommand.StartChildAttack turns the stationary mount to face the target (ForceLookAt), so the
  # rider's saddle point may only rotate about the unmoved mount: same horizontal radius, same height (b204 step 4).
  $pb=Get-KmcChunk7StagedVector $Step.before.riderPosition $Why;$pa=Get-KmcChunk7StagedVector $Step.after.riderPosition $Why
  $mpb=Get-KmcChunk7StagedVector $Step.before.mountPosition $Why;$mpa=Get-KmcChunk7StagedVector $Step.after.mountPosition $Why
  $seatBefore=Get-KmcChunk7StagedFlat $pb $mpb;$seatAfter=Get-KmcChunk7StagedFlat $pa $mpa
  if([Math]::Abs($seatBefore-$seatAfter)-gt0.05-or$seatAfter-gt0.75-or[Math]::Abs(($pb[1]-$mpb[1])-($pa[1]-$mpa[1]))-gt0.05){throw ($Why+': the in-range shot origin moved.')}
  # A displaced saddle point must face the target from the mount (b204: -93.0 degrees, target -93.0 degrees).
  if((Get-KmcChunk7StagedFlat $pb $pa)-gt0.05){
   $tp=Get-KmcChunk7StagedVector $shots[0].targetPosition $Why;$tx=$tp[0]-$mpa[0];$tz=$tp[2]-$mpa[2];$sx=$pa[0]-$mpa[0];$sz=$pa[2]-$mpa[2]
   $tl=[Math]::Sqrt($tx*$tx+$tz*$tz)
   if($seatAfter-le0.1-or$tl-le0.5-or($sx*$tx+$sz*$tz)/($seatAfter*$tl)-lt[Math]::Cos(5.0*[Math]::PI/180.0)){throw ($Why+': the in-range shot origin moved.')}
  }
 }
 Assert-KmcChunk7StagedSettled $Step.after ($Why+' after')
}
# owner-seed-staged-approach-range-v1 (F6): the native range quantities at the approach shot's stop, recorded before any
# assertion. The reader recomputes the flat and MechanicsDistance (2.1.7b GeometryUtils) distances from the recorded points,
# binds them to the step's after snapshot and end distance, and the footprints and range to the staged envelope.
function Assert-KmcChunk7StagedNativeRange($Step,$s,[string]$Why){
 $n=$Step.nativeRangeAtStop
 Assert-KmcExactProperties $n @('contract','riderPosition','mountPosition','targetPosition','mountTargetFlat','riderTargetFlat','mountTargetMechanics','riderTargetMechanics','riderCorpulence','mountCorpulence','targetCorpulence','weaponRangeMeters','commandApproachRadius') 'staged native range at stop'
 if($n.contract-cne'owner-seed-staged-approach-range-v1'){throw ($Why+': the native range record contract differs.')}
 $rp=Get-KmcChunk7StagedVector $n.riderPosition $Why;$mp=Get-KmcChunk7StagedVector $n.mountPosition $Why;$tp=Get-KmcChunk7StagedVector $n.targetPosition $Why
 foreach($f in @('mountTargetFlat','riderTargetFlat','mountTargetMechanics','riderTargetMechanics','riderCorpulence','mountCorpulence','targetCorpulence','weaponRangeMeters','commandApproachRadius')){if(-not(Test-KmcChunk7FiniteNumber $n.$f)-or[double]$n.$f-lt0){throw ($Why+': a native range quantity is missing or not finite.')}}
 if((Get-KmcChunk7StagedFlat $mp $Step.after.mountPosition)-gt1e-4-or(Get-KmcChunk7StagedFlat $rp $Step.after.riderPosition)-gt1e-4){throw ($Why+': the native range record is not the stop.')}
 $mf=Get-KmcChunk7StagedFlat $mp $tp;$rf=Get-KmcChunk7StagedFlat $rp $tp
 if([Math]::Abs($mf-[double]$n.mountTargetFlat)-gt1e-4-or[Math]::Abs($rf-[double]$n.riderTargetFlat)-gt1e-4-or[Math]::Abs($mf-[double]$Step.endDistance)-gt1e-4){throw ($Why+': the native flat distances differ from the recorded points.')}
 if([Math]::Abs((Get-KmcChunk7MechanicsDistance $mp $tp)-[double]$n.mountTargetMechanics)-gt1e-4-or[Math]::Abs((Get-KmcChunk7MechanicsDistance $rp $tp)-[double]$n.riderTargetMechanics)-gt1e-4){throw ($Why+': the native MechanicsDistance values differ from the recorded points.')}
 if([Math]::Abs([double]$n.mountCorpulence+[double]$n.targetCorpulence+[double]$n.weaponRangeMeters-[double]$s.envelope)-gt0.01-or[Math]::Abs([double]$n.weaponRangeMeters-[double]$s.weaponRangeMeters)-gt1e-4){throw ($Why+': the native footprints and range differ from the staged envelope.')}
}# The up-front geometry plan (owner-seed-staged-geometry-plan-v1, Chunk7StagedGeometry). The reader replays every
# recorded candidate from its recorded points: the fixed candidate order, each requested point, each proposal's first
# refusing term (walkable node, exact trace, a real leg, the declared band, the stop-short margin), the chosen leg,
# the simulated end and native movement time, the double move's second Move, exhaustion at 5.95 s, the 8-leg limit,
# the shot's range and the approach trace. Walkable flags, goals and trace ends are native observations it cannot
# recompute; everything derived from them it does.
$script:KmcStagedFan=@(0.0,22.5,-22.5,45.0,-45.0,67.5,-67.5,90.0,-90.0,112.5,-112.5,135.0,-135.0,157.5,-157.5,180.0)
$script:KmcStagedPlacementDistances=@(8.0,9.0,7.0,10.0)
$script:KmcStagedMaxResidual=[double][float]0.3
function Get-KmcChunk7StagedVector($P,[string]$Why){
 if(@($P).Count-ne3-or@($P|Where-Object{-not(Test-KmcChunk7FiniteNumber $_)}).Count-ne0){throw ($Why+': a recorded point is missing or not finite.')}
 ,@([double]$P[0],[double]$P[1],[double]$P[2])
}
function Get-KmcChunk7StagedRotation([double]$X,[double]$Z,[double]$Degrees){$r=$Degrees*[Math]::PI/180.0;$c=[Math]::Cos($r);$s=[Math]::Sin($r);,@(($X*$c+$Z*$s),(-$X*$s+$Z*$c))}
function Test-KmcChunk7StagedNear($A,$B,[double]$Tolerance=1e-6){[Math]::Abs([double]$A[0]-[double]$B[0])-le$Tolerance-and[Math]::Abs([double]$A[1]-[double]$B[1])-le$Tolerance-and[Math]::Abs([double]$A[2]-[double]$B[2])-le$Tolerance}
function Get-KmcChunk7StagedBand([double]$D,[double]$Floor,[double]$Ceiling){if($D-lt$Floor){'below-band'}elseif($D-gt$Ceiling){'above-band'}else{$null}}
function Get-KmcChunk7StagedLegGeometry([string]$Kind,$Start,$Goal,$Target,[double]$Envelope){
 $d=Get-KmcChunk7StagedFlat $Goal $Target;$length=Get-KmcChunk7StagedFlat $Start $Goal
 $stop=@(([double]$Goal[0]-([double]$Goal[0]-[double]$Start[0])/$length*0.5),[double]$Goal[1],([double]$Goal[2]-([double]$Goal[2]-[double]$Start[2])/$length*0.5))
 $sd=Get-KmcChunk7StagedFlat $stop $Target
 switch -CaseSensitive($Kind){
  {$_-cin@('move-short','move-two-moves')}{$band=Get-KmcChunk7StagedBand $d 6.5 ($Envelope-1.5);$margin=Get-KmcChunk7StagedBand $sd 6.5 ($Envelope-1.5)}
  'move-beyond-range'{$band=Get-KmcChunk7StagedBand $d ($Envelope+3.0-0.0) ($Envelope+3.0+1.5);$margin=Get-KmcChunk7StagedBand $sd ($Envelope+2.5) ($Envelope+3.0+1.5)}
  'move-exhaust'{$band=Get-KmcChunk7StagedBand $d ($Envelope+2.0) ($Envelope+6.0);$margin=Get-KmcChunk7StagedBand $sd ($Envelope+2.0) ($Envelope+6.0)}
  default{return $null}
 }
 if($null-ne$band){$band}elseif($null-ne$margin){'stop-short-'+$margin}else{$null}
}
# Geometry revision 2 (owner-seed-staged-geometry-plan-v2, F6): the beyond-range leg aims at its band centre (envelope + 3.75 m)
# and a planned beyond-range leg keeps a spare eligible proposal after its choice; bands and margins are unchanged.
function Get-KmcChunk7StagedLengths([string]$Kind,[double]$StartDistance,[double]$Envelope,[int]$Revision=1){
 switch -CaseSensitive($Kind){'move-short'{,@(2.0)}'move-probe'{,@(2.0)}'move-two-moves'{,@(7.0)}'move-exhaust'{,@(7.0,5.0,3.5)}
  'move-beyond-range'{$aim=if($Revision-ge2){3.0+0.75}else{3.0};$l=[Math]::Max(0.6,$Envelope+$aim-$StartDistance);,@($l,($l+2.0),($l+4.0))}default{throw ('Unknown staged movement kind '+$Kind+'.')}}
}
function Get-KmcChunk7StagedRevision([string]$Contract){switch -CaseSensitive($Contract){'owner-seed-staged-geometry-plan-v1'{1}'owner-seed-staged-geometry-plan-v2'{2}default{throw 'Staged actions lack their up-front geometry plan.'}}}
# One recorded proposal checked against its replayed request and recomputed term; returns the term.
function Get-KmcChunk7StagedProposalTerm($P,[double]$Angle,[double]$Length,$Requested,$Start,[string]$Why){
 if(@($P).Count-ne13){throw ($Why+': a proposal record is malformed.')}
 if([double]$P[0]-ne$Angle-or[Math]::Abs([double]$P[1]-$Length)-gt1e-9){throw ($Why+': the proposal order differs from the declared lengths and fan.')}
 $req=@([double]$P[2],[double]$P[3],[double]$P[4]);if(-not(Test-KmcChunk7StagedNear $req $Requested)){throw ($Why+': a requested point differs from the declared geometry.')}
 if($P[5]-isnot[bool]){throw ($Why+': a walkable flag is mistyped.')}
 $goal=Get-KmcChunk7StagedVector @($P[6],$P[7],$P[8]) $Why;$trace=Get-KmcChunk7StagedVector @($P[9],$P[10],$P[11]) $Why
 if(-not$P[5]-and-not(Test-KmcChunk7StagedNear $trace $Start)){throw ($Why+': an unwalkable proposal records a trace.')}
 $term=if(-not$P[5]){'not-walkable'}elseif((Get-KmcChunk7StagedFlat $trace $goal)-gt$script:KmcStagedMaxResidual){'trace-inexact'}elseif((Get-KmcChunk7StagedFlat $Start $goal)-le0.5){'too-short'}else{$null}
 ,@($term,$goal)
}
# Replays one recorded simulation of the declared plan from a start; throws on any inconsistency, returns $true
# when every leg was eligible (feasible) and $false when it refused at its recorded first refusing leg and term.
function Test-KmcChunk7StagedSimulation($Sim,$MountStart,$Target,[double]$Envelope,[double]$Speed,[string]$Why,[int]$Revision=1){
 if($null-eq$Sim){throw ($Why+': the simulation is missing.')}
 if(-not(Test-KmcChunk7StagedNear (Get-KmcChunk7StagedVector $Sim.target $Why) $Target)){throw ($Why+': the simulated target differs.')}
 $plan=@(Get-KmcChunk7StagedPlan);$legs=@($Sim.legs);$li=0;$step=0;$leg=0;$time=0.0;$side=1;$pos=$MountStart
 $refuse={param([string]$Kind,[string]$Term)$r=$Sim.refusal
  if($null-eq$r-or$r.step-ne$step-or$r.kind-cne$Kind-or$r.leg-ne$leg-or$r.term-cne$Term-or$li-ne$legs.Count){throw ($Why+': the recorded refusal differs from the replayed first refusing leg ('+$step+' '+$Kind+' '+$leg+' '+$Term+').')}}
 $next={$round=$plan[$step][2];$step++;$leg=0;if($step-lt$plan.Count-and$plan[$step][2]-ne$round){$time=0.0}}
 while($step-lt$plan.Count){$kind=[string]$plan[$step][1]
  if(-not$kind.StartsWith('move-',[StringComparison]::Ordinal)){
   if($kind-cin@('attack-ranged','attack-ranged-approach')){
    if($li-ge$legs.Count){throw ($Why+': a planned check is missing.')};$l=$legs[$li];$li++
    if($l.step-ne$step-or$l.kind-cne$kind-or$l.leg-ne0-or-not(Test-KmcChunk7StagedNear (Get-KmcChunk7StagedVector $l.start $Why) $pos)){throw ($Why+': a check is out of place.')}
    $d=Get-KmcChunk7StagedFlat $pos $Target;$check=$null;$chosen=$null;$props=@($l.proposals)
    if($kind-ceq'attack-ranged'){if($d-gt$Envelope){$check='shot-out-of-range'};if($props.Count-ne0){throw ($Why+': the shot check records proposals.')}}
    elseif($d-le$Envelope+0.5+0.5){$check='approach-start-in-range';if($props.Count-ne0){throw ($Why+': an in-range approach records proposals.')}}
    else{$ux=([double]$pos[0]-[double]$Target[0])/$d;$uz=([double]$pos[2]-[double]$Target[2])/$d;$k=0
     foreach($inside in @(1.0,3.0,5.0)){$along=$Envelope-$inside
      if($k-ge$props.Count){throw ($Why+': the approach stopped before its first eligible point.')}
      $r=Get-KmcChunk7StagedProposalTerm $props[$k] 0.0 $inside @(([double]$Target[0]+$ux*$along),[double]$pos[1],([double]$Target[2]+$uz*$along)) $pos ($Why+' approach')
      $term=$r[0];if($null-eq$term-and(Get-KmcChunk7StagedFlat $r[1] $Target)-gt$Envelope){$term='approach-outside-range'}
      if([string]$props[$k][12]-cne[string]$term-or($null-eq$term)-ne($null-eq$props[$k][12])){throw ($Why+': an approach term differs from its recomputation.')}
      $k++;if($null-eq$term){$chosen=$k-1;break}}
     if($k-ne$props.Count){throw ($Why+': the approach records proposals past its first eligible point.')}
     if($null-eq$chosen){$check='no-destination'}}
    if([string]$l.check-cne[string]$check-or($null-eq$l.check)-ne($null-eq$check)-or($null-eq$chosen)-ne($null-eq$l.chosen)-or($null-ne$chosen-and$l.chosen-ne$chosen)){throw ($Why+': a check differs from its recomputation.')}
    if($null-ne$check){&$refuse $kind $check;return $false}
   }
   . $next;continue
  }
  if($li-ge$legs.Count){throw ($Why+': a planned leg is missing.')};$l=$legs[$li];$li++;$lw=$Why+' step '+$step+' leg '+$leg
  if($l.step-ne$step-or$l.kind-cne$kind-or$l.leg-ne$leg-or$l.side-ne$side-or[Math]::Abs([double]$l.time-$time)-gt1e-9-or-not(Test-KmcChunk7StagedNear (Get-KmcChunk7StagedVector $l.start $lw) $pos)){throw ($lw+': the leg is out of place.')}
  $rx=[double]$pos[0]-[double]$Target[0];$rz=[double]$pos[2]-[double]$Target[2]
  if($rx*$rx+$rz*$rz-le1e-8){if($l.check-cne'no-direction'){throw ($lw+': the missing direction is not recorded.')};&$refuse $kind 'no-direction';return $false}
  $dir=if($kind-ceq'move-beyond-range'){@($rx,$rz)}else{Get-KmcChunk7StagedRotation $rx $rz (90.0*$side)}
  $norm=[Math]::Sqrt($dir[0]*$dir[0]+$dir[1]*$dir[1]);$dx=$dir[0]/$norm;$dz=$dir[1]/$norm
  $props=@($l.proposals);$k=0;$chosen=$null;$goal=$null;$spare=$null;$requireSpare=$Revision-ge2-and$kind-ceq'move-beyond-range'
  $hasSpare=$null-ne$l.PSObject.Properties['spare'];if($hasSpare-ne($Revision-ge2)){throw ($lw+': the leg''s spare field does not match its geometry revision.')}
  :search foreach($length in (Get-KmcChunk7StagedLengths $kind (Get-KmcChunk7StagedFlat $pos $Target) $Envelope $Revision)){foreach($angle in $script:KmcStagedFan){
   if($k-ge$props.Count){throw ($lw+': the leg stopped before its first eligible proposal.')}
   $u=Get-KmcChunk7StagedRotation $dx $dz $angle
   $r=Get-KmcChunk7StagedProposalTerm $props[$k] $angle $length @(([double]$pos[0]+$u[0]*$length),[double]$pos[1],([double]$pos[2]+$u[1]*$length)) $pos $lw
   $term=$r[0];if($null-eq$term){$term=Get-KmcChunk7StagedLegGeometry $kind $pos $r[1] $Target $Envelope}
   # The exhaustion tail term: a leg ending below 5.95 s with at most 0.5 m + the 0.5 m margin of budget left.
   if($null-eq$term-and$kind-ceq'move-exhaust'){$after=$time+(Get-KmcChunk7StagedFlat $pos $r[1])/$Speed;if($after-lt5.95-and(6.0-$after)*$Speed-le0.5+0.5){$term='exhaust-tail'}}
   if([string]$props[$k][12]-cne[string]$term-or($null-eq$term)-ne($null-eq$props[$k][12])){throw ($lw+': proposal '+$k+' records '+$props[$k][12]+' but recomputes '+$term+'.')}
   $k++;if($null-ne$term){continue}
   if($null-eq$chosen){$chosen=$k-1;$goal=$r[1];if(-not$requireSpare){break search};continue}
   $spare=$k-1;break search}}
  if($k-ne$props.Count){throw ($lw+': proposals are recorded past the first eligible one (or its spare).')}
  if($requireSpare-and$null-ne$chosen-and$null-eq$spare){
   if($null-ne$l.chosen-or$null-ne$l.spare-or$l.check-cne'no-spare-proposal'){throw ($lw+': a beyond-range leg without a spare proposal is not refused.')};&$refuse $kind 'no-spare-proposal';return $false}
  if($hasSpare-and(($null-eq$spare)-ne($null-eq$l.spare)-or($null-ne$spare-and$l.spare-ne$spare))){throw ($lw+': the recorded spare is not the next eligible proposal.')}
  if($null-eq$chosen){if($null-ne$l.chosen){throw ($lw+': a refused leg records a choice.')};&$refuse $kind 'no-destination';return $false}
  if($l.chosen-ne$chosen){throw ($lw+': the recorded choice is not the first eligible proposal.')}
  if($kind-cne'move-probe'){$legTime=(Get-KmcChunk7StagedFlat $pos $goal)/$Speed
   if($time+$legTime-gt6.0){$f=(6.0-$time)/$legTime;$pos=@(([double]$pos[0]+([double]$goal[0]-[double]$pos[0])*$f),([double]$pos[1]+([double]$goal[1]-[double]$pos[1])*$f),([double]$pos[2]+([double]$goal[2]-[double]$pos[2])*$f));$time=6.0}
   else{$pos=$goal;$time+=$legTime}}
  if(-not(Test-KmcChunk7StagedNear (Get-KmcChunk7StagedVector $l.end $lw) $pos)-or[Math]::Abs([double]$l.timeAfter-$time)-gt1e-9){throw ($lw+': the simulated end or movement time differs.')}
  $side=-$side;$leg++
  $wants=if($kind-ceq'move-two-moves'){$time-le3.0}elseif($kind-ceq'move-exhaust'){$time-lt5.95}else{$false}
  if(-not$wants){. $next}
  elseif($leg-ge8){&$refuse $kind 'leg-limit';return $false}
 }
 if($li-ne$legs.Count-or$null-ne$Sim.refusal){throw ($Why+': a feasible simulation records a refusal or extra legs.')}
 return $true
}
# The recorded selection: every candidate in the fixed order (distances 8, 9, 7, 10 m, each over the fan) with its
# placement terms and simulation replayed; the selected one is the first feasible. Returns the selected candidate.
function Get-KmcChunk7StagedPlanSelection($Plan){
 if($null-eq$Plan){throw 'Staged actions lack their up-front geometry plan.'};$revision=Get-KmcChunk7StagedRevision ([string]$Plan.contract)
 foreach($f in @('weaponRange','mountCorpulence','plannedTargetCorpulence','envelope','speed')){if(-not(Test-KmcChunk7FiniteNumber $Plan.$f)-or[double]$Plan.$f-le0){throw 'The staged geometry plan lacks its inputs.'}}
 if([double]$Plan.plannedTargetCorpulence-ne0.7-or[double]$Plan.stopShortMargin-ne0.5-or$Plan.maxLegs-ne8-or
    (ConvertTo-Json -InputObject @($Plan.placementDistances|ForEach-Object{[double]$_}) -Compress)-cne(ConvertTo-Json -InputObject $script:KmcStagedPlacementDistances -Compress)-or
    (ConvertTo-Json -InputObject @($Plan.angles|ForEach-Object{[double]$_}) -Compress)-cne(ConvertTo-Json -InputObject $script:KmcStagedFan -Compress)){throw 'The staged geometry plan''s declared parameters differ.'}
 if([Math]::Abs([double]$Plan.envelope-([double]$Plan.mountCorpulence+[double]$Plan.plannedTargetCorpulence+[double]$Plan.weaponRange))-gt1e-6){throw 'The planned envelope is not the mount footprint, the planned target footprint and the weapon range.'}
 $mount=Get-KmcChunk7StagedVector $Plan.mountStart 'Staged plan';$rider=Get-KmcChunk7StagedVector $Plan.riderStart 'Staged plan'
 $candidates=@($Plan.candidates);$i=0;$selected=$null
 foreach($distance in $script:KmcStagedPlacementDistances){foreach($angle in $script:KmcStagedFan){
  if($i-ge$candidates.Count){break};$c=$candidates[$i];$why='Staged plan candidate '+$i
  if([double]$c.angle-ne$angle-or[double]$c.distance-ne$distance){throw ($why+': the candidate order differs from the declared order.')}
  $u=Get-KmcChunk7StagedRotation 0.0 1.0 $angle;$req=@(($mount[0]+$u[0]*$distance),$mount[1],($mount[2]+$u[1]*$distance))
  if(-not(Test-KmcChunk7StagedNear (Get-KmcChunk7StagedVector $c.requested $why) $req)){throw ($why+': the requested placement differs.')}
  if($c.walkable-isnot[bool]){throw ($why+': the walkable flag is mistyped.')};$goal=Get-KmcChunk7StagedVector $c.goal $why;$rd=Get-KmcChunk7StagedFlat $rider $goal
  $term=if(-not$c.walkable){'not-walkable'}elseif((Get-KmcChunk7StagedFlat $goal $req)-gt0.3){'placement-offset'}elseif([Math]::Abs($goal[1]-$mount[1])-gt1.0){'placement-rise'}elseif($rd-lt3.0-or$rd-gt20.0){'placement-spawn-range'}else{$null}
  if($null-eq$term){if(-not(Test-KmcChunk7StagedSimulation $c.simulation $mount $goal ([double]$Plan.envelope) ([double]$Plan.speed) $why $revision)){$term='plan-infeasible'}}
  elseif($null-ne$c.simulation){throw ($why+': a placement-refused candidate records a simulation.')}
  if([string]$c.refusal-cne[string]$term-or($null-eq$c.refusal)-ne($null-eq$term)){throw ($why+': the recorded refusal differs from its recomputation.')}
  $i++;if($null-eq$term){$selected=$c;break}}
  if($null-ne$selected){break}}
 if($i-ne$candidates.Count){throw 'The staged plan records candidates past the first feasible one or outside the declared order.'}
 if($null-eq$selected){if($null-ne$Plan.selected-or$candidates.Count-ne64){throw 'A refused staged plan must record every declared candidate and no selection.'};return $null}
 if($Plan.selected-ne$i-1){throw 'The staged plan did not select its first feasible candidate.'}
 $selected
}
# The plan bound to the run: the selected placement is the authorized target point, the planned envelope is the
# spawned target's, and the in-combat confirmation replays feasible from the mount's start of the first step.
function Assert-KmcChunk7StagedGeometryPlan($s,$o,$roles){
 $p=$s.geometryPlan;$selected=Get-KmcChunk7StagedPlanSelection $p
 if($null-eq$selected){throw 'The staged geometry plan admitted no placement; the case is uncovered on this fixture.'}
 # The authorization records the native float point; the plan the same point widened to double.
 if(-not(Test-KmcChunk7StagedNear (Get-KmcChunk7StagedVector $o.targetAuthorization.position 'Staged target authorization') (Get-KmcChunk7StagedVector $selected.goal 'Staged plan') 1e-4)){throw 'The target authorization did not place the selected staged candidate.'}
 if([Math]::Abs([double]$p.weaponRange-[double]$s.weaponRangeMeters)-gt1e-4-or[Math]::Abs([double]$p.mountCorpulence-[double]$s.mountCorpulence)-gt1e-4-or[Math]::Abs([double]$s.envelope-[double]$p.envelope)-gt0.01){throw 'The spawned target''s envelope differs from the planned one.'}
 if((Get-KmcChunk7StagedFlat $p.riderStart $o.targetAuthorization.riderPosition)-gt0.05){throw 'The staged plan''s rider start is not the authorized rider position.'}
 $c=$s.geometryConfirmation;if($null-eq$c){throw 'Staged actions lack their in-combat plan confirmation.'}
 $start=Get-KmcChunk7StagedVector $c.mountStart 'Staged confirmation';$target=Get-KmcChunk7StagedVector $c.target 'Staged confirmation';$first=$s.steps[0].before
 $m=Get-KmcChunk7CombatActor $first $roles[1]
 if((Get-KmcChunk7StagedFlat $start $first.mountPosition)-gt0.05-or(Get-KmcChunk7StagedFlat $start $p.mountStart)-gt0.05){throw 'The staged confirmation did not start from mount A''s position.'}
 if((Get-KmcChunk7StagedFlat $target $selected.goal)-gt0.3-or[Math]::Abs((Get-KmcChunk7StagedFlat $first.mountPosition $target)-[double]$first.targetDistance)-gt0.01){throw 'The staged confirmation target is not the spawned target at its planned placement.'}
 if([Math]::Abs([double]$c.envelope-[double]$s.envelope)-gt1e-4-or$null-eq$m.native-or[Math]::Abs([double]$c.speed-[double]$m.native.speedMps)-gt1e-4){throw 'The staged confirmation does not use the actual envelope and the mount''s native speed.'}
 if(-not(Test-KmcChunk7StagedSimulation $c.simulation $start $target ([double]$c.envelope) ([double]$c.speed) 'Staged confirmation' (Get-KmcChunk7StagedRevision ([string]$p.contract)))){throw 'The selected staged plan was not feasible from the in-combat start.'}
}
function Assert-KmcChunk7StagedCombat($Request,$Observations){
 if(-not(Test-KmcChunk7OwnedPairsStaged $Request)){throw 'Staged evidence requires its closed owner-seed selection.'}
 Assert-KmcChunk7OwnedPairsSelection $Request
 if((Get-KmcChunk7OwnedLoadCount $Request)-ne1-or(Get-KmcChunk7OwnedWriteCount $Request)-ne0){throw 'Staged actions retain one native load and zero writes.'}
 $o=$Observations;$roles=@(Get-KmcChunk7OwnedPairIds $Request)
 Assert-KmcChunk7OwnerSeedDerivedCombatStart $Request $o
 foreach($key in @('r1','firstActionMount','localFailure','registeredDisable','removalRefusal','pendingSurvivor','nativeLife','attackModes','reaction')){if($null-ne$o.PSObject.Properties[$key]){throw 'Staged actions cannot carry another combat case''s evidence.'}}
 if(@($o.activations).Count-ne0-or@($o.inputs).Count-ne0){throw 'Staged actions record no primary activation and no relationship input.'}
 $s=$o.staged
 # owner-seed-staged-v2 (F6): geometry plan revision 2 and the approach shot's native range record; v1 keeps its path.
 $stagedV2=$null-ne$s-and$s.contract-ceq'owner-seed-staged-v2'
 if($null-eq$s-or($s.contract-cne'owner-seed-staged-v1'-and-not$stagedV2)-or$s.riderId-cne$roles[0]-or$s.mountId-cne$roles[1]-or$s.weaponRanged-ne$true-or[string]$s.weapon-cnotmatch'^[0-9a-f]{32}$'-or-not(Test-KmcChunk7FiniteNumber $s.weaponRangeMeters)-or$s.weaponRangeMeters-le0-or$s.scrollUnits-ne4){throw 'Staged actions lack rider A''s ranged weapon or the declared scroll stack.'}
 Assert-KmcChunk7Boolean $s.complete $true
 if($null-eq$s.PSObject.Properties['rules']-or$null-eq$s.rules-or$s.rules.dropped-ne0-or@($s.rules.errors).Count-ne0){throw 'Staged rule observation dropped or failed.'}
 foreach($f in @('envelope','mountCorpulence','targetCorpulence')){if(-not(Test-KmcChunk7FiniteNumber $s.$f)-or$s.$f-le0){throw 'Staged weapon envelope is missing.'}}
 if([Math]::Abs([double]$s.envelope-([double]$s.mountCorpulence+[double]$s.targetCorpulence+[double]$s.weaponRangeMeters))-gt0.01){throw 'Staged weapon envelope is not the pair''s native footprint plus weapon range.'}
 if($null-eq$s.defensiveCheck-or$s.defensiveCheck.covered-ne$false-or$s.defensiveCheck.reason-isnot[string]-or-not$s.defensiveCheck.reason.StartsWith('The pair never stands inside',[StringComparison]::Ordinal)){throw 'The unexercised defensive check must be recorded as uncovered with its reason.'}
 $ab=$s.scrollAbility
 if($null-eq$ab-or$ab.blueprint-cne'5590652e1c2225c4ca30c4a699ab3649'-or$ab.sourceItemBlueprint-cne'cd635d5720937b044a354dba17abad8d'-or$ab.caster-cne$roles[0]-or$ab.availableForCast-ne$true){throw 'The disposable scroll does not give rider A its native Cure Light Wounds.'}
 $plan=@(Get-KmcChunk7StagedPlan);$declared=@($s.plan)
 if($declared.Count-ne$plan.Count){throw 'Staged plan differs.'}
 for($i=0;$i-lt$plan.Count;$i++){if($declared[$i].row-cne$plan[$i][0]-or$declared[$i].kind-cne$plan[$i][1]-or$declared[$i].round-ne$plan[$i][2]){throw 'Staged plan differs.'}}
 if(-not(Test-KmcExactJsonInteger $s.firstRound)){throw 'Staged actions lack their first native round.'};$n=[long]$s.firstRound
 $steps=@($s.steps);if($steps.Count-ne$plan.Count){throw 'Staged actions need every planned native step.'}
 # The whole leg plan was chosen up front, before any lease, placement or input, and confirmed in combat.
 $planContract=if($null-ne$s.geometryPlan-and$null-ne$s.geometryPlan.PSObject.Properties['contract']){[string]$s.geometryPlan.contract}else{''}
 if((Get-KmcChunk7StagedRevision $planContract)-ne$(if($stagedV2){2}else{1})){throw 'The staged contract and its geometry plan revision differ.'}
 Assert-KmcChunk7StagedGeometryPlan $s $o $roles
 for($i=0;$i-lt$steps.Count;$i++){$has=$null-ne$steps[$i].PSObject.Properties['nativeRangeAtStop'];if($has-ne($stagedV2-and$steps[$i].kind-ceq'attack-ranged-approach')){throw 'The approach shot''s native range record does not match its staged contract.'}}
 $spends=0
 for($i=0;$i-lt$steps.Count;$i++){$step=$steps[$i];$why='Staged step '+$i+' '+$plan[$i][1]
  if($step.index-ne$i-or$step.row-cne$plan[$i][0]-or$step.kind-cne$plan[$i][1]-or$step.round-ne$n+$plan[$i][2]){throw ($why+': plan position or native round differs.')}
  Assert-KmcChunk7StagedState $step.before ($why+' before');Assert-KmcChunk7StagedSettled $step.after ($why+' after')
  if($step.before.currentActor-cne$roles[0]-or$step.after.currentActor-cne$roles[0]-or$step.before.turn-ne$step.after.turn){throw ($why+': the step left rider A''s native turn.')}
  Assert-KmcChunk7StagedPairBIdle $step.before $step.after $roles
  if(@($step.rules|Where-Object{$_.opportunity-eq$true-or$_.actor-cne$roles[0]}).Count-ne0){throw ($why+': an attack of opportunity or a foreign attack landed inside the staged window.')}
  switch -CaseSensitive([string]$step.kind){
   'cast-standard-scroll'{Assert-KmcChunk7StagedCast $step $o $s $roles $why;$spends++;if(@($step.rules).Count-ne0){throw ($why+': the cast window holds an attack.')}}
   'attack-ranged'{Assert-KmcChunk7StagedRanged $step $o $s $roles $false $why}
   'attack-ranged-approach'{Assert-KmcChunk7StagedRanged $step $o $s $roles $true $why}
   default{
    if(@($step.rules).Count-ne0){throw ($why+': the movement window holds an attack.')}
    $legs=@($step.legs);$bounded=$step.kind-cin@('move-two-moves','move-exhaust')
    if(($bounded-and($legs.Count-lt1-or$legs.Count-gt8))-or(-not$bounded-and$legs.Count-ne1)){throw ($why+': the native leg count differs.')}
    for($j=0;$j-lt$legs.Count;$j++){[void](Assert-KmcChunk7StagedLeg $legs[$j] $o $roles ($why+' leg '+$j))}
    if($legs[0].traceSequenceBefore-lt$step.traceSequenceBefore-or$legs[-1].traceSequenceAfter-gt$step.traceSequenceAfter){throw ($why+': a leg lies outside its step window.')}
   }
  }
 }
 # One native activation per declared round: the same turn identity, rider A's single cost commitment,
 # and no turn transition, preparation or round boundary between its first and last step.
 $transitions=@('prepare-before','prepare-after','clear-before','clear-after','combat-clear-before','combat-clear-after','round-state-before','round-state-after','round-handler-before','round-handler-after','ai-round-before','ai-round-after','turn-end-before','turn-end-after','chunk7-end-input-before','chunk7-end-input-after')
 $turns=@()
 foreach($round in 0,1,2,3){
  $group=@(for($i=0;$i-lt$plan.Count;$i++){if($plan[$i][2]-eq$round){$steps[$i]}})
  if(@($group|ForEach-Object{[string]$_.before.turn}|Select-Object -Unique).Count-ne1){throw ('Staged round N+'+$round+' spans more than one native turn.')}
  $window=@(Get-KmcChunk7StagedTrace $o $group[0].traceSequenceBefore $group[-1].traceSequenceAfter)
  if(@($window|Where-Object{[string]$_.boundary-cin$transitions}).Count-ne0){throw ('Staged round N+'+$round+' holds a native turn transition inside its activation.')}
  if(@($window|Where-Object{$_.boundary-ceq'cost-after'-and$_.state.actor-ceq$roles[0]}).Count-ne1){throw ('Staged round N+'+$round+' does not carry exactly one rider cost commitment.')}
  Assert-KmcChunk7NoCostBoundary $o.allocationTrace $group[0].traceSequenceBefore $group[-1].traceSequenceAfter @($roles[2],$roles[3]) ('Staged round N+'+$round)
  # Every pair cost boundary of the activation is stamped by the same rider turn, round and Preparing/Acting window.
  $stamps=@($window|Where-Object{$_.boundary-cin@('cost-before','cost-after','actor-cost-before','actor-cost-after')-and$_.state.actor-cin@($roles[0],$roles[1])})
  if(@($stamps|Where-Object{[string]$_.turn-cne[string]$group[0].before.turn-or$_.round-ne$n+$round-or$_.currentActor-cne$roles[0]-or[string]$_.turnStatus-cnotin@('Preparing','Acting')}).Count-ne0){throw ('Staged round N+'+$round+' has a cost boundary outside rider A''s own native activation.')}
  $turns+=[string]$group[0].before.turn
 }
 if(@($turns|Select-Object -Unique).Count-ne4){throw 'Staged rounds reuse one native turn identity.'}
 # Exactly the two shots were recorded, each inside its own window.
 if((ConvertTo-Json -InputObject @($s.rules.events) -Depth 10 -Compress)-cne(ConvertTo-Json -InputObject @($steps|ForEach-Object{@($_.rules)}) -Depth 10 -Compress)){throw 'The staged rule observer recorded attacks outside the step windows.'}
 # C6D-move-cast-move: both short legs fit the mount's first Move; the cast between them changed nothing for the mount.
 $l0=$steps[0].legs[0];$l2=$steps[2].legs[0]
 $m0=Get-KmcChunk7CombatActor $l0.after $roles[1];$m2b=Get-KmcChunk7CombatActor $l2.before $roles[1];$m2a=Get-KmcChunk7CombatActor $l2.after $roles[1]
 if($l2.after.mountUsedTwoMove-ne$false-or[double]$m2a.move-gt3.001){throw 'C6D-move-cast-move: two short legs exceeded one native Move.'}
 if(-not(Test-KmcChunk7StagedClose $m2b.move $m0.move)-or[double]$m2a.move-le[double]$m0.move+0.001){throw 'C6D-move-cast-move: the second leg did not continue the same native Move allocation.'}
 # It starts from the first leg's retained allocation and consumes the remaining native movement time of that same allocation.
 if($l2.before.mountUsedOneMove-ne$true-or-not(Test-KmcChunk7FiniteNumber $m2b.native.remainingNativeTime)-or-not(Test-KmcChunk7FiniteNumber $m2a.native.remainingNativeTime)-or
    [double]$m2b.native.remainingNativeTime-ge5.999-or[double]$m2a.native.remainingNativeTime-ge[double]$m2b.native.remainingNativeTime-0.001){throw 'C6D-move-cast-move: the second leg did not consume the retained native movement time.'}
 # C6D-double-move-ranged: the shot follows the mount's two native Moves inside the same activation with the rider's Standard available.
 $shot=$steps[4];$r4=Get-KmcChunk7CombatActor $shot.before $roles[0];$m4=Get-KmcChunk7CombatActor $shot.before $roles[1]
 if([double]$r4.standard-gt0.001-or$shot.before.riderHasStandard-ne$true){throw 'C6D-double-move-ranged: rider A''s Standard was not available immediately before the shot.'}
 if($shot.before.mountUsedTwoMove-ne$true-or[double]$m4.move-le3.001){throw 'C6D-double-move-ranged: the shot did not follow the mount''s two native Moves.'}
 $r8=Get-KmcChunk7CombatActor $steps[8].before $roles[0]
 if([double]$r8.standard-gt0.001-or$steps[8].before.riderHasStandard-ne$true){throw 'C6D-double-move-ranged: the next-turn scroll control did not start from the renewed Standard.'}
 # Ranged approach row: beyond range, the mount's whole native movement, the native refusal, then the approach shot.
 if([double]$steps[5].after.targetDistance-lt[double]$s.envelope+2.5){throw 'Ranged approach: the pair did not move beyond weapon range.'}
 $last=@($steps[6].legs)[-1];if([double](Get-KmcChunk7CombatActor $last.after $roles[1]).move-lt5.95){throw 'Ranged approach: the mount''s native movement was not exhausted.'}
 if([double](Get-KmcChunk7CombatActor $steps[7].legs[0].before $roles[1]).move-lt5.95){throw 'Ranged approach: the probe did not start from native exhaustion.'}
 # The disposable stack: built on the equipped entity, two native spends, released with its slot restored.
 $item=$s.scroll
 if($null-eq$item-or$item.blueprint-cne'cd635d5720937b044a354dba17abad8d'-or$item.requestedCount-ne4-or$item.equipped.exactSlot-ne$true-or$item.equipped.count-ne1-or$item.equipped.item-eq0-or
    $item.stacked.exactSlot-ne$true-or$item.stacked.count-ne4-or$item.stacked.item-ne$item.equipped.item){throw 'The disposable scroll stack was not built on its equipped entity.'}
 if($item.beforeCleanup.exactSlot-ne$true-or$item.beforeCleanup.count-ne4-$spends-or$spends-ne2){throw 'The disposable scroll stack does not conserve its two observed native spends.'}
 foreach($f in @('disposed','slotRestored','noOwnedItemResident','ownedItemReleased')){Assert-KmcChunk7Boolean $item.$f $true}
 $ct=$s.castTrace
 if($null-eq$ct-or$ct.faults-ne0-or$ct.dropped-ne0-or$ct.identityRegistry.faults-ne0-or$ct.identityRegistry.released-ne$true-or$ct.identityRegistry.retainedCount-ne0){throw 'The native cast observer is incomplete or not retired.'}
 if($null-eq$s.PSObject.Properties['originalSlots']-or$null-eq$s.originalSlots-or$s.originalSlots.restored-ne$true){throw 'The original quick slots were not observed restored.'}
 foreach($field in @('pairResidueAbsent','nativeCombatEnded','initiativeInputsRestored','firstPairAiRestored','controlsSettled','inheritedPetsPreserved')){Assert-KmcChunk7Boolean $o.cleanup.$field $true}
 Assert-KmcChunk7Boolean $o.cleanup.ownedHorseRemoved $false;Assert-KmcChunk7Boolean $o.cleanup.nativeSaveWrittenByThisDriver $false;Assert-KmcChunk7Boolean $o.cleanup.delegatedPersistenceCleanup $false
}
