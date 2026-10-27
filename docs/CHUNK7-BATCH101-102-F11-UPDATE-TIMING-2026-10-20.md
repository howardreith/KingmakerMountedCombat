# Batches 101-102: F11 Update phase timing and pose-component reuse — IN PROGRESS

The standing Chunk 7 mission remains active. Batch101 added opt-in per-frame
timing for `world.UpdatePairs` and `unifiedTurn.Update` inside the existing
KMC Update measurement. Collector contract v3, the strict external reader and
its synthetic fixture passed focused checks (collector 67/0, admission 24/0,
reader 78/0). The revised reader also passed on the original immutable
batch73 v1 and batch80 v2 sidecars. Fast passed 3/0 and offline Candidate 7/0.
Independent review passed; its receipt is
`analysis-cache/chunk7-continuation/codex-takeover/f11-phase-review/review01.json`
(SHA-256 `f150b2ad106b6a0adf5867882dd5deeb626847f88246ab86772e86442e15ceb4`).
Source commit `8bfc49d16cd405107e61b5a62a2281e63b891d9b` was guarded-pushed.

Batch101 froze package
`KingmakerMountedCombat-0.1.0-chunk7b-preview.82-c7b-world113-diagnostic.zip`
(SHA-256 `746bfb0d8e559ad2ccbe60b6688aa6810065e194083b3b6c6c5503337106ff7e`,
DLL SHA-256 `78399e3019444ed31d250535565220d5ef0f8cf5ca80c0cbff6a9880dc62f007`,
MVID `20169927-080d-4eb5-955a-505c2fff3486`). Suite
`chunk7b-completion-b101-20261021` has SHA-256
`eb17b92ca9d827199481aac6164b1293c8904cf93a93590c5c6f8a9abfa04bef`.
The sole declared scenario, `20261021-c7b-world113-b101s1-f11-phase`, passed
game/outer 8/0. Twelve windows completed, with zero KMC log messages and
stable owner/subscription boundaries. The sidecar SHA-256 is
`3df6800696cc550d7ff6d32aee722d1b3ef138ae3cf934b3e85ab3c0009e920b`;
world artifact SHA-256 is
`ff5259933d2055a094de66126f76cd39533acb641008770b8ec60c85314087e1`.

The product then removed a concrete per-frame managed array in the mounted
invariant check: `riderView.GetComponents<MountedRiderPoseAdapter>()` now uses
the exact installed Unity `GetComponents<T>(List<T>)` overload with a reusable
one-element-capacity list, preserves the single-component and reference checks,
and clears the list even on failure. The installed CoreModule API probe passed;
independent review passed its extracted-helper 11/0 and source checks. Its
receipt is
`analysis-cache/chunk7-continuation/codex-takeover/pose-component-list-review/review01.json`
(SHA-256 `cdf55ebc93c2b56e5eccbb357302c0d03b0590255be26247cde49b40e31b276b`).
Fast passed 3/0 and offline Candidate 7/0. Source commit
`f3ff874eb97f8d7e97f03206dc812b2bbf76c421` was guarded-pushed.

Batch102 froze package
`KingmakerMountedCombat-0.1.0-chunk7b-preview.82-c7b-world114-diagnostic.zip`
(SHA-256 `b5ad2f0368f1025b7a5976ab231e2c85a35a2c78106c6cee537da1b85bd2c198`,
DLL SHA-256 `49e3b899e32bbe996b9449e86c9871cfac9ebeb934aa0339e774f4e169080b86`,
MVID `e88d61f0-f17a-45bc-8f8b-c0b04f537249`). Suite
`chunk7b-completion-b102-20261021` has SHA-256
`fa3260ff20bc9e1313fa7d4c16fe80e1a34f6a7d54a617614f38fa8ab493651e`.
The sole declared scenario, `20261021-c7b-world114-b102s1-f11-pose-list`,
passed game/outer 8/0 in about 126 seconds. Sidecar SHA-256 is
`054605ec70f5a424e06c19e9bc27b1991dda0923b95947caaeec0501bc596047`;
world artifact SHA-256 is
`3b5d42207a1f93ebc3ae5e5fdd53ac52f80ba47b0370041309403b9b9f803068`;
outer result SHA-256 is
`3d89c1e1fea8ab87b86ca13c3671cedc045de6a6008b9e2a804ff37dd79f57c7`.
The declaration and guarded scripts are under
`analysis-cache/chunk7-continuation/batch102-f11-pose-list/`. Suite and
development-scoped native WhatIf passed before live use.

| Settled state | Batch101 median KMC/Pair/Turn Update means (ms) | Batch102 median KMC/Pair/Turn Update means (ms) |
| --- | --- | --- |
| 0 pairs after cycle 1 | 0.204614 / 0.002166 / 0.003562 | 0.191283 / 0.002093 / 0.003105 |
| 1 pair during cycle 2 | 0.247132 / 0.048092 / 0.003257 | 0.240601 / 0.046014 / 0.003002 |
| 2 pairs during cycle 2 | 0.268838 / 0.061752 / 0.003627 | 0.263440 / 0.057948 / 0.003383 |
| 0 pairs after cycle 2 | 0.211667 / 0.002162 / 0.003387 | 0.207890 / 0.002258 / 0.003523 |

Each median is over three independent 120-frame windows after 30 warmup
frames. The batch101 final zero group contains one gen-0-GC-associated outlier;
batch102's final zero group contains a gen-0 event in its third window. The
active-pair increment is localized mainly to `world.UpdatePairs`, not the
unified-turn span. Batch102's active-pair means are lower, but the zero-state
baseline and runtime conditions also differ. These observations support the
specific allocation-site correction; they do not prove a causal timing gain or
measure allocated-byte/event rate. Both sidecars explicitly retain
`managedAllocationRateMeasured=false` and `performanceAccepted=false`.

Both guarded transactions ended `restored`: original Mods, protected saves and
Working restored, baseline immutable, write allowlist passed, exact Howie
profile bytes, no accepted cache changes, and no game/transaction lock left.
This is development evidence on two different DLLs. F11 allocation rate,
diagnostics-off, ordinary-player controls and final-package acceptance remain
open; F09, F10 and the readiness-gated private 7E mission also remain open.
