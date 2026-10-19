# Chunk 7B–7D resume — IN PROGRESS

## October 19 - preview.50 reviewed source; native retry next

Branch `codex/mounted-combat-chunk7-successor`; last published HEAD
`29661db4f37bbf5158e101fe6bedc5c557400f3d`. Worktree contains the
reviewed preview.50 diagnostic/reader/test/version change and this checkpoint
documentation, awaiting a coherent commit and guarded push. Last frozen package
is preview.49 SHA-256
`dce8ccec3964d7b09064afc5e2fb2ddeb3355d37558fefe159a9aff8a1974cfe`,
suite SHA-256 `4781fcdb46c6067dde015b2cb8cb6e03f52d80944c3b12f41faa4fc1b2266b8a`.
Batch24 original FAIL ledger is
`analysis-cache/chunk7-continuation/owner-seed-batch24/batch24-ledger.json`.
The later TB frame's B rider native JoinCombat/Prepare.Tick/Cooldowns.Clear
cleared 6/3/6 after about 24 ms; its earlier same-frame callback/retry passed.
The game and transaction ended, Mods/protected saves/Working restored, profile
recorded six accepted native cache changes and byte-exact restoration false.

Current preview.50 source is independently reviewed PASS at
`analysis-cache/chunk7-continuation/codex-takeover/group50-disable-review/review04-pass.json`
SHA-256 `2526f755edad4bc1ed393eb56bdccd612c243cf1e32c9bd92d0131d5d621a78e`.
Three earlier review FAIL receipts are preserved. Local focused reader 95/0,
actual-method adapter 49/0, Fast 3/0 with source 147/0; reviewer independently
reproduced 17/0 targeted cases. No preview.50 native result exists. Next commands:
inspect `git diff --check`, commit, guarded-push through
`codex-policy/Push-KingmakerMountedCombat.ps1` with WhatIf first, build one
preview.50 package, Candidate and suite snapshot, then create a fresh one-slot
batch from the qualified owner-seed disable source and run guarded WhatIf before
actual native execution. Preserve all original FAILs and exact artifact pins.
Area Auto/cold, schema2 lawful debt migration, refusal/menu/no-DLL and doors
remain open; PR #5 stays draft. No game or runtime transaction currently runs.

## October 19 - batch23 native results; preview.49 source review checkpoint

Branch `codex/mounted-combat-chunk7-successor`, published HEAD
`dc1c1f63c2a53910a6d58989099ceb831928d467`. Worktree currently dirty
with preview.49 registered-disable diagnostic sequencing, version triad and
current documentation. Preview.48 frozen package SHA-256
`f6ba06d3114a932f2d4a5cff4f3fc9335a8475c22158c7b485a4183b5ea2343e`,
DLL SHA-256 `cae48cb93c39b6bb821e50027a3d89528fb51ece2e5cbee348f8d68baa06d417`,
MVID `6e029a71-76db-4bef-a477-36f0f68abac5`, suite SHA-256
`b1c9f1a2a4000bdb3d7a70f1ae954caba5a4032b8853e77581a0c5602621a5db`.
Private batch23 ledger: `analysis-cache/chunk7-continuation/owner-seed-batch23/batch23-ledger.json`.
Slot1 disable original FAIL, evidence SHA-256
`7b4686e37ba86e289c94dcbef97a1aa064da5e33507813c09f424319b339b646`:
sole failed prelude clause `paused=false` despite a same-frame setter call.
Slot2 three-load PASS, evidence SHA-256
`ace883e6cafaac52e03b846b5a7450b2a62339a9c5edf007446b366521eeae2f`.
Slot3 area Auto original FAIL, evidence SHA-256
`88d0efa1f35d5be775e0994a7f6012708b9385545963203fc88b8f88567f0770`:
effective Oleg destination `IsCapital=true`, zero transfer/save requests.
All wrappers completed; no game or lock remains. Mods/protected saves restored.
Area profile report has six policy-accepted native cache changes and explicitly
reports `profileBytesRestoredExactly=false`.

Installed `Kingmaker.Game.IsPaused` getter tests active Pause mode; setter
queues `StartMode(Pause)`, but `DoStartMode(Pause)` refuses while TB combat is
active. The initial preview.49 wait-for-Pause draft was rejected by independent
review (FAIL receipt SHA-256
`3310dc2e4ed52d2e7a4a007c172a5ca5abac0acb960bd5a007e319f5641705c0`)
and is not the current source. The corrected source uses exact quiescent TB
combat admission, requires no native Pause, and retains exact synchronous and
next-frame debt/clock/round/owner checks. Focused registered reader PASS 65/0;
actual-method disable adapter PASS 48/0; final Fast log
`analysis-cache/chunk7-continuation/codex-takeover/group49/fast03.log`
PASS 3/0, source 147/0. An invalid Fast `-AssemblyTarget` invocation failed
before checks in `fast01.log`; the initial draft's Fast 3/0 is in `fast02.log`.
Corrected-source independent review PASS receipt SHA-256
`dbdfce391a6c52e32bdba44aa608e106e5d675fb8a312962dc3f18e83bab5ff6`.
It flags a genuine native uncertainty: RemoveUnit may allow next-turn time/debt
to advance before the strict later-frame check. No preview.49 native credit
exists. Next: inspect diff, commit/guarded-push,
freeze one preview.49 package/Candidate/suite and retry only the changed disable
case after scoped WhatIf. The capital area destination is not viable for this
two-pair owner-seed case; find an independently eligible visited non-capital
transition before another Auto attempt. Schema-2 TB cold debt requires a lawful
boundary/representable input, not a synthetic resource rewrite. P07, real
refusal/menu, genuine DLL-absent cold and doors remain open. PR #5 draft; no
merge/release/tag/permanent installation/full7E/HUMAN PLAY.

## October 19 - preview.48 source review checkpoint; no live game

