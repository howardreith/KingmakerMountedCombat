[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param()

# Reconciles an interrupted KMC Kingmaker native-test reservation that holds no runtime lock (docs/NATIVE-RESERVATION.md):
# an acquisition that never reached its lock (binding phase acquiring/reserved: nothing shared was mutated, for example a
# launcher or preparation step stopped before its transaction) or a release interrupted after the lock was verified
# closed (phase closing). Only this lab's own binding, its own lease tokens and a writer that is gone qualify; a present
# lock (use that transaction's recovery entrypoint), an abandoned or handed-off transaction, and a live, foreign,
# malformed or mismatched record all refuse with every record byte-identical. -WhatIf reports without changing anything.
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
$requestedWhatIf=[bool]$WhatIfPreference
$WhatIfPreference=$false
$stateRoot=Join-Path (Get-KmcLabRoot) 'runtime-state'
Assert-KmcNoGameProcesses
$binding=Read-KmcReservationBinding $stateRoot
if($null-eq$binding){Write-Host 'No KMC reservation binding exists; nothing to reconcile.';return}
$WhatIfPreference=$requestedWhatIf
if(-not$PSCmdlet.ShouldProcess($stateRoot,"reconcile the interrupted KMC reservation of run $($binding.runId) (phase $($binding.phase))")){
    Write-Host "Reservation reconciliation WhatIf: run $($binding.runId), phase $($binding.phase); nothing changed."
    return
}
$WhatIfPreference=$false
$result=Repair-KmcNativeReservation -StateRoot $stateRoot
Write-Host "PASS reconciled the KMC reservation of run $($result.runId) (phase $($result.phase); lease removed: $($result.leaseRemoved))."
