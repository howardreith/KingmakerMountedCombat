# PR-2 supplemental ledger excerpt (operator records, not controller output)

Scoped, read-only excerpt of `analysis-cache/mounted-alpha2-melee-pose-20261124/decomposition/TRANSITION-LEDGER.jsonl`, the
append-only, hash-chained operator ledger the owner asked for (each entry carries the SHA-256 of the previous line in
`prev_sha256`). The lines below are copied byte for byte; each is followed by its own SHA-256. Generated 2026-11-27T22:24:08+00:00.

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

```json
{"authority": "owner instruction 2026-11-27b", "budget": "300 -> 360", "ceilings": "51 child / 58 lifetime / 6 infrastructure; 64 combined", "execution_allowances": {"PR-1": 15, "PR-2": 5, "PR-3": 5, "PR-4": 14, "PR-5": "completed-round cap 9, execution ceiling 10", "PR-6": 2, "transport": 6}, "file": "owner-continuations/owner-continuation-2026-11-27b-continuation.md", "file_sha256": "d98eab757e164fe411777b8f2061932ecea5d45450d4bdfaadbef63d06492a34", "kind": "owner_instruction", "pr4": "supported resume of the 11/11 cap stop to 14, then a separate resume --amend-mission replacing the file-command repair protocol with a confirmed in-game action", "prev_sha256": "38b301b052e4ca39f4e2a5a7d49c572e87f517257068eb5d584aa9c677768160", "seq": 152, "transport": "bounded completion of the Link failure family, complete unit gate, reviews 5 (and 6)", "utc": "2026-11-27T15:07:42+00:00"}
```
SHA-256 `21400c4fbb6084167dc0dc747e52bc76471d1f3fe59e6846f272012d8e6f90da`

```json
{"authority": "owner instruction 2026-11-27b section 2", "before": "spent 264.6 of 300; remaining 35.4; entries 349", "budget_id": "mounted-alpha2-melee-pose-20261124", "from_minutes": 300, "kind": "budget_adjustment", "mechanism": "WorkflowBudget.ps1 Update-KmcWorkflowBudgetRecord (locked), allowanceAdjustments entry appended", "note": "the adjustment authorizingText reads I authorize increasing the existing shared validation allowance from 300 to 360 minutes (a paraphrase); the verbatim owner text is: Increase the existing shared validation allowance from 300 to 360 minutes using the locked allowance-adjustment mechanism and this authorization (the record cites the instruction file and its sha256)", "prev_sha256": "21400c4fbb6084167dc0dc747e52bc76471d1f3fe59e6846f272012d8e6f90da", "result": "spent 264.6 of 360 min; remaining 95.4 min; entries 349 (running 0, interrupted 0, unreconciled 0)", "seq": 153, "to_minutes": 360, "utc": "2026-11-27T15:07:42+00:00"}
```
SHA-256 `8f0a12e31176bc70e4f0a9d03755a89609eb22143cf67bda5cf20aedb5125844`

```json
{"authority": "owner instruction 2026-11-27b section 3", "correction": "The 27a report said a PR-2 round-4 submission leaving any of R1-F6, R1-F7, R1-F9, R1-F10 open would be stopped again by the controller. That was wrong: seq 150 is a supported resume from a no-progress stop (ACTIVE, 3 of 5), and the pinned controller _resume_updates resets every finding rounds_open to 0 after a no-progress resume (owner reviewed the disagreement; counting restarts). Findings, history and spent reviews are not erased. Pinned state read 2026-11-27: status ACTIVE, rounds_completed final 3, limits max_review_rounds_per_checkpoint 5, rounds_open 0 for R1-F6, R1-F7, R1-F9, R1-F10, R2-F3 and R3-F1 (all still open). No discrepancy with the documented transition.", "corrects": "decomposition/FINAL-HANDOFF-2026-11-27a.md, PR-2 section and decision 3 (and the matching 27a summary)", "kind": "correction", "prev_sha256": "8f0a12e31176bc70e4f0a9d03755a89609eb22143cf67bda5cf20aedb5125844", "seq": 154, "utc": "2026-11-27T15:07:42+00:00"}
```
SHA-256 `81b505561a4a3eb5d7d21c9d70af8a83e9239726822526638f59bd837ebd3ef5`

