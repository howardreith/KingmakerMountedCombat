# Sprint 1 — saving reliability: PARTIAL, READY FOR REVIEW (2026-10-29, iteration 3)

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
`0.1.0-chunk7b-preview.98`, DLL `a680dd69…`, MVID `0b40e3b8…` (built at `4644a433`;
packages `…preview.98-sprint1i-diagnostic.zip` `078e841d…` and `…sprint1j…` `5995751b…`
carry the same bytes). Candidate tier 9/0 on the packaged bytes. Workflow enforcement after
two reviews: per-step and block-ceiling gates, verified inheritance with nested native
attempts counted under their parent, re-admission before the lock and before the launch,
restoration suspended (never charged) and completion only at the final verdict, explicit
reconciliation, locked record updates; `Test-TierSelection.ps1` 29/0. Shared budget
`sprint1`: 78.47 of 90 min spent.

Native, on this candidate with the isolated root spelled as the player's
(`-EngineShapedSaveRoot`): the Quick overwrite through the engine's own QuickSave handler
body (`Game.MakeQuickSave`, twice over a full one-slot capacity) lands as native replacement
commits onto `Quick_1.zks` with changed bytes and the staging leaf gone; its separate-process
cold load restores both pairs; the Manual overwrite control passed natively (outer reader
corrected, re-evaluated). Open: the genuine area-transition Auto overwrite is not run (only
the first-ever slot-route Auto exists), and UPG-01 failed in the engine's load of the
preview.211 archive before the load callback (blocker recorded, exception text not retained
on the base-game-only route).

**Owner play/visual acceptance of Chunk 7 remains TODO.** No merge, release or installation
is selected. Live installation restored exactly: Mods tree empty as at intake, loader
parameters and preferences exact, protected saves unchanged, no lock or game process.

The [sprint report](docs/SPRINT1-SAVE-RELIABILITY-2026-10-29.md) holds identities, the
native batch outcomes, cost, safety and the next checks. Prior Chunk 7 state:
[CURRENT before Sprint 1](docs/history/CURRENT-before-sprint1-2026-10-29.md).
