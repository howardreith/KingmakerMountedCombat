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

---

## Iteration 2 — review corrections and the owner-seed save routes (2026-10-29, later)

**Disposition: still SPRINT 1 PARTIAL — READY FOR REVIEW.** Candidate
`0.1.0-chunk7b-preview.98`, commit `aa08ba1a`, DLL SHA-256
`4c085da3cc8f55d91f95f39ce4bbe2abf254ab6592ff63fb46ad880d825f23c5`, MVID
`63c75dab-55d1-49ab-91ab-ff315a198d73`, package
`…preview.98-sprint1h-aa08ba1a-diagnostic.zip` `207d635d…`, suite
`sprint1h-aa08ba1a-20261029` `6d1818ae…`. Candidate tier 8/0 on these bytes;
Test-TierSelection 26/0; components 913/0.

### Review items

1. **Per-step budget gate (P1).** `Test-Tiers.ps1` re-admits every step against the
   cumulative allowance, the 30-minute block ceiling and the step's recorded cost; known
   oversized work is refused at admission (the known part of a selection is a lower bound)
   and at the step boundary; a refusal stops only steps that have not started. The native
   launcher enforces the batch rule: five distinct scenario/case selections per batch, two
   failed attempts per selection. Tests: a marker stub after the exhausting stub never runs;
   the block ceiling; oversized refusals at both boundaries; the batch rule.
2. **Inherited entries (P1).** An inherited entry is used only when it exists, is running,
   belongs to a live process and has allowance left; completed, missing or dead parents are
   reported as stale and the invocation is accounted on its own. The launcher admits before
   source/package validation and completes through a script-scope trap, restoring the
   caller's context; an early launcher failure followed by another invocation in the same
   shell is metered (tested).
3. **Accounting (P2).** Admission precedes every expensive launcher phase; heartbeats at the
   preflight, lock and launch boundaries and every 15 s around tier children; interrupted
   entries are charged to the last measured point, to their estimate when unmeasured, and
   otherwise block admission until `scripts/Reconcile-WorkflowBudget.ps1` records them.
   Record updates run under an exclusive lock file. The first iteration's unrecorded work
   (78 s, 36 s, 85 s) is imported; this iteration's spend is in the same record.
4. **Path guard (P2).** `Canonical` accepts only `<drive>:<separator>…`; drive-relative and
   root-relative forms resolving onto the real archives are refused (tested), mixed
   separators remain accepted.

### Keeping the player's path condition in the lab

Isolated runs can opt in (`-EngineShapedSaveRoot`, request `engineShapedSaveRoot`) to an
engine-shaped root: the isolation hands `SaveManager.PrepareSave` the isolated root's parent
spelled as Unity spells `Application.persistentDataPath`, so every archive path the engine
mints is the player's mixed-separator form inside the same owned root; `SavePath` stays
canonical and the authority compares observed paths by file identity. The evidence of the
runs below records the prepared path as
`C:/Dev/KingmakerMountedCombatLab/runtime-staging/persistence-<run>\Saved Games\<leaf>`.

### Native batch `sprint1:owner-seed-chain-2` (NO_MODS owner seed, two mounted pairs)

| Run | Route | Outcome |
| --- | --- | --- |
| `20261029-sprint1-r01-ownerseed-manual-r4` | owner-seed Manual producer (new slot) | PASS; `Manual_304_KMC_C7_SEED_WORLD.zks` |
| `20261029-sprint1-r02-ownerseed-manual-cold` | separate-process cold load of r01 | PASS; both pairs restored |
| `20261029-sprint1-r03-ownerseed-quick-r2` | Quick producer: `SaveManager.GetNextQuickslot()` + `Game.SaveGame`, the body of the hotkey's `MakeQuickSave` (key press not synthesized), first-ever slot | PASS; `Quick_1.zks` `3b2f4ba3…` |
| `20261029-sprint1-r04-ownerseed-quick-cold` | separate-process cold load of the Quick save | PASS; mounted state restored |
| `20261029-sprint1-r05-ownerseed-auto` | Auto producer: `GetNextAutoslot()` + `Game.SaveGame` (slot route, not an area event), first-ever slot | PASS; `Auto_1.zks` `1f5972bc…` |

