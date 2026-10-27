# Chunk 7 batch189 — six-mounted native Manual and separate cold restoration PASS

Status: development **PASS** for two declared native scenarios on one frozen candidate. The
standing completion mission remains **IN PROGRESS**. No engineering-completion,
performance-acceptance or HUMAN PLAY claim follows.

## Frozen inputs

Both runs used the batch185 cold-qualified six-Horse fixture. They were base-game-only and
launched as orphaned processes with exit receipts; no owner action occurred.

| Input | Identity |
| --- | --- |
| Source commit | `a2ef0e9555176fab592380c720dbb7608e8bc21d` ([preparation record](CHUNK7-B189-SIX-MOUNTED-MANUAL-AND-COLD-2026-10-22.md)) |
| Version / qualifier | `0.1.0-chunk7b-preview.94` / `c7b-world189-sixpersist` |
| ZIP SHA-256 | `dfb2921c5e4c797142ea54807f818b8996de450cf960818ea2bb8093e8c7c3ef` |
| DLL SHA-256 | `4b227428024ddbaae3720f4b22b11330e1a46d0ad0f93299837d8eee9b10d21c` |
| DLL MVID | `bba2981d-654a-4150-83da-fdfbe74015f0` |
| Package manifest | `ec1aea92fad13f5f3c2bc0a1341047ac953cb2685c042cf25fb4eb7ea93e8478` |
| Suite | `chunk7b-completion-b189-sixpersist-20261022` |
| Suite SHA-256 | `e51a5b7e645b698370d209fa2a1dda1c8817b83411892a68d98222eac62d62b5` |

Before launch, the package was built once from the clean pushed commit. Candidate
(`-SkipBuild -AssemblyTarget Kingmaker`) passed 6/0 in 3.75 min. Each run's
development-scoped WhatIf passed with the unchanged loader dependency digest `db2c3901…8921`.
The cold selection could be written only after the producer settled. Its real source gate was
then exercised read-only first: it passed in 3.6 s, re-running the six-pair reader on the
producer's evidence and verifying the Manual309 bytes.

## Producer — `20261022-c7b-world189-b189s1-sixsave`

Case `six-horse-mounted-persistence-save`, checkpoint `multi-exploration-manual`, selection
SHA-256 `1b6b450a…851c`.

- **Game:** PASS 6/0 in 83 s and 2,302 frames; 1 load and 1 save. Rows:
  - `C7-owned-pairs-inherited-custody`
  - `C7-persistence-six-native-mounts`
  - `C7-persistence-six-mounted-archive`
  - `C7-persistence-native-category-all-pair-archive`
  - `C7-persistence-six-native-continuation`
  - `C7-persistence-cleanup`
- **Mounts:** six native selected-ability Mounts in role order, 0 refusals.
- **Hotbars:** zero KMC hotbar controls before binding; then exactly each rider's own Dismount
  and Rider Primary (twelve slots).
- **Manual:** one native Manual, `Manual_309_KMC_C7_SIX_MOUNTED_WORLD.zks`, 1,333,701 bytes,
  SHA-256 `666b561fe9be90ee092936ddf096d2b962321b6d0ca488d7ed3f63ac73ed5bc8`. Its read-back
  holds 6 pairs and 12 slots, with no participation and no combat. Every role's owner,
  generation and declared profile matches (three Medium, three Small).
- **Continuation:** six fixed two-metre native ground moves, 1.95–1.99 m with residual at most
  0.22 m.
- **Cleanup:** settled; inherited pets preserved, selection restored.
- **Receipts:** game result `d64939e2…`; evidence manifest `059e1735…`; persistence evidence
  `065f37c5113d9d60dbe1f67c5b85ff977d47bbcc3f5917fbd1a2f02189a67356`.

## Cold consumer — `20261022-c7b-world189-b189s2-sixcold`

Case `six-horse-mounted-persistence-load`. Selection SHA-256 `20895ec6…1b2d` pins the producer
run and its persistence evidence.

- **Game:** PASS 3/0 in 57 s; 1 load and 0 saves. Rows:
  - `C7-persistence-cold-all-pair-identity-and-no-prepare`
  - `C7-persistence-six-native-continuation`
  - `C7-persistence-cleanup`
- **Restoration:** the separate process (producer 1328, consumer 41948) restored the whole graph
  to presentation. It made 12 semantic restores, 6 presentations and 12 native post-loads. The
  observer recorded 12 debt restores before and after, with 0 Prepare, 0 round, 0 combat-actor
  restores and 0 native inputs.
- **Continuation:** six moves through the restored pairs, with the same bounds.
- **Separate-process proof:** the runner's proof passed, and was confirmed again read-only. The
  loaded archive equals the producer's read-back. Owners, generations and slots are identical,
  on the same candidate, with an unchanged roster, progression, remote Linzi and pair provenance.
- **Receipts:** game result `3c06451c…`; evidence manifest `a11cc2fe…`; persistence evidence
  `eaaacc1b…`.

## Restoration

For both runs:

- Outer PASS, with Mods restored, save protection passed, baseline immutable, Working restored
  and the save write-allowlist passed.
- The restored protected-save inventory digest is `df434471…61ab`.
- Run, transaction and save-transaction records are `restored`.
- Profile bytes were restored exactly, with zero accepted cache changes.
- Base-game isolation: stage verified, dependencies unchanged, process exited.
- No Kingmaker process or transaction lock remains.

## What this closes and what it does not

F09 now has development evidence for six-pair lifecycle stress (batch188) and for genuine
six-mounted native persistence with separate-process cold restoration (batch189), on exact
candidates. The six-pair *performance* half of the stress is still open: batch188's ~5 ms KMC
Update was observer overhead, because the six-pair observer's per-frame custody check hashes its
source receipt inside the timed update. A representative measurement is F11 work.

Still open:

- Final-package private 7E.
- Remaining F10, F11 and door work.
- SHOULD-FIX 3: the unattended exit's real result through the full game-result reader. The exit
  was not exercised in either run.
