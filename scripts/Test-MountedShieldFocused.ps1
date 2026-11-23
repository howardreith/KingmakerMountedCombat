[CmdletBinding()]
param()
# Focused selection for Mounted Shield (docs/missions/mounted-shield/MISSION.md). It declares exactly these checks and
# nothing broader (docs/VALIDATION-POLICY.md 3: no silent broadening):
#   1. Test-MountedShieldNativeContract.ps1: the installed 2.1.7b identities the feat depends on (Shield Focus, Greater
#      Shield Focus, the Power Attack donor, the four stock shield armor types' base bonus) and the compiled production
#      paths on genuine engine objects: registration contract, base-bonus read, amount, mount-AC write under the engine's
#      own stacking/touch/flat-footed rules, check write under the native roll arithmetic and the strict comparison,
#      three-feat publication, and the embedded icon's identity.
#   2. Test-MountedArcheryFeatContract.ps1: the directly affected neighbor. Mounted Archery's donor contract and its
#      coordinated publication, which Mounted Shield's registration and the generalized publication set now share.
#   3. Test-MountedRangedMovementContracts.ps1: the other consumer of the shared publication (Mounted Archery's ranged
#      rule availability); its compiled seams must be unchanged.
# The production amount policy, contribution ledger, publication set and contract text are unit-tested by the component
# runner (tests/MountedShieldTests.cs and the existing Mounted Combat/Archery suites), which the Fast tier itself runs with
# Validate-Source. None of this is native gameplay evidence.
$ErrorActionPreference='Stop'
$failed=New-Object 'System.Collections.Generic.List[string]'
# A PowerShell 7 parent leaves its own module directories in PSModulePath; a Windows PowerShell 5.1 grandchild then
# cannot autoload its built-in Get-FileHash. Each child computes its own default module path instead.
$env:PSModulePath=$null
$children=@('Test-MountedShieldNativeContract.ps1','Test-MountedArcheryFeatContract.ps1','Test-MountedRangedMovementContracts.ps1')
foreach($child in $children){
    Write-Host ('MOUNTED-SHIELD CHILD '+$child)
    & powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot $child)
    if($LASTEXITCODE -ne 0){$failed.Add($child)}
}
if($failed.Count -ne 0){Write-Host ('MOUNTED-SHIELD FOCUSED FAIL: '+($failed -join ', '));exit 1}
Write-Host ('MOUNTED-SHIELD FOCUSED PASS ('+$children.Count+' of '+$children.Count+' children)')
