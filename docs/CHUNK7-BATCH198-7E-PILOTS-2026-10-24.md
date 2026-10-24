# Chunk 7 batch198: 7E development pilots, sixth pass

Status: **development pilots; not 7E qualification. Every run restored exactly.** Eight native
launches on one frozen candidate (`767feaea`), no startup retry. **Row 58 passed for the first time**
(4/0): native Oleg's moved the three story companions and pair B to remote exactly as in b197, the
capital admission recorded them, and the genuine `KMC_C7_CAPITAL` Manual 309 was written. **Row 59 failed
closed** on a diagnostic defect before Bind's readiness check was reached. The X5 upgrade rehearsal
(batch B) did not run: its observer.6 package cannot be built at this HEAD. The standing mission remains
IN PROGRESS.

## Candidate (development pilot)

| Item | Identity |
| --- | --- |
| Source | `767feaea` (admits the capital's native remote story companions for rows 58–59), frozen worktree `worktrees/chunk7-7e-b198-source-767feaea` (LF re-materialized); packaged and run from the clean root branch checkout at the same commit |
| Package | `KingmakerMountedCombat-0.1.0-chunk7b-preview.94-c7e-b198-pilots-diagnostic.zip`, `0052bf49…`, 3,689,351 bytes; `Info.json` (329) and the DLL (7,991,296) only |
| Manifest / DLL / MVID | `841cbe97…` / `870b6437…` / `e16d4a0e-655e-42c8-83e7-b3ca977e3a3c` |
| Candidate tier | 6/0 (3.73 min), receipt `b198-candidate-01.log` (`89018807…`) |
| Suite | `chunk7e-b198-pilots-20261024`, `37c6f8b4…` (save digest `4f6375da…`, Mods digest `ed57b9d2…`) |
| Root `bin\Release` DLL | `870b6437…` before and after packaging (equal to the package DLL) |
| Helpers | `analysis-cache/chunk7-continuation/b198-stage.ps1`, `b198-run.ps1`, `b198-native-launch.ps1`, `b198-derive.ps1`, `b198-check.ps1`, `b198-freeze-step.ps1`, `new-suite-b198.ps1`, `b198-go.ps1` (reuses each b197 plan's scenario, selection parameter, persistence case and area target with a b198 run id); freeze record `b198-freeze.json` (`bea7a7e2…`) |

The helpers are the b197 helpers with only the batch, candidate and pin values changed.
`b198-derive.ps1 -Batch b197` reproduced all five b197 chain selections (c2, c3, c4, c5, x4s)
byte-for-byte. They could still not be reused: `Assert-KmcChunk7OwnedSourceCompletion` requires the
lineage source to have run on the current candidate (commit, DLL, MVID). So the chain c1–c5 was run
again on this candidate. The r58 selection is the x4s selection with case `persistence-load` and the x4s
persistence artifact as checkpoint, as in b197. The r59 selection is the r58 selection with the r58 artifact as
checkpoint and `expectedNativeWriteCount` 0, with case `multi-exploration-capital-arrival-cold-manual` and no
area target. Every run passed its development-scoped WhatIf first.

## Runs

| Batch | Run (`20261024-c7e-b198-…`) | Verdict | Key evidence |
| --- | --- | --- | --- |
| A | `c1-ownerseed-cold` | **PASS** (4/0) | artifact `82bcde38…` |
| A | `c2-ownerfield` | **PASS** (7/0) | Manual 304; artifact `1b64ed69…` |
| A | `c3-acquired-cold` | **PASS** (8/0) | Manual 305; artifact `efe1c5f4…` |
| A | `c4-cumulative` | **PASS** (7/0) | Manual 306; Small Ranger `169a5931…`, Horse `67912538…`; artifact `7960a312…` |
| A | `c5-acquired-postfield` | **PASS** (8/0) | Manual 307 `2ac3bd56…`; artifact `45d3d1f7…` |
| B | `x4s-mounted-save` | **PASS** (5/0) | persistence artifact `a7d40bfd…` |
| B | `r58-capital-arrival` | **PASS** (4/0) | persistence artifact `209798ef…`; Manual 309 `KMC_C7_CAPITAL` `a2af8e39…` |
| B | `r59-capital-cold` | **FAIL** (1/1) | `ReadSource` refused; see below; artifact `16c85682…` |

After every run (PASS and FAIL alike):
- The outer result reports Mods restored, save protection passed, Working restored, Baseline unchanged
  and the write allowlist passed, with restored save inventory digest `95fabf2e…`.
- The external `Test-RuntimeResult` read every result without error (33/0 and 29/0).
- `Params.xml` and PlayerPrefs (including `KingdomDifficulty`) equal the run's own snapshot, and
  `Assert-KmcPersistenceProfileUnchanged` passed with 0 admitted changes.
- No Kingmaker process and no lock remained.

Batches: A = c1–c5 (5 launches). B = x4s, r58, r59 (3 launches). No Steamworks startup failure occurred.

### Row 58: PASS

The cold load restored both pairs (main Ranger `73681ef2…` / Horse `65951649…`; Small Ranger
`169a5931…` / Horse `67912538…`). The single transfer to Oleg's (`fd225a93…` → `ead426a6…`) settled in
the capital: retired 1, failed 0, active owners 1, save allowed. The exact UMM line was: "Dismounted on
arrival because the companion is not directly controllable in this area: KMC_C7_20261024-c7e-b198-c4-cumulative
and Horse. Mount again after leaving. 1 of 2 mounted relationships restored without Mount or preparation."
The restoration observer recorded only `restore-debt-before/after` events with zero debt, `prepared` false and not in combat. One Manual (309,
`KMC_C7_CAPITAL`, 1,174,846 bytes) was written in the capital.

Arrival roster (`capitalArrival.after.roster`). All seven actors were in party, characters where applicable,
in game and controllable before the transfer.

| Actor | inParty | character | remote | inGame | controllable |
| --- | --- | --- | --- | --- | --- |
| Main Ranger `73681ef2…` | true | true | false | true | true |
| Story companion `5dd7bd1d…` | false | false | true | true | false |
| Story companion `8265f8cf…` | false | false | true | true | false |
| Story companion `fd213210…` | false | false | true | true | false |
| Small Ranger (pair B rider) `169a5931…` | false | false | true | false | false |
| Main Horse `65951649…` | true | false | false | true | true |
| Small Horse (pair B mount) `67912538…` | false | false | false | false | false |

`retiredOutOfScene` (shape `remote`) names exactly pair B (both in state, view present, not in game,
not controllable, live rig not validated). The `admittedNativeRemoteCompanions` record (identical in
`capitalArrival.retiredOutOfScene` and `inheritedCleanup.capitalOutOfScene`) holds exactly the three story
companions `5dd7bd1d…`, `8265f8cf…` and `fd213210…`. Each has inParty false, character false, remote true,
inGame true, inState true, directlyControllable false, viewPresent true, mainCharacter false and pairMember false.
Each has skippedChecks `field-party-membership`, `field-character-membership` and `not-remote-companion`.

### Row 59: fails closed in `ReadSource`, before Bind's readiness check

The game reported FAIL from `Chunk7PersistenceScenario.ObserveCapitalCold` →
`BindOwnedFixture` → `Chunk7OwnedPairFixture.Bind` → `ReadSource`:

> System.InvalidOperationException: Staged source lacks its complete successful same-candidate two-owner Manual identity.

Bind never reached `FixCapitalNativeRemote` or any custody check. So **the readiness check for the remote
companions (View.AgentASP present, commands empty, not in combat) was not exercised**: no observation exists
for it. Nothing was adapted.

Read-only diagnosis of the refused condition (`Chunk7OwnedPairFixture.cs`, the generic acquired-lineage
branch of `ReadSource`): every field matches the staged c5 receipt except the last one:
- c5's scenario, evidence kind, run, PASS, no errors, commit, DLL and MVID all match.
- The selection case `preserve-two-acquired-field`, base-game-only, custody settled, and the write SHA, game id and game name also match.
- The failing condition is `nativeWrite.area == originalArea`. Here `originalArea` is `request.PersistenceLoad.Area`
  (only `multi-area-autosave` uses `selection.SourceArea`). For row 59 the load is the capital Manual 309, so
  `originalArea` is `ead426a6…` (Oleg's). The lineage Manual 307 was written in `b10e8651…` (Spider Cave).

Row 58 passed the same check because it loads the x4s Manual written in `b10e8651…`.
This is a product diagnostic defect in the row-59 path. Row 59 has never run natively, and its offline tests
did not model the staged source's area. The fix belongs in source and needs review: for the capital cold
case, compare the lineage write area with the lineage area, not the load area. Then re-pilot r58 and r59
on one candidate.

## Batch B (X5 upgrade/rollback rehearsal): not run, prerequisite missing

The rehearsal needs observer `0.1.0-observer.6` packaged by `scripts/Package-Observer.ps1` at the clean
execution HEAD (`Assert-KmcNormalPlayerObserverManifest`). At `767feaea`,
`tools/KmcRemovalObserver/ObserverIdentity.cs` declares `ProductVersion = "0.1.0-observer.6"`. But
`tools/KmcRemovalObserver/Info.json` still says `"Version": "0.1.0-observer.5"`. The X5 commit `b3d993c9`
bumped the identity and not Info.json. `scripts/Validate-ObserverPackage.ps1` line 19 refuses that pair
before packaging completes ("Observer Info.json version differs from its identity source."). Fixing it is a
source change that moves HEAD and needs review. So batch B stopped before any build, package or launch;
no observer artifact was written. The preview.211 `-b` previous package is present in `artifacts`.

## Lab notes

- A shared-config slip occurred while creating the frozen worktree. `git -C <worktree> config core.autocrlf false`
  wrote the shared `.git/config` value. It was restored to `true` (the recorded repository value) within a minute,
  before any build, and the root and worktree were verified clean. The worktree itself was re-materialized as LF with per-command
  `-c core.autocrlf=false`.
- Final state: no Kingmaker process, no lock, Mods/saves/profile restored after every run.

## Next

1. Review-gated fix of the row-59 `ReadSource` area comparison for the capital cold case, then re-pilot
   x4s → r58 → r59 on one candidate (the chain must run again on the new candidate).
2. Bump `tools/KmcRemovalObserver/Info.json` to `0.1.0-observer.6` (review), then build observer.6 and pilot
   X5 u1 → u2 → u3.
3. Exact-package 7E qualification of the rows that pass in development.
