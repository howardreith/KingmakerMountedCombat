<!-- agent-review:v1 enrollment=kmc-codex-mmr-horse-trip-20261125213249-4b8b key=r05-review part=1/1 sha256=2e5c157eba8a9dfb43142343858fda51da5c50e040019a97a6d2978d8566a135 -->
### Codex GPT-6.2 Astra — review — round 5 — candidate `2b59c0e87c`

**Decision:** `changes_required` — The focused gate passed. Claim cleanup can still lose a replacement request, omit its result, or publish repeated interruption records. The new archive staging file can also change before its terminal rename. The requested write-or-flush failure check remains outstanding.

## Continuation for Claude

Round 5 exhausts the recorded PR-4 review allocation. Preserve the stop for the owner's decision. If continuation is authorized, keep the correction within the repair policy, file lifecycle and existing real-folder tests. Keep the verified archive contents protected through publication. Retire a claim only after establishing its identity under the handle that performs retirement. Give replacement and recovered claims the existing interrupted result and a single archive before admitting queued work. Exercise the failure boundaries in the findings below through the focused gate. Preserve the exact horse and fact predicates, the world-write gate, the native removal classification and the Mounted Shield checks. N07 and N10 remain owner playtest observations.

#### Findings

**R4-F3** · P2 · defect · blocks readiness — A failed interruption archive still deletes the replacement claim
- Location: `src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs:576`; acceptance: MMR-06, S06
- Trigger: B replaces A between the first exclusive read and the deleting open, then reading B or committing its interrupted archive fails.
- Expected: A failure while identifying or preserving B leaves its exact bytes discoverable for recovery before C is admitted.
- Actual: Opening B with FileOptions.DeleteOnClose commits to deleting it before ReadAll, Digest or Commit succeeds. If the interrupted archive cannot be published, disposing the handle still deletes B. The next poll can take C with only A's archive present.
- Why it blocks / does not: Cleanup for A can permanently consume B's repair request without a durable disposition.
- Requested correction: Make deletion conditional on verifying the contents of the held file. Leave a replacement recoverable through its own result and archive publication.
- Requested evidence: Replace A with B at BeforeRetire, queue C and obstruct B's interrupted archive. Verify B survives the failure. Exercise same-session and fresh-session recovery, then show B settled before C runs and A removed exactly once.

**R4-F2** · P2 · defect · blocks readiness — A locked claim recovered after restart republishes its interrupted archive
- Location: `src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs:132`; acceptance: MMR-06, S06
- Trigger: A fresh session finds a readable .claimed file whose existing reader prevents exclusive retirement.
- Expected: After the first interrupted archive commits, subsequent polls retry only retirement.
- Actual: The unowned-claim branch clears the session and calls Close without passing an archive record. Each poll writes the interrupted result and commits another archive before the exclusive open fails again.
- Why it blocks / does not: A single stranded request can create a new terminal file every two seconds for the duration of the lock.
- Requested correction: Retain the interrupted decision and committed archive identity across polls, including when the current session first discovers an unowned claim.
- Requested evidence: Start a fresh session with a stranded .claimed file held with FileShare.Read and a newer request waiting. Poll repeatedly, release the handle and verify one interrupted archive, zero removals for the stranded claim and normal processing of the waiting request.

**R5-F1** · P2 · defect · blocks readiness — The pending archive can change between flush and publication
- Location: `src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs:602`; acceptance: MMR-06, S06
- Trigger: A writer changes the completed .pending file after its stream closes and before the terminal move.
- Expected: The terminal rename publishes the exact payload bound to A's decision, or fails with A's claim available for retry.
- Actual: Commit closes the exclusive stream after Flush(true), then File.Move resolves the pending filename again. A change to that file in the interval publishes the changed contents under A's terminal suffix. The existing replacement tests edit .claimed and leave this new source untouched.
- Why it blocks / does not: An applied archive can identify a different request from the one whose horse was changed.
- Requested correction: Keep the completed staging file's identity and contents protected through terminal publication.
- Requested evidence: At BeforeCommit, attempt to edit or replace the actual .pending file. Verify the change is excluded or publication stops safely, then check the final archive byte-for-byte against A and confirm one removal.

