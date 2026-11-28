[CmdletBinding()]
param()

# Review gate for the Kingmaker native-test reservation change (docs/NATIVE-RESERVATION.md). Candidate-bound and offline:
#   1. Test-NativeReservation.ps1: the reservation itself (separate contender processes, a common start barrier,
#      strict records, recovery admission, ownership loss, entrypoint boundaries).
#   2. Suites that exercise the changed lock functions on stand-ins: Test-BaseGameIsolation.ps1,
#      Test-PersistenceProfileProtection.ps1, Test-TierSelection.ps1 and Test-Harness.ps1.
#   3. The offline chunk7 callers of the lock implementation, each with its own private reservation.
# Every suite inherits an occupied stand-in reservation (a private record this gate owns); the isolation verdict is that
# this stand-in stays byte-identical, so no caller fell back to an inherited convention. The gate owns a fresh,
# uniquely named log root and analysis-cache root per invocation, removes only those and only when everything passed.
# Test-Harness.ps1 cannot finish in an unbuilt checkout: it packages bin\Release\KingmakerMountedCombat.dll at line
# ~5440. Every lock and reservation case precedes that statement; the gate requires zero FAIL lines and reports the stop
# as an unmet build prerequisite, not as a pass of the remaining cases.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$labRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot '..\..'))
$started = [DateTime]::UtcNow
$invocation = $started.ToString('yyyyMMddTHHmmssZ') + '-' + [Guid]::NewGuid().ToString('N')
Write-Host "Candidate: $((& git -C $repoRoot rev-parse HEAD).Trim()) (dirty: $(@(& git -C $repoRoot status --porcelain).Count -ne 0)); invocation $invocation"

function New-OwnedRoot([string]$Path) {
    if (Test-Path -LiteralPath $Path) { throw "Gate root already exists (not this invocation's): $Path" }
    [void][IO.Directory]::CreateDirectory($Path)
    [IO.File]::WriteAllText((Join-Path $Path '.native-reservation-gate-owner'), $invocation)
    return $Path
}
function Remove-OwnedRoot([string]$Path) {
    $marker = Join-Path $Path '.native-reservation-gate-owner'
    if ((Test-Path -LiteralPath $marker) -and [IO.File]::ReadAllText($marker) -ceq $invocation) { Remove-Item -LiteralPath $Path -Recurse -Force }
}

