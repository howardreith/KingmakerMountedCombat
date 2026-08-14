# Phase 1 feasibility report

Status: BLOCKED — CRITICAL

## Outcome

Phase 1 has established that a Kingmaker movement-only mounted pair is mechanically feasible under the bounded Architecture B seam: the mount remains the sole pathfinding authority while KMC synchronizes the rider's logical and view state and owns reversible cleanup. The canonical fixture guard, exact Working load, lifecycle suites, five pair-only movement rows, five boundary rows, visual capture, and external-state restoration all produced qualified evidence.

The final named-row ledger is `22 PASS / 0 attributable FAIL / 3 DEFER — EVIDENCED`. The three deferrals are mandatory mission §27.7 rows: doorway control, selection, and party formation. The current fixture cannot provide a combat-neutral matched doorway control, and it contains no eligible directly controllable non-pair unit for the selection or formation rows. These are fixture/control limitations, not Architecture B failures, but they prevent the Phase 1 definition of done. Therefore neither `PHASE 1 COMPLETE — PROCEED RECOMMENDED` nor `PHASE 1 COMPLETE — PIVOT RECOMMENDED` is truthful.

Evidence is frozen at branch `codex/mounted-combat-feasibility`, commit `fc7215481acf97ce1863eb1c75b3433889d2af7d`. The qualified diagnostic package SHA-256 is `2c47d7ad4e82942bd303ab848ab82b9163a76ba9db79d52aaf8bfb0f028d80fc`; packaged DLL SHA-256 is `202701ed9232e4f5a4d5ff65c468c684d1ed1dd53e4a8030be93b960a7cd202a`, MVID `a702808c-e8a0-4755-bc24-5ed4e945866a`.

## Exact targets

- Kingmaker: Steam app `640820`, build `6757524`, product `2.1.7b`; `Assembly-CSharp.dll` SHA-256 `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`.
- Installed UMM: `0.28.2.0`, SHA-256 `75b96e25a3a9fbadb47dd14a4ab490cb8c98143a6242aff3bba6145cd3047f39`.
- Harmony12: `1.2.0.1`, SHA-256 `aa1cd48317254985d8b700cc74953477d1b40c3022ce9aa4c95ed2b8327e1292`.
- Kingmaker Unity core: SHA-256 `3a76df7f709d465e3273502e08edbffb536b1c2f78c3a132b8668e59fddd2803`, MVID `bd5ffe06-494e-4588-a068-c8443cc48c47`.
- Wrath read-only reference: Steam product `2.7.0x`; `Assembly-CSharp.dll` SHA-256 `2cb7160b7154d4ffacc77b9c51b1eb26199e1294300f04fdfc073367b2ef8953`, MVID `90a9869c-2792-4c7b-bfb7-5a8b33da7c82`.

Read-only source references: TabletopTweaks-Base `221decc02265c780685650ea987bca0a9eeaf49a` (MIT); AutoMount `59bd24fd49d9e4868e2b88d8d0019f21e6f69cf3` (MIT-form text with placeholder notice, no copying); DragonFixes `cf74881ebc2e3762bdab720275fc903d44377764` (MIT); pathfinder-wotr-multiplayer `6402b9ba9dd503dfb6054e595919fbd8992c4a21` (no local license, observation only); and harness snapshot source `c06793d2238577093b96a2dc3172839070e7d69a` (no included license, concepts only).

No reference source, Wrath assembly, or proprietary extracted asset is reused in production.

## Architecture finding

Wrath's mounted subsystem is cross-cutting engine behavior rather than a cosmetic attachment: reciprocal state, mount-authoritative commands, rider path suppression, avoidance ownership, entity/view synchronization, selection/formation projection, lifecycle cleanup, and persistence repair cooperate as one system. Kingmaker lacks those mounted types but exposes enough narrow primitives for the Phase 1 slice: exact pet/master and controllability checks, current size, stock mount pathfinding, rider `AgentOverride`, reference-counted avoidance, command-routing seams, selection/stop/cancel seams, lifecycle boundaries, and reversible view transforms.

Static weighted scores remain A/B/C/D = `41/66/77/89`. Architecture B is the provisional product-fit leader because live evidence now proves its one-mover movement, synchronization, cleanup, and guarded boundary seams without a broad engine replacement. A remains rejected as too cross-cutting; C and D remain bounded pivots if later evidence fires an existing kill criterion. No K1–K12 kill criterion fired. This is a provisional disposition, not authorization to execute Phase 2 while the mandatory rows remain deferred.

