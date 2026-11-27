# PR-1 supplemental ledger excerpt (operator records, not controller output)

Scoped, read-only excerpt of `analysis-cache/mounted-alpha2-melee-pose-20261124/decomposition/TRANSITION-LEDGER.jsonl`, the
append-only, hash-chained operator ledger the owner asked for (each entry carries the SHA-256 of the previous line in
`prev_sha256`). The lines below are copied byte for byte; each is followed by its own SHA-256. Generated 2026-11-27T02:08:55+00:00.

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

```json
{"budget": "budget record read at 2026-11-26T14:45:30Z: budget mounted-alpha2-melee-pose-20261124: spent 118.6 of 150 min; remaining 31.4 min; entries 160 (running 0, interrupted 0, unreconciled 0)", "candidate": "7c6ec09036cc68efca48baf77238ac5c5028dab5", "controller_status": "ACTIVE (7 of 8)", "decision": "changes_required", "kind": "review_result", "note": "the decomposition approval's stop rule (a carried finding rejected in two consecutive child rounds) is read against the later owner extension 2026-11-26b, which authorized rounds 7-8 for these carried findings and kept the controller's no-progress handling; the controller left the enrollment ACTIVE, so round 8 is used and the reading is reported to the owner", "open_blocking": ["R1-F11", "R4-F1", "R4-F5", "R6-F1", "R7-F1", "R7-F2"], "open_optional": ["R7-F3"], "pr": "PR-1", "prev_sha256": "e657e17eb099c4af400f8d0d2efc185bcf03f874222e35bdf231ab27225667d3", "program_rounds": "25 of 33 lifetime (18 of 26 allocated child reviews)", "resolved": ["R4-F6", "R5-F3", "R6-F2"], "round": 7, "seq": 61, "utc": "2026-11-26T14:45:30+00:00"}
```
SHA-256 `6f7663730fc96ef6dd0a34cb3fd86a93c9dedfe2d72f3b2f8f4b53068632795c`

```json
{"budget": "budget record read at 2026-11-26T14:55:34Z (before this entry): budget mounted-alpha2-melee-pose-20261124: spent 120.82 of 150 min; remaining 29.18 min; entries 167 (running 0, interrupted 0, unreconciled 0)", "candidate": "the commit on codex/mmr-attack-orders that regenerates docs/missions/mmr-split/PR-1-ledger-excerpt.md with this entry", "child_rounds": "7 of 8 used (round 8 is the last)", "kind": "pre_review_check", "pr": "PR-1", "prev_sha256": "6f7663730fc96ef6dd0a34cb3fd86a93c9dedfe2d72f3b2f8f4b53068632795c", "program_rounds": "25 of 33 lifetime (18 of 26 allocated child reviews)", "round": 8, "seq": 62, "utc": "2026-11-26T14:55:34+00:00"}
```
SHA-256 `bcb5141b48512f1a7477e007f4358c4c1ef482405eecc9cf1dd0d4d53f45b638`

```json
{"budget": "budget record read at 2026-11-26T15:19:36Z: budget mounted-alpha2-melee-pose-20261124: spent 121.44 of 150 min; remaining 28.57 min; entries 168 (running 0, interrupted 0, unreconciled 0)", "candidate": "b1575568827096453af5697f7f94b9f3d9ad814d", "controller_status": "BLOCKED (review-round cap reached for final, 8/8); stop preserved, not resumed, no replacement enrollment (owner instruction 2026-11-26b)", "decision": "changes_required", "deferral_accepted": ["R7-F3"], "kind": "review_result", "note": "program end: PR-1 is not accepted, so PR-2 (depends on PR-1 acceptance) and PR-6 (depends on every feature acceptance) cannot start; PR-4 and PR-5 stops preserved; PR-3 is the only accepted child", "open_blocking": ["R8-F1", "R8-F2"], "open_optional": ["R8-F3"], "pr": "PR-1", "prev_sha256": "bcb5141b48512f1a7477e007f4358c4c1ef482405eecc9cf1dd0d4d53f45b638", "program_rounds": "26 of 33 lifetime (19 of 26 allocated child reviews)", "records": "decomposition/pr1/developer-report.r8.json, pr1/controller-review-r8.json, status/pr1-post-review-r8-status.json; structured review state/<enrollment>/rounds/r08/envelope-1.json", "round": 8, "seq": 63, "utc": "2026-11-26T15:19:48+00:00", "verified": ["R1-F11", "R4-F1", "R4-F5", "R6-F1", "R7-F1", "R7-F2"]}
```
SHA-256 `405d3edf278e977abcc5f3ec5782f32ef3d8b0d146fc09236b6ab40d3ef4e343`

