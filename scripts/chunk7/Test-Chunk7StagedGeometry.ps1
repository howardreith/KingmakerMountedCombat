[CmdletBinding()]
param([string]$RepositoryRoot,[string]$OutputDirectory)
# 7E F04 staged leg geometry terms, compiled alone (no native or Unity reference) with the real
# b202 st exhaustion-leg proposals: which term refused each proposal, and the corrected bands.
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/staged-geometry-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
$sources=@((Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk7StagedGeometry.cs'),(Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/StagedGeometryTests.cs'))
$framework='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7';$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$exe=Join-Path $OutputDirectory 'staged-geometry.exe';$rsp=Join-Path $OutputDirectory 'compile.rsp'
$arguments=@('/nologo','/target:exe','/langversion:7.3','/warnaserror+','/optimize+','/deterministic+','/nostdlib+','/main:KingmakerMountedCombat.Tests.StagedGeometryTests',('/out:'+$exe))
$arguments+=@('mscorlib.dll','System.dll','System.Core.dll')|ForEach-Object{'/reference:'+(Join-Path $framework $_)}
[IO.File]::WriteAllLines($rsp,@(($arguments+$sources)|ForEach-Object{'"'+$_+'"'}),[Text.UTF8Encoding]::new($false))
&$compiler ('@'+$rsp);if($LASTEXITCODE-ne0){throw 'Staged geometry compile failed.'}
&$exe;if($LASTEXITCODE-ne0){throw 'Staged geometry regression failed.'}
