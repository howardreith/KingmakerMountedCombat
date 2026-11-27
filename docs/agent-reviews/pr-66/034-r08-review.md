<!-- agent-review:v1 enrollment=kmc-codex-mmr-horse-trip-20261125213249-4b8b key=r08-review part=1/1 sha256=2d78695b7449e66d8b661e577bea9e50e4bf153126426871966ac76c4029634b -->
### Codex GPT-6.2 Astra — review — round 8 — candidate `3eb8503648`

**Decision:** `changes_required` — The focused gate passed. S06 remains blocked because the pathname implementation can expose an incomplete archive and skip a replacement request after an I/O failure. On Windows, a newly claimed symbolic link can authorize fact removal before the code rejects the link. The two prior findings about the Windows retirement handle are resolved.

## Continuation for Claude

Round 8 reaches the recorded PR-4 review cap. Any further formal review requires owner authorization. If work resumes, keep the correction within the repair-file lifecycle and its focused fixtures. Publish only complete archive bytes, keep a displaced claim discoverable until its disposition is recorded, and check Windows claim eligibility before its contents can authorize removal. Preserve frozen decisions, readable-but-locked claim recovery, and the existing horse fact and world-write gates. Exercise the failure points identified below and run the existing focused Fast gate on the corrected candidate. N07 and N10 remain owner playtest observations.

#### Findings

**R4-F3** · P2 · defect · blocks readiness — A failed retirement check strands the replacement under .settled
- Location: `src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs:757`; acceptance: MMR-06, S06
- Trigger: B replaces A's verified .retiring entry while C is queued, and reading B or returning it to .claimed fails after the settling move.
- Expected: B remains discoverable and receives its own interruption result and archive before C runs.
- Actual: Close moves the current entry to .settled before verifying it there. If HoldsBytes throws or the return move fails, B stays under .settled. RecoverRetiring scans only .retiring files. The next poll can clear A's session record and process C while B still lacks a result and an interrupted archive.
- Why it blocks / does not: A requested repair can disappear from processing even though its bytes remain on disk.
- Requested correction: Keep every entry awaiting verification or return in a state that ordinary and fresh-session recovery can distinguish from a completed retirement.
- Requested evidence: Extend the late-replacement fixture with a failure after the settling move. Verify recovery in the same and a fresh session, B's byte-exact interrupted archive and named result before C, B's intact fact, and one removal of A.

**R5-F1** · P2 · defect · blocks readiness — The pathname fallback exposes the terminal archive before the write finishes
- Location: `src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs:846`; acceptance: MMR-06, S06
- Trigger: A reader opens the terminal name during publication, or the write fails after creating that name.
- Expected: The first readable terminal archive contains A's complete raw request. Failure cleanup acts only on the file created by that attempt.
- Actual: Commit creates the terminal name with FileShare.Read before writing the payload. A compatible reader can observe an empty file or a prefix. On failure, cleanup moves whichever entry then occupies target and suppresses a move error. The fixture samples first readability only after the complete write and flush.
- Why it blocks / does not: An owner or reader can receive an incomplete applied archive. Failure cleanup can also displace a different file or leave the partial archive under its terminal name.
- Requested correction: Publish the complete verified payload in one non-overwriting operation with established local filesystem semantics. Keep failed-attempt cleanup bound to the owned file.
- Requested evidence: Observe the terminal name during the existing partial-write fault with a reader that shares write access. Cover a replacement present at cleanup. Verify complete bytes at first readability, preservation of the replacement, a recoverable claim on failure, and one archive and one removal after retry.

**R8-F1** · P2 · defect · blocks readiness — Windows checks for a linked claim after it can authorize removal
- Location: `src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs:661`; acceptance: MMR-06, S06
- Trigger: A newly claimed Windows file symbolic link points to valid request text for a loaded horse with one candidate.
- Expected: The service establishes that the claimed entry is eligible before using its contents for removal or archival. A refused link remains recoverable with the horse and linked target intact.
- Actual: TryRead follows the link. DecideClaimed can remove the fact, WriteResult reports Applied, and Close reads the target again to archive it. Only then does VerifiedFileOperations.Open reject the reparse point. Each subsequent poll encounters the same retirement failure while the link remains.
- Why it blocks / does not: An entry the repair service refuses to settle can change the loaded horse and leave subsequent requests blocked. Recovery of a stranded link can also copy an unrelated target into the repair archive.
- Requested correction: Apply the Windows no-follow eligibility check to the handle supplying the claim bytes before selection and archival. Preserve recovery for eligible regular files held by another reader.
- Requested evidence: Exercise a fresh linked request naming a loaded candidate and a recovered linked claim. Verify that rejection precedes removal and copying of target bytes, and retain ordinary-file and locked-file controls. Identify which observations exercise an actual reparse point.

