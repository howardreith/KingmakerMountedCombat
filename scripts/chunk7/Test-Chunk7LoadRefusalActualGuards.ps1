[CmdletBinding()]
param([string]$RepositoryRoot,[string]$DiagnosticSourceDirectory,[string]$TestSource,[string]$OutputDirectory,[ValidateSet('Refusal','ExistingAuthority')][string]$EntryPoint='Refusal')
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('DiagnosticSourceDirectory')){$DiagnosticSourceDirectory=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics'}
if(-not$PSBoundParameters.ContainsKey('TestSource')){$TestSource=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/OwnedPairsAuthorityTests.cs'}
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/owned-pairs-authority-'+[Guid]::NewGuid().ToString('N'))}
$OutputDirectory=[IO.Path]::GetFullPath($OutputDirectory)
$analysis=[IO.Path]::GetFullPath((Join-Path $RepositoryRoot '../../analysis-cache'))
if(-not$OutputDirectory.StartsWith($analysis+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)-or(Test-Path -LiteralPath $OutputDirectory)){throw 'Fresh private analysis-cache output required.'}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
function Get-BootstrapMethod([string]$Text,[string]$Signature){
 $start=$Text.IndexOf($Signature,[StringComparison]::Ordinal);if($start-lt0-or$start-ne$Text.LastIndexOf($Signature,[StringComparison]::Ordinal)){throw ('Missing or ambiguous actual boundary: '+$Signature)}
 $pattern=@'
//[^\r\n]*|/\*[\s\S]*?\*/|@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|'(?:\\.|[^'\\])*'|[{}]
'@
 $depth=0;$opened=$false
 foreach($match in [regex]::Matches($Text.Substring($start),$pattern)){
  if($match.Value-ceq'{'){$depth++;$opened=$true}elseif($match.Value-ceq'}'){$depth--;if($opened-and$depth-eq0){return $Text.Substring($start,$match.Index+1)}}
 }
 throw 'Unbalanced actual C# method.'
}
$authorityPath=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics/PersistenceSaveAuthorization.cs';$authority=[IO.File]::ReadAllText($authorityPath)
# Actual constructor, admission, quota, window and identity logic is unchanged.
# Only OS file/directory boundaries are replaced; no archive exists or is read.
$directory=Get-BootstrapMethod $authority '        private static void RequireDirectory(string path)'
$hash=Get-BootstrapMethod $authority '        private static string ReadOwnedHash(string path)'
$authority=$authority.Replace($directory,'        private static void RequireDirectory(string path) { KingmakerMountedCombat.Tests.BootstrapVirtualStorage.RequireDirectory(path); }').Replace($hash,'        private static string ReadOwnedHash(string path) { return KingmakerMountedCombat.Tests.BootstrapVirtualStorage.Hash(path); }')
$authority='using Directory = KingmakerMountedCombat.Tests.BootstrapVirtualDirectory;'+[Environment]::NewLine+'using File = KingmakerMountedCombat.Tests.BootstrapVirtualFile;'+[Environment]::NewLine+$authority
$actual=Join-Path $OutputDirectory 'ActualPersistenceSaveAuthorization.cs';[IO.File]::WriteAllText($actual,$authority,[Text.UTF8Encoding]::new($false))
[xml]$project=Get-Content -Raw -LiteralPath (Join-Path $RepositoryRoot 'tests/KingmakerMountedCombat.Tests.csproj')
$sources=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$overrides=@('RuntimeProtocol.cs','RuntimeSaveAuthorization.cs','Chunk7OrdinaryAcquisitionSelection.cs','Chunk7OrdinaryFieldSelection.cs','Chunk7SmallCalibrationSelection.cs','Chunk7ProgressionSelection.cs','Chunk7NativeBootstrapSelection.cs','Chunk7BootstrapInventorySelection.cs','Chunk7ManualSeedSelection.cs','Chunk7ManualDerivedSelection.cs','Chunk7AcquiredManualSelection.cs','Chunk7AssistedFixtureSelection.cs','Chunk7AssistedColdInventorySelection.cs','Chunk7CumulativeAcquisitionSelection.cs','Chunk7OwnedPairsSelection.cs','PersistenceSaveAuthorization.AssistedFixture.cs','PersistenceSaveAuthorization.AssistedColdInventory.cs')
foreach($node in $project.SelectNodes("//*[local-name()='Compile']")){
 $path=[IO.Path]::GetFullPath((Join-Path (Join-Path $RepositoryRoot 'tests') $node.Include));$leaf=Split-Path -Leaf $path
 if($leaf-ceq'PersistenceSaveAuthorization.OwnedPairs.cs'){continue}
 if($leaf-ceq'PersistenceSaveAuthorization.cs'){$path=$actual}elseif($leaf-cin$overrides){$candidate=Join-Path $DiagnosticSourceDirectory $leaf;if(Test-Path -LiteralPath $candidate){$path=$candidate}}
 [void]$sources.Add($path)
}
foreach($leaf in $overrides){$path=Join-Path $DiagnosticSourceDirectory $leaf;if(-not(Test-Path -LiteralPath $path)){$path=Join-Path $RepositoryRoot ('src/KingmakerMountedCombat/Diagnostics/'+$leaf)};[void]$sources.Add($path)}
$partialPath=Join-Path $DiagnosticSourceDirectory 'PersistenceSaveAuthorization.OwnedPairs.cs'
$partial='using Directory = KingmakerMountedCombat.Tests.BootstrapVirtualDirectory;'+[Environment]::NewLine+'using File = KingmakerMountedCombat.Tests.BootstrapVirtualFile;'+[Environment]::NewLine+[IO.File]::ReadAllText($partialPath)
$partialActual=Join-Path $OutputDirectory 'ActualOwnedPairsAuthorization.cs';[IO.File]::WriteAllText($partialActual,$partial,[Text.UTF8Encoding]::new($false));[void]$sources.Add($partialActual)
[void]$sources.Add($TestSource)
$draftRoot=[IO.Path]::GetFullPath((Join-Path $DiagnosticSourceDirectory '../../..'))
$guardPath=Join-Path $DiagnosticSourceDirectory 'Chunk7PersistenceScenario.LoadRefusal.cs';$guard=[IO.File]::ReadAllText($guardPath)
$actualGuards=Join-Path $OutputDirectory 'ActualLoadRefusalObservationGuards.cs'
$guardMethods=@('internal static void ValidateLoadRefusalPreservedState(','internal static void ValidateLoadRefusalDerivation(','private static string LoadRefusalTextHash(')|ForEach-Object{Get-BootstrapMethod $guard $_}
$descriptor=$guard.Substring($guard.IndexOf('private static JObject LoadRefusalDescriptor('));$descriptor=$descriptor.Substring(0,$descriptor.IndexOf('};')+2)
$body='using System;using System.IO;using System.Linq;using System.Text;using System.Security.Cryptography;using KingmakerMountedCombat.Integration;using Newtonsoft.Json.Linq;namespace KingmakerMountedCombat.Diagnostics{internal sealed partial class Chunk7PersistenceScenario {private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}'+($guardMethods-join[Environment]::NewLine)+$descriptor+'}}'
[IO.File]::WriteAllText($actualGuards,$body,[Text.UTF8Encoding]::new($false));[void]$sources.Add($actualGuards)
$custodyPath=Join-Path $DiagnosticSourceDirectory 'Chunk7OwnedPairFixture.cs'
$custody=[IO.File]::ReadAllText($custodyPath);$bindGuard=Get-BootstrapMethod $custody 'private void RequireBindMode('
$bindBody='using System;namespace KingmakerMountedCombat.Diagnostics{internal sealed class LoadRefusalBindModeAdapter {private readonly RuntimeRequest request;private readonly Chunk7OwnedPairsSelection selection;private bool bound,released;internal LoadRefusalBindModeAdapter(RuntimeRequest value){request=value;selection=value.OwnedPairs;bound=false;released=false;}private static void Require(bool value,string text){if(!value)throw new InvalidOperationException(text);}internal void Check(bool combat){RequireBindMode(combat);}'+$bindGuard+'}}'
$actualBind=Join-Path $OutputDirectory 'ActualLoadRefusalBindModeGuard.cs';[IO.File]::WriteAllText($actualBind,$bindBody,[Text.UTF8Encoding]::new($false));[void]$sources.Add($actualBind)
$patchPath=Join-Path $draftRoot 'src/KingmakerMountedCombat/Integration/MountedPatchController.cs';$patch=[IO.File]::ReadAllText($patchPath)
$prefixes=@((Get-BootstrapMethod $patch 'private static bool GameLoadAdmissionCore('))
foreach($signature in @('internal static bool GameLoadAdmissionPrefix(','internal static bool GameMainMenuLoadAdmissionPrefix(','internal static bool GameSmokeLoadAdmissionPrefix(')){$p=$patch.IndexOf($signature,[StringComparison]::Ordinal);if($p-lt0){throw 'Missing actual native entry wrapper.'};$prefixes+=$patch.Substring($p,$patch.IndexOf(';',$p)-$p+1)}
$template=Join-Path $draftRoot 'tests/Chunk7Diagnostics/LoadRefusalActualGuardTests.cs';$actualTest=Join-Path $OutputDirectory 'ActualLoadRefusalGuardTests.cs'
[IO.File]::WriteAllText($actualTest,([IO.File]::ReadAllText($template).Replace('__ACTUAL_PREFIXES__',($prefixes-join[Environment]::NewLine))),[Text.UTF8Encoding]::new($false));[void]$sources.Add($actualTest)
[void]$sources.Add((Join-Path $DiagnosticSourceDirectory 'RuntimeSaveAuthorization.LoadRefusal.cs'))
foreach($relative in @('src/KingmakerMountedCombat/Integration/MountedSaveData.cs','src/KingmakerMountedCombat/Integration/SavedCombatData.cs','src/KingmakerMountedCombat/Integration/MountedLoadAdmissionPolicy.cs','src/KingmakerMountedCombat/Integration/MountedArchiveData.cs','src/KingmakerMountedCombat/Integration/MountedArchiveCodec.cs','src/KingmakerMountedCombat/Integration/NativeMountedSaveFormat.cs','src/KingmakerMountedCombat/Domain/MountedRiderPoseProfile.cs')){[void]$sources.Add((Join-Path $RepositoryRoot $relative))}
$timer=[Diagnostics.Stopwatch]::StartNew()
$framework='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7';$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$exe=Join-Path $OutputDirectory 'owned-pairs-authority.exe';$rsp=Join-Path $OutputDirectory 'compile.rsp'
$main=if($EntryPoint-ceq'ExistingAuthority'){'KingmakerMountedCombat.Tests.OwnedPairsAuthorityTests'}else{'KingmakerMountedCombat.Tests.LoadRefusalActualGuardTests'}
$arguments=@('/nologo','/target:exe','/langversion:7.3','/warnaserror+','/optimize+','/deterministic+','/nostdlib+',('/main:'+$main),('/out:'+$exe))
$json='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll'
$arguments+=('/reference:'+$json)
$arguments+=@('mscorlib.dll','System.dll','System.Core.dll')|ForEach-Object{'/reference:'+(Join-Path $framework $_)}
[IO.File]::WriteAllLines($rsp,@(($arguments+@($sources))|ForEach-Object{'"'+$_+'"'}),[Text.UTF8Encoding]::new($false))
&$compiler ('@'+$rsp);if($LASTEXITCODE-ne0){throw 'Owned pairs actual-authority compile failed.'}
[void][Reflection.Assembly]::LoadFrom($json);$assembly=[Reflection.Assembly]::LoadFrom($exe)
$capture=New-Object IO.StringWriter;$previous=[Console]::Out;$invoke=[object[]]::new(1)
$invoke[0]=[string[]]@((Join-Path $OutputDirectory 'opaque-file-fixtures'),(Join-Path ([IO.Path]::GetDirectoryName($json)) 'UnityEngine.dll'))
if($assembly.EntryPoint.GetParameters().Length-eq0){$invoke=[object[]]@()}
try{[Console]::SetOut($capture);$testExit=[int]$assembly.EntryPoint.Invoke($null,$invoke)}finally{[Console]::SetOut($previous)}
$output=@($capture.ToString()-split'\r?\n'|Where-Object{$_-ne''});$output|Set-Content -LiteralPath (Join-Path $OutputDirectory 'test.log');$output|ForEach-Object{Write-Output $_}
$passed=@($output|Where-Object{$_-cmatch'^PASS '}).Count;$failed=@($output|Where-Object{$_-cmatch'^FAIL '}).Count
[ordered]@{status=$(if($testExit-eq0-and$failed-eq0-and$passed-gt0){'PASS'}else{'FAIL'});passed=$passed;failed=$failed;testExit=$testExit;elapsedSeconds=$timer.Elapsed.TotalSeconds;actualMethods=$true;actualAuthorization=$true;actualSourceValidators=$true;nativePolicyFacade=$true;storageHashAndDirectoryFacade=$true;actualPrivateFileFacts=$true;nativeInvoked=$false;trackedSourceEdited=$false;inputs=@(@($sources)+@($guardPath,$patchPath,$template,$custodyPath)|ForEach-Object{@{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash.ToLowerInvariant()}});limits='Actual private early-load authorization lease, actual prefix ordering/fence, actual request/authority/bind-mode and static receipt/state guards with real DTO/codec validators. Native Game/policy bodies and archive-hash/directory boundaries are facades. Immutable length/timestamp checks use disposable opaque private files. No native archives/load/refusal/menu/cleanup delivery or full product compile is claimed.'}|ConvertTo-Json -Depth 12|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json')
if($testExit-ne0-or$failed-ne0){throw 'Actual refusal guards failed; preserve private outputs.'}
