# PR-4 supplemental ledger excerpt (operator records, not controller output)

Scoped, read-only excerpt of `analysis-cache/mounted-alpha2-melee-pose-20261124/decomposition/TRANSITION-LEDGER.jsonl`, the
append-only, hash-chained operator ledger the owner asked for (each entry carries the SHA-256 of the previous line in
`prev_sha256`). The lines below are copied byte for byte; each is followed by its own SHA-256. Generated 2026-11-27T19:00:26+00:00.

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

```json
{"candidate": "3eb85036481e17f87b01c577ccd81d9695fb3dd1", "controller_status": "BLOCKED (review-round cap reached for final, 8/8); stop preserved: not resumed, no replacement enrollment", "decision": "changes_required", "kind": "review_result", "open_blocking": ["R4-F3", "R5-F1", "R8-F1"], "pr": "PR-4", "prev_sha256": "9f3d1e4dae2efaa889c83166c137232e81dc9a5cc1d7f646f29950bb8761a448", "round": 8, "seq": 117, "utc": "2026-11-27T03:42:24+00:00", "verified": ["R7-F1", "R7-F2"]}
```
SHA-256 `700b439ec9cdb6d88162700b620091864546faf3116c05a7d2d3173a6ffe0f4b`

```json
{"accepted_source": {"PR-1": "H aa15e1f2 A f7edd298", "PR-3": "H 8bfe701d A 54311f1d"}, "authority": "owner instruction 2026-11-26f", "budget": "210 -> 270", "execution_allowances": {"PR-1": 15, "PR-2": 3, "PR-3": 5, "PR-4": 10, "PR-5": 10, "PR-6": 2}, "file": "owner-continuations/owner-continuation-2026-11-26f-continuation.md", "file_sha256": "dddeea571f7e09ab1026929b3565b5195db75f6a3e04828fd7273b5c7e186a8a", "kind": "owner_instruction", "pr4": "supported resume of the 8/8 cap stop to completed-round cap 10", "pr5": "transport activation invalidates round 7; completed-round cap 9, execution ceiling 10; one conditional R4-F2 no-progress resume", "prev_sha256": "4cb4c278ca4abcdd063e16c18437d65c63b399e1152bf0ecdacaac2ce6ebae60", "product_ceilings": "45 child / 52 lifetime executions; plus at most 2 infrastructure reviews (combined 54)", "seq": 119, "utc": "2026-11-27T04:52:35+00:00"}
```
SHA-256 `9092246a73e2b3a3eaa3f003d7cf4048a11143cc38273201db147cda5158bda5`

```json
{"authority": "owner instruction 2026-11-26f section 3", "before": "spent 180.6 of 210 min; remaining 29.4 min; entries 267", "budget_id": "mounted-alpha2-melee-pose-20261124", "from_minutes": 210, "kind": "budget_adjustment", "mechanism": "WorkflowBudget.ps1 Update-KmcWorkflowBudgetRecord (locked), allowanceAdjustments entry appended", "prev_sha256": "9092246a73e2b3a3eaa3f003d7cf4048a11143cc38273201db147cda5158bda5", "result": "budget mounted-alpha2-melee-pose-20261124: spent 180.6 of 270 min; remaining 89.4 min; entries 267 (running 0, interrupted 0, unreconciled 0)", "seq": 120, "to_minutes": 270, "utc": "2026-11-27T04:52:35+00:00"}
```
SHA-256 `55b58e38294a4d42524eb50c4dc832c2c86072f7b6685b5b2a706bf406bb11c3`

```json
{"authority": "owner instruction 2026-11-26f sections 2 and 7 (completed-round cap 10, supported resume from the 8/8 stop recorded in owner-continuations/resume-pr4-26f-output.txt)", "blocking_evidence": "R4-F3, R5-F1 (pathname branch, Windows fallback execution), R8-F1 (Windows handle branch; actual junctions and a simulated symbolic-link boundary); components 1563/0; operation/failure table committed", "budget": "budget mounted-alpha2-melee-pose-20261124: spent 220.8 of 270 min; remaining 49.2 min; entries 322 (running 0, interrupted 0, unreconciled 0)", "candidate": "the records commit that regenerates docs/missions/mmr-split/PR-4-ledger-excerpt.md with this entry (source commit 9871ac2d)", "child_rounds": "8 of 10 before this round; 9 of 10 after", "kind": "pre_review_check", "pr": "PR-4", "prev_sha256": "0f366ba8fc2dee098724f77110c50fd8cd67a67877fe9e11a1ed3b017d752278", "program": "before: product child 37 of 45, lifetime 44 of 52, infrastructure 2 of 2 (combined 46 of 54); after: 38 / 45, 45 / 52, combined 47 / 54", "round": 9, "seq": 132, "utc": "2026-11-27T06:43:07+00:00"}
```
SHA-256 `35cebc8879c29af965a22b637bfe7980a6356816d956e0a0a33432ef63423f57`

