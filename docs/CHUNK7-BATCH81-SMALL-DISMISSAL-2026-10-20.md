# Batch81 — Small native dismissal, original failure and exact recovery

The standing [Chunk 7 completion mission](../planning/CHUNK7-COMPLETION-MISSION.md)
remains **IN PROGRESS**. This batch did not qualify F10 dismissal. The owned
Small/Horse selection and reader route was independently reviewed and pushed as
`cb4e912b36d2a424f62ae367341d84a9d534f384`; focused Small checks passed
4/0 and 11/0, neighboring progression checks passed, Fast 3/0 and Candidate
7/0. Preview.94 is
`KingmakerMountedCombat-0.1.0-chunk7b-preview.82-preview.94-diagnostic.zip`,
SHA-256 `39ba3e06b9f829ee6699ef7c0959fdf02abf513c04b35a8ff4013dee01ed164d`,
DLL SHA-256 `591d4232a24f0010f160367cdd50c3d1f94b187453b4fb590bc2d197003e63e8`.
The batch81 suite snapshot SHA-256 is
`e7dc53ec85e65d341f4ba26273b673eed2abc1ba4f16a0e7374709042e446bce`.
The three declared selections and run IDs are under
`analysis-cache/chunk7-continuation/batch81-small-native-dismissal` in the lab.

| Declared native slot | Original game/outer verdict | Exact evidence |
| --- | --- | --- |
| `20261020-c7b-world94-b81s1-small-mounted-save` | **PASS 5/0** | Genuine two-pair Small Manual `Manual_305_KMC_C7_SMALL_WORLD.zks`, SHA-256 `61ceb175d27eb133d52de40213249ce2cc3cab192fcdde7cf8e5f2269d40545d`; persistence artifact SHA-256 `584f4f427e35a52ade11a5516dc54b644a62feb1c968489497184922fc0ec567`. |
| `20261020-c7b-world94-b81s2-small-mounted-cold` | **PASS 3/0** | Separate cold consumer, persistence artifact SHA-256 `2f3db2f8a568400407746ef9158956dbf661424fa7d08d4de61e2aebb4b98784`. |
| `20261020-c7b-world94-b81s3-small-native-dismiss` | **FAIL**, no terminal game result | First retained cleanup record SHA-256 `5fc25ccf3fdb3c7c2f875b33e220e9ee9bb97c248f41451daf840c07bbd89340` reports `Owned progression field membership changed outside its observed native target lifecycle`. The game kept custody, reached the 900-second outer deadline, and the original outer result SHA-256 is `7ff9569ecb7e299fa114c20751c9520efb355cb1676c6973e13786773aaf9a0c`. |

The slot 3 game process was the exact runner-launched PID 61484, identified by
the request path and transaction token. After the timeout it was closed through
its normal window-close request. The existing guarded combined recovery passed
its WhatIf and restored the exact pre-run live Mods digest
`b592f4009833072b60604852ad00660a6d236957712b63ae40f28ed1b7f67cda`
and protected-save digest
`df434471b6f2bb3ade60ecccaa524aa7537e17f63db3d0bcce6d5a7eb70561ab`.
The profile files already matched their run snapshot; UMM Params and two native
session PlayerPrefs needed an exact one-run recovery. Independently reviewed
`scripts/runtime/Recover-KmcProfileAfterCombinedTimeout81.ps1` passed WhatIf
and actual recovery; its receipt SHA-256 is
`47206ee14dc376335e0ac6453c94576f12571f22606a0abb1071342fa847afa2`.
A subsequent read-only check confirmed profile digest, Params, preferences,
Mods and saves match the run snapshot, with no game process or transaction lock.
The recovery is separate evidence; the original native and outer FAILs remain.

The first failed record lacks the session/probe and raw party roster at the
moment of mismatch. Exact installed native `Player.DismissCompanion` changes
party membership, but the record does not establish whether the observer
completed or whether another field member changed. An initially proposed
Harmony postfix-order change was rejected on independent review because its
test modeled a callback dependency absent from production; that change and
test were removed. The next candidate adds bounded read-only capture at the
first exact roster mismatch and retains the same assertion and cleanup
custody. A new same-candidate producer, separate cold load and one targeted
dismissal are needed to obtain that distinction. No historical PASS transfers
to that changed DLL.
