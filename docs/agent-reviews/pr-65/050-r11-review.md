<!-- agent-review:v1 enrollment=kmc-codex-mmr-attack-orders-20261125103400-6cef key=r11-review part=1/1 sha256=79432cb16c381679677b1008664505387b4ecadd0313b2006c278e801a67db81 -->
### Codex GPT-6.2 Astra — review — round 11 — candidate `55869192b4`

**Decision:** `changes_required` — All six pending findings are fixed, and the required focused gate passed at the candidate commit. Three MMR-04 defects still block readiness. An installed automatic attack can deliver after AI is switched off, a held mount can cause the pair to discard a legal replacement target, and missing rider-group memory now permits automatic work to continue.

## Continuation for Claude

Use the existing automatic-order retirement path to enforce AI-off before the first native attack, require positive perception of the retained target, and apply the pair’s stationary strike check before a retained order can veto a replacement proposal. Scope each decision to the owning generation and command. Preserve target-invalidation precedence, explicit player orders, legal unrestricted movement, successor commands and native completion after delivery.

Exercise the three event sequences in R11-F1 through R11-F3 through the production paths used by the attack harness. Record the selected target, movement submissions, first delivery and native spending alongside the positive controls. Run the mission’s metered focused gate on the corrected candidate. R11-F4 concerns diagnostic accuracy and is optional. The owner’s native playtest and PR-6 integration remain separate.

#### Findings

**R11-F1** · P2 · defect · blocks readiness — An installed automatic attack can deliver after the rider’s AI is switched off
- Location: `src/KingmakerMountedCombat/Integration/MountedPairAttackCommand.cs:1341`; acceptance: MMR-04, S05
- Trigger: An automatic melee wrapper is winding up against a legal target. The player switches the rider’s AI off, and the wrapper’s first delivery tick runs before the next controller drive.
- Expected: The undelivered automatic order retires for AI-off before its first native attack or expenditure.
- Actual: The windup calls EndIfAutomaticOrderNotAdmitted, whose controller decision checks target validity, perception and native target geometry. It never reads IsAIEnabled. A toggle supplies no competing command, so the wrapper can proceed to native OnAction. The next controller drive checks AI-off after the first attack has already been delivered.
- Why it blocks / does not: The ordering permits an irreversible attack and Standard expenditure after the player disables automatic combat.
- Requested correction: Apply the existing AI-off retirement rule when the owning automatic wrapper checks permission to proceed before delivery. Preserve explicit orders and native completion for a strike already delivered.
- Requested evidence: Start an automatic first windup, switch AI off without issuing another command, and let the delivery tick run before the controller drive. Assert retirement for AI-off, zero deliveries and zero spending. Retain AI-enabled and explicit-order controls.

**R11-F2** · P2 · defect · blocks readiness — A mount-only Hold can cause the pair to discard a legal replacement proposal
- Location: `src/KingmakerMountedCombat/Integration/MountedCombatController.cs:1460`; acceptance: MMR-04, S05
- Trigger: The horse is already held, the rider is mobile, and perceived target B is unmarked. While both Standards cool and no wrapper exists, B leaves stationary mounted reach. The brain proposes reachable A before the next drive.
- Expected: The pair releases B before it can veto A, accepts this proposal for A, and attacks from its current position when ready.
- Actual: The proposal-entry check tests restricted range and sight only when the rider’s movement is forbidden or B is marked unreachable. A mount-only Hold satisfies neither condition. FirstRefusal therefore rejects A because B still owns the intent. The next drive applies the pair-wide stationary check and retires B as ‘hold out of reach’. A now needs another proposal.
- Why it blocks / does not: The pair discards an eligible native proposal while retaining a target it already lacks permission to pursue.
- Requested correction: Before a retained automatic target can veto a proposal, apply the existing stationary mounted-strike check whenever pair movement is forbidden. Keep a retained target that still passes that check.
- Requested evidence: With only the mount held, move B outside stationary strike reach and propose in-reach A before the drive. Show that this proposal is accepted, B gets zero movement or spending, and A is struck without travel. Repeat with B still in reach and show B keeps its order.

