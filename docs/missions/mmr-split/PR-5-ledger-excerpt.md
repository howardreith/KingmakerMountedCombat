# PR-5 supplemental ledger excerpt (operator records, not controller output)

Scoped, read-only excerpt of `analysis-cache/mounted-alpha2-melee-pose-20261124/decomposition/TRANSITION-LEDGER.jsonl`, the
append-only, hash-chained operator ledger the owner asked for (each entry carries the SHA-256 of the previous line in
`prev_sha256`). The lines below are copied byte for byte; each is followed by its own SHA-256. Generated 2026-11-26T03:17:25+00:00.

```json
{"allocations": {"PR-1": 6, "PR-2": 3, "PR-3": 3, "PR-4": 2, "PR-5": 2, "PR-6": 2}, "budget_adjustment": "allowanceAdjustments entry 2026-11-26T02:32:18Z 90 -> 150 via Update-KmcWorkflowBudgetRecord (locked); balance 78.52 of 150, 71.48 remaining, 59 entries preserved", "instruction_file": "owner-continuations/owner-continuation-2026-11-26-pr1-extension-and-budget.md", "instruction_sha256": "16db6774779cfa369bc390f4af56465844fbf902ba161317b0d16b002a671972", "kind": "owner_decision", "prev_sha256": "f7775c23a15fc0491a5bf82a78c4b4c54981abdab0efe98803ff563e3061434c", "program_rounds": {"allocated_child": 18, "lifetime_cap": 25, "spent": 10}, "seq": 14, "text": "Bounded continuation: PR-1 ceiling 3 -> 6 (rounds 4-6 authorized); other allocations unchanged; program 18 allocated child reviews / 25 lifetime (10 spent); validation allowance 90 -> 150 min on the same budget ID; sequence PR-4, PR-5, then PR-1 resume, then PR-2, PR-3, PR-6.", "utc": "2026-11-26T02:32:30+00:00"}
```
SHA-256 `e5e75214fe978088fb099fc2d6e6e9c6721f9636ea313255ec8d6679c71404fa`

```json
{"branch": "codex/mmr-horse-bite", "enrollment": "kmc-codex-mmr-horse-bite-20261125221703-5f10", "initial_push": "audited ordinary push; receipt push-audits/codex_mmr-horse-bite-aa362695-2026-11-26T031700+0000.json", "kind": "enrollment", "max_rounds": 2, "pr": "PR-5", "pr_number": 67, "pre_submission": "extraction rechecked against 46444b30 (owned files identical; every added shared-file line present in 46444b30); its three catalogue findings (R1-F13, R2-F4, R4-F7) resolved on PR #64; aa362695 adds SupplyExact containment of the boundary construction, nativeProof=false markers and the owner authorization record", "prev_sha256": "bd4e651a21d7b3a3f7924e66e566b13f1f85202e3a9b1d4ac13d5aa8c9b86be7", "seq": 21, "target": "main", "utc": "2026-11-26T03:17:25+00:00"}
```
SHA-256 `3634fc8c91cae4bd2179eb82b90854b36393d64abf9eb93610ab6f50afd96482`

```json
{"applicable_limits": "owner instruction 2026-11-26 (seq 14; docs/missions/mmr-split/owner-authorization-2026-11-26.md): validation 150 min on the shared budget, program 18 allocated child reviews / 25 lifetime, PR-5 2 rounds; the hash-pinned mission still states the earlier 90 / 15 / 22", "budget": "see the gate log's budget line; 82.1 of 150 min spent after the local focused run, 0 unreconciled", "candidate": "the commit on codex/mmr-horse-bite whose parent is aa362695 and which adds docs/missions/mmr-split/PR-5-ledger-excerpt.md", "child_rounds": "0 of 2 used", "kind": "pre_review_check", "pr": "PR-5", "prev_sha256": "3634fc8c91cae4bd2179eb82b90854b36393d64abf9eb93610ab6f50afd96482", "program_rounds": "12 of 25 lifetime (5 of 18 allocated child reviews)", "round": 1, "seq": 22, "utc": "2026-11-26T03:17:25+00:00"}
```
SHA-256 `31931c5db9c211fdcbac2969b96c1bf41350fd3330ed368727001983f882e8bd`

Applicable limits: seq 14 is the owner decision of 2026-11-26 (verbatim text committed as
`docs/missions/mmr-split/owner-authorization-2026-11-26.md`). It raised the shared validation allowance from 90 to 150 minutes
(budget record allowanceAdjustments, 2026-11-26T02:32:18Z) and set the program to 18 allocated child reviews / 25 lifetime;
PR-5 keeps 2 rounds. The hash-pinned mission still states the earlier 90 / 15 / 22 limits (the owner said not to rewrite it
for the numbers). The last entry is the round-1 check for the candidate that adds this file.
