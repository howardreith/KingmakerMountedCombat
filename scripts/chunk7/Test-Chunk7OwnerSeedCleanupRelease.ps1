[CmdletBinding()]
param([string]$RepositoryRoot,[string]$SourcePath,[string]$OutputDirectory)
# Compiles the ACTUAL owner-seed cleanup release methods (OwnerSeedCleanupReady through
# ReleaseSetupWriteCustody) with native-boundary stubs only, and runs the release cases.
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$SourcePath){$SourcePath=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk7WorldScenarioEngine.OwnerSeed.cs'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/owner-seed-cleanup-release-'+[Guid]::NewGuid().ToString('N'))}
if(Test-Path -LiteralPath $OutputDirectory){throw 'Fresh focused output directory is required.'}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
$text=[IO.File]::ReadAllText($SourcePath)
$start=$text.IndexOf('        private Chunk7NativeBuilderCancellation setupCancellation;',[StringComparison]::Ordinal)
$end=$text.IndexOf('        private bool OwnerSeedOwnershipPreserved()',[StringComparison]::Ordinal)
if($start-lt0-or$end-le$start-or$text.IndexOf('private bool ReleaseSetupWriteCustody()',$start,[StringComparison]::Ordinal)-gt$end){throw 'Actual owner-seed cleanup release methods were not found.'}
$methods=$text.Substring($start,$end-$start)
$template=[IO.File]::ReadAllText((Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/OwnerSeedCleanupReleaseTemplate.cs'))
if(-not$template.Contains('__METHODS__')){throw 'Template lacks its method marker.'}
$actual=Join-Path $OutputDirectory 'OwnerSeedCleanupRelease.cs';[IO.File]::WriteAllText($actual,$template.Replace('__METHODS__',$methods),[Text.UTF8Encoding]::new($false))
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$exe=Join-Path $OutputDirectory 'OwnerSeedCleanupRelease.exe'
& $compiler /nologo /target:exe /langversion:7.3 /warnaserror+ /nowarn:0169,0414,0649 "/out:$exe" $actual
if($LASTEXITCODE-ne0){throw 'Actual owner-seed cleanup release compilation failed.'}
& $exe;$code=$LASTEXITCODE
[pscustomobject]@{source=$SourcePath;sha256=(Get-FileHash -LiteralPath $SourcePath).Hash.ToLowerInvariant();exitCode=$code;nativeInvoked=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json')
if($code-ne0){throw 'Owner-seed cleanup release regression failed.'}
