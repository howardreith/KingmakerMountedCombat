# Risk and kill criteria

Status: IN PROGRESS

No architecture kill criterion fired. Architecture B's frozen F0 runnable pair-only movement, lifecycle, cleanup, boundary, native-asset, and external-restoration evidence is positive. The user-attested protected-save epoch has cleared the F1 safety admission gate without granting any non-Working write authority. The three mandatory Proceed rows remain downstream and unexecuted.

Frozen F0 gate ledger: source 21 PASS / 0 FAIL; Release build PASS; pure/component 112 PASS / 0 FAIL; visual-capture contracts 12 PASS / 0 FAIL; guarded harness/protocol 105 PASS / 0 FAIL; assembly-backed 69 PASS / 0 FAIL (Kingmaker 58, Wrath 11). The F0 named runtime-row disposition is 22 PASS / 0 attributable FAIL / 3 DEFER — EVIDENCED. Historical runner, validator, capture, and instrumentation failures remain preserved but are not architecture kills. F1 now has descriptor and protected-save admission but no runtime row result.

Protected-save authority implementation commit `6abc293f12bacbc250fc4c012fbced05b3763881` is on `codex/mounted-combat-feasibility`. Its exact four-file scope is `scripts/runtime/RuntimeHarness.Common.ps1`, new `scripts/runtime/New-KmcProtectedSaveContinuityAuthority.ps1`, `scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1`, and `scripts/Test-Harness.ps1`, with SHA-256 `59e24b0156f3f8c44ebd99c7bb403952108a7cef505bf55d633ca173f47d0ee1`, `9a5b983a7863b54457bbb4b869a5346b8d41d5744f901536ded647fb8127b2f3`, `b425fce49d329ccea6cba04dd423c242a656d4aff15ed63234f248c559db16d4`, and `cd6f3aa14e5c08211cc39d6e51fe86e304140993e137a5b8ed2c66fd1a41cf8`. AST `4/4`, source `21/0`, Release build, component `112/0`, visual `12/0`, harness `134/0`, assembly-backed `69/0`, and diff check passed.

## Evidence epochs and current safety gate

F0 is frozen at runtime-evidence commit `fc7215481acf97ce1863eb1c75b3433889d2af7d` and Working SHA-256/length/ticks `a5f7a7fb77f0465df1591360ecd1730e2c28215d83e27aafc70efef3110e6dc5` / `718821` / `639222573406912936`.

F1 was descriptor-requalified at execution HEAD `15bbf0e2029ebc43d8bada48b83b4f55d43f8db0` by committed transaction `fixture-requalification-20260814T1140026497594Z`, qualified at `2026-08-14T11:40:02.8457604+00:00`. Qualification SHA-256/length/ticks are `95e9a3c6203f11cfdd7e615849eb61ebc060131075f6a7f40771dcd7eeb68a7a` / `1272` / `639223044028647586`; phase-`committed` state SHA-256/length/ticks are `68fdc0f759e7f7daffaffaeb27996357e74c8f5861c5bf9345005edbefa66cce` / `1707` / `639223044029797599`; exact prior F0 qualification backup SHA-256/length are `c1e33c75004212039258d6d90ba20e43c37c50966c94e685dbad1ca0e653654f` / `1272`.

The F1 audit is exactly Baseline 1 / Working 1 / near-match 0. Baseline is unchanged at `c29d965c9ff5dc0f971659d9ae154877aa4a9a461ca220d1ce28e7c7fd9d2512` / `686605` / `639222474845172002`; revised Working alone is descriptor-qualified at `5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6` / `816452` / `639222975870964407`. Exact names, distinct paths, Manual/v1, and common GameName `cvb`, GameId `d41185be-edc1-47c0-b9d5-e1d7a9c8e65f`, and Area `9d1278a2f599b2a4daab53abdfe88d2e` passed; only Working is writable. Requalification preserved its then-current save inventory before/after at `973f203e16aad29d7e4865bae6f81149d959a4ac179953c87781a06d1c71898d`, `271` files, `3178473776` bytes. Normal guard and already-committed recovery `-WhatIf` passed. No process, lock, sentinel, live KMC tree, or transaction debris remains.

