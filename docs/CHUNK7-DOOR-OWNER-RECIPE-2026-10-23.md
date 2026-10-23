# Chunk 7 door: one owner recipe

Status: **OWNER ACTION PENDING.** The 7B ordinary-door row stays OPEN until this seed exists
and the door probe passes on it. All other work continues meanwhile.

## Why an owner step is needed

The door row needs a real ordinary hinged door (`StandardDoor`) near a mounted pair. A
zero-launch census of every `.zks` the project has ever saved, plus both owner masters, finds
only these doors:

| Area | Ordinary doors | Note |
| --- | --- | --- |
| Spider Cave (both parts) | 0 | |
| Oleg's Trading Post | 0 | |
| Manticore Lair | 0 | |
| Thorn River Camp | 0 | |
| Jamandi's Mansion throne room | 0 | |
| Dungeon hub | 0 | |
| Old Cyclops Tomb | 1 | Closed, unrevealed secret door; not ordinary |
| Endless "Area_Dwarf_1" | 9 | A different campaign; its base-only load fails |

The admitted owner seed reveals no other enterable location. Reaching a new one natively
would mean automating global-map travel and story progress, which is out of scope. The
capital route is not retried, and no visited flag or blueprint is fabricated.

## Attempt 1 (2026-10-23): not eligible

The owner saved `Manual_305_KMC_C7_DOOR_SEED.zks` in the Old Cyclops Tomb
(area `81b13ce8…`) next to a wall switch. A zero-launch read of the archive finds:

- one `StandardButton`, revealed (perception DC 12, passed) and enabled: the switch;
- one `StandardDoor` (`077d4200…`) that is closed (`State=false`), unrevealed, with
  perception DC 200: the secret door the census already listed.

The door is opened by the switch, not by clicking the door, so it is not an ordinary
one-click door. The probe's mounted click targets the door itself, and the closed-door
extension would not change that. The save stays in the owner's folder untouched and
is not admitted. The row stays OPEN.

**Easier route for the next attempt:** on arrival in each candidate location, make one
ordinary manual save (any name starting `KMC_C7_DOOR_`) without opening anything or
arranging the party. A zero-launch read of each save tells which location has an
ordinary `StandardDoor`, and only that location then needs the steps below.

## Attempt 2 (2026-10-23): census PASS, native qualification pending

The owner re-saved `Manual_305_KMC_C7_DOOR_SEED.zks` (SHA-256 `2f55b3e4…bd7210`, 4,067,560
bytes) in the Old Sycamore (area `fb4cffc2…`), in front of the door by Chief Sootscale's
lair, with the nearby enemies dead. A zero-launch read finds:

| Door | Open | Enabled | Revealed | Lock |
| --- | --- | --- | --- | --- |
| `3d9a8eff…` | no | yes | yes | already unlocked |
| `c40b33b4…` | no | yes | yes | not unlocked |
| `2e0b98fa…` | no | **no** | no | — (disabled, not a candidate) |

Two ordinary, revealed, enabled `StandardDoor` records exist, and the archive has zero
KMC or observer references. Both doors are **closed**, so the probe needs the closed-door
start (open by the mounted click, restore closed). Door-to-pair distances are not stored
in the save; the probe's read-only native inventory decides them. A read-only master copy
is frozen at `fixtures/owner-supplied/KMC_C7_DOOR_SEED_OLD_SYCAMORE.master.zks` (same hash).
The owner's save is not written. The row stays OPEN until the native probe passes.

## Recipe (about 15–30 minutes of ordinary play)

**Preparation**

1. Make sure no lab run is in progress: no Kingmaker process, and no
   `C:\Dev\KingmakerMountedCombatLab\runtime-state\active-transaction.lock`.
2. Make a fresh ordinary copy of
   `C:\Dev\KingmakerMountedCombatLab\fixtures\owner-supplied\KMC_C7_NO_MODS_OWNER_SEED.master.zks`
   (SHA-256 `3405d7c8…`, 1,239,025 bytes). Do not use a link or move the master.
