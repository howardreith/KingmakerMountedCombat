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

    Invoke-Case 'an interrupted acquisition or release completes only with matching evidence and the exact token' {
        param($case)
        $state = Get-State $case
        # Interrupted before its lock: a fresh lease, no lock, no journal.
        $session = Enter-KmcNativeReservation -StateRoot $state -RunId 'before-lock' -Purpose 'runtime-scenario'
        Set-LeaseField 'holderProcessId' (Get-DeadProcessId)
        Assert-Throws { Complete-KmcInterruptedReservation -StateRoot $state -RunId 'before-lock' -LeaseToken ('0' * 32) } 'completable lease' 'completion with another token'
        $combined = Get-KmcRunTransactionStatePath $state 'before-lock'
        [void][IO.Directory]::CreateDirectory((Split-Path -Parent $combined))
        [IO.File]::WriteAllText($combined, ('{"runId":"before-lock","token":"' + $session.RunToken + '","phase":"prepared"}'))
        Assert-Throws { Complete-KmcInterruptedReservation -StateRoot $state -RunId 'before-lock' -LeaseToken $session.Token } 'not terminal for this transaction' 'completion over an unresolved journal'
        # A terminal-looking journal that is unbound, or bound to another token, is not evidence either.
        [IO.File]::WriteAllText($combined, '{"phase":"restored"}')
        Assert-Throws { Complete-KmcInterruptedReservation -StateRoot $state -RunId 'before-lock' -LeaseToken $session.Token } 'not terminal for this transaction' 'completion over an unbound journal'
        [IO.File]::WriteAllText($combined, ('{"runId":"before-lock","token":"' + ('f' * 64) + '","phase":"restored"}'))
        Assert-Throws { Complete-KmcInterruptedReservation -StateRoot $state -RunId 'before-lock' -LeaseToken $session.Token } 'not terminal for this transaction' 'completion over another token''s journal'
        [IO.File]::WriteAllText($combined, ('{"runId":"before-lock","token":"' + $session.RunToken + '","phase":"restored"}'))
        # Every child journal of the run counts: an unresolved Working-save child still refuses.
        $child = Get-KmcSaveTransactionStatePath $state 'before-lock'
        [void][IO.Directory]::CreateDirectory((Split-Path -Parent $child))
        [IO.File]::WriteAllText($child, ('{"runId":"before-lock","token":"' + $session.RunToken + '","phase":"original-moved"}'))
        Assert-Throws { Complete-KmcInterruptedReservation -StateRoot $state -RunId 'before-lock' -LeaseToken $session.Token } 'not terminal for this transaction' 'completion over an unresolved child journal'
        Assert-That (Test-Path -LiteralPath $convention.RecordPath) 'a refused completion removed the lease'
        [IO.File]::WriteAllText($child, ('{"runId":"before-lock","token":"' + $session.RunToken + '","phase":"restored"}'))
        Complete-KmcInterruptedReservation -StateRoot $state -RunId 'before-lock' -LeaseToken $session.Token
        Assert-That (-not (Test-Path -LiteralPath $convention.RecordPath)) 'the interrupted acquisition was not completed'
        # Interrupted after the verified unlink: the lock is gone, the lease remains; a replaced lease is left alone.
        $lock = Open-KmcRuntimeLock -StateRoot (Get-State $case 'after') -RunId 'after-unlink'
        $token = [string]$lock.Reservation.Token
        $lock.Stream.Dispose(); Remove-Item -LiteralPath $lock.Path -Force
        Set-LeaseField 'holderProcessId' (Get-DeadProcessId)
        Set-LeaseField 'token' ([Guid]::NewGuid().ToString('N'))
        $replaced = Get-Sha $convention.RecordPath
        Assert-Throws { Complete-KmcInterruptedReservation -StateRoot (Get-State $case 'after') -RunId 'after-unlink' -LeaseToken $token } 'completable lease' 'completion over a replaced lease'
        Assert-That ((Get-Sha $convention.RecordPath) -ceq $replaced) 'the replaced lease changed'
        Complete-KmcInterruptedReservation -StateRoot (Get-State $case 'after') -RunId 'after-unlink' -LeaseToken ([string](Read-Lease).token)
        Assert-That (-not (Test-Path -LiteralPath $convention.RecordPath)) 'the interrupted release was not completed'
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
        Assert-Throws { Exit-KmcUnlockedNativeReservation $replaced } 'no longer this transaction' 'a release over a replaced lease (success path)'
        Assert-That (-not $replaced.Released) 'a failed release marked the session released'
        Assert-That ((Get-Sha $convention.RecordPath) -ceq $before) 'a failed release changed the current record'
        # On an error path the operation's own error wins: the cleanup failure is reported, not thrown.
        Exit-KmcUnlockedNativeReservation $replaced -PreserveError -WarningAction SilentlyContinue
        Assert-That ((Get-Sha $convention.RecordPath) -ceq $before) 'an error-path cleanup changed the current record'
        Remove-Item -LiteralPath $convention.RecordPath -Force
        $malformed = Enter-KmcNativeReservation -StateRoot (Get-State $case 'malformed') -RunId 'malformed' -Purpose 'runtime-preview'
        [IO.File]::WriteAllText($convention.RecordPath, '{"protocol":"kingmaker-native-test-reservation/9"}')
        Assert-Throws { Exit-KmcUnlockedNativeReservation $malformed } 'no longer this transaction' 'a release over an unknown-protocol record'
        Assert-That ([IO.File]::ReadAllText($convention.RecordPath) -ceq '{"protocol":"kingmaker-native-test-reservation/9"}') 'an unknown-protocol record was removed'
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

    Invoke-Case 'the closed preflight removes only exactly its own strictly valid record' {
        param($case)
        # The inline closed-entry participation, extracted from the entry and bound to this case's private mutex and record.
        $tokens = $null; $errors = $null
        $ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'runtime\Invoke-Chunk7ScopedPreflight.ps1'), [ref]$tokens, [ref]$errors)
        $names = @('Invoke-KmcScopedReservationMutex', 'Enter-KmcScopedReservation', 'Exit-KmcScopedReservation')
        $text = [string]::Join("`n", @($ast.FindAll({ param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $names -ccontains $n.Name }, $false) | ForEach-Object { $_.Extent.Text }))
        Assert-That ($text.Contains("'Global\KingmakerNativeTest.Reservation.v1'")) 'the closed entry does not use the machine-wide mutex'
        $text = $text.Replace("'Global\KingmakerNativeTest.Reservation.v1'", "'" + $convention.MutexName + "'").Replace("'KMC_NATIVE_RESERVATION_TEST_ROOT'", "'KMC_SCOPED_SELFTEST_UNSET'")
        $script:scopedReservationRecord = $convention.RecordPath; $script:scopedReservationToken = $null; $script:scopedReservationRunId = $null
        . ([scriptblock]::Create($text))
        Enter-KmcScopedReservation (Get-State $case)
        $own = [IO.File]::ReadAllText($convention.RecordPath)
        foreach ($variant in @('{"lab":"KingmakerMountedCombat","token":"' + $script:scopedReservationToken + '"}',
                ($own -replace 'kingmaker-native-test-reservation/1', 'kingmaker-native-test-reservation/2'),
                ($own -replace '"preparation-scoped-preflight"', '"runtime-scenario"'))) {
            [IO.File]::WriteAllText($convention.RecordPath, $variant)
            Assert-Throws { Exit-KmcScopedReservation } 'no longer exactly its own' "the closed release over: $variant"
            Assert-That ([IO.File]::ReadAllText($convention.RecordPath) -ceq $variant) "the closed release removed: $variant"
        }
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

    Invoke-Case 'entrypoints keep the reservation boundaries: launcher admission, cleanup and verified close; ownership before shared moves' {
        param($case)
        $launcher = Get-Text 'runtime\Invoke-KingmakerRuntimeScenario.ps1'
        $enter = $launcher.IndexOf('Enter-KmcEntrypointReservation -StateRoot $runtimeState', [StringComparison]::Ordinal)
        Assert-That ($enter -gt $launcher.IndexOf('Request-KmcWorkflowAdmission -RepoRoot', [StringComparison]::Ordinal)) 'the launcher reserves before workflow admission'
        foreach ($read in @('Get-KmcBaseGameDependencies -GameRoot', '$beforeMods=', '$beforeSaves=')) { Assert-That ($enter -lt $launcher.IndexOf($read, [StringComparison]::Ordinal)) "the launcher reads $read before reserving" }
        Assert-That ($launcher.Contains('trap { try{Exit-KmcUnlockedNativeReservation $nativeReservation}catch{}')) 'the launcher has no exception cleanup boundary'
        Assert-That (([regex]::Matches($launcher, 'Exit-KmcUnlockedNativeReservation \$nativeReservation  # after the last shared read')).Count -eq 2) 'the preview and declined returns do not release after their last shared read'
        Assert-That ($launcher.Contains("-Purpose `$(if(`$requestedWhatIf){'runtime-preview'}else{'runtime-scenario'})")) 'the launcher preview does not reserve'
        Assert-That ($launcher.Contains('if($modsRestored-and$saveProtection-and$processExited-and(-not$BaseGameOnly-or$baseGameDependenciesUnchanged)){Close-KmcRuntimeLock $lock}else{Abandon-KmcRuntimeLock $lock}')) 'the launcher can release after a failed installed-dependency postcheck'
        Assert-That ($launcher.Contains("[void](Assert-KmcRuntimeLockOwner `$lock)`n    Invoke-KmcFinalizeF1GameStart")) 'no ownership check immediately before launch'
        $recovery = Get-Text 'runtime\Recover-KingmakerRuntimeTransaction.ps1'
        Assert-That ($recovery.IndexOf('Enter-KmcStaleTransactionReservation -StateRoot $stateRoot -ExpectedRunId', [StringComparison]::Ordinal) -lt $recovery.IndexOf('$beforeMods=Get-KmcDirectoryManifest', [StringComparison]::Ordinal)) 'transaction recovery reads before reserving'
        Assert-That ($recovery.IndexOf("if([string]`$journal.runId-cne[string]`$raw.runId-or`$journalToken-cne[string]`$raw.token)", [StringComparison]::Ordinal) -lt $recovery.IndexOf('Enter-KmcStaleTransactionReservation', [StringComparison]::Ordinal)) 'transaction recovery changes ownership before binding its journal'
        Assert-That ($recovery.Contains("if(`$profilePending){Close-KmcRuntimeLock `$lock -KeepFor 'profile-recovery'}else{Close-KmcRuntimeLock `$lock}")) 'transaction recovery releases before the owed profile recovery'
        Assert-That ($recovery.Contains('Assert-KmcBaseGameDependencyReceipt $modsState.baseGameDependencies -VerifyFiles')) 'transaction recovery does not verify the installed dependencies'
        $profile = Get-Text 'runtime\Recover-KmcPersistenceProfile.ps1'
        Assert-That ($profile.IndexOf('Enter-KmcFollowUpReservation -StateRoot $stateRoot -RunId $RunId -RunToken $runToken', [StringComparison]::Ordinal) -lt $profile.IndexOf('(Get-KmcDirectoryManifest $state.liveModsRoot)', [StringComparison]::Ordinal)) 'profile recovery reads before reserving'
        foreach ($pair in @(@('runtime\New-KmcQualificationSuiteSnapshot.ps1', '[void](Assert-KmcQualificationAdmissionQuiescent'), @('runtime\New-KmcProtectedSaveContinuityAuthority.ps1', '$stateBefore = Get-KmcDirectoryManifest'), @('runtime\New-KmcChainedProtectedSaveContinuityAuthority.ps1', '$stateBefore = Get-KmcDirectoryManifest'), @('runtime\Test-KmcFixtureGuard.ps1', '$saveMetadataBefore ='), @('runtime\FixtureRecovery.ps1', '$audit=Get-KmcFixtureCandidateAudit'))) {
            $text = Get-Text $pair[0]
            $enter = $text.IndexOf('Enter-KmcNativeReservation', [StringComparison]::Ordinal); $read = $text.IndexOf($pair[1], [StringComparison]::Ordinal)
            Assert-That ($enter -gt 0 -and $read -gt $enter) "$($pair[0]) reads '$($pair[1])' before reserving"
        }
        $scoped = Get-Text 'runtime\Invoke-Chunk7ScopedPreflight.ps1'
        Assert-That ($scoped.IndexOf("Enter-KmcScopedReservation (Join-Path `$LabRoot 'runtime-state')", [StringComparison]::Ordinal) -lt $scoped.IndexOf('$bytes=BootstrapHold $pin.path', [StringComparison]::Ordinal)) 'the scoped preflight holds shared handles before reserving'
        Assert-That ($scoped.Contains("if(`$released){try{Exit-KmcScopedReservation;`$reservationReleased=`$true}")) 'the scoped preflight releases without settled handles'
        Assert-That ($scoped.IndexOf('if($released){try{Exit-KmcScopedReservation', [StringComparison]::Ordinal) -lt $scoped.IndexOf('Publish-KmcScopedOutput $outputOwner $receipt', [StringComparison]::Ordinal)) 'the scoped receipt is written before the release result is known'
        Assert-That ($scoped.Contains("[Environment]::SetEnvironmentVariable('KMC_NATIVE_RESERVATION_BORROW_TOKEN',`$scopedReservationToken,'Process')`n try{`$result=Invoke-KmcScopedFreshGuards")) 'the embedded launcher guard does not borrow the closed preflight''s reservation'
        foreach ($tool in @('Recover-KmcB34S3Profile', 'Recover-KmcB75S1Profile', 'Recover-KmcP07Head7348Profile', 'Recover-FinalizeF1CreatorPreferences', 'Recover-FinalizeF1CopiedSymbols')) {
            Assert-That ((Get-Text "runtime\$tool.ps1") -match 'Enter-KmcStaleTransactionReservation -StateRoot \$stateRoot -ExpectedRunId \$\w+ -ExpectedLockToken ') "$tool takes over without binding its pinned token"
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
