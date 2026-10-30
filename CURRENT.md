# Sprint 1 — saving reliability: PARTIAL, READY FOR FINAL REVIEW (2026-10-30, iteration 5)

**SPRINT 1 PARTIAL — READY FOR FINAL REVIEW; OWNER PLAY ACCEPTANCE PENDING.** No merge, release or
installation is selected. The one open item is the last step of UPG-01 (below); everything else in
scope is closed.

The owner's playtest finding (Quick and Auto saves fail with KMC enabled, Manual works) was reproduced
and repaired at source: the native replacement commit refused the engine's own mixed-separator archive
paths (Unity's forward-slash persistent data path joined with backslashes), which only overwrites reach.
Manual saves with new names never reach that branch. The reproduction on the frozen preview.96 DLL and
the verification on the candidate use the real engine types offline
(`scripts/Test-NativeArchiveCommitPaths.ps1`); the guard also refuses drive-relative and root-relative
spellings and never writes outside the owned isolated root.

Review branch `codex/mounted-combat-sprint1-save-reliability`
([draft PR #6](https://github.com/howardreith/KingmakerMountedCombat/pull/6)), candidate
`0.1.0-chunk7b-preview.98`. Current bytes: DLL `7f49f857…` / MVID `333b8676…` built at `53e48785`
(package `…preview.98-sprint1n-diagnostic.zip` `2c04d648…`, suite `sprint1n-53e48785-20261030`
`58775520…`); Candidate tier 9/0 on the sprint1m bytes (`2f8db203`) and Fast tier 4/0 on these, the
only delta being the observer correction `53e48785`. The production save paths are unchanged since
`4644a433`; every later commit is diagnostics, harness, readers or documentation. Evidence map,
retained versus current: SAVE-01 and SAVE-03 ran on DLL `a680dd69…` (iteration 3, r06–r08, not rerun
on later bytes), SAVE-02 on `bed87d75…` (iteration 4, r11/r12), UPG-01 on `7f49f857…` (iteration 5,
r13-r2/r14-r2; the first pair r13/r14 on `2a0be4d8…`). Shared budget `sprint1`: 123.97 of 135 min.

Dispositions:

- **SAVE-01 Quick overwrite: PASS natively** (r06, twice through the engine's QuickSave handler body
  onto `Quick_1.zks`; separate-process cold load r07 restored both pairs).
- **SAVE-02 Auto overwrite: PASS natively** (r11, the genuine `Game.LoadArea(…, AfterEntry)` autosave
  over an admitted pre-existing `Auto_1.zks` through the replacement branch, exactly once; cold r12).
- **SAVE-03 Manual overwrite: PASS natively** (r08; outer reader corrected and re-evaluated on the
  retained observations, original outer FAIL preserved).
- **UPG-01: load and resave PASS natively; cold load of the resave not yet accepted.** The genuine
  preview.211 mounted Quick (`83e92e1e…`) loaded under the current candidate with its actual serialized
  dependencies, Call of the Wild 1.14.4c-2.1 and Craft Magic Items 2.1.0, staged from the Oct-12
  producer's retained tree inside the owned transaction (never installed): the original main character
  `b6628a77…` and mount `d79a4f6c…` resolved as the mounted pair, the world was controllable, one native
  Manual resave landed as a new schema-3 archive in the same area and campaign, and the original archive
  stayed byte-identical (r13 PASS, r13-r2 PASS on the final bytes, resave `Manual_1_KMC_P01.zks`
  `dfaf00be…`). The separate-process cold load of that resave (r14, r14-r2) restored the mounted pair
  natively (the first cold assertion passed on r14-r2 once the observer read the schema-3 member) but
  the P05 scenario's Chunk-5-era counter expectation (exactly two semantic and one presentation
  restore) failed on the resave, which carries one combat participation owner from the producer's own
  scripted attack; the actual counter values are not in the evidence. Two consumer attempts failed in
  batch `sprint1:upgrade-2` and the remaining allowance cannot fund the corrected build, suite,
  producer and consumer cycle, so the chain stops here. r09/r10 remain retained as knowingly invalid
  base-game-only configurations; nothing is claimed for other Call of the Wild versions or other saves.
- **PROC-01:** the bounded loop stays in force (Fast/Candidate tiers, the shared 135-minute `sprint1`
  record with its one recorded adjustment, batch and two-equivalent-failure rules, parameter-shape
  refusals decided before admission).

**Exact remaining action for UPG-01** (about 7 budget minutes on a fresh allowance decision): make the
P05 cold scenario record the actual `SemanticRestoreCount`/`PresentationRestoreCount` in its evidence
and derive the expected semantic count from the archive's own actor snapshots (pairs plus participation
owners); build once, package, freeze a suite, then rerun the producer and the consumer on that one DLL
under a new batch (`sprint1:upgrade-3`).

**Owner sanity check** (separate from engineering acceptance, on the owner's own installation with the
mods they play with, the preview.98 package installed by the owner): load the reported save; QuickSave
twice and confirm the quicksave slot's timestamp changes both times; travel to another area and confirm
the autosave slot updates; make a Manual save and reload it; reload the quicksave in a fresh game start.

**Owner play/visual acceptance of Chunk 7 remains TODO.** Live installation restored exactly after every
run: Mods tree empty as at intake, loader parameters and preferences exact, protected saves unchanged,
no lock or game process.

The [sprint report](docs/SPRINT1-SAVE-RELIABILITY-2026-10-29.md) holds identities, the native batch
outcomes, cost, safety and the next checks. Prior Chunk 7 state:
[CURRENT before Sprint 1](docs/history/CURRENT-before-sprint1-2026-10-29.md).
