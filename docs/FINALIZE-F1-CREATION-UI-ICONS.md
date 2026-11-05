# F1 creation/UI/icons — IN PROGRESS

Selected by Howie on 2026-11-04. Base: integrated preview.103 `d2e26174d391a8c70fd159beed476035bb5f84eb`, verified against fetched origin/main. Branch: `codex/kmc-finalize-f1-creation-ui-icons`. Root worktree and its unknown empty untracked MountedCombatDefenseService.cs preserved.

Package inspected under `C:\Dev\KingmakerMountedCombatLab\analysis-cache\finalization-intake\KMC_Finalization_Package`; original ZIP and screenshots retained outside Git. Read master, backlog, rules, delivery, art brief, evidence, owner notes and F1 mission. Recent efficient-development practices are in VALIDATION-POLICY.md (Oct 16–17 acceleration): focused regressions, one frozen Candidate, dependency-scoped runtime audits, immutable evidence and cumulative budget, two equivalent failures, one executor.

Budget: `finalize-f1`, 60 minutes cumulative validation, maximum 3 real launches, 30-minute block and four-hour active-work ceilings. Root override required because this worktree is nested: `KMC_WORKFLOW_BUDGET_ROOT=C:\Dev\KingmakerMountedCombatLab\runtime-state\workflow-budgets`. Reserve final confirmation plus packaging/WhatIf before optional checks. No native launch yet.

## Intake and causal boundaries

- No Kingmaker process; Steam running. Live Mods directory empty at intake. No KMC installed DLL or foreign scroll-fix assembly is available there. Owner screenshot stack is different; exact inventory/DLL location requested once while independent work continues. Do not claim this empty installation is the owner stack.
- Native Assembly-CSharp SHA256 `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`; UMM SHA256 `f6f7e18aff88910b01d23e3ce0829f3b4188c6784b0fcea56f9b3af8aa14d0a9`.
- Bounded local selector decompilation stays outside Git under `analysis-cache\finalize-f1-contracts`. Native same-selection FillData calls FillFeatureDataLight without refreshSort except when selection index changes. Light refresh updates eligibility/callbacks but does not regroup sibling rows by default. This explains a plausible stale grouping path; not yet a reproduced full disappearance cause. Native animated full refill captures selection state across an asynchronous fade; inspect preview replacement and foreign hook before adding overlap.
- Charge keys are referenced by the blueprint but absent from its registration dictionary (direct cause of unresolved keys).
- Mounted feat uses native PrerequisiteStatValue and duplicates its prerequisite in body. Feat and reaction share saddle image. Keep stable IDs and reaction economy.

## Next

Finish original icon exports and consumer bindings; inspect native preview lifecycle and foreign scroll hook when supplied. Focused compilation then independent review before any live use. UI-02/PERF-01 need actual visible sequence and timing; source inspection cannot close them. No selected outcome is yet PASS.

## 2026-11-04 implementation checkpoint

Owner supplied exact preview.103 ZIP and Level-Up Selection Scroll Fix ZIP in handoffs. KMC DLL SHA256 `0687e6fdeb2d079db70bf516506f85c1e72e6d303fe9cb6c7819e6d08159abfa`. Scroll DLL SHA256 `ca5c1cf454d1a66e44dc067dae2023c1d13cca3143730545e1ec5b317e440136`, Info version 1.0.0, Harmony owner `KingmakerLevelUpSelectionScrollFix`; patches CharBSelectionSwitchFeatures.SetupFeatureSelection postfix. Wraps collection-switcher ItemsContainer in ScrollRect once, updates content/viewport size, preserves normalized scroll position, forces layout. It does not patch CharBSelectorLayer.FillFeatureDataLight (the row regrouping path). Bounded decompilation remains local-only.

Working code includes creator diagnostic session under existing runner, not a new campaign fixture. It skips the Working load, waits for two fresh creator cancellations, requires no loaded area, then performs normal settled completion/restoration. Metrics sample input entry to following frame after queued preview work; they are cheap CPU/frame bounds, not GPU presentation timestamps. CUA frames must establish actual visible correctness. A named session PASS alone establishes two cancellation lifetimes, never UI acceptance. Guarded owner-input staging retains exact source/hash/folder checks and never labels the owner download as a producer run. Three real-launch cap is explicit for finalize-f1. No native launch yet.

Independent review found and corrected remaining routine UMM prose, a scalar runner-default typo, an accidentally broadened registration-audit list and nested navigation timing reset. Focused owner-input safety checks: seven positive/negative cases PASS. Fast #01 PASS (1050 component +149 source assertions); #02 compile FAIL preserved, corrected; #03 PASS; #04 source-policy FAIL caught accidental audit-list entry, corrected. Cumulative meter is authoritative; ~2.1 min at this checkpoint. No old evidence reattributed.

