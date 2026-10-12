# Chunk 6 engineering closeout — October 11, 2026

**All agreed engineering gates PASS on the unchanged released preview.211; owner acceptance remains pending.** Native campaign and matrix execution is complete. The owner explicitly selected this qualification after stopping the original release-time campaign. The authorized release remains unchanged; its original assumed outcomes remain historical assumptions. The new receipts below establish the subsequently observed results.

## Product and execution identities

| Identity | Value |
|---|---|
| Released tag / internal version | `v0.1.0-chunk6-preview.211` / `0.1.0-chunk6d-preview.211` |
| Frozen product source | `a0d70fa41f25498aa2f31dfdfebc1ad29750f4a6` |
| Product DLL SHA-256 | `11e993093d2042f615cfc9d6aecc33d630d2e41e8125dd680656b6a1057a48ea` |
| DLL MVID | `0aa68ba0-f293-480e-8ecb-d7bdaeca73d0` |
| Package SHA-256 | `e89f431ae1303b217e987b2ff12171a05bac434d97809962c3596e2942874039` |
| Manifest SHA-256 | `ca54c37cc5de332bad4514d04b585ccf5b31977a57f44c6f0e19288a769d8ec3` |
| Execution harness and acceptance-reader revision | `7e49e0348ee944108af4f8ccec5dda899097f1c1` |
| Final offline test revision | `c43f32f897c958021b9b191779707202bf5bd724` (two regression-test corrections; acceptance readers unchanged) |
| Execution reader digest | `d7a8d7d4be30cd677fb6d3520f796f33ea2e93fa709c9ab1b4b175a6c12c1bd4` |
| Suite | `20261011-chunk6-closeout211-b`, SHA-256 `fd702b307deb0c6cf9890694b2f9db61bcee133c598ebd8d87c4cee791682512` |
| Execution freeze | `candidate211-execution2-freeze.json`, SHA-256 `f65de7a06a3c70860b02ab79037f585b8c01e9edb928e287e8a4c4765c1a6e6a` |

The review branch is `codex/mounted-combat-chunk6-final-qualification`, based on workflow-reset main `6f993c226829acd23418b38dd47845da1a924652`. Documentation commits after the reader revision are separate provenance. The clean frozen product worktree is `C:\Dev\KingmakerMountedCombatLab\worktrees\qualification211`. Product and compiled observer/fixture bytes were not rebuilt or changed. Existing dirty `chunk6de` and `reader209` worktrees were preserved.

The bounded execution repair added explicit frozen-source admission while retaining exact source, package, suite and reader guards. It also corrected a CRLF-sensitive source check and WhatIf admission ordering. Focused safety and entry checks passed before live use; [the preparation record](CHUNK6-FINAL-QUALIFICATION.md) retains those checks and failed probes. The changed launcher received its own full WhatIf proof: `purity211-resume2-process.json`, observed exit 0 in 129.4 minutes. The original proof and failed first entry attempt remain preserved.

## Observed qualification

Lab paths below are relative to `C:\Dev\KingmakerMountedCombatLab`. Let `P` be `analysis-cache\chunk6-continuation\20261005-6br` and `M` be `analysis-cache\chunk5-persistence`.

| Gate | Observed result | Receipt / artifact |
|---|---|---|
| Campaign coverage | 109/109 qualified PASS: A24 + B1 + C84 | `P\campaign211-coverage-execution2.json`; settled plan `P\final-plan211-merged-abc.json` |
| Campaign closures | A: 24 PASS / 1 retained FAIL; B: 1 PASS / 1 retained FAIL; C: 84 PASS | `P\final211-execution2-closure.json`, `P\final211b-execution2-closure.json`, `P\final211c-execution2-closure.json` |
| Chunk 5 matrix | 105/105 PASS: 100 new transactions and 5 exact campaign reuses, no new failures | `M\final211-execution2-matrix-summary.json`; `P\final211matrix-execution2-closure.json` |
| Chunk 5 completion | PASS: all 105 mandatory behaviors; record checks 106/106; nonmandatory P08-tb remains NOTRUN | `P\final211-chunk5-completion1-process.json`, observed exit 0 |
| Chunk 6A completion | PASS: all 87 mandatory behaviors; record checks 87/87; 40 historical failures retained | `P\final211-chunk6-completion1-process.json`, observed exit 0 in 24.4 minutes including generation |
| Final FULL | PASS, aggregate 1/0; components 720/0; released DLL and ledger bytes unchanged | `P\final211-full2-process.json`, observed exit 0 in 32.97 minutes |

