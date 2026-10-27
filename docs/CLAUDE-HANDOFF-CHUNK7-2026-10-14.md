# Claude takeover — Chunk 7B–7D

Prepared October 14, 2026 for the owner's next working session. Status: IN PROGRESS; the breadth candidate is not ready. Codex stopped safely at the owner's quota request and guarded-pushed all completed work plus inactive draft backups. This document prepares tomorrow's takeover; it does not restart implementation or native work today.

## Start here

Use the existing Windows lab at `C:\Dev\KingmakerMountedCombatLab`. Repository: `C:\Dev\KingmakerMountedCombatLab\repo\KingmakerMountedCombat`. Continue from actual current refs and source; no Codex session or agent needs to be resumed. Tool session IDs in receipts are historical provenance, not portable handles.

Read in this order:

1. [AGENTS.md](../AGENTS.md), [validation policy](VALIDATION-POLICY.md), [CURRENT](../CURRENT.md), and [resume record](../AUTONOMOUS-RESUME.md).
2. [Complete active owner mission](../planning/CHUNK7-CONTINUATION-MISSION.md), [durable architecture brief](../planning/CHUNK7-DEVELOPMENT-BRIEF.md), [opening review](../planning/CHUNK7-FOUNDATION-REVIEW.md), and [implementation plan](../planning/CHUNK7-IMPLEMENTATION-PLAN.md).
3. [Exact safe-stop record](CHUNK7-OWNER-STOP-2026-10-14.md), [preview10 candidate](CHUNK7-PREVIEW10-CANDIDATE.md), [assisted fixture implementation](CHUNK7-ASSISTED-FIXTURE-SUPPORT.md), and [native failure/restoration history](CHUNK7-NATIVE-OBSERVATIONS-2026-10-12.md).
4. [Inactive draft manifest and handoffs](../planning/drafts/chunk7-owner-stop-2026-10-14/README.md) before touching either draft.

The original owner attachments are under `C:\Dev\KingmakerMountedCombatLab\handoffs`: `KMC_Chunk7_Sustained_Codex_Prompt.md`, `KMC_Chunk7_Foundation_Review_2026-10-12.md`, and `CHUNK7-DEVELOPMENT-BRIEF.md`. The sustained mission supersedes the completed7A source-only stopping point and its former candidate/native prohibition. Current safety and promotion boundaries remain.

**Do not start from the older `handoffs\KMC_Claude_Takeover` packet.** Its September22 snapshot points to Chunk5 preview.57 and a different branch/configuration. Preserve it as historical evidence. Its next actions, installation pins and model advice are not current authority. Likewise, historical paragraphs in AUTONOMOUS-RESUME do not override its active stopping-point section.

## Exact repository and artifact identities

| Identity | Recorded value |
| --- | --- |
| Verified origin | `https://github.com/howardreith/KingmakerMountedCombat.git` |
| Active review branch | `codex/mounted-combat-chunk7-preview10-harness` |
| Safe-stop commit, freshly rechecked local and remote for this handoff | `72de9da2e61ac9921d71968a1f733bfc5b029610` |
| Safe-stop tree | `2e72dcb56cba7cc36245b48b42d5e46848aa75dd` |
| Published product branch | `codex/mounted-combat-chunk7-assisted-fixtures` |
| Published product source | `0e1ce2d92a368866ce0803d070a6f3b7b96bd971` |
| Product tree | `59fc88acd78006a8ea83aeeefc21dcc9e2833243` |
| Candidate03 external harness | `b8db761b3d175aebc701a270a6016838b46a3768` |
| Both unfinished drafts' independent base | `40157ba1dfeea8d4b2713319dad4047f6f7af0e4` |

This document is a later documentation-only descendant. Refresh HEAD rather than resetting to any identity above. The canonical tree was clean on entry to this handoff task. The October14 foundation remote rewrite029089→92c8daf4 had identical tree; local refs, other worktrees and all frozen worktrees were preserved. In particular, leave the separate Claude Chunk6 worktree and detached qualification/candidate worktrees alone.

Preview10 version: `0.1.0-chunk7b-preview.10`; assembly0.1.0.0. All paths below are relative to the lab, not the repository:

