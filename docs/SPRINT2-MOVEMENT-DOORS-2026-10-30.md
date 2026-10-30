# Sprint 2 — exploration speed, single-click doors and collision safety (2026-10-30)

> **Completion iteration (same day, PR #7 follow-up): see [Completion iteration](#completion-iteration-2026-10-30-afternoon)
> at the end of this file for the current disposition, identity and evidence. The first-iteration record below is kept as
> written.**

**Disposition (first iteration): SPRINT 2 ENGINEERING PARTIAL — READY FOR REVIEW; OWNER PLAY ACCEPTANCE PENDING.**
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

## Completion iteration (2026-10-30, afternoon)

**Disposition: SPRINT 2 ENGINEERING COMPLETE — READY FOR FINAL REVIEW; OWNER PLAY ACCEPTANCE PENDING.** The three
source gaps named by the reviewer are closed at source and demonstrated natively; the reported roguelike surface and the
owner's exact dungeon door remain a stated coverage limit (the owner's own saves were located and copied, see
"Fixture intake"), covered by a faithful representative of the same door class and campaign.

### Identity

Branch `codex/mounted-combat-sprint2-movement-doors`, HEAD `3ef8119f` (reader correction) over `63d83e2e` (product +
diagnostics), on top of the reviewed `5a0f2abd`. Product `0.1.0-chunk7b-preview.100`. DLL SHA-256
`85542fbfc7fe49b751e72c0d50db95a5962fe9ad9924aab46491d793cf43d696`, MVID `9d761c39-8cf1-4045-9ae4-e74671e051ee`
(byte-identical in both packages: the build is deterministic). Packages: sprint2f
`KingmakerMountedCombat-0.1.0-chunk7b-preview.100-sprint2f-diagnostic.zip` (`b915dba2…`, suite
`sprint2f-63d83e2e-20261030` `ac50fab5…`, used by r05) and sprint2g `…-sprint2g-diagnostic.zip` (`6d29b6f8…`, suite
`sprint2g-3ef8119f-20261030` `9b9afadb…`, used by r07; needed only because the launcher binds the package to HEAD and
the reader correction moved HEAD). Candidate tier 7/0 on the packaged bytes (components 934/0 including 19 new policy and
sampler tests, harness, assembly contracts 751/0); world-evidence reader tests 389/0.

### Source corrections (reviewer items A, B, C)

