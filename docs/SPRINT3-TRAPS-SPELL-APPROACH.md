# Sprint 3 — traps and spell target approach while mounted (2026-10-31)

**Disposition: SPRINT 3 ENGINEERING PARTIAL — READY FOR REVIEW; OWNER PLAY ACCEPTANCE SEPARATE.** SPELL-01 is
demonstrated natively in real time and in turn-based combat on the final candidate. TRAP-01 is implemented, proven
offline and shares the natively proven driver, but has no native evidence: every trap on the Working fixture sits in a
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
  rule. Replanning follows a moving target only beyond 0.75 m; a new rider request releases the carried command.
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

Offline: `tests/MountedCarriedApproachPolicyTests.cs` (7 tests) in the Fast tier (954 component tests, 149 source
validations, PASS); `scripts/Test-Sprint3ApproachReaders.ps1` (54 checks: positive envelopes, schema 47/dispatch/
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
mission allowed up to 12 MiB; 9 MiB is the narrowest value that admits this assembly (8.44 MiB).

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

## Outcomes

| Item | Disposition | Evidence |
| --- | --- | --- |
| SPELL-01 real time: Cure Light Wounds on a distant ally (12 m) | **PASS natively** | r08, r12, r13, r15, r16 `S3-touch-friendly-distant`: one ordinary ability click, the mount's carried approach to the sticky-touch radius, native cast, one held-touch delivery, one heal, memorized slot spent natively, no charge left |
| SPELL-01 real time: held charge after a Stop, second ordinary click | **PASS natively** | same runs, `S3-touch-held-charge`: the Stop releases the carried move and leaves the native command to the native Stop; the charge stays held; the second click resolves to the held touch (no recast, no second scroll charge) and delivers once |
| SPELL-01 real time: offensive touch on a hostile | **PASS natively** | `S3-touch-hostile`: native touch attack roll, one scroll charge; r06's legal miss kept the charge (the row records both outcomes) |
| SPELL-01 real time: ranged spell beyond range | **PASS natively** | `S3-ranged-distant`: Snowball from beyond its range; the mount approaches to the spell's own `GetApproachDistance`, cast and projectile attack roll at 10.2 m against a 10.0 m range |
| SPELL-01 turn-based: touch on a distant ally within one turn | **PASS natively** | r10, r11, r14, r17 `S3-tb-touch-friendly`: cast on the rider's turn, carried mount movement on that turn, one delivery, rider Standard cost events, native mount Move cooldown |
| SPELL-01 turn-based: insufficient movement | **PASS natively** | r14, r17 `S3-tb-insufficient-movement`: recipient placed 26.9 m away against a 24.4 m allowance; the carried approach ends through the native approach-end rule (`nativeApproachEndSignalled`), the held charge survives the turn boundary, a second ordinary click on the next rider turn resolves to the held touch and delivers once (r11 showed the same native sequence; only the row's bookkeeping failed it) |
| TRAP-01: mounted disarm of a discovered trap | **NOT DEMONSTRATED natively (fixture limit)** | The Working fixture area has four active traps; all four sit in navmesh graph areas disconnected from the party's (areas 13 and 7 against the party's 10); saved discovery does not survive the load; the only entrance near the first trap is a secret passage whose native interaction refuses (`CanInteract` false, no skill, DC 0); no closed clickable door within 12 m; every bounded exploration leg settled at the island edge with the trap in fog, so no native Perception roll was possible. The product path (click, carried approach to the native safe point, native `TrapObjectView.Interact` Trickery check) is proven offline by the policy tests, and its driver is the one proven natively for spells; the trap-specific safe-point branch and the native Trickery roll have no native evidence. |
| Unmounted controls | **Offline only** | The carrier refuses unmounted riders, the mount's and third units' commands (policy tests); no native unmounted trap/spell control row ran |
| Interruption / replacement | **Partial** | A new rider request releases the carried command (policy test); the Stop case is native (held-charge row); a door click during a carried approach is routed through the same replacement path as a ground move (source), not re-run natively |

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
- **Diagnostic DLL size**: 8.45 MiB against the 9 MiB bound; the next sizeable diagnostic needs a split assembly or a
  bound decision.
- **Launch wrapper text match**: the launcher's "development-scoped check PASS" line is not matched by an ASCII literal
  (non-ASCII hyphen and console wrapping); launches were issued by hand after reading the WhatIf verdict.