Branch `codex/mounted-combat-chunk7-successor`, last published frozen HEAD
`10cbba57237163c90c5a38af49765bb7099d7729`. Worktree intentionally
dirty with preview.48 source/reader/test/doc changes. Preview.47 package
SHA-256 `a76cf9dc50b747ad4deb553faf942d8d22ca3c0abcc3e2538a3773f5c6aedf5f`,
DLL `3e3c9f655de9e07fcb20c55f1d5e898dc4e62aa1b484f8954ecced98cf46ab8b`,
MVID `18b9f342-ef17-4c5f-b26b-2f8b9a68c947`; package build 11/0,
Candidate 6/0, suite snapshot SHA-256
`efc0e743d68b52a7e84897a5f5bff31f8f51481b889f48b493feb240ad1fbf2e`.

Batch22 private ledger `analysis-cache/chunk7-continuation/owner-seed-batch22/batch22-ledger.json`
holds three original FAILs. Slot1 evidence SHA-256
`61bcc4a262d719574da28f81d08bee5d7e635ab34044d256c2c729928150c7cc`:
sole pause predicate false after native pending survivor. Slot2 game artifact
SHA-256 `34f2ef9a9bc78c4008c9408ba4ba79453db1833b13666ec7450b19826dba7425`
passed in game; corrected reader then rejects same-frame rider components in
final settled view. Slot3 pending artifact SHA-256
`1b435f40f9e8c5cf1681f9874532417355b3d434c21af97955e283183c1670e4`:
LoadArea returned at Oleg; pair B failed direct control after pair A restored,
save fence held and no Auto wrote. Original timeout result SHA-256
`19bb2ce9f3add5dfc01913145b5c5900ecd1fdb4284b7986cc545b9994df9366`.
Exact PID 54468 exited normally. Guarded combined recovery restored Mods digest
`b592f4009833072b60604852ad00660a6d236957712b63ae40f28ed1b7f67cda`
and saves digest `df434471b6f2bb3ade60ecccaa524aa7537e17f63db3d0bcce6d5a7eb70561ab`.
Separate profile recovery PASS receipt SHA-256
`a31d8714034130a4e77ac012d1c5616a2a5354161494c5e313fbf16c474be907`.
No Kingmaker process or runtime lock remains.

Preview.48 source and remaining limits are summarized in CURRENT.md. Fast
`group48-fast02.log` PASS 3/0, source 147/0; repeated reader 106/0,
owner-seed repeated 29/0, owned reader 680/0, owner-seed area 48/0,
actual cleanup adapter 99/0, actual area preflight adapter 6/0.
Reader independent review PASS. Initial area/source review found the missing
preflight regression and retains its FAIL; corrected-source review PASS receipt
SHA-256 `0a312558344c60424e9c15a88be4bdc1f3f4500cb7491d0c018fd1011b966da4`.
Next: inspect complete diff; commit and guarded-push; freeze one
preview.48 package/Candidate/suite; declare a bounded native batch. Preserve
all preview.47 FAILs. Schema2 debt and other Directive 4 cases still require
work. PR #5 remains draft; no merge/release/tag/permanent install/full7E/HUMAN PLAY.

## October 19 - preview.46 batch21 settled; preview.47 source IN PROGRESS

Branch `codex/mounted-combat-chunk7-successor`, guarded-pushed frozen HEAD
`40439d09d3cf162126d3c5721eb1f2cbb4ca3d73`; current worktree has an
uncommitted six-file source/reader/test diagnostic diff plus this documentation.
Preview.46 package SHA-256 `3237bd2edc33aa61ce409086bc439abf04733807d1960fbb18f00d824c819571`,
DLL `5adf26e5940fb41b902b464d126640a841dcc2072d8ee959dffdf24b0d913798`,
MVID `d0cb8c99-2f9a-4cf2-9c26-f20459d3d009`; Candidate PASS 7/0 and suite
snapshot SHA-256 `1516171035003db2b366800eae5c38ef0cea97e67623625cdf22bbc1c9e73fc9`.
Private `analysis-cache/chunk7-continuation/owner-seed-batch21/batch21-ledger.json`
records all three selected native runs as FAIL. Slot1 registered disable stopped
at a composite prelude before toggle; Horse removal refusal and pending survivor
had passed. Slot2 native load replaced Player/area/actors and reached counts
8/4/4/1, then the diagnostic wrongly required per-fact Horse subscriber object
identities to survive replacement. Slot3 area AfterEntry Auto timed out before an
Auto write; its pending artifact has no awaiting-ready observation and reports
unadopted destination custody. Exact cause of that native boundary is unknown.
The original runtime results remain FAIL; no changed-byte native PASS exists.

After slot3, the exact game PID exited through a guarded normal window close.
Repository guarded combined recovery restored Mods/saves and protected fixtures.
The exact separate profile recovery receipt at
`runtime-evidence/20261019-c7b-world46-ownerseed-b21s3-areaauto/profile-recovery-after-combined.json`
is PASS, SHA-256 `3b01c60b17c033778bbbfaad40a99107f8c5bfd1d177b2b23d37c5b3f165cdb3`.
It verifies restored Params SHA-256 `1bdebe8e9f97f5b58a3cf0de0cec48f84764e52e54b61e4df74a2cb96e6d8696`,
preferences `ac5553ebbc0a2a106e1c5c792357fe359fb5e7c5fbebf7385d6cf1e79062786f`,
Mods digest `b592f4009833072b60604852ad00660a6d236957712b63ae40f28ed1b7f67cda`,
and saves digest `df434471b6f2bb3ade60ecccaa524aa7537e17f63db3d0bcce6d5a7eb70561ab`.
No game or runtime lock remains.

