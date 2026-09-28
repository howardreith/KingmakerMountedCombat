# Independent replay of exact native movement observations.
# Pinned native TurnController.TickMovement 06000C37, normal TB approach only.
Set-StrictMode -Version Latest
function Assert-KmcNativeApproachTick($Before,$After,$Identity,[long]$Turn) {
    if($Before.boundary -cne 'approach-movement-before' -or $After.boundary -cne 'approach-movement-after' -or
       $After.sequence -ne $Before.sequence+1 -or $Before.frame -ne $After.frame -or $Before.gameTicks -ne $After.gameTicks) {throw 'Movement callback pair is not exact.'}
    foreach($event in @($Before,$After)) {
        if($event.command -ne $Identity.commandObject -or $event.commandActor -cne $Identity.casterId -or
           $event.state.actor -cne $Identity.casterId -or $event.currentActor -cne $Identity.casterId -or
           $event.actionType -cne 'Move' -or $event.commandType -cne 'Kingmaker.UnitLogic.Commands.UnitUseAbility' -or
           $event.turn -ne $Turn -or $event.movementTurn -ne $Turn -or $event.turnStatus -cne 'Acting' -or
           $event.nativeTurnBased -ne $true -or $event.nativePassing -ne $false -or
           $event.started -ne $false -or $event.acted -ne $false -or $event.finished -ne $false -or
           $event.slotCommand -ne $Identity.commandObject) {throw 'Movement belongs to another command or turn boundary.'}
        if($event.force -ne $false -or $event.fiveFootStep -ne $false) {throw 'Normal approach contract does not declare forced or five-foot movement.'}
        foreach($value in @($event.delta,$event.state.move,$event.state.timeMoved,$event.state.timeForced,$event.state.timeStepped)) {
            if($null -eq $value -or [double]::IsNaN([double]$value) -or [double]::IsInfinity([double]$value) -or [double]$value -lt 0) {throw 'Movement measurement is not finite and nonnegative.'}
        }
    }
    if([double]$After.delta -gt [double]$Before.delta) {throw 'Accepted native delta exceeds its input.'}
    $expectedMove=[single]([single]$Before.state.move + [single]$After.delta)
    $expectedTime=[single]([single]$Before.state.timeMoved + [single]$After.delta)
    if([Math]::Abs([double]$After.state.move-$expectedMove) -gt 0.0001 -or
       [Math]::Abs([double]$After.state.timeMoved-$expectedTime) -gt 0.0001) {throw 'Native movement resource/time delta differs.'}
    foreach($field in @('standard','swift','reactions','reactionCooldown','initiativeCooldown','initiativeOrder','reactionsPerRound','timeForced','timeStepped')) {
        if($null -eq $Before.state.$field -or $null -eq $After.state.$field -or [double]::IsNaN([double]$Before.state.$field) -or [double]::IsInfinity([double]$Before.state.$field) -or $Before.state.$field -ne $After.state.$field) {throw "Movement wrote an unrelated field: $field"}
    }
}
function Assert-KmcNativeApproachWindow($Events,$Identity,[long]$Turn,[double]$BaselineMove,[double]$CostBeforeMove,[bool]$RequireApproach,[object]$BaselineTime=$null) {
    $events=@($Events)
    if($events.Count%2 -ne 0 -or ($RequireApproach -and $events.Count -eq 0)) {throw 'Exact native approach movement observation is missing.'}
    $expected=$BaselineMove;$lastSequence=0;$time=$BaselineTime;$total=0.0
    for($index=0;$index -lt $events.Count;$index+=2) {
        $before=$events[$index];$after=$events[$index+1]
        Assert-KmcNativeApproachTick $before $after $Identity $Turn
        if($before.sequence -le $lastSequence -or [Math]::Abs([double]$before.state.move-$expected) -gt 0.0001 -or
           ($null -ne $time -and [Math]::Abs([double]$before.state.timeMoved-[double]$time) -gt 0.0001)) {throw 'Movement trace has an unexplained pre-acted resource or time gap.'}
        $expected=[double]$after.state.move;$time=[double]$after.state.timeMoved;$lastSequence=$after.sequence;$total+=[double]$after.delta
    }
    if($RequireApproach -and $total -le 0){throw 'Exact native approach movement observation has no accepted time.'}
    if([Math]::Abs($CostBeforeMove-$expected) -gt 0.0001) {throw 'Pre-acted Move endpoint lacks exact native movement events.'}
}
function Assert-KmcNativeMovementEvidence($Events,$PreClick,$CostBefore,$Identity,[string]$Ability,[bool]$InCombat,[bool]$TurnBased,[bool]$Required,$Hooks) {
    $events=@($Events|Where-Object boundary -CLike 'approach-movement-*')
    $allowed=$InCombat -and $TurnBased -and $Ability -ceq 'f053faad986631688defa003cd7bda0e'
    if(-not $allowed) {if($events.Count -ne 0){throw 'Native movement is undeclared in this window.'};return}
    $matching=@($Hooks|Where-Object {$_.method -ceq 'TurnBased.Controllers.TurnController.TickMovement' -and $_.token -ceq '06000C37' -and $_.moduleMvid -ceq '07fa1e4d-8618-41b3-9b8d-faa17d3b26f7' -and $_.prefix -ceq 'ApproachMovementBefore' -and $_.postfix -ceq 'ApproachMovementAfter'})
    if($matching.Count -ne 1){throw 'Exact native movement hook is missing.'}
    foreach($field in @('move','timeMoved','timeForced','timeStepped')) {
        $value=$PreClick.state.rider.$field
        if($null -eq $value -or [double]::IsNaN([double]$value) -or [double]::IsInfinity([double]$value) -or [double]$value -lt 0){throw 'Native movement baseline is invalid.'}
    }
    if($PreClick.state.rider.nativeTurnObject -eq 0 -or $PreClick.state.rider.nativeTurnObject -ne $CostBefore.turn){throw 'Baseline belongs to another turn.'}
    $previousTicks=[long]$PreClick.gameTicks
    foreach($event in $events) {
        if($event.sequence -le $PreClick.allocationSequence -or $event.sequence -ge $CostBefore.sequence -or
            $event.gameTicks -lt $previousTicks -or $event.gameTicks -gt $CostBefore.gameTicks){throw 'Movement left the exact pre-acted window.'}
        $previousTicks=[long]$event.gameTicks
        foreach($field in @('timeForced','timeStepped')) {
            if([Math]::Abs([double]$event.state.$field-[double]$PreClick.state.rider.$field) -gt 0.0001){throw 'Movement has undeclared mode debt.'}
        }
    }
    Assert-KmcNativeApproachWindow $events $Identity $CostBefore.turn $PreClick.state.rider.move $CostBefore.state.move $Required $PreClick.state.rider.timeMoved
}
