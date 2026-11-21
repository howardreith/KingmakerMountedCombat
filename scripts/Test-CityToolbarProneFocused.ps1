[CmdletBinding()]
param([ValidateSet('Debug','Release')][string]$Configuration='Release')
# Focused selection for the mounted city toolbar, city companion roster and prone-target mission
# (docs/MOUNTED-COMBAT-CITY-AND-PRONE-MISSION.md). The controller's focused gate accepts one -Focused script name, so this
# wrapper declares exactly five checks and nothing broader (docs/VALIDATION-POLICY.md 3: no silent broadening):
#   5. Test-CityAreaAdapterBaseline.ps1: the area-restore adapter's own compile step on base dbf63702 and on the committed
#      candidate, with normalized diagnostics compared (the candidate may add none).
#   3. Test-CityArchiveRowBound.ps1: the production archive DTO, validator and codec compiled from this worktree
#      (128-row round-trip, 129-row refusal, off-bar record).
#   4. chunk7\Test-Chunk7SixPairArchiveAdapter.ps1: the actual CaptureArchive / RestoreArchivePresentation / Update methods
#      against typed facades (absent arrangements requested, loaded-world restore).
# Not selected: chunk7\Test-Chunk7AreaRestorePreflightAdapter.ps1, whose facade does not compile against base dbf63702
# either (it lacks the area-transfer lifecycle, retained-intent and MountedAreaControlRule members RestoreAreaPair uses).
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

foreach($child in @('Test-CityArchiveRowBound.ps1','chunk7\Test-Chunk7SixPairArchiveAdapter.ps1','Test-CityAreaAdapterBaseline.ps1')){
    Write-Host ('CITY-TOOLBAR-PRONE CHILD '+$child)
    & powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot $child)
    if($LASTEXITCODE -ne 0){$failed.Add($child)}
}

if($failed.Count -ne 0){Write-Host ('CITY-TOOLBAR-PRONE FOCUSED FAIL: '+($failed -join ', '));exit 1}
Write-Host 'CITY-TOOLBAR-PRONE FOCUSED PASS (5 of 5 children)'
