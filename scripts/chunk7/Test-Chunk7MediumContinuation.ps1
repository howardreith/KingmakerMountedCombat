[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory,[string]$FixtureDirectory,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$ReaderDirectory){$ReaderDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
if(-not$FixtureDirectory){$FixtureDirectory=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/medium-continuation-'+[Guid]::NewGuid().ToString('N'))}
if(Test-Path -LiteralPath $OutputDirectory){throw 'Preserve prior Medium continuation evidence.'};[void][IO.Directory]::CreateDirectory($OutputDirectory)
. (Join-Path $ReaderDirectory 'RuntimeHarness.Common.ps1')
$script:passes=0;$script:failures=0;$timer=[Diagnostics.Stopwatch]::StartNew()
function Fixture([string]$Kind){$leaf=if($Kind-ceq'cumulative'){'CumulativeMediumFixture.json'}else{'AcquiredManualMediumFixture.json'};[IO.File]::ReadAllText((Join-Path $FixtureDirectory $leaf))|ConvertFrom-Json}
function Check([string]$Kind,$F){if($Kind-ceq'cumulative'){Assert-KmcChunk7CumulativeEvidence $F.request $F.artifact $F.source}else{Assert-KmcChunk7AcquiredManualEvidence $F.request $F.artifact $F.source}}
function Good([string]$Kind){try{Check $Kind (Fixture $Kind);$script:passes++;Write-Host ('PASS complete Medium '+$Kind)}catch{$script:failures++;Write-Host ('FAIL '+$Kind+': '+$_+' '+$_.ScriptStackTrace)}}
function Bad([string]$Kind,[string]$Name,[scriptblock]$Mutate){$f=Fixture $Kind;$refused=$false;try{&$Mutate $f;Check $Kind $f}catch{$refused=$true};if($refused){$script:passes++;Write-Host ('PASS refuses '+$Kind+' '+$Name)}else{$script:failures++;Write-Host ('FAIL accepted '+$Kind+' '+$Name)}}
Good 'cumulative';Good 'acquired'
Bad 'cumulative' 'Small body substituted' {param($f)$f.artifact.observations.field.bodyObservation.OriginalSizeOrdinal=3}
Bad 'cumulative' 'unqualified live Medium rig' {param($f)$f.artifact.observations.liveEligibility.exactMediumBodyMember=$false}
Bad 'cumulative' 'save after selector instead of before' {param($f)$f.artifact.observations.nativeWrite.saveBeforeFieldSelection=$false}
Bad 'cumulative' 'prior owner missing' {param($f)$f.request.cumulativeAcquisition.priorOwnerId=$null}
Bad 'cumulative' 'legacy Small pins borrowed' {param($f)$f.request.cumulativeAcquisition.smallRaceGuid='a'*32}
Bad 'cumulative' 'native selector skipped' {param($f)$f.artifact.observations.fieldSelection.openCount=0}
Bad 'acquired' 'Small field body substituted' {param($f)$f.artifact.observations.bodyObservation.OriginalSizeOrdinal=3}
Bad 'acquired' 'foreign Medium field rig' {param($f)$f.artifact.observations.bodyObservation.SkeletonName='Foreign'}
Bad 'acquired' 'field Manual source order substituted' {param($f)$f.source.observations.nativeWrite.saveAfterFieldSelection=$false;$f.artifact.observations.sourceAcquisition.nativeWrite.saveAfterFieldSelection=$false}
Bad 'acquired' 'unearned source selector authority' {param($f)$f.artifact.observations.sourceSavedBeforeSelector=$true}
Bad 'acquired' 'native field selector fabricated' {param($f)$f.artifact.observations.selector.openCount=1}
Bad 'acquired' 'unknown paid body case' {param($f)$f.source.observations.selection.case='paid-foreign-ranger-horse';$f.artifact.observations.sourceAcquisition.selection.case='paid-foreign-ranger-horse'}
$timer.Stop();@{schemaVersion=1;pass=$script:passes;fail=$script:failures;seconds=$timer.Elapsed.TotalSeconds;nativeExecution=$false;archiveAccess=$false;fixtureKind='synthetic-json-only';readerPins=@('Chunk7ManualDerivedEvidence.ps1','Chunk7CumulativeAcquisitionEvidence.ps1','Chunk7AcquiredManualEvidence.ps1')|ForEach-Object{@{path=$_;sha256=(Get-FileHash -LiteralPath (Join-Path $ReaderDirectory $_)).Hash.ToLowerInvariant()}};fixturePins=@('CumulativeMediumFixture.json','AcquiredManualMediumFixture.json')|ForEach-Object{@{path=$_;sha256=(Get-FileHash -LiteralPath (Join-Path $FixtureDirectory $_)).Hash.ToLowerInvariant()}}}|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
Write-Host ('TOTAL PASS='+$script:passes+' FAIL='+$script:failures);if($script:failures-ne0){throw 'Medium continuation reader checks failed.'}
