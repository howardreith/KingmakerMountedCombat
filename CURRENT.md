# Combined actor-allocation and paired-activation milestone

Status: **IN PROGRESS**. [Milestone report](docs/PAIRED-ACTIVATION-MILESTONE.md).

First gate PASS in both arrangements on preview.9 (I/J,96/0). R17 passes mount-first P01/A05/P03/P02 (50/0); T19 passes rider-first P01/A05/P03, then refuses native get-up input (49/2). Working20 preserves the native get-up exception and implements exact preparation-command ownership, actor-local condition forfeiture and shared finalization. New P05 exercises native DoNothing/SelfHarm and forced split. P04/P05, spent-Standard mode, remaining A01-A09 and final A05/A10 remain unqualified. All twenty A-T transactions restored actual human preview.13/current external state. This milestone is IN PROGRESS.

Intake is clean `codex/mounted-combat-phase3f-playable-core` at reviewed `45e3d276754257f4513342d5bce7626dd609d252`, containing Chunk 2 binary source `c804ba052760063f747cde83265e660916984d72`, development candidate `0.1.0-chunk2-preview.10`. Preserve all descendants. [Frozen Chunk 2 repairs, identities and outstanding gates](docs/CHUNK2-ACTOR-ALLOCATIONS.md).

Implement one rider-principal activation for the supported pair mounted before encounter, with distinct native actor costs, complete exactly-once preparation, authorized partner commands and correct completion/next-actor participation. This mission supersedes historical separate-turn/scheduler restrictions. First gate: three full paired activations in each pre-pair initiative arrangement, then remaining A01-A09 and final-path A05/A10. Prior A05/A10 evidence does not qualify the new path.

Actual owner preview.13 is the separate rollback/play baseline. [Accepted human play](docs/CHUNK1-HUMAN-PLAY.md) requires no repetition before coding. Its three experimental settings remain untouched. One pair, Horse/Mammoth, native full/Single/Primary attacks and CRPG transport policy remain. Guarded temporary testing, commits, branch publication and private packages are authorized; permanent installation, main merge, public release, persistence and content expansion are not.
