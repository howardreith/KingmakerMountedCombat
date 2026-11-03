# Sprint 5: Small riders, relative size and the Riding Dog

**Status: NOT the Sprint 5 completion label. Two selected outcomes are open.**

1. **Open defect: the near boot of every female rig is cut off.** On the final Horse seat (g4), all six female rigs lose the boot
   on the rider's right at the saddle. On the Dog (g3, previous seat) it sinks into the fur. Their far-side legs and all male rigs
   display correctly. The foot bones report a position clear of the saddle, so this is not explained by clearance. **The cause is
   not known.**
2. **Not executed: the final Dog seat.** The last Dog correction (feet spread .68 to .80, drop .67 to .52, for all 12 rigs) is in the
   final DLL but has never been rendered. The budget guard refused the run (g5) before launch: 9.62 minutes remained and a launch
   needs 10. So no Dog row, including the pet level sequence, ran on the final DLL. Whether that correction helps is unknown.

Everything else selected was demonstrated, as listed below: 24 profiles mounted, moved and dismounted, the extremes acted, a real
mirrored rider sat correctly, and a Riding Dog levelled natively. Owner play acceptance is separate. No merge, release or installation
is part of this work.

| | |
| --- | --- |
| Baseline | `main` `e8450a56`, the merge of Sprint 4 PR #9 |
| Branch | `codex/mounted-combat-sprint5-small-riders-riding-dog` |
| Final compiled source | `877f4b7f06baeb77e9702fa12bd7a505c617929d`. Later commits change docs only |
| Final candidate | sprint5s, DLL `011682da4dce695a84c95b455bcbd250d2b7af8a34fa273b78b76a63a0d91432`, MVID `87950acb-17f5-4802-aa79-1df042f02803`, 8,614,400 bytes |
| Package | `artifacts\KingmakerMountedCombat-0.1.0-chunk7b-preview.100-sprint5s-diagnostic.zip`, SHA-256 `e29388dedda4912b33c60c1df4ce5a2434325336a2c36af25e2ab6dc117ab6ad` |
| Suite | `sprint5s-877f4b7f-20261102`, SHA-256 `cfdb83c622dca4fa8ec8b9abc64691b23f02adb773a13d092bf7426cf7a5b6f0` |
| Package bound | Unchanged at 9 MiB. No increase was needed |
| Budget | `sprint5`: see Costs |

The reviewer of this work did not see the private captures or run the game. Nothing here is independently reproduced.

## Evidence, by binary

**This finishing pass.** g3 and g4 differ only by the Dog seat literals and the seat record hash.

| Run | DLL | Game | Outer reader | What it shows |
| --- | --- | --- | --- | --- |
| `20261102-sprint5-g4-horse` | `011682da` (sprint5s, final) | PASS, 7 rows | PASS | Horse: selection, all 12 rigs, actions on the male Halfling and both extremes, mirrored rider |
| `20261102-sprint5-g3-dog` | `4ac811b3` (sprint5r) | PASS, 13 rows | PASS | Dog: all 12 rigs, actions, Medium refusal, Enlarge, rank 4, pet levels, mirrored rider, stock Dog, replacement |
| `20261102-sprint5-g5-dog` | `011682da` | Never launched | FAIL | Budget guard refusal, `launchIssued: false`. Staging rolled back |
| `20261102-sprint5-g2-horse` | `58919cf1` (sprint5q) | PASS, 7 rows | PASS | Horse, earlier feet. Images showed the near boot problem |
| `20261102-sprint5-g1-horse` | `222f7b4c` (sprint5p) | Rows through `S5-actions` PASS | FAIL | Scenario walk stranded the pair. See failures |

**Retained from the first pass, on its frozen reader.** These are not re-judged by the v2 reader, and only the Riding Dog's
advancement level literal differs between the two DLLs here.

| Run | DLL | Game | Outer reader | What it shows |
| --- | --- | --- | --- | --- |
| `20261102-sprint5-f6-dog` | `ad3ee75e` (sprint5o) | PASS 11/11 | PASS | Riding Dog: selection, all four rigs, actions, Enlarge, rank 4, stock Dog, replacement |
| `20261102-sprint5-f2-horse` | `7ef02e56` (sprint5n) | PASS 6/6 | PASS | Horse: selection, all four rigs, actions |
| `20261102-sprint5-f3-save` | `7ef02e56` | PASS | PASS | Native Manual `8e42b8ed…` holding the Medium/Mammoth pair and `small-halfling-female-ridingdog-v1` |
| `20261102-sprint5-f4-cold` | `7ef02e56` | PASS | PASS | Separate process (PID 12800) cold-loads that file, then dismount and remount |
| `20261102-sprint5-f1-dog` | `7ef02e56` | PASS 11/11 | Original FAIL, reader-only re-evaluation PASS | Same Dog rows as f6 on the f2 to f4 binary |
| `20261102-sprint5-f5-base` | `7ef02e56` | FAIL, expected | FAIL, recovered | Base game, no Call of the Wild. Registration only: the Working fixture cannot load without that mod |

