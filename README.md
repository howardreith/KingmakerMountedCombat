# Mr. Bard's Kingmaker Mounted Combat

A Unity Mod Manager (UMM) mod for Pathfinder: Kingmaker (engine build 2.1.7b) that lets a character ride their animal
companion: a Horse or Riding Dog added by the mod, or a supported stock Mammoth. Rider and mount keep their own actions,
health and defenses. No other mod is required.

Release notes: [0.2.0](docs/RELEASE-NOTES-0.2.0.md) (what is supported and the known limits) and
[0.2.1](docs/RELEASE-NOTES-0.2.1.md).

## Install

1. Back up `%USERPROFILE%\AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\Saved Games`.
2. Copy the package's `KingmakerMountedCombat` folder into the game's `Mods` folder, replacing any older copy.
3. Start the game. UMM lists the mod as **Mr. Bard's Kingmaker Mounted Combat**.

The folder, mod ID and DLL are still named `KingmakerMountedCombat`; only the displayed name changed.

## Playing

- **Mount / Dismount** is one ability on the rider. Move, attack and cast with the ordinary controls.
- **Mounted Charge** works whenever the mod is enabled: a mounted rider's ordinary Charge is carried by the mount along a
  clear straight line. The game's usual Charge requirements still apply.
- **Mounted Combat** (the feat) has its own in-game toggle, **Mounted Combat Reaction**.
- **Renaming a companion:** open the character sheet on your animal companion and press **Rename**, at the right end of
  its name. Type the new name and press **Rename** to keep it, or **Cancel** (or Esc) to leave the name as it was. Each
  companion's name is its own and is saved with your game. An empty name is refused; to go back to the original name,
  type it in.

## Settings

The mod has no options page. Its UMM entry offers only UMM's own controls (enable, disable, version).

## Before removing the mod

**Some campaigns cannot be loaded without the mod.** A save still needs it if any character has:

- a Horse or Riding Dog from this mod;
- the Mounted Combat feat;
- Divine Bond taken through this mod's Paladin level-five choice (Weapon Bond included).

Never edit a save to strip mod data. Removing the mod from such a campaign is not supported.

For any other campaign, prepare a clean save first:

1. Load the campaign. Leave combat and let any save or area load finish.
2. Press **Ctrl+Shift+F11** and confirm. The mod dismounts every pair, writes a new save named `KMC_CLEANUP` and checks
   that it holds no mounted pair, no mod control and no mod content.
3. Wait for the result message.
   - **"Prepared: …"** — quit the game, delete `Mods\KingmakerMountedCombat\`, then load the `KMC_CLEANUP` save.
   - **"Not prepared: …"** or **"Preparation unconfirmed: …"** — do **not** remove the mod. The message names the
     cause, for example a companion or feat from this mod. Fix it if you can, then try again.

Turning the mod off in UMM also dismounts every pair, but it does not write or check a save. Use the steps above
before deleting the mod.

## Logs and problems

The mod writes to UMM's log, `<Kingmaker install>\Kingmaker_Data\Managed\UnityModManager\Log.txt`, with each line
prefixed `[KingmakerMountedCombat]`. To report a problem, copy that log before the next launch, keep the save, and note
the version, whether you were in turn-based or real-time combat, and the steps.
