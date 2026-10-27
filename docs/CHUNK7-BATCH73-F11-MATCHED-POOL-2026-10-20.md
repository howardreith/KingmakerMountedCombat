# Batch73: current-candidate F11 matched-pool observation

The [standing Chunk 7 mission](../planning/CHUNK7-COMPLETION-MISSION.md)
remains **IN PROGRESS**. Batch73 declared two guarded native scenarios and ran
both under the approved Howie context, base-only staging and the immutable
`KMC_C7_NO_MODS_OWNER_SEED` master through independent disposable copies. The
one-slot original declaration and later slot-2 amendments are retained under
`analysis-cache/chunk7-continuation/batch73-f11-measurement/`. There was one
live executor at a time.

Preview.87 package SHA-256
`1aa0edfb13c3ee299c066ec1806ad6e80ee29f35a4ea910201e9dd1bf118929a`
and manifest SHA-256
`c9f0c73df2778892939104392e7170101c58ef64a3a0977fd48421010169a020`
bind documentation HEAD `6023ce0dbd8a6e5ad856ab39d15942db98fe8812`.
The rebuilt DLL SHA-256
`a78bc086492552b1723fd3713b9c5a50ac67c73232e0cc4f3be4e37cac51d77a`
and MVID `9e1b213e-7cd8-4448-9c4e-cca29ffd1881` are identical to
preview.86. The fresh stable external-state suite
`chunk7b-completion-b73-20261020` has SHA-256
`45835f16e5c620904be74388a4a318ab3a0daaecc30331959b5451becb5aae31`;
its WhatIf and actual admission passed. The initial preview.86 scenario WhatIf
refused before staging because that package manifest bound the preceding HEAD.
No native run or restoration claim is attached to that refusal.

Slot 1, `20261020-c7b-world87-b73s1-f11-pool`, used the native
owner-seed Manual producer. Its game and outer results **PASS 5/0** and it
wrote one genuine mounted Manual. The scenario intentionally sampled only
`unmounted-0` and `mounted-1`; it produced no matched-pool sidecar. The
original measurement purpose was therefore not met by slot 1. Its two
development windows measured KMC Update means 0.2193 and 0.2340 ms, and
zero KMC log messages. Outer result SHA-256
`9d2549920b2459cce66b22b5b36dc203cf563cd88b27ea2e3ca24341cd131022`.
The separately selected slot-2 exploration case answers the matched-pool
question. Its first WhatIf refused a Windows PowerShell UTF-8 BOM in a newly
generated selection before any transaction. The original file and a new
no-BOM equivalent are retained; the corrected WhatIf passed.

Slot 2, `20261020-c7b-world87-b73s2-f11-matched`, used the existing
zero-write `owner-seed-exploration` route and returned **game/outer PASS
8/0**. The [raw matched-pool artifact](../../../runtime-evidence/20261020-c7b-world87-b73s2-f11-matched/chunk7-matched-pool-measurements.json),
SHA-256 `a209a9570cafe3aad77650f298a53fa8cc0909a9c05484d94aea35ce0c246206`,
reports complete/released observation of four exact 30-warmup/120-sample
windows with retained native object pins, the same actor population within
each window, stable owner and subscription counts, and zero KMC messages.

| Settled pool | Active pairs | KMC Update mean / p95 (ms) | Whole-frame p95 (ms) |
| --- | ---: | ---: | ---: |
| After cycle 1 | 0 | 0.3416 / 0.4292 | 11.690 |
| During cycle 2 | 1 | 0.4237 / 0.5484 | 11.423 |
| During cycle 2 | 2 | 0.4575 / 0.5822 | 11.527 |
| After cycle 2 | 0 | 0.3440 / 0.4309 | 11.803 |

The two zero-pair means differ by 0.0024 ms. The 1- and 2-pair means exceed
their 0-pair midpoint by about 0.081 and 0.115 ms. These are observed
inclusive diagnostic costs, not an optimization verdict: each active-pair
state has only one window, and the observer/scenario work is inside the KMC
timer. The increasing per-pair delta exceeds this sequence's zero-pair
spread and merits the planned repeated-window and diagnostics-off comparison.
Process heap changes remain occupancy, not allocation throughput. The
artifact correctly says `managedAllocationRateMeasured=false`,
`fullyInertDiagnostics=false`, and `performanceAccepted=false`.

The [game result](../../../runtime-evidence/20261020-c7b-world87-b73s2-f11-matched/runtime-game-result.json)
is SHA-256 `6a97489f096fd82629e70a2df2bf1035aff4b43aa92cdfc0d11330c1dab8cd17`;
the [outer result](../../../runtime-evidence/20261020-c7b-world87-b73s2-f11-matched/runtime-result.json)
is SHA-256 `c5246b27868d4517932b0a3b3bf7a4719f9b6e6f577331c1c975ea1d12c06aea`.
Both transactions report Mods, Working, baseline and protected-save restoration.
Both profile receipts report exact bytes and zero accepted cache changes;
slot-2 profile receipt SHA-256
`9f72e8ff6697a34928c759cea07133e2f463f6e04ced56705678370b10a6d575`.
No game process or runtime lock remains. These are development measurements
on preview.87. The selected F11 repeated 0/1/2 windows, managed allocation
seam, diagnostics-off comparison, conditional six-pair stress and exact final
package qualification are still open.
