param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
. (Join-Path $repo 'scripts/runtime/NativePassiveResourceEvidence.ps1')
. (Join-Path $PSScriptRoot 'Test-NativeGroundFixtures.ps1')
. (Join-Path $PSScriptRoot 'runtime/NativeTerminalBridgeEvidence.ps1')
$paths=[xml](Get-Content -Raw (Join-Path $repo 'LocalGamePaths.props'));$managed=Join-Path $paths.Project.PropertyGroup.KingmakerInstallDir 'Kingmaker_Data/Managed'
[Reflection.Assembly]::LoadFrom((Join-Path $managed 'Newtonsoft.Json.dll'))|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $repo ('bin/'+$Configuration+'/KingmakerMountedCombat.dll')))
$method=$assembly.GetType('KingmakerMountedCombat.Diagnostics.NativeTerminalBridgeEvidence',$true).GetMethod('AssertComplete',[Reflection.BindingFlags]'Static,NonPublic')
function New-TerminalBridgeCase([bool]$Tb,[string]$Relationship){
 $p=New-Passive;if($Tb){$p=New-Passive -Tb $true -ExpectedDecay 0 -ExpectedReactionCooldown 0.05 -ExpectedAllowance 0}
 $identity=[pscustomobject]@{commandObject=333;processObject=444;contextObject=555;controlIdentity='shell:3';casterId='rider';targetId='mount';generationAtInit=1;commandType='Move';abilityGuid='f053faad986631688defa003cd7bda0e'}
 $p|Add-Member commandIdentity (Copy-Passive $identity)
 $states=@{}
 foreach($boundary in @('before','after')){
  $s=[pscustomobject]@{rider=(Copy-Passive $p.$boundary.rider);mount=(Copy-Passive $p.$boundary.mount);relationshipState=$Relationship;generation=2;ledger=@{acceptedMount=1;acceptedDismount=0;forcedDetach=1}}
  foreach($role in @('rider','mount')){$s.$role|Add-Member nativePrepareCount 1};$states[$boundary]=$s
 }
 $terminal=[pscustomobject]@{boundary='terminal';identity=(Copy-Passive $identity);acted=$true;finished=$true;processEnded=$true;result='Success';simulatingClick=$false;frame=$p.before.frame;gameTicks=$p.before.gameTicks;allocationSequence=$p.before.allocationSequence;state=$states.before;nativeAllocation=@{rider=(Copy-Passive $p.before.rider);mount=(Copy-Passive $p.before.mount)}}
 $proof=[pscustomobject]@{identity=$identity;mountId='mount';samples=@($terminal)}
 $target=[pscustomobject]@{frame=$p.after.frame;gameTicks=$p.after.gameTicks;allocationSequence=$p.after.allocationSequence;turnBased=$Tb;state=$states.after;riderResources=(Copy-Passive $p.after.rider);mountResources=(Copy-Passive $p.after.mount)}
 [pscustomobject]@{proof=$proof;boundary=$target;bridge=$p}
}
$script:checks=0
function Check-Bridge($c,[bool]$Expected){
 $producer=$true;try{$a=[object[]]::new(5);$a[0]=[Newtonsoft.Json.Linq.JObject]::Parse(($c.proof|ConvertTo-Json -Depth 60 -Compress));$a[1]=[Newtonsoft.Json.Linq.JObject]::Parse(($c.boundary|ConvertTo-Json -Depth 60 -Compress));$a[2]=[Newtonsoft.Json.Linq.JObject]::Parse(($c.bridge|ConvertTo-Json -Depth 60 -Compress));$a[3]=$tb;$a[4]=$relationship;$null=$method.Invoke($null,$a)}catch{$producer=$false;if($Expected){throw}}
 $external=$true;try{Assert-KmcNativeTerminalBridge $c.proof $c.boundary $c.bridge $tb $relationship}catch{$external=$false;if($Expected){throw}}
 if($producer-ne$Expected-or$external-ne$Expected){throw ('Terminal bridge producer/external '+$producer+'/'+$external+' expected '+$Expected)};$script:checks+=2
}
foreach($tb in @($false,$true)){foreach($relationship in @('Mounted','Unmounted')){
 $p=New-TerminalBridgeCase $tb $relationship;Check-Bridge $p $true
 function Reject-Bridge([scriptblock]$Mutation){$x=Copy-Passive $p;& $Mutation $x;try{Check-Bridge $x $false}catch{throw ('Mutation '+$Mutation.ToString()+': '+$_)}}
 foreach($field in @('commandObject','processObject','contextObject')){Reject-Bridge {param($x)$x.bridge.commandIdentity.$field++};Reject-Bridge {param($x)$x.proof.samples[0].identity.$field++}}
 foreach($field in @('controlIdentity','casterId','targetId','abilityGuid','commandType')){Reject-Bridge {param($x)$x.bridge.commandIdentity.$field='foreign'}}
 foreach($field in @('acted','finished','processEnded')){Reject-Bridge {param($x)$x.proof.samples[0].$field=$false}}
 Reject-Bridge {param($x)$x.proof.samples[0].simulatingClick=$true};Reject-Bridge {param($x)$x.proof.samples[0].result='Interrupt'}
 foreach($field in @('frame','gameTicks','allocationSequence')){Reject-Bridge {param($x)$x.boundary.$field++};Reject-Bridge {param($x)$x.proof.samples[0].$field++}}
 Reject-Bridge {param($x)$x.boundary.turnBased=-not$x.boundary.turnBased}
 Reject-Bridge {param($x)$x.boundary.state.generation++};Reject-Bridge {param($x)$x.boundary.state.ledger.forcedDetach++}
 Reject-Bridge {param($x)$x.boundary.state.relationshipState='foreign'}
 foreach($role in @('rider','mount')){foreach($field in @('standard','move','swift','reactions','reactionCooldown','initiativeCooldown','initiativeOrder','reactionsPerRound')){
  Reject-Bridge {param($x)$x.proof.samples[0].state.$role.$field++}
  Reject-Bridge {param($x)$x.boundary.state.$role.$field++}
  Reject-Bridge {param($x)$x.bridge.after.$role.$field++;$x.boundary.($role+'Resources').$field++;$x.boundary.state.$role.$field++}
 }}
 Reject-Bridge {param($x)$x.bridge.observerHooks=@()};Reject-Bridge {param($x)$x.bridge.events=@()}
}}
'TERMINAL RESOURCE BRIDGE PASS='+$checks+' FAIL=0; synthetic producer/external only'
