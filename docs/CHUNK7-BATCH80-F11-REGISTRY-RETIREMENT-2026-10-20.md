# Batch80 F11 idle registry retirement — IN PROGRESS

The standing Chunk 7 completion mission remains active. In the ordinary frame
path, `MountedWorldService.UpdatePairs` calls `ReleaseSettledAdmissions`, which
calls `MountedPairRegistry.RetireSettled` even when all owners are active or
reserved. Commit `7f18a59301b6d39d6259feea2e06c48cbd2ed719` makes that
case return after a direct state scan. When any owner is detached, the prior
snapshot traversal, per-owner failure isolation, and retry behavior remain
unchanged. This removes a specific idle-path roster/error allocation; it does
not establish a managed allocation rate or measured speedup.

Independent review found no blocker. The reviewer ran the existing ownership
checks **9/0** and five extra in-memory active, retained, recursive and retry
checks **5/0**. Source SHA-256 was
`2b2fc706053fda3f85b027a15436ec80adf7c881346e4bd677c41b557c219579`.
Fast passed **3/0** (component 846/0, source validation 147/0); offline
Candidate passed **7/0** including the Kingmaker assembly check (0.49 and
3.69 minutes respectively). The first
Fast invocation incorrectly supplied `-AssemblyTarget` and was refused before
testing. The first native WhatIf incorrectly supplied manual-review-only
artifact pins and was refused before transaction. Neither refusal launched the
game or changed the live environment.

Preview.93 package is
`C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.1.0-chunk7b-preview.82-preview.93-diagnostic.zip`,
SHA-256 `eaf451b8acc890cc50c424f92593577360ba672e3e050af681df7ff169988ada`;
manifest SHA-256 `9e2f39f7579833507d3f0d235ac6bcc3495f2c7db22d018347c0337718a94914`;
DLL SHA-256 `cef162311511d5d8a4d1d8dba27192e090aaa71096b99d44063200311ec65c02`,
MVID `ce8963af-e76a-43e8-8bdd-596df082b81b`. The fresh append-only suite
`chunk7b-completion-b80-20261020` has SHA-256
`5103556dcea9e9650e5cf4b7e8ba51ed2f5d7711dbffcf4117ca8131202bfad7`.
The one-scenario declaration and unchanged owner-seed selection are in
`analysis-cache/chunk7-continuation/batch80-f11-registry-retirement/`.
Suite WhatIf/creation and the corrected development-scoped native WhatIf
passed. The latter is a scoped audit, not whole-lab purity proof.

The sole native scenario,
`20261020-c7b-world93-b80s1-f11-registry-retirement`, passed game/outer
**8/0** in 126.9 seconds from runner start to restored result. Its twelve-window sidecar SHA-256 is
`138cc4b4ad4a6cf5b8627d08a20fd7a14eef90e36a8ebe762a2c2bb7f176b2a2`;
world artifact SHA-256 is
`8eb3da9d558709a80da513ffdc1f3c8bd7814fb6d2d4511da2efc63ebb368ae8`.
All windows completed, recorded zero KMC messages and four unique owned
subscribers. The first zero-pair window had a gen-0 collection and a large
mean outlier; no other window recorded an in-window gen-0 collection.

| Settled pool | Active pairs | Three KMC Update means (ms) | Median mean (ms) |
| --- | ---: | --- | ---: |
| After cycle 1 | 0 | 1.2536, 0.2002, 0.1978 | 0.2002 |
| During cycle 2 | 1 | 0.2496, 0.2438, 0.2387 | 0.2438 |
| During cycle 2 | 2 | 0.2556, 0.2614, 0.2588 | 0.2588 |
| After cycle 2 | 0 | 0.1993, 0.2004, 0.1960 | 0.1993 |

The midpoint of the two zero-state median means is about 0.1998 ms; the one-
and two-pair medians exceed it by about 0.0440 and 0.0590 ms. Active-pair cost
still exceeds the observed zero-state spread. Batch79 used a different DLL,
time window and GC pattern, so the changed means cannot be attributed solely
to this correction. Allocation-event rate, diagnostics-off behavior,
normal-player controls and final-package acceptance remain open. The sidecar
continues to report `managedAllocationRateMeasured=false`,
`fullyInertDiagnostics=false`, and `performanceAccepted=false`.

The guarded transaction reports `phase=restored`, original Mods and protected
save inventory digests restored, zero restoration errors, and exact Howie
profile bytes with no accepted cache changes. The game process and transaction
lock are absent. No merge, release, tag or permanent installation occurred.
