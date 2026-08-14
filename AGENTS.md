# AGENTS.md — Kingmaker Mounted Combat

## Product boundary

This repository builds one independent Unity Mod Manager mod and its research tooling:

```text
Product:      Kingmaker Mounted Combat
Repository:   KingmakerMountedCombat
Assembly:     KingmakerMountedCombat.dll
UMM ID:       KingmakerMountedCombat
Namespace:    KingmakerMountedCombat
Framework:    .NET Framework 4.7
Language:     C# 7.3
Game target:  Pathfinder: Kingmaker Enhanced Plus Edition
Harmony:      exact installed legacy Harmony12 compatibility surface
```

It must never become part of, or a required dependency of, Kingmaker Buff Planner, Tabletop Added Rules, Gunslinger, Call of the Wild, Wrath of the Righteous, or another gameplay mod.

## Phase history and active scope

Phase 1 is completed historical evidence. Its final ledger is `25 PASS / 0 attributable FAIL / 0 DEFER`; Architecture B was selected, no K1-K12 criterion fired, and the frozen evidence/package identities remain authoritative. Do not rewrite, rebuild in place, relabel, or reinterpret that evidence.

The active mission is the user-authorized Phase 2 master mission in `planning/MOUNTED-COMBAT-PHASE-2-MASTER-MISSION.md`, executed only on `codex/mounted-combat-phase2-alpha`. It authorizes:

1. transient persistence/uninstall policy, native lifecycle investigation, and one player-facing Mount/Dismount action;
2. one Medium humanoid/Mammoth presentation, UI, camera, and procedural-pose tranche;
3. only after explicit acceptance of the exact Phase 2A visual-review build, a private melee-combat alpha with one rider basic melee attack, one explicit Mammoth natural attack, bounded action economy and movement-to-attack, lifecycle qualification, and bounded reach/AoO/charge stretch work;
4. private diagnostic/playtest packaging and guarded branch publication.

Phase 2 does not authorize a public release, merge to `main`, Phase 3 implementation, persistent mounted state or automatic remount, additional mounts or rider sizes, enemy/AI riders, ranged mounted combat, mounted spellcasting, mounted feats, Cavalier content, or full Wrath parity. Do not silently expand scope.

## Required architecture

Keep these concerns separate:

1. **Forensics** — exact local assembly/member inspection and dependency mapping.
2. **Game adapters** — bounded Kingmaker/UMM/Harmony integration.
3. **Relationship domain** — valid pair, lifecycle, transition guards, cleanup, invariants.
4. **Command routing** — decisions about authoritative mover and delegated commands.
5. **Movement synchronization** — pathing/avoidance ownership and measured synchronization.
6. **View attachment** — rider anchor resolution, offsets, pose/animation observations.
7. **Selection and formation** — player control and party-command behavior.
8. **Runtime testing** — request/result protocol, telemetry, screenshots, restoration.
9. **Diagnostics** — structured contract exports, state snapshots, and reports.

Harmony patches, UMM callbacks, and UI handlers must delegate to services. Do not build a monolithic `Main`, `MonoBehaviour`, patch class, or diagnostic window containing business logic.

## Contract-first rule

Do not write the mounted relationship implementation until `planning/ASSEMBLY-CONTRACT-MATRIX.md` and `planning/MOUNTED-SUBSYSTEM-DEPENDENCY-GRAPH.md` establish:

- the exact Wrath responsibilities being studied;
- the exact Kingmaker candidate hooks or their absence;
- pathfinding/avoidance ownership;
- command-routing control points;
- entity/view position semantics;
- selection and formation control points;
- lifecycle and cleanup events;
- serialization risks;
- the proposed narrow implementation seam;
- confidence and evidence for each claim.

A guessed class name from Wrath is not a Kingmaker contract.

For Phase 2, update the corresponding Phase 2 contract record before each implementation tranche. Combat production is forbidden until the exact Phase 2A review build is explicitly accepted by the user.

## Code and test style

- Match repository conventions once established; do not mix styles.
- Production code must compile under C# 7.3.
- Prefer immutable request/result/state descriptions where practical.
- Keep Unity and Kingmaker static state behind narrow adapters.
- Use explicit composition roots or constructor injection, not a scattered service locator.
- Test behavior: valid/invalid relationship transitions, delegated commands, movement authority, cleanup idempotence, measured residual drift, restored selection, external-state restoration, and structured evidence.
- Prefer realistic integration tests and real runtime scenarios. Mock only an inaccessible external boundary.
- Do not add a test library merely for convenience when a project-owned deterministic runner suffices.
- Every runtime defect receives a regression test/scenario.
- Never weaken thresholds, package allowlists, safety guards, or assertions merely to make a gate pass.

