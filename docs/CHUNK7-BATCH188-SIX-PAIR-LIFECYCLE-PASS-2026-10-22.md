# Chunk 7 batch188 — six-pair native lifecycle PASS with exact sibling restoration

Status: development **PASS** for one declared native scenario. The standing completion
mission remains **IN PROGRESS**. No engineering-completion, performance-acceptance or
HUMAN PLAY claim follows.

## Scenario and frozen inputs

One declared native scenario, `20261022-c7b-world188-b188s1-sixslot`. Owned-pairs case
`six-horse-cold-fixture-lifecycle-stress`; one load, zero saves, base-game-only staging.
Fixture: the batch185 cold-qualified six-Horse `Manual_308_KMC_C7_SIX_HORSE_WORLD.zks`
(SHA-256 `b1e817a6…bc291`), loaded from an ordinary run-owned copy. Selection bytes were
unchanged from batch187 (`6d064b70…0528`).

| Input | Identity |
| --- | --- |
| Source commit | `64a8206ef0e6293a1b3fc6ec46328c3c6bf76bd2` |
| Version / qualifier | `0.1.0-chunk7b-preview.94` / `c7b-world188-slot` |
| ZIP SHA-256 | `f3797c63ad576481b172a5a1a0926da865bc5eb604839bb54a2010138b695e30` |
| DLL SHA-256 | `595a1297f126ad75145b74dc3a2f7042beb116b09470e58e15cea85918625eb5` |
| DLL MVID | `c1af3e6f-b465-4571-bb29-ca25cb8e8ee0` |
| Package manifest | `5ba3984e40b3678dbd86d2ce921002aab7cb161f360a5f7976f942379055ef60` |
| Suite | `chunk7b-completion-b188-slot-20261022` |
| Suite SHA-256 | `3bac8e7a4c2d860fb3b5b1187b4ad98075a7bc68b047f88446119a18833592fc` |

Before launch: the package was built once from the clean pushed commit; Candidate
(`-SkipBuild -AssemblyTarget Kingmaker`) passed 6/0 in 3.77 min; the suite was frozen; and the
development-scoped runner WhatIf passed with the unchanged loader dependency digest
`db2c3901…8921`. The run was launched as an orphaned process with an exit receipt. No owner
action occurred.

## Results

- **Game:** PASS, 6/0 subscenarios in 115 s and 4,596 frames:
  - `C7-world-fixture`
  - `C7-six-native-mounts`
  - `C7-six-selection-routing`
  - `C7-six-cycles`
  - `C7-six-sampling`
  - `C7-world-cleanup`
- **Game result:** SHA-256 `a8d63b1d65abe1a37baa11d5d67a6478484d9ac0f6e7e778c83e8343aa43f30b`.
- **Outer:** PASS, with evidence manifest `08c284d1…3e0d`.
- **Sibling order:** one shared native rider parent with 16 children. Its exact child
  instance order was verified identical after every settled cycle
  (`siblingOrderVerifiedCycles=3`). This is the native counterpart of the batch187 failure,
  which occurred under the same first-in-first-out order in cycle 3.
- **Inputs:** 36 native Mount/Dismount casts and 0 refusals; six routed ground moves.
- **Sampling:** 15 measured windows (0/1/2/6/0 active pairs).
- **Persistence:** zero save snapshots, semantic restores and presentation restores.
- **Cleanup:** pair residue absent, inherited pets preserved, controls, settings and
  selection restored, native processes and cleanup custody settled.
- **Unattended exit:** not exercised, because cleanup settled normally.

## Restoration

- Transaction phase is `restored`.
- Mods restored, save protection passed, baseline immutable, Working restored, and the save
  write-allowlist passed.
- The restored protected-save inventory digest is `df434471…61ab`.
- Profile bytes were restored exactly, with zero accepted cache changes.
- Base-game isolation: stage verified, dependencies unchanged, process exited.
- No Kingmaker process or transaction lock remains.

## Measurement note (F11 development observation, not acceptance)

Per-frame KMC Update means were about 5 ms in every state, including zero active pairs. The
recorded phase split attributes almost none of that to product pair or turn updates:

| Window | Active pairs | KMC Update | Pair-update phase | Turn-update phase |
| --- | --- | --- | --- | --- |
| 0 | 0 | 4.99 ms | 0.003 ms | 0.003 ms |
| 9 | 6 | 5.27 ms | 0.191 ms | 0.005 ms |

The remaining ~5 ms is outside those phases, consistent with this observer's own per-frame
verification inside the measured update. A representative six-pair performance measurement
must move that verification to window boundaries before any F11 acceptance. Batch157's
two-pair matched pool measured about 0.2 ms in total.

## Remaining

- The six-mounted genuine Manual and an independent cold consumer.
- Remaining F10, F11 and door work.
- Final-package private 7E; batch188 is development evidence on this exact candidate.
- Feeding the exit's real result through the full game-result reader before any
  exit-produced result counts toward qualification.
