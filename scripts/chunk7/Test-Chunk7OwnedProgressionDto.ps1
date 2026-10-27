[CmdletBinding()]
param([string]$RepositoryRoot,[string]$DiagnosticSourceDirectory,[string]$ReaderDirectory,[string]$FixtureScript,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('DiagnosticSourceDirectory')){$DiagnosticSourceDirectory=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics'}
if(-not$PSBoundParameters.ContainsKey('ReaderDirectory')){$ReaderDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
if(-not$PSBoundParameters.ContainsKey('FixtureScript')){$FixtureScript=Join-Path $PSScriptRoot 'Test-Chunk7ProgressionEvidence.ps1'}
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/owned-progression-dto-'+[Guid]::NewGuid().ToString('N'))}
# Installed Json.NET's default deserializer resolves its managed Unity vector
# converter types. Load exact installed references in place; do not copy binaries.
$managed='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed'
foreach($leaf in @('UnityEngine.CoreModule.dll','UnityEngine.dll','Newtonsoft.Json.dll')){[void][Reflection.Assembly]::LoadFrom((Join-Path $managed $leaf))}
& (Join-Path $RepositoryRoot 'scripts/chunk7/Test-Chunk7ProgressionDto.ps1') -RepositoryRoot $RepositoryRoot -DiagnosticSourceDirectory $DiagnosticSourceDirectory -TestSource (Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/ProgressionDtoTests.cs') -EvidenceDtoSource (Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk7EvidenceDto.cs') -FixtureScript $FixtureScript -ReaderDirectory $ReaderDirectory -OutputDirectory $OutputDirectory
