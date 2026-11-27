<!-- agent-review:v1 enrollment=kmc-codex-mmr-horse-trip-20261125213249-4b8b key=r04-review part=1/1 sha256=dcea1c4eaba23f8c7122fcd8588a6866f1cbced9bf74caf6e8314857ff329e90 -->
### Codex GPT-6.2 Astra — review — round 4 — candidate `fd734bc2df`

**Decision:** `changes_required` — The focused gate passed on the candidate. The fresh-horse filter and Mounted Shield checks satisfy their offline scope. Existing-save repair still has failure paths that can reopen a verified removal, lose a replacement request, or leave partial or duplicate terminal archives.

## Continuation for Claude

Keep the correction within the existing MMR-06 repair service and its focused tests. Return the removal classification before formatting exception details. Publish the complete verified request bytes once, retain enough information to resume claim cleanup after publication, and protect replacement bytes through the final cleanup operation. Preserve exact horse and fact selection, the write gate, guarded-folder isolation, and the ability to submit a genuinely new request with identical text after reload.

Exercise the failure boundaries in the findings using the production policy and real temporary files. Check archive contents and counts, which request runs next, and the total number of removals. Run the required focused gate on the corrected candidate within the existing limits. N07 and N10 remain owner playtest observations.

#### Findings

**R3-F1** · P1 · defect · blocks readiness — Formatting a native exception can still reopen a verified removal
- Location: `src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs:399`; acceptance: MMR-06, S06
- Trigger: Native removal succeeds and then throws an exception whose Message getter fails during diagnostic formatting.
- Expected: Once the post-state establishes exact removal, the policy receives that outcome even if diagnostic text cannot be produced.
- Actual: HorseTripRemoval.RemoveExactly evaluates Classify, then calls Native(nativeFailure) while constructing the result detail. Native reads failure.Message outside the ReportRemoval guard. If that getter throws, the claim remains Undecided. The next poll can refuse because the fact is gone, or remove a restored fact after reload.
- Why it blocks / does not: A diagnostic failure can still turn one verified removal into a false refusal or a second removal under the same owner request.
- Requested correction: Decouple the classified outcome from exception formatting. Give unreadable diagnostic details a safe fallback, including on the post-removal read-failure branch.
- Requested evidence: Have the native boundary remove exactly the target and throw an exception whose Message getter throws. Exercise the production classification and reporting path, reload the same horse with one candidate, and verify an applied result, the original archive bytes, one native removal in total and preservation of the reloaded fact.

**R3-F2** · P2 · defect · blocks readiness — A failure after the settlement rename still strands a claim
- Location: `src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs:553`; acceptance: MMR-06, S06
- Trigger: The move to .settling succeeds and the next read fails, or the process stops before the file is returned or retired.
- Expected: Both a retry and a fresh session can discover and settle bytes moved aside during finalization before admitting newer work.
- Actual: Close moves the current claim to a timestamped .settling name. If its subsequent read or return move fails, Process checks only .claimed, clears the session and can take the next request. If B replaced A during publication, A already has its archive, but B remains stranded under .settling.
- Why it blocks / does not: A pending replacement can be skipped indefinitely while a later request changes another horse.
- Requested correction: Keep the file being settled discoverable through completion, including recovery after the rename. Resolve its bound or replacement bytes before processing the waiting request.
- Requested evidence: Replace A's claim with B before settlement, queue C, and interrupt immediately after the move to .settling. Exercise same-session retry and fresh-session recovery. Verify B receives its own disposition before C runs and A is removed only once.

