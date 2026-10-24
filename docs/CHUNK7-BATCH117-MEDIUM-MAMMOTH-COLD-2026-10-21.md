# Batch117 — same-candidate Medium Horse-to-Mammoth cold continuation

The [batch116 producer](CHUNK7-BATCH116-MEDIUM-MAMMOTH-PRODUCER-2026-10-21.md)
could not qualify a cold consumer after the compiled observer changed. Batch117
froze the reviewed exact-rank and unprepared cold contract as pushed
`9353bb3630259ac2c8bb2d345d882ccc525eff6b`, preview.84/world121.
Fast passed 3/0; progression protocol 41/0; immutable Mammoth source 5/0,
Mammoth cold reader 10/0, Small cold reader 17/0; Candidate passed 6/0.
The independent reviewer identified two gaps, rank inflation and unexpected
native Prepared=true on load. Both were corrected and re-reviewed before
candidate freeze. No source archive or original historical FAIL was rewritten.

One diagnostic package `KingmakerMountedCombat-0.1.0-chunk7b-preview.84-c7b-world121-diagnostic.zip`
has SHA-256 `bdf74c267838f178b84051ab032c85c73a2a64a2249a1def1bf8987692ffa0f7`;
its DLL has SHA-256 `75c894806a312dc7c467b16f9dd37da8a08201eba34059dce6e49270fab68bc6`
and MVID `e2c8844a-e412-4cd9-a015-665ee69942af`. Frozen suite
`chunk7b-completion-b117-20261021` has SHA-256
`1cfec6e9fbfd4966966695a37354038106c1624ea1bff897a442be8edf763901`.
The declaration was exactly four dependent native scenarios; each slot passed
its own dependency-scoped guarded WhatIf before launch. One initial slot-1
WhatIf failed purity because a concurrent offline Candidate check removed its
own temporary `runtime-evidence` directory. It launched no game. Candidate
finished, then the slot-1 WhatIf was rerun alone and passed. No native failure
is hidden by that preflight correction.

| Declared slot | Native and outer result | Immutable evidence |
| --- | --- | --- |
| `20261021-c7b-world121-b117s1-medium-mounted` | **PASS 5/0**; genuine two-pair mounted Medium Horse Manual. | `chunk7-persistence-evidence.json` SHA-256 `ae39d84bc5316cf28a571ff044af1a3e21554c1b2288cd5978b951e8b526019e`; Manual SHA-256 `2d1081dfc7b9fa09999fa77d020160bd76c7c3b05bd966408d75455315bd5b0c`. |
| `20261021-c7b-world121-b117s2-medium-mounted-cold` | Separate-process independent-copy **PASS 3/0**, zero writes. | `chunk7-persistence-evidence.json` SHA-256 `34db296fd321e1649bc464b09d7b56545c9ac5016e8b2ae48fd5ded50453a1f5`. |
| `20261021-c7b-world121-b117s3-medium-mammoth-retrain` | Genuine paid native Horse-to-Mammoth respec and Manual **PASS 3/0**; Ranger 10, exactly companion rank 7, Medium rider and Large Mammoth. | `chunk7-progression-evidence.json` SHA-256 `eec2745883afa342d3d2ab5b954dcbd1acaed73b8a06dc90ee1ee84561aac81d`; `Manual_306_KMC_C7_PROGRESSION.zks` SHA-256 `2ec6b200784308dbef4f25a00e7eeed93abb5b7b0fb439222b3853f95da83aba`. |
| `20261021-c7b-world121-b117s4-medium-mammoth-cold` | Separate-process independent-copy **PASS 3/0**, one load and zero writes. Exact producer actor/pet, level 10/6, XP 105,000, rank 7, sourced Mammoth fact, Large upgrade, retired Horse absent and independent mounted control pair persisted. All four actors were unprepared and out of combat at initial frame 3941 and unchanged at frame 3946. | `chunk7-progression-evidence.json` SHA-256 `69ded47ea48604bd099cc4d26639b1a4e89cce987d2faa5253ba33b2c039e09f`. |

The four game processes were distinct. Each guarded transaction reached
`restored`, with actual Mods, Working, protected saves and profile bytes exact;
accepted cache changes were zero. No game process or active lock remained after
slot 4. This is development-scoped audit, not final whole-lab purity.

The genuine replacement/Manual/cold portion of F10 is now **PASS** on this
qualified lineage. It does not demonstrate a mounted Mammoth field session,
general new/existing-character acquisition, all progression/removal paths,
measured F11 behavior, or final-package 7E reuse. The standing engineering
mission remains **IN PROGRESS**.
