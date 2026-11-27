<!-- agent-review:v1 enrollment=kmc-codex-mmr-horse-trip-20261125213249-4b8b key=r11-review part=1/1 sha256=c6a06eccc5e19060ec4bba792dbb56935391f23561f2a7afa48c71963536f327 -->
### Codex GPT-6.2 Astra — review — round 11 — candidate `68a19050fa`

**Decision:** `changes_required` — The focused gate passed and the inherited stock TrippingBite is isolated at horse construction. Existing-save repair can still remove a fact from an unfinished request, expose an incomplete archive, or replace a correct Applied result with Interrupted. These outcomes block S06.

## Continuation for Claude

Round 11 uses the last recorded PR-4 review slot. Further review requires owner authorization. If work resumes, keep the correction within request admission, archive publication, recovery, and service teardown. Establish a completed submission on each supported platform and protect the archive through its first readable instant. Preserve a recovery entry's original name and classification across failures, settle older work before queued requests, and release retained file descriptors on unload. Exercise the cases in the findings with the focused filesystem harness, observe the effective Unix permissions, and bind the Fast gate to the corrected candidate. Preserve the verified horse selection, frozen decisions, and Windows handle behavior. N07 and N10 remain owner playtest observations.

#### Findings

**R10-F2** · P2 · defect · blocks readiness — Unix can still authorize a request before its writer finishes
- Location: `src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs:678`; acceptance: MMR-06, S06
- Trigger: A player writes directly to the documented request file and pauses between two horse lines on macOS or a Linux filesystem where acquiring the lease fails with an error other than EAGAIN.
- Expected: A deciding read obtains a completed, stable submission before authorizing removal.
- Actual: macOS returns ENOTSUP from WriterLeaseFree. ReadEligible rejects only EAGAIN, so both the request and claim reads can accept the first line while the same writer remains open. Other Linux lease errors also fall through. Even a successful Linux lease is released before the read.
- Why it blocks / does not: The completed two-horse submission requires zero removals. The first horse can already have lost its fact.
- Requested correction: Establish completion through the deciding read on each supported Unix path. Treat an unavailable or failed completion check as unresolved and retain a working single-horse submission path.
- Requested evidence: Pause a writer after its first horse line under both the supported-lease and unavailable-lease paths. Complete the second line and observe a malformed refusal with zero removals. Retain a completed single-horse control.

**R5-F1** · P2 · defect · blocks readiness — The fallback can expose the terminal archive before its payload is protected
- Location: `src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs:750`; acceptance: MMR-06, S06
- Trigger: Descriptor publication is unavailable and either the volume ignores mode zero or an ordinary same-account restore changes permissions while a failed write awaits retry.
- Expected: The first readable bytes under the terminal suffix are the complete bytes that authorized the decision. A refused publication keeps the claim recoverable.
- Actual: The fallback creates the terminal name before checking its mode. A volume that creates it as 0644 therefore exposes an empty archive immediately. On a permissions-preserving volume, the code caches the initial mode across retries. A permission restore after a partial write can expose those bytes. An equal-length edit after read-back can also pass the final device, inode, and size comparison.
- Why it blocks / does not: A reader can see an empty, partial, or incorrect Applied archive for a removal that already occurred.
- Requested correction: Use a fallback that protects the complete payload through publication. Where that protection is unavailable, retain the claim without exposing a readable terminal entry.
- Requested evidence: After descriptor-link failure, observe the terminal with a compatible reader on the effective-mode mismatch path and during a partial-write permission restore. Check the first readable bytes and retain the one-removal and foreign-replacement controls.

