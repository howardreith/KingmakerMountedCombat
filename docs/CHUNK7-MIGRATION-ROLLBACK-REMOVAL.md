# Kingmaker Mounted Combat: install, upgrade, rollback and removal (Chunk 7)

Status: DRAFT for a private engineering candidate. It is not a public release, and it does not
authorize installing into a human campaign. Package identity: [TBD after 7E freeze].

**Before anything: back up your saves.** Copy the whole Saved Games folder,
`%USERPROFILE%\AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\Saved Games`, and keep the
preview.211 package. Rollback depends on both.

## Install (fresh)

1. Close the game.
2. Unzip the package. It contains only `Info.json` and `KingmakerMountedCombat.dll`.
3. Put them in `<Kingmaker install>\Mods\KingmakerMountedCombat\`.
4. Start the game, open UMM (Ctrl+F10) and check that Kingmaker Mounted Combat is enabled and shows the
   expected version.
5. Check the UMM log for the version line and no KMC `[Exception]` or error line.

Rehearsed: fresh install into a base-game-only tree, with Mount, move and Dismount
(batch191 development PASS, row 42 in the 7E matrix).

## Upgrade from preview.211

1. Close the game. Back up saves (see above).
2. Replace the two files in `Mods\KingmakerMountedCombat\` with the Chunk 7 files. Keep the folder.
   UMM deletes its old assembly cache there on the next start; that is expected.
3. Start the game and load a save.

What changes:
- The preview.211 start-up exception on a base-game install ("Horse companion blueprint registration …
  no longer has seven stock options") is gone (fix `81c191af`).
- preview.211 wrote mounted data as schema 2. Chunk 7 reads valid schema-1 and schema-2 data through a
  migration adapter, and writes schema 3 on the next save.
- KMC keeps no settings file, so there are no settings to carry over. Real-time Charge starts off.

### What was rehearsed (X5, batch200, development pilot on DLL `ec20ee8f…`)

| Step | What ran | Result |
| --- | --- | --- |
| u1 previous install | preview.211 (`-b` package, DLL `11e99309…`) in a base-game-only tree; loaded a save with no KMC data | PASS 27/0. The one known Horse registration line appeared once, as expected. |
| u2 upgrade | Chunk 7 placed over u1's folder (old UMM cache carried); loaded a Chunk 7 six-Horse Manual; Mount, mounted move, Dismount, unmounted move | PASS 42/0. No KMC error lines; the known line was absent. |
| u3 rollback | u1's folder state restored (preview.211); the same save with no KMC data loaded | PASS 27/0. The same single known line. |

### Gaps (not rehearsed)

- **No save written by preview.211 with a mounted pair was upgraded.** No working preview.211 save
  producer exists for this lab. Mounted-state upgrade is qualified on **migrated derivatives** of every
  supported historical schema (7E rows 17–22, development lineage), not on an archive written by
  preview.211.
- **X3 residual gap:** no cold load of a pending-only archive written by the live producer, with genuine
  PairId and generation. Rows 13–14 cover the live-producer cold load only in the turn-based action-debt
  shape. Row 19 loads a schema-2 resave at generation 1.
- u2 loaded a save written by a Chunk 7 build, not one written by preview.211.
- This is a development pilot. Exact-package 7E qualification of rows 55–56 has not run.

## Rollback to preview.211

1. Close the game.
2. Put the preview.211 files back in `Mods\KingmakerMountedCombat\`.
3. Load **a save made before the upgrade** (your backup).

- Rollback restores the **pre-upgrade** save, not a post-upgrade one. A save written by Chunk 7 is not
  certified under preview.211. The preview.211 build source (`a0d70fa4`, `MountedSaveData.Decode`)
  classifies a schema above 2 as future data. It reports "Mounted metadata uses a newer schema;
  original data is preserved." and restores no pair. That was read from the source only; a native load
  was **not** rehearsed (u3 loads no Chunk 7 save).
- preview.211 logs its known Horse registration exception again on a base-game install (u3 confirmed it).
- **Base-game installs (no mods, no Call of the Wild):** preview.211 cannot register the KMC Horse there.
  It recognized only the Call of the Wild Ranger list (u1 and u3 logged the exception). This is the
  owner's own configuration: the owner's install and saves use no mods. So a preview.211 user on such an
  install never had a working Horse choice. A campaign that acquired a KMC Horse under Chunk 7 cannot be
  rolled back to preview.211 with its Horse intact. Loading such a campaign under preview.211 was not
  tested.

## Removal

1. In game, with no save in progress, open the KMC page in UMM and press **Prepare to disable / remove
   KMC**.
2. Wait for `Prepared: cleanup save KMC_CLEANUP … KMC can now be disabled or removed …`. If you see
   `Not prepared:` or `Preparation unconfirmed:`, fix the stated cause and do **not** remove KMC.
3. Close the game, delete `Mods\KingmakerMountedCombat\`, start the game and load that cleanup save.

- Preparation dismounts every pair, writes one new save and checks it has no mounted pair, no combat
  participation, no KMC control binding and no KMC blueprint.
- **A campaign that contains a KMC Horse still needs the mod.** Preparation refuses while any character
  has the KMC Horse or its companion feature. Only campaigns using stock mounts (for example the Mammoth)
  can be prepared for removal.
- Rehearsed: positive cleanup of a mounted stock-Mammoth campaign, then a separate load with KMC fully
  absent; the world continued and the main character moved normally (batches 65–67, development PASS on
  an earlier candidate; 7E rows 31–32 not yet run on the final package).
- **Do not load a mounted save, or any save with a KMC Horse, without KMC.** That was not tested and the
  outcome is unknown. Never edit a save to strip KMC data.

## Disable without removing

Turning KMC off in UMM dismounts every pair through KMC's cleanup. Disabling is not removal: keep
the files installed and use the preparation flow before deleting them. The toggle is refused, and the
UMM log says why, in these cases:

| UMM log line | Cause | What to do |
| --- | --- | --- |
| `Diagnostic services cannot be disabled while a mounted save is still being written; retry once it finishes.` | A save is still being written. | Wait, then retry. |
| `Diagnostic services cannot be disabled while a mounted save is still being loaded; retry once the area has finished loading.` | A load is still in progress. | Wait until the area has loaded, then retry. |
| `Diagnostic services could not be disabled because mounted cleanup retained residue.` | A pair could not be cleaned up. | Load a save, then retry. Report it if it repeats. |
| `Private-alpha services could not disable without overwriting an externally changed Ranger selection.` | Something else changed the Ranger companion list. | Report it with the other mods you use. |

These texts were read from the source (`CompositionRoot.SetEnabled`).
