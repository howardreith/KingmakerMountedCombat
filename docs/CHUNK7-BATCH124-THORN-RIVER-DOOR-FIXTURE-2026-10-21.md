# Chunk 7 batch124: Thorn River Camp door inventory

Status: **native fixture PASS; ordinary door remains unqualified.** The active
[completion mission](../planning/CHUNK7-COMPLETION-MISSION.md) continues.

The accepted mounted owner-seed Manual, SHA-256
`b4d1ede7843c600f201dcd7b71cd2558cad44210e7e0eb78d9a58f6e89f64003`,
contains a revealed, open, unexplored Thorn River Camp location
`e929000884b47804f9c026c873aaa57b`, its actual stock entrance
`1ab738bc965352a479b68d1bd325c810`, and non-capital destination
`7d72372ade2f14448a3fec6d61e06bd8`. This was the final revealed field
destination on that accepted source not already inventoried. The new closed
source-only branch rechecks the saved map row and entrance at runtime. It
uses two native Dismounts, one `LoadArea(entry, None)`, destination/party/free-
control checks, two native Mounts, and one distinct Manual. A bounded read-only
inventory observes `StandardDoor` objects without clicking or altering them.
The existing Areas isolation and exact transaction restoration cover this case.

Independent read-only source/safety review found no admission or restoration
blocker. Focused synthetic checks passed: fixture reader 79/0, Areas isolation
39/0, persistence wiring 161/0, owned DTO 182/0, source 284/0, manifest
143/0, owned reader 690/0, persistence evidence 29/0, and area source 11/0.
The source route commit is `a514453e`; the preview.89 version commit is
`a610eda7b6f0482f8ae0cd3c8b0e1d5609d85e87`. The clean-source package
`C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.1.0-chunk7b-preview.89-c7b-world89-thorn-river-door-inventory-diagnostic.zip`
has SHA-256 `03fc2f88afb8ed166fa2dc29a386e6ebcdfe2c45edb7739bb5941a0b43376ac8`;
DLL SHA-256 `16a51011b9bad88c92a89b50daa5852ca6675d8f3fe660a17c9d3a2639dffac5`,
MVID `d60ec1f1-8182-4e43-a4e0-d1621da31a61`. Candidate passed 6/0 in
3.77 minutes with the package build reused. No broader historical campaign
was selected for this source change.

The append-only current-state suite `chunk7b-completion-b124-20261021` is
SHA-256 `aead64a81c00bfbc1314f1830604a5873d8cfd0301065ef8cdd23e61d7135f64`.
Suite WhatIf and the development-scoped runtime WhatIf passed. The batch
declaration and fresh, byte-identical owner-seed selection copy are under
`analysis-cache/chunk7-continuation/b124-thorn-river-door`; selection SHA-256
`0a240abd3d199c470322d9f9c6268ac2cc82882f89b685c868c3df54470ae3cb`.
Only one native scenario was declared and launched, under the approved Howie
execution context.

`20261021-c7b-world89-b124s1-thorn-door` passed game and outer reader **5/0**.
Runtime result SHA-256 `e063fb5b5d6ecebac6a50cbf18fa5e273c726bc214dde8ff9fcf1b5252262124`;
game result `af952fba0524083995706211b5098368aab61ba36c7e55b2cf17becca8ad9590`;
persistence evidence `8a8b02902456d355b8ce666670364d9fcec86207c1003e5165ef6a36ed0b91af`.
The genuine destination `Manual_305_KMC_C7_THORN_RIVER_DOOR_FIXTURE.zks` is
1,326,830 bytes, SHA-256
`312faae6abc5b8fd087bf25553240f2b88102584f3ff342c816a69a8e5af0ff3`.
Its after-remount inventory observed **zero** `StandardDoor` candidates
(`candidateCount=0`, `truncated=false`) at the two recorded mount positions.
`interactionTested=false`; neither a door click nor an ordinary-door PASS
occurred. Spider Cave and Manticore Lair also had zero candidates on their
own earlier inputs; Old Cyclops Tomb had one ineligible closed, distant secret
door. These scene-specific observations do not exclude doors elsewhere.

Transaction state is `restored`, SHA-256
`bf35a4f99f5acd0c2bdbd7cd741697f3940a0ce0efb011ad922f26228d8356ee`.
Mods before/after digest is
`b592f4009833072b60604852ad00660a6d236957712b63ae40f28ed1b7f67cda`;
protected-save inventory before/after digest is
`df434471b6f2bb3ade60ecccaa524aa7537e17f63db3d0bcce6d5a7eb70561ab`.
Baseline immutable, Working restored, save allowlist and protection passed.
The original live `Areas` was restored exactly; profile bytes are exact and
there are zero accepted cache exceptions. No Kingmaker process or runtime lock
remains.

The owner-seed's admitted field destination inventory is now exhausted for
this door search. The next door attempt needs a newly admitted source with an
observed suitable door and safe mounted approach, or a precise bounded owner
navigation recipe after identifying its destination and starting disposable
copy. Continue independent F09/F10/F11 implementation and qualification;
this fixture survey is not an engineering closeout or 7E package rehearsal.
