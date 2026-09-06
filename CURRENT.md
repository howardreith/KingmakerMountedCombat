# Chunk 1: Ordinary attack correctness

Status: IN PROGRESS. Owner mission: 2026-09-06 revised completion plan and Chunk 1 prompt. This replaces historical phase restrictions; it preserves their evidence.

- Branch: `codex/mounted-combat-phase3f-playable-core`.
- Clean intake HEAD and verified host remote: `1d2b8c3ccad14009653af9dc6420ee9af7b2e804`; no unknown changes or other worktrees.
- Historical candidate: `0.1.0-phase3h-preview.6`, binary source `fb65cf826f5914fc29ab5035e6409685bd982c66`. [Exact identities and historical results](docs/PHASE3H-IMPLEMENTATION.md).
- Actual installed intake: `0.1.0-phase3g-preview.7`, DLL/cache SHA-256 `6bc01b982309125963d2355983b37391f244cfea314093fe4b008ad21a241ff3`. Never restore historical installation pins over current state.
- Toolchain: installed Kingmaker assemblies, UMM 0.28.2, legacy Harmony12 compatibility API, .NET Framework 4.7, C# 7.3.

Rules: one existing pair/profile; ordinary native UnitAttack sequence and native costs; explicit Primary single; unmounted behavior retained. Unified turn, paired scheduler and diagnostic overlay remain false. Mount transport consumes mount resources and does not add a rider Move cost or tabletop melee restriction. Relationship remains transient. No new content, permanent install, main merge or release.

Open gates: C01 ordinary/Primary; C02 prediction purity/continuity/revalidation; C03 native Rapid Shot, BAB/haste and spent/restricted controls; applicable native regression. HUMAN PLAY pending: computer-use native pipe unavailable. Mod-absent control requires a safe native fixture without permanent KMC references. Actor allocation completion and paired activation remain later dependencies.

First causal trace: `20260906-chunk1-trace-tb-A`, source `665a559d94d842d16fc3ecf9cd37ab530d30a2c2`, preview.1. Native mode was already Single before mounted routing; the fixture highlighted the target while the pointer still selected ClickGroundHandler. No hostile prediction ran between hover and dispatch. Six historical leaves PASS / longbow leaf FAIL. All 239 trace observations intact. Full intake restoration PASS at 13:46:29 UTC.

Next experiment: preview.2 `ordinary-attack-controls-tb` runs matched C01-B (enabled/unmounted), C01-C (mounted ordinary), C01-D (mounted Primary), using coherent temporary pointer inputs, the native prediction method and native click admission. It also measures repeated prediction and same-target continuity. Gameplay routing/planning is unchanged. No full-mode override, artificial attacks or cooldown reset is used. C03 and final native regression remain pending.

Roadmap (only Chunk 1 active): ordinary attacks; actor allocations; pair-aware activations; sustained combat/UX; persistence; remaining combat features; multiple pairs/profiles/release.

Local handoff: `C:\Dev\KingmakerMountedCombatLab\handoffs\Kingmaker_Mounted_Combat_Revised_Project_Plan.docx`. Record source/package/documentation identities separately in the milestone report.