Current preview.47 diff checks process-owned subscribers stable while requiring
one or two fresh fact-owned Horse subscribers per native reload according to
the admitted archive profile (two for the owner-seed case), and stable ones
across area/settlement. The first independent repeated-world review caught a
legitimate older one-Horse 3/16 route regression; its FAIL receipt is retained.
The repaired generic reader's 104 synthetic cases, owned 59 and owner-seed 29
pass, including stale/new identity negatives. Corrected-source independent
review PASS receipt `group47/repeated-world-independent-review02.json` SHA-256
`61f7f389d8de9fe6f38a28199d710a853c7c38a0293d103089545e4a650b6b94`;
failure-only diagnostic review PASS receipt
`group47/failure-diagnostics-independent-review01.json` SHA-256
`936b96c1ab1bef97add8608055cc63b8dc2203b45e41b6790336b2412cb42c82`.
Disable now emits a per-clause failure snapshot without
changing the prelude decision. Area interruption captures dispatch-return and
native loading/restore counters if the run enters retained cleanup. Fast
build/components/source PASS 3/0 (147/0 source), repeated actual-method adapter
76/0, disable actual-method adapter 47/0, registered reader 64/0 and repeated
protocol 27/0 plus runner/completion 7/0. Corrected-source Fast is
`group47/fast-final02.log`. Incorrect Fast `-Focused` invocation
and missing-parameter direct script invocations failed before checks; retained
as local command errors. Next command: inspect the reviewed complete diff and
scope, commit/guarded-push, freeze one preview.47 candidate, and choose a new bounded native
batch. Do not rerun unchanged area/disable failures. Schema2 debt and remaining
P07/refusal/no-DLL/doors still require diagnosis or native execution. PR #5
stays draft; no merge/release/tag/permanent installation/full7E/HUMAN PLAY.

## October 19 - unfrozen successor repair source; next command

Branch `codex/mounted-combat-chunk7-successor`, frozen published HEAD
`8685fb5f569958d79f0eb78b556ee1116dbdedd7`; current worktree has
uncommitted source/test/docs changes. No game, runtime lock or active transaction.
Preview.45 native verdicts and restoration are recorded immediately below.
The current diff repairs the diagnostic disable combat boundary, routes
repeated-load ordinal 2/3 through the native world-disposal entry, retains
exact `Game.State` across those loads while requiring fresh Player/area/scene
state and actors, and adds a bounded read-only area readiness observation
including the native-loading interval.
The next product version is preview.46. Actual-method adapters PASS 47/0 and
76/0; inherited-epoch 95/0, repeated reader 101/0, owned replay reader 59/0,
owner-seed repeated reader 29/0, and final Fast 3/0 including source 147/0
passed on the complete preview.46 diff. The first incomplete version-triad
Fast run failed source validation 146/1 and remains preserved in `fast02.log`.
The first disable adapter compile FAIL was an
expression-bodied extraction error and remains preserved in `group46`.
Installed native call inventory: `analysis-cache/chunk7-continuation/codex-takeover/group46/native-boundaries46g.txt`.
It confirms Game.LoadGame is in-place; Game.LoadGameFromMainMenu disposes the
prior area and Player but retains `Game.State`; native Prepare.Tick performs
Cooldowns.Clear on newly prepared actors. Exact-hash independent reviews PASS:
disable `group46/disable-independent-review01.json`, repeated/area
`group46/repeated-area-independent-review02.json`, and loading-snapshot
supplement `group46/area-loading-independent-review02.json`. Next: inspect
the full diff, commit/guarded-push, build/freeze preview.46
once and run the scoped affected native cases. Schema2 TB debt remains FAIL;
do not invent action entitlement. No changed-byte native PASS exists. Do not
run native from this unfrozen worktree. PR #5 remains draft;
no merge/release/tag/permanent installation/full7E/HUMAN PLAY.

## October 19 - preview.45 native qualification settled; next repair IN PROGRESS

Branch `codex/mounted-combat-chunk7-successor`, clean guarded-pushed HEAD
`8685fb5f569958d79f0eb78b556ee1116dbdedd7`; PR #5 is draft. Frozen
preview.45 package SHA-256 `3a5a4b6cc14939ec8f9dec0cad0af68cbdb2623db238d3805aa7200ddf24c8f7`,
DLL `bc7c5d1e06671a90ee775b7f189917a902d6eb738a685cef53ccdaa89239a834`.
Candidate passed 7/0; suite snapshot SHA-256
`6dbeca2405392857815780c7eca68327075bd354be1a389da9fd2e6b02096ade`.
No game process or runtime lock remains. Every completed wrapper restored its
exposed state, or exact guarded transaction and separate profile recovery did;
the original timed-out results remain FAIL.

Private ledgers: `analysis-cache/chunk7-continuation/owner-seed-batch19/batch19-ledger.json`
and `owner-seed-batch20/batch20-ledger.json`. Batch19: pending source PASS,
schema2 cold FAIL, schema1 source PASS, schema1 cold/current3 resave PASS;
schema2 third load deferred. Batch20: schema1 third-process cold PASS;
area AfterEntry Auto FAIL/TIMEOUT before autosave, three-load FAIL/TIMEOUT at
second callback, registered disable FAIL before toggle; area cold deferred.
Schema1 migration's three-process chain is complete on preview.45. All prior
failures and dependent unrun cases retain their original statuses.

Exact defects to resolve: schema2 rider A debt restores as 6/3/6, then native
`UnitCombatPrepareController.Tick` changes it to 0/0/0 when saved
`Prepared=false`; area transfer never reaches exact two-owner restoration and
the source-world cleanup pin then refuses the changed area; repeated load's
second callback has semantic/presentation/postload counts 8/4/4 but no native
disposal and retains the prior Player/State/actors; disable's diagnostic prelude
uses exploration-only `Chunk7NativeFixture.Idle` on a valid TB combat boundary.
Do not relax debt, owner, world, or cleanup assertions to obtain PASS. Next:
inspect exact native contracts and failure snapshots, repair the scoped source
and actual-method regressions, independent review, focused tests, then one new
frozen candidate and affected native reruns. Batch18 P07/refusal/no-DLL and
dependent area/schema2 consumers remain selected but unrun. No merge, release,
tag, permanent installation, full7E, or HUMAN PLAY is selected.

## October 19 - preview.45 source composition IN PROGRESS

Branch `codex/mounted-combat-chunk7-successor`, last clean guarded-pushed HEAD
`8a13f7142b80da4405c2c21675c6be246d9c629d`; PR #5 remains draft.
Preview.44 package SHA `309069e337c330bb7590a0a0c620fa984f0368e85775b14dd38bddadca49ed5b`
and Candidate 7/7 remain frozen. No game process or runtime lock is live. The
timed-out batch17 slot4 was recovered by guarded transaction and exact profile
recovery; receipt SHA `c37f4daf2a4f9e3d0e8ae29e06d12367cdef93996bf30c279e14b2f3a3b303b9`.
The original scenario remains FAIL, as do batch16 schema2 cold/schema1 source
and batch17 area prelaunch. Batch16 slot1 pending producer passed.