```json
{"budget": "budget record read before this entry: budget mounted-alpha2-melee-pose-20261124: spent 121.44 of 150 min; remaining 28.57 min; entries 168 (running 0, interrupted 0, unreconciled 0)", "instruction": "owner-continuations/owner-continuation-2026-11-26c-continuation.md", "kind": "owner_decision", "prev_sha256": "d66eb124e73c85b5c939da75ecc16a8b1d48c85205e1e65c444ad69835395def", "seq": 65, "sha256": "cc5331e350c1b846ba798f34fc9b1d72cc274c07e3188ff6248117f9f04d3242", "summary": "PR-3 accepted for integration (H 8bfe701d, A 54311f1d); total caps PR-1 10, PR-2 3, PR-3 5 (unchanged), PR-4 5, PR-5 6, PR-6 2; program 31 child / 38 lifetime reviews (incl. 7 on PR #64); budget unchanged at 150 min; order #65 -> #67 -> #66 -> PR-2 -> PR-6; activate each child only when selected; a later stop is preserved and not repeatedly resumed", "utc": "2026-11-26T16:16:56+00:00"}
```
SHA-256 `d5c74ba8df5f472f99ea56ee7e899bde48461b715d627eada87598c1e3d850b5`

```json
{"archive_head_A": "54311f1d0fd293028decc148c57a4b9f63e79760", "authority": "owner instruction 2026-11-26c section 1", "enrollment": "kmc-codex-mmr-horse-seat-20261126011011-d3c5", "frozen": true, "github_pr": 68, "kind": "owner_acceptance", "native": "rendered seating and save/load pending for the owner playtest; no claim that Kingmaker was run", "optional_open": "PR-3/R4-F1 optional_polish: not reopened, no formal review; clarification carried in decomposition/INTEGRATION-NOTES.md", "pr": "PR-3", "prev_sha256": "d5c74ba8df5f472f99ea56ee7e899bde48461b715d627eada87598c1e3d850b5", "reviewed_source_H": "8bfe701d448325b1d50c6f4ff2619946a8174eb9", "seq": 66, "utc": "2026-11-26T16:16:56+00:00"}
```
SHA-256 `0649f08ac102274340301bb8d78cdaa7dfdfd0b98ae3f17e3ce6fdb90ca7fac1`

```json
{"authority": "owner instruction 2026-11-26c section 3", "correction": "The 2026-11-25 supplemental two-rejection rule applies to unresolved findings carried from PR #64, including mapped successor findings for those obligations. PR-1's mission carried no open findings; PR #65's R1-F11, R4-F1 and R4-F5 (enrollment kmc-codex-mmr-attack-orders-20261125103400-6cef) were child-generated. Round 8 therefore used its existing authorization (2026-11-26b ceiling 8) without any inferred override of that rule. This is not a rule that larger caps override stopping conditions; the narrower carried-finding rule still applies wherever carried PR #64 findings exist. Seq 61 is preserved unchanged.", "corrects_seq": 61, "kind": "correction", "optional_distinction": "PR-1/R7-F3 (optional) was deferred and the deferral accepted in round 8; PR-1/R8-F3 (optional_polish, receipt inventory) is a separate, currently open optional finding raised in round 8.", "prev_sha256": "0649f08ac102274340301bb8d78cdaa7dfdfd0b98ae3f17e3ce6fdb90ca7fac1", "seq": 67, "utc": "2026-11-26T16:16:56+00:00"}
```
SHA-256 `f0f414ed53d3703340331330af2cb7d445fcbb79ad2a318c2a0c2e42e67b30da`

```json
{"after": "ACTIVE, 8 of 10", "authority": "owner instruction 2026-11-26c section 2 (docs/missions/mmr-split/owner-authorization-2026-11-26c.md)", "before": "BLOCKED (review-round cap reached for final, 8/8)", "enrollment": "kmc-codex-mmr-attack-orders-20261125103400-6cef", "kind": "resume", "operation": "agent_review.py resume --max-rounds 10", "pr": "PR-1", "prev_sha256": "f0f414ed53d3703340331330af2cb7d445fcbb79ad2a318c2a0c2e42e67b30da", "records": "decomposition/status/pr1-pre-resume-3-status.json, pr1-resume-3.out, pr1-post-resume-3-status.json", "seq": 68, "utc": "2026-11-26T16:26:32+00:00"}
```
SHA-256 `d5a802ac65eca9d314cc7d630c2651adcb51e3b43b803a78d26c79f21738c8c8`

