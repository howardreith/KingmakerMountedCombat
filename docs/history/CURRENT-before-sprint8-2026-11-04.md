# Sprint 7: usable Mounted Charge (2026-11-03)

**SPRINT 7 ENGINEERING COMPLETE - READY FOR FINAL REVIEW; OWNER PLAY ACCEPTANCE SEPARATE.** No merge, release or installation is selected.

Branch `codex/mounted-combat-sprint7-charge` from `main` `4efd45a0`. Final compiled candidate sprint7l (`ca10a71d`; DLL `aa211023...`, MVID `49f21a56...`). Later commits change docs only. Corrective runs q02 and q03 are on sprint7l; a01 to a05 stay on sprint7i and a06 on sprint7j. Budget `sprint7`: 77.75 of 90 minutes, 15 launches.

- **CHARGE-01:** Mounted Charge is on by default in real time and turn-based combat, through the rider's ordinary ability and the native target handler.
- One forced travel on the mount; one native rider attack with the native modifiers. The rider pays the native full round. In turn-based combat the mount pays its travel from its own Move and keeps its Standard.
- Charge replaces the pair's ordinary attack or move. The native brain waits for a running charge. A delivered charge leaves the native buff to its round; a cancelled one removes it.
- A committed charge survives a cold load with no refund and no duplicate delivery.

- A pending or refused request leaves a live charge untouched; only an admitted replacement ends it, once. Every forced line reads the mount's body lane.

Stated for review: the adaptations, the mount-queue limitation and the unexecuted list in the report.

Record: [docs/SPRINT7-MOUNTED-CHARGE.md](docs/SPRINT7-MOUNTED-CHARGE.md). Prior state: [CURRENT before Sprint 7](docs/history/CURRENT-before-sprint7-2026-11-03.md).

Live installation at closeout: Mods tree empty, the same as each run's captured start; protected saves restored; no transaction lock or game process; no candidate installed. Owner play acceptance of Sprints 1 to 7 and the Sprint 3 trap check remain separate.