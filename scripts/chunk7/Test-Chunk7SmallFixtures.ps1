[CmdletBinding()]
param([string]$RepositoryRoot,[string]$RuntimeDirectory,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$RuntimeDirectory){$RuntimeDirectory=Join-Path $PSScriptRoot '../runtime'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/small-fixtures-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
. (Join-Path $RuntimeDirectory 'RuntimeHarness.Common.ps1')
. (Join-Path $RuntimeDirectory 'Chunk7SmallCalibrationFixtures.ps1')
. (Join-Path $PSScriptRoot 'Test-Chunk7SmallEvidence.ps1') -RepositoryRoot $RepositoryRoot -ReaderDirectory $RuntimeDirectory -FieldTestSource (Join-Path $PSScriptRoot 'Test-Chunk7FieldEvidence.ps1') -FieldReaderDirectory $RuntimeDirectory -FixturesOnly
$script:passes=0;$f=New-SmallFixture;$leaf=Join-Path $OutputDirectory 'selection.json';$selection=ConvertTo-KmcChunk7Canonical $f.request.smallCalibration
function Write-Selection([string]$Text){[IO.File]::WriteAllText($leaf,$Text,[Text.UTF8Encoding]::new($false))}
Good 'exact private selection intake retains byte hash and path' {Write-Selection $selection;$read=Read-KmcChunk7SmallSelectionFile $leaf $f.request.runId;if($read.path-cne$leaf-or$read.sha256-cne(Get-KmcSha256 $leaf)-or(ConvertTo-KmcChunk7Canonical $read.selection)-cne$selection){throw 'Exact private intake changed.'}}
Bad 'duplicate top-level selection member' {Write-Selection ($selection.Insert(1,'"case":"compiled-small-horse-rt",'));[void](Read-KmcChunk7SmallSelectionFile $leaf $f.request.runId)}
Bad 'numeric profile declaration' {$q=Copy-O $f.request.smallCalibration;$q.profileId=1;Write-Selection (ConvertTo-KmcChunk7Canonical $q);[void](Read-KmcChunk7SmallSelectionFile $leaf $f.request.runId)}
Bad 'request-supplied pose parameters forbidden' {$q=Copy-O $f.request.smallCalibration;$q|Add-Member -NotePropertyName pose -NotePropertyValue @{x=0};Write-Selection (ConvertTo-KmcChunk7Canonical $q);[void](Read-KmcChunk7SmallSelectionFile $leaf $f.request.runId)}
Bad 'future native write permission forbidden' {$q=Copy-O $f.request.smallCalibration;$q.source|Add-Member -NotePropertyName writable -NotePropertyValue $true;Write-Selection (ConvertTo-KmcChunk7Canonical $q);[void](Read-KmcChunk7SmallSelectionFile $leaf $f.request.runId)}
Bad 'same source and consuming run refused' {Write-Selection $selection;[void](Read-KmcChunk7SmallSelectionFile $leaf $f.request.smallCalibration.source.sourceRunId)}
Bad 'empty selection' {Write-Selection '';[void](Read-KmcChunk7SmallSelectionFile $leaf $f.request.runId)}
Bad 'oversized selection' {Write-Selection (' '*16385);[void](Read-KmcChunk7SmallSelectionFile $leaf $f.request.runId)}
Bad 'invalid JSON' {Write-Selection '{';[void](Read-KmcChunk7SmallSelectionFile $leaf $f.request.runId)}
Bad 'outside project path refused before read' {[void](Read-KmcChunk7SmallSelectionFile 'C:/kmc-unowned-selection.json' $f.request.runId)}
Write-Host ('TOTAL PASS '+$script:passes+' FAIL 0; private selection intake only, no native archive or guarded runner')