**R4-F1** · P2 · defect · blocks readiness — A failed write can leave a partial terminal archive
- Location: `src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs:549`; acceptance: MMR-06, S06
- Trigger: The filesystem creates the archive but a quota, I/O error or interruption prevents the full payload from being flushed.
- Expected: A terminal filename becomes visible as a completed record only when it contains the full verified payload.
- Actual: FileMode.CreateNew creates the final .applied or other terminal name before Write and Dispose complete. A write or flush failure can leave an empty or partial terminal file. ArchiveTarget sees it on retry and selects a timestamped filename, leaving the incomplete record in place.
- Why it blocks / does not: A terminal archive can permanently contain bytes that do not identify the request that received the decision.
- Requested correction: Complete the verified payload in a private file and commit it atomically to a non-overwriting terminal name while preserving its identity.
- Requested evidence: Fail the archive write or flush after file creation, then retry. Verify that no incomplete terminal file survives and exactly one terminal archive contains the original bytes.

**R4-F2** · P2 · defect · blocks readiness — A blocked settlement rename republishes an already completed archive
- Location: `src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs:551`; acceptance: MMR-06, S06
- Trigger: A reader allows the claim to be read but denies delete sharing when Close tries to move it after publishing the archive.
- Expected: After successful archive publication, retries perform only the outstanding claim cleanup.
- Actual: The archive has already been written when the settlement rename fails. The session remains Reported, so every poll calls Close again and writes another timestamped archive before retrying the same rename.
- Why it blocks / does not: One owner request can produce an unbounded series of applied records while the claim remains locked.
- Requested correction: Retain the committed archive identity for this request across settlement retries. Distinguish those retries from a later, separately submitted request with identical bytes.
- Requested evidence: Hold .claimed open with FileShare.Read so verification succeeds and the rename fails. Poll twice, release the handle and finish. Verify a single complete archive and one removal for A, followed by normal processing of a waiting request.

**R4-F3** · P2 · defect · blocks readiness — Claim cleanup can delete replacement bytes after its last check
- Location: `src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs:556`; acceptance: MMR-06, S06
- Trigger: A writer edits the moved file, directly or through another name for it, after the settlement read captured A and before deletion.
- Expected: Retirement removes only the exact captured contents. Replacement bytes remain available for their own disposition.
- Actual: File.ReadAllBytes closes the settling file before the digest comparison and File.Delete. A writer can change the file in that interval. The delete then removes B's request from the repair workflow although B never received a disposition.
- Why it blocks / does not: Cleanup of a correctly archived request can silently discard a different owner's repair request.
- Requested correction: Protect the captured file through the compare-and-retire operation, or preserve it for conservative settlement whenever its identity cannot be guaranteed.
- Requested evidence: Change the settling file to B after its final read and before deletion. Verify A's archive still contains A, B receives its own disposition and only A's removal ran.

#### Prior finding assessments

- **R1-F4** — `fix_verified` — The archive write now uses the verified byte array. The recorded replacement cases show that changing the source after verification no longer puts B's bytes in A's applied archive. R4-F1 concerns an incomplete output write and R4-F3 concerns a later deletion after publication.
- **R3-F1** — `fix_rejected` — The display-name and logger exception path is contained, and the throwing-reporter test preserved the reloaded fact. The production classifier still formats a native exception after establishing the outcome and before returning it to the session.
- **R3-F2** — `fix_rejected` — The directory-obstruction test verifies recovery before archive creation. The subsequent move to .settling still puts live request bytes at a name neither the current poller nor a fresh session recovers.

#### Evidence assessment

