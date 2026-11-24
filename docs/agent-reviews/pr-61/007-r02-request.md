<!-- agent-review:v1 enrollment=kmc-codex-mounted-shield-20261123114646-4433 key=r02-request part=1/1 sha256=d6c30988e7b36fe1c29c08f17c1d409647edd29df04f347cdca8f7a2edf84b8c -->
### Claude Opus — review request — round 2 — candidate `39c5d755d3`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mounted-shield-20261123114646-4433-r02-e65be0` |
| Checkpoint | `final` (final) |
| Candidate H | `39c5d755d39236cc0d13d6f7caecead5fed2a324` |
| Target B (`main`) | `fb743911200f150af407198c6c28519e522ef58c` |
| Merge base M | `fb743911200f150af407198c6c28519e522ef58c` |
| Mission | `mission` sha256 `4468693f2f` |
| Workflow | `6d613defe7` |
| Owner guidance | none |
| Owner amendments | revision 2, sha256 `d735f07ddc3131f5d90ae6094232e474b958614c6f432696956166a25d7d5c73` |
| Developer observed | foreground session `c7869a80-8e15-40a3-879d-92360cb9ff3f`: models ['claude-opus-5-5'], effort ['high'], permission modes ['auto'] (CLI-recorded transcript; the headless permission profile does not apply to a foreground session) |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 2 of 5 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Private-alpha checkpoint for Mounted Shield under owner amendment 2. That amendment changed the milestone to an installable private alpha for the owner's own playtesting and deferred the full native gameplay, persistence, compatibility and UI matrix to that playtest. Feature design unchanged: one passive feat, prerequisites Mounted Combat and Shield Focus, the base-shield + Focus/Greater Focus amount rule without enhancement, and the supplied icon fd519874. UMM 0.31.1 stays the build reference and declared minimum; the installed UMM 0.33.0 is unchanged. Changes in this continuation: alpha product version 0.2.16-alpha.1 (version.json, Info.json, BuildIdentity); the replay-subscriber contract test reads 0.2.N-alpha.M as release N; the closure batch for the PN14-F1/F2 residuals in bb903669, and the round-1 corrections in 6fb53633. Review: one closure/safety review of 8189d4da returned CHANGES REQUIRED with no shipped-feature defect (an earlier attempt failed in transport and produced no verdict). One correction batch followed, and the targeted recheck on bb903669 returned PASS FOR GUARDED NATIVE TESTING. PN14-F5 is rebutted for this milestone with pre-mission evidence the reviewer supported. The controller's final review round 1 (on d5fe3820) required seven corrections (R1-F1..F7), made in 6fb53633: base-only admission of the Shield rows, the Brilliant Energy stacking rule, the real replay resolver for the alpha version, and four reader/evidence gaps. Offline gates pass on 6fb53633 and on the records head 39c5d755; the supplemental targeted recheck PASS was on bb903669, before those corrections. The alpha package was built through the supported Package.ps1 and validated. NO NATIVE TESTING OCCURRED: zero guarded starts. The optional no-save smoke was left unrun on the reviewer's safety finding (the no-save path takes no UMM settings or profile snapshot). No agent-generated save was created: every save-backed guarded route requires the historical qualification suite, and this session has no desktop-control tool. Not a release, not full feature acceptance, not native qualification; PR #61 stays draft.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| KMC_WORKFLOW_BUDGET_ID=mounted-shield agent_review.py verify --gate focused -- -Tier Fast -Focused Test-MountedShieldFocused.ps1 (on 6fb53633, the production source) | yes | pass | 87.3 s | Build on the pinned UMM 0.31.1 reference; components 1525/0; Validate-Source 153/0; focused 18/18: selection rollback 68/0, shield readers 206/0, native contract 111/0 including icon identity, the force-stacking Brilliant Energy case, the genuine-object EffectiveShield case and the Shield-row base-only admission, replay subscriber contract through the real resolver 29/0, native loader 101/0, frozen source 23/0, UMM bindings 25/0 against 0.31.1 and 0.33.0. Offline. DLL e2fda1f2f0e41618729f59ab855c7848e27500df3b4222589e42e506b4723825, MVID 1f6af610-74f3-4c8c-ba9a-421c02b2781e. Budget mounted-shield. |
| KMC_WORKFLOW_BUDGET_ID=mounted-shield agent_review.py verify --gate focused -- -Tier Fast -Focused Test-MountedShieldFocused.ps1 (on 39c5d755, the records head) | yes | pass | 64.9 s | Same scope; the head adds records only to 6fb53633 (identical DLL e2fda1f2). |
| scripts/Package.ps1 -ArtifactQualifier mounted-shield-6fb53633 (supported packaging path, with Validate-Package) | yes | pass | about 40 s | artifacts/KingmakerMountedCombat-0.2.16-alpha.1-mounted-shield-6fb53633-diagnostic.zip, SHA-256 c3d80a58afa52d0506b001c2cfbddc776af1ade9b6796a5d5f3522c2f725b0bb, 4,282,777 bytes; Info.json Version 0.2.16-alpha.1, ManagerVersion 0.31.1; the DLL above; Validate-Package 11/0. The 8189d4da and bb903669 packages are superseded. |
| Independent supplemental review per docs/missions/mounted-shield/PRE_NATIVE_REVIEW.md (closure review of 8189d4da, then targeted recheck of bb903669) | yes | pass | 13.83 min + 8.35 min | Targeted recheck verdict PASS FOR GUARDED NATIVE TESTING on bb903669 under the no-live-use plan, after the closure review on 8189d4da (CHANGES REQUIRED) and one transport-failed attempt. The controller round-1 corrections in 6fb53633 came after it; they are under this controller review. Supplemental code/safety evidence only. |
| Guarded mod-load-smoke of the alpha (one start) | no | not_run | 0 | Left unrun on the closure review's safety finding: the no-save path takes no profile snapshot of live UMM Params.xml, PlayerPrefs or profile caches, and its save digest covers metadata only. Qualifying that protection is runner work outside this bounded continuation. Full WhatIf passed (purity PASS, launch=false). Zero of three permitted guarded starts used. Startup under UMM 0.33.0 is deferred to owner private-alpha playtest; prepared under owner amendments revision 2 sha256 d735f07ddc3131f5d90ae6094232e474b958614c6f432696956166a25d7d5c73. |
| Agent-generated disposable test save (bounded attempt) | no | not_run | investigation only | Infrastructure obstacle. Every save-backed guarded scenario requires the historical qualification suite before launch, and it is absent: that covers every scenario except mod-load-smoke, including chunk7-native-main-bootstrap and chunk7-assisted-fixture-preparation, and campaign-b, which first loads the historical Working save. No desktop-control tool is available. The missing capability is a guarded fresh-campaign admission mode with isolation, snapshot and restoration; it was not built within the bound. Prepared under owner amendments revision 2 sha256 d735f07ddc3131f5d90ae6094232e474b958614c6f432696956166a25d7d5c73. |
| Native gameplay, persistence, compatibility and UI acceptance matrix (acceptance ledger native parts) | no | not_run | 0 | Deferred to owner private-alpha playtest; prepared under owner amendments revision 2 sha256 d735f07ddc3131f5d90ae6094232e474b958614c6f432696956166a25d7d5c73. Prioritized checklist in docs/mounted-shield/PRIVATE-ALPHA.md: feat selection and icon, +3/+4, enhancement exclusion, competing shield bonuses, the Mounted Combat check contribution, equipment changes, dismount, save/reload; then RT/TB combat, multiple pairs, respec, death, area transitions, compatibility, UI. Not passed. |
| scripts/Test-Tiers.ps1 -Tier Candidate | no | fail | 247.6 s (historical run on cb74ad58) | Historical receipt preserved, not re-run on bb903669: Test-Harness.ps1 280 pass/2 fail on a missing historical phase3e package, and RuntimeArtifactManifestContract lacks its run directory. Both fail identically on base 5be0f737 (environmental). Not converted to PASS. |

