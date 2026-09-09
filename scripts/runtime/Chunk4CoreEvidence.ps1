function Test-KmcChunk4CoreScenario {
    param([string]$Scenario)
    return $Scenario -cin @('chunk4-rider-incapacitation-tb','chunk4-rider-death-tb','chunk4-mount-death-tb',
        'chunk4-targeting-rider-rt','chunk4-targeting-mount-rt','chunk4-horse-strike-comparison-rt','chunk4-ranged-native-control-rt')
}
function Get-KmcChunk4CoreLeaves {
    param([string]$Scenario)
    switch -CaseSensitive ($Scenario) {
        'chunk4-rider-incapacitation-tb' {return @('C4-LIFE-rider-incapacitation')}
        'chunk4-rider-death-tb' {return @('C4-LIFE-rider-death-live-command')}
        'chunk4-mount-death-tb' {return @('C4-LIFE-mount-death-live-command')}
        'chunk4-targeting-rider-rt' {return @('C4-TARGETING-rider-heal','C4-TARGETING-area-both','C4-TARGETING-rider-hostile')}
        'chunk4-targeting-mount-rt' {return @('C4-TARGETING-mount-heal','C4-TARGETING-mount-hostile')}
        'chunk4-horse-strike-comparison-rt' {return @('C4-HORSE-mounted-three-primaries','C4-HORSE-unmounted-strike-recovery')}
        'chunk4-ranged-native-control-rt' {return @('C4-RANGED-native-mixed-range')}
        default {throw 'Unregistered Chunk 4 native core scenario.'}
    }
}
function Assert-KmcChunk4CoreEvidence {
    param($Request,$Artifact,[AllowNull()][string]$Status)
    if($Artifact.schemaVersion -ne 22 -or !(Test-KmcChunk4CoreScenario $Request.scenario)){throw 'Core scenario requires exact schema22 and a registered root.'}
    Assert-KmcMountedRuntimeConfiguration $Artifact.observations.phase3fActualConfiguration $true 'Chunk 4 core configuration'
    $required=@(Get-KmcChunk4CoreLeaves $Request.scenario)
    $failureOnly=@('phase3d-horse-tranche-cleanup','phase3d-horse-scenario-deadline','phase3d-horse-leaf-deadline','phase3d-horse-runtime-exception')
    $names=New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal);$pass=0;$fail=0
    foreach($row in $Artifact.rows){
        if($row.name -cnotin ($required+$failureOnly) -or !$names.Add([string]$row.name) -or $row.status -cnotin @('PASS','FAIL')){throw 'Invalid or duplicate core row.'}
        if($row.status -ceq 'FAIL'){$fail++;continue};$pass++
        $e=$row.evidence
        if($row.name -cin $failureOnly -or $e.level -cne 'NATIVE INTEGRATION' -or $e.caseId -cne $row.name){throw 'Core PASS lacks exact native identity.'}
        if($row.name.StartsWith('C4-LIFE-')){Assert-KmcChunk4LifeRow $e}
        elseif($row.name.StartsWith('C4-HORSE-')){Assert-KmcChunk4HorseRow $e}
        elseif($row.name -ceq 'C4-RANGED-native-mixed-range'){Assert-KmcChunk4NativeRangedRow $e}
        elseif($row.name.EndsWith('-heal')){Assert-KmcChunk4HealRow $e}
        elseif($row.name -ceq 'C4-TARGETING-area-both'){Assert-KmcChunk4AreaRow $e}
        else{Assert-KmcChunk4HostileRow $e}
    }
    if($Artifact.subscenarioPassCount -ne $pass -or $Artifact.subscenarioFailCount -ne $fail){throw 'Core result counts disagree with native rows.'}
    if($Artifact.status -ceq 'PASS' -and ($pass -ne $required.Count -or $fail -ne 0 -or @($Artifact.errors).Count -ne 0)){throw 'Core PASS omitted a required new native case.'}
    if($Status -ceq 'PASS' -and $Artifact.status -cne 'PASS'){throw 'Runtime PASS contains a failed core scenario.'}
    if($Artifact.status -ceq 'PASS'){
        $trace=$Artifact.observations.ordinaryAttackTrace
        if($trace.dropped -ne 0 -or @($trace.events|Where-Object {$_.PSObject.Properties['observationError']}).Count -ne 0){throw 'Core native command observations dropped or failed.'}
    }
}
function Assert-KmcChunk4InstantStop {
    param($Before,$After)
    foreach($actor in @('rider','mount')){foreach($cost in @('standard','move')){
        if($Before.$actor.$cost -ne $After.$actor.$cost){throw 'Native Stop refunded or added a genuine actor cost.'}
    }}
}
function Assert-KmcChunk4LifeRow {
    param($e)
    $subject=if($e.caseId -ceq 'C4-LIFE-mount-death-live-command'){'mount'}else{'rider'}
    $survivor=if($subject -ceq 'mount'){'rider'}else{'mount'}
    if($e.mode -cne 'TB' -or $e.damageDispatches -ne 1 -or $e.nativeDamage -le 0 -or
        $e.subject -cne $e.beforeDamage.$subject.id -or $e.survivor -cne $e.beforeDamage.$survivor.id -or $e.subject -ceq $e.survivor -or
        $e.beforeDamage.live.relationship -cne 'Mounted' -or $e.beforeDamage.pairCommand -ne $true -or
        $e.liveCommandBefore.started -ne $true -or $e.liveCommandBefore.finished -ne $false -or
        $e.finalLife.live.relationship -cne 'Unmounted' -or $e.finalLife.attachmentResidue -ne $false -or
        $e.finalLife.attachmentRestoreVerified -ne $true -or $null -ne $e.finalLife.privatePartner -or $null -ne $e.finalLife.identity -or
        $e.finalLife.pairCommand -ne $false -or $e.finalLife.pairIntent -ne $false -or $e.finalLife.pairMovement -ne $false -or
        $e.finalLife.$survivor.conscious -ne $true -or $e.finalLife.$survivor.inState -ne $true -or
        $e.finalLife.$survivor.enabledRenderers -lt 1 -or $e.finalLife.$survivor.damage -ne $e.beforeDamage.$survivor.damage){throw 'Native rider/mount life stimulus or independent cleanup is incomplete.'}
    if($e.incapacitation -ne ($e.caseId -ceq 'C4-LIFE-rider-incapacitation') -or $e.finalLife.$subject.conscious -ne $false -or
        $e.finalLife.$subject.dead -ne (!$e.incapacitation)){throw 'Actual native life result was absent, changed or resurrected.'}
    if(@($e.unrelatedTurns).Count -ne 2 -or $e.unrelatedTurns[0].actor -ceq $e.unrelatedTurns[1].actor){throw 'Life cleanup lacks two distinct unrelated native turns.'}
    for($i=0;$i -lt 2;$i++){if($e.unrelatedTurns[$i].actor -cne $e.unrelatedOrderBefore[$i] -or $e.unrelatedTurns[$i].actor -cin @($e.subject,$e.survivor)){throw 'Life cleanup changed unrelated native participation.'}}
    if(@($e.nativeLifeEvents.events|Where-Object {$_.kind -ceq 'native-life-state' -and $_.actor -ceq $e.subject}).Count -lt 1 -or
        $e.nativeRules.dropped -ne 0 -or @($e.nativeRules.events|Where-Object {$_.kind -ceq 'damage-after' -and $_.target -ceq $e.subject -and $_.damage -gt 0}).Count -ne 1){throw 'Life PASS lacks actual native damage and life callbacks.'}
    foreach($actor in @('rider','mount')){if(@($e.finalLife.live.$actor.raw|Where-Object {$null -ne $_}).Count -ne 0 -or @($e.finalLife.live.$actor.queue).Count -ne 0){throw 'Native life cleanup retained a pair command.'}}
}
function Assert-KmcChunk4SpellCost {
    param($Trace,[string]$Caster,[string]$Blueprint)
    $before=@($Trace|Where-Object {$_.actor -ceq $Caster -and $_.spellBlueprint -ceq $Blueprint -and $_.boundary -ceq 'cost-before'})
    $after=@($Trace|Where-Object {$_.actor -ceq $Caster -and $_.spellBlueprint -ceq $Blueprint -and $_.boundary -ceq 'cost-after'})
    if($before.Count -ne 1 -or $after.Count -ne 1 -or $before[0].command -ne $after[0].command -or $before[0].frame -ne $after[0].frame -or
        $after[0].standard -le $before[0].standard){throw 'Native spell lacks one actual actor cost callback.'}
}
function Assert-KmcChunk4HealRow {
    param($e)
    if($e.mode -cne 'RT' -or $e.healClick.before.pair.relationship -cne 'Mounted' -or $e.subject -ceq $e.other -or $e.caster -cin @($e.subject,$e.other) -or
        $e.healBlueprint -cne '5590652e1c2225c4ca30c4a699ab3649' -or $e.nativeWound -le 0 -or $e.pauseDuration -lt .35 -or
        $e.healSlotInitiallyAvailable -ne $true -or $e.afterHeal.healSlotAvailable -ne $false -or
        $e.healClick.clicked -ne $true -or $e.healClick.queryPure -ne $true -or $e.healClick.canTarget -ne $true -or $e.healClick.available -ne $true -or
        $e.healClick.resolvedActor -cne $e.subject -or @($e.healRules).Count -ne 1 -or $e.healRules[0].actor -cne $e.caster -or
        $e.healRules[0].target -cne $e.subject -or $e.healRules[0].value -le 0 -or $e.afterHeal.subjectDamage -ge $e.afterWound.subjectDamage -or
        $e.afterHeal.otherDamage -ne $e.afterWound.otherDamage){throw 'Native heal is not independently targeted, pure while paused, spent and delivered once.'}
    foreach($key in @('casterStandard','casterMove','subjectDamage','otherDamage','healSlotAvailable')){if($e.healClick.before.$key -ne $e.healClick.after.$key){throw 'Paused spell input spent or applied a native effect.'}}
    foreach($key in @('pair','casterStandard','casterMove','casterPosition','subjectDamage','otherDamage','healSlotAvailable')){
        if((ConvertTo-Json -InputObject $e.pausedBegin.$key -Depth 20 -Compress) -cne
            (ConvertTo-Json -InputObject $e.pausedEnd.$key -Depth 20 -Compress)){throw 'Queued spell changed cost, position, slots or effects while paused.'}
    }
    Assert-KmcChunk4SpellCost $e.nativeTrace $e.caster $e.healBlueprint
}
function Assert-KmcChunk4AreaRow {
    param($e)
    $rider=$e.state.pair.rider.id;$mount=$e.state.pair.mount.id
    if($e.mode -cne 'RT' -or $e.state.pair.relationship -cne 'Mounted' -or [string]::IsNullOrEmpty($e.entity) -or [string]::IsNullOrEmpty($e.blueprint) -or
        $rider -ceq $mount -or $e.caster -cin @($rider,$mount) -or $e.state.areaSlotAvailable -ne $false -or
        $rider -cnotin $e.unitsInside -or $mount -cnotin $e.unitsInside -or @($e.firstSaves).Count -ne 2){throw 'One native area did not include both independent actors.'}
    foreach($actor in @($rider,$mount)){
        $save=@($e.firstSaves|Where-Object {$_.actor -ceq $actor})
        if($save.Count -ne 1 -or $save[0].type -cne 'Reflex' -or $save[0].dc -le 0 -or $save[0].passed -isnot [bool] -or
            !(Test-KmcExactJsonInteger $save[0].stat) -or !(Test-KmcExactJsonInteger $save[0].roll) -or
            @($e.allSaves|Where-Object {$_.actor -ceq $actor -and $_.gameTicks -eq $save[0].gameTicks}).Count -ne 1){throw 'Native area duplicated or omitted an actor defense/save.'}
    }
    Assert-KmcChunk4SpellCost $e.nativeTrace $e.caster '0fd00984a2c0e0a429cf1a911b4ec5ca'
}
function Assert-KmcChunk4HostileRow {
    param($e)
    $cmd=$e.hostileCommand
    $attacks=@($e.nativeRules.events|Where-Object {$_.kind -ceq 'attack-before' -and $_.actor -ceq $cmd.executor -and $_.target -ceq $e.subject})
    $rolls=@($e.nativeRules.events|Where-Object {$_.kind -ceq 'attack-roll' -and $_.actor -ceq $cmd.executor -and $_.target -ceq $e.subject})
    if($e.mode -cne 'RT' -or $e.beforeHostile.pair.relationship -cne 'Mounted' -or $e.nativeRules.dropped -ne 0 -or $cmd.type -cne 'Kingmaker.UnitLogic.Commands.UnitAttack' -or
        $cmd.executor -cin @($e.subject,$e.other,$e.caster) -or $cmd.started -ne $true -or $cmd.acted -ne $true -or $cmd.finished -ne $true -or
        $attacks.Count -lt 1 -or $rolls.Count -ne $attacks.Count -or @($rolls|Where-Object {$_.nativeAC -le 0}).Count -ne 0 -or
        @($e.nativeRules.attacks|Where-Object {$_.resolved -ne $true}).Count -ne 0){throw 'Incoming native attack/defense/delivery was not independently observed.'}
}
function Assert-KmcChunk4NativeRangedRow {
    param($e)
    if($e.mode -cne 'RT' -or $e.before.relationship -cne 'Unmounted' -or $e.command.type -cne 'Kingmaker.UnitLogic.Commands.UnitAttack' -or
        $e.command.result -cne 'Interrupt' -or $e.command.acted -ne $true -or $e.completed -lt 1 -or $e.completed -ge $e.planned -or
        @($e.nativePlan).Count -ne $e.planned -or $e.rules.riderResolved -ne $e.completed -or $e.rules.pairForcedD20 -ne 0 -or
        $e.rules.mountNonOpportunityAttackRules -ne 0 -or $e.nativeRangeRejection.rangeOriginDistance -le $e.nativeRangeRejection.approachRadius){throw 'Native mixed-range control did not retain the exact unmounted terminal and delivered prefix.'}
    for($i=0;$i -lt $e.planned;$i++){if($e.nativePlan[$i].ranged -ne ($i -lt $e.completed)){throw 'Native mixed-range control changed the weapon plan.'}}
    Assert-KmcChunk4InstantStop $e.beforeStop $e.afterStopInput
}
function Assert-KmcChunk4HorseRow {
    param($e)
    $mounted=$e.caseId -ceq 'C4-HORSE-mounted-three-primaries';$count=if($mounted){3}else{1}
    if($e.mode -cne 'RT' -or @($e.routines).Count -ne $count){throw 'Horse comparison lacks its native routines.'}
    foreach($routine in $e.routines){
        if($routine.planned -lt 1 -or $routine.completed -ne $routine.planned -or $routine.resolved -ne $routine.completed -or
            $routine.command.finished -ne $true -or $routine.command.acted -ne $true -or
            ($routine.command.result -cne 'Success' -and $null -eq $routine.nativeRecovery) -or $routine.rulesAfter.pairForcedD20 -ne 0 -or
            ($mounted -and ($routine.completed -ne 1 -or $routine.after.rider.standard -ne 0 -or $routine.after.rider.move -ne 0))){throw 'Horse native strike/recovery or independent costs failed.'}
        Assert-KmcChunk4InstantStop $routine.beforeStop $routine.afterStopInput
    }
}
