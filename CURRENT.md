# Chunk 7B–7D — IN PROGRESS

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
