<!-- agent-review:v1 enrollment=kmc-codex-mmr-horse-seat-20261126011011-d3c5 key=r02-review part=1/1 sha256=6594379a831eacd145ac337e3bb32e671a1efe884a3b0b6b8b5fe75d34323716 -->
### Codex GPT-6.2 Astra — review — round 2 — candidate `2e309fdda1`

**Decision:** `changes_required` — Four prior findings are resolved. R1-F1 remains open. The corrected harness shows that an empty animation list keeps the seat provisional. It assumes the positive signal that the adapter needs to recognize a genuine stationary idle.

## Continuation for Claude

Trace the installed Kingmaker 2.1.7b humanoid idle path far enough to identify the animation that enters ActiveAnimations and what GetWeight reports when it contributes to the rider. If that contract supports the current predicate, record the evidence. If it differs, adjust the predicate and fixture together. Retain the prolonged bind-pose, stationary-idle and remount comparisons and run the existing focused gate on the resulting candidate. Keep the correction within Medium Horse seat calibration and its offline evidence. N01 and N10 remain with the owner.

#### Findings

**R1-F1** · P1 · evidence_gap · blocks readiness — The positive idle signal is assumed by the fixture
- Location: `src/KingmakerMountedCombat/Integration/MountedRiderPoseAdapter.cs:334`; acceptance: S02
- Trigger: A newly attached or restored Medium rider moves from a bind pose to stationary idle while the animation graph is already playing.
- Expected: The adapter has a verified way to recognize when the resting animation contributes to this rider, including an idle whose pelvis stays still.
- Actual: The fixture switches from an empty list to a hard-coded locomotion handle of weight 1 and writes the lower pelvis independently. The installed-contract check at Test-MmrHorseSeatNativeContract.ps1 line 126 confirms calls to CanRunIdleAction, IsReleased and GetWeight. It does not establish how the stock humanoid idle enters ActiveAnimations or what that weight measures. The production predicate also counts an unreleased positive-weight handle whose Action is null as a base contributor.
- Why it blocks / does not: Every successful correction of a high attachment seat now depends on this signal. If the ordinary idle is absent from the qualifying entries, the seat stays provisional. If an entry qualifies before the resting pose contributes, the sampler can permanently confirm the high pose.
- Requested correction: Establish the stock idle handle, its registration and its weight semantics from the installed contract. Use that evidence to justify the predicate or narrow it to the verified contributor. Account for a missing Action in the positive branch.
- Requested evidence: Provide a version-bound offline trace or contract check identifying the resting animation before and after it contributes. Exercise those states in the existing long-bind-to-stationary-idle case and compare the final seat with a remount.

**R2-F1** · P3 · optional_polish · non-blocking — The repeated-attachment case overstates its coverage
- Location: `tests/Chunk7Diagnostics/HorseSeatAttachmentTemplate.cs:1240`; acceptance: S02
- Trigger: Reading the R1-F2 PASS line as evidence of replacement or per-frame leg ordering.
- Expected: The case label and harness header describe the operations the case exercises and the assertions it makes.
- Actual: The R1-F2 case attaches the same rider view three times. Its leg assertion accepts any positive even total, including calls made during priming. The label claims view replacement and seat-before-legs ordering. The file header also still describes ApplyPose as a stub.
- Why it blocks / does not: Separate cases exercise real view replacement within the harness, and the shipping method and offset-sensitive geometry establish the required pelvis sequence.
- Requested correction: Describe this case as repeated attachment, offset application and restoration. Credit replacement to the existing replacement cases and update the header to identify ApplyLeg as the stub.
- Requested evidence: The case name, header and focused output should agree with the exercised path.

**R2-F2** · P3 · optional_polish · non-blocking — The diagnostic assertion does not check the printed vectors
- Location: `tests/Chunk7Diagnostics/HorseSeatAttachmentTemplate.cs:1296`; acceptance: S02
- Trigger: The presentation formatter retains its labels but emits an incorrect numeric basis.
- Expected: The regression assertion compares the displayed applied basis with the basis projected for the fixture.
- Actual: The R1-F4 assertion checks the formatter's prefixes and the magnitude of a separate adapter property. A formatter that printed a zero Small vector after the correct prefix would still pass.
- Why it blocks / does not: The current source and captured output both show the correct values for all three controls.
- Requested correction: Compare the formatted Small basis with SmallCorrection and the Horse applied basis with its expected measured value.
- Requested evidence: An incorrect printed vector should fail the existing presentation case.

