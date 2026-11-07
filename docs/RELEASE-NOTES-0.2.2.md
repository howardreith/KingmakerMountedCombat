# Mr. Bard's Kingmaker Mounted Combat 0.2.2

Released by owner order on 2026-11-07. This update to [0.2.1](RELEASE-NOTES-0.2.1.md) polishes the interface and gives
the mod its public name. **Gameplay rules are unchanged.** Everything 0.2.0's notes say about features, support, known
limits, saves and rollback still applies: [0.2.0 notes](RELEASE-NOTES-0.2.0.md).

## Changed

- **Public name:** Unity Mod Manager now lists the mod as **Mr. Bard's Kingmaker Mounted Combat**.
  - The mod ID, folder name, DLL name and save data are unchanged, so existing installs and saves are unaffected.
- **Renaming a companion:** the character sheet has one **Rename** button, at the right end of the companion's name.
  - It no longer covers the level and experience line.
  - A long name shrinks to fit and, at the 24-character limit, ends in "…".
  - **Default Name** is gone. To go back to the original name, type it in.
- **Unity Mod Manager:** the mod no longer has an options page. UMM shows its usual row only.
- **Mounted Charge** is always on while the mod is enabled. It has no switch.
- **Preparing to remove the mod:** this has moved from the UMM page to the game itself.
  - Press **Ctrl+Shift+F11** and confirm. The mod writes and checks a clean save named `KMC_CLEANUP`.
  - The steps, and which campaigns still need the mod, are in the [README](../README.md#before-removing-the-mod).

## Upgrade

1. Back up `Saved Games`.
2. Close the game. Replace the `Mods\KingmakerMountedCombat\` folder with this package's.

Saves move freely between 0.2.0, 0.2.1 and 0.2.2. All three versions hold the same game data and use the same
assembly version (0.1.0.0).

## How this binary relates to the testing

- **In-game testing:** the interface changes were tested in game on the reviewed build
  (`docs/UI-POLISH-REVIEW-HANDOFF.md`), whose source is merged unchanged as `617f2e63`:
  - the Rename control's layout, Confirm and Cancel, per-mount names, and save and reload;
  - the UMM entry;
  - a real-time Mounted Charge.
- **This build:** changes only the version string from that build, and was not itself run in the game.
- **Not tested in game:** the Ctrl+Shift+F11 shortcut. The cleanup it starts is unchanged from earlier versions.
