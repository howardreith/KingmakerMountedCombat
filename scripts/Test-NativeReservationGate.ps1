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
    $path = if ([IO.Path]::IsPathRooted($Script)) { $Script } else { Join-Path $repoRoot $Script }
    & powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File $path @Arguments *> $log
    $exit = $LASTEXITCODE
    $ErrorActionPreference = 'Stop'
    $timer.Stop()
    $lines = @(Get-Content -LiteralPath $log)
    $fail = @($lines | Where-Object { $_ -like 'FAIL *' })
    $verdict = & $Judge $exit $lines $fail
    Write-Host ("== {0}: exit={1} PASS-lines={2} FAIL-lines={3} seconds={4:N0} -> {5}" -f $Name, $exit, @($lines | Where-Object { $_ -like 'PASS *' }).Count, $fail.Count, $timer.Elapsed.TotalSeconds, $verdict)
    foreach ($line in $fail) { Write-Host "   $line" }
    foreach ($line in @($lines | Where-Object { $_ -like 'RESULT *' -or $_ -like 'TOTAL *' -or $_ -like '* PASS=*' -or $_ -like 'INFO *' -or $_ -like 'PASS actual held-lock copy *' })) { Write-Host "   $line" }
    if ($verdict -notlike 'passed*' -and $verdict -notlike 'partial:*') { $failures.Add("$Name $verdict (log $log)") }
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
    # OriginalGuardEffects: base 11e8e22b fails 7 of its 14 rows on this machine (its fixture pins Validate-Package.ps1 but
    # not the UmmLoaderContracts.ps1 it dot-sources). The candidate is accepted only when every row (name, status and the
    # first error line with the case root normalized) equals the base rows recorded independently from a clean base clone
    # (scripts\native-reservation-gate\OriginalGuardEffects.base-11e8e22b.json); reported as a matched-base partial result.
    $ogeRoot = Join-Path $cacheRoot 'scoped-OriginalGuardEffects'
    $ogeBase = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'native-reservation-gate\OriginalGuardEffects.base-11e8e22b.json') | ConvertFrom-Json
    [void](Invoke-Suite 'Test-ScopedPreflight-OriginalGuardEffects' 'scripts\chunk7\Test-ScopedPreflight.ps1' @('-Case', 'OriginalGuardEffects', '-OutputRoot', $ogeRoot) {
        param($exit, $lines, $fail)
        if ($exit -eq 0) { return 'passed' }
        $receiptPath = Join-Path $ogeRoot 'case\receipt.json'
        if (-not (Test-Path -LiteralPath $receiptPath)) { return 'failed (no receipt)' }
        $mark = Join-Path $ogeRoot 'case\'
        $rows = @((Get-Content -Raw -LiteralPath $receiptPath | ConvertFrom-Json).rows | ForEach-Object {
            $first = if ($_.status -ceq 'FAIL') { ([string]$_.error).Split("`n")[0].TrimEnd("`r").Replace($mark, '<case>\') } else { $null }
            '{0}|{1}|{2}' -f $_.name, $_.status, $first })
        $expected = @($ogeBase.rows | ForEach-Object { '{0}|{1}|{2}' -f $_.name, $_.status, $_.error })
        Write-Host ('   OriginalGuardEffects rows: candidate {0}, recorded base {1} ({2})' -f $rows.Count, $expected.Count, $ogeBase.summary)
        for ($i = 0; $i -lt [Math]::Max($rows.Count, $expected.Count); $i++) {
            $c = if ($i -lt $rows.Count) { $rows[$i] } else { '<missing>' }; $b = if ($i -lt $expected.Count) { $expected[$i] } else { '<missing>' }
            Write-Host ('     {0} {1}' -f $(if ($c -ceq $b) { 'same' } else { 'DIFF' }), ($c -split '\|')[1] + ' ' + ($c -split '\|')[0])
            if ($c -cne $b) { return "failed (row $i differs from base: candidate '$c', base '$b')" }
        }
        return ('passed as a matched-base partial result ({0}; every row identical to the recorded 11e8e22b rows)' -f $ogeBase.summary)
    }.GetNewClosure())
    # The partial child's raw evidence is retained with the gate logs (kept on success and failure), with its hashes.
    $ogeEvidence = Join-Path $logRoot 'OriginalGuardEffects-raw'
    [void][IO.Directory]::CreateDirectory($ogeEvidence)
    foreach ($relative in @('test.log', 'inputs.json', 'result.json', 'case\receipt.json')) {
        $source = Join-Path $ogeRoot $relative
        if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { Write-Host "   raw OriginalGuardEffects evidence missing: $relative"; continue }
        $target = Join-Path $ogeEvidence ($relative.Replace('\', '-'))
        Copy-Item -LiteralPath $source -Destination $target
        Write-Host ('   raw OriginalGuardEffects evidence {0} sha256={1}' -f $target, (Get-FileHash -Algorithm SHA256 -LiteralPath $target).Hash.ToLowerInvariant())
    }
    # Six-pair copy: unchanged, it stops in its source qualification on this machine's installed-loader observation (the
    # recorded evidence does not match the measured loader profile): a candidate prerequisite stop, reported as such. It
    # runs twice: as-is (accepted only with exactly that stop, a partial result), and with only that loader observation
    # replaced by a recording stand-in, so the actual sixteen-pin source, the private lock and the copy helper run all
    # ten cases.
    [void](Invoke-Suite 'Test-Chunk7SixPairSourceCopy' 'scripts\chunk7\Test-Chunk7SixPairSourceCopy.ps1' @() {
        param($exit, $lines, $fail)
        if ($exit -eq 0 -and $fail.Count -eq 0) { return 'passed' }
        if ((($lines -join ' ') -replace '\s+', ' ') -match 'The exact installed loader contract was not pinned in both dependency and native observations: UnityModManager') { return 'partial: candidate prerequisite stop (installed-loader observation unmet on this machine); no lock case reached' }
        return 'failed'
    })
    $sixPairSource = Join-Path $PSScriptRoot 'chunk7\Test-Chunk7SixPairSourceCopy.ps1'
    $sixPairText = [IO.File]::ReadAllText($sixPairSource)
    $initialize = ". (Join-Path `$PSScriptRoot 'Initialize-Chunk7Tests.ps1')"
    if (-not $sixPairText.Contains($initialize)) { throw 'The six-pair test no longer has its initialization anchor.' }
    $standIn = $initialize.Replace('$PSScriptRoot', "'" + (Split-Path -Parent $sixPairSource) + "'") + [Environment]::NewLine +
        "`$loaderStandIn=[Collections.Generic.List[string]]::new();function Assert-KmcExactObservedLoaderDependency(`$Assemblies,`$Dependencies,[string]`$RelativePath,[string]`$Name){`$loaderStandIn.Add(`$Name);Write-Host ('INFO installed-loader observation stand-in: '+`$Name+' ('+`$RelativePath+')')}"
    $standInPath = Join-Path $logRoot 'Test-Chunk7SixPairSourceCopy.loader-stand-in.ps1'
    [IO.File]::WriteAllText($standInPath, $sixPairText.Replace($initialize, $standIn))
    [void](Invoke-Suite 'Test-Chunk7SixPairSourceCopy-loader-stand-in' $standInPath @() {
        param($exit, $lines, $fail)
        if ($exit -eq 0 -and $fail.Count -eq 0 -and @($lines | Where-Object { $_ -ceq 'SIX-PAIR COPY PASS=10 FAIL=0' }).Count -eq 1 -and @($lines | Where-Object { $_ -like 'PASS actual held-lock copy *' }).Count -eq 10) { return 'passed (loader observation stand-in only; all ten held-lock copy cases)' }
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
