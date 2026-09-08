function Assert-KmcPairedDeathEvidence($Artifact, $Evidence) {
    $rider=[string]$Artifact.observations.riderId; $mount=[string]$Artifact.observations.horseId
    $e=$Evidence; $before=$e.beforeDamage; $after=$e.afterRemoval
    $order=@($e.nativeUnrelatedOrderBefore)
    if($order.Count -lt 1 -or $order[0] -cne $e.expectedNextActor -or
        $after.currentActor -cne $e.expectedNextActor -or $order -ccontains $rider -or $order -ccontains $mount) {
        throw 'Native death changed the next unrelated actor order.'
    }
    if($e.lethalDamageThreshold -le 0 -or $e.lethalDamageThreshold -ne $e.hitPointsBefore+$e.constitutionBefore+$e.temporaryHitPointsBefore+1 -or
        $e.damageToPartyBefore -le 0 -or
        [double]::IsInfinity([double]$e.damageToPartyBefore) -or [double]::IsNaN([double]$e.damageToPartyBefore) -or
        $e.damageToPartyAfter -ne $e.damageToPartyBefore -or
        $e.sourceIsPlayersEnemy -ne $true -or $e.sourceIsPlayerFaction -ne $false -or
        $e.requestedDamage -ne [Math]::Ceiling(($e.lethalDamageThreshold+1.0)/$e.damageToPartyBefore) -or
        $e.nativeDamageBeforeDifficulty -ne $e.requestedDamage -or $e.nativeDamage -lt $e.lethalDamageThreshold) {
        throw 'Death stimulus lacks lethal native damage through the unchanged difficulty multiplier.'
    }
    if($e.level -cne 'NATIVE INTEGRATION' -or $e.passed -ne $true -or $e.mountedBeforeCombat -ne $true -or
        $e.inputKind -cne 'labelled-native-damage-effect-stimulus' -or $e.damageDispatches -ne 1 -or
        $e.targetActor -cne $mount -or $e.sourceActor -ceq $rider -or $e.sourceActor -ceq $mount -or
        [string]::IsNullOrWhiteSpace([string]$e.sourceActor) -or $e.nativeDamage -le 0 -or
        $e.mountDead -ne $true -or $e.relationshipAfter -cne 'Unmounted' -or
        $e.riderDamageAfter -ne $e.riderDamageBefore) {throw 'Native death stimulus, independent health or lifecycle evidence differs.'}
    Assert-KmcPairedNativeSamples $Artifact @($e.beforeEncounter,$e.beforeMovement,$e.beforeAttack,$before,$e.afterDamageDispatch,$after)
    if($before.identity -cnotmatch '^[0-9a-f]{32}:1$' -or $before.currentActor -cne $rider -or
        ![string]::IsNullOrWhiteSpace([string]$after.identity) -or $after.currentActor -cin @($rider,$mount) -or
        [string]::IsNullOrWhiteSpace([string]$after.currentActor) -or
        $before.mount.standard -ne 6 -or $before.mount.move -ne 3 -or $before.rider.standard -ne 0 -or $before.rider.move -ne 0) {
        throw 'Native death did not follow real expenditure and retire the exact paired grant.'
    }
    foreach($actor in @('rider','mount')) {
        foreach($count in @('Clears','Effects')) {
            if($e.beforeMovement.($actor+$count) -ne $e.beforeEncounter.($actor+$count)+1 -or
                $after.($actor+$count) -ne $e.beforeMovement.($actor+$count)) {throw 'Death replayed preparation or effects.'}
        }
    }
    $attack=$e.attack
    if($attack.actor -cne $mount -or $attack.resourceOwner -cne $mount -or $attack.result -cne 'Success' -or
        $attack.nativeRule -ne $true -or $attack.completedAttacks -ne 1) {throw 'Native death fixture lacks a real mount Primary.'}
    $move=$e.movement
    if($move.clicked -ne $true -or $move.admitted -ne $true -or $move.distance -le 0.1 -or
        $move.travelledDistance -le 0.1 -or $move.nativeAllowedTime -le 0 -or
        [Math]::Abs($move.nativeAllowedTime-$move.nativeMoveCost) -gt 0.02 -or
        $move.result -cne 'Success' -or $move.riderAfter.standard -ne 0 -or $move.riderAfter.move -ne 0) {throw 'Death fixture movement cost/provenance differs.'}
    $native=@($Artifact.observations.actorAllocationTrace.events | Where-Object {
        $_.sequence -gt $e.beforeEncounter.traceSequence -and $_.sequence -le $after.traceSequence
    })
    foreach($actor in @($rider,$mount)) {
        foreach($boundary in @('prepare-before','clear-after','round-state-after','prepare-after','turn-end-before','turn-end-after')) {
            if(@($native|Where-Object {$_.boundary -ceq $boundary -and $_.state.actor -ceq $actor}).Count -ne 1) {
                throw "Native death allocation has missing or duplicate $boundary."
            }
        }
    }
    $removeBefore=@($native|Where-Object {$_.boundary -ceq 'remove-unit-before' -and $_.state.actor -ceq $mount})
    $removeAfter=@($native|Where-Object {$_.boundary -ceq 'remove-unit-after' -and $_.state.actor -ceq $mount})
    # Installed death first leaves combat (native combat-state Clear), then
    # delivers both leave-combat and death notifications to RemoveUnit. The
    # first removal finalizes the grant; the second must be idempotent.
    $clearBefore=@($native|Where-Object {$_.boundary -ceq 'combat-clear-before' -and $_.state.actor -ceq $mount})
    $clearAfter=@($native|Where-Object {$_.boundary -ceq 'combat-clear-after' -and $_.state.actor -ceq $mount})
    if($clearBefore.Count -ne 1 -or $clearAfter.Count -ne 1 -or
        $clearBefore[0].sequence -le $before.traceSequence -or $clearAfter[0].sequence -le $clearBefore[0].sequence -or
        $clearBefore[0].activationIdentity -cne $before.identity -or $clearAfter[0].activationIdentity -cne $before.identity -or
        $clearBefore[0].state.standard -ne 6 -or $clearBefore[0].state.move -ne 3 -or
        $clearAfter[0].state.standard -ne 0 -or $clearAfter[0].state.move -ne 0 -or
        $clearAfter[0].state.prepared -ne $false) {throw 'Native death combat-state clearing provenance differs.'}
    if($removeBefore.Count -ne 2 -or $removeAfter.Count -ne 2 -or
        $removeBefore[0].sequence -le $before.traceSequence -or $removeAfter[0].sequence -le $removeBefore[0].sequence -or
        $removeBefore[0].sequence -le $clearAfter[0].sequence -or $removeBefore[0].activationIdentity -cne $before.identity -or
        $removeBefore[0].state.standard -ne 0 -or $removeBefore[0].state.move -ne 0 -or
        $removeAfter[0].state.standard -ne 6 -or $removeAfter[0].state.move -ne 3 -or
        ![string]::IsNullOrWhiteSpace([string]$removeAfter[0].activationIdentity)) {throw 'Native removal boundary refunded debt or retained the activation.'}
    if($removeBefore[1].sequence -le $removeAfter[0].sequence -or $removeAfter[1].sequence -le $removeBefore[1].sequence -or
        $removeBefore[1].state.standard -ne 6 -or $removeBefore[1].state.move -ne 3 -or
        $removeAfter[1].state.standard -ne 6 -or $removeAfter[1].state.move -ne 3 -or
        ![string]::IsNullOrWhiteSpace([string]$removeBefore[1].activationIdentity) -or
        ![string]::IsNullOrWhiteSpace([string]$removeAfter[1].activationIdentity)) {throw 'Repeated native death removal changed settled participation or debt.'}
    foreach($actor in @($rider,$mount)) {
        $ended=@($native|Where-Object {$_.boundary -ceq 'turn-end-after' -and $_.state.actor -ceq $actor})[0]
        if($ended.sequence -le $removeBefore[0].sequence -or $ended.sequence -ge $removeAfter[0].sequence) {
            throw 'Native actor grants were not finalized inside the removal boundary.'
        }
    }
    $life=@($e.nativeLifeEvents.events|Where-Object {$_.kind -ceq 'native-life-state' -and $_.actor -ceq $mount -and $_.lifeState -ceq 'Dead'})
    if($life.Count -ne 1 -or @($e.nativeLifeEvents.events|Where-Object {$_.kind -ceq 'native-life-state' -and $_.actor -ceq $rider}).Count -ne 0) {
        throw 'Independent native mount death event evidence differs.'
    }
}
