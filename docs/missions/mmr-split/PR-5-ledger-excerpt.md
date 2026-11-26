# PR-5 supplemental ledger excerpt (operator records, not controller output)

Scoped, read-only excerpt of `analysis-cache/mounted-alpha2-melee-pose-20261124/decomposition/TRANSITION-LEDGER.jsonl`, the
append-only, hash-chained operator ledger the owner asked for (each entry carries the SHA-256 of the previous line in
`prev_sha256`). The lines below are copied byte for byte; each is followed by its own SHA-256. Generated 2026-11-26T13:47:57+00:00.

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

```json
{"child_rounds": "2 of 2 used", "enrollment": "kmc-codex-mmr-horse-bite-20261125221703-5f10", "kind": "child_stop", "last_candidate": "97dce9a945394306e0bf365095c640ae1739cb4b", "next": "stop preserved per owner instruction 2026-11-26 section 2; return to PR-1 (PR #65) via the owner-specified resume", "note": "R2-F2 is a regression introduced by the round-2 correction (PlannedAttack read moved outside the observation guard); branch unmerged", "open_findings": ["R1-F4", "R1-F9", "R2-F1", "R2-F2", "R2-F3", "R2-F4", "R2-F5", "R2-F6", "R2-F7"], "pr": "PR-5", "pr_number": 67, "prev_sha256": "002731c8ac370b1a470907811da041f470471c0577c41707adb8762708428012", "program_rounds": "14 of 25 lifetime (7 of 18 allocated child reviews)", "reason": "review-round cap reached for final (2/2)", "seq": 27, "status": "BLOCKED", "utc": "2026-11-26T04:11:36+00:00", "verified_fixed_in_r2": ["R1-F1", "R1-F2", "R1-F3", "R1-F5", "R1-F6", "R1-F7", "R1-F8", "R1-F10", "R1-F11"]}
```
SHA-256 `a5304b721e4d1d2ff7ddd0331af061e9e936d1794ea67f94c7483a16cafb5c3e`

```json
{"correct": "from blocks_readiness in the latest accepted results: #65 e5735eb3 7 blocking/0 optional; #66 b36f08a7 4/0; #67 97dce9a9 7/2; #68 642cb481 1/0; total 19 blockers (12 defect, 7 evidence_gap) and 2 optional_polish", "error": "priority was treated as blocking status; PR-1's seven P2 findings were described as non-blocking", "kind": "correction", "preserved": "original handoff copied unchanged to FINAL-HANDOFF-2026-11-26.as-written-0715Z.md (sha256 ac2323a4b8f5d53bf9681e0e582767fb00eb4a4f854bee25c3d76a6990b5b0cf); review JSON, reviewer flags and controller findings untouched", "prev_sha256": "08f02c58d1abb39dcf0a3e18dd24fb502d83ee834d12bc16a359736da601ece8", "seq": 42, "source": "owner instruction 2026-11-26b section 1", "subject": "FINAL-HANDOFF-2026-11-26.md (07:15Z)", "utc": "2026-11-26T12:08:34+00:00"}
```
SHA-256 `19b938522a1a8ef48e3a6e681476c8a70a6b65fac5c1c98bf8bcdf122813d63d`

```json
{"instruction": "owner-continuations/owner-continuation-2026-11-26b-review-extension.md", "kind": "owner_decision", "prev_sha256": "19b938522a1a8ef48e3a6e681476c8a70a6b65fac5c1c98bf8bcdf122813d63d", "program": "26 child / 33 lifetime reviews (incl. PR #64's 7); 20 lifetime spent; 13 available (8 new + 5 reserved for PR-2 and PR-6)", "review_ceilings": {"PR-1 #65": 8, "PR-2": 3, "PR-3 #68": 5, "PR-4 #66": 4, "PR-5 #67": 4, "PR-6": 2}, "seq": 43, "sequence": ["#68", "#66", "#67", "#65", "PR-2", "PR-6"], "sha256": "8f45f1bbb55ae10e9ea45c9c9d9394995da85f752a2ee8ed01b7d8720767c261", "utc": "2026-11-26T12:08:34+00:00", "validation": "unchanged at 150 min; budget record read at 2026-11-26T12:08:34Z: budget mounted-alpha2-melee-pose-20261124: spent 100.79 of 150 min; remaining 49.22 min; entries 117 (running 0, interrupted 0, unreconciled 0)"}
```
SHA-256 `0cbfb0423e613d5f989cee870982da615afbad39bb4f6ab8d15d9594198619e0`

