[CmdletBinding()]
param([ValidateSet('Debug','Release')][string]$Configuration='Release')
# Shared synthetic fixtures exercised independently by the C# producer replay and external PowerShell replay.
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'runtime/NativeApproachMovementEvidence.ps1')
[Reflection.Assembly]::LoadFrom('C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll')|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $PSScriptRoot "../bin/$Configuration/KingmakerMountedCombat.dll"))
$method=$assembly.GetType('KingmakerMountedCombat.Diagnostics.NativeApproachMovementEvidence',$true).GetMethod('Evaluate',[Reflection.BindingFlags]'Static,NonPublic')
function Copy-Movement($v){$v|ConvertTo-Json -Depth 30|ConvertFrom-Json}
function New-MovementCase {
    $before=[pscustomobject]@{boundary='approach-movement-before';sequence=1;frame=500;gameTicks=1200;command=101;commandActor='rider';currentActor='rider';actionType='Move';commandType='Kingmaker.UnitLogic.Commands.UnitUseAbility';turn=501;movementTurn=501;turnStatus='Acting';nativeTurnBased=$true;nativePassing=$false;started=$false;acted=$false;finished=$false;slotCommand=101;force=$false;fiveFootStep=$false;delta=0.075;state=[pscustomobject]@{actor='rider';move=0.25;timeMoved=1.5;timeForced=0.0;timeStepped=0.0;standard=4.0;swift=2.0;reactions=1;reactionsPerRound=1;reactionCooldown=0.0;initiativeCooldown=0.0;initiativeOrder=12}}
    $after=Copy-Movement $before;$after.boundary='approach-movement-after';$after.sequence=2;$after.delta=0.05;$after.state.move=0.3;$after.state.timeMoved=1.55
    $before2=Copy-Movement $after;$before2.boundary='approach-movement-before';$before2.sequence=5;$before2.frame=501;$before2.gameTicks=1210;$before2.delta=0.075
    $after2=Copy-Movement $before2;$after2.boundary='approach-movement-after';$after2.sequence=6;$after2.state.move=0.375;$after2.state.timeMoved=1.625
    [pscustomobject]@{events=@($before,$after,$before2,$after2);identity=[pscustomobject]@{commandObject=101;casterId='rider'};
        pre=[pscustomobject]@{gameTicks=1000;allocationSequence=0;state=[pscustomobject]@{rider=[pscustomobject]@{nativeTurnObject=501;move=0.25;timeMoved=1.5;timeForced=0.0;timeStepped=0.0}}};
        cost=[pscustomobject]@{sequence=9;gameTicks=2000;turn=501;state=[pscustomobject]@{move=0.375}};
        ability='f053faad986631688defa003cd7bda0e';inCombat=$true;turnBased=$true;required=$true;
        hooks=@([pscustomobject]@{method='TurnBased.Controllers.TurnController.TickMovement';token='06000C37';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';prefix='ApproachMovementBefore';postfix='ApproachMovementAfter'})}
}
function Invoke-MovementProducer($c) {
    $arguments=[object[]]::new(10)
    $arguments[0]=[Newtonsoft.Json.Linq.JArray]::Parse((ConvertTo-Json -InputObject @($c.events) -Depth 30 -Compress))
    $arguments[1]=[Newtonsoft.Json.Linq.JObject]::Parse(($c.pre|ConvertTo-Json -Depth 30 -Compress))
    $arguments[2]=[Newtonsoft.Json.Linq.JObject]::Parse(($c.cost|ConvertTo-Json -Depth 30 -Compress))
    $arguments[3]=[int]$c.identity.commandObject;$arguments[4]=[string]$c.identity.casterId;$arguments[5]=[string]$c.ability
    $arguments[6]=[bool]$c.inCombat;$arguments[7]=[bool]$c.turnBased;$arguments[8]=[bool]$c.required
    $arguments[9]=[Newtonsoft.Json.Linq.JArray]::Parse((ConvertTo-Json -InputObject @($c.hooks) -Depth 30 -Compress))
    $answer=$method.Invoke($null,$arguments).ToString()|ConvertFrom-Json
    if($answer.pass -ne $true){throw ($answer.errors -join '; ')}
}
$script:checks=0
function Check-MovementBoth($c,[bool]$expected) {
    foreach($engine in @('producer','external')) {
        $pass=$true
        try {
            if($engine -ceq 'producer'){Invoke-MovementProducer $c}else{Assert-KmcNativeMovementEvidence $c.events $c.pre $c.cost $c.identity $c.ability $c.inCombat $c.turnBased $c.required $c.hooks}
        } catch {$pass=$false;if($expected){throw}}
        if($pass -ne $expected){throw "Movement $engine admitted an invalid synthetic case"};$script:checks++
    }
}
function Reject-MovementBoth([scriptblock]$mutate){$c=New-MovementCase;& $mutate $c;Check-MovementBoth $c $false}
Check-MovementBoth (New-MovementCase) $true
foreach($field in @('command','slotCommand','turn','movementTurn')){Reject-MovementBoth {param($c) $c.events[1].$field++}}
foreach($field in @('commandActor','currentActor')){Reject-MovementBoth {param($c) $c.events[1].$field='mount'}}
foreach($field in @('started','acted','finished','nativePassing','force','fiveFootStep')){Reject-MovementBoth {param($c) $c.events[1].$field=$true}}
foreach($field in @('standard','swift','reactions','reactionsPerRound','reactionCooldown','initiativeCooldown','initiativeOrder','timeForced','timeStepped')){Reject-MovementBoth {param($c) $c.events[1].state.$field++}}
Reject-MovementBoth {param($c) $c.events[1].state.move+=0.01}
Reject-MovementBoth {param($c) $c.events[1].state.timeMoved+=0.01}
Reject-MovementBoth {param($c) $c.events[1].delta=0.1}
Reject-MovementBoth {param($c) $c.events[1].delta=[double]::NaN}
Reject-MovementBoth {param($c) $c.events[1].frame++}
Reject-MovementBoth {param($c) $c.events[1].sequence++}
Reject-MovementBoth {param($c) $c.events[2].state.move+=0.01;$c.events[3].state.move+=0.01}
Reject-MovementBoth {param($c) $c.events[2].state.timeMoved+=0.01;$c.events[3].state.timeMoved+=0.01}
Reject-MovementBoth {param($c) $c.pre.state.rider.timeMoved=0.0}
Reject-MovementBoth {param($c) $c.pre.state.rider.nativeTurnObject++}
Reject-MovementBoth {param($c) $c.pre.state.rider.timeForced=1.0}
Reject-MovementBoth {param($c) $c.pre.state.rider.timeStepped=1.0}
Reject-MovementBoth {param($c) $c.cost.state.move=0.25}
Reject-MovementBoth {param($c) $c.cost.state.move=0.4}
Reject-MovementBoth {param($c) $c.events=@()}
Reject-MovementBoth {param($c) $c.events=@($c.events[0])}
Reject-MovementBoth {param($c) $c.cost.sequence=6}
Reject-MovementBoth {param($c) $c.pre.allocationSequence=1}
Reject-MovementBoth {param($c) $c.events[2].gameTicks=1190;$c.events[3].gameTicks=1190}
Reject-MovementBoth {param($c) $c.cost.gameTicks=1200}
Reject-MovementBoth {param($c) $c.hooks=@()}
Reject-MovementBoth {param($c) $c.hooks[0].moduleMvid='wrong'}
Reject-MovementBoth {param($c) $c.hooks+=@($c.hooks[0])}
Reject-MovementBoth {param($c) $c.turnBased=$false}
Reject-MovementBoth {param($c) $c.inCombat=$false}
Reject-MovementBoth {param($c) $c.ability='3af2b81f4d72bbb30501fa730fcdf36e'}
Reject-MovementBoth {param($c) foreach($e in $c.events){$e.delta=0;$e.state.move=0.25;$e.state.timeMoved=1.5};$c.cost.state.move=0.25}
foreach($field in @('move','timeMoved','timeForced','timeStepped')){Reject-MovementBoth {param($c) $c.pre.state.rider.$field=$null}}
foreach($mode in @('adjacent-mount','dismount','realtime','exploration')) {
    $c=New-MovementCase;$c.events=@();$c.cost.state.move=0.25;$c.required=$false
    if($mode -ceq 'dismount'){$c.ability='3af2b81f4d72bbb30501fa730fcdf36e'}
    if($mode -ceq 'realtime'){$c.turnBased=$false}
    if($mode -ceq 'exploration'){$c.inCombat=$false;$c.turnBased=$false}
    Check-MovementBoth $c $true
}
Write-Host "NATIVE MOVEMENT PRODUCER+EXTERNAL PASS=$script:checks FAIL=0; shared synthetic checks, not native qualification."