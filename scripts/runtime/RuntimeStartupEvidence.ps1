# Passive startup evidence only. This helper supplies no load, action or completion authority.
function Assert-KmcRuntimeStartupTrace {
    param([Parameter(Mandatory=$true)]$Trace)
    if ($null -eq $Trace -or $Trace.GetType().FullName -cne 'System.Management.Automation.PSCustomObject') { throw 'Runtime startup trace must be one object.' }
    Assert-KmcExactProperties $Trace @('schemaVersion','kind','clockOrigin','callbackCapacity','sampleCapacity','clockFrequency','callbacksObserved','closedCallbacks','orderingFailure','readFailure','samples') 'runtime startup trace'
    foreach ($name in @('schemaVersion','callbackCapacity','sampleCapacity','clockFrequency','callbacksObserved','closedCallbacks')) {
        if (($Trace.$name -isnot [int]) -and ($Trace.$name -isnot [long])) { throw "Runtime startup trace $name must be an integer." }
    }
    if ($Trace.schemaVersion -ne 1 -or $Trace.callbackCapacity -ne 10 -or $Trace.sampleCapacity -ne 50 -or
        $Trace.clockFrequency -le 0 -or $Trace.callbacksObserved -lt 0 -or $Trace.callbacksObserved -gt 10 -or
        $Trace.closedCallbacks -lt 0 -or $Trace.closedCallbacks -gt $Trace.callbacksObserved -or
        $Trace.kind -isnot [string] -or $Trace.kind -cne 'runtime-startup-callback-trace' -or
        $Trace.clockOrigin -isnot [string] -or $Trace.clockOrigin -cne 'accepted-runtime-host' -or
        $Trace.orderingFailure -isnot [bool] -or $Trace.readFailure -isnot [bool] -or
        $Trace.samples -isnot [array] -or $Trace.samples.Count -gt 50) { throw 'Runtime startup trace declaration or bounds are invalid.' }
    $entries=0; $exits=0; $open=$false; $next=0; $lastTicks=[long]0; $sawNull=$false
    foreach ($sample in $Trace.samples) {
        if ($null -eq $sample -or $sample.GetType().FullName -cne 'System.Management.Automation.PSCustomObject') { throw 'Runtime startup sample must be one object.' }
        Assert-KmcExactProperties $sample @('callback','phase','elapsedTicks','unityFrame','isFocused','runInBackground') 'runtime startup sample'
        if (($sample.callback -isnot [int] -and $sample.callback -isnot [long]) -or
            $sample.callback -lt 1 -or $sample.callback -gt 10 -or $sample.phase -isnot [string]) { throw 'Runtime startup sample identity is invalid.' }
        if ($null -eq $sample.elapsedTicks) { $sawNull=$true }
        elseif (($sample.elapsedTicks -isnot [int] -and $sample.elapsedTicks -isnot [long]) -or
            $sample.elapsedTicks -lt $lastTicks) { throw 'Runtime startup clock sample is invalid or regressed.' }
        else { $lastTicks=[long]$sample.elapsedTicks }
        if ($null -eq $sample.unityFrame) { $sawNull=$true }
        elseif (($sample.unityFrame -isnot [int] -and $sample.unityFrame -isnot [long]) -or
            $sample.unityFrame -lt 0 -or $sample.unityFrame -gt [int]::MaxValue) { throw 'Runtime startup Unity frame sample is invalid.' }
        foreach ($flag in @('isFocused','runInBackground')) {
            if ($null -eq $sample.$flag) { $sawNull=$true }
            elseif ($sample.$flag -isnot [bool]) { throw "Runtime startup $flag sample must be Boolean or unavailable." }
        }
        if ($sample.phase -ceq 'on-update-entry') {
            if ($open -or $sample.callback -ne ($entries+1)) { throw 'Runtime startup callback entries are not ordered.' }
            $entries++; $open=$true; $next=1
        }
        else {
            if (-not $open -or $sample.callback -ne $entries) { throw 'Runtime startup phase has no matching callback.' }
            switch -CaseSensitive ($sample.phase) {
                'pre-host-start' { if ($next -ne 1) { throw 'Runtime startup pre-host start is out of order.' }; $next=2 }
                'pre-host-end' { if ($next -ne 2) { throw 'Runtime startup pre-host end is out of order.' }; $next=3 }
                'host-entry' { if ($next -ne 3) { throw 'Runtime startup host entry is out of order.' }; $next=4 }
                'on-update-exit' { $open=$false; $exits++; $next=0 }
                default { throw 'Runtime startup phase is unknown.' }
            }
        }
    }
    if ($entries -ne $Trace.callbacksObserved -or $exits -ne $Trace.closedCallbacks -or
        $sawNull -ne [bool]$Trace.readFailure) { throw 'Runtime startup counts or unavailable-read flag do not match raw samples.' }
}

# Passive capture of the engine's own UberLogger Warning/Error channel while a request was
# active. A tail is observational only: it never proves or refutes a native outcome by itself.
function Assert-KmcNativeEngineLogTail {
    param([Parameter(Mandatory=$true)][AllowNull()]$Tail)
    if ($null -eq $Tail -or $Tail.GetType().FullName -cne 'System.Management.Automation.PSCustomObject') { throw 'Native engine log tail must be one object.' }
    Assert-KmcExactProperties $Tail @('contract','capacity','observed','dropped','entries') 'native engine log tail'
    foreach ($name in @('capacity','observed','dropped')) {
        if (($Tail.$name -isnot [int]) -and ($Tail.$name -isnot [long])) { throw "Native engine log tail $name must be an integer." }
    }
    if ($Tail.contract -isnot [string] -or $Tail.contract -cne 'native-engine-log-tail-v1' -or $Tail.capacity -ne 64 -or
        $Tail.observed -lt 0 -or $Tail.dropped -lt 0 -or $Tail.entries -isnot [array]) { throw 'Native engine log tail declaration or bounds are invalid.' }
    $rows = @($Tail.entries)
    if ($rows.Count -gt 64 -or $rows.Count -ne [Math]::Min([long]$Tail.observed, 64) -or $Tail.dropped -ne ($Tail.observed - $rows.Count)) { throw 'Native engine log tail counts do not account for its rows.' }
    foreach ($row in $rows) {
        if ($null -eq $row -or $row.GetType().FullName -cne 'System.Management.Automation.PSCustomObject') { throw 'Native engine log tail row must be one object.' }
        Assert-KmcExactProperties $row @('severity','channel','timeStamp','message','callstack') 'native engine log tail row'
        if ($row.severity -isnot [string] -or $row.severity -cnotin @('Warning','Error')) { throw 'Native engine log tail row severity is invalid.' }
        if ($null -ne $row.channel -and $row.channel -isnot [string]) { throw 'Native engine log tail row channel is invalid.' }
        if ($null -eq $row.timeStamp -or $row.timeStamp -is [array]) { throw 'Native engine log tail row time stamp is invalid.' }
        if ($null -ne $row.message -and ($row.message -isnot [string] -or $row.message.Length -gt 2000)) { throw 'Native engine log tail row message is invalid.' }
        if ($row.callstack -isnot [array] -or @($row.callstack).Count -gt 12) { throw 'Native engine log tail row callstack is invalid.' }
        foreach ($frame in @($row.callstack)) { if ($null -ne $frame -and ($frame -isnot [string] -or $frame.Length -gt 2000)) { throw 'Native engine log tail frame is invalid.' } }
    }
}
