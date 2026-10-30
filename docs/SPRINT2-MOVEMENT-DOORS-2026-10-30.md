# Sprint 2 — exploration speed, single-click doors and collision safety (2026-10-30)

**Disposition: SPRINT 2 ENGINEERING PARTIAL — READY FOR REVIEW; OWNER PLAY ACCEPTANCE PENDING.**
DOOR-01 and SPACE-01 are repaired at source and demonstrated natively on representative exterior door
geometry. MOVE-01 was measured natively with a matched set and no KMC speed defect was found on the
available fixture; the owner's roguelike surface and the dungeon door where the pair was trapped have no
authorized fixture, so those exact reproductions are a recorded coverage limit, not a claim.

Review branch `codex/mounted-combat-sprint2-movement-doors` (from `main` `05a9efd0`, the PR #6 merge):
`04b4077a` product + diagnostics + readers, `8cbc27c4` version `0.1.0-chunk7b-preview.99`, `84da91f5`
harness terms, `e2b85fcc` and `5caeb275` diagnostics-only corrections. Candidate identity (sprint2e, built at
`5caeb275`): DLL SHA-256 `7cb519a4ffa8ff7285161bc746e57ce4652574c331196841627f5f6b168be283`, MVID
`a4531312-36f8-42dd-bfba-750871177ce9`, package
`artifacts\KingmakerMountedCombat-0.1.0-chunk7b-preview.99-sprint2e-diagnostic.zip` SHA-256
`0031ae56c8b6d957c31bc9b45665cf87dabf03e02e976940aa059ad7e1c9bcdd`, suite `sprint2e-5caeb275-20261030`
SHA-256 `96537e396425dfe45d90369eb84708d919c8d0fec991ca085fbb2ae27147f48e`. The production door change is
identical in every sprint2 package (sprint2a `db410300…` at `04b4077a`, sprint2c `dbc269d8…` at `84da91f5`,
sprint2d `8c7b9038…` at `e2b85fcc`); only diagnostics changed between them.

## Causal defects repaired (2.1.7b contracts read from the installed assembly)

1. **DOOR-01, long approach.** `MountedDoorInteractionCommand` started immediately and stayed running
   during the mount's approach, so the native `UnitCommand.Tick` "longer than 1 round" interrupt ended
   every approach past 9 s of game time (KMC's own 12 s cutoff was unreachable). The command now stays
   unstarted while the mount approaches (unstarted commands are never ticked), `CanStart` admits it only
   inside the native proximity radius with line of sight, and the interaction runs through `OnAction`, so
   turn-based combat charges the rider exactly as a native interaction would.
2. **DOOR-01, click while moving.** A door click while the mount carried an ordinary player move was
   refused ("wait for its current command to finish"), which forced a second click. Admission now follows
   native replacement (`MountedDoorApproachPolicy`): the mount's real-time player move yields to the door
   click, a repeated click on the approached door keeps the accepted command, another door replaces it;
   pair attacks, queues and turn-based ground moves still refuse.
3. **DOOR-01/SPACE-01, approach target.** The delegated move targeted the `StandardDoor` component
   transform with no LOS object and a pure-distance range test. It now uses the native map-object approach
   point, the door's own LOS object id (`MountedDoorApproachMove`), and the native `IsUnitCloseEnough`
   predicate with the mount as the pair's footprint. A finished approach that ends outside range retries
   after the native 0.2–0.6 s cooldown, bounded by progress (three approaches without 0.25 m gain, or a
   4 s game-time stall, refuse with a reason). No elapsed-time cutoff remains.
4. **SPACE-01, occupancy.** After the door changes state the mount's navmesh occupancy is revalidated;
   an off-mesh mount with a walkable node within 1.5 m gets one native repath, never a transport, and the
   outcome records the result. Nothing disables obstacles.

Preserved: rider/mount independence, the mount's Move slot ownership, no resource writes, unmounted door
routing (policy unchanged), the TB rider-turn drive of the delegated move.

## Dispositions and evidence

| Item | Disposition | Input and geometry | Evidence |
| --- | --- | --- | --- |
| DOOR-01 single click, long approach | **PASS natively** | Old Sycamore `TavernDoor` (radius 2.0 m, static cut); pair A clicked once from 18.7 m after a native ground move away; interaction delivered at 1.99 m with LOS in 2.66 game s, one approach, no retry | r04 `observations.door.interactions[0]`, `longApproach` |
| DOOR-01 click while the mount moves | **PASS natively** | Pair B's mount given an ordinary 6 m ground move; door clicked while it moved (11.8 m away): the move ended `Interrupt`, the approach replaced it, one interaction | r04 `interactions[1]`, `clickWhileMoving` (`replacedMoveCount` 1) |
| SPACE-01 occupancy at the door state change | **PASS on this geometry** | Both pairs on the navmesh after opening, 1.96–1.99 m from the approach point, no recovery needed; capture shows the pair outside the doorway | r04 `occupancy`, `OccupancyRecovery: none`, `movement-visuals/sprint2-door-clearance.png` |
| SPACE-01 reported narrow dungeon door, opposite side, second pair at the endpoint | **NOT RUN** (no authorized fixture) | — | coverage limit |
| MOVE-01 matched set | **Measured; no KMC clamp found** | Same fixture, ordinary ground clicks of about 9 m: party-selected pair 2.85–2.96 m/s at a walk gait, every mover capped at the party limit 3.048 m/s; pair selected alone 4.43 m/s (r02: 4.22); the same Horse unmounted 4.23 (4.04); the rider on foot 3.07 (3.06); light encumbrance, clear weather, no slow conditions | r04/r02 `observations.speedLegs` |
| MOVE-01 roguelike surface | **NOT RUN** (no authorized fixture) | — | coverage limit |
| Interruption/control | Partial | Replacement of a running move and of an earlier door approach proven offline (policy tests) and natively (move replacement); Stop/pause/unreachable retained from the unchanged admission paths, not re-run natively | `MountedDoorApproachPolicyTests` 9/9 |

MOVE-01 reading: a 50-ft Horse paced to a 30-ft party walks because the native `UnitAnimationController`
selects the Slow gait whenever a unit moves under 95% of its own blueprint speed; its travel equals the
party's. Selected alone, the mounted pair is the fastest actor. Remaining candidates for the owner's area
that this fixture cannot show: weather at or past the engine's slowdown threshold (halves everyone), the
difficult-terrain condition, and the accelerated-move rule that skips any unit whose speed is affected.

## Native batch and cost

| Run | Candidate | Result |
| --- | --- | --- |
| r01 `20261030-sprint2-r01-explore-doors-speed` | sprint2c `dbc269d8…` | FAIL in the scenario: both doors PASS and the party leg measured; the mounted-solo leg selected the mount, which native selection projects to its rider (observer fault, fixed `e2b85fcc`) |
| r02 `…-r02-…` | sprint2d `8c7b9038…` | Game PASS with all four legs; outer validator FAIL: my new row name `C7-world-sprint2-speed-legs` was not in the subscenario registry (fixed `5caeb275`); the corrected reader passes the retained r02 artifact offline |
| r03 | — | refused by the batch rule before any lock or launch (third attempt of one selection); batch 2 declared on the diagnosed observer faults |
| r04 `…-r04-…` | sprint2e `7cb519a4…` | **PASS 9/0**, restored |

Budget `sprint2`: 22.83 of 90 min after 11 entries (three Candidate tiers 7/0 at 3.7 min each, three
WhatIfs, three launches of 2.2–2.7 min). Outside the record: four packages (28 s each), three suite
freezes (50 s each), two reader-only re-evaluations. Active work about 3 h 25 min. Restoration: every run
ended `restored`; the live Mods tree is empty as at intake (digest `e3b0c442…`), protected saves unchanged,
no lock, no game process, no candidate installed.

## Owner play checklist (own installation, preview.99 package)

1. On the roguelike surface, select the rider alone and click about 15 m away; then select the whole party
   and do the same. The horse should keep the party's pace at a walk, and outrun everyone when alone. Note
   the weather shown in the HUD.
2. Click a door once from 15 m or more while mounted; do not click again. Then click a door while the horse
   is still carrying out a move.
3. At the dungeon door where the pair was trapped, repeat step 2 from both sides and report the door's name
   and whether the pair could move away afterwards.

Captures: `runtime-evidence\20261030-sprint2-r04-explore-doors-speed\movement-visuals\sprint2-door-clearance.png`
(also r01), `chunk7-two-pair-mounted.png`.

## Remaining blocker

An owner-supplied disposable save on the roguelike surface and one at the trapping dungeon door would turn
the two NOT RUN rows into native evidence. No merge, release or installation is part of this sprint.