```json
{"authority": "owner instructions 2026-11-27a section 6 and 2026-11-27b section 3", "budget": "spent 265.84 of 360 min; remaining 94.17 min", "candidate": "the records commit that regenerates docs/missions/mmr-split/PR-2-ledger-excerpt.md with this entry (source commit 49e8cef0)", "checks": "components 1545/0, lifecycle contract 99/0, approach native contract 17/0", "child_usage": "PR-2 3 of 5 before; 4 of 5 after", "counterexamples": "R1-F10 production pairing both arrangements and adoption after the horse acted; R1-F6 1/60 s and 0.1 s ticks inside/beyond; R1-F7 standalone read and handler-less dispatch then a same-target request; R1-F9 live wrapper weapon/reach changed (moving and in-range); R2-F3 101.25-degree arrival with a 25 mm opening and a closed control", "executions": "product child 40 -> 41 of 51; lifetime 47 -> 48 of 58; infrastructure 4 of 6; combined 51 -> 52 of 64", "kind": "pre_review_check", "pr": "PR-2", "prev_sha256": "81b505561a4a3eb5d7d21c9d70af8a83e9239726822526638f59bd837ebd3ef5", "production_path": "attack-path harness on spliced production members (322/0); simulated engine boundary: facade TurnController/agent/pathing", "round": 4, "seq": 155, "utc": "2026-11-27T15:24:43+00:00"}
```
SHA-256 `d0db221a16fed8055a09d0d9183fa64e0feaabdeaf383876c283bcb6264c577c`

```json
{"candidate": "8f64faf52e69c6ac55c03a750f22a894137083c9", "controller_status": "ACTIVE (4 of 5)", "decision": "changes_required", "executions": "PR-2 4 of 5; product child 41 of 51; lifetime 48 of 58; infrastructure 5 of 6; combined 53 of 64", "kind": "review_result", "new_blocking": ["R4-F1", "R4-F2", "R4-F3"], "pr": "PR-2", "prev_sha256": "a4c1e9d27825f14a323c3131f54256713815c8fb800949654ed7d6a438db6ec3", "rejected": ["R1-F6", "R1-F7", "R1-F10"], "round": 4, "seq": 160, "utc": "2026-11-27T15:42:31+00:00", "verified": ["R1-F9", "R2-F3", "R3-F1"]}
```
SHA-256 `c1a82cfa2782bff9a179739104da629e6f18b499b459e3f259855ddd5909c604`

```json
{"authority": "owner instructions 2026-11-27a section 6 and 2026-11-27b section 3 (the last allowed PR-2 execution)", "budget": "spent 287.6 of 360 min", "candidate": "the records commit that regenerates docs/missions/mmr-split/PR-2-ledger-excerpt.md with this entry", "checks": "harness 325/0, components 1545/0, lifecycle contract 99/0, approach native contract 19/0", "child_usage": "PR-2 4 of 5 before; 5 of 5 after", "counterexamples": "R1-F6 restricted single move at 1/60 s and 0.1 s incl. the 0.12 m opening and an unaffordable new target with a prior order; R4-F1 cancel before the first tick then an affordable ground move; R1-F7 throwing dispatch (transpiler executed under installed Harmony 1.2); R4-F2 unscoped read and scoped direct input; R4-F3 heading-independent farther flank; R1-F10 both roster orders through production SuppressPairedCandidate", "executions": "product child 41 -> 42 of 51; lifetime 48 -> 49 of 58; infrastructure 5 of 6; combined 53 -> 54 of 64", "kind": "pre_review_check", "pr": "PR-2", "prev_sha256": "c1a82cfa2782bff9a179739104da629e6f18b499b459e3f259855ddd5909c604", "round": 5, "seq": 161, "utc": "2026-11-27T15:50:39+00:00"}
```
SHA-256 `c4790df7597eeb27fb35fa5481793741cc4ed3c8d7f58427a4aa13ed4fc9ee1f`

