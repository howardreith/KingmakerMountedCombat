[CmdletBinding()]
param([string]$RepositoryRoot,[string]$DiagnosticSourceDirectory,[string]$FieldDiagnosticDirectory,[string]$TestSource,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$DiagnosticSourceDirectory){$DiagnosticSourceDirectory=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics'}
if(-not$FieldDiagnosticDirectory){$FieldDiagnosticDirectory=$DiagnosticSourceDirectory}
if(-not$TestSource){$TestSource=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/SmallCalibrationProtocolTests.cs'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/small-protocol-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
$sources=@($TestSource,(Join-Path $RepositoryRoot 'tests/TestRunner.cs'),(Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/BuildIdentity.cs'),(Join-Path $DiagnosticSourceDirectory 'RuntimeProtocol.cs'),(Join-Path $DiagnosticSourceDirectory 'Chunk7NativeBootstrapSelection.cs'),(Join-Path $DiagnosticSourceDirectory 'Chunk7BootstrapInventorySelection.cs'),(Join-Path $DiagnosticSourceDirectory 'Chunk7ManualSeedSelection.cs'),(Join-Path $DiagnosticSourceDirectory 'Chunk7ManualDerivedSelection.cs'),(Join-Path $DiagnosticSourceDirectory 'Chunk7AcquiredManualSelection.cs'),(Join-Path $DiagnosticSourceDirectory 'Chunk7AssistedFixtureSelection.cs'),(Join-Path $DiagnosticSourceDirectory 'Chunk7AssistedColdInventorySelection.cs'),(Join-Path $DiagnosticSourceDirectory 'Chunk7CumulativeAcquisitionSelection.cs'),(Join-Path $DiagnosticSourceDirectory 'Chunk7OwnedPairsSelection.cs'),(Join-Path $DiagnosticSourceDirectory 'Chunk7SmallCalibrationSelection.cs'),(Join-Path $DiagnosticSourceDirectory 'Chunk7SmallFixtureSelection.cs'),(Join-Path $DiagnosticSourceDirectory 'Chunk7StockMammothFixtureSelection.cs'),(Join-Path $DiagnosticSourceDirectory 'Chunk7ProgressionSelection.cs'),(Join-Path $FieldDiagnosticDirectory 'Chunk7OrdinaryFieldSelection.cs'),(Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk7OrdinaryAcquisitionSelection.cs'))
foreach($leaf in @('PoseVector3.cs','MountedRiderPoseProfile.cs','DeclaredSmallRiderBody.cs','MountedRiderProfileCatalog.cs')){$sources+=Join-Path $RepositoryRoot ('src/KingmakerMountedCombat/Domain/'+$leaf)}
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe';$exe=Join-Path $OutputDirectory 'small-protocol.exe'
&$compiler /nologo /target:exe /langversion:7.3 /warnaserror+ "/out:$exe" $sources
if($LASTEXITCODE-ne0){throw 'Small pure protocol compile failed.'}
&$exe $OutputDirectory;if($LASTEXITCODE-ne0){throw 'Small pure protocol checks failed.'}
