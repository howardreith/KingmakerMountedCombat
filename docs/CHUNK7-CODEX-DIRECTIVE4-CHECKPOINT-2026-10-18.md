# Chunk 7 Directive 4 Codex checkpoint

## October 19 - batch32 P07 prerequisite FAIL; safe recovery complete

The successor reader checkpoint `7348dace0242ea11f6ad75fc24ad08d0ec29b86c`
was repackaged without changing its preview.52 DLL (SHA-256
`18f619bd28e872b73470944aaf46671887d91d99ff26f80f30a8fd50019a1433`).
Main package SHA-256
`e87bed650a2d216217e5f38788facb84331179d12bcdbf46125f5571782e28b8`;
removal observer package SHA-256
`f1cad1de85d727497768e2493380e133fa5bd11f452f20088a47f438c01837ba`;
suite SHA-256 `92c53c00f9606a542c570eb42478f246a0d7bc367dc0692ab2c9076ebdd0e7eb`.
Candidate 7/0, main package 11/0, observer package 7/0; suite and scoped
WhatIf passed. The batch has two declared P07 attempts:

| Slot | Native result | Restoration |
| --- | --- | --- |
| b32s1 `prepare-removal` | FAIL before game launch: sandbox denied guarded Mods move and rollback | Exact original Mods/Working restored by the project's recovery script; no game or lock remained |
| b32s2 `prepare-removal` | FAIL: one Working load, zero writes; native `SaveManager.LoadRoutine` threw `Sequence contains no matching element` before callback | Combined Mods/saves restored; exact reviewed profile recovery restored UMM parameters and three native preferences, then released lock |

Slot1 lasted 24.36 s; its original runtime-result SHA-256 is
`cafecfb722f858470e8fffeb8c9062dc70daa2070180e4e697543e8e7afd0980`.
Slot2 lasted 41.14 s; its original runtime-result SHA-256 is
`24e8d258ea1f7b7fd1b129217fb4b0a1f5383da4c3b8e0e70709551223867640`
and game-result SHA-256 is
`7e4a7ef2a3859e8583963281b0e8aef5270bbc722002b56e41118e3503f2f91b`.
The exact profile-recovery receipt SHA-256 is
`c17e15a0d34aed31372af4c91822e3179ddec036b42fb2763a0679492681546e`.
Selected commands were `Test-Tiers.ps1 -Tier Candidate -AssemblyTarget Kingmaker`,
`Package.ps1 -ArtifactQualifier c7b-p07-head7348 -IncludeRemovalObserver`,
`New-KmcQualificationSuiteSnapshot.ps1` with the pinned package/authority
first in WhatIf and then commit mode, and `Invoke-KingmakerRuntimeScenario.ps1`
with `persistence-p07-save` / `prepare-removal`, the pinned suite and package,
separate run IDs, and a scoped WhatIf before each native attempt. Slot1 used
`Recover-KingmakerRuntimeTransaction.ps1` WhatIf then actual; slot2 used
`Recover-KmcP07Head7348Profile.ps1` WhatIf then actual. The private launch log,
package/suite manifests, original result roots and recovery receipt retain
the full paths and exact invocation pins.
No positive cleanup archive or DLL-absent consumer exists from this batch.
The original FAIL artifacts are retained under the two private runtime-evidence
roots. The b32s2 `output_log.txt` supplies the exact native exception; the game
result had no mounted assertions. New `Recover-KmcP07Head7348Profile.ps1` is a
one-run, hash-pinned guarded restoration after UMM appended KMC and advanced
its update-check timestamp. Actual-owner WhatIf passed; independent review
receipt SHA-256 `1a164d18016105f19e81664d0e091dae799aa2891f1a648ce2a0af467366bb40`
passed after under-lock captured-byte, lock-token and profile checks. Actual
recovery PASS. No game/lock remains; Working SHA-256 is
`5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6`.
Directive 4 remains IN PROGRESS: P07 needs a distinct load diagnosis before
another attempt; schema2 TB debt, area Auto/cold and doors are still open.

## October 19 - preview.52 selected native breadth and reader-only correction

Published frozen candidate: `f5fe6fcda39f809b92f2a6cab34bab4a2a108acb`,
package SHA-256 `1b191abb67860fadd707d0f8a32e9dc08e2fbc95842178c9ca1496540d48e4da`,
DLL SHA-256 `18f619bd28e872b73470944aaf46671887d91d99ff26f80f30a8fd50019a1433`,
suite SHA-256 `1f71f8fb1b0e4c1f22794d6347e55fc1c6e32fca09e67d4238e883138792a64a`.
Package build 11/0, Candidate 6/0. Private batch27-31 ledgers and immutable
runtime-evidence roots bind individual selections, source archives, game
artifacts and restoration. Five actual native runs; batch29 was a prelaunch
WhatIf FAIL on a BOM-marked private JSON selection, then corrected in batch30.

