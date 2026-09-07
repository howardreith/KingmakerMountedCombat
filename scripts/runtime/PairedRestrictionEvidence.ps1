function Assert-KmcPairedNativeOrder($Artifact, $Evidence) {
    foreach($activation in @($Evidence.activations)) {
        $visits=@($Evidence.turnVisits|Where-Object round -EQ $activation.round)
        if($visits.Count -lt 3){throw 'Native actor order is incomplete.'}
        $expected=@($visits[0].nativeOrder)
        if($expected.Count -ne $visits.Count -or @($expected|Select-Object -Unique).Count -ne $expected.Count -or
            $expected -cnotcontains $Artifact.observations.riderId -or $expected -ccontains $Artifact.observations.horseId -or
            (($visits|ForEach-Object actor)-join ',') -cne ($expected-join ',')) {throw 'Paired participation changed native actor order.'}
        foreach($visit in $visits) {
            $hit=@($Artifact.observations.actorAllocationTrace.events|Where-Object sequence -EQ $visit.traceSequence)
            if(($visit.nativeOrder-join ',') -cne ($expected-join ',') -or $hit.Count -ne 1 -or
                $visit.traceSequence -gt $Evidence.traceEndSequence -or $Evidence.traceEndSequence -le 0 -or
                $hit[0].boundary -cne 'turn-observed' -or $hit[0].state.actor -cne $visit.actor -or
                $hit[0].round -ne $visit.round -or $hit[0].frame -ne $visit.frame -or $hit[0].gameTicks -ne $visit.gameTicks -or
                $hit[0].detail -cne ('native-order='+($expected-join ','))) {throw 'Native order lacks its exact observation.'}
        }
    }
}

function Assert-KmcPairedSpentModeEvidence($Artifact, $Evidence, $Mode) {
    $attack=$Evidence.modeAttack
    if($attack.clicked -ne $true -or $attack.nativeRule -ne $true -or $attack.completedAttacks -ne 1 -or
        $attack.before.kind -cne 'native-mode-standard-spend-before' -or $attack.after.kind -cne 'native-mode-standard-spend-after' -or
        $attack.result -cne 'Success' -or $attack.actor -cne $Artifact.observations.horseId -or
        $attack.resourceOwner -cne $Artifact.observations.horseId -or $attack.before.mount.standard -ne 0 -or
        $attack.before.mount.move -le 0 -or $attack.before.mount.move -ge 3 -or
        $attack.after.mount.standard -ne 6 -or $attack.after.mount.move -ne 3 -or
        $attack.after.rider.standard -ne 0 -or $attack.after.rider.move -ne 0 -or
        $Mode.mount.standard -ne 6 -or $Mode.mount.move -ne 3) {throw 'Mode test lacks actual native Standard expenditure.'}
    Assert-KmcPairedNativeSamples $Artifact @($attack.before,$attack.after)
    foreach($field in @('riderClears','mountClears','riderEffects','mountEffects')) {
        if($attack.before.$field -ne $attack.after.$field -or $Mode.$field -ne $attack.after.$field) {
            throw 'Mode setup granted resources or replayed effects.'
        }
    }
}

