# Risk and kill criteria

Status: BLOCKED — CRITICAL

No architecture kill criterion has fired. A separate external-state hard stop under mission §26.2 prevents the fixture-backed movement and lifecycle qualification needed to decide Architecture B versus a C/D pivot.

Current offline gate ledger: source 21 PASS / 0 FAIL; pure/component 56 PASS / 0 FAIL; guarded harness/protocol 58 PASS / 0 FAIL; assembly-backed 47 PASS / 0 FAIL (Kingmaker 36, Wrath 11). Runtime mission rows remain 1 PASS / 0 FAIL / 24 DEFER — EVIDENCED, with zero mounted-pair samples.

| ID | Risk / kill criterion | Final Phase 1 evidence | Status | Remaining control |
|---|---|---|---|---|
| K1 | Two active nav agents collide, oscillate, or diverge | Default-off adapter stops/disables rider `AgentASP`, owns one avoidance lease, and leaves mount stock movement authoritative; no mounted runtime sample exists | IN PROGRESS | Fixture-backed movement telemetry |
| K2 | No scoped authoritative-mover control point | Exact `AgentOverride`, stock rider suppression, `CountingGuard`, and private command-recipient rewrite seams are proven | PASS | Retain exact MVID/token guards |
| K3 | Pair fails valid turns/doorways passed by unmounted control | No qualifying fixture; no traversal run | DEFER — EVIDENCED | Matched Mammoth control and mounted run |
| K4 | Selection/formation cannot restore | Public selection seams and pair-scoped routing exist; deterministic command decisions pass; game behavior unmeasured | IN PROGRESS | Fixture selection/formation suite |
| K5 | Cleanup leaves movement, avoidance, selection, command, or view residue | Coordinator, rollback, retryable ownership, best-effort cleanup, and deterministic cleanup tests pass; game residue unmeasured | IN PROGRESS | Runtime lifecycle and residue telemetry |
| K6 | Broad global movement patch affects other units | Eight exact guarded entry/control patches, exact MVID gate, 47 assembly-backed checks (Kingmaker 36, Wrath 11), no movement-tick replacement; no party-area isolation run | IN PROGRESS | Fixture non-mounted-isolation proof |
| K7 | Save/load/area retains half-mounted state | Runtime-only domain; save/load prefixes clean and block on residue; no relationship JSON. Boundary engines are offline-qualified, but mode/lifecycle rows call handlers directly, area transition pre-cleans before real reload, and save-safety deliberately performs no stock save | IN PROGRESS | Working/Baseline fixture boundary suite plus claim-scoped live evidence |
| K8 | Presentation requires Wrath assets | No Wrath asset/code dependency or payload exists; whether acceptable presentation needs unavailable assets is not knowable without visual review | IN PROGRESS | Native Mammoth-only visual review |
| K9 | No native candidate is remotely plausible | Riding horse rejected as non-companion; rank-7+ Mammoth has exact AddPet linkage and bounded `Spine` anchor hypothesis; no rider pose evidence | IN PROGRESS | Fixture-backed anchor/clipping capture |
| K10 | Fresh-process tests remain nondeterministically unstable | Scaffold smoke passes twice consecutively from exact commit/package; movement suite did not run | IN PROGRESS | Two-pass lifecycle/movement suite |
| K11 | Solution depends on Wrath runtime assembly | Source/package allowlists and exact references prove Kingmaker/UMM/Harmony12 only | PASS | Preserve validators |
| K12 | External state cannot be restored exactly | WhatIf purity passes; two repaired failure transactions and two final PASS transactions all restored their exact transaction-baseline Mods digest `e62320...`; no protected-save mutation | PASS | Preserve token/sentinel/quarantine/stable-exit guards |

## Proven critical hard stop

Status: BLOCKED — CRITICAL

The exact filename audit reports one canonical Baseline candidate, `Manual_298_KMC_AUTOMATION_BASELINE.zks`, zero canonical Working candidates, and one rejected trailing-underscore near-match, `Manual_299_KMC_AUTOMATION_WORKING_.zks`. The near-match is not authorization to inspect, rename, copy, or load it. Existing KBP/KMG fixtures belong to other projects and remain prohibited substitutes. The filename-first gate stopped before either KMC archive or any valued save archive was opened.

The KMC-only filename-prefiltered descriptor guard, schema-v2 host, exact Working loader, proactive Working-only load authorization, all-stock-save denial, combined Mods/Working transaction, and token-owned recovery are implemented and offline-qualified. They cannot inspect descriptors, create durable fixture qualification, or launch while the exact filename gate remains Baseline=1 / Working=0 with a rejected KMC-looking near-match.

This proves mission §26.2: an exact eligible Baseline/Working pair cannot be established. It blocks movement, lifecycle, doorway, selection, formation, boundary, drift, and visual evidence, so no truthful Phase 2 architecture recommendation can be issued.

Boundary claim scope is intentionally narrow. Direct lifecycle-handler probes test KMC cleanup behavior but do not prove real EventBus/controller delivery ordering. The area-transition row pre-invokes cleanup and then uses real `ReloadArea(AutoSaveMode.None)`. The save-safety row proves cleanup, unchanged Working bytes, zero stock-save requests, and no custom relationship serialization; it does not invoke or qualify Kingmaker's unsafe temporary-leaf `SaveRoutine` path.

Exact first safe command after project-owned fixtures are made available:

```powershell
Get-ChildItem -LiteralPath 'C:\Users\Howie\AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\Saved Games' -File | Where-Object Name -Match '^Manual_[0-9]+_KMC_AUTOMATION_(BASELINE|WORKING)\.zks$' | Select-Object Name,Length,LastWriteTimeUtc
```