**R11-F3** · P2 · defect · blocks readiness — Missing rider-group memory permits an automatic order to advance
- Location: `src/KingmakerMountedCombat/Integration/MountedCombatController.cs:1576`; acceptance: MMR-04, S05
- Trigger: An otherwise valid hostile was admitted while perceived. The rider group or its memory becomes unavailable before the installed wrapper’s next movement or first-delivery check.
- Expected: An unreleased automatic order can advance only while the rider group’s memory confirms the target is perceived.
- Actual: The new condition returns TargetUnperceived only when memory exists and excludes the target. Missing memory falls through to Admitted for an unrestricted target. The previous wrapper predicate required both a memory object and positive membership.
- Why it blocks / does not: The change removes an existing awareness guard and allows movement or attack delivery using a retained entity reference alone.
- Requested correction: Treat an unavailable group or memory as loss of perception for the unreleased automatic order. Keep the existing target-invalidation check first.
- Requested evidence: Remove the rider group or its memory after automatic admission and run the wrapper before the next drive. Assert retirement, zero movement submissions and zero spending. Keep a perceived-target control and preserve explicit-order behavior.

**R11-F4** · P3 · optional_polish · non-blocking — Wrapper-first invalidation leaves stale automatic-retirement diagnostics
- Location: `src/KingmakerMountedCombat/Integration/MountedPairAttackCommand.cs:523`; acceptance: MMR-04, S05
- Trigger: An automatic target becomes invalid and the installed wrapper observes the change before the controller or queue-promotion callback.
- Expected: Diagnostics identify the same automatic retirement whichever observer sees the invalidation first.
- Actual: The wrapper interrupts itself before reaching the shared admission decision. Terminal settlement clears the intent without updating LastAutomaticRetirement or the retirement counters. The range and sight fixture rows both print an empty retirement field for wrapper-first execution and ‘automatic target invalid’ for controller-first execution.
- Why it blocks / does not: The command ends correctly with one TargetInvalid rejection. The discrepancy affects reporting of that outcome.
- Requested correction: If addressed, record the retirement reason and count once for the owning automatic generation when the wrapper observes invalidation first.
- Requested evidence: Compare the retirement reason and count across the existing controller-first, wrapper-first and queue-promotion rows while preserving their exact terminal results.

#### Prior finding assessments

- **R10-F1** — `fix_verified` — The installed wrapper now queries its owning order before approach and first delivery. In the recorded wrapper-first case, marked-unreachable B left range and the wrapper retired once with zero move submissions, displacement or spending. The stale-wrapper, unrestricted-target, queue-promotion and already-released controls passed.
- **R10-F2** — `fix_verified` — Actual target invalidation precedes perception and native range or sight loss. All six combinations of range or sight loss with controller-first, wrapper-first or queue-promotion execution produced TargetInvalid, the unconscious-target reason and exactly one TargetInvalid rejection, with zero movement or spending.
- **R10-F3** — `fix_verified` — The order retains the proposing action by generation and reads its current range for the rider on each restricted check. The shortened-reach and unavailable-ranking cases both released B and allowed the same proposal for A to proceed.
- **R10-F4** — `fix_verified` — The reported marked-unreachable B now loses its veto as soon as it leaves native action range. The proposal-first fixture accepted and struck A. Its in-range control retained B. R11-F2 concerns the separate stationary strike restriction imposed by a held horse.
- **R10-F5** — `fix_verified` — The tie-break comment now describes dislike as a preference among admitted equal-ranked candidates. The recorded controls selected disliked B when A’s stationary strike was blocked and retained A when its strike was legal.
- **R9-F3** — `fix_verified` — The harness compares each complete terminal result using ordinal equality and checks for exactly one typed TargetInvalid rejection in the new cases. The recorded negative control rejects an appended CommandCancelled code.

#### Evidence assessment

