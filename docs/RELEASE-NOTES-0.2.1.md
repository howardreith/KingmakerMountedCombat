# Kingmaker Mounted Combat 0.2.1

Released by owner order on 2026-11-07. This is a text-only update to [0.2.0](RELEASE-NOTES-0.2.0.md), and there is no gameplay change. Everything 0.2.0's notes say about support, limits, saves, upgrade, rollback and removal still applies.

## Changed

- **Messages:** the in-game messages and the Horse's description no longer call the mod a "private alpha" or an "experiment". For example, "Private-alpha mounting is available only outside combat." now reads "Mounting is available only outside combat."
- **Mod log:** the startup line in the Unity Mod Manager log reads "Kingmaker Mounted Combat 0.2.1 loaded with mounted combat services."
- **DLL description:** now "Mounted combat for Pathfinder: Kingmaker".

## Upgrade

Replace the `Mods\KingmakerMountedCombat\` folder with this package's. Saves move freely between 0.2.0 and 0.2.1, because the two versions hold the same game data, use the same assembly version, and have identical gameplay code.

## How this binary relates to the testing

- **Gameplay code:** identical to 0.2.0 and to the tested F6 build.
- **What changed:** the text change (PR #23) and the version string.
- **Offline checks:** the offline test tiers passed.
- **In-game:** this build was not run in the game.
