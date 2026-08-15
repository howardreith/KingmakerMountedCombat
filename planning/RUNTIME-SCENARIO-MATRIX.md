# Runtime scenario matrix

Status: PHASE 1 COMPLETE — PROCEED RECOMMENDED

The guarded F1 descriptor and protected-save authority gates are PASS. The exact audit reports one canonical `KMC_AUTOMATION_BASELINE`, one canonical `KMC_AUTOMATION_WORKING`, and zero KMC-looking near-matches. It verified distinct paths, exact internal names, shared campaign/area identity, immutable Baseline, and Working-only write authorization. The user-attested Auto/Quick transition is an exact protected baseline, never project write authority. The six missing F1 processes passed, completing the named-row ledger at `25 PASS / 0 attributable FAIL / 0 DEFER`.

Frozen F0 offline gate ledger: source validation 21 PASS / 0 FAIL; Release build PASS; pure/component 112 PASS / 0 FAIL; visual-capture contracts 12 PASS / 0 FAIL; guarded harness/protocol 105 PASS / 0 FAIL; assembly-backed 69 PASS / 0 FAIL (Kingmaker 58, Wrath 11).

Protected-save authority implementation commit `6abc293f12bacbc250fc4c012fbced05b3763881` is on `codex/mounted-combat-feasibility`. It changes `scripts/runtime/RuntimeHarness.Common.ps1`, adds `scripts/runtime/New-KmcProtectedSaveContinuityAuthority.ps1`, and changes `scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1` plus `scripts/Test-Harness.ps1`; SHA-256 values are respectively `59e24b0156f3f8c44ebd99c7bb403952108a7cef505bf55d633ca173f47d0ee1`, `9a5b983a7863b54457bbb4b869a5346b8d41d5744f901536ded647fb8127b2f3`, `b425fce49d329ccea6cba04dd423c242a656d4aff15ed63234f248c559db16d4`, and `cd6f3aa14e5c08211cc39d6e51fe86e304140993e137a5b8ed2c66fd1a41cf8`. Gates passed AST `4/4`, source `21/0`, Release build, component `112/0`, visual `12/0`, harness `134/0`, assembly-backed `69/0`, and diff check. This validates the admission guard and supplies no F1 runtime row.

## Fixture evidence epochs

F0 is the frozen runtime epoch at commit `fc7215481acf97ce1863eb1c75b3433889d2af7d`. It used Working SHA-256 `a5f7a7fb77f0465df1591360ecd1730e2c28215d83e27aafc70efef3110e6dc5`, length `718821`, ticks `639222573406912936`; all executed evidence below belongs to F0 unless explicitly stated otherwise. Its named-row ledger remains `22 PASS / 0 attributable FAIL / 3 DEFER — EVIDENCED`.

F1 is descriptor-requalified and runtime-qualified for the Phase 1 slice. Transaction `fixture-requalification-20260814T1140026497594Z` committed at `2026-08-14T11:40:02.8457604+00:00`. Qualification SHA-256/length/ticks are `95e9a3c6203f11cfdd7e615849eb61ebc060131075f6a7f40771dcd7eeb68a7a` / `1272` / `639223044028647586`; phase-`committed` state SHA-256/length/ticks are `68fdc0f759e7f7daffaffaeb27996357e74c8f5861c5bf9345005edbefa66cce` / `1707` / `639223044029797599`. The exact F0 qualification backup is SHA-256 `c1e33c75004212039258d6d90ba20e43c37c50966c94e685dbad1ca0e653654f`, length `1272`.

F1 retains unchanged Baseline SHA-256/length/ticks `c29d965c9ff5dc0f971659d9ae154877aa4a9a461ca220d1ce28e7c7fd9d2512` / `686605` / `639222474845172002` and descriptor-qualifies only revised Working `5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6` / `816452` / `639222975870964407`. The 1/1/0 audit, exact names, distinct paths, Manual/v1 descriptors, and shared `cvb` / `d41185be-edc1-47c0-b9d5-e1d7a9c8e65f` / `9d1278a2f599b2a4daab53abdfe88d2e` identity passed. Working alone is writable. The requalification transaction preserved its current before/after inventory at digest `973f203e16aad29d7e4865bae6f81149d959a4ac179953c87781a06d1c71898d`, `271` files, `3178473776` bytes. Normal guard and recovery `-WhatIf` passed, with no process, lock, sentinel, live KMC tree, or transaction debris.

Frozen F0 inventory was digest `58a54974423316041e29d32b1093c5d6775dbd67fe2b37ffaea3c9ae27de19be`, `271` files, `3178063612` bytes. Current F1 differs in exactly three entries and no others:

| Entry | Authorization | F0 length / ticks | Current F1 length / ticks |
|---|---|---|---|
| Working | Authorized | `718821` / `639222573406912936` | `816452` / `639222975870964407` |
| `Auto_1120` | User-authorized protected baseline only | `204829` / `639222474628040540` | `333208` / `639222975467301685` |
| `Quick_438` | User-authorized protected baseline only | `625411` / `639220694761623881` | `809565` / `639222975512345112` |

