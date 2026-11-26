# PR-5 supplemental ledger excerpt (operator records, not controller output)

Scoped, read-only excerpt of `analysis-cache/mounted-alpha2-melee-pose-20261124/decomposition/TRANSITION-LEDGER.jsonl`, the
append-only, hash-chained operator ledger the owner asked for (each entry carries the SHA-256 of the previous line in
`prev_sha256`). The lines below are copied byte for byte; each is followed by its own SHA-256. Generated 2026-11-26T03:53:57+00:00.

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

```json
{"corrects_seq": 22, "kind": "correction", "pr": "PR-5", "prev_sha256": "31931c5db9c211fdcbac2969b96c1bf41350fd3330ed368727001983f882e8bd", "seq": 23, "text": "seq 22 stated an estimated 82.1 min; the budget record read at 2026-11-26T03:19Z shows spent 82.84 of 150 min, remaining 67.17, 66 entries, 0 running/interrupted/unreconciled (after the PR-4 round-2 gate and the local PR-5 focused run). The round-1 gate for the candidate adding this excerpt follows.", "utc": "2026-11-26T03:17:47+00:00"}
```
SHA-256 `2a5edec69892fdb39ab1b9acea4c1c36a49d9f224dbe4e436bfd42bc45c1f4a5`

```json
{"corrects_seq": 23, "kind": "correction", "pr": "PR-5", "prev_sha256": "2a5edec69892fdb39ab1b9acea4c1c36a49d9f224dbe4e436bfd42bc45c1f4a5", "seq": 24, "text": "seq 23 gives the budget read time as 03:19Z; the read took place between seq 22 (03:17:25Z) and seq 23 (03:17:47Z). The figure (82.84 of 150 min, 0 unreconciled) is unchanged. Recorded after the round-1 candidate 22de1f7d was gated, so its committed excerpt ends at seq 23; disclosed in the round-1 developer report.", "utc": "2026-11-26T03:18:51+00:00"}
```
SHA-256 `922f91e8de28afb419e0a77999569735763d446edd64a04aedbf2598c00ff48a`

```json
{"candidate": "22de1f7dec95c60428dc5d8c2e9c66b6f1e295c7", "child_rounds": "1 of 2 used", "decision": "changes_required", "findings": ["R1-F1", "R1-F2", "R1-F3", "R1-F4", "R1-F5", "R1-F6", "R1-F7", "R1-F8", "R1-F9", "R1-F10", "R1-F11"], "kind": "review_result", "pr": "PR-5", "prev_sha256": "922f91e8de28afb419e0a77999569735763d446edd64a04aedbf2598c00ff48a", "program_rounds": "13 of 25 lifetime (6 of 18 allocated child reviews)", "round": 1, "seq": 25, "utc": "2026-11-26T03:53:56+00:00"}
```
SHA-256 `4a9a5b9db04e3afd9d70ebad8ac68258593207a75d67e595df0e0f17d3d7c3da`

```json
{"applicable_limits": "owner instruction 2026-11-26 (seq 14): validation 150 min on the shared budget, program 18 allocated child reviews / 25 lifetime, PR-5 2 rounds (round 2 is the last)", "budget": "budget record read at 2026-11-26T03:53:38Z (before this entry): spent 86.25 of 150 min, remaining 63.75, 72 entries, 0 running/interrupted/unreconciled; the round-2 gate follows", "candidate": "the commit on codex/mmr-horse-bite whose parent is d587cc0c and which regenerates docs/missions/mmr-split/PR-5-ledger-excerpt.md with this entry", "child_rounds": "1 of 2 used", "kind": "pre_review_check", "pr": "PR-5", "prev_sha256": "4a9a5b9db04e3afd9d70ebad8ac68258593207a75d67e595df0e0f17d3d7c3da", "program_rounds": "13 of 25 lifetime (6 of 18 allocated child reviews)", "round": 2, "seq": 26, "utc": "2026-11-26T03:53:57+00:00"}
```
SHA-256 `002731c8ac370b1a470907811da041f470471c0577c41707adb8762708428012`

Applicable limits: seq 14 is the owner decision of 2026-11-26 (verbatim text committed as
`docs/missions/mmr-split/owner-authorization-2026-11-26.md`). It raised the shared validation allowance from 90 to 150 minutes
(budget record allowanceAdjustments, 2026-11-26T02:32:18Z) and set the program to 18 allocated child reviews / 25 lifetime;
PR-5 keeps 2 rounds. The hash-pinned mission still states the earlier 90 / 15 / 22 limits (the owner said not to rewrite it
for the numbers). Seq 22 is the round-1 check; seq 23 corrects its budget figure and seq 24 the stated read time of seq 23
(recorded after the round-1 gate). Seq 25 is the round-1 result and seq 26 the round-2 check for the candidate that adds this version.
