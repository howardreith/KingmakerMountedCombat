<!-- agent-review:v1 enrollment=kmc-codex-mmr-attack-orders-20261125103400-6cef key=r15-review part=1/1 sha256=49c2dc8b977d3b35437f3ed69395ae1bfe7f1279b7337b7918d182e2773ab58f -->
### Codex GPT-6.2 Astra — review — round 15 — candidate `aa15e1f227`

**Decision:** `ready_for_owner_review` — PR-1 meets the source and offline acceptance criteria for S04, S05 and S08. Both round-14 findings are verified, and the controller recorded a passing focused gate on the submitted commit. Native gameplay confirmation remains with the owner.

#### Findings

None.

#### Prior finding assessments

- **R14-F1** — `fix_verified` — Entry 104 preserves the original omission. Entry 106 reconciles round-14 submission at 29 recorded child rounds plus the one executed, unsettled PR-5 review, giving 30 child and 37 lifetime executions. Entry 107 predates the candidate gate and records 14 of 15 PR-1 rounds used, 31 of 42 child and 38 of 49 lifetime executions before round 15, and projected totals of 32 and 39 afterward.
- **R14-F2** — `fix_verified` — Round11InstalledWrapper now labels EndCause as sampled after the wrapper tick and before the retiring drive. Round14ReleasedStrikeRangeLoss independently observed the wrapper immediately after that drive with cause Unrecorded, transaction Attacking, its slot retained and one attack remaining. The routine then completed with two total acts on B, one modeled Standard spent and zero redispatches.

#### Evidence assessment

- **Inspected:** The enrolled mission, repository validation policy, developer report, prior findings ledger and candidate-bound gate evidence.; The round-15 changes to PR-1-ledger-excerpt.md and AttackPathHarnessTemplate.cs, the recorded review ceilings, and the production dispatch, automatic admission and pre-start approach paths exercised by the harness.
- **Executed verification:** Matched the supplied 9,990-line feature diff to Git M..H across all 31 files. The production source tree matches round 14 exactly.; Recomputed all 46 ledger-entry SHA-256 digests, checked the links between consecutively excerpted entries and confirmed that all 44 earlier entries match the round-14 snapshot.; Verified the controller receipt against HEAD and the gate log's SHA-256. The focused Fast gate exited 0 in 46.9 seconds. All four stages and all 15 focused children passed, including 1,542 component checks, 153 source checks, 82 lifecycle checks and 208 attack-path checks. The Mounted Shield check matched the packet, production and embedded icon bytes.
- **Source inspection only:** Production source and assembly-contract checks establish where native attack planning, start and spending occur. The attack-path harness exercises production methods with a simulated engine. In-game timing and damage attribution remain unmeasured.
- **Required gaps:** none
- **Limitations:** The review used the supplied operator-ledger excerpt. The external ledger and sibling controller states were outside the snapshot.; The controller withheld the mixed-session transcript. Its receipt and log establish the required focused run. The separately reported direct harness run has no independent receipt in the supplied evidence.; R13-F3 remains an accepted optional deferral for duplicate admission checks during an out-of-range approach.

#### Summary

