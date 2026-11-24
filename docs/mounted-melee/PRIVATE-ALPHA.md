# Mounted Combat 0.2.16-alpha.4 — private alpha (melee and presentation repairs)

Mission: [docs/missions/mounted-alpha2-melee-pose/MISSION.md](../missions/mounted-alpha2-melee-pose/MISSION.md). Milestone: an
installable private alpha for Howie's own playtesting. Gameplay, visual, compatibility and save/load acceptance is the owner's
alpha playtest and is listed below as outstanding. This is not a release.

> **NO NATIVE TESTING OCCURRED.** The game was not started for this alpha (zero guarded starts). Nothing in the checklist
> below has been observed in the game; every row is pending your playtest.
>
> **Do not playtest the earlier alpha.2 or alpha.3 packages** (`…-mounted-alpha2-c5b147a6-…`, `d82368ce…`;
> `…-mounted-alpha3-bd63035e-…`, `3be62a3d…`): they are kept only as history.

## Package

| Item | Value |
| --- | --- |
| Version | `0.2.16-alpha.4` (version.json, Info.json `Version`, `BuildIdentity.ProductVersion`) |
| Loader | Info.json `ManagerVersion` `0.31.1` (build reference and declared minimum); your installed UMM is unchanged |
| ZIP | `artifacts/KingmakerMountedCombat-0.2.16-alpha.4-mounted-alpha4-66492352-diagnostic.zip`, SHA-256 `aeb816de72d13397e4800046859d1e4cf33ccc166f34747ec21afede6a400dc5`, 4,308,850 bytes |
| DLL | `KingmakerMountedCombat.dll`, SHA-256 `99cad9609b2149d11b740fd7777206cadd092f82b35a7bb073b970aba39a883d`, MVID `fe54643a-b8a6-4cb8-89a9-6a4d48883970` |
| Mounted Shield icon | embedded, unchanged, SHA-256 `fd519874e9d231946aad68c46f93a4e0a3a2ea04d4037807e6c7a4d36629f9d0` |
| Source | commit `66492352fa95b0c20855524799722f4125130a7f` on `codex/mounted-alpha2-melee-pose` (draft PR #64), based on main `783b2b3d` |

The packaging script names every agent-built package `…-diagnostic.zip`; the alpha identity is the version and this record
([package-identity.json](package-identity.json), verified by `scripts/Test-MountedAlpha2Package.ps1` in the focused gate).

## Install

1. Back up `%USERPROFILE%\AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\Saved Games` and your current
   `<Kingmaker>\Mods\KingmakerMountedCombat` folder (alpha 1). Keep that backed-up save set: it is your rollback.
2. Test on a copy of a save. Close the game, copy the package's `KingmakerMountedCombat` folder into `<Kingmaker>\Mods`,
   replacing the old copy. UMM should list version `0.2.16-alpha.4`.

## Roll back

Close the game, restore the alpha 1 folder, and load your **backed-up alpha-1 saves**. Whether alpha 1 loads a save written
by alpha 4 is **unverified**; do not rely on it.

## What changed (source-level; none observed in the game)

| Issue | In this alpha |
| --- | --- |
| MMR-01 rider too high | The Horse seat now settles on your rider's own still pose about two seconds after mounting (measured live, not a stored number), so first mount, load and remount should end at the same height. Each mount logs `Horse seatCalibration: phase=attach` and then `phase=settled`. Mammoth and Small riders unchanged. |
| MMR-02 TB attack click not approaching | When the game ignores your rider's melee click on an enemy, KMC accepts it only if the strike is already legal from where the pair stands, or the horse can lawfully walk (its own remaining movement and step mode, and only if it may still move this turn) to a spot from which the strike is legal; the horse then walks exactly that checked path. Otherwise nothing happens, as before. Each such click logs `…dropped by native's path check: decision=…; mountGrant=…`. |
| MMR-03 horse not attacking in TB | One ordinary melee click carries to the horse on its own turn-share, also after the pair moved to reach the enemy. A bow order, a newer order to either actor, an explicit horse order, an AI intent and separate turns do not spend the horse. |
| MMR-04 RT standing still | The game's own AI choice is kept, except that when it ranks two enemies already in reach equally (its order is then arbitrary) the pair takes the nearer. The pair's own movement no longer leaves a Hold on the horse. Any Hold on the horse (including an old one from a save) keeps automatic attacks still: press **Stop** once if the horse shows Hold and the pair will not engage. Once per fight: `Mounted real-time engagement: firstStop=…; order=…; nativeRanking=[…]`. |
| MMR-05 RT pause before advancing | The pair starts moving toward an out-of-reach target right away (whether the rider or the horse will strike first), while the attack itself still waits for initiative/cooldown and swings once. An automatic attack stops if its target becomes hidden. |
| MMR-06 horse Trip | New horses never get the trip. **An existing horse is only fixed when you ask** (below). |
| MMR-07 bite animation | No animation change; each strike logs `Horse strike: …` (animation-system facts, not proof of what was drawn). |

### Existing horse that still trips (MMR-06)

New horses no longer get the trip. A horse from alpha 1 or earlier keeps it until you ask, because a TrippingBite granted another
way looks identical in the save. When such a horse is loaded, the log (`output_log.txt`) shows a line starting `KMC Horse Trip
repair candidate` that names the horse and its exact request line, `horse=<UniqueId>`. KMC writes no file on its own.

To apply: back up your save, load it with that horse in the party, and create the folder `%USERPROFILE%\AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\KingmakerMountedCombat\` if it does not exist. In it,
create `horse-trip-repair-request.txt` containing only that line (blank lines and lines starting with `#` are allowed; any other
content, or a second horse line, makes the request refused). Within a few seconds:

