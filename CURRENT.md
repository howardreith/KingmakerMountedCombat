# F5 IN PROGRESS — companion names and Riding Dog tack (2026-11-06)

Branch codex/kmc-finalize-f5-names-dog-tack, worktree worktrees/finalize-f5, from main 3f19e9bd (the F4 landing). Requirements PET-01 and PET-02. Record finalize-f5: 60 min, at most 3 starts. No merge, release, install or F6.

**F4 closeout:** F4 ACCEPTED FOR INTEGRATION WITH WORLD-02 NATIVE VALIDATION DEFERRED; OWNER ACCEPTANCE SEPARATE. The owner merged PR #19 at the reviewed head 46f76f7e as 3f19e9bd. WORLD-02 is deferred, not passed: see [docs/FINALIZATION-DEFERRED-ITEMS.md](docs/FINALIZATION-DEFERRED-ITEMS.md) and the F4 handoff's integration closeout.

# F4 record at review, kept: ENGINEERING PARTIAL — WORLD-02 INPUT BLOCKED ONLY; OWNER ACCEPTANCE SEPARATE (2026-11-06)

Branch codex/kmc-finalize-f4-orders-ai-transitions, from main a92b043a. Read the [handoff](docs/FINALIZE-F4-REVIEW-HANDOFF.md), current section first.

**Entrance → Auto → reload chain, complete on one binary:**
- **Binary:** DLL 3240963f…7f3d, MVID cc86d1d6-502e-4991-8805-618755a9ae47.
- **Producer:** r08 `20261106-finalize-f4f-dungeon-08` PASS (package f4f 4efc6665…). It wrote `Auto_1.zks` with SHA-256 b8e4109ea5bd9deae8e8e77b7b0318e1afa0d42895071d1f3a52a29c284540f5, 395809 bytes: native Auto, campaign "cvb", on the floor.
- **Reload:** r10 `20261106-finalize-f4g-dungeon-reload-10` PASS. Package f4g e1f195c1… was built from the records head, docs-only delta, with the identical DLL.
  - It cold-loaded that exact Auto on its floor and campaign, with the same rider and mount, unique bound views and exact restore counts.
  - No inherited transfer state.
  - Then an ordinary move and stock attack (a native miss).
  - 0 writes, and the source is unchanged.

**Earlier results, at their own binaries:** the Hold control (r04 rows), and the chest-during-travel and gait results (r05).

**Sole outstanding item: WORLD-02 trap, INPUT BLOCKED.** It needs an owner Manual save beside a revealed, reachable trap.

Verdicts:
- r01–r04, r06, r07 and start 9 (pre-staging refusal, my detached-HEAD error): FAIL, as recorded.
- r05, r08 and r10: PASS.

Spend: finalize-f4 68.17/80 min, 10 of 10 starts. All adjustments are append-only. No game, lock or waiter remains. No merge, release, install or F5.

F3 closeout: the F3 review was accepted at PR #18 head 5dd64825; the owner merged it as a92b043a.
