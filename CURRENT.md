# F2 — PARTIAL, ready for review (2026-11-05)

Branch codex/kmc-finalize-f2-canonical-controls from main 66e96f90 (F1 PR #16 merged). [Handoff](docs/FINALIZE-F2-REVIEW-HANDOFF.md), [identity table](docs/FINALIZE-F2-CANONICAL-CONTROLS.md).

One persistent Mount / Dismount control, the ordinary Charge mounted and unmounted, one-time placement and old-slot repair, new icon. Native runs r01–r03 at their own identities (all FAIL, retained; restoration exact). r03 (F2c) showed every behaviour except the toolbar repaint after an automatic placement; corrected candidate F2d (source 7879e463, DLL 70b55746) fixes it and has not run. Save → cold-load round trip not run natively; a draft case is on local branch codex/kmc-finalize-f2-persistence-draft.

finalize-f2: 20.97/60 min, 3/3 starts. No further start without a new owner decision. No game, lock or installed candidate. No merge, release, install or F3.

F1 record: [F1 handoff](docs/FINALIZE-F1-REVIEW-HANDOFF.md).
