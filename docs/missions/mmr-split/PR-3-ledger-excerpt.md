# PR-3 supplemental ledger excerpt (operator records, not controller output)

Scoped, read-only excerpt of `analysis-cache/mounted-alpha2-melee-pose-20261124/decomposition/TRANSITION-LEDGER.jsonl`, the
append-only, hash-chained operator ledger the owner asked for (each entry carries the SHA-256 of the previous line in
`prev_sha256`). The lines below are copied byte for byte; each is followed by its own SHA-256. Generated 2026-11-26T12:16:23+00:00.

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

```json
{"candidate": "bbb0eeb6bcec63808753a071876764034a7baa41", "child_rounds": "1 of 3 used", "decision": "changes_required", "findings": ["R1-F1", "R1-F2", "R1-F3", "R1-F4", "R1-F5"], "kind": "review_result", "pr": "PR-3", "prev_sha256": "be84f5fd85902372a3793dc231b1285d0d6bc8d710e7f43c85aca7b4e077c03e", "program_rounds": "18 of 25 lifetime (11 of 18 allocated child reviews)", "round": 1, "seq": 37, "utc": "2026-11-26T06:36:01+00:00"}
```
SHA-256 `bea278943dff6e518f04f508b59e0d5a9d8a751a1e02eb742c65547728d39909`

```json
{"budget": "budget record read at 2026-11-26T06:36:01Z (before this entry): budget mounted-alpha2-melee-pose-20261124: spent 95.87 of 150 min; remaining 54.13 min; entries 109 (running 0, interrupted 0, unreconciled 0)", "candidate": "the commit on codex/mmr-horse-seat that regenerates docs/missions/mmr-split/PR-3-ledger-excerpt.md with this entry", "child_rounds": "1 of 3 used", "kind": "pre_review_check", "pr": "PR-3", "prev_sha256": "bea278943dff6e518f04f508b59e0d5a9d8a751a1e02eb742c65547728d39909", "program_rounds": "18 of 25 lifetime (11 of 18 allocated child reviews)", "round": 2, "seq": 38, "utc": "2026-11-26T06:36:01+00:00"}
```
SHA-256 `aac97d8ef9049aa7671198d9a2e311d812b37f7cf6a06500939af5d65c93f339`

```json
{"candidate": "2e309fdda1f66c6e92b607fc8a7322cdbae77729", "child_rounds": "2 of 3 used", "decision": "changes_required", "findings": ["R1-F1 (carried, P1)", "R2-F1 (P3)", "R2-F2 (P3)"], "kind": "review_result", "pr": "PR-3", "prev_sha256": "aac97d8ef9049aa7671198d9a2e311d812b37f7cf6a06500939af5d65c93f339", "program_rounds": "19 of 25 lifetime (12 of 18 allocated child reviews)", "resolved": ["R1-F2", "R1-F3", "R1-F4", "R1-F5"], "round": 2, "seq": 39, "utc": "2026-11-26T07:02:27+00:00"}
```
SHA-256 `702eaf7d9954f885190cb45d3c65c94b7c4b56ee0ae03e14e720503193b364af`

```json
{"budget": "budget record read at 2026-11-26T07:02:27Z (before this entry): budget mounted-alpha2-melee-pose-20261124: spent 100.22 of 150 min; remaining 49.78 min; entries 116 (running 0, interrupted 0, unreconciled 0)", "candidate": "the commit on codex/mmr-horse-seat that regenerates docs/missions/mmr-split/PR-3-ledger-excerpt.md with this entry", "child_rounds": "2 of 3 used", "kind": "pre_review_check", "pr": "PR-3", "prev_sha256": "702eaf7d9954f885190cb45d3c65c94b7c4b56ee0ae03e14e720503193b364af", "program_rounds": "19 of 25 lifetime (12 of 18 allocated child reviews)", "round": 3, "seq": 40, "utc": "2026-11-26T07:02:27+00:00"}
```
SHA-256 `4b2d129e4bf46bab5c05c697ee08be882864febd1623c893a95aaea6c3e8da48`

```json
{"budget": "budget record read at 2026-11-26T07:15:26Z: budget mounted-alpha2-melee-pose-20261124: spent 100.79 of 150 min; remaining 49.22 min; entries 117 (running 0, interrupted 0, unreconciled 0)", "candidate": "642cb4818c567c14e05af3befa486b90e28afec0", "child_rounds": "3 of 3 used", "controller_status": "BLOCKED: no progress: R1-F1 open for 3 consecutive reviews (round limit 3 of 3 also reached)", "decision": "changes_required", "findings": ["R1-F1 (carried, P1)"], "kind": "review_result", "pr": "PR-3", "prev_sha256": "4b2d129e4bf46bab5c05c697ee08be882864febd1623c893a95aaea6c3e8da48", "program_rounds": "20 of 25 lifetime (13 of 18 allocated child reviews)", "resolved": ["R2-F1", "R2-F2"], "round": 3, "seq": 41, "utc": "2026-11-26T07:15:26+00:00"}
```
SHA-256 `08f02c58d1abb39dcf0a3e18dd24fb502d83ee834d12bc16a359736da601ece8`

