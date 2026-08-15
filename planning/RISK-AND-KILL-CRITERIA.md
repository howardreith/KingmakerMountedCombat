# Risk and kill criteria

Status: PHASE 1 COMPLETE — PROCEED RECOMMENDED

No architecture kill criterion fired. Architecture B's movement, selection/formation, lifecycle, cleanup, boundary, native-asset, and external-restoration evidence is complete for Phase 1. The user-attested protected-save epoch cleared F1 admission without granting any non-Working write authority, and all three previously missing rows passed twice.

Final offline gates are source 21 PASS / 0 FAIL; Release build PASS; pure/component 112 PASS / 0 FAIL; visual-capture contracts 12 PASS / 0 FAIL; guarded harness/protocol 134 PASS / 0 FAIL; assembly-backed 69 PASS / 0 FAIL (Kingmaker 58, Wrath 11). F0 contributed 22 PASS / 0 attributable FAIL / 3 DEFER — EVIDENCED; F1 replaced the three deferrals with repeatable PASS, producing the final `25 PASS / 0 attributable FAIL / 0 DEFER` named-row ledger. Historical runner, validator, capture, and instrumentation failures remain preserved but are not architecture kills.

Protected-save authority implementation commit `6abc293f12bacbc250fc4c012fbced05b3763881` is on `codex/mounted-combat-feasibility`. Its exact four-file scope is `scripts/runtime/RuntimeHarness.Common.ps1`, new `scripts/runtime/New-KmcProtectedSaveContinuityAuthority.ps1`, `scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1`, and `scripts/Test-Harness.ps1`, with SHA-256 `59e24b0156f3f8c44ebd99c7bb403952108a7cef505bf55d633ca173f47d0ee1`, `9a5b983a7863b54457bbb4b869a5346b8d41d5744f901536ded647fb8127b2f3`, `b425fce49d329ccea6cba04dd423c242a656d4aff15ed63234f248c559db16d4`, and `cd6f3aa14e5c08211cc39d6e51fe86e304140993e137a5b8ed2c66fd1a41cf8`. AST `4/4`, source `21/0`, Release build, component `112/0`, visual `12/0`, harness `134/0`, assembly-backed `69/0`, and diff check passed.

## Evidence epochs and current safety gate

F0 is frozen at runtime-evidence commit `fc7215481acf97ce1863eb1c75b3433889d2af7d` and Working SHA-256/length/ticks `a5f7a7fb77f0465df1591360ecd1730e2c28215d83e27aafc70efef3110e6dc5` / `718821` / `639222573406912936`.

F1 was descriptor-requalified at execution HEAD `15bbf0e2029ebc43d8bada48b83b4f55d43f8db0` by committed transaction `fixture-requalification-20260814T1140026497594Z`, qualified at `2026-08-14T11:40:02.8457604+00:00`. Qualification SHA-256/length/ticks are `95e9a3c6203f11cfdd7e615849eb61ebc060131075f6a7f40771dcd7eeb68a7a` / `1272` / `639223044028647586`; phase-`committed` state SHA-256/length/ticks are `68fdc0f759e7f7daffaffaeb27996357e74c8f5861c5bf9345005edbefa66cce` / `1707` / `639223044029797599`; exact prior F0 qualification backup SHA-256/length are `c1e33c75004212039258d6d90ba20e43c37c50966c94e685dbad1ca0e653654f` / `1272`.

The F1 audit is exactly Baseline 1 / Working 1 / near-match 0. Baseline is unchanged at `c29d965c9ff5dc0f971659d9ae154877aa4a9a461ca220d1ce28e7c7fd9d2512` / `686605` / `639222474845172002`; revised Working alone is descriptor-qualified at `5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6` / `816452` / `639222975870964407`. Exact names, distinct paths, Manual/v1, and common GameName `cvb`, GameId `d41185be-edc1-47c0-b9d5-e1d7a9c8e65f`, and Area `9d1278a2f599b2a4daab53abdfe88d2e` passed; only Working is writable. Requalification preserved its then-current save inventory before/after at `973f203e16aad29d7e4865bae6f81149d959a4ac179953c87781a06d1c71898d`, `271` files, `3178473776` bytes. Normal guard and already-committed recovery `-WhatIf` passed. No process, lock, sentinel, live KMC tree, or transaction debris remains.

Frozen F0 inventory was digest `58a54974423316041e29d32b1093c5d6775dbd67fe2b37ffaea3c9ae27de19be`, `271` files, `3178063612` bytes. Current F1 differs in exactly three entries and no others: authorized Working `718821` / `639222573406912936` to `816452` / `639222975870964407`; user-attested protected Auto `204829` / `639222474628040540` to `333208` / `639222975467301685`; user-attested protected Quick `625411` / `639220694761623881` to `809565` / `639222975512345112`. The user attested that Auto and Quick changed in the external Kingmaker fixture-preparation session. They are now baselined but remain non-writable.

