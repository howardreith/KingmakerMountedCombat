function Assert-KmcPairedConditionCommandEvidence($Artifact, $Evidence) {
    $rider=[string]$Artifact.observations.riderId
    $mount=[string]$Artifact.observations.horseId
    $trace=@($Artifact.observations.actorAllocationTrace.events)
    $cases=@($Evidence.cases)
    if($Evidence.level -cne 'NATIVE INTEGRATION' -or $Evidence.passed -ne $true -or $cases.Count -ne 2 -or
        ($cases.name -join ',') -cne 'mount-do-nothing,mount-self-harm') {throw 'Native condition command coverage is incomplete.'}
    $reassertions=@($Evidence.modeExitAiReassertions)
    if($reassertions.Count -ne 2){throw 'Native condition fixture lacks exact mode-exit AI restoration evidence.'}
    foreach($reset in $reassertions) {
        if($reset.nativeTb -ne $false -or $reset.controllerInitialized -ne $false -or $reset.actorsIdle -ne $true -or $reset.passed -ne $true) {
            throw 'AI reassertion did not use a completed native shutdown and idle actors.'
        }
        foreach($role in @('rider','mount')) {
            $before=@($reset.($role+'Before').states);$after=@($reset.($role+'After').states)
            $id=if($role -ceq 'rider'){$rider}else{$mount}
            if($before.Count -ne 1 -or $after.Count -ne 1 -or $before[0].unitId -cne $id -or $after[0].unitId -cne $id -or
                $before[0].rawAiBefore -ne $after[0].rawAiBefore -or $before[0].effectiveAiBefore -ne $after[0].effectiveAiBefore -or
                $after[0].commandsEmptyDuring -ne $true -or $after[0].rawAiDuring -ne $false -or $after[0].effectiveAiDuring -ne $false) {
                throw 'AI reassertion changed membership/original state or retained active commands.'
            }
        }
    }
    $identities=New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    for($i=0;$i -lt 2;$i++) {
        $case=$cases[$i];$stimulus=$case.stimulus
        $binding=$stimulus.factBinding
        if($binding.actor -cne $mount -or $binding.activeFactCount -ne 1 -or $binding.actionCount -ne 2 -or
            $binding.activeComponent -eq $binding.templateComponent -or $binding.activeComponent -eq 0 -or
            $binding.exactActionBound -ne $true -or $stimulus.nativeFactVisits -ne 1 -or $stimulus.preparingAtFact -ne $true) {
            throw 'Condition stimulus did not use its exact active native round fact once during preparation.'
        }
        $expectedType=if($i -eq 0){'Kingmaker.UnitLogic.Commands.UnitDoNothing'}else{'Kingmaker.UnitLogic.Commands.UnitSelfHarm'}
        $nativeTerminalStandard=6+6*$i
        if($case.passed -ne $true -or $case.outsideCombat -ne $true -or $case.mountedBeforeCombat -ne $true -or
            !$identities.Add([string]$case.activation) -or [string]$case.activation -cnotmatch '^[0-9a-f]{32}:[1-9][0-9]*$' -or
            $stimulus.inputKind -cne 'native-round-fact-condition-stimulus' -or $stimulus.actor -cne $mount -or
            $stimulus.activation -cne $case.activation -or $stimulus.conditionApplications -ne 1 -or $stimulus.choiceOverrides -ne 1 -or
            $stimulus.conditionActiveAfterApplication -ne $true -or $stimulus.conditionImmuneAfterApplication -ne $false -or
            $stimulus.nativePartAbsentBefore -ne $true -or $stimulus.nativePartCreated -ne $true -or $stimulus.nativePartRemoved -ne $true -or
            $stimulus.directControlBefore -ne $true -or $stimulus.directControlAfter -ne $true -or
            $stimulus.directControlRestored -ne $true -or $stimulus.cleanupResourcesUnchanged -ne $true -or
            $stimulus.choice -ne (30+30*$i) -or $stimulus.nativeSelfDamageRules -ne $i -or $stimulus.ownedConditionRestored -ne $true -or
            $stimulus.before.standard -ne 0 -or $stimulus.before.move -ne 0 -or
            ($i -eq 1 -and $stimulus.nativeSelfDamage -le 0)) {throw 'Native condition stimulus, grant or restoration differs.'}
        $samples=@($case.beforeEncounter,$case.admission,$case.ended,$case.forcedSplit,$case.beforeEndInput,$case.afterEnd)
        Assert-KmcPairedNativeSamples $Artifact $samples
        foreach($sample in @($case.admission,$case.ended,$case.forcedSplit,$case.beforeEndInput)) {
            if($sample.identity -cne $case.activation -or $sample.currentActor -cne $rider) {throw 'Condition command lost its principal activation.'}
        }
        foreach($actor in @('rider','mount')) {
            foreach($count in @('Clears','Effects')) {
                if($case.admission.($actor+$count) -ne $case.beforeEncounter.($actor+$count)+1 -or
                    $case.beforeEndInput.($actor+$count) -ne $case.admission.($actor+$count)) {throw 'Condition preparation or effects duplicated.'}
            }
        }
        if($case.samePrincipal -ne $true -or $case.mountEnded -ne $true -or $case.riderEnded -ne $false -or
            $case.relationshipAfter -cne 'Unmounted' -or
            $case.ended.mount.standard -ne $nativeTerminalStandard -or $case.ended.mount.move -ne 3 -or
            $case.ended.rider.standard -ne 0 -or $case.ended.rider.move -ne 0 -or
            $case.commandAtAdmission.type -cne $expectedType -or $case.commandAtAdmission.executor -cne $mount -or
            $case.commandAtEnd.id -ne $case.commandAtAdmission.id -or $case.commandAtEnd.type -cne $expectedType -or
            $case.commandAtEnd.executor -cne $mount -or
            $case.commandAtEnd.finished -ne $true -or $case.commandAtEnd.result -cne 'Success') {throw 'Native condition failed actor-local completion or native costs.'}
        # The native Prepare prefix observes the entry before production reserves
        # its new identity. Bound the complete interval, then bind admission to
        # that identity; do not relabel this earlier observation as a grant.
        $native=@($trace|Where-Object {$_.sequence -gt $case.beforeEncounter.traceSequence -and $_.sequence -le $case.afterEnd.traceSequence})
        $stimuli=@($native|Where-Object {$_.boundary -ceq 'native-condition-fact-stimulus' -and $_.state.actor -ceq $mount})
        $admissions=@($native|Where-Object {$_.boundary -ceq 'admission-after' -and $_.command -eq $case.commandAtEnd.id})
        if($stimuli.Count -ne 1 -or $stimuli[0].frame -ne $stimulus.frame -or $stimuli[0].gameTicks -ne $stimulus.gameTicks -or
            $admissions.Count -ne 1 -or $admissions[0].activationIdentity -cne $case.activation -or
            $admissions[0].commandActor -cne $mount -or $admissions[0].commandType -cne $expectedType -or
            $admissions[0].ignoreCooldown -ne $false -or $admissions[0].sequence -le $stimuli[0].sequence) {throw 'Condition command lacks exact native factory admission evidence.'}
        $adapterBefore=@($native|Where-Object {$_.boundary -ceq 'paired-confusion-adapter-before' -and $_.state.actor -ceq $mount})
        $adapterAfter=@($native|Where-Object {$_.boundary -ceq 'paired-confusion-adapter-after' -and $_.state.actor -ceq $mount})
        if($adapterBefore.Count -ne 1 -or $adapterAfter.Count -ne 1 -or
            $adapterBefore[0].activationIdentity -cne $case.activation -or $adapterAfter[0].activationIdentity -cne $case.activation -or
            $adapterBefore[0].sequence -le $stimuli[0].sequence -or $adapterBefore[0].sequence -ge $admissions[0].sequence -or
            $adapterAfter[0].sequence -le $admissions[0].sequence -or
            $adapterBefore[0].state.preparingPairedActor -ne $true -or $adapterAfter[0].state.preparingPairedActor -ne $true -or
            $adapterAfter[0].state.confusionPart -ne $true -or $adapterAfter[0].state.confusionCommand -ne $case.commandAtEnd.id) {
            throw 'Condition command lacks exactly one authoritative adapter preparation and native command.'
        }
        foreach($actor in @($rider,$mount)) {
            foreach($boundary in @('prepare-before','clear-after','round-state-after','prepare-after','turn-end-before','turn-end-after')) {
                if(@($native|Where-Object {$_.boundary -ceq $boundary -and $_.state.actor -ceq $actor}).Count -ne 1) {
                    throw "Condition allocation has missing or duplicate $boundary."
                }
            }
        }
        $costBefore=@($native|Where-Object {$_.boundary -ceq 'actor-cost-before' -and $_.command -eq $case.commandAtEnd.id})
        $costAfter=@($native|Where-Object {$_.boundary -ceq 'actor-cost-after' -and $_.command -eq $case.commandAtEnd.id})
        $endBefore=@($native|Where-Object {$_.boundary -ceq 'turn-end-before' -and $_.state.actor -ceq $mount})
        $endAfter=@($native|Where-Object {$_.boundary -ceq 'turn-end-after' -and $_.state.actor -ceq $mount})
        if($costBefore.Count -ne 1 -or $costAfter.Count -ne 1 -or
            $costBefore[0].state.actor -cne $mount -or $costAfter[0].state.actor -cne $mount -or
            $costBefore[0].commandType -cne $expectedType -or $costAfter[0].commandType -cne $expectedType -or
            $costBefore[0].state.standard -ne 6 -or $costAfter[0].state.standard -ne $nativeTerminalStandard -or
            $costBefore[0].state.move -ne 3 -or $costAfter[0].state.move -ne 3 -or
            $costAfter[0].sequence -le $costBefore[0].sequence -or $costAfter[0].sequence -ge $case.ended.traceSequence -or
            $endBefore[0].sequence -le $case.beforeEndInput.traceSequence -or
            $endBefore[0].state.standard -ne $nativeTerminalStandard -or $endAfter[0].state.standard -ne 6 -or
            $endBefore[0].state.move -ne 3 -or $endAfter[0].state.move -ne 3) {
            throw 'Native condition action charge or final forfeiture settlement differs.'
        }
        $ops=@($case.operations)
        if($ops.Count -ne 1){throw 'Principal continuation attack is missing.'}
        $op=$ops[0]
        if($op.actor -cne $rider -or $op.full -ne $false -or $op.hoverPure -ne $true -or $op.clicked -ne $true -or
            $op.nativeFull -ne $false -or $op.nativeSinglePrimary -ne $false -or $op.nativePlan -ne 1 -or $op.completed -ne 1 -or
            $op.nativeRules -ne 1 -or $op.command.type -cne 'Kingmaker.UnitLogic.Commands.UnitAttack' -or
            $op.traceCase -cne 'paired-rider-single' -or $op.verified -ne $true -or $op.ruleObservationFrame -lt $op.after.frame -or
            $op.after.rider.standard -ne 6 -or $op.after.rider.move -ne 0 -or $op.after.mount.standard -ne $nativeTerminalStandard -or $op.after.mount.move -ne 3) {
            throw 'Forced split did not retain native principal attack and spent mount resources.'
        }
        if($op.command.result -cne 'Success') {
            $recovery=$op.nativeRecovery
            if($op.command.result -cne 'Interrupt' -or $null -eq $recovery -or
                $recovery.boundary -cne 'native-recovery-interrupt' -or $recovery.commandType -cne 'Kingmaker.UnitLogic.Commands.UnitAttack' -or
                $recovery.command -ne $op.command.id -or $recovery.caseId -cne $op.traceCase -or $recovery.result -cne 'Success' -or
                $recovery.planned -ne 1 -or $recovery.completed -ne 1 -or $null -ne $recovery.plannedWeapon -or
                $recovery.actor -cne $rider -or $recovery.frame -lt $op.before.frame -or $recovery.frame -gt $op.after.frame -or
                !$recovery.detail.Contains('Kingmaker.UnitLogic.Commands.UnitAttack.OnTick')) {
                throw 'Unproven or premature interruption cannot qualify the native principal continuation.'
            }
            $observed=@($Artifact.observations.ordinaryAttackTrace.events | Where-Object {
                $_.boundary -ceq 'native-recovery-interrupt' -and $_.command -eq $op.command.id -and $_.frame -eq $recovery.frame
            })
            if($observed.Count -ne 1 -or ($observed[0]|ConvertTo-Json -Depth 25 -Compress) -cne ($recovery|ConvertTo-Json -Depth 25 -Compress)) {
                throw 'Native principal recovery lacks its exact independent trace observation.'
            }
        }
        if([string]::IsNullOrWhiteSpace([string]$case.nextActor) -or $case.nextActor -ceq $rider -or
            $case.afterEnd.currentActor -cne $case.nextActor -or
            ($case.nextActor -ceq $mount -and $case.nextRound -le $case.round) -or
            @($case.visits|Where-Object {$_.actor -ceq $mount -and $_.round -le $case.round}).Count -ne 0) {throw 'Condition completion duplicated paired participation.'}
    }
}
