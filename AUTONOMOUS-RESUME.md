# Sprint 3 resume record (2026-11-01, review iteration)

Machine: DESKTOP-SRJJ623. Worktree `C:\Dev\KingmakerMountedCombatLab\worktrees\sprint3-traps-spell-approach`,
branch `codex/mounted-combat-sprint3-traps-spell-approach` (the request validator requires the
`codex/mounted-combat-` prefix). The root repository stays on `main` `6240d91e` with its frozen
`bin\Release` DLL untouched.

## Custody

No native game, executor, transaction or lock remains. Retained under `runtime-evidence`:
`20261031-sprint3-r01-approach-rt` .. `r08` (real time), `r09`..`r11` (turn-based), `r12-approach-rt`, `r13-approach-rt`, `r14-approach-tb`, `r15-approach-rt`, `r16-approach-rt`, `r17-approach-tb` (PASS end to end on sprint3k) and `20261101-sprint3-r18-neighbor-rt` (PASS end to end on sprint3l, batch `sprint3:review-1`).
Engine `Areas` unpack residue from r01 under `runtime-backups\20261031-sprint3-r01-approach-rt\engine-areas-residue\`
(manifest beside it). Immutable inputs: the `KMC_AUTOMATION_WORKING` fixture with the retained dependencies
`analysis-cache\sprint1\retained-dependencies-final211.json` (staged in base-game mode, never installed). Candidates
sprint3a..sprint3l (packages under `artifacts\`, suites under `runtime-state\qualification-suite-snapshots\`);
the current one is sprint3l (`6c19b814`, suite `sprint3l-6c19b814-20261101` SHA-256 `25c14685…`, package `a1987a78…`, DLL `8abeffb9…`). Budget record `runtime-state\workflow-budgets\sprint3.json`: allowance 90,
80.45 spent after the review iteration (reconciled: five Fast tiers first charged to the development record and the uncharged sprint3k package were imported once), batches `sprint3:approach-1` to `-5` and `sprint3:review-1`.

## Next action

1. Final review of PR #8 (disposition PARTIAL). Owner play acceptance stays separate. No merge, release
   or installation is part of this workflow.
2. TRAP-01 is the only outcome without native evidence. The two compatible input forms (a Manual save of the
   Working-fixture game `cvb` with its Druid on the Mammoth beside a reachable trap, or a Manual save of the owner's
   "Akasa" game with the pre-owned pair beside a reachable trap plus a narrow owner-seed trap stage) are specified in the
   report's *Review corrections* section. The trap row refuses before any exploration when no trap is reachable.
   Do not re-explore the Working fixture's cut-off trap rooms.
3. The turn-based neighbor (`sprint3-approach-tb`) has not run on sprint3l; it needs a launch the remaining
   allowance cannot admit.
4. Any further launch needs a new explicit allowance or the remaining `sprint3` minutes and its own declared batch id
   (two equivalent failures per scenario key per batch; a refused request still takes the lock briefly).
5. Tooling facts: the package manifest binds the exact HEAD (a docs commit invalidates a WhatIf against an older
   package); the diagnostic DLL entry bound is 9 MiB (`Validate-Package.ps1`, boundary case in `Test-Harness.ps1`; the
   assembly is 8,463,872 bytes, about 8.07 MiB); the Candidate tier's output is relayed only at its end (capture it to a file before it throws); suite freeze in-process with `-Confirm:$false`; `Package.ps1` and the suite freeze are
   charged as imported `kind=other` entries; WMI-detached wrappers log through `cmd /c ... > outer.log 2>&1` (a plain
   `*>` redirect misses early refusals); the recipient placement helper's `-EngineShapedSaveRoot` facts are unchanged.

Prior records: [AUTONOMOUS-RESUME before Sprint 3](docs/history/AUTONOMOUS-RESUME-before-sprint3-2026-10-31.md).
