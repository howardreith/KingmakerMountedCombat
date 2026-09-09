$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')

$kmcPass=0
function New-SlopeEnvelope {
    # Parser envelope only, never native evidence.
    return (@{startY=2; riderMoveBefore=0; minimumY=2; maximumY=2.75; heightChange=.75; dropped=0;
        samples=@(@{frame=1;position=@(0,2,0);stockAgentEnabled=$true;avoidanceDisabled=$false;corpulence=1.8;riderMove=0},
            @{frame=2;position=@(0,2.25,1);stockAgentEnabled=$true;avoidanceDisabled=$false;corpulence=1.8;riderMove=0},
            @{frame=3;position=@(0,2.75,2);stockAgentEnabled=$true;avoidanceDisabled=$false;corpulence=1.8;riderMove=0})} | ConvertTo-Json -Depth 8 | ConvertFrom-Json)
}
function Reject-Slope([scriptblock]$Change) {
    $kmcEnvelope=New-SlopeEnvelope
    & $Change $kmcEnvelope
    $kmcRejected=$false
    try { Assert-KmcChunk4NativeSlope $kmcEnvelope } catch { $kmcRejected=$true }
    if(!$kmcRejected) {throw ('Invalid slope envelope accepted: '+$Change)}
    $script:kmcPass++
}
Assert-KmcChunk4NativeSlope (New-SlopeEnvelope); $kmcPass++
Reject-Slope {param($e) $e.heightChange=.5}
Reject-Slope {param($e) $e.maximumY=3}
Reject-Slope {param($e) $e.minimumY=1}
Reject-Slope {param($e) $e.startY=[double]::NaN}
Reject-Slope {param($e) $e.dropped=1}
Reject-Slope {param($e) $e.dropped='0'}
Reject-Slope {param($e) $e.samples=@($e.samples[0],$e.samples[1])}
Reject-Slope {param($e) $e.samples[2].frame=2}
Reject-Slope {param($e) $e.samples[1].frame='2'}
Reject-Slope {param($e) $e.samples[1].position=@(1,2)}
Reject-Slope {param($e) $e.samples[1].position[0]=[double]::PositiveInfinity}
Reject-Slope {param($e) $e.samples[1].position[1]='2.25'}
Reject-Slope {param($e) $e.samples[1].stockAgentEnabled=$false}
Reject-Slope {param($e) $e.samples[1].stockAgentEnabled='true'}
Reject-Slope {param($e) $e.samples[1].avoidanceDisabled=$true}
Reject-Slope {param($e) $e.samples[1].avoidanceDisabled=0}
Reject-Slope {param($e) $e.samples[1].corpulence=0}
Reject-Slope {param($e) $e.samples[1].corpulence=1.7}
Reject-Slope {param($e) $e.samples[1].riderMove=.1}
Reject-Slope {param($e) $e.samples[1].riderMove=-1}
Reject-Slope {param($e) $e.riderMoveBefore=-1}
Reject-Slope {param($e) $e.samples[1].position[1]=2; $e.samples[2].position[1]=2.2; $e.maximumY=2.2; $e.heightChange=.2}
Reject-Slope {param($e) $e.samples[1] | Add-Member -NotePropertyName ghost -NotePropertyValue $true}
foreach($kmcField in @('enablePairedActivation','enableUnifiedMountedTurn','enablePairedCommandScheduler','enableDiagnosticOverlay','overlayPresent')) {
    $kmcConfig=@{enablePairedActivation=$true;enableUnifiedMountedTurn=$false;enablePairedCommandScheduler=$false;enableDiagnosticOverlay=$false;overlayPresent=$false}
    Assert-KmcChunk4PairedConfiguration ([pscustomobject]$kmcConfig); $kmcPass++
    $kmcConfig[$kmcField]=!$kmcConfig[$kmcField]
    $kmcRejected=$false; try {Assert-KmcChunk4PairedConfiguration ([pscustomobject]$kmcConfig)} catch {$kmcRejected=$true}
    if(!$kmcRejected){throw ('Wrong measured configuration accepted: '+$kmcField)}; $kmcPass++
    $kmcConfig[$kmcField]='false'
    $kmcRejected=$false; try {Assert-KmcChunk4PairedConfiguration ([pscustomobject]$kmcConfig) -AllowIntake} catch {$kmcRejected=$true}
    if(!$kmcRejected){throw ('Nonboolean configuration accepted: '+$kmcField)}; $kmcPass++
}
Assert-KmcChunk4PairedConfiguration ([pscustomobject]@{enablePairedActivation=$false;enableUnifiedMountedTurn=$false;enablePairedCommandScheduler=$false;enableDiagnosticOverlay=$false;overlayPresent=$false}) -AllowIntake; $kmcPass++
if((Get-KmcChunk4TraversalRows 'chunk4-traversal-core') -join ',' -cne 'mounted-distance-door-interaction,mounted-pair-doorway,mounted-pair-turns-and-corners,mounted-pair-party-formation'){throw 'Core traversal order changed.'}; $kmcPass++
if((Get-KmcChunk4TraversalRows 'chunk4-traversal-slope') -cne 'mounted-pair-slope'){throw 'Slope traversal row changed.'}; $kmcPass++
function New-BlockedState([int]$Frame,[double]$X) {
    return @{frame=$Frame;position=@($X,0,0);riderPosition=@($X,1,0);farDistance=(10-$X);homeDistance=$X;
        doorOpen=$false;cutEnabled=$true;cutNeedsUpdate=$false;reallyMoving=$false;agentEnabled=$true;avoidanceDisabled=$false;
        corpulence=1.8;riderMove=0;mountMove=0;riderStandard=0;mountStandard=0;moveStarted=$true;moveFinished=$true;moveResult='Success';
        pathError=$false;pathPoints=2;pathState='Complete'}
}
function New-BlockedEnvelope {
    return (@{level='NATIVE INTEGRATION';caseId='C4-TRAVERSAL-closed-door-stop-return';rider='rider';mount='mount';
        before=(New-BlockedState 0 0);destination=@(10,0,0);moveType='Kingmaker.UnitLogic.Commands.UnitMoveTo';moveExecutor='mount';
        beforeStop=(New-BlockedState 3 4);elapsed=3;samples=@((New-BlockedState 1 2),(New-BlockedState 2 4));
        afterStopInput=(New-BlockedState 3 4);afterStop=(New-BlockedState 4 4);afterReturn=(New-BlockedState 5 0);returnResult='Success'} |
        ConvertTo-Json -Depth 8 | ConvertFrom-Json)
}
function Reject-Blocked([scriptblock]$Change) {
    $kmcEnvelope=New-BlockedEnvelope; & $Change $kmcEnvelope
    $kmcRejected=$false; try {Assert-KmcChunk4BlockedDoor $kmcEnvelope} catch {$kmcRejected=$true}
    if(!$kmcRejected){throw ('Invalid blocked-door envelope accepted: '+$Change)}; $script:kmcPass++
}
Assert-KmcChunk4BlockedDoor (New-BlockedEnvelope); $kmcPass++
Reject-Blocked {param($e) $e.level='COMPONENT'}
Reject-Blocked {param($e) $e.caseId='unknown'}
Reject-Blocked {param($e) $e.mount='rider'}
Reject-Blocked {param($e) $e.moveType='Kingmaker.UnitLogic.Commands.UnitAttack'}
Reject-Blocked {param($e) $e.moveExecutor='rider'}
Reject-Blocked {param($e) $e.returnResult='Interrupt'}
Reject-Blocked {param($e) $e.elapsed=[double]::NaN}
Reject-Blocked {param($e) $e.elapsed=1}
Reject-Blocked {param($e) $e.destination[1]=[double]::PositiveInfinity}
Reject-Blocked {param($e) $e.destination=@(10,0)}
Reject-Blocked {param($e) $e.samples=@()}
Reject-Blocked {param($e) $e.samples[1].frame=1}
Reject-Blocked {param($e) $e.samples[1].doorOpen=$true}
Reject-Blocked {param($e) $e.samples[1].doorOpen='false'}
Reject-Blocked {param($e) $e.samples[1].cutEnabled=$false}
Reject-Blocked {param($e) $e.samples[1].cutNeedsUpdate=$true}
Reject-Blocked {param($e) $e.samples[1].agentEnabled=$false}
Reject-Blocked {param($e) $e.samples[1].avoidanceDisabled=$true}
Reject-Blocked {param($e) $e.samples[1].corpulence=1}
Reject-Blocked {param($e) $e.samples[1].riderMove=.1}
Reject-Blocked {param($e) $e.samples[1].farDistance=5}
Reject-Blocked {param($e) $e.samples[1].homeDistance=3}
Reject-Blocked {param($e) $e.samples[1].position[1]=[double]::NaN}
Reject-Blocked {param($e) $e.samples[1].riderPosition=@(1,2)}
Reject-Blocked {param($e) $e.samples[1].pathError='false'}
Reject-Blocked {param($e) $e.samples[1].pathPoints=-1}
Reject-Blocked {param($e) foreach($s in $e.samples){$s.moveStarted=$false;$s.moveFinished=$false}}
Reject-Blocked {param($e) $e.beforeStop.position[0]=9;$e.beforeStop.farDistance=1;$e.beforeStop.homeDistance=9}
Reject-Blocked {param($e) $e.afterStop.reallyMoving=$true}
Reject-Blocked {param($e) $e.afterReturn.position[0]=4;$e.afterReturn.farDistance=6;$e.afterReturn.homeDistance=4}
Reject-Blocked {param($e) $e.afterStopInput.mountMove=.1}
Write-Output "CHUNK 4 TRAVERSAL COMPONENT PASS=$kmcPass FAIL=0"
