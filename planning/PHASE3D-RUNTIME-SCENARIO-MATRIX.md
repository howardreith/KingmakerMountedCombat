# Phase 3D Runtime Scenario Matrix

Status: BLOCKED — CRITICAL

All rows require a fresh clean Phase 3D package, guarded `KMC_AUTOMATION_WORKING` use, exact Mods/save restoration, structured telemetry, and independent post-run zero-mutation audit. `PASS` requires actual native input admission and visible outcome where specified; internal service invocation alone receives no usability credit.

## Dev.21 architecture disposition - 2026-09-04

Focused Mammoth TB run `20260904T011000Z-phase3d-dev21-mammoth-tb-passA` crossed actual input admission but failed `49/1` at outcome. The rider-owned shared turn prepared the mount ledger, the Mammoth-owned Standard attack was accepted against an in-range target, and the exact `UnitAttack` occupied the Mammoth Standard slot. It never started because stock Kingmaker TB advances a command only when its executor is `CurrentTurn.Unit`; the current principal must remain the rider. No resource, movement, attack, rule, damage, AoO, target-life, relationship, or presentation mutation occurred. Independent audit `20260904T020000Z-phase3d-dev21-mammoth-tb-passA-postrun-audit` passed exact restoration before inspection.

This blocks `mounted-shared-turn-action-order` and any TB row requiring a mount-owned action during the rider turn. It also prevents final credit for the requested unified turn model. The bounded repair allowance is exhausted; all unresolved TB rows remain uncredited, and no manual-review alpha is produced. The Phase 3C separate-turn fallback remains available behind `EnableUnifiedMountedTurn=false`.

## Runtime automation checkpoint — 2026-08-30T22:31:43Z

The strict runtime tranche is implemented for three guarded outer scenarios: `phase3d-horse-presentation-suite`, `phase3d-unified-combat-rt-suite`, and `phase3d-unified-combat-tb-suite`. Their exact required row sets, semantic ownership/resource/cardinality checks, immutable artifact validation, and synthetic mutation regressions pass locally. The tables below distinguish fresh clean-package runtime credit from partial, deferred, and blocked coverage; offline or synthetic evidence is never relabeled as runtime PASS.

The new aggregates directly cover the core presentation, Rider Primary, stock melee, Shortbow/Light Crossbow/Sling, mode-transition, shared-turn, combat Mount/Dismount, five-foot, ordinary AoO, and unmounted-control rows. Rider/mount native incapacitation, full lifecycle/save/area/Wild Shape/door regressions, and the focused Mammoth regression remain separate existing guarded scenarios. Any matrix item not physically exercised by the aggregates must be run separately or retained as an explicit manual/known-limit gate.

Dev.20 presentation `20260903T180000Z-phase3d-dev20-presentation-passA` passes all four rows, six subscenarios, and `49/0` assertions; audit `20260903T190200Z-phase3d-dev20-presentation-passA-postrun-audit` proves exact restoration. This credits runtime asset identity, dimensions, rider principal, pose constants, Horse-only root lowering, and Mammoth-profile nonmutation. Visual seat contact/clipping and icon/portrait readability remain manual.

Focused Mammoth TB `20260903T220000Z-phase3d-dev20-mammoth-tb-passA` is immutable pre-input `FAIL 27/1`, with independent audit PASS. Its legacy diagnostic requested an independent Mammoth native turn that the Phase 3D coordinator correctly suppresses. No click, command, movement, resource, attack, rule, or relationship mutation occurred. Dev.21 schema v55 instead starts the rider principal and retains Mammoth as the separate action/command/resource actor. The resulting fresh run is the controlling `49/1` architecture failure recorded above; no Mammoth regression credit is claimed.

