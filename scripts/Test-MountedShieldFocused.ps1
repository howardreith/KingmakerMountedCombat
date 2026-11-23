[CmdletBinding()]
param()
# Focused selection for Mounted Shield (docs/missions/mounted-shield/MISSION.md and owner amendment 1). It declares exactly
# these checks and nothing broader (docs/VALIDATION-POLICY.md 3: no silent broadening):
#   1. Test-MountedShieldNativeContract.ps1: the installed 2.1.7b identities the feat depends on (Shield Focus, Greater
#      Shield Focus, the Power Attack donor, the four stock shield armor types' base bonus) and the compiled production
#      paths on genuine engine objects: registration contract, base-bonus read, amount, mount-AC write under the engine's
#      own stacking/touch/flat-footed rules, check write under the native roll arithmetic and the strict comparison,
#      three-feat publication, and the embedded icon's identity.
#   2. Test-MountedArcheryFeatContract.ps1: the directly affected neighbor. Mounted Archery's donor contract and its
#      coordinated publication, which Mounted Shield's registration and the generalized publication set now share.
#   3. Test-MountedRangedMovementContracts.ps1: the other consumer of the shared publication (Mounted Archery's ranged
#      rule availability); its compiled seams must be unchanged.
#   4. Test-ContinuousDriveContracts.ps1: the compiled update-failure stop in CompositionRoot (one latch store, deferred
#      retry), which Mounted Shield's stop joins (supplemental review PN3-F1).
#   5. chunk7\Test-Chunk7ReplaySubscriberContract.ps1: the reviewed global EventBus subscriber inventory; Mounted Shield
#      adds only a rulebook handler, which is not part of it (PN3-F4).
#   6. chunk7\Test-Chunk7RangerSelectionContract.ps1: the selection enable/disable transaction SetSelectionEnabled runs.
#   7. Test-FinalizeF1LocalizationKeys.ps1: every literal localization consumer has a registration (the new feat keys).
#   8. Test-MountedShieldSelectionRollback.ps1: the compiled SetSelectionEnabled on genuine selections: consecutive refused
#      disables and enables, success after refusal, all three feats and the Riding Dog list (amendment 1, PN3-F3 remainder).
#   9. Test-Sprint6ShieldReaders.ps1: the Mounted Shield gameplay driver's reader (rows, persistence case, registrations).
#  10. Test-Sprint6FeatReaders.ps1: the Sprint 6 reader, whose planned-total check helper and record the driver now share.
#  11. chunk7\Test-Chunk7CombatPersistenceEvidence.ps1 and 12. chunk7\Test-Chunk7PersistenceEvidence.ps1: the persistence
#      readers and case registries the new mounted-shield-tb-manual case joins.
#  13. chunk7\Test-Chunk7ProfileAreasIsolation.ps1: the profile/area isolation scenario list the shield scenarios join.
#  14. Test-UmmBuildReference.ps1: the pinned UMM 0.31.1 restore helper on the genuine archive (the build restores it).
#  15. Test-Chunk7NativeLoaderContracts.ps1: the native inventory reader for the selected 0.33.0 runtime identity.
#  16. Test-FrozenProductSource.ps1: build provenance (the loader pin and its restore/build helpers are product inputs).
#  17-18. Test-UmmLoaderContracts.ps1: the compiled product's loader bindings against the genuine 0.31.1 reference and
#      the installed 0.33.0 loader, each with its reviewed identity profile.
# The production amount policy, contribution ledger, publication set and contract text are unit-tested by the component
# runner (tests/MountedShieldTests.cs and the existing Mounted Combat/Archery suites), which the Fast tier itself runs with
# Validate-Source. None of this is native gameplay evidence.
$ErrorActionPreference='Stop'
$failed=New-Object 'System.Collections.Generic.List[string]'
# A PowerShell 7 parent leaves its own module directories in PSModulePath; a Windows PowerShell 5.1 grandchild then
# cannot autoload its built-in Get-FileHash. Each child computes its own default module path instead.
$env:PSModulePath=$null
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$reference=Join-Path $repoRoot 'obj\references\umm-0.31.1\UnityModManager.dll'
$children=@(
    @('Test-MountedShieldNativeContract.ps1'),@('Test-MountedArcheryFeatContract.ps1'),@('Test-MountedRangedMovementContracts.ps1'),
    @('Test-ContinuousDriveContracts.ps1'),@('chunk7\Test-Chunk7ReplaySubscriberContract.ps1'),@('chunk7\Test-Chunk7RangerSelectionContract.ps1'),
    @('Test-FinalizeF1LocalizationKeys.ps1'),@('Test-MountedShieldSelectionRollback.ps1'),@('Test-Sprint6ShieldReaders.ps1'),@('Test-Sprint6FeatReaders.ps1'),
    @('chunk7\Test-Chunk7CombatPersistenceEvidence.ps1'),@('chunk7\Test-Chunk7PersistenceEvidence.ps1'),@('chunk7\Test-Chunk7ProfileAreasIsolation.ps1'),
    @('Test-UmmBuildReference.ps1'),@('Test-Chunk7NativeLoaderContracts.ps1'),@('Test-FrozenProductSource.ps1'),
    @('Test-UmmLoaderContracts.ps1','-Reference',$reference,'-Profile','umm-0.31.1','-IgnoreLocalProfile'),
    @('Test-UmmLoaderContracts.ps1','-Profile','umm-0.33.0','-IgnoreLocalProfile'))
foreach($child in $children){
    $label=($child -join ' ')
    Write-Host ('MOUNTED-SHIELD CHILD '+$label)
    $arguments=@('-NoLogo','-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot $child[0]))+@($child | Select-Object -Skip 1)
    & powershell.exe @arguments
    if($LASTEXITCODE -ne 0){$failed.Add($label)}
}
if($failed.Count -ne 0){Write-Host ('MOUNTED-SHIELD FOCUSED FAIL: '+($failed -join ', '));exit 1}
Write-Host ('MOUNTED-SHIELD FOCUSED PASS ('+$children.Count+' of '+$children.Count+' children)')
