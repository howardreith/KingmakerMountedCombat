# Mr. Bard's Kingmaker Mounted Combat 0.2.7

Released by owner order on 2026-11-12. This update to [0.2.6](RELEASE-NOTES-0.2.6.md) fixes a mounted horse walking
behind a running party. Everything 0.2.0's notes say about features, support, known limits, saves and rollback still
applies: [0.2.0 notes](RELEASE-NOTES-0.2.0.md).

## Fixed

- **The horse falling behind the party during exploration.**
  - **Before:** on a long group move (about 13 m or more) with the game's Accelerated Move option on, the party broke
    into a run while the ridden horse kept walking at the group's walking pace. In the reported log it averaged about
    2.8 m/s against the party's 5. Now and then it did run, which made the problem look intermittent.
  - **Cause:** the game speeds up a long group move by telling every *selected* unit to run. While a rider is mounted,
    the mod shows the horse as its rider in the selection and gives the rider's order to the horse. The run
    instruction therefore went to the rider, who has no order of its own, and skipped the horse that was carrying the
    order out.
  - **Now:** when the game tells the selection to run, the horse executing its selected rider's move is told to run
    too, through the game's own rule.
- **What still slows the horse:** the game's own run rule still decides: Accelerated Move switched off, combat, a
  condition that reduces its speed (slowed, entangled, difficult terrain and similar), heavy party load, and short
  moves. Turn-based movement, movement budgets, attacks, door and trap approaches and mounted charges are unchanged.
- **Log:** each long mounted group move writes one "Mounted group pace" line saying whether the horse joined the run,
  and the existing pace summary now also reports whether the horse's and the party's orders ran.

## Not changed

Walking to an area exit, gamepad stick running, and the group's walking pace (still computed from the rider, not the
horse) are unchanged. The issues listed as not changed in the [0.2.5 notes](RELEASE-NOTES-0.2.5.md) remain.

## Upgrade

1. Back up `Saved Games`.
2. Close the game. Replace the `Mods\KingmakerMountedCombat\` folder with this package's.

Saves are unchanged from 0.2.6: saves made with 0.2.0–0.2.6 load in 0.2.7, and the save format is the same. The
assembly version stays 0.1.0.0.

## How this binary relates to the testing

- **Not run in the game.**
  - The cause was traced from the reported log through the game's and the mod's source.
  - Offline checks pass against the installed game and mod-manager assemblies: component tests (seven for this fix),
    source validation, package validation, and new checks that the game's run rule is unchanged.
  - No in-game run was possible: this machine has no mounted-combat test save.
- **Engineering record:** [the horse group pace handoff](HORSE-GROUP-PACE-REPAIR-HANDOFF.md) (PR #35, merged as
  `2e97cbd4`).
