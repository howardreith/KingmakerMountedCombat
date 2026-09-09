function Assert-KmcChunk4ChargeEvidence {
    param($Request,$Artifact,[AllowNull()][string]$Status)
    if ([long]$Artifact.schemaVersion -notin @(18,19,20) -or $Request.scenario -cnotin @('chunk4-charge-safety-rt','chunk4-charge-safety-tb')) {
        throw 'Chunk 4 Charge requires its exact schema and parameterized mode.'
    }
    Assert-KmcMountedRuntimeConfiguration $Artifact.observations.phase3fActualConfiguration $true 'Chunk 4 Charge configuration'
    $required=@('C4-CHARGE-mounted-rider','C4-CHARGE-unmounted-rider')
    if ([long]$Artifact.schemaVersion -ge 19) {
        $required+=@('C4-CHARGE-mounted-mount','C4-CHARGE-unrelated-actor','C4-CHARGE-queued-state-change')
    }
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
        $mode=if($Request.scenario.EndsWith('-tb')){'TB'}else{'RT'}
        if ($row.name -ceq 'C4-CHARGE-queued-state-change') {
            if ($e.level -cne 'NATIVE INTEGRATION' -or $e.mode -cne $mode -or
                $e.inputKind -cne 'native-mount-handler-and-native-queue-promotion' -or
                $e.blueprint -cne 'c78506dd0e14f7c45a599990e4e65038' -or
                $e.availableWhileUnmounted -ne $true -or $e.canTargetWhileUnmounted -ne $true -or
                $e.queuedWhileUnmounted -ne $true -or $e.pausedQueueCostsPure -ne $true -or
                $e.nativeQueueApi -cne 'UnitCommands.AddToQueueInternal060026B8' -or
                $e.mountSucceeded -ne $true -or $e.combatBeforeMount -ne $false -or
                $e.rejectedBeforeStart -ne $true -or $e.admissionCostsAndPositionPure -ne $true -or
                $e.chargingObserved -ne $false -or $e.warningDelta -ne 1 -or
                $e.rulesBeforeRecovery.pairForcedD20 -ne 0 -or $e.rulesBeforeRecovery.riderAttackRules -ne 0 -or
                $e.rulesBeforeRecovery.mountAttackRules -ne 0 -or @($e.admission).Count -ne 2) {
                throw 'Queued Charge did not safely cross a real pre-combat Mount transition.'
            }
            $before=$e.admission[0];$after=$e.admission[1]
            if ($before.boundary -cne 'private-run-before' -or $after.boundary -cne 'private-run-after' -or
                $before.command -ne $after.command -or $before.started -ne $false -or $before.acted -ne $false -or
                $after.started -ne $false -or $after.acted -ne $false -or
                ([long]$Artifact.schemaVersion -eq 19 -and $after.finished -ne $true) -or
                $before.standard -ne $after.standard -or $before.move -ne $after.move -or
                (@($before.actorPosition)-join ',') -cne (@($after.actorPosition)-join ',')) {
                throw 'Native queue promotion changed costs or motion before Charge rejection.'
            }
            if ([long]$Artifact.schemaVersion -eq 20) {
                $rejection=Test-KmcChunk4ChargeRejectionBoundary $e.admission
                $approach=@($e.approachExecution)
                if($approach.Count % 2 -ne 0){throw 'Charge approach observer omitted one side of a boundary.'}
                for($index=0;$index -lt $approach.Count;$index+=2) {
                    if ($approach[$index].boundary -cne 'charge-approach-before' -or
                        $approach[$index+1].boundary -cne 'charge-approach-after' -or
                        $approach[$index].command -ne $before.command -or $approach[$index+1].command -ne $before.command) {
                        throw 'Charge approach observation lost its exact native queued command.'
                    }
                    if(Test-KmcChunk4ChargeRejectionBoundary @($approach[$index],$approach[$index+1])){$rejection=$true}
                }
                if (!$rejection -or $e.executionRejectedWhileMounted -ne $true -or
                    $e.charge.started -ne $false -or $e.charge.acted -ne $false -or $e.charge.finished -ne $true -or
                    $e.charge.queued -ne $false -or $e.charge.contained -ne $false) {
                    throw 'Queued Charge lacks a pure mounted rejection before native approach or expenditure.'
                }
            }
            Assert-KmcChunk4ChargeRecovery $e.recovery $mode
            continue
        }
        $mounted=$row.name -cin @('C4-CHARGE-mounted-rider','C4-CHARGE-mounted-mount')
        if ($e.level -cne 'NATIVE INTEGRATION' -or $e.inputKind -cne 'scripted-native-handler-integration' -or
            $e.mode -cne $mode -or $e.mounted -ne $mounted -or $e.hoverPure -ne $true -or @($e.identity).Count -ne 1 -or
            $e.identity[0].logic -cne 'Kingmaker.UnitLogic.Abilities.Components.AbilityCustomCharge' -or
            $e.identity[0].assemblyMvid -cne '07fa1e4d-8618-41b3-9b8d-faa17d3b26f7' -or
            $e.identity[0].blueprint -cne 'c78506dd0e14f7c45a599990e4e65038' -or @($e.samples).Count -lt 1 -or
            $e.rules.pairForcedD20 -ne 0) {throw 'Charge identity, input, observation or native-roll evidence missing.'}
        if ([long]$Artifact.schemaVersion -ge 19) {
            $expectedRider=$row.name -cin @('C4-CHARGE-mounted-rider','C4-CHARGE-unmounted-rider')
            $expectedMount=$row.name -ceq 'C4-CHARGE-mounted-mount'
            if ($e.actorIsRider -ne $expectedRider -or $e.actorIsMount -ne $expectedMount -or
                $e.actorId -cne $e.before.actor.id -or
                ($expectedRider -ne ($e.actorId -ceq $e.before.rider.id)) -or
                ($expectedMount -ne ($e.actorId -ceq $e.before.mount.id)) -or
                $e.pairMounted -ne ($row.name -cne 'C4-CHARGE-unmounted-rider') -or
                $e.warningDelta -ne $(if($mounted){1}else{0})) {
                throw 'Charge control actor, pair scope or feedback attribution mismatch.'
            }
        }
        if ($mounted) {
            foreach($name in @('maximumRiderStandard','maximumRiderMove','maximumMountStandard','maximumMountMove','riderDistance','mountDistance')) {
                if (!(Test-KmcFiniteNonnegativeJsonNumber $e.$name)) {throw 'Charge safety measurement must be finite and nonnegative.'}
            }
            if ($e.safeRejected -ne $true -or $e.observedCharging -ne $false -or
                $e.riderDistance -ge 0.01 -or $e.mountDistance -ge 0.01 -or
                $e.maximumRiderStandard -ge 0.001 -or $e.maximumRiderMove -ge 0.001 -or
                $e.maximumMountStandard -ge 0.001 -or $e.maximumMountMove -ge 0.001 -or
                $e.rules.riderAttackRules -ne 0 -or $e.rules.mountAttackRules -ne 0 -or
                $e.before.nativeGeometry.customCanTarget -ne $true -or
                $e.feedback -cne 'Charge is not yet supported while mounted.' -or
                $e.before.nativeReason -cne $e.feedback -or
                @($e.nativeWarnings).Count -lt $(if($row.name -ceq 'C4-CHARGE-mounted-mount'){1}else{4})) {
                throw 'Mounted Charge started movement, delivery or expenditure.'
            }
            if ($e.before.nativeCanTarget -ne $false -or $e.before.nativeAvailable -ne $false) {
                throw 'Mounted Charge query still advertises native eligibility.'
            }
            foreach($warning in $e.nativeWarnings) {
                if($warning.text -cne $e.feedback -or $warning.addToLog -ne $true) {throw 'Native Charge warning missing.'}
            }
            if ($row.name -ceq 'C4-CHARGE-mounted-rider') { Assert-KmcChunk4ChargeRecovery $e.recovery $mode }
        } elseif ($e.nativeChargeCompleted -ne $true -or
            ([long]$Artifact.schemaVersion -eq 18 -and ($e.riderDistance -le 1 -or $e.maximumRiderStandard -le 0)) -or
            ([long]$Artifact.schemaVersion -ge 19 -and ($e.actorDistance -le 1 -or $e.maximumActorStandard -le 0)) -or
            $e.before.nativeCanTarget -ne $true -or $e.before.nativeAvailable -ne $true -or $e.rules.riderResolved -le 0) {
            throw 'Unmounted Charge control did not complete a native charge attack and cost.'
        }
    }
    if ($Artifact.subscenarioPassCount -ne $pass -or $Artifact.subscenarioFailCount -ne $fail -or
        (($Artifact.status -ceq 'PASS') -ne ($fail -eq 0 -and @($Artifact.errors).Count -eq 0))) {throw 'Charge status/count mismatch.'}
    if ($Artifact.status -ceq 'PASS') {foreach($name in $required){if(-not $names.Contains($name)){throw 'Required Charge control missing.'}}}
    if ($Status -ceq 'PASS' -and $Artifact.status -cne 'PASS') {throw 'Runtime PASS contains failed Charge evidence.'}
}

