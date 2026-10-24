# Claude handoff after batch187 — October 22, 2026

Status: **IN PROGRESS**. This is an owner-requested transfer of execution,
not Chunk7 completion. No native run is active. No merge, release, tag,
permanent human installation or HUMAN PLAY acceptance occurred.

## Assignment and current checkout

Continue the [standing completion mission](../planning/CHUNK7-COMPLETION-MISSION.md)
through **CHUNK 7 ENGINEERING COMPLETE — OWNER ACCEPTANCE PENDING**. All remaining
7B/Directive4, 7C, 7D and readiness-gated private7E remain selected. Read its
[original scope/matrices](../planning/CHUNK7-ORIGINAL-SCOPE-AND-MATRICES.md),
current AGENTS, [validation policy](VALIDATION-POLICY.md), CURRENT and the active
AUTONOMOUS-RESUME section. Do not restart historical discriminator, observer,
acquisition or prologue work merely because an older document says “next.”

- Lab: `C:\Dev\KingmakerMountedCombatLab`.
- Actual active checkout: `repo\KingmakerMountedCombat` beneath that lab.
- Branch: `codex/mounted-combat-chunk7-successor`.
- Existing draft [PR #5](https://github.com/howardreith/KingmakerMountedCombat/pull/5).
- Last executed/frozen source: `94093cec68b251d1d263dadaa6afb9b111c94c8f`.
  The handoff commit is its descendant; discover current HEAD rather than reset.
- `worktrees\chunk7-successor` is an old detached checkout at `6652c7fe`;
  it is **not** the current successor branch. Preserve all other worktrees.
- Private handoff directory: `handoffs\KMC_Claude_Chunk7_20261022_B187`.
  Its `publication.json` records the final handoff commit/remote after push.
  Its `settled-state-readback.json` records the read-only current-state check.

The owner's latest constraint: avoid tests that require manual intervention
before project review; the owner is often away. **Do not launch another native
six-pair attempt until a reviewed, tested, bounded unattended terminal-FAIL exit
is ready, as well as the causal attachment correction.** No blanket permission
to kill a game, abandon a transaction or falsify cleanup is implied.

## What batch187 proved, and what it did not

One declared native scenario ran, one load and zero saves:
`20261022-c7b-world187-b187s1-attachment`, owned-family case
`six-horse-cold-fixture-lifecycle-stress`. The new bounded observer was the
distinguishing observation after batch186; no unchanged discriminator was replayed.

The original native behavior remains **FAIL**. No atomic game/world completion
artifact committed. The outer result is separately **FAIL** with
`Kingmaker process attribution changed during the run.` Do not equate that outer
error with the causal gameplay defect, or rewrite it as PASS.

The exact hierarchy trace establishes shared-parent sibling-index drift:

- All six rider views retained the same original parent instance `-93932`.
- The parent had 16 children before mounting and 10 after six detachments.
- Rider B `c4e2c853-4b72-4904-ad84-d0b7bfba7470` captured indices 7, 10 and 13
  across cycles 1, 2 and 3.
- Third-cycle row63: restore requested captured index13 with 11 parent children.
  Row64: parent remained identical, now 12 children, rider at index11; the
  requested index13 was unrepresentable while four other riders were absent.
- Unity clamped the index; the existing exact verification correctly failed.
  Repeated sequential capture/restore shifted the shared ordering.

The unchanged actual domain lease reproduces the exact cycle3/rider1 failure
offline using first-acquire indices `{0,7,5,4,8,9}` and ten anonymous other
siblings. One-rider/three-cycle control passes. This is an offline native-layout
reconstruction, not native qualification or a product fix.

The runtime also destroys its pose adapter before lease restoration succeeds.
An attachment failure therefore leaves the next cleanup retry with a retained
lease but a destroyed pose component. This neighboring retry defect needs repair.

## Accepted small correction at this handoff

Batch187 exposed a second failure in cleanup **evidence recording**:
`JObject.FromObject(cleanupAttempts)` threw `Object serialized to Array. JObject
instance expected.` under installed Json.NET dictionary conversion. This
prevented the cleanup-pending artifact from being published.

The handoff source constructs an explicit JObject and assigns each integer
counter. It preserves first-failure reporting, repeat counts, independent
failures, false return and successful retries. It does not alter native cleanup,
resource state, sibling verification, process exit or PASS criteria.

Independent review: CLEAR, restricted to this change and its two focused files.
`scripts/chunk7/Test-Chunk7WorldCleanupJson.ps1` extracts the actual TryCleanup
method, uses installed Newtonsoft, reproduces the prior converter failure and
passes **7/0**. Release build and Fast pass **3/0** (849 components, 147 source
checks), 0.69 minutes. The first transcript captured the command and interval
but omitted child stdout. A bounded repeat with **SkipBuild** captured Fast2/0
and JSON7/0 (0.1 minutes for Fast) in
`analysis-cache\chunk7-continuation\b187-claude-handoff-test-capture-02.log`.
The original `b187-claude-handoff-focused-01.log` remains intact.
No new diagnostic ZIP, suite, preview or native run was created for this fix.
Local build outputs differ from the frozen batch187 package and are unqualified.

## Unfinished unattended-exit draft — preserved, not active source

Private `UNREVIEWED-failure-exit-draft` contains full source snapshots, a tracked
diff, and SHA256 manifest from immediately before the owner requested transfer.
It is **uncompiled, untested and unreviewed implementation**, not accepted code.
Only its design seam had review. Its host hook, csproj entry, new partial and
world-engine hash/scope changes were removed from active source after copying;
the reviewed JSON correction remains. Do not bulk-overwrite current files with
these snapshots. The saved diff also includes the now-accepted JSON change.

Design intent: after normal `DrainChunk7Completion` cannot settle, only the
exact disposable six-pair, one-load/zero-write case may publish an honest atomic
terminal FAIL and call normal `Application.Quit()` once. Require current request,
token, fixture/world identity, actual failed row, committed hashed causal artifact,
elapsed drain deadline and independently proven load/write/serialization
quiescence. Preserve retained volatile cleanup as FAIL; never set the normal gate
Settled, fabricate cleanup, invoke successful completion or kill the process.
The outer runner must still restore and independently report every surface.

Critical unfinished details:

1. Existing `RetainCleanup` latches `cleanupPendingReported` **before** its writer.
   The draft moves that latch after durable publication/hash and deduplicates
   its failure message. Test writer failure, partial publication/hash failure,
   retry and immutable artifact identity; do not assume this is already repaired.
2. Compile against exact installed types and check every guard, actual counters,
   command-line identity, no active/queued load, worker/scope/fence/pending write,
   serialization, deadline, stale/missing artifact and changed world.
3. Test publication before exit, publication failure => no exit, changed facts
   before exit => no exit, exactly one quit, and unchanged ordinary completion.
   The draft currently declines any preexisting result file, including one
   written immediately before a failed recheck; review that fail-closed state.
4. Existing `Test-Chunk7HostCompletion.ps1` and host boundary templates provide
   actual-method test infrastructure. The legacy WorldCleanupAdapter template
   also lacks `sixAttachmentObservation` needed by the already-committed six
   release method; update its external facade if selecting that test. It was
   not rerun as part of this JSON-only checkpoint.
5. Obtain independent implementation/safety review and focused positive/negative
   results before any live use. The draft is useful work in progress, not proof
   that unattended failures can yet settle without the owner.

## Next causal product correction

No sibling-slot correction has been implemented. Relevant files:

- `Domain/ScopedTransformAttachmentLease.cs` — injected exact capture/restore.
- `Integration/KingmakerMountedPairRuntime.cs` — acquire, pose teardown,
  attachment restoration, inherited replacement and residue/reference gates.
- `Diagnostics/SixPairAttachmentObservation.cs` and the SixPair engine partial —
  existing bounded observer and selected lifecycle; do not reverse only test
  Dismount order or weaken assertions to manufacture a pass.

Reviewed design direction, still needing implementation review: keep an inert,
owned sibling-slot reservation at each rider's vacated position. Capture fully,
own the reservation before mutation, insert it immediately after the rider and
then detach the rider. During restore, keep the marker until the original
parent/index/pose verifies; remove it and verify again. Handle root transforms
in the exact original scene. Retain idempotent custody through partial creation,
movement, restoration or release failures. Do not write unrelated actors.

Delay pose-adapter destruction until attachment plus slot restoration succeeds;
keep Deconfigure/baseline verification before detachment. Include marker custody
in every no-residue/reference-release/cleanup gate. Explicitly handle inherited
replacement: `ReleaseInheritedReplacement` currently inserts a replacement at
the original index before old-view restoration, so a second view can move the
reservation. Scene/view teardown and third-party marker destruction must have
honest, reviewed failure/retry behavior.

Tests needed: exact observed six-rider layout for three cycles and changed
mount/dismount orders; one/two-pair controls; unrelated siblings; null-parent
scene roots; partial acquire/restore/release retries; replacement and area/view
teardown; pose retry and all residue gates. Preserve exact sibling assertions.
After correction and unattended-exit review, freeze one new candidate and run
the affected native lifecycle, then genuine six-mounted Manual and independent
cold continuation. Batch187 already supplied the causal observation; do not
repeat it unchanged. The next batch number is 188 if no later work exists.

## Fixture and evidence identities

The current fixture is **unmounted** six-Horse Manual308, produced by batch184
and cold-qualified by batch185. It does not certify mounted persistence.

`runtime-staging/persistence-20261022-c7b-world185-b185s1-sixthhorse-cold/Saved Games/Manual_308_KMC_C7_SIX_HORSE_WORLD.zks`

SHA256 `b1e817a60c8bbe35b5947b4eab3b3f35060493ca20873d5de5fdf8a81efbc291`,
length 1,332,308; last-write ticks `639282712706531806`.
Batch185 sixteen-input settled pinset:
`b185-settled-evidence-pins.json`, SHA256
`7d5c4d140e5c9f875ee8a7b11c104e369afc8dee49f34263b53c25ee82ec7fac`.
The reviewed `Chunk7SixPairCheckpointSource.ps1` gate proves this exact source
without recursively replaying the acquisition ancestry. Preserve remote Linzi
and all six sourced reciprocal Horse facts. Ordinary copies only, no hard links.

Batch187 identities:

| Input | Identity |
| --- | --- |
| Version / qualifier | `0.1.0-chunk7b-preview.94` / `c7b-world187-attachment` |
| ZIP SHA256 | `527c8cd051f0e17742c03db969c06df97bea7506db1447cbf902a613aeed8d8e` |
| DLL SHA256 | `780987bd22fb8a016fb4e4bd9fa5bb33a089fa428ecd2d2bef1b3f93fbeba796` |
| DLL MVID | `55f41b96-f335-4728-8679-bb7cf34f18ac` |
| Package manifest | `e25601802c0dee03a23a5c161d6c46949acd6b194e290bc390cc553a8f31b631` |
| Suite | `chunk7b-completion-b187-attachment-20261022` |
| Suite SHA256 | `b7689358055ffe3d56bfc8ece265ce81e970f1fb164ba45282f48ea68c95f33e` |
| Selection SHA256 | `6d064b7063523cb2ee679a4edea5efb2ed156faa1012363174f6a8f60ed1060c` |

Private evidence paths beneath `analysis-cache/chunk7-continuation`:

- `b187-attachment-native-first4mb-01.txt`: immutable 4MiB native snapshot,
  SHA256 `73e22ee8c36ba5ad5285e3aded2c614c712d54515d94b7bba0541d71789d4d16`.
- `b187-attachment-native-diagnostic-01.json`: original process attribution and
  64 parsed observer rows. The observer intentionally stops at first failure.
- `b187-native-layout-repro.cs`, its compile log and `-01.log`: actual domain
  lease, bounded list-backed native clamp reconstruction; exact FAIL/control.
- `b187-cleanup-serialization-failure-01.txt`: nine-line native stack excerpt,
  SHA256 `4b6a48ceebc41381e5c8ba595b10c4c30b24892615c4360aca5458b9bf095404`.
- `b187-attachment-current-execution.md`: contemporaneous chronology.
- `b186-next-six-mounted-persistence-plan.md`: source map for the distinct
  mounted save/cold route; design only, not implementation or native authority.

Original result, restoration and isolation records remain under
`runtime-evidence/20261022-c7b-world187-b187s1-attachment`; transaction state is
under `runtime-state/{run-transactions,transactions,save-transactions}`.
The private handoff evidence manifest pins a bounded set of these inputs.

## Restored environment; do not run old recovery

Owner normally closed PID48080 (started 16:37:57.5506209Z), without saving.
The existing supervised wrapper then exited1 and restored in finally; it did
not need a recovery script. All transaction phases are restored; original FAIL
is unchanged. Read-only verification at **2026-10-22T17:08:28.5801116Z** found no
Kingmaker process/active lock and matched actual current surfaces to this run's
own snapshot:

| Surface | Restored SHA256/digest |
| --- | --- |
| Mods | `b592f4009833072b60604852ad00660a6d236957712b63ae40f28ed1b7f67cda` |
| Protected saves | `df434471b6f2bb3ade60ecccaa524aa7537e17f63db3d0bcce6d5a7eb70561ab` |
| Profile excluding saves/output log | `fe9788aed9525b702760ed123cad00c100d258f9cd24f5e42fe87dc226975ab0` |
| UMM Params | `343997c80ec0d3d9796438beb4c68d0bdd4b3495d99d1816cd8f4b40d6df534b` |
| PlayerPrefs | `ae26ad89a144794e03d8af5033c0c4b5103594ff587b2d0c194d49765641e824` |

Profile bytes restored exactly; zero accepted cache exceptions. This is not a
claim that runtime output_log is restored. Installed dependency receipt reports
unchanged digest `db2c3901c5158e48ff0c1ed720734857355b0f6560e5a5c36e285ba2c0498921`.
Batch186's original failure and separate completed combined/profile recovery
remain independent; never rerun its one-off recovery (nor batch34's old one).

Keep approved Howie context, SID
`S-1-5-21-1108991106-2476425705-1721024090-1001`. No new identity/security changes.
Future transactions snapshot actual current installation; these hashes are
evidence, not future restoration targets. One live executor, no competing Mods
or profile staging, batches of at most five honestly declared scenarios.

## Remaining mission beyond this defect

Use the existing requirement-to-evidence table in the standing mission.
Pending-only schema2 batches41–42, area Auto/cold60–61 and positive cleanup/
genuine DLL-absent65/67 have development evidence; do not restart them on intake.
They still need applicable final-package reuse/qualification decisions.

- F09: six-pair lifecycle and mounted Manual/cold remain open. Small default male
  Halfling/Horse and Medium Horse/Mammoth have development evidence, not final
  package acceptance. The conditional six-pair wording remains intact; the
  current admitted six-Horse fixture makes this concrete, not a setup exclusion.
- F10: many paid acquisition, later-level, retrain, dismissal/replacement and
  Manual/cold paths pass development qualification. Broader ordinary new-character
  and general acquisition/progression paths remain open; disclose assisted setup.
- F11: use retained batch156 allocation-event and batch157 same-DLL observer-off
  evidence. Request-free normal-player controls/diagnostics-off and final
  performance acceptance remain open. Do not equate heap with allocation rate or
  reopen unsuccessful Mono/Recorder work without a new reason.
- Doors remain unqualified. Spider/Manticore/Thorn River inspected routes offered
  no eligible door; Tomb had only an ineligible hidden door. Do not retry capital
  Oleg or fabricate visited flags. Investigate supported bounded setup without
  making owner navigation a routine prerequisite.
- F12/private7E: when implementation/review gates are ready, freeze one exact
  package and execute mapped FULL/native final matrix, temporary fresh install,
  upgrade/rollback/removal and representative RT/TB sessions. Earlier preview
  PASSes are development evidence unless exact reuse rules match. HUMAN PLAY
  stays pending; mandatory red engineering gates cannot hide there.

Retain explicit TB Charge/other feature deferrals already documented. Continue
the selected mission after each passing check or pushed checkpoint. Publish only
through `codex-policy\Push-KingmakerMountedCombat.ps1` (WhatIf then guarded live
push, clean worktree); update draft PR5 factually. No force-push or main merge.
