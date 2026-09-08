# Combined actor-allocation and paired-activation milestone

Status: **IN PROGRESS**. [Milestone report](docs/PAIRED-ACTIVATION-MILESTONE.md).

First gate PASS on preview9 in both arrangements. AM34 Mammoth TB strict/native PASS1/0,66 assertions; actual preview13 restoration PASS. AN34 ordinary C01-B/C pass, then C01-D exposes missing re-arming after native actor removal: a still-mounted pair gets separate turns in its next encounter. Working35 re-arms outside combat without preparing or writing resources, and adds native encounter ownership checks. Build/source22/0, components364/0, patches30/0, serializer3/0, ordinary protocol39/0 pass. All40 A-AN transactions restored actual human state. Next: commit/package35 and focused ordinary retry, then remaining A10, final Mammoth and all4 final A05. Milestone remains IN PROGRESS.

Intake is clean `codex/mounted-combat-phase3f-playable-core` at reviewed `45e3d276754257f4513342d5bce7626dd609d252`, containing Chunk 2 binary source `c804ba052760063f747cde83265e660916984d72`, development candidate `0.1.0-chunk2-preview.10`. Preserve all descendants. [Frozen Chunk 2 repairs, identities and outstanding gates](docs/CHUNK2-ACTOR-ALLOCATIONS.md).

Implement one rider-principal activation for the supported pair mounted before encounter, with distinct native actor costs, complete exactly-once preparation, authorized partner commands and correct completion/next-actor participation. This mission supersedes historical separate-turn/scheduler restrictions. First gate: three full paired activations in each pre-pair initiative arrangement, then remaining A01-A09 and final-path A05/A10. Prior A05/A10 evidence does not qualify the new path.

Actual owner preview.13 is the separate rollback/play baseline. [Accepted human play](docs/CHUNK1-HUMAN-PLAY.md) requires no repetition before coding. Its three experimental settings remain untouched. One pair, Horse/Mammoth, native full/Single/Primary attacks and CRPG transport policy remain. Guarded temporary testing, commits, branch publication and private packages are authorized; permanent installation, main merge, public release, persistence and content expansion are not.
