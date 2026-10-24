# Batch82 — native Small dismissal inventory failure

The standing [completion mission](../planning/CHUNK7-COMPLETION-MISSION.md) remains
**IN PROGRESS**. This batch used pushed observer source
`d3b7332b1752a54615e3129ccb5a60c8587e333f`, preview.95 package SHA-256
`5fa4d8569467e2cb81989b7a4b4e1928c88fedce5b3430e52bceb69aa3e772f4`
(DLL `06e11ee5ec895bc4e85ffa33d50715c45d352e4d9d53ed082be3f18e4ac16c43`),
and suite `chunk7b-completion-b82-20261020` SHA-256
`d36c9c73f21fb8276eb1f7131787c809760fb7bae04f5df9886c0bced172af05`.
The three declared selections are under
`analysis-cache/chunk7-continuation/batch82-small-dismiss-observation` in the lab.
Each run passed its dependency-scoped WhatIf before launch.

| Slot | Native and outer result | Exact evidence |
| --- | --- | --- |
| `20261020-c7b-world95-b82s1-small-mounted-save` | **PASS 5/0** | Fresh mounted Small Manual archive SHA-256 `9a6439df82b99751b1ce4110a15f77f96f058bb0e681578803e5447500177943`; persistence artifact `0391397ad62a1bd71603f70fd192850057a9507df0897962e625dd39ff5c4aa1`. |
| `20261020-c7b-world95-b82s2-small-mounted-cold` | **PASS 3/0** | Separate cold consumer persistence artifact `03663c4d6a118e43e52b9a40f19cd4f0e25485bd63e5c0f9946540e38a41cdb7`. |
| `20261020-c7b-world95-b82s3-small-native-dismiss` | **FAIL** | Retained cleanup-pending record `96ef3aa218a7633cb12d17ed32c479a1adcb647725931969be46b7c7d655ff1b`; original outer result `b45f95d848663ebce1afcc2a63ed40b7bbdaa5a0a115f9639864c2e6072b9983`. |

The failure now has a distinguishing cause within the native call. The real
party-selector Yes entered exact `Player.DismissCompanion` once, but an
`InvalidOperationException: Collection was modified; enumeration operation may
not execute` escaped before its exit hook. The target left native Party and
PartyCharacters and became out of game, but was not destroyed; its original pet
was not destroyed. The observer therefore did not claim a complete dismissal,
and the world guard correctly refused cleanup. This preserves batch81's earlier
FAIL as its own input; the preview.95 result does not retroactively change it.

Bounded decompilation of the exact installed method shows it enumerates
`Player.Inventory.Items` and calls `item.HoldingSlot.RemoveItem()` for each
target-owned item before destroying the companion. `ItemSlot.RemoveItem` can
call `TryMergeInCollection`, and `ItemsCollection.Extract` removes from that same
list. The saved target has equipped armor, but the present record does not show
which nested operation changed a collection. The next observer captures bounded
inventory identities immediately before native entry and at the first failure,
without changing the call or its assertion. A safe native setup change, if
needed, must be separately disclosed and qualified; this failed archive is not
reclassified as a pass.

The timed-out game was the exact runner-launched PID 33364, matched by request
path and transaction token; it exited after a normal window-close request.
The guarded combined recovery passed WhatIf and actual, restoring pre-run live
Mods digest `b592f4009833072b60604852ad00660a6d236957712b63ae40f28ed1b7f67cda`
and protected-save digest
`df434471b6f2bb3ade60ecccaa524aa7537e17f63db3d0bcce6d5a7eb70561ab`,
including immutable baseline and Working. Profile files were already exact.
Independently reviewed one-run
`scripts/runtime/Recover-KmcProfileAfterCombinedTimeout82.ps1` passed WhatIf
and actual; its receipt SHA-256 is
`a12074da5ddd529e3b1e6d62d672ac583f00cfd097be0dc9cdc3850f9b0f34fd`.
Subsequent read-only checks confirmed exact profile, UMM Params, session
preferences, Mods and saves, no Kingmaker process and no transaction lock.
The recovery does not convert the original native or outer FAIL to PASS.