```json
{"candidate": "239264fc7670da9af35ca0d0428dc37073c210b5", "controller_status": "BLOCKED: no progress (R5-F1 open for 3 consecutive reviews) at 9 of 10 completed rounds", "decision": "changes_required", "disposition": "stop preserved per owner instruction 2026-11-26f section 2 (the no-progress exception covers only Bite); not resumed", "executions": "PR-4 9 of 10; product child 38 of 45; lifetime 45 of 52; infrastructure 2 of 2; combined 47 of 54", "kind": "child_stop", "open_blocking": ["R5-F1", "R9-F1", "R9-F2", "R9-F3", "R9-F4"], "pr": "PR-4", "prev_sha256": "35cebc8879c29af965a22b637bfe7980a6356816d956e0a0a33432ef63423f57", "round": 9, "seq": 133, "utc": "2026-11-27T06:59:19+00:00", "verified": ["R4-F3", "R8-F1"]}
```
SHA-256 `7f98b6a096bd1dc87eabf7ad3391262e53098bdfc98fd32e0eea198171d4353a`

```json
{"accepted_source": {"PR-1": "H aa15e1f2 A f7edd298", "PR-3": "H 8bfe701d A 54311f1d"}, "authority": "owner instruction 2026-11-27a", "budget": "270 -> 300", "execution_allowances": {"PR-1": 15, "PR-2": 5, "PR-3": 5, "PR-4": 11, "PR-5": 10, "PR-6": 2}, "file": "owner-continuations/owner-continuation-2026-11-27a-continuation.md", "file_sha256": "ef66406f4cbe61c2179541e1cc13a6cab59ac8b0f7c13d006d5bb93ac0df3e2d", "infrastructure": "supported cancellation of devproc-claude-gh-transport-literal-escapes-20261126235651-28f5 only; continue branch claude/gh-transport-literal-escapes and PR #8 unenrolled with standalone validation; reviews 3 and 4", "kind": "owner_instruction", "pr5": "after acceptance: separate immutable install, activate on Bite only, invalidate round 7 via supported route, fresh round 8 (execution 8, completed result 7); one conditional R4-F2 no-progress resume within cap 9 / ceiling 10", "prev_sha256": "17ee49b1cd28a6853713f18560952f92734ed652f50870ed5884e2e1b45e60a3", "product_ceilings": "48 child / 55 lifetime executions (incl. PR #64's 7); infrastructure 4 total (2 spent); combined 59", "resumptions": "PR-2 no-progress stop and exhausted cap -> completed-round cap 5; PR-4 no-progress stop on R5-F1 at 9 -> cap 11", "seq": 136, "utc": "2026-11-27T13:14:58+00:00"}
```
SHA-256 `0b6c25ca98dee0b5ba9c981ef55a74ab5183cfcf13780d9116e708320bb1096e`

```json
{"authority": "owner instruction 2026-11-27a section 3", "before": "spent 221.5 of 270 min; remaining 48.5 min; entries 323", "budget_id": "mounted-alpha2-melee-pose-20261124", "from_minutes": 270, "kind": "budget_adjustment", "mechanism": "WorkflowBudget.ps1 Update-KmcWorkflowBudgetRecord (locked), allowanceAdjustments entry appended", "prev_sha256": "0b6c25ca98dee0b5ba9c981ef55a74ab5183cfcf13780d9116e708320bb1096e", "result": "budget mounted-alpha2-melee-pose-20261124: spent 221.5 of 300 min; remaining 78.5 min; entries 323 (running 0, interrupted 0, unreconciled 0)", "seq": 137, "to_minutes": 300, "utc": "2026-11-27T13:14:58+00:00"}
```
SHA-256 `0e91ae108f446f18755ccf47040e18cfe644094d28a88921001e86e58d09cddc`

```json
{"authority": "owner instruction 2026-11-27a section 2 (Prospective stopping policy)", "change": "From this authorization onward the operator-maintained rule requiring an owner stop after two consecutive rejections of a carried finding is superseded for the existing remediation enrollments (PR-2, PR-4, PR-5). The controller's own no-progress stops, review caps and all blocking findings are preserved; no automatic resumption of a future controller stop. Frozen mission snapshots are not edited; the rule's earlier effect stands.", "kind": "owner_policy_change", "prev_sha256": "df82fdc5f36970275325edb307ebf7aa6ee58c22b692701b0634db6de219ecdf", "seq": 139, "utc": "2026-11-27T13:14:58+00:00"}
```
SHA-256 `6a2dbf5a9d3b59eb7a6a8291dae78638303c40ac404203c39ca0c2b12c02d02b`

```json
{"after": "ACTIVE, 9 of 11", "authority": "owner instruction 2026-11-27a section 2", "before": "BLOCKED (no progress: R5-F1 open for 3 consecutive reviews), 9 of 10", "enrollment": "kmc-codex-mmr-horse-trip-20261125213249-4b8b", "kind": "resume", "operation": "agent_review.py resume --max-rounds 11", "pr": "PR-4", "prev_sha256": "910be46407befee7a7636b7d4d855ff3cbe2400703da3d49238a32f54b763c43", "records": "owner-continuations/pre-resume-pr4-27a-status.json, resume-pr4-27a-output.txt", "seq": 142, "utc": "2026-11-27T13:39:30+00:00"}
```
SHA-256 `82b4a76633d8d1fdd221e1b331d612131a969754bb4f301847f8fe5d1345ba08`

