# Sprint 8: Paladin Divine Mount (2026-11-04)

**SPRINT 8 ENGINEERING COMPLETE - READY FOR FINAL REVIEW; OWNER PLAY ACCEPTANCE SEPARATE.** No merge, release or installation is selected.

Branch `codex/mounted-combat-sprint8-paladin-mount`, PR #13, from `main` `6a3f2e88`. Final compiled candidate sprint8m (`3a8f0c66`; DLL `a094ecb2...`, MVID `ba06a57e...`). Later commits change docs only. Budget `sprint8`: 103.42 of 115 minutes, 33 launches.

- **PAL-01:** at Paladin 5 the native level-up holds for a choice of Weapon Bond or Divine Mount. Weapon Bond is unchanged and creates no animal; the mount path holds no weapon-bond fact, resource or upgrade.
- **Progression:** paladin level is the effective druid level. Actual pet levels are native commits. Call of the mount, Intelligence 6, celestial defenses and smite unlocked at 11, spell resistance at 15.
- **Review corrections:** a smite mark belongs to the mount that placed it, with one current target per mount. Celestial defenses follow the pet's actual Hit Dice, exactly one tier at a time.
- **Bodies:** Medium takes the Horse; Small takes the Horse or the Riding Dog.
- **Existing characters** are not rewritten; the native respec changes the choice both ways. Another class's companion stays the one companion and levels stack. Archetypes replacing the bond stay legal.
- **UI-01 is deferred.**

Stated for review: the CRPG adaptations, limits and unexecuted list in the report, including that clean uninstallation has not been demonstrated. Evidence stands on its own binaries: t01 to t04 on sprint8m, q01 to q05 on sprint8l, a04 and a10 on sprint8k.

Record: [docs/SPRINT8-PALADIN-MOUNT.md](docs/SPRINT8-PALADIN-MOUNT.md). Prior state: [CURRENT before Sprint 8](docs/history/CURRENT-before-sprint8-2026-11-04.md).

Live installation at closeout: Mods tree empty, the same as each run's captured start; protected saves restored; no transaction or game process; no candidate installed. Owner play acceptance of Sprints 1 to 8 and the Sprint 3 trap check remain separate.
