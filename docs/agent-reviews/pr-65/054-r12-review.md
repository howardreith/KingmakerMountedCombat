<!-- agent-review:v1 enrollment=kmc-codex-mmr-attack-orders-20261125103400-6cef key=r12-review part=1/1 sha256=0c55389e636c398453b894e74b30ad1efa5c662a0e9646bbd48084d5ae5ad72e -->
### Codex GPT-6.2 Astra — review — round 12 — candidate `29816172ad`

**Decision:** `changes_required` — The four round-11 production issues are corrected, and the focused gate passed. Two issues block readiness. Switching AI off after the first strike lands can leave the repeating order alive. The new mount-only Hold test also supplies the movement restriction directly and omits the requested spending observation.

Continuation for Claude

When the controller observes AI-off, retire the repeating automatic order while allowing its already-delivered native routine to finish. Exercise the production drive between the first and final attacks, then verify that the old order cannot dispatch again after AI is re-enabled. For the Hold case, set the horse’s HoldState, derive the restriction through production code, and measure spending before restoring readiness. Preserve explicit orders, turn-based authority, native attack cardinality and the existing pre-delivery controls. Run the prescribed focused Fast gate on the corrected candidate within the existing limits. R12-F3 and R12-F4 are optional.

#### Findings

**R12-F1** · P2 · defect · blocks readiness — AI-off after first delivery can leave the repeating order alive
- Location: `src/KingmakerMountedCombat/Integration/MountedCombatController.cs:1891`; acceptance: MMR-04, S05
- Trigger: The rider lands the first attack of a real-time automatic routine. Its target remains valid, and the controller observes AI-off while the wrapper still has a native attack to finish.
- Expected: The AI-off update retires future dispatch for that generation. The native routine already in progress completes its remaining attacks with its original action cost.
- Actual: TryRetireStockIntent now returns directly from RetireAutomaticOrderIfNotAdmitted for every automatic order. That method returns false at line 1644 when StrikeReleased is true, before reading AI state. If AI is re-enabled before the routine finishes, successful terminal settlement keeps the old generation and the next drive can dispatch the horse against the old target without a fresh brain proposal.
- Why it blocks / does not: An observed AI-off interval can be lost, allowing a stale target to consume the partner’s next legal attack.
- Requested correction: Apply the order’s AI-off retirement rule on the controller path after delivery. Release the generation and preserve the live wrapper through the existing native-completion path.
- Requested evidence: Deliver the first act of an automatic two-attack routine, switch AI off, and drive once. Assert that the intent is cleared and the wrapper is still running. Re-enable AI before the remaining act, observe normal completion with one Standard spent, then drive with an eligible horse. Expect zero new dispatches until a fresh proposal.

**R12-F2** · P2 · evidence_gap · blocks readiness — The mount-only Hold replay bypasses the horse’s Hold input
- Location: `tests/Chunk7Diagnostics/AttackPathHarnessTemplate.cs:3281`; acceptance: MMR-04, S05
- Trigger: The focused harness reports that the horse’s Hold caused B to retire before A’s proposal was decided.
- Expected: Only the horse is held, the production movement predicate reads that state, and the fixture measures B’s spending before either actor becomes ready to strike A.
- Actual: Round12MountOnlyHold sets controller.MovementForbidden directly. The harness adapter returns that flag and the horse’s HoldState is never set. The case captures StandardSpends at line 3289 but neither compares nor prints its delta before enabling both Standards. The log establishes target selection and zero travel under an injected restriction.
- Why it blocks / does not: The requested mount-only trigger and retired-target spending observation remain unverified in this replay. Losing the horse’s Hold input in the controller adapter would leave both rows green.
- Requested correction: Drive this case from the simulated horse’s HoldState through the production AutomaticMovementForbidden method. Keep the rider mobile and both targets unmarked. Assert and print the pre-readiness spending delta.
- Requested evidence: With B outside stationary reach, record acceptance of A on that proposal, zero B movement and spending, and one stationary delivery on A after readiness returns. With B still in reach, record retention and one stationary delivery on B.

