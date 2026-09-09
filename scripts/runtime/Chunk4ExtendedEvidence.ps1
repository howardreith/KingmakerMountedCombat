function Test-KmcChunk4ExtendedScenario {
    param([string]$Scenario)
    return $Scenario -cin @('chunk4-obstruction-ranged-rt','chunk4-interrupt-melee-rt','chunk4-interrupt-ranged-rt','chunk4-inspection-rt','chunk4-session-rt','chunk4-session-tb')
}
function Get-KmcChunk4ExtendedLeaves {
    param([string]$Scenario)
    if(!(Test-KmcChunk4ExtendedScenario $Scenario)){throw 'Unregistered extended Chunk 4 scenario.'}
    if($Scenario -ceq 'chunk4-obstruction-ranged-rt'){return @('C4-OBSTRUCTION-ranged-native-geometry')}
    if($Scenario -ceq 'chunk4-inspection-rt'){return @('C4-INSPECTION-rider','C4-INSPECTION-mount')}
    if($Scenario.StartsWith('chunk4-session-')){$mode=if($Scenario.EndsWith('-tb')){'TB'}else{'RT'};return @(1..3|ForEach-Object {"C4-SESSION-$mode-$_"})}
    $weapon=if($Scenario -ceq 'chunk4-interrupt-ranged-rt'){'ranged'}else{'melee'}
    $cases=@('pause-resume','pause-stop-recover','moving-target','retarget-windup')
    if($weapon -ceq 'ranged'){$cases+=@('retarget-inflight')}
    $cases+=@('target-death-windup');$cases+=if($weapon -ceq 'ranged'){'target-death-inflight'}else{'target-death-midroutine'}
    return @($cases|ForEach-Object {"C4-INTERRUPT-$weapon-$_"})
}
function Assert-KmcChunk4ExtendedEvidence {
    param($Request,$Artifact,[AllowNull()][string]$Status)
    if($Artifact.schemaVersion -ne 23 -or !(Test-KmcChunk4ExtendedScenario $Request.scenario)){throw 'Extended Chunk 4 requires schema23 and an exact registered root.'}
    Assert-KmcMountedRuntimeConfiguration $Artifact.observations.phase3fActualConfiguration $true 'Chunk 4 extended configuration'
    $required=@(Get-KmcChunk4ExtendedLeaves $Request.scenario)
    $failureOnly=@('phase3d-horse-tranche-cleanup','phase3d-horse-scenario-deadline','phase3d-horse-leaf-deadline','phase3d-horse-runtime-exception')
    $names=New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal);$pass=0;$fail=0
    foreach($row in $Artifact.rows){
        if($row.name -cnotin ($required+$failureOnly) -or !$names.Add([string]$row.name) -or $row.status -cnotin @('PASS','FAIL')){throw 'Invalid or duplicate extended row.'}
        if($row.status -ceq 'FAIL'){$fail++;continue};$pass++
        $e=$row.evidence
        if($row.name -cin $failureOnly -or $e.level -cne 'NATIVE INTEGRATION' -or $e.caseId -cne $row.name){throw 'Extended PASS lacks native identity.'}
        if($row.name.StartsWith('C4-INSPECTION-')){Assert-KmcChunk4InspectionRow $e}
        elseif($row.name.StartsWith('C4-SESSION-')){Assert-KmcChunk4SessionRow $e $Artifact.observations.chunk4SessionSubscriptionsBefore}
        elseif($row.name -ceq 'C4-OBSTRUCTION-ranged-native-geometry'){Assert-KmcChunk4ObstructionRow $e}
        else{Assert-KmcChunk4InterruptRow $e}
    }
    if($Artifact.subscenarioPassCount -ne $pass -or $Artifact.subscenarioFailCount -ne $fail){throw 'Extended native row totals disagree.'}
    if($Artifact.status -ceq 'PASS' -and ($pass -ne $required.Count -or $fail -ne 0 -or @($Artifact.errors).Count -ne 0)){throw 'Extended PASS omits a required native case.'}
    if($Status -ceq 'PASS' -and $Artifact.status -cne 'PASS'){throw 'Runtime PASS contains a failed extended gate.'}
    if($Artifact.status -ceq 'PASS'){
        $trace=$Artifact.observations.ordinaryAttackTrace
        if($trace.dropped -ne 0 -or @($trace.events|Where-Object {$_.PSObject.Properties['observationError']}).Count -ne 0){throw 'Extended native observations dropped or failed.'}
        if($Request.scenario -ceq 'chunk4-inspection-rt' -and $Artifact.observations.chunk4InspectionClosed -ne $true){throw 'Inspection did not close its owned window.'}
    }
}
function Assert-KmcChunk4SameCosts {
    param($Before,$After)
    foreach($actor in @('rider','mount')){foreach($cost in @('standard','move','swift')){
        if($null -eq $Before.$actor.$cost -or $null -eq $After.$actor.$cost -or $Before.$actor.$cost -ne $After.$actor.$cost){throw 'Native input changed or omitted a genuine actor cost.'}
    }}
}
function Assert-KmcChunk4ResolvedRules {
    param($Rules)
    if($Rules.dropped -ne 0 -or @($Rules.attacks).Count -lt 1){throw 'Missing or dropped native rule observations.'}
    $seen=New-Object 'Collections.Generic.HashSet[long]'
    foreach($attack in $Rules.attacks){
        if(!$seen.Add([long]$attack.identity) -or $attack.resolved -ne $true){throw 'Duplicated native attack identity or unresolved released delivery.'}
        $resolved=@($Rules.events|Where-Object {$_.kind -ceq 'weapon-resolved' -and $_.attack -eq $attack.identity})
        if($resolved.Count -ne 1 -or $resolved[0].firstResolution -ne $true -or $resolved[0].actor -cne $attack.actor -or $resolved[0].target -cne $attack.target){throw 'Native delivery did not resolve exactly once for its original actor and target.'}
    }
}
function Assert-KmcChunk4NativeRoutine {
    param($Command,[int]$Completed,[int]$Planned,$Trace,[string]$Actor,[bool]$AllowRangeTail=$false)
    if($Command.executor -cne $Actor -or $Command.started -ne $true -or $Command.acted -ne $true -or $Command.finished -ne $true -or
        $Completed -lt 1 -or $Completed -gt $Planned -or (!$AllowRangeTail -and $Completed -ne $Planned)){throw 'Legal native routine is incomplete or has the wrong owner.'}
    $events=@($Trace|Where-Object {$_.command -eq $Command.id})
    if(@($events|Where-Object {$_.boundary -ceq 'start-after'}).Count -ne 1 -or
        @($events|Where-Object {$_.boundary -ceq 'delivery-after'}).Count -ne $Completed -or
        @($events|Where-Object {$_.boundary -ceq 'cost-after'}).Count -ne 1){throw 'Native start, delivery or cost callback cardinality disagrees.'}
    if($Command.result -cnotin @('Success','Interrupt')){throw 'Native legal completion faulted.'}
    if($Command.result -ceq 'Interrupt' -and !$AllowRangeTail){
        $recovery=@($events|Where-Object {$_.boundary -ceq 'native-recovery-interrupt' -and $_.completed -eq $Planned})
        if($recovery.Count -ne 1){throw 'Interrupted full delivery lacks its exact native recovery boundary.'}
    }
}
function Assert-KmcChunk4InterruptRow {
    param($e)
    if($e.mode -cne 'RT' -or $e.inputKind -cne 'native-pointer-prediction-and-click' -or $e.before.live.relationship -cne 'Mounted' -or
        $e.after.live.relationship -cne 'Mounted' -or $e.before.live.rider.id -ceq $e.before.live.mount.id){throw 'Interruption lacks the real paired ordinary control.'}
    $weapon=if($e.ranged){'ranged'}else{'melee'}
    if($e.caseId -cne "C4-INTERRUPT-$weapon-$($e.kind)"){throw 'Interruption parameter identity disagrees.'}
    Assert-KmcChunk4ResolvedRules $e.rulesAfter
    Assert-KmcChunk4SameCosts $e.beforeStop.live $e.afterStopInput.live
    Assert-KmcChunk4NativeRoutine $e.legalCompletion $e.legalCompletedAttacks $e.legalNativePlan $e.nativeTrace $e.before.live.rider.id $e.legalNativeRangedTail
    if($e.legalNativeRangedTail){
        $r=$e.legalRangeRejection
        if(!$e.ranged -or $e.legalCompletion.result -cne 'Interrupt' -or $r.command -ne $e.legalCompletion.id -or
            $r.nativeCommandLoS -ne $true -or $r.nativeSequenceTick -ne $true -or $r.nativeMeleeTailRangeRejected -ne $true -or
            $r.targetDead -ne $false -or $r.targetUnconscious -ne $false -or $r.targetInState -ne $true -or
            $r.completed -ne $e.legalCompletedAttacks -or @($r.plan).Count -ne $e.legalNativePlan -or $r.completed -ge $r.plan.Count){throw 'Interruption recovery misclassified its actual native ranged tail.'}
        for($i=0;$i -lt $r.plan.Count;$i++){
            $radius=$r.mountCorpulence+$r.targetCorpulence+$r.plan[$i].weaponRange
            if($r.plan[$i].ranged -ne ($i -lt $r.completed) -or
                ($i -lt $r.completed -and $r.rangeOriginDistance -gt $radius) -or
                ($i -ge $r.completed -and $r.rangeOriginDistance -le $radius+.05)){throw 'Recovery skipped an eligible native weapon range.'}
        }
    }
    if($e.kind.StartsWith('pause-')){
        if($e.pauseDuration -lt .4 -or ($e.pausedBegin|ConvertTo-Json -Depth 30 -Compress) -cne ($e.pausedEnd|ConvertTo-Json -Depth 30 -Compress)){throw 'Paused command changed or lacked a real observation interval.'}
        if($e.kind -ceq 'pause-stop-recover'){
            Assert-KmcChunk4SameCosts $e.pausedBegin.live $e.pausedAfterStop.live
            if($e.legalCompletion.id -eq $e.beforeStimulus.firstCommand.id){throw 'Stop recovery reused the cancelled command.'}
        }elseif($e.legalCompletion.id -ne $e.beforeStimulus.firstCommand.id){throw 'Pause restarted the native windup.'}
    }
    if($e.kind -ceq 'moving-target' -and ($e.targetMoved -lt 1 -or $e.targetMove.result -cne 'Success' -or $e.targetMove.executor -cne $e.target)){throw 'Moving target did not complete a real native move.'}
    if($e.kind -ceq 'moving-target' -and $e.ranged){
        $path=$e.targetPath
        if($path.accepted -ne $true -or $path.error -ne $false -or $path.direct -lt 2.5 -or $path.direct -gt 3.5 -or
            $path.length -lt $path.direct-.01 -or $path.length -gt $path.direct*1.5+.5 -or $path.endpointError -gt .3 -or
            @($path.samples).Count -lt 2 -or @($path.samples).Count -gt 128 -or $path.before.target.id -cne $e.target -or
            $path.before.rider.id -cne $e.before.live.rider.id -or $path.before.mount.id -cne $e.before.live.mount.id -or
            ($path.before|ConvertTo-Json -Depth 30 -Compress) -cne ($path.after|ConvertTo-Json -Depth 30 -Compress)){
            throw 'Ranged moving-target setup lacks a bounded native path with unchanged actor state.'
        }
        foreach($sample in $path.samples){
            if($sample.blocked -ne $false -or $sample.distance -gt $path.radius-.5){throw 'Ranged moving-target path crosses obstruction or leaves native reach.'}
        }
    }
    if($e.kind.StartsWith('target-death-')){
        if($e.damageDispatches -ne 1 -or $e.nativeDamage -le 0 -or $e.beforeStimulus.target.dead -ne $false -or
            $e.after.target.dead -ne $true -or $e.targetLifeTransitions -lt 1){throw 'Target death lacks actual native effect and life transition.'}
        Assert-KmcChunk4SameCosts $e.beforeStimulus.live $e.afterStimulusInput.live
    }elseif($e.damageDispatches -ne 0){throw 'Nondeath interruption injected damage.'}
    if($e.kind.EndsWith('inflight')){
        if(@($e.inFlightAtStimulus).Count -lt 1){throw 'Requested in-flight interruption had no released native projectile.'}
        foreach($flight in $e.inFlightAtStimulus){
            $projectile=@($e.beforeStimulus.nativeProjectiles|Where-Object {$_.attack -eq $flight.identity -and $_.target -ceq $e.target -and
                $_.hit -eq $false -and $_.destroyed -eq $false})
            if($flight.resolved -ne $false -or $projectile.Count -ne 1){throw 'In-flight stimulus lacks its real live projectile.'}
            $resolved=@($e.rulesAfter.events|Where-Object {$_.kind -ceq 'weapon-resolved' -and $_.attack -eq $flight.identity})
            if($resolved.Count -ne 1 -or $resolved[0].target -cne $e.target){throw 'Released projectile lost or changed its original target.'}
        }
    }elseif($e.kind.EndsWith('windup') -and $e.beforeStimulus.firstIndex -ne 0){throw 'Requested windup interruption followed a delivery.'}
    elseif($e.kind.EndsWith('midroutine') -and ($e.beforeStimulus.firstIndex -lt 1 -or $e.beforeStimulus.firstCommand.finished -ne $false)){throw 'Requested middle-of-routine interruption missed its native window.'}
    if($e.kind.StartsWith('retarget-') -or $e.kind.StartsWith('target-death-')){
        $start=@($e.nativeTrace|Where-Object {$_.boundary -ceq 'start-after' -and $_.command -eq $e.legalCompletion.id})
        if([string]::IsNullOrWhiteSpace($e.otherTarget) -or $e.otherTarget -ceq $e.target -or $start[0].target -cne $e.otherTarget){throw 'Retarget or postdeath legal input failed its distinct target.'}
    }
}
function Assert-KmcChunk4InspectionRow {
    param($e)
    $actor=if($e.caseId.EndsWith('-rider')){'rider'}else{'mount'}
    if($e.inputKind -cne 'native-character-hotkey-and-group-selection' -or $e.actor -cne $e.before.$actor.id -or
        $e.boundActor -cne $e.actor -or $e.groupActor -cne $e.actor -or $e.shown -ne $true -or $e.active -ne $true -or
        $e.screenIndex -ne 1 -or [string]::IsNullOrWhiteSpace($e.displayedName)){throw 'Native independent character binding is incomplete.'}
    Assert-KmcChunk4SameCosts $e.before $e.after
    foreach($unit in @('rider','mount')){if(($e.before.$unit.position|ConvertTo-Json -Compress) -cne ($e.after.$unit.position|ConvertTo-Json -Compress)){throw 'Inspection moved the pair.'}}
}
function Assert-KmcChunk4SessionRow {
    param($e,$SubscriptionsBefore)
    if($e.mode -cnotin @('RT','TB') -or $e.cycle -lt 1 -or $e.cycle -gt 3 -or $e.caseId -cne "C4-SESSION-$($e.mode)-$($e.cycle)" -or
        $e.inputKind -cne 'native-mount-ordinary-pointer-combat-exit-dismount' -or $e.mountedBeforeCombat -ne $true){throw 'Session lacks exact ordinary pre-combat cycle identity.'}
    Assert-KmcChunk4ResolvedRules $e.rulesAfter
    Assert-KmcChunk4SameCosts $e.beforeStop.live $e.afterStopInput.live
    Assert-KmcChunk4NativeRoutine $e.riderRoutine $e.riderCompleted $e.riderPlan $e.nativeTrace $e.beforeOrdinary.live.rider.id
    Assert-KmcChunk4NativeRoutine $e.mountRoutine $e.mountCompleted $e.mountPlan $e.nativeTrace $e.beforeOrdinary.live.mount.id
    $selected=if($e.cycle -eq 2){$e.beforeOrdinary.live.mount.id}else{$e.beforeOrdinary.live.rider.id}
    if($e.selectedFirst -cne $selected -or $e.beforeOrdinary.live.relationship -cne 'Mounted' -or
        $e.beforeDeath.playerCombat -ne $true -or $e.damageDispatches -ne 1 -or $e.nativeDamage -le 0 -or $e.nativeLifeTransitions -lt 1 -or
        $e.nativeEncounterExit.targetLife.dead -ne $true){throw 'Session did not exercise ordinary selection and actual native enemy death.'}
    foreach($state in @($e.nativeEncounterExit,$e.afterDismount,$e.settledAfter)){
        foreach($field in @('playerCombat','riderCombat','mountCombat','tbActive','tbInitialized')){if($state.$field -ne $false){throw 'Native session did not exit its real combat loop.'}}
    }
    foreach($state in @($e.afterDismount,$e.settledAfter)){
        if($state.live.relationship -cne 'Unmounted' -or $state.records -ne 0 -or $null -ne $state.privatePartner -or
            $state.attachmentResidue -ne $false -or $state.attachmentRestored -ne $true -or $state.poseRestored -ne $true){throw 'Native session left actor, partner or presentation state.'}
    }
    $before=@($SubscriptionsBefore.entries|ForEach-Object {"$($_.interface):$($_.type):$($_.identity)"}|Sort-Object)
    $after=@($e.subscriptionsAfter.entries|ForEach-Object {"$($_.interface):$($_.type):$($_.identity)"}|Sort-Object)
    if($e.recordsAfter -ne 0 -or $e.subscriptionsAfter.executing -ne $false -or $before.Count -lt 1 -or
        ($before -join "`n") -cne ($after -join "`n")){throw 'Repeated session retained or lost an owned native subscription.'}
}

