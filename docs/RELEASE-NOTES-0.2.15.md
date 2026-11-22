# Mr. Bard's Kingmaker Mounted Combat 0.2.15

Released by owner order on 2026-11-22. This update to [0.2.14](RELEASE-NOTES-0.2.14.md) makes a held save say what it is
waiting for and why it stopped. It also records the details needed to repair the post-combat save and door lock. The current support, limits,
removal and rollback contract is [CURRENT-SUPPORT.md](CURRENT-SUPPORT.md). Every issue's status is in the
[current issue ledger](CURRENT-KNOWN-ISSUES.md).

**This release does not fix the post-combat save and door lock (X05).** Its cause is not yet known. 0.2.15 names it the
next time it happens. **Nothing in this release was run in the game before release, not even startup** (see
[Testing](#testing)).

## Changed

- **A held save now says what it is waiting for, and why it stopped (X05).**
  - **Before:** after combat, a quicksave could keep waiting with "Saving after current actions and projectiles finish;
    new actions are briefly held". Each attempt then failed with "Save was not written: the native world changed while
    its effects were finishing". While a save waits, new actions are held, so doors would not open.
  - **Now:**
    - when a save gives up because a menu, dialog or cutscene opened, the message names it and what the save was waiting
      for (for example "the game menu opened while it waited for Leinna's AcidSplash projectile");
    - a loading screen and a replaced world are named the same way;
    - a save that runs out of its 30 seconds names what did not finish.
  - **Unchanged:** when a save may proceed, the action hold during the wait, the 30-second limit, the order of queued
    saves and pausing all behave exactly as in 0.2.14.
- **The log records each held save.** `Mounted save wait:` lines name the character, attack, projectile, spell or
  mounted pair the save is waiting for, and when that changes. A `Mounted save wait ended:` line gives the result, how
  long the save waited and how long it was queued.

## If saving is held after combat

1. Quicksave once. Do not open the game menu or another full-screen window for about 35 seconds.
2. If the save still does not complete, send `output_log.txt` (in
   `%USERPROFILE%\AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker`). The `Mounted save wait` lines identify what held
   it.
3. Opening a menu during the wait ends that save attempt; the previous complete save stays unchanged. Doors and other
   actions work again once no save is waiting.

## Install or upgrade

1. Back up your `Saved Games` folder.
2. Close the game, then unzip the `KingmakerMountedCombat` folder into `<Kingmaker install>\Mods\`, replacing the earlier
   version.

- **Saves:** saves made with 0.2.0–0.2.14 load in 0.2.15. 0.2.15 writes the same save format as 0.2.14.
- **Going back:** 0.2.14 reads 0.2.15 saves unchanged.

## Testing

- **Offline:**
  - A new regression runs the actual save-wait code with stand-in game objects (16 cases). It covers:
    - menu, loading-screen and replaced-world refusals named;
    - three queued quicksaves during a menu, each with its own result and nothing held afterwards;
    - doors held during the wait and released after it;
    - pause restored;
    - cancellation, faults and the 30-second limit timed from activation;
    - the latest state of a blocker reported;
    - logging failures changing nothing.
  - Eight deliberate breakages of the production code are each caught.
  - The rewritten readiness check was compared with 0.2.14's over 1,024 combinations of its conditions: identical.
  - Checks against the installed game library cover the projectile, spell and action descriptions (228 contracts).
  - The full component suite passes 1,488/0.
  - Two rounds of independent review; the second approved the diagnostics.
- **Not tested in the game:** startup, a held save, the new messages, doors during and after a wait.

Details: [handoff](SAVE-AND-DOOR-LOCK-HANDOFF.md), [mission](MOUNTED-SAVE-AND-DOOR-LOCK-MISSION.md),
[current support, removal and rollback](CURRENT-SUPPORT.md), [known issues](CURRENT-KNOWN-ISSUES.md).