**R12-F3** · P3 · optional_polish · non-blocking — A started approach can bypass automatic-retirement diagnostics
- Location: `src/KingmakerMountedCombat/Integration/MountedPairAttackCommand.cs:1572`; acceptance: MMR-04, S05
- Trigger: During an automatic rider approach, the conscious horse loses CanMove while its perceived target remains outside strike reach. The wrapper ticks before the controller.
- Expected: Either observer records the automatic retirement once with the same reason.
- Actual: TickApproach calls EndIfMovementForbiddenAtApproach before the shared automatic decision on its out-of-range path. The wrapper cancels itself and terminal settlement clears the intent. A controller-first observation records 'hold out of reach' and increments the automatic-retirement counter. A wrapper-first observation bypasses both diagnostic writes.
- Why it blocks / does not: Both paths stop the approach. The discrepancy affects the reason and count reported for the same stop.
- Requested correction: Let the owning automatic order decide this movement restriction before the generic approach cancellation.
- Requested evidence: If addressed, compare controller-first and wrapper-first ticks with a live carrier. Check the retirement reason and count, exact carrier cleanup, and zero delivery.

**R12-F4** · P3 · optional_polish · non-blocking — A waiting stationary order builds the same strike probe twice per drive
- Location: `src/KingmakerMountedCombat/Integration/MountedCombatController.cs:2038`; acceptance: MMR-04, S05
- Trigger: Both Standards are cooling and a held pair retains an in-range melee target without a live wrapper.
- Expected: One controller drive needs one automatic-retirement decision for the same waiting order.
- Actual: TryDriveStockAttackIntent checks automatic admission at line 2016 and calls it again through TryRetireStockIntent at line 2038. Each check now constructs a planned stationary strike when the held melee order has no wrapper.
- Why it blocks / does not: The duplicate native plan construction has no demonstrated effect on selection or spending.
- Requested correction: Avoid the second automatic check within the same drive. Preserve fresh checks at proposal, queue-promotion and wrapper callbacks. If simplifying the explicit branch, point the compiled stationary-strike check at its reachable automatic call site.
- Requested evidence: If addressed, compare PlannedStrikeProbeCount across one held, cooling drive and retain the same target with zero movement or spending.

#### Prior finding assessments

- **R11-F1** — `fix_verified` — The owning wrapper now reads the AI-off rule before its first delivery. The gate recorded a started windup, zero deliveries, zero spending and one retirement. The AI-on automatic and AI-off explicit controls each delivered once. R12-F1 concerns the later, already-delivered state.
- **R11-F2** — `fix_verified` — The production proposal path checks stationary strike admission whenever pair movement is forbidden, including a horse Hold. With that restriction supplied, the gate showed A replacing out-of-reach B and in-reach B retaining its order. R12-F2 identifies the separate defect in how the new fixture supplies and measures the case.
- **R11-F3** — `fix_verified` — After target invalidation is checked, either missing group memory or absence of the target now retires the automatic order. Both missing-state cases finished with zero moves and spending. The perceived-target and explicit-order controls each submitted a move.
- **R11-F4** — `fix_verified` — Wrapper-first target invalidation now reaches the owning order’s retirement decision. All six range and sight rows recorded 'automatic target invalid' once, the unconscious-target terminal reason, exactly one TargetInvalid rejection, and zero movement or spending.

#### Evidence assessment

