. (Join-Path $PSScriptRoot 'PairedRestrictionEvidence.ps1')
. (Join-Path $PSScriptRoot 'PairedConditionCommandEvidence.ps1')
. (Join-Path $PSScriptRoot 'PairedDeathEvidence.ps1')

function Test-KmcActorAllocationScenario([string]$Scenario) {
    return $Scenario -cin @('actor-allocation-rider-first-tb','actor-allocation-mount-first-tb',
        'actor-allocation-rider-first-unmounted-tb','actor-allocation-mount-first-unmounted-tb')
}

function Assert-KmcPairedMovementEvidence($move) {
    foreach($field in @('distance','travelledDistance','nativeShiftDistance','nativeMoveCost','nativeAllowedTime')) {
        $value=$move.$field
        if($null -eq $value -or $value -is [string] -or [double]::IsNaN([double]$value) -or
            [double]::IsInfinity([double]$value) -or [double]$value -lt 0) {throw 'Missing or invalid native movement measurement.'}
    }
    if($move.purpose -ceq 'exhausted-rejection') {
        if($move.distance -ge 0.02 -or $move.travelledDistance -ge 0.02 -or $move.nativeShiftDistance -ge 0.02 -or
            [Math]::Abs([double]$move.nativeMoveCost) -ge 0.001 -or [Math]::Abs([double]$move.nativeAllowedTime) -ge 0.001) {throw 'Exhausted movement delivered motion or cost mutation.'}
    } elseif($move.admitted -ne $true -or $move.distance -le 0.02 -or $move.nativeMoveCost -le 0 -or
        [Math]::Abs([double]$move.nativeMoveCost-[double]$move.nativeAllowedTime) -ge 0.02 -or
        $move.travelledDistance -lt ([double]$move.distance-0.02) -or
        [Math]::Abs([double]$move.travelledDistance-[double]$move.nativeShiftDistance) -ge 0.35 -or
        $move.nativeShiftDistance -gt ([double]$move.nativeAllowedTime*[double]$move.before.speedMps+0.35)) {
        throw 'Movement lacks measured native travel/displacement/time/cost agreement.'
    }
}

function Assert-KmcPairedReactionEvidence($Evidence) {
    if($null -eq $Evidence -or $Evidence.passed -ne $true -or $Evidence.inputKind -cne 'scripted-native-AI-command' -or
        [string]::IsNullOrWhiteSpace([string]$Evidence.nativeTurnActor) -or
        $Evidence.activationIdentity -ceq $Evidence.refreshIdentity -or @($Evidence.operations).Count -ne 4 -or
        $Evidence.mountBefore.reactions -ne 1 -or $Evidence.mountAfterConsumption.reactions -ne 0 -or
        $Evidence.mountAfterRefresh.reactions -ne 1 -or $Evidence.mountAfterRefresh.reactionCooldown -ne 0 -or
        $Evidence.mountAfterRefresh.disengageTargets -ne 0) {throw 'Native reaction consumption/refresh coverage is incomplete.'}
    foreach($move in $Evidence.operations) {
        if($move.before.actor -cne $Evidence.nativeTurnActor -or $move.after.actor -cne $Evidence.nativeTurnActor -or
            $move.result -cne 'Success' -or $move.distance -le 3.8 -or $move.nativeTime -le 0 -or
            [Math]::Abs([double]$move.nativeTime-[double]$move.nativeCost) -ge 0.02 -or
            $move.mountOpportunityRules -ne 1 -or $move.mountAfter.reactions -ne 0 -or
            $move.mountBefore.standard -ne $move.mountAfter.standard -or $move.mountBefore.move -ne $move.mountAfter.move) {
            throw 'Native reaction stimulus, duplicate rejection or ordinary cost conservation failed.'
        }
    }
}

