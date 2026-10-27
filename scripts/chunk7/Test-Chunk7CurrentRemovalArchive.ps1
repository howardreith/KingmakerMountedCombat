[CmdletBinding()]
param([string]$RepositoryRoot='C:/Dev/KingmakerMountedCombatLab/repo/KingmakerMountedCombat',[string]$DraftRoot,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$DraftRoot){$DraftRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
$private=[IO.Path]::GetFullPath((Join-Path $DraftRoot '..'))
if(-not$OutputDirectory){$OutputDirectory=Join-Path $private ('checks/current-archive-'+[Guid]::NewGuid().ToString('N'))}
$OutputDirectory=[IO.Path]::GetFullPath($OutputDirectory)
if(-not$OutputDirectory.StartsWith($private+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)-or(Test-Path -LiteralPath $OutputDirectory)){throw 'Fresh private output directory required.'}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
function Block([string]$Text,[string]$Signature){$s=$Text.IndexOf($Signature,[StringComparison]::Ordinal);if($s-lt0-or$s-ne$Text.LastIndexOf($Signature,[StringComparison]::Ordinal)){throw 'Actual guard/nested diagnostic projection absent or ambiguous.'};$o=$Text.IndexOf('{',$s);$d=1;$e=$o+1;while($d-gt0-and$e-lt$Text.Length){if($Text[$e]-ceq'{'){$d++}elseif($Text[$e]-ceq'}'){$d--};$e++};if($d-ne0){throw 'Unbalanced actual source block.'};$Text.Substring($s,$e-$s)}
$source=Join-Path $DraftRoot 'src/KingmakerMountedCombat/Diagnostics/RuntimePersistenceRemovalScenario.CurrentArchive.cs'
$text=[IO.File]::ReadAllText($source);$sourceHash=(Get-FileHash -LiteralPath $source).Hash.ToLowerInvariant()
$actual=Join-Path $OutputDirectory 'ActualCurrentRemovalArchive.cs'
$body='using System;using KingmakerMountedCombat.Integration;namespace KingmakerMountedCombat.Diagnostics { internal sealed partial class RuntimePersistenceScenario {'+[Environment]::NewLine+(Block $text 'internal sealed class RemovalArchiveObservation')+[Environment]::NewLine+(Block $text 'internal static RemovalArchiveObservation ValidateCurrentRemovalArchive(')+[Environment]::NewLine+'}}'
[IO.File]::WriteAllText($actual,$body,[Text.UTF8Encoding]::new($false))
$relative=@('src/KingmakerMountedCombat/Integration/MountedSaveData.cs','src/KingmakerMountedCombat/Integration/SavedCombatData.cs','src/KingmakerMountedCombat/Domain/PairedActivationSnapshot.cs','src/KingmakerMountedCombat/Integration/MountedLoadAdmissionPolicy.cs','src/KingmakerMountedCombat/Integration/MountedArchiveData.cs','src/KingmakerMountedCombat/Integration/MountedArchiveCodec.cs','src/KingmakerMountedCombat/Integration/NativeMountedSaveFormat.cs','src/KingmakerMountedCombat/Domain/DeclaredSmallRiderBody.cs','src/KingmakerMountedCombat/Domain/MountedRiderProfileCatalog.cs','src/KingmakerMountedCombat/Domain/MountedRiderPoseProfile.cs','src/KingmakerMountedCombat/Domain/PoseVector3.cs')
$test=Join-Path $DraftRoot 'tests/Chunk7Diagnostics/CurrentRemovalArchiveContractTests.cs';$paths=@($relative|ForEach-Object{Join-Path $RepositoryRoot $_})+@($test)
$pins=@(@($paths)+@($source)|ForEach-Object{@{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash.ToLowerInvariant()}})
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe';$managed='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed';$json=Join-Path $managed 'Newtonsoft.Json.dll';$unity=Join-Path $managed 'UnityEngine.dll'
$framework='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7';$exe=Join-Path $OutputDirectory 'CurrentRemovalArchiveTests.exe';$rsp=Join-Path $OutputDirectory 'compile.rsp'
$arguments=@('/nologo','/target:exe','/langversion:7.3','/warnaserror+','/optimize+','/deterministic+','/nostdlib+',('/out:'+$exe),('/reference:'+$json))+@('mscorlib.dll','System.dll','System.Core.dll'|ForEach-Object{'/reference:'+(Join-Path $framework $_)})
[IO.File]::WriteAllLines($rsp,@(($arguments+@($actual)+$paths)|ForEach-Object{'"'+$_+'"'}),[Text.UTF8Encoding]::new($false));$timer=[Diagnostics.Stopwatch]::StartNew()
$compile=@(&$compiler /noconfig ('@'+$rsp) 2>&1);$compileExit=$LASTEXITCODE;$compile|Set-Content -LiteralPath (Join-Path $OutputDirectory 'compile.log')
$testExit=$null;$output=@()
if($compileExit-eq0){
 [void][Reflection.Assembly]::LoadFrom($json);$assembly=[Reflection.Assembly]::LoadFrom($exe);$capture=New-Object IO.StringWriter;$previous=[Console]::Out
 $invokeArguments=[object[]]::new(1);$invokeArguments[0]=[string[]]@($unity)
 try{[Console]::SetOut($capture);$testExit=[int]$assembly.EntryPoint.Invoke($null,$invokeArguments)}finally{[Console]::SetOut($previous)}
 $output=@($capture.ToString()-split'\r?\n'|Where-Object{$_-ne''});$output|Set-Content -LiteralPath (Join-Path $OutputDirectory 'test.log');$output|ForEach-Object{Write-Output $_}
}
$passed=@($output|Where-Object{$_-cmatch'^PASS '}).Count;$failed=@($output|Where-Object{$_-cmatch'^FAIL '}).Count
$inputsFixed=@($pins|Where-Object{(Get-FileHash -LiteralPath $_.path).Hash.ToLowerInvariant()-cne$_.sha256}).Count-eq0
$ok=$compileExit-eq0-and$testExit-eq0-and$passed-gt0-and$failed-eq0-and$inputsFixed
[ordered]@{status=$(if($ok){'PASS'}else{'FAIL'});passed=$passed;failed=$failed;elapsedSeconds=$timer.Elapsed.TotalSeconds;sourceSha256=$sourceHash;actualSha256=(Get-FileHash -LiteralPath $actual).Hash.ToLowerInvariant();inputs=$pins;inputsFixed=$inputsFixed;compileExit=$compileExit;testExit=$testExit;jsonSha256=(Get-FileHash -LiteralPath $json).Hash.ToLowerInvariant();nativeInvoked=$false;trackedSourceEdited=$false;actualSaveValidators=$true;actualArchiveCodec=$true;limits='Exact original private static helper and nested observation plus actual source DTO/codec/validation classes. Only generated in-memory graphs are encoded/decoded. Native ISaver storage, game callbacks, cleanup delivery, full product compile and runtime qualification are not exercised.'}|ConvertTo-Json -Depth 10|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json')
if(-not$ok){$compile|Write-Output;throw 'Actual current removal archive guard check failed; preserve outputs.'}
