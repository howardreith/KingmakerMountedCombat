function Assert-KmcMammothPreparation($Evidence,[string]$Rider,[string]$Mount) {
    $trace=@($Evidence.trace.events);$first=$Evidence.firstGrant;$last=$Evidence.nextGrant
    $prefix=$first.identity -replace ':1$',':'
    $nativeStart=@($trace|Where-Object boundary -CEQ 'prepare-before'|Select-Object -First 1)
    if($nativeStart.Count-ne1){throw 'Mammoth has no native Prepare boundary.'}
    $previous=0L
    foreach($event in $trace){if($event.sequence-le$previous){throw 'Mammoth trace sequence is not strictly increasing.'};$previous=[long]$event.sequence}
    $initialCounts=@()
    foreach($role in @('rider','mount')){
        $actor=if($role-ceq'rider'){$Rider}else{$Mount}
        $events=@($trace|Where-Object {$_.state.actor-ceq$actor})
        $actorObject=$first.$role.actorObject
        if($actorObject-eq0-or$last.$role.actorObject-ne$actorObject){throw 'Mammoth actor object changed.'}
        $classified=[Collections.Generic.HashSet[long]]::new()
        foreach($grant in 1,2){
            $parts=@{}
            foreach($boundary in @('prepare-before','clear-before','clear-after','round-state-after','prepare-after')){
                $part=@($events|Where-Object {$_.boundary-ceq$boundary-and$_.state.grantSequence-eq$grant})
                if($part.Count-ne1){throw 'Mammoth native preparation/effect count or order differs.'}
                $parts[$boundary]=$part[0]
            }
            $start=$parts['prepare-before'];$end=$parts['prepare-after']
            $lower=if($grant-eq1){$Evidence.beforeEncounter.traceSequence}else{$Evidence.afterAttack.traceSequence}
            $upper=if($grant-eq1){$first.traceSequence}else{$last.traceSequence}
            if($start.sequence-le$lower-or$end.sequence-ge$upper-or$start.preparingTurn-eq0-or$start.turn-eq0){
                throw 'Mammoth Prepare context or sample order differs.'
            }
            $sequence=[long]$start.sequence-1
            foreach($boundary in @('prepare-before','clear-before','clear-after','round-state-after','prepare-after')){
                $p=$parts[$boundary]
                if($p.sequence-le$sequence-or$p.frame-ne$start.frame-or$p.gameTicks-ne$start.gameTicks-or
                    $p.encounter-cne$start.encounter-or$p.session-ne$start.session-or$p.controller-ne$start.controller-or
                    $p.turn-ne$start.turn-or$p.currentActor-cne$Rider-or$p.state.actorObject-ne$actorObject-or
                    $p.nativeTurnBased-ne$true-or$p.nativePassing-ne$false-or$p.state.inCombat-ne$true){
                    throw 'Mammoth Prepare callback identity or order differs.'
                }
                $expected=$prefix+$grant
                if($boundary-ceq'prepare-before'-and$role-ceq'rider'){$expected=$prefix+($grant-1)}
                if($p.activationIdentity-cne$expected){throw 'Mammoth Prepare activation differs.'}
                if($boundary-cin@('clear-before','clear-after','round-state-after')-and$p.preparingTurn-ne$start.preparingTurn){
                    throw 'Mammoth native effect has a different preparation context.'
                }
                $sequence=[long]$p.sequence
                if($boundary-cin@('clear-before','clear-after')){[void]$classified.Add($sequence)}
            }
            $before=$parts['clear-before'];$after=$parts['clear-after']
            if($after.sequence-ne$before.sequence+1){throw 'Mammoth Clear callbacks are not paired.'}
            foreach($field in @('standard','move','swift','reactionCooldown','initiativeCooldown')){
                if($after.state.$field-ne0){throw 'Mammoth Prepare did not clear native cooldowns.'}
            }
            foreach($field in @('reactions','reactionsPerRound','initiativeOrder')){
                if($before.state.$field-ne$after.state.$field){throw 'Mammoth Clear changed discrete resources or initiative order.'}
            }
            if($parts['round-state-after'].state.reactions-ne$parts['round-state-after'].state.reactionsPerRound){
                throw 'Mammoth round effect did not restore its declared reaction allowance.'
            }
        }
        foreach($boundary in @('prepare-before','round-state-after','prepare-after')){
            if(@($events|Where-Object boundary -CEQ $boundary).Count-ne2){throw 'Mammoth has an extra preparation or round effect.'}
        }
        $initial=@($events|Where-Object {$_.boundary-cin@('clear-before','clear-after')-and-not$classified.Contains([long]$_.sequence)})
        $initialCounts+=@($initial.Count)
        if($initial.Count-eq0){continue}
        if($initial.Count-ne2-or$initial[0].boundary-cne'clear-before'-or$initial[1].boundary-cne'clear-after'-or
            $initial[1].sequence-ne$initial[0].sequence+1){throw 'Mammoth unclassified or duplicate Clear.'}
        $a=$initial[0];$b=$initial[1]
        foreach($p in $initial){
            if($p.sequence-le$Evidence.beforeEncounter.traceSequence-or$p.sequence-ge$nativeStart[0].sequence-or
                $p.activationIdentity-cne($prefix+'0')-or$p.currentActor-ne$null-or$p.turn-ne0-or$p.preparingTurn-ne0-or
                $p.state.grantSequence-ne0-or$p.state.actorObject-ne$actorObject-or$p.state.prepared-ne$true-or
                $p.state.inCombat-ne$true-or$p.state.preparingPairedActor-ne$false-or$p.nativeTurnBased-ne$true-or
                $p.nativePassing-ne$false-or$p.round-ne1-or$p.state.pairedGrantIdentity-cne($prefix+'0')){
                throw 'Mammoth initial Clear is outside native encounter preparation.'
            }
            foreach($field in @('standard','move','swift','reactionCooldown','initiativeCooldown')){
                if($p.state.$field-ne0){throw 'Mammoth initial Clear erased existing debt.'}
            }
            foreach($field in @('frame','gameTicks','encounter','session','controller','roundStartTicks')){
                if($p.$field-cne$a.$field){throw 'Mammoth initial Clear callback identity differs.'}
            }
            foreach($field in @('reactions','reactionsPerRound','initiativeOrder')){
                if($p.state.$field-ne$a.state.$field-or$p.state.$field-ne$first.$role.$field){
                    throw 'Mammoth initial Clear changed reactions or initiative order.'
                }
            }
        }
    }
    if($initialCounts[0]-ne$initialCounts[1]){throw 'Mammoth initial encounter Clear is missing one actor.'}
}

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
    Assert-KmcMammothPreparation $e $rider $mount
    foreach($actor in @($rider,$mount)) {
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
