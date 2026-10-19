[CmdletBinding()]
param([string]$RepositoryRoot,[string]$DiagnosticSourceDirectory,[string]$TestSource,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/schema2-resave-contract-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
if(-not$DiagnosticSourceDirectory){$DiagnosticSourceDirectory=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics'}
if(-not$TestSource){$TestSource=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/Schema2ResaveContractTests.cs'}
function Method([string]$Text,[string]$Signature){$s=$Text.IndexOf($Signature,[StringComparison]::Ordinal);if($s-lt0){throw 'Actual continuity method absent'};$o=$Text.IndexOf('{',$s);$d=1;$e=$o+1;while($d-gt0-and$e-lt$Text.Length){if($Text[$e]-ceq'{'){$d++}elseif($Text[$e]-ceq'}'){$d--};$e++};if($d-ne0){throw 'Unbalanced continuity method'};$Text.Substring($s,$e-$s)}
$source=Join-Path $DiagnosticSourceDirectory 'Chunk7PersistenceScenario.Schema2.cs';$original=[IO.File]::ReadAllText($source)
$projectionSource=Join-Path $DiagnosticSourceDirectory 'Chunk7PersistenceScenario.Schema2Derivative.cs';$projection=[IO.File]::ReadAllText($projectionSource)
$actual=Join-Path $OutputDirectory 'ActualSchema2Continuity.cs'
$body='using System; using System.Linq; using KingmakerMountedCombat.Integration; using Newtonsoft.Json.Linq; namespace KingmakerMountedCombat.Diagnostics { internal sealed partial class Chunk7PersistenceScenario { private static void Require(bool value,string message) { if(!value) throw new InvalidOperationException(message); }'+[Environment]::NewLine+(Method $original '        private static void NormalizeSchema2Collections(')+[Environment]::NewLine+(Method $original '        internal static void ValidateSchema2Continuity(')+[Environment]::NewLine+(Method $projection '        internal static MountedSaveData ProjectOwnedPendingSchema2(')+[Environment]::NewLine+'} }'
[IO.File]::WriteAllText($actual,$body,[Text.UTF8Encoding]::new($false))
$relative=@('src/KingmakerMountedCombat/Integration/MountedSaveData.cs','src/KingmakerMountedCombat/Integration/SavedCombatData.cs','src/KingmakerMountedCombat/Domain/PairedActivationSnapshot.cs','src/KingmakerMountedCombat/Integration/MountedLoadAdmissionPolicy.cs','src/KingmakerMountedCombat/Integration/MountedArchiveData.cs','src/KingmakerMountedCombat/Integration/MountedArchiveCodec.cs','src/KingmakerMountedCombat/Integration/NativeMountedSaveFormat.cs','src/KingmakerMountedCombat/Integration/RetainedNativeReentryPolicy.cs','src/KingmakerMountedCombat/Domain/DeclaredSmallRiderBody.cs','src/KingmakerMountedCombat/Domain/MountedRiderProfileCatalog.cs','src/KingmakerMountedCombat/Domain/MountedRiderPoseProfile.cs','src/KingmakerMountedCombat/Domain/PoseVector3.cs','tests/Chunk7PersistenceTests.cs')
$paths=@($relative|ForEach-Object{Join-Path $RepositoryRoot $_})+@($TestSource)
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$json='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll'
$exe=Join-Path $OutputDirectory 'Schema2ResaveTests.exe'
&$compiler /nologo /target:exe /langversion:7.3 /optimize+ /deterministic+ (('/out:')+$exe) (('/reference:')+$json) $actual @paths
$compileExit=$LASTEXITCODE;$testExit=$null;$output=@()
if($compileExit-eq0){Copy-Item -LiteralPath $json -Destination (Join-Path $OutputDirectory 'Newtonsoft.Json.dll');$output=@(&$exe (Join-Path ([IO.Path]::GetDirectoryName($json)) 'UnityEngine.dll') 2>&1);$testExit=$LASTEXITCODE;$output|ForEach-Object{Write-Host $_}}
$receipt=[ordered]@{schemaVersion=1;sourceSha256=(Get-FileHash -LiteralPath $source).Hash.ToLowerInvariant();projectionSha256=(Get-FileHash -LiteralPath $projectionSource).Hash.ToLowerInvariant();actualSha256=(Get-FileHash -LiteralPath $actual).Hash.ToLowerInvariant();inputs=@($paths|ForEach-Object{@{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash.ToLowerInvariant()}});compileExit=$compileExit;testExit=$testExit;pass=@($output|Where-Object{$_-cmatch'^PASS '}).Count;fail=@($output|Where-Object{$_-cmatch'^FAIL '}).Count;nativeInvoked=$false;archiveRead=$false;unityDependencySha256=(Get-FileHash -LiteralPath (Join-Path ([IO.Path]::GetDirectoryName($json)) 'UnityEngine.dll')).Hash.ToLowerInvariant();limits='Actual production graph-continuity method with real DTO validators. No Unity save/callback/worker delivery is claimed.'}
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'receipt.json'),($receipt|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
if($compileExit-ne0-or$testExit-ne0){throw 'Schema2 resave contract failed; preserve receipt.'}
