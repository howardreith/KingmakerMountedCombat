function Test-KmcChunk4CoreScenario {
    param([string]$Scenario)
    return $Scenario -cin @('chunk4-rider-incapacitation-tb','chunk4-rider-death-tb','chunk4-mount-death-tb',
        'chunk4-targeting-area-unmounted-rt','chunk4-targeting-rider-rt','chunk4-targeting-mount-rt','chunk4-horse-strike-comparison-rt','chunk4-ranged-native-control-rt')
}
function Get-KmcChunk4CoreLeaves {
    param([string]$Scenario)
    switch -CaseSensitive ($Scenario) {
        'chunk4-rider-incapacitation-tb' {return @('C4-LIFE-rider-incapacitation')}
        'chunk4-rider-death-tb' {return @('C4-LIFE-rider-death-live-command')}
        'chunk4-mount-death-tb' {return @('C4-LIFE-mount-death-live-command')}
        'chunk4-targeting-rider-rt' {return @('C4-TARGETING-rider-heal','C4-TARGETING-area-both','C4-TARGETING-rider-hostile')}
        'chunk4-targeting-area-unmounted-rt' {return @('C4-TARGETING-area-unmounted')}
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
        elseif($row.name -cin @('C4-TARGETING-area-both','C4-TARGETING-area-unmounted')){Assert-KmcChunk4AreaRow $e}
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
        $e.finalLife.attachmentRestoreVerified -ne $true -or $null -ne $e.finalLife.privatePartner -or
        $e.finalLife.pairCommand -ne $false -or $e.finalLife.pairIntent -ne $false -or $e.finalLife.pairMovement -ne $false -or
        $e.finalLife.$survivor.conscious -ne $true -or $e.finalLife.$survivor.inState -ne $true -or
        $e.finalLife.$survivor.enabledRenderers -lt 1 -or $e.finalLife.$survivor.damage -ne $e.beforeDamage.$survivor.damage){throw 'Native rider/mount life stimulus or independent cleanup is incomplete.'}
    if($e.incapacitation -ne ($e.caseId -ceq 'C4-LIFE-rider-incapacitation') -or $e.finalLife.$subject.conscious -ne $false -or
        $e.finalLife.$subject.dead -ne (!$e.incapacitation)){throw 'Actual native life result was absent, changed or resurrected.'}
    $roster=@($e.eligibleRosterBeforeDamage);$principal=$e.principalRosterIndex
    $ids=New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    foreach($id in $roster){if([string]::IsNullOrWhiteSpace($id) -or !$ids.Add([string]$id)){throw 'Life fixture roster is empty or duplicated.'}}
    if($roster.Count -lt 4 -or !(Test-KmcExactJsonInteger $principal) -or $principal -lt 0 -or $principal -ge $roster.Count -or
        $roster[$principal] -cne $e.beforeDamage.currentActor -or $e.beforeDamage.currentActor -cne $e.beforeDamage.rider.id -or
        !$ids.Contains([string]$e.subject) -or !$ids.Contains([string]$e.survivor)){throw 'Life fixture lacks its exact native roster and rider principal.'}
    $expected=@(for($offset=1;$offset -lt $roster.Count;$offset++){
        $actor=$roster[($principal+$offset)%$roster.Count]
        if($actor -cnotin @($e.subject,$e.survivor)){$actor}
    })
    if((ConvertTo-Json -InputObject $expected -Compress) -cne (ConvertTo-Json -InputObject @($e.unrelatedOrderBefore) -Compress)){
        throw 'Life fixture expected order does not follow the current actor through the native cyclic roster.'
    }
    if(@($e.unrelatedTurns).Count -ne 2 -or $e.unrelatedTurns[0].actor -ceq $e.unrelatedTurns[1].actor){throw 'Life cleanup lacks two distinct unrelated native turns.'}
    for($i=0;$i -lt 2;$i++){if($e.unrelatedTurns[$i].actor -cne $e.unrelatedOrderBefore[$i] -or $e.unrelatedTurns[$i].actor -cin @($e.subject,$e.survivor)){throw 'Life cleanup changed unrelated native participation.'}}
    if(@($e.nativeLifeEvents.events|Where-Object {$_.kind -ceq 'native-life-state' -and $_.actor -ceq $e.subject -and
        $_.lifeState -ceq $(if($e.incapacitation){'Unconscious'}else{'Dead'}) -and $_.frame -le $e.unrelatedTurns[0].frame}).Count -ne 1 -or
        $e.nativeRules.dropped -ne 0 -or @($e.nativeRules.events|Where-Object {$_.kind -ceq 'damage-after' -and $_.target -ceq $e.subject -and $_.damage -gt 0}).Count -ne 1){throw 'Life PASS lacks actual native damage and life callbacks.'}
    foreach($actor in @('rider','mount')){if(@($e.finalLife.live.$actor.raw|Where-Object {$null -ne $_}).Count -ne 0 -or @($e.finalLife.live.$actor.queue).Count -ne 0){throw 'Native life cleanup retained a pair command.'}}
    foreach($life in @($e.afterCleanup,$e.finalLife)) {
        if($null -eq $life -or $life.split -isnot [bool] -or $life.finalized -isnot [bool] -or
            $life.riderEnded -isnot [bool] -or $life.mountEnded -isnot [bool] -or
            $life.live.relationship -cne 'Unmounted' -or $life.pairCommand -ne $false -or $null -ne $life.privatePartner -or
            ($null -ne $life.identity -and (!$life.split -or $life.identity -cne $e.beforeDamage.identity)) -or
            $life.riderGrants -ne $e.beforeDamage.riderGrants -or $life.mountGrants -ne $e.beforeDamage.mountGrants) {
            throw 'Life cleanup retained active/new pair ownership or issued another actor grant.'
        }
    }
    Assert-KmcChunk4DeathPolicy $e
    if($e.nativeRecoveryExpected -isnot [bool] -or $e.finalLife.$subject.finallyDead -isnot [bool] -or
        $e.nativeRecoveryExpected -eq $e.finalLife.$subject.finallyDead -or
        ($e.incapacitation -and $e.finalLife.$subject.finallyDead) -or
        ($e.caseId -ceq 'C4-LIFE-rider-death-live-command' -and !$e.finalLife.$subject.finallyDead)){throw 'Life result does not establish its exact native finality and recovery policy.'}
    $returns=@($e.nativeLifeEvents.events|Where-Object {$_.kind -ceq 'native-life-state' -and $_.actor -ceq $e.subject -and $_.lifeState -ceq 'Conscious'})
    if(!$e.nativeRecoveryExpected -and $returns.Count -ne 0){throw 'Permanent native rider death was resurrected.'}
    if($e.nativeRecoveryExpected){
        if($returns.Count -ne 1 -or $returns[0].frame -le $e.unrelatedTurns[1].frame -or
            $returns[0].frame -gt $e.nativeEncounterExit.frame){throw 'Native recovery is missing, duplicated or occurred before unrelated turns completed.'}
        foreach($method in @(
            @('0600918e','Tick','Kingmaker.Controllers.Units.UnitReturnToConsciousController'),
            @('06009191','MakeUnitConscious','Kingmaker.Controllers.Units.UnitReturnToConsciousController'),
            @('06009164','SetLifeState','Kingmaker.Controllers.Units.UnitLifeController'))){
            if(@($returns[0].nativeSource|Where-Object {$_.token -ceq $method[0] -and $_.method -ceq $method[1] -and $_.type -ceq $method[2] -and
                $_.assemblyMvid -ceq '07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'}).Count -ne 1){throw 'Life recovery lacks its exact native controller call source.'}
        }
    }
    foreach($exit in @($e.nativeEncounterExit,$e.afterPolicyRestore)){
    if($null -eq $exit -or $null -ne $exit.identity -or $null -ne $exit.privatePartner -or $exit.actorRecords -ne 0 -or
        $exit.playerCombat -ne $false -or $exit.riderCombat -ne $false -or $exit.mountCombat -ne $false -or
        $exit.tbActive -ne $false -or $exit.tbInitialized -ne $false -or $exit.live.relationship -cne 'Unmounted' -or
        $exit.attachmentResidue -ne $false -or $exit.attachmentRestoreVerified -ne $true -or $exit.pairCommand -ne $false -or
        $exit.pairIntent -ne $false -or $exit.pairMovement -ne $false -or $exit.$subject.conscious -ne $e.nativeRecoveryExpected -or
        $exit.$subject.dead -ne (!$e.nativeRecoveryExpected) -or $exit.$subject.finallyDead -ne (!$e.nativeRecoveryExpected) -or
        $exit.riderGrants -ne $e.beforeDamage.riderGrants -or $exit.mountGrants -ne $e.beforeDamage.mountGrants -or
        $exit.$survivor.conscious -ne $true -or $exit.$survivor.inState -ne $true -or $exit.$survivor.enabledRenderers -lt 1 -or
        $exit.$survivor.damage -ne $e.beforeDamage.$survivor.damage) {throw 'Native encounter exit did not retire the split accounting state or preserve the life result.'}
    foreach($actor in @('rider','mount')) {
        if(@($exit.live.$actor.raw|Where-Object {$null -ne $_}).Count -ne 0 -or @($exit.live.$actor.queue).Count -ne 0){throw 'Native encounter exit retained a pair command.'}
    }
    if($e.nativeRecoveryExpected){
        $expectedDamage=[Math]::Max(0,$exit.$subject.hp-[Math]::Max(1,$exit.$subject.characterLevel)-$exit.$subject.nonLethalDamage)
        if($exit.$subject.damage -ne $expectedDamage -or $returns[0].damage -ne $expectedDamage){throw 'Automatic recovery did not retain the native health result.'}
    }elseif($exit.$subject.damage -ne $e.finalLife.$subject.damage){throw 'Permanent native death damage was rewritten.'}
    }
    if($e.afterPolicyRestore.gameTicks-$e.nativeEncounterExit.gameTicks -lt 2500000 -or
        $e.afterPolicyRestore.frame -le $e.nativeEncounterExit.frame){throw 'Native death policy restoration lacks subsequent native simulation observation.'}
    if($e.enemyDamageDispatches -ne 1 -or $e.enemyNativeDamage -le 0 -or $e.enemyLifeTransitions -lt 1 -or
        $e.enemyBeforeDamage.id -cne $e.enemyAfterDeath.id -or $e.enemyBeforeDamage.conscious -ne $true -or $e.enemyAfterDeath.dead -ne $true -or
        [string]::IsNullOrWhiteSpace($e.enemyDamageSource) -or $e.enemyDamageSource -ceq $e.subject) {throw 'Life retirement lacks an actual labelled native enemy defeat.'}
}
function Assert-KmcChunk4DeathPolicy {
    param($e)
    $policy=$e.nativeDeathPolicy;$permanent=$e.caseId -ceq 'C4-LIFE-rider-death-live-command'
    if($policy.permanentDeathFixture -ne $permanent -or $policy.restoration.restored -ne $true -or
        (ConvertTo-Json $policy.before -Depth 8 -Compress) -cne (ConvertTo-Json $policy.restoration.state -Depth 8 -Compress)){
        throw 'Life fixture did not preserve and restore the actual native difficulty settings.'
    }
    foreach($state in @($policy.before,$policy.effective,$policy.restoration.state)){
        if($state.trueDeath -isnot [bool] -or $state.deathDoorCondition -isnot [bool] -or
            $state.riseAfterCombat.value -isnot [bool] -or $state.deathDoor.value -isnot [bool] -or
            $state.trueDeath -eq $state.riseAfterCombat.value -or $state.deathDoorCondition -ne $state.deathDoor.value -or
            $state.damageToParty -le 0 -or $state.damageToParty -ne $policy.before.damageToParty){throw 'Native death difficulty identities or unchanged damage multiplier are absent.'}
        foreach($setting in @('riseAfterCombat','deathDoor')){
            if($state.$setting.persisted -cne $policy.before.$setting.persisted -or
                ($null -ne $state.$setting.raw -and $state.$setting.raw -isnot [bool])){throw 'Life fixture changed persisted difficulty or lost cached-setting evidence.'}
        }
    }
    if($permanent){
        if($policy.effective.trueDeath -ne $true -or $policy.effective.deathDoorCondition -ne $false -or
            $policy.effective.riseAfterCombat.raw -ne $false -or $policy.effective.deathDoor.raw -ne $false){throw 'Persistent rider death did not select the native true-death policy.'}
    }elseif($policy.effective.trueDeath -ne $policy.before.trueDeath -or $policy.effective.deathDoorCondition -ne $policy.before.deathDoorCondition){
        throw 'Nonfinal life fixture changed the actual native death policy.'
    }
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
    $mounted=$e.caseId -ceq 'C4-TARGETING-area-both'
    $relationship=if($mounted){'Mounted'}else{'Unmounted'}
    if($e.caseId -cnotin @('C4-TARGETING-area-both','C4-TARGETING-area-unmounted') -or $e.mounted -ne $mounted -or
        $e.mode -cne 'RT' -or $e.state.pair.relationship -cne $relationship -or $e.before.pair.relationship -cne $relationship -or
        [string]::IsNullOrEmpty($e.entity) -or $e.blueprint -cne 'bcb6329cefc66da41b011299a43cc681' -or
        [string]::IsNullOrEmpty($rider) -or [string]::IsNullOrEmpty($mount) -or $rider -ceq $mount -or
        $e.caster -cin @($rider,$mount) -or $e.state.areaSlotAvailable -ne $false -or $e.before.areaSlotAvailable -ne $true -or
        $e.state.subjectDamage -ne $e.before.subjectDamage -or $e.state.otherDamage -ne $e.before.otherDamage -or
        @($e.unitsInside|Where-Object {$_ -ceq $rider}).Count -ne 1 -or @($e.unitsInside|Where-Object {$_ -ceq $mount}).Count -ne 1 -or
        @($e.firstSaves).Count -ne 2 -or @($e.allSaves).Count -lt 2 -or @($e.definition).Count -lt 1 -or $e.ruleDrops -ne 0){throw 'One native area did not independently include both actors with native slot expenditure.'}
    foreach($definition in $e.definition){
        if($definition.type -cne 'Kingmaker.UnitLogic.Abilities.Components.AreaEffects.AbilityAreaEffectRunAction' -or
            $null -eq $definition.unitEnter -or $null -eq $definition.round){throw 'Native area action metadata is missing.'}
    }
    $identities=New-Object 'Collections.Generic.HashSet[long]'
    $callbacks=New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    foreach($save in $e.allSaves){
        $source=$save.nativeSource
        if($save.kind -cne 'saving-throw' -or $save.actor -cnotin @($rider,$mount) -or $save.target -cne $save.actor -or
            !(Test-KmcExactJsonInteger $save.identity) -or !$identities.Add([long]$save.identity) -or
            !(Test-KmcExactJsonInteger $save.frame) -or !(Test-KmcExactJsonInteger $save.gameTicks) -or
            $save.type -cne 'Reflex' -or $save.dc -le 0 -or $save.passed -isnot [bool] -or
            !(Test-KmcExactJsonInteger $save.stat) -or !(Test-KmcExactJsonInteger $save.roll) -or
            $source.area -cne $e.entity -or $source.areaBlueprint -cne $e.blueprint -or $source.caster -cne $e.caster -or
            $source.actorInside -ne $true -or @($source.callbacks).Count -ne 1){throw 'Native saving throw lacks exact independent actor and area identity.'}
        $callback=$source.callbacks[0]
        $token=if($callback.kind -ceq 'unit-enter'){'06002ccd'}elseif($callback.kind -ceq 'round'){'06002cd0'}else{''}
        if($token -ceq '' -or $callback.token -cne $token -or $callback.assemblyMvid -cne '07fa1e4d-8618-41b3-9b8d-faa17d3b26f7' -or
            !$callbacks.Add($save.actor+':'+$token+':'+[string]$save.gameTicks)){throw 'Native area duplicated an actor callback or lacks its exact local callback contract.'}
    }
    foreach($actor in @($rider,$mount)){
        $entry=@($e.allSaves|Where-Object {$_.actor -ceq $actor -and $_.nativeSource.callbacks[0].kind -ceq 'unit-enter'})
        $first=@($e.firstSaves|Where-Object {$_.actor -ceq $actor})
        $stat=if($actor -ceq $rider){$e.before.riderReflex}else{$e.before.mountReflex}
        if($entry.Count -ne 1 -or $first.Count -ne 1 -or !(Test-KmcExactJsonInteger $stat) -or $first[0].stat -ne $stat -or
            (ConvertTo-Json $entry[0] -Depth 12 -Compress) -cne (ConvertTo-Json $first[0] -Depth 12 -Compress)){
            throw 'Native area omitted or duplicated an entry save, or used the wrong actor defense.'
        }
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
