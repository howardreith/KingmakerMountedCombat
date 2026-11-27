<!-- agent-review:v1 enrollment=kmc-codex-mmr-attack-orders-20261125103400-6cef key=r14-review part=1/1 sha256=b9aa45424af51648458ec16ec96ade8ee5bc4e878756d529c9bae24a5187e8e1 -->
### Codex GPT-6.2 Astra — review — round 14 — candidate `0ecf38f717`

**Decision:** `changes_required` — The candidate-bound Fast gate verifies both combat corrections. The remaining blocker is evidence of the program-wide review allocation check required before round 14.

## Continuation for Claude

Supply the dated supplemental-ledger entry or captured readout that ties the program totals to this submission. Entry 104 establishes the PR-1 count. Entry 99 gives an earlier program count, and entry 100 records the new ceilings. If the pre-submission check was missed, append a dated reconciliation that identifies the omission and distinguishes it from the check for the next authorized submission. Keep the correction scoped to the records and preserve the reviewed product, gate evidence, and ledger history.

#### Findings

**R14-F1** · P2 · evidence_gap · blocks readiness — Round 14’s pre-review record omits the program-wide usage check
- Location: `docs/missions/mmr-split/PR-1-ledger-excerpt.md:223`; acceptance: Mission §7, Mission §8
- Trigger: PR-1 submits round 14 after the program ceilings changed while a previously executed PR-5 verdict awaited settlement.
- Expected: A dated record ties both the child allocation and the program-wide usage against the applicable ceilings to the pre-review check.
- Actual: Entry 104 records 13 of 15 PR-1 rounds used and the validation-minute balance. The last supplied usage total is entry 99, with 37 lifetime and 30 child reviews under the previous ceilings. Entry 100 changes the ceilings to 49 and 42. The excerpt skips entry 102, and the supplied records do not establish which program totals were checked for round 14.
- Why it blocks / does not: The child authorization and focused gate are evidenced. The separate, mandatory pre-submission program check remains unverified.
- Requested correction: Supply the existing record of the check. If it was missed, document that fact and append a reconciliation from the intervening ledger entries, preserving their original dates and contents.
- Requested evidence: A scoped ledger excerpt or captured readout showing the child and lifetime program totals, applicable ceilings, and timing relative to submission. Account for the already-executed PR-5 round 7 once.

**R14-F2** · P3 · optional_polish · non-blocking — The older released-strike PASS label overstates its observation window
- Location: `tests/Chunk7Diagnostics/AttackPathHarnessTemplate.cs:3142`; acceptance: MMR-04, S05
- Trigger: A reader uses the older control’s PASS line to determine whether the retiring controller drive changed the wrapper’s cause.
- Expected: The older control’s result label describes the wrapper-first observation it actually samples.
- Actual: Round11InstalledWrapper reads EndCause at line 3124, drives at line 3126, and uses the earlier value in a PASS message that also describes controller retirement.
- Why it blocks / does not: Round14ReleasedStrikeRangeLoss supplies and asserts the missing post-drive observation. Only the older result label remains misleading.
- Requested correction: Label the older cause sample as pre-drive and leave the post-retirement claim with the new control.
- Requested evidence: If the label is updated, its wording should match the sampling order. The existing round-14 assertions provide the behavioral evidence.

#### Prior finding assessments

- **R13-F1** — `fix_verified` — An exhausted, released wrapper now uses the retained target’s stationary strike probe. During the unfinished success animation, generation 1 kept the same target with zero retirements, moves, or displacement. The horse then struck hostile-1 without a fresh proposal, with one modeled Standard per actor. Moving the target out of reach produced one retirement and zero horse attacks.
- **R13-F2** — `fix_verified` — The new replay samples immediately after the retiring drive and before another engine tick. The wrapper still owned the rider’s Standard slot, had one attack remaining, and reported Unrecorded and Attacking. After B returned to reach, both acts had struck B, the transaction completed, spending stayed at one Standard, and redispatches stayed at zero.
- **R13-F3** — `deferral_accepted` — The duplicate checks remain on the continuing out-of-range paths. They repeat work without changing the observed admission or cleanup result. This remains optional.

#### Evidence assessment