Frozen F0 inventory was digest `58a54974423316041e29d32b1093c5d6775dbd67fe2b37ffaea3c9ae27de19be`, `271` files, `3178063612` bytes. Current F1 differs in exactly three entries and no others: authorized Working `718821` / `639222573406912936` to `816452` / `639222975870964407`; user-attested protected Auto `204829` / `639222474628040540` to `333208` / `639222975467301685`; user-attested protected Quick `625411` / `639220694761623881` to `809565` / `639222975512345112`. The user attested that Auto and Quick changed in the external Kingmaker fixture-preparation session. They are now baselined but remain non-writable.

Append-only authority `C:\Dev\KingmakerMountedCombatLab\runtime-state\protected-save-authorities\20260814T1445257441387Z-user-fixture-preparation.json`, epoch `20260814T1445257441387Z-user-fixture-preparation`, SHA-256 `77fd415d177423dd7da58e3685e418f38cf954dead75068424a717e2be6b3ca9`, binds current inventory digest `973f203e16aad29d7e4865bae6f81149d959a4ac179953c87781a06d1c71898d`, Auto SHA-256 `9599e9e15bdd04cde7b15d47795510b95cd8e1333f107f2e3b64469ebf3330fc`, and Quick SHA-256 `e26dc2ce00e6ed68953c2c9b1e89584ae3c3bd50f4cf698ecc2c624e21d10575`. WhatIf, append-only creation, and independent continuity validation passed with no save mutation or residue.

| ID | Risk / kill criterion | Final Phase 1 evidence | Status | Remaining control |
|---|---|---|---|---|
| K1 | Two active nav agents collide, oscillate, or diverge | Open ground, stop/start, turns/corners, pause/unpause, and destination-cancel passed A/B with the Mammoth as the sole authoritative mover; zero oscillation, stuck, unexpected-repath, command-replacement, synchronization-violation, or cleanup-residue counts | PASS | Preserve one-mover and fixed synchronization gates |
| K2 | No scoped authoritative-mover control point | Exact `AgentOverride`, stock rider suppression, `CountingGuard`, private command-recipient rewrite, and live routed movement are proven without a global tick replacement | PASS | Retain exact MVID/token/active-pair guards |
| K3 | Pair fails valid turns/doorways passed by unmounted control | F0 turns/corners passed A/B. Its unmounted Mammoth passed the doorway control and the mounted route was accepted and stable; native hostiles then triggered correct `CombatStarted` cleanup before a corrected repeat completed. This is F0 fixture contamination, not an attributable traversal failure. F1 is admitted with no doorway result yet | DEFER — EVIDENCED | Exact same-Mammoth unmounted control and mounted traversal in two fresh F1 processes |
| K4 | Selection/formation cannot restore | F0 pair selection retention and every cleanup restoration passed, but away/back and group formation could not execute because F0 had no eligible directly controllable non-pair; no restoration failure occurred. F1 is admitted with no selection/formation result yet | DEFER — EVIDENCED | Run selection and meaningful stock group formation A/B against F1 Working |
| K5 | Cleanup leaves movement, avoidance, selection, command, or view residue | Lifecycle A/B passed 8/8 rows and 339/0 assertions each. All qualified movement and boundary rows ended Unmounted with stock agents, avoidance, overrides/components, `ForbidRotation`, attachment parent/lease, commands, pause, and selection restored without residue | PASS | Preserve direct-handler claim labels and residue validators |
| K6 | Broad global movement patch affects other units | Eight exact active-pair guards and no movement-tick replacement prove B does not require a broad global patch. No material non-pair mutation was observed in F0, but F0's absent third unit prevented the mandatory formation/isolation runtime proof | PASS | Retain narrow guards; complete F1 non-pair formation/isolation A/B before Proceed |
| K7 | Save/load/area retains half-mounted state | All five individual boundary rows passed with exact evidence and restoration. Save is cleanup-only with no stock save/serialization; load performs real exact-Working `Game.LoadGame` through the native prefix; area directly pre-cleans before real `ReloadArea`. No relationship reconstructed and no old/fresh-world residue remained | PASS | Do not broaden claims to native TB/RT/area delivery, stock save round-trip, persistence, or uninstall |
| K8 | Presentation requires Wrath assets | Original code plus the native Kingmaker Mammoth produces a readable mounted silhouette; no Wrath code, runtime assembly, model, clip, controller, material, texture, or offset is required or shipped | PASS | Any new pose/animation work must remain original or Kingmaker-native |
| K9 | No native candidate is remotely plausible | Usable camera frames classify the pair `MECHANICALLY VIABLE, NEW ANIMATION/POSE WORK REQUIRED`. The standing/intersecting pose is not polished, but it is remotely plausible and explicitly does not fire K9 | PASS | New seated pose/animation and clipping review in a separately authorized phase |
| K10 | Fresh-process tests remain nondeterministically unstable | Lifecycle passed A/B; each runnable pair-only movement row passed A/B from its same clean commit/package. Earlier failures were bounded, explained instrumentation/harness defects with regressions; fixture unavailability is deterministic | PASS | Preserve same-commit/package repeatability and narrow endpoint margin |
| K11 | Solution depends on Wrath runtime assembly | Source/package allowlists and exact references prove Kingmaker/UMM/Harmony12 only | PASS | Preserve source/package/assembly validators |
| K12 | External state cannot be restored exactly | Every F0 live transaction restored exact live Mods and save metadata. F1 requalification and protected-save authority creation changed no save, kept Baseline byte-identical, and left no process or transaction residue. Auto/Quick hashes and metadata plus every other protected entry are now mandatory runtime invariants | PASS | Prove exact protected continuity and transaction restoration after every F1 process |