```json
{"after": "ACTIVE, 2 of 4, nine findings open (seven blocking, R2-F6/R2-F7 optional); streaks preserved", "authority": "owner instruction 2026-11-26b (docs/missions/mmr-split/owner-authorization-2026-11-26b.md)", "before": "BLOCKED (round cap 2 of 2)", "enrollment": "kmc-codex-mmr-horse-bite-20261125221703-5f10", "kind": "resume", "operation": "agent_review.py resume --max-rounds 4", "pr": "PR-5", "prev_sha256": "256d4d76706bf2095bf8379ad0636f9a3fed8b03c90e3eacfa94ecb34fd3492b", "records": "decomposition/status/pr5-pre-resume-status.json, pr5-resume.out, pr5-post-resume-status.json", "seq": 52, "utc": "2026-11-26T13:13:46+00:00"}
```
SHA-256 `095fbb24b317fbcfd5eceb22bbe2193db4b740bee1063047f3b209097a0dc47a`

```json
{"attempts": [{"budget_entry": "mounted-alpha2-melee-pose-20261124:20261126T130042922Z:32652:e90dfe57", "result": "failed at build in about 2 s (fresh worktree lacked the git-ignored LocalGamePaths.props); no check ran"}, {"budget_entry": "mounted-alpha2-melee-pose-20261124:20261126T130105827Z:44652:b1163834", "result": "pass 14 of 14, 1.48 min; DLL sha256 750fbcda...; every count identical to the original log"}], "authority": "owner instruction 2026-11-26b section 6", "commit": "b1c82f31b47dc96d93d556bc152970f4f634f17d", "finding": "R1-F9", "kind": "authorized_verification", "pr": "PR-5", "prev_sha256": "095fbb24b317fbcfd5eceb22bbe2193db4b740bee1063047f3b209097a0dc47a", "records": "docs/missions/mmr-split/evidence/PR-5-b1c82f31-verification-2026-11-26.md (+ log, identity, comparison); decomposition/pr5/r1f9-verify", "seq": 53, "utc": "2026-11-26T13:13:46+00:00", "worktree": "worktrees/mmr-horse-bite-b1c82f31-verify (detached, isolated)"}
```
SHA-256 `82bdde1b3c5cba3d0d2378116541d9ea02a7943aefb94ec9f34d6765b758f4e0`

```json
{"finding": "R2-F5", "kind": "evidence_runs", "pr": "PR-5", "prev_sha256": "82bdde1b3c5cba3d0d2378116541d9ea02a7943aefb94ec9f34d6765b758f4e0", "records": "decomposition/pr5/r2f5-mutation; docs/missions/mmr-split/evidence/PR-5-R2-F5-mutation.*", "runs": ["build 34.3 s + bite contract 1.0 s on UNMUTATED source (mutation edit failed to apply; demonstrates nothing)", "build 41.9 s + bite contract 1.0 s on the mutated source: the R2-F5 check fails (record write at IL 68 before get_name at IL 74); source restored"], "seq": 54, "utc": "2026-11-26T13:13:46+00:00"}
```
SHA-256 `1e7b91f5efcf0af14a40be76f6bdb55e1cd82b1830f934e502726df6f994b541`

```json
{"budget": "budget record read at 2026-11-26T13:13:46Z (before this entry): budget mounted-alpha2-melee-pose-20261124: spent 110.12 of 150 min; remaining 39.88 min; entries 134 (running 0, interrupted 0, unreconciled 0)", "candidate": "the commit on codex/mmr-horse-bite that regenerates docs/missions/mmr-split/PR-5-ledger-excerpt.md with this entry", "child_rounds": "2 of 4 used", "kind": "pre_review_check", "pr": "PR-5", "prev_sha256": "1e7b91f5efcf0af14a40be76f6bdb55e1cd82b1830f934e502726df6f994b541", "program_rounds": "22 of 33 lifetime (15 of 26 allocated child reviews)", "round": 3, "seq": 55, "utc": "2026-11-26T13:13:46+00:00"}
```
SHA-256 `7290aeb0e0f68c099de18af1394ef0f26fe50cf0977bef5191b21b25a6018c82`

