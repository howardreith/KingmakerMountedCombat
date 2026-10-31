# Sprint 2 resume record (2026-10-31, fifth iteration)

Machine: DESKTOP-SRJJ623. Worktree `C:\Dev\KingmakerMountedCombatLab\worktrees\sprint2-movement-doors`,
branch `codex/mounted-combat-sprint2-movement-doors` (the request validator requires the
`codex/mounted-combat-` prefix). The root repository stays on `main` `05a9efd0` with its frozen
`bin\Release` DLL untouched.

## Custody

No native game, executor, transaction or lock remains. Retained under `runtime-evidence`: r01/r02/r04/r05/r07/r08
(original verdicts), the full-run PASS r09 `20261030-sprint2-r09-doors-speed-v4-final` and r10
`20261030-sprint2-r10-traversal-core-dungeon-door-final` on DLL `809573ad…` (sprint2k), and the fifth-iteration FAIL runs
r13 `20261031-sprint2-r13-owner-surface-speed`, r14 `20261031-sprint2-r14-doors-tb-control` (sprint2n) and r16
`20261031-sprint2-r16-owner-surface-speed-level` (sprint2o); r15 was refused at admission (batch guard). Immutable
inputs: owner seed `fixtures\owner-supplied\KMC_C7_DOOR_SEED_OLD_SYCAMORE_OPEN.master.zks` (`90789101…`), the qualified
`KMC_AUTOMATION_WORKING` fixture with retained dependencies `analysis-cache\sprint1\retained-dependencies-final211.json`,
and the owner's surface save `fixtures\owner-supplied\KMC_S2_ROGUELIKE_SURFACE_CANDIDATE.master.zks` (`1fda72b1…`,
registered in `scripts/runtime/Chunk7OwnerSeedRegistry.json`). Candidates: sprint2m `0700ea67…` (superseded, never
run), sprint2n `8fcc85b7…` (suite `sprint2n-c74ab7b7-20261031`), sprint2o `1e6d008a…` (suite
`sprint2o-e76f02f4-20261031`). Budget record `runtime-state\workflow-budgets\sprint2.json`: allowance 120 (one explicit
+30 adjustment recorded), 105.87 spent, batches `sprint2:completion-1` to `-5` and `-5b`.

## Next action

1. Final review of draft PR #7 (disposition PARTIAL). Owner play acceptance stays separate. No merge, release or
   installation is part of this workflow.
2. Owner-surface MOVE-01 needs a discriminating observation before any further attempt (two equivalent failures): an
   owned run-scoped observation of what interrupts the leg units' moves (callers of `UnitMoveTo.Interrupt` /
   `UnitCommands.InterruptMove`, or game-log retention), then a grounded route choice (terrain height by physics raycast,
   party leg after the solo legs, or an owner save on an open level surface). The owner-surface route itself is
   complete: selection `analysis-cache\sprint2\owner-surface-speed.json`, wrapper route `owner-surface`.
3. Turn-based door control needs a qualified door that permits combat interaction: the retained seed's `TavernDoor` is
   `NotInCombat`. Either a new door seed or a TB-selected scan predicate (`!NotInCombat`) once an inventory proves such a
   door within 28 m; selection `analysis-cache\sprint2\owner-seed-exploration-door-open-tb.json`, route `two-pair-tb`.
4. Both need a new explicit allowance (each launch needs its 600 s outer timeout remaining at admission; the batch guard
   keys on the scenario, so a changed experiment needs a new batch id).
5. Tooling facts: the launcher binds the package manifest to the exact HEAD (any commit needs a new package + suite);
   `New-KmcQualificationSuiteSnapshot.ps1` runs in-process with `-Confirm:$false`; native turn-based GameTime does not
   advance inside a turn; `ObstacleAnalyzer.GetNearestNode` keeps the requested height (no terrain elevation from it);
   the wrapper's `*>` logs are UTF-16.

Prior records: [AUTONOMOUS-RESUME before Sprint 2](docs/history/AUTONOMOUS-RESUME-before-sprint2-2026-10-30.md).