function Assert-KmcPairedActivationEvidence($Request, $Artifact, [string]$Status) {
    if ([string]$Request.scenario -cnotin @('actor-allocation-rider-first-tb','actor-allocation-mount-first-tb') -or
        [long]$Artifact.schemaVersion -notin @(11,12,13,14,15,16,17)) { throw 'Paired lifecycle evidence requires the exact mounted allocation scenario and schema 11-17.' }
    $config=$Artifact.observations.phase3fActualConfiguration
    if ($config.enablePairedActivation -ne $true) { throw 'Paired activation path was not selected.' }
    foreach($flag in @('enableUnifiedMountedTurn','enablePairedCommandScheduler','enableDiagnosticOverlay','overlayPresent')) {
        if($config.$flag -isnot [bool] -or $config.$flag) { throw 'An incompatible experimental authority or overlay was enabled.' }
    }
    $names=New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    $pass=0;$fail=0
    foreach($row in $Artifact.rows) {
        if(!$names.Add([string]$row.name) -or $row.status -cnotin @('PASS','FAIL') -or
            $row.name -cnotin @('P01-three-paired-activations','P02-paired-native-transitions','P03-paired-ordinary-controls','P04-paired-native-restrictions','P05-paired-native-condition-commands','P06-paired-native-mount-death','A05-native-preparation-callbacks','phase3d-horse-scenario-deadline','phase3d-horse-leaf-deadline','phase3d-horse-runtime-exception','phase3d-horse-tranche-cleanup')) {
            throw 'Unknown, duplicate or malformed paired activation row.'
        }
        if($row.status -ceq 'FAIL') {$fail++;continue};$pass++
        if($row.name -ceq 'A05-native-preparation-callbacks') { Assert-KmcAllocationCallbackEvidence $Artifact $row.evidence;continue }
        if($row.name -ceq 'P03-paired-ordinary-controls') {
            if([long]$Artifact.schemaVersion -lt 14){throw 'Paired ordinary control coverage requires schema14 or later.'}
            Assert-KmcPairedControlEvidence $Artifact $row.evidence;continue
        }
        if($row.name -ceq 'P04-paired-native-restrictions') {
            if([long]$Artifact.schemaVersion -lt 15){throw 'Paired restriction coverage requires schema15 or later.'}
            Assert-KmcPairedRestrictionEvidence $Artifact $row.evidence;continue
        }
        if($row.name -ceq 'P05-paired-native-condition-commands') {
            if([long]$Artifact.schemaVersion -lt 16){throw 'Native condition commands require schema16.'}
            Assert-KmcPairedConditionCommandEvidence $Artifact $row.evidence;continue
        }
        if($row.name -ceq 'P06-paired-native-mount-death') {
            if([long]$Artifact.schemaVersion -lt 17){throw 'Native paired death requires schema17.'}
            Assert-KmcPairedDeathEvidence $Artifact $row.evidence;continue
        }
        if($row.name -ceq 'P02-paired-native-transitions') {
            if([long]$Artifact.schemaVersion -lt 13){throw 'Transition coverage requires schema13.'}
            Assert-KmcPairedTransitionEvidence $Artifact $row.evidence;continue
        }
        if($row.name -cne 'P01-three-paired-activations') { throw 'Failure-only paired row claimed PASS.' }
        $e=$row.evidence;$trace=$Artifact.observations.actorAllocationTrace
        if([long]$Artifact.schemaVersion -ge 15) { Assert-KmcPairedNativeOrder $Artifact $e }
        $gateEvents=@($trace.events)
        if([long]$Artifact.schemaVersion -ge 13) {
            $cutoff=[long]$e.traceEndSequence
            $seal=@($gateEvents|Where-Object boundary -CEQ 'first-gate-sealed')
            if($cutoff -le 0 -or $seal.Count -ne 1 -or $seal[0].sequence -ne $cutoff -or
                $seal[0].state.actor -cne $Artifact.observations.riderId) {throw 'Invalid paired first-gate seal.'}
            $gateEvents=@($gateEvents|Where-Object sequence -LE $cutoff)
        }
        if([long]$Artifact.schemaVersion -ge 12) {
            Assert-KmcPairedReactionEvidence $e.reactions
            $lease=$Artifact.observations.reactionTargetCondition
            if($lease.actor -cne $e.reactions.nativeTurnActor -or $lease.condition -cne 'ImmuneToCombatManeuvers' -or
                $lease.before -ne $false -or $lease.applied -ne $true -or $lease.restored -ne $true) {
                throw 'Native reaction target condition lease was not acquired/restored exactly.'
            }
        }
        $rider=[string]$Artifact.observations.riderId;$mount=[string]$Artifact.observations.horseId
        if($e.level -cne 'NATIVE INTEGRATION' -or $e.gameplayQualified -ne $true -or
            $e.inputKind -cne 'scripted-native-handler-integration' -or $e.principal -cne $rider -or
            @($e.activations).Count -ne 3 -or @($e.errors).Count -ne 0 -or $trace.dropped -ne 0 -or
            $trace.observationErrors -ne 0 -or $Artifact.observations.allocationFixture.outsideCombat -ne $true -or
            $Artifact.observations.allocationFixture.endTurnInput -cne 'Game.PauseBind' -or
            $Artifact.observations.allocationPartyCombatRestored -ne $true -or
            $Artifact.observations.allocationInitiativeRestored -ne $true) {throw 'Paired loop coverage, provenance or restoration is incomplete.'}
        if(@($e.turnVisits | Where-Object actor -CEQ $mount).Count -ne 0) {throw 'Mount received a second native turn.'}
        $identities=New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
        foreach($sample in $e.activations) {
            if(!$identities.Add([string]$sample.identity) -or [string]$sample.identity -cnotmatch '^[0-9a-f]{32}:[1-9][0-9]*$') {throw 'Missing or repeated activation identity.'}
            foreach($before in @($sample.riderBefore,$sample.mountBefore)) {
                if($before.standard -ne 0 -or $before.move -ne 0 -or $before.canAct -ne $true) {throw 'Fresh allocation retained old debt or was not natively ready.'}
            }
            $round=[int]$sample.round
            foreach($actor in @($rider,$mount)) {
                $events=@($gateEvents | Where-Object {$_.round -eq $round -and $_.state.actor -ceq $actor})
                foreach($boundary in @('prepare-before','clear-before','clear-after','round-state-before','round-state-after','prepare-after','turn-end-before','turn-end-after')) {
                    if(@($events|Where-Object boundary -CEQ $boundary).Count -ne 1) {throw "Paired actor has missing/duplicate $boundary."}
                }
            }
            $visits=@($e.turnVisits | Where-Object round -EQ $round)
            if(@($visits|Where-Object {!$_.friendly}).Count -lt 1 -or
                @($visits|Where-Object {$_.friendly -and !$_.principal}).Count -lt 1) {throw 'Unrelated friendly/enemy turns are missing.'}
            foreach($move in @($sample.operations|Where-Object kind -CEQ 'movement')) {
                if($move.samePrincipalTurn -ne $true -or $move.fiveFootStep -ne $false -or $move.singleMove -ne $false -or
                    [Math]::Abs([double]$move.riderAfter.move-[double]$move.riderBefore.move) -gt 0.0001) {throw 'Movement has wrong activation, limits or resource owner.'}
                Assert-KmcPairedMovementEvidence $move
            }
        }
        $first=$e.activations[0];$second=$e.activations[1];$third=$e.activations[2]
        $attacks=@($first.operations|Where-Object kind -CEQ 'attack')
        if(($attacks.actor -join '|') -cne (@($rider,$mount)-join '|') -or $first.exhaustedMovementRejected -ne $true -or
            $second.conversionAttackRejected -ne $true -or $third.earlyEnd -ne $true) {throw 'Attack/exhaustion/conversion/early-end cases are incomplete.'}
        foreach($attack in $attacks) {
            if($attack.clicked -ne $true -or $attack.result -cne 'Success' -or $attack.completedAttacks -ne 1 -or
                $attack.nativeRule -ne $true -or $attack.after.standard -le 0) {throw 'Paired native Primary attack did not complete and charge.'}
        }
        foreach($state in @($e.refresh.rider,$e.refresh.mount)) {
            if($state.standard -ne 0 -or $state.move -ne 0 -or $state.canAct -ne $true) {throw 'Early-end refresh failed.'}
        }
    }
    $requiredPass=if([long]$Artifact.schemaVersion -eq 17){7}elseif([long]$Artifact.schemaVersion -eq 16){6}elseif([long]$Artifact.schemaVersion -eq 15){5}elseif([long]$Artifact.schemaVersion -eq 14){4}elseif([long]$Artifact.schemaVersion -ge 13){3}else{2}
    if($pass -ne $Artifact.subscenarioPassCount -or $fail -ne $Artifact.subscenarioFailCount -or
        ($Status -ceq 'PASS' -and ($pass -ne $requiredPass -or $fail -ne 0 -or !$names.Contains('P01-three-paired-activations') -or
            ([long]$Artifact.schemaVersion -ge 13 -and !$names.Contains('P02-paired-native-transitions')) -or
            ([long]$Artifact.schemaVersion -ge 14 -and !$names.Contains('P03-paired-ordinary-controls')) -or
            ([long]$Artifact.schemaVersion -ge 15 -and !$names.Contains('P04-paired-native-restrictions')) -or
            ([long]$Artifact.schemaVersion -ge 16 -and !$names.Contains('P05-paired-native-condition-commands')) -or
            ([long]$Artifact.schemaVersion -ge 17 -and !$names.Contains('P06-paired-native-mount-death')) -or
            !$names.Contains('A05-native-preparation-callbacks') -or $Artifact.errors.Count -ne 0)) -or
        ($Status -ceq 'FAIL' -and $fail -eq 0)) {throw 'Paired activation status/counts differ.'}
}

