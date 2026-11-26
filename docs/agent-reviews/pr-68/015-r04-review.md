<!-- agent-review:v1 enrollment=kmc-codex-mmr-horse-seat-20261126011011-d3c5 key=r04-review part=1/1 sha256=c9fe492f3299b24d2545f2566ea6ad6f25aa1838b359f4a22094239abedff8b0 -->
### Codex GPT-6.2 Astra — review — round 4 — candidate `8bfe701d44`

**Decision:** `ready_for_owner_review` — R1-F1 is verified. The Kingmaker 2.1.7b trace establishes which idle animation the adapter reads and how its weight reaches the mixer. The candidate’s focused gate shows recovery from a prolonged high bind pose to the remount height. S02 and S08 meet the required offline acceptance criteria.

#### Findings

**R4-F1** · P3 · optional_polish · non-blocking — The trace overstates when idle gains weight and when the seat can settle
- Location: `docs/missions/mmr-split/evidence/PR-3-idle-weight-trace.md:82`; acceptance: S02
- Trigger: A stationary clip registers while another action has full weight, or the rider is already stable at attachment.
- Expected: The trace should describe when the blend calculation yields positive weight and make clear that the minimum elapsed time and stable window can overlap.
- Actual: Line 82 says the first transition update makes the idle weight positive, although the documented formula can receive w = 0. Line 96 describes the half-second stability window as starting after two seconds. The gate records a continuously stable seat settling at 2.017 seconds.
- Why it blocks / does not: The adapter reads the actual weight on each frame. The operand trace and attachment results establish the required behavior independently of these timing statements.
- Requested correction: Qualify the first-update claim by the resulting blend weight. Describe settlement as requiring both time thresholds to hold on the same frame.
- Requested evidence: The existing UpdateAnimations and UpdateInternal trace and the frame-121 gate output suffice for this wording correction.

#### Prior finding assessments

- **R1-F1** — `fix_verified` — The version-bound operands follow the stationary humanoid clip to a PlayableInfo or off-hand AnimationComposition. AddAnimationClip assigns that object to handle.ActiveAnimation and adds the same object to m_ActiveAnimations. GetWeight reads the playable’s m_Weight directly or through the composition’s first playable. Reset, transition writes and mixer normalization establish the source and meaning of its zero and positive values. The candidate’s bind-to-idle and remount results exercise the corresponding adapter decisions.

#### Evidence assessment

- **Inspected:** Controller mission, request, prior findings ledger, developer report, transcript notice, focused receipt and log.; AGENTS.md, validation policy, current handoffs, supplied feature diff, round-three-to-four changes and supplemental accounting records.; The installed idle-weight trace, its operand checks, the production pose adapter and sampler, pair attachment and restoration, profile selection and the attachment fixtures.
- **Executed verification:** The controller receipt and checksum-matched log confirm that Fast with Test-MmrHorseSeatFocused.ps1 passed on the candidate in 96.3 seconds. All 14 focused children passed, along with 1,526 component checks and 153 source checks.; The installed seat contract passed 30/30, the attachment harness 19/19 and the attachment-slot harness 8/8. Each of the five ineligible animation states held for 420 frames without settling. The subsequent stationary idle measured 0.5400001 m above the Chest against 0.5399996 m on remount.; The gate also recorded recovery after 31.5 seconds of disabled animation, rejection of cumulative drift, retention of the confirmed seat through two action poses, restoration on dismount and one configured adapter after view replacement.; The Mounted Shield contract passed 114/114 and selection rollback passed 68/68. The packet, production and embedded icon had identical SHA-256 hashes. The gate used the existing shared budget ID and closed at 102.47 of 150 minutes.
- **Source inspection only:** The correction changes the adapter’s explanatory comment. Its predicate still requires native idle eligibility, positive weight on the locomotion handle’s current animation and absence of a positively weighted released non-additive action.; The Medium Horse profile owns the settling sampler. Calibration uses the attachment-frame anchor-to-Chest relation and the current rider-local pelvis. The paired mechanics anchor and action authority retain their existing paths.
- **Required gaps:** none
- **Limitations:** The controller supplied the gate receipt and log and withheld the mixed-session developer transcript. This review used read-only inspection.; The attachment harness models animation inputs and transform translation and counts leg IK calls. Rendered seating, the live timing of the first contributing idle frame and native save/load behavior remain owner observations.

#### Summary

