<!-- agent-review:v1 enrollment=kmc-codex-mmr-attack-orders-20261125103400-6cef key=r02-review part=1/1 sha256=7c3026bfea93eff94490bb0ca0440a8e054fa479c434ddff8c6f3088f9ad0f61 -->
### Codex GPT-6.2 Astra — review — round 2 — candidate `49b1c7b94e`

**Decision:** `changes_required` — The focused Fast gate passed on the submitted commit. Source inspection still shows ways for a turn-based horse to pursue without established movement entitlement, a retired attack to reach native queue promotion, and a rider-only interruption to discard an eligible horse attack. The real-time obstruction test also supplies the lateral movement it claims to demonstrate.

## Continuation for Claude

Use the planned strike’s native admission and delivery facts consistently at dispatch, promotion, start, and retirement. Keep enough generation and command identity to refuse stale queued work and release only the movement and manual target that the retiring order owns. For real-time approach, cover adoption of an existing carrier, a prolonged wait behind older work, loss of perception while the horse itself is moving, and a blocked rider sight line at point-arrival distance.

Drive the event orderings in the findings through the existing production seams. The queue fixture must promote the successor during cleanup, and the obstruction fixture must distinguish the rider’s strike sight from the horse’s sight of a world point. Supply the existing operator-ledger excerpt as review evidence and bind the focused gate receipt to the corrected candidate. Keep the work within PR-1 and its existing round and validation allowances. Turn-based click routing, the other child missions, packaging, and native owner observations retain their assigned scope.

#### Findings

**R1-F1** · P1 · defect · blocks readiness — Horse follow-through still admits targets outside the planned strike
- Location: `src/KingmakerMountedCombat/Integration/MountedCombatController.cs:1842`; acceptance: MMR-03, S04
- Trigger: A reach-weapon rider finishes, and the prepared horse is just outside its own reach or loses reach while its wrapper waits.
- Expected: A horse following the rider’s order has a currently admissible planned strike, or independently established authority for the movement it needs.
- Actual: IsMountAlreadyInPrimaryRange uses the selected weapon’s range, a 0.05 m tolerance, and HasLOS. MountedPairSingleAttack uses the planned weapon, exact native distance, and IsUnitEnoughClose with command sight. A horse just outside the exact radius can pass the first gate. The dispatch also sets allowApproach for ordinary melee, so a target leaving reach before start can cause a delegated move.
- Why it blocks / does not: An ordinary rider order can install a horse Standard and start pursuit without establishing the horse’s movement entitlement.
- Requested correction: Use the planned horse strike’s admission at dispatch and preserve the follow-through restriction through promotion and start. Settle a lost opportunity without starting an unauthorized carrier. Preserve explicit horse orders and native attack cardinality.
- Requested evidence: Use actual strike geometry for an immobile horse just outside its radius, an in-range control, and a target that leaves reach after dispatch. Record horse dispatches, carriers, and starts.

**R1-F2** · P1 · defect · blocks readiness — Controller-first retirement lets a stale explicit wrapper pass queue promotion
- Location: `src/KingmakerMountedCombat/Integration/MountedCombatController.cs:1697`; acceptance: MMR-03, S04
- Trigger: The rider wrapper is queue-only when the horse accepts a newer order. The controller retires the old order before the rider’s native queue advances.
- Expected: The superseded queued wrapper is refused before native Run can install it or write player state, whichever update runs first.
- Actual: RetireSupersededPlayerOrder clears stockIntentCommand and interrupts the queued wrapper. SweepFinishedCommand drops its reference when the command is absent from the raw slot. If the queue advances later, AdmitQueuedExplicitRun returns true for the now-unmatched reference. Kingmaker queue advancement can reach an interrupted entry, as the existing automatic-command regression documents.
- Why it blocks / does not: The older explicit command can enter native Run and overwrite Hold or manual selection after supersession, even if its interrupted attack never strikes.
- Requested correction: Preserve enough identity to refuse the exact retired wrapper at promotion, or safely remove that exact entry. Keep unrelated queued commands and legitimate actor-specific orders intact.
- Requested evidence: Queue the rider attack, accept competing horse work, and exercise both controller-retirement-first and promotion-first orderings. Verify zero stale slot installations or target/Hold writes and survival of the successor.

**R1-F3** · P1 · defect · blocks readiness — A rider-only block during first-strike windup still cancels the horse
- Location: `src/KingmakerMountedCombat/Integration/MountedCombatController.cs:2766`; acceptance: MMR-03, S04
- Trigger: The rider becomes dazed after native attack start and before its first attack rule, while the horse remains prepared and in range.
- Expected: When the rider delivers no strike because of an evidenced rider-only block, the same activation can still offer the valid target once to an independently eligible horse.
- Actual: The rider-only terminal branch requires ChildAttackStartCount == 0. StartChildAttack increments that count immediately after base.OnStart, before the animation delivers the first attack. A native RunningCannotAct or attack-ban terminal in that interval can have zero attack rules and zero completed attacks, yet the terminal handler clears the generation.
- Why it blocks / does not: The timing of a rider-only restriction still determines whether the prepared horse receives its legal attack.
- Requested correction: Distinguish an undelivered windup from a delivered attack using native terminal facts. Retire the rider attempt, then recheck the horse and the generation.
- Requested evidence: Interrupt a started rider during first-strike windup with an evidenced capability block. Show zero rider attacks, one eligible horse attempt, and zero attempts with a spent or ended horse.