function Assert-KmcPairedNativeSamples($Artifact, $Samples) {
    $trace=@($Artifact.observations.actorAllocationTrace.events)
    foreach($sample in $Samples) {
        $hit=@($trace|Where-Object sequence -EQ $sample.traceSequence)
        if($hit.Count -ne 1 -or $hit[0].boundary -cne $sample.kind -or $hit[0].gameTicks -ne $sample.gameTicks) {throw 'Transition sample is not bound to a native trace event.'}
        foreach($actor in @('rider','mount')) {
            $id=if($actor -ceq 'rider'){$Artifact.observations.riderId}else{$Artifact.observations.horseId}
            if($sample.$actor.actor -cne $id){throw 'Transition actor changed.'}
            $events=@($trace|Where-Object {$_.sequence -le $sample.traceSequence -and $_.state.actor -ceq $id})
            if(@($events|Where-Object boundary -CEQ 'clear-after').Count -ne $sample.($actor+'Clears') -or
                @($events|Where-Object boundary -CEQ 'round-state-after').Count -ne $sample.($actor+'Effects')) {throw 'Transition grant/effect observations differ from native trace.'}
        }
    }
}

function Assert-KmcPairedControlEvidence($Artifact, $Evidence) {
    if($Evidence.level -cne 'NATIVE INTEGRATION' -or $Evidence.passed -ne $true -or
        $Evidence.inputKind -cne 'scripted-native-control-integration' -or $Evidence.automaticEndInputCount -ne 0 -or
        $Evidence.automaticEndSettingRestored -ne $true -or $Evidence.excessAttackRejected -ne $true -or
        @($Evidence.operations).Count -ne 3) {throw 'Paired ordinary native controls lack exact coverage/provenance.'}
    $rider=$Artifact.observations.riderId;$mount=$Artifact.observations.horseId
    $samples=@($Evidence.before,$Evidence.automaticRefresh,$Evidence.explicitRefresh,$Evidence.explicitEndBefore)
    for($i=0;$i -lt 3;$i++) {
        $op=$Evidence.operations[$i];$full=$i -ne 2;$actor=if($i -eq 1){$rider}else{$mount}
        if([long]$Artifact.schemaVersion -ge 15){Assert-KmcPairedAttackFixtureReach $op}
        $key=if($i -eq 1){'rider'}else{'mount'};$other=if($i -eq 1){'mount'}else{'rider'}
        $samples+=@($op.before,$op.after)
        if($op.actor -cne $actor -or $op.contextActor -cne $actor -or $op.selectedActor -cne $actor -or
            $op.hoverPure -ne $true -or $op.full -ne $full -or $op.fullEnabled -ne $full -or
            $op.nativeFull -ne $full -or $op.nativeSinglePrimary -ne $false -or $op.clicked -ne $true -or
            $op.command.executor -cne $actor -or $op.command.finished -ne $true -or $op.command.result -cne 'Success' -or
            $op.nativePlan -lt 1 -or (!$full -and $op.nativePlan -ne 1) -or $op.completed -ne $op.nativePlan -or
            $op.nativeRules -ne $op.completed -or $op.after.$key.standard -ne 6 -or
            $op.after.$key.move -ne $(if($full){3}else{0}) -or
            $op.after.$other.standard -ne $op.before.$other.standard -or $op.after.$other.move -ne $op.before.$other.move) {
            throw 'Ordinary paired actor selection, prediction, sequence or native cost contract failed.'
        }
        foreach($field in @('riderClears','mountClears','riderEffects','mountEffects')) {
            if($op.before.$field -ne $op.after.$field){throw 'Ordinary attack input granted resources or repeated effects.'}
        }
    }
    Assert-KmcPairedNativeSamples $Artifact $samples
    for($i=1;$i -le 2;$i++) {
        $after=if($i -eq 1){$Evidence.automaticRefresh}else{$Evidence.explicitRefresh}
        if($after.identity -ceq $Evidence.before.identity){throw 'Next ordinary activation reused an old grant.'}
        foreach($actor in @('rider','mount')) {
            if($after.$actor.standard -ne 0 -or $after.$actor.move -ne 0 -or
                $after.($actor+'Clears') -ne $Evidence.before.($actor+'Clears')+$i -or
                $after.($actor+'Effects') -ne $Evidence.before.($actor+'Effects')+$i) {
                throw 'Ordinary control completion failed exactly-once native renewal.'
            }
        }
    }
    if($Evidence.movement.selectedActor -cne $mount -or $Evidence.movement.fiveFootStep -ne $false -or
        $Evidence.movement.riderBefore.move -ne $Evidence.movement.riderAfter.move) {throw 'Selected mount movement charged or selected the wrong actor.'}
    Assert-KmcPairedMovementEvidence $Evidence.movement
}