## Runtime safety

- Only repository-owned guarded scripts may stage, deploy, or launch Kingmaker runtime scenarios.
- The harness must prove source validation and `-WhatIf` purity before live use.
- Live `Mods` staging must be transactional, locked, recoverable, and restored exactly.
- Only `KMC_AUTOMATION_WORKING` may be mutable. `KMC_AUTOMATION_BASELINE` and all other saves are protected.
- The prototype must mount only a specifically validated controllable rider and larger active companion in a disposable fixture.
- The Phase 2 relationship remains runtime-only and intentionally nonserialized. Before the accepted combat tranche it must clear or safely dismount on invalidation, combat start, save/load, area unload/transition, mode change, view detach, death/incapacitation, party removal, mod disable, exception recovery, and process exit where observable. Later combat behavior may retain a valid pair only where the accepted Phase 2 contracts and runtime evidence explicitly qualify it.
- Never claim runtime qualification from compilation, detached reflection, a main-menu load, or a screenshot alone.

## Wrath and asset restrictions

- Wrath is read-only and must never be launched or modified by this project.
- Production code must have no compile-time or runtime dependency on a Wrath assembly.
- Do not import or redistribute Wrath code or assets.
- Asset investigation may record names, types, bone/transform metadata, clip/controller identifiers, dimensions, hashes, and screenshots for internal evidence.
- Extracted proprietary objects remain outside Git and outside packages.
- Do not commit bulk decompiled source. Commit original summaries and contract data.

## Git and publication

- Work on `codex/mounted-combat-phase2-alpha`.
- Commit coherent checkpoints.
- Never reset, clean, restore, rebase, force-push, or discard unknown state.
- Use only the project-owned guarded push helper after it passes tests.
- Do not merge to `main` or create a public release.

## Durable records

Maintain at minimum:

```text
planning/MOUNTED-COMBAT-PHASE-1-MISSION.md
planning/ASSEMBLY-CONTRACT-MATRIX.md
planning/MOUNTED-SUBSYSTEM-DEPENDENCY-GRAPH.md
planning/KINGMAKER-WRATH-TYPE-MAP.json
planning/ASSET-RIG-ANIMATION-INVENTORY.md
planning/ARCHITECTURE-OPTIONS.md
planning/RISK-AND-KILL-CRITERIA.md
planning/RUNTIME-SCENARIO-MATRIX.md
MOUNTED-COMBAT-JOURNAL.md
AUTONOMOUS-RESUME.md
AUTONOMOUS-BLOCKERS.md
docs/PHASE-1-FEASIBILITY-REPORT.md
docs/PHASE-1-IMPLEMENTATION-REPORT.md
docs/PHASE-1-QUALIFICATION.md
docs/PHASE-2-RECOMMENDATION.md
docs/PHASE-2-MISSION-DRAFT.md
planning/MOUNTED-COMBAT-PHASE-2-MASTER-MISSION.md
planning/PHASE-2-CONTRACT-MATRIX.md
planning/PHASE-2-RISK-AND-KILL-CRITERIA.md
planning/PHASE-2-RUNTIME-SCENARIO-MATRIX.md
planning/PRESENTATION-POSE-STRATEGY.md
planning/PERSISTENCE-UNINSTALL-POLICY.md
planning/PLAYER-ACTION-UI-CONTRACT.md
planning/COMBAT-ACTION-ECONOMY-CONTRACT.md
planning/TARGETING-REACH-CHARGE-CONTRACT.md
planning/DIAGNOSTIC-TARGET-POLICY.md
docs/PHASE-2A-PRESENTATION-REPORT.md
docs/PHASE-2A-MANUAL-REVIEW.md
docs/PHASE-2-COMBAT-IMPLEMENTATION-REPORT.md
docs/PHASE-2-QUALIFICATION.md
docs/PRIVATE-ALPHA-PLAYTEST.md
docs/PHASE-3-EXPANSION-MISSION-DRAFT.md
```

Every meaningful checkpoint records:

```text
date/time
branch and exact HEAD
active version
work completed
commands/tests run
exact PASS/FAIL counts
runtime evidence IDs and paths
hashes/MVIDs when relevant
rejected theories
current uncertainty
external state and restoration result
exact next action
```
