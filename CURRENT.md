# F2 — ENGINEERING COMPLETE, ready for final review (2026-11-05)

Branch codex/kmc-finalize-f2-canonical-controls from main 66e96f90 (F1 PR #16 merged), PR #17. [Handoff](docs/FINALIZE-F2-REVIEW-HANDOFF.md), [identity table](docs/FINALIZE-F2-CANONICAL-CONTROLS.md).

What F2 delivers: one persistent Mount / Dismount control, the ordinary Charge mounted and unmounted, one-time placement with remembered removal, old-slot migration, and a new icon.

Native evidence:
- **Producer** r05 (F2f `ff1f2e5a`) PASS, on the authentic preview.211 Quick:
  - the toolbar repaint after placement;
  - the old Dismount row migrated in place;
  - the slot cycle;
  - kept {6@9, 7@0} and removed {7@0} Manuals.
- **Consumer** r07 (F2g `ffb5c328`, through the one declared r05 bridge) PASS:
  - a new process restored the control at slot 9, drawn and usable;
  - loading the removed archive kept it absent through updates, reselection and a control cycle.
- **Earlier runs:** r01–r04 and r06 are FAIL at their original verdicts and are retained.
- **Restoration:** exact on every run.

Spend: finalize-f2 44.87/60 min, 7 of 8 starts. No game, lock or installed candidate remains. Owner art approval and laptop-stack acceptance are separate. No merge, release, install or F3.

F1 record: [F1 handoff](docs/FINALIZE-F1-REVIEW-HANDOFF.md).