**R5-F2** · P2 · defect · blocks readiness — A replacement archived during retirement receives no owner-facing result
- Location: `src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs:579`; acceptance: MMR-06, S06
- Trigger: The final deleting open finds B and the inline interrupted archive succeeds.
- Expected: B receives an interrupted result naming B and explaining how to retry, consistent with the other replacement-claim path.
- Actual: The late-mismatch branch commits B's bytes directly with InterruptedSuffix and returns to A's publication. The result file still says Applied for A. Once B is deleted, no later poll can report its interruption.
- Why it blocks / does not: The owner loses the result explaining that B was untouched and requires a new request, even when all file operations succeed.
- Requested correction: Route B through the policy's interrupted reporting path so its result and archive are both completed.
- Requested evidence: Extend the successful BeforeRetire replacement case to inspect the result before a queued C runs. Verify it names B as interrupted, A's applied archive still holds A, and B's fact remains present.

**R4-F1** · P2 · evidence_gap · blocks readiness — The archive test faults the rename after a successful flush
- Location: `tests/HorseTripRepairPolicyTests.cs:753`; acceptance: MMR-06, S06
- Trigger: Assessing the requested R4-F1 evidence for a partial write or failed flush.
- Expected: A failure during private-payload write or flush leaves no partial terminal archive, and retry produces exactly one complete archive.
- Actual: FailedCommitLeavesNoPartialArchive creates a directory at the terminal name after the private write and flush have succeeded. It exercises a failed move and a successful retry. It never faults the write or flush requested in R4-F1.
- Why it blocks / does not: The source addresses partial publication on this path, but the requested failure observation is absent.
- Requested correction: Exercise a failure after private file creation during the payload write or flush. Retain the existing final-move obstruction case.
- Requested evidence: Record the failed poll with the claim present and no terminal archive. After retry, assert one terminal archive with the original raw bytes and one removal in total.

#### Prior finding assessments

- **R3-F1** — `fix_verified` — MessageOf contains the throwing Message getter on the native and feature-read failure paths. The recorded case applied once, archived A's request and preserved the reloaded fact across subsequent same-session and fresh-session polls.
- **R3-F2** — `fix_verified` — The settlement rename that stranded a claim under an undiscoverable name is gone. The recorded same-session and fresh-session cases found B at .claimed and settled it before C. R4-F3 covers the separate loss during the later deleting open.
- **R4-F1** — `fix_rejected` — The private-file implementation puts the terminal move after a successful write, flush and close. The remaining gap is evidence. The recorded test obstructed the move after a complete flush.
- **R4-F2** — `fix_rejected` — The applied-claim sharing test passed with one archive. Interrupted recovery bypasses the new session record, so the same denied retirement still republishes that claim on every poll.
- **R4-F3** — `fix_rejected` — The late-replacement test preserved B when its interrupted commit succeeded. DeleteOnClose still disposes of B if that commit throws, before B has a recoverable disposition.

#### Evidence assessment

