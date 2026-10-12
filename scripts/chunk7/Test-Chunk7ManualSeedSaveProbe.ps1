[CmdletBinding()]
param([string]$RepositoryRoot,[string]$DiagnosticSourceDirectory,[string]$TemplatePath,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('DiagnosticSourceDirectory')){$DiagnosticSourceDirectory=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics'}
if(-not$PSBoundParameters.ContainsKey('TemplatePath')){$TemplatePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/ManualSeedSaveProbeTemplate.cs'}
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/manual-seed-probe-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
$path=Join-Path $DiagnosticSourceDirectory 'Chunk7ManualSeedSaveProbe.cs';$source=[IO.File]::ReadAllText($path)
$source=[regex]::Replace($source,'(?m)^using (?:Harmony12|Kingmaker|Kingmaker\.EntitySystem\.Persistence|KingmakerMountedCombat\.Integration|UnityEngine);\r?\n','')
$source=$source.Replace('typeof(Game).Module.ModuleVersionId == new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7")','MetadataBoundary.Matches()').Replace('method.MetadataToken == token.Value','MetadataBoundary.Matches()')
$actual=Join-Path $OutputDirectory 'ActualManualSeedSaveProbe.cs';[IO.File]::WriteAllText($actual,$source,[Text.UTF8Encoding]::new($false))
$framework='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7';$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe';$json='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll'
$exe=Join-Path $OutputDirectory 'ManualSeedProbeActual.exe';$rsp=Join-Path $OutputDirectory 'compile.rsp'
$args=@('/nologo','/target:exe','/langversion:7.3','/optimize+','/deterministic+','/nostdlib+','/nowarn:0649,0169,0414',('/out:'+$exe),('/reference:'+$json))
$args+=@('mscorlib.dll','System.dll','System.Core.dll','System.Xml.dll','System.Runtime.Serialization.dll')|ForEach-Object{'/reference:'+(Join-Path $framework $_)};$args+=@($actual,$TemplatePath)
[IO.File]::WriteAllLines($rsp,@($args|ForEach-Object{'"'+$_+'"'}),[Text.UTF8Encoding]::new($false))
&$compiler ('@'+$rsp);if($LASTEXITCODE-ne0){throw 'Manual seed actual probe compile failed'}
Copy-Item -LiteralPath $json -Destination (Join-Path $OutputDirectory 'Newtonsoft.Json.dll')
&$exe;if($LASTEXITCODE-ne0){throw 'Manual seed actual probe regression failed'}
@{schemaVersion=1;sourceSha256=(Get-FileHash -LiteralPath $path).Hash.ToLowerInvariant();actualProbeMethods=$true;nativeObjectsAndPatchMetadataBoundarySubstituted=$true;nativeExecution=$false;archiveAccess=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