Dev.19 TB `20260903T120000Z-phase3d-dev19-tb-passA` and audit `20260903T123000Z-phase3d-dev19-tb-passA-postrun-audit` close the bounded automated fixture investigation. Adjacency passed at `1.10947466m <= 2.9m`; the exact rider owned an actionable selected `Preparing` turn, both ledgers were unspent, and Mount was visible/enabled. The Horse alone retained a dormant AI-created Standard-slot `UnitAttack`, so the harness's extra `horse.Commands.Empty` wait prevented input. Product admission does not impose that predicate and the pair-local coordinator interrupts mount commands after successful Mount. No Mount click/cast/delivery or resource/relationship/turn mutation occurred. Those combat-Mount rows remain `DEFER — EVIDENCED`; dev.21 supersedes the former downstream manual disposition by proving the shared-turn executor blocker before a manual package could be issued.

Dev.19 presentation `20260903T150000Z-phase3d-dev19-presentation-passA`, audited by the retry `20260903T160000Z-phase3d-dev19-presentation-passA-postrun-audit-retry`, reached exact healthy presentation state but failed before row emission when generic JSON serialization recursively traversed `PoseVector3.normalized`. Dev.20 writes explicit scalar `x/y/z` evidence; fresh runtime is pending. The failed artifact proves cleanup only and gives no presentation-row or visual credit.

Dev.18 TB observation `20260903T090000Z-phase3d-dev18-tb-observationA`, audited before inspection, names the first TB blocker exactly. The rider held a stable actionable `Preparing` turn, both pair command containers and ledgers were clear, and selection was exact; Mount was rejected solely because the unmounted fixture pair was not adjacent. Dev.19 repairs only fixture staging with one visible native Horse ground move before target creation. PASS evidence must prove its exact player-created Horse `UnitMoveTo`, native success, Horse-only displacement, final adjacency under the production policy, unchanged unmounted/noncombat state, and rider reselection. All TB matrix rows remain `IN PROGRESS` until a fresh clean dev.19 process crosses this boundary and completes them.

Latest RT checkpoint: clean guarded-published dev.17 RT A/B `20260903T023000Z-phase3d-dev17-rt-passA` / `20260903T041500Z-phase3d-dev17-rt-passB` each passed all 29 reached mounted/transition rows and failed only the final automated unmounted Sling control. Both audits passed before inspection. A native four-attack rider melee continuation damaged each distinct fresh Sling target before input; readiness correctly refused the contaminated boundary. Automated `unmounted-ranged-control` is therefore `DEFER — EVIDENCED` to a future isolated control, and both aggregate artifacts remain truthful `FAIL`. Dev.17 TB Pass A `20260903T053000Z-phase3d-dev17-tb-passA` then timed out before the combat Mount click with the exact rider current/`Preparing`; its independent audit passed, but its evidence omitted the rider/Horse command predicates. Dev.18 is one observation-only checkpoint that binds native turn/roster, exact raw commands/queues, actor resources, selection, target/memory, and unified-turn state before any behavioral repair.

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

Dev.12 RT A `20260902T072000Z-phase3d-dev12-rt-passA` is immutable uncredited aggregate `FAIL` (`6/2` outer; `50/2` assertions) after mandatory audit `20260902T080000Z-phase3d-dev12-rt-passA-postrun-audit` passed before inspection. Every reached explicit Rider/Horse primary retention row passed. A fresh stock target passed exact readiness and real request/intent admission `1/1`; the Horse moved `4.7231493m`, then the delegated native point move remained nonterminal until the unchanged pair-command bound. No attack rule fired, the target remained valid, and the rider was `1.70507455m` away at the deadline. Since dev.10/dev.11 completed the same geometry, dev.13 adds only the missing full terminal outcome, final mechanics distance/radius, LoS, movement-agent, and raw-command evidence. One fresh audited run must attribute this boundary before any production repair.