The save pair `f3`/`f4` was not repeated on any later binary, so no fat or thin rig has been saved or cold-loaded.

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

**Corrections in the finishing pass, driven by the images:**

- Horse feet: spread .61 to .72 of reach, drop .67 to .60 (g2 to g4). Male near boots now hang clear on the saddle flap. Female ones
  do not.
- Dog feet: spread .68 to .80, drop .67 to .52 (after g3). **Never rendered.**
- The Horse's -0.16 root offset never took effect, because of static initialization order. It is now an explicit zero, with no
  change in behavior.

### Supported matrix

All 24 declarations are admitted in the final DLL. Each rig has its own skeleton, such as `MaleHalfling_fat`. The first eight
profile IDs are unchanged.

| Rig | Horse profile | Riding Dog profile | Horse near boot, final seat (g4) | Dog near boot, previous seat (g3) |
| --- | --- | --- | --- | --- |
| Male Halfling | `small-default-halfling-horse-v1` | `small-halfling-male-ridingdog-v1` | Clear | Clear |
| Male Halfling fat | `small-halfling-male-fat-horse-v1` | `small-halfling-male-fat-ridingdog-v1` | Clear | Clear |
| Male Halfling thin | `small-halfling-male-thin-horse-v1` | `small-halfling-male-thin-ridingdog-v1` | Clear | Clear |
| Male Gnome | `small-gnome-male-horse-v1` | `small-gnome-male-ridingdog-v1` | Clear | Clear |
| Male Gnome fat | `small-gnome-male-fat-horse-v1` | `small-gnome-male-fat-ridingdog-v1` | Clear | Clear |
| Male Gnome thin | `small-gnome-male-thin-horse-v1` | `small-gnome-male-thin-ridingdog-v1` | Clear in the front view; the bedroll hides it from the side | Dark frame, boot not readable |
| Female Halfling | `small-halfling-female-horse-v1` | `small-halfling-female-ridingdog-v1` | **Cut off** | **In the fur** |
| Female Halfling fat | `small-halfling-female-fat-horse-v1` | `small-halfling-female-fat-ridingdog-v1` | **Cut off** | **In the fur** |
| Female Halfling thin | `small-halfling-female-thin-horse-v1` | `small-halfling-female-thin-ridingdog-v1` | **Cut off** | **In the fur** |
| Female Gnome | `small-gnome-female-horse-v1` | `small-gnome-female-ridingdog-v1` | **Cut off** | **In the fur** |
| Female Gnome fat | `small-gnome-female-fat-horse-v1` | `small-gnome-female-fat-ridingdog-v1` | **Cut off** | **In the fur** |
| Female Gnome thin | `small-gnome-female-thin-horse-v1` | `small-gnome-female-thin-ridingdog-v1` | **Cut off** | **In the fur** |

Every rig mounted, moved and dismounted in `S5-rigs` on both mounts. Gear, cast, turn, four rider attacks, one mount attack and
dismount ran on the male Halfling and on both extremes, female Halfling thin and male Gnome fat, on both mounts.

**Fat and thin are close to the default build.** Measured thigh plus shin is within .002 of default; only hip width differs. The
fixture dresses every rig in the same gear, so the images do not separate the bodies much.

**Refused with feedback, as before:**

- Small riders on the Mammoth: no measured seat.
- Medium riders on the Riding Dog: too large (seen in g3, `S5-medium-refused`).
- A rider currently resized away from its native size, or any polymorph.
- A skeleton with no declaration. The message names the skeleton and the supported builds.

**The first pass's refusal of fat and thin presets is superseded by this matrix.** It is kept in the history of this file and in the
f-series evidence.

### The open female boot defect

What was measured and seen:

- The female `R_foot` bone is at about x .29 to .39 on the Horse. The saddle skirt is about ±.32, and the left foot is symmetric. The
  rendered shin hangs almost straight down, which does not match that position.
