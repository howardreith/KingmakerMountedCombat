# Chunk 7 batch197: 7E development pilots, fifth pass

Status: **development pilots; not 7E qualification. Every run restored exactly.** Eleven native
launches on one frozen candidate (`c68fabd9`), including one retry of the known Steamworks startup
failure. **Row 57, the two-pair native life and the Small scroll/potion item pilot passed for the first
time**, and the acquired chain and the X4 producer passed again. Row 58 failed closed on the known risk:
in Oleg's, native state moves the whole party except the main character and its pet to remote. That
exceeds the one admitted out-of-scene pair, so row 59 did not run. The standing mission remains IN PROGRESS.

## Candidate (development pilot)

| Item | Identity |
| --- | --- |
| Source | `c68fabd9` (`dd360e32` life count fix, `2f931e52` Small Manual calibration retirement, `c68fabd9` capital out-of-scene pair; review CLEAR), frozen worktree `worktrees/chunk7-7e-b197-source-c68fabd9`; packaged and run from the clean root branch checkout at the same commit |
| Package | `KingmakerMountedCombat-0.1.0-chunk7b-preview.94-c7e-b197-pilots-diagnostic.zip`, `6a7427e7…`, 3,686,246 bytes; `Info.json` (329) and the DLL (7,981,568) only |
| Manifest / DLL / MVID | `f6f62d0a…` / `c0fa0ebe…` / `afafb4fe-b911-4ae3-a953-0fda2acbc85f` |
| Candidate tier | 6/0 (3.75 min), receipt `b197-candidate-01.log` (`f30dd673…`) |
| Suite | `chunk7e-b197-pilots-20261024`, `d3ae951c…` (save digest `4f6375da…`, Mods digest `ed57b9d2…`) |
| Root `bin\Release` DLL | `c0fa0ebe…` before and after packaging (equal to the package DLL) |
| Helpers | `analysis-cache/chunk7-continuation/b197-stage.ps1`, `b197-run.ps1`, `b197-native-launch.ps1`, `b197-derive.ps1`, `b197-check.ps1`, `b197-freeze-step.ps1`, `new-suite-b197.ps1`; freeze record `b197-freeze.json` (`8848e151…`) |

No other agent committed during the batch, so the root runner was used (no worktree branch was needed).
Every run passed its development-scoped WhatIf first. `b197-derive.ps1` reproduced all ten b195 and b196
chain selections byte-for-byte from their receipts before use. Its only b197-specific line maps the c3
link to the retry run `c3r`.

## Runs

| Batch | Run (`20261024-c7e-b197-…`) | Verdict | Key evidence |
| --- | --- | --- | --- |
| A | `c1-ownerseed-cold` | **PASS** (4/0) | artifact `674047a0…` |
| A | `c2-ownerfield` | **PASS** (7/0) | Manual 304 `0d88d948…`; Horse `6c5b58a9…`; artifact `6a0dbf82…` |
| A | `c3-acquired-cold` | **FAIL (startup)** | `SteamManager.Awake`: `DllNotFoundException: steam_api64`, game exited before a result (same signature as b194 c1); log kept read-only at `b197-c3-startup/output_log.txt` (`07fe75ba…`) |
| A | `c3r-acquired-cold` | **PASS** (8/0), the single retry | Manual 305 `f3cf111a…`; artifact `06b30254…` |
| A | `c4-cumulative` | **PASS** (7/0) | Manual 306 `1e7d8644…`; Small Ranger `e00a70d2…`, Horse `40a02e44…`; artifact `7488e3dc…` |
| B | `c5-acquired-postfield` | **PASS** (8/0) | Manual 307 `8dd2ff2b…`, artifact `eb648cf7…` |
| B | `x4s-mounted-save` | **PASS** (5/0) | persistence artifact `c3a286c6…` |
| B | `r58-capital-arrival` | **FAIL** (1/2) | roster beyond the admitted pair; see below |
| B | `r57-ownerseed-life` | **PASS** (9/0) | artifact `5c925cf1…`; see below |
| B | `p2-twopair-life` | **PASS** (9/0) | artifact `219f3e4d…`; see below |
| C | `s1-small-presentation` | **PASS** (11/0) | artifact `79719f9f…`; scroll and potion presentation while mounted |

After every run (PASS and FAIL alike):
- The outer result reports Mods restored, save protection passed, Working restored, Baseline unchanged
  and the write allowlist passed, with restored save inventory digest `95fabf2e…`.
