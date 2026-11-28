[CmdletBinding()]
param()

# Self-test of the Kingmaker native-test reservation (docs/NATIVE-RESERVATION.md) as KMC implements it in
# scripts/runtime/RuntimeHarness.Common.ps1: admission, the durable binding of a lease to its KMC transaction, release,
# unresolved and handed-off transactions, recovery admission (Resume/Adopt/Repair), ownership loss, strict record
# parsing and the entrypoint ordering. Contenders are separate powershell.exe processes
# (scripts/NativeReservationContender.ps1) on distinct stand-in runtime-state roots that share only a self-test
# reservation (each case its own private mutex and record below the system temporary directory). Never touches the
# machine-wide reservation, the lab's runtime state, the game or the owner's profile. Emits PASS/FAIL lines and a RESULT
# line.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime\RuntimeHarness.Common.ps1')
Clear-KmcTestReservationConvention
$productionConvention = Get-KmcReservationConvention

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
    $script:convention = Use-KmcTestReservationConvention (Join-Path $case 'reservation-root')
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
function Get-BindingPath([string]$State) { return Join-Path $State 'native-reservation.json' }

# A lease record exactly as written, for stand-ins of other writers and of malformed or ambiguous records.
function Set-RecordValue($Value) {
    [void][IO.Directory]::CreateDirectory((Split-Path -Parent $convention.RecordPath))
    [IO.File]::WriteAllText($convention.RecordPath, ($Value | ConvertTo-Json -Depth 4))
}

function New-RecordValue([string]$Holder, [int]$HolderProcessId, [string]$HolderStartUtc, [string]$RunId = 'elsewhere') {
    return [ordered]@{ protocol = 'kingmaker-native-test-reservation/1'; lab = $Holder; runId = $RunId; purpose = 'stand-in'
        token = [Guid]::NewGuid().ToString('N'); holderProcessId = $HolderProcessId; holderStartUtc = $HolderStartUtc
        host = $env:COMPUTERNAME; createdUtc = [DateTime]::UtcNow.ToString('o') }
}

function Get-DeadProcessId {
    $process = Start-Process -FilePath (Join-Path $env:WINDIR 'System32\PING.EXE') -ArgumentList @('-n', '1', '127.0.0.1') -WindowStyle Hidden -PassThru
    [void]$process.WaitForExit(10000)
    return $process.Id
}

function Get-OwnStartUtc { return (Get-Process -Id $PID).StartTime.ToUniversalTime().ToString('o') }

# Rewrites the lease's holder (a stand-in for the holding process having exited, or for a malformed identity).
function Set-LeaseHolder($ProcessId, $StartUtc) {
    $record = Get-Content -Raw -LiteralPath $convention.RecordPath | ConvertFrom-Json
    $record.holderProcessId = $ProcessId
    $record.holderStartUtc = $StartUtc
    [IO.File]::WriteAllText($convention.RecordPath, ($record | ConvertTo-Json -Depth 4))
}

# A crashed transaction: its KMC lock (owner gone) and its reservation (writer gone) stay behind.
function New-AbandonedTransaction([string]$State, [string]$RunId) {
    $lock = Open-KmcRuntimeLock -StateRoot $State -RunId $RunId
    Abandon-KmcRuntimeLock $lock
    $payload = Read-KmcJson $lock.Path
    $payload.ownerProcessId = 2147483646
    Write-KmcJsonAtomic -Path $lock.Path -Value $payload
    Set-LeaseHolder (Get-DeadProcessId) '2020-01-01T00:00:00.0000000Z'
    return $lock
}

function Start-Contender([string]$State, [string]$Name, [string]$Signals, [string]$Mode = 'Open', [string]$RunId = '', [string]$Barrier = '') {
    $arguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', ('"' + (Join-Path $PSScriptRoot 'NativeReservationContender.ps1') + '"'),
        '-StateRoot', ('"' + $State + '"'), '-Name', $Name, '-Signals', ('"' + $Signals + '"'), '-Mode', $Mode)
    if ($RunId) { $arguments += @('-RunId', $RunId) }
    if ($Barrier) { $arguments += @('-StartBarrier', ('"' + $Barrier + '"')) }
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
    $outcome = Wait-Signal $Signals $Name @('refused', 'prepare', 'restore')
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
    Assert-That (-not (Test-Path -LiteralPath (Get-BindingPath $WinnerState))) "winner $Winner left its binding"
    $after = "$Loser-after"
    Start-Contender $LoserState $after $Signals
    foreach ($phase in @('prepare', 'run', 'restore')) {
        $outcome = Wait-Signal $Signals $after @($phase, 'refused')
        if ($outcome.What -eq 'refused') { throw "$after was refused after the winner released: $($outcome.Text)" }
        Send-Go $Signals $after $phase
    }
    [void](Wait-Signal $Signals $after @('released'))
}

