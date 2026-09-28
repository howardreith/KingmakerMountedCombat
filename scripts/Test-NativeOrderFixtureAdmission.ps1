param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$paths=[xml](Get-Content -Raw (Join-Path $repo 'LocalGamePaths.props'));$managed=Join-Path $paths.Project.PropertyGroup.KingmakerInstallDir 'Kingmaker_Data/Managed'
[Reflection.Assembly]::LoadFrom((Join-Path $managed 'Newtonsoft.Json.dll'))|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $repo ('bin/'+$Configuration+'/KingmakerMountedCombat.dll')))
$method=$assembly.GetType('KingmakerMountedCombat.Diagnostics.NativeMountOrderEvidence',$true).GetMethod('CanPrepareFixture',[Reflection.BindingFlags]'Static,NonPublic')
$checks=0
for($mask=0;$mask-lt256;$mask++){
 $arguments=[object[]]::new(8);for($bit=0;$bit-lt8;$bit++){$arguments[$bit]=[bool]($mask-band(1-shl$bit))}
 if([bool]$method.Invoke($null,$arguments)-ne($mask-eq240)){throw ('Incorrect pre-encounter fixture admission '+$mask)};$checks++
}
$source=Get-Content -Raw (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk6aMountOrderScenario.cs')
$start=$source.IndexOf('private void PrepareChunk6aMountOrderFixture()');$end=$source.IndexOf('private void ObserveChunk6aMountOrderTurn()',$start);if($start-lt0-or$end-le$start){throw 'Fixture source missing'}
$fixture=$source.Substring($start,$end-$start)
if($fixture-cmatch'CombatController.IsInTurnBasedCombat'){throw 'Live encounter query cannot establish pre-encounter setting'};$checks++
if($fixture-cnotmatch'turnBasedModeProbe\?\.TemporaryValue == true, turnBasedModeProbe\?\.TemporaryValueIsCurrent == true'){throw 'Exact temporary native mode lease missing'};$checks++
if($fixture-cnotmatch'CanPrepareFixture\(chunk6aOrderInputOwned, rider.IsInCombat, horse.IsInCombat,\s+Game.Instance.Player.IsInCombat, relationship.State == RelationshipState.Unmounted, Chunk6aIdle,'){throw 'Fixture arguments bypass fresh native state'};$checks++
$native=[Reflection.Assembly]::LoadFrom((Join-Path $managed 'Assembly-CSharp.dll'))
$m=$native.GetType('TurnBased.Controllers.CombatController',$true).GetMethod('IsInTurnBasedCombat',[Reflection.BindingFlags]'Public,Static')
$sha=[Security.Cryptography.SHA256]::Create();$hash=([BitConverter]::ToString($sha.ComputeHash($m.GetMethodBody().GetILAsByteArray()))).Replace('-','').ToLowerInvariant()
if($native.ManifestModule.ModuleVersionId.ToString()-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'-or$m.MetadataToken-ne0x06000BF6-or$hash-cne'732e637c0bde12d66b92c405bff7d42e26488c29db0c6d22f6084856d5e95da0'){throw 'Pinned mode query contract changed'};$checks++
'ORDER PRE-ENCOUNTER ADMISSION PASS='+$checks+' FAIL=0; no native engine invocation or resource write'
