# Sprint 5: Small riders, relative size and the Riding Dog (2026-11-02)

**Engineering complete for the default-build Halfling and Gnome rigs, ready for review. Not the full Sprint 5 completion label:**
the fat and thin build presets are refused with feedback and unsupported. Owner play acceptance is separate. No merge, release or
installation is selected.

Branch `codex/mounted-combat-sprint5-small-riders-riding-dog` from `main` `e8450a56`. This corrects the merged Sprint 4 record:
PR #9 is merged, and its "not merged" line predates that. Final compiled candidate sprint5o (`992fe68e`; DLL `ad3ee75e…`, MVID
`0bad1c69…`). Later commits change docs only. Budget `sprint5`: 81.74 of 90 minutes, no adjustment.

- **POSE-01:** the Halfling sank into the Horse because its seat was calculated to reach the stirrups. Small seats are now fixed from
  the mount's measured back, and gear no longer affects admission. Four rigs on two mounts, with images inspected.
- **SIZE-01:** the mount must be at least one effective native size larger, on a supported anatomy. A Medium rider is refused on the
  dog; a native Enlarge detaches the pair and keeps ownership.
- **DOG-01:** an additive Riding Dog, chosen through the native Ranger selection in an existing campaign and Medium from rank 1, with
  rank 4 advancement. Rider and dog attack on their own actions. A native save survives a separate-process cold load with
  working controls.
- **Owner decision:** extend the budget for the fat and thin presets (about 15 validation minutes), or accept the refusal for now.

Record: [docs/SPRINT5-SMALL-RIDERS-RIDING-DOG.md](docs/SPRINT5-SMALL-RIDERS-RIDING-DOG.md). Prior state:
[CURRENT before Sprint 5](docs/history/CURRENT-before-sprint5-2026-11-02.md).

Live installation at closeout: Mods tree empty, the same as each run's captured start; protected saves restored; no transaction lock
or game process; no candidate installed. Owner play acceptance of Sprints 1 to 5 and the Sprint 3 trap check remain separate.
