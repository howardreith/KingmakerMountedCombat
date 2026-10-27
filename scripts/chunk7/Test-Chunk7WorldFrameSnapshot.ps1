[CmdletBinding()]
param([ValidateSet('Debug','Release')][string]$Configuration='Release',[string]$ProductDll,[string]$ManagedDirectory,[string]$OutputRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$lab=[IO.Path]::GetFullPath((Join-Path $repo '../..'))
if(-not$ProductDll){$ProductDll=Join-Path $repo "bin/$Configuration/KingmakerMountedCombat.dll"}
if(-not$ManagedDirectory){$layout=(Get-Content -Raw (Join-Path $lab 'environment-intake.json')|ConvertFrom-Json).requestedLayout;$ManagedDirectory=Join-Path $layout.kingmakerInstallDir 'Kingmaker_Data/Managed'}
if(-not$OutputRoot){$OutputRoot=Join-Path $lab ('analysis-cache/chunk7-continuation/world-frame-snapshot-'+[Guid]::NewGuid().ToString('N'))}
if(Test-Path -LiteralPath $OutputRoot){throw 'World frame-snapshot output already exists.'}
if((Get-FileHash (Join-Path $ManagedDirectory 'Assembly-CSharp.dll')).Hash.ToLowerInvariant()-cne'3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb'){throw 'Installed native assembly identity changed.'}
[void][IO.Directory]::CreateDirectory($OutputRoot)
$source=Join-Path $repo 'tests/Chunk7Diagnostics/WorldFrameSnapshotContracts.cs'
$compiler=Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$exe=Join-Path $OutputRoot 'WorldFrameSnapshotContracts.exe'
$clock=[Diagnostics.Stopwatch]::StartNew()
& $compiler /nologo /target:exe /langversion:7.3 "/out:$exe" $source
if($LASTEXITCODE-ne0){throw 'Actual world frame-snapshot contract compilation failed.'}
& $exe $ManagedDirectory $ProductDll
$exitCode=$LASTEXITCODE
$clock.Stop()
$receipt=[ordered]@{kind='chunk7-world-frame-snapshot-actual';status=$(if($exitCode-eq0){'PASS'}else{'FAIL'});exitCode=$exitCode;durationSeconds=$clock.Elapsed.TotalSeconds;productDllSha256=(Get-FileHash -LiteralPath $ProductDll -Algorithm SHA256).Hash.ToLowerInvariant();testSha256=(Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant();nativeProof=$false;allocationRateMeasured=$false}
$receipt|ConvertTo-Json -Depth 4|Set-Content -LiteralPath (Join-Path $OutputRoot 'receipt.json') -Encoding UTF8
if($exitCode-ne0){throw 'Actual world frame-snapshot contract failed.'}