```json
{"budget": "budget record read at 2026-11-26T16:26:31Z (before this entry): budget mounted-alpha2-melee-pose-20261124: spent 122.87 of 150 min; remaining 27.13 min; entries 178 (running 0, interrupted 0, unreconciled 0)", "candidate": "the commit on codex/mmr-attack-orders that regenerates docs/missions/mmr-split/PR-1-ledger-excerpt.md with this entry", "child_rounds": "8 of 10 used", "kind": "pre_review_check", "pr": "PR-1", "prev_sha256": "d5a802ac65eca9d314cc7d630c2651adcb51e3b43b803a78d26c79f21738c8c8", "program_rounds": "26 of 38 lifetime (19 of 31 allocated child reviews)", "round": 9, "seq": 69, "utc": "2026-11-26T16:26:32+00:00"}
```
SHA-256 `cc2edf9cbca9d8ee7d14300cd01492a53cbf2b60bc4b44df8d682672f57a0564`

```json
{"candidate": "f2f2fa554a501d5fb1c2e276a9bd988df697fc58", "controller_status": "ACTIVE (9 of 10)", "decision": "changes_required", "kind": "review_result", "open_blocking": ["R9-F1", "R9-F2", "R9-F3"], "open_optional": ["R9-F4"], "pr": "PR-1", "prev_sha256": "cc2edf9cbca9d8ee7d14300cd01492a53cbf2b60bc4b44df8d682672f57a0564", "records": "decomposition/pr1/developer-report.r9.json, pr1/controller-review-r9.json", "round": 9, "seq": 70, "utc": "2026-11-26T16:43:57+00:00", "verified": ["R8-F1", "R8-F2", "R8-F3"]}
```
SHA-256 `4b43ce0a40e0d9ef5fcb7aafcfc6dff1bbe9974c36d1e62f63d5c34bf51401ab`

```json
{"budget": "budget record read at 2026-11-26T16:43:57Z (before this entry): budget mounted-alpha2-melee-pose-20261124: spent 125.02 of 150 min; remaining 24.98 min; entries 184 (running 0, interrupted 0, unreconciled 0)", "candidate": "the commit on codex/mmr-attack-orders that regenerates docs/missions/mmr-split/PR-1-ledger-excerpt.md with this entry", "child_rounds": "9 of 10 used (round 10 is the last)", "kind": "pre_review_check", "pr": "PR-1", "prev_sha256": "4b43ce0a40e0d9ef5fcb7aafcfc6dff1bbe9974c36d1e62f63d5c34bf51401ab", "program_rounds": "27 of 38 lifetime (20 of 31 allocated child reviews)", "round": 10, "seq": 71, "utc": "2026-11-26T16:43:57+00:00"}
```
SHA-256 `0d55f156293c8bb9fe2c5229b3704431eb623046648f17ada8b7de84825cdb4f`

```json
{"budget": "budget record read at 2026-11-26T17:00:35Z: budget mounted-alpha2-melee-pose-20261124: spent 125.72 of 150 min; remaining 24.28 min; entries 185 (running 0, interrupted 0, unreconciled 0)", "candidate": "6e51224edd3ab3475f79b692d8c5f2ce973f7452", "consequence": "PR-2 (depends on PR-1 acceptance) and PR-6 cannot start; work continues with PR-5 then PR-4 (owner instruction 2026-11-26c section 4)", "controller_status": "BLOCKED (review-round cap reached for final, 10/10); stop preserved: not resumed, no replacement enrollment (owner instruction 2026-11-26c section 2)", "decision": "changes_required", "kind": "review_result", "open_blocking": ["R10-F1", "R10-F2", "R10-F3", "R10-F4", "R9-F3"], "open_optional": ["R10-F5"], "pr": "PR-1", "prev_sha256": "0d55f156293c8bb9fe2c5229b3704431eb623046648f17ada8b7de84825cdb4f", "program_rounds": "28 of 38 lifetime (21 of 31 allocated child reviews)", "records": "decomposition/pr1/developer-report.r10.json, pr1/controller-review-r10.json, status/pr1-post-review-r10-status.json", "rejected": ["R9-F3"], "round": 10, "seq": 72, "utc": "2026-11-26T17:00:35+00:00", "verified": ["R9-F1", "R9-F2", "R9-F4"]}
```
SHA-256 `9ca96d91b3ea55253197bc4fe4f541243922e507bbe7eea01cb9aac77b2f332f`