function Assert-KmcPairedTransitionEvidence($Artifact, $Evidence) {
    if($Evidence.level -cne 'NATIVE INTEGRATION' -or $Evidence.passed -ne $true -or
        $Evidence.inputKind -cne 'scripted-native-control-integration' -or $Evidence.modeSettingRestored -ne $true -or
        @($Evidence.movements).Count -ne 3) {throw 'Paired native transitions lack coverage/provenance or setting restoration.'}
    Assert-KmcPairedNativeSamples $Artifact $Evidence.events
    $trace=@($Artifact.observations.actorAllocationTrace.events)
    function One-PairedEvent([string]$kind) {
        $events=@($Evidence.events|Where-Object kind -CEQ $kind)
        if($events.Count -ne 1){throw "Missing or duplicated native transition: $kind"}
        return $events[0]
    }
    function No-PairedRefresh($before,$after) {
        foreach($field in @('riderClears','mountClears','riderEffects','mountEffects')) {
            if($before.$field -ne $after.$field){throw 'Transition replayed a native grant or effect.'}
        }
        if(@($trace|Where-Object {$_.sequence -gt $before.traceSequence -and $_.sequence -le $after.traceSequence -and
            $_.boundary -ceq 'fact-after' -and $_.state.actor -cin @($Artifact.observations.riderId,$Artifact.observations.horseId)}).Count -ne 0) {
            throw 'Transition replayed native per-round fact processing.'
        }
    }
    $delay=One-PairedEvent 'native-delay-input-before'
    $resumed=@($Evidence.events|Where-Object kind -CIN @('native-delay-existing-grant-resumed','native-delay-no-forward-target-rejected'))
    if($resumed.Count -ne 1 -or $resumed[0].identity -cne $delay.identity -or
        $resumed[0].mount.reactions -ne $delay.mount.reactions){throw 'Delay lost identity or repeated reaction initialization.'}
    No-PairedRefresh $delay $resumed[0]
    $used=One-PairedEvent 'used-pair-delay-input-before';$rejected=One-PairedEvent 'used-pair-delay-rejected'
    if($used.mount.move -le 0 -or $rejected.mount.move -ne $used.mount.move -or $rejected.identity -cne $used.identity){throw 'Used-pair Delay refunded or moved ownership.'}
    No-PairedRefresh $used $rejected
    $mode=One-PairedEvent 'native-mode-exit-before';$exited=One-PairedEvent 'native-mode-exit-after';$renewed=One-PairedEvent 'native-mode-next-paired-grant'
    if([long]$Artifact.schemaVersion -ge 15) { Assert-KmcPairedSpentModeEvidence $Artifact $Evidence $mode }
    No-PairedRefresh $mode $exited
    $elapsed=([long]$exited.gameTicks-[long]$mode.gameTicks)/10000000.0
    if($elapsed -lt 0 -or [long]$renewed.gameTicks-[long]$mode.gameTicks -lt 60000000 -or $renewed.identity -ceq $mode.identity){throw 'Mode toggle fabricated an early or repeated grant.'}
    foreach($actor in @('rider','mount')) {
        if([double]$exited.$actor.standard+$elapsed -lt 5.9999 -or
            [double]$exited.$actor.move+$elapsed -lt [double]$mode.$actor.move -or
            $renewed.$actor.standard -ne 0 -or $renewed.$actor.move -ne 0 -or
            $renewed.($actor+'Clears') -ne $mode.($actor+'Clears')+1 -or
            $renewed.($actor+'Effects') -ne $mode.($actor+'Effects')+1) {throw 'Mode transition lost debt or duplicated native preparation/effects.'}
    }
    $split=One-PairedEvent 'native-dismount-input-before';$splitAfter=One-PairedEvent 'native-dismount-after'
    $independent=One-PairedEvent 'native-split-next-independent-mount-grant'
    No-PairedRefresh $split $splitAfter
    if($splitAfter.mount.move -lt $split.mount.move -or $independent.round -le $split.round -or
        $independent.mountClears -ne $split.mountClears+1 -or $independent.mountEffects -ne $split.mountEffects+1 -or
        $independent.mount.standard -ne 0 -or $independent.mount.move -ne 0) {throw 'Split duplicated participation or erased allocation.'}
    foreach($visit in @($Evidence.events|Where-Object {$_.kind -ceq 'transition-native-turn-observed' -and $_.currentActor -ceq $Artifact.observations.horseId})) {
        if($visit.traceSequence -le $splitAfter.traceSequence -or $visit.round -le $split.round){throw 'Mount received a duplicate native turn.'}
    }
    $moves=@($Evidence.movements)
    if(($moves.purpose -join '|') -cne 'partial-stop|five-foot-step|ordinary-after-step-rejected'){throw 'Native transition movement cases changed.'}
    Assert-KmcPairedMovementEvidence $moves[0]
    if($moves[0].nativeTbStopVerified -ne $true -or $moves[0].stopInput -ne $true -or $moves[0].distance -ge 2.9 -or
        $moves[0].pausedAtStop -ne $false -or $moves[0].pausedAfterStop -ne $false -or
        $null -eq $moves[0].stopBefore.move -or $moves[0].stopAfter.move -ne $moves[0].stopBefore.move) {
        throw 'Native TB Stop lacks a partial path interruption with conserved debt; native TB rejects Pause.'
    }
    $step=$moves[1]
    if($step.fiveFootStep -ne $true -or $step.admitted -ne $true -or $step.distance -le 0.02 -or $step.nativeAllowedTime -le 0 -or
        $step.nativeMoveCost -ne 0 -or $step.after.standard -ne 0 -or $step.after.metresStepped -le 0 -or
        $step.after.metresStepped -gt [double]$step.before.stepRangeMetres+0.01 -or $step.travelledDistance -lt [double]$step.distance-0.02 -or
        [Math]::Abs([double]$step.travelledDistance-[double]$step.nativeShiftDistance) -ge 0.35 -or
        $step.nativeShiftDistance -gt [double]$step.nativeAllowedTime*[double]$step.before.speedMps+0.35) {throw 'Native step lacks distance/time or conserved costs.'}
    foreach($field in @('distance','travelledDistance','nativeShiftDistance','nativeMoveCost','nativeAllowedTime')) {
        $limit=if($field -cin @('nativeMoveCost','nativeAllowedTime')){0.001}else{0.02}
        if($null -eq $moves[2].$field -or [Math]::Abs([double]$moves[2].$field) -ge $limit){throw 'Step restriction allowed excess native movement.'}
    }
    foreach($move in $moves) {
        if($move.riderBefore.move -ne $move.riderAfter.move){throw 'Paired transport charged rider Move.'}
    }
}