```json
{"candidate": "e1a24af85890f10ae4e738e09de7ad333c62db31", "controller_status": "BLOCKED: review-round cap reached for final (5/5)", "decision": "changes_required", "disposition": "cap stop preserved (allowance 5 exhausted; no further resume authorized)", "executions": "PR-2 5 of 5; product child 42 of 51; lifetime 49 of 58; infrastructure 6 of 6; combined 55 of 64", "kind": "child_stop", "open_blocking": ["R1-F6", "R5-F1", "R5-F2", "R5-F3"], "pr": "PR-2", "prev_sha256": "dcea2a73404a06c285c6d4c5ed4d60fa9642f897a6f6c37ec9bd651d36ac6698", "round": 5, "seq": 164, "utc": "2026-11-27T16:07:51+00:00", "verified_this_round": ["R1-F10", "R1-F7", "R4-F1", "R4-F2", "R4-F3"]}
```
SHA-256 `747ead57126b56f43a494a33edf69b33539cde4269d60ddfba5384a41a213dac`

```json
{"authority": "owner instruction 2026-11-27c (new authority beyond 27b)", "budget": "360 -> 390", "ceilings": "55 child / 62 lifetime (incl. PR #64 seven) / 8 infrastructure; 70 combined", "execution_allowances": {"PR-1": 15, "PR-2": 7, "PR-3": 5, "PR-4": 16, "PR-5": "completed-round cap 9, execution ceiling 10", "PR-6": 2, "transport": 8}, "file": "owner-continuations/owner-continuation-2026-11-27c-continuation.md", "file_sha256": "4f0719e8d3c8f37a229d11a5842c9568afbf483bf80fe1cc93494b9da6d40348", "kind": "owner_instruction", "prev_sha256": "51cd5492cddfec905c2fd89e9585e548073b3b4a216bcbdd2d729b29b32ef174", "resumes_authorized": ["PR-4 no-progress stop 14/14 -> cap 16", "PR-2 cap stop 5/5 -> cap 7"], "seq": 173, "tracked_record": "docs/missions/mmr-split/owner-authorization-2026-11-27c.md on the PR-2 and PR-4 branches", "utc": "2026-11-27T17:47:25+00:00"}
```
SHA-256 `9271b94f526c92eb26ba1aad032df758b5532517efade3aebb230bca63eefd6f`

```json
{"after": "ACTIVE, 5 of 7; open findings unchanged; rounds_open still 2,1,1,1 (a cap-only resume does NOT reset the consecutive-open streak: a round-6 candidate leaving R1-F6 open would reach 3 and be stopped for no progress)", "authority": "owner instruction 2026-11-27c section 2", "before": "BLOCKED review-round cap 5 of 5, open R1-F6 R5-F1 R5-F2 R5-F3 with rounds_open 2,1,1,1; no lock, no developer, no pending publications", "enrollment": "kmc-codex-mmr-tb-approach-20261126213805-50cc", "kind": "resume", "operation": "agent_review.py resume --max-rounds 7 --reason ... (a first invocation without --reason was refused by argparse before any state change: resume-pr2-27c-argparse-refused.txt)", "pr": "PR-2", "prev_sha256": "021917c45a93dd2e366fa58e6c2b398f990dfa7fbe6b54b573f52383ca7c15e4", "records": "owner-continuations/pre-resume-pr2-27c-status.json, resume-pr2-27c-output.txt, post-resume-pr2-27c-status.json", "seq": 175, "utc": "2026-11-27T17:48:31+00:00"}
```
SHA-256 `56e31907813dfbde8458dedb04c4976336dc331ccccea6c91bbde6f8873a5680`

```json
{"authority": "owner instruction 2026-11-27d (follows 27c)", "budget": "390 -> 450", "file": "owner-continuations/owner-continuation-2026-11-27d-continuation.md", "file_sha256": "eac6a0b8efe28d477e86f0f8fe639a726735b1ac7399aee097a52515274903ac", "frozen": "PR-1 aa15e1f2/f7edd298, PR-3 8bfe701d/54311f1d, PR-4 d615d1d5/78c9bae4, PR-5 1253836a/dc5ef19d, transport 55ce73c", "kind": "owner_instruction", "prev_sha256": "9bbb9a0abcffabdf8d4a1a2048aa01639e0da8dcde9cc06dacbe2ed6c0015f28", "review_allocations": "unchanged: PR-2 cap 7 (5 spent), PR-6 cap 2; ceilings 55 child / 62 lifetime / 8 infrastructure / 70 combined", "seq": 196, "tracked_record": "docs/missions/mmr-split/owner-authorization-2026-11-27d.md on the PR-2 branch", "utc": "2026-11-27T20:26:55+00:00"}
```
SHA-256 `2de3bf4c38736abdb1cb847997f2aeb5f384ef8523be4515aafc336a81325691`

