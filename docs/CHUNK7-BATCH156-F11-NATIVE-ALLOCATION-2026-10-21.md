# Batch156: private F11 native allocation measurement

The [standing Chunk 7 mission](../planning/CHUNK7-COMPLETION-MISSION.md)
remains **IN PROGRESS**. One declared guarded native scenario ran under the
approved Howie context, base-only staging and a fresh disposable copy of the
qualified `KMC_C7_NO_MODS_OWNER_SEED`. The selection and declaration are in
`analysis-cache/chunk7-continuation/batch156-f11-native/`; no other native
scenario was hidden in the wrapper.

Reviewed and pushed product/observer integration commit
`e1f500cebea7ec3a266e23647b3ce775adfe62db` passed Candidate **7/0**.
The exact private package is
`artifacts/KingmakerMountedCombat-0.1.0-chunk7b-preview.94-c7d-f11-native-b156-diagnostic.zip`,
SHA-256 `973b87129521af516fbe88a0d22f940dc4a43e593ff01a8bd1faaa47b736f7a6`;
its DLL SHA-256 is `160225b233efe46afc0d1308b7088eef46c193bb10334ad4582fe1f771df8f70`,
MVID `17dd91fb-26b0-44bb-ba75-005bc0619e50`. The append-only suite
`chunk7b-completion-b156-f11-20261021` has SHA-256
`702e059aa235e9ca6a87a37fcfb8a0ebeac30c3c0bf3a58540d947d77f2dc2d4`.
Its own WhatIf and the scenario's development-scoped guarded WhatIf passed
before the native launch.

Run `20261021-c7d-world156-b156s1-f11-native` loaded one disposable source,
made zero save requests, and returned **game PASS 8/0**. The positive
in-game callback handshake counted one managed allocation before sampling.
All twelve declared matched windows completed, with 30 warmup and 120 measured
Updates each. Every window closed on its owning thread with zero overflow,
reentrancy and active residue. Four foreign-thread callbacks were observed in
one two-pair window and excluded from its same-thread byte total. The raw
matched artifact SHA-256 is
`4f6c00272705efdefc9721a0b495ef93b45bf1a5976bf972c2d23f6d77911c08`;
the original game result SHA-256 is
`d493646d5b36c6f71f43431593f2afe72efafc18c9855ada51c8afa5bac9daa8`.

| Settled state | Median Mono managed object bytes/s on KMC Update thread | Median inclusive KMC Update mean, ms |
| --- | ---: | ---: |
| 0 pairs after cycle 1 | 2,301,293 | 0.2258 |
| 1 pair during cycle 2 | 2,409,475 | 0.2691 |
| 2 pairs during cycle 2 | 2,539,969 | 0.2881 |
| 0 pairs after cycle 2 | 2,300,245 | 0.2179 |

Each state has three windows. The second two-pair window had a transient
1.3121 ms mean and a lower time-normalized rate, while its raw object-byte
count matched the other two-pair windows. The median is reported with that
outlier retained, not deleted. KMC emitted zero messages in all twelve
windows and the matched ownership/subscription checks passed. These values
include observer overhead and all managed allocations on the Update OS thread
within `CompositionRoot.UpdateCore`, not just KMC-owned objects. Unity/native
allocations and other threads are excluded. The artifact truthfully reports
`fullyInertDiagnostics=false` and `performanceAccepted=false`; normal-player
diagnostics-off comparison and exact final-package qualification remain open.

The **original outer result is FAIL**, SHA-256
`105bdf775d97830db2ff46891c1c9927e28bfe99d7e8890cc6a84ef900fa672d`.
Its reader summed serialized `Single` frame deltas as JSON `Double`; row 8's
derived rate differed from the product by 0.0161 bytes/s, just above the
existing 0.01 tolerance. The reviewed one-line reader correction reconstructs
each `Single` before the product's `Double` summation. Neither tolerance nor
the preservation assertion changed. Focused matched-pool checks pass **88/0**,
including an explicit float-roundtrip case; the full game-result validator
passes read-only on the unchanged evidence. The separate reader-only
re-evaluation **PASS** is pinned by
`analysis-cache/chunk7-continuation/batch156-f11-native/reader-reevaluation.json`,
SHA-256 `d9a2bf749b0e4371fe09827865be92f399bd9c48b7134ebdec5965a0492acd03`.
It does not rewrite the original outer FAIL or claim a second native execution.

The combined transaction reached `restored` with Mods, Working, baseline,
protected-save and write-allowlist checks true; the game process exited and
the runtime lock is absent. The profile has **26 accepted native cache
changes**, so profile-byte equality is false. No recovery script ran. F09
conditional six-pair stress and ordinary doors, F10 broader progression,
F11 request-free normal-player controls and the readiness-gated private 7E
rehearsal remain open. No merge, release, permanent installation or HUMAN
PLAY acceptance is selected.
