# Chunk 7 batch50: native Small field reached; stale final money assertion

Preview.68 at `ce5298412694d703bce16b40bd4ae34e9630e6e6` was
guarded-pushed, then packaged once. Fast passed 3/0 and Candidate passed 6/0
(3.66 minutes). Package SHA-256
`fab54ae0955e8408a6d800a419f8920230a61a32590cb8ed417dcc5c248e9215`,
manifest `f0c7a549ab229f5ce84f60154ea2e192f374c08e4d7d8d90d47ad1ad1f64e635`,
DLL `e87d3a8e4cf425a820a9cdda87f8b42122f673393fd24e83c3bb4ebe7f641e2b`,
MVID `e00673e0-875e-4c62-8e51-f865be9321a6`. The fresh suite
`chunk7b-completion-b50-20261020.json` SHA-256
`b4fb99ffff9c3547280ea3117fc6d479ca440f5eae6d85b9fec43ea9d290267b`
kept save digest `7017207d543f81d0ddf7c15562a855f43492c7ab6da4c91ab34d1cf18ced69e9`
and Mods digest `e81a49d813bcff8cdb70fb6e9f007234fd6ce5674758873d9e31e828ece2e21b`.
The private one-slot declaration `group68-native-b50/declared-b50.json` SHA-256
`9990afaaf509be09f158a6ae06826b37eede21ea8c478970e4f76ca323fc53f1`
preceded the native run. Suite and corrected scoped scenario WhatIf passed;
an initial WhatIf invocation with obsolete individual save pins refused before
launch and was replaced by the required three suite pins.

The single native slot `20261020-c7b-world68-b50s1-small` remains **FAIL**.
Its outer result SHA-256 is
`968964b82d04915c2cc8cd1d9bef1a1292146d5bd1d928bdb0a4cd3d91ac8cc2`;
game result SHA-256 is
`4e5661a67fdf5cd8a9b02cd80c2c34d92a697f8807f8da73be8631e28ab409b6`;
Small evidence SHA-256 is
`060d70e03ebfb6fc769b0587ea3fb0c942b9d786a997836130983c8e4fc992b6`.
The game rows were source PASS, native load PASS, native choices PASS,
runtime FAIL and cleanup PASS (4/1). No Manual or cold consumer exists.

The batch49 changed-view boundary was passed: native main commit 1 and paid
hire commits 4 completed, and the exact paid actor moved from the remote
roster into the native five-character field. The settled world contains the
original four IDs, that paid Halfling, and both reciprocal Horses: exactly
seven actors. The first failure was the final fixture condition in
`CaptureUnmountedFixture`. It required `initialMoney - nativeHireCost`
although the previously verified stage-3 check and retained native evidence
show `400057 + 411 - 8000 = 392468`. The `411` was the native class starting
gold, not a KMC grant. The original FAIL is a stale final assertion; it does
not certify a Small Manual, mounted pose, or save/load.

The guarded transaction reported Mods and saves restored, Baseline immutable,
Working restored, save allowlist PASS and transaction phases `restored`.
Read-only check found no Kingmaker process or runtime lock. The guarded
profile policy admitted only its recorded cache changes; profile byte
equality is **not** claimed:
19 changes were accepted under the existing native cache policy (three
achievement-cache rewrites and sixteen Unity analytics leaves). Profile
cache receipt SHA-256 is
`6fb84ca56afa15b7d1dd29e10172d3dd8fa0157f69a364dbc9349f611ec52c9b`.
No manual recovery was needed.

The next candidate corrects the final paid-balance assertion while preserving
the exact roster, rig, Horse, XP, preparation, money-write and Manual guards.
Focused adapter checks cover the retained native grant and reject an omitted
grant, a double hire charge and an extra grant. Independent review passed;
native verification is still required before any Small result is promoted.
