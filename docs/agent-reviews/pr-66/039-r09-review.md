<!-- agent-review:v1 enrollment=kmc-codex-mmr-horse-trip-20261125213249-4b8b key=r09-review part=1/1 sha256=8a5b7d870635119809fe32500c533a97b49897e594a84f4ae37c19f57aa406f5 -->
### Codex GPT-6.2 Astra — review — round 9 — candidate `239264fc76`

**Decision:** `changes_required` — The focused gate passed, and the candidate fixes the reported Windows link refusal and stranded-replacement cases. Saved-horse repair still has paths that can remove a fact from an incomplete request or publish another file’s bytes as the repair record.

## Continuation for Claude

Keep the correction within the repair file operations and their focused fixtures. Bind eligibility and reading to the same open file, exclude active writers from deciding reads, and bind publication to the completed payload on every supported path. Bound staging across retries and recover only identifiable unfinished work. Preserve the verified Windows retirement operation, exact horse and fact selection, and frozen decisions. Exercise the interleavings in the findings, record which filesystem operations actually ran, and run the required focused gate on the corrected candidate. N07 and N10 remain owner playtest observations.

#### Findings

**R5-F1** · P2 · defect · blocks readiness — The Unix fallback can publish a replacement staging file
- Location: `src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs:535`; acceptance: MMR-06, S06
- Trigger: A local save replaces the staging entry after A’s payload is flushed, and publication uses the pathname because /proc is absent or the descriptor link failed.
- Expected: The first readable archive contains the complete bytes that authorized A’s decision. Losing the protected publication source leaves the claim available for retry.
- Actual: After any unsuccessful linkat call, line 535 resolves ownedPath again. If a replacement occupies that name, link publishes the replacement under A’s terminal name and Close can retire A. The same lookup is used directly without /proc. Even when the descriptor link succeeds, an in-place edit to the named staging file on a filesystem with advisory sharing changes the bytes it publishes.
- Why it blocks / does not: An Applied archive can identify a horse other than the one whose fact was removed. The first reader can receive that incorrect record even though publication succeeded.
- Requested correction: Bind every non-overwriting publication path to the completed, protected payload. Preserve recoverability when descriptor-based publication fails, without selecting a substitute file by its staging pathname.
- Requested evidence: Exercise a staging-name replacement and an in-place change at publication, including the alternate path after a descriptor-link failure. Check the first readable bytes, preservation of the replacement, and one exact archive and one removal after retry. Identify the platform or simulated boundary used.

**R9-F1** · P2 · defect · blocks readiness — Windows can authorize removal while the request is still being written
- Location: `src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs:647`; acceptance: MMR-06, S06
- Trigger: A writer permits read and delete sharing and flushes the first horse line of a two-line request before the repair poll runs.
- Expected: The repair waits for the writer to close, then validates the complete request. Ordinary read-only holders remain compatible with the deciding read.
- Actual: ReadEligible passes ShareAll for both request and claim reads. The rename can succeed while the writer holds the file, and DecideClaimed can accept the first line and remove that horse’s fact. The exclusive retirement open occurs after removal.
- Why it blocks / does not: The owner’s completed two-horse request is malformed and authorizes zero removals. Acting on its prefix can change a horse before the owner finishes writing.
- Requested correction: Exclude concurrent writers during deciding Windows reads while preserving the no-follow handle and compatibility with read-only holders.
- Requested evidence: Hold a writable request across a poll after its first line, complete its second horse line, then close it. Verify zero removal calls throughout and a malformed refusal for the completed request. Retain the read-locked regular-claim control.

**R9-F2** · P2 · defect · blocks readiness — The Unix eligibility check and deciding read can refer to different files
- Location: `src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs:746`; acceptance: MMR-06, S06
- Trigger: A regular claim is replaced by a link between its attribute check and its read. The linked file contains request text for a loaded horse.
- Expected: The same open, eligible regular file supplies both the eligibility decision and the bytes used for selection or archive copying.
- Actual: File.GetAttributes checks one directory entry. File.ReadAllBytes then opens the pathname again and can follow a replacement link. On an undecided claim, those target bytes reach candidate selection and removal before Close checks the claim again.
- Why it blocks / does not: An ineligible link can supply the apparent owner authorization for a saved-horse change on the supported Unix path.
- Requested correction: Use a no-follow open on the Unix path, verify regular-file eligibility on that descriptor, and read the deciding bytes through it.
- Requested evidence: Cover substitution at the open boundary for an undecided claim and for recovery. Verify the target supplies neither a removal nor an archive copy, and retain an ordinary-file control.