Every run: Mods restored, saves protected, profile bytes exact, no lock. Retained failures
on the way (all restored exactly): `r01-ownerseed-manual` (in-game request refusal of the
new flag, fixed in `05494fcd`), `r01-…-r2` and `r03-ownerseed-quick` (scenario
canonical-spelling assertions, fixed in `31d50be2` / `aa08ba1a`), `r01-…-r3` (game PASS,
outer reader canonical-spelling check; reader-only correction `22f41eb7`; re-evaluation
receipt `analysis-cache\sprint1\r01-r3-reader-only-reevaluation.log`, original outer FAIL
retained). r03–r05 consumed r01/r02 produced by the previous build of the same source line
(`22f41eb7`); the admission accepted them, which is recorded here rather than relabelled.

### Still open

- **Overwrite/rotation** (the repaired branch) is not exercised natively: every owner-seed
  route writes a first-ever slot. The offline reproduction and verification against real
  engine types remain the evidence for the repair itself.
- **Genuine area-transition Auto event** and **Manual overwrite** not run; **UPG-01** not run
  (input and blocker unchanged).
- The old Working fixture lineage still fails in the engine's `Player.PostLoad` on both
  builds (unchanged, retained).

### Cost (this iteration)

| Item | Value |
| --- | --- |
| Active work | about 2 h 10 min |
| Shared budget `sprint1`, cumulative | 50.4 of 90 min (9 tier runs 28.8 min, 27 native entries 17.7 min, including the imported 3.3 min) |
| Real game launches this iteration | 9 (5 PASS, 4 retained FAIL/refusal) |
| Restoration outside the spend | none needed beyond the runs' own settlement |

**Next check (smallest):** a `multi-exploration-quick-rotation` case in the owner-seed
checkpoint family (second `GetNextQuickslot()` under slot count 1 over `Quick_1.zks`, both
leaves declared) on this candidate, then its cold load; the same for Auto via
`multi-area-autosave` after an in-run second transition.

## Iteration 3 — the repaired branch through ordinary saves; launch accounting (2026-10-29, later)

**Disposition: still SPRINT 1 PARTIAL — READY FOR REVIEW.** The Quick overwrite (the
repaired branch) is now exercised natively through the engine's own QuickSave handler body,
twice, with cold load; the Manual overwrite control passed natively (reader-only outer
correction, re-evaluated); the genuine area-transition Auto overwrite and UPG-01 remain the
missing results. Owner play acceptance is separate.

Candidate: product `0.1.0-chunk7b-preview.98`, DLL SHA-256
`a680dd6982485fa0d4305d5f3294327fb3b133b508d56c69765eec81236722ea`, MVID
`0b40e3b8-460e-4dff-8ced-619f009fa795` (deterministic build; identical bytes from `4644a433`
through `b3256e52`, which change scripts only). Packages of the same DLL:
`…preview.98-sprint1i-diagnostic.zip` `078e841d…` (commit `4644a433`, suite
`sprint1i-4644a433-20261029` `65d03aa9…`, runs r06 and the first r07 attempt) and
`…preview.98-sprint1j-diagnostic.zip` `5995751b…` (commit `984ae01a`, suite
`sprint1j-984ae01a-20261029` `70b5b2e3…`, runs r07, r08, r09). Candidate tier 9/0 on the
packaged bytes (components 913/0 with the new authority tests, Validate-Source 149/0,
Test-Harness, paired-activation, manifest, assembly Kingmaker, `Test-NativeArchiveCommitPaths`
7/0, `chunk7\Test-Chunk7OwnedPairsEvidence.ps1` with the rotation fixture and its refusals);
`Test-TierSelection` 29/0; `chunk7\Test-Chunk7OwnedPairsSource.ps1` PASS.