The preview.45 worktree composition has the 140-slot hotbar observer, area-target
DTO fix, read-only selected native combat-debt diagnostic/external reader,
synthetic schema-2 fixture correction, repeated-world count capture and exact
cleanup-only custody, plus the independently reviewed post-timeout profile
recovery script. Focused applied checks pass 187/0, 29/0, 61/0, 76/0, owned
pair reader 680/0, repeated-world adapter 75/0, owned replay epochs 91/0 and
repeated-world reader 100/0. Prior 679/1 and the first compile failure remain
preserved. Release build and source validation pass after qualifying
`UnityEngine.Time.frameCount` (DLL SHA-256
`bc7c5d1e06671a90ee775b7f189917a902d6eb738a685cef53ccdaa89239a834`).
No game/transaction is active. Next: review composed diff and source payload,
commit and guarded-push; one proper preview.45 package/Candidate and fresh
current-state suite before scoped native reruns. No candidate/native preview.45
credit exists yet. The private runtime resume is
`analysis-cache/chunk7-continuation/codex-takeover/AUTONOMOUS-RESUME-RUNTIME44.md`.
No merge/release/tag/permanent installation/full7E/HUMAN PLAY is selected.

## October 19 - preview.44 selected source connection IN PROGRESS

Reviewed group44 source is applied after closure HEAD `7c1bcadc`; no preview.44
native PASS yet. Focused C#775/0 plus actual505-source compile PASS; merged
PS501/0; required applied assisted236/0. Earlier FAILs are preserved. Exact
scope, private review/check receipts, substitutions and next native dependencies
are in [the active checkpoint](docs/CHUNK7-CODEX-DIRECTIVE4-CHECKPOINT-2026-10-18.md).
No game/transaction is live. Finish the clean source/package review checkpoint,
guarded-push, one proper candidate build/Candidate and suite, then selected
migration/area/disable/refusal/removal batches. Do not stop at this source commit.
No merge/release/tag/permanent installation, full7E or HUMAN PLAY is selected.
Private exact in-progress state: codex-takeover/AUTONOMOUS-RESUME-GROUP44.md.

## October 19 - preview.43 native batches settled; Directive 4 IN PROGRESS

Frozen source `3cc8f0b051d1a5ff15879489722efa2eec8c1f45` is guarded-pushed on
`codex/mounted-combat-chunk7-successor`; PR #5 remains draft. Preview.43 was
built once and reused for Candidate PASS 6/6 (224.1519907 s). Package SHA-256
`9e1b6bdc09c592c8ca7585d7c4137f1bb4b213adb43606d0978064a972d98e3e`,
DLL `bb2a65d3d2afd8e5379a54e0a1db1978145ef2c2ede39ca517a236bd24fa26e4`,
MVID `fd6820e0-d020-4311-a139-add433a38775`.

| Native run suffix | Result | Seconds | Finding |
| --- | --- | ---: | --- |
| b14s1-worker | PASS | 96.4207832 | Selected-A cancel/drain and ordinary replacement completed. |
| b14s2-barrier | PASS | 94.3399028 | Nonselected B unsettled Mount blocked save globally; settled native write passed. |
| b14s3-pendingsave | FAIL | 95.2917790 | Historical writer correctly preserved loaded schema3; source fixture needs current3 write plus metadata-only schema2 derivative. |
| b15s1-schema1save | FAIL | 88.3024618 | Diagnostic observer cast a null unrelated actor hotbar collection before writing. |

Full run prefix: `20261019-c7b-world43-ownerseed-`. All four actual runs have
five restoration flags true, all three transactions restored, exact profile
restoration, no game and no lock. Both failed producers and all earlier FAILs
remain immutable. Their dependent cold loads were never launched. Batches14/15
are FAIL overall; the two P07 slots also remain unrun pending current-format
observer connection. Ledgers: `analysis-cache/chunk7-continuation/owner-seed-batch14/`
and `owner-seed-batch15/`.

Worker evidence SHA `cde1aea0547859f2f32126e413bd5d94254e3f886d59000cb5e2e957d3e8c37d`;
barrier `fb9aa27b1f0d322d54fcd8bb177526d26c94465722f16b71bfe6f645d805cc66`;
pending FAIL `fd954c29ff48518bf75e37ebefd62dc9b564ced7ba94474c0ae9df3fcc7ff135`;
schema1 FAIL `f0b8eb0e02c63d1b8f0579d1c99379d8a4c0494141e9eb2f25fde5b701888b28`.
Accepted native output hashes: worker `7ffac650b55429bb27a6df22d818e8c54496f126441fe12076f9ed587b174572`,
barrier `b17267eefa4b6275e5daa2f1bb2c1df54a5546e67f9b2a85df13067fc34e1176`.

Preview.43 execution freeze is closed. Next source group is IN PROGRESS:
null-aware hotbar identity observation; current pending producer and faithful
historical schema2 derivative; controlled visited-area AfterEntry Auto and its
actual cold load; representative repeated load; registered two-owner disable,
permanent Horse refusal, current positive cleanup and genuine DLL-absent cold;
real native malformed/future refusal and save-menu stability. Private draft
checks do not establish native acceptance. Review the composed source before
one new clean candidate build, Candidate and scoped preflight/native batches.
Do not weaken future-data preservation, load/write quotas or restoration guards.
No merge, release, tag, permanent installation, full7E or HUMAN PLAY is selected.
The prior preview.41 TB and zero save/cold PASS retains its original inputs.

## October 18 — preview.42 batch 13 settled; preview.43 source IN PROGRESS

