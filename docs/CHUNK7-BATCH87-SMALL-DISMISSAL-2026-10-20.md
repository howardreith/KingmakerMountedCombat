# Batch87 — native Small dismissal completed; diagnostic cleanup refused

The [standing mission](../planning/CHUNK7-COMPLETION-MISSION.md) remains
**IN PROGRESS**. Pushed `25547c979042c5bfbfc0746f748f725bf59fe7d8`
was frozen as `c7b-world100` (preview.82 product label), package SHA-256
`593dda286ca903cbe51ea6b7ab9e2fbb64978a851d2341326ca05f32f72b55f0`,
manifest SHA-256 `d9bc9c1e75f40fc9b31fff92708daf839674af24867d889e5a006f04953efbee`,
DLL SHA-256 `d93027c8fe6e6f69dc7d183fdb9195c308851edc28c57e9303be78b6b39387af`,
MVID `7573a4ee-8468-430e-a18e-df30e33dafaa`. Offline Candidate passed
7/0 in 5.69 minutes; packaging passed its source/build/package gates.
The append-only suite `chunk7b-completion-b87-20261020` passed WhatIf and
actual, SHA-256 `57a0444414e657dfed6b406a20e77235b61345826c533ad0bfa1c8780c8dd500`.
Three scenarios were declared in
`analysis-cache/chunk7-continuation/batch87-native-selector-flags/declared-b87.json`
(SHA-256 `c632ab9fdf9751ff7bf7d18b6ab45738daa4ec076ca7ece695c27ad75ce0f15e`).

| Slot | Native/outer verdict | Evidence |
| --- | --- | --- |
| `20261020-c7b-world100-b87s1-small-mounted-save` | **PASS 5/0** | Genuine mounted Small Manual SHA-256 `46df885c0fa8441da7ed3411e2bc8624bd14296184454959779ecab53eadf905`; producer artifact SHA-256 `09ee78aca29e9aefc4c5dd00c233fabb6b28b3973ec687b80e8b18b700ffdd7a`. |
| `20261020-c7b-world100-b87s2-small-mounted-cold` | **PASS 3/0** | Separate same-candidate cold artifact SHA-256 `9f855cce51f60e0fb431c9fa2c1d3bacaadce4f04596279b063f505775ddb7d7`. |
| `20261020-c7b-world100-b87s3-small-native-dismiss` | **FAIL 0/1 outer; no terminal game result** | Cleanup-pending artifact SHA-256 `02a57ff23aee15bd12dfda3729ac64c501d9d9942b3cbd7c71845cfb6ea0f799`; original outer result SHA-256 `b0d9ea70c1e4cd3279f20083b7092b2777c7736f3dfe7e77ad9e4a46b11eb164`. |

Every live selection passed dependency-scoped WhatIf. An initial slot-1
WhatIf refused before launch because manual-review-only package pins were
supplied to an automated case; removing those pins produced the admitted
WhatIf and native run. That refusal did not add a native scenario.

The new failure-only observation establishes the sequence that batch86 could
not resolve. The native selector opened. The target rider then had
`IsInGame=false` while `IsDirectlyControllable=true`, conscious and alive;
the mount stayed in game and controllable. `CompanionInvalidated` dismounted
the target. The bounded log excerpt SHA-256 is
`9d19f7d97d8a5cab93fe745eb09b681c3d9d07e2fbcaf042d59e3f92855dff75`.
The pending session reports one native `Player.DismissCompanion` entry and
exit, `complete=true`, a native-dismissal-complete event, and settled target
pair custody. The still-mounted control owner remains. These observations
explain the actor invalidation and show native progress, but the scenario
is **FAIL** because cleanup never finished and the game wrote no terminal
result. Do not promote the pending session to a qualified dismissal PASS.

The cleanup guard used `owner.RiderId == selection.ActorId` and the analogous
mount comparison. `MountedPairRegistry` stores its actor keys as GUID `N`
strings, while the admitted selection retains GUID `D` strings. Thus even
the declared control owner failed the string comparison. The narrow candidate
compares exact captured rider/mount object pairs instead; it still refuses
null or foreign owners and keeps all cleanup/settlement checks. The mounted
controllability/life-state invariant is unchanged: native dismissal legitimately
made the target rider leave the game. This diagnostic correction requires
offline checks and a fresh native result before F10 dismissal passes.
Independent review passed on source SHA-256
`1306d09ba7c08c964eed54270aed31200e24f812be79b7fd9121251e9a7b5161`;
its 14/0 reference-identity checks rejected foreign, reversed and crossed
pairs while admitting only the exact target and control actor pairs. The
reviewer found no other observed b87 cleanup blocker.
Focused progression wiring passed 219/0, owned wiring 231/0, and custody
21/0 + 17/0 + 5/0 (offline); Candidate passed 7/0 in 6.84 minutes on the
corrected source. These checks do not replace native verification.

The outer launcher timed out while cleanup retained custody. Its exact
Kingmaker process (PID 64156, executable, run request and token verified)
accepted a normal window-close request and exited. The wrapper then reported
its original FAIL and completed combined restoration. Mods digest
`b592f4009833072b60604852ad00660a6d236957712b63ae40f28ed1b7f67cda`,
protected-save digest
`df434471b6f2bb3ade60ecccaa524aa7537e17f63db3d0bcce6d5a7eb70561ab`,
Working, and all allowlist gates match. The native-startup profile tree equals
its b87 pre-run snapshot digest
`503dff1e55a83832e0200842eafa72dcbad95a8967c2ae5aaf068d6da285f128`
with zero unaccepted deltas; UMM Params SHA-256
`343997c80ec0d3d9796438beb4c68d0bdd4b3495d99d1816cd8f4b40d6df534b`
and preferences SHA-256
`ae26ad89a144794e03d8af5033c0c4b5103594ff587b2d0c194d49765641e824`
equal their captured pre-run bytes. No additional recovery script was needed.
There is no game process or active transaction lock. The original FAIL and
its restoration evidence remain distinct; batches 81–86 remain separate.
