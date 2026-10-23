# Chunk 7 batch194: 7E development pilots, second pass

Status: **development pilots; not 7E qualification.** Eleven native runs in two batches of at most
five (plus one Steamworks startup retry), every one with exact restoration. The acquired chain now
exists on one candidate. Four pilots failed; one failure has a focused fix awaiting review, one needs
a decision about a test expectation, one is a fixture limit. The standing mission remains IN PROGRESS.

## Candidate (development pilot)

| Item | Identity |
| --- | --- |
| Source | `1eecc9bb` (batch193 native-life fix, review CLEAR), frozen worktree `worktrees/chunk7-7e-b194-source-1eecc9bb` |
| Package | `KingmakerMountedCombat-0.1.0-chunk7b-preview.94-c7e-b194-pilots-diagnostic.zip`, `ef9b36b1…`, 3,670,458 bytes; `Info.json` and the DLL only |
| Manifest / DLL / MVID | `022035ba…` / `c7e0258f…` / `f7a1a6e2-a29b-4961-93ae-c9d3fccb3dc6` |
| Candidate tier | 6/0 (3.8 min), receipt `b194-candidate-01.log` (`30ecb586…`) |
| Suite | `chunk7e-b194-pilots-20261023`, `1703cb5d…` |
| Root `bin\Release` DLL | `c7e0258f…` before and after packaging |
| Helpers | `analysis-cache/chunk7-continuation/b194-stage.ps1` (read-only selection and plan, WhatIf, WMI launch), `b194-run.ps1`, `b194-native-launch.ps1`; freeze record `b194-freeze.json` |

Every run passed its development-scoped WhatIf first. Every outer result reports Mods restored,
save protection passed, Working restored and Baseline unchanged; the current external
`Test-RuntimeResult` reads every result (PASS and FAIL alike) without error.

## Runs

| Batch | Run | Case | Verdict | Key evidence |
| --- | --- | --- | --- | --- |
| A | `…b194-a1-ownerseed-life` | row 57, owner-seed incapacitation | **FAIL** | Passed fixture, custody, restored mounts, native orders, safe native damage, **native life callback** and **healthy neighbour**. Failed at the survivor check (below). |
| A | `…b194-c1-ownerseed-cold` | b159 shape, owner-seed cold inventory | **FAIL (startup)** | KMC loaded, then `SteamManager.Awake` reported `DllNotFoundException: steam_api64` and Kingmaker exited before a game result (same signature as b154s4 and b177). Log kept read-only at `b194-c1-startup/output_log.txt` (`fcd698bc…`); Steam manifest `StateFlags=4`, build `6757524`, unchanged. |
| A | `…b194-c1r-ownerseed-cold` | same, one retry | **PASS** | Artifact `3c20b61a…`. |
| A | `…b194-c2-ownerfield` | b160 shape, existing Ranger 4 Horse | **PASS** | Manual 304 `0431c37f…`; new Horse `e20f313c…`. |
| A | `…b194-c3-acquired-cold` | b161 shape, acquired field | **PASS** | Manual 305 `04779d01…`. |
| B | `…b194-c4-cumulative` | b163 shape, paid Small Ranger | **PASS** | Manual 306 `8fc41419…`; Small Ranger `afc109d5…`, Horse `95ac4d8d…`. |
| B | `…b194-c5-acquired-postfield` | b164 shape, two-owner Manual | **PASS** | Manual 307 `d01cd299…`, artifact `6937e9fb…`; two owners and a seven-actor field. |
| B | `…b194-p1-realtime-charge` | X2 RT Charge | **FAIL (fixture)** | Feature enabled by the diagnostic lease, straight route, landing clear, but KMC refused: **"Mounted Charge requires a melee weapon."** Rider A (main Ranger) wields no melee weapon in this source. |
| B | `…b194-p2-twopair-life` | X2 two-pair native life | **FAIL** | Same survivor check as a1, on the second lineage. |
| B | `…b194-x4s-mounted-save` | X4 producer, `multi-exploration-manual` | **PASS** | Persistence artifact `a9a79950…`. |
| B | `…b194-x4c-three-area-cold` | X4 three area transfers | **FAIL** | First repeated-world record: "accumulated or lost native KMC subscriber roles" (below). |