Published source `b9631449357b2ef191256ded54d21c975c12cd64` on the successor branch.
One preview.42 package build was reused for Candidate **PASS 6/6** in
221.2420399 seconds. Package SHA-256
`286cc78595f59393739c917c86a02db7a3d1d3d9a9331531bac361a6c5c7bc95`,
DLL SHA-256 `11e36f03fd8d58763516ca7291508c4bd367ad1ea47e8f12c67a2904b5de288e`,
MVID `bfb6ec50-babc-4f92-9d2b-c6a4dd070b5c`. Exact private receipts are in
`analysis-cache/chunk7-continuation/owner-seed-batch13/`.

Batch 13 is **FAIL**: three actual runs, one PASS and two preserved FAILs;
slots 3/4 were DEFER — EVIDENCED and never launched.

- `20261018-c7b-world42-ownerseed-b13s1-worker`: FAIL, 90.8167054 s, game PASS.
  The restored-start driver left all six field actors selected. The strict
  selected-A native-save evidence contract rejected it. Add normal Select(A)
  after world/authority admission, with a regression; preserve this FAIL.
- `20261018-c7b-world42-ownerseed-b13s2-barrier`: FAIL, 90.4350347 s, game PASS.
  Normal B Dismount retired B and its live slot bindings, but four recognized
  hotbar templates remained. Aggregate facts 6→4 cannot independently exclude
  an active B fact masked by an absent A fact. Independent review requires a
  per-actor/per-kind native fact inventory and fresh binary/native retry.
- `20261018-c7b-world42-ownerseed-b13s5-inventory`: PASS, 84.6206638 s,
  evidence SHA `5a339ee104ccf2aa8062501d53b760816e1b9af59681121bfc090e10683966a3`.
  Native ToIndoor/ToOutdoor both resolve to the current SpiderCave area, not a
  second area. MapExit resolves GlobalMapArea; all three use BeforeExit.
  This was read-only discovery, with zero travel/interaction/save requests.

All actual runs have five restoration flags true, all three transaction phases
restored, profile bytes restored exactly, and no remaining game or lock. Failed
outputs are not accepted cold sources. Native barriers/worker/migration are still
unaccepted. The original preview.41 TB and zero save/cold PASS results retain
their original inputs.

Preview.43 source checks are now PASS on the affected inputs: C# authority431,
worker adapter82, preparation/facts39, current-parent guards56, schema1/schema2
guards29/26, actual fixture bind-mode48, source147; PS owned reader680,
source247, boundary218, schema1 reader54 and actual derivative input14, all
with zero failures in their final runs. Affected product compile03 PASS in
26.6666074 s. Exact commands, durations, hashes and retained failures are in
`codex-takeover/group43/`; C# summary03 SHA
`ef98c536fda77d7c2ad300af4884540938adb6d7be1df249b3bb1239ef1417ee`.

Independent review found and closed the actual schema1-current fixture-mode
guard omission, external result-row registration and source-schema evidence
gaps. A generated in-memory projection of failed worker evidence also exposed
compact owner IDs versus native hyphenated actor IDs; the reader now uses the
existing strict native identity comparator. The original worker FAIL stays
unchanged, and its synthetic projection has no native credit. Preparation may
retain four templates; completed save/cold boundaries still require exactly
their saved live slots. The final review receipt is
`owner-seed-review/d4-group43-review01.json` and is required before qualification.

Private batches14/15 declare worker/barrier retries and both migration chains
(historical input, native schema3 resave, separate third-process actual schema3
load). Existing positive P07 cleanup is not runnable unchanged: its diagnostic
driver still reads the legacy schema1/2 format after the current schema3 writer.
Those two removal slots are unrun DEFER - EVIDENCED pending a bounded current
observer/reader connection; no Working incompatibility is inferred. Area route
preparation must respect actual GlobalMap cleanup policy. No native run or
Candidate packaging is claimed by these source checks.

Next coherent group (preview.43) is IN PROGRESS: worker selection setup, exact
control-fact evidence, schema1 source/historical migration/native resave and
both schema1/schema2 third-process loads of actual schema3 native output. Root
is sole native executor. Review, focused checks, one clean candidate build,
Candidate and scoped WhatIf precede fresh bounded native batches. Area transfer,
repeated lifecycle, registered disable/permanent Horse refusal, positive cleanup
and genuine no-DLL load, native malformed/future refusal and opportunistic doors
remain required. No merge/release/permanent installation/full7E/HUMAN PLAY.

## Resume here — Codex takeover, Directive 4, October 18

This section supersedes the active chronology below. Branch:
`codex/mounted-combat-chunk7-successor`, active root
`C:\Dev\KingmakerMountedCombatLab\repo\KingmakerMountedCombat`.
Recorded HEAD before this reader/document checkpoint:
`6652c7fe1e8adb0edb61c28f520275bf5f708b65`, published through the original guarded
push helper. Unknown worktrees/history were preserved; the old successor worktree
is detached at exactly that source and is now the frozen preview.41 source.

No game, lock or native transaction is live. Batch 12 has two actual runs:
1. `20261018-c7b-world41-ownerseed-b12s1-tbload` **PASS**, 104.4654715 s;
   evidence SHA `2b21042ef0984e53a7da3af114c01c048ad4d01cbfb35bf2e7fc4293e3fe1f8e`.
   Accepted batch-11 producer reused, exact saved completed boundary 2.286 retained
   at both restoration phases; native continuation and exact restoration passed.
2. `20261018-c7b-world41-ownerseed-b12s2-zerosave` **FAIL**, 115.7495532 s;
   evidence SHA `5f1c81c90bb237c6365470a4dc5c29ee127e83d6c20df2a21e5fe60c59702178`.
   Game PASS, reader phase error: B was Active during its attack, its own Horse's
   approach move rose 0.0990104 to 0.7453083, and native Dismount came afterward.
   Mods/saves/profile restored exactly. Do not admit this failed producer as input.

