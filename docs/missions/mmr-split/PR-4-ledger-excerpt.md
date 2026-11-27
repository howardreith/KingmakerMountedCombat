# PR-4 supplemental ledger excerpt (operator records, not controller output)

Scoped, read-only excerpt of `analysis-cache/mounted-alpha2-melee-pose-20261124/decomposition/TRANSITION-LEDGER.jsonl`, the
append-only, hash-chained operator ledger the owner asked for (each entry carries the SHA-256 of the previous line in
`prev_sha256`). The lines below are copied byte for byte; each is followed by its own SHA-256. Generated 2026-11-27T03:27:19+00:00.

```json
{"allocations": {"PR-1": 6, "PR-2": 3, "PR-3": 3, "PR-4": 2, "PR-5": 2, "PR-6": 2}, "budget_adjustment": "allowanceAdjustments entry 2026-11-26T02:32:18Z 90 -> 150 via Update-KmcWorkflowBudgetRecord (locked); balance 78.52 of 150, 71.48 remaining, 59 entries preserved", "instruction_file": "owner-continuations/owner-continuation-2026-11-26-pr1-extension-and-budget.md", "instruction_sha256": "16db6774779cfa369bc390f4af56465844fbf902ba161317b0d16b002a671972", "kind": "owner_decision", "prev_sha256": "f7775c23a15fc0491a5bf82a78c4b4c54981abdab0efe98803ff563e3061434c", "program_rounds": {"allocated_child": 18, "lifetime_cap": 25, "spent": 10}, "seq": 14, "text": "Bounded continuation: PR-1 ceiling 3 -> 6 (rounds 4-6 authorized); other allocations unchanged; program 18 allocated child reviews / 25 lifetime (10 spent); validation allowance 90 -> 150 min on the same budget ID; sequence PR-4, PR-5, then PR-1 resume, then PR-2, PR-3, PR-6.", "utc": "2026-11-26T02:32:30+00:00"}
```
SHA-256 `e5e75214fe978088fb099fc2d6e6e9c6721f9636ea313255ec8d6679c71404fa`

```json
{"branch": "codex/mmr-horse-trip", "enrollment": "kmc-codex-mmr-horse-trip-20261125213249-4b8b", "first_gate": "gate-focused-5099cd120474-d43d449b6c4-a3357fdbeb8-20261126T023359939397Z pass", "initial_push": "audited ordinary push; receipt push-audits/codex_mmr-horse-trip-5099cd12-2026-11-26T023246+0000.json", "kind": "enrollment", "max_rounds": 2, "pr": "PR-4", "pr_number": 66, "prev_sha256": "e5e75214fe978088fb099fc2d6e6e9c6721f9636ea313255ec8d6679c71404fa", "seq": 15, "target": "main", "utc": "2026-11-26T02:34:21+00:00"}
```
SHA-256 `0c7cd85902763e2647896b8ebebbc7dfe5ae82289ea7ee08aca4c76c40e20c67`

```json
{"budget": "79.45 of 150 min after the first gate (approx.); 0 unreconciled", "candidate": "the commit on codex/mmr-horse-trip whose parent is 5099cd12 and which adds docs/missions/mmr-split/PR-4-ledger-excerpt.md", "child_rounds": "0 of 2 used", "kind": "pre_review_check", "pr": "PR-4", "prev_sha256": "0c7cd85902763e2647896b8ebebbc7dfe5ae82289ea7ee08aca4c76c40e20c67", "program_rounds": "10 of 25 lifetime (3 of 18 allocated child reviews)", "round": 1, "seq": 16, "utc": "2026-11-26T02:34:21+00:00"}
```
SHA-256 `24fd98877f75f26165bcc5c63f3179058dd3368055c415c602f27908e0868354`

```json
{"candidate": "8fc887ad2ccd3d8f965634cc5a8697f4f6c7391d", "child_rounds": "1 of 2 used", "decision": "changes_required", "findings": ["R1-F1", "R1-F2", "R1-F3", "R1-F4", "R1-F5", "R1-F6", "R1-F7", "R1-F8"], "kind": "review_result", "pr": "PR-4", "prev_sha256": "24fd98877f75f26165bcc5c63f3179058dd3368055c415c602f27908e0868354", "program_rounds": "11 of 25 lifetime (4 of 18 allocated child reviews)", "round": 1, "seq": 17, "utc": "2026-11-26T02:59:29+00:00"}
```
SHA-256 `650e6c6632bc6210b5eed6dd71f2299a710e37d4b7be9bd8f7ca4063b82f741b`

