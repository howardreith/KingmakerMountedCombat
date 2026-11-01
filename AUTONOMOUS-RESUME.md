# Sprint 4 resume record (2026-11-01)

Machine: DESKTOP-SRJJ623. Worktree `C:\Dev\KingmakerMountedCombatLab\worktrees\sprint4-natural-attacks`, branch
`codex/mounted-combat-sprint4-natural-attacks-retargeting`. The root repository stays on `main`.

## Custody

No native game, executor, transaction or lock remains. Retained under `runtime-evidence`: `20261101-sprint4-r01-rt`,
`r02-rt`, `r03-rt`, `r04-tb`, `r05-tb`. Reader re-evaluation of r03:
`analysis-cache\sprint4\20261101-sprint4-r03-rt-reader-reevaluation.json`. Candidates sprint4a..sprint4f under `artifacts\`;
current is sprint4f, suite `sprint4f-fdb3adf5-20261101`. Budget record `runtime-state\workflow-budgets\sprint4.json`,
batches `sprint4:accept-1` to `-3`.

## Next action

1. Review the Sprint 4 PR. Owner play acceptance stays separate (checklist in the report).
2. No merge, release or installation is part of this workflow.
3. A further launch needs its own batch id and a new package: the manifest binds the exact HEAD, including docs commits.
4. Tooling facts: run a Sprint 4 row with `sprint4-natural-rt` or `sprint4-natural-tb`, schema 48, reader
   `scripts/runtime/Sprint4NaturalEvidence.ps1`; a new row needs both registries (`RuntimeProtocol.cs` and
   `RuntimeHarness.Common.ps1`).

Prior records: [AUTONOMOUS-RESUME before Sprint 4](docs/history/AUTONOMOUS-RESUME-before-sprint4-2026-11-01.md).