- Frozen original product worktree: `worktrees/chunk7b-preview10-0e1ce2d`. Do not rebuild or mutate it.
- Package: `artifacts/KingmakerMountedCombat-0.1.0-chunk7b-preview.10-c7b-world10-diagnostic.zip`. SHA256 `de4c54821551ddb08da24a06ce4debb7523b1f0640f619ef2d479fb44ce5433a`.
- Manifest: package path plus `.manifest.json`; SHA256 `5b352c6f32f5ca69058ecd771308b41088cdc23015d6c1d48f40a8a9bcd556f7`.
- DLL SHA256 `e433c15741509a1d9ecddd54a82ab9154b3e9e90b24fbb8c0fce07a5b60b3487`; MVID `5b466e91-eede-46d7-844c-e071c2dc0ae4`.
- Suite: `runtime-state/qualification-suite-snapshots/chunk7b-world10-20261014.json`; SHA256 `fb0a1b1a7e732af795032a088d103c22f60e70074eefde98d9fa2e82e5284916`.
- Original immutable freeze: `analysis-cache/chunk7-continuation/candidate-world10/freeze01.json`; SHA256 `c42e60bb1c4a451ef5a9e7c9a988992cb4133d529bfb5dfb4df3792253dc1463`.

Reuse unchanged artifacts. A documentation/handoff commit is not a new product build or a claim that old evidence ran on a new binary. Product/observer changes need affected checks, review and a newly identified candidate before native use.

## What is implemented and what is proven

R1/R2 were repaired with focused regressions at `2276bc40b892befd4204e1ab851fc2c0ca1c16d9`. Repeated detached cleanup preserves the split-release round. Valid schema2 mounted-plus-pending-only migration retains debt without inventing an activation or admitting conflicting ownership. Native regressions remain open.

The production implementation now has independent pairs under one world/control/subscription layer, exact actor/command/context/session/generation routing, a shared actor-allocation ledger, all-pair schema3 capture/import, explicit schema1/2 adapters, exact-object post-load checks and worker/cleanup ownership. Native capacity is2, offline capacity6, retained capacity12. Native writer is schema3; supported decoding/migration does not independently authorize restoration. Do not restore the old7A one-pair/writer2 restriction from its historical prompt.

Published preview10 additionally connects bounded assisted new-campaign preparation, separate one-load/zero-write cold inventory, and qualified assisted ancestry to ordinary paid acquisition. Startup telemetry observes actual Unity frames/elapsed time; it does not change actions or deadlines. Reviewed consumer repairs preserve main XP/readiness, inherited-pair custody and exact cold profile restoration. This is implemented source, not successful native proof.

| Actual verification | Result and limit |
| --- | --- |
| Once-only preview10 package build |48.5995196s; source147/0 and package11/0. |
| Candidate03: `scripts/Test-Tiers.ps1 -Tier Candidate -AssemblyTarget Kingmaker -SkipBuild` |6/6 stages PASS in214.1538249s: components817/0, source147/0, Harness271/0, paired activation, manifest11/0, Kingmaker contracts701/0. Offline only. |
| Candidate01 and02 |Remain FAIL: stale optional-result field set, then omitted assisted registrations in the manifest normalizer. External test/schema corrections changed no product/package bytes; original failures retained. |
| Current approved `scripts/chunk7/Test-Chunk7ProgressionArchive.ps1` |19/0 in18.8470646s:8 valid generated cases plus11 mutations. Only generated analysis-cache ZIPs; no installed/protected archive reads. Supersedes the unexecuted15-case request without erasing its rejection. |
| Safe-stop documentation/backup checks |33 local links, clean diff checks, both20/21-target patches reconstructed exact LF-normalized text in private copies. Custody only; drafts are not tested source. Initial backup-check fixture/EOL failures retained. |

Native attempts1–9 are all final FAIL and settled. Attempt7 observed two native Mounts then failed its first ground click; that is not movement/action-budget qualification. Preview9 failed before any load request, eventually self-exited, and was separately restored. Its host callback count did not establish actual Unity frame advancement; startup cause remains unproved. No preview10 native transaction began.

## Safe stop and evidence custody

The independently reviewed full preview10 WhatIf started but was stopped at the owner's quota request. It passed source/package prechecks, not the complete purity audit. Original session33591 is closed with actual exit-1; no terminal audit receipt or after-input inventory exists. The stop helper's immediate process-absence check failed transiently; a separate later observation proved process absence. Keep both, as recorded in the safe-stop document. Never resume that session, overwrite evidence01 or promote it to PASS.

The recorded read-only continuity check at2026-10-14T13:49:49.3082888Z passed17.4560884s: actual current Mods and save bytes/metadata matched the admitted suite; permanent fixtures exact; game/lock/sentinel absent; no native10. This handoff task performs no new runtime/archive audit, staging, save write or game launch. Refresh actual live state before future use; these are dated observations, not tomorrow's restoration target.

