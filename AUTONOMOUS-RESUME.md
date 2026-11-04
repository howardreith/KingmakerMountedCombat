# Sprint 9 resume record (2026-11-04)

Machine: DESKTOP-SRJJ623. Worktree `C:\Dev\KingmakerMountedCombatLab\worktrees\sprint9-integration-release`, branch `codex/mounted-combat-sprint9-integration-release`. The root repository stays on `main`.

## Custody

No native game, executor, transaction or lock remains. Retained under `runtime-evidence`, prefix `20261104-sprint9-`: `a01` to `a06`, `e01` to `e08`, `f01`, `g11` to `g21` where run, plus `20261104-c7e-final-B901-r31`, `-r32`, `-B902-r31`, `-r32`. Candidates sprint9a to sprint9g under `artifacts\`; the current one is sprint9g. Budget record `runtime-state\workflow-budgets\sprint9.json`: 79.73 of 90. A package plus another launch would fall below the admission guard and needs an owner decision.

## Next action

1. Review the Sprint 9 PR. Owner play acceptance stays separate: [the script](docs/SPRINT9-OWNER-PLAY-SCRIPT.md). Record only results the owner supplies.
2. No merge, tag, publication or installation is selected.
3. A further launch needs a new package, because the manifest binds the exact HEAD including docs commits.
4. Tooling facts: `integrated-tb-feat-charge-manual` is a `chunk7-persistence-save` then `-load` case. `sprint8-paladin-tb` and `sprint3-approach-rt-spells` are the narrow selections. The base-game control is `chunk7-normal-player-control` with `-ObserverPackagePath`, `-TimeoutSeconds 900`. The removal chain needs run ids matching `*-c7e-final-B*-r31`/`r32` and a run map.

Prior records: [AUTONOMOUS-RESUME before Sprint 9](docs/history/AUTONOMOUS-RESUME-before-sprint9-2026-11-04.md).