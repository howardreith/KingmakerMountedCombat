# Runtime scenario matrix

Status: IN PROGRESS

The guarded F1 descriptor and protected-save authority gates are PASS. The exact audit reports one canonical `KMC_AUTOMATION_BASELINE`, one canonical `KMC_AUTOMATION_WORKING`, and zero KMC-looking near-matches. It verified distinct paths, exact internal names, shared campaign/area identity, immutable Baseline, and Working-only write authorization. The user-attested Auto/Quick transition is now an exact protected baseline, never project write authority. No runtime scenario has executed against F1.

Frozen F0 offline gate ledger: source validation 21 PASS / 0 FAIL; Release build PASS; pure/component 112 PASS / 0 FAIL; visual-capture contracts 12 PASS / 0 FAIL; guarded harness/protocol 105 PASS / 0 FAIL; assembly-backed 69 PASS / 0 FAIL (Kingmaker 58, Wrath 11).

Protected-save authority implementation commit `6abc293f12bacbc250fc4c012fbced05b3763881` is on `codex/mounted-combat-feasibility`. It changes `scripts/runtime/RuntimeHarness.Common.ps1`, adds `scripts/runtime/New-KmcProtectedSaveContinuityAuthority.ps1`, and changes `scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1` plus `scripts/Test-Harness.ps1`; SHA-256 values are respectively `59e24b0156f3f8c44ebd99c7bb403952108a7cef505bf55d633ca173f47d0ee1`, `9a5b983a7863b54457bbb4b869a5346b8d41d5744f901536ded647fb8127b2f3`, `b425fce49d329ccea6cba04dd423c242a656d4aff15ed63234f248c559db16d4`, and `cd6f3aa14e5c08211cc39d6e51fe86e304140993e137a5b8ed2c66fd1a41cf8`. Gates passed AST `4/4`, source `21/0`, Release build, component `112/0`, visual `12/0`, harness `134/0`, assembly-backed `69/0`, and diff check. This validates the admission guard and supplies no F1 runtime row.

## Fixture evidence epochs

F0 is the frozen runtime epoch at commit `fc7215481acf97ce1863eb1c75b3433889d2af7d`. It used Working SHA-256 `a5f7a7fb77f0465df1591360ecd1730e2c28215d83e27aafc70efef3110e6dc5`, length `718821`, ticks `639222573406912936`; all executed evidence below belongs to F0 unless explicitly stated otherwise. Its named-row ledger remains `22 PASS / 0 attributable FAIL / 3 DEFER — EVIDENCED`.

F1 is descriptor-requalified and admitted for the six missing Phase 1 processes. Transaction `fixture-requalification-20260814T1140026497594Z` committed at `2026-08-14T11:40:02.8457604+00:00`. Qualification SHA-256/length/ticks are `95e9a3c6203f11cfdd7e615849eb61ebc060131075f6a7f40771dcd7eeb68a7a` / `1272` / `639223044028647586`; phase-`committed` state SHA-256/length/ticks are `68fdc0f759e7f7daffaffaeb27996357e74c8f5861c5bf9345005edbefa66cce` / `1707` / `639223044029797599`. The exact F0 qualification backup is SHA-256 `c1e33c75004212039258d6d90ba20e43c37c50966c94e685dbad1ca0e653654f`, length `1272`.

F1 retains unchanged Baseline SHA-256/length/ticks `c29d965c9ff5dc0f971659d9ae154877aa4a9a461ca220d1ce28e7c7fd9d2512` / `686605` / `639222474845172002` and descriptor-qualifies only revised Working `5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6` / `816452` / `639222975870964407`. The 1/1/0 audit, exact names, distinct paths, Manual/v1 descriptors, and shared `cvb` / `d41185be-edc1-47c0-b9d5-e1d7a9c8e65f` / `9d1278a2f599b2a4daab53abdfe88d2e` identity passed. Working alone is writable. The requalification transaction preserved its current before/after inventory at digest `973f203e16aad29d7e4865bae6f81149d959a4ac179953c87781a06d1c71898d`, `271` files, `3178473776` bytes. Normal guard and recovery `-WhatIf` passed, with no process, lock, sentinel, live KMC tree, or transaction debris.

