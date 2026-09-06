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

Matched run `20260906-chunk1-controls-tb-A`, source `85432cf79de83dc5e76ddc73599549658f50bc4e`, preview.2: C01-C and C01-D PASS; C01-B FAIL. Native unmounted and mounted plans both contain five shots. Mounted completes all five with Standard 6 / Move 3; Primary completes one with Standard 6 / Move 0. Prediction purity and mounted repeated-request continuity PASS. Unmounted command was interrupted after four shots; its cause remains unobserved. Full actual intake restoration PASS at 14:23:02 UTC.

Next experiment: preview.3 adds native interruption caller/target-state observations and 15 matched parameterized controls for ordinary/Primary, Rapid Shot off/on, BAB 6, haste, a post-prediction Staggered condition, native right-click Single choice, and genuinely spent Standard. The aggregate diagnostic deadline scales with the added cases; each leaf retains 30 seconds. Gameplay routing/planning is unchanged. Carried-movement/spent-Move controls and final native regression remain open. No full-mode override, artificial attacks or cooldown reset is used.

Roadmap (only Chunk 1 active): ordinary attacks; actor allocations; pair-aware activations; sustained combat/UX; persistence; remaining combat features; multiple pairs/profiles/release.

Local handoff: `C:\Dev\KingmakerMountedCombatLab\handoffs\Kingmaker_Mounted_Combat_Revised_Project_Plan.docx`. Record source/package/documentation identities separately in the milestone report.
