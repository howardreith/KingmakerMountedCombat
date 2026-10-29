# Sprint 1 — saving reliability: PARTIAL, READY FOR REVIEW (2026-10-29)

The owner's playtest finding (Quick and Auto saves fail with KMC enabled, Manual works)
is reproduced and repaired at source: the native replacement commit refused the engine's
own mixed-separator archive paths (Unity's forward-slash persistent data path joined with
backslashes), which only overwrites reach. Manual saves with new names never reach that
branch. The lab's isolated root is canonical, which is why every lab rotation row passed.
Reproduction on the frozen preview.96 DLL and verification on the candidate both use the
real engine types offline (`scripts/Test-NativeArchiveCommitPaths.ps1`).

Review branch `codex/mounted-combat-sprint1-save-reliability`, candidate
`0.1.0-chunk7b-preview.97` (commit `d590822f`, DLL `09c00dda…`, package
`…preview.97-sprint1b-d590822f-diagnostic.zip` `d4a857fa…`). Candidate tier 7/0 on the
packaged bytes; workflow enforcement (`Test-Tiers.ps1 -Plan`, nested `-Focused` paths,
Kingmaker default, Full gate, shared `WorkflowBudget.ps1`, launcher admission) tested
22/0 and used live.

Open: native rotation proof (`persistence-p05-save/quick`) is blocked by a pre-existing
engine `Player.PostLoad` failure of the Chunk 5 Working fixture lineage on preview.96 and
preview.97 alike (two equivalent failures retained, restored exactly); UPG-01 input exists
(preview.211 `Quick_1.zks`, mounted) but shares that lineage; owner play verification of
Quick/Auto on their own saves remains the decisive check.

**Owner play/visual acceptance of Chunk 7 remains TODO.** No merge, release or installation
is selected. Live installation restored exactly: Mods tree empty as at intake, loader
parameters and preferences exact, protected saves unchanged, no lock or game process.

The [sprint report](docs/SPRINT1-SAVE-RELIABILITY-2026-10-29.md) holds identities, the
native batch outcomes, cost, safety and the next checks. Prior Chunk 7 state:
[CURRENT before Sprint 1](docs/history/CURRENT-before-sprint1-2026-10-29.md).