### Workflow corrections (A1–A3, `3acb4ee1`)

- **Nested native attempts count.** A real game launch under a verified live parent entry is
  admitted against the batch's five selections and the two-failure limit, recorded as an
  attempt under the parent (`parentEntryId`, no spend of its own), and completed with its own
  outcome; a nested dry run records no attempt; the parent's time is counted once and the
  child never completes the parent. A dead parent with a still-"running" attempt reconciles
  as one spend (`counted-in-parent`). Tested with a live parent: five selections, sixth
  refused, two failures then a third refused, dry run, single-counted time, parent ownership.
- **Recheck before the game.** The launcher re-admits before the live transaction (after
  preflight; a refusal leaves no evidence directory, lock, staging or game) and immediately
  before `Start-Process` (a refusal propagates to the transaction's `finally`, which restores
  normally; a running game or save worker is never interrupted). Tested with a launcher-shaped
  stub whose preflight or staging advances the clock past exhaustion (launch marker absent,
  owned lock released by the stub's own restoration path) and by an order check on the real
  launcher's text (lock re-admission before `Open-KmcRuntimeLock`, launch re-admission before
  `Start-Process`, suspension before restoration, resumption before the final reader,
  completion only after it).
- **Final failures and reader time.** Restoration suspends the entry's clock (excluded, never
  charged; a dead suspended entry is charged only to its suspension); the final readers run
  metered again; the entry completes only after the final verdict, so a late restoration or
  reader failure stays a failed attempt (r07's refused first attempt and r08's outer FAIL are
  recorded exactly so). Tested: game PASS then reader FAIL, restoration FAIL, success, and
  completion while suspended, each with the expected net seconds and the retry limit.

### The repaired branch through ordinary save behaviour (`4644a433`, readers `984ae01a`, `b3256e52`)

Two owner-seed checkpoint cases in the existing family, selected by name, one write plan each:
`multi-exploration-rotation-quick` (three writes) and `multi-exploration-overwrite-manual`
(two writes). The first write is the generic one (slot route for Quick; `CreateNewSave` for
Manual). Every further write is an ordinary overwrite of that selected output and must land as
a native replacement commit: `NativeMountedArchiveCommit` records a commit only at the
transpiled replacement site, so the commit count and its destination, the changed bytes at the
unchanged leaf, the absent next-number staging leaf, the settled two-descriptor inventory, the
released serialization holds and a full read-back of the two-pair graph are the proof per
write. The authority's worker-cancel seed/replacement model is generalised: a rotation
re-reserves the same staging leaf after each landed overwrite, and every owned-pair write after
the first must be such a replacement (`OwnedPairsAuthorityTests`: both cases, the over-quota
write, a one-write checkpoint that never reserves a replacement).

**Input provenance.** Quick overwrites call `Game.MakeQuickSave()`, the method the engine's
`Keyboard.Bind("QuickSave", …)` delegate invokes (`UpdateSaveListIfNeeded` then
`SaveGame(GetNextQuickslot())`); the key press itself is not synthesized, and the producer
first verifies that `GetNextQuickslot()` returns the existing descriptor under the projected
one-slot capacity. The Manual overwrite calls `Game.SaveGame` on the existing Manual
descriptor, as the save menu's overwrite does. Neither route touches the replacement method.

### Native batch `sprint1:owner-seed-chain-2` (two further selections) and `sprint1:upgrade-1`

