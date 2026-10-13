[CmdletBinding()]
param([string]$RepositoryRoot,[string]$DiagnosticSourceDirectory,[string]$TemplatePath,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$DiagnosticSourceDirectory){$DiagnosticSourceDirectory=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics'}
if(-not$TemplatePath){$TemplatePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/CumulativeCustodyTemplate.cs'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/chunk7cumulativecustody-'+[Guid]::NewGuid().ToString('N'))};[void][IO.Directory]::CreateDirectory($OutputDirectory)
$sourcePath=Join-Path $DiagnosticSourceDirectory 'Chunk7ManualDerivedScenario.Cumulative.cs';$source=[IO.File]::ReadAllText($sourcePath)
$start=$source.IndexOf('        private void BindCumulativeOwner()',[StringComparison]::Ordinal);$end=$source.LastIndexOf('    }',[StringComparison]::Ordinal)
if($start-lt0-or$end-le$start){throw 'Actual cumulative method boundary missing.'}
$methods=$source.Substring($start,$end-$start)
$buildPath=Join-Path $DiagnosticSourceDirectory 'Chunk7ManualDerivedScenario.Build.cs';if(-not(Test-Path $buildPath)){$buildPath=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk7ManualDerivedScenario.Build.cs'}
$build=[IO.File]::ReadAllText($buildPath);$a=$build.IndexOf('        private JObject CaptureNoArrangement()',[StringComparison]::Ordinal);$b=$build.IndexOf('        private void StartBuild()',$a,[StringComparison]::Ordinal);if($a-lt0-or$b-le$a){throw 'Actual native progression capture missing.'};$methods+=$build.Substring($a,$b-$a)
$body=[IO.File]::ReadAllText($TemplatePath).Replace('@@ACTUAL_METHODS@@',$methods);$actual=Join-Path $OutputDirectory 'ActualCumulativeCustody.cs';[IO.File]::WriteAllText($actual,$body,[Text.UTF8Encoding]::new($false))
$framework='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7';$json='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll';$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe';$exe=Join-Path $OutputDirectory 'custody.exe';$rsp=Join-Path $OutputDirectory 'compile.rsp'
$args=@('/nologo','/target:exe','/langversion:7.3','/optimize+','/deterministic+','/nostdlib+',('/out:'+$exe),('/reference:'+$json));$args+=@('mscorlib.dll','System.dll','System.Core.dll')|ForEach-Object{'/reference:'+(Join-Path $framework $_)};$args+=$actual
[IO.File]::WriteAllLines($rsp,@($args|ForEach-Object{'"'+$_+'"'}),[Text.UTF8Encoding]::new($false));&$compiler ('@'+$rsp);if($LASTEXITCODE-ne0){throw 'Actual cumulative custody compile failed.'}
[IO.File]::Copy($json,(Join-Path $OutputDirectory 'Newtonsoft.Json.dll'),$false);&$exe;if($LASTEXITCODE-ne0){throw 'Actual cumulative custody checks failed.'}
@{sourceSha256=(Get-FileHash $sourcePath).Hash.ToLowerInvariant();progressionSourceSha256=(Get-FileHash $buildPath).Hash.ToLowerInvariant();actualMethods='BindCumulativeOwner through CaptureCumulativeFinal plus CaptureNoArrangement';substitution='native object/property containers only; no native execution';archiveAccess=$false}|ConvertTo-Json|Set-Content (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
