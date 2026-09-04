# Phase 3E Risk and Kill-Criteria Register

Status: IN PROGRESS

## Architecture budget

Authorized budget:

- one exact command-lifecycle observation checkpoint;
- one primary paired-scheduler implementation;
- at most two narrowly attributable scheduler repair cycles;
- one controlled rider-owned scheduling-shell investigation only after an evidenced primary kill criterion.

An observation build may add fields/counters but no command drive. A repair cycle may correct one native-order predicate, one start predicate, one terminal/slot restoration defect, or one turn-completion defect. A global controller replacement is not authorized.

The one observation checkpoint is consumed. Dev.1 proved positive exact stock enumeration (`2,485` encounters, all stock false) with the mount awake/in `AwakeUnits`, rider exact current, rider status `Preparing`, and zero scheduler drives. Option A is selected as the one primary implementation.

## Controlling risks and kill criteria

| ID | Criterion | Current status | Required disposition |
|---|---|---|---|
| K1 | Command advances only by permanently changing `CurrentTurn.Unit`. | TODO | Immediate primary pivot; permanent substitution prohibited. |
| K2 | A bounded temporary substitution emits duplicate turn, UI, initiative, or new-round effects that cannot be isolated. | TODO | Kill substitution path; evaluate next authorized option only. |
| K3 | Exact mount command is double-ticked in one Unity frame. | TODO | Stop package; attribute native versus KMC drive. One bounded repair allowed if source ordering is exact. |
| K4 | Advancing arbitrary AI/non-KMC commands is required. | TODO | Kill primary; per-unit broad advancement is prohibited. |
| K5 | Native lifecycle cannot finish without bypassing rule, weapon, target, or animation ownership. | TODO | Kill primary; evaluate scheduling shell only if child remains wholly mount-owned. |
| K6 | Mount resource ownership/cardinality cannot remain exact. | TODO | Kill architecture; do not compensate by manual refund/charge. |
| K7 | Rider resources are consumed or refreshed by scheduling. | TODO | Stop and repair if narrowly attributable; otherwise kill. |
| K8 | Unrelated units' command processing changes. | TODO | Kill broad seam and restore fallback. |
| K9 | Turn completion skips, duplicates, or deadlocks a combatant. | TODO | One narrow completion repair permitted; repeated failure kills primary. |
| K10 | Fresh-process A/B results remain nondeterministic. | TODO | Kill after the bounded repeat/repair budget; do not normalize evidence. |
| K11 | Cleanup leaves command, slot, queue, lease, turn, movement, or presentation residue. | TODO | One narrow terminal/cleanup repair permitted; repeated residue kills primary. |
| K12 | Only a broad global `TurnController` replacement can satisfy the contract. | TODO | `PAIRED SCHEDULER PIVOT — SEPARATE-TURN FALLBACK READY`. |

## Tranche risks

| ID | Risk | Control | Gate |
|---|---|---|---|
| R1 | Stock never enumerates a mounted mount because it is absent from `AwakeUnits`. | One observation-only reference-exact encounter checkpoint. | Select A only on positive encounter; otherwise evaluate supplied-command seam B. |
| R2 | Equality extension still fails because rider turn remains `Preparing`. | Preserve exact status observation; allow pair eligibility at Preparing only for the first exact lease without writing status. | Vertical slice must start within bounded actionable frames. |
| R3 | Postfix turns an unrelated false result into true after native forced-finish/queue effects. | Recheck exact lease plus all preserved hard gates and require nonterminal exact slot identity. | Component mutation tests plus runtime no-override controls. |
| R4 | Stock tick and explicit KMC tick both advance the lease. | Record encounter/drive source and last Unity frame; permit only one path per frame. | K3 cardinality gate. |
| R5 | `UnitCommands.Run` replaces paired Move/Standard state. | Register before admission, snapshot slots/queue, require exact post-admission identity, refuse foreign residue. | Slot-replacement component/runtime rows. |
| R6 | Wrapper/child split obscures gameplay ownership. | Record both top-level scheduler command and child actor/weapon/rule identities. | One-chain cardinality gate. |
| R7 | Native `TurnController` ignores mount start/end events, leaving UI/completion stale. | Keep UI rider-owned; explicit pair lease feeds only pair-aware completion policy. Never relabel mount event as rider event. | sequencing/end-turn/next-round rows. |
| R8 | Existing `ContinueActing` patch retains a rider turn forever on nominal but unusable mount actions. | Require live exact pair action eligibility or active lease, not nominal cooldown alone. | mount-only, rider-only, both-spent, invalid-target rows. |
| R9 | Existing `PrepareExactMountLedger` duplicates native new-round effects. | Reference-exact rider-turn generation guard; one mount initialization per natural rider turn. | next-round-reset-once and native-event cardinality. |
| R10 | Movement is scheduled twice by `ExactTurnMovementAdapter` and the new service. | Existing adapter remains sole movement authority until a failed exact movement row proves otherwise. | movement drive/source telemetry. |
| R11 | Scheduler accidentally adopts AoO/free commands. | Exact registration origin and explicit `UnitAttackOfOpportunity`/free rejection. | component tests and ordinary/AoO isolation controls. |
| R12 | Lifecycle boundary occurs during native command callback. | Reentrancy guard, latched interrupt request, exact terminal cleanup after callback. | cancellation, interruption, mode, death, save/area rows. |
| R13 | Persistent state contaminates campaign saves. | Plain runtime service only; no `UnitPart`, fact, blueprint state, serializer field, or save payload. | source inspection and save cleanup row. |
| R14 | Persistent setting auto-flips after transient fault. | Gate is read-only during fault; log and fail closed for current lease only. | exception cleanup/fallback test. |
| R15 | Phase 3D RT/presentation behavior regresses. | Do not rewrite existing controller paths; run only applicable seam regressions after scheduler stability. | focused RT, Horse, Mammoth checks. |

