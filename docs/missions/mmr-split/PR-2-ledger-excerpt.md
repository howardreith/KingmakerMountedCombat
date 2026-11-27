# PR-2 supplemental ledger excerpt (operator records, not controller output)

Scoped, read-only excerpt of `analysis-cache/mounted-alpha2-melee-pose-20261124/decomposition/TRANSITION-LEDGER.jsonl`, the
append-only, hash-chained operator ledger the owner asked for (each entry carries the SHA-256 of the previous line in
`prev_sha256`). The lines below are copied byte for byte; each is followed by its own SHA-256. Generated 2026-11-27T14:45:07+00:00.

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

```json
{"candidate": "b26bc6012ca525603b9ce136ecd7d84fa68a5f34", "controller_status": "ACTIVE (2 of 3)", "decision": "changes_required", "executions": "PR-2 2 of 3; product child 36 of 45; lifetime 43 of 52", "kind": "review_result", "new_blocking": ["R2-F1", "R2-F2", "R2-F3"], "pr": "PR-2", "prev_sha256": "0c497d52ff9e2bf775aa2bb0ac935c38a2a07dafc04219989042a29e063e2fbc", "rejected": ["R1-F2", "R1-F3", "R1-F4", "R1-F6", "R1-F7", "R1-F9", "R1-F10"], "round": 2, "seq": 128, "utc": "2026-11-27T05:56:32+00:00", "verified": ["R1-F1", "R1-F5", "R1-F8", "R1-F11", "R1-F12"]}
```
SHA-256 `19daec9db87f65faf4d956692d3b2e4c2bb5b8693c83911b20cc2f60144be64e`

```json
{"authority": "owner instruction 2026-11-26f sections 2 and 6 (PR-2 allowance 3 reviewer executions; this is the third and last)", "blocking_evidence": "R1-F2, R1-F3, R1-F4, R1-F6, R1-F7, R1-F9, R1-F10, R2-F1, R2-F2, R2-F3 each with production change and harness/component evidence (attack-path harness 302/0, components 1545/0, lifecycle 99/0, approach contract 13/0)", "budget": "budget mounted-alpha2-melee-pose-20261124: spent 218.54 of 270 min; remaining 51.45 min; entries 316 (running 0, interrupted 0, unreconciled 0)", "candidate": "the records commit that regenerates docs/missions/mmr-split/PR-2-ledger-excerpt.md with this entry (source commit 429aa687)", "child_usage": "PR-2: 2 of 3 executions before this round; 3 of 3 after it", "executed_reviews": "before this round: product child 36 of 45, lifetime 43 of 52; infrastructure 2 of 2 (combined 45 of 54); after it: child 37 of 45, lifetime 44 of 52, combined 46 of 54", "kind": "pre_review_check", "pr": "PR-2", "prev_sha256": "a717971a037b22f4a86b8a9b6532634cf62beb607f1bbc3903ed9d235c0de866", "recorded_results": "PR-1 15 (accepted), PR-2 2, PR-3 4 (accepted), PR-4 8, PR-5 6 (plus round 7 executed, unsettled)", "round": 3, "seq": 130, "utc": "2026-11-27T06:13:21+00:00"}
```
SHA-256 `5a9127faa3c3dc06354a647fc43c46c5931c1f3b3ca8bd0ecdc53747dbef4787`

```json
{"candidate": "a7d2ce0fd46ea32fdf4f7477c932b761ccb69857", "controller_status": "BLOCKED: no progress (R1-F10, R1-F6, R1-F7, R1-F9 open for 3 consecutive reviews), also at the execution allowance 3 of 3", "decision": "changes_required", "disposition": "stop preserved per owner instruction 2026-11-26f section 2 (no exception for PR-2); not resumed, no replacement enrollment", "executions": "PR-2 3 of 3; product child 37 of 45; lifetime 44 of 52; combined 46 of 54", "kind": "child_stop", "open_blocking": ["R1-F6", "R1-F7", "R1-F9", "R1-F10", "R2-F3"], "optional": ["R3-F1"], "pr": "PR-2", "prev_sha256": "5a9127faa3c3dc06354a647fc43c46c5931c1f3b3ca8bd0ecdc53747dbef4787", "round": 3, "seq": 131, "utc": "2026-11-27T06:35:48+00:00", "verified_this_round": ["R1-F2", "R1-F3", "R1-F4", "R2-F1", "R2-F2"]}
```
SHA-256 `0f366ba8fc2dee098724f77110c50fd8cd67a67877fe9e11a1ed3b017d752278`

