# Phase 3D Unified Mounted Combat Implementation

Status: IN PROGRESS — production and runtime automation are offline-green; fresh guarded runtime qualification is pending

## Scope and identity

Phase 3D starts from accepted Phase 3C HEAD `63595b832f7f89c854edef5a9eb4d21dee026590` on `codex/mounted-combat-phase3d-unified-combat`. The production implementation checkpoint is `9bf83d21b5367e057130b41bf7379512a638c025`; the presentation checkpoint is `63d324d909ec333cd16cde5e942f419759faa664`. The runtime protocol/scenario checkpoint described here is not a runtime PASS until it is committed, guarded-published, packaged from clean HEAD, and exercised against the exact guarded Kingmaker fixture.

Current development version is `0.1.0-phase3d-dev.6`. The immutable dev.1 package failed before gameplay at nested registration admission. Dev.2 repaired that boundary and exposed the Horse presentation precondition. Dev.3 repaired the Horse coordinate boundary, then its first RT process admitted the exact native Rider Primary cast request while still on the first combat-entry frame and never reached ability delivery. Dev.4 repaired that native-shell boundary and proved exact delivery, but exposed an unrelated optional-telemetry reflection fault before the child attack could start. Dev.5 repaired that observer and proved one successful child attack with pair retention, then exposed an independent scalar-evidence serialization defect. Dev.6 is the bounded repair for that Phase 3D artifact seam.

## First runtime checkpoint

Dev.1 package/manifest/DLL/MVID were `28406ec450f60cd6e426be7b87f9146a7f0609df9dc85f014b584ba7754752d9` / `3c262539e6c5276bfa1821c15132d7c1c7ecdc91f6eec963b6eec78f54345919` / `38df7b7b177589d6ea8fcdae9b47e59a885979790cb164a5fd2c10866bd84126` / `8dbda12f-ce20-40b2-88e9-49d63e8fd208`. Suite `20260830T230000Z-phase3d-dev1-unified-combat-suite1` passed RT/TB WhatIf and independent audits. RT A `20260830T233000Z-phase3d-dev1-rt-passA` is immutable uncredited `FAIL 3/1` at scenario start: the registration prerequisite rejected the Phase 3D name, so no Phase 3D artifact or gameplay row was produced. Mandatory restoration audit passed before evidence read. This result provides diagnostic attribution only.

## Dev.3 RT attribution and dev.4 bounded repair

The clean guarded-published dev.3 package has ZIP/manifest/DLL SHA-256 `70d6e373464ba805c31d9e08db6dd9f241918fdacd3ecfed296915167981020a` / `8fcf83cc33ceed6c2f4e9e87f5c86fe69bb451e03903cc52ed52efc0283640a4` / `80b20bf02c836946dbd24a44890a91a1e970ff0811d00080b42fbac8e29a0d77`, MVID `3b980b66-be62-4397-9a1a-b1ce5292d9e8`. Separate RT/TB WhatIf and independent audit passed. Its RT A run admitted one exact hostile Rider Primary selection and cast request with zero native refusal, while cancel and invalid-target controls retained the pair. No dispatch, child command, attack, or cleanup followed; the pair remained mounted until the exact leaf deadline. The run and its postrun restoration are immutable, but its gameplay result is FAIL and uncredited.

Exact installed assembly inspection attributes two contributing boundaries. The diagnostic clicked before it had proved all native RT `ShouldStartCommand` gates (`0x0600911F`). Independently, `UnitUseAbility.Init` (`0x06002728`) initialized the custom intent shell with `NeedLoS=true`, permitting the shell to seek pre-dispatch approach through the disabled rider mover even though KMC's child transaction owns mount movement and final attack LoS. Exact `UnitActionController.UpdateCooldowns` (`0x06009120`) also maps an acted RTWP `Free` shell to rider Move cooldown; leaving that stock asymmetry in place would make one explicit primary consume both the shell's rider Move and the child actor's Standard.

Dev.4 therefore waits for two stable frames of exact rider/Horse action, initiative, command, hands, equipment, selection, target, and transaction readiness. An exact-token `UnitUseAbility.Init` postfix then sets `NeedLoS=false` and `IgnoreCooldown=true` only for the reference-identical KMC Rider Primary/Mount Primary shell cast by the exact active rider in the exact mounted relationship. Mount, Dismount, foreign abilities, wrong casters, and unmounted state are untouched. The shell remains a real native Free input command but owns no action resource; the actor-owned child `UnitAttack` is the sole Standard owner and still applies native range/LoS/weapon/rule behavior. Fresh clean-package runtime evidence is mandatory before this repair receives qualification credit.

## Dev.4 delivery proof and dev.5 observer repair

