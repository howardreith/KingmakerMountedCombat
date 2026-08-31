# Phase 3D Unified Mounted Combat Implementation

Status: IN PROGRESS — production and runtime automation are offline-green; fresh guarded runtime qualification is pending

## Scope and identity

Phase 3D starts from accepted Phase 3C HEAD `63595b832f7f89c854edef5a9eb4d21dee026590` on `codex/mounted-combat-phase3d-unified-combat`. The production implementation checkpoint is `9bf83d21b5367e057130b41bf7379512a638c025`; the presentation checkpoint is `63d324d909ec333cd16cde5e942f419759faa664`. The runtime protocol/scenario checkpoint described here is not a runtime PASS until it is committed, guarded-published, packaged from clean HEAD, and exercised against the exact guarded Kingmaker fixture.

Current development version is `0.1.0-phase3d-dev.3`. The immutable dev.1 package failed before gameplay at nested registration admission. Dev.2 repaired that boundary, passed RT/TB WhatIf, and reached the Horse presentation precondition, where RT A stopped before the Phase 3D tranche because the Phase 3D pelvis-local lowering exceeded the unchanged right-stirrup gate. Dev.3 is the single evidence-backed Horse-coordinate repair described below; fresh clean identity and runtime evidence remain pending.

## First runtime checkpoint

Dev.1 package/manifest/DLL/MVID were `28406ec450f60cd6e426be7b87f9146a7f0609df9dc85f014b584ba7754752d9` / `3c262539e6c5276bfa1821c15132d7c1c7ecdc91f6eec963b6eec78f54345919` / `38df7b7b177589d6ea8fcdae9b47e59a885979790cb164a5fd2c10866bd84126` / `8dbda12f-ce20-40b2-88e9-49d63e8fd208`. Suite `20260830T230000Z-phase3d-dev1-unified-combat-suite1` passed RT/TB WhatIf and independent audits. RT A `20260830T233000Z-phase3d-dev1-rt-passA` is immutable uncredited `FAIL 3/1` at scenario start: the registration prerequisite rejected the Phase 3D name, so no Phase 3D artifact or gameplay row was produced. Mandatory restoration audit passed before evidence read. This result provides diagnostic attribution only.

## Implemented architecture

The implementation keeps the established boundaries rather than adding a synthetic actor:

- `UnifiedMountedTurnCoordinator` retains the rider as the real Kingmaker turn controller and initiative principal, skips only the exact active mount candidate, projects only the rider into `InitiativeTrackerVM`, and defers dismount split participation to a safe later round boundary.
- Rider and mount retain their own native cooldown/action ledgers. A natural rider-turn start prepares the mount ledger once; a mid-turn combat Mount merges the pair without clearing either ledger.
- `MountedCombatController` admits an exact player-created stock rider `UnitAttack` only after the native attack-request event, creates one bounded pair intent, gives physical approach to the mount, and dispatches native rider/mount attacks without a shared Standard pool.
- RTWP keeps the exact target intent through ordinary cooldown waits and successful attacks. Stop, ground click, a new target, target invalidation, lifecycle cleanup, or mode/turn termination cancels it, and duplicate-dispatch telemetry must remain zero.
- TB uses the same hostile-click path during the rider-led turn. It deterministically dispatches rider first and then the mount when each actor independently has a legal Standard action. Rider Primary and Mount Primary remain explicit single-actor alternatives.
- Ranged routing detects the exact native weapon blueprint rather than a bow allowlist. The mount approaches only to native rider range and line of sight; a ranged pair intent never adds an automatic Horse melee attack. Native `UnitAttack` remains responsible for ammunition, reload, attack rules, range, cover, concealment, and ranged AoO behavior.
- Combat Mount/Dismount requires the exact owned eligible adjacent pair, correct active rider turn, and available rider Move action. It charges only rider Move and preserves every already-spent rider/mount resource.
- Mounted five-foot movement uses Kingmaker's native five-foot mode and distance, transfers physical movement to the mount without charging ordinary Move, and suppresses disengage AoO only for the exact active mounted step. Ordinary mounted movement and every unmounted unit remain on the stock AoO path.
- The Phase 3C separate-turn behavior remains available behind `EnableUnifiedMountedTurn=false` as the fail-closed fallback required by the mission.

