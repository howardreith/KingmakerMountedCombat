[CmdletBinding()]
param([string]$RepositoryRoot,[string]$DiagnosticSourceDirectory,[string]$FieldDiagnosticDirectory,[string]$TestSource,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$DiagnosticSourceDirectory){$DiagnosticSourceDirectory=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics'}
if(-not$FieldDiagnosticDirectory){$FieldDiagnosticDirectory=$DiagnosticSourceDirectory}
if(-not$TestSource){$TestSource=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/SmallCalibrationProtocolTests.cs'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/small-protocol-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
$nativeSource=Join-Path $DiagnosticSourceDirectory 'Chunk7PersistenceScenario.cs'
$nativeText=[IO.File]::ReadAllText($nativeSource)
$nativeStart=$nativeText.IndexOf('        internal static string NativeName(SaveInfo.SaveType type) =>',[StringComparison]::Ordinal)
$nativeEnd=$nativeText.IndexOf('        internal static string NativeLeaf(', $nativeStart,[StringComparison]::Ordinal)
if($nativeStart-lt0-or$nativeEnd-le$nativeStart){throw 'Actual native Manual naming overloads are missing.'}
$nativeExtract=$nativeText.Substring($nativeStart,$nativeEnd-$nativeStart)
$nativeAdapter=Join-Path $OutputDirectory 'native-name.cs'
[IO.File]::WriteAllText($nativeAdapter,('using System; namespace Kingmaker.EntitySystem.Persistence { internal class SaveInfo { internal enum SaveType { Manual, Quick, Auto } } } namespace KingmakerMountedCombat.Diagnostics { using SaveInfo=Kingmaker.EntitySystem.Persistence.SaveInfo; internal static class RuntimePersistenceScenario { internal static string SlotName(SaveInfo.SaveType type) { return type.ToString(); } } internal sealed class Chunk7PersistenceScenario { private const string ManualName="KMC_C7_WORLD";'+$nativeExtract+' } }'),[Text.UTF8Encoding]::new($false))
$sources=@($TestSource,(Join-Path $RepositoryRoot 'tests/TestRunner.cs'),(Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/BuildIdentity.cs'),(Join-Path $DiagnosticSourceDirectory 'RuntimeProtocol.cs'),(Join-Path $DiagnosticSourceDirectory 'Chunk7NativeBootstrapSelection.cs'),(Join-Path $DiagnosticSourceDirectory 'Chunk7BootstrapInventorySelection.cs'),(Join-Path $DiagnosticSourceDirectory 'Chunk7ManualSeedSelection.cs'),(Join-Path $DiagnosticSourceDirectory 'Chunk7ManualDerivedSelection.cs'),(Join-Path $DiagnosticSourceDirectory 'Chunk7AcquiredManualSelection.cs'),(Join-Path $DiagnosticSourceDirectory 'Chunk7AssistedFixtureSelection.cs'),(Join-Path $DiagnosticSourceDirectory 'Chunk7AssistedColdInventorySelection.cs'),(Join-Path $DiagnosticSourceDirectory 'Chunk7CumulativeAcquisitionSelection.cs'),(Join-Path $DiagnosticSourceDirectory 'Chunk7OwnedPairsSelection.cs'),(Join-Path $DiagnosticSourceDirectory 'Chunk7SmallCalibrationSelection.cs'),(Join-Path $DiagnosticSourceDirectory 'Chunk7SmallFixtureSelection.cs'),(Join-Path $DiagnosticSourceDirectory 'Chunk7StockMammothFixtureSelection.cs'),(Join-Path $DiagnosticSourceDirectory 'Chunk7ProgressionSelection.cs'),(Join-Path $FieldDiagnosticDirectory 'Chunk7OrdinaryFieldSelection.cs'),(Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk7OrdinaryAcquisitionSelection.cs'))
$sources+=$nativeAdapter
foreach($leaf in @('PoseVector3.cs','MountedRiderPoseProfile.cs','DeclaredSmallRiderBody.cs','MountedRiderProfileCatalog.cs')){$sources+=Join-Path $RepositoryRoot ('src/KingmakerMountedCombat/Domain/'+$leaf)}
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe';$exe=Join-Path $OutputDirectory 'small-protocol.exe'
&$compiler /nologo /target:exe /langversion:7.3 /warnaserror+ "/out:$exe" $sources
if($LASTEXITCODE-ne0){throw 'Small pure protocol compile failed.'}
&$exe $OutputDirectory;if($LASTEXITCODE-ne0){throw 'Small pure protocol checks failed.'}
