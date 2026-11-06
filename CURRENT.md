# F4 — ENGINEERING PARTIAL after the entrance/reload finishing mission, ready for final review (2026-11-06)

Branch codex/kmc-finalize-f4-orders-ai-transitions, from main a92b043a. Read the [handoff](docs/FINALIZE-F4-REVIEW-HANDOFF.md), current section first.

Current candidate f4f:
- source 3f824e30;
- DLL 3240963f…7f3d, MVID cc86d1d6-502e-4991-8805-618755a9ae47;
- package 4efc6665…245e;
- suite finalize-f4f-3f824e30-20261106.

The engine is Kingmaker 2.1.7b with the retained CotW/CMI selection.

**Accepted:**
- **Real dungeon entrance: r08 `20261106-finalize-f4f-dungeon-08` PASS.** The setup transfer reached the hub with no write. The native entrance action and party `Go` loaded the floor with the same world, party and mounted pair (generation 1). The bound views stayed the same and the ordinary continuation ran.
- **Its after-entry Auto:** `Auto_1.zks` with SHA-256 `b8e4109ea5bd9deae8e8e77b7b0318e1afa0d42895071d1f3a52a29c284540f5`, 395809 bytes. It was admitted in the hub and committed on the floor, with the pair.
- **Earlier results, at their own binaries:** the Hold control (r04 rows), and the chest and gait results (r05).

**Not run:**
- **The native reload of that Auto** (`persistence-p07-load` / `area-dungeon-entry-auto`). It is built, reviewed and WhatIf-validated against r08's real receipt. All 8 starts are used; r07 FAILed on a schema-3 read-back defect that is now corrected.

**Input blocked:**
- **WORLD-02 trap.** It needs an owner Manual save beside a revealed, reachable trap.

Verdicts:
- r01–r04, r06 and r07: FAIL, as recorded.
- r05 and r08: PASS.

Spend: finalize-f4 64.47/75 min, 8 of 8 starts. No game, lock or waiter remains. No merge, release, install or F5.

F3 closeout: the F3 review was accepted at PR #18 head 5dd64825; the owner merged it as a92b043a.