| Run | Route | Outcome |
| --- | --- | --- |
| r06 `20261029-sprint1-r06-ownerseed-rotation-quick` | Quick producer, 3 writes | PASS. Write 1 slot route `Quick_1.zks` `5f11bffd…`; write 2 handler, commits 0→1 onto `Quick_1.zks`, `82a548aa…`, `Quick_2.zks` staging absent; write 3 handler, commits 1→2, `dcb23dc1…`; no failed save, no replacement failure, capacity 1, both pairs' custody and slots unchanged after each read-back. |
| r07 first attempt | cold consumer of r06 | refused in preflight: the consumer's producer gate pinned one native write (reader `Chunk7OwnedPairsFixtures.ps1`, fixed in `984ae01a` to the producer's own count; `Test-Chunk7OwnedPairsSource` covers 3 accepted / 1 refused / plain Quick with 3 refused). Counted as a failed attempt of that selection. |
| r07 `20261029-sprint1-r07-ownerseed-rotation-quick-cold` | separate-process cold load of the rotated `Quick_1.zks` `dcb23dc1…` | PASS, both pairs restored (`loadedEqualsProducerReadback`), restored exactly. |
| r08 `20261029-sprint1-r08-ownerseed-overwrite-manual` | Manual control: new `Manual_305_KMC_C7_SEED_CHECKPOINT.zks`, then its overwrite | game PASS (2 writes, 1 replacement commit, final `24929d4f…`, staging `Manual_306…` absent); outer reader FAIL on my new settlement check (it expected one saved descriptor of the category; a Manual overwrite sees the source Manual and the new one). Reader-only correction `b3256e52`; re-evaluation receipt `analysis-cache\sprint1\r08-reader-only-reevaluation.log` (PASS); the original outer FAIL is retained in its `runtime-result.json`. |
| r09 `20261029-sprint1-r09-upg01-p05-load-quick` | UPG-01: `persistence-p05-load/quick` on the preview.211-written `Quick_1.zks` `83e92e1a…` (provenance verified: `final211-p05-save-quick` PASS, `final211-p05-load-quick` request pins the same bytes) | FAIL in the engine: "loading pipeline stopped before the exact Working load callback completed" (1 assertion). The exception text is not retained on the base-game-only route (no UMM log; the profile is restored byte-exact). Same symptom as the lineage's `Player.PostLoad` failure in iteration 1, but not proven for this archive. Restored exactly. |

Every run restored exactly (`restoredSaveInventoryDigest` `60e3d6cc…` unchanged, Mods tree
empty as at intake, loader parameters `ca5d4022…`, no lock or game process).

### Still open

- **Genuine area-transition Auto overwrite.** Not run. The one-transition area producer writes a
  first-ever `Auto_1`; a second `AfterEntry` transition in the same process (re-entering the
  destination, so the replacement stays same-area) needs the area scenario's single-shot
  observation state made re-entrant and the authority's same-area replacement contract kept.
  Not attempted within this iteration's active-work ceiling; the r05 slot-route Auto remains a
  first-ever write only.
- **UPG-01.** Attempted on its own archive; blocked in the engine's load before the callback
  (above). Stays open with that recorded blocker; no resave or derivative cold load was possible.
- Manual overwrite: native PASS with a reader-only outer correction; no further native run was
  spent on it (the retained evidence has every observation).

### Cost (this iteration)

| Item | Value |
| --- | --- |
| Active work | about 2 h 50 min |
| Shared budget `sprint1`, cumulative | 78.47 of 90 min (this iteration 28.1 min: offline tiers 18.7 min incl. two failed builds from a mis-targeted project-file edit, native entries 6.9 min, imported packaging/suite freezes 2.6 min) |
| Real game launches this iteration | 5 attempts: r06 PASS, r07 preflight refusal, r07 PASS, r08 game PASS/outer FAIL (re-evaluated PASS), r09 FAIL |
| Restoration outside the spend | the runs' own settlement only (suspended, excluded) |

**Safety note.** Two wasted builds came from editing the product project file with a .NET
file API on a relative path inside the tool shell, which resolved against the root repository;
the root's project file was restored to its committed bytes immediately (`git checkout --`
on that one file), the root stayed on `main` with its frozen preview.96 DLL untouched
(`9d090645…`), and the worktree edit was redone by absolute path.

## Iteration 4 — the genuine area-transition Auto overwrite and the UPG-01 cause (2026-10-30)

