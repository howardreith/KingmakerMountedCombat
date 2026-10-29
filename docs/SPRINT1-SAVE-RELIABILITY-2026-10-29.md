# Sprint 1 — saving reliability and an enforced development loop (2026-10-29)

**Disposition: SPRINT 1 PARTIAL — READY FOR REVIEW.** Owner play acceptance is separate.
Review branch `codex/mounted-combat-sprint1-save-reliability` (from main `8aa44d3a`):
`3ac7384e` persistence repair + regressions, `2273d3d5` workflow enforcement, `d590822f`
version stamp, then this record. Product `0.1.0-chunk7b-preview.97`, DLL SHA-256
`09c00dda524bf0a61eb721f543cdea68776178bce0b8c73455aa3e0c8324c64e`, MVID
`8180a0b0-8f1c-4e30-b4d5-f87d56cc5ba7`. Package
`artifacts\KingmakerMountedCombat-0.1.0-chunk7b-preview.97-sprint1b-d590822f-diagnostic.zip`
SHA-256 `d4a857fa36c59344448059eece16611ee7ce0f669bc8920afd6653ca1e2ecfd3`; suite
`sprint1b-d590822f-20261029` SHA-256 `2320b9d25874b382d882984a2e6d14011a682dfe83cf8cd8f9b8da6689155dbb`.
(Earlier same-commit package `sprint1-d590822f` and its suite `sprint1-d590822f-20261029`
were superseded only by the branch rename the request validator requires; bytes identical.)

## Cause and repair

