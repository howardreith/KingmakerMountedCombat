param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$labRoot=[IO.Path]::GetFullPath((Join-Path $repoRoot '../..'))
# Synthetic tests only. Original obstruction artifact is read unchanged as a shape fixture.
$ErrorActionPreference='Stop'
. (Join-Path $repoRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'runtime/Chunk6aReplacementEvidence.ps1')
function Copy-StopDraft($v){$v|ConvertTo-Json -Depth 90|ConvertFrom-Json}
$original=(Join-Path $labRoot 'runtime-evidence/c6a-envelope-a-obstruction/phase3d-horse-scenario-evidence.json')
$originalHash=(Get-FileHash $original).Hash
$artifact=Get-Content $original -Raw|ConvertFrom-Json
$proof=Copy-StopDraft $artifact.observations.chunk6aObstruction.commandProof
$proof.contract='unacted-native-replacement-no-cost-or-transition'
$terminal=$proof.samples[-1]
$geometry=Copy-StopDraft $proof.preClick.state.geometry
$dx=[double]$geometry.horsePosition.x-[double]$geometry.riderPosition.x;$dz=[double]$geometry.horsePosition.z-[double]$geometry.riderPosition.z
$length=[Math]::Sqrt($dx*$dx+$dz*$dz)
$geometry.riderPosition.x=[double]$geometry.riderPosition.x+0.5*$dx/$length
$geometry.riderPosition.z=[double]$geometry.riderPosition.z+0.5*$dz/$length
$geometry.horizontalDistance=$length-0.5;$geometry.centerDistance=$length-0.5
$terminal.state.geometry=$geometry
$events=@()
foreach($name in @('replacement-before','command-ended','replacement-after')) {
    $events+=[pscustomobject]@{sequence=$events.Count+1;boundary=$name;frame=$terminal.frame;gameTicks=$terminal.gameTicks;runDepth=1;
        commandObject=$proof.identity.commandObject;casterId=$proof.identity.casterId;moveSlotObject=$proof.identity.commandObject;
        processObject=0;contextObject=0;started=$false;acted=$false;finished=($name -cne 'replacement-before');result='Interrupt';state=(Copy-StopDraft $terminal.state)}
}
$input=[pscustomobject]@{contract='native-replacement-surrounds-exact-unacted-mount-terminal';commandObject=$proof.identity.commandObject;casterId=$proof.identity.casterId;
    complete=$true;allocationTraceComplete=$true;allocationEvents=@();errors=@();events=$events;observerHooks=@(
        [pscustomobject]@{token='060026B2';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'},
        [pscustomobject]@{token='060027B2';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'})}
$case=[pscustomobject]@{contract='native-replacement-during-exact-mount-approach';noResidue=$true;commandProof=$proof;input=$input;
    trigger=[pscustomobject]@{approachObserved=$true;riderReallyMoving=$true;gameTicks=$terminal.gameTicks;frame=$terminal.frame;
    commandObject=$proof.identity.commandObject;moveSlotObject=$proof.identity.commandObject;started=$false;acted=$false;finished=$false;
    geometry=(Copy-StopDraft $geometry);riderDisplacement=0.5}}

$replacementId=17291
$case|Add-Member -NotePropertyName destination -NotePropertyValue (Copy-StopDraft $proof.preClick.state.geometry.riderPosition)
$input|Add-Member -NotePropertyName replacementObject -NotePropertyValue $replacementId
for($i=0;$i-lt3;$i++){
    $r=[pscustomobject]@{commandObject=$replacementId;class='Kingmaker.UnitLogic.Commands.UnitMoveTo';executorId=$(if($i-eq0){$null}else{$proof.identity.casterId});type='Move';createdByPlayer=$true;ignoreCooldown=$true;started=$false;acted=$false;finished=$false;result='None';destination=(Copy-StopDraft $case.destination)}
    $events[$i]|Add-Member -NotePropertyName replacement -NotePropertyValue $r
}
$events[2].moveSlotObject=$replacementId
$input.allocationEvents=@(for($i=0;$i-lt2;$i++){
    [pscustomobject]@{boundary=@('admission-before','admission-after')[$i];sequence=801+$i;command=$replacementId;commandType='Kingmaker.UnitLogic.Commands.UnitMoveTo';state=[pscustomobject]@{actor=$proof.identity.casterId};actionType='Move';started=$false;acted=$false;finished=$false;frame=$terminal.frame;gameTicks=$terminal.gameTicks;commandActor=$(if($i-eq0){$null}else{$proof.identity.casterId});commandInitialized=($i-eq1)}
})

$script:checks=0
function Reject-Stop([scriptblock]$mutate,[string]$reason) {
    $copy=Copy-StopDraft $case;& $mutate $copy;$rejected=$false
    try {Assert-KmcReplacementInput $copy} catch {if($_.Exception.Message.IndexOf($reason,[StringComparison]::Ordinal)-lt 0){throw};$rejected=$true}
    if(-not $rejected){throw 'Invalid synthetic Stop proof admitted'};$script:checks++
}
Assert-KmcReplacementInput $case;$script:checks++
Reject-Stop {param($c) $c.commandProof.contract='unacted-native-obstruction-no-cost-or-transition'} 'Missing unacted Replacement contract'
Reject-Stop {param($c) $c.noResidue=$false} 'retained residue'
Reject-Stop {param($c) $c.input.complete=$false} 'incomplete'
Reject-Stop {param($c) $c.input.errors=@('missing callback')} 'incomplete'
Reject-Stop {param($c) $c.input.events=@($c.input.events[0],$c.input.events[2])} 'exactly one input'
Reject-Stop {param($c) $c.input.events+=@($c.input.events[0])} 'exactly one input'
foreach($field in @('sequence','runDepth','commandObject','processObject','contextObject','frame','gameTicks')) {
    Reject-Stop {param($c) $c.input.events[1].$field++} 'synchronous order'
}
foreach($field in @('started','acted')) {Reject-Stop {param($c) $c.input.events[0].$field=$true} 'synchronous order'}
Reject-Stop {param($c) $c.input.events[1].boundary='replacement-after'} 'synchronous order'
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
Reject-Stop {param($c) $c.input.allocationEvents=@([pscustomobject]@{boundary='cost-after';state=[pscustomobject]@{actor=$c.commandProof.identity.casterId}})} 'Replacement input contains native cost'
Reject-Stop {param($c) $c.input.allocationEvents=@([pscustomobject]@{boundary='admission-before';state=[pscustomobject]@{actor=$c.commandProof.mountId}})} 'admission pair'

foreach($field in @('commandObject','type','class','createdByPlayer','ignoreCooldown','started','acted','finished','result','executorId')){
    Reject-Stop {param($c)$c.input.events[2].replacement.$field='wrong'} ''
}
Reject-Stop {param($c)$c.input.replacementObject=$c.commandProof.identity.commandObject} 'identity'
Reject-Stop {param($c)$c.input.events[1].moveSlotObject=0} 'exact Move slot'
Reject-Stop {param($c)$c.input.events[2].moveSlotObject=$c.commandProof.identity.commandObject} 'exact Move slot'
Reject-Stop {param($c)$c.input.events[0].replacement.executorId='premature'} 'constructor'
Reject-Stop {param($c)$c.destination.x++} 'destination'
Reject-Stop {param($c)$c.input.events[1].replacement.destination.z++} 'destination'
Reject-Stop {param($c)$c.input.allocationEvents=@()} 'admission pair'
Reject-Stop {param($c)$c.input.allocationEvents+=@($c.input.allocationEvents[0])} 'admission pair'
foreach($i in 0..1){foreach($field in @('command','commandType','actionType','frame','gameTicks','started','acted','finished')){
    Reject-Stop {param($c)$c.input.allocationEvents[$i].$field='wrong'} 'admission'
}}
Reject-Stop {param($c)$c.input.allocationEvents[0].commandActor='premature'} 'already initialized'
Reject-Stop {param($c)$c.input.allocationEvents[1].commandActor='foreign'} 'not initialized'
Reject-Stop {param($c)$c.input.allocationEvents[1].commandInitialized=$false} 'not initialized'
Reject-Stop {param($c)$c.input.allocationEvents[1].sequence=800} 'order'

if((Get-FileHash $original).Hash -cne $originalHash){throw 'Original artifact changed'}
Write-Host "REPLACEMENT PASS=$script:checks FAIL=0; synthetic contract only, offline regression, no native qualification."
