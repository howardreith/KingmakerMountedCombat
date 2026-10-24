# Chunk 7 batch201: 7E development pilots, X1 owner-seed combat and the X2 charge lineage

Status: **development pilots; not 7E qualification. Every run restored exactly.** Eight native
launches on one frozen candidate (`fe5d8e9a`), in two batches of four, with no startup retry. **None of
the five combat cases passed.** Death proper, attack modes and staged actions FAILed on in-game
assertions. Reactions ended in their pre-input "uncovered" refusal. RT Charge was refused at target
placement before any measured step. **The charge seed lineage passed** (cold qualification, Manual
producer and cold consumer). Nothing was adapted. The standing mission remains IN PROGRESS.

## Candidate (development pilot)

| Item | Identity |
| --- | --- |
| Source | `fe5d8e9a` (the charge seed `1157405e`/`41d9266c`, the X1 re-review `be6cabd1`, docs), frozen worktree `worktrees/chunk7-7e-b201-source-fe5d8e9a` (detached, LF re-materialized with per-command `-c core.autocrlf=false`; shared config stays `true`); packaged and run from the clean root branch checkout at the same commit |
| Package | `KingmakerMountedCombat-0.1.0-chunk7b-preview.94-c7e-b201-pilots-diagnostic.zip`, `2c4ddd2a…`, 3,709,368 bytes; Package.ps1 11/0 |
| Manifest / DLL / MVID | `f6f97388…` / `16fc8486…` / `07a13203-592d-4e20-9b0f-0b5dfebb3cbd` |
| Candidate tier | 6/0 (3.75 min; 751/0 inner checks), receipt `b201-candidate-01.log` (`b6d24a9c…`) |
| Suite | `chunk7e-b201-pilots-20261024`, `c8f957f8…` (save digest `147bbec4…`, Mods digest `ed57b9d2…`), dry run first. The save digest differs from b200's `4f6375da…` because the owner's Saved Games now holds `Manual_306_KMC_C7_CHARGE_SEED` |
| Root `bin\Release` DLL | `16fc8486…` before packaging, after the build and after the Candidate tier |
| Helpers | `analysis-cache/chunk7-continuation/b201-*.ps1`: the b199 helpers with only batch, candidate, package and suite pins changed. `b201-launch.ps1` adds a pre-launch check (no game, no lock, a WhatIf log present) and a WMI launch of an already staged plan. Also `new-suite-b201.ps1` and the freeze record `b201-freeze.json` (`99154115…`) |

**Same-candidate source rule (Batch A not needed).** For every combat case, `Get-KmcChunk7OwnedPairsOwnerSeedDerivedSource`
pins the producer receipt to its own recorded candidate. So the row-1 lineage (world26 `b3s1d-save` → `b4s1-load`,
Manual 304 `b4d1ede7…`) is reused as row 57 reused it in b197. All four combat WhatIfs passed on this
candidate (about 0.35 min each), so no row-1 producer or consumer run was needed.

**Selections.** Batch B: `b197-r57-selection.json` with only `case` changed (same source, checkpoint, NO_MODS seed,
roles, party, `baseGameOnly`, 1 load and 0 writes). Batch C, as in CURRENT:
- k2: the world26 `b3s1d-save` request's `ownedPairs`, with only the seed id and hash changed to `KMC_C7_CHARGE_SEED` / `1ec2f321…`. `expectedNativeWriteCount` is 1, rider A is `73681ef2`, and `xpAssistance` targets rider B `fd213210` only.
- k3 and k4: pins taken from k2 and k3.

Every launch passed its development-scoped WhatIf first, and was launched detached via WMI `Win32_Process.Create`
only with no Kingmaker process and no lock.

## Runs