## Findings

### Native life: the unconscious rider gets its own native turn (decision needed)

In both a1 and p2 the native turn controller ran `Prepare` for the unconscious rider in round N+1
(a1: `fd213210`, initiative 50; p2: `afc109d5`), with a native grant identity and **no** paired
grant identity, before the surviving mount's turn. The survivor check requires the incapacitated
rider's grant count to stay unchanged, so it fails whenever that rider's initiative falls before the
surviving mount's. Everything KMC owns passed: the life callback, the detach, the healthy pair's
renewal and attack, and the surviving mount's fresh native turn (a1 trace: mount `grantSequence`
+1, Standard and Move 0).

A narrow correction was drafted: admit at most one native, unpaired preparation of the still
unconscious rider in N+1, record it as `incapacitatedNativeTurnGrants`, and have the reader bind it
to the trace. **It was not applied**: the edit was blocked as a weakening of a test assertion, which
is a fair call for a human or reviewer to make. The decision is whether the native turn of an
unconscious unit is accepted engine behavior for this assertion. Until then rows 57 and the two-pair
variant stay FAIL.

### X4: Horse subscriber expectation misses the Small Horse profile (fix committed, needs review)

`RecordRepeatedWorld` expects one `HorseCompanionAddPet` subscriber per pair whose profile id is the
**Medium** Horse profile. `HorseCompanionAddPet` is the native AddPet component of each KMC Horse
owner, and in this lineage both owners have a KMC Horse (the Small Ranger's profile is
`small-default-halfling-horse-v1`, which mounts the same Horse blueprint). The only earlier PASS of
this recorder (b48 three-loads, `dc1c1f63`) had two Medium Horse pairs, and the Small profile
arrived later. The fix counts pairs whose resolved profile mounts the KMC Horse blueprint and puts
the observed counts in the failure message, since the snapshot is not retained on failure. The
original counts were not recorded, so the diagnosis is by code path; the re-pilot confirms it.
A source regression in `Test-Chunk7RepeatedWorldProtocol.ps1` refuses the old expression (checked
against the previous source). Build clean, repeated-world protocol 27/0 plus runner 8/0, Fast 3/0.

### X2 RT Charge: fixture limit

KMC's refusal is correct product behavior. A charge pilot needs a source whose rider A wields a
melee weapon; this lineage's main Ranger does not. Equipping one would be fixture manipulation
unless done by a native weapon-set switch, which the scenario does not perform. Not run again.

### F03 spells and items (zero-launch, on c5's Manual 307)

- Main Ranger: one level-1 memorized slot, empty and unavailable. Small Ranger: none. No rider holds
  any prepared spell, and memorizing needs a native rest no fixture step performs. **Prepared-spell
  casting while mounted is uncovered on this source.**
- An honest item route already exists: `compiled-owner-seed-fixture-small-horse-presentation-rt`
  (Small calibration) casts a native Cure scroll on the Horse and drinks a native Cure potion while
  mounted, from a fixture-declared two-unit stack on the equipped unit, with native spending traced
  (b136: game PASS 11/11, reader-only PASS). It is the next pilot; it was not run in this batch
  because the batch stopped on the X4 fix.

## Next

1. Independent review of the X4 subscriber fix.
2. Decision on the native-life survivor assertion (the unconscious rider's native turn).
3. A successor pilot candidate. The producer-backed cases pin every chain link to the current
   candidate, so x4c needs c1–c5 and x4s re-run on it (six runs) before the re-pilot. Also row 57
   and p2 if the decision allows, and the Small presentation item pilot.
4. RT Charge: choose a source with a melee rider, or record the row as uncovered on the selected
   sources.