#### Prior finding assessments

- **R4-F3** — `fix_rejected` — The recorded late replacement survived when both the second read and move-back succeeded. A failure in either step can still remove B from the recovery queue before its disposition.
- **R5-F1** — `fix_rejected` — Writing through the newly created file removes the prior staged-name substitution. Its terminal name is already visible during the write, and cleanup still resolves that name independently. The post-flush observation misses both conditions.
- **R7-F1** — `fix_verified` — The retirement open uses FILE_FLAG_OPEN_REPARSE_POINT and checks the opened entry's attributes before setting deletion disposition on that handle. This closes the target-deletion path. The gate exercised hard-link and ordinary retirement. Symbolic-link refusal is supported by the source and compiled flag. R8-F1 concerns the earlier use of the link's contents.
- **R7-F2** — `fix_verified` — The compiled Open calls kernel32 CreateFileW directly. Its access argument includes DELETE for claims and staged archives, with GENERIC_WRITE added for staging. The desktop Windows fixtures exercised publication and retirement. The access mask no longer depends on Unity Mono's FileStream rights translation.

#### Evidence assessment

- **Inspected:** The enrolled mission, repository validation policy and handoff, developer dispositions, prior findings ledger, controller receipt and gate log.; The 4,990-line feature diff and the correction since abbf22e2, including the horse construction filter, save-repair host, claim state, file operations, focused contracts and component fixtures.
- **Executed verification:** Read-only checks confirmed the candidate HEAD, the enrolled mission SHA-256, and the controller gate log SHA-256 of 87758799cad8935788569124c1ab8cdaae51b7a48ebc45253a043843d4ac6516. The feature diff passed git diff --check.; The controller ran Test-Tiers.ps1 -Tier Fast -Focused Test-MmrHorseTripFocused.ps1 at this candidate. It exited 0 in 36.9 seconds. Components passed 1561/1561, Validate-Source passed 153/153, and all 12 focused children passed with nativeProof=false.; The Trip contract passed 52 checks, including the installed Mammoth, Dog and Wolf facts, the compiled horse filter, one bite and two hoof limbs, and the CreateFileW call and access constants. The Mounted Shield contract passed 114 checks, including embedded icon identity, and its rollback checks passed 68/68.; The Windows file fixtures recorded intact sibling bytes after hard-link retirement and successful ordinary retirement. The forced pathname fixtures recorded B's interruption before C when the move-back succeeded, and one exact archive after a partial-write failure whose cleanup succeeded.
- **Source inspection only:** The incomplete-archive and stranded-.settled cases follow from the order of operations in Commit, Close and RecoverRetiring. The recorded fixtures did not observe those failure states.; The Windows no-follow flag and attribute check protect the target at retirement. Earlier request, claim and pre-archive reads still use File.ReadAllBytes. The symbolic-link path was traced in source.
- **Required gaps:** Show that the first readable terminal archive contains the complete request and that failed publication preserves a replacement occupying the pathname.; Show that a failed verification or return after the retirement move preserves B for interruption reporting before queued C.; Show that a Windows file reparse point rejected for retirement cannot authorize a fact removal or cause its target to be archived as the claim.
- **Limitations:** The foreground developer transcript was withheld. Execution claims here use the candidate-bound controller receipt and log.; The gate ran on Windows desktop .NET. The pathname fixtures used Windows filesystem semantics. The link fixture used a hard link. Unity Mono, native Linux/macOS filesystems, gameplay and save/reload were outside the recorded execution.

#### Summary

