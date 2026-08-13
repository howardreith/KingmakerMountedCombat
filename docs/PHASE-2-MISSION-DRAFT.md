# Phase 2 mission draft

Status: BLOCKED — CRITICAL

This is a conditional planning draft, not authorization. No final architecture has been selected and no Phase 2 work may be executed under the current status. The immediate work remains a bounded Phase 1 resume from the missing canonical Working-fixture gate.

## Admission gate: finish Phase 1 first

1. Through Kingmaker, create a canonical `KMC_AUTOMATION_WORKING` fixture. Rerun the documented exact filename audit and require exactly one Baseline candidate and exactly one Working candidate. Reject near-matches; do not rename, copy, open, or infer internal identity from filenames.
2. Re-run the existing offline guard/recovery/protocol regression gates. Do not redesign or claim these components as pending: the KMC-owned fixture guard, crash-safe recovery transaction, v2 schemas and artifact binding, scenario host, and movement/lifecycle engines are already implemented and qualified offline.
3. Before loading either KMC candidate, apply the implemented descriptor guard. It must prove distinct non-linked paths, exact internal names, matching `GameId`/`GameName`/`Area`, Baseline immutability, and a write allowlist admitting only Working. Open no non-KMC archive.
4. Prove in Working that the exact Medium rider has the exact active rank-7+ Mammoth pet, the current Mammoth size is larger, both views and stock agents are valid, the game mode is Default, and combat is inactive.
5. Run the lifecycle, movement, doorway control, selection, formation, pause/cancel, boundary, and visual scenarios twice in fresh processes with exact Mods and Working-fixture restoration evidence.
6. Keep evidence claims scoped. Direct invocation of turn-based/real-time subscriber methods proves handler cleanup only, not real EventBus delivery. Pre-cleaning before real area reload proves cleanup plus reload invocation, not event delivery. Add real transition evidence where safely obtainable or retain an explicit limitation.
7. Continue to deny every stock `SaveRoutine` call in Phase 1. Qualify save safety through cleanup-before-boundary, no custom mounted serialization, unchanged/restored Working evidence, and protected Baseline evidence; do not describe this as a stock save or save-round-trip.
8. Apply the existing kill criteria, disable B on a qualifying failure, rescore A/B/C/D, and publish the final Phase 1 proceed, pivot, manual-review, or stop decision before authorizing Phase 2.

## Conditional Phase 2 objective if Architecture B proceeds

Build the smallest combat-capable mount-authoritative pair without assuming any unproven persistence, action, targeting, animation, EventBus, save, or mode contract.

Order of work:

1. explicit mounted-pair persistence policy and uninstall/repair behavior;
2. rider basic melee attack;
3. mount basic natural attack;
4. movement/action economy;
5. combat start/end behavior;
6. death, unconsciousness, prone, and forced movement;
7. turn-based and real-time parity;
8. charge;
9. reach and attacks of opportunity;
10. ranged/spellcasting restrictions;
11. mounted feats;
12. additional mounts/rider sizes;
13. UI and presentation polish;
14. compatibility and hardening.

Every stage requires exact Kingmaker contract mapping, deterministic tests, two-pass runtime scenarios, save/uninstall safety, non-mounted isolation, and a narrow Harmony surface. Failure of movement authority, cleanup, save safety, presentation, or external-state restoration triggers a C/D pivot; no broader workaround is authorized implicitly.

If Phase 1 selects C or D, discard the B combat order and write a new mission for the selected simplified architecture. If Phase 1 cannot select an architecture responsibly, stop. Do not execute any part of this draft under the current blocked status.