Append-only authority `C:\Dev\KingmakerMountedCombatLab\runtime-state\protected-save-authorities\20260814T1445257441387Z-user-fixture-preparation.json`, epoch `20260814T1445257441387Z-user-fixture-preparation`, SHA-256 `77fd415d177423dd7da58e3685e418f38cf954dead75068424a717e2be6b3ca9`, binds current inventory digest `973f203e16aad29d7e4865bae6f81149d959a4ac179953c87781a06d1c71898d`, Auto SHA-256 `9599e9e15bdd04cde7b15d47795510b95cd8e1333f107f2e3b64469ebf3330fc`, and Quick SHA-256 `e26dc2ce00e6ed68953c2c9b1e89584ae3c3bd50f4cf698ecc2c624e21d10575`. WhatIf, append-only creation, and independent continuity validation passed with no save mutation or residue.

All six F1 runs used commit `d5bd7fa9c434f04c6f8487b61ea49e3cf983c397`, package SHA-256 `5ce3bd7d98a090ee05405cc4b4725fa58f13f1926958a69905ba478374c75a4d`, DLL SHA-256 `202701ed9232e4f5a4d5ff65c468c684d1ed1dd53e4a8030be93b960a7cd202a`, and MVID `a702808c-e8a0-4755-bc24-5ed4e945866a`. Doorway A/B passed `60/0` each, selection A/B `54/0` each, and formation A/B `58/0` each, totaling `344/0`. Every process independently passed request/game/final validation `31/0`, `39/0`, and `29/0`, ended residue-free, and restored exact Mods and protected save state.

| ID | Risk / kill criterion | Final Phase 1 evidence | Status | Remaining control |
|---|---|---|---|---|
| K1 | Two active nav agents collide, oscillate, or diverge | Open ground, stop/start, turns/corners, pause/unpause, and destination-cancel passed A/B with the Mammoth as the sole authoritative mover; zero oscillation, stuck, unexpected-repath, command-replacement, synchronization-violation, or cleanup-residue counts | PASS | Preserve one-mover and fixed synchronization gates |
| K2 | No scoped authoritative-mover control point | Exact `AgentOverride`, stock rider suppression, `CountingGuard`, private command-recipient rewrite, and live routed movement are proven without a global tick replacement | PASS | Retain exact MVID/token/active-pair guards |
| K3 | Pair fails valid turns/doorways passed by unmounted control | Turns/corners passed A/B. F1 doorway A/B used the same open StandardDoor, passed the exact same-current-Mammoth unmounted control, then qualified 2/2 strict mounted/control legs at maximum final/best distances `1.2317738508` / `1.2061703079`, with zero instability or residue | PASS | Preserve matched-control and fixed endpoint gates; broader indoor geometry remains Phase 2 evidence |
| K4 | Selection/formation cannot restore | F1 selection A/B normalized mount-to-rider selection, switched to the same eligible third unit and back, routed movement, and restored exact state. Formation A/B used the stock group command, moved both recipients, met target/separation gates, and restored exact state | PASS | Preserve real-unit away/back and stock group-command regressions |
| K5 | Cleanup leaves movement, avoidance, selection, command, or view residue | Lifecycle A/B passed 8/8 rows and 339/0 assertions each. All qualified movement and boundary rows ended Unmounted with stock agents, avoidance, overrides/components, `ForbidRotation`, attachment parent/lease, commands, pause, and selection restored without residue | PASS | Preserve direct-handler claim labels and residue validators |
| K6 | Broad global movement patch affects other units | Eight exact active-pair guards and no movement-tick replacement prove B does not require a broad global patch. Selection and stock formation A/B used the same real non-pair with zero interference, command replacement, or selection loss | PASS | Retain active-pair guards and non-pair identity/command assertions |
| K7 | Save/load/area retains half-mounted state | All five individual boundary rows passed with exact evidence and restoration. Save is cleanup-only with no stock save/serialization; load performs real exact-Working `Game.LoadGame` through the native prefix; area directly pre-cleans before real `ReloadArea`. No relationship reconstructed and no old/fresh-world residue remained | PASS | Do not broaden claims to native TB/RT/area delivery, stock save round-trip, persistence, or uninstall |
| K8 | Presentation requires Wrath assets | Original code plus the native Kingmaker Mammoth produces a readable mounted silhouette; no Wrath code, runtime assembly, model, clip, controller, material, texture, or offset is required or shipped | PASS | Any new pose/animation work must remain original or Kingmaker-native |
| K9 | No native candidate is remotely plausible | Usable camera frames classify the pair `MECHANICALLY VIABLE, NEW ANIMATION/POSE WORK REQUIRED`. The standing/intersecting pose is not polished, but it is remotely plausible and explicitly does not fire K9 | PASS | New seated pose/animation and clipping review in a separately authorized phase |
| K10 | Fresh-process tests remain nondeterministically unstable | Lifecycle and every required movement row passed A/B from the same clean commit/package for its evidence epoch. F1 doorway, selection, and formation repeated exactly in six fresh processes. Earlier failures were bounded instrumentation or fixture defects with regressions | PASS | Preserve same-commit/package repeatability and recorded narrow endpoint margins |
| K11 | Solution depends on Wrath runtime assembly | Source/package allowlists and exact references prove Kingmaker/UMM/Harmony12 only | PASS | Preserve source/package/assembly validators |
| K12 | External state cannot be restored exactly | Every F0 live transaction restored exact live Mods and save metadata. F1 requalification and protected-save authority creation changed no save; all six F1 processes restored exact Working, protected inventory, Auto/Quick pins, live Mods, locks, and sentinels with all 18 transaction states restored and no errors | PASS | Preserve exact protected continuity and transaction restoration for every later process |