Clean guarded-published dev.4 commit `9a26f5103c0e9a1692af819e93640c91c806b5dc` produced package/manifest/DLL SHA-256 `ce29d2a3c15f6a640642893d47e4dabcbfe16388b2dbcdcc7b182dc81a5ed695` / `f26b31f2735ba0d6ce464f38cd060fd8c48dbb800df8b8155edb8d7b123f164d` / `49e0d0e22a9cccd2747d2764451b4a2d9ac128cb43dddde91a5c1e7bed9c14a1`, MVID `5c5d523a-3c48-4129-9cd0-0cff0d9c53e8`. Stable suite `20260831T132000Z-phase3d-dev4-unified-combat-suite4`, snapshot SHA-256 `cd4edee8a810a891d3d917b2a43f11723a9baf937106a374899b53b83d3d8636`, passed distinct RT/TB WhatIf and an independent presentation-path zero-mutation audit.

RT A `20260831T161500Z-phase3d-dev4-rt-passA` is immutable uncredited `FAIL`. Its mandatory independent postrun audit passed before evidence inspection. Exact native evidence proves the dev.4 repair succeeded at its boundary: the hostile Rider Primary click produced one selection start/end, one cast request, one shell preparation, no refusal, a rider-owned Free-slot shell with `NeedLoS=false`, `IgnoreCooldown=true`, no cooldown, `CanStart=true`, and terminal `started=true`, `acted=true`, `finished=true`, `Result=Success`. The service then recorded `DispatchStarted`, primary delivery, an accepted KMC RiderMelee command, and `DispatchCompleted`; the relationship remained mounted through that activation.

The child failed in `MountedPairAttackCommand.CreateAndValidateChildAttack` before native attack start. Optional ammunition/reload description called `Type.GetProperty(name)` on Kingmaker's inherited weapon surface; the runtime threw `AmbiguousMatchException` before the method's getter-only catch. This observation-only fault then triggered the legitimate exception cleanup path. Dev.5 replaces that ambiguous lookup with `OptionalPublicPropertyReader`: it walks public declarations from the most-derived type to its base types, ignores indexers, catches both discovery and getter failures, and truthfully returns unavailable when foreign/dynamic state cannot be read. It cannot select a weapon, admit a command, alter range/LoS, consume an action, or change a rule event. A deterministic regression reproduces hidden inherited property names, requires the most-derived readable value, proves fallback after a throwing getter, and proves absent values remain absent.

Clean guarded-published dev.5 commit `10d1d3ef17ed032d35dabed51cb28d63076de304` produced package/manifest/DLL SHA-256 `cffd518a9c81386631024e3065067a0e0df376945cb5895f2c5041990ad970b4` / `dcdbf69ed7fb212fc3b5da56b99c23a4f6177d187974fa5fc32c1296badc0bfc` / `7e3d6f5ba3cf445a3f809e15fb57e6927c0d8d2f913de8149e1f7b5e6ef2cd8c`, MVID `b22d25e6-1197-4a87-a1e7-b26e8f82fca5`. Stable suite `20260831T181500Z-phase3d-dev5-unified-combat-suite5`, snapshot SHA-256 `8dee138522f9d4a6463f4f9afd88d0f6906a486b074c66aa4868db83439314ab`, passed separate RT/TB WhatIf and an independent presentation-path zero-mutation audit.

RT A `20260831T212000Z-phase3d-dev5-rt-passA` is immutable uncredited `FAIL`; mandatory independent postrun audit `20260831T220000Z-phase3d-dev5-rt-passA-postrun-audit` passed before evidence inspection. The live command prepared/delivered one exact native Rider Primary, accepted RiderMelee once, completed `Success` with one child attack and zero repaths, emitted no relationship-end activation record, and remained `Mounted` through command terminal. The subsequent diagnostic `CaptureOutcome` called `JObject.FromObject` on `CapturePresentationObservation()`, whose declared and actual contract is scalar `string`; Newtonsoft correctly rejected a scalar where `JObject` was required. Dev.6 preserves that scalar directly as a `JValue` and wraps it only where row evidence requires an object. Source regression forbids all three erroneous scalar-to-`JObject` sites. No gameplay, action, turn, movement, rule, lifecycle, or presentation behavior changes.

The complete dev.6 offline gate passes source `22/0`, Release, component `294/0`, visual/source-order `18/0`, harness/protocol `237/0`, assembly contracts `380/0` (`356` Kingmaker + `24` exact Wrath), PowerShell parser `26/0`, JSON parser `7/0`, diff, and prohibited-payload validation. Pre-commit DLL SHA-256/MVID are `115b6e9f88bb0a7a12be6bfb11a6e1f111131771f7eff1395262e9bf29ee266f` / `aaee9cb1-59f9-4a6b-999c-43424dbf2a2e`; clean-HEAD package binding and fresh runtime evidence remain mandatory.

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
