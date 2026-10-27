# Chunk 7 batch125: clean owner-seed F10 source inventory

Status: **native read-only source inventory PASS; acquisition and separate cold
consumer remain unqualified.** The [standing completion mission](../planning/CHUNK7-COMPLETION-MISSION.md)
continues. This was one declared native scenario, not a restart of the
historical prologue, a progression save, or F10 acquisition proof.

The reviewed preview.91 source is `ed40ef959c1a79f745568f014a30bfd0dbde8df4`.
Candidate passed 7/0 in 4.4 minutes after the focused Manual-derived producer
reader 112/0, acquired cold reader 147/0, source readers 30/0 each and their
affected contract neighbors passed. The clean-source package is
`KingmakerMountedCombat-0.1.0-chunk7b-preview.91-c7b-f10-ownerseed-acquisition-diagnostic.zip`,
SHA-256 `d3bf0d9caf58e43bd15b858a8b11ed72d35d24ac48201ec34a5a45a486807bae`;
DLL SHA-256 `974197d0fb655a228acdad82c1cfd65955403df4e06e88eb03b7200765c14537`,
MVID `70833b73-da58-4be8-8fe1-2d352dd81d23`.

The append-only suite `chunk7b-completion-f10-ownerseed-20261021` has SHA-256
`db91d29c6cd4fe0307537507bc84cfe316886f798bc4a9d5baa78bb69a84a678`.
Its WhatIf and the scenario's dependency-scoped WhatIf passed. The immutable
batch declaration and BOM-free selection are under
`analysis-cache/chunk7-continuation/b125-f10-ownerseed-cold`; selection SHA-256
`2e5800c5b07f4d87f66c515f557341ca02a6b4ebb93f6b1342bcf05a2732790e`.
The two first suite invocation errors (a mistyped pin and PowerShell `-File`
switch syntax) occurred before a snapshot or launch; the corrected WhatIf and
actual suite each passed. No unrecorded native scenario was hidden in them.

`20261021-c7b-world91-b125s1-ownerseed-cold` passed native and outer checks
**4/0**, with one load of a fresh disposable copy of the registered
`KMC_C7_NO_MODS_OWNER_SEED` master and zero writes. Runtime result SHA-256 is
`dd96e0c1c2f334ecffe8fcf3300efec32f48f55eb2736a69af7349a1cec3107f`;
game result `ae9040bfc1adb41ef2473782e0dc6649bb682206c1bd033d7c1a9c8fcaac77b5`;
strict cold artifact `84a61f7b31a498bef822cee45fd883c3c0ce772c87ed1373ae73e61e68d2cead`.
The native main is the pet-free Human Ranger
`73681ef2-b281-4bc6-bc05-2de68c1616cb`, level 3 at 9,000 XP and eligible
for a native level-4 choice. The four-character original party has no pets;
native money is 400,057, current hire cost 8,000, XP rate 100, and
`OnlyOneSave=false`. These are observed fixture facts, not resource grants.

Transaction `20261021-c7b-world91-b125s1-ownerseed-cold` reached `restored`,
SHA-256 `bb0426d791cdfc49988f9b46c3ff73fd08a0e5d516732e27aa975ba837c5d07a`.
Mods, Working, protected saves, and base-game dependencies passed restoration;
profile bytes restored exactly with zero accepted cache exceptions. The game
process and active transaction lock are absent. This ran as Howie,
SID `S-1-5-21-1108991106-2476425705-1721024090-1001`.

The reviewed Manual-derived producer still requires a same-candidate
`chunk7-bootstrap-manual-seed` artifact. That ancestry would require the old
native new-game/prologue bootstrap, whose retained attempts failed. Batch125
supplies an admitted, reference-free owner-seed source with exact current native
readiness, but its artifact is honestly an assisted cold inventory, not a
Manual-seed producer. The next implementation must add a closed owner-seed
source mode and retain separate artifact, archive, actor, save and reader
identities. It must not relabel the cold artifact, replay the prologue, fabricate
XP/Prepared, or loosen the selected preservation checks. After focused review,
build a new candidate and run the actual existing Ranger acquisition, genuine
field Manual, and independent acquired cold consumer on that candidate.

The guarded push helper's `-WhatIf` invocation was rejected by automatic
approval review before it ran (`approval required by policy; AskForApproval is
Never`). The local source commit and package are intact; no branch push was
attempted through another path. Draft PR #5 therefore still reflects the
previous remote checkpoint until the publication boundary is resolved.
