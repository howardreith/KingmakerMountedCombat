# Chunk 7 batch202: 7E development re-pilots of the five changed X1/X2 combat cases

Status: **development pilots; not 7E qualification. Every run restored.** Six native launches on one frozen
candidate (`7aaf0dfb`): Batch B 4, Batch C 2. Batch C's second launch was the one permitted retry after the
known Steamworks startup failure. **C11 reactions passed** for the first time (paired Delay used, exactly
one native attack of opportunity). Death proper and attack modes passed in game (9/0, 8/0) but the external
reader failed each. Staged FAILed in game at the same exhaustion leg, now with its proposals recorded. RT
Charge executed with the longsword in game (7/0), but the reader failed its cost check. Nothing was
adapted. The standing mission remains IN PROGRESS.

## Candidate (development pilot)

| Item | Identity |
| --- | --- |
| Source | `7aaf0dfb` (the batch201 fixes `fddddea8`, `4ec7b172`, `1fb6c2a5`, `ff25b49f`, `ef2dc138`, `708c876c`, docs). Frozen worktree `worktrees/chunk7-7e-b202-source-7aaf0dfb` (detached, LF re-materialized with per-command `-c core.autocrlf=false`; shared config stays `true`). Packaged and run from the clean root branch checkout at the same commit |
| Package | `KingmakerMountedCombat-0.1.0-chunk7b-preview.94-c7e-b202-pilots-diagnostic.zip`, `1d06a143…`, 3,712,588 bytes; Package.ps1 11/0 |
| Manifest / DLL / MVID | `48a2fd49…` / `b26bbcd6…` / `2e6d6438-adc0-4a45-b7ec-9e5d5928438f` |
| Candidate tier | 6/0 (3.76 min; 751/0 inner checks), receipt `b202-candidate-01.log` (`23bcbfe0…`) |
| Suite | `chunk7e-b202-pilots-20261024`, `08c55087…` (save digest `147bbec4…`, Mods digest `ed57b9d2…`, both equal to b201), dry run first |
| Root `bin\Release` DLL | `b26bbcd6…` before packaging, after the build and after the Candidate tier |
| Helpers | `analysis-cache/chunk7-continuation/b202-*.ps1` and `new-suite-b202.ps1`: the b201 helpers with only batch, package, manifest and suite pins changed. Freeze record `b202-freeze.json` (`d55c21b6…`) |

**Selections.** Batch B: the b201 selections byte for byte (same SHA-256s; `b197-r57-selection.json` with only
`case` changed). Batch C: the b201 k4 selection byte for byte (`a1f717f8…`).

**Charge lineage reused (k1–k3 not rerun).** `owner-seed-realtime-charge` is in the combat list of
`Get-KmcChunk7OwnedPairsOwnerSeedDerivedSource`, so its producer receipt is pinned to its own recorded candidate.
The k4 WhatIf passed on this candidate with the b201 lineage (k2 `20261024-c7e-b201-k2-chargeseed-save`, world
evidence `deeb957a…`, `Manual_307` `c0f1e05c…`; k3 `…-k3-chargeseed-load`, persistence `3f6b1071…`). So only k4 ran.

Every launch passed its development-scoped WhatIf first. Each was launched detached via WMI `Win32_Process.Create`
only with no Kingmaker process and no lock.

## Runs

| Batch | Run (`20261024-c7e-b202-…`) | Game | Outer / reader | Key evidence |
| --- | --- | --- | --- | --- |
| B | `d1-owner-seed-native-death-cleanup` | PASS 9/0 | **FAIL**: "Unmounted death control contract differs." | all eight `deathDetach` conditions true; control `survivorSlot` 4 vs 5 derived from the recorded order |
| B | `am-owner-seed-attack-modes` | PASS 8/0 | **FAIL**: "Attack mode interrupt claims a completed plan." | unmounted-on leg: `Interrupt` with 2 of 2 planned attacks completed |
| B | `cr-owner-seed-combat-reaction` | PASS 6/0 | **PASS** (6/0; reader 33/0 and 29/0) | paired Delay used; exactly one native AoO |
| B | `st-owner-seed-staged` | FAIL 6/1 | **FAIL** | step 90, exhaustion leg, 48 proposals recorded, none accepted |
| C | `k4-chargeseed-rtcharge` | none | **FAIL**: "Kingmaker exited before committing its atomic game result." | Steamworks `DllNotFoundException: steam_api64` in `SteamManager.Awake` (known startup failure) |
| C | `k4r-chargeseed-rtcharge` (the one retry) | PASS 7/0 | **FAIL**: "Native RT expenditure was not applied at its exact callback." | charge executed (Success, longsword); the full-round move expectation was not met |