Assignment: close SAVE-02 through one genuine supported area event that overwrites an
admitted, verified pre-existing Auto of the same fixture campaign/area, and recover the
UPG-01 cause before any further load. One-time allowance adjustment on the same `sprint1`
record: 90 → 135 min (`allowanceAdjustments[0]`, applied 2026-10-30T03:18:32Z, authority
"Sprint 1 completion mission, review iteration 4 prompt forwarded by the owner, 2026-10-29").

### SAVE-02: admitted pre-existing Auto, overwritten by the engine's own `AfterEntry` autosave (`d48e7627`)

- **Selection.** `owner-seed-checkpoint-save` / `multi-area-autosave` on the accepted
  Manticore → Spider Cave route (`manticore-to-spider-v1`, source Manual `86a5ff4b…`, enter point
  `00becd53…`, destination `b10e8651…`) now carries `existingAutoRunId/ArtifactSha256/Sha256`:
  the first-ever producer `20261029-c7e-final-B6-r28r`'s own `Auto_1.zks` (`ede760c8…`, its
  evidence artifact pinned). `ValidateKind` refuses the fields on every other case; the source
  gate (`Get-KmcChunk7OwnedExistingAutoSource`) requires that producer to be an accepted
  first-ever write of the same campaign, route and destination, never itself an overwrite.
- **Authority.** `ReserveOwnedPairsAreaOverwrite` marks the admitted `Auto_1.zks` writable
  (`AdmissionArea` = destination) and reserves staging `Auto_2.zks`; the plain Quick/Manual
  replacement path refuses while an overwrite reservation is pending; the bootstrap allowlist
  admits the pre-existing slot read-only beside the Manual. The launcher copies the admitted
  bytes under their own leaf and records `existingAutoSha256` in `owner.json`.
- **Scenario observations** (`evidence.areaOverwrite`, contract
  `sprint1-area-auto-overwrite-v1`): the admitted slot is selected by the native
  `GetNextAutoslot()` under capacity 1 (`ReferenceEquals` the admitted descriptor) before the
  transition; the genuine area entry raises the Auto request; the replacement branch commits
  exactly once at the existing leaf (`NativeMountedArchiveCommit` count +1, bytes changed,
  staging `Auto_2.zks` absent); holds settle; both pairs retained.
- **Readers.** `Test-KmcChunk7OwnedAreaOverwriteSelection/Overwrite`,
  `Assert-KmcChunk7OwnedAreaOverwrite` (path identity, not spelling), admission inventory of
  two descriptors; tests: authority 795/0, owner-seed area reader 71/0, owned source 337/0.
- **Native.** 
- **Cold.** `20261030-sprint1-r12-area-auto-overwrite-cold` **PASS** (`owner-seed-checkpoint-load`,
  checkpoint = r11, separate process, 112 s wall): the cold load of the overwritten
  `Auto_1.zks` (`15ac93fd…`, 740,629 B, area `b10e8651…`, campaign "Testy the Modless")
  restored both mounted pairs (schema 3, generation 5, rules `crpg-transport-v1`) with the
  restoration-entry observer validated before native mutation and no actions or
  preparation granted (game 3/3, loads 1, writes 0). SAVE-02's required observations are
  therefore all present: genuine event → native Auto request, existing slot selected under the
  disposable capacity, replacement branch committed exactly once (not invoked by the
  fixture), bytes at the existing slot with staging retired, holds settled, mounted
  relationships retained, separate-process cold load, engine-shaped path condition.


### UPG-01: the cause chain, decoded from the engine (no product repair yet)

- The r09 Unity player log survived (`recovered-output_log.txt`, `197ea294…`, copied into
  the r09 evidence with provenance): `Kingmaker.Player.PostLoad` → `Enumerable.Single`
  "Sequence contains no matching element", inside the native `LoadRoutine`; KMC's
  `TrackNativeLoad` only wraps and rethrows. No engine error line exists anywhere on disk.
