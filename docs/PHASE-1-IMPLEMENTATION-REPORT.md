# Phase 1 implementation report

Status: BLOCKED — CRITICAL

## Delivered default-off diagnostic implementation

- Independent .NET Framework 4.7/C# 7.3/AnyCPU UMM mod identity: `KingmakerMountedCombat` / `KingmakerMountedCombat.dll`, version `0.0.1-feasibility`.
- Explicit relationship coordinator with `Unmounted`, `Validating`, `Mounting`, `Mounted`, `Dismounting`, `Faulted`, and `Disposed` states; one-pair ownership, rollback, idempotent best-effort cleanup, and retryable residue handling.
- Exact Mammoth-only adapter requiring a distinct Medium directly controllable rider, exact reciprocal active companion, conscious/alive pair, larger current mount size, Default game mode, no combat, valid views/stock agents, and no pre-existing movement override.
- Mount-authoritative movement: the mount retains its exact stock pathfinding agent; the rider stock agent is stopped and disabled under one avoidance lease; one KMC-owned `RiderMovementAgent` synchronizes rider entity/view/anchor position and rotation.
- Pair-scoped command routing, stop/hold/cancel handling, selection normalization, and cleanup-boundary restoration. No global replacement of Kingmaker's movement tick, command system, selection manager, or formation helper was added.
- Reversible presentation attachment with exact transform snapshot/restore, global anchor/component residue detection, and structured position/yaw phase telemetry.
- Harmony12 and EventBus/UMM lifecycle integration for combat, life state, area unload, mode changes, save/load cleanup, disable, unload, session stop, and exception recovery.
- Default-off diagnostics, frame-driven lifecycle/movement/boundary engines, render-boundary screenshot capture, exact subscenario results, JSONL evidence, and artifact-manifest binding.
- Schema-v2 request/result contracts, canonical fixture guard, exact descriptor validation, Baseline immutability, Working-only authorization, transactional Working recovery, DotNetZip artifact quarantine, and exact external restoration.
- Boundary evidence is append-only and durable per record. It records exact request/post-initial/pre-dispatch/current file identity, descriptor identity, authorization counters, relationship authority, synchronous cleanup primitives, loading progression, and fresh-world residue state.
- Active-load failures are latched until Kingmaker's loading pipeline stops; CompositionRoot suppresses movement telemetry and pair validation while that drain is active. The engine never reopens the Working archive during an active load phase.

This remains a movement-only feasibility prototype. It adds no mounted attacks, combat action economy, charge, feats, enemy riders, persistence, broad animation system, or public-release surface.

## Qualified artifact

- Evidence/source commit: `fc7215481acf97ce1863eb1c75b3433889d2af7d`.
- Diagnostic ZIP: `artifacts/KingmakerMountedCombat-0.0.1-feasibility-diagnostic.zip`.
- ZIP SHA-256: `2c47d7ad4e82942bd303ab848ab82b9163a76ba9db79d52aaf8bfb0f028d80fc`.
- Manifest SHA-256: `89cd9b94dcbe3c9aa98a28a49a5516e31da98c351cb020d9b6f08de8e79a3c75`.
- DLL SHA-256: `202701ed9232e4f5a4d5ff65c468c684d1ed1dd53e4a8030be93b960a7cd202a`.
- DLL MVID: `a702808c-e8a0-4755-bc24-5ed4e945866a`.

The package contains only the project-owned `Info.json` and `KingmakerMountedCombat.dll`; it includes no game DLL, dependency DLL, save, runtime evidence, credential, or proprietary asset.

## Runtime qualification

The canonical Baseline/Working fixture pair passed the pre-open filename audit and project-owned descriptor guard. Only Working was load/write-authorized; Baseline remained immutable. The exact live Mods context was cloned transactionally, KMC was overlaid, and every admitted run restored Mods, Working, protected-save metadata, locks, and sentinels exactly.

Lifecycle A/B each passed eight rows and `339/0` assertions. Pair-only movement A/B passed:

