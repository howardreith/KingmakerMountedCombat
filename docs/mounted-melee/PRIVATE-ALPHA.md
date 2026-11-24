# Mounted Combat 0.2.16-alpha.3 — private alpha (melee and presentation repairs)

Mission: [docs/missions/mounted-alpha2-melee-pose/MISSION.md](../missions/mounted-alpha2-melee-pose/MISSION.md). Milestone: an
installable private alpha for Howie's own playtesting. Gameplay, visual, compatibility and save/load acceptance is the owner's
alpha playtest and is listed below as outstanding. This is not a release.

> **NO NATIVE TESTING OCCURRED.** The game was not started for this alpha (zero guarded starts). Nothing in the checklist
> below has been observed in the game; every row is pending your playtest.
>
> **Do not playtest the earlier 0.2.16-alpha.2 package** (`…-mounted-alpha2-c5b147a6-diagnostic.zip`, `d82368ce…`): it is
> kept only as history (its rider approach suppressed the horse's follow-through).

## Package

| Item | Value |
| --- | --- |
| Version | `0.2.16-alpha.3` (version.json, Info.json `Version`, `BuildIdentity.ProductVersion`) |
| Loader | Info.json `ManagerVersion` `0.31.1` (build reference and declared minimum); your installed UMM is unchanged |
| ZIP | `artifacts/KingmakerMountedCombat-0.2.16-alpha.3-mounted-alpha3-bd63035e-diagnostic.zip`, SHA-256 `3be62a3d4a95dbef235f5964743e0f4866ddd020d56d9386d85ecb521808850f`, 4,304,071 bytes |
| DLL | `KingmakerMountedCombat.dll`, SHA-256 `31e798c7e8515b065d14cc67f65c4e1d7ef179de6f40b9494a6f2ffc74559a4b`, MVID `9277a9f6-0fd0-4da0-877f-cacdfe91dc55` |
| Mounted Shield icon | embedded, unchanged, SHA-256 `fd519874e9d231946aad68c46f93a4e0a3a2ea04d4037807e6c7a4d36629f9d0` |
| Source | commit `bd63035e5c4082b47111f9cf2621988133500fc2` on `codex/mounted-alpha2-melee-pose` (draft PR #64), based on main `783b2b3d` |

The packaging script names every agent-built package `…-diagnostic.zip`; the alpha identity is the version and this record
([package-identity.json](package-identity.json), verified by `scripts/Test-MountedAlpha2Package.ps1` in the focused gate).

## Install

1. Back up `%USERPROFILE%\AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\Saved Games` and your current
   `<Kingmaker>\Mods\KingmakerMountedCombat` folder (alpha 1). Keep that backed-up save set: it is your rollback.
2. Test on a copy of a save. Close the game, copy the package's `KingmakerMountedCombat` folder into `<Kingmaker>\Mods`,
   replacing the old copy. UMM should list version `0.2.16-alpha.3`.

## Roll back

Close the game, restore the alpha 1 folder, and load your **backed-up alpha-1 saves**. Whether alpha 1 loads a save written
by alpha 3 is **unverified**; do not rely on it. Delete any `horse-trip-repair-*` files from the mod folder if you roll back.

## What changed (source-level; none observed in the game)

| Issue | In this alpha |
| --- | --- |
| MMR-01 rider too high | The Horse uses one declared seat basis, so first mount, load and remount should seat identically. Only its sideways value is a documented measurement; its height comes from an older test value and is a hypothesis. The `seatCalibration` log line shows `observedSeatFromSource` at each mount so we can compare. Mammoth and Small riders unchanged. |
| MMR-02 TB attack click not approaching | When the game ignores your rider's melee click on an enemy, KMC now accepts it only if the strike is already legal from where the pair stands, or the horse's own path to a striking spot fits its remaining movement in the current step mode. Otherwise nothing happens, as before (no move, previous order kept). Each such click logs `Turn-based rider melee attack click dropped by native's path check: decision=…`. |
| MMR-03 horse not attacking in TB | One ordinary melee click carries to the horse on its own turn-share, also after the pair moved to reach the enemy. A bow order, a newer order to either actor, an explicit horse order, an AI intent and separate turns do not spend the horse. |
| MMR-04 RT standing still | No targeting change; the game's own AI choice is kept. A mount Hold left by the pair's own movement no longer blocks automatic attacks; a Hold you set still does. Once per fight: `Mounted real-time engagement: firstStop=…; order=…; nativeRanking=[…]` shows what the AI proposed and ranked and where it stopped. |
| MMR-05 RT pause before advancing | The pair now starts moving toward an out-of-reach target right away, while the attack itself still waits for initiative/cooldown and swings once. |
| MMR-06 horse Trip | New horses never get the trip. **An existing horse is only fixed when you ask** (below). |
| MMR-07 bite animation | No animation change; each strike logs `Horse strike: …` (animation-system facts, not proof of what was drawn). |

### Existing horse that still trips (MMR-06)

A horse from alpha 1 or earlier keeps the trip until you ask, because a TrippingBite granted another way looks identical in
the save. When such a horse is loaded, the log shows `KMC Horse Trip repair candidate`, and the mod folder gets
`horse-trip-repair-candidates.txt` naming the horse and its request line (`horse=<UniqueId>`).
To apply: back up your save, load it with that horse in the party, and create `horse-trip-repair-request.txt` in the mod
folder containing only that line. Within a few seconds the log shows `Removed the inherited stock TrippingBite from the KMC
Horse at the owner's request`, the request becomes `horse-trip-repair-request.txt.applied`, and `horse-trip-repair-result.txt`
explains the outcome. Only that one fact is removed from that one horse; bite and hoof attacks, other horses, wolves and dogs
are unchanged. A request that does not match exactly one such fact on a loaded horse changes nothing (the result file says
why). Recovery: the change reaches your save only when the game next saves; reload your backup to undo it. If you gave that
horse TrippingBite on purpose, do not apply.

## Playtest checklist (all pending; tie observations to `0.2.16-alpha.3`)

1. **Seating:** mount from an unmounted save; check the saddle from the side; walk, turn, stop; dismount (rider stands
   normally); remount at the same spot; save and reload once. Send the `seatCalibration` lines.
2. **TB horse follow-through:** fresh pair turn, melee click on an adjacent enemy → rider attack then **Horse** Bite/Hoof in the
   combat log. Repeat on an enemy a few steps away: the pair moves, the rider attacks, then the horse. A bow click must not use
   the horse; End Turn stops it; the horse never attacks twice.
3. **TB click approach:** (a) enemy one step beyond reach with the 5-ft-step toggle on; (b) a farther enemy within the
   horse's move; (c) an enemy already in reach after the horse used all its movement — each should attack; (d) an enemy beyond
   the horse's movement and (e) one behind an obstacle should do nothing. Send the `decision=` lines.
4. **TB after incompatible or exhausted movement:** move the pair with its full movement (or a move that forbids a step), then
   click an adjacent enemy: one ordinary attack only. An **extra step** is any further horse movement after its movement was
   spent; an **extra attack** is a second rider or horse attack beyond what the remaining actions allow (a full routine after
   moving more than a step, or a horse attack when the horse's Standard is spent).
5. **RT timing comparison:** in real time, order an attack on an enemy ~10 m away while the rider's previous attack is still
   cooling down; do the same with a similar unmounted melee character. Note for each: time from click to **first movement**
   (the pair should start moving at once, like the unmounted control) and time to **first swing** (it should still wait for
   the cooldown). A short video is ideal.
6. **RT engagement:** with AI on and no Stop/Hold, start a fight near two enemies: does the pair advance, on which enemy, and
   what does the `firstStop` line say? Check Stop, Hold on the rider, Hold on the horse, and AI-off keep it still, and a chosen
   farther target is kept.
7. **Trip:** a horse bite hit is no longer followed by "Combat Maneuver: Trip" (new horse, or after the repair above). A
   wolf/dog companion still trips.
8. **Bite animation:** watch a horse routine; record a short clip if possible and compare it with that strike's `Horse
   strike:` line (`handle=none` = no animation for that strike; `activeClip=BiteAttack` with a high `speedScale` = the bite clip
   was compressed into a short window).
9. **Mounted Shield:** mount AC and the Mounted Combat check bonus as in alpha 1 (+2); unequipping or dismounting removes the
   AC bonus, with no duplicate after remount or reload.

**Logs to send** (`output_log.txt`): lines with `Horse strike:`, `Mounted real-time engagement:`, `dropped by native's path
check`, `Mounted combat command terminal`, `seatCalibration`, `KMC Horse Trip repair` and `Removed the inherited stock TrippingBite`.

## Known limitations

- No game run: every change is source-level and offline-tested only.
- MMR-01's seat height is a hypothesis (provenance above); MMR-07 changes no animation.
- RT ordering: while the rider's early-dispatched attack waits for its cooldown, a horse that becomes ready first attacks after
  the rider's strike, not before.
