# Phase 1 qualification

Status: IN PROGRESS

Frozen F0 runtime-evidence commit: `fc7215481acf97ce1863eb1c75b3433889d2af7d`

Frozen F0 qualified package: SHA-256 `2c47d7ad4e82942bd303ab848ab82b9163a76ba9db79d52aaf8bfb0f028d80fc`; DLL SHA-256 `202701ed9232e4f5a4d5ff65c468c684d1ed1dd53e4a8030be93b960a7cd202a`; DLL MVID `a702808c-e8a0-4755-bc24-5ed4e945866a`.

Protected-save authority implementation commit: `6abc293f12bacbc250fc4c012fbced05b3763881` on `codex/mounted-combat-feasibility`.

## Evidence epochs

F0 is the frozen completed runtime epoch. It owns the current 25-row ledger, including `22 PASS / 0 attributable FAIL / 3 DEFER — EVIDENCED`, and used Working SHA-256 `a5f7a7fb77f0465df1591360ecd1730e2c28215d83e27aafc70efef3110e6dc5`, length `718821`, ticks `639222573406912936`.

F1 is descriptor-requalified and admitted for only the six missing Phase 1 processes. Transaction `fixture-requalification-20260814T1140026497594Z` committed the revised Working qualification at `2026-08-14T11:40:02.8457604+00:00`. The qualification record is SHA-256 `95e9a3c6203f11cfdd7e615849eb61ebc060131075f6a7f40771dcd7eeb68a7a`, length `1272`, ticks `639223044028647586`; its phase-`committed` state is SHA-256 `68fdc0f759e7f7daffaffaeb27996357e74c8f5861c5bf9345005edbefa66cce`, length `1707`, ticks `639223044029797599`. The durable prior-record backup exactly preserves F0 qualification SHA-256 `c1e33c75004212039258d6d90ba20e43c37c50966c94e685dbad1ca0e653654f`, length `1272`.

F1 proves exactly Baseline 1 / Working 1 / near-match 0. Baseline remains byte-identical at SHA-256 `c29d965c9ff5dc0f971659d9ae154877aa4a9a461ca220d1ce28e7c7fd9d2512`, length `686605`, ticks `639222474845172002`; Working is SHA-256 `5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6`, length `816452`, ticks `639222975870964407`. The archives have exact internal names, distinct paths, Manual/v1 descriptors, and common GameName `cvb`, GameId `d41185be-edc1-47c0-b9d5-e1d7a9c8e65f`, and Area `9d1278a2f599b2a4daab53abdfe88d2e`. Only Working is writable. The requalification transaction preserved the then-current full-save inventory before/after at digest `973f203e16aad29d7e4865bae6f81149d959a4ac179953c87781a06d1c71898d`, `271` files, `3178473776` bytes. Normal guard and already-committed recovery `-WhatIf` passed; no process, lock, sentinel, transaction debris, or live KMC tree remains.

The user explicitly attested that the manual Kingmaker F1 preparation session also changed exactly `Auto_1120.zks` and `Quick_438.zks`; those changes were external user activity, not a KMC transaction. Frozen F0 inventory was digest `58a54974423316041e29d32b1093c5d6775dbd67fe2b37ffaea3c9ae27de19be`, `271` files, `3178063612` bytes. Current F1 differs in exactly three entries and no others: Working changed from length/ticks `718821` / `639222573406912936` to `816452` / `639222975870964407`; Auto changed from `204829` / `639222474628040540` to `333208` / `639222975467301685`; Quick changed from `625411` / `639220694761623881` to `809565` / `639222975512345112`. Working retains the sole project write authorization. Auto and Quick are now exact protected baselines only and remain outside project write authority.

