[CmdletBinding()]
param([string]$RepositoryRoot,[string]$DiagnosticSourceDirectory,[string]$TemplatePath,[string]$OutputDirectory,[switch]$NativeFlowOnly)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$DiagnosticSourceDirectory){$DiagnosticSourceDirectory=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics'}
if(-not$TemplatePath){$TemplatePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/NativeBootstrapProbeTemplate.cs'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/native-bootstrap-probe-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
function Get-Method([string]$Text,[string]$Signature){$s=$Text.IndexOf($Signature,[StringComparison]::Ordinal);if($s-lt0){throw ('Missing method '+$Signature)};$o=$Text.IndexOf('{',$s);$d=1;$e=$o+1;while($d-gt0-and$e-lt$Text.Length){if($Text[$e]-ceq'{'){$d++}elseif($Text[$e]-ceq'}'){$d--};$e++};if($d-ne0){throw 'Unbalanced method'};$Text.Substring($s,$e-$s)}
$sourcePath=Join-Path $DiagnosticSourceDirectory 'Chunk7NativeBootstrapProbe.cs';$original=[IO.File]::ReadAllText($sourcePath);$body=$original
$body=[regex]::Replace($body,'(?m)^using (?:Harmony12|Kingmaker|Kingmaker\.Blueprints\.Area|Kingmaker\.EntitySystem\.Persistence|UnityEngine);\r?\n','')
$install=Get-Method $body '        internal void Install()';$start=$install.IndexOf('// Native contract validation;',[StringComparison]::Ordinal);$end=$install.IndexOf('            harmony = HarmonyInstance.Create(',[StringComparison]::Ordinal);if($start-lt0-or$end-lt$start){throw 'Metadata boundary changed'}
$body=$body.Replace($install,$install.Substring(0,$start)+'MetadataBoundary.Require();'+[Environment]::NewLine+$install.Substring($end))
$body=$body.Replace('if (method == null || method.MetadataToken != token)','if (method == null)')
if($body.Contains('private static IdentityContract GetIdentityContract()')){
 $contract=Get-Method $body '        private static IdentityContract GetIdentityContract()'
 $replacement=@'
        private static IdentityContract GetIdentityContract()
        {
            return new IdentityContract { Owner = typeof(Game).GetMethod("LoadNewGame"), Setter = typeof(Player).GetProperty("GameId").GetSetMethod(),
                State = typeof(Game).GetField("State"), Player = typeof(NativeState).GetField("PlayerState"),
                NewGuid = typeof(Guid).GetMethod("NewGuid", Type.EmptyTypes), ToStringMethod = typeof(object).GetMethod("ToString", Type.EmptyTypes) };
        }
'@
 $body=$body.Replace($contract,$replacement)
}
$scenarioPath=Join-Path $DiagnosticSourceDirectory 'Chunk7NativeBootstrapScenario.cs';$scenario=[IO.File]::ReadAllText($scenarioPath)
$begin=$scenario.IndexOf('                var save = probe.NativeSave;', [StringComparison]::Ordinal);$finish=$scenario.IndexOf('                Require(controls.NativeCastRequestCount', $begin, [StringComparison]::Ordinal)
if($begin-lt0-or$finish-le$begin){throw 'Actual native completion binding extent changed'}
$template=[IO.File]::ReadAllText($TemplatePath).Replace('/*ACTUAL_COMPLETION_BINDING*/',$scenario.Substring($begin,$finish-$begin))
$actualTemplate=Join-Path $OutputDirectory 'ActualCompletionTemplate.cs';[IO.File]::WriteAllText($actualTemplate,$template,[Text.UTF8Encoding]::new($false))
$actual=Join-Path $OutputDirectory 'ActualBootstrapProbe.cs';[IO.File]::WriteAllText($actual,$body,[Text.UTF8Encoding]::new($false));[IO.File]::WriteAllText((Join-Path $OutputDirectory 'source.cs'),$original,[Text.UTF8Encoding]::new($false))
$framework='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7';$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$json='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll';$identity=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics/RetainedObjectIdentity.cs'
$exe=Join-Path $OutputDirectory 'ProbeTests.exe';$rsp=Join-Path $OutputDirectory 'compile.rsp';$args=@('/nologo','/target:exe','/langversion:7.3','/optimize+','/deterministic+','/nostdlib+','/nowarn:0649,0169,0414',('/out:'+$exe),('/reference:'+$json))
$args+=@('mscorlib.dll','System.dll','System.Core.dll','System.Xml.dll','System.Runtime.Serialization.dll')|ForEach-Object{'/reference:'+(Join-Path $framework $_)};$args+=@($actual,$actualTemplate,$identity)
[IO.File]::WriteAllLines($rsp,@($args|ForEach-Object{'"'+$_+'"'}),[Text.UTF8Encoding]::new($false));&$compiler ('@'+$rsp);$compileExit=$LASTEXITCODE
$testExit=$null;$output=@();if($compileExit-eq0){Copy-Item -LiteralPath $json -Destination (Join-Path $OutputDirectory 'Newtonsoft.Json.dll');$mode=if($NativeFlowOnly){'--native-flow'}else{'--all'};$output=@(&$exe $mode 2>&1);$testExit=$LASTEXITCODE;$output|ForEach-Object{Write-Host $_}}
$receipt=[ordered]@{schemaVersion=1;sourcePath=$sourcePath;sourceSha256=(Get-FileHash -LiteralPath $sourcePath).Hash.ToLowerInvariant();actualSha256=(Get-FileHash -LiteralPath $actual).Hash.ToLowerInvariant();templateSha256=(Get-FileHash -LiteralPath $TemplatePath).Hash.ToLowerInvariant();scenarioSha256=(Get-FileHash -LiteralPath $scenarioPath).Hash.ToLowerInvariant();actualCompletionTemplateSha256=(Get-FileHash -LiteralPath $actualTemplate).Hash.ToLowerInvariant();compileExit=$compileExit;testExit=$testExit;pass=@($output|Where-Object{$_-cmatch'^PASS '}).Count;fail=@($output|Where-Object{$_-cmatch'^FAIL '}).Count;nativeFlowOnly=[bool]$NativeFlowOnly;nativeInvoked=$false;archiveRead=$false;externalSubstitutions=@('exact native method/field metadata boundary','Harmony patch registration','native Game/Player/SaveManager objects and exact identity IL sequence');limits='Dynamic IL executes original-shaped Guid setter plus actual inserted observer; this is not the Unity/Mono native process.'}
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'receipt.json'),($receipt|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
if($compileExit-ne0-or$testExit-ne0){throw 'Native bootstrap probe regression failed; original receipt preserved.'}
