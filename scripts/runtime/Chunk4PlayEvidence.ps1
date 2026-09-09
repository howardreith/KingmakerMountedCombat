function Assert-KmcChunk4PlayEvidence {
    param($Request,$Artifact,[AllowNull()][string]$Status)
    $roots=@('chunk4-sustained-melee-rt','chunk4-sustained-ranged-rt','chunk4-sustained-tb')
    if($Artifact.schemaVersion -ne 21 -or $Request.scenario -cnotin $roots){throw 'Sustained play requires exact schema21 and a registered root.'}
    Assert-KmcMountedRuntimeConfiguration $Artifact.observations.phase3fActualConfiguration $true 'Chunk 4 sustained configuration'
    $tb=$Request.scenario -ceq 'chunk4-sustained-tb'
    if($tb){
        $required=@('C4-SUSTAINED-TB-rider-first','C4-SUSTAINED-TB-mount-first','C4-SUSTAINED-TB-rider-exhausted',
            'C4-SUSTAINED-TB-mount-exhausted','C4-SUSTAINED-TB-early-end','C4-SUSTAINED-TB-after-early-end')
    }else{
        $weapon=if($Request.scenario -ceq 'chunk4-sustained-ranged-rt'){'ranged'}else{'melee'}
        $required=@('adjacent-held','adjacent-repeat','approach-held','approach-repeat')|ForEach-Object {'C4-SUSTAINED-'+$weapon+'-'+$_}
    }
    $failureOnly=@('phase3d-horse-tranche-cleanup','phase3d-horse-scenario-deadline','phase3d-horse-leaf-deadline','phase3d-horse-runtime-exception')
    $names=New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    $identities=New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    $pass=0;$fail=0
    foreach($row in $Artifact.rows){
        if($row.name -cnotin ($required+$failureOnly) -or !$names.Add([string]$row.name) -or $row.status -cnotin @('PASS','FAIL')){throw 'Invalid or duplicate sustained row.'}
        if($row.status -ceq 'FAIL'){$fail++;continue}
        $pass++
        if($row.name -cin $failureOnly -or $row.evidence.level -cne 'NATIVE INTEGRATION' -or $row.evidence.caseId -cne $row.name){throw 'Sustained PASS lacks exact native identity.'}
        if($tb){
            if(!$identities.Add([string]$row.evidence.identity)){throw 'A new paired turn reused an earlier activation identity.'}
            Assert-KmcChunk4PairedPlayRow $row
        }else{Assert-KmcChunk4SustainedRow $row $Artifact $weapon}
    }
    if($Artifact.subscenarioPassCount -ne $pass -or $Artifact.subscenarioFailCount -ne $fail){throw 'Sustained evidence counts disagree with rows.'}
    if($Artifact.status -ceq 'PASS' -and ($pass -ne $required.Count -or $fail -ne 0 -or @($Artifact.errors).Count -ne 0)){throw 'Sustained PASS omitted a mandatory new native case.'}
    if($Status -ceq 'PASS' -and $Artifact.status -cne 'PASS'){throw 'Runtime PASS includes failed sustained evidence.'}
    if($Artifact.status -ceq 'PASS'){
        $trace=$Artifact.observations.ordinaryTrace
        if($trace.dropped -ne 0 -or @($trace.events|Where-Object {$_.PSObject.Properties['observationError']}).Count -ne 0){throw 'Sustained native observations were dropped or failed.'}
    }
}

