[CmdletBinding()]
param()
# Focused selection for PR-4 MMR-06 (Horse Trip construction and existing-save repair) (child of the Mounted Melee Repair decomposition; see the mission under
# docs/missions/mmr-split/). It declares exactly these checks and nothing broader (docs/VALIDATION-POLICY.md 3):
#   1. Test-MmrHorseTripNativeContract.ps1: MMR-06 installed data (the stock Mammoth's AddFacts carry TrippingBite =
#      ManeuverOnAttack(Bite, Trip); the Dog and Wolf keep it) and the compiled Horse inherited-fact filter, repair-candidate
#      predicate, the single native removal boundary of the confirmed in-game repair action, its intent-only UI, its lifecycle
#      invalidation and the absence of the withdrawn file-request protocol (OWNER AMENDMENT revision 1), on genuine Kingmaker objects.
#   The confirmed action itself (snapshots, intent, consent, guarded dispatch, classified outcome, stale/gate/reload/reporting cases, inert
#   legacy files) is exercised by the component runner (tests/HorseTripRepairPolicyTests.cs) through the shared HorseTripRepairUpdater.
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
    @('Test-MmrHorseTripNativeContract.ps1'),
    @('Test-MountedShieldNativeContract.ps1'),@('Test-MountedShieldSelectionRollback.ps1'),@('Test-MountedArcheryFeatContract.ps1'),
    @('chunk7\Test-Chunk7RangerSelectionContract.ps1'),@('chunk7\Test-Chunk7ReplaySubscriberContract.ps1'),
    @('Test-FinalizeF1LocalizationKeys.ps1'),
    @('Test-UmmBuildReference.ps1'),@('Test-Chunk7NativeLoaderContracts.ps1'),@('Test-FrozenProductSource.ps1'),
    @('Test-UmmLoaderContracts.ps1','-Reference',$reference,'-Profile','umm-0.31.1','-IgnoreLocalProfile'),
    @('Test-UmmLoaderContracts.ps1','-Profile','umm-0.33.0','-IgnoreLocalProfile'))
foreach($child in $children){
    $label=($child -join ' ')
    Write-Host ('MMR-HORSE-TRIP CHILD '+$label)
    $arguments=@('-NoLogo','-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot $child[0]))+@($child | Select-Object -Skip 1)
    & powershell.exe @arguments
    if($LASTEXITCODE -ne 0){$failed.Add($label)}
}
if($failed.Count -ne 0){Write-Host ('MMR-HORSE-TRIP FOCUSED FAIL: '+($failed -join ', ')+' nativeProof=false');exit 1}
Write-Host ('MMR-HORSE-TRIP FOCUSED PASS ('+$children.Count+' of '+$children.Count+' children) nativeProof=false')
