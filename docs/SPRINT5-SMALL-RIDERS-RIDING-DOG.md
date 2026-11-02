# Sprint 5: Small riders, relative size and the Riding Dog

**Status: engineering complete for the default-build Halfling and Gnome rigs, ready for review. Not the full Sprint 5
completion label.** The fat and thin build presets (eight more skeletons) are refused with feedback and not supported. They
need one more validated pass, and the budget has no launch left for it. Owner play acceptance is separate. No merge, release or
installation is part of this work.

| | |
| --- | --- |
| Baseline | `main` `e8450a56`, the merge of Sprint 4 PR #9 |
| Branch | `codex/mounted-combat-sprint5-small-riders-riding-dog` |
| Final compiled candidate | sprint5o, commit `992fe68e`, DLL `ad3ee75eded0f54d…`, MVID `0bad1c69-23eb-424c-82b7-4e7abb8ac51a`, 8,598,016 bytes |
| Package bound | Unchanged at 9 MiB. No increase was needed |
| Budget | `sprint5`: see Costs |

## Evidence, by binary

Only the Riding Dog's advancement level literal differs between the two DLLs in the table.

| Run | DLL | Game | Outer reader | What it shows |
| --- | --- | --- | --- | --- |
| `20261102-sprint5-f6-dog` | `ad3ee75e` (sprint5o) | PASS 11/11 | PASS | Riding Dog: selection, all four rigs, actions, Enlarge, rank 4, stock Dog, replacement |
| `20261102-sprint5-f2-horse` | `7ef02e56` (sprint5n) | PASS 6/6 | PASS | Horse: selection, all four rigs, actions |
| `20261102-sprint5-f3-save` | `7ef02e56` | PASS | PASS | Native Manual `8e42b8ed…` holding the Medium/Mammoth pair and `small-halfling-female-ridingdog-v1` |
| `20261102-sprint5-f4-cold` | `7ef02e56` | PASS | PASS | Separate process (PID 12800) cold-loads that file, then dismount and remount |
| `20261102-sprint5-f1-dog` | `7ef02e56` | PASS 11/11 | Original FAIL, reader-only re-evaluation PASS | Same Dog rows as f6 on the f2 to f4 binary |
| `20261102-sprint5-f5-base` | `7ef02e56` | FAIL, expected | FAIL, recovered | Base game, no Call of the Wild. Registration only: the Working fixture cannot load without that mod |

The save pair `f3`/`f4` and the Horse run `f2` were not repeated on sprint5o. They are not claimed for that exact binary.

## POSE-01: the Halfling in the Horse

**First cause.** The batch70 Small seat was calculated to put the feet on the Horse's stirrups. Small legs reach about 0.35,
while the stirrups hang about 0.75 below the saddle, so meeting them pulled the pelvis about 0.59 below the accepted Medium seat:
mount-local height 1.63 against 2.22, with the feet 0.015 from the stirrups in retained b197 evidence. The fit numbers looked
excellent because they measured the wrong thing. Baked Horse mesh slices put the saddle surface near 1.82, so that pelvis was inside the
Horse.

**Second cause, found in r02.** The seat also inherited the rider's animation pose at the moment of mounting, so it differed
between rigs and would differ between remounts and reloads.

**Repair** ([seat record](SPRINT5-SMALL-SEATS.json)):

- A Small seat is a fixed offset from the mount's anchor bone, set from the measured back. Mechanics positions, reach, collisions,
  navigation and actions do not change.
- Legs straddle the back, sized by each rig's own thigh and shin reach, instead of reaching for stirrups.
- Gear and cosmetics no longer take part in admission. The exact-costume check stays only for the historical F09 diagnostics.
- Scale: a rider parented with its world transform kept is already compensated, so only later mount growth is undone. The first
  version shrank the rider; the independent review caught that before any launch.
- The medium path is unchanged.

**One visual correction** after r12: the Halfling's near thigh was lost in the dog's fur mid-stride, so the dog seat rose 0.03 and
0.02 and the feet spread from 0.61 to 0.68 of reach.

### Supported matrix

| Rig (default-build preset) | Horse profile | Riding Dog profile | Viewed in game |
| --- | --- | --- | --- |
| Male Halfling | `small-default-halfling-horse-v1` | `small-halfling-male-ridingdog-v1` | Both |
| Female Halfling | `small-halfling-female-horse-v1` | `small-halfling-female-ridingdog-v1` | Both |
| Male Gnome | `small-gnome-male-horse-v1` | `small-gnome-male-ridingdog-v1` | Both |
| Female Gnome | `small-gnome-female-horse-v1` | `small-gnome-female-ridingdog-v1` | Both |

**Not supported, and refused with feedback:**