| Batch | Run (`20261024-c7e-b201-…`) | Verdict | Key evidence |
| --- | --- | --- | --- |
| B | `d1-owner-seed-native-death-cleanup` | **FAIL** (game 6/1) | step 70, death-cleanup assertion (below) |
| B | `am-owner-seed-attack-modes` | **FAIL** (game 5/1) | step 80, the first measurement was a single attack, not a full attack |
| B | `cr-owner-seed-combat-reaction` | **FAIL: uncovered refusal before input** (game 5/1) | step 85, the target holds no available attack of opportunity |
| B | `st-owner-seed-staged` | **FAIL** (game 6/1) | step 90, no walkable destination for the round-N+2 exhaustion leg |
| C | `k1-chargeseed-cold` (assisted cold inventory, owner-seed-readonly-inventory) | **PASS** (4/0) | charge seed isolated cold qualification |
| C | `k2-chargeseed-save` (owner-seed-persistence-save, multi-exploration-manual) | **PASS** (5/0) | world evidence `deeb957a…`; `Manual_307_KMC_C7_SEED_WORLD` `c0f1e05c…` (1,270,375 bytes) |
| C | `k3-chargeseed-load` (owner-seed-persistence-load, multi-exploration-manual) | **PASS** (4/0) | persistence evidence `3f6b1071…` |
| C | `k4-chargeseed-rtcharge` (owner-seed-realtime-charge) | **FAIL** (game 4/1) | step 2, target placement refused before any measured step |

After every run (PASS and FAIL alike):
- The outer result reports Mods restored, save protection passed, Working restored, Baseline unchanged
  and the write allowlist passed, with restored save inventory digest `a4a79262…`.
- `Params.xml` and PlayerPrefs (including `KingdomDifficulty`) equal the run's own snapshot, and
  `Assert-KmcPersistenceProfileUnchanged` passed with 0 admitted changes.
- No Kingmaker process and no lock remained.
- The external `Test-RuntimeResult` read all eight results without error (33/0 and 29/0).

The owner's `Manual_306_KMC_C7_CHARGE_SEED` was only read through the registered lab master. Save protection passed on every run.

### Death proper (`owner-seed-native-death-cleanup`): FAIL

> Step 70, action 0: Native death cleanup changed the original split round, retained presentation or granted preparation.

This is `Chunk7CombatNativeLife.cs` line 439. It is one combined `Require` over five conditions: pair B Unmounted,
`PairedActivationSplit`, `SplitReleaseRound == lifeDamageRound`, `NoPreparedPairResidue`, and grant counts
unchanged. The conditions are not recorded separately. The steps before it passed: the stock
Tick → SetLifeState → OnUnitDeath chain, the healthy-pair checks and the ledger check. Observed:
- Damage round 2. Rider B `fd213210` went Conscious → Dead (one transition).
- Deliveries:
  - seq 18 `CombatEnded` (`HandleUnitLeaveCombat`), Mounted → Mounted, no cleanup.
  - seq 19 `UnitDeath`, Mounted → Unmounted, cleanup Death succeeded.
  - seq 20 `UnitIncapacitated`, idempotent from Unmounted.
- `riderDamageRoundForceToEnd` 1.
- Right after the damage, the allocation trace shows `combat-clear-before/after` and `clear-before/after`, then
  `remove-unit-before`, forced end and turn end, all in frame 5350.
- The later cleanup record shows pair B Unmounted, owner released, `noPreparedPairResidue` true and nothing
  unsettled.

So the remaining candidates are the split-round state (`PairedActivationSplit` or its release round) and the
grant counts. The native death delivered a `CombatEnded` leave-combat, and a combat clear followed. Which
conjunct failed is not recorded, and nothing here establishes the cause.

### Attack modes (`owner-seed-attack-modes`): FAIL

> Step 80, action 0: Native full attack escaped its exact plan, rolls or actor allocation: {"identity":332267520,"executor":"73681ef2-…","target":"bc1d01c0-…","fullAttack":false,"singleAttack":false,"result":"Success","completedAttacks":1,"plan":[{"index":0,"weapon":"4de4658c…","ranged":true,"natural":false,"owner":"73681ef2-…","iterativePenalty":0}]}

The first measurement was `mounted-off` (round 1, pair Mounted).
- Rapid Shot was switched off natively out of combat (isOn true → false) and was not running.
- The unit click produced the product's stock dispatch `action=RiderRanged` (riderStandard 0, mountStandard 0).
- Native recorded one ranged roll and attack: `fullAttack=False`, `attacksCount=1`, attack bonus 12, iterative penalty 0.
- The case requires a native full attack. A mounted rider's click is routed to the paired RiderRanged command,
  which makes one ranged attack. No Rapid Shot-on measurement ran.

### Reactions (`owner-seed-combat-reaction`): pre-input uncovered refusal

