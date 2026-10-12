
[CmdletBinding()]
param([string]$RepositoryRoot,[string]$DiagnosticSourceDirectory,[string]$TemplatePath,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('DiagnosticSourceDirectory')){$DiagnosticSourceDirectory=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics'}
if(-not$PSBoundParameters.ContainsKey('TemplatePath')){$TemplatePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/NativeBootstrapAdapterTemplate.cs'}
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/native-bootstrap-adapter-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
function Get-ActualMethod([string]$Text,[string]$Signature){
 $start=$Text.IndexOf($Signature,[StringComparison]::Ordinal);if($start-lt0){throw ('Missing actual method '+$Signature)}
 $open=$Text.IndexOf('{',$start);$depth=1;$end=$open+1
 while($depth-gt0-and$end-lt$Text.Length){if($Text[$end]-ceq'{'){$depth++}elseif($Text[$end]-ceq'}'){$depth--};$end++}
 if($depth-ne0){throw 'Unbalanced exact method'};$Text.Substring($start,$end-$start)
}
$scenarioPath=Join-Path $DiagnosticSourceDirectory 'Chunk7NativeBootstrapScenario.cs';$scenario=[IO.File]::ReadAllText($scenarioPath)
$methods=@('internal void BindAndStart(','private void RequireNativeMenuAdmission()','private void RequireOriginalMenu()','private void RequireAuthority(','private void RequireNativeWorld()','private void BeginCleanup()','private void PollCleanup()','private void Retain(','internal bool DrainForHost()','public void Dispose()','private void Pass(','private void Fail(','private static void Require(')
$actual=($methods|ForEach-Object{Get-ActualMethod $scenario ('        '+$_)})-join[Environment]::NewLine
foreach($signature in @('private bool Busy()','private void RequireNoOwners()')){
 $start=$scenario.IndexOf('        '+$signature,[StringComparison]::Ordinal);if($start-lt0){throw 'Missing actual expression boundary'}
 $end=$scenario.IndexOf(';',$start);$actual+=[Environment]::NewLine+$scenario.Substring($start,$end-$start+1)
}
# Exact Update entry and catch: the later world/evidence body is not executed by
# this admission adapter. This proves popup refusal precedes loading early return.
$update=Get-ActualMethod $scenario '        internal void Update()'
$marker='                Require(probe != null && probe.Complete';$cut=$update.IndexOf($marker,[StringComparison]::Ordinal)
$catch=$update.LastIndexOf('            catch (Exception error)',[StringComparison]::Ordinal)
if($cut-lt0-or$catch-lt$cut){throw 'Actual Update admission slice changed'}
$actual+=[Environment]::NewLine+$update.Substring(0,$cut)+'            }'+[Environment]::NewLine+$update.Substring($catch)
$body=[IO.File]::ReadAllText($TemplatePath).Replace('/*ACTUAL_SCENARIO_METHODS*/',$actual)
$adapter=Join-Path $OutputDirectory 'BootstrapActualMethods.cs';[IO.File]::WriteAllText($adapter,$body,[Text.UTF8Encoding]::new($false))
$probePath=Join-Path $DiagnosticSourceDirectory 'Chunk7NativeBootstrapProbe.cs';$probe=[IO.File]::ReadAllText($probePath)
$probe=[regex]::Replace($probe,'(?m)^using (?:Harmony12|Kingmaker|Kingmaker\.Blueprints\.Area|Kingmaker\.EntitySystem\.Persistence|UnityEngine);\r?\n','')
$install=Get-ActualMethod $probe '        internal void Install()'
$marker=$install.IndexOf('// Native contract validation;',[StringComparison]::Ordinal);$tail=$install.IndexOf('            harmony = HarmonyInstance.Create(',[StringComparison]::Ordinal)
if($marker-lt0-or$tail-lt$marker){throw 'Probe metadata boundary changed'}
$replacement=$install.Substring(0,$marker)+'MetadataBoundary.Require();'+[Environment]::NewLine+$install.Substring($tail)
$probe=$probe.Replace($install,$replacement)
$patch=Get-ActualMethod $probe '        private void Patch('
$probe=$probe.Replace($patch,'        private void Patch(Type type,string name,int token,Type[] parameters,string prefix,string postfix) { harmony.Patch(); }')
$actualProbe=Join-Path $OutputDirectory 'ActualBootstrapProbe.cs';[IO.File]::WriteAllText($actualProbe,$probe,[Text.UTF8Encoding]::new($false))
$framework='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7'
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$json='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll'
$identity=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics/RetainedObjectIdentity.cs'
$exe=Join-Path $OutputDirectory 'NativeBootstrapActualMethods.exe';$rsp=Join-Path $OutputDirectory 'compile.rsp'
$args=@('/nologo','/target:exe','/langversion:7.3','/optimize+','/deterministic+','/nostdlib+','/nowarn:0649,0169,0414',('/out:'+$exe),('/reference:'+$json))
$args+=@('mscorlib.dll','System.dll','System.Core.dll','System.Xml.dll','System.Runtime.Serialization.dll')|ForEach-Object{'/reference:'+(Join-Path $framework $_)}
$args+=@($adapter,$actualProbe,$identity)
[IO.File]::WriteAllLines($rsp,@($args|ForEach-Object{'"'+$_+'"'}),[Text.UTF8Encoding]::new($false))
&$compiler ('@'+$rsp);if($LASTEXITCODE-ne0){throw 'Native bootstrap actual-method adapter compile failed'}
Copy-Item -LiteralPath $json -Destination (Join-Path $OutputDirectory 'Newtonsoft.Json.dll')
&$exe;if($LASTEXITCODE-ne0){throw 'Native bootstrap actual-method adapter failed'}
@{scenarioSha256=(Get-FileHash -LiteralPath $scenarioPath).Hash.ToLowerInvariant();probeSha256=(Get-FileHash -LiteralPath $probePath).Hash.ToLowerInvariant();identitySha256=(Get-FileHash -LiteralPath $identity).Hash.ToLowerInvariant();nativeCalls=0;archivesRead=0;externalBoundarySubstitutes=@('native game objects','Harmony and metadata dispatch','artifact writer');actualMethods=$methods;actualUpdateAdmissionPrefix=$true}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $OutputDirectory 'boundary-receipt.json') -Encoding UTF8
