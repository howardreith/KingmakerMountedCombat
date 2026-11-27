<!-- agent-review:v1 enrollment=kmc-codex-mmr-horse-trip-20261125213249-4b8b key=r07-review part=1/1 sha256=2de921d7b7eb892ccb5e867e9edf82afce71beaaedfa52ab8ad52c1c3666302b -->
### Codex GPT-6.2 Astra — review — round 7 — candidate `abbf22e22c`

**Decision:** `changes_required` — The focused gate passed. It verifies the per-claim archive reset, truthful restart reporting, and the two rename exclusions on ordinary Windows files. Linux/Mono still publishes and deletes through replaceable pathnames. The new Windows retirement path can also delete the target of a symbolic link outside the repair folder.

## Continuation for Claude

Keep the correction within the repair file operations and their focused fixtures. On Linux/Mono, protect the completed payload through its first appearance under the terminal name and preserve any replacement encountered during retirement for its own disposition. On Windows, establish that the delete-capable handle belongs to an ordinary claim file in the repair directory. Verify how the shipped Windows Mono profile obtains that handle.

Use disposable files to observe the first readable archive bytes, the fate of a late replacement, and the survival of a linked target. Retain the passing Windows controls, the per-claim reset, and the restart wording. Run the existing focused gate on the corrected candidate. N07 and N10 remain owner playtest observations.

#### Findings

**R7-F1** · P1 · defect · blocks readiness — Windows claim retirement can delete a symbolic link’s target
- Location: `src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs:659`; acceptance: MMR-06, S06
- Trigger: A stranded .claimed entry on Windows is a file symbolic link to a regular file that the process can delete.
- Expected: Retirement affects only an owned claim entry. The linked file keeps its name and contents.
- Actual: VerifiedFileOperations.Open follows the link with FileOptions.None. The digest matches because the earlier read followed the same link. DeleteWhenClosed then marks the target for deletion. Fresh-session interruption reaches this path even for a malformed claim.
- Why it blocks / does not: Settling a repair claim can delete an unrelated owner file outside the repair directory. A pre-existing link is sufficient.
- Requested correction: Acquire the retirement handle without following a file symbolic link and verify the opened entry is eligible before setting its disposition. Leave an unsupported entry recoverable.
- Requested evidence: Exercise recovery with a Windows file link and a disposable target in a sibling directory. Verify the target still exists with its original bytes and that ordinary claim retirement still succeeds.

**R4-F3** · P2 · defect · blocks readiness — Linux/Mono retirement still deletes a pathname after verifying a different handle
- Location: `src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs:674`; acceptance: MMR-06, S06
- Trigger: A local rename replaces the .retiring entry between the digest comparison and deletion.
- Expected: Only the verified claim is retired. A replacement survives for settlement before the next queued request.
- Actual: The fallback hashes A through the open stream and then calls File.Delete(retiring). That call resolves the directory entry again and can delete B. The new BeforeRetireDelete observation exists only in the Windows branch.
- Why it blocks / does not: B can disappear before the owner receives its interruption result or archive.
- Requested correction: Make retirement on the supported Linux/Mono path act on an entry whose identity is still protected, or preserve the displaced entry for recovery when ownership cannot be established.
- Requested evidence: Exercise replacement at the actual fallback retirement operation after verification. Show that replacement is excluded or that B receives its own result and archive before queued C, with one removal of A.

**R5-F1** · P2 · defect · blocks readiness — Linux/Mono publishes a staged replacement before checking its bytes
- Location: `src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs:757`; acceptance: MMR-06, S06
- Trigger: The .pending entry is replaced after A’s payload is flushed and before the fallback moves it.
- Expected: The first readable bytes under A’s terminal suffix belong to A. Failed publication preserves the claim and cleans up only files owned by the attempt.
- Actual: The fallback moves whichever file occupies pending to the terminal name, then reads and compares it. A reader can obtain B from A’s .applied archive before the mismatch is detected. The finally block also cleans up by pathname.
- Why it blocks / does not: An owner or reader can observe an applied archive for the wrong request. Subsequent cleanup cannot retract that observation.
- Requested correction: Bind Linux/Mono publication to the verified, complete payload through the terminal operation. Keep failure cleanup bound to the attempt’s own files.
- Requested evidence: Exercise the production fallback with a staging replacement and observe the terminal name at first visibility. Check raw archive bytes, recoverability after refusal, and one removal. The current first-visibility hook runs only on Windows.