- **Inspected:** The enrolled mission, repository validation policy and handoffs, full feature diff, developer report, prior findings ledger, transcript notice, and controller gate receipt and log.; The correction from round-11 candidate 55869192 through 29816172, including automatic proposal admission, controller drive, queued promotion, wrapper approach and delivery, terminal settlement, and the affected harness and compiled checks.
- **Executed verification:** Read-only Git checks confirmed the candidate and merge base. The mission SHA-256 and focused-log SHA-256 matched their supplied identities. The six whole-file extraction paths matched 46444b30 at extraction commit 95c26e59.; The controller receipt records the required focused Fast gate at 29816172, exit 0 in 44.4 seconds with the worktree unchanged. Components passed 1542/1542, Validate-Source 153/153, lifecycle contracts 82/82, and the attack-path harness 200/200. All 15 focused children passed.; Gate log lines 1983–2019 record the six invalidation orderings, the AI-off windup and its controls, the two stationary-target results, and the missing-group and missing-memory cases. Line 2626 confirms byte identity between the supplied Mounted Shield icon and the resource embedded in the built output.
- **Source inspection only:** The post-delivery repeat follows from the StrikeReleased guard, the new unconditional return for automatic orders, and successful terminal settlement retaining the generation. The candidate gate tests AI-off before delivery.; The active-approach diagnostic discrepancy and the duplicate stationary strike probe were traced through production call order. Frame-time cost was unmeasured.
- **Required gaps:** A production-drive regression showing retirement of future dispatch after AI-off is observed during a delivered routine, while that routine completes with its original attack count and spending.; A mount-only Hold replay using the horse’s HoldState, with a measured zero spending delta for the retired target before readiness returns.
- **Limitations:** The foreground developer transcript was withheld. The controller receipt and its hash-verified log establish the candidate gate. The additional local runs described in the report were not independently confirmed.; The attack-path harness executes production members against a simulated engine. Native gameplay and timing remain owner observations. No build, launch, installation or save operation was performed in this review.

#### Summary