| Selected case | Current evidence | Status |
| --- | --- | --- |
| Registered two-owner disable/re-enable, local failure retry, permanent Horse removal refusal | Batch27 game and external reader 9/0 | PASS |
| Zero-mounted TB retained-owner Manual | Batch28 game and external reader 4/0; native archive `692e1788...` | PASS |
| Exact separate-process zero-mounted cold | Batch30 game and external reader 3/0, one load/zero writes | PASS |
| Malformed and future metadata real native load refusals and menu refresh | Batch31 each game 5/0; original external FAILs retained. Corrected reader re-evaluates both complete immutable artifacts PASS after focused 53/0 and independent 34/0 review | PASS |
| Schema2 TB pending-only migration | Batch19 original cold FAIL: saved A outside combat/unprepared with 6/3/6; installed native JoinCombat/Prepare.Tick clears debt; lawful boundary/representable input unresolved | FAIL |
| Area Auto and cold | Batch23 original area preflight refused capital Oleg, zero transfer/save requests; no qualified visited non-capital target | IN PROGRESS |
| Positive reference-free cleanup and genuine DLL-absent load | Permanent Horse references make accepted two-Horse derivative unsafe for positive removal | IN PROGRESS |
| Doors | No qualified ordinary door on this owner-seed route | DEFER — EVIDENCED |

The reader fix changes only `Chunk7LoadRefusalEvidence.ps1` and its focused
test, not the preview.52 DLL. `RetainedObjectIdentity` uses signed nonzero
Int32 labels and retains at most 128 references; the old external reader
confused label value with retention count. Independent review PASS receipt
SHA-256 `9c9e2829e1f1eceff73dd7c889b9c5cc2ed67170109cff6c57ed9b55101a7a91`.
All five game processes exited, Mods/protected saves/Working restored; only
batch27 had four policy-accepted native profile-cache changes and therefore
not exact profile-byte restoration. No game or lock is live. PR #5 remains
draft. No merge, release, tag or permanent installation. Directive 4 is
IN PROGRESS while schema2 and removal/area prerequisites are investigated.

## October 19 - preview.46 frozen native batch21 FAIL; preview.47 diagnostic repair IN PROGRESS

Guarded-pushed preview.46 `40439d09d3cf162126d3c5721eb1f2cbb4ca3d73`
passed Candidate 7/0; package SHA-256
`3237bd2edc33aa61ce409086bc439abf04733807d1960fbb18f00d824c819571`,
DLL SHA-256 `5adf26e5940fb41b902b464d126640a841dcc2072d8ee959dffdf24b0d913798`.
Suite snapshot SHA-256 `1516171035003db2b366800eae5c38ef0cea97e67623625cdf22bbc1c9e73fc9`.
The private `owner-seed-batch21/batch21-ledger.json` records three actual runs,
all FAIL. Disable slot1 game evidence SHA-256
`7a553537be6c8d0813423c7069bc7d97e8c631665126a117462cb2123e6533b5`:
the permanent Horse removal refusal and real pending survivor passed, but the
registered-disable composite prelude failed before callback. Three-load slot2
evidence SHA-256 `9e8c66f95e74a81e4f1001a4cae70ffe47fb561664fd7da6a9c618ec50974407`:
second native load disposed/replaced Player, area/scene and actors with exact
8/4/4 restoration/disposal 1, but the diagnostic rejected newly instantiated
`HorseCompanionAddPet` fact subscribers. Two process-owned subscribers stayed
stable; 4 unique subscribers and 17 interface entries remained. Area slot3
timed out without Auto write. Its cleanup-pending artifact SHA-256
`b40c38f49d2063cc0ecc743902a8231557191f5e54bcc2684ecf3a7cbaf13102`
records `areaTransfer.before`, no awaiting-ready observation, and failed
destination adoption with retained native custody. It does not establish
whether synchronous LoadArea returned or which engine boundary stopped.

Area slot3 original runtime result SHA-256
`fd037e6c6214a7dcb1037c78f28ed4b2a8c7c8416b35cd3b4d3b9925ef59a685`
remains FAIL/restoration-blocked. The exact PID closed via guarded normal
window close, repository guarded transaction recovery restored Mods and saves,
and separate exact profile recovery passed (receipt SHA-256
`3b01c60b17c033778bbbfaad40a99107f8c5bfd1d177b2b23d37c5b3f165cdb3`).
No game, lock or unsettled live state remains. The recovery receipt pins restored
profile and Mods/save digests; no old native result was promoted to PASS.

