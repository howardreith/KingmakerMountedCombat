# Phase 3D Runtime Scenario Matrix

Status: IN PROGRESS

All rows require a fresh clean Phase 3D package, guarded `KMC_AUTOMATION_WORKING` use, exact Mods/save restoration, structured telemetry, and independent post-run zero-mutation audit. `PASS` requires actual native input admission and visible outcome where specified; internal service invocation alone receives no usability credit.

## Runtime automation checkpoint — 2026-08-30T22:31:43Z

The strict runtime tranche is implemented for three guarded outer scenarios: `phase3d-horse-presentation-suite`, `phase3d-unified-combat-rt-suite`, and `phase3d-unified-combat-tb-suite`. Their exact required row sets, semantic ownership/resource/cardinality checks, immutable artifact validation, and synthetic mutation regressions pass locally. Every table row below remains `TODO` or `IN PROGRESS` until fresh clean-package Kingmaker evidence is admitted; offline or synthetic evidence is not relabeled as runtime PASS.

The new aggregates directly cover the core presentation, Rider Primary, stock melee, Shortbow/Light Crossbow/Sling, mode-transition, shared-turn, combat Mount/Dismount, five-foot, ordinary AoO, and unmounted-control rows. Rider/mount native incapacitation, full lifecycle/save/area/Wild Shape/door regressions, and the focused Mammoth regression remain separate existing guarded scenarios. Any matrix item not physically exercised by the aggregates must be run separately or retained as an explicit manual/known-limit gate.

Dev.1 RT attempt `20260830T233000Z-phase3d-dev1-rt-passA` is immutable uncredited `FAIL 3/1` before this matrix began: its nested Horse registration prerequisite rejected the new outer scenario name, so no Phase 3D artifact or gameplay row existed. Mandatory postrun restoration passed. Dev.2 repairs only that exact closed scenario allowlist and passes the complete offline gate; every matrix row below remains uncredited until fresh dev.2 evidence.

Dev.2 then stopped at the unchanged Horse presentation precondition; dev.3 repaired only the attributable coordinate frame and reached real Phase 3D RT input. Dev.3 RT A `20260831T062000Z-phase3d-dev3-rt-passA` is immutable uncredited `FAIL 3/2`: exact Rider Primary target cancel and rejection retained the pair, and one hostile click produced exactly one native selection-start/end and cast request, but the native `UnitUseAbility` shell never dispatched. Postrun restoration audit passed before evidence inspection. Dev.4 performs the one bounded repair: require two stable frames of exact Kingmaker RT command-start gates, clear pre-dispatch `NeedLoS`, and make only exact mounted KMC primary intent shells cooldown-neutral so RT's Free-to-Move mapping cannot double-consume resources. No row below is credited until a fresh clean dev.4 package crosses the runtime boundary.

Dev.4 RT A `20260831T161500Z-phase3d-dev4-rt-passA` crossed that boundary and is immutable uncredited `FAIL 2/1` at the Phase 3D row layer. Exact evidence proves native shell preparation, start, acted/finished `Success`, primary delivery, and accepted RiderMelee dispatch with the pair retained. The command then faulted before child attack start because optional ammunition/reload telemetry used an ambiguous inherited `GetProperty` lookup. Independent audit passed before evidence inspection. Dev.5 changes only that non-authoritative reader to deterministic most-derived public declaration traversal with fail-closed unavailable output. No matrix row receives credit until a fresh clean dev.5 package produces the actual attack/resource/rule outcome.

Dev.5 RT A `20260831T212000Z-phase3d-dev5-rt-passA` proved the repaired gameplay path and is immutable uncredited `FAIL 2/1` at the Phase 3D row layer only because evidence capture threw after command terminal. Exact logs bind one native Rider Primary delivery, one accepted RiderMelee command, terminal `Success`, one child attack, zero repaths, pair state `Mounted`, and zero relationship-end activation. `CaptureOutcome` then passed the scalar string returned by `CapturePresentationObservation()` to `JObject.FromObject`, which requires object-shaped JSON. Independent postrun audit passed before inspection. Dev.6 stores the string as a scalar token and wraps it only for row evidence; production behavior is byte-source unchanged outside version identity. No row receives qualification credit until fresh clean dev.6 evidence completes its exact assertions and cleanup.

