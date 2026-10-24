# Chunk 7 Small mounted cold readiness — batches 75–77

Status: **PASS on preview.90 for the affected mounted Manual and separate cold
consumer**. This is development evidence, not final-package reuse or owner
visual acceptance. The original batch74, batch75 and batch76 failures remain
FAIL on their own inputs.

Batch74 preview.88 saved a genuine mounted Small world, but its cold consumer
timed out after four semantic restores and before presentation. Preview.89 added
one bounded read-only deadline observation. Batch75's native producer passed in
the game but its outer result failed on one newly created native achievement
cache leaf. The exact-run guarded recovery accepted only that pinned leaf,
restored settings, Mods and protected saves, and released the lock; its receipt
is `runtime-evidence/20261020-c7b-world89-b75s1-small-mounted-save/profile-recovery-exact-b75s1.json`.
Its `profileBytesRestoredExactly` is **false** because the single cache change
remains; it is not a blanket profile equality claim.

Batch76 preview.89 used a fresh mounted Manual producer, game/outer **PASS 5/0**
(`20261020-c7b-world89-b76s1-small-mounted-save`, evidence SHA-256
`26fbc02c348eaf1f665d465693c73a5129edc95a04b9b3aabaa81751b2b76281`).
Its separate cold consumer, `20261020-c7b-world89-b76s2-small-mounted-cold`,
remains original game/outer **FAIL**. At the 35-second stage-0 deadline, its
read-only observation reported `all-native-actor-state-restored`, four semantic
restores, zero presentation restores and the exact blocking feedback:
`Self referencing loop detected for property 'Normalized' ... Path
'AvatarLocalScale.Normalized.Normalized'`. Its evidence SHA-256 is
`2d9e7de0c0fb78ddcacd86031759f39b774adb646327a897f926589fcba9fd8d`.
Both batch76 transactions restored profile bytes, Mods and protected saves
exactly, with no surviving game process or lock.

The cause was the transient Small view-identity probe serializing the whole
`SmallRiderBodyObservation` through Json.NET. `PoseVector3.Normalized` returns
another vector, so generic property traversal recurred. Commit `c67beb33`
projects the observation's 20 fields explicitly, including scale X/Y/Z and
ordered equipment slots, without walking computed vector getters. It changes
only the readiness identity; native calls, reference and hierarchy checks,
LateUpdate observation, profile admission and restoration remain intact.
Independent review passed, the compiled focused regression passed **8/0**,
additional independent field/culture/null checks passed **27/0**, Fast passed
**3/0**, and the offline Candidate tier passed **7/0**. No prior FAIL was
reclassified.

Preview.90 is package SHA-256
`e132044eb5d86fb7f734b2a6c69e337315e935dc7bada1794bd56d001c6cd53c`,
manifest SHA-256
`8eca0746278178d034af61a2987ab8245066ca142194c9d48046c6ea4e629b16`,
DLL SHA-256 `15777338a2d17849b51700e2ee5b4d58a2b39a1932429f6b1a09362147edff9d`,
MVID `2a126d49-728d-44e5-9580-18fe7dfa52bc`, source commit
`c67beb339584678d1f434504517f2cad22be23fc`. The fresh qualification suite
`chunk7b-completion-b77-20261020` is SHA-256
`75bd24617f81fab8b030639a08523cf8a032928f20c7a1fb90a3ced6a485733f`.
Batch77 declared two scenarios before live use. A live invocation containing a
WhatIf-only audit switch was refused before launch; a cold selection with a BOM
was likewise refused before transaction and corrected without changing the
selected fixture or product.

| Batch77 native scenario | Game / outer verdict | Evidence and restoration |
| --- | --- | --- |
| `20261020-c7b-world90-b77s1-small-mounted-save` | **PASS / PASS, 5/0** | Genuine mounted `Manual_305_KMC_C7_SMALL_WORLD.zks`, SHA-256 `19297ab1ece407b650d31f64bd13a6b2dd42eebc652a299a23e39b27486e14a6`; persistence evidence SHA-256 `8f42b418fee656549cbcf218a41880748944b6c33ca299ef11c8276533ed4882`. |
| `20261020-c7b-world90-b77s2-small-mounted-cold` | **PASS / PASS, 3/0** | Separate process and independent copy of slot 1; `all-pair-presentation-restored`; persistence evidence SHA-256 `7620c298c91f33f5bc456a91c4beef2a7ccb66ef4758a13605d58cc682e16f3f`. |

Both batch77 transactions reported Mods and protected saves restored, exact
profile bytes with zero accepted cache exceptions, and no game process remained.
The declared selections and scenario list are under
`analysis-cache/chunk7-continuation/batch77-small-readiness-fix/`.

Remaining mission gates include F10 native acquisition/progression/removal,
ordinary door qualification, F11 repeated and allocation-sensitive measurement,
conditional six-pair stress and the selected exact-package 7E private rehearsal.
The standing [completion mission](../planning/CHUNK7-COMPLETION-MISSION.md)
continues; preview.90 is not the engineering closeout.
