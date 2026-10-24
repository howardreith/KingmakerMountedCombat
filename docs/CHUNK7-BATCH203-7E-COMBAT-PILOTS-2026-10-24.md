# Chunk 7 batch203: 7E development re-pilots of the five X1/X2 combat cases

Status: **development pilots; not 7E qualification. Every run restored.** Five native launches on one frozen
candidate (`2ffacbda`): Batch B 4, Batch C 1. There was no startup failure and no retry. **Four of the five cases
passed in game and in the external reader:**
- death proper
- attack modes
- C11 reactions
- RT Charge

Staged FAILed in game one leg earlier than in b202. It failed at the round-3 `move-beyond-range` leg, which
is the risk CURRENT.md named for `7b183f8b`. Nothing was adapted. The standing mission remains IN PROGRESS.

## Candidate (development pilot)

| Item | Identity |
| --- | --- |
| Source | `2ffacbda` (the batch202 fixes `80ece0c1`, `02fcb8ee`, `7b183f8b`, `69ba2615`, `7243be75`, `a52d0059`, docs). Frozen worktree `worktrees/chunk7-7e-b203-source-2ffacbda` (detached, LF re-materialized with per-command `-c core.autocrlf=false`; shared config stays `true`). Packaged and run from the clean root branch checkout at the same commit |
| Package | `KingmakerMountedCombat-0.1.0-chunk7b-preview.94-c7e-b203-pilots-diagnostic.zip`, `f3063c24…`, 3,714,526 bytes; Package.ps1 11/0 |
| Manifest / DLL / MVID | `377ee31e…` / `4fd2ebba…` / `b47492d7-0988-4396-9743-dc4d443236c9` |
| Candidate tier | 6/0 (3.85 min; 751/0 inner checks), receipt `b203-candidate-01.log` (`9fbdaa3b…`) |
| Suite | `chunk7e-b203-pilots-20261024`, `8de0597d…` (save digest `147bbec4…`, Mods digest `ed57b9d2…`, both equal to b202), dry run first |
| Root `bin\Release` DLL | `4fd2ebba…` before packaging, after the build and after the Candidate tier |
| Helpers | `analysis-cache/chunk7-continuation/b203-*.ps1` and `new-suite-b203.ps1`: the b202 helpers with only batch, source, package, manifest and suite pins changed. Freeze record `b203-freeze.json` (`04b3e375…`) |

**Selections.** Every selection is the b202 selection byte for byte (same SHA-256s). Death `a6751b61…`, attack
modes `b6ffd27e…`, reaction `8ac61e7b…`, staged `d764eb44…`, charge k4 `a1f717f8…`.

**Charge lineage reused (k1–k3 not rerun).** As in b202, the k4 WhatIf passed on this candidate with the b201 lineage
(k2 `20261024-c7e-b201-k2-chargeseed-save`, world evidence `deeb957a…`, `Manual_307` `c0f1e05c…`; k3
`…-k3-chargeseed-load`, persistence `3f6b1071…`). So only k4 ran.

All five WhatIfs passed before the first launch. Each launch was detached via WMI `Win32_Process.Create`, and only
with no Kingmaker process and no lock.

## Runs

| Batch | Run (`20261024-c7e-b203-…`) | Game | Outer / reader | Key evidence |
| --- | --- | --- | --- | --- |
| B | `d1-owner-seed-native-death-cleanup` | PASS 9/0 | **PASS** (33/0, 29/0) | all eight `deathDetach` conditions true; `nativeOrderAfterDeath` = derived order; `survivorSlot` 4 |
| B | `am-owner-seed-attack-modes` | PASS 8/0 | **PASS** (33/0, 29/0) | unmounted-on `Interrupt` (2 of 2) caused natively by `UnitAttack.OnTick`, no KMC frame, chain untruncated |
| B | `cr-owner-seed-combat-reaction` | PASS 6/0 | **PASS** (33/0, 29/0) | paired Delay used; exactly one native AoO |
| B | `st-owner-seed-staged` | FAIL 6/1 | **FAIL** | step 90, `move-beyond-range` leg, 21 proposals, none eligible |
| C | `k4-chargeseed-rtcharge` | PASS 7/0 | **PASS** (33/0, 29/0) | 67.5° 8 m placement; Standard 6 − 0.04 = 5.96, Move/Swift unchanged; longsword unchanged |