Relevant private paths under `analysis-cache/chunk7-continuation`:

| Receipt | SHA256 / meaning |
| --- | --- |
| `candidate-world10/assisted-fixture-whatif-cancelled01.json` | `1b75c9ba127c7e453cf364c37b8a5d02b6dda88af69ac6e17622fbee2134c752`; incomplete audit/actual caller/stop failure. |
| `candidate-world10/owner-stop-continuity01.json` | `f1901ad322b43ee976439cbfc5808f0fb98e470f078d56695e4808bc28abfb45`; exact current continuity at safe stop. |
| `owner-stop-publication-20261014-01/publication01.json` | `4ffc3962773aef2129efcc2ac598988ed645c3a285a01623cd6775db1c052109`; guarded-pushed72de9da2, exact remote and clean tree. |
| `candidate-world10/CURRENT.json` |Mutable local state/resume summary; distinguish it from immutable freeze01. |

Native9 restoration receipt SHA `76c90ddf4a5b160ed19fb26e048d0b5a0cc9b553e028e1dd4134f0d0e7989aff`; independent closeout `10637bb626c1d4eabdaa8000128d378166921750f56d3af6af6ed958eb11c1c8`. All five restoration flags true, errors empty, profile/settings exact, cache changes0, dependencies exact, lock released. Original native9 FAIL remains unchanged. Exact inventory digests are in the linked safe-stop/native records. Do not confuse native9's restoration digest contract with the newer suite inventory digest contract.

## Unfinished source drafts: preserve, review, finish

The published patches under `planning/drafts/chunk7-owner-stop-2026-10-14` are inactive backups, not active source. Both start independently at40157, overlap and must not be applied sequentially. The manifest pins raw private bases/payloads and LF-normalized zero-context patches. Exact private sources remain under the lab analysis-cache lanes below; proposed commands are templates, not executed results.

**Owned schema1:** `owned-schema1-next01/integration-map01.json`, SHA `912451cfcfab594464ea37965b9a098ea3c2938e083f821142e8ca7025f24bb4`;20 targets,14 replacements/6 new. It drafts a normal schema3 source Manual with only pairA mounted and pairB unmounted/unmodified, a guarded metadata-only generated schema1 copy, then native load/schema3 resave. Preserve all representable slots and unrelated actors. Refuse incompatible second mounted owners, Combat, retained participation/debt and unrepresentable Charge; do not collapse a two-owner save, drop obligations or introduce a native schema1 writer. CONTRACT02 corrects preserved CONTRACT01 metadata. Missing public qualification/locked CREATE_NEW copy failure-path tests, whole compiler/scenario/worker/callback/cleanup and reader closure, and independent final review remain.

**Owned area Auto:** `owned-area-autosave-next01/integration-map01.json`, SHA `da62f4eaa4d2480fff7dc45d7d1449872d4bce65ee18e8a7f51d78623860877d`;21 targets,19 replacements/2 new. It drafts area-only SourceArea authority, acquired-source versus destination-Auto area separation, full four-actor/two-owner readiness, restoration before actual native Auto, and actual request/prepared/header/completion ancestry into cold intake. Area Auto retains zero explicit SaveGame callbacks. The boundary template has six extraction placeholders and no portable entry; neither new test is registered. Inspect generated phase syntax, fixture paths, compiler facades/old field sets, pre-dispatch retained cleanup custody and cold Party ordering. None of it has been parsed, compiled, tested, integrated or independently approved as a complete change.

First source work should inspect these explicit gaps, create a complete private composition, reconcile overlapping files additively, complete missing tests and select focused affected checks. Preserve snapshots; do not overwrite mapped payloads or copy sparse trees blindly into production. Independent review precedes candidate/native use.

## First actions tomorrow and native dependency

Once the owner invokes takeover, begin with read-only state refresh rather than a build or game launch:

~~~powershell
Set-Location 'C:\Dev\KingmakerMountedCombatLab\repo\KingmakerMountedCombat'
git status --porcelain=v1 --untracked-files=all
git branch --show-current
git rev-parse HEAD
git remote get-url origin
git worktree list --porcelain
git fetch origin
~~~

Inspect any new commits/dirty work; preserve unknown state. Check for an actual game/lock/sentinel before touching runtime state. A historical PID is not authority to kill a current process.

