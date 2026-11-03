# Sprint 5: Small riders, relative size and the Riding Dog (2026-11-03)

**SPRINT 5 ENGINEERING COMPLETE — READY FOR FINAL REVIEW; OWNER PLAY ACCEPTANCE SEPARATE.** No merge, release or installation is
selected.

Branch `codex/mounted-combat-sprint5-small-riders-riding-dog` from `main` `e8450a56`. This corrects the merged Sprint 4 record:
PR #9 is merged, and its "not merged" line predates that. Final compiled candidate sprint5v (`fcdce675`; DLL `0aa9bfe5…`, MVID
`e9782372…`). Later commits change docs only. Candidate tier 8/0 on that source. Budget `sprint5`: see the report's Costs; the
allowance is 150 minutes, after two explicit +30 adjustments.

- **POSE-01:** Small seats are fixed from the mount's measured back, and gear no longer affects admission. 12 rigs on two mounts are
  declared, 24 profiles including fat and thin, and all mounted, moved and dismounted. Images inspected on the final DLL.
- **Female boot:** the game's mesh merge stops at a bone the female doll lacks, so the right leg stays rigid on the pelvis, which a seat
  exposes. A seated Small rider's view now gets the missing bone and rebuilds natively. Medium riders are untouched.
- **SIZE-01:** the mount must be at least one effective native size larger, on a supported anatomy. A Medium rider is refused on the
  dog; a native Enlarge detaches the pair and keeps ownership.
- **DOG-01:** an additive Riding Dog, chosen through the native Ranger selection in an existing campaign and Medium from rank 1, with
  rank 4 advancement. The pet levels natively from 1 to 4, once while mounted, with one upgrade and the same identity, owner and seat. A
  native save survives a separate-process cold load (retained, default rigs).

Record: [docs/SPRINT5-SMALL-RIDERS-RIDING-DOG.md](docs/SPRINT5-SMALL-RIDERS-RIDING-DOG.md). Visual packet:
[Sprint 5 Boot Packet](https://claude.ai/artifact/T1vJzEguDxW1PJsLzzRsBg). Prior state:
[CURRENT before Sprint 5](docs/history/CURRENT-before-sprint5-2026-11-02.md).

Live installation at closeout: Mods tree empty, the same as each run's captured start; protected saves restored; no transaction lock
or game process; no candidate installed. Owner play acceptance of Sprints 1 to 5 and the Sprint 3 trap check remain separate.