Unfrozen preview.47 source checks subscriber roles/cardinality from the admitted
native archive: one Horse fact gives 3 unique/16 interface entries on the older
Mammoth/Horse route; two Horse facts give 4/17 on the owner-seed route. Process
subscriber identities stay stable, fact identities refresh on native reload and
remain stable on same-Player area/settlement. The first independent review
correctly found and preserved a FAIL for the older route; corrected-source
independent review02 PASS (SHA-256
`61f7f389d8de9fe6f38a28199d710a853c7c38a0293d103089545e4a650b6b94`).
External synthetic reader 104/0, owned 59/0 and owner-seed
29/0 pass with stale-Horse and changed-process negatives. Failure-only disable
per-clause detail and area interruption/dispatch-return snapshot add no action,
save, scope or native budget authority. Fast 3/0, source 147/0, disable adapter
47/0, repeated adapter 76/0, registered reader 64/0, repeated protocol 27/0
and runner/completion 7/0 pass. Failure-diagnostics review01 PASS (SHA-256
`936b96c1ab1bef97add8608055cc63b8dc2203b45e41b6790336b2412cb42c82`).
Changed-byte native execution remains; schema2 debt and remaining refusal/no-DLL/doors cases
remain unresolved. No merge/release/tag/permanent installation or HUMAN PLAY.

## October 19 - preview.45 native checkpoint; successor source IN PROGRESS

Frozen preview.45 `8685fb5f` package SHA-256
`3a5a4b6cc14939ec8f9dec0cad0af68cbdb2623db238d3805aa7200ddf24c8f7`
passed Candidate 7/0. Private batches19/20 preserve each native result and
recovery receipt. Pending current3 producer, schema1 producer, schema1
cold/current3 resave and its separate third-process cold PASS. Schema2 cold
FAIL after native `UnitCombatPrepareController.Tick` cleared the rejoining
rider's saved Standard/Move/Swift 6/3/6. Area AfterEntry Auto FAIL/TIMEOUT
before autosave; three-load replay FAIL/TIMEOUT at the second load; registered
disable FAIL before UMM toggle. Area cold and schema2 third cold remain deferred.
The timed-out game processes were closed gracefully; guarded transaction and
exact profile recovery each passed separately. Original results stay FAIL.
No game or runtime lock remains, and the installation/protected saves were
restored exactly. No preview.45 area, schema2 cold, repeated-load or disable
acceptance is claimed.

Unfrozen preview.46 successor changes: the disable diagnostic uses quiescent TB combat
membership instead of exploration-only idle (actual-method adapter 47/0);
repeated-load dispatch uses the installed `Game.LoadGameFromMainMenu` disposal
entry rather than in-place `Game.LoadGame` (actual-method adapter 76/0).
The scenario, inherited fixture and evidence reader now retain exact `Game.State`
while requiring fresh Player, area/scene state and actors; inherited epochs
PASS 95/0, repeated reader 101/0, owned reader 59/0 and owner-seed repeated
reader 29/0. An area readiness snapshot records the latest loading or
post-loading barrier without granting scope or save admission. A bounded installed-assembly call inventory
confirms the load-entry distinction and native Prepare.Tick's Clear call;
private file `analysis-cache/chunk7-continuation/codex-takeover/group46/native-boundaries46g.txt`
is not shippable code. Fast build/components/source validation PASS 3/0,
source validation 147/0 on the complete preview.46 diff; an incomplete
version-triad Fast run failed source validation 146/1 and is preserved.
The adapter's first compile failure from an
expression-bodied extraction remains FAIL; after using a block body, 47/0
passed. Exact-hash independent reviews passed for the disable, repeated-load,
area and version deltas; private receipts are in `codex-takeover/group46`.
The source still needs a clean commit and a new candidate before native use.
Schema2 resource-safe repair is still under investigation. P07,
real malformed/future refusal/menu, genuine no-DLL, dependent area cold and
doors remain unrun. Directive 4 remains IN PROGRESS; PR #5 remains draft.

## October 19 - preview.45 repair checkpoint IN PROGRESS

The frozen preview.44 candidate passed Candidate 7/7, but selected native
qualification found a schema-2 cold combat-debt FAIL after restoration, a
schema-1 source diagnostic hotbar bound below the installed 140 slots, an area
Auto request DTO refusal before launch, and a second repeated native load count
failure followed by blocked cleanup. The repeated-load timeout was resolved by
guarded transaction and independently reviewed exact UMM profile recovery;
the original runtime result stays FAIL. Only the current-schema pending source
from this batch passed. The dependent schema-2/schema-1 resaves and area cold
load were not run. Exact immutable receipts and failures remain in the private
batch16/17 ledgers and `AUTONOMOUS-RESUME-RUNTIME44.md`.