## Qualified runtime evidence

- Fixture qualification proved exactly one canonical Baseline, exactly one canonical Working, zero KMC near-matches, distinct non-linked paths, exact internal names, shared `GameId`/`GameName`/`Area`, immutable Baseline, and write authorization only for `KMC_AUTOMATION_WORKING`.
- The overlay fixture-intake pilot resolved the exact Medium rider and larger active Mammoth, both stock agents, and the live `Spine` anchor; it passed `3/3` subscenarios and `13/13` assertions.
- Lifecycle A/B runs `20260814T034950Z-lifecycle-suite-passA` and `20260814T035800Z-lifecycle-suite-passB` each passed `8/8` named rows and `339/0` assertions. Directly invoked lifecycle handlers prove their handler/service behavior and exact cleanup, not independent EventBus delivery.
- Pair-only movement repeated A/B with strict evidence and exact restoration: pause/unpause `49/0` each, destination-cancel `49/0` each, open-ground `47/0` each, stop/start `61/0` each, and turns/corners `74/0` each. The open-ground B endpoint retained `0.0033627902` margin inside the unchanged `1.25` endpoint gate.
- Every qualified movement row preserved one authoritative mover, bounded entity/view/anchor synchronization, no oscillation/repath/stuck/selection-loss violation, exact stopped-boundary closure, residue-free dismount, and unchanged external state.

### Boundary evidence

| Row | Evidence ID | Assertions | Truthful claim limit |
|---|---|---:|---|
| Turn-based entry | `20260814T090000Z-boundary-tb-pass` | `56/0` | `Direct HandleTurnBasedModeStateChanged(true) invocation only; native mode-event delivery was not exercised.` |
| Realtime entry | `20260814T091500Z-boundary-rt-pass` | `56/0` | `Direct HandleTurnBasedModeStateChanged(false) invocation only; native mode-event delivery was not exercised.` |
| Save safety | `20260814T093000Z-boundary-save-pass` | `59/0` | `Direct GuardBoundary(SaveRequested) service invocation only; stock SaveRoutine and serialization were not exercised.` |
| Load safety | `20260814T094500Z-boundary-load-pass` | `44/0` | `Real Game.LoadGame of the exact Working descriptor exercised the native LoadRoutine prefix; no UI load request was exercised.` |
| Area transition | `20260814T100000Z-boundary-area-pass` | `44/0` | `Direct OnAreaBeginUnloading cleanup was latched before real Game.ReloadArea; native area-event delivery was not independently observed or qualified.` |

The boundary aggregate is `259 PASS / 0 FAIL` assertions. Every individual run also passed strict request validation `31/0`, game-result validation `39/0`, and final-result validation `29/0`. Evidence proves exact pre-boundary Mounted authority, synchronous cleanup/restoration latches, authorization deltas, loading start/stop and callback where applicable, clean fresh-world identity, zero KMC movement-agent/attachment residue, unchanged Baseline, restored Working, exact Mods restoration, and no lingering process, lock, or sentinel.

## Presentation finding

Usable explicit-camera frames support the classification `MECHANICALLY VIABLE, NEW ANIMATION/POSE WORK REQUIRED`. The rider remains rigidly upright and the lower body intersects or is occluded by the Mammoth back. That does not fire K9, but it is not presentation-complete. Some later-state captures are black or otherwise unusable, and camera-only evidence does not prove portrait state, UI selection state, or camera-follow behavior.

## Remaining critical blocker

- Doorway: the available attempt was contaminated by native combat after initial control/traversal observations, so the current fixture does not provide the required combat-neutral matched Mammoth control.
- Selection: no eligible directly controllable non-pair unit exists for the mandatory away/back test.
- Formation: the same missing third controllable unit prevents a meaningful group-command/formation test.

Completing those rows requires a project-owned Working fixture with a combat-neutral doorway/control route and an eligible third directly controllable unit. Pose/animation work also requires an explicitly scoped next mission. Until the three mandatory rows are proven or fail attributable criteria, Phase 1 remains `BLOCKED — CRITICAL` and Phase 2 remains unauthorized.
