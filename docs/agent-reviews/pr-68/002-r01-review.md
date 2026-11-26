<!-- agent-review:v1 enrollment=kmc-codex-mmr-horse-seat-20261126011011-d3c5 key=r01-review part=1/1 sha256=081bca52ecab9c25ca8008b63dcc17c786216491a8b7387cee13cae74db93565 -->
### Codex GPT-6.2 Astra — review — round 1 — candidate `bbb0eeb6bc`

**Decision:** `changes_required` — The candidate passed its focused gate. S02 still has two evidence gaps at the point where a seat becomes permanent. The sampler also accepts an interrupted stability window, and the new diagnostic reports incorrect values for other profiles.

## Continuation for Claude

Establish when the rider’s animation graph is actually supplying a resting pose. Keep the seat provisional until that condition holds, or demonstrate the installed contract that guarantees it on attachment and rebind. Reset the stability window on an invalid observation. Exercise the production pelvis-offset and seat-write sequence through the existing attachment harness, including a remount after drift. Restrict calibration reporting to values the selected profile actually observed or applied.

Keep the correction within PR-3 presentation, diagnostics and focused checks. Preserve the existing pose constants, mechanics anchor, native action authority and Shield regressions. Provide the controller’s focused Fast receipt and log for the corrected candidate within the existing review and validation limits. N01 and N10 remain owner playtest observations.

#### Findings

**R1-F1** · P1 · evidence_gap · blocks readiness — A playing graph can qualify before a resting pose is established
- Location: `src/KingmakerMountedCombat/Integration/MountedRiderPoseAdapter.cs:286`; acceptance: S02
- Trigger: A newly attached or restored view has a valid playing graph, zero movement speed and no blocking action while its base animation has yet to contribute to the rider’s pose.
- Expected: The attachment sample stays provisional until the sampled rig receives an evaluated resting pose. A stationary pelvis must still qualify once that pose is available.
- Actual: SeatAnimationAdvanceSeconds returns Time.deltaTime for an enabled Animator and playing graph. SeatRestEligible accepts an empty ActiveAnimations collection and explicitly accepts null after CanRunIdleAction succeeds. A stable bind pose can therefore satisfy both timers under those inputs and become final. The harness always supplies a full-weight locomotion animation, and its mounted-load case gives the bind pose one frame. The installed-contract check finds graph creation and Play calls but establishes no bound on when a resting animation starts contributing.
- Why it blocks / does not: A premature confirmation prevents the later genuine idle from correcting the first-mount or restored seat. This is the readiness distinction on which the one-time calibration depends.
- Requested correction: Use a verified indication that the resting animation is contributing to the sampled rig, or provide the installed initialization and update contract that excludes a noncontributing interval long enough to settle. Preserve support for a constant-height idle.
- Requested evidence: Hold the high bind pose beyond the settle interval with the graph playing, positive game delta and no contributing base animation. Then supply the lower stationary idle and compare with a remount. An installed-contract demonstration that this initial state cannot persist on an admitted view would also resolve the gap.

**R1-F2** · P1 · evidence_gap · blocks readiness — The lifecycle harness skips the Horse pelvis offset
- Location: `tests/Chunk7Diagnostics/HorseSeatAttachmentTemplate.cs:470`; acceptance: S02
- Trigger: The seat harness drives a cast, idle or replacement through its local ApplyPose implementation.
- Expected: The lifecycle check exercises the shipping order from native pelvis baseline through the profile offset, calibration and final seat write, then verifies restoration.
- Actual: The harness replaces ApplyPose with a direct call to ApplyAnimatedSeat. It omits the Horse pelvis offset of (0, -0.17, -0.02). On its own unrotated fixture, it expects 0.71 above the Chest. Including the production offset gives 0.54 on the same inputs. The harness would also keep invoking the seat helper if the shipping ApplyPose stopped calling it. The drift case compares against this synthetic expected value without performing the remount named in its label.
- Why it blocks / does not: The passing lifecycle cases bypass the production operation that supplies the pelvis position being calibrated. They cannot detect a missing, duplicated or misordered profile offset.
- Requested correction: Exercise the production pelvis mutation and seat-call sequence in the harness. Keep Unity-specific operations behind the existing facade and derive the expected seat from the posed fixture. Compare the post-drift seat directly with a remount.
- Requested evidence: Show that first mount, remount and replacement apply the nonzero profile offset exactly once, that dismount restores the original local pelvis, and that the check detects omission or reordering of the seat call.