**R9-F3** · P2 · defect · blocks readiness — Pathname publication leaves a new staging file on every failed retry
- Location: `src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs:947`; acceptance: MMR-06, S06
- Trigger: The claim has a frozen decision and a directory continues to occupy its terminal archive name across multiple polls.
- Expected: Retries use bounded private state and finish with one exact archive when the obstruction clears.
- Actual: Each Commit attempt creates a new .pending file. Disposing the stream leaves that name in place after a write, flush or link failure. A successful retry also leaves its .pending name as a writable hard-link alias of the terminal archive. The existing NoStaging assertion runs on the default Windows path.
- Why it blocks / does not: One persistent archive obstruction causes a new file to accumulate in the player’s data directory on every eligible two-second poll.
- Requested correction: Reclaim or safely reuse owned staging state across failed attempts, and retire the staging alias after successful publication. Keep cleanup tied to the file created by the attempt.
- Requested evidence: Keep the terminal name obstructed across several pathname-branch polls and count the private files. Clear the obstruction and verify one archive, one removal, no surviving staging alias, and preservation of any foreign replacement.

**R9-F4** · P2 · defect · blocks readiness — Recovery treats unrecognized settled files as interrupted requests
- Location: `src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs:885`; acceptance: MMR-06, S06
- Trigger: There is no current claim, but the repair folder contains a completed .claimed.<guid>.settled record or a backup matching .claimed.*.settled.
- Expected: Recovery takes ownership only of identifiable unfinished work. Completed records and unrecognized files retain their names and disposition.
- Actual: RecoverRetiring adds every matching name with the wrong field count or digest length to pending. It then moves that file to .claimed. A fresh session publishes an Interrupted result and archive for it. The preceding implementation itself produced the single-GUID .settled name on successful retirement.
- Why it blocks / does not: An idle poll can move an owner’s backup and overwrite a correct Applied result even though there is no unfinished request.
- Requested correction: Require evidence that a settled entry represents pending work before moving it. Preserve unrecognized filenames and handle the prior completed-record format conservatively.
- Requested evidence: Poll a fresh session with a completed older-format record, its Applied result and archive, and a backup matching the settled glob. Verify their names and bytes stay intact and no Interrupted archive appears. Retain the genuine stranded-replacement cases.

#### Prior finding assessments

- **R4-F3** — `fix_verified` — The new settled name records A’s digest prefix. In all four recorded cases, recovery found B after the failed check or return, produced B’s exact Interrupted archive and named result before C, and preserved B’s fact. Only A and C had removals. R9-F4 concerns the separate treatment of unrecognized names.
- **R5-F1** — `fix_rejected` — The forced Windows pathname fixture showed an absent terminal during the partial write, preservation of the foreign terminal entry, and complete bytes on retry. It exercised CreateHardLinkW. The Unix source still permits publication from a newly resolved staging name or a modified staging inode.
- **R8-F1** — `fix_verified` — On Windows, CreateFileW opens the final entry without following a reparse point. The attribute check precedes reading on that same handle. Actual junctions and simulated file-reparse cases produced zero removals, results or archives. The ordinary request applied and the read-locked regular claim settled after release. R9-F2 concerns the separate Unix implementation.

#### Evidence assessment

