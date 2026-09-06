# Chunk 1: Ordinary attack correctness

Status: IN PROGRESS. The owner's 2026-09-06 revised plan and Chunk 1 mission govern this work. [Milestone evidence](docs/CHUNK1-ORDINARY-ATTACKS.md); [frozen Phase 3H history](docs/PHASE3H-IMPLEMENTATION.md).

- Branch: `codex/mounted-combat-phase3f-playable-core`.
- Clean intake and verified remote: `1d2b8c3ccad14009653af9dc6420ee9af7b2e804`.
- Historical candidate: `0.1.0-phase3h-preview.6`, packaging source `fb65cf826f5914fc29ab5035e6409685bd982c66`.
- Latest native candidate: preview.8, source `78a04fa3297905c0e37a54dd0d3d82fa57a274a7`, ZIP SHA-256 `f044e16d340a6051c0a7fc8e49217e61ad87eb222840eb6de702a21d13357482`.
- Working preview.9 changes only disposable movement fixture entry and endpoint observations. Source/package identity follows the next commit.
- Actual installed intake: `0.1.0-phase3g-preview.7`; installed DLL/cache SHA-256 `6bc01b982309125963d2355983b37391f244cfea314093fe4b008ad21a241ff3`. Installed Kingmaker 2.1.7b, UMM 0.28.2, legacy Harmony12, .NET Framework 4.7, C# 7.3 remain authoritative.

Rules: one existing pair/profile; ordinary native UnitAttack plan/effects/costs; explicit Primary single; unmounted behavior retained. Unified turn, paired scheduler and overlay stay false. Mount transport spends mount resources without a rider Move cost or a new tabletop melee restriction. No full allocator/scheduler, content, persistence, permanent install, main merge or release.

Causal findings: the historical Single-mode failure began in the fixture's ground-handler/pointer mismatch. Correct native pointer prediction admits a full plan. Separately, mounted mixed-weapon admission reused bow reach for a later bite; production now refreshes range from the current native PlannedAttack. No mode, attack list or cooldown is forced.

Latest native run `20260906-chunk1-clear-tb-A`: 15 PASS / 1 FAIL, with 2,244 intact trace events. C01 B/C/D, Rapid-off, BAB, Haste, stale restriction, native Single and spent Standard controls pass. The failure occurs before measured movement: the fixture could not find a distant endpoint within its already-adjacent melee ring. Preview.9 starts movement cases farther away and enters legal adjacency using native ground input, retaining displacement, ownership, same-turn and attack assertions. Full actual intake restoration PASS at 16:35:06 UTC; immutable log SHA-256 `67ee992aa2917a850a61bbb29e4ea2986ecd9834b107517d6dce6cd88e021749`.

Open gates: remaining rider/carried movement and mixed-range controls; all cross-control qualification gates; final exact-candidate RT ordinary/Primary/approach/pause, party selection, unmounted and Mammoth regression. HUMAN PLAY pending because the desktop native pipe is unavailable. Mod-absent certification pending a safe native fixture; do not strip custom save references. Next: commit/package preview.9, snapshot `20260906-chunk1-entry-suite9`, guarded `ordinary-attack-controls-tb` run `20260906-chunk1-entry-tb-A` at 900 seconds. Build/source 22/0, COMPONENT 345/0 and ordinary protocol 39/0 PASS.

Roadmap (only Chunk 1 active): ordinary attacks; actor allocations; pair-aware activations; sustained combat/UX; persistence; remaining combat features; multiple pairs/profiles/release. Local plan: `C:\Dev\KingmakerMountedCombatLab\handoffs\Kingmaker_Mounted_Combat_Revised_Project_Plan.docx`.
