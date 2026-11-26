<!-- agent-review:v1 enrollment=kmc-codex-mmr-horse-seat-20261126011011-d3c5 key=r03-review part=1/1 sha256=0c01dabb41faea26d234eacd1530e03c3b9f3d0b84ddeffcdcf63238c8256e74 -->
### Codex GPT-6.2 Astra — review — round 3 — candidate `642cb4818c`

**Decision:** `changes_required` — The focused gate passed, and both round-2 harness findings are fixed. R1-F1 remains open because the installed-assembly checks do not establish what the idle animation’s GetWeight() value measures. The adapter uses that value to decide when it can permanently confirm the seat.

## Continuation for Claude

PR-3 has used all three allocated rounds. Preserve this result for the owner’s decision. If further work is authorized, close R1-F1 with a short, version-bound offline trace through the stock humanoid idle branch. Show that the same animation is registered in ActiveAnimations and assigned to the locomotion handle, then identify how its GetWeight() value relates to the blend applied to that animation. The existing long-bind and stationary-remount cases already exercise the adapter once those input semantics are established. Change the predicate only if the trace identifies a mismatch. Keep the follow-up within Horse seat calibration and its focused gate.

#### Findings

**R1-F1** · P1 · evidence_gap · blocks readiness — The installed check does not establish the idle contribution read by GetWeight()
- Location: `scripts/Test-MmrHorseSeatNativeContract.ps1:169`; acceptance: S02
- Trigger: A newly attached or restored Medium Horse rider moves from a high bind pose into stationary idle while the animation graph is playing.
- Expected: The installed contract identifies the stock stationary animation in ActiveAnimations and establishes that the value read by the adapter represents that animation’s contribution.
- Actual: StoresActive checks for an ActiveAnimation setter call and the m_ActiveAnimations field token. The humanoid check finds IsIdle and clip-add calls in the same method. The weight check finds TransitionIn and SetWeight in UpdateInternal. None follows the stationary branch’s animation object through registration or inspects what that object’s GetWeight() returns. The fixture supplies weights of zero and one and separately writes the idle pelvis.
- Why it blocks / does not: Every recovery from a high attachment pose depends on this positive signal. If it arrives before the idle contributes, the sampler can confirm the high basis permanently. If the registered idle never satisfies it, the seat stays provisional.
- Requested correction: Trace the stationary clip through registration and the concrete GetWeight() implementation. Use the result to substantiate or correct the existing predicate.
- Requested evidence: A concise Kingmaker 2.1.7b method-and-operand trace or focused offline contract check showing the same idle composition in the list and on the handle, and the source of its zero and positive blend values. Pair it with the existing bind-to-idle and remount output.

#### Prior finding assessments

- **R1-F1** — `fix_rejected` — The narrower predicate fixes the missing-action case. The gate also confirms rejection of absent, zero-weight, non-locomotion and stale entries. The remaining gap is the meaning of the positive value on the stock idle composition. The new checks establish relevant call sites, but leave the registration and weight relationship assumed by the fixture.
- **R2-F1** — `fix_verified` — The header identifies ApplyPose as production code and ApplyLeg as a call counter. The case now describes three attachments of one view. Each printed the expected seat, 424 leg calls and successful restoration. Separate passing cases exercise view replacement.
- **R2-F2** — `fix_verified` — The assertion now compares the displayed Horse vectors with the independently projected fixture basis (0.0000, 0.7000, -0.0200) and the full Small result with its fixed basis (0.0000, -0.0500, 0.0200). The gate printed both matches and the Mammoth’s no-saddle result.

#### Evidence assessment

