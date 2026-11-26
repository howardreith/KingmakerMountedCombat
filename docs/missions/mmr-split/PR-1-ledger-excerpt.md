# PR-1 supplemental ledger excerpt (operator records, not controller output)

Scoped, read-only excerpt of `analysis-cache/mounted-alpha2-melee-pose-20261124/decomposition/TRANSITION-LEDGER.jsonl`, the
append-only, hash-chained operator ledger the owner asked for (each entry carries the SHA-256 of the previous line in
`prev_sha256`). The lines below are copied byte for byte; each is followed by its own SHA-256. Generated 2026-11-26T14:25:06+00:00.

```json
{"branch": "codex/mmr-attack-orders", "enrollment": "kmc-codex-mmr-attack-orders-20261125103400-6cef", "extraction_commit": "95c26e59", "extraction_proof": "build clean; components 1539/0; Validate-Source 153/0; orders native contract 4/0; lifecycle contract 49/0; attack-path harness 16/0 (metered rerun after removing a PR-2 helper); whole-file identity of 6 owned files with 46444b30; shared-file residuals only PR-2/PR-5 pieces", "initial_push": "audited ordinary push (AGENTS.md; no project helper on main); receipt push-audits/codex_mmr-attack-orders-95c26e59-2026-11-25T153350+0000.json", "kind": "enrollment", "max_rounds": 3, "mission": "docs/missions/mmr-split/PR-1-attack-orders.md", "mission_commit": "f29a45e3", "pr": "PR-1", "pr_number": 65, "prev_sha256": "4ecc6180bebafa14b800701c0c0b66d2a6a255353a0ce6f517bb10c37081dd8a", "program_rounds": {"cap": 22, "child_allocated_so_far": 3, "spent_before": 7}, "seq": 7, "target": "main", "utc": "2026-11-25T15:34:13+00:00"}
```
SHA-256 `71bb4e989d85640c990c2c46a4b131a2b6a0014e58c2e96944ca413919432633`

```json
{"budget": "67.17 of 90 min spent; 0 unreconciled", "candidate": "95c26e59", "child_rounds": "0 of 3 used", "gate": "gate-focused-95c26e59c465-d43d449b6c4-af2513083e1-20261125T153534161716Z pass", "kind": "pre_review_check", "pr": "PR-1", "prev_sha256": "71bb4e989d85640c990c2c46a4b131a2b6a0014e58c2e96944ca413919432633", "program_rounds": "7 of 22 used (0 of 15 additional)", "round": 1, "seq": 8, "utc": "2026-11-25T15:35:59+00:00"}
```
SHA-256 `53558efb63050ec929f3ec0510c310ce5edc7ff3ced9087dc9a1ad8c35c24f16`

```json
{"blocking": 12, "candidate": "95c26e59", "child_rounds": "1 of 3", "decision": "changes_required", "findings": 13, "kind": "review_round", "pr": "PR-1", "prev_sha256": "13f26de5b2f61c035782a1c47ef975bd98c581e349173d233dd86df1f7f9b56f", "program_rounds": "8 of 22", "publication": "r01-review POST failed ambiguously (transient GitHub failures; bodies verified renderable via the stateless markdown endpoint); ordinary resume, then resume --resend-undelivered after checking PR #65 (absent) delivered it; no limit change", "round": 1, "seq": 10, "utc": "2026-11-25T16:30:30+00:00"}
```
SHA-256 `54bbd3e0d2b44ee629f19f931ecb75f97b263107706fe03c66f734a0467519c7`

```json
{"budget": "budget mounted-alpha2-melee-pose-20261124: spent 75.97 of 90 min; remaining 14.03 min; entries 55 (running 0, interrupted 0, unreconciled 0)", "candidate": "49b1c7b9", "child_rounds": "1 of 3 used (round 2 would make 2 of 3)", "gate": "gate-focused-49b1c7b94e9a-d43d449b6c4-af2513083e1-20261125T164039504028Z pass", "kind": "pre_review_check", "pr": "PR-1", "prev_sha256": "54bbd3e0d2b44ee629f19f931ecb75f97b263107706fe03c66f734a0467519c7", "program_rounds": "8 of 22 used (1 of 15 additional)", "round": 2, "round1_check": "ledger seq 8 (pre_review_check PR-1 round 1: 0 of 3 child, 7 of 22 program) was appended at 15:35Z, before the r01 request (15:36Z)", "seq": 11, "utc": "2026-11-25T16:40:48+00:00"}
```
SHA-256 `059b5f4d1bad237e11ea971b11cf43f6e00739e8d529371bdb4c05414495bbc5`

