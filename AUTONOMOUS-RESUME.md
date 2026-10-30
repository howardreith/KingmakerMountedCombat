# Sprint 1 resume record (2026-10-30, iteration 6)

Machine: DESKTOP-SRJJ623. Worktree
`C:\Dev\KingmakerMountedCombatLab\worktrees\sprint1-save-reliability`, branch
`codex/mounted-combat-sprint1-save-reliability` (the request validator requires the
`codex/mounted-combat-` prefix). The root repository stays on `main` with the frozen
preview.96 `bin\Release` DLL untouched.

## Custody

No native game, executor, transaction or lock remains. Retained under `runtime-evidence`
(iterations 1–5 as the previous records listed) plus iteration 6's
`20261030-sprint1-r15-upg01-resave-cold-corrected` (FAIL on `3ada7451…` at
`native-ordinary-attack-delivered` after the corrected counter check, identity, hotbar, no-cast and
movement stages passed; its `initial` row carries `coldRestoreExpectation` with the derived IDs and both
actual counters). Its isolated root `runtime-staging\persistence-20261030-sprint1-r15-…` holds the
r13-r2 resave copy (unchanged, `dfaf00be…`) and the diagnostic residue the cleanup reported. Immutable
inputs: genuine Quick `83e92e1e…` (`runtime-staging\persistence-final211-p05-save-quick`), r13-r2
resave `dfaf00be3afe1f57ac8eccaa27af2daba7cf3a0329bda160da141878b8f41a78` (856,711 B), dependency
selection `analysis-cache\sprint1\retained-dependencies-final211.json` (`523d32dc…`). Current package
`…preview.98-sprint1o-diagnostic.zip` (`819571a3…`, DLL `3ada7451…`, MVID `d471c2e6…`, built at
`77f2d3c8`), suite `sprint1o-77f2d3c8-20261030` (`17d35245…`); sprint1n (`7f49f857…`) retained for
r13-r2/r14-r2. Classifier re-evaluation receipt
`analysis-cache\sprint1\write-classification-reevaluation-77f2d3c8.json` (`a4275bd3…`). Shared budget
record `runtime-state\workflow-budgets\sprint1.json`: 129.15 of 135 min, 98 entries; batch
`sprint1:upgrade-3` holds one failed `persistence-p05-load/manual` attempt (a second attempt there
needs a materially changed experiment; a third needs a new batch).

## Next action

1. UPG-01 last step (fresh allowance decision; about 7–8 budget minutes): make the P05 scenario record
   `MountedCombatRuleProbe`'s actual `AttackRuleCount`, `AttackRollCount`, `LastInitiatorId`,
   `LastTargetId` and `UnexpectedPairAttackCount` on `assertion-failed`; then settle the cause with one
   producer/consumer pair on one DLL: if the producer resaves only after `Game.Instance.Player.IsInCombat`
   has cleared, the consumer starts outside combat (fixture artifact); if the mount still attacks on its
   own after a cold load into restored RT combat, that is a pair-authority product defect to fix before
   any rerun. The chain wrapper pattern (session scratchpad `chain6.ps1`) and each run's request record
   the exact arguments; the consumer reuses the r13-r2 resave through the `upgrade-resave-manual` source
   role (archive-hash pinned, producer DLL not pinned).
2. Final review of PR #6; then the owner's own sanity check with the preview.98 package on their
   reported save (CURRENT.md); owner play acceptance stays separate. No merge, release or installation.
3. If a later product candidate is cut: new package and suite (the suite pins package, DLL, MVID and
   commit); the Quick/Manual rows (r06–r08) ran on `a680dd69…` and SAVE-02 on `bed87d75…`; rerun only
   what a changed production path affects.
4. Test debt outside scope: `scripts/chunk7/Test-Chunk7SixPairExitReader.ps1` is 3/22 at `main`
   (fixture requests carry an older `productVersion` than `version.json`). The P04, Chunk 6A and 6B
   readers pin `Combat.Actors.Count` as the semantic expectation (equal to the derived distinct union only
   when every pair actor is also a combat actor); untouched here.
5. A WMI-started chain wrapper must carry its own env and writes a UTF-16 log (`tr -d '\0'` before
   `grep`). Use absolute Windows paths with .NET and PowerShell from the tool shell; Bash tool commands
   beyond about 60 lines are truncated; no `python` on this machine (use perl).

Prior Chunk 7 resume: [AUTONOMOUS-RESUME before Sprint 1](docs/history/AUTONOMOUS-RESUME-before-sprint1-2026-10-29.md).