function Assert-KmcChunk4SustainedRow {
    param($Row,$Artifact,[string]$Weapon)
    $e=$Row.evidence;$repeat=$Row.name.EndsWith('-repeat');$approach=$Row.name.Contains('-approach-')
    if($e.mode -cne 'RT' -or $e.repeat -isnot [bool] -or $e.repeat -ne $repeat -or
        $e.approach -isnot [bool] -or $e.approach -ne $approach -or $e.ranged -isnot [bool] -or
        $e.ranged -ne ($Weapon -ceq 'ranged') -or $e.inputKind -cne 'scripted-native-pointer-prediction-and-click' -or
        $e.nativeEnoughCloseBefore -isnot [bool] -or $e.nativeEnoughCloseBefore -eq $approach -or
        $e.intentStarts -ne 1 -or $e.duplicateDispatches -ne 0 -or $e.rules.pairForcedD20 -ne 0 -or
        @($e.samples).Count -lt 12 -or @($e.riderStartPeriods).Count -ne 2 -or
        $e.before.relationship -cne 'Mounted' -or $e.afterStop.relationship -cne 'Mounted'){throw 'Sustained RT parameters, continuity or native sample scope is incomplete.'}
    $rider=$e.before.rider.id;$mount=$e.before.mount.id
    if([string]::IsNullOrEmpty($rider) -or [string]::IsNullOrEmpty($mount) -or $rider -ceq $mount){throw 'Sustained actor identities are not independent.'}
    foreach($actor in @($e.before.rider,$e.before.mount)){
        if($actor.standard -ne 0 -or $actor.move -ne 0){throw 'Sustained comparison did not begin with native readiness.'}
    }
    if($e.targetProvisioning.durabilityLeaseAmount -ne 4096 -or $e.targetProvisioning.targetId -cne $e.target){throw 'Sustained target lacks its initial durable fixture lease.'}
    $lastHp=[double]$e.targetProvisioning.temporaryHitPointsAfterProvisioning
    foreach($sample in $e.samples){
        foreach($name in @('time','riderStandard','riderMove','mountStandard','mountMove','targetTemporaryHp')){
            if(!(Test-KmcFiniteNonnegativeJsonNumber $sample.$name)){throw 'Sustained sample has invalid native numbers.'}
        }
        if($sample.riderMove -gt 0.001 -or $sample.targetTemporaryHp -gt $lastHp){throw 'Transport taxed rider Move or target durability was replenished.'}
        $lastHp=[double]$sample.targetTemporaryHp
    }
    if($repeat -and @($e.clicks).Count -lt 12){throw 'Repeated-input comparison has too few actual clicks.'}
    if(!$repeat -and @($e.clicks).Count -ne 0){throw 'Held-order control contains repeated requests.'}
    foreach($click in $e.clicks){
        if($click.pure -ne $true -or
            (ConvertTo-Json -InputObject $click.before -Depth 30 -Compress) -cne (ConvertTo-Json -InputObject $click.after -Depth 30 -Compress) -or
            (@($click.clocksBefore)-join ',') -cne (@($click.clocksAfter)-join ',')){throw 'Repeated input changed its native command, windup clock or costs.'}
    }
    $seen=New-Object 'Collections.Generic.HashSet[int]'
    $complete=0;$riderDelivered=0;$mountDelivered=0;$fullRider=0;$tailRider=0
    foreach($routine in $e.routines){
        $command=$routine.command
        if(!$seen.Add([int]$command.id) -or $routine.actor -cnotin @($rider,$mount) -or $command.executor -cne $routine.actor -or
            $command.started -ne $true -or $command.finished -ne $true -or
            !(Test-KmcExactJsonInteger $routine.planned) -or !(Test-KmcExactJsonInteger $routine.completed) -or
            $routine.planned -lt 1 -or $routine.completed -lt 0 -or $routine.completed -gt $routine.planned){throw 'Sustained routine identity, native plan or terminal state is invalid.'}
        $events=@($e.nativeTrace|Where-Object {$_.command -eq $command.id})
        $starts=@($events|Where-Object {$_.boundary -ceq 'start-after'})
        $costBefore=@($events|Where-Object {$_.boundary -ceq 'cost-before'})
        $costAfter=@($events|Where-Object {$_.boundary -ceq 'cost-after'})
        if($starts.Count -ne 1 -or $starts[0].actor -cne $routine.actor -or $starts[0].standard -gt 0.001){throw 'Native routine restarted or began without readiness.'}
        if($routine.completed -gt 0){
            if($command.acted -ne $true -or $costBefore.Count -ne 1 -or $costAfter.Count -ne 1 -or
                $costBefore[0].frame -ne $costAfter[0].frame -or $costAfter[0].standard -le $costBefore[0].standard){throw 'Delivered routine lacks one genuine native expenditure callback.'}
        }elseif($costBefore.Count -ne 0 -or $costAfter.Count -ne 0 -or $command.acted -ne $false){throw 'Unacted stopped routine spent native actions.'}
        if($routine.completedObserved -eq $true){
            $tail=$null -ne $routine.PSObject.Properties['nativeRangedTailTermination'] -and $routine.nativeRangedTailTermination -eq $true
            if($tail){
                $range=$routine.nativeRangeRejection
                if($Weapon -cne 'ranged' -or $routine.actor -cne $rider -or $command.result -cne 'Interrupt' -or
                    $routine.completed -lt 1 -or $routine.completed -ge $routine.planned -or $null -eq $range -or
                    $range.boundary -cne 'target-invalid' -or $range.command -ne $command.id -or $range.completed -ne $routine.completed -or
                    $range.targetDead -ne $false -or $range.targetUnconscious -ne $false -or $range.targetInState -ne $true -or
                    $range.nativeActorLoS -ne $true -or $range.mountCorpulence -lt 0 -or $range.targetCorpulence -lt 0 -or
                    $range.rangeOriginDistance -le $range.pairApproachRadius -or @($range.plan).Count -ne $routine.planned){throw 'Ranged tail termination lacks its exact native range observation.'}
                if(@($events|Where-Object {$_.boundary -ceq 'target-invalid' -and $_.completed -eq $routine.completed}).Count -lt 1){throw 'Ranged termination lacks a native UpdateTarget rejection event.'}
                for($i=0;$i -lt $range.plan.Count;$i++){
                    if($range.plan[$i].ranged -ne ($i -lt $routine.completed)){throw 'Ranged termination skipped an eligible ranged attack or misclassified its prefix.'}
                    $radius=$range.mountCorpulence+$range.targetCorpulence+$range.plan[$i].weaponRange
                    if(($i -lt $routine.completed -and $range.rangeOriginDistance -gt $radius) -or
                        ($i -ge $routine.completed -and $range.rangeOriginDistance -le $radius)){throw 'Ranged terminal changed an individual native weapon reach.'}
                }
            }elseif($routine.completed -ne $routine.planned -or $command.result -cne 'Success'){throw 'Incomplete interrupted routine was counted as complete.'}
            if($routine.actor -ceq $rider){$complete++;if($tail){$tailRider++}else{$fullRider++}}
        }
        if($routine.actor -ceq $rider){$riderDelivered+=$routine.completed}else{$mountDelivered+=$routine.completed}
    }
    if($null -ne $e.PSObject.Properties['nativeFullRiderRoutines'] -and
        ($e.nativeFullRiderRoutines -ne $fullRider -or $e.nativeRangedTailRiderRoutines -ne $tailRider)){throw 'Full native plans were conflated with ranged-tail terminals.'}
    if($complete -lt 3 -or $complete -ne $e.completeRiderRoutines -or
        $riderDelivered -ne $e.rules.riderNonOpportunityAttackRules -or $riderDelivered -ne $e.rules.riderResolved -or
        $mountDelivered -ne $e.rules.mountNonOpportunityAttackRules -or $mountDelivered -ne $e.rules.mountResolved){throw 'Sustained native plans, deliveries and projectile resolutions disagree.'}
    foreach($actor in @($e.afterStop.rider,$e.afterStop.mount)){
        if(@($actor.raw|Where-Object {$null -ne $_}).Count -ne 0 -or @($actor.queue).Count -ne 0){throw 'Stop left a native command or queue behind.'}
    }
    foreach($actor in @('rider','mount')){
        foreach($cost in @('standard','move')){
            if($e.beforeStop.$actor.$cost -ne $e.afterStopInput.$actor.$cost){throw 'Stop refunded or added a genuine actor cost.'}
        }
    }
    if($approach -and $e.mountDistance -le 0.5){throw 'Approach case did not move the real mount.'}
    if($repeat){
        $heldName=$Row.name.Replace('-repeat','-held')
        $held=@($Artifact.rows|Where-Object {$_.name -ceq $heldName -and $_.status -ceq 'PASS'})
        if($held.Count -ne 1 -or $held[0].evidence.weapon -cne $e.weapon){throw 'Repeated input lacks its matched held-order control.'}
        $heldPeriods=@($held[0].evidence.riderStartPeriods);$repeatPeriods=@($e.riderStartPeriods)
        $tolerance=2*[Math]::Max([double]$held[0].evidence.maximumNativeFrameStep,[double]$e.maximumNativeFrameStep)+0.02
        if($tolerance -gt 0.25 -or [Math]::Abs($e.cadenceComparison.observedFrameTolerance-$tolerance) -gt 0.000000001 -or
            ($repeatPeriods|Measure-Object -Average).Average -lt ($heldPeriods|Measure-Object -Minimum).Minimum-$tolerance){throw 'Repeated input accelerated cadence or the measured frame variation cannot resolve the comparison.'}
    }
}