The matrix controller exited 0 at `2026-10-12T01:09:12.2592534Z` after 168.4 minutes. The C campaign remainder controller exited 0 after 175.8 minutes. Original A aggregate controller receipts remain absent; reconciliation binds genuine individual terminal receipts and does not invent a historical controller exit.

Final ledger artifacts: Chunk 5 SHA-256 `fb6ff4fc4e89021dde9ba65fcb5acd66b68727d3852640b35fb99a5d4a2e004c`; Chunk 6A SHA-256 `8b78a0c8309e90a635eed8f9202b09fdfc3d1aa80f2c39c26fcd8b9996006bf0`. Both describe the same released product. The 6A composite gates bind the actual completed Chunk 5 ledger. Historical preview.105, preview.205 and preview.209 ledgers retain their own identities; preview.209 PASS is not used as preview.211 execution credit.

Coverage SHA-256: `df870dd877604f454b948a8dce680eb21ff7a6c617af713332e2558e87e0a34d`; merged plan: `258bf88e2126ce4a07dc0e488df53eb3bf15ede83ba7d7f8482a94df56dcc061`; C closure: `f89c57d030eaa7e4741f1af6733355552ceeb8300096cbc82b6c2ce7365c07b3`; matrix closure: `c258695e46352c79ec704e33c9dc0959e6ebc9d332fbbc7abf9ede70c90a12a9`.

## Supported scope and explicit deferrals

- Legal combat Mount/Dismount in RT and TB, the rider's single native Move, paired participation, forced cleanup and the agreed save/cold-load/area/disable/removal contracts.
- Pair-owned Mounted Charge in RT, default-off: 29 distinct qualified rows across three cohorts. TB delivery remains **DEFER — EVIDENCED**; its exact cost-free refusal passed. Refusal is not delivery.
- Ordinary casting and item use: 15/15 rows in each mounted/unmounted RT/TB baseline.
- Staged actions: 5/5 rows each in RT/TB, including move-cast-move and double-move ranged action under the substantive same-activation rules. Native action costs, preparation and turns remain authoritative.
- Defensive Mounted Combat feat: **FEASIBILITY COMPLETE — DEFENSIVE FEAT DEFERRED; NOT IMPLEMENTED**. The bounded 4/4 feasibility rows in each mode do not implement a reaction, negated attack or Ride check.

`P\final211-feature-reconciliation1.json` (SHA-256 `bb7310b2ad19a3ccde073a4414fc9128ca4b26de73b3e1d871761450206e0f56`) binds these matrices and 31 cross-feature/persistence/lifecycle runs. `c6-final211-c-4-sustained-ranged-rt` is now qualified campaign evidence (native 65/0); `dev211-4-sustained-ranged-rt` remains separate development evidence.

## Retained failures and restoration

Original A stage 25 remains FAIL: worker 1, recorded game -1, missing atomic game result and artifact-manifest error. Restoration did not qualify it, and the owner's stop is not established as the cause. Fresh B stage 1 passed with worker/game exits 0 and exact restoration.

B stage 2 (`c6-final211-b-6a-combat-mount-tb`) remains FAIL, native 50/2, worker 1/game 0, restored. Its trace showed rider/target reciprocal engagement flags dropping at initial pair motion while target identity/position and mount flags persisted. The recorded diagnosis matched the known fixture signature; it did not establish a new product defect. One fresh C successor passed 53/0. No repeated retry loop or gameplay correction was used. The 40 older retained failures in the 6A ledger and both current campaign failures in their closures remain immutable.