The user attested that Auto and Quick changed during the external Kingmaker fixture-preparation session, not in a KMC transaction. Their current exact bytes and metadata are protected baselines only. The project must not restore or mutate either save.

The append-only authority is `C:\Dev\KingmakerMountedCombatLab\runtime-state\protected-save-authorities\20260814T1445257441387Z-user-fixture-preparation.json`, epoch `20260814T1445257441387Z-user-fixture-preparation`, SHA-256 `77fd415d177423dd7da58e3685e418f38cf954dead75068424a717e2be6b3ca9`, length `119319`, ticks `639223155954515237`. It binds current inventory digest `973f203e16aad29d7e4865bae6f81149d959a4ac179953c87781a06d1c71898d`, Auto SHA-256 `9599e9e15bdd04cde7b15d47795510b95cd8e1333f107f2e3b64469ebf3330fc`, and Quick SHA-256 `e26dc2ce00e6ed68953c2c9b1e89584ae3c3bd50f4cf698ecc2c624e21d10575`. Creation WhatIf, append-only actual creation, and independent continuity validation passed with no save mutation or residue.

## Executed F1 completion evidence

All six rows used commit `d5bd7fa9c434f04c6f8487b61ea49e3cf983c397`, package SHA-256 `5ce3bd7d98a090ee05405cc4b4725fa58f13f1926958a69905ba478374c75a4d`, DLL SHA-256 `202701ed9232e4f5a4d5ff65c468c684d1ed1dd53e4a8030be93b960a7cd202a`, and MVID `a702808c-e8a0-4755-bc24-5ed4e945866a`. Each passed request/game/final validation `31/0`, `39/0`, and `29/0`.

| Scenario | Process A | Process B | Qualified evidence |
|---|---|---|---|
| `mounted-pair-doorway` | `20260814T150000Z-doorway-passA`, 60/0 | `20260814T151500Z-doorway-passB`, 60/0 | Same open `DoorHolder/dwarf_dungeon_door_01`; same-current-Mammoth unmounted control true; approach legitimately skipped because start was already near; 2/2 strict path legs; max final/best `1.2317738508` / `1.2061703079`; no instability or residue |
| `mounted-pair-selection` | `20260814T153000Z-selection-passA`, 54/0 | `20260814T154500Z-selection-passB`, 54/0 | Same non-pair `d17c8fd0-6627-40c4-bf9a-024cf001c1f7`; mount normalized to rider; away/back true; 1/1 routed endpoint at `1.1999048650` / `1.2147087582`; zero selection loss/interference |
| `mounted-pair-party-formation` | `20260814T160000Z-formation-passA`, 58/0 | `20260814T161500Z-formation-passB`, 58/0 | Stock group movement; normalized rider+non-pair selection; mount final `1.2357761832` / `1.2424143584`; non-pair final `0.0239113005` / `0.0450875333`; minimum separation `2.0965969637` / `2.0840256324` above required `1.5606060028`; zero interference/replacement/loss |

F1 runtime assertions total `344 PASS / 0 FAIL`. Every row has exact final synchronization and residue-free cleanup. Every combined/Mods/save transaction is `restored` with all five restoration flags true and no errors. Live Mods are exact pre/post digest `e62320bbe7d4b83edc128a62f9f5852b0669c5549859602c335c649318578d47` (`348` files / `44` directories / `69797076` bytes); protected save inventory remains exact digest `973f203e16aad29d7e4865bae6f81149d959a4ac179953c87781a06d1c71898d` (`271` files / `3178473776` bytes).

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

## F1 completion queue

| Order | Scenario | F1 status | Required control |
|---|---|---|---|
| 1 | `mounted-pair-doorway` A/B | PASS | Exact same current-size Mammoth unmounted matched control, then mounted route, each in a fresh process |
| 2 | `mounted-pair-selection` A/B | PASS | Select away to the eligible directly controllable third unit and back, each in a fresh process |
| 3 | `mounted-pair-party-formation` A/B | PASS | Meaningful stock group movement command with the eligible third unit, each in a fresh process |

The queue is complete. Do not rerun completed archaeology, lifecycle/boundary suites, or movement rows absent a narrow regression gate. Do not synthesize units, weaken thresholds, or execute Phase 2 under this mission.

## Phase 2A presentation qualification addendum

Phase 2A presentation runtime qualification remains `IN PROGRESS`. The seven exact rows are `pose-idle`, `pose-walk-run`, `pose-turn-stop`, `pose-doorway-formation`, `pose-equipment-variants`, `ui-selection-portrait-actionbar`, and `camera-follow-and-command-routing`; each still requires two fresh-process passes before the two complete-suite processes and manual review build.