```json
{"budget": "budget record read at 2026-11-26T13:35:03Z: budget mounted-alpha2-melee-pose-20261124: spent 111.62 of 150 min; remaining 38.38 min; entries 135 (running 0, interrupted 0, unreconciled 0)", "candidate": "eb35a475881fcd24fda838f119fdcce34681cfb8", "child_rounds": "3 of 4", "decision": "changes_required", "kind": "review_result", "note": "a first submission was refused before review for missing dispositions of the optional R2-F6/R2-F7 (no round used); resubmitted with deferred_optional citing the owner instruction", "open_blocking": ["R2-F2 (carried)", "R2-F3 (carried, evidence_gap)", "R3-F1", "R3-F2", "R3-F3", "R3-F4", "R3-F5", "R3-F6"], "open_optional": ["R2-F6", "R2-F7", "R3-F7"], "pr": "PR-5", "prev_sha256": "7290aeb0e0f68c099de18af1394ef0f26fe50cf0977bef5191b21b25a6018c82", "program_rounds": "23 of 33 lifetime (16 of 26 allocated child reviews)", "resolved": ["R1-F4", "R1-F9", "R2-F1", "R2-F4", "R2-F5"], "round": 3, "seq": 56, "utc": "2026-11-26T13:35:03+00:00"}
```
SHA-256 `8eaf8e2f0f9030f1e9e4af60a9df97f672369d09f3c754ea0dc0b55d2ee2d1fa`

```json
{"budget": "budget record read at 2026-11-26T13:47:56Z (before this entry): budget mounted-alpha2-melee-pose-20261124: spent 114.42 of 150 min; remaining 35.58 min; entries 147 (running 0, interrupted 0, unreconciled 0)", "candidate": "the commit on codex/mmr-horse-bite that regenerates docs/missions/mmr-split/PR-5-ledger-excerpt.md with this entry", "child_rounds": "3 of 4 used (round 4 is the last)", "kind": "pre_review_check", "pr": "PR-5", "prev_sha256": "8eaf8e2f0f9030f1e9e4af60a9df97f672369d09f3c754ea0dc0b55d2ee2d1fa", "program_rounds": "23 of 33 lifetime (16 of 26 allocated child reviews)", "round": 4, "seq": 57, "utc": "2026-11-26T13:47:57+00:00"}
```
SHA-256 `1d576f12f88c6b4d2842d99dd2e1febdb1d5131eb0b621ef7e838dd119044984`

Applicable limits: seq 14 is the owner decision of 2026-11-26 (verbatim text committed as
`docs/missions/mmr-split/owner-authorization-2026-11-26.md`). It raised the shared validation allowance from 90 to 150 minutes
(budget record allowanceAdjustments, 2026-11-26T02:32:18Z) and set the program to 18 allocated child reviews / 25 lifetime;
PR-5 keeps 2 rounds. The hash-pinned mission still states the earlier 90 / 15 / 22 limits (the owner said not to rewrite it
for the numbers). Seq 22 is the round-1 check; seq 23 corrects its budget figure and seq 24 the stated read time of seq 23
(recorded after the round-1 gate). Seq 25 is the round-1 result and seq 26 the round-2 check; seq 42 corrects the blocking counts
in the final handoff (from blocks_readiness); seq 43 is the owner extension of 2026-11-26 (`docs/missions/mmr-split/owner-authorization-2026-11-26b.md`):
PR-5 total ceiling 4, program 26 child / 33 lifetime reviews, validation unchanged at 150 minutes. Seq 52 is the resume to a ceiling
of 4, seq 53 the authorized R1-F9 verification on b1c82f31, seq 54 the R2-F5 evidence runs, seq 55 the round-3 check and seq 56
the round-3 result; the last entry is the round-4 check for the candidate that adds this version.