- **Inspected:** The enrolled mission, repository instructions, validation policy, prior findings ledger, developer report, full feature diff and the four files changed since round 2.; The production attachment, pose application, calibration, restoration and presentation-reporting paths, the installed-contract assertions and the fixture that supplies animation state.; The controller’s candidate-bound receipt and log. Git also confirmed that the seven named extraction files matched their PR #64 source at the extraction commit.
- **Executed verification:** Verified the controller receipt against the candidate and the gate log’s SHA-256. Test-Tiers.ps1 -Tier Fast -Focused Test-MmrHorseSeatFocused.ps1 exited 0 in 34.4 seconds. All 14 focused children passed.; The gate recorded 1,526 component checks, 153 source checks, 24 seat contract checks, 19 seat attachment cases and 8 attachment-slot cases, all passing. Each of the five high-bind controls recorded 420 ineligible frames out of 420. The subsequent stationary seat matched the remount control.; Mounted Shield passed 114 contract checks and 68 rollback checks. The packet, production and embedded icon hashes matched. Archery, Ranger/Riding Dog, replay, localization and loader checks also passed.
- **Source inspection only:** SeatRestEligible checks native idle eligibility and requires an unreleased locomotion action whose current animation matches the list entry and has positive weight. A released non-additive animation with positive weight still excludes the frame.; The adapter samples the posed pelvis before writing the animated seat. Its confirmed basis is final for that attachment. The settling path is selected only for the Medium Horse profile.
- **Required gaps:** R1-F1 requires a version-bound link from the stationary humanoid clip registration to the blend value returned by GetWeight() on that same animation.
- **Limitations:** The reviewer used the archived gate in the read-only snapshot. The controller withheld the mixed foreground-session transcript, so the developer’s preparatory runs could not be independently confirmed.; The attachment harness supplies animation weights and pelvis poses independently, uses translation-only transforms and counts leg calls. Rendered seating, native blend timing and save/load behavior remain owner observations.

#### Summary

