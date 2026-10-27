[CmdletBinding()]
param([string]$RepositoryRoot,[string]$DiagnosticSourceDirectory,[string]$TemplatePath,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$DiagnosticSourceDirectory){$DiagnosticSourceDirectory=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics'}
if(-not$TemplatePath){$TemplatePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/TwoOwnerCustodyTemplate.cs'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/chunk7twoownercustody-'+[Guid]::NewGuid().ToString('N'))};[void][IO.Directory]::CreateDirectory($OutputDirectory)
function Span([string]$Text,[string]$Begin,[string]$End){$a=$Text.IndexOf($Begin,[StringComparison]::Ordinal);$b=$Text.IndexOf($End,$a,[StringComparison]::Ordinal);if($a-lt0-or$b-le$a){throw ('Exact source span missing: '+$Begin)};$Text.Substring($a,$b-$a)}
$sourcePath=Join-Path $DiagnosticSourceDirectory 'Chunk7AcquiredManualScenario.TwoOwners.cs';$source=[IO.File]::ReadAllText($sourcePath)
$a=$source.IndexOf('        private void RequireTwoOwnerSource()',[StringComparison]::Ordinal);$b=$source.LastIndexOf('    }',[StringComparison]::Ordinal);if($a-lt0-or$b-le$a){throw 'Exact two-owner methods missing.'};$methods=$source.Substring($a,$b-$a)
$fieldPath=Join-Path $DiagnosticSourceDirectory 'Chunk7AcquiredManualScenario.Field.cs';$field=[IO.File]::ReadAllText($fieldPath)
$methods+=Span $field '        private UnitEntityData[] OwnedActors()' '        private void StartField()'
$savePath=Join-Path $DiagnosticSourceDirectory 'Chunk7AcquiredManualScenario.Save.cs';$save=[IO.File]::ReadAllText($savePath)
$methods+=Span $save '        private JObject CaptureNoArrangement()' '        private void RequireSaveWorldReferences()'
$body=[IO.File]::ReadAllText($TemplatePath).Replace('@@ACTUAL_METHODS@@',$methods);$actual=Join-Path $OutputDirectory 'ActualTwoOwnerCustody.cs';[IO.File]::WriteAllText($actual,$body,[Text.UTF8Encoding]::new($false))
$framework='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7';$json='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll';$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe';$exe=Join-Path $OutputDirectory 'custody.exe';$rsp=Join-Path $OutputDirectory 'compile.rsp'
$args=@('/nologo','/target:exe','/langversion:7.3','/optimize+','/deterministic+','/nostdlib+',('/out:'+$exe),('/reference:'+$json));$args+=@('mscorlib.dll','System.dll','System.Core.dll')|ForEach-Object{'/reference:'+(Join-Path $framework $_)};$args+=$actual
[IO.File]::WriteAllLines($rsp,@($args|ForEach-Object{'"'+$_+'"'}),[Text.UTF8Encoding]::new($false));&$compiler ('@'+$rsp);if($LASTEXITCODE-ne0){throw 'Actual two-owner custody compile failed.'}
[IO.File]::Copy($json,(Join-Path $OutputDirectory 'Newtonsoft.Json.dll'),$false)
# Installed Json.NET initializes its Unity vector converter when Formatting.None
# serializes a JToken. Keep that exact runtime dependency in the disposable host.
$unity=Join-Path (Split-Path $json -Parent) 'UnityEngine.dll'
[IO.File]::Copy($unity,(Join-Path $OutputDirectory 'UnityEngine.dll'),$false)
[IO.File]::Copy((Join-Path (Split-Path $json -Parent) 'UnityEngine.CoreModule.dll'),(Join-Path $OutputDirectory 'UnityEngine.CoreModule.dll'),$false)
&$exe;if($LASTEXITCODE-ne0){throw 'Actual two-owner custody checks failed.'}
@{sourceSha256=(Get-FileHash $sourcePath).Hash.ToLowerInvariant();fieldSourceSha256=(Get-FileHash $fieldPath).Hash.ToLowerInvariant();saveSourceSha256=(Get-FileHash $savePath).Hash.ToLowerInvariant();actualMethods='RequireTwoOwnerSource through CaptureTwoOwners; OwnedActors/RequireAcquiredOwner/CaptureOwner/CaptureResources/CaptureNoArrangement';substitution='native object/property containers only; no native execution';archiveAccess=$false}|ConvertTo-Json|Set-Content (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
