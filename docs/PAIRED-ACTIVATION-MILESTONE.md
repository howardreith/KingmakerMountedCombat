# Combined actor-allocation and paired-activation milestone

Status: **IN PROGRESS**.

## Intake and authority

2026-09-07: clean integration branch `codex/mounted-combat-phase3f-playable-core`, HEAD `45e3d276754257f4513342d5bce7626dd609d252`, including Chunk 2 binary source `c804ba052760063f747cde83265e660916984d72` / `0.1.0-chunk2-preview.10`. No Kingmaker or Wrath process observed at intake. Actual host/install/saves/settings/caches/foreign Mods will be independently inventoried before any temporary transaction; historical snapshots are not restoration authority.

The owner authorizes paired lifecycle implementation for one supported pair mounted before encounter. Rider is initiative principal; each actor retains native health, defenses, commands and action costs. Existing CRPG transport and attack semantics remain. [Chunk 2 evidence and outstanding accounting](CHUNK2-ACTOR-ALLOCATIONS.md), [earlier command admission / K9](PHASE3E-PAIRED-SCHEDULER-IMPLEMENTATION.md), [reference map](../planning/WOTR-SHARED-TURN-MAP.md). Accepted preview.13 [human play](CHUNK1-HUMAN-PLAY.md) remains separate evidence.

## Qualification ledger

| Gate | Status | Evidence |
|---|---|---|
| Whole loop, rider-first pre-pair initiative | IN PROGRESS | A failed on first movement; three complete activations still required. |
| Whole loop, mount-first pre-pair initiative | TODO | Same rider-principal policy required. |
| Supported A01-A09 | TODO | Historical callback/lifetime evidence retained; new activation path unqualified. |
| Final-path A05 | TODO | Exact native preparation/effects/reactions required. |
| Final-candidate A10 and controls | TODO | Accepted current scenarios required on the new path. |
| External restoration | TODO | Actual intake authority required for each transaction. |

Same-campaign cold-load debt rebinding remains a later persistence dependency. Safe rejection of an unsupported transition is not qualification of that transition. No private play recommendation follows from instrumentation or the first gate alone.

## First implementation checkpoint

Candidate `0.1.0-paired-preview.1`, developer setting `EnablePairedActivation=true`; the three old experimental flags remain false. The existing turn service owns encounter/activation identity, per-actor preparation reservation, command admission and completion. A native partner context runs `Prepare` and actor callbacks but never becomes CurrentTurn or runs Start/Tick. The native preparation tail keeps gameplay, round/fact, confusion and readiness operations; only the partner's final cursor/path prediction projection is omitted. The selector injects the exact mount into the existing candidate exclusion expression, retaining native wrap, sorting, round boundaries and unrelated order. No selector retry, initiative mirroring or old partial preparation runs on this path.

Exact installed Kingmaker SHA256 `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7` reverified. Local bounded IL and R/S summaries are under lab `analysis-cache/paired-activation-native/` and the frozen Chunk 2 campaign. Focused component tests **354 PASS / 0 FAIL** and installed Kingmaker contracts **437 PASS / 0 FAIL**; no native gameplay qualification yet.

Fresh actual intake 2026-09-07T12:27:02Z, host DESKTOP-SRJJ623, owner preview.13 DLL/cache `282f0ad326fdfcd11b8d547cecd70456fa1c90c5a8e68aebd8ab5db3fc9c3864`. Save digest `7332daa55136ab2e7d8ad2c4c4fe496a35e0059a55a48cd07c550c1c810f1d92`, Mods digest `a4985d9881558608802427bc7758ed631830f61b4978774b3d549473fb1da58b`. Campaign authority: lab `analysis-cache/runtime-evidence/paired-activation-20260907/intake.json`.

Deployment harness **243 PASS / 0 FAIL**, source validation **22 PASS / 0 FAIL**, native IL construction and rollback **5 PASS / 0 FAIL**. The isolated probe invokes no game methods. Harmony normalizes short branches, so the selector verifier accepts the two equivalent branch encodings while requiring the exact candidate local, field and native exclusion target. The existing mounted allocation registrations select the developer path before mounting, enter TB before encounter, and carry schema 11 for the new three-activation gate; schema 7 remains the historical separate-turn evidence contract.

## First native experiment

Run `20260907-paired-A`, source `f7c9707854c8e4e02f05d803ddfdaa4ec5d702c8`, preview.1 DLL `5a4245b259f7aa032349fe5ed1b1c0f940b1f9fbad2b8ed61699498722647c47` / MVID `08947a25-a328-487f-9f12-bb6a18853ee6`: **FAIL**, 45 assertions PASS / 3 FAIL (one setup subscenario PASS, two subscenarios FAIL). Both actors received exactly one observed preparation, healing and reaction initialization; native initiative inputs were rider 44 / mount -19 and both became ready. The principal reached Acting, but the initial admitted Move never started or debited time. Exact native `UnitMoveTo` constructors call `IgnoreCooldown`; the new blanket guard incorrectly rejected this native movement convention. Preview.2 permits that exact owned native movement type while retaining native movement charging and attack cooldown checks. The first-loop scenario is the regression; it remains unqualified pending success.

Cleanup also exposed the newly tracked rider reference surviving encounter exit. Retirement now discards supplemental pair references at a verified completed encounter while leaving native cooldown recovery untouched. No cold-load debt preservation is claimed.

Actual preview.13/current saves/Mods restoration **PASS**, independent audit `20260907-paired-A-restoration.json`, 13:02:22Z; digests equal intake, game exited and lock absent. Evidence is under lab `runtime-evidence/20260907-paired-A/`; copied native log SHA `33fa2319f1cca398ad2d9dd59ab10a8c4c459a7f490bc5ed1135a5dfcc03acf8`. The earlier automatic approval rejection was resolved by completing the exact repository-owned WhatIf (SHA `08651e96ba6c99c2e76d451d33207eb841a6094cb46e9eaf0c8f632d905fc8da`) and invoking the repository launcher directly. No guard was weakened.
