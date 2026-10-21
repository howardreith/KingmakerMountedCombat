# Batch110 — native Small admission after bounded comparison change

The reviewed batch109 change at pushed `372d68674ac1b4eb3500a1fa9b6943042a22374b`
passed offline Candidate **7/0** in 3.8 minutes. It was packaged once as
`KingmakerMountedCombat-0.1.0-chunk7b-preview.82-c7b-world116-diagnostic-diagnostic.zip`:
package SHA-256 `ba90d8759541687767da40fbaf6e7c95d22c3400f6e46611375d662d7bb15b0f`,
DLL SHA-256 `0177a68002f7ed3d63f3f120ffe27103ce71f9b377e63c88f24a71ef5212b90b`,
MVID `feccdb94-9a68-434d-b0aa-12c6f73533e3`. The append-only suite
`chunk7b-completion-b110-20261021`, SHA-256
`505de6fcdde936f72cc5f75a849de3916ce3927f2c67d7808b805633521fdef0`,
passed WhatIf and creation against the current external state.

One scenario was declared before launch in
`analysis-cache/chunk7-continuation/batch110-small-admission/declared-b110.json`.
Its development-scoped WhatIf passed. The guarded run
`20261021-c7b-world116-b110s1-small-admission` passed in the game and outer
reader **5/0**. It cold-loaded the already-qualified unmounted F09 Manual,
mounted and saved the declared Small Halfling/Horse pair while preserving its
neighboring Medium Horse owner, and produced the genuine
`Manual_305_KMC_C7_SMALL_WORLD.zks` with SHA-256
`1bec0635fdac21ad35795a9f73ebf84ca251475663673119a98c79e63ba919d4`.
The native evidence artifact SHA-256 is
`f8080a6ccdc5e7989f78bc9513364e5238135e0ca0ec091bb06cefb39ca205d9`.
This verifies the changed admission path on these exact product/fixture bytes;
it is not an allocated-byte/event-rate or causal speedup measurement.

The outer result and orchestration both report PASS. Mods and Working were
restored, Baseline stayed immutable, protected-save checks passed, and the
restored save-inventory digest was
`df434471b6f2bb3ade60ecccaa524aa7537e17f63db3d0bcce6d5a7eb70561ab`.
`profile-cache-changes.json` reports exact profile-byte restoration and zero
accepted native cache changes. No game process or runtime lock remained.
Earlier batch108 results remain evidence for world115 only. F11 allocation-rate,
native diagnostics-off/ordinary-player controls and final-package reuse remain
open, along with F09/F10 breadth and the selected private 7E rehearsal.