There is no K13. The stop/start recovery false negative was an evidence-state-machine defect: two corrected A/B passes proved the unchanged thresholds and recovery contract. It did not create a new kill criterion.

## Claim-scoped boundary qualification

The following individual runs are PASS:

- `20260814T090000Z-boundary-tb-pass`: 1 PASS / 0 FAIL rows, 56/0 assertions; direct turn-based lifecycle-handler cleanup only, with native delivery false.
- `20260814T091500Z-boundary-rt-pass`: 1 PASS / 0 FAIL rows, 56/0 assertions; direct real-time lifecycle-handler cleanup only, with native delivery false.
- `20260814T093000Z-boundary-save-pass`: 1 PASS / 0 FAIL rows, 59/0 assertions; direct `GuardBoundary(SaveRequested)` cleanup, zero stock `SaveRoutine` or serialization.
- `20260814T094500Z-boundary-load-pass`: 1 PASS / 0 FAIL rows, 44/0 assertions; real exact-Working `Game.LoadGame` plus native `LoadRoutine`-prefix authorization, clean fresh world, no UI-load claim.
- `20260814T100000Z-boundary-area-pass`: 1 PASS / 0 FAIL rows, 44/0 assertions; direct `OnAreaBeginUnloading` cleanup latch before real `ReloadArea(AutoSaveMode.None)`, with native area-event delivery not independently qualified.

These passes close K7 only for the Phase 1 runtime-only/no-save design. They do not establish persistence, stock save round-trip, native mode/area event delivery, or uninstall behavior.

## Final completion disposition

Status: PHASE 1 COMPLETE — PROCEED RECOMMENDED

Fixture descriptor identity and protected-save continuity pass. The F1 exact audit is Baseline=1 / Working=1 / KMC-looking near-match=0, with distinct paths, exact internal names, shared `GameId`/`GameName`/`Area` identity, Baseline immutability, Working-only write authorization, and exact user-attested protected Auto/Quick pins.

The three F0 deferrals are superseded by F1 PASS evidence, not erased: doorway, away/back selection, and stock party formation each passed twice in fresh processes under unchanged gates. No K1–K12 criterion fired. The final 25-row ledger is `25 PASS / 0 attributable FAIL / 0 DEFER`.

Architecture B is selected and remains default-off. The truthful status is `PHASE 1 COMPLETE — PROCEED RECOMMENDED`, subject to the existing `MECHANICALLY VIABLE, NEW ANIMATION/POSE WORK REQUIRED` limitation. Phase 2 remains outside current execution authorization and requires a separate mission.

## Phase 2A review-gate overlay

The separately authorized Phase 2 mission has not changed any Phase 1 kill criterion. Dev.13 complete-suite A/B each pass all seven presentation rows at `381/0` under unchanged synchronization, cleanup, screenshot, and `2000/500`-microsecond pose-cost gates. The first-row maxima are `24.8` and `15.4` microseconds, so the dev.12 `2017.7` outlier is closed by reversible cold-path priming rather than threshold relaxation.

The remaining presentation risk is subjective and deliberately human-owned: pose acceptability, fixture-geometry occlusion, doorway/edge framing, native blue selection silhouette, camera feel, and physical pointer behavior. The guarded review path permits no save writes, rejects save/load/combat/area/mode drift, records only `PENDING`, and restores external state after normal exit. Until the user accepts the exact published review package, the truthful authorization state is `BLOCKED - MANUAL VISUAL ACCEPTANCE REQUIRED` once all remaining package/repeat/publication work is complete; no combat implementation is permitted before then.
