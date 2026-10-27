# Batch186 six-pair lifecycle failure and exact recovery

Status: **FAIL**, with separate recovery **PASS**. Engineering remains IN PROGRESS.

One native scenario ran on source `bd6392aef689ceef79345020eec3ef814279d778`:
`20261022-c7b-world186-b186s1-sixpairs-stress`, one load, zero saves. Its
preview.94 / `c7b-world186-sixstress` DLL was
`3ddf697172d32640a7c7f498d4454e81f0c40d6cf8c45f7d19f17749a24cddc4`,
MVID `7575d280-c9d8-4b95-82ba-cac11083052a`; ZIP
`7bc406f421dc7ddaea552f7903521ef75476e96a1823fc962907a85ffc0b7e11`.
Candidate passed 6/0. These offline checks did not predict native success.

The native log records Dismount shell32 failing in cycle3 for rider
`c4e2c853-4b72-4904-ad84-d0b7bfba7470`: the transform lease could not verify
its captured sibling index. Presentation cleanup retained custody. The pose
adapter had already been destroyed, producing a secondary cleanup risk.
No atomic game/world result committed; accepted input log lines do not prove
the scenario's resource, lifecycle or performance assertions. The wrapper
timed out and intentionally left the game and transaction intact. Original
outer result SHA `7370920f22341f12de0adf6a6fbfaa7da047fac375421b61294ca6d3ac3d6cd8`
remains FAIL with restoration false at that time.

The owner quit normally. Under the unchanged Howie SID ending `-1001`, the
existing combined recovery passed WhatIf and live recovery. The separately
reviewed `Recover-KmcProfileAfterCombinedTimeout186.ps1` changes exactly five
run-specific pins from the existing Timeout86 mechanism. Its WhatIf and live
recovery passed; it restored this run's captured UMM Params and only the two
admitted Unity session preference keys. Profile/cache delta was empty.
Recovery receipt `profile-recovery-after-combined.json` records completion at
`2026-10-22T16:24:03.9638849+00:00`. No game or active lock remains.

| Restored surface | Exact identity |
| --- | --- |
| Mods | `b592f4009833072b60604852ad00660a6d236957712b63ae40f28ed1b7f67cda` |
| Protected saves | `df434471b6f2bb3ade60ecccaa524aa7537e17f63db3d0bcce6d5a7eb70561ab` |
| UMM Params | `343997c80ec0d3d9796438beb4c68d0bdd4b3495d99d1816cd8f4b40d6df534b` |
| PlayerPrefs | `ae26ad89a144794e03d8af5033c0c4b5103594ff587b2d0c194d49765641e824` |

Profile bytes restored exactly, zero cache exceptions. This recovery does not
rewrite the original result or qualify six-pair behavior. Private evidence is
under `runtime-evidence/<runId>` and `analysis-cache/chunk7-continuation`;
`b186-stuck-native-log-first4mb-01.txt` SHA
`4beec8f3a6e3f8dead2275fd7256f43854986993f0f55956c2f8b1c84d48c437`
preserves the first failure. The actual domain lease with a list-backed
hierarchy reproduced an unrepresentable captured index with six riders;
one-rider cycles passed. This supports shared-parent clamping but lacks native
parent/count observations to prove the exact cause.

## Next distinguishing observation

The six-case-only `SixPairAttachmentObservation` records actual node/anchor,
captured/current parent IDs, sibling indices, child counts and scene identity
immediately around existing Acquire and Restore calls. It admits exactly six
rider IDs, at most three acquisitions each and 96 records overall, deduplicates
restore retries, and stops at the first failure. Existing log lines preserve
the observation even if host cleanup cannot commit an artifact. No native
objects are retained; default/inactive paths return before Unity reads.
Observer errors cannot replace native calls, results or exceptions. Domain
restoration and pose order are unchanged; this is not a repair.

Actual observer checks passed 15/0; actual six-pair orchestration passed 18/0.
The first observer probe lacked the installed Newtonsoft Unity dependency;
the first orchestration probe misplaced a facade using directive. Both failed
probe logs remain, followed by corrected checks. Independent review required
the inactive integration guard, now applied. Observation ends before host
cleanup, so a new cleanup-only failure would need separate diagnosis.

Declare one next native case (batch187), same immutable qualified Manual308,
same six-case behavior, new frozen observer candidate. It must capture the
missing causal values; an unchanged failure without those values is not useful
qualification. Do not suppress sibling verification, reverse only diagnostic
Dismount order, fabricate resource state or declare the original input out of
scope. Mounted six-pair Manual/cold and the remaining F10/F11/doors/private7E
remain selected after the defect is repaired and reviewed.