1. Refresh the reading order and draft dependency pins. Pick one bounded source gap; complete its missing guards/entry/registration, review composition, compile/test only affected scopes and record actual results. No historical aggregate replay is needed merely to resume.
2. If proceeding with unchanged preview10, reuse its frozen product. Before native10, prepare a **fresh numbered concrete audit**, current input inventories and independent review, then complete the full guarded WhatIf with actual caller/after-input proof. Existing `candidate-world10/Invoke-AssistedFixtureWhatIf01.ps1`, `Invoke-AssistedFixtureNative01.ps1`, `assisted-fixture-concrete-audit-inputs01.json` and `assisted-fixture-execution01.json` are reviewed historical templates, not commands to rerun unchanged. Preserve original01. Changed product needs a newly identified candidate after affected checks/review.
3. The full audit had a declared180-minute plan; native cases retain their existing600-second budget and safe-settlement obligations. Never kill a live transaction to meet a timer. Current finite native ledger is9 used of25; inspect the mission/ledger before selecting further cases rather than silently changing the budget.
4. Native10's intended role is assisted campaign producer. Only its fully qualified same-candidate terminal Manual and exact restoration admit separate read-only cold inventory, then ordinary paid Medium acquisition retaining the first owner. Owned combat/persistence/progression routes need their exact successful mounted checkpoint/acquired ancestry. A failed Auto, unmounted acquired save or successful decoding is not a substitute.
5. If a concrete fixture/environment blocks a row, settle safely, preserve FAIL/blocker evidence and continue independent selected work. Do not repeatedly relaunch equivalent unchanged failures or weaken guards.

Required mission coverage still includes independent two-pair exploration/dispatch; RT/TB actions, both initiative orders, expenditure/reactions and unrelated actors; R1/R2 native regressions; Manual/Quick/Auto/cold/debt/area/removal/migration; ordinary acquisition, leveling, replacement/retraining; one measured declared Small profile; feasible six-pair stress; matched0/1/2/6 costs, repeated settled lifecycles, diagnostics-off and unmounted controls. Medium Horse/Mammoth remain supported; Small catalogue is EMPTY. RT Charge remains default-off; unsupported TB Charge refuses without cost; the defensive feat is deferred. Missing required evidence prevents an unqualified completion claim.

## Owner authority and non-negotiable boundaries

The sustained mission already authorizes source/tests/docs, focused compilation, independent review, private frozen candidates, selected guarded native cases, coherent commits and guarded pushes. Owner messages also approved synthetic fixture archive checks, required Harness/package/disposable-fixture archive validators, and the specific read-only Working player/party graph diagnostic. The owner authorized creating a new disposable campaign/save and using facilitating setup tools where necessary; no ordinary permission loop is pending. Those approvals do not certify an archive, bypass qualification, permit foreign-mod mutation, or override protected saves.

Use one integration owner and one live executor. Work inside the lab except repository-owned guarded reversible operations against installed Kingmaker. KMC_AUTOMATION_BASELINE and all valued/human saves remain protected; only qualified disposable Working or explicitly qualified run-scoped isolated saves are mutable. Never directly curate live Mods, hot-replace DLLs, alter other products, launch/modify Wrath, or copy credentials. Stop on Steam auth/update/cloud/purchase/entitlement or unexpected dialogs. Snapshot actual current installation/settings/caches/protected saves and restore exactly in finally; dated pins in this document are not restoration targets.

Native owns actions, Prepare, initiative, reactions and resources. Keep ArchitectureB, exact object/session/generation authority, detached participation, exactly-once spending and unfinished cleanup ownership. Never manufacture/refund/replay expenditure or release shared custody before all actual native work settles. Patches/UI delegate to services; target installed Kingmaker2.1.7b/Harmony12, .NET4.7/C#7.3. No proprietary DLLs, decompiled bulk source, assets, save archives or private raw receipts belong in Git/packages.

Validation: follow the current policy; ordinary scope has15-minute planning target/30-minute ceiling excluding necessary settlement. Candidate includes Fast; build unchanged artifacts once. Full historical109-stage/105-row campaigns are not automatically selected. Keep old/new evidence bound to exact source, product, harness, reader and fixture bytes.

Publish through `C:\Dev\KingmakerMountedCombatLab\codex-policy\Push-KingmakerMountedCombat.ps1` after a clean coherent commit: WhatIf, then actual guarded push and exact remote verification. Preserve all branches/other worktrees; no destructive reset/clean, rebase, force-push, main merge, tags/releases, full7E rehearsal, permanent installation or HUMAN PLAY acceptance. Keep CURRENT/resume concise and current. The endpoint remains a breadth candidate ready for engineering and owner play review, with required/human gates honestly listed.