# Text order of an entrypoint: the reservation call precedes the first shared read.
function Assert-Before([string]$Script, [string]$First, [string]$Then) {
    $text = [IO.File]::ReadAllText((Join-Path $PSScriptRoot $Script)).Replace("`r`n", "`n")
    $First = $First.Replace("`r`n", "`n"); $Then = $Then.Replace("`r`n", "`n")
    $a = $text.IndexOf($First, [StringComparison]::Ordinal); $b = $text.IndexOf($Then, [StringComparison]::Ordinal)
    Assert-That ($a -ge 0) "$Script lacks: $First"
    Assert-That ($b -ge 0) "$Script lacks: $Then"
    Assert-That ($a -lt $b) "$Script reads '$Then' before '$First'"
}

try {
    # --- convention -------------------------------------------------------------------------------------------------
    Invoke-Case 'the production convention is neutral, machine-wide and exact' {
        param($case)
        Assert-That ([string]$productionConvention.MutexName -ceq 'Global\KingmakerNativeTest.Reservation.v1') "mutex name $($productionConvention.MutexName)"
        Assert-That ([string]$productionConvention.RecordPath -eq (Join-Path $env:ProgramData 'KingmakerNativeTest\reservation.v1.json')) "record path $($productionConvention.RecordPath)"
        $lab = (Get-KmcHarnessLabRoot).TrimEnd('\') + '\'
        Assert-That (-not ([IO.Path]::GetFullPath($productionConvention.RecordPath)).StartsWith($lab, [StringComparison]::OrdinalIgnoreCase)) 'the record lives in the KMC lab'
    }

    Invoke-Case 'a test reservation is bound to a disposable root and never covers the production runtime-state' {
        param($case)
        Assert-That ($convention.RecordPath.StartsWith($case, [StringComparison]::OrdinalIgnoreCase)) 'the test record is not below the case root'
        Assert-That ($convention.MutexName -cne $productionConvention.MutexName) 'the test mutex is the production mutex'
        Assert-Throws { Get-KmcReservationConvention -StateRoot (Join-Path (Get-KmcHarnessLabRoot) 'runtime-state') } 'cannot cover this lab' 'a test reservation for the production runtime-state'
        Assert-Throws { Use-KmcTestReservationConvention 'C:\Windows\kmc-not-disposable' } 'disposable root' 'a test reservation below a non-disposable root'
        Assert-That ((Get-KmcReservationConvention).RecordPath -eq $convention.RecordPath) 'the test root changed after a refused override'
    }

    Invoke-Case 'a lab alone (no peer, no reservation folder yet) is admitted, bound and released' {
        param($case)
        Assert-That (-not (Test-Path -LiteralPath (Split-Path -Parent $convention.RecordPath))) 'stand-in reservation folder pre-exists'
        $state = Get-State $case
        $lock = Open-KmcRuntimeLock -StateRoot $state -RunId 'alone'
        try {
            $record = Read-KmcReservationRecord $convention
            $binding = Read-KmcReservationBinding $state
            Assert-That ([string]$record.lab -ceq 'KingmakerMountedCombat' -and [string]$record.runId -ceq 'alone' -and [string]$record.token -ceq [string]$lock.Reservation.Token -and [int]$record.holderProcessId -eq $PID) 'unexpected record'
            Assert-That ([string]$binding.phase -ceq 'locked' -and [string]$binding.lockToken -ceq [string]$lock.Token -and [string]$binding.leaseToken -ceq [string]$record.token) 'unexpected binding'
            [void](Assert-KmcRuntimeLockOwner $lock)
        }
        finally { Close-KmcRuntimeLock $lock }
        Assert-That (-not (Test-Path -LiteralPath $convention.RecordPath)) 'reservation left behind'
        Assert-That (-not (Test-Path -LiteralPath (Get-BindingPath $state))) 'binding left behind'
        Assert-That (-not (Test-Path -LiteralPath (Get-LockPath $state))) 'lock left behind'
    }

    Invoke-Case 'another lab''s reservation refuses entry before the KMC lock and binding' {
        param($case)
        Set-RecordValue (New-RecordValue 'KingmakerFlight' 4 '2020-01-01T00:00:00.0000000Z' 'flight-run')
        $before = Get-Sha $convention.RecordPath
        $state = Get-State $case
        Assert-Throws { Open-KmcRuntimeLock -StateRoot $state -RunId 'blocked' } 'reserved by KingmakerFlight run flight-run' 'entry under a foreign reservation'
        Assert-That (-not (Test-Path -LiteralPath (Get-LockPath $state)) -and -not (Test-Path -LiteralPath (Get-BindingPath $state))) 'the KMC lock or binding was written'
        Assert-That ((Get-Sha $convention.RecordPath) -ceq $before) 'the foreign record changed'
    }

    # --- strict records (F6) ----------------------------------------------------------------------------------------
    Invoke-Case 'malformed or indeterminate records refuse entry and recovery with lease and lock byte-identical' {
        param($case)
        $state = Get-State $case
        $stale = New-AbandonedTransaction $state 'crashed'
        $lockBytes = Get-Sha $stale.Path
        $good = Get-Content -Raw -LiteralPath $convention.RecordPath | ConvertFrom-Json
        $variants = @(
            @{ name = 'live PID, missing start time'; value = { $v = $good.PSObject.Copy(); $v.holderProcessId = $PID; $v.PSObject.Properties.Remove('holderStartUtc'); $v } },
            @{ name = 'live PID, invalid start time'; value = { $v = $good.PSObject.Copy(); $v.holderProcessId = $PID; $v.holderStartUtc = 'yesterday'; $v } },
            @{ name = 'live PID, numeric start time'; value = { $v = $good.PSObject.Copy(); $v.holderProcessId = $PID; $v.holderStartUtc = 12; $v } },
            @{ name = 'missing token'; value = { $v = $good.PSObject.Copy(); $v.PSObject.Properties.Remove('token'); $v } },
            @{ name = 'malformed token'; value = { $v = $good.PSObject.Copy(); $v.token = 'not-a-token'; $v } },
            @{ name = 'missing run'; value = { $v = $good.PSObject.Copy(); $v.PSObject.Properties.Remove('runId'); $v } },
            @{ name = 'string PID'; value = { $v = $good.PSObject.Copy(); $v.holderProcessId = '123'; $v } },
            @{ name = 'other protocol'; value = { $v = $good.PSObject.Copy(); $v.protocol = 'kingmaker-native-test-reservation/10'; $v } },
            @{ name = 'protocol case'; value = { $v = $good.PSObject.Copy(); $v.protocol = 'Kingmaker-native-test-reservation/1'; $v } }
        )
        foreach ($variant in $variants) {
            Set-RecordValue (& $variant.value)
            $leaseBytes = Get-Sha $convention.RecordPath
            $refused = $false
            try { [void](Adopt-KmcStaleRuntimeLock $state) } catch { $refused = $true }
            Assert-That $refused "recovery adopted a record with $($variant.name)"
            Assert-Throws { Open-KmcRuntimeLock -StateRoot (Get-State $case 'other') -RunId 'next' } '' "entry over a record with $($variant.name)"
            Assert-That ((Get-Sha $convention.RecordPath) -ceq $leaseBytes -and (Get-Sha $stale.Path) -ceq $lockBytes) "a refusal changed the lease or the lock ($($variant.name))"
        }
        # The valid dead holder is adopted.
        Set-RecordValue $good
        $adopted = Adopt-KmcStaleRuntimeLock $state
        Close-KmcRuntimeLock $adopted
        Assert-That (-not (Test-Path -LiteralPath $convention.RecordPath)) 'the valid dead holder''s reservation was not recovered'
    }

    Invoke-Case 'optional peer fields are tolerated' {
        param($case)
        $value = New-RecordValue 'KingmakerWakingRune' 4 '2020-01-01T00:00:00.0000000Z' 'wr-run'
        $value.extra = [ordered]@{ anything = 1 }
        $value.Remove('purpose'); $value.Remove('createdUtc')
        Set-RecordValue $value
        Assert-Throws { Open-KmcRuntimeLock -StateRoot (Get-State $case) -RunId 'blocked' } 'reserved by KingmakerWakingRune run wr-run' 'entry under a peer record with optional fields'
    }

    # --- concurrency (F10) ------------------------------------------------------------------------------------------
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
            Start-Contender $xState $x $signals 'Open' '' $barrier
            Start-Contender $yState $y $signals 'Open' '' $barrier
            [void](Wait-Signal $signals $x @('ready')); [void](Wait-Signal $signals $y @('ready'))
            Assert-That (-not (Test-Path -LiteralPath $convention.RecordPath)) "round ${round}: the reservation was taken before the common start"
            [IO.File]::WriteAllText($barrier, 'go')
            $xo = Wait-Signal $signals $x @('prepare', 'refused'); $yo = Wait-Signal $signals $y @('prepare', 'refused')
            Assert-That (@(@($xo.What, $yo.What) | Where-Object { $_ -eq 'prepare' }).Count -eq 1) "round ${round}: $($xo.What) / $($yo.What)"
            $winner = if ($xo.What -eq 'prepare') { $x } else { $y }
            $loser = if ($winner -ceq $x) { $y } else { $x }
            $loserOutcome = if ($winner -ceq $x) { $yo } else { $xo }
            $loserState = if ($winner -ceq $x) { $yState } else { $xState }
            Assert-That ($loserOutcome.Text -like "*reserved by KingmakerMountedCombat run contender-$winner*") "round ${round}: the loser was not refused by the winner: $($loserOutcome.Text)"
            Assert-NoMutation $loserState $loser
            Assert-That (-not (Test-Path -LiteralPath (Get-BindingPath $loserState))) "round ${round}: the loser wrote a binding"
            foreach ($phase in @('prepare', 'run', 'restore')) { [void](Wait-Signal $signals $winner @($phase)); Send-Go $signals $winner $phase }
            [void](Wait-Signal $signals $winner @('released'))
            $results.Add("INFO simultaneous round ${round}: ready x2, common start, winner $winner admitted, loser $loser refused by it")
        }
    }

    # --- unresolved, recovery admission and run binding (F5) --------------------------------------------------------
    Invoke-Case 'an abandoned transaction keeps its reservation, lock and binding' {
        param($case)
        $state = Get-State $case
        $lock = Open-KmcRuntimeLock -StateRoot $state -RunId 'unresolved'
        Abandon-KmcRuntimeLock $lock
        Assert-That ([string](Read-KmcReservationRecord $convention).token -ceq [string]$lock.Reservation.Token) 'the reservation was released over an unresolved transaction'
        Assert-That ([string](Read-KmcReservationBinding $state).phase -ceq 'abandoned') 'the binding is not marked abandoned'
        Assert-That (Test-Path -LiteralPath (Get-LockPath $state)) 'the lock was removed'
        Assert-Throws { Open-KmcRuntimeLock -StateRoot (Get-State $case 'other') -RunId 'next' } 'reserved by KingmakerMountedCombat run unresolved' 'a new transaction before recovery'
    }

    Invoke-Case 'recovery adopts only the reservation bound to the stale lock''s own run, then releases it on close' {
        param($case)
        $state = Get-State $case
        $stale = New-AbandonedTransaction $state 'crashed'
        $adopted = Adopt-KmcStaleRuntimeLock $state
        try {
            Assert-That ([string]$adopted.Token -ceq [string]$stale.Token) 'adopted lock token changed'
            $record = Read-KmcReservationRecord $convention
            Assert-That ([string]$record.token -ceq [string]$adopted.Reservation.Token -and [string]$record.adopted.runId -ceq 'crashed' -and [int]$record.holderProcessId -eq $PID) 'the reservation was not adopted'
            Assert-That ([string](Read-KmcReservationBinding $state).previousLeaseToken -ceq [string]$stale.Reservation.Token) 'the binding does not record the adopted lease'
        }
        finally { Close-KmcRuntimeLock $adopted }
        Assert-That (-not (Test-Path -LiteralPath $convention.RecordPath) -and -not (Test-Path -LiteralPath (Get-BindingPath $state))) 'the adopted reservation was not released'
    }

    Invoke-Case 'run A''s abandoned lease paired with run B''s stale lock refuses byte-identically; A then recovers' {
        param($case)
        $stateA = Get-State $case 'a'; $stateB = Get-State $case 'b'
        $staleA = New-AbandonedTransaction $stateA 'run-a'
        # Run B's stale lock with its own (pre-protocol) shape in another root, plus run A's binding copied beside it.
        [IO.File]::WriteAllText((Get-LockPath $stateB), (@{ schemaVersion = 1; runId = 'run-b'; token = ('b' * 64); ownerProcessId = 2147483646; createdAtUtc = [DateTime]::UtcNow.ToString('o') } | ConvertTo-Json -Compress))
        $leaseBytes = Get-Sha $convention.RecordPath; $lockB = Get-Sha (Get-LockPath $stateB)
        Assert-Throws { Adopt-KmcStaleRuntimeLock $stateB } 'reserved by KingmakerMountedCombat run run-a' 'run B adopting run A''s lease (no binding in B)'
        $bindingA = Get-Content -Raw -LiteralPath (Get-BindingPath $stateA) | ConvertFrom-Json
        $bindingA.stateRoot = $stateB
        [IO.File]::WriteAllText((Get-BindingPath $stateB), ($bindingA | ConvertTo-Json))
        Assert-Throws { Adopt-KmcStaleRuntimeLock $stateB } 'belongs to run run-a' 'run B adopting run A''s lease through a copied binding'
        # The same run id but another lock token is refused too.
        $bindingA.runId = 'run-b'; [IO.File]::WriteAllText((Get-BindingPath $stateB), ($bindingA | ConvertTo-Json))
        Assert-Throws { Adopt-KmcStaleRuntimeLock $stateB } 'names another lock' 'a binding naming another lock'
        Assert-That ((Get-Sha $convention.RecordPath) -ceq $leaseBytes -and (Get-Sha (Get-LockPath $stateB)) -ceq $lockB) 'a refusal changed the lease or run B''s lock'
        Remove-Item -LiteralPath (Get-BindingPath $stateB) -Force
        $adopted = Adopt-KmcStaleRuntimeLock $stateA
        Assert-That ([string]$adopted.RunId -ceq 'run-a') 'run A was not recovered'
        Close-KmcRuntimeLock $adopted
        Assert-That (-not (Test-Path -LiteralPath $convention.RecordPath)) 'run A''s reservation was not released after recovery'
    }

    Invoke-Case 'a protocol-1 run whose lease vanished refuses recovery and records the incident; a pre-protocol lock recovers fresh' {
        param($case)
        $state = Get-State $case
        $stale = New-AbandonedTransaction $state 'lost-lease'
        Remove-Item -LiteralPath $convention.RecordPath -Force
        $bindingBytes = Get-Sha (Get-BindingPath $state); $lockBytes = Get-Sha $stale.Path
        Assert-Throws { Adopt-KmcStaleRuntimeLock $state } 'lease of protocol-1 run lost-lease is missing' 'recovery of a protocol-1 run without its lease'
        Assert-That (-not (Test-Path -LiteralPath $convention.RecordPath)) 'a fresh lease was minted for a lost protocol-1 lease'
        Assert-That ((Get-Sha (Get-BindingPath $state)) -ceq $bindingBytes -and (Get-Sha $stale.Path) -ceq $lockBytes) 'the refusal changed the binding or lock'
        Assert-That (Test-Path -LiteralPath (Join-Path $state 'native-reservation-incidents\lost-lease.first.json')) 'no incident record'
        $legacy = Get-State $case 'legacy'
        [IO.File]::WriteAllText((Get-LockPath $legacy), (@{ schemaVersion = 1; runId = 'pre-protocol'; token = ('c' * 64); ownerProcessId = 2147483646; createdAtUtc = [DateTime]::UtcNow.ToString('o') } | ConvertTo-Json -Compress))
        Remove-Item -LiteralPath (Get-BindingPath $state) -Force
        $adopted = Adopt-KmcStaleRuntimeLock $legacy
        Assert-That ([string](Read-KmcReservationRecord $convention).adopted.legacy -ceq 'pre-protocol stale lock') 'the pre-protocol lock did not take a fresh reservation'
        Close-KmcRuntimeLock $adopted
    }

    Invoke-Case 'an interrupted adoption is retried from either lease token' {
        param($case)
        $state = Get-State $case
        $stale = New-AbandonedTransaction $state 'retry'
        # Interrupted after the binding moved to a new token but before the lease was replaced.
        $binding = Get-Content -Raw -LiteralPath (Get-BindingPath $state) | ConvertFrom-Json
        $binding.previousLeaseToken = $binding.leaseToken; $binding.leaseToken = [Guid]::NewGuid().ToString('N')
        [IO.File]::WriteAllText((Get-BindingPath $state), ($binding | ConvertTo-Json))
        $adopted = Adopt-KmcStaleRuntimeLock $state
        Close-KmcRuntimeLock $adopted
        Assert-That (-not (Test-Path -LiteralPath $convention.RecordPath)) 'the retried adoption did not complete'
    }

    Invoke-Case 'recovery never adopts another lab''s abandoned reservation or a live KMC holder''s' {
        param($case)
        $state = Get-State $case
        [void](New-AbandonedTransaction $state 'crashed')
        $lockBefore = Get-Sha (Get-LockPath $state)
        Set-RecordValue (New-RecordValue 'KingmakerWakingRune' (Get-DeadProcessId) '2020-01-01T00:00:00.0000000Z' 'crashed')
        $before = Get-Sha $convention.RecordPath
        Assert-Throws { Adopt-KmcStaleRuntimeLock $state } 'reserved by KingmakerWakingRune run crashed' 'recovery over a foreign record'
        Assert-That ((Get-Sha $convention.RecordPath) -ceq $before) 'the foreign record changed'
        Set-LeaseHolder $PID (Get-OwnStartUtc)
        $record = Get-Content -Raw -LiteralPath $convention.RecordPath | ConvertFrom-Json
        $record.lab = 'KingmakerMountedCombat'; $record.token = [string](Read-KmcReservationBinding $state).leaseToken; $record | Add-Member -NotePropertyName stateRoot -NotePropertyValue $state -Force
        [IO.File]::WriteAllText($convention.RecordPath, ($record | ConvertTo-Json))
        Assert-Throws { Adopt-KmcStaleRuntimeLock $state } 'may still run' 'recovery over a live holder'
        Assert-That ((Get-Sha (Get-LockPath $state)) -ceq $lockBefore) 'the stale lock was changed'
    }

    # --- interrupted transitions (F7) -------------------------------------------------------------------------------
    Invoke-Case 'an acquisition interrupted before its lock is reconciled only by the matching repair' {
        param($case)
        $state = Get-State $case
        $session = Enter-KmcNativeReservation -StateRoot $state -RunId 'interrupted-entry' -Purpose 'runtime-scenario'
        Set-LeaseHolder (Get-DeadProcessId) '2020-01-01T00:00:00.0000000Z'
        Assert-Throws { Open-KmcRuntimeLock -StateRoot (Get-State $case 'other') -RunId 'next' } 'reserved by KingmakerMountedCombat run interrupted-entry' 'entry over the interrupted acquisition'
        # Another root's repair has no binding for it; a foreign record in place is left alone.
        Assert-Throws { Repair-KmcNativeReservation -StateRoot (Get-State $case 'other') } 'No KMC reservation binding' 'repair from another root'
        $lease = Get-Content -Raw -LiteralPath $convention.RecordPath
        Set-RecordValue (New-RecordValue 'KingmakerFlight' (Get-DeadProcessId) '2020-01-01T00:00:00.0000000Z' 'interrupted-entry')
        $foreign = Get-Sha $convention.RecordPath
        Assert-Throws { Repair-KmcNativeReservation -StateRoot $state } 'not by this binding' 'repair over a foreign record'
        Assert-That ((Get-Sha $convention.RecordPath) -ceq $foreign) 'the foreign record changed'
        [IO.File]::WriteAllText($convention.RecordPath, $lease)
        $repaired = Repair-KmcNativeReservation -StateRoot $state
        Assert-That ([string]$repaired.phase -ceq 'reserved' -and $repaired.leaseRemoved) 'unexpected repair result'
        Assert-That (-not (Test-Path -LiteralPath $convention.RecordPath) -and -not (Test-Path -LiteralPath (Get-BindingPath $state))) 'the repair left the reservation'
    }

    Invoke-Case 'repair refuses a live writer, a present lock and an abandoned or handed-off transaction' {
        param($case)
        $state = Get-State $case
        $session = Enter-KmcNativeReservation -StateRoot $state -RunId 'live' -Purpose 'runtime-scenario'
        Assert-Throws { Repair-KmcNativeReservation -StateRoot $state } 'may still run' 'repair over a live writer'
        Exit-KmcNativeReservation $session
        $abandoned = Get-State $case 'abandoned'
        [void](New-AbandonedTransaction $abandoned 'abandoned')
        Assert-Throws { Repair-KmcNativeReservation -StateRoot $abandoned } 'its own recovery entrypoint' 'repair with a lock present'
    }

    Invoke-Case 'a release interrupted after a verified close is retryable in reverse order' {
        param($case)
        $state = Get-State $case
        $lock = Open-KmcRuntimeLock -StateRoot $state -RunId 'release'
        # The final release fails: the mutex is held elsewhere past the bounded wait.
        $holder = Start-Process -FilePath 'powershell.exe' -ArgumentList @('-NoProfile', '-Command', ('$m=New-Object System.Threading.Mutex($false,''' + $convention.MutexName + ''');[void]$m.WaitOne();Start-Sleep -Seconds 14;$m.ReleaseMutex()')) -WindowStyle Hidden -PassThru
        $contenders.Add($holder)
        Start-Sleep -Seconds 2
        Assert-Throws { Close-KmcRuntimeLock $lock } 'mutex stayed busy' 'close while the mutex is held elsewhere'
        Assert-That (-not (Test-Path -LiteralPath (Get-LockPath $state))) 'the KMC lock was not removed first'
        Assert-That ([string](Read-KmcReservationBinding $state).phase -ceq 'closing' -and (Test-Path -LiteralPath $convention.RecordPath)) 'the failed release lost its evidence'
        [void]$holder.WaitForExit(30000)
        Exit-KmcLockReservation $lock
        Assert-That (-not (Test-Path -LiteralPath $convention.RecordPath) -and -not (Test-Path -LiteralPath (Get-BindingPath $state))) 'the retried release did not complete'
        # The same interruption after the process ended: the repair reconciles the verified close.
        $second = Open-KmcRuntimeLock -StateRoot $state -RunId 'release-2'
        Set-KmcReservationPhase -Session $second.Reservation -Phase 'closing'
        $second.Stream.Dispose(); Remove-Item -LiteralPath (Get-LockPath $state) -Force
        Set-LeaseHolder (Get-DeadProcessId) '2020-01-01T00:00:00.0000000Z'
        $repaired = Repair-KmcNativeReservation -StateRoot $state
        Assert-That ([string]$repaired.phase -ceq 'closing' -and -not (Test-Path -LiteralPath $convention.RecordPath)) 'the interrupted release was not reconciled'
    }

    # --- ownership loss (F4) ----------------------------------------------------------------------------------------
    Invoke-Case 'a lost reservation stops the next write and close; the replacement and first incident stay' {
        param($case)
        $state = Get-State $case
        $lock = Open-KmcRuntimeLock -StateRoot $state -RunId 'lost'
        $working = Join-Path $case 'Working.zks'; [IO.File]::WriteAllText($working, 'working bytes')
        Set-RecordValue (New-RecordValue 'KingmakerBuffPlanner' 4 '2020-01-01T00:00:00.0000000Z' 'kbp-run')
        $replacement = Get-Sha $convention.RecordPath
        # The existing ownership boundary before a shared write refuses.
        $wrote = $false
        try { [void](Assert-KmcRuntimeLockOwner $lock); [IO.File]::WriteAllText($working, 'changed'); $wrote = $true } catch { }
        Assert-That (-not $wrote -and [IO.File]::ReadAllText($working) -ceq 'working bytes') 'a write happened after the reservation was lost'
        $first = Join-Path $state 'native-reservation-incidents\lost.first.json'
        Assert-That (Test-Path -LiteralPath $first) 'no first incident'
        $firstBytes = Get-Sha $first
        Assert-Throws { Close-KmcRuntimeLock $lock } 'is lost' 'close of a lost reservation'
        Assert-That ((Get-Sha $convention.RecordPath) -ceq $replacement) 'the replacement record changed'
        Assert-That (Test-Path -LiteralPath (Get-LockPath $state)) 'the KMC lock was removed after the loss'
        Assert-That ((Get-Sha $first) -ceq $firstBytes) 'the first incident was replaced'
        Assert-That (@(Get-ChildItem -LiteralPath (Join-Path $state 'native-reservation-incidents') -Filter 'lost.*.json').Count -ge 2) 'the close disposition was not recorded separately'
        Assert-Throws { Adopt-KmcStaleRuntimeLock $state } '' 'recovery over the replacement'
        Assert-That ((Get-Sha $convention.RecordPath) -ceq $replacement) 'recovery changed the replacement record'
    }

    Invoke-Case 'a stale KMC lock refuses a new transaction and releases the reservation it took' {
        param($case)
        $state = Get-State $case
        [IO.File]::WriteAllText((Get-LockPath $state), 'stale')
        Assert-Throws { Open-KmcRuntimeLock -StateRoot $state -RunId 'blocked' } 'runtime lock already exists' 'entry over a stale lock'
        Assert-That (-not (Test-Path -LiteralPath $convention.RecordPath) -and -not (Test-Path -LiteralPath (Get-BindingPath $state))) 'the refused entry kept the reservation'
    }

    # --- handoff (F3) -----------------------------------------------------------------------------------------------
    Invoke-Case 'a recovery handoff keeps peers out until the follow-up recovery of the same run closes' {
        param($case)
        $signals = Get-State $case 'signals'
        $state = Get-State $case 'kmc'; $peer = Get-State $case 'peer'
        Start-Contender $state 'R' $signals 'HandoffClose' 'handoff-run'
        [void](Wait-Signal $signals 'R' @('handed-off'))
        [void]($contenders[0].WaitForExit(30000))
        Assert-That ([string](Read-KmcReservationBinding $state).phase -ceq 'handoff' -and -not (Test-Path -LiteralPath (Get-LockPath $state))) 'the handoff did not close the lock and keep the reservation'
        Assert-LoserRefused $peer 'peer-after-handoff' $signals 'handoff-run'
        Assert-Throws { Repair-KmcNativeReservation -StateRoot $state } 'phase handoff' 'repair of a handed-off reservation'
        Assert-Throws { Resume-KmcNativeReservation -StateRoot $state -RunId 'other-run' -Mode Handoff -Purpose 'profile-recovery' } 'belongs to run handoff-run' 'another run resuming the handoff'
        Start-Contender $state 'P' $signals 'HandoffResume' 'handoff-run'
        $outcome = Wait-Signal $signals 'P' @('restore', 'refused')
        Assert-That ($outcome.What -eq 'restore') "the follow-up recovery was refused: $($outcome.Text)"
        Assert-LoserRefused $peer 'peer-during-profile' $signals 'handoff-run'
        Send-Go $signals 'P' 'restore'
        [void](Wait-Signal $signals 'P' @('released'))
        Assert-That (-not (Test-Path -LiteralPath $convention.RecordPath)) 'the follow-up recovery did not release'
    }

    # --- preparation wrapper (F2) -----------------------------------------------------------------------------------
    Invoke-Case 'a preparation step cannot run under a peer reservation and releases its own afterwards' {
        param($case)
        $state = Get-State $case
        $protected = Join-Path $case 'protected.zks'; [IO.File]::WriteAllText($protected, 'protected')
        Set-RecordValue (New-RecordValue 'KingmakerFlight' 4 '2020-01-01T00:00:00.0000000Z' 'flight-run')
        $opened = $false
        Assert-Throws { Invoke-KmcUnderPreparationReservation -StateRoot $state -Purpose 'preparation-test' -Body { $script:opened = $true; [IO.File]::OpenRead($protected).Dispose() } } 'reserved by KingmakerFlight' 'preparation under a peer reservation'
        Assert-That (-not $opened) 'the preparation body ran under a peer reservation'
        Remove-Item -LiteralPath $convention.RecordPath -Force
        $held = Invoke-KmcUnderPreparationReservation -StateRoot $state -Purpose 'preparation-test' -Body { $handle = [IO.File]::Open($protected, 'Open', 'Read', 'Read'); try { (Read-KmcReservationRecord $convention).purpose } finally { $handle.Dispose() } }
        Assert-That ([string]$held -ceq 'preparation-test') 'the preparation body did not run under its own reservation'
        Assert-That (-not (Test-Path -LiteralPath $convention.RecordPath) -and -not (Test-Path -LiteralPath (Get-BindingPath $state))) 'the preparation kept its reservation'
        $exclusive = [IO.File]::Open($protected, 'Open', 'ReadWrite', 'None'); $exclusive.Dispose()
    }

    Invoke-Case 'the preparation helper reserves on behalf of its caller and releases exactly its run' {
        param($case)
        $state = Get-State $case
        $helper = Join-Path $PSScriptRoot 'runtime\Invoke-KmcPreparationReservation.ps1'
        $out = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $helper -Action Enter -StateRoot $state -RunId 'helper-run' -Purpose 'preparation-helper' -HolderProcessId $PID
        Assert-That ($LASTEXITCODE -eq 0) "helper enter failed: $out"
        Assert-That ([int](Read-KmcReservationRecord $convention).holderProcessId -eq $PID) 'the helper did not reserve on behalf of its caller'
        $ErrorActionPreference = 'Continue'
        try { $null = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $helper -Action Exit -StateRoot $state -RunId 'other-run' 2>$null; $otherExit = $LASTEXITCODE }
        finally { $ErrorActionPreference = 'Stop' }
        Assert-That ($otherExit -ne 0 -and (Test-Path -LiteralPath $convention.RecordPath)) 'the helper released another run'
        $out = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $helper -Action Exit -StateRoot $state -RunId 'helper-run'
        Assert-That ($LASTEXITCODE -eq 0 -and -not (Test-Path -LiteralPath $convention.RecordPath)) "helper exit failed: $out"
    }

    # --- entrypoint ordering (F1, F2, F3) ---------------------------------------------------------------------------
    Invoke-Case 'every participating entrypoint reserves before its first shared read' {
        param($case)
        Assert-Before 'runtime\Invoke-KingmakerRuntimeScenario.ps1' "Request-KmcWorkflowAdmission -RepoRoot" 'Enter-KmcNativeReservation -StateRoot $runtimeState'
        Assert-Before 'runtime\Invoke-KingmakerRuntimeScenario.ps1' 'Enter-KmcNativeReservation -StateRoot $runtimeState' 'Get-KmcBaseGameDependencies -GameRoot'
        Assert-Before 'runtime\Invoke-KingmakerRuntimeScenario.ps1' 'Enter-KmcNativeReservation -StateRoot $runtimeState' '$beforeMods='
        Assert-Before 'runtime\Invoke-KingmakerRuntimeScenario.ps1' 'Enter-KmcNativeReservation -StateRoot $runtimeState' '$beforeSaves='
        Assert-Before 'runtime\Invoke-KingmakerRuntimeScenario.ps1' '[void](Assert-KmcRuntimeLockOwner $lock)
    Invoke-KmcFinalizeF1GameStart' 'Start-Process -FilePath $SteamPath'
        Assert-Before 'runtime\Recover-KingmakerRuntimeTransaction.ps1' 'Resume-KmcNativeReservation -StateRoot $stateRoot' '$beforeMods=Get-KmcDirectoryManifest'
        Assert-Before 'runtime\Recover-KingmakerRuntimeTransaction.ps1' 'Resume-KmcNativeReservation -StateRoot $stateRoot' '$beforeSaves=Get-KmcSaveMetadataInventory'
        Assert-Before 'runtime\Recover-KmcPersistenceProfile.ps1' '$nativeReservation=Resume-KmcNativeReservation' 'Get-KmcDirectoryManifest $state.liveModsRoot'
        Assert-Before 'runtime\Recover-KmcPersistenceProfile.ps1' '$nativeReservation=Resume-KmcNativeReservation' '$prefs=Get-KmcPersistencePlayerPrefs'
        Assert-Before 'runtime\FixtureRecovery.ps1' '$reservation=Enter-KmcNativeReservation' '$audit=Get-KmcFixtureCandidateAudit $SaveRoot'
        Assert-Before 'runtime\Test-KmcFixtureGuard.ps1' '$nativeReservation = Enter-KmcNativeReservation' '$saveMetadataBefore ='
        Assert-Before 'runtime\Test-KmcFixtureGuard.ps1' '$nativeReservation = Enter-KmcNativeReservation' '$audit = Get-KmcFixtureCandidateAudit'
        Assert-Before 'runtime\New-KmcQualificationSuiteSnapshot.ps1' '$preparationReservation=Enter-KmcNativeReservation' '[void](Assert-KmcQualificationAdmissionQuiescent'
        foreach ($tool in @('runtime\New-KmcProtectedSaveContinuityAuthority.ps1', 'runtime\New-KmcChainedProtectedSaveContinuityAuthority.ps1')) {
            Assert-Before $tool '$preparationReservation = Enter-KmcNativeReservation' '$stateBefore = Get-KmcDirectoryManifest'
            Assert-Before $tool '$preparationReservation = Enter-KmcNativeReservation' 'Get-KmcSaveMetadataInventory'
        }
        Assert-Before 'runtime\Invoke-Chunk7ScopedPreflight.ps1' '-Action Enter -StateRoot' "`ntry{`n"
        $closure = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'runtime\Invoke-Chunk7ScopedPreflight.ps1')).Replace("`r`n", "`n")
        Assert-That ($closure.IndexOf('-Action Exit -StateRoot', [StringComparison]::Ordinal) -gt $closure.LastIndexOf('}finally{', [StringComparison]::Ordinal)) 'the scoped preflight releases before its handles are closed'
        # Release follows every shared-surface verification, independent of the gameplay verdict.
        $launcher = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'runtime\Invoke-KingmakerRuntimeScenario.ps1'))
        Assert-That ($launcher.Contains('if($modsRestored-and$saveProtection-and$processExited-and(-not$BaseGameOnly-or$baseGameDependenciesUnchanged)){Close-KmcRuntimeLock $lock}else{Abandon-KmcRuntimeLock $lock}')) 'the launcher can release after a failed installed-dependency postcheck'
        $recovery = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'runtime\Recover-KingmakerRuntimeTransaction.ps1'))
        Assert-That ($recovery.Contains("if(`$profilePending){Close-KmcRuntimeLock `$lock -KeepReservationFor 'profile-recovery'}else{Close-KmcRuntimeLock `$lock}")) 'the transaction recovery releases before the profile recovery'
        # WhatIf never acquires: each acquisition is guarded by the WhatIf/dry-run flag.
        Assert-That ($launcher.Contains("if(-not`$requestedWhatIf){`r`n    `$actualRunId=") -or $launcher.Contains("if(-not`$requestedWhatIf){`n    `$actualRunId=")) 'the launcher acquires under WhatIf'
    }
}
finally {
    Stop-Contenders
    Clear-KmcTestReservationConvention
    if (Test-Path -LiteralPath $root) { Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue }
}

$results | ForEach-Object { Write-Host $_ }
$failed = @($results | Where-Object { $_ -like 'FAIL*' }).Count
$total = @($results | Where-Object { $_ -like 'PASS*' -or $_ -like 'FAIL*' }).Count
Write-Host "RESULT total=$total passed=$($total - $failed) failed=$failed"
if ($failed -gt 0 -or $total -eq 0) { exit 1 }
