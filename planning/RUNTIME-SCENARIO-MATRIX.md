# Runtime scenario matrix

Status: BLOCKED — CRITICAL

The guarded no-save scaffold is qualified. Fixture-backed scenarios cannot start because the exact filename audit currently reports one canonical Baseline candidate, zero canonical Working candidates, and one rejected trailing-underscore near-match: `Manual_299_KMC_AUTOMATION_WORKING_.zks`. The exact Baseline is `Manual_298_KMC_AUTOMATION_BASELINE.zks`. The filename-first gate stopped before either KMC archive was opened; other-project and personal fixtures remain prohibited.

Offline gate ledger: source validation 21 PASS / 0 FAIL; pure/component 56 PASS / 0 FAIL; guarded harness/protocol 58 PASS / 0 FAIL; assembly-backed 47 PASS / 0 FAIL (Kingmaker 36, Wrath 11). These are implementation and protocol qualifications, not fixture-backed runtime proof.

## Executed runtime evidence

| Scenario | Result | Exact evidence |
|---|---|---|
| `mod-load-smoke` | PASS twice in consecutive fresh processes | Commit `e3f71bc902d79c5be3f1a66c6b99396d94d39018`; package SHA-256 `2c77b677bc4af129ccc9e22d136b6e21754007e8ba8144ee2a2304d77fac10b9`; evidence IDs `20260813T185500Z-mod-load-smoke-passA` and `20260813T185700Z-mod-load-smoke-passB` |
| `mod-load-smoke` engineering attempt | FAIL, preserved and repaired | `20260813T184500Z-mod-load-smoke-pass1`: process-global JSON metadata and a UMM cache exposed two harness defects; exact recovery restored Mods |
| `mod-load-smoke` engineering attempt | FAIL, preserved and repaired | `20260813T185200Z-mod-load-smoke-fixed-pass1`: exact game result passed, then a post-exit process-enumeration race blocked restore; guarded recovery restored Mods |

Both final PASS runs proved Kingmaker `2.1.7b`, exact gameplay assembly hash/MVID, UMM `0.28.2.0`, Harmony12 `1.2.0.1`, KMC identity/hash/MVID, `Unmounted`, movement experiment disabled, mode `None`, no loaded area, zero save requests, zero load requests, no errors, stable process exit, protected-save metadata unchanged, and the transaction-before Mods digest restored.

## Required scenario disposition

