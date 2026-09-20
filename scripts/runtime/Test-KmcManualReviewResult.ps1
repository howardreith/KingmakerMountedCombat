[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ResultPath,
    [Parameter(Mandatory = $true)][string]$RequestPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')

$request = Read-KmcJson $RequestPath
$root = [IO.Path]::GetFullPath([string]$request.evidenceRoot).TrimEnd('\')
$fullResult = Assert-KmcChildPath $ResultPath $root 'manual review result'
if ([IO.Path]::GetFileName($fullResult) -cne 'manual-review-result.json' -or -not (Test-Path -LiteralPath $fullResult -PathType Leaf)) {
    throw 'Manual review result path or leaf is not exact.'
}
Assert-KmcNotReparsePoint $fullResult 'manual review result'
Assert-KmcNotHardLink $fullResult 'manual review result'
$raw = [IO.File]::ReadAllText($fullResult, (New-Object Text.UTF8Encoding($false, $true)))
Assert-KmcJsonObjectMembersUnique $raw 'manual review result'
$result = $raw | ConvertFrom-Json
$properties = @(
    'schemaVersion','evidenceKind','runId','scenario','status','branch','commit','productVersion','dllSha256','dllMvid',
    'transactionToken','startedAtUtc','completedAtUtc','reviewReady','readyAtUtc','readyEvidenceSha256','visualAcceptance',
    'processExited','modsRestored','saveProtectionPassed','baselineImmutable','workingRestored','saveWriteAllowlistPassed',
    'restoredSaveInventoryDigest','errors'
)
Assert-KmcExactProperties $result $properties 'manual review result'
if ([int]$result.schemaVersion -ne 1 -or [string]$result.evidenceKind -cne 'manual-visual-review-session' -or
    [string]$result.scenario -cne 'manual-visual-review' -or [string]$request.scenario -cne 'manual-visual-review') {
    throw 'Manual review result schema, kind, or scenario is invalid.'
}
foreach ($name in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid','transactionToken')) {
    if ([string]$result.$name -cne [string]$request.$name) { throw "Manual review result $name differs from its request." }
}
$started = [DateTimeOffset]::MinValue
$completed = [DateTimeOffset]::MinValue
$readyAt = [DateTimeOffset]::MinValue
if (-not [DateTimeOffset]::TryParse([string]$result.startedAtUtc, [ref]$started) -or
    -not [DateTimeOffset]::TryParse([string]$result.completedAtUtc, [ref]$completed) -or
    -not [DateTimeOffset]::TryParse([string]$result.readyAtUtc, [ref]$readyAt) -or
    $readyAt -lt $started -or $completed -lt $readyAt) {
    throw 'Manual review result timestamps are invalid or out of order.'
}
$readyPath = Assert-KmcChildPath (Join-Path $root 'manual-review-ready.json') $root 'manual review READY evidence'
if ([string]$result.readyEvidenceSha256 -cnotmatch '^[0-9a-f]{64}$' -or
    -not (Test-Path -LiteralPath $readyPath -PathType Leaf) -or
    (Get-KmcSha256 $readyPath) -cne [string]$result.readyEvidenceSha256) {
    throw 'Manual review result does not bind the exact READY evidence bytes.'
}
if ([string]$result.status -ceq 'PASS') {
    if ($result.reviewReady -ne $true -or [string]$result.visualAcceptance -cne 'PENDING' -or
        $result.processExited -ne $true -or $result.modsRestored -ne $true -or $result.saveProtectionPassed -ne $true -or
        $result.baselineImmutable -ne $true -or $result.workingRestored -ne $true -or
        $result.saveWriteAllowlistPassed -ne $true -or [string]$result.restoredSaveInventoryDigest -cnotmatch '^[0-9a-f]{64}$' -or
        $result.errors -isnot [Array] -or @($result.errors).Count -ne 0) {
        throw 'PASS manual review result lacks exact READY, pending acceptance, exit, restoration, or empty-error proof.'
    }
}
elseif ([string]$result.status -cne 'FAIL') {
    throw 'Manual review result status must be PASS or FAIL.'
}

Write-Host 'TOTAL PASS=24 FAIL=0'