**R1-F4** · P1 · defect · blocks readiness — Adopting a live carrier bypasses the movement-permission recheck
- Location: `src/KingmakerMountedCombat/Integration/MountedPairAttackCommand.cs:1141`; acceptance: MMR-04, MMR-05, S05
- Trigger: A real-time rider becomes strike-ready with an owned pre-start move still installed and movement newly forbidden.
- Expected: Native start rechecks current movement permission before it creates or adopts the rider’s carrier.
- Actual: OnStart returns as soon as it recognizes its live pre-start move. EndIfMovementForbiddenAtApproach runs below that return. The adopted-carrier path then enters ordinary TickApproach, which can continue or repath without consulting that callback.
- Why it blocks / does not: The wrapper can retain movement authority across the readiness handoff after the restriction that authorizes movement has changed.
- Requested correction: Apply the current movement check to adoption as well as fresh carrier admission. Release the exact owned approach when permission has ended and preserve any successor.
- Requested evidence: Establish a live pre-start carrier, revoke movement at handoff, then start and tick the wrapper. Show no continued or replacement carrier and no strike. Retain the permitted-adoption control.

**R1-F7** · P1 · defect · blocks readiness — Point-target sight does not resolve an obstructed rider strike
- Location: `src/KingmakerMountedCombat/Integration/MountedPairAttackCommand.cs:646`; acceptance: MMR-04, MMR-05, S05
- Trigger: The horse is within point-arrival distance. The rider’s eye line is obstructed, and a nearby position would admit the strike.
- Expected: The owned approach progresses toward a reachable position that admits the rider’s actual strike, preserving native sight and readiness at release.
- Actual: Both carrier issuers still target the hostile’s world point. Setting NeedLoS asks whether the horse sees that point. It can be clear while the rider’s strike line is blocked. The recorded Kingmaker contract also gives a point move blocker ID zero, which can make the hostile itself obstruct arrival. The harness discards the blocker ID and explicitly adds 0.25 m sideways per tick. Those steps account for the reported 0.75 m.
- Why it blocks / does not: A reachable rider strike can still stall or exhaust retries. The passing fixture supplies the missing movement and cannot establish the correction.
- Requested correction: Make pre-start and started carrier decisions account for the rider’s strike origin and target identity when distance alone is satisfied. Use a reachable movement goal and retain the exact native strike gate.
- Requested evidence: Exercise different rider and horse sight lines, the point move’s real blocker identity, and an available lateral route. Show how production selects or requests movement, zero strikes while blocked or cooling, and one legal start after arrival and readiness.

**R1-F11** · P2 · defect · blocks readiness — Manual-target ownership can cross generations or be relinquished without a write
- Location: `src/KingmakerMountedCombat/Integration/MountedCombatController.cs:939`; acceptance: MMR-03, MMR-04, S04, S05
- Trigger: An automatic order inherits an explicit selection, or competing work enters an actor’s queue while the current order owns its manual target.
- Expected: Cleanup releases only the relevant actor’s manual-target assignment made by the retiring generation.
- Actual: TryExecuteAction sets an actor-wide ownership flag for every nonautomatic action, including an explicit actor-specific action outside a stock generation. Its terminal can leave the flag set, and automatic Begin does not reset it. Retirement can then clear an inherited player target. Conversely, ObserveAcceptedWork drops ownership on any competing acceptance, including queue insertion that never wrote a target.
- Why it blocks / does not: A retired order can erase another order’s selection or leave its own stale selection available to native targeting.
- Requested correction: Bind manual-target ownership to the actor, generation, and assignment. Distinguish queue acceptance and promotion from an actual replacement target write.
- Requested evidence: Retire an automatic order after an actor-specific action selected the same enemy. Also cover a genuinely newer same-target assignment and a queued command that does not write a target. Check the selection after each retirement.

**R1-F12** · P2 · evidence_gap · blocks readiness — The allocation records are quoted only in the developer disposition
- Location: `docs/missions/mmr-split/PR-1-attack-orders.md:181`; acceptance: Mission §7, Mission §8
- Trigger: Round 2 is submitted under the existing child and program review limits.
- Expected: An attributable record ties the child and program allocation checks to their submissions.
- Actual: The disposition quotes dated records 8 and 11 and gives plausible counts and matching gate details. The submitted primary evidence contains the Fast receipt and log. These establish validation minutes and gate timing. Neither the supplemental ledger nor a captured readout establishes when the separate formal-round checks were recorded.
- Why it blocks / does not: The pre-submission checks required by the mission cannot be independently confirmed from the supplied records.
- Requested correction: Make the cited existing operator records available as a scoped primary-source excerpt or captured read-only output.
- Requested evidence: The dated entries showing the candidate, gate, child count, program count, and relationship to submission. A scoped excerpt is sufficient.

**R2-F1** · P1 · defect · blocks readiness — Cancellation treats the beginning of windup as a released strike
- Location: `src/KingmakerMountedCombat/Integration/MountedPairAttackCommand.cs:951`; acceptance: MMR-03, MMR-04, S04, S05
- Trigger: A newer player command is accepted, or an automatic target leaves perception, after the attack animation starts and before the first strike is released.
- Expected: Supersession and perception loss stop unreleased work. Native retains authority over a strike or projectile already released.
- Actual: StrikeReleased becomes true on any child start. Both RetainedOrderSuperseded and RetireAutomaticIntentIfUnperceived then leave the wrapper running. During first-strike windup the attack index and rule can still be empty. An already installed explicit wrapper also has no override check at OnStart or StartChildAttack if the newer acceptance precedes the next controller update.
- Why it blocks / does not: An older player order or an automatic order on a lost target can deliver its first strike after cancellation should have taken effect.
- Requested correction: Use native release evidence appropriate to the strike and enforce the retained generation’s cancellation before first delivery, including for an installed wrapper.
- Requested evidence: Accept a competing command on the other actor during windup and remove an automatic target from memory during windup. Check zero first deliveries. Include a genuinely released strike or projectile control.

