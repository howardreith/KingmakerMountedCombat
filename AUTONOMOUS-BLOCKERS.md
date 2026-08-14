# Autonomous blockers

Status: BLOCKED — CRITICAL

Evidence/source commit: `fc7215481acf97ce1863eb1c75b3433889d2af7d`

Qualified package SHA-256: `2c47d7ad4e82942bd303ab848ab82b9163a76ba9db79d52aaf8bfb0f028d80fc`

Qualified DLL SHA-256 / MVID: `202701ed9232e4f5a4d5ff65c468c684d1ed1dd53e4a8030be93b960a7cd202a` / `a702808c-e8a0-4755-bc24-5ed4e945866a`

## Cleared blocker: exact KMC fixture identity

Status: PASS

The canonical audit proved exactly one `KMC_AUTOMATION_BASELINE`, exactly one `KMC_AUTOMATION_WORKING`, and zero other KMC near-matches. The guard opened only those canonical archives and verified distinct non-linked direct-child paths, exact internal names, Manual/v1 descriptors, and shared campaign identity: GameName `cvb`, GameId `d41185be-edc1-47c0-b9d5-e1d7a9c8e65f`, Area `9d1278a2f599b2a4daab53abdfe88d2e`.

Baseline SHA-256 is `c29d965c9ff5dc0f971659d9ae154877aa4a9a461ca220d1ce28e7c7fd9d2512`; qualified Working SHA-256 is `a5f7a7fb77f0465df1591360ecd1730e2c28215d83e27aafc70efef3110e6dc5`. Baseline is immutable and never a repair target. The sole writable allowlist identity is exact Working. Other-project, ordinary, auto, quick, and foreign saves were not admitted.

## Cleared blocker: fixture load context and pair availability

Status: PASS

Early direct and native-bootstrap load attempts truthfully failed in `Player.PostLoad` because package-only staging omitted the fixture's required live mod context. Both failures restored external state exactly and received bounded regressions. The schema-3 transaction now creates a byte-verified disposable clone of the exact live Mods context and overlays only KMC.

The guarded overlay fixture-intake run `20260813T233100Z-fixture-intake-overlay-pilot` loaded exact Working, verified campaign/area identity, and resolved exactly one valid Medium rider with the larger active Mammoth, enabled stock agents, and exact `Spine` anchor. It passed `3/3` subscenarios and `13/13` assertions. This cleared pair availability without weakening standalone package isolation; the prior two KMC-only no-save smokes remain the package's independent-load evidence.

## Cleared blocker: pair-only lifecycle, movement, and boundaries

Status: PASS

- Lifecycle A/B (`20260814T034950Z-lifecycle-suite-passA`, `20260814T035800Z-lifecycle-suite-passB`) each passed `8/8` rows and `339/0` assertions with exact cleanup/restoration. Direct handler/service calls do not claim independent native EventBus delivery.
- Pair-only movement A/B passed pause/unpause `49/0`, destination cancel `49/0`, open ground `47/0`, stop/start `61/0`, and turns/corners `74/0` per run. The mount remained authoritative, synchronization stayed within unchanged gates, and cleanup was residue-free.
- Boundary runs passed: TB `20260814T090000Z-boundary-tb-pass` `56/0`; realtime `20260814T091500Z-boundary-rt-pass` `56/0`; save `20260814T093000Z-boundary-save-pass` `59/0`; load `20260814T094500Z-boundary-load-pass` `44/0`; area `20260814T100000Z-boundary-area-pass` `44/0`.
- Boundary aggregate is `259 PASS / 0 FAIL`. Each run also passed strict request `31/0`, game `39/0`, and final `29/0` validation and exact Mods/save restoration.

The exact claim limits are: `Direct HandleTurnBasedModeStateChanged(true) invocation only; native mode-event delivery was not exercised.`; `Direct HandleTurnBasedModeStateChanged(false) invocation only; native mode-event delivery was not exercised.`; `Direct GuardBoundary(SaveRequested) service invocation only; stock SaveRoutine and serialization were not exercised.`; `Real Game.LoadGame of the exact Working descriptor exercised the native LoadRoutine prefix; no UI load request was exercised.`; and `Direct OnAreaBeginUnloading cleanup was latched before real Game.ReloadArea; native area-event delivery was not independently observed or qualified.`

## Active critical blocker: mandatory §27.7 fixture/control coverage

Status: BLOCKED — CRITICAL

The final named-row ledger is `22 PASS / 0 attributable FAIL / 3 DEFER — EVIDENCED`. The remaining rows are mandatory and cannot be waived:

### Doorway control

Status: DEFER — EVIDENCED

The available doorway attempt was contaminated by native combat after initial control/traversal observations. The current fixture therefore cannot provide the required combat-neutral same-Mammoth unmounted control and mounted comparison. The attempt is neither an attributable Architecture B failure nor a qualifying PASS. Do not rerun it unchanged and do not weaken the matched-control requirement.

### Selection away/back

Status: DEFER — EVIDENCED

The current Working fixture has no eligible directly controllable non-pair unit. The mandatory away/back selection test cannot execute honestly. Do not synthesize a third unit, substitute the mount, or infer UI/portrait selection from camera-only evidence.

### Party formation

Status: DEFER — EVIDENCED

Formation has the same missing third-unit prerequisite. A pair-only command cannot prove that KMC preserves an uninvolved party member's stock formation target and command identity.

Unblocking requires a project-owned Working fixture with both a combat-neutral doorway/control route and one eligible directly controllable non-pair unit. Baseline remains immutable. The canonical audit and descriptor/authorization guard must rerun before the revised Working fixture is opened, followed by strict A/B execution of only these three missing rows.

## Evidence-backed visual finding

Status: PASS

Usable explicit-camera frames are classified `MECHANICALLY VIABLE, NEW ANIMATION/POSE WORK REQUIRED`. The rider is rigidly upright and the lower body intersects or is occluded by the Mammoth back. Some later-state frames are black/unusable. Camera-only frames do not prove UI selection, portrait state, selection-circle behavior, or camera follow.

This closes the Phase 1 visual-classification requirement without adding another deferred named row. No visual kill criterion fired, but pose/animation work must be separately scoped and authorized before combat expansion or any production-readiness claim.

## Runtime safety state

Status: PASS

The final source/build gates are source `21/0`, component `112/0`, visual `12/0`, harness `105/0`, and assembly-backed `69/0`. Every admitted live run restored exact Mods, authorized Working, protected-save metadata, process state, lock, sentinel, and deployed KMC state. No Kingmaker, Wrath, or UMM process and no active KMC transaction residue remains at closure.

## Architecture and authorization state

Status: BLOCKED — CRITICAL

Static scores remain A/B/C/D = `41/66/77/89`. Architecture B is the provisional product-fit leader; C/D remain documented pivots and A remains rejected as too cross-cutting. No K1–K12 kill criterion fired. The three mandatory deferrals nevertheless prevent a truthful proceed or pivot completion status.

Phase 2 remains a draft only. Executing it, preparing a new fixture, or beginning pose/animation work requires the applicable new authorization; public release requires separate explicit authorization.