There is no K13. The stop/start recovery false negative was an evidence-state-machine defect: two corrected A/B passes proved the unchanged thresholds and recovery contract. It did not create a new kill criterion.

## Claim-scoped boundary qualification

The following individual runs are PASS:

- `20260814T090000Z-boundary-tb-pass`: 1 PASS / 0 FAIL rows, 56/0 assertions; direct turn-based lifecycle-handler cleanup only, with native delivery false.
- `20260814T091500Z-boundary-rt-pass`: 1 PASS / 0 FAIL rows, 56/0 assertions; direct real-time lifecycle-handler cleanup only, with native delivery false.
- `20260814T093000Z-boundary-save-pass`: 1 PASS / 0 FAIL rows, 59/0 assertions; direct `GuardBoundary(SaveRequested)` cleanup, zero stock `SaveRoutine` or serialization.
- `20260814T094500Z-boundary-load-pass`: 1 PASS / 0 FAIL rows, 44/0 assertions; real exact-Working `Game.LoadGame` plus native `LoadRoutine`-prefix authorization, clean fresh world, no UI-load claim.
- `20260814T100000Z-boundary-area-pass`: 1 PASS / 0 FAIL rows, 44/0 assertions; direct `OnAreaBeginUnloading` cleanup latch before real `ReloadArea(AutoSaveMode.None)`, with native area-event delivery not independently qualified.

These passes close K7 only for the Phase 1 runtime-only/no-save design. They do not establish persistence, stock save round-trip, native mode/area event delivery, or uninstall behavior.

## Active completion gate

Status: IN PROGRESS

Fixture descriptor identity and protected-save continuity are not blockers. The F1 exact audit is Baseline=1 / Working=1 / KMC-looking near-match=0, with distinct paths, exact internal names, shared `GameId`/`GameName`/`Area` identity, Baseline immutability, Working-only write authorization, and exact user-attested protected Auto/Quick pins.

The frozen F0 Working fixture could not supply three mandatory Proceed rows:

1. its only qualified doorway route is contaminated by native hostiles after the unmounted control passes and mounted traversal begins;
2. it contains no eligible directly controllable non-pair for selection away/back;
3. the same missing unit prevents a meaningful party-formation and non-mounted-isolation run.

All three remain F0 `DEFER — EVIDENCED`. They are not K3/K4 failures and therefore do not authorize the Pivot path, but mission §§18 and 27.7 do not allow them to be converted into Proceed passes. F1 admission does not reclassify them because no F1 scenario has executed. The visual classification also does not substitute for mechanical qualification. Architecture B remains the provisional product-fit leader and stays default-off; Phase 2 is not authorized.

The revised `KMC_AUTOMATION_WORKING` descriptor is qualified, Baseline remains immutable, and F1 is admitted under the exact protected-save authority. Exact next action: commit and publish this safety checkpoint, package the exact clean evidence HEAD, then run only doorway, selection, and formation A/B. Every process must preserve Auto/Quick and all other protected saves exactly. Phase 2 remains outside authorization.
