# Mr. Bard's Kingmaker Mounted Combat 0.2.10

Released by owner order on 2026-11-13. This update to [0.2.9](RELEASE-NOTES-0.2.9.md) fixes the **Mounted Archery**
feat, which 0.2.9 never offered. Everything 0.2.0's notes say about features, support, known limits, saves and rollback
still applies: [0.2.0 notes](RELEASE-NOTES-0.2.0.md).

## Fixed

- **Mounted Archery was missing from every feat list.**
  - **Symptom:** in 0.2.9 the feat never appeared, not even among the unavailable feats, and the log showed:
    `Mounted Archery feat blueprint registration: InvalidOperationException - Mounted Archery did not retain the single-rank combat-feat contract.`
    Mounted Combat and the rest of the mod still worked.
  - **Cause:** the mod builds Mounted Archery from the game's own Power Attack feat. A startup check wrongly required
    one of the game's internal feat flags to be off. Every stock feat in Kingmaker 2.1.7b has that flag on, so the
    check failed on every installation and the mod removed the feat. No other mod was involved.
  - **Fix:** the check now expects the same value as Power Attack and Mounted Combat. The feat itself is unchanged: the
    same prerequisites (1 rank in Mobility, and Mounted Combat), the same effect, icon and text, offered wherever
    Mounted Combat is (ordinary, fighter and other combat-feat choices).
- **The mounted ranged-movement penalty now applies.** The turn-based penalty for shooting after the mount's double move
  (-4, or -2 with Mounted Archery) only runs while Mounted Archery is available. Because the feat was missing, 0.2.9
  never applied it. In 0.2.10 it applies as the [0.2.9 notes](RELEASE-NOTES-0.2.9.md) describe. That is a balance
  change for mounted shooters who have played 0.2.9.
- **Clearer log:** a successful start now logs one `Mounted Archery feat contract:` line. If the feat's startup check
  ever fails again, the error names the exact problem and the values found.

## Known limits

- The limits in the [0.2.9 notes](RELEASE-NOTES-0.2.9.md) and the issues listed as not changed in the
  [0.2.5 notes](RELEASE-NOTES-0.2.5.md) remain.

## Upgrade

1. Back up `Saved Games`.
2. Close the game. Replace the `Mods\KingmakerMountedCombat\` folder with this package's.

- **Saves:** the format is unchanged. Saves made with 0.2.0–0.2.9 load in 0.2.10. No 0.2.9 save can contain Mounted
  Archery, because 0.2.9 never offered it.
- **Going back:** a campaign where any character has taken Mounted Archery needs 0.2.10 or later, like the Mounted
  Combat feat (see "Before removing the mod" in the README).
- **Assembly version:** stays 0.1.0.0.

## How this binary relates to the testing

**Not run in the game.** This machine has no mounted-combat test save, so the feat screen was not checked in game.
- **The cause** was confirmed from the installed game's own data: the stock Power Attack, Dodge, Mobility, Point Blank
  Shot, Rapid Shot and Toughness feats all have the flag on.
- **Offline checks** pass:
  - 1,217 component tests;
  - a new check that runs the mod's own feat construction, startup check and feat-list publication on genuine game
    feat objects built from the stock values;
  - a mutation check showing both new tests catch the 0.2.9 rule;
  - source validation and package validation.

**Engineering record:** [the registration repair handoff](MOUNTED-ARCHERY-REGISTRATION-REPAIR-HANDOFF.md) (PR #41,
merged as `81b3a30a`). It lists the in-game checks still to run.
