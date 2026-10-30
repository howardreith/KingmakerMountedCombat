# Sprint 1 — saving reliability: PARTIAL, READY FOR FINAL REVIEW (2026-10-30, iteration 6)

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
`0.1.0-chunk7b-preview.98`. Current bytes: DLL `3ada7451…` / MVID `d471c2e6…` built at `77f2d3c8`
(package `…preview.98-sprint1o-diagnostic.zip` `819571a3…`, suite `sprint1o-77f2d3c8-20261030`
`17d35245…`). Candidate tier 9/0 on the sprint1m bytes (`2f8db203`); the later deltas are observer
code (`53e48785`, `77f2d3c8`), two archive-DTO derivation helpers and one harness classifier, covered by
the affected offline checks (`Test-PersistenceData` 141 PASS, `Test-BaseGameIsolation` 119/0). The
production save, restore and serialization paths are unchanged since `4644a433`. Evidence map, retained
versus current: SAVE-01 and SAVE-03 ran on DLL `a680dd69…` (iteration 3, r06–r08), SAVE-02 on
`bed87d75…` (iteration 4, r11/r12), the UPG-01 producer on `7f49f857…` (r13-r2), the UPG-01 consumer on
`3ada7451…` (r15). Shared budget `sprint1`: 129.15 of 135 min.

Dispositions:

- **SAVE-01 Quick overwrite: PASS natively** (r06, twice through the engine's QuickSave handler body
  onto `Quick_1.zks`; separate-process cold load r07 restored both pairs).
- **SAVE-02 Auto overwrite: PASS natively** (r11, the genuine `Game.LoadArea(…, AfterEntry)` autosave
  over an admitted pre-existing `Auto_1.zks` through the replacement branch, exactly once; cold r12).
- **SAVE-03 Manual overwrite: PASS natively** (r08; outer reader corrected and re-evaluated on the
  retained observations, original outer FAIL preserved).
- **UPG-01: load and resave PASS natively; cold load of the resave restores correctly but its combat
  continuation is not yet accepted.** The genuine preview.211 mounted Quick (`83e92e1e…`) loaded under
  the candidate with Call of the Wild 1.14.4c-2.1 and Craft Magic Items 2.1.0 staged from the Oct-12
  producer's retained tree (never installed) and resaved one schema-3 Manual (`Manual_1_KMC_P01.zks`
  `dfaf00be…`, r13-r2). Correction to iteration 5: the restore counters WERE recorded on the retained
  r14-r2 failure rows (semantics 3, presentation 1); the resave carries one pair, one participation owner
  and three combat actors, so the schema-3 restore contract (one semantic restore per distinct saved
  actor, one presentation per attached pair) expects exactly 3/1 and the old fixed `2 && 1` check was
  wrong. The corrected consumer (r15, on `3ada7451…`, reusing the r13-r2 resave as its immutable input)
  passed the derived counter check, actor identity, uniqueness, hotbar bindings, no-cast and the mounted
  movement stage, then failed `native-ordinary-attack-delivered`: the resave had captured the producer's
  still-active party combat (resaved right after its target was destroyed), the world restored in combat,
  and after the rider's own attack rule the check's only failable term is the unexpected-pair-attack count
  (the mount attacked on its own; deduced, not recorded). Product defect or fixture artifact is not yet
  established. r09/r10 and r14/r14-r2 remain retained as failed attempts.
- **PROC-01:** the bounded loop stays in force (Fast/Candidate tiers, the shared 135-minute `sprint1`
  record with its one recorded adjustment, batch and two-equivalent-failure rules, parameter-shape
  refusals decided before admission). Also repaired this iteration: a dependency-folder write exception
  that let a mismatching or orphaned UMM loader cache pass; four retained runs re-evaluated identical.

**Exact remaining action for UPG-01** (needs a fresh allowance decision; 5.85 min remain): record the
rule probe's actual initiator, target and unexpected-pair counts on failure, then decide the cause with one
native pair: either the producer resaves only after the engine has left combat (fixture artifact) or the
restored RT combat participation must not leave the mount free to act on its own brain (product). About
7–8 budget minutes for build, suite, producer and consumer.

**Owner sanity check** (separate from engineering acceptance, on the owner's own installation with the
mods they play with, the preview.98 package installed by the owner): load the reported save; QuickSave
twice and confirm the quicksave slot's timestamp changes both times; travel to another area and confirm
the autosave slot updates; make a Manual save and reload it; reload the quicksave in a fresh game start.

**Owner play/visual acceptance of Chunk 7 remains TODO.** Live installation restored exactly after every
run: Mods tree empty as at intake, loader parameters and preferences exact, protected saves unchanged,
no lock or game process. The [sprint report](docs/SPRINT1-SAVE-RELIABILITY-2026-10-29.md) holds the
identities, batch outcomes, cost and safety. Prior Chunk 7 state:
[CURRENT before Sprint 1](docs/history/CURRENT-before-sprint1-2026-10-29.md).
