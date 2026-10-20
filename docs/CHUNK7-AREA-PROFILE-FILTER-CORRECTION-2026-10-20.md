# Chunk 7 area fixture: batch48 profile comparison correction

The original batch48 Manticore producer remains an **outer FAIL** even though
its native game result was PASS. Its Areas restoration stopped on what appeared
to be a new empty achievement-cache leaf (`achievements.dat` plus U+FFFD and
`@`; `beforeExact=0`, `afterExact=1`). Exact-run recovery separately restored
the profile. The original result, native result and recovery identities remain
in [the batch48 record](CHUNK7-BATCH48-NATIVE-DISCRIMINATORS-2026-10-20.md).

The cause is in `Assert-KmcPersistenceProfileOutsideAreasUnchanged`, before
Areas settlement. Its filter wrote `[string]$_.path.StartsWith(...)`.
PowerShell cast the **Boolean result** to the nonempty string `False` or
`True`; either is truthy under `-or`. Consequently, the filter removed every
pre-run profile entry. The exact UTF-8 batch48 intake snapshot contains the
reported empty leaf. Replaying its outside-Areas filter against the snapshot
retains 0 of 480 entries before the correction, and all 480 afterward,
including that exact leaf. The current read-only profile inventory also has
the leaf. This explains batch48's false `beforeExact=0`; it does not promote
the original transaction to PASS or prove the old game's output on new bytes.

The one-expression correction casts the **path** before `StartsWith`. It
changes neither the closed selection of routes allowed to isolate Areas nor
the cache policy. A preexisting empty leaf remains recognized; a genuinely
new empty leaf still refuses settlement; a new settled 12,288-byte leaf is
admitted under the existing recorded-change rule. The focused Areas-isolation
test passes 29/0 and profile-protection test 62/0. Independent review passed.

The next area fixture producer must use a freshly declared native scenario,
scoped WhatIf and the guarded Howie executor. Its own original outer result
must PASS before the Manual is admitted for area Auto/cold and doors. Reuse a
frozen product package only under its exact source/package/suite rules; this
PowerShell safety fix alone does not change the DLL.
