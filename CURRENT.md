# F5 ENGINEERING COMPLETE — READY FOR FINAL REVIEW; OWNER ACCEPTANCE SEPARATE (2026-11-07, producer/consumer completion mission)

Branch codex/kmc-finalize-f5-names-dog-tack. Frozen and launched at d87b148e (compiled source fd8bb820; DLL `8459126c…`, MVID e90f71ee…, byte-identical to r06). Base main 3f19e9bd. Read the [handoff](docs/FINALIZE-F5-REVIEW-HANDOFF.md), top section.

**Status:**
- **Producer r07** `20261107-finalize-f5h-producer-07`: PASS.
  - Every naming row passes, including the duplicate Unicode `Ébène l'Ancien`.
  - The corrected saddle is coherent, with clearance −0.0036…0.0508 and 0 unsupported.
  - Native Manual `d39f8b46…`, 858008 bytes. Restored, digest `60e3d6cc…`.
- **Cold consumer r08** `20261107-finalize-f5h-consumer-08`: PASS, in a fresh process loading r07's exact Manual.
  - The saved IDs, masters, blueprints and both names are intact, with zero naming-service writes.
  - One saddle was re-fitted in the new process and survived the remount. Dog statistics are unchanged.
  - No save was written, and restoration is complete.
  - The cross-run reader `Test-Finalize5ColdAgainstSource.ps1` passes.
- **r06:** ORIGINAL OUTER FAIL / NATIVE GAME PASS, kept. It was not used as a source.

**Spend:** 74.9 of 90 minutes, 8 of 8 starts, on the same finalize-f5 record (ceiling 7 → 8 append-only). A ninth start is refused.

**Next:** final review. Owner art and full-stack acceptance are separate. WORLD-02 stays deferred; F6 is not started; no merge, release or install.

# Superseded F5 state after the saddle-diagnosis mission (2026-11-07)

Branch codex/kmc-finalize-f5-names-dog-tack. Product at fd8bb820 (r06); harness reader fix at d6c88978; records commit after it. Base main 3f19e9bd. Read the [handoff](docs/FINALIZE-F5-REVIEW-HANDOFF.md), top section.

**Status:**
- **Saddle (PET-02):** the cause is proven by an offline replay of a private real-dog specimen (r05). The first failing stage was sampling: the back reference fell on chest-underside vertices where the dog's back has no nearby vertex. Conversion and skinning are exact.
  - Corrected: the back is sampled on the surface, the girth sits behind the elbows, and the fit measures per-vertex clearance, which the reader requires.
  - r06 shows one coherent pad, seat and girth, unmounted, mounted and walking, with numbers identical to the replay.
- **Naming (PET-01):** every producer row passes game-side again in r06 (Manual `c673673c…`, 376897 bytes).
- **Cold consumer:** not run. r06's recorded verdict is FAIL because the harness read the game's UTF-8 evidence as ANSI and mis-compared `Ébène`. A UTF-8 re-read passes its whole reader chain. A FAIL is no source.

**Spend:** 63.97 of 90 minutes, 6 of 7 starts, on the same finalize-f5 record. An eighth start is refused.

**Next:** owner decision. Recommended: start ceiling 7 → 8 (no time increase), then a Candidate at d6c88978, one producer and its consumer. WORLD-02 stays deferred; F6 is not started; no merge, release or install.

# Superseded F5 state after the completion mission (2026-11-07)

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