- IL decoded: `Player.PostLoad` resolves the main character's inventory with
  `CrossSceneState.AllEntityData.Single(e => e.UniqueId == MainCharacter.UniqueId)`; the
  main character (`b6628a77…`, first unit of `party.json`) is therefore absent after
  `ThreadedGameLoader.DoLoad`. `CollectionsConverter.ReadJson` catches a per-element
  exception, logs "Failed to deserialize collection element" through `UberDebug` and skips
  the element; `BlueprintConverter.ReadJson` throws on an unknown guid. `UberDebug` writes
  only to `UberLogger` sinks, and the shipped game persists none of them.
- Ruled out: every stock/KMC blueprint guid the archive references is registered on this
  build; DLC headers identical across the Oct-11/27/29 archives; engine assembly unchanged.
  Open candidates: the product DLL (chunk7b vs chunk6d-preview.211) and UMM 0.33.2 (was
  0.28.2) on the main-thread/threaded JSON interaction. The Aug-14 Working fixture fails the
  same way on preview.96/.97, so this is not specific to the preview.211 Quick.
- **Repair of the evidence gap** (product): `NativeEngineLogTail` (`UberLogger.ILogger`,
  Warning/Error only, bounded 64 rows, forwards errors to the KMC log) registered for the life
  of a runtime request and published as `engineLogTail` on every result path (completion,
  no-save, six-pair exit) with a strict optional reader
  (`Assert-KmcNativeEngineLogTail`, startup/engine-tail reader 61/0).
- **Diagnostic load.** `20261030-sprint1-r10-upg01-p05-load-quick-logtail` **FAIL as r09** (87 s, batch
  `sprint1:upgrade-1`, the same `Player.PostLoad` exception), but with the engine's channel
  retained: 16 rows, and before the `Single` failure only the engine's own startup noise
  (BugReportCanvas NRE, duplicate common AssetBundle loads), identical to the passing r11/r12
  baseline. No "Failed to deserialize collection element" or "Failed to load blueprint" line
  reached the sink, so the element drop is silent on that channel as well (worker-thread
  logging is a plausible reason; not proven).
- **Cause, classified: fixture/dependency mismatch.** The archive's `party.json` carries 14
  `$type` members from the `CallOfTheWild` assembly: the main character `b6628a77…` holds six
  Call of the Wild unit parts (`UnitPartConfusionControl`, `UnitPartSizeOverride`,
  `UnitPartActivatableAbilityActionTypeModifier`, `FullAttackWatcherUnitPart`,
  `UnitPartSanctuary`, `UnitPartFlying`), the companion `d17c8fd0…` three, the mount
  `d79a4f6c…` none. The Aug-14 Working fixture `Manual_299` shows the identical pattern. Both
  were written while Call of the Wild was loaded (the Oct-12 save and load ran on the live-clone
  Mods overlay), and both fail only on the base-game-only staging whose Mods tree, like the
  owner's current live tree, has no Call of the Wild assembly: the units whose parts cannot be
  resolved are dropped from `CrossSceneState`, and `Player.PostLoad` fails on its first lookup.
  Every KMC-referenced blueprint and KMC's own schema-2 metadata resolve; KMC touches no
  entity data before `PostLoad` (`TrackNativeLoad` only wraps, `SelectLoad` reads only
  `kmc-mounted-state`). Not a product migration defect; not an observer defect.
- **Remaining action.** UPG-01's load → resave → separate-process cold load needs the
  archive's dependency staged: a run whose staged Mods tree carries the owner's Call of the
  Wild installation beside KMC (a retained snapshot of it or a reinstalled copy; the
  base-game-only mode cannot carry it and the current suite pins an empty Mods digest). The
  genuine archive is never edited. Owner note: any save made with Call of the Wild loaded
  cannot load without it, regardless of KMC's version.


### Workflow corrections