Claude's product fix `f1e54002` has independent review PASS
(`owner-seed-review/d4-noncurrent-boundary-review01.json`) and accepted native
coverage above. Preview.41 product source stays 6652c7fe, DLL SHA
`2fdce155accf089e30d59970cf5862a92d8423ccb9e5086921c751f781cc74d2`, MVID
`daf5ccc5-3a74-40e6-9563-8e8fa28daa4f`, package SHA
`291d8fb4747d1f1e307a68c259a0d6631e5ecb37909ab8fa0c3bb7f9ed252709`.
Candidate 6/6 passed in 388.1997642 s. Original batch-12 runs use the original
external-root world41 suite; preserve it. New root suite
`chunk7b-world41-codex-20261018` SHA
`49fc6fb093b219cf7f71ec1092297260736e764fd9719423a6eaf64907be2b22` protects the
actual current installation. Product freeze
`owner-seed-batch12/preview41-product-freeze-codex.json` SHA
`4f632f87bbd0b65464e2c3a3d4e84584dffd351a18a5cc149f79504f9cd30dd8` was validated
through `Get-KmcFrozenProductSource`; no product rebuild is needed for reader work.

Current reader delta is only the two combat-persistence PS1 files plus these records.
Focused04 passed 126/0 in 29.7798737 seconds. Independent reader reviews 01–03
remain FAIL; review04 is PASS, bound to reader SHA
`1811b3a15aeb300c9e296528e6ea5baeca762cd41f87aa9795566fb725092d71` and test SHA
`0e8653b383b3283069e40b0c83d4009ee7dd335c473892188dec657884021082`.
The revision closes scalar typing, role formatting and same/cross-role owner conflicts.

**Next command/work:** inspect `git diff --check`, commit the reviewed reader/doc delta,
and generate a new zero producer launcher using the unchanged preview.41 package,
new root suite and the explicit three frozen-source arguments. Keep old launchers
and FAIL evidence. Declare the producer rerun and dependent cold load before use,
at most five declared scenarios in batch 12. Each launch needs its own scoped
WhatIf and owner no-deployment notice; never edit input source during a live run.
Only a terminal-PASS producer can seed the separate-process cold load.

Then continue the selected pending survivor/save-barrier/worker, area/Auto,
disable/removal and schema-1/2 source connections and native checks. Existing
owner-seed checkpoint support still lists only Quick, Auto, RT/TB debt and zero;
do not merely select an unconnected case. For one-pair source cases, investigate
the already-authorized native exploration Dismount of B and prove no B owner remains.
Keep Horse-reference removal refusal; positive DLL-absent needs a reference-free
fixture. Preserve the mission's later gates and the original failed ledgers.

Use only the original `C:\Dev\KingmakerMountedCombatLab\codex-policy\Push-KingmakerMountedCombat.ps1`
from the dedicated root. Auto review rejected the copied helper twice, including
after owner approval; moving the clean successor branch into this root resolved the
tooling restriction without changing the approved helper or weakening its guards.

## History — earlier resume (preserved below)

## Resume here (October 18, 2026)