- **Inspected:** The enrolled mission, repository validation policy and active handoffs, controller inputs, prior findings, developer report, supplemental records, full feature diff and correction since fd734bc2.; Horse construction, candidate provenance, composition-root gates, both production Close callers, removal classification, archive commit and retirement, and the assertions behind the recorded tests.
- **Executed verification:** Confirmed HEAD, merge base and mission SHA-256. The supplied feature diff matches Git across 19 files and 3,694 lines. The gate log SHA-256 matches the controller receipt.; The controller ran Test-Tiers.ps1 -Tier Fast -Focused Test-MmrHorseTripFocused.ps1 on H. It exited 0 in 33.7 seconds with components 1551/0, Validate-Source 153/0, Trip contract 51/0 and all 12 focused children passing.; The gate recorded the throwing exception-message case, terminal refusal and supersession retries, blocked retirement of an applied claim, and successful archival of a late replacement. The fixtures used the production policy and file implementation on real temporary directories.; Mounted Shield contract checks passed 114/0 and selection rollback passed 68/0. The embedded icon matched the supplied and production asset SHA-256. Loader binding checks passed for UMM 0.31.1 and 0.33.0.
- **Source inspection only:** The loss on a failed late-replacement commit, repeated interrupted archives, missing result for B and mutable pending-file interval follow from the production control flow. The recorded cases do not exercise those outcomes.; Commit writes and flushes the private payload before moving it. The recorded R4-F1 obstruction occurs after both operations succeed.
- **Required gaps:** Observe recovery with B substituted during retirement, B's interrupted publication failing, and C queued. Also verify B's owner-facing result on successful recovery.; Verify one interrupted archive across repeated polls of a locked claim recovered by a fresh session.; Exercise a change to the completed pending archive before its rename and an actual private-payload write or flush failure before retry.
- **Limitations:** This was a read-only review. The controller supplied the candidate-bound receipt and log. The mixed foreground transcript was withheld, so the developer's additional local run was not independently confirmed.; The gate reports nativeProof=false. Installed data, compiled contracts and component fixtures establish the offline behavior. Live bite damage, maneuver delivery and save/reload behavior await owner observation.

#### Summary

