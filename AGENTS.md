# Kingmaker Mounted Combat agent instructions

Read [docs/VALIDATION-POLICY.md](docs/VALIDATION-POLICY.md) before selecting checks,
then CURRENT.md and AUTONOMOUS-RESUME.md. Ordinary development ends at a reviewable
focused checkpoint; scripts/Test-Tiers.ps1 defaults to Fast. Candidate includes
Fast. Full, package/suite/purity/native campaigns and final ledgers are explicitly
selected qualification work. Earlier mission/handoff chronology cannot select
the 109-stage campaign or 105-row matrix for every edit.

## Product and architecture

One independent Kingmaker UMM mod: KingmakerMountedCombat.dll, .NET Framework
4.7, C# 7.3, actual Kingmaker 2.1.7b and installed Harmony12 contracts. No required
dependency on Buff Planner, Gunslinger/Tabletop Rules, CotW or Wrath.

Keep forensics, bounded game adapters, relationship domain/lifecycle, command
routing, movement synchronization, view attachment, selection/formation,
runtime testing and diagnostics separate. Patches, callbacks and UI delegate to
services. Verify exact local contracts, dependency graph, pathing/avoidance,
native command hooks, entity/view semantics, lifecycle/serialization and the
chosen narrow seam before changing engine behavior. A Wrath name is not a
Kingmaker contract. Preserve Architecture B and stable pair/save identities.

The native engine owns every action budget, preparation, initiative, reaction and
turn. Never clear/refund/synthesize/replay resources or call native Prepare to
manufacture entitlement. Preserve pair-owned command authority, exactly-once
native spending, both initiative arrangements, unrelated actors and unmounted
controls. Keep required paired configuration and the accepted CRPG transport
preset; incompatible legacy authorities/overlay must not run together. Do not
remove UnifiedMountedTurnCoordinator while active movement accounting uses it.

## Runtime and save safety

- Only repository-owned guarded scripts may stage/deploy/launch. Existing live
  safety/identity checks and WhatIf qualification remain before live use.
- Transactions lock, snapshot actual current Mods/settings/caches and protected
  saves, stage controllably, roll back and restore exactly. Never use historical
  preview pins as the current restoration target. Preserve the owner's actual
  installation. Never hot-replace DLLs, alter foreign mods, interrupt a human
  session or kill unrelated processes.
- KMC_AUTOMATION_BASELINE and all human saves are protected. Use only the
  qualified disposable Working fixture or explicitly authorized run-scoped
  isolated save roots; preserve strict older-scenario guards. Mod-absent controls
  need a fixture without permanent KMC references; never strip a human save.
- Preserve lifecycle, invalidation, exception, death, area, disable/removal and
  cold-load cleanup contracts. Restoration must grant no actions or preparation.
- Stop on Steam authentication, purchase, update, cloud, entitlement or unexpected
  dialogs. Native behavior requires structured game evidence; compilation,
  detached reflection, main-menu loading or screenshots alone do not prove it.

## Tests, evidence and assets

Use existing behavior/contract infrastructure and focused regressions for concrete
risks. Do not weaken thresholds, package/dependency allowlists, safety guards,
completion validators or assertions to report PASS. Preserve failed evidence,
frozen binaries, source/reader/fixture identities and old/new reader verdicts.
Full preview.211 acceptance remains pending, as CURRENT.md records.

Wrath is read-only: never launch/modify it or depend on its assemblies in
production. Record original contract summaries and metadata; do not import or
redistribute proprietary code/assets, extracted objects or bulk decompiled source.

## Git and durable records

Use a dedicated codex/* review branch. Preserve unknown state, other worktrees
and frozen artifacts; no destructive reset/clean, history rewrite or force-push.
No merge, release or permanent installation is part of this workflow task.
Use a project-owned guarded push helper when available; otherwise audit the
tracked payload, clean worktree, approved origin and fast-forward relation before
an ordinary push. Record scope, commands/durations/counts, uncertainties, deferred
checks, exact artifacts and restoration where relevant in the current handoff.

Pre-reset chronology and missions remain intact in
docs/history/AGENTS-before-workflow-reset-2026-10-11.md. Consult architecture,
action, persistence and diagnostic-target contracts for the assigned change;
historical next-action/branch/preview pins do not override this policy.
