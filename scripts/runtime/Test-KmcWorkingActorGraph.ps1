[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedQualificationSha256,
    [Parameter(Mandatory=$true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedWorkingSha256)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'WorkingActorGraphDiagnostic.ps1')
# Only stdout is emitted. No output/save/staging path or native execution option.
$result=Invoke-KmcQualifiedWorkingActorGraphDiagnostic -ExpectedQualificationSha256 $ExpectedQualificationSha256 -ExpectedWorkingSha256 $ExpectedWorkingSha256
$result|ConvertTo-Json -Depth 12
if($result.status-cne'PASS'){throw 'Working actor-graph diagnostic refused unsupported or malformed structure; see bounded observation.'}
