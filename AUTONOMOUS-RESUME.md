# Sprint 3 resume record (2026-11-01, finishing mission)

Machine: DESKTOP-SRJJ623. Worktree `C:\Dev\KingmakerMountedCombatLab\worktrees\sprint3-traps-spell-approach`,
branch `codex/mounted-combat-sprint3-traps-spell-approach` (the request validator requires the
`codex/mounted-combat-` prefix). The root repository stays on `main` `6240d91e` with its frozen
`bin\Release` DLL untouched.

## Custody

No native game, executor, transaction or lock remains. Retained under `runtime-evidence`:
`20261031-sprint3-r01-approach-rt` .. `r08` (real time), `r09`..`r11` (turn-based), `r12-approach-rt`, `r13-approach-rt`, `r14-approach-tb`, `r15-approach-rt`, `r16-approach-rt`, `r17-approach-tb` (PASS end to end on sprint3k), `20261101-sprint3-r18-neighbor-rt` (PASS end to end on sprint3l, batch `sprint3:review-1`), `20261101-sprint3-r19-trap-rt` (sprint3m, trap row only: the declared door's hostile precondition refused; restoration complete) and the finishing-mission trap runs `20261101-sprint3-r20-trap-rt`, `r21`, `r23`, `r24` (sprint3n, o, p, q; r22 was refused by the batch guard before launch and has no evidence tree).
Engine `Areas` unpack residue from r01 under `runtime-backups\20261031-sprint3-r01-approach-rt\engine-areas-residue\`
(manifest beside it). Immutable inputs: the `KMC_AUTOMATION_WORKING` fixture with the retained dependencies
`analysis-cache\sprint1\retained-dependencies-final211.json` (staged in base-game mode, never installed). Candidates
sprint3a..sprint3q (packages under `artifacts\`, suites under `runtime-state\qualification-suite-snapshots\`);
the current one is sprint3q (`de5abf08`, suite `sprint3q-de5abf08-20261101` SHA-256 `8fa2910d…`, package `6958aa3c…`, DLL `fac005d6…`; product source unchanged since sprint3m `24c92355`). Budget record `runtime-state\workflow-budgets\sprint3.json`: allowance 135 after the recorded +15 (second review) and ONE +30 (finishing mission) adjustments,
118.27 spent (88.75 at the +30), batches `sprint3:approach-1` to `-5`, `sprint3:review-1`, `sprint3:final-1` and `-2`.

## Next action

1. Final review of PR #8 (disposition PARTIAL). Owner play acceptance stays separate. No merge, release
   or installation is part of this workflow.
2. TRAP-01 is the only outcome without native evidence. The declared dwarf door route is now prepared natively (r24:
   the two declared hostiles dead through the native life rules, the door open on a settled graph), but no active trap
   joins the mounted pair's navmesh area behind it. The remaining input (a normal-play save beside a reachable trap, or
   an owner decision naming a further route) is in the report's *Finishing mission* section. Do not chain doors, roam
   or weaken a guard without that decision.
3. No turn-based replay is selected: no product, turn or resource behaviour changed after sprint3m.
4. Any further launch needs a new explicit allowance or the remaining `sprint3` minutes and its own declared batch id
   (two equivalent failures per scenario key per batch; a refused request still takes the lock briefly).
5. Tooling facts: the package manifest binds the exact HEAD (a docs commit invalidates a WhatIf against an older
   package); the diagnostic DLL entry bound is 9 MiB (`Validate-Package.ps1`, boundary case in `Test-Harness.ps1`; the
   assembly is 8,483,328 bytes at sprint3q); the Candidate tier's output is relayed only at its end (capture it to a file before it throws); suite freeze in-process with `-Confirm:$false`; `Package.ps1` and the suite freeze are
   charged as imported `kind=other` entries; WMI-detached wrappers log through `cmd /c ... > outer.log 2>&1` (a plain
   `*>` redirect misses early refusals); the recipient placement helper's `-EngineShapedSaveRoot` facts are unchanged.

Prior records: [AUTONOMOUS-RESUME before Sprint 3](docs/history/AUTONOMOUS-RESUME-before-sprint3-2026-10-31.md).
