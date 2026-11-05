# F1 review handoff

## Disposition

**F1: PARTIAL — READY FOR REVIEW; OWNER ACCEPTANCE SEPARATE.** UI-03 has direct native confirmation; UI-02 and ART-01 have substantial creator evidence, and PERF-01 now has actual rendered-endpoint measurements. Cancellation/fresh-preview, reaction toolbar, Charge tooltip and ordinary in-game UMM inspection remain selected work, so this is not engineering completion.

This is Codex-operated testing. The prepared run/directory names contain “owner” because they predate the owner's request for autonomous operation; no observation is attributed to Howie.

## Identities

- Repository howardreith/KingmakerMountedCombat; draft [PR16](https://github.com/howardreith/KingmakerMountedCombat/pull/16); branch codex/kmc-finalize-f1-creation-ui-icons. Integrated base d2e26174d391a8c70fd159beed476035bb5f84eb. Native execution head fe1a0514a458890ded4681b53e3cc1b9f9ca7992; later changes are recovery/tests/reporting only.
- Frozen product source **39a153cf9524683d615f9efef60ff38c45fcd123**, version0.1.0-chunk7b-preview.103, qualifier finalize-f1c. DLL SHA256 **8625d1fa64b6036bff6cafcce2a0f7a3f7ec16829b1306c012c697a0d13b3072**, MVID **89c5fdb6-6d04-499a-b072-762f96c4a23f**,8,884,736 bytes.
- Existing package `C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.1.0-chunk7b-preview.103-finalize-f1c-diagnostic.zip`, SHA256 **4d67c72e3f08c16ef655d262ca0d101b789ef1a86ce66153387b83e920ce7d39**. Exactly Info.json and mod DLL under KingmakerMountedCombat/, original icons embedded. Manifest SHA256 **5d78e232981d251d362d21c737d058abd794d87c05c194560e0f7b5ddd0ec22e**. No rebuild this continuation.
- Suite finalize-f1c-39a153cf-20261104 SHA256 **11072f7c478d64d4cef942f213f22a0296b6ef07857ac10a667f4fd513c3c950**. Freeze analysis-cache/finalize-f1-contracts/f1c-product-freeze.json SHA256 **1c0889f3de44e4021cb547944064001a11c34e407794e3fc9a5decd026445360**; detached product worktree finalize-f1c-39a153cf.
- Actual second-start stack: **Kingmaker2.1.7b, UMM0.33.2.0, KMCpreview103-F1c and Scroll Fix1.0.0,2/2 enabled**,2560x1440, Elf/Rogue level1 preview. Structured loader inventory exists for this run. This is distinct from Howie's incompletely inventoried laptop stack.
- Engine Assembly-CSharp SHA256 **3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb**; UMM SHA256 **f6f7e18aff88910b01d23e3ce0829f3b4188c6784b0fcea56f9b3af8aa14d0a9**. Scroll Fix DLL SHA256 **ca5c1cf454d1a66e44dc067dae2023c1d13cca3143730545e1ec5b317e440136**.
- New run **20261105-finalize-f1c-owner-creator-01**, actual start2026-11-05T03:43:04.3617922Z. Evidence under lab runtime-evidence/same-run. Retained output log SHA256 **30e6a38275ef65fe11ed7a949f3f9ec1ff746217e24c1c87984da52b8aaebf46**. Raw game result SHA256 **ba99cb4489595e6230b1818da1a512ecb4d042b5fec4ce6a0e8d8600efaf56e1**; original outer result **e765aa6339fe0c8ed4a2a4fc407281b25bf7c48c984330a536244e1fd5df17c0**.

## Causal findings and correction

| Requirement | Before / causal boundary | Change | Direct outcome |
|---|---|---|---|
| UI-01 | Accepted/active UMM feedback exposed RiderMelee and movement authority terminology | Routine allowlist behind Diagnostics; errors/refusals/unknown feedback, version/settings/removal/save warnings retained | Source/focused PASS retained. Ordinary in-game page after actions TODO. |
| UI-02 | Same-index preview replacement skips grouping/layout; native equal-value ReInit can retain old state references | Narrow native sort/layout invalidation and Init on replaced equal-value KMC offering; no new listeners, facts or per-frame redraw | Native rank0 red unavailable → rank1 eligible alphabetical M group → select → Next → Back: usable list and KMC selection retained. Rank removal cleared selection. No disappearance observed in exercised path. Cancellation/fresh preview and exact regroup after removal TODO. |
| ART-01 | Shared saddle did not distinguish the feat | Original combat-ring emblem; source/export/consumer identities in assets-source/finalize-f1/manifest.json | Native ordinary/disabled/selected feat row and tooltip icon readable. Native reaction toolbar TODO; owner art approval separate. |
| PERF-01 | Owner lag report had no attributed timing | Existing bounded current-preview rendered endpoint | Cold1=38.22ms; warm10 median302.52ms,worst341.52ms. Same lab configuration. No matched baseline/control, so no regression or external-cause PASS. |
| UI-03 | Duplicate body prerequisite | Native trained Mobility1 metadata retained; redundant body line removed | Native tooltip shows red Mobility:1 at rank0 and green at rank1, with no repeated body prerequisite. Missile Shield provides native neighboring prerequisite comparison. PASS for observed formatting. |
| UI-04 | Shared reaction artwork and awkward reaction prose | Original defensive glyph and revised copy, economy unchanged | Feat tooltip copy resolves natively. Reaction toolbar/on/off inspection TODO. |
| Charge neighbor | Two missing localization keys | Existing-family registration |23 consumer-key source check retained; native Charge tooltip TODO. No F2 mechanic work. |

The black selected row is the ordinary native selected style, separately observed from the reported disappearing list. Scroll returns to the top after selection/re-entry on this stack; this invalidated the intended fixed-coordinate MountedCombat/Martial alternation. Neighboring Alertness/Armor Proficiency selections still worked. The ten warm samples are changed native feat rows, not ten verified Mounted Combat reselections. No claim of preserved scroll position or a foreign root cause is made.

Scroll Fix owner **KingmakerLevelUpSelectionScrollFix**, SetupFeatureSelection postfix. Native log shows one wrapper creation, reused by subsequent setups (first five postfix invocations logged). It does not directly patch ReInit. Foreign source/install unchanged. No clean A/B was spent because no disappearing-list failure was reproduced here; this does not close owner-stack acceptance.

## Evidence

All new captures are original normal-resolution2560x1440 PNGs, opened and inspected. Small review set in **C:\Dev\KingmakerMountedCombatLab\artifacts\finalize-f1-owner-operated-1\captures**:

| Frame | Direct observation |
|---|---|
| creator-021.png | Mobility0, red unavailable Mounted Combat, native prerequisite and resolved copy |
| creator-022.png | Native Missile Shield prerequisite comparison and unrelated unavailable rows |
| creator-024.png | Mobility rank1 in skill editor |
| creator-026.png | Mounted Combat in eligible alphabetical group |
| creator-027.png | Selected dark row, acquired feat icon and green prerequisite |
| creator-032.png | After actual Mounted Combat select/Next/Back: nonempty list and selected top feat icon |

creator-029.png separately shows the intervening Character page. creator-033.png shows the nonempty list with selected icon cleared after rank removal. Static frames supplement the actual navigation inputs and native log; no frame alone proves asynchronous correctness. Images remain local review artifacts, not hosted GitHub attachments. The approved push helper's binary allowlist was preserved; browser attachment host was unavailable. The local handoff links these files directly.

Timing evidence: lab runtime-evidence/20261105-finalize-f1c-owner-creator-01/**observed-ui-and-timing-summary.json**, with raw log, sample values, image paths/hashes and endpoint. First SetFeature rendered sample38.22ms; next ten comparable selector changes341.52,312.32,324.65,302.13,315.55,300.46,295.13,302.91,296.17,299.16ms. Additional reselection306.38ms is outside that ten-sample summary. Endpoint: **end-of-rendered-frame-current-preview-bound-rows**,160 bound rows, selected/current preview references and layout/canvas completion checked. It is a Unity rendered-frame endpoint, not a physical display scanout timestamp.

Separate processing bound: first SetFeature nextFrameMs41.33; next ten median272.70ms,worst309.49ms. Do not substitute it for rendered latency. No allocations were measured; no FPS target is inferred. Shader fallback warnings and BugReportCanvas.OnEnable NullReferenceException remain in the raw log, without causal attribution.

| Evidence | Original result / identity |
|---|---|
| Candidate-b |10 stages PASS,4.05min; predates final row/endpoint corrections. Includes Fast. |
| Final row/endpoint Fast |4 stages PASS,0.62min;1051 component,149 source,777 assembly checks. Retained on original bytes. |
| Branch/accounting/localization | Prior focused results retained; exact branch10 cases, package11 assertions. Original BOM fixture failure and F1b package outer-status failure retained. |
| Copied-PDB recovery |8 positive/negative cases and independent review; first-start original FAIL retained, separate restoration receipt. |
| Second-start preflight | Current frozen-product/current-harness/package/suite/WhatIf checks passed; no Candidate replay. |
| Second native start | **FAIL**: observer1100-second deadline. Only one creator opened; no two-close PASS, no cancellation/fresh-preview acceptance. |
| Creator preference recovery | Initial normal restore refused changed key set. One-run exact recovery: focused8/0, WhatIf PASS, independent safety review and execution PASS. Initial recovery check exposed PowerShell array serialization; corrected. First execution refused its own exclusive lock read before writes; corrected held-lock check. Both failures retained in transcript/log record. |

Launch accounting retains preflight time/failures while counting only actual start attempts, including failed starts. No budget history was removed.

## Review-worthy risks / explicit remaining work

Selected blockers remain: cancellation/fresh preview without inherited facts/callbacks; reaction toolbar/on/off; resolved Charge tooltip; ordinary UMM page after actions; credible performance regression attribution. Owner art approval and broader owner-stack acceptance remain separate.

The creator route correctly refuses campaign areas. The qualified Manual308 normal-control fixture lacks the feat/toggle and automatically drives/exits; older feat/Charge routes enter combat or the explicitly excluded legacy fixture. No existing inspected route covers both missing creator and presentation observations in one remaining start. Combining them needs a narrowly reviewed external observer route, focused verification, packaging and WhatIf, not a blind launch or a renamed legacy scenario. The earlier assertion that a suitable presentation route was already pinned was incorrect.

At53.50/75min, only21.50min remains before any further validation. Full1200s final reservation leaves90s for all required preparation; the prior creator plan also reserved120s preparation. No credible current qualified finishing plan fits. No third start was consumed and no allowance/batch/cap was silently raised. This is a preparation/allowance gap, not an outstanding Full Access permission or a request for another settings recovery loop.

## Spend and state

Same authoritative finalize-f1 record: **53.50/75min,21.50min remaining,2/3 actual starts** after retained-log extraction. Owner explicitly approved60→75; adjustment appended with original history and cap intact. Capture-restored-1 was activated only after successful Windows pixel capture and visible harmless Explorer selection; changed-autonomous-batch.json preserves earlier failures/corrections. Owner-operated-1 was not activated.

Latest native validation entry charged the full interval through deadline/readers (approximately18.85min); restoration excluded by existing policy. Subsequent exact recovery and its checks are cleanup, not another native attempt. Current continuation authoring/review and cleanup remain separately recorded; no parallel live executor. No product rebuild, historical campaign or optional clean launch.

Second-run Mods and protected saves restored to current snapshots: Mods digest **e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855**, save metadata digest **60e3d6cc271af65408d8b0d7db6c132ca4f0f42f38eba4dee46589a21db9c2a1**. Baseline immutable; Working restored; save allowlist passed and suite content checked. Profile snapshot SHA256 **90588536e15c3116194a4b82272cc706c12f7c23e97d47dba38f0bd0b50b27c5**. Exact one-run recovery restores14 captured preferences (including two removed native keys) and original UMM parameters; separate reviewed-preferences-restoration.json. This run's profile had zero accepted cache changes and restores exactly to its own intake.

First-run original native/restoration FAIL remains unchanged; separate reviewed-symbol-restoration.json records recovery. Its **seven admitted native cache deltas** remain qualified and are not erased by second-run exact restoration to a newer intake. Do not claim the whole historical profile byte-exact.

No Kingmaker process, active transaction lock or installed candidate remains. No human save, foreign mod or other product was modified. Production package remains frozen and unchanged.

## Next decision

Keep PR16 draft. Reviewer must select a bounded continuation that fits both the missing native path and existing safety/accounting constraints; this handoff does not authorize extra starts, a new batch or increased allowance. Do not impose the known engineering preparation gap on Howie as a new campaign-save/test request. The eventual small owner playcheck remains Mobility/select/navigation plus art/copy/UMM; no merge, release, permanent installation or F2.

Earlier detailed results and original chronology remain in [the preserved pre-native handoff](history/FINALIZE-F1-HANDOFF-before-native-20261105.md) and [implementation record](FINALIZE-F1-CREATION-UI-ICONS.md).
