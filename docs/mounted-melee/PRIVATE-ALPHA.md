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
| ZIP | `artifacts/KingmakerMountedCombat-0.2.16-alpha.2-mounted-alpha2-3c52882a-diagnostic.zip`, SHA-256 `e0a00993012176fc5b700c0d290c1269749315bfe2fd0270f86a75babc9667e3`, 4,287,279 bytes |
| DLL | `KingmakerMountedCombat.dll`, SHA-256 `a2cecac0d65925a21429e12a13e5109a3185916c54c6f72acf09589dda7447b7`, MVID `42a2aa87-30dc-4717-a88f-04a8f99a2d72` |
| Mounted Shield icon | embedded, unchanged, SHA-256 `fd519874e9d231946aad68c46f93a4e0a3a2ea04d4037807e6c7a4d36629f9d0` |
| Source | commit `3c52882adaa897008a474859b64cfca644371195` on `codex/mounted-alpha2-melee-pose` (draft PR #64), based on main `783b2b3d` (PR #61 merged) |

The packaging script names every agent-built package `…-diagnostic.zip`; the alpha identity is the version and this record
([package-identity.json](package-identity.json), verified by `scripts/Test-MountedAlpha2Package.ps1` in the focused gate).
The earlier `…-ea112a71-diagnostic.zip` (`b9168c62…`) and `…-94ad290c-diagnostic.zip` (`16061b2a…`) are superseded and is not the alpha. Alpha 1 (`0.2.16-alpha.1`, ZIP
`d7143ba3…`) is your rollback target.

## Install

1. Back up `%USERPROFILE%\AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\Saved Games` and your current
   `<Kingmaker>\Mods\KingmakerMountedCombat` folder (alpha 1). Keep that backed-up save set: it is your rollback.
2. Test on a copy of a save. Close the game, copy the package's `KingmakerMountedCombat` folder into `<Kingmaker>\Mods`,
   replacing the old copy. UMM should list version `0.2.16-alpha.2`.

## Roll back

Close the game, restore the alpha 1 folder, and load your **backed-up alpha-1 saves**. Whether alpha 1 loads a save written
by alpha 2 is **unverified**; do not rely on it. (Alpha 2 adds no blueprint and changes a saved Horse only by removing the
inherited bite-Trip fact, but that has not been tested in either direction.)

## What changed (source-level; none observed in the game)

| Issue | Change |
| --- | --- |
| MMR-01 rider too high | Hypothesis (from source, not yet matched by an in-game comparison): the Horse seat was measured once from the rider's pose in the frame Mount Companion fired. The Horse now uses the measured seat its existing correction was tuned against, so first mount, load and remount should seat identically. Mammoth and Small riders are unchanged. |
| MMR-02 TB attack click not approaching | Inferred from the game's code: native drops a rider attack click whose visualized path (the mount's, under KMC) does not end in the rider's reach. Only that refusal of native's own predicted melee attack on the highlighted enemy is now admitted, and only when the mount's remaining step or move allowance covers the straight-line gap to reach. A blocked or longer route is still refused by the pair's approach. |
| MMR-03 horse not attacking in TB | One ordinary **melee** click carries to the horse: after the rider's attack (or if the rider cannot act), the horse makes its own lawful attack in the same paired activation. A bow click, a newer order on either actor, an explicit horse order, an AI intent and separate (unpaired) turns do not spend the horse. |
| MMR-04 RT standing still | The waiting pair now travels (MMR-05). Target choice is still the rider's native AI. A once-per-fight `Mounted real-time engagement: firstStop=…` log line says where engagement stopped (no-proposal, refused:reason, accepted-not-dispatched, travelling, engaging). |
| MMR-05 RT pause before advancing | While neither actor's strike can start (initiative, cooldown, busy hands, equipment change), the mount moves toward an out-of-reach melee target; the strike waits for native readiness. The move stops at the pair's reach, gives way to any newer order or door click, does not leave the mount on Hold, and a route that cannot reach ends the order once ("The mounted pair cannot reach that target."). |
| MMR-06 horse Trip | The Horse no longer inherits the stock Mammoth's exact TrippingBite (bite hit → Trip). An existing horse loses that one inherited, sourceless fact once on a settled load. Wolves, dogs, Riding Dogs and any separately granted trip keep theirs. |
| MMR-07 bite animation | No animation change. Each horse strike logs `Horse strike: index=…; weapon=…; special=…; playback=…; action=…; speedScale=…`. |

## Playtest checklist (one ordinary encounter covers most of it; all pending)

1. **Seating:** mount from an unmounted save; check the saddle from the side at normal zoom; walk, turn, stop; dismount and
   check the rider stands normally (unmounted restoration); remount at the same spot and camera; save and reload once.
2. **TB click approach:** on a fresh pair turn, click an enemy just beyond melee reach: the pair should move and attack with
   no ground click. Repeat with the 5-ft-step toggle when only a step is needed. After movement is spent, the same click must
   not create movement or an attack.
3. **TB horse follow-through:** watch the combat log for **Horse** Bite/Hoof attacks after the rider's melee attack. After the
   pair moved, the horse may get a single attack instead of its full bite+hooves routine; standing still it may get the full
   routine. A bow click must not use the horse. End Turn mid-sequence stops it; the horse never attacks twice.
4. **RT engagement:** with AI on, a melee weapon and no Stop/Hold, start a fight: the pair should advance. If two enemies are
   reachable, note which it chose. Give an explicit attack order from outside reach and compare when movement starts with an
   unmounted melee character. Then check Stop, Hold and AI-off keep the pair still, and an explicitly chosen farther target
   is kept.
5. **Trip:** a horse bite hit should no longer be followed by "Combat Maneuver: Trip". A wolf/dog companion should still trip.
6. **Bite animation:** watch a horse routine at normal zoom.
7. **Mounted Shield:** mount AC and the Mounted Combat check bonus as in alpha 1 (your screenshots showed +2); unequipping the
   shield or dismounting removes the AC bonus, with no duplicate after remount or reload.

**Logs to send if something is off** (`output_log.txt` in the save folder's parent): lines containing `Horse strike:` (bite:
`playback=no-handle` means nothing was drawn; `playback=played` with a large `speedScale` means it played but fast),
`Mounted real-time engagement:`, `Admitted a turn-based rider melee attack click`, `Mounted combat command terminal` and
`Removed the inherited stock TrippingBite`.

## Known limitations

- No game run: every behavior above is source-level and offline-tested only.
- MMR-01 and MMR-02 causes are inferred from source and the installed game code, not from traces of your play.
- MMR-04: if the rider's native AI never proposes an attack (AI off, Hold, unreachable), this alpha does not change that;
  the engagement log line names the case.
- MMR-07: no change to what is drawn; visible bite is your observation.
