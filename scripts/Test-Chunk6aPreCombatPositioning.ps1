param([switch]$FunctionsOnly)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
function Copy-Positioning($v){$v|ConvertTo-Json -Depth 50|ConvertFrom-Json}
function New-KmcPositioningFixture($Proof) {
    $rider=$Proof.identity.casterId;$mount=$Proof.mountId
    $point=[pscustomobject]@{x=2.3;y=0;z=0}
    $state=[pscustomobject]@{rider=@{actor=$rider};mount=@{actor=$mount};selectedIds=@($rider);relationshipState='Unmounted';generation=$Proof.identity.generationAtInit;ledger=@{acceptedMount=1;acceptedDismount=1;forcedDetach=1}}
    $foot=[pscustomobject]@{corpulence=0.5;probeRadius=1.25;probes=@(1..8|ForEach-Object{[pscustomobject]@{residual=0.0}})}
    $candidate=[pscustomobject]@{requestedSeparation=2.3;separation=2.3;travel=2.0;routeResidual=0.0;walkable=$true;eligible=$true;blockers=@();point=$point;footprint=$foot}
    $command=[pscustomobject]@{id=401;type='Kingmaker.UnitLogic.Commands.UnitMoveTo';executor=$rider;finished=$false;result='None'}
    $terminal=Copy-Positioning $command;$terminal.finished=$true;$terminal.result='Success'
    $events=@();$i=0
    foreach($boundary in @('command-bound','path-request','path-complete-before','path-complete-after','command-ended')) {
        $events+=[pscustomobject]@{boundary=$boundary;sequence=(++$i);commandObject=401;actorId=$rider;gameTicks=(100+$i);pathObject=201;requestSequence=1;
            points=$null;pathError=$null;pathState=$null;finished=($boundary -ceq 'command-ended')}
    }
    $path=[pscustomobject]@{complete=$true;errors=@();commandObject=401;actorId=$rider;events=$events;
        observerHooks=@(@('060018A3','060018B9','0600184F','06001850','060027B2')|ForEach-Object{[pscustomobject]@{token=$_;moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'}})}
    [pscustomobject]@{contract='native-ground-positioning-before-fresh-encounter';pass=$true;traceComplete=$true;beforeInCombat=$false;afterInCombat=$false;
        minimumTravel=0.25;maximumTravel=4;clearanceRadius=1.25;residual=0.01;frameBefore=10;frameAfter=40;gameTicksBefore=90;gameTicksAfter=110;
        before=$state;after=(Copy-Positioning $state);admittedCommand=$command;terminalCommand=$terminal;createdByPlayer=$true;candidates=@($candidate);
        destination=$point;originNavigation=@{footprint=$foot;actingClearance=$foot};terminalNavigation=@{footprint=$foot;actingClearance=$foot};path=$path;
        events=@([pscustomobject]@{boundary='admission-after';command=401;commandActor=$rider})}
}
if($FunctionsOnly){return}
$proof=[pscustomobject]@{identity=@{casterId='rider';generationAtInit=1};mountId='mount'}
$fixture=New-KmcPositioningFixture $proof
Assert-KmcChunk6aPreCombatPositioning $fixture $proof
$checks=1
function Reject-Positioning([scriptblock]$Mutation,[string]$Reason) {
    $copy=Copy-Positioning $fixture;& $Mutation $copy;$rejected=$false
    try{Assert-KmcChunk6aPreCombatPositioning $copy $proof}catch{if($_.Exception.Message -notlike ('*'+$Reason+'*')){throw};$rejected=$true}
    if(-not $rejected){throw 'Accepted corrupt positioning evidence'};$script:checks++
}
Reject-Positioning {param($c)$c.beforeInCombat=$true} 'outside-combat'
Reject-Positioning {param($c)$c.afterInCombat=$true} 'outside-combat'
Reject-Positioning {param($c)$c.traceComplete=$false} 'outside-combat'
Reject-Positioning {param($c)$c.maximumTravel=5} 'bounds or time'
Reject-Positioning {param($c)$c.minimumTravel=0} 'bounds or time'
Reject-Positioning {param($c)$c.residual=0.07} 'bounds or time'
Reject-Positioning {param($c)$c.frameAfter=$c.frameBefore} 'bounds or time'
Reject-Positioning {param($c)$c.terminalCommand.id=402} 'exact successful'
Reject-Positioning {param($c)$c.terminalCommand.result='Interrupt'} 'exact successful'
Reject-Positioning {param($c)$c.createdByPlayer=$false} 'exact successful'
Reject-Positioning {param($c)$c.after.selectedIds=@('mount')} 'actor, selection'
Reject-Positioning {param($c)$c.after.generation++} 'actor, selection'
Reject-Positioning {param($c)$c.after.ledger.forcedDetach++} 'transition ledger'
Reject-Positioning {param($c)$c.candidates=@()} 'search is missing'
Reject-Positioning {param($c)$c.candidates+=@($c.candidates[0])} 'search is missing'
foreach($value in @(1.9,2.7)){ $copy=Copy-Positioning $fixture;$copy.candidates[0].requestedSeparation=$value;$rejected=$false;try{Assert-KmcChunk6aPreCombatPositioning $copy $proof}catch{$rejected=$true};if(!$rejected){throw 'Nominal separation bound weakened'};$checks++ }
Reject-Positioning {param($c)$c.candidates[0].travel=4.1} 'route and clearance'
Reject-Positioning {param($c)$c.candidates[0].routeResidual=0.1} 'route and clearance'
# Frozen 205 allocation-rider-first-tb: the v2 ground plan proves a forward route that ends at the origin through its
# reciprocal origin boundary (reverse route from the point reaching the origin); the measured residual is the reverse one
# and only under the complete v2 proof.
function Add-ReciprocalProof($c) {
    $origin=[pscustomobject]@{x=0;y=0;z=0}
    $c|Add-Member -NotePropertyName plan -NotePropertyValue ([pscustomobject]@{contract='bounded-native-ground-plan-v2';origin=$origin;originInsideNavmesh=$true;originNativeNearest=[pscustomobject]@{walkable=$true;requested=$origin;clamped=$origin}}) -Force
    $cand=$c.candidates[0];$cand.routeResidual=2.0
    $cand|Add-Member -NotePropertyName routeEnd -NotePropertyValue ([pscustomobject]@{x=0;y=0;z=0}) -Force
    $cand|Add-Member -NotePropertyName reverseRouteEnd -NotePropertyValue ([pscustomobject]@{x=0.0002;y=0;z=0}) -Force
    $cand|Add-Member -NotePropertyName routeProof -NotePropertyValue 'reciprocal-origin-boundary' -Force
}
$copy=Copy-Positioning $fixture;Add-ReciprocalProof $copy;Assert-KmcChunk6aPreCombatPositioning $copy $proof;$script:checks++
Reject-Positioning {param($c)Add-ReciprocalProof $c;$c.candidates[0].reverseRouteEnd.x=0.5} 'route and clearance'
Reject-Positioning {param($c)Add-ReciprocalProof $c;$c.candidates[0].routeEnd.x=0.5} 'route and clearance'
Reject-Positioning {param($c)Add-ReciprocalProof $c;$c.candidates[0].routeProof='forward'} 'route and clearance'
Reject-Positioning {param($c)Add-ReciprocalProof $c;$c.plan.contract='bounded-native-ground-plan'} 'route and clearance'
Reject-Positioning {param($c)Add-ReciprocalProof $c;$c.plan.originInsideNavmesh=$false} 'route and clearance'
Reject-Positioning {param($c)Add-ReciprocalProof $c;$c.plan.originNativeNearest.walkable=$false} 'route and clearance'
Reject-Positioning {param($c)$c.candidates[0].blockers=@('actor')} 'route and clearance'
Reject-Positioning {param($c)$c.candidates[0].footprint.probes[0].residual=0.1} 'footprint is clipped'
Reject-Positioning {param($c)$c.clearanceRadius=0.5} 'clearance footprint'
Reject-Positioning {param($c)$c.destination.x++} 'another destination'
Reject-Positioning {param($c)$c.originNavigation.footprint=$null} 'origin or terminal'
Reject-Positioning {param($c)$c.path.commandObject=402} 'exact command'
Reject-Positioning {param($c)$c.path.observerHooks=@()} 'hook installation'
Reject-Positioning {param($c)$c.path.events=@($c.path.events|Where-Object boundary -CNE 'path-request')} 'sequence differs'
Reject-Positioning {param($c)$c.events=@()} 'admission callback'
Reject-Positioning {param($c)$c.events+=@([pscustomobject]@{boundary='prepare-before'})} 'allocation or reset'
Write-Host "PRE-COMBAT POSITIONING PASS=$checks FAIL=0; synthetic native-fixture evidence only"