**R7-F2** · P2 · evidence_gap · blocks readiness — The delete-capable open is unverified for Kingmaker’s Windows Mono profile
- Location: `src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs:508`; acceptance: MMR-06, S06
- Trigger: The Windows player enters VerifiedFileOperations.Open for a normal archive or claim.
- Expected: The Windows game runtime can obtain the delete-capable file handle required by the new publication and retirement operations.
- Actual: Win32NT selects this overload in both desktop .NET and the Windows Unity/Mono player. The gate executed the component runner on desktop .NET. Its installed-contract checks did not establish how the game’s FileStream implementation passes the requested Delete access to the operating system.
- Why it blocks / does not: Both terminal operations require DELETE access. If the target profile opens the stream with only read/write access, an ordinary repair can remove the fact and then fail every attempt to finish its archive.
- Requested correction: Establish the access contract of the file-open mechanism used by the supported Windows player. If the managed overload discards the required rights, use an open operation that supplies them explicitly.
- Requested evidence: Provide a version-bound offline trace showing the requested access reaches the native open, or a matching-runtime temporary-file check that publishes and retires an ordinary claim through the production operations.

#### Prior finding assessments

- **R4-F3** — `fix_rejected` — The Windows fixture excluded the rename after A was verified. The Linux/Mono branch retains the same independently resolved File.Delete that can consume B.
- **R5-F1** — `fix_rejected` — The Windows handle rename protected the staged file through publication. The Linux/Mono branch still makes the terminal name visible before checking which bytes it published.
- **R6-F1** — `fix_verified` — Take clears CommittedArchive when B is taken. The same-session fixture retained A’s applied archive, wrote B’s interrupted archive and result, preserved B’s fact, then processed C. A retry of the same claim retains its committed archive.
- **R6-F2** — `fix_verified` — After A’s removal and failed archive, fresh-session recovery named horse-A, acknowledged the possible earlier removal and directed the owner to the matching candidate log. The fixture asserted one removal and one removal call across both sessions.

#### Evidence assessment

- **Inspected:** The enrolled mission, repository validation policy and handoffs, full feature diff, prior findings ledger, developer report, and controller receipt and log.; The round-7 production and test changes, claim selection and publication lifecycle, Horse blueprint construction, saved-fact predicate, integration host, and focused gate entry points.
- **Executed verification:** I verified HEAD and the merge base, matched the mission and gate-log SHA-256 digests to the controller inputs, and ran git diff --check over the feature change.; The controller receipt records Test-Tiers.ps1 -Tier Fast -Focused Test-MmrHorseTripFocused.ps1 on the exact candidate. It exited 0 in 37.0 seconds with 1,558 component tests, 153 source checks and all 12 focused children passing.; The Windows fixture recorded both late rename attempts being excluded. The terminal archive could not be read while the staging handle was held and contained A afterward. The replacement-claim and fresh-session recovery cases also passed.; The installed Trip contract passed 51 checks, including the Mammoth, Dog and Wolf facts, the compiled Horse filter, and one bite plus two hoof limbs. Mounted Shield passed 114 contract checks including embedded icon identity. Both UMM loader profiles passed.
- **Source inspection only:** The Linux/Mono replacement failures follow from the order of File.Move, digest verification and File.Delete in the fallback. The Windows gate exercised a different branch.; The Windows symbolic-link finding follows from opening the claim with FileOptions.None and applying disposition to the resolved handle. The supplied fixtures use ordinary files.
- **Required gaps:** Correct the Linux/Mono publication and retirement paths and exercise their actual final operations.; Demonstrate that Windows claim retirement preserves a linked target and that the supported Windows Mono file-open path supplies the access required for handle-based rename and deletion.
- **Limitations:** The controller withheld the foreground developer transcript. The candidate gate is independently recorded. The additional local runs mentioned in the report could not be confirmed from that transcript.; The review was read-only. The gate reports nativeProof=false. Gameplay, save/reload behavior and Linux runtime certification remain unobserved.

#### Summary

