# Batch69 F09 neighboring-pair combat-entry diagnosis

The standing [Chunk 7 completion mission](../planning/CHUNK7-COMPLETION-MISSION.md)
remains in progress. This is one declared native scenario, not Small profile
qualification. Batch68's original FAIL and its separate recovery evidence remain
unchanged.

Run `20261020-c7b-world84-b69s1-small-drift` used the unchanged batch53 F09
Manual, the same exact F09 selection, preview.84 package SHA-256
`cfaedc6758cd1aaf280ddb47e35297007a04e9162621065584ba9acb07f8d1cc`,
DLL SHA-256 `700e33e7ba78c9e4fc5e971f00847edc87ce808ff921057dded2aefa26e5107e`,
and frozen qualification suite SHA-256
`0709fb58e50fa1878b82404a038bddc8598f4ec2c3a7eede5fa4813258304331`.
The dependency-scoped guarded WhatIf passed before launch. The original
[runtime result](../../../runtime-evidence/20261020-c7b-world84-b69s1-small-drift/runtime-result.json)
is **FAIL**, SHA-256
`4db82cdee7328aad3505881882a8d70a3cdc57542a7a5ab2e525cb208a5038f2`:
four subscenarios passed and runtime/cleanup failed the strict neighboring
snapshot check at calibration stage 3. No mounted Small pose or representative
attack passed.

The read-only [native observation](../../../runtime-evidence/20261020-c7b-world84-b69s1-small-drift/chunk7-small-calibration-evidence.json)
pins the first change to frame 4636, immediately after the scenario spawned its
owned combat target. The neighboring main/Horse pair retained exact actor,
descriptor, view, sourced feature, rank and reciprocal custody. Both had
Standard/Move/Swift 0/0/0, one reaction and `Prepared=false`, initiative 0
before the encounter. Both were in combat and `Prepared=true` afterwards;
initiative became 7 and 5. Every other captured resource stayed equal. The
allocation trace recorded the native combat clear of these same actors during
failed-run cleanup in the next frame. The change is consistent with ordinary
native combat entry; no KMC action grant is evidenced. This is a diagnosis of
the observer's cross-phase comparison, not permission to ignore neighboring
resource changes.

The reviewed correction admits only this bounded post-target combat-entry
transition: both neighbors must be in combat and prepared, the target must be
active and in combat, neighbor commands must be empty, initiative must move
from zero to a positive value, and all non-preparation/non-initiative resource
fields and static custody must stay exact. It records the original and admitted
snapshots. Its first admission is restricted to stage 3 and that exact
snapshot remains frozen until native combat exit. The external reader requires
the witness, rejects any neighboring action-budget change, and requires final
resources to match the cold baseline. Batch69's original FAIL remains FAIL;
the correction needs a new native verification before claiming Small behavior.
Independent review passed after four reader false positives were corrected.
The focused synthetic F09 reader passed 25/0, the neighboring assisted
connection passed 265/0 after stale test-only scaffolding was updated, and
offline Candidate passed 7/0. The build had no warnings/errors.

The guarded transaction ended at `restored`: Mods and protected saves match
their captured digests, the baseline and Working were restored, and the
profile-cache result reports byte-exact profile restoration with no accepted
cache changes. No game process or transaction lock remained after this run.