Three early guarded `pose-idle` attempts are preserved without qualification credit. `20260815T034700Z-pose-idle-calibrationA` failed in game at `24/2` and exposed cumulative-average and stationary-phase evidence defects. `20260815T041000Z-pose-idle-dev3-calibrationA` passed in game at `26/0` but final validation rejected stale transition-frame cleanup evidence. `20260815T042100Z-pose-idle-dev4-passA` passed in game at `26/0` and proved exact later-frame cleanup, but the independent external validator still duplicated the obsolete unconditional Update-sample rule. In all three processes, external Working, protected saves, and Mods restored exactly; none is Pass A.

Version `0.1.0-phase2a-dev.5` aligned the external stationary-row policy, after which `20260815T044100Z-pose-idle-dev5-passA` and `20260815T044700Z-pose-idle-dev5-passB` qualified `pose-idle` twice at `26/0` from the same clean commit/package. Both proved healthy/applied pose coverage, microunit residuals, zero drift, exact stationary final synchronization, exact cleanup, and external restoration. At that checkpoint, `pose-idle` was the only qualified presentation row; subjective acceptance remained open.

`20260815T045300Z-pose-walk-run-dev5-passA` passed in game at `68/0` but its final result is `FAIL` and receives no credit: the validator rejected the route's deliberately measured `86.85`-degree inter-leg direction change as turns/corners-only evidence. Dev.6 corrects that exact contract while preserving the dedicated `>=75`-degree turn-row gate and rejecting stray turn metrics on unrelated rows. Its complete local gate is source `21/0`, Release build PASS, component `141/0`, visual `12/0`, harness/protocol `139/0`, and assembly-backed `101/0`. The next runtime cell is a fresh dev.6 `pose-walk-run` attempt.

That fresh dev.6 cell, `20260815T050600Z-pose-walk-run-dev6-passA`, failed at `67/1` and receives no qualification credit. It passed both route endpoints, collected `241/80` walk/run samples at `1.25/4.064`, kept 442/442 pose observations healthy and applied, measured `86.9282` degrees of direction change, passed final synchronization and exact cleanup, and restored Working/protected saves/Mods exactly. Its completed mean pose cost was `294.7244` microseconds, but one application took `123729.0` microseconds and correctly violated the unchanged `2000`-microsecond maximum. Inspection found nine reference allocations per evaluated frame in the timed pose path: seven baseline snapshots and two two-bone solutions. Dev.7 converts both carriers to immutable value types, pre-sizes the reused frame list, and makes successful restoration allocation-free while retaining exact failure aggregation. Its complete local gate passes source `21/0`, Release build, component `142/0`, visual `12/0`, harness/protocol `139/0`, and assembly-backed `101/0`. The next runtime cell is a fresh exact-package dev.7 walk/run attempt.

Dev.7 exact-package attempts `20260815T053000Z-pose-walk-run-dev7-passA` and `20260815T054500Z-pose-walk-run-dev7-passB` then qualified walk/run at `68/0` each. They recorded 448/449 healthy and applied frames with maximum/mean costs `1623.1/13.7308` and `1672.5/13.6069` microseconds, completed 2/2 endpoints, exercised walk/run and substantial inter-leg direction change, passed final synchronization, left zero pose-component or attachment residue, and restored every external authority. `pose-idle` and `pose-walk-run` are now the two qualified presentation rows; five individual rows, two complete-suite processes, and subjective acceptance remain open.

## Measured F0 movement and visual disposition

All runnable pair-only rows passed twice with final synchronization, zero cleanup residue, and exact external restoration. Open-ground pass B finished `1.2466372098` from the endpoint, retaining only `0.0033627902` margin inside the unchanged `1.25` gate. Stop/start A/B held exact zero stopped drift for approximately `0.761` / `0.759` seconds and then moved approximately `11.436` / `11.417` units on distinct restart paths. Turns/corners A/B reached 3/3 endpoints and measured maximum direction changes of approximately `139.03` degrees. Position and rotation qualification retained the unchanged `0.10` gates, explicit raw-lag bounds/recovery, and zero final outstanding recovery.

Usable game-camera frames classify presentation `MECHANICALLY VIABLE, NEW ANIMATION/POSE WORK REQUIRED`. The silhouette is stable and readable, but the rider is rigidly upright with the lower body intersecting or occluded by the Mammoth back and has no saddle, reins, or seated pose. Some later-state frames are black or clipped; camera-only evidence does not prove portrait state, camera follow, UI selection, away/back selection, or party formation.

Frozen F0 named-row disposition remains **22 PASS / 0 attributable FAIL / 3 DEFER — EVIDENCED** for provenance. F1 supersedes each deferral with repeatable PASS evidence. The final Phase 1 named-row disposition is **25 PASS / 0 attributable FAIL / 0 DEFER**. No kill criterion fired; the controlling status is `PHASE 1 COMPLETE — PROCEED RECOMMENDED` with Architecture B.
