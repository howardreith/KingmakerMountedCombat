# Batch79 F11 frame-snapshot correction — IN PROGRESS

The standing Chunk 7 mission remains active. Read-only inspection of the
`CompositionRoot.UpdateCore` path found two repeated `MountedWorldService.Pairs`
array copies in `UpdatePairs` and `ReleaseSettledAdmissions`, plus an error-list
allocation in each successful `Visit`. Reviewed commit `6106d22f` caches the
published pair roster for traversal, invalidates it after either pair
publication, and creates a `Visit` error list only after a local failure.
The public `Pairs` getter still gives callers their own copy. The reviewer
checked in-callback publication, healthy-pair continuation after a failure,
next-traversal visibility, and external-copy isolation. The change removes
these specific warmed-frame allocations; it does **not** prove a zero-allocation
frame or quantify total managed allocation rate.

The actual-DLL focused contract passed **8/0**, with four independent reviewer
checks. The neighboring observation-lifetime adapter passed **71/0**; offline
Fast passed **3/0**, Candidate **7/0**, and `git diff --check` passed. Two
command-selection refusals preceded those checks: Fast does not accept
`-AssemblyTarget`, and Candidate `-Focused` accepts only scripts directly under
`scripts`. Neither refusal opened a game transaction. The source commit was
guarded-pushed before package creation.

Preview.92 diagnostic package is
`KingmakerMountedCombat-0.1.0-chunk7b-preview.82-preview.92-diagnostic-diagnostic.zip`,
SHA-256 `f6dea93fe765e29ba1f4880c605b5e4ff31e7c7a9273b0b43b9af1775ca197cc`;
manifest SHA-256 `a210f1abdab8ea605f7d677835e062d0442c188c285f1d2a0292943ed5ba779d`;
DLL SHA-256 `e7ad74f9d20f27bc236912637b5240682c92eee540aa0ec9b423774dd8167ab2`,
MVID `35b26d19-29e8-4965-aa9a-e4df8674b0c6`. The doubled `diagnostic`
word is only the package qualifier passed to a packager that already appends
that word; the frozen artifact is unambiguous. Suite
`chunk7b-completion-b79-20261020` has SHA-256
`028a405cf72eae0005121a5d597572fcc29fbfa81cb07410f9eb92daa196d8c8`.
The one-scenario declaration and copied unchanged owner-seed selection are in
`analysis-cache/chunk7-continuation/batch79-f11-frame-snapshot/`.

The suite WhatIf and creation passed; the development-scoped native WhatIf
passed without a transaction. The sole live scenario,
`20261020-c7b-world92-b79s1-f11-frame-snapshot`, returned **game/outer PASS
8/0**. Its [raw twelve-window sidecar](../../../runtime-evidence/20261020-c7b-world92-b79s1-f11-frame-snapshot/chunk7-matched-pool-measurements.json)
has SHA-256 `dc9e0c9e14d75732264e35b6aca7d7bf2c3765f318e824719e23dec1dd374fb4`;
the world artifact has SHA-256
`815e5aabf42df5deeab43817e3291b92e0d768771c5aae4999ac7a0b4a7ba67a`.
All twelve windows completed and released, recorded zero KMC log messages,
and held four unique owned subscribers. None recorded an in-window GC-count
increment. The transaction reports `phase=restored`, original Mods and
protected-save digests restored, exact Howie-profile bytes, no accepted cache
changes, no game process and no active transaction lock.

| Settled pool | Active pairs | Three KMC Update means (ms) | Median mean (ms) |
| --- | ---: | --- | ---: |
| After cycle 1 | 0 | 0.2290, 0.2525, 0.2464 | 0.2464 |
| During cycle 2 | 1 | 0.2943, 0.2948, 0.2825 | 0.2943 |
| During cycle 2 | 2 | 0.2944, 0.3210, 0.2987 | 0.2987 |
| After cycle 2 | 0 | 0.2257, 0.2370, 0.2363 | 0.2363 |

The midpoint of the two zero-state median means is 0.2414 ms. The one- and
two-pair medians exceed it by about 0.0529 and 0.0573 ms. This remains above
the observed zero-state window spread and keeps the predeclared per-pair cost
investigation open. Batch78's different candidate, elapsed order and GC
outlier prevent attributing the changed timings to this correction. The sidecar
still truthfully says `managedAllocationRateMeasured=false`,
`fullyInertDiagnostics=false`, `performanceAccepted=false`. A supported
allocation-event counter, diagnostics-off and ordinary-player comparisons, and
final-package measurement remain required before F11 acceptance.