Dev.13 RT A `20260902T102000Z-phase3d-dev13-rt-passA` is immutable uncredited aggregate `FAIL` (`29/2` outer; `73/2` assertions; `28/1` Phase 3D rows) after mandatory audit `20260902T113000Z-phase3d-dev13-rt-passA-postrun-audit` passed before inspection. The dev.12 stock-melee stall did not reproduce. Every reached Rider Primary retention, explicit Horse Primary, normal melee click/approach/repeat/cancel, separate-ledger, invalid-target, Shortbow, Light Crossbow, Sling, native ranged-AoO, rider tracker/initiative/portrait, and RT-to-TB-to-RT row passed. The sole Phase 3D failure was combat Dismount: a Move-ready rider admitted one exact native Dismount cast, then asynchronous delivery returned `accepted=False` after that Move shell had become acted and committed its cooldown. Dev.14 marks only the exact delivery context as an already-admitted native Move shell, suppresses only the duplicate Move-resource recheck, and adds strict shell/cooldown/activation/cleanup evidence. All non-resource gates and native cost ownership remain unchanged; fresh clean-package aggregate evidence is pending.

Dev.14 RT A `20260902T153000Z-phase3d-dev14-rt-passA` is immutable uncredited aggregate `FAIL` (`28/4` outer; `72/6` assertions; `27/3` Phase 3D rows) after the independent restoration audit passed before inspection. Its combat Dismount succeeded exactly once with manual `Mounted -> Unmounted`, native rider Move cooldown `2.97256064`, and no residual command/intent. Its typed Move-slot observation was false only because `UnitCommands.Move` cannot return the raw-slot `UnitUseAbility`; dev.15 uses `GetCommand(CommandType.Move)`. All normal melee, mounted ranged, shared initiative/portrait, mode-transition, and unmounted melee rows passed again. The first Rider Primary retained `Mounted` but its Horse point move remained nonterminal after `6.125914m` despite reaching the unchanged child attack boundary; dev.15 stops only that exact move at proven legal range/LoS and records exclusive terminal evidence. The final unmounted Sling click lacked stable post-swap readiness, so dev.15 adds an admission wait and raw Standard-slot `UnitAttack` observation. No dev.14 row is promoted to final qualification until a clean dev.15 aggregate passes.

## Shared initiative

| Scenario | RT | TB | Required proof | Status |
|---|---:|---:|---|---|
| mounted-combat-start-single-initiative-entry |  | yes | one rider entry, zero mount entries | DEFER — EVIDENCED — dev.17 exact tracker state A/B passed; visible turn-order rendering was not manually credited |
| mounted-rider-initiative-bonus |  | yes | rider roll/result/bonus owns pair placement | PASS — dev.17 RT-to-TB A/B |
| mounted-turn-rider-portrait |  | yes | tracker current entry and portrait are rider | DEFER — EVIDENCED — exact runtime tracker state passed dev.17 A/B; visible rendering was not manually credited |
| mounted-separate-action-ledgers | yes | yes | independent before/after cooldowns | FAIL — RT passed dev.17 A/B; dev.21 proves the separate mount TB ledger cannot execute under the rider turn |
| mounted-shared-turn-action-order |  | yes | mount move, rider attack, mount attack without duplicate | FAIL — dev.21 mount-owned Standard command admitted but never started |
| mount-in-combat-before-either-acted |  | yes | merge preserves both ledgers; no extra turn | DEFER — EVIDENCED — dev.19 proved eligibility before a harness-only pre-input stop |
| mount-in-combat-rider-already-acted |  | yes | spent rider resources preserved | DEFER — EVIDENCED — not reached before the architecture stop |
| mount-in-combat-mount-already-acted |  | yes | spent mount resources preserved | DEFER — EVIDENCED — not reached before the architecture stop |
| dismount-in-combat-no-extra-turn |  | yes | split deferred to safe next-round boundary | DEFER — EVIDENCED — RT Dismount passed dev.17 A/B; TB split timing was not credited |
| RT-to-TB-shared-turn | yes | yes | one rider turn, cooldown preservation | PASS — dev.17 A/B |
| TB-to-RT-shared-turn | yes | yes | pair initiative/cooldown reconciliation | PASS — dev.17 A/B |
| rider-incapacitation-turn-cleanup | yes | yes | clean dismount, coherent next actor | DEFER — EVIDENCED — not reached before the architecture stop |
| mount-incapacitation-turn-cleanup | yes | yes | clean dismount/removal, coherent next actor | DEFER — EVIDENCED — not reached before the architecture stop |