function Test-KmcChunk4ChargeRejectionBoundary {
    param([object[]]$Pair)
    if(@($Pair).Count -ne 2){return $false}
    $before=$Pair[0];$after=$Pair[1]
    if($before.relationship -cne 'Mounted' -or $after.relationship -cne 'Mounted' -or
        $before.command -ne $after.command -or $before.frame -ne $after.frame -or
        $before.started -ne $false -or $before.acted -ne $false -or $before.finished -ne $false -or
        $after.started -ne $false -or $after.acted -ne $false -or $after.finished -ne $true -or
        $before.charging -ne $false -or $after.charging -ne $false){return $false}
    foreach($name in @('standard','move','mountStandard','mountMove')) {
        if(!(Test-KmcFiniteNonnegativeJsonNumber $before.$name) -or
            !(Test-KmcFiniteNonnegativeJsonNumber $after.$name) -or $before.$name -ne $after.$name){return $false}
    }
    foreach($name in @('actorPosition','mountPosition')) {
        if(@($before.$name).Count -ne 3 -or @($after.$name).Count -ne 3 -or
            (@($before.$name)-join ',') -cne (@($after.$name)-join ',')){return $false}
    }
    return $true
}

function Assert-KmcChunk4ChargeRecovery {
    param($Recovery,[string]$Mode)
    $queue=$Recovery.liveQueue; $attack=$Recovery.ordinaryAttack
    if($Recovery.completed -ne $true -or $Recovery.moveDistance -le 0.5 -or
        $Recovery.afterMove.rider.move -ge 0.001 -or $queue.queuedRejectedBeforeInit -ne $true -or
        $queue.queuePure -ne $true -or $queue.executionPure -ne $true -or $queue.clickPure -ne $true -or
        $queue.preparedStartRejected -ne $true -or $queue.clickRejected -ne $true -or
        $queue.sameLiveAttack -ne $true -or $queue.warnings -ne 3 -or
        $attack.complete -ne $true -or $attack.isCharge -ne $false -or $attack.planned -lt 1 -or
        $attack.completed -ne $attack.planned -or $attack.maximumRiderStandard -le 0 -or
        $attack.rules.riderResolved -lt 1 -or $attack.rules.pairForcedD20 -ne 0 -or
        ($Mode -ceq 'TB' -and ($Recovery.nativeEndInput -ne $true -or [string]::IsNullOrWhiteSpace($Recovery.nextUnrelatedActor)))) {
        throw 'Charge rejection did not preserve a queued/live legal command and native recovery.'
    }
}
