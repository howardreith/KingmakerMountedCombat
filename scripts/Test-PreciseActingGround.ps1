param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/PreciseActingGroundEvidence.ps1')
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$paths=[xml](Get-Content -Raw (Join-Path $repo 'LocalGamePaths.props'));$managed=Join-Path $paths.Project.PropertyGroup.KingmakerInstallDir 'Kingmaker_Data/Managed'
$resolver=[ResolveEventHandler]{param($sender,$e)$name=([Reflection.AssemblyName]::new($e.Name)).Name+'.dll';foreach($dir in @($managed,(Join-Path $managed 'UnityModManager'))){$p=Join-Path $dir $name;if(Test-Path -LiteralPath $p){return [Reflection.Assembly]::LoadFrom($p)}};return $null}
[AppDomain]::CurrentDomain.add_AssemblyResolve($resolver)
[Reflection.Assembly]::LoadFrom((Join-Path $managed 'Newtonsoft.Json.dll'))|Out-Null
$native=[Reflection.Assembly]::LoadFrom((Join-Path $managed 'Assembly-CSharp.dll'))
if($native.ManifestModule.ModuleVersionId.ToString()-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'){throw 'Pinned native MVID differs'}
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $repo ('bin/'+$Configuration+'/KingmakerMountedCombat.dll')))
$type=$assembly.GetType('KingmakerMountedCombat.Diagnostics.NativeActingGroundInput',$true)
$flags=[Reflection.BindingFlags]'Static,NonPublic'
$method=$type.GetMethod('AssertComplete',$flags);$factory=$type.GetMethod('Create',$flags)
$point=[Activator]::CreateInstance($factory.GetParameters()[0].ParameterType,[object[]]@([single]1,[single]2,[single]3))
$command=$factory.Invoke($null,[object[]]@($point,$null,[single]25,[single]0.2,$false))
if([Math]::Abs($command.ApproachRadius-0.03)-gt0.000001-or-not$command.CreatedByPlayer-or$command.MovementDelay-ne[single]0.2-or$command.Orientation-ne[single]25-or$command.ShowTargetMarker-ne$false-or$null-ne$command.SpeedLimit-or$command.Target.x-ne1-or$command.Target.y-ne2-or$command.Target.z-ne3){throw 'Compiled native factory lost its precise radius or native input properties'}
$old=[Activator]::CreateInstance($native.GetType('Kingmaker.UnitLogic.Commands.UnitMoveTo',$true),[object[]]@($point,[single]0.3))
if($old.ApproachRadius-le0.06){throw 'Pinned normal ground constructor no longer reproduces the larger arrival radius'}
$checks=2
$pinHashes=@{
 '060093D9'='816a8c1bb46df317a2805030f776d7b51d1af379c9844a51d21028b4f1d1af62'
 '060093DB'='a5ed6ae99c10c86c3fea1581b0574517c2487f7f5d986381006e69b2c24b1c73'
 '060093DC'='1b537eaab206cf2a920b9d9fe8973a854bb1acb81117a0c910739496f8a6d21b'
 '060026FF'='564b034856debb2341d52fea096eb64a3dc5f8dfc51f8868621506441318d7dd'
}
foreach($token in $pinHashes.Keys){
 $member=$native.ManifestModule.ResolveMethod([Convert]::ToInt32($token,16));$hash=[Security.Cryptography.SHA256]::Create()
 $actual=([BitConverter]::ToString($hash.ComputeHash($member.GetMethodBody().GetILAsByteArray()))).Replace('-','').ToLowerInvariant()
 if($actual-cne$pinHashes[$token]){throw ('Pinned native ground input differs '+$token)};$checks++
}
$caller=Get-Content -LiteralPath (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk6aTurnSetup.cs') -Raw
foreach($boundary in @('ClickGroundHandler.GetDefaultDirection(chunk6aActingSetupDestination)','commandRunner: (unit, point, speedLimit, orientation, delay, marker)','NativeActingGroundInput.Create(point, speedLimit, orientation, delay, marker)','ReferenceEquals(ownedCommand, chunk6aActingSetupCommand)','NativeActingGroundInput.AssertComplete(chunk6aActingSetup)','(float)chunk6aActingSetup["residual"] > MountedCombatSpatialPolicy.DiagnosticPlacementTolerance')){
 if(-not$caller.Contains($boundary)){throw ('Precise fixture caller lost boundary '+$boundary)};$checks++
}

function New-PreciseFixture {
 [pscustomobject]@{before=[pscustomobject]@{currentTurnActor='rider';frame=100};admittedCommand=[pscustomobject]@{id=401};terminalCommand=[pscustomobject]@{id=401};placementTolerance=0.06;destination=[pscustomobject]@{x=1.0;y=2.0;z=3.0};
 nativeGroundInput=[pscustomobject]@{contract='native-ground-input-with-one-precise-setup-command';methodToken='060093DB';directionToken='060093D9';constructorToken='060026FF';callbackCount=1;actorId='rider';selectedIds=@('rider');commandObject=401;createdByPlayer=$true;frame=100;approachRadius=0.03;terminalApproachRadius=0.03;agentApproachRadius=0.03;targetPoint=[pscustomobject]@{x=1.0;y=2.0;z=3.0}}}
}
function Check-Precise($v,[bool]$Expected){
 $p=$true;$c=$true;$producerError=$null
 try{Assert-KmcPreciseActingGroundInput $v}catch{$p=$false}
 try{$j=[Newtonsoft.Json.Linq.JObject]::Parse(($v|ConvertTo-Json -Depth 20 -Compress));$arguments=[object[]]::new(1);$arguments[0]=$j;$null=$method.Invoke($null,$arguments)}catch{$c=$false;$producerError=$_.Exception.ToString()}
 if($p-ne$Expected-or$c-ne$Expected){throw ('Precise producer/external mismatch: expected='+$Expected+' producer='+$c+' external='+$p+' '+$producerError)}
 $script:checks+=2
}
function Reject-Precise([scriptblock]$Change){$v=New-PreciseFixture;& $Change $v;Check-Precise $v $false}
Check-Precise (New-PreciseFixture) $true
Reject-Precise {param($v)$v.nativeGroundInput=$null}
foreach($field in @('contract','methodToken','directionToken','constructorToken','actorId','callbackCount','commandObject','createdByPlayer','frame')){
 Reject-Precise {param($v)$v.nativeGroundInput.$field='wrong'}
}
foreach($field in @('callbackCount','commandObject','frame')){Reject-Precise {param($v)$v.nativeGroundInput.$field++};Reject-Precise {param($v)$v.nativeGroundInput.$field=$null}}
Reject-Precise {param($v)$v.nativeGroundInput.callbackCount=0}
Reject-Precise {param($v)$v.nativeGroundInput.createdByPlayer=$false}
Reject-Precise {param($v)$v.nativeGroundInput.createdByPlayer='True'}
Reject-Precise {param($v)$v.nativeGroundInput.selectedIds=@('rider','mount')}
Reject-Precise {param($v)$v.nativeGroundInput.selectedIds=@('mount')}
Reject-Precise {param($v)$v.admittedCommand.id++}
Reject-Precise {param($v)$v.terminalCommand.id++}
Reject-Precise {param($v)$v.placementTolerance=0.3}
foreach($field in @('approachRadius','terminalApproachRadius','agentApproachRadius')){
 foreach($bad in @($null,0.3,0.06,0.0,'0.03')){Reject-Precise {param($v)$v.nativeGroundInput.$field=$bad}}
}
foreach($axis in @('x','y','z')){Reject-Precise {param($v)$v.nativeGroundInput.targetPoint.$axis++};Reject-Precise {param($v)$v.destination.$axis=$null}}
[AppDomain]::CurrentDomain.remove_AssemblyResolve($resolver)
'PRECISE ACTING GROUND PASS='+$checks+' FAIL=0; native construction and synthetic evidence, not Unity movement qualification'
