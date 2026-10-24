# Closed RT two-pair reader. Depends on shared Chunk7 world/trace and combat snapshot helpers.
Set-StrictMode -Version Latest
function Test-KmcChunk7RealtimeScenario([string]$Scenario){$Scenario-cin@('chunk7-two-pair-realtime-core','chunk7-two-pair-realtime-casting','chunk7-two-pair-realtime-charge')}
function Get-KmcChunk7RealtimeRows([string]$Scenario){
    if(-not(Test-KmcChunk7RealtimeScenario $Scenario)){throw 'Unknown two-pair RT case.'}
    @($Scenario,'C7-combat-fixture','C7-combat-native-mounts','C7-combat-restored-mounts','C7-combat-cleanup','C7-combat-runtime','C7-combat-interrupted')
    if($Scenario-ceq'chunk7-two-pair-realtime-core'){@('C7-rt-charge-default-off','C7-rt-concurrent-primary','C7-rt-companion-native-action','C7-rt-player-click-integrity','C7-rt-local-cancel','C7-rt-unmounted-native-control','C7-combat-local-cleanup-failure')}
    elseif($Scenario-ceq'chunk7-two-pair-realtime-casting'){@('C7-rt-native-casting','C7-rt-casting-neighbour')}
    else{@('C7-rt-charge-native-success','C7-rt-charge-neighbour-movement','C7-rt-charge-native-cancel')}
}
function Get-KmcChunk7RealtimeRuntimeRows {
    @('chunk7-two-pair-realtime-core','chunk7-two-pair-realtime-casting','chunk7-two-pair-realtime-charge'|ForEach-Object{Get-KmcChunk7RealtimeRows $_}|Select-Object -Unique)
}
function Assert-KmcChunk7RealtimeArtifact($Request,$Manifest,[string]$Status='PASS'){
    $present=@($Manifest.artifacts|Where-Object{$_.relativePath-ceq'chunk7-realtime-evidence.json'-or$_.kind-ceq'chunk7-realtime-evidence'})
    if(-not(Test-KmcChunk7RealtimeScenario (Get-KmcChunk7SelectedScenario $Request))){if($present.Count-ne0){throw 'Foreign scenario supplied RT evidence.'};return}
    if($present.Count-eq0-and$Status-cne'PASS'){return}
    $artifact=Read-KmcChunk7ManifestArtifact $Request $Manifest 'chunk7-realtime-evidence.json' 'chunk7-realtime-evidence'
    Assert-KmcChunk7RealtimeEvidence $Request $artifact $Status
}
function Assert-KmcChunk7RealtimeSnapshot($Snapshot,[string[]]$Ids){
    Assert-KmcChunk7ExactIds @($Snapshot.actors) $Ids
    if($null-ne$Snapshot.currentActor-or-not(Test-KmcExactJsonInteger $Snapshot.frame)-or$Snapshot.frame-lt0){throw 'RT observation contains a native TB context or invalid frame.'}
    foreach($id in $Ids){$null=Get-KmcChunk7CombatActor $Snapshot $id}
}
function Assert-KmcChunk7RealtimeDisplacement($Before,$After,[string]$Actor){
    $a=Get-KmcChunk7CombatActor $Before $Actor;$b=Get-KmcChunk7CombatActor $After $Actor;$distance=0.0
    foreach($axis in @('x','y','z')){if(-not(Test-KmcChunk7FiniteNumber $a.position.$axis)-or-not(Test-KmcChunk7FiniteNumber $b.position.$axis)){throw 'RT physical position missing or invalid.'};$distance+=[Math]::Pow([double]$b.position.$axis-[double]$a.position.$axis,2)}
    if([Math]::Sqrt($distance)-le0.02){throw 'Native RT command has no physical displacement.'}
}
function Assert-KmcChunk7RealtimeCost($Trace,[string]$Actor,[long]$Command,[string]$Type,[int]$Start,[int]$End,[switch]$FullRound){
    $events=@($Trace.events|Where-Object{$_.command-eq$Command-and$_.boundary-cin@('cost-before','cost-after')})
    if($Command-eq0-or$events.Count-ne2){throw 'RT actor cost is missing or repeated for its exact native command.'}
    $before=@($events|Where-Object boundary -CEQ 'cost-before');$after=@($events|Where-Object boundary -CEQ 'cost-after')
    if($before.Count-ne1-or$after.Count-ne1){throw 'RT native cost boundary pair differs.'};$before=$before[0];$after=$after[0]
    foreach($e in $events){if($e.commandActor-cne$Actor-or$e.state.actor-cne$Actor-or$e.commandType-cne$Type-or$e.actionType-cne'Standard'-or$e.frame-lt$Start-or$e.frame-gt$End){throw 'RT cost escaped actor, command, type or native window.'}}
    if($after.sequence-le$before.sequence-or$after.state.standard-le$before.state.standard-or($FullRound-and$after.state.move-le$before.state.move)){throw 'Native RT expenditure was not applied at its exact callback.'}
    foreach($e in $events){foreach($field in @('standard','move','swift','reactions','reactionCooldown')){if(-not(Test-KmcChunk7FiniteNumber $e.state.$field)-or$e.state.$field-lt0){throw 'RT native cost contains invalid resource observation.'}}}
}
function Assert-KmcChunk7RealtimePrimary($Attack,$Trace,[string]$Actor,[string]$Caster,[string]$Target,[string[]]$Ids){
    foreach($s in @($Attack.before,$Attack.after)){Assert-KmcChunk7RealtimeSnapshot $s $Ids}
    if($Attack.casterId-cne$Caster-or$Attack.executorId-cne$Caster-or$Attack.selectionAfterInput-ceq$Caster-or$Attack.selectionAfterInput-cnotin$Ids-or
       $Attack.nativeCastDelta-ne1-or$Attack.nativeRefusalDelta-ne0-or$Attack.command-eq0-or$Attack.outcome.actorId-cne$Actor-or
       $Attack.outcome.resourceOwnerId-cne$Actor-or$Attack.outcome.targetId-cne$Target-or$Attack.outcome.result-cne'Success'-or$Attack.outcome.completedAttacks-ne1){throw 'RT primary exact caster/action-owner outcome differs.'}
    Assert-KmcChunk7Boolean $Attack.clicked $true;Assert-KmcChunk7Boolean $Attack.outcome.nativeAttackRule $true;Assert-KmcChunk7Boolean $Attack.outcome.singleAttack $true
    $cost=@($Trace.events|Where-Object{$_.boundary-ceq'cost-after'-and$_.state.actor-ceq$Actor-and$_.commandType-ceq'KingmakerMountedCombat.Integration.MountedPairAttackCommand'-and$_.frame-ge$Attack.before.frame-and$_.frame-le$Attack.after.frame})
    if($cost.Count-ne1){throw 'RT primary lacks one native action cost.'}
    Assert-KmcChunk7RealtimeCost $Trace $Actor $cost[0].command 'KingmakerMountedCombat.Integration.MountedPairAttackCommand' $Attack.before.frame $Attack.after.frame
    return [long]$cost[0].command
}
function Assert-KmcChunk7RealtimeOwnerStable($Before,$After,[string]$Rider,[string]$Mount){
    # Pair-owner records carry the registry's N-form actor keys; actors are D-form UniqueIds.
    $left=@($Before.owners|Where-Object{$_.state-ceq'Active'-and(Test-KmcChunk7OwnerActorMatch $_.riderId $Rider)});$right=@($After.owners|Where-Object{$_.state-ceq'Active'-and(Test-KmcChunk7OwnerActorMatch $_.riderId $Rider)})
    if($left.Count-ne1-or$right.Count-ne1-or-not(Test-KmcChunk7OwnerActorMatch $left[0].mountId $Mount)-or-not(Test-KmcChunk7OwnerActorMatch $right[0].mountId $Mount)-or$left[0].id-cne$right[0].id-or$left[0].generation-ne$right[0].generation-or$left[0].generation-le0){throw 'RT execution changed exact stable pair custody.'}
}
function Assert-KmcChunk7RealtimeStop($Stop,[string[]]$Ids){
    foreach($s in @($Stop.before,$Stop.after)){Assert-KmcChunk7RealtimeSnapshot $s $Ids}
    if($Stop.before.frame-ne$Stop.after.frame-or$Stop.before.gameTicks-ne$Stop.after.gameTicks-or$Stop.neighbourOwnerBefore-cne$Stop.neighbourOwnerAfter-or
       $Stop.neighbourGenerationBefore-ne$Stop.neighbourGenerationAfter-or$Stop.neighbourGenerationBefore-le0-or
       (ConvertTo-KmcChunk7Canonical $Stop.neighbourCommandsBefore)-cne(ConvertTo-KmcChunk7Canonical $Stop.neighbourCommandsAfter)){throw 'Native local Stop changed neighbour identity or command custody.'}
    if(@($Stop.neighbourCommandsBefore|Where-Object{-not$_.finished-and$_.actorId-cin@($Ids[2],$Ids[3])-and$_.identity-ne0}).Count-eq0){throw 'Cancellation has no concurrent live neighbour command.'}
    Assert-KmcChunk7SameCombatBudget $Stop.before $Stop.after @($Ids[2],$Ids[3])
}
function Assert-KmcChunk7RealtimeShell($Shell,[long]$Command,[string]$Actor,[string]$Target){
    if($Shell.identity-ne$Command-or$Command-eq0-or$Shell.actorId-cne$Actor-or$Shell.targetId-cne$Target-or$Shell.type-cne'Kingmaker.UnitLogic.Commands.UnitUseAbility'){throw 'Native overlapping shell binding differs.'}
    Assert-KmcChunk7Boolean $Shell.finished $false
}
function Assert-KmcChunk7RealtimeEvidence($Request,$Artifact,[string]$Status){
    Assert-KmcChunk7ArtifactEnvelope $Request $Artifact 'chunk7-realtime-evidence'
    if(-not(Test-KmcChunk7RealtimeScenario (Get-KmcChunk7SelectedScenario $Request))){throw 'RT artifact has an unknown closed case.'}
    if($Status-ceq'PASS'-and$Artifact.status-cne'PASS'){throw 'PASS RT result requires a PASS artifact.'};if($Artifact.status-cne'PASS'){return}
    $o=$Artifact.observations;$ids=@($o.actorIds);$trace=$o.allocationTrace
    $owned=Test-KmcChunk7OwnedPairsScenario $Request.scenario
    if($owned){Assert-KmcChunk7OwnedActionFixture $Request $o}else{
    if($o.scenario-cne$Request.scenario-or$o.workingSha256-cne$Request.fixture.working.sha256-or$o.workingSha256-cne'5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6'-or
       $ids.Count-ne4-or@($ids|Select-Object -Unique).Count-ne4-or$ids[0]-cne'b6628a77-4962-47a4-a17c-88d9836fc9d5'-or$ids[1]-cne'd79a4f6c-b74e-4868-95bd-533899131acb'-or$ids[2]-cne'd17c8fd0-6627-40c4-bf9a-024cf001c1f7'-or
       $o.targetId-cin$ids-or$o.targetGroupId-cne('KMC.RuntimeHostile.'+$Request.runId+':two-pair-target')){throw 'RT exact Working fixture identity differs.'}
        }
    [Guid]$g=[Guid]::Empty;if(-not[Guid]::TryParse($ids[3],[ref]$g)-or-not[Guid]::TryParse($o.targetId,[ref]$g)){throw 'RT native dynamic actor identity invalid.'}
    foreach($field in @('realTime','selectedRealtimeCaseComplete')){Assert-KmcChunk7Boolean $o.$field $true}
    foreach($field in @('initiativeInputsChanged','fullRealtimeCombatProof','naturalUnconsciousStimulusProven')){Assert-KmcChunk7Boolean $o.$field $false}
    Assert-KmcChunk7Boolean $o.chargeSettingAtAdmission ((Get-KmcChunk7SelectedScenario $Request)-ceq'chunk7-two-pair-realtime-charge')
    if(@($o.initiativeInputs).Count-ne4){throw 'RT original initiative observations incomplete.'}
    foreach($row in $o.initiativeInputs){if($row.before-ne$row.input-or$row.actorId-cnotin$ids){throw 'RT setup changed initiative.'}}
    if(-not$owned){if($o.acquisition.nativeRangerCommitCount-ne4-or$o.acquisition.horseFactRankAtCommit-ne1-or$o.acquisition.horsePresentAtNativeCommit-ne$true){throw 'Native Horse acquisition receipt differs.'}}
    Assert-KmcChunk7CheckpointTrace $trace $Request
    if($o.nativeEffects.dropped-ne0-or@($o.nativeCompletion.errors).Count-ne0-or$o.nativeCompletion.traceComplete-ne$true-or@($o.nativeRounds.errors).Count-ne0){throw 'RT raw observer is incomplete.'}
    # Owner-seed-derived combat restored both pairs mounted: its admission replaces the native Mount inputs.
    $derived=$owned-and(Test-KmcChunk7OwnedPairsSeedDerivedCombat $Request)
    if($derived){Assert-KmcChunk7OwnerSeedDerivedCombatStart $Request $o}else{
    $mounts=@($o.inputs|Where-Object{$_.label-cin@('mount-a','mount-b')});if($mounts.Count-ne2){throw 'Two native mounts missing.'}
    for($i=0;$i-lt2;$i++){$m=$mounts[$i];if($m.casterId-cne$ids[$i*2]-or$m.targetId-cne$ids[$i*2+1]-or$m.abilityGuid-cne'f053faad986631688defa003cd7bda0e'-or$m.nativeCastDelta-ne1-or$m.nativeRefusalDelta-ne0-or$m.generationAtInit-lt0){throw 'Native RT mount binding differs.'};foreach($f in @('clicked','terminal','processTerminal')){Assert-KmcChunk7Boolean $m.$f $true}}}
    if((Get-KmcChunk7SelectedScenario $Request)-ceq'chunk7-two-pair-realtime-core') {Assert-KmcChunk7RealtimeCore $o $ids $trace;if($derived){Assert-KmcChunk7OwnerSeedCompanionAction $Request $o;Assert-KmcChunk7OwnerSeedPlayerClick $Request $o}elseif($null-ne$o.PSObject.Properties['companionAction']-or$null-ne$o.PSObject.Properties['playerClick']){throw 'Only owner-seed-derived RT records an unrelated companion action or player-click window.'}}
    elseif((Get-KmcChunk7SelectedScenario $Request)-ceq'chunk7-two-pair-realtime-casting'){Assert-KmcChunk7RealtimeCasting $o $ids $trace}
    else{Assert-KmcChunk7RealtimeCharge $o $ids $trace}
    if($owned){foreach($field in @('pairResidueAbsent','nativeCombatEnded','initiativeInputsRestored','firstPairAiRestored','controlsSettled','inheritedPetsPreserved')){Assert-KmcChunk7Boolean $o.cleanup.$field $true};Assert-KmcChunk7Boolean $o.cleanup.ownedHorseRemoved $false;Assert-KmcChunk7Boolean $o.cleanup.nativeProgressionRequiresExternalWorkingRestoration $false}else{foreach($field in @('pairResidueAbsent','ownedHorseRemoved','nativeCombatEnded','initiativeInputsRestored','firstPairAiRestored','controlsSettled','nativeProgressionRequiresExternalWorkingRestoration')){Assert-KmcChunk7Boolean $o.cleanup.$field $true}}
    Assert-KmcChunk7Boolean $o.cleanup.nativeSaveWrittenByThisDriver $false;Assert-KmcChunk7Boolean $o.cleanup.delegatedPersistenceCleanup $false
}
function Assert-KmcChunk7RealtimeCore($o,[string[]]$ids,$trace){
    if(@($o.defaultOffCharge).Count-ne2){throw 'Both default-off Charge controls require evidence.'}
    for($i=0;$i-lt2;$i++){$r=$o.defaultOffCharge[$i];if($r.casterId-cne$ids[$i*2]-or$r.reason-cne'Enable Mounted Charge in the KMC mod settings.'-or$r.castDelta-ne0){throw 'Default-off native Charge refusal differs.'};foreach($f in @('featureEnabled','available','visible','nativeFactPresent','clicked','inputAttempted')){Assert-KmcChunk7Boolean $r.$f $false}
        if($r.nativeAbilityFacts-isnot[Array]-or$r.nativeAbilityFacts-ccontains'd79eaec224a7a832e738eb81baef9d49'-or$r.nativeAbilityFacts-cnotcontains'3af2b81f4d72bbb30501fa730fcdf36e'-or$r.stimulus-cne'read-only native ability inventory and availability; default-off fact is not leased'){throw 'Default-off native Charge fact/visibility contract differs.'}
        Assert-KmcChunk7SameCombatBudget $r.before $r.after $ids}
    $windows=@($o.realtimeWindows);if($windows.Count-ne2-or$windows[0].kind-cne'two-native-primary-inputs'-or$windows[1].kind-cne'native-stop-local'){throw 'RT core windows differ.'}
    $w=$windows[0];$a=Assert-KmcChunk7RealtimePrimary $w.a $trace $ids[0] $ids[0] $o.targetId $ids;$b=Assert-KmcChunk7RealtimePrimary $w.b $trace $ids[3] $ids[2] $o.targetId $ids
    if($a-eq$b-or$w.a.before.frame-ne$w.b.before.frame-or@($w.admittedShells).Count-ne2){throw 'Two RT native primary requests are not concurrent independent commands.'}
    Assert-KmcChunk7RealtimeShell $w.admittedShells[0] $w.a.command $ids[0] $o.targetId;Assert-KmcChunk7RealtimeShell $w.admittedShells[1] $w.b.command $ids[2] $o.targetId
    if(@($w.before.owners|Where-Object state -CEQ 'Active').Count-ne2-or@($w.after.owners|Where-Object state -CEQ 'Active').Count-ne2){throw 'Concurrent RT execution lost independent owners.'}
    foreach($i in 0..1){Assert-KmcChunk7RealtimeOwnerStable $w.before $w.after $ids[2*$i] $ids[2*$i+1]}
    $w=$windows[1];$null=Assert-KmcChunk7RealtimePrimary $w.neighbour $trace $ids[3] $ids[2] $o.targetId $ids
    if($w.movement.casterId-cne$ids[0]-or$w.movement.executorId-cne$ids[1]-or$w.movement.command-eq0){throw 'Local move escaped exact mover.'};Assert-KmcChunk7Boolean $w.movement.clicked $true
    Assert-KmcChunk7RealtimeDisplacement $w.movement.before $w.stop.before $ids[1];Assert-KmcChunk7RealtimeStop $w.stop $ids
    $u=$o.unmountedControl
    if($u.actorId-cne$ids[0]-or$u.commandType-cne'Kingmaker.UnitLogic.Commands.UnitAttack'-or$u.selectionAfterInput-cne$ids[2]-or@($u.before.owners|Where-Object{$_.state-ceq'Active'-and(Test-KmcChunk7OwnerActorMatch $_.riderId $ids[0])}).Count-ne0){throw 'Unmounted ordinary control retained mounted authority.'}
    Assert-KmcChunk7Boolean $u.clicked $true;Assert-KmcChunk7Boolean $u.nativeStopSent $true;Assert-KmcChunk7Boolean $u.terminal $true
    $d=@($o.inputs|Where-Object label -CEQ 'rt-native-dismount-a');if($d.Count-ne1-or$d[0].casterId-cne$ids[0]-or$d[0].targetId-cne$ids[0]-or$d[0].abilityGuid-cne'3af2b81f4d72bbb30501fa730fcdf36e'-or$d[0].nativeCastDelta-ne1-or$d[0].nativeRefusalDelta-ne0){throw 'Native independent Dismount input is missing.'}
    foreach($f in @('clicked','terminal','processTerminal')){Assert-KmcChunk7Boolean $d[0].$f $true}
    $events=@($u.effectsAfter.events|Select-Object -Skip @($u.effectsBefore.events).Count);$hits=@($events|Where-Object{$_.kind-ceq'weapon-resolved'-and$_.actor-ceq$ids[0]-and$_.target-ceq$o.targetId})
    if($hits.Count-ne1-or@($u.after.owners|Where-Object{$_.id-ceq$u.neighbourOwner-and$_.state-ceq'Active'-and(Test-KmcChunk7OwnerActorMatch $_.riderId $ids[2])}).Count-ne1){throw 'Unmounted native delivery repeated or changed neighbour custody.'}
    Assert-KmcChunk7RealtimeCost $trace $ids[0] $u.command 'Kingmaker.UnitLogic.Commands.UnitAttack' $u.before.frame $u.after.frame
    $f=$o.localFailure;if($f.failureCount-ne1-or$f.injectedPair-cne'b'){throw 'Selected local cleanup failure differs.'};Assert-KmcChunk7Boolean $f.healthyNeighbourVisited $true
    if(@($f.held.owners|Where-Object{(Test-KmcChunk7OwnerActorMatch $_.riderId $ids[2])-and(Test-KmcChunk7OwnerActorMatch $_.mountId $ids[3])-and$_.retainsObligations}).Count-ne1){throw 'RT failed owner lost cleanup custody.'}
    Assert-KmcChunk7SameCombatBudget $f.before $f.held @($ids[1])
}
function Assert-KmcChunk7RealtimeCasting($o,[string[]]$ids,$trace){
    foreach($i in 0..1){Assert-KmcChunk7RealtimeOwnerStable $o.casting.before $o.casting.after $ids[2*$i] $ids[2*$i+1]}
    $c=$o.casting;$aIndex=if($c.casterId-ceq$ids[0]){3}elseif($c.casterId-ceq$ids[2]){0}else{throw 'Caster is outside the two mounted riders.'}
    $casterIndex=if($aIndex-eq3){2}else{0}
    if($c.targetId-cne$ids[1]-or$c.blueprint-cne'5590652e1c2225c4ca30c4a699ab3649'-or$c.executorId-cne$c.casterId-or$c.selectionAfterInput-ceq$c.casterId-or
       $c.damageBefore-ne0-or$c.wound-le0-or$c.damageAfter-ge$c.wound-or$c.requestedNativeDamage-le0-or$c.nativeDamageDifficulty-le0){throw 'Native prepared cast actor/target/wound differs.'}
    foreach($f in @('clicked','slotAvailableBefore','shellFinished','executionEnded')){Assert-KmcChunk7Boolean $c.$f $true};Assert-KmcChunk7Boolean $c.slotAvailableAfter $false
    Assert-KmcChunk7RealtimeCost $trace $c.casterId $c.command 'Kingmaker.UnitLogic.Commands.UnitUseAbility' $c.before.frame $c.after.frame
    $null=Assert-KmcChunk7RealtimePrimary $c.neighbour $trace $ids[$aIndex] $ids[$casterIndex] $o.targetId $ids
    if(@($c.admittedShells).Count-ne2-or$c.before.frame-ne$c.neighbour.before.frame){throw 'Mounted casting and neighbour action did not overlap.'}
    Assert-KmcChunk7RealtimeShell $c.admittedShells[0] $c.command $c.casterId $ids[1];Assert-KmcChunk7RealtimeShell $c.admittedShells[1] $c.neighbour.command $ids[$casterIndex] $o.targetId
    $events=@($c.effectsAfter.events|Select-Object -Skip @($c.effectsBefore.events).Count)
    if(@($events|Where-Object{$_.kind-ceq'heal-after'-and$_.actor-ceq$c.casterId-and$_.target-ceq$ids[1]-and$_.value-gt0}).Count-ne1-or
       @($events|Where-Object{$_.kind-ceq'damage-after'-and$_.actor-ceq$o.targetId-and$_.target-ceq$ids[1]-and$_.damage-gt0}).Count-ne1){throw 'Native damage/heal callbacks are absent or repeated.'}
    $healers=@($o.castingInventory|Where-Object{@($_.preparedSpells|Where-Object blueprint -CEQ $c.blueprint).Count-gt0})
    if($healers.Count-ne1-or$healers[0].actorId-cne$c.casterId-or@($healers[0].preparedSpells|Where-Object{$_.blueprint-ceq$c.blueprint-and$_.available-eq$true}).Count-ne1){throw 'Native prepared slot inventory differs from consumed exact caster.'}
}
function Assert-KmcChunk7RealtimeCharge($o,[string[]]$ids,$trace){
    $windows=@($o.realtimeWindows);if($windows.Count-ne2-or$windows[0].kind-cne'charge-success'-or$windows[1].kind-cne'charge-cancel'){throw 'Opt-in Charge success and cancellation windows required.'}
    $shells=[Collections.Generic.HashSet[long]]::new()
    foreach($c in $windows){
        foreach($i in 0..1){Assert-KmcChunk7RealtimeOwnerStable $c.before $c.after $ids[2*$i] $ids[2*$i+1]}
        foreach($f in @('featureEnabled','available','targetAllowed','straightRoute','clicked')){Assert-KmcChunk7Boolean $c.$f $true};Assert-KmcChunk7Boolean $c.landingBlocked $false
        if($c.casterId-cne$ids[0]-or$c.targetId-cne$o.targetId-or$c.abilityGuid-cne'd79eaec224a7a832e738eb81baef9d49'-or$c.castDelta-ne1-or$c.refusalDelta-ne0-or$c.selectionAfterInput-cne$ids[2]-or
           $c.command-eq0-or-not$shells.Add([long]$c.shell)){throw 'Charge exact native shell/actor input differs.'}
        Assert-KmcChunk7RealtimeShell $c.admittedShell $c.shell $ids[0] $o.targetId
        Assert-KmcChunk7RealtimeCost $trace $ids[0] $c.shell 'Kingmaker.UnitLogic.Commands.UnitUseAbility' $c.before.frame $c.after.frame -FullRound
        if($c.movement.executorId-cne$ids[3]-or$c.movement.casterId-cne$ids[2]-or$c.movement.command-eq0-or
           @($c.admittedNeighbourCommands|Where-Object{$_.identity-eq$c.movement.command-and$_.actorId-ceq$ids[3]-and-not$_.finished}).Count-ne1){throw 'Charge lacks concurrent exact native neighbour movement.'}
        Assert-KmcChunk7Boolean $c.movement.clicked $true;Assert-KmcChunk7RealtimeDisplacement $c.movement.before $c.after $ids[3]
        foreach($id in @($ids[2],$ids[3])){if((Get-KmcChunk7CombatActor $c.after $id).standard-ne0){throw 'Neighbour paid Charge expenditure.'}}
        if(@($c.after.owners|Where-Object{$_.id-ceq$c.neighbourOwner-and$_.state-ceq'Active'-and(Test-KmcChunk7OwnerActorMatch $_.riderId $ids[2])}).Count-ne1){throw 'Charge changed independent neighbour owner.'}
        $s=$c.settled;$lease=$s.lease
        foreach($f in @('shellActed','shellFinished','executionEnded','commandFinished')){Assert-KmcChunk7Boolean $s.$f $true}
        foreach($f in @('applied','restored','chargingRestoredExactly','speedOverrideRestoredExactly','riderChargingRestoredExactly')){Assert-KmcChunk7Boolean $lease.$f $true}
        foreach($f in @('forcedPathOutstanding','buffOutstanding')){Assert-KmcChunk7Boolean $lease.$f $false}
        $events=@($c.effectsAfter.events|Select-Object -Skip @($c.effectsBefore.events).Count);$attacks=@($events|Where-Object{$_.kind-ceq'attack-before'-and$_.actor-ceq$ids[0]-and$_.target-ceq$o.targetId-and$_.opportunity-eq$false})
        if($c.kind-ceq'charge-success'){
            if($attacks.Count-ne1-or$c.outcome.actorId-cne$ids[0]-or$c.outcome.resourceOwnerId-cne$ids[0]-or$c.outcome.targetId-cne$o.targetId-or$c.outcome.result-cne'Success'-or$c.outcome.completedAttacks-ne1){throw 'Native Charge success did not deliver exactly one rider attack.'};Assert-KmcChunk7Boolean $c.outcome.nativeAttackRule $true
        }else{
            if($attacks.Count-ne0){throw 'Cancelled Charge attacked or replayed.'};Assert-KmcChunk7RealtimeStop $c.stop $ids
            Assert-KmcChunk7Boolean $c.stop.charge.mountCharging $true;Assert-KmcChunk7Boolean $c.stop.charge.mountMoving $true;Assert-KmcChunk7Boolean $c.stop.charge.shellActed $true
            Assert-KmcChunk7RealtimeDisplacement $c.before $c.stop.before $ids[1]
        }
    }
    if($o.chargeRetreat.movement.casterId-cne$ids[0]-or$o.chargeRetreat.movement.executorId-cne$ids[1]){throw 'Second Charge used no native retreat input.'}
    Assert-KmcChunk7RealtimeDisplacement $o.chargeRetreat.movement.before $o.chargeRetreat.after $ids[1]
}
