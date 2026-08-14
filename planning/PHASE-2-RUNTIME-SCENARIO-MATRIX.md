# Phase 2 runtime scenario matrix

Status: IN PROGRESS

All core claims require two consecutive fresh-process passes from one clean commit/package. Every run must bind branch, commit, version, package/DLL SHA-256, MVID, exact fixture authority, telemetry/artifacts, and external restoration. `KMC_AUTOMATION_BASELINE` is immutable; only exact `KMC_AUTOMATION_WORKING` may enter the guarded write transaction.

## Phase 2A presentation and safety

| Order | Scenario | Required observations | Pass A | Pass B |
|---:|---|---|---|---|
| 1 | `player-action-availability` | supported/unsupported eligibility and exact reason; no mutation | TODO | TODO |
| 2 | `mount-dismount-user-flow` | player action, selection normalization, rollback, repeat activation | TODO | TODO |
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