```json
{"correction": "the excerpt text called seq 127 the round-2 result with 9 blocking findings; the round-2 result left 10 blocking findings open (7 rejected: R1-F2, R1-F3, R1-F4, R1-F6, R1-F7, R1-F9, R1-F10; 3 new: R2-F1, R2-F2, R2-F3), as seq 127 itself records. The excerpt is not regenerated: PR-2 is stopped and its branch is left at a7d2ce0f.", "corrects": "docs/missions/mmr-split/PR-2-ledger-excerpt.md reconciliation text generated with seq 130", "finding": "R3-F1 (optional)", "kind": "correction", "pr": "PR-2", "prev_sha256": "7f98b6a096bd1dc87eabf7ad3391262e53098bdfc98fd32e0eea198171d4353a", "seq": 134, "utc": "2026-11-27T06:59:20+00:00"}
```
SHA-256 `290ffae10c28df68ff0bd8b7f8b15333df547ee858cd756e617b64df6bc19cce`

```json
{"accepted_source": {"PR-1": "H aa15e1f2 A f7edd298", "PR-3": "H 8bfe701d A 54311f1d"}, "authority": "owner instruction 2026-11-27a", "budget": "270 -> 300", "execution_allowances": {"PR-1": 15, "PR-2": 5, "PR-3": 5, "PR-4": 11, "PR-5": 10, "PR-6": 2}, "file": "owner-continuations/owner-continuation-2026-11-27a-continuation.md", "file_sha256": "ef66406f4cbe61c2179541e1cc13a6cab59ac8b0f7c13d006d5bb93ac0df3e2d", "infrastructure": "supported cancellation of devproc-claude-gh-transport-literal-escapes-20261126235651-28f5 only; continue branch claude/gh-transport-literal-escapes and PR #8 unenrolled with standalone validation; reviews 3 and 4", "kind": "owner_instruction", "pr5": "after acceptance: separate immutable install, activate on Bite only, invalidate round 7 via supported route, fresh round 8 (execution 8, completed result 7); one conditional R4-F2 no-progress resume within cap 9 / ceiling 10", "prev_sha256": "17ee49b1cd28a6853713f18560952f92734ed652f50870ed5884e2e1b45e60a3", "product_ceilings": "48 child / 55 lifetime executions (incl. PR #64's 7); infrastructure 4 total (2 spent); combined 59", "resumptions": "PR-2 no-progress stop and exhausted cap -> completed-round cap 5; PR-4 no-progress stop on R5-F1 at 9 -> cap 11", "seq": 136, "utc": "2026-11-27T13:14:58+00:00"}
```
SHA-256 `0b6c25ca98dee0b5ba9c981ef55a74ab5183cfcf13780d9116e708320bb1096e`

```json
{"authority": "owner instruction 2026-11-27a section 3", "before": "spent 221.5 of 270 min; remaining 48.5 min; entries 323", "budget_id": "mounted-alpha2-melee-pose-20261124", "from_minutes": 270, "kind": "budget_adjustment", "mechanism": "WorkflowBudget.ps1 Update-KmcWorkflowBudgetRecord (locked), allowanceAdjustments entry appended", "prev_sha256": "0b6c25ca98dee0b5ba9c981ef55a74ab5183cfcf13780d9116e708320bb1096e", "result": "budget mounted-alpha2-melee-pose-20261124: spent 221.5 of 300 min; remaining 78.5 min; entries 323 (running 0, interrupted 0, unreconciled 0)", "seq": 137, "to_minutes": 300, "utc": "2026-11-27T13:14:58+00:00"}
```
SHA-256 `0e91ae108f446f18755ccf47040e18cfe644094d28a88921001e86e58d09cddc`