```json
{"accepted": ["PR-3 #68 H 8bfe701d A 54311f1d"], "authority": "owner instruction 2026-11-26c", "budget": "136.82 of 150 min spent at 2026-11-26T18:18:05Z", "deliverable": "integrated private alpha not produced: work allowance exhausted, original functionality incomplete", "file": "decomposition/FINAL-HANDOFF-2026-11-26c.md", "kind": "final_report", "not_started": ["PR-2", "PR-6"], "open_blocking_total": 13, "open_optional_total": 2, "prev_sha256": "e84eacb070aa18e0ae4526635b6a4bccc7dfd8495d814f5dd2f8a8df4aa97d8e", "program_rounds": "32 of 38 lifetime (25 of 31 child)", "seq": 83, "sha256": "4d855e9caaa11a4a0981315a21e9f81005a40cbec55711ba5a5c459e8b17a930", "stopped": ["PR-1 #65 10/10 @6e51224e", "PR-5 #67 6/6 @84036b7c", "PR-4 #66 5/5 @2b59c0e8"], "utc": "2026-11-26T18:19:03+00:00"}
```
SHA-256 `1e8c33c855f7387a8a6fec8570c5d5c1d8750c259a231f7f384b0e0b85ff8b9b`

```json
{"authority": "owner instruction 2026-11-26d", "caps": {"PR-1": 13, "PR-2": 3, "PR-3": 5, "PR-4": 8, "PR-5": 8, "PR-6": 2}, "file": "owner-continuations/owner-continuation-2026-11-26d-continuation.md", "kind": "owner_instruction", "note": "PR-3 stays accepted and frozen (H 8bfe701d, A 54311f1d); ledger-61 correction retained", "order": "PR-1, PR-2, PR-5, PR-4, PR-6", "prev_sha256": "1e8c33c855f7387a8a6fec8570c5d5c1d8750c259a231f7f384b0e0b85ff8b9b", "program_ceilings": "39 child / 46 lifetime (including 7 on PR #64)", "seq": 84, "sha256": "cee6b6047f4c82db84a21b93cae3cf54dbcabfc9297c61546b2f3232ea866ca1", "utc": "2026-11-26T22:27:27+00:00"}
```
SHA-256 `85e25b3bd9eaefcdf82b4fa8444accf8a2b98f5c8d0a83890c1fe5213a498d0d`

```json
{"authority": "owner instruction 2026-11-26d section 2", "budget_id": "mounted-alpha2-melee-pose-20261124", "from_minutes": 150, "kind": "budget_adjustment", "mechanism": "WorkflowBudget.ps1 Update-KmcWorkflowBudgetRecord (locked), allowanceAdjustments entry appended", "prev_sha256": "85e25b3bd9eaefcdf82b4fa8444accf8a2b98f5c8d0a83890c1fe5213a498d0d", "result": "budget mounted-alpha2-melee-pose-20261124: spent 136.82 of 210 min; remaining 73.18 min; entries 206 (running 0, interrupted 0, unreconciled 0)", "seq": 85, "to_minutes": 210, "utc": "2026-11-26T22:27:27+00:00"}
```
SHA-256 `e3b6c0e2ffb6b9a24ae6fe0285b8f592c0f6a1761c93731ceb93316674ec96a7`

```json
{"authority": "owner instruction 2026-11-26d section 2", "enrollment": "kmc-codex-mmr-attack-orders-20261125103400-6cef", "kind": "resume", "operation": "agent_review.py resume --max-rounds 13 (supported operation; no counters reset, no replacement enrollment)", "output": "owner-continuations/resume-pr1-26d-output.txt", "pr": "PR-1", "prev_sha256": "e3b6c0e2ffb6b9a24ae6fe0285b8f592c0f6a1761c93731ceb93316674ec96a7", "result": "ACTIVE, phase develop, rounds_completed final 10, round_limit 13, open R10-F1..F5, R9-F3", "seq": 86, "utc": "2026-11-26T22:43:34+00:00"}
```
SHA-256 `6665cc7bd54501dc66cecc7f05bf12401e698c839710931e4afb8649de9c96dd`

