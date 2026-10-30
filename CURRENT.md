# Sprint 1 — saving reliability: PARTIAL, READY FOR FINAL REVIEW (2026-10-30, iteration 4)

The owner's playtest finding (Quick and Auto saves fail with KMC enabled, Manual works)
is reproduced and repaired at source: the native replacement commit refused the engine's
own mixed-separator archive paths (Unity's forward-slash persistent data path joined with
backslashes), which only overwrites reach. Manual saves with new names never reach that
branch. The lab's isolated root was canonical, which is why every lab rotation row passed.
The reproduction on the frozen preview.96 DLL and the verification on the candidate use
the real engine types offline (`scripts/Test-NativeArchiveCommitPaths.ps1`); the path
guard also refuses drive-relative and root-relative spellings.

Review branch `codex/mounted-combat-sprint1-save-reliability`
([draft PR #6](https://github.com/howardreith/KingmakerMountedCombat/pull/6)), candidate
`0.1.0-chunk7b-preview.98`, DLL `bed87d75…`, MVID `846f1c80…` (built at `f4294828`;
package `…preview.98-sprint1l-diagnostic.zip` `3eec930a…`, suite
`sprint1l-f4294828-20261030` `97bf6e99…`). Candidate tier 12/0 on the packaged bytes.
Shared budget `sprint1`: 100.98 of 135 min (one-time +45 min adjustment recorded on the
record with its authority).

Native, on this candidate with the isolated root spelled as the player's
(`-EngineShapedSaveRoot`): the Quick overwrite through the engine's QuickSave handler body
(iteration 3); the Manual overwrite control (iteration 3); and now **SAVE-02's genuine
Auto overwrite**: an admitted, verified pre-existing `Auto_1` of the same campaign/area
(the accepted first-ever producer's own archive) is selected by the engine's
`GetNextAutoslot()` under capacity 1 and overwritten by the one genuine
`Game.LoadArea(…, AfterEntry)` transition through the replacement branch, exactly once,
bytes changed at the existing leaf, staging retired, holds settled, both pairs retained;
its separate-process cold load restores both pairs (r11, r12 PASS).

**UPG-01 is classified, not closed.** The genuine preview.211 Quick (and the Aug-14
Working fixture) carry Call of the Wild unit parts on the main character and a companion;
on the base-game-only staging (no Call of the Wild assembly, as in the owner's current
empty Mods tree) those units are dropped during the engine's threaded deserialization
and `Player.PostLoad` fails on its first lookup, before KMC's callback. KMC's blueprints and
schema-2 metadata resolve; KMC touches no entity data before `PostLoad`. The new
`engineLogTail` (UberLogger Warning/Error tail in every runtime game result) retained the
engine's channel for r10. Remaining action: stage the archive's dependency (the owner's
Call of the Wild beside KMC) and run load → resave → cold load; the archive is never edited.

**Owner play/visual acceptance of Chunk 7 remains TODO.** No merge, release or installation
is selected. Live installation restored exactly after every run: Mods tree empty as at
intake, loader parameters and preferences exact, protected saves unchanged, no lock or
game process.

The [sprint report](docs/SPRINT1-SAVE-RELIABILITY-2026-10-29.md) holds identities, the
native batch outcomes, cost, safety and the next checks. Prior Chunk 7 state:
[CURRENT before Sprint 1](docs/history/CURRENT-before-sprint1-2026-10-29.md).
