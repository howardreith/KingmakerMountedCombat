[CmdletBinding()]
param()
# Focused selection for Mounted Combat alpha 2 (docs/missions/mounted-alpha2-melee-pose/MISSION.md). It declares exactly
# these checks and nothing broader (docs/VALIDATION-POLICY.md 3: no silent broadening):
#   1. Test-MountedAlpha2NativeContract.ps1: MMR-06 installed data (the stock Mammoth's AddFacts carry TrippingBite =
#      ManeuverOnAttack(Bite, Trip); the Dog and Wolf keep it) and the compiled Horse inherited-fact filter and saved-horse
#      repair on genuine objects; the installed members the MMR-02 click admission, the MMR-04/05 readiness gate and the
#      MMR-07 telemetry bind; the native horse animation bundle identity.
#   1b. Test-MountedAlpha2Package.ps1: the receipt for the exact alpha ZIP named in docs/mounted-melee/package-identity.json
#      (archive hash/size/entries, enclosed DLL hash/MVID, version, UMM minimum, icon, manifest commit, Validate-Package).
#   2. Test-MountedPresentationContracts.ps1: the paired prediction/action-slot presentation seams beside the MMR-02
#      IgnoreClick admission.
#   3-4. Test-MountedShieldNativeContract.ps1 and Test-MountedShieldSelectionRollback.ps1: Mounted Shield regression (S08):
#      registration, amounts, AC/check writes, publication and the embedded icon identity.
#   5. Test-MountedArcheryFeatContract.ps1: the shared feat publication neighbor of the Horse registration service.
#   6. chunk7\Test-Chunk7RangerSelectionContract.ps1: the Horse/Riding Dog selection transaction of the changed service.
#   7. chunk7\Test-Chunk7ReplaySubscriberContract.ps1: the reviewed subscriber inventory and the 0.2.16-alpha.2 version.
#   8. Test-FinalizeF1LocalizationKeys.ps1: localization consumers.
#   9-13. Loader baseline: Test-UmmBuildReference.ps1, Test-Chunk7NativeLoaderContracts.ps1, Test-FrozenProductSource.ps1,
#      Test-UmmLoaderContracts.ps1 against the genuine 0.31.1 reference and the installed 0.33.0 loader.
# The MMR-01/02/03/04/05 production policies (AnimatedSaddleCalibration, PairedAttackClickAdmissionPolicy,
# UnifiedMountedStockAttackPolicy.MountJoinsIntent and DecideNext with PairedActivation state) are unit-tested by the
# component runner (tests/MountedStockAttackPolicyTests.cs), which the Fast tier itself runs with Validate-Source.
# Known baseline failures, identical on the accepted Mounted Shield build 122a9f94 and not selected here:
# Test-MountedAttackStallRepair.ps1 (Run: parameter count mismatch) and Test-WorldOwnershipContracts.ps1 (missing field
# pairSaveReady). None of this is native gameplay or visual evidence.
$ErrorActionPreference='Stop'
$failed=New-Object 'System.Collections.Generic.List[string]'
# A PowerShell 7 parent leaves its own module directories in PSModulePath; a Windows PowerShell 5.1 grandchild then
# cannot autoload its built-in Get-FileHash. Each child computes its own default module path instead.
$env:PSModulePath=$null
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$reference=Join-Path $repoRoot 'obj\references\umm-0.31.1\UnityModManager.dll'
$children=@(
    @('Test-MountedAlpha2NativeContract.ps1'),@('Test-MountedAlpha2Package.ps1'),@('Test-MountedPresentationContracts.ps1'),
    @('Test-MountedShieldNativeContract.ps1'),@('Test-MountedShieldSelectionRollback.ps1'),@('Test-MountedArcheryFeatContract.ps1'),
    @('chunk7\Test-Chunk7RangerSelectionContract.ps1'),@('chunk7\Test-Chunk7ReplaySubscriberContract.ps1'),
    @('Test-FinalizeF1LocalizationKeys.ps1'),
    @('Test-UmmBuildReference.ps1'),@('Test-Chunk7NativeLoaderContracts.ps1'),@('Test-FrozenProductSource.ps1'),
    @('Test-UmmLoaderContracts.ps1','-Reference',$reference,'-Profile','umm-0.31.1','-IgnoreLocalProfile'),
    @('Test-UmmLoaderContracts.ps1','-Profile','umm-0.33.0','-IgnoreLocalProfile'))
foreach($child in $children){
    $label=($child -join ' ')
    Write-Host ('MOUNTED-ALPHA2 CHILD '+$label)
    $arguments=@('-NoLogo','-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot $child[0]))+@($child | Select-Object -Skip 1)
    & powershell.exe @arguments
    if($LASTEXITCODE -ne 0){$failed.Add($label)}
}
if($failed.Count -ne 0){Write-Host ('MOUNTED-ALPHA2 FOCUSED FAIL: '+($failed -join ', '));exit 1}
Write-Host ('MOUNTED-ALPHA2 FOCUSED PASS ('+$children.Count+' of '+$children.Count+' children)')
