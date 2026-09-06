function Get-KmcOrdinaryAttackControlCases {
    $cases=@(
        @{id='C01-B';mounted=$false;primary=$false;rapid=$true;bab=$null;haste=$false;preparation='fresh'},
        @{id='C01-C';mounted=$true;primary=$false;rapid=$true;bab=$null;haste=$false;preparation='fresh'},
        @{id='C01-D';mounted=$true;primary=$true;rapid=$true;bab=$null;haste=$false;preparation='fresh'}
    )
    foreach($variant in @('rapid-off','bab','haste','restricted','single','spent-standard')) {
        foreach($control in @('B','C')) {
            $cases+=@{id=$(if($variant -ceq 'restricted'){'C02'}else{'C03'})+'-'+$variant+'-'+$control
                mounted=$control -ceq 'C';primary=$false;rapid=$variant -notin @('rapid-off','bab','haste')
                bab=$(if($variant -in @('bab','haste')){6}else{$null});haste=$variant -ceq 'haste'
                preparation=$(switch($variant){restricted {'stale-staggered'};single {'native-single'};'spent-standard' {'spent-standard'};default {'fresh'}})}
        }
    }
    $cases+=@(
        @{id='C03-rider-move-B';mounted=$false;primary=$false;rapid=$true;bab=$null;haste=$false;preparation='rider-move'},
        @{id='C03-carried-move-C';mounted=$true;primary=$false;rapid=$true;bab=$null;haste=$false;preparation='carried-move'},
        @{id='C03-mixed-range-B';mounted=$false;primary=$false;rapid=$true;bab=$null;haste=$false;preparation='mixed-range'},
        @{id='C03-mixed-range-C';mounted=$true;primary=$false;rapid=$true;bab=$null;haste=$false;preparation='mixed-range'}
    )
    return $cases
}

function Assert-KmcNativeAttackBonusEvidence($Roll) {
    $native=$Roll.nativeCalculation
    if($null -eq $native -or $null -eq $native.innerResult -or $null -eq $native.bab -or
        $null -eq $native.concealment -or $null -eq $native.flanking -or $null -eq $native.shootIntoCombat -or
        $native.playerFaction -isnot [bool] -or $native.trueDeath -isnot [bool]) {throw 'Native bonus calculation is incomplete.'}
    $expected=[int]$native.innerResult+[int]$native.concealment+[int]$native.flanking+[int]$native.shootIntoCombat
    if($native.playerFaction -and !$native.trueDeath) { $expected=[Math]::Max($expected,[int]$native.bab-[int]$Roll.iterativePenalty-2) }
    if($Roll.attackBonus -ne $native.result -or $native.result -ne $expected) {
        throw 'Delivered attack bonus differs from native target adjustments and difficulty minimum.'
    }
}

