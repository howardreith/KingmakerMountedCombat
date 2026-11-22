# Mr. Bard's Kingmaker Mounted Combat 0.2.14

Released by owner order on 2026-11-22. This update to [0.2.13](RELEASE-NOTES-0.2.13.md) keeps the Mount / Dismount
shortcut where you put it, lists each companion's animal in the capital's inventory and character screens, and lets a
mounted rider shoot an enemy that was already unconscious. The current support, limits, removal and rollback contract
is [CURRENT-SUPPORT.md](CURRENT-SUPPORT.md). Every issue's status is in the [current issue ledger](CURRENT-KNOWN-ISSUES.md).

**None of these three changes was played in the game before release.** Startup was checked in the game; the behaviour
was checked by offline tests only (see [Testing](#testing)).

## Fixed

- **The Mount / Dismount shortcut lost its place on the action bar (X02).**
  - **Before:** a Mount / Dismount shortcut placed in a slot of your choice could disappear or move. This happened after
    visiting the capital, after a companion spent time in the capital's reserve roster, after saving there, or after the
    horse was briefly unavailable.
  - **Now:** Mounted Combat remembers your arrangement for each rider while the control is temporarily unusable. It puts
    the shortcut back in the same slot once, when the same companion is back. It respects your own edits:
    - a shortcut you removed stays removed;
    - a shortcut you moved stays in its new slot;
    - another ability you put in the old slot is never overwritten;
    - a second copy of the control is reduced to one.

    A companion that is replaced or released starts fresh, so the old slot stays empty for the new one.
  - **Your save:** the save format is unchanged. A capital save from an earlier version that still shows the shortcut on
    a reserve companion's bar keeps it in that slot.
- **Animal companions were missing from the capital's Inventory and Character screens (X03).**
  - **Before:** in the capital, a mercenary or custom companion's Horse (or other animal companion) could not be selected
    in the full-screen Inventory, Character or Spellbook screens. The game leaves out the pets of companions waiting in
    the capital.
  - **Now:** each listed companion's own animal companion appears right after its owner while Mounted Combat is enabled.
    It is the same animal, not a copy, so its equipment and details are its own. Nothing else changes: party membership,
    formation, experience and combat stay as they were. Disabling Mounted Combat removes the extra entries.
- **A mounted rider's attack on an enemy that was already unconscious was refused (X04).**
  - **Before:** clicking an enemy that had already fallen unconscious to shoot it from the saddle did nothing, although
    the same rider on foot could attack it.
  - **Now:** an attack you order yourself on a living, unconscious enemy proceeds as it does on foot. Dead enemies are
    still refused. Automatic target choice, Mounted Charge and attack continuation still pick only conscious enemies.
  - **Still open:** the original report said a mounted ranged rider could not attack a *prone* enemy. That has not been
    reproduced. It is not known whether that enemy was conscious and prone, or already unconscious. If you can still
    reproduce it on 0.2.14, please report whether the enemy was standing up again, lying awake (for example tripped) or
    unconscious, and whether you clicked the body or the spot where it had stood.

## Unchanged

Everything else is as in [0.2.13](RELEASE-NOTES-0.2.13.md), including its retained limits, the shooting-from-the-saddle
rule, the Tabletop Rules Expansion compatibility and the still-open items listed there.

## Upgrade

1. Back up `Saved Games`.
2. Close the game. Replace the `Mods\KingmakerMountedCombat\` folder with this package's.

### Saves

- Saves made with 0.2.0–0.2.13 load in 0.2.14. 0.2.14 writes the same save format as 0.2.13.
- **Going back:** 0.2.13 reads 0.2.14 saves; it then follows its own toolbar rules again. Rollback otherwise means your
  pre-upgrade backup and the older package ([details](CURRENT-SUPPORT.md#rollback-and-downgrade)).

## Testing

- **Offline:**
  - **Toolbar:** one shared component now decides where the shortcut goes. 53 toolbar tests drive it through capital
    and area changes, the reserve roster, saving and loading, companion loss and replacement, and the game's own slot
    clean-up. A fixture drives the actual game-side adapter (12 checks).
  - **Patches:** the new action-bar patches were run against the installed game library, with the game's clean-up
    simulated.
  - **Test strength:** 41 deliberate breakages of the production code are each caught by a named test.
  - **Shared checks:** the full component suite passes 1,473/0, along with source, contract and package validation.
  - **Reviews:** the changes passed several independent source reviews. Their findings were reproduced, corrected and
    accepted before release.
- **In the game, startup only:** this release's DLL started in a fresh game process beside the 16 other mods
  installed on the test machine, including Tabletop Rules Expansion 0.0.140. Every mod was `Active`, and every Mounted
  Combat patch installed, including the new action-bar patches and the capital screen patch. The guarded harness marked
  the run failed for one reason only: the test machine runs Unity Mod Manager 0.33.0, while the harness expects 0.33.2.
- **Not tested in the game:** the shortcut through a real capital visit, reserve roster and save/load; selecting and
  equipping a companion's animal in the capital screens; and attacks on prone or unconscious enemies. The test machine
  has no disposable save prepared for these, and no automated script drives them.
- **Package:** this release's DLL is rebuilt with the new version number, so its hash differs from the reviewed test
  build (`8de5a104…`, which passed the same startup check). The source is the same except for the version number. The
  release package's hashes are on the GitHub release. The DLL started in the game above is that release DLL.

**Engineering record:** [the handoff](CITY-AND-PRONE-HANDOFF.md) (PR #57, merged into main as `4e5c9c0c`), with the
exact playtest steps for the untested cases.