**R10-F4** · P2 · defect · blocks readiness — A failed post-move check loses the origin of a recovered entry
- Location: `src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs:1185`; acceptance: MMR-06, S06
- Trigger: A settled entry is restored to its completed bytes after the scan, and its first verification at .claimed fails transiently.
- Expected: The entry actually admitted for interruption is verified as unfinished. A completed or ineligible replacement retains its proper name through retries.
- Actual: After File.Move, the original settled name and digest prefix exist only in local variables. A sharing violation during HoldsIdentityPrefix leaves the completed record at .claimed. On the next poll RecoverRetiring sees .claimed and returns, so the ordinary interruption path consumes it. For a .retiring entry, the short-circuit at line 1185 also skips the post-move eligibility check entirely.
- Why it blocks / does not: Recovery can overwrite a valid Applied result or leave an ineligible replacement occupying the claim name.
- Requested correction: Preserve the recovery origin until classification and any return move succeed. Recheck eligibility for both recognized name shapes before allowing the ordinary claim lifecycle to consume the entry.
- Requested evidence: Restore completed bytes before the recovery move while a writer permits rename but prevents the first verification read. Release it and retry in the same and a fresh session. Verify the settled record and Applied result/archive remain intact. Also substitute an ineligible retiring entry and verify its original name survives.

**R10-F5** · P2 · evidence_gap · blocks readiness — The final access mode of a private archive was not observed
- Location: `tests/HorseTripRepairPolicyTests.cs:695`; acceptance: MMR-06, S06
- Trigger: The focused gate reports a final mode of 600 for protected creation.
- Expected: The published private archive has owner-only effective access and contains the original request bytes.
- Actual: The source now passes 0600 to fchmod. The test prints SimulatedPosix.LastChmod, which records that argument. Its pathname lstat supplies a fixed 0644 for regular files. The retained Linux probe does not perform the final chmod and stat.
- Why it blocks / does not: The prior finding explicitly requested observation of the published file's effective mode under a restrictive umask. The recorded value establishes the requested mode only.
- Requested correction: Complete the focused permission observation and label the existing simulated value as the fchmod argument.
- Requested evidence: Publish through the fallback for a private request under umask 077. Record the final file's observed mode and compare its bytes with the request. A bounded local filesystem probe is sufficient.

**R11-F1** · P1 · defect · blocks readiness — A Linux lease break can signal the game process
- Location: `src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs:613`; acceptance: MMR-06, S06
- Trigger: An editor opens the request for writing after the read lease is acquired and before it is released.
- Expected: Contention over the request delays or refuses the repair while the host process continues running.
- Actual: F_SETLEASE can deliver SIGIO to the lease holder when a conflicting open requests a break. The mod acquires the lease in the game process without arranging safe notification. Under the default Linux disposition, SIGIO terminates the process.
- Why it blocks / does not: Ordinary file contention can terminate Kingmaker with unsaved play. The Windows simulation produces no lease-break signal.
- Requested correction: Use a submission check with process-safe contention behavior. If retaining Linux leases, account for break notification without relying on or changing an undocumented game-wide signal disposition.
- Requested evidence: Exercise overlapping reader and writer opens in a disposable Linux process using the selected mechanism. Record normal process survival, writer completion, and the resulting request decision.

**R11-F2** · P2 · defect · blocks readiness — A missing or resized fallback archive can stall the repair queue indefinitely
- Location: `src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs:787`; acceptance: MMR-06, S06
- Trigger: The fallback terminal is moved away without a replacement, or its length changes before final verification.
- Expected: A transient lookup error retries verification. Confirmed loss or invalidation of the attempted archive has a bounded route to publishing the frozen decision.
- Actual: Every lstat error retains the same terminal name, including ENOENT. A size mismatch also retains Published=true. Subsequent polls keep checking the missing name or the same wrong size and skip the write that the diagnostic says will be retried.
- Why it blocks / does not: The frozen claim can block all later repair requests for the rest of the session after the filesystem obstruction has cleared.
- Requested correction: Give confirmed absence and a changed published file explicit recovery transitions. Preserve other files and the original removal decision while selecting a safe publication target.
- Requested evidence: After a fallback write fault, move its terminal file aside and leave the name vacant. Separately change the file length after chmod and before successful verification. In each case show that the same session retires the claim and admits a queued request with one original removal.

