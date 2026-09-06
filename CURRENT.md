# Chunk 1: Ordinary attack correctness

Status: IN PROGRESS. The owner's 2026-09-06 revised plan and Chunk 1 mission govern this work. [Milestone evidence](docs/CHUNK1-ORDINARY-ATTACKS.md); [frozen Phase 3H history](docs/PHASE3H-IMPLEMENTATION.md).

- Branch: `codex/mounted-combat-phase3f-playable-core`.
- Clean intake and verified remote: `1d2b8c3ccad14009653af9dc6420ee9af7b2e804`.
- Historical candidate: `0.1.0-phase3h-preview.6`, packaging source `fb65cf826f5914fc29ab5035e6409685bd982c66`.
- Latest native candidate: preview.9, source `520387948816003648cdb64df2d1dc5f8b133e7e`, ZIP SHA-256 `8b5ea93b746f0b54083bff4a1d79970a9d5ac0919264b528b7c22b1a77dff325`.
- Working preview.10 adds native sight checks to distant fixture placement and guards uninitialized native plan access. Source/package identity follows the next commit.
- Actual installed intake: `0.1.0-phase3g-preview.7`; installed DLL/cache SHA-256 `6bc01b982309125963d2355983b37391f244cfea314093fe4b008ad21a241ff3`. Installed Kingmaker 2.1.7b, UMM 0.28.2, legacy Harmony12, .NET Framework 4.7, C# 7.3 remain authoritative.

Rules: one existing pair/profile; ordinary native UnitAttack plan/effects/costs; explicit Primary single; unmounted behavior retained. Unified turn, paired scheduler and overlay stay false. Mount transport spends mount resources without a rider Move cost or a new tabletop melee restriction. No full allocator/scheduler, content, persistence, permanent install, main merge or release.

Causal findings: the historical Single-mode failure began in the fixture's ground-handler/pointer mismatch. Correct native pointer prediction admits a full plan. Separately, mounted mixed-weapon admission reused bow reach for a later bite; production now refreshes range from the current native PlannedAttack. No mode, attack list or cooldown is forced.

Latest native run `20260906-chunk1-entry-tb-A`: 18 PASS / 1 FAIL, 2,778 intact events. All stationary cases and both movement ownership controls pass; carried movement preserves the rider full plan. The mounted distant target fails native sight before attack, so its range regression remains red. Preview.10 requires clear native sight in target setup. Full intake restoration PASS at16:51:34 UTC; log `cca7c2156dc445b53702aa7afa22eeea282571be5644535fb87046897f61ab83`.

Open gates: final exact-candidate native mixed-range and all cross-control qualification; RT ordinary/Primary/approach/pause, party selection, unmounted and Mammoth regression. HUMAN PLAY pending: desktop native pipe unavailable. Mod-absent certification pending a safe native fixture; do not strip custom save references. Next: commit/package preview.10, snapshot `20260906-chunk1-sight-suite10`, guarded `ordinary-attack-controls-tb` run `20260906-chunk1-sight-tb-A` at900s. Full suite PASS/0: source22, COMPONENT345, visual23, inventory10, harness243, phase14/29, ordinary39, ASSEMBLY CONTRACT439. General actor allocation and pair-aware activation remain later chunks.

Roadmap (only Chunk 1 active): ordinary attacks; actor allocations; pair-aware activations; sustained combat/UX; persistence; remaining combat features; multiple pairs/profiles/release. Local plan: `C:\Dev\KingmakerMountedCombatLab\handoffs\Kingmaker_Mounted_Combat_Revised_Project_Plan.docx`.
