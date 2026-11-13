# Mr. Bard's Kingmaker Mounted Combat 0.2.8

Released by owner order on 2026-11-12. This update to [0.2.7](RELEASE-NOTES-0.2.7.md) fixes two problems. Mounted
characters no longer dismount at the start of every dialogue and cutscene, and Mount no longer stays disabled after an
earlier scene. Everything 0.2.0's notes say about features, support, known limits, saves and rollback still applies:
[0.2.0 notes](RELEASE-NOTES-0.2.0.md).

## Fixed

- **Dismounting whenever a dialogue or cutscene began.**
  - **Before:** every dialogue and every cutscene dismounted every mounted pair at its start. The pair tried to remount
    afterwards. During combat, or when a fight followed a hostile dialogue, it stayed on foot.
  - **Now:** dialogue, camera moves and input locks keep the rider seated. Mount, Dismount and attack orders still wait
    until normal play resumes.
  - **Hostile dialogue into combat:** the pair enters the fight mounted, with no Mount action.
  - **Scenes during combat:** the pair stays mounted, and the current turn and its spent actions are unchanged.
  - **Scenes that move the rider:** only a scene that actually moves, hides, teleports or animates the rider
    temporarily dismounts that one pair; other pairs keep riding. The scene's placement of the rider is kept. Once the
    scene lets go, the pair remounts without a Mount action. If the scene left the horse behind (up to 30 m away), the
    horse first walks to the rider; the rider is moved at most 2 m when reseated.
  - **A scene moving only the horse:** the rider rides along.
  - **A pair still temporarily dismounted when a fight starts:** it fights on foot and remounts after the fight, with
    no Mount action or refund.
- **Mount disabled for the rest of the session after a scene** (reported at Bald Hilltop).
  - **Before:** once any scene had controlled the rider or the horse, Mount and Dismount stayed disabled with "Mounting
    is blocked during loading, area transitions, and cutscenes" until the game was reloaded.
  - **Cause:** the game keeps a per-character scene-control record and never clears it after the scene ends, and the
    mod treated that record's existence as active control.
  - **Now:** Mount and Dismount are refused only while a scene is actually controlling this rider or horse, during
    loading, under the cutscene input lock and in cutscene modes. Availability returns as soon as the scene ends or is
    skipped.
- **Log:**
  - one line when mounted pairs are kept through a dialogue or cutscene;
  - one line per pair handed to a scene, with any refusal and its reason;
  - lines for the horse's walk back and for the remount, or why the remount was refused;
  - when Mount or Dismount is unavailable, one line per change of reasons, naming what blocks it.

## Not changed and known limits

- **During combat**, a scene that moves the rider is not handed the rider; the rider stays mounted.
- **A scene teleport of the rider by 3 m or less** leaves the rider on the horse.
- **Remounting after a scene** is skipped if the horse cannot get within 2 m of the rider, or the two end up more than
  30 m apart or in unconnected areas. The pair stays dismounted and the log says why.
- **Rest, camp, world-map travel, area transitions and saving** keep their 0.2.7 behaviour.
- **Earlier issues:** the issues listed as not changed in the [0.2.5 notes](RELEASE-NOTES-0.2.5.md) remain.

## Upgrade

1. Back up `Saved Games`.
2. Close the game. Replace the `Mods\KingmakerMountedCombat\` folder with this package's.

Saves are unchanged from 0.2.7. Saves made with 0.2.0–0.2.7 load in 0.2.8, and the save format is the same. A save
made in 0.2.7 while a pair was dismounted for a dialogue remounts under the new rules. The assembly version stays
0.1.0.0.

## How this binary relates to the testing

**Not run in the game.** No in-game run was possible: this machine has no mounted-combat test save, so Bald Hilltop and
the scenes were not tested.
- **How the causes were found:** traced through the installed game's and the mod's source. Neither occurrence was
  reproduced in game.
- **Offline checks** pass against the installed game and mod-manager assemblies:
  - component tests, 17 of them for these fixes, with mutation checks showing they catch the old behaviour;
  - source validation and package validation;
  - new checks of the game's scene, cutscene-lock and scene-control behaviour.
- **Reviews:** a self-review and an owner review requested changes, and every finding was corrected before merge.

**Engineering record:** [the cutscene continuity handoff](CUTSCENE-CONTINUITY-REPAIR-HANDOFF.md), which also covers the
Bald Hilltop fix (PR #37, merged as `2dbf62c5`).
