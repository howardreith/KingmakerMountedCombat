# Chunk 7 batch199: 7E development pilots, seventh pass

Status: **development pilots; not 7E qualification. Every run restored exactly.** Nine native
launches on one frozen candidate (`3842ad94`), no startup retry. The acquired chain (c1–c5), the X4
producer and row 58 passed again. **Row 59 passed for the first time** (2/0): Bind's readiness check
admitted the three native remote story companions, and the capital Manual 309 cold-loaded with exactly
the main pair restored and pair B unmounted out of scene. **The X5 rehearsal stopped at u1** (outer FAIL):
the released preview.211 logs an exception at Horse blueprint registration in the base-game-only tree,
so its start is not the "ordinary quiet" start that u1 requires. u2 and u3 need a passed u1 and did not
run. The standing mission remains IN PROGRESS.

## Candidate (development pilot)

| Item | Identity |
| --- | --- |
| Source | `3842ad94` (row-59 lineage-area pin `c4945317`, observer.6 Info.json `58015966`, review follow-up `3842ad94`), frozen worktree `worktrees/chunk7-7e-b199-source-3842ad94` (detached, LF re-materialized with per-command `-c core.autocrlf=false`; shared config stays `true`); packaged and run from the clean root branch checkout at the same commit |
| Package | `KingmakerMountedCombat-0.1.0-chunk7b-preview.94-c7e-b199-pilots-diagnostic.zip`, `fbe187c8…`, 3,689,473 bytes |
| Manifest / DLL / MVID | `26246b59…` / `ec20ee8f…` / `eef3c13c-f782-4170-9360-d2c9549146dd` |
| Candidate tier | 6/0 (3.76 min), receipt `b199-candidate-01.log` (`2165db22…`) |
| Suite | `chunk7e-b199-pilots-20261024`, `3e98d8c3…` (save digest `4f6375da…`, Mods digest `ed57b9d2…`) |
| Root `bin\Release` DLL | `ec20ee8f…` before packaging, after the Candidate tier and after the observer build (equal to the package DLL) |
| Observer (X5) | `KmcRemovalObserver-0.1.0-observer.6-c7e-b199-x5-01-diagnostic.zip`, `eb9a5d87…`, manifest `cddd4319…` (commit `3842ad94`, observer DLL `2c7c3b60…`, MVID `298490af-…`), built by `Package-Observer.ps1` at the clean execution HEAD after batches A and B (7/0) |
| Helpers | `analysis-cache/chunk7-continuation/b199-*.ps1` (the b198 helpers with only batch, candidate and pin values changed), `new-suite-b199.ps1`, `b199-observer-package.ps1`, `b199-x5-run.ps1`, `b199-x5-launch.ps1`; freeze record `b199-freeze.json` (`4b7a05e0…`) |

`b199-derive.ps1 -Batch b198` reproduced all five b198 chain selections (c2, c3, c4, c5, x4s)
byte-for-byte before any launch. The chain ran again on this candidate, because
`Assert-KmcChunk7OwnedSourceCompletion` binds the lineage to the current candidate. The r58 selection
is the x4s selection with case `persistence-load` and the x4s persistence artifact as checkpoint. The r59
selection is the r58 selection with the r58 artifact as checkpoint and `expectedNativeWriteCount` 0. It
also carries the new `sourceArea` field, read from this batch's r58 record
`capitalArrival.sourceArea` (`b10e86516b6437d45a629931c30936fe`, Spider Cave). Every launch passed its
development-scoped WhatIf first.

## Runs

| Batch | Run (`20261024-c7e-b199-…`) | Verdict | Key evidence |
| --- | --- | --- | --- |
| A | `c1-ownerseed-cold` | **PASS** (4/0) | artifact `d7c77c3f…` |
| A | `c2-ownerfield` | **PASS** (7/0) | Manual 304; artifact `c8111592…` |
| A | `c3-acquired-cold` | **PASS** (8/0) | Manual 305; artifact `c3b4350b…` |
| A | `c4-cumulative` | **PASS** (7/0) | Manual 306; Small Ranger `66e2933b…`, Horse `a1923328…`, main Horse `789d7739…`; artifact `ada05506…` |
| A | `c5-acquired-postfield` | **PASS** (8/0) | Manual 307 `458d19c3…`; artifact `5d07ab96…` |
| B | `x4s-mounted-save` | **PASS** (5/0) | Manual 308; persistence artifact `b73c1232…` |
| B | `r58-capital-arrival` | **PASS** (4/0) | persistence artifact `9127dfdd…`; Manual 309 `KMC_C7_CAPITAL` `d24d86b0…` (1,175,094 bytes) |
| B | `r59-capital-cold` | **PASS** (2/0) | persistence artifact `b590fcd6…` |
| C | `x5-u1-previous` | **FAIL** (outer; observer 27/0) | see X5 below; result `fd26d6ed…` |

After every run (PASS and FAIL alike):
- The outer result reports Mods restored, save protection passed, Working restored, Baseline unchanged
  and the write allowlist passed, with restored save inventory digest `95fabf2e…`.
- `Params.xml` and PlayerPrefs (including `KingdomDifficulty`) equal the run's own snapshot, and
  `Assert-KmcPersistenceProfileUnchanged` passed with 0 admitted changes.
- No Kingmaker process and no lock remained.
- The external `Test-RuntimeResult` read the eight PASS results without error (33/0 and 29/0). On u1 it
  refused, as designed, with the binding message quoted below.

Batches: A = c1–c5 (5 launches). B = x4s, r58, r59 (3 launches). C = u1 (1 launch). No Steamworks
startup failure occurred.

