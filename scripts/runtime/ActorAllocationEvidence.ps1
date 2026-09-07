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
            $row.name -cnotin @('T01-native-allocation-trace','phase3d-horse-scenario-deadline','phase3d-horse-leaf-deadline','phase3d-horse-runtime-exception','phase3d-horse-tranche-cleanup')) {
            throw 'Unknown, duplicate or malformed allocation trace row.'
        }
        if($row.status -ceq 'FAIL') {$fail++;continue}
        $pass++
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
        ($Status -ceq 'PASS' -and ($fail -ne 0 -or $pass -ne 1 -or $Artifact.errors.Count -ne 0)) -or
        ($Status -ceq 'FAIL' -and $fail -eq 0)) {throw 'Allocation trace status/counts differ.'}
}
