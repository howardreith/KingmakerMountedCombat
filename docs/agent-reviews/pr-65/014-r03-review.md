<!-- agent-review:v1 enrollment=kmc-codex-mmr-attack-orders-20261125103400-6cef key=r03-review part=1/1 sha256=f2c5804836e363d183880a1ee49a6d3c48b57caea49966ca01516074efbd8aa7 -->
### Codex GPT-6.2 Astra — review — round 3 — candidate `19fef30849`

**Decision:** `changes_required` — The candidate passed the required focused gate, including the Mounted Shield regressions. S04 and S05 still have reproducible failure paths. An undelivered rider attack can consume the horse’s opportunity, an obstructed rider can repeatedly request a move that finishes without moving, and real-time retirement can mishandle both queued and already released commands.

## Continuation for Claude
Round 3 uses the last formal round allocated to this enrollment. Any further formal review or change to the allocation requires owner authorization.

For any authorized correction, make the controller distinguish admission, first delivery and terminal outcome. Apply that distinction to horse follow-through, supersession and player feedback. Make retirement refuse the exact cancelled queue entry without retaining finished wrappers. Tie manual-target cleanup to the assignment that actually occurred. For approach, evaluate current strike admission before cancelling movement, choose a goal that can produce progress, and keep waiting time separate from both native execution and KMC’s timeout.

Use the existing focused gate and exercise the controller through queue promotion, terminal handling and the next dispatch. Include a delivery-capable control, an initially legal horse dispatch whose target moves, a carrier already inside its goal radius, and a wait across the documented native timeout. Keep native action spending, the paired coordinator, actor-specific commands and the other child PRs within their existing boundaries. N04–N06 remain owner playtests.

#### Findings

**R1-F3** · P1 · defect · blocks readiness — An unstarted rider force-finish still discards horse follow-through
- Location: `src/KingmakerMountedCombat/Integration/MountedCombatController.cs:2812`; acceptance: MMR-03, S04
- Trigger: After admission, the rider becomes unable to act before native start. The horse is prepared, in reach and still addressable.
- Expected: An evidenced rider-only refusal with zero delivery ends the rider attempt and lets the same activation recheck the horse once.
- Actual: HandleCommandTerminal requires !command.IsActed before MarkRiderEnded. Kingmaker’s documented ForceFinishForTurnBased sets IsActed on an unstarted command without calling OnAction, raising an attack rule or spending Standard. The handler therefore clears the generation and never offers the horse its attack.
- Why it blocks / does not: A rider-only restriction still cancels an independently eligible horse’s attack.
- Requested correction: Use the recorded native terminal branch, start and delivery facts to distinguish a force-finish bookkeeping mark from a delivered action. Preserve the existing prohibition on redispatching the rider.
- Requested evidence: Drive the terminal handler and next dispatch for both an unstarted TurnBasedCannotAct force-finish and an interrupted first windup. Show zero rider deliveries, one eligible horse attempt, and zero attempts for an ended or spent horse.

**R1-F7** · P1 · defect · blocks readiness — The nearest sight goal can complete without moving or turning
- Location: `src/KingmakerMountedCombat/Integration/MountedPairAttackCommand.cs:643`; acceptance: MMR-04, MMR-05, S05
- Trigger: The rider’s eye is blocked, but the nearest projected sight-valid position lies inside the point carrier’s arrival radius.
- Expected: The carrier makes progress toward a position that admits the rider’s actual strike. Failed or zero-displacement destinations allow a viable alternative.
- Actual: TryRiderSightGoal ranks destinations by distance using the heading the horse would acquire by travelling. In the supplied sight fixture, change only horse x from 1.30 to 1.12. The selected goal is x=1.05 at the same z, just 0.07 m away. UnitMoveTo accepts it inside its 0.10 m arrival radius. The horse never turns, the rider’s eye stays at z=0.40 behind the obstruction, and a retry selects the same goal. Farther qualifying goals exist.
- Why it blocks / does not: An ordinary order can exhaust its approach attempts despite having a reachable firing position.
- Requested correction: Account for the carrier’s arrival radius and achievable heading when selecting a sight goal. Give a failed goal a bounded alternative while preserving the native strike check at release.
- Requested evidence: Use the offset fixture above in the pre-start and started paths. Show actual movement or turning that clears the rider’s sight, zero starts while blocked or cooling, and one start after readiness.

**R1-F11** · P2 · defect · blocks readiness — Manual-target ownership still follows command classification instead of the write
- Location: `src/KingmakerMountedCombat/Integration/MountedCombatController.cs:1120`; acceptance: MMR-03, MMR-04, S04, S05
- Trigger: A compatible player action writes the same target, or a marked command is offered for promotion and refused.
- Expected: The actor’s latest actual target assignment determines ownership independently of whether its command competes with the attack.
- Actual: ObserveAcceptedWork returns before checking ownership for a coexisting Free or Swift action, even when it writes the same manual target. Conversely, InternalRunPostfix reports Promotion whenever fromQueue is true, including a refused Run that installed nothing. MarkManualTarget can then relinquish ownership despite no write. The recorded hostile-click contract also assigns ManualTarget after Run.
- Why it blocks / does not: Retirement can erase a later player selection or leave the retiring generation’s own selection in place.
- Requested correction: Track replacement assignment independently of the competing-work classification. Verify that a promoted command installed and wrote the target, and account for the click handler’s post-Run assignment.
- Requested evidence: Observe the actor’s final selection after a same-target coexisting player write, a queue insertion with no write, a refused promotion, and retirement of an automatic order following an actor-specific selection.