- **Inspected:** Controller mission, prior findings, developer report, transcript notice, gate receipt and log, repository validation policy, and current handoff.; Full feature diff and round-13-to-round-14 changes, including the retained-order admission decision, attack wrapper, dispatch and retirement paths, harness construction, and supplemental ledger excerpt.
- **Executed verification:** Confirmed the candidate HEAD and merge base, matched the supplied 31-file feature diff to Git, verified the mission and gate-log SHA-256 values, and checked all 44 published ledger-entry hashes.; The controller ran Test-Tiers.ps1 -Tier Fast -Focused Test-MmrAttackOrdersFocused.ps1 at the candidate. It exited 0 in 54.8 seconds. Fast passed 4/4 stages, components passed 1542/1542, source validation passed 153/153, and all 15 focused children passed.; The attack-path harness passed 208/208 checks and the lifecycle contract passed 82/82. Gate lines 2028–2035 record the recovery-animation and released-strike observations. The Mounted Shield check also found identical packet, production, and embedded icon bytes.
- **Source inspection only:** Traced the exhausted-plan fallback and its surrounding invalidation, perception, native eligibility, and player-order checks.; Confirmed the repeated admission calls on continuing active and deferred approaches, and the sampling order in the older released-strike control.
- **Required gaps:** An attributable record of the program-wide review totals checked before round 14, as required by mission §7.
- **Limitations:** The harness models engine timing, command slots, and Standard expenditure. Native gameplay observations N04–N06 remain owner-pending.; The controller withheld the mixed foreground transcript. The reported run with the production fix withheld is documented by the developer but has no separate supplied execution receipt.

#### Summary

