# Sprint 3 — traps and spell target approach while mounted (2026-10-31)

> **Review iteration (2026-11-01):** the section *Review corrections* below holds the current disposition, candidate
> identity and evidence; the first-iteration record that follows it is kept, with its incorrect claims corrected in place.

**Disposition (first iteration): SPRINT 3 ENGINEERING PARTIAL — READY FOR REVIEW; OWNER PLAY ACCEPTANCE SEPARATE.**
SPELL-01 is demonstrated natively in real time and in turn-based combat on the candidate of that iteration. TRAP-01 is
implemented and shares the natively evidenced driver, but its trap-specific layers have neither native evidence nor
offline execution: every trap on the Working fixture sits in a
navmesh area that ordinary input cannot enter (fixture limit, recorded below). Nothing is merged, released or installed.

Review branch `codex/mounted-combat-sprint3-traps-spell-approach` from `main` `6240d91e` (the PR #7 merge). Product
commits `f22e6bd1` (carried approach) and `3980e1cf` (turn-based admission of the delegated approach move); diagnostics,
readers and registries `f12662b6`, `9cc2a463`, `ae72ebdb`, `1af5bb2c`, `63534a24`, `2993655f`, `e0011565`, `c63bd260`,
`0a4e0e1f`, `6f54bcf8`, `845892d7`; docs follow. Final candidate sprint3k (built at `845892d7`): DLL SHA-256
`ad961c3dbdf5316598434a3fd6750e2a6afee1d687f43e887bf5e8a9570e4045`, MVID `565f890d-3522-4812-8e59-88907e1f3ed1`,
8 457 216 bytes; package `artifacts\KingmakerMountedCombat-0.1.0-chunk7b-preview.100-sprint3k-diagnostic.zip` SHA-256
`75521a56af393f7a6f382cd2eef73f57f39fedacb5a28996d6164d2a97e64075`; suite `sprint3k-845892d7-20261031` SHA-256
`b2b6636c4a43de563ca4a4cd60635b6ac7a26a2f0cab115aafc53f302febac0f` (protected-save authority
`20260816T0115379973604Z-user-attested-quicksave-continuity`, authority `111c794b…`, pin set `6f137d57…`). The production
carried-approach code is identical from sprint3d on; only `3980e1cf` changed production after that (one admission
predicate), so sprint3f/3g/3h carry the same product behaviour and sprint3a–3e differ from it only by that predicate.

## Review corrections (2026-11-01, PR #8 review of `aa8702b9`)

**Disposition after the review iteration: SPRINT 3 ENGINEERING PARTIAL — READY FOR REVIEW; OWNER PLAY ACCEPTANCE
SEPARATE.** Both command-ownership defects are repaired with focused regressions and the held-charge ownership neighbor
ran natively on the corrected candidate (r18 PASS end to end on sprint3l). TRAP-01 still has no native evidence: neither inspected
retained alternative supplies a reachable trap (measured below), so a compatible external fixture is required.

Corrected candidate sprint3l, built at `6c19b814`: DLL SHA-256
`8abeffb960b1e933c25f726f72256359980211e77051837ae71ecf3b64f957a5`, MVID `a09a28ff-bc3b-4243-ab83-bb519c60993c`,
8,463,872 bytes; package `artifacts\KingmakerMountedCombat-0.1.0-chunk7b-preview.100-sprint3l-diagnostic.zip` SHA-256
`a1987a781c01ee3bb897bd610918fd88eae85bd6cb81b2ff2372fd1e46486ea1`; suite `sprint3l-6c19b814-20261101` SHA-256
`25c146852ce0efe3d1b09a39df6fe1b7299aad856f471c412e273a33c296ed0a` (same protected-save authority and pin set).

### Correction A: admission before destructive release

The `UnitCommands.Run` prefix used to call `ObservePairCommandRequest` before the door, stock-attack and stock-command
gates, so a request refused later (for example a wrong-turn door click) had already stopped the delegated move and
discarded the approach. Now `MountedCarriedRequestFlow.Admit` runs those gates in their existing order without touching
the approach and gives a ticket only to a request every gate admitted. A new Run postfix completes the ticket from what
native Run actually displaced (`MountedCarriedApproachPolicy.AfterAdmittedRequest`):

- a refused request, a cursor simulation and a native preparation command get no ticket and release nothing;
- an admitted rider request releases the approach only if native Run displaced the carried command (replaced it or
  interrupted it as an out-of-range unstarted command); a command Run queued or ran beside it keeps it;
- the carrier's own delegated move and the carried command's own run never release it;
- an admitted mount request releases the approach only when another command now owns the mount's movement, and only
  an ordinary player order (`CreatedByPlayer`, no `AiAction`) also ends the rider's pending command; an AI request has
  no authority over it;
- a native boundary inside Run that already settled the approach, a duplicate completion and another unit's request
  never release.

The Sprint 2 door route keeps its own two-phase admission; it now runs inside the same gate sequence.

### Correction B: release only movement the carrier still owns

`ClearFinishedDelegatedMove` used to call `mount.View.StopMoving()` unconditionally and reported restoration as "the
mount has no commands". It now goes through `MountedOwnMovePolicy.Release`: it ends only the carrier's exact move,
sweeps only when that move still sits in its slot, re-reads ownership after the sweep (native
`RemoveFinishedAndUpdateQueue` can `Run` the queue head) and stops the agent only when no other live or queued command
owns the mount's movement. `MountMoveSlotRestored` now means "our move is finished and in no slot"; a retained successor
is reported separately (`SuccessorRetained`). Native facts relied on: `UnitCommand.Interrupt(false)` and `OnEnded`
never touch the agent; only `StopMoving` does. A due move while another command owns the mount no longer throws (which
used to interrupt the rider's command through the exception path): the approach yields (`YieldedToMountOwner`) and the
controller defers re-binding while the mount is busy.

The same unconditional `StopMoving` pattern exists in the Sprint 2 door command (`MountedDoorInteractionCommand`) and
the pair attack command (`MountedPairAttackCommand`); neither is part of this review and both are unchanged.

### Focused regressions (offline, the production seams the prefix, postfix and carrier call)

`tests/MountedCarriedApproachPolicyTests.cs`, nine new cases replacing the old "any different rider request releases"
case that encoded the defect: a request refused by the door gate and one refused by the last gate (later gates not run,
nothing released, approach identity unchanged); an admitted replacement released exactly once with a duplicate
completion inert; the carrier's own move, the carried command's own run and a non-displacing request kept; player
versus AI mount-order authority; a native boundary, a foreign container and a missing ticket inert; cleanup while the
carrier owns its slot; a finished move whose slot holds a successor (the successor's command and path untouched, also
for a successor in another slot); a successor the cleanup sweep activates (path kept, not stopped); repeated cleanup
idempotent and never stopping a later successor. The fakes model native slot, queue and agent ownership; nothing is
asserted from source text.

### Other changes

- The trap row now refuses before any exploration leg when no candidate trap shares the pair's navmesh area (native A*
  connectivity), recording the obstacle inventory; the review's instruction not to re-explore the cut-off Working rooms
  is enforced by the row itself.
- Carried outcomes record `successorRetained` and `yieldedToMountOwner`; the reader refuses either on these fixtures,
  which issue no other mount order (fields absent on older evidence are tolerated).
- Named subset scenario `sprint3-approach-rt-neighbor` runs exactly the existing `S3-touch-held-charge` row; the
  reader accepts it only under that name and never as the full real-time set.
- Harness pins: the door and stock-attack routing pins follow the new admission seam; the Phase 3E `schemaVersion` pin
  was stale since `f12662b6` (schema 47 prefix) because no Candidate tier ran during Sprint 3; it now matches the full
  current expression.

### Validation on the corrected bytes

| Check | Result |
| --- | --- |
| Candidate tier (build, components, Validate-Source, Test-Harness, …) | build, components and Validate-Source PASS; Test-Harness 280 PASS, 1 FAIL (the stale Phase 3E pin), so the tier stopped there |
| Test-Harness, the corrected Phase 3E test evaluated whole | 9 of 9 assertions PASS |
| Test-PairedActivationContracts, Test-RuntimeArtifactManifestContract, Test-AssemblyContracts Kingmaker | PASS (30 patch-construction, 11 manifest, 751 assembly) |
| Component tests on the frozen sprint3l build | 962 PASS (the nine new cases included) |
| Validate-Source (inside Package.ps1) | 149 PASS |
| Sprint 3 reader regressions | 63 PASS |
| Native r18 `sprint3-approach-rt-neighbor` on sprint3l | PASS end to end: runtime result PASS, reader PASS, Mods restored, save protection passed, 0 errors |

The Candidate tier was not rerun after the pin fix (a rerun did not fit the remaining allowance beside the native
neighbor); the failing test was evaluated whole instead and its other 280 harness tests had already passed on the same
product bytes.

### Native evidence: what ran on which bytes

- **Executed on the corrected bytes (sprint3l):** r18, the held-charge row (Stop, second ordinary click, the carrier's
  own delegated move through the new Run admission and cleanup). One ordinary cast click; the mount carried the delivery approach; the Stop released the carried move (first outcome: released by the Stop, own move restored, no successor, no yield) and the charge stayed held; the second ordinary click resolved to the held touch, was carried again and satisfied at 1.58 m (second outcome: own move restored, no successor, no yield); one heal, one scroll charge, no charge left; 4.87 m of mount travel; rider and mount command sets empty at the end. Evidence `runtime-evidence\20261101-sprint3-r18-neighbor-rt`, batch `sprint3:review-1`.
- **Retained, earlier bytes, still valid as evidence of the native behaviour they recorded:** real-time spell rows on
  sprint3e to sprint3k (r08, r12, r13, r15, r16; r16 on sprint3k `ad961c3d…`, aggregate FAIL because of its trap row),
  turn-based rows r14 natively and r17 end to end on sprint3k `ad961c3d…`. They predate both corrections and are not
  claimed for sprint3l.
- **Offline only on the corrected bytes:** the turn-based neighbor (the insufficient-movement row uses the changed
  cleanup and the native approach-end path); the remaining real-time spell rows; unmounted controls; a refused request
  during a live carried approach and a successor during cleanup (both covered only by the regressions above).
- **Not executed:** TRAP-01 natively (below).

### TRAP-01: fixture feasibility (two retained alternatives, offline inventories)

The review's correction stands: the seven original policy tests choose the command category, compare flags and drift
and drive the shared Sprint 2 lifecycle; they do not execute the trap-specific safe point (`OverrideTarget` outside the
trigger zone with the mount's footprint) or the native `TrapObjectView.Interact` Trickery check. Those layers have no
native evidence and no offline execution; only the shared driver (the delegated move to the command's own radius, the
native command acting afterwards) is natively evidenced, through the spell rows.

| Candidate | Load facts | Trap facts | Verdict |
| --- | --- | --- | --- |
| `KMC_S2_ROGUELIKE_SURFACE_CANDIDATE` (`1fda72b1…`, Manual, game "Akasa", area `c49315fe…`) | owner seed, pre-owned pair | no `TrapObjectData` in the area state | no trap |
| `KMC_S2_DUNGEON_FLOOR_CANDIDATE` (`4c1db37e…`, **Auto**, Endless floor `63de91df…`, delve seed −1950525545) | needs Call of the Wild 1.14.4c-2.1 (unit parts in party and area state; retained bytes exist); pre-owned pair rider `121dc2f0…` on Horse `fb764728…` (a `KMC_Horse_AddPet` fact), party level 1, standing near (28, −34) | three active undiscovered traps (Snowball `b903a3bd…` at (56, 52), Magic Missile `111f3390…` at (7.9, 52.4) and (63.9, 52)), 88–93 m from the pair; 9 of 10 doors closed; at least three hostile groups between pair and traps (nearest revealed group 22–31 m away) | no trap reachable without fighting through the floor, which is a playthrough |

The Working fixture is not re-explored (r12 to r16 measured all four traps in A* areas 13 and 7 against the party's
10, the nearby `Secret_Door` interaction refusing with `CanInteract` false, no skill, DC 0). One retained fact was not
pursued under the review's instruction: the r15 inventory shows the closed `dwarf_dungeon_door_01` at (40, 60) as
revealed and clickable from the second settle; whether opening it joins area 10 to a trap area is unknown.

**Required external input (either form):**

1. **For the existing route, no row change:** a Manual save of the Working-fixture game (`cvb`, game id
   `d41185be-edc1-47c0-b9d5-e1d7a9c8e65f`, area `9d1278a2f599b2a4daab53abdfe88d2e`) with the Druid `b6628a77…` mounted
   on the Mammoth `d79a4f6c…` and the recipient `d17c8fd0…` in the party, standing out of combat in the same navmesh
   area as an active disarmable trap 4–30 m away (undiscovered, or discovered and revealed), no hostiles within the
   trap's perception radius. It would replace or sit beside the qualified Working identity, which needs the existing
   fixture requalification and protected-save authority steps for the new hash.
2. **For the owner's own campaign:** a Manual save of the "Akasa" game on an Endless floor with the pre-owned pair
   (rider `121dc2f0…` on Horse `fb764728…`) out of combat within 4–30 m of an active disarmable trap in the same room,
   hostiles cleared, registered in `scripts/runtime/Chunk7OwnerSeedRegistry.json` as type `Manual` and staged with the
   retained Call of the Wild bytes. This form also needs a narrow owner-seed trap stage binding the trap row to that
   pair (not built: there is no admitted input to build it against).

Not examined (the review capped this at two alternatives): the registered owner seeds on area `b10e8651…`, including
`KMC_C7_MOUNTED6_SEED`.

## First causal boundary (installed 2.1.7b IL, read-only)

Both reports share one cause. The rider's own native command (`UnitUseAbility` for a spell, `UnitInteractWithObject`
for a discovered trap) approaches its target through `UnitCommand.TickApproaching`, which calls the protected virtual
`OnTickApproaching` (token `0x060027AE`) and then `Executor.View.MoveTo(target, radius)`. A carried rider's view is
attached to the mount and its stock `AgentASP` is disabled, so `MoveTo` never moves anything: the cast or disarm waits
for a range it cannot reach, and in real time the native "longer than one round" interrupt eventually ends it. Nothing
in KMC re-routed these commands before this sprint (doors and ground moves were routed; abilities and interactions were
not). In turn-based combat the same path requests a path once and sets `NextApproachTime = MaxValue`;
`UnitActionController.TickCommandTurnBased` then force-finishes a should-approach command once its executor is no longer
moving, which is the native "insufficient movement" rule.

Other contracts the change relies on (all read from `Assembly-CSharp.dll`, MVID `07fa1e4d…`): `ClickMapObjectHandler`
→ `UnitInteractWithObject` (trap approach radius 0.3 m, LOS required; `UpdateTarget` sets an `OverrideTarget` so the
executor stops outside `TrapObjectView.ScriptZoneTrigger`); `TrapObjectView.Interact` rolls `RuleSkillCheck(user,
SkillThievery, DisableDC)` and raises `IDisarmTrapHandler` (critical failure triggers the trap); `UnitUseAbility.Init`
(sticky touch real-time radius = speed × 3 s; `AbilityData.GetApproachDistance` for ranged spells);
`AbilityEffectStickyTouch.Apply` creates `UnitPartTouch` and queues the delivery `UnitUseAbility` first; a native touch
miss keeps the charge held; `PartyPerceptionController.Tick` rolls each moving controllable character's Perception once
per rank per trap (`StaticEntityData.LastPerceptionRollRank`, persisted in the save) inside
`PerceptionCheckComponent.Radius` (a 3D distance) with line of sight and the trap outside fog; `UnitCommands.Run`
replaces a same-slot running command and interrupts other unstarted commands.

## Product change (`src/KingmakerMountedCombat`)

- `Domain/MountedCarriedApproachPolicy.cs` (new): pure decisions. A command is carried only when it is the mounted
  rider's own unstarted `UnitUseAbility` or trap `UnitInteractWithObject`; started/finished commands, the mount's and
  third units' commands, Mount/Dismount relationship shells, the Charge shell, AI-originated casts and cursor
  simulations stay native. Real-time move end → lifecycle retry; turn-based move end → signal the native approach-end
  rule. Replanning follows a moving target only beyond 0.75 m; since the review, only a rider request native Run admitted
  and that displaced the carried command releases it (correction A).
- `Integration/MountedCarriedApproach.cs` (new): one driver per carried command. Each native approach tick is replaced
  by: the exact native `OnTickApproaching` (a pinned `callvirt` bridge, so the game's own TB bookkeeping still runs),
  then the mount's delegated move through the accepted Sprint 2 lifecycle (`MountedDoorApproachLifecycle`,
  operational clock, `MountedDoorApproachMove`) to the command's own radius minus 0.5 m (floor 0.3 m). For a trap the
  delegated point is the native safe point outside the trigger zone with the mount's corpulence, applied as
  `OverrideTarget`. In turn-based combat a finished delegated move sets `NextApproachTime = MaxValue`, exactly what the
  native tick does, so the native rule ends an unreachable command. Exhaustion (no progress / stall) calls the
  command's own `Interrupt()`; nothing is finished, spent or refunded by KMC.
- `MountedCombatController.cs`: the carried driver joins the retained-pair-command, exact-mount-movement, animation
  and independent-turn predicates, the update sweep, cancel/interrupt observation and the door-click interplay (a
  door click releases a carried move the same way it replaces a ground move). `MountedPatchController.cs`: the native
  approach prefix defers to the carrier; the command-run prefix observes rider requests. One new rejection code
  (`NoPath`).
- `PairedActivationLifecycle.cs` (`3980e1cf`): the rider-turn admission of a cooldown-ignoring partner command tested
  the exact type `UnitMoveTo`; the delegated approach move is a derived `UnitMoveTo`, so in turn-based combat it was
  never admitted on the rider's turn (r09 waited the whole turn). The test is now `is UnitMoveTo`. This is also the
  most likely reason Sprint 2's turn-based door approach never moved; it is recorded, not re-run.

Preserved: native cost ownership (the cast, the touch delivery, the Trickery check, the trap consequence and every
turn-based budget are the engine's), pair-owned command authority, unmounted controls (unmounted units never enter the
carrier), the Charge and relationship shells, Stop/pause behaviour (a Stop releases the carried move and leaves the
native command to the native Stop).

Offline: `tests/MountedCarriedApproachPolicyTests.cs` (7 tests at the first iteration, 15 after the review) in the Fast
tier (954 component tests then, 962 after the review; 149 source validations, PASS); `scripts/Test-Sprint3ApproachReaders.ps1`
(54 checks at the first iteration, 63 after the review: positive envelopes, schema 47/dispatch/
registry agreement, and a negative per native fact — trap click without native discovery, discovery without an
observed roll, Trickery/Perception fixture ranks beyond their bound or unrestored, double heal, held charge left after
delivery, legal miss keeps the charge, ranged cast beyond range, charge lost across the turn, insufficient-movement row
without the native rule, failed producer envelope promoted).

## Native harness (diagnostic assembly only)

`Diagnostics/Sprint3ApproachScenario.cs` joins the casting-fixture family (Mammoth engine, Working fixture Druid
`b6628a77…` on Mammoth `d79a4f6c…`, recipient `d17c8fd0…`): scenarios `sprint3-approach-rt` (rows `S3-trap-disarm`,
`S3-touch-friendly-distant`, `S3-touch-held-charge`, `S3-touch-hostile`, `S3-ranged-distant`) and `sprint3-approach-tb`
(`S3-tb-insufficient-movement`, `S3-tb-touch-friendly`), evidence schema 47, observation key `sprint3Approach`, reader
`scripts/runtime/Sprint3ApproachEvidence.ps1`. Every input is ordinary: `ClickMapObjectHandler.OnClick` for the trap,
`ClickWithSelectedAbilityHandler.OnClick` for spells, `SelectionManager.Instance.Stop()` for the stop, ground clicks for
exploration legs. The fixture is a native scroll stack of Cure Light Wounds plus the Druid's memorized slots and a
Snowball scroll; the trap row grants the disposable rider Trickery ranks up to character level (nobody on the Working
fixture has one) and, only when the save's recorded rolls had consumed the pair's current ranks, one Perception rank, both
restored at drain and checked by the reader. Discovery is the party's own native Perception roll during bounded
exploration legs (walkable point three metres inside the 3D perception radius, outside the trigger zone, re-ordered at
most three times when the pair settles short); the row never sets a discovery flag.

The diagnostic DLL reached the package validator's 8 MiB entry bound at Sprint 2's end; the bound is raised once to
9 MiB (`scripts/Validate-Package.ps1`, `scripts/Test-Harness.ps1` boundary case `limit=9MB`, one-over refused). The
mission allowed up to 12 MiB; 9 MiB is the narrowest value that admits this assembly (8,457,216 bytes, about 8.07 MiB, at sprint3k;
8,463,872 bytes at sprint3l).

## Native runs (budget record `sprint3`, batches `sprint3:approach-1` to `-4`)

All runs: Working fixture only, `-BaseGameOnly` with the retained CotW/CMI staging, WhatIf development-scoped audit
PASS before each candidate, detached through WMI, exact restoration (Mods tree empty, no lock, no game process
afterwards). Engine Areas unpack residue from r01 is retained under
`runtime-backups\20261031-sprint3-r01-approach-rt\engine-areas-residue\` with its manifest.

| Run | Candidate (DLL) | Scenario | Outcome | Cause / change |
| --- | --- | --- | --- | --- |
| r01 | 3a `eaeff76f…` | rt | FAIL before rows | Child-entry preamble "parent identity missing" (the family rule did not list the new scenarios); engine `Areas` unpack outside the isolation; stale lock recovered by the profile recovery script |
| r02 | 3b `3bed8618…` | rt | 1/5 | Spells carried natively; evidence defects (rod charge accounting, scroll re-click recast, cross-row rolls); trap undiscovered on load |
| r03–r05 | 3b | rt | refused | Batch rule (third equivalent attempt) → batch `approach-2` |
| r06 | 3c `0c603a8a…` | rt | 3/5 | Friendly/held/ranged PASS; hostile legal miss kept the charge (row semantics corrected); every trap `perceptionPassed=false` after load (saved discovery does not survive) |
| r07 | 3d `3f0a1c25…` | rt | 4/6 | 4 spell rows PASS; trap legs ended 12 m from an 8 m radius; cleanup deadline from a 46 m walk-back |
| r08 | 3e `7d29909d…` | rt | 4/5 | 4 spell rows PASS, cleanup clean; trap leg settled 7.9 m from the trap, no native roll (rider 3D distance outside the radius; legs stopped short) |
| r09 | 3e | tb | leaf deadline | Delegated move never admitted on the rider's turn → `3980e1cf` |
| r10 | 3f `65ebe30c…` | tb | 1/2 | `S3-tb-touch-friendly` PASS natively; insufficient row's recipient reachable in one turn (15.7 m) |
| r11 | 3g `3d2e5728…` | tb | 1/2 | Native sequence correct (short on turn one, native approach-end rule, charge held, delivered on the next rider turn, one heal); the row re-entered its held-charge step a turn later and failed itself → `c63bd260` |
| r12 | 3h `eaf7059a…` | rt | 4/5 | 4 spell rows PASS; trap legs with gates recorded for the first time: the trap stayed in fog on every frame, no party member was ever moving inside the radius, every re-order settled at the same spot (the legs are cut off) |
| r13 | 3i `373ec2ec…` | rt | 4/5 | Every trap `reachable=false` by the native graph connectivity test (A* areas); no closed clickable door within 12 m of either settle |
| r14 | 3i | tb | native 2/2, reader refused | Both turn-based rows PASS natively (the insufficient-movement row complete: native approach-end rule, charge held across the turn, re-delivered on the next turn); the reader refused the friendly row because its recipient point lay on the cut-off island (3.7 m instead of 9 m) → `6f54bcf8` |
| r15 | 3j `0d96c040…` | rt | 4/5 | Obstacle inventory: mount node area 10, trap-room node area 13 (second trap: area 7); nearest door 12.3 m away, closed, unrevealed, not clickable; a `Secret_Door` map object 1.5 m from the settle (`InteractionSkillCheck`); no party member can reach any trap point. r15's first request was refused by the batch rule (two real-time failures in `approach-4`) and re-declared as `approach-5` |
| r16 | 3k `ad961c3d…` | rt | 4/5 | The secret passage is revealed and perception-passed but its native interaction refuses: `CanInteract()` false, skill `Unknown`, DC 0 (designer-scripted, not player-openable from this side); no ordinary input reaches the trap rooms |
| r17 | 3k | tb | 2/2, reader PASS | Both turn-based rows PASS end to end on the final candidate: friendly recipient at 9.0 m with 7.7 m of carried travel; insufficient row at 26.9 m against the 24.4 m allowance, native approach-end rule, charge held across the turn, delivered on the next rider turn; exact restoration |
| r18 | 3l `8abeffb9…` | rt-neighbor | 1/1, reader PASS | Review iteration, batch `sprint3:review-1`: the held-charge ownership neighbor on the corrected bytes; Stop release and carried re-delivery with own move restored, no successor, no yield; exact restoration |

## Outcomes

| Item | Disposition | Evidence |
| --- | --- | --- |
| SPELL-01 real time: Cure Light Wounds on a distant ally (12 m) | **PASS natively** | r08, r12, r13, r15, r16 `S3-touch-friendly-distant`: one ordinary ability click, the mount's carried approach to the sticky-touch radius, native cast, one held-touch delivery, one heal, memorized slot spent natively, no charge left |
| SPELL-01 real time: held charge after a Stop, second ordinary click | **PASS natively** | same runs, `S3-touch-held-charge`: the Stop releases the carried move and leaves the native command to the native Stop; the charge stays held; the second click resolves to the held touch (no recast, no second scroll charge) and delivers once |
| SPELL-01 real time: offensive touch on a hostile | **PASS natively** | `S3-touch-hostile`: native touch attack roll, one scroll charge; r06's legal miss kept the charge (the row records both outcomes) |
| SPELL-01 real time: ranged spell beyond range | **PASS natively** | `S3-ranged-distant`: Snowball from beyond its range; the mount approaches to the spell's own `GetApproachDistance`, cast and projectile attack roll at 10.2 m against a 10.0 m range |
| SPELL-01 turn-based: touch on a distant ally within one turn | **PASS natively** | r10, r11, r14, r17 `S3-tb-touch-friendly`: cast on the rider's turn, carried mount movement on that turn, one delivery, rider Standard cost events, native mount Move cooldown |
| SPELL-01 turn-based: insufficient movement | **PASS natively** | r14, r17 `S3-tb-insufficient-movement`: recipient placed 26.9 m away against a 24.4 m allowance; the carried approach ends through the native approach-end rule (`nativeApproachEndSignalled`), the held charge survives the turn boundary, a second ordinary click on the next rider turn resolves to the held touch and delivers once (r11 showed the same native sequence; only the row's bookkeeping failed it) |
| TRAP-01: mounted disarm of a discovered trap | **NOT DEMONSTRATED natively (fixture limit)** | The Working fixture area has four active traps; all four sit in navmesh graph areas disconnected from the party's (areas 13 and 7 against the party's 10); saved discovery does not survive the load; the only entrance near the first trap is a secret passage whose native interaction refuses (`CanInteract` false, no skill, DC 0); no closed clickable door within 12 m; every bounded exploration leg settled at the island edge with the trap in fog, so no native Perception roll was possible. The shared driver (the mount's delegated move to the command's own radius, the native command acting afterwards) is natively evidenced through the spell rows. The policy tests only choose the command category and drive the shared lifecycle; the trap-specific safe point and the native `TrapObjectView.Interact` Trickery check have neither native evidence nor offline execution (corrected after the review). |
| Unmounted controls | **Offline only** | The carrier refuses unmounted riders, the mount's and third units' commands (policy tests); no native unmounted trap/spell control row ran |
| Interruption / replacement | **Partial** | An admitted rider request that displaced the carried command releases it and a refused one releases nothing (review regressions, correction A); the Stop case is native (held-charge row); a door click during a carried approach is routed through the same replacement path as a ground move (source), not re-run natively |

TRAP-01 reading: the mechanism a trap needs (replace the native per-tick approach with the mount's delegated move to
the command's own radius, then let the native command act) is what SPELL-01 exercised natively in every run from r06
on; what remains unproven is the trap-specific target (the `OverrideTarget` safe point outside the trigger zone) and
the Trickery consequence. A fixture with a discovered, reachable trap is the required input (see Risks).

## Sprint 2 carryover (recorded once, not reopened)

- Turn-based door control was not demonstrated in Sprint 2; the `3980e1cf` admission predicate is the probable cause
  of the never-moving turn-based approach, unverified against a door.
- The owner's dungeon autosave `Auto_1121` was refused by the persistence authority's `Manual`-only pin (kept).
- The Working fixture's dwarf dungeon door `NotInCombat` state is unknown (inferred from a Chunk 6A refusal).
- The diagnostic DLL hit the 8 MiB package bound (raised to 9 MiB here, see above).

## Budget, launches, state

Budget record `runtime-state\workflow-budgets\sprint3.json`: 65.77 of 90 min across 48 entries: fifteen
native launches (r01, r02, r06 to r17; r03 to r05 and r15's first request were refused by the batch rule and not
charged), their WhatIf qualifications, and imported `kind=other` entries for the candidate packages, suite freezes and
reader reruns. Batches: `sprint3:approach-1` (r01, r02), `-2` (r06, r07), `-3` (r08, r09), `-4` (r10 to r14), `-5`
(r15 to r17). Each batch change followed a discriminating observation (preamble and fixture defects, trap discovery not
surviving the load, the perception gates, the graph connectivity test). Mission limits respected: at most five native
scenarios per batch, two equivalent failures before a changed experiment, one native executor at a time, no human save
loaded.

Live installation at closeout: Mods tree empty, loader parameters and profile root at intake, protected saves
(`KMC_AUTOMATION_BASELINE`, the owner's originals) unchanged, no lock, no game process, no candidate installed. The
Working fixture is the only save loaded; it was read-only copied for offline inspection and never written.

## Risks and open items

- **TRAP-01 native evidence is missing** (fixture limit, above). Required input: a disposable save (never a human
  save) on an area where an active, disarmable trap stands in the party's own graph area, 4 to 30 m from the mounted
  pair, either undiscovered (the row discovers it through the party's own Perception while walking inside its radius)
  or already discovered and revealed. The row, reader and registries are complete; only the fixture is missing.
  Candidates: a copy of `KMC_AUTOMATION_WORKING` taken after the party has opened the Lvl01 secret passage from its
  scripted side, or an owner-made save registered as a disposable copy (`Chunk7OwnerSeedRegistry.json`, type `Manual`).
- **Turn-based door (Sprint 2 carryover)**: `3980e1cf` admits derived `UnitMoveTo` commands on the rider's turn, which
  the turn-based door approach also needs; it was not re-run against a door.
- **General mounted interactions**: only traps (and Sprint 2 doors) are carried. A mounted click on a secret passage,
  chest or lever still meets the first causal boundary when it needs an approach (r16 never reached that point: the
  passage refused before any command). Widening the carrier to every `UnitInteractWithObject` is a small policy change
  on the same driver, left for a later sprint with its own native rows.
- **Fixture rank grants**: the trap row grants the disposable rider Trickery ranks (character level) and, only when the
  save's recorded rolls had consumed the pair's ranks, one Perception rank; both are restored at drain and bounded by
  the reader. The Perception grant never fired (`allowedBefore` true in r12 to r16).
- **Harness gap**: the shared tranche reports an unknown subscenario `phase3d-horse-tranche-cleanup-deadline` when the
  casting cleanup overruns (r07); it is a FAIL row without a reader rule. The trap row's return leg keeps the party
  inside the cleanup window since r08.
- **Diagnostic DLL size**: 8,463,872 bytes (about 8.07 MiB) at sprint3l against the 9 MiB bound; the next sizeable diagnostic needs a split assembly or a
  bound decision.
- **Launch wrapper text match**: the launcher's "development-scoped check PASS" line is not matched by an ASCII literal
  (non-ASCII hyphen and console wrapping); launches were issued by hand after reading the WhatIf verdict.
