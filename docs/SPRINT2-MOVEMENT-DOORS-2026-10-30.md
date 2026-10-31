# Sprint 2 — exploration speed, single-click doors and collision safety (2026-10-30)

> **Completion iteration (same day, PR #7 follow-up): see [Completion iteration](#completion-iteration-2026-10-30-afternoon)
> "Review-fix iteration" and "Third iteration" at the end of this file; the third iteration holds the current disposition, identity and evidence. The first-iteration record below is kept as
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

### Review-fix iteration (same afternoon) and final disposition

**Final disposition: SPRINT 2 ENGINEERING PARTIAL — READY FOR FINAL REVIEW; OWNER PLAY ACCEPTANCE PENDING.** The
"COMPLETE" wording above was written before the independent delta review and r08; it is superseded here. The three
source gaps are closed and demonstrated natively, but the agreed list still carries NOT RUN rows (a turn-based door
control; the owner's exact surface and door) and the last run on the final binary ended in an unrelated diagnostics step,
so completion is not claimed.

**Independent delta review** (one bounded read-only review of `05a9efd0..63d83e2e`, Integration + Domain) found eight
items, all addressed in `4c57b5a8`: (1) the stall clock keyed on `IsStarted`, but a native `UnitMoveTo` travels
unstarted, so the 4 s bound was unreachable; it now keys on the live delegated move owning the mount's Move slot, and a
tick gap over 0.5 s (turn-based rounds) is re-sampled, never charged; (2) a turn-based recovery with paired activation off
had nothing driving it; recovery is now admitted only when the unified turn coordinator drives the mount, and its clock
follows the same mode/opportunity rule; (3) pair attack and mounted charge admission ignored a pending settlement; both
now cancel it (one owner of the mount); (4) a door click cancelled the recovery before deciding admission; it now decides
first and cancels only when it replaces the recovery or is admitted; (5) recovery cleanup could stop the agent while a
foreign command held the slot; it now touches the agent only when the slot is ours or empty; (6) the rider-turn animation
adapter lacked the recovery move (added as "door-recovery"); (7) a reentrant cancel during `Update` could null-dereference
(local used); (8) settlement setup after the interaction could fail the native action, and `OnEnded` could throw into the
native caller (both guarded). Component tests 934/0; Candidate tier 7/0 on the sprint2h bytes. Final identity: HEAD
`b3f78482` (lab recovery fix) over `4c57b5a8`; DLL SHA-256
`ee2f72a930808728bfd113ff1f70f865beaf7021ad0bcc018d1fc0aedbe50426`, MVID `ee7142af-86c6-4b95-9845-c48f71e59096`;
packages sprint2h `b5168c84…` (suite `sprint2h-4c57b5a8-20261030`, freeze refused by r07's stale lock, unused) and
sprint2i `91165cf5…` (suite `sprint2i-b3f78482-20261030` `f2235aaa…`, used by r08).

**r07 stale lock.** The r07 transaction (phase `restored`, Mods and saves at their restored digests) left
`runtime-state\active-transaction.lock` because the final profile-delta check threw. `Recover-KmcPersistenceProfile.ps1`
refused with "Base-game UMM parameters changed beyond the native rewrite": its pre-check compared the loader parameters
without the run's retained dependency ids although the restore step reads them from the durable Mods transaction state.
The delta was verified to be exactly the admitted shape (entries `CallOfTheWild`, `CraftMagicItems`,
`KingmakerMountedCombat` with a forward `LastUpdateCheck`; the only PlayerPrefs deltas Unity's session counter and id).
One-line fix (`b3f78482`): the pre-check passes `Get-KmcRunRetainedDependencyIds`. Recovery then passed in stale-lock
mode (`runtime-evidence\…r07…\profile-recovery.json`): loader parameters back at intake (`ca5d4022…`), lock released,
profile digest `1a8f703d…`, Mods empty, no game process.

**r08 `20261030-sprint2-r08-doors-speed-v4-fixed`** (sprint2i, batch `sprint2:completion-2`): game FAIL in the
MEASURED phase, outer FAIL; evidence retained. The door probe v4 completed and passed its own assertions on the fixed
binary: pair A one click from **25.6 m** (the 20 m setup goal was clear this time), unstarted on all 218 moving polls,
interaction at 1.96 m with LOS after 3.33 game seconds, settled footprint 7/8 → one owned 0.5 m recovery, clear
afterwards; pair B at 3.55 m during A's click, click-while-moving replaced its move, footprint 8/8 with no recovery;
Stop control clean (Interrupt, no interaction, no pending settlement, 4 frames). Speed legs mounted-party 2.82 m/s
(v1 2.84, Slow gait) and mounted-solo 4.55 (4.64) were measured; the run then failed in the first iteration's
unchanged cycle step (`MoveOnClearRoute`: "No walkable native 2 m route goal for the unmounted-control move lies clear
of every other unit's native avoidance footprint"), a layout consequence of the longer 20 m setup and the endpoint
neighbor leaving the actors spread, not a product fault; the horse-solo and rider-solo legs were not reached (r05 holds
the full matched set with the same sampler). A further attempt needs the probe to return pair A to its pre-setup start
before the cycle; it was not made because the four-hour active ceiling was exceeded.

**Outcome map (final).** DOOR-01: native PASS on both binaries (r05, r07; r08 door block); turn-based control NOT RUN
natively. SPACE-01: native PASS on the representative dungeon door (r07) and with recovery/second pair/Stop (r05, r08
door block); owner's exact door NOT RUN. MOVE-01: measured with the aligned sampler (r05 full set; r08 two legs); no KMC
clamp; owner's surface NOT RUN. Lab: r07's profile classification of the engine `Areas\` cache remains a follow-up.

**Cost.** Budget `sprint2` 58.35 of 90 min at closeout (every package, freeze, failed freeze, recovery and
re-evaluation charged); active work about 4 h 50 min for this iteration, over the four-hour ceiling by the review-fix
pass and the r07 recovery, which were judged necessary before a closeout. Live state: Mods tree empty, no lock, no game
process, no candidate installed, protected saves unchanged, loader parameters and profile root at intake.

## Third iteration (2026-10-30, evening): corrections A/B, nine-second regression, full-run PASS on the final binary

**Final disposition: SPRINT 2 ENGINEERING PARTIAL — READY FOR FINAL REVIEW; OWNER PLAY ACCEPTANCE PENDING.** The two
remaining source defects are closed and the selected door/speed/collision outcomes pass end to end on the final binary;
two agreed rows remain NOT RUN natively (the owner's exact surface/door; a turn-based door control), each with its exact
technical contract named below. This section supersedes the dispositions above.

### Identity

Commits over `4224afba`: `5b02d4dd` (product + diagnostics + lab) and `7d124fad` (delta-review fixes), then the docs
commit. Product `0.1.0-chunk7b-preview.100`, DLL SHA-256 `809573ade5fb7324d94c42f1daf5008b91e050706f79f0d1f3d7fe42529147ee`,
MVID `2b00f9fa-5f00-402e-992f-04c8e242daf0`, package `KingmakerMountedCombat-0.1.0-chunk7b-preview.100-sprint2k-diagnostic.zip`
SHA-256 `4a3497f936022601e86945f7bdecf6f2fcb4bb7ff7d8e0928a71bad9291dc68f`, suite `sprint2k-7d124fad-20261030`
SHA-256 `0e3c770164260cfa0d2db0d60fe14b1d8a588a576563ed778be245e326273ef1`. Candidate tier 8/0 on the sprint2j bytes
(`5b02d4dd`; components 942/0 incl. 8 new lifecycle tests, Harness, assembly contracts 751/0, reader 395/0, Areas
selection 7/0); the sprint2k product delta is the five review items below, covered by the component run (942/0) and the
two native runs. Independent delta review of `5b02d4dd` (read-only, bounded) ran BEFORE the native batch.

### Source corrections

- **A. A timeout never authorizes movement.** `Domain/MountedDoorSettlementLifecycle.cs`: the settlement evaluates only
  after the door's navmesh change is observed settled on two consecutive frames; past the 3 s deadline the step is
  `Unresolved`: no footprint sample, no candidate search, no move, `FootprintClear`/`OccupancyInsideNavmesh` null,
  `RecoveryDisposition = Unresolved`, `OccupancyRecovery = "unresolved"`; the door's interaction result stays on its own
  fields. Tests: delayed-then-settled graph; deadline with a pending cut though a sample would read clear; deadline with a
  would-be candidate; normal clear / blocked / no-candidate / off-turn / busy paths (all through the decision call path).
- **B. Refused input mutates nothing.** `MountedDoorApproachPolicy.PlanAdmission` and the two-phase
  `TryRouteMountedDoorInteraction`: phase one computes the turn gate, the disposition and whether the busy Move slot is
  only the pair's own recovery, without touching anything; only an admissible plan mutates in phase two (stock intent
  cleared and a dispatched stock attack interrupted, recovery cancelled, earlier door interrupted and its finished move
  swept, player move interrupted), re-decides and installs the command. Tests: every disposition off-turn plans
  `RejectWrongTurn` with no mutation; same-door retention; each replacement exactly once; busy mount not ours rejected.
- **Nine-second regression (equivalent proof).** `Domain/MountedDoorApproachLifecycle.cs` is now the command's approach
  state machine (unstarted until valid arrival; native-cooldown retries bounded by progress; mode-aware stall clock that
  re-samples tick gaps and moves its anchor only on real displacement). The OFFLINE regression (simulated game seconds,
  labelled as such) drives a 15 s approach past the nine-second boundary while moving, asserts no start/interrupt in
  between, and completes once at arrival; the native observations (r09: 171 and 39 moving polls, `startedWhileMoving`
  false on both pairs) show the command unstarted throughout real movement. The native >9 s route is NOT RUN.
- **Delta review of `5b02d4dd`** (five items, fixed in `7d124fad`): the stall anchor had been re-set every tick (slow
  creep would read as stillness); a dispatched stock attack was no longer replaceable by a door click; the phase-two
  stock cancel used the controller-wide Cancel; the replaced earlier door's finished move was not swept before
  re-deciding; the Unresolved path left `OccupancyInsideNavmesh` stale.

### Harness obstacles

- **Engine `Areas\` unpack cache (r07):** the lab already has an Areas isolation (intake tree held, engine-generated tree
  quarantined with its digest, original restored byte-exact) selected per route; `chunk4-traversal-core` now selects it
  (`PersistenceProfileAreasIsolation.ps1`, one line) with a selection test that also checks the retained r07 inventory
  (no Areas leaves at intake; the seven quarantined members). No guard was weakened; unrelated profile changes still
  refuse. r10 exercised it natively: `profile-areas-isolation.json` records the generated digest `56fa566a…` and
  `originalAreasRestoredExactly: true`; the live `Areas\` is empty as at intake.
- **r08 cycle/placement fault:** the door probe now returns both pairs toward their pre-setup starts through ordinary
  moves after its inputs (`layoutRestore` block, reader + tests). r09: pair A 11.9 m → 0.05 m, pair B 7.2 m → 0.05 m,
  both Success; the engine's cycles, sampling and cleanup then passed.

### Native runs on the final binary (batch `sprint2:completion-3`)

| Run | Route | Game | Outer |
| --- | --- | --- | --- |
| r09 `20261030-sprint2-r09-doors-speed-v4-final` | two pairs, Old Sycamore `TavernDoor` | PASS 33/0 | PASS |
| r10 `20261030-sprint2-r10-traversal-core-dungeon-door-final` | Working fixture, `DoorHolder/dwarf_dungeon_door_01` | PASS 4/4 rows, 257 assertions | PASS |

r09: unmounted native control opened the door (7.2 m, Success); pair A one click from 17.9 m, unstarted on 171 moving
polls, interaction at 1.97 m with LOS after 2.40 game s; settled in 0.017 s, footprint 7/8 → one owned 0.5 m recovery,
clear afterwards; pair B at 3.54 m during A's click (on the navmesh afterwards), then click-while-moving (move
`Interrupt`, one interaction, 39 unstarted moving polls, same recovery); Stop control clean (Interrupt, no interaction,
no pending settlement, 4 frames); layout restored; speed legs (v2) party-paced 2.69 m/s (v1 2.70, Slow gait), pair alone
4.55 (4.65), Horse unmounted 4.18 (4.26), rider on foot 3.02 (3.06); cycles, sampling, cleanup PASS. r10: the Mammoth
pair opened the closed dwarf dungeon door once (settled 0.06 s, footprint 8/8, no recovery), the row's readiness probe saw
the cut toggle off, the pair traversed the doorway; doorway both ways with unmounted control, turns/corners and party
formation PASS. Capture `…r10…\movement-visuals\mounted-distance-door-interaction-door-mounted-01.png` (`840473858…`).

### Still NOT RUN, with the exact contract

- **MOVE-01 on the owner's surface / SPACE-01 at the owner's exact door.** Inputs are copied and pinned
  (`KMC_S2_ROGUELIKE_SURFACE_CANDIDATE.master.zks` `1fda72b1…`, `KMC_S2_DUNGEON_FLOOR_CANDIDATE.master.zks`
  `4c1db37e…`, game "Akasa" `48a4bec2…`, rider `121dc2f0…` with KMC Horse `fb764728…`, Call of the Wild 1.14.4c-2.1).
  The only native speed/door route is `Chunk7WorldScenarioEngine` (two pairs: `Chunk7OwnedPairsSelection`, PowerShell
  selection validator, owner-seed registry, world evidence reader) whose setup builds two petless Ranger riders or binds
  the one registered pre-owned MOUNTED6 identity; a pre-owned single-pair mode (bind rider `121dc2f0…` to its existing
  pet, skip pair B, run speed legs on one pair) is a new selection contract across the C# selection, the launcher's
  owner-seed source and the reader, plus CotW staging for the owner-seed route. Not attempted inside this effort's
  ceilings; the engine rules measured (party pacing, walk gait under 95 % of own speed) are area-independent, but the
  surface's own weather/terrain was not measured.
- **Turn-based door control.** No existing route clicks a door in turn-based combat; the owner-seed combat checkpoint
  cases (`owner-seed-combat-*`, `Chunk7CombatCheckpointDriver`) would need a door stage (rider's acting turn, cost and
  movement allocation, rejected off-turn input) and reader rows. Offline, the stall clock, recovery admission and the
  two-phase admission are proven turn-aware; `MountedDoorInteractionCommand.OnAction` still charges through the native
  `UnitCommand` lifecycle unchanged.

### Cost and state

Budget `sprint2` 73.22 of 90 min at closeout (this iteration: package sprint2j/k, Candidate tier 3.89, suite freeze,
two WhatIfs, two launches, local test runs, all charged). Active work for this iteration about 2 h 40 min. Live state:
Mods tree empty, no lock, no game process, no candidate installed, protected saves unchanged, loader parameters and
profile root (including `Areas\`) at intake. Original verdicts retained: r05 outer FAIL (+ offline re-evaluation), r07
outer FAIL (+ recovery receipt), r08 FAIL.

## Fourth iteration (2026-10-31, early morning): the two missing acceptance routes implemented; native runs refused by the exhausted allowance

**Disposition: SPRINT 2 ENGINEERING PARTIAL — READY FOR FINAL REVIEW; OWNER PLAY ACCEPTANCE PENDING.** The two
agreed NOT RUN rows now have their complete native routes (adapter, selection, launcher dispatch, readers, tests), both
pass the guarded WhatIf qualification, and neither was launched: the `sprint2` allowance (no extension or new budget id
was selected) refuses a launch whose outer timeout (600 s) exceeds the remaining minutes. This section supersedes the
"Still NOT RUN" contracts above with executable routes and names the exact remaining cause.

### Identity

Commit `dd02ebfc` over `edeceabd` (diagnostics, readers, registry, tests; no production movement/admission/settlement
change). Product `0.1.0-chunk7b-preview.100`, DLL SHA-256 `70bbf16732099f64210366bd19c3b4bb9d2ec4761d7eec8239769e947560a134`,
MVID `8d8eeecb-8099-42d4-9dd7-63650903c461`, package `KingmakerMountedCombat-0.1.0-chunk7b-preview.100-sprint2l-diagnostic.zip`
SHA-256 `fc9252a0434ff4a1bf8d2c6e343bf9444521ba75eb83749e05753d9224aae60a`, suite `sprint2l-dd02ebfc-20261031`
SHA-256 `da7bdb194833d46ab8e84306374f92126281d94f5df6197cb173b70afee71408`. The r09/r10 evidence on DLL `809573ad…`
(sprint2k) is retained unchanged; the sprint2l delta is diagnostics-only (the two new routes, shared door-probe helpers,
the row registry, the combat-target basis for the opt-in, the request validator's descriptor branch for the new case).

### Route 1: MOVE-01 on the owner's surface (`owner-surface-speed`)

- Input: the owner's own `Manual_301_After_mounting.zks` (header name "After mounting", game "Akasa"
  `48a4bec2…`, area `DungeonStartHub_Roguelike` `c49315fe…`, Endless), registered as `KMC_S2_ROGUELIKE_SURFACE_CANDIDATE`
  in `scripts/runtime/Chunk7OwnerSeedRegistry.json` from the pinned master (`1fda72b1…`, 596816 bytes) and intake record
  (`eb30c543…`); the master is copied, never loaded. Selection `analysis-cache\sprint2\owner-surface-speed.json`
  (rider `121dc2f0…`, Horse `fb764728…`, four-member field roster, base-only, one load, zero writes). Call of the Wild
  1.14.4c-2.1 and Craft Magic Items 2.1.0 are staged from the retained Sprint 1 bytes
  (`analysis-cache\sprint1\retained-dependencies-final211.json`) because the save serializes Call of the Wild unit parts.
- Engine mode (`Chunk7WorldScenarioEngine.OwnerSurface.cs`): the save loads through the supported missing-metadata
  path (no KMC archive member, nothing restored); the one pre-owned reciprocal pair is bound exactly (nothing built,
  granted, levelled, acquired or saved; no second pair); ordinary native Mount; the four aligned speed legs
  (`sprint2-speed-legs/v2`: mounted with the party, mounted alone, Horse alone, rider on foot) on that surface; ordinary
  native Dismount; the shared cleanup preserves the owner's pet, writes no matched pool and records
  `ownerSurfacePairPreserved`. Rows `C7-world-owner-surface-mount`, `C7-world-owner-surface-legs`.
- Reader `Assert-KmcChunk7OwnerSurfaceSpeedEvidence` (PASS only when all four legs ran on the surface, the primary
  mover per leg is the Horse/Horse/Horse/rider, the pair is preserved, zero writes, exact roster).

### Route 2: DOOR-01 / SPACE-01 in native turn-based combat (`turnBasedDoor` opt-in)

- Selection `analysis-cache\sprint2\owner-seed-exploration-door-open-tb.json` = the r09 selection plus
  `"turnBasedDoor": true` (owner-seed exploration only; the Manual producer and every other kind refuse it).
- Stage (`Chunk7TurnBasedDoorProbe`, engine step 45) after the real-time door probe released the door and before the
  speed legs, both pairs mounted: door lease captured and closed as fixture preparation; the first pair's bounded
  ordinary move to a clear goal 5 m outside the door radius; the project's diagnostic target spawned in the selected
  turn-based mode with the accepted rider-first initiative inputs and AI lease (basis
  `owner-seed-disposable-copy-turn-based-door`); other party turns ended through the native End Turn binding; an
  unmounted member's own stock click as the native cost control when its turn comes first; on the first rider's acting
  turn the second pair's off-turn click (expected refused, nothing mutated), the first rider's click (routed command,
  approach on the rider's turn only, one interaction from inside the radius with line of sight), a second off-turn click
  while the approach is live (the live command and delegated move must be intact), cooldowns of all four actors before
  and after (rider Standard spent exactly once in (0.5, 6.05] s, mount Standard unchanged, no refund anywhere),
  `ApproachesWithoutProgress == 0`, no off-turn mount movement; the encounter ends by removing the target; initiative,
  AI lease and the real-time mode selection restored; the door lease restored exactly; cleanup never dismounts a pair
  inside a live encounter. A door flagged `NotInCombat` refuses before any encounter (legitimate native refusal,
  recorded). Row `C7-world-turn-based-door`; reader `Assert-KmcChunk7TurnBasedDoorEvidence`.

### Offline verification and review

Compile clean (five passes while fixing), `Validate-Source` 149/0, new `scripts/Test-Sprint2OwnerSurfaceAndTurnBasedReaders.ps1`
91/0, `Test-Chunk7WorldEvidence` 395/0, `Test-Chunk7OwnedPairsEvidence` 1481/0, `Test-Chunk7OwnedPairsWiring` 261/0,
`Test-PersistenceAreasIsolationSelection` 7/0. One bounded independent read-only review of the delta before the commit
found five items, all fixed in `dd02ebfc`: the request validator's descriptor branch did not admit the new case (would
have refused the run before any native input); the capability row's `workingSha256`/`ownerSeedSha256` shape for the
new mode; cleanup could dismount pairs inside a live encounter on the stage's failure path; a pending native control
must complete before the rider's turn is admitted; the engine's 360 s bound sits under the host's 420 s.

### Native admission

| Run | Route | WhatIf (development-scoped) | Launch |
| --- | --- | --- | --- |
| r11 `20261031-sprint2-r11-owner-surface-speed` | owner-surface (two-pair campaign scenario, CotW+CMI staged) | PASS | refused by the budget guard |
| r12 `20261031-sprint2-r12-doors-tb-control` | two-pair door seed with the turn-based opt-in | PASS | not requested (same refusal applies) |

The r11 launch was refused at admission, before Steam checks, locks, staging or any game process: "Workflow budget
refuses oversized work: native chunk7-owned-two-pair-campaign/-/launch/… is known to take about 10 min and only 7.03 min
remain". The estimate is the run's outer timeout (600 s), not the observed launch durations (1.8 to 2.9 min); the
mission selects no extension, reset or new budget id, so no launch can be admitted. Nothing was weakened to proceed.

### Cost and state

Budget `sprint2` 83.27 of 90 min (this iteration: offline compile and reader regressions 7.0, package sprint2l and the
review-fix compile 0.8, suite freeze 1.6, two WhatIfs 0.6; 54 entries, batch `sprint2:completion-4` holds only the two
WhatIfs). Active work this iteration about 3 h 10 min. Live state verified after the refusal: Mods tree empty, loader
parameters at intake (`ca5d4022…`), profile `Areas\` empty, no lock, no game process, the owner's originals
`Manual_301_After_mounting.zks` (`1fda72b1…`, 2026-08-28) and `Auto_1121.zks` (`4c1db37e…`, 2026-09-05) untouched.

### Exact remaining step

With a new explicit allowance selected by the owner (each launch needs at least its 600 s outer timeout remaining at
admission; two launches plus their restoration): run `r11` then `r12` through the wrapper routes `owner-surface` and
`two-pair-tb` against package sprint2l and suite `sprint2l-dd02ebfc-20261031`, read the results with the committed
readers, and only then decide ENGINEERING COMPLETE. No source change is needed for that step.

## Fifth iteration (2026-10-31, morning): acceptance contracts corrected; both remaining routes run natively; neither outcome demonstrated

**Disposition: SPRINT 2 ENGINEERING PARTIAL — READY FOR FINAL REVIEW; OWNER PLAY ACCEPTANCE PENDING.** The two
acceptance-contract defects named by the review of `adecb147` are fixed and independently reviewed, both routes were
executed natively against the corrected contracts, and both ended in a recorded native observation rather than a PASS:
the owner-surface party-paced leg is interrupted by a native event the retained data cannot name (twice, identically), and
the only qualifying door on the retained owner seed is natively flagged `NotInCombat`, so no turn-based door control is
possible on it. No PASS was manufactured; every failed and refused attempt is retained.

### Authorization and budget

The owner forwarded the review of PR #7 at `adecb147` (2026-10-31) with one explicit adjustment of the same `sprint2`
record, 90 to 120 minutes (recorded in the record's `adjustments` list at 83.27 spent; not a reset, no new budget id, no
further extension). The guard's launch estimate (the 600 s outer timeout) and all other timeouts were kept.

### Identity

Commits over `adecb147`: `fdf69f65` (corrections A and B), `c74ab7b7` (review blocker), `4efe4158` (speed legs v4 after
r13) and `e76f02f4` (build fix). Diagnostics only (engine speed-leg partial, turn-based door probe, readers, tests); no
production routing, cost, admission or settlement code changed; the r09/r10 evidence on DLL `809573ad…` (sprint2k) is
retained unchanged. Candidates (all packaged, suite-frozen and WhatIf-qualified; Mods digest `e3b0c442…`, the empty tree):

| Candidate | Commit | DLL SHA-256 / MVID | Package SHA-256 | Suite |
| --- | --- | --- | --- | --- |
| sprint2m | `fdf69f65` | `17be0d631af49114102025d14ee079ddcec997a09c2864c16dc9f4053962b293` / `2bea245a-a6ae-4e15-b241-9d380047933a` | `0700ea67ba7671b6851af91468d52885227d7571c4ed131d11d81428dbdbdb9a` | `sprint2m-fdf69f65-20261031` `f72dfa89…` (superseded before launch) |
| sprint2n | `c74ab7b7` | `3f8cd7765c61d0f671f6c789eab79d94ebb5cfbf0a8ca77597d6e77c7dca91f2` / `3efead4b-07f2-4990-8995-7ac75b9a1b4a` | `8fcc85b7c0165f1227436bd06392339f0965c7187721960c94c8c1df113b1263` | `sprint2n-c74ab7b7-20261031` `f15974bb2c3e6fb63459d8420e7e466e17d6f0ca5e55ce212d8df87493cb1d97` (r13, r14) |
| sprint2o | `e76f02f4` | `38a48eb3e7344a27c24d860b98458827e7eaeea9adbb815bd6c417edd1b39fd4` / `ea00f915-eefb-44d5-b37a-cc3a8c133dcd` | `1e6d008aef9e163b39ab9212a9a045096c5c00cfe185ebd0b1fbd0eef073ca1d` | `sprint2o-e76f02f4-20261031` `fab33665a46cbd459ccc93879428c87d70b45d3800f25a0ce5bf7c455df31d7d` (r16) |

### Correction A: the owner-surface reader matches mount-owned movement

Mounted ground input is routed to the mount (`GroundCommandPrefix` → `RouteGroundCommand`); the retained r09 legs show
a mounted pair with the Horse as its only mover, no rider Move row, and the mounted riders' stock agent disabled. The
previous reader demanded a rider mover row in both mounted legs and its fixture fabricated one (commands 702/705). Now:

- Speed legs contract `sprint2-speed-legs/v3` (`v4` adds the end state, below): every applicable leg records `carried`
  rows, one per moving mount with an active Mounted owner, read passively: rider/mount/owner ids and generation,
  relationship state, the rider's own Move slot at the click and at the end (`riderNewMoveCommand` stays false unless a
  new player Move appears on the rider), its stock agent state, its position offset to the mount at start/end/maximum,
  its own sampled path and whether the same owner/generation/mount held through every poll. Nothing moves the rider.
- `Assert-KmcChunk7SpeedLegCarried` (shared, v3+): each carried row names a mover in the mount role, the rider is never
  itself a mover, offset ≤ 1.0 m (observed natively 0.23–0.47 m), path ≥ half the mount's, attached throughout, stock
  agent disabled, no new rider Move; every mount-role mover carries exactly one rider. v2 legs (r09) stay accepted.
- `Assert-KmcChunk7OwnerSurfaceSpeedEvidence`: requires v3+; mounted legs have the Horse in the mount role, no rider
  mover row, exactly one carried row for the owner's rider on its Horse under the same owner/generation as the mounted
  window; the mounted-solo leg moves the Horse alone; the unmounted legs move exactly their one actor (Horse as the
  owner's pet, rider on foot) and carry nobody; every mover is in the roster and its own executor. Primary movers
  Horse/Horse/Horse/rider, area, roster, zero writes, inputs and cleanup checks are unchanged.
- Fixtures use the producer's real shape (r09); refusals: an independent rider Move on either mounted leg, a mounted leg
  carrying nobody, another horse, another actor/owner/generation, detachment, leaving the Horse, no travel, a new rider
  Move, the stock agent enabled, the solo leg moving another actor, an unmounted leg reporting a carried rider, a
  foreign executor, a foreign roster actor, v2 legs on the surface.

Confirmed natively (r13, r16): the Horse was the pair's only mover of the mounted-party leg; the carried rider had no
Move command of its own, its stock agent was disabled, it stayed 0.47 m from the Horse and travelled 7.6 m beside it.

### Correction B: turn-based acceptance tests the movement contract through settlement

Contract `owner-seed-turn-based-door-control-v2`, movement-cost contract `paired-approach-charged-to-own-mount-move/v1`
(the accepted Chunk 7 checkpoint rule: a rider's turn-based approach is native movement charged once to its own mount's
Move, equal to the allocation's movement time, inside the six-second budget; the rider keeps its own Move).

- Probe: a third cooldown boundary `cooldownsAtActionEnd` (first poll after the routed command finished), the rider
  turn's movement and remaining time there, the mount position and distance travelled, the pair's own allocation
  observation (`MountedCombatController.LastPairedMovementObservation`, parsed into `allocation`), the command result;
  movement stays observed through the owned settlement/recovery window (`settlementPolls`, `settlementMovingPolls`,
  `settlementOffTurnMovingPolls`, `turnChangedDuringSettlement`); a `movementCost` object relates the raw rows; the
  in-game Requires mirror the reader. `scope` labels state that the off-turn click proves cross-pair isolation (not the
  same controller's pending-recovery preservation, which stays offline coverage) and that zero stalls is not a native
  exhaustion/off-turn wait exercise.
- Reader: recomputes every delta from the raw rows (a fabricated delta object is refused); requires the allocation
  observation; mount Move charge > 0 for real travel, ≤ allocation time + 0.05 and equal to it when no five-foot step,
  mount Move ≤ 6.001 at the action end, allocation ledger equal to the native cooldown, rider Move unchanged (no
  transport tax), rider Standard +6 exactly once at the action end ([5.5, 6.05]), mount Standard untouched, second pair
  untouched; afterwards no increase of the mount's Move and no movement unless the settlement admitted one owned
  recovery, no re-spend of the Standard, no off-turn or turn-changing movement during settlement.
- Review blocker (`c74ab7b7`): native turn-based GameTime does not advance inside a turn (the retained TB allocation
  trace `20261018-c7b-world33-ownerseed-b6s3-tbrider` holds constant ticks across a whole rider turn, 158 and 213
  events at one tick value, and jumps only at turn boundaries), so the first version's "charge ≤ elapsed game seconds"
  bound would have refused a correct run; it was removed before any launch. Game-seconds figures remain recorded data.

The movement-cost contract was not exercised natively: r14 refused before the encounter (below).

### Offline verification and review

Compile clean; `Test-Sprint2OwnerSurfaceAndTurnBasedReaders` 133/0 (was 91); `Test-Chunk7WorldEvidence` 413/0 (was
395; v3 and v4 fixtures and their refusals); `Validate-Source` 149/0. One bounded independent read-only delta review of
`fdf69f65` (twelve findings: the blocker above, two minor, nine verified-fine items including the observation ownership
of the allocation string through `CanMoveInTurnBased` → `TryOverrideMountTurnMovement` → `TickMountMovement`, the
action-end boundary, the parser, field parity, C# 7.3 legality, the v2 path and the r09 shape).

### Native execution

| Run | Candidate / batch | WhatIf | Native result |
| --- | --- | --- | --- |
| r13 `20261031-sprint2-r13-owner-surface-speed` | sprint2n / `sprint2:completion-5` | PASS | Game FAIL (79 s). Rows `C7-world-fixture` PASS, `C7-world-owner-surface-mount` PASS (the owner's `Manual_301_After_mounting` loaded through the missing-metadata path, one load, zero writes; the pre-owned pair `121dc2f0…`/`fb764728…` bound exactly; ordinary native Mount, one active owner), `C7-world-runtime` FAIL at the first leg, `C7-world-cleanup` PASS (pair preserved, no save). Mounted-party leg: selection Akasa, Leinna and the rider (projected to the Horse); all three movers ended `Interrupt` at the same game time (3.96 s), 1.2–1.5 m short of the one admitted goal, after climbing 1.0–1.7 m toward a goal recorded at the start height; the Horse stood still about 1.7 s before the common interrupt. Horse: path 6.69 m, effective 2.93 m/s (party pace limit 3.048 m/s, Slow gait, no acceleration, own modified speed 5.08 m/s); carried rider exactly as designed. |
| r14 `20261031-sprint2-r14-doors-tb-control` | sprint2n / `sprint2:completion-5` | PASS | Game FAIL (88 s). Rows fixture, owner-seed-setup, two-native-mounts and selection-routing (the real-time door probe, released door) PASS; `C7-world-runtime` FAIL at the turn-based stage's encounter admission: the Old Sycamore `TavernDoor` reports `NotInCombat = true` (recorded on the fixture row before any encounter: open, enabled, static cut, radius 2.0, restrictions 0); the door had been closed as fixture preparation and was restored exactly (open, enabled, cut, unlock); cleanup PASS. No encounter, target, initiative or AI lease was created. |
| r15 `20261031-sprint2-r15-owner-surface-speed-level` | sprint2o / `sprint2:completion-5` | PASS | Launch refused at admission by the native batch guard ("refuses a third equivalent attempt": the batch key is the scenario, and r13 and r14 both failed under it). Retained; no game process. |
| r16 `20261031-sprint2-r16-owner-surface-speed-level` | sprint2o / `sprint2:completion-5b` | PASS | Game FAIL (38 s), identical to r13: the same admitted goal (the v4 level filter saw rise 0.0 at every sample because `ObstacleAnalyzer.GetNearestNode` keeps the requested height), the same three `Interrupt` results at the same game time (3.98 s), end state `Default`, unpaused, not in combat, not loading, same area. Cleanup PASS, restoration exact. |

Interpretation of the owner-surface data: the party-paced segment that did run (6.7 m) shows the Horse carrying the
owner's rider at 2.92–2.93 m/s against the native party-pace limit of 3.048 m/s in the Slow gait, the same shape as r09's
party-paced leg (2.69 m/s on the door seed) and the native pacing rule (party pace = slowest selected member's run speed;
Slow gait under 95 % of own speed); no KMC speed clamp or routing deficit is visible in that segment. The mounted-solo,
Horse-alone and rider-alone legs on the owner's surface were never reached, so the matched comparison on the owner's
surface is NOT DEMONSTRATED and MOVE-01 is not closed on it; r09's four legs on the door seed stand as before.

Both failures are recorded observations, not product discrepancies that this sprint can attribute: the interrupting event
is not a dialog, cutscene, load or combat (end state), and it hit two unmounted members and the Horse at the same instant;
the `NotInCombat` flag is the game's own door data.

### Causal blockers and exact remaining actions

1. Owner-surface MOVE-01: two equivalent failures (r13, r16) and no observation that discriminates a fix; a third attempt
   is not admitted. Remaining action, on a new explicit allowance: a run-scoped observation of the interrupting event
   (an owned callback on the leg units' `UnitMoveTo.Interrupt`/`UnitCommands.InterruptMove` callers, or retention of the
   game log for the run), then a route choice grounded in it (terrain height by physics raycast rather than the navmesh
   node, the party leg after the solo legs from a different start, or an owner save on an open level surface).
2. Turn-based door control: the only qualifying door on the retained owner seed is natively `NotInCombat`; the TB stage
   refuses it before any encounter, as designed. Remaining action: a door seed whose nearest qualifying door permits
   combat interaction (or a TB-selected scan predicate `!NotInCombat`, once a door inventory proves such a door lies
   within the 28 m scan radius), then r14's route again on a new explicit allowance. The r10 dwarf-dungeon door belongs to
   the traversal-core scenario, which has no turn-based stage.

### Cost and state

Budget `sprint2` 105.87 of 120 min (remaining 14.13, unused: no further launch is admitted by the equivalent-failure
rule, and the four-hour active ceiling is near). This iteration charged 22.6 min: compile and reader regressions 5.2;
package and suite sprint2m 6.7; blocker fix, package and suite sprint2n 1.5; v4 tests, failed package attempt, package
and suite sprint2o 2.4; six WhatIfs 2.0; launches r13 1.9, r14 1.9, r16 1.1. Entries 67; batches `sprint2:completion-5`
(r13, r14 failed; r15 refused) and `sprint2:completion-5b` (r16 failed). Active work this iteration about 3 h 30 min.

Live state after the last restoration: Mods tree empty (digest `e3b0c442…` before every launch), profile bytes restored
exactly on r13, r14 and r16, profile `Areas\` empty, no lock, no game process; the owner's originals
`Manual_301_After_mounting.zks` (`1fda72b1…`, 2026-08-28) and `Auto_1121.zks` untouched; no candidate installed.

## Sixth iteration (2026-10-31, afternoon): the turn-based clock correction, the surface interruption cause, the combat-usable door gate

Owner-forwarded mission (review of PR #7 at `f1b9638c`): fix the production door supervision clocks that still used
campaign `GameTime` (frozen inside a turn-based acting turn), establish the owner-surface interruption cause from
retained logs or one bounded run-scoped observation, require a combat-usable door in the turn-based selection before
any preparation, then complete both native outcomes. One explicit +20 minute adjustment (120 to 140) was recorded on
the same `sprint2` record (`adjustments[1]`, 2026-10-31T15:11:04Z, spent 105.87 at the adjustment); no reset, no new id.

### Native clock facts (installed 2.1.7b IL, read-only)
`TimeController.Tick` computes `DeltaTime = min(Time.deltaTime, PlayerTimeScale*DebugTimeScale/12)`, zeroes
`GameDeltaTime`, and only in the Default (1) and Cutscene (7) modes sets `GameDeltaTime = DeltaTime` and
`Player.GameTime += DeltaTime`; in turn-based combat it then calls `CombatController.TickTime`, which sets
`Player.GameTime = m_StartTime + TimeSinceStart` on every frame and advances `TimeSinceStart` only while passing between
turns. Inside an acting turn campaign GameTime is therefore rewound every frame while `GameDeltaTime` (the movement
timestep) stays positive. Both getters are public; KMC never calls `SetDeltaTime`/`SetGameDeltaTime`.

### Production correction (commits `058792e5`, `0c0e39c0`)
- `Domain/MountedDoorApproachLifecycle.cs`: new pure `MountedDoorOperationalClock` (accumulates the engine's per-frame
  gameplay timestep once per ticked frame; non-finite, non-positive or >0.5 s steps skipped, never charged). The
  observation carries a turn-based `TurnToken`: an approach that ended on a turn (allocation spent, route finished
  short) is re-attempted on a later turn only, as the frozen native cooldown defers the native rider approach; real
  time keeps the cooldown alone (review MAJOR-1).
- `Integration/MountedDoorInteractionCommand.cs`: the lifecycle is driven by the operational clock
  (`GameDeltaTime`, 0 while paused); the outcome keeps `ApproachElapsedGameSeconds` as observed campaign time and adds
  `ApproachElapsedOperationalSeconds`. `Integration/MountedDoorOccupancySettlement.cs`: the settlement wait and the
  recovery stall run on the operational clock (`SettleOperationalSecondsLimit` 3 s, independent of movement
  opportunity; the recovery stall keeps its opportunity gate); `OccupancySettleOperationalSeconds` added.
- Nothing writes game time, time scale, cooldowns, movement, turn state or resources.
- Offline regressions (`tests/MountedDoorLifecycleTests.cs`, controlled time, not native time): frozen campaign time
  with the retry cooldown and the no-progress bound elapsing on ticked frames; live move with opportunity and no
  displacement stalls; paused/off-turn/exhausted frames charge nothing and resuming charges no gap; pending navmesh
  update becomes Unresolved within its operational bound; turn-based approach end defers to a later turn.
- Evidence contracts: door v5 and turn-based door v3 carry the operational seconds beside the campaign seconds;
  readers bound the operational figures, require their real-time agreement with the campaign figures, and keep every
  retained shape (door v4, TB v2, legs v1 to v4) at its original verdict.

### Independent delta review (read-only, 058792e5)
No blockers; one MAJOR (false exhaustion of a turn-based approach after the mount's allocation ran out, fixed in
`0c0e39c0` with the turn token and an offline case); minors fixed: later speed legs continue only when the failed leg
ended in the quiet default mode, only the first interrupt of a mover's command is described, a stale observer is
replaced and the observer is removed even when the drain throws, RT settlement agreement and finite positive bounded
TB operational seconds in the readers.

### Diagnostics
- Turn-based door selection: `Chunk7NativeDoorProbe.FindQualifyingDoor(mounts, requireCombatUsable)` and
  `DoorFacts.CombatUsable` (`StandardDoor.NotInCombat` false) decide in the shared selection at case 7, before the
  real-time probe, any fixture close or any encounter; an absence fails the turn-based row with the read-only inventory
  (`door.rows` carry `notInCombat`, `requiredCombatUsable`). The probe reuses the same door for the real-time rows.
- Speed legs v5/v6: a failed leg is recorded (`accepted` false, `failure`) and the later legs continue for diagnosis;
  `speedLegsAccepted` stays false and fails the row at the end; the run-scoped `SpeedLegInterruptObserver` (own
  Harmony id, `UnitCommand.Interrupt` 0x060027AC) records the first native interrupt of each issued speed-leg Move
  (callers, mode, position, native path status, nearby map objects) before cleanup; one read-only door inventory of
  the owner's surface at start.

### Candidates
sprint2p (`058792e5`, DLL `14472bcc655771ab07c7e023a424c287db6d9e5c914c5b1b27d74146c6677d4b`, MVID
`c127c19a-2407-4211-9ad7-9ce62915f4d8`, package `31442e7d…`, suite `sprint2p-058792e5-20261031`) superseded by the
review fixes before any launch. sprint2q (`0c0e39c0`, DLL `21c76460993ebaa26a144fedb94b7f4eab8b2412c40b63af475a81757f286199`,
MVID `0c55c511-774d-4921-a59d-fe8d057ead3c`, package `f195cdbc8ecea53657b44f3d9e8186636b0d84b75a98479b110bdccfbf5fd1ab`,
suite `sprint2q-0c0e39c0-20261031` sha `a4bb6c1bce9e587fbbe9eeec90c541c9a430cee84557dcfc57aa92a93a1dd7fa`).
Later candidates: sprint2r (`538a1192`, speed legs v6: goal and level samples on the navmesh node height; DLL
`6391c0bf2c84978333c8c83544c5cd065995264fdcce8be1f7fc3d906c5dc4c4`, MVID `cc715d09-390d-4b54-8a85-6210156655c3`, package
`5e1003c25f8c3ccb976114f554acf826d93d3d1dfdc524ad8eb6e1ac4a191b6d`, suite `sprint2r-538a1192-20261031` sha `bd543069…`)
and sprint2s (`ba0eb6e5`, bearings tried at 9, 7 and 5 m; DLL `a51affa159fc9efb52ab2198578a23983e36cf85e9777f34ed66e8f20fc8a0ae`,
MVID `220274de-7526-4b69-bacb-bdcee1967bda`, package `927b46b0260b4baafcb31eac39fe3b3947816fddf1541d98ab85d272c873b495`,
suite `sprint2s-ba0eb6e5-20261031` sha `e1ce72e0…`). The production code (clock correction, turn token) is identical
from `0c0e39c0` onward; sprint2r and sprint2s differ from sprint2q in diagnostics only.

### Native runs (every WhatIf PASS; Mods tree empty before and after; profile, protected saves and the owner's originals restored exactly)

| Run | Candidate / batch | Result |
| --- | --- | --- |
| r17 `20261031-sprint2-r17-owner-surface-interrupt-observation` | sprint2q / `sprint2:completion-6` | Game FAIL (runtime row; 126 s). Owner pair loaded through the missing-metadata path and mounted natively. **Cause identified:** each of the three party movers was interrupted at a different time (2.47 s, 2.78 s, 3.95 s) by the native path-completion chain `UnitMovementAgent.TickMovement > CompleteMovement > InterruptMovement > UnitEntityView.OnMovementInterrupted > UnitCommand.Interrupt` with `pathDone=true`, `pathError=false`, a two-vertex path and 1.19-1.53 m left to the target; Default mode, unpaused, no combat, no loading, no map object within 8 m. The goal (-18.03, 0.70, 11.61) lay 1.7 m below the raised surface (the units ended at y 1.74-2.36): `GetNearestNode.clampedPosition` keeps the requested height, so the path ended at the node nearest in 3D, short of the goal. Not a trigger, dialog, combat or KMC call (the only KMC frames are the passive `UnitMoveController.Tick` and `CompleteMovement` prefix wrappers). The sequence continued for diagnosis: **mounted-solo 4.56 m/s** (Horse carrying the rider, 8.95 m, Success, rider 0.47 m beside it, no own Move), **horse-solo 4.54 m/s** (8.95 m), **rider-solo 3.03 m/s** (8.74 m); party-paced Horse 2.90 m/s at Slow gait against the 3.048 m/s limit over 6.70 m. Aggregate acceptance false (the party leg); door inventory of the hub: zero StandardDoors. |
| r18 `20261031-sprint2-r18-doors-tb-combat-usable` | sprint2q / `sprint2:completion-6-tb` | Game FAIL at case 7, before any real-time preparation, fixture close or encounter (109 s). The shared selection required combat usability and found none: Old Sycamore has exactly three StandardDoors, `TavernDoor` (eligible, `NotInCombat` true, 6.5/9.3 m), `RootsDoor` (one restriction, `NotInCombat` true, 40 m), `MiteYardDoor` (`NotInCombat` false, closed, unrevealed, not interactable, 94 m). Rows recorded under `door.rows` with `requiredCombatUsable`. Legitimate native refusal; no door or campaign state touched; cleanup PASS. |
| r19 `20261031-sprint2-r19-owner-surface-node-height` | sprint2r / `sprint2:completion-6` | Runtime rows all PASS (122 s), host verdict FAIL by the reader (`legsApplicable` 3 of 4). With the goal on the node height the **mounted-party** leg completed (Horse 7.46 m, 2.56 m/s Slow gait, residual 0.04 m; party members 3.02 and 2.69 m/s; no interrupt), **mounted-solo** 4.53 m/s (9.00 m, residual 0.01), **rider-solo** 3.01 m/s (8.97 m). The level samples now measure terrain (rises up to 3.3 m on the refused bearings). The **horse-solo** leg found no admissible nine-metre bearing after the dismount (clamp residuals 0.6-3.6 m, a unit 0.01 m inside the clearance on the one level bearing, real elevation changes on the others) and was recorded not applicable: the comparison stayed incomplete. |
| r20 `20261031-sprint2-r20-owner-surface-shorter-routes` | sprint2s / `sprint2:completion-6c` | Game FAIL (runtime row; 124 s). The bearing-major fallback admitted a five-metre route on the first bearing for the party leg (r19 had a nine-metre route on another bearing from the same start); the party formation moved the Horse 2.97 m (Success, residual 0.05 m, 2.80 m/s Slow gait) and the engine compared that against the nine-metre constant while the reader already used the admitted distance. The three solo legs were accepted at seven metres: **mounted-solo 4.39 m/s**, **horse-solo 4.40 m/s**, **rider-solo 2.94 m/s**. No interrupt. |
| r21 `20261031-sprint2-r21-owner-surface-distance-major` | sprint2t / `sprint2:completion-6c` | **All four legs accepted natively** (124 s): rows fixture, owner-surface-mount, owner-surface-legs and cleanup PASS; the recorded host verdict is FAIL because the reader's exact-property list did not know the two v6 goal-search keys (`startNodeHeight`, `heightBasis`); corrected reader `d1667bcd` (reader-only) re-evaluated the retained artifact (`5b168a2c…`): `Assert-KmcChunk7WorldArtifact` PASS (`analysis-cache\sprint2\20261031-sprint2-r21-owner-surface-distance-major-reader-only-reevaluation.log`; the original verdict stays in `runtime-result.json`). Legs: **mounted-party** 9 m route, Horse 7.46 m at **2.63 m/s** (Slow gait, party limit 3.048; members 3.02 and 2.66 m/s), rider carried 0.47 m beside it with no Move of its own; **mounted-solo** 9.00 m at **4.52 m/s**; **horse-solo** 6.96 m (7 m route) at **4.43 m/s**; **rider-solo** 7.13 m (7 m route) at **2.91 m/s**. No interrupt, no combat, no pause; restoration exact. |

### Interpretation (MOVE-01 on the owner's surface)
The pre-owned pair on the owner's own roguelike-hub save moves at the Horse's speed when the rider is mounted: 4.52 m/s
carrying the rider against 4.43 m/s for the Horse alone (r21; r17 gave 4.56 and 4.54), while the rider on foot walks
2.91 to 3.03 m/s. With the whole party selected the native party pacing governs (limit 3.048 m/s, the Horse at Slow
gait 2.63 to 2.90 m/s). No KMC speed clamp, no independent rider locomotion command, the rider attached throughout.
This agrees with r09 on the door seed (pair alone 4.55, Horse 4.18, rider 3.02, party-paced 2.69). The earlier
interrupted party legs (r13, r16, r17) were a diagnostic goal-height error (goal below the raised surface), not a
gameplay or KMC defect; nine metres and level terrain were diagnostic choices (the hub admits 7 m routes for two legs).

### Turn-based door control: NOT DEMONSTRATED
The combat-usability gate refused r18 before any preparation: the owner's Old Sycamore seed has no qualifying door with
`NotInCombat` false within reach (the only such door, `MiteYardDoor`, is 94 m away, closed, unrevealed and not
interactable). The corrected clock code therefore has no native door exercise on this candidate line (real-time or
turn-based): r09/r10 remain the real-time door evidence on `809573ad…`, and the clock correction is covered offline
(five controlled-time regressions) and by the independent review only. Smallest discriminating next action: an
owner-made save with the party within 28 m of a revealed, interactable `NotInCombat`-false door (Old Sycamore's Mite
yard, or another area), then the unchanged `two-pair-tb` route (the gate selects such a door itself); a compact
real-time door run on the same save would exercise the corrected clock in real time.

### Cost and state
Budget `sprint2` 130.73 of 140 min (one explicit +20 adjustment recorded; remaining 9.27, below one launch's 600 s
admission estimate). This iteration charged 24.9 min over 25 entries: five Fast tiers (build, 947 component tests,
Validate-Source 149, focused readers) 5.1; five package-and-suite freezes (sprint2p..sprint2t) 7.5; seven WhatIfs 2.4;
launches r17 2.1, r18 1.5, r19 2.1, r20 2.4, r21 2.4; the reader re-evaluation 0.1. Batches `sprint2:completion-6`
(r17, r19), `sprint2:completion-6-tb` (r18), `sprint2:completion-6c` (r20, r21); every launch WhatIf PASS; no launch
refused. Active work this iteration about 1 h 55 min.

Live state after the last restoration: Mods tree empty, profile `Areas\` empty, `modsRestored`, `saveProtectionPassed`,
`baselineImmutable`, `workingRestored` and `saveWriteAllowlistPassed` true on r17 to r21, no lock, no game process; the
owner's originals (`Manual_301_After_mounting.zks` `1fda72b1…`, the Old Sycamore seed `90789101…`) untouched; no
candidate installed. Retained unchanged: r09/r10 (PASS on `809573ad…`), r13/r14/r16 (FAIL), r15 (refused).

## Seventh iteration (2026-10-31, evening): finishing mission, native door validation on the corrected clock

Owner-forwarded mission (review of PR #7 at `6f5a2ca7`): MOVE-01 and the operational-clock source are accepted; the
remaining gate is native door validation on the corrected production code (real-time interaction/settlement and a
supported turn-based interaction). One explicit +20 minute adjustment (140 to 160) was recorded on the same `sprint2`
record (`adjustments[2]`, 2026-10-31T17:45:58Z, spent 130.73 at the adjustment; the recording script left the allowance
field at 140 for twenty minutes and it was aligned to 160 at 18:04Z with a note on the adjustment entry).

### Bounded fixture resolution (offline first, then one inspection)
| Candidate | Door facts | Predicate preventing turn-based use | Kind |
| --- | --- | --- | --- |
| `KMC_AUTOMATION_WORKING` (Endless dungeon floor `9d1278a2…`, r07/r10 traversal fixture) | `DoorHolder/dwarf_dungeon_door_01`, toggling cut, the only qualifying door; its `NotInCombat` flag was never inventoried | Inferred `NotInCombat` true: the Chunk 6A obstruction case closed this same open toggling-cut door in combat and native `InteractionComponent.CanInteract` refused it (docs/CHUNK6A-COMBAT-MOUNT.md, CM02); no retained row carries the flag, so this is an inference from the retained refusal, not a measured flag | native door data (inferred) |
| `KMC_C7_DOOR_SEED_OLD_SYCAMORE_OPEN` (Old Sycamore, r18 inventory with the flag) | `TavernDoor` eligible, `NotInCombat` true, 6.5/9.3 m; `RootsDoor` one restriction, `NotInCombat` true, 40 m; `MiteYardDoor` `NotInCombat` false, closed, unrevealed, not interactable, 94 m | No combat-usable qualifying door within 28 m; the one `NotInCombat`-false door is 94 m away, unrevealed and not interactable (an ordinary approach would have to cross the mite-infested yard; not attempted) | native door data (measured, r18) |
| `KMC_S2_DUNGEON_FLOOR_CANDIDATE` (owner's `Auto_1121.zks`, Endless floor `63de91df…`, same party as the surface save, 30 StandardDoor references of the dwarf dungeon class) | Not inventoried: the run refused before the area was loaded | Adapter limitation: the persistence isolation authority pins every owner-seed input to native save type `Manual` (`PersistenceSaveAuthorization`: "Native save name, type, campaign or area differs from the run contract"); the copied autosave enumerates as type `Auto`, so r23 refused at the isolated enumeration check before any load. The registry now admits the entry (type `Auto`) but the authority pins were deliberately NOT loosened (strict persistence guards) | adapter / safety guard |

Nothing in the installed scene data can be read offline for `NotInCombat` (scene bundle data), so the flags come from
retained in-game rows only.

### Changes (commits `43c10412`, `3ad92495`; no production code changed)
- Owner-surface single-pair turn-based door stage: the explicit `turnBasedDoor` opt-in on the `owner-surface-speed`
  selection runs, after the native Mount, the shared combat-usable door selection (`FindQualifyingDoor(mounts,
  requireCombatUsable)` from the Horse's position, before any preparation or encounter; an absence fails the row with
  the inventory), then `Chunk7TurnBasedDoorProbe` with ONE pair (second pair null: cross-pair off-turn rows recorded not
  applicable, second-pair cooldown roles null, initiative inputs for the two pair actors, selection after inputs on the
  declared other party member), then the native Dismount and cleanup; no speed legs. `RuntimeProtocol.Basis` admits the
  same combat-target basis on the seed's run copy. Readers: owner-surface door mode and turn-based single-pair mode,
  fixtures and refusals (focused readers 168/0, world 437/0, components 947/0, Validate-Source 149/0).
- The owner's autosave `Auto_1121.zks` registered as `KMC_S2_DUNGEON_FLOOR_CANDIDATE` (type `Auto`; the registry
  validator admits `Manual` or `Auto` for owner-identified read-only masters). The persistence authority's `Manual`
  pins were left untouched (see the matrix), so the entry is registered but not loadable through the current authority.
- Message literals in the diagnostics shortened: the diagnostic assembly reached the package validator's 8 MiB entry
  bound (8,390,144 bytes, refused as sprint2u); the bound is unchanged; the trimmed assembly is exactly 8,388,608 bytes.
- Candidate sprint2v (`3ad92495`): DLL `39a1006c69925d1e3f47b71c76d6e65fb27de8522c4737f2afc9271215437483`, MVID
  `df69c928-ccae-46a3-a6a6-c0f9df5711d5`, package `1115d2bca4ea1a194013582041aa1ab6a6d05d96a91d483e9ef5edf4b0b21424`,
  suite `sprint2v-3ad92495-20261031` sha `78733e87468df31939f8c7581d2478f1ba0421851a309fdb2ea3b37d3eea6456`. The first
  WhatIf of the real-time route against sprint2t was refused (the package manifest binds the exact HEAD, and the docs
  commit `6f5a2ca7` had moved it), so the real-time run also waited for this candidate.

### Native runs (WhatIf PASS for both; Mods tree empty before and after; profile, protected saves and the owner's originals restored exactly)
| Run | Candidate / batch | Result |
| --- | --- | --- |
| r23 `20261031-sprint2-r23-owner-dungeon-door-tb-single-pair` | sprint2v / `sprint2:completion-7-tb` | Game FAIL before any load (108 s): `PersistenceIsolationBootstrap` "Isolated native enumeration differs from the single copied fixture": the copied `Auto_1121.zks` enumerates natively as type `Auto` while the owner-seed authority entry is pinned `Manual`. No area loaded, no door touched, no evidence beyond the refusal; cleanup PASS, restoration flags true. The dungeon floor's doors remain uninventoried. |
| r22 `20261031-sprint2-r22-traversal-core-dungeon-door-clock` | sprint2v / `sprint2:completion-7` | **PASS natively on the corrected clock**: `chunk4-traversal-core` on the Working fixture, rows `mounted-distance-door-interaction` 59/0, `mounted-pair-doorway` 62/0, `mounted-pair-turns-and-corners` 76/0, `mounted-pair-party-formation` 60/0 (257 assertions, 173 s). The routed mounted door click from distance on `DoorHolder/dwarf_dungeon_door_01` (toggling cut) approached, interacted once, settled the navmesh (door-traversal readiness 0.088 s, five settled path replacements, no path error), traversed the doorway and the native closed-door stop/return control passed, with the pair idle and usable afterwards. This is the real-time door interaction/settlement evidence on the corrected production clock (same route as r09/r10's r10, now on `39a1006c…`). The chunk 4 evidence shape records cooldowns, paths and residuals but not the door command's operational-second fields, so the in-turn clock figures themselves have no native record yet. |

### Outcome
- Real-time door interaction and settlement on the corrected production clock: **demonstrated** (r22).
- Supported turn-based door interaction on the corrected clock: **not demonstrated**. The single-pair stage is built and
  offline-verified, but no retained copy supplies a loadable save with a combat-usable door: the Working fixture's door
  class is inferred `NotInCombat` (Chunk 6A refusal), Old Sycamore's only such door is 94 m away and unrevealed (r18),
  and the owner's dungeon-floor autosave cannot pass the persistence authority's `Manual` pin (r23). Justified external
  input: an owner-made **Manual** save (any name) on a floor or area where a revealed, interactable door with
  `StandardDoor.NotInCombat` false stands within 28 m of the mounted pair (candidates: a floor of the owner's Endless
  run with a non-dwarf door class, or Old Sycamore with the party moved to the Mite yard door and the fog revealed).
  Register it in `scripts/runtime/Chunk7OwnerSeedRegistry.json`, point a copy of
  `analysis-cache\sprint2\owner-surface-dungeon-door-tb.json` at it and run route `owner-surface-door`; the gate selects
  the door itself and refuses early with the inventory when none qualifies.

### Cost and state
Budget `sprint2` 143.58 of 160 min (adjustments +30, +20, +20 on the one record; remaining 16.42). This mission charged
12.85 min over 11 entries: five Fast tiers 6.4 (three failed on fixture/reader shape before the code was right), the
refused sprint2u package and the sprint2v package/suite 3.3, three WhatIfs (one refused on the stale HEAD binding) 1.1,
launches r23 1.5 and r22 2.9. Batches `sprint2:completion-7-tb` (r23 failed) and `sprint2:completion-7` (r22 PASS). Active
work about 55 min.

Live state after the last restoration: Mods tree empty, profile `Areas\` empty, restoration flags true on r22 and r23,
no lock, no game process; the owner's originals `Manual_301_After_mounting.zks` (`1fda72b1…`) and `Auto_1121.zks`
(`4c1db37e…`, 2026-09-05T12:28:20Z) untouched; no candidate installed. Retained unchanged: r09/r10 (PASS on
`809573ad…`), r13/r14/r16, r17, r19, r20 (FAIL), r15 (refused), r18 (gate refusal), r21 (PASS natively, FAIL verdict,
re-evaluated PASS by `d1667bcd`).
