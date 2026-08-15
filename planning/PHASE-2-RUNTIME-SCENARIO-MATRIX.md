# Phase 2 runtime scenario matrix

Status: IN PROGRESS

All core claims require two consecutive fresh-process passes from one clean commit/package. Every run must bind branch, commit, version, package/DLL SHA-256, MVID, exact fixture authority, telemetry/artifacts, and external restoration. `KMC_AUTOMATION_BASELINE` is immutable; only exact `KMC_AUTOMATION_WORKING` may enter the guarded write transaction.

## Phase 2A presentation and safety

| Order | Scenario | Required observations | Pass A | Pass B |
|---:|---|---|---|---|
| 1 | `player-action-availability` | supported/unsupported eligibility and exact reason; no mutation | PASS — `20260814T234000Z-player-action-availability-passA`, `29/0` | PASS — `20260814T234500Z-player-action-availability-passB`, `29/0` |
| 2 | `mount-dismount-user-flow` | player action, selection normalization, rollback, repeat activation | PASS — `20260814T235000Z-mount-dismount-user-flow-passA`, `44/0` | PASS — `20260814T235500Z-mount-dismount-user-flow-passB`, `44/0` |
| 3 | `pose-idle` | mounted baseline, jitter, lower-body pose, cleanup restore | TODO | TODO |
| 4 | `pose-walk-run` | mount authority, pose stability, frame cost, upper-body freedom | TODO | TODO |
| 5 | `pose-turn-stop` | substantial turn/reversal, stop/start, no cumulative drift | TODO | TODO |
| 6 | `pose-doorway-formation` | selected doorway, stock group command, non-pair isolation | TODO | TODO |
| 7 | `pose-equipment-variants` | one-handed, shield when available, two-handed when safe | TODO | TODO |
| 8 | `ui-selection-portrait-actionbar` | selected identity, click rider/mount, portrait, circle, action bar | TODO | TODO |
| 9 | `camera-follow-and-command-routing` | camera subject through movement/switches and ground commands | TODO | TODO |
| 10 | `native-save-clean-dismount` | actual save request delivery, clean dismount, no serialized relationship | PASS — `20260815T010000Z-native-save-qualified-passA`, `63/0` | PASS — `20260815T010500Z-native-save-qualified-passB`, `63/0` |
| 11 | `native-area-clean-dismount` | actual area boundary delivery where safely observable | TODO | TODO |
| 12 | `native-mode-transition-cleanup` | actual RT/TB transition delivery where safely observable | TODO | TODO |
| 13 | `presentation-residue-and-uninstall-safety` | mod disable/feature absent, transform/UI/entity residue zero | TODO | TODO |

## Combat rows locked pending exact visual acceptance

| Tranche | Scenarios | Status |
|---|---|---|
| Rider melee | mounted hit/miss RT, hit TB, invalid target, target death, cleanup, non-mounted control | TODO |
| Mount attack | explicit primary attack RT/TB, invalid target, target death, rider interruption, non-mounted control | TODO |
| Action economy/movement | entry/exit RT/TB, movement-to-target, cancellation, obstacles/doorway, resource accounting | TODO |
| Lifecycle | rider/mount death and incapacitation, view/party/save/load/area/disable/exception boundaries | TODO |
| Stretch | reach, AoO, and basic charge RT/TB plus isolation | TODO |

Combat implementation and execution are forbidden before explicit user acceptance of the exact Phase 2A review package.

Current native-lifecycle checkpoint: all four native rows have exact protocol allowlists and schema-v2 semantic validators. Release build and source validation pass; component `128/0`, visual-capture contract `12/0`, harness/protocol `138/0`, and assembly-backed contracts `75/0` (`64/0` Kingmaker, `11/0` Wrath). Native save is qualified twice. Area, mode, and disable/uninstall remain `TODO` until one clean package succeeds twice in fresh processes.

Pre-launch incident `20260815T005000Z-native-save-clean-dismount-passA` is a truthful orchestration `FAIL`, not a scenario execution: a redundant request-validator allowlist rejected the row after transactional staging but before Steam or Kingmaker launch. Guarded `finally` restoration completed for Mods and Working, all protected-save checks passed, and no lock remains. The three duplicated allowlists are repaired together with a regression over all four native rows; the failed run ID is retired and neither Pass cell receives credit.

Native-save qualification used clean published commit `3781a56f5fa000c89fca1e0809b4eb9fa821734d`, package SHA-256 `3266557bb437f7a8c6bc0570db7e63f0f3c2a322d4d0d2014163cf0f69c7e87f`, DLL SHA-256 `c2fff5b084a695780f3834cfe55741cb033cb86da886014a4a9d58b5dd2278dd`, and MVID `abec6038-807f-4ed1-a54f-70b62fda06dd`. Each fresh process delivered exactly one `SaveManager.SaveRoutine` prefix while Mounted, synchronously cleaned to Unmounted through `SaveRequested`, consumed exactly one bounded Working suppression, performed zero authorized or unauthorized file writes, left the post-load Working identity unchanged, and restored all external state.

Preserved area attempt `20260815T011000Z-native-area-qualified-passA` is a truthful `FAIL` at `48/3`, not Pass A. `Game.ReloadArea` returned on frame 2 before `ISceneHandler.OnAreaBeginUnloading`; the producer prematurely captured a Mounted cleanup latch with no delivery. By frame 65 the ledger contained the exact successful Mounted-to-Unmounted unload delivery and all later area stages, proving asynchronous dispatch rather than missing native cleanup. External validation rejected the malformed latch, and guarded restoration still completed exactly. A deterministic progress-state regression now forbids cleanup or loading-start evidence before actual delivery and preserves cleanup-before-loading ordering. The failed run ID is retired.

First repaired-package attempt `20260815T013000Z-native-area-qualified-passA-retry` is also retained without Pass credit. Its in-game row passed `47/0` and recorded exact unload cleanup at frame 5 plus a clean fresh world at frame 74, but producer cleanup and loading-start records shared frame 5. Unity had not yet completed deferred destruction of the owned anchor/component, so the strict external loading-start validator correctly rejected counts `1/1` instead of zero. All transactions restored. The progress regression now requires a later Update observation before loading-start evidence, preserving the cleanup latch while allowing end-of-frame destruction; validators remain unchanged.

End-of-frame attempt `20260815T013500Z-native-area-qualified-passA-endframe` remains unqualified despite another in-game `47/0` PASS. It correctly separated cleanup frame 5 from residue-free loading-start frame 6, but the generic engine path had already copied a private early loading observation into the cleanup-latch record. The strict phase validator rejected `loading.observed=true` before the declared loading-start phase. The native path now publishes sticky loading flags only through its progress decision; early loading remains private until cleanup and the next Update. No validator changed, and all transactions restored.
