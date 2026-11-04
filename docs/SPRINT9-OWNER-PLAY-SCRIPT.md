# Owner play script: release candidate preview.102

Three ordinary sessions. Run them on any computer, on your own install, in whatever order suits you. Each line has one thing to look for. Write "OK" or what you saw; only what you report gets recorded.

**Before anything:** copy your whole `Saved Games` folder. Install by putting the zip's `KingmakerMountedCombat` folder in `<Kingmaker install>\Mods\`. Start a campaign, or use a save you don't mind extending.

## Session 1: your normal install, Paladin (about 30 minutes)

Run this with your usual setup. If that means no other mods, this is the first time the Paladin path has been played that way, so it is the most valuable session.

1. Open UMM (Ctrl+F10). **Kingmaker Mounted Combat** shows `0.1.0-chunk7b-preview.102`, and its page reads sensibly.
2. Take a Paladin to level 5, or hire one at the mercenary manager. **Divine Bond** waits for a choice. Does it read clearly?
3. Choose **Divine Mount**, then **Horse**. A Halfling or Gnome also sees **Riding Dog**. The mount appears and is yours.
4. Level further. **Without Call of the Wild, does the mount level by itself, and does it heal when it does?** Note its level against your Paladin's.
5. Ride, walk, dismount. Stand away from the mount and use **Call Divine Mount**: it arrives beside you and uses one call.
6. On another Paladin, choose **Weapon Bond**: the stock feature, and no animal.

## Session 2: mounted play and a restart (about 30 minutes)

1. Ride with a **Small rider**, female Halfling if you can. Does the seat and motion look right walking, turning and attacking, from your usual camera? Does a cast look right?
2. The ability list has **Mount Companion**, **Dismount**, **Mounted Charge**. It has no Rider Primary or Mount Primary.
3. Click an enemy, and let it die while another stands near: the pair moves on without a fresh click where the game would.
4. Press Stop, then order a move: the pair obeys at once.
5. With the **Mounted Combat** feat, get the mount hit: the rider sometimes negates it, at most once a round.
6. **Mounted Charge** on an enemy in a clear straight line: one attack on arrival. Try it in real time and turn-based.
7. Open a **door** by clicking it from across the room: one click, and you ride through.
8. Heal a **distant ally** with a touch spell: one click, the pair carries you there, one heal.
9. Change area: everyone stays mounted, and an autosave appears.
10. **QuickSave, Manual save, quit the game entirely, restart, load each.** Still mounted, nothing refunded, and you can ride and fight.

## Session 3: the items no agent could reach (about 20 minutes)

1. **Trap:** ride near a trap you have found, in the same room, and click it. Does the rider approach and attempt to disarm, with the game's own result? If you have a save where this is possible, keep it: it would also let the automated row run.
2. **Turn-based Paladin:** at Paladin 11 with the mount leveled, in a turn-based fight while mounted, select the mount and use **Celestial Smite** first. It should fire at once. End the turn: no use returns. Next turn, attack with the mount: bonus damage. Before smiting, try it on an enemy far across the map: it refuses with "move the pair into range first", the mount stays put and no use is spent. Ride into range and try again: it works.
3. **Turn-based door:** in turn-based combat near a usable door, click it while mounted. This has never been demonstrated, so any result helps.

## If something goes wrong

Don't save over the save you were using. Copy `<Kingmaker install>\Kingmaker_Data\Managed\UnityModManager\Log.txt` and `output_log.txt` before the next launch. Note the step and what you saw.

## Removing it

Read the removal section of [the release notes](SPRINT9-RELEASE-NOTES.md) first. Going back means restoring the saves you copied at the start.
