# Mr. Bard's Kingmaker Mounted Combat 0.2.5

Released by owner order on 2026-11-12. This update to [0.2.4](RELEASE-NOTES-0.2.4.md) fixes loading a save from a
running game during mounted combat and clicking an area entrance while mounted. Everything 0.2.0's notes say about
features, support, known limits, saves and rollback still applies: [0.2.0 notes](RELEASE-NOTES-0.2.0.md).

## Fixed

- **Loading a save from a running game during mounted combat.**
  - **Before:** loading from the in-game load window or by quickload during a mounted pair's combat turn was refused
    with "Area change refused". Loading from the main menu always worked. This predates 0.2.4.
  - **Now:** a load is no longer treated as leaving the area. It is still refused, with a message, while a save is
    being written or still queued, or while any mounted pair is finishing a charge; try again once it finishes.
- **Clicking an area entrance while mounted.**
  - **Before:** the party walked to the entrance but the mounted pair stayed put, and the transition never happened.
    It only worked once the party was already standing next to the entrance.
  - **Now:** the horse receives the same entrance order as the rest of the party and carries its rider there.
    Unmounted parties, ordinary moves and doors are unchanged.

## Not changed yet

- **Saving after the Ratnook door.** The reported saving problem is not yet explained: the log shows no save running
  after the door, but not where the attempt stopped. Save behaviour is unchanged. This version logs one line per save
  request (naming anything that refuses it) and one line when a save completes. If saving fails for you, please send
  `output_log.txt` from that session.
- **Rider dismounted on arrival**, the extra green movement marker and the horse falling behind the party remain as
  described in the [0.2.4 notes](RELEASE-NOTES-0.2.4.md).

## Upgrade

1. Back up `Saved Games`.
2. Close the game. Replace the `Mods\KingmakerMountedCombat\` folder with this package's.

Saves are unchanged from 0.2.4: saves made with 0.2.0–0.2.4 load in 0.2.5, and the save format is the same. The
assembly version stays 0.1.0.0.

## How this binary relates to the testing

- **Not run in the game.**
  - Both causes were traced in the installed game code and in the mod's source, and they fit the reported log.
  - The repair had two independent reviews. Its findings were fixed, including charge cleanup on every pair before a
    load and save-completion logging that cannot block saving.
  - Offline checks pass: 1,158 component tests, source validation, persistence and installed-assembly contract checks,
    a detached two-pair load-admission check, and package validation.
  - No in-game run was possible during development.
- **Engineering record:** [the save/load and entrance repair handoff](SAVE-LOAD-ENTRANCE-REPAIR-HANDOFF.md) (PR #31,
  merged as `af138c57`).