**Proven.** Quick and Auto saves overwrite an existing slot: `SaveManager.GetNextQuickslot`
counts every Quick save in the root (13 in the owner's), `GetNextAutoslot` counts the
campaign's Auto saves, and when the slot count is reached the engine passes the existing
descriptor into `SerializeAndSaveThread`'s replacement branch (`originalSave.Saver.Clear();
saveInfo.Saver.RenameFile(originalSave.FolderName)`), which KMC transpiles into
`NativeMountedArchiveCommit.PreservePrevious/Replace`. Their validator required
`Path.GetFullPath(path) == path`. The engine mints every archive path from Unity's
`Application.persistentDataPath`, which the mod now logs at load as
`C:/Users/Howie/AppData/LocalLow/Owlcat Games/Pathfinder Kingmaker` (UMM log, run
`20261029-sprint1-p05-save-quick-r2`), joined with backslashes: the player's paths never
equal their canonical form, so the validator threw `IOException`, the engine's catch
deleted the staged archive and raised `SavingError`, and KMC reported "the previous
complete save is unchanged". The lab's isolated authority substitutes a canonical root,
which is why every lab rotation row passed. Manual saves with a new name never reach the
branch (first-ever write), which is why Manual kept working; overwriting an existing Manual
would have failed the same way.

**Repair** (`NativeArchiveReplacementPath`): canonicalize both engine strings and check
file identity (two distinct existing regular files in one directory, plain rooted local
paths); `File.Replace` runs on the canonical forms, the descriptor keeps the engine's own
string. No slot, rotation, transaction, timeout, barrier or gameplay change.

**Remaining hypotheses.** The owner's installed build at the time of the report is unknown
(the live Mods tree was empty at intake); the branch has existed since 2026-09-20, so every
build since is affected. The roguelike context was not separately exercised.

## Scope

| ID | Status | Evidence |
| --- | --- | --- |
| SAVE-01 | Repaired; offline proof executed; native rotation route NOT RUN | Reproduction on the frozen preview.96 DLL `9d090645…`: `Test-NativeArchiveCommitPaths.ps1 -ExpectRefusal` 4/0 (real ZipSaver/SaveInfo, engine-shaped paths, no commit, both archives intact). Verification on the candidate: 7/0. Components 912/0 incl. 4 new path tests. `persistence-p05-save/quick` (the only native rotation route) failed before any save, see below. |
| SAVE-02 | Same repair; actual area-autosave event NOT RUN on the candidate | First-ever Auto rows (owner-seed `multi-exploration-auto`, `multi-area-autosave`) remain RETAINED UNAFFECTED EVIDENCE on preview.96: the repair touches only the replacement branch. |
| SAVE-03 | Manual path untouched; contracts pass; native Manual control NOT RUN | `Test-PersistenceContracts.ps1` (atomic replacement, locked-file failure keeps both archives, owned leases) PASS; Candidate tier 7/0 on the packaged bytes (Test-Harness 281/0, assembly contracts Kingmaker 751/0). |
| Save failure safety | EXECUTED offline on the candidate | Locked replacement leaves previous and staged archives intact, no commit recorded (`Test-PersistenceContracts.ps1`, `NativeSaveCommitOutcome` tests). |
| Action-state regression | Not applicable | Settlement, snapshots and restoration are unchanged. |
| UPG-01 | Genuine input identified; upgrade load NOT RUN (blocked) | `runtime-staging\persistence-final211-p05-save-quick\Saved Games\Quick_1.zks`, SHA-256 `83e92e1a…`, written 2026-10-11 by preview.211 (`kmc-mounted-state` schema 2, `Mounted: true`). Its lineage is the Chunk 5 Working fixture whose native load now fails in the engine (below). The owner's own saves carry no KMC metadata. |
| PROC-01 | Implemented and exercised | `Test-Tiers.ps1 -Plan`, nested `-Focused` paths, Kingmaker default, `-QualificationTask` gate for Full, `scripts/WorkflowBudget.ps1` shared record (lab `runtime-state\workflow-budgets\sprint1.json`), launcher admission before lock, `scripts/workflow-impact-map.json`; `Test-TierSelection.ps1` 22/0. |

## Native batch (3 launches, 2 dry runs)

- `20261029-sprint1-p05-save-quick` (preview.97): refused before staging — branch outside
  `codex/mounted-combat-*`; lock, evidence and transaction record retained (restored).
- `20261029-sprint1-p05-save-quick-r2` (preview.97): **FAIL** while loading
  `KMC_AUTOMATION_WORKING`: `Kingmaker.Player.PostLoad` → `Enumerable.Single` "Sequence
  contains no matching element" (Unity log stack; engine frame, before any save request).
  UMM pruned the absent `CheatMenu` entry from `Params.xml`, which the non-base-game delta
  rule refuses, so the run ended `restoration-blocked` with Mods, saves and profile already
  exact. Recovery: the drifted file was kept as `params-drifted-by-umm.xml` in the evidence,
  the backed-up bytes were restored (SHA-256 `ca5d4022…`), then
  `Recover-KmcPersistenceProfile.ps1` PASS (`profile-recovery.json`, lock released).
- `20261029-sprint1-baseline96-p05-save-quick` (accepted preview.96, `-BaseGameOnly`):
  **identical FAIL** at the same engine frame; restored exactly. The route last passed on
  preview.211 (`final211-p05-save-quick`, 2026-10-12). Classification: pre-existing
  baseline route failure, retained, not caused by this sprint; two equivalent failures,
  so no further repetition.

## Cost

| Item | Value |
| --- | --- |
| Active work (intake → report) | about 2 h |
| Validation recorded in `sprint1` budget | 10.4 min (Fast/Candidate tiers, package, suite, dry runs, two launches) |
| Validation outside the record | ~1.3 min Fast runs before the budget existed; ~1.4 min baseline run (preview.96 launcher) |
| Settlement/restoration | ~2 min (profile recovery), recorded separately |
| Game launches | 3 (1 refused pre-lock, 2 FAIL at fixture load); 2 WhatIf dry runs |

## Safety

Live installation: Mods tree empty (as at intake), `UnityModManager\Params.xml` exact
(`ca5d4022…`), PlayerPrefs restored, protected-save digest `60e3d6cc…` unchanged, no game
process, worker, transaction or lock; no candidate installed. The frozen root
`bin\Release` preview.96 DLL is untouched (work happened in
`worktrees\sprint1-save-reliability`). Failed evidence, the drifted parameter copy,
suites and packages are retained.

## Review focus

- `src/.../Integration/NativeArchiveReplacementPath.cs`, `NativeMountedArchiveCommit.cs`
  (identity vs string-form checks; descriptor keeps the engine string), `Main.cs` log line.
- `scripts/Test-Tiers.ps1`, `scripts/WorkflowBudget.ps1`,
  `scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1` (admission placement; restoration
  never shortened), `scripts/Test-TierSelection.ps1`, `scripts/Validate-Source.ps1` anchor.
- Risk: the player-shaped path is proven offline against real engine types, not in a game
  session; the lab cannot reproduce it without changing what the isolation substitutes.

**Owner verification (short):** install the preview.97 package, load the reported save,
press QuickSave twice (both must report "Game saved" with full Quick slots), cross an area
boundary that autosaves, then make one Manual save; the UMM log should show
`Native persistent data path: C:/Users/...` and no `SavingError`.

**Smallest next lab check:** a rotation case in the owner-seed family (`GetNextQuickslot`
twice with slot count 1) or a refreshed Working fixture made on the current product, then
`persistence-p05-save/quick` and `persistence-p05-load/quick` on preview.97; UPG-01 can
load `Quick_1.zks` above once that lineage loads again.
