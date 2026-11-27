# PR-2 supplemental ledger excerpt (operator records, not controller output)

Scoped, read-only excerpt of `analysis-cache/mounted-alpha2-melee-pose-20261124/decomposition/TRANSITION-LEDGER.jsonl`, the
append-only, hash-chained operator ledger the owner asked for (each entry carries the SHA-256 of the previous line in
`prev_sha256`). The lines below are copied byte for byte; each is followed by its own SHA-256. Generated 2026-11-27T05:28:46+00:00.

```json
{"authority": "owner instruction 2026-11-26e", "budget": "210 minutes unchanged, no new minutes", "caps": {"PR-1": 15, "PR-2": 3, "PR-3": 5, "PR-4": 8, "PR-5": "9 after round-7 settlement", "PR-6": 2}, "exceptions": "one owner resume of PR-4's R4-F3 no-progress stop within cap 8; one ordinary resume to settle Bite round 7; one owner resume of an R4-F2 no-progress stop after settlement, cap 9", "file": "owner-continuations/owner-continuation-2026-11-26e-continuation.md", "kind": "owner_instruction", "prev_sha256": "6bc4aaa433798efb37014d888d7ae61d4e99be42aee3f96a80b2e03c7e6d122d", "program_ceilings": "42 child / 49 lifetime (including 7 on PR #64)", "seq": 100, "sha256": "e16e9b72bfe4588e59c6b0f3f628b0528a3d1c48e744b4dced67da5252b0354b", "utc": "2026-11-27T01:47:39+00:00"}
```
SHA-256 `79073cb9b25e833343d64310a17aa4dd32ae4fe6abdee84bd97ad85ad073eebc`

```json
{"authority": "owner instruction 2026-11-26e section 7 (PR-2 starts on PR-1 accepted source; existing three-review allocation)", "base": "PR-1 source H aa15e1f2277364ed9dc2c2c322462fa4a9166322 (target branch codex/mmr-attack-orders, whose head is archive A f7edd298)", "enrollment": "kmc-codex-mmr-tb-approach-20261126213805-50cc", "github_pr": 70, "kind": "enrollment", "local_focused": "16 of 16 children, harness 245/0, lifecycle 99/0", "max_rounds": 3, "pr": "PR-2", "prev_sha256": "16a65518abc3990cb754ffb2d3c9118f9a735f8d7462b9d3126d95b97ac08591", "reconciliation": "prepared commits e6b75f29, 6d531c00, 356a0f08 cherry-picked onto H as aeaf42df, 9cbd4b2f, 46e573d1; conflicts at shared insertion points resolved by union; reconciliation commit 003dc9bd; prepared branch preserved as codex/mmr-tb-approach-prepared-on-95c26e59", "seq": 109, "utc": "2026-11-27T02:38:23+00:00"}
```
SHA-256 `927503323769665d762e3826d7fb39b35b22aa3bdd8cb14a56028213b94611f5`

```json
{"budget": "budget mounted-alpha2-melee-pose-20261124: spent 174.38 of 210 min (read after the PR-2 gate)", "candidate": "003dc9bdd5beca6bdb84da6316810e0aece25efa", "child_rounds": "0 of 3 used", "kind": "pre_review_check", "pr": "PR-2", "prev_sha256": "927503323769665d762e3826d7fb39b35b22aa3bdd8cb14a56028213b94611f5", "program_rounds": "before this round: 32 executed child reviews of 42 (31 controller-recorded + PR-5 round 7 unsettled, counted once), 39 executed lifetime of 49; after it 33 / 40", "round": 1, "seq": 110, "utc": "2026-11-27T02:40:02+00:00"}
```
SHA-256 `3fd9a8a4e6c17fafd54b2f8909a42c9282fb2c1164c36fead208853c2cf094eb`

```json
{"correction": "seq 110 stated the budget as 174.38 minutes spent without a fresh read; the read at 2026-11-27T02:42Z shows: budget mounted-alpha2-melee-pose-20261124: spent 174.3 of 210 min; remaining 35.7 min; entries 248 (running 0, interrupted 0, unreconciled 0)", "corrects_seq": 110, "kind": "correction", "pr": "PR-2", "prev_sha256": "3fd9a8a4e6c17fafd54b2f8909a42c9282fb2c1164c36fead208853c2cf094eb", "seq": 111, "utc": "2026-11-27T02:40:14+00:00"}
```
SHA-256 `b52ea848d0134acd1d7011d129c8e0ed35d9871881084625860b354472fd2e87`

```json
{"accepted_source": {"PR-1": "H aa15e1f2 A f7edd298", "PR-3": "H 8bfe701d A 54311f1d"}, "authority": "owner instruction 2026-11-26f", "budget": "210 -> 270", "execution_allowances": {"PR-1": 15, "PR-2": 3, "PR-3": 5, "PR-4": 10, "PR-5": 10, "PR-6": 2}, "file": "owner-continuations/owner-continuation-2026-11-26f-continuation.md", "file_sha256": "dddeea571f7e09ab1026929b3565b5195db75f6a3e04828fd7273b5c7e186a8a", "kind": "owner_instruction", "pr4": "supported resume of the 8/8 cap stop to completed-round cap 10", "pr5": "transport activation invalidates round 7; completed-round cap 9, execution ceiling 10; one conditional R4-F2 no-progress resume", "prev_sha256": "4cb4c278ca4abcdd063e16c18437d65c63b399e1152bf0ecdacaac2ce6ebae60", "product_ceilings": "45 child / 52 lifetime executions; plus at most 2 infrastructure reviews (combined 54)", "seq": 119, "utc": "2026-11-27T04:52:35+00:00"}
```
SHA-256 `9092246a73e2b3a3eaa3f003d7cf4048a11143cc38273201db147cda5158bda5`

