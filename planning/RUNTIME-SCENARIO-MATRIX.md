# Runtime scenario matrix

Status: BLOCKED — CRITICAL

The guarded fixture gate is PASS. The exact save audit reports one canonical `KMC_AUTOMATION_BASELINE`, one canonical `KMC_AUTOMATION_WORKING`, and zero KMC-looking near-matches. The internal guard verified distinct paths, exact internal names, shared campaign/area identity, immutable Baseline, and Working-only write authorization before load. All admitted live runs restored the live Mods tree and save metadata exactly.

Current offline gate ledger: source validation 21 PASS / 0 FAIL; Release build PASS; pure/component 112 PASS / 0 FAIL; visual-capture contracts 12 PASS / 0 FAIL; guarded harness/protocol 105 PASS / 0 FAIL; assembly-backed 69 PASS / 0 FAIL (Kingmaker 58, Wrath 11).

## Executed runtime evidence

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

The five boundary runs use commit `fc7215481acf97ce1863eb1c75b3433889d2af7d`, DLL SHA-256 `202701ed9232e4f5a4d5ff65c468c684d1ed1dd53e4a8030be93b960a7cd202a`, and MVID `a702808c-e8a0-4755-bc24-5ed4e945866a`. Each has strict request/game/final and boundary-JSONL validation, exact Working identity continuity, terminal pre-restoration byte verification, and exact external restoration.

Historical engineering FAIL results remain preserved. They include fixture bootstrap, wrapper/restoration, endpoint-accounting, camera-capture, pause-validator, and stop/start recovery-oracle defects, plus the deterministic fixture-availability results. Each attributable harness/instrumentation defect has a regression. None is reclassified as a gameplay PASS, and none fired an architecture kill criterion.

## Required scenario disposition

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
| Movement | `mounted-pair-doorway` | DEFER — EVIDENCED | Valid unmounted Mammoth control and stable mounted entry; native hostile contamination prevents corrected repeatability |
| Movement | `mounted-pair-selection` | DEFER — EVIDENCED | No eligible directly controllable non-pair, so required away/back cannot run |
| Movement | `mounted-pair-party-formation` | DEFER — EVIDENCED | Same missing third-unit prerequisite; non-mounted party interference remains unproved |
| Movement | `mounted-pair-pause-unpause` | PASS | A/B; stable command/destination, exact paused drift zero, mode-aware telemetry |
| Movement | `mounted-pair-destination-cancel` | PASS | A/B; command and movement representations stop, final synchronization and cleanup pass |
| Boundary | `mounted-pair-turn-based-entry-cleanup` | PASS | Direct handler only; no native controller/EventBus delivery claim |
| Boundary | `mounted-pair-realtime-entry-cleanup` | PASS | Direct handler only; no native controller/EventBus delivery claim |
| Boundary | `mounted-pair-save-safety` | PASS | Direct service cleanup; unchanged Working and no custom relationship serialization; no stock save |
| Boundary | `mounted-pair-load-safety` | PASS | Real exact-Working load and native prefix; clean fresh world |
| Boundary | `mounted-pair-area-transition-safety` | PASS | Direct pre-clean then real reload; native area-event delivery not independently qualified |

## Measured movement and visual disposition

All runnable pair-only rows passed twice with final synchronization, zero cleanup residue, and exact external restoration. Open-ground pass B finished `1.2466372098` from the endpoint, retaining only `0.0033627902` margin inside the unchanged `1.25` gate. Stop/start A/B held exact zero stopped drift for approximately `0.761` / `0.759` seconds and then moved approximately `11.436` / `11.417` units on distinct restart paths. Turns/corners A/B reached 3/3 endpoints and measured maximum direction changes of approximately `139.03` degrees. Position and rotation qualification retained the unchanged `0.10` gates, explicit raw-lag bounds/recovery, and zero final outstanding recovery.

Usable game-camera frames classify presentation `MECHANICALLY VIABLE, NEW ANIMATION/POSE WORK REQUIRED`. The silhouette is stable and readable, but the rider is rigidly upright with the lower body intersecting or occluded by the Mammoth back and has no saddle, reins, or seated pose. Some later-state frames are black or clipped; camera-only evidence does not prove portrait state, camera follow, UI selection, away/back selection, or party formation.

Final named-row disposition: **22 PASS / 0 attributable FAIL / 3 DEFER — EVIDENCED**. The three DEFER rows are mandatory under the mission and prevent `PHASE 1 COMPLETE — PROCEED RECOMMENDED`. No kill criterion fired, so they do not authorize `PIVOT RECOMMENDED`. The controlling status remains `BLOCKED — CRITICAL`.
