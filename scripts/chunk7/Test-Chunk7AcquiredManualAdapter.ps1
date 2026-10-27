[CmdletBinding()]
param([string]$RepositoryRoot,[string]$DiagnosticSourceDirectory,[string]$TemplatePath,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('DiagnosticSourceDirectory')){$DiagnosticSourceDirectory=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics'}
if(-not$PSBoundParameters.ContainsKey('TemplatePath')){$TemplatePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/AcquiredManualAdapterTemplate.cs'}
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/acquired-manual-adapter-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
function Get-ActualMethod([string]$Text,[string]$Signature){$start=$Text.IndexOf($Signature,[StringComparison]::Ordinal);if($start-lt0){throw ('Missing actual method '+$Signature)};$open=$Text.IndexOf('{',$start);$depth=1;$end=$open+1;while($depth-gt0-and$end-lt$Text.Length){if($Text[$end]-ceq'{'){$depth++}elseif($Text[$end]-ceq'}'){$depth--};$end++};if($depth-ne0){throw 'Unbalanced actual method'};$Text.Substring($start,$end-$start)}
$scenarioPath=Join-Path $DiagnosticSourceDirectory 'Chunk7AcquiredManualScenario.cs';$scenario=[IO.File]::ReadAllText($scenarioPath)
$methods=@('internal void BeginBeforeLoad()','private void RequireAuthority(','private void RequireWorld()','private void BeginCleanup()','private void PollCleanup()','private void Retain(','internal bool DrainForHost()','public void Dispose()','private void Pass(','private void Fail(','private static void Require(')
$actual=($methods|ForEach-Object{Get-ActualMethod $scenario ('        '+$_)})-join[Environment]::NewLine
foreach($signature in @('private bool Busy()','private void RequireNoOwners()','private void RequireNoEffects()')){$start=$scenario.IndexOf('        '+$signature,[StringComparison]::Ordinal);if($start-lt0){throw 'Missing actual expression'};$end=$scenario.IndexOf(';',$start);$actual+=[Environment]::NewLine+$scenario.Substring($start,$end-$start+1)}
$constructor=Get-ActualMethod $scenario '        internal Chunk7AcquiredManualScenario('
$guardStart=$constructor.IndexOf('            if (request?', [StringComparison]::Ordinal);$guardEnd=$constructor.IndexOf('            this.request = request;', [StringComparison]::Ordinal)
if($guardStart-lt0-or$guardEnd-le$guardStart){throw 'Actual constructor admission boundary changed'}
$guard=$constructor.Substring($guardStart,$guardEnd-$guardStart)
$savePath=Join-Path $DiagnosticSourceDirectory 'Chunk7AcquiredManualScenario.Save.cs';if(-not(Test-Path -LiteralPath $savePath)){$savePath=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk7AcquiredManualScenario.Save.cs'}
$actual+=[Environment]::NewLine+(Get-ActualMethod ([IO.File]::ReadAllText($savePath)) '        private void RequireSaveWorldReferences()')
$body=[IO.File]::ReadAllText($TemplatePath).Replace('/*ACTUAL_SCENARIO_METHODS*/',$actual).Replace('/*ACTUAL_CONSTRUCTOR_GUARD*/',$guard)
$adapter=Join-Path $OutputDirectory 'ActualAcquiredManualMethods.cs';[IO.File]::WriteAllText($adapter,$body,[Text.UTF8Encoding]::new($false))
$probePath=Join-Path $DiagnosticSourceDirectory 'Chunk7BootstrapInventoryProbe.cs';if(-not(Test-Path -LiteralPath $probePath)){$probePath=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk7BootstrapInventoryProbe.cs'};$probe=[IO.File]::ReadAllText($probePath)
$probe=[regex]::Replace($probe,'(?m)^using (?:Harmony12|Kingmaker|Kingmaker\.EntitySystem\.Persistence|UnityEngine);\r?\n','')
$probe='using Time = UnityEngine.Time; using Player = Kingmaker.Player;'+[Environment]::NewLine+$probe
$install=Get-ActualMethod $probe '        internal void Install()';$marker=$install.IndexOf('// Native contract validation;',[StringComparison]::Ordinal)
if($marker-lt0){throw 'Native metadata substitution marker missing'}
$probe=$probe.Replace($install,$install.Substring(0,$marker)+'MetadataBoundary.Require(); harmony = HarmonyInstance.Create(HarmonyId); active = this; harmony.Patch(); installed = true; }')
$actualProbe=Join-Path $OutputDirectory 'ActualBootstrapInventoryProbe.cs';[IO.File]::WriteAllText($actualProbe,$probe,[Text.UTF8Encoding]::new($false))
$framework='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7';$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$json='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll'
$identity=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics/RetainedObjectIdentity.cs';$rosterPath=Join-Path $DiagnosticSourceDirectory 'Chunk7NativeFieldRoster.cs';$exe=Join-Path $OutputDirectory 'AcquiredManualActualMethods.exe';$rsp=Join-Path $OutputDirectory 'compile.rsp'
$args=@('/nologo','/target:exe','/langversion:7.3','/optimize+','/deterministic+','/nostdlib+','/nowarn:0649,0169,0414',('/out:'+$exe),('/reference:'+$json))
$args+=@('mscorlib.dll','System.dll','System.Core.dll','System.Xml.dll','System.Runtime.Serialization.dll')|ForEach-Object{'/reference:'+(Join-Path $framework $_)};$args+=@($adapter,$actualProbe,$identity,$rosterPath)
[IO.File]::WriteAllLines($rsp,@($args|ForEach-Object{'"'+$_+'"'}),[Text.UTF8Encoding]::new($false))
&$compiler ('@'+$rsp);if($LASTEXITCODE-ne0){throw 'Cold inventory actual-method compile failed'}
Copy-Item -LiteralPath $json -Destination (Join-Path $OutputDirectory 'Newtonsoft.Json.dll')
&$exe;if($LASTEXITCODE-ne0){throw 'Cold inventory actual-method regression failed'}
@{scenarioSha256=(Get-FileHash -LiteralPath $scenarioPath).Hash.ToLowerInvariant();probeSha256=(Get-FileHash -LiteralPath $probePath).Hash.ToLowerInvariant();rosterSha256=(Get-FileHash -LiteralPath $rosterPath).Hash.ToLowerInvariant();templateSha256=(Get-FileHash -LiteralPath $TemplatePath).Hash.ToLowerInvariant();unmodeledTwoOwnerBranchRefused=$true;externalNativeObjectsSubstituted=$true;metadataHarmonyBoundarySubstituted=$true;fileHashBoundarySubstituted=$true;nativeExecution=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