```json
{"budget": "budget mounted-alpha2-melee-pose-20261124: spent 139.22 of 210 min; remaining 70.78 min; entries 210 (running 0, interrupted 0, unreconciled 0)", "candidate": "c8e7cf5e (source); the records commit that regenerates docs/missions/mmr-split/PR-1-ledger-excerpt.md with this entry follows", "child_rounds": "10 of 13 used", "invariant_map": {"entry_points": ["ObserveAutomaticProposal (before the retained order can refuse a replacement)", "TryDriveStockAttackIntent (before dispatch)", "TryRetireStockIntent via AdmitQueuedAutomaticRun (queued promotion)", "MountedPairAttackCommand.EndIfAutomaticOrderNotAdmitted at TickPreStartApproach, OnStart, TickApproach/Repath/BeginDelegatedMove, StartChildAttack and the windup check in OnTick"], "findings": ["R10-F1", "R10-F2", "R10-F3", "R10-F4", "R9-F3", "R10-F5 (comment only, no separate review)"], "fixture_outputs": "attack-path harness 185/0: Round11RetainedAdmission (4 rows), Round11InstalledWrapper (3), Round11QueuedPromotion (2), Round11InvalidationWithAdmissionLoss (6 rows, each exactly TargetInvalid|Expected target invalidation: target became unconscious|TargetInvalid), Round11ExactAssertionRejectsAppendedCode, Round9InvalidationBeforeGeometry exact comparison; lifecycle contract 80/0; local Fast tier 4/0", "invariant": "an unreleased automatic order (generation, target, owned command) has one current admission answer, typed in one precedence: target invalid, then unperceived, then outside native IsTargetValid (restricted rule with the proposing action's current range read from native for the rider), else admitted; every observer gets the same answer and the order is retired once through the one path for that cause"}, "kind": "pre_review_check", "pr": "PR-1", "prev_sha256": "6665cc7bd54501dc66cecc7f05bf12401e698c839710931e4afb8649de9c96dd", "program_rounds": "32 of 46 lifetime (25 of 39 child) before this round", "round": 11, "seq": 87, "utc": "2026-11-26T22:43:34+00:00"}
```
SHA-256 `35edc6fb2b0a58bdad29c18a347f42831a24e4a92357c4ee07488020970aa267`

```json
{"candidate": "55869192b442567a6a921eb875c49b5aebb4289b", "controller_status": "ACTIVE (rounds 11 of 13)", "decision": "changes_required", "kind": "review_result", "open_blocking": ["R11-F1", "R11-F2", "R11-F3"], "open_optional": ["R11-F4"], "pr": "PR-1", "prev_sha256": "35edc6fb2b0a58bdad29c18a347f42831a24e4a92357c4ee07488020970aa267", "program_rounds": "33 of 46 lifetime (26 of 39 child)", "round": 11, "seq": 88, "utc": "2026-11-26T23:09:09+00:00", "verified": ["R10-F1", "R10-F2", "R10-F3", "R10-F4", "R10-F5", "R9-F3"]}
```
SHA-256 `b159fecba7fe781dca7cba937c0d0e657086b1c944f6d4b3a1618e3759900b6e`

```json
{"budget": "budget mounted-alpha2-melee-pose-20261124: spent 142.44 of 210 min; remaining 67.57 min; entries 214 (running 0, interrupted 0, unreconciled 0)", "candidate": "the records commit that regenerates docs/missions/mmr-split/PR-1-ledger-excerpt.md with this entry", "child_rounds": "11 of 13 used", "invariant_map": {"entry_points": ["ObserveAutomaticProposal", "TryDriveStockAttackIntent", "TryRetireStockIntent/AdmitQueuedAutomaticRun", "the order's own command at pre-start, start, approach, carrier, windup and its own target-loss observation (TryEndExpectedTargetInvalidation)"], "findings": ["R11-F1", "R11-F2", "R11-F3", "R11-F4 (optional, same pass)"], "fixture_outputs": "attack-path harness 200/0: Round12AiOffBeforeDelivery (AI off: windup, delivered 0, spends 0, retirement 'ai off'; AI-on and explicit controls delivered 1), Round12MountOnlyHold (A accepted before the drive, 'hold out of reach', A struck without travel; in-reach B control kept), Round12MissingMemory (group or memory removed: retired as perception loss, moves 0; perceived and explicit controls advance), R11-F4 diagnostics equal on all six invalidation rows; lifecycle contract 82/0; local Fast tier 4/0", "invariant": "the round-11 invariant, completed: the automatic order's one typed admission decision also carries its own boundaries (MountedAutomaticAttackPolicy.RetireReason: real time, newer player work, direct control, AI off, brain inactive, Stop, stationary strike under any forbidden pair movement), and an unavailable rider group or memory is perception loss; TryRetireStockIntent defers to it entirely for automatic orders"}, "kind": "pre_review_check", "pr": "PR-1", "prev_sha256": "b159fecba7fe781dca7cba937c0d0e657086b1c944f6d4b3a1618e3759900b6e", "program_rounds": "33 of 46 lifetime (26 of 39 child) before this round", "round": 12, "seq": 89, "utc": "2026-11-26T23:09:10+00:00"}
```
SHA-256 `977e926b20abb66ccea5a12d7014e6c7555f5b4f945019207c01a114b3336445`

