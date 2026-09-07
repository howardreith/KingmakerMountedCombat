# Chunk 2: Actor allocations and movement conservation

Status: **IN PROGRESS**. [Milestone report](docs/CHUNK2-ACTOR-ALLOCATIONS.md).

- Branch `codex/mounted-combat-phase3f-playable-core`; clean intake `aa0bdc41110923a0aae3bd1ac49322e5f2b75c02`, descendant of reviewed `b3f063337644215312de97d9736892212777ac1c`.
- Accepted Chunk 1 source `a8745640e18ce068e412b4e360c7b0a3d46c738a`, version `0.1.0-chunk1-preview.13`. [Frozen engineering qualification and package identities](docs/CHUNK1-ORDINARY-ATTACKS.md#exact-candidate): 32 NATIVE INTEGRATION PASS / 0 FAIL; COMPONENT 345/0; ASSEMBLY CONTRACT 439/0.
- **OWNER-REPORTED HUMAN PLAY:** preview.13 installed/enabled; TB ordinary attacks including full bow work well; Horse approach/attack works and the tested rider-to-Horse switch did not grant another move; hovering spends nothing; RT combat, controls, mounting and dismounting work well. This does not qualify exhaustive allocations, precise Rapid Shot modifiers, visible Bite recovery, formal Mounted Charge or cold load. [Installation record](docs/CHUNK1-HUMAN-PLAY.md).
- Actual intake is preview.13. Fresh transaction snapshots protect current saves/settings/caches and foreign Mods. Historical preview.7 restoration evidence remains historical.

Active gate: actor-owned movement/action conservation, both initiative orders and three native rounds, preparation callbacks, lifecycle/mode boundaries, A01-A09 plus exact-candidate Chunk 1 regression A10. Start with a compact causal native trace; retain working behavior where evidence supports it.

Separate native turns, one pair, Horse/Mammoth, native attack sequences/costs and explicit Primary remain. `EnableUnifiedMountedTurn=false`, `EnablePairedCommandScheduler=false`, `EnableDiagnosticOverlay=false`. Mount transport spends mount resources without a rider Move tax or new tabletop melee restriction. Preserve active accounting through UnifiedMountedTurnCoordinator.

Temporary guarded runtime validation, private packaging, coherent commits and guarded branch publication are authorized. No permanent installation, main merge or public release. Coordinated activations are Chunk 3; content and persistence remain later milestones.
