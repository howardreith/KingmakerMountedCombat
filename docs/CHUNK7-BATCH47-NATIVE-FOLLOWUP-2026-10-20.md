# Chunk 7 batch47: native follow-up and exact timeout recovery

The standing completion mission remains active. Preview.65 was committed and
guarded-pushed at `779876ce65c8069509118bfee3f8a00e862976c8`. Candidate
passed 6/0 in 3.73 minutes. The frozen package SHA-256 is
`85e0302ca01f74bbb9bde27206a8d81bb618ad574ca02b3c6ef16991bad97475`,
manifest SHA-256 `afdd622c8b6ab016bf7df049dc883a525a96a8ff933b6d0110aa787988558a11`,
DLL SHA-256 `bb2f35ccc1d36fcd343306832afd0048b69e60f4ff222fbbca4d14aa416562c8`,
MVID `734ba257-f082-4b1a-807c-8c4f83cccbe9`. Append-only suite
`chunk7b-completion-b47-20261019` SHA-256
`33b0ab12c72a25a2f7890b8848801f741a9d3a25d97711d281a7541131f3d53b`
retained save digest `7017207d543f81d0ddf7c15562a855f43492c7ab6da4c91ab34d1cf18ced69e9`
and Mods digest `e81a49d813bcff8cdb70fb6e9f007234fd6ce5674758873d9e31e828ece2e21b`.
The private two-slot declaration `group65-native-b47/declared-b47.json` SHA-256
`cac3db7b9466e4ad3ea1c437dc88298460244931c402331797c5cabaef306576`
preceded both native runs. Suite and both development-scoped WhatIf checks
passed. The two live slots ran sequentially in the approved Howie context.

| Slot | Original outcome | Separate restoration |
| --- | --- | --- |
| `20261019-c7b-world65-b47s1-manticore` | **Outer FAIL** SHA-256 `e5515c118b18918f8f642b8f0cec67374ca865f365c017d5349b90de7c09f2da`; game **PASS 5/0** SHA-256 `226d4c002b90606835f4d69e12b66a6d2fdff81a530da313ca790581eeffef7c`, genuine non-capital Manual evidence SHA-256 `57e35ea800e27ee39479441f5890bec056800efd64010ada55e5f99f906181bb`. The outer profile check again reported a newly created zero-length `achievements.dat*` leaf after its sixty-second wait. This is the third distinct original outer FAIL; the Manual is **not** an admitted area source. | Mods, Working and protected saves restored by the wrapper. Read-only recheck afterward found no cache delta and showed that the printed zero-leaf name also exists in the intake snapshot. Exact-run generic profile recovery WhatIf and live PASS, receipt SHA-256 `43ae4a31aa68068001384acfbaff25227bcf4af25539a80f79a7a65499a98a78`. Game and lock absent. Recovery does not promote the original FAIL. |
| `20261019-c7b-world65-b47s2-small` | **Outer FAIL** SHA-256 `1fc86e63e22e9b14eebdc553e812f5f59584ae52635a4970b14cbc470ed2634f`; no atomic game result or Manual. The first retained failure is `Captured live native view changed` in `Chunk7NativeWorldScope.RequireOriginalActors`, reached from F09 `RequireRoster`. The cleanup-pending artifact SHA-256 `c1280e708453ebc80cb0f6c34e21546b1ba9c2236da44e2e67bb193f3c2d011a` records one native load, zero writes, and custody retained when the same strict view check blocked cleanup. The 900-second outer result then timed out and deliberately left the owned game process and transaction staged. This does not verify the preview.65 paid-hire correction or Small profile. | The exact process PID 26428, start time, executable, run ID, command-line token and window were checked before a normal `CloseMainWindow` request; no unrelated process was touched. After stable process absence, `Recover-KingmakerRuntimeTransaction.ps1` WhatIf and live PASS restored Mods digest `b592f4009833072b60604852ad00660a6d236957712b63ae40f28ed1b7f67cda` and save inventory digest `df434471b6f2bb3ade60ecccaa524aa7537e17f63db3d0bcce6d5a7eb70561ab`, including immutable Baseline and restored Working. That recovery released the lock but left startup Params/PlayerPrefs changed. A new exact-run `Recover-KmcProfileAfterCombinedTimeout47.ps1` passed WhatIf and independent review, then live PASS; receipt SHA-256 `ab2a36169b65df58858308a879846d4d4587c8ba707d2553ac4a42584360aaa4`. Read-only final check: game and lock absent, transaction `restored`, Mods and saves exact, Params/Prefs equal this run's snapshot, profile identity equal with zero cache/analytics delta. The original FAIL remains. |

The Manticore profile error needs a changed diagnosis before another producer
attempt. The printed name matching a pre-existing zero-length snapshot leaf
could reflect a transient distinct native name lost during JSON/display
conversion or a path comparison defect; neither is proved. Capture exact
UTF-16 code units and prior/current membership only on the next zero-leaf
failure. Do not extend the wait again without new evidence or whitelist a
profile change.

For Small, do not relax the live actor/view fence from this failure alone.
Capture which actor, old/new view identity and F09 stage at the exact mismatch,
then distinguish a lawful native view rebuild from wrong-world replacement.
The cleanup timeout is downstream of that first failure. A future native run
must add this missing observation or verify a reviewed correction; it must not
repeat preview.65 unchanged.
