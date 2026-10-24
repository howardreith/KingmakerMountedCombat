# Batch84 — Small dismissal stopped before native removal

The [standing completion mission](../planning/CHUNK7-COMPLETION-MISSION.md)
remains **IN PROGRESS**. Pushed source `7c6ba455a12d4863937ad3f452258280d23a0026`
was frozen as preview.97 (`c7b-world97`), package SHA-256
`6e351c71d1e40a76ab74fea828114b0c3306d85249f0389c254eb4de2069ebd8`,
DLL SHA-256 `15daa906582dd54082bc59f1d7fecc8e0d184ec2d8142f5b7287625e71311387`,
MVID `f1b57dfa-14a6-4f4d-a3f8-81b1a9187ba5`. The frozen
`chunk7b-completion-b84-20261020` suite has SHA-256
`9dc714795dac00290158adace1869335dbc72fb03bb635fac8a42509d28f8127`.
The three declared selections and original results are retained under
`analysis-cache/chunk7-continuation/batch84-small-dismiss-correction` and
`runtime-evidence`; each dependency-scoped WhatIf passed before live use.

| Declared slot | Native/outer result | Immutable evidence |
| --- | --- | --- |
| `20261020-c7b-world97-b84s1-small-mounted-save` | **PASS 5/0** | Genuine mounted Small Manual SHA-256 `62d0dbb4f90ecf8f6f116cc380a9ad1ba85bc848e5a502537b2550e2d91737aa`; producer artifact `75a2210a85804d0b2b995721c59e48cb9654bee9287f19fe0718f22088f71195`. |
| `20261020-c7b-world97-b84s2-small-mounted-cold` | **PASS 3/0** | Separate cold artifact `9f6edb51ddbc081b87f5aa2b5f72d0783b69eb204f7704dfd12015c5eae4381c`. |
| `20261020-c7b-world97-b84s3-small-native-dismiss` | **FAIL** before native dismissal | Cleanup-pending artifact `b9424fbf9594ae4840aef0d3a90f3a918c01818baf8b010c8c1a5f58030abbc6`; original outer result `611b87b546b89c28eb3084498983b6fd97c4341e1ee9df723f5c9f42b83a6d26`. |

The restored two-pair world and source identity passed. Immediately after the
native `ShowPartySelection` call, the game log recorded a mounted view-activity
invariant failure and `CompanionInvalidated` dismount. The progression cleanup
then refused an undeclared owner and retained custody. The native dismissal
confirmation and `Player.DismissCompanion` were **not reached**; this run does not
verify the reviewed inventory auto-merge correction. The exact invalidating
actor, mode and four view-activity flags were not retained by preview.97's
warning, so the present evidence distinguishes this failure from batches 81–83
but does not justify relaxing the invariant. The installed `ShowPartySelection`
dispatches a group-change event whose `GroupManager.HandleCall` opens the native
selector. A bounded read-only observation at the existing failure boundary is
the next diagnostic step. Do not replay an unchanged dismissal attempt.

The follow-up observation captures the exact rider/mount IDs, mode and four
view flags only when this same predicate fails. The predicate and cleanup are
unchanged. Independent review passed for source SHA-256
`2e5c0578b5efa313847739a66a421d98b9b31abb89288d7ad703fd66c3586af9`,
with focused policy checks 126/0 and Fast 3/0. Native observation is pending.

The timed-out exact process exited after its verified normal close request.
Guarded combined recovery passed WhatIf and actual. The independently reviewed
exact-run `scripts/runtime/Recover-KmcProfileAfterCombinedTimeout84.ps1` passed
WhatIf and actual; its receipt SHA-256 is
`f47ecab3f09a29443bcfe72ee3ab760b001bfff12456f7365bdbded17bca1edb`.
Read-only post-checks confirmed the pre-run live Mods digest
`b592f4009833072b60604852ad00660a6d236957712b63ae40f28ed1b7f67cda`,
protected-save digest
`df434471b6f2bb3ade60ecccaa524aa7537e17f63db3d0bcce6d5a7eb70561ab`,
and exact profile, UMM Params and preferences. There is no game process or
transaction lock. Recovery remains separate from the original FAIL.