- Fat and thin build presets (`MaleHalfling_fat`, `FemaleGnome_thin` and the rest). These are ordinary character-creation choices.
  Their refusal comes from the exact skeleton name and is tested offline; no fat or thin body was viewed in game.
- Small riders on the Mammoth: no measured seat.
- Medium riders on the Riding Dog: too large.
- A rider currently resized away from its native size, or any polymorph.

**Left-handed riders are unproven.** The mirrored leg handling is in the code and admission allows a mirrored rig, but the row's
setup produced an unmirrored avatar (`mirror=False`), so a mirrored seat has never been seen.

### Inspected images

I opened these and looked at the pixels. All are under `runtime-evidence\<run>\movement-visuals\`, named `sprint5-<name>.png`.
The blue shapes are the game's own silhouette overlay for a unit behind a wall or another unit; those frames show the outline only.

| Run | Image | What is visible |
| --- | --- | --- |
| r02 (earlier seat) | `horse-male-halfling-idle-side` | On the Horse, but too far forward over the pommel. Led to the fixed seat |
| r07 (Horse seat as final) | `horse-male-halfling-idle-side` | In the saddle, between pommel and cantle |
| r07 | `horse-female-gnome-idle-other`, `horse-male-gnome-idle-front` | Silhouette: clean seat above the back |
| r08 | `horse-action-gear-removed` | Same seat with the armor off |
| r08 | `horse-action-attack-1` | Silhouette: seated with arms in the attack |
| r08 | `horse-action-dismounted` | Standing on the ground at full size |
| f2 | `horse-female-gnome-idle-side`, `horse-male-gnome-idle-other`, `horse-action-cast` | Silhouette: seated. The cast frame is mostly occluded |
| f1 | `dog-female-halfling-move-1`, `dog-male-gnome-idle-other` | Mid-stride, thigh and knee at the flank; silhouette of a seated rider |
| f6 | `dog-female-gnome-idle-side` | Seated, leg clear of the body |
| r05, r06 (earlier dog seat) | `dog-male-halfling-idle-other`, `dog-action-attack-2`, `dog-action-mount-attack`, `dog-action-cast` | Seated through idle, cast, attack and bite |
| r12 | `dog-cold-restored-side`, `dog-cold-remounted-side` | Seated after the separate-process cold load and after remounting (earlier dog seat) |
| f4 | `dog-cold-remounted-side` | The same after remounting, on the corrected seat |
| f6 | `dog-enlarged-detached` | Silhouette: the enlarged rider standing clear of the dog |

Not every combination was viewed. Each rig on each mount was opened at least once, except these two gaps, which are inferred from the
same fixed seat and measured seat coordinates:

- Horse: the action frames ran on the male Halfling only.
- Riding Dog: cast and attack were viewed on the male Halfling only, and on the earlier dog seat.

The grown-dog and stock-Dog frames are silhouettes, or omit the stock Dog, so they are not visual evidence for those points.

## SIZE-01: native size and anatomy

The rule uses effective native categories, never render scale: the mount must be at least one category larger. A supported anatomy
is also required, so size alone never makes a creature rideable. The ordinary Dog stays unmountable.

| Case | Result | Evidence |
| --- | --- | --- |
| Small on Horse or Riding Dog | Mounted | f2, f6 |
| Medium on Riding Dog | Refused: "The mount must be at least one size category larger than its rider." | f6, also offline |
| Native Enlarge on a mounted Halfling, on the dog | Detached, owner kept, no action costs in the sampled cooldowns, position moved to the dismount point | f6 |
| Enlarge removed | Back to Small and able to ride | f6: size restored, and the next row mounts again |
| Legal and illegal size pairs, later mount growth | Offline policy tests | `SmallProfilePolicyTests` |

Limit: a mount changing size while ridden (Animal Growth) has not been seen in game. Its seat scale is covered offline only.

**Old profile compatibility.** `small-default-halfling-horse-v1` keeps its ID and now names the male Halfling rig on the Horse, so
existing archives load without a migration. Schema 1 and 2 rules and the Medium Horse and Mammoth IDs are unchanged. No old
archive holding that ID was cold-loaded in this sprint.

## DOG-01: the Riding Dog

| | |
| --- | --- |
| Unit | `b7a14f25d3c94e0d8e6f1a2c5d90e431` |
| Feature | `5c8e2b9a417f4d63a1d07e3bf26c9184` |
| Advancement | `e31d6f08a95c47b2b8c4f27a6d10e5c7` |
| Size feature (used only if the stock Dog is under Medium) | `92fb07c3d6e14a58bd23c8e1f47a0d69` |

It is additive. The stock Dog blueprint is only read and spawned as a comparison, and existing pets are not converted. A Riding Dog
registration failure leaves the Horse and all mounted controls working. That isolation exists because r01 showed the opposite.

**Selection path.** Ordinary native level-up on the Working campaign, which predates the Riding Dog: Ranger 4, Hunter's Bond,
Ranger companion, Riding Dog. Pet bound to the exact owner. This uses the native level-up controller, not the on-screen UI.

**Offered by** every native selection that offers the stock Dog: 7 on the pure base game, 14 with Call of the Wild. Only the Ranger
path was actually taken. Paladin Divine Mount is not implemented.

### Statistics and adaptation

| | Riding Dog (this mod) | Bestiary Riding Dog | Core companion Dog |
| --- | --- | --- | --- |
| Size | Medium from rank 1 | Medium | Small, Medium at 4th level |
| Str / Dex / Con | 15 / 15 / 15 | 15 / 15 / 15 | 13 / 17 / 15 |
| Int / Wis / Cha | 2 / 12 / 6 | 2 / 12 / 6 | 2 / 12 / 6 |
| Speed | 40 ft | 40 ft | 40 ft |
| Bite | 1d6, with the stock Dog's trip fact | 1d6 plus trip | 1d4, then 1d6 |
| Advancement | Rank 4: Str +2, Con +2 (17 / 15 / 17) | None | 4th level: Str +4, Dex -2, Con +2 (17 / 15 / 17) |

The Riding Dog takes the Bestiary's base scores and reaches the same 17 / 15 / 17 as a grown companion Dog at the same rank, so it
trades early Strength and Dexterity for riding size. No official source lists a Riding Dog as a companion: **this table is our
adaptation.** Hit dice, BAB, saves and natural armor come from native companion progression and the stock Dog's facts, so nothing is
granted twice.

Observed at rank 1 in f6: Medium, Str 15, Dex 15, Con 15, HP 11, AC 19, speed 40, `WolfBite` 1d6, and the `TrippingBite` fact. At
rank 4: Str 17, Con 17, HP 12, still Medium, same view scale, seat valid. **So there is no grown rig to fit.**

Sources:

- [Riding Dog, Archives of Nethys](https://aonprd.com/MonsterDisplay.aspx?ItemName=Riding%20Dog) and
  [d20pfsrd](https://www.d20pfsrd.com/bestiary/monster-listings/animals/canines/dog/riding-dog/): the two matched.
- [Core Rulebook druid companions](https://legacy.aonprd.com/coreRulebook/classes/druid.html): the Dog entry came through a tool
  summary of a truncated page, not verbatim text. Please check it against the book.

### What differs by installation

| | Pure base game (f5 log) | With Call of the Wild (fixture) |
| --- | --- | --- |
| Stock Dog blueprint size | Medium | Medium, made Small by its own added fact |
| Stock Dog advancement level | Rank 7 | Rank 4 |
| Riding Dog | Medium, rank 4 advancement | Same; the size facts are dropped |

The f5 run found that the first version copied the stock level, which would have given base-game players a rank 7 advancement
against a description of rank 4. It is now fixed at 4. **Riding on the pure base game has not been seen**: the fixture cannot load
there, so f5 proves registration and selection offers only.

### Actions, spending and lifecycle

- Ordinary clicks: rider 4 attacks and mount 1 bite in both f2 and f6. The rider's Standard cooldown ran while the mount's stayed
  at 0, then the mount's own ran when it was selected and clicked. Carrying donates nothing.
- Trip is present as a fact. A successful trip was not observed: it needs a hit, and no dice were forced.
- Pet character level stayed 1 at rank 4, with experience moved from 2,000 to 9,000. This is the existing Horse manual-levelling
  policy, and the manual level-up itself was not run. The HP above therefore reflects only the Constitution change.
- Replacement: removing the feature with the native fact call a respec makes left no pet and no orphan.
- **The on-screen respec UI was not run for the dog.**
- Removal preparation covers all four Riding Dog identities.

### Save continuity

f3 wrote the archive. In a separate process f4 restored the same owner, dog blueprint and rig, kept the sourced feature, resolved the
same profile, showed the seat, and then dismounted and remounted with ordinary controls. Gear changes ran mounted in f2 and f6, not
after the cold load.

### Neighbors

| Neighbor | Result |
| --- | --- |
| Medium/Mammoth | Mounted control row in f2 and f6; in the archive and cold load in f3 and f4 |
| Ordinary Dog | Not mountable; effective Small at 0.66 scale, level 0, 13/17/15 under Call of the Wild. Numbers only: the frame did not show it |
| Medium/Horse | **Not run in this sprint.** Its path is unchanged in code and the 1,024 component checks pass |

## Evidence classes

| Class | Contents |
| --- | --- |
| Executed on the final DLL | f6 |
| Retained on the previous DLL | f2, f3, f4, f1, f5 |
| Offline | Fast and Candidate tiers (Candidate 8/0 on `d1991a4c`: 1,024 component, 281 harness, 751 assembly), 34 reader checks, 183 Ranger contract checks |
| Not executed | Everything listed as a limit above; turn-based play on the Small profiles; a Sprint 1 to 4 requalification, which was not selected |

Body setup is disclosed in every row: a native Doll and race on a disposable actor, with the view rebuild native polymorph uses.
It is not character creation. The XP that makes level-ups possible is also disclosed setup.

## Original failures, retained

| Run | What happened |
| --- | --- |
| r01 | A strict stock Dog check faulted Horse registration too. Led to isolation and to reading the real stock values |
| r02 | No third party member for the dog; a following null reference. Horse rows and the first images were good. Led to the separate Dog scenario |
| r03 | Scenario bug: a null string stored in JSON is not type Null, so it tried to mount with no profile |
| r04 | The diagnostic target guard did not know the Riding Dog |
| r05 | Game PASS; outer: row names unregistered |
| r06 | Game PASS; outer: reader field name |
| r07 | Target spawned inside its minimum range |
| r08 | Game PASS; outer: schema 49 missing from the master list |
| r09 | Save route lacked XP setup |
| r10 | **Blocked.** See Recovery |
| f1 | Game PASS; outer: preamble wildcard. Re-evaluated PASS |
| f5 | Expected load failure; found the rank 7 defect. See Recovery |

Reader re-evaluations with old and new verdicts are in `analysis-cache\sprint5\`. The reviews found real defects, which were fixed:

- First review: the scale bug, stale Ranger audit shapes, partial restore.
- Delta review: selection drift gating the mod, per-frame costume allocations, an over-broad size filter, incomplete isolation,
  wrong removal text.

## Recovery and final state

- **r10:** my body setup replaced a view that the save route's world scope watches, so the scope kept custody by design and the
  game never quit. After confirming the process was this run's own (started 12 s after its lock, launcher gone, no save worker), I
  stopped that exact PID. Transaction and profile recovery both passed.
- **f5:** it left the engine's `Areas` cache in the live profile, because that scenario has no Areas isolation. The launcher
  refused to finish. Following the Sprint 2 precedent, seven engine-generated files went to
  `runtime-backups\profile-20261102-sprint5-f5-base\engine-areas-residue\`, with a hash receipt beside it; nothing was deleted.
  Guarded recovery then passed. The profile diff is empty and Params and PlayerPrefs match the snapshot.
- **Maintenance debt:** a run on `horse-companion-blueprint-registration` with no Call of the Wild leaves that residue, and it costs
  a manual recovery.
- **Closeout:** no game process, no lock, live `Mods` empty, protected saves restored. No candidate is installed.

## Costs

`sprint5` budget: see the record `runtime-state\workflow-budgets\sprint5.json`, 90-minute allowance, no adjustment. Final spend is in
CURRENT.md. 18 native launches across batches `sprint5:accept-1` to `-10` and `sprint5:final-1` to `-3`. r10's stall cost a full
10 minutes, and the rest averaged about 2.

## Owner checklist

1. Halfling on a Horse: mount, walk, turn, attack, cast, dismount, remount. It should sit in the saddle with the legs either side.
2. Change armor, helmet and weapon while mounted. It should stay mounted.
3. Gnome and female rigs on a Horse.
4. Level a Ranger to 4 and choose **Riding Dog**, on the real level-up screen, in an existing campaign. This is the check I could
   not make: the headless controller took the choice.
5. Ride it as a Small character, then attack with the rider and with the dog.
6. Try a Medium character: Mount should refuse with the size message.
7. Save while mounted on the dog, restart the game, and load.
8. Level a companion past rank 4 using the manual pet level-up, which I did not run.
9. Create a **fat or thin** Halfling or Gnome: expect a refusal. Tell me if this needs to work now.
10. Try a left-handed Small character: unproven, so please look at the legs.

## Limits, in priority order

1. **Fat and thin builds are refused.** This is the reason the full completion label is not used. Next step: declare the eight
   variants, measure their reach, and take one structured visual pass; estimated about 15 validation minutes across two launches.
2. Left-handed seat never seen.
3. Riding on the pure base game never seen, because no base-only fixture exists.
4. Turn-based Small riding not run in this sprint.
5. Mount growth while ridden, successful trip, manual pet level-up and on-screen selection and respec not seen.
6. Horse, save and cold-load evidence is on the previous DLL, one literal apart.
7. Other classes' selections are offered but were not taken.
