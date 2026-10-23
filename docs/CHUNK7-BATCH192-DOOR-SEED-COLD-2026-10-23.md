# Chunk 7 batch192: door seed cold load (FAIL, area pin)

Status: **s1 FAIL, restored.** The open-door Old Sycamore seed is registered. Its required
isolated cold load stopped on a product area pin, not on the seed. A minimal fix is committed
offline and **needs independent review and a new candidate** before any native use. The 7B
ordinary-door row stays OPEN; the exploration run (s2) was not started.

## Inputs

| Item | Identity |
| --- | --- |
| Seed | `KMC_C7_DOOR_SEED_OLD_SYCAMORE_OPEN`, master `fixtures/owner-supplied/KMC_C7_DOOR_SEED_OLD_SYCAMORE_OPEN.master.zks`, SHA-256 `90789101…f20a6a`, 4,121,538 bytes; intake `…OPEN.intake01.json` `dd890da9…` |
| Registry | commit `46eb4dd3` (the attempt-2 closed-door entry stays registered as history; never used natively) |
| Candidate | source `46eb4dd3`, `0.1.0-chunk7b-preview.94`, package `…c7b-world192-doorseed-diagnostic.zip` `b56494be…`, manifest `3292b9f5…`, DLL `07e1f4b8…`, MVID `9604b1ba-3da0-4ed2-b0c5-b9138731b34b`; payload `Info.json` + DLL only; Candidate tier 6/0 (257 s) |
| Suite | `chunk7b-completion-b192-doorseed-20261023`, SHA-256 `babaf824…`; fresh save and Mods digests (`4f6375da…`, `ed57b9d2…`) |
| Selections | cold `b192-door-seed-cold-selection.json` `fa3e3d01…`; exploration `b192-door-seed-exploration-selection.json` `1cf4c8d7…` (five-member party, rider B XP assistance to level 8, zero writes) |
| Freeze record | `analysis-cache/chunk7-continuation/b192-freeze.json` |

## s1 `20261023-c7b-world192-b192s1-doorseed-cold`

`chunk7-assisted-cold-inventory`, `owner-seed-readonly-inventory`, base-game only. Runner WhatIf
PASS, then live (detached WMI wrapper, about 2 minutes).

- **Verdict: FAIL.** `Chunk7StockAreaObservation.Capture` threw "Stock route observation requires
  the exact admitted owner-seed area." The observation hard-codes the Spider Cave seed area
  `b10e8651…`; this seed is in the Old Sycamore (`fb4cffc2…`).
- Before the throw, the seed's own native qualification had recorded `qualified=true` with no
  failures. That is not a PASS: the scenario did not complete.
- Restoration: `modsRestored`, `saveProtectionPassed`, `workingRestored` and `baselineImmutable`
  all true; no Kingmaker process and no transaction lock afterwards. Evidence retained under
  `runtime-evidence/20261023-c7b-world192-b192s1-doorseed-cold` (result `0f845668…`).

## Fix (offline, unreviewed)

The cold scenario now captures `stockAreaRouteObservation` only when the loaded area is the
Spider Cave seed area (`Chunk7StockAreaObservation.AppliesTo`); any other registered seed area
records no route claim. The reader already treats the observation as optional and still pins
the Spider Cave area whenever it is present. The offline facade gains the same member.
Checks: Release build clean, `Test-Chunk7AssistedColdScenario` 92/0, Fast 3/0. The skip branch
is not exercised offline (the facade's `AppliesTo` returns true); the native s1 retry is its check.

## Next

1. Independent review of the fix.
2. New candidate and suite from the reviewed commit.
3. s1 retry (cold load), then s2 `owner-seed-exploration` with the door probe.
