[CmdletBinding()]
param()
# Focused selection for PR-1 MMR-03/04/05 (attack orders and real-time melee) (child of the Mounted Melee Repair decomposition; see the mission under
# docs/missions/mmr-split/). It declares exactly these checks and nothing broader (docs/VALIDATION-POLICY.md 3):
#   1. Test-MmrAttackOrdersNativeContract.ps1: the installed CanActInCombat readiness members and the native brain target
#      choice (DecisionContext.Enemies, TargetInfo.Sort/FillWeight) the real-time engagement binds.
#   2. Test-MountedAlpha2LifecycleContract.ps1: native slot/queue acceptance and promotion, the carrier Hold write and Hold
#      input, the pre-start approach hook with the untouched native start gate, the installed Character_Brain bytes and the
#      ranking reader, perception guards, retirement and ledger release.
#   3. Test-MountedAlpha2AttackPathHarness.ps1: reviews R3-F1/R4-F2 on the ACTUAL MountedPairAttackCommand approach/start
#      members, MountedPairSingleAttack and the approach prefix (counterexamples and controls).
#   4. Test-MountedPresentationContracts.ps1: the paired prediction/action-slot presentation seams.
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
    @('Test-MmrAttackOrdersNativeContract.ps1'),@('Test-MountedAlpha2LifecycleContract.ps1'),@('Test-MountedAlpha2AttackPathHarness.ps1'),@('Test-MountedPresentationContracts.ps1'),
    @('Test-MountedShieldNativeContract.ps1'),@('Test-MountedShieldSelectionRollback.ps1'),@('Test-MountedArcheryFeatContract.ps1'),
    @('chunk7\Test-Chunk7RangerSelectionContract.ps1'),@('chunk7\Test-Chunk7ReplaySubscriberContract.ps1'),
    @('Test-FinalizeF1LocalizationKeys.ps1'),
    @('Test-UmmBuildReference.ps1'),@('Test-Chunk7NativeLoaderContracts.ps1'),@('Test-FrozenProductSource.ps1'),
    @('Test-UmmLoaderContracts.ps1','-Reference',$reference,'-Profile','umm-0.31.1','-IgnoreLocalProfile'),
    @('Test-UmmLoaderContracts.ps1','-Profile','umm-0.33.0','-IgnoreLocalProfile'))
foreach($child in $children){
    $label=($child -join ' ')
    Write-Host ('MMR-ATTACK-ORDERS CHILD '+$label)
    $arguments=@('-NoLogo','-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot $child[0]))+@($child | Select-Object -Skip 1)
    & powershell.exe @arguments
    if($LASTEXITCODE -ne 0){$failed.Add($label)}
}
if($failed.Count -ne 0){Write-Host ('MMR-ATTACK-ORDERS FOCUSED FAIL: '+($failed -join ', '));exit 1}
Write-Host ('MMR-ATTACK-ORDERS FOCUSED PASS ('+$children.Count+' of '+$children.Count+' children)')