Commit `6abc293f12bacbc250fc4c012fbced05b3763881` adds append-only protected-save epoch authority and launcher enforcement in exactly `scripts/runtime/RuntimeHarness.Common.ps1`, new `scripts/runtime/New-KmcProtectedSaveContinuityAuthority.ps1`, `scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1`, and `scripts/Test-Harness.ps1`. Their SHA-256 values are `59e24b0156f3f8c44ebd99c7bb403952108a7cef505bf55d633ca173f47d0ee1`, `9a5b983a7863b54457bbb4b869a5346b8d41d5744f901536ded647fb8127b2f3`, `b425fce49d329ccea6cba04dd423c242a656d4aff15ed63234f248c559db16d4`, and `cd6f3aa14e5c08211cc39d6e51fe86e304140993e137a5b8ed2c66fd1a41cf8`. AST parse passed `4/4`; the complete Release gate passed source `21/0`, build, component `112/0`, visual `12/0`, harness `134/0`, and assembly-backed `69/0`; `git diff --check` passed.

The external transition is durably recorded at `C:\Dev\KingmakerMountedCombatLab\runtime-state\protected-save-authorities\20260814T1445257441387Z-user-fixture-preparation.json`, epoch `20260814T1445257441387Z-user-fixture-preparation`, SHA-256 `77fd415d177423dd7da58e3685e418f38cf954dead75068424a717e2be6b3ca9`, length `119319`, ticks `639223155954515237`. It binds inventory digest `973f203e16aad29d7e4865bae6f81149d959a4ac179953c87781a06d1c71898d`, `271` files, `3178473776` bytes; `Auto_1120.zks` SHA-256 `9599e9e15bdd04cde7b15d47795510b95cd8e1333f107f2e3b64469ebf3330fc`; and `Quick_438.zks` SHA-256 `e26dc2ce00e6ed68953c2c9b1e89584ae3c3bd50f4cf698ecc2c624e21d10575`. WhatIf, append-only creation, and independent continuity validation passed without save mutation or residue.

All runtime-behavior results in the ledger below remain frozen F0 results until the six F1 processes complete. F1 runtime behavior is currently `TODO`; the protected-save authority now permits only those bounded runs.

## Gate ledger

| Gate | Result | Evidence |
|---|---|---|
| Standalone identity/separation | PASS | Independent repo/branch/assembly/UMM ID/namespace/version; no foreign gameplay dependency or prohibited payload |
| Environment/provenance | PASS | Exact Kingmaker `2.1.7b`, UMM `0.28.2.0`, Harmony12, Unity, and Wrath hashes/MVIDs |
| .NET Framework 4.7 / C# 7.3 build | PASS | Release build with zero warnings/errors; Copy Local disabled for local game/tool references |
| Wrath responsibility map | PASS | 21 matrix responsibilities and exact bounded local contracts |
| Kingmaker equivalence map | PASS | 15 machine-readable responsibilities and narrow control points |
| Native candidate/rig | PASS | Horse rejected; exact active larger Mammoth, live views/stock agents, and `Spine` anchor verified |
| Canonical fixture descriptor guard | PASS | F1 descriptor-requalified exactly Baseline=1, Working=1, near-match=0; distinct non-linked paths, exact internal names, shared campaign/area identity |
| Baseline/Working pair safety | PASS | Baseline byte-identical; only exact revised Working authorized; requalification committed without changing its then-current inventory |
| Cross-epoch protected-save continuity | PASS | User-attested transition records exactly Working, `Auto_1120.zks`, and `Quick_438.zks`; append-only authority pins Auto/Quick hashes and metadata while preserving Working-only write authorization and every other F0 entry |
| Runtime harness/WhatIf | PASS | F0 package/WhatIf PASS. Authority commit `6abc293f12bacbc250fc4c012fbced05b3763881`: AST `4/4`, source `21/0`, build, component `112/0`, visual `12/0`, harness `134/0`, assembly `69/0`, diff PASS; authority creation WhatIf and independent continuity validation PASS. Scenario WhatIf remains the next prelaunch gate |
| Relationship/lifecycle | PASS | Two fresh lifecycle suites, each `8/8` rows and `339/0` assertions, with direct-handler scope retained |
| One-mover movement | PASS | Open-ground A/B, exact mount authority, bounded synchronization, clean stop/dismount, no residue |
| Stop/start | PASS | Repaired A/B each `61/0`; exact stationary-boundary reconciliation without threshold weakening |
| Turns/corners | PASS | A/B each `74/0`; substantial turns/reversals and bounded synchronization |
| Pause/unpause | PASS | A/B each `49/0`; authority and destination stable across pause |
| Destination cancel | PASS | A/B each `49/0`; effective pair motion and commands stopped cleanly |
| Doorway matched control | DEFER — EVIDENCED | Frozen F0 native combat contaminated the attempt. F1 is TODO and requires the exact same-Mammoth unmounted control and mounted traversal twice |
| Selection away/back | DEFER — EVIDENCED | Frozen F0 lacked an eligible non-pair unit. F1 is TODO and requires away to the eligible third unit and back twice; no synthetic substitute |
| Party formation | DEFER — EVIDENCED | Frozen F0 lacked the third-unit prerequisite. F1 is TODO and requires a meaningful stock group movement command twice |
| Turn-based boundary | PASS | `20260814T090000Z-boundary-tb-pass`, `56/0`; direct handler only |
| Realtime boundary | PASS | `20260814T091500Z-boundary-rt-pass`, `56/0`; direct handler only |
| Save safety | PASS | `20260814T093000Z-boundary-save-pass`, `59/0`; direct guard, zero stock SaveRoutine/write |
| Load safety | PASS | `20260814T094500Z-boundary-load-pass`, `44/0`; real exact Working load and native prefix, clean fresh world |
| Area transition | PASS | `20260814T100000Z-boundary-area-pass`, `44/0`; direct pre-clean followed by real ReloadArea |
| Visual classification | PASS | `MECHANICALLY VIABLE, NEW ANIMATION/POSE WORK REQUIRED`; no visual kill criterion fired, while black-frame and camera-only coverage limitations remain |
| External restoration | PASS | Every F0 run restored exact external state; F1 requalification and authority creation changed no save. Future runs must preserve the newly pinned Auto/Quick baseline and all other protected entries |
| Architecture disposition | IN PROGRESS | Protected-save admission is cleared; three F1 rows still require A/B evidence before B can be finalized |

