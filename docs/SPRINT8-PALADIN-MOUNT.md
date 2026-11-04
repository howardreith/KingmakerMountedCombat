# Sprint 8: Paladin Divine Mount (PAL-01)

**SPRINT 8 ENGINEERING COMPLETE - READY FOR REVIEW; OWNER PLAY ACCEPTANCE SEPARATE.** No merge, release or installation is selected.

## Identity

| | |
|---|---|
| Baseline | `main` `6a3f2e88`, the real merge of PR #12 (Sprint 7) |
| Final compiled source | `d60a071b`, candidate sprint8l. Later commits are docs only |
| DLL | SHA-256 `0e9f131993c220b1ec2a61a19e75bf6aea444977cb2c2fb96916106c0f30f12d`, MVID `ba64fc76-9668-4ffa-a3e4-82cd64f3f59d`, 8,826,368 bytes (limit 9 MiB) |
| Package | `KingmakerMountedCombat-0.1.0-chunk7b-preview.100-sprint8l-diagnostic.zip`, SHA-256 `5d7de3071cd65acb66489094305e749a58408e3c17f939e59e758a4be4d33e40` |
| Suite | `sprint8l-d60a071b-20261104` |
| Earlier accepted candidate | sprint8k, `48c5163a`, DLL `b427b592de212b3446513e2191da3459813a1da43bbf21e06c805f74b7e48d8b`, MVID `12260f69-bc02-4da1-b757-83039f3c2a78` (a04, a10) |
## Rules and native contract