> Step 85, action 0: The diagnostic target has no single available native attack of opportunity; C11 disengagement is uncovered on this fixture: {"label":"precondition","id":"9d60fcdb-…","round":1,"reactions":1,"reactionsPerRound":1,"canAttackOfOpportunity":true,"canActInCombat":false,"waitingInitiative":true,"initiativeCooldown":3.0,"surprised":false,"canAct":true,"threatHand":true,"riderRemembersTarget":true,"mountRemembersTarget":true,…,"engagesRider":false,"engagesMount":false,"engagedUnits":[],"mountDistance":3.5,…}

The gates that failed are `canActInCombat` false and `waitingInitiative` true (initiative cooldown 3.0), in round 1. By the
case design this records C11 disengagement as **uncovered** on this fixture and geometry. No ground order was issued.

### Staged moving actions (`owner-seed-staged`): FAIL

> Step 90, action 0: No walkable native destination satisfied the declared geometry. (`NavmeshDestination` ← `IssueStagedLeg` ← `IssueStagedStep` ← `TickStaged`)

Steps 0–5 were issued:
- Round N, `C6D-move-cast-move`: a leg, the scroll cast on rider A (native shell Success), then a leg.
- Round N+1, `C6D-double-move-ranged`: two moves, then the ranged attack.
- Round N+2: the `move-beyond-range` leg.

Step 6, `move-exhaust` (round 3), found no navmesh-traced destination for its leg from (-22.95, 2.40, -24.72).
The movement-exhaustion refusal, the next-turn scroll control and the beyond-range approach shot did not run.
This is a geometry failure inside the run, not the case's declared uncovered half.

### RT Charge (`owner-seed-realtime-charge`): FAIL before any measured step

> Step 2, action 0: Diagnostic target placement refused before any measured step: Diagnostic target placement is not on a walkable native node. (`DiagnosticCombatTargetService.ValidatePlacement` ← `AdmitTargetAuthorization` ← `PrepareEncounter`)

- The mounted start was restored from the cold load: phase `all-pair-presentation-restored`, 4 semantic and 2
  presentation restores, 0 native casts, 0 Mount inputs, loaded equal to the producer read-back.
- Rider A was at (-18.09, 2.20, -10.75). The proposed target position (-18.55, 0.78, -2.80) is not on a walkable node.
- The target authorization binds seed `1ec2f321…`, derived Manual `c0f1e05c…`, producer k2 and consumer k3.
- No charge input was issued, and the `chargeWeapon` observation was not reached. The area is Spider Cave (`b10e8651…`),
  the same as the NO_MODS seed. Whether the charge seed's saved start position differs was not compared.

### Charge seed lineage: PASS

- k1: the seed's isolated cold qualification passed (4/0).
- k2: the producer loaded the registered master, set up both Horses natively and wrote `Manual_307_KMC_C7_SEED_WORLD` (`c0f1e05c…`). Its two owners:
  - Main Ranger `73681ef2` with Horse `da5b6ded-9bd7-497f-b679-62f9b40eab2e`.
  - Rider B `fd213210` with Horse `18889ac1-5db7-44a8-9444-cdebee5dacb9`.
  - Field party: the four characters plus both Horses.
- k3: a separate process cold-loaded that Manual and passed (4/0).

## Lab notes

- Launches: Batch B 4, Batch C 4 (each at most 5). No Steamworks startup failure, no dialogs, no recovery.
- No `git config` was run against any worktree, and the frozen worktree stayed clean.
- Final state: no Kingmaker process and no lock. Mods, saves and profile were restored after every run.

## Next

Each of the following needs diagnosis or a changed experiment before another run. Two equivalent failures without a new
observation are not to be repeated.
1. Death proper: record each conjunct of the line-439 check separately, then decide whether native combat end on the
   subject's death is the expected shape.
2. Attack modes: a mounted rider's unit click is a paired RiderRanged single attack. The case's "native full attack"
   premise needs a design decision.
3. Staged: geometry for the exhaustion leg at round N+2 on this start.
4. RT Charge: target placement on the charge seed's start (the realtime engine's placement rule on this start).
5. C11 is recorded as uncovered on this fixture (the target is waiting on initiative and cannot act in combat in round 1).