## Narrow native refresh correction (source-backed; visible acceptance TODO)

Exact installed method `CharBSelectorLayer.FillFeatureDataLight(FeatureSelectionState,bool)` token `0x06009F79`. `LevelUpController.UpdatePreview` replaces Preview and creates a new LevelUpState, then sends its late-update command. `CharBFeatureSelector.FillData` treats the same selection blueprint as reusable, passing false for refreshSort unless the index changed. Thus a replaced preview at the same index rebinds eligibility but retains old sibling grouping and omits UpdateLayout. The adapter asks this existing native light refresh to sort only when a new selection-state instance replaces the old one for the same KMC-offering selection; resets its native per-pass filter accumulators; schedules native layout invalidation. No custom sort, scroll reset, full library scan, every-frame redraw, blueprint mutation or added subscriptions. Foreign switcher hook is separate and unchanged.

Regression scene: rank 0 -> rank 1 -> select -> Next -> Back, rank removal and cancel; second fresh creator must have no selected Mounted Combat fact or inherited callbacks. Compare native prerequisite feat and unrelated rows. Prepared F1 session is not a source-only proof of this scene. Disappearance after navigation remains unproven until visible replay; do not conflate it with the source-proven stale grouping path.

Computer-use blocker observed before any launch: both Sky and unified CUA report missing Windows native pipe (os error 2); prescribed retry/reset failed. Owner asked to reconnect once while independent source/package work continues. No launches consumed or runtime surfaces staged.

## Current contract / risk / qualification boundary

| Contract | Implementation and evidence | Status |
|---|---|---|
| Same-index preview replacement | Native sorting/rebinding; conditional native layout update, exact token/MVID; only selections still publishing KMC feat | IN PROGRESS — visible scene pending |
| Saved identity / economy | Existing feat, toggle, buff and control GUIDs and prerequisite rank unchanged; no action/resource code touched | PASS — source review |
| Ordinary UMM failures | Routine persistence status behind Diagnostics; refusals/errors, removal outcome, save dependencies visible | IN PROGRESS — native page pending |
| Original assets | Editable offline vector recipe, 512px masters, 64px RGBA runtime icons; no runtime authoring/dependencies; source/export hashes in assets-source/finalize-f1/manifest.json | PASS — technical source/export inspection; owner approval TODO |
| Foreign compatibility input | Supplied Scroll Fix 1.0.0 exact pinned DLL and cloned read-only folder; original producer-provenance checks intact; seven safety cases plus launch-limit neighbors pass | PASS — offline only |
| Native/visible acceptance | No game launched, no measured latency, no native frames; original owner screenshots retained with original identities | TODO |

Concrete risks: the source-proven grouping correction may not resolve the distinct asynchronous disappearance report; scroll preservation needs native observation; current timing bound is input-to-next-frame after queued preview work rather than a GPU timestamp. The new observer's lifecycle PASS alone is not UI acceptance. Locale reload is not yet observed. No F2 controls/mechanics or save-schema changes are selected.

Gunslinger reference inspected: approved rapid-reload 64px export, SHA256 `5c3280145815f7b161602da161264ac1f43fb3ac3c4bf47ff907b0919a2aa9d3`, under `assets-source/original-icons/icon-overhaul-v2/pilot/exports/rapid-reload.png`; guides read through authenticated read-only GitHub API. Reference pixels remain outside Git. Owner S03 native Missile Shield ring was also inspected; no reference pixels copied.

## 2026-11-04 final local checkpoint — IN PROGRESS

Compiled source fd7f64f56ad39bf35b66b2e1cea3bd4638628e1f; final DLL SHA256 4608aa2643dc40e12f15b4cd80124ce8a2b0710ef11e2933df689ea4b60c32c1, MVID 9922f48f-3d64-46a2-a1d9-d935e7a3c387, 8,880,128 bytes. Immutable package `KingmakerMountedCombat-0.1.0-chunk7b-preview.103-finalize-f1a-diagnostic.zip` SHA256 407b553cfe61fcf415a0054ea9d859966b29c501a3e997595e3eb11c3bb87f72. Exactly Info.json and mod DLL; no foreign/proprietary payload. Original source retained at worktrees/finalize-f1a-fd7f64f5.

Candidate-a passed 8 stages (3.96 min) before selector correction; later focused pass passed 5 stages (0.53 min), 23 localization keys and assembly-backed 776 assertions. Final narrowed publication-state check compiled by clean package build; package validation 11 PASS. No old Candidate result is attributed to this binary's native behavior. Clean and owner-scroll WhatIf both PASS. Initial WhatIf refusal for manual-only artifact parameters retained as FAIL; corrected ordinary package/suite parameters passed without weakening guards.

