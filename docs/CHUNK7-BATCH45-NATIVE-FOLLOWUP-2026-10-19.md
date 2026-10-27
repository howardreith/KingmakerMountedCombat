# Chunk 7 batch45: native fixture follow-up and preview.64 diagnosis

The standing completion mission remains active. Batch45 declared exactly two
guarded native scenarios on preview.63, package SHA-256
`5054a5633b467d2cd8b077390c2b8e355b2df233210b7dd4d32bf7de9ce3ca5c`,
DLL SHA-256 `e427a006b9b2756acbd358af5c7e402762fa2132cbf5cf2c9a41ef5a31ab1940`,
and append-only suite `chunk7b-completion-b45-20261019` SHA-256
`d3de879acd87ac36b7fdabc69e4a171a0472b346acc9a2b389470e7ac113f73f`.
The private declaration is `group63-native-b45/declared-b45.json`, SHA-256
`bae2e851f8b59fe6da0e0f27986f582c4d4e0bcb27b5888e9cfae279c4e77dac`.
Both scoped WhatIf checks passed before live staging.

| Slot | Original result | Settlement |
| --- | --- | --- |
| `20261019-c7b-world63-b45s1-manticore` | **Outer FAIL**, result SHA-256 `31517c179539095201e94f52014cb5b8e9f7c275935bf62e02bd70ea6a962b1b`. Game **PASS 5/0**, evidence SHA-256 `6ad5abeeba4bdd561188d3f9347a746e53f17212f91b69a86931fc632bb17eff`: native non-capital Manticore transfer, both remounts, four control rebindings and genuine `Manual_305_KMC_C7_AREA_FIXTURE.zks` SHA-256 `68c32129cb01f1fcac8e0dd389ba656d2cd75cf263e88343a847f71590797a12`. The outer profile guard saw a new zero-length native achievement-cache leaf at game exit. This is **not** an accepted area source or area Auto/cold/door result. | Mods, protected saves and Working restored. Exact-run guarded generic profile recovery, snapshot SHA-256 `d990ff924e4b0d33fd6f1fe11687d4b69f77e40eefaa3997d883ad7c98bc1636`, passed; receipt SHA-256 `1db9195015ce0da1f3f602d4bed98b01ee12feb6de899a86ea23431579e1b81f`. Startup Params and PlayerPrefs match their pinned recovery targets; game and lock are absent. Recovery does not promote the original outer FAIL. |
| `20261019-c7b-world63-b45s2-small` | **Outer and game FAIL**, result SHA-256 `0eb01c015ad85a9417a4c7fcf9ac311047cfd4aec942f158c5a754ac232326ab`, game 3 PASS/1 FAIL, evidence SHA-256 `6b48665a0bd366dd61142ee7a120bb863603d5d4d58e2149baf92a02f641cfd3`. Paid Halfling CharGen reached native Total and its commit, but the observed balance did not equal the initial balance minus the earlier quoted 8,000 cost. The actual resulting balance/debit was not captured. No Small Manual, cold admission or profile result followed. | Transaction `restored`: Mods, protected saves, Working and startup settings exact; profile-cache changes zero and `profileBytesRestoredExactly=true`. Game and lock absent. |

The installed `CreateCustomCompanion.RunAction` asks for a cost before CharGen
but its success callback recomputes `GetCustomCompanionCost()` before spending.
`LevelUpController.Commit` applies the level before that callback, and the
installed cost formula depends on `Player.PartyLevel`. This makes a changed
native debit plausible, not proved: neither the actual debit nor party level
at charge is in the original trace. Preview.64 adds a bounded failure event
containing initial balance and quote, actual balance/debit, current native cost,
party level and actor level. The once-only paid assertion still rejects an
unexpected value; it does not change money or grant entitlement.

The preview.64 runtime safety correction gives only a newly created zero-length
native achievement-cache leaf up to ten seconds of read-only settlement after
game exit. It verifies non-cache profile identity, UMM Params, PlayerPrefs,
analytics and all invalid nonzero cache sizes before any retry. It never
restores or whitelists a changed leaf. Focused synthetic profile protection
passed 59/0, including simultaneous zero plus foreign profile/settings drift
and simultaneous zero plus invalid nonzero cache. Independent source/safety
review passed. Fast passed 3/0 (production/component build, source validation
147/0). This is an offline correction; batch45's original outer FAIL
and separate recovery remain unchanged.

Next: commit and guarded-push the reviewed preview.64 source, build one
unchanged package, freeze a new current-state suite,
pass scoped WhatIf and select at most two targeted native scenarios. Retry the
Manticore producer only to qualify its guarded Manual with the settled profile
check. Run Small once to observe the actual native debit; keep the current
assertion until the observation supports a precise correction. Do not claim
area Auto/cold, door, Small calibration, positive DLL-absent cleanup,
progression, measured hardening or final private rehearsal from batch45.