- It is on all six female rigs and on the rider's right only, in g2 and g4 on the Horse and g3 on the Dog.
- In g3's wide Dog view, a female boot was visible at frame 5133 and hidden at 5134, as its bone went from x .273 to .258. The rider
  rolls with the dog's torso.

What this does not show: the cause. It could be in the female mesh, foot or animation, but none of that is proven. The reliable next
check is a single female Horse launch comparing the right foot bone with its baked mesh vertices.

### Left-handed riders

Seen with a real mirror on the final DLL (Horse, g4) and on sprint5r (Dog, g3), using native `LeftHandedOverride`: `Mirror=true`,
animator scale x -1, male Halfling, resolved to the rig's ordinary profile. The earlier row's `DollState.SetLeftHanded` left
`mirror=False`. Mounted feet on the Horse were at ±.31. Dog feet were .29 and -.20, on the seat before the final Dog correction.
Close images on both mounts show a clean straddle. It was restored to `false` afterwards. Only that one rig was viewed mirrored, so a
**mirrored female rig has not been seen**.

### Inspected images

I opened these and looked at the pixels. All are under `runtime-evidence\<run>\movement-visuals\`, named `sprint5-<name>.png`.
The blue shapes are the game's own silhouette overlay for a unit behind a wall or another unit; those frames show the outline only.

**Capture setup, disclosed:** the finishing pass's frames use a temporary white fill light that exists only during the fixture's render,
so they are brighter than play. Mammoths in the background are the fixture's control and target. Automated scores are not the verdict:
162 Horse shots with 159 scored clear, and 183 Dog shots with 177 clear. The unclear ones were two Mammoth control views, one attack
frame and four Dog dismounted frames.

| Run | Image | What is visible |
| --- | --- | --- |
| g4 (final) | `horse-<each of 12 rigs>-idle-close-side` | Seated. Male boots on the saddle flap, female near boots cut off |
| g4 | `horse-<each of 12 rigs>-idle-close-other`, `-idle-close-front`, `-dismounted` | Far legs and boots whole on all 12. Dismounted at full size |
| g4 | `horse-left-handed-close-side`, `-close-other` | **Genuinely mirrored rider:** seated, both boots whole |
| g4 | `horse-action-female-halfling-thin-cast`, `-attack-1` | Seated and in the action at a distance |
| g4 | `horse-action-male-gnome-fat-mount-attack`, `-gear-removed` | Mount attack with the target bleeding; the seat is kept with armor off |
| g3 | `dog-<each of 12 rigs>-idle-close-side` | Male boots clear of the fur, except the dark male Gnome thin frame. Female near boots in the fur |
| g3 | `dog-<each of 12 rigs>-idle-close-other` | Far legs and boots whole on all 12 |
| g3 | `dog-left-handed-close-side`, `-close-other` | **Genuinely mirrored rider** on the dog: seated, boots whole |
| g3 | `dog-mounted-commit-side`, `dog-leveled-close-side`, `dog-leveled-move` | Same pair seated after the mounted level-up and moving. The near boot sits low in the fur. A hood appears that other frames lack; not traced |
| g3 | `dog-action-female-halfling-thin-cast`, `-attack-1`, `dog-action-male-gnome-fat-mount-attack`, `-gear-removed`, `-dismounted` | Seated through the actions; the dismounted Gnome stands beside the dog, cropped at the frame edge |
| g3 | `dog-enlarged-detached` | The enlarged rider is on the ground, cropped at the frame edge |
| g3 | `dog-beside-stock-side` | Too occluded for comparison. **Not visual evidence for the stock Dog** |

**First pass, retained:**

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

Not every frame was opened: 12 rigs across the close views and a sample of the action frames. Move frames and most wide views were
not. The grown-dog and stock-Dog frames are silhouettes, occluded, or omit the stock Dog, so they are not visual evidence for those
points.

## SIZE-01: native size and anatomy

The rule uses effective native categories, never render scale: the mount must be at least one category larger. A supported anatomy
is also required, so size alone never makes a creature rideable. The ordinary Dog stays unmountable.

| Case | Result | Evidence |
| --- | --- | --- |
| Small on Horse or Riding Dog | Mounted | g4 Horse; g3 Dog; f2, f6 |
| Medium on Riding Dog | Refused: "The mount must be at least one size category larger than its rider." | g3, f6, also offline |
| Native Enlarge on a mounted Halfling, on the dog | Detached, owner kept, no action costs in the sampled cooldowns, position moved to the dismount point | g3, f6 |
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
| Advancement | Rank 4: Str +2, Con +2 (17 / 15 / 17 array) | None | 4th level: Str +4, Dex -2, Con +2 (17 / 15 / 17 array) |

The Riding Dog takes the Bestiary's base scores and reaches the same 17 / 15 / 17 array as a grown companion Dog at the same rank, so
it trades early Strength and Dexterity for riding size. **17 / 15 / 17 is base plus species advancement, not the final total of a
levelled pet:** native companion bonuses come on top. In g3 a level 4 pet had Str 18. No official source lists a Riding Dog as a
companion: **this table is our adaptation.** Hit dice, BAB, saves and natural armor come from native companion progression and the
stock Dog's facts, so nothing is granted twice.

Observed at rank 1 in f6: Medium, Str 15, Dex 15, Con 15, HP 11, AC 19, speed 40, `WolfBite` 1d6, and the `TrippingBite` fact. At
rank 4: Str 17, Con 17, HP 12, still Medium, same view scale, seat valid. **So there is no grown rig to fit.**

Sources:

- [Riding Dog, Archives of Nethys](https://aonprd.com/MonsterDisplay.aspx?ItemName=Riding%20Dog): Medium, 40 ft, bite 1d6 plus trip,
  15 / 15 / 15, 2 / 12 / 6. [d20pfsrd](https://www.d20pfsrd.com/bestiary/monster-listings/animals/canines/dog/riding-dog/) matched.
- [Core Rulebook druid companions](https://legacy.aonprd.com/coreRulebook/classes/druid.html), Dog: Small, 13 / 17 / 15, bite 1d4; at
  4th level Medium, bite 1d6, Str +4, Dex -2, Con +2.

**Correction:** an earlier version of this report said the Dog entry came only from a truncated tool summary. The mission's review
verified both entries above, as cited, so that caveat is withdrawn.

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
- First pass: pet character level stayed 1 at rank 4, with experience moved from 2,000 to 9,000, and the manual level-up was not run.
  The HP in the statistics above therefore reflects only the Constitution change. The pet level sequence below closes that gap.
- Replacement: removing the feature with the native fact call a respec makes left no pet and no orphan.
- **The on-screen respec UI was not run for the dog.**
- Removal preparation covers all four Riding Dog identities.

### Pet level sequence (g3, sprint5r; not repeated on the final DLL)

`S5-pet-level` ran on the Riding Dog, male Halfling rider, using the native level-up controller rather than the on-screen UI. Owner
rank and pet character level are separate: the owner stood at rank 4, the pet at level 1.

| Commit | Pet level | Hit points base / max | BAB | Fort / Ref / Will | New from native progression |
| --- | --- | --- | --- | --- | --- |
| Unmounted | 1 to 2 | 9 to 15 / 12 to 21 | 0 to 1 | 3 / 3 / 0 | Skills |
| **While mounted** | 2 to 3 | 15 to 21 / 21 to 30 | 1 to 2 | 3 / 3 / 1 | Alertness (chosen), Evasion, Str 18 |
| Unmounted | 3 to 4 | 21 to 27 / 30 to 39 | 2 to 3 | 4 / 4 / 1 | Wis 13 (chosen), natural armor |

- Class `AnimalClass`, hit die 8. Experience stayed 9,000 through the commits, and reached 15,000 when the owner then went from rank 4
  to 5.
- **Upgrade once:** count and rank were 1 / 1 throughout, at Str 17 before the native companion bonus. Size stayed Medium.
- **Continuity:** same pet identity (`29021c5e…`), one Riding Dog unit, same owner. After the mounted commit the pair was still Mounted
  on `small-halfling-male-ridingdog-v1`, and the levelled pet was mounted and moved again with ordinary controls.
- **Acquisition** is the Ranger selection described above.

**Limit on the pure base game.** Call of the Wild patches `AddPet` (`TryLevelUpPet`), and this sequence ran with it loaded. A read of the
base game's `AddPet` suggests it would apply class levels itself, but that path has not been seen.

### Save continuity

f3 wrote the archive. In a separate process f4 restored the same owner, dog blueprint and rig, kept the sourced feature, resolved the
same profile, showed the seat, and then dismounted and remounted with ordinary controls. Gear changes ran mounted in f2 and f6, not
after the cold load.

### Neighbors

| Neighbor | Result |
| --- | --- |
| Medium/Mammoth | Mounted control row in g2, g3, g4, f2 and f6; in the archive and cold load in f3 and f4 |
| Ordinary Dog | Not mountable; effective Small at 0.66 scale, level 0, 13/17/15 under Call of the Wild. Numbers only: the frames did not show it |
| Medium/Horse | **Not run in this sprint.** Its path is unchanged in code and the 1,028 component checks pass |

## Evidence classes

| Class | Contents |
| --- | --- |
| Executed on the final DLL | g4: Horse rows, including all 12 rigs and the mirrored rider |
| Retained on an earlier DLL | g3 (one set of Dog literals and the seat hash away): every Dog row and the pet levels. g1, g2. First pass f1 to f6 and r01 to r12 |
| Offline, on `877f4b7f` | Candidate 8/0 in 3.99 min, offline verification only: 1,028 component, 149 source, 78 reader, 281 harness, 751 assembly |
| Not executed | **g5, the final Dog seat**. Fat and thin save and cold load. Everything listed as a limit below. Turn-based play on the Small profiles. A Sprint 1 to 4 requalification, which was not selected |

Body setup is disclosed in every row: a native Doll and race on a disposable actor, with the view rebuild native polymorph uses, and
`LeftHandedOverride` for the mirrored rider. It is not character creation. The XP that makes level-ups possible is also disclosed
setup. The reader is contract v2: it requires a real mirror and each extreme's own body. Retained evidence stays bound to its frozen
reader.

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
| g1 | "No bounded walkable Phase 3D point satisfied the requested distance." The scenario's walks stranded the pair in `S5-body-actions` after 12 rigs. `S5-rigs` and `S5-actions` passed. Walks now stay around home |
| g2 | Outer PASS. Images showed the female near boot inside the saddle, which led to the Horse feet change |
| g3 | Outer PASS. Images showed female near boots in the fur, which led to the Dog feet change |
| g4 | Outer PASS. Images showed the Horse feet change fixed males and **not** females |
| g5 | **Refused before launch by the budget guard.** The required WhatIf took the remaining time from 10.37 to 9.62 minutes, below the launch reservation of 10. The controller stopped before writing its completion marker, so the refusal was not read until the next session |

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

`sprint5` budget, record `runtime-state\workflow-budgets\sprint5.json`: **114.48 of 120 minutes**, 5.52 remaining, which cannot admit a
launch. The allowance was 90, with one explicit +30 adjustment of the same budget from the finishing mission (recorded
under `allowanceAdjustments`), and no reset or new ID. The first pass spent 81.74.

22 native launches: 18 in the first pass, plus g1 to g4 in batch `sprint5:finish-1`. g5 is a 23rd attempt that never launched. r10's
stall cost a full 10 minutes.

## Owner checklist

1. **Female rider on a Horse and a Riding Dog: look at the boot on the rider's right.** This is the open defect, and the images show it
   cut off. Tell me whether it also looks wrong in ordinary play.
2. **Any rider on a Riding Dog: look at both feet.** This seat has never been rendered.
3. Halfling on a Horse: mount, walk, turn, attack, cast, dismount, remount. It should sit in the saddle with the legs either side.
4. Change armor, helmet and weapon while mounted. It should stay mounted.
5. Create a **fat or thin** Halfling or Gnome and ride both mounts. Save and reload, which has not been run for these builds.
6. Level a Ranger to 4 and choose **Riding Dog**, on the real level-up screen, in an existing campaign. This is the check I could
   not make: the headless controller took the choice.
7. Level the dog through the real pet level-up screen, once while riding it. Its Strength and Hit Points should grow, and it should
   stay Medium and mounted.
8. Attack with the rider and with the dog.
9. Try a Medium character: Mount should refuse with the size message.
10. Save while mounted on the dog, restart the game, and load.
11. Try a left-handed Small character, ideally female, and look at the legs.

## Limits, in priority order

1. **Female near boot defect, cause unknown.** Blocks the completion label. See "The open female boot defect".
2. **Final Dog seat never rendered, and no Dog row ran on the final DLL.** Blocks the completion label. Next step for both: one female
   Horse diagnostic and one Dog launch, about 15 validation minutes, which needs a new allowance from the owner. The current mission
   allows no further extension.
3. Mirrored female rig not seen.
4. Fat and thin builds not saved or cold-loaded. The save pair is retained on `7ef02e56`, default rigs only.
5. Riding and pet levels on the pure base game never seen, because no base-only fixture exists.
6. Turn-based Small riding not run in this sprint.
7. Mount growth while ridden, successful trip, and on-screen selection, level-up and respec not seen.
8. Other classes' selections are offered but were not taken.
