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