```json
{"budget": "budget mounted-alpha2-melee-pose-20261124: spent 76.96 of 90 min; remaining 13.05 min; entries 57 (running 0, interrupted 0, unreconciled 0)", "candidate": "the commit on codex/mmr-attack-orders whose parent is 49b1c7b9 and which adds docs/missions/mmr-split/PR-1-ledger-excerpt.md", "child_rounds": "2 of 3 used (round 3 is the last allowed)", "kind": "pre_review_check", "pr": "PR-1", "prev_sha256": "059b5f4d1bad237e11ea971b11cf43f6e00739e8d529371bdb4c05414495bbc5", "program_rounds": "9 of 22 used (2 of 15 additional)", "round": 3, "seq": 12, "utc": "2026-11-25T17:08:32+00:00"}
```
SHA-256 `46588a73e4a2f160a8cde218b4fb066e741aa610be068691970a076f6e49d8e6`

```json
{"budget": "78.52 of 90 min; 11.48 remaining", "candidate": "19fef308", "consequence": "PR-1 allocation exhausted: owner decision required. PR-2 waits on PR-1 acceptance; PR-6 waits on all. PR-3/4/5 prepared locally, not enrolled.", "decision": "changes_required", "kind": "review_round", "open": ["R1-F3", "R1-F7", "R1-F11", "R2-F1", "R2-F3", "R2-F4", "R3-F1", "R3-F2", "R3-F3", "R3-F4", "R3-F5 (P3, non-blocking)"], "pr": "PR-1", "prev_sha256": "46588a73e4a2f160a8cde218b4fb066e741aa610be068691970a076f6e49d8e6", "program_rounds": "10 of 22 (3 of 15 additional)", "round": 3, "seq": 13, "status": "BLOCKED: no progress: R1-F11, R1-F3, R1-F7 open for 3 consecutive reviews; 3 of 3 rounds", "utc": "2026-11-25T17:29:04+00:00", "verified_this_round": ["R1-F1", "R1-F2", "R1-F4", "R1-F12", "R2-F2", "R2-F5"], "verified_total": ["R1-F5", "R1-F6", "R1-F8", "R1-F9", "R1-F10", "R1-F13", "R1-F1", "R1-F2", "R1-F4", "R1-F12", "R2-F2", "R2-F5"]}
```
SHA-256 `f7775c23a15fc0491a5bf82a78c4b4c54981abdab0efe98803ff563e3061434c`

```json
{"allocations": {"PR-1": 6, "PR-2": 3, "PR-3": 3, "PR-4": 2, "PR-5": 2, "PR-6": 2}, "budget_adjustment": "allowanceAdjustments entry 2026-11-26T02:32:18Z 90 -> 150 via Update-KmcWorkflowBudgetRecord (locked); balance 78.52 of 150, 71.48 remaining, 59 entries preserved", "instruction_file": "owner-continuations/owner-continuation-2026-11-26-pr1-extension-and-budget.md", "instruction_sha256": "16db6774779cfa369bc390f4af56465844fbf902ba161317b0d16b002a671972", "kind": "owner_decision", "prev_sha256": "f7775c23a15fc0491a5bf82a78c4b4c54981abdab0efe98803ff563e3061434c", "program_rounds": {"allocated_child": 18, "lifetime_cap": 25, "spent": 10}, "seq": 14, "text": "Bounded continuation: PR-1 ceiling 3 -> 6 (rounds 4-6 authorized); other allocations unchanged; program 18 allocated child reviews / 25 lifetime (10 spent); validation allowance 90 -> 150 min on the same budget ID; sequence PR-4, PR-5, then PR-1 resume, then PR-2, PR-3, PR-6.", "utc": "2026-11-26T02:32:30+00:00"}
```
SHA-256 `e5e75214fe978088fb099fc2d6e6e9c6721f9636ea313255ec8d6679c71404fa`