```json
{"actual_at_round_1_submission": "80.01 of 150 min spent (within 90 as well); PR-4 0 of 2 rounds used before round 1; program 10 of 25 lifetime (3 of 18 allocated child reviews)", "correction": "seq 16 recorded the budget as approx. 79.45 min; the record shows the first gate ended at 80.01 min. Both are within the original 90 and the authorized 150.", "enrolled_mission_limits": "the PR-4 mission (5099cd12) states the limits of the 2026-11-25 approval: 90 validation minutes, 15 additional child rounds, 22 lifetime rounds", "kind": "reconciliation", "pr": "PR-4", "prev_sha256": "650e6c6632bc6210b5eed6dd71f2299a710e37d4b7be9bd8f7ca4063b82f741b", "re": "PR-4 review R1-F8", "seq": 18, "superseding_authorization": "owner instruction of 2026-11-26 (seq 14), committed verbatim as docs/missions/mmr-split/owner-authorization-2026-11-26.md, sha256 16db6774779cfa369bc390f4af56465844fbf902ba161317b0d16b002a671972: validation allowance 90 -> 150 on the same budget ID; PR-1 ceiling 3 -> 6; program 18 allocated child reviews, 25 lifetime; PR-4 stays at 2 rounds. The mission is hash-pinned and was not rewritten for the numbers (the owner said not to).", "utc": "2026-11-26T02:59:29+00:00"}
```
SHA-256 `a308d67c69983914aa7171d08d21bee5a23d3bad3836d14af165b1e28bc4718b`

```json
{"budget": "81.07 of 150 min spent before the round-2 gate; 0 unreconciled", "candidate": "the commit on codex/mmr-horse-trip that regenerates docs/missions/mmr-split/PR-4-ledger-excerpt.md with this entry, on top of the R1 corrections", "child_rounds": "1 of 2 used (round 2 is the last)", "kind": "pre_review_check", "pr": "PR-4", "prev_sha256": "a308d67c69983914aa7171d08d21bee5a23d3bad3836d14af165b1e28bc4718b", "program_rounds": "11 of 25 lifetime (4 of 18 allocated child reviews)", "round": 2, "seq": 19, "utc": "2026-11-26T02:59:29+00:00"}
```
SHA-256 `2d38699850836ceb48994b6f39486557e4210674195a31213863082f5137cd1b`

```json
{"child_rounds": "2 of 2 used", "enrollment": "kmc-codex-mmr-horse-trip-20261125213249-4b8b", "kind": "child_stop", "last_candidate": "b36f08a7570e43ee6a042c2d7af9ce8546a1c185", "next": "stop preserved per owner instruction 2026-11-26 section 2; continue with PR-5", "open_findings": ["R1-F1", "R1-F2", "R1-F4", "R2-F1"], "pr": "PR-4", "pr_number": 66, "prev_sha256": "2d38699850836ceb48994b6f39486557e4210674195a31213863082f5137cd1b", "program_rounds": "12 of 25 lifetime (5 of 18 allocated child reviews)", "reason": "review-round cap reached for final (2/2)", "seq": 20, "status": "BLOCKED", "utc": "2026-11-26T03:13:42+00:00", "verified_fixed_in_r2": ["R1-F3", "R1-F5", "R1-F6", "R1-F7", "R1-F8"]}
```
SHA-256 `bd4e651a21d7b3a3f7924e66e566b13f1f85202e3a9b1d4ac13d5aa8c9b86be7`

```json
{"correct": "from blocks_readiness in the latest accepted results: #65 e5735eb3 7 blocking/0 optional; #66 b36f08a7 4/0; #67 97dce9a9 7/2; #68 642cb481 1/0; total 19 blockers (12 defect, 7 evidence_gap) and 2 optional_polish", "error": "priority was treated as blocking status; PR-1's seven P2 findings were described as non-blocking", "kind": "correction", "preserved": "original handoff copied unchanged to FINAL-HANDOFF-2026-11-26.as-written-0715Z.md (sha256 ac2323a4b8f5d53bf9681e0e582767fb00eb4a4f854bee25c3d76a6990b5b0cf); review JSON, reviewer flags and controller findings untouched", "prev_sha256": "08f02c58d1abb39dcf0a3e18dd24fb502d83ee834d12bc16a359736da601ece8", "seq": 42, "source": "owner instruction 2026-11-26b section 1", "subject": "FINAL-HANDOFF-2026-11-26.md (07:15Z)", "utc": "2026-11-26T12:08:34+00:00"}
```
SHA-256 `19b938522a1a8ef48e3a6e681476c8a70a6b65fac5c1c98bf8bcdf122813d63d`

