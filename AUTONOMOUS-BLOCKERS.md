# Autonomous blockers

Status: BLOCKED — CRITICAL

Frozen F0 evidence/source commit: `fc7215481acf97ce1863eb1c75b3433889d2af7d`

Frozen F0 qualified package SHA-256: `2c47d7ad4e82942bd303ab848ab82b9163a76ba9db79d52aaf8bfb0f028d80fc`

Frozen F0 qualified DLL SHA-256 / MVID: `202701ed9232e4f5a4d5ff65c468c684d1ed1dd53e4a8030be93b960a7cd202a` / `a702808c-e8a0-4755-bc24-5ed4e945866a`

Safety-guard implementation commit: `29e594d6d053824a47ac22b9dd702aede1036031` on `codex/mounted-combat-feasibility`

## Evidence-epoch boundary

The completed `22 PASS / 0 attributable FAIL / 3 DEFER — EVIDENCED` row ledger is frozen F0 runtime evidence. F0 used Working SHA-256 `a5f7a7fb77f0465df1591360ecd1730e2c28215d83e27aafc70efef3110e6dc5`, length `718821`, ticks `639222573406912936`. It remains truthful historical evidence and is not retroactively rebound to a different archive.

F1 is descriptor-requalified at execution HEAD `15bbf0e2029ebc43d8bada48b83b4f55d43f8db0`, but it is not admitted for runtime. Cross-epoch comparison exposed unauthorized metadata changes to two valued non-KMC saves. No runtime scenario has executed against F1. Consequently the top-level status remains `BLOCKED — CRITICAL`, and the three missing mandatory rows retain their frozen F0 disposition.

## F1 descriptor requalification

Status: PASS

Transaction `fixture-requalification-20260814T1140026497594Z` committed at `qualifiedAtUtc` `2026-08-14T11:40:02.8457604+00:00`. The descriptor qualification record is SHA-256 `95e9a3c6203f11cfdd7e615849eb61ebc060131075f6a7f40771dcd7eeb68a7a`, length `1272`, ticks `639223044028647586`; transaction state is phase `committed`, SHA-256 `68fdc0f759e7f7daffaffaeb27996357e74c8f5861c5bf9345005edbefa66cce`, length `1707`, ticks `639223044029797599`. The durable prior-record backup is the exact F0 qualification, SHA-256 `c1e33c75004212039258d6d90ba20e43c37c50966c94e685dbad1ca0e653654f`, length `1272`.

The audit proved exactly Baseline 1 / Working 1 / near-match 0; exact internal names; distinct paths; Manual/v1 descriptors; and shared GameName `cvb`, GameId `d41185be-edc1-47c0-b9d5-e1d7a9c8e65f`, and Area `9d1278a2f599b2a4daab53abdfe88d2e`. Baseline is byte-for-byte unchanged at SHA-256 `c29d965c9ff5dc0f971659d9ae154877aa4a9a461ca220d1ce28e7c7fd9d2512`, length `686605`, ticks `639222474845172002`. Revised Working alone was descriptor-requalified at SHA-256 `5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6`, length `816452`, ticks `639222975870964407`; no other save was authorized.

The requalification transaction preserved its current before/after save inventory at digest `973f203e16aad29d7e4865bae6f81149d959a4ac179953c87781a06d1c71898d`, `271` files, `3178473776` bytes. The normal guard passed and the recovery `-WhatIf` passed in already-committed mode. No Kingmaker/Wrath/UMM process, active lock, sentinel, live KMC tree, non-restored transaction, or requalification debris remains. This is a descriptor-transaction PASS only; it does not clear cross-epoch runtime admission.

## Active P0 blocker: cross-epoch valued-save drift

Status: BLOCKED — CRITICAL

Frozen F0 inventory was digest `58a54974423316041e29d32b1093c5d6775dbd67fe2b37ffaea3c9ae27de19be`, `271` files, `3178063612` bytes. Current F1 inventory differs in exactly three entries and no others:

| Entry | Authorization | F0 length / ticks | Current F1 length / ticks |
|---|---|---|---|
| Working | Authorized | `718821` / `639222573406912936` | `816452` / `639222975870964407` |
| `Auto_1120` | Unauthorized | `204829` / `639222474628040540` | `333208` / `639222975467301685` |
| `Quick_438` | Unauthorized | `625411` / `639220694761623881` | `809565` / `639222975512345112` |

The Working delta is within the user's explicit replacement authorization. The `Auto_1120` and `Quick_438` deltas are not. The project does not infer who or what changed them, and the successful requalification pre/post equality proves only that the requalification transaction itself did not change the then-current inventory. F1 remains non-admitted for runtime. Do not package, run a runtime-scenario `-WhatIf`, publish, launch Kingmaker, restore either valued save, or mutate either valued save. User-owned resolution or explicit authority for both valued saves is required before runtime admission can be reconsidered.

## Hardened continuity guard and final real audit

Status: BLOCKED — CRITICAL

Local commit `29e594d6d053824a47ac22b9dd702aede1036031` hardens continuity admission in exactly four files: `scripts/runtime/RuntimeHarness.Common.ps1`, `scripts/runtime/Test-KmcFixtureGuard.ps1`, `scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1`, and `scripts/Test-Harness.ps1`. Their SHA-256 values are respectively `6605ef74d10a7a99ea1ec21550689a2fa3600ecde89354389acd5593bfa525ca`, `79129c6041fe55403b35750b563fa32d4f90c83e6f93f956053331af19e48647`, `3259ff4d6a41505d9faea3655cb79a87ee487b8e69f8ef72ab2505153a69bb81`, and `f44133f5f6e88ac2b028e609b0dbb69d03161b598e40fbf75b29015872e4563e`.

