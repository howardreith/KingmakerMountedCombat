# Phase 2 recommendation

Status: BLOCKED — CRITICAL

## Decision

There is no final architecture recommendation and Phase 2 is not authorized. Phase 1 stopped at the fixture-admission boundary: the exact filename audit found one canonical `KMC_AUTOMATION_BASELINE` candidate and zero canonical `KMC_AUTOMATION_WORKING` candidates. The only Working-like filename was `Manual_299_KMC_AUTOMATION_WORKING_.zks`; it was rejected as a near-match and its archive was not opened. A filename cannot establish its internal identity, and renaming or substituting that file is not authorized.

Architecture B remains a default-off experimental candidate whose movement and presentation have not been qualified. Architecture D is the lowest-risk fallback in the current paper scoring, C remains a presentation-focused fallback, and A should not proceed on existing evidence. None of those observations is a Phase 2 selection.

Confidence is medium that B has a scoped Kingmaker movement seam and low that its movement or presentation will be acceptable. Confidence is high that no responsible architecture decision can be made without save-admitted pair telemetry and the required fresh-process runs.

## Implemented and qualified offline

- The KMC-owned fixture guard is implemented and covered by deterministic harness tests. It requires distinct canonical paths, exact internal save names, matching `GameId`/`GameName`/`Area` identity, baseline immutability, and a write allowlist limited to `KMC_AUTOMATION_WORKING`.
- Crash-safe Working-fixture recovery is implemented with a durable recovery plan, exact hash/length/time and metadata checks, narrowly recognized temporary/sidecar names, quarantine instead of deletion, and idempotent interrupted-move restoration. It never repairs or overwrites Baseline.
- The v2 request/game-result/final-result protocol, artifact-manifest binding, schema validation, scenario-bound load accounting, and host completion/failure paths are implemented and qualified offline.
- The movement/lifecycle engines, telemetry writer, bounded abort path, cleanup retry, and default-off experiment are implemented and pass their offline regression gates.
- Exact Kingmaker/Wrath contracts, deterministic relationship rollback/cleanup, package/source guards, and transactional Mods restoration have already been qualified to their documented non-save boundaries.

These are implementation and offline-test results, not evidence that either KMC fixture passed its internal descriptor guard and not runtime proof of mounted movement.

## Runtime evidence boundaries

- The turn-based and real-time boundary scenario code directly invokes the subscribed handler methods. It qualifies the cleanup service and handler boundary, but does not prove that a real Kingmaker EventBus transition delivered those callbacks.
- The area-reload scenario pre-cleans the relationship and then invokes the real reload operation. Its claims must distinguish cleanup behavior from event-delivery behavior.
- Every stock `SaveRoutine` invocation is denied in Phase 1. The save-safety design can prove pre-boundary cleanup, absence of custom mounted serialization, and an unchanged Working fixture; it cannot be reported as a successful stock engine save or save-round-trip.
- No save-backed lifecycle, movement, selection, formation, doorway, visual, or two-pass fresh-process run has been executed because the canonical Working candidate is absent.

## Proven contracts

- Wrath mount-authoritative command, movement, avoidance, entity/view, selection, formation, lifecycle, and persistence responsibilities.
- Kingmaker pair validation, stock mount movement, rider suppression/avoidance, command origin, selection/cancel, lifecycle, and save/load cleanup control points.
- Default-off relationship state, deterministic rollback/cleanup, exact patches, telemetry, build/package/runtime transaction safety, and fixture recovery behavior at the offline boundary.
- Native rank-7+ Mammoth ownership/size metadata and the bounded `Spine` anchor hypothesis.

## Contracts still requiring runtime evidence

- actual one-mover stability, avoidance behavior, drift, stop/start, turns, doorway control, formation, selection, pause/cancel;
- view-root side effects, rider pose/scale/offset, clipping, camera and selection-circle presentation;
- in-game cleanup residue on combat, death, view, area, mode, save, load, disable, and failure boundaries;
- actual EventBus delivery for mode and lifecycle transitions where only direct-handler coverage currently exists;
- turn-based and real-time mounted action ownership, targeting, attacks, reach, action economy, and any future persistence policy.

## Required next gate

The user must create a canonical Working fixture through Kingmaker. Phase 1 then resumes by rerunning the exact filename audit, requiring exactly one Baseline and exactly one Working candidate, and applying the implemented internal descriptor guard before either save is loaded. Baseline remains immutable; only Working may be admitted for writable runtime qualification.

After admission, run the bounded Architecture B movement/lifecycle suite twice in fresh processes, preserve exact restoration evidence, obtain visual evidence or require manual visual review, and apply the existing kill criteria. Only that evidence may produce a proceed, pivot, manual-review, or stop recommendation.

Explicit non-goals remain full mounted combat, Cavalier, feats, charge, attacks of opportunity/reach, ranged combat/spellcasting, enemy riders, multiple mounts or rider sizes, polymorph, broad indoor support, mounted persistence, and public release.
