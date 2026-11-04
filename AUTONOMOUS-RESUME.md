# Sprint 8 resume record (2026-11-04)

Machine: DESKTOP-SRJJ623. Worktree `C:\Dev\KingmakerMountedCombatLab\worktrees\sprint8-paladin-mount`, branch `codex/mounted-combat-sprint8-paladin-mount`. The root repository stays on `main`.

## Custody

No native game, executor, transaction or lock remains. Retained under `runtime-evidence`, prefix `20261104-sprint8-`: `r01` to `r09`, `s01` to `s03`, `c01`, `a01` to `a10`, `q01` to `q05`, `t01` to `t04`, `n01`. Candidates sprint8a to sprint8m under `artifacts\`, plus the negative control sprint8x; the current one is sprint8m, with q01 to q05 on sprint8l and a04, a10 on sprint8k. `n01` ran on the local, never-pushed branch `codex/mounted-combat-sprint8-negative-control` (`10044985`), which holds the reviewed defects on purpose: do not merge or package it. Budget record `runtime-state\workflow-budgets\sprint8.json`: 103.42 of 115, one +25 adjustment. A package plus another launch would fall below the admission guard's roughly 10 minutes and needs an owner decision.

## Next action

1. Final review of PR #13. Owner play acceptance stays separate (checklist in the report).
2. No merge, release, installation or Sprint 9 is selected. UI-01 is deferred.
3. A further launch needs a new package, because the manifest binds the exact HEAD including docs commits.
4. Tooling facts: rows run under `sprint8-paladin-choice-rt`, `sprint8-paladin-mount-rt`, `sprint8-paladin-dog-rt`, `sprint8-paladin-mixed-rt`, `sprint8-paladin-celestial-rt`, schema 52, reader `scripts/runtime/Sprint8PaladinEvidence.ps1`, tests `scripts/Test-Sprint8PaladinReaders.ps1`. The save route is `chunk7-persistence-save` then `-load` with `-PersistenceCase paladin-bond-exploration-manual`; the cold load pins `-PersistenceSourceRunId` and `-ExpectedPersistenceSourceSha256` (the archive's `nativeWrite.sha256`).
5. Launch with `-BaseGameOnly -TimeoutSeconds 600 -RetainedDependencySelectionPath analysis-cache\sprint1\retained-dependencies-final211.json`, and declare a new `KMC_WORKFLOW_NATIVE_BATCH` for a changed experiment after two failures of one scenario.

Prior records: [AUTONOMOUS-RESUME before Sprint 8](docs/history/AUTONOMOUS-RESUME-before-sprint8-2026-11-04.md).