**R1-F3** · P2 · defect · blocks readiness — A non-finite sample leaves the previous stability window open
- Location: `src/KingmakerMountedCombat/Domain/AnimatedSaddleCalibration.cs:93`; acceptance: S02
- Trigger: A non-finite pelvis sample arrives between two short runs of otherwise eligible observations.
- Expected: An invalid sample breaks continuity. The next finite, evaluated and eligible sample starts a fresh stability window.
- Actual: Observe returns on a non-finite position before clearing hasWindow or inspecting advance and eligibility. For example, after the two-second minimum, 0.375 seconds of stable observations, a NaN on a paused frame and another 0.125 seconds at the same height can confirm the old window. The existing NaN assertion starts with a fresh sampler.
- Why it blocks / does not: Two short runs separated by an invalid frame can permanently confirm a pose even though neither run meets the required half-second window.
- Requested correction: Invalidate the open window when the sampled position is non-finite and preserve the distinction between evaluated, ineligible and paused frames.
- Requested evidence: Interrupt a nearly complete high-pose window with a non-finite frame. Verify that a brief return to the high pose cannot settle it and that a subsequent full stationary-idle window does settle.

**R1-F4** · P2 · defect · blocks readiness — Small and Mammoth diagnostics report calibration values they never measured
- Location: `src/KingmakerMountedCombat/Integration/KingmakerMountedPairRuntime.cs:1007`; acceptance: S02
- Trigger: A presentation observation is captured for a fixed-seat Small rider or a Medium Mammoth.
- Expected: A reported applied basis matches the lease being projected. Profiles without a measured calibration are identified accordingly.
- Actual: CapturePresentationObservation labels every adapter without a sampler as attachment and prints its observed and applied vectors. The Small fixed-seat branch never initializes those fields. A Halfling Horse seat applies approximately (0, 0.445, -0.248) from the Chest but reports (0, 0, 0). A Medium Mammoth also reports attachment calibration despite having no animated saddle.
- Why it blocks / does not: The new observation gives false measurements for the profiles used as isolation controls. Their actual seat geometry is preserved.
- Requested correction: Scope the measured calibration fields to the profiles that populate them, or report the actual fixed basis and an explicit state for profiles without an animated saddle.
- Requested evidence: Check the presentation observation for a settled Medium Horse, a Small fixed-seat profile with a nonzero offset and a Medium Mammoth.

**R1-F5** · P3 · optional_polish · non-blocking — The seat extraction includes an unrelated test fixture
- Location: `tests/MountedStockAttackPolicyTests.cs:510`; acceptance: —
- Trigger: Reviewing the shared test-file extraction against its PR-3 ownership.
- Expected: The shared test file carries the seat case and its dependencies.
- Actual: LoadedUnit has no references in this candidate. Its original consumer exercised LoadedInstanceRepairLedger in the separate repair area.
- Why it blocks / does not: The unused test declaration has no effect on the executed checks.
- Requested correction: Remove the unused LoadedUnit declaration from the PR-3 extraction.
- Requested evidence: The shared-file diff and ordinary focused compile suffice.

#### Evidence assessment

- **Inspected:** The controller-pinned mission, developer report, prior-findings ledger, transcript notice, gate receipt and log, repository instructions, validation policy and current handoffs.; The 15-file feature diff, seat sampler and adapter, pair attachment and restoration, Horse and Small profile constants, pose baseline lease, movement synchronization, native-contract script and both lifecycle harnesses.
- **Executed verification:** The controller ran Test-Tiers.ps1 -Tier Fast -Focused Test-MmrHorseSeatFocused.ps1 at the requested commit. It exited 0 in 34.7 seconds with 14 of 14 focused children passing. I verified the supplied log’s SHA-256 against the receipt.; The log records components 1526/0, Validate-Source 153/0, seat installed-contract checks 19/0, seat attachment cases 16/0 and attachment-slot cases 8/0. The facade recovered after 1,890 Animator-disabled frames and after a 31.5-second active cast. Its stationary first-mount and remount heights agreed within the test tolerance, and two later action holds preserved the confirmed basis.; Mounted Shield passed 114/0, including byte identity of the embedded icon. Shield rollback passed 68/0, Archery 54/0 and Ranger/Riding Dog 183/0. The replay, localization and loader checks passed. The product DLL SHA-256 was 6073baebc0c43bf642bc4422f47220b363949d0964aef76002cc3bca1a1d81a3.; Read-only Git comparison confirmed the merge base and candidate, a clean diff check, and identical blobs for the seven whole files extracted from 46444b30 into f67c3b16.
- **Source inspection only:** The normalized sample combines the attachment-frame anchor-to-Chest vector with the current pelvis-to-rider-root vector. Confirmation is final for that adapter. The change targets the Medium Horse profile and preserves the existing movement and action code.; The interrupted-window result, the missing pose operation in the harness, and the incorrect Small and Mammoth status fields follow from source inspection. The startup readiness concern identifies an unverified engine condition.
- **Required gaps:** Evidence that a playing graph supplies a freshly evaluated resting pose before permanent calibration, including attachment or rebind before the base animation contributes.; A lifecycle case using the production Horse pelvis offset and seat-write order, with direct remount and restoration observations.; A mid-window non-finite interruption case and accurate diagnostic observations for fixed-seat and uncalibrated profiles.
- **Limitations:** The controller withheld the mixed foreground transcript. The candidate-bound gate receipt and log establish the reported formal execution. Earlier local development runs were not independently confirmed.; The gate was offline. Unity rendering, a native mounted reload and owner-visible saddle contact remain unobserved on this candidate.; The gate recorded 94.86 minutes spent on the existing shared budget with a configured allowance of 150 minutes. The enrolled mission records 90 minutes and the supplemental ledger attributes the adjustment to the November 26 owner instruction.