**R11-F3** · P2 · defect · blocks readiness — Unloading the horse service leaks a pending Unix archive descriptor
- Location: `src/KingmakerMountedCombat/Integration/HorseCompanionBlueprintService.cs:533`; acceptance: MMR-06, S06
- Trigger: UMM unloads the mod after a fallback write or metadata failure and before its next successful poll.
- Expected: Successful service teardown closes every descriptor it owns and preserves the on-disk claim for conservative recovery.
- Actual: UnixPublicationAttempt retains a raw integer descriptor between polls. Successful HorseCompanionBlueprintService.Dispose discards the service without clearing the repair session. The descriptor has no finalizer or safe-handle owner.
- Why it blocks / does not: A successful mod unload leaves a writable file descriptor open for the lifetime of the game. Repeated unloads with pending attempts can accumulate descriptors.
- Requested correction: Tie the retained attempt to successful service disposal and close it exactly once. Preserve retry state when teardown itself is refused.
- Requested evidence: Leave a fallback attempt pending, invoke the owning teardown path, and observe descriptor closure. A fresh session should recover the claim without another fact removal.

**R11-F4** · P2 · defect · blocks readiness — Returning one completed record lets a queued request overtake another pending claim
- Location: `src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs:1187`; acceptance: MMR-06, S06
- Trigger: Two entries qualify during the recovery scan, and the oldest is recognized as completed only after the move.
- Expected: Genuine stranded claims receive their interruption result and archive before the poll admits a newer request.
- Actual: RecoverRetiring examines only pending[0]. After returning that completed record to its original name, it returns with .claimed vacant. Process then takes the new request even when pending[1] is a genuine stranded claim.
- Why it blocks / does not: A newer removal can run first, and the older interruption can subsequently overwrite its owner-facing result.
- Requested correction: Continue with the remaining recovery candidates, or defer new request admission until a subsequent scan establishes that no older work remains.
- Requested evidence: Seed two recovery candidates and a queued request. Restore the oldest candidate to its completed bytes before the move. Verify that the second candidate is interrupted before the queued horse is processed and the completed record stays intact.

**R11-F5** · P3 · optional_polish · non-blocking — The fallback comment still describes the previous permissions and cleanup
- Location: `src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs:528`; acceptance: —
- Trigger: A maintainer consults the file-operation contract above LibcPosixFiles.
- Expected: The comment describes the publication mode and failure ownership used by the implementation.
- Actual: The production comment still describes fchmod(0644) and deletion of the failed attempt. The current branch requests 0600 and retains the descriptor across polls.
- Why it blocks / does not: This is a maintenance comment. The executable behavior and its evidence are assessed separately.
- Requested correction: Update the fallback comment alongside the implementation and remove the obsolete PublishedMode constant if it remains unused.
- Requested evidence: Source inspection is sufficient.

#### Prior finding assessments

- **R10-F1** — `fix_verified` — The fallback no longer unlinks a terminal pathname. The recorded replacement case preserved the foreign bytes, counted zero unlinks, published the original request once, and recorded one removal.
- **R10-F2** — `fix_rejected` — The simulated EAGAIN case passed. Production accepts ENOTSUP on macOS and other non-EAGAIN failures, and releases a successful lease before reading. A writer can still supply an unfinished decision.
- **R10-F3** — `fix_verified` — Recovery recognizes the legacy 32-lowercase-hex retiring name. Both gate branches interrupted the stranded horse-B request before applying horse-C and preserved the legacy completed record.
- **R10-F4** — `fix_rejected` — The tested restore passed when the first post-move read succeeded. A transient failure at that read loses the settled name, allowing the next poll to report the same completed record as Interrupted. Retiring entries also bypass the post-move eligibility check.
- **R10-F5** — `fix_rejected` — The 0644 argument was corrected to 0600. The gate inspected LastChmod, and the Linux probe did not inspect a published archive under a restrictive umask. The requested final-mode observation remains outstanding.
- **R5-F1** — `fix_rejected` — The new check withholds payload writes when initial mode bits are wider than requested, but the terminal name already exists. Its mode is cached during retries, and a same-size content change can survive final verification. The simulated Windows share mode excludes readers and writers more strongly than these Unix operations.
- **R9-F3** — `fix_verified` — Two injected initial fstat failures and one post-chmod lstat failure retained one attempt. The gate observed file counts of 3/3/3/3, one protected create, one exact archive, and one removal. R11-F2 concerns the distinct case of a definitively missing or changed target.
- **R9-F4** — `fix_verified` — Both production matchers require the absolute end of the filename. The gate rejected trailing line feeds for both shapes, accepted the producer names, and preserved the older completed record and backup.