## Boundary reconciliation

The five boundary rows total `259 PASS / 0 FAIL` runtime assertions. Each individual run also passed strict request validation `31/0`, game-result validation `39/0`, and final-result validation `29/0`. The evidence binds the exact commit/package/DLL, exact Working descriptor and file identities, per-row authorization counters, relationship state, cleanup latch, loading transitions, fresh-world campaign identity, and final external restoration.

Claim scope remains deliberately narrow:

- Turn-based: `Direct HandleTurnBasedModeStateChanged(true) invocation only; native mode-event delivery was not exercised.`
- Realtime: `Direct HandleTurnBasedModeStateChanged(false) invocation only; native mode-event delivery was not exercised.`
- Save: `Direct GuardBoundary(SaveRequested) service invocation only; stock SaveRoutine and serialization were not exercised.`
- Load: `Real Game.LoadGame of the exact Working descriptor exercised the native LoadRoutine prefix; no UI load request was exercised.`
- Area: `Direct OnAreaBeginUnloading cleanup was latched before real Game.ReloadArea; native area-event delivery was not independently observed or qualified.`

## Final row disposition

The mission's 25 named rows are frozen for F0 as:

```text
22 PASS
0 attributable FAIL
3 DEFER — EVIDENCED
```

The three F0 deferrals are not kill-criterion failures. No K1–K12 criterion fired. They also cannot be silently carried forward as F1 results: F1 currently has zero runtime scenarios. The protected-save blocker is resolved, so doorway, selection, and formation may now run under the new authority.

## Evidence needed to complete

The revised project-owned Working fixture and the user-attested Auto/Quick protected baseline are admitted. Commit and publish this safety checkpoint, package the exact clean evidence HEAD, then run the following rows under the existing guard, unchanged thresholds, strict A/B evidence, and exact restoration checks:

1. `mounted-pair-doorway`, including the exact same-Mammoth unmounted matched control;
2. `mounted-pair-selection`, away to the eligible third unit and back; and
3. `mounted-pair-party-formation`, using a meaningful stock group movement command.

Separately scope pose/animation work and UI/portrait/camera-follow capture; camera-only frames cannot prove those surfaces. A Phase 2 mission may be executed only after a new authorization.
