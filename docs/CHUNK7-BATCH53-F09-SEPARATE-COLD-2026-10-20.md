# Chunk 7 batch53: F09 separate cold native load and reader correction

Preview.71 was guarded-pushed at `784a6296f9e0d91c44ad25dda826db57f609e929`.
Its diagnostic package SHA-256 is
`f2b4da085c174c08136dc84cfc064ead82828de67357217aeab91a49d4cdfa24`,
manifest `80e248e8d20260ef9f4927751416753b6c479824c7e4e61469566f77d1239e17`,
DLL `8f7f89769c82e0ac15f5d972441d29e68d6f67ff4ad942fe55f25ece12047d89`,
MVID `bf98be50-898a-41b3-88f0-78959a0ea1a3`. Package validation passed
11/0 and Candidate 6/0 (3.66 minutes). The append-only suite
`chunk7b-completion-b53-20261020.json` SHA-256 is
`7d11328826d0a88b2c4ac2ff920bb0cf00b3aa6adcc0e10f3e5315dac7125004`.
The two-scenario declaration is
`analysis-cache/chunk7-continuation/codex-takeover/group71-native-b53/declared-b53.json`
SHA-256 `40c672deae61e2e86a5852aa513156e7dc9ed8b788f802d30f4b0e32855d82b`.
Each scenario passed a separate development-scoped WhatIf. The first WhatIf
attempt included manual-review-only artifact pins and refused before launch;
the corrected suite-bound WhatIf passed. No hidden scenario was launched.

Slot 1, `20261020-c7b-world71-b53s1-small`, **PASS**. The genuine native
F09 Manual producer wrote `Manual_304_KMC_C7_SMALL_FIXTURE.zks`, SHA-256
`ddf2b9cf4c0c0a49c93a659d2628ae6622f02e0a400a2df287bd5713f12ed829`.
Producer artifact SHA-256
`9d26bd146970fc53192bf2f28cef8c6655a7c79beb1326c075fec0a5c406e52f`;
outer result `e9fc3b5a2a564b656f8e8db2adce01d8617fe656a08edaa3f8f972d4533a8e79`.
The same-candidate source gate subsequently revalidated the native Manual,
base-only isolation, protected fixture anchors and nine settled receipt pins.

Slot 2, `20261020-c7b-world71-b53s2-smallcold`, had a **PASS native game
result** (SHA-256
`d3286022d2f60c2f75fc42bc97ba0e579871b269c7ae1a1c61cf8587635403d9`)
and **PASS native artifact** (SHA-256
`d688026b61aeabd874a2220d9f5642cd9fae2dd21b9b465531d73992acc7bd11`),
but its **original outer runtime result remains FAIL** (SHA-256
`1c3167ef45e62b3d85ab5f49b8bc3776d4e832a7506ab593010c0d947df01be2`).
The external F09 reader required a nonnull `Player.GameId` at both native
`PostLoad` events. Both events actually had null `GameId`, while loaded and
settled worlds had the exact saved campaign ID. The existing owner-seed reader
already allowed this native timing. The game-side observer read one independent
copy in a different process, seven native actors, five characters, reciprocal
Horses, 392468 money, retained progression, empty mounted metadata, one
authorized load and zero writes. Its source and cleanup remained pinned.

A bounded **reader-only correction** now permits null `GameId` at those F09
`PostLoad` events while rejecting a wrong nonnull value and requiring exact
campaign identity in the loaded and settled worlds. Standalone game/result
validators now import the source gate, as the runner already did. Focused F09
reader 7/0 and cold wiring 114/0 passed; the standalone original game-result
reader passed 39/0, and the original FAIL result still validates as a FAIL.
Independent review found no blocking issue. The immutable original
request/artifact/source passed the corrected reader without another native
run. Separate read-only re-evaluation receipt SHA-256
`d188012fb1f9a19410a572b67381bfb15be5e813a215a79196ed734b49c59bfc`
is in `analysis-cache/chunk7-continuation/codex-takeover/group71-native-b53/`.
It pins input hashes and preserves the original outer FAIL.

Both batch53 transactions are `restored`. Base-only staging reports original
Mods and protected saves restored; both profile receipts report zero accepted
cache changes and exact profile-byte restoration. No game process or runtime
lock remained after either run. The scoped WhatIf is not a full-lab purity
claim. The preview.71 product DLL was not changed by the reader correction;
the corrected reader does not create a new package or new native result.

F09 now has a genuine Small Manual with a separate cold-load development
result, limited to this seven-actor unmounted fixture and reader version.
Mounted Small calibration, Medium profiles, conditional six-pair breadth and
the other standing mission gates remain open. Historical F09 FAILs and the
batch52 preview.70 PASS retain their own identities.