#### Prior finding assessments

- **R1-F1** — `fix_rejected` — The prolonged empty-list bind-pose reproduction is fixed. The remaining part of the finding is the verified indication of a contributing resting pose. All successful idle fixtures supply a synthetic full-weight handle, and the installed check does not establish the corresponding native idle path.
- **R1-F2** — `fix_verified` — The harness now executes the shipping ApplyPose with the nonzero Horse offset. The post-drift seat measured 0.5400001 above the Chest against 0.5399996 on an actual harness remount. Separate replacement cases use the same production method, check one configured adapter and verify restoration of the original local pelvis.
- **R1-F3** — `fix_verified` — Observe clears the open window on a non-finite sample. The passing component case interrupts a nearly complete high window with NaN, rejects a brief return to that height and settles only after the subsequent stationary idle.
- **R1-F4** — `fix_verified` — CapturePresentationObservation uses the corrected formatter. The gate printed a settled Horse basis, a fixed Small basis of (0, -0.05, 0.02) with the observation marked unmeasured, and no animated saddle for the Mammoth.
- **R1-F5** — `fix_verified` — The unused LoadedUnit declaration was removed from MountedStockAttackPolicyTests.cs. The candidate test assembly compiled and its component checks passed.

#### Evidence assessment

- **Inspected:** The enrolled mission, repository validation policy, current handoffs, prior findings, developer dispositions, 15-file feature diff and round-2 correction.; The sampler, pose and attachment leases, rider synchronization, profile selection, presentation formatter, harness extraction script, fixture and installed-contract checks.
- **Executed verification:** Confirmed HEAD and merge base, checked the enrolled mission hash, and ran git diff --check successfully. The seven designated extraction files also matched their PR #64 sources at f67c3b16.; Verified the controller receipt and its log SHA-256, 2799c4ad7dab617a5acb91f0950351261f65d2e9b64224ab1cfcfd3d6a4648e5. Test-Tiers.ps1 -Tier Fast -Focused Test-MmrHorseSeatFocused.ps1 passed on the exact candidate in 33.7 seconds with 14 of 14 focused children.; The log records 1,526 component checks, 153 source checks, 19 seat contract checks, 19 seat harness cases and 8 attachment-slot cases, all passing. The harness ignored 1,890 unevaluated frames and recovered to the remount height. Two later held action poses preserved the confirmed reference.; Mounted Shield passed 114 contract checks and 68 rollback checks, including byte equality of the packet, production and embedded icon. Archery, Ranger/Riding Dog, replay, localization and loader checks also passed.
- **Source inspection only:** The shipping ApplyPose writes the profile pelvis offset, projects the seat, then applies both legs. The harness executes that method and checks the resulting pelvis. Leg-call order is established by source inspection.; The settling path is selected only for the Medium Horse profile. The changes preserve the existing mechanics anchor, profile constants, save identities and native action authority.
- **Required gaps:** R1-F1 needs an installed-contract basis for identifying the contributing stationary idle among ActiveAnimations. The positive harness case supplies that relationship itself.
- **Limitations:** The reviewer used read-only inspection. Execution evidence comes from the controller receipt and log. The foreground developer transcript was withheld, so earlier direct runs received no independent credit.; The seat harness uses translation-only world positions and a counted leg-IK stub. The gate reports nativeProof=false. Rendered seating, actual mounted reload and live area transitions remain owner observations.

#### Summary

