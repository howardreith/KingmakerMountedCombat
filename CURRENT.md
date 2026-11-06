# F3 — ENGINEERING PARTIAL, ready for review (2026-11-06)

Branch codex/kmc-finalize-f3-movement-economy-hud from main 81702786 (F2 PR #17 merged by the owner). [Handoff](docs/FINALIZE-F3-REVIEW-HANDOFF.md), [rules and Wrath comparison](docs/FINALIZE-F3-RULES-COMPARISON.md).

What F3 delivers:
- Mounted movement preview and execution both read the mount's own movement, whichever pair actor is selected.
- After a delivered Charge the pair has no further travel or step; the mount keeps its Standard attack.
- The initiative strip hides the paired mount while native scheduling skips it.
- The pair's destination marker uses the selected (bright) style.
- Refusals name the actor whose Standard is spent.

Native evidence (final compiled source fc62e9b7, DLL e7701bc2…):
- **TB r03** recorded all seven rows; under reader r2 every F3 assertion holds on them. The run stays **FAIL**: after the rows, the tranche cleanup timed out with the game in EscMode.
- **RT r04** PASS.
- **Earlier runs:** r01 and r02 are FAIL at their original verdicts and are retained.
- **Restoration:** exact on every run.

Open items: no TB run has a PASS verdict, and the ECON-02 full-round rider case is native-unverified.

Spend: finalize-f3 40.00/90 min, 4 of 4 starts. No game, lock or installed candidate remains. Owner acceptance separate. No merge, release, install or F4.

F2 record: [F2 handoff](docs/FINALIZE-F2-REVIEW-HANDOFF.md).
