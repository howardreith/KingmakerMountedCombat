# Batch85 — Small native selector view activity

The [standing completion mission](../planning/CHUNK7-COMPLETION-MISSION.md)
remains **IN PROGRESS**. Pushed source `52f17f1703395fc0daf130a257275cc479814216`
was frozen as `c7b-world98` (preview.82 label), package SHA-256
`f92154bfe12684f35ae03ffe57fad6ddd133e2f341de6dec62c71c2f6cb0411b`,
DLL SHA-256 `0e92629033ab406d06fe2c3627c4e58941c537edff0b57cfbbbe66486e31989b`,
MVID `3aac46c3-8ac8-424f-8503-d4a733f0d83b`. The frozen suite
`chunk7b-completion-b85-20261020` has SHA-256
`8488ddccef3efbc1d84421db450ae1688a277c8d8bb9ea0dce752b24fae76377`.
The three declared selections are under
`analysis-cache/chunk7-continuation/batch85-selector-activity-observation`;
their declaration SHA-256 is
`5b18ac96a0b9b2336835907ae6203a90bb9b1de1a5fffbf45738f773fe2b4c49`.
Every live selection passed its dependency-scoped WhatIf before launch. Two
incorrect slot-2 WhatIf attempts were refused before launch (UTF-8 BOM and a
mistyped suite path); the corrected WhatIf passed. They are not native runs.

| Declared slot | Native/outer result | Evidence |
| --- | --- | --- |
| `20261020-c7b-world98-b85s1-small-mounted-save` | **PASS 5/0** | Genuine mounted Small Manual SHA-256 `15b7b134294036fbf45de1e21cdcf6bf1c1f9e1837c7fe344150589ce0e44d30`; artifact `0df7ab063365aa2c07e32d71dfd480ae70b27c96c61cc8ab336c7463ec`. |
| `20261020-c7b-world98-b85s2-small-mounted-cold` | **PASS 3/0** | Separate same-candidate cold artifact `1e263c1629efeac5e14d4b3fefa8bd54e3adda931832bb4edcc8ab336c7463ec`. |
| `20261020-c7b-world98-b85s3-small-native-dismiss` | **FAIL** before native dismissal | Cleanup-pending record `0cfd7660eb2cc29156a77df42eb6bb06c0ddac81641cb1d01f88bbfbf959027c`; original timed-out outer result `cba539d77bc984ff056ec73eb2c1bdfcf596ea66bed4d3e7f1a965fc5aadefe0`. |

The loaded two-pair mounted world passed restoration. Immediately after the
native `ShowPartySelection` call, the mounted invariant saw the Small rider's
captured view inactive (`riderSelf=False`, `riderHierarchy=False`) while its
Horse remained active, with `mode=Default` and
`disposition=PreserveWorldInteraction`. The selector-open record and a bounded
immutable copy of the relevant game-log excerpt (SHA-256
`a8393fe2384173258a58d7fa8d97d3c9c7f36bdd4cfda9a71d9b23231192c0f`)
retain the ordering. The observer did not capture the GroupManager activity
flag in this binary, so the exact selector gate still needs native verification.
The subsequent `CompanionInvalidated` dismount led the progression cleanup to
refuse an undeclared owner and retain custody. No dismissal confirmation or
`Player.DismissCompanion` was reached; this is neither a native dismissal PASS
nor a test of the reviewed inventory auto-merge repair. Earlier batch81–84
FAILs remain separate.

The timed-out exact process exited after a verified normal close request.
Guarded combined recovery passed WhatIf and actual, restoring the pre-run live
Mods digest `b592f4009833072b60604852ad00660a6d236957712b63ae40f28ed1b7f67cda`
and protected-save digest
`df434471b6f2bb3ade60ecccaa524aa7537e17f63db3d0bcce6d5a7eb70561ab`.
The independently reviewed exact-run
`scripts/runtime/Recover-KmcProfileAfterCombinedTimeout85.ps1` passed WhatIf
and actual; its separate receipt reports profile, UMM Params and preferences
restored and game absent. The original outer FAIL remains unchanged.

The follow-up source candidate permits transient view inactivity only while
the exact native party-selector component is active and the current mode still
retains a mounted relationship. Exact view identity, reciprocal companion,
controllability, life state, size/profile, movement/AI leases and presentation
checks remain; closing the selector restores the strict world-view predicate.
Independent review passed for the exact runtime/policy/test source SHA-256 values
`545779e4a91b2a7e64f5ca835936ecb5fd07ab34f7a27e64b7851b78bd0d55cc`,
`1d538e92791373eadb0d74f286ef26142a655228ce633fee4c50f2ee896e7bc3`
and `718fe01b78a5ccc3af14c6f73a1cdcd38fc7e391cfbb4b55423d728b724399b9`.
Focused behavior passed 14/0 plus 224 exhaustive policy combinations; Fast
passed 3/0 (offline only). Native validation remains open.
