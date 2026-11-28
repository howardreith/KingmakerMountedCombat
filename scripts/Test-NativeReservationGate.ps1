[CmdletBinding()]
param()

# Review gate for the Kingmaker native-test reservation change: the reservation self-test (separate contender processes)
# and the base-game isolation suite, which opens and closes KMC runtime locks through the changed admission functions.
# Both run offline against stand-ins with a private reservation; neither touches the game, the owner's profile or the
# machine-wide reservation.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
foreach ($suite in @('Test-NativeReservation.ps1', 'Test-BaseGameIsolation.ps1')) {
    Write-Host "== $suite"
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot $suite)
    if ($LASTEXITCODE -ne 0) { throw "$suite failed (exit=$LASTEXITCODE)." }
}
Write-Host 'Native reservation gate passed.'
