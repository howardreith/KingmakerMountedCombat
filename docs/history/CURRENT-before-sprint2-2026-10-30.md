# Sprint 1 — saving reliability: ENGINEERING COMPLETE, READY FOR FINAL REVIEW (2026-10-30, iteration 7)

**SPRINT 1 ENGINEERING COMPLETE — READY FOR FINAL REVIEW; OWNER PLAY ACCEPTANCE PENDING.** No merge, release
or installation is selected. Every item in scope (SAVE-01/02/03, UPG-01, PROC-01) has its accepted native
evidence; owner play acceptance stays separate.

The owner's playtest finding (Quick and Auto saves fail with KMC enabled, Manual works) was reproduced
and repaired at source: the native replacement commit refused the engine's own mixed-separator archive
paths (Unity's forward-slash persistent data path joined with backslashes), which only overwrites reach.
Manual saves with new names never reach that branch. The reproduction on the frozen preview.96 DLL and
the verification on the candidate use the real engine types offline
(`scripts/Test-NativeArchiveCommitPaths.ps1`); the guard also refuses drive-relative and root-relative
spellings and never writes outside the owned isolated root.

Review branch `codex/mounted-combat-sprint1-save-reliability`
([draft PR #6](https://github.com/howardreith/KingmakerMountedCombat/pull/6)), candidate
`0.1.0-chunk7b-preview.98` (that display string is shared by several binaries; identity is the hash).
Current bytes: DLL `02558b81…` / MVID `3f28f251…` built at `1655511b` (package
`…preview.98-sprint1q-diagnostic.zip` `93deddbb…`, suite `sprint1q-1655511b-20261030` `97638780…`).
Candidate tier 9/0 on the sprint1m bytes (`2f8db203`); every later delta (`53e48785`, `77f2d3c8`,
`68094f1e`, `1655511b`) is observer code, two archive-DTO derivation helpers or one harness classifier,
covered by the affected offline checks (Fast 3/0 with components 914/0, `Test-PersistenceData` 141 PASS,
`Test-BaseGameIsolation` 119/0). The production save, restore and serialization paths are unchanged
since `4644a433`. Evidence map, retained versus current: SAVE-01 and SAVE-03 ran on DLL `a680dd69…`
(iteration 3, r06–r08), SAVE-02 on `bed87d75…` (iteration 4, r11/r12), the UPG-01 producer on
`7f49f857…` (r13-r2), the UPG-01 consumer on `02558b81…` (r17). Shared budget `sprint1`: 138.28 of
150 min (one +45 and one +15 adjustment of the same record, every entry retained).

Dispositions:

- **SAVE-01 Quick overwrite: PASS natively** (r06, twice through the engine's QuickSave handler body
  onto `Quick_1.zks`; separate-process cold load r07 restored both pairs).
- **SAVE-02 Auto overwrite: PASS natively** (r11, the genuine `Game.LoadArea(…, AfterEntry)` autosave
  over an admitted pre-existing `Auto_1.zks` through the replacement branch, exactly once; cold r12).
- **SAVE-03 Manual overwrite: PASS natively** (r08; outer reader corrected and re-evaluated on the
  retained observations, original outer FAIL preserved).
- **UPG-01: PASS natively, end to end.** The genuine preview.211 mounted Quick (`83e92e1e…`) loaded under
  the candidate with Call of the Wild 1.14.4c-2.1 and Craft Magic Items 2.1.0 staged from the Oct-12
  producer's retained tree (never installed), was usable, and resaved one schema-3 Manual
  (`Manual_1_KMC_P01.zks` `dfaf00be…`, r13-r2). The separate-process cold load of that unchanged resave
  (r17, same staged dependencies) restored exactly its 3 saved actors and 1 pair, the same native
  rider and mount, hotbar bindings, no synthetic Mount, mounted movement, the pair's attack on a new
  hostile in the product's own order, spent and unrefunded actions, retained pair authority, the
  continuation and the fixture cleanup, writing nothing. The earlier consumer failures were observer
  defects, each explained from retained evidence: r14/r14-r2 a schema-2-era fixed counter (the resave's
  three in-combat actors restore as 3/1), r15 an attack check that assumed a rider without restored
  Standard debt (KMC's own stock-click policy dispatches the ready mount first, a legal pair command; the
  rider attacked 9.9 s of game time later), r16 my "exactly one mount rule" term (one native full attack
  carries two natural attacks). The in-combat resave is a supported player state and was never altered.
- **PROC-01:** the bounded loop stays in force (Fast/Candidate tiers, the shared `sprint1` record,
  batch and two-equivalent-failure rules, parameter-shape refusals decided before admission); the
  dependency-folder write exception no longer admits a mismatching or orphaned loader cache.

**Owner sanity check** (separate from engineering acceptance, on the owner's own installation with the
mods they play with, the preview.98 package installed by the owner): load the reported save; QuickSave
twice and confirm the quicksave slot's timestamp changes both times; travel to another area and confirm
the autosave slot updates; make a Manual save and reload it; reload the quicksave in a fresh game start.

**Owner play/visual acceptance of Chunk 7 remains TODO.** Live installation restored exactly after every
run: Mods tree empty as at intake, loader parameters and preferences exact, protected saves unchanged,
no lock or game process. The [sprint report](docs/SPRINT1-SAVE-RELIABILITY-2026-10-29.md) holds the
identities, batch outcomes, cost and safety. Prior Chunk 7 state:
[CURRENT before Sprint 1](docs/history/CURRENT-before-sprint1-2026-10-29.md).