```json
{"candidate": "29816172ad6f416f3590e5a8532ceea8edc66123", "controller_status": "ACTIVE (rounds 12 of 13; round 13 is the last)", "decision": "changes_required", "kind": "review_result", "open_blocking": ["R12-F1", "R12-F2"], "open_optional": ["R12-F3", "R12-F4"], "pr": "PR-1", "prev_sha256": "977e926b20abb66ccea5a12d7014e6c7555f5b4f945019207c01a114b3336445", "program_rounds": "34 of 46 lifetime (27 of 39 child)", "round": 12, "seq": 90, "utc": "2026-11-26T23:27:05+00:00", "verified": ["R11-F1", "R11-F2", "R11-F3", "R11-F4"]}
```
SHA-256 `532c44b355f4a574aaae168f44b3db8300ed018db9f2bc4f9d1b93788cdc1c70`

```json
{"budget": "budget mounted-alpha2-melee-pose-20261124: spent 144.55 of 210 min; remaining 65.45 min; entries 217 (running 0, interrupted 0, unreconciled 0)", "candidate": "the records commit that regenerates docs/missions/mmr-split/PR-1-ledger-excerpt.md with this entry", "child_rounds": "12 of 13 used (round 13 is the last)", "invariant_map": {"entry_points": ["RetireAutomaticOrderIfNotAdmitted at the drive and proposal (released command: RetireOrderWrapper LeaveToNative)", "MountedPairAttackCommand OnStart/TickApproach/TickDeferredCarrier ask the decision before the generic approach restriction", "TryDriveStockAttackIntent decides an automatic order once"], "findings": ["R12-F1", "R12-F2", "R12-F3 (optional, same pass)", "R12-F4 (optional, same pass)"], "fixture_outputs": "attack-path harness 204/0 with AutomaticMovementForbidden spliced: Round13AiOffAfterDelivery (released, wrapper running, 'ai off', routine completed with struck 2, one Standard, 0 new dispatches, fresh proposal admitted), Round12MountOnlyHold via horse HoldState (preReadinessSpends 0, one delivery on A / on B), Round13HeldDuringApproach (controller and wrapper first: 'hold out of reach|1', carrier released, no delivery), Round13OneProbePerDrive (1 probe); lifecycle contract 82/0; local Fast tier 4/0", "invariant": "the automatic order's one admission decision governs its generation at the controller's boundaries even after delivery (the generation is released; a delivered routine keeps native completion, with no admission-loss cause written on it); pair movement restriction comes from production AutomaticMovementForbidden (the horse's HoldState through MountedHoldProvenance)"}, "kind": "pre_review_check", "pr": "PR-1", "prev_sha256": "532c44b355f4a574aaae168f44b3db8300ed018db9f2bc4f9d1b93788cdc1c70", "program_rounds": "34 of 46 lifetime (27 of 39 child) before this round", "round": 13, "seq": 91, "utc": "2026-11-26T23:27:05+00:00"}
```
SHA-256 `c1f8f417df4ff2e1b455b9a5f75561b397cf5486a0aaa673b448a725dc63c975`

```json
{"candidate": "915f2fe7380b2439c97d5cc5bfccf5e233e3be32", "consequence": "PR-2 cannot start (needs PR-1's accepted source); continuing with independently authorized PR-5 (owner instruction 2026-11-26d section 4)", "controller_status": "BLOCKED (review-round cap reached for final, 13/13); stop preserved: not resumed, no replacement enrollment", "decision": "changes_required", "kind": "review_result", "open_blocking": ["R13-F1", "R13-F2"], "open_optional": ["R13-F3"], "pr": "PR-1", "prev_sha256": "c1f8f417df4ff2e1b455b9a5f75561b397cf5486a0aaa673b448a725dc63c975", "program_rounds": "35 of 46 lifetime (28 of 39 child)", "round": 13, "seq": 92, "utc": "2026-11-26T23:38:21+00:00", "verified": ["R12-F1", "R12-F2", "R12-F3", "R12-F4"]}
```
SHA-256 `af4bbb440e88b36b6d1ec08e1b769faec9ffe6bdb5d2bcfb0d8ebe9d02a9fdfc`

