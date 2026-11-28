[CmdletBinding()]
param()
# Focused selection for PR-2 MMR-02 (turn-based approach and movement accounting), stacked on PR-1 (child of the Mounted Melee Repair decomposition; see the mission under
# docs/missions/mmr-split/). It declares exactly these checks and nothing broader (docs/VALIDATION-POLICY.md 3):
#   1. Test-MmrTbApproachNativeContract.ps1: the installed TurnController.IgnoreClick members the click admission binds and the
#      product's IgnoreClick patches.
#   2. Test-MmrAttackOrdersNativeContract.ps1 (PR-1's area, inherited): CanActInCombat and the native brain target choice.
#   3. Test-MountedAlpha2LifecycleContract.ps1: PR-1's seams plus section 4 (the turn-based click probe, policy, grant and the
#      route-bound carrier wiring).
#   4. Test-MountedAlpha2AttackPathHarness.ps1: PR-1's pre-start/perception cases plus the route cases on the ACTUAL carrier,
#      walker, probe and revalidator members (corner, shifted start, narrow sight windows, grant boundaries, flank completion,
#      turning route).
#   5. Test-MountedPresentationContracts.ps1: the paired prediction/action-slot presentation seams beside the IgnoreClick admission.
#   6. chunk7\Test-Chunk7CurrentWriterReaders.ps1 (R13-F2, round 16): the persistence evidence readers that consume this candidate's
#      schema-4 output, through the real dispatcher and the runner's parent verifier, over graphs written by the candidate's own
#      archive codec (compiled from src/): current-format migration child and parent, historical schema-1/2 migrations to current
#      output, ordinary and inherited save/cold, previous-writer (schema 3) controls and malformed/mismatched-version refusals.
#   Regression neighbors (preserved behavior): Test-MountedShieldNativeContract.ps1 and Test-MountedShieldSelectionRollback.ps1
#   (Mounted Shield registration, amounts, AC/check writes, publication, embedded icon identity); Test-MountedArcheryFeatContract.ps1;
#   chunk7\Test-Chunk7RangerSelectionContract.ps1 (Horse/Riding Dog selection); chunk7\Test-Chunk7ReplaySubscriberContract.ps1
#   (subscriber inventory and version); Test-FinalizeF1LocalizationKeys.ps1; loader baseline Test-UmmBuildReference.ps1,
#   Test-Chunk7NativeLoaderContracts.ps1, Test-FrozenProductSource.ps1, Test-UmmLoaderContracts.ps1 (genuine 0.31.1 reference,
#   installed 0.33.0 loader).
# Known baseline failures on main, not selected: Test-MountedAttackStallRepair.ps1 and Test-WorldOwnershipContracts.ps1.
# The Fast tier runs the component tests and Validate-Source itself. None of this is native gameplay or visual evidence.
$ErrorActionPreference='Stop'
$failed=New-Object 'System.Collections.Generic.List[string]'
# A PowerShell 7 parent leaves its own module directories in PSModulePath; a Windows PowerShell 5.1 grandchild then
# cannot autoload its built-in Get-FileHash. Each child computes its own default module path instead.
$env:PSModulePath=$null
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$reference=Join-Path $repoRoot 'obj\references\umm-0.31.1\UnityModManager.dll'
$children=@(
    @('Test-MmrTbApproachNativeContract.ps1'),@('Test-MmrAttackOrdersNativeContract.ps1'),@('Test-MountedAlpha2LifecycleContract.ps1'),@('Test-MountedAlpha2AttackPathHarness.ps1'),@('Test-MountedPresentationContracts.ps1'),@('chunk7\Test-Chunk7CurrentWriterReaders.ps1'),
    @('Test-MountedShieldNativeContract.ps1'),@('Test-MountedShieldSelectionRollback.ps1'),@('Test-MountedArcheryFeatContract.ps1'),
    @('chunk7\Test-Chunk7RangerSelectionContract.ps1'),@('chunk7\Test-Chunk7ReplaySubscriberContract.ps1'),
    @('Test-FinalizeF1LocalizationKeys.ps1'),
    @('Test-UmmBuildReference.ps1'),@('Test-Chunk7NativeLoaderContracts.ps1'),@('Test-FrozenProductSource.ps1'),
    @('Test-UmmLoaderContracts.ps1','-Reference',$reference,'-Profile','umm-0.31.1','-IgnoreLocalProfile'),
    @('Test-UmmLoaderContracts.ps1','-Profile','umm-0.33.0','-IgnoreLocalProfile'))
foreach($child in $children){
    $label=($child -join ' ')
    Write-Host ('MMR-TB-APPROACH CHILD '+$label)
    $arguments=@('-NoLogo','-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot $child[0]))+@($child | Select-Object -Skip 1)
    & powershell.exe @arguments
    if($LASTEXITCODE -ne 0){$failed.Add($label)}
}
if($failed.Count -ne 0){Write-Host ('MMR-TB-APPROACH FOCUSED FAIL: '+($failed -join ', '));exit 1}
Write-Host ('MMR-TB-APPROACH FOCUSED PASS ('+$children.Count+' of '+$children.Count+' children)')
