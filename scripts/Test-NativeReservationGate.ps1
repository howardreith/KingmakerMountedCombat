[CmdletBinding()]
param()

# Review gate for the Kingmaker native-test reservation change (docs/NATIVE-RESERVATION.md). Candidate-bound and offline:
#   1. Test-NativeReservation.ps1: the reservation itself (separate contender processes, a common start barrier, run
#      binding, resume/adopt/repair, strict records, ownership loss, handoff, entrypoint ordering).
#   2. The suites that exercise the changed admission functions on stand-ins: Test-BaseGameIsolation.ps1,
#      Test-PersistenceProfileProtection.ps1, Test-TierSelection.ps1 and Test-Harness.ps1 (fixture guard,
#      requalification, qualified-fixture recovery including resumption, lock and adoption cases).
#   3. The offline chunk7 callers of the lock implementation (F9), each with its own private reservation; they write
#      only below the lab's analysis-cache, which their own guards require.
# Every suite runs with an occupied stand-in reservation in the inherited environment; it must stay byte-identical (no
# caller falls back to an ambient convention) and the machine-wide record must be unchanged. Test-Harness.ps1 stops at
# a known point in an unbuilt checkout (it packages bin\Release\KingmakerMountedCombat.dll, line ~5436; baseline
# 11e8e22b stops at the same statement); every lock and reservation case precedes it, and the gate requires zero FAIL
# lines and exactly that stop.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$labRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot '..\..'))
$started = [DateTime]::UtcNow
Write-Host "Candidate: $((& git -C $repoRoot rev-parse HEAD).Trim()) (dirty: $(@(& git -C $repoRoot status --porcelain).Count -ne 0))"

$production = Join-Path $env:ProgramData 'KingmakerNativeTest\reservation.v1.json'
function Get-ProductionState { if (Test-Path -LiteralPath $production) { (Get-FileHash -Algorithm SHA256 -LiteralPath $production).Hash } else { 'absent' } }
$productionBefore = Get-ProductionState

# The occupied stand-in every suite inherits (a caller that used the inherited convention would refuse or change it).
$ambientRoot = Join-Path ([IO.Path]::GetTempPath()) ('kmc-reservation-gate-ambient-' + [Guid]::NewGuid().ToString('N'))
$ambientRecord = Join-Path $ambientRoot 'reservation\reservation.v1.json'
[void][IO.Directory]::CreateDirectory((Split-Path -Parent $ambientRecord))
[IO.File]::WriteAllText($ambientRecord, ([ordered]@{ protocol = 'kingmaker-native-test-reservation/1'; lab = 'KingmakerGateStandIn'; runId = 'occupied'; purpose = 'stand-in for an occupied reservation'; token = ('e' * 32); holderProcessId = $PID; holderStartUtc = (Get-Process -Id $PID).StartTime.ToUniversalTime().ToString('o') } | ConvertTo-Json))
$ambientBefore = (Get-FileHash -Algorithm SHA256 -LiteralPath $ambientRecord).Hash
$previousAmbient = $env:KMC_NATIVE_RESERVATION_TEST_ROOT
$env:KMC_NATIVE_RESERVATION_TEST_ROOT = $ambientRoot

$failures = New-Object System.Collections.Generic.List[string]
$logRoot = Join-Path $repoRoot ('obj\native-reservation-gate\' + $started.ToString('yyyyMMddTHHmmssZ'))
[void][IO.Directory]::CreateDirectory($logRoot)

function Invoke-Suite([string]$Name, [string]$Script, [string[]]$Arguments = @(), [scriptblock]$Judge) {
    $log = Join-Path $logRoot ($Name + '.log')
    $timer = [Diagnostics.Stopwatch]::StartNew()
    $ErrorActionPreference = 'Continue'
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repoRoot $Script) @Arguments *> $log
    $exit = $LASTEXITCODE
    $ErrorActionPreference = 'Stop'
    $timer.Stop()
    $lines = @(Get-Content -LiteralPath $log)
    $pass = @($lines | Where-Object { $_ -like 'PASS *' }).Count
    $fail = @($lines | Where-Object { $_ -like 'FAIL *' })
    $verdict = & $Judge $exit $lines $fail
    Write-Host ("== {0}: exit={1} PASS-lines={2} FAIL-lines={3} seconds={4:N0} -> {5}" -f $Name, $exit, $pass, $fail.Count, $timer.Elapsed.TotalSeconds, $verdict)
    foreach ($line in $fail) { Write-Host "   $line" }
    foreach ($line in @($lines | Where-Object { $_ -like 'RESULT *' -or $_ -like 'TOTAL *' -or $_ -like '* PASS=*' -or $_ -like 'INFO *' })) { Write-Host "   $line" }
    if ($verdict -notlike 'passed*') { $failures.Add("$Name $verdict (log $log)") }
    return $lines
}