```json
{"command": "agent_review.py resume --reason \"Owner authorizes three additional PR-1 review rounds, raising its total ceiling to six while preserving findings and spent rounds\" --max-rounds 6", "enrollment": "kmc-codex-mmr-attack-orders-20261125103400-6cef", "kind": "resume", "pr": "PR-1", "prev_sha256": "a5304b721e4d1d2ff7ddd0331af061e9e936d1794ea67f94c7483a16cafb5c3e", "result": "ACTIVE develop; round_limit 6; rounds_completed final=3; open findings preserved: R1-F11, R1-F3, R1-F7, R2-F1, R2-F3, R2-F4, R3-F1, R3-F2, R3-F3, R3-F4, R3-F5", "seq": 28, "status_file": "status/pr1-post-resume-status.json", "utc": "2026-11-26T04:12:01+00:00"}
```
SHA-256 `f13e6ebe116587958065b1d8cadf10dd257366f2f27517780adc5face37d73e0`

```json
{"applicable_limits": "owner instruction 2026-11-26 (seq 14; docs/missions/mmr-split/owner-authorization-2026-11-26.md): PR-1 ceiling 6 (resumed by the supported operation, seq 28), validation 150 min on the shared budget, program 18 allocated child reviews / 25 lifetime; the hash-pinned mission still states the earlier limits", "budget": "budget record read at 2026-11-26T04:49:38Z (before this entry): spent 88.66 of 150 min, remaining 61.33, 83 entries, 0 running/interrupted/unreconciled; the round-4 gate follows", "candidate": "the commit on codex/mmr-attack-orders whose parent is 7fff7fb8 and which adds the owner authorization and regenerates docs/missions/mmr-split/PR-1-ledger-excerpt.md with this entry", "child_rounds": "3 of 6 used (round 4 next)", "kind": "pre_review_check", "pr": "PR-1", "prev_sha256": "f13e6ebe116587958065b1d8cadf10dd257366f2f27517780adc5face37d73e0", "program_rounds": "14 of 25 lifetime (7 of 18 allocated child reviews)", "round": 4, "seq": 29, "utc": "2026-11-26T04:49:57+00:00"}
```
SHA-256 `649732af9c13330552370c886b85393ae0977ae925f85d8c84296dcdd1b4c3a2`

```json
{"candidate": "e9ef7f6fc129ae242e89580b401bea4e7432abe8", "child_rounds": "4 of 6 used", "decision": "changes_required", "kind": "review_result", "open_findings": ["R3-F1", "R1-F11", "R2-F4", "R3-F4", "R3-F5", "R4-F1", "R4-F2", "R4-F3", "R4-F4", "R4-F5", "R4-F6"], "pr": "PR-1", "prev_sha256": "649732af9c13330552370c886b85393ae0977ae925f85d8c84296dcdd1b4c3a2", "program_rounds": "15 of 25 lifetime (8 of 18 allocated child reviews)", "round": 4, "seq": 30, "utc": "2026-11-26T05:07:46+00:00", "verified_fixed": ["R1-F3", "R1-F7", "R2-F1", "R2-F3", "R3-F2", "R3-F3"]}
```
SHA-256 `c64519c4e64866e42ef975a92e4534e3249c0800dfbc7cd1b6e456465863c0bf`

```json
{"applicable_limits": "owner instruction 2026-11-26 (seq 14): PR-1 ceiling 6, validation 150 min, program 18 allocated child reviews / 25 lifetime", "budget": "budget record read at 2026-11-26T05:21:36Z (before this entry): spent 90.88 of 150 min, remaining 59.12, 93 entries, 0 running/interrupted/unreconciled; the round-5 gate follows", "candidate": "the commit on codex/mmr-attack-orders whose parent is 125f8989 and which regenerates docs/missions/mmr-split/PR-1-ledger-excerpt.md with this entry", "child_rounds": "4 of 6 used (round 5 next)", "kind": "pre_review_check", "pr": "PR-1", "prev_sha256": "c64519c4e64866e42ef975a92e4534e3249c0800dfbc7cd1b6e456465863c0bf", "program_rounds": "15 of 25 lifetime (8 of 18 allocated child reviews)", "round": 5, "seq": 31, "utc": "2026-11-26T05:21:49+00:00"}
```
SHA-256 `e20446dd89e3e1e109b5bca68fc23b1e63cf9ad07d71400053b11214dbba47fa`