- **Delivered behavior:** The offline attachment path recovers from a high provisional seat, matches first mount to remount at stationary idle, restores the unmounted pelvis and gives a replacement view one configured adapter. The Small and Mammoth controls passed.
- **Scope reviewed:** The complete PR-3 MMR-01 source/offline checkpoint, including S02 seating lifecycle, S08 Mounted Shield preservation and the round-3 corrections.
- **Pending later-phase work:** none
- **Unresolved questions:** Which blend value does the registered stock humanoid idle composition return from GetWeight() as it begins contributing to the rider?
- **Remaining owner review:** Decide how to proceed with R1-F1 after the third allocated review.; N01 covers visible seating at matched camera angles on first mount, remount and mounted reload, including a short move, turn, stop and dismount.; N10 covers one native reload or area/view transition with the existing Horse, a single pair and no stale attachment, duplicate Shield effect or extra action.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a216ab-0a8e-7181-9d99-446f12630923`, duration 657.6s, response sha256 `b78ed8fa90061f84c879498f8416800f1e53b686ac44d4c08a1017421aa04fe3`, request `kmc-codex-mmr-horse-seat-20261126011011-d3c5-r03-4fc996`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "642cb4818c567c14e05af3befa486b90e28afec0",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The focused gate passed, and both round-2 harness findings are fixed. R1-F1 remains open because the installed-assembly checks do not establish what the idle animation\u2019s GetWeight() value measures. The adapter uses that value to decide when it can permanently confirm the seat.\n\n## Continuation for Claude\n\nPR-3 has used all three allocated rounds. Preserve this result for the owner\u2019s decision. If further work is authorized, close R1-F1 with a short, version-bound offline trace through the stock humanoid idle branch. Show that the same animation is registered in ActiveAnimations and assigned to the locomotion handle, then identify how its GetWeight() value relates to the blend applied to that animation. The existing long-bind and stationary-remount cases already exercise the adapter once those input semantics are established. Change the predicate only if the trace identifies a mismatch. Keep the follow-up within Horse seat calibration and its focused gate.",
  "evidence_assessment": {
    "executed_verification": [
      "Verified the controller receipt against the candidate and the gate log\u2019s SHA-256. Test-Tiers.ps1 -Tier Fast -Focused Test-MmrHorseSeatFocused.ps1 exited 0 in 34.4 seconds. All 14 focused children passed.",
      "The gate recorded 1,526 component checks, 153 source checks, 24 seat contract checks, 19 seat attachment cases and 8 attachment-slot cases, all passing. Each of the five high-bind controls recorded 420 ineligible frames out of 420. The subsequent stationary seat matched the remount control.",
      "Mounted Shield passed 114 contract checks and 68 rollback checks. The packet, production and embedded icon hashes matched. Archery, Ranger/Riding Dog, replay, localization and loader checks also passed."
    ],
    "inspected": [
      "The enrolled mission, repository instructions, validation policy, prior findings ledger, developer report, full feature diff and the four files changed since round 2.",
      "The production attachment, pose application, calibration, restoration and presentation-reporting paths, the installed-contract assertions and the fixture that supplies animation state.",
      "The controller\u2019s candidate-bound receipt and log. Git also confirmed that the seven named extraction files matched their PR #64 source at the extraction commit."
    ],
    "limitations": [
      "The reviewer used the archived gate in the read-only snapshot. The controller withheld the mixed foreground-session transcript, so the developer\u2019s preparatory runs could not be independently confirmed.",
      "The attachment harness supplies animation weights and pelvis poses independently, uses translation-only transforms and counts leg calls. Rendered seating, native blend timing and save/load behavior remain owner observations."
    ],
    "required_gaps": [
      "R1-F1 requires a version-bound link from the stationary humanoid clip registration to the blend value returned by GetWeight() on that same animation."
    ],
    "source_inspection_only": [
      "SeatRestEligible checks native idle eligibility and requires an unreleased locomotion action whose current animation matches the list entry and has positive weight. A released non-additive animation with positive weight still excludes the frame.",
      "The adapter samples the posed pelvis before writing the animated seat. Its confirmed basis is final for that attachment. The settling path is selected only for the Medium Horse profile."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "S02"
      ],
      "actual": "StoresActive checks for an ActiveAnimation setter call and the m_ActiveAnimations field token. The humanoid check finds IsIdle and clip-add calls in the same method. The weight check finds TransitionIn and SetWeight in UpdateInternal. None follows the stationary branch\u2019s animation object through registration or inspects what that object\u2019s GetWeight() returns. The fixture supplies weights of zero and one and separately writes the idle pelvis.",
      "blocking_rationale": "Every recovery from a high attachment pose depends on this positive signal. If it arrives before the idle contributes, the sampler can confirm the high basis permanently. If the registered idle never satisfies it, the seat stays provisional.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "The installed contract identifies the stock stationary animation in ActiveAnimations and establishes that the value read by the adapter represents that animation\u2019s contribution.",
      "id": "R1-F1",
      "location": {
        "file": "scripts/Test-MmrHorseSeatNativeContract.ps1",
        "line": 169
      },
      "priority": "P1",
      "requested_correction": "Trace the stationary clip through registration and the concrete GetWeight() implementation. Use the result to substantiate or correct the existing predicate.",
      "requested_evidence": "A concise Kingmaker 2.1.7b method-and-operand trace or focused offline contract check showing the same idle composition in the list and on the handle, and the source of its zero and positive blend values. Pair it with the existing bind-to-idle and remount output.",
      "title": "The installed check does not establish the idle contribution read by GetWeight()",
      "trigger": "A newly attached or restored Medium Horse rider moves from a high bind pose into stationary idle while the animation graph is playing."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F1",
      "rationale": "The narrower predicate fixes the missing-action case. The gate also confirms rejection of absent, zero-weight, non-locomotion and stale entries. The remaining gap is the meaning of the positive value on the stock idle composition. The new checks establish relevant call sites, but leave the registration and weight relationship assumed by the fixture."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R2-F1",
      "rationale": "The header identifies ApplyPose as production code and ApplyLeg as a call counter. The case now describes three attachments of one view. Each printed the expected seat, 424 leg calls and successful restoration. Separate passing cases exercise view replacement."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R2-F2",
      "rationale": "The assertion now compares the displayed Horse vectors with the independently projected fixture basis (0.0000, 0.7000, -0.0200) and the full Small result with its fixed basis (0.0000, -0.0500, 0.0200). The gate printed both matches and the Mammoth\u2019s no-saddle result."
    }
  ],
  "request_id": "kmc-codex-mmr-horse-seat-20261126011011-d3c5-r03-4fc996",
  "round": 3,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The offline attachment path recovers from a high provisional seat, matches first mount to remount at stationary idle, restores the unmounted pelvis and gives a replacement view one configured adapter. The Small and Mammoth controls passed.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "Decide how to proceed with R1-F1 after the third allocated review.",
      "N01 covers visible seating at matched camera angles on first mount, remount and mounted reload, including a short move, turn, stop and dismount.",
      "N10 covers one native reload or area/view transition with the existing Horse, a single pair and no stale attachment, duplicate Shield effect or extra action."
    ],
    "scope_reviewed": "The complete PR-3 MMR-01 source/offline checkpoint, including S02 seating lifecycle, S08 Mounted Shield preservation and the round-3 corrections.",
    "unresolved_questions": [
      "Which blend value does the registered stock humanoid idle composition return from GetWeight() as it begins contributing to the rider?"
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