- **Delivered behavior:** Fresh KMC horse construction filters the Mammoth donor's exact stock Bite-to-Trip fact and preserves the bite and hoof weapons. Existing horses receive a candidate diagnostic. Ordinary repair requests select one named horse and one matching sourceless fact through the shared world-write gate. The recorded refusal, supersession and applied-decision retry cases passed.
- **Scope reviewed:** PR-4 MMR-06 and its S06/S08 offline acceptance at the fixed candidate, with correction review focused on repair-file publication, retirement and their consumers.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** Decide how to proceed at the recorded eight-round PR-4 ceiling.; N07 covers a plain horse landing a damaging bite without intrinsic Trip, with legitimate wolf, dog or independently granted maneuver controls where available.; N10 covers an existing horse across save/reload or an area transition, including pair identity, shield effects and action ownership.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a21b0d-2d14-7f93-b1af-c3d68ffb0773`, duration 731.1s, response sha256 `88c9c0c067e0ad5dec2e77dbb9d08c2787bf732e6aee2cd6d0100dae3b87eebf`, request `kmc-codex-mmr-horse-trip-20261125213249-4b8b-r08-9e7b89`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "3eb85036481e17f87b01c577ccd81d9695fb3dd1",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The focused gate passed. S06 remains blocked because the pathname implementation can expose an incomplete archive and skip a replacement request after an I/O failure. On Windows, a newly claimed symbolic link can authorize fact removal before the code rejects the link. The two prior findings about the Windows retirement handle are resolved.\n\n## Continuation for Claude\n\nRound 8 reaches the recorded PR-4 review cap. Any further formal review requires owner authorization. If work resumes, keep the correction within the repair-file lifecycle and its focused fixtures. Publish only complete archive bytes, keep a displaced claim discoverable until its disposition is recorded, and check Windows claim eligibility before its contents can authorize removal. Preserve frozen decisions, readable-but-locked claim recovery, and the existing horse fact and world-write gates. Exercise the failure points identified below and run the existing focused Fast gate on the corrected candidate. N07 and N10 remain owner playtest observations.",
  "evidence_assessment": {
    "executed_verification": [
      "Read-only checks confirmed the candidate HEAD, the enrolled mission SHA-256, and the controller gate log SHA-256 of 87758799cad8935788569124c1ab8cdaae51b7a48ebc45253a043843d4ac6516. The feature diff passed git diff --check.",
      "The controller ran Test-Tiers.ps1 -Tier Fast -Focused Test-MmrHorseTripFocused.ps1 at this candidate. It exited 0 in 36.9 seconds. Components passed 1561/1561, Validate-Source passed 153/153, and all 12 focused children passed with nativeProof=false.",
      "The Trip contract passed 52 checks, including the installed Mammoth, Dog and Wolf facts, the compiled horse filter, one bite and two hoof limbs, and the CreateFileW call and access constants. The Mounted Shield contract passed 114 checks, including embedded icon identity, and its rollback checks passed 68/68.",
      "The Windows file fixtures recorded intact sibling bytes after hard-link retirement and successful ordinary retirement. The forced pathname fixtures recorded B's interruption before C when the move-back succeeded, and one exact archive after a partial-write failure whose cleanup succeeded."
    ],
    "inspected": [
      "The enrolled mission, repository validation policy and handoff, developer dispositions, prior findings ledger, controller receipt and gate log.",
      "The 4,990-line feature diff and the correction since abbf22e2, including the horse construction filter, save-repair host, claim state, file operations, focused contracts and component fixtures."
    ],
    "limitations": [
      "The foreground developer transcript was withheld. Execution claims here use the candidate-bound controller receipt and log.",
      "The gate ran on Windows desktop .NET. The pathname fixtures used Windows filesystem semantics. The link fixture used a hard link. Unity Mono, native Linux/macOS filesystems, gameplay and save/reload were outside the recorded execution."
    ],
    "required_gaps": [
      "Show that the first readable terminal archive contains the complete request and that failed publication preserves a replacement occupying the pathname.",
      "Show that a failed verification or return after the retirement move preserves B for interruption reporting before queued C.",
      "Show that a Windows file reparse point rejected for retirement cannot authorize a fact removal or cause its target to be archived as the claim."
    ],
    "source_inspection_only": [
      "The incomplete-archive and stranded-.settled cases follow from the order of operations in Commit, Close and RecoverRetiring. The recorded fixtures did not observe those failure states.",
      "The Windows no-follow flag and attribute check protect the target at retirement. Earlier request, claim and pre-archive reads still use File.ReadAllBytes. The symbolic-link path was traced in source."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "Close moves the current entry to .settled before verifying it there. If HoldsBytes throws or the return move fails, B stays under .settled. RecoverRetiring scans only .retiring files. The next poll can clear A's session record and process C while B still lacks a result and an interrupted archive.",
      "blocking_rationale": "A requested repair can disappear from processing even though its bytes remain on disk.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "B remains discoverable and receives its own interruption result and archive before C runs.",
      "id": "R4-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs",
        "line": 757
      },
      "priority": "P2",
      "requested_correction": "Keep every entry awaiting verification or return in a state that ordinary and fresh-session recovery can distinguish from a completed retirement.",
      "requested_evidence": "Extend the late-replacement fixture with a failure after the settling move. Verify recovery in the same and a fresh session, B's byte-exact interrupted archive and named result before C, B's intact fact, and one removal of A.",
      "title": "A failed retirement check strands the replacement under .settled",
      "trigger": "B replaces A's verified .retiring entry while C is queued, and reading B or returning it to .claimed fails after the settling move."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "Commit creates the terminal name with FileShare.Read before writing the payload. A compatible reader can observe an empty file or a prefix. On failure, cleanup moves whichever entry then occupies target and suppresses a move error. The fixture samples first readability only after the complete write and flush.",
      "blocking_rationale": "An owner or reader can receive an incomplete applied archive. Failure cleanup can also displace a different file or leave the partial archive under its terminal name.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The first readable terminal archive contains A's complete raw request. Failure cleanup acts only on the file created by that attempt.",
      "id": "R5-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs",
        "line": 846
      },
      "priority": "P2",
      "requested_correction": "Publish the complete verified payload in one non-overwriting operation with established local filesystem semantics. Keep failed-attempt cleanup bound to the owned file.",
      "requested_evidence": "Observe the terminal name during the existing partial-write fault with a reader that shares write access. Cover a replacement present at cleanup. Verify complete bytes at first readability, preservation of the replacement, a recoverable claim on failure, and one archive and one removal after retry.",
      "title": "The pathname fallback exposes the terminal archive before the write finishes",
      "trigger": "A reader opens the terminal name during publication, or the write fails after creating that name."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "TryRead follows the link. DecideClaimed can remove the fact, WriteResult reports Applied, and Close reads the target again to archive it. Only then does VerifiedFileOperations.Open reject the reparse point. Each subsequent poll encounters the same retirement failure while the link remains.",
      "blocking_rationale": "An entry the repair service refuses to settle can change the loaded horse and leave subsequent requests blocked. Recovery of a stranded link can also copy an unrelated target into the repair archive.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The service establishes that the claimed entry is eligible before using its contents for removal or archival. A refused link remains recoverable with the horse and linked target intact.",
      "id": "R8-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs",
        "line": 661
      },
      "priority": "P2",
      "requested_correction": "Apply the Windows no-follow eligibility check to the handle supplying the claim bytes before selection and archival. Preserve recovery for eligible regular files held by another reader.",
      "requested_evidence": "Exercise a fresh linked request naming a loaded candidate and a recovered linked claim. Verify that rejection precedes removal and copying of target bytes, and retain ordinary-file and locked-file controls. Identify which observations exercise an actual reparse point.",
      "title": "Windows checks for a linked claim after it can authorize removal",
      "trigger": "A newly claimed Windows file symbolic link points to valid request text for a loaded horse with one candidate."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_rejected",
      "finding_id": "R4-F3",
      "rationale": "The recorded late replacement survived when both the second read and move-back succeeded. A failure in either step can still remove B from the recovery queue before its disposition."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R5-F1",
      "rationale": "Writing through the newly created file removes the prior staged-name substitution. Its terminal name is already visible during the write, and cleanup still resolves that name independently. The post-flush observation misses both conditions."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R7-F1",
      "rationale": "The retirement open uses FILE_FLAG_OPEN_REPARSE_POINT and checks the opened entry's attributes before setting deletion disposition on that handle. This closes the target-deletion path. The gate exercised hard-link and ordinary retirement. Symbolic-link refusal is supported by the source and compiled flag. R8-F1 concerns the earlier use of the link's contents."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R7-F2",
      "rationale": "The compiled Open calls kernel32 CreateFileW directly. Its access argument includes DELETE for claims and staged archives, with GENERIC_WRITE added for staging. The desktop Windows fixtures exercised publication and retirement. The access mask no longer depends on Unity Mono's FileStream rights translation."
    }
  ],
  "request_id": "kmc-codex-mmr-horse-trip-20261125213249-4b8b-r08-9e7b89",
  "round": 8,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "Fresh KMC horse construction filters the Mammoth donor's exact stock Bite-to-Trip fact and preserves the bite and hoof weapons. Existing horses receive a candidate diagnostic. Ordinary repair requests select one named horse and one matching sourceless fact through the shared world-write gate. The recorded refusal, supersession and applied-decision retry cases passed.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "Decide how to proceed at the recorded eight-round PR-4 ceiling.",
      "N07 covers a plain horse landing a damaging bite without intrinsic Trip, with legitimate wolf, dog or independently granted maneuver controls where available.",
      "N10 covers an existing horse across save/reload or an area transition, including pair identity, shield effects and action ownership."
    ],
    "scope_reviewed": "PR-4 MMR-06 and its S06/S08 offline acceptance at the fixed candidate, with correction review focused on repair-file publication, retirement and their consumers.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
