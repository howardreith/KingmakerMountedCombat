[CmdletBinding()]
param([ValidateSet('Debug','Release')][string]$Configuration='Release')
# Focused selection for the mounted city toolbar, city companion roster and prone-target mission
# (docs/MOUNTED-COMBAT-CITY-AND-PRONE-MISSION.md). The controller's focused gate accepts one -Focused script name, so this
# wrapper declares exactly two existing checks and nothing broader (docs/VALIDATION-POLICY.md 3: no silent broadening).
# Not selected: chunk7\Test-Chunk7SixPairArchiveAdapter.ps1 and chunk7\Test-Chunk7AreaRestorePreflightAdapter.ps1, whose
# typed facades no longer compile against the current product (missing members added by later features, e.g. F2's
# MountedControlSlotPolicy in MountedArchiveData, retained-intent and area-control types); a pre-existing failure.
#   1. Test-AssemblyContracts.ps1 -Target Kingmaker: the installed Kingmaker 2.1.7b contracts, including the rows this
#      mission relies on (UIUtility.GetGroup token and pet source, GroupController.SetGroup, UnitUISettings bad-slot
#      replacement). Wrath is not selected.
#   2. Test-GunslingerClickComposition.ps1: the 0.2.13 Tabletop Rules Expansion click-composition regression in both
#      patch orders, because the mounted stock-attack routing (MountedCombatController.TryRouteMountedStockAttack) changed.
# The component tests (bin\Tests, including the new placement-intent, city-roster and downed-target tests) and
# Validate-Source run in the Fast tier itself. None of this is native gameplay evidence.
$ErrorActionPreference='Stop'
$failed=New-Object 'System.Collections.Generic.List[string]'
# A PowerShell 7 parent leaves its own module directories in PSModulePath; a Windows PowerShell 5.1 grandchild then
# cannot autoload its built-in Get-FileHash. Each child computes its own default module path instead.
$env:PSModulePath=$null

Write-Host 'CITY-TOOLBAR-PRONE CHILD Test-AssemblyContracts.ps1 -Target Kingmaker'
& powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Test-AssemblyContracts.ps1') -Target Kingmaker -Configuration $Configuration
if($LASTEXITCODE -ne 0){$failed.Add('Test-AssemblyContracts.ps1')}

Write-Host 'CITY-TOOLBAR-PRONE CHILD Test-GunslingerClickComposition.ps1'
& powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Test-GunslingerClickComposition.ps1') -Configuration $Configuration
if($LASTEXITCODE -ne 0){$failed.Add('Test-GunslingerClickComposition.ps1')}

if($failed.Count -ne 0){Write-Host ('CITY-TOOLBAR-PRONE FOCUSED FAIL: '+($failed -join ', '));exit 1}
Write-Host 'CITY-TOOLBAR-PRONE FOCUSED PASS (2 of 2 children)'