| Row | Pass A | Pass B | Assertions per run |
|---|---|---|---:|
| Pause/unpause | `20260814T053000Z-pause-passA-qualified` | `20260814T054800Z-pause-passB-qualified` | `49/0` |
| Destination cancel | `20260814T060500Z-cancel-passA-qualified` | `20260814T062200Z-cancel-passB-qualified` | `49/0` |
| Open ground | `20260814T064000Z-open-ground-passA-qualified` | `20260814T065700Z-open-ground-passB-qualified` | `47/0` |
| Stop/start | `20260814T073500Z-stop-start-recovery-passA` | `20260814T075000Z-stop-start-recovery-passB` | `61/0` |
| Turns/corners | `20260814T081500Z-turns-corners-passA` | `20260814T083000Z-turns-corners-passB` | `74/0` |

The repaired stop/start runs distinguish a benign aligned LateUpdate lag from a real stationary-boundary failure without weakening any threshold or schema.

## Boundary qualification and claim scope

The fixed artifact is `boundary-scenario-evidence.jsonl`; JSON `artifactKind` is `boundary-scenario-evidence`, and manifest kind is `boundary-evidence`. The exact suite order is turn-based entry, realtime entry, save safety, load safety, and area transition. Every assertion failure terminates the current row and suppresses subsequent rows; no failed row can be followed by an executed row.

| Evidence ID | Runtime result | Exact supported claim |
|---|---:|---|
| `20260814T090000Z-boundary-tb-pass` | `56/0` | `Direct HandleTurnBasedModeStateChanged(true) invocation only; native mode-event delivery was not exercised.` |
| `20260814T091500Z-boundary-rt-pass` | `56/0` | `Direct HandleTurnBasedModeStateChanged(false) invocation only; native mode-event delivery was not exercised.` |
| `20260814T093000Z-boundary-save-pass` | `59/0` | `Direct GuardBoundary(SaveRequested) service invocation only; stock SaveRoutine and serialization were not exercised.` |
| `20260814T094500Z-boundary-load-pass` | `44/0` | `Real Game.LoadGame of the exact Working descriptor exercised the native LoadRoutine prefix; no UI load request was exercised.` |
| `20260814T100000Z-boundary-area-pass` | `44/0` | `Direct OnAreaBeginUnloading cleanup was latched before real Game.ReloadArea; native area-event delivery was not independently observed or qualified.` |

Boundary aggregate: `259 PASS / 0 FAIL` assertions. Each run separately passed request `31/0`, game result `39/0`, and final result `29/0` validation. Save safety invoked no stock `SaveRoutine` and authorized no write. Load safety authorized exactly the second Working load and proved loading start, stop, callback, new-world campaign identity, and zero KMC residue. Area safety latched direct cleanup before real `ReloadArea` and proved clean stable-world completion.

## Verification gates

- Source validation: `21 PASS / 0 FAIL`.
- Release build: PASS, zero warnings/errors.
- Pure/component tests: `112 PASS / 0 FAIL`.
- Visual capture tests: `12 PASS / 0 FAIL`.
- Guarded harness/protocol tests: `105 PASS / 0 FAIL`.
- Assembly-backed checks: `69 PASS / 0 FAIL`.
- Package validation and guarded WhatIf purity: PASS.
- Independent producer/validator integration audit: no remaining P0/P1 finding.

## Not delivered or claimed

The doorway row is not qualified because native combat contaminated the available matched-control attempt. Selection and formation are not qualified because the current fixture has no eligible directly controllable non-pair unit. Camera-only screenshots do not prove UI/portrait selection or camera-follow behavior. Presentation is mechanically viable but needs new pose/animation work.

The final named-row ledger is `22 PASS / 0 attributable FAIL / 3 DEFER — EVIDENCED`. No K1–K12 kill criterion fired, but mandatory §27.7 coverage is incomplete. The implementation is not a public release, Phase 1 is not complete, and Phase 2 execution is not authorized.
