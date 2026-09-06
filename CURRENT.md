# Chunk 1: Ordinary attack correctness

Status: IN PROGRESS. The owner's 2026-09-06 revised plan and Chunk 1 mission govern this work. [Milestone evidence](docs/CHUNK1-ORDINARY-ATTACKS.md); [frozen Phase 3H history](docs/PHASE3H-IMPLEMENTATION.md).

- Branch: `codex/mounted-combat-phase3f-playable-core`.
- Clean intake and verified remote: `1d2b8c3ccad14009653af9dc6420ee9af7b2e804`.
- Historical candidate: `0.1.0-phase3h-preview.6`, packaging source `fb65cf826f5914fc29ab5035e6409685bd982c66`.
- Native-qualified candidate: `0.1.0-chunk1-preview.10`, source `e82ca0397ae3e8fe9429a8818a8df32492c43f16`, ZIP SHA-256 `0299c18b887885d17ad1e381df2a5ba06bc6f3c713ec734d90d9a2d9c057086c`.
- Package qualifier: `native-sight-controls`; later documentation commits preserve the exact source and binary identities above.
- Actual installed intake: `0.1.0-phase3g-preview.7`; installed DLL/cache SHA-256 `6bc01b982309125963d2355983b37391f244cfea314093fe4b008ad21a241ff3`. Installed Kingmaker 2.1.7b, UMM 0.28.2, legacy Harmony12, .NET Framework 4.7, C# 7.3 remain authoritative.

Rules: one existing pair/profile; ordinary native UnitAttack plan/effects/costs; explicit Primary single; unmounted behavior retained. Unified turn, paired scheduler and overlay stay false. Mount transport spends mount resources without a rider Move cost or a new tabletop melee restriction. No full allocator/scheduler, content, persistence, permanent install, main merge or release.

Causal findings: the historical Single-mode failure began in the fixture's ground-handler/pointer mismatch. Correct native pointer prediction admits a full plan. Separately, mounted mixed-weapon admission reused bow reach for a later bite; production now refreshes range from the current native PlannedAttack. No mode, attack list or cooldown is forced.

Latest native run `20260906-chunk1-sight-tb-A`: 19 focused PASS / 0 FAIL, all cross-control validation PASS; complete host21/0. Trace2,835 events/0 drops/0 errors. C01-C03 native plans/modifiers/costs, purity/continuity/stale conditions, own/carried movement and exact mixed-weapon rejection pass. Full actual intake restoration PASS at17:13:24 UTC; log `674d489aa05a675a8b15372fba2fd6c137ebccb30898194920b5146d497c5832`.

Open gates: preview13 focused unmounted controls, Mammoth and repeat exact-candidate C01-C03/RT/party regression; guarded publication. Preview10 RT9/0 and party1/0 (60 assertions) passed. The broad legacy RT fixture failed before its unmounted controls because forced criticals exhausted its target and its validator still assumes one attack per dispatch. Preserve that failure; reuse unchanged unmounted assertions through stable `unmounted-attack-controls-rt`. HUMAN PLAY pending: desktop native pipe unavailable. Mod-absent certification pending a safe native fixture; do not strip custom save references. Next: build/test/commit preview13 and package it, freeze a fresh snapshot, run focused unmounted controls. Record exact new identities in ACTIVE-RUN.json; keep HEAD fixed until all runtime runs finish. Full offline suite PASS/0: source22, COMPONENT345, visual23, inventory10, harness243, phase14/29, ordinary39, ASSEMBLY CONTRACT439. Actor allocations and pair-aware activations remain future chunks.

Roadmap (only Chunk 1 active): ordinary attacks; actor allocations; pair-aware activations; sustained combat/UX; persistence; remaining combat features; multiple pairs/profiles/release. Local plan: `C:\Dev\KingmakerMountedCombatLab\handoffs\Kingmaker_Mounted_Combat_Revised_Project_Plan.docx`.
