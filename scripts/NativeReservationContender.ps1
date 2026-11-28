[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$StateRoot,
    [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,60}$')][string]$Name,
    [Parameter(Mandatory = $true)][string]$Signals,
    [ValidateSet('Open','HandoffClose','HandoffResume')][string]$Mode = 'Open',
    [string]$RunId,
    # When set, the contender signals <Name>.ready once its harness is loaded and attempts admission only when this
    # file appears (a common start for simultaneous arrivals).
    [string]$StartBarrier
)

# A disposable contender for scripts/Test-NativeReservation.ps1: a separate process that runs KMC's actual admission and
# release against a stand-in runtime-state root. The self-test reservation is inherited from the parent through
# KMC_NATIVE_RESERVATION_TEST_ROOT (Use-KmcTestReservationConvention). Modes:
#   Open           Open-KmcRuntimeLock, then prepare/run/restore mutation sections (each left on <Name>.<phase>.go),
#                  then Close-KmcRuntimeLock.
#   HandoffClose   Open-KmcRuntimeLock, then Close-KmcRuntimeLock -KeepReservationFor profile-recovery (the
#                  transaction recovery's handoff), then exit with the reservation kept.
#   HandoffResume  Resume-KmcNativeReservation -Mode Handoff for -RunId, Open-KmcRuntimeLock with it, a 'restore'
#                  section, then Close-KmcRuntimeLock (the follow-up profile recovery).
# A refusal is written as <Name>.refused with its message.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime\RuntimeHarness.Common.ps1')
if ([string]::IsNullOrEmpty([Environment]::GetEnvironmentVariable('KMC_NATIVE_RESERVATION_TEST_ROOT', 'Process'))) { throw 'The contender requires the self-test reservation.' }
if ([string]::IsNullOrWhiteSpace($RunId)) { $RunId = "contender-$Name" }

function Write-Signal([string]$What, [string]$Text = '') {
    [IO.File]::WriteAllText((Join-Path $Signals "$Name.$What"), $Text)
}

function Wait-Go([string]$Phase) {
    $go = Join-Path $Signals "$Name.$Phase.go"
    $until = [DateTime]::UtcNow.AddSeconds(120)
    while (-not (Test-Path -LiteralPath $go)) {
        if ([DateTime]::UtcNow -gt $until) { throw "no signal to leave $Phase" }
        Start-Sleep -Milliseconds 20
    }
}

if (-not [string]::IsNullOrWhiteSpace($StartBarrier)) {
    Write-Signal 'ready'
    $until = [DateTime]::UtcNow.AddSeconds(120)
    while (-not (Test-Path -LiteralPath $StartBarrier)) {
        if ([DateTime]::UtcNow -gt $until) { throw 'no common start' }
        Start-Sleep -Milliseconds 5
    }
}

$mutations = Join-Path $StateRoot 'mutations'
try {
    if ($Mode -ceq 'HandoffResume') {
        $session = Resume-KmcNativeReservation -StateRoot $StateRoot -RunId $RunId -Mode Handoff -Purpose 'profile-recovery'
        $lock = Open-KmcRuntimeLock -StateRoot $StateRoot -RunId $RunId -Purpose 'profile-recovery' -Reservation $session
    }
    else { $lock = Open-KmcRuntimeLock -StateRoot $StateRoot -RunId $RunId }
}
catch {
    Write-Signal 'refused' $_.Exception.Message
    exit 3
}
[void][IO.Directory]::CreateDirectory($mutations)
if ($Mode -ceq 'HandoffClose') {
    Close-KmcRuntimeLock $lock -KeepReservationFor 'profile-recovery'
    Write-Signal 'handed-off'
    exit 0
}
$phases = if ($Mode -ceq 'HandoffResume') { @('restore') } else { @('prepare', 'run', 'restore') }
try {
    foreach ($phase in $phases) {
        [IO.File]::WriteAllText((Join-Path $mutations "$Name.$phase"), 'mutation section entered')
        Write-Signal $phase
        Wait-Go $phase
    }
    Get-ChildItem -LiteralPath $mutations -Filter "$Name.*" | Remove-Item -Force
}
finally { Close-KmcRuntimeLock $lock }
Write-Signal 'released'
exit 0