## Five-foot step

| Scenario | Required proof | Status |
|---|---|---|
| mounted-five-foot-step-no-aao | adjacent hostile requests zero successful disengage AoO for exact step | DEFER — EVIDENCED — not reached before the shared-turn architecture stop |
| mounted-five-foot-step-distance | mount movement is positive and no more than native `7.5 ft` plus tolerance | DEFER — EVIDENCED — not reached before the shared-turn architecture stop |
| mounted-five-foot-step-resource | rider and mount ordinary Move cooldown unchanged | DEFER — EVIDENCED — not reached before the shared-turn architecture stop |
| mounted-five-foot-step-after-movement-rejected | stock restriction and explicit feedback | DEFER — EVIDENCED — not reached before the shared-turn architecture stop |
| mounted-ordinary-move-aao-control | valid stock disengage AoO can occur | DEFER — EVIDENCED — not reached before the shared-turn architecture stop |
| unmounted-five-foot-step-control | stock unmounted result unchanged | DEFER — EVIDENCED — not reached before the shared-turn architecture stop |

## Rider Primary isolation

| Scenario | Required proof | Status |
|---|---|---|
| rider-primary-does-not-dismount-rt | relationship remains mounted after terminal hit/miss | PASS — dev.17 A/B |
| rider-primary-does-not-dismount-tb | same under rider-led shared turn | DEFER — EVIDENCED — not reached before the architecture stop |
| rider-primary-rejection-does-not-dismount | no transition/cleanup event | PASS — RT dev.17 A/B; TB not separately credited |
| rider-primary-target-cancel-does-not-dismount | target selector cancel has no cleanup | PASS — RT dev.17 A/B; TB not separately credited |
| rider-primary-after-movement-does-not-dismount | movement plus attack retains exact pair | PASS — dev.17 RT A/B |
| rider-primary-after-shared-turn-transition-does-not-dismount | RT/TB transition plus attack retains pair | PASS — dev.17 A/B |

## Stock melee

| Scenario | RT | TB | Status |
|---|---:|---:|---|
| mounted-stock-click-melee-adjacent | yes |  | PASS — dev.17 RT A/B |
| mounted-stock-click-melee-approach | yes |  | PASS — dev.17 RT A/B |
| mounted-stock-click-melee-auto-repeat | yes |  | PASS — dev.17 RT A/B |
| mounted-stock-click-melee-cancel | yes |  | PASS — dev.17 RT A/B |
| mounted-stock-click-melee-shared-turn |  | yes | FAIL — the mount-owned half cannot advance under the rider-owned turn |
| mounted-stock-click-melee-rider-only-explicit | yes | yes | DEFER — EVIDENCED — RT passed dev.17 A/B; TB not separately credited |
| mounted-stock-click-melee-mount-only-explicit | yes | yes | FAIL — RT passed dev.17 A/B; TB is the exact dev.21 executor failure |
| mounted-stock-click-invalid-target-feedback | yes | yes | DEFER — EVIDENCED — RT passed dev.17 A/B; TB not separately credited |
| unmounted-stock-attack-control | yes | yes | DEFER — EVIDENCED — RT passed dev.17 A/B; TB not separately credited |

Each mounted row records actual `ClickUnitHandler` admission, exact stock command identity, movement, attack/rule/roll/damage cardinality, actor/resource owner, terminal result, cancellation, and relationship retention.

## Ranged