```json
{"authority": "owner instruction 2026-11-26f section 3", "before": "spent 180.6 of 210 min; remaining 29.4 min; entries 267", "budget_id": "mounted-alpha2-melee-pose-20261124", "from_minutes": 210, "kind": "budget_adjustment", "mechanism": "WorkflowBudget.ps1 Update-KmcWorkflowBudgetRecord (locked), allowanceAdjustments entry appended", "prev_sha256": "9092246a73e2b3a3eaa3f003d7cf4048a11143cc38273201db147cda5158bda5", "result": "budget mounted-alpha2-melee-pose-20261124: spent 180.6 of 270 min; remaining 89.4 min; entries 267 (running 0, interrupted 0, unreconciled 0)", "seq": 120, "to_minutes": 270, "utc": "2026-11-27T04:52:35+00:00"}
```
SHA-256 `55b58e38294a4d42524eb50c4dc832c2c86072f7b6685b5b2a706bf406bb11c3`

```json
{"candidate": "003dc9bdd5beca6bdb84da6316810e0aece25efa", "controller_status": "ACTIVE (1 of 3)", "decision": "changes_required", "kind": "review_result", "new_blocking": ["R1-F1", "R1-F2", "R1-F3", "R1-F4", "R1-F5", "R1-F6", "R1-F7", "R1-F8", "R1-F9", "R1-F10", "R1-F11"], "note": "appended late (after the closing report of 26e); original order preserved", "optional": ["R1-F12"], "pr": "PR-2", "prev_sha256": "e101c5e30103fc5620a6fdee4d8c6c9573d7cf440e447a5e809e79a25e06dc79", "round": 1, "seq": 123, "utc": "2026-11-27T05:27:20+00:00"}
```
SHA-256 `a8ab80946741e66e790fa5179fb455980b8546adf10d03fdfd8a4dfa1b265079`

```json
{"authority": "owner instruction 2026-11-26f section 6 (Accounting)", "balances_at_reconciliation": "PR-2 1 of 3 executions; product child 35 of 45, lifetime 42 of 52; infrastructure 1 of 2; combined 43 of 54", "finding": "R1-F11", "fix": "docs/missions/mmr-split/PR-2-ledger-excerpt.md (byte-for-byte lines with SHA-256) and docs/missions/mmr-split/PR-2-submission-checklist.md are committed in the next candidate; the PR-2 mission file is not edited (the controller pins its snapshot)", "kind": "reconciliation", "omission": "the record was not committed into the candidate (no PR-2 ledger excerpt existed), so the reviewer could not verify it", "original_record": "seq 110 (kind pre_review_check, utc 2026-11-27T02:40:02Z) written before the round-1 review request; its budget figure corrected by seq 111 (2026-11-27T02:40:14Z, read 174.3 of 210 min); the round-1 report file was written 2026-11-27T02:40:01Z and the review returned 02:57:34Z", "pr": "PR-2", "prev_sha256": "a8ab80946741e66e790fa5179fb455980b8546adf10d03fdfd8a4dfa1b265079", "seq": 124, "utc": "2026-11-27T05:27:20+00:00"}
```
SHA-256 `a85fd4998dfb7a1da2d27e355e8059a145b396ba909f4551e338d28a8848d959`

```json
{"authority": "owner instruction 2026-11-26f sections 2 and 6 (PR-2 allowance 3 reviewer executions)", "blocking_evidence": "R1-F1..R1-F11 each with production change and evidence (attack-path harness through actual click admission, allocation and strike; components; installed IL sequences); R1-F12 optional, fixed", "budget": "budget mounted-alpha2-melee-pose-20261124: spent 199.64 of 270 min; remaining 70.37 min; entries 293 (running 0, interrupted 0, unreconciled 0)", "candidate": "the records commit that regenerates docs/missions/mmr-split/PR-2-ledger-excerpt.md with this entry (source commit f86c50e4)", "child_usage": "PR-2: 1 of 3 executions used before this round (round 1, recorded); 2 of 3 after it", "executed_reviews": "before this round: product child 35 of 45, lifetime 42 of 52; infrastructure 1 of 2 (combined 43 of 54); after it: child 36 of 45, lifetime 43 of 52, combined 44 of 54", "kind": "pre_review_check", "pr": "PR-2", "prev_sha256": "e486a3a4a52c1872baa0c98bf3ef78b341d13e5d0fa0c942bb075c7b77709d0e", "recorded_results": "PR-1 15 (accepted), PR-2 1, PR-3 4 (accepted), PR-4 8, PR-5 6 (plus round 7 executed, unsettled)", "round": 2, "seq": 126, "utc": "2026-11-27T05:28:45+00:00"}
```
SHA-256 `fd0e94f0ba538fce4b90c221d26c88fbc78c119f09e28b8c16707f3a8e076d6a`

Reconciliation (PR #70 review R1-F11): the original round-1 pre-review record exists. Seq 110 (2026-11-27T02:40:02Z) was
written before the review request, and its budget figure was corrected by seq 111. It was not committed into the round-1
candidate, so it could not be verified there. This excerpt and docs/missions/mmr-split/PR-2-submission-checklist.md close that
gap without rewriting the chronology:
- seq 100: owner instruction 2026-11-26e;
- seq 109: enrollment on PR-1 source H aa15e1f2;
- seq 119: owner instruction 2026-11-26f (PR-2 allowance 3 reviewer executions, product ceilings 45 child / 52 lifetime, plus two
  infrastructure reviews);
- seq 120: the budget increase to 270 minutes;
- seq 123: the round-1 result, appended late;
- seq 124: the R1-F11 reconciliation;
- the last entry: the round-2 pre-review check.