Dev.6 RT A `20260901T015000Z-phase3d-dev6-rt-passA` completed the artifact and is immutable uncredited `FAIL` (`18/11` outer; `17/10` Phase 3D rows). Independent postrun audit `20260901T022500Z-phase3d-dev6-rt-passA-postrun-audit` passed before inspection. Exact PASS observations include all RT Rider Primary pre-transition rows, explicit Horse Primary, stock melee admission/approach/separate ledgers, invalid-target feedback, long-range Shortbow approach/autofire/cancel/no-forced-melee/LoS/cover-concealment, and rider initiative ownership. Exact failures isolate stale scenario assumptions rather than a production policy defect: target death legally ended repeated melee intent before the later cancel click; adjacent ranged rows rejected the mission-authorized already-in-melee Horse attack; AoO was sampled before native engagement/tick completion; and the tracker constructor was invoked while `CurrentTurn` was null, when exact `UpdateUnits` returns empty by design. The following Rider Primary shell was then stranded by the still-pending native next-unit handoff. Dev.7 makes one bounded diagnostic repair for those four attributable seams and retains every production rule, deadline, cardinality, and restoration gate.

Dev.7 RT A `20260901T064000Z-phase3d-dev7-rt-passA` is immutable uncredited `FAIL` (`19/2` outer; `18/1` Phase 3D rows) after mandatory audit `20260901T073000Z-phase3d-dev7-rt-passA-postrun-audit` passed before inspection. Every row through long-range ranged cancellation passed, including the repaired active melee cancellation. The only gameplay failure was `AwaitRangedAdjacentAttackRt`. Local ignored log attribution proves the adjacent intent remained healthy and alternating; the Horse attacked first because the rider still lacked a Standard action after two long-range shots. Exact Kingmaker `UnitCombatEngagementController` queues ranged provocation on the rider's `RuleAttackRoll` and delivers it on a later engagement tick. Dev.8 therefore isolates one rider-first control with idle rider/Horse/hostile commands, native `AttackOfOpportunity(rider,true)` admission, explicit ranged/no-suppression roll evidence, exact stock counter consumption, and immediate cancellation after one `1/1/1` hostile chain. This is the only repair for the newly exposed cooldown-contaminated control; another failure receives an architecture disposition, not another patch.

Dev.8 RT A `20260901T120000Z-phase3d-dev8-rt-passA` is immutable uncredited `FAIL` (`19/5` outer; `18/4` Phase 3D rows; `63/8` assertions) after mandatory audit `20260901T130000Z-phase3d-dev8-rt-passA-postrun-audit` passed before inspection. The focused adjacent Shortbow and native ranged-AoO rows passed exactly with one unsuppressed rider roll, zero Horse dispatch/movement, native opportunity count `1 -> 0`, and one hostile attack/roll/damage chain. Rider Primary retained the pair with zero cleanup/end events but timed out because a point-target delegated `UnitMoveTo` incorrectly required LoS and never became terminal after valid Horse approach. Crossbow fired natively but reused-target rule capture was contaminated; the immediate Sling swap was not admitted. Dev.9 is the one bounded repair: delegated point movement is LoS-free while the child attack retains native hostile LoS, and each ranged variant receives a freshly verified target plus stable readiness and exact input/child/rule cardinality evidence.

Dev.9 RT A `20260901T163000Z-phase3d-dev9-rt-passA` is immutable uncredited `FAIL` (`6/2` outer; `5/1` Phase 3D rows; `50/2` assertions) after mandatory audit `20260901T173000Z-phase3d-dev9-rt-passA-postrun-audit` passed before inspection. The point-move repair passed: Rider Primary completed one rider attack after `6.673599m` of terminal Horse-owned movement, charged rider Standard only, retained `Mounted`, and emitted no cleanup/end event. Explicit Horse Primary then completed one Horse-owned Bite and retained the pair. The following normal hostile click was admitted and completed a rider attack, but the diagnostic reused the 128-HP target already damaged by both forced-critical explicit controls; target invalidation ended party combat before the persistent minimum. Dev.10 is scenario-only: retire that target, create a distinct six-metre stock target, prove stable exact readiness, and require matching click/request/intent, Horse approach, dispatch/rule/roll/damage, duplicate, cleanup, and target-ID evidence. No production attack or movement policy changes.