#### Summary

- **Delivered behavior:** The Medium Horse adapter measures a provisional seat, confirms one normalized resting sample and preserves it through later actions. The focused gate demonstrates recovery for the modeled disabled-Animator and cast cases, Chest-height normalization, attachment cleanup and the Shield regression controls.
- **Scope reviewed:** The complete PR-3 MMR-01 diff against main, including its extraction, corrections and offline evidence for S02 and S08. Section 4.2 of the enrolled mission lists two verified historical regression obligations, R1-F12 and R5-F3.
- **Pending later-phase work:** none
- **Unresolved questions:** Does the installed rider animation lifecycle guarantee a contributing resting pose before the sampler can confirm a bind pose on attachment or rebind?
- **Remaining owner review:** N01. Compare visible seating on first mount, remount and mounted reload at matched camera angle, profile and stance, including movement, turning, stopping and unmounted restoration.; N10. Confirm one pair with the correct effects through a real save/load or area/view transition using an existing Horse.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a2167b-73c7-7f53-915a-73a33de31f6f`, duration 995.7s, response sha256 `92e41010c104ac6d63383ed76190f364502a2cda8d9ec63305425d2576d2cc75`, request `kmc-codex-mmr-horse-seat-20261126011011-d3c5-r01-bc619d`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "bbb0eeb6bcec63808753a071876764034a7baa41",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The candidate passed its focused gate. S02 still has two evidence gaps at the point where a seat becomes permanent. The sampler also accepts an interrupted stability window, and the new diagnostic reports incorrect values for other profiles.\n\n## Continuation for Claude\n\nEstablish when the rider\u2019s animation graph is actually supplying a resting pose. Keep the seat provisional until that condition holds, or demonstrate the installed contract that guarantees it on attachment and rebind. Reset the stability window on an invalid observation. Exercise the production pelvis-offset and seat-write sequence through the existing attachment harness, including a remount after drift. Restrict calibration reporting to values the selected profile actually observed or applied.\n\nKeep the correction within PR-3 presentation, diagnostics and focused checks. Preserve the existing pose constants, mechanics anchor, native action authority and Shield regressions. Provide the controller\u2019s focused Fast receipt and log for the corrected candidate within the existing review and validation limits. N01 and N10 remain owner playtest observations.",
  "evidence_assessment": {
    "executed_verification": [
      "The controller ran Test-Tiers.ps1 -Tier Fast -Focused Test-MmrHorseSeatFocused.ps1 at the requested commit. It exited 0 in 34.7 seconds with 14 of 14 focused children passing. I verified the supplied log\u2019s SHA-256 against the receipt.",
      "The log records components 1526/0, Validate-Source 153/0, seat installed-contract checks 19/0, seat attachment cases 16/0 and attachment-slot cases 8/0. The facade recovered after 1,890 Animator-disabled frames and after a 31.5-second active cast. Its stationary first-mount and remount heights agreed within the test tolerance, and two later action holds preserved the confirmed basis.",
      "Mounted Shield passed 114/0, including byte identity of the embedded icon. Shield rollback passed 68/0, Archery 54/0 and Ranger/Riding Dog 183/0. The replay, localization and loader checks passed. The product DLL SHA-256 was 6073baebc0c43bf642bc4422f47220b363949d0964aef76002cc3bca1a1d81a3.",
      "Read-only Git comparison confirmed the merge base and candidate, a clean diff check, and identical blobs for the seven whole files extracted from 46444b30 into f67c3b16."
    ],
    "inspected": [
      "The controller-pinned mission, developer report, prior-findings ledger, transcript notice, gate receipt and log, repository instructions, validation policy and current handoffs.",
      "The 15-file feature diff, seat sampler and adapter, pair attachment and restoration, Horse and Small profile constants, pose baseline lease, movement synchronization, native-contract script and both lifecycle harnesses."
    ],
    "limitations": [
      "The controller withheld the mixed foreground transcript. The candidate-bound gate receipt and log establish the reported formal execution. Earlier local development runs were not independently confirmed.",
      "The gate was offline. Unity rendering, a native mounted reload and owner-visible saddle contact remain unobserved on this candidate.",
      "The gate recorded 94.86 minutes spent on the existing shared budget with a configured allowance of 150 minutes. The enrolled mission records 90 minutes and the supplemental ledger attributes the adjustment to the November 26 owner instruction."
    ],
    "required_gaps": [
      "Evidence that a playing graph supplies a freshly evaluated resting pose before permanent calibration, including attachment or rebind before the base animation contributes.",
      "A lifecycle case using the production Horse pelvis offset and seat-write order, with direct remount and restoration observations.",
      "A mid-window non-finite interruption case and accurate diagnostic observations for fixed-seat and uncalibrated profiles."
    ],
    "source_inspection_only": [
      "The normalized sample combines the attachment-frame anchor-to-Chest vector with the current pelvis-to-rider-root vector. Confirmation is final for that adapter. The change targets the Medium Horse profile and preserves the existing movement and action code.",
      "The interrupted-window result, the missing pose operation in the harness, and the incorrect Small and Mammoth status fields follow from source inspection. The startup readiness concern identifies an unverified engine condition."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "S02"
      ],
      "actual": "SeatAnimationAdvanceSeconds returns Time.deltaTime for an enabled Animator and playing graph. SeatRestEligible accepts an empty ActiveAnimations collection and explicitly accepts null after CanRunIdleAction succeeds. A stable bind pose can therefore satisfy both timers under those inputs and become final. The harness always supplies a full-weight locomotion animation, and its mounted-load case gives the bind pose one frame. The installed-contract check finds graph creation and Play calls but establishes no bound on when a resting animation starts contributing.",
      "blocking_rationale": "A premature confirmation prevents the later genuine idle from correcting the first-mount or restored seat. This is the readiness distinction on which the one-time calibration depends.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "The attachment sample stays provisional until the sampled rig receives an evaluated resting pose. A stationary pelvis must still qualify once that pose is available.",
      "id": "R1-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedRiderPoseAdapter.cs",
        "line": 286
      },
      "priority": "P1",
      "requested_correction": "Use a verified indication that the resting animation is contributing to the sampled rig, or provide the installed initialization and update contract that excludes a noncontributing interval long enough to settle. Preserve support for a constant-height idle.",
      "requested_evidence": "Hold the high bind pose beyond the settle interval with the graph playing, positive game delta and no contributing base animation. Then supply the lower stationary idle and compare with a remount. An installed-contract demonstration that this initial state cannot persist on an admitted view would also resolve the gap.",
      "title": "A playing graph can qualify before a resting pose is established",
      "trigger": "A newly attached or restored view has a valid playing graph, zero movement speed and no blocking action while its base animation has yet to contribute to the rider\u2019s pose."
    },
    {
      "acceptance_ids": [
        "S02"
      ],
      "actual": "The harness replaces ApplyPose with a direct call to ApplyAnimatedSeat. It omits the Horse pelvis offset of (0, -0.17, -0.02). On its own unrotated fixture, it expects 0.71 above the Chest. Including the production offset gives 0.54 on the same inputs. The harness would also keep invoking the seat helper if the shipping ApplyPose stopped calling it. The drift case compares against this synthetic expected value without performing the remount named in its label.",
      "blocking_rationale": "The passing lifecycle cases bypass the production operation that supplies the pelvis position being calibrated. They cannot detect a missing, duplicated or misordered profile offset.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "The lifecycle check exercises the shipping order from native pelvis baseline through the profile offset, calibration and final seat write, then verifies restoration.",
      "id": "R1-F2",
      "location": {
        "file": "tests/Chunk7Diagnostics/HorseSeatAttachmentTemplate.cs",
        "line": 470
      },
      "priority": "P1",
      "requested_correction": "Exercise the production pelvis mutation and seat-call sequence in the harness. Keep Unity-specific operations behind the existing facade and derive the expected seat from the posed fixture. Compare the post-drift seat directly with a remount.",
      "requested_evidence": "Show that first mount, remount and replacement apply the nonzero profile offset exactly once, that dismount restores the original local pelvis, and that the check detects omission or reordering of the seat call.",
      "title": "The lifecycle harness skips the Horse pelvis offset",
      "trigger": "The seat harness drives a cast, idle or replacement through its local ApplyPose implementation."
    },
    {
      "acceptance_ids": [
        "S02"
      ],
      "actual": "Observe returns on a non-finite position before clearing hasWindow or inspecting advance and eligibility. For example, after the two-second minimum, 0.375 seconds of stable observations, a NaN on a paused frame and another 0.125 seconds at the same height can confirm the old window. The existing NaN assertion starts with a fresh sampler.",
      "blocking_rationale": "Two short runs separated by an invalid frame can permanently confirm a pose even though neither run meets the required half-second window.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "An invalid sample breaks continuity. The next finite, evaluated and eligible sample starts a fresh stability window.",
      "id": "R1-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/AnimatedSaddleCalibration.cs",
        "line": 93
      },
      "priority": "P2",
      "requested_correction": "Invalidate the open window when the sampled position is non-finite and preserve the distinction between evaluated, ineligible and paused frames.",
      "requested_evidence": "Interrupt a nearly complete high-pose window with a non-finite frame. Verify that a brief return to the high pose cannot settle it and that a subsequent full stationary-idle window does settle.",
      "title": "A non-finite sample leaves the previous stability window open",
      "trigger": "A non-finite pelvis sample arrives between two short runs of otherwise eligible observations."
    },
    {
      "acceptance_ids": [
        "S02"
      ],
      "actual": "CapturePresentationObservation labels every adapter without a sampler as attachment and prints its observed and applied vectors. The Small fixed-seat branch never initializes those fields. A Halfling Horse seat applies approximately (0, 0.445, -0.248) from the Chest but reports (0, 0, 0). A Medium Mammoth also reports attachment calibration despite having no animated saddle.",
      "blocking_rationale": "The new observation gives false measurements for the profiles used as isolation controls. Their actual seat geometry is preserved.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A reported applied basis matches the lease being projected. Profiles without a measured calibration are identified accordingly.",
      "id": "R1-F4",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/KingmakerMountedPairRuntime.cs",
        "line": 1007
      },
      "priority": "P2",
      "requested_correction": "Scope the measured calibration fields to the profiles that populate them, or report the actual fixed basis and an explicit state for profiles without an animated saddle.",
      "requested_evidence": "Check the presentation observation for a settled Medium Horse, a Small fixed-seat profile with a nonzero offset and a Medium Mammoth.",
      "title": "Small and Mammoth diagnostics report calibration values they never measured",
      "trigger": "A presentation observation is captured for a fixed-seat Small rider or a Medium Mammoth."
    },
    {
      "acceptance_ids": [],
      "actual": "LoadedUnit has no references in this candidate. Its original consumer exercised LoadedInstanceRepairLedger in the separate repair area.",
      "blocking_rationale": "The unused test declaration has no effect on the executed checks.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "The shared test file carries the seat case and its dependencies.",
      "id": "R1-F5",
      "location": {
        "file": "tests/MountedStockAttackPolicyTests.cs",
        "line": 510
      },
      "priority": "P3",
      "requested_correction": "Remove the unused LoadedUnit declaration from the PR-3 extraction.",
      "requested_evidence": "The shared-file diff and ordinary focused compile suffice.",
      "title": "The seat extraction includes an unrelated test fixture",
      "trigger": "Reviewing the shared test-file extraction against its PR-3 ownership."
    }
  ],
  "prior_finding_assessments": [],
  "request_id": "kmc-codex-mmr-horse-seat-20261126011011-d3c5-r01-bc619d",
  "round": 1,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The Medium Horse adapter measures a provisional seat, confirms one normalized resting sample and preserves it through later actions. The focused gate demonstrates recovery for the modeled disabled-Animator and cast cases, Chest-height normalization, attachment cleanup and the Shield regression controls.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "N01. Compare visible seating on first mount, remount and mounted reload at matched camera angle, profile and stance, including movement, turning, stopping and unmounted restoration.",
      "N10. Confirm one pair with the correct effects through a real save/load or area/view transition using an existing Horse."
    ],
    "scope_reviewed": "The complete PR-3 MMR-01 diff against main, including its extraction, corrections and offline evidence for S02 and S08. Section 4.2 of the enrolled mission lists two verified historical regression obligations, R1-F12 and R5-F3.",
    "unresolved_questions": [
      "Does the installed rider animation lifecycle guarantee a contributing resting pose before the sampler can confirm a bind pose on attachment or rebind?"
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
