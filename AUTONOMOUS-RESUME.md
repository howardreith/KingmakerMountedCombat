# Sprint 1 resume record (2026-10-30, iteration 5)

Machine: DESKTOP-SRJJ623. Worktree
`C:\Dev\KingmakerMountedCombatLab\worktrees\sprint1-save-reliability`, branch
`codex/mounted-combat-sprint1-save-reliability` (the request validator requires the
`codex/mounted-combat-` prefix). The root repository stays on `main` with the frozen
preview.96 `bin\Release` DLL untouched.

## Custody

No native game, executor, transaction or lock remains. Retained under `runtime-evidence`
(iterations 1–4 as the previous records listed) plus iteration 5's
`20261030-sprint1-r13-upg01-deps-load-resave` (PASS on `2a0be4d8…`),
`20261030-sprint1-r14-upg01-deps-resave-cold` (FAIL: the observer read the legacy schema-2 view, null
for the schema-3 resave), `20261030-sprint1-r13-upg01-deps-load-resave-r2` (PASS on `7f49f857…`:
the genuine preview.211 Quick cold-loaded with Call of the Wild 1.14.4c-2.1 and Craft Magic Items
2.1.0 staged, one native Manual resave `Manual_1_KMC_P01.zks` `dfaf00be…`) and
`20261030-sprint1-r14-upg01-deps-resave-cold-r2` (FAIL on `early-semantic-and-once-presentation`
after the mounted pair was restored; counters not recorded). Their isolated roots under
`runtime-staging\persistence-20261030-sprint1-r1{3,4}-*` hold the retained Quick copy (unchanged)
and the resave. Dependency selection: `analysis-cache\sprint1\retained-dependencies-final211.json`
(`523d32dc…`), sourced from `runtime-staging\final211-p05-save-quick\Mods-staged-after`. Current
package `…preview.98-sprint1n-diagnostic.zip` (`2c04d648…`, DLL `7f49f857…`), suite
`sprint1n-53e48785-20261030`; `sprint1m` (`2a0be4d8…`) retained for r13/r14. Shared budget record
`runtime-state\workflow-budgets\sprint1.json`: 123.97 of 135 min, 92 entries; batch
`sprint1:upgrade-2` holds two failed `persistence-p05-load/manual` attempts, so its consumer needs a
new batch.

## Next action

1. UPG-01 last step (about 7 budget minutes, needs a fresh allowance decision): make the P05 cold
   scenario (`RuntimePersistenceScenario`, Cold branch) record the actual restore counters in its
   evidence and derive the expected semantic count from the archive's actor snapshots (pairs plus
   participation owners); build once, package, freeze a suite, then run the producer
   (`persistence-p05-load/quick -UpgradeChain`) and the consumer
   (`persistence-p05-load/manual -UpgradeChain`) on that one DLL under `sprint1:upgrade-3`, both
   `-BaseGameOnly -RetainedDependencySelectionPath <selection>` (the chain wrapper used this
   iteration lives in the session scratchpad; its arguments are recorded in each run's request).
2. Final review of PR #6; then the owner's own sanity check with the preview.98 package on their
   reported save (CURRENT.md); owner play acceptance stays separate. No merge, release or installation.
3. If a later product candidate is cut: new package and suite (the suite pins package, DLL, MVID and
   commit); the Quick/Manual rows (r06–r08) ran on `a680dd69…` and SAVE-02 on `bed87d75…`; rerun only
   what a changed production path affects.
4. Test debt outside scope: `scripts/chunk7/Test-Chunk7SixPairExitReader.ps1` is 3/22 at `main`
   (fixture requests carry an older `productVersion` than `version.json`).
5. A WMI-started chain wrapper must carry its own env and writes a UTF-16 log (`tr -d '\0'` before
   `grep`). Use absolute paths with .NET file APIs in the tool shell; Bash tool commands beyond about
   60 lines are truncated; a function that `return ,$array` nests under a caller's `@( )`.

Prior Chunk 7 resume: [AUTONOMOUS-RESUME before Sprint 1](docs/history/AUTONOMOUS-RESUME-before-sprint1-2026-10-29.md).
