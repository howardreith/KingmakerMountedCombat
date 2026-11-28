[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$StateRoot,
    [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,60}$')][string]$Name,
    [Parameter(Mandatory = $true)][string]$Signals,
    # When set, the contender signals <Name>.ready once its harness is loaded and attempts admission only when this
    # file appears (a common start for simultaneous arrivals).
    [string]$StartBarrier
)

# A disposable contender for scripts/Test-NativeReservation.ps1: a separate process that runs KMC's actual admission
# (Open-KmcRuntimeLock) against a stand-in runtime-state root, passes through prepare, run and restore mutation sections
# (each left on <Name>.<phase>.go) and releases with Close-KmcRuntimeLock. The self-test reservation is inherited from
# the parent through KMC_NATIVE_RESERVATION_TEST_ROOT. A refusal is written as <Name>.refused with its message.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime\RuntimeHarness.Common.ps1')
if ([string]::IsNullOrEmpty([Environment]::GetEnvironmentVariable('KMC_NATIVE_RESERVATION_TEST_ROOT', 'Process'))) { throw 'The contender requires the self-test reservation.' }

function Write-Signal([string]$What, [string]$Text = '') {
    [IO.File]::WriteAllText((Join-Path $Signals "$Name.$What"), $Text)
}

if (-not [string]::IsNullOrWhiteSpace($StartBarrier)) {
    Write-Signal 'ready'
    $until = [DateTime]::UtcNow.AddSeconds(120)
    while (-not (Test-Path -LiteralPath $StartBarrier)) {
        if ([DateTime]::UtcNow -gt $until) { throw 'no common start' }
        Start-Sleep -Milliseconds 5
    }
}

try { $lock = Open-KmcRuntimeLock -StateRoot $StateRoot -RunId "contender-$Name" }
catch {
    Write-Signal 'refused' $_.Exception.Message
    exit 3
}
$mutations = Join-Path $StateRoot 'mutations'
[void][IO.Directory]::CreateDirectory($mutations)
try {
    foreach ($phase in @('prepare', 'run', 'restore')) {
        [IO.File]::WriteAllText((Join-Path $mutations "$Name.$phase"), 'mutation section entered')
        Write-Signal $phase
        $go = Join-Path $Signals "$Name.$phase.go"
        $until = [DateTime]::UtcNow.AddSeconds(120)
        while (-not (Test-Path -LiteralPath $go)) {
            if ([DateTime]::UtcNow -gt $until) { throw "no signal to leave $phase" }
            Start-Sleep -Milliseconds 20
        }
    }
    Get-ChildItem -LiteralPath $mutations -Filter "$Name.*" | Remove-Item -Force
}
finally { Close-KmcRuntimeLock $lock }
Write-Signal 'released'
exit 0
