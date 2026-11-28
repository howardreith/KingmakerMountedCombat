[CmdletBinding()]
param()

# Self-test of the Kingmaker native-test reservation (docs/NATIVE-RESERVATION.md) as KMC implements it in
# scripts/runtime/RuntimeHarness.Common.ps1: admission (Open-KmcRuntimeLock), release (Close-KmcRuntimeLock), unresolved
# transactions (Abandon-KmcRuntimeLock), recovery admission (Enter-KmcStaleTransactionReservation and
# Adopt-KmcStaleRuntimeLock), ownership loss and strict record parsing. Contenders are separate powershell.exe processes
# (scripts/NativeReservationContender.ps1) on distinct stand-in runtime-state roots that share only a self-test
# reservation; each case has its own private mutex and record below the system temporary directory, restored at the
# end. Never touches the machine-wide reservation, the lab's runtime state, the game or the owner's profile.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime\RuntimeHarness.Common.ps1')
$previousSelection = [Environment]::GetEnvironmentVariable('KMC_NATIVE_RESERVATION_TEST_ROOT', 'Process')
$productionConvention = Get-KmcDefaultReservationConvention

$root = Join-Path ([IO.Path]::GetTempPath()) ('kmc-reservation-selftest-' + [Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($root)
$results = New-Object System.Collections.Generic.List[string]
$contenders = New-Object System.Collections.Generic.List[System.Diagnostics.Process]
$caseNumber = 0
$convention = $null

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
    [void](Use-KmcTestReservationConvention (Join-Path $case 'reservation-root'))
    $script:convention = Get-KmcReservationConvention -StateRoot $case
    try {
        & $Body $case
        $results.Add("PASS $Name")
    }
    catch { $results.Add("FAIL ${Name}: $($_.Exception.Message)") }
    finally { Stop-Contenders }
}

function Get-Sha([string]$Path) { return (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash }
function Get-State([string]$Case, [string]$Name = 'state') { $path = Join-Path $Case $Name; [void][IO.Directory]::CreateDirectory($path); return $path }
function Get-LockPath([string]$State) { return Join-Path $State 'active-transaction.lock' }
function Read-Lease { return (Get-Content -Raw -LiteralPath $convention.RecordPath | ConvertFrom-Json) }

function Set-RecordValue($Value) {
    [void][IO.Directory]::CreateDirectory((Split-Path -Parent $convention.RecordPath))
    [IO.File]::WriteAllText($convention.RecordPath, ($Value | ConvertTo-Json -Depth 4))
}

function New-RecordValue([string]$Holder, [int]$HolderProcessId, [string]$HolderStartUtc, [string]$RunId = 'elsewhere') {
    return [ordered]@{ protocol = 'kingmaker-native-test-reservation/1'; lab = $Holder; runId = $RunId; purpose = 'stand-in'
        token = [Guid]::NewGuid().ToString('N'); holderProcessId = $HolderProcessId; holderStartUtc = $HolderStartUtc
        host = 'stand-in'; createdUtc = '2020-01-01T00:00:00.0000000Z' }
}

function Get-DeadProcessId {
    $process = Start-Process -FilePath (Join-Path $env:WINDIR 'System32\PING.EXE') -ArgumentList @('-n', '1', '127.0.0.1') -WindowStyle Hidden -PassThru
    [void]$process.WaitForExit(10000)
    return $process.Id
}

function Set-LeaseField([string]$Name, $Value) {
    $record = Read-Lease
    $record | Add-Member -NotePropertyName $Name -NotePropertyValue $Value -Force
    [IO.File]::WriteAllText($convention.RecordPath, ($record | ConvertTo-Json -Depth 4))
}

# A crashed transaction: its KMC lock (owner gone) and its lease (writer gone) stay behind.
function New-AbandonedTransaction([string]$State, [string]$RunId) {
    $lock = Open-KmcRuntimeLock -StateRoot $State -RunId $RunId
    Abandon-KmcRuntimeLock $lock
    $payload = Read-KmcJson $lock.Path
    $payload.ownerProcessId = 2147483646
    Write-KmcJsonAtomic -Path $lock.Path -Value $payload
    Set-LeaseField 'holderProcessId' (Get-DeadProcessId)
    return $lock
}

function Start-Contender([string]$State, [string]$Name, [string]$Signals, [string]$Barrier = '', [string]$Mode = 'Open', [string]$RunId = '', [string]$RunToken = '') {
    $arguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', ('"' + (Join-Path $PSScriptRoot 'NativeReservationContender.ps1') + '"'),
        '-StateRoot', ('"' + $State + '"'), '-Name', $Name, '-Signals', ('"' + $Signals + '"'), '-Mode', $Mode)
    if ($Barrier) { $arguments += @('-StartBarrier', ('"' + $Barrier + '"')) }
    if ($RunId) { $arguments += @('-RunId', $RunId) }
    if ($RunToken) { $arguments += @('-RunToken', $RunToken) }
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

function Assert-NoMutation([string]$State, [string]$Name) {
    Assert-That (-not (Test-Path -LiteralPath (Get-LockPath $State))) "$Name left a lock"
    Assert-That (@(Get-ChildItem -LiteralPath (Join-Path $State 'mutations') -Filter "$Name.*" -ErrorAction SilentlyContinue).Count -eq 0) "$Name mutated its root"
}

function Assert-LoserRefused([string]$LoserState, [string]$Name, [string]$Signals, [string]$WinnerRunId) {
    Start-Contender $LoserState $Name $Signals
    $outcome = Wait-Signal $Signals $Name @('refused', 'prepare')
    if ($outcome.What -ne 'refused') { throw "loser $Name entered its mutation section while the winner held the reservation" }
    Assert-That ($outcome.Text -like "*reserved by KingmakerMountedCombat run $WinnerRunId*") "loser $Name was refused for another reason: $($outcome.Text)"
    Assert-NoMutation $LoserState $Name
    Assert-That ([string](Read-KmcReservationRecord $convention).runId -ceq $WinnerRunId) "the winner's record changed while $Name was refused"
}

function Invoke-ArrivalOrder([string]$WinnerState, [string]$Winner, [string]$LoserState, [string]$Loser, [string]$Signals) {
    Start-Contender $WinnerState $Winner $Signals
    foreach ($phase in @('prepare', 'run', 'restore')) {
        $outcome = Wait-Signal $Signals $Winner @($phase, 'refused')
        if ($outcome.What -eq 'refused') { throw "winner $Winner was refused: $($outcome.Text)" }
        Assert-LoserRefused $LoserState "$Loser-during-$phase" $Signals "contender-$Winner"
        Send-Go $Signals $Winner $phase
    }
    [void](Wait-Signal $Signals $Winner @('released'))
    Assert-That (-not (Test-Path -LiteralPath $convention.RecordPath)) "winner $Winner left its reservation"
    $after = "$Loser-after"
    Start-Contender $LoserState $after $Signals
    foreach ($phase in @('prepare', 'run', 'restore')) {
        $outcome = Wait-Signal $Signals $after @($phase, 'refused')
        if ($outcome.What -eq 'refused') { throw "$after was refused after the winner released: $($outcome.Text)" }
        Send-Go $Signals $after $phase
    }
    [void](Wait-Signal $Signals $after @('released'))
}

function Get-Text([string]$Script) { return [IO.File]::ReadAllText((Join-Path $PSScriptRoot $Script)).Replace("`r`n", "`n") }

try {
    Invoke-Case 'the production convention is neutral, machine-wide and exact' {
        param($case)
        Assert-That ([string]$productionConvention.MutexName -ceq 'Global\KingmakerNativeTest.Reservation.v1') "mutex name $($productionConvention.MutexName)"
        Assert-That ([string]$productionConvention.RecordPath -eq (Join-Path $env:ProgramData 'KingmakerNativeTest\reservation.v1.json')) "record path $($productionConvention.RecordPath)"
        $lab = (Get-KmcHarnessLabRoot).TrimEnd('\') + '\'
        Assert-That (-not ([IO.Path]::GetFullPath($productionConvention.RecordPath)).StartsWith($lab, [StringComparison]::OrdinalIgnoreCase)) 'the record lives in the KMC lab'
    }

    Invoke-Case 'a test reservation is bound to disposable roots and its selection is restored' {
        param($case)
        Assert-That ($convention.RecordPath.StartsWith($case, [StringComparison]::OrdinalIgnoreCase) -and $convention.MutexName -cne $productionConvention.MutexName) 'the test convention is not private'
        Assert-Throws { Get-KmcReservationConvention -StateRoot (Join-Path (Get-KmcHarnessLabRoot) 'runtime-state') } 'only disposable stand-in state' 'a test reservation for the production runtime-state'
        Assert-Throws { Get-KmcReservationConvention -StateRoot 'C:\Windows\kmc-state' } 'only disposable stand-in state' 'a test reservation for a non-disposable state root'
        Assert-Throws { [void](Use-KmcTestReservationConvention 'C:\Windows\kmc-not-disposable') } 'disposable root' 'a test reservation below a non-disposable root'
        $before = [Environment]::GetEnvironmentVariable('KMC_NATIVE_RESERVATION_TEST_ROOT', 'Process')
        $previous = Use-KmcTestReservationConvention (Join-Path $case 'nested-root')
        Assert-That ($previous -ceq $before) 'the previous selection was not returned'
        Restore-KmcTestReservationConvention $previous
        Assert-That ([Environment]::GetEnvironmentVariable('KMC_NATIVE_RESERVATION_TEST_ROOT', 'Process') -ceq $before) 'the selection was not restored'
    }

    Invoke-Case 'a lab alone is admitted with a lease naming its run, state root and lock token, and released lock-then-lease' {
        param($case)
        $state = Get-State $case
        $lock = Open-KmcRuntimeLock -StateRoot $state -RunId 'alone'
        try {
            $record = Read-KmcReservationRecord $convention
            Assert-That ([string]$record.lab -ceq 'KingmakerMountedCombat' -and [string]$record.runId -ceq 'alone' -and [int]$record.holderProcessId -eq $PID) 'unexpected record'
            Assert-That ([string]$record.kmcLockToken -ceq [string]$lock.Token -and [string]$record.stateRoot -eq $state) 'the lease does not name the KMC lock'
        }
        finally { Close-KmcRuntimeLock $lock }
        Assert-That (-not (Test-Path -LiteralPath $convention.RecordPath) -and -not (Test-Path -LiteralPath (Get-LockPath $state))) 'lock or lease left behind'
    }

    Invoke-Case 'another lab''s reservation refuses entry before the KMC lock' {
        param($case)
        Set-RecordValue (New-RecordValue 'KingmakerFlight' 4 '2020-01-01T00:00:00.0000000Z' 'flight-run')
        $before = Get-Sha $convention.RecordPath
        $state = Get-State $case
        Assert-Throws { Open-KmcRuntimeLock -StateRoot $state -RunId 'blocked' } 'reserved by KingmakerFlight run flight-run' 'entry under a foreign reservation'
        Assert-That (-not (Test-Path -LiteralPath (Get-LockPath $state)) -and (Get-Sha $convention.RecordPath) -ceq $before) 'the lock was taken or the record changed'
    }

    Invoke-Case 'malformed records refuse entry and recovery before any liveness decision; lease and lock stay byte-identical' {
        param($case)
        $state = Get-State $case
        $stale = New-AbandonedTransaction $state 'crashed'
        $lockBytes = Get-Sha $stale.Path
        $good = Read-Lease
        $dead = [int]$good.holderProcessId
        $variants = @(
            @{ name = 'dead PID, malformed start time'; edit = { param($v) $v.holderStartUtc = 'yesterday' } },
            @{ name = 'dead PID, numeric start time'; edit = { param($v) $v.holderStartUtc = 12 } },
            @{ name = 'dead PID, missing start time'; edit = { param($v) $v.PSObject.Properties.Remove('holderStartUtc') } },
            @{ name = 'dead PID, start time without a zone'; edit = { param($v) $v.holderStartUtc = ([string]$v.holderStartUtc).TrimEnd('Z') } },
            @{ name = 'live PID, start time without a zone'; edit = { param($v) $v.holderProcessId = $PID; $v.holderStartUtc = (Get-Process -Id $PID).StartTime.ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffffff') } },
            @{ name = 'live PID, missing start time'; edit = { param($v) $v.holderProcessId = $PID; $v.PSObject.Properties.Remove('holderStartUtc') } },
            @{ name = 'missing purpose'; edit = { param($v) $v.PSObject.Properties.Remove('purpose') } },
            @{ name = 'missing host'; edit = { param($v) $v.PSObject.Properties.Remove('host') } },
            @{ name = 'malformed createdUtc'; edit = { param($v) $v.createdUtc = 'now' } },
            @{ name = 'missing token'; edit = { param($v) $v.PSObject.Properties.Remove('token') } },
            @{ name = 'string PID'; edit = { param($v) $v.holderProcessId = [string]$dead } },
            @{ name = 'protocol case'; edit = { param($v) $v.protocol = 'Kingmaker-native-test-reservation/1' } },
            @{ name = 'other protocol'; edit = { param($v) $v.protocol = 'kingmaker-native-test-reservation/10' } }
        )
        foreach ($variant in $variants) {
            $value = $good.PSObject.Copy(); & $variant.edit $value
            Set-RecordValue $value
            $leaseBytes = Get-Sha $convention.RecordPath
            Assert-Throws { [void](Adopt-KmcStaleRuntimeLock $state) } 'malformed' "recovery over a record with $($variant.name)"
            Assert-Throws { Open-KmcRuntimeLock -StateRoot (Get-State $case 'other') -RunId 'next' } 'malformed' "entry over a record with $($variant.name)"
            Assert-That ((Get-Sha $convention.RecordPath) -ceq $leaseBytes -and (Get-Sha $stale.Path) -ceq $lockBytes) "a refusal changed the lease or the lock ($($variant.name))"
        }
        Set-RecordValue $good
        $adopted = Adopt-KmcStaleRuntimeLock $state
        Close-KmcRuntimeLock $adopted
        Assert-That (-not (Test-Path -LiteralPath $convention.RecordPath)) 'the valid dead holder''s reservation was not recovered'
    }

    Invoke-Case 'a valid peer record (own token and timestamp formats, extra fields) is recognized, named and preserved' {
        param($case)
        $value = New-RecordValue 'KingmakerWakingRune' 4 '2020-01-01T00:00:00Z' 'wr-run'
        $value.token = [Guid]::NewGuid().ToString(); $value.createdUtc = '2020-01-01T00:00:00+00:00'
        $value.statePath = 'C:\Peer\transaction.json'; $value.extra = [ordered]@{ anything = 1 }
        Set-RecordValue $value
        $before = Get-Sha $convention.RecordPath
        Assert-Throws { Open-KmcRuntimeLock -StateRoot (Get-State $case) -RunId 'blocked' } 'reserved by KingmakerWakingRune run wr-run' 'entry under a peer record'
        $state = Get-State $case 'stale'
        [IO.File]::WriteAllText((Get-LockPath $state), (@{ schemaVersion = 1; runId = 'mine'; token = ('a' * 64); ownerProcessId = 2147483646; createdAtUtc = [DateTime]::UtcNow.ToString('o') } | ConvertTo-Json -Compress))
        Assert-Throws { [void](Adopt-KmcStaleRuntimeLock $state) } 'reserved by KingmakerWakingRune run wr-run' 'recovery under a peer record'
        Assert-That ((Get-Sha $convention.RecordPath) -ceq $before) 'the peer record changed'
    }

    Invoke-Case 'two labs'' roots exclude each other through the reservation alone, in either arrival order' {
        param($case)
        $signals = Get-State $case 'signals'
        Invoke-ArrivalOrder (Get-State $case 'a') 'A' (Get-State $case 'b') 'B' $signals
        Invoke-ArrivalOrder (Get-State $case 'b') 'B2' (Get-State $case 'a') 'A2' $signals
    }

    Invoke-Case 'simultaneous arrivals released from a common start admit exactly one; the loser names the winner and mutates nothing' {
        param($case)
        $signals = Get-State $case 'signals'
        foreach ($round in 1..3) {
            $barrier = Join-Path $signals "start-$round"
            $x = "X$round"; $y = "Y$round"
            $xState = Get-State $case "x$round"; $yState = Get-State $case "y$round"
            Start-Contender $xState $x $signals $barrier
            Start-Contender $yState $y $signals $barrier
            [void](Wait-Signal $signals $x @('ready')); [void](Wait-Signal $signals $y @('ready'))
            Assert-That (-not (Test-Path -LiteralPath $convention.RecordPath)) "round ${round}: the reservation was taken before the common start"
            [IO.File]::WriteAllText($barrier, 'go')
            $xo = Wait-Signal $signals $x @('prepare', 'refused'); $yo = Wait-Signal $signals $y @('prepare', 'refused')
            Assert-That (@(@($xo.What, $yo.What) | Where-Object { $_ -eq 'prepare' }).Count -eq 1) "round ${round}: $($xo.What) / $($yo.What)"
            $winner = if ($xo.What -eq 'prepare') { $x } else { $y }
            $loser = if ($winner -ceq $x) { $y } else { $x }
            $loserOutcome = if ($winner -ceq $x) { $yo } else { $xo }
            Assert-That ($loserOutcome.Text -like "*reserved by KingmakerMountedCombat run contender-$winner*") "round ${round}: the loser was not refused by the winner: $($loserOutcome.Text)"
            Assert-NoMutation $(if ($winner -ceq $x) { $yState } else { $xState }) $loser
            foreach ($phase in @('prepare', 'run', 'restore')) { [void](Wait-Signal $signals $winner @($phase)); Send-Go $signals $winner $phase }
            [void](Wait-Signal $signals $winner @('released'))
            $results.Add("INFO simultaneous round ${round}: ready x2, common start, winner $winner admitted, loser $loser refused by it")
        }
    }

    Invoke-Case 'an abandoned transaction keeps its lease and lock; peers stay out' {
        param($case)
        $state = Get-State $case
        $lock = Open-KmcRuntimeLock -StateRoot $state -RunId 'unresolved'
        Abandon-KmcRuntimeLock $lock
        Assert-That ([string](Read-KmcReservationRecord $convention).token -ceq [string]$lock.Reservation.Token -and (Test-Path -LiteralPath (Get-LockPath $state))) 'the abandoned transaction released something'
        Assert-Throws { Open-KmcRuntimeLock -StateRoot (Get-State $case 'other') -RunId 'next' } 'reserved by KingmakerMountedCombat run unresolved' 'a new transaction over the abandoned one'
    }

    Invoke-Case 'recovery takes over only the lease naming the stale lock, then releases it on close; an interrupted takeover retries' {
        param($case)
        $state = Get-State $case
        $stale = New-AbandonedTransaction $state 'crashed'
        # Interrupted after the lease was taken over but before the lock was opened: the lease still names the lock.
        $first = Enter-KmcStaleTransactionReservation -StateRoot $state -ExpectedRunId 'crashed' -Purpose 'recovery'
        Set-LeaseField 'holderProcessId' (Get-DeadProcessId)
        $adopted = Adopt-KmcStaleRuntimeLock $state
        try {
            Assert-That ([string]$adopted.Token -ceq [string]$stale.Token -and [string](Read-Lease).kmcLockToken -ceq [string]$stale.Token -and [int](Read-Lease).holderProcessId -eq $PID) 'the lease was not taken over for the same lock'
        }
        finally { Close-KmcRuntimeLock $adopted }
        Assert-That (-not (Test-Path -LiteralPath $convention.RecordPath) -and -not (Test-Path -LiteralPath (Get-LockPath $state))) 'the recovered transaction was not released'
    }

    Invoke-Case 'run A''s lease paired with run B''s stale lock, an expected-identity mismatch, a missing lease and a live writer all refuse byte-identically' {
        param($case)
        $stateA = Get-State $case 'a'; $stateB = Get-State $case 'b'
        [void](New-AbandonedTransaction $stateA 'run-a')
        [IO.File]::WriteAllText((Get-LockPath $stateB), (@{ schemaVersion = 1; runId = 'run-b'; token = ('b' * 64); ownerProcessId = 2147483646; createdAtUtc = [DateTime]::UtcNow.ToString('o') } | ConvertTo-Json -Compress))
        $lease = Get-Sha $convention.RecordPath; $lockA = Get-Sha (Get-LockPath $stateA); $lockB = Get-Sha (Get-LockPath $stateB)
        Assert-Throws { [void](Adopt-KmcStaleRuntimeLock $stateB) } 'not by the expected transaction of KMC run run-b' 'run B taking over run A''s lease'
        Assert-Throws { [void](Enter-KmcStaleTransactionReservation -StateRoot $stateA -ExpectedRunId 'run-x' -Purpose 'recovery') } 'belongs to run run-a, not run-x' 'a recovery expecting another run'
        Assert-Throws { [void](Enter-KmcStaleTransactionReservation -StateRoot $stateA -ExpectedRunId 'run-a' -ExpectedLockToken ('c' * 64) -Purpose 'recovery') } 'expected token' 'a recovery expecting another lock token'
        Set-LeaseField 'kmcLockToken' ('d' * 64)
        $leaseD = Get-Sha $convention.RecordPath
        Assert-Throws { [void](Adopt-KmcStaleRuntimeLock $stateA) } 'not by the expected transaction of KMC run run-a' 'a lease naming another lock of the same run'
        Assert-That ((Get-Sha $convention.RecordPath) -ceq $leaseD) 'the mismatched lease changed'
        Set-LeaseField 'kmcResumable' $false; Set-LeaseField 'holderProcessId' $PID; Set-LeaseField 'holderStartUtc' ((Get-Process -Id $PID).StartTime.ToUniversalTime().ToString('o')); Set-LeaseField 'kmcLockToken' ([string](Read-KmcJson (Get-LockPath $stateA)).token)
        Assert-Throws { [void](Adopt-KmcStaleRuntimeLock $stateA) } 'may still run' 'recovery over a live writer'
        Remove-Item -LiteralPath $convention.RecordPath -Force
        Assert-Throws { [void](Adopt-KmcStaleRuntimeLock $stateA) } 'No Kingmaker native-test reservation names KMC run run-a' 'recovery without a lease'
        Assert-That (-not (Test-Path -LiteralPath $convention.RecordPath)) 'a lease was minted for a stale lock without one'
        Assert-That ((Get-Sha (Get-LockPath $stateA)) -ceq $lockA -and (Get-Sha (Get-LockPath $stateB)) -ceq $lockB) 'a refusal changed a stale lock'
    }

    Invoke-Case 'recovery inspects the stale lock without holding it and revalidates the same identity through the exclusive handle' {
        param($case)
        $state = Get-State $case
        $stale = New-AbandonedTransaction $state 'inspected'
        Set-RecordValue (New-RecordValue 'KingmakerFlight' 4 '2020-01-01T00:00:00.0000000Z' 'flight-run')
        # Another reader holds the stale lock open (shared read): an exclusive open would fail with a sharing violation.
        $reader = [IO.File]::Open($stale.Path, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::ReadWrite)
        try { Assert-Throws { [void](Adopt-KmcStaleRuntimeLock $state) } 'reserved by KingmakerFlight' 'adoption under a peer reservation' }
        finally { $reader.Dispose() }
        # The lock changes after the reservation was taken for it: the exclusive revalidation refuses.
        Remove-Item -LiteralPath $convention.RecordPath -Force
        $lock = Open-KmcRuntimeLock -StateRoot (Get-State $case 'fresh') -RunId 'inspected-2'
        Abandon-KmcRuntimeLock $lock
        $payload = Read-KmcJson $lock.Path; $payload.ownerProcessId = 2147483646; Write-KmcJsonAtomic -Path $lock.Path -Value $payload
        Set-LeaseField 'holderProcessId' (Get-DeadProcessId)
        $reservation = Enter-KmcStaleTransactionReservation -StateRoot (Get-State $case 'fresh') -ExpectedRunId 'inspected-2' -Purpose 'recovery'
        $payload.token = ('e' * 64); Write-KmcJsonAtomic -Path $lock.Path -Value $payload
        Assert-Throws { [void](Adopt-KmcStaleRuntimeLock (Get-State $case 'fresh') -Reservation $reservation) } 'changed between inspection and takeover' 'takeover of a changed lock'
    }

    Invoke-Case 'a lost reservation stops the next shared write and the close; the replacement and the first incident stay' {
        param($case)
        $state = Get-State $case
        $lock = Open-KmcRuntimeLock -StateRoot $state -RunId 'lost'
        $working = Join-Path $case 'Working.zks'; [IO.File]::WriteAllText($working, 'working bytes')
        Set-RecordValue (New-RecordValue 'KingmakerBuffPlanner' 4 '2020-01-01T00:00:00.0000000Z' 'kbp-run')
        $replacement = Get-Sha $convention.RecordPath
        $wrote = $false
        try { [void](Assert-KmcRuntimeLockOwner $lock); [IO.File]::WriteAllText($working, 'changed'); $wrote = $true } catch { }
        Assert-That (-not $wrote -and [IO.File]::ReadAllText($working) -ceq 'working bytes') 'a write happened after the reservation was lost'
        $first = Join-Path $state 'native-reservation-incidents\lost.first.json'
        Assert-That (Test-Path -LiteralPath $first) 'no first incident'
        $firstBytes = Get-Sha $first
        Assert-Throws { Close-KmcRuntimeLock $lock } 'is lost' 'close of a lost reservation'
        Assert-That ((Get-Sha $convention.RecordPath) -ceq $replacement -and (Test-Path -LiteralPath (Get-LockPath $state)) -and (Get-Sha $first) -ceq $firstBytes) 'the replacement, the lock or the first incident changed'
    }

    Invoke-Case 'a fresh admission releases lock-then-lease on refusal and never releases a created or adopted lock''s lease' {
        param($case)
        $state = Get-State $case
        [IO.File]::WriteAllText((Get-LockPath $state), 'stale')
        Assert-Throws { Open-KmcRuntimeLock -StateRoot $state -RunId 'blocked' } 'runtime lock already exists' 'entry over a stale lock'
        Assert-That (-not (Test-Path -LiteralPath $convention.RecordPath)) 'the refused entry kept the reservation'
        $session = Enter-KmcNativeReservation -StateRoot (Get-State $case 'early') -RunId 'early' -Purpose 'runtime-scenario'
        Exit-KmcUnlockedNativeReservation $session
        Assert-That (-not (Test-Path -LiteralPath $convention.RecordPath) -and $session.Released) 'an early reservation without a lock was not released'
        $lock = Open-KmcRuntimeLock -StateRoot (Get-State $case 'held') -RunId 'held'
        Exit-KmcUnlockedNativeReservation $lock.Reservation
        Assert-That (Test-Path -LiteralPath $convention.RecordPath) 'the lease of a created lock was released by the pre-lock boundary'
        Close-KmcRuntimeLock $lock
    }

    Invoke-Case 'a recovery that exits before taking its lock leaves the transaction resumable; peers stay out and a retry continues it' {
        param($case)
        $state = Get-State $case
        $stale = New-AbandonedTransaction $state 'declined'
        # A recovery takes the lease over in this (live) process, then declines before holding the lock.
        $session = Enter-KmcStaleTransactionReservation -StateRoot $state -ExpectedRunId 'declined' -ExpectedLockToken ([string]$stale.Token) -Purpose 'recovery'
        Exit-KmcUnlockedNativeReservation $session
        Assert-That ((Read-Lease).kmcResumable -eq $true -and [int](Read-Lease).holderProcessId -eq $PID) 'the declined recovery did not leave a resumable lease'
        Assert-Throws { Open-KmcRuntimeLock -StateRoot (Get-State $case 'peer') -RunId 'peer' } 'reserved by KingmakerMountedCombat run declined' 'a peer over the resumable lease'
        # The retry continues although the declining host is still alive.
        $adopted = Adopt-KmcStaleRuntimeLock $state -ExpectedRunId 'declined'
        Close-KmcRuntimeLock $adopted
        Assert-That (-not (Test-Path -LiteralPath $convention.RecordPath)) 'the retried recovery did not release'
    }

    Invoke-Case 'a one-run recovery bound to its own run refuses a different current abandoned run byte-identically' {
        param($case)
        $state = Get-State $case
        [void](New-AbandonedTransaction $state 'other-run')
        $lease = Get-Sha $convention.RecordPath; $lock = Get-Sha (Get-LockPath $state)
        Assert-Throws { [void](Adopt-KmcStaleRuntimeLock $state -ExpectedRunId 'my-run') } 'belongs to run other-run, not my-run' 'adoption of another run'
        Assert-Throws { [void](Enter-KmcStaleTransactionReservation -StateRoot $state -ExpectedRunId 'other-run' -ExpectedLockToken ('f' * 64) -Purpose 'recovery') } 'expected token' 'a journal token that differs from the lock'
        Assert-That ((Get-Sha $convention.RecordPath) -ceq $lease -and (Get-Sha (Get-LockPath $state)) -ceq $lock) 'a refusal changed the lease or the lock'
    }

    Invoke-Case 'the profile follow-up keeps peers out from the transaction recovery''s close until its own verified close' {
        param($case)
        $signals = Get-State $case 'signals'
        $state = Get-State $case 'kmc'; $peer = Get-State $case 'peer'
        $stale = New-AbandonedTransaction $state 'handoff-run'
        $runToken = [string]$stale.Token
        Start-Contender $state 'R' $signals '' 'Recovery' 'handoff-run'
        [void](Wait-Signal $signals 'R' @('handed-off', 'refused'))
        [void]($contenders[0].WaitForExit(30000))
        $lease = Read-Lease
        Assert-That ($lease.kmcPendingFollowUp -ceq 'profile-recovery' -and [string]$lease.kmcRunToken -ceq $runToken -and -not (Test-Path -LiteralPath (Get-LockPath $state))) 'the recovery did not keep the reservation for the follow-up'
        Assert-LoserRefused $peer 'peer-between' $signals 'handoff-run'
        Assert-Throws { [void](Enter-KmcFollowUpReservation -StateRoot $state -RunId 'handoff-run' -RunToken ('c' * 64) -FollowUp 'profile-recovery') } 'not by the expected transaction' 'a follow-up with another run token'
        Assert-Throws { Complete-KmcInterruptedReservation -StateRoot $state -RunId 'handoff-run' -LeaseToken ([string](Read-Lease).token) } 'completable lease' 'completion of an owed follow-up'
        Start-Contender $state 'P' $signals '' 'FollowUp' 'handoff-run' $runToken
        $outcome = Wait-Signal $signals 'P' @('restore', 'refused')
        Assert-That ($outcome.What -eq 'restore') "the follow-up was refused: $($outcome.Text)"
        Assert-LoserRefused $peer 'peer-during' $signals 'handoff-run'
        Send-Go $signals 'P' 'restore'
        [void](Wait-Signal $signals 'P' @('released'))
        Assert-That (-not (Test-Path -LiteralPath $convention.RecordPath)) 'the follow-up did not release'
        Start-Contender $peer 'peer-after' $signals
        foreach ($phase in @('prepare', 'run', 'restore')) { $o = Wait-Signal $signals 'peer-after' @($phase, 'refused'); Assert-That ($o.What -eq $phase) "the peer was refused after the follow-up: $($o.Text)"; Send-Go $signals 'peer-after' $phase }
        [void](Wait-Signal $signals 'peer-after' @('released'))
    }

    Invoke-Case 'interrupted completion: abbreviated records, unbound records, pending children and unlisted pending artifacts refuse byte-identically' {
        param($case)
        $state = Get-State $case
        # Window 1, an acquisition stopped before its lock: no ledger and no artifact of the run.
        $session = Enter-KmcNativeReservation -StateRoot $state -RunId 'before-lock' -Purpose 'runtime-scenario'
        Set-LeaseField 'holderProcessId' (Get-DeadProcessId)
        Assert-Throws { Complete-KmcInterruptedReservation -StateRoot $state -RunId 'before-lock' -LeaseToken ('0' * 32) } 'completable lease' 'completion with another token'
        $combined = Get-KmcRunTransactionStatePath $state 'before-lock'
        [void][IO.Directory]::CreateDirectory((Split-Path -Parent $combined))
        [IO.File]::WriteAllText($combined, ('{"runId":"before-lock","token":"' + $session.RunToken + '","phase":"restored"}'))
        $before = Get-Sha $convention.RecordPath
        Assert-Throws { Complete-KmcInterruptedReservation -StateRoot $state -RunId 'before-lock' -LeaseToken $session.Token } 'no obligation ledger' 'completion with artifacts but no ledger'
        Assert-That ((Get-Sha $convention.RecordPath) -ceq $before) 'a refused completion changed the lease'
        Remove-Item -LiteralPath $combined -Force
        Complete-KmcInterruptedReservation -StateRoot $state -RunId 'before-lock' -LeaseToken $session.Token
        Assert-That (-not (Test-Path -LiteralPath $convention.RecordPath)) 'the genuine pre-lock interruption was not completed'

        # Window 2 with evidence that does not prove restoration (the verified post-unlink success, built from records a
        # real disposable restore produced, is in Test-Harness.ps1).
        $after = Get-State $case 'after'; $afterBackups = Join-Path $case 'runtime-backups'
        $lock = Open-KmcRuntimeLock -StateRoot $after -RunId 'after-unlink'
        $token = [string]$lock.Token
        foreach ($name in @('profile', 'areas', 'working', 'mods')) { Register-KmcRunObligation -Lock $lock -Name $name }
        Complete-KmcRunObligation -Lock $lock -RunToken $token -Name 'profile' -Evidence 'stand-in verified profile restoration'
        $lock.Stream.Dispose(); Remove-Item -LiteralPath $lock.Path -Force
        Set-LeaseField 'holderProcessId' (Get-DeadProcessId)
        $lease = [string](Read-Lease).token
        $modsPath = Get-KmcTransactionStatePath $after 'after-unlink'; $savePath = Get-KmcSaveTransactionStatePath $after 'after-unlink'
        $areasPath = Join-Path $afterBackups 'profile-after-unlink\Areas-isolation.json'
        foreach ($p in @($modsPath, $savePath, $areasPath)) { [void][IO.Directory]::CreateDirectory((Split-Path -Parent $p)) }
        # Abbreviated positive records (the shape earlier rounds accepted) prove nothing.
        $mods = '{"runId":"after-unlink","token":"' + $token + '","phase":"restored","restoredAtUtc":"2026-01-01T00:00:00.0000000Z","restoredDigest":"' + ('a' * 64) + '"}'
        $save = '{"runId":"after-unlink","token":"' + $token + '","phase":"restored","restoredAtUtc":"2026-01-01T00:00:00.0000000Z","restoredInventoryDigest":"' + ('b' * 64) + '","beforeInventory":{"digest":"' + ('b' * 64) + '"}}'
        $areas = '{"schemaVersion":1,"runId":"after-unlink","token":"' + $token + '","stage":"restored"}'
        $refusals = @(
            @('abbreviated restored records', $null, $null, 'lacks its schema fields'),
            @('a wrong-type terminal phase', $modsPath, ($mods -replace '"phase":"restored"', '"phase":1'), 'mods:'),
            @('a started child whose terminal record is missing', $savePath, '', 'working: its terminal record is missing'),
            @('a terminal record bound to another token', $savePath, ($save -replace $token, ('f' * 64)), 'working: its terminal record is unbound'))
        Assert-That ($refusals.Count -eq 4) 'the refusal cases are not four separate cases'
        foreach ($refusal in $refusals) {
            [IO.File]::WriteAllText($modsPath, $mods); [IO.File]::WriteAllText($savePath, $save); [IO.File]::WriteAllText($areasPath, $areas)
            if ($null -ne $refusal[1]) { if ($refusal[2] -eq '') { Remove-Item -LiteralPath $refusal[1] -Force } else { [IO.File]::WriteAllText($refusal[1], $refusal[2]) } }
            $before = Get-Sha $convention.RecordPath
            Assert-Throws { Complete-KmcInterruptedReservation -StateRoot $after -RunId 'after-unlink' -LeaseToken $lease } $refusal[3] ('completion over ' + $refusal[0])
            Assert-That ((Get-Sha $convention.RecordPath) -ceq $before) ('completion over ' + $refusal[0] + ' changed the lease')
        }
        # A pending artifact of the run that the ledger does not list cannot hide behind a valid ledger.
        $ledger = Get-KmcRunObligationLedgerPath $after 'after-unlink'
        $value = Read-KmcJson $ledger; $value.obligations = @($value.obligations | Where-Object { $_.name -ceq 'profile' }); Write-KmcJsonAtomic $ledger $value
        foreach ($p in @($modsPath, $savePath, $areasPath)) { Remove-Item -LiteralPath $p -Force }
        $pending = Join-Path $after 'fixture-recoveries\after-unlink.json'; [void][IO.Directory]::CreateDirectory((Split-Path -Parent $pending))
        [IO.File]::WriteAllText($pending, ('{"schemaVersion":1,"runId":"after-unlink","token":"' + $token + '","phase":"prepared"}'))
        $before = Get-Sha $convention.RecordPath
        Assert-Throws { Complete-KmcInterruptedReservation -StateRoot $after -RunId 'after-unlink' -LeaseToken $lease } 'the ledger records no such child' 'completion over a pending journal omitted from the ledger'
        Assert-That ((Get-Sha $convention.RecordPath) -ceq $before) 'the omitted pending journal case changed the lease'
        Remove-Item -LiteralPath $pending -Force
        Write-KmcJsonAtomic (Join-Path $afterBackups 'profile-after-unlink\snapshot.json') ([ordered]@{ runId = 'after-unlink'; token = $token })
        Complete-KmcInterruptedReservation -StateRoot $after -RunId 'after-unlink' -LeaseToken $lease
        Assert-That (-not (Test-Path -LiteralPath $convention.RecordPath)) 'a run whose only obligation is closed was not completed'
    }

    Invoke-Case 'a private test reservation never covers a real shared surface' {
        param($case)
        Assert-Throws { Assert-KmcReservationCoversSurface -Path @('C:\Windows\Saved Games') } 'cannot cover the shared surface' 'a private reservation over a real save root'
        $state = Get-State $case
        $lock = Open-KmcRuntimeLock -StateRoot $state -RunId 'surface'
        try {
            Assert-Throws { [void](Enter-KmcWorkingSaveTransaction -Lock $lock -Pair ([pscustomobject]@{}) -SaveRoot 'C:\Windows\kmc-saves' -StateRoot $state -BackupRoot (Get-State $case 'b') -StagingRoot (Get-State $case 's') -Scenario 'fixture-intake') } 'cannot cover the shared surface' 'a Working-save transaction on a real save root'
            Assert-Throws { [void](Enter-KmcModsTransaction -Lock $lock -LiveModsRoot 'C:\Windows\kmc-mods' -PackagePath 'x' -StateRoot $state -BackupRoot (Get-State $case 'b') -StagingRoot (Get-State $case 's')) } 'cannot cover the shared surface' 'a Mods transaction on a real Mods root'
        }
        finally { Close-KmcRuntimeLock $lock }
    }

    Invoke-Case 'a -WhatIf caller removes exactly its own lease (never simulated) and is released only after the removal; a failed release surfaces and keeps the lease' {
        param($case)
        $state = Get-State $case
        $session = Enter-KmcNativeReservation -StateRoot $state -RunId 'preview' -Purpose 'runtime-preview'
        & { $WhatIfPreference = $true; Exit-KmcUnlockedNativeReservation $session }
        Assert-That (-not (Test-Path -LiteralPath $convention.RecordPath)) 'a -WhatIf release left the real lease behind'
        Assert-That $session.Released 'the session is not marked released after its removal'
        $replaced = Enter-KmcNativeReservation -StateRoot $state -RunId 'audit' -Purpose 'preparation-fixture-guard'
        Set-LeaseField 'token' ([Guid]::NewGuid().ToString('N'))
        $before = Get-Sha $convention.RecordPath
        Assert-Throws { Exit-KmcUnlockedNativeReservation $replaced } 'no longer exactly this transaction' 'a release over a replaced lease (success path)'
        Assert-That (-not $replaced.Released) 'a failed release marked the session released'
        Assert-That ((Get-Sha $convention.RecordPath) -ceq $before) 'a failed release changed the current record'
        # On an error path the operation's own error wins: the cleanup failure is reported, not thrown.
        Exit-KmcUnlockedNativeReservation $replaced -PreserveError -WarningAction SilentlyContinue
        Assert-That ((Get-Sha $convention.RecordPath) -ceq $before) 'an error-path cleanup changed the current record'
        Remove-Item -LiteralPath $convention.RecordPath -Force
        $malformed = Enter-KmcNativeReservation -StateRoot (Get-State $case 'malformed') -RunId 'malformed' -Purpose 'runtime-preview'
        [IO.File]::WriteAllText($convention.RecordPath, '{"protocol":"kingmaker-native-test-reservation/9"}')
        Assert-Throws { Exit-KmcUnlockedNativeReservation $malformed } 'no longer exactly this transaction' 'a release over an unknown-protocol record'
        Assert-That ([IO.File]::ReadAllText($convention.RecordPath) -ceq '{"protocol":"kingmaker-native-test-reservation/9"}') 'an unknown-protocol record was removed'
        # Same token, same identity, but one diagnostic field changed: not exactly its own record, so never removed.
        Remove-Item -LiteralPath $convention.RecordPath -Force
        $diagnostic = Enter-KmcNativeReservation -StateRoot (Get-State $case 'diagnostic') -RunId 'diagnostic' -Purpose 'runtime-preview'
        Set-LeaseField 'host' 7
        $tampered = Get-Sha $convention.RecordPath
        Assert-Throws { Exit-KmcUnlockedNativeReservation $diagnostic } 'no longer exactly this transaction' 'a release over a record with a malformed diagnostic field'
        Assert-That ((Get-Sha $convention.RecordPath) -ceq $tampered) 'a record with a malformed diagnostic field was removed'
        Remove-Item -LiteralPath $convention.RecordPath -Force
    }

    Invoke-Case 'a caller-supplied reservation stays with the caller when its lock cannot be created' {
        param($case)
        $state = Get-State $case
        $session = Enter-KmcNativeReservation -StateRoot $state -RunId 'supplied' -Purpose 'runtime-scenario'
        [IO.File]::WriteAllText((Get-LockPath $state), 'foreign')
        Assert-Throws { [void](Open-KmcRuntimeLock -StateRoot $state -RunId 'supplied' -Reservation $session) } 'already exists' 'a lock over an existing lock file'
        Assert-That ([string](Read-Lease).token -ceq [string]$session.Token -and -not $session.Released) 'Open released the caller''s reservation before the caller''s final shared reads'
        Remove-Item -LiteralPath (Get-LockPath $state) -Force
        Exit-KmcUnlockedNativeReservation $session
        Assert-That (-not (Test-Path -LiteralPath $convention.RecordPath)) 'the caller could not release its own reservation afterwards'
    }

    Invoke-Case 'KMC''s own holder start is compared at full round-trip precision; a shortened instant is a live writer' {
        param($case)
        $state = Get-State $case
        $own = (Get-Process -Id $PID).StartTime.ToUniversalTime()
        $session = Enter-KmcNativeReservation -StateRoot $state -RunId 'precision' -Purpose 'runtime-scenario'
        Assert-That ([string](Read-Lease).holderStartUtc -cmatch '^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{7}Z$') 'KMC writes a shortened holder start'
        foreach ($shortened in @($own.ToString('yyyy-MM-ddTHH:mm:ssZ'), $own.ToString('yyyy-MM-ddTHH:mm:ss.fffZ'), ($own.ToString('yyyy-MM-ddTHH:mm:ss.fffffff') + '+00:00'))) {
            Set-LeaseField 'holderStartUtc' $shortened
            Assert-That (Test-KmcReservationHolderAlive (Read-KmcReservationRecord $convention)) "the live writer with start $shortened counted as gone"
            Assert-Throws { Complete-KmcInterruptedReservation -StateRoot $state -RunId 'precision' -LeaseToken $session.Token } 'may still run' "completion over a live writer recorded as $shortened"
        }
        Set-LeaseField 'holderStartUtc' $own.ToString('o')
        Assert-That (Test-KmcReservationHolderAlive (Read-KmcReservationRecord $convention)) 'the exact live writer counted as gone'
        Set-LeaseField 'holderProcessId' (Get-DeadProcessId)
        Assert-That (-not (Test-KmcReservationHolderAlive (Read-KmcReservationRecord $convention))) 'a gone writer with an exact start counted as live'
        Complete-KmcInterruptedReservation -StateRoot $state -RunId 'precision' -LeaseToken $session.Token
    }

    Invoke-Case 'recovery binds the journal run and token: a same-run wrong-token takeover refuses byte-identically' {
        param($case)
        $state = Get-State $case
        $lock = New-AbandonedTransaction $state 'pinned'
        $before = Get-Sha $convention.RecordPath; $lockBefore = Get-Sha $lock.Path
        Assert-Throws { [void](Enter-KmcStaleTransactionReservation -StateRoot $state -ExpectedRunId 'pinned' -ExpectedLockToken ('e' * 64) -Purpose 'recovery-one-run') } 'differs from the recovery''s expected token' 'a same-run takeover with a wrong pinned token'
        Assert-That ((Get-Sha $convention.RecordPath) -ceq $before -and (Get-Sha $lock.Path) -ceq $lockBefore) 'a refused takeover changed the lease or lock'
        $session = Enter-KmcStaleTransactionReservation -StateRoot $state -ExpectedRunId 'pinned' -ExpectedLockToken ([string]$lock.Token) -Purpose 'recovery-one-run'
        $adopted = Adopt-KmcStaleRuntimeLock -StateRoot $state -ExpectedRunId 'pinned' -ExpectedLockToken ([string]$lock.Token) -Reservation $session
        Close-KmcRuntimeLock $adopted
        Assert-That (-not (Test-Path -LiteralPath $convention.RecordPath)) 'the bound recovery did not release'
    }

    Invoke-Case 'an ownership loss records what the transaction already restored and what remains' {
        param($case)
        $state = Get-State $case
        $lock = Open-KmcRuntimeLock -StateRoot $state -RunId 'progress'
        Add-KmcReservationProgress $lock 'live Mods returned to the pre-run tree'
        Set-LeaseField 'token' ([Guid]::NewGuid().ToString('N'))
        Assert-Throws { [void](Assert-KmcRuntimeLockOwner $lock) } 'lost' 'the ownership pair after a replacement'
        $incident = Read-KmcJson (Join-Path $state 'native-reservation-incidents\progress.first.json')
        Assert-That (@($incident.restorationPerformed) -join '|' -like '*live Mods returned to the pre-run tree*') 'the incident does not record the restoration performed'
        Assert-That ([string]$incident.remainingDifferences -like '*progress*') 'the incident does not record the remaining differences'
        $lock.Stream.Dispose()
    }

    Invoke-Case 'the closed preflight''s embedded launcher guard borrows its exact lease; a foreign contender stays out until the owner releases' {
        param($case)
        $state = Get-State $case
        $signals = Get-State $case 'signals'
        $outer = Enter-KmcNativeReservation -StateRoot $state -RunId 'closed-preflight' -Purpose 'preparation-scoped-preflight'
        try {
            [Environment]::SetEnvironmentVariable('KMC_NATIVE_RESERVATION_BORROW_TOKEN', ('0' * 32), 'Process')
            Assert-Throws { [void](Enter-KmcEntrypointReservation -StateRoot $state -RunId 'launcher' -Purpose 'runtime-preview') } 'not the running scoped preflight' 'borrowing with another token'
            [Environment]::SetEnvironmentVariable('KMC_NATIVE_RESERVATION_BORROW_TOKEN', [string]$outer.Token, 'Process')
            $inner = Enter-KmcEntrypointReservation -StateRoot $state -RunId 'launcher' -Purpose 'runtime-preview'
            Assert-That ($inner.Borrowed -and [string]$inner.Token -ceq [string]$outer.Token) 'the guard did not borrow the closed preflight''s lease'
            Assert-KmcNativeReservationHeld -Session $inner -Context 'borrowed guard'
            Assert-LoserRefused (Get-State $case 'foreign') 'foreign-during-guard' $signals 'closed-preflight'
            Exit-KmcUnlockedNativeReservation $inner
            Assert-Throws { Exit-KmcNativeReservation $inner } 'borrowed' 'a borrower releasing'
            Assert-Throws { [void](Open-KmcRuntimeLock -StateRoot $state -RunId 'closed-preflight' -Reservation $inner) } 'borrowed' 'a borrower taking a lock'
            Assert-That ([string](Read-Lease).token -ceq [string]$outer.Token) 'the borrower changed the owner''s lease'
        }
        finally { [Environment]::SetEnvironmentVariable('KMC_NATIVE_RESERVATION_BORROW_TOKEN', $null, 'Process') }
        $standalone = $null
        Assert-Throws { $standalone = Enter-KmcEntrypointReservation -StateRoot (Get-State $case 'standalone') -RunId 'standalone' -Purpose 'runtime-preview' } 'reserved by KingmakerMountedCombat run closed-preflight' 'standalone launcher admission while the closed preflight holds'
        Exit-KmcNativeReservation $outer
        Start-Contender (Get-State $case 'foreign') 'foreign-after' $signals
        foreach ($phase in @('prepare', 'run', 'restore')) {
            $outcome = Wait-Signal $signals 'foreign-after' @($phase, 'refused')
            if ($outcome.What -eq 'refused') { throw "the foreign contender was refused after the owner released: $($outcome.Text)" }
            Send-Go $signals 'foreign-after' $phase
        }
        [void](Wait-Signal $signals 'foreign-after' @('released'))
    }

    Invoke-Case 'the closed preflight removes only exactly its own record; each replacement is left alone with its first incident; occupied admission names the holder' {
        param($case)
        # The inline closed-entry participation, extracted from the entry and bound to this case's private mutex and record.
        $tokens = $null; $errors = $null
        $ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'runtime\Invoke-Chunk7ScopedPreflight.ps1'), [ref]$tokens, [ref]$errors)
        $names = @('Invoke-KmcScopedReservationMutex', 'Enter-KmcScopedReservation', 'Exit-KmcScopedReservation')
        $text = [string]::Join("`n", @($ast.FindAll({ param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $names -ccontains $n.Name }, $false) | ForEach-Object { $_.Extent.Text }))
        Assert-That ($text.Contains("'Global\KingmakerNativeTest.Reservation.v1'")) 'the closed entry does not use the machine-wide mutex'
        $text = $text.Replace("'Global\KingmakerNativeTest.Reservation.v1'", "'" + $convention.MutexName + "'").Replace("'KMC_NATIVE_RESERVATION_TEST_ROOT'", "'KMC_SCOPED_SELFTEST_UNSET'")
        $state = Get-State $case
        $script:scopedReservationRecord = $convention.RecordPath; $script:scopedReservationToken = $null; $script:scopedReservationRunId = $null; $script:scopedReservationBytes = $null; $script:scopedReservationStateRoot = $null
        . ([scriptblock]::Create($text))
        # Occupied: refused, naming the readable holder; the holder's record is untouched.
        Set-RecordValue (New-RecordValue 'KingmakerFlight' 4 '2020-01-01T00:00:00Z' 'peer-run')
        $peer = Get-Sha $convention.RecordPath
        Assert-Throws { Enter-KmcScopedReservation $state } 'reserved by KingmakerFlight run peer-run' 'the closed entry under a readable peer record'
        Assert-That ((Get-Sha $convention.RecordPath) -ceq $peer) 'the closed entry changed the peer record'
        Remove-Item -LiteralPath $convention.RecordPath -Force
        Enter-KmcScopedReservation $state
        $own = [IO.File]::ReadAllText($convention.RecordPath)
        $ownRecord = $own | ConvertFrom-Json
        $variants = @(
            ($own -replace 'kingmaker-native-test-reservation/1', 'kingmaker-native-test-reservation/2'),
            ($own -replace [regex]::Escape([string]$ownRecord.createdUtc), 'not-a-timestamp'),
            ((New-RecordValue 'KingmakerFlight' 4 '2020-01-01T00:00:00Z' 'foreign-run') | ConvertTo-Json))
        Assert-That ($variants.Count -eq 3) 'the replacement cases are not three separate cases'
        $incidents = Join-Path $state 'native-reservation-incidents'
        foreach ($variant in $variants) {
            Assert-That ($variant -cne $own) 'a replacement case equals the own record'
            [IO.File]::WriteAllText($convention.RecordPath, $variant)
            Assert-Throws { Exit-KmcScopedReservation } 'no longer exactly its own' "the closed release over: $variant"
            Assert-That ([IO.File]::ReadAllText($convention.RecordPath) -ceq $variant) "the closed release removed: $variant"
        }
        Assert-That (Test-Path -LiteralPath (Join-Path $incidents ([string]$ownRecord.runId + '.first.json'))) 'the closed release recorded no first incident'
        Assert-That (@(Get-ChildItem -LiteralPath $incidents -Filter ([string]$ownRecord.runId + '*.json')).Count -eq 3) 'each refused release was not recorded once'
        Assert-That ([string](Read-KmcJson (Join-Path $incidents ([string]$ownRecord.runId + '.first.json'))).observedRecordText -like '*reservation/2*') 'the first incident is not the first observation'
        [IO.File]::WriteAllText($convention.RecordPath, $own)
        Exit-KmcScopedReservation
        Assert-That (-not (Test-Path -LiteralPath $convention.RecordPath)) 'the closed release left its own record'
    }

    Invoke-Case 'a failed private setup never falls back to the production reservation; private fixture and profile paths refuse real surfaces' {
        param($case)
        $selected = [Environment]::GetEnvironmentVariable('KMC_NATIVE_RESERVATION_TEST_ROOT', 'Process')
        try {
            [Environment]::SetEnvironmentVariable('KMC_NATIVE_RESERVATION_TEST_ROOT', 'C:\Windows\kmc-not-disposable', 'Process')
            Assert-Throws { [void](Enter-KmcNativeReservation -StateRoot (Get-State $case) -RunId 'setup' -Purpose 'runtime-scenario') } 'not a disposable root' 'a reservation under a failed private setup'
            Assert-Throws { Invoke-KmcReservationTestHook 'none'; [void](Get-KmcReservationConvention -StateRoot (Get-State $case)) } 'not a disposable root' 'the convention under a failed private setup'
        }
        finally { [Environment]::SetEnvironmentVariable('KMC_NATIVE_RESERVATION_TEST_ROOT', $selected, 'Process') }
        Assert-That ((Get-KmcPlayerPrefsKeyPath) -like 'Software\KmcNativeReservationSelfTest\*') 'private mode reads the owner''s PlayerPrefs key'
        Assert-Throws { [void](Invoke-KmcQualifiedWorkingFixtureRecovery -RunId 'fixture' -SaveRoot 'C:\Windows\kmc-saves' -StateRoot (Get-State $case) -BackupRoot (Get-State $case 'b') -QualificationPath (Join-Path $case 'q.json') -HistoricalAuthority ([pscustomobject]@{}) -Confirm:$false) } 'cannot cover the shared surface' 'a private fixture repair over a real save root'
        Assert-That (-not (Test-Path -LiteralPath $convention.RecordPath)) 'a refused private fixture repair reserved'
    }

    Invoke-Case 'a started child without its terminal record stays open; a never-started child needs none; a changed installed loader keeps its obligation open' {
        param($case)
        $state = Get-State $case
        # A started child (here Mods, in a run of its own) without its terminal record stays open; so does an abbreviated
        # record the writer's contract refuses.
        $modsState = Get-State $case 'mods-state'
        $modsLock = Open-KmcRuntimeLock -StateRoot $modsState -RunId 'obligations-mods' -Reservation (Enter-KmcNativeReservation -StateRoot $modsState -RunId 'obligations-mods' -Purpose 'runtime-scenario')
        Register-KmcRunObligation -Lock $modsLock -Name 'mods'
        $o = Get-KmcRunObligationState -StateRoot $modsState -RunId 'obligations-mods'
        Assert-That (-not $o.Closed -and (@($o.Open) -join '|') -like '*mods: its terminal record is missing*') 'a started Mods child with no receipt was not open'
        $modsPath = Get-KmcTransactionStatePath $modsState 'obligations-mods'; [void][IO.Directory]::CreateDirectory((Split-Path -Parent $modsPath))
        Write-KmcJsonAtomic $modsPath ([ordered]@{ schemaVersion = 4; runId = 'obligations-mods'; token = [string]$modsLock.Token; phase = 'restored'; liveModsRoot = 'C:\Mods'; beforeDigest = ('a' * 64); restoredAtUtc = '2026-01-01T00:00:00.0000000Z'; restoredDigest = ('a' * 64) })
        Assert-That ((@((Get-KmcRunObligationState -StateRoot $modsState -RunId 'obligations-mods').Open) -join '|') -like '*mods: its record lacks its schema fields*') 'an abbreviated schema-4 Mods record was accepted'
        Remove-Item -LiteralPath $modsPath -Force
        $modsLock.Stream.Dispose(); Remove-Item -LiteralPath $convention.RecordPath -Force
        $state2 = $modsState
        $lock = Open-KmcRuntimeLock -StateRoot $state -RunId 'obligations'
        try {
            $token = [string]$lock.Token
            Assert-That ((Get-KmcRunObligationState -StateRoot $state -RunId 'obligations').Closed) 'a run with no started child is not closed'
            # A stand-in installed loader: the receipt Get-KmcBaseGameDependencies produces for it, recorded in the Mods journal.
            $game = Join-Path $case 'game'; $loader = Join-Path $game 'Kingmaker_Data\Managed\UnityModManager'
            [void][IO.Directory]::CreateDirectory($loader); [void][IO.Directory]::CreateDirectory((Join-Path $game 'Mods'))
            [IO.File]::WriteAllText((Join-Path $loader 'Config.xml'), '<Config><ModsDirectory>Mods</ModsDirectory><ModInfo>Info.json</ModInfo><GameExe>Kingmaker.exe</GameExe></Config>')
            [IO.File]::WriteAllText((Join-Path $loader 'Params.xml'), '<Param><ShowOnStart>1</ShowOnStart><ModParams><Mod Id="KingmakerMountedCombat" Enabled="true"><Hotkey><keyCode>None</keyCode><modifiers>0</modifiers></Hotkey></Mod></ModParams></Param>')
            foreach ($name in @('UnityModManager.dll', '0Harmony12.dll', '0Harmony-1.2.dll', '0Harmony.dll', 'dnlib.dll')) { [IO.File]::WriteAllText((Join-Path $loader $name), ('stand-in-' + $name)) }
            $assembly = Join-Path $game 'Kingmaker_Data\Managed\Assembly-CSharp.dll'; [IO.File]::WriteAllText($assembly, 'stand-in-gameplay')
            $pins = @(foreach ($pair in @(@('gameplayAssembly', $assembly), @('umm', (Join-Path $loader 'UnityModManager.dll')), @('harmony', (Join-Path $loader '0Harmony12.dll')))) { [pscustomobject]@{ role = $pair[0]; length = (Get-Item -LiteralPath $pair[1]).Length; sha256 = (Get-KmcSha256 $pair[1]) } })
            $receipt = Get-KmcBaseGameDependencies -GameRoot $game -Fingerprint ([pscustomobject]@{ kingmaker = [pscustomobject]@{ files = $pins } }) -RuntimeLoaderProfile ([pscustomobject]@{ Id = 'stand-in'; UmmSha256 = (Get-KmcSha256 (Join-Path $loader 'UnityModManager.dll')); Harmony2Sha256 = (Get-KmcSha256 (Join-Path $loader '0Harmony.dll')) })
            Register-KmcRunObligation -Lock $lock -Name 'installed-dependencies' -Evidence $receipt
            [void][IO.Directory]::CreateDirectory((Split-Path -Parent $modsPath))
            Assert-That ((Get-KmcRunObligationState -StateRoot $state -RunId 'obligations').Closed) 'exact loader evidence did not close the run'
            [IO.File]::WriteAllText((Join-Path $loader 'dnlib.dll'), 'changed loader bytes')
            $o = Get-KmcRunObligationState -StateRoot $state -RunId 'obligations'
            Assert-That (-not $o.Closed -and (@($o.Open) -join '|') -like '*installed-dependencies: the installed loader dependencies differ*') 'a changed installed loader did not keep its obligation open'
            [IO.File]::WriteAllText((Join-Path $loader 'dnlib.dll'), 'stand-in-dnlib.dll')
            # The selection is verified independently of the Mods child: with the Mods record gone the loader is still
            # checked, and private-only judgement never opens a loader file.
            Remove-Item -LiteralPath (Join-Path $loader 'dnlib.dll') -Force
            $o = Get-KmcRunObligationState -StateRoot $state -RunId 'obligations'
            Assert-That ((@($o.Open) -join '|') -like '*installed-dependencies: the installed loader dependencies differ*') 'a missing installed loader file did not keep the selection open without the Mods record'
            $p = Get-KmcRunObligationState -StateRoot $state -RunId 'obligations' -PrivateOnly
            Assert-That ((@($p.Open) -join '|') -notlike '*installed-dependencies*') 'private-only judgement read the installed loader files'
            [IO.File]::WriteAllText((Join-Path $loader 'dnlib.dll'), 'stand-in-dnlib.dll')
            Assert-That ((Get-KmcRunObligationState -StateRoot $state -RunId 'obligations').Closed) 'restored evidence did not close the run again'
            # A started child whose receipt is removed after its bytes were restored stays open.
            Assert-That (-not (Get-KmcRunObligationState -StateRoot $state2 -RunId 'obligations-mods').Closed) 'removing a started child''s receipt erased its obligation'
            # An invalid ledger leaves everything open.
            $ledger = Get-KmcRunObligationLedgerPath $state 'obligations'; $ledgerBytes = [IO.File]::ReadAllBytes($ledger)
            [IO.File]::WriteAllText($ledger, '{"schemaVersion":1}')
            Assert-That ((@((Get-KmcRunObligationState -StateRoot $state -RunId 'obligations').Open) -join '|') -like 'ledger:*') 'an invalid ledger did not leave the run open'
            [IO.File]::WriteAllBytes($ledger, $ledgerBytes)
        }
        finally { $lock.Stream.Dispose() }
    }

    Invoke-Case 'admit-then-bind: a same-run wrong-token journal, a wrong-run journal and a changed pin refuse before any takeover; lease and lock stay byte-identical' {
        param($case)
        $state = Get-State $case
        $lock = New-AbandonedTransaction $state 'bound'
        $journal = Join-Path $state 'journal.json'; $pinned = Join-Path $state 'pinned.json'
        [IO.File]::WriteAllText($pinned, 'reviewed'); $pinSha = Get-KmcSha256 $pinned
        $leaseBefore = Get-Sha $convention.RecordPath; $lockBefore = Get-Sha $lock.Path
        $cases = @(
            @('a same-run wrong-token journal', ('{"runId":"bound","token":"' + ('e' * 64) + '"}'), @{}, 'does not name run bound'),
            @('a matching-token wrong-run journal', ('{"runId":"other","token":"' + [string]$lock.Token + '"}'), @{}, 'does not name run bound'),
            @('a changed reviewed pin', ('{"runId":"bound","token":"' + [string]$lock.Token + '"}'), @{ $pinned = ('0' * 64) }, 'reviewed recovery pin differs'))
        foreach ($c in $cases) {
            [IO.File]::WriteAllText($journal, $c[1])
            Assert-Throws { [void](Enter-KmcBoundRecoveryReservation -StateRoot $state -RunId 'bound' -RequiredJournal @($journal) -Pins $c[2] -Purpose 'recovery') } $c[3] $c[0]
            Assert-That ((Get-Sha $convention.RecordPath) -ceq $leaseBefore -and (Get-Sha $lock.Path) -ceq $lockBefore) ($c[0] + ' changed the lease or the lock')
        }
        Remove-Item -LiteralPath $journal -Force
        Assert-Throws { [void](Enter-KmcBoundRecoveryReservation -StateRoot $state -RunId 'bound' -RequiredJournal @($journal) -Purpose 'recovery') } 'lacks a required recovery journal' 'a recovery without its mandatory journal'
        Assert-Throws { [void](Enter-KmcBoundRecoveryReservation -StateRoot $state -RunId 'bound' -AlternativeJournal @($journal, (Join-Path $state 'other.json')) -Purpose 'recovery') } 'has no recovery journal' 'a recovery without any alternative journal'
        # A pinned snapshot with its mandatory combined journal missing still refuses.
        [IO.File]::WriteAllText($journal, ('{"runId":"bound","token":"' + [string]$lock.Token + '"}'))
        Assert-Throws { [void](Enter-KmcBoundRecoveryReservation -StateRoot $state -RunId 'bound' -RequiredJournal @($journal, (Get-KmcRunTransactionStatePath $state 'bound')) -Pins @{ $pinned = $pinSha } -Purpose 'recovery') } 'lacks a required recovery journal' 'a pinned snapshot without its combined journal'
        Remove-Item -LiteralPath $journal -Force
        Assert-That ((Get-Sha $convention.RecordPath) -ceq $leaseBefore -and (Get-Sha $lock.Path) -ceq $lockBefore) 'a recovery without its journals changed the lease or the lock'
        [IO.File]::WriteAllText($journal, ('{"runId":"bound","token":"' + [string]$lock.Token + '"}'))
        $session = Enter-KmcBoundRecoveryReservation -StateRoot $state -RunId 'bound' -RequiredJournal @($journal) -Pins @{ $pinned = $pinSha } -Purpose 'recovery'
        $adopted = Adopt-KmcStaleRuntimeLock -StateRoot $state -ExpectedRunId 'bound' -ExpectedLockToken ([string]$lock.Token) -Reservation $session
        Close-KmcRuntimeLock $adopted
        Assert-That (-not (Test-Path -LiteralPath $convention.RecordPath)) 'the bound recovery did not release'
    }

    Invoke-Case 'the local unlink is effective under inherited -WhatIf and confirmed: success removes lock then lease; a failed unlink keeps the lease and peers out' {
        param($case)
        $state = Get-State $case
        $lock = Open-KmcRuntimeLock -StateRoot $state -RunId 'unlink'
        & { $WhatIfPreference = $true; Close-KmcRuntimeLock $lock }
        Assert-That (-not (Test-Path -LiteralPath $lock.Path) -and -not (Test-Path -LiteralPath $convention.RecordPath)) 'a -WhatIf close simulated the unlink or kept the lease'
        $lock = Open-KmcRuntimeLock -StateRoot $state -RunId 'unlink-fails'
        $hook = Join-Path $case 'hook.ps1'
        [IO.File]::WriteAllText($hook, ("(Get-Item -LiteralPath '" + $lock.Path + "').Attributes='ReadOnly'"))
        [Environment]::SetEnvironmentVariable('KMC_NATIVE_RESERVATION_TEST_HOOK_CLOSE_BEFORE_UNLINK', $hook, 'Process')
        try { Assert-Throws { & { $WhatIfPreference = $true; Close-KmcRuntimeLock $lock } } 'could not be removed' 'a close whose unlink fails' }
        finally { [Environment]::SetEnvironmentVariable('KMC_NATIVE_RESERVATION_TEST_HOOK_CLOSE_BEFORE_UNLINK', $null, 'Process') }
        Assert-That ((Test-Path -LiteralPath $lock.Path) -and [string](Read-Lease).runId -ceq 'unlink-fails') 'a failed unlink released the reservation'
        Assert-Throws { [void](Open-KmcRuntimeLock -StateRoot (Get-State $case 'peer') -RunId 'peer') } 'reserved by KingmakerMountedCombat run unlink-fails' 'a peer after a failed unlink'
        (Get-Item -LiteralPath $lock.Path).Attributes = 'Normal'
    }

    Invoke-Case 'an adopted transition that finds its lease replaced records the first incident and leaves the replacement byte-identical' {
        param($case)
        $state = Get-State $case
        $lock = New-AbandonedTransaction $state 'adopted'
        $session = Enter-KmcStaleTransactionReservation -StateRoot $state -ExpectedRunId 'adopted' -ExpectedLockToken ([string]$lock.Token) -Purpose 'recovery-preview'
        Set-LeaseField 'token' ([Guid]::NewGuid().ToString('N'))
        $replaced = Get-Sha $convention.RecordPath
        Assert-Throws { Exit-KmcUnlockedNativeReservation $session } 'no longer exactly this session' 'an adopted preview cleanup over a replaced lease'
        Assert-That ((Get-Sha $convention.RecordPath) -ceq $replaced) 'the replacement changed'
        Assert-That (Test-Path -LiteralPath (Join-Path $state 'native-reservation-incidents\adopted.first.json')) 'the adopted transition recorded no first incident'
    }

    Invoke-Case 'borrowing is bound to the outer owner''s process and runtime-state root: an independent launcher with the token and another root refuse' {
        param($case)
        $state = Get-State $case
        $outer = Enter-KmcNativeReservation -StateRoot $state -RunId 'closed-preflight' -Purpose 'preparation-scoped-preflight'
        $result = Join-Path $case 'independent.txt'
        $script = Join-Path $case 'independent.ps1'
        [IO.File]::WriteAllText($script, (". '" + (Join-Path $PSScriptRoot 'runtime\RuntimeHarness.Common.ps1') + "'`ntry { [void](Enter-KmcEntrypointReservation -StateRoot '" + $state + "' -RunId 'independent' -Purpose 'runtime-preview'); [IO.File]::WriteAllText('" + $result + "','borrowed') } catch { [IO.File]::WriteAllText('" + $result + "',('refused: ' + `$_.Exception.Message)) }"))
        [Environment]::SetEnvironmentVariable('KMC_NATIVE_RESERVATION_BORROW_TOKEN', [string]$outer.Token, 'Process')
        try {
            $process = Start-Process -FilePath 'powershell.exe' -ArgumentList @('-NoProfile', '-NonInteractive', '-ExecutionPolicy', 'Bypass', '-File', ('"' + $script + '"')) -WindowStyle Hidden -PassThru
            $contenders.Add($process); [void]$process.WaitForExit(120000)
            Assert-That ([IO.File]::ReadAllText($result) -like 'refused: *not the running scoped preflight*') ('an independent launcher process with the token: ' + [IO.File]::ReadAllText($result))
            Assert-Throws { [void](Enter-KmcEntrypointReservation -StateRoot (Get-State $case 'other-root') -RunId 'other' -Purpose 'runtime-preview') } 'not the running scoped preflight' 'borrowing for another runtime-state root'
            $inner = Enter-KmcEntrypointReservation -StateRoot $state -RunId 'guard' -Purpose 'runtime-preview'
            Assert-That ($inner.Borrowed) 'the same-process guard was not admitted'
        }
        finally { [Environment]::SetEnvironmentVariable('KMC_NATIVE_RESERVATION_BORROW_TOKEN', $null, 'Process') }
        Exit-KmcNativeReservation $outer
    }

    Invoke-Case 'a private reservation: profile snapshot and Areas intake refuse every non-disposable surface before opening it; the disposable snapshot registers its obligation' {
        param($case)
        . (Join-Path $PSScriptRoot 'runtime\PersistenceProfileProtection.ps1')
        . (Join-Path $PSScriptRoot 'runtime\PersistenceProfileAreasIsolation.ps1')
        $state = Get-State $case
        $profile = Join-Path $case 'profile'; $saves = Join-Path $profile 'Saved Games'; $game = Join-Path $case 'game'; $backups = Join-Path $case 'backups'
        $params = Join-Path $game 'Kingmaker_Data\Managed\UnityModManager\Params.xml'
        foreach ($d in @($saves, (Split-Path -Parent $params), $backups)) { [void][IO.Directory]::CreateDirectory($d) }
        [IO.File]::WriteAllText($params, '<Params />'); [IO.File]::WriteAllText((Join-Path $profile 'sentinel.txt'), 'in scope')
        $lock = Open-KmcRuntimeLock -StateRoot $state -RunId 'surfaces'
        try {
            foreach ($bad in @(@('C:\Windows\kmc-profile\Saved Games', $game, $backups), @($saves, 'C:\Windows\kmc-game', $backups), @($saves, $game, 'C:\Windows\kmc-backups'))) {
                Assert-Throws { [void](New-KmcPersistenceProfileSnapshot -Lock $lock -SaveRoot $bad[0] -GameRoot $bad[1] -BackupRoot $bad[2]) } 'cannot cover the shared surface' ('a snapshot over ' + ($bad -join ', '))
            }
            Assert-That (-not (Test-Path -LiteralPath (Join-Path $backups 'profile-surfaces'))) 'a refused snapshot wrote a backup'
            Assert-Throws { [void](Enter-KmcPersistenceAreasIsolation -Lock $lock -Snapshot ([pscustomobject]@{ runId = 'surfaces'; token = $lock.Token; profile = 'C:\Windows\kmc-profile' }) -BackupRoot $backups) } 'cannot cover the shared surface' 'Areas intake over a real profile'
            $snapshot = New-KmcPersistenceProfileSnapshot -Lock $lock -SaveRoot $saves -GameRoot $game -BackupRoot $backups
            Assert-That ((Get-KmcRunObligationState -StateRoot $state -RunId 'surfaces' -BackupRoot $backups).Started -ccontains 'profile') 'the snapshot did not register the profile obligation'
            Assert-That ([string]$snapshot.profile -ceq [IO.Path]::GetFullPath($profile)) 'the disposable snapshot names another profile'
        }
        finally { $lock.Stream.Dispose() }
    }

    Invoke-Case 'the closed preflight''s nested admission: the guard program it builds from the launcher contains the borrowing admission, and the entry borrows only around that guard' {
        param($case)
        # The closed preflight itself cannot run privately (it refuses private mode, needs the reviewed full-audit proof and
        # pinned platform, and its guard prefix reads the configured live installation right after admission). Its own
        # program builder is run here on the candidate launcher, and the borrowed admission of that program is exercised
        # in-process under an outer lease (the borrowing case above).
        . (Join-Path $PSScriptRoot 'runtime\ScopedPreflightProof.ps1')
        . (Join-Path $PSScriptRoot 'runtime\ScopedPreflightGuards.ps1')
        $launcher = Join-Path $PSScriptRoot 'runtime\Invoke-KingmakerRuntimeScenario.ps1'
        $program = Get-KmcScopedOriginalGuardProgram $launcher ((Get-FileHash -LiteralPath $launcher).Hash.ToLowerInvariant())
        $calls = @($program.script.Ast.FindAll({ param($n) $n -is [Management.Automation.Language.CommandAst] }, $true) | ForEach-Object { $_.GetCommandName() })
        Assert-That ($calls -ccontains 'Enter-KmcEntrypointReservation' -and $calls -cnotcontains 'Enter-KmcNativeReservation') 'the closed guard program does not admit through the borrowing entrypoint'
        $entry = Get-Text 'runtime\Invoke-Chunk7ScopedPreflight.ps1'
        Assert-That ($entry.Contains("[Environment]::SetEnvironmentVariable('KMC_NATIVE_RESERVATION_BORROW_TOKEN',`$scopedReservationToken,'Process')`n try{`$result=Invoke-KmcScopedFreshGuards `$proof `$code `$inputs `$owned `$rows}finally{[Environment]::SetEnvironmentVariable('KMC_NATIVE_RESERVATION_BORROW_TOKEN',`$null,'Process')}")) 'the closed entry does not borrow exactly around its fresh guard'
    }

    Invoke-Case 'the requalification recovery caller binds its journal before any takeover; a missing journal is admitted only with proven pre-journal evidence' {
        param($case)
        $state = Get-State $case
        $saves = Get-State $case 'saves'
        $guard = Join-Path $PSScriptRoot 'runtime\Test-KmcFixtureGuard.ps1'
        [IO.File]::WriteAllText((Join-Path $state 'fixture-qualification.json'), '{"stand-in":"qualification"}'); $script:pinnedQualification = Get-KmcSha256 (Join-Path $state 'fixture-qualification.json')
        $recover = { & $guard -SaveRoot $saves -StateRoot $state -RecoverWorkingRequalification -RequalificationRunId 'requal' -ExpectedExistingQualificationSha256 $script:pinnedQualification -ExpectedBaselineSha256 ('2' * 64) -ExpectedSupersededWorkingSha256 ('3' * 64) -ExpectedRevisedWorkingSha256 ('4' * 64) -WhatIf }
        $lock = Open-KmcRuntimeLock -StateRoot $state -RunId 'requal' -Purpose 'fixture-requalification'
        Register-KmcRunObligation -Lock $lock -Name 'fixture-requalification'
        Abandon-KmcRuntimeLock $lock
        $payload = Read-KmcJson $lock.Path; $payload.ownerProcessId = 2147483646; Write-KmcJsonAtomic -Path $lock.Path -Value $payload
        Set-LeaseField 'holderProcessId' (Get-DeadProcessId)
        $journal = Join-Path $state 'fixture-requalifications\requal.json'
        [void][IO.Directory]::CreateDirectory((Split-Path -Parent $journal))
        $leaseBefore = Get-Sha $convention.RecordPath; $lockBefore = Get-Sha $lock.Path
        foreach ($bad in @(('{"runId":"requal","token":"' + ('e' * 64) + '","phase":"prepared"}'), ('{"runId":"other","token":"' + [string]$lock.Token + '","phase":"prepared"}'))) {
            [IO.File]::WriteAllText($journal, $bad)
            Assert-Throws $recover 'does not name run requal' "the guard recovery over journal $bad"
            Assert-That ((Get-Sha $convention.RecordPath) -ceq $leaseBefore -and (Get-Sha $lock.Path) -ceq $lockBefore) 'a refused guard recovery changed the lease or the lock'
        }
        # A missing journal with a conflicting run artifact is not a proven pre-journal interruption.
        Remove-Item -LiteralPath $journal -Force
        $conflict = Join-Path $state 'fixture-recoveries\requal.json'; [void][IO.Directory]::CreateDirectory((Split-Path -Parent $conflict))
        [IO.File]::WriteAllText($conflict, ('{"schemaVersion":1,"runId":"requal","token":"' + [string]$lock.Token + '","phase":"prepared"}'))
        Assert-Throws $recover 'does not prove an interruption before the journal' 'the guard recovery with a missing journal and a conflicting artifact'
        Assert-That ((Get-Sha $convention.RecordPath) -ceq $leaseBefore -and (Get-Sha $lock.Path) -ceq $lockBefore) 'the conflicting-artifact refusal changed the lease or the lock'
        Remove-Item -LiteralPath $conflict -Force
        # Missing or changed pinned qualification, or a prior backup that is not its exact copy: refused before any takeover.
        $qualification = Join-Path $state 'fixture-qualification.json'; $qualificationBytes = [IO.File]::ReadAllBytes($qualification)
        $prior = Join-Path $state 'fixture-requalifications\requal.prior.json'
        foreach ($variant in @('missing', 'changed', 'prior')) {
            switch ($variant) { 'missing' { Remove-Item -LiteralPath $qualification -Force } 'changed' { [IO.File]::WriteAllText($qualification, 'changed') } 'prior' { [IO.File]::WriteAllBytes($qualification, $qualificationBytes); [IO.File]::WriteAllText($prior, 'not the pinned qualification') } }
            Assert-Throws $recover 'before any takeover' "the pre-journal recovery with a $variant qualification"
            Assert-That ((Get-Sha $convention.RecordPath) -ceq $leaseBefore -and (Get-Sha $lock.Path) -ceq $lockBefore) "the $variant qualification refusal changed the lease or the lock"
        }
        Remove-Item -LiteralPath $prior -Force
        # The genuine pre-journal interruption (only the ledger entry, the exact pinned qualification) is admitted: the takeover happens.
        $genuine = ''
        try { & $recover | Out-Null } catch { $genuine = $_.Exception.Message }
        Assert-That ($genuine -notlike '*does not prove*' -and $genuine -notlike '*refuses before any takeover*' -and (Get-Sha $convention.RecordPath) -cne $leaseBefore) ('the genuine pre-journal interruption was not admitted: ' + $genuine)
    }

    Invoke-Case 'Areas isolation records each custody change: a loss after the original move stops the copy, keeps the replacement and names the held original' {
        param($case)
        . (Join-Path $PSScriptRoot 'runtime\PersistenceProfileProtection.ps1')
        . (Join-Path $PSScriptRoot 'runtime\PersistenceProfileAreasIsolation.ps1')
        $fakeLab = Get-State $case 'lab'
        function Get-KmcLabRoot { return $fakeLab }
        $state = Get-State $case
        $profile = Join-Path $case 'profile'; $saves = Join-Path $profile 'Saved Games'; $game = Join-Path $case 'game'; $backups = Join-Path $case 'backups'
        $params = Join-Path $game 'Kingmaker_Data\Managed\UnityModManager\Params.xml'
        foreach ($d in @($saves, (Join-Path $profile 'Areas'), (Split-Path -Parent $params), $backups)) { [void][IO.Directory]::CreateDirectory($d) }
        [IO.File]::WriteAllText((Join-Path $profile 'Areas\original.bin'), 'original'); [IO.File]::WriteAllText($params, '<Params />')
        Write-KmcJsonAtomic (Join-Path $fakeLab 'environment-intake.json') ([ordered]@{ requestedLayout = [ordered]@{ kingmakerSaveRoot = $saves; kingmakerInstallDir = $game } })
        $lock = Open-KmcRuntimeLock -StateRoot $state -RunId 'areas-loss'
        try {
            $snapshot = New-KmcPersistenceProfileSnapshot -Lock $lock -SaveRoot $saves -GameRoot $game -BackupRoot $backups
            $peer = ((New-RecordValue 'KingmakerFlight' 4 '2020-01-01T00:00:00Z' 'peer-run') | ConvertTo-Json -Compress)
            $hook = Join-Path $case 'hook.ps1'; [IO.File]::WriteAllText($hook, ("[IO.File]::WriteAllText('" + $convention.RecordPath + "','" + $peer + "')"))
            [Environment]::SetEnvironmentVariable('KMC_NATIVE_RESERVATION_TEST_HOOK_AREAS_AFTER_ORIGINAL_MOVE', $hook, 'Process')
            try { Assert-Throws { [void](Enter-KmcPersistenceAreasIsolation -Lock $lock -Snapshot $snapshot -BackupRoot $backups) } 'lost' 'the Areas copy after a loss' }
            finally { [Environment]::SetEnvironmentVariable('KMC_NATIVE_RESERVATION_TEST_HOOK_AREAS_AFTER_ORIGINAL_MOVE', $null, 'Process') }
            $held = Join-Path $backups 'profile-areas-loss\Areas-original'
            Assert-That ((Test-Path -LiteralPath (Join-Path $held 'original.bin')) -and -not (Test-Path -LiteralPath (Join-Path $profile 'Areas'))) 'the copy ran after the loss or the original moved again'
            Assert-That ([IO.File]::ReadAllText($convention.RecordPath) -ceq $peer) 'the replacement changed'
            $incident = Read-KmcJson (Join-Path $state 'native-reservation-incidents\areas-loss.first.json')
            Assert-That ((@($incident.restorationPerformed) -join '|') -like ('*original Areas held at ' + $held + '*live Areas absent*')) 'the first incident does not describe the held original and the absent live tree'
        }
        finally { $lock.Stream.Dispose() }
    }

    Invoke-Case 'the close keeps the checked identities: a substituted local lock or a changed record run stops the unlink with an incident' {
        param($case)
        $state = Get-State $case
        $lock = Open-KmcRuntimeLock -StateRoot $state -RunId 'identity'
        $hook = Join-Path $case 'substitute.ps1'
        [IO.File]::WriteAllText($hook, ("Remove-Item -LiteralPath '" + $lock.Path + "' -Force; [IO.File]::WriteAllText('" + $lock.Path + "','substituted lock')"))
        [Environment]::SetEnvironmentVariable('KMC_NATIVE_RESERVATION_TEST_HOOK_CLOSE_BEFORE_UNLINK', $hook, 'Process')
        try { Assert-Throws { Close-KmcRuntimeLock $lock } 'another file replaced the held local lock' 'a close over a substituted local lock' }
        finally { [Environment]::SetEnvironmentVariable('KMC_NATIVE_RESERVATION_TEST_HOOK_CLOSE_BEFORE_UNLINK', $null, 'Process') }
        Assert-That ([IO.File]::ReadAllText($lock.Path) -ceq 'substituted lock' -and [string](Read-Lease).runId -ceq 'identity') 'the substituted lock or the reservation did not survive'
        Assert-That (Test-Path -LiteralPath (Join-Path $state 'native-reservation-incidents\identity.first.json')) 'the substitution recorded no incident'
        Remove-Item -LiteralPath $lock.Path -Force; Remove-Item -LiteralPath $convention.RecordPath -Force
        $lock = Open-KmcRuntimeLock -StateRoot (Get-State $case 'run') -RunId 'record-run'
        Set-LeaseField 'runId' 'someone-else'
        $changed = Get-Sha $convention.RecordPath
        Assert-Throws { [void](Assert-KmcRuntimeLockOwner $lock) } 'lost' 'the ownership pair after the record''s run changed with the same token'
        Assert-Throws { Close-KmcRuntimeLock $lock } 'lost' 'a close after the record''s run changed with the same token'
        Assert-That ((Get-Sha $convention.RecordPath) -ceq $changed -and (Test-Path -LiteralPath $lock.Path)) 'the changed record or the lock was removed'
    }

    Invoke-Case 'owned shared reads: under the caller''s held lock (peers excluded), standalone under a reservation of its own, refused before reading under an occupied lease' {
        param($case)
        $state = Get-State $case
        $lock = Open-KmcRuntimeLock -StateRoot $state -RunId 'validator-owner'
        try {
            $read = Invoke-KmcOwnedSharedRead -OwnerLock $lock -Purpose 'request-validation' -Path @((Join-Path $case 'stand-in-mono.dll')) -Read { Assert-Throws { [void](Open-KmcRuntimeLock -StateRoot (Get-State $case 'peer') -RunId 'peer') } 'reserved by KingmakerMountedCombat run validator-owner' 'a peer during the owned read'; 'read under the owner' }
            Assert-That ($read -ceq 'read under the owner' -and [string](Read-Lease).runId -ceq 'validator-owner') 'the owned read did not run or released the owner''s reservation'
        }
        finally { Close-KmcRuntimeLock $lock }
        Set-RecordValue (New-RecordValue 'KingmakerFlight' 4 '2020-01-01T00:00:00Z' 'peer-run')
        $script:sharedReadRan = $false
        Assert-Throws { Invoke-KmcOwnedSharedRead -StateRoot $state -Purpose 'starting-installation' -Path @($case) -Read { $script:sharedReadRan = $true } } 'reserved by KingmakerFlight run peer-run' 'a standalone read under an occupied lease'
        Assert-That (-not $script:sharedReadRan) 'the standalone read ran under an occupied lease'
        Remove-Item -LiteralPath $convention.RecordPath -Force
        $read = Invoke-KmcOwnedSharedRead -StateRoot $state -Purpose 'starting-installation' -Path @($case) -Read { [string](Read-Lease).purpose }
        Assert-That ($read -ceq 'preparation-starting-installation' -and -not (Test-Path -LiteralPath $convention.RecordPath)) 'the standalone read did not hold, then release, its own reservation'
    }

    Invoke-Case 'a private snapshot refuses a redirected backup ancestor and a changed convention before opening anything' {
        param($case)
        . (Join-Path $PSScriptRoot 'runtime\PersistenceProfileProtection.ps1')
        $state = Get-State $case
        $profile = Join-Path $case 'profile'; $saves = Join-Path $profile 'Saved Games'; $game = Join-Path $case 'game'
        $params = Join-Path $game 'Kingmaker_Data\Managed\UnityModManager\Params.xml'
        foreach ($d in @($saves, (Split-Path -Parent $params))) { [void][IO.Directory]::CreateDirectory($d) }
        [IO.File]::WriteAllText($params, '<Params />')
        $elsewhere = Get-State $case 'elsewhere'; $link = Join-Path $case 'redirect'
        [void](New-Item -ItemType Junction -Path $link -Target $elsewhere)
        $lock = Open-KmcRuntimeLock -StateRoot $state -RunId 'redirect'
        try {
            Assert-Throws { [void](New-KmcPersistenceProfileSnapshot -Lock $lock -SaveRoot $saves -GameRoot $game -BackupRoot (Join-Path $link 'backups')) } 'reparse' 'a snapshot whose backup ancestor redirects'
            Assert-That (@(Get-ChildItem -LiteralPath $elsewhere -Force).Count -eq 0) 'the redirected destination was written'
            $selected = [Environment]::GetEnvironmentVariable('KMC_NATIVE_RESERVATION_TEST_ROOT', 'Process')
            try {
                [Environment]::SetEnvironmentVariable('KMC_NATIVE_RESERVATION_TEST_ROOT', (Join-Path $case 'another-root'), 'Process')
                Assert-Throws { [void](New-KmcPersistenceProfileSnapshot -Lock $lock -SaveRoot $saves -GameRoot $game -BackupRoot (Join-Path $case 'backups')) } 'convention changed under its held lock' 'a snapshot after the ambient convention changed'
                [Environment]::SetEnvironmentVariable('KMC_NATIVE_RESERVATION_TEST_ROOT', $null, 'Process')
                Assert-Throws { [void](Get-KmcPersistencePlayerPrefs -Lock $lock) } 'convention changed under its held lock' 'preferences under a private lock with no ambient convention'
            }
            finally { [Environment]::SetEnvironmentVariable('KMC_NATIVE_RESERVATION_TEST_ROOT', $selected, 'Process') }
            Assert-That (-not (Test-Path -LiteralPath (Join-Path $case 'backups'))) 'a refused snapshot created its backup'
        }
        finally { $lock.Stream.Dispose(); [IO.Directory]::Delete($link) }
    }

    Invoke-Case 'preference restoration rechecks ownership before each preference write and records each completed one' {
        param($case)
        . (Join-Path $PSScriptRoot 'runtime\PersistenceProfileProtection.ps1')
        $fakeLab = Get-State $case 'lab'
        function Get-KmcLabRoot { return $fakeLab }
        $state = Get-State $case
        $profile = Join-Path $case 'profile'; $saves = Join-Path $profile 'Saved Games'; $game = Join-Path $case 'game'; $backups = Join-Path $case 'backups'
        $params = Join-Path $game 'Kingmaker_Data\Managed\UnityModManager\Params.xml'
        foreach ($d in @($saves, (Split-Path -Parent $params), $backups)) { [void][IO.Directory]::CreateDirectory($d) }
        [IO.File]::WriteAllText($params, '<Params />')
        Write-KmcJsonAtomic (Join-Path $fakeLab 'environment-intake.json') ([ordered]@{ requestedLayout = [ordered]@{ kingmakerSaveRoot = $saves; kingmakerInstallDir = $game } })
        $lock = Open-KmcRuntimeLock -StateRoot $state -RunId 'preferences'
        $keyPath = Get-KmcPlayerPrefsKeyPath -Lock $lock
        $key = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey($keyPath)
        try {
            $names = @('KingdomDifficulty_h4200925179', 'unity.player_session_count_h922449978')
            foreach ($name in $names) { $key.SetValue($name, [byte[]]@(1, 1), [Microsoft.Win32.RegistryValueKind]::Binary) }
            $snapshot = New-KmcPersistenceProfileSnapshot -Lock $lock -SaveRoot $saves -GameRoot $game -BackupRoot $backups
            foreach ($name in $names) { $key.SetValue($name, [byte[]]@(2, 2), [Microsoft.Win32.RegistryValueKind]::Binary) }
            $peer = ((New-RecordValue 'KingmakerFlight' 4 '2020-01-01T00:00:00Z' 'peer-run') | ConvertTo-Json -Compress)
            $hook = Join-Path $case 'hook.ps1'; [IO.File]::WriteAllText($hook, ("[IO.File]::WriteAllText('" + $convention.RecordPath + "','" + $peer + "')"))
            [Environment]::SetEnvironmentVariable('KMC_NATIVE_RESERVATION_TEST_HOOK_SETTINGS_AFTER_PREFERENCE', $hook, 'Process')
            try { Assert-Throws { Restore-KmcPersistenceStartupSettings -Lock $lock -Snapshot $snapshot -BackupRoot $backups -ExpectedCurrentParamsSha256 (Get-KmcSha256 $params) -ExpectedCurrentPrefsSha256 (Get-KmcTextSha256 (Get-KmcPersistencePlayerPrefs -Lock $lock)) -Confirm:$false } 'lost' 'the second preference write after a loss' }
            finally { [Environment]::SetEnvironmentVariable('KMC_NATIVE_RESERVATION_TEST_HOOK_SETTINGS_AFTER_PREFERENCE', $null, 'Process') }
            $values = @($names | ForEach-Object { [string]::Join(',', [byte[]]$key.GetValue($_)) })
            Assert-That (@($values | Where-Object { $_ -ceq '1,1' }).Count -eq 1 -and @($values | Where-Object { $_ -ceq '2,2' }).Count -eq 1) ('the writes did not stop after the first preference: ' + ($values -join ' / '))
            Assert-That ([IO.File]::ReadAllText($convention.RecordPath) -ceq $peer) 'the replacement changed'
            $incident = Read-KmcJson (Join-Path $state 'native-reservation-incidents\preferences.first.json')
            Assert-That ((@($incident.restorationPerformed) -join '|') -like '*preference restored: *') 'the first incident does not name the completed preference write'
        }
        finally { $key.Dispose(); [Microsoft.Win32.Registry]::CurrentUser.DeleteSubKeyTree($keyPath, $false); $lock.Stream.Dispose() }
    }

    Invoke-Case 'importing the harness compiles no generated assembly; the file-identity type appears only when a lock is closed' {
        param($case)
        $probe = Join-Path $case 'probe.txt'
        $script = Join-Path $case 'probe.ps1'
        [IO.File]::WriteAllText($script, (". '" + (Join-Path $PSScriptRoot 'runtime\RuntimeHarness.Common.ps1') + "'`n[IO.File]::WriteAllText('" + $probe + "', [string][bool]('KingmakerMountedCombat.Reservation.FileIdentity' -as [type]) + ' ' + @([AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { `$_.IsDynamic -eq `$false -and [string]::IsNullOrEmpty(`$_.Location) }).Count)"))
        $process = Start-Process -FilePath 'powershell.exe' -ArgumentList @('-NoProfile', '-NonInteractive', '-ExecutionPolicy', 'Bypass', '-File', ('"' + $script + '"')) -WindowStyle Hidden -PassThru
        $contenders.Add($process); [void]$process.WaitForExit(120000)
        Assert-That ([IO.File]::ReadAllText($probe) -ceq 'False 0') ('importing the harness loaded a generated assembly: ' + [IO.File]::ReadAllText($probe))
    }

    Invoke-Case 'a close refuses when the local lock''s ownership bytes changed in place (same file); bytes, record and incident survive' {
        param($case)
        $state = Get-State $case
        $lock = Open-KmcRuntimeLock -StateRoot $state -RunId 'in-place'
        $changed = '{"schemaVersion":1,"runId":"someone-else","token":"' + ('e' * 64) + '","ownerProcessId":4,"createdAtUtc":"2020-01-01T00:00:00Z"}'
        $hook = Join-Path $case 'rewrite.ps1'
        [IO.File]::WriteAllText($hook, ("`$s=[IO.File]::Open('" + $lock.Path + "','Open','ReadWrite','None'); `$b=[Text.Encoding]::UTF8.GetBytes('" + $changed + "'); `$s.SetLength(0); `$s.Write(`$b,0,`$b.Length); `$s.Dispose()"))
        [Environment]::SetEnvironmentVariable('KMC_NATIVE_RESERVATION_TEST_HOOK_CLOSE_BEFORE_UNLINK', $hook, 'Process')
        try { Assert-Throws { Close-KmcRuntimeLock $lock } 'ownership bytes changed' 'a close over in-place changed ownership bytes' }
        finally { [Environment]::SetEnvironmentVariable('KMC_NATIVE_RESERVATION_TEST_HOOK_CLOSE_BEFORE_UNLINK', $null, 'Process') }
        Assert-That ([IO.File]::ReadAllText($lock.Path) -ceq $changed -and [string](Read-Lease).runId -ceq 'in-place') 'the changed lock bytes or the reservation did not survive'
        Assert-That (Test-Path -LiteralPath (Join-Path $state 'native-reservation-incidents\in-place.first.json')) 'the in-place change recorded no incident'
    }

    Invoke-Case 'owned reads need a verified paired reservation and inputs inside its convention: a local-only lock and an out-of-scope input refuse before reading' {
        param($case)
        $state = Get-State $case
        $lock = Open-KmcRuntimeLock -StateRoot $state -RunId 'owned-paths'
        try {
            $localOnly = [pscustomobject]@{ Path = $lock.Path; Stream = $lock.Stream; RunId = $lock.RunId; Token = $lock.Token }
            $script:pathReadRan = $false
            Assert-Throws { Invoke-KmcOwnedSharedRead -OwnerLock $localOnly -Purpose 'request-validation' -Path @($case) -Read { $script:pathReadRan = $true } } 'no verified reservation of its own' 'an owned read under a local-only lock'
            Assert-Throws { Invoke-KmcOwnedSharedRead -OwnerLock $lock -Purpose 'request-validation' -Path @('C:\Windows\kmc-live-mono.dll') -Read { $script:pathReadRan = $true } } 'cannot cover the shared surface' 'an owned read of an out-of-scope input'
            Assert-That (-not $script:pathReadRan) 'a refused owned read ran'
            Assert-That ((Invoke-KmcOwnedSharedRead -OwnerLock $lock -Purpose 'request-validation' -Path @($case) -Read { 'ok' }) -ceq 'ok') 'a valid owned stand-in read did not run'
        }
        finally { Close-KmcRuntimeLock $lock }
    }

    Invoke-Case 'the loader selection is bound to the run and token by digest: a substituted valid receipt keeps the obligation open and completion refused' {
        param($case)
        $makeGame = {
            param([string]$Game)
            $loader = Join-Path $Game 'Kingmaker_Data\Managed\UnityModManager'
            [void][IO.Directory]::CreateDirectory($loader); [void][IO.Directory]::CreateDirectory((Join-Path $Game 'Mods'))
            [IO.File]::WriteAllText((Join-Path $loader 'Config.xml'), '<Config><ModsDirectory>Mods</ModsDirectory><ModInfo>Info.json</ModInfo><GameExe>Kingmaker.exe</GameExe></Config>')
            [IO.File]::WriteAllText((Join-Path $loader 'Params.xml'), '<Param><ShowOnStart>1</ShowOnStart><ModParams><Mod Id="KingmakerMountedCombat" Enabled="true"><Hotkey><keyCode>None</keyCode><modifiers>0</modifiers></Hotkey></Mod></ModParams></Param>')
            foreach ($name in @('UnityModManager.dll', '0Harmony12.dll', '0Harmony-1.2.dll', '0Harmony.dll', 'dnlib.dll')) { [IO.File]::WriteAllText((Join-Path $loader $name), ('stand-in-' + $name + $Game)) }
            $assembly = Join-Path $Game 'Kingmaker_Data\Managed\Assembly-CSharp.dll'; [IO.File]::WriteAllText($assembly, ('stand-in-gameplay' + $Game))
            $pins = @(foreach ($pair in @(@('gameplayAssembly', $assembly), @('umm', (Join-Path $loader 'UnityModManager.dll')), @('harmony', (Join-Path $loader '0Harmony12.dll')))) { [pscustomobject]@{ role = $pair[0]; length = (Get-Item -LiteralPath $pair[1]).Length; sha256 = (Get-KmcSha256 $pair[1]) } })
            Get-KmcBaseGameDependencies -GameRoot $Game -Fingerprint ([pscustomobject]@{ kingmaker = [pscustomobject]@{ files = $pins } }) -RuntimeLoaderProfile ([pscustomobject]@{ Id = 'stand-in'; UmmSha256 = (Get-KmcSha256 (Join-Path $loader 'UnityModManager.dll')); Harmony2Sha256 = (Get-KmcSha256 (Join-Path $loader '0Harmony.dll')) })
        }
        $first = & $makeGame (Join-Path $case 'game-one'); $second = & $makeGame (Join-Path $case 'game-two')
        $state = Get-State $case
        $lock = Open-KmcRuntimeLock -StateRoot $state -RunId 'bound-loader'
        Register-KmcRunObligation -Lock $lock -Name 'installed-dependencies' -Evidence $first
        $evidence = Join-Path $state 'run-obligations\bound-loader.installed-dependencies.json'
        Assert-That ((Get-KmcRunObligationState -StateRoot $state -RunId 'bound-loader').Closed) 'the exact original intake did not verify'
        [IO.File]::WriteAllText((Join-Path $case 'game-one\Kingmaker_Data\Managed\UnityModManager\dnlib.dll'), 'changed first installation')
        Write-KmcJsonAtomic $evidence ([ordered]@{ schemaVersion = 1; runId = 'bound-loader'; token = [string]$lock.Token; name = 'installed-dependencies'; receipt = $second })
        $o = Get-KmcRunObligationState -StateRoot $state -RunId 'bound-loader'
        Assert-That ((@($o.Open) -join '|') -like '*installed-dependencies: its recorded loader selection is not the one bound to this run*') ('a substituted receipt was trusted: ' + (@($o.Open) -join '|'))
        $lock.Stream.Dispose(); Remove-Item -LiteralPath $lock.Path -Force
        Set-LeaseField 'holderProcessId' (Get-DeadProcessId)
        $before = Get-Sha $convention.RecordPath
        Assert-Throws { Complete-KmcInterruptedReservation -StateRoot $state -RunId 'bound-loader' -LeaseToken ([string](Read-Lease).token) } 'not the one bound to this run' 'completion over a substituted loader receipt'
        Assert-That ((Get-Sha $convention.RecordPath) -ceq $before) 'the substituted-receipt refusal changed the lease'
    }

    Invoke-Case 'an adopted preview hands its lease back once: a later cleanup of the same exit never transitions a newer takeover; abandon keeps the primary failure' {
        param($case)
        $state = Get-State $case
        $lock = New-AbandonedTransaction $state 'handback'
        $session = Enter-KmcStaleTransactionReservation -StateRoot $state -ExpectedRunId 'handback' -ExpectedLockToken ([string]$lock.Token) -Purpose 'recovery-preview'
        Exit-KmcUnlockedNativeReservation $session
        $second = Enter-KmcStaleTransactionReservation -StateRoot $state -ExpectedRunId 'handback' -ExpectedLockToken ([string]$lock.Token) -Purpose 'recovery'
        $takeover = Get-Sha $convention.RecordPath
        Exit-KmcUnlockedNativeReservation $session
        Assert-That ((Get-Sha $convention.RecordPath) -ceq $takeover -and -not (Test-Path -LiteralPath (Join-Path $state 'native-reservation-incidents\handback.first.json'))) 'a second cleanup of the same exit transitioned the newer takeover'
        $adopted = Adopt-KmcStaleRuntimeLock -StateRoot $state -ExpectedRunId 'handback' -ExpectedLockToken ([string]$lock.Token) -Reservation $second
        $adopted.Stream.Dispose()
        $primary = $null
        try { try { throw 'primary diagnosis' } finally { Abandon-KmcRuntimeLock $adopted } } catch { $primary = $_.Exception.Message }
        Assert-That ($primary -ceq 'primary diagnosis') ('abandon over a disposed handle replaced the primary failure: ' + $primary)
    }

    Invoke-Case 'the held private convention reaches the later unchanged-profile check: a changed ambient selection refuses Areas isolation before the preference key' {
        param($case)
        . (Join-Path $PSScriptRoot 'runtime\PersistenceProfileProtection.ps1')
        . (Join-Path $PSScriptRoot 'runtime\PersistenceProfileAreasIsolation.ps1')
        $fakeLab = Get-State $case 'lab'
        function Get-KmcLabRoot { return $fakeLab }
        $state = Get-State $case
        $profile = Join-Path $case 'profile'; $saves = Join-Path $profile 'Saved Games'; $game = Join-Path $case 'game'; $backups = Join-Path $case 'backups'
        $params = Join-Path $game 'Kingmaker_Data\Managed\UnityModManager\Params.xml'
        foreach ($d in @($saves, (Join-Path $profile 'Areas'), (Split-Path -Parent $params), $backups)) { [void][IO.Directory]::CreateDirectory($d) }
        [IO.File]::WriteAllText((Join-Path $profile 'Areas\original.bin'), 'original'); [IO.File]::WriteAllText($params, '<Params />')
        Write-KmcJsonAtomic (Join-Path $fakeLab 'environment-intake.json') ([ordered]@{ requestedLayout = [ordered]@{ kingmakerSaveRoot = $saves; kingmakerInstallDir = $game } })
        $lock = Open-KmcRuntimeLock -StateRoot $state -RunId 'later-check'
        try {
            $snapshot = New-KmcPersistenceProfileSnapshot -Lock $lock -SaveRoot $saves -GameRoot $game -BackupRoot $backups
            $selected = [Environment]::GetEnvironmentVariable('KMC_NATIVE_RESERVATION_TEST_ROOT', 'Process')
            try {
                [Environment]::SetEnvironmentVariable('KMC_NATIVE_RESERVATION_TEST_ROOT', (Join-Path $case 'another-root'), 'Process')
                Assert-Throws { [void](Enter-KmcPersistenceAreasIsolation -Lock $lock -Snapshot $snapshot -BackupRoot $backups) } 'convention changed under its held lock' 'Areas isolation after the ambient selection changed'
            }
            finally { [Environment]::SetEnvironmentVariable('KMC_NATIVE_RESERVATION_TEST_ROOT', $selected, 'Process') }
            Assert-That ((Test-Path -LiteralPath (Join-Path $profile 'Areas\original.bin')) -and -not (Test-Path -LiteralPath (Join-Path $backups 'profile-later-check\Areas-original'))) 'Areas moved after the refusal'
        }
        finally { $lock.Stream.Dispose() }
    }

    Invoke-Case 'completion uses the writers'' contracts: a truncated requalification record, a restored Working record with failed baseline protection and an orphan run artifact refuse' {
        param($case)
        $state = Get-State $case
        $backups = Join-Path $case 'runtime-backups'
        $lock = Open-KmcRuntimeLock -StateRoot $state -RunId 'contracts'
        $token = [string]$lock.Token
        Register-KmcRunObligation -Lock $lock -Name 'fixture-requalification'; Register-KmcRunObligation -Lock $lock -Name 'working'
        $lock.Stream.Dispose(); Remove-Item -LiteralPath $lock.Path -Force
        Set-LeaseField 'holderProcessId' (Get-DeadProcessId)
        $lease = [string](Read-Lease).token
        $requal = Join-Path $state 'fixture-requalifications\contracts.json'; $save = Get-KmcSaveTransactionStatePath $state 'contracts'
        foreach ($p in @($requal, $save)) { [void][IO.Directory]::CreateDirectory((Split-Path -Parent $p)) }
        [IO.File]::WriteAllText($requal, ('{"schemaVersion":1,"runId":"contracts","token":"' + $token + '","phase":"committed"}'))
        $restored = [ordered]@{ schemaVersion = 2; runId = 'contracts'; token = $token; phase = 'restored'; preparedAtUtc = 'x'; scenario = 'fixture-intake'; maxRuntimeArchiveWrites = 0; saveRoot = 'C:\s'; baselinePath = 'b'; baselineSha256 = ('1' * 64); baselineLength = 1; baselineLastWriteTimeUtcTicks = 1
            expectedGameName = 'g'; expectedGameId = 'i'; expectedArea = 'a'; workingPath = 'w'; workingSha256 = ('2' * 64); workingLength = 1; workingLastWriteTimeUtcTicks = 1; backupPath = 'k'; backupSha256 = ('3' * 64); backupLength = 1; artifactQuarantineRoot = 'q'
            beforeInventory = [ordered]@{ digest = ('4' * 64) }; restoreStartedAtUtc = 'x'; saveWriteAllowlistPassed = $true; runtimeInventoryDigest = ('5' * 64); workingDisposition = 'd'; artifactPlan = @(); artifactPlanDigest = ('6' * 64); recoveryPlannedAtUtc = 'x'
            baselineImmutable = $false; artifactsQuarantinedAtUtc = 'x'; workingRestoredAtUtc = 'x'; restoredInventoryDigest = ('4' * 64); restoredAtUtc = 'x' }
        Write-KmcJsonAtomic $save $restored
        $before = Get-Sha $convention.RecordPath
        $o = Get-KmcRunObligationState -StateRoot $state -RunId 'contracts' -BackupRoot $backups
        Assert-That ((@($o.Open) -join '|') -like '*fixture-requalification: its record is invalid*' -and (@($o.Open) -join '|') -like '*working: its restored record is invalid*') ('the writers'' contracts were not applied: ' + (@($o.Open) -join '|'))
        Assert-Throws { Complete-KmcInterruptedReservation -StateRoot $state -RunId 'contracts' -LeaseToken $lease -BackupRoot $backups } 'open obligations' 'completion over invalid terminal records'
        Remove-Item -LiteralPath $requal, $save -Force
        [void][IO.Directory]::CreateDirectory((Join-Path $backups 'contracts'))
        Assert-Throws { Complete-KmcInterruptedReservation -StateRoot $state -RunId 'contracts' -LeaseToken $lease -BackupRoot $backups } 'the ledger records no such child' 'completion over an orphan Mods backup directory'
        Assert-That ((Get-Sha $convention.RecordPath) -ceq $before) 'a refused completion changed the lease'
    }

    Invoke-Case 'entrypoints keep the reservation boundaries: launcher admission, cleanup and verified close; ownership before shared moves' {
        param($case)
        $launcher = Get-Text 'runtime\Invoke-KingmakerRuntimeScenario.ps1'
        $enter = $launcher.IndexOf('Enter-KmcEntrypointReservation -StateRoot $runtimeState', [StringComparison]::Ordinal)
        Assert-That ($enter -gt $launcher.IndexOf('Request-KmcWorkflowAdmission -RepoRoot', [StringComparison]::Ordinal)) 'the launcher reserves before workflow admission'
        foreach ($read in @('Get-KmcBaseGameDependencies -GameRoot', '$beforeMods=', '$beforeSaves=')) { Assert-That ($enter -lt $launcher.IndexOf($read, [StringComparison]::Ordinal)) "the launcher reads $read before reserving" }
        Assert-That ($launcher.Contains('trap { $trapReservation=Get-Variable -Name nativeReservation -ValueOnly -ErrorAction SilentlyContinue; Exit-KmcUnlockedNativeReservation $trapReservation -PreserveError;')) 'the launcher has no exception cleanup boundary that keeps both failures'
        Assert-That ($launcher.Contains("-RequestPath `$requestPath -PackageManifestPath `$packageManifestPath -OwnerLock `$lock") -and $launcher.Contains("Test-RuntimeGameResult.ps1') -OwnerLock `$lock")) 'the launcher does not pass its lock to the validators'
        Assert-That ($launcher.IndexOf("Register-KmcRunObligation -Lock `$lock -Name 'installed-dependencies' -Evidence `$baseGameDependencies", [StringComparison]::Ordinal) -lt $launcher.IndexOf('New-KmcPersistenceProfileSnapshot -Lock $lock', [StringComparison]::Ordinal)) 'the loader selection is not recorded before the profile snapshot'
        foreach ($validator in @('runtime\Test-RuntimeGameResult.ps1', 'runtime\Test-RuntimeResult.ps1')) { Assert-That ((Get-Text $validator).Contains("Test-RuntimeRequest.ps1') -RequestPath `$RequestPath -OwnerLock `$OwnerLock")) "$validator does not pass the owner's lock on" }
        Assert-That ((Get-Text 'runtime\Test-RuntimeGameResult.ps1').Contains("if(`$VerifyLiveWorkingIdentity){Invoke-KmcOwnedSharedRead -OwnerLock `$OwnerLock")) 'the live Working check is not owned'
        Assert-That ((Get-Text 'runtime\Test-RuntimeRequest.ps1').Contains("Invoke-KmcOwnedSharedRead -OwnerLock `$OwnerLock -Purpose 'request-validation'")) 'the F11 read is not owned'
        Assert-That (([regex]::Matches((Get-Text 'Test-Chunk2StartingInstallation.ps1'), "Invoke-KmcOwnedSharedRead -Purpose 'starting-installation'")).Count -eq 2) 'a starting-installation read is unowned'
        Assert-That (([regex]::Matches($launcher, 'Exit-KmcUnlockedNativeReservation \$nativeReservation  # after the last shared read')).Count -eq 2) 'the preview and declined returns do not release after their last shared read'
        Assert-That ($launcher.Contains("-Purpose `$(if(`$requestedWhatIf){'runtime-preview'}else{'runtime-scenario'})")) 'the launcher preview does not reserve'
        Assert-That ($launcher.Contains('if($modsRestored-and$saveProtection-and$processExited-and(-not$BaseGameOnly-or$baseGameDependenciesUnchanged)){$runObligations=Get-KmcRunObligationState') -and $launcher.Contains('if($null-ne$runObligations-and$runObligations.Closed){Close-KmcRuntimeLock $lock}')) 'the launcher can release after a failed installed-dependency postcheck'
        Assert-That ($launcher.Contains("[void](Assert-KmcRuntimeLockOwner `$lock)`n    Invoke-KmcFinalizeF1GameStart")) 'no ownership check immediately before launch'
        $recovery = Get-Text 'runtime\Recover-KingmakerRuntimeTransaction.ps1'
        Assert-That ($recovery.IndexOf('Enter-KmcBoundRecoveryReservation -StateRoot $stateRoot -RunId', [StringComparison]::Ordinal) -lt $recovery.IndexOf('$beforeMods=Get-KmcDirectoryManifest', [StringComparison]::Ordinal)) 'transaction recovery reads before reserving'
        Assert-That ($recovery.IndexOf('Enter-KmcBoundRecoveryReservation -StateRoot $stateRoot -RunId ([string]$raw.runId) -AlternativeJournal', [StringComparison]::Ordinal) -gt 0) 'transaction recovery changes ownership before binding its journal'
        Assert-That ($recovery.Contains("`$profilePending=Assert-KmcRecoveredRunObligations") -and $recovery.Contains("if(`$profilePending){Close-KmcRuntimeLock `$lock -KeepFor 'profile-recovery'}else{Close-KmcRuntimeLock `$lock}")) 'transaction recovery releases without its run''s obligation state'
        $profile = Get-Text 'runtime\Recover-KmcPersistenceProfile.ps1'
        Assert-That ($profile.IndexOf('Enter-KmcFollowUpReservation -StateRoot $stateRoot -RunId $RunId -RunToken $runToken', [StringComparison]::Ordinal) -lt $profile.IndexOf('(Get-KmcDirectoryManifest $state.liveModsRoot)', [StringComparison]::Ordinal)) 'profile recovery reads before reserving'
        foreach ($pair in @(@('runtime\New-KmcQualificationSuiteSnapshot.ps1', '[void](Assert-KmcQualificationAdmissionQuiescent'), @('runtime\New-KmcProtectedSaveContinuityAuthority.ps1', '$stateBefore = Get-KmcDirectoryManifest'), @('runtime\New-KmcChainedProtectedSaveContinuityAuthority.ps1', '$stateBefore = Get-KmcDirectoryManifest'), @('runtime\Test-KmcFixtureGuard.ps1', '$saveMetadataBefore ='), @('runtime\FixtureRecovery.ps1', '$audit=Get-KmcFixtureCandidateAudit'))) {
            $text = Get-Text $pair[0]
            $enter = $text.IndexOf('Enter-KmcNativeReservation', [StringComparison]::Ordinal); $read = $text.IndexOf($pair[1], [StringComparison]::Ordinal)
            Assert-That ($enter -gt 0 -and $read -gt $enter) "$($pair[0]) reads '$($pair[1])' before reserving"
        }
        $scoped = Get-Text 'runtime\Invoke-Chunk7ScopedPreflight.ps1'
        Assert-That ($scoped.IndexOf("Enter-KmcScopedReservation (Join-Path `$LabRoot 'runtime-state')", [StringComparison]::Ordinal) -lt $scoped.IndexOf('$bytes=BootstrapHold $pin.path', [StringComparison]::Ordinal)) 'the scoped preflight holds shared handles before reserving'
        Assert-That ($scoped.Contains("if(`$released){try{Exit-KmcScopedReservation `$true;`$reservationReleased=`$true}catch{`$errors.Add(") -and $scoped.Contains("`$receipt['ownedInputHandlesReleased']=`$released;`$receipt['nativeReservationReleased']=`$reservationReleased")) 'the scoped preflight releases without settled handles or mixes the two observations'
        Assert-That ($scoped.IndexOf('if($released){try{Exit-KmcScopedReservation', [StringComparison]::Ordinal) -lt $scoped.IndexOf('Publish-KmcScopedOutput $outputOwner $receipt', [StringComparison]::Ordinal)) 'the scoped receipt is written before the release result is known'
        Assert-That ($scoped.Contains("[Environment]::SetEnvironmentVariable('KMC_NATIVE_RESERVATION_BORROW_TOKEN',`$scopedReservationToken,'Process')`n try{`$result=Invoke-KmcScopedFreshGuards")) 'the embedded launcher guard does not borrow the closed preflight''s reservation'
        foreach ($tool in @('Recover-KmcB34S3Profile', 'Recover-KmcB75S1Profile', 'Recover-KmcP07Head7348Profile', 'Recover-FinalizeF1CreatorPreferences', 'Recover-FinalizeF1CopiedSymbols')) {
            Assert-That ((Get-Text "runtime\$tool.ps1") -match 'Enter-KmcBoundRecoveryReservation -StateRoot \$stateRoot -RunId \$\w+ .*-RequiredJournal ') "$tool takes over without binding its journals and pins"
        }
        foreach ($tool in @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'runtime') -Filter 'Recover-KmcProfileAfterCombinedTimeout*.ps1')) {
            $text = Get-Text "runtime\$($tool.Name)"
            foreach ($write in @(' [IO.File]::WriteAllBytes($paramsPath,$capturedParams)', " `$key=[Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Software\Owlcat Games\Pathfinder Kingmaker',`$true)")) {
                $at = $text.IndexOf($write, [StringComparison]::Ordinal)
                Assert-That ($at -gt 0 -and $text.LastIndexOf('[void](Assert-KmcRuntimeLockOwner $lock)', $at, [StringComparison]::Ordinal) -gt ($at - 100)) "$($tool.Name) has no ownership check immediately before: $write"
            }
        }
        $areas = Get-Text 'runtime\PersistenceProfileAreasIsolation.ps1'
        Assert-That ($areas.Contains("[void](Assert-KmcRuntimeLockOwner `$Lock)  # ownership pair immediately before the shared copy`n    [void](Copy-KmcDirectoryTreeExact -SourceRoot `$paths.copy -DestinationRoot `$paths.live)")) 'no ownership check immediately before the live Areas copy'
        $common = Get-Text 'runtime\RuntimeHarness.Common.ps1'
        foreach ($move in @('Move-Item -LiteralPath $fullLive -Destination $originalBackup', 'Move-Item -LiteralPath $ready -Destination $fullLive', 'Move-Item -LiteralPath $fullLive -Destination $stagedAfter', 'Move-Item -LiteralPath $backup -Destination $fullLive', 'Move-Item -LiteralPath $sourcePath -Destination $quarantinePath')) {
            $at = $common.IndexOf($move, [StringComparison]::Ordinal)
            $check = $common.LastIndexOf('[void](Assert-KmcRuntimeLockOwner $Lock)', $at, [StringComparison]::Ordinal)
            Assert-That ($at -gt 0 -and $check -gt 0 -and ($at - $check) -lt 140) "no ownership check immediately before: $move"
        }
        Assert-That ($common.Contains('if (-not $workingIsOriginal) { [void](Assert-KmcRuntimeLockOwner $Lock); Copy-Item -LiteralPath $backupPath -Destination $workingPath;')) 'no ownership check before the Working restore copy'
    }
}
finally {
    Stop-Contenders
    Restore-KmcTestReservationConvention $previousSelection
}

$results | ForEach-Object { Write-Host $_ }
$failed = @($results | Where-Object { $_ -like 'FAIL*' }).Count
# The owned case root is removed only after a fully successful run; failed case material stays for inspection.
if ($failed -eq 0) { Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue }
else { Write-Host "Failed case material retained: $root" }
$total = @($results | Where-Object { $_ -like 'PASS*' -or $_ -like 'FAIL*' }).Count
Write-Host "RESULT total=$total passed=$($total - $failed) failed=$failed"
if ($failed -gt 0 -or $total -eq 0) { exit 1 }