**R2-F2** · P1 · defect · blocks readiness — Controller-first perception loss can leave the horse’s native path active
- Location: `src/KingmakerMountedCombat/Integration/MountedCombatController.cs:1720`; acceptance: MMR-04, MMR-05, S05
- Trigger: The horse is approaching as the automatic striker when the controller observes perception loss before the wrapper does.
- Expected: Perception loss releases movement owned by the retiring wrapper regardless of which observer notices first.
- Actual: Controller-first perception retirement calls InterruptOwnedCommand. Ordinary OnEnded releases delegatedMove only. When the horse is the striker, the native pre-start approach has no delegatedMove. The command-local perception guard calls StopNativePreStartApproach, but that guard never runs once the controller has interrupted the wrapper.
- Why it blocks / does not: The horse’s previously issued path can continue toward the lost target after the automatic order has been retired.
- Requested correction: Use ownership-aware retirement for the horse’s native pre-start movement as well as the rider’s delegated carrier. Preserve a successor already steering the horse.
- Requested evidence: Start a horse-owned automatic approach and remove the target from memory before the command’s next approach tick. Let the controller observe first. Check that its path stops, no strike starts, and a successor-path control keeps moving.

**R2-F3** · P2 · defect · blocks readiness — Waiting for older horse work consumes the chase timeout
- Location: `src/KingmakerMountedCombat/Integration/MountedPairAttackCommand.cs:1196`; acceptance: MMR-05, S05
- Trigger: A started real-time rider wrapper defers its first carrier behind older horse work for longer than the execution allowance.
- Expected: Time spent waiting for older work does not count as a failed chase. Legal movement begins from current target information once the slot is available.
- Actual: A deferred carrier leaves the wrapper started. OnTick applies the 8.5-second execution bound plus the capped travel allowance before checking carrierDeferred. A long cast or several older queued actions can therefore fault an order that has never moved. If the wait clears in time, BeginDelegatedMove initially uses the targetSnapshot taken before the wait.
- Why it blocks / does not: A legitimate player click can expire solely because older compatible horse work took time to finish.
- Requested correction: Account for the wait separately from bounded movement and attack execution. Refresh the movement destination on admission and retain cancellation, target validity, and existing bounds for an actual chase.
- Requested evidence: Hold the slot with older work beyond the configured movement allowance, move the target during the wait, then free the slot. Show one carrier toward the current target and one legal strike.

**R2-F4** · P2 · defect · blocks readiness — An entirely unexecuted pair order can now end silently
- Location: `src/KingmakerMountedCombat/Integration/MountedCombatController.cs:2769`; acceptance: MMR-03, S04
- Trigger: Native ends an admitted rider attempt without execution and the horse is already spent or ended.
- Expected: If neither actor executes, the player receives the evidenced refusal once. A successful horse follow-through completes without a spurious warning.
- Actual: The new rider-specific terminal returns before the unexecuted-order feedback path. When the horse cannot act, CompleteTurnBasedIntent asks GetRejectionFeedback, which returns null because the rider wrapper was previously dispatched. The order is then cleared as resources exhausted.
- Why it blocks / does not: A player can receive neither an attack nor the existing one-time explanation for a dropped click.
- Requested correction: Carry the rider’s unexecuted outcome until the horse decision settles. Deliver the existing feedback once if the whole order ends without execution.
- Requested evidence: Force-finish a rider for an evidenced capability block with the horse already spent. Show zero attacks, one visible refusal, and no retries. Compare with an eligible horse that follows through.

**R2-F5** · P2 · evidence_gap · blocks readiness — The queue-successor test promotes too early to verify cleanup ordering
- Location: `tests/Chunk7Diagnostics/AttackPathHarnessTemplate.cs:825`; acceptance: MMR-04, MMR-05, S05
- Trigger: The harness exercises a queued Standard successor while the old delegated Move is still live.
- Expected: The regression exercises a successor that becomes active inside the exact carrier’s cleanup sweep.
- Actual: The queue-promoted harness case calls RemoveFinishedAndUpdateQueue before order.Interrupt. Its facade promotes a Standard whenever the Standard slot is free, even with the live carrier in Move. The recorded native contract requires Move to be empty too. The asserted successor is therefore already installed when cleanup starts.
- Why it blocks / does not: Both reported successor cases exercise the same ordering. They would pass if the ownership check were accidentally moved ahead of the cleanup sweep.
- Requested correction: Match the established native Standard-promotion condition in the facade. Leave the successor queued until interrupting the carrier invokes the production cleanup sweep.
- Requested evidence: Assert queue-only membership before cleanup, the successor in Standard after the cleanup sweep, and zero StopMoving calls on its path. Keep the direct-successor and no-successor controls.

#### Prior finding assessments

