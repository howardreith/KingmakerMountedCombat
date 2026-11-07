# Kingmaker Mounted Combat 0.2.0: first full version

Released by owner order on 2026-11-07, after the six finalization sprints (F1–F6) were accepted and merged. This version follows the alpha series that ended with Sprint 9 (`0.1.0-chunk7b-preview.103`). Unity Mod Manager reads every alpha label as `0.1.x`, at most 0.1.7.103, so it reads `0.2.0` as newer than all of them.

The package and its hashes are on the GitHub release. The engineering record is [the F6 handoff](FINALIZE-F6-REVIEW-HANDOFF.md), top section, which links F1–F5.

## Supported

- **Game and loader:** Kingmaker with Unity Mod Manager; no other mod is required.
  - **Tested engine build: 2.1.7b only.** `Info.json` declares 2.1.7.
  - The lab ran with the test fixture's own dependencies (Call of the Wild and Craft Magic Items). Neither a KMC-only game nor a full personal mod stack has been verified end to end.
- **Mounts:** your companion can be ridden. That covers the KMC Horse or Riding Dog (from a Ranger's companion choice or a Paladin's Divine Bond) and a supported stock Mammoth.
- **Riders:** Medium riders on the Horse or Mammoth; Halfling and Gnome riders on the Horse or Riding Dog.
- **Pairs:** up to six mounted pairs. Two pairs are tested in combat; six are tested for exploration and saving only.

## What the alpha series added (Sprints 1–9)

- **Saving:** Manual, Quick and Auto saves work while mounted.
- **World:** one-click doors and carried touch spells, with fair movement speed.
- **Combat:** ordinary clicks, native attacks and Haste, retargeting on a kill, Stop and Hold.
- **Small riders:** proper seating, plus the Riding Dog.
- **Feats and classes:**
  - the Mounted Combat feat, with its hit-negation reaction;
  - Mounted Charge, in real time and turn-based;
  - Divine Bond at Paladin level 5, offering Weapon Bond or Divine Mount.

## What the finalization sprints added (F1–F6)

- **Level-up and presentation (F1):**
  - On the level-up feat screen, the Mounted Combat row now refreshes and reads correctly when ranks change.
  - The mod's player-facing text has been rewritten, and its actions have distinct icons.
- **One Mount / Dismount control (F2):**
  - It is placed on the toolbar once, toggles between Mount and Dismount, and is remembered by saves.
  - Charge is one ordinary ability.
  - An old toolbar row from earlier builds is migrated in place.
- **Turn-based movement (F3):**
  - The mount owns the pair's movement.
  - With the rider selected, the action panel's five-foot, Move, Standard and full-round slots show the mount's actions.
  - The rider can full-attack while the mount still moves legally in the same turn, and both initiative arrangements are respected.
- **World orders and AI (F4):**
  - Clicking a chest while mounted carries the rider over to open it.
  - Entering a dungeon from the world map keeps the pair mounted.
  - Mounted archers keep shooting after a kill, and mounted melee follows enemies that step away.
  - The Horse runs with the party.
- **Names and tack (F5):**
  - A companion's character sheet has **Rename** and **Default Name** buttons. Names are saved, and Buff Planner shows them.
  - The KMC Riding Dog wears a fitted saddle. It is visual only.
- **Rider falls (F6):** when a ridden mount dies, collapses or falls prone, its rider is thrown once.
  - The rider makes a native **Mobility check against DC 15**, with the game's own difficulty adjustment.
  - **Success:** a safe landing, with no damage.
  - **Failure:** 1d6 damage and prone.
  - Each fall gets one combat-log line.
  - In turn-based play, a thrown rider stands up on its next turn, and standing takes its Move action.
  - The fall itself spends and refunds no action. Losing the mount does end the pair's interrupted turn.
  - Voluntary dismount, loading a save and leaving an area never throw anyone, and a mount that dies after already throwing its rider does not throw it again.

## Settings

Open them in UMM with Ctrl+F10.
- **Enable Mounted Charge** is on by default. It can change only outside combat with every pair dismounted, and it is not saved.
- The Mounted Combat reaction has its own in-game toggle.

## Known limits

- **Traps (WORLD-02):** riding up to disarm a trap has not been tested in game and has not passed, so mounted trap disarming is not claimed as supported.
- **Turn-based doors** have not been demonstrated.
- **Base game only (without Call of the Wild):** Paladin and Riding Dog gameplay has not been verified.
- **Rider falls:** collapse to unconscious and unusual condition or immunity combinations are covered by offline tests, not every in-game combination. A save made while a thrown rider is still prone has not been loaded in a test.
- **Interface:** the tests drive the game's controllers, not a physical mouse and keyboard. Some screens have not been exercised by a test: the rename buttons, the portrait tooltip, some combat-log and tooltip screens, and Buff Planner's screens.
- **Polish still open:**
  - After a rider's attack, a banner may say the rider has no Standard action left; it spends nothing.
  - After a Charge, the mount's greyed Move slot still shows distance.
  - There is no mount badge on the rider's portrait.
  - The selected-slot glow is missing while a swapped slot picks a target.
- **Adaptations:**
  - Mounted Charge is the rider's charge, with the mount paying its travel.
  - A Divine Mount is an ordinary persistent companion with celestial defenses, not a full template.
  - Celestial Smite has one current target per mount and lasts one day.
- **Not supported:**
  - other mods, beyond the fixture's Call of the Wild and Craft Magic Items;
  - borrowed, summoned, flying or exotic mounts;
  - barding;
  - Cavalier;
  - enemy mounted AI;
  - six-pair combat.
- **Style:** the owner's sign-off on art and full-stack play is separate from this engineering release.

## Upgrade, rollback and removal

- **Upgrade:**
  1. Back up `Saved Games`.
  2. Replace the `Mods\KingmakerMountedCombat\` folder with this package's, then load.
- **Older characters are not converted.** A Paladin already past level 5 keeps the stock bond; the native respec is the route to change.
- **Rollback** means your pre-upgrade backup plus the matching older package and its dependencies.
  - Saves written by F2-era or later builds, including this one, are not supported by older builds.
- **These can tie a save to this mod:**
  - a custom Horse or Riding Dog;
  - the Mounted Combat feat;
  - Divine Bond, including Weapon Bond taken through the mod's level-five selection.
- **Companion names** use the game's own name field.
- **Clean removal from a save that used this mod's content is unproven.**
  - **Prepare to disable / remove KMC** on the UMM page refuses and names what it finds.
  - Never edit a save to strip mod data.

## How this binary relates to the testing

- In-game testing ran on the finalization candidates named in each handoff. The last was F6's f6f (DLL `87fed161…`).
- This 0.2.0 build changes only the version label; gameplay code is identical to the accepted F6 source.
- The release DLL was compared with f6f and was not itself run in the game.
- The assembly version stays `0.1.0.0`, so existing saves resolve the mod's types exactly as before.
