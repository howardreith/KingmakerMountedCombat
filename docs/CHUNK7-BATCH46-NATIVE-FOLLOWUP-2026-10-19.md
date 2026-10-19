# Chunk 7 batch46: area fixture and native paid-hire diagnosis

The standing completion mission remains active. Batch46 declared exactly two
guarded native scenarios on preview.64, package SHA-256
`84730f105e21c7b4d557a39a47f57e89ea7825f4a8b8c7736542972edfd30d9a`,
DLL SHA-256 `3ae877fc249d9866c1f0f0bf1bc292f1c7da8daa2e4a9c9ff6d94633211b9b1a`,
and append-only suite `chunk7b-completion-b46-20261019` SHA-256
`6ee6ed9283d90264a454d4698a2e85eec8eb00e2f5ad9d27f46d9822f48f092d`.
The private declaration `group64-native-b46/declared-b46.json` has SHA-256
`cc2442e01d09536cc571c08d7137efabc6dd1827179450e44682c8d753955c4f`.
Suite and both scoped scenario WhatIf checks passed. A first attempted live
invocation included a WhatIf-only audit switch and was rejected before staging;
it launched no game and produced no native evidence. The corrected invocation
used the same declared slot and run ID.

| Slot | Original result | Settlement |
| --- | --- | --- |
| `20261019-c7b-world64-b46s1-manticore` | **Outer FAIL**, result SHA-256 `54dd68c8b9ef5fa083f7f055bc947c1c8b4783d145b94bb6d51fd37f78b8393b`. Game **PASS 5/0**, evidence SHA-256 `ca5ae2f62769df3ae46c1686494044fdad0586591f50b118c72f35ac4abd4872`: genuine non-capital Manticore Manual, persistence artifact SHA-256 `a5d8bfb4ac1c42e6f51a462e7028baa7e23f74a1b740ed6e2da6f508bd5aa749`. A newly created zero-length native achievement-cache leaf persisted beyond the preview.64 ten-second read-only exit wait. The original outer FAIL is not an accepted area fixture, Auto/cold or door result. | Mods, Working and protected saves restored. The cache leaf settled independently afterward; read-only cache delta then reported zero changes. Exact-run guarded generic profile recovery passed separately, snapshot SHA-256 `15caf52d60255d3e00c12732cc46589e71f6ff9ec715cb8a9716a1c1de0ea5ab`, receipt SHA-256 `cbdf5df7630180e865c94ef368130effb357d847e6c8544561b017f1e920e3d8`. Startup Params and PlayerPrefs matched their pinned targets. Game and lock absent; transaction `restored`. |
| `20261019-c7b-world64-b46s2-small` | **Outer and game FAIL**, result SHA-256 `a343953580b47a1f77cffb8250809894ecb2b7079c1e9f666c36ec85b4441bef`, game 3 PASS/1 FAIL, evidence SHA-256 `e1edc5cbba45b477c5f388eba57a8d3499694b2abe70166a0a83a90e6355f185`, F09 artifact SHA-256 `ba0564560ff82336769a0c80cafd546a5a70beef00f98f4adaa1acc59a3febb7`. The native paid CharGen committed, then the strict gross-debit guard saw `moneyBefore=400057`, `quotedCost=8000`, `moneyNow=392468`, observed net debit `7589`, current native cost `8000`, party level 4 and actor level 1. No Small Manual/cold/profile result followed. | Mods, Working, protected saves and startup settings exact. Accepted native cache changes were empty and the profile checker reported `profileBytesRestoredExactly=true`. Game and lock absent; transaction `restored`. |

Bounded read-only inspection of the exact installed `Assembly-CSharp.dll`
distinguishes the Small result. `CreateCustomCompanion.RunAction` charges the
current native cost in its success callback. `LevelUpController.Commit` first
calls `SetupNewCharacher`, whose `LevelUpHelper.AddStartingItems` grants the
selected class or replacement archetype starting gold for a non-Endless custom
companion. `Player.SpendMoney` and `GainMoney` subtract and add their arguments
exactly. The observed 411 difference is consistent with this native first-level
grant, while the observed cost did **not** drift. Preview.65 source checks the
actual selected class, campaign rule, first-level starting gold and unchanged
callback cost, and accepts only `initial money + native starting gold - exact
quoted hire cost` once. It does not write money or bypass the paid callback.
Versioned readers require the explicit grant/spend evidence for preview.65;
earlier frozen evidence retains its original reader contract and verdict.

The zero-length native cache leaf also persisted beyond ten seconds and then
settled without mutation. Preview.65 extends the same narrow read-only exit
settlement check to sixty seconds. All other profile/settings/invalid-cache
drift still fails immediately. This is an offline correction until a new
guarded Manticore producer passes the full outer transaction. Preserve the
batch45 and batch46 original FAILs and their separate recovery evidence.

Independent production/safety review found no offline blocker. Focused checks
passed: profile protection 61/0 (including delayed population and delayed
disappearance after the old ten-second window), ordinary-choice reader 51/0,
Manual-derived reader 105/0, Small fixture reader 72/0, and Fast 3/0 with
source validation 147/0. An initial Small synthetic test failed because its
new test case omitted the expected `choices` property; correction and rerun
passed 72/0. No preview.65 native verdict is inferred from these checks.
