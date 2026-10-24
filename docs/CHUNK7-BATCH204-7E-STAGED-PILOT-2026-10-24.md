# Chunk 7 batch204: 7E development pilot of the staged whole-plan geometry

Status: **development pilot; not 7E qualification. The run restored.** One native launch of `owner-seed-staged` on
one frozen candidate (`36945a81`). There was no startup failure and no retry.

**The game passed (8/0), and every staged leg executed natively.** The up-front plan selected a placement. The
external reader replayed the geometry plan and accepted it, then FAILed at staged step 1, the round-1 scroll cast:

> Staged step 1 cast-standard-scroll: the native cast failed or duplicated.

Rider A's step-1 cast window holds two `cast-after` events. The reader requires exactly one. Nothing was adapted, and
the standing mission remains IN PROGRESS.

## Candidate (development pilot)

| Item | Identity |
| --- | --- |
| Source | `36945a81` (staged whole-plan geometry `74fdc951`, review fixes `f8380e9c`, docs). Frozen worktree `worktrees/chunk7-7e-b204-source-36945a81` (detached, LF re-materialized with per-command `-c core.autocrlf=false`; shared config stays `true`). Packaged and run from the clean root branch checkout at the same commit |
| Package | `KingmakerMountedCombat-0.1.0-chunk7b-preview.94-c7e-b204-pilots-diagnostic.zip`, `7715bd6d…`, 3,719,813 bytes; Package.ps1 11/0 |
| Manifest / DLL / MVID | `80e6dd8b…` / `feca06e9…` / `ac3d08b6-8098-4c3c-9f93-7c2116f3451c` |
| Candidate tier | 6/0 (5.89 min; 751/0 inner checks), receipt `b204-candidate-01.log` (`a8ee2f25…`) |
| Suite | `chunk7e-b204-pilots-20261024`, `513e07cd…` (save digest `147bbec4…`, Mods digest `ed57b9d2…`, both equal to b203), dry run first |
| Root `bin\Release` DLL | `feca06e9…` before packaging, after the build and after the Candidate tier |
| Helpers | `analysis-cache/chunk7-continuation/b204-*.ps1` and `new-suite-b204.ps1`: the b203 helpers with only batch, source, package, manifest and suite pins changed. Freeze record `b204-freeze.json` (`5f0fe5e2…`) |

**Selection.** The b203 staged selection, byte for byte: `d764eb44…`. Plan `b204-st-plan.json` `ac08d5d6…`. The WhatIf
(development-scoped) passed before the launch. The launch was detached via WMI `Win32_Process.Create`, with no
Kingmaker process and no lock.

## Run

| Run | Game | Outer / reader | Wall time |
| --- | --- | --- | --- |
| `20261024-c7e-b204-st-owner-seed-staged` | **PASS 8/0** (109 s, 4,233 frames, 1 load, 0 writes) | **FAIL** / **FAIL** (same message) | 20:44:53–20:47:45 UTC |

After the run:
- The outer result reports Mods restored, save protection passed, Working restored, Baseline unchanged and the write
  allowlist passed, with restored save inventory digest `a4a79262…` (the same as b203).
- `Params.xml` and PlayerPrefs (including `KingdomDifficulty`) equal the run's own snapshot.
- `Assert-KmcPersistenceProfileUnchanged` passed with 0 admitted changes.
- No Kingmaker process and no lock remained.

### Geometry plan (`owner-seed-staged-geometry-plan-v1`)

- Inputs: envelope 16.84 m (weapon 15.24 m), native speed 5.08, mount corpulence 0.9, planned target corpulence 0.7.
  The placement distances were 8, 9, 7 and 10 m, across 16 angles, with a 0.5 m stop-short margin and at most 8 legs.
- **12 candidates were recorded, and candidate 11 was selected: 8 m at 135°.** Its target point is
  (−14.26, 0.27, −18.92) and its simulation holds all 12 legs through `attack-ranged-approach`.

