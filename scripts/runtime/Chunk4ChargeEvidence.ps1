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
            $e.identity[0].blueprint -cnotmatch '^[0-9a-f]{32}$' -or @($e.samples).Count -lt 1 -or
            $e.rules.pairForcedD20 -ne 0) {throw 'Charge identity, input, observation or native-roll evidence missing.'}
        if ($mounted) {
            if ($e.safeRejected -ne $true -or $e.observedCharging -ne $false -or
                $e.riderDistance -ge 0.01 -or $e.mountDistance -ge 0.01 -or
                $e.maximumRiderStandard -ge 0.001 -or $e.maximumRiderMove -ge 0.001 -or $e.rules.riderAttackRules -ne 0) {
                throw 'Mounted Charge started movement, delivery or expenditure.'
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