**R2-F1** · P1 · defect · blocks readiness — Real-time controller retirement bypasses the strike-release check
- Location: `src/KingmakerMountedCombat/Integration/MountedCombatController.cs:1679`; acceptance: MMR-03, MMR-04, S04, S05
- Trigger: The horse accepts newer player work after the rider’s first strike has been released while its native command is still running.
- Expected: Newer work stops an unreleased first strike. Once a strike has been released, the engine owns its completion and the retired order cannot repeat.
- Actual: The wrapper now checks supersession during windup. The real-time controller still calls InterruptOwnedCommand on any unfinished current wrapper when newer work retires the order, without checking StrikeReleased. The delivered control sets a harness flag and ticks only the wrapper. Its UnitCommand.Tick never calls OnAction, and the control never executes controller retirement.
- Why it blocks / does not: Supersession on the other participant can interrupt a released native routine. The positive control also cannot establish ordering at first delivery.
- Requested correction: Apply the release boundary consistently in controller retirement and verify that the pre-delivery check precedes native OnAction. Retire future dispatch without interrupting native completion of the released strike.
- Requested evidence: Drive the controller as well as the wrapper. Accept newer horse work before and after the rider’s first delivery, and remove an automatic target during windup. Count deliveries and interruptions with a positive control that can actually deliver.

**R2-F3** · P2 · defect · blocks readiness — Carrier deferral adjusts only one clock and carries the deduction into the attack
- Location: `src/KingmakerMountedCombat/Integration/MountedPairAttackCommand.cs:1416`; acceptance: MMR-05, S05
- Trigger: An older horse action holds the slot for a long interval before the rider’s approach and first windup.
- Expected: Waiting behind older horse work preserves the order. Movement starts toward the current target once available, and each execution phase retains its normal time bound.
- Actual: The wrapper enters carrierDeferred after native start. ChaseElapsed subtracts the wait only from KMC’s check. The repository documents a separate nine-second UnitCommand.Tick interrupt for started commands, which the 16-second harness case omits. There is also a definite clock error after the wait. StartChildAttack resets TimeSinceStart to zero while deferredSeconds stays positive, making the new attack phase’s elapsed time negative.
- Why it blocks / does not: The supplied test cannot establish that the order survives a long wait in the installed engine, and the subtraction extends KMC’s bound for a subsequent stalled attack.
- Requested correction: Establish the installed timeout’s applicability to this wrapper and keep the wait outside any applicable started-command deadline. Reset phase accounting when the attack clock restarts.
- Requested evidence: Exercise a wait beyond nine seconds with the applicable native timeout represented, move the target, then free the slot. Check one carrier to the current target, one legal start and the ordinary timeout for a deliberately stalled subsequent windup.

**R2-F4** · P2 · defect · blocks readiness — Horse admission discards the rider’s refusal before either actor delivers
- Location: `src/KingmakerMountedCombat/Integration/MountedCombatController.cs:1970`; acceptance: MMR-03, S04
- Trigger: A rider-only refusal is retained for the horse, and the horse is admitted but ends without executing, or the generation is replaced before that decision.
- Expected: The current generation presents the rider’s evidenced refusal once if neither actor executes. A successful follow-through or a new order does not inherit that warning.
- Actual: A horse dispatch clears riderRefusalAwaitingHorse immediately. If the target leaves reach before start, the horse wrapper interrupts without delivering. The Failed terminal clears the order without calling ShowStockFeedback. Conversely, ClearStockAttackIntent does not clear the saved refusal if the order is cancelled before horse dispatch, so a later generation can display it.
- Why it blocks / does not: A player can receive no explanation for an entirely unexecuted order, or receive an earlier rider’s refusal for a later order.
- Requested correction: Bind the pending refusal to its generation and settle it from actual delivery or final retirement. Clear it on every cancellation or replacement boundary.
- Requested evidence: Count visible feedback for an already-spent horse, an admitted horse that loses reach before start, and a successful follow-through. Replace a pending order and verify that the later generation receives no stale message.

**R3-F1** · P1 · defect · blocks readiness — A real-time explicit wrapper can still promote after controller retirement
- Location: `src/KingmakerMountedCombat/Integration/MountedCombatController.cs:1673`; acceptance: MMR-04, MMR-05, S05
- Trigger: A rider attack is queue-only in real time when newer horse work retires its generation before the rider queue advances.
- Expected: A retired explicit rider wrapper cannot be installed or write Hold or manual selection when the native queue advances.
- Actual: Only the turn-based supersession branch removes and records the queued wrapper. Real-time retirement clears stockIntentCommand and interrupts the wrapper. SweepFinishedCommand drops its reference when the wrapper is absent from the raw slot, leaving the queue entry. Later AdmitQueuedExplicitRun returns true for the unmatched command.
- Why it blocks / does not: An interrupted explicit wrapper can enter native Run after supersession and overwrite player state even though it never attacks.
- Requested correction: Apply exact-wrapper queue retirement to the real-time and general cancellation paths as well as turn-based supersession. Preserve unrelated queue entries and legitimate actor-specific orders.
- Requested evidence: Queue an explicit rider order behind older work, accept newer horse work, then exercise both controller-first and promotion-first orderings in real time. Check zero stale slot installations and target/Hold writes, with the successor preserved.