```json
{"after": "spent 368.14 of 450; remaining 81.85; entries 439", "authority": "owner instruction 2026-11-27d section 2", "before": "spent 368.14 of 390; remaining 21.85; entries 439", "budget_id": "mounted-alpha2-melee-pose-20261124", "from_minutes": 390, "kind": "budget_adjustment", "mechanism": "WorkflowBudget.ps1 Update-KmcWorkflowBudgetRecord (locked)", "prev_sha256": "bc3ebcea391b2909b9ebd541c307e7b9ec5a101f2b7be6e9b30b67d433424b16", "seq": 198, "to_minutes": 450, "utc": "2026-11-27T20:27:10+00:00"}
```
SHA-256 `02efe26d0261a8c972b0bc376281a348394b1ab0b804b8778fbba624403bc3e9`

```json
{"authority": "owner instruction 2026-11-27d sections 3-4 (cap 7; 5 spent)", "budget": "spent ~373.1 of 450 min", "candidate": "the records commit adding owner-authorization-2026-11-27d.md and the regenerated PR-2 excerpt (source commits b748099d, 2ab72b8d, d8be2f26)", "checks": "attack-path harness 343/0, components 1545/0, approach native contract 26/0, lifecycle contract 99/0, presentation 239/0, Validate-Source 153/0 (built DLL sha256 01ec974f..., MVID afa472f2...)", "executions": "product child 49 -> 50 of 55; lifetime 56 -> 57 of 62; combined 64 -> 65 of 70", "implementation": "a delegated in-session developer agent implemented the corrections under this session; R1-F6: provisional replacement request with its own carrier, decided at that carrier first TickDelegated from the actual DeltaTime (installed scheduler order TimeController < DefaultPointerController < UnitMoveController < UnitActionController verified from IL); commit retires the previous order once through the normal path; refusal zeroes the tick before any debit and preserves the previous order, carrier and spending; TwoActions workaround removed", "kind": "pre_review_check", "open_counters_before": "R1-F6 2, R5-F1 1, R5-F2 1, R5-F3 1 (all four claimed fixed)", "pr": "PR-2", "prev_sha256": "d52875b053bd08a2a7c50da356381ac52e6fa971c5669fbc926217ecb9a37f5c", "round": 6, "seq": 200, "utc": "2026-11-27T21:10:33+00:00"}
```
SHA-256 `9e2070b2a47fc48dffc523f9e298c7f8c4cada0527bdc01f5587d6484f452636`

```json
{"candidate": "41a785095fedfc7490f85540fa7e182b5c461342", "consequence": "PR-6 cannot start: PR-2 is not accepted", "controller_status": "BLOCKED: no progress: R1-F6 open for 3 consecutive reviews (6 of 7 rounds)", "decision": "changes_required", "disposition": "genuine new controller stop preserved (owner instruction 2026-11-27d section 7); no resume authority for a no-progress stop; counters untouched", "executions": "PR-2 6 of 7; product child 50 of 55; lifetime 57 of 62; infrastructure 8 of 8; combined 65 of 70", "kind": "child_stop", "new_blocking": ["R6-F1 P1 manual target and Hold changed before commit", "R6-F2 P1 first-tick travel measured from the saved path origin", "R6-F3 P1 commit omits live rider entitlement and loses click-time target state", "R6-F4 P2 carrier revalidation treats the route-search ceiling as executable", "R6-F5 P2 continuing the earlier target leaves the newer proposal active", "R6-F6 P2 committed attack never owns its provisional carrier", "R6-F7 P2 failed installation/commit can leave the first-tick gate active"], "pr": "PR-2", "prev_sha256": "9e2070b2a47fc48dffc523f9e298c7f8c4cada0527bdc01f5587d6484f452636", "rejected": ["R1-F6 (P1: the paired scheduler cannot drive the provisional carrier to its first tick; NativeActorEligibleForCommand omits it; a fresh Preparing turn has no rider command; the harness set Acting and called TickDelegated directly)", "R5-F3 (generation read from a start counter; prior acceptance supplied in an unverified turn state)"], "result": "state rounds/r06/result-1.json sha256 40c1ff72e599ca326418c102ef24f43c2e82b6b6e3cad36a837c128fd2074149 (unedited)", "round": 6, "seq": 201, "utc": "2026-11-27T21:26:04+00:00", "verified": ["R5-F1", "R5-F2"]}
```
SHA-256 `fbf0de36a2e7b8038f0153933ec190a297705e1f05fee5ce1e307dcabeadefaa`