Frozen F0 inventory was digest `58a54974423316041e29d32b1093c5d6775dbd67fe2b37ffaea3c9ae27de19be`, `271` files, `3178063612` bytes. Current F1 differs in exactly three entries and no others:

| Entry | Authorization | F0 length / ticks | Current F1 length / ticks |
|---|---|---|---|
| Working | Authorized | `718821` / `639222573406912936` | `816452` / `639222975870964407` |
| `Auto_1120` | User-authorized protected baseline only | `204829` / `639222474628040540` | `333208` / `639222975467301685` |
| `Quick_438` | User-authorized protected baseline only | `625411` / `639220694761623881` | `809565` / `639222975512345112` |

The user attested that Auto and Quick changed during the external Kingmaker fixture-preparation session, not in a KMC transaction. Their current exact bytes and metadata are protected baselines only. The project must not restore or mutate either save.

The append-only authority is `C:\Dev\KingmakerMountedCombatLab\runtime-state\protected-save-authorities\20260814T1445257441387Z-user-fixture-preparation.json`, epoch `20260814T1445257441387Z-user-fixture-preparation`, SHA-256 `77fd415d177423dd7da58e3685e418f38cf954dead75068424a717e2be6b3ca9`, length `119319`, ticks `639223155954515237`. It binds current inventory digest `973f203e16aad29d7e4865bae6f81149d959a4ac179953c87781a06d1c71898d`, Auto SHA-256 `9599e9e15bdd04cde7b15d47795510b95cd8e1333f107f2e3b64469ebf3330fc`, and Quick SHA-256 `e26dc2ce00e6ed68953c2c9b1e89584ae3c3bd50f4cf698ecc2c624e21d10575`. Creation WhatIf, append-only actual creation, and independent continuity validation passed with no save mutation or residue.

## Executed F0 runtime evidence

| Evidence group | Result | Exact evidence |
|---|---|---|
| `mod-load-smoke` | PASS twice in consecutive fresh processes | `20260813T185500Z-mod-load-smoke-passA` and `20260813T185700Z-mod-load-smoke-passB`; same commit/package, zero save/load requests, exact restoration |
| Fixture intake and forensic rows | PASS 3/3 rows, 13/0 assertions | `20260813T233100Z-fixture-intake-overlay-pilot`: `export-mounted-contracts` 3/0, `export-candidate-mount-rigs` 4/0, `observe-mount-diagnostic-availability` 6/0; exact Medium rider and larger active Mammoth resolved |
| Lifecycle pass A | PASS 8/8 rows, 339/0 assertions | `20260814T034950Z-lifecycle-suite-passA` |
| Lifecycle pass B | PASS 8/8 rows, 339/0 assertions | `20260814T035800Z-lifecycle-suite-passB`; same commit/package as A |
| Pause/unpause A/B | PASS 1/1 row and 49/0 assertions in each process | `20260814T053000Z-pause-passA-qualified` and `20260814T054800Z-pause-passB-qualified` |
| Destination-cancel A/B | PASS 1/1 row and 49/0 assertions in each process | `20260814T060500Z-cancel-passA-qualified` and `20260814T062200Z-cancel-passB-qualified` |
| Open-ground A/B | PASS 1/1 row and 47/0 assertions in each process | `20260814T064000Z-open-ground-passA-qualified` and `20260814T065700Z-open-ground-passB-qualified` |
| Stop/start A/B | PASS 1/1 row and 61/0 assertions in each process | `20260814T073500Z-stop-start-recovery-passA` and `20260814T075000Z-stop-start-recovery-passB` |
| Turns/corners A/B | PASS 1/1 row and 74/0 assertions in each process | `20260814T081500Z-turns-corners-passA` and `20260814T083000Z-turns-corners-passB` |
| Doorway control | DEFER — EVIDENCED | `20260814T045500Z-movement-suite-corrected-passA`: unmounted Mammoth completed 2/2 endpoints at maximum `1.1952427374 < 1.25`; mounted reverse path was accepted and stable before native combat triggered residue-free `CombatStarted` cleanup |
| Selection / formation availability | DEFER — EVIDENCED | `20260814T051000Z-selection-passA` proved the fixture has no eligible directly controllable non-pair; formation was not launched merely to reproduce the same missing prerequisite |
| Turn-based boundary | PASS 1/1 row, 56/0 assertions | `20260814T090000Z-boundary-tb-pass`; direct lifecycle-handler cleanup only, `nativeDeliveryObserved=false` |
| Real-time boundary | PASS 1/1 row, 56/0 assertions | `20260814T091500Z-boundary-rt-pass`; direct lifecycle-handler cleanup only, `nativeDeliveryObserved=false` |
| Save boundary | PASS 1/1 row, 59/0 assertions | `20260814T093000Z-boundary-save-pass`; direct `GuardBoundary(SaveRequested)` cleanup, no stock `SaveRoutine`, save write, serialization, or save-round-trip claim |
| Load boundary | PASS 1/1 row, 44/0 assertions | `20260814T094500Z-boundary-load-pass`; real exact-Working `Game.LoadGame` plus native `LoadRoutine`-prefix authorization, observed loading/callback/fresh-world cleanup; no UI-load claim |
| Area boundary | PASS 1/1 row, 44/0 assertions | `20260814T100000Z-boundary-area-pass`; direct `OnAreaBeginUnloading` cleanup latch before real `ReloadArea(AutoSaveMode.None)`; native area-event delivery not independently qualified |

