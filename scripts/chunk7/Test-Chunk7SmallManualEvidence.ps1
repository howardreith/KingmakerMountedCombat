[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory,[string]$FixturePath,[string]$OutputDirectory,[switch]$FixturesOnly)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))};if(-not$ReaderDirectory){$ReaderDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
if(-not$FixturePath){$FixturePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/SmallManualFixture.json'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/small-manual-evidence-'+[Guid]::NewGuid().ToString('N'))}
. (Join-Path $ReaderDirectory 'RuntimeHarness.Common.ps1')
$script:smallManualFixturePath=$FixturePath
function New-SmallManualFixture{[IO.File]::ReadAllText($script:smallManualFixturePath)|ConvertFrom-Json}
function Check-SmallManual($F){Assert-KmcChunk7SmallEvidence $F.request $F.artifact}
if($FixturesOnly){return}
# RETIRED (owner decision 2026-10-24, docs/CHUNK7-RETIRED-SMALL-MANUAL-CALIBRATION-2026-10-24.md). The fixture encodes the
# Auto-derived source contract (Auto_1.zks, saveBeforeFieldSelection) that ed40ef95 replaced with the closed Manual field leaves, so
# the unchanged reader must refuse it at the closed source modes. Its former mutation matrix ran against that refused base, so
# every case "refused" vacuously; it is not run. The F09 Small reader (Test-Chunk7SmallF09Evidence.ps1) is the live coverage
# built from this fixture, and Test-Chunk7RetiredSmallCalibrationCases.ps1 proves the runner refuses both retired cases.
$retiredError=$null;try{Check-SmallManual (New-SmallManualFixture)}catch{$retiredError=$_.Exception.Message}
if($retiredError-cne'Manual-derived output is outside the closed source modes.'){throw ('The retired Small Manual fixture was not refused at the closed source modes: '+$retiredError)}
Write-Host 'RETIRED PASS=1 FAIL=0: the retired Auto-derived Small Manual fixture is refused at the closed source modes; no mutation matrix runs on a refused base.'