```json
{"correct": "from blocks_readiness in the latest accepted results: #65 e5735eb3 7 blocking/0 optional; #66 b36f08a7 4/0; #67 97dce9a9 7/2; #68 642cb481 1/0; total 19 blockers (12 defect, 7 evidence_gap) and 2 optional_polish", "error": "priority was treated as blocking status; PR-1's seven P2 findings were described as non-blocking", "kind": "correction", "preserved": "original handoff copied unchanged to FINAL-HANDOFF-2026-11-26.as-written-0715Z.md (sha256 ac2323a4b8f5d53bf9681e0e582767fb00eb4a4f854bee25c3d76a6990b5b0cf); review JSON, reviewer flags and controller findings untouched", "prev_sha256": "08f02c58d1abb39dcf0a3e18dd24fb502d83ee834d12bc16a359736da601ece8", "seq": 42, "source": "owner instruction 2026-11-26b section 1", "subject": "FINAL-HANDOFF-2026-11-26.md (07:15Z)", "utc": "2026-11-26T12:08:34+00:00"}
```
SHA-256 `19b938522a1a8ef48e3a6e681476c8a70a6b65fac5c1c98bf8bcdf122813d63d`

```json
{"instruction": "owner-continuations/owner-continuation-2026-11-26b-review-extension.md", "kind": "owner_decision", "prev_sha256": "19b938522a1a8ef48e3a6e681476c8a70a6b65fac5c1c98bf8bcdf122813d63d", "program": "26 child / 33 lifetime reviews (incl. PR #64's 7); 20 lifetime spent; 13 available (8 new + 5 reserved for PR-2 and PR-6)", "review_ceilings": {"PR-1 #65": 8, "PR-2": 3, "PR-3 #68": 5, "PR-4 #66": 4, "PR-5 #67": 4, "PR-6": 2}, "seq": 43, "sequence": ["#68", "#66", "#67", "#65", "PR-2", "PR-6"], "sha256": "8f45f1bbb55ae10e9ea45c9c9d9394995da85f752a2ee8ed01b7d8720767c261", "utc": "2026-11-26T12:08:34+00:00", "validation": "unchanged at 150 min; budget record read at 2026-11-26T12:08:34Z: budget mounted-alpha2-melee-pose-20261124: spent 100.79 of 150 min; remaining 49.22 min; entries 117 (running 0, interrupted 0, unreconciled 0)"}
```
SHA-256 `0cbfb0423e613d5f989cee870982da615afbad39bb4f6ab8d15d9594198619e0`

```json
{"after": "ACTIVE, 3 of 5, open R1-F1; no-progress streak preserved", "authority": "owner instruction 2026-11-26b (docs/missions/mmr-split/owner-authorization-2026-11-26b.md; sha256 8f45f1bbb55ae10e9ea45c9c9d9394995da85f752a2ee8ed01b7d8720767c261 as saved in owner-continuations)", "before": "BLOCKED (no progress: R1-F1 open for 3 consecutive reviews), 3 of 3", "enrollment": "kmc-codex-mmr-horse-seat-20261126011011-d3c5", "kind": "resume", "operation": "agent_review.py resume --max-rounds 5", "pr": "PR-3", "prev_sha256": "0cbfb0423e613d5f989cee870982da615afbad39bb4f6ab8d15d9594198619e0", "records": "decomposition/status/pr3-pre-resume-status.json, pr3-resume.out, pr3-post-resume-status.json", "seq": 44, "utc": "2026-11-26T12:16:16+00:00"}
```
SHA-256 `2876ad76c0ae14fde27790dde2af0755e0d0b21dd830c68349e350aa86ed4003`

```json
{"budget": "budget record read at 2026-11-26T12:16:15Z (before this entry): budget mounted-alpha2-melee-pose-20261124: spent 100.87 of 150 min; remaining 49.13 min; entries 119 (running 0, interrupted 0, unreconciled 0)", "candidate": "the commit on codex/mmr-horse-seat that regenerates docs/missions/mmr-split/PR-3-ledger-excerpt.md with this entry", "child_rounds": "3 of 5 used", "kind": "pre_review_check", "pr": "PR-3", "prev_sha256": "2876ad76c0ae14fde27790dde2af0755e0d0b21dd830c68349e350aa86ed4003", "program_rounds": "20 of 33 lifetime (13 of 26 allocated child reviews)", "round": 4, "seq": 45, "utc": "2026-11-26T12:16:16+00:00"}
```
SHA-256 `7ab96200729e4441674297b9a531a00d7435cd9aedd87b9d7b048b89ee74b33d`

Applicable limits: seq 14 is the owner decision of 2026-11-26 (`docs/missions/mmr-split/owner-authorization-2026-11-26.md`): validation
allowance 150 minutes, PR-3 3 rounds. Seq 43 is the owner extension of 2026-11-26 (`docs/missions/mmr-split/owner-authorization-2026-11-26b.md`):
PR-3 total ceiling 5, program 26 child / 33 lifetime reviews, validation unchanged at 150 minutes; seq 42 corrects the blocking counts in the final handoff;
the hash-pinned mission still states the earlier limits (the owner said not to rewrite it for the numbers).
PR-3 entries: seq 36/37 round 1, 38/39 round 2, 40/41 round 3 (BLOCKED, no progress), 44 the resume to a ceiling of 5; the last entry
is the round-4 check for the candidate adding this version.
