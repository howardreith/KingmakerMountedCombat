# Chunk 7 batch205: 7E development pilots of the five combat cases on the corrected staged reader

Status: **development pilots; not 7E qualification. Every run restored.** Five native launches on one frozen
candidate (`704904fe`), with no startup failure and no retry. **All five cases passed in game and in the external
reader:** `owner-seed-staged`, `owner-seed-native-death-cleanup`, `owner-seed-attack-modes`,
`owner-seed-combat-reaction` and `owner-seed-realtime-charge` (k4). This is the first staged run read by the corrected
reader (`fa68c97d`, `bc3a2b27`). Nothing was adapted. The standing mission remains IN PROGRESS.

## Candidate (development pilot)

The product DLL did not change after b204. A new candidate was still needed: `Assert-KmcPackageManifest` requires the
package manifest's commit to equal the harness HEAD. The b204 package binds `36945a81`, and the corrected reader is
at `704904fe`. So the package and suite were rebuilt from `704904fe`. The rebuilt DLL is byte-identical to b204's.

| Item | Identity |
| --- | --- |
| Source | `704904fe` (reader fixes `fa68c97d`, `bc3a2b27`, record `704904fe`; product source unchanged since `36945a81`). Frozen worktree `worktrees/chunk7-7e-b205-source-704904fe` (detached, LF re-materialized with per-command `-c core.autocrlf=false`; shared config stays `true`; clean afterwards). Packaged and run from the clean root branch checkout at the same commit |
| Package | `KingmakerMountedCombat-0.1.0-chunk7b-preview.94-c7e-b205-pilots-diagnostic.zip`, `6dac3bf9…`, 3,719,813 bytes; Package.ps1 11/0 (build 147/0) |
| Manifest / DLL / MVID | `6cf97583…` / **`feca06e9…`** / `ac3d08b6-8098-4c3c-9f93-7c2116f3451c`. The DLL and MVID equal b204's. The package hash differs only because the manifest binds a different commit and path |
| Candidate tier | 6/0 (3.88 min; 751/0 inner checks), receipt `b205-candidate-01.log` (`adc7b352…`) |
| Suite | `chunk7e-b205-pilots-20261024`, `60224882…` (save digest `147bbec4…`, Mods digest `ed57b9d2…`, both equal to b204), dry run first |
| Root `bin\Release` DLL | `feca06e9…` before packaging, after the build and after the Candidate tier |
| Helpers | `analysis-cache/chunk7-continuation/b205-*.ps1` and `new-suite-b205.ps1`: the b204 helpers with only the batch, source, package, manifest and suite pins changed. Freeze record `b205-freeze.json` (`36795ad2…`) |

**Selections** (byte for byte the earlier ones). Staged: the b204 selection `d764eb44…`. Death, attack modes, C11
and RT Charge: the b203 selections `a6751b61…`, `b6ffd27e…`, `8ac61e7b…` and `a1f717f8…`. RT Charge reuses the
b201 k1–k3 charge-seed lineage. Every development-scoped WhatIf passed before its launch. Each launch was detached
via WMI `Win32_Process.Create`, with no Kingmaker process and no lock.

## Runs

| Tag | Run | Plan | Game | Outer / reader | Wall time (UTC) |
| --- | --- | --- | --- | --- | --- |
| st | `20261024-c7e-b205-st-owner-seed-staged` | `83364e83…` | **PASS 8/0** (107.6 s, 4,433 frames) | **PASS** / **PASS 33/0, 29/0** | 21:58:29–22:01:16 |
| d1 | `20261024-c7e-b205-d1-owner-seed-native-death-cleanup` | `9092399c…` | **PASS 9/0** (86.3 s, 2,616 frames) | **PASS** / **PASS 33/0, 29/0** | 22:02:14–22:04:36 |
| am | `20261024-c7e-b205-am-owner-seed-attack-modes` | `2b5042a7…` | **PASS 8/0** (92.6 s, 3,267 frames) | **PASS** / **PASS 33/0, 29/0** | 22:05:33–22:08:01 |
| cr | `20261024-c7e-b205-cr-owner-seed-combat-reaction` | `962a6d67…` | **PASS 6/0** (68.3 s, 1,141 frames) | **PASS** / **PASS 33/0, 29/0** | 22:09:09–22:11:11 |
| k4 | `20261024-c7e-b205-k4-chargeseed-rtcharge` | `ec683961…` | **PASS 7/0** (70.7 s, 1,390 frames) | **PASS** / **PASS 33/0, 29/0** | 22:12:08–22:14:12 |

Each run made one Working load request, with no Baseline or unauthorized load.

After every run:
- The outer result reports Mods restored, save protection passed, Working restored, Baseline unchanged and the write
  allowlist passed. The restored save inventory digest is `a4a79262…`, the same as b203 and b204.
- `Params.xml` and PlayerPrefs (including `KingdomDifficulty`) equal the run's own snapshot.
- `Assert-KmcPersistenceProfileUnchanged` passed with 0 admitted changes.
- No Kingmaker process and no lock remained.

### Staged (`owner-seed-staged`)