### Row 58: PASS again

Same native shape as b198. The single transfer (`fd225a93…`, Spider Cave `b10e8651…` → Oleg's
`ead426a6…`) settled with retired 1, failed 0, active owners 1, save allowed, phase
`all-pair-presentation-restored`. The exact UMM line was: "Dismounted on arrival because the companion is
not directly controllable in this area: KMC_C7_20261024-c7e-b199-c4-cumulative and Horse. Mount again
after leaving. 1 of 2 mounted relationships restored without Mount or preparation."
`retiredOutOfScene` names exactly pair B (`66e2933b…`, `a1923328…`). `admittedNativeRemoteCompanions`
holds exactly the three story companions `5dd7bd1d…`, `8265f8cf…` and `fd213210…`.

### Row 59: PASS (first native pass)

The product loaded r58's Manual 309 (`d24d86b0…`, area `ead426a6…`, capital). Game 2/0
(`C7-persistence-capital-arrival-cold`, `C7-persistence-cleanup`), outer PASS.

- **ReadSource:** the c4945317 pin held. The staged c5 receipt was accepted against the selection's
  `sourceArea` (Spider Cave), not the load area.
- **Bind's readiness check:** it passed for the remote story companions. In `Chunk7OwnedPairFixture.Bind`,
  every party actor outside the out-of-scene pair must be in state and in game, have `View.AgentASP`
  and a non-null `Commands`, have empty commands, and not be in combat. That set includes the three
  remote story companions. Bind completed (`inheritedFixture.phase` `admitted`), and the capital
  out-of-scene record names exactly pair B. Its `admittedNativeRemoteCompanions` are the same three
  companions as in r58, each with inParty false, character false, remote true, inGame true, inState
  true, directlyControllable false, viewPresent true, and the same three skipped checks.
- **Cold-load roster** (`capitalCold.world.roster`):

| Actor | inParty | character | remote | inGame | controllable | view |
| --- | --- | --- | --- | --- | --- | --- |
| Main Ranger `73681ef2…` | true | true | false | true | true | true |
| Story companion `5dd7bd1d…` | false | false | true | true | false | true |
| Story companion `8265f8cf…` | false | false | true | true | false | true |
| Story companion `fd213210…` | false | false | true | true | false | true |
| Small Ranger (pair B rider) `66e2933b…` | false | false | true | false | false | true |
| Main Horse `789d7739…` | true | false | false | true | true | true |
| Small Horse (pair B mount) `a1923328…` | false | false | false | false | false | true |

- **The product's one-pair capital restore:** phase `all-pair-presentation-restored`, feedback "Saved
  mounted world restored all 1 attachments and 0 participation owners without Mount execution or new
  native preparation." Registry owners 1 and active owners 1, both the main pair `dae5a306…` at
  generation 1, state Active. Semantic restores 2, presentation restores 1, native post-loads 2, native cast
  requests 0. Retired 0, failed pairs 0, refused transfers 0, save allowed, capital true, mode Default.
  Pair B (`66e2933b…` / `a1923328…`) is unmounted and out of scene, with no owner. The restoration
  observer recorded only `restore-debt-before/after` for the main rider and mount: zero debt,
  `prepared` false, not in combat, 0 dropped, complete. Cleanup: pairs clear, controls settled, save
  custody clear, selection restored.

## Batch C (X5 upgrade/rollback rehearsal): u1 FAIL, u2 and u3 not run

u1 (`-UpgradeRehearsalStep u1-previous-install`, `-UpgradeRehearsalId x5-b199-20261024`) staged the
admitted preview.211 `-b` package (`e89f431a…`) with observer.6 in the isolated base-game tree and loaded
the reference-free NO_MODS owner seed. The observer passed 27/0. Its game record binds the observer result
(`61b1483f…`) and records loaded KMC DLL `11e99309…` / MVID `0aa68ba0-…`, which is exactly preview.211.
The outer result is **FAIL** with one error:

> KMC UMM session log is not an ordinary quiet no-request start: … "errorLines":1 …
> "[KingmakerMountedCombat] [Exception] Horse companion blueprint registration: InvalidOperationException - The exact Ranger companion AllFeatures surface no longer has seven stock options."

The PASS-status game record then differs from the FAIL outer status, so
`Assert-KmcChunk7NormalPlayerRuntimeBinding` and the external reader refuse with "Normal-player run result
does not bind its exact observer result (observer result present=True)." That is a consequence of the
UMM log refusal, not a second defect.

Cause (read-only): preview.211 admits only the seven-option Ranger `AllFeatures` list that Call of the Wild
rewrites. A base-game-only install has the native twelve options. Chunk 7 fixed this in `81c191af`
(see [the Ranger repair](CHUNK7-RANGER-BOOTSTRAP-REPAIR.md)). So the released preview.211 genuinely logs this
exception on a base-game-only install. The X5 design premise "the old mod loads, its log is quiet" does
not hold for this release. u1 cannot pass as designed. This needs an owner or review decision, for example an
exact admitted preview.211 registration line in u1/u3, or documenting the limitation instead. Nothing was
adapted. u2 and u3 require a passed u1 of the same rehearsal id, so they were not launched. u1's records
are retained at the lab root.

## Lab notes

- No `git config` was run against any worktree; `core.autocrlf` stayed `true`.
- Final state: no Kingmaker process, no lock, Mods/saves/profile restored after every run.

## Next

1. Decide the X5 u1 premise for preview.211's base-game-only registration exception (owner/review), then
   pilot u1 → u2 → u3.
2. Exact-package 7E qualification of the rows that pass in development (58, 59 and the chain).