The five boundary runs use F0 commit `fc7215481acf97ce1863eb1c75b3433889d2af7d`, DLL SHA-256 `202701ed9232e4f5a4d5ff65c468c684d1ed1dd53e4a8030be93b960a7cd202a`, and MVID `a702808c-e8a0-4755-bc24-5ed4e945866a`. Each has strict request/game/final and boundary-JSONL validation, exact F0 Working identity continuity, terminal pre-restoration byte verification, and exact external restoration.

Historical engineering FAIL results remain preserved. They include fixture bootstrap, wrapper/restoration, endpoint-accounting, camera-capture, pause-validator, and stop/start recovery-oracle defects, plus the deterministic fixture-availability results. Each attributable harness/instrumentation defect has a regression. None is reclassified as a gameplay PASS, and none fired an architecture kill criterion.

## Frozen F0 required-scenario disposition

| Group | Scenario | Status | Qualified claim / exact limitation |
|---|---|---|---|
| Scaffold | `mod-load-smoke` | PASS | Two same-commit fresh-process passes with no-save identity and exact restoration |
| Forensics | `export-mounted-contracts` | PASS | Fixture-intake subscenario plus exact contract maps and assembly checks |
| Forensics | `export-candidate-mount-rigs` | PASS | Fixture-intake subscenario plus exact native metadata inventory |
| Forensics | `observe-mount-diagnostic-availability` | PASS | Exact Medium rider and larger active Mammoth pair available |
| Lifecycle | `mounted-pair-create-and-clear` | PASS | Exact state transition and zero movement/view/selection residue, A/B |
| Lifecycle | `mounted-pair-double-mount-rejected` | PASS | Exact rejection and unchanged pair state, A/B |
| Lifecycle | `mounted-pair-invalid-pair-rejected` | PASS | Side-effect-free validation rejection, A/B |
| Lifecycle | `mounted-pair-cleanup-idempotent` | PASS | Repeated cleanup and zero residue, A/B |
| Lifecycle | `mounted-pair-death-cleanup` | PASS | Direct lifecycle-handler cleanup, A/B; native EventBus delivery not claimed |
| Lifecycle | `mounted-pair-combat-start-cleanup` | PASS | Direct lifecycle-handler cleanup, A/B; native combat delivery was separately observed during contaminated traversal |
| Lifecycle | `mounted-pair-area-unload-cleanup` | PASS | Direct lifecycle-handler cleanup, A/B; native area-event delivery not claimed |
| Lifecycle | `mounted-pair-mod-disable-cleanup` | PASS | Direct relationship-service cleanup, A/B; native UMM callback delivery not claimed |
| Movement | `mounted-pair-open-ground` | PASS | A/B; one mover, no oscillation, fixed `<= 0.10` synchronization gates, residue-free cleanup |
| Movement | `mounted-pair-stop-start` | PASS | A/B; exact zero stopped drift, distinct restart path, fixed synchronization/recovery gates |
| Movement | `mounted-pair-turns-and-corners` | PASS | A/B; 3/3 endpoints and substantial direction change, zero instability |
| Movement | `mounted-pair-doorway` | DEFER — EVIDENCED | F0 valid unmounted Mammoth control and stable mounted entry; native hostile contamination prevented corrected repeatability |
| Movement | `mounted-pair-selection` | DEFER — EVIDENCED | F0 had no eligible directly controllable non-pair, so required away/back could not run |
| Movement | `mounted-pair-party-formation` | DEFER — EVIDENCED | F0 had the same missing third-unit prerequisite; non-mounted party interference remained unproved |
| Movement | `mounted-pair-pause-unpause` | PASS | A/B; stable command/destination, exact paused drift zero, mode-aware telemetry |
| Movement | `mounted-pair-destination-cancel` | PASS | A/B; command and movement representations stop, final synchronization and cleanup pass |
| Boundary | `mounted-pair-turn-based-entry-cleanup` | PASS | Direct handler only; no native controller/EventBus delivery claim |
| Boundary | `mounted-pair-realtime-entry-cleanup` | PASS | Direct handler only; no native controller/EventBus delivery claim |
| Boundary | `mounted-pair-save-safety` | PASS | Direct service cleanup; unchanged Working and no custom relationship serialization; no stock save |
| Boundary | `mounted-pair-load-safety` | PASS | Real exact-Working load and native prefix; clean fresh world |
| Boundary | `mounted-pair-area-transition-safety` | PASS | Direct pre-clean then real reload; native area-event delivery not independently qualified |

