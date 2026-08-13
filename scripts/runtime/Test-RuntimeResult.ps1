[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ResultPath,
    [Parameter(Mandatory = $true)][string]$RequestPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')

$result = Read-KmcJson $ResultPath
$request = Read-KmcJson $RequestPath
$required = @('schemaVersion', 'runId', 'scenario', 'status', 'branch', 'commit', 'productVersion', 'dllSha256', 'dllMvid', 'transactionToken', 'startedAtUtc', 'completedAtUtc', 'modsRestored', 'saveProtectionPassed', 'gameResultSha256', 'errors')
$actual = @($result.PSObject.Properties.Name | Sort-Object)
if (($actual -join "`n") -cne (($required | Sort-Object) -join "`n")) {
    throw "Runtime result property set is not exact: $($actual -join ', ')"
}

if ([int]$result.schemaVersion -ne 1) { throw 'Runtime result schemaVersion must be 1.' }
foreach ($name in @('runId', 'scenario', 'branch', 'commit', 'productVersion', 'dllSha256', 'dllMvid', 'transactionToken')) {
    if ([string]$result.$name -cne [string]$request.$name) {
        throw "Runtime result identity mismatch: $name"
    }
}
if ([string]$result.status -cnotin @('PASS', 'FAIL')) { throw 'Runtime result status must be PASS or FAIL.' }
$started = [DateTimeOffset]::MinValue
$completed = [DateTimeOffset]::MinValue
if (-not [DateTimeOffset]::TryParse([string]$result.startedAtUtc, [ref]$started) -or
    -not [DateTimeOffset]::TryParse([string]$result.completedAtUtc, [ref]$completed) -or
    $completed -lt $started) {
    throw 'Runtime result timestamps are invalid or reversed.'
}
if ($result.modsRestored -ne $true) { throw 'Runtime result does not prove exact Mods restoration.' }
if ($result.saveProtectionPassed -ne $true) { throw 'Runtime result does not prove save protection.' }
if ($null -eq $result.errors -or $result.errors -is [string]) { throw 'Runtime result errors must be an array.' }
if ([string]$result.status -ceq 'PASS' -and @($result.errors).Count -ne 0) { throw 'PASS runtime result contains errors.' }
if ([string]$result.status -ceq 'PASS' -and [string]$result.gameResultSha256 -notmatch '^[0-9a-f]{64}$') { throw 'PASS runtime result lacks the atomic game-result hash.' }

Write-Host 'TOTAL PASS=11 FAIL=0'