Runtime disposition: R1 is `PASS` and closed; stock does enumerate the exact active mount command. R2 is confirmed as a real simultaneous false input and remains an active vertical-slice gate. No K1-K12 criterion fired during observation.

Dev.2 offline disposition: the primary Option A source contains no current-turn/unit/status substitution, explicit command start/tick, global unit advancement, cooldown write, AoO adoption, or serialized lease. Exact-reference/component and source-boundary tests pass, including one-drive-per-frame and fault cleanup. This does not close K1-K12 at runtime; all remain live gates for the fresh-process vertical slice. No runtime repair cycle has been consumed.

Dev.2 runtime attribution: immutable run `20260904T094306Z-phase3e-dev2-mammoth-tb-passA` exercised one exact native lifecycle successfully and fired no K1-K12 criterion. It retained rider current identity, mount executor/weapon/rule/resource ownership, one drive per frame, one start/terminal/charge, no foreign/AoO/AI adoption, and exact restoration. The sole `FAIL 69/1` was the diagnostic's incorrect subtraction of admission frame from start frame across a native `WaitingForUI` interval. Dev.3 changes that measurement to first eligible grant and adds a three-frame rejection mutation. This consumes no scheduler repair cycle and grants no runtime PASS until fresh dev.3 A/B.

Dev.3 runtime attribution: immutable run `20260904T113800Z-phase3e-dev3-mammoth-tb-passA` is outer `FAIL`, game `PASS 70/0`, and repeats every K1-K12-safe lifecycle fact with one-frame grant-to-start and exact audited restoration. Its outer validator had a latent schema-55/56 assumption that action-actor `CanActInCombat` must be false; dev.21/dev.2/dev.3 all record true. Dev.4 corrects only that external predicate and its synthetic mutation. Direct validation and harness `241/0` pass. No kill criterion or scheduler repair cycle fired.

Dev.4 Gate 1 disposition: fresh same-package A/B `20260904T133300Z-phase3e-dev4-mammoth-tb-passA` and `20260904T140400Z-phase3e-dev4-mammoth-tb-passB` pass `70/0` each after independent audit-before-read restoration. Both use Option A without any current-turn substitution, explicit command tick/start, broad unit advance, rider resource mutation, foreign/AI/AoO adoption, duplicate frame drive, nondeterminism, or cleanup residue. Mount command, weapon, rule, animation, target, result, and Standard ownership remain exact. No K1-K12 criterion and no scheduler repair cycle fired. K9 turn-completion behavior remains an explicit Gate 2 test rather than an inferred PASS.

Dev.5 Horse TB disposition: immutable run `20260904T155035Z-phase3e-dev5-horse-tb-gate2` encountered one foreign unmounted-Horse AI `UnitAttack` before Mount input. The rider stayed exact actionable current for all `2,520` sampled frames and the scheduler never registered, leased, or drove the foreign command. Therefore K4 did not fire; the runtime confirms the exclusion rather than requiring AI advancement. Dev.6 repairs only the diagnostic target-introduction race by acquiring the existing reversible Horse-AI lease before target creation. No production path, scheduler repair cycle, or kill criterion is consumed.

## Stop and fallback rules

When a kill criterion fires, record the exact package, run ID, source/assembly identities, pre/post state, restoration audit, rejected theory, and why the remaining primary option is unsafe. Historical evidence remains immutable.

If Options A-C fail, the authorized rider-owned shell may be investigated once. It succeeds only if the shell consumes no rider action/rule/weapon/animation ownership and advances exactly one mount-owned child with exact cancellation and cleanup. If it also fails, package the already accepted safe behavior with `EnableUnifiedMountedTurn=false` by default and make no unified-TB claim.

## Non-kill ordinary failures

Compile errors, a test assertion exposing a local defect, an observation schema mistake, and one narrowly attributable start/order/terminal/completion defect are not automatically kill criteria. They must remain inside the stated package/repair budget and may never be “fixed” by weakening assertions, thresholds, allowlists, restoration gates, or ownership rules.
