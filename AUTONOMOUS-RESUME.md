# Sprint 2 resume record (2026-10-30)

Machine: DESKTOP-SRJJ623. Worktree `C:\Dev\KingmakerMountedCombatLab\worktrees\sprint2-movement-doors`,
branch `codex/mounted-combat-sprint2-movement-doors` (the request validator requires the
`codex/mounted-combat-` prefix). The root repository stays on `main` `05a9efd0` with its frozen
`bin\Release` DLL untouched.

## Custody

No native game, executor, transaction or lock remains. Retained under `runtime-evidence`:
`20261030-sprint2-r01-explore-doors-speed` (FAIL, observer fault at the mounted-solo leg; both door
interactions PASS, party leg measured), `20261030-sprint2-r02-explore-doors-speed` (game PASS, outer
validator FAIL on an unregistered row name; all four legs measured; corrected reader passes the artifact
offline), `20261030-sprint2-r04-explore-doors-speed` (PASS 9/0, restored). r03 was refused by the native
batch rule before any lock or launch. Immutable input: owner seed
`fixtures\owner-supplied\KMC_C7_DOOR_SEED_OLD_SYCAMORE_OPEN.master.zks` (`9078910198…`), selection
`analysis-cache\sprint2\owner-seed-exploration-door-open.json` (the retained 192d `ownedPairs` block).
Packages: sprint2a `db410300…` (`04b4077a`), sprint2b/c `dbc269d8…` (`8cbc27c4`/`84da91f5`), sprint2d
`8c7b9038…` (`e2b85fcc`), sprint2e `7cb519a4…` (`5caeb275`, package `0031ae56…`, suite
`sprint2e-5caeb275-20261030` `96537e39…`). Budget record `runtime-state\workflow-budgets\sprint2.json`:
22.83 of 90 min, 11 entries, batches `sprint2:exploration-1` (r01, r02 failed) and `sprint2:exploration-2`
(r04 PASS). The r04 UMM session log is overwritten by the next game session; a copy of each run's log sits
in this session's scratchpad only (not retained evidence); the attributing lines are quoted in the report.

## Next action

1. Review of the draft PR for this branch; then the owner's play checklist (CURRENT.md). Owner play
   acceptance stays separate. No merge, release or installation is part of this workflow.
2. If the owner supplies a roguelike-surface save or the trapping dungeon-door save: admit it as an owner
   seed (archive validation, one isolated cold load), then run the same `owner-seed-exploration` case; the
   speed legs and the v3 door probe need no code change for a new door as long as it is an unrestricted
   `StandardDoor` with a navmesh cut.
3. Known limits: opposite-side door approach, a second pair at the endpoint and the turn-based door path
   were not exercised natively (the TB drive logic is unchanged). The reader label "door probe writes the
   v2 contract the reader requires" now checks v3 (label not renamed).
4. Tooling facts: a new `Pass("...")` row name must be in the compiled subscenario registry or the outer
   validator fails the run after the game passed; `Chunk7NativeFixture.Select` on a mounted mount's view
   is projected to its rider by design; `-DevelopmentScopedAudit` is valid only with `-WhatIf`; a
   parameter-shape or batch-rule refusal leaves no lock, evidence or budget entry.

Prior Sprint 1 resume: [AUTONOMOUS-RESUME before Sprint 2](docs/history/AUTONOMOUS-RESUME-before-sprint2-2026-10-30.md).
