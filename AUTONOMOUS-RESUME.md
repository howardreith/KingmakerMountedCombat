# Sprint 3 resume record (2026-11-01, second review)

Machine: DESKTOP-SRJJ623. Worktree `C:\Dev\KingmakerMountedCombatLab\worktrees\sprint3-traps-spell-approach`,
branch `codex/mounted-combat-sprint3-traps-spell-approach` (the request validator requires the
`codex/mounted-combat-` prefix). The root repository stays on `main` `6240d91e` with its frozen
`bin\Release` DLL untouched.

## Custody

No native game, executor, transaction or lock remains. Retained under `runtime-evidence`:
`20261031-sprint3-r01-approach-rt` .. `r08` (real time), `r09`..`r11` (turn-based), `r12-approach-rt`, `r13-approach-rt`, `r14-approach-tb`, `r15-approach-rt`, `r16-approach-rt`, `r17-approach-tb` (PASS end to end on sprint3k), `20261101-sprint3-r18-neighbor-rt` (PASS end to end on sprint3l, batch `sprint3:review-1`) and `20261101-sprint3-r19-trap-rt` (sprint3m, trap row only: the declared door's hostile precondition refused; restoration complete).
Engine `Areas` unpack residue from r01 under `runtime-backups\20261031-sprint3-r01-approach-rt\engine-areas-residue\`
(manifest beside it). Immutable inputs: the `KMC_AUTOMATION_WORKING` fixture with the retained dependencies
`analysis-cache\sprint1\retained-dependencies-final211.json` (staged in base-game mode, never installed). Candidates
sprint3a..sprint3m (packages under `artifacts\`, suites under `runtime-state\qualification-suite-snapshots\`);
the current one is sprint3m (`24c92355`, suite `sprint3m-24c92355-20261101` SHA-256 `2356f454…`, package `c9ff0978…`, DLL `9cef7894…`). Budget record `runtime-state\workflow-budgets\sprint3.json`: allowance 105 after ONE recorded +15 adjustment (second review),
88.75 spent (80.45 at the adjustment, including the earlier reconciliation), batches `sprint3:approach-1` to `-5` and `sprint3:review-1`.

## Next action

1. Final review of PR #8 (disposition PARTIAL). Owner play acceptance stays separate. No merge, release
   or installation is part of this workflow.
2. TRAP-01 is the only outcome without native evidence. The declared dwarf door route is closed (r19: an awake
   two-unit hostile group beyond it). The only remaining input is a new save made in normal play, specified in the
   report's *Second review* section. With it, run `sprint3-approach-rt-trap`. Do not re-explore the Working fixture,
   chain doors or weaken the hostile guard.
3. No turn-based replay is selected: the second-review correction changes no turn or resource behaviour.
4. Any further launch needs a new explicit allowance or the remaining `sprint3` minutes and its own declared batch id
   (two equivalent failures per scenario key per batch; a refused request still takes the lock briefly).
5. Tooling facts: the package manifest binds the exact HEAD (a docs commit invalidates a WhatIf against an older
   package); the diagnostic DLL entry bound is 9 MiB (`Validate-Package.ps1`, boundary case in `Test-Harness.ps1`; the
   assembly is 8,475,136 bytes at sprint3m); the Candidate tier's output is relayed only at its end (capture it to a file before it throws); suite freeze in-process with `-Confirm:$false`; `Package.ps1` and the suite freeze are
   charged as imported `kind=other` entries; WMI-detached wrappers log through `cmd /c ... > outer.log 2>&1` (a plain
   `*>` redirect misses early refusals); the recipient placement helper's `-EngineShapedSaveRoot` facts are unchanged.

Prior records: [AUTONOMOUS-RESUME before Sprint 3](docs/history/AUTONOMOUS-RESUME-before-sprint3-2026-10-31.md).
