[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][ValidateSet('Enter','Exit')][string]$Action,
    [Parameter(Mandatory = $true)][string]$StateRoot,
    [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$RunId,
    [string]$Purpose = 'preparation',
    [ValidateRange(1, 2147483647)][int]$HolderProcessId = $PID
)

# Kingmaker native-test reservation for a preparation entrypoint that does not load the runtime harness itself (the
# closed scoped preflight): Enter reserves for $StateRoot on behalf of the calling process (-HolderProcessId), so the
# record names the process that actually reads the shared trees; Exit releases exactly the reservation bound to
# -RunId in phase 'reserved'. See docs/NATIVE-RESERVATION.md.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
if ($Action -ceq 'Enter') {
    $session = Enter-KmcNativeReservation -StateRoot $StateRoot -RunId $RunId -Purpose $Purpose -HolderProcessId $HolderProcessId
    Write-Output ([string]$session.RunId)
    return
}
$fullState = [IO.Path]::GetFullPath($StateRoot).TrimEnd('\')
$binding = Read-KmcReservationBinding $fullState
if ($null -eq $binding -or [string]$binding.runId -cne $RunId -or [string]$binding.phase -cne 'reserved') {
    throw "No reserved preparation binding of run $RunId exists in $fullState; nothing is released."
}
$session = New-KmcReservationSession $fullState $RunId ([string]$binding.purpose) ([string]$binding.leaseToken) (Get-KmcReservationConvention -StateRoot $fullState)
Exit-KmcNativeReservation $session
Write-Output "released $RunId"
