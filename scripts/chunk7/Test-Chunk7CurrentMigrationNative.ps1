[CmdletBinding()]
param([string]$RepositoryRoot,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
$analysis=[IO.Path]::GetFullPath((Join-Path $RepositoryRoot '../../analysis-cache'))
if(-not$OutputDirectory){$OutputDirectory=Join-Path $analysis ('chunk7-continuation/current-migration-native-'+[Guid]::NewGuid().ToString('N'))}
$OutputDirectory=[IO.Path]::GetFullPath($OutputDirectory)
if(-not$OutputDirectory.StartsWith($analysis+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)-or(Test-Path -LiteralPath $OutputDirectory)){throw 'Fresh analysis-cache output required.'}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
function Get-CurrentMigrationMethod([string]$Text,[string]$Signature){
 $s=$Text.IndexOf($Signature,[StringComparison]::Ordinal);if($s-lt0-or$Text.LastIndexOf($Signature,[StringComparison]::Ordinal)-ne$s){throw 'Actual migration guard is absent or ambiguous.'}
 $o=$Text.IndexOf('{',$s);$depth=1;$end=$o+1
 while($depth-gt0-and$end-lt$Text.Length){if($Text[$end]-ceq'{'){$depth++}elseif($Text[$end]-ceq'}'){$depth--};$end++}
 if($depth-ne0){throw 'Unbalanced actual guard.'};$Text.Substring($s,$end-$s)
}
$diagnostics=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics'
$sourcePaths=@('Chunk7PersistenceScenario.CurrentMigration.cs','Chunk7PersistenceScenario.Schema1.cs','Chunk7PersistenceScenario.Schema2.cs','Chunk7PersistenceScenario.cs')|ForEach-Object{Join-Path $diagnostics $_}
$current,$schema1,$schema2,$scenario=@($sourcePaths|ForEach-Object{[IO.File]::ReadAllText($_)})
$methods=@((Get-CurrentMigrationMethod $current '        internal static JObject ValidateCurrentMigrationParent('),
 (Get-CurrentMigrationMethod $schema1 '        internal static void ValidateOwnedSchema1Archive('),
 (Get-CurrentMigrationMethod $schema2 '        private static void NormalizeSchema2Collections('),
 (Get-CurrentMigrationMethod $schema2 '        internal static void ValidateSchema2Continuity('))
$constants=@()
foreach($spec in @(@($schema1,'OwnedSchema1Cold'),@($schema1,'Schema1OutputName'),@($schema2,'Schema2OutputName'),@($scenario,'EvidenceKind'))){
 $m=[regex]::Matches($spec[0],('(?m)^\s*internal const string '+$spec[1]+'\s*=[^\r\n]+;'));if($m.Count-ne1){throw 'Exact production constant absent or ambiguous.'};$constants+=$m[0].Value
}
$actual=Join-Path $OutputDirectory 'ActualCurrentMigrationGuards.cs'
$body='using System; using System.IO; using System.Linq; using System.Text.RegularExpressions; using KingmakerMountedCombat.Integration; using Newtonsoft.Json.Linq; namespace KingmakerMountedCombat.Diagnostics { internal sealed partial class Chunk7PersistenceScenario { private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}'+[Environment]::NewLine+(($constants+$methods)-join[Environment]::NewLine)+[Environment]::NewLine+'} }'
[IO.File]::WriteAllText($actual,$body,[Text.UTF8Encoding]::new($false))
$relative=@('src/KingmakerMountedCombat/Integration/MountedSaveData.cs','src/KingmakerMountedCombat/Integration/SavedCombatData.cs','src/KingmakerMountedCombat/Domain/PairedActivationSnapshot.cs','src/KingmakerMountedCombat/Integration/MountedLoadAdmissionPolicy.cs','src/KingmakerMountedCombat/Integration/MountedArchiveData.cs','src/KingmakerMountedCombat/Integration/MountedArchiveCodec.cs','src/KingmakerMountedCombat/Integration/NativeMountedSaveFormat.cs','src/KingmakerMountedCombat/Integration/RetainedNativeReentryPolicy.cs','src/KingmakerMountedCombat/Domain/DeclaredSmallRiderBody.cs','src/KingmakerMountedCombat/Domain/MountedRiderProfileCatalog.cs','src/KingmakerMountedCombat/Domain/MountedRiderPoseProfile.cs','src/KingmakerMountedCombat/Domain/PoseVector3.cs','tests/Chunk7PersistenceTests.cs','tests/Chunk7Diagnostics/CurrentMigrationContractTests.cs')
$paths=@($relative|ForEach-Object{Join-Path $RepositoryRoot $_})
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$managed='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed';$json=Join-Path $managed 'Newtonsoft.Json.dll';$unity=Join-Path $managed 'UnityEngine.dll'
$framework='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7'
$exe=Join-Path $OutputDirectory 'CurrentMigrationTests.exe';$rsp=Join-Path $OutputDirectory 'compile.rsp';$timer=[Diagnostics.Stopwatch]::StartNew()
$compilerArguments=@('/nologo','/target:exe','/main:CurrentMigrationContractTests','/langversion:7.3','/optimize+','/deterministic+','/nostdlib+',('/out:'+$exe),('/reference:'+$json))
$compilerArguments+=@('mscorlib.dll','System.dll','System.Core.dll')|ForEach-Object{'/reference:'+(Join-Path $framework $_)}
[IO.File]::WriteAllLines($rsp,@(($compilerArguments+@($actual)+$paths)|ForEach-Object{'"'+$_+'"'}),[Text.UTF8Encoding]::new($false))
$output=@(&$compiler /noconfig ('@'+$rsp) 2>&1);$compileExit=$LASTEXITCODE;$output|Set-Content -LiteralPath (Join-Path $OutputDirectory 'compile.log') -Encoding UTF8
$testExit=$null;$test=@();if($compileExit-eq0){Copy-Item -LiteralPath $json -Destination (Join-Path $OutputDirectory 'Newtonsoft.Json.dll');$test=@(&$exe $unity 2>&1);$testExit=$LASTEXITCODE;$test|Set-Content -LiteralPath (Join-Path $OutputDirectory 'test.log') -Encoding UTF8;$test|ForEach-Object{Write-Output $_}}
$passed=@($test|Where-Object{$_-cmatch'^PASS '}).Count;$failed=@($test|Where-Object{$_-cmatch'^FAIL '}).Count
$ok=$compileExit-eq0-and$testExit-eq0-and$passed-gt0-and$failed-eq0
$receipt=[ordered]@{status=$(if($ok){'PASS'}else{'FAIL'});passed=$passed;failed=$failed;compileExit=$compileExit;testExit=$testExit;elapsedSeconds=$timer.Elapsed.TotalSeconds;actualSha256=(Get-FileHash -LiteralPath $actual).Hash.ToLowerInvariant();inputs=@(($sourcePaths+$paths+@($json,$unity))|ForEach-Object{[ordered]@{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash.ToLowerInvariant()}});nativeInvoked=$false;archivesRead=0;limits='Actual parent, schema1 representability and schema2 continuity guards with real archive/historical codecs. Typed request/profile facades only; no native loader, save, callback or world delivery.'}
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'receipt.json'),($receipt|ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false))
if(-not$ok){$output|Write-Output;throw 'Current-format migration actual guard regression failed; preserve outputs.'}