## F1 execution queue

| Order | Scenario | F1 status | Required control |
|---|---|---|---|
| 1 | `mounted-pair-doorway` A/B | TODO | Exact same current-size Mammoth unmounted matched control, then mounted route, each in a fresh process |
| 2 | `mounted-pair-selection` A/B | TODO | Select away to the eligible directly controllable third unit and back, each in a fresh process |
| 3 | `mounted-pair-party-formation` A/B | TODO | Meaningful stock group movement command with the eligible third unit, each in a fresh process |

Exact next action is to commit and publish the protected-save authority checkpoint, package the exact clean evidence HEAD, then run the queue with the authority pins and an immediate WhatIf before each live process. Do not rerun completed archaeology, lifecycle/boundary suites, or previously qualified movement rows absent a narrow regression gate. Do not synthesize units, change geometry during a run, weaken thresholds, or execute Phase 2.

## Measured F0 movement and visual disposition

All runnable pair-only rows passed twice with final synchronization, zero cleanup residue, and exact external restoration. Open-ground pass B finished `1.2466372098` from the endpoint, retaining only `0.0033627902` margin inside the unchanged `1.25` gate. Stop/start A/B held exact zero stopped drift for approximately `0.761` / `0.759` seconds and then moved approximately `11.436` / `11.417` units on distinct restart paths. Turns/corners A/B reached 3/3 endpoints and measured maximum direction changes of approximately `139.03` degrees. Position and rotation qualification retained the unchanged `0.10` gates, explicit raw-lag bounds/recovery, and zero final outstanding recovery.

Usable game-camera frames classify presentation `MECHANICALLY VIABLE, NEW ANIMATION/POSE WORK REQUIRED`. The silhouette is stable and readable, but the rider is rigidly upright with the lower body intersecting or occluded by the Mammoth back and has no saddle, reins, or seated pose. Some later-state frames are black or clipped; camera-only evidence does not prove portrait state, camera follow, UI selection, away/back selection, or party formation.

Frozen F0 named-row disposition: **22 PASS / 0 attributable FAIL / 3 DEFER — EVIDENCED**. F1 descriptor and protected-save admission do not change those results and supply no runtime result. No kill criterion has fired. The controlling status is `IN PROGRESS` until the three mandatory rows complete twice and the ledger is reconciled.
