$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/NativeMountApproachPathEvidence.ps1')
function Copy-PathCase($v){$v|ConvertTo-Json -Depth 40|ConvertFrom-Json}
function New-PathCase {
 $id=[pscustomobject]@{commandObject=101;casterId='rider';targetId='mount';abilityGuid='f053faad986631688defa003cd7bda0e'}
 $proof=[pscustomobject]@{identity=$id;preClick=[pscustomobject]@{gameTicks=1000};samples=@([pscustomobject]@{boundary='init';gameTicks=1000})}
 $hooks=@(
  @('060027A6','Kingmaker.UnitLogic.Commands.Base.UnitCommand.TickApproaching','ApproachBefore','ApproachAfter'),
  @('0600700F','Kingmaker.TurnBasedMode.PathVisualizer.CurrentPathForUnit',$null,'PreviewAfter'),
  @('060018B6','Kingmaker.View.UnitMovementAgent.FollowPrecomputedPath','PrecomputedBefore','PrecomputedAfter'),
  @('060018A3','Kingmaker.View.UnitMovementAgent.PathTo','RequestedBefore',$null))
 $path=[pscustomobject]@{pathObject=201;pathType='Pathfinding.XPath';pathState='Complete';pathError=$false;horizontalLength=3.0;points=@([pscustomobject]@{x=0.0;y=0.0;z=0.0},[pscustomobject]@{x=3.0;y=0.0;z=0.0})}
 $basis=[pscustomobject]@{sequence=0;boundary='';frame=10;gameTicks=1000;commandObject=101;moveSlotObject=101;forcedPathObject=0;casterId='rider';targetId='mount';receiverObject=801;approachDepth=1;started=$false;acted=$false;finished=$false;commandType='Move';abilityGuid=$id.abilityGuid;processObject=0;contextObject=0;turnObject=401;turnActor='rider';turnStatus='Acting';destination=[pscustomobject]@{x=5.0;y=0.0;z=0.0};targetPosition=[pscustomobject]@{x=5.0;y=0.0;z=0.0};approachRadius=2.85;path=$null;agentPathObject=0}
 $names=@('preview-before-click','command-bound','approach-before','preview-return','precomputed-before','precomputed-after','approach-after')
 $events=@(for($i=0;$i -lt $names.Count;$i++){
  $e=Copy-PathCase $basis;$e.sequence=$i+1;$e.boundary=$names[$i]
  if($i -lt 2){$e.approachDepth=0}
  if($i -eq 0){$e.commandObject=0}
  if($i -in @(0,3,4,5)){$e.path=Copy-PathCase $path}
  if($i -in @(4,5)){$e.path.pathObject=202;$e.path.pathType='Pathfinding.ForcedPath'}
  if($i -ge 5){$e.agentPathObject=202}
  $e.receiverObject=if($i -in @(0,3)){803}elseif($i -eq 1){0}elseif($i -in @(2,6)){101}else{802}
  $e
 })
 [pscustomobject]@{command=$proof;path=[pscustomobject]@{contract='exact-native-mount-preview-and-consumed-path-observation';commandObject=101;casterId='rider';targetId='mount';complete=$true;errors=@();actorViewObject=801;movementAgentObject=802;pathVisualizerObject=803;events=$events;observerHooks=@(foreach($h in $hooks){[pscustomobject]@{token=$h[0];method=$h[1];prefix=$h[2];postfix=$h[3];moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'}})}}
}
$script:passed=0
function Check-Path($c,[bool]$expected,[string]$label){
 $actual=$true;try{Assert-KmcNativeMountApproachPath $c.path $c.command}catch{$actual=$false;if($expected){throw}}
 if($actual -ne $expected){throw ('Path evidence unexpectedly accepted '+$label)}
 $script:passed++
}
function Reject-Path([string]$label,[scriptblock]$mutation){$c=New-PathCase;& $mutation $c;Check-Path $c $false $label}
Check-Path (New-PathCase) $true 'native precomputed path'
Reject-Path 'incomplete' {param($c) $c.path.complete=$false}
Reject-Path 'observer error' {param($c) $c.path.errors=@('missing')}
Reject-Path 'mixed command' {param($c) $c.path.commandObject++}
Reject-Path 'wrong caster' {param($c) $c.path.casterId='foreign'}
Reject-Path 'wrong target' {param($c) $c.path.targetId='foreign'}
Reject-Path 'wrong callback receiver' {param($c) $c.path.events[4].receiverObject++}
Reject-Path 'missing hook' {param($c) $c.path.observerHooks=@($c.path.observerHooks|Select-Object -Skip 1)}
Reject-Path 'wrong native token' {param($c) $c.path.observerHooks[0].token='06000000'}
Reject-Path 'wrong native MVID' {param($c) $c.path.observerHooks[0].moduleMvid='foreign'}
Reject-Path 'missing callback' {param($c) $c.path.events=@($c.path.events|Where-Object boundary -CNE 'preview-return')}
Reject-Path 'duplicate callback' {param($c) $c.path.events+=@(Copy-PathCase $c.path.events[3])}
Reject-Path 'late baseline' {param($c) $c.path.events[0].gameTicks=1001}
foreach($field in @('commandObject','moveSlotObject','forcedPathObject','turnObject','processObject','contextObject','sequence','approachDepth')){Reject-Path $field {param($c) $c.path.events[4].$field++}}
foreach($field in @('started','acted','finished')){Reject-Path $field {param($c) $c.path.events[4].$field=$true}}
foreach($field in @('casterId','targetId','turnActor','turnStatus','commandType','abilityGuid')){Reject-Path $field {param($c) $c.path.events[4].$field='foreign'}}
Reject-Path 'foreign consumed path object' {param($c) $c.path.events[5].path.pathObject++}
Reject-Path 'agent path differs' {param($c) $c.path.events[5].agentPathObject++}
Reject-Path 'foreign consumed points' {param($c) $c.path.events[4].path.points[1].x=4;$c.path.events[4].path.horizontalLength=4}
Reject-Path 'fabricated length' {param($c) $c.path.events[4].path.horizontalLength=4}
Reject-Path 'native path error' {param($c) $c.path.events[3].path.pathError=$true}
Reject-Path 'empty native path' {param($c) $c.path.events[3].path.points=@()}
Reject-Path 'stale preview destination' {param($c) foreach($i in @(3,4,5)){$c.path.events[$i].path.points[1].x=-3}}
Reject-Path 'nonfinite coordinate' {param($c) $c.path.events[4].path.points[1].x=[double]::NaN}
Reject-Path 'nonfinite length' {param($c) $c.path.events[4].path.horizontalLength=[double]::PositiveInfinity}
Reject-Path 'nonsynchronous callback' {param($c) $c.path.events[4].frame++}
$c=New-PathCase;$c.path.events=@($c.path.events[0..3]);$c.path.events[3].path.pathObject=0;$c.path.events[3].path.points=$null
$e=Copy-PathCase $c.path.events[2];$e.sequence=5;$e.boundary='path-request-before';$e.receiverObject=802;$c.path.events+=@($e)
Check-Path $c $true 'native direct path with null preview'
$direct=Copy-PathCase $c
$c.path.events[3].boundary='path-request-before';$c.path.events[3].receiverObject=802;$c.path.events[3].path=$null
Check-Path $c $false 'direct path missing preview callback'
$c=Copy-PathCase $direct;$c.path.events[4].frame++
Check-Path $c $false 'direct path asynchronous preview callback'
$c=Copy-PathCase $direct;$c.path.events[3].path.pathObject=201
Check-Path $c $false 'direct path nonnull preview'
$c=Copy-PathCase $direct
$c.path.events[4].destination.x=6
Check-Path $c $false 'direct path wrong target'
Write-Host "NATIVE MOUNT PATH PROTOCOL PASS=$script:passed FAIL=0; synthetic checks, not native qualification."