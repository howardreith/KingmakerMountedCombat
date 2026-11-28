[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$StateRoot,
    [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,60}$')][string]$Name,
    [Parameter(Mandatory = $true)][string]$Signals,
    [Parameter(Mandatory = $true)][string]$MutexName,
    [Parameter(Mandatory = $true)][string]$RecordPath
)

# A disposable contender for scripts/Test-NativeReservation.ps1: a separate process that runs KMC's actual admission
# (Open-KmcRuntimeLock, which takes the Kingmaker native-test reservation first) against a stand-in runtime-state root
# and a self-test reservation, then passes through prepare, run and restore mutation sections. Each section writes its
# marker on entry and is left only when the test signals it; release is Close-KmcRuntimeLock. A refusal is written as
# <Name>.refused with its message.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime\RuntimeHarness.Common.ps1')
$script:KmcReservationConvention = [pscustomobject]@{ MutexName = $MutexName; RecordPath = $RecordPath }

function Write-Signal([string]$What, [string]$Text = '') {
    [IO.File]::WriteAllText((Join-Path $Signals "$Name.$What"), $Text)
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
            Start-Sleep -Milliseconds 50
        }
    }
    Get-ChildItem -LiteralPath $mutations -Filter "$Name.*" | Remove-Item -Force
}
finally { Close-KmcRuntimeLock $lock }
Write-Signal 'released'
exit 0
