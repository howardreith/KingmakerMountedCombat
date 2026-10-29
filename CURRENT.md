# Sprint 1 — saving reliability: PARTIAL, READY FOR REVIEW (2026-10-29, iteration 2)

The owner's playtest finding (Quick and Auto saves fail with KMC enabled, Manual works)
is reproduced and repaired at source: the native replacement commit refused the engine's
own mixed-separator archive paths (Unity's forward-slash persistent data path joined with
backslashes), which only overwrites reach. Manual saves with new names never reach that
branch. The lab's isolated root was canonical, which is why every lab rotation row passed.
The reproduction on the frozen preview.96 DLL and the verification on the candidate use
the real engine types offline (`scripts/Test-NativeArchiveCommitPaths.ps1`); the path
guard now also refuses drive-relative and root-relative spellings.

Review branch `codex/mounted-combat-sprint1-save-reliability`
([draft PR #6](https://github.com/howardreith/KingmakerMountedCombat/pull/6)), candidate
`0.1.0-chunk7b-preview.98` (commit `aa08ba1a`, DLL `4c085da3…`, package
`…preview.98-sprint1h-aa08ba1a-diagnostic.zip` `207d635d…`). Candidate tier 8/0 on the
packaged bytes. Workflow enforcement after review: per-step and block-ceiling gates,
verified inheritance, accounting from the first expensive launcher phase, the native batch
rule, explicit reconciliation (`scripts/Reconcile-WorkflowBudget.ps1`), locked record
updates; `Test-TierSelection.ps1` 26/0. Shared budget `sprint1`: 50.4 of 90 min spent.

Native, on this candidate with the isolated root spelled as the player's
(`-EngineShapedSaveRoot`): owner-seed Manual producer, separate-process cold load, Quick
producer through the real slot route, its separate-process cold load, and the Auto slot
route all PASS with exact restoration. Open: overwrite/rotation (the repaired branch) is
not exercised natively, no genuine area-transition Auto event, no Manual overwrite, UPG-01
not run; the old Working fixture lineage still fails in the engine's `Player.PostLoad` on
preview.96 and this candidate alike.

**Owner play/visual acceptance of Chunk 7 remains TODO.** No merge, release or installation
is selected. Live installation restored exactly: Mods tree empty as at intake, loader
parameters and preferences exact, protected saves unchanged, no lock or game process.

The [sprint report](docs/SPRINT1-SAVE-RELIABILITY-2026-10-29.md) holds identities, the
native batch outcomes, cost, safety and the next checks. Prior Chunk 7 state:
[CURRENT before Sprint 1](docs/history/CURRENT-before-sprint1-2026-10-29.md).
