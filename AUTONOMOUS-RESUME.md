# Sprint 2 resume record (2026-10-31, fourth iteration)

Machine: DESKTOP-SRJJ623. Worktree `C:\Dev\KingmakerMountedCombatLab\worktrees\sprint2-movement-doors`,
branch `codex/mounted-combat-sprint2-movement-doors` (the request validator requires the
`codex/mounted-combat-` prefix). The root repository stays on `main` `05a9efd0` with its frozen
`bin\Release` DLL untouched.

## Custody

No native game, executor, transaction or lock remains. Retained under `runtime-evidence`: r01/r02/r04/r05/r07/r08
(see the report; r05/r07/r08 keep their original FAIL verdicts) and the full-run PASS r09
`20261030-sprint2-r09-doors-speed-v4-final` and r10 `20261030-sprint2-r10-traversal-core-dungeon-door-final` on DLL
`809573ad…` (package sprint2k, suite `sprint2k-7d124fad-20261030`). Immutable inputs: owner seed
`fixtures\owner-supplied\KMC_C7_DOOR_SEED_OLD_SYCAMORE_OPEN.master.zks` (`90789101…`), the qualified
`KMC_AUTOMATION_WORKING` fixture with retained dependencies `analysis-cache\sprint1\retained-dependencies-final211.json`,
and the owner's surface save `fixtures\owner-supplied\KMC_S2_ROGUELIKE_SURFACE_CANDIDATE.master.zks` (`1fda72b1…`, now
registered in `scripts/runtime/Chunk7OwnerSeedRegistry.json`; the dungeon floor `KMC_S2_DUNGEON_FLOOR_CANDIDATE`
`4c1db37e…` stays copied and unregistered). Fourth iteration: commit `dd02ebfc`, package sprint2l `fc9252a0…` (DLL
`70bbf167…` / MVID `8d8eeecb…`), suite `sprint2l-dd02ebfc-20261031` `da7bdb19…`; WhatIf PASS for
`20261031-sprint2-r11-owner-surface-speed` and `20261031-sprint2-r12-doors-tb-control`; no launch (budget guard
refusal). Budget record `runtime-state\workflow-budgets\sprint2.json`: 83.27 of 90 min, batches
`sprint2:completion-1` to `sprint2:completion-4`.

## Next action

1. Final review of draft PR #7 (product `7d124fad` + diagnostics `dd02ebfc`; disposition PARTIAL: the owner-surface
   speed run and the turn-based door control are implemented, WhatIf-qualified and NOT RUN natively). Owner play
   acceptance stays separate. No merge, release or installation is part of this workflow.
2. If the owner selects a new explicit allowance (a new budget id; each launch needs its 600 s outer timeout remaining
   at admission, observed launches take 1.8 to 2.9 min plus restoration): run `r11` (wrapper route `owner-surface`,
   selection `analysis-cache\sprint2\owner-surface-speed.json`, `-RetainedDependencySelectionPath` for Call of the
   Wild) and `r12` (route `two-pair-tb`, selection `analysis-cache\sprint2\owner-seed-exploration-door-open-tb.json`)
   against package sprint2l and suite `sprint2l-dd02ebfc-20261031`; the committed readers decide the rows
   `C7-world-owner-surface-mount`, `C7-world-owner-surface-legs`, `C7-world-turn-based-door`. The wrapper lives in
   the session scratchpad; its argument sets are recorded in the report's fourth iteration.
3. Tooling facts: the launcher binds the package manifest to the exact HEAD (any commit needs a new package + suite);
   `New-KmcQualificationSuiteSnapshot.ps1` must run in-process with `-Confirm:$false`; the budget guard's launch
   estimate is the run's outer timeout, not its history; `chunk4-traversal-core` runs under the Areas isolation.

Prior records: [AUTONOMOUS-RESUME before Sprint 2](docs/history/AUTONOMOUS-RESUME-before-sprint2-2026-10-30.md).