```json
{"instruction": "owner-continuations/owner-continuation-2026-11-26b-review-extension.md", "kind": "owner_decision", "prev_sha256": "19b938522a1a8ef48e3a6e681476c8a70a6b65fac5c1c98bf8bcdf122813d63d", "program": "26 child / 33 lifetime reviews (incl. PR #64's 7); 20 lifetime spent; 13 available (8 new + 5 reserved for PR-2 and PR-6)", "review_ceilings": {"PR-1 #65": 8, "PR-2": 3, "PR-3 #68": 5, "PR-4 #66": 4, "PR-5 #67": 4, "PR-6": 2}, "seq": 43, "sequence": ["#68", "#66", "#67", "#65", "PR-2", "PR-6"], "sha256": "8f45f1bbb55ae10e9ea45c9c9d9394995da85f752a2ee8ed01b7d8720767c261", "utc": "2026-11-26T12:08:34+00:00", "validation": "unchanged at 150 min; budget record read at 2026-11-26T12:08:34Z: budget mounted-alpha2-melee-pose-20261124: spent 100.79 of 150 min; remaining 49.22 min; entries 117 (running 0, interrupted 0, unreconciled 0)"}
```
SHA-256 `0cbfb0423e613d5f989cee870982da615afbad39bb4f6ab8d15d9594198619e0`

```json
{"after": "ACTIVE, 2 of 4, open R1-F1, R1-F2, R1-F4, R2-F1; streaks preserved", "authority": "owner instruction 2026-11-26b (docs/missions/mmr-split/owner-authorization-2026-11-26b.md)", "before": "BLOCKED (round cap 2 of 2)", "enrollment": "kmc-codex-mmr-horse-trip-20261125213249-4b8b", "kind": "resume", "operation": "agent_review.py resume --max-rounds 4", "pr": "PR-4", "prev_sha256": "0d989c6a1366d62b6ef61e8f1d82ebef0dbc2c488c30be63f1e5dae663f428d6", "records": "decomposition/status/pr4-pre-resume-status.json, pr4-resume.out, pr4-post-resume-status.json", "seq": 49, "utc": "2026-11-26T12:38:47+00:00"}
```
SHA-256 `5807b2f9f9cb053d7bddc42678c0f53c5d00d1501e92f5d47a604543797959d5`

```json
{"budget": "budget record read at 2026-11-26T12:38:47Z (before this entry): budget mounted-alpha2-melee-pose-20261124: spent 103.83 of 150 min; remaining 46.17 min; entries 121 (running 0, interrupted 0, unreconciled 0)", "candidate": "the commit on codex/mmr-horse-trip that regenerates docs/missions/mmr-split/PR-4-ledger-excerpt.md with this entry", "child_rounds": "2 of 4 used", "kind": "pre_review_check", "pr": "PR-4", "prev_sha256": "5807b2f9f9cb053d7bddc42678c0f53c5d00d1501e92f5d47a604543797959d5", "program_rounds": "21 of 33 lifetime (14 of 26 allocated child reviews)", "round": 3, "seq": 50, "utc": "2026-11-26T12:38:47+00:00"}
```
SHA-256 `4b869a5c89f1886733b33b43cb6c8dd7263c2c557012aaa55a22b2358e408378`

```json
{"budget": "budget record read at 2026-11-26T12:57:02Z: budget mounted-alpha2-melee-pose-20261124: spent 105.03 of 150 min; remaining 44.97 min; entries 123 (running 0, interrupted 0, unreconciled 0)", "candidate": "6369c5992c86df106c0ad91b9d4a6171abb57496", "child_rounds": "3 of 4", "controller_status": "BLOCKED: no progress: R1-F4 open for 3 consecutive reviews (preserved; not resumed, per owner instruction 2026-11-26b section 3)", "decision": "changes_required", "kind": "review_result", "open_blocking": ["R1-F4 (P2 defect: staged claim can change between its hash check and the final rename)", "R3-F1 (P1 defect: a diagnostic exception after an exact removal leaves the claim Undecided)", "R3-F2 (P2 defect: a failed final rename strands the staged claim outside recovery)"], "pr": "PR-4", "prev_sha256": "4b869a5c89f1886733b33b43cb6c8dd7263c2c557012aaa55a22b2358e408378", "program_rounds": "22 of 33 lifetime (15 of 26 allocated child reviews)", "records": "decomposition/pr4/controller-review-r3.json; status/pr4-r3-blocked-status.json", "resolved": ["R1-F1", "R1-F2", "R2-F1"], "round": 3, "seq": 51, "utc": "2026-11-26T12:57:04+00:00"}
```
SHA-256 `256d4d76706bf2095bf8379ad0636f9a3fed8b03c90e3eacfa94ecb34fd3492b`

