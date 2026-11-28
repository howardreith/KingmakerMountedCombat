[CmdletBinding()]
param()

# Self-test of the Kingmaker native-test reservation (docs/NATIVE-RESERVATION.md) as KMC implements it in
# scripts/runtime/RuntimeHarness.Common.ps1: admission (Open-KmcRuntimeLock), release (Close-KmcRuntimeLock), unresolved
# transactions (Abandon-KmcRuntimeLock) and recovery admission (Adopt-KmcStaleRuntimeLock). Contenders are separate
# powershell.exe processes (scripts/NativeReservationContender.ps1) on two distinct stand-in runtime-state roots that
# share only a self-test reservation (a private mutex name and record path). Never touches the machine-wide reservation,
# the lab's runtime state, the game or the owner's profile. Emits PASS/FAIL lines and a RESULT line.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime\RuntimeHarness.Common.ps1')
$defaultConvention = $script:KmcReservationConvention

$root = Join-Path ([IO.Path]::GetTempPath()) ('kmc-reservation-selftest-' + [Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($root)
$convention = Use-KmcTestReservationConvention $root
$results = New-Object System.Collections.Generic.List[string]
$contenders = New-Object System.Collections.Generic.List[System.Diagnostics.Process]
$caseNumber = 0

function Assert-That([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }

function Assert-Throws([scriptblock]$Body, [string]$Fragment, [string]$Message) {
    try { & $Body }
    catch {
        if ($_.Exception.Message -notlike "*$Fragment*") { throw "$Message threw '$($_.Exception.Message)', expected '*$Fragment*'" }
        return
    }
    throw "$Message did not throw"
}

function Stop-Contenders {
    foreach ($process in $contenders) { if (-not $process.HasExited) { $process.Kill(); [void]$process.WaitForExit(5000) } }
    $contenders.Clear()
}

function Invoke-Case([string]$Name, [scriptblock]$Body) {
    $script:caseNumber++
    $case = Join-Path $root ('case' + $script:caseNumber)
    [void][IO.Directory]::CreateDirectory($case)
    try {
        & $Body $case
        $results.Add("PASS $Name")
    }
    catch { $results.Add("FAIL ${Name}: $($_.Exception.Message)") }
    finally {
        Stop-Contenders
        if (Test-Path -LiteralPath $convention.RecordPath) { Remove-Item -LiteralPath $convention.RecordPath -Force }
    }
}

function Get-Sha([string]$Path) { return (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash }

# A record as another writer would leave it.
function Set-Record([string]$Holder, [int]$HolderProcessId, [string]$HolderStartUtc, [string]$RunId = 'elsewhere') {
    [void][IO.Directory]::CreateDirectory((Split-Path -Parent $convention.RecordPath))
    $record = [ordered]@{ protocol = 'kingmaker-native-test-reservation/1'; lab = $Holder; runId = $RunId; purpose = 'stand-in'
        token = [Guid]::NewGuid().ToString('N'); holderProcessId = $HolderProcessId; holderStartUtc = $HolderStartUtc
        host = $env:COMPUTERNAME; createdUtc = [DateTime]::UtcNow.ToString('o') }
    [IO.File]::WriteAllText($convention.RecordPath, ($record | ConvertTo-Json))
}

function Get-DeadProcessId {
    $process = Start-Process -FilePath (Join-Path $env:WINDIR 'System32\PING.EXE') -ArgumentList @('-n', '1', '127.0.0.1') -WindowStyle Hidden -PassThru
    [void]$process.WaitForExit(10000)
    return $process.Id
}

# A stale KMC lock and reservation, as a crashed transaction leaves them (its process gone).
function New-AbandonedTransaction([string]$StateRoot, [string]$RunId) {
    $lock = Open-KmcRuntimeLock -StateRoot $StateRoot -RunId $RunId
    Abandon-KmcRuntimeLock $lock
    $payload = Read-KmcJson $lock.Path
    $payload.ownerProcessId = 2147483646
    Write-KmcJsonAtomic -Path $lock.Path -Value $payload
    $record = Read-KmcJson $convention.RecordPath
    $record.holderProcessId = Get-DeadProcessId
    [IO.File]::WriteAllText($convention.RecordPath, ($record | ConvertTo-Json -Depth 4))
    return $lock
}

function Start-Contender([string]$StateRoot, [string]$Name, [string]$Signals) {
    $arguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', ('"' + (Join-Path $PSScriptRoot 'NativeReservationContender.ps1') + '"'),
        '-StateRoot', ('"' + $StateRoot + '"'), '-Name', $Name, '-Signals', ('"' + $Signals + '"'),
        '-MutexName', $convention.MutexName, '-RecordPath', ('"' + $convention.RecordPath + '"'))
    $process = Start-Process -FilePath 'powershell.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $contenders.Add($process)
}

function Wait-Signal([string]$Signals, [string]$Name, [string[]]$What, [int]$Seconds = 90) {
    $until = [DateTime]::UtcNow.AddSeconds($Seconds)
    while ([DateTime]::UtcNow -lt $until) {
        foreach ($candidate in $What) {
            $path = Join-Path $Signals "$Name.$candidate"
            if (Test-Path -LiteralPath $path) { return [pscustomobject]@{ What = $candidate; Text = [IO.File]::ReadAllText($path) } }
        }
        Start-Sleep -Milliseconds 50
    }
    throw "contender $Name reached none of: $($What -join ', ')"
}

function Send-Go([string]$Signals, [string]$Name, [string]$Phase) { [IO.File]::WriteAllText((Join-Path $Signals "$Name.$Phase.go"), 'go') }

# A loser arriving while the winner is inside a mutation section: refused by the reservation, with no lock and no
# mutation marker in its own root, and the winner's record unchanged.
function Assert-LoserRefused([string]$LoserRoot, [string]$Name, [string]$Signals, [string]$WinnerRunId) {
    Start-Contender $LoserRoot $Name $Signals
    $outcome = Wait-Signal $Signals $Name @('refused', 'prepare')
    if ($outcome.What -ne 'refused') { throw "loser $Name entered its mutation section while the winner held the reservation" }
    Assert-That ($outcome.Text -like "*reserved by KingmakerMountedCombat run $WinnerRunId*") "loser $Name was refused for another reason: $($outcome.Text)"
    Assert-That (-not (Test-Path -LiteralPath (Join-Path $LoserRoot 'active-transaction.lock'))) "loser $Name left a lock"
    Assert-That (@(Get-ChildItem -LiteralPath (Join-Path $LoserRoot 'mutations') -Filter "$Name.*" -ErrorAction SilentlyContinue).Count -eq 0) "loser $Name mutated its root"
    Assert-That ([string](Read-KmcReservationRecord $convention).runId -ceq $WinnerRunId) "the winner's record changed while $Name was refused"
}

# The winner holds through prepare, run and restore; a loser is refused in each; after release the loser enters.
function Invoke-ArrivalOrder([string]$WinnerRoot, [string]$Winner, [string]$LoserRoot, [string]$Loser, [string]$Signals) {
    Start-Contender $WinnerRoot $Winner $Signals
    foreach ($phase in @('prepare', 'run', 'restore')) {
        $outcome = Wait-Signal $Signals $Winner @($phase, 'refused')
        if ($outcome.What -eq 'refused') { throw "winner $Winner was refused: $($outcome.Text)" }
        Assert-LoserRefused $LoserRoot "$Loser-during-$phase" $Signals "contender-$Winner"
        Send-Go $Signals $Winner $phase
    }
    [void](Wait-Signal $Signals $Winner @('released'))
    Assert-That (-not (Test-Path -LiteralPath $convention.RecordPath)) "winner $Winner left its reservation"
    $after = "$Loser-after"
    Start-Contender $LoserRoot $after $Signals
    foreach ($phase in @('prepare', 'run', 'restore')) {
        $outcome = Wait-Signal $Signals $after @($phase, 'refused')
        if ($outcome.What -eq 'refused') { throw "$after was refused after the winner released: $($outcome.Text)" }
        Send-Go $Signals $after $phase
    }
    [void](Wait-Signal $Signals $after @('released'))
}

try {
    Invoke-Case 'the production convention is neutral and machine-wide' {
        param($case)
        Assert-That ([string]$defaultConvention.MutexName -ceq 'Global\KingmakerNativeTest.Reservation.v1') "mutex name $($defaultConvention.MutexName)"
        Assert-That ([string]$defaultConvention.RecordPath -eq (Join-Path $env:ProgramData 'KingmakerNativeTest\reservation.v1.json')) "record path $($defaultConvention.RecordPath)"
        $production = Get-KmcDefaultReservationConvention
        Assert-That ($production.MutexName -ceq $defaultConvention.MutexName -and $production.RecordPath -eq $defaultConvention.RecordPath) 'default convention drifted'
        $lab = (Get-KmcLabRoot).TrimEnd('\') + '\'
        Assert-That (-not ([IO.Path]::GetFullPath($production.RecordPath)).StartsWith($lab, [StringComparison]::OrdinalIgnoreCase)) 'the record lives in the KMC lab'
    }

    Invoke-Case 'a lab alone (no peer, no reservation folder yet) is admitted and releases' {
        param($case)
        Assert-That (-not (Test-Path -LiteralPath (Split-Path -Parent $convention.RecordPath))) 'stand-in reservation folder pre-exists'
        $state = Join-Path $case 'state'
        $lock = Open-KmcRuntimeLock -StateRoot $state -RunId 'alone'
        try {
            $record = Read-KmcReservationRecord $convention
            Assert-That ([string]$record.lab -ceq 'KingmakerMountedCombat' -and [string]$record.runId -ceq 'alone' -and [string]$record.token -ceq [string]$lock.Reservation.token -and [int]$record.holderProcessId -eq $PID) 'unexpected record'
            Assert-That (Test-KmcNativeReservationHeld $lock.Reservation) 'not held'
            [void](Assert-KmcRuntimeLockOwner $lock)
        }
        finally { Close-KmcRuntimeLock $lock }
        Assert-That (-not (Test-Path -LiteralPath $convention.RecordPath)) 'reservation left behind'
        Assert-That (-not (Test-Path -LiteralPath (Join-Path $state 'active-transaction.lock'))) 'lock left behind'
    }

    Invoke-Case 'another lab''s reservation refuses entry before the KMC lock' {
        param($case)
        Set-Record 'KingmakerFlight' 4 '2020-01-01T00:00:00.0000000Z' 'flight-run'
        $before = Get-Sha $convention.RecordPath
        $state = Join-Path $case 'state'
        Assert-Throws { Open-KmcRuntimeLock -StateRoot $state -RunId 'blocked' } 'reserved by KingmakerFlight run flight-run' 'entry under a foreign reservation'
        Assert-That (-not (Test-Path -LiteralPath (Join-Path $state 'active-transaction.lock'))) 'the KMC lock was taken'
        Assert-That ((Get-Sha $convention.RecordPath) -ceq $before) 'the foreign record changed'
    }

    Invoke-Case 'an unreadable reservation record refuses entry and recovery and is left for the owner' {
        param($case)
        [void][IO.Directory]::CreateDirectory((Split-Path -Parent $convention.RecordPath))
        [IO.File]::WriteAllText($convention.RecordPath, 'not json')
        $state = Join-Path $case 'state'
        Assert-Throws { Open-KmcRuntimeLock -StateRoot $state -RunId 'blocked' } 'unreadable or of an unknown protocol' 'entry over an unreadable record'
        [void][IO.Directory]::CreateDirectory($state)
        [IO.File]::WriteAllText((Join-Path $state 'active-transaction.lock'), '{}')
        Assert-Throws { Adopt-KmcStaleRuntimeLock $state } 'unreadable or of an unknown protocol' 'recovery over an unreadable record'
        Assert-That ([IO.File]::ReadAllText($convention.RecordPath) -ceq 'not json') 'the record changed'
    }

    Invoke-Case 'two labs'' roots exclude each other through the reservation alone, in either arrival order' {
        param($case)
        $signals = Join-Path $case 'signals'
        [void][IO.Directory]::CreateDirectory($signals)
        Invoke-ArrivalOrder (Join-Path $case 'a') 'A' (Join-Path $case 'b') 'B' $signals
        Invoke-ArrivalOrder (Join-Path $case 'b') 'B2' (Join-Path $case 'a') 'A2' $signals
    }

    Invoke-Case 'simultaneous arrivals admit exactly one contender' {
        param($case)
        $signals = Join-Path $case 'signals'
        [void][IO.Directory]::CreateDirectory($signals)
        foreach ($round in 1..3) {
            Start-Contender (Join-Path $case 'x') "X$round" $signals
            Start-Contender (Join-Path $case 'y') "Y$round" $signals
            $x = Wait-Signal $signals "X$round" @('prepare', 'refused')
            $y = Wait-Signal $signals "Y$round" @('prepare', 'refused')
            Assert-That (@(@($x.What, $y.What) | Where-Object { $_ -eq 'prepare' }).Count -eq 1) "round ${round}: $($x.What) / $($y.What)"
            $winner = if ($x.What -eq 'prepare') { "X$round" } else { "Y$round" }
            foreach ($phase in @('prepare', 'run', 'restore')) {
                [void](Wait-Signal $signals $winner @($phase))
                Send-Go $signals $winner $phase
            }
            [void](Wait-Signal $signals $winner @('released'))
        }
    }

    Invoke-Case 'an abandoned transaction keeps its reservation and lock' {
        param($case)
        $state = Join-Path $case 'state'
        $lock = Open-KmcRuntimeLock -StateRoot $state -RunId 'unresolved'
        Abandon-KmcRuntimeLock $lock
        $record = Read-KmcReservationRecord $convention
        Assert-That ($null -ne $record -and [string]$record.token -ceq [string]$lock.Reservation.token) 'the reservation was released over an unresolved transaction'
        Assert-That (Test-Path -LiteralPath (Join-Path $state 'active-transaction.lock')) 'the lock was removed'
        Assert-Throws { Open-KmcRuntimeLock -StateRoot (Join-Path $case 'other') -RunId 'next' } 'reserved by KingmakerMountedCombat run unresolved' 'a new transaction before recovery'
    }

    Invoke-Case 'recovery adopts only KMC''s own abandoned reservation, then releases it on close' {
        param($case)
        $state = Join-Path $case 'state'
        $stale = New-AbandonedTransaction $state 'crashed'
        Assert-Throws { Open-KmcRuntimeLock -StateRoot (Join-Path $case 'other') -RunId 'next' } 'reserved by KingmakerMountedCombat run crashed' 'a new transaction over the abandoned reservation'
        $adopted = Adopt-KmcStaleRuntimeLock $state
        try {
            Assert-That ([string]$adopted.Token -ceq [string]$stale.Token) 'adopted lock token changed'
            $record = Read-KmcReservationRecord $convention
            Assert-That ([string]$record.token -ceq [string]$adopted.Reservation.token -and [string]$record.adopted.runId -ceq 'crashed' -and [int]$record.holderProcessId -eq $PID) 'the reservation was not adopted'
        }
        finally { Close-KmcRuntimeLock $adopted }
        Assert-That (-not (Test-Path -LiteralPath $convention.RecordPath)) 'the adopted reservation was not released'
        Assert-That (-not (Test-Path -LiteralPath (Join-Path $state 'active-transaction.lock'))) 'the adopted lock was not released'
    }

    Invoke-Case 'recovery never adopts another lab''s abandoned reservation or a live KMC holder''s' {
        param($case)
        $state = Join-Path $case 'state'
        [void](New-AbandonedTransaction $state 'crashed')
        $lockBefore = Get-Sha (Join-Path $state 'active-transaction.lock')
        Set-Record 'KingmakerWakingRune' (Get-DeadProcessId) '2020-01-01T00:00:00.0000000Z' 'wr-run'
        $before = Get-Sha $convention.RecordPath
        Assert-Throws { Adopt-KmcStaleRuntimeLock $state } 'reserved by KingmakerWakingRune run wr-run' 'recovery over a foreign record'
        Assert-That ((Get-Sha $convention.RecordPath) -ceq $before) 'the foreign record changed'
        Set-Record 'KingmakerMountedCombat' $PID ((Get-Process -Id $PID).StartTime.ToUniversalTime().ToString('o')) 'live'
        Assert-Throws { Adopt-KmcStaleRuntimeLock $state } 'reserved by KingmakerMountedCombat run live' 'recovery over a live holder'
        Assert-That ((Get-Sha (Join-Path $state 'active-transaction.lock')) -ceq $lockBefore) 'the stale lock was changed'
    }

    Invoke-Case 'a refused recovery keeps an adopted reservation and releases a fresh one' {
        param($case)
        $state = Join-Path $case 'state'
        [void][IO.Directory]::CreateDirectory($state)
        # No stale lock to adopt and no record: the fresh reservation taken for the attempt is released.
        Assert-Throws { Adopt-KmcStaleRuntimeLock $state } '' 'adoption without a stale lock'
        Assert-That (-not (Test-Path -LiteralPath $convention.RecordPath)) 'a refused recovery kept its fresh reservation'
        # KMC's abandoned record but no adoptable lock: the record keeps standing for the unresolved transaction.
        Set-Record 'KingmakerMountedCombat' (Get-DeadProcessId) '2020-01-01T00:00:00.0000000Z' 'crashed'
        Assert-Throws { Adopt-KmcStaleRuntimeLock $state } '' 'adoption without a stale lock'
        $record = Read-KmcReservationRecord $convention
        Assert-That ($null -ne $record -and $null -ne $record.PSObject.Properties['adopted'] -and [string]$record.adopted.runId -ceq 'crashed') ("the adopted reservation was released by a refused recovery: " + ($record | ConvertTo-Json -Compress))
    }

    Invoke-Case 'a lost reservation fails the close and the new holder''s record is left alone' {
        param($case)
        $state = Join-Path $case 'state'
        $lock = Open-KmcRuntimeLock -StateRoot $state -RunId 'lost'
        Set-Record 'KingmakerBuffPlanner' 4 '2020-01-01T00:00:00.0000000Z' 'kbp-run'
        Assert-That (-not (Test-KmcNativeReservationHeld $lock.Reservation)) 'a lost reservation still counts as held'
        Assert-Throws { Close-KmcRuntimeLock $lock } 'no longer this transaction' 'release of a lost reservation'
        Assert-That ([string](Read-KmcReservationRecord $convention).lab -ceq 'KingmakerBuffPlanner') 'the new holder''s record was removed'
        Assert-That (-not (Test-Path -LiteralPath (Join-Path $state 'active-transaction.lock'))) 'the KMC lock was kept'
    }

    Invoke-Case 'a stale KMC lock refuses a new transaction and releases the reservation it took' {
        param($case)
        $state = Join-Path $case 'state'
        [void][IO.Directory]::CreateDirectory($state)
        [IO.File]::WriteAllText((Join-Path $state 'active-transaction.lock'), 'stale')
        Assert-Throws { Open-KmcRuntimeLock -StateRoot $state -RunId 'blocked' } 'runtime lock already exists' 'entry over a stale lock'
        Assert-That (-not (Test-Path -LiteralPath $convention.RecordPath)) 'the refused entry kept the reservation'
    }
}
finally {
    Stop-Contenders
    if (Test-Path -LiteralPath $root) { Remove-Item -LiteralPath $root -Recurse -Force }
}

$results | ForEach-Object { Write-Host $_ }
$failed = @($results | Where-Object { $_ -like 'FAIL*' }).Count
Write-Host "RESULT total=$($results.Count) passed=$($results.Count - $failed) failed=$failed"
if ($failed -gt 0 -or $results.Count -eq 0) { exit 1 }
