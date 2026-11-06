# F4 — ENGINEERING PARTIAL after the final reload execution mission, ready for final review (2026-11-06)

Branch codex/kmc-finalize-f4-orders-ai-transitions, from main a92b043a. Read the [handoff](docs/FINALIZE-F4-REVIEW-HANDOFF.md), current section first.

Candidate f4f:
- source 3f824e30;
- DLL 3240963f…7f3d, MVID cc86d1d6-502e-4991-8805-618755a9ae47;
- package 4efc6665…245e;
- suite finalize-f4f-3f824e30-20261106 (f6513447…).

The engine is Kingmaker 2.1.7b with the retained CotW/CMI selection.

**Accepted:**
- **r08 dungeon entrance PASS.** Its after-entry Auto is `b8e4109ea5bd9deae8e8e77b7b0318e1afa0d42895071d1f3a52a29c284540f5`, 395809 bytes.
- **Earlier results, at their own binaries:** the Hold control (r04 rows), and the chest and gait results (r05).

**Not run:**
- **The native reload.** It was prepared, and the exact request was validated (PowerShell validator plus compiled f4f `Validate`, declaring one read-only Auto).
- **Start 9 was consumed by a pre-staging refusal** that I caused by detaching the worktree. The launcher's manifest check needs `git branch --show-current`, and it threw on the detached HEAD. No game ran, and there was nothing to restore. A tenth start is refused.

**Input blocked:**
- **WORLD-02 trap.** It needs an owner Manual save beside a revealed, reachable trap.

Verdicts:
- r01–r04, r06, r07 and the start-9 refusal: FAIL, as recorded.
- r05 and r08: PASS.

Spend: finalize-f4 64.7/80 min, 9 of 9 starts. Both adjustments are append-only. No game, lock or waiter remains. No merge, release, install or F5.

F3 closeout: the F3 review was accepted at PR #18 head 5dd64825; the owner merged it as a92b043a.
