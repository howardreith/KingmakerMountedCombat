# Sprint 5: Small riders, relative size and the Riding Dog

**SPRINT 5 ENGINEERING COMPLETE — READY FOR FINAL REVIEW; OWNER PLAY ACCEPTANCE SEPARATE.**

The two items that held this label back are closed. No merge, release or installation is part of this work.

- **The missing right boot on female rigs had a native cause, now repaired.** On the female doll, the game's mesh merge leaves the right
  leg rigid on the pelvis, so that leg stayed in its standing pose while its bones were seated. See "The female boot: cause and repair".
- **The final Dog pose was rendered and inspected.** The earlier `.80`/`.52` proposal was never rendered and answered this same symptom,
  so it was reverted to `.68`/`.67` before any launch used it.

**Visual packet of real captures:** [Sprint 5 Boot Packet](https://claude.ai/artifact/T1vJzEguDxW1PJsLzzRsBg) (private until shared).

| | |
| --- | --- |
| Baseline | `main` `e8450a56`, the merge of Sprint 4 PR #9 |
| Branch | `codex/mounted-combat-sprint5-small-riders-riding-dog` |
| Final compiled source | `fcdce67564ee02f27a351e5fd1211ba35b8d824e`. Later commits change docs only |
| Final candidate | sprint5v, DLL `0aa9bfe552ac818bc73af1b49b2141d97339111227840c7c594d1d80f84ebefd`, MVID `e9782372-98f6-4d6e-b27d-a7995e6385c7`, 8,636,416 bytes |
| Package | `artifacts\KingmakerMountedCombat-0.1.0-chunk7b-preview.100-sprint5v-diagnostic.zip`, SHA-256 `3b8eb7c0501f16456b09abfa1dead675d87bb8fa0f1346fe3117358e8bf2fb14` |
| Suite | `sprint5v-fcdce675-20261102`, SHA-256 `65c40018d5a6db9daabbbf1719fa0af9a2809279204dff9cae9a8061d708e019` |
| Package bound | Unchanged at 9 MiB. No increase was needed |
| Budget | `sprint5`: see Costs |

The reviewer of this work did not see the private captures or run the game. Nothing here is independently reproduced.

## Evidence, by binary

**The boot closeout.** h2 and h3 ran the same final DLL bytes; only a reader line and the package identity differ between them.

| Run | DLL | Game | Outer reader | What it shows |
| --- | --- | --- | --- | --- |
| `20261103-sprint5-h3-horse` | `0aa9bfe5` (sprint5v, final) | PASS, 8 rows | PASS | Horse: boot comparison, all 12 rigs, actions on the male Halfling and both extremes, mirrored rider |
| `20261103-sprint5-h2-dog` | `0aa9bfe5` (sprint5u, same bytes) | PASS, 14 rows | Original FAIL, reader-only re-evaluation PASS | Dog: boot comparison, all 12 rigs, actions, Medium refusal, Enlarge, rank 4, pet levels, mirrored rider, stock Dog, replacement |
| `20261103-sprint5-h1-boot` | `1d2b6a86` (sprint5t, diagnostic) | PASS, 4 rows | FAIL by design | Ended after the boot row, so the later rows are missing. The cause and the rejected explanations |

**The body-variant pass.** g3 and g4 differ only by the Dog seat literals and the seat record hash. The g4 "final" labels below are
historical.

| Run | DLL | Game | Outer reader | What it shows |
| --- | --- | --- | --- | --- |
| `20261102-sprint5-g4-horse` | `011682da` (sprint5s, final) | PASS, 7 rows | PASS | Horse: selection, all 12 rigs, actions on the male Halfling and both extremes, mirrored rider |
| `20261102-sprint5-g3-dog` | `4ac811b3` (sprint5r) | PASS, 13 rows | PASS | Dog: all 12 rigs, actions, Medium refusal, Enlarge, rank 4, pet levels, mirrored rider, stock Dog, replacement |
| `20261102-sprint5-g5-dog` | `011682da` | Never launched | FAIL | Budget guard refusal, `launchIssued: false`. Staging rolled back |
| `20261102-sprint5-g2-horse` | `58919cf1` (sprint5q) | PASS, 7 rows | PASS | Horse, earlier feet. Images showed the near boot problem |
| `20261102-sprint5-g1-horse` | `222f7b4c` (sprint5p) | Rows through `S5-actions` PASS | FAIL | Scenario walk stranded the pair. See failures |

**Retained from the first pass, on its frozen reader.** These are not re-judged by a later reader, and only the Riding Dog's
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

**Corrections in the finishing passes:**

- Horse feet: spread .61 to .72 of reach, drop .67 to .60 (g2 to g4). Rendered clean, so kept. The female image that prompted the change
  was the merge defect, not the saddle.
- Dog feet: a proposed spread .80 and drop .52 (after g3) was **never rendered and reverted** to .68 and .67, which h2 rendered.
- The Horse's -0.16 root offset never took effect, because of static initialization order. It is now an explicit zero, with no
  change in behavior.

### Supported matrix

All 24 declarations are admitted in the final DLL. Each rig has its own skeleton, such as `MaleHalfling_fat`. The first eight
profile IDs are unchanged.

| Rig | Horse profile | Riding Dog profile | Boot, final Horse (h3) | Boot, final Dog (h2) |
| --- | --- | --- | --- | --- |
| Male Halfling | `small-default-halfling-horse-v1` | `small-halfling-male-ridingdog-v1` | Clear | Clear |
| Male Halfling fat | `small-halfling-male-fat-horse-v1` | `small-halfling-male-fat-ridingdog-v1` | Clear | Clear in motion and far side; a mammoth blocks the idle near view |
| Male Halfling thin | `small-halfling-male-thin-horse-v1` | `small-halfling-male-thin-ridingdog-v1` | Clear | Clear |
| Male Gnome | `small-gnome-male-horse-v1` | `small-gnome-male-ridingdog-v1` | Clear | Clear |
| Male Gnome fat | `small-gnome-male-fat-horse-v1` | `small-gnome-male-fat-ridingdog-v1` | Clear | Clear in motion and far side; a mammoth blocks the idle near view |
| Male Gnome thin | `small-gnome-male-thin-horse-v1` | `small-gnome-male-thin-ridingdog-v1` | Clear | Clear; the earlier dark frame is now readable |
| Female Halfling | `small-halfling-female-horse-v1` | `small-halfling-female-ridingdog-v1` | Clear | Clear |
| Female Halfling fat | `small-halfling-female-fat-horse-v1` | `small-halfling-female-fat-ridingdog-v1` | Clear | Clear |
| Female Halfling thin | `small-halfling-female-thin-horse-v1` | `small-halfling-female-thin-ridingdog-v1` | Clear | Clear |
| Female Gnome | `small-gnome-female-horse-v1` | `small-gnome-female-ridingdog-v1` | Clear | Clear |
| Female Gnome fat | `small-gnome-female-fat-horse-v1` | `small-gnome-female-fat-ridingdog-v1` | Clear | Clear |
| Female Gnome thin | `small-gnome-female-thin-horse-v1` | `small-gnome-female-thin-ridingdog-v1` | Clear | Clear |

"Clear" means the whole boot is visible from the rider's right side, resting on the saddle flap or the dog's coat. On the Dog, the boot
rests on the coat rather than hanging free of it, on male and female rigs alike. See Limits.

Every rig mounted, moved and dismounted in `S5-rigs` on both mounts. Gear, cast, turn, rider attacks, one mount attack and dismount
ran on the male Halfling and on both extremes, female Halfling thin and male Gnome fat, on both mounts.

**Fat and thin are close to the default build.** Measured thigh plus shin is within .002 of default; only hip width differs. The
fixture dresses every rig in the same gear, so the images do not separate the bodies much.

**Refused with feedback, as before:**

- Small riders on the Mammoth: no measured seat.
- Medium riders on the Riding Dog: too large (seen in g3, `S5-medium-refused`).
- A rider currently resized away from its native size, or any polymorph.
- A skeleton with no declaration. The message names the skeleton and the supported builds.

**The first pass's refusal of fat and thin presets is superseded by this matrix.** It is kept in the history of this file and in the
f-series evidence.

### The female boot: cause and repair

**Correction.** The earlier version of this report said the foot bones' clear position ruled out clearance. A bone origin is not the boot's
surface, so that conclusion was not established then. Run h1 compared real geometry and images instead.

**Cause.** The native merge, `Character.EnsureBones`, stops mapping a body part at the first bone the avatar's hierarchy lacks, and leaves
every later bone on the merged list's first bone. On the female doll that first bone is `Pelvis`. The female doll's hierarchy lacks
`L_back_weapon_slot_10`, and it comes before all of the `R_` leg bones in the parts' lists. So the right leg's vertices stay rigid on the
pelvis, in their standing pose. Standing, that looks normal. A seat bends the leg, and exposes it.

| Rig and moment | Parts where mapping stopped | `R_leg` merged slots | Right lower-leg vertices following `R_leg` |
| --- | --- | --- | --- |
| Female Halfling, standing, native | 13 | 5 | 0 |
| Female Halfling, seated, repaired | 0 | 13 | 308 |
| Male Halfling control, native | 0 | 13 | 308 |

**Competing explanations, from one controlled comparison with the same gear on both rigs (h1):**

| Question | Observation | Result |
| --- | --- | --- |
| Overlap with the saddle? | With the mount's renderers hidden for one render, the female right leg still hangs straight down; the male control splays | Rejected |
| Camera side, or anatomy? | On a mirrored female the near leg is whole and the far leg shows the defect | Anatomy: it follows the `R_` bones |
| Foot rotation (my lead)? | A fixture experiment made the foot follow the shin; the frame stayed cut off | Rejected |
| Armor? | Armor off with the mount hidden shows the same leg | Rejected |
| Capture, lighting or silhouette? | Same leg in ordinary light and in the ordinary-camera frame | Rejected |
| Mounted regression, or native? | Before any mount, the female right leg stays nearly straight while walking, in two frames each from h2 and h3, where the male rear leg bends | Native, exposed by the seat |

The fixture sets the body with a native Doll, and `DollData.CreateUnitView` uses this same `FemaleDoll` prefab for ordinary characters. A
character made on the real creation screen has not been run. Whether it also applies outside these Small rigs was not examined.

**Repair.** When a Small rider is seated, `SmallRiderRigCompletion` adds each bone the body parts name and the avatar lacks, with no
geometry, and lets the native merge rebuild, as an ordinary gear change does. It checks again after each native rebuild. Medium riders,
mechanics positions, collision, navigation, actions, saves and stock assets are untouched. A failure here is logged, and the seat keeps
the native mesh.

**One behavior to know.** The added bone stays for the life of that view. A female rider who has mounted once keeps a correctly moving
right leg after dismounting, until the view is next rebuilt. That differs from her native walk before mounting, and it is visible in
`horse-boot-female-halfling-dismounted-walk`.

**Dog constants.** `.80`/`.52` answered this same symptom, so it was reverted rather than kept unseen. h2 shows the `.68`/`.67` pose working
on all 12 rigs. The Horse's `.72`/`.60` stays, as already rendered.

### Left-handed riders

Seen with a real mirror on the final DLL on both mounts (h2, h3), using native `LeftHandedOverride`: `Mirror=true`, animator scale x -1,
on the male Halfling and now the female Halfling, each resolved to the rig's ordinary profile. The earlier row's
`DollState.SetLeftHanded` left `mirror=False`. Close images on both mounts show a clean straddle, including the female's far side after the
repair. It was restored to `false` afterwards. No complete mirrored matrix was selected.

### Inspected images

I opened these and looked at the pixels. All are under `runtime-evidence\<run>\movement-visuals\`, named `sprint5-<name>.png`.
The blue shapes are the game's own silhouette overlay for a unit behind a wall or another unit; those frames show the outline only.

**Capture setup, disclosed:** posed frames use a temporary white fill light that exists only during the fixture's render, so they are
brighter than play. `-ordinary-light` frames omit it, and `-ordinary-camera` frames also keep the camera where play has it. `-nomount`
frames hide the mount's renderers for one render and are diagnostics, not acceptance. Mammoths are the fixture's control and target, and
can block a view. Automated scores are not the verdict.

**Final candidate (h2 and h3):**

| Run | Image | What is visible |
| --- | --- | --- |
| h3 | `horse-<each of 12 rigs>-idle-close-side`, `-idle-close-other`, `-idle-close-front`, `-move-close-1` | Whole boots on both sides, in place and walking. Female rigs match the male control |
| h2 | `dog-<each of 12 rigs>-idle-close-side`, `-idle-close-other`, `-idle-close-front`, `-move-close-1` | Whole boots resting on the coat, in place and walking. A mammoth blocks a few idle near views |
| h3, h2 | `*-boot-female-halfling-mounted-near-ordinary-light`, `-mounted-ordinary-camera`, `<mount>-<12 rigs>-ordinary-camera` | Ordinary appearance. At that zoom the rider is small. On the Horse a pillar can put part of the mount behind the game's silhouette |
| h3, h2 | `*-action-female-halfling-thin-cast-close`, `-attack-close`, `-turn-close`, and the male Gnome fat equivalents | Seated and whole through cast, attack and turn |
| h3, h2 | `*-boot-female-halfling-armor-off-near`, `-armor-restored-near`, `-dismounted-near`, `-remounted-near`, `*-action-female-halfling-thin-gear-restored` | One gear change, dismount and remount on the affected rig. Full size standing, with no scale or pose residue |
| h3, h2 | `*-boot-female-halfling-left-handed-mounted-near`, `-mounted-far`, `*-left-handed-close-side`, `-close-other` | Mirrored female and mirrored male seated, boots whole |
| h3, h2 | `*-boot-female-halfling-standing-walk-1`, `-2`, `*-boot-male-halfling-standing-walk-1`, `-2`, `horse-boot-female-halfling-dismounted-walk` | The native baseline: the female right leg stays nearly straight walking before any mount |
| h2 | `dog-mounted-commit-side`, `dog-leveled-close-side`, `-close-other`, `dog-leveled-move`, `dog-enlarged-detached`, `dog-beside-stock-side` | Levelled pair seated on the final pose. The near boot sits low in the coat in the levelled frame. A hood appears in these frames, not traced. Enlarged rider standing clear. The stock frame is too occluded to count |
| h1 | `horse-boot-female-halfling-mounted-near`, `-nomount`, `-front`, `-front-nomount`, `-far`, `-armor-off-near-nomount`, `-experiment-foot-follows-near`, `-left-handed-mounted-near-nomount`, `horse-boot-male-halfling-mounted-near`, `-nomount`, `-front`, `-front-nomount` | The discriminators in the table above |

Not every frame was opened: h2 and h3 together hold 561 shots, and the move-close-2, attack-1 to -3 and most wide frames were sampled.

**Earlier evidence, retained (g3 and g4). Their "final" labels are historical.**

| Run | Image | What is visible |
| --- | --- | --- |
| g4 | `horse-<each of 12 rigs>-idle-close-side` | Seated. Male boots on the saddle flap, female near boots cut off |
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
| Medium/Horse | **Not run in this sprint.** Its path is unchanged in code and the 1,029 component checks pass |

## Evidence classes

| Class | Contents |
| --- | --- |
| Executed on the final DLL bytes | h3: all Horse rows. h2: all Dog rows, with the outer FAIL and its re-evaluation recorded under failures |
| Retained on an earlier DLL | g3 pet levels are repeated in h2 as a side effect of its row list, not as a new requirement. f3 and f4 save and cold load, default rigs only. g1 to g4, f1 to f6, r01 to r12, h1 |
| Offline, on `fcdce675` | Candidate 8/0 in 3.88 min, offline verification only: 1,029 component, 149 source, 90 reader, 281 harness, 751 assembly |
| Not executed | g5. Fat and thin save and cold load. Everything listed as a limit below. Turn-based play on the Small profiles. A Sprint 1 to 4 requalification, which was not selected |

Body setup is disclosed in every row: a native Doll and race on a disposable actor, with the view rebuild native polymorph uses, and
`LeftHandedOverride` for the mirrored rider. It is not character creation. The XP that makes level-ups possible is also disclosed
setup. The reader is contract v3: it adds the boot row, and needs a seated rig's merge to complete with both lower legs present and a
geometry observation from the same frame as its pixels. Retained evidence stays bound to its frozen reader.

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
| g2 | Outer PASS. Images showed the female near boot inside the saddle, which led to the Horse feet change. That image was the merge defect |
| g3 | Outer PASS. Images showed female near boots in the fur, which led to the Dog feet proposal. That image was the merge defect, and the proposal was reverted |
| g4 | Outer PASS. Images showed the Horse feet change fixed males and **not** females |
| g5 | **Refused before launch by the budget guard.** The WhatIf and the launch's own staging prelude took the remaining time from 10.37 to 9.62 minutes, below the launch reservation of 10. An earlier version blamed the WhatIf alone. The controller stopped before writing its completion marker, so the refusal was not read until the next session |
| h1 | FAIL by design: the diagnostic candidate ended after `S5-boot`. Found the cause. Its first reading, foot rotation, was rejected by its own experiment |
| h2 | Game PASS. Outer FAIL: "a seated rig has no matching lower legs", from a check I had added requiring equal left and right counts. The male control is natively 312 and 308. Corrected to within 5 percent, and a mostly missing leg still refuses. Re-evaluated PASS on the same observation (`analysis-cache\sprint5\h2-dog-reader-reevaluation.json`). My background waiter for this run was stopped by Claude Code under memory pressure. The detached controller was unaffected and finished normally |

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

`sprint5` budget, record `runtime-state\workflow-budgets\sprint5.json`: **139.58 of 150 minutes**. The allowance was 90, with two explicit +30
adjustments of the same budget from the finishing missions (120, then 150, both recorded under `allowanceAdjustments`), and no reset or new
ID. The first pass spent 81.74, and the body-variant pass ended at 114.48.

25 native launches: 18 in the first pass, g1 to g4 in batch `sprint5:finish-1`, and h1, h2 and h3 in `sprint5:boot-1` to `-3`. g5 is an
attempt that never launched. r10's stall cost a full 10 minutes.

## Owner checklist

1. **Female Halfling or Gnome on a Horse and a Riding Dog:** look at the boot on the rider's right. It should now be whole, as on a male rig.
2. Walk, turn, attack and cast while riding each mount.
3. Change armor while mounted. The seat should hold.
4. Dismount, remount, and walk on foot. A female rider who has mounted once should keep a normal stride. Tell me if you would rather she
   return to the game's native walk after dismounting.
5. Create a **fat or thin** Halfling or Gnome on the real creation screen and ride both mounts. Save and reload, which has not been run
   for these builds.
6. Level a Ranger to 4 and choose **Riding Dog**, on the real level-up screen, in an existing campaign. This is the check I could
   not make: the headless controller took the choice.
7. Level the dog through the real pet level-up screen, once while riding it.
8. Try a Medium character: Mount should refuse with the size message.
9. Save while mounted on the dog, restart the game, and load.
10. Try a left-handed Small character.

## Limits, in priority order

1. **On the Dog, the boot rests on the coat.** It is whole and readable, but the deepest measured boot vertices sit about 0.06 to 0.12 units
   under the coat's outer vertices, on male and female rigs alike. In a levelled frame the near boot sits low. The coat is soft-looking, so I
   judged this plausible rather than a defect. Please say if it reads wrong in play.
2. Fat and thin builds and the rig repair have not been saved or cold-loaded. The repair changes no save data and uses the same attach
   path, but a repaired rider has not been seen after a real load. f3 and f4 are retained on `7ef02e56`, default rigs only.
3. A character made on the real creation screen has not been run. The Doll setup uses the same native prefab.
4. The native defect itself was not examined on other races, and no claim is made about Medium female riders.
5. Riding and pet levels on the pure base game never seen, because no base-only fixture exists.
6. Turn-based Small riding not run in this sprint.
7. Mount growth while ridden, successful trip, and on-screen selection, level-up and respec not seen.
8. Other classes' selections are offered but were not taken.