| Scenario | RT | TB | Status |
|---|---:|---:|---|
| mounted-bow-adjacent | yes |  | PASS — dev.17 RT A/B |
| mounted-bow-approach-to-range | yes |  | PASS — dev.17 RT A/B |
| mounted-bow-auto-fire | yes |  | PASS — dev.17 RT A/B |
| mounted-bow-cancel | yes |  | PASS — dev.17 RT A/B |
| mounted-bow-shared-turn |  | yes | DEFER — EVIDENCED — rider ranged ownership is viable, but the unified TB suite stopped at the mount executor blocker |
| mounted-crossbow-or-reload-control | yes | yes | DEFER — EVIDENCED — RT passed dev.17 A/B; TB not separately credited |
| mounted-sling-control | yes | yes | DEFER — EVIDENCED — mounted RT passed dev.17 A/B; TB not separately credited |
| mounted-ranged-line-of-sight | yes | yes | DEFER — EVIDENCED — RT passed dev.17 A/B; TB not separately credited |
| mounted-ranged-cover-concealment | yes | yes | DEFER — EVIDENCED — RT observation passed dev.17 A/B; TB not separately credited |
| mounted-ranged-aao-native-control | yes | yes | DEFER — EVIDENCED — RT passed dev.17 A/B; TB not separately credited |
| mounted-ranged-does-not-force-melee | yes | yes | DEFER — EVIDENCED — RT passed dev.17 A/B; TB not separately credited |
| unmounted-ranged-control | yes | yes | DEFER — EVIDENCED — native prior-command continuation contaminated two isolated RT targets before input; TB not reached |

## UI, presentation, and regression

| Scenario | Required proof | Status |
|---|---|---|
| mounted-single-rider-turn-portrait | one rider tracker portrait, rider selection/action bar/camera | DEFER — EVIDENCED — dev.17 tracker state A/B and dev.20 presentation runtime passed; visible review was not issued |
| Horse-small-portrait-close-up | original KMC art crop readable at party/tracker size | DEFER — EVIDENCED — dev.20 runtime identity/dimensions PASS; human legibility review was not issued |
| saddle-icon | native safe reference or original KMC-owned saddle art; no Wrath asset | DEFER — EVIDENCED — dev.20 original KMC asset identity/load PASS; human legibility review was not issued |
| mount-ability-in-combat | visible, eligible, one rider Move cost | DEFER — EVIDENCED — dev.19 proved visible/enabled exact eligibility before input; cost/merge not credited |
| dismount-ability-in-combat | visible, one rider Move cost, no extra turn | DEFER — EVIDENCED — RT native delivery and rider Move charge passed dev.17 A/B; TB no-extra-turn result not credited |
| Horse-pose-final-idle-walk-run-turn-stop | final one-cycle lower-seat acceptance | DEFER — EVIDENCED — dev.20 runtime profile/pose stability PASS at pelvis `Y=-0.17`, Horse root `Y=-0.08`; visual contact review was not issued |
| Horse-pose-Mammoth-unchanged | Mammoth profile bytes and behavior unchanged | DEFER — EVIDENCED — dev.20 presentation/source profile gates passed, but the focused behavioral regression did not complete |
| Horse creation/lifecycle/save reload | previously qualified behavior remains exact | TODO |
| door approach/open/traverse | pair interaction remains exact | TODO |
| Wild Shape and save/area cleanup | clean dismount | TODO |
| target-selected Mount outside combat | Phase 3C control remains exact | TODO |
| explicit Dismount | Phase 3C control plus combat cost contract | TODO |
| focused Mammoth Mount/move/Primary/Dismount | one bounded regression only | FAIL — dev.21 Primary input admitted but its Mammoth-owned TB command never started |
| foreign-mod isolation | no dependency/reference or foreign mutation | PASS — static dependency gates and postrun external-state audits |

## Deferred manual gate

No Phase 3D manual-review package is issued. The intended checklist remains in `docs/PHASE3D-PLAYTEST.md`, but it is not executable against dev.21. The controlling status is `BLOCKED — CRITICAL` until a new mission chooses the accepted separate-turn fallback, authorizes a purpose-built paired-command scheduler, or revises independent ownership.