function Assert-KmcActorAllocationEvidence {
    param($Request, $Artifact, [string]$Status)
    if ([long]$Artifact.schemaVersion -in @(11,12,13,14,15,16,17)) {
        Assert-KmcPairedActivationEvidence $Request $Artifact $Status
        return
    }
    if (!(Test-KmcActorAllocationScenario ([string]$Request.scenario)) -or [long]$Artifact.schemaVersion -ne 7) {
        throw 'Allocation trace requires the registered separate-turn native fixture schema.'
    }
    foreach($flag in @('enableUnifiedMountedTurn','enablePairedCommandScheduler','enableDiagnosticOverlay','overlayPresent')) {
        if($Artifact.observations.phase3fActualConfiguration.$flag -isnot [bool] -or $Artifact.observations.phase3fActualConfiguration.$flag) {
            throw 'Allocation trace changed the required false configuration.'
        }
    }
    $pass=0; $fail=0
    $names=New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    foreach($row in $Artifact.rows) {
        if(!$names.Add([string]$row.name) -or $row.status -cnotin @('PASS','FAIL') -or
            $row.name -cnotin @('T01-native-allocation-trace','A05-native-preparation-callbacks','T02-native-exhaustion-refresh-trace','phase3d-horse-scenario-deadline','phase3d-horse-leaf-deadline','phase3d-horse-runtime-exception','phase3d-horse-tranche-cleanup')) {
            throw 'Unknown, duplicate or malformed allocation trace row.'
        }
        if($row.status -ceq 'FAIL') {$fail++;continue}
        $pass++
        if($row.name -ceq 'T02-native-exhaustion-refresh-trace') {
            Assert-KmcAllocationConservationTrace $Request $Artifact $row.evidence
            continue
        }
        if($row.name -ceq 'A05-native-preparation-callbacks') {
            Assert-KmcAllocationCallbackEvidence $Artifact $row.evidence
            continue
        }
        if($row.name -cne 'T01-native-allocation-trace') {throw 'Failure-only allocation row claimed PASS.'}
        $e=$row.evidence; $trace=$Artifact.observations.actorAllocationTrace
        if($e.level -cne 'NATIVE INTEGRATION' -or $e.gameplayQualified -ne $false -or
            $trace.dropped -ne 0 -or $trace.observationErrors -ne 0 -or $trace.events.Count -eq 0 -or
            $e.firstRound -lt 1 -or $e.endRound -lt $e.firstRound+3 -or $e.samples.Count -ne 6 -or
            $e.order.Count -ne 2 -or $e.order[0] -ceq $e.order[1] -or
            $Artifact.observations.allocationPartyCombatRestored -ne $true -or
            $Artifact.observations.allocationInitiativeRestored -ne $true) {throw 'Allocation trace lacks complete native rounds, actor identity or restoration.'}
        $rider=[string]$Artifact.observations.riderId; $mount=[string]$Artifact.observations.horseId
        $expectedOrder=if(([string]$Request.scenario).Contains('rider-first')){@($rider,$mount)}else{@($mount,$rider)}
        if(($e.order -join '|') -cne ($expectedOrder -join '|') -or $Artifact.observations.allocationFixture.outsideCombat -ne $true -or
            $Artifact.observations.allocationFixture.endTurnInput -cne 'Game.PauseBind') {throw 'Allocation fixture order or native input provenance differs.'}
        for($round=[int]$e.firstRound;$round-lt[int]$e.firstRound+3;$round++) {
            if(($e.rounds.([string]$round) -join '|') -cne ($expectedOrder -join '|')) {throw 'Native initiative order or participation differs.'}
            foreach($actor in $expectedOrder) {
                $events=@($trace.events | Where-Object {$_.round -eq $round -and $_.state.actor -ceq $actor})
                foreach($boundary in @('prepare-before','prepare-after','clear-before','clear-after','round-state-before','round-state-after','turn-end-before','turn-end-after','end-turn-input-before','end-turn-input-after')) {
                    if(@($events | Where-Object boundary -CEQ $boundary).Count -ne 1) {throw "Allocation trace missing or duplicated $boundary for $actor round $round."}
                }
                foreach($boundary in @('round-handler-before','ready-handler-before')) {
                    if(@($events | Where-Object boundary -CEQ $boundary).Count -lt 1) {throw 'Allocation trace lacks native callback observations.'}
                }
            }
        }
    }
    if($pass -ne $Artifact.subscenarioPassCount -or $fail -ne $Artifact.subscenarioFailCount -or
        ($Status -ceq 'PASS' -and ($fail -ne 0 -or $pass -ne (1+[int]$names.Contains('A05-native-preparation-callbacks')+[int]$names.Contains('T02-native-exhaustion-refresh-trace')) -or
            !$names.Contains('T01-native-allocation-trace') -or $Artifact.errors.Count -ne 0)) -or
        ($Status -ceq 'FAIL' -and $fail -eq 0)) {throw 'Allocation trace status/counts differ.'}
}