```json
{"candidate": "390f1093c6cd8f4fd5984eec93e29e338b6a4749", "child_rounds": "5 of 6 used", "decision": "changes_required", "kind": "review_result", "open_findings": ["R1-F11", "R4-F1", "R4-F5", "R4-F6", "R5-F1", "R5-F2", "R5-F3"], "pr": "PR-1", "prev_sha256": "e20446dd89e3e1e109b5bca68fc23b1e63cf9ad07d71400053b11214dbba47fa", "program_rounds": "16 of 25 lifetime (9 of 18 allocated child reviews)", "round": 5, "seq": 32, "utc": "2026-11-26T05:52:14+00:00", "verified_fixed": ["R3-F1", "R2-F4", "R3-F4", "R3-F5", "R4-F2", "R4-F3", "R4-F4"]}
```
SHA-256 `3764c18c855df1c30c53761a682e6596d783e6f682eda4bf2bbc1614b8d8edec`

```json
{"applicable_limits": "owner instruction 2026-11-26 (seq 14): PR-1 ceiling 6 (round 6 is the last), validation 150 min, program 18 allocated child reviews / 25 lifetime", "budget": "budget record read at 2026-11-26T05:51:59Z (before this entry): spent 92.77 of 150 min, remaining 57.23, 102 entries, 0 running/interrupted/unreconciled; the round-6 gate follows", "candidate": "the commit on codex/mmr-attack-orders whose parent is c8b91abf and which regenerates docs/missions/mmr-split/PR-1-ledger-excerpt.md with this entry", "child_rounds": "5 of 6 used", "kind": "pre_review_check", "pr": "PR-1", "prev_sha256": "3764c18c855df1c30c53761a682e6596d783e6f682eda4bf2bbc1614b8d8edec", "program_rounds": "16 of 25 lifetime (9 of 18 allocated child reviews)", "round": 6, "seq": 33, "utc": "2026-11-26T05:52:14+00:00"}
```
SHA-256 `706579f5612d27e4cee4f19b8866f2b1ca47bd351d216ffe9486aa22ee4ac0f4`

```json
{"child_rounds": "6 of 6 used", "enrollment": "kmc-codex-mmr-attack-orders-20261125103400-6cef", "kind": "child_stop", "last_candidate": "e5735eb36302ef76582ff4a6a38ea9a311b361f6", "next": "stop preserved; PR-2 (depends on PR-1 acceptance) and PR-6 (depends on all acceptances) cannot proceed; continue the independent PR-3 under its own 3-round allocation", "open_findings": ["R1-F11", "R4-F1", "R4-F5", "R4-F6", "R5-F3", "R6-F1", "R6-F2"], "open_priorities": "all P2 (no P1 open)", "pr": "PR-1", "pr_number": 65, "prev_sha256": "706579f5612d27e4cee4f19b8866f2b1ca47bd351d216ffe9486aa22ee4ac0f4", "program_rounds": "17 of 25 lifetime (10 of 18 allocated child reviews)", "reason": "no progress: R1-F11, R4-F1, R4-F5, R4-F6 open for 3 consecutive reviews (round 6 of 6, the owner-raised ceiling)", "seq": 34, "status": "BLOCKED", "utc": "2026-11-26T06:08:35+00:00", "verified_fixed_in_r6": ["R5-F1", "R5-F2"]}
```
SHA-256 `120216ca3d1af31d849b2714db08e2737dc1ebc47098f914243b9d279798cc1b`

```json
{"correct": "from blocks_readiness in the latest accepted results: #65 e5735eb3 7 blocking/0 optional; #66 b36f08a7 4/0; #67 97dce9a9 7/2; #68 642cb481 1/0; total 19 blockers (12 defect, 7 evidence_gap) and 2 optional_polish", "error": "priority was treated as blocking status; PR-1's seven P2 findings were described as non-blocking", "kind": "correction", "preserved": "original handoff copied unchanged to FINAL-HANDOFF-2026-11-26.as-written-0715Z.md (sha256 ac2323a4b8f5d53bf9681e0e582767fb00eb4a4f854bee25c3d76a6990b5b0cf); review JSON, reviewer flags and controller findings untouched", "prev_sha256": "08f02c58d1abb39dcf0a3e18dd24fb502d83ee834d12bc16a359736da601ece8", "seq": 42, "source": "owner instruction 2026-11-26b section 1", "subject": "FINAL-HANDOFF-2026-11-26.md (07:15Z)", "utc": "2026-11-26T12:08:34+00:00"}
```
SHA-256 `19b938522a1a8ef48e3a6e681476c8a70a6b65fac5c1c98bf8bcdf122813d63d`