First final FULL exited 1 after 31.98 minutes: the historical area-reader test still treated preview.54 clean-Dismount as current area-restoration proof. The existing contract had deliberately moved that claim to P07 save/area reload in `e7c2802`. Two unmodified tests reproduced the mismatch once. Commit `c43f32f` repairs only those tests: the legacy archive still has reader coverage and is explicitly refused as current qualification; the ledger regression uses the accepted P07 binding. Focused results: area reader 24/0, area rows 18/0, area ledger 8/0, primary contracts 10014/0, actual exit 0 in 0.82 minutes. Diagnosis through focused completion took 6.38 minutes. `P\final211-area-baseline1.json`, `P\final211-area-focused1-process.json` and `P\final211-area-repair-completion1.json` retain the observations. Acceptance readers, native fixtures, product and completed ledgers are unchanged. The first FULL failure remains retained; the corrected final run has its own receipt.

Known historical achievement-cache changes, P03 condition timeout, orchestration read race and death-restoration timing remain documented in the [release-time handoff](CODEX-HANDOFF-2026-10-11.md); they are not newly fixed or evidence that any similar future failure may be ignored.

All actual campaign and matrix transactions recorded successful restoration, including the failed runs. This is receipt-backed restoration of actual per-transaction installation, Mods, protected saves, baseline/working fixtures and save allowlists under the existing cache policy; it is not a claim of a new whole-filesystem inventory. Final native work left no game, executor, active transaction lock or live Mods sentinel. The human preview.105 installation was preserved. No protected human save was written. Old unrelated log followers were left untouched.

## Owner acceptance and publication boundary

Outstanding owner checklist:

- Review this closeout and the retained failures and limitations.
- Separately authorize any preview.211 human session or installation.
- Perform HUMAN PLAY, visual/HUD and physical-input acceptance for RT/TB Mount/Dismount, RT Charge, casting/items and staged actions, then accept or reject the named scope. Automation cannot supply this acceptance.
- Decide separately whether to merge the review branch; no merge or PR publication is authorized by this mission.

Closeout branch: `codex/mounted-combat-chunk6-final-qualification`. The exact final documentation commit, guarded push result and remote equality are recorded in lab `P\final211-publication-checkpoint.json` after publication. This mission makes no additional main merge, PR publication, tag/release change or permanent installation. The qualified binary is the existing released preview.211; no successor binary or replacement release asset is needed for this qualification.

Final offline command: `scripts/Test-Tiers.ps1 -Tier Full -SkipBuild -AssemblyTarget Both -Configuration Release`. No unchanged product rebuild was performed. Completion receipts: Chunk 5 SHA-256 `950afb1f193ce7ba02d1b3844b3eb7baab621c6f9caa381e2902fbc996789a1a`; Chunk 6A `c4990614ee93cbed5d23142361e916a9c12b5e6f47aefa80e91731a041459b84`; final FULL `05fe9b7d028d220e6096612f2d1363355b433aaa8cb3f596ff1ff8a77b06ba6f`. The exact stdout/stderr hashes and OS exit observations are in those receipts.

Final bounded state check: 2026-10-12T02:52:46.9641500Z, `P\final211-operational-checkpoint.json`, SHA-256 `f7fad9670d08142cc9d1666f0a72f3b363a1f23fa54f7ab81ba28857bf946ff8`. No game, matching runtime executor, active lock or live sentinel remained; 211 actual transactions have restoration receipts. Installed preview.105 DLL SHA-256 `8e231c388540cee50087ae47a2843bff06c69b6bf668b4a35f0ddfc3844f61a2` was independently checked. Human acceptance of preview.211 requires a separately authorized owner session/installation; this mission did not install it permanently.
