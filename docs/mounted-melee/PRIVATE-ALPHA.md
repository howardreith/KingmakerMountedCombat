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
| ZIP | `artifacts/KingmakerMountedCombat-0.2.16-alpha.2-mounted-alpha2-c5b147a6-diagnostic.zip`, SHA-256 `d82368ce3f9f8b7d9fc4d6fd4f151c40d0947aaed43c76dd3bf54aa1257a9b05`, 4,286,856 bytes |
| DLL | `KingmakerMountedCombat.dll`, SHA-256 `b2d3788e26d0556a164c824644b63b492e50504112b105bad2032b01f63b6a75`, MVID `0bfff03f-3c34-4e72-8414-b0eba5b842ed` |
| Mounted Shield icon | embedded, unchanged, SHA-256 `fd519874e9d231946aad68c46f93a4e0a3a2ea04d4037807e6c7a4d36629f9d0` |
| Source | commit `c5b147a62b511c7ca21e3481e95e3d2ec3e33cc0` on `codex/mounted-alpha2-melee-pose` (draft PR #64), based on main `783b2b3d` (PR #61 merged) |

The packaging script names every agent-built package `…-diagnostic.zip`; the alpha identity is the version and this record
([package-identity.json](package-identity.json), verified by `scripts/Test-MountedAlpha2Package.ps1` in the focused gate).
Earlier alpha-2 builds (`ea112a71`, `94ad290c`, `3c52882a`) are superseded. Alpha 1 (`0.2.16-alpha.1`, ZIP `d7143ba3…`) is your
rollback target.

## Install

1. Back up `%USERPROFILE%\AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\Saved Games` and your current
   `<Kingmaker>\Mods\KingmakerMountedCombat` folder (alpha 1). Keep that backed-up save set: it is your rollback.
2. Test on a copy of a save. Close the game, copy the package's `KingmakerMountedCombat` folder into `<Kingmaker>\Mods`,
   replacing the old copy. UMM should list version `0.2.16-alpha.2`.

## Roll back

Close the game, restore the alpha 1 folder, and load your **backed-up alpha-1 saves**. Whether alpha 1 loads a save written
by alpha 2 is **unverified**; do not rely on it.

## What changed (source-level; none observed in the game)

| Issue | In this alpha |
| --- | --- |
| MMR-01 rider too high | **Changed.** Hypothesis from source: the Horse seat was measured from the rider's pose in the frame Mount Companion fired. The Horse now uses the measured seat its existing correction was tuned against, so first mount, load and remount should seat identically. Mammoth and Small riders are unchanged. |
| MMR-02 TB attack click not approaching | **Not changed (diagnostic only).** Likely cause, from the game's code: native drops a rider attack click whose visualized path (the mount's, under KMC) does not end in the rider's reach. A safe repair needs a legal-route check that cannot be verified without a game run, so native behavior is kept. The log line `Turn-based rider melee attack click dropped by native's path check` records each such click. |
| MMR-03 horse not attacking in TB | **Changed.** One ordinary **melee** click carries to the horse: after the rider's attack (or if the rider cannot act), the horse makes its own lawful attack in the same paired activation. A bow order (even if you switch weapons), a newer order on either actor, an explicit horse order, an AI intent and separate (unpaired) turns do not spend the horse. |
| MMR-04 RT standing still | **Not changed (explained + diagnostic).** The game's own AI already picks the nearest perceived enemy (a target you chose stays first); KMC passes that choice through. A once-per-fight `Mounted real-time engagement: firstStop=…` line says what happened: `no-proposal`, `refused:<reason>`, `accepted-not-dispatched`, `dispatched-not-moved`, `dispatched-moved` or `player-command`. |
| MMR-05 RT pause before advancing | **Not changed.** Confirmed cause: the pair waits until an attack can start (initiative, cooldown) before it moves. An early-movement repair was tried and withdrawn after review found ownership, Hold and perception hazards that need a game run to verify. Behavior is as in alpha 1. |
| MMR-06 horse Trip | **Changed.** The Horse no longer inherits the stock Mammoth's exact TrippingBite (bite hit → Trip). An existing horse loses that one inherited copy once, on a settled load, only where it is provably the inherited one. Wolves, dogs, Riding Dogs and any separately granted trip keep theirs. |
| MMR-07 bite animation | **No animation change.** Each Horse strike logs `Horse strike: horse=…; index=…; weapon=…; handle=…; action=…; activeClip=…; clipWeight=…; speedScale=…`. These are animation-system facts, not proof of what was drawn. |

## Playtest checklist (one ordinary encounter covers most of it; all pending)

1. **Seating:** mount from an unmounted save; check the saddle from the side at normal zoom; walk, turn, stop; dismount and
   check the rider stands normally; remount at the same spot and camera; save and reload once.
2. **TB horse follow-through:** on a fresh pair turn, a melee click on an adjacent enemy: the combat log should show the
   rider's attack and then **Horse** Bite/Hoof attacks. After the pair moved, the horse may get a single attack rather than its
   full routine. A bow click must not use the horse. End Turn mid-sequence stops it; the horse never attacks twice.
3. **TB click approach (unchanged):** click an enemy just beyond melee reach and note whether the pair moves. If nothing
   happens, the log line above explains it; moving closer first still works as in alpha 1. Try the 5-ft-step toggle too.
4. **RT engagement (unchanged):** with AI on and no Stop/Hold, start a fight near two enemies; note whether the pair advances,
   on which enemy, and the `firstStop` line. Check Stop, Hold and AI-off keep it still and a chosen farther target is kept.
5. **Trip:** a horse bite hit should no longer be followed by "Combat Maneuver: Trip". A wolf/dog companion should still trip.
6. **Bite animation:** watch a horse routine at normal zoom; if possible record a short clip. Compare it with that strike's
   `Horse strike:` line (same index): `handle=none` means native had no animation for that strike; `activeClip=BiteAttack`
   with a high `speedScale` means the bite clip was active but compressed into a short window.
7. **Mounted Shield:** mount AC and the Mounted Combat check bonus as in alpha 1 (your screenshots showed +2); unequipping the
   shield or dismounting removes the AC bonus, with no duplicate after remount or reload.

**Logs to send if something is off** (`output_log.txt` in the save folder's parent): lines containing `Horse strike:`,
`Mounted real-time engagement:`, `dropped by native's path check`, `Mounted combat command terminal` and
`Removed the inherited stock TrippingBite`.

## Known limitations

- No game run: every change above is source-level and offline-tested only.
- MMR-02 and MMR-05 keep alpha 1 behavior; MMR-04 is explained, not changed.
- MMR-01's cause is a hypothesis from source, not a matched in-game comparison.
- MMR-07: no change to what is drawn; visible bite is your observation.
