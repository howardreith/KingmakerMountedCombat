function Assert-KmcChunk4ChargeEvidence {
    param($Request,$Artifact,[AllowNull()][string]$Status)
    if ([long]$Artifact.schemaVersion -ne 18 -or $Request.scenario -cnotin @('chunk4-charge-safety-rt','chunk4-charge-safety-tb')) {
        throw 'Chunk 4 Charge requires its exact schema and parameterized mode.'
    }
    Assert-KmcMountedRuntimeConfiguration $Artifact.observations.phase3fActualConfiguration $true 'Chunk 4 Charge configuration'
    $required=@('C4-CHARGE-mounted-rider','C4-CHARGE-unmounted-rider')
    $failureOnly=@('phase3d-horse-tranche-cleanup','phase3d-horse-scenario-deadline','phase3d-horse-leaf-deadline','phase3d-horse-runtime-exception')
    $names=New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    $pass=0;$fail=0
    foreach ($row in $Artifact.rows) {
        if ($row.name -cnotin ($required+$failureOnly) -or -not $names.Add([string]$row.name) -or $row.status -cnotin @('PASS','FAIL')) {
            throw 'Invalid or duplicate Chunk 4 Charge row.'
        }
        if ($row.status -ceq 'FAIL') {$fail++;continue}
        $pass++
        if ($row.name -cin $failureOnly) {throw 'Failure-only Chunk 4 row claimed PASS.'}
        $e=$row.evidence
        $mounted=$row.name -ceq 'C4-CHARGE-mounted-rider'
        $mode=if($Request.scenario.EndsWith('-tb')){'TB'}else{'RT'}
        if ($e.level -cne 'NATIVE INTEGRATION' -or $e.inputKind -cne 'scripted-native-handler-integration' -or
            $e.mode -cne $mode -or $e.mounted -ne $mounted -or $e.hoverPure -ne $true -or @($e.identity).Count -ne 1 -or
            $e.identity[0].logic -cne 'Kingmaker.UnitLogic.Abilities.Components.AbilityCustomCharge' -or
            $e.identity[0].assemblyMvid -cne '07fa1e4d-8618-41b3-9b8d-faa17d3b26f7' -or
            $e.identity[0].blueprint -cne 'c78506dd0e14f7c45a599990e4e65038' -or @($e.samples).Count -lt 1 -or
            $e.rules.pairForcedD20 -ne 0) {throw 'Charge identity, input, observation or native-roll evidence missing.'}
        if ($mounted) {
            foreach($name in @('maximumRiderStandard','maximumRiderMove','maximumMountStandard','maximumMountMove','riderDistance','mountDistance')) {
                if (!(Test-KmcFiniteNonnegativeJsonNumber $e.$name)) {throw 'Charge safety measurement must be finite and nonnegative.'}
            }
            if ($e.safeRejected -ne $true -or $e.observedCharging -ne $false -or
                $e.riderDistance -ge 0.01 -or $e.mountDistance -ge 0.01 -or
                $e.maximumRiderStandard -ge 0.001 -or $e.maximumRiderMove -ge 0.001 -or
                $e.maximumMountStandard -ge 0.001 -or $e.maximumMountMove -ge 0.001 -or $e.rules.riderAttackRules -ne 0 -or
                $e.before.nativeCanTarget -ne $false -or $e.before.nativeAvailable -ne $false -or
                $e.before.nativeGeometry.customCanTarget -ne $true -or
                $e.feedback -cne 'Charge is not yet supported while mounted.' -or
                $e.before.nativeReason -cne $e.feedback -or @($e.nativeWarnings).Count -lt 4) {
                throw 'Mounted Charge started movement, delivery or expenditure.'
            }
            foreach($warning in $e.nativeWarnings) {
                if($warning.text -cne $e.feedback -or $warning.addToLog -ne $true) {throw 'Native Charge warning missing.'}
            }
            $recovery=$e.recovery; $queue=$recovery.liveQueue; $attack=$recovery.ordinaryAttack
            if($recovery.completed -ne $true -or $recovery.moveDistance -le 0.5 -or
                $recovery.afterMove.rider.move -ge 0.001 -or $queue.queuedRejectedBeforeInit -ne $true -or
                $queue.queuePure -ne $true -or $queue.executionPure -ne $true -or $queue.clickPure -ne $true -or
                $queue.preparedStartRejected -ne $true -or $queue.clickRejected -ne $true -or
                $queue.sameLiveAttack -ne $true -or $queue.warnings -ne 3 -or
                $attack.complete -ne $true -or $attack.isCharge -ne $false -or $attack.planned -lt 1 -or
                $attack.completed -ne $attack.planned -or $attack.maximumRiderStandard -le 0 -or
                $attack.rules.riderResolved -lt 1 -or $attack.rules.pairForcedD20 -ne 0 -or
                ($mode -ceq 'TB' -and ($recovery.nativeEndInput -ne $true -or [string]::IsNullOrWhiteSpace($recovery.nextUnrelatedActor)))) {
                throw 'Charge rejection did not preserve a queued/live legal command and native recovery.'
            }
        } elseif ($e.nativeChargeCompleted -ne $true -or $e.riderDistance -le 1 -or
            $e.maximumRiderStandard -le 0 -or $e.rules.riderResolved -le 0) {
            throw 'Unmounted Charge control did not complete a native charge attack and cost.'
        }
    }
    if ($Artifact.subscenarioPassCount -ne $pass -or $Artifact.subscenarioFailCount -ne $fail -or
        (($Artifact.status -ceq 'PASS') -ne ($fail -eq 0 -and @($Artifact.errors).Count -eq 0))) {throw 'Charge status/count mismatch.'}
    if ($Artifact.status -ceq 'PASS') {foreach($name in $required){if(-not $names.Contains($name)){throw 'Required Charge control missing.'}}}
    if ($Status -ceq 'PASS' -and $Artifact.status -cne 'PASS') {throw 'Runtime PASS contains failed Charge evidence.'}
}
