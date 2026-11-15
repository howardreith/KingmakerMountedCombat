# Mr. Bard's Kingmaker Mounted Combat 0.2.11

Released by owner order on 2026-11-15. This update to [0.2.10](RELEASE-NOTES-0.2.10.md) fixes a mounted attack order
that could hang turn-based combat. Everything 0.2.0's notes say about features, support, known limits, saves and
rollback still applies: [0.2.0 notes](RELEASE-NOTES-0.2.0.md).

## Fixed

- **A mounted attack order could hang turn-based combat when the rider could not act.**
  - **Symptom:** in turn-based combat, ordering a mounted rider who cannot act to attack stalled the fight. It was
    reported against the Lady of Shallows with Serenity and Fascinate on the rider. The log repeated
    `Mounted combat command accepted` and
    `Mounted combat command terminal: … result=Success; reason=Success; childAttacks=0` thousands of times.
  - **Cause:** in turn-based combat the game ends an order given to a character who cannot act without doing
    anything, but reports the order as a success. The mod read that as a finished attack, kept the order and issued
    it again on every update.
  - **Fix:**
    - An attack order is kept only after an attack was actually made. A miss still counts. An order the game ended
      without an attack is dropped once.
    - In turn-based combat the order is refused up front when the attacking character is helpless, dazed, stunned or
      otherwise prevented from acting or attacking. You see one message, for example
      `Royon cannot act right now (dazed).`
    - Only the character making the attack is checked. If the rider cannot act, the mount keeps its own actions:
      select it to attack or move, or end the turn. The reverse holds for a mount that cannot act.
    - No action is spent, refunded or granted by any of this.
- **No false "completed".** An attack that never happened is no longer reported as
  `Mounted pair attack completed.`
- **Clearer log:** an order dropped this way logs one `Mounted attack intent retired without execution:` line with
  the characters involved and what the game reported about the attacker. A refused order logs one
  `Mounted stock attack dispatch rejected:` line.

## Changed behaviour to know about

- **A dropped order is not retried.** If the game ends a turn-based attack order before it starts for a passing reason
  (for example the character is still getting up from a stun), 0.2.10 happened to keep retrying until the attack went
  through. 0.2.11 drops the order once and tells you, as the game does for a character who is not mounted. Give the
  order again.
- **Real time is unchanged.** There the game keeps the order waiting and starts it when the character can act again.

## Known limits

- **The exact trigger was not identified.** Which Serenity or Fascinate effect stopped the rider was not determined.
  The fix does not depend on it, and the new log line reports it.
- **Real time, rider cannot act:** the rider's attack order waits until the rider can act, and the mount does not
  attack in the rider's place for that order. That is unchanged.
- The limits in the [0.2.9 notes](RELEASE-NOTES-0.2.9.md) and the issues listed as not changed in the
  [0.2.5 notes](RELEASE-NOTES-0.2.5.md) remain.

## Upgrade

1. Back up `Saved Games`.
2. Close the game. Replace the `Mods\KingmakerMountedCombat\` folder with this package's.

- **Saves:** the format is unchanged. Saves made with 0.2.0–0.2.10 load in 0.2.11.
- **Going back:** 0.2.11 adds nothing to a save. The Mounted Archery rule from the [0.2.10 notes](RELEASE-NOTES-0.2.10.md)
  still applies.
- **Assembly version:** stays 0.1.0.0.

## How this binary relates to the testing

**Not run in the game.** This machine has no mounted-combat test save, so the reported fight was not reproduced and
the fix was not checked in game.
- **The cause** was confirmed in the mod's source and in the installed game's own code (Kingmaker 2.1.7b): the game
  reports success for an order it ended without starting.
- **Offline checks** pass:
  - 1,229 component tests;
  - a new check that runs the mod's own combat controller on genuine game objects: an attack order that the game ended
    without an attack is dropped and is not issued again;
  - the same check fails on the 0.2.10 build;
  - mutation checks showing the new tests catch both the old rule and a missing up-front refusal;
  - one independent review of the change;
  - source validation and package validation.

**If you still see a stall:** send the log. Search it for `retired without execution` and
`Mounted stock attack dispatch rejected`.

**Engineering record:** [the stall repair handoff](MOUNTED-ATTACK-STALL-REPAIR-HANDOFF.md) (PR #43, merged as
`22945394`). It lists the in-game checks still to run.
