# Autonomous blockers

Status: PHASE 1 COMPLETE — PROCEED RECOMMENDED

Branch: `codex/mounted-combat-feasibility`

Active safety implementation commit: `6abc293f12bacbc250fc4c012fbced05b3763881`

## Evidence epochs

F0 remains the frozen runtime-evidence epoch at `fc7215481acf97ce1863eb1c75b3433889d2af7d`, with `22 PASS / 0 attributable FAIL / 3 DEFER — EVIDENCED`. F1 is the intentionally revised Working fixture. It independently supersedes the three F0 deferrals with six fresh-process PASS results, producing the final `25 PASS / 0 attributable FAIL / 0 DEFER` ledger.

## Cleared blocker: F1 descriptor and Baseline identity

Status: PASS

Canonical audit is Baseline 1 / Working 1 / near-match 0. Baseline remains immutable at SHA-256 `c29d965c9ff5dc0f971659d9ae154877aa4a9a461ca220d1ce28e7c7fd9d2512`, length `686605`, ticks `639222474845172002`. Revised Working is SHA-256 `5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6`, length `816452`, ticks `639222975870964407`. The descriptors have exact internal names `KMC_AUTOMATION_BASELINE` / `KMC_AUTOMATION_WORKING`, distinct paths, Manual/v1 type, shared GameName `cvb`, GameId `d41185be-edc1-47c0-b9d5-e1d7a9c8e65f`, and Area `9d1278a2f599b2a4daab53abdfe88d2e`. Only Working is write-authorized.

Qualification SHA-256 is `95e9a3c6203f11cfdd7e615849eb61ebc060131075f6a7f40771dcd7eeb68a7a`, length `1272`, ticks `639223044028647586`. Requalification transaction `fixture-requalification-20260814T1140026497594Z` is committed and preserved its then-current inventory before/after at digest `973f203e16aad29d7e4865bae6f81149d959a4ac179953c87781a06d1c71898d`, `271` files, `3178473776` bytes.

## Cleared blocker: user-attested protected-save epoch

Status: PASS

The frozen F0-to-current metadata comparison has exactly three changed entries and no additions/removals:

| Entry | Authority | F0 length / ticks | F1 length / ticks | F1 SHA-256 |
| --- | --- | ---: | ---: | --- |
| `Manual_299_KMC_AUTOMATION_WORKING.zks` | Existing Working-only write authorization | `718821` / `639222573406912936` | `816452` / `639222975870964407` | `5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6` |
| `Auto_1120.zks` | User-authorized protected baseline only; never project-writable | `204829` / `639222474628040540` | `333208` / `639222975467301685` | `9599e9e15bdd04cde7b15d47795510b95cd8e1333f107f2e3b64469ebf3330fc` |
| `Quick_438.zks` | User-authorized protected baseline only; never project-writable | `625411` / `639220694761623881` | `809565` / `639222975512345112` | `e26dc2ce00e6ed68953c2c9b1e89584ae3c3bd50f4cf698ecc2c624e21d10575` |

The user attested that the Auto/Quick changes were external Kingmaker activity during manual F1 fixture preparation, not KMC transaction writes. No other non-KMC entry changed. The project did not extract, deserialize, or inspect either archive internally; it recorded only permitted raw SHA-256, length, and timestamp.

The new append-only authority is `C:\Dev\KingmakerMountedCombatLab\runtime-state\protected-save-authorities\20260814T1445257441387Z-user-fixture-preparation.json`, epoch `20260814T1445257441387Z-user-fixture-preparation`, authorized at `2026-08-14T14:46:34.9125219+00:00`, SHA-256 `77fd415d177423dd7da58e3685e418f38cf954dead75068424a717e2be6b3ca9`, length `119319`, ticks `639223155954515237`. It binds current inventory digest `973f203e16aad29d7e4865bae6f81149d959a4ac179953c87781a06d1c71898d`, `271` files, `3178473776` bytes, and retains F0 as the authority for every unchanged entry.

WhatIf purity, append-only creation, and independent continuity validation passed. Subsequent KMC transactions must preserve exact Auto/Quick hashes and metadata and every other non-Working metadata entry. Any drift stops runtime again.

## Cleared blocker: fail-closed launcher enforcement

Status: PASS

Commit `6abc293f12bacbc250fc4c012fbced05b3763881` changes `scripts/runtime/RuntimeHarness.Common.ps1`, adds `scripts/runtime/New-KmcProtectedSaveContinuityAuthority.ps1`, and changes `scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1` plus `scripts/Test-Harness.ps1`. The launcher requires explicit authority/path/epoch/hash and Auto/Quick name/hash pins for every save-backed run; it verifies them before ShouldProcess, again in WhatIf, and again while holding the runtime lock before staging or launching.

Exact file SHA-256 values are `59e24b0156f3f8c44ebd99c7bb403952108a7cef505bf55d633ca173f47d0ee1`, `9a5b983a7863b54457bbb4b869a5346b8d41d5744f901536ded647fb8127b2f3`, `b425fce49d329ccea6cba04dd423c242a656d4aff15ed63234f248c559db16d4`, and `cd6f3aa14e5c08211cc39d6e51fe86e304140993e137a5b8ed2c66fd1a41cf8`. Gates passed AST `4/4`, source `21/0`, Release build, component `112/0`, visual `12/0`, harness `134/0`, assembly-backed `69/0`, and diff check. DLL remains SHA-256 `202701ed9232e4f5a4d5ff65c468c684d1ed1dd53e4a8030be93b960a7cd202a`, MVID `a702808c-e8a0-4755-bc24-5ed4e945866a`.

## Cleared blocker: mandatory F1 qualification

Status: PASS

The frozen F0 ledger remains preserved for provenance. The three missing rows each passed twice in fresh processes under unchanged gates:

1. `mounted-pair-doorway`: `20260814T150000Z-doorway-passA` / `20260814T151500Z-doorway-passB`, `60/0` each; exact same-current-Mammoth unmounted matched control, then 2/2 strict mounted legs.
2. `mounted-pair-selection`: `20260814T153000Z-selection-passA` / `20260814T154500Z-selection-passB`, `54/0` each; exact real third-unit away/back selection and routed movement.
3. `mounted-pair-party-formation`: `20260814T160000Z-formation-passA` / `20260814T161500Z-formation-passB`, `58/0` each; meaningful stock group movement with target arrival, clearance, and isolation.

The six processes total `344/0` runtime assertions. Every process passed strict request/game/final validation at `31/0`, `39/0`, and `29/0`; all 18 transaction states are restored; protected inventory and live Mods are exact; no process, lock, sentinel, or KMC deployment remains.

## Visual and architecture state

Status: PASS

The visual classification remains `MECHANICALLY VIABLE, NEW ANIMATION/POSE WORK REQUIRED`. No K1–K12 kill criterion fired. Architecture B is selected with final A/B/C/D scores `41/71/77/89`; C and D remain documented pivots if later evidence fires a criterion. Phase 2 and a public release remain unauthorized.

## Active Phase 1 blockers

Status: PASS

No active Phase 1 evidence, safety, restoration, or publication blocker remains. The rider still needs original pose/animation work, but that is a scoped Phase 2 prerequisite rather than a Phase 1 kill or evidence gap.

## Exact next action

No further Phase 1 action is pending. Closure commit `05a21e0f4607e2177b5e394f4f6e470eb83c1e7b` was published by the guarded non-force helper and verified at the remote. Preserve the d5bd-bound diagnostic artifact and protected-save pins, await a separate user-approved Phase 2 mission if desired, and do not create a public release.
