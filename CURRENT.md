# F2 — PARTIAL, ready for review (2026-11-05)

Branch codex/kmc-finalize-f2-canonical-controls from main 66e96f90 (F1 PR #16 merged). [Handoff](docs/FINALIZE-F2-REVIEW-HANDOFF.md), [identity table](docs/FINALIZE-F2-CANONICAL-CONTROLS.md).

One persistent Mount / Dismount control, the ordinary Charge mounted and unmounted, one-time placement and old-slot repair, new icon. Finishing round on the UPG-01 chain with the authentic preview.211 Quick:
- r05 (F2f `ff1f2e5a`) PASS natively: the toolbar repaint after the first placement, the old Dismount row migrated in place, Dismount → Mount through the slot, and two native Manuals with exactly the kept {6@9, 7@0} and removed {7@0} rows.
- r06, the separate-process reload, refused itself at the isolation bootstrap before any load. The harness gap is fixed at `8b610af1` and has not run. The reload half is NOT RUN.
- r01–r04 and r06 are FAIL at their original verdicts, retained. Restoration was exact on every run.

finalize-f2: 37.22/60 min, 6/6 starts. Further starts need a new owner decision. No game, lock or installed candidate. No merge, release, install or F3.

F1 record: [F1 handoff](docs/FINALIZE-F1-REVIEW-HANDOFF.md).