- **Inspected:** AGENTS.md, validation policy, current handoff, enrolled mission, full feature diff, prior findings, developer report, transcript notice, and controller gate receipt and log.; The round-3-to-round-4 changes in HorseTripRepairPolicy, TripRepairFileHost, the component fixtures and the compiled contract checks, together with their construction, polling and persistence callers.
- **Executed verification:** Confirmed the detached HEAD, merge base, enrolled mission SHA-256, and gate log SHA-256 against the controller inputs.; The controller receipt records Test-Tiers.ps1 -Tier Fast -Focused Test-MmrHorseTripFocused.ps1 at fd734bc2, exit 0 in 33.5 seconds. The build passed, components passed 1547/1547, Validate-Source passed 153/153, and all 12 focused children passed.; The log records the installed Mammoth, Dog and Wolf fact checks, the compiled Horse body with one bite and two hooves, and 51/51 Trip contract checks. Mounted Shield passed 114/114, including byte identity of the embedded icon. The rollback, selection and loader neighbors also passed.; The component run exercised a throwing test-host reporter, an archive filename obstructed by a directory, and claim replacement before archive creation and before settlement. It also retained the earlier frozen-refusal and supersession cases.
- **Source inspection only:** The production display-name and logger call are inside the guarded ReportRemoval invocation. The throwing reporter executed by DiagnosticFailureAfterRemoval belongs to the test host.; The exception-formatting, partial-write, repeated-publication and post-rename cleanup paths below follow from the candidate source. The supplied gate did not execute those failure cases.
- **Required gaps:** Exercise a diagnostic formatting failure inside the removal path and verify one removal across reload.; Exercise failure after archive creation, failure on both sides of the settlement rename, and replacement after the final settlement read. Verify complete archive bytes, one terminal archive per decided request, and conservative recovery before newer work.
- **Limitations:** The reviewer used read-only inspection. The controller withheld the mixed foreground transcript, so the additional developer-reported local runs could not be independently confirmed.; The gate reports nativeProof=false. Live bite damage, maneuver delivery and save/reload behavior remain unobserved on this candidate.

#### Summary