**R3-F2** · P2 · defect · blocks readiness — The retired-wrapper set retains commands that will never promote
- Location: `src/KingmakerMountedCombat/Integration/MountedCombatController.cs:1699`; acceptance: MMR-03, S04, PR #64 R4-F8
- Trigger: A turn-based attack is superseded before it starts, and its wrapper is removed successfully or was already in a slot.
- Expected: A retired wrapper remains identifiable to a late promotion while finished commands and their referenced entities can be released.
- Actual: RetireSupersededPlayerOrder adds every unfinished, unstarted wrapper to retiredQueuedWrappers, including slot-only wrappers and entries it immediately removes from the queue. The sole removal is a future AdmitQueuedExplicitRun for that identity. Normal successful removal eliminates that future callback.
- Why it blocks / does not: Repeated ordinary cancellations retain finished commands and their actor and target references for the controller’s lifetime, regressing the required cleanup behavior.
- Requested correction: Represent retirement on the exact wrapper or use an equivalent lifetime-bounded mechanism. Preserve the refusal of a promotion already in progress.
- Requested evidence: Repeat supersession for slot-only and queue-only wrappers that never receive a later callback. Show bounded controller retention and continued refusal when a late promotion does arrive.

**R3-F3** · P2 · defect · blocks readiness — Movement revocation cancels an attack that no longer needs movement
- Location: `src/KingmakerMountedCombat/Integration/MountedPairAttackCommand.cs:1464`; acceptance: MMR-05, S05
- Trigger: An explicit rider approach is active when the horse loses movement and the target comes into the rider’s legal strike range.
- Expected: Once the rider’s strike is admissible, the command releases its carrier and lets the ready rider attack from the current position.
- Actual: TickApproach cancels for forbidden movement before evaluating current native strike admission. A rider whose target has just entered legal reach loses the whole order. The deferred-carrier branch already evaluates admission first.
- Why it blocks / does not: A restriction on horse movement can suppress an otherwise legal stationary rider attack.
- Requested correction: Evaluate actual strike admission before applying the movement restriction to continued approach. Keep the out-of-range cancellation and exact-carrier cleanup.
- Requested evidence: With a live carrier, revoke horse movement on the tick the target enters reach. Observe no further travel and one legal rider start. The still-out-of-range control must release the carrier and produce zero starts.

**R3-F4** · P2 · evidence_gap · blocks readiness — The lost-reach regression starts outside reach
- Location: `tests/Chunk7Diagnostics/AttackPathHarnessTemplate.cs:908`; acceptance: MMR-03, S04
- Trigger: The focused harness reports that a target left horse reach after dispatch.
- Expected: The moving-target case demonstrates that the controller admits an initially legal horse follow-through and that loss of reach before native start prevents both movement and attack.
- Actual: The case labelled ‘left reach before start’ begins at 2.41 m with a 2.40 m horse radius, then moves the target to 3.50 m. It constructs the wrapper directly and calls ForbidApproach. The fixture never observes legal follow-through selection or an in-range-to-out-of-range transition.
- Why it blocks / does not: The submitted regression cannot detect a failure connecting the controller’s selection-time admission to the no-approach wrapper it dispatches.
- Requested correction: Start inside the exact strike radius, drive the production selection/dispatch seam, and move the target outside only after observing horse dispatch.
- Requested evidence: Report dispatch, carrier and start counts for initially outside, inside-then-outside and continuously inside cases.

**R3-F5** · P3 · optional_polish · non-blocking — Turn-based updates rebuild a horse attack plan before it can be used
- Location: `src/KingmakerMountedCombat/Integration/MountedCombatController.cs:1871`; acceptance: MMR-03, S04
- Trigger: A retained turn-based rider order is driven repeatedly while the rider is still attacking or the horse cannot join.
- Expected: The horse strike-plan probe runs when the dispatcher can actually consider horse follow-through.
- Actual: The argument to MountJoinsIntent eagerly calls FollowThroughStrikeAdmissibleNow on every turn-based rider-order drive. Its probe constructs and initializes a native attack plan even while a wrapper is active or the accepted order is ranged.
- Why it blocks / does not: This is avoidable work in the update path. No measured performance regression is claimed.
- Requested correction: Short-circuit ineligible, ranged and already-active cases before building the horse plan.
- Requested evidence: Use PlannedStrikeProbeCount to show zero probes during a continuing rider routine and a probe when horse follow-through is considered.

#### Prior finding assessments

- **R1-F1** — `fix_verified` — The selector uses the planned horse strike’s exact admission, the turn-based follow-through receives no approach authority, and OnStart rechecks geometry. The separate defect in the submitted moving-target test is R3-F4.
- **R1-F11** — `fix_rejected` — Generation and actor scoping are improved. A coexisting target write is still skipped, and an unsuccessful promotion can relinquish ownership without a write.
- **R1-F12** — `fix_verified` — The scoped operator excerpt now supplies attributable child and program counts, candidates and their relationship to submission. Its five record hashes match. The round-1 timestamps have one-second resolution.
- **R1-F2** — `fix_verified` — For the reported turn-based supersession, controller-first retirement removes the exact entry and promotion-first admission refuses it. This is verified by source and compiled wiring. The distinct real-time branch and strong-reference lifetime are R3-F1 and R3-F2.
- **R1-F3** — `fix_rejected` — The new condition covers a started, unacted first windup. It excludes the documented native rider-only force-finish that marks IsActed without delivery, again clearing the eligible horse’s order.
- **R1-F4** — `fix_verified` — OnStart checks movement permission before adopting a live carrier. The harness observed exact-carrier release with zero strikes on revocation and one start in the permitted control. R3-F3 concerns the later arrival tick.
- **R1-F7** — `fix_rejected` — The goal now uses rider-eye geometry. Its nearest admissible projection can still fall inside the carrier’s arrival radius while requiring a turn the horse never makes.
- **R2-F1** — `fix_rejected` — Child start alone no longer marks release. Real-time controller retirement still interrupts a released wrapper, and the harness never runs native first delivery or that retirement path.
- **R2-F2** — `fix_verified` — Native Interrupt reaches OnEnded, which now releases the horse’s pre-start approach and checks for another movement owner. The harness observed one stop without a successor, zero with a successor and zero attack starts.
- **R2-F3** — `fix_rejected` — The destination refresh is correct. The long-wait fixture omits the native started-command timeout, and the accumulated wait is still subtracted after the attack clock resets.
- **R2-F4** — `fix_rejected` — The fallback covers immediate exhaustion. Horse dispatch discards it before delivery, and general cancellation can leave it available to another generation.
- **R2-F5** — `fix_verified` — The revised fixture asserts queue-only membership while the old Move is live. The exact cleanup sweep promotes the Standard successor, which receives zero StopMoving calls. The direct-successor and no-successor controls also passed.