```json
{"budget": "budget record read before this entry: budget mounted-alpha2-melee-pose-20261124: spent 121.44 of 150 min; remaining 28.57 min; entries 168 (running 0, interrupted 0, unreconciled 0)", "instruction": "owner-continuations/owner-continuation-2026-11-26c-continuation.md", "kind": "owner_decision", "prev_sha256": "d66eb124e73c85b5c939da75ecc16a8b1d48c85205e1e65c444ad69835395def", "seq": 65, "sha256": "cc5331e350c1b846ba798f34fc9b1d72cc274c07e3188ff6248117f9f04d3242", "summary": "PR-3 accepted for integration (H 8bfe701d, A 54311f1d); total caps PR-1 10, PR-2 3, PR-3 5 (unchanged), PR-4 5, PR-5 6, PR-6 2; program 31 child / 38 lifetime reviews (incl. 7 on PR #64); budget unchanged at 150 min; order #65 -> #67 -> #66 -> PR-2 -> PR-6; activate each child only when selected; a later stop is preserved and not repeatedly resumed", "utc": "2026-11-26T16:16:56+00:00"}
```
SHA-256 `d5c74ba8df5f472f99ea56ee7e899bde48461b715d627eada87598c1e3d850b5`

```json
{"after": "ACTIVE, 3 of 5", "authority": "owner instruction 2026-11-26c section 2 (docs/missions/mmr-split/owner-authorization-2026-11-26c.md)", "before": "BLOCKED (no progress: R1-F4 open for 3 consecutive reviews), 3 of 4", "enrollment": "kmc-codex-mmr-horse-trip-20261125213249-4b8b", "kind": "resume", "operation": "agent_review.py resume --max-rounds 5", "pr": "PR-4", "prev_sha256": "4c39ee553a55e6e8ed886e8288c0b5cc8cea00f382b15ad076a94e1420f5e0bd", "records": "decomposition/status/pr4-pre-resume-2-status.json, pr4-resume-2.out, pr4-post-resume-2-status.json", "seq": 78, "utc": "2026-11-26T17:50:24+00:00"}
```
SHA-256 `b7fe358f0bf45a92646aaf54eed0c6464e1dce29f40d2b443256642bc546bcec`

```json
{"budget": "budget record read at 2026-11-26T17:50:24Z (before this entry): budget mounted-alpha2-melee-pose-20261124: spent 134.78 of 150 min; remaining 15.22 min; entries 203 (running 0, interrupted 0, unreconciled 0)", "candidate": "the commit on codex/mmr-horse-trip that regenerates docs/missions/mmr-split/PR-4-ledger-excerpt.md with this entry", "child_rounds": "3 of 5 used", "kind": "pre_review_check", "pr": "PR-4", "prev_sha256": "b7fe358f0bf45a92646aaf54eed0c6464e1dce29f40d2b443256642bc546bcec", "program_rounds": "30 of 38 lifetime (23 of 31 allocated child reviews)", "round": 4, "seq": 79, "utc": "2026-11-26T17:50:24+00:00"}
```
SHA-256 `d6a00d7510b2535aaeb4449c7c588fe1f9d875866672ed56796d3929770cb6a0`

```json
{"candidate": "fd734bc2df4a47eef36fd8ae298a1b41f1ef734c", "controller_status": "ACTIVE (4 of 5)", "decision": "changes_required", "kind": "review_result", "open_blocking": ["R3-F1", "R3-F2", "R4-F1", "R4-F2", "R4-F3"], "open_optional": [], "pr": "PR-4", "prev_sha256": "d6a00d7510b2535aaeb4449c7c588fe1f9d875866672ed56796d3929770cb6a0", "records": "decomposition/pr4/developer-report.r4.json, pr4/controller-review-r4.json", "rejected": ["R3-F1", "R3-F2"], "round": 4, "seq": 80, "utc": "2026-11-26T18:05:54+00:00", "verified": ["R1-F4"]}
```
SHA-256 `fb6af3af3b250d4f66b04fbf38981cd98116764ef4612751c5c6d4d0db3e9a9f`

```json
{"budget": "budget record read at 2026-11-26T18:05:54Z (before this entry): budget mounted-alpha2-melee-pose-20261124: spent 136.27 of 150 min; remaining 13.73 min; entries 205 (running 0, interrupted 0, unreconciled 0)", "candidate": "the commit on codex/mmr-horse-trip that regenerates docs/missions/mmr-split/PR-4-ledger-excerpt.md with this entry", "child_rounds": "4 of 5 used (round 5 is the last)", "kind": "pre_review_check", "pr": "PR-4", "prev_sha256": "fb6af3af3b250d4f66b04fbf38981cd98116764ef4612751c5c6d4d0db3e9a9f", "program_rounds": "31 of 38 lifetime (24 of 31 allocated child reviews)", "round": 5, "seq": 81, "utc": "2026-11-26T18:05:54+00:00"}
```
SHA-256 `fffe81eab17528673d4bb62ec8743ffc2c4ff2b0c50b52ffcb8eda44a99c1b4e`

