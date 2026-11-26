# PR-3 supplemental ledger excerpt (operator records, not controller output)

Scoped, read-only excerpt of `analysis-cache/mounted-alpha2-melee-pose-20261124/decomposition/TRANSITION-LEDGER.jsonl`, the
append-only, hash-chained operator ledger the owner asked for (each entry carries the SHA-256 of the previous line in
`prev_sha256`). The lines below are copied byte for byte; each is followed by its own SHA-256. Generated 2026-11-26T06:10:32+00:00.

```json
{"allocations": {"PR-1": 6, "PR-2": 3, "PR-3": 3, "PR-4": 2, "PR-5": 2, "PR-6": 2}, "budget_adjustment": "allowanceAdjustments entry 2026-11-26T02:32:18Z 90 -> 150 via Update-KmcWorkflowBudgetRecord (locked); balance 78.52 of 150, 71.48 remaining, 59 entries preserved", "instruction_file": "owner-continuations/owner-continuation-2026-11-26-pr1-extension-and-budget.md", "instruction_sha256": "16db6774779cfa369bc390f4af56465844fbf902ba161317b0d16b002a671972", "kind": "owner_decision", "prev_sha256": "f7775c23a15fc0491a5bf82a78c4b4c54981abdab0efe98803ff563e3061434c", "program_rounds": {"allocated_child": 18, "lifetime_cap": 25, "spent": 10}, "seq": 14, "text": "Bounded continuation: PR-1 ceiling 3 -> 6 (rounds 4-6 authorized); other allocations unchanged; program 18 allocated child reviews / 25 lifetime (10 spent); validation allowance 90 -> 150 min on the same budget ID; sequence PR-4, PR-5, then PR-1 resume, then PR-2, PR-3, PR-6.", "utc": "2026-11-26T02:32:30+00:00"}
```
SHA-256 `e5e75214fe978088fb099fc2d6e6e9c6721f9636ea313255ec8d6679c71404fa`

```json
{"branch": "codex/mmr-horse-seat", "enrollment": "kmc-codex-mmr-horse-seat-20261126011011-d3c5", "initial_push": "audited ordinary push; receipt push-audits/codex_mmr-horse-seat-0ba4962c-2026-11-26T061008+0000.json", "kind": "enrollment", "max_rounds": 3, "pr": "PR-3", "pr_number": 68, "pre_submission": "branch rechecked (clean, unpushed, base 783b2b3d = main); carried PR #64 findings R5-F4, R7-F2, R7-F3 corrected in c930f85c; 0ba4962c adds configuration forwarding, nativeProof=false markers and the owner authorization record; local metered focused run 14 of 14", "prev_sha256": "120216ca3d1af31d849b2714db08e2737dc1ebc47098f914243b9d279798cc1b", "seq": 35, "target": "main", "utc": "2026-11-26T06:10:32+00:00"}
```
SHA-256 `a93b5547a53f07dec2dfb6d63a212ffe5e9e3d03257316ad47a49050c33af7b2`

```json
{"applicable_limits": "owner instruction 2026-11-26 (seq 14): PR-3 3 rounds, validation 150 min on the shared budget, program 18 allocated child reviews / 25 lifetime; the hash-pinned mission states the earlier limits", "budget": "budget record read at 2026-11-26T06:10:32Z (before this entry): budget mounted-alpha2-melee-pose-20261124: spent 94.29 of 150 min; remaining 55.72 min; entries 104 (running 0, interrupted 0, unreconciled 0)", "candidate": "the commit on codex/mmr-horse-seat whose parent is 0ba4962c and which adds docs/missions/mmr-split/PR-3-ledger-excerpt.md", "child_rounds": "0 of 3 used", "kind": "pre_review_check", "pr": "PR-3", "prev_sha256": "a93b5547a53f07dec2dfb6d63a212ffe5e9e3d03257316ad47a49050c33af7b2", "program_rounds": "17 of 25 lifetime (10 of 18 allocated child reviews)", "round": 1, "seq": 36, "utc": "2026-11-26T06:10:32+00:00"}
```
SHA-256 `be84f5fd85902372a3793dc231b1285d0d6bc8d710e7f43c85aca7b4e077c03e`

Applicable limits: seq 14 is the owner decision of 2026-11-26 (verbatim text committed as
`docs/missions/mmr-split/owner-authorization-2026-11-26.md`). It raised the shared validation allowance from 90 to 150 minutes
and set the program to 18 allocated child reviews / 25 lifetime; PR-3 keeps 3 rounds. The hash-pinned mission still states the
earlier limits (the owner said not to rewrite it for the numbers). The last entry is the round-1 check for the candidate adding this file.