3. Put the copy in your Kingmaker save folder under a new, unused file name, for example
   `Manual_390_KMC_C7_DOOR_START.zks`. Never overwrite or rename any `KMC_AUTOMATION_*` save
   or any of your own saves.
4. In Unity Mod Manager (Ctrl+F10 in game, or its installer), switch **every** mod off for
   this session, including KMC and the observer if present. The seed was made without mods
   and must stay free of mod data.

**In the game**

5. Load the copy.
6. Travel by the ordinary global map to the nearest location you can reach that is **not** any
   of these: Oleg's Trading Post, Spider Cave, Manticore Lair, Old Cyclops Tomb, Thorn River.
   Old Sycamore is a likely candidate, but any location works if it has a normal door.
7. Find an ordinary hinged door that opens with one click. It must not be secret, locked or
   tied to a quest or dialogue. Avoid story dialogue choices along the way; leave or skip
   them where you can.
8. Click the door once so it stands **open**.
9. Gather the party on flat walkable ground **8–20 m** from the door, out of combat, with no
   dialogue or cutscene, and with walkable floor on both sides of the doorway.

**Save and finish**

10. Make one new manual save named exactly `KMC_C7_DOOR_SEED`.
11. Exit the game normally, then switch your usual mods back on.
12. Tell me the save's file path, the location's name, and anything unusual on the way (fights,
    dialogue, level-ups).

## What happens next (no further owner action)

1. A zero-launch census of your save confirms an ordinary `StandardDoor` record that is
   enabled, open and not secret.
2. The new seed replaces the old owner-seed pin in a new closed source mode. The existing
   setup then runs on a disposable copy, guarded and with exact restoration:
   - Ranger 4 with the native Horse choice;
   - two native Mounts;
   - a Manual save.
3. The existing door probe exercises the real mounted door click with a selection change,
   preserving the other actors and pair.

Your master saves are never written. If no reachable location has a usable door, say so; that
result is recorded as a fixture limitation, not as a waived requirement.

## Native route for the Old Sycamore seed (offline-prepared, not yet run)

**Route.** The registered seed is consumed by the existing `owner-seed-exploration` case of
`chunk7-owned-two-pair-campaign`; no new setup chain is needed. That case loads the seed, builds
the main (Ranger 3, 11,052 XP, so exactly Ranger 4 at its ready level) and rider B
(`fd213210…`, level 4, no Ranger levels; native XP assistance to the exact minimal level 8, as
before) to Hunter's Bond, mounts both pairs, and at its stage 7 runs the door probe when a
qualifying door is within bounds; otherwise it records `door.applicable=false`. Selection
differences from the no-mods seed: `seedId KMC_C7_DOOR_SEED_OLD_SYCAMORE`, its SHA-256, and
five `partyIds` (`7e5cf212…` joined the party).

**Seed intake.** Registry entry plus intake record
`fixtures/owner-supplied/KMC_C7_DOOR_SEED_OLD_SYCAMORE.intake01.json` (SHA-256 `8919ad87…`).
The real owner-seed source gate (`Get-KmcChunk7OwnedPairsSource`) admits the selection offline.
An isolated native cold load is still required.

**Closed-door start (product change, needs review and a new candidate).** `Chunk7NativeDoorProbe`
now also qualifies a closed door that is natively interactable, preferring an open door when
both exist. A door captured closed:
- needs no setup close before the first input;
- is closed again by setup only before the second input (`setupInteractions = 1`);
- has an active cut while closed (`originalCut = !originalOpen`);
- is restored closed through the same native interaction toggle, since `StandardDoor` has no
  public Close.

The reader admits exactly these two shapes.

**Open risks the native run decides.**
- *Distance.* The party was saved right in front of the door. Both mounts must start more
  than the interaction radius + 0.5 m and at most 28 m from the door. If they are too close,
  the run records the door as not applicable. In that case a bounded approach or retreat is a
  further product change.
- *Locks.* Door `c40b33b4…` has never been unlocked. A locked door selected first would fail
  natively, not pass.
- *Suite.* The owner's save folder gained new saves, so the qualification suite snapshot must
  be refreshed together with the new candidate.