- **Delivered behavior:** Fresh KMC horses omit the Mammoth donor’s exact stock bite-to-Trip fact and retain their bite and hoof limbs. Saved horses expose an owner-requested repair for a single matching sourceless fact. The per-claim archive reset and restart guidance are verified.
- **Scope reviewed:** Final PR #66 source and offline checkpoint for MMR-06, S06 and the S08 regression boundary at the fixed candidate.
- **Pending later-phase work:** none
- **Unresolved questions:** Does the shipped Windows Mono implementation preserve DELETE access through the selected FileStream constructor?
- **Remaining owner review:** N07 covers a successful plain-horse bite and damage without intrinsic Trip, with legitimate trip and granted-maneuver controls where available.; N10 covers an existing horse across save/reload or an area transition, including pair, effect and action integrity.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a21af8-3241-7f61-8875-43df36156fe6`, duration 609.7s, response sha256 `0e21815b02d28ffb29f08cfe6591687cf91b3732ec186c0117de9c4f43950a86`, request `kmc-codex-mmr-horse-trip-20261125213249-4b8b-r07-9bc073`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "abbf22e22cc448dd06cc18b94fb6a2f0897d9721",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The focused gate passed. It verifies the per-claim archive reset, truthful restart reporting, and the two rename exclusions on ordinary Windows files. Linux/Mono still publishes and deletes through replaceable pathnames. The new Windows retirement path can also delete the target of a symbolic link outside the repair folder.\n\n## Continuation for Claude\n\nKeep the correction within the repair file operations and their focused fixtures. On Linux/Mono, protect the completed payload through its first appearance under the terminal name and preserve any replacement encountered during retirement for its own disposition. On Windows, establish that the delete-capable handle belongs to an ordinary claim file in the repair directory. Verify how the shipped Windows Mono profile obtains that handle.\n\nUse disposable files to observe the first readable archive bytes, the fate of a late replacement, and the survival of a linked target. Retain the passing Windows controls, the per-claim reset, and the restart wording. Run the existing focused gate on the corrected candidate. N07 and N10 remain owner playtest observations.",
  "evidence_assessment": {
    "executed_verification": [
      "I verified HEAD and the merge base, matched the mission and gate-log SHA-256 digests to the controller inputs, and ran git diff --check over the feature change.",
      "The controller receipt records Test-Tiers.ps1 -Tier Fast -Focused Test-MmrHorseTripFocused.ps1 on the exact candidate. It exited 0 in 37.0 seconds with 1,558 component tests, 153 source checks and all 12 focused children passing.",
      "The Windows fixture recorded both late rename attempts being excluded. The terminal archive could not be read while the staging handle was held and contained A afterward. The replacement-claim and fresh-session recovery cases also passed.",
      "The installed Trip contract passed 51 checks, including the Mammoth, Dog and Wolf facts, the compiled Horse filter, and one bite plus two hoof limbs. Mounted Shield passed 114 contract checks including embedded icon identity. Both UMM loader profiles passed."
    ],
    "inspected": [
      "The enrolled mission, repository validation policy and handoffs, full feature diff, prior findings ledger, developer report, and controller receipt and log.",
      "The round-7 production and test changes, claim selection and publication lifecycle, Horse blueprint construction, saved-fact predicate, integration host, and focused gate entry points."
    ],
    "limitations": [
      "The controller withheld the foreground developer transcript. The candidate gate is independently recorded. The additional local runs mentioned in the report could not be confirmed from that transcript.",
      "The review was read-only. The gate reports nativeProof=false. Gameplay, save/reload behavior and Linux runtime certification remain unobserved."
    ],
    "required_gaps": [
      "Correct the Linux/Mono publication and retirement paths and exercise their actual final operations.",
      "Demonstrate that Windows claim retirement preserves a linked target and that the supported Windows Mono file-open path supplies the access required for handle-based rename and deletion."
    ],
    "source_inspection_only": [
      "The Linux/Mono replacement failures follow from the order of File.Move, digest verification and File.Delete in the fallback. The Windows gate exercised a different branch.",
      "The Windows symbolic-link finding follows from opening the claim with FileOptions.None and applying disposition to the resolved handle. The supplied fixtures use ordinary files."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "VerifiedFileOperations.Open follows the link with FileOptions.None. The digest matches because the earlier read followed the same link. DeleteWhenClosed then marks the target for deletion. Fresh-session interruption reaches this path even for a malformed claim.",
      "blocking_rationale": "Settling a repair claim can delete an unrelated owner file outside the repair directory. A pre-existing link is sufficient.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Retirement affects only an owned claim entry. The linked file keeps its name and contents.",
      "id": "R7-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs",
        "line": 659
      },
      "priority": "P1",
      "requested_correction": "Acquire the retirement handle without following a file symbolic link and verify the opened entry is eligible before setting its disposition. Leave an unsupported entry recoverable.",
      "requested_evidence": "Exercise recovery with a Windows file link and a disposable target in a sibling directory. Verify the target still exists with its original bytes and that ordinary claim retirement still succeeds.",
      "title": "Windows claim retirement can delete a symbolic link\u2019s target",
      "trigger": "A stranded .claimed entry on Windows is a file symbolic link to a regular file that the process can delete."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "The fallback hashes A through the open stream and then calls File.Delete(retiring). That call resolves the directory entry again and can delete B. The new BeforeRetireDelete observation exists only in the Windows branch.",
      "blocking_rationale": "B can disappear before the owner receives its interruption result or archive.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Only the verified claim is retired. A replacement survives for settlement before the next queued request.",
      "id": "R4-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs",
        "line": 674
      },
      "priority": "P2",
      "requested_correction": "Make retirement on the supported Linux/Mono path act on an entry whose identity is still protected, or preserve the displaced entry for recovery when ownership cannot be established.",
      "requested_evidence": "Exercise replacement at the actual fallback retirement operation after verification. Show that replacement is excluded or that B receives its own result and archive before queued C, with one removal of A.",
      "title": "Linux/Mono retirement still deletes a pathname after verifying a different handle",
      "trigger": "A local rename replaces the .retiring entry between the digest comparison and deletion."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "The fallback moves whichever file occupies pending to the terminal name, then reads and compares it. A reader can obtain B from A\u2019s .applied archive before the mismatch is detected. The finally block also cleans up by pathname.",
      "blocking_rationale": "An owner or reader can observe an applied archive for the wrong request. Subsequent cleanup cannot retract that observation.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The first readable bytes under A\u2019s terminal suffix belong to A. Failed publication preserves the claim and cleans up only files owned by the attempt.",
      "id": "R5-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs",
        "line": 757
      },
      "priority": "P2",
      "requested_correction": "Bind Linux/Mono publication to the verified, complete payload through the terminal operation. Keep failure cleanup bound to the attempt\u2019s own files.",
      "requested_evidence": "Exercise the production fallback with a staging replacement and observe the terminal name at first visibility. Check raw archive bytes, recoverability after refusal, and one removal. The current first-visibility hook runs only on Windows.",
      "title": "Linux/Mono publishes a staged replacement before checking its bytes",
      "trigger": "The .pending entry is replaced after A\u2019s payload is flushed and before the fallback moves it."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "Win32NT selects this overload in both desktop .NET and the Windows Unity/Mono player. The gate executed the component runner on desktop .NET. Its installed-contract checks did not establish how the game\u2019s FileStream implementation passes the requested Delete access to the operating system.",
      "blocking_rationale": "Both terminal operations require DELETE access. If the target profile opens the stream with only read/write access, an ordinary repair can remove the fact and then fail every attempt to finish its archive.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "The Windows game runtime can obtain the delete-capable file handle required by the new publication and retirement operations.",
      "id": "R7-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs",
        "line": 508
      },
      "priority": "P2",
      "requested_correction": "Establish the access contract of the file-open mechanism used by the supported Windows player. If the managed overload discards the required rights, use an open operation that supplies them explicitly.",
      "requested_evidence": "Provide a version-bound offline trace showing the requested access reaches the native open, or a matching-runtime temporary-file check that publishes and retires an ordinary claim through the production operations.",
      "title": "The delete-capable open is unverified for Kingmaker\u2019s Windows Mono profile",
      "trigger": "The Windows player enters VerifiedFileOperations.Open for a normal archive or claim."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_rejected",
      "finding_id": "R4-F3",
      "rationale": "The Windows fixture excluded the rename after A was verified. The Linux/Mono branch retains the same independently resolved File.Delete that can consume B."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R5-F1",
      "rationale": "The Windows handle rename protected the staged file through publication. The Linux/Mono branch still makes the terminal name visible before checking which bytes it published."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R6-F1",
      "rationale": "Take clears CommittedArchive when B is taken. The same-session fixture retained A\u2019s applied archive, wrote B\u2019s interrupted archive and result, preserved B\u2019s fact, then processed C. A retry of the same claim retains its committed archive."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R6-F2",
      "rationale": "After A\u2019s removal and failed archive, fresh-session recovery named horse-A, acknowledged the possible earlier removal and directed the owner to the matching candidate log. The fixture asserted one removal and one removal call across both sessions."
    }
  ],
  "request_id": "kmc-codex-mmr-horse-trip-20261125213249-4b8b-r07-9bc073",
  "round": 7,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "Fresh KMC horses omit the Mammoth donor\u2019s exact stock bite-to-Trip fact and retain their bite and hoof limbs. Saved horses expose an owner-requested repair for a single matching sourceless fact. The per-claim archive reset and restart guidance are verified.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "N07 covers a successful plain-horse bite and damage without intrinsic Trip, with legitimate trip and granted-maneuver controls where available.",
      "N10 covers an existing horse across save/reload or an area transition, including pair, effect and action integrity."
    ],
    "scope_reviewed": "Final PR #66 source and offline checkpoint for MMR-06, S06 and the S08 regression boundary at the fixed candidate.",
    "unresolved_questions": [
      "Does the shipped Windows Mono implementation preserve DELETE access through the selected FileStream constructor?"
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
