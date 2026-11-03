# Sprint 6 resume record (2026-11-03)

Machine: DESKTOP-SRJJ623. Worktree `C:\Dev\KingmakerMountedCombatLab\worktrees\sprint6-defensive-feat`, branch `codex/mounted-combat-sprint6-defensive-feat`. The root repository stays on `main`.

## Custody

No native game, executor, transaction or lock remains. Retained under `runtime-evidence`: `20261103-sprint6-r01-rt`, `r02-rt`, `r04-rt` to `r07-rt`, `t01-tb`, `t02-tb`, `s01-save` to `s03-save`, `c01-cold`, `f01-forced`, `f02-forced`. No `r03`: the batch guard refused it before launch. Reader re-evaluation under `analysis-cache\sprint6\`. Candidates sprint6a to sprint6i under `artifacts\`; the current one is sprint6i, suite `sprint6i-95b2945a-20261103`. The four accepted runs r07, t02, s03 and c01 belong to sprint6g. Budget record `runtime-state\workflow-budgets\sprint6.json`.

## Next action

1. Review of the Sprint 6 PR. Owner play acceptance stays separate (test sequence in the report).
2. No merge, release, installation, Charge expansion or Sprint 7 is selected.
3. A further launch needs a new package, because the manifest binds the exact HEAD including docs commits.
4. Tooling facts: rows run under `sprint6-feat-rt`, `sprint6-feat-tb` and `sprint6-feat-forced-rt`, schema 50, reader `scripts/runtime/Sprint6FeatEvidence.ps1`, tests `scripts/Test-Sprint6FeatReaders.ps1`. The save route is `chunk7-persistence-save` then `-load` with `-PersistenceCase mounted-combat-feat-tb-debt-manual`; the cold load pins `-PersistenceSourceRunId` and `-ExpectedPersistenceSourceSha256` (the archive's `nativeWrite.sha256`).
5. Launch with `-BaseGameOnly -TimeoutSeconds 600 -RetainedDependencySelectionPath analysis-cache\sprint1\retained-dependencies-final211.json`. Keep `-DevelopmentScopedAudit` to the WhatIf: a real launch refuses it.
6. A failed non-owned persistence run does not exit the game by itself. It costs the whole 600 s and needs recovery: stop that run's own process, then `Recover-KingmakerRuntimeTransaction`, then `Recover-KmcPersistenceProfile -AfterTransactionRecovery`.

Prior records: [AUTONOMOUS-RESUME before Sprint 6](docs/history/AUTONOMOUS-RESUME-before-sprint6-2026-11-03.md).
