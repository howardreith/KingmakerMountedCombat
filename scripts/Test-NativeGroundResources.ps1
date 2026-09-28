param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/NativePassiveResourceEvidence.ps1')
. (Join-Path $PSScriptRoot 'Test-NativeGroundFixtures.ps1')
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'));$paths=[xml](Get-Content -Raw (Join-Path $repo 'LocalGamePaths.props'));$managed=Join-Path $paths.Project.PropertyGroup.KingmakerInstallDir 'Kingmaker_Data/Managed'
[Reflection.Assembly]::LoadFrom((Join-Path $managed 'Newtonsoft.Json.dll'))|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $repo ('bin/'+$Configuration+'/KingmakerMountedCombat.dll')))
$method=$assembly.GetType('KingmakerMountedCombat.Diagnostics.NativePassiveResourceEvidence',$true).GetMethod('AssertGround',[Reflection.BindingFlags]'Static,NonPublic')
$script:checks=0
function Check-Ground($p,[bool]$Expected){
 $producer=$true;try{$arguments=[object[]]::new(1);$arguments[0]=[Newtonsoft.Json.Linq.JObject]::Parse(($p|ConvertTo-Json -Depth 60 -Compress));$null=$method.Invoke($null,$arguments)}catch{$producer=$false;if($Expected){throw}}
 $external=$true;try{Assert-KmcNativeGroundResources $p}catch{$external=$false;if($Expected){throw}}
 if($producer-ne$Expected-or$external-ne$Expected){throw ('Ground producer/external '+$producer+'/'+$external+' expected '+$Expected)};$script:checks+=2
}
foreach($mover in @('rider','mount')){
 $p=New-GroundResources $mover;Check-Ground $p $true
 function Reject-Ground([scriptblock]$Mutate){$x=Copy-Passive $p;& $Mutate $x;try{Check-Ground $x $false}catch{throw ('Mutation '+$Mutate.ToString()+': '+$_)}}
 foreach($field in @('standard','move','swift','reactions','reactionCooldown','initiativeCooldown','initiativeOrder','reactionsPerRound')){
  Reject-Ground {param($x)$x.events[3].state.$field++}
  foreach($role in @('rider','mount')){Reject-Ground {param($x)$x.after.$role.$field++}}
 }
 foreach($role in @('rider','mount')){
  Reject-Ground {param($x)$x.before.$role.inCombat=$true}
  Reject-Ground {param($x)$x.before.$role.grantSequence=1}
  Reject-Ground {param($x)$x.after.$role.actorObject++}
 }
 foreach($i in 0..3){
  foreach($field in @('commandType','actionType','commandActor')){Reject-Ground {param($x)$x.events[$i].$field='foreign'}}
  Reject-Ground {param($x)$x.events[$i].command++}
  Reject-Ground {param($x)$x.events[$i].acted=-not$x.events[$i].acted}
  Reject-Ground {param($x)$x.events[$i].simulatingClick=$true}
  Reject-Ground {param($x)$x.events[$i].state.inCombat=$true}
 }
 Reject-Ground {param($x)$x.groundCommand.commandObject=0}
 Reject-Ground {param($x)$x.groundCommand.casterId='foreign'}
 Reject-Ground {param($x)$x.turnBased=$true}
 Reject-Ground {param($x)$x.traceComplete=$false}
 Reject-Ground {param($x)$x.observerHooks=@()}
 Reject-Ground {param($x)$x.events=@($x.events|Select-Object -Skip 1)}
 Reject-Ground {param($x)$x.events+=@($x.events[3])}
 Reject-Ground {param($x)$x.events[3].boundary='actor-cost-after'}
 Reject-Ground {param($x)$x.events[4].boundary='clear-before'}
 Reject-Ground {param($x)$x.events[4].boundary='opportunity-before'}
 Reject-Ground {param($x)$x.events[4].gameDeltaTime=0.2}
 Reject-Ground {param($x)$x.events[4].sequence++}
 Reject-Ground {param($x)$x.events[4].gameTicks=$x.after.gameTicks+1}
 Reject-Ground {param($x)$x.events[5].state.reactions=0}
}
'OUTSIDE-COMBAT GROUND RESOURCES PASS='+$script:checks+' FAIL=0; synthetic producer/external only'
