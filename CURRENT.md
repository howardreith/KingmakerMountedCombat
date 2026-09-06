# Chunk 1: Ordinary attack correctness

Status: **PASS — engineering qualification**. [Milestone report](docs/CHUNK1-ORDINARY-ATTACKS.md); [historical Phase 3H](docs/PHASE3H-IMPLEMENTATION.md).

- Branch `codex/mounted-combat-phase3f-playable-core`; intake/reviewed ancestor `1d2b8c3ccad14009653af9dc6420ee9af7b2e804` preserved.
- Final source `a8745640e18ce068e412b4e360c7b0a3d46c738a`, version `0.1.0-chunk1-preview.13`. Later documentation commits remain distinct from the packaged source.
- Package `KingmakerMountedCombat-0.1.0-chunk1-preview.13-actor-isolation-diagnostic.zip`, SHA-256 `62ddcff6f26e02c6abb6ce80e02a14276b856caa8911a72a5f9f764042173e74`; [full identities](docs/CHUNK1-ORDINARY-ATTACKS.md#exact-candidate).
- C01-C03 and applicable regression: 32 NATIVE INTEGRATION PASS/0 FAIL on this exact package. COMPONENT 345/0; ASSEMBLY CONTRACT 439/0; applicable build/protocol/package checks PASS. HUMAN PLAY and safe mod-absent A remain TODO.
- Actual intake installation remains Phase 3G preview 7. All 19 runtime transactions restored current saves, Mods, settings and caches; final audit `2026-09-06T19:20:29.3863897Z`. No permanent installation or release.

Finding: historical native Single arose from mismatched fixture pointer/prediction state; a separate product defect reused bow reach for a later bite. Correct native input and current-weapon range admission now pass matched B/C/D and native modifier/cost controls. Prior failures remain in the journal and iteration record.

Rules: one pair, existing Horse/Mammoth profiles, native UnitAttack sequences/costs, explicit Primary single. `EnableUnifiedMountedTurn=false`, `EnablePairedCommandScheduler=false`, `EnableDiagnosticOverlay=false`. Mount transport spends mount resources without rider Move tax or a new tabletop melee restriction. Preserve active movement accounting through UnifiedMountedTurnCoordinator.

Open gates: HUMAN PLAY/visual Horse Bite concern and safe mod-absent certification. Guarded branch publication is recorded in the campaign ACTIVE-RUN.json and final delivery. Next development dependency is Chunk 2 native actor allocations, not another scheduling shell. No Chunk 2 implementation is included. Next experiment under its mission: observe preparation/refresh before and after native callbacks across both actor orders, retaining expenditure through complete movement exhaustion/conversion.

Roadmap: ordinary attacks; actor allocations; pair-aware activations; sustained combat/UX; persistence; remaining combat features; multiple pairs/profiles/release. Only Chunk 1 execution was authorized here. Revised local plan: `C:/Dev/KingmakerMountedCombatLab/handoffs/Kingmaker_Mounted_Combat_Revised_Project_Plan.docx`.
