# Kingmaker Mounted Combat: Chunk 7 release notes (DRAFT)

**This is not a public release.** It is a private engineering candidate. Owner/HUMAN PLAY acceptance is
pending, exact-package 7E qualification has not run, and nothing here authorizes installing into a human
campaign.

- Package: [TBD after 7E freeze]
- DLL / MVID / commit: [TBD after 7E freeze]
- Previous release: `v0.1.0-chunk6-preview.211` (package version `0.1.0-chunk6d-preview.211`)

## Highlights

- Mount Companion and Dismount are ordinary game abilities. The UMM Mount/Dismount buttons and
  experimental toggles are removed. **The action bar is not pre-filled:** place the abilities by hand.
- Up to six mounted pairs. Combat is tested with two pairs.
- One Small rider profile: the stock default male Halfling on the KMC Horse.
- The capital dismounts companion pairs on arrival, keeps your main character's own pair mounted and
  still allows saving. Mount again after leaving.
- Multi-pair saves (schema 3) with migration from schema 1 and 2. Saving waits for every pair.
- One setting: optional real-time Mounted Charge, off by default and not saved between launches.

See [the changelog](CHUNK7-CHANGELOG.md) for the full list with commits,
[the support and configuration guide](CHUNK7-SUPPORT-CONFIG.md) for settings, logs and messages, and
[install, upgrade, rollback and removal](CHUNK7-MIGRATION-ROLLBACK-REMOVAL.md).

## Fixed

- **preview.211 logs a Horse registration exception on base-game-only installs.** At start, without
  Call of the Wild, it logs:
  "[Exception] Horse companion blueprint registration: InvalidOperationException - The exact Ranger
  companion AllFeatures surface no longer has seven stock options."
  It recognized only the Call of the Wild Ranger companion list; the base game has twelve options.
  Every preview.211 user on a base-game install hits it, the owner's own configuration included (no
  mods, no Call of the Wild), and on such an install the Horse choice was never registered.
  Fixed in `81c191af`: Chunk 7 recognizes either list exactly. The upgrade rehearsal saw the line
  under preview.211 (u1, u3) and not under Chunk 7 (u2).

## Upgrade, rollback and removal: what is and is not proven

- **Mounted-state upgrade is qualified on migrated derivatives, not on an archive written by
  preview.211.** The upgrade path for saves is carried by the schema-1/2 to schema-3 migration chains
  (7E rows 17–22), whose inputs are derivatives made from schema-3 archives. No save written by
  preview.211 with a mounted pair was upgraded. **Loading a preview.211 save under this build is untested
  by automation**; it is an owner check ([owner play checklist](CHUNK7-OWNER-PLAY-CHECKLIST.md) §11),
  together with placing Mount Companion and Dismount on the action bar by hand (§2) and the capital
  dismount on arrival and re-mount after leaving (§9).
- The installation-level rehearsal (X5, batch200, development pilot) passed: u1 preview.211 install
  27/0, u2 upgrade 42/0, u3 rollback 27/0. Every run restored exactly.
- **Rollback restores the pre-upgrade save, not a post-upgrade one.** Keep your pre-upgrade saves and the
  preview.211 package. A save written by Chunk 7 is not certified under preview.211, and u3 does not load
  one.
- **Pending-only save written by the live build:** the 7E campaign now includes a cold load of that save
  exactly as written (genuine pair identity and generation, no migration), 7E row 75.
- A campaign containing a KMC Horse still requires the mod. Use "Prepare to disable / remove KMC" before
  removing it. Removal was rehearsed only for a stock-Mammoth campaign.

## Combat coverage

Two-pair turn-based (both initiative orders) and real-time combat, Mount/Dismount in combat,
incapacitation and death of a mounted rider, Rapid Shot full attacks, the disengagement attack of
opportunity, staged moving actions (move, cast, move; double move and shot; beyond range; exhausted
movement) and real-time Charge with a melee weapon have development evidence (batches 197 and 205).
Exact-package 7E qualification re-runs each of them on the final candidate.

Added to the 7E campaign on October 24, with no development run yet: a scroll cast from inside an enemy's
reach (the game's casting-defensively check), folded into the disengagement row; and a rider knocked out,
healed with a scroll by a companion and standing up next to the enemy (the stand-up attack of opportunity),
folded into the incapacitation row.

**Uncovered (final known set).** These are recorded, not waived, and are not tested by automation:

- C03: base attack bonus variation and a mounted full attack with Rapid Shot off (the test Ranger has
  BAB 4, so the game offers it a single attack only; a full attack needs BAB 6). Synthetic stat edits are
  not accepted. Rapid Shot has development evidence. Haste variation is excluded by owner decision.
- Prepared-spell casting while mounted. The leveled Ranger's level-1 slot is empty. Filling it needs a
  spellbook choice and the game's camp rest. That rest rolls a random encounter the harness cannot
  bound, spends rations and runs skill checks. Items (scroll and potion) while mounted have development evidence.
- The owner's level-6 test save (October 24) holds a level-6 Ranger with base attack bonus 6 and two prepared
  spells, but that Ranger took an animal companion (a Smilodon) at level 4. A Ranger keeps one animal companion,
  so the test Horse cannot be given to it and it cannot ride; prepared casting and a full attack at base attack
  bonus 6 while mounted stay untested and need an owner decision. A character whose animal companion is not the
  KMC Horse (or a supported Mammoth) keeps that companion and is not offered Mount Companion.
- Saves: the upgrade of a mounted save written by preview.211 (above).
- The full Small rider calibration on a save made by the same build: the two cases that did it were
  retired; the Small profile is covered by its own calibration and by mounted save and load.

## Owner decisions (October 24, 2026)

Recorded verbatim; the owner answered a direct question:

- Haste attack variation: "Approve exclusion".
- Main-menu character creation for your own main character, the retired level-up autosave families, the
  legacy Manual 302→303 route, and six-pair combat (six pairs are qualified for performance and save/load
  only): "Approve all four" as exclusions.
- The five Chunk 6A historical replay failures (LedgerProtocol, RegressionLedger, LegacyLedger, AreaLedger,
  UnrelatedFullLedger): "Report as classified". They stay FAIL with an unresolved cause, separate from
  Chunk 7, and are not re-qualified; the current behaviour checks must pass.
- Prepared-spell casting and a full attack at base attack bonus 6 or more: the owner chose to supply a
  new no-mods test save (a level-6 Ranger with a memorized, rested level-1 spell). Pending at the time.

## Known limits

- Turn-based Charge is unavailable (it refuses without spending actions). The defensive Mounted Combat
  feat is not implemented.
- Six-pair combat is not qualified (owner decision, October 24: six pairs are qualified for performance
  and save/load only).
- Other mod combinations are not tested.
- Performance acceptance (F11) is pending on the final candidate.
- Visuals, physical input and feel need owner/HUMAN PLAY acceptance, which is **pending**.
