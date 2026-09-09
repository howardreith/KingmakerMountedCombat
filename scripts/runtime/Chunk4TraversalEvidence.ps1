# Focused extensions to existing strict movement/boundary evidence contracts.
# These checks do not replace the native route, endpoint, cleanup or snapshot checks.
function Test-KmcChunk4TraversalScenario {
    param([string]$Scenario)
    return $Scenario -cin @('chunk4-traversal-core','chunk4-traversal-slope')
}
function Get-KmcChunk4TraversalRows {
    param([string]$Scenario)
    switch -CaseSensitive ($Scenario) {
        'chunk4-traversal-core' { return @('mounted-distance-door-interaction','mounted-pair-doorway','mounted-pair-turns-and-corners','mounted-pair-party-formation') }
        'chunk4-traversal-slope' { return @('mounted-pair-slope') }
        default { throw 'No exact Chunk 4 traversal root.' }
    }
}
function Assert-KmcChunk4PairedConfiguration {
    param($Value,[switch]$AllowIntake)
    Assert-KmcExactProperties $Value @('enablePairedActivation','enableUnifiedMountedTurn','enablePairedCommandScheduler','enableDiagnosticOverlay','overlayPresent') 'Chunk 4 measured configuration'
    foreach($kmcName in @('enablePairedActivation','enableUnifiedMountedTurn','enablePairedCommandScheduler','enableDiagnosticOverlay','overlayPresent')) {
        if($Value.$kmcName -isnot [bool]) { throw 'Chunk 4 configuration must contain actual JSON booleans.' }
        if($kmcName -ne 'enablePairedActivation' -and $Value.$kmcName) { throw 'Chunk 4 incompatible authority or overlay is active.' }
    }
    if(!$AllowIntake -and !$Value.enablePairedActivation) { throw 'Chunk 4 native measurement did not use paired activation.' }
}
function Assert-KmcChunk4NativeSlope {
    param($Value)
    Assert-KmcExactProperties $Value @('startY','riderMoveBefore','minimumY','maximumY','heightChange','dropped','samples') 'Chunk 4 native slope'
    foreach($kmcName in @('startY','riderMoveBefore','minimumY','maximumY','heightChange')) {
        if(!(Test-KmcJsonNumber $Value.$kmcName) -or [double]::IsNaN([double]$Value.$kmcName) -or [double]::IsInfinity([double]$Value.$kmcName)) { throw 'Slope values must be finite JSON numbers.' }
    }
    if(!(Test-KmcExactJsonInteger $Value.dropped) -or $Value.dropped -ne 0 -or $Value.samples -isnot [array] -or
        $Value.samples.Count -lt 3 -or $Value.samples.Count -gt 512 -or $Value.riderMoveBefore -lt 0) { throw 'Slope needs bounded native samples with no drops.' }
    $kmcMin=[double]$Value.startY; $kmcMax=$kmcMin; $kmcFrame=-1L; $kmcRadius=$null
    foreach($kmcSample in $Value.samples) {
        Assert-KmcExactProperties $kmcSample @('frame','position','stockAgentEnabled','avoidanceDisabled','corpulence','riderMove') 'Chunk 4 native slope sample'
        if(!(Test-KmcExactJsonInteger $kmcSample.frame) -or $kmcSample.frame -le $kmcFrame -or
            $kmcSample.position -isnot [array] -or $kmcSample.position.Count -ne 3 -or
            $kmcSample.stockAgentEnabled -isnot [bool] -or !$kmcSample.stockAgentEnabled -or
            $kmcSample.avoidanceDisabled -isnot [bool] -or $kmcSample.avoidanceDisabled) { throw 'Slope requires distinct native frames and stock movement/avoidance.' }
        foreach($kmcNumber in @($kmcSample.position)+@($kmcSample.corpulence,$kmcSample.riderMove)) {
            if(!(Test-KmcJsonNumber $kmcNumber) -or [double]::IsNaN([double]$kmcNumber) -or [double]::IsInfinity([double]$kmcNumber)) { throw 'Slope sample values must be finite JSON numbers.' }
        }
        if($kmcSample.corpulence -le 0 -or $kmcSample.riderMove -lt 0 -or
            $kmcSample.riderMove -gt [double]$Value.riderMoveBefore + 0.0001) { throw 'Slope changed the native footprint or charged carried rider movement.' }
        if($null -eq $kmcRadius) { $kmcRadius=[double]$kmcSample.corpulence }
        elseif([Math]::Abs([double]$kmcSample.corpulence-$kmcRadius) -gt 0.000001) { throw 'Slope changed the mount footprint during traversal.' }
        $kmcFrame=[long]$kmcSample.frame
        $kmcMin=[Math]::Min($kmcMin,[double]$kmcSample.position[1]); $kmcMax=[Math]::Max($kmcMax,[double]$kmcSample.position[1])
    }
    if($kmcMax-$kmcMin -lt 0.5 -or [Math]::Abs([double]$Value.minimumY-$kmcMin) -gt 0.000001 -or
        [Math]::Abs([double]$Value.maximumY-$kmcMax) -gt 0.000001 -or
        [Math]::Abs([double]$Value.heightChange-($kmcMax-$kmcMin)) -gt 0.000001) { throw 'Slope extent must reconcile to at least half a metre of actual native motion.' }
}
function Assert-KmcChunk4BlockedDoor {
    param($Value)
    Assert-KmcExactProperties $Value @('level','caseId','rider','mount','before','destination','moveType','moveExecutor',
        'beforeStop','elapsed','samples','afterStopInput','afterStop','afterReturn','returnResult') 'Chunk 4 blocked door'
    if($Value.level -cne 'NATIVE INTEGRATION' -or $Value.caseId -cne 'C4-TRAVERSAL-closed-door-stop-return' -or
        [string]::IsNullOrWhiteSpace($Value.rider) -or [string]::IsNullOrWhiteSpace($Value.mount) -or $Value.rider -ceq $Value.mount -or
        $Value.moveExecutor -cne $Value.mount -or $Value.moveType -cne 'Kingmaker.UnitLogic.Commands.UnitMoveTo' -or
        $Value.returnResult -cne 'Success' -or !(Test-KmcJsonNumber $Value.elapsed) -or [double]::IsNaN([double]$Value.elapsed) -or
        [double]::IsInfinity([double]$Value.elapsed) -or $Value.elapsed -lt 2 -or
        $Value.samples -isnot [array] -or $Value.samples.Count -lt 1 -or $Value.samples.Count -gt 512 -or
        $Value.destination -isnot [array] -or $Value.destination.Count -ne 3) { throw 'Blocked-door evidence lacks its exact native command and bounded measurement.' }
    foreach($kmcNumber in $Value.destination) {
        if(!(Test-KmcJsonNumber $kmcNumber) -or [double]::IsNaN([double]$kmcNumber) -or [double]::IsInfinity([double]$kmcNumber)) {throw 'Blocked-door destination must be finite native geometry.'}
    }
    $kmcFrames=-1L; $kmcCommandObserved=$false
    foreach($kmcState in @($Value.before)+@($Value.samples)+@($Value.beforeStop,$Value.afterStopInput,$Value.afterStop,$Value.afterReturn)) {
        Assert-KmcExactProperties $kmcState @('frame','position','riderPosition','farDistance','homeDistance','doorOpen','cutEnabled','cutNeedsUpdate',
            'reallyMoving','agentEnabled','avoidanceDisabled','corpulence','riderMove','mountMove','riderStandard','mountStandard',
            'moveStarted','moveFinished','moveResult','pathError','pathPoints','pathState') 'Chunk 4 blocked-door native state'
        foreach($kmcPosition in @('position','riderPosition')) {
            if($kmcState.$kmcPosition -isnot [array] -or $kmcState.$kmcPosition.Count -ne 3) {throw 'Blocked route must record both actual actor positions.'}
            foreach($kmcNumber in $kmcState.$kmcPosition) {
                if(!(Test-KmcJsonNumber $kmcNumber) -or [double]::IsNaN([double]$kmcNumber) -or [double]::IsInfinity([double]$kmcNumber)) {throw 'Blocked-route actor position must be finite.'}
            }
        }
        if(!(Test-KmcExactJsonInteger $kmcState.frame) -or $kmcState.frame -lt 0) {throw 'Blocked-route frame is invalid.'}
        foreach($kmcField in @('doorOpen','cutEnabled','cutNeedsUpdate','reallyMoving','agentEnabled','avoidanceDisabled')) {
            if($kmcState.$kmcField -isnot [bool]) {throw 'Blocked-route native states must be actual booleans.'}
        }
        foreach($kmcField in @('moveStarted','moveFinished','pathError')) {
            Assert-KmcNullableJsonBoolean $kmcState.$kmcField ('Blocked-route '+$kmcField)
        }
        if($null -ne $kmcState.pathPoints -and (!(Test-KmcExactJsonInteger $kmcState.pathPoints) -or $kmcState.pathPoints -lt 0)) {throw 'Blocked-route path point count is invalid.'}
        if($kmcState.doorOpen -or !$kmcState.cutEnabled -or $kmcState.cutNeedsUpdate -or !$kmcState.agentEnabled -or $kmcState.avoidanceDisabled) {throw 'Blocked route changed door/collision state or measured an unready cut.'}
        foreach($kmcField in @('farDistance','homeDistance','corpulence','riderMove','mountMove','riderStandard','mountStandard')) {
            if(!(Test-KmcJsonNumber $kmcState.$kmcField) -or [double]::IsNaN([double]$kmcState.$kmcField) -or
                [double]::IsInfinity([double]$kmcState.$kmcField) -or $kmcState.$kmcField -lt 0) {throw 'Blocked-route distances and costs must be finite nonnegative numbers.'}
        }
        if($kmcState.corpulence -le 0 -or [Math]::Abs($kmcState.corpulence-$Value.before.corpulence) -gt 0.000001 -or
            $kmcState.riderMove -gt $Value.before.riderMove + 0.0001) {throw 'Blocked route changed the mount footprint or taxed the carried rider.'}
        $kmcDx=[double]$kmcState.position[0]-[double]$Value.destination[0]; $kmcDz=[double]$kmcState.position[2]-[double]$Value.destination[2]
        if([Math]::Abs([double]$kmcState.farDistance-[Math]::Sqrt($kmcDx*$kmcDx+$kmcDz*$kmcDz)) -gt 0.0001) {throw 'Blocked-route remaining distance does not reconcile to native positions.'}
        $kmcDx=[double]$kmcState.position[0]-[double]$Value.before.position[0]; $kmcDz=[double]$kmcState.position[2]-[double]$Value.before.position[2]
        if([Math]::Abs([double]$kmcState.homeDistance-[Math]::Sqrt($kmcDx*$kmcDx+$kmcDz*$kmcDz)) -gt 0.0001) {throw 'Blocked-route return distance does not reconcile to native positions.'}
    }
    foreach($kmcState in $Value.samples) {
        if($kmcState.frame -le $kmcFrames) {throw 'Blocked-route samples reused or reversed a native frame.'}; $kmcFrames=$kmcState.frame
        if($kmcState.moveStarted -eq $true -or $kmcState.moveFinished -eq $true) {$kmcCommandObserved=$true}
    }
    if(!$kmcCommandObserved -or $Value.beforeStop.farDistance -le 1.25 -or $Value.afterReturn.homeDistance -gt 1.25 -or
        $Value.afterStop.reallyMoving -or $Value.afterReturn.reallyMoving) {throw 'Blocked route lacks a real attempted order, safe Stop or completed return.'}
    foreach($kmcCost in @('riderMove','mountMove','riderStandard','mountStandard')) {
        if($Value.beforeStop.$kmcCost -ne $Value.afterStopInput.$kmcCost) {throw 'Blocked-route Stop changed a genuine native cost.'}
    }
}