- **Parameter-shape refusals precede budget admission** (`f4294828`). The harness case
  "development-scoped WhatIf is refused without WhatIf" runs the launcher as a child that must
  be refused. Under a sprint budget each such child was recorded as a *failed native attempt*
  of the default batch `sprint1:batch1`; the third Candidate run therefore tripped the
  two-equivalent-failure rule before the scoped-audit refusal and the case failed on the wrong
  message (Candidate #1 of this iteration, 11/1). The scoped-audit, manual-review and
  offline-cloud shape checks now run before `Request-KmcWorkflowAdmission`; the harness case
  asserts that the refused children leave the budget record unchanged; the policy records the
  rule. The two historical `sprint1:batch1` entries stay in the record as what they were.
- Policy text now states the allowance adjustment mechanism (`allowanceMinutes` authoritative,
  `allowanceAdjustments` with the authorizing assignment).

### Test debt disclosed (not introduced here unless stated)

- `scripts/chunk7/Test-Chunk7OwnedPairsAuthority.ps1` was 766/11 at the reviewed head
  `ffd24b26` (764/0 at `main`): iteration 3's `4644a433` broke nine owner-seed setup cases
  (fixture `Scope()` output name) and two rotation cases (repeated reservation expected a
  refusal). Repaired here (`ownedPairsReplacementPending`, fixture scope); 795/0.
- `scripts/chunk7/Test-Chunk7OwnerSeedAreaEvidence.ps1` was 50/3 at `main` (pre-sprint): the
  cold builder lacked `checkpointCandidate` and `areaContinuationPlans`. Repaired; 71/0.
- `scripts/chunk7/Test-Chunk7SixPairExitReader.ps1` is 3/22 at `main` `8aa44d3a` and here:
  its retained fixture requests carry an older `productVersion` than `version.json`
  ("Runtime request product version is not exact"). Reproduced at the root checkout
  (12 s); not repaired (outside scope); the three source-pin cases, including the updated
  exit field set, pass. Excluded from this iteration's Candidate selection for that reason.

### Native batches `sprint1:area-overwrite-1` and `sprint1:upgrade-1`

| Run | Selection | Result |
| --- | --- | --- |
| `20261030-sprint1-r11-area-auto-overwrite` | `chunk7-owned-two-pair-campaign` / `multi-area-autosave`, admitted Auto overwrite, `sprint1:area-overwrite-1` | PASS (114 s) |
| `20261030-sprint1-r12-area-auto-overwrite-cold` | `owner-seed-checkpoint-load`, checkpoint r11, `sprint1:area-overwrite-1` | PASS (112 s) |
| `20261030-sprint1-r10-upg01-p05-load-quick-logtail` | `persistence-p05-load` / `quick`, source `final211-p05-save-quick`, `sprint1:upgrade-1` | FAIL as r09; engine tail retained (87 s) |

Each launch was preceded by its development-scoped WhatIf (PASS: 26 s, 25 s, 19 s). Package
`…preview.98-sprint1l-diagnostic.zip` `3eec930a…`, suite `sprint1l-f4294828-20261030`
`97bf6e99…`, DLL `bed87d75…` / MVID `846f1c80…` built at `f4294828`.


### Cost (this iteration)

| Item | Value |
| --- | --- |
| Active work | about 3 h 35 min |
| Shared budget `sprint1`, cumulative | 100.98 of 135 min (this iteration 22.5 min: offline tiers about 14.2 min, native 5.6 min for three WhatIfs and three launches, imported packaging/suite/direct-run time 2.75 min) |
| Real game launches this iteration | 3 launches: r11 PASS, r12 PASS, r10 FAIL (classified) |
| Restoration outside the spend | the runs' own settlement only (suspended, excluded) |

Offline tiers this iteration: Fast ×6 (#1 authority pre-existing failures, #2 area reader
pre-existing failures, #3 source-gate pin collision, #4 PASS, #5 SixPairExitReader
pre-existing, #6 startup wrapper count pin), Candidate ×2 (#1 harness budget interaction,
#2 12/0); packages `sprint1k` (`912abfd5…`, superseded unused) and `sprint1l`
(`3eec930a…`, same DLL `bed87d75…` / MVID `846f1c80…`); suite
`sprint1l-f4294828-20261030` (`97bf6e99…`).
