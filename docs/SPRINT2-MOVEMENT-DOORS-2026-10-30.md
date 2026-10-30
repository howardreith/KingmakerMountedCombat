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