| Candidates (8 m) | Refusal |
| --- | --- |
| 0° and −22.5° | `placement-rise` |
| 22.5° | `placement-offset` |
| −45°, −67.5°, −90° and −112.5° | `not-walkable` |
| 45°, 67.5°, 90° and 112.5° | `plan-infeasible`: first refusal at step 5 `move-beyond-range`, leg 0, term `no-destination` |
| **135°** | none (**selected**) |

- **Confirmation.** The geometry confirmation passed before input:
  - mount A's start (−19.92, 0.36, −13.26) equals the plan's;
  - the spawned target equals candidate 11's point;
  - the envelope is 16.84 and the speed 5.08;
  - the 12-leg re-simulation has no refusal.
- The external `Assert-KmcChunk7StagedGeometryPlan` replay ran before the step loop and did not throw.

### Native legs (all executed; the game reports `complete`)

| Step | Round | Kind | Outcome |
| --- | --- | --- | --- |
| 0 | 1 | move-short | 2.00 m, matches plan; end 8.25 m |
| 1 | 1 | cast-standard-scroll | `UnitUseAbility` finished Success, one admitted shell; stack 4 → 3; **two rider `cast-after` events** |
| 2 | 1 | move-short | 2.00 m; end 8.49 m |
| 3 | 2 | move-two-moves | 7.00 + 7.00 + 6.98 m; end 7.96 m |
| 4 | 2 | attack-ranged | `MountedPairAttackCommand` Success ("Mounted pair command accepted: RiderRanged") |
| 5 | 3 | move-beyond-range | 13.88 m (20 proposals); end 20.30 m |
| 6 | 3 | move-exhaust | 7.00 + 7.00 + 2.67 m; end 21.71 m |
| 7 | 3 | move-probe | 0.00 m displacement, no carrier |
| 8 | 3 | cast-standard-scroll | `UnitUseAbility` finished Success; two rider `cast-after` events |
| 9 | 4 | attack-ranged-approach | `MountedPairAttackCommand` Success (RiderRanged); start 21.71 m, end 16.89 m |

- No leg recorded `planAbandoned`; every leg found a proposal that continues the plan.
- Only the first leg's destination equals the planned goal (`matchesPlan` true). Every later leg was re-searched from
  the mount's actual native position: `matchesPlan` false, with destinations within about 0.07 m of the planned goal
  at steps 5 and 6.
- No frame-hitch measure is recorded in the staged evidence.

### External reader failure (step 1)

`Assert-KmcChunk7StagedCast` selects every `cast-after` event whose actor is rider A in the step window and requires
exactly one: the scroll ability, with a nonzero process and no spell or arcane failure.

Step 1's window recorded 15 cast events at frame 4641. Among them are two rider-A `cast-after` events:
- the scroll ability `5590652e…`, which sources the equipped stack (`cd635d57…`; item spend 4 → 3);
- right after the spell spend, a second `cast-before`/`cast-after` pair for blueprint `47808d23…`. This one has no
  source item and targets rider A, and it is followed by a `heal` event of value 0.

Both events have a nonzero process and no spell or arcane failure, so the count term is the one that fails. Step 8
has the same shape. The b203 evidence for its step 1 already holds the same `47808d23…` `cast-after`, but
b203 failed in game before the reader reached this check. This is the first staged run whose game passed, so it is
the first time the external reader evaluated a staged cast.

The cause is **not diagnosed here**. It is not attributed to the product or to the reader beyond the recorded events.

## Lab notes

- Launches: 1 (at most 5). There were no startup failures, dialogs or recoveries.
- No `git config` was run against any worktree, and the frozen worktree stayed clean.
- Final state: no Kingmaker process and no lock. Mods, saves and profile were restored.

## Next

1. Diagnose offline, from the recorded cast events, the second rider-A `cast-after` (`47808d23…`, no source item) in
   each scroll-cast window. Decide whether the native scroll cast emits a nested rule cast or whether something
   duplicated. Keep the old reader verdict, and re-evaluate the immutable b204 result only if every needed
   observation exists.
2. The whole-plan geometry selected a feasible placement on Spider Cave, and every native leg completed. The later
   staged reader terms (legs, shots, approach) have not yet been evaluated on native evidence.
