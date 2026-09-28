# Exact synchronous path ownership for the positive TB Mount; no native path is modified.
Set-StrictMode -Version Latest
function Assert-KmcNativeMountApproachPath($PathProof,$CommandProof) {
    $p=$PathProof;$id=$CommandProof.identity
    if($null -eq $p -or $p.contract -cne 'exact-native-mount-preview-and-consumed-path-observation' -or $p.complete -ne $true -or @($p.errors).Count -ne 0){throw 'Exact native Mount path observation is incomplete.'}
    if($p.commandObject -ne $id.commandObject -or $p.casterId -cne $id.casterId -or $p.targetId -cne $id.targetId){throw 'Native path belongs to another command or actor.'}
    if($p.actorViewObject -eq 0 -or $p.movementAgentObject -eq 0){throw 'Native path omitted the exact live view or movement agent.'}
    $hooks=@(@('060027A6','Kingmaker.UnitLogic.Commands.Base.UnitCommand.TickApproaching','ApproachBefore','ApproachAfter'),@('0600700F','Kingmaker.TurnBasedMode.PathVisualizer.CurrentPathForUnit',$null,'PreviewAfter'),@('060018B6','Kingmaker.View.UnitMovementAgent.FollowPrecomputedPath','PrecomputedBefore','PrecomputedAfter'),@('060018A3','Kingmaker.View.UnitMovementAgent.PathTo','RequestedBefore',$null))
    if(@($p.observerHooks).Count -ne 4){throw 'Native path hook inventory differs.'}
    foreach($hook in $hooks){$found=@($p.observerHooks|Where-Object token -CEQ $hook[0]);if($found.Count -ne 1 -or $found[0].method -cne $hook[1] -or $found[0].prefix -cne $hook[2] -or $found[0].postfix -cne $hook[3] -or $found[0].moduleMvid -cne '07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'){throw 'Native path hook identity differs.'}}
    $events=@($p.events);$init=@($CommandProof.samples|Where-Object boundary -CEQ 'init')[0]
    if($events.Count -lt 5 -or $events[0].boundary -cne 'preview-before-click' -or $events[1].boundary -cne 'command-bound' -or $events[2].boundary -cne 'approach-before'){throw 'Native path observation omitted its pre-click, command or approach boundary.'}
    $previous=[long]$CommandProof.preClick.gameTicks
    for($i=0;$i -lt $events.Count;$i++){
        $e=$events[$i]
        if($e.sequence -ne $i+1 -or $e.gameTicks -lt $previous -or $e.casterId -cne $id.casterId -or $e.targetId -cne $id.targetId){throw 'Native path event order or actor differs.'};$previous=[long]$e.gameTicks
        $receiver=if($e.boundary -cin @('preview-before-click','preview-return')){$p.pathVisualizerObject}elseif($e.boundary -ceq 'command-bound'){0}elseif($e.boundary -cin @('approach-before','approach-after')){$id.commandObject}else{$p.movementAgentObject}
        if($e.receiverObject -ne $receiver){throw 'Native path callback used another receiver.'}
        if($i -eq 0){if($e.commandObject -ne 0 -or $e.gameTicks -gt $init.gameTicks -or $e.approachDepth -ne 0){throw 'Preview baseline followed command admission.'};continue}
        if($e.commandObject -ne $id.commandObject -or $e.moveSlotObject -ne $id.commandObject -or $e.commandType -cne 'Move' -or $e.abilityGuid -cne $id.abilityGuid -or $e.processObject -ne 0 -or $e.contextObject -ne 0 -or $e.started -ne $false -or $e.acted -ne $false -or $e.finished -ne $false -or $e.forcedPathObject -ne 0){throw 'Path consumption lost exact unacted native command ownership.'}
        if($e.turnObject -eq 0 -or $e.turnActor -cne $id.casterId -or $e.turnStatus -cne 'Acting' -or $e.turnObject -ne $events[1].turnObject -or $e.approachDepth -ne $(if($i -eq 1){0}else{1})){throw 'Path consumption belongs to another turn or callback.'}
        if($e.boundary -cnotin @('command-bound','approach-before','preview-return','precomputed-before','precomputed-after','approach-after','path-request-before')){throw 'Undeclared path boundary.'}
    }
    $returned=@($events|Where-Object boundary -CEQ 'preview-return');$before=@($events|Where-Object boundary -CEQ 'precomputed-before');$after=@($events|Where-Object boundary -CEQ 'precomputed-after');$requests=@($events|Where-Object boundary -CEQ 'path-request-before')
    function Test-NativePathPoints($Path){
        if($null -eq $Path -or $Path.pathObject -eq 0 -or $Path.pathError -ne $false -or @($Path.points).Count -eq 0){throw 'Consumed native path is empty or failed.'}
        $sum=0.0;$last=$null
        foreach($point in @($Path.points)){foreach($axis in @('x','y','z')){if($null -eq $point.$axis -or [double]::IsNaN([double]$point.$axis) -or [double]::IsInfinity([double]$point.$axis)){throw 'Consumed path point is not finite.'}}
            if($null -ne $last){$sum+=[Math]::Sqrt([Math]::Pow([double]$point.x-[double]$last.x,2)+[Math]::Pow([double]$point.z-[double]$last.z,2))};$last=$point
        }
        if($null -eq $Path.horizontalLength -or [double]::IsNaN([double]$Path.horizontalLength) -or [double]::IsInfinity([double]$Path.horizontalLength) -or [Math]::Abs([double]$Path.horizontalLength-$sum) -gt 0.0001){throw 'Consumed path length differs from its points.'}
    }
    if($before.Count -gt 0){
        if($returned.Count -ne 1 -or $before.Count -ne 1 -or $after.Count -ne 1 -or $requests.Count -ne 0){throw 'Precomputed route lacks one exact preview/consumption pair.'}
        $b=$before[0];$a=$after[0];$r=$returned[0];$ended=@($events|Where-Object boundary -CEQ 'approach-after')
        if($ended.Count -ne 1 -or $r.sequence+1 -ne $b.sequence -or $b.sequence+1 -ne $a.sequence -or $a.sequence+1 -ne $ended[0].sequence -or $b.frame -ne $r.frame -or $a.frame -ne $b.frame -or $r.gameTicks -ne $b.gameTicks -or $b.gameTicks -ne $a.gameTicks){throw 'Native path callbacks are not synchronous and ordered.'}
        foreach($e in @($r,$b,$a)){Test-NativePathPoints $e.path}
        if(($r.path.points|ConvertTo-Json -Depth 20 -Compress) -cne ($b.path.points|ConvertTo-Json -Depth 20 -Compress) -or $b.path.pathObject -ne $a.path.pathObject -or $a.agentPathObject -ne $a.path.pathObject){throw 'Agent consumed a different precomputed path.'}
        $end=@($b.path.points)[-1];$distance=[Math]::Sqrt([Math]::Pow([double]$end.x-[double]$b.destination.x,2)+[Math]::Pow([double]$end.z-[double]$b.destination.z,2))
        if($null -eq $b.approachRadius -or [double]$b.approachRadius -le 0 -or $distance -gt [double]$b.approachRadius+0.0001){throw 'Consumed preview path does not reach this exact Mount approach point.'}
    } else {
        if($after.Count -ne 0 -or $requests.Count -lt 1 -or $returned.Count -ne $requests.Count -or @($returned|Where-Object {$_.path.pathObject -ne 0}).Count -ne 0){throw 'Native direct route is not backed by a null preview and actual PathTo.'}
        foreach($e in $requests){
            $r=@($returned|Where-Object sequence -EQ ($e.sequence-1))
            if($r.Count -ne 1 -or $r[0].frame -ne $e.frame -or $r[0].gameTicks -ne $e.gameTicks){throw 'Direct path request lacks its synchronous null preview callback.'}
            if(($e.destination|ConvertTo-Json -Compress) -cne ($e.targetPosition|ConvertTo-Json -Compress) -or $null -ne $e.path){throw 'Direct path request targeted another point or read worker-owned contents.'}}
    }
}
