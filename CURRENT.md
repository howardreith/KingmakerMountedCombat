# Active Chunk 4 - native movement observation, 2026-09-20 UTC

IN PROGRESS. Published source41 `4115ec6ea8149e48b48952b21cca3f44df3a3257` passes CE obstruction47/0. CF Charge RT rejects mounted Charge safely but fails its legal recovery movement assertion: native CompleteMovement interrupts at .459m from the destination (arrival radius .3m), after2.429463m actual travel with rider Move0. No later Charge controls run. All83 native transactions restored actual human pairedpreview37 and protected data.

Candidate `0.1.0-chunk4-preview.42` adds read-only path-end, native radius/position and destination-occupancy observations. It does not accept Interrupt as Success or change gameplay/fixture selection. Full local checks pass; the cause remains unresolved pending focused RT evidence. Package/publish/fresh suite42, then CG RT Charge with added observations before any broad campaign. [Report](docs/CHUNK4-PLAYABILITY.md), [resume](AUTONOMOUS-RESUME.md). No permanent installation or Chunk5 work.

---
# Combined actor-allocation and paired-activation milestone

Status: **PASS for the supported pre-combat pair**. [Implementation, evidence, limits and private checklist](docs/PAIRED-ACTIVATION-MILESTONE.md).

Final `0.1.0-paired-preview.37`, tested source `ec5d44e6eddc9839d273176b345f7c9701520450`: three paired activations plus refresh in both initiative arrangements; supported A01-A09; final A05 with 36 matched preparation samples; accepted A10 with 32 cases / 287 assertions; Mammoth TB with 66 assertions. All ten final native runs and their outer validators PASS. Full final checks PASS. Configuration: paired activation true; both legacy authorities and diagnostic overlay false.

The private package remains the exact tested ZIP, SHA256 `2215156d43679913ee134c563f704cee37ce564ab70dc50d7009781250ee3738`; DLL `20080fdcf83c7628611c3f6354a3e47c3065e2b25998a9c01b39a69796ed57bb`. This documentation checkpoint does not rebuild the payload. Work remains on `codex/mounted-combat-phase3f-playable-core`, preserving reviewed `45e3d276754257f4513342d5bce7626dd609d252`, Chunk 2 source `c804ba052760063f747cde83265e660916984d72` and legitimate descendants. [Frozen Chunk 2 evidence](docs/CHUNK2-ACTOR-ALLOCATIONS.md).

All 56 campaign transactions independently restored their actual preview.13 intake, protected saves and full Mods tree; the last audit was at 2026-09-08 12:12 UTC. Later read-only reconciliation finds preview.37 already installed by the separately recorded 18:29 UTC deployment, with the exact preview.13 DLL/cache retained in its backup. Current saves and several foreign-mod settings also differ from the campaign intake; preserve this newer external state. UMM parameter bytes still match, and no game or transaction lock remains. This reconciliation performs no runtime transaction or installation. [Accepted preview.13 human play](docs/CHUNK1-HUMAN-PLAY.md) retains its scope.

One pair, Horse/Mammoth, mounted before combat; rider initiative is principal and native actor costs remain separate. Mounted save restoration and cold-load debt rebinding remain later work. Cross-round Delay is visibly rejected; other unqualified transitions and new-candidate human review remain explicit in the report. These limits do not qualify a complete mounted-combat feature.
