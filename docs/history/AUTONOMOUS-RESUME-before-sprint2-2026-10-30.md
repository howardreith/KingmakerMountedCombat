# Sprint 1 resume record (2026-10-30, iteration 7)

Machine: DESKTOP-SRJJ623. Worktree
`C:\Dev\KingmakerMountedCombatLab\worktrees\sprint1-save-reliability`, branch
`codex/mounted-combat-sprint1-save-reliability` (the request validator requires the
`codex/mounted-combat-` prefix). The root repository stays on `main` with the frozen
preview.96 `bin\Release` DLL untouched.

## Custody

No native game, executor, transaction or lock remains. Retained under `runtime-evidence`
(iterations 1–6 as the previous records listed) plus iteration 7's
`20261030-sprint1-r16-upg01-resave-cold-pair-order` (FAIL on `f6b21085…` at `pair-stock-attack-order`,
my "exactly one mount rule" term; its `attack` observation records the whole sequence) and
`20261030-sprint1-r17-upg01-resave-cold-pair-order-r2` (PASS on `02558b81…`: the complete UPG-01
consumer with 24 assertions, no write, readers PASS). Their isolated roots under
`runtime-staging\persistence-20261030-sprint1-r1{6,7}-*` hold the r13-r2 resave copy (unchanged).
Immutable inputs: genuine Quick `83e92e1e…` (`runtime-staging\persistence-final211-p05-save-quick`),
r13-r2 resave `dfaf00be3afe1f57ac8eccaa27af2daba7cf3a0329bda160da141878b8f41a78` (856,711 B), dependency
selection `analysis-cache\sprint1\retained-dependencies-final211.json` (`523d32dc…`). Current package
`…preview.98-sprint1q-diagnostic.zip` (`93deddbb…`, DLL `02558b81…`, MVID `3f28f251…`, built at
`1655511b`), suite `sprint1q-1655511b-20261030` (`97638780…`); sprint1p (`f6b21085…`, r16), sprint1o
(`3ada7451…`, r15) and sprint1n (`7f49f857…`, r13-r2/r14-r2) retained. Shared budget record
`runtime-state\workflow-budgets\sprint1.json`: 138.28 of 150 min, 108 entries, two recorded allowance
adjustments (+45, +15), batches `sprint1:upgrade-3` (r15, r16 failed) and `sprint1:upgrade-4` (r17 PASS).
The r16/r17 UMM session log (`Kingmaker_Data\Managed\UnityModManager\Log.txt`) is overwritten by the
next game session; the attributing lines are quoted in the sprint report.

## Next action

1. Final review of PR #6 (head of this branch); then the owner's own sanity check with the preview.98
   package on their reported save (CURRENT.md); owner play acceptance stays separate. No merge,
   release or installation is part of this workflow.
2. If a later product candidate is cut: new package and suite (the suite pins package, DLL, MVID and
   commit); the Quick/Manual rows (r06–r08) ran on `a680dd69…`, SAVE-02 on `bed87d75…`, the UPG-01
   producer on `7f49f857…` and its consumer on `02558b81…`; rerun only what a changed production path
   affects (the save, restore and serialization paths are unchanged since `4644a433`).
3. Known, not in scope: the probe's DidTrigger-based `UnexpectedPairAttackCount` received duplicated
   notifications for the mount's natural attack rules (4 and 5 for 2 rules) in the Call of the Wild
   configuration; distinct rules are counted from the begin/end-paired records, the notification count
   is recorded only. The P04, Chunk 6A and 6B readers pin `Combat.Actors.Count` as the semantic
   expectation (equal to the derived distinct union only when every pair actor is also a combat actor).
   `scripts/chunk7/Test-Chunk7SixPairExitReader.ps1` is 3/22 at `main` (fixture requests carry an older
   `productVersion` than `version.json`).
4. Tooling facts: a WMI-started chain wrapper must carry its own env and writes a UTF-16 log
   (`tr -d '\0'` before `grep`); observation `gameTicks` are 100-ns ticks; use absolute Windows paths
   with .NET and PowerShell from the tool shell; Bash tool commands beyond about 60 lines are truncated;
   no `python` on this machine (use perl); `Test-Tiers.ps1` records under budget `development` unless
   `KMC_WORKFLOW_BUDGET_ID` is set (import the duration otherwise).

Prior Chunk 7 resume: [AUTONOMOUS-RESUME before Sprint 1](docs/history/AUTONOMOUS-RESUME-before-sprint1-2026-10-29.md).