```json
{"authority": "owner instruction 2026-11-27a sections 2 and 7 (cap 11; prospective policy: the operator two-rejection stop is superseded; controller no-progress stops preserved)", "budget": "spent 242.9 of 300 min; remaining 57.1 min", "candidate": "the records commit that regenerates docs/missions/mmr-split/PR-4-ledger-excerpt.md with this entry (source commits 20941d31, fb5d6b01)", "child_rounds": "9 of 11 before; 10 of 11 after", "counterexamples": "R5-F1 staging replacement/in-place change/alternate path after descriptor-link failure (no staging name; protected create; replacement refused); R9-F3 obstructed terminal across 3 polls under both mechanisms; R9-F1 writer held across polls then completed -> Malformed, 0 removals; R9-F2 link substituted at the open boundary for an undecided claim and a recovery entry; R9-F4 fresh session with older completed record, Applied result/archive and backup", "executions": "product child 38 -> 39 of 48; lifetime 45 -> 46 of 55; infrastructure 3 of 4 (review 4 pending); combined 48 -> 49 of 59", "gate": "focused pass 72.4 s, gate-focused-fb5d6b01beb2-d43d449b6c4-a3357fdbeb8-20261127T140401675427Z; components 1568/0", "kind": "pre_review_check", "pr": "PR-4", "prev_sha256": "d9f60c73c3c5fd4207d19a0630223d18fc59a3e3e76f13da23b4feb881f16757", "production_path": "HorseTripRepairPolicy.Process/HorseTripRepairFiles on real folders; Windows handle branch native; Unix branch over SimulatedPosix (simulated boundary); Linux syscall semantics by Perl probe on Debian 13/WSL2", "round": 10, "seq": 144, "utc": "2026-11-27T14:05:03+00:00"}
```
SHA-256 `c7c023fe9084d989592d39ce902caa36952f3337ffeef697f8e6076953b138f8`

```json
{"candidate": "9f2d8c0978c415a35c19f5ede3e4b0cf2997208f", "controller_status": "ACTIVE (10 of 11)", "decision": "changes_required", "executions": "PR-4 10 of 11; product child 39 of 48; lifetime 46 of 55; infrastructure 4 of 4; combined 50 of 59", "kind": "review_result", "new_blocking": ["R10-F1", "R10-F2", "R10-F3", "R10-F4", "R10-F5"], "pr": "PR-4", "prev_sha256": "d30c3d8639ff62ec38007a382a614da8890fbbb2a75eb2663f700e05b8bf9d61", "rejected": ["R5-F1", "R9-F3", "R9-F4"], "round": 10, "seq": 147, "utc": "2026-11-27T14:23:21+00:00", "verified": ["R9-F1", "R9-F2"]}
```
SHA-256 `4f13a91f6cd69c8797e18b42c26f61462eb07e2f1f8f72442e158162d9acde8d`

```json
{"authority": "owner instruction 2026-11-27a sections 2 and 7 (cap 11; the last allowed round)", "budget": "spent 263.9 of 300 min; remaining 36.1 min", "candidate": "the records commit that regenerates docs/missions/mmr-split/PR-4-ledger-excerpt.md with this entry (source commit 1d6b0836)", "child_rounds": "10 of 11 before; 11 of 11 after", "components": "1572/0", "counterexamples": "R10-F1 replacement after the fault; R9-F3 repeated fstat and transient lstat failures; R5-F1 mount-defined permissions; R10-F2 Unix writer paused across two polls; R9-F4 trailing line feed (matcher); R10-F3 round-8 retiring entry before a queued request; R10-F4 completed record restored before the move; R10-F5 final mode", "executions": "product child 39 -> 40 of 48; lifetime 46 -> 47 of 55; infrastructure 4 of 4; combined 50 -> 51 of 59", "kind": "pre_review_check", "pr": "PR-4", "prev_sha256": "4f13a91f6cd69c8797e18b42c26f61462eb07e2f1f8f72442e158162d9acde8d", "production_path": "HorseTripRepairFiles/UnixFiles on real folders; Unix branch over SimulatedPosix (simulated boundary); Windows native", "round": 11, "seq": 148, "utc": "2026-11-27T14:30:13+00:00"}
```
SHA-256 `1c3e5664b5ed5ee9650837945a255be5ee9401ef028003cc067c2dab3f7d85a7`

