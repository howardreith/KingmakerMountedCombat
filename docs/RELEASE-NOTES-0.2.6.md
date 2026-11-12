# Mr. Bard's Kingmaker Mounted Combat 0.2.6

Released by owner order on 2026-11-12. This update to [0.2.5](RELEASE-NOTES-0.2.5.md) removes misleading warnings
after ordinary mounted attacks. Everything 0.2.0's notes say about features, support, known limits, saves and rollback
still applies: [0.2.0 notes](RELEASE-NOTES-0.2.0.md).

## Fixed

- **A warning after a mounted attack had already finished.**
  - **Before:** often at the end of a rider's full attack, a banner such as "Royon has no Standard action left this
    turn." appeared even though the attack had gone through normally. The same could happen after a single attack, a
    mount's attack, or melee and ranged attacks. It did not appear every time: if the target died or the turn ended
    first, there was no banner.
  - **Cause:** after a successful attack the mod kept the attack order and, on the next update, correctly found the
    action spent. It then mistook the finished attack for a new click that had never been carried out. No extra
    attack was made and no action was spent or refunded: actions were always counted correctly by the game.
  - **Now:** a finished attack ends quietly. A repeating real-time attack that stops because the target, turn,
    action, path or range changed also stops quietly. These stops are written to the log.
- **What still warns:** a new attack click by an actor whose Standard action is already spent (with the hint to
  select the partner if it can still attack), a new target or action, and a click after Stop. A genuine failure to
  start a command, or an attack the current weapon or setup cannot make, still shows its warning, and every refused
  attack dispatch is now logged.

## Not changed

Action costs, full-attack counts, turn order, movement and the paired-turn setup are unchanged. The issues listed as
not changed in the [0.2.5 notes](RELEASE-NOTES-0.2.5.md) remain.

## Upgrade

1. Back up `Saved Games`.
2. Close the game. Replace the `Mods\KingmakerMountedCombat\` folder with this package's.

Saves are unchanged from 0.2.5: saves made with 0.2.0–0.2.5 load in 0.2.6, and the save format is the same. The
assembly version stays 0.1.0.0.

## How this binary relates to the testing

- **Not run in the game.**
  - The cause was traced in the mod's source and matches the reported wording; no in-game log of the warning was
    available.
  - The fix had an independent source review and a further review correction: later warnings are suppressed only for
    routine changes, never for command failures or unsupported setups.
  - Offline checks pass against the installed game and mod-manager assemblies: 1,168 component tests (including ten
    for this fix), source validation and package validation.
  - No in-game run was possible: this machine has no mounted-combat test save.
- **Engineering record:** [the attack completion warnings handoff](ATTACK-COMPLETION-WARNINGS-HANDOFF.md) (PR #33,
  merged as `b8359e31`).
