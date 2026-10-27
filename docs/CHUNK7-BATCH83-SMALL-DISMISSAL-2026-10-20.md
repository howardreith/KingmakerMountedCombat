# Batch83 — exact native inventory mutation at Small dismissal

The [standing completion mission](../planning/CHUNK7-COMPLETION-MISSION.md)
remains **IN PROGRESS**. This batch used pushed source `44a0ef22f39b74f5e49715322317362e145ad70d`,
preview.96 package SHA-256 `b05cdb70ba543329a10e70f762213b74b3bbd23cd0567e449ea042cade743d16`
(DLL `e398f5da199f5766dbd665dad472ea2fc0e185bc0e636d72242da5f3562a332a`),
and suite `chunk7b-completion-b83-20261020` SHA-256
`c6f68b3f74ef37838b623056904ee9a73f46853ec95e110a0f7763677b69846b`.
Three scenarios were declared under
`analysis-cache/chunk7-continuation/batch83-small-dismiss-inventory`; each
selection passed its dependency-scoped WhatIf before launch.

| Slot | Original result | Immutable evidence |
| --- | --- | --- |
| `20261020-c7b-world96-b83s1-small-mounted-save` | Native/outer **PASS 5/0** | Genuine Manual SHA-256 `026bf57330c6436ba02492b1092174abe11a4b30fd28358fdd2b267c48f3168b`; artifact `ee93ff28b0f279ebe268fd5b2fa7efce33cad1abec828e8905a9c2136b3ea7de`. |
| `20261020-c7b-world96-b83s2-small-mounted-cold` | Native/outer **PASS 3/0** | Separate cold artifact `58a824b6f0a89c672fecb8ba5da65b70af595d4d250d484d34ee6dccb13928d6`. |
| `20261020-c7b-world96-b83s3-small-native-dismiss` | Native/outer **FAIL** | Original pending artifact `4b484620973176616fcf50816d1e113980ccd95b811b008cf42542a6f05cd48b`; original outer result `eb0baf97283a576033afaef410c7fbb96eab588249b94f0d31a6a075cc4171eb`. |

The read-only entry/failure snapshots show inventory membership **76 to 75**.
The equipped target-owned stackable with blueprint
`bc93a78d71bef084fa155e529660ed0d` and object hash `-1201838848`
disappeared; a pre-existing unowned stack of the same blueprint (object
`-867537408`) increased from one to two. Other target equipment remained.
The exact installed `Player.DismissCompanion` enumerates `Inventory.Items` and
calls `ItemSlot.RemoveItem()`; its default auto-merge removes this item from
the same list, invalidating the enumerator. The resulting exception precedes
native completion and rider destruction. This is the causal defect, not an
accepted dismissal. Batch81, batch82 and batch83 original failures retain
their separate inputs and verdicts.

The exact timed-out process exited after a verified normal close request.
Combined guarded recovery passed WhatIf and actual, returning live Mods digest
`b592f4009833072b60604852ad00660a6d236957712b63ae40f28ed1b7f67cda`
and protected-save digest
`df434471b6f2bb3ade60ecccaa524aa7537e17f63db3d0bcce6d5a7eb70561ab`.
The independently reviewed, exact-run
`scripts/runtime/Recover-KmcProfileAfterCombinedTimeout83.ps1` passed WhatIf
and actual; receipt SHA-256
`644172dbf26ed09be450e5de8864b934320d6487ea62f5c4b918938556061198`.
Read-only checks confirmed exact profile, UMM Params, preferences, Mods and
protected saves, with no game process or transaction lock. Recovery is separate
from and does not change the original FAIL.

A narrow candidate captures current mounted rider/session/roster eligibility
before native party removal. Its exact-callsite transpiler changes only the
`RemoveItem(bool)` auto-merge argument from true to false for that owner. Native
party removal, in-game transition, equipment callbacks, boolean result,
exceptions, destruction and event order remain in their original control flow.
Unrelated actors keep the original argument. A first prefix approach passed
focused 22/0 and Fast 3/0 but was rejected in review because it moved native
equipment events ahead of party removal; it was never run in the game. The
replacement passes focused 30/0, Fast 3/0, offline Candidate 6/0 and a separate
pinned installed Harmony12 patch/unpatch test 1/0 without invoking native
dismissal. Independent production/safety review passed; it checked the exact
native event order and unique callsite. Native qualification remains required
before F10 dismissal is PASS.
