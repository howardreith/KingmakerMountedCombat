# Phase 1 qualification

Status: BLOCKED — CRITICAL

Evidence commit: `fc7215481acf97ce1863eb1c75b3433889d2af7d`

Qualified package: SHA-256 `2c47d7ad4e82942bd303ab848ab82b9163a76ba9db79d52aaf8bfb0f028d80fc`; DLL SHA-256 `202701ed9232e4f5a4d5ff65c468c684d1ed1dd53e4a8030be93b960a7cd202a`; DLL MVID `a702808c-e8a0-4755-bc24-5ed4e945866a`.

## Gate ledger

| Gate | Result | Evidence |
|---|---|---|
| Standalone identity/separation | PASS | Independent repo/branch/assembly/UMM ID/namespace/version; no foreign gameplay dependency or prohibited payload |
| Environment/provenance | PASS | Exact Kingmaker `2.1.7b`, UMM `0.28.2.0`, Harmony12, Unity, and Wrath hashes/MVIDs |
| .NET Framework 4.7 / C# 7.3 build | PASS | Release build with zero warnings/errors; Copy Local disabled for local game/tool references |
| Wrath responsibility map | PASS | 21 matrix responsibilities and exact bounded local contracts |
| Kingmaker equivalence map | PASS | 15 machine-readable responsibilities and narrow control points |
| Native candidate/rig | PASS | Horse rejected; exact active larger Mammoth, live views/stock agents, and `Spine` anchor verified |
| Canonical fixture guard | PASS | Exactly Baseline=1, Working=1, near-match=0; distinct non-linked paths, exact internal names, shared campaign/area identity |
| Baseline/Working safety | PASS | Baseline immutable; only exact Working authorized; transactional restore and quarantine verified |
| Runtime harness/WhatIf | PASS | Source `21/0`, component `112/0`, visual `12/0`, harness `105/0`, assembly-backed `69/0`; WhatIf and package gates PASS |
| Relationship/lifecycle | PASS | Two fresh lifecycle suites, each `8/8` rows and `339/0` assertions, with direct-handler scope retained |
| One-mover movement | PASS | Open-ground A/B, exact mount authority, bounded synchronization, clean stop/dismount, no residue |
| Stop/start | PASS | Repaired A/B each `61/0`; exact stationary-boundary reconciliation without threshold weakening |
| Turns/corners | PASS | A/B each `74/0`; substantial turns/reversals and bounded synchronization |
| Pause/unpause | PASS | A/B each `49/0`; authority and destination stable across pause |
| Destination cancel | PASS | A/B each `49/0`; effective pair motion and commands stopped cleanly |
| Doorway matched control | DEFER — EVIDENCED | Native combat contaminated the available attempt; no combat-neutral matched control is obtainable from the current fixture |
| Selection away/back | DEFER — EVIDENCED | Current fixture has no eligible directly controllable non-pair unit; no synthetic substitute used |
| Party formation | DEFER — EVIDENCED | Same missing third-unit prerequisite prevents a meaningful stock group-command test |
| Turn-based boundary | PASS | `20260814T090000Z-boundary-tb-pass`, `56/0`; direct handler only |
| Realtime boundary | PASS | `20260814T091500Z-boundary-rt-pass`, `56/0`; direct handler only |
| Save safety | PASS | `20260814T093000Z-boundary-save-pass`, `59/0`; direct guard, zero stock SaveRoutine/write |
| Load safety | PASS | `20260814T094500Z-boundary-load-pass`, `44/0`; real exact Working load and native prefix, clean fresh world |
| Area transition | PASS | `20260814T100000Z-boundary-area-pass`, `44/0`; direct pre-clean followed by real ReloadArea |
| Visual classification | PASS | `MECHANICALLY VIABLE, NEW ANIMATION/POSE WORK REQUIRED`; no visual kill criterion fired, while black-frame and camera-only coverage limitations remain |
| External restoration | PASS | Every admitted run restored exact Mods, Working, protected-save metadata, process, lock, sentinel, and live KMC state |
| Architecture disposition | BLOCKED — CRITICAL | B is provisional product-fit leader; mandatory §27.7 doorway/selection/formation evidence is incomplete |

## Boundary reconciliation

The five boundary rows total `259 PASS / 0 FAIL` runtime assertions. Each individual run also passed strict request validation `31/0`, game-result validation `39/0`, and final-result validation `29/0`. The evidence binds the exact commit/package/DLL, exact Working descriptor and file identities, per-row authorization counters, relationship state, cleanup latch, loading transitions, fresh-world campaign identity, and final external restoration.

Claim scope remains deliberately narrow:

- Turn-based: `Direct HandleTurnBasedModeStateChanged(true) invocation only; native mode-event delivery was not exercised.`
- Realtime: `Direct HandleTurnBasedModeStateChanged(false) invocation only; native mode-event delivery was not exercised.`
- Save: `Direct GuardBoundary(SaveRequested) service invocation only; stock SaveRoutine and serialization were not exercised.`
- Load: `Real Game.LoadGame of the exact Working descriptor exercised the native LoadRoutine prefix; no UI load request was exercised.`
- Area: `Direct OnAreaBeginUnloading cleanup was latched before real Game.ReloadArea; native area-event delivery was not independently observed or qualified.`

## Final row disposition

The mission's 25 named rows close as:

```text
22 PASS
0 attributable FAIL
3 DEFER — EVIDENCED
```

The three deferrals are not kill-criterion failures. No K1–K12 criterion fired. However, doorway, selection, and formation are mandatory §27.7 evidence, so they cannot be silently omitted or converted to PASS. Phase 1 therefore remains `BLOCKED — CRITICAL`.

## Evidence needed to unblock

Prepare a project-owned Working fixture that provides both:

1. a combat-neutral doorway/corner route where the same current Mammoth can complete an exact unmounted control before mounted traversal; and
2. one eligible directly controllable non-pair unit for away/back selection and two-recipient formation.

Then rerun only the three missing rows under the existing guard, unchanged thresholds, strict A/B evidence, and exact restoration checks. Separately scope pose/animation work and UI/portrait/camera-follow capture; camera-only frames cannot prove those surfaces. A Phase 2 mission may be executed only after a new authorization.
