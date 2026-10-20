# Chunk 7 batch51: Small field passes; native source descriptor differs

Preview.69 at `d827d6283a9f40172d5657f826cb7142ea5fd98e` was
guarded-pushed and packaged once. Candidate passed 6/0 (3.6 minutes).
Package SHA-256 `90b4a2cd76dff6db8e44be804b37944d9f4a9e1327ed2c44ebe48a680e199fea`,
manifest `9d8ec34aa019002eaeb5a1c4b193506346e9f8976362471a8ae641e6f1052630`,
DLL `6193cec103156d8b1cecf71c53f1158c95f9cbcd8304959023d33dcdf2d9179f`,
MVID `d0650902-d86d-4ccb-b7ae-ce06e378bdd1`. The append-only suite
`chunk7b-completion-b51-20261020.json` SHA-256
`4ec24c50e0afef6bfb4b91c91c9cfda9fb424bf7369e2e2c0679954001ec1497`
kept save digest `7017207d543f81d0ddf7c15562a855f43492c7ab6da4c91ab34d1cf18ced69e9`
and Mods digest `e81a49d813bcff8cdb70fb6e9f007234fd6ce5674758873d9e31e828ece2e21b`.
Private one-slot declaration `group69-native-b51/declared-b51.json` SHA-256
`5a7b44ab8507f60d38b445ce3dfa2dfb2e980a13c1e158e81bbd0aeca108082f`
preceded the run. Suite and scoped scenario WhatIf passed.

The one native slot `20261020-c7b-world69-b51s1-small` remains **FAIL**.
Outer result SHA-256 `85213c1e5e3b58ed13896e322e3ed221426120b623a98b14c9beccba5798e768`,
game result `e7bc69b0909af25fdccaa6bfdb547de886b47b27c6458abc62af275ae617e7ae`,
Small evidence `7844cac5d6b5457777200b4f3bb0313da750f2cc7e5d05838491d7bf44a2f6f1`.
The source, native load, native choices, unmounted field and cleanup rows passed;
runtime failed at `BeginManual` before native numbering, reservation or save.
The paid hire completed four native commits and the selected Halfling and Horse
passed the exact seven-actor field and live rig inventory. This does not certify
a Manual, cold load, mounted Small profile or later checks.

The failing source condition required the sole `SaveManager` list descriptor
to be the same CLR object as `WorkingFixtureLoader.Descriptor`. The isolated
save root after the run contains only the immutable original seed. A prior
owner-seed Manual producer diagnosed the same native distinction: `LoadZipSave`
creates the loader descriptor separately from the manager's list object
(`Chunk7WorldScenarioEngine.OwnerSeedManual.cs`, with native record cited there).
Thus this is a known descriptor-identity contract mismatch, not evidence of
an extra archive. The current correction captures the manager's sole source
object only after matching the loader's file, path, name, game and area and
validating it against the immutable archive authority, then uses that exact
manager object in the later two-entry inventory. If the list differs for
another reason, the corrected check will still refuse with count and
reference details. Independent review passed for the targeted correction;
native verification remains pending. The selected next native scenario is its
regression path.

The guarded transaction reports Mods, protected saves and Working restored,
Baseline immutable and no restoration errors. Read-only inspection found no
Kingmaker process or runtime lock. The existing profile policy admitted 19
native cache changes; byte-for-byte profile equality is not claimed. Profile
cache receipt SHA-256 `2574dc4d9ee02010a47720f026dfeddcde2e989d5beb5105bbb85c36b0d73308`.
No manual recovery was needed. The batch49 and batch50 original failures
remain separate.