Rule source: [Core Rulebook, Paladin, Divine Bond](https://legacy.aonprd.com/corerulebook/classes/paladin.html). Native values are from the installed Kingmaker 2.1.7b assembly and the stock blueprints observed in play.

| Rule (Core Rulebook) | Native contract | Sprint 8 behaviour |
|---|---|---|
| At 5th level the paladin chooses one bond; the form cannot be changed | `PaladinProgression` level 5 grants `WeaponBondFeature` directly. `LevelUpHelper.AddFeatures` adds level-entry features with no prerequisite check | Level 5 holds a native `BlueprintFeatureSelection` offering Weapon Bond or Divine Mount. Reversible, exact slot replacement |
| Weapon: +1 at 5th, +1 per three levels, uses at 5, 9, 13, 17 | `WeaponBondPlus2` to `Plus6`, `WeaponBondAdditionalUse` in the class level entries | Unchanged on the weapon path. On the mount path these entries are not added, so nothing hidden remains |
| Archetypes may replace the bond | `RemoveFeatures` entries; `CanAddArchetype` refuses when a removal has no matching entry | An archetype removing level 5's Weapon Bond now removes the selection instead, so it is still legal and is offered no bond |
| Mount functions as a druid's companion at paladin level | `AddPet` reads `AnimalCompanionRank`; rank to pet level is native `RankToLevel`. Other classes stack ranks. Selections offer `AnimalCompanionEmptyCompanion` when a pet exists | Rank = paladin level: 5 ranks on choosing, then one per paladin level from a Paladin-only progression. Not the Ranger's reduced rate |
| Heavy horse for Medium, pony for Small; dog also suitable | Horse and Riding Dog are the supported additive mounts. No pony exists. A Medium rider cannot ride the Riding Dog | Medium: Horse. Small: Horse or Riding Dog |
| Intelligence at least 6 | Horse and Riding Dog base Intelligence is 2 | A fact on the current pet: +4 Intelligence, reaching 6 for an Intelligence 2 animal. See adaptations |
| Call mount: full-round, 1/day at 5th, +1 per 4 levels (4 at 17th), appears adjacent | `BlueprintAbilityResource`, `IncreaseResourceAmount`, `AbilityResourceLogic`, native `Translocate` | **Call Divine Mount**, a real spell-like full-round ability with those uses. Refused while mounted, or if the mount cannot act |
| 11th: celestial template, magical beast for spells | No native template. `AddDamageResistancePhysical`, `AddDamageResistanceEnergy`, ability and buff components exist | Pet facts: DR 5/evil and resist 10 at 11th, DR 10/evil and resist 15 at 13th (when the rank table reaches 11 HD); Celestial Smite 1/day. See adaptations |
| 15th: SR = paladin level + 11 | `UnitPartSpellResistance` | A pet fact holding SR = current paladin level + 11, refreshed when the paladin levels |
| Mount dies: no new mount for 30 days or a level, -1 attack and damage | Native companions are persistent and recover (`AllowDyingCondition`). There is no summon to refuse | Not modelled. See adaptations |

Declared CRPG adaptations:

- **Persistent companion.** The mount is an ordinary persistent native companion. It is not summoned and it does not vanish. Replacing it uses the native respec.
- **Death.** A fallen mount follows the native companion rule and recovers. The 30-day lockout and the -1 penalty are not modelled, because there is no summon for them to refuse.
- **Intelligence.** A flat +4 rather than a floor at 6, so an animal whose Intelligence was already raised keeps the raise.
- **Celestial template.** Damage reduction, energy resistance and smite are real. Darkvision has no native meaning for party units. The magical-beast reclassification is not applied, so spells that target animals still affect the mount. The template's own spell resistance (CR + 5) is not added; the Divine Bond's own SR at 15th is. The template's values come from memory of the Bestiary, agreeing with the installed Call of the Wild template text. The primary page could not be fetched, so they are not verified against it.
- **Smite.** The mark lasts one day, standing in for "until the mount rests", and one target can be marked at a time.
- **Mixed classes.** A companion from another class becomes the bonded mount: it receives the Intelligence, celestial and spell-resistance facts, whatever animal it is.
- **Size.** No pony exists. A Small paladin chooses the Horse or the Riding Dog, and a Medium paladin does not see the Riding Dog because it could not ride it.

## Behaviour delivered

**Level five.** The native level-up holds open at Paladin 5 until the bond is chosen. Weapon Bond gives exactly the stock feature and later upgrades, and no animal. Divine Mount then asks which mount.

**Mount path.** At each boundary, actual pet levels are native commits, measured in `q01`, with level 5 from the producer `q03`:

| Paladin level | Effective rank | Actual pet level | Calls/day | Pet facts |
|---|---|---|---|---|
| 5 | 5 | 5 | 1 | Intelligence 6, species advancement |
| 8 | 8 | 7 | 1 | |
| 9 | 9 | 8 | 2 | |
| 11 | 11 | 9 | 2 | DR 5/evil, resist 10 ×3, Celestial Smite |
| 13 | 13 | 11 | 3 | DR 10/evil, resist 15 ×3, smite kept |
| 15 | 15 | 12 | 3 | SR 26 |
| 17 | 17 | 14 | 4 | SR 28 |

The pet's level is not automatic in the supported installation: Call of the Wild's `TryLevelUpPet` raises the pet's experience, and the player levels it through the native pet level-up. The row made 13 such commits. Experience alone is not counted: the reader requires the level, and refuses a pet left behind the rank table. A character on the bench does not sync its pet until it rejoins the party, as with any companion.

**Size.** Medium offers Horse only; the Riding Dog is refused by a prerequisite and by the native controller when forced. Small (real Halfling character creation) offers Horse and Riding Dog. Ride, walk and dismount on both; an ordinary hostile click attacks from the Horse.

**Call.** 12 m to 1.6 m for one use. Refused while mounted, and while the mount is down.

**Smite.** Marked evil target: +2 attack at a disclosed Charisma 14 (a horse's Charisma 6 adds nothing) and +14 damage at 14 Hit Dice, one use. Used while mounted in the row.

**Existing characters.** Nothing is rewritten on load. A character built on the stock class keeps its bond, gets no offer of the new choice, no animal, and its later upgrades continue, in a live session and after a cold load. The route to change is the native respec: weapon to mount left one pet and no weapon fact, ability or resource; mount back to weapon left no pet, mount fact or rank; experience was unchanged.

**One companion.** Ranger then Paladin: the stock Dog stays, only the mixed-class choice is offered, ranks stack (1 + 5 = 6), and the bond's facts apply to the Dog. Paladin then Ranger: the Horse stays, ranks stack, and Horse, Riding Dog and stock Dog are refused.

**Archetypes.** Divine Hunter, Iroran Paladin and Sacred Servant replace the bond and stay legal; a Divine Hunter row was offered no bond and has none.

**Compatibility.** Another selection already offering the weapon bond, or a changed level-5 entry at registration, leaves the class untouched and mounted play running. A slot changed later is refused and reported with nothing overwritten, and all leased slots return together. Characters outside the party keep the stock bond, since nobody can choose for them.

**Lifecycle.** A level chosen in full and abandoned leaves the owner and the real pet untouched (the stock `AddFeatureToCompanion` writes through its preview, so the bond uses its own component). Native dismissal removes the owner and the mount with no orphan. A fallen mount returns as the same companion with its facts.

## Failed results and causes

All failed evidence is retained. Reader re-evaluations were made with the current reader, against immutable evidence.

| Run | Candidate | Verdict | Cause | Correction |
|---|---|---|---|---|
| r01, r02 | a, b | FAIL | Row names unregistered; a recalculated preview replaces the controller's state, and the driver held the old one | Registered; state read afresh |
| r03 | c | FAIL, re-evaluated PASS | Reader read a choice log a `JObject` re-parenting had dropped | Reader reads committed facts; log fixed |
| r04 | d | FAIL | Reader demanded a pet during the creation chain. Also found: **smite uses zeroed at the step to 13**, a benched owner's pet stale, no hostile in the row | Smite is its own fact; rows field the owner and spawn a native target |
| r05, r07 | e | FAIL, r05 re-evaluated PASS | Same reader demand; Paladin refused as a second class by the row's alignment | Reader checks owner facts at 5; row picks lawful good |
| r06 | e | FAIL | **Real defect: a hired owner's pet missed the bond's facts until the next level** | Live companion sync applies them |
| r09 | f | FAIL, re-evaluated PASS | Reader strict-mode field lookup | Reader |
| s01 | g | FAIL | Post-save "2 m north" goal ended at a wall from this spawn point | The existing clear-line selector. Threshold unchanged |
| s02 | h | FAIL, re-evaluated PASS | Generic reader demanded Ranger receipts | Case branch |
| a02 | j | FAIL | Fallen-mount step read too early: the mount takes about 450 frames to stand | Row waits, records the reason |
| a09 | k | FAIL | Wrong neighbor: `multi-exploration-manual` assumes pending XP this fixture lacks, and fails before any selection. Not investigated on the baseline | Replaced by `riding-dog-exploration-manual`, a10 |

Inventory in r01 also showed the mixed-class choice lists only stock companions and the KMC companions lacked the stock second-animal refusal, which would have let a second animal be chosen. Both are fixed for all classes.

Independent review found: the weapon filter depended on the lease; a partial lease stayed live; the sync skipped mixed owners; a vacuous archetype check; a permanent smite mark; weapon-path characters missing from the removal scan; SR entries accumulating across loads; duplicate component names. All fixed. The delta review confirmed them closed and found a registration failure's text being erased, no retry after a benefit fault, and a vacuous refusal check, also fixed.

## Evidence

Under `runtime-evidence\`, prefix `20261104-sprint8-`. Every run reports `modsRestored` and `saveProtectionPassed` true.

| Run | Binary | Outcome |
|---|---|---|
| `q01-mount` | sprint8l | Progression table above; call, ride, attack, smite, fallen mount, disabled filter, outside-party bond |
| `q02-dog` | sprint8l | Small rider, both choices, Riding Dog, ride, native dismissal |
| `q03-save`, `q04-cold` | sprint8l | Separate-process cold load: mounted pair rebound on its profile, same pet at rank 5, Intelligence 6; a level-15 owner with greater celestial, smite and SR 26; an old weapon character levelled 7 to 8 with its upgrade |
| `q05-mixed` | sprint8l | Both class orders, cancelled preview, archetype |
| `a04-choice` | sprint8k | Inventory, live slot drift, weapon path, old character, retrain both ways |
| `a10-neighbor` | sprint8k | Ordinary Ranger takes the Riding Dog, mounts and saves (shared companion prerequisite) |

Sprint8k differs from sprint8l in the failure-text order, the sync's retry and pet key, and a reader check. `a04-choice` and `a10-neighbor` were not repeated on sprint8l: the retrain's mount path uses that sync. Those two stay sprint8k evidence and are not relabelled.

Offline, on sprint8l: Candidate, Fast 1047 and 149, focused reader 31, harness 281 and 751.

## Limits, stated for review

- **Driven by controller, not by clicks.** Rows use the native hire, respec and level-up controllers and the builder's commit, not the on-screen phases. How the choice looks on the level-up screen has not been seen.
- **Class table.** The Paladin's class progression panel still lists weapon upgrades, which a mount path never receives.
- **Turn-based.** Call and Smite ran in real time only. Mounted turn-based play is unchanged code but was not re-run with a Paladin.
- **Base game alone.** The fixture always loads Call of the Wild. Stock `AddPet` would level the pet by itself without it; that path has not been seen for any KMC companion.
- **Old characters.** The input is built on the stock class in this DLL with the choice switched off, which matches a pre-Sprint 8 save's shape. No save written by an older DLL was loaded.
- **Respec** ran on a hired companion. The main character was not run; no stock companion is a Paladin.
- **Offline only:** registration refusal when another mod offers the weapon bond, and rollback of a registration fault.
- **Uninstall.** A Paladin past level 5 under this mod holds a KMC reference on either bond, including Weapon Bond. The removal report names it. Respec with the mod enabled does not clear it, so a character made under Sprint 8 ties its save to the mod until retrained with the choice off, which was not exercised.
- **A party member levelled by `AddClassLevels`** rather than by a player would stay without a bond. No stock unit was found to do this, though blueprints were not searched exhaustively.
- **Celestial tiers follow the paladin's level,** via the rank table, not the pet's current level, so an unlevelled pet has them early.
- **Magical beast** is not applied, and no spell-targeting behaviour was tested on the mount.

## UI-01

**Deferred, low priority.** The mount-side control was not started. It would add a control kind to the shared native control service with its own native rows, and the sprint's budget went to the class feature. Sprint 8 closes without it.

## Budget and restoration

Record `sprint8`: **80.65 of 90** minutes, 55 entries, 28 launches, no adjustment. Batches `sprint8:b1` to `b6`, `s1` to `s3`, `accept1` to `accept3`, `confirm1`, `confirm2`.

Live installation at closeout: Mods tree empty, the same as each run's captured start; no game process; no candidate installed; protected saves restored per each run's result. Sprint 7's record is untouched.

## Owner play checklist

1. Install the package. Use a Paladin at level 4, or hire one at the mercenary manager.
2. Level to 5: **Divine Bond** should wait for a choice. Check how it reads.
3. Choose **Divine Mount**, then **Horse** (Medium) or **Horse/Riding Dog** (Halfling or Gnome). The mount appears and is yours.
4. Select the mount and take its level-ups: its portrait should offer them, up to the level the table shows.
5. Ride, walk, attack, dismount. Then **Call Divine Mount** from a distance: one use, and it arrives beside you. It should refuse while mounted.
6. On another character, choose **Weapon Bond**: stock behaviour, no animal.
7. Load an old save with a Paladin above 5: nothing changes, and no choice is offered on the next level.
8. Retrain that Paladin to the mount, then back: one mount, then none.
9. Take a Ranger with a pet to Paladin 5: the choice should offer only the existing companion.
10. If you reach level 11 and 15, check the mount's Celestial Smite, its protections and spell resistance.
