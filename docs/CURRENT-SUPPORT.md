# Current support, removal and rollback

The current support contract for Mr. Bard's Kingmaker Mounted Combat (KMC). Written by Sprint 8 of the Known Issues
Remediation Epic on 2026-11-19. It replaces reading the [0.2.0 notes](RELEASE-NOTES-0.2.0.md) and the Chunk 7 guides
([support](CHUNK7-SUPPORT-CONFIG.md), [rollback and removal](CHUNK7-MIGRATION-ROLLBACK-REMOVAL.md)) for current
status. Those documents stay as historical records.

Every statement below comes from current source, an accepted handoff or a release note, and links to it. Issue
dispositions are in the [current issue ledger](CURRENT-KNOWN-ISSUES.md). The epic's final accounting is the
[epic closeout](KNOWN-ISSUES-REMEDIATION-EPIC-CLOSEOUT.md).

## Two builds to keep apart

| | Current release `v0.2.12` | Earlier releases (`v0.2.11` and before) |
|---|---|---|
| What | Released 2026-11-19 by owner order, from the Known Issues Remediation Epic (PRs #45 to #53, merged into main as `446985b7`). Its package identity (zip and DLL SHA-256, MVID, release-source commit, tagged merge) is on its GitHub release. [Notes](RELEASE-NOTES-0.2.12.md). | `v0.2.11`: tag at historical release merge `7a5cd2c3` on 2026-11-15, built from release-source commit `86ad9cc1`; the mounted-attack-stall repair was merged earlier at `22945394`. [Notes](RELEASE-NOTES-0.2.11.md). |
| Version label | 0.2.12; assembly version 0.1.0.0 | 0.2.0–0.2.11; assembly version 0.1.0.0. Development builds of main between `v0.2.11` and `v0.2.12` **also said 0.2.11** but are different binaries from the `v0.2.11` package. |
| Native gameplay evidence | Not run in the game before release ([0.2.11 notes](RELEASE-NOTES-0.2.11.md)) | Not run for any known-issues sprint |

Wherever this document says **since 0.2.12**, the behaviour is new in 0.2.12 (from the Known Issues Remediation
Epic) and is not in 0.2.11 or earlier. Tell the
two apart by the DLL's SHA-256 (the release package identity is on the GitHub release), never by the version label.

## Supported play

### Game, loader and dependencies

- **Game:** Pathfinder: Kingmaker, engine build **2.1.7b** only. `Info.json` declares `GameVersion` 2.1.7.
- **Loader source baseline (unreleased):** `Info.json` and the removal observer declare **0.31.1**. Builds now use
  exact pinned UMM 0.31.1/Harmony12 references; the baseline build, bound-member and native checks remain pending
  ([record](UMM-0311-BASELINE.md)). Released 0.2.15 still advertises 0.28.2; that old floor was not assessed
  (no genuine 0.28.2 binary was available). Earlier API compatibility passed offline on genuine 0.33.0 and 0.33.2
  binaries; 0.33.0 and 0.33.2 identities are reviewed profiles ([ledger, T03](CURRENT-KNOWN-ISSUES.md#t03--loader-matrix-on-the-development-machine-2026-11-15),
  `planning/UMM-LOADER-PROFILES.json`). Historical native qualification ran on UMM 0.33.2
  (`planning/ENVIRONMENT-FINGERPRINT.json`). No loader was run natively by the known-issues sprints.
- **Dependencies:** none. `Info.json` `Requirements` is empty. The historical native lab ran with its fixture's own
  Call of the Wild and Craft Magic Items; neither a KMC-only game nor a full personal mod stack was verified end to end
  ([0.2.0 notes](RELEASE-NOTES-0.2.0.md#supported)).
- **Tabletop Rules Expansion (X01):** with Tabletop Rules Expansion (mod id `KingmakerGunslinger`) installed —
  reproduced with 0.0.191 and 0.0.140 — `v0.2.12` fails to load (`Not loaded`), and a campaign that uses Mounted
  Combat content then fails to load while it is absent. 0.2.11 does not have this conflict. **Fixed in 0.2.13**:
  verified offline with both versions' released binaries; startup checked in the game beside 0.0.140 only; 0.0.191
  natively, the reported save and gameplay are not verified
  ([notes](RELEASE-NOTES-0.2.13.md), [handoff](GUNSLINGER-CLICK-COMPAT-REPAIR-HANDOFF.md),
  [ledger](CURRENT-KNOWN-ISSUES.md#after-release-0212)).

### Riders, mounts and pairs

- **Mounts:** the rider's own animal companion: the KMC Horse or Riding Dog (from a Ranger's companion choice or a
  Paladin's Divine Bond) and a supported stock Mammoth.
- **Riders:** Medium riders on the Horse or Mammoth; Halfling and Gnome riders on the Horse or Riding Dog
  ([0.2.0 notes](RELEASE-NOTES-0.2.0.md#supported)).
- **Pairs:**

  | | Source | Native evidence |
  |---|---|---|
  | Capacity | Six pairs. Admission, the archive format and the ownership registry all declare six (`GameMountedRelationshipService.NativePairCapacity`, `MountedRetainedIntentRecord.MaximumPairs`); main's charge probe asserts it ([T01](CURRENT-KNOWN-ISSUES.md#t01--what-the-probe-now-establishes)). | — |
  | Combat | The same single admission limit of six; no separate combat limit was found in source | **Two pairs** qualified natively (historical Chunk 7) |
  | Exploration, Manual save and reload | Six | **Six pairs** qualified natively (historical Chunk 7) |
  | Six-pair combat | Admitted by source | **Not qualified.** Excluded by the owner's scope decision and by this epic |

### Exploration

- One-click doors, chests, carried touch spells, looting corpses and camping while mounted; the Horse runs with the
  party ([0.2.0](RELEASE-NOTES-0.2.0.md), [0.2.3](RELEASE-NOTES-0.2.3.md), [0.2.7](RELEASE-NOTES-0.2.7.md)).
- Area entrances: the mount receives the party's entrance order and carries its rider ([0.2.5](RELEASE-NOTES-0.2.5.md)).
- Same-area doors (for example Ratnook Hill): the rider steps down and remounts on the other side, with no action
  cost ([0.2.4](RELEASE-NOTES-0.2.4.md)).
- World map: pairs travel across it and remount on arrival in the next area ([0.2.3](RELEASE-NOTES-0.2.3.md)).
  Capital and similar areas dismount companion pairs on arrival.
- **Since 0.2.14 (offline-tested, not played):**
  - The Mount / Dismount shortcut keeps the slot you chose through the capital, the reserve roster, saving and loading,
    and temporary companion unavailability. Your moves and removals are kept, and other abilities are never overwritten
    (X02).
  - In the capital, each companion's own animal companion is listed in the Inventory, Character and Spellbook screens
    (X03).
- **Since 0.2.12:** entrance and exit orders, the gamepad follower pass and group pace use the horse as the mover
  (M01–M03); a gamepad stick drives a mounted pair's mount (M05); extra destination markers from the entrance carry,
  scene regroup, door approach and cannot-attack click approach are removed (M04); a mounted party crosses the
  Candlemere Tower wade-through (C04).
- **Not claimed:** mounted trap disarming (WORLD-02, [deferred](FINALIZATION-DEFERRED-ITEMS.md)); turn-based doors.

### Turn-based combat

- **Paired activation (F3):** the mount owns the pair's movement. With the rider selected, the action panel's
  five-foot, Move, Standard and full-round slots show the mount's actions. The rider can full-attack while the mount
  still moves legally in the same turn.
- **Initiative:** The paired authority supports rider and mount actors that entered combat at the same or different native
  initiative positions. While mounted, the mount's independent turn candidate is suppressed and the pair acts
  through one shared activation; carried movement on a separate mount slot does not occur. A pair formed during an
  encounter forms only on the rider's turn ([Sprint 5 R01](KNOWN-ISSUES-SPRINT5-HANDOFF.md#r01--why-the-separate-initiative-case-has-no-reachable-instance)).
  The game owns every action budget, preparation, initiative and turn ([0.2.0](RELEASE-NOTES-0.2.0.md)).
- **Attacks:** ordinary clicks, native attacks and Haste, retargeting on a kill, Stop and Hold. A turn-based attack
  order the game ends before it starts is dropped once with a message and is not retried; an actor that cannot act is
  refused up front, and only that actor is judged ([0.2.11](RELEASE-NOTES-0.2.11.md)).
- **Mounted Charge:** always available while the mod is enabled (no switch since [0.2.2](RELEASE-NOTES-0.2.2.md)). The
  rider charges; the mount pays its travel along a clear straight line; the game's Charge requirements apply.
- **Rider falls:** a ridden mount that dies, collapses or falls prone throws its rider once (Mobility DC 15; failure
  1d6 and prone) ([0.2.0](RELEASE-NOTES-0.2.0.md)).
- **Since 0.2.12:** a same-target click after a finished attack is a fresh request with its own answer (A01); terminal
  messages describe the actual end, so a Stop beside a dazed rider is no longer "cannot act" (A02).

### Real-time combat

- A rider's attack order that the rider cannot carry out **waits natively** until the rider can act; the mount does not
  attack in the rider's place for that order. Native interrupts the attacks of a `CanNotAttack` actor. This is intended
  and unchanged ([0.2.11](RELEASE-NOTES-0.2.11.md), [Sprint 6 A04](KNOWN-ISSUES-SPRINT6-HANDOFF.md#a04--automatic-proposal-churn-established-corrected)).
- **Since 0.2.12:** with AI on, the party brain's automatic attack proposals for a rider that cannot attack are refused
  instead of being admitted and interrupted every 0.3–0.4 s (A04); only a genuine approach failure marks an automatic
  target unreachable (A05).
- The mounted ranged-movement penalty does not apply in real time.

### Saving and loading

- Manual, Quick and Auto saves work while mounted ([0.2.0](RELEASE-NOTES-0.2.0.md)). Loading during mounted combat from
  a running game works; a load is refused, with a message, while a save is being written or queued or while a pair is
  finishing a charge ([0.2.5](RELEASE-NOTES-0.2.5.md)).
- A pair waiting to remount (during an event, camp or world-map travel) is saved in the `kmc-retained-intent` member
  and restored after loading ([0.2.3](RELEASE-NOTES-0.2.3.md)).
- **Since 0.2.12:** after an area arrival whose views are not yet active (a world-map arrival), the pair waits unattached
  and remounts once ordinary play has settled, instead of being dismounted (S02). Its arrival placement survives a save
  and load through retained-intent version 2 (see [Rollback](#rollback-and-downgrade)).
- **Unresolved:** a saving failure reported after the Ratnook door in 0.2.4 has no established cause (S01). Diagnostics
  were added on main; no save behaviour changed.
- **Since 0.2.15 (offline-tested, not played):** a held save names what it waits for and why it stopped (a menu, dialog,
  cutscene, loading screen, replaced world or the 30-second limit), and the log records each held save
  (`Mounted save wait`). Opening a menu while a save waits ends that attempt; the previous complete save is unchanged.
  New actions, doors included, are held only while a save waits. **Unresolved:** the post-combat save and door lock (X05)
  has no established cause.

### Settings and diagnostics

- No options page; UMM shows only its own controls ([0.2.2](RELEASE-NOTES-0.2.2.md)). The Mounted Combat reaction has
  its own in-game toggle.
- The other configuration values are fixed for the supported preset (`DiagnosticSettings`: paired activation, Mounted
  Charge and transport movement on; the legacy unified turn, the paired command scheduler and the diagnostic overlay
  off).
- **Ordinary play versus automation:** the binary contains the repository's diagnostic scenario code. It runs only
  when the game is started with the guarded runtime request argument (`RuntimeAutomationHost`). Native qualification
  evidence came through those paths; they are not a player feature, and their command-line options are not settings.

## Retained adaptations and intentional limits

### Cutscenes, dialogues and scene staging

Final accepted behaviour ([0.2.8 notes](RELEASE-NOTES-0.2.8.md), [Sprint 4](KNOWN-ISSUES-SPRINT4-HANDOFF.md)):

- Dialogue, camera moves and input locks keep riders seated. Mount, Dismount and attack orders wait until normal play
  resumes. A hostile dialogue into combat enters the fight mounted.
- Only a scene that actually moves, hides, teleports or animates the rider temporarily dismounts that one pair; the
  scene's placement stands and the pair remounts afterwards without a Mount action.
- **Since 0.2.12 (C02):** a scene's rider-only placement of 3 m or less is honoured when it is attributable to the exact
  scene operation. Before, it could leave the rider on the horse (a limit listed in the 0.2.8 notes). An action run by
  another handler inside a cutscene's open scope counts as the cutscene's.
- **Safe limits kept (C03):** a pair still staged when combat starts fights unmounted and remounts after combat, with
  one notice per custody and encounter (the notice is since 0.2.12). Restore needs the rider within 2 m of the mount;
  regrouping walks the mount at most twice, 20 s of game time each, up to 30 m, in the same navigation area, out of
  combat only; a scene that keeps control for 60 s of settled play drops the pair with one message. Nothing teleports
  across a navigation boundary.
- **Unsupported (C01):** a scripted movement command on a seated rider **during combat** waits for the scene's own
  timeout; the rider then returns to the saddle once. The staging lifecycle cannot take an actively participating pair
  out of combat while preserving restoration intent ([blocker](KNOWN-ISSUES-SPRINT4-HANDOFF.md#c01--scripted-rider-movement-during-combat)).
- **Unsupported (designer `RunCommand`):** a designer action that moves a seated rider outside any scene cannot complete
  while mounted. It is in the same class as C01 and is unexercised ([Sprint 3 closeout](KNOWN-ISSUES-SPRINT3-CLOSEOUT.md)).

### Shooting from the saddle and Mounted Archery

The final rule ([Sprint 5 R02](KNOWN-ISSUES-SPRINT5-HANDOFF.md#the-final-ranged-movement-rule-r02)), effective from
0.2.10 (0.2.9 never applied it):

- **Scope:** the active rider of a mounted pair, **turn-based only**. Real time is native.
- **Attacks:** ranged weapon attacks with a ranged, non-natural weapon the rider wields: bows, crossbows, slings,
  thrown weapons and firearms, including Gunslinger firearms and Dead Shot, with no Gunslinger dependency. Excluded:
  spell rays, touch spells, kinetic blasts, bombs, melee, natural attacks and the mount's own attacks.
- **Travel basis:** the mount's normal locomotion in the pair's current shared activation,
  `TimeMoved − TimeMovedInForceMode − TimeMovedByFiveFootStep`. Five-foot steps and Charge do not count. Riderless
  travel, travel before mounting, earlier rounds and other pairs never count.
- **Threshold:** a Move is 3 s of movement time; the penalty applies once travel exceeds **3.25 s**. The 0.25 s is one
  native turn-based tick that native may grant as borrowed time at the end of a move, so up to 0.25 s (about 1.3 m for
  a 50 ft horse) of genuine second-Move travel goes unpenalised.
- **Penalty:** **−4**, or **−2** with Mounted Archery (Mobility 1 rank and Mounted Combat). Decided when each roll is
  made; earlier rolls stay final.
- **Not implemented:** Run (Kingmaker has none), PF1 midpoint resolution, Mythic Mounted Archery, and any real-time
  window.
- **Native attack floor:** the game's difficulty-based floor on player attack rolls can absorb part of the penalty
  for a low-bonus rider.
- **Initiative (R01):** as in [Turn-based combat](#turn-based-combat): while mounted, the mount's independent turn
  candidate is suppressed and the pair acts through one shared activation, so there is no separate-slot carried
  travel to count. Closed as not reproducible; no product change.

### Presentation (since 0.2.12, no screen viewed)

All four are projections of current state onto native UI. None changes an action, cost, grant, save, initiative or
selection ([Sprint 7](KNOWN-ISSUES-SPRINT7-HANDOFF.md)). No screen was viewed and no physical input was used.

- **Post-Charge movement display (U01):** after forced Charge travel the mount's Move and Standard slots show 0 usable
  distance and no five-foot step for that activation. Presentation only.
- **Targeting-slot glow (U02):** the public slot whose click handed targeting to a hidden delivery ability is drawn
  selected while it targets. Presentation only.
- **Paladin class panel (U03):** the class panel on the character screen omits the six Weapon Bond upgrades for a
  Divine Mount character or a bond not yet chosen. Per-level tooltips and the console screen are not changed.
- **Mounted badge and tracker hover (U04):** a mounted rider's party portrait carries a saddle badge; hovering a folded
  mount in the world highlights its rider's existing turn-tracker entry. A portrait hover does not reach the tracker,
  by the game's design.

### Paladin Divine Mount

- Divine Bond at Paladin level 5 offers Weapon Bond or Divine Mount ([0.2.0](RELEASE-NOTES-0.2.0.md)). A Paladin already
  past level 5 keeps the stock bond; the native respec is the route to change.
- A Divine Mount is an ordinary persistent companion with celestial defences, not a full celestial template. Celestial
  Smite has one current target per mount and lasts one day.

## Removal and save dependencies

### Which campaigns need KMC

KMC treats a save containing registered KMC content as dependent on KMC. Removing KMC from that campaign is
unsupported while those references remain. Loading such a save without KMC was not tested; its exact outcome is
unknown. The removal preparation refuses to certify the campaign, naming each reference, when any of these is
present (`MountedRemovalPreparation`):

- a KMC Horse or Riding Dog unit, or a character with the KMC Horse or Riding Dog companion or advancement feature;
- the KMC Divine Bond choice (whether Weapon Bond or Divine Mount was taken through it), the Divine Mount bond, Divine
  Mount benefits on a mount, or a Celestial Smite mark on any unit;
- the Mounted Combat feat;
- the Mounted Archery feat.

After writing the cleanup save it also scans every member of that save for any registered KMC blueprint reference and
refuses if one remains.

### Cleanup save versus a campaign with KMC content

- **Reference-free cleanup save:** Ctrl+Shift+F11 in game, then confirm ([README](../README.md#before-removing-the-mod)).
  KMC dismounts every pair, writes a new save named `KMC_CLEANUP`, and reports "Prepared: …" only if that written save
  holds no mounted pair, no combat participation, no KMC control binding and no KMC blueprint reference. Only then
  delete `Mods\KingmakerMountedCombat\` and load that save. Historical native evidence: a mounted stock-Mammoth
  campaign was cleaned and loaded with KMC absent (Chunk 7 F7 rows 31–32). The Ctrl+Shift+F11 shortcut itself was not
  tested in game ([0.2.2 notes](RELEASE-NOTES-0.2.2.md)).
- **A campaign still containing KMC content:** removing KMC is **not supported**. KMC does not remove its features,
  feats, companions or bond choices, and no tool strips them. Never edit a save to strip mod data. Loading such a save
  without KMC was not tested; its exact outcome is unknown, so keep KMC installed for that campaign.
- **Disabling in UMM** dismounts every pair but writes and checks nothing. It is not removal.

### No migration exists for removal

- There is no uninstall migration and no save stripping.
- The only migrations are in-memory reads of KMC's own older metadata: `kmc-mounted-state` schema 1 or 2 is read and
  written back as schema 3 on the next save (`MountedArchiveCodec`); `kmc-retained-intent` version 1 is read unchanged.

## Rollback and downgrade

**Before any upgrade, back up** `%USERPROFILE%\AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\Saved Games`.
Rollback means that pre-upgrade backup plus the matching older package and its dependencies
([0.2.0 notes](RELEASE-NOTES-0.2.0.md#upgrade-rollback-and-removal)). Match the older package by its recorded SHA-256
(on its GitHub release), not by its version label: every release from 0.2.0 to 0.2.12 has assembly version
0.1.0.0, and development builds of main between `v0.2.11` and `v0.2.12` also said 0.2.11.

| Downgrade | What happens | Source |
|---|---|---|
| Any save with Mounted Archery, to 0.2.9 or earlier | Unsafe: the save needs 0.2.10 or later | [0.2.10 notes](RELEASE-NOTES-0.2.10.md) |
| Any save with other KMC content, to a build without it (or to no KMC) | Unsafe and untested | [Removal](#which-campaigns-need-kmc) |
| A save written by 0.2.12 (or a development build of main after `v0.2.11`) holding a pair deferred at arrival (`KMC-RETAINED-INTENT 2`), to released 0.2.3–0.2.11 | The older reader refuses the version 2 text by its header; every pair in it loads dismounted. Nothing else in the save is affected by this member. | `MountedRetainedIntentRecord`; [Sprint 2](KNOWN-ISSUES-SPRINT2-HANDOFF.md) |
| A save holding retained intent (any version), to 0.2.0–0.2.2 | The member is ignored; the waiting pair loads dismounted | [0.2.3 notes](RELEASE-NOTES-0.2.3.md) |
| A save written by 0.2.0 or later (schema 3), to a build before Chunk 7 (preview.211 or earlier) | Not supported. Such a reader classifies schema 3 as future data and restores no pair | [0.2.0 notes](RELEASE-NOTES-0.2.0.md), [Chunk 7 guide](CHUNK7-MIGRATION-ROLLBACK-REMOVAL.md) |
| Between 0.2.0 and 0.2.12 otherwise | The `kmc-mounted-state` format is unchanged across these releases | Each release's notes |
| 0.2.15 to 0.2.14 | The save format is unchanged; 0.2.14 reads 0.2.15 saves | [0.2.15 notes](RELEASE-NOTES-0.2.15.md) |

A current reader that meets a newer `kmc-mounted-state` schema reports "Mounted metadata uses an unsupported schema;
original data is preserved." and restores no pair from it (`MountedArchiveCodec`).

## Still unresolved

The same set as the [ledger](CURRENT-KNOWN-ISSUES.md#every-id):

- **S01:** the saving failure reported after the Ratnook door in 0.2.4; cause not established.
- **M04's original incident:** the reported extra green marker is not attributable, though every identified marker
  defect is repaired (since 0.2.12).
- **C01:** in-combat scripted movement of a seated rider, an unsupported path (see
  [Cutscenes](#cutscenes-dialogues-and-scene-staging)).
- **X01 (after release 0.2.12):** fixed in 0.2.13; native verification beside 0.0.191, the reported save and
  gameplay remain open (see [Game, loader and dependencies](#game-loader-and-dependencies)).
- **X02–X04 (after release 0.2.13):** released in 0.2.14 and not played in the game. X04's original report, a mounted
  ranged rider unable to attack a *prone* enemy, is unreproduced; only an explicit attack on an already-unconscious
  enemy is corrected ([ledger](CURRENT-KNOWN-ISSUES.md#after-release-0213)).
- **X05 (after release 0.2.14):** the post-combat save and door lock. 0.2.15 adds its diagnostics and accurate save
  messages; the cause is not established and nothing was played in the game
  ([ledger](CURRENT-KNOWN-ISSUES.md#after-release-0214)).
- **T04–T08:** stale developer test harnesses (open correction). **T09:** a harness that needs retained native
  evidence (open investigation). Neither affects play.

## Not supported, and excluded from the epic

Not implemented, and not a commitment:

- Cavalier; barding; enemy mounted AI; borrowed, summoned, flying or exotic mounts; a full celestial template;
- Run; Mythic Mounted Archery; a real-time mounted ranged penalty;
- general removal of KMC content from dependent saves;
- six-pair combat qualification.

Kept visible as evidence gaps, outside the epic (no sprint task): WORLD-02 mounted trap disarm; turn-based doors;
Paladin and Riding Dog gameplay without Call of the Wild; prone-rider save/load and unusual fall combinations; physical
UI, other resolutions and the cleanup shortcut ([UI polish handoff](UI-POLISH-REVIEW-HANDOFF.md)); the owner's full
personal mod stack; native acceptance of the released 0.2.3–0.2.11 repairs.

## Reporting a problem

The mod writes to UMM's log, `<Kingmaker install>\Kingmaker_Data\Managed\UnityModManager\Log.txt`, each line prefixed
`[KingmakerMountedCombat]`. Copy it before the next launch, keep the save, and note the version, turn-based or real
time, and the steps ([README](../README.md#logs-and-problems)).
