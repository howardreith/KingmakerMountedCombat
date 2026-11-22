[CmdletBinding()]
param()
# Focused selection for the post-combat save and door lock incident (docs/MOUNTED-SAVE-AND-DOOR-LOCK-MISSION.md). It declares
# exactly two checks and nothing broader (docs/VALIDATION-POLICY.md 3: no silent broadening):
#   1. Test-DeferredSaveWait.ps1: the production deferred wait, its readiness result and observers, the native loading-queue
#      adapter and the command hold, spliced verbatim; sixteen lifecycle cases (menu/loading/world refusals named, queued
#      requests, door hold released, pause, cancellation, faults, timing from activation, same-owner condition changes,
#      contained logging, no retention) and eight one-line production mutations in generated copies; then the production
#      readiness evaluation against the baseline 61519031 predicate over 1,024 term combinations.
#   2. Test-PersistenceContracts.ps1: the installed Kingmaker 2.1.7b persistence contracts, including the first-offender
#      projectile/ability/command queries and their read-only describers against real native types.
# Not selected, because each already fails on base 61519031 with the identical result on this candidate (logs under
# artifacts\save-door-lock; docs/SAVE-AND-DOOR-LOCK-HANDOFF.md, section 6):
#   - chunk7\Test-Chunk7SaveBoundaryAdapter.ps1: PASS=35 FAIL=47, byte-identical log;
#   - Test-ChargeOwnershipContracts.ps1: 71 PASS, then "Parameter count mismatch" in a charge-owner reflective call;
#   - chunk7\Test-Chunk7SixPairNativeOwnership.ps1: its facade lacks MountedPairCandidate and does not compile.
# The changed pair-readiness binding is covered by the production build and the component MountedPairOwnershipTests.
# Components and Validate-Source run in the Fast tier itself. None of this is native gameplay evidence.
$ErrorActionPreference='Stop'
$failed=New-Object 'System.Collections.Generic.List[string]'
# A PowerShell 7 parent leaves its own module directories in PSModulePath; a Windows PowerShell 5.1 grandchild then
# cannot autoload its built-in Get-FileHash. Each child computes its own default module path instead.
$env:PSModulePath=$null
$children=@('Test-DeferredSaveWait.ps1','Test-PersistenceContracts.ps1')
foreach($child in $children){
    Write-Host ('SAVE-DOOR-LOCK CHILD '+$child)
    & powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot $child)
    if($LASTEXITCODE -ne 0){$failed.Add($child)}
}
if($failed.Count -ne 0){Write-Host ('SAVE-DOOR-LOCK FOCUSED FAIL: '+($failed -join ', '));exit 1}
Write-Host ('SAVE-DOOR-LOCK FOCUSED PASS ('+$children.Count+' of '+$children.Count+' children)')