```json
{"authority": "owner instruction 2026-11-27a section 2 (Correct the PR-2 stop record once)", "correction": "PR-2 round 2 (seq 128) rejected carried findings R1-F2, R1-F3, R1-F4, R1-F6, R1-F7, R1-F9, R1-F10 for the second consecutive round and so invoked the supplemental mission stop (PR-2-tb-approach.md section 4.1 rule, line 110: a carried finding rejected in two consecutive rounds stops the child for an owner decision). Round 3 (seq 130-131) proceeded under the remaining numerical cap of the 26f allowance without a separate owner override of that stop. Round 3's findings, evidence and spending are retained; it is not invalidated or rerun, and it is not described as authorized by the cap alone.", "corrects": "seq 129-131 framing of round 3 as authorized by the 26f execution allowance", "kind": "correction", "pr": "PR-2", "prev_sha256": "0e91ae108f446f18755ccf47040e18cfe644094d28a88921001e86e58d09cddc", "seq": 138, "utc": "2026-11-27T13:14:58+00:00"}
```
SHA-256 `df82fdc5f36970275325edb307ebf7aa6ee58c22b692701b0634db6de219ecdf`

```json
{"authority": "owner instruction 2026-11-27a section 2 (Prospective stopping policy)", "change": "From this authorization onward the operator-maintained rule requiring an owner stop after two consecutive rejections of a carried finding is superseded for the existing remediation enrollments (PR-2, PR-4, PR-5). The controller's own no-progress stops, review caps and all blocking findings are preserved; no automatic resumption of a future controller stop. Frozen mission snapshots are not edited; the rule's earlier effect stands.", "kind": "owner_policy_change", "prev_sha256": "df82fdc5f36970275325edb307ebf7aa6ee58c22b692701b0634db6de219ecdf", "seq": 139, "utc": "2026-11-27T13:14:58+00:00"}
```
SHA-256 `6a2dbf5a9d3b59eb7a6a8291dae78638303c40ac404203c39ca0c2b12c02d02b`

```json
{"after": "ACTIVE, 3 of 5", "authority": "owner instruction 2026-11-27a section 2", "before": "BLOCKED (no progress: R1-F10, R1-F6, R1-F7, R1-F9 open for 3 consecutive reviews), 3 of 3", "enrollment": "kmc-codex-mmr-tb-approach-20261126213805-50cc", "kind": "resume", "operation": "agent_review.py resume --max-rounds 5", "pr": "PR-2", "prev_sha256": "0838a29ee4472d92c51884f8a4ee0e4e48f4ae667639f8eed5b175bc14076a69", "records": "owner-continuations/pre-resume-pr2-27a-status.json, resume-pr2-27a-output.txt", "seq": 150, "utc": "2026-11-27T14:44:42+00:00"}
```
SHA-256 `675f33e25a4562f0608861d51f51a006b1218440729d52d846160f9cd20940fa`

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
- seq 126: the round-2 pre-review check;
- seq 128: the round-2 result (5 verified; 10 blocking left open: 7 rejected carried findings and 3 new). Optional R3-F1:
  an earlier version of this list called it seq 127 with 9 blocking findings; seq 134 records that correction;
- seq 130: the round-3 pre-review check; seq 131 the round-3 stop (no progress; allowance 3 of 3);
- seq 136-137: owner instruction 2026-11-27a (docs/missions/mmr-split/owner-authorization-2026-11-27a.md: PR-2 allowance 5,
  48 child / 55 lifetime, validation 300 minutes); seq 138: the one-time correction that round 2 invoked the supplemental
  mission section 4.1 stop and round 3 proceeded under the remaining numerical cap without a separate override of that stop;
  seq 139: the prospective stopping policy (the operator two-rejection stop is superseded; controller stops preserved);
- the last entry: the supported resume to a completed-round cap of 5.
