# Chunk 7 batch43 fixture qualification — engineering in progress

The [standing completion mission](../planning/CHUNK7-COMPLETION-MISSION.md)
still selects 7B, 7C, 7D and readiness-gated private 7E. Batch43 used pushed
source `38318d5705f1fba6394e6cd7c6d919565e45eed1`, preview.61 DLL SHA-256
`35578fa4fc702777613da0e6edc559b67fa76938e3e74992ab3632d791eb2efd`,
package SHA-256
`eaaa303408d09414b660de237c42fcaac3da127a5e4d96f5770968f7a9f47730`,
and append-only suite `chunk7b-completion-b43-20261019` SHA-256
`edbcb1a0454d090df576d712345e3068887a471172b6835bec8869aaa65f97a7`.
Candidate passed 6/0. The committed Manticore reader passed 62/0. Both native
slots ran sequentially under the approved Howie SID, with scoped WhatIf first;
there was never a competing game or Mods transaction. The initial unscoped,
read-only WhatIf was interrupted before completion; its scoped replacement
passed. A live invocation carrying the WhatIf-only audit flag was rejected
before any transaction. The first F09 selection used a UTF-8 BOM and was
rejected at intake; the same JSON object without BOM passed scoped WhatIf.
These pre-native intake mistakes consumed no additional native scenario.

| Declared slot | Original native result | Restoration |
| --- | --- | --- |
| `20261019-c7b-world61-b43s1-manticore`, mounted owner-seed derivative | **FAIL**, game rows 2 PASS/1 FAIL, outer result SHA-256 `e7cab1cee971012e3e3db17a816ab2814b09f73a93303c16fc457c032944f808`. The first native Dismount was accepted once. The first owner became `Detached`, the second owner stayed `Active`, and the fixture's stage-90 assertion incorrectly required immediate retirement. No area transfer or new Manual occurred. | Mods and protected saves restored; no accepted cache exception. The game-exit profile check reported a transient achievement-cache name/size discrepancy, so startup settings and the stale lock were restored through this run's exact snapshot-pinned `Recover-KmcPersistenceProfile.ps1` WhatIf and guarded execution. Recovery receipt SHA-256 `4bce45f06175e61e74a90be01b21257a94fd8cc5bff7540eca344f84964bde54`; game absent and lock released. Original FAIL retained. |
| `20261019-c7b-world61-b43s2-small`, clean owner seed | **FAIL**, game rows 3 PASS/1 FAIL, outer result SHA-256 `6328ed87430870e6922e70ae7b656eac2ec29edbac6c73cf09e1056554ee1d21`. Native Ranger 3→4/Horse completed. The paid companion builder reached race choice, where installed `CharacterBuildController.SetRace` selected the race and then `CharBPhaseRace.SetupDescription` dereferenced absent headless UI. No paid completion, field admission or Manual occurred. | Mods, protected saves, Working and startup profile restored; game absent and lock released without separate recovery. Original FAIL retained. |

The bounded preview.62 correction preserves those failures. The Manticore
fixture now requires exactly one active original survivor after the first
Dismount, tolerating detached custody only until the existing
`ReleaseSettledAdmissions` sweep after the second. It still requires zero
owners, active pairs and managed slots before native `LoadArea`. The Small
builder uses installed `LevelUpController.SelectRace`, checks its Boolean
return and native state/preview, and avoids only the missing description UI.
Installed source shows `CharacterBuildController.SetRace` calls that same
native method before `SetupDescription`. No saved counter, Prepared flag,
native action budget, foreign actor or archive input is fabricated.

Independent read-only review passed for targeted native verification, receipt
SHA-256 `8cfd980528b7e1dd6f96c4b4230b4069fe150f6b66b37b5993b55acd09b03714`.
Fast passed 3/0, source validation 147/0, F09 adapter 60/0 and wiring 38/0.
The unchanged Manticore reader passed 62/0 again, receipt SHA-256
`03ed50606cddba6b5bdf278d10e546510dc4d6bdd022a5fda066df6419e8217f`.
These offline results do not upgrade either
original native FAIL. Next: commit and guarded-push the reviewed correction,
build preview.62 once, run Candidate, freeze a new suite and rerun only the
two affected native scenarios on fresh disposable copies. Then qualify area
Auto/cold and Small calibration from accepted outputs. Positive removal,
progression, 7D and private 7E remain independently selected.
