# Sprint 7 resume record (2026-11-03)

Machine: DESKTOP-SRJJ623. Worktree `C:\Dev\KingmakerMountedCombatLab\worktrees\sprint7-charge`, branch `codex/mounted-combat-sprint7-charge`. The root repository stays on `main`.

## Custody

No native game, executor, transaction or lock remains. Retained under `runtime-evidence`: `20261103-sprint7-` `r01-rt`, `r02-tb`, `r03-rt`, `r05-rt`, `r06-tbm`, `s01-save`, `a01-rt`, `a02-tb`, `a03-tbm`, `a04-save`, `a05-cold`, `a06-pounce`. No `r04`: the batch guard refused it before launch. Reader re-evaluations under `analysis-cache\sprint7\`. Candidates sprint7a to sprint7j under `artifacts\`; the accepted product runs are sprint7i, the current one sprint7j. Budget record `runtime-state\workflow-budgets\sprint7.json`, batches `sprint7:accept-1` to `-4`.

## Next action

1. Review of the Sprint 7 PR. Owner play acceptance stays separate (checklist in the report).
2. No merge, release, installation or Sprint 8 is selected.
3. A further launch needs a new package, because the manifest binds the exact HEAD including docs commits.
4. Tooling facts: rows run under `sprint7-charge-rt`, `sprint7-charge-tb`, `sprint7-charge-tb-mount-first` and `sprint7-charge-pounce-rt`, schema 51, reader `scripts/runtime/Sprint7ChargeEvidence.ps1`, tests `scripts/Test-Sprint7ChargeReaders.ps1`. The save route is `chunk7-persistence-save` then `-load` with `-PersistenceCase mounted-charge-tb-committed-manual`; the cold load pins `-PersistenceSourceRunId` and `-ExpectedPersistenceSourceSha256` (the archive's `nativeWrite.sha256`).
5. Launch with `-BaseGameOnly -TimeoutSeconds 600 -RetainedDependencySelectionPath analysis-cache\sprint1\retained-dependencies-final211.json`, and declare a new `KMC_WORKFLOW_NATIVE_BATCH` for a changed experiment after two failures of one scenario.

Prior records: [AUTONOMOUS-RESUME before Sprint 7](docs/history/AUTONOMOUS-RESUME-before-sprint7-2026-11-03.md).