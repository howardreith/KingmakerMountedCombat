$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
$probe=Join-Path $chunk7AnalysisRoot ('probe-'+[Guid]::NewGuid().ToString('N'));[void][IO.Directory]::CreateDirectory($probe)
$vs=Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Microsoft Visual Studio/Installer/vswhere.exe'
$compiler=@(& $vs -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild/**/Bin/Roslyn/csc.exe')|Select-Object -First 1
$exe=Join-Path $probe 'Chunk7PersistenceProtocolTests.exe'
& $compiler /nologo /target:exe /langversion:7.3 "/out:$exe" (Join-Path $repo 'src/KingmakerMountedCombat/BuildIdentity.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/RuntimeProtocol.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7NativeBootstrapSelection.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7BootstrapInventorySelection.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7ManualSeedSelection.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7ManualDerivedSelection.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7AcquiredManualSelection.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7OrdinaryAcquisitionSelection.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7OrdinaryFieldSelection.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7SmallCalibrationSelection.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7ProgressionSelection.cs') (Join-Path $chunk7TestSource 'Chunk7PersistenceProtocolTests.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Domain/PoseVector3.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Domain/MountedRiderPoseProfile.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Domain/DeclaredSmallRiderBody.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Domain/MountedRiderProfileCatalog.cs')
if($LASTEXITCODE-ne0){throw 'Staged Chunk 7 persistence protocol compilation failed.'}
& $exe
if($LASTEXITCODE-ne0){throw 'Staged Chunk 7 persistence protocol checks failed.'}
