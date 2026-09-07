# Chunk 2: Actor allocations and movement conservation

Status: **IN PROGRESS**. Intake branch `codex/mounted-combat-phase3f-playable-core`, HEAD `aa0bdc41110923a0aae3bd1ac49322e5f2b75c02`; no unknown changes. Reviewed `b3f063337644215312de97d9736892212777ac1c` and descendants preserved. Chunk 1 source/package remain frozen; [identities](CHUNK1-ORDINARY-ATTACKS.md#exact-candidate).

## Intake and owner evidence

Host `DESKTOP-SRJJ623`, Windows 10.0.19045, expected lab and checkout found. Local installation record and actual Info.json identify preview.13. No Kingmaker/Wrath process was present at intake. Freshly inventory actual installation, settings, caches, protected saves and foreign Mods before runtime; historical preview.7 pins are not restoration authority.

**OWNER-REPORTED HUMAN PLAY**, supplied with this mission: preview.13 installed and enabled; TB ordinary attacks including a full bow attack work well; Horse approach/attack works, and the tested rider-movement to Horse-control switch did not yield another move; hovering spends nothing; RT combat, controls, mounting and dismounting work well. This does not establish exhaustive allocations, precise Rapid Shot modifiers, visible Bite recovery, formal Mounted Charge, every mount action or cold load. Accepted checks need not be repeated as a prerequisite.

## Native contract and causal findings

IN PROGRESS. Existing adapter keys records by combat controller/global round/start time; restores expenditure in a Prepare postfix; consults native Standard before preparation; retains actor references without retirement. These are inspection targets, not yet reproduced native failures. UnifiedMountedTurnCoordinator still owns active movement accounting with its experimental setting false.

## Qualification ledger

No Chunk 2 gameplay PASS is claimed at intake. A01-A09: TODO; A10 exact final candidate: TODO. Required trace: both fixture initiative orders over at least three complete native rounds, corresponding unmounted controls, actual command/cost/callback observations without measured resource or initiative resets.

## Manual checks for new behavior

After a candidate is qualified: use both actor initiative orders; compare partial movement with exhaustion across rider/Horse controls; compare move-plus-Horse-attack with double movement; repeat into subsequent rounds. Previously accepted Chunk 1 human behavior remains recorded above.

## Checkpoint

Intake: source unchanged; no runtime launched or external state mutated. Next: inspect exact installed Prepare/movement/charge contracts and build the focused allocation trace through the qualified fixture/harness.

## Allocation contract inspection checkpoint

IN PROGRESS. Exact installed Assembly-CSharp SHA-256 `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`; UMM 0.28.2 and legacy Harmony12 unchanged. Native Prepare `0x06000C3C` interrupts as applicable, ticks AI, clears cooldowns (`0x0600C3BE`), reapplies acting-command costs, refreshes reactions, runs combat round state (`0x0600939D`), round handlers (`0x06000C7F`), AI round processing, each-round facts (`0x0600A2D2`), confusion, then readiness/UI handlers (`0x06000C80`). Existing allocation reconciliation is later, in the postfix. This order is ASSEMBLY CONTRACT, not proof of a runtime symptom.

Native movement uses action-time units, actor CurrentSpeedMps and native MetersOfFiveFootStep; native remaining-time calculation uses 3/6 time boundaries. Command-end rounding (`0x06000C5E`) depends on the actual command and current actor. Game.PauseBind (`0x06000CB7`) invokes the normal End Turn flow including its native forfeiture; the trace does not substitute ForceToEnd(false) or edit measured readiness. Supplementary original member inventory/bounded references are local at `analysis-cache/chunk2-native/`; proprietary source is excluded from Git and packages.

Diagnostic preview.1 adds read-only boundary observations and stable order/control scenario parameters to the existing guarded Horse fixture. Initiative stat inputs change only before the encounter and restore after cleanup. Production allocation and attack behavior are unchanged. `T01-native-allocation-trace` qualifies complete observation coverage only and explicitly does not close A01-A09. New protocol negatives reject missing/duplicated callbacks, wrong order, incomplete rounds, state changes during measured setup, missing restoration and false gameplay promotion.

Offline checkpoint 2026-09-07T02:28:57.0813918Z: COMPONENT345/0; ASSEMBLY CONTRACT446/0 (422 Kingmaker,24 Wrath); source22, visual23, inventory10, harness243, Phase3G14, Phase3H29, ordinary39, allocation protocol11 and host registration15 all PASS/0. Initial assembly run misplaced seven new Kingmaker entries in the Wrath list (24/7); corrected only their target grouping and reran both exact assembly suites (446/0). Original failure retained in trace-gates.txt; final trace-contracts-final.txt and trace-components-final.txt are local. No gameplay assertions weakened or native success claimed. Next: immutable diagnostic package and fresh snapshot/guarded trace.