Dev.10 RT A `20260901T214000Z-phase3d-dev10-rt-passA` is immutable uncredited aggregate `FAIL` (`13/2` Phase 3D rows; `57/2` assertions) after mandatory audit `20260901T224000Z-phase3d-dev10-rt-passA-postrun-audit` passed before inspection. Its stock-melee isolation passed with distinct target IDs, exact readiness/input `1/1`, positive Horse approach, rider/Horse dispatch `2/1`, native attack/roll/damage `3/3/3`, zero duplicates, exact cancellation, and pair retention. Every Rider Primary/explicit Horse Primary row reached before it also retained the pair. The sole leaf failure occurred at the later long-range Shortbow boundary: click handling returned true but no current native attack request or mounted intent was observed. Exact installed `ClickUnitHandler` has other true-return branches, so dev.11 binds the exact UI selection manager, one selected/nearest rider, hostile visible nonparty/nonloot/noncontrollable target branch, and immediate request/intent cardinality before ranged execution. No production ranged code changes.

Dev.11 RT A `20260902T024000Z-phase3d-dev11-rt-passA` is immutable uncredited aggregate `FAIL` (`20/3` outer; `64/4` assertions) after mandatory audit `20260902T034000Z-phase3d-dev11-rt-passA-postrun-audit` passed before inspection. Its exact Shortbow input gate passed and every implemented RT ranged leaf passed. The only gameplay leaf is cancellation attribution: one ground command cancelled intent with unchanged KMC dispatch and duplicate counters, but one additional rider rule appeared within five frames and the artifact did not preserve its native opportunity flag. The other leaf is an attributable diagnostic exception from placing a ranged-variant target at `2m` below the service's `3m` minimum. Dev.12 records per-rule AoO identity and exact rider/Horse slots/queues around cancellation, requires zero post-cancel ordinary rules, and places isolated crossbow/sling targets at `4m`. No production combat behavior changes; fresh clean-package runtime evidence remains required.

## Shared initiative

| Scenario | RT | TB | Required proof | Status |
|---|---:|---:|---|---|
| mounted-combat-start-single-initiative-entry |  | yes | one rider entry, zero mount entries | TODO |
| mounted-rider-initiative-bonus |  | yes | rider roll/result/bonus owns pair placement | TODO |
| mounted-turn-rider-portrait |  | yes | tracker current entry and portrait are rider | TODO |
| mounted-separate-action-ledgers | yes | yes | independent before/after cooldowns | TODO |
| mounted-shared-turn-action-order |  | yes | mount move, rider attack, mount attack without duplicate | TODO |
| mount-in-combat-before-either-acted |  | yes | merge preserves both ledgers; no extra turn | TODO |
| mount-in-combat-rider-already-acted |  | yes | spent rider resources preserved | TODO |
| mount-in-combat-mount-already-acted |  | yes | spent mount resources preserved | TODO |
| dismount-in-combat-no-extra-turn |  | yes | split deferred to safe next-round boundary | TODO |
| RT-to-TB-shared-turn | yes | yes | one rider turn, cooldown preservation | TODO |
| TB-to-RT-shared-turn | yes | yes | pair initiative/cooldown reconciliation | TODO |
| rider-incapacitation-turn-cleanup | yes | yes | clean dismount, coherent next actor | TODO |
| mount-incapacitation-turn-cleanup | yes | yes | clean dismount/removal, coherent next actor | TODO |

## Five-foot step

| Scenario | Required proof | Status |
|---|---|---|
| mounted-five-foot-step-no-aao | adjacent hostile requests zero successful disengage AoO for exact step | TODO |
| mounted-five-foot-step-distance | mount movement is positive and no more than native `7.5 ft` plus tolerance | TODO |
| mounted-five-foot-step-resource | rider and mount ordinary Move cooldown unchanged | TODO |
| mounted-five-foot-step-after-movement-rejected | stock restriction and explicit feedback | TODO |
| mounted-ordinary-move-aao-control | valid stock disengage AoO can occur | TODO |
| unmounted-five-foot-step-control | stock unmounted result unchanged | TODO |

## Rider Primary isolation

| Scenario | Required proof | Status |
|---|---|---|
| rider-primary-does-not-dismount-rt | relationship remains mounted after terminal hit/miss | IN PROGRESS - dev.8 proves retention through native/KMC admission but the child timed out; dev.9 terminal repair pending |
| rider-primary-does-not-dismount-tb | same under rider-led shared turn | TODO |
| rider-primary-rejection-does-not-dismount | no transition/cleanup event | TODO |
| rider-primary-target-cancel-does-not-dismount | target selector cancel has no cleanup | TODO |
| rider-primary-after-movement-does-not-dismount | movement plus attack retains exact pair | TODO |
| rider-primary-after-shared-turn-transition-does-not-dismount | RT/TB transition plus attack retains pair | TODO |

