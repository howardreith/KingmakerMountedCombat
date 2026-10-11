param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$labRoot=[IO.Path]::GetFullPath((Join-Path $repoRoot '../..'))
# Synthetic tests only. Original obstruction artifact is read unchanged as a shape fixture.
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'runtime/Chunk6aStopEvidence.ps1')
function Copy-StopDraft($v){$v|ConvertTo-Json -Depth 90|ConvertFrom-Json}
$original=(Join-Path $labRoot 'runtime-evidence/c6a-envelope-a-obstruction/phase3d-horse-scenario-evidence.json')
$originalHash=(Get-FileHash $original).Hash
$artifact=Get-Content $original -Raw|ConvertFrom-Json
$proof=Copy-StopDraft $artifact.observations.chunk6aObstruction.commandProof
$proof.contract='unacted-native-stop-no-cost-or-transition'
$terminal=$proof.samples[-1]
$geometry=Copy-StopDraft $proof.preClick.state.geometry
$dx=[double]$geometry.horsePosition.x-[double]$geometry.riderPosition.x;$dz=[double]$geometry.horsePosition.z-[double]$geometry.riderPosition.z
$length=[Math]::Sqrt($dx*$dx+$dz*$dz)
$geometry.riderPosition.x=[double]$geometry.riderPosition.x+0.5*$dx/$length
$geometry.riderPosition.z=[double]$geometry.riderPosition.z+0.5*$dz/$length
$geometry.horizontalDistance=$length-0.5;$geometry.centerDistance=$length-0.5
$terminal.state.geometry=$geometry
$events=@()
foreach($name in @('stop-before','command-ended','stop-after')) {
    $events+=[pscustomobject]@{sequence=$events.Count+1;boundary=$name;frame=$terminal.frame;gameTicks=$terminal.gameTicks;stopDepth=1;
        commandObject=$proof.identity.commandObject;casterId=$proof.identity.casterId;moveSlotObject=$proof.identity.commandObject;
        processObject=0;contextObject=0;started=$false;acted=$false;finished=($name -cne 'stop-before');result='Interrupt';state=(Copy-StopDraft $terminal.state)}
}
$input=[pscustomobject]@{contract='native-stop-surrounds-exact-unacted-mount-terminal';commandObject=$proof.identity.commandObject;casterId=$proof.identity.casterId;
    complete=$true;allocationTraceComplete=$true;allocationEvents=@();errors=@();events=$events;observerHooks=@(
        [pscustomobject]@{token='060000B9';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'},
        [pscustomobject]@{token='060027B2';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'})}
$case=[pscustomobject]@{contract='native-stop-during-exact-mount-approach';noResidue=$true;commandProof=$proof;input=$input;
    trigger=[pscustomobject]@{approachObserved=$true;riderReallyMoving=$true;gameTicks=$terminal.gameTicks;frame=$terminal.frame;
    commandObject=$proof.identity.commandObject;moveSlotObject=$proof.identity.commandObject;started=$false;acted=$false;finished=$false;
    geometry=(Copy-StopDraft $geometry);riderDisplacement=0.5}}
$script:checks=0
function Reject-Stop([scriptblock]$mutate,[string]$reason) {
    $copy=Copy-StopDraft $case;& $mutate $copy;$rejected=$false
    try {Assert-KmcStopInput $copy} catch {if($_.Exception.Message.IndexOf($reason,[StringComparison]::Ordinal)-lt 0){throw};$rejected=$true}
    if(-not $rejected){throw 'Invalid synthetic Stop proof admitted'};$script:checks++
}
Assert-KmcStopInput $case;$script:checks++
Reject-Stop {param($c) $c.commandProof.contract='unacted-native-obstruction-no-cost-or-transition'} 'Missing unacted Stop contract'
Reject-Stop {param($c) $c.noResidue=$false} 'retained residue'
Reject-Stop {param($c) $c.input.complete=$false} 'incomplete'
Reject-Stop {param($c) $c.input.errors=@('missing callback')} 'incomplete'
Reject-Stop {param($c) $c.input.events=@($c.input.events[0],$c.input.events[2])} 'exactly one input'
Reject-Stop {param($c) $c.input.events+=@($c.input.events[0])} 'exactly one input'
foreach($field in @('sequence','stopDepth','commandObject','processObject','contextObject','frame','gameTicks')) {
    Reject-Stop {param($c) $c.input.events[1].$field++} 'synchronous order'
}
foreach($field in @('started','acted')) {Reject-Stop {param($c) $c.input.events[0].$field=$true} 'synchronous order'}
Reject-Stop {param($c) $c.input.events[1].boundary='stop-after'} 'synchronous order'
Reject-Stop {param($c) $c.input.events[0].finished=$true} 'pending Move slot'
Reject-Stop {param($c) $c.input.events[0].moveSlotObject++} 'pending Move slot'
Reject-Stop {param($c) $c.input.events[1].result='Fail'} 'Interrupt terminal'
Reject-Stop {param($c) $c.input.events[2].finished=$false} 'Interrupt terminal'
Reject-Stop {param($c) $c.input.events[0].state.selectedIds+=@('mount')} 'selection or relationship'
Reject-Stop {param($c) $c.input.events[0].state.generation++} 'selection or relationship'
Reject-Stop {param($c) $c.input.events[1].state.ledger.forcedDetach++} 'transition ledger'
foreach($actor in @('rider','mount')) {foreach($field in @('standard','move','swift','reactions','reactionCooldown','initiativeCooldown','initiativeOrder','nativePrepareCount')) {
    Reject-Stop {param($c) $c.input.events[2].state.$actor.$field++} 'resource inside its native input callback'
}}
Reject-Stop {param($c) $c.trigger.approachObserved=$false} 'pending native approach'
Reject-Stop {param($c) $c.trigger.riderReallyMoving=$false} 'pending native approach'
Reject-Stop {param($c) $c.trigger.commandObject++} 'pending native approach'
Reject-Stop {param($c) $c.trigger.acted=$true} 'pending native approach'
Reject-Stop {param($c) $c.trigger.gameTicks++} 'pending native approach'
Reject-Stop {param($c) $c.trigger.geometry.riderPosition.x++} 'geometry differs'
Reject-Stop {param($c) $c.trigger.riderDisplacement=[double]::NaN} 'displacement'
Reject-Stop {param($c) $c.trigger.riderDisplacement=0} 'displacement'
Reject-Stop {param($c) $c.input.observerHooks[0].moduleMvid='wrong'} 'hook identity'
Reject-Stop {param($c) $c.input.observerHooks=@($c.input.observerHooks[1])} 'hook inventory'
Reject-Stop {param($c) $e=Copy-StopDraft $c.commandProof.resourceWindow.events[0];$e.boundary='cost-before';$e.state.actor=$c.commandProof.identity.casterId;$c.commandProof.resourceWindow.events+=@($e)} 'native cost'
Reject-Stop {param($c) $c.input.allocationTraceComplete=$false} 'allocation trace is incomplete'
Reject-Stop {param($c) $c.input.allocationEvents=@([pscustomobject]@{boundary='cost-after';state=[pscustomobject]@{actor=$c.commandProof.identity.casterId}})} 'Stop input contains native cost'
Reject-Stop {param($c) $c.input.allocationEvents=@([pscustomobject]@{boundary='admission-before';state=[pscustomobject]@{actor=$c.commandProof.mountId}})} 'another command admission'
if((Get-FileHash $original).Hash -cne $originalHash){throw 'Original artifact changed'}
Write-Host "STOP PASS=$script:checks FAIL=0; synthetic contract only, offline regression, no native qualification."