- **Inspected:** The enrolled mission, repository validation policy and handoff, feature diff, prior findings, developer dispositions, operation/failure table, controller receipt and gate log.; The horse construction filter, saved-fact predicate, composition-root write gate, repair session, file operations, production polling caller and focused fixtures. I compared the round-9 changes with the preceding candidate.
- **Executed verification:** I verified the snapshot HEAD, the enrolled mission digest, and the controller gate log’s SHA-256 of 8584b0e57b4686692caf71a47c863ebce74db0e80858a1735ae062b80ed068d1.; The controller ran Test-Tiers.ps1 -Tier Fast -Focused Test-MmrHorseTripFocused.ps1 on the candidate. It exited 0 in 42.6 seconds. Components passed 1,563/1,563, Validate-Source passed 153/153, and all 12 focused children passed.; The installed-data and compiled Trip contract passed 52/52. It identified the Mammoth’s stock Bite-to-Trip component and checked the horse’s inherited facts, one bite and two hooves, and exclusions for independently sourced facts. The Mounted Shield contract passed 114/114 and confirmed identical packet, production and embedded icon bytes.; The Windows fixtures exercised two actual junctions, simulated file-reparse classification, a read-locked regular claim, the partial-write archive failure, and both post-settlement failures in the same and a fresh session. The pathname cases ran through ForcePathnameOperations on Windows.
- **Source inspection only:** The Unix link fallback, the separate Unix attribute check and read, Windows write sharing, staging-file retention, and recovery of unrecognized settled names were traced in source. The gate did not execute the failure cases reported below.
- **Required gaps:** Show that an active Windows writer and a substituted Unix link cannot supply bytes that authorize a removal.; Show exact bytes at first archive readability after staging-file edits or replacement, including failure of descriptor-based publication. Account for private files across repeated failures and eventual success.; Show that recovery preserves completed records and unrecognized backups while still settling a genuinely stranded replacement before a queued request.
- **Limitations:** The reviewer performed read-only inspection. The controller withheld the mixed developer-session transcript. Its candidate-bound receipt confirms the focused run, while the separately reported development runs and environment probe have no individual transcript in the supplied evidence.; The gate reports nativeProof=false. It used desktop Windows for filesystem tests. The file-symbolic-link condition was simulated, the directory junctions were real, and neither the Unix system calls nor the Unity Mono player were exercised.

#### Summary