- **Delivered behavior:** Offline evidence shows a held pair retaining an eligible melee target through the rider’s final animation and the horse following through. When a restricted target leaves native action range, the repeating order retires while the delivered routine retains its original action cost and can finish.
- **Scope reviewed:** Final PR-1 checkpoint for MMR-03, MMR-04, and MMR-05, including S04, S05, the S08 regression gate, and required review accounting.
- **Pending later-phase work:** none
- **Unresolved questions:** Which contemporaneous operator record establishes the program-wide balance checked for the round-14 submission?
- **Remaining owner review:** N04 — Confirm horse-owned attacks, targets, weapons, and action costs in both initiative arrangements, including an already-used horse.; N05 — Observe automatic target choice and retention in a real encounter with AI-off, Stop, and Hold controls.; N06 — Compare mounted and unmounted movement onset under equivalent cooldown and preparation, and observe the first actual attack.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a21ac5-5daf-7dd3-bf1a-87354abc9bb0`, duration 691.1s, response sha256 `211780c3b9533bae2042798f2b317ece2a74d4613186b2b22e5785f806d5eedd`, request `kmc-codex-mmr-attack-orders-20261125103400-6cef-r14-25a6c0`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "0ecf38f717bca261eb5955654a32e1932acff114",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The candidate-bound Fast gate verifies both combat corrections. The remaining blocker is evidence of the program-wide review allocation check required before round 14.\n\n## Continuation for Claude\n\nSupply the dated supplemental-ledger entry or captured readout that ties the program totals to this submission. Entry 104 establishes the PR-1 count. Entry 99 gives an earlier program count, and entry 100 records the new ceilings. If the pre-submission check was missed, append a dated reconciliation that identifies the omission and distinguishes it from the check for the next authorized submission. Keep the correction scoped to the records and preserve the reviewed product, gate evidence, and ledger history.",
  "evidence_assessment": {
    "executed_verification": [
      "Confirmed the candidate HEAD and merge base, matched the supplied 31-file feature diff to Git, verified the mission and gate-log SHA-256 values, and checked all 44 published ledger-entry hashes.",
      "The controller ran Test-Tiers.ps1 -Tier Fast -Focused Test-MmrAttackOrdersFocused.ps1 at the candidate. It exited 0 in 54.8 seconds. Fast passed 4/4 stages, components passed 1542/1542, source validation passed 153/153, and all 15 focused children passed.",
      "The attack-path harness passed 208/208 checks and the lifecycle contract passed 82/82. Gate lines 2028\u20132035 record the recovery-animation and released-strike observations. The Mounted Shield check also found identical packet, production, and embedded icon bytes."
    ],
    "inspected": [
      "Controller mission, prior findings, developer report, transcript notice, gate receipt and log, repository validation policy, and current handoff.",
      "Full feature diff and round-13-to-round-14 changes, including the retained-order admission decision, attack wrapper, dispatch and retirement paths, harness construction, and supplemental ledger excerpt."
    ],
    "limitations": [
      "The harness models engine timing, command slots, and Standard expenditure. Native gameplay observations N04\u2013N06 remain owner-pending.",
      "The controller withheld the mixed foreground transcript. The reported run with the production fix withheld is documented by the developer but has no separate supplied execution receipt."
    ],
    "required_gaps": [
      "An attributable record of the program-wide review totals checked before round 14, as required by mission \u00a77."
    ],
    "source_inspection_only": [
      "Traced the exhausted-plan fallback and its surrounding invalidation, perception, native eligibility, and player-order checks.",
      "Confirmed the repeated admission calls on continuing active and deferred approaches, and the sampling order in the older released-strike control."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "Mission \u00a77",
        "Mission \u00a78"
      ],
      "actual": "Entry 104 records 13 of 15 PR-1 rounds used and the validation-minute balance. The last supplied usage total is entry 99, with 37 lifetime and 30 child reviews under the previous ceilings. Entry 100 changes the ceilings to 49 and 42. The excerpt skips entry 102, and the supplied records do not establish which program totals were checked for round 14.",
      "blocking_rationale": "The child authorization and focused gate are evidenced. The separate, mandatory pre-submission program check remains unverified.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "A dated record ties both the child allocation and the program-wide usage against the applicable ceilings to the pre-review check.",
      "id": "R14-F1",
      "location": {
        "file": "docs/missions/mmr-split/PR-1-ledger-excerpt.md",
        "line": 223
      },
      "priority": "P2",
      "requested_correction": "Supply the existing record of the check. If it was missed, document that fact and append a reconciliation from the intervening ledger entries, preserving their original dates and contents.",
      "requested_evidence": "A scoped ledger excerpt or captured readout showing the child and lifetime program totals, applicable ceilings, and timing relative to submission. Account for the already-executed PR-5 round 7 once.",
      "title": "Round 14\u2019s pre-review record omits the program-wide usage check",
      "trigger": "PR-1 submits round 14 after the program ceilings changed while a previously executed PR-5 verdict awaited settlement."
    },
    {
      "acceptance_ids": [
        "MMR-04",
        "S05"
      ],
      "actual": "Round11InstalledWrapper reads EndCause at line 3124, drives at line 3126, and uses the earlier value in a PASS message that also describes controller retirement.",
      "blocking_rationale": "Round14ReleasedStrikeRangeLoss supplies and asserts the missing post-drive observation. Only the older result label remains misleading.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "The older control\u2019s result label describes the wrapper-first observation it actually samples.",
      "id": "R14-F2",
      "location": {
        "file": "tests/Chunk7Diagnostics/AttackPathHarnessTemplate.cs",
        "line": 3142
      },
      "priority": "P3",
      "requested_correction": "Label the older cause sample as pre-drive and leave the post-retirement claim with the new control.",
      "requested_evidence": "If the label is updated, its wording should match the sampling order. The existing round-14 assertions provide the behavioral evidence.",
      "title": "The older released-strike PASS label overstates its observation window",
      "trigger": "A reader uses the older control\u2019s PASS line to determine whether the retiring controller drive changed the wrapper\u2019s cause."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R13-F1",
      "rationale": "An exhausted, released wrapper now uses the retained target\u2019s stationary strike probe. During the unfinished success animation, generation 1 kept the same target with zero retirements, moves, or displacement. The horse then struck hostile-1 without a fresh proposal, with one modeled Standard per actor. Moving the target out of reach produced one retirement and zero horse attacks."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R13-F2",
      "rationale": "The new replay samples immediately after the retiring drive and before another engine tick. The wrapper still owned the rider\u2019s Standard slot, had one attack remaining, and reported Unrecorded and Attacking. After B returned to reach, both acts had struck B, the transaction completed, spending stayed at one Standard, and redispatches stayed at zero."
    },
    {
      "assessment": "deferral_accepted",
      "finding_id": "R13-F3",
      "rationale": "The duplicate checks remain on the continuing out-of-range paths. They repeat work without changing the observed admission or cleanup result. This remains optional."
    }
  ],
  "request_id": "kmc-codex-mmr-attack-orders-20261125103400-6cef-r14-25a6c0",
  "round": 14,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "Offline evidence shows a held pair retaining an eligible melee target through the rider\u2019s final animation and the horse following through. When a restricted target leaves native action range, the repeating order retires while the delivered routine retains its original action cost and can finish.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "N04 \u2014 Confirm horse-owned attacks, targets, weapons, and action costs in both initiative arrangements, including an already-used horse.",
      "N05 \u2014 Observe automatic target choice and retention in a real encounter with AI-off, Stop, and Hold controls.",
      "N06 \u2014 Compare mounted and unmounted movement onset under equivalent cooldown and preparation, and observe the first actual attack."
    ],
    "scope_reviewed": "Final PR-1 checkpoint for MMR-03, MMR-04, and MMR-05, including S04, S05, the S08 regression gate, and required review accounting.",
    "unresolved_questions": [
      "Which contemporaneous operator record establishes the program-wide balance checked for the round-14 submission?"
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
