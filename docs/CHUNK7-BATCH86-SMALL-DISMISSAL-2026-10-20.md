# Batch86 — Small native dismissal after selector-scoped view correction

The [standing completion mission](../planning/CHUNK7-COMPLETION-MISSION.md)
remains **IN PROGRESS**. Pushed source `61625d41376d36e3cf6b1a0bdb836e21ba14a047`
was frozen as `c7b-world99` (preview.82 product label). The package SHA-256 is
`aaa32ebcd732b64ba95ac0fb9f9aed5d0c9711ff0929607deca69292a0c249c4`,
manifest SHA-256 `1f46f83fa1167613832ef0a2a62daa72736e91ce002ef619fd08edfbb1010356`,
DLL SHA-256 `9b34860df1017386f1c76dd649addae00776f127c38ef6ed0be2c9f4eee3bbbb`,
and MVID `0fe56b78-0a72-49c3-9830-fbf6d4e2acb9`. Frozen suite
`chunk7b-completion-b86-20261020` has SHA-256
`cf5940f1e5b4c1fb8c37bdc56f72e4ca5b999b9e957cc975430044878edba68b`.
Three native scenarios were declared in
`analysis-cache/chunk7-continuation/batch86-selector-scoped-dismissal/declared-b86.json`
(SHA-256 `7781df4e0d97a1a417d18f8bbf4c275896c758c3713dfb99560e2`).

| Declared slot | Native/outer result | Evidence |
| --- | --- | --- |
| `20261020-c7b-world99-b86s1-small-mounted-save` | **PASS 5/0** | Genuine mounted Small Manual SHA-256 `077ec1d24e78051719cab27c141763452cec1c6ed099d640d27c33868027d75a`; artifact SHA-256 `6a42bb028ed7cd01f44f268eefd68e9630dca6cec8a56bc3e6f4b1bafb0e35e7`. |
| `20261020-c7b-world99-b86s2-small-mounted-cold` | **PASS 3/0** | Separate same-candidate cold artifact SHA-256 `d2002608d9e77c4aff423f8395b73224bfab25d34e8dfd89eebc1383ecd65109`. |
| `20261020-c7b-world99-b86s3-small-native-dismiss` | **FAIL** | Cleanup-pending record SHA-256 `f6cb749bca9db31f7f1fb5e65d9ff3261fe88f200d9abcf6541d620b2b6c748a`; original outer FAIL SHA-256 `d26068618e7f2a5c9ee9269211c0efebeaabf472b64c02398817a15098d24701`. |

Every live selection passed dependency-scoped WhatIf. The original slot-2
WhatIf refused before launch because its copied producer artifact pin still
identified batch85. Its original selection and refusal log remain intact; the
separate corrected selection (SHA-256
`b076b1f65b9d064d41beeebfcbed02e26dd9a0090b6a51897805ff913f221a2b`)
passed WhatIf and was used for the actual slot. No extra native scenario was run.

The selector-scoped view correction got past batch85's view-activity failure.
After `ShowPartySelection`, the mounted invariant instead reported
`Mounted controllability or life-state invariant changed`, then
`CompanionInvalidated` dismounted the relationship. The cleanup guard refused
an undeclared owner and retained custody until the host timed out. The bounded
game-log excerpt has SHA-256
`0424d0ddca5c038668096ba534c89529a818b8886937f2268785418bc0f19851`.
The pending artifact in this binary omits the session stage; the evidence does
not establish whether `Player.DismissCompanion` was entered. This is no native
dismissal or auto-merge-repair PASS. Batch81–85 failures remain separate.

The exact timed-out Kingmaker process was identified by PID/start/executable
and token, accepted a normal close request, and exited. The guarded combined
recovery passed WhatIf and actual, restoring Mods digest
`b592f4009833072b60604852ad00660a6d236957712b63ae40f28ed1b7f67cda`
and protected-save digest
`df434471b6f2bb3ade60ecccaa524aa7537e17f63db3d0bcce6d5a7eb70561ab`.
The independently reviewed exact-run
`scripts/runtime/Recover-KmcProfileAfterCombinedTimeout86.ps1` passed WhatIf
and actual; its receipt SHA-256 is
`bb5881813ebebf830bbdcc555eed079a1b83e5079a327b08b98c9c8f060d6880`.
It restored UMM Params SHA-256
`343997c80ec0d3d9796438beb4c68d0bdd4b3495d99d1816cd8f4b40d6df534b`
and preferences SHA-256
`ae26ad89a144794e03d8af5033c0c4b5103594ff587b2d0c194d49765641e824`.
No game or active transaction lock remains. The original scenario stays FAIL;
the recovery receipt does not change its verdict. Never replay this exact-run
recovery for another transaction.

The next bounded diagnostic candidate records the precise rider/mount
in-game, controllability and life-state flags only when the unchanged mounted
invariant fails. If cleanup retains custody past its existing deadline, it also
captures the bounded progression session and stage in the pending artifact.
These observations were independently reviewed on runtime source SHA-256
`e436a1946097ffa8dfcbe3dc8755c61f7508f7f8d5e5f886c1501546e61573f5`
and scenario source SHA-256
`cea0c3678be7336c8d02e3d0e90855822d249bc2275f017887851626fdac70e2`;
the reviewer found no blocking issue and its pending-reader positive/refusal
checks passed 6/0. A fresh native observation is required before changing
the predicate. Offline `Test-Tiers.ps1 -Tier Candidate -AssemblyTarget Kingmaker`
passed 7/0 in 5.69 minutes on this diagnostic source; it is not native
qualification. Keep F10 dismissal, acquisition/progression/replacement, F11
and the selected private 7E open under the standing mission.
