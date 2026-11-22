[CmdletBinding()]
param()
# Focused reproduction of Test-Harness.ps1's target-liveness assertion (test 'combat target source uses isolated group exact
# native primary raw AI no-loot and zero weapon mutation', message 'target liveness must preserve in-state/hostility admission
# and native released/finished lifecycle without invented success'), evaluated against THIS worktree's sources with exactly
# the predicate that test now uses. The mission changed the life-state part of MountedPairAttackCommand's target validity
# from an inline '!targetState.IsFinallyDead' to TargetLifeAdmits (UnifiedMountedStockAttackPolicy.TargetLifeAdmits); the
# in-state, hostility, CanAttack, termination-policy and native child-completion parts are unchanged and still required.
# The behavioural controls (dead target refused, explicit order on an already-unconscious target admitted, a target that
# falls unconscious later refused) are the component tests in tests/MountedDownedTargetOrderTests.cs. Source checks only.
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$commandSource=[IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\MountedPairAttackCommand.cs'))
$stockAttackPolicySource=[IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Domain\MountedStockAttack.cs'))
$harnessSource=[IO.File]::ReadAllText((Join-Path $repoRoot 'scripts\Test-Harness.ps1'))
$passed=0;$failed=0
function Check([string]$Name,[bool]$Condition){
    if($Condition){$script:passed++;Write-Output ('PASS '+$Name)}else{$script:failed++;Write-Output ('FAIL '+$Name)}
}
Check 'target liveness keeps termination policy, attack completion, in-state, hostility and CanAttack with the delegated life rule' (
    $commandSource.Contains('MountedTargetTerminationPolicy.Decide(') -and
    $commandSource.Contains('childAttack.IsActed && childAttack.LastAttackRule != null') -and
    $commandSource.Contains('inState && TargetLifeAdmits(attackTarget) && hostile && actionActor.CanAttack(attackTarget)') -and
    $commandSource.Contains('UnifiedMountedStockAttackPolicy.TargetLifeAdmits(state.IsConscious, state.IsDead,') -and
    $commandSource.Contains('state.IsFinallyDead, unconsciousOrderedTarget != null && ReferenceEquals(unit, unconsciousOrderedTarget));') -and
    $stockAttackPolicySource.Contains('return !targetDead && !targetFinallyDead && (targetConscious || targetUnconsciousWhenOrdered);') -and
    $commandSource.Contains('attackTarget != null && attackTarget.IsInState') -and
    $commandSource.Contains('childAttack != null && childAttack.IsFinished'))
Check 'the old inline life rule no longer stands in the target-validity line' (-not $commandSource.Contains('!targetState.IsFinallyDead && hostile && actionActor.CanAttack(attackTarget)'))
Check 'Test-Harness.ps1 carries the same corrected predicate' (
    $harnessSource.Contains("`$commandSource.Contains('inState && TargetLifeAdmits(attackTarget) && hostile && actionActor.CanAttack(attackTarget)')") -and
    $harnessSource.Contains("`$stockAttackPolicySource.Contains('return !targetDead && !targetFinallyDead && (targetConscious || targetUnconsciousWhenOrdered);')") -and
    -not $harnessSource.Contains("`$commandSource.Contains('!targetState.IsFinallyDead && hostile && actionActor.CanAttack(attackTarget)')"))
Write-Output ('TARGET LIVENESS PASS='+$passed+' FAIL='+$failed+'; source checks against this worktree; behavioural controls are tests/MountedDownedTargetOrderTests.cs')
if($failed -ne 0){exit 1}