The frozen implementation passed AST parse `4/4` and the complete Release gate: source `21/0`, build PASS, component `112/0`, visual `12/0`, harness `132/0`, assembly-backed `69/0`, and `git diff --check` PASS. Two independent read-only audits returned GO. Those GO decisions cover guard correctness only; recovery never grants admission, and they do not convert F1 into a runtime-admitted fixture.

The final real standalone `AuditWorkingContinuity -WhatIf` pinned current qualification SHA-256 `95e9a3c6203f11cfdd7e615849eb61ebc060131075f6a7f40771dcd7eeb68a7a` and frozen F0 save-transaction authority SHA-256 `b25f80c799207657650cf29118078edc4685bcbb51e1609ba8b167cab13052e0`. It produced the expected safety FAIL: `Save write allowlist violation: Auto_1120.zks, Quick_438.zks`. Qualification and authority remained byte-identical; no Kingmaker process, active lock, sentinel, or live KMC tree remained. The audit was read-only and launched no runtime scenario.

## Cleared blocker: exact KMC fixture identity

Status: PASS

The frozen F0 canonical audit proved exactly one `KMC_AUTOMATION_BASELINE`, exactly one `KMC_AUTOMATION_WORKING`, and zero other KMC near-matches. The guard opened only those canonical archives and verified distinct non-linked direct-child paths, exact internal names, Manual/v1 descriptors, and shared campaign identity: GameName `cvb`, GameId `d41185be-edc1-47c0-b9d5-e1d7a9c8e65f`, Area `9d1278a2f599b2a4daab53abdfe88d2e`.

Baseline SHA-256 is `c29d965c9ff5dc0f971659d9ae154877aa4a9a461ca220d1ce28e7c7fd9d2512`; frozen F0 Working SHA-256 is `a5f7a7fb77f0465df1591360ecd1730e2c28215d83e27aafc70efef3110e6dc5`. Baseline is immutable and never a repair target. The sole writable allowlist identity is exact Working. Other-project, ordinary, auto, quick, and foreign saves were not admitted.

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

## Downstream blocker: mandatory §27.7 F1 runtime coverage

Status: BLOCKED — CRITICAL

The frozen F0 named-row ledger is `22 PASS / 0 attributable FAIL / 3 DEFER — EVIDENCED`. F1 descriptor requalification does not prove scene contents or any runtime row, and the P0 valued-save hold currently forbids execution. Doorway, selection, and formation remain mandatory and cannot be waived:

### Doorway control

Frozen F0 status: DEFER — EVIDENCED

Current F1 status: TODO

The F0 doorway attempt was contaminated by native combat after initial control/traversal observations. It is neither an attributable Architecture B failure nor a qualifying PASS. F1 must now run the exact same-Mammoth unmounted control and mounted comparison twice in fresh processes without weakening the matched-control requirement.

### Selection away/back

Frozen F0 status: DEFER — EVIDENCED

Current F1 status: TODO

F0 Working had no eligible directly controllable non-pair unit. F1 must prove selection away to an eligible third unit and back twice in fresh processes. Do not synthesize a unit, substitute the mount, or infer UI/portrait selection from camera-only evidence.

### Party formation

Frozen F0 status: DEFER — EVIDENCED

Current F1 status: TODO

F0 formation had the same missing third-unit prerequisite. F1 must issue a meaningful stock group movement command twice and prove the third unit's stock command/formation behavior and non-pair isolation. A pair-only command remains insufficient.

The revised project-owned Working descriptor is qualified and Baseline remains immutable, but F1 is not admitted for runtime. Strict A/B execution of these rows remains downstream work only after the valued-save safety hold is resolved.

## Evidence-backed visual finding

Status: PASS

Usable explicit-camera frames are classified `MECHANICALLY VIABLE, NEW ANIMATION/POSE WORK REQUIRED`. The rider is rigidly upright and the lower body intersects or is occluded by the Mammoth back. Some later-state frames are black/unusable. Camera-only frames do not prove UI selection, portrait state, selection-circle behavior, or camera follow.

This closes the Phase 1 visual-classification requirement without adding another deferred named row. No visual kill criterion fired, but pose/animation work must be separately scoped and authorized before combat expansion or any production-readiness claim.

## Runtime safety state

Status: BLOCKED — CRITICAL

The frozen F0 source/build gates are source `21/0`, component `112/0`, visual `12/0`, harness `105/0`, and assembly-backed `69/0`. Every F0 admitted live run restored exact Mods, then-authorized Working, protected-save metadata, process state, lock, sentinel, and deployed KMC state. F1 requalification preserved its current pre/post inventory and left no process or transaction residue, but cross-epoch comparison and the hardened real continuity audit both identify unauthorized changes to `Auto_1120` and `Quick_438`. Runtime safety admission therefore remains blocked even though no F1 scenario executed.

## Architecture and authorization state

Status: BLOCKED — CRITICAL

Static scores remain A/B/C/D = `41/66/77/89`. Architecture B is the provisional product-fit leader; C/D remain documented pivots and A remains rejected as too cross-cutting. No K1–K12 kill criterion fired. The three F0 mandatory deferrals and absence of F1 runtime results nevertheless prevent a truthful proceed or pivot completion status.

Phase 2 remains a draft only. Exact next action: HOLD without package, runtime-scenario `-WhatIf`, publication, or game launch while the user owns resolution or grants explicit authority for changed valued saves `Auto_1120` and `Quick_438`. The project must not restore or mutate them. Only after resolution may the pinned continuity audit and runtime-admission decision be repeated. Executing Phase 2 or beginning pose/animation work is not authorized.
