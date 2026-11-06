# F3 — ENGINEERING COMPLETE, ready for final review (2026-11-06)

Branch codex/kmc-finalize-f3-movement-economy-hud, from main 81702786 (F2 PR #17, merged by the owner). Read the [handoff](docs/FINALIZE-F3-REVIEW-HANDOFF.md) and the [rules and Wrath comparison](docs/FINALIZE-F3-RULES-COMPARISON.md).

What F3 delivers:
- **Movement.** The mounted movement preview, the movement-facing prediction-panel slots and execution all read the mount's own movement, whichever pair actor is selected. Rider-only actions (Swift, attacks, spells) stay the rider's.
- **Full attack.** A rider full attack spends only the rider. The unmoved mount keeps its legal movement in the same activation.
- **Charge.** After a delivered Charge, the pair has no further travel or step, and the mount keeps its Standard attack.
- **Initiative and HUD:**
  - the initiative strip hides the paired mount while native scheduling skips it;
  - the pair's destination marker uses the selected style;
  - refusals name the actor.

Native evidence, on the final compiled source ef5dd11b (candidate finalize-f3d, DLL 4a88036b…, MVID d441e6ef…):
- **TB r05 PASS** with external reader r3, a native teardown and exact restoration. In it, a genuine native full attack (4 planned and 4 delivered strikes; rider 6/3; mount unmoved at 0/0) is followed by a 6.10 m mount move in the same activation. Truthful panel observations were recorded after the first move, the full attack, the mount move, the step, the Charge and the double move.
- **RT r04 PASS** is retained at its own bytes.
- **Earlier TB runs:** r01, r02 and r03 keep their FAIL verdicts. r03's rows count only at single-attack scope.

Spend: finalize-f3 51.47/90 min, 5 of 6 starts. No game, lock or installed candidate remains. Owner acceptance is separate. No merge, release, install or F4.

F2 record: [F2 handoff](docs/FINALIZE-F2-REVIEW-HANDOFF.md).