function Assert-KmcAllocationConservationTrace($Request, $Artifact, $Evidence) {
    if($Evidence.level -cne 'NATIVE INTEGRATION' -or $Evidence.gameplayQualified -ne $false -or
        $Evidence.coverageComplete -ne $true -or $Evidence.inputKind -cne 'scripted-native-handler-integration' -or
        $Evidence.endRound -lt $Evidence.firstRound+2 -or @($Evidence.samples).Count -ne 4) {
        throw 'Conservation trace lacks complete native coverage.'
    }
    $rider=[string]$Artifact.observations.riderId; $mount=[string]$Artifact.observations.horseId
    $order=if(([string]$Request.scenario).Contains('rider-first')){@($rider,$mount)}else{@($mount,$rider)}
    foreach($round in ([int]$Evidence.firstRound)..([int]$Evidence.firstRound+1)) {
        $samples=@($Evidence.samples|Where-Object round -EQ $round)
        if(($samples.actor -join '|') -cne ($order -join '|')) {throw 'Conservation trace actor order differs.'}
        foreach($sample in $samples) {
            $mover=if(([string]$Request.scenario).Contains('unmounted')){[string]$sample.actor}else{$mount}
            if($sample.mover -cne $mover -or $sample.before.actor -cne $mover -or $sample.after.actor -cne $mover -or
                @($sample.attempts).Count -lt 1 -or @($sample.attempts).Count -gt 12) {throw 'Conservation trace ownership or bounded request count differs.'}
            foreach($attempt in $sample.attempts) {
                if($attempt.before.actor -cne $mover -or $attempt.after.actor -cne $mover -or
                    $attempt.clicked -isnot [bool] -or $attempt.admitted -isnot [bool] -or $attempt.distance -lt 0 -or
                    $attempt.riderBefore.actor -cne $rider -or $attempt.riderAfter.actor -cne $rider -or
                    $attempt.mountBefore.actor -cne $mount -or $attempt.mountAfter.actor -cne $mount) {throw 'Conservation trace request lacks native actor observations.'}
            }
            $events=@($Artifact.observations.actorAllocationTrace.events|Where-Object {$_.round -eq $round -and $_.state.actor -ceq $sample.actor})
            foreach($boundary in @('prepare-before','prepare-after','turn-end-before','turn-end-after')) {
                if(@($events|Where-Object boundary -CEQ $boundary).Count -ne 1) {throw 'Conservation trace lacks a native grant/end boundary.'}
            }
        }
    }
}