#### Evidence assessment

- **Inspected:** Controller mission, developer report, transcript notice, eight pending ledger entries, gate receipt and log, repository instructions, validation policy, and current handoffs.; The 26-file feature diff and round-11 correction against 9f2d8c09, including horse construction, the removal gate, file operations, recovery fixtures, and the service that owns the repair session.; The operation/failure table and retained round-10 Linux syscall probe and output.
- **Executed verification:** Read-only Git checks confirmed H and merge base M. The mission SHA-256 matched the enrollment, and the gate log SHA-256 matched its controller receipt, ba40b97ad3a1eef0f499d475b50e534154abb6b0369d536768bc0481686a1d49.; The controller ran Test-Tiers.ps1 -Tier Fast -Focused Test-MmrHorseTripFocused.ps1 at H. It exited 0 in 43.0 seconds with 1,572 component passes, 153 source-validation passes, and 12 of 12 focused children passing.; The Trip contract passed 52 checks, including the installed Mammoth, Dog and Wolf facts and the compiled horse body with one bite and two hooves. The carried R7-F5 terminal-decision regression passed.; The Mounted Shield contract passed 114 checks and its rollback child passed. The packet, production, and embedded icon shared SHA-256 fd519874e9d231946aad68c46f93a4e0a3a2ea04d4037807e6c7a4d36629f9d0. Both UMM binding checks passed.
- **Source inspection only:** The macOS and non-EAGAIN lease branches, Linux lease-break signal exposure, and the interval between releasing a lease and reading the bytes.; The cached permission check, post-publication size handling, recovery after a failed classification, and teardown of a session holding a raw descriptor.
- **Required gaps:** A completed Unix submission under writer contention and unavailable lease support, with process survival and zero premature removals.; The first readable fallback bytes during a permission change, the effective final archive mode, and successful retry after the terminal name disappears or its size changes.; Recovery after a post-move read failure, ordering with two pending entries, and descriptor closure when the owning service unloads.
- **Limitations:** The controller withheld the mixed developer-session transcript. The candidate-bound receipt establishes the Fast run. The additional standalone builds and component runs in the developer report could not be independently confirmed.; The candidate exercised Windows file operations and production C# over a simulated POSIX boundary on Windows. The retained Linux probe covered earlier filesystem primitives. It did not exercise the new lease or inspect the final archive mode. macOS and Unity Mono execution remain unverified.; The gate records nativeProof=false. The read-only review did not launch Kingmaker, build the candidate, or modify the workspace.

#### Summary