```json
{"budget": "budget record read at 2026-11-26T18:18:05Z: budget mounted-alpha2-melee-pose-20261124: spent 136.82 of 150 min; remaining 13.18 min; entries 206 (running 0, interrupted 0, unreconciled 0)", "candidate": "2b59c0e87c7f6ce8ea04f38373bd978334268963", "controller_status": "BLOCKED (review-round cap reached for final, 5/5); stop preserved: not resumed, no replacement enrollment", "decision": "changes_required", "kind": "review_result", "note": "every authorized feature child has reached a controller stop or acceptance; PR-2 and PR-6 cannot start", "open_blocking": ["R4-F3", "R4-F2", "R5-F1", "R5-F2", "R4-F1"], "open_optional": [], "pr": "PR-4", "prev_sha256": "fffe81eab17528673d4bb62ec8743ffc2c4ff2b0c50b52ffcb8eda44a99c1b4e", "program_rounds": "32 of 38 lifetime (25 of 31 allocated child reviews)", "records": "decomposition/pr4/developer-report.r5.json, pr4/controller-review-r5.json, status/pr4-post-review-r5-status.json", "rejected": ["R4-F1", "R4-F2", "R4-F3"], "round": 5, "seq": 82, "utc": "2026-11-26T18:18:05+00:00", "verified": ["R3-F1", "R3-F2"]}
```
SHA-256 `e84eacb070aa18e0ae4526635b6a4bccc7dfd8495d814f5dd2f8a8df4aa97d8e`

```json
{"authority": "owner instruction 2026-11-26d", "caps": {"PR-1": 13, "PR-2": 3, "PR-3": 5, "PR-4": 8, "PR-5": 8, "PR-6": 2}, "file": "owner-continuations/owner-continuation-2026-11-26d-continuation.md", "kind": "owner_instruction", "note": "PR-3 stays accepted and frozen (H 8bfe701d, A 54311f1d); ledger-61 correction retained", "order": "PR-1, PR-2, PR-5, PR-4, PR-6", "prev_sha256": "1e8c33c855f7387a8a6fec8570c5d5c1d8750c259a231f7f384b0e0b85ff8b9b", "program_ceilings": "39 child / 46 lifetime (including 7 on PR #64)", "seq": 84, "sha256": "cee6b6047f4c82db84a21b93cae3cf54dbcabfc9297c61546b2f3232ea866ca1", "utc": "2026-11-26T22:27:27+00:00"}
```
SHA-256 `85e25b3bd9eaefcdf82b4fa8444accf8a2b98f5c8d0a83890c1fe5213a498d0d`

```json
{"authority": "owner instruction 2026-11-26d section 2", "budget_id": "mounted-alpha2-melee-pose-20261124", "from_minutes": 150, "kind": "budget_adjustment", "mechanism": "WorkflowBudget.ps1 Update-KmcWorkflowBudgetRecord (locked), allowanceAdjustments entry appended", "prev_sha256": "85e25b3bd9eaefcdf82b4fa8444accf8a2b98f5c8d0a83890c1fe5213a498d0d", "result": "budget mounted-alpha2-melee-pose-20261124: spent 136.82 of 210 min; remaining 73.18 min; entries 206 (running 0, interrupted 0, unreconciled 0)", "seq": 85, "to_minutes": 210, "utc": "2026-11-26T22:27:27+00:00"}
```
SHA-256 `e3b6c0e2ffb6b9a24ae6fe0285b8f592c0f6a1761c93731ceb93316674ec96a7`

```json
{"authority": "owner instruction 2026-11-26d sections 2 and 4 (PR-1 at its cap, PR-2 blocked, PR-5 paused on an unsettled publication)", "enrollment": "kmc-codex-mmr-horse-trip-20261125213249-4b8b", "kind": "resume", "operation": "agent_review.py resume --max-rounds 8 (supported operation; no counters reset, no replacement enrollment)", "output": "owner-continuations/resume-pr4-26d-output.txt", "pr": "PR-4", "prev_sha256": "2201d426c3f87895093395d8c02e7a76e8ff50c2d6620609222ca19d7d19dbcf", "seq": 96, "utc": "2026-11-27T00:08:12+00:00"}
```
SHA-256 `c7a1b8eadcf334106139d71109189bc136c5b141c92b22a006c806c98aab3c14`