- **Delivered behavior:** Fresh horse construction filters the verified stock TrippingBite grant while preserving the bite, both hooves and other donor facts. Existing-horse repair requires an explicit horse ID and one matching sourceless fact. The remaining defects concern how repair files authorize and record that change.
- **Scope reviewed:** Final PR-4 source/offline checkpoint for MMR-06, S06 and S08, including the round-9 corrections, their callers and the candidate-bound focused evidence.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** N07 — Observe an ordinary horse bite hit and deal damage without intrinsic Trip. Compare legitimate dog, wolf or independently granted maneuvers where available.; N10 — Observe a pre-repair horse through save/reload or an area transition, including continued usability, a single pair, correct effects and ordinary action entitlement.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a21bbf-a413-7070-b7e2-030c8118bd67`, duration 826.6s, response sha256 `feba9467a8d409c64fb36550d88aa25b1a9e24327d9ff76a20789755e9f26e4d`, request `kmc-codex-mmr-horse-trip-20261125213249-4b8b-r09-c0a9c5`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "239264fc7670da9af35ca0d0428dc37073c210b5",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The focused gate passed, and the candidate fixes the reported Windows link refusal and stranded-replacement cases. Saved-horse repair still has paths that can remove a fact from an incomplete request or publish another file\u2019s bytes as the repair record.\n\n## Continuation for Claude\n\nKeep the correction within the repair file operations and their focused fixtures. Bind eligibility and reading to the same open file, exclude active writers from deciding reads, and bind publication to the completed payload on every supported path. Bound staging across retries and recover only identifiable unfinished work. Preserve the verified Windows retirement operation, exact horse and fact selection, and frozen decisions. Exercise the interleavings in the findings, record which filesystem operations actually ran, and run the required focused gate on the corrected candidate. N07 and N10 remain owner playtest observations.",
  "evidence_assessment": {
    "executed_verification": [
      "I verified the snapshot HEAD, the enrolled mission digest, and the controller gate log\u2019s SHA-256 of 8584b0e57b4686692caf71a47c863ebce74db0e80858a1735ae062b80ed068d1.",
      "The controller ran Test-Tiers.ps1 -Tier Fast -Focused Test-MmrHorseTripFocused.ps1 on the candidate. It exited 0 in 42.6 seconds. Components passed 1,563/1,563, Validate-Source passed 153/153, and all 12 focused children passed.",
      "The installed-data and compiled Trip contract passed 52/52. It identified the Mammoth\u2019s stock Bite-to-Trip component and checked the horse\u2019s inherited facts, one bite and two hooves, and exclusions for independently sourced facts. The Mounted Shield contract passed 114/114 and confirmed identical packet, production and embedded icon bytes.",
      "The Windows fixtures exercised two actual junctions, simulated file-reparse classification, a read-locked regular claim, the partial-write archive failure, and both post-settlement failures in the same and a fresh session. The pathname cases ran through ForcePathnameOperations on Windows."
    ],
    "inspected": [
      "The enrolled mission, repository validation policy and handoff, feature diff, prior findings, developer dispositions, operation/failure table, controller receipt and gate log.",
      "The horse construction filter, saved-fact predicate, composition-root write gate, repair session, file operations, production polling caller and focused fixtures. I compared the round-9 changes with the preceding candidate."
    ],
    "limitations": [
      "The reviewer performed read-only inspection. The controller withheld the mixed developer-session transcript. Its candidate-bound receipt confirms the focused run, while the separately reported development runs and environment probe have no individual transcript in the supplied evidence.",
      "The gate reports nativeProof=false. It used desktop Windows for filesystem tests. The file-symbolic-link condition was simulated, the directory junctions were real, and neither the Unix system calls nor the Unity Mono player were exercised."
    ],
    "required_gaps": [
      "Show that an active Windows writer and a substituted Unix link cannot supply bytes that authorize a removal.",
      "Show exact bytes at first archive readability after staging-file edits or replacement, including failure of descriptor-based publication. Account for private files across repeated failures and eventual success.",
      "Show that recovery preserves completed records and unrecognized backups while still settling a genuinely stranded replacement before a queued request."
    ],
    "source_inspection_only": [
      "The Unix link fallback, the separate Unix attribute check and read, Windows write sharing, staging-file retention, and recovery of unrecognized settled names were traced in source. The gate did not execute the failure cases reported below."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "After any unsuccessful linkat call, line 535 resolves ownedPath again. If a replacement occupies that name, link publishes the replacement under A\u2019s terminal name and Close can retire A. The same lookup is used directly without /proc. Even when the descriptor link succeeds, an in-place edit to the named staging file on a filesystem with advisory sharing changes the bytes it publishes.",
      "blocking_rationale": "An Applied archive can identify a horse other than the one whose fact was removed. The first reader can receive that incorrect record even though publication succeeded.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The first readable archive contains the complete bytes that authorized A\u2019s decision. Losing the protected publication source leaves the claim available for retry.",
      "id": "R5-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs",
        "line": 535
      },
      "priority": "P2",
      "requested_correction": "Bind every non-overwriting publication path to the completed, protected payload. Preserve recoverability when descriptor-based publication fails, without selecting a substitute file by its staging pathname.",
      "requested_evidence": "Exercise a staging-name replacement and an in-place change at publication, including the alternate path after a descriptor-link failure. Check the first readable bytes, preservation of the replacement, and one exact archive and one removal after retry. Identify the platform or simulated boundary used.",
      "title": "The Unix fallback can publish a replacement staging file",
      "trigger": "A local save replaces the staging entry after A\u2019s payload is flushed, and publication uses the pathname because /proc is absent or the descriptor link failed."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "ReadEligible passes ShareAll for both request and claim reads. The rename can succeed while the writer holds the file, and DecideClaimed can accept the first line and remove that horse\u2019s fact. The exclusive retirement open occurs after removal.",
      "blocking_rationale": "The owner\u2019s completed two-horse request is malformed and authorizes zero removals. Acting on its prefix can change a horse before the owner finishes writing.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The repair waits for the writer to close, then validates the complete request. Ordinary read-only holders remain compatible with the deciding read.",
      "id": "R9-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs",
        "line": 647
      },
      "priority": "P2",
      "requested_correction": "Exclude concurrent writers during deciding Windows reads while preserving the no-follow handle and compatibility with read-only holders.",
      "requested_evidence": "Hold a writable request across a poll after its first line, complete its second horse line, then close it. Verify zero removal calls throughout and a malformed refusal for the completed request. Retain the read-locked regular-claim control.",
      "title": "Windows can authorize removal while the request is still being written",
      "trigger": "A writer permits read and delete sharing and flushes the first horse line of a two-line request before the repair poll runs."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "File.GetAttributes checks one directory entry. File.ReadAllBytes then opens the pathname again and can follow a replacement link. On an undecided claim, those target bytes reach candidate selection and removal before Close checks the claim again.",
      "blocking_rationale": "An ineligible link can supply the apparent owner authorization for a saved-horse change on the supported Unix path.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The same open, eligible regular file supplies both the eligibility decision and the bytes used for selection or archive copying.",
      "id": "R9-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs",
        "line": 746
      },
      "priority": "P2",
      "requested_correction": "Use a no-follow open on the Unix path, verify regular-file eligibility on that descriptor, and read the deciding bytes through it.",
      "requested_evidence": "Cover substitution at the open boundary for an undecided claim and for recovery. Verify the target supplies neither a removal nor an archive copy, and retain an ordinary-file control.",
      "title": "The Unix eligibility check and deciding read can refer to different files",
      "trigger": "A regular claim is replaced by a link between its attribute check and its read. The linked file contains request text for a loaded horse."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "Each Commit attempt creates a new .pending file. Disposing the stream leaves that name in place after a write, flush or link failure. A successful retry also leaves its .pending name as a writable hard-link alias of the terminal archive. The existing NoStaging assertion runs on the default Windows path.",
      "blocking_rationale": "One persistent archive obstruction causes a new file to accumulate in the player\u2019s data directory on every eligible two-second poll.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Retries use bounded private state and finish with one exact archive when the obstruction clears.",
      "id": "R9-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs",
        "line": 947
      },
      "priority": "P2",
      "requested_correction": "Reclaim or safely reuse owned staging state across failed attempts, and retire the staging alias after successful publication. Keep cleanup tied to the file created by the attempt.",
      "requested_evidence": "Keep the terminal name obstructed across several pathname-branch polls and count the private files. Clear the obstruction and verify one archive, one removal, no surviving staging alias, and preservation of any foreign replacement.",
      "title": "Pathname publication leaves a new staging file on every failed retry",
      "trigger": "The claim has a frozen decision and a directory continues to occupy its terminal archive name across multiple polls."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "RecoverRetiring adds every matching name with the wrong field count or digest length to pending. It then moves that file to .claimed. A fresh session publishes an Interrupted result and archive for it. The preceding implementation itself produced the single-GUID .settled name on successful retirement.",
      "blocking_rationale": "An idle poll can move an owner\u2019s backup and overwrite a correct Applied result even though there is no unfinished request.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Recovery takes ownership only of identifiable unfinished work. Completed records and unrecognized files retain their names and disposition.",
      "id": "R9-F4",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs",
        "line": 885
      },
      "priority": "P2",
      "requested_correction": "Require evidence that a settled entry represents pending work before moving it. Preserve unrecognized filenames and handle the prior completed-record format conservatively.",
      "requested_evidence": "Poll a fresh session with a completed older-format record, its Applied result and archive, and a backup matching the settled glob. Verify their names and bytes stay intact and no Interrupted archive appears. Retain the genuine stranded-replacement cases.",
      "title": "Recovery treats unrecognized settled files as interrupted requests",
      "trigger": "There is no current claim, but the repair folder contains a completed .claimed.<guid>.settled record or a backup matching .claimed.*.settled."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R4-F3",
      "rationale": "The new settled name records A\u2019s digest prefix. In all four recorded cases, recovery found B after the failed check or return, produced B\u2019s exact Interrupted archive and named result before C, and preserved B\u2019s fact. Only A and C had removals. R9-F4 concerns the separate treatment of unrecognized names."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R5-F1",
      "rationale": "The forced Windows pathname fixture showed an absent terminal during the partial write, preservation of the foreign terminal entry, and complete bytes on retry. It exercised CreateHardLinkW. The Unix source still permits publication from a newly resolved staging name or a modified staging inode."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R8-F1",
      "rationale": "On Windows, CreateFileW opens the final entry without following a reparse point. The attribute check precedes reading on that same handle. Actual junctions and simulated file-reparse cases produced zero removals, results or archives. The ordinary request applied and the read-locked regular claim settled after release. R9-F2 concerns the separate Unix implementation."
    }
  ],
  "request_id": "kmc-codex-mmr-horse-trip-20261125213249-4b8b-r09-c0a9c5",
  "round": 9,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "Fresh horse construction filters the verified stock TrippingBite grant while preserving the bite, both hooves and other donor facts. Existing-horse repair requires an explicit horse ID and one matching sourceless fact. The remaining defects concern how repair files authorize and record that change.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "N07 \u2014 Observe an ordinary horse bite hit and deal damage without intrinsic Trip. Compare legitimate dog, wolf or independently granted maneuvers where available.",
      "N10 \u2014 Observe a pre-repair horse through save/reload or an area transition, including continued usability, a single pair, correct effects and ordinary action entitlement."
    ],
    "scope_reviewed": "Final PR-4 source/offline checkpoint for MMR-06, S06 and S08, including the round-9 corrections, their callers and the candidate-bound focused evidence.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
