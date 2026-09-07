function Test-KmcActorAllocationScenario([string]$Scenario) {
    return $Scenario -cin @('actor-allocation-rider-first-tb','actor-allocation-mount-first-tb',
        'actor-allocation-rider-first-unmounted-tb','actor-allocation-mount-first-unmounted-tb')
}

function Assert-KmcPairedActivationEvidence($Request, $Artifact, [string]$Status) {
    if ([string]$Request.scenario -cnotin @('actor-allocation-rider-first-tb','actor-allocation-mount-first-tb') -or
        [long]$Artifact.schemaVersion -ne 11) { throw 'Paired lifecycle evidence requires the exact mounted allocation scenario and schema 11.' }
    $config=$Artifact.observations.phase3fActualConfiguration
    if ($config.enablePairedActivation -ne $true) { throw 'Paired activation path was not selected.' }
    foreach($flag in @('enableUnifiedMountedTurn','enablePairedCommandScheduler','enableDiagnosticOverlay','overlayPresent')) {
        if($config.$flag -isnot [bool] -or $config.$flag) { throw 'An incompatible experimental authority or overlay was enabled.' }
    }
    $names=New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    $pass=0;$fail=0
    foreach($row in $Artifact.rows) {
        if(!$names.Add([string]$row.name) -or $row.status -cnotin @('PASS','FAIL') -or
            $row.name -cnotin @('P01-three-paired-activations','A05-native-preparation-callbacks','phase3d-horse-scenario-deadline','phase3d-horse-leaf-deadline','phase3d-horse-runtime-exception','phase3d-horse-tranche-cleanup')) {
            throw 'Unknown, duplicate or malformed paired activation row.'
        }
        if($row.status -ceq 'FAIL') {$fail++;continue};$pass++
        if($row.name -ceq 'A05-native-preparation-callbacks') { Assert-KmcAllocationCallbackEvidence $Artifact $row.evidence;continue }
        if($row.name -cne 'P01-three-paired-activations') { throw 'Failure-only paired row claimed PASS.' }
        $e=$row.evidence;$trace=$Artifact.observations.actorAllocationTrace
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
                $events=@($trace.events | Where-Object {$_.round -eq $round -and $_.state.actor -ceq $actor})
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
                if($move.purpose -ceq 'exhausted-rejection') {
                    if($move.distance -ge 0.02 -or [Math]::Abs([double]$move.nativeMoveCost) -ge 0.001 -or [Math]::Abs([double]$move.nativeAllowedTime) -ge 0.001) {throw 'Exhausted movement delivered motion or cost mutation.'}
                } elseif($move.admitted -ne $true -or $move.distance -le 0.02 -or $move.nativeMoveCost -le 0 -or
                    [Math]::Abs([double]$move.nativeMoveCost-[double]$move.nativeAllowedTime) -ge 0.02 -or
                    [Math]::Abs([double]$move.distance-[double]$move.nativeAllowedTime*[double]$move.before.speedMps) -ge 0.35) {throw 'Movement lacks measured native distance/time/cost agreement.'}
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
    if($pass -ne $Artifact.subscenarioPassCount -or $fail -ne $Artifact.subscenarioFailCount -or
        ($Status -ceq 'PASS' -and ($pass -ne 2 -or $fail -ne 0 -or !$names.Contains('P01-three-paired-activations') -or
            !$names.Contains('A05-native-preparation-callbacks') -or $Artifact.errors.Count -ne 0)) -or
        ($Status -ceq 'FAIL' -and $fail -eq 0)) {throw 'Paired activation status/counts differ.'}
}

function Assert-KmcActorAllocationEvidence {
    param($Request, $Artifact, [string]$Status)
    if ([long]$Artifact.schemaVersion -eq 11) {
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
    $coverage=@($Artifact.rows|Where-Object name -CEQ 'T01-native-allocation-trace')
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
    foreach($actor in $actors) {
        foreach($round in $first..($first+2)) {
            $rows=@($Artifact.observations.actorAllocationTrace.events|Where-Object {$_.round -eq $round -and $_.state.actor -ceq $actor})
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