| Group | Scenario | Save needed | Status | Evidence / remaining acceptance |
|---|---|---:|---|---|
| Scaffold | `mod-load-smoke` | No | PASS | Two same-commit fresh-process passes with exact restore |
| Forensics | `export-mounted-contracts` | Working fixture under the schema-v2 host | DEFER — EVIDENCED | Exact offline map plus 47 assembly-backed checks already provide the bounded contract evidence; a literal runtime export is not claimed as run |
| Forensics | `export-candidate-mount-rigs` | Working fixture for a meaningful live view | DEFER — EVIDENCED | Exact read-only native asset metadata is recorded; no-area prefab instantiation would not prove animation stability |
| Forensics | `observe-mount-diagnostic-availability` | Working fixture | DEFER — EVIDENCED | Requires an exact valid rider/Mammoth pair and diagnostic UI observation |
| Lifecycle | `mounted-pair-create-and-clear` | Working fixture | DEFER — EVIDENCED | State transitions plus zero movement/view/selection residue |
| Lifecycle | `mounted-pair-double-mount-rejected` | Working fixture | DEFER — EVIDENCED | Exact rejection and unchanged pair state |
| Lifecycle | `mounted-pair-invalid-pair-rejected` | Working fixture | DEFER — EVIDENCED | Validation reason and no mutation |
| Lifecycle | `mounted-pair-cleanup-idempotent` | Working fixture | DEFER — EVIDENCED | Repeated cleanup plus zero residue |
| Lifecycle | `mounted-pair-death-cleanup` | Working fixture | DEFER — EVIDENCED | Engine invokes the exact lifecycle handler directly, then checks next-frame restored agent/view state; EventBus delivery is not yet runtime-proven |
| Lifecycle | `mounted-pair-combat-start-cleanup` | Working fixture | DEFER — EVIDENCED | Engine invokes the exact lifecycle handler directly and requires cleanup before continuing; live EventBus ordering is not yet runtime-proven |
| Lifecycle | `mounted-pair-area-unload-cleanup` | Working fixture | DEFER — EVIDENCED | Direct area-unload handler probe requires no retained relationship, override, or avoidance lease; live EventBus delivery is not yet runtime-proven |
| Lifecycle | `mounted-pair-mod-disable-cleanup` | Working fixture | DEFER — EVIDENCED | Disable returns only after zero residue |
| Movement | `mounted-pair-open-ground` | Working fixture | DEFER — EVIDENCED | Destination, one mover, no oscillation, residual `<= 0.10` world unit |
| Movement | `mounted-pair-stop-start` | Working fixture | DEFER — EVIDENCED | Repeated start/stop and stationary drift wait |
| Movement | `mounted-pair-turns-and-corners` | Working fixture | DEFER — EVIDENCED | Substantial turn, repeated reversals, stuck/oscillation/repath counts |
| Movement | `mounted-pair-doorway` | Working fixture | DEFER — EVIDENCED | Same Mammoth/current size/path unmounted control; reached/rejection reason, stuck time, oscillations, repaths, maximum residual |
| Movement | `mounted-pair-selection` | Working fixture | DEFER — EVIDENCED | Switch away/back, exact selection loss count, restored selection |
| Movement | `mounted-pair-party-formation` | Working fixture | DEFER — EVIDENCED | One mover, no duplicate slot, no non-mounted party interference |
| Movement | `mounted-pair-pause-unpause` | Working fixture | DEFER — EVIDENCED | Stable state, command, and destination across pause |
| Movement | `mounted-pair-destination-cancel` | Working fixture | DEFER — EVIDENCED | Pair command and both effective representations stop |
| Boundary | `mounted-pair-turn-based-entry-cleanup` | Working fixture | DEFER — EVIDENCED | Direct turn-based lifecycle-handler probe requires clean dismount; a real controller/EventBus transition is not yet runtime-proven |
| Boundary | `mounted-pair-realtime-entry-cleanup` | Working fixture | DEFER — EVIDENCED | Direct realtime lifecycle-handler probe requires clean dismount; a real controller/EventBus transition is not yet runtime-proven |
| Boundary | `mounted-pair-save-safety` | Baseline + Working fixtures | DEFER — EVIDENCED | Proves mounted cleanup, unchanged Working bytes, zero stock-save requests, and absence of custom relationship serialization. It deliberately does not invoke unsafe `SaveRoutine` and does not prove a completed stock-save cycle |
| Boundary | `mounted-pair-load-safety` | Baseline + Working fixtures | DEFER — EVIDENCED | Uses the exact qualified Working reload through the real guarded load boundary; requires no reconstructed or half-mounted state |
| Boundary | `mounted-pair-area-transition-safety` | Working fixture | DEFER — EVIDENCED | Direct cleanup-handler invocation precedes real `ReloadArea(AutoSaveMode.None)`; this proves the adapter boundary only if run, not independent EventBus delivery timing |

Required per-sample movement telemetry remains: run/evidence identity and UTC; branch/commit/version/DLL hash/MVID; stable rider/mount IDs; relationship/combat/turn-based state; requested destination and authoritative mover; both stock-agent and avoidance flags; entity/view/anchor position and rotation; position and rotation residuals; selection, active commands, and formation state; cleanup trigger/result/residue; exceptions; Mods restoration; save protection.

Visual evidence must independently cover idle, walk, run when available, turn, stop, selection circle, party movement, doorway/corner, and the instantaneous diagnostic mount/dismount transition. Telemetry cannot substitute for visual classification.

Scenario-row result: 1 PASS / 0 FAIL / 24 DEFER — EVIDENCED. Live process attempts: 2 PASS / 2 FAIL, with both failures preserved as repaired harness regressions. Movement samples: 0. Maximum drift/residual, doorway/control, selection, formation, lifecycle residue, and visual classification are not measured. No fixture-backed row, direct-handler boundary probe, guarded load, or save-safety row has run.
