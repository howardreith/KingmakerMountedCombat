# F5 ENGINEERING PARTIAL — CORRECTED SADDLE STILL MIS-RENDERS; COLD CONSUMER NOT RUN; OWNER ACCEPTANCE SEPARATE (2026-11-07, completion mission)

Branch codex/kmc-finalize-f5-names-dog-tack (head after the records commit; product/harness at d32fdc28), from main 3f19e9bd. Read the [handoff](docs/FINALIZE-F5-REVIEW-HANDOFF.md), top section.

**Status:**
- **Naming (PET-01):** demonstrated on the producer side in four runs. r04 also covers the ordinary overtip label on both companions.
- **Saddle (PET-02):** single, adopted, mechanically neutral. The fit along the centre line is now on design, but r04's reader and frames both show the pad rendering as shards and a flank strip, not a drape. The cause is not diagnosed.
- **Cold consumer:** not run. A FAIL producer is no source.

**Start 4:** `20261106-finalize-f5d-producer-04` (f5d, DLL 119e7a98…, MVID 31f3c7c9…). Game side PASS and the Manual was written (3e70c359…). Reader FAIL: a pad/seat point 0.544 m above the back. Restoration complete.

**Spend:** finalize-f5 is at 42.52/60 min, 4 of 6 starts. This includes 430 s of F5 Fast tiers cross-referenced from the development record. Starts 5 and 6 are unused: with 17.48 min left, a corrected candidate plus two 10-minute admissions does not fit.

**Next (owner):** diagnose the saddle render, likely from per-vertex fit data of the real dog, then decide on time and starts for one producer and one consumer. No game, lock or waiter remains. No merge, release, install or F6.

**F4:** accepted for integration with WORLD-02 deferred, not passed ([deferred items](docs/FINALIZATION-DEFERRED-ITEMS.md)).

# Superseded F5 state at review (2026-11-07)

Branch codex/kmc-finalize-f5-names-dog-tack, worktree worktrees/finalize-f5, from main 3f19e9bd (the F4 landing). Requirements PET-01 and PET-02. Read the [handoff](docs/FINALIZE-F5-REVIEW-HANDOFF.md).

**Status:**
- **Naming (PET-01):** fully exercised in the game on the producer side through the real sheet button and native dialog, in all three runs.
- **Tack (PET-02):** attached, adopted, single and mechanically neutral in every run. Its fitted shape failed the reader in r03 and has been corrected offline only.
- **Cold consumer:** not run.

**Runs (all FAIL, verdicts kept, causes corrected; restoration complete each time):**
- r01 (f5a): a pre-F2 slot assertion.
- r02 (f5b): F2's control migration of a stale harness Dismount binding.
- r03 (f5c): game side PASS on every row and the Manual written; the reader refused the tack shape.

**Next:** two more starts on this record (producer + consumer) from the branch head, which needs owner authorization. Spend: finalize-f5 26.83/60 min, 3 of 3 starts. No game, lock or waiter remains. No merge, release, install or F6.

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