After every run, the PASS runs and the FAIL alike:
- The outer result reports Mods restored, save protection passed, Working restored, Baseline unchanged and
  the write allowlist passed, with restored save inventory digest `a4a79262…`.
- `Params.xml` and PlayerPrefs (including `KingdomDifficulty`) equal the run's own snapshot.
- `Assert-KmcPersistenceProfileUnchanged` passed with 0 admitted changes on all five runs.
- No Kingmaker process and no lock remained.

The owner's saves were only read through the registered lab masters.

### Death proper (`owner-seed-native-death-cleanup`): PASS (development pilot)

Contract `native-death-actor-removal-detach-v1`, observed in round 2 (the damage round). All eight conditions are
**true**:
- `observedInDamageRound`
- `pairUnmounted` (pair state Unmounted)
- `activationRetiredByRemoval` (activation identity null, `activationSplit` false)
- `noSplitReleaseRound` (−1)
- `survivorPendingForDamageRound` (pending split mount `e9cd259e`, round 2)
- `partnerContextReleased`
- `noPreparedPairResidue`
- `grantsUnchanged` ([2,2,2,2] → [2,2,2,2])

The orders match:
- The damage-time order is `73681ef2, fd213210, 5dd7bd1d, d9b8281b, 8265f8cf, e9cd259e, e3a3be8b`.
- The control contract `unmounted-dead-native-turn-control-v2` recorded `nativeOrderAfterDeath`
  `73681ef2, 5dd7bd1d, d9b8281b, 8265f8cf, e9cd259e, e3a3be8b`. That is exactly the damage-time order minus the
  dead rider `fd213210`.
- The survivor mount B `e9cd259e` is at slot 4, which equals the recorded `survivorSlot` 4.

The control was `5dd7bd1d` (slot 1; candidates `5dd7bd1d` slot 1 and `8265f8cf` slot 3). It was killed by the same
route (request 385, native damage 77). Its round-3 turn recorded 0 events, Prepare, ends and forced ends. It was dead
and out of combat, grant 1 → 1.

### Attack modes (`owner-seed-attack-modes`): PASS (development pilot)

| Leg | Round | Mounted | Native estimate → mode | Rapid Shot (isOn/buff/running) | Command | Attacks | Attack bonus |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `mounted-off` | 1 | yes | 1 → single (cursor false) | off/off/off | Success | 1 of plan 1 | 12 |
| `mounted-on` | 2 | yes | 2 → full (cursor true) | on/on/on | Success | 2 of plan 2 | 10/10 |
| `unmounted-on` | 4 | no | 2 → full (cursor true) | on/on/on | **Interrupt** | 2 of plan 2 | 10/10 |

- Every plan entry is rider A's own ranged weapon `4de4658c…` (15.24 m), not natural, with iterative penalty 0.
  BAB is 4 in every roll. The Rapid Shot comparison is off [12], on [10, 10], bonus −2.
- **Interrupt watch** (contract v2, watched from the click). On the mounted legs, no `Interrupt(bool)` was
  recorded. The unmounted-on leg recorded exactly one:
  - It came at frame 7054, one frame after the second shot (frame 7053), round 4.
  - Its prior state was result Success, acted true, finished false, `timeSinceStart` 3.09 s, attack index 2 of 2
    planned. `interruptAsSoonAsPossible` was false, the executor could act, and the target was in state and alive.
  - Caller chain: `UnitCommand.Interrupt_Patch3` < **`Kingmaker.UnitLogic.Commands.UnitAttack.OnTick`** <
    `UnitCommand.Tick_Patch1` < `UnitActionController.TickCommand` < `TickOnUnit` < `BaseUnitController.TickUnit` <
    `Tick` < `GameMode.Tick` < `Game.Tick` < `Runner.Update`.
  - `callersTruncated` was false. **No KingmakerMountedCombat frame** is on the chain. The two `_Patch` entries are
    the Harmony-patched native methods themselves.
  - The reader admitted it as one native overwrite of an acted Success after the last shot. As stated in CURRENT,
    this rules out a KMC caller on the stack, not every KMC state influence.

### Reactions (`owner-seed-combat-reaction`): PASS (development pilot)

- **The paired Delay was used.** In round 1 the target `2abc46ff` was still waiting on initiative
  (`canActInCombat` false, initiative cooldown 2.0). Rider A (slot 0) took one native same-round Delay behind it
  (target slot 2, `canDelay` true, status Delayed, suspended). The gate held at the precondition: one reaction per
  round, `canActInCombat` true, not waiting.