After every run (PASS and FAIL alike):
- The outer result reports Mods restored, save protection passed, Working restored, Baseline unchanged and
  the write allowlist passed, with restored save inventory digest `a4a79262…`.
- `Params.xml` and PlayerPrefs (including `KingdomDifficulty`) equal the run's own snapshot.
- `Assert-KmcPersistenceProfileUnchanged` passed. It admitted 0 changes on five runs. On k4r it admitted
  3 native achievement-cache rewrites (`achievements.dat*`, 12,288 bytes each, policy
  `native-achievement-cache-settled-size`, `profileBytesRestoredExactly` false).
- No Kingmaker process and no lock remained.

The owner's `Manual_306_KMC_C7_CHARGE_SEED` was only read through the registered lab master.

### Death proper (`owner-seed-native-death-cleanup`): game PASS, reader FAIL

> Unmounted death control contract differs. (`Chunk7NativeLifeEvidence.ps1` line 353)

The eight recorded `nativeLife.deathDetach` conditions (contract `native-death-actor-removal-detach-v1`, observed in round 2 = damage round 2) are all **true**:

| Condition | Value |
| --- | --- |
| `observedInDamageRound` | true |
| `pairUnmounted` | true (pair state Unmounted) |
| `activationRetiredByRemoval` | true (activation identity null, `activationSplit` false) |
| `noSplitReleaseRound` | true (`splitReleaseRound` −1) |
| `survivorPendingForDamageRound` | true (pending split mount `e9cd259e`, round 2) |
| `partnerContextReleased` | true (partner context actor null) |
| `noPreparedPairResidue` | true |
| `grantsUnchanged` | true (`grantsBefore` = `grantsAfter` = [2,2,2,2]) |

The reader's earlier death checks passed: the delivery chain, the N+1 order and grants, and the dead rider's
turn. The run then failed at the unmounted control, where the reader requires
`deathControl.survivorSlot` == the survivor slot it derives from `nativeLife.nativeNextRoundOrder`. Observed:
- The producer recorded `survivorSlot` 4.
- The recorded next-round order is `73681ef2, fd213210, 8265f8cf, 29f3c260, 5dd7bd1d, e9cd259e, e3a3be8b`. It
  still lists the dead rider `fd213210` at index 1 and puts mount B `e9cd259e` at index 5.
- The control candidates `8265f8cf` (slot 1) and `5dd7bd1d` (slot 3) also sit one below their indexes in that
  recorded order (2 and 4). This is consistent with the producer reading an order without the dead rider, but
  that is not established.
- The control itself: `8265f8cf` (not main, mortal) was killed by the same route (request 250, native damage
  50). Its round-3 turn recorded 0 events, Prepare, ends or forced ends; dead, out of combat, grant 1 → 1.

### Attack modes (`owner-seed-attack-modes`): game PASS, reader FAIL

> Attack mode interrupt claims a completed plan. (`Chunk7CombatEvidence.ps1` line 221)

| Leg | Round | Mounted | Native estimate → mode | Rapid Shot (isOn/buff/running) | Command | Attacks | Attack bonus |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `mounted-off` | 1 | yes | 1 → single (cursor false) | off/off/off | Success | 1 of plan 1 | 12 |
| `mounted-on` | 2 | yes | 2 → full (cursor true) | on/on/on | Success | 2 of plan 2 | 10/10 (Rapid Shot −2 each) |
| `unmounted-on` | 4 | no | 2 → full (cursor true) | on/on/on | **Interrupt** | 2 of plan 2 | 10/10 (Rapid Shot −2 each) |

- Every plan entry is rider A's own ranged weapon `4de4658c…` (range 15.24 m), not natural, with iterative penalty 0.
- One roll and one attack rule were recorded per planned attack.
- The Rapid Shot comparison is off [12], on [10, 10], bonus −2.
- The reader refuses a native `Interrupt` result whose completed attacks reach the plan size. The unmounted
  control finished both planned shots and still ended as `Interrupt`.

### Reactions (`owner-seed-combat-reaction`): PASS (development pilot)

