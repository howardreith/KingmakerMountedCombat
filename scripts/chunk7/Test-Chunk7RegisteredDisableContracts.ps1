[CmdletBinding()]
param([string]$RepositoryRoot='C:/Dev/KingmakerMountedCombatLab/repo/KingmakerMountedCombat',[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$draft=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if(-not$OutputDirectory){$OutputDirectory=Join-Path $draft ('checks/contracts-'+[Guid]::NewGuid().ToString('N'))}
$OutputDirectory=[IO.Path]::GetFullPath($OutputDirectory)
if(-not$OutputDirectory.StartsWith($draft+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)-or(Test-Path -LiteralPath $OutputDirectory)){throw 'Fresh private draft output required.'}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
$timer=[Diagnostics.Stopwatch]::StartNew();$passed=0;$failed=0;$complete=$false;$rows=[Collections.Generic.List[object]]::new()
function Assert-Contract([bool]$Condition,[string]$Message){$rows.Add([ordered]@{status=$(if($Condition){'PASS'}else{'FAIL'});message=$Message});if(-not$Condition){$script:failed++;throw $Message};$script:passed++;Write-Output ('PASS '+$Message)}
$managed='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed'
$umm=Join-Path $managed 'UnityModManager'
$product=Join-Path $RepositoryRoot 'bin/Release/KingmakerMountedCombat.dll'
$manifestPath='C:/Dev/KingmakerMountedCombatLab/artifacts/KingmakerMountedCombat-0.1.0-chunk7b-preview.43-c7b-world43-diagnostic.zip.manifest.json'
$manifest=Get-Content -Raw -LiteralPath $manifestPath|ConvertFrom-Json
$gamePath=Join-Path $managed 'Assembly-CSharp.dll';$gameHash=(Get-FileHash -LiteralPath $gamePath).Hash.ToLowerInvariant();$productHash=(Get-FileHash -LiteralPath $product).Hash.ToLowerInvariant()
$flags=[Reflection.BindingFlags]'Public,NonPublic,Instance,Static,FlattenHierarchy'
try {
 Assert-Contract ($manifest.version-ceq'0.1.0-chunk7b-preview.43'-and$productHash-ceq$manifest.dllSha256) 'read-only compiled service surface equals the frozen preview.43 package identity'
 Assert-Contract ($gameHash-ceq'3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb') 'exact installed Kingmaker 2.1.7b assembly'
 [AppDomain]::CurrentDomain.add_ReflectionOnlyAssemblyResolve({param($sender,$eventArgs)
  $name=($eventArgs.Name-split',')[0]+'.dll'
  foreach($folder in @($managed,$umm,(Join-Path $RepositoryRoot 'bin/Release'))){$candidate=Join-Path $folder $name;if(Test-Path -LiteralPath $candidate){return [Reflection.Assembly]::ReflectionOnlyLoadFrom($candidate)}}
  return [Reflection.Assembly]::ReflectionOnlyLoad($eventArgs.Name)
 })
 $assembly=[Reflection.Assembly]::ReflectionOnlyLoadFrom($product);$game=[Reflection.Assembly]::ReflectionOnlyLoadFrom($gamePath)
 function ProductType([string]$Name){$assembly.GetType('KingmakerMountedCombat.'+$Name,$true)}
 function ExactMethod($Type,[string]$Name,[string]$Return,[int]$Count){$methods=@($Type.GetMethods($flags)|Where-Object{$_.Name-ceq$Name-and$_.GetParameters().Count-eq$Count-and$_.ReturnType.FullName-ceq$Return});Assert-Contract ($methods.Count-eq1) ($Type.Name+'.'+$Name+' has the required existing contract')}
 $removal=ProductType 'Integration.MountedRemovalPreparation'
 ExactMethod $removal 'Assess' 'KingmakerMountedCombat.Integration.RemovalAssessment' 0
 ExactMethod $removal 'Begin' 'System.Boolean' 0
 foreach($name in @('AssessmentCount','RefusalCount','CleanupSaveCount')){Assert-Contract ($removal.GetProperty($name,$flags).PropertyType.FullName-ceq'System.Int32') ('actual removal metric '+$name)}
 Assert-Contract ($removal.GetProperty('LastAssessment',$flags).PropertyType.FullName-ceq'KingmakerMountedCombat.Integration.RemovalAssessment') 'actual latest removal assessment is observable'
 $assessment=ProductType 'Integration.RemovalAssessment';Assert-Contract ($assessment.GetField('PermanentReferences',$flags).FieldType.FullName.StartsWith('System.Collections.Generic.List`1[[System.String,',[StringComparison]::Ordinal)) 'actual permanent reference findings remain observable strings'
 $world=ProductType 'Integration.MountedWorldService';$event=$world.GetEvent('PairDismounting',$flags)
 Assert-Contract ($event.EventHandlerType.GetGenericArguments()[0].FullName-ceq'KingmakerMountedCombat.Integration.GameMountedRelationshipService'-and$event.EventHandlerType.GetGenericArguments()[1].FullName-ceq'KingmakerMountedCombat.Domain.CleanupTrigger') 'actual per-pair cleanup visitation event exposes exact relationship and trigger'
 $relationship=ProductType 'Integration.GameMountedRelationshipService';Assert-Contract ($relationship.GetProperty('LastTransition',$flags).PropertyType.FullName-ceq'KingmakerMountedCombat.Domain.TransitionResult') 'actual transition result is readable after each registered callback'
 foreach($name in @('PairOwner','Runtime','State')){Assert-Contract ($null-ne$relationship.GetProperty($name,$flags)) ('actual relationship '+$name+' remains readable')}
 $transition=ProductType 'Domain.TransitionResult';foreach($name in @('Succeeded','State','Trigger','Errors','MovementAuthorityResidual','PresentationResidual')){Assert-Contract ($null-ne$transition.GetProperty($name,$flags)) ('actual transition '+$name+' remains readable')}
 $controls=ProductType 'Integration.NativeMountedControlService'
 ExactMethod $controls 'CaptureSnapshot' 'KingmakerMountedCombat.Integration.NativeMountedControlSnapshot' 0
 ExactMethod $controls 'CapturePersistentSlots' 'KingmakerMountedCombat.Integration.SavedMountedSlot[]' 0
 ExactMethod $controls 'HasUnsettledShellFor' 'System.Boolean' 1
 ExactMethod (ProductType 'Diagnostics.NativeActorAllocationTrace') 'ObjectIdentity' 'System.Int32' 1
 ExactMethod (ProductType 'Diagnostics.NativeActorAllocationTrace') 'GrantCount' 'System.Int32' 1
 $gameType=$game.GetType('Kingmaker.Game',$true);$pause=$gameType.GetProperty('IsPaused',$flags)
 Assert-Contract ($pause.PropertyType.FullName-ceq'System.Boolean'-and$null-ne$pause.GetSetMethod($true)) 'native pause surface used by the existing diagnostic remains an actual bool property'
 $unit=$game.GetType('Kingmaker.EntitySystem.Entities.UnitEntityData',$true);Assert-Contract ($unit.GetProperty('IsInCombat',$flags).PropertyType.FullName-ceq'System.Boolean') 'passive unrelated/target in-combat observation uses the actual unit contract'
 $ummAssembly=[Reflection.Assembly]::ReflectionOnlyLoadFrom((Join-Path $umm 'UnityModManager.dll'));$entry=$ummAssembly.GetType('UnityModManagerNet.UnityModManager+ModEntry',$true);$toggle=$entry.GetField('OnToggle',$flags)
 Assert-Contract ($toggle.FieldType.GetGenericTypeDefinition().FullName-ceq'System.Func`3'-and$toggle.FieldType.GetGenericArguments()[0].FullName-ceq$entry.FullName-and$toggle.FieldType.GetGenericArguments()[1].FullName-ceq'System.Boolean'-and$toggle.FieldType.GetGenericArguments()[2].FullName-ceq'System.Boolean') 'actual registered UMM OnToggle callback remains ModEntry,bool -> bool'
 Assert-Contract ((Get-FileHash -LiteralPath $product).Hash.ToLowerInvariant()-ceq$productHash) 'read-only product bytes stayed fixed during metadata inspection'
 $complete=$true
} finally {
 if(-not$complete-and$failed-eq0){$failed++;$rows.Add(@{status='FAIL';message='Metadata inspection terminated before all selected contracts completed.'})}
 [ordered]@{status=$(if($complete-and$failed-eq0-and$passed-gt0){'PASS'}else{'FAIL'});passed=$passed;failed=$failed;elapsedSeconds=$timer.Elapsed.TotalSeconds;productSha256=$productHash;packageManifestSha256=(Get-FileHash -LiteralPath $manifestPath).Hash.ToLowerInvariant();gameAssemblySha256=$gameHash;nativeInvoked=$false;reflectionOnly=$true;trackedSourceEdited=$false;checks=@($rows);limits='Metadata presence/type validation only against the frozen compiled preview.43 service surface and exact installed references. No draft product compilation, native callbacks, game or runtime qualification.'}|ConvertTo-Json -Depth 10|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json')
}
