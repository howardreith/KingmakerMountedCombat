# Re-derive the optional fixture's evidence before accepting any new TB command window.
function Assert-KmcChunk6aPreCombatPositioning($Setup,$Proof) {
    if($null -eq $Setup -or $Setup.contract -cne 'native-ground-positioning-before-fresh-encounter' -or
        $Setup.pass -ne $true -or $Setup.traceComplete -ne $true -or $Setup.beforeInCombat -ne $false -or $Setup.afterInCombat -ne $false) {
        throw 'TB positioning omitted its complete outside-combat transaction.'
    }
    foreach($value in @($Setup.minimumTravel,$Setup.maximumTravel,$Setup.clearanceRadius,$Setup.residual,$Setup.frameBefore,$Setup.frameAfter,$Setup.gameTicksBefore,$Setup.gameTicksAfter)) {
        if($null -eq $value -or [double]::IsNaN([double]$value) -or [double]::IsInfinity([double]$value)){throw 'TB positioning omitted finite bounds.'}
    }
    if($Setup.minimumTravel -ne 0.25 -or $Setup.maximumTravel -ne 4 -or $Setup.residual -lt 0 -or $Setup.residual -gt 0.06 -or
        $Setup.frameAfter -le $Setup.frameBefore -or $Setup.gameTicksAfter -lt $Setup.gameTicksBefore){throw 'TB positioning bounds or time changed.'}
    $rider=$Proof.identity.casterId;$before=$Setup.before;$after=$Setup.after;$a=$Setup.admittedCommand;$t=$Setup.terminalCommand
    if($a.id -eq 0 -or $a.id -ne $t.id -or $a.executor -cne $rider -or $t.executor -cne $rider -or $Setup.createdByPlayer -ne $true -or
        $a.type -cne 'Kingmaker.UnitLogic.Commands.UnitMoveTo' -or $t.type -cne $a.type -or $t.finished -ne $true -or $t.result -cne 'Success') {
        throw 'TB positioning lost its exact successful native ground command.'
    }
    foreach($state in @($before,$after)) {
        if($state.rider.actor -cne $rider -or $state.mount.actor -cne $Proof.mountId -or $state.relationshipState -cne 'Unmounted' -or
            $state.generation -ne $Proof.identity.generationAtInit -or @($state.selectedIds).Count -ne 1 -or $state.selectedIds[0] -cne $rider) {
            throw 'TB positioning changed actor, selection or relationship identity.'
        }
    }
    if(($before.ledger|ConvertTo-Json -Depth 20 -Compress) -cne ($after.ledger|ConvertTo-Json -Depth 20 -Compress)){throw 'TB positioning changed transition ledger.'}
    $candidates=@($Setup.candidates);$eligible=@($candidates|Where-Object eligible -EQ $true)
    if($candidates.Count -lt 1 -or $candidates.Count -gt 72 -or $eligible.Count -ne 1){throw 'TB positioning search is missing or unbounded.'}
    $c=$eligible[0]
    foreach($v in @($c.requestedSeparation,$c.separation,$c.travel,$c.routeResidual)) {
        if($null -eq $v -or [double]::IsNaN([double]$v) -or [double]::IsInfinity([double]$v)){throw 'TB positioning candidate omitted finite geometry.'}
    }
    # The v2 ground plan proves a candidate whose forward native route query ends at the origin through its
    # reciprocal origin boundary (the origin inside the navmesh and walkable, the forward route end and the
    # reverse route from the point both at the origin); the producer and the shared plan reader measure that
    # candidate by its reverse residual (frozen 205 allocation-rider-first-tb: forward residual 1.83 = travel,
    # reverse residual 0, the native move delivered to the destination). This rule measures the same residual.
    $measuredRoute=[double]$c.routeResidual
    if($null -ne $Setup.PSObject.Properties['plan'] -and $Setup.plan.contract -ceq 'bounded-native-ground-plan-v2' -and
        $null -ne $c.PSObject.Properties['routeProof'] -and $c.routeProof -ceq 'reciprocal-origin-boundary' -and
        $null -ne $c.PSObject.Properties['routeEnd'] -and $null -ne $c.PSObject.Properties['reverseRouteEnd']) {
        $horizontal={param($p,$q) $a=if($p -is [array]){@([double]$p[0],[double]$p[2])}else{@([double]$p.x,[double]$p.z)};$b=if($q -is [array]){@([double]$q[0],[double]$q[2])}else{@([double]$q.x,[double]$q.z)};[Math]::Sqrt([Math]::Pow($a[0]-$b[0],2)+[Math]::Pow($a[1]-$b[1],2))}
        $planOrigin=$Setup.plan.origin
        $clipped=& $horizontal $c.routeEnd $planOrigin;$reverse=& $horizontal $c.reverseRouteEnd $planOrigin
        $nearestOrigin=& $horizontal $Setup.plan.originNativeNearest.clamped $planOrigin
        if($Setup.plan.originInsideNavmesh -eq $true -and $Setup.plan.originNativeNearest.walkable -eq $true -and
            $nearestOrigin -lt 0.001 -and $clipped -lt 0.001 -and $reverse -lt 0.001){$measuredRoute=$reverse}
    }
    if($c.walkable -ne $true -or $c.requestedSeparation -lt 2 -or $c.requestedSeparation -gt 2.65 -or
        [Math]::Abs($c.separation-$c.requestedSeparation) -gt 0.45 -or $c.travel -lt 0.25 -or $c.travel -gt 4 -or
        $c.routeResidual -lt 0 -or $measuredRoute -lt 0 -or $measuredRoute -ge 0.001 -or @($c.blockers).Count -ne 0){throw 'TB positioning candidate violates unchanged route and clearance bounds.'}
    $footprint=$c.footprint
    if($footprint.corpulence -lt 0 -or [Math]::Abs($Setup.clearanceRadius-([Math]::Max(0.5,$footprint.corpulence)+0.75)) -gt 0.00001 -or
        $footprint.probeRadius -ne $Setup.clearanceRadius -or @($footprint.probes).Count -ne 8){throw 'TB positioning clearance footprint changed.'}
    foreach($probe in $footprint.probes) {
        if($null -eq $probe.residual -or [double]::IsNaN($probe.residual) -or [double]::IsInfinity($probe.residual) -or
            $probe.residual -lt 0 -or $probe.residual -ge 0.001){throw 'TB positioning endpoint footprint is clipped.'}
    }
    foreach($field in @('x','y','z')) {if($c.point.$field -ne $Setup.destination.$field){throw 'TB positioning consumed another destination.'}}
    if($null -eq $Setup.originNavigation.footprint -or $null -eq $Setup.terminalNavigation.footprint -or
        $null -eq $Setup.originNavigation.actingClearance -or $null -eq $Setup.terminalNavigation.actingClearance){throw 'TB positioning omitted origin or terminal navigation.'}
    Assert-KmcChunk6aPathEvidence $Setup.path $a.id $rider $false
    $events=@($Setup.events)
    if(@($events|Where-Object {$_.boundary -ceq 'admission-after' -and $_.command -eq $a.id -and $_.commandActor -ceq $rider}).Count -ne 1){throw 'TB positioning native admission callback missing.'}
    if(@($events|Where-Object {$_.boundary -cmatch '^(prepare-|clear-|combat-clear-|turn-end-)' }).Count -ne 0){throw 'TB positioning entered native allocation or reset.'}
}
