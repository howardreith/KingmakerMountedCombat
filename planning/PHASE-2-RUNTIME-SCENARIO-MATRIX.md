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
| 10 | `native-save-clean-dismount` | actual save request delivery, clean dismount, no serialized relationship | TODO | TODO |
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

Offline native-lifecycle checkpoint: all four native rows have exact protocol allowlists and schema-v2 semantic validators. Release build and source validation pass; component `126/0`, visual-capture contract `12/0`, harness/protocol `138/0`, and assembly-backed contracts `75/0` (`64/0` Kingmaker, `11/0` Wrath). The Pass A/B cells remain `TODO` until clean-commit packaged execution succeeds twice in fresh processes.

Pre-launch incident `20260815T005000Z-native-save-clean-dismount-passA` is a truthful orchestration `FAIL`, not a scenario execution: a redundant request-validator allowlist rejected the row after transactional staging but before Steam or Kingmaker launch. Guarded `finally` restoration completed for Mods and Working, all protected-save checks passed, and no lock remains. The three duplicated allowlists are repaired together with a regression over all four native rows; the failed run ID is retired and neither Pass cell receives credit.