- **Delivered behavior:** New KMC horses omit the Mammoth's exact stock Bite-to-Trip grant while retaining the bite and both hooves. Existing sourceless copies are reported for explicit per-horse repair. The focused evidence supports that selection boundary and the frozen terminal decisions.
- **Scope reviewed:** Final PR-4 source/offline checkpoint for MMR-06, S06, the S08 regression neighbors, the carried R7-F5 obligation, and all eight pending findings.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** Decide whether to authorize further PR-4 review after the recorded eleventh slot.; N07 remains pending for a successful plain-horse bite and damage, with legitimate Trip controls where available.; N10 remains pending for a pre-repair horse across save/reload or an area transition, including pair, shield, and action behavior.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a21d6b-0d95-70a0-9af7-1b46b242bd94`, duration 740.5s, response sha256 `6dbb5766c2c560281443e484c08132b2f9bb91c02cf65773b1be7952416dbfab`, request `kmc-codex-mmr-horse-trip-20261125213249-4b8b-r11-49ad08`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "68a19050fa61244cc724988ae8772cb5f2b2d6f1",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The focused gate passed and the inherited stock TrippingBite is isolated at horse construction. Existing-save repair can still remove a fact from an unfinished request, expose an incomplete archive, or replace a correct Applied result with Interrupted. These outcomes block S06.\n\n## Continuation for Claude\n\nRound 11 uses the last recorded PR-4 review slot. Further review requires owner authorization. If work resumes, keep the correction within request admission, archive publication, recovery, and service teardown. Establish a completed submission on each supported platform and protect the archive through its first readable instant. Preserve a recovery entry's original name and classification across failures, settle older work before queued requests, and release retained file descriptors on unload. Exercise the cases in the findings with the focused filesystem harness, observe the effective Unix permissions, and bind the Fast gate to the corrected candidate. Preserve the verified horse selection, frozen decisions, and Windows handle behavior. N07 and N10 remain owner playtest observations.",
  "evidence_assessment": {
    "executed_verification": [
      "Read-only Git checks confirmed H and merge base M. The mission SHA-256 matched the enrollment, and the gate log SHA-256 matched its controller receipt, ba40b97ad3a1eef0f499d475b50e534154abb6b0369d536768bc0481686a1d49.",
      "The controller ran Test-Tiers.ps1 -Tier Fast -Focused Test-MmrHorseTripFocused.ps1 at H. It exited 0 in 43.0 seconds with 1,572 component passes, 153 source-validation passes, and 12 of 12 focused children passing.",
      "The Trip contract passed 52 checks, including the installed Mammoth, Dog and Wolf facts and the compiled horse body with one bite and two hooves. The carried R7-F5 terminal-decision regression passed.",
      "The Mounted Shield contract passed 114 checks and its rollback child passed. The packet, production, and embedded icon shared SHA-256 fd519874e9d231946aad68c46f93a4e0a3a2ea04d4037807e6c7a4d36629f9d0. Both UMM binding checks passed."
    ],
    "inspected": [
      "Controller mission, developer report, transcript notice, eight pending ledger entries, gate receipt and log, repository instructions, validation policy, and current handoffs.",
      "The 26-file feature diff and round-11 correction against 9f2d8c09, including horse construction, the removal gate, file operations, recovery fixtures, and the service that owns the repair session.",
      "The operation/failure table and retained round-10 Linux syscall probe and output."
    ],
    "limitations": [
      "The controller withheld the mixed developer-session transcript. The candidate-bound receipt establishes the Fast run. The additional standalone builds and component runs in the developer report could not be independently confirmed.",
      "The candidate exercised Windows file operations and production C# over a simulated POSIX boundary on Windows. The retained Linux probe covered earlier filesystem primitives. It did not exercise the new lease or inspect the final archive mode. macOS and Unity Mono execution remain unverified.",
      "The gate records nativeProof=false. The read-only review did not launch Kingmaker, build the candidate, or modify the workspace."
    ],
    "required_gaps": [
      "A completed Unix submission under writer contention and unavailable lease support, with process survival and zero premature removals.",
      "The first readable fallback bytes during a permission change, the effective final archive mode, and successful retry after the terminal name disappears or its size changes.",
      "Recovery after a post-move read failure, ordering with two pending entries, and descriptor closure when the owning service unloads."
    ],
    "source_inspection_only": [
      "The macOS and non-EAGAIN lease branches, Linux lease-break signal exposure, and the interval between releasing a lease and reading the bytes.",
      "The cached permission check, post-publication size handling, recovery after a failed classification, and teardown of a session holding a raw descriptor."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "macOS returns ENOTSUP from WriterLeaseFree. ReadEligible rejects only EAGAIN, so both the request and claim reads can accept the first line while the same writer remains open. Other Linux lease errors also fall through. Even a successful Linux lease is released before the read.",
      "blocking_rationale": "The completed two-horse submission requires zero removals. The first horse can already have lost its fact.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A deciding read obtains a completed, stable submission before authorizing removal.",
      "id": "R10-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs",
        "line": 678
      },
      "priority": "P2",
      "requested_correction": "Establish completion through the deciding read on each supported Unix path. Treat an unavailable or failed completion check as unresolved and retain a working single-horse submission path.",
      "requested_evidence": "Pause a writer after its first horse line under both the supported-lease and unavailable-lease paths. Complete the second line and observe a malformed refusal with zero removals. Retain a completed single-horse control.",
      "title": "Unix can still authorize a request before its writer finishes",
      "trigger": "A player writes directly to the documented request file and pauses between two horse lines on macOS or a Linux filesystem where acquiring the lease fails with an error other than EAGAIN."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "The fallback creates the terminal name before checking its mode. A volume that creates it as 0644 therefore exposes an empty archive immediately. On a permissions-preserving volume, the code caches the initial mode across retries. A permission restore after a partial write can expose those bytes. An equal-length edit after read-back can also pass the final device, inode, and size comparison.",
      "blocking_rationale": "A reader can see an empty, partial, or incorrect Applied archive for a removal that already occurred.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The first readable bytes under the terminal suffix are the complete bytes that authorized the decision. A refused publication keeps the claim recoverable.",
      "id": "R5-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs",
        "line": 750
      },
      "priority": "P2",
      "requested_correction": "Use a fallback that protects the complete payload through publication. Where that protection is unavailable, retain the claim without exposing a readable terminal entry.",
      "requested_evidence": "After descriptor-link failure, observe the terminal with a compatible reader on the effective-mode mismatch path and during a partial-write permission restore. Check the first readable bytes and retain the one-removal and foreign-replacement controls.",
      "title": "The fallback can expose the terminal archive before its payload is protected",
      "trigger": "Descriptor publication is unavailable and either the volume ignores mode zero or an ordinary same-account restore changes permissions while a failed write awaits retry."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "After File.Move, the original settled name and digest prefix exist only in local variables. A sharing violation during HoldsIdentityPrefix leaves the completed record at .claimed. On the next poll RecoverRetiring sees .claimed and returns, so the ordinary interruption path consumes it. For a .retiring entry, the short-circuit at line 1185 also skips the post-move eligibility check entirely.",
      "blocking_rationale": "Recovery can overwrite a valid Applied result or leave an ineligible replacement occupying the claim name.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The entry actually admitted for interruption is verified as unfinished. A completed or ineligible replacement retains its proper name through retries.",
      "id": "R10-F4",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs",
        "line": 1185
      },
      "priority": "P2",
      "requested_correction": "Preserve the recovery origin until classification and any return move succeed. Recheck eligibility for both recognized name shapes before allowing the ordinary claim lifecycle to consume the entry.",
      "requested_evidence": "Restore completed bytes before the recovery move while a writer permits rename but prevents the first verification read. Release it and retry in the same and a fresh session. Verify the settled record and Applied result/archive remain intact. Also substitute an ineligible retiring entry and verify its original name survives.",
      "title": "A failed post-move check loses the origin of a recovered entry",
      "trigger": "A settled entry is restored to its completed bytes after the scan, and its first verification at .claimed fails transiently."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "The source now passes 0600 to fchmod. The test prints SimulatedPosix.LastChmod, which records that argument. Its pathname lstat supplies a fixed 0644 for regular files. The retained Linux probe does not perform the final chmod and stat.",
      "blocking_rationale": "The prior finding explicitly requested observation of the published file's effective mode under a restrictive umask. The recorded value establishes the requested mode only.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "The published private archive has owner-only effective access and contains the original request bytes.",
      "id": "R10-F5",
      "location": {
        "file": "tests/HorseTripRepairPolicyTests.cs",
        "line": 695
      },
      "priority": "P2",
      "requested_correction": "Complete the focused permission observation and label the existing simulated value as the fchmod argument.",
      "requested_evidence": "Publish through the fallback for a private request under umask 077. Record the final file's observed mode and compare its bytes with the request. A bounded local filesystem probe is sufficient.",
      "title": "The final access mode of a private archive was not observed",
      "trigger": "The focused gate reports a final mode of 600 for protected creation."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "F_SETLEASE can deliver SIGIO to the lease holder when a conflicting open requests a break. The mod acquires the lease in the game process without arranging safe notification. Under the default Linux disposition, SIGIO terminates the process.",
      "blocking_rationale": "Ordinary file contention can terminate Kingmaker with unsaved play. The Windows simulation produces no lease-break signal.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Contention over the request delays or refuses the repair while the host process continues running.",
      "id": "R11-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs",
        "line": 613
      },
      "priority": "P1",
      "requested_correction": "Use a submission check with process-safe contention behavior. If retaining Linux leases, account for break notification without relying on or changing an undocumented game-wide signal disposition.",
      "requested_evidence": "Exercise overlapping reader and writer opens in a disposable Linux process using the selected mechanism. Record normal process survival, writer completion, and the resulting request decision.",
      "title": "A Linux lease break can signal the game process",
      "trigger": "An editor opens the request for writing after the read lease is acquired and before it is released."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "Every lstat error retains the same terminal name, including ENOENT. A size mismatch also retains Published=true. Subsequent polls keep checking the missing name or the same wrong size and skip the write that the diagnostic says will be retried.",
      "blocking_rationale": "The frozen claim can block all later repair requests for the rest of the session after the filesystem obstruction has cleared.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A transient lookup error retries verification. Confirmed loss or invalidation of the attempted archive has a bounded route to publishing the frozen decision.",
      "id": "R11-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs",
        "line": 787
      },
      "priority": "P2",
      "requested_correction": "Give confirmed absence and a changed published file explicit recovery transitions. Preserve other files and the original removal decision while selecting a safe publication target.",
      "requested_evidence": "After a fallback write fault, move its terminal file aside and leave the name vacant. Separately change the file length after chmod and before successful verification. In each case show that the same session retires the claim and admits a queued request with one original removal.",
      "title": "A missing or resized fallback archive can stall the repair queue indefinitely",
      "trigger": "The fallback terminal is moved away without a replacement, or its length changes before final verification."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "UnixPublicationAttempt retains a raw integer descriptor between polls. Successful HorseCompanionBlueprintService.Dispose discards the service without clearing the repair session. The descriptor has no finalizer or safe-handle owner.",
      "blocking_rationale": "A successful mod unload leaves a writable file descriptor open for the lifetime of the game. Repeated unloads with pending attempts can accumulate descriptors.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Successful service teardown closes every descriptor it owns and preserves the on-disk claim for conservative recovery.",
      "id": "R11-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/HorseCompanionBlueprintService.cs",
        "line": 533
      },
      "priority": "P2",
      "requested_correction": "Tie the retained attempt to successful service disposal and close it exactly once. Preserve retry state when teardown itself is refused.",
      "requested_evidence": "Leave a fallback attempt pending, invoke the owning teardown path, and observe descriptor closure. A fresh session should recover the claim without another fact removal.",
      "title": "Unloading the horse service leaks a pending Unix archive descriptor",
      "trigger": "UMM unloads the mod after a fallback write or metadata failure and before its next successful poll."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "RecoverRetiring examines only pending[0]. After returning that completed record to its original name, it returns with .claimed vacant. Process then takes the new request even when pending[1] is a genuine stranded claim.",
      "blocking_rationale": "A newer removal can run first, and the older interruption can subsequently overwrite its owner-facing result.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Genuine stranded claims receive their interruption result and archive before the poll admits a newer request.",
      "id": "R11-F4",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs",
        "line": 1187
      },
      "priority": "P2",
      "requested_correction": "Continue with the remaining recovery candidates, or defer new request admission until a subsequent scan establishes that no older work remains.",
      "requested_evidence": "Seed two recovery candidates and a queued request. Restore the oldest candidate to its completed bytes before the move. Verify that the second candidate is interrupted before the queued horse is processed and the completed record stays intact.",
      "title": "Returning one completed record lets a queued request overtake another pending claim",
      "trigger": "Two entries qualify during the recovery scan, and the oldest is recognized as completed only after the move."
    },
    {
      "acceptance_ids": [],
      "actual": "The production comment still describes fchmod(0644) and deletion of the failed attempt. The current branch requests 0600 and retains the descriptor across polls.",
      "blocking_rationale": "This is a maintenance comment. The executable behavior and its evidence are assessed separately.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "The comment describes the publication mode and failure ownership used by the implementation.",
      "id": "R11-F5",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs",
        "line": 528
      },
      "priority": "P3",
      "requested_correction": "Update the fallback comment alongside the implementation and remove the obsolete PublishedMode constant if it remains unused.",
      "requested_evidence": "Source inspection is sufficient.",
      "title": "The fallback comment still describes the previous permissions and cleanup",
      "trigger": "A maintainer consults the file-operation contract above LibcPosixFiles."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R10-F1",
      "rationale": "The fallback no longer unlinks a terminal pathname. The recorded replacement case preserved the foreign bytes, counted zero unlinks, published the original request once, and recorded one removal."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R10-F2",
      "rationale": "The simulated EAGAIN case passed. Production accepts ENOTSUP on macOS and other non-EAGAIN failures, and releases a successful lease before reading. A writer can still supply an unfinished decision."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R10-F3",
      "rationale": "Recovery recognizes the legacy 32-lowercase-hex retiring name. Both gate branches interrupted the stranded horse-B request before applying horse-C and preserved the legacy completed record."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R10-F4",
      "rationale": "The tested restore passed when the first post-move read succeeded. A transient failure at that read loses the settled name, allowing the next poll to report the same completed record as Interrupted. Retiring entries also bypass the post-move eligibility check."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R10-F5",
      "rationale": "The 0644 argument was corrected to 0600. The gate inspected LastChmod, and the Linux probe did not inspect a published archive under a restrictive umask. The requested final-mode observation remains outstanding."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R5-F1",
      "rationale": "The new check withholds payload writes when initial mode bits are wider than requested, but the terminal name already exists. Its mode is cached during retries, and a same-size content change can survive final verification. The simulated Windows share mode excludes readers and writers more strongly than these Unix operations."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R9-F3",
      "rationale": "Two injected initial fstat failures and one post-chmod lstat failure retained one attempt. The gate observed file counts of 3/3/3/3, one protected create, one exact archive, and one removal. R11-F2 concerns the distinct case of a definitively missing or changed target."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R9-F4",
      "rationale": "Both production matchers require the absolute end of the filename. The gate rejected trailing line feeds for both shapes, accepted the producer names, and preserved the older completed record and backup."
    }
  ],
  "request_id": "kmc-codex-mmr-horse-trip-20261125213249-4b8b-r11-49ad08",
  "round": 11,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "New KMC horses omit the Mammoth's exact stock Bite-to-Trip grant while retaining the bite and both hooves. Existing sourceless copies are reported for explicit per-horse repair. The focused evidence supports that selection boundary and the frozen terminal decisions.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "Decide whether to authorize further PR-4 review after the recorded eleventh slot.",
      "N07 remains pending for a successful plain-horse bite and damage, with legitimate Trip controls where available.",
      "N10 remains pending for a pre-repair horse across save/reload or an area transition, including pair, shield, and action behavior."
    ],
    "scope_reviewed": "Final PR-4 source/offline checkpoint for MMR-06, S06, the S08 regression neighbors, the carried R7-F5 obligation, and all eight pending findings.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