```json
{"accepted": ["PR-3 #68 H 8bfe701d A 54311f1d"], "authority": "owner instruction 2026-11-26d", "budget": "150.32 of 210 min spent at 2026-11-27T00:29:03Z", "deliverable": "integrated private alpha not produced; every feature child at a controller stop or blocked by dependency", "file": "decomposition/FINAL-HANDOFF-2026-11-26d.md", "file_sha256": "829c701cb4bac8ea62a01f428b78bdbcee026c097c161a6432b9115468563e6b", "kind": "final_report", "not_started": ["PR-2", "PR-6"], "open_blocking_total": 9, "prev_sha256": "6eed31481c283615225e65a61396fe7f1b167d23d1b3553f11b6470e54c29811", "program_reviews": "37 of 46 lifetime run (30 of 39 child, incl. PR-5 round 7 unsettled)", "seq": 99, "stopped": {"PR-1": "BLOCKED cap 13/13", "PR-4": "BLOCKED no progress (R4-F3, 3 consecutive reviews)", "PR-5": "PAUSED round-7 verdict unsettled (GitHub altered literal \u0000 text)"}, "utc": "2026-11-27T00:30:13+00:00"}
```
SHA-256 `6bc4aaa433798efb37014d888d7ae61d4e99be42aee3f96a80b2e03c7e6d122d`

```json
{"authority": "owner instruction 2026-11-26e", "budget": "210 minutes unchanged, no new minutes", "caps": {"PR-1": 15, "PR-2": 3, "PR-3": 5, "PR-4": 8, "PR-5": "9 after round-7 settlement", "PR-6": 2}, "exceptions": "one owner resume of PR-4's R4-F3 no-progress stop within cap 8; one ordinary resume to settle Bite round 7; one owner resume of an R4-F2 no-progress stop after settlement, cap 9", "file": "owner-continuations/owner-continuation-2026-11-26e-continuation.md", "kind": "owner_instruction", "prev_sha256": "6bc4aaa433798efb37014d888d7ae61d4e99be42aee3f96a80b2e03c7e6d122d", "program_ceilings": "42 child / 49 lifetime (including 7 on PR #64)", "seq": 100, "sha256": "e16e9b72bfe4588e59c6b0f3f628b0528a3d1c48e744b4dced67da5252b0354b", "utc": "2026-11-27T01:47:39+00:00"}
```
SHA-256 `79073cb9b25e833343d64310a17aa4dd32ae4fe6abdee84bd97ad85ad073eebc`

```json
{"correction": "Seq 95 stated that GitHub stored the reviewer's literal \u0000 text as caret-at. That is wrong. A three-way comparison (decomposition/pr5/r7-publication-comparison.json) shows the preserved outbox body, the direct REST response (urllib, no gh) and the paginated REST listing (urllib) are identical: length 21093, sha256 ee1f1006aa7ef0f29b7ed2ca4192936617fc5a64b239535ac8e9a7a0cee1506d, body after the marker hashes to the marker digest family, two literal six-character sequences backslash-u0000 (indices 3386, 16266), no U+0000, no caret-at. The discrepancy first appears in the controller's own transport: gh 2.99.0 (`gh api repos/.../issues/comments/<id>` and `gh api --paginate ... --jq`) returns length 21087 with both literal backslash-u0000 sequences replaced by backslash-caret-at (first difference at index 3387: expected code points u+0075 u+0030 u+0030 u+0030 u+0030, gh output u+005E u+0040); the raw gh JSON bytes read \\^@ where the API JSON reads \\u0000. The earlier diagnosis used the same gh transport.", "corrects_seq": 95, "kind": "correction", "pr": "PR-5", "prev_sha256": "79073cb9b25e833343d64310a17aa4dd32ae4fe6abdee84bd97ad85ad073eebc", "seq": 101, "status": "Bite remains PAUSED at round 7 validated, candidate dda42b9c unchanged", "utc": "2026-11-27T01:47:39+00:00"}
```
SHA-256 `034ab0635641787cbbe1cf4fc59d0037da12fc4881ef5abe153fd65b9bf7f37d`