```json
{"candidate": "68a19050fa61244cc724988ae8772cb5f2b2d6f1", "controller_status": "BLOCKED: review-round cap reached for final (11/11)", "decision": "changes_required", "disposition": "cap stop preserved (owner instruction 2026-11-27a: allowance 11 exhausted; no further resume authorized)", "executions": "PR-4 11 of 11; product child 40 of 48; lifetime 47 of 55; infrastructure 4 of 4; combined 51 of 59", "kind": "child_stop", "open_blocking": ["R5-F1", "R10-F2", "R10-F4", "R10-F5", "R11-F1", "R11-F2", "R11-F3", "R11-F4", "R11-F5"], "pr": "PR-4", "prev_sha256": "1c3e5664b5ed5ee9650837945a255be5ee9401ef028003cc067c2dab3f7d85a7", "round": 11, "seq": 149, "utc": "2026-11-27T14:44:05+00:00"}
```
SHA-256 `0838a29ee4472d92c51884f8a4ee0e4e48f4ae667639f8eed5b175bc14076a69`

```json
{"authority": "owner instruction 2026-11-27b", "budget": "300 -> 360", "ceilings": "51 child / 58 lifetime / 6 infrastructure; 64 combined", "execution_allowances": {"PR-1": 15, "PR-2": 5, "PR-3": 5, "PR-4": 14, "PR-5": "completed-round cap 9, execution ceiling 10", "PR-6": 2, "transport": 6}, "file": "owner-continuations/owner-continuation-2026-11-27b-continuation.md", "file_sha256": "d98eab757e164fe411777b8f2061932ecea5d45450d4bdfaadbef63d06492a34", "kind": "owner_instruction", "pr4": "supported resume of the 11/11 cap stop to 14, then a separate resume --amend-mission replacing the file-command repair protocol with a confirmed in-game action", "prev_sha256": "38b301b052e4ca39f4e2a5a7d49c572e87f517257068eb5d584aa9c677768160", "seq": 152, "transport": "bounded completion of the Link failure family, complete unit gate, reviews 5 (and 6)", "utc": "2026-11-27T15:07:42+00:00"}
```
SHA-256 `21400c4fbb6084167dc0dc747e52bc76471d1f3fe59e6846f272012d8e6f90da`

```json
{"authority": "owner instruction 2026-11-27b section 2", "before": "spent 264.6 of 300; remaining 35.4; entries 349", "budget_id": "mounted-alpha2-melee-pose-20261124", "from_minutes": 300, "kind": "budget_adjustment", "mechanism": "WorkflowBudget.ps1 Update-KmcWorkflowBudgetRecord (locked), allowanceAdjustments entry appended", "note": "the adjustment authorizingText reads I authorize increasing the existing shared validation allowance from 300 to 360 minutes (a paraphrase); the verbatim owner text is: Increase the existing shared validation allowance from 300 to 360 minutes using the locked allowance-adjustment mechanism and this authorization (the record cites the instruction file and its sha256)", "prev_sha256": "21400c4fbb6084167dc0dc747e52bc76471d1f3fe59e6846f272012d8e6f90da", "result": "spent 264.6 of 360 min; remaining 95.4 min; entries 349 (running 0, interrupted 0, unreconciled 0)", "seq": 153, "to_minutes": 360, "utc": "2026-11-27T15:07:42+00:00"}
```
SHA-256 `8f0a12e31176bc70e4f0a9d03755a89609eb22143cf67bda5cf20aedb5125844`

```json
{"after": "ACTIVE, 11 of 14", "authority": "owner instruction 2026-11-27b section 2", "before": "BLOCKED (review-round cap 11/11), no pending publications, no developer or lock, last round 11 accepted", "enrollment": "kmc-codex-mmr-horse-trip-20261125213249-4b8b", "kind": "resume", "operation": "agent_review.py resume --max-rounds 14", "pr": "PR-4", "prev_sha256": "d0db221a16fed8055a09d0d9183fa64e0feaabdeaf383876c283bcb6264c577c", "records": "owner-continuations/pre-resume-pr4-27b-status.json, resume-pr4-27b-output.txt", "seq": 156, "utc": "2026-11-27T15:28:56+00:00"}
```
SHA-256 `39ac1d9d1887f0743014e4caa57e861a1d4fb76ee07cc24cb6e73bbeab155acd`

```json
{"authority": "owner instruction 2026-11-27b section 4", "kind": "mission_amendment", "operation": "agent_review.py resume --amend-mission owner-continuation-2026-11-27b-continuation.md (separate from the cap resume; foreground; no guidance route)", "pr": "PR-4", "prev_sha256": "39ac1d9d1887f0743014e4caa57e861a1d4fb76ee07cc24cb6e73bbeab155acd", "report_citation": "prepared under owner amendments revision 1 sha256 f9410bf809e35a83f8293bb5b54cfd344594aa0c2e5b90e633468c216e761b28", "result": "OWNER AMENDMENT revision 1, sha256 f9410bf809e35a83f8293bb5b54cfd344594aa0c2e5b90e633468c216e761b28 (message sha256 d98eab75...), integrity intact; effective task read in full (status --instructions parts 1 and 2 saved under owner-continuations/amend-pr4-27b-*)", "seq": 157, "utc": "2026-11-27T15:28:56+00:00"}
```
SHA-256 `3bfac2657aa7ff65e71d4999d4b3b0738f0a4394ecd534d17636bdc75995d66b`