- **Applied:** the request is renamed `…request.txt.claimed`, then `…request.txt.applied`; `horse-trip-repair-result.txt`
  explains it, and the log shows `Removed the inherited stock TrippingBite from the KMC Horse at the owner's request`. Only that
  one fact is removed from that one horse; bite and hoof attacks, other horses, wolves and dogs are unchanged.
- **Refused:** a malformed request, or a horse with no such fact or more than one, gives `.refused`; nothing changed.
- **Waiting:** if that horse is not loaded in the party, the request is kept until it is.
- **Interrupted:** if the game closed mid-request, the leftover `.claimed` becomes `.interrupted` and nothing more changes. Check
  the horse (a bite hit followed by a Trip means the fact is still there) and write a new request if needed.

Recovery: the change reaches your save only when the game next saves; reload your backup to undo it (then write a new request
to remove the fact again). If you gave that horse TrippingBite on purpose, do not apply.

## Playtest checklist (all pending; tie observations to `0.2.16-alpha.4`)

1. **Seating:** mount from an unmounted save and wait about two seconds (the rider may sit higher for a moment while the cast
   finishes, then settles). Check the saddle from the side; walk, turn, stop; dismount (rider stands normally); remount at the
   same spot; save and reload once. Send the `Horse seatCalibration:` lines: each mount should show `phase=attach` then
   `phase=settled`, and the settled `appliedSeatFromSource` should match across first mount, remount and reload
   (`phase=unsettled` means the pose never held still and the attachment sample was kept).
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
6. **RT engagement:** with AI on and no Stop/Hold, start a fight near two enemies (also two adjacent ones): does the pair
   advance, on which enemy (the nearer of two equally placed), and what does the `firstStop` line say? Check Stop, Hold on the
   rider, Hold on the horse and AI-off keep it still, and a chosen farther target is kept.
7. **Trip:** a horse bite hit is no longer followed by "Combat Maneuver: Trip" (new horse, or after the repair above). A
   wolf/dog companion still trips.
8. **Bite animation:** watch a horse routine; record a short clip if possible and compare it with that strike's `Horse
   strike:` line (`handle=none` = no animation for that strike; `activeClip=BiteAttack` with a high `speedScale` = the bite clip
   was compressed into a short window).
9. **Mounted Shield:** mount AC and the Mounted Combat check bonus as in alpha 1 (+2); unequipping or dismounting removes the
   AC bonus, with no duplicate after remount or reload.

**Logs to send** (`output_log.txt`): lines with `Horse strike:`, `Mounted real-time engagement:`, `dropped by native's path
check`, `Mounted combat command terminal`, `Horse seatCalibration`, `KMC Horse Trip repair` and `Removed the inherited stock TrippingBite`.

## Known limitations

- No game run: every change is source-level and offline-tested only.
- MMR-01: the settle thresholds are design choices; a mount made mid-fight may settle on the combat stance. MMR-07 changes
  no animation.
- RT ordering: while the rider's early-dispatched attack waits for its cooldown, a horse that becomes ready first attacks after
  the rider's strike, not before.