function Assert-KmcPairedRestrictionEvidence($Artifact, $Evidence) {
    if($Evidence.level -cne 'NATIVE INTEGRATION' -or $Evidence.passed -ne $true -or
        $Evidence.inputKind -cne 'scripted-native-control-integration' -or $Evidence.ownedConditionRestored -ne $true -or
        $Evidence.automaticEndSettingRestored -ne $true -or $Evidence.automaticEndInputCount -ne 0 -or
        @($Evidence.operations).Count -ne 2 -or @($Evidence.movements).Count -ne 3) {throw 'Restriction coverage or restoration is incomplete.'}
    function Restriction-Event([string]$kind) {
        $found=@($Evidence.events|Where-Object kind -CEQ $kind)
        if($found.Count -ne 1){throw ('Missing or repeated native restriction event: '+$kind)}
        return $found[0]
    }
    function Unchanged-RestrictionGrant($before,$after) {
        if($before.identity -cne $after.identity){throw 'Restriction input changed activation identity.'}
        foreach($field in @('riderClears','mountClears','riderEffects','mountEffects')) {
            if($before.$field -ne $after.$field){throw 'Restriction input replayed native preparation/effects.'}
        }
    }
    $samples=@($Evidence.events)
    for($i=0;$i -lt 2;$i++) {
        $op=$Evidence.operations[$i];$key=if($i -eq 0){'mount'}else{'rider'};$other=if($i -eq 0){'rider'}else{'mount'}
        $id=if($i -eq 0){$Artifact.observations.horseId}else{$Artifact.observations.riderId};$full=$i -eq 1
        if($op.actor -cne $id -or $op.contextActor -cne $id -or $op.selectedActor -cne $id -or
            $op.hoverPure -ne $true -or $op.clicked -ne $true -or $op.full -ne $full -or $op.fullEnabled -ne $full -or
            $op.nativeFull -ne $full -or $op.nativeSinglePrimary -ne $false -or $op.nativePlan -lt 1 -or
            (!$full -and $op.nativePlan -ne 1) -or $op.completed -ne $op.nativePlan -or $op.nativeRules -ne $op.completed -or
            $op.command.executor -cne $id -or $op.command.result -cne 'Success' -or $op.command.finished -ne $true -or
            $op.after.$key.standard -ne 6 -or $op.after.$key.move -ne $(if($full){3}else{0}) -or
            $op.after.$other.standard -ne $op.before.$other.standard -or $op.after.$other.move -ne $op.before.$other.move) {
            throw 'Restricted actor native attack, mode, rule or cost failed.'
        }
        Unchanged-RestrictionGrant $op.before $op.after
        $samples+=@($op.before,$op.after)
    }
    Assert-KmcPairedNativeSamples $Artifact $samples
    $before=Restriction-Event 'staggered-before-move';$denied=Restriction-Event 'staggered-move-rejects-standard'
    Unchanged-RestrictionGrant $before $denied
    if($before.condition -cne 'Staggered' -or $before.mountConditions -cnotcontains 'Staggered' -or
        $denied.mount.standard -ne 0 -or $denied.mount.move -le 0 -or $denied.mount.hasStandard -ne $false -or
        $denied.noNewAttackOrPendingIntent -ne $true -or $denied.nativeRulesBefore -ne $denied.nativeRulesAfter) {
        throw 'Staggered partial movement did not exclude Standard or retained a future attack.'
    }
    $unexpected=@($Artifact.observations.ordinaryAttackTrace.events|Where-Object {
        $_.boundary -ceq 'start-after' -and $_.actor -ceq $Artifact.observations.horseId -and
        $_.frame -ge $before.frame -and $_.frame -le $denied.frame})
    if($unexpected.Count -ne 0){throw 'Unaffordable staggered attack reached native start.'}
    $moves=@($Evidence.movements)
    if($moves[0].purpose -cne 'staggered-partial' -or $moves[0].fiveFootStep -ne $false -or
        $moves[0].selectedActor -cne $Artifact.observations.horseId){throw 'Staggered movement used the wrong actor/input.'}
    Assert-KmcPairedMovementEvidence $moves[0]
    $rejected=$moves[1]
    if($rejected.purpose -cne 'staggered-standard-rejects-move' -or $rejected.fiveFootStep -ne $false -or
        $rejected.after.standard -ne 6 -or $rejected.after.move -ne 0 -or $rejected.distance -ge 0.02 -or
        $rejected.travelledDistance -ge 0.02 -or $rejected.nativeShiftDistance -ge 0.02 -or
        [Math]::Abs([double]$rejected.nativeMoveCost) -ge 0.001 -or
        [Math]::Abs([double]$rejected.nativeAllowedTime) -ge 0.001) {throw 'Staggered Standard gained ordinary movement.'}
    foreach($move in @($moves[0],$moves[1])) {
        if($move.riderAfter.move -ne $move.riderBefore.move){throw 'Restricted mount motion charged rider transport.'}
    }
    $getUpBefore=Restriction-Event 'native-get-up-input-before';$getUpAfter=Restriction-Event 'native-get-up-finished'
    Unchanged-RestrictionGrant $getUpBefore $getUpAfter
    $getUp=@($Evidence.nativeConditionEvents.events|Where-Object kind -CEQ 'native-get-up')
    $move=$moves[2]
    if($getUp.Count -ne 1 -or $getUp[0].actor -cne $Artifact.observations.horseId -or
        $getUp[0].frame -lt $getUpBefore.frame -or $getUp[0].frame -gt $getUpAfter.frame -or
        $getUpBefore.mount.prone -ne $true -or $getUpBefore.mountCanGetUp -ne $true -or $getUpAfter.mount.prone -ne $false -or
        $move.purpose -cne 'native-get-up-input' -or $move.admitted -ne $true -or
        $move.nativeAllowedTime -lt 0 -or [Math]::Abs([double]$move.nativeMoveCost-3-[double]$move.nativeAllowedTime) -ge 0.001 -or
        $move.travelledDistance -lt ([double]$move.distance-0.02) -or
        [Math]::Abs([double]$move.travelledDistance-[double]$move.nativeShiftDistance) -ge 0.35 -or
        $move.nativeShiftDistance -gt ([double]$move.nativeAllowedTime*[double]$move.before.speedMps+0.35) -or
        $getUpAfter.mount.standard -ne 0 -or $getUpAfter.rider.move -ne 0) {throw 'Native get-up callback, cost or movement conservation failed.'}
    $disabled=Restriction-Event 'disabled-mount-before-rider-full'
    if($disabled.condition -cne 'Stunned' -or $disabled.mountConditions -cnotcontains 'Stunned' -or $disabled.mountAble -ne $false) {
        throw 'Disabled mount test did not reach native incapacity.'
    }
    $ends=@($Evidence.events|Where-Object {$_.kind -cin @('restriction-native-end-input','disabled-auto-end-wait')})
    $next=@($Evidence.events|Where-Object kind -CEQ 'restriction-native-next-grant')
    if($ends.Count -ne 4 -or $next.Count -ne 4){throw 'Restriction subsequent-activation coverage is incomplete.'}
    for($i=0;$i -lt 4;$i++) {
        if($next[$i].identity -ceq $ends[$i].identity -or $next[$i].traceSequence -le $ends[$i].traceSequence){throw 'Restriction reused a grant.'}
        foreach($actor in @('rider','mount')) {
            if($next[$i].$actor.standard -ne 0 -or $next[$i].$actor.move -ne 0 -or
                $next[$i].($actor+'Clears') -ne $ends[$i].($actor+'Clears')+1 -or
                $next[$i].($actor+'Effects') -ne $ends[$i].($actor+'Effects')+1) {throw 'Restriction renewal duplicated effects or retained debt.'}
        }
    }
    $forced=@($Artifact.observations.actorAllocationTrace.events|Where-Object {
        $_.boundary -ceq 'end-turn-input-before' -and $_.state.actor -ceq $Artifact.observations.riderId -and
        $_.sequence -gt $disabled.traceSequence -and $_.sequence -lt $next[3].traceSequence})
    if($forced.Count -ne 0){throw 'Disabled automatic completion was replaced by End input.'}
    $restored=Restriction-Event 'native-restrictions-restored'
    if(@($restored.mountConditions).Count -ne 0 -or $restored.mount.prone -ne $false) {throw 'Native restriction state was not restored.'}
}
