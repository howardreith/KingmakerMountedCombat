[CmdletBinding()]
param([string]$RepositoryRoot,[string]$DiagnosticSourceDirectory,[string]$TestSource,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$DiagnosticSourceDirectory){$DiagnosticSourceDirectory=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics'}
if(-not$TestSource){$TestSource=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/OwnedSchema1ContractTests.cs'}
$analysis=[IO.Path]::GetFullPath((Join-Path $RepositoryRoot '../../analysis-cache'))
if(-not$OutputDirectory){$OutputDirectory=Join-Path $analysis ('chunk7-continuation/owned-schema1-native-'+[Guid]::NewGuid().ToString('N'))}
$OutputDirectory=[IO.Path]::GetFullPath($OutputDirectory)
if(-not$OutputDirectory.StartsWith($analysis+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)-or(Test-Path -LiteralPath $OutputDirectory)){throw 'Fresh analysis-cache output required.'}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
function Get-OwnedSchema1Method([string]$Text,[string]$Signature){$s=$Text.IndexOf($Signature,[StringComparison]::Ordinal);if($s-lt0-or$Text.LastIndexOf($Signature,[StringComparison]::Ordinal)-ne$s){throw 'Actual owned schema1 guard signature is absent or ambiguous.'};$o=$Text.IndexOf('{',$s);$d=1;$e=$o+1;while($d-gt0-and$e-lt$Text.Length){if($Text[$e]-ceq'{'){$d++}elseif($Text[$e]-ceq'}'){$d--};$e++};if($d-ne0){throw 'Unbalanced actual guard.'};return $Text.Substring($s,$e-$s)}
$source=Join-Path $DiagnosticSourceDirectory 'Chunk7PersistenceScenario.Schema1.cs';$text=[IO.File]::ReadAllText($source)
$methods=@('        internal static void ValidateOwnedSchema1Field(','        internal static void ValidateOwnedSchema1Archive(','        internal static void ValidateOwnedSchema1PreservedField(','        internal sealed class OwnedSchema1HotbarObservation')
$actual=Join-Path $OutputDirectory 'ActualOwnedSchema1Guards.cs'
$body='using System; using System.Linq; using KingmakerMountedCombat.Integration; using Newtonsoft.Json.Linq; namespace KingmakerMountedCombat.Diagnostics { internal sealed partial class Chunk7PersistenceScenario { private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}'+[Environment]::NewLine+(@($methods|ForEach-Object{Get-OwnedSchema1Method $text $_})-join[Environment]::NewLine)+[Environment]::NewLine+'} internal static class SupportedMountedProfiles { internal static readonly Profile Horse=new Profile(); internal sealed class Profile { internal string Id => "medium-humanoid-horse-v1"; } } }'
[IO.File]::WriteAllText($actual,$body,[Text.UTF8Encoding]::new($false))
$relative=@('src/KingmakerMountedCombat/Integration/MountedSaveData.cs','src/KingmakerMountedCombat/Integration/SavedCombatData.cs','src/KingmakerMountedCombat/Domain/PairedActivationSnapshot.cs','src/KingmakerMountedCombat/Integration/MountedLoadAdmissionPolicy.cs','src/KingmakerMountedCombat/Integration/MountedArchiveData.cs','src/KingmakerMountedCombat/Integration/MountedArchiveCodec.cs','src/KingmakerMountedCombat/Integration/NativeMountedSaveFormat.cs','src/KingmakerMountedCombat/Domain/DeclaredSmallRiderBody.cs','src/KingmakerMountedCombat/Domain/MountedRiderProfileCatalog.cs','src/KingmakerMountedCombat/Domain/MountedRiderPoseProfile.cs','src/KingmakerMountedCombat/Domain/PoseVector3.cs')
$paths=@($relative|ForEach-Object{Join-Path $RepositoryRoot $_})+@($TestSource)
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$managed='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed';$json=Join-Path $managed 'Newtonsoft.Json.dll';$unity=Join-Path $managed 'UnityEngine.dll'
$framework='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7'
$exe=Join-Path $OutputDirectory 'OwnedSchema1Tests.exe';$rsp=Join-Path $OutputDirectory 'compile.rsp';$timer=[Diagnostics.Stopwatch]::StartNew()
$compilerArguments=@('/nologo','/target:exe','/langversion:7.3','/optimize+','/deterministic+','/nostdlib+',('/out:'+$exe),('/reference:'+$json))
$compilerArguments+=@('mscorlib.dll','System.dll','System.Core.dll')|ForEach-Object{'/reference:'+(Join-Path $framework $_)}
[IO.File]::WriteAllLines($rsp,@(($compilerArguments+@($actual)+$paths)|ForEach-Object{'"'+$_+'"'}),[Text.UTF8Encoding]::new($false))
$output=@(&$compiler /noconfig ('@'+$rsp) 2>&1);$compileExit=$LASTEXITCODE;$output|Set-Content -LiteralPath (Join-Path $OutputDirectory 'compile.log') -Encoding UTF8
$testExit=$null;$test=@();if($compileExit-eq0){Copy-Item -LiteralPath $json -Destination (Join-Path $OutputDirectory 'Newtonsoft.Json.dll');$test=@(&$exe $unity 2>&1);$testExit=$LASTEXITCODE;$test|Set-Content -LiteralPath (Join-Path $OutputDirectory 'test.log') -Encoding UTF8;$test|ForEach-Object{Write-Output $_}}
$passed=@($test|Where-Object{$_-cmatch'^PASS '}).Count;$failed=@($test|Where-Object{$_-cmatch'^FAIL '}).Count
$ok=$compileExit-eq0-and$testExit-eq0-and$passed-gt0-and$failed-eq0
$receipt=[ordered]@{schemaVersion=1;status=$(if($ok){'PASS'}else{'FAIL'});passed=$passed;failed=$failed;compileExit=$compileExit;testExit=$testExit;elapsedSeconds=$timer.Elapsed.TotalSeconds;sourceSha256=(Get-FileHash -LiteralPath $source).Hash.ToLowerInvariant();actualSha256=(Get-FileHash -LiteralPath $actual).Hash.ToLowerInvariant();inputs=@(($paths+@($json,$unity))|ForEach-Object{[ordered]@{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash.ToLowerInvariant()}});nativeInvoked=$false;archivesRead=0;limits='Three exact production guard methods, real DTO validators/historical decoder/default writer. The Horse profile ID is the sole constant facade; no Game, load/save worker or cleanup delivery is executed.'}
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'receipt.json'),($receipt|ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false))
if(-not$ok){throw 'Owned schema-1 actual guard regression failed; preserve outputs.'}