```json
{"budget": "budget mounted-alpha2-melee-pose-20261124: spent 149.77 of 210 min; remaining 60.23 min; entries 232 (running 0, interrupted 0, unreconciled 0)", "candidate": "the records commit that regenerates docs/missions/mmr-split/PR-4-ledger-excerpt.md with this entry", "child_rounds": "5 of 8 used", "invariant_map": {"entry_points": ["HorseTripRepairPolicy.Process (recovered/replacement claim -> session.Take + Decide(Interrupted) + Publish)", "HorseTripRepairPolicy.Publish", "HorseTripRepairFiles.Close (commit, private-name retirement, return of a replacement), Commit, RecoverRetiring via TryReadClaim"], "findings": ["R4-F1", "R4-F2", "R4-F3", "R5-F1", "R5-F2"], "fixture_outputs": "components 1555/0 (Windows): write and flush faults leave no terminal or private file, retry one exact archive, one removal; staged-archive edit excluded, rename-replacement refused then exact archive; B at retirement: own Interrupted result naming horse-B and archive, survives an obstructed archive, settles before C in same and fresh sessions, A removed once; locked recovered claim: 4 failing polls, 1 interruption archive, 1 result write, 0 removals, completes after release, then C runs; existing final-rename obstruction case kept; trip native contract 51/0; local focused 12/12", "invariant": "every terminal disposition (Applied, Refused, Superseded, Uncertain, Interrupted, including a recovered or replacement claim) is frozen on the session for its exact claim bytes and published by one retried sequence: result, staged archive commit (published under a held staging handle, verified), conditional retirement of the verified file only; retries finish only unfinished stages; a replacement keeps its bytes at the claim name and receives its own Interrupted result and archive before newer work"}, "kind": "pre_review_check", "pr": "PR-4", "prev_sha256": "c7a1b8eadcf334106139d71109189bc136c5b141c92b22a006c806c98aab3c14", "program_rounds": "36 reviews run of 46 lifetime (29 of 39 child; PR-5 round 7 ran but its verdict is unsettled) before this round", "round": 6, "seq": 97, "utc": "2026-11-27T00:16:10+00:00"}
```
SHA-256 `ed17e57af2fbf601827585a28e71b03c67a875a7be7ae8764afd9f8c34566045`

```json
{"budget": "budget mounted-alpha2-melee-pose-20261124: spent 150.32 of 210 min; remaining 59.68 min; entries 233 (running 0, interrupted 0, unreconciled 0), read 2026-11-27T00:29:03Z", "candidate": "729bda744312f657d331bc2ba1cb3497733791d0", "controller_status": "BLOCKED (no progress: R4-F3 open for 3 consecutive reviews); stop preserved: not resumed (owner instruction 2026-11-26d: a cap extension does not override a newly arising no-progress stop), no replacement enrollment", "decision": "changes_required", "kind": "review_result", "open_blocking": ["R4-F3", "R5-F1", "R6-F1", "R6-F2"], "pr": "PR-4", "prev_sha256": "ed17e57af2fbf601827585a28e71b03c67a875a7be7ae8764afd9f8c34566045", "round": 6, "seq": 98, "utc": "2026-11-27T00:29:19+00:00", "verified": ["R4-F1", "R4-F2", "R5-F2"]}
```
SHA-256 `6eed31481c283615225e65a61396fe7f1b167d23d1b3553f11b6470e54c29811`

```json
{"authority": "owner instruction 2026-11-26e", "budget": "210 minutes unchanged, no new minutes", "caps": {"PR-1": 15, "PR-2": 3, "PR-3": 5, "PR-4": 8, "PR-5": "9 after round-7 settlement", "PR-6": 2}, "exceptions": "one owner resume of PR-4's R4-F3 no-progress stop within cap 8; one ordinary resume to settle Bite round 7; one owner resume of an R4-F2 no-progress stop after settlement, cap 9", "file": "owner-continuations/owner-continuation-2026-11-26e-continuation.md", "kind": "owner_instruction", "prev_sha256": "6bc4aaa433798efb37014d888d7ae61d4e99be42aee3f96a80b2e03c7e6d122d", "program_ceilings": "42 child / 49 lifetime (including 7 on PR #64)", "seq": 100, "sha256": "e16e9b72bfe4588e59c6b0f3f628b0528a3d1c48e744b4dced67da5252b0354b", "utc": "2026-11-27T01:47:39+00:00"}
```
SHA-256 `79073cb9b25e833343d64310a17aa4dd32ae4fe6abdee84bd97ad85ad073eebc`