```json
{"authority": "owner instruction 2026-11-27b sections 2 and 4 (cap 14; OWNER AMENDMENT revision 1)", "budget": "spent 307.7 of 360 min", "candidate": "the records commit that regenerates docs/missions/mmr-split/PR-4-ledger-excerpt.md with this entry (source commit 780d61f6)", "checks": "components 1537/0, trip native contract 50/0, Validate-Source 153/0", "child_rounds": "11 of 14 before; 12 of 14 after", "dispositions": "R5-F1, R10-F2, R10-F4, R10-F5, R11-F1..R11-F5 rebutted citing OWNER AMENDMENT 1 (withdrawn file protocol, code removed, replacement path evidence)", "executions": "product child 42 -> 43 of 51; lifetime 49 -> 50 of 58; infrastructure 6 of 6; combined 55 -> 56 of 64", "kind": "pre_review_check", "pr": "PR-4", "prev_sha256": "747ead57126b56f43a494a33edf69b33539cde4269d60ddfba5384a41a213dac", "report_citation": "prepared under owner amendments revision 1 sha256 f9410bf809e35a83f8293bb5b54cfd344594aa0c2e5b90e633468c216e761b28", "round": 12, "seq": 165, "utc": "2026-11-27T16:16:41+00:00"}
```
SHA-256 `b7e46ad736d3db1f1b5a1774792428e9db487159c9ae5b1aace0b9358c49ac7b`

```json
{"candidate": "e04a342b9214b502e48b19de28d090ed0855aeb0", "controller_status": "ACTIVE (12 of 14)", "decision": "changes_required", "executions": "PR-4 12 of 14; product child 43 of 51; lifetime 50 of 58; infrastructure 6 of 6; combined 56 of 64", "kind": "review_result", "new_blocking": ["R12-F1", "R12-F2", "R12-F3", "R12-F4", "R12-F5", "R12-F6", "R12-F7"], "note": "a first submission at 16:17Z was refused by the controller (exit 4) until the amended task was displayed in-session; no round was consumed", "optional": ["R12-F8", "R12-F9", "R12-F10"], "pr": "PR-4", "prev_sha256": "b7e46ad736d3db1f1b5a1774792428e9db487159c9ae5b1aace0b9358c49ac7b", "rebuttal_accepted": ["R5-F1", "R10-F2", "R10-F4", "R10-F5", "R11-F1", "R11-F2", "R11-F3", "R11-F4", "R11-F5"], "round": 12, "seq": 166, "utc": "2026-11-27T16:34:44+00:00"}
```
SHA-256 `aaed90e93c9affdf46ecb74ac890816154a9438d48cfd174d252d8e170f20abb`

```json
{"clarification": "seq 149 listed R11-F5 in open_blocking; the controller records R11-F5 as optional polish (blocks_readiness false). The round-11 stop left 8 blocking findings (R5-F1, R10-F2, R10-F4, R10-F5, R11-F1, R11-F2, R11-F3, R11-F4) and 1 optional (R11-F5). Seq 149 and its execution totals are preserved unchanged.", "clarifies": "seq 149 (round-11 child stop)", "finding": "R12-F10 (optional)", "kind": "clarification", "pr": "PR-4", "prev_sha256": "aaed90e93c9affdf46ecb74ac890816154a9438d48cfd174d252d8e170f20abb", "seq": 167, "utc": "2026-11-27T16:40:26+00:00"}
```
SHA-256 `905bfb1479ab147bd0eabfe609c41d6730bed30ba0cda40edf17c415e31eec43`

```json
{"authority": "owner instruction 2026-11-27b (OWNER AMENDMENT revision 1)", "budget": "spent ~309.6 of 360 min", "candidate": "the records commit that regenerates docs/missions/mmr-split/PR-4-ledger-excerpt.md with this entry (source commit 525b9011)", "checks": "components 1541/0, trip native contract 51/0, Validate-Source 153/0, companion readers", "child_rounds": "12 of 14 before; 13 of 14 after", "executions": "product child 43 -> 44 of 51; lifetime 50 -> 51 of 58; combined 56 -> 57 of 64", "kind": "pre_review_check", "pr": "PR-4", "prev_sha256": "905bfb1479ab147bd0eabfe609c41d6730bed30ba0cda40edf17c415e31eec43", "report_citation": "prepared under owner amendments revision 1 sha256 f9410bf809e35a83f8293bb5b54cfd344594aa0c2e5b90e633468c216e761b28", "round": 13, "seq": 168, "utc": "2026-11-27T16:41:12+00:00"}
```
SHA-256 `63fb0140181be310ac98cc87b8e1f5cfec28f37eda713f5d22d24a2d0028192c`