- **The paired Delay was used.** In round 1 the target `d25f171b` was still waiting on initiative
  (`canActInCombat` false, initiative cooldown 4.0). Rider A (slot 0) took one native same-round Delay behind it
  (slot 4, `canDelay` true; `suspended` and `resumed` both true). It was measured on resume in round 1, when the
  gate held: one reaction per round, `canActInCombat` true, not waiting.
- The approach drew no attack. The disengagement was one ordinary ground order (5.6 m, five-foot-step and
  single-action modes off, result Success).
- **The AoO was observed.** The rule capture holds exactly one attack: the target's native attack of opportunity
  on rider A, a natural melee weapon `5d7d23f5…`, in round 1. The target's reactions dropped 1 → 0.
- The prone stand-up half stays **uncovered** (`proneStandUp.covered=false`).

### Staged moving actions (`owner-seed-staged`): FAIL

> Step 90, action 0: No walkable native destination satisfied the declared geometry (48 proposals recorded). (`NavmeshDestination` ← `IssueStagedLeg` ← `IssueStagedStep` ← `TickStaged`)

Steps 0–5 were issued as in b201: round 1 move, scroll cast, move; round 2 two moves and the ranged attack;
round 3 the `move-beyond-range` leg. Step 6, `move-exhaust` (round 3), started from rider A at
(−22.95, 2.40, −24.72) with no move left. Its recorded proposals:
- 16 angles (0, ±22.5 … 180) at each of 7, 5 and 3.5 m.
- 14 were walkable with an exact native trace (residual 0): at 7 m the angles 90, 112.5, 135 and 157.5; at 5 m
  and 3.5 m the angles 67.5 to 157.5. **None was `geometryAccepted`.**
- The other 34 were not walkable (trace residual equal to the leg length).
- A proposal records only `geometryAccepted`, not which band condition refused it.

### RT Charge (`owner-seed-realtime-charge`): retry game PASS, reader FAIL

The first launch hit the known Steamworks startup failure: `[Steamworks.NET] Could not load [lib]steam_api.dll`,
then `System.DllNotFoundException: steam_api64` at `SteamManager.Awake`. Kingmaker exited before its result,
and the run restored. The Unity log is kept as `b202-k4-unity-output_log.txt`. One retry was launched under a
fresh run id with the same selection (`k4r`).

- **Placement.** Six candidates were recorded, all at 8 m from mount A at (−18.55, 0.78, −10.80). The angles 0,
  22.5, −22.5, 45 and −45 were not walkable. At **67.5° the candidate was walkable with an exact trace and no
  lane blocker, and was selected**: (−11.16, 0.66, −7.74).
- **Charge executed with the longsword.** The `charge-success` window: rider A's native shell
  (`d79eaec2…`, cast delta 1, refusal 0) ended in Success with one native attack rule on target `c19c3308`
  (single attack). `chargeWeapon` before and after: hand set 1, primary `571c56d1…`, Longsword, melee,
  unchanged. The `charge-cancel` window also ran and settled. The native retreat was recorded.
- **Failure.**

  > Native RT expenditure was not applied at its exact callback. (`Chunk7RealtimeEvidence.ps1` line 37, called with `-FullRound` for the charge at line 208)

  Both windows' cost boundaries are `UnitUseAbility`, action type Standard: before standard 0, move 0; after
  standard 5.96, move 0. The full-round check also requires the move cooldown to grow, and it stayed 0.

## Lab notes

- Launches: Batch B 4, Batch C 2 (each at most 5). One Steamworks startup failure, retried once. No dialogs, no recovery.
- No `git config` was run against any worktree, and the frozen worktree stayed clean.
- Final state: no Kingmaker process and no lock. Mods, saves and profile were restored after every run.

## Next

Each item needs diagnosis before another run. Two equivalent failures without a new observation are not to be
repeated.
1. Death proper: the control's `survivorSlot` (4) differs from the recorded order's (5). Decide which order the
   control and the reader should share; the order still lists the dead rider.
2. Attack modes: the native unmounted full attack completed both planned shots but reported `Interrupt`. Decide
   whether that native shape is admissible (cf. the native ranged full-attack melee tail).
3. Staged: 14 walkable exact-trace proposals were refused by the band geometry. The refusing condition is not
   recorded.
4. RT Charge: the native charge shell spent only the Standard cooldown (5.96) and no move. The reader's
   full-round expectation needs review against this native cost shape.
5. C11 disengagement now has a passing development pilot; the prone half stays uncovered.
