# Sprint 8 resume record (2026-11-04)

Machine: DESKTOP-SRJJ623. Worktree `C:\Dev\KingmakerMountedCombatLab\worktrees\sprint8-paladin-mount`, branch `codex/mounted-combat-sprint8-paladin-mount`. The root repository stays on `main`.

## Custody

No native game, executor, transaction or lock remains. Retained under `runtime-evidence`, prefix `20261104-sprint8-`: `r01` to `r09`, `s01` to `s03`, `c01`, `a01` to `a10`, `q01` to `q05`. Candidates sprint8a to sprint8l under `artifacts\`; the current one is sprint8l, with `a04` and `a10` on sprint8k. Budget record `runtime-state\workflow-budgets\sprint8.json`: 80.65 of 90. Another launch is below the admission guard's roughly 10 minutes and needs an owner decision.

## Next action

1. Review of the Sprint 8 PR. Owner play acceptance stays separate (checklist in the report).
2. No merge, release, installation or Sprint 9 is selected. UI-01 is deferred.
3. A further launch needs a new package, because the manifest binds the exact HEAD including docs commits.
4. Tooling facts: rows run under `sprint8-paladin-choice-rt`, `sprint8-paladin-mount-rt`, `sprint8-paladin-dog-rt`, `sprint8-paladin-mixed-rt`, schema 52, reader `scripts/runtime/Sprint8PaladinEvidence.ps1`, tests `scripts/Test-Sprint8PaladinReaders.ps1`. The save route is `chunk7-persistence-save` then `-load` with `-PersistenceCase paladin-bond-exploration-manual`; the cold load pins `-PersistenceSourceRunId` and `-ExpectedPersistenceSourceSha256` (the archive's `nativeWrite.sha256`).
5. Launch with `-BaseGameOnly -TimeoutSeconds 600 -RetainedDependencySelectionPath analysis-cache\sprint1\retained-dependencies-final211.json`, and declare a new `KMC_WORKFLOW_NATIVE_BATCH` for a changed experiment after two failures of one scenario.

Prior records: [AUTONOMOUS-RESUME before Sprint 8](docs/history/AUTONOMOUS-RESUME-before-sprint8-2026-11-04.md).