```json
{"authority": "owner instruction 2026-11-26e section 2 (one owner resume of the R4-F3 no-progress stop within cap 8)", "enrollment": "kmc-codex-mmr-horse-trip-20261125213249-4b8b", "kind": "resume", "operation": "agent_review.py resume (no --max-rounds; cap stays 8)", "output": "owner-continuations/resume-pr4-26e-output.txt", "pr": "PR-4", "prev_sha256": "b52ea848d0134acd1d7011d129c8e0ed35d9871881084625860b354472fd2e87", "seq": 112, "sequencing": "PR-2 round 1 returned 11 blocking findings that need a redesign of inherited route/allowance code; PR-4 (well-scoped, single authorized resume) taken while PR-2 waits; one child active at a time", "utc": "2026-11-27T03:01:05+00:00"}
```
SHA-256 `02c3d591a092cc2f3ee6ec2fa60ea002d90d37e8c4f29aa4d4cd4771a87b71ba`

```json
{"budget": "budget mounted-alpha2-melee-pose-20261124: spent 175.98 of 210 min; remaining 34.02 min; entries 254 (running 0, interrupted 0, unreconciled 0)", "candidate": "the records commit that regenerates docs/missions/mmr-split/PR-4-ledger-excerpt.md with this entry", "child_rounds": "6 of 8 used", "invariant_map": {"entry_points": ["HorseTripRepairFiles.Commit (Windows: VerifiedFileOperations.Open + RenameTo; failure: DeleteWhenClosed on the staged handle)", "HorseTripRepairFiles.Close retirement (Windows: VerifiedFileOperations.Open + verify + DeleteWhenClosed)", "HorseTripRepairSession.Take (resets CommittedArchive)", "HorseTripRepairPolicy.InterruptedResult"], "findings": ["R4-F3", "R5-F1", "R6-F1", "R6-F2"], "fixture_outputs": "components 1558/0 (Windows): staging rename-replacement excluded, terminal not readable before the staging handle closes and then exactly A; claim rename-replacement after verification excluded, only A deleted, one removal; replacement after A's committed archive and failed retirement gets its own interrupted archive and result in the same session, then C runs; fresh-session recovery after an applied removal reports 'This recovery made no further change; an earlier session may already have removed the fact' with the candidate-log guidance, zero further removals; existing write/flush, locked-recovery and replacement-result cases pass; trip native contract 51/0; local focused 12/12", "invariant": "publication and retirement act on the identity of the verified file through the final operation: on Windows the operation is performed through the exact exclusive handle that wrote (staged archive) or verified (claim) the bytes, so a directory-entry replacement cannot redirect it; elsewhere the bounded pathname contract applies; each claim has its own publication record; recovery reports only what it knows"}, "kind": "pre_review_check", "pr": "PR-4", "prev_sha256": "02c3d591a092cc2f3ee6ec2fa60ea002d90d37e8c4f29aa4d4cd4771a87b71ba", "program_rounds": "before this round: 33 executed child reviews of 42 (32 controller-recorded: PR-1 15, PR-2 1, PR-3 4, PR-4 6, PR-5 6; plus PR-5 round 7 executed, verdict unsettled, counted once), 40 executed lifetime of 49 (including 7 on PR #64); after it 34 / 41", "round": 7, "seq": 113, "utc": "2026-11-27T03:05:23+00:00"}
```
SHA-256 `d043ff021f2591918c253b055f8321afdc46c113ead740e7dee742ec770fdcac`

```json
{"budget": "budget mounted-alpha2-melee-pose-20261124: spent 179.4 of 210 min; remaining 30.6 min; entries 265 (running 0, interrupted 0, unreconciled 0)", "candidate": "the records commit that regenerates docs/missions/mmr-split/PR-4-ledger-excerpt.md with this entry", "child_rounds": "7 of 8 used (round 8 is the last)", "invariant_map": {"entry_points": ["VerifiedFileOperations.Open/Handle/RenameTo/DeleteWhenClosed (kernel32)", "HorseTripRepairFiles.Close retirement (both branches)", "HorseTripRepairFiles.Commit (both branches)"], "findings": ["R4-F3", "R5-F1", "R7-F1", "R7-F2"], "fixture_outputs": "components 1561/0 (Windows; the pathname branch exercised on Windows through ForcePathnameOperations): linked claim (hard link; unprivileged symbolic links not creatable here, Win32 error 1314) retires leaving the sibling target's bytes intact, ordinary retirement still works; pathname branch: replacement at the final retirement operation returns and settles before C, A removed once, nothing deleted; pathname publication: write failure leaves no terminal archive (incomplete moved aside), terminal readable only as A or not at all, retry one exact archive; round-7 Windows cases unchanged; trip native contract 52/0 including the compiled CreateFileW access-mask check", "invariant": "the final publication and retirement operations act only on the verified file: on Windows through the exact kernel32 handle (explicit DELETE access, no sharing, reparse points opened as themselves and refused); on other platforms no pathname deletion at all (retirement keeps the verified file under a settled name and returns a replacement; publication writes the payload through the exclusively created terminal name, an incomplete write is moved aside)"}, "kind": "pre_review_check", "pr": "PR-4", "prev_sha256": "d043ff021f2591918c253b055f8321afdc46c113ead740e7dee742ec770fdcac", "program_rounds": "before this round: 34 executed child reviews of 42 (33 controller-recorded: PR-1 15, PR-2 1, PR-3 4, PR-4 7, PR-5 6; plus PR-5 round 7 executed, verdict unsettled, counted once), 41 executed lifetime of 49; after it 35 / 42", "round": 8, "seq": 114, "utc": "2026-11-27T03:26:19+00:00"}
```
SHA-256 `1d87b587216d6d5c84d4a3a1680da81d0928e78e58cb02993f9c5d4c832a11e0`