**Evidence references:**
- prepared under owner amendments revision 2 sha256 d735f07ddc3131f5d90ae6094232e474b958614c6f432696956166a25d7d5c73
- docs/mounted-shield/PRIVATE-ALPHA.md (package identity, install, rollback, owner playtest checklist, known limitations, what was actually run)
- docs/mounted-shield/HANDOFF.md section 0 (private-alpha paragraph: source, package, checks, zero native starts, save-creation limitation, review)
- docs/mounted-shield/PRE_NATIVE_REVIEW.md (private-alpha section: closure review, correction batch, targeted recheck; PN14-F5 rebuttal)
- docs/mounted-shield/ACCEPTANCE_LEDGER.json continuation_12 (native parts deferred to the owner's playtest)
- C:/Dev/KingmakerMountedCombatLab/analysis-cache/mounted-shield-pre-native-8189d4da/ (failed transport attempt), -8189d4da-r2/ (closure review), -bb903669/ (targeted recheck PASS)
- controller gate receipts gate-focused-bb903669c038-d43d449b6c4-aa425160ecb-20261124T041424273088Z and gate-focused-d5fe38209665-d43d449b6c4-aa425160ecb-20261124T042641400718Z
- docs/mounted-shield/PRE_NATIVE_REVIEW.md (controller final review round 1 section and dispositions)
- controller gate receipts gate-focused-6fb53633f0aa-d43d449b6c4-aa425160ecb-20261124T050508250172Z and the records-head receipt for 39c5d755
- C:/Dev/KingmakerMountedCombatLab/artifacts/KingmakerMountedCombat-0.2.16-alpha.1-mounted-shield-6fb53633-diagnostic.zip (+ .manifest.json)

**Prior findings to assess:** `R1-F1`, `R1-F2`, `R1-F3`, `R1-F4`, `R1-F5`, `R1-F6`, `R1-F7`

**Owner amendments to the mission — revision 2, captured-source sha256 `d735f07ddc3131f5d90ae6094232e474b958614c6f432696956166a25d7d5c73`.** Applied by the owner with `resume --amend-mission`; each owner message is preserved verbatim, in order. The effective task is the original mission plus these amendments. The reported work comes from a developer invocation whose input included them, and the reviewer of this round receives a byte-exact copy of the captured document. Public rendering after the controller's standard sanitization; the digest identifies the private captured bytes, which this rendering can differ from.

```text
# Owner amendments to the mission

Cumulative record kept by the agent-review controller. Each owner message below was applied by the owner through `resume --amend-mission` and is preserved verbatim, in order, between its labeled marker lines. Later amendments supersede conflicting earlier requirements; requirements they do not change still apply.

----- BEGIN OWNER AMENDMENT 1 (captured 2026-11-23T19:36:29Z; message sha256 5fefaedbb11bc0870dd2897f9eb4d502036ab46cd1ca69301ec7dbf25c3455b8) -----
Continue the Mounted Shield mission through the existing software-factory workflow. Complete the remaining authorized implementation, verification, and review work, then reach the actual owner-review boundary or document a genuine remaining blocker.

This is a continuation of the existing mission. Preserve its enrollment, PR, findings, evidence, and spent budgets.

## 1. Resume the existing run and record this amendment

Known identities:

- Repository: `howardreith/KingmakerMountedCombat`
- Feature PR: https://github.com/howardreith/KingmakerMountedCombat/pull/61
- Feature branch: `codex/mounted-shield`
- Last observed feature head: `f19c8333b7aac863e94c5b5c7dfadd89d7ad4a00`
- Enrollment: `kmc-codex-mounted-shield-20261123114646-4433`
- Plan: `single`
- Mode: `foreground`
- Lab root previously used: `C:\Dev\KingmakerMountedCombatLab`

Verify the actual current worktree, remote head, controller state, and any active command before proceeding. Preserve unrelated work. If the branch has advanced, inspect the changes and reconcile them without resetting or overwriting work.

Read the current repository instructions and validation policy, the governing packet under `docs/missions/mounted-shield/`, and the implementation records under `docs/mounted-shield/`.

Use the controller installation pinned by this enrollment. Keep development in this interactive foreground Claude session, with the configured independent Codex reviewer. Do not create a replacement enrollment or a second writer.

Save this entire continuation prompt verbatim outside the worktree and record it through the supported same-session amendment route:

`python $kmcController resume --enrollment kmc-codex-mounted-shield-20261123114646-4433 --reason "Owner continuation: import PR #63 and complete Mounted Shield" --amend-mission $kmcOwnerContinuationFile`

Resolve those variables to the observed controller and saved prompt paths. Run the amendment between controller commands. Read the entire returned effective task and delivery marker, including any additional instruction parts, and include its returned `report_citation` in the next developer report.

The UMM decision below explicitly supersedes the original mission’s prohibition on changing the loader pin for this researched change. It does not extend budgets or authorize controller changes.

## 2. Incorporate the prepared UMM baseline change

Pro has prepared:

- Companion PR: https://github.com/howardreith/KingmakerMountedCombat/pull/63
- Branch: `codex/umm-0311-baseline-f19c8333`
- Commit: `5be0f737b1d40df6df4da886707d721801557ee9`
- PR base: `codex/mounted-shield`

Inspect and incorporate that commit into the existing Mounted Shield worktree using the repository’s allowed local Git workflow. Use the enrollment’s guarded push route afterward. If equivalent changes have already arrived, reconcile the content and record the relationship instead of applying duplicate changes.

The owner’s selected policy is:

- **UMM 0.31.1 is the pinned build reference and declared minimum compatibility baseline**, matching Tabletop Rules and Cutscene Skip.
- **UMM 0.33.0 is the separately recorded current Windows native-test environment.**
- The obsolete native expectation of 0.33.2 is corrected.
- Native receipts must identify the exact loader actually used.
- A build against 0.31.1 does not by itself prove native gameplay on 0.31.1.
- Do not downgrade or replace the owner’s installed loader as part of this continuation.

Preserve the companion patch’s archive/DLL hash checks, reference restoration, MSBuild enforcement, package exclusion, and factory build-provenance integration. Do not fall back to compiling against whichever loader happens to be installed.

Read `docs/umm-baseline/VALIDATION.md` and `docs/umm-baseline/loader-metadata.json`. The portable checks documented there are useful evidence, but Windows PowerShell 5.1, the full Windows product build, compiled binding checks, packaging, and native gameplay remain to be verified.

Treat the pin decision as resolved. Do not ask me to choose the version again.

## 3. Correct the remaining state rollback defect

Pro found an incomplete rollback in:

`src/KingmakerMountedCombat/Integration/HorseCompanionBlueprintService.cs`

In `SetSelectionEnabled`, `selectionDesired` is changed before the operation. When `ApplySelectionDesired` fails, the code restores feat publication using `previousSelectionDesired` but does not restore the stored `selectionDesired`.

This permits the following sequence:

1. The mod is enabled.
2. A disable attempt is refused because the relevant selection has changed externally.
3. Publication is restored, but the stored desired state remains disabled.
4. A second refused disable uses the incorrect stored state and withdraws feats while the root keeps its services enabled.

The inverse sequence can publish feats while the services remain disabled.

Reproduce this through the actual production path, then correct the rollback so the stored desired state and feat publication remain consistent with the root’s retained state.

Cover:

- Two consecutive refused disables.
- Two consecutive refused enables.
- A successful transition after a refusal.
- Consistent behavior for Mounted Combat, Mounted Archery, and Mounted Shield.
- Preservation of foreign selection entries and existing ownership rules.

Use behavior-oriented tests against production code. Exercise the real refusal path rather than implementing a test-only copy of the state machine.

Update the disposition of the earlier PN3-F3 finding honestly. It was described as fixed, but this remaining path must be addressed and reviewed.

Also reassess PN3-F5, the prerequisite-ignoring foreign-selection race. Resolve or rebut it using concrete selection behavior and the mission’s supported-selection contract. Record whether it is a blocking violation or an optional compatibility limitation, with evidence. Avoid a broad prerequisite interception unless a supported, reproducible path requires it.

## 4. Finish the gameplay test tooling

The missing Mounted Shield gameplay driver is in scope. Implementing it, producing a new DLL, and obtaining review are normal development work; they are not additional owner decisions.

Reuse the existing scenario, observation, and result-reader infrastructure. Build the smallest coherent tooling needed for the outstanding acceptance rows, including native feat selection, shield/equipment changes, mount AC observation, and the actual Mounted Combat check.

Finish this tooling and its focused offline verification before freezing the next candidate for pre-native review.

Pay particular attention to forced-roll tests: the “equal” boundary must target the final native check total including Mounted Shield. A helper derived only from general Mobility can accidentally test a result three points above equality when the shield contribution is +3.

Preserve the original feature contract and approved artwork:

- One passive feat; no new player-facing toggle or second icon.
- Prerequisites remain Mounted Combat and Shield Focus.
- Physical shield base bonus plus Shield Focus and Greater Shield Focus once each; enhancement excluded.
- Native shield stacking on the mount.
- The full eligible amount added only to the existing hit-negation Mobility check.
- Passive AC remains when the existing reaction preference is off or its use is spent.
- The approved 64×64 icon remains unchanged.

Approved runtime PNG SHA-256:

`fd519874e9d231946aad68c46f93a4e0a3a2ea04d4037807e6c7a4d36629f9d0`

## 5. Verify fixture availability and complete the relevant gates

Inspect actual fixture and qualification records, including any already admitted seed or immutable master. Do not conclude that no fixture exists solely because a named Working save is absent from the writable save directory.

Use an existing qualified KMC fixture or an owner-identified disposable seed admitted through the established validation and isolated cold-load process. Keep immutable masters outside the writable save root and use fresh disposable working copies.

Do not relabel another project’s fixture or an ordinary play save as qualified.

If no authorized admissible seed exists, that is a genuine blocker for fixture-dependent native rows. Still finish the source corrections, gameplay driver, reader changes, feasible offline checks, and current source/safety review. Identify the exact missing artifact and qualification requirement in the handoff.

Before testing, read the current cumulative budgets and record a short impact-to-check map. Preserve spent allowances. Use focused checks and required neighboring regressions; do not launch the historical Full campaign merely to increase coverage counts.

Required verification includes:

- A fresh Windows product build using the genuine pinned 0.31.1 reference.
- Compiled binding checks against 0.31.1 and the selected installed runtime using the existing loader-contract tooling.
- The changed Windows restore, reference, native-reader, and build-provenance checks.
- The actual Mounted Shield focused gate and affected publication/defense regressions.
- Repository, packet, and final embedded-DLL icon identity.

Use the actual supported controller grammar and real script names. Candidate includes Fast; avoid rerunning both on unchanged bytes without a concrete reason.

Freeze the coherent source and safety changes, then follow `docs/missions/mounted-shield/PRE_NATIVE_REVIEW.md` for the independent read-only review before live use. Include the imported pin changes, rollback fix, gameplay driver, readers, and remaining findings in its scope.

The previous three supplementary reviews were not an owner-imposed three-round cap. Preserve the actual configured review and no-progress limits. Do not stop solely because three supplementary reviews already occurred, and do not fabricate readiness to make the controller launch a reviewer.

## 6. Complete native acceptance where admitted

When the actual loader, fixture, review, lock, budget, backup, and restoration gates permit, run the outstanding cases through the existing guarded path.

Use the full original acceptance ledger as the contract. Group related rows into practical scenarios rather than treating every row as a separate game launch.

The remaining evidence must cover:

- Native prerequisite and level-up behavior, including same-level picks, Back/cancel, respec, and the feat/icon presentation.
- Actual shield eligibility, enhancement exclusion, focus contributions, equip/unequip, active-set changes, and native shield suppression/restoration.
- Normal, touch, and flat-footed AC; competing shield bonuses; mount-sheet attribution; and the independent check bonus when another shield effect is stronger.
- Actual check-local contribution, equality/failure/success boundaries, existing reaction limits and costs, passive AC with the reaction off or spent, and relevant TB/RT behavior.
- Two-pair isolation, mount changes, dismount, disable/disposal, area transitions, save/load, and exact cleanup.
- Third-feat publication and relevant neighboring behavior.

Keep source, offline, native, UI, and restoration evidence distinct. Record the exact source, DLL, loader, fixture, commands, observations, and elapsed time.

Retain historical failed or blocked receipts. Add new evidence and dispositions without rewriting earlier outcomes. Never transfer gameplay evidence to a changed binary without a valid documented relationship.

Complete restoration even if a test fails or a budget expires. A smoke test cannot substitute for gameplay or UI acceptance.

## 7. Finish through the existing controller

After the required evidence is complete, prepare the schema-valid developer report, guarded-push, verify, and request the controller’s final review on the actual candidate.

Read and act on its returned findings and continuation in this same session. Continue ordinary in-scope corrections without asking me to relay review results.

Use `checkpoint_ready` only when the actual checkpoint requirements are met. Preserve unresolved required evidence as a blocker.

After an accepted `ready_for_owner_review`, run the supported `finalize` and `status --remote` steps. Report `AWAITING_OWNER_REVIEW` only when the controller actually returns it with a current handoff.

Update the Mounted Shield handoff, acceptance ledger, review dispositions, and current-state notes. Include:

- PR and enrollment.
- Incorporation of companion commit `5be0f737b1d40df6df4da886707d721801557ee9`.
- Actual controller endpoint and accepted review, if any.
- Reviewed source and final head, with their relationship.
- Build and test receipts with precise scope.
- DLL, package, and icon hashes and DLL MVID where produced.
- Native/UI evidence and restoration receipts.
- Any remaining blocker, including the exact external input needed.

Keep the PR in draft for owner review. Merge, release, normal-play installation, controller replacement, and changes to other enrollments remain outside this continuation.

The desired outcome is a completed, reviewed Mounted Shield candidate with the 0.31.1 baseline incorporated. If an external prerequisite still prevents that, the handoff must contain all useful completed work and identify that specific remaining prerequisite.
----- END OWNER AMENDMENT 1 -----

----- BEGIN OWNER AMENDMENT 2 (captured 2026-11-24T03:47:46Z; message sha256 69c17293474c68b2113e3efadbcf18472f303b63c54950004ddcc6046c9c4679) -----
Please resume the existing Mounted Shield mission and carry it to a reviewable private-alpha checkpoint.

This is an explicit owner amendment. I am changing the current acceptance milestone: prepare the feature and an installable private alpha package for my own testing. I will perform the broader in-game gameplay and UI acceptance during alpha testing.

I also authorize and instruct you to make a bounded attempt to create an appropriate disposable test save yourself. I do not need to provide the save, and recovering the exact historical save pair is not a requirement for this amended milestone.

## 1. Resume the existing work and record this amendment

Continue with:

- Repository: `howardreith/KingmakerMountedCombat`
- Branch: `codex/mounted-shield`
- Draft PR: #61
- Enrollment: `kmc-codex-mounted-shield-20261123114646-4433`
- Workflow budget: `mounted-shield`
- Last reviewed branch head: `2b5e4aef717e2258e2c9b7f70469cb63c0e9040a`
- Last implementation commit in that head: `cb1a881a5c6c8d3fe6b8c0e68c5a10e68b965177`

Inspect the actual current state before proceeding. Preserve any legitimate subsequent work.

Save this entire prompt verbatim in a new durable UTF-8 file outside the worktree. Apply it through the existing controller’s supported same-session `resume --amend-mission` mechanism, using the existing enrollment. Read the entire returned effective task, including all returned parts, and include its amendment citation in subsequent reports.

Preserve the existing enrollment, PR, findings, review history, failed evidence, and spent budgets. Do not edit controller state or code, start another enrollment, or reset counters.

Read the current repository instructions, validation policy, CURRENT.md, AUTONOMOUS-RESUME.md, and the Mounted Shield handoff and review records. This amendment supersedes earlier requirements that conflict with the private-alpha milestone; unaffected requirements remain in force.

Set `$env:KMC_WORKFLOW_BUDGET_ID = 'mounted-shield'` in every shell that runs budgeted validation, and confirm which ledger is actually charged.

## 2. Define the current acceptance milestone accurately

Required for this checkpoint:

- Complete the applicable source corrections and independent review.
- Pass the applicable current-source build, focused regression, loader-binding, icon, and package checks.
- Produce a private alpha package through the repository’s supported packaging path.
- Provide an accurate handoff, owner playtest instructions, known limitations, and exact artifact identities.
- Preserve runtime safety and complete restoration for any native work you actually perform.

The full native gameplay, persistence, compatibility, and UI acceptance matrix is deferred to my alpha playtesting. Keep those observations listed explicitly as outstanding.

For schema-backed reporting, represent genuinely deferred validation as `required: false` and `result: "not_run"`, with “deferred to owner private-alpha playtest” and the amendment citation in the explanation. Preserve historical failed and blocked receipts unchanged.

Do not call unperformed testing passed. Do not claim full feature acceptance, native qualification, or release readiness.

This amendment does not defer an observed correctness defect, unsafe runner behavior, failed restoration, or a mandatory controller gate. Those remain real findings. Where a prior finding depended on a requirement this amendment withdraws from the current milestone, use the supported evidence-backed rebuttal process and cite the changed requirement; do not falsely mark it fixed.

## 3. Close the current code and review work without expanding the mission

Preserve the existing feature design: one passive Mounted Shield feat, its existing prerequisites and amount rule, and the supplied icon. No second toggle or unrelated feature expansion.

Preserve UMM 0.31.1 as the build reference and declared minimum. Keep the separately identified installed UMM 0.33.0 runtime unchanged.

Review and record the closure of PN14-F1 through F4 on the actual current source. The latest external review found those corrections coherent, along with the prior second-rider preparation and cleanup fixes. Treat that as review input, not a substitute for an accepted factory result.

For PN14-F5, compare against the pre-mission implementation. The unrestricted forced-die behavior in the older Sprint 6 scenarios predates Mounted Shield, while the new Shield scenarios enforce legal dice. Seek an evidence-backed disposition that leaves recalibration of the older scenarios outside this alpha milestone unless you establish a concrete current regression or dependency.

Preserve historical receipts. If older diagnostics are corrected later, their new runs receive new receipts. Do not treat the existence of historical accepted runs as a reason that current diagnostics can never be improved.

Do not start unrelated refactors, recalibrate the entire older campaign, or build a general game-automation framework for this continuation.

## 4. Create a disposable test save yourself

You are authorized to create a fresh, isolated test campaign and generate a real save through the game’s native creation and save paths.

First inspect the available desktop-control and native automation tools. Establish what you can actually observe and control. Use existing supported tooling where practical.

Prefer the standalone Beneath the Stolen Lands campaign if the DLC is already installed, enabled, and suitable. The repository’s `NativeCampaignBootstrap.cs` already contains relevant Endless-DLC start selection, but inspect its actual prerequisites rather than assuming it is a complete character-creation robot.

Otherwise use the simplest suitable main-campaign route. A fallback should respond to a concrete obstacle, not start another exhaustive search.

Use the existing guarded launch/staging/restoration mechanisms and an explicitly isolated test save root or profile. Honor actual runtime locks and active sessions. Verify the save, profile, cache, settings, and mod locations that will be written; a different game installation directory alone does not establish isolation.

Create and retain two distinct things when feasible:

1. The original game-generated seed, unchanged.
2. A disposable prepared copy suitable for the basic Mounted Shield smoke check.

A useful initial seed needs a genuine campaign/player identity, a stable loaded area, a completed native save, and successful cold loading of a fresh copy. It does not universally need two riders or the historical Mammoth setup.

For a prepared smoke-test copy, aim for one valid supported rider–mount pair, with Mounted Combat, Shield Focus, Mounted Shield, and an equipped mundane heavy shield. The actual relationship and feat prerequisites must be valid.

Use ordinary character creation, level-up, and companion acquisition where practical. Existing supported fixture routines may supply XP or equipment on a separate disposable copy if that is the efficient route. Record every such preparation step and distinguish it from observations of ordinary character progression or feat selection. Do not grind through hours of combat merely to acquire test levels.

Record the save as **agent-generated and owner-authorized**. It is not owner-supplied, the historical Working save, or previously qualified producer output.

Record its actual creation method, campaign/DLC, game and mod versions, save hash and size, area and relevant actors, and preparation steps. Keep the master outside the writable save root and use fresh ordinary copies for subsequent loads.

If a new admission path is needed, make only a narrow, reviewed change for the actual new campaign and fixture. Preserve the older scenarios’ strict guards. Do not replace their historical hash literals, rename an arbitrary archive to impersonate the old fixture, or fabricate qualification evidence.

Porting the full historical Shield driver to the new fixture is not a prerequisite for this private alpha.

## 5. Bound the save-creation and native work

Use these ceilings within the existing remaining budgets:

- Up to 30 minutes of active seed creation, preparation, and narrowly necessary setup work.
- Up to 10 additional minutes for a basic smoke/cold-load attempt.
- No more than three guarded game starts in this continuation.

These are maximums, not targets or an extension of existing budgets. Use less when the task is straightforward. Stop starting new work at the limit; allow active saves and required cleanup/restoration to finish safely.

Do not repeat an equivalent failed attempt without a new observation or changed approach.

If tooling access, an unavailable campaign, or a reproducible infrastructure obstacle prevents creation within that bound, preserve useful output and report the specific limitation. Continue with the remaining alpha preparation. Do not turn the missing seed into another demand that I supply historical files.

If creating the save would require a substantial new UI automation system or a long campaign playthrough, stop that expansion and document the smaller missing capability.

## 6. Run a small smoke check if the setup is available

Prioritize:

1. The intended candidate starts with the selected loader and registers the feature without a new error.
2. A fresh copy of the generated save loads successfully.
3. A valid mounted rider with Mounted Shield, Shield Focus, and a mundane heavy shield gives the mount the expected +3 shield contribution, in a setup without a stronger competing shield bonus.
4. Unequipping the shield or dismounting removes the contribution.
5. If practical within the bound, save and cold-reload the prepared state, checking for successful loading and the absence of duplicate contributions.

Use actual game observations and available structured stat/log evidence, supplemented by screenshots. A menu launch alone does not establish Mounted Shield gameplay behavior.

If only the raw seed and its cold load are achieved, preserve them and report the feature smoke as unrun. If an actual defect appears, record and address it through the existing workflow; do not relabel a failure as deferred testing.

The comprehensive real-time/turn-based combat cases, legal check boundaries, enhancement exclusions, competing shield bonuses, multiple pairs, respec, shield bash/buckler behavior, death, area transitions, compatibility combinations, and complete UI inspection remain on my alpha checklist unless genuinely exercised.

## 7. Keep review bounded and tied to this amended scope

Before live use, obtain any independent source/safety clearance required for the current candidate and the runner changes you actually intend to execute. Reuse existing clearance for unchanged reviewed machinery where the workflow permits.

For supplemental pre-native review in this continuation, allow one focused closure/safety review and, if necessary, one coherent correction batch with a targeted recheck. Do not start another indefinite series of broad supplemental reviews.

Carry forward every applicable unresolved finding. Ask the reviewer to assess this amended alpha milestone, including current correctness and safety, without reinstating the deferred full gameplay matrix as a prerequisite.

If native work is unavailable, do not spend successive rounds qualifying a runner you cannot use. Complete the required source review and alpha handoff instead.

The normal final factory review still applies, with its existing counters and limits. A material unresolved defect or mandatory gate failure must be reported honestly; neither this amendment nor the review limit authorizes forcing a passing result.

## 8. Package the alpha and finish the handoff

Build and validate the current candidate through the existing supported path. Give the artifact a clear alpha identity, with consistent metadata and exact source, DLL, and ZIP hashes. Do not present an older package as the current candidate.

Run the applicable current-source gates. Keep the earlier broad Candidate failures and their baseline classification visible; do not convert them to PASS or waive a controller-required gate through this amendment.

Update the existing Mounted Shield records and provide:

- The private alpha ZIP and its exact identity.
- The actual current source/head and factory status.
- The independent review result and applicable finding dispositions.
- A concise record of checks actually executed.
- The generated seed and prepared test copy, if created, with provenance and qualification limits.
- A practical owner playtest checklist, prioritizing feat selection and icon appearance, +3/+4 amounts, enhancement exclusion, competing shield bonuses, Mounted Combat check contribution, equipment changes, dismount, and save/reload.
- Remaining gameplay/UI observations, clearly assigned to owner alpha testing.
- Installation and rollback instructions for my later manual use.

If no native testing occurred, label that prominently. If a package remains unqualified because of a real blocker, identify it as a diagnostic artifact rather than an install-ready alpha.

Submit a schema-valid developer report against the amended milestone. Use `checkpoint_ready` only when that milestone’s actual required conditions are satisfied.

After an accepted `ready_for_owner_review`, use the normal finalize and remote-status verification. Describe the resulting readiness specifically as readiness for owner review of the private alpha, with broader gameplay acceptance still pending.

If acceptance remains blocked, provide the completed artifacts and the precise current blocker without inventing a status or restarting the workflow.

Keep PR #61 in draft. Preparing this alpha does not authorize a merge, public release, tag, permanent normal-play installation, or changes to another project’s enrollment.
----- END OWNER AMENDMENT 2 -----
```

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
