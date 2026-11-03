# Sprint 5: Small riders, relative size and the Riding Dog (2026-11-03)

**Not the Sprint 5 completion label. Two selected outcomes are open:**

1. **Female near boot, cause unknown.** All six female rigs lose the boot on the rider's right: cut off at the saddle on the final
   Horse seat (g4), in the fur on the previous Dog seat (g3). Far-side legs and male rigs display correctly.
2. **Final Dog seat never rendered.** The budget guard refused g5 before launch, with 9.62 minutes left against a 10-minute reservation.
   No Dog row, including the pet levels, ran on the final DLL.

Owner play acceptance is separate. No merge, release or installation is selected.

Branch `codex/mounted-combat-sprint5-small-riders-riding-dog` from `main` `e8450a56`. This corrects the merged Sprint 4 record:
PR #9 is merged, and its "not merged" line predates that. Final compiled candidate sprint5s (`877f4b7f`; DLL `011682da…`, MVID
`87950acb…`). Later commits change docs only. Candidate tier 8/0 on that source. Budget `sprint5`: see the report's Costs; the
allowance is 120 minutes, after one explicit +30 adjustment.

- **POSE-01:** Small seats are fixed from the mount's measured back, and gear no longer affects admission. 12 rigs on two mounts are
  declared, 24 profiles including fat and thin, and all mounted, moved and dismounted. Images inspected. A real mirrored rider sits
  correctly on both mounts.
- **SIZE-01:** the mount must be at least one effective native size larger, on a supported anatomy. A Medium rider is refused on the
  dog; a native Enlarge detaches the pair and keeps ownership.
- **DOG-01:** an additive Riding Dog, chosen through the native Ranger selection in an existing campaign and Medium from rank 1, with
  rank 4 advancement. On sprint5r, the pet levelled natively from 1 to 4, once while mounted, with one upgrade and the same identity,
  owner and seat. A native save survives a separate-process cold load (retained, default rigs).
- **Owner decision:** allow one more validation pass (a female Horse diagnostic and a Dog launch, about 15 minutes), or decide from
  play whether the boot matters.

Record: [docs/SPRINT5-SMALL-RIDERS-RIDING-DOG.md](docs/SPRINT5-SMALL-RIDERS-RIDING-DOG.md). Prior state:
[CURRENT before Sprint 5](docs/history/CURRENT-before-sprint5-2026-11-02.md).

Live installation at closeout: Mods tree empty, the same as each run's captured start; protected saves restored; no transaction lock
or game process; no candidate installed. Owner play acceptance of Sprints 1 to 5 and the Sprint 3 trap check remain separate.