Meter checkpoint: 10.12/60 min; 0/3 real launches. No installation mutation/transaction/restoration. Current UI setting is enabled (owner confirmed), but this session cannot start its Windows sandbox helper (`codex-windows-sandbox-setup.exe`, program not found); independent trusted Node route fails too. Native/visible/performance acceptance remains TODO. No repeated settings/fixture requests.

Automatic approval review rejected direct push and requires the exact guarded feature-branch helper. Existing helper is stale for F1's exact branch/worktree/new art; original unchanged. Reviewed narrow proposal and read-only positive/negative tests exist outside product source; owner approval pending. Full template-based handoff with exact identities/outcomes/limits: `C:\Dev\KingmakerMountedCombatLab\artifacts\finalize-f1-review-handoff.md`. No PR yet, no release selected.

## Guarded publication resolved (2026-11-04)

Howie approved the reviewed push-guard update and draft PR. Original helper backed up; approved helper SHA256 fad5336008c771f72852f04216933d4d00ee991c73ed10dba26c350819775343. Exact helper WhatIf PASS, then guarded push verified remote 4832086d8a42ce274cc32d52952d9cfeb54f5eec. Draft PR #16: https://github.com/howardreith/KingmakerMountedCombat/pull/16. Compiled source fd7f64f5 independently confirmed published through GitHub's commit API. Later handoff documentation does not change DLL/package identity.

Meter 10.13/60 minutes at publication; real launches 0/3. One UI client retry after approval still failed to start codex-windows-sandbox-setup.exe (program not found). Native acceptance remains TODO. No runtime transaction/installation replacement/restoration. No new build, Candidate replay or expanded campaign selected.

## F1 continuation: corrected candidate preparation (2026-11-04)

Owner-selected Full Access is authorized; only the obsolete global blanket prohibition was replaced in codex-home/AGENTS.md, retaining all project launch/save/restoration/publication guards. Desktop CUA connection recovered after a fresh runtime reset: current Explorer handoffs window observed and Escape input succeeded before any game staging. No further settings request is needed.

Actual-launch cap now uses an atomic actualGameStartAttemptUtc marker immediately before Start-Process. Preflight reservation failures retain time and verdict, but consume no actual launch; a failed start consumes one. Focused isolated accounting and compatibility tests passed. Historical entries and equivalent-failure rules are unchanged.

Routine PlayerActions/Combat feedback is hidden behind Diagnostics by an exact allowlist; errors, refusals and unknown feedback remain visible. Version, settings, removal outcome and save-dependency warnings remain ordinary UI.

Native ReInit token 0x06009FA8 compares FeatureSelectionState by value. A replacement preview equal by value can therefore leave a row bound to the old state. The narrow prefix reuses native Init with the current caller callback only for this skipped KMC-offering case. Other rows retain native behavior. Independent source review found no remaining concrete regression; visible acceptance remains required.

The observer now times SetFeature through the rendered frame with current-preview reference-bound rows, settled native layout and visible canvas alpha, targeting the clicked selection/item in either selector pane. The older next-frame timer stays separately labeled as a preview-processing bound. No source/observer result alone establishes UI acceptance.

Candidate-b initial pass: 10 stages PASS, 4.05 minutes, including accounting/compatibility/localization, 1051 component and 149 source assertions. It predates the final row/observer review corrections; affected compile/contracts follow separately. Meter at that pass: 14.22/60 minutes, zero actual game-start attempts. F1a binary and its original evidence remain immutable. New product code requires a new F1b package; no old native result will be relabeled.

Affected final row/observer verification: Fast 4 stages PASS (0.62 min), 1051 component,149 source,777 assembly-backed assertions including the new exact ReInit contract. Focused Test-AssemblyContracts used its default Both (read-only local Kingmaker/Wrath inspection); no Wrath launch/mutation. Meter14.83/60 min,0 actual starts. Log logs/finalize-f1b-row-focused.log.

F1b prelaunch FAIL retained: request validators still required the historical branch prefix. No actualGameStartAttemptUtc marker; zero starts. runtime-result.json confirms modsRestored=true and saveProtectionPassed=true. Both validators now additionally admit only exact selected F1 branch (independent review complete). Fast compile/components/source passed; focused request fixture initially failed for UTF8 BOM, then corrected no-BOM fixture passed all10 external/compiled branch cases. Source change requires F1c; F1b remains immutable. Meter18.03/60 min,0/3 actual starts. Package wrapper's invalid completion status remains original FAIL despite successful package11/0.
