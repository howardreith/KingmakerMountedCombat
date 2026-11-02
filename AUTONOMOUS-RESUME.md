# Sprint 4 resume record (2026-11-01)

Machine: DESKTOP-SRJJ623. Worktree `C:\Dev\KingmakerMountedCombatLab\worktrees\sprint4-natural-attacks`, branch
`codex/mounted-combat-sprint4-natural-attacks-retargeting`. The root repository stays on `main`.

## Custody

No native game, executor, transaction or lock remains. Retained under `runtime-evidence`: `20261101-sprint4-r01-rt`, `r02-rt`,
`r03-rt`, `r04-tb`, `r05-tb`, and `20261102-sprint4-r06-intent`, `r07-intent`, `r08-intent`. Reader re-evaluations of r03 and r08
are under `analysis-cache\sprint4\`. Candidates sprint4a..sprint4i under `artifacts\`; current is sprint4i, suite
`sprint4i-526c32e6-20261101`. Budget record `runtime-state\workflow-budgets\sprint4.json`, batches `sprint4:accept-1` to `-5`.

## Next action

1. Final review of PR #9. Owner play acceptance stays separate (checklist in the report).
2. No merge, release or installation is part of this workflow.
3. A further launch needs its own batch id and a new package: the manifest binds the exact HEAD, including docs commits.
4. Tooling facts: rows run under `sprint4-natural-rt`, `sprint4-natural-tb` or `sprint4-intent-rt`, schema 48, reader
   `scripts/runtime/Sprint4NaturalEvidence.ps1`. A new row needs both registries (`RuntimeProtocol.cs` and
   `RuntimeHarness.Common.ps1`). Launch these with `-TimeoutSeconds 600`: the default 360 s wait is shorter than the game's 460 s
   deadline and blocks the launcher's restoration. After such a block: `Recover-KingmakerRuntimeTransaction`, then
   `Recover-KmcPersistenceProfile -AfterTransactionRecovery`.

Prior records: [AUTONOMOUS-RESUME before Sprint 4](docs/history/AUTONOMOUS-RESUME-before-sprint4-2026-11-01.md).