- **Delivered behavior:** Offline checks cover legal horse follow-through, melee target selection, approach before strike readiness, pre-delivery cancellation, and Mounted Shield regressions including embedded icon identity. Automatic-order retirement after first delivery still needs correction.
- **Scope reviewed:** Final PR-1 source and offline acceptance for MMR-03, MMR-04 and MMR-05, including S04, S05, S08 and the round-12 correction’s affected consumers.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** N04. Confirm horse-attributed attacks and routine cardinality in both initiative arrangements, including the spent-horse control.; N05. Observe real-time acquisition and approach with two eligible enemies, a selected target, AI-off and Stop/Hold.; N06. Compare first movement and first strike with an equivalent unmounted actor under matching cooldown and preparation.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a21a20-1d99-7c42-99e5-5f6e4be6e01e`, duration 685.3s, response sha256 `0ac721708416ba60ddaec828d186a6d9ad99ebe83c9d1966e55602df3035844c`, request `kmc-codex-mmr-attack-orders-20261125103400-6cef-r12-1580b1`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "29816172ad6f416f3590e5a8532ceea8edc66123",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The four round-11 production issues are corrected, and the focused gate passed. Two issues block readiness. Switching AI off after the first strike lands can leave the repeating order alive. The new mount-only Hold test also supplies the movement restriction directly and omits the requested spending observation.\n\nContinuation for Claude\n\nWhen the controller observes AI-off, retire the repeating automatic order while allowing its already-delivered native routine to finish. Exercise the production drive between the first and final attacks, then verify that the old order cannot dispatch again after AI is re-enabled. For the Hold case, set the horse\u2019s HoldState, derive the restriction through production code, and measure spending before restoring readiness. Preserve explicit orders, turn-based authority, native attack cardinality and the existing pre-delivery controls. Run the prescribed focused Fast gate on the corrected candidate within the existing limits. R12-F3 and R12-F4 are optional.",
  "evidence_assessment": {
    "executed_verification": [
      "Read-only Git checks confirmed the candidate and merge base. The mission SHA-256 and focused-log SHA-256 matched their supplied identities. The six whole-file extraction paths matched 46444b30 at extraction commit 95c26e59.",
      "The controller receipt records the required focused Fast gate at 29816172, exit 0 in 44.4 seconds with the worktree unchanged. Components passed 1542/1542, Validate-Source 153/153, lifecycle contracts 82/82, and the attack-path harness 200/200. All 15 focused children passed.",
      "Gate log lines 1983\u20132019 record the six invalidation orderings, the AI-off windup and its controls, the two stationary-target results, and the missing-group and missing-memory cases. Line 2626 confirms byte identity between the supplied Mounted Shield icon and the resource embedded in the built output."
    ],
    "inspected": [
      "The enrolled mission, repository validation policy and handoffs, full feature diff, developer report, prior findings ledger, transcript notice, and controller gate receipt and log.",
      "The correction from round-11 candidate 55869192 through 29816172, including automatic proposal admission, controller drive, queued promotion, wrapper approach and delivery, terminal settlement, and the affected harness and compiled checks."
    ],
    "limitations": [
      "The foreground developer transcript was withheld. The controller receipt and its hash-verified log establish the candidate gate. The additional local runs described in the report were not independently confirmed.",
      "The attack-path harness executes production members against a simulated engine. Native gameplay and timing remain owner observations. No build, launch, installation or save operation was performed in this review."
    ],
    "required_gaps": [
      "A production-drive regression showing retirement of future dispatch after AI-off is observed during a delivered routine, while that routine completes with its original attack count and spending.",
      "A mount-only Hold replay using the horse\u2019s HoldState, with a measured zero spending delta for the retired target before readiness returns."
    ],
    "source_inspection_only": [
      "The post-delivery repeat follows from the StrikeReleased guard, the new unconditional return for automatic orders, and successful terminal settlement retaining the generation. The candidate gate tests AI-off before delivery.",
      "The active-approach diagnostic discrepancy and the duplicate stationary strike probe were traced through production call order. Frame-time cost was unmeasured."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "MMR-04",
        "S05"
      ],
      "actual": "TryRetireStockIntent now returns directly from RetireAutomaticOrderIfNotAdmitted for every automatic order. That method returns false at line 1644 when StrikeReleased is true, before reading AI state. If AI is re-enabled before the routine finishes, successful terminal settlement keeps the old generation and the next drive can dispatch the horse against the old target without a fresh brain proposal.",
      "blocking_rationale": "An observed AI-off interval can be lost, allowing a stale target to consume the partner\u2019s next legal attack.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The AI-off update retires future dispatch for that generation. The native routine already in progress completes its remaining attacks with its original action cost.",
      "id": "R12-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedCombatController.cs",
        "line": 1891
      },
      "priority": "P2",
      "requested_correction": "Apply the order\u2019s AI-off retirement rule on the controller path after delivery. Release the generation and preserve the live wrapper through the existing native-completion path.",
      "requested_evidence": "Deliver the first act of an automatic two-attack routine, switch AI off, and drive once. Assert that the intent is cleared and the wrapper is still running. Re-enable AI before the remaining act, observe normal completion with one Standard spent, then drive with an eligible horse. Expect zero new dispatches until a fresh proposal.",
      "title": "AI-off after first delivery can leave the repeating order alive",
      "trigger": "The rider lands the first attack of a real-time automatic routine. Its target remains valid, and the controller observes AI-off while the wrapper still has a native attack to finish."
    },
    {
      "acceptance_ids": [
        "MMR-04",
        "S05"
      ],
      "actual": "Round12MountOnlyHold sets controller.MovementForbidden directly. The harness adapter returns that flag and the horse\u2019s HoldState is never set. The case captures StandardSpends at line 3289 but neither compares nor prints its delta before enabling both Standards. The log establishes target selection and zero travel under an injected restriction.",
      "blocking_rationale": "The requested mount-only trigger and retired-target spending observation remain unverified in this replay. Losing the horse\u2019s Hold input in the controller adapter would leave both rows green.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "Only the horse is held, the production movement predicate reads that state, and the fixture measures B\u2019s spending before either actor becomes ready to strike A.",
      "id": "R12-F2",
      "location": {
        "file": "tests/Chunk7Diagnostics/AttackPathHarnessTemplate.cs",
        "line": 3281
      },
      "priority": "P2",
      "requested_correction": "Drive this case from the simulated horse\u2019s HoldState through the production AutomaticMovementForbidden method. Keep the rider mobile and both targets unmarked. Assert and print the pre-readiness spending delta.",
      "requested_evidence": "With B outside stationary reach, record acceptance of A on that proposal, zero B movement and spending, and one stationary delivery on A after readiness returns. With B still in reach, record retention and one stationary delivery on B.",
      "title": "The mount-only Hold replay bypasses the horse\u2019s Hold input",
      "trigger": "The focused harness reports that the horse\u2019s Hold caused B to retire before A\u2019s proposal was decided."
    },
    {
      "acceptance_ids": [
        "MMR-04",
        "S05"
      ],
      "actual": "TickApproach calls EndIfMovementForbiddenAtApproach before the shared automatic decision on its out-of-range path. The wrapper cancels itself and terminal settlement clears the intent. A controller-first observation records 'hold out of reach' and increments the automatic-retirement counter. A wrapper-first observation bypasses both diagnostic writes.",
      "blocking_rationale": "Both paths stop the approach. The discrepancy affects the reason and count reported for the same stop.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "Either observer records the automatic retirement once with the same reason.",
      "id": "R12-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedPairAttackCommand.cs",
        "line": 1572
      },
      "priority": "P3",
      "requested_correction": "Let the owning automatic order decide this movement restriction before the generic approach cancellation.",
      "requested_evidence": "If addressed, compare controller-first and wrapper-first ticks with a live carrier. Check the retirement reason and count, exact carrier cleanup, and zero delivery.",
      "title": "A started approach can bypass automatic-retirement diagnostics",
      "trigger": "During an automatic rider approach, the conscious horse loses CanMove while its perceived target remains outside strike reach. The wrapper ticks before the controller."
    },
    {
      "acceptance_ids": [
        "MMR-04",
        "S05"
      ],
      "actual": "TryDriveStockAttackIntent checks automatic admission at line 2016 and calls it again through TryRetireStockIntent at line 2038. Each check now constructs a planned stationary strike when the held melee order has no wrapper.",
      "blocking_rationale": "The duplicate native plan construction has no demonstrated effect on selection or spending.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "One controller drive needs one automatic-retirement decision for the same waiting order.",
      "id": "R12-F4",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedCombatController.cs",
        "line": 2038
      },
      "priority": "P3",
      "requested_correction": "Avoid the second automatic check within the same drive. Preserve fresh checks at proposal, queue-promotion and wrapper callbacks. If simplifying the explicit branch, point the compiled stationary-strike check at its reachable automatic call site.",
      "requested_evidence": "If addressed, compare PlannedStrikeProbeCount across one held, cooling drive and retain the same target with zero movement or spending.",
      "title": "A waiting stationary order builds the same strike probe twice per drive",
      "trigger": "Both Standards are cooling and a held pair retains an in-range melee target without a live wrapper."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R11-F1",
      "rationale": "The owning wrapper now reads the AI-off rule before its first delivery. The gate recorded a started windup, zero deliveries, zero spending and one retirement. The AI-on automatic and AI-off explicit controls each delivered once. R12-F1 concerns the later, already-delivered state."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R11-F2",
      "rationale": "The production proposal path checks stationary strike admission whenever pair movement is forbidden, including a horse Hold. With that restriction supplied, the gate showed A replacing out-of-reach B and in-reach B retaining its order. R12-F2 identifies the separate defect in how the new fixture supplies and measures the case."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R11-F3",
      "rationale": "After target invalidation is checked, either missing group memory or absence of the target now retires the automatic order. Both missing-state cases finished with zero moves and spending. The perceived-target and explicit-order controls each submitted a move."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R11-F4",
      "rationale": "Wrapper-first target invalidation now reaches the owning order\u2019s retirement decision. All six range and sight rows recorded 'automatic target invalid' once, the unconscious-target terminal reason, exactly one TargetInvalid rejection, and zero movement or spending."
    }
  ],
  "request_id": "kmc-codex-mmr-attack-orders-20261125103400-6cef-r12-1580b1",
  "round": 12,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "Offline checks cover legal horse follow-through, melee target selection, approach before strike readiness, pre-delivery cancellation, and Mounted Shield regressions including embedded icon identity. Automatic-order retirement after first delivery still needs correction.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "N04. Confirm horse-attributed attacks and routine cardinality in both initiative arrangements, including the spent-horse control.",
      "N05. Observe real-time acquisition and approach with two eligible enemies, a selected target, AI-off and Stop/Hold.",
      "N06. Compare first movement and first strike with an equivalent unmounted actor under matching cooldown and preparation."
    ],
    "scope_reviewed": "Final PR-1 source and offline acceptance for MMR-03, MMR-04 and MMR-05, including S04, S05, S08 and the round-12 correction\u2019s affected consumers.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
