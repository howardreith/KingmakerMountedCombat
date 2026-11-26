# PR-4 supplemental ledger excerpt (operator records, not controller output)

Scoped, read-only excerpt of `analysis-cache/mounted-alpha2-melee-pose-20261124/decomposition/TRANSITION-LEDGER.jsonl`, the
append-only, hash-chained operator ledger the owner asked for (each entry carries the SHA-256 of the previous line in
`prev_sha256`). The lines below are copied byte for byte; each is followed by its own SHA-256. Generated 2026-11-26T02:59:40+00:00.

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

Reconciliation (PR-4 review R1-F8): seq 14 is the owner decision of 2026-11-26 (verbatim text committed as
`docs/missions/mmr-split/owner-authorization-2026-11-26.md`, SHA-256 in seq 18). It raised the validation allowance of the
shared budget from 90 to 150 minutes (recorded in the budget record allowanceAdjustments, 2026-11-26T02:32:18Z) and the program
to 18 allocated child reviews / 25 lifetime; PR-4 stays at 2 rounds. The hash-pinned mission still states the earlier
90 / 15 / 22 limits because the owner said not to rewrite it for the numbers. Seq 18 corrects seq 16 (80.01, not approx. 79.45,
minutes after the first gate); seq 19 is the round-2 check for the candidate that adds this version of the file.