- **R1-F1** — `fix_rejected` — The selector now consumes a reach boolean, but its producer disagrees with the planned native strike and follow-through still permits approach. The component case supplies the boolean directly.
- **R1-F2** — `fix_rejected` — Pending dispatch now observes supersession, and promotion-first retirement is guarded. Controller-first retirement forgets the queued wrapper before its later native Run.
- **R1-F3** — `fix_rejected` — The generation-scoped mark works before child start. A causal rider-only block after start but before first delivery still clears the horse’s opportunity.
- **R1-F4** — `fix_rejected` — The short wait behind older work and the newly forbidden deferred case passed in the harness. The live-carrier adoption branch returns before the start-time permission check.
- **R1-F5** — `fix_verified` — The update checks both raw-slot and queue membership. An unfinished, unstarted explicit orphan releases activeCommand, clears only its matching intent, and interrupts the exact missing wrapper. This was verified by source inspection and compiled wiring.
- **R1-F6** — `fix_verified` — Production now inspects the Move slot and approaching commands in other raw slots after the sweep before calling StopMoving. R2-F5 identifies a separate flaw in the harness evidence for promotion during that sweep.
- **R1-F7** — `fix_rejected` — Enabling sight on a world-point command does not make its arrival predicate match the rider’s strike. The harness hard-codes the lateral step and ignores the native blocker identity.
- **R1-F8** — `fix_verified` — A live owned pre-start carrier now satisfies IsApproachingBeforeAttack. The forwarded group stop synchronously clears the matching intent and interrupts that wrapper before cancelling mount movement. The harness exercised the predicate.
- **R1-F9** — `fix_verified` — For the reported no-wrapper case, removal from the rider’s populated group memory now retires the generation before the readiness probe and before refusal of a new proposal. R2-F2 covers the distinct active horse-movement case.
- **R1-F10** — `fix_verified` — Fresh melee selection now compares in-reach weights 0 and 1. The production-seam component case covers the nearer candidate and preserves manual-target, live-order, ranged, and ineligible-candidate controls.
- **R1-F11** — `fix_rejected` — The actor-wide flags narrow cleanup, but a non-stock assignment can survive into another generation and an accepted command can relinquish ownership without writing a target.
- **R1-F12** — `fix_rejected` — The quoted sequence numbers, times, and counts identify the requested records. The controller evidence corroborates the current gate and minute balance. The separate pre-submission ledger observations remain available only as developer claims.
- **R1-F13** — `fix_verified` — ObserveEngagementProgress resets at the combat boundary, then returns on Reported before sampling player operations, slots, or movement. This verifies the requested source change.

#### Evidence assessment

- **Inspected:** The enrolled mission, AGENTS.md, validation policy, CURRENT.md, AUTONOMOUS-RESUME.md, full feature diff, correction commit, 13 prior findings, developer dispositions, transcript notice, and controller evidence.; The retained-order selector, controller, native Run hooks, paired attack and movement commands, terminal policies, affected tests, and repository records of Kingmaker queue promotion, point-target sight, native interruption, and player feedback.
- **Executed verification:** Read-only Git checks confirmed HEAD and the merge base. The feature diff passed git diff --check, and the six whole files designated for extraction matched 46444b30 at the extraction commit.; Verified the controller receipt and its log digest ae9733890ccd724bd5c7a9721665142cb398468873de35b1fabc2c65a3020724. The focused Fast gate ran at 49b1c7b94e9a63b08b005abf440ee7585e848516, exited 0 in 38.6 seconds, and recorded an unchanged worktree. All 15 focused children passed.; The log records 1,542 component checks, 153 source checks, 4 attack-order native contract checks, 56 lifecycle checks, and 29 attack-path harness checks passing. Mounted Shield passed 114 checks, selection rollback passed 68, and the output icon matched the expected embedded bytes. Both required UMM binding profiles passed.
- **Source inspection only:** Verified the explicit orphan cleanup, post-sweep successor inspection, forwarded group-stop wiring, perception check for a retained target with no wrapper and a populated memory, and early exit after engagement reporting.; The harness checks the pre-start approach predicate. It does not forward a trap stop and advance past retry. The compiled lifecycle checks likewise do not replay an explicit queue drop or a replacement brain proposal.
- **Required gaps:** Exercise exact horse strike admission, both queue-retirement orderings, and a rider-only capability interruption between native start and first strike.; Exercise movement permission at carrier adoption, a wait longer than the chase allowance, controller-first perception loss during horse movement, and a reachable rider sight position with faithful point-target sight semantics.; Demonstrate manual-target ownership across generations, feedback for a wholly unexecuted click, and successor promotion inside carrier cleanup.; Provide an attributable copy or scoped readout of the cited pre-review allocation records.
- **Limitations:** This review used the read-only snapshot. Executed build and test results come from the controller receipt. The mixed developer-session transcript was withheld.; The attack-path harness uses engine facades. Its recorded movement and attack-start counts are offline observations. The seven new lifecycle checks inspect compiled references and calls.; The gate recorded 75.97 of 90 validation minutes spent, with 14.03 remaining and zero unreconciled entries. The supplemental formal-round ledger is separate from that receipt.

#### Summary

