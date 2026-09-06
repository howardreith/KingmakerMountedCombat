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

## Revised completion plan and active scope

Phase 1 is completed historical evidence. Its final ledger is `25 PASS / 0 attributable FAIL / 0 DEFER`; Architecture B was selected, no K1-K12 criterion fired, and the frozen evidence/package identities remain authoritative. Do not rewrite, rebuild in place, relabel, or reinterpret that evidence.

The owner's 2026-09-06 Chunk 1 mission supersedes historical Phase 2/3 scope and branch restrictions. Work on `codex/mounted-combat-phase3f-playable-core`, preserving reviewed ancestor `1d2b8c3ccad14009653af9dc6420ee9af7b2e804` and legitimate descendants. See [CURRENT.md](CURRENT.md) for exact baseline, active candidate and gates, and [Phase 3H evidence](docs/PHASE3H-IMPLEMENTATION.md) for history. The revised plan is local evidence at `C:\Dev\KingmakerMountedCombatLab\handoffs\Kingmaker_Mounted_Combat_Revised_Project_Plan.docx`.

Chunk 1 authorizes ordinary stationary attack correctness, scoped prediction/routing/native attack and minimal allocation repairs, fixture/protocol changes, coherent commits, guarded branch publication, private packaging and temporary transactional runtime validation. Preserve the native UnitAttack-derived sequence, native planning/effects/costs, explicit single Primary, one pair and existing Horse/Mammoth profiles. Keep `EnableUnifiedMountedTurn=false`, `EnablePairedCommandScheduler=false`, `EnableDiagnosticOverlay=false`. Mount transport spends mount resources; carried motion alone adds no rider Move cost or tabletop melee restriction.

The seven-chunk roadmap is: ordinary attacks; actor allocations; pair-aware activations; sustained combat/UX; persistence; remaining combat features; multiple pairs/profiles/release. Only Chunk 1 is active. Do not implement the full scheduler, new content/art/profiles, persistence, feats, charge or casting here. No main merge, release or permanent installation is authorized.

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

Retain the established relationship architecture and historical contract evidence in `planning/ASSEMBLY-CONTRACT-MATRIX.md` and `planning/MOUNTED-SUBSYSTEM-DEPENDENCY-GRAPH.md`. Verify exact installed signatures and call order for newly touched integration boundaries, covering the relevant:

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

For Chunk 1, reproduce matched unmounted-enabled, mounted-ordinary and mounted-Primary conditions on one exact build. Trace prediction through native mode selection, admission, start, planning, delivery, completion and costs before choosing a repair. Prediction must be read-only; execution revalidates. Do not force full mode, manufacture attacks, clear cooldowns or advance turns inside certified behavior. Two equivalent failed traces require a different hypothesis or a missing observation next. Keep historical failures intact. Native integration qualifies gameplay; component checks and assembly contracts cannot prove callback ordering. HUMAN PLAY and safe mod-absent certification may remain pending; missing mandatory native evidence means BLOCKED, not complete.

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
- Use the currently qualified disposable working fixture and validated controllable pair. Native mod-absent controls require a rider/save without permanent KMC references; never strip custom content from a save.
- Snapshot actual intake installation, caches/settings and protected saves. Never restore historical pins over newer human state. Do not launch over a human session, kill unrelated processes, hot-replace DLLs or alter foreign mods. Stop on authentication, updates, cloud conflicts or unexpected dialogs.
- The relationship remains runtime-only and nonserialized; preserve current lifecycle cleanup and accepted combat retention contracts. Do not delete UnifiedMountedTurnCoordinator: active movement accounting still uses it with the experimental flags false.
- Never claim runtime qualification from compilation, detached reflection, a main-menu load, or a screenshot alone.

## Wrath and asset restrictions

- Wrath is read-only and must never be launched or modified by this project.
- Production code must have no compile-time or runtime dependency on a Wrath assembly.
- Do not import or redistribute Wrath code or assets.
- Asset investigation may record names, types, bone/transform metadata, clip/controller identifiers, dimensions, hashes, and screenshots for internal evidence.
- Extracted proprietary objects remain outside Git and outside packages.
- Do not commit bulk decompiled source. Commit original summaries and contract data.

## Git and publication

- Work on `codex/mounted-combat-phase3f-playable-core`.
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