- **Geometry plan.** 12 candidates were recorded, and **candidate 11 was selected: 8 m at 135°**, target point
  (−14.26, 0.69, −18.92). The refusals match b204: `not-walkable` 4, `plan-infeasible` 4, `placement-rise` 2 and
  `placement-offset` 1. Mount A's start (−19.92, 0.36, −13.26), envelope 16.84 and speed 5.08 also equal b204's.

| Step | Round | Kind | Native outcome |
| --- | --- | --- | --- |
| 0 | 1 | move-short | 2.00 m, matches plan; end 8.25 m |
| 1 | 1 | cast-standard-scroll | admitted scroll window (below) |
| 2 | 1 | move-short | 2.00 m; end 8.49 m |
| 3 | 2 | move-two-moves | 7.00 + 7.00 + 6.98 m; end 7.96 m |
| 4 | 2 | attack-ranged | in-range RiderRanged shot; seat check below |
| 5 | 3 | move-beyond-range | 13.88 m (20 proposals); end 20.30 m |
| 6 | 3 | move-exhaust | 7.00 + 7.00 + 2.69 m; end 21.70 m |
| 7 | 3 | move-probe | 0.00 m |
| 8 | 3 | cast-standard-scroll | admitted scroll window (below) |
| 9 | 4 | attack-ranged-approach | approach shot; 21.70 → 16.87 m |

- No leg was `planAbandoned`. As in b204, only step 0 matched the planned goal exactly. Later legs were re-searched
  from the mount's actual native position.
- **Both scroll windows (steps 1 and 8) have the same 15-event shape:**
  - start, action and concentration events;
  - one sourced `cast-before`/`cast-after` of the scroll;
  - the spell spend, which brackets the item spend of the equipped stack `cd635d57…`;
  - `action-after`;
  - one sourceless `cast-before`/`cast-after`, which is the native sticky-touch delivery;
  - a `heal` of value 0.

  The corrected reader admitted exactly that shape. The scroll stack was 4 requested and equipped; it was released,
  the slot restored and the item disposed.
- **In-range shot seat check (step 4).**
  - Mount A did not move: (−13.838, 0.550, −10.973) before and after.
  - The rider moved from (−13.377, 1.973, −11.046) to (−13.863, 1.973, −11.440), 0.63 m in the plane. Its height
    did not change.
  - The seat radius about the mount pivot was 0.467 m before and after, so the seat rotated under the mount's
    `ForceLookAt`.
  - The target distance was 7.957 m before and after.
  - The reader's seat terms (radius ≤ 0.75 m, same height, a displaced seat facing the target within 5°) passed.

### Death proper (`owner-seed-native-death-cleanup`)

- All eight `deathDetach` conditions were true: observed in the damage round, pair unmounted, activation retired by
  removal, no split release round, survivor pending for the damage round, partner context released, no prepared pair
  residue and grants unchanged (`[2,2,2,2]` → `[2,2,2,2]`).
- The control's `survivorSlot` is 4. `nativeOrderAfterDeath` is the damage-time order without the dead rider, and the
  reader recomputed and accepted it.

### Attack modes (`owner-seed-attack-modes`)

| Leg | Native estimate | Attack | Result |
| --- | --- | --- | --- |
| mounted-off | 1 | single, AB 12 | Success, 1 of 1 |
| mounted-on | 2 | full, AB 10/10 | Success, 2 of 2 |
| unmounted-on | 2 | full, AB 10/10 | `Interrupt`, 2 of 2 |

- The Rapid Shot comparison is off [12], on [10, 10], a bonus of −2.
- The unmounted-on interrupt watch recorded one `Interrupt(bool)` at frame 7073 (round 4, attack index 2 of 2) over
  an acted Success. Its caller was native `UnitAttack.OnTick`. This is the b203 shape, and the reader admitted it.

### C11 reactions (`owner-seed-combat-reaction`)

- The target was waiting on initiative (rider slot 0, target slot 3, wait 3.0). Rider A took the paired Delay, which
  was suspended and then resumed in round 1.
- On resume, the 5.6 m disengagement (Success) drew exactly one native attack: the target's attack on rider A in
  round 1. The target's reactions dropped 1 → 0.
- The prone stand-up half is recorded as uncovered (`proneStandUp.covered=false`), as before.

### RT Charge (`owner-seed-realtime-charge`, k4)

- Placement candidate 5 of 6 (67.5°, 8 m) was selected, the same point as b202 and b203.
- The evidence records Standard 5.96 (`6 − 0.04`). The RT reader requires the exact native shape: only
  `StandardAction = 6 − TimeSinceStart`, with Move and Swift unchanged. It passed.
- The longsword (`571c56d1…`, melee primary) was the same before and after.

## Lab notes

- Launches: 5 (the limit is 5). There were no startup failures, dialogs or recoveries.
- No `git config` was run against any worktree, and the frozen worktree stayed clean.
- Final state: no Kingmaker process and no lock. Mods, saves and profile were restored after every run.
- These passes are development pilot evidence on candidate `704904fe`. They are not 7E qualification, which still
  needs its selected qualification procedure.

## Next

1. All five combat cases now pass on one candidate. The remaining 7E work (F10 breadth, doors, the F11 acceptance
   protocol and private 7E qualification) follows the standing mission. Uncovered halves stay uncovered: C11 prone
   stand-up, a mounted full attack with Rapid Shot off, and BAB/haste variation.
