function Assert-KmcPhase3hLoopEvidence {
    param($Request,$Artifact,[AllowNull()][string]$Status)
    if ([long]$Artifact.schemaVersion -ne 9 -or [string]$Request.scenario -cnotin @('phase3h-combat-loop-rt','phase3h-combat-loop-tb')) {
        throw 'Phase 3H controls require exact schema 9 and named RT/TB scope.'
    }
    $configuration=$Artifact.observations.phase3fActualConfiguration
    Assert-KmcExactProperties $configuration @('enableUnifiedMountedTurn','enablePairedCommandScheduler','enableDiagnosticOverlay','overlayPresent') 'Phase 3H configuration'
    foreach($name in @('enableUnifiedMountedTurn','enablePairedCommandScheduler','enableDiagnosticOverlay','overlayPresent')) {
        if($configuration.$name -isnot [bool] -or $configuration.$name -ne $false){throw 'Phase 3H configuration differs from shipped C0.'}
    }
    $required=@('3h-rider-longbow-ordinary','3h-rider-longbow-primary','3h-rider-melee-ordinary','3h-rider-melee-primary','3h-horse-bite-ordinary','3h-horse-bite-primary')
    if($Request.scenario -ceq 'phase3h-combat-loop-rt'){$required+=@('3h-paused-dismount','3h-paused-mount-stop','3h-paused-mount-execute')}
    $allowed=@($required)+@('3h-paused-control-failure','phase3d-horse-tranche-cleanup','phase3d-horse-scenario-deadline','phase3d-horse-leaf-deadline','phase3d-horse-runtime-exception')
    $names=New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    $pass=0;$fail=0
    foreach($row in $Artifact.rows) {
        if($row.name -cnotin $allowed -or -not $names.Add([string]$row.name) -or $row.status -cnotin @('PASS','FAIL')){throw 'Invalid Phase 3H row.'}
        if($row.status -ceq 'FAIL'){$fail++;continue}
        $pass++
        if($row.name -cnotin $required){throw 'Failure-only row claimed PASS.'}
        if($row.name.StartsWith('3h-paused-')) {
            $e=$row.evidence;$stopping=$row.name -ceq '3h-paused-mount-stop'
            $delta=if($stopping){0}else{1};$result=if($stopping){'Interrupt'}else{'Success'}
            if($e.inputKind -cne 'scripted-native-handler-integration' -or $e.queuedBeforeExecution -ne $true -or
                $e.finished -ne $true -or $e.acted -eq $stopping -or $e.result -cne $result -or $e.dispatchDelta -ne $delta){throw 'Paused queue lacks native cancellation/execution proof.'}
            continue
        }
        $e=$row.evidence;$mount=$row.name.StartsWith('3h-horse-',[StringComparison]::Ordinal)
        $actor=if($mount){$Artifact.observations.horseId}else{$Artifact.observations.riderId}
        $resolved=if($mount){$e.rules.mountResolved}else{$e.rules.riderResolved}
        $expected=if($Request.scenario -ceq 'phase3h-combat-loop-rt' -and -not $mount -and $row.name.EndsWith('-ordinary')){2}else{1}
        if($row.name -ceq '3h-rider-longbow-ordinary'){$expected=2}
        if($e.inputKind -cne 'scripted-native-handler-integration' -or $resolved -lt $expected -or
            $e.lastOutcome.result -cne 'Success' -or $e.lastOutcome.actorId -cne $actor -or
            $e.lastOutcome.commandOwnerId -cne $actor -or $e.lastOutcome.resourceOwnerId -cne $actor -or
            $e.lastOutcome.childAttackStartCount -ne 1 -or $e.rules.pairForcedD20 -ne 0 -or
            $e.ledger.relationshipState -cne 'Mounted') {throw 'Phase 3H row lacks exact native command/rule/effect ownership.'}
        if($row.name.EndsWith('-ordinary') -and $e.intentStarts -ne 1){throw 'Ordinary continuation restarted its generation.'}
        if($row.name -ceq '3h-rider-longbow-ordinary' -and
            ($e.lastOutcome.nativeFullAttack -ne $true -or $e.lastOutcome.nativePlannedAttackCount -lt 2 -or
             $e.lastOutcome.nativeCompletedAttackCount -ne $e.lastOutcome.nativePlannedAttackCount)) {throw 'Native Rapid Shot sequence did not complete its actual plan.'}
        if($row.name.EndsWith('-primary') -and ($e.lastOutcome.singleAttackMode -ne $true -or
            $e.lastOutcome.nativePlannedAttackCount -ne 1)) {throw 'Explicit Primary lost its single attack boundary.'}
        if($Request.scenario -ceq 'phase3h-combat-loop-tb'){
            $actorLedger=if($mount){$e.ledger.mount}else{$e.ledger.rider}
            $otherLedger=if($mount){$e.ledger.rider}else{$e.ledger.mount}
            if($e.turnActor -cne $actor -or $actorLedger.standard -le $e.actorBefore.standard -or
                [Math]::Abs($otherLedger.standard-$e.otherBefore.standard) -gt 0.001){throw 'TB actor turn or separate action cost mismatch.'}
            $approach=$row.name -cin @('3h-rider-melee-ordinary','3h-horse-bite-ordinary')
            if(($approach -and $e.mountDisplacement -lt 0.5) -or (-not $approach -and $e.mountDisplacement -gt 0.1)){throw 'TB stationary/approach case geometry mismatch.'}
            if($row.name -ceq '3h-rider-longbow-ordinary' -and $actorLedger.move -lt 3){throw 'Native full-round sequence was not charged.'}
        }
    }
    if($Artifact.subscenarioPassCount -ne $pass -or $Artifact.subscenarioFailCount -ne $fail -or
        (($Artifact.status -ceq 'PASS') -ne ($fail -eq 0 -and @($Artifact.errors).Count -eq 0))){throw 'Phase 3H status/count mismatch.'}
    if($Artifact.status -ceq 'PASS') {foreach($name in $required){if(-not $names.Contains($name)){throw 'Required Phase 3H row omitted.'}}}
    if($Status -ceq 'PASS' -and $Artifact.status -cne 'PASS'){throw 'Runtime PASS contains failed Phase 3H evidence.'}
}
