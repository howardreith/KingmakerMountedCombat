# Mr. Bard's Kingmaker Mounted Combat 0.2.4

Released by owner order on 2026-11-12. This update to [0.2.3](RELEASE-NOTES-0.2.3.md) fixes a door that left the whole
party unable to move. Everything 0.2.0's notes say about features, support, known limits, saves and rollback still
applies: [0.2.0 notes](RELEASE-NOTES-0.2.0.md).

## Fixed

- **Doors that lead to another spot in the same area no longer freeze the party.** This was seen at Ratnook Hill.
  - **Before:** walking through such a door while mounted dismounted the rider. Afterwards no party member could
    move, and saving was blocked.
  - **Now:** the rider steps down for the door and remounts automatically once the party arrives on the other side,
    with no Mount action and no action cost.
  - Doors and exits that load a different area work as before.
- **A door transition that the game never carries out** (for example, refused while a save is still being written)
  now remounts the rider where they stand once the save finishes. Before, it caused the same freeze.
- **If a campaign is already stuck:** load a save made before the door. The fixed version then handles the door
  normally.

## Not changed

- **Rider dismounted on arrival.** On arriving in an area after a loading screen, a rider can occasionally be
  dismounted by a safety check. Mount again as usual. This was seen once at Ratnook Hill, has a different cause, and
  is still being investigated.
- **Still not reproduced:** the extra green movement marker and the horse falling behind the party. The
  "Mounted exploration observation" log lines from 0.2.3 remain.

## Upgrade

1. Back up `Saved Games`.
2. Close the game. Replace the `Mods\KingmakerMountedCombat\` folder with this package's.

Saves are unchanged from 0.2.3: saves made with 0.2.0–0.2.3 load in 0.2.4, and the save format is the same. The
assembly version stays 0.1.0.0.

## How this binary relates to the testing

- **Not run in the game.**
  - The cause was traced in the installed game code and matches the reported log.
  - The repair was independently reviewed and passes the offline checks: 1,147 component tests, source validation,
    and checks against the installed game assembly.
  - No in-game run was possible during development. The owner is testing this release in the campaign where the
    problem was found.
- **Engineering record:** [the Ratnook door repair handoff](RATNOOK-DOOR-REPAIR-HANDOFF.md) (PR #29, merged as
  `9d7f575e`).