```json
{"candidate": "abbf22e22cc448dd06cc18b94fb6a2f0897d9721", "controller_status": "ACTIVE (7 of 8)", "decision": "changes_required", "kind": "review_result", "new_blocking": ["R7-F1", "R7-F2"], "note": "recorded after the round-8 check (seq 114); appended late, original order preserved", "pr": "PR-4", "prev_sha256": "1d87b587216d6d5c84d4a3a1680da81d0928e78e58cb02993f9c5d4c832a11e0", "rejected": ["R4-F3 (Linux/Mono branch)", "R5-F1 (Linux/Mono branch)"], "round": 7, "seq": 115, "utc": "2026-11-27T03:27:19+00:00", "verified": ["R6-F1", "R6-F2"]}
```
SHA-256 `75516da4fb6f60f74d0858dbfc22156e0fb3504b2c202ab1d0d2d42bdadc69c1`

```json
{"correction": "The PR-4 ledger excerpt generated with seq 114 described seq 114 as the round-7 result; seq 114 is the round-8 check, and the round-7 result was appended late as seq 115.", "corrects_seq": 114, "kind": "correction", "pr": "PR-4", "prev_sha256": "75516da4fb6f60f74d0858dbfc22156e0fb3504b2c202ab1d0d2d42bdadc69c1", "seq": 116, "utc": "2026-11-27T03:27:19+00:00"}
```
SHA-256 `9f3d1e4dae2efaa889c83166c137232e81dc9a5cc1d7f646f29950bb8761a448`

Reconciliation (PR-4 review R1-F8): seq 14 is the owner decision of 2026-11-26 (verbatim text committed as
`docs/missions/mmr-split/owner-authorization-2026-11-26.md`, SHA-256 in seq 18). It raised the validation allowance of the
shared budget from 90 to 150 minutes (recorded in the budget record allowanceAdjustments, 2026-11-26T02:32:18Z) and the program
to 18 allocated child reviews / 25 lifetime; PR-4 stays at 2 rounds. The hash-pinned mission still states the earlier
90 / 15 / 22 limits because the owner said not to rewrite it for the numbers. Seq 18 corrects seq 16 (80.01, not approx. 79.45,
minutes after the first gate); seq 19 is the round-2 check and seq 20 the round-2 stop (round cap 2 of 2).
Seq 42 corrects the blocking counts in the final handoff (from blocks_readiness); seq 43 is the owner extension of 2026-11-26
(`docs/missions/mmr-split/owner-authorization-2026-11-26b.md`): PR-4 total ceiling 4, program 26 child / 33 lifetime reviews,
validation unchanged at 150 minutes. Seq 49 is the resume to a ceiling of 4, seq 50 the round-3 check, seq 51 the round-3 result;
seq 65 records owner instruction 2026-11-26c (total cap 5), seq 78 the resume to 5, seq 79 the round-4 check, seq 80 the round-4
result, seq 81 the round-5 check, seq 82 the round-5 result (cap stop at 5/5); seq 84-85 record owner instruction 2026-11-26d
(docs/missions/mmr-split/owner-authorization-2026-11-26d.md: PR-4 total cap 8, program 39 child / 46 lifetime, validation 210 minutes);
seq 96 is the resume to 8, seq 97 the round-6 check, seq 98 the round-6 result (no-progress stop); seq 100 owner instruction 2026-11-26e (one owner resume within 8), seq 112 that resume; seq 113 the round-7 check, seq 114 the round-8 check, seq 115 the round-7 result (appended late), seq 116 the correction of that order; the round-8 check is seq 114.
