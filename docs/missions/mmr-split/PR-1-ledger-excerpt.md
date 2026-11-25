# PR-1 supplemental ledger excerpt (operator records, not controller output)

Scoped, read-only excerpt of `analysis-cache/mounted-alpha2-melee-pose-20261124/decomposition/TRANSITION-LEDGER.jsonl`, the
append-only, hash-chained operator ledger the owner asked for (each entry carries the SHA-256 of the previous line in
`prev_sha256`). The lines below are copied byte for byte; each is followed by its own SHA-256. Generated 2026-11-25T17:08:50+00:00.

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

Controller facts for cross-checking: the r01 request comment on PR #65 was created 2026-11-25T15:35:59Z (after seq 8);
the r02 request followed gate receipt gate-focused-49b1c7b94e9a-d43d449b6c4-af2513083e1-20261125T164039504028Z (after seq 11);
seq 12 records the round-3 check for the candidate that adds this file.