## Stock melee

| Scenario | RT | TB | Status |
|---|---:|---:|---|
| mounted-stock-click-melee-adjacent | yes |  | TODO |
| mounted-stock-click-melee-approach | yes |  | TODO |
| mounted-stock-click-melee-auto-repeat | yes |  | TODO |
| mounted-stock-click-melee-cancel | yes |  | TODO |
| mounted-stock-click-melee-shared-turn |  | yes | TODO |
| mounted-stock-click-melee-rider-only-explicit | yes | yes | TODO |
| mounted-stock-click-melee-mount-only-explicit | yes | yes | TODO |
| mounted-stock-click-invalid-target-feedback | yes | yes | TODO |
| unmounted-stock-attack-control | yes | yes | TODO |

Each mounted row records actual `ClickUnitHandler` admission, exact stock command identity, movement, attack/rule/roll/damage cardinality, actor/resource owner, terminal result, cancellation, and relationship retention.

## Ranged

| Scenario | RT | TB | Status |
|---|---:|---:|---|
| mounted-bow-adjacent | yes |  | TODO |
| mounted-bow-approach-to-range | yes |  | TODO |
| mounted-bow-auto-fire | yes |  | TODO |
| mounted-bow-cancel | yes |  | TODO |
| mounted-bow-shared-turn |  | yes | TODO |
| mounted-crossbow-or-reload-control | yes | yes | IN PROGRESS - native dev.8 shot observed; fresh-target exact-cardinality rerun pending |
| mounted-sling-control | yes | yes | IN PROGRESS - dev.9 stable equipment/target admission rerun pending |
| mounted-ranged-line-of-sight | yes | yes | TODO |
| mounted-ranged-cover-concealment | yes | yes | TODO |
| mounted-ranged-aao-native-control | yes | yes | IN PROGRESS - exact dev.8 RT row PASS inside an uncredited aggregate; clean aggregate/TB pending |
| mounted-ranged-does-not-force-melee | yes | yes | TODO |
| unmounted-ranged-control | yes | yes | TODO |

## UI, presentation, and regression

| Scenario | Required proof | Status |
|---|---|---|
| mounted-single-rider-turn-portrait | one rider tracker portrait, rider selection/action bar/camera | TODO |
| Horse-small-portrait-close-up | original KMC art crop readable at party/tracker size | IN PROGRESS - original close-up integrated; runtime rendering/human legibility pending |
| saddle-icon | native safe reference or original KMC-owned saddle art; no Wrath asset | IN PROGRESS - native path/manifest scan found no safe reference; original KMC saddle integrated; runtime rendering/human legibility pending |
| mount-ability-in-combat | visible, eligible, one rider Move cost | TODO |
| dismount-ability-in-combat | visible, one rider Move cost, no extra turn | TODO |
| Horse-pose-final-idle-walk-run-turn-stop | final one-cycle lower-seat acceptance | IN PROGRESS - dev.2 pelvis-local `Y=-0.29` failed exact right-stirrup gate (`0.5325693`); single repair preserves pelvis `Y=-0.17` and applies stable Horse mount-root `Y=-0.08`; fresh dev.3 runtime/manual evidence pending |
| Horse-pose-Mammoth-unchanged | Mammoth profile bytes and behavior unchanged | IN PROGRESS - source constants and component locks unchanged; focused runtime regression pending |
| Horse creation/lifecycle/save reload | previously qualified behavior remains exact | TODO |
| door approach/open/traverse | pair interaction remains exact | TODO |
| Wild Shape and save/area cleanup | clean dismount | TODO |
| target-selected Mount outside combat | Phase 3C control remains exact | TODO |
| explicit Dismount | Phase 3C control plus combat cost contract | TODO |
| focused Mammoth Mount/move/Primary/Dismount | one bounded regression only | TODO |
| foreign-mod isolation | no dependency/reference or foreign mutation | TODO |

## Manual gate

The final review package must ask a human to verify ordinary hostile right-click feel, one shared rider portrait/turn, understandable separate actor actions, five-foot-step cursor/feedback, combat Mount/Dismount feedback, ranged stopping behavior, saddle icon legibility, tight Horse portrait crop, and the final lowered Horse seat. The milestone stops at `UNIFIED MOUNTED COMBAT ALPHA — MANUAL REVIEW REQUIRED`.