```json
{"child_rounds": "13 of 14", "decision": "changes_required", "kind": "review_result", "new": ["R13-F1", "R13-F2", "R13-F3"], "pr": "PR-4", "prev_sha256": "63fb0140181be310ac98cc87b8e1f5cfec28f37eda713f5d22d24a2d0028192c", "rejected": ["R12-F1", "R12-F2", "R12-F4", "R12-F6", "R12-F7", "R12-F9"], "round": 13, "seq": 169, "utc": "2026-11-27T17:04:23+00:00", "verified": ["R12-F3", "R12-F5", "R12-F8", "R12-F10"]}
```
SHA-256 `806fce9a17c936f8c5cc3ebf682f184fe9eaf6d4837fd2134e98d577c49a13c4`

```json
{"authority": "owner instruction 2026-11-27b (OWNER AMENDMENT revision 1)", "budget": "spent ~310.6 of 360 min", "candidate": "the records commit regenerating PR-4-ledger-excerpt.md with this entry (source commit 0718e824)", "checks": "components 1547/0, trip native contract 51/0, companion readers 80, Validate-Source 153/0", "child_rounds": "13 of 14 before; 14 of 14 after (final authorized PR-4 round)", "executions": "product child 44 -> 45 of 51; lifetime 51 -> 52 of 58; combined 57 -> 58 of 64", "kind": "pre_review_check", "pr": "PR-4", "prev_sha256": "806fce9a17c936f8c5cc3ebf682f184fe9eaf6d4837fd2134e98d577c49a13c4", "report_citation": "prepared under owner amendments revision 1 sha256 f9410bf809e35a83f8293bb5b54cfd344594aa0c2e5b90e633468c216e761b28", "round": 14, "seq": 170, "utc": "2026-11-27T17:04:23+00:00"}
```
SHA-256 `1e670393d28c412fae6f0cc10d73b527e139ecf193470214ae9040c5984ab434`

```json
{"candidate": "93d806b0340045886c3e8aac64764f334c50519d", "controller_status": "BLOCKED: no progress: R12-F1, R12-F6 open for 3 consecutive reviews (also review-round cap 14/14 reached)", "decision": "changes_required", "disposition": "stop preserved; allowance 14 exhausted; no further resume authorized; no unreviewed change pushed to the enrolled branch", "executions": "PR-4 14 of 14; product child 45 of 51; lifetime 52 of 58; infrastructure 6 of 6; combined 58 of 64", "kind": "child_stop", "new_blocking": ["R14-F1 (P1: an ordinary update clears the selection between the Select and Confirm clicks, so the owner cannot complete the repair in play)", "R14-F2 (the companion reader always loads the Release component assembly)"], "open_blocking": ["R12-F1", "R12-F6", "R14-F1", "R14-F2"], "pr": "PR-4", "prev_sha256": "1e670393d28c412fae6f0cc10d73b527e139ecf193470214ae9040c5984ab434", "rejected": ["R12-F1", "R12-F6"], "result": "state rounds/r14/result-1.json sha256 cde2a3baf042a6e5a209544669cecfa1e643b5468c9b13728d16b06b63599bb0 (unedited)", "round": 14, "seq": 171, "utc": "2026-11-27T17:14:25+00:00", "verified": ["R12-F2", "R12-F4", "R12-F7", "R12-F9", "R13-F1", "R13-F2", "R13-F3"]}
```
SHA-256 `8ffc3b21b8f37c5e841b1231a7bee50584c065d9ca5a203c6e4019d7d4f1fcea`

```json
{"authority": "owner instruction 2026-11-27c (new authority beyond 27b)", "budget": "360 -> 390", "ceilings": "55 child / 62 lifetime (incl. PR #64 seven) / 8 infrastructure; 70 combined", "execution_allowances": {"PR-1": 15, "PR-2": 7, "PR-3": 5, "PR-4": 16, "PR-5": "completed-round cap 9, execution ceiling 10", "PR-6": 2, "transport": 8}, "file": "owner-continuations/owner-continuation-2026-11-27c-continuation.md", "file_sha256": "4f0719e8d3c8f37a229d11a5842c9568afbf483bf80fe1cc93494b9da6d40348", "kind": "owner_instruction", "prev_sha256": "51cd5492cddfec905c2fd89e9585e548073b3b4a216bcbdd2d729b29b32ef174", "resumes_authorized": ["PR-4 no-progress stop 14/14 -> cap 16", "PR-2 cap stop 5/5 -> cap 7"], "seq": 173, "tracked_record": "docs/missions/mmr-split/owner-authorization-2026-11-27c.md on the PR-2 and PR-4 branches", "utc": "2026-11-27T17:47:25+00:00"}
```
SHA-256 `9271b94f526c92eb26ba1aad032df758b5532517efade3aebb230bca63eefd6f`

