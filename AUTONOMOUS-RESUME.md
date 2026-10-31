# Sprint 3 resume record (2026-10-31)

Machine: DESKTOP-SRJJ623. Worktree `C:\Dev\KingmakerMountedCombatLab\worktrees\sprint3-traps-spell-approach`,
branch `codex/mounted-combat-sprint3-traps-spell-approach` (the request validator requires the
`codex/mounted-combat-` prefix). The root repository stays on `main` `6240d91e` with its frozen
`bin\Release` DLL untouched.

## Custody

No native game, executor, transaction or lock remains. Retained under `runtime-evidence`:
`20261031-sprint3-r01-approach-rt` .. `r08` (real time), `r09`..`r11` (turn-based), `r12-approach-rt`, `r13-approach-rt`, `r14-approach-tb`, `r15-approach-rt`, `r16-approach-rt` and `r17-approach-tb` (PASS end to end on sprint3k).
Engine `Areas` unpack residue from r01 under `runtime-backups\20261031-sprint3-r01-approach-rt\engine-areas-residue\`
(manifest beside it). Immutable inputs: the `KMC_AUTOMATION_WORKING` fixture with the retained dependencies
`analysis-cache\sprint1\retained-dependencies-final211.json` (staged in base-game mode, never installed). Candidates
sprint3a..sprint3k (packages under `artifacts\`, suites under `runtime-state\qualification-suite-snapshots\`);
the final one is sprint3k (`845892d7`, suite `sprint3k-845892d7-20261031` SHA-256 `b2b6636c…`, package `75521a56…`, DLL `ad961c3d…`). Budget record `runtime-state\workflow-budgets\sprint3.json`: allowance 90,
65.77 spent, batches `sprint3:approach-1` to `-5`.

## Next action

1. Final review of the PR opened from this branch (disposition PARTIAL). Owner play acceptance stays separate. No merge, release
   or installation is part of this workflow.
2. TRAP-01 is the only outcome without native evidence. Required input: a disposable save (never a human save) on
   an area where an active, disarmable trap stands in the party's own navmesh area 4 to 30 m from the mounted pair
   (undiscovered or already discovered and revealed). Then run `sprint3-approach-rt` on that fixture; the trap row
   discovers, clicks, carries and judges by itself and refuses early with the obstacle inventory when no trap is
   reachable. Nothing in the row, reader or registries needs to change.
3. Any further launch needs a new explicit allowance or the remaining `sprint3` minutes and its own declared batch id
   (two equivalent failures per scenario key per batch; a refused request still takes the lock briefly).
4. Tooling facts: the package manifest binds the exact HEAD (a docs commit invalidates a WhatIf against an older
   package); the diagnostic DLL entry bound is 9 MiB (`Validate-Package.ps1`, boundary case in `Test-Harness.ps1`; the
   assembly is at 8.44 MiB); suite freeze in-process with `-Confirm:$false`; `Package.ps1` and the suite freeze are
   charged as imported `kind=other` entries; WMI-detached wrappers log through `cmd /c ... > outer.log 2>&1` (a plain
   `*>` redirect misses early refusals); the recipient placement helper's `-EngineShapedSaveRoot` facts are unchanged.

Prior records: [AUTONOMOUS-RESUME before Sprint 3](docs/history/AUTONOMOUS-RESUME-before-sprint3-2026-10-31.md).
