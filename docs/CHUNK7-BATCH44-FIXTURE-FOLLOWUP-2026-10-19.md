# Chunk 7 batch44: native fixture follow-up and preview.63 correction

The standing Chunk 7 completion mission remains active. Batch44 declared exactly
two guarded native scenarios on the frozen preview.62 package (SHA-256
`85828a5d7fb9fcac354b5e96b897a1ae28e8f0ed43f9338312cb85f307dcc981`),
DLL SHA-256 `2829323320c028643f356e2ca2059d9ec9d7d482a477ee6eeb458d5a5580d45d`,
suite `chunk7b-completion-b44-20261019` SHA-256
`95024746053b52671535a7e788a37687333a274ce7b597195d28b3159e8c0cb7`.
The scoped WhatIf checks passed before each live slot. The declaration is the
private `group62-native-b44/declared-b44.json` at SHA-256
`90f5bd0764b2113336ca17adb0176ca67a20172df987f7175980b72bd330b2c5`.

| Slot | Original result | Transaction settlement |
| --- | --- | --- |
| `20261019-c7b-world62-b44s1-manticore` | **FAIL**, outer result SHA-256 `a5f2d93e927fa4e5c3832d0480310e28f4b6bea92b7bb7845ea2ec26840ec313`, game 2 PASS/1 FAIL. Both native Dismounts settled, the sole native `LoadArea` reached non-capital ManticoreLair2, and both native Mounts were accepted. The four pinned managed hotbar slots had no persistent KMC bindings after remount. The fixture rejected rebind because it required the old ability kind already present at those positions. No Manual, area Auto/cold, or door result followed. Game evidence SHA-256 `8f477563c22f8afd0042da2ad76ab6041564e9a20c5a47d2983eaf1b67163f38`. | Mods, protected saves and Working restored. Game exit reported a transient native achievement-cache name/size discrepancy; the exact run's generic guarded profile recovery passed, receipt SHA-256 `f03a79fd341e218845938144d025d15c81832d6f191edda6cb2765fdb2f9e80f`, and released the lock. The original native FAIL remains. |
| `20261019-c7b-world62-b44s2-small` | **FAIL**, outer result SHA-256 `b048d0c0ef3c25bdd50cf9566e87dba966a6ac3a26ed8e77ab813c2380e76f6a`, game 3 PASS/1 FAIL. Native main Ranger 3→4/Horse and paid Halfling CharGen choices proceeded to its Total commit. The driver demanded the 8,000 hire expenditure at the first observed level transition, while the native success callback had not yet charged the initial 400,057 balance. No paid completion, field admission, or Manual followed. Game evidence SHA-256 `2460bd026cc0a6fc38236ac3d7f14c217c179cf04bab2d8eb53cca666c50fc71`. | Mods, protected saves, Working and startup settings restored; transaction phase is `restored`, with no game or lock. The runtime separately admitted three settled native achievement-cache rewrites/creation (`profile-cache-changes.json` SHA-256 `7059284e7a5162d549ce2086b04725a1304cab681e2ec371780f5286d4af2b1e`); profile bytes are therefore **not** claimed equal. |

The preview.63 source correction allows rebind only into a null cell, an exact
actor-owned native empty cell, or the same actor's original control kind; it
still refuses foreign hotbar elements and verifies the exact four bindings and
active facts afterward. The retained game artifact records zero persistent
bindings after remount but does not expose the post-remount slot classes, so
this correction requires the targeted native retry.

Installed `CreateCustomCompanion.RunAction` supplies a success callback which
calls native `SpendMoney(GetCustomCompanionCost())`; `Player.CreateCustomCompanion`
passes it into CharGen, and `LevelUpController.Commit` calls success after
applying the level. The paid driver now observes only the initial or exact
single-cost balance, records its one transition, and requires the paid balance
before terminal completion. It never writes money or changes native callback
order. Preview.63 paid evidence declares `native-hire-spent-v1`; both affected
readers require the explicit once-only event for preview.63 and later, while
retaining old-format synthetic and pre-.63 fixtures on their original inputs.

Independent read-only review found no remaining source/reader blocker for a
targeted native retry. Ordinary choice reader passed 47/0 (including missing,
duplicate and wrong-cost refusals); Manual-derived reader passed 103/0 with
the new versioned path; Manticore reader passed 62/0; F09 adapter passed 60/0,
fixture evidence 67/0, and source-copy checks 19/0. A first Fast sweep during
version synchronization failed metadata equality and remains recorded; after
`BuildIdentity.cs`, `version.json` and `Info.json` agreed, source validation
passed 147/0 and Fast passed 3/0. These are offline checks only.

Next: commit and guarded-push the coherent preview.63 source and reader, build
the unchanged package once, run Candidate, freeze a fresh suite, pass scoped
WhatIf and retry only the two changed native paths with new run IDs. An accepted
Manticore Manual needs separate area Auto/cold and door qualification; an
accepted Small Manual needs separate cold admission and body/profile calibration.
Positive reference-free mounted cleanup and DLL-absent cold, F10 acquisition
and progression, measured 7D hardening, and the selected private 7E rehearsal
remain required by the standing mission.