**Governing instructions:** the [owner directives](docs/CHUNK7-OWNER-DIRECTIVES-2026-10-16.md) (Directive 3 is the active assignment) and [CURRENT](CURRENT.md#current-state-october-17-2026). The October 14 stop recorded below is superseded: development is resumed under those directives.

1. **Refresh state.** Check the worktrees, actual refs, `runtime-state` transactions and locks, and that no Kingmaker or UMM process is running. Preserve unknown edits, frozen candidates and separate worktrees.
2. **Product and harness.** This branch is `codex/mounted-combat-chunk7-successor` (draft PR #5).
   - Build once per committed head with `scripts/Package.ps1 -ArtifactQualifier c7b-world<N>`.
   - Create a fresh qualification suite.
   - The launcher passes `-Confirm:$false` on native runs.
   - The lab helper `analysis-cache/chunk7-continuation/owner-seed-batch02/Prepare-Candidate.ps1` does all three.
3. **Before each native run:**
   - run the scoped WhatIf (`-DevelopmentScopedAudit -WhatIf`);
   - launch detached;
   - tell the owner first. The whole `Mods` tree, UMM `Params.xml`, the profile and the save root are under a no-deployment window until restoration is verified.
4. **Seed.** Use only `KMC_C7_NO_MODS_OWNER_SEED` (SHA-256 `3405d7c8…821b924f`), as fresh ordinary copies, never the master and never a hard link. Do not use the superseded `KMC_C7_OWNER_SEED`.
5. **Directive 3 work is complete on the owner-seed route** as of October 18. That covers the behaviour tests, the not-ready family gate, the Manual save and its separate-process cold load, and RT plus TB rider-first and mount-first combat, all accepted on preview.34 (`bba4edde`). See [owner-seed evidence](docs/CHUNK7-OWNER-SEED-EVIDENCE.md). The batch ledgers 01–07 and review receipts are in the lab under `analysis-cache/chunk7-continuation/owner-seed-*`.
   - **Open product debt** ([known product debt](docs/CHUNK7-KNOWN-PRODUCT-DEBT.md)):
     - the RT party-brain proposal warning spam;
     - the superseded Chunk 6A R3 probe row;
     - the recorded combat fixtures' forfeit shape.
   - **Next candidates need owner direction:** acquisition, Quick/Auto/area/removal/migration persistence breadth, doors, Small calibration, stress, human review.
   - **Helper:** `Prepare-Candidate.ps1` lives in each batch folder. The latest copy is in `owner-seed-batch06`.
6. **Guarded push.**
   - The root branch uses `C:\Dev\KingmakerMountedCombatLab\codex-policy\Push-KingmakerMountedCombat.ps1`.
   - This branch uses an unmodified copy of that script whose only change is the repository root, pointed at `worktrees/chunk7-successor`. Verify the copy against the policy script before reuse.
   - Never force-push. Never merge, release or install for human play.

## History — October 14 stop and earlier (preserved as written)

Stopped at the owner’s October14 request to conserve quota and push. Do not restart audit/native work without a resume instruction. Root is sole integrator/live executor. Branch `codex/mounted-combat-chunk7-preview10-harness`; product branch `codex/mounted-combat-chunk7-assisted-fixtures`; origin `https://github.com/howardreith/KingmakerMountedCombat.git`. Refresh actual HEAD/status and preserve all other/frozen worktrees. No game, runtime transaction, lock, sentinel or test session remains.

## Active stopping point

[Claude takeover handoff](docs/CLAUDE-HANDOFF-CHUNK7-2026-10-14.md) supplies the reading order, exact refs, inactive draft limitations and first implementation/native prerequisites. Preparing that document does not resume runtime work.

[Complete checkpoint, exact receipts and next action](docs/CHUNK7-OWNER-STOP-2026-10-14.md). Product remains published0e1ce2d92a368866ce0803d070a6f3b7b96bd971, tree59fc88acd78006a8ea83aeeefc21dcc9e2833243. Candidate03 passed6/6 in214.1538249s on harnessb8db761b; preview10 was built once and remains frozen. Preserve `worktrees/chunk7b-preview10-0e1ce2d`; no rebuild. Historical Candidate01/02 and native1–9 failures are unchanged.

The full WhatIf actualaf0706/provider33591 was stopped incomplete; original session closed actualb80444 exit-1. Immediate stop-helper FAILe53dad and separate absence proof4bc3b3 remain retained. No native10, completed purity audit or native admission. Read-only current continuity actual17ed96/provider64083 to e56cd1 exit0 passed17.4560884s: Mods/save bytes and metadata match original suite, protected fixtures exact, no game/lock/sentinel. Receiptf1901ad322b43ee976439cbfc5808f0fb98e470f078d56695e4808bc28abfb45. Approved synthetic archive check actualf7ccae/provider27577 to7f8c7f exit0 passed19/0 in18.8470646s; no installed/protected archive reads.

Unfinished [schema1/area Auto patch backups](planning/drafts/chunk7-owner-stop-2026-10-14/README.md) preserve20/21 targets without integration, compile or execution. Both independent drafts base40157 and overlap. Their exact private maps are `owned-schema1-next01/integration-map01.json` SHA912451cfcfab594464ea37965b9a098ea3c2938e083f821142e8ca7025f24bb4 and `owned-area-autosave-next01/integration-map01.json` SHAda62f4eaa4d2480fff7dc45d7d1449872d4bce65ee18e8a7f51d78623860877d under analysis-cache/chunk7-continuation. Not integration-ready; missing guard/compiler/reader closure and boundary entry/registration are documented.

Next after owner resume: inspect frozen drafts and reconcile overlaps privately before integration/focused execution. Before native10 on unchanged preview10, create a fresh numbered concrete audit/inventories, independent review, full guarded WhatIf and actual caller/after-input proof; never resume or promote incomplete audit01. Only a successful assisted producer with exact restoration admits cold inventory then ordinary paid Medium acquisition. Nine of25 native attempts used, all final FAIL. Breadth candidate remains not ready. Final stopping-commit/push identity is in the private `owner-stop-publication-20261014-01/publication01.json` receipt.

## Integrated source checkpoint (historical)

This checkpoint contains assisted producer90 followed by cold58: 109 composed source/test paths, three preview10 identity files and documentation. Private maps under `analysis-cache/chunk7-continuation`:

- Producer `assisted-integration-next01/integration-map01.json`: `72db48be25f7056b2a3b66d11f3a5571342774b8cb946262dd9e112b45ddb4df`; apply `7958b4ff2faad64a8599158bf40eacb48de2cae25fb1fb58b69f2ddc122742c4`.
- Cold host `assisted-cold-inventory-host-next01/integration-map01.json`: `d983a3615c9f8cf381effc626ebf28487e87e0f905a2bd0aecb09304af4e89e2`.
- Cold reader `assisted-cold-inventory-next01/integration-map01.json`: `c8779822230cae19323917c7322d7571097bf44ff2e4f472020e5039cfa2648a`; combined cold apply `a89c62c1890986a64780cd6b9f0d80e429e86a32230f1cba17bc58c4c6b08f08`.

Do not reapply the maps. Independent current109 and historical262 review is `894730ddd5711d103a2e85fdc17ff338b68083a670085fa5647a341c44b8450d`. This checkpoint is committed and guarded-pushed at `ee2bdfc9a69482af0870ead5566597e49cd0dbff`, tree `18e6604c0b2566a0e41203168fda0d2b10a2479d`; remote verified exact. Publication receipt `assisted-cold-integration-next01/publication01.json` SHA `ca154f3e003dccd263c7dd7ba25976ea01fb0f65a60e2c945a7670a084fe85ef`. Root refreshed this clean state on October 14. No package or native10 transaction has started.

The reviewed startup13 and consumer34 changes are now integrated into45 unique source/test paths, with explicit additive merges of the project and aggregate child registrations. Apply receipt `startup-consumer-integration-next01/apply01.json` SHA `a83ae9a4f3cd2524d4ead51abb0f80c2f7152e326c9c3860fa030b659eeece60`. Do not reapply maps. Independent composition review PASS: `startup-consumer-integration-next01/independent-review-r2/composition45-review01.json` SHA `0d8f2a839f8dac7661ee15e22e3f31e6be01a0fcb5385b8ceefd61e2db6f266d`. All45 canonical hashes match; both exact additive merges preserve all old entries (488 product Compile entries,171 aggregate children).

Startup final13 map SHA `f9850aeebe9aacf5130951d98019b558bb0c08f5ffb7403f73fd59512be3c08b`, review `355341b040f56721ad70a6d4b9341108fe6815fbbd1b78479f9de750ee8f2c43`: separate44/0,45/0,50/0 and installed-contract compile487/13 passed. It passively samples first10 UMM callbacks with actual Unity frames/monotonic timing and focus flags; no action/deadline changes. Historical enclosing failures remain FAIL.

Consumer final34 map `assisted-acquisition-connection-next01/integration-map01.json` SHA `1b5577847c9f3bb492297ab955e999e6c44fe482a23852a8df69c795d43bc0bf`; independent source/hash/results review `70866a4fbd6ff941d1b7a31c6e769f95f44f1caa848bd4748c5ca25deb75aa0b`; root corrected PS review `2a937624d10e89d6938abcd123d2463bda8c4d63dce725fe0448ddf01c4c52f8`. All three review findings are closed: main-XP mapping23/1→24/0, admitted Small custody35/4→39/0, cold profile-restoration qualification10/13→23/0. Request/authority171/0 and separate reader37+76+101+69 passed156.8253654s. Actual affected portable projection24/prior39/wiring17 passed24.0126328s,1377 pins unchanged; its original enclosing vector remains FAIL. Supervisor wiring33 includes nested checks and is not the actual17 result. Installed-contract compile487/13 passed22.4s. See the support document for substitutions and retained failures.

The optional `assisted-producer-and-cold` source mode requalifies completed same-candidate producer/cold lineage and exact profile restoration, then uses the immutable producer Manual for ordinary cumulative acquisition. Legacy source guards remain. The separate Small source route retains both owners, but the compiled Small catalogue remains EMPTY. This source was subsequently published and packaged as preview10; use the active preview10 checkpoint above for the current Candidate correction.

The original next step was `scripts/Package.ps1 -ArtifactQualifier c7b-world10` once from the clean source commit; that build is now complete as the active checkpoint records. Run `scripts/Test-Tiers.ps1 -Tier Candidate -AssemblyTarget Kingmaker -SkipBuild` on that same build. Its six stages include components, source, harness, paired activation, artifact manifest and Kingmaker contracts. Do not add the broad diagnostic aggregate or unchanged Fast. Freeze package/source/DLL/MVID/suite; source corrections retain failed candidates and select affected reruns.

Prepare actual 16-field assisted selection and owner authority from the source contract. Native preparation requires fresh full WhatIf/caller proof and independent admission review. Root alone executes. Only a fully qualified same-candidate final Manual with exact producer restoration may seed cold inventory (one load/zero writes). Preserve existing timeouts, failed evidence and protected saves.

## Actual focused checks

Producer build actual04d9d8 exit0: Release product/components/observer, source147/0, 8.7355366 seconds. Intermediate DLL `5e298b2ef2b48dc5db5df5bdb5ae10cc673d46fd3adaf272d309b0ba5d8651c3`, MVID `c800509f-a23f-4a2a-a5ca-d5ab726472e4`; before cold and preview10 identity, not a candidate.

Reader actual66a30c / provider66324 CLOSED / 06b7c4 exit0: six stages 5+72+108+40+31+6 = 262/0 in 102.875060 seconds. `analysis-cache/c7af-integrated01/reader/receipt.json` SHA `9b67eee3d0d4253d17cfedc01105ca9bd238b5a92b39557469f9ad6d7db2bb47`. Original outer reporting-only hash typo remains with separate correction; it was not an execution input/guard. All producer evidence keeps its before-cold bytes and0290 provenance.

Cold reviews `4c886dccb0edd731c200bce59aeaa64168a54747e5342437faf9e15a95461e68` and `198368281001638337573b98758c1672815d6a617e31e3fd5d23f76c6f66b4b5` verify final53+5. Private compile486/13 passed. The original nine-stage vector02 stays FAIL from a zero-assertion argument-fixture failure; its eight successful stages passed654. Separate affected comparison03 passed5/0 in3.3706467 seconds, with its failed immediate executor pin precheck explicitly retained. Actual scenario74-to-reader100 passed with declared native facades. [Details](docs/CHUNK7-ASSISTED-FIXTURE-SUPPORT.md).

## Settled native9 and Git continuity

Run `20261013-chunk7b-world09-working-observer`: source `3262eb02969a39c387a66986eb4f75d6c8fdb6d9`, harness `029089ae07c47308849e21b366bfd96788004927`. Full WhatIf passed9027.385412 seconds/1374 unchanged pins. Original native supervisor failed668.517396 seconds while game ran. Late game failed1045.791095 seconds at host callback10 before any load/save request, then self-exited. No manual close/kill/input.

Recovery actualc1f0d3 / provider20892 CLOSED / 1a39f3 exit0:21.7858148 seconds, five flags true, errors empty, profile/settings exact, cache0, dependencies unchanged, lock released. Receipt `76c90ddf4a5b160ed19fb26e048d0b5a0cc9b553e028e1dd4134f0d0e7989aff`; independent closeout `10637bb626c1d4eabdaa8000128d378166921750f56d3af6af6ed958eb11c1c8`. Root postcheck03:00:26.6742831Z found game/lock/sentinel absent. Original FAIL/afterPinsVerified=false unchanged. [Exact digests](docs/CHUNK7-NATIVE-OBSERVATIONS-2026-10-12.md).

Nine of25 native attempts used, all final FAIL. Preview9 package `artifacts/KingmakerMountedCombat-0.1.0-chunk7b-preview.9-c7b-world09-diagnostic.zip`, SHA `cb8eb2a014694eb3a027a146f510f626fd9e5e175739c551a4fbd3a00f7367c5`; DLL `4be2f2e5a40c29eebd9e1fdc54e8cebf1f157abd35baf19a3f061ee02c2ebd30`, MVID `4fefa97a-3f1b-40c2-87fc-9f173b7ba3e4`. Keep frozen artifacts and earlier failed history unchanged.

Fetch observed external foundation029089→92c8daf4c3efc5cf7f6fc36b21798e02cd66b165. Both have tree `1c15365fc9039db2829e3c268ef4a77819db9bac`; empty diff verified. New review branch starts at92c8, carrying known edits. Local foundation029089 and all frozen/other worktrees remain. No reset/rebase/merge/force-push. Independent continuity note `9dcf603e643325764171bc7b4a205223eee7c3c8afbfcb461f21aa02eda296de`.

## Remaining mission

Native2/offline6/retained12; writer3 and explicit1/2 adapters; decode grants no restoration authority. Medium Horse/Mammoth; Small catalogue EMPTY. RT Charge default-off; TB Charge refusal and defensive feat deferred. RT/TB both-order actions/debt, native Manual/Quick/Auto/cold/area/removal/migration, ordinary new/existing progression/acquisition/replacement/retraining, Small pose, six-pair stress, matched0/1/2/6 and unmounted/diagnostics-off controls remain mandatory. Owner authorized a disposable campaign; no fixture/archive permission question pending. No7E, merge, release, permanent installation or HUMAN PLAY acceptance. Work is stopped at the owner’s request.