- The external `Test-RuntimeResult`, with its case readers, read every result without error.
- `Params.xml` and PlayerPrefs (including `KingdomDifficulty`) equal the run's own snapshot, and
  `Assert-KmcPersistenceProfileUnchanged` passed with 0 admitted changes.
- No Kingmaker process and no lock remained.

Batches: A = c1, c2, c3, c3r, c4. B = c5, x4s, r58, r57, p2. C = s1. Not run: row 59, which needs row 58's
Manual.

### Row 58: native Oleg's moves the whole party except the main pair to remote

The cold load restored both pairs. The single transfer to Oleg's settled in the capital with the product
behavior unchanged from b196: retired 1, failed 0, active owners 1, save allowed, and the exact UMM line.
The fixture then refused: "Inherited native field roster changed beyond the capital's out-of-scene
companion pair" (`RequireCapitalRoster`). No Manual was written; the bounded exit quit the game normally.

Recorded arrival roster (all 7 actors):

| Actor | inParty | character | remote | inGame | controllable |
| --- | --- | --- | --- | --- | --- |
| Main Ranger `73681ef2…` | true | true | false | true | true |
| Story companion `5dd7bd1d…` | **false** | false | **true** | true | **false** |
| Story companion `8265f8cf…` | **false** | false | **true** | true | **false** |
| Story companion `fd213210…` | **false** | false | **true** | true | **false** |
| Small Ranger (pair B rider) `e00a70d2…` | false | false | true | false | false |
| Main Horse `6c5b58a9…` | true | false | false | true | true |
| Small Horse (pair B mount) `40a02e44…` | false | false | false | false | false |

All seven were in party and controllable before the transfer. So the known risk is confirmed: native
Oleg's also sends the three story companions to remote, not only the hired pair B. Nothing was adapted. Row 59 is blocked.

### Row 57 and the two-pair native life: both PASS, control available in both

- **Row 57 (owner seed).**
  - Native next-round order: main `73681ef2`, rider `fd213210`, `5dd7bd1d`, `29eb9e6a`, `8265f8cf`,
    mount `e9cd259e`, `e3a3be8b`. That gives `riderSlotsBeforeRenewal` 0 and `expectedRiderSlots` 1.
  - The incapacitated rider's round-3 turn matched the derived slot exactly: one Prepare, one end,
    Preparing, Standard 6, one `native:` grant identity, `pairedIdentities` 0 (the `dd360e32` fix holds).
  - Control eligibility rows, both eligible:
    - `5dd7bd1d…` slot 2: not main, not pet, not essential, not immortal, dying allowed, conscious, in combat, unpaired;
    - `8265f8cf…` slot 4: same flags.
  - **Control available:** `5dd7bd1d…` (slot 2, before the survivor's slot 5). Native damage 58; grants
    1 → 3. Its round-3 turn equals the rider's sequence, Standard before/after, and Prepare order before the survivor.
- **Two-pair native life (c5 lineage).**
  - Subject: Small Ranger `e00a70d2…`; survivor: its Horse `40a02e44…`.
  - Order: main, `e00a70d2`, `4519f814`, `fd213210`, `8265f8cf`, `5dd7bd1d`, `40a02e44`, `6c5b58a9`
    (slots 0 / 1 as above).
  - Rider turn exact, `pairedIdentities` 0.
  - Control eligibility rows, all eligible with the same flags: `fd213210…` slot 3, `8265f8cf…` slot 4,
    `5dd7bd1d…` slot 5.
  - **Control available:** `fd213210…` (slot 3, before the survivor's slot 6). Damage 55; grants 1 → 3;
    turn matched.

In row 57 the control's recorded `afterDamage` snapshot still shows `Conscious` (damage 58 against 56 HP).
Its round-3 turn records it unconscious, so that snapshot precedes the native life update.

### Small item pilot: PASS on its proper source

`compiled-owner-seed-fixture-small-horse-presentation-rt` was run on the b53s1 Small fixture source
(`ddf2b9cf…`, selection unchanged from b136). All eleven subscenarios passed: read-only source, native
party placement, production Mount, ground move, both primaries, **scroll presentation and potion
presentation while mounted**, native Dismount, pose observation and cleanup.

## Next

1. Owner/review decision for row 58: the capital admission must cover the native remote-party shape
   (story companions remote but in game), or the row must use an area that keeps the party. Then
   re-pilot rows 58 and 59 with an x4s producer on the same candidate.
2. Exact-package 7E qualification of the rows that now pass in development (X4, row 57, the two-pair life,
   the Small item route).