- **Delivered behavior:** The supported Medium Horse rider starts with a provisional seat and confirms it once from a stable, contributing idle. In the offline harness, first mount, remount and replacement views converged on the same Chest-relative height, and dismount restored the original pelvis and placement.
- **Scope reviewed:** Draft PR #68, MMR-01 Horse seating lifecycle, including S02, the S08 Shield regression boundary and the round-four evidence for R1-F1. Integration and alpha packaging belong to PR-6.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** N01 — Compare first mount, remount and mounted reload at the same camera angle, profile and stance. Observe idle, a short move, turn and stop, and normal unmounted posture.; N10 — Use an existing Horse through a mounted reload or area/view transition and confirm one pair, correct seating and Shield effects, and normal action availability.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a217cb-5d28-7d82-a901-844d5a44cdc7`, duration 521.8s, response sha256 `1f6e921bcbd6bcc9a93349e3ae9514ba05bef0977abb2b4faec0bf597a9ddd58`, request `kmc-codex-mmr-horse-seat-20261126011011-d3c5-r04-4ca59f`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "8bfe701d448325b1d50c6f4ff2619946a8174eb9",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "ready_for_owner_review",
  "decision_rationale": "R1-F1 is verified. The Kingmaker 2.1.7b trace establishes which idle animation the adapter reads and how its weight reaches the mixer. The candidate\u2019s focused gate shows recovery from a prolonged high bind pose to the remount height. S02 and S08 meet the required offline acceptance criteria.",
  "evidence_assessment": {
    "executed_verification": [
      "The controller receipt and checksum-matched log confirm that Fast with Test-MmrHorseSeatFocused.ps1 passed on the candidate in 96.3 seconds. All 14 focused children passed, along with 1,526 component checks and 153 source checks.",
      "The installed seat contract passed 30/30, the attachment harness 19/19 and the attachment-slot harness 8/8. Each of the five ineligible animation states held for 420 frames without settling. The subsequent stationary idle measured 0.5400001 m above the Chest against 0.5399996 m on remount.",
      "The gate also recorded recovery after 31.5 seconds of disabled animation, rejection of cumulative drift, retention of the confirmed seat through two action poses, restoration on dismount and one configured adapter after view replacement.",
      "The Mounted Shield contract passed 114/114 and selection rollback passed 68/68. The packet, production and embedded icon had identical SHA-256 hashes. The gate used the existing shared budget ID and closed at 102.47 of 150 minutes."
    ],
    "inspected": [
      "Controller mission, request, prior findings ledger, developer report, transcript notice, focused receipt and log.",
      "AGENTS.md, validation policy, current handoffs, supplied feature diff, round-three-to-four changes and supplemental accounting records.",
      "The installed idle-weight trace, its operand checks, the production pose adapter and sampler, pair attachment and restoration, profile selection and the attachment fixtures."
    ],
    "limitations": [
      "The controller supplied the gate receipt and log and withheld the mixed-session developer transcript. This review used read-only inspection.",
      "The attachment harness models animation inputs and transform translation and counts leg IK calls. Rendered seating, the live timing of the first contributing idle frame and native save/load behavior remain owner observations."
    ],
    "required_gaps": [],
    "source_inspection_only": [
      "The correction changes the adapter\u2019s explanatory comment. Its predicate still requires native idle eligibility, positive weight on the locomotion handle\u2019s current animation and absence of a positively weighted released non-additive action.",
      "The Medium Horse profile owns the settling sampler. Calibration uses the attachment-frame anchor-to-Chest relation and the current rider-local pelvis. The paired mechanics anchor and action authority retain their existing paths."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "S02"
      ],
      "actual": "Line 82 says the first transition update makes the idle weight positive, although the documented formula can receive w = 0. Line 96 describes the half-second stability window as starting after two seconds. The gate records a continuously stable seat settling at 2.017 seconds.",
      "blocking_rationale": "The adapter reads the actual weight on each frame. The operand trace and attachment results establish the required behavior independently of these timing statements.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "The trace should describe when the blend calculation yields positive weight and make clear that the minimum elapsed time and stable window can overlap.",
      "id": "R4-F1",
      "location": {
        "file": "docs/missions/mmr-split/evidence/PR-3-idle-weight-trace.md",
        "line": 82
      },
      "priority": "P3",
      "requested_correction": "Qualify the first-update claim by the resulting blend weight. Describe settlement as requiring both time thresholds to hold on the same frame.",
      "requested_evidence": "The existing UpdateAnimations and UpdateInternal trace and the frame-121 gate output suffice for this wording correction.",
      "title": "The trace overstates when idle gains weight and when the seat can settle",
      "trigger": "A stationary clip registers while another action has full weight, or the rider is already stable at attachment."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F1",
      "rationale": "The version-bound operands follow the stationary humanoid clip to a PlayableInfo or off-hand AnimationComposition. AddAnimationClip assigns that object to handle.ActiveAnimation and adds the same object to m_ActiveAnimations. GetWeight reads the playable\u2019s m_Weight directly or through the composition\u2019s first playable. Reset, transition writes and mixer normalization establish the source and meaning of its zero and positive values. The candidate\u2019s bind-to-idle and remount results exercise the corresponding adapter decisions."
    }
  ],
  "request_id": "kmc-codex-mmr-horse-seat-20261126011011-d3c5-r04-4ca59f",
  "round": 4,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The supported Medium Horse rider starts with a provisional seat and confirms it once from a stable, contributing idle. In the offline harness, first mount, remount and replacement views converged on the same Chest-relative height, and dismount restored the original pelvis and placement.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "N01 \u2014 Compare first mount, remount and mounted reload at the same camera angle, profile and stance. Observe idle, a short move, turn and stop, and normal unmounted posture.",
      "N10 \u2014 Use an existing Horse through a mounted reload or area/view transition and confirm one pair, correct seating and Shield effects, and normal action availability."
    ],
    "scope_reviewed": "Draft PR #68, MMR-01 Horse seating lifecycle, including S02, the S08 Shield regression boundary and the round-four evidence for R1-F1. Integration and alpha packaging belong to PR-6.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