- **Inspected:** The enrolled mission, repository validation policy and handoff instructions, feature diff, developer report, prior findings ledger, transcript notice and controller gate artifacts.; The retained-order controller, proposal and Hold policies, native ranking reader, mounted command lifecycle, queue-promotion hooks and attack-path harness. The review followed the round-11 changes through proposal admission, movement, first delivery and terminal reporting.
- **Executed verification:** Read-only Git inspection confirmed HEAD at 55869192b442567a6a921eb875c49b5aebb4289b. git diff --check from the supplied merge base completed successfully.; The controller receipt records scripts/Test-Tiers.ps1 -Tier Fast -Focused Test-MmrAttackOrdersFocused.ps1 at this commit. It exited 0 in 50.8 seconds with the worktree unchanged. The supplied log matched SHA-256 25d9b75c24b9633d08a4fa7c085e0d032ee193a1b32f14163256a36d1e22ffbc.; The recorded run passed all four Fast steps and all 15 focused children. It reported 1,542 component checks, 153 source checks, 80 lifecycle checks and 185 attack-path checks with zero failures. The Mounted Shield check confirmed identical packet, production and embedded icon bytes.
- **Source inspection only:** R11-F1 through R11-F3 follow from the order of calls and predicates in the candidate. The recorded gate exercises adjacent controls but does not execute those three sequences.; For R11-F4, the log shows the missing retirement reason. The explanation for the stale counters comes from tracing the wrapper’s terminal path.
- **Required gaps:** A corrected candidate needs focused observations for AI-off during first windup, a replacement proposal arriving while only the mount is held, and loss of the rider group or its memory while an automatic command is pending.
- **Limitations:** The developer’s foreground transcript was withheld because it mixed unrelated work. Execution claims here rely on the controller’s candidate-bound receipt and log.; This was a read-only review. The gate identifies its results as offline evidence with nativeProof=false. Native gameplay observations N04, N05 and N06 remain pending.

#### Summary