- **Delivered behavior:** The Medium Horse adapter starts from a provisional seat, samples the posed pelvis relative to the attachment geometry and confirms one stable reference. The harness demonstrates recovery after pauses, casts and drift, consistent remount and replacement geometry, and clean dismount restoration.
- **Scope reviewed:** Final PR-3 review of MMR-01 and the S02 and S08 offline obligations. Integrated alpha packaging belongs to PR-6.
- **Pending later-phase work:** none
- **Unresolved questions:** Which installed stationary-idle animation supplies the positive weight used by SeatRestEligible, and when does that weight establish contribution to the rider?
- **Remaining owner review:** N01. Compare first mount, remount and mounted reload at matched camera angles and stance, including a short move, turn, stop and dismount.; N10. Check a loaded or transitioned pre-repair pair for a single attachment, correct effects and ordinary action availability.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a21692-b484-7b23-806d-d3fb91cea46b`, duration 762.3s, response sha256 `de8995edb166755b7fda9949042de3a67b206b78fa89c2e8c936af565960dc2a`, request `kmc-codex-mmr-horse-seat-20261126011011-d3c5-r02-4f54fb`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "2e309fdda1f66c6e92b607fc8a7322cdbae77729",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "Four prior findings are resolved. R1-F1 remains open. The corrected harness shows that an empty animation list keeps the seat provisional. It assumes the positive signal that the adapter needs to recognize a genuine stationary idle.\n\n## Continuation for Claude\n\nTrace the installed Kingmaker 2.1.7b humanoid idle path far enough to identify the animation that enters ActiveAnimations and what GetWeight reports when it contributes to the rider. If that contract supports the current predicate, record the evidence. If it differs, adjust the predicate and fixture together. Retain the prolonged bind-pose, stationary-idle and remount comparisons and run the existing focused gate on the resulting candidate. Keep the correction within Medium Horse seat calibration and its offline evidence. N01 and N10 remain with the owner.",
  "evidence_assessment": {
    "executed_verification": [
      "Confirmed HEAD and merge base, checked the enrolled mission hash, and ran git diff --check successfully. The seven designated extraction files also matched their PR #64 sources at f67c3b16.",
      "Verified the controller receipt and its log SHA-256, 2799c4ad7dab617a5acb91f0950351261f65d2e9b64224ab1cfcfd3d6a4648e5. Test-Tiers.ps1 -Tier Fast -Focused Test-MmrHorseSeatFocused.ps1 passed on the exact candidate in 33.7 seconds with 14 of 14 focused children.",
      "The log records 1,526 component checks, 153 source checks, 19 seat contract checks, 19 seat harness cases and 8 attachment-slot cases, all passing. The harness ignored 1,890 unevaluated frames and recovered to the remount height. Two later held action poses preserved the confirmed reference.",
      "Mounted Shield passed 114 contract checks and 68 rollback checks, including byte equality of the packet, production and embedded icon. Archery, Ranger/Riding Dog, replay, localization and loader checks also passed."
    ],
    "inspected": [
      "The enrolled mission, repository validation policy, current handoffs, prior findings, developer dispositions, 15-file feature diff and round-2 correction.",
      "The sampler, pose and attachment leases, rider synchronization, profile selection, presentation formatter, harness extraction script, fixture and installed-contract checks."
    ],
    "limitations": [
      "The reviewer used read-only inspection. Execution evidence comes from the controller receipt and log. The foreground developer transcript was withheld, so earlier direct runs received no independent credit.",
      "The seat harness uses translation-only world positions and a counted leg-IK stub. The gate reports nativeProof=false. Rendered seating, actual mounted reload and live area transitions remain owner observations."
    ],
    "required_gaps": [
      "R1-F1 needs an installed-contract basis for identifying the contributing stationary idle among ActiveAnimations. The positive harness case supplies that relationship itself."
    ],
    "source_inspection_only": [
      "The shipping ApplyPose writes the profile pelvis offset, projects the seat, then applies both legs. The harness executes that method and checks the resulting pelvis. Leg-call order is established by source inspection.",
      "The settling path is selected only for the Medium Horse profile. The changes preserve the existing mechanics anchor, profile constants, save identities and native action authority."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "S02"
      ],
      "actual": "The fixture switches from an empty list to a hard-coded locomotion handle of weight 1 and writes the lower pelvis independently. The installed-contract check at Test-MmrHorseSeatNativeContract.ps1 line 126 confirms calls to CanRunIdleAction, IsReleased and GetWeight. It does not establish how the stock humanoid idle enters ActiveAnimations or what that weight measures. The production predicate also counts an unreleased positive-weight handle whose Action is null as a base contributor.",
      "blocking_rationale": "Every successful correction of a high attachment seat now depends on this signal. If the ordinary idle is absent from the qualifying entries, the seat stays provisional. If an entry qualifies before the resting pose contributes, the sampler can permanently confirm the high pose.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "The adapter has a verified way to recognize when the resting animation contributes to this rider, including an idle whose pelvis stays still.",
      "id": "R1-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedRiderPoseAdapter.cs",
        "line": 334
      },
      "priority": "P1",
      "requested_correction": "Establish the stock idle handle, its registration and its weight semantics from the installed contract. Use that evidence to justify the predicate or narrow it to the verified contributor. Account for a missing Action in the positive branch.",
      "requested_evidence": "Provide a version-bound offline trace or contract check identifying the resting animation before and after it contributes. Exercise those states in the existing long-bind-to-stationary-idle case and compare the final seat with a remount.",
      "title": "The positive idle signal is assumed by the fixture",
      "trigger": "A newly attached or restored Medium rider moves from a bind pose to stationary idle while the animation graph is already playing."
    },
    {
      "acceptance_ids": [
        "S02"
      ],
      "actual": "The R1-F2 case attaches the same rider view three times. Its leg assertion accepts any positive even total, including calls made during priming. The label claims view replacement and seat-before-legs ordering. The file header also still describes ApplyPose as a stub.",
      "blocking_rationale": "Separate cases exercise real view replacement within the harness, and the shipping method and offset-sensitive geometry establish the required pelvis sequence.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "The case label and harness header describe the operations the case exercises and the assertions it makes.",
      "id": "R2-F1",
      "location": {
        "file": "tests/Chunk7Diagnostics/HorseSeatAttachmentTemplate.cs",
        "line": 1240
      },
      "priority": "P3",
      "requested_correction": "Describe this case as repeated attachment, offset application and restoration. Credit replacement to the existing replacement cases and update the header to identify ApplyLeg as the stub.",
      "requested_evidence": "The case name, header and focused output should agree with the exercised path.",
      "title": "The repeated-attachment case overstates its coverage",
      "trigger": "Reading the R1-F2 PASS line as evidence of replacement or per-frame leg ordering."
    },
    {
      "acceptance_ids": [
        "S02"
      ],
      "actual": "The R1-F4 assertion checks the formatter's prefixes and the magnitude of a separate adapter property. A formatter that printed a zero Small vector after the correct prefix would still pass.",
      "blocking_rationale": "The current source and captured output both show the correct values for all three controls.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "The regression assertion compares the displayed applied basis with the basis projected for the fixture.",
      "id": "R2-F2",
      "location": {
        "file": "tests/Chunk7Diagnostics/HorseSeatAttachmentTemplate.cs",
        "line": 1296
      },
      "priority": "P3",
      "requested_correction": "Compare the formatted Small basis with SmallCorrection and the Horse applied basis with its expected measured value.",
      "requested_evidence": "An incorrect printed vector should fail the existing presentation case.",
      "title": "The diagnostic assertion does not check the printed vectors",
      "trigger": "The presentation formatter retains its labels but emits an incorrect numeric basis."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F1",
      "rationale": "The prolonged empty-list bind-pose reproduction is fixed. The remaining part of the finding is the verified indication of a contributing resting pose. All successful idle fixtures supply a synthetic full-weight handle, and the installed check does not establish the corresponding native idle path."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F2",
      "rationale": "The harness now executes the shipping ApplyPose with the nonzero Horse offset. The post-drift seat measured 0.5400001 above the Chest against 0.5399996 on an actual harness remount. Separate replacement cases use the same production method, check one configured adapter and verify restoration of the original local pelvis."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F3",
      "rationale": "Observe clears the open window on a non-finite sample. The passing component case interrupts a nearly complete high window with NaN, rejects a brief return to that height and settles only after the subsequent stationary idle."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F4",
      "rationale": "CapturePresentationObservation uses the corrected formatter. The gate printed a settled Horse basis, a fixed Small basis of (0, -0.05, 0.02) with the observation marked unmeasured, and no animated saddle for the Mammoth."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F5",
      "rationale": "The unused LoadedUnit declaration was removed from MountedStockAttackPolicyTests.cs. The candidate test assembly compiled and its component checks passed."
    }
  ],
  "request_id": "kmc-codex-mmr-horse-seat-20261126011011-d3c5-r02-4f54fb",
  "round": 2,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The Medium Horse adapter starts from a provisional seat, samples the posed pelvis relative to the attachment geometry and confirms one stable reference. The harness demonstrates recovery after pauses, casts and drift, consistent remount and replacement geometry, and clean dismount restoration.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "N01. Compare first mount, remount and mounted reload at matched camera angles and stance, including a short move, turn, stop and dismount.",
      "N10. Check a loaded or transitioned pre-repair pair for a single attachment, correct effects and ordinary action availability."
    ],
    "scope_reviewed": "Final PR-3 review of MMR-01 and the S02 and S08 offline obligations. Integrated alpha packaging belongs to PR-6.",
    "unresolved_questions": [
      "Which installed stationary-idle animation supplies the positive weight used by SeatRestEligible, and when does that weight establish contribution to the rider?"
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
