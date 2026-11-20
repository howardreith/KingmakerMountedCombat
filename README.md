# Mr. Bard's Kingmaker Mounted Combat

A Unity Mod Manager (UMM) mod for Pathfinder: Kingmaker (engine build 2.1.7b) that lets a character ride their animal
companion: a Horse or Riding Dog added by the mod, or a supported stock Mammoth. Rider and mount keep their own actions,
health and defenses. No other mod is required.

Release notes: [0.2.11](docs/RELEASE-NOTES-0.2.11.md) (current), [0.2.10](docs/RELEASE-NOTES-0.2.10.md),
[0.2.9](docs/RELEASE-NOTES-0.2.9.md), [0.2.8](docs/RELEASE-NOTES-0.2.8.md), [0.2.7](docs/RELEASE-NOTES-0.2.7.md),
[0.2.6](docs/RELEASE-NOTES-0.2.6.md), [0.2.5](docs/RELEASE-NOTES-0.2.5.md), [0.2.4](docs/RELEASE-NOTES-0.2.4.md),
[0.2.3](docs/RELEASE-NOTES-0.2.3.md), [0.2.2](docs/RELEASE-NOTES-0.2.2.md), [0.2.0](docs/RELEASE-NOTES-0.2.0.md)
and [0.2.1](docs/RELEASE-NOTES-0.2.1.md).

## Current status

- **Release:** 0.2.11 is the current release.
- **What is supported, the retained limits, removal and rollback:** [docs/CURRENT-SUPPORT.md](docs/CURRENT-SUPPORT.md).
  It replaces the 0.2.0 notes' known-limits list for current status.
- **Known issues:** [docs/CURRENT-KNOWN-ISSUES.md](docs/CURRENT-KNOWN-ISSUES.md), one current disposition per issue.
  The Known Issues Remediation Epic's corrections are in unmerged draft pull requests (#45 to #53), not in
  0.2.11, and none was run in the game. Its final accounting:
  [docs/KNOWN-ISSUES-REMEDIATION-EPIC-CLOSEOUT.md](docs/KNOWN-ISSUES-REMEDIATION-EPIC-CLOSEOUT.md).
- **Still open:** a saving failure reported after the Ratnook door in 0.2.4 has no established cause; the original
  extra-green-marker report is not attributed; a scripted scene that moves a seated rider during combat waits for the
  scene's own timeout (unsupported). Five developer test harnesses are stale and one needs missing evidence (T04–T09);
  they do not affect play.
- **Not supported:** Cavalier, barding, enemy mounted AI, borrowed, summoned, flying or exotic mounts, Run, Mythic
  Mounted Archery, a real-time mounted ranged penalty, and six-pair combat (two pairs are tested in combat; six in
  exploration and saving).

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
- **Shooting from the saddle (turn-based only):** a mounted rider's ranged weapon attacks take -4 once the mount has
  moved more than one move action's worth this turn, judged when each attack is made. Five-foot steps and Charge do not
  count. The **Mounted Archery** feat (Mobility 1 rank and Mounted Combat) reduces this to -2. Real-time combat is
  unaffected.
- **Renaming a companion:** open the character sheet on your animal companion and press **Rename**, at the right end of
  its name. Type the new name and press **Rename** to keep it, or **Cancel** (or Esc) to leave the name as it was. Each
  companion's name is its own and is saved with your game. An empty name is refused; to go back to the original name,
  type it in.

## Settings

The mod has no options page. Its UMM entry offers only UMM's own controls (enable, disable, version).

## Before removing the mod

**Some campaigns depend on the mod.** The mod treats a save as dependent, and will not prepare it for removal, if
any character has:

- a Horse or Riding Dog from this mod;
- the Mounted Combat feat;
- the Mounted Archery feat;
- Divine Bond taken through this mod's Paladin level-five choice, whichever bond was chosen (Weapon Bond or Divine
  Mount);
- a Celestial Smite mark still active on any creature.

Never edit a save to strip mod data. Removing the mod from such a campaign is not supported: the mod has no tool that
removes its content from a campaign and no migration that does. Loading such a save without the mod has not been
tested, so what would happen is unknown; keep the mod installed for that campaign.

For any other campaign, prepare a clean save first:

1. Load the campaign. Leave combat and let any save or area load finish.
2. Press **Ctrl+Shift+F11** and confirm. The mod dismounts every pair, writes a new save named `KMC_CLEANUP` and checks
   that it holds no mounted pair, no mod control and no mod content.
3. Wait for the result message.
   - **"Prepared: …"** — quit the game, delete `Mods\KingmakerMountedCombat\`, then load the `KMC_CLEANUP` save.
   - **"Not prepared: …"** or **"Preparation unconfirmed: …"** — do **not** remove the mod. The message names the
     cause, for example a companion or feat from this mod. Fix it if you can, then try again.

The check also refuses if the written save still names anything from this mod.

Turning the mod off in UMM also dismounts every pair, but it does not write or check a save. Use the steps above
before deleting the mod.

**Going back to an older version** needs the backup you made before upgrading and the matching older package.
Every release since 0.2.0 has the same assembly version, so the version number does not show whether an older build
can read a save. A campaign with Mounted Archery needs 0.2.10 or later. Details:
[docs/CURRENT-SUPPORT.md](docs/CURRENT-SUPPORT.md#rollback-and-downgrade).

## Logs and problems

The mod writes to UMM's log, `<Kingmaker install>\Kingmaker_Data\Managed\UnityModManager\Log.txt`, with each line
prefixed `[KingmakerMountedCombat]`. To report a problem, copy that log before the next launch, keep the save, and note
the version, whether you were in turn-based or real-time combat, and the steps.
