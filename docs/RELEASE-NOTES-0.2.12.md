# Mr. Bard's Kingmaker Mounted Combat 0.2.12

Released by owner order on 2026-11-19. This update to [0.2.11](RELEASE-NOTES-0.2.11.md) delivers the Known Issues
Remediation Epic: fixes for exploration, gamepad play, scenes, mounted attack feedback and the interface. The current
support, limits, removal and rollback contract is [CURRENT-SUPPORT.md](CURRENT-SUPPORT.md); it replaces the 0.2.0
notes' known-limits list. Every issue's status is in the [current issue ledger](CURRENT-KNOWN-ISSUES.md).

## Fixed

### Exploration and loading

- **Dismounted after arriving from the world map.**
  - **Before:** after the loading screen of an area entered from the world map, a safety check could find the rider's
    and horse's models not yet active and dismount the pair. Seen at Ratnook Hill and Candlemere Tower.
  - **Now:** the pair waits, unattached, until the models are active, then remounts about a third of a second into
    ordinary play, with no Mount action. With pause-on-load it remounts after you unpause. If a fight starts in that
    moment, the pair fights dismounted, as before. A pair that cannot remount within ten seconds of play stays
    dismounted with a message.
  - Saving and loading in between keeps the pair's arrival position (see [Saves](#saves)).
- **Candlemere Tower: "wade through" while mounted.** The screen faded but the party did not cross unless you
  dismounted first. A mounted party now crosses like an unmounted one.
- **Horse pace.**
  - Walking to an area entrance or exit: the horse now joins the party's run.
  - Gamepad stick running now reaches the horse.
  - The party's group walking pace now uses the horse's speed, not the rider's. The game's own 30 ft floor and rules
    are unchanged.
- **Gamepad stick on a mounted rider.** Before, moving a mounted rider with the stick moved nothing, could dismount the
  pair and then stopped it from remounting. The stick now drives the horse.
- **Extra green movement markers.** Four ways a mounted pair could show a marker the game would not show for one unit
  are removed: the duplicate marker of an entrance walk, the marker under a scene's regroup walk, a marker for the
  horse's walk to a door, and a marker left under the rider after clicking an enemy the rider could not attack (that
  approach is now the horse's). Whether one of these is the marker originally reported is not established.

### Scenes

- **Short scene moves of a mounted rider.** A scene that moves only the rider by 3 m or less now has the rider step
  down for it and remount afterwards, like longer moves. Before, the rider could stay on the horse and the scene's
  placement was lost (a limit listed in the [0.2.8 notes](RELEASE-NOTES-0.2.8.md)).
- **A pair held through a fight.** When a pair that a scene had stepped down is still waiting when combat starts, you
  now get one on-screen notice per fight that it will remount afterwards. Before, this was only a log line.

### Mounted attacks

- **Clicking the same target right after an attack finished** now gets its own answer (for example, that no Standard
  action is left). Before, it could get no answer at all.
- **Messages describe what actually ended the order.** Stopping an attack while the rider is dazed now says the order
  was stopped, not that the rider cannot act.
- **Real time, AI on, a rider who cannot attack:** the party AI no longer re-issues the attack every third of a second.
  Your own orders still wait as before.
- **Automatic attacks no longer mark targets unreachable by mistake.** An automatic attack that ended because the
  target died, the rider was stopped or the order was cancelled no longer tells the rider's AI the target cannot be
  reached. Only a real failed approach does.

### Interface

- **After a Charge,** the horse's greyed Move and Standard slots show no remaining distance and no five-foot step.
- **Targeting glow:** a mounted action slot whose ability picks its target through a hidden helper ability now shows
  the selected glow while you choose the target.
- **Paladin class panel:** on the character screen, the class panel no longer lists Weapon Bond upgrades for a Paladin
  who chose Divine Mount or has not chosen a bond yet. Per-level tooltips are unchanged.
- **Mounted badge:** a mounted rider's party portrait shows a small saddle badge.
- **Turn tracker:** hovering over a mount in the world highlights its rider's entry in the turn tracker, where the
  mount's own entry is folded into its rider's.

None of the interface changes spends, grants or changes anything in the game; they only change what is shown.

## Not changed

- **Saving after the Ratnook door** (reported in 0.2.4): still not explained. This version logs more about each save
  request. If saving fails for you, send the log and say whether you used Save, quicksave or an autosave.
- **A scripted scene that moves a seated rider during combat** waits for the scene's own timeout, then the rider
  returns to the saddle. This is not supported.
- **Shooting from the saddle** follows the same rule as 0.2.10 and 0.2.11: turn-based only, −4 (−2 with Mounted
  Archery) once the mount has used more than one move action's worth of travel; five-foot steps and Charge do not
  count. The exact rule is in [CURRENT-SUPPORT.md](CURRENT-SUPPORT.md#shooting-from-the-saddle-and-mounted-archery).
- Scene regrouping limits, rider falls, the cleanup save and everything else not listed above are unchanged.

## Upgrade

1. Back up `Saved Games`.
2. Close the game. Replace the `Mods\KingmakerMountedCombat\` folder with this package's.

### Saves

- Saves made with 0.2.0–0.2.11 load in 0.2.12.
- **One save change:** a save made while a pair is waiting to remount after a world-map arrival records where the pair
  arrived (the `kmc-retained-intent` entry, version 2). Every other save is written exactly as before.
- **Going back to 0.2.11 or earlier:** such a save still loads, but that waiting pair loads dismounted; mount again.
  Rollback otherwise means your pre-upgrade backup and the older package. A campaign with Mounted Archery needs 0.2.10
  or later. The assembly version stays 0.1.0.0, so the version number does not show what an older build can read
  ([details](CURRENT-SUPPORT.md#rollback-and-downgrade)).

## How this binary relates to the testing

**Not run in the game.** This machine has no mounted-combat test save, so none of these fixes was checked in game, no
gamepad session was run and no screen of the interface changes was viewed.
- **The causes** were established in the mod's source and the installed game's own code (Kingmaker 2.1.7b), or, for
  Candlemere, inferred from the game's interaction data; the owner's Candlemere log was not available.
- **Offline checks** pass on the release build:
  - 1,411 component tests;
  - focused checks that run the mod's own code on genuine game objects for each fix, each shown to fail on the build
    before its fix;
  - mutation checks for every sprint, each caught;
  - 815 checks against the installed game assembly, source validation and package validation.
- **Reviews:** every change was reviewed by the owner, with each requested correction made before acceptance.

**Engineering record:** [the epic closeout](KNOWN-ISSUES-REMEDIATION-EPIC-CLOSEOUT.md) (PRs #45–#53, merged into main
as `446985b7`) and the sprint handoffs it links.