```json
{"after": "ACTIVE, 14 of 16; open findings unchanged; the controller reset rounds_open to 0 for each open finding (its own no-progress resume behaviour; not altered by hand)", "authority": "owner instruction 2026-11-27c section 2", "before": "BLOCKED no progress (R12-F1, R12-F6 open 3 consecutive reviews), 14 of 14, open R12-F1 R12-F6 R14-F1 R14-F2 with rounds_open 3,3,1,1; no lock, no developer, no pending publications", "enrollment": "kmc-codex-mmr-horse-trip-20261125213249-4b8b", "kind": "resume", "operation": "agent_review.py resume --max-rounds 16 --reason ... (a first invocation without --reason was refused by argparse before any state change: resume-pr4-27c-argparse-refused.txt)", "pr": "PR-4", "prev_sha256": "9271b94f526c92eb26ba1aad032df758b5532517efade3aebb230bca63eefd6f", "records": "owner-continuations/pre-resume-pr4-27c-status.json, resume-pr4-27c-output.txt, post-resume-pr4-27c-status.json", "seq": 174, "utc": "2026-11-27T17:48:31+00:00"}
```
SHA-256 `021917c45a93dd2e366fa58e6c2b398f990dfa7fbe6b54b573f52383ca7c15e4`

```json
{"after": "spent 311.29 of 390; remaining 78.7; entries 395", "authority": "owner instruction 2026-11-27c section 2", "before": "spent 311.29 of 360; remaining 48.7; entries 395", "budget_id": "mounted-alpha2-melee-pose-20261124", "from_minutes": 360, "kind": "budget_adjustment", "mechanism": "WorkflowBudget.ps1 Update-KmcWorkflowBudgetRecord (locked), allowanceAdjustments entry appended", "prev_sha256": "56e31907813dfbde8458dedb04c4976336dc331ccccea6c91bbde6f8873a5680", "seq": 176, "to_minutes": 390, "utc": "2026-11-27T17:50:12+00:00"}
```
SHA-256 `d18742dad8285076a8a2def30d48b3df100deba71a19012b88af71a5731e43f9`

```json
{"authority": "owner instruction 2026-11-27c section 2 (once)", "evidence": "session transcript: the only unmetered tool call touching the il_callers scan in the 27b window ran 15:11:54.096Z-15:11:54.583Z (0.49 s); the scan itself was not rerun", "item": "the unmetered PR-2 IL caller scan disclosed in FINAL-HANDOFF-2026-11-27b", "kind": "budget_reconciliation", "prev_sha256": "d18742dad8285076a8a2def30d48b3df100deba71a19012b88af71a5731e43f9", "recorded": "imported entry, 10 s (honest upper bound), via Add-KmcWorkflowImportedEntry", "seq": 177, "utc": "2026-11-27T17:50:12+00:00"}
```
SHA-256 `d66701b75d27371de26fdc3c1a331e033188f7fc0f268dc4157e41f64c340aa6`

```json
{"authority": "owner instruction 2026-11-27c sections 2-3 (cap 16; OWNER AMENDMENT revision 1 preserved)", "budget": "spent ~314.1 of 390 min", "candidate": "the records commit regenerating PR-4-ledger-excerpt.md with this entry (source commit 004793af)", "checks": "Release: components 1548/0, trip native contract 51/0, companion readers 83 (compiled observer), Validate-Source 153/0; Debug (bin/Release hidden): companion readers 83 loading bin/Debug (MVID 5f8fce8d...), trip contract 51/0; Release requested with Release hidden refused", "child_rounds": "14 of 16 before; 15 of 16 after", "executions": "product child 45 -> 46 of 55; lifetime 52 -> 53 of 62; combined 58 -> 59 of 70", "kind": "pre_review_check", "open_counters_before": "R12-F1 0, R12-F6 0, R14-F1 0, R14-F2 0 (after the controller no-progress resume)", "pr": "PR-4", "prev_sha256": "d66701b75d27371de26fdc3c1a331e033188f7fc0f268dc4157e41f64c340aa6", "report_citation": "prepared under owner amendments revision 1 sha256 f9410bf809e35a83f8293bb5b54cfd344594aa0c2e5b90e633468c216e761b28", "round": 15, "seq": 178, "submission_check": "R14-F1 SelectionSurvivesUpdates via HorseTripRepairUpdater.Run; R12-F1 ServiceLifecycleSequence.Disable/FailureCleanup/Enable (CompositionRoot delegates) with a substitute engine side; R12-F6 compiled Main.OnMaintenanceGui through compiled UmmPanelObservation into the reader; R14-F2 Configuration declared by the focused wrapper and forwarded", "unpublished_local_correction": "none: the worktree was clean at 93d806b0 when work began", "utc": "2026-11-27T18:05:32+00:00"}
```
SHA-256 `d500fd0d76c04b3b677092e79f59b5b2576aab723ed38a90c8f50573e6266eb0`