- **A. SPACE-01 footprint-aware, settled, owned collision handling.** New `Domain/MountedDoorOccupancyPolicy.cs` and
  `Integration/MountedDoorOccupancySettlement.cs`. The command's native action still ends at `door.Interact` (turn-based
  cost stays native); the controller then owns a settlement that (1) waits for the door's navmesh change to land
  (door in its expected state, `NavmeshCut.RequiresUpdate()` false, no queued A* graph update, a frame after the last
  tile update; bounded at 3 game seconds), (2) samples the mount's logical footprint (the movement-agent corpulence circle
  around the entity position, 8 ring samples: on the navmesh, reachable along the mesh from the center without crossing
  a wall, same navmesh area through `PathUtilities.IsPathPossible`; the carried rider occupies nothing of its own),
  (3) if the footprint is not clear, searches 80 candidates (5 rings to 2.5 m by 16 bearings) for the first that is
  walkable, on-mesh, reachable by navmesh trace, in the same area, outside the door's cut bounds and body-clear, and
  runs exactly one owned `UnitMoveTo` on the mount (never a transport, no collision toggle, no resource write),
  (4) observes completion / interruption / replacement (a player's own order natively replaces it) / stall and the
  footprint afterwards, (5) reports the disposition on the same outcome instance. Stop, retarget, dismount, area change
  and a later door click cancel it; in turn-based combat a recovery is admitted only on the rider's acting turn.
  Prevention: closing a door whose cut is written on state changes while the footprint overlaps that cut is refused
  before the command starts (no cost spent) with the reason.
- **B. DOOR-01 stall handling no longer disabled by paired activation.** `MountedDoorApproachPolicy.StallClockRuns`
  decides from the actual mode and the mount's movement opportunity: a running real-time approach always counts
  stillness; in turn-based combat only frames on which the native movement override admitted the mount with budget left
  (`MountedCombatController.TryOverrideMountTurnMovement` records that frame); a paused game counts nothing. The
  `!NativePartnerMovement` term is gone; the overall travel cutoff stays removed. Tests cover the production-facing
  combination (real time with paired activation on), paused and off-turn controls, sustained progress and genuine
  no-progress.
- **C. MOVE-01 measurement.** `Diagnostics/SpeedLegSampler.cs` opens the motion interval at the last at-rest sample so
  path and time cover the same samples; the v1 (unaligned) figure is recorded beside it. Speed-leg contract v2. The
  deterministic test proves a constant 2 m/s leg reads exactly 2 m/s with the first segment included and that the v1
  interval reads 3 m/s on the same samples. Retained r02/r04 keep their v1 data.

### Fixture intake (mission section 5)

One bounded read-only header inventory of the configured save directory (284 archives, header.json only) located the
owner's own Endless-campaign game "Akasa" (`48a4bec2…`): `Manual_301_After_mounting.zks` (2026-08-28, area
`c49315fe…` = DungeonStartHub_Roguelike, the roguelike surface, with the KMC Horse `fb764728…` owned by rider
`121dc2f0…`; the owner's 2026-08-28 session log names the trapping door `dwarf_dungeon_door_01`) and `Auto_1121.zks`
(2026-09-05, dungeon floor `63de91df…`, 30 StandardDoor references). Both were copied as read-only intake candidates
with provenance records (`fixtures\owner-supplied\KMC_S2_ROGUELIKE_SURFACE_CANDIDATE.*`, master SHA-256
`1fda72b1…`; `KMC_S2_DUNGEON_FLOOR_CANDIDATE.*`, `4c1db37e…`); the originals are byte- and timestamp-identical. They
are **not admitted**: the two-pair owner-seed contract requires two petless Ranger riders built by the native level-up
driver (or the one registered pre-owned MOUNTED6 identity), and this party already owns its Horse; admission needs a
pre-owned-pair selection generalization and Call of the Wild 1.14.4c-2.1 staged from retained bytes. The representative
chosen instead is the already-qualified `KMC_AUTOMATION_WORKING` fixture: the same Endless campaign, a dungeon floor
(`9d1278a2…`) of the same dwarf-dungeon tileset, door `DoorHolder/dwarf_dungeon_door_01` with
`DisableNavmeshCutWhenOpen` true (the cut toggles with the door state, the collision class the Old Sycamore static-cut
door cannot exercise), loaded with Call of the Wild and Craft Magic Items staged from retained bytes.

### Native runs

| Run | Candidate | Game | Outer | What it proved |
| --- | --- | --- | --- | --- |
| r05 `20261030-sprint2-r05-doors-speed-v4` (two-pair, Old Sycamore seed) | sprint2f | PASS 33/0 | FAIL, then PASS offline on the corrected reader (`analysis-cache\sprint2\20261030-sprint2-r05-doors-speed-v4-reader-only-reevaluation.log`) | door v4 probe and speed legs v2 below |
| r06 | sprint2g not yet built | refused before launch | - | package bound to the previous HEAD after the reader commit (0.1 min) |
| r07 `20261030-sprint2-r07-traversal-core-dungeon-door` (Working fixture, dwarf dungeon door) | sprint2g | PASS 4/4 rows, 257 assertions | FAIL: profile classification (engine `Areas\` unpack cache) | dungeon-door interaction, traversal, doorway, turns, formation |

r05 (door `TavernDoor`, radius 2 m): the unmounted party member `5dd7bd1d…` opened the door first through the native
`UnitInteractWithObject` (control; from 7.2 m, Success). Pair A then clicked once from 17.8 m after the farthest clear
setup move the fixture offers (26 and 20 m had no clear goal; 12 m did): the command stayed **unstarted on all 172 polls on
which the mount moved**, started inside the radius with line of sight (1.999 m), interacted once, approach 2.43 game
seconds. The settled footprint was 7/8 (one ring sample inside the door's static cut); the owned recovery moved the
mount 0.5 m (admitted candidate 1 of 1 evaluated), completed, footprint clear afterwards, Move slot restored. Pair B
stood 3.55 m from the approach point while pair A clicked (endpoint neighbor; on the navmesh afterwards), then clicked
while its mount carried an ordinary move (the move ended `Interrupt`, one interaction, 40 unstarted moving polls, same
0.5 m recovery). Stop control: pair A clicked from 17.7 m, the native Stop (`UnitCommands.InterruptAll` on the rider)
ended it after 3 moving frames with the command still unstarted: result Interrupt, no interaction, door closed, mount and
rider command-free, no pending settlement, settled in 4 frames. Speed legs (v2 aligned interval, v1 beside):
party-paced pair 2.54 m/s (v1 2.55; Slow gait throughout, limit 3.048), pair alone 4.17 (4.25), Horse unmounted 4.33
(4.42), rider on foot 3.04 (3.08). The v1 bias is 2 to 4 % upward on these 9 m legs; the ordering and the walk-gait
explanation are unchanged. Capture `runtime-evidence\20261030-sprint2-r05-doors-speed-v4\movement-visuals\sprint2-door-clearance.png`
(viewed: both mounted pairs stand on open ground in front of the fenced tavern doorway, neither inside the fence or wall).

r07 (door `DoorHolder/dwarf_dungeon_door_01`, toggling cut): `mounted-distance-door-interaction` PASS (59 assertions):
the Mammoth pair clicked the closed door from outside immediate range, one approach, one interaction, door open; the
settlement observed the cut land (settled in 0.11 s), footprint clear 8/8, no recovery; the row's own readiness probe
recorded the cut toggling off (`initialNavmeshCutRequiresUpdate` true then false, a frame after the tile update) and the
pair then traversed the doorway to the far side (`door-mounted`). `mounted-pair-doorway` PASS (62): unmounted door
control and the mounted pair through the doorway both ways. `mounted-pair-turns-and-corners` PASS (76) and
`mounted-pair-party-formation` PASS (60). Capture
`runtime-evidence\20261030-sprint2-r07-traversal-core-dungeon-door\movement-visuals\mounted-distance-door-interaction-door-mounted-01.png`
(viewed: the mounted Mammoth stands beside the closed double door in the tiled dungeon room, not in the wall). The outer
FAIL: the engine unpacks the loaded archive into the profile root's `Areas\` (7 files, written at 20:11:24Z during the
session) and the lab's profile-recovery rule admits no new leaf there; restoration otherwise completed (Mods tree,
Working fixture, loader parameters). The seven files were moved, hashes recorded, into
`runtime-backups\20261030-sprint2-r07-traversal-core-dungeon-door\engine-areas-residue\`, after which the profile root's
inventory digest equals its intake snapshot (`1a8f703d…`). The protection rule was not weakened; admitting the
engine's unpack cache for Working-fixture scenarios is a follow-up for the lab, not this sprint.

### Acceptance map

| Outcome group | Disposition |
| --- | --- |
| DOOR-01 single click, long approach | PASS natively (r05: 17.8 m, unstarted while moving; r07: dungeon door). The literal >9 s approach was not reproduced: the Old Sycamore fixture offers no clear route beyond 12 m inside the probe's 28 m bound and the accelerated mount covers 17.8 m in 2.4 s; the structural proof is that the command is never started while the mount moves, so the native nine-second interrupt (started commands only) cannot apply. |
| DOOR-01 click while moving / same door / other door / Stop / pause / no-progress | click-while-moving PASS natively (r05); same-door retention and other-door replacement offline (policy tests), unchanged from the first iteration; Stop PASS natively (r05); pause and no-progress offline (stall-clock tests), pause also implied by game-time accounting. |
| DOOR-01 turn-based control | NOT RUN natively: a turn-based door needs the combat checkpoint chain (three launches) that the remaining allowance did not cover; offline the stall clock and recovery admission are proven turn-aware. |
| SPACE-01 narrow dungeon door, both sides | PASS natively on the representative dwarf dungeon door (r07: open from outside, traverse through, doorway both ways), settled footprint clear. |
| SPACE-01 second pair at the endpoint, recovery | PASS natively (r05: neighbor at 3.55 m; completed owned recovery of 0.5 m with the footprint clear afterwards); unreachable / interrupted / replaced branches offline. |
| SPACE-01 owner's exact door | NOT RUN (owner save located and copied, not admissible without a selection generalization). |
| MOVE-01 matched set | Measured with the aligned sampler; no KMC clamp; native party pacing and walk gait explain the report. |
| MOVE-01 roguelike surface | NOT RUN natively (same owner save). |

### Cost and restoration

Budget `sprint2` (one record, no reset): 28.20 min after reconciling the first iteration's uncharged packages, suite
freezes and reader re-evaluations; 46.13 of 90 min at closeout (Candidate tier 3.95, three WhatIfs, three launch
records, two packages, two suite freezes plus one failed freeze attempt, offline re-evaluations and the r07 profile
diagnosis, all charged). Active work for this iteration about 3 h 50 min. Live state at closeout: Mods tree empty as at
intake, no lock, no game process, no candidate installed, protected saves unchanged, profile root at its intake digest.