function Assert-KmcChunk4ObstructionRow {
    param($e)
    $rider=$e.before.live.rider.id;$mount=$e.before.live.mount.id
    if($e.caseId -cne 'C4-OBSTRUCTION-ranged-native-geometry' -or $e.mode -cne 'RT' -or $e.inputKind -cne 'native-pointer-prediction-and-click' -or
        $e.initialQueryPure -ne $true -or $e.inputAccepted -isnot [bool] -or $e.before.live.relationship -cne 'Mounted' -or
        $rider -ceq $mount -or $e.before.sight.actor -cne $rider -or $e.before.sight.target -cne $e.before.target.id -or
        $e.before.sight.needLoS -ne $true -or $e.before.sight.blocked -ne $true -or $e.before.sight.enoughClose -ne $false -or
        $e.before.sight.weapon -cne 'd5947b9cb1500c040b026bf6b4b57fa7' -or $e.before.mountCorpulence -le 0 -or
        $e.observationSeconds -le 0 -or $e.blockedFrames -lt 1 -or $e.sampleDrops -ne 0 -or
        @($e.samples).Count -lt 1 -or @($e.samples).Count -gt 512 -or @($e.candidates).Count -lt 1 -or @($e.candidates).Count -gt 32){
        throw 'Obstruction lacks a bounded actual native command visibility failure.'
    }
    Assert-KmcChunk4SameCosts $e.before.live $e.afterInput.live
    Assert-KmcChunk4SameCosts $e.beforeStop.live $e.afterStopInput.live
    Assert-KmcChunk4SameCosts $e.recoveryBeforeStop.live $e.recoveryAfterStopInput.live
    $previousFrame=-1;$previousSeconds=[double]::NegativeInfinity;$blocked=0
    foreach($sample in @($e.samples)){
        if(!(Test-KmcExactJsonInteger $sample.frame) -or $sample.frame -le $previousFrame -or
            $sample.gameSeconds -le $previousSeconds -or $sample.sight.blocked -isnot [bool] -or
            $sample.sight.actor -cne $rider -or $sample.sight.target -cne $e.before.target.id){throw 'Obstruction samples do not describe distinct native actor/target frames.'}
        if($sample.sight.blocked){$blocked++};$previousFrame=$sample.frame;$previousSeconds=$sample.gameSeconds
    }
    if($blocked -lt 1 -or $e.blockedFrames -lt $blocked){throw 'Obstruction has no actual blocked sample.'}
    foreach($state in @($e.before,$e.afterInput,$e.beforeStop,$e.afterStopInput,$e.recoveryBefore,$e.recoveryBeforeStop,$e.recoveryAfterStopInput,$e.after)+@($e.samples)){
        if($state.live.relationship -cne 'Mounted' -or $state.live.rider.id -cne $rider -or $state.live.mount.id -cne $mount -or
            $state.live.rider.move -ne $e.before.live.rider.move -or $state.mountCorpulence -ne $e.before.mountCorpulence -or
            $state.mountAgentEnabled -ne $true -or $state.avoidanceDisabled -ne $false -or
            $state.target.conscious -ne $true -or $state.target.dead -ne $false){throw 'Obstruction changed native footprint, collision, target life or rider transport costs.'}
    }
    foreach($delivery in @($e.blockedTrace|Where-Object {$_.actor -ceq $rider -and $_.boundary -ceq 'delivery-before'})){
        if($delivery.nativeCommandLoS -ne $true -or $delivery.target -cne $e.before.target.id){throw 'Rider delivered through a native obstruction or changed the original target.'}
    }
    if($e.recoveryBefore.sight.blocked -ne $false -or $e.recoveryBefore.target.id -ceq $e.before.target.id -or
        $e.recoveryTarget -cne $e.recoveryBefore.target.id -or $e.after.intent -ne $false -or
        $e.after.activeCommand -ne $false -or $e.after.groundMovement -ne $false){throw 'Obstruction lacks a distinct clear recovery and completed Stop cleanup.'}
    Assert-KmcChunk4ResolvedRules $e.rulesAfter
    Assert-KmcChunk4NativeRoutine $e.recoveryCommand $e.recoveryCompleted $e.recoveryPlan $e.recoveryTrace $rider $e.recoveryNativeRangeTail
    if($e.recoveryNativeRangeTail){
        $r=$e.recoveryRangeRejection
        if($e.recoveryCommand.result -cne 'Interrupt' -or $r.command -ne $e.recoveryCommand.id -or
            $r.nativeCommandLoS -ne $true -or $r.nativeSequenceTick -ne $true -or $r.nativeMeleeTailRangeRejected -ne $true -or
            $r.targetDead -ne $false -or $r.targetUnconscious -ne $false -or $r.targetInState -ne $true -or
            $r.completed -ne $e.recoveryCompleted -or @($r.plan).Count -ne $e.recoveryPlan -or $r.completed -ge $r.plan.Count){throw 'Obstruction recovery lacks its actual native range-tail rejection.'}
        for($i=0;$i -lt $r.plan.Count;$i++){
            $radius=$r.mountCorpulence+$r.targetCorpulence+$r.plan[$i].weaponRange
            if($r.plan[$i].ranged -ne ($i -lt $r.completed) -or ($i -lt $r.completed -and $r.rangeOriginDistance -gt $radius) -or
                ($i -ge $r.completed -and $r.rangeOriginDistance -le $radius+.05)){throw 'Obstruction recovery skipped a native eligible weapon range.'}
        }
    }
}
