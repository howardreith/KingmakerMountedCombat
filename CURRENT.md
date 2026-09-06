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

Matched run `20260906-chunk1-controls-tb-A`, source `85432cf79de83dc5e76ddc73599549658f50bc4e`, preview.2: C01-C and C01-D PASS; C01-B FAIL. Both native plans contain five attacks (four bow attacks and a bite, as the next trace established). Mounted completes five with Standard 6 / Move 3; Primary completes one with Standard 6 / Move 0. Prediction purity and mounted repeated-request continuity PASS. Full actual intake restoration PASS at 14:23:02 UTC.

Latest source/package: `013e923b1f316df33a5592dc0e7832132283b3ca`, preview.3, qualifier `native-attack-variants`; exact identities in the report. Run `20260906-chunk1-variants-tb-A` has 8 PASS / 7 FAIL leaves, with cross-control qualification still red. Full current intake restoration PASS at 14:52:44 UTC.

Correction to the earlier five-shot description: the native plan is four bow attacks plus an existing native bite (BAB 11 with Rapid Shot). Unmounted UpdateTarget rejects the distant bite; mounted ValidateNativeSequenceTarget incorrectly retains the first bow's 16.84 m admission radius. This is a demonstrated product defect in per-weapon range refresh. Unmounted Single delivers its complete attack, then native UnitAttack.OnTick changes Success to Interrupt during recovery; a blanket Success-only baseline assertion also misdescribes native behavior. Haste fixture currently adds a buff named Haste without an extra attack or attack bonus, so its native-effect gate remains red.

Working candidate `0.1.0-chunk1-preview.4` refreshes range from PlannedAttack.WeaponRange. Nineteen stable controls now cover native precombat adjacency, distant mixed-weapon rejection, exact post-completion native recovery, Haste components, rider-owned Move and carried mount movement. The RT full-plan smoke receives the same legal all-weapon fixture positioning; its plan/completion assertions remain intact. Release build/source 22/0 PASS; full offline checks running. Next: commit/package this candidate, capture its current-state suite, then guarded `ordinary-attack-controls-tb` at 900 seconds. Final exact-candidate native regression remains open. No native mode, attack list or cooldown is forced.

Roadmap (only Chunk 1 active): ordinary attacks; actor allocations; pair-aware activations; sustained combat/UX; persistence; remaining combat features; multiple pairs/profiles/release.

Local handoff: `C:\Dev\KingmakerMountedCombatLab\handoffs\Kingmaker_Mounted_Combat_Revised_Project_Plan.docx`. Record source/package/documentation identities separately in the milestone report.
