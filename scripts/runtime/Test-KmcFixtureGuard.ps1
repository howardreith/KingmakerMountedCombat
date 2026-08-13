[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Medium')]
param(
    [Parameter(Mandatory = $true)][string]$SaveRoot,
    [Parameter(Mandatory = $true)][string]$StateRoot,
    [switch]$InitializeQualification
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
$requestedWhatIf = [bool]$WhatIfPreference
$WhatIfPreference = $false

$fullStateRoot = [IO.Path]::GetFullPath($StateRoot).TrimEnd('\')
if (-not (Test-Path -LiteralPath $fullStateRoot -PathType Container)) {
    throw "Runtime state root is missing: $fullStateRoot"
}
Assert-KmcNotReparsePoint $fullStateRoot 'runtime state root'
$qualificationPath = Assert-KmcChildPath (Join-Path $fullStateRoot 'fixture-qualification.json') $fullStateRoot 'fixture qualification'
$audit = Get-KmcFixtureCandidateAudit $SaveRoot
Write-Host "Exact filename audit: baseline=$($audit.baselineCount); working=$($audit.workingCount)."
if ($audit.rejectedKmcLookingNames.Count -ne 0) {
    Write-Host "Rejected KMC-looking filenames: $($audit.rejectedKmcLookingNames -join ', ')"
}

$pair = Get-KmcValidatedFixturePair -SaveRoot $SaveRoot
$WhatIfPreference = $requestedWhatIf
if ($InitializeQualification -and -not (Test-Path -LiteralPath $qualificationPath)) {
    if (-not $PSCmdlet.ShouldProcess($qualificationPath, 'record immutable KMC fixture qualification')) {
        $WhatIfPreference = $false
        Write-Host 'Fixture guard WhatIf PASS; both exact descriptors were read, no qualification was written, and no save was loaded or mutated.'
        return
    }
}
$WhatIfPreference = $false
$pair = Assert-KmcFixtureQualification -Pair $pair -QualificationPath $qualificationPath -InitializeQualification:$InitializeQualification
Write-Host 'KMC fixture guard PASS.'
Write-Host "Baseline: $($pair.baseline.fileName) / $($pair.baseline.sha256)"
Write-Host "Working:  $($pair.working.fileName) / $($pair.working.sha256)"
Write-Host "Campaign: $($pair.expectedGameName) / $($pair.expectedGameId) / $($pair.expectedArea)"
Write-Host 'Writable save allowlist: KMC_AUTOMATION_WORKING only.'