function Assert-KmcChunk4PairedPlayRow {
    param($Row)
    $e=$Row.evidence;$rider=$e.before.rider.id;$mount=$e.before.mount.id
    if($e.mode -cne 'TB' -or [string]::IsNullOrEmpty($e.identity) -or $rider -ceq $mount -or
        $e.principal -cne $rider -or $e.partner -cne $mount -or $e.riderPrepared -ne $true -or $e.mountPrepared -ne $true -or
        $e.before.relationship -cne 'Mounted'){throw 'Paired play lost its principal, partner or native preparation.'}
    foreach($actor in @($e.before.rider,$e.before.mount)){
        if($actor.standard -ne 0 -or $actor.move -ne 0){throw 'Fresh native paired turn retained an earlier debit.'}
    }
    if($Row.name -ceq 'C4-SUSTAINED-TB-after-early-end'){
        if(@($e.operations).Count -ne 0){throw 'Fresh post-End observation manufactured an action.'}
        return
    }
    if([string]::IsNullOrEmpty($e.nextUnrelatedActor) -or $e.nextUnrelatedActor -cin @($rider,$mount)){throw 'Paired End lacks a subsequent unrelated native actor.'}
    if($Row.name -ceq 'C4-SUSTAINED-TB-early-end'){
        if($e.earlyEnd -ne $true -or @($e.operations).Count -ne 0){throw 'Early End was not an unused native activation.'}
        foreach($actor in @($e.beforeEnd.rider,$e.beforeEnd.mount)){
            if($actor.standard -ne 0 -or $actor.move -ne 0){throw 'Early End secretly spent an action.'}
        }
        return
    }
    $mountFirst=$Row.name -cin @('C4-SUSTAINED-TB-mount-first','C4-SUSTAINED-TB-mount-exhausted')
    $firstFull=$Row.name -cin @('C4-SUSTAINED-TB-rider-exhausted','C4-SUSTAINED-TB-mount-exhausted')
    $partnerMove=$Row.name -ceq 'C4-SUSTAINED-TB-rider-exhausted'
    $count=if($partnerMove){3}else{2}
    if($e.earlyEnd -ne $false -or @($e.operations).Count -ne $count){throw 'Paired play omitted its requested native action sequence.'}
    for($index=0;$index -lt 2;$index++){
        $operation=$e.operations[$index];$isMount=$mountFirst -eq ($index -eq 0)
        $actorKey=if($isMount){'mount'}else{'rider'};$otherKey=if($isMount){'rider'}else{'mount'}
        $actor=if($isMount){$mount}else{$rider};$full=$firstFull -and $index -eq 0
        $kind=if($full){'ordinary-Full'}else{'explicit-Primary'};$move=if($full){3}else{0}
        if($operation.kind -cne $kind -or $operation.actor -cne $actor -or $operation.beforeIdentity -cne $e.identity -or
            $operation.command.executor -cne $actor -or $operation.command.started -ne $true -or $operation.command.acted -ne $true -or
            $operation.command.finished -ne $true -or $operation.command.result -cne 'Success' -or
            $operation.nativeFull -ne $full -or $operation.nativePrimary -eq $full -or
            $operation.completed -lt 1 -or $operation.completed -ne $operation.planned -or $operation.resolved -ne $operation.completed -or
            (!$full -and $operation.completed -ne 1) -or $operation.rulesAfter.pairForcedD20 -ne 0 -or
            $operation.after.$actorKey.standard -ne 6 -or $operation.after.$actorKey.move -ne $move -or
            $operation.after.$otherKey.standard -ne $operation.before.$otherKey.standard -or
            $operation.after.$otherKey.move -ne $operation.before.$otherKey.move){throw 'Paired attack mode, native sequence or independent costs disagree.'}
        if($index -eq 1 -and ($operation.before.$otherKey.standard -ne 6 -or $operation.before.$actorKey.standard -ne 0)){
            throw 'The second actor did not act from its own untouched budget after the first spent.'
        }
    }
    if($partnerMove){
        $operation=$e.operations[2]
        if($operation.kind -cne 'partner-move-after-rider-exhaustion' -or $operation.command.executor -cne $mount -or
            $operation.command.finished -ne $true -or $operation.command.result -cne 'Success' -or
            $operation.before.rider.standard -ne 6 -or $operation.before.rider.move -ne 3 -or
            $operation.after.rider.standard -ne 6 -or $operation.after.rider.move -ne 3 -or
            $operation.after.mount.standard -ne 6 -or $operation.after.mount.move -le 0 -or $operation.after.mount.move -gt 3){throw 'Partner movement changed exhausted rider costs or exceeded remaining native movement.'}
    }
}
