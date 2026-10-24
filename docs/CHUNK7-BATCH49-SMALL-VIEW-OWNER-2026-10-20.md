# Chunk 7 batch49: paid remote actor owns the changed view

Preview.67 was guarded-pushed at
`7aebe1e2ce716a8da6c2a404a3a4445032d5a9a7` and independently reviewed.
Fast passed 3/0; Candidate passed 6/0 in 3.65 minutes. Package SHA-256
`3467872b824053036e538168ba81b24c43eac3139fcaba981b2d06c5d193515c`,
manifest SHA-256
`8b345aae6f30b1d47755909ec4c9781f64afeda6945433c23a34f97073f8c87e`,
DLL SHA-256 `834126dcc3748e406a5ef7b32532d1e0770ce0e20de7c461da7766370f1d03c8`,
MVID `1be4e1cd-0b24-4753-8e74-d0fa0bf5e7ed`. Fresh suite
`chunk7b-completion-b49-20261020.json` SHA-256
`388bc32b305346208dd2180bb73fc803523290091d0ee2bab94d695716bf5a9f`
retained save digest `7017207d543f81d0ddf7c15562a855f43492c7ab6da4c91ab34d1cf18ced69e9`
and Mods digest `e81a49d813bcff8cdb70fb6e9f007234fd6ce5674758873d9e31e828ece2e21b`.
The private one-slot declaration `group67-native-b49/declared-b49.json` SHA-256
`cf665005e9957c273763cad568eb40cf0fd63ed94fd6659f3be7fcde2e543fdd`
preceded the native run. Suite and scoped scenario WhatIf passed.

The one native slot `20261020-c7b-world67-b49s1-small` remains an **original
FAIL**. The first retained F09 error occurred at stage 3, after main commit
1 and paid-hire commit 3. The changed actor was
`74c40317-da3d-41d2-9a6a-8d65dc9c8a43`, identical by reference to both
the F09 captured paid actor and the build driver's actor. It was the sole
remote companion, absent from PartyCharacters and `Game.State.Units`, at
level 4. Its captured CLR view changed from `f4cd2e00` to `15acae00`.
The main actor and new Horse remained exact scene members; the Horse had a
different ID. The native builder binding read false at this failure boundary;
the installed driver's own checks allow a closed controller between native
level commits, so false alone does not identify a foreign builder. Pending
evidence SHA-256 `0d607117bce88bb45944a8eea1fe0e61262d85eb08f4efb6a2f298060ebfbc19`
contains the full role/roster observation. No Manual or atomic game result
was reached. Outer result SHA-256
`26a6041189d7f52e19ed19acc664ad20d5bd532b5caf8a62d90a20c9afaecca8`
records early exit after the exact owned process was closed normally; the
post-result reader refused the pending artifact without an atomic manifest.

The guarded wrapper restored Mods and saves, including immutable Baseline
and Working. Read-only final check found transaction phase `restored`, game
and lock absent, Params/PlayerPrefs equal to this run's snapshot, exact
profile bytes and zero admitted cache changes. No manual recovery was needed.
The original FAIL is not a Small fixture PASS or verification of the
preview.65 paid-hire money correction.

The causal scope error is F09 tracking the paid remote actor's view as soon
as CharGen exposes that actor, although installed native level commits can
turn it off and on before the paid build completes. The next correction
in preview.68 delays only F09's capture of that remote actor until completed
native build and exact remote/party/scene identity checks. The original
world and Horse fences remain active. Independent review passed, the focused
world-scope adapter passed 75/0 and Fast passed 3/0. It must be verified in a
new native run before it or any downstream Small result is accepted.
