# Sprint 7: usable Mounted Charge (2026-11-03)

**SPRINT 7 ENGINEERING COMPLETE - READY FOR REVIEW; OWNER PLAY ACCEPTANCE SEPARATE.** No merge, release or installation is selected.

Branch `codex/mounted-combat-sprint7-charge` from `main` `4efd45a0`. Final compiled candidate sprint7j (`5f7c214b`; DLL `b1cfd8d4...`, MVID `6af2075f...`). Later commits change docs only. The accepted product runs a01 to a05 stay on sprint7i (`31f48c19`; DLL `cc7ffb1e...`); sprint7j adds a diagnostic control row, a06. Budget `sprint7`: 63.88 of 90 minutes, 12 launches.

- **CHARGE-01:** Mounted Charge is on by default in real time and turn-based combat, through the rider's ordinary ability and the native target handler.
- One forced travel on the mount; one native rider attack with the native modifiers. The rider pays the native full round. In turn-based combat the mount pays its travel from its own Move and keeps its Standard.
- Charge replaces the pair's ordinary attack or move. The native brain waits for a running charge. A delivered charge leaves the native buff to its round; a cancelled one removes it.
- A committed charge survives a cold load with no refund and no duplicate delivery.

Stated for review: the adaptations and the unexecuted list in the report.

Record: [docs/SPRINT7-MOUNTED-CHARGE.md](docs/SPRINT7-MOUNTED-CHARGE.md). Prior state: [CURRENT before Sprint 7](docs/history/CURRENT-before-sprint7-2026-11-03.md).

Live installation at closeout: Mods tree empty, the same as each run's captured start; protected saves restored; no transaction lock or game process; no candidate installed. Owner play acceptance of Sprints 1 to 7 and the Sprint 3 trap check remain separate.