$exitZero = { param($exit, $lines, $fail) if ($exit -eq 0 -and $fail.Count -eq 0) { 'passed' } else { 'failed' } }

try {
    $reservation = Invoke-Suite 'Test-NativeReservation' 'scripts\Test-NativeReservation.ps1' @() $exitZero
    foreach ($line in @($reservation | Where-Object { $_ -like 'PASS *' })) { Write-Host "   $line" }
    [void](Invoke-Suite 'Test-BaseGameIsolation' 'scripts\Test-BaseGameIsolation.ps1' @() $exitZero)
    [void](Invoke-Suite 'Test-PersistenceProfileProtection' 'scripts\Test-PersistenceProfileProtection.ps1' @() $exitZero)
    [void](Invoke-Suite 'Test-TierSelection' 'scripts\Test-TierSelection.ps1' @() $exitZero)
    $harness = Invoke-Suite 'Test-Harness' 'scripts\Test-Harness.ps1' @() {
        param($exit, $lines, $fail)
        $text = $lines -join "`n"
        if ($fail.Count -ne 0) { return 'failed' }
        if ($exit -eq 0) { return 'passed' }
        if ($text -match 'bin\\Release\\KingmakerMountedCombat\.dll' -and $text -match 'Copy-Item -LiteralPath \(Join-Path \$repoRoot ''bin\\Release\\Kingmake') { return 'passed up to the known unbuilt-checkout stop (matched baseline 11e8e22b)' }
        return 'failed'
    }
    $selected = @($harness | Where-Object { $_ -like 'PASS *' -and $_ -match '(?i)lock|adopt|recover|requalif|reservation|fixture guard|transaction' })
    Write-Host "   Test-Harness directly affected cases (lock, adoption, recovery, requalification, reservation, guard, transaction): $($selected.Count) PASS"
    foreach ($line in $selected) { Write-Host "     $line" }

    $cache = Join-Path $labRoot 'analysis-cache'
    $stamp = $started.ToString('yyyyMMddTHHmmssZ')
    $callers = @(
        @{ name = 'Test-Chunk7AssistedAcquisitionSource'; args = @('-OutputDirectory', (Join-Path $cache "native-reservation-gate-$stamp\assisted-acquisition")) },
        @{ name = 'Test-Chunk7AssistedColdInventoryReadiness'; args = @('-OutputRoot', (Join-Path $cache "native-reservation-gate-$stamp\cold-readiness")) },
        @{ name = 'Test-Chunk7AssistedColdInventorySource'; args = @('-OutputRoot', (Join-Path $cache "native-reservation-gate-$stamp\cold-source")) },
        @{ name = 'Test-Chunk7CumulativeCopy'; args = @('-OutputDirectory', (Join-Path $cache "native-reservation-gate-$stamp\cumulative")) },
        @{ name = 'Test-Chunk7FieldFixtures'; args = @('-OutputDirectory', (Join-Path $cache "native-reservation-gate-$stamp\field")) },
        @{ name = 'Test-Chunk7OrdinaryFixtures'; args = @() },
        @{ name = 'Test-Chunk7ProgressionFixtures'; args = @('-OutputDirectory', (Join-Path $cache "native-reservation-gate-$stamp\progression")) },
        @{ name = 'Test-Chunk7SmallManualCopy'; args = @('-OutputDirectory', (Join-Path $cache "native-reservation-gate-$stamp\small-manual")) }
    )
    foreach ($caller in $callers) { [void](Invoke-Suite $caller.name ('scripts\chunk7\' + $caller.name + '.ps1') $caller.args $exitZero) }
}
finally {
    $env:KMC_NATIVE_RESERVATION_TEST_ROOT = $previousAmbient
}

$ambientAfter = (Get-FileHash -Algorithm SHA256 -LiteralPath $ambientRecord).Hash
$productionAfter = Get-ProductionState
Write-Host "Occupied inherited stand-in reservation byte-identical: $($ambientAfter -ceq $ambientBefore)"
Write-Host "Machine-wide reservation record before/after: $productionBefore / $productionAfter"
if ($ambientAfter -cne $ambientBefore) { $failures.Add('a suite changed the inherited stand-in reservation') }
if ($productionAfter -cne $productionBefore) { $failures.Add('the machine-wide reservation record changed during the gate') }
Remove-Item -LiteralPath $ambientRoot -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath (Join-Path (Join-Path $labRoot 'analysis-cache') ('native-reservation-gate-' + $started.ToString('yyyyMMddTHHmmssZ'))) -Recurse -Force -ErrorAction SilentlyContinue
Write-Host ("Gate runtime: {0:N0} s; logs: {1}" -f ([DateTime]::UtcNow - $started).TotalSeconds, $logRoot)
if ($failures.Count -ne 0) { throw ('Native reservation gate failed: ' + ($failures -join ' | ')) }
Write-Host 'Native reservation gate passed.'