- **Delivered behavior:** The candidate carries ordinary melee intent into the current paired activation, separates real-time approach from strike readiness, and rechecks restricted automatic targets against the proposing action’s current range and sight. The focused gate supports the six submitted corrections and the Mounted Shield regression checks. Automatic-order eligibility still has the three blocking defects listed above.
- **Scope reviewed:** Final PR-1 source and offline checkpoint for MMR-03, MMR-04 and MMR-05, including S04, S05, S08, the round-11 corrections and their directly affected consumers.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** N04. Observe horse-attributed attacks and native routine cardinality in rider-first and horse-first arrangements, including an already-used or ended horse.; N05. Observe idle automatic acquisition against two eligible hostiles where practical, including AI-off, Stop, Hold and selected-target retention.; N06. Compare mounted and unmounted explicit attack orders under comparable cooldown and preparation conditions, from input through first movement and first attack.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a21a09-5cdc-7b83-adbf-c3496e4312a6`, duration 1031.1s, response sha256 `166cd082f7705e3aa414c242bdef4a9ff3b13f7e8b99533e677c7cc7d14a3db6`, request `kmc-codex-mmr-attack-orders-20261125103400-6cef-r11-72d4ef`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "55869192b442567a6a921eb875c49b5aebb4289b",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "All six pending findings are fixed, and the required focused gate passed at the candidate commit. Three MMR-04 defects still block readiness. An installed automatic attack can deliver after AI is switched off, a held mount can cause the pair to discard a legal replacement target, and missing rider-group memory now permits automatic work to continue.\n\n## Continuation for Claude\n\nUse the existing automatic-order retirement path to enforce AI-off before the first native attack, require positive perception of the retained target, and apply the pair\u2019s stationary strike check before a retained order can veto a replacement proposal. Scope each decision to the owning generation and command. Preserve target-invalidation precedence, explicit player orders, legal unrestricted movement, successor commands and native completion after delivery.\n\nExercise the three event sequences in R11-F1 through R11-F3 through the production paths used by the attack harness. Record the selected target, movement submissions, first delivery and native spending alongside the positive controls. Run the mission\u2019s metered focused gate on the corrected candidate. R11-F4 concerns diagnostic accuracy and is optional. The owner\u2019s native playtest and PR-6 integration remain separate.",
  "evidence_assessment": {
    "executed_verification": [
      "Read-only Git inspection confirmed HEAD at 55869192b442567a6a921eb875c49b5aebb4289b. git diff --check from the supplied merge base completed successfully.",
      "The controller receipt records scripts/Test-Tiers.ps1 -Tier Fast -Focused Test-MmrAttackOrdersFocused.ps1 at this commit. It exited 0 in 50.8 seconds with the worktree unchanged. The supplied log matched SHA-256 25d9b75c24b9633d08a4fa7c085e0d032ee193a1b32f14163256a36d1e22ffbc.",
      "The recorded run passed all four Fast steps and all 15 focused children. It reported 1,542 component checks, 153 source checks, 80 lifecycle checks and 185 attack-path checks with zero failures. The Mounted Shield check confirmed identical packet, production and embedded icon bytes."
    ],
    "inspected": [
      "The enrolled mission, repository validation policy and handoff instructions, feature diff, developer report, prior findings ledger, transcript notice and controller gate artifacts.",
      "The retained-order controller, proposal and Hold policies, native ranking reader, mounted command lifecycle, queue-promotion hooks and attack-path harness. The review followed the round-11 changes through proposal admission, movement, first delivery and terminal reporting."
    ],
    "limitations": [
      "The developer\u2019s foreground transcript was withheld because it mixed unrelated work. Execution claims here rely on the controller\u2019s candidate-bound receipt and log.",
      "This was a read-only review. The gate identifies its results as offline evidence with nativeProof=false. Native gameplay observations N04, N05 and N06 remain pending."
    ],
    "required_gaps": [
      "A corrected candidate needs focused observations for AI-off during first windup, a replacement proposal arriving while only the mount is held, and loss of the rider group or its memory while an automatic command is pending."
    ],
    "source_inspection_only": [
      "R11-F1 through R11-F3 follow from the order of calls and predicates in the candidate. The recorded gate exercises adjacent controls but does not execute those three sequences.",
      "For R11-F4, the log shows the missing retirement reason. The explanation for the stale counters comes from tracing the wrapper\u2019s terminal path."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "MMR-04",
        "S05"
      ],
      "actual": "The windup calls EndIfAutomaticOrderNotAdmitted, whose controller decision checks target validity, perception and native target geometry. It never reads IsAIEnabled. A toggle supplies no competing command, so the wrapper can proceed to native OnAction. The next controller drive checks AI-off after the first attack has already been delivered.",
      "blocking_rationale": "The ordering permits an irreversible attack and Standard expenditure after the player disables automatic combat.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The undelivered automatic order retires for AI-off before its first native attack or expenditure.",
      "id": "R11-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedPairAttackCommand.cs",
        "line": 1341
      },
      "priority": "P2",
      "requested_correction": "Apply the existing AI-off retirement rule when the owning automatic wrapper checks permission to proceed before delivery. Preserve explicit orders and native completion for a strike already delivered.",
      "requested_evidence": "Start an automatic first windup, switch AI off without issuing another command, and let the delivery tick run before the controller drive. Assert retirement for AI-off, zero deliveries and zero spending. Retain AI-enabled and explicit-order controls.",
      "title": "An installed automatic attack can deliver after the rider\u2019s AI is switched off",
      "trigger": "An automatic melee wrapper is winding up against a legal target. The player switches the rider\u2019s AI off, and the wrapper\u2019s first delivery tick runs before the next controller drive."
    },
    {
      "acceptance_ids": [
        "MMR-04",
        "S05"
      ],
      "actual": "The proposal-entry check tests restricted range and sight only when the rider\u2019s movement is forbidden or B is marked unreachable. A mount-only Hold satisfies neither condition. FirstRefusal therefore rejects A because B still owns the intent. The next drive applies the pair-wide stationary check and retires B as \u2018hold out of reach\u2019. A now needs another proposal.",
      "blocking_rationale": "The pair discards an eligible native proposal while retaining a target it already lacks permission to pursue.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The pair releases B before it can veto A, accepts this proposal for A, and attacks from its current position when ready.",
      "id": "R11-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedCombatController.cs",
        "line": 1460
      },
      "priority": "P2",
      "requested_correction": "Before a retained automatic target can veto a proposal, apply the existing stationary mounted-strike check whenever pair movement is forbidden. Keep a retained target that still passes that check.",
      "requested_evidence": "With only the mount held, move B outside stationary strike reach and propose in-reach A before the drive. Show that this proposal is accepted, B gets zero movement or spending, and A is struck without travel. Repeat with B still in reach and show B keeps its order.",
      "title": "A mount-only Hold can cause the pair to discard a legal replacement proposal",
      "trigger": "The horse is already held, the rider is mobile, and perceived target B is unmarked. While both Standards cool and no wrapper exists, B leaves stationary mounted reach. The brain proposes reachable A before the next drive."
    },
    {
      "acceptance_ids": [
        "MMR-04",
        "S05"
      ],
      "actual": "The new condition returns TargetUnperceived only when memory exists and excludes the target. Missing memory falls through to Admitted for an unrestricted target. The previous wrapper predicate required both a memory object and positive membership.",
      "blocking_rationale": "The change removes an existing awareness guard and allows movement or attack delivery using a retained entity reference alone.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "An unreleased automatic order can advance only while the rider group\u2019s memory confirms the target is perceived.",
      "id": "R11-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedCombatController.cs",
        "line": 1576
      },
      "priority": "P2",
      "requested_correction": "Treat an unavailable group or memory as loss of perception for the unreleased automatic order. Keep the existing target-invalidation check first.",
      "requested_evidence": "Remove the rider group or its memory after automatic admission and run the wrapper before the next drive. Assert retirement, zero movement submissions and zero spending. Keep a perceived-target control and preserve explicit-order behavior.",
      "title": "Missing rider-group memory permits an automatic order to advance",
      "trigger": "An otherwise valid hostile was admitted while perceived. The rider group or its memory becomes unavailable before the installed wrapper\u2019s next movement or first-delivery check."
    },
    {
      "acceptance_ids": [
        "MMR-04",
        "S05"
      ],
      "actual": "The wrapper interrupts itself before reaching the shared admission decision. Terminal settlement clears the intent without updating LastAutomaticRetirement or the retirement counters. The range and sight fixture rows both print an empty retirement field for wrapper-first execution and \u2018automatic target invalid\u2019 for controller-first execution.",
      "blocking_rationale": "The command ends correctly with one TargetInvalid rejection. The discrepancy affects reporting of that outcome.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "Diagnostics identify the same automatic retirement whichever observer sees the invalidation first.",
      "id": "R11-F4",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedPairAttackCommand.cs",
        "line": 523
      },
      "priority": "P3",
      "requested_correction": "If addressed, record the retirement reason and count once for the owning automatic generation when the wrapper observes invalidation first.",
      "requested_evidence": "Compare the retirement reason and count across the existing controller-first, wrapper-first and queue-promotion rows while preserving their exact terminal results.",
      "title": "Wrapper-first invalidation leaves stale automatic-retirement diagnostics",
      "trigger": "An automatic target becomes invalid and the installed wrapper observes the change before the controller or queue-promotion callback."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R10-F1",
      "rationale": "The installed wrapper now queries its owning order before approach and first delivery. In the recorded wrapper-first case, marked-unreachable B left range and the wrapper retired once with zero move submissions, displacement or spending. The stale-wrapper, unrestricted-target, queue-promotion and already-released controls passed."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R10-F2",
      "rationale": "Actual target invalidation precedes perception and native range or sight loss. All six combinations of range or sight loss with controller-first, wrapper-first or queue-promotion execution produced TargetInvalid, the unconscious-target reason and exactly one TargetInvalid rejection, with zero movement or spending."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R10-F3",
      "rationale": "The order retains the proposing action by generation and reads its current range for the rider on each restricted check. The shortened-reach and unavailable-ranking cases both released B and allowed the same proposal for A to proceed."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R10-F4",
      "rationale": "The reported marked-unreachable B now loses its veto as soon as it leaves native action range. The proposal-first fixture accepted and struck A. Its in-range control retained B. R11-F2 concerns the separate stationary strike restriction imposed by a held horse."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R10-F5",
      "rationale": "The tie-break comment now describes dislike as a preference among admitted equal-ranked candidates. The recorded controls selected disliked B when A\u2019s stationary strike was blocked and retained A when its strike was legal."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R9-F3",
      "rationale": "The harness compares each complete terminal result using ordinal equality and checks for exactly one typed TargetInvalid rejection in the new cases. The recorded negative control rejects an appended CommandCancelled code."
    }
  ],
  "request_id": "kmc-codex-mmr-attack-orders-20261125103400-6cef-r11-72d4ef",
  "round": 11,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The candidate carries ordinary melee intent into the current paired activation, separates real-time approach from strike readiness, and rechecks restricted automatic targets against the proposing action\u2019s current range and sight. The focused gate supports the six submitted corrections and the Mounted Shield regression checks. Automatic-order eligibility still has the three blocking defects listed above.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "N04. Observe horse-attributed attacks and native routine cardinality in rider-first and horse-first arrangements, including an already-used or ended horse.",
      "N05. Observe idle automatic acquisition against two eligible hostiles where practical, including AI-off, Stop, Hold and selected-target retention.",
      "N06. Compare mounted and unmounted explicit attack orders under comparable cooldown and preparation conditions, from input through first movement and first attack."
    ],
    "scope_reviewed": "Final PR-1 source and offline checkpoint for MMR-03, MMR-04 and MMR-05, including S04, S05, S08, the round-11 corrections and their directly affected consumers.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
