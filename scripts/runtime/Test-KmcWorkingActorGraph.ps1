[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedQualificationSha256,
    [Parameter(Mandatory=$true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedWorkingSha256)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'WorkingActorGraphDiagnostic.ps1')
# Only stdout is emitted. No output/save/staging path or native execution option.
# Kingmaker native-test reservation (docs/NATIVE-RESERVATION.md): the diagnostic holds a write-denying handle on the live
# Working save, so it reserves before its first shared read and releases after its last (no lock is taken).
$nativeReservation=Enter-KmcNativeReservation -StateRoot (Join-Path (Get-KmcLabRoot) 'runtime-state') -RunId ([DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffffffZ')+'-working-graph') -Purpose 'preparation-working-diagnostic'
$diagnosticFailed=$false
try{$result=Invoke-KmcQualifiedWorkingActorGraphDiagnostic -ExpectedQualificationSha256 $ExpectedQualificationSha256 -ExpectedWorkingSha256 $ExpectedWorkingSha256}
catch{$diagnosticFailed=$true;throw}
finally{Exit-KmcUnlockedNativeReservation $nativeReservation -PreserveError:$diagnosticFailed}
$result|ConvertTo-Json -Depth 12
if($result.status-cne'PASS'){throw 'Working actor-graph diagnostic refused unsupported or malformed structure; see bounded observation.'}