- **Delivered behavior:** The candidate passes the required offline gate and preserves the checked Mounted Shield and loader regressions. It corrects explicit orphan cleanup, successor inspection, pre-start group-stop recognition, fresh melee target ranking, and several short-wait and retirement paths. The remaining findings affect ordinary pair orders and lawful real-time movement.
- **Scope reviewed:** Final PR-1 checkpoint for MMR-03, MMR-04, MMR-05, and S04, S05, S08. Reviewed the fixed candidate, every prior disposition, the correction commit, and its directly affected callers and tests.
- **Pending later-phase work:** none
- **Unresolved questions:** The supplied records establish the current minute balance. Independent confirmation of the cited pre-review formal-round checks remains outstanding.
- **Remaining owner review:** N04 covers horse-attributed attacks and native spending under both initiative arrangements, including an already-used horse.; N05 covers nearest eligible real-time engagement with AI, Stop, Hold, and explicit target controls.; N06 covers actual movement responsiveness and first strike against a comparable unmounted actor.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a21395-879c-7123-8347-de34410ebff9`, duration 1193.8s, response sha256 `7b929ebf1a7e15fbbd745d4f788d5f13921e5bd32e2d822d8555f42df63e0a68`, request `kmc-codex-mmr-attack-orders-20261125103400-6cef-r02-268bf9`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "49b1c7b94e9a63b08b005abf440ee7585e848516",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The focused Fast gate passed on the submitted commit. Source inspection still shows ways for a turn-based horse to pursue without established movement entitlement, a retired attack to reach native queue promotion, and a rider-only interruption to discard an eligible horse attack. The real-time obstruction test also supplies the lateral movement it claims to demonstrate.\n\n## Continuation for Claude\n\nUse the planned strike\u2019s native admission and delivery facts consistently at dispatch, promotion, start, and retirement. Keep enough generation and command identity to refuse stale queued work and release only the movement and manual target that the retiring order owns. For real-time approach, cover adoption of an existing carrier, a prolonged wait behind older work, loss of perception while the horse itself is moving, and a blocked rider sight line at point-arrival distance.\n\nDrive the event orderings in the findings through the existing production seams. The queue fixture must promote the successor during cleanup, and the obstruction fixture must distinguish the rider\u2019s strike sight from the horse\u2019s sight of a world point. Supply the existing operator-ledger excerpt as review evidence and bind the focused gate receipt to the corrected candidate. Keep the work within PR-1 and its existing round and validation allowances. Turn-based click routing, the other child missions, packaging, and native owner observations retain their assigned scope.",
  "evidence_assessment": {
    "executed_verification": [
      "Read-only Git checks confirmed HEAD and the merge base. The feature diff passed git diff --check, and the six whole files designated for extraction matched 46444b30 at the extraction commit.",
      "Verified the controller receipt and its log digest ae9733890ccd724bd5c7a9721665142cb398468873de35b1fabc2c65a3020724. The focused Fast gate ran at 49b1c7b94e9a63b08b005abf440ee7585e848516, exited 0 in 38.6 seconds, and recorded an unchanged worktree. All 15 focused children passed.",
      "The log records 1,542 component checks, 153 source checks, 4 attack-order native contract checks, 56 lifecycle checks, and 29 attack-path harness checks passing. Mounted Shield passed 114 checks, selection rollback passed 68, and the output icon matched the expected embedded bytes. Both required UMM binding profiles passed."
    ],
    "inspected": [
      "The enrolled mission, AGENTS.md, validation policy, CURRENT.md, AUTONOMOUS-RESUME.md, full feature diff, correction commit, 13 prior findings, developer dispositions, transcript notice, and controller evidence.",
      "The retained-order selector, controller, native Run hooks, paired attack and movement commands, terminal policies, affected tests, and repository records of Kingmaker queue promotion, point-target sight, native interruption, and player feedback."
    ],
    "limitations": [
      "This review used the read-only snapshot. Executed build and test results come from the controller receipt. The mixed developer-session transcript was withheld.",
      "The attack-path harness uses engine facades. Its recorded movement and attack-start counts are offline observations. The seven new lifecycle checks inspect compiled references and calls.",
      "The gate recorded 75.97 of 90 validation minutes spent, with 14.03 remaining and zero unreconciled entries. The supplemental formal-round ledger is separate from that receipt."
    ],
    "required_gaps": [
      "Exercise exact horse strike admission, both queue-retirement orderings, and a rider-only capability interruption between native start and first strike.",
      "Exercise movement permission at carrier adoption, a wait longer than the chase allowance, controller-first perception loss during horse movement, and a reachable rider sight position with faithful point-target sight semantics.",
      "Demonstrate manual-target ownership across generations, feedback for a wholly unexecuted click, and successor promotion inside carrier cleanup.",
      "Provide an attributable copy or scoped readout of the cited pre-review allocation records."
    ],
    "source_inspection_only": [
      "Verified the explicit orphan cleanup, post-sweep successor inspection, forwarded group-stop wiring, perception check for a retained target with no wrapper and a populated memory, and early exit after engagement reporting.",
      "The harness checks the pre-start approach predicate. It does not forward a trap stop and advance past retry. The compiled lifecycle checks likewise do not replay an explicit queue drop or a replacement brain proposal."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "MMR-03",
        "S04"
      ],
      "actual": "IsMountAlreadyInPrimaryRange uses the selected weapon\u2019s range, a 0.05 m tolerance, and HasLOS. MountedPairSingleAttack uses the planned weapon, exact native distance, and IsUnitEnoughClose with command sight. A horse just outside the exact radius can pass the first gate. The dispatch also sets allowApproach for ordinary melee, so a target leaving reach before start can cause a delegated move.",
      "blocking_rationale": "An ordinary rider order can install a horse Standard and start pursuit without establishing the horse\u2019s movement entitlement.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A horse following the rider\u2019s order has a currently admissible planned strike, or independently established authority for the movement it needs.",
      "id": "R1-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedCombatController.cs",
        "line": 1842
      },
      "priority": "P1",
      "requested_correction": "Use the planned horse strike\u2019s admission at dispatch and preserve the follow-through restriction through promotion and start. Settle a lost opportunity without starting an unauthorized carrier. Preserve explicit horse orders and native attack cardinality.",
      "requested_evidence": "Use actual strike geometry for an immobile horse just outside its radius, an in-range control, and a target that leaves reach after dispatch. Record horse dispatches, carriers, and starts.",
      "title": "Horse follow-through still admits targets outside the planned strike",
      "trigger": "A reach-weapon rider finishes, and the prepared horse is just outside its own reach or loses reach while its wrapper waits."
    },
    {
      "acceptance_ids": [
        "MMR-03",
        "S04"
      ],
      "actual": "RetireSupersededPlayerOrder clears stockIntentCommand and interrupts the queued wrapper. SweepFinishedCommand drops its reference when the command is absent from the raw slot. If the queue advances later, AdmitQueuedExplicitRun returns true for the now-unmatched reference. Kingmaker queue advancement can reach an interrupted entry, as the existing automatic-command regression documents.",
      "blocking_rationale": "The older explicit command can enter native Run and overwrite Hold or manual selection after supersession, even if its interrupted attack never strikes.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The superseded queued wrapper is refused before native Run can install it or write player state, whichever update runs first.",
      "id": "R1-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedCombatController.cs",
        "line": 1697
      },
      "priority": "P1",
      "requested_correction": "Preserve enough identity to refuse the exact retired wrapper at promotion, or safely remove that exact entry. Keep unrelated queued commands and legitimate actor-specific orders intact.",
      "requested_evidence": "Queue the rider attack, accept competing horse work, and exercise both controller-retirement-first and promotion-first orderings. Verify zero stale slot installations or target/Hold writes and survival of the successor.",
      "title": "Controller-first retirement lets a stale explicit wrapper pass queue promotion",
      "trigger": "The rider wrapper is queue-only when the horse accepts a newer order. The controller retires the old order before the rider\u2019s native queue advances."
    },
    {
      "acceptance_ids": [
        "MMR-03",
        "S04"
      ],
      "actual": "The rider-only terminal branch requires ChildAttackStartCount == 0. StartChildAttack increments that count immediately after base.OnStart, before the animation delivers the first attack. A native RunningCannotAct or attack-ban terminal in that interval can have zero attack rules and zero completed attacks, yet the terminal handler clears the generation.",
      "blocking_rationale": "The timing of a rider-only restriction still determines whether the prepared horse receives its legal attack.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "When the rider delivers no strike because of an evidenced rider-only block, the same activation can still offer the valid target once to an independently eligible horse.",
      "id": "R1-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedCombatController.cs",
        "line": 2766
      },
      "priority": "P1",
      "requested_correction": "Distinguish an undelivered windup from a delivered attack using native terminal facts. Retire the rider attempt, then recheck the horse and the generation.",
      "requested_evidence": "Interrupt a started rider during first-strike windup with an evidenced capability block. Show zero rider attacks, one eligible horse attempt, and zero attempts with a spent or ended horse.",
      "title": "A rider-only block during first-strike windup still cancels the horse",
      "trigger": "The rider becomes dazed after native attack start and before its first attack rule, while the horse remains prepared and in range."
    },
    {
      "acceptance_ids": [
        "MMR-04",
        "MMR-05",
        "S05"
      ],
      "actual": "OnStart returns as soon as it recognizes its live pre-start move. EndIfMovementForbiddenAtApproach runs below that return. The adopted-carrier path then enters ordinary TickApproach, which can continue or repath without consulting that callback.",
      "blocking_rationale": "The wrapper can retain movement authority across the readiness handoff after the restriction that authorizes movement has changed.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Native start rechecks current movement permission before it creates or adopts the rider\u2019s carrier.",
      "id": "R1-F4",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedPairAttackCommand.cs",
        "line": 1141
      },
      "priority": "P1",
      "requested_correction": "Apply the current movement check to adoption as well as fresh carrier admission. Release the exact owned approach when permission has ended and preserve any successor.",
      "requested_evidence": "Establish a live pre-start carrier, revoke movement at handoff, then start and tick the wrapper. Show no continued or replacement carrier and no strike. Retain the permitted-adoption control.",
      "title": "Adopting a live carrier bypasses the movement-permission recheck",
      "trigger": "A real-time rider becomes strike-ready with an owned pre-start move still installed and movement newly forbidden."
    },
    {
      "acceptance_ids": [
        "MMR-04",
        "MMR-05",
        "S05"
      ],
      "actual": "Both carrier issuers still target the hostile\u2019s world point. Setting NeedLoS asks whether the horse sees that point. It can be clear while the rider\u2019s strike line is blocked. The recorded Kingmaker contract also gives a point move blocker ID zero, which can make the hostile itself obstruct arrival. The harness discards the blocker ID and explicitly adds 0.25 m sideways per tick. Those steps account for the reported 0.75 m.",
      "blocking_rationale": "A reachable rider strike can still stall or exhaust retries. The passing fixture supplies the missing movement and cannot establish the correction.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The owned approach progresses toward a reachable position that admits the rider\u2019s actual strike, preserving native sight and readiness at release.",
      "id": "R1-F7",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedPairAttackCommand.cs",
        "line": 646
      },
      "priority": "P1",
      "requested_correction": "Make pre-start and started carrier decisions account for the rider\u2019s strike origin and target identity when distance alone is satisfied. Use a reachable movement goal and retain the exact native strike gate.",
      "requested_evidence": "Exercise different rider and horse sight lines, the point move\u2019s real blocker identity, and an available lateral route. Show how production selects or requests movement, zero strikes while blocked or cooling, and one legal start after arrival and readiness.",
      "title": "Point-target sight does not resolve an obstructed rider strike",
      "trigger": "The horse is within point-arrival distance. The rider\u2019s eye line is obstructed, and a nearby position would admit the strike."
    },
    {
      "acceptance_ids": [
        "MMR-03",
        "MMR-04",
        "S04",
        "S05"
      ],
      "actual": "TryExecuteAction sets an actor-wide ownership flag for every nonautomatic action, including an explicit actor-specific action outside a stock generation. Its terminal can leave the flag set, and automatic Begin does not reset it. Retirement can then clear an inherited player target. Conversely, ObserveAcceptedWork drops ownership on any competing acceptance, including queue insertion that never wrote a target.",
      "blocking_rationale": "A retired order can erase another order\u2019s selection or leave its own stale selection available to native targeting.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Cleanup releases only the relevant actor\u2019s manual-target assignment made by the retiring generation.",
      "id": "R1-F11",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedCombatController.cs",
        "line": 939
      },
      "priority": "P2",
      "requested_correction": "Bind manual-target ownership to the actor, generation, and assignment. Distinguish queue acceptance and promotion from an actual replacement target write.",
      "requested_evidence": "Retire an automatic order after an actor-specific action selected the same enemy. Also cover a genuinely newer same-target assignment and a queued command that does not write a target. Check the selection after each retirement.",
      "title": "Manual-target ownership can cross generations or be relinquished without a write",
      "trigger": "An automatic order inherits an explicit selection, or competing work enters an actor\u2019s queue while the current order owns its manual target."
    },
    {
      "acceptance_ids": [
        "Mission \u00a77",
        "Mission \u00a78"
      ],
      "actual": "The disposition quotes dated records 8 and 11 and gives plausible counts and matching gate details. The submitted primary evidence contains the Fast receipt and log. These establish validation minutes and gate timing. Neither the supplemental ledger nor a captured readout establishes when the separate formal-round checks were recorded.",
      "blocking_rationale": "The pre-submission checks required by the mission cannot be independently confirmed from the supplied records.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "An attributable record ties the child and program allocation checks to their submissions.",
      "id": "R1-F12",
      "location": {
        "file": "docs/missions/mmr-split/PR-1-attack-orders.md",
        "line": 181
      },
      "priority": "P2",
      "requested_correction": "Make the cited existing operator records available as a scoped primary-source excerpt or captured read-only output.",
      "requested_evidence": "The dated entries showing the candidate, gate, child count, program count, and relationship to submission. A scoped excerpt is sufficient.",
      "title": "The allocation records are quoted only in the developer disposition",
      "trigger": "Round 2 is submitted under the existing child and program review limits."
    },
    {
      "acceptance_ids": [
        "MMR-03",
        "MMR-04",
        "S04",
        "S05"
      ],
      "actual": "StrikeReleased becomes true on any child start. Both RetainedOrderSuperseded and RetireAutomaticIntentIfUnperceived then leave the wrapper running. During first-strike windup the attack index and rule can still be empty. An already installed explicit wrapper also has no override check at OnStart or StartChildAttack if the newer acceptance precedes the next controller update.",
      "blocking_rationale": "An older player order or an automatic order on a lost target can deliver its first strike after cancellation should have taken effect.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Supersession and perception loss stop unreleased work. Native retains authority over a strike or projectile already released.",
      "id": "R2-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedPairAttackCommand.cs",
        "line": 951
      },
      "priority": "P1",
      "requested_correction": "Use native release evidence appropriate to the strike and enforce the retained generation\u2019s cancellation before first delivery, including for an installed wrapper.",
      "requested_evidence": "Accept a competing command on the other actor during windup and remove an automatic target from memory during windup. Check zero first deliveries. Include a genuinely released strike or projectile control.",
      "title": "Cancellation treats the beginning of windup as a released strike",
      "trigger": "A newer player command is accepted, or an automatic target leaves perception, after the attack animation starts and before the first strike is released."
    },
    {
      "acceptance_ids": [
        "MMR-04",
        "MMR-05",
        "S05"
      ],
      "actual": "Controller-first perception retirement calls InterruptOwnedCommand. Ordinary OnEnded releases delegatedMove only. When the horse is the striker, the native pre-start approach has no delegatedMove. The command-local perception guard calls StopNativePreStartApproach, but that guard never runs once the controller has interrupted the wrapper.",
      "blocking_rationale": "The horse\u2019s previously issued path can continue toward the lost target after the automatic order has been retired.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Perception loss releases movement owned by the retiring wrapper regardless of which observer notices first.",
      "id": "R2-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedCombatController.cs",
        "line": 1720
      },
      "priority": "P1",
      "requested_correction": "Use ownership-aware retirement for the horse\u2019s native pre-start movement as well as the rider\u2019s delegated carrier. Preserve a successor already steering the horse.",
      "requested_evidence": "Start a horse-owned automatic approach and remove the target from memory before the command\u2019s next approach tick. Let the controller observe first. Check that its path stops, no strike starts, and a successor-path control keeps moving.",
      "title": "Controller-first perception loss can leave the horse\u2019s native path active",
      "trigger": "The horse is approaching as the automatic striker when the controller observes perception loss before the wrapper does."
    },
    {
      "acceptance_ids": [
        "MMR-05",
        "S05"
      ],
      "actual": "A deferred carrier leaves the wrapper started. OnTick applies the 8.5-second execution bound plus the capped travel allowance before checking carrierDeferred. A long cast or several older queued actions can therefore fault an order that has never moved. If the wait clears in time, BeginDelegatedMove initially uses the targetSnapshot taken before the wait.",
      "blocking_rationale": "A legitimate player click can expire solely because older compatible horse work took time to finish.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Time spent waiting for older work does not count as a failed chase. Legal movement begins from current target information once the slot is available.",
      "id": "R2-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedPairAttackCommand.cs",
        "line": 1196
      },
      "priority": "P2",
      "requested_correction": "Account for the wait separately from bounded movement and attack execution. Refresh the movement destination on admission and retain cancellation, target validity, and existing bounds for an actual chase.",
      "requested_evidence": "Hold the slot with older work beyond the configured movement allowance, move the target during the wait, then free the slot. Show one carrier toward the current target and one legal strike.",
      "title": "Waiting for older horse work consumes the chase timeout",
      "trigger": "A started real-time rider wrapper defers its first carrier behind older horse work for longer than the execution allowance."
    },
    {
      "acceptance_ids": [
        "MMR-03",
        "S04"
      ],
      "actual": "The new rider-specific terminal returns before the unexecuted-order feedback path. When the horse cannot act, CompleteTurnBasedIntent asks GetRejectionFeedback, which returns null because the rider wrapper was previously dispatched. The order is then cleared as resources exhausted.",
      "blocking_rationale": "A player can receive neither an attack nor the existing one-time explanation for a dropped click.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "If neither actor executes, the player receives the evidenced refusal once. A successful horse follow-through completes without a spurious warning.",
      "id": "R2-F4",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedCombatController.cs",
        "line": 2769
      },
      "priority": "P2",
      "requested_correction": "Carry the rider\u2019s unexecuted outcome until the horse decision settles. Deliver the existing feedback once if the whole order ends without execution.",
      "requested_evidence": "Force-finish a rider for an evidenced capability block with the horse already spent. Show zero attacks, one visible refusal, and no retries. Compare with an eligible horse that follows through.",
      "title": "An entirely unexecuted pair order can now end silently",
      "trigger": "Native ends an admitted rider attempt without execution and the horse is already spent or ended."
    },
    {
      "acceptance_ids": [
        "MMR-04",
        "MMR-05",
        "S05"
      ],
      "actual": "The queue-promoted harness case calls RemoveFinishedAndUpdateQueue before order.Interrupt. Its facade promotes a Standard whenever the Standard slot is free, even with the live carrier in Move. The recorded native contract requires Move to be empty too. The asserted successor is therefore already installed when cleanup starts.",
      "blocking_rationale": "Both reported successor cases exercise the same ordering. They would pass if the ownership check were accidentally moved ahead of the cleanup sweep.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "The regression exercises a successor that becomes active inside the exact carrier\u2019s cleanup sweep.",
      "id": "R2-F5",
      "location": {
        "file": "tests/Chunk7Diagnostics/AttackPathHarnessTemplate.cs",
        "line": 825
      },
      "priority": "P2",
      "requested_correction": "Match the established native Standard-promotion condition in the facade. Leave the successor queued until interrupting the carrier invokes the production cleanup sweep.",
      "requested_evidence": "Assert queue-only membership before cleanup, the successor in Standard after the cleanup sweep, and zero StopMoving calls on its path. Keep the direct-successor and no-successor controls.",
      "title": "The queue-successor test promotes too early to verify cleanup ordering",
      "trigger": "The harness exercises a queued Standard successor while the old delegated Move is still live."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F1",
      "rationale": "The selector now consumes a reach boolean, but its producer disagrees with the planned native strike and follow-through still permits approach. The component case supplies the boolean directly."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F2",
      "rationale": "Pending dispatch now observes supersession, and promotion-first retirement is guarded. Controller-first retirement forgets the queued wrapper before its later native Run."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F3",
      "rationale": "The generation-scoped mark works before child start. A causal rider-only block after start but before first delivery still clears the horse\u2019s opportunity."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F4",
      "rationale": "The short wait behind older work and the newly forbidden deferred case passed in the harness. The live-carrier adoption branch returns before the start-time permission check."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F5",
      "rationale": "The update checks both raw-slot and queue membership. An unfinished, unstarted explicit orphan releases activeCommand, clears only its matching intent, and interrupts the exact missing wrapper. This was verified by source inspection and compiled wiring."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F6",
      "rationale": "Production now inspects the Move slot and approaching commands in other raw slots after the sweep before calling StopMoving. R2-F5 identifies a separate flaw in the harness evidence for promotion during that sweep."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F7",
      "rationale": "Enabling sight on a world-point command does not make its arrival predicate match the rider\u2019s strike. The harness hard-codes the lateral step and ignores the native blocker identity."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F8",
      "rationale": "A live owned pre-start carrier now satisfies IsApproachingBeforeAttack. The forwarded group stop synchronously clears the matching intent and interrupts that wrapper before cancelling mount movement. The harness exercised the predicate."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F9",
      "rationale": "For the reported no-wrapper case, removal from the rider\u2019s populated group memory now retires the generation before the readiness probe and before refusal of a new proposal. R2-F2 covers the distinct active horse-movement case."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F10",
      "rationale": "Fresh melee selection now compares in-reach weights 0 and 1. The production-seam component case covers the nearer candidate and preserves manual-target, live-order, ranged, and ineligible-candidate controls."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F11",
      "rationale": "The actor-wide flags narrow cleanup, but a non-stock assignment can survive into another generation and an accepted command can relinquish ownership without writing a target."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F12",
      "rationale": "The quoted sequence numbers, times, and counts identify the requested records. The controller evidence corroborates the current gate and minute balance. The separate pre-submission ledger observations remain available only as developer claims."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F13",
      "rationale": "ObserveEngagementProgress resets at the combat boundary, then returns on Reported before sampling player operations, slots, or movement. This verifies the requested source change."
    }
  ],
  "request_id": "kmc-codex-mmr-attack-orders-20261125103400-6cef-r02-268bf9",
  "round": 2,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The candidate passes the required offline gate and preserves the checked Mounted Shield and loader regressions. It corrects explicit orphan cleanup, successor inspection, pre-start group-stop recognition, fresh melee target ranking, and several short-wait and retirement paths. The remaining findings affect ordinary pair orders and lawful real-time movement.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "N04 covers horse-attributed attacks and native spending under both initiative arrangements, including an already-used horse.",
      "N05 covers nearest eligible real-time engagement with AI, Stop, Hold, and explicit target controls.",
      "N06 covers actual movement responsiveness and first strike against a comparable unmounted actor."
    ],
    "scope_reviewed": "Final PR-1 checkpoint for MMR-03, MMR-04, MMR-05, and S04, S05, S08. Reviewed the fixed candidate, every prior disposition, the correction commit, and its directly affected callers and tests.",
    "unresolved_questions": [
      "The supplied records establish the current minute balance. Independent confirmation of the cited pre-review formal-round checks remains outstanding."
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