```json
{"authority": "owner instruction 2026-11-27e (follows FINAL-HANDOFF-2026-11-27d)", "budget": "450 unchanged (no new allowance)", "ceilings": "57 child / 64 lifetime / 8 infrastructure / 72 combined", "deliverable": "complete five-child integrated private alpha; a four-child alpha does not satisfy it", "file": "owner-continuations/owner-continuation-2026-11-27e-continuation.md", "file_sha256": "766dc61caa5aadd023f940459cf3cd58ff1fa9804f3f5d1512f9125e5290a84c", "kind": "owner_instruction", "pr2": "lift the round-6 no-progress stop (R1-F6) on kmc-codex-mmr-tb-approach-20261126213805-50cc; total cap 9", "prev_sha256": "6c0a0bda9715116bc7635917d68bdf28d03efc9e3089f26dde28ced128a05e7e", "seq": 203, "utc": "2026-11-27T21:39:48+00:00"}
```
SHA-256 `bffc226afbb5defa86a33aee56b1937226e2e586a877510133c2ddb789d0bc4e`

```json
{"after": "ACTIVE, develop, 6 of 9; findings preserved; the controller reset rounds_open to 0 (its documented no-progress-resume bookkeeping)", "authority": "owner instruction 2026-11-27e section 2", "before": "BLOCKED no progress (R1-F6 open 3 consecutive reviews), 6 of 7, open R1-F6 R5-F3 R6-F1..R6-F7", "enrollment": "kmc-codex-mmr-tb-approach-20261126213805-50cc", "kind": "resume", "operation": "agent_review.py resume --max-rounds 9 --reason ... (foreground)", "pr": "PR-2", "prev_sha256": "bffc226afbb5defa86a33aee56b1937226e2e586a877510133c2ddb789d0bc4e", "records": "owner-continuations/pre-resume-pr2-27e-status.json, resume-pr2-27e-output.txt, post-resume-pr2-27e-status.json", "seq": 204, "utc": "2026-11-27T21:40:02+00:00"}
```
SHA-256 `9a8322faeffbe68a0dc6a8c91bf76033966eb8b4eeced1fc5303accd393c90e2`

```json
{"authority": "owner instruction 2026-11-27e sections 3-4 (cap 9)", "budget": "spent ~377.5 of 450 min", "candidate": "the records commit regenerating PR-2-ledger-excerpt.md with this entry (source commit 8cec6a37, rider-side provisional order)", "checks": "attack-path harness 375/0, approach native contract 31/0, components 1545/0, lifecycle 99/0, presentation 239/0, Validate-Source 153/0", "executions": "product child 50 -> 51 of 57; lifetime 57 -> 58 of 64; combined 65 -> 66 of 72", "implementation": "delegated in-session developer agents; lead bounded inspection by this session of BeginProvisionalOrder, BindFirstTickCarrier, DecideFirstTickProposal, CommitProvisionalOrder (installation refusal before install, decision rechecks turn/addressability/exact command and carrier possession/newer work/target under click facts/rider entitlement/plan, live carrier path, success read from the accepted generation)", "kind": "pre_review_check", "open_counters_before": "all 0 after the no-progress resume", "pr": "PR-2", "prev_sha256": "9a8322faeffbe68a0dc6a8c91bf76033966eb8b4eeced1fc5303accd393c90e2", "round": 7, "seq": 205, "utc": "2026-11-27T22:24:08+00:00"}
```
SHA-256 `4473dc60e0d92d4a0978e49427da73fc71c1e6813ec66f6b5183125b99488727`

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
- seq 150: the supported resume to a completed-round cap of 5; seq 152-154: owner instruction 2026-11-27b (docs/missions/mmr-split/owner-authorization-2026-11-27b.md), the budget increase to 360 minutes and the correction of the 27a explanation (the resume reset the consecutive-open counters);
- seq 155: the round-4 pre-review check; seq 160: the round-4 result;
- the last entry: the round-5 pre-review check.
