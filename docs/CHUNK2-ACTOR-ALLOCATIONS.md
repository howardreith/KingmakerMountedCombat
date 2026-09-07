# Chunk 2: Actor allocations and movement conservation

Status: **IN PROGRESS**. Integration branch `codex/mounted-combat-phase3f-playable-core`; intake `aa0bdc41110923a0aae3bd1ac49322e5f2b75c02` and reviewed `b3f063337644215312de97d9736892212777ac1c` preserved. Production attacks/accounting still match Chunk 1. Diagnostic preview.3 repairs observation/cleanup only. No Chunk 2 gameplay qualification is claimed.

## Owner and intake evidence

**OWNER-REPORTED HUMAN PLAY**, preview.13 installed/enabled: TB ordinary attacks including full bow work well; Horse approach/attack works and the tested rider-to-Horse switch yielded no extra move; hovering spends nothing; RT combat/controls/mount/dismount work well. These are accepted without repeat prerequisites. Exhaustive allocations, precise Rapid Shot modifiers, visible Bite recovery, formal Mounted Charge and cold load remain separate questions. [Chunk 1 identities](CHUNK1-ORDINARY-ATTACKS.md#exact-candidate), [human play/installation](CHUNK1-HUMAN-PLAY.md).

Actual host DESKTOP-SRJJ623, Windows 10.0.19045. Actual preview.13 DLL/cache SHA256 `282f0ad326fdfcd11b8d547cecd70456fa1c90c5a8e68aebd8ab5db3fc9c3864`. Fresh intake at 2026-09-07T02:10:36.9284568Z: saves `7332daa55136ab2e7d8ad2c4c4fe496a35e0059a55a48cd07c550c1c810f1d92`; Mods `a4985d9881558608802427bc7758ed631830f61b4978774b3d549473fb1da58b`. Full actual-state snapshots include foreign Mods/settings/caches; historical preview.7 pins are not restoration authority. Intake and independent audits: `analysis-cache/runtime-evidence/chunk2-20260906/`.

## Native contract and causal findings

Exact Kingmaker Assembly-CSharp SHA256 `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`; UMM0.28.2/Harmony12 unchanged. Prepare `06000C3C` interrupts as applicable, ticks AI, clears cooldowns, reapplies acting-command charges, resets reactions, invokes combat round state `0600939D`, round handlers, AI round processing, each-round facts, confusion, then readiness/UI callbacks. Native costs and movement use action time, CurrentSpeedMps and MetersOfFiveFootStep (observed 2.286); exact native formulas use 3/6 time boundaries. Native End Turn (`Game.PauseBind`, `06000CB7`) forfeits unused actions; that is distinct from actual expenditure.

**NATIVE INTEGRATION finding**, trace-B: all three rider-first rounds and six short native moves executed before fixture serialization failed. Rider Move remained zero. At Horse preparation, round-state/AI/readiness callbacks observed Move=0 despite retained delivered movement; the postfix restored it afterward. First-round sequences 122/128/150 show `0 / 0 / 0.3263616`, against retained `0.3263616`. Rounds two and three repeat with `0.3154111` and `0.3215028`. This establishes the postfix is too late. The round-state entry is a source-confirmed narrow candidate seam after native acting-command reapplication; no production repair has yet been selected or qualified.

First-round rider delegation ran with Horse native `CanActInCombat=false`, initiative cooldown `5.00000048`, and no Horse Prepare yet. At round-two rider input, Horse Standard/Move still read `5.326364 / 2.32636237`; the global-round adapter subsequently retained only the new debit. These observations expose the entitlement/readiness ambiguity; they do not prove an early reservation legal or justify clearing cooldowns. Native activation ownership remains under investigation. Current global-round identity, no explicit actor retirement, and mode conversion remain open review targets. No manual new-round replay, forced full attacks, rider resource borrowing or experimental flags are enabled.

## Runtime ledger

| Run ID suffix (all `20260907-chunk2-`) | Candidate/source | Actual result | Restoration |
|---|---|---|---|
| rider-trace-A | preview.1 / `5fa84f0` | FAIL: invalid subminimum navigation query before movement; 31 native events; outer cleanup timeout | PASS actual intake |
| rider-trace-B | preview.2 / `b1fff5a` | FAIL: game JSON defaults serialized integer-key dictionary as array at completion; three native rounds/six moves, 528 events, zero dropped/observation errors; outer cleanup waited on fixture party combat state | PASS actual intake |

Full artifacts are immutable at `runtime-evidence/<run-id>/`; compact B evidence is `analysis-cache/runtime-evidence/chunk2-20260906/trace-B-causal-summary.json`. Preview.3 uses explicit round JSON and a serializer independent of game defaults; retained movement fields are now included. Cleanup leaves combat only for exact disposable party members captured idle before encounter setup, after measured behavior. Both failures remain failures; the next runs regress these fixture defects. No timeout/assertion is relaxed.

## Qualification and next action

A01-A09: TODO; A10 on exact final candidate: TODO. Both orders over three complete rounds plus matched unmounted controls must establish entitlement and callback/cost semantics before the smallest production repair. `T01-native-allocation-trace` qualifies observation coverage only, with `gameplayQualified=false`. Native facts/reaction effects, exhaustion/conversion, mode/session boundaries and full regression are still required.

Latest focused gates: source22/0, COMPONENT345/0, ASSEMBLY CONTRACT448/0, allocation envelope12/0. Earlier applicable suite: visual23, inventory10, harness243, Phase3G14, Phase3H29, ordinary39, host registration15, exact starting-installation6, all PASS/0. The initial misplaced Kingmaker contract entries caused Wrath24/7 and were corrected/rerun; original logs remain. The preview.13 intake registration permits only the three exact Info/DLL/cache hashes, rejects extra files/modified bytes, and keeps whole-tree fresh snapshot restoration.

Next: immutable preview.3 package and matched guarded mount-first, rider-first and unmounted traces; then minimal production repair and final A01-A10 acceptance. Package/source identities and the active run are recorded separately in campaign `ACTIVE-RUN.json`. No permanent install, main merge or public release.

## Manual checks after qualification

Use both actor orders. Compare partial movement with exhaustion through rider/Horse controls; compare move-plus-Horse-attack with double movement; repeat into subsequent native rounds. Accepted Chunk 1 owner play remains recorded above.
