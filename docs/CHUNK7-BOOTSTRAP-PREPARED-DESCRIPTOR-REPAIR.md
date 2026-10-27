# Native bootstrap descriptor repair — source PASS, native proof pending

The native SaveRoutine keeps the requested SaveInfo and creates a distinct Type/Name copy before PrepareSave. Preview 6's observer incorrectly required those objects to be identical, and its completion scenario inspected the original request. Native attempt 5 remains FAIL; its output cannot seed cold inventory or Manual saving.

The observer now retains requested and prepared objects separately. It records preparation arguments and each guard result before refusal. Admission still requires the exact game, player, manager, authored new-game callback sequence and one Auto request. Completion requires the exact LastCompletedSave.Requested and Written references, sequence, path, registered prepared descriptor, session/generation authority and sole authorized native write. Matching values alone cannot establish completion. Native action/resource and callback/worker cleanup ownership are unchanged.

The strict bootstrap reader checks the distinct descriptor labels, copied name, preparation guards and exact completion identities. Existing cold-inventory, Manual-seed and identity-graph readers were checked against the changed evidence. Ten existing files changed; no project registration or wrapper expansion was needed.

| Focused check | Result | Measured duration |
| --- | --- | --- |
| Original actual-method reproduction | 2 PASS / 3 FAIL; actual exit 1 retained | 1.8642516 s host wait |
| Repaired actual probe and completion predicate | 56 PASS / 0 FAIL | 0.8014374 s host wait |
| Admission/cleanup adapter | 72 PASS / 0 FAIL | 0.8012579 s host wait |
| Bootstrap reader | 132 PASS / 0 FAIL | 3.6214428 s host wait |
| Cold-inventory reader | 151 PASS / 0 FAIL | 9.4166186 s host wait |
| Manual-seed reader | 70 PASS / 0 FAIL | 10.0144380 s combined host waits; process stopwatch unavailable |
| Identity graph | 84 PASS / 0 FAIL | 4.6010680 s host wait |
| Installed-contract product compilation | 464 sources, exit 0 | 1.9437029 s compiler stopwatch |

Final focused assertions total 565. The original 130/2 reader run used an older synthetic null request name; that input and failure remain preserved. Corrected fixtures model the observed native Type/Name copy. Actual-method tests substitute engine objects and external observers; JSON tests execute the real strict readers. These checks provide offline evidence only.

Private evidence is under analysis-cache/chunk7-continuation/bootstrap-prepared-repair-next. The immutable integration map is 14a18a7c13ee4844e4a913fee670ff39c42a313d3e231a646739e7237c0eb631; verification is 4824b586be8cd25b673d3eafb6d0114f4059b091a0f30d85943f8dc6d0858dcd. Independent source/closure review passed. Root applied all ten exact base/payload pins on 4dfbe18db0aeb33baca13109b57de14cd95f8096; receipt 0496c7790fe9c59cdf3e779b5781299a6c8d53aa783ce257e49995c1217f622a verifies all 464 resulting production files equal the successful compiler inputs. No redundant compilation was needed at application.

Next native dependency: freeze and qualify a changed candidate, obtain its fresh reviewed full WhatIf admission, then rerun the affected authored bootstrap (zero loads, one isolated Auto). Only an actual PASS with exact candidate identities and restoration may authorize the subsequent cold inventory and Manual seed. Native budget remains 5/25; no additional native attempt occurred during this repair. The [sustained mission](../planning/CHUNK7-CONTINUATION-MISSION.md), [source breadth checkpoint](CHUNK7-PROFILE-PROGRESSION-INTAKE-CHECKPOINT.md) and [failed native/restoration record](CHUNK7-NATIVE-OBSERVATIONS-2026-10-12.md) remain distinct. The breadth candidate is not ready.