## Rider Primary P0

Phase 3C's `UnitMoveContiniously.Init` prefix treated a pair-member continuous-movement command as `UnexpectedCommand`, which could end the relationship while Rider Primary was selecting, approaching, or resolving. Phase 3D delegates rider continuous movement to the mount and cancels only a superseded stock-attack intent; it does not invoke relationship cleanup.

The activation ledger correlates a monotonic activation sequence with ability owner, selected unit, target mode, relationship state, pair/view identities, lifecycle deliveries, combat/mode transition, cleanup trigger, and terminal result. Fresh RT/TB hit, rejection, target-cancel, post-movement, and post-mode-transition scenarios must all retain the same relationship. Automatic remount is not used.

## Original presentation assets

The exact Kingmaker bundle inventory did not expose a safely reusable saddle/tack icon, so Phase 3D uses original KMC-owned artwork:

- `MountSaddleIcon.png`, 128x128, is used only for Mount/Dismount and communicates tack rather than Horse identity;
- `HorsePortraitSmall.png`, 185x242, is a tighter original close-up derived from the KMC-owned Horse master; large and medium Horse portraits remain unchanged;
- the accepted Horse procedural pelvis/leg pose remains local `Y=-0.17`; the final lowering is exact Horse mount-root-local `Y=-0.08`, applied after source-anchor resolution so animation-driven pelvis axes cannot turn a vertical product decision into a diagonal displacement;
- all Mammoth profile constants and resources remain unchanged.

The image-generation skill supplied the original KMC saddle master and the KMC-master-derived small portrait crop. No Wrath, Kingmaker, YouTube, screenshot, or third-party artwork is embedded or redistributed. Exact prompts, dimensions, rights boundary, and hashes are recorded in `planning/MOUNT-ICON-ASSET-AUDIT.md`.

Dev.2 runtime attribution is exact: `Pelvis.localPosition Y=-0.29` produced left/right stirrup distances `0.491643876/0.5325693` despite zero clamps and microunit solver residuals. The second value truthfully failed the existing `0.5` gate. Dev.3 restores the Phase 3C procedural pose and moves the owned Horse rider root by `(0,-0.08,0)` in the stable mount-root frame. Mammoth root offset is `(0,0,0)`. The gate is unchanged, and actual saddle contact/clipping remains manual review.

## Strict runtime tranche

`Phase3dHorseScenarioTranche` adds three guarded outer scenarios and one immutable `phase3d-horse-scenario-evidence.json` artifact:

- `phase3d-horse-presentation-suite` binds the exact asset dimensions, rider principal, and Horse pelvis profile;
- `phase3d-unified-combat-rt-suite` exercises real native Rider Primary target selection/cancel/rejection, persistent melee intent, ranged Shortbow approach/autofire/cancel, adjacent native ranged AoO, Light Crossbow and Sling observations, RT-to-TB-to-RT reconciliation, and unmounted melee/ranged controls;
- `phase3d-unified-combat-tb-suite` begins unmounted in combat, proves three combat-Mount resource states, exact tracker projection, explicit and stock pair attacks, ranged rider-only dispatch, mounted five-foot and ordinary-move AoO controls, combat Dismount, and an unmounted five-foot control.

The external validator requires the exact row set for each suite, reconciles every row with the final protocol result, rejects unknown/duplicate rows, rechecks immutable artifact bytes, and applies semantic ownership/resource/cardinality predicates. Synthetic protocol tests construct valid evidence for all three suites and prove one targeted mutation per suite is rejected.

## Qualification boundary

Offline compilation or synthetic protocol evidence does not prove Kingmaker control usability. Runtime credit requires a clean guarded package, stable-suite admission, separate mode `WhatIf` checks, independent zero-mutation audits, actual native input admission, physical movement/attack outcome, exact rule-event cardinality, and exact cleanup. Pointer feel, tracker rendering, portrait/icon readability, and final Horse seat/clipping remain focused human gates.

No Paladin Divine Steed, mounted spellcasting, mounted feats/charge, persistent relationship, automatic remount, additional mounts, Small riders, enemy riders, public release, or `main` merge is implemented or authorized.
