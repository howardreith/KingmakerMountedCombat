function Assert-KmcOrdinaryAttackControlsEvidence {
    param($Request, $Artifact, [string]$Status)
    if ([string]$Request.scenario -cne 'ordinary-attack-controls-tb' -or [long]$Artifact.schemaVersion -ne 1) {
        throw 'Ordinary attack controls require the exact stable scenario and evidence schema.'
    }
    $configuration=$Artifact.observations.phase3fActualConfiguration
    foreach($flag in @('enableUnifiedMountedTurn','enablePairedCommandScheduler','enableDiagnosticOverlay','overlayPresent')) {
        if($configuration.$flag -ne $false){throw 'Ordinary controls changed the required C0 configuration.'}
    }
    $names=New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    $pass=0; $fail=0
    foreach($row in $Artifact.rows) {
        if($row.name -cnotin @('C01-B','C01-C','C01-D','phase3d-horse-scenario-deadline','phase3d-horse-leaf-deadline','phase3d-horse-runtime-exception','phase3d-horse-tranche-cleanup') -or
            -not $names.Add([string]$row.name) -or $row.status -cnotin @('PASS','FAIL')){throw 'Unknown, repeated or malformed ordinary attack row.'}
        if($row.status -ceq 'FAIL'){$fail++;continue}
        if($row.name -cnotin @('C01-B','C01-C','C01-D')){throw 'Failure-only row claimed PASS.'}
        $pass++; $e=$row.evidence; $primary=$row.name -ceq 'C01-D'; $mounted=$row.name -cne 'C01-B'
        if($e.level -cne 'NATIVE INTEGRATION' -or $e.parameters.mode -cne 'TB' -or
            $e.parameters.mounted -ne $mounted -or $e.parameters.primary -ne $primary -or
            $e.parameters.rapidShot -ne $true -or $e.parameters.weapon -cne 'Longbow' -or
            $e.prediction.count -lt 3 -or $e.prediction.pure -ne $true -or $e.continuity -ne $true -or
            $e.nativeCommand.result -cne 'Success' -or $e.nativeCommand.finished -ne $true -or
            $e.completed -ne $e.planned -or $e.rules.riderResolved -ne $e.planned -or
            $e.rules.mountNonOpportunityAttackRules -ne 0 -or $e.rules.pairForcedD20 -ne 0) {throw 'Ordinary attack lacks native lifecycle, pure prediction or exact effects.'}
        if($e.before.rider.standard -ne 0 -or $e.before.rider.move -ne 0 -or
            [Math]::Abs($e.after.rider.standard-6) -gt 0.001 -or
            [Math]::Abs($e.after.mount.standard-$e.before.mount.standard) -gt 0.001 -or
            [Math]::Abs($e.after.mount.move-$e.before.mount.move) -gt 0.001) {throw 'Ordinary control action costs or fresh allocation differ.'}
        if($null -eq $e.prediction.before -or $null -eq $e.prediction.after -or
            ($e.prediction.before|ConvertTo-Json -Depth 16 -Compress) -cne ($e.prediction.after|ConvertTo-Json -Depth 16 -Compress)) {
            throw 'Prediction purity is not supported by equal live snapshots.'
        }
        $move=if($primary){0}else{3}
        if([Math]::Abs($e.after.rider.move-$move) -gt 0.001){throw 'Native Single/full-round Move cost differs.'}
        if($primary) {
            if($e.planned -ne 1 -or $e.nativeSingle -ne $true -or $e.nativeFull -ne $false){throw 'Primary is not exactly one native single attack.'}
        } else {
            if($e.planned -lt 2 -or $e.nativeSingle -ne $false -or $e.nativeFull -ne $true -or
                $e.prediction.fullEnabled -ne $true){throw 'Ordinary native full attack was not admitted.'}
            if($mounted -and ($e.intentStarts -ne 1 -or $e.repeatedRequests -lt 3)){throw 'Mounted ordinary continuity lacks repeated native requests.'}
        }
    }
    if($pass -ne $Artifact.subscenarioPassCount -or $fail -ne $Artifact.subscenarioFailCount){throw 'Ordinary control counts differ.'}
    if($Artifact.status -ceq 'PASS') {
        if($fail -ne 0 -or @($Artifact.errors).Count -ne 0){throw 'Ordinary PASS includes a failure.'}
        foreach($name in @('C01-B','C01-C','C01-D')){if(-not $names.Contains($name)){throw 'Matched ordinary control omitted.'}}
        $baseline=@($Artifact.rows|Where-Object name -ceq 'C01-B')[0].evidence
        $mounted=@($Artifact.rows|Where-Object name -ceq 'C01-C')[0].evidence
        if($baseline.planned -ne $mounted.planned){throw 'Matched mounted and native plans differ.'}
        $trace=$Artifact.observations.ordinaryAttackTrace
        if($trace.dropped -ne 0 -or @($trace.events|Where-Object {$_.PSObject.Properties.Name -contains 'observationError'}).Count -ne 0){throw 'Native trace is incomplete.'}
        foreach($boundary in @('simulation-before','plan-after','start-after','delivery-after','ended','cost-after')) {
            if(@($trace.events|Where-Object boundary -ceq $boundary).Count -eq 0){throw 'Native trace boundary is missing.'}
        }
    }
    if($Status -ceq 'PASS' -and $Artifact.status -cne 'PASS'){throw 'Runtime PASS contains failed ordinary controls.'}
}