- The disengagement was one ordinary ground order of 5.6 m (five-foot-step and single-action modes off). It ended in
  Success, with a clear distance of 7.6 m.
- **The AoO was observed.** The rule capture holds exactly one attack: the target's native attack of opportunity
  on rider A in round 1, with natural melee weapon `5d7d23f5…`. The target's reactions dropped 1 → 0.
- The prone stand-up half stays **uncovered** (`proneStandUp.covered=false`).

### Staged moving actions (`owner-seed-staged`): FAIL

> Step 90, action 0: System.InvalidOperationException: No walkable native destination satisfied the declared geometry (21 proposals recorded). (`NavmeshDestinationRefusing` ← `IssueStagedLeg` ← `IssueStagedStep` ← `TickStaged`)

Steps 0–4 were issued as in b202:
- round 1: move, scroll cast, move (end 8.49 m);
- round 2: two moves (end 13.36 m) and the ranged attack (RiderRanged accepted).

Step 5 is `move-beyond-range` in round 3 (envelope 16.84 m). Rider A started at (−23.55, 1.49, −17.63) with move
0 and was refused. Its recorded proposals:

| Length (m) | Angles | Refusal |
| --- | --- | --- |
| 6.48 | 0, 22.5, 45, 67.5 | not walkable (residual = leg) |
| 6.48 | −22.5 (residual 0.12), −45, −67.5 (residual 0) | `below-band` |
| 8.48 | 0, ±22.5, ±45 (−45 residual 8.24), 67.5 | not walkable |
| 8.48 | −67.5 (residual 0) | `below-band` |
| 10.48 | 0, ±22.5, ±45, 67.5 | not walkable |
| 10.48 | −67.5 (walkable, residual 0.41) | `trace-inexact` |

- 15 proposals were not walkable, 5 were `below-band` and 1 was `trace-inexact`.
  *Correction (offline, October 24): the recorded refusal terms are 16 not walkable, 4 `below-band` and 1
  `trace-inexact`; the table above is right and this count was wrong.*
- No proposal reached the lookahead term. None was `geometryAccepted` or eligible.
- The 6.48 m base length equals the aim (envelope + 3 m = 19.84 m) minus the 13.36 m start distance.
- This is the risk CURRENT.md recorded for `7b183f8b`: no walkable band strip on that side of the target from this
  placement. It needs diagnosis before another run. It is not attributed to terrain beyond what the recorded
  proposals show.

### RT Charge (`owner-seed-realtime-charge`): PASS (development pilot)

- **Placement.** Six candidates were recorded, all at 8 m from mount A at (−18.55, 0.78, −10.80). The angles 0,
  22.5, −22.5, 45 and −45 were not walkable. At **67.5° the candidate was walkable with an exact trace and no
  lane blocker, and was selected**: (−11.16, 0.66, −7.74). This is the same selection as b202.
- **Cost shape.** For each window, rider A's cost boundary is `UnitUseAbility`, action type Standard, at
  `timeSinceStart` 0.04:

  | Window | Shell | Standard before → after | Move | Swift |
  | --- | --- | --- | --- | --- |
  | `charge-success` | `1302151680` | 0 → **5.96** (6 − 0.04) | 0 → 0 | 0 → 0 |
  | `charge-cancel` | `945658112` | 0 → **5.96** (6 − 0.04) | 0 → 0 | 0 → 0 |

  The paired `MountedPairAttackCommand` boundary in between changed nothing (Standard 4.61 → 4.61).
- **Outcome.** `charge-success` ended Success with one native attack rule (single attack) on target `12b77c79`.
  `charge-cancel` ran, stopped and settled. The native retreat was recorded.
- **Weapon.** `chargeWeapon` before and after: hand set 1, primary `571c56d1…`, Longsword, melee, unchanged.

## Lab notes

- Launches: Batch B 4, Batch C 1 (each at most 5). There were no startup failures, dialogs or recoveries.
- No `git config` was run against any worktree, and the frozen worktree stayed clean.
- Final state: no Kingmaker process and no lock. Mods, saves and profile were restored after every run.

## Next

1. Staged: diagnose the beyond-range refusal from the 21 recorded proposals and the placement. The lookahead was
   never reached. Two equivalent failures without a new observation are not to be repeated.
2. Death proper, attack modes, C11 and RT Charge now each have a passing development pilot on one candidate. This is
   not 7E qualification.
