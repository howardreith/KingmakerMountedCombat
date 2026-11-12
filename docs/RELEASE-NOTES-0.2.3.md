# Mr. Bard's Kingmaker Mounted Combat 0.2.3

Released by owner order on 2026-11-12. This update to [0.2.2](RELEASE-NOTES-0.2.2.md) repairs exploration problems
the owner reported in play. Everything 0.2.0's notes say about features, support, known limits, saves and rollback
still applies: [0.2.0 notes](RELEASE-NOTES-0.2.0.md).

## Fixed

- **World map no longer blocks entering areas after leaving mounted.** Before, leaving a local area while mounted,
  arriving on the world map, and then trying to enter any location was refused ("Area change refused: a mounted
  activation ... still owns this world boundary"). Mounted pairs now travel across the world map and remount on
  arrival in the next area. Saving and reloading on the world map keeps that, including the Mount/Dismount button's
  hotbar slot.
  - **If a campaign is already stuck:** load a save made inside a local area (not on the world map), then continue.
- **Scripted events no longer leave you dismounted** (for example the Miner's Lever cart in the Old Sycamore, other
  locked cutscenes and dialogues). The rider steps down for the event and remounts automatically afterwards, with no
  Mount action and no action cost.
- **Camping works while mounted.** Riders and mounts walk to camp, the rest proceeds normally, and the pair remounts
  afterwards. Cancelling the camp also remounts. Native rest rules, supplies, effects and ambushes are unchanged; an
  ambush leaves the pair dismounted (use the combat Mount control if you want to ride into that fight).
- **Looting corpses while mounted.** Clicking a corpse now carries the rider to it on the mount and opens the normal
  loot window.
- **Item cat (cosmetic familiar).** A mounted rider's cat now follows the mount on the ground instead of swinging
  around with it.

## Automatic remount: when it does not happen

The pair stays dismounted, with one message, if the event ends in combat, either unit died or left the party, the
pairing changed, or the event moved rider and mount far apart. Mount again as usual.

## Not changed

- An extra green movement marker and a mounted horse falling behind the party were reported but could not yet be
  reproduced. This build writes "Mounted exploration observation" lines to the game log to help find the cause.

## Upgrade

1. Back up `Saved Games`.
2. Close the game. Replace the `Mods\KingmakerMountedCombat\` folder with this package's.

Saves made with 0.2.0–0.2.2 load in 0.2.3. A save made with 0.2.3 while a pair is waiting to remount (during an
event, a camp or world-map travel) has one extra entry (`kmc-retained-intent`). Earlier versions ignore it, and the
pair then loads dismounted. The assembly version stays 0.1.0.0.

## How this binary relates to the testing

- **Not run in the game.** The repairs were reviewed three times and pass the offline checks (1,135 component tests,
  source validation), but no in-game run was possible during development. The owner is testing this release in the
  campaign where the problems were found.
- **Engineering record:** [the exploration repair handoff](EXPLORATION-REPAIR-HANDOFF.md) (PR #27, merged as
  `ff183945`).
