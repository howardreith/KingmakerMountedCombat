# Kingmaker Mounted Combat: support and configuration (Chunk 7)

Status: DRAFT for a private engineering candidate. It is not a public release, and it does not
authorize installing into a human campaign.

## What is supported

- Kingmaker 2.1.7 with Unity Mod Manager (UMM). Info.json declares manager 0.28.2; the lab ran UMM
  0.33.2.0. KMC has no other mod dependency.
- Riders: eligible Medium riders on the KMC Horse or a supported stock Mammoth, plus one Small profile:
  the stock default male Halfling on the KMC Horse, unpolymorphed, with the Horse at its native Large
  size.
- The KMC Horse is a Ranger animal-companion choice. KMC adds it to the game's choice list; the game
  owns acquisition and progression.
- Up to six mounted pairs. Two-pair combat is the tested combat breadth. Six pairs are tested for
  exploration, Manual save and reload, not for combat.

## Settings

KMC has **one** setting. Open it in Unity Mod Manager (Ctrl+F10 in game) under Kingmaker Mounted Combat.

| Setting | Default | Notes |
| --- | --- | --- |
| Enable optional real-time Mounted Charge | Off | Can be changed only outside combat, with every pair dismounted and no save or load in progress. Turn-based Charge is unavailable and refuses without spending actions. **Not saved:** it resets to off on every game launch (KMC writes no settings file). |

The other values in the code (`DiagnosticSettings`) are fixed for the supported preset and have no
control: paired activation on, paired command scheduler off, legacy unified turn off, diagnostic
overlay off, transport movement on. Test-automation command-line options are for the lab only and are
not a player setting.

The UMM page also has a **Prepare to disable / remove KMC** button. See
[install, upgrade, rollback and removal](CHUNK7-MIGRATION-ROLLBACK-REMOVAL.md).

## Where the logs are

| Log | Location | Contents |
| --- | --- | --- |
| UMM log (the main one) | `<Kingmaker install>\Kingmaker_Data\Managed\UnityModManager\Log.txt` | Every KMC line, prefixed `[KingmakerMountedCombat]`. KMC writes errors and exceptions here, not to the Unity log. |
| Unity player log | `%USERPROFILE%\AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\output_log.txt` (beside the `Saved Games` folder) | The game's own errors. |

Both logs are rewritten at every game start. For the UMM log, every lab run's receipt found exactly one
UMM session header in the file after earlier sessions. Copy the logs **before** you launch the game
again.

## What the messages mean

The KMC page in UMM shows the pair count, the selected pair's last feedback, the save/load summary and
the removal status. The same text is written to the UMM log.

| Message (start of text) | Meaning | What to do |
| --- | --- | --- |
| `Kingmaker Mounted Combat <version> loaded …` | KMC started. | Nothing. Check that the version is the one you installed. |
| `Registered the original KMC horse companion blueprint trio and exact Ranger append transaction` | The Horse was added to the Ranger companion list. | Nothing. |
| `[Exception] Horse companion blueprint registration: … no longer has seven stock options.` | Known preview.211 defect on a base-game install. | Upgrade to Chunk 7. If you see it on Chunk 7, report it. |
| `Mounted pairs: N / 6` | Pairs currently mounted. | Nothing. |
| `This save has no mounted relationship metadata.` | The save holds no KMC pair. | Nothing. |
| `Native unmounted save restored without inventing a pair.` | Unmounted save loaded. | Nothing. |
| `Saved mounted world restored all N attachments and M participation owners …` | Every saved pair was restored, with no new Mount and no extra actions. | Nothing. |
| `All N mounted relationships restored before area autosave …` | Area change kept every pair. | Nothing. |
| `Dismounted on arrival because the companion is not directly controllable in this area: …` | Capital (or a similar area): that pair was dismounted. | Mount again after leaving. |
| `All-pair area restoration failed: … load a valid save to resume.` | An area change could not restore the pairs. Saving is blocked. | Load an earlier save and report it. |
| `Mounted metadata uses a newer schema; original data is preserved.` | The save came from a newer KMC. | Use the newer KMC. Do not save over it. |
| `Mounted metadata campaign/area differs from the selected native archive.` | The KMC data does not match the save. | Report it with the save. |
| `Not prepared: …` / `Preparation unconfirmed: …` | Removal preparation refused or could not confirm. | Read the stated cause. Do not remove KMC. |
| `Prepared: cleanup save … KMC can now be disabled or removed …` | A clean save was written. | You can remove KMC and load that save. |
| `Unload refused: an owned archive worker can still commit …` | A save was still being written. | Wait for the save to finish, then retry. |

When Mount Companion or another KMC ability is unavailable, the game's ability tooltip gives the reason
(for example, "Both units must be directly controllable."). Fix the stated cause. Repeated clicks or a
selection change do not grant actions.

## Known limits

- The action bar is not pre-filled. Place Mount Companion and Dismount by hand.
- Turn-based Charge is unavailable. Real-time Charge is off by default and needs a melee weapon.
- The defensive Mounted Combat feat is not implemented. Paladin Divine Steed and other class mounts are
  out of scope.
- Capital and similar areas dismount companion pairs on arrival.
- A campaign that contains a KMC Horse needs KMC installed.
- A save written by Chunk 7 is not certified under preview.211.
- Other mod combinations are not tested. The Call of the Wild Ranger list is recognized for Horse
  registration only.
- Combat behaviours recorded as **uncovered** by automated evidence:
  - **C11 prone stand-up.** Disengagement is implemented, but the game offers no deterministic prone
    source on the test saves.
  - **F04 defensive casting check.** Moving casts are implemented, but no cast runs while threatened
    past the game's one-second window on the test geometry.
  - **C03 base attack bonus and haste variation.** Rapid Shot is implemented, but there is no native
    source on the test saves, and synthetic stat edits are not accepted.
  - **Prepared-spell casting while mounted.** The test Ranger's spell slot is empty. Filling it needs a
    spellbook choice and a native camp rest, and that rest rolls an uncontrolled random encounter.
- Implemented and awaiting their first in-game pilot: death of a mounted rider, Rapid Shot full
  attacks, disengagement attacks of opportunity, staged moving actions, and real-time Charge with a
  melee weapon (on the owner's charge save).
- Performance acceptance is pending. Owner/HUMAN PLAY acceptance is pending.

## How to report a problem

1. Stop playing, but do not save over the save you were using.
2. Copy the UMM log and the Unity log before the next launch.
3. Copy the save (`.zks`) you loaded and, if you made one, the save after the problem. Saves are in
   `%USERPROFILE%\AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\Saved Games`.
4. Write down:
   - the KMC version from the first KMC log line;
   - turn-based or real-time;
   - which pair (rider and mount names) and which actor;
   - the exact steps;
   - what you expected and what happened;
   - the KMC UMM page text (a screenshot is fine).
5. Note any other installed mods.