- **Delivered behavior:** An ordinary turn-based melee order retains the target for an eligible horse during the current paired activation. Real-time melee preserves explicit orders, uses distance to choose among equally ranked eligible native targets, and permits legal approach while the strike awaits native readiness.
- **Scope reviewed:** PR-1 covers MMR-03, MMR-04 and MMR-05, including the Mounted Shield regression gate. Round 15 changed the ledger excerpt and one harness message. Integrated packaging and delivery belong to PR-6.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** N04. Confirm horse-owned attacks by actor, target and weapon in both initiative arrangements. Check that a spent or ended horse gets no repeat attack.; N05. Observe real-time engagement with two eligible hostiles and verify AI-off, Stop, Hold and explicit-target behavior.; N06. Compare order acceptance, carrier creation, first movement, strike readiness and first attack against an equivalent unmounted control.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a21ad2-61b5-7841-8a20-87ef85c2e2ea`, duration 347.9s, response sha256 `0d417b6baa9e01d94d72a00839cf1ef11793b0c8bb1e8a6ee72d86121d05ae8d`, request `kmc-codex-mmr-attack-orders-20261125103400-6cef-r15-04c464`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "aa15e1f2277364ed9dc2c2c322462fa4a9166322",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "ready_for_owner_review",
  "decision_rationale": "PR-1 meets the source and offline acceptance criteria for S04, S05 and S08. Both round-14 findings are verified, and the controller recorded a passing focused gate on the submitted commit. Native gameplay confirmation remains with the owner.",
  "evidence_assessment": {
    "executed_verification": [
      "Matched the supplied 9,990-line feature diff to Git M..H across all 31 files. The production source tree matches round 14 exactly.",
      "Recomputed all 46 ledger-entry SHA-256 digests, checked the links between consecutively excerpted entries and confirmed that all 44 earlier entries match the round-14 snapshot.",
      "Verified the controller receipt against HEAD and the gate log's SHA-256. The focused Fast gate exited 0 in 46.9 seconds. All four stages and all 15 focused children passed, including 1,542 component checks, 153 source checks, 82 lifecycle checks and 208 attack-path checks. The Mounted Shield check matched the packet, production and embedded icon bytes."
    ],
    "inspected": [
      "The enrolled mission, repository validation policy, developer report, prior findings ledger and candidate-bound gate evidence.",
      "The round-15 changes to PR-1-ledger-excerpt.md and AttackPathHarnessTemplate.cs, the recorded review ceilings, and the production dispatch, automatic admission and pre-start approach paths exercised by the harness."
    ],
    "limitations": [
      "The review used the supplied operator-ledger excerpt. The external ledger and sibling controller states were outside the snapshot.",
      "The controller withheld the mixed-session transcript. Its receipt and log establish the required focused run. The separately reported direct harness run has no independent receipt in the supplied evidence.",
      "R13-F3 remains an accepted optional deferral for duplicate admission checks during an out-of-range approach."
    ],
    "required_gaps": [],
    "source_inspection_only": [
      "Production source and assembly-contract checks establish where native attack planning, start and spending occur. The attack-path harness exercises production methods with a simulated engine. In-game timing and damage attribution remain unmeasured."
    ]
  },
  "findings": [],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R14-F1",
      "rationale": "Entry 104 preserves the original omission. Entry 106 reconciles round-14 submission at 29 recorded child rounds plus the one executed, unsettled PR-5 review, giving 30 child and 37 lifetime executions. Entry 107 predates the candidate gate and records 14 of 15 PR-1 rounds used, 31 of 42 child and 38 of 49 lifetime executions before round 15, and projected totals of 32 and 39 afterward."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R14-F2",
      "rationale": "Round11InstalledWrapper now labels EndCause as sampled after the wrapper tick and before the retiring drive. Round14ReleasedStrikeRangeLoss independently observed the wrapper immediately after that drive with cause Unrecorded, transaction Attacking, its slot retained and one attack remaining. The routine then completed with two total acts on B, one modeled Standard spent and zero redispatches."
    }
  ],
  "request_id": "kmc-codex-mmr-attack-orders-20261125103400-6cef-r15-04c464",
  "round": 15,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "An ordinary turn-based melee order retains the target for an eligible horse during the current paired activation. Real-time melee preserves explicit orders, uses distance to choose among equally ranked eligible native targets, and permits legal approach while the strike awaits native readiness.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "N04. Confirm horse-owned attacks by actor, target and weapon in both initiative arrangements. Check that a spent or ended horse gets no repeat attack.",
      "N05. Observe real-time engagement with two eligible hostiles and verify AI-off, Stop, Hold and explicit-target behavior.",
      "N06. Compare order acceptance, carrier creation, first movement, strike readiness and first attack against an equivalent unmounted control."
    ],
    "scope_reviewed": "PR-1 covers MMR-03, MMR-04 and MMR-05, including the Mounted Shield regression gate. Round 15 changed the ledger excerpt and one harness message. Integrated packaging and delivery belong to PR-6.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
