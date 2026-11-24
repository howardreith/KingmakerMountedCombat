# Mounted Combat 0.2.16-alpha.2 — private alpha (melee and presentation repairs)

Mission: [docs/missions/mounted-alpha2-melee-pose/MISSION.md](../missions/mounted-alpha2-melee-pose/MISSION.md). Milestone: an
installable private alpha for Howie's own playtesting. Gameplay, visual, compatibility and save/load acceptance is the owner's
alpha playtest and is listed below as outstanding. This is not a release.

> **NO NATIVE TESTING OCCURRED.** The game was not started for this alpha (zero guarded starts). Nothing in the checklist
> below has been observed in the game; every row is pending your playtest.

## Package

| Item | Value |
| --- | --- |
| Version | `0.2.16-alpha.2` (version.json, Info.json `Version`, `BuildIdentity.ProductVersion`) |
| Loader | Info.json `ManagerVersion` `0.31.1` (build reference and declared minimum); your installed UMM is unchanged |
| ZIP | `artifacts/KingmakerMountedCombat-0.2.16-alpha.2-mounted-alpha2-ea112a71-diagnostic.zip`, SHA-256 `b9168c6225bc34598e172aefdce124c1e4fdcc876dbc9dd012c203313a6dc2e7`, 4,284,987 bytes |
| DLL | `KingmakerMountedCombat.dll`, SHA-256 `16c844db1e5b51a086b9ee33951339fcdecb674bd2f462336e2435eb5385112c`, MVID `00c67828-d35b-4bcf-ad82-6675a35f92f0` |
| Mounted Shield icon | embedded, unchanged, SHA-256 `fd519874e9d231946aad68c46f93a4e0a3a2ea04d4037807e6c7a4d36629f9d0` |
| Source | commit `ea112a71819520e9df915eff721ba42d042792bd` on `codex/mounted-alpha2-melee-pose` (draft PR #64), based on main `783b2b3d` (PR #61 merged) |

The packaging script names every agent-built package `…-diagnostic.zip`; the alpha identity is the version and this record.
Alpha 1 (`0.2.16-alpha.1`, ZIP `d7143ba3…`) is the previous package and your rollback target.

## Install

1. Back up `%USERPROFILE%\AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\Saved Games` and your current
   `<Kingmaker>\Mods\KingmakerMountedCombat` folder (alpha 1).
2. Test on a copy of a save. Close the game, copy the package's `KingmakerMountedCombat` folder into `<Kingmaker>\Mods`,
   replacing the old copy. UMM should list version `0.2.16-alpha.2`.

## Roll back

Close the game, restore the alpha 1 folder, and use saves made before alpha 2 (or your backup). A save written by alpha 2
differs from alpha 1 only in that an existing KMC Horse no longer carries the inherited bite-Trip fact; alpha 1 loads it
the same way, but that has not been tested.

## What changed (all source-level; none observed in the game)

| Issue | Change |
| --- | --- |
| MMR-01 rider too high | The Horse seat no longer samples the rider's pose in the frame Mount Companion fires; it uses the measured seat the existing Horse correction was tuned against, so first mount, mounted load and remount seat identically. |
| MMR-02 TB attack click not approaching | A turn-based rider attack click on an enemy just out of reach, which native dropped because its path check reads the rider (the mount moves the pair), is now admitted when the mount has lawful movement left. The pair then approaches with that movement. |
| MMR-03 horse not attacking in TB | One ordinary attack click now carries to the horse: after the rider's attack, the horse makes its own lawful attack in the same paired activation, without a second click. An explicit horse order, an AI intent and separate (unpaired) turns are unchanged. |
| MMR-04 RT standing still at combat start | A real-time melee intent no longer waits for initiative before moving (see MMR-05). Target choice is still the rider's native AI proposal. |
| MMR-05 RT pause before advancing | While neither actor may strike yet (initiative or cooldown), the mount starts moving toward an out-of-reach melee target at once; the strike itself still waits for native readiness. |
| MMR-06 horse Trip | The Horse no longer inherits the stock Mammoth's TrippingBite (bite hit → Trip). An existing horse loses that one inherited fact once on load. Wolves, dogs and Riding Dogs keep their trip. |
| MMR-07 bite animation | Investigated, no animation change: the native horse bite action exists (clip `BiteAttack`) and is the one played; native speeds up all three clips of a bite/hoof/hoof routine together, which may make the bite hard to see. The log line `Mounted combat command terminal` now ends with `horseBite={action=…,speedScale=…,routineAttacks=…,acted=…}` for each horse attack. |

## Playtest checklist (priority order; all pending)

1. **Seating:** mount from an unmounted save; check the saddle from the side at normal zoom; dismount and remount at the
   same spot and camera; save and reload once. The seat should look the same in all three. Note any difference.
2. **TB click approach:** on a fresh pair turn, click an enemy just beyond melee reach. The pair should move and attack
   without a ground click. After movement is spent, the same click should not create extra movement.
3. **TB horse follow-through:** after that click, watch the combat log for **Horse** attacks (Bite, Hoof) after the
   rider's. The horse must not attack again after it has acted. End Turn mid-sequence should stop it.
4. **RT engagement:** with AI on and a melee weapon, start a fight near an enemy: the pair should advance. Then give an
   explicit attack order from outside reach and compare when movement starts with an unmounted melee character.
5. **Trip:** a horse bite hit should no longer be followed by "Combat Maneuver: Trip". A wolf/dog companion should still trip.
6. **Bite animation:** watch a horse attack routine at normal zoom. If the bite is still not visible, send `output_log.txt`
   lines containing `horseBite={` (they show the native action and its speed-up).
7. **Mounted Shield:** mount AC and the Mounted Combat check bonus should be as in alpha 1 (your screenshots showed +2).

If AI-on real-time combat still stands still, note whether the rider had a Hold or a completed move order, and send the
log; the native AI proposal itself was not changed.

## Known limitations

- No game run: every behavior above is source-level and offline-tested only.
- MMR-02's cause is inferred from the installed native code, not from a trace of your click.
- MMR-04: if the rider's native AI never proposes an attack (AI off, Hold, unreachable target), this alpha does not change that.
- MMR-07: no change to what is drawn; visible bite is your observation.
