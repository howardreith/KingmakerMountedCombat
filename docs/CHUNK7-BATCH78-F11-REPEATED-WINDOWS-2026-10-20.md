# Batch78 F11 repeated resident-pool windows — IN PROGRESS

The standing Chunk 7 mission remains active. Preview.91 extends the existing
zero-write owner-seed exploration measurement from one to three separately
labeled 30-warmup/120-sample windows at each settled 0/1/2/0-pair state.
It adds no native command or resource operation. The exact 22-object pin,
native settled-custody guard, four lifecycle anchors, 30-second per-state and
300-second overall deadlines, and failed-observer-release custody remain.
The sidecar contract is v2 with twelve rows; the external reader still accepts
the immutable batch73 v1 four-row artifact on its original bytes.

Independent source review passed. The focused actual collector, extracted
same-state admission and external reader checks passed **67/0, 24/0 and 73/0**;
the reviewer independently replayed the immutable batch73 v1 artifact through
the current reader. Fast passed **3/0** and offline Candidate **7/0**. Source
commit `65df53533138cd211c3be1244c9d15ea4d2d285c` was guarded-pushed.
Preview.91 package SHA-256 is
`597c03a36a02cb24aa23945f16c7d2577e02823722b7cb9ddb18c06f44cfa6b0`,
manifest SHA-256
`73edbe1611a06f923dc61c89333bc06a01dcd3d34c53997799ac3df90e9eca35`,
DLL SHA-256 `c6a1fca2421b00abd9bb1cc6fe7949966741f81d12b43a55f78f5a88641917fd`,
MVID `49e08b83-62c6-450d-a4fd-7801acbae072`.

The fresh stable suite `chunk7b-completion-b78-20261020` has SHA-256
`f3ef743ac92450b668a8ad44d6be900fd582b0ef786e1c031d3cdf8d0d85dfee`.
Batch78 declared one native scenario under
`analysis-cache/chunk7-continuation/batch78-f11-repeated/`. The first WhatIf
was refused before launch for a 600-second outer budget; the selected route
requires 900 seconds for setup, worker and cleanup. Corrected WhatIf passed.

The only live scenario, `20261020-c7b-world91-b78s1-f11-repeated`, returned
**game/outer PASS 8/0**, with a complete and released twelve-window sidecar.
The [raw sidecar](../../../runtime-evidence/20261020-c7b-world91-b78s1-f11-repeated/chunk7-matched-pool-measurements.json)
has SHA-256 `3355ba79826a65bcdb2d115a87375bbc4d794678f971000310b6eb2d610b627b`;
the corresponding world artifact has SHA-256
`4d2691e76f12eb940bb99bf70978b21cffdc42b9581853a67361e9a6ae42ba2f`.
All twelve windows reported zero KMC log messages and the same four unique
owned native subscribers before and after. Mods and protected saves restored;
the profile receipt reports exact bytes and zero cache exceptions. The
transaction settled, and no game process remained.

| Settled pool | Active pairs | Three KMC Update means (ms) | Median mean (ms) | Median per-window p50 (ms) |
| --- | ---: | --- | ---: | ---: |
| After cycle 1 | 0 | 0.2248, 0.2326, 0.2354 | 0.2326 | 0.2200 |
| During cycle 2 | 1 | 0.2975, 0.2842, 0.2893 | 0.2893 | 0.2734 |
| During cycle 2 | 2 | 0.3035, 0.3027, 0.3075 | 0.3035 | 0.2886 |
| After cycle 2 | 0 | 0.2298, 0.2484, 1.2631 | 0.2484 | 0.2320 |

The final zero-pair window's mean includes one 122.5563 ms service sample;
its p50 was 0.2320 ms and p95 0.3303 ms. All three native GC collection
counters advanced in that window and retained heap occupancy dropped by
126,636,032 bytes. This associates the outlier with a collection window but
does not attribute it solely to KMC or establish allocation rate. Using the
two zero-state median means' midpoint (0.2405 ms), one- and two-pair medians
are about 0.0488 and 0.0630 ms higher. The increasing cost merits diagnosis
under the predeclared rule; this is not a performance acceptance verdict.

The sidecar correctly reports `managedAllocationRateMeasured=false`,
`fullyInertDiagnostics=false` and `performanceAccepted=false`. F11 still needs
a reviewed allocation-event instrument, diagnostics-off comparison, normal
player controls and final-package measurement. The conditional six-pair gate
also waits for an admitted native fixture; this batch does not exclude it.
