# PR-4 supplemental ledger excerpt (operator records, not controller output)

Scoped, read-only excerpt of `analysis-cache/mounted-alpha2-melee-pose-20261124/decomposition/TRANSITION-LEDGER.jsonl`, the
append-only, hash-chained operator ledger the owner asked for (each entry carries the SHA-256 of the previous line in
`prev_sha256`). The lines below are copied byte for byte; each is followed by its own SHA-256. Generated 2026-11-26T12:39:56+00:00.

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

Reconciliation (PR-4 review R1-F8): seq 14 is the owner decision of 2026-11-26 (verbatim text committed as
`docs/missions/mmr-split/owner-authorization-2026-11-26.md`, SHA-256 in seq 18). It raised the validation allowance of the
shared budget from 90 to 150 minutes (recorded in the budget record allowanceAdjustments, 2026-11-26T02:32:18Z) and the program
to 18 allocated child reviews / 25 lifetime; PR-4 stays at 2 rounds. The hash-pinned mission still states the earlier
90 / 15 / 22 limits because the owner said not to rewrite it for the numbers. Seq 18 corrects seq 16 (80.01, not approx. 79.45,
minutes after the first gate); seq 19 is the round-2 check and seq 20 the round-2 stop (round cap 2 of 2).
Seq 42 corrects the blocking counts in the final handoff (from blocks_readiness); seq 43 is the owner extension of 2026-11-26
(`docs/missions/mmr-split/owner-authorization-2026-11-26b.md`): PR-4 total ceiling 4, program 26 child / 33 lifetime reviews,
validation unchanged at 150 minutes. Seq 49 is the resume to a ceiling of 4; the last entry is the round-3 check for the
candidate that adds this version of the file.
