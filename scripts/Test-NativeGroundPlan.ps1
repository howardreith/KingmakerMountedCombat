param([ValidateSet('Debug','Release')][string]$Configuration='Release',[switch]$FunctionsOnly)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/NativePreCombatGroundEvidence.ps1')
function Copy-GroundPlan($p){$p|ConvertTo-Json -Depth 80|ConvertFrom-Json}
function New-PlanPoint([double]$x,[double]$y,[double]$z){[pscustomobject]@{x=$x;y=$y;z=$z}}
function New-PlanFixture([bool]$Joint=$false,[bool]$RejectAll=$false){
 $mover='rider';$partner='mount';$extra=0.75;if($Joint){$mover='mount';$partner='rider';$extra=0.0}
 $origin=New-PlanPoint 4.3 0 0;$center=New-PlanPoint 0 0 0
 $occupants=@([pscustomobject]@{actorId=$mover;position=$origin;corpulence=0.5},[pscustomobject]@{actorId=$partner;position=$center;corpulence=0.5})
 New-PlanSearch $mover $partner $origin $center (0.5+$extra) $Joint $RejectAll $occupants $null
}
function New-PlanSearch($Actor,$Partner,$Origin,$Center,$Clearance,[bool]$Joint,[bool]$RejectAll,$Occupants,$Override){
 $p=[pscustomobject]@{contract='bounded-native-ground-plan';maximumCandidates=72;actorId=$Actor;partnerId=$Partner;origin=$Origin;center=$Center;corpulence=0.5;clearanceRadius=$Clearance;joint=$Joint;partnerPositionOverride=$Override;occupants=@($Occupants|ForEach-Object{Copy-GroundPlan $_});candidates=@();selectedIndex=-1}
 $length=[Math]::Sqrt([Math]::Pow($Origin.x-$Center.x,2)+[Math]::Pow($Origin.z-$Center.z,2));$dx=($Origin.x-$Center.x)/$length;$dz=($Origin.z-$Center.z)/$length
 foreach($radius in @(2.3,2.65,2.0)){
  foreach($i in 0..23){
   $signed=$i;if($i%2-eq1){$signed=-$i};$a=$signed*15*[Math]::PI/180
   $point=New-PlanPoint ($Center.x+([Math]::Cos($a)*$dx+[Math]::Sin($a)*$dz)*$radius) $Center.y ($Center.z+(-[Math]::Sin($a)*$dx+[Math]::Cos($a)*$dz)*$radius)
   $c=[pscustomobject]@{requested=(Copy-GroundPlan $point);point=$point;requestedSeparation=$radius;walkable=(-not$RejectAll);eligible=$false}
   $p.candidates+=@($c);if($RejectAll){continue}
   $travel=[Math]::Sqrt([Math]::Pow($Origin.x-$point.x,2)+[Math]::Pow($Origin.z-$point.z,2));$probes=@()
   foreach($j in 0..7){$angle=$j*[Math]::PI/4;$to=@(($point.x+[Math]::Sin($angle)*$Clearance),$point.y,($point.z+[Math]::Cos($angle)*$Clearance));$probes+=@([pscustomobject]@{requested=$to;endpoint=$to.Clone();residual=0.0})}
   $c|Add-Member travel $travel;$c|Add-Member separation $radius;$c|Add-Member routeEnd (Copy-GroundPlan $point);$c|Add-Member routeResidual 0.0
   $c|Add-Member footprint ([pscustomobject]@{center=@($point.x,$point.y,$point.z);corpulence=0.5;probeRadius=$Clearance;probes=$probes});$c|Add-Member blockers @()
   $c.eligible=$travel-ge0.25-and$travel-le4
   if(-not$c.eligible){continue}
   if($Joint){
    $counterfactual=@($p.occupants|ForEach-Object{Copy-GroundPlan $_});foreach($u in $counterfactual){if($u.actorId-ceq$Actor){$u.position=Copy-GroundPlan $point}}
    $nested=New-PlanSearch $Partner $Actor $Center $point 1.25 $false $false $counterfactual $point
    $c|Add-Member riderSearch $nested;if($nested.selectedIndex-lt0){continue}
   }
   $p.selectedIndex=$p.candidates.Count-1;return $p
  }
 }
 $p
}
if($FunctionsOnly){return}
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'));$paths=[xml](Get-Content -Raw (Join-Path $repo 'LocalGamePaths.props'));$managed=Join-Path $paths.Project.PropertyGroup.KingmakerInstallDir 'Kingmaker_Data/Managed'
[Reflection.Assembly]::LoadFrom((Join-Path $managed 'Newtonsoft.Json.dll'))|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $repo ('bin/'+$Configuration+'/KingmakerMountedCombat.dll')))
$method=$assembly.GetType('KingmakerMountedCombat.Diagnostics.NativePreCombatGroundEvidence',$true).GetMethod('AssertSearch',[Reflection.BindingFlags]'Static,NonPublic')
$script:checks=0
function Check-Plan($p,[bool]$Expected){
 $producer=$true;$result1=-2;try{$arguments=[object[]]::new(1);$arguments[0]=[Newtonsoft.Json.Linq.JObject]::Parse(($p|ConvertTo-Json -Depth 80 -Compress));$result1=$method.Invoke($null,$arguments)}catch{$producer=$false;if($Expected){throw}}
 $external=$true;$result2=-3;try{$result2=Assert-KmcNativeGroundPlan $p}catch{$external=$false;if($Expected){throw}}
 if($producer-ne$Expected-or$external-ne$Expected-or($Expected-and$result1-ne$result2)){throw ('Plan producer/external '+$producer+'/'+$external+' expected '+$Expected+' result '+$result1+'/'+$result2)};$script:checks+=2
}
foreach($joint in @($false,$true)){
 $p=New-PlanFixture $joint;Check-Plan $p $true
 function Reject-Plan([scriptblock]$Mutation){$x=Copy-GroundPlan $p;& $Mutation $x;try{Check-Plan $x $false}catch{throw ('Mutation '+$Mutation.ToString()+': '+$_)}}
 foreach($field in @('contract','actorId','partnerId')){Reject-Plan {param($x)$x.$field='foreign'}}
 foreach($field in @('maximumCandidates','corpulence','clearanceRadius','selectedIndex')){Reject-Plan {param($x)$x.$field++}}
 foreach($field in @('origin','center')){foreach($axis in @('x','y','z')){Reject-Plan {param($x)$x.$field.$axis++}}}
 Reject-Plan {param($x)$x.candidates=@()};Reject-Plan {param($x)$x.candidates+=@($x.candidates[0])}
 Reject-Plan {param($x)$x.candidates[0].eligible=$false};Reject-Plan {param($x)$x.candidates[0].walkable=$false}
 foreach($field in @('requestedSeparation','travel','separation','routeResidual')){Reject-Plan {param($x)$x.candidates[0].$field++}}
 foreach($field in @('requested','point','routeEnd')){foreach($axis in @('x','y','z')){if($field-ceq'routeEnd'-and$axis-ceq'y'){continue};Reject-Plan {param($x)$x.candidates[0].$field.$axis++}}}
 foreach($field in @('corpulence','probeRadius')){Reject-Plan {param($x)$x.candidates[0].footprint.$field++}}
 Reject-Plan {param($x)$x.candidates[0].footprint.center[0]++};Reject-Plan {param($x)$x.candidates[0].footprint.probes=@()}
 foreach($j in 0..7){Reject-Plan {param($x)$x.candidates[0].footprint.probes[$j].residual=0.1};Reject-Plan {param($x)$x.candidates[0].footprint.probes[$j].endpoint[0]++};Reject-Plan {param($x)$x.candidates[0].footprint.probes[$j].requested[0]++}}
 Reject-Plan {param($x)$x.candidates[0].blockers=@('foreign')};Reject-Plan {param($x)$x.occupants=@($x.occupants[0])}
 foreach($i in 0..1){Reject-Plan {param($x)$x.occupants[$i].actorId='foreign'};Reject-Plan {param($x)$x.occupants[$i].position.x++};Reject-Plan {param($x)$x.occupants[$i].corpulence=-1}}
 if($joint){
  Reject-Plan {param($x)$x.candidates[0].riderSearch=$null};Reject-Plan {param($x)$x.candidates[0].riderSearch.joint=$true}
  foreach($field in @('actorId','partnerId')){Reject-Plan {param($x)$x.candidates[0].riderSearch.$field='foreign'}}
  Reject-Plan {param($x)$x.candidates[0].riderSearch.partnerPositionOverride.x++}
  Reject-Plan {param($x)$x.candidates[0].riderSearch.occupants[0].position.x++}
  Reject-Plan {param($x)$x.candidates[0].riderSearch.candidates[0].eligible=$true}
  Reject-Plan {param($x)$x.candidates[0].riderSearch.candidates[1].footprint.probes[0].residual=0.1}
 }
 $p=New-PlanFixture $joint $true;Check-Plan $p $true
 $x=Copy-GroundPlan $p;$x.candidates=@($x.candidates|Select-Object -First 71);Check-Plan $x $false
 $x=Copy-GroundPlan $p;$x.selectedIndex=0;Check-Plan $x $false
}
'BOUNDED GROUND PLAN PASS='+$checks+' FAIL=0; synthetic producer/external only'