function Assert-KmcAllocationCallbackEvidence($Artifact, $Evidence) {
    if($Evidence.level -cne 'NATIVE INTEGRATION' -or $Evidence.passed -ne $true -or @($Evidence.errors).Count -ne 0) {
        throw 'Native callback result is not a successful measured result.'
    }
    $coverageName=if([long]$Artifact.schemaVersion -in @(11,12,13,14,15,16,17)){'P01-three-paired-activations'}else{'T01-native-allocation-trace'}
    $coverage=@($Artifact.rows|Where-Object name -CEQ $coverageName)
    if($coverage.Count -ne 1 -or $coverage[0].status -cne 'PASS'){throw 'Native callback result requires complete trace coverage.'}
    $actors=@($Artifact.observations.allocationNativeRoundFacts | ForEach-Object {[string]$_.actor})
    $restored=@($Artifact.observations.allocationNativeRoundFactsRestored)
    if($actors.Count -lt 2 -or @($actors|Select-Object -Unique).Count -ne $actors.Count -or
        $actors -cnotcontains [string]$Artifact.observations.riderId -or $actors -cnotcontains [string]$Artifact.observations.horseId -or
        $restored.Count -ne $actors.Count -or @($restored|Where-Object {$_.restored -ne $true -or $_.originalDamage -ne $_.currentDamage}).Count -ne 0 -or
        (($restored.actor | Sort-Object) -join '|') -cne (($actors | Sort-Object) -join '|') -or
        @($Evidence.measurements).Count -ne $actors.Count*3) {throw 'Native fact ownership, cleanup or measurement coverage is incomplete.'}
    $componentTypes=@($Artifact.observations.allocationNativeRoundFacts.nativeComponent|Select-Object -Unique)
    if($componentTypes.Count -ne 1 -or $componentTypes[0] -cnotin @('Kingmaker.UnitLogic.Buffs.Components.AddEffectFastHealing','Kingmaker.UnitLogic.Mechanics.Components.AddFactContextActions')) {
        throw 'Unknown native preparation fact component.'
    }
    $componentType=$componentTypes[0]
    $first=[int]$coverage[0].evidence.firstRound
    $callbackEvents=@($Artifact.observations.actorAllocationTrace.events)
    if([long]$Artifact.schemaVersion -ge 13) {
        $cutoff=[long]$coverage[0].evidence.traceEndSequence
        $seal=@($callbackEvents|Where-Object boundary -CEQ 'first-gate-sealed')
        if($cutoff -le 0 -or $seal.Count -ne 1 -or $seal[0].sequence -ne $cutoff -or
            $seal[0].state.actor -cne $Artifact.observations.riderId) {throw 'First-gate native trace cutoff is not an exact seal event.'}
        $callbackEvents=@($callbackEvents|Where-Object sequence -LE $cutoff)
    }
    foreach($actor in $actors) {
        foreach($round in $first..($first+2)) {
            $rows=@($callbackEvents|Where-Object {$_.round -eq $round -and $_.state.actor -ceq $actor})
            $sequence=0L
            foreach($boundary in @('prepare-before','clear-before','clear-after','round-state-before','round-state-after','ai-round-before','ai-round-after','fact-before','fact-after','prepare-after')) {
                $hits=@($rows|Where-Object {$_.boundary -ceq $boundary -and
                    (!$boundary.StartsWith('fact-') -or $_.detail -ceq $componentType)})
                if($hits.Count -ne 1 -or [long]$hits[0].sequence -le $sequence){throw 'Native preparation callback order/count is wrong.'}
                $sequence=[long]$hits[0].sequence
            }
            $before=@($rows|Where-Object {$_.boundary -ceq 'fact-before' -and $_.detail -ceq $componentType})[0]
            $after=@($rows|Where-Object {$_.boundary -ceq 'fact-after' -and $_.detail -ceq $componentType})[0]
            if([int]$before.state.damage-[int]$after.state.damage -ne 1 -or $before.callbackObject -ne $after.callbackObject) {
                throw 'Native fast healing did not deliver exactly one observed effect.'
            }
            foreach($kind in @('round-handler','ready-handler')) {
                $before=@($rows|Where-Object boundary -CEQ ($kind+'-before'))
                $after=@($rows|Where-Object boundary -CEQ ($kind+'-after'))
                if($before.Count -eq 0 -or $before.Count -ne $after.Count -or
                    @($before.callbackObject|Select-Object -Unique).Count -ne $before.Count -or
                    ($before.callbackObject -join ',') -cne ($after.callbackObject -join ',')) {throw 'Native handler instances were skipped or duplicated.'}
                $lower=if($kind -ceq 'round-handler'){'round-state-after'}else{'fact-after'}
                $upper=if($kind -ceq 'round-handler'){'ai-round-before'}else{'prepare-after'}
                $lowerSequence=($rows|Where-Object boundary -CEQ $lower|Measure-Object sequence -Maximum).Maximum
                $upperSequence=($rows|Where-Object boundary -CEQ $upper|Measure-Object sequence -Minimum).Minimum
                if($before[0].sequence -le $lowerSequence -or $after[-1].sequence -ge $upperSequence) {throw 'Native handler boundary order is wrong.'}
            }
            foreach($event in @($rows|Where-Object boundary -CIN @('round-state-before','round-handler-before','ai-round-before','fact-before','ready-handler-before'))) {
                $retained=$event.state.retained
                if($null -ne $retained -and $retained.Round -eq $round -and
                    ([double]$event.state.move+0.00001 -lt [double]$retained.MoveUsed -or
                     $retained.StandardUsed -and [double]$event.state.standard -lt 6.0)) {throw 'Native callback observed lost retained expenditure.'}
            }
        }
    }
}