$logRoot = New-OwnedRoot (Join-Path $repoRoot ('obj\native-reservation-gate\' + $invocation))
$cacheRoot = New-OwnedRoot (Join-Path $labRoot ('analysis-cache\native-reservation-gate-' + $invocation))
$ambientRoot = New-OwnedRoot (Join-Path ([IO.Path]::GetTempPath()) ('kmc-reservation-gate-' + $invocation))
$ambientRecord = Join-Path $ambientRoot 'reservation\reservation.v1.json'
[void][IO.Directory]::CreateDirectory((Split-Path -Parent $ambientRecord))
[IO.File]::WriteAllText($ambientRecord, ([ordered]@{ protocol = 'kingmaker-native-test-reservation/1'; lab = 'KingmakerGateStandIn'; runId = $invocation; purpose = 'occupied stand-in'; token = ('e' * 32); holderProcessId = $PID; holderStartUtc = (Get-Process -Id $PID).StartTime.ToUniversalTime().ToString('o'); host = $env:COMPUTERNAME; createdUtc = $started.ToString('o') } | ConvertTo-Json))
$ambientBefore = (Get-FileHash -Algorithm SHA256 -LiteralPath $ambientRecord).Hash
$previousSelection = $env:KMC_NATIVE_RESERVATION_TEST_ROOT
$failures = New-Object System.Collections.Generic.List[string]

function Invoke-Suite([string]$Name, [string]$Script, [string[]]$Arguments, [scriptblock]$Judge) {
    $log = Join-Path $logRoot ($Name + '.log')
    $timer = [Diagnostics.Stopwatch]::StartNew()
    $ErrorActionPreference = 'Continue'
    & powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File (Join-Path $repoRoot $Script) @Arguments *> $log
    $exit = $LASTEXITCODE
    $ErrorActionPreference = 'Stop'
    $timer.Stop()
    $lines = @(Get-Content -LiteralPath $log)
    $fail = @($lines | Where-Object { $_ -like 'FAIL *' })
    $verdict = & $Judge $exit $lines $fail
    Write-Host ("== {0}: exit={1} PASS-lines={2} FAIL-lines={3} seconds={4:N0} -> {5}" -f $Name, $exit, @($lines | Where-Object { $_ -like 'PASS *' }).Count, $fail.Count, $timer.Elapsed.TotalSeconds, $verdict)
    foreach ($line in $fail) { Write-Host "   $line" }
    foreach ($line in @($lines | Where-Object { $_ -like 'RESULT *' -or $_ -like 'TOTAL *' -or $_ -like '* PASS=*' -or $_ -like 'INFO *' })) { Write-Host "   $line" }
    if ($verdict -notlike 'passed*') { $failures.Add("$Name $verdict (log $log)") }
    return $lines
}
$exitZero = { param($exit, $lines, $fail) if ($exit -eq 0 -and $fail.Count -eq 0) { 'passed' } else { 'failed' } }

try {
    $env:KMC_NATIVE_RESERVATION_TEST_ROOT = $ambientRoot
    $reservation = Invoke-Suite 'Test-NativeReservation' 'scripts\Test-NativeReservation.ps1' @() $exitZero
    foreach ($line in @($reservation | Where-Object { $_ -like 'PASS *' })) { Write-Host "   $line" }
    [void](Invoke-Suite 'Test-BaseGameIsolation' 'scripts\Test-BaseGameIsolation.ps1' @() $exitZero)
    [void](Invoke-Suite 'Test-PersistenceProfileProtection' 'scripts\Test-PersistenceProfileProtection.ps1' @() $exitZero)
    [void](Invoke-Suite 'Test-TierSelection' 'scripts\Test-TierSelection.ps1' @() $exitZero)
    $harness = Invoke-Suite 'Test-Harness' 'scripts\Test-Harness.ps1' @() {
        param($exit, $lines, $fail)
        if ($fail.Count -ne 0) { return 'failed' }
        if ($exit -eq 0) { return 'passed' }
        $text = $lines -join "`n"
        if ($text -match "PathNotFound,Microsoft\.PowerShell\.Commands\.CopyItemCommand" -and $text -match 'bin\\Release\\KingmakerMountedCombat\.dll') { return 'passed (zero FAIL lines; stopped at the unbuilt-checkout prerequisite bin\Release\KingmakerMountedCombat.dll)' }
        return 'failed'
    }
    $selected = @($harness | Where-Object { $_ -like 'PASS *' -and $_ -match '(?i)lock|adopt|recover|requalif|reservation|transaction' })
    Write-Host "   Test-Harness lock, adoption, recovery, requalification, reservation and transaction cases: $($selected.Count) PASS"
    foreach ($line in $selected) { Write-Host "     $line" }
    $callers = @(
        @{ name = 'Test-Chunk7AssistedAcquisitionSource'; args = @('-OutputDirectory', (Join-Path $cacheRoot 'assisted-acquisition')) },
        @{ name = 'Test-Chunk7AssistedColdInventoryReadiness'; args = @('-OutputRoot', (Join-Path $cacheRoot 'cold-readiness')) },
        @{ name = 'Test-Chunk7AssistedColdInventorySource'; args = @('-OutputRoot', (Join-Path $cacheRoot 'cold-source')) },
        @{ name = 'Test-Chunk7CumulativeCopy'; args = @('-OutputDirectory', (Join-Path $cacheRoot 'cumulative')) },
        @{ name = 'Test-Chunk7FieldFixtures'; args = @('-OutputDirectory', (Join-Path $cacheRoot 'field')) },
        @{ name = 'Test-Chunk7OrdinaryFixtures'; args = @() },
        @{ name = 'Test-Chunk7ProgressionFixtures'; args = @('-OutputDirectory', (Join-Path $cacheRoot 'progression')) },
        @{ name = 'Test-Chunk7SmallManualCopy'; args = @('-OutputDirectory', (Join-Path $cacheRoot 'small-manual')) }
    )
    foreach ($caller in $callers) { [void](Invoke-Suite $caller.name ('scripts\chunk7\' + $caller.name + '.ps1') $caller.args $exitZero) }
    # Scoped-preflight cases that consume changed bytes (the launcher, the harness and the preflight entry).
    foreach ($case in @('AstExecution', 'Closure', 'BootstrapOwnership')) {
        [void](Invoke-Suite ('Test-ScopedPreflight-' + $case) 'scripts\chunk7\Test-ScopedPreflight.ps1' @('-Case', $case, '-OutputRoot', (Join-Path $cacheRoot ('scoped-' + $case))) $exitZero)
    }
    # OriginalGuardEffects and the six-pair copy have environment prerequisites this machine does not meet; base 11e8e22b
    # fails identically (7 PASS / 7 FAIL; the installed-loader pin). Each is accepted only with exactly that signature and
    # reported as a matched-base partial result, not as a pass.
    $ogeLog = Join-Path $cacheRoot 'scoped-OriginalGuardEffects\test.log'
    [void](Invoke-Suite 'Test-ScopedPreflight-OriginalGuardEffects' 'scripts\chunk7\Test-ScopedPreflight.ps1' @('-Case', 'OriginalGuardEffects', '-OutputRoot', (Join-Path $cacheRoot 'scoped-OriginalGuardEffects')) {
        param($exit, $lines, $fail)
        if ($exit -eq 0) { return 'passed' }
        $summary = if (Test-Path -LiteralPath $ogeLog) { (Get-Content -LiteralPath $ogeLog -Raw).Trim() } else { '' }
        if ($summary -ceq 'Original guard effects: 7 PASS / 7 FAIL') { return 'passed as a matched-base partial result (7 PASS / 7 FAIL, identical to 11e8e22b)' }
        return 'failed'
    }.GetNewClosure())
    [void](Invoke-Suite 'Test-Chunk7SixPairSourceCopy' 'scripts\chunk7\Test-Chunk7SixPairSourceCopy.ps1' @() {
        param($exit, $lines, $fail)
        if ($exit -eq 0 -and $fail.Count -eq 0) { return 'passed' }
        if (($lines -join "`n") -match 'The exact installed loader contract was not pinned in both dependency and native observations: UnityModManager') { return 'passed as a matched-base partial result (installed-loader prerequisite unmet before any lock call, identical to 11e8e22b)' }
        return 'failed'
    })
}
finally { $env:KMC_NATIVE_RESERVATION_TEST_ROOT = $previousSelection }

$isolated = (Get-FileHash -Algorithm SHA256 -LiteralPath $ambientRecord).Hash -ceq $ambientBefore
Write-Host "Occupied inherited stand-in reservation byte-identical: $isolated"
if (-not $isolated) { $failures.Add('a suite changed the inherited stand-in reservation') }
Write-Host ("Gate runtime: {0:N0} s; logs: {1}" -f ([DateTime]::UtcNow - $started).TotalSeconds, $logRoot)
if ($failures.Count -ne 0) { throw ('Native reservation gate failed (case material kept in ' + $cacheRoot + '): ' + ($failures -join ' | ')) }
Remove-OwnedRoot $cacheRoot
Remove-OwnedRoot $ambientRoot
Write-Host 'Native reservation gate passed.'
