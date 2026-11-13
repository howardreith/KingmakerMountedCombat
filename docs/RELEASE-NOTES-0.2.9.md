# Mr. Bard's Kingmaker Mounted Combat 0.2.9

Released by owner order on 2026-11-13. This update to [0.2.8](RELEASE-NOTES-0.2.8.md) adds the **Mounted Archery**
feat and a turn-based penalty for shooting from a mount that has moved more than one move action's worth. Everything
0.2.0's notes say about features, support, known limits, saves and rollback still applies:
[0.2.0 notes](RELEASE-NOTES-0.2.0.md).

## New

- **Shooting from a moving mount (turn-based only).** This is a balance change for every mounted rider.
  - **The penalty:** a mounted rider's ranged weapon attack takes **-4** once the mount has used more than one move
    action's worth of normal movement in its current turn.
  - **Which attacks:** bows, crossbows, slings, thrown weapons and firearms the rider wields. Gunslinger firearms and
    Dead Shot are included, with no Gunslinger dependency.
  - **Judged at each attack.** An attack made before the mount's second move takes no penalty, and is never revised by
    later movement. The table shows typical turns.
  - **No penalty for:** five-foot steps, charges, a single move at any speed, standing still, melee, natural attacks,
    spells and the mount's own attacks.
  - **Mounting mid-combat** is covered. Movement the horse made riderless earlier in the round never counts.
  - **The combat log** shows the penalty as "Mounted movement" or "Mounted movement (Mounted Archery)".
  - **Real-time combat** is unchanged.

  | Turn | Penalty |
  |---|---|
  | double move, then shoot | -4 (-2 with Mounted Archery) |
  | move, shoot, move, shoot | none on the first shot, -4/-2 on the second |
  | shoot, then double move | none |
  | one move, a five-foot step, a charge, or standing still | none |

- **Mounted Archery (combat feat).**
  - **Effect:** reduces the penalty to **-2**.
  - **Prerequisites:** 1 rank in Mobility, and Mounted Combat.
  - **Where to take it:** wherever Mounted Combat can be taken (ordinary, fighter and other combat-feat choices), listed
    just after it.
  - **Icon:** original, made for the mod.
- **Compatibility with other mods that change feat lists:**
  - Another mod adding to or reordering a feat list changes nothing.
  - If another mod removes Mounted Archery from the ordinary or fighter feat list, the mod withdraws the feat and its
    penalty for the session, so nobody can take a feat that does nothing. It removes only its own Mounted Archery entry,
    leaves Mounted Combat and the other mod's entries in place, and logs one warning.
- **Log:** one `Mounted ranged movement:` line for each turn-based mounted ranged attack, giving the mount's travel and
  the penalty applied.

## Known limits

- **Rider and mount on separate initiative turns:** no penalty is applied. This is deliberately conservative.
- **The second move's first moment:** up to 0.25 s of travel into the second move (a twelfth of a move) can
  go unpenalized. This is the game's own movement-timing tolerance.
- **Low attack bonuses:** the game's difficulty-based floor on player attack rolls can absorb part of the penalty for
  riders with a low attack bonus.
- **Earlier issues:** the issues listed as not changed in the [0.2.5 notes](RELEASE-NOTES-0.2.5.md) and the limits in
  the [0.2.8 notes](RELEASE-NOTES-0.2.8.md) remain.

## Upgrade

1. Back up `Saved Games`.
2. Close the game. Replace the `Mods\KingmakerMountedCombat\` folder with this package's.

- **Saves:** the format is unchanged. Saves made with 0.2.0–0.2.8 load in 0.2.9.
- **Going back:** a campaign where any character has taken Mounted Archery needs 0.2.9 or later, like the Mounted
  Combat feat (see "Before removing the mod" in the README).
- **Assembly version:** stays 0.1.0.0.

## How this binary relates to the testing

**Not run in the game.** This machine has no mounted-combat test save, so the penalty, the feat and its selection screens
were not tested in game.
- **Game contracts:** the attack-roll and movement timing the rule depends on were traced through the installed game's
  source, and are pinned by checks against the installed game assembly.
- **Offline checks** pass against the installed game and mod-manager assemblies:
  - 1,213 component tests, including the new ones for this feature;
  - mutation checks showing the new tests catch the defects found in review;
  - source validation and package validation.
- **Reviews:** an independent source review and three rounds of owner review requested changes. Every finding was
  corrected before merge.

**Engineering record:** [the Mounted Archery handoff](MOUNTED-ARCHERY-HANDOFF.md) (PR #39, merged as `e06337b0`). It
lists the in-game checks still to run.
