# Sprint 2 resume record (2026-10-31, sixth iteration)

Machine: DESKTOP-SRJJ623. Worktree `C:\Dev\KingmakerMountedCombatLab\worktrees\sprint2-movement-doors`,
branch `codex/mounted-combat-sprint2-movement-doors` (the request validator requires the
`codex/mounted-combat-` prefix). The root repository stays on `main` `05a9efd0` with its frozen
`bin\Release` DLL untouched.

## Custody

No native game, executor, transaction or lock remains. Retained under `runtime-evidence`: r01..r08 (original
verdicts), the full-run PASS r09/r10 on DLL `809573ad…` (sprint2k), the fifth-iteration FAIL runs r13/r14/r16, and the
sixth-iteration runs r17 (interrupt cause identified), r18 (combat-usable door gate refused), r19, r20 (route search
refinements) and r21 (all four owner-surface legs accepted; host verdict FAIL on reader shape, re-evaluated PASS by
reader `d1667bcd`, log `analysis-cache\sprint2\20261031-sprint2-r21-owner-surface-distance-major-reader-only-reevaluation.log`).
Immutable inputs: owner seed `fixtures\owner-supplied\KMC_C7_DOOR_SEED_OLD_SYCAMORE_OPEN.master.zks` (`90789101…`), the
owner's surface save `fixtures\owner-supplied\KMC_S2_ROGUELIKE_SURFACE_CANDIDATE.master.zks` (`1fda72b1…`), the qualified
`KMC_AUTOMATION_WORKING` fixture with retained dependencies `analysis-cache\sprint1\retained-dependencies-final211.json`.
Candidates this iteration: sprint2p (`058792e5`, superseded unlaunched), sprint2q (`0c0e39c0`; r17, r18), sprint2r
(`538a1192`; r19), sprint2s (`ba0eb6e5`; r20), sprint2t (`de4ff23b`; r21; suite `sprint2t-de4ff23b-20261031`). Budget
record `runtime-state\workflow-budgets\sprint2.json`: allowance 140 (adjustments +30, +20), 130.73 spent, 92 entries,
batches `sprint2:completion-1` to `-6c`.

## Next action

1. Final review of draft PR #7 (disposition PARTIAL). Owner play acceptance stays separate. No merge, release or
   installation is part of this workflow.
2. Turn-based door control: the only outcome not demonstrated. It needs an owner-made save with the party within 28 m
   of a revealed, interactable door whose `StandardDoor.NotInCombat` is false (Old Sycamore's `MiteYardDoor`, or another
   area); register it in `scripts/runtime/Chunk7OwnerSeedRegistry.json`, point a selection like
   `analysis-cache\sprint2\owner-seed-exploration-door-open-tb.json` at it and run route `two-pair-tb`: the shared
   selection now requires combat usability itself and refuses early with the inventory otherwise. A compact real-time
   door run on the same save would also exercise the corrected production clock natively (not yet done on this line).
3. Both need a new explicit allowance (9.27 min remain; a launch needs its 600 s outer timeout at admission) and a new
   declared batch id.
4. Tooling facts: the package manifest binds to the exact HEAD (any commit needs a new package + suite);
   `New-KmcQualificationSuiteSnapshot.ps1` runs in-process with `-Confirm:$false`; `Package.ps1` and the suite freeze are
   charged as imported `kind=other` entries; native TB GameTime is rewound every frame of an acting turn while
   `GameDeltaTime` runs; `GetNearestNode.clampedPosition` keeps the requested height and `node.position.y*0.001` is the
   real navmesh height; the wrapper's `*>` logs are UTF-16; the batch guard keys on the scenario, so a different
   experiment uses its own declared batch id.

Prior records: [AUTONOMOUS-RESUME before Sprint 2](docs/history/AUTONOMOUS-RESUME-before-sprint2-2026-10-30.md).