#### Evidence assessment

- **Inspected:** The enrolled mission, repository validation policy, current handoffs, developer report, transcript notice, prior-findings ledger, and full 23-file feature diff. The correction commit was also compared with its round-2 parent.; The stock-intent controller, native Run and queue hooks, retained-order ledger, strike admission and movement code, terminal classifier, native contract records, focused gate and attack-path harness.
- **Executed verification:** The controller receipt binds the Fast focused gate to 19fef3084980c7159cdb9dba4d96768737986718. It records exit 0 in 36.4 seconds, an unchanged worktree and 15 of 15 focused children passing. The supplied log matches its SHA-256 bd53c3840aa8fe398fe3b6d617e5c5c5249ff74806d4e57e6b753b3232a64456.; The log records 1,542 component checks, 153 source checks, 4 attack-order native contract checks, 56 lifecycle contract checks and 42 attack-path harness checks, all passing. The harness identifies its results as nativeProof=false.; The same gate records 239 presentation checks, 114 Mounted Shield contract checks, 68 selection/rollback checks including icon identity, 54 Archery checks, 183 Ranger/Riding Dog checks, 29 replay checks and 25 checks for each UMM profile, all passing. The tested product DLL SHA-256 was 425a54cf9a4b21b2f9566b317ee2843a5434c4917aeb32e220cfd46e921b212a.; Read-only review checks confirmed the candidate identity, the feature diff against the stated merge base, a clean git diff --check for that range and the hashes of the five supplied ledger records. The gate closed with 78.52 of 90 validation minutes spent and zero unreconciled entries.
- **Source inspection only:** The turn-based queue-removal fix, rider-only terminal classification, real-time retirement, manual-target ownership and feedback lifetime were traced in source. The focused gate did not drive those controller paths end to end.; The sight-goal counterexample was checked arithmetically using the submitted fixture’s dimensions, heading calculation and arrival rule. It is a source counterexample, not an observed game run.
- **Required gaps:** Exercise the controller’s terminal-to-dispatch and retirement-to-promotion paths with observable horse attempts, first deliveries, feedback, manual-target writes and successor state.; Exercise a genuinely in-range horse dispatch followed by lost reach, a sight goal within the carrier’s arrival radius, and stationary strike admission when movement becomes forbidden.; Verify the first-delivery and long-running-command boundaries against the installed contract used by the offline checks. A stub that never delivers an attack or times out cannot establish those outcomes.
- **Limitations:** The foreground developer transcript is withheld. Execution claims above come from the candidate-bound controller receipt and log. The reviewer used read-only inspection and did not rerun the build or launch Kingmaker.; The lifecycle checks establish compiled calls and wiring. The harness executes selected production members with a simulated engine boundary. Neither establishes native action attribution, latency or gameplay.; Ledger entry 8 and the recorded round-1 request share the timestamp 15:35:59Z. Their timestamps alone cannot resolve ordering within that second.

#### Summary

