function Test-KmcActorAllocationScenario([string]$Scenario) {
    return $Scenario -cin @('actor-allocation-rider-first-tb','actor-allocation-mount-first-tb',
        'actor-allocation-rider-first-unmounted-tb','actor-allocation-mount-first-unmounted-tb')
}

function Assert-KmcActorAllocationEvidence {
    param($Request, $Artifact, [string]$Status)
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
            $row.name -cnotin @('T01-native-allocation-trace','A05-native-preparation-callbacks','phase3d-horse-scenario-deadline','phase3d-horse-leaf-deadline','phase3d-horse-runtime-exception','phase3d-horse-tranche-cleanup')) {
            throw 'Unknown, duplicate or malformed allocation trace row.'
        }
        if($row.status -ceq 'FAIL') {$fail++;continue}
        $pass++
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
        ($Status -ceq 'PASS' -and ($fail -ne 0 -or $pass -ne (1+[int]$names.Contains('A05-native-preparation-callbacks')) -or
            !$names.Contains('T01-native-allocation-trace') -or $Artifact.errors.Count -ne 0)) -or
        ($Status -ceq 'FAIL' -and $fail -eq 0)) {throw 'Allocation trace status/counts differ.'}
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
    $first=[int]$coverage[0].evidence.firstRound
    foreach($actor in $actors) {
        foreach($round in $first..($first+2)) {
            $rows=@($Artifact.observations.actorAllocationTrace.events|Where-Object {$_.round -eq $round -and $_.state.actor -ceq $actor})
            $sequence=0L
            foreach($boundary in @('prepare-before','clear-before','clear-after','round-state-before','round-state-after','ai-round-before','ai-round-after','fact-before','fact-after','prepare-after')) {
                $hits=@($rows|Where-Object {$_.boundary -ceq $boundary -and
                    (!$boundary.StartsWith('fact-') -or $_.detail -ceq 'Kingmaker.UnitLogic.Buffs.Components.AddEffectFastHealing')})
                if($hits.Count -ne 1 -or [long]$hits[0].sequence -le $sequence){throw 'Native preparation callback order/count is wrong.'}
                $sequence=[long]$hits[0].sequence
            }
            $before=@($rows|Where-Object {$_.boundary -ceq 'fact-before' -and $_.detail -ceq 'Kingmaker.UnitLogic.Buffs.Components.AddEffectFastHealing'})[0]
            $after=@($rows|Where-Object {$_.boundary -ceq 'fact-after' -and $_.detail -ceq 'Kingmaker.UnitLogic.Buffs.Components.AddEffectFastHealing'})[0]
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