function Assert-KmcOrdinaryAttackControlsEvidence {
    param($Request, $Artifact, [string]$Status)
    if ([string]$Request.scenario -cne 'ordinary-attack-controls-tb' -or [long]$Artifact.schemaVersion -ne 1) {
        throw 'Ordinary attack controls require the exact stable scenario and evidence schema.'
    }
    $configuration=$Artifact.observations.phase3fActualConfiguration
    foreach($flag in @('enableUnifiedMountedTurn','enablePairedCommandScheduler','enableDiagnosticOverlay','overlayPresent')) {
        if($configuration.$flag -ne $false){throw 'Ordinary controls changed the required C0 configuration.'}
    }
    $cases=@(Get-KmcOrdinaryAttackControlCases)
    $names=New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    $pass=0; $fail=0
    foreach($row in $Artifact.rows) {
        $matched=@($cases|Where-Object {$_.id -ceq $row.name})
        if(($matched.Count -ne 1 -and $row.name -cnotin @('phase3d-horse-scenario-deadline','phase3d-horse-leaf-deadline','phase3d-horse-runtime-exception','phase3d-horse-tranche-cleanup')) -or
            -not $names.Add([string]$row.name) -or $row.status -cnotin @('PASS','FAIL')){throw 'Unknown, repeated or malformed ordinary attack row.'}
        if($row.status -ceq 'FAIL'){$fail++;continue}
        if($matched.Count -ne 1){throw 'Failure-only row claimed PASS.'}
        $case=$matched[0];$pass++;$e=$row.evidence
        $full=(!$case.primary -and $case.preparation -cin @('fresh','carried-move','mixed-range'))
        $mixed=$case.preparation -ceq 'mixed-range'
        if($e.level -cne 'NATIVE INTEGRATION' -or $e.parameters.mode -cne 'TB' -or
            $e.parameters.mounted -ne $case.mounted -or $e.parameters.primary -ne $case.primary -or
            $e.parameters.rapidShot -ne $case.rapid -or $e.parameters.weapon -cne 'Longbow' -or
            $e.parameters.bab -ne $case.bab -or $e.parameters.haste -ne $case.haste -or $e.parameters.preparation -cne $case.preparation -or
            $e.prediction.count -lt 3 -or $e.prediction.pure -ne $true -or $e.continuity -ne $true -or
            $e.nativeCommand.finished -ne $true -or
            $e.nativeCommand.executor -cne $e.before.rider.id -or
            (!$mixed -and $e.completed -ne $e.planned) -or $e.rules.riderResolved -ne $e.completed -or
            $e.rules.mountNonOpportunityAttackRules -ne 0 -or $e.rules.pairForcedD20 -ne 0) {throw 'Ordinary attack lacks native lifecycle, pure prediction or exact effects.'}
        if($e.before.rider.standard -ne 0 -or
            ($case.preparation -cne 'rider-move' -and $e.before.rider.move -ne 0) -or
            ($case.preparation -ceq 'rider-move' -and ($e.before.rider.move -le 0 -or $e.before.rider.move -gt 3)) -or
            [Math]::Abs($e.after.rider.standard-6) -gt 0.001 -or
            [Math]::Abs($e.after.mount.standard-$e.before.mount.standard) -gt 0.001 -or
            [Math]::Abs($e.after.mount.move-$e.before.mount.move) -gt 0.001) {throw 'Ordinary control action costs or fresh allocation differ.'}
        if($null -eq $e.prediction.before -or $null -eq $e.prediction.after -or
            ($e.prediction.before|ConvertTo-Json -Depth 16 -Compress) -cne ($e.prediction.after|ConvertTo-Json -Depth 16 -Compress)) {
            throw 'Prediction purity is not supported by equal live snapshots.'
        }
        if([Math]::Abs($e.after.rider.move-$(if($full -or $case.preparation -ceq 'rider-move'){3}else{0})) -gt 0.001 -or
            $e.nativeSingle -ne $case.primary -or $e.nativeFull -ne $full){throw 'Native Single/full-round mode or cost differs.'}
        if($full) {
            if($e.planned -lt 2 -or $e.planned -ne $e.prediction.nativeEstimate -or $e.prediction.fullEnabled -ne $true){throw 'Ordinary native plan was not admitted.'}
            if($case.mounted -and ($e.intentStarts -ne 1 -or $e.repeatedRequests -lt 3)){throw 'Mounted ordinary continuity lacks repeated native requests.'}
        } elseif($e.planned -ne 1){throw 'Native single action produced multiple attacks.'}
        if($mixed) {
            $rejection=$e.nativeRangeRejection
            $ranged=@($e.nativePlan|Where-Object ranged -eq $true)
            if($e.nativeCommand.result -cne 'Interrupt' -or $e.completed -ne $ranged.Count -or $e.completed -ge $e.planned -or
                $null -eq $rejection -or $rejection.command -ne $e.nativeCommand.id -or $rejection.caseId -cne $case.id -or
                $rejection.targetDead -ne $false -or $rejection.targetUnconscious -ne $false -or $rejection.targetUntargetable -ne $false -or
                $e.nativePlan[$e.completed].natural -ne $true -or $rejection.plannedWeapon -cne $e.nativePlan[$e.completed].weapon -or
                $rejection.rangeOriginDistance -le $(if($case.mounted){$rejection.pairApproachRadius}else{$rejection.approachRadius})) {
                throw 'Mixed-weapon sequence did not stop at the exact native out-of-reach attack.'
            }
            if(@($e.rules.attackRuleEvents|Where-Object {$_.weaponGuid -cne $ranged[0].weapon}).Count -ne 0){throw 'Mixed sequence delivered an out-of-reach natural attack.'}
        } elseif($e.nativeCommand.result -cne 'Success') {
            $recovery=$e.nativeRecovery
            if($case.mounted -or $e.nativeCommand.result -cne 'Interrupt' -or $null -eq $recovery -or
                $recovery.boundary -cne 'native-recovery-interrupt' -or $recovery.commandType -cne 'Kingmaker.UnitLogic.Commands.UnitAttack' -or
                $recovery.command -ne $e.nativeCommand.id -or $recovery.caseId -cne $case.id -or $recovery.result -cne 'Success' -or
                $recovery.planned -ne $e.planned -or $recovery.completed -ne $e.planned -or $null -ne $recovery.plannedWeapon -or
                !$recovery.detail.Contains('Kingmaker.UnitLogic.Commands.UnitAttack.OnTick')) {
                throw 'Unproven or premature interruption cannot qualify native completion.'
            }
        }
        if($null -eq $e.variation -or $e.variation.rapidShot -ne $case.rapid -or
            ($null -ne $case.bab -and $e.variation.babBase -ne $case.bab) -or
            $case.haste -ne (![string]::IsNullOrEmpty([string]$e.variation.haste)) -or
            ($case.haste -and $e.variation.haste -cne '03464790f40c3c24aa684b57155f3280')){throw 'Native fixture inputs differ.'}
        if($case.preparation -ceq 'stale-staggered' -and ($e.variation.staggered -ne $true -or $e.prediction.fullEnabled -ne $true)) {
            throw 'Stale full prediction was not revalidated against native Staggered.'
        }
        if($case.preparation -in @('native-single','spent-standard') -and
            ($e.prediction.nativeCursorCycles -lt 1 -or $e.prediction.fullAfterChoice -ne $false)){throw 'Native Single cursor input is missing.'}
        if($case.preparation -ceq 'spent-standard') {
            $spent=$e.spentStandard
            if($null -eq $spent -or $spent.sameNativeTurn -ne $true -or $spent.secondAttackStarted -ne $false -or
                $spent.rulesBefore.riderResolved -ne 1 -or $spent.rulesAfter.riderResolved -ne 1 -or
                $spent.before.rider.standard -ne 6 -or $spent.after.rider.standard -ne 6 -or $spent.after.rider.move -ne 0) {
                throw 'A spent native Standard was refreshed or admitted another attack.'
            }
        }
        if($case.preparation -cin @('rider-move','carried-move')) {
            $movement=$e.movement;$owner=if($case.mounted){'mount'}else{'rider'}
            if($null -eq $movement -or $movement.passed -ne $true -or $movement.sameNativeTurn -ne $true -or
                $movement.displacement -le 2 -or $movement.command.result -cne 'Success' -or
                $movement.command.executor -cne $movement.before.$owner.id -or
                $movement.after.$owner.move -le $movement.before.$owner.move -or
                $movement.after.rider.standard -ne $movement.before.rider.standard -or
                ($case.mounted -and $movement.after.rider.move -ne $movement.before.rider.move)) {
                throw 'Native physical movement or actor-owned expenditure is unproven.'
            }
        }
        if(@($e.nativePlan).Count -ne $e.planned -or @($e.rules.attackRollEvents).Count -ne $e.completed) {
            throw 'Native attack modifiers/plan observations are missing.'
        }
        $rollIndex=0
        foreach($roll in $e.rules.attackRollEvents) {
            if($roll.actor -cne $e.before.rider.id -or $null -eq $roll.attackBonus -or $null -eq $roll.iterativePenalty){throw 'Native rule ownership or modifiers are absent.'}
            $plan=$e.nativePlan[$rollIndex++]
            if($roll.weapon -cne $plan.weapon -or $roll.ranged -ne $plan.ranged -or $roll.iterativePenalty -ne $plan.penalty) {
                throw 'Delivered weapon or iterative penalty differs from the native plan.'
            }
            Assert-KmcNativeAttackBonusEvidence $roll
        }
    }
    if($pass -ne $Artifact.subscenarioPassCount -or $fail -ne $Artifact.subscenarioFailCount){throw 'Ordinary control counts differ.'}
    if($Artifact.status -ceq 'PASS') {
        if($fail -ne 0 -or @($Artifact.errors).Count -ne 0){throw 'Ordinary PASS includes a failure.'}
        $evidence=@{}
        foreach($case in $cases){
            if(-not $names.Contains($case.id)){throw 'Matched ordinary control omitted.'}
            $evidence[$case.id]=@($Artifact.rows|Where-Object name -ceq $case.id)[0].evidence
        }
        foreach($prefix in @('C01','C03-rapid-off','C03-bab','C03-haste','C02-restricted','C03-single','C03-spent-standard','C03-mixed-range')) {
            $baseline=$evidence[$prefix+'-B'];$mounted=$evidence[$prefix+'-C']
            if($baseline.planned -ne $mounted.planned -or
                ($baseline.nativePlan|ConvertTo-Json -Depth 8 -Compress) -cne ($mounted.nativePlan|ConvertTo-Json -Depth 8 -Compress)) {
                throw 'Matched mounted and native plans differ.'
            }
            $baseRolls=@($baseline.rules.attackRollEvents);$mountedRolls=@($mounted.rules.attackRollEvents)
            if($baseRolls.Count -ne $mountedRolls.Count){throw 'Matched native delivery counts differ.'}
            for($index=0;$index -lt $baseRolls.Count;$index++) {
                if($baseRolls[$index].nativeCalculation.innerResult -ne $mountedRolls[$index].nativeCalculation.innerResult -or
                    $baseRolls[$index].statModifier -ne $mountedRolls[$index].statModifier -or
                    $baseRolls[$index].secondaryBonus -ne $mountedRolls[$index].secondaryBonus) {
                    throw 'Matched target-independent native attack inputs differ.'
                }
            }
        }
        foreach($control in @('B','C')) {
            $rapid=$evidence['C01-'+$control];$off=$evidence['C03-rapid-off-'+$control]
            $bab=$evidence['C03-bab-'+$control];$haste=$evidence['C03-haste-'+$control]
            if($rapid.planned -ne $off.planned+1 -or $haste.planned -ne $bab.planned+1 -or $bab.planned -ge $off.planned) {
                throw 'Native Rapid Shot/BAB/haste attack-count controls differ.'
            }
            $rapidBonus=($rapid.rules.attackRollEvents|Where-Object ranged|ForEach-Object {$_.nativeCalculation.innerResult}|Measure-Object -Maximum).Maximum
            $offBonus=($off.rules.attackRollEvents|Where-Object ranged|ForEach-Object {$_.nativeCalculation.innerResult}|Measure-Object -Maximum).Maximum
            $babBonus=($bab.rules.attackRollEvents|Where-Object ranged|ForEach-Object {$_.nativeCalculation.innerResult}|Measure-Object -Maximum).Maximum
            $hasteBonus=($haste.rules.attackRollEvents|Where-Object ranged|ForEach-Object {$_.nativeCalculation.innerResult}|Measure-Object -Maximum).Maximum
            if($rapidBonus -ne $offBonus-2 -or $hasteBonus -ne $babBonus+1){throw 'Native Rapid Shot/haste attack modifiers differ.'}
        }
        if(($evidence['C03-carried-move-C'].nativePlan|ConvertTo-Json -Depth 8 -Compress) -cne
            ($evidence['C01-C'].nativePlan|ConvertTo-Json -Depth 8 -Compress)) {
            throw 'Mount transport changed the rider native full plan.'
        }
        $trace=$Artifact.observations.ordinaryAttackTrace
        if($trace.dropped -ne 0 -or @($trace.events|Where-Object {$_.PSObject.Properties.Name -contains 'observationError'}).Count -ne 0){throw 'Native trace is incomplete.'}
        foreach($boundary in @('simulation-before','plan-after','start-after','delivery-after','ended','cost-after')) {
            if(@($trace.events|Where-Object boundary -ceq $boundary).Count -eq 0){throw 'Native trace boundary is missing.'}
        }
    }
    if($Status -ceq 'PASS' -and $Artifact.status -cne 'PASS'){throw 'Runtime PASS contains failed ordinary controls.'}
}