```json
{"instruction": "owner-continuations/owner-continuation-2026-11-26b-review-extension.md", "kind": "owner_decision", "prev_sha256": "19b938522a1a8ef48e3a6e681476c8a70a6b65fac5c1c98bf8bcdf122813d63d", "program": "26 child / 33 lifetime reviews (incl. PR #64's 7); 20 lifetime spent; 13 available (8 new + 5 reserved for PR-2 and PR-6)", "review_ceilings": {"PR-1 #65": 8, "PR-2": 3, "PR-3 #68": 5, "PR-4 #66": 4, "PR-5 #67": 4, "PR-6": 2}, "seq": 43, "sequence": ["#68", "#66", "#67", "#65", "PR-2", "PR-6"], "sha256": "8f45f1bbb55ae10e9ea45c9c9d9394995da85f752a2ee8ed01b7d8720767c261", "utc": "2026-11-26T12:08:34+00:00", "validation": "unchanged at 150 min; budget record read at 2026-11-26T12:08:34Z: budget mounted-alpha2-melee-pose-20261124: spent 100.79 of 150 min; remaining 49.22 min; entries 117 (running 0, interrupted 0, unreconciled 0)"}
```
SHA-256 `0cbfb0423e613d5f989cee870982da615afbad39bb4f6ab8d15d9594198619e0`

```json
{"after": "ACTIVE, 6 of 8, seven blockers open; no-progress streaks preserved (any of the four still open after round 7 stops the child)", "authority": "owner instruction 2026-11-26b (docs/missions/mmr-split/owner-authorization-2026-11-26b.md)", "before": "BLOCKED (no progress: R1-F11, R4-F1, R4-F5, R4-F6 open for 3 consecutive reviews), 6 of 6", "enrollment": "kmc-codex-mmr-attack-orders-20261125103400-6cef", "kind": "resume", "operation": "agent_review.py resume --max-rounds 8", "pr": "PR-1", "prev_sha256": "c8e2555bce043d06b05bdf08e7856d8d68b51808de0d6c2c98165faa84244786", "records": "decomposition/status/pr1-pre-resume-2-status.json, pr1-resume-2.out, pr1-post-resume-2-status.json", "seq": 59, "utc": "2026-11-26T14:25:05+00:00"}
```
SHA-256 `a82bcb4ff55d5bfe7a427a8dbd8122b87f4691e6e2a3856da82891f167bd5423`

```json
{"budget": "budget record read at 2026-11-26T14:25:05Z (before this entry): budget mounted-alpha2-melee-pose-20261124: spent 117.7 of 150 min; remaining 32.3 min; entries 159 (running 0, interrupted 0, unreconciled 0)", "candidate": "the commit on codex/mmr-attack-orders that regenerates docs/missions/mmr-split/PR-1-ledger-excerpt.md with this entry", "child_rounds": "6 of 8 used", "kind": "pre_review_check", "pr": "PR-1", "prev_sha256": "a82bcb4ff55d5bfe7a427a8dbd8122b87f4691e6e2a3856da82891f167bd5423", "program_rounds": "24 of 33 lifetime (17 of 26 allocated child reviews)", "round": 7, "seq": 60, "utc": "2026-11-26T14:25:05+00:00"}
```
SHA-256 `e657e17eb099c4af400f8d0d2efc185bcf03f874222e35bdf231ab27225667d3`

Controller facts for cross-checking: the r01 request comment on PR #65 was created 2026-11-25T15:35:59Z (after seq 8);
the r02 request followed gate receipt gate-focused-49b1c7b94e9a-d43d449b6c4-af2513083e1-20261125T164039504028Z (after seq 11).
Seq 14 is the owner decision of 2026-11-26 (verbatim text committed as docs/missions/mmr-split/owner-authorization-2026-11-26.md):
PR-1 ceiling 3 -> 6, validation 90 -> 150 minutes, program 18 allocated child reviews / 25 lifetime. Seq 28 records the resume
by the supported controller operation with that ceiling; seq 30 is the round-4 result, seq 31 the round-5 check, seq 32 the round-5 result,
seq 33 the round-6 check and seq 34 the round-6 stop (no progress). Seq 42 corrects the blocking counts in the final handoff; seq 43 is
the owner extension of 2026-11-26 (docs/missions/mmr-split/owner-authorization-2026-11-26b.md): PR-1 ceiling 8, program 26 child /
33 lifetime reviews, validation unchanged at 150 minutes. The PR-1 resume to 8 and the round-7 check are the last two entries.