Preview.45 source preserves the actual native debt assertion and records selected
full seven-actor comparisons plus bounded native Join/Prepare observations to
attribute the schema-2 loss. It raises the diagnostic-only hotbar observation
bound to 140 while retaining strict identity checks, passes the area target as
the strict DTO shape, and captures all six repeated-load counters before the
assertion. A failed repeated-load epoch can adopt exact current-world custody
solely for cleanup; it cannot qualify a measured load or dispatch another.
Synthetic schema-2 fixture and external reader now follow the selected journal.
The exact post-timeout profile recovery script is included for audit.

Each source group received independent private review. Applied focused checks:
schema1 187/0, area 29/0, debt probe 61/0, debt reader 76/0, owned-pair reader
680/0, repeated-world actual methods 75/0, owned replay epochs 91/0, repeated
world reader 100/0. The first full compile failed on a diagnostic `Time` typo;
after correcting it to `UnityEngine.Time`, source validation 147/0 and Release
product build passed. This is offline evidence only. Earlier failed checks remain
recorded. The worktree is uncommitted at this checkpoint. Next: coherent diff
review, guarded push, one proper package/Candidate/suite, then exact scoped
WhatIf and selected native reruns. No preview.45 game PASS exists yet.

## October 19 - preview.44 selected source connection IN PROGRESS

The reviewed group44 source is applied after closure HEAD `7c1bcadc` on
`codex/mounted-combat-chunk7-successor`. No preview.44 game run is accepted yet.
Preview.43 worker/barrier native PASS and failed migration producers retain
their original bytes and verdicts. No game or transaction remains live.

- Schema1 observation preserves null/native collection and unrelated slot
  identities. Only exact already-inactive KMC templates may clear naturally;
  completed-save/cold control counts stay strict.
- The pending source writes genuine current3; a guarded, labeled metadata-only
  schema2 copy exercises the actual historical loader and native current resave.
  The correct future-data preservation rule is unchanged.
- Selected owner-seed connections cover a visited distinct-area native
  AfterEntry Auto plus its actual cold output, three native loads, registered
  two-owner disable/local failure and permanent-Horse removal refusal. Direct
  native LoadArea assistance is disclosed; it proves no ordinary exit/travel.
- Positive P07 reads full current3 with an explicitly observation-only singular
  projection. It still requires actual reference-free cleanup and a separate
  genuine DLL-absent native load. Existing Working compatibility is unproved
  on this candidate and will be measured, not inferred from foreign markers.
- Malformed/future derivatives retain every native ZIP member; exact alternate
  identity is read-only. Real load admission decides first; an unexpected accept
  fails before native world disposal. Full conservation and save-menu refresh
  are required. Counted load/write quotas and unselected paths remain intact.

Offline source checks: merged C# 775/0, actual505-source compile PASS
24.2910658s; merged PS 501/0 (all28 parse), original refusal PS copies81,
artifact47, wire20, final-root9 PASS. Required applied-source assisted connection
PASS236/0 in27.2362778s; source147/0. Compiler/input receipts and independent
reviews are private in `analysis-cache/chunk7-continuation/codex-takeover/group44*`.
The applied test run found a missing new partial in the linked test project,
then an outdated exhaustive DTO property list; both FAILs remain preserved.
Corrections add the real dependency and the two already-validated provenance
properties. No gameplay authority was widened. Earlier composition/test failures
also remain recorded. Private compiler proof DLL is never a package input.

Portable refusal157/0 and single-build packaging22/0 pass; the latter keeps both
existing package validators and all source/identity/immutable-output guards.
Independent packaging review PASS081492112ddc44bfb72b4b38f54029827b7d0576753bb5db9b625ea0e37841ea.
Cumulative DTO28/0 also passes. Next: clean commit and
guarded push, then one proper preview.44 build/package and Candidate (includes
Fast), freeze suite and declare bounded native batches. Every actual attempt
needs scoped WhatIf and verified restoration before another. Remaining selected
native work is migration1/2 through current resave and third-process cold,
area/Auto cold, repeated load, disable/refusal, malformed/future refusal/menu,
positive cleanup and genuine noDLL. Directive4 stays IN PROGRESS. Doors remain
opportunistic; acquisition/progression, Small, six-pair/performance, HUMAN PLAY,
full7E, merge/release/tag/permanent installation are not accepted here.

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