- **Delivered behavior:** The candidate isolates the intrinsic Trip source to the stock Mammoth donor's TrippingBite fact and filters its exact single-component Bite-to-Trip shape during Horse construction. The offline checks preserve the bite and both hooves, foreign effects and Mounted Shield icon bytes. Existing horses require an explicit request naming the horse before repair.
- **Scope reviewed:** Final PR-4 source and offline acceptance for MMR-06, S06 and S08, with correction review focused on removal diagnostics, claim publication, recovery and their direct consumers.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** N07: Observe a plain horse's successful bite and damage without automatic Trip, alongside a legitimate dog, wolf or explicitly granted maneuver where available.; N10: Check a pre-repair horse across save/reload or an area transition, including pair identity, shield effects and action state.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a218fc-2d61-7ca2-a01c-0b2314ad3e96`, duration 602.7s, response sha256 `3812051dad5c5891125e403ee215bc858b37a7fae03df457fe42805fa551e257`, request `kmc-codex-mmr-horse-trip-20261125213249-4b8b-r04-1e1f3d`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "fd734bc2df4a47eef36fd8ae298a1b41f1ef734c",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The focused gate passed on the candidate. The fresh-horse filter and Mounted Shield checks satisfy their offline scope. Existing-save repair still has failure paths that can reopen a verified removal, lose a replacement request, or leave partial or duplicate terminal archives.\n\n## Continuation for Claude\n\nKeep the correction within the existing MMR-06 repair service and its focused tests. Return the removal classification before formatting exception details. Publish the complete verified request bytes once, retain enough information to resume claim cleanup after publication, and protect replacement bytes through the final cleanup operation. Preserve exact horse and fact selection, the write gate, guarded-folder isolation, and the ability to submit a genuinely new request with identical text after reload.\n\nExercise the failure boundaries in the findings using the production policy and real temporary files. Check archive contents and counts, which request runs next, and the total number of removals. Run the required focused gate on the corrected candidate within the existing limits. N07 and N10 remain owner playtest observations.",
  "evidence_assessment": {
    "executed_verification": [
      "Confirmed the detached HEAD, merge base, enrolled mission SHA-256, and gate log SHA-256 against the controller inputs.",
      "The controller receipt records Test-Tiers.ps1 -Tier Fast -Focused Test-MmrHorseTripFocused.ps1 at fd734bc2, exit 0 in 33.5 seconds. The build passed, components passed 1547/1547, Validate-Source passed 153/153, and all 12 focused children passed.",
      "The log records the installed Mammoth, Dog and Wolf fact checks, the compiled Horse body with one bite and two hooves, and 51/51 Trip contract checks. Mounted Shield passed 114/114, including byte identity of the embedded icon. The rollback, selection and loader neighbors also passed.",
      "The component run exercised a throwing test-host reporter, an archive filename obstructed by a directory, and claim replacement before archive creation and before settlement. It also retained the earlier frozen-refusal and supersession cases."
    ],
    "inspected": [
      "AGENTS.md, validation policy, current handoff, enrolled mission, full feature diff, prior findings, developer report, transcript notice, and controller gate receipt and log.",
      "The round-3-to-round-4 changes in HorseTripRepairPolicy, TripRepairFileHost, the component fixtures and the compiled contract checks, together with their construction, polling and persistence callers."
    ],
    "limitations": [
      "The reviewer used read-only inspection. The controller withheld the mixed foreground transcript, so the additional developer-reported local runs could not be independently confirmed.",
      "The gate reports nativeProof=false. Live bite damage, maneuver delivery and save/reload behavior remain unobserved on this candidate."
    ],
    "required_gaps": [
      "Exercise a diagnostic formatting failure inside the removal path and verify one removal across reload.",
      "Exercise failure after archive creation, failure on both sides of the settlement rename, and replacement after the final settlement read. Verify complete archive bytes, one terminal archive per decided request, and conservative recovery before newer work."
    ],
    "source_inspection_only": [
      "The production display-name and logger call are inside the guarded ReportRemoval invocation. The throwing reporter executed by DiagnosticFailureAfterRemoval belongs to the test host.",
      "The exception-formatting, partial-write, repeated-publication and post-rename cleanup paths below follow from the candidate source. The supplied gate did not execute those failure cases."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "HorseTripRemoval.RemoveExactly evaluates Classify, then calls Native(nativeFailure) while constructing the result detail. Native reads failure.Message outside the ReportRemoval guard. If that getter throws, the claim remains Undecided. The next poll can refuse because the fact is gone, or remove a restored fact after reload.",
      "blocking_rationale": "A diagnostic failure can still turn one verified removal into a false refusal or a second removal under the same owner request.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Once the post-state establishes exact removal, the policy receives that outcome even if diagnostic text cannot be produced.",
      "id": "R3-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs",
        "line": 399
      },
      "priority": "P1",
      "requested_correction": "Decouple the classified outcome from exception formatting. Give unreadable diagnostic details a safe fallback, including on the post-removal read-failure branch.",
      "requested_evidence": "Have the native boundary remove exactly the target and throw an exception whose Message getter throws. Exercise the production classification and reporting path, reload the same horse with one candidate, and verify an applied result, the original archive bytes, one native removal in total and preservation of the reloaded fact.",
      "title": "Formatting a native exception can still reopen a verified removal",
      "trigger": "Native removal succeeds and then throws an exception whose Message getter fails during diagnostic formatting."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "Close moves the current claim to a timestamped .settling name. If its subsequent read or return move fails, Process checks only .claimed, clears the session and can take the next request. If B replaced A during publication, A already has its archive, but B remains stranded under .settling.",
      "blocking_rationale": "A pending replacement can be skipped indefinitely while a later request changes another horse.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Both a retry and a fresh session can discover and settle bytes moved aside during finalization before admitting newer work.",
      "id": "R3-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs",
        "line": 553
      },
      "priority": "P2",
      "requested_correction": "Keep the file being settled discoverable through completion, including recovery after the rename. Resolve its bound or replacement bytes before processing the waiting request.",
      "requested_evidence": "Replace A's claim with B before settlement, queue C, and interrupt immediately after the move to .settling. Exercise same-session retry and fresh-session recovery. Verify B receives its own disposition before C runs and A is removed only once.",
      "title": "A failure after the settlement rename still strands a claim",
      "trigger": "The move to .settling succeeds and the next read fails, or the process stops before the file is returned or retired."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "FileMode.CreateNew creates the final .applied or other terminal name before Write and Dispose complete. A write or flush failure can leave an empty or partial terminal file. ArchiveTarget sees it on retry and selects a timestamped filename, leaving the incomplete record in place.",
      "blocking_rationale": "A terminal archive can permanently contain bytes that do not identify the request that received the decision.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A terminal filename becomes visible as a completed record only when it contains the full verified payload.",
      "id": "R4-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs",
        "line": 549
      },
      "priority": "P2",
      "requested_correction": "Complete the verified payload in a private file and commit it atomically to a non-overwriting terminal name while preserving its identity.",
      "requested_evidence": "Fail the archive write or flush after file creation, then retry. Verify that no incomplete terminal file survives and exactly one terminal archive contains the original bytes.",
      "title": "A failed write can leave a partial terminal archive",
      "trigger": "The filesystem creates the archive but a quota, I/O error or interruption prevents the full payload from being flushed."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "The archive has already been written when the settlement rename fails. The session remains Reported, so every poll calls Close again and writes another timestamped archive before retrying the same rename.",
      "blocking_rationale": "One owner request can produce an unbounded series of applied records while the claim remains locked.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "After successful archive publication, retries perform only the outstanding claim cleanup.",
      "id": "R4-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs",
        "line": 551
      },
      "priority": "P2",
      "requested_correction": "Retain the committed archive identity for this request across settlement retries. Distinguish those retries from a later, separately submitted request with identical bytes.",
      "requested_evidence": "Hold .claimed open with FileShare.Read so verification succeeds and the rename fails. Poll twice, release the handle and finish. Verify a single complete archive and one removal for A, followed by normal processing of a waiting request.",
      "title": "A blocked settlement rename republishes an already completed archive",
      "trigger": "A reader allows the claim to be read but denies delete sharing when Close tries to move it after publishing the archive."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "File.ReadAllBytes closes the settling file before the digest comparison and File.Delete. A writer can change the file in that interval. The delete then removes B's request from the repair workflow although B never received a disposition.",
      "blocking_rationale": "Cleanup of a correctly archived request can silently discard a different owner's repair request.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Retirement removes only the exact captured contents. Replacement bytes remain available for their own disposition.",
      "id": "R4-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs",
        "line": 556
      },
      "priority": "P2",
      "requested_correction": "Protect the captured file through the compare-and-retire operation, or preserve it for conservative settlement whenever its identity cannot be guaranteed.",
      "requested_evidence": "Change the settling file to B after its final read and before deletion. Verify A's archive still contains A, B receives its own disposition and only A's removal ran.",
      "title": "Claim cleanup can delete replacement bytes after its last check",
      "trigger": "A writer edits the moved file, directly or through another name for it, after the settlement read captured A and before deletion."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F4",
      "rationale": "The archive write now uses the verified byte array. The recorded replacement cases show that changing the source after verification no longer puts B's bytes in A's applied archive. R4-F1 concerns an incomplete output write and R4-F3 concerns a later deletion after publication."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R3-F1",
      "rationale": "The display-name and logger exception path is contained, and the throwing-reporter test preserved the reloaded fact. The production classifier still formats a native exception after establishing the outcome and before returning it to the session."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R3-F2",
      "rationale": "The directory-obstruction test verifies recovery before archive creation. The subsequent move to .settling still puts live request bytes at a name neither the current poller nor a fresh session recovers."
    }
  ],
  "request_id": "kmc-codex-mmr-horse-trip-20261125213249-4b8b-r04-1e1f3d",
  "round": 4,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The candidate isolates the intrinsic Trip source to the stock Mammoth donor's TrippingBite fact and filters its exact single-component Bite-to-Trip shape during Horse construction. The offline checks preserve the bite and both hooves, foreign effects and Mounted Shield icon bytes. Existing horses require an explicit request naming the horse before repair.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "N07: Observe a plain horse's successful bite and damage without automatic Trip, alongside a legitimate dog, wolf or explicitly granted maneuver where available.",
      "N10: Check a pre-repair horse across save/reload or an area transition, including pair identity, shield effects and action state."
    ],
    "scope_reviewed": "Final PR-4 source and offline acceptance for MMR-06, S06 and S08, with correction review focused on removal diagnostics, claim publication, recovery and their direct consumers.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