- **Delivered behavior:** The candidate adds same-activation horse selection, nearest-eligible melee target selection and pre-start approach under native strike readiness. The focused gate confirms several corrected command paths and the S08 regression checks. The remaining defects prevent acceptance of ordinary follow-through and real-time attack progression.
- **Scope reviewed:** Final PR-1 checkpoint for MMR-03, MMR-04 and MMR-05 against S04, S05, S08 and the carried regression obligations. All 12 pending findings were assessed against the fixed candidate.
- **Pending later-phase work:** none
- **Unresolved questions:** The submitted checks do not establish a UnitAttack exemption from the native started-command timeout or the ordering of the first native OnAction relative to the wrapper’s cancellation check.
- **Remaining owner review:** N04 — Observe horse-attributed native attacks and routine cardinality in both initiative arrangements, including a spent or ended horse.; N05 — Observe nearest eligible reachable hostile acquisition with two targets, including AI-off, Stop/Hold and retained explicit-target controls.; N06 — Compare input, first movement, attack readiness and first strike with an equivalent unmounted actor.; The owner controls any further formal review after this enrollment’s third round.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a213b0-ca00-7b91-9e8f-30b0989b4bdb`, duration 1012.3s, response sha256 `f4bfc7ddccaec5393f8edd9f23daed964248c46e127b2bfd3b06453e3d674348`, request `kmc-codex-mmr-attack-orders-20261125103400-6cef-r03-f45c81`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "19fef3084980c7159cdb9dba4d96768737986718",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The candidate passed the required focused gate, including the Mounted Shield regressions. S04 and S05 still have reproducible failure paths. An undelivered rider attack can consume the horse\u2019s opportunity, an obstructed rider can repeatedly request a move that finishes without moving, and real-time retirement can mishandle both queued and already released commands.\n\n## Continuation for Claude\nRound 3 uses the last formal round allocated to this enrollment. Any further formal review or change to the allocation requires owner authorization.\n\nFor any authorized correction, make the controller distinguish admission, first delivery and terminal outcome. Apply that distinction to horse follow-through, supersession and player feedback. Make retirement refuse the exact cancelled queue entry without retaining finished wrappers. Tie manual-target cleanup to the assignment that actually occurred. For approach, evaluate current strike admission before cancelling movement, choose a goal that can produce progress, and keep waiting time separate from both native execution and KMC\u2019s timeout.\n\nUse the existing focused gate and exercise the controller through queue promotion, terminal handling and the next dispatch. Include a delivery-capable control, an initially legal horse dispatch whose target moves, a carrier already inside its goal radius, and a wait across the documented native timeout. Keep native action spending, the paired coordinator, actor-specific commands and the other child PRs within their existing boundaries. N04\u2013N06 remain owner playtests.",
  "evidence_assessment": {
    "executed_verification": [
      "The controller receipt binds the Fast focused gate to 19fef3084980c7159cdb9dba4d96768737986718. It records exit 0 in 36.4 seconds, an unchanged worktree and 15 of 15 focused children passing. The supplied log matches its SHA-256 bd53c3840aa8fe398fe3b6d617e5c5c5249ff74806d4e57e6b753b3232a64456.",
      "The log records 1,542 component checks, 153 source checks, 4 attack-order native contract checks, 56 lifecycle contract checks and 42 attack-path harness checks, all passing. The harness identifies its results as nativeProof=false.",
      "The same gate records 239 presentation checks, 114 Mounted Shield contract checks, 68 selection/rollback checks including icon identity, 54 Archery checks, 183 Ranger/Riding Dog checks, 29 replay checks and 25 checks for each UMM profile, all passing. The tested product DLL SHA-256 was 425a54cf9a4b21b2f9566b317ee2843a5434c4917aeb32e220cfd46e921b212a.",
      "Read-only review checks confirmed the candidate identity, the feature diff against the stated merge base, a clean git diff --check for that range and the hashes of the five supplied ledger records. The gate closed with 78.52 of 90 validation minutes spent and zero unreconciled entries."
    ],
    "inspected": [
      "The enrolled mission, repository validation policy, current handoffs, developer report, transcript notice, prior-findings ledger, and full 23-file feature diff. The correction commit was also compared with its round-2 parent.",
      "The stock-intent controller, native Run and queue hooks, retained-order ledger, strike admission and movement code, terminal classifier, native contract records, focused gate and attack-path harness."
    ],
    "limitations": [
      "The foreground developer transcript is withheld. Execution claims above come from the candidate-bound controller receipt and log. The reviewer used read-only inspection and did not rerun the build or launch Kingmaker.",
      "The lifecycle checks establish compiled calls and wiring. The harness executes selected production members with a simulated engine boundary. Neither establishes native action attribution, latency or gameplay.",
      "Ledger entry 8 and the recorded round-1 request share the timestamp 15:35:59Z. Their timestamps alone cannot resolve ordering within that second."
    ],
    "required_gaps": [
      "Exercise the controller\u2019s terminal-to-dispatch and retirement-to-promotion paths with observable horse attempts, first deliveries, feedback, manual-target writes and successor state.",
      "Exercise a genuinely in-range horse dispatch followed by lost reach, a sight goal within the carrier\u2019s arrival radius, and stationary strike admission when movement becomes forbidden.",
      "Verify the first-delivery and long-running-command boundaries against the installed contract used by the offline checks. A stub that never delivers an attack or times out cannot establish those outcomes."
    ],
    "source_inspection_only": [
      "The turn-based queue-removal fix, rider-only terminal classification, real-time retirement, manual-target ownership and feedback lifetime were traced in source. The focused gate did not drive those controller paths end to end.",
      "The sight-goal counterexample was checked arithmetically using the submitted fixture\u2019s dimensions, heading calculation and arrival rule. It is a source counterexample, not an observed game run."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "MMR-03",
        "S04"
      ],
      "actual": "HandleCommandTerminal requires !command.IsActed before MarkRiderEnded. Kingmaker\u2019s documented ForceFinishForTurnBased sets IsActed on an unstarted command without calling OnAction, raising an attack rule or spending Standard. The handler therefore clears the generation and never offers the horse its attack.",
      "blocking_rationale": "A rider-only restriction still cancels an independently eligible horse\u2019s attack.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "An evidenced rider-only refusal with zero delivery ends the rider attempt and lets the same activation recheck the horse once.",
      "id": "R1-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedCombatController.cs",
        "line": 2812
      },
      "priority": "P1",
      "requested_correction": "Use the recorded native terminal branch, start and delivery facts to distinguish a force-finish bookkeeping mark from a delivered action. Preserve the existing prohibition on redispatching the rider.",
      "requested_evidence": "Drive the terminal handler and next dispatch for both an unstarted TurnBasedCannotAct force-finish and an interrupted first windup. Show zero rider deliveries, one eligible horse attempt, and zero attempts for an ended or spent horse.",
      "title": "An unstarted rider force-finish still discards horse follow-through",
      "trigger": "After admission, the rider becomes unable to act before native start. The horse is prepared, in reach and still addressable."
    },
    {
      "acceptance_ids": [
        "MMR-04",
        "MMR-05",
        "S05"
      ],
      "actual": "TryRiderSightGoal ranks destinations by distance using the heading the horse would acquire by travelling. In the supplied sight fixture, change only horse x from 1.30 to 1.12. The selected goal is x=1.05 at the same z, just 0.07 m away. UnitMoveTo accepts it inside its 0.10 m arrival radius. The horse never turns, the rider\u2019s eye stays at z=0.40 behind the obstruction, and a retry selects the same goal. Farther qualifying goals exist.",
      "blocking_rationale": "An ordinary order can exhaust its approach attempts despite having a reachable firing position.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The carrier makes progress toward a position that admits the rider\u2019s actual strike. Failed or zero-displacement destinations allow a viable alternative.",
      "id": "R1-F7",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedPairAttackCommand.cs",
        "line": 643
      },
      "priority": "P1",
      "requested_correction": "Account for the carrier\u2019s arrival radius and achievable heading when selecting a sight goal. Give a failed goal a bounded alternative while preserving the native strike check at release.",
      "requested_evidence": "Use the offset fixture above in the pre-start and started paths. Show actual movement or turning that clears the rider\u2019s sight, zero starts while blocked or cooling, and one start after readiness.",
      "title": "The nearest sight goal can complete without moving or turning",
      "trigger": "The rider\u2019s eye is blocked, but the nearest projected sight-valid position lies inside the point carrier\u2019s arrival radius."
    },
    {
      "acceptance_ids": [
        "MMR-03",
        "MMR-04",
        "S04",
        "S05"
      ],
      "actual": "ObserveAcceptedWork returns before checking ownership for a coexisting Free or Swift action, even when it writes the same manual target. Conversely, InternalRunPostfix reports Promotion whenever fromQueue is true, including a refused Run that installed nothing. MarkManualTarget can then relinquish ownership despite no write. The recorded hostile-click contract also assigns ManualTarget after Run.",
      "blocking_rationale": "Retirement can erase a later player selection or leave the retiring generation\u2019s own selection in place.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The actor\u2019s latest actual target assignment determines ownership independently of whether its command competes with the attack.",
      "id": "R1-F11",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedCombatController.cs",
        "line": 1120
      },
      "priority": "P2",
      "requested_correction": "Track replacement assignment independently of the competing-work classification. Verify that a promoted command installed and wrote the target, and account for the click handler\u2019s post-Run assignment.",
      "requested_evidence": "Observe the actor\u2019s final selection after a same-target coexisting player write, a queue insertion with no write, a refused promotion, and retirement of an automatic order following an actor-specific selection.",
      "title": "Manual-target ownership still follows command classification instead of the write",
      "trigger": "A compatible player action writes the same target, or a marked command is offered for promotion and refused."
    },
    {
      "acceptance_ids": [
        "MMR-03",
        "MMR-04",
        "S04",
        "S05"
      ],
      "actual": "The wrapper now checks supersession during windup. The real-time controller still calls InterruptOwnedCommand on any unfinished current wrapper when newer work retires the order, without checking StrikeReleased. The delivered control sets a harness flag and ticks only the wrapper. Its UnitCommand.Tick never calls OnAction, and the control never executes controller retirement.",
      "blocking_rationale": "Supersession on the other participant can interrupt a released native routine. The positive control also cannot establish ordering at first delivery.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Newer work stops an unreleased first strike. Once a strike has been released, the engine owns its completion and the retired order cannot repeat.",
      "id": "R2-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedCombatController.cs",
        "line": 1679
      },
      "priority": "P1",
      "requested_correction": "Apply the release boundary consistently in controller retirement and verify that the pre-delivery check precedes native OnAction. Retire future dispatch without interrupting native completion of the released strike.",
      "requested_evidence": "Drive the controller as well as the wrapper. Accept newer horse work before and after the rider\u2019s first delivery, and remove an automatic target during windup. Count deliveries and interruptions with a positive control that can actually deliver.",
      "title": "Real-time controller retirement bypasses the strike-release check",
      "trigger": "The horse accepts newer player work after the rider\u2019s first strike has been released while its native command is still running."
    },
    {
      "acceptance_ids": [
        "MMR-05",
        "S05"
      ],
      "actual": "The wrapper enters carrierDeferred after native start. ChaseElapsed subtracts the wait only from KMC\u2019s check. The repository documents a separate nine-second UnitCommand.Tick interrupt for started commands, which the 16-second harness case omits. There is also a definite clock error after the wait. StartChildAttack resets TimeSinceStart to zero while deferredSeconds stays positive, making the new attack phase\u2019s elapsed time negative.",
      "blocking_rationale": "The supplied test cannot establish that the order survives a long wait in the installed engine, and the subtraction extends KMC\u2019s bound for a subsequent stalled attack.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Waiting behind older horse work preserves the order. Movement starts toward the current target once available, and each execution phase retains its normal time bound.",
      "id": "R2-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedPairAttackCommand.cs",
        "line": 1416
      },
      "priority": "P2",
      "requested_correction": "Establish the installed timeout\u2019s applicability to this wrapper and keep the wait outside any applicable started-command deadline. Reset phase accounting when the attack clock restarts.",
      "requested_evidence": "Exercise a wait beyond nine seconds with the applicable native timeout represented, move the target, then free the slot. Check one carrier to the current target, one legal start and the ordinary timeout for a deliberately stalled subsequent windup.",
      "title": "Carrier deferral adjusts only one clock and carries the deduction into the attack",
      "trigger": "An older horse action holds the slot for a long interval before the rider\u2019s approach and first windup."
    },
    {
      "acceptance_ids": [
        "MMR-03",
        "S04"
      ],
      "actual": "A horse dispatch clears riderRefusalAwaitingHorse immediately. If the target leaves reach before start, the horse wrapper interrupts without delivering. The Failed terminal clears the order without calling ShowStockFeedback. Conversely, ClearStockAttackIntent does not clear the saved refusal if the order is cancelled before horse dispatch, so a later generation can display it.",
      "blocking_rationale": "A player can receive no explanation for an entirely unexecuted order, or receive an earlier rider\u2019s refusal for a later order.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The current generation presents the rider\u2019s evidenced refusal once if neither actor executes. A successful follow-through or a new order does not inherit that warning.",
      "id": "R2-F4",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedCombatController.cs",
        "line": 1970
      },
      "priority": "P2",
      "requested_correction": "Bind the pending refusal to its generation and settle it from actual delivery or final retirement. Clear it on every cancellation or replacement boundary.",
      "requested_evidence": "Count visible feedback for an already-spent horse, an admitted horse that loses reach before start, and a successful follow-through. Replace a pending order and verify that the later generation receives no stale message.",
      "title": "Horse admission discards the rider\u2019s refusal before either actor delivers",
      "trigger": "A rider-only refusal is retained for the horse, and the horse is admitted but ends without executing, or the generation is replaced before that decision."
    },
    {
      "acceptance_ids": [
        "MMR-04",
        "MMR-05",
        "S05"
      ],
      "actual": "Only the turn-based supersession branch removes and records the queued wrapper. Real-time retirement clears stockIntentCommand and interrupts the wrapper. SweepFinishedCommand drops its reference when the wrapper is absent from the raw slot, leaving the queue entry. Later AdmitQueuedExplicitRun returns true for the unmatched command.",
      "blocking_rationale": "An interrupted explicit wrapper can enter native Run after supersession and overwrite player state even though it never attacks.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A retired explicit rider wrapper cannot be installed or write Hold or manual selection when the native queue advances.",
      "id": "R3-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedCombatController.cs",
        "line": 1673
      },
      "priority": "P1",
      "requested_correction": "Apply exact-wrapper queue retirement to the real-time and general cancellation paths as well as turn-based supersession. Preserve unrelated queue entries and legitimate actor-specific orders.",
      "requested_evidence": "Queue an explicit rider order behind older work, accept newer horse work, then exercise both controller-first and promotion-first orderings in real time. Check zero stale slot installations and target/Hold writes, with the successor preserved.",
      "title": "A real-time explicit wrapper can still promote after controller retirement",
      "trigger": "A rider attack is queue-only in real time when newer horse work retires its generation before the rider queue advances."
    },
    {
      "acceptance_ids": [
        "MMR-03",
        "S04",
        "PR #64 R4-F8"
      ],
      "actual": "RetireSupersededPlayerOrder adds every unfinished, unstarted wrapper to retiredQueuedWrappers, including slot-only wrappers and entries it immediately removes from the queue. The sole removal is a future AdmitQueuedExplicitRun for that identity. Normal successful removal eliminates that future callback.",
      "blocking_rationale": "Repeated ordinary cancellations retain finished commands and their actor and target references for the controller\u2019s lifetime, regressing the required cleanup behavior.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A retired wrapper remains identifiable to a late promotion while finished commands and their referenced entities can be released.",
      "id": "R3-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedCombatController.cs",
        "line": 1699
      },
      "priority": "P2",
      "requested_correction": "Represent retirement on the exact wrapper or use an equivalent lifetime-bounded mechanism. Preserve the refusal of a promotion already in progress.",
      "requested_evidence": "Repeat supersession for slot-only and queue-only wrappers that never receive a later callback. Show bounded controller retention and continued refusal when a late promotion does arrive.",
      "title": "The retired-wrapper set retains commands that will never promote",
      "trigger": "A turn-based attack is superseded before it starts, and its wrapper is removed successfully or was already in a slot."
    },
    {
      "acceptance_ids": [
        "MMR-05",
        "S05"
      ],
      "actual": "TickApproach cancels for forbidden movement before evaluating current native strike admission. A rider whose target has just entered legal reach loses the whole order. The deferred-carrier branch already evaluates admission first.",
      "blocking_rationale": "A restriction on horse movement can suppress an otherwise legal stationary rider attack.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Once the rider\u2019s strike is admissible, the command releases its carrier and lets the ready rider attack from the current position.",
      "id": "R3-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedPairAttackCommand.cs",
        "line": 1464
      },
      "priority": "P2",
      "requested_correction": "Evaluate actual strike admission before applying the movement restriction to continued approach. Keep the out-of-range cancellation and exact-carrier cleanup.",
      "requested_evidence": "With a live carrier, revoke horse movement on the tick the target enters reach. Observe no further travel and one legal rider start. The still-out-of-range control must release the carrier and produce zero starts.",
      "title": "Movement revocation cancels an attack that no longer needs movement",
      "trigger": "An explicit rider approach is active when the horse loses movement and the target comes into the rider\u2019s legal strike range."
    },
    {
      "acceptance_ids": [
        "MMR-03",
        "S04"
      ],
      "actual": "The case labelled \u2018left reach before start\u2019 begins at 2.41 m with a 2.40 m horse radius, then moves the target to 3.50 m. It constructs the wrapper directly and calls ForbidApproach. The fixture never observes legal follow-through selection or an in-range-to-out-of-range transition.",
      "blocking_rationale": "The submitted regression cannot detect a failure connecting the controller\u2019s selection-time admission to the no-approach wrapper it dispatches.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "The moving-target case demonstrates that the controller admits an initially legal horse follow-through and that loss of reach before native start prevents both movement and attack.",
      "id": "R3-F4",
      "location": {
        "file": "tests/Chunk7Diagnostics/AttackPathHarnessTemplate.cs",
        "line": 908
      },
      "priority": "P2",
      "requested_correction": "Start inside the exact strike radius, drive the production selection/dispatch seam, and move the target outside only after observing horse dispatch.",
      "requested_evidence": "Report dispatch, carrier and start counts for initially outside, inside-then-outside and continuously inside cases.",
      "title": "The lost-reach regression starts outside reach",
      "trigger": "The focused harness reports that a target left horse reach after dispatch."
    },
    {
      "acceptance_ids": [
        "MMR-03",
        "S04"
      ],
      "actual": "The argument to MountJoinsIntent eagerly calls FollowThroughStrikeAdmissibleNow on every turn-based rider-order drive. Its probe constructs and initializes a native attack plan even while a wrapper is active or the accepted order is ranged.",
      "blocking_rationale": "This is avoidable work in the update path. No measured performance regression is claimed.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "The horse strike-plan probe runs when the dispatcher can actually consider horse follow-through.",
      "id": "R3-F5",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedCombatController.cs",
        "line": 1871
      },
      "priority": "P3",
      "requested_correction": "Short-circuit ineligible, ranged and already-active cases before building the horse plan.",
      "requested_evidence": "Use PlannedStrikeProbeCount to show zero probes during a continuing rider routine and a probe when horse follow-through is considered.",
      "title": "Turn-based updates rebuild a horse attack plan before it can be used",
      "trigger": "A retained turn-based rider order is driven repeatedly while the rider is still attacking or the horse cannot join."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F1",
      "rationale": "The selector uses the planned horse strike\u2019s exact admission, the turn-based follow-through receives no approach authority, and OnStart rechecks geometry. The separate defect in the submitted moving-target test is R3-F4."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F11",
      "rationale": "Generation and actor scoping are improved. A coexisting target write is still skipped, and an unsuccessful promotion can relinquish ownership without a write."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F12",
      "rationale": "The scoped operator excerpt now supplies attributable child and program counts, candidates and their relationship to submission. Its five record hashes match. The round-1 timestamps have one-second resolution."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F2",
      "rationale": "For the reported turn-based supersession, controller-first retirement removes the exact entry and promotion-first admission refuses it. This is verified by source and compiled wiring. The distinct real-time branch and strong-reference lifetime are R3-F1 and R3-F2."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F3",
      "rationale": "The new condition covers a started, unacted first windup. It excludes the documented native rider-only force-finish that marks IsActed without delivery, again clearing the eligible horse\u2019s order."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F4",
      "rationale": "OnStart checks movement permission before adopting a live carrier. The harness observed exact-carrier release with zero strikes on revocation and one start in the permitted control. R3-F3 concerns the later arrival tick."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F7",
      "rationale": "The goal now uses rider-eye geometry. Its nearest admissible projection can still fall inside the carrier\u2019s arrival radius while requiring a turn the horse never makes."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R2-F1",
      "rationale": "Child start alone no longer marks release. Real-time controller retirement still interrupts a released wrapper, and the harness never runs native first delivery or that retirement path."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R2-F2",
      "rationale": "Native Interrupt reaches OnEnded, which now releases the horse\u2019s pre-start approach and checks for another movement owner. The harness observed one stop without a successor, zero with a successor and zero attack starts."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R2-F3",
      "rationale": "The destination refresh is correct. The long-wait fixture omits the native started-command timeout, and the accumulated wait is still subtracted after the attack clock resets."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R2-F4",
      "rationale": "The fallback covers immediate exhaustion. Horse dispatch discards it before delivery, and general cancellation can leave it available to another generation."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R2-F5",
      "rationale": "The revised fixture asserts queue-only membership while the old Move is live. The exact cleanup sweep promotes the Standard successor, which receives zero StopMoving calls. The direct-successor and no-successor controls also passed."
    }
  ],
  "request_id": "kmc-codex-mmr-attack-orders-20261125103400-6cef-r03-f45c81",
  "round": 3,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The candidate adds same-activation horse selection, nearest-eligible melee target selection and pre-start approach under native strike readiness. The focused gate confirms several corrected command paths and the S08 regression checks. The remaining defects prevent acceptance of ordinary follow-through and real-time attack progression.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "N04 \u2014 Observe horse-attributed native attacks and routine cardinality in both initiative arrangements, including a spent or ended horse.",
      "N05 \u2014 Observe nearest eligible reachable hostile acquisition with two targets, including AI-off, Stop/Hold and retained explicit-target controls.",
      "N06 \u2014 Compare input, first movement, attack readiness and first strike with an equivalent unmounted actor.",
      "The owner controls any further formal review after this enrollment\u2019s third round."
    ],
    "scope_reviewed": "Final PR-1 checkpoint for MMR-03, MMR-04 and MMR-05 against S04, S05, S08 and the carried regression obligations. All 12 pending findings were assessed against the fixed candidate.",
    "unresolved_questions": [
      "The submitted checks do not establish a UnitAttack exemption from the native started-command timeout or the ordering of the first native OnAction relative to the wrapper\u2019s cancellation check."
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
