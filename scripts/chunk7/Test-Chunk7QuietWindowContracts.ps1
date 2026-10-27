[CmdletBinding()]
param([ValidateSet('Debug','Release')][string]$Configuration='Release',[string]$ProductDll,[string]$ManagedDirectory,[string]$OutputRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$lab=[IO.Path]::GetFullPath((Join-Path $repo '../..'))
if(-not$ProductDll){$ProductDll=Join-Path $repo "bin/$Configuration/KingmakerMountedCombat.dll"}
if(-not$ManagedDirectory){$layout=(Get-Content -Raw (Join-Path $lab 'environment-intake.json')|ConvertFrom-Json).requestedLayout;$ManagedDirectory=Join-Path $layout.kingmakerInstallDir 'Kingmaker_Data/Managed'}
if(-not$OutputRoot){$OutputRoot=Join-Path $lab ('analysis-cache/chunk7-continuation/quiet-contracts-'+[Guid]::NewGuid().ToString('N'))}
if((Get-FileHash (Join-Path $ManagedDirectory 'Assembly-CSharp.dll')).Hash.ToLowerInvariant()-cne'3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb'){throw 'Installed native assembly identity changed.'}
[void][IO.Directory]::CreateDirectory($OutputRoot)
$source=Join-Path $repo 'tests/Chunk7Diagnostics/QuietWindowContracts.cs'
$compiler=Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$exe=Join-Path $OutputRoot 'QuietWindowContracts.exe'
& $compiler /nologo /target:exe /langversion:7.3 "/out:$exe" $source
if($LASTEXITCODE-ne0){throw 'Quiet-window actual-method contract compilation failed.'}
& $exe $ManagedDirectory $ProductDll
if($LASTEXITCODE-ne0){throw 'Quiet-window actual-method contract failed.'}
