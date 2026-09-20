function Assert-KmcPairedMammothEvidence($Record) {
    $e=$Record.pairedActivation; $rider=[string]$Record.riderId; $mount=[string]$Record.mountId
    if($Record.scenario -cne 'mounted-mammoth-primary-hit-tb' -or $Record.schemaVersion -ne 57 -or
        $e.level -cne 'NATIVE INTEGRATION' -or $e.passed -ne $true -or $e.outsideCombat -ne $true -or
        $e.enablePairedActivation -ne $true -or $e.enableUnifiedMountedTurn -ne $false -or
        $e.enablePairedCommandScheduler -ne $false -or $e.enableDiagnosticOverlay -ne $false -or
        $e.rider -cne $rider -or $e.mount -cne $mount) {throw 'Mammoth activation configuration or pair identity differs.'}
    $trace=@($e.trace.events)
    if($e.trace.dropped -ne 0 -or $e.trace.observationErrors -ne 0 -or $trace.Count -lt 1) {
        throw 'Mammoth activation trace is incomplete.'
    }
    foreach($key in @('beforeEncounter','firstGrant','afterAttack','nextGrant')) {
        $s=$e.$key; $matches=@($trace|Where-Object {$_.sequence -eq $s.traceSequence})
        if($matches.Count -ne 1) {throw 'Mammoth sample has no unique native trace position.'}
        $event=$matches[0]
        if($event.boundary -cne $s.kind -or $event.frame -ne $s.frame -or $event.gameTicks -ne $s.gameTicks -or
            $event.activationIdentity -cne $s.identity -or $event.currentActor -cne $s.currentActor -or
            $s.rider.actor -cne $rider -or $s.mount.actor -cne $mount) {throw 'Mammoth sample is detached from native observation.'}
    }
    $first=$e.firstGrant; $last=$e.nextGrant; $attack=$e.afterAttack
    if($first.identity -cnotmatch '^[0-9a-f]{32}:1$' -or $last.identity -cne ($first.identity -replace ':1$',':2') -or
        $first.currentActor -cne $rider -or $last.currentActor -cne $rider -or
        $attack.identity -cne $first.identity -or $attack.currentActor -cne $rider -or
        $first.riderPreparations -ne 1 -or $first.mountPreparations -ne 1 -or
        $last.riderPreparations -ne 2 -or $last.mountPreparations -ne 2 -or
        $first.traceSequence -le $e.beforeEncounter.traceSequence -or $attack.traceSequence -le $first.traceSequence -or
        $last.traceSequence -le $attack.traceSequence) {throw 'Mammoth activation did not renew exactly once after native expenditure.'}
    foreach($grant in @($first,$last)) {
        foreach($actor in @('rider','mount')) {
            if($grant.$actor.standard -ne 0 -or $grant.$actor.move -ne 0 -or $grant.$actor.prepared -ne $true) {
                throw 'Mammoth native grant lacks complete fresh actor resources.'
            }
        }
    }
    if($attack.mount.standard -ne 6 -or $attack.rider.standard -ne 0 -or $attack.rider.move -ne 0) {
        throw 'Mammoth Primary did not retain native per-actor costs.'
    }
    foreach($actor in @($rider,$mount)) {
        foreach($boundary in @('prepare-before','clear-after','round-state-after','prepare-after')) {
            $events=@($trace|Where-Object {$_.boundary -ceq $boundary -and $_.state.actor -ceq $actor})
            if($events.Count -ne 2 -or $events[0].sequence -le $e.beforeEncounter.traceSequence -or
                $events[0].sequence -ge $first.traceSequence -or $events[1].sequence -le $attack.traceSequence -or
                $events[1].sequence -ge $last.traceSequence) {throw 'Mammoth native preparation/effect count or order differs.'}
        }
        foreach($boundary in @('turn-end-before','turn-end-after')) {
            $events=@($trace|Where-Object {$_.boundary -ceq $boundary -and $_.state.actor -ceq $actor})
            if($events.Count -ne 1 -or $events[0].sequence -le $attack.traceSequence -or
                $events[0].sequence -ge $last.traceSequence) {throw 'Mammoth grant did not end once between activations.'}
        }
    }
    $visits=@($e.visits); $inputs=@($e.endInputs)
    if(@($visits|Where-Object {$_.actor -ceq $mount}).Count -ne 0 -or
        @($trace|Where-Object {$_.currentActor -ceq $mount}).Count -ne 0 -or
        @($visits|Where-Object {$_.actor -cne $rider -and $_.frame -gt $attack.frame -and $_.frame -lt $last.frame}).Count -lt 1 -or
        @($inputs|Where-Object {$_.actor -ceq $rider}).Count -ne 1) {throw 'Mammoth native participation or End input differs.'}
    foreach($kmcEndInput in $inputs) {
        $observed=@($trace|Where-Object {$_.boundary -ceq 'mammoth-native-end-input' -and
            $_.frame -eq $kmcEndInput.frame -and $_.currentActor -ceq $kmcEndInput.actor -and $_.state.actor -ceq $kmcEndInput.actor})
        if($kmcEndInput.kind -cne 'Game.PauseBind' -or $observed.Count -ne 1 -or $kmcEndInput.actor -ceq $mount) {
            throw 'Mammoth End input is detached from the actual native principal.'
        }
    }
}