- **Delivered behavior:** Fresh Horse construction filters the stock Mammoth Bite-to-Trip grant and retains the bite and two hooves. Existing horses require an explicit request for one eligible fact on the named horse. The focused evidence supports those boundaries, terminal refusal handling and the Mounted Shield regression checks. Repair-file finalization still needs correction.
- **Scope reviewed:** Final PR-4 checkpoint for MMR-06, S06 and S08, including the five pending findings and the production paths affected by the round-5 correction.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** Decide whether to authorize further PR-4 work after the recorded review limit.; N07 covers a successful plain-horse bite and damage without automatic Trip, alongside legitimate maneuver controls where available.; N10 covers an existing horse through reload or an area transition, with a single usable pair, correct effects and no extra action.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a2190a-47d6-7f51-bbdd-a0514e28650a`, duration 623.7s, response sha256 `3eb697a03aa0a86ae53924039c3675cc63976f90a62fdb9b090d6956f426eaa9`, request `kmc-codex-mmr-horse-trip-20261125213249-4b8b-r05-a065a2`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "2b59c0e87c7f6ce8ea04f38373bd978334268963",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The focused gate passed. Claim cleanup can still lose a replacement request, omit its result, or publish repeated interruption records. The new archive staging file can also change before its terminal rename. The requested write-or-flush failure check remains outstanding.\n\n## Continuation for Claude\n\nRound 5 exhausts the recorded PR-4 review allocation. Preserve the stop for the owner's decision. If continuation is authorized, keep the correction within the repair policy, file lifecycle and existing real-folder tests. Keep the verified archive contents protected through publication. Retire a claim only after establishing its identity under the handle that performs retirement. Give replacement and recovered claims the existing interrupted result and a single archive before admitting queued work. Exercise the failure boundaries in the findings below through the focused gate. Preserve the exact horse and fact predicates, the world-write gate, the native removal classification and the Mounted Shield checks. N07 and N10 remain owner playtest observations.",
  "evidence_assessment": {
    "executed_verification": [
      "Confirmed HEAD, merge base and mission SHA-256. The supplied feature diff matches Git across 19 files and 3,694 lines. The gate log SHA-256 matches the controller receipt.",
      "The controller ran Test-Tiers.ps1 -Tier Fast -Focused Test-MmrHorseTripFocused.ps1 on H. It exited 0 in 33.7 seconds with components 1551/0, Validate-Source 153/0, Trip contract 51/0 and all 12 focused children passing.",
      "The gate recorded the throwing exception-message case, terminal refusal and supersession retries, blocked retirement of an applied claim, and successful archival of a late replacement. The fixtures used the production policy and file implementation on real temporary directories.",
      "Mounted Shield contract checks passed 114/0 and selection rollback passed 68/0. The embedded icon matched the supplied and production asset SHA-256. Loader binding checks passed for UMM 0.31.1 and 0.33.0."
    ],
    "inspected": [
      "The enrolled mission, repository validation policy and active handoffs, controller inputs, prior findings, developer report, supplemental records, full feature diff and correction since fd734bc2.",
      "Horse construction, candidate provenance, composition-root gates, both production Close callers, removal classification, archive commit and retirement, and the assertions behind the recorded tests."
    ],
    "limitations": [
      "This was a read-only review. The controller supplied the candidate-bound receipt and log. The mixed foreground transcript was withheld, so the developer's additional local run was not independently confirmed.",
      "The gate reports nativeProof=false. Installed data, compiled contracts and component fixtures establish the offline behavior. Live bite damage, maneuver delivery and save/reload behavior await owner observation."
    ],
    "required_gaps": [
      "Observe recovery with B substituted during retirement, B's interrupted publication failing, and C queued. Also verify B's owner-facing result on successful recovery.",
      "Verify one interrupted archive across repeated polls of a locked claim recovered by a fresh session.",
      "Exercise a change to the completed pending archive before its rename and an actual private-payload write or flush failure before retry."
    ],
    "source_inspection_only": [
      "The loss on a failed late-replacement commit, repeated interrupted archives, missing result for B and mutable pending-file interval follow from the production control flow. The recorded cases do not exercise those outcomes.",
      "Commit writes and flushes the private payload before moving it. The recorded R4-F1 obstruction occurs after both operations succeed."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "Opening B with FileOptions.DeleteOnClose commits to deleting it before ReadAll, Digest or Commit succeeds. If the interrupted archive cannot be published, disposing the handle still deletes B. The next poll can take C with only A's archive present.",
      "blocking_rationale": "Cleanup for A can permanently consume B's repair request without a durable disposition.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A failure while identifying or preserving B leaves its exact bytes discoverable for recovery before C is admitted.",
      "id": "R4-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs",
        "line": 576
      },
      "priority": "P2",
      "requested_correction": "Make deletion conditional on verifying the contents of the held file. Leave a replacement recoverable through its own result and archive publication.",
      "requested_evidence": "Replace A with B at BeforeRetire, queue C and obstruct B's interrupted archive. Verify B survives the failure. Exercise same-session and fresh-session recovery, then show B settled before C runs and A removed exactly once.",
      "title": "A failed interruption archive still deletes the replacement claim",
      "trigger": "B replaces A between the first exclusive read and the deleting open, then reading B or committing its interrupted archive fails."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "The unowned-claim branch clears the session and calls Close without passing an archive record. Each poll writes the interrupted result and commits another archive before the exclusive open fails again.",
      "blocking_rationale": "A single stranded request can create a new terminal file every two seconds for the duration of the lock.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "After the first interrupted archive commits, subsequent polls retry only retirement.",
      "id": "R4-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs",
        "line": 132
      },
      "priority": "P2",
      "requested_correction": "Retain the interrupted decision and committed archive identity across polls, including when the current session first discovers an unowned claim.",
      "requested_evidence": "Start a fresh session with a stranded .claimed file held with FileShare.Read and a newer request waiting. Poll repeatedly, release the handle and verify one interrupted archive, zero removals for the stranded claim and normal processing of the waiting request.",
      "title": "A locked claim recovered after restart republishes its interrupted archive",
      "trigger": "A fresh session finds a readable .claimed file whose existing reader prevents exclusive retirement."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "Commit closes the exclusive stream after Flush(true), then File.Move resolves the pending filename again. A change to that file in the interval publishes the changed contents under A's terminal suffix. The existing replacement tests edit .claimed and leave this new source untouched.",
      "blocking_rationale": "An applied archive can identify a different request from the one whose horse was changed.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The terminal rename publishes the exact payload bound to A's decision, or fails with A's claim available for retry.",
      "id": "R5-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs",
        "line": 602
      },
      "priority": "P2",
      "requested_correction": "Keep the completed staging file's identity and contents protected through terminal publication.",
      "requested_evidence": "At BeforeCommit, attempt to edit or replace the actual .pending file. Verify the change is excluded or publication stops safely, then check the final archive byte-for-byte against A and confirm one removal.",
      "title": "The pending archive can change between flush and publication",
      "trigger": "A writer changes the completed .pending file after its stream closes and before the terminal move."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "The late-mismatch branch commits B's bytes directly with InterruptedSuffix and returns to A's publication. The result file still says Applied for A. Once B is deleted, no later poll can report its interruption.",
      "blocking_rationale": "The owner loses the result explaining that B was untouched and requires a new request, even when all file operations succeed.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "B receives an interrupted result naming B and explaining how to retry, consistent with the other replacement-claim path.",
      "id": "R5-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs",
        "line": 579
      },
      "priority": "P2",
      "requested_correction": "Route B through the policy's interrupted reporting path so its result and archive are both completed.",
      "requested_evidence": "Extend the successful BeforeRetire replacement case to inspect the result before a queued C runs. Verify it names B as interrupted, A's applied archive still holds A, and B's fact remains present.",
      "title": "A replacement archived during retirement receives no owner-facing result",
      "trigger": "The final deleting open finds B and the inline interrupted archive succeeds."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "FailedCommitLeavesNoPartialArchive creates a directory at the terminal name after the private write and flush have succeeded. It exercises a failed move and a successful retry. It never faults the write or flush requested in R4-F1.",
      "blocking_rationale": "The source addresses partial publication on this path, but the requested failure observation is absent.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "A failure during private-payload write or flush leaves no partial terminal archive, and retry produces exactly one complete archive.",
      "id": "R4-F1",
      "location": {
        "file": "tests/HorseTripRepairPolicyTests.cs",
        "line": 753
      },
      "priority": "P2",
      "requested_correction": "Exercise a failure after private file creation during the payload write or flush. Retain the existing final-move obstruction case.",
      "requested_evidence": "Record the failed poll with the claim present and no terminal archive. After retry, assert one terminal archive with the original raw bytes and one removal in total.",
      "title": "The archive test faults the rename after a successful flush",
      "trigger": "Assessing the requested R4-F1 evidence for a partial write or failed flush."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R3-F1",
      "rationale": "MessageOf contains the throwing Message getter on the native and feature-read failure paths. The recorded case applied once, archived A's request and preserved the reloaded fact across subsequent same-session and fresh-session polls."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R3-F2",
      "rationale": "The settlement rename that stranded a claim under an undiscoverable name is gone. The recorded same-session and fresh-session cases found B at .claimed and settled it before C. R4-F3 covers the separate loss during the later deleting open."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R4-F1",
      "rationale": "The private-file implementation puts the terminal move after a successful write, flush and close. The remaining gap is evidence. The recorded test obstructed the move after a complete flush."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R4-F2",
      "rationale": "The applied-claim sharing test passed with one archive. Interrupted recovery bypasses the new session record, so the same denied retirement still republishes that claim on every poll."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R4-F3",
      "rationale": "The late-replacement test preserved B when its interrupted commit succeeded. DeleteOnClose still disposes of B if that commit throws, before B has a recoverable disposition."
    }
  ],
  "request_id": "kmc-codex-mmr-horse-trip-20261125213249-4b8b-r05-a065a2",
  "round": 5,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "Fresh Horse construction filters the stock Mammoth Bite-to-Trip grant and retains the bite and two hooves. Existing horses require an explicit request for one eligible fact on the named horse. The focused evidence supports those boundaries, terminal refusal handling and the Mounted Shield regression checks. Repair-file finalization still needs correction.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "Decide whether to authorize further PR-4 work after the recorded review limit.",
      "N07 covers a successful plain-horse bite and damage without automatic Trip, alongside legitimate maneuver controls where available.",
      "N10 covers an existing horse through reload or an area transition, with a single usable pair, correct effects and no extra action."
    ],
    "scope_reviewed": "Final PR-4 checkpoint for MMR-06, S06 and S08, including the five pending findings and the production paths affected by the round-5 correction.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