```json
{"candidate": "1845306959b45c4a94d9dd095aa416077b5608d4", "correction": "commit 5c... retargets the Mounted Shield PN4-F3/enable checks, the continuous-drive failure-cleanup checks and the harness composition-boundary assertions to the sequence plus boundary mappings (same order and service reach); Mounted Shield 114/0, continuous drive 59/0, marker ownership 18/0, extracted harness guard block 4/4", "gate": "focused Fast (Release)", "kind": "gate_failure", "no_review_spent": true, "pr": "PR-4", "prev_sha256": "d500fd0d76c04b3b677092e79f59b5b2576aab723ed38a90c8f50573e6266eb0", "result": "FAIL 45.5 s, log gate-focused-1845306959b4-...-20261127T180619409072Z.log (preserved); Test-MountedShieldNativeContract.ps1 stopped at the PN4-F3 lifecycle-order check, which read CompositionRoot.PerformFailureCleanup IL now delegated to ServiceLifecycleSequence", "round": 15, "seq": 179, "utc": "2026-11-27T18:10:27+00:00"}
```
SHA-256 `32a15ceec75b0f5f892f03872060ac927fc431f4986d374cc564789fab74a6a9`

```json
{"child_rounds": "15 of 16", "decision": "changes_required", "kind": "review_result", "new": ["R15-F1 (six children defaulted to Release)", "R15-F2 (refused-disable fixture expected a removal during an active save/load)", "R15-F3 (stopped-interval and taken-consent controls dropped)", "R15-F4 optional (mapping wording)"], "pr": "PR-4", "prev_sha256": "67115e45c4110ad663ce62916013042623fbfad78d1b5fafc212d8991cfe52ac", "rejected": ["R14-F2 (Debug execution record missing)"], "round": 15, "seq": 183, "utc": "2026-11-27T19:00:26+00:00", "verified": ["R14-F1", "R12-F1", "R12-F6"]}
```
SHA-256 `eaaccb1486b31cd6bbbadb17dd3012057397a19d81f7a0d57a0947f862747f52`

```json
{"authority": "owner instruction 2026-11-27c (cap 16; the last authorized PR-4 round); OWNER AMENDMENT revision 1", "budget": "spent 364.9 of 390 min", "candidate": "the records commit adding docs/missions/mmr-split/PR-4-r16-debug-focused-run.txt and the regenerated excerpt (source commit acdb7198)", "checks": "Release components 1548/0; Debug full focused wrapper with bin/Release absent: 13 of 13 children, every product-bound child on bin/Debug (sha256 7b233483..., MVID 5f8fce8d...)", "executions": "product child 46 -> 47 of 55; lifetime 53 -> 54 of 62; combined 61 -> 62 of 70", "kind": "pre_review_check", "open_counters_before": "R14-F2 1 (rejected once), R15-F1..F3 new", "pr": "PR-4", "prev_sha256": "eaaccb1486b31cd6bbbadb17dd3012057397a19d81f7a0d57a0947f862747f52", "report_citation": "prepared under owner amendments revision 1 sha256 f9410bf809e35a83f8293bb5b54cfd344594aa0c2e5b90e633468c216e761b28", "round": 16, "seq": 184, "utc": "2026-11-27T19:00:26+00:00"}
```
SHA-256 `aac943a1a98ea87c67d13a919c26c74a20bc7067ffc7e1c42c5075c93b037265`

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
seq 96 is the resume to 8, seq 97 the round-6 check, seq 98 the round-6 result (no-progress stop); seq 100 owner instruction 2026-11-26e (one owner resume within 8), seq 112 that resume; seq 113 the round-7 check, seq 114 the round-8 check, seq 115 the round-7 result (appended late), seq 116 the correction of that order; the round-8 check is seq 114; seq 117 the round-8 result (cap stop at 8/8); seq 119-120 owner instruction 2026-11-26f (PR-4 resume to a completed-round cap of 10; validation 270 minutes); seq 132 is the round-9 check and seq 133 the round-9 result (no-progress stop on R5-F1 at 9 of 10); seq 136-137 record owner instruction 2026-11-27a (docs/missions/mmr-split/owner-authorization-2026-11-27a.md: PR-4 total allowance 11, program 48 child / 55 lifetime, validation 300 minutes) and seq 139 its prospective stopping policy (the operator two-rejection stop is superseded; controller no-progress stops and caps preserved); seq 142 is the supported resume to 11, seq 144 the round-10 check, seq 147 the round-10 result; seq 148 the round-11 check and seq 149 the round-11 stop (cap 11/11); seq 152-153 record owner instruction 2026-11-27b (docs/missions/mmr-split/owner-authorization-2026-11-27b.md: PR-4 total 14, validation 360 minutes), seq 156 the resume to 14 and seq 157 OWNER AMENDMENT revision 1 (resume --amend-mission); seq 165 the round-12 check, seq 166 the round-12 result, seq 167 the clarification of seq 149 (R11-F5 optional), seq 168 the round-13 check, seq 169 the round-13 result, seq 171 the round-14 stop (no progress, cap 14), seq 173 owner instruction 2026-11-27c, seq 174 the resume to 16, seq 176-177 the budget increase to 390 and the scan reconciliation; seq 178 the round-15 check (source 004793af); seq 179 its failed first gate (guards corrected in 14acf756), seq 183 the round-15 result; the last entry is the round-16 check.
