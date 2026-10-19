# Chunk 7B–7D — IN PROGRESS

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

## Active Directive 4 checkpoint — October 18, Codex takeover

Continue [Directive 4](docs/CHUNK7-OWNER-DIRECTIVES-2026-10-16.md#assignment-directive-4).
Its endpoint is still **IN PROGRESS**. Small, progression certification, six-pair
stress/performance, full 7E, merge/release and HUMAN PLAY remain separate gates.

The active successor branch now occupies the dedicated repository
`C:\Dev\KingmakerMountedCombatLab\repo\KingmakerMountedCombat`. Use the original
`codex-policy/Push-KingmakerMountedCombat.ps1`; copied-helper execution was rejected
by automatic approval review even after owner approval. The original guarded helper
successfully published `6652c7fe1e8adb0edb61c28f520275bf5f708b65`. The former successor
worktree is preserved detached at that source as the preview.41 frozen product.

**New accepted result:** `20261018-c7b-world41-ownerseed-b12s1-tbload`, preview.41,
terminal **PASS** in 104.4654715 seconds. Codex independently reviewed Claude's
`f1e54002` fix, then cold-loaded the accepted batch-11 TB producer in another process.
Completed A's saved `Boundary.Movement.MetresStepped=2.286` stayed exactly `2.286`
at both shared-world and presentation restoration. Native continuation and cleanup
passed. Mods, Working, Baseline and protected saves were restored; profile bytes were
restored exactly and the runtime lock was released.

**Retained failure:** `20261018-c7b-world41-ownerseed-b12s2-zerosave`, terminal
**FAIL** in 115.7495532 seconds, despite game PASS. The external reader treated the
zero-mounted save boundary as though the preceding attacks were already detached;
rider B was actually still mounted and its own Horse paid the native approach.
Restoration passed. The failed producer cannot seed a cold load. The reader correction
has focused 126/0 checks and final independent review PASS after three retained FAIL
reviews closed malformed scalar, identity-format and conflicting-owner cases. Immutable
raw re-evaluation is diagnostic only. No native transaction is live at this checkpoint.

**Fresh zero checkpoint accepted:** b12s4 native Manual producer PASS in
126.9279735 s and b12s5 separate-process cold load PASS in 119.7310995 s on the
same frozen preview.41 product, with harness/reader `2b3d6702`. The save has zero
mounted pairs and two retained participation owners. Exact restoration, native
continuation and each Horse's independent native turn at N+1 passed. Original b12s2 remains
FAIL, its dependent b12s3 was never launched, and b12s5 uses accepted b12s4 only.
Batch 12 is settled with no live transaction. Source work now connects worker
cancellation, nonselected-owner save fencing and legacy pending-survivor migration.

The next source checkpoint is preview.42: explicit owner-seed worker replacement,
nonselected-owner Mount save fencing and legacy schema-2 pending survivor paths,
plus read-only stock transition discovery. Focused checks pass (authority401,
boundary authority115, actual adapters78/26, owned reader584, source205,
boundary reader204, combat source29 plus nested29/55/126, route19 and existing
cold composition210; no failures in these final runs). Initial failed generated
fixtures and the corrected live-versus-migrated owner comparison remain recorded.
Independent source review found no additional defect. Candidate packaging and
all five declared native batch-13 cases remain TODO at this source checkpoint.
Earlier Directive 4 evidence remains on its original inputs: RT click integrity and
first-action Mount in both orders (preview.35); Quick/Auto save plus cold load
(preview.36); RT debt save/load (previews.37/.38); TB debt producer (preview.40).
The [handoff](docs/CODEX-HANDOFF-CHUNK7-DIRECTIVE4-2026-10-18.md) records those exact
batch/slot identities. Pending survivor, nonselected-owner barrier, worker cancel,
area/Auto continuity, disable/removal and schemas 1/2 still require their selected
source connections and native evidence. Doors remain opportunistic.

Preview.41 was built once: DLL MVID `daf5ccc5-3a74-40e6-9563-8e8fa28daa4f`, package
SHA-256 `291d8fb4747d1f1e307a68c259a0d6631e5ecb37909ab8fa0c3bb7f9ed252709`.
Candidate passed 6/6 in 388.1997642 seconds (834 components, 147 source checks,
281 harness checks, paired contracts, manifest checks and 751 Kingmaker contracts).
Its unchanged package is reused through the existing frozen-product guard; a fresh
suite binds the new execution root. Private batch, review, freeze and suite identities
are in `analysis-cache/chunk7-continuation/owner-seed-batch12/`.

## History — Directive 3 checkpoint (preserved below)

## Current state (October 18, 2026)

**Governing instructions.** The owner's [directives of October 16–17](docs/CHUNK7-OWNER-DIRECTIVES-2026-10-16.md), including Directive 3 (the PR #5 review and the persistence/combat assignment), and the [validation policy's development-acceleration section](docs/VALIDATION-POLICY.md#development-acceleration-owner-directives-of-october-16-17-2026). The active development workflow uses:
- the approved owner-seed route;
- a dependency-scoped development audit (`-DevelopmentScopedAudit`), labelled as not a whole-lab purity proof;
- focused behaviour tests;
- bounded batches of at most five declared native scenarios.

The old lifetime limit of 25 attempts and the mandatory whole-lab rehash are historical, not current admission requirements. All historical attempt ids and failures are preserved.

**Branch roles:**
- **This branch, `codex/mounted-combat-chunk7-successor`,** is the unified product plus harness line. It is reviewed as draft PR #5, which is not to be merged, released or installed for human play. Product and harness identities are recorded per run: one package per committed head, pinned by commit, DLL SHA-256 and MVID, plus a fresh qualification suite.
- **`codex/mounted-combat-chunk7-preview10-harness`** holds the root records: directives, the dated checkpoint history and earlier campaign documents. It carries no successor code.
- **The private lab, `C:\Dev\KingmakerMountedCombatLab`,** holds fixtures (the immutable owner-seed master and its intake), runtime state and evidence, the batch ledgers and the review receipts. None of these are in Git.

**What works**, summarised in [owner-seed evidence](docs/CHUNK7-OWNER-SEED-EVIDENCE.md):
- The owner seed `KMC_C7_NO_MODS_OWNER_SEED` (SHA-256 `3405d7c8…821b924f`) is qualified by the accepted cold load `20261017-c7b-world18-ownerseed-s4-cold`.
- One two-pair movement, selection and control slice was accepted in `20261017-c7b-world22-ownerseed-b2s4-twopair` (preview.22, `162a6d14`). It covered disclosed native setup, both native Mounts, independent moves, an independent Dismount with the other pair retained, unmounted control and remount cycles, with verified restoration.
- **Two-pair Manual persistence** (October 18), on preview.26 `20e23743`:
  - `20261018-c7b-world26-ownerseed-b3s1d-save` wrote one genuine owner-seed-derived native Manual whose schema-3 read-back matches both live pairs.
  - `20261018-c7b-world26-ownerseed-b4s1-load` cold-loaded an independent copy of it in a separate process and restored both owners with the producer's identities and generations.
  - Restoration was verified both times.
  - Product fix on the way: KMC refused to load its own all-pair saves (`0ccda7cc`).
- **Two-pair combat** (October 18), on preview.34 `bba4edde`. All three run on cold-load-restored mounted pairs against the runtime-only stock Mammoth target:
  - RT with Valerie's ordinary native action: `20261018-c7b-world34-ownerseed-b6s5m-rt`, PASS, 10/10. Concurrent independent primaries, each paid by its own actor. The companion took exactly one ordinary command with its own cost, and neither pair was touched.
  - TB mount-before-rider: `20261018-c7b-world34-ownerseed-b6s4m-tbmount`, PASS, 12/12.
  - TB rider-before-mount: `20261018-c7b-world34-ownerseed-b7s1-tbrider`, PASS, 12/12.
  - Each TB run covered:
    - each pair's full native activation;
    - native renewal in the next round;
    - a native Dismount as the first action of a prepared turn;
    - a native rider-removal forfeit with a pending-only survivor;
    - isolated cleanup failure.
  - Restoration was verified for every run.
- **Product change, owner decision of October 18:** a voluntary turn-based Mount/Dismount is now admitted in the rider's prepared (Preparing) turn (`06ec7180`). This supersedes Chunk 6A rule R3, which made the transition unavailable as a turn's first action.

**Not proven:**
- acquisition (setup is disclosed assistance);
- door behaviour (no qualifying door at the seed location);
- Quick/Auto/area/removal/migration persistence breadth;
- Small calibration;
- stress/performance;
- human review.

The Chunk 6A probe row `CM01-combat-mount-preparing-refused` still asserts the superseded R3 and will fail if re-run. See [known product debt](docs/CHUNK7-KNOWN-PRODUCT-DEBT.md).

**Directive 3 endpoint status:** reached on the owner-seed route. The pieces are:
- the reusable owner-seed-derived two-pair native Manual;
- its successful separate-process cold load with both owners restored;
- the targeted RT/TB combat slice showing independent actor expenditure.

Details are in [owner-seed evidence](docs/CHUNK7-OWNER-SEED-EVIDENCE.md). Nothing is merged, released, installed for human play or claimed as HUMAN PLAY acceptance.

**Known not-ready families:** ordinary acquisition, manual derived, native progression and assisted fixture production still wait for a level-up autosave that the installed game never requests. Do not select them until their completion contract is repaired.

## History — state before October 16, 2026 (preserved as written)

The [sustained mission](planning/CHUNK7-CONTINUATION-MISSION.md) is stopped at the owner’s October14 quota/push request. [Safe stopping point, exact continuity and resume](docs/CHUNK7-OWNER-STOP-2026-10-14.md). No game, runtime transaction, lock, sentinel or test session remains. [Claude takeover guide](docs/CLAUDE-HANDOFF-CHUNK7-2026-10-14.md) is prepared for the next owner-invoked session. The breadth candidate is not ready. Root owns integration and every live transaction on `codex/mounted-combat-chunk7-preview10-harness`, origin `https://github.com/howardreith/KingmakerMountedCombat.git`.

Preview10 is frozen from published product `0e1ce2d92a368866ce0803d070a6f3b7b96bd971`. It was built once in48.5995196 seconds (source147/0, package11/0). Actual Candidate03 passed all six stages in214.1538249 seconds on harness `b8db761b3d175aebc701a270a6016838b46a3768`: components817/0, source147/0, Harness271/0, paired-activation contracts, manifest11/0 and Kingmaker contracts701/0. Candidate01/02 remain FAIL; the two external contract repairs changed no product/package bytes. Suite WhatIf passed113.3220773 seconds and append-only Create passed44.08256 seconds. [Exact artifact, source, review and suite identities](docs/CHUNK7-PREVIEW10-CANDIDATE.md).

The reviewed implementation connects bounded assisted campaign preparation, separate read-only cold inventory, and an explicit qualified assisted-source route to ordinary acquisition. Passive startup telemetry records actual Unity frames and elapsed time. It changes no action or timeout. The three consumer review repairs preserve main XP from readiness, bind inherited-pair checks to admitted custody, and require exact cold profile restoration. [Behavior, focused checks and retained failures](docs/CHUNK7-ASSISTED-FIXTURE-SUPPORT.md).

Nine of25 native attempts are used, all final FAIL and settled. Preview9 failed before any load request; startup cause remains unexplained. Separate recovery restored Mods, protected saves, profile/settings and dependencies exactly, with zero accepted cache changes, then released the lock. [Exact receipts and unchanged failure](docs/CHUNK7-NATIVE-OBSERVATIONS-2026-10-12.md). No preview10 native transaction has started.

The full read-only preview10 WhatIf was stopped incomplete at the owner’s request; its actual terminal exit-1, transient immediate-stop failure and later proven process absence are preserved. It grants no purity PASS or native admission. Current Mods/saves exactly match the admitted suite. Approved synthetic progression archive checks passed19/0 in18.8470646s. Two unfinished source drafts are pushed as inactive patch backups, explicitly uncompiled/unexecuted and not integration-ready. On resume, review those drafts; any native10 requires a fresh numbered reviewed full audit against current inputs.

Native capacity two, offline six, retained twelve; native writer schema3 with explicit schema1/2 adapters. Medium Horse/Mammoth remain supported; Small catalogue is EMPTY pending body/pose proof. RT Charge is default-off; TB Charge cost-free refusal and defensive feat remain deferred. Required RT/TB action/debt, persistence/area/removal, ordinary progression, Small/six-pair, matched measurements and unmounted/diagnostics-off controls remain open. No7E, merge, release, permanent installation or HUMAN PLAY acceptance is selected.

The October14 remote foundation rewrite029089→92c8daf4 has identical tree `1c15365fc9039db2829e3c268ef4a77819db9bac`. Local foundation, unknown/other worktrees and frozen artifacts remain preserved. [Preview9](docs/CHUNK7-PREVIEW9-CANDIDATE.md) retains its original identities.