```json
{"authority": "owner instruction 2026-11-26e section 2", "enrollment": "kmc-codex-mmr-attack-orders-20261125103400-6cef", "kind": "resume", "operation": "agent_review.py resume --max-rounds 15", "output": "owner-continuations/resume-pr1-26e-output.txt", "pr": "PR-1", "prev_sha256": "e9a55a43f710320f913a607b2c883ac94c487d784e5261aa2f459ce5afc54a08", "seq": 103, "utc": "2026-11-27T02:03:42+00:00"}
```
SHA-256 `0739447286722da725339b3f23445b4575f34a83c010d0a50ea6b17ed3e3ab66`

```json
{"budget": "budget mounted-alpha2-melee-pose-20261124: spent 152.4 of 210 min; remaining 57.6 min; entries 239 (running 0, interrupted 0, unreconciled 0)", "candidate": "the records commit that regenerates docs/missions/mmr-split/PR-1-ledger-excerpt.md with this entry", "child_rounds": "13 of 15 used", "invariant_map": {"entry_points": ["MountedCombatController.StockIntentStrikeAdmissibleNow (used by DecideAutomaticOrderAdmission)", "MountedPairAttackCommand.AttackPlanExhausted"], "findings": ["R13-F1", "R13-F2"], "fixture_outputs": "attack-path harness 208/0 (harness Tick now defers OnEnded while the animation plays, as installed UnitCommand.Tick 0x060027A7): Round14HeldFinalActRecovery eligible: interval Result=Success/exhausted/unfinished, same generation and target, 0 retirements, 0 moves; horse follows through on the same target, spends 2 (one per actor), 0 fresh proposals; control: leaving stationary reach retires (hold out of reach); with the fix withheld the eligible case fails (retired, hold out of reach). Round14ReleasedStrikeRangeLoss sampled right after the retiring drive: cause Unrecorded, transaction Attacking, unfinished, owns the slot, 1 attack remaining; completion Success, 2 acts on B, Completed, spends unchanged at 1, 0 redispatches; local focused 4/0", "invariant": "the automatic order's stationary-strike boundary judges the retained target's current eligibility for future dispatch; a released wrapper with an exhausted plan (final act delivered, native animation finishing, IsFinished false) is judged by the production planned-strike probe, never by its empty plan; genuine loss of stationary reach, AI off, Stop, newer player work, invalid/unperceived targets and native ineligibility still retire"}, "kind": "pre_review_check", "pr": "PR-1", "prev_sha256": "0739447286722da725339b3f23445b4575f34a83c010d0a50ea6b17ed3e3ab66", "round": 14, "seq": 104, "utc": "2026-11-27T02:08:55+00:00"}
```
SHA-256 `3c0ff488b41705071ee97097ed666df1564977147a993cfc11bdc1e3313d867a`

Controller facts for cross-checking: the r01 request comment on PR #65 was created 2026-11-25T15:35:59Z (after seq 8);
the r02 request followed gate receipt gate-focused-49b1c7b94e9a-d43d449b6c4-af2513083e1-20261125T164039504028Z (after seq 11).
Seq 14 is the owner decision of 2026-11-26 (verbatim text committed as docs/missions/mmr-split/owner-authorization-2026-11-26.md):
PR-1 ceiling 3 -> 6, validation 90 -> 150 minutes, program 18 allocated child reviews / 25 lifetime. Seq 28 records the resume
by the supported controller operation with that ceiling; seq 30 is the round-4 result, seq 31 the round-5 check, seq 32 the round-5 result,
seq 33 the round-6 check and seq 34 the round-6 stop (no progress). Seq 42 corrects the blocking counts in the final handoff; seq 43 is
the owner extension of 2026-11-26 (docs/missions/mmr-split/owner-authorization-2026-11-26b.md): PR-1 ceiling 8, program 26 child /
33 lifetime reviews, validation unchanged at 150 minutes. Seq 59 is the PR-1 resume to 8, seq 60 the round-7 check, seq 61 the round-7
result, seq 62 the round-8 check, seq 63 the round-8 result (cap stop at 8/8), seq 64 the final report of 2026-11-26b;
seq 65-67 record owner instruction 2026-11-26c (total cap 10), the PR-3 acceptance and the correction to seq 61; seq 68 is the
resume to 10, seq 69 the round-9 check, seq 70 the round-9 result; the last entry is the round-10 check for the candidate that
adds this version.
