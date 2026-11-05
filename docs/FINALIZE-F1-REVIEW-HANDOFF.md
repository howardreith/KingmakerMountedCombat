# F1 final bounded review handoff

## Disposition

**F1: PARTIAL — READY FOR REVIEW; OWNER ACCEPTANCE SEPARATE.** The product corrections and external finishing route are implemented, and retained native images/timings are now published with their original session identities. The last native confirmation was not executed: preparation consumed the margin needed for the mandatory1200-second reservation; cancellation/fresh-preview and in-game presentation acceptance remain incomplete.

This was Codex-operated work. No new manual test or campaign save is requested from Howie.

## Identities

- Repository howardreith/KingmakerMountedCombat; draft [PR16](https://github.com/howardreith/KingmakerMountedCombat/pull/16), branch codex/kmc-finalize-f1-creation-ui-icons. Integrated base **d2e26174d391a8c70fd159beed476035bb5f84eb**. Review implementation through **00c419af**, followed by reporting/comment-only changes. Original second native execution head **fe1a0514a458890ded4681b53e3cc1b9f9ca7992**.
- Frozen product source **39a153cf9524683d615f9efef60ff38c45fcd123**, version0.1.0-chunk7b-preview.103, F1c. DLL SHA256 **8625d1fa64b6036bff6cafcce2a0f7a3f7ec16829b1306c012c697a0d13b3072**, MVID **89c5fdb6-6d04-499a-b072-762f96c4a23f**,8,884,736 bytes.
- Unchanged main package: lab artifacts/**KingmakerMountedCombat-0.1.0-chunk7b-preview.103-finalize-f1c-diagnostic.zip**, SHA256 **4d67c72e3f08c16ef655d262ca0d101b789ef1a86ce66153387b83e920ce7d39**. Exactly Info.json and DLL under KingmakerMountedCombat/. Original embedded icons; no game assemblies or reference assets.
- Product freeze **1c0889f3de44e4021cb547944064001a11c34e407794e3fc9a5decd026445360**; suite finalize-f1c-39a153cf-20261104 SHA256 **11072f7c478d64d4cef942f213f22a0296b6ef07857ac10a667f4fd513c3c950**. Detached product worktree finalize-f1c-39a153cf retained. No new main package or Candidate replay.
- Prepared external observer:0.1.0-observer.7, producer **4454a7b3136d4245303a711d888d56b58b423cdf**. Lab artifacts/**KmcRemovalObserver-0.1.0-observer.7-finalize-f1-finish-c-diagnostic.zip**, SHA256 **a90df23f839209b2e196ecefb64f368d58e570d8ed0ba6d5efa4520438fca966**; manifest SHA256 **c5299b57d4464b49b133b4494e7550e0d18e7501d88402eff3778f44765113d9**. DLL SHA256 **5c60d6ff428c05b095802c4a54032de4fad6beda4cb1355f2a6128bd4b254516**, MVID **d904cee8-a9b9-42f9-8cf7-a5b127d0308e**,90,624 bytes. Exactly observer Info.json/DLL. Never staged or run in this continuation.
- Prepared run **20261105-finalize-f1-finish-01**, new explicit scenario **finalize-f1-finish**. Scene **Manual_308_KMC_C7_SIX_HORSE_WORLD.zks**, SHA256 **b1e817a60c8bbe35b5947b4eab3b3f35060493ca20873d5de5fdf8a81efbc291**,1,332,308 bytes. Source: closed batch185 checkpoint, reused through its existing16 literal evidence/archive/restoration pins.
- Exact disposable rider **73681ef2-b281-4bc6-bc05-2de68c1616cb**, Horse **30b79228-3fe5-48ac-aa88-2de9085837b7**; neighboring pair c4e2c853-4b72-4904-ad84-d0b7bfba7470 /0e0c7d2d-06b4-4510-9f81-d847340d1424. Expected new stack is KMC + observer + supplied Scroll Fix1.0.0. This is a prepared configuration, not a loaded-inventory observation.
- Actual retained native evidence uses **Kingmaker2.1.7b / UMM0.33.2.0 / KMCpreview103-F1c / Scroll Fix1.0.0**,2/2 enabled,2560×1440, Elf/Rogue1 preview. Engine SHA256 **3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb**, UMM **f6f7e18aff88910b01d23e3ce0829f3b4188c6784b0fcea56f9b3af8aa14d0a9**, Scroll Fix **ca5c1cf454d1a66e44dc067dae2023c1d13cca3143730545e1ec5b317e440136**. This differs from Howie's broader laptop stack.

## Causal findings and correction

| Requirement | Causal boundary / correction | Actual outcome |
|---|---|---|
| UI-01 | Routine accepted/active PlayerActions/Combat engineering prose behind Diagnostics; errors/refusals/unknown feedback, version, real settings, removal and save warnings preserved | Source/focused PASS retained. Ordinary in-game UMM after actions TODO. |
| UI-02 | Same-index preview replacement missed grouping/layout; equal-value native ReInit skipped new state/callback references. Narrow native sorting/layout and Init correction, no blueprint/fact trick or added listeners | Actual rank0 unavailable→rank1 eligible→Mounted Combat select→Next→Back had usable list/retained selection; rank removal cleared selection. Exact removal regrouping, cancellation and fresh interaction TODO. |
| ART-01 | Original distinct feat/reaction artwork; source/export/consumer identities in assets-source/finalize-f1/manifest.json | Ordinary/disabled/selected feat row and tooltip readable natively. Reaction toolbar inspection TODO; owner art approval separate. |
| PERF-01 | Existing current-preview rendered endpoint; no FPS target or comparison invention | Descriptive evidence retained: cold1=38.22ms; warm10 mixed native-row changes median302.52ms,worst341.52ms. **Missing matched baseline accepted by Howie as F1 limitation.** No improvement/no-regression/external-cause PASS. No new timing campaign. |
| UI-03 | Keep native trained Mobility1 prerequisite; remove duplicate body prerequisite | **PASS:** actual red/green native formatting and resolved body copy; native Missile Shield comparison retained. |
| UI-04 | Distinct reaction glyph and revised wording, same trained rank/economy | Feat description resolves natively. Reaction tooltip/on-off toolbar TODO. |
| Charge neighbor | Register two existing-family localization keys |23 consumer-key source check retained; actual tooltip TODO. No new permanent public action or F2 mechanic. |

Scroll reset trace: native FillFeatureDataLight invokes native ReInit, availability/filter/sibling ordering and SetupFilter; KMC's narrow replacement path uses native Init and UpdateLayout. Native Init clears/rebinds each affected row; none of these inspected KMC methods explicitly assigns scroll position. Scroll Fix owns SetupFeatureSelection's separate collection-switcher wrapper and preserves its known normalized position. A native/foreign reset writer is **not causally established**, so no product change or foreign blame was made. The external finishing mode adds bounded before/after light-refresh scroll observations, but they have not run. The black selected row is native selection styling, separately observed from disappearance.

The new external mode preserves the original creator scenario's no-area rule. It separately requires rank1→0 with a disabled current-preview-bound row, two cancellations and a new controller without inherited feat/selection before loading Manual308. It blocks native saves, grants only the exact real Mounted Combat feature to the declared rider in memory as disclosed presentation setup, uses native Mount, observes native reaction transitions, holds for UI inspection and exits through a visible finish button or1100s failure deadline. It does not prove ordinary acquisition.

Own-intake preference restoration is scoped to this new finishing request/token and the observed14-key creation/session family. Unknown changes refuse. The old one-run recovery hashes are not authority for the new run. Existing profile routing, transaction/suite/save/Mods protection, native identities and restoration remain in place.

## Evidence

[Six openable native captures](evidence/finalize-f1/README.md) and the [small timing/image manifest](evidence/finalize-f1/native-evidence.json) are tracked for PR review. They come only from **20261105-finalize-f1c-owner-creator-01**, product39a153cf/executionfe1a0514. None is labeled execution on the later observer. Static frames supplement actual inputs/navigation and the native log; they do not alone establish asynchronous correctness.

The first rendered sample was38.22ms; next10 were341.52,312.32,324.65,302.13,315.55,300.46,295.13,302.91,296.17,299.16ms. Endpoint: end-of-rendered-frame-current-preview-bound-rows,160 bound rows, settled layout/canvas and current selection references. This is a Unity rendered endpoint, not physical display scanout. The intended fixed-coordinate KMC/Martial alternation was disrupted by scroll reset, so these are mixed-row selections. Separate nextFrame processing bound: cold41.33ms; warm median272.70ms,worst309.49ms. No allocations or FPS conclusion.

Retained log SHA256 **30e6a38275ef65fe11ed7a949f3f9ec1ff746217e24c1c87984da52b8aaebf46**. Shader fallback warnings and BugReportCanvas.OnEnable exception remain without attribution. Original game result SHA256 **ba99cb4489595e6230b1818da1a512ecb4d042b5fec4ce6a0e8d8600efaf56e1**; outer result **e765aa6339fe0c8ed4a2a4fc407281b25bf7c48c984330a536244e1fd5df17c0**.

| Check / attempt | Original outcome and limit |
|---|---|
| Prior Candidate-b / final row Fast |10 stages /4 stages PASS on their original inputs;1051 component,149 source,777 assembly checks retained, not replayed or reattributed. |
| F1c package/accounting/branch/localization | Existing11 package assertions and focused results retained. Original BOM and outer package-status failures preserved. |
| First native F1c launch | Original FAIL from capture failure. Original copied-PDB restoration failure and separate exact symbol review/recovery preserved. |
| Second native F1c launch | **FAIL:**1100s deadline before cancellation/fresh preview. Partial native observations/timings remain usable within stated scope. |
| Second-run preference recovery | Normal restore refused creation preference changes; separately reviewed pinned recovery restored14 original values and UMM settings.8 focused cases; original array/held-lock refusals preserved. |
| New finishing route | Observer compiles;22 focused request/reader/preference/source-pin checks and52 legacy normal-player neighbors PASS. Offline only. |
| New observer packaging | Initial missing IMGUI reference, dependency allowlist refusal and reflection-loaded DLL lock failures retained. Exact Unity UI module references added; fresh isolated packaging PASS. Main package unchanged. |
| Finishing WhatIf | Two whole-lab attempts interrupted before staging; full370.8s and273.3s charged. I omitted the existing DevelopmentScopedAudit option; broad historical archive hashing was the avoidable cause. The initial fixture-gate diagnosis was incomplete. |
| Corrected development-scoped WhatIf | **PASS**,69s; runtime-state/live Mods content and saves checked; run-owned archive surfaces only. Not whole-lab qualification. No game/staging/lock. |
| Publication / source admission |17 positive/negative checks PASS for six exact PNG path/hash exceptions, preserved old art registry, exact frozen observer reuse and unchanged legacy admission. Full1320s reservation refusal also PASS before staging/start. |

Source/packaging distinction is preserved: the exact observerC package may now survive docs/external-runner descendants only with its fixed package/DLL/producer pins, clean ancestor relation and unchanged entire compiled observer/import tree. Legacy controls retain exact-HEAD admission. No observer rebuild is needed for these reporting changes.

## Review-worthy risks / explicit remaining work

Selected native blockers remain: correct regrouping after rank removal; cancellation and a genuinely fresh usable feat preview; reaction toolbar/art/tooltip/on-off; resolved Charge tooltip; ordinary UMM after actions. No current source blocker was found by independent review, but the combined mode is **offline/WhatIf-qualified only**, not native-confirmed.

The accepted performance limitation closes the request for a matched comparison here; it does not turn these samples into a regression, improvement or external-cause finding. Owner art approval and broader owner-stack acceptance remain separate.

The finishing wrapper defaults to the selected development WhatIf and requires full1200s+120s preparation before any live invocation. It correctly omits the WhatIf-only audit switch for live execution; the runner rejects that switch on live runs and hashes historical archive roots only in its declined-ShouldProcess branch.

## Spend and state

Same authoritative **finalize-f1** record: **72.45/90min,17.55min remaining,2/3 actual starts** at the final engineering-guard checkpoint. Publication audit accounting is appended separately. The initial60→75 approval remains; the final75→90 approval is appended, never substituted.53 entries then; zero running/unreconciled, two explicitly reconciled interrupted WhatIf entries. No reset, shortened reservation, fourth start or automatic increase.

Full1200s no longer fits; the wrapper's1320s upfront check refuses before staging. The third actual start is unspent. This is an avoidable preparation/admission failure, **not** an outstanding Full Access, host-capture or human-operation prerequisite.

Latest continuation began04:30UTC; authoring/review/validation wall span is recorded separately from earlier work and owner idle, within the four-hour active-work ceiling. Native blocks remain below30min and there was one live executor. No historical campaign, compatibility matrix or new timing campaign was selected.

No live stage/scene/feature grant occurred in this continuation. The corrected WhatIf proved current Mods/saves unchanged. No game, transaction lock, save worker or installed candidate remains. The earlier second-run restoration remains exact to its own intake: Mods digest **e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855**, save metadata **60e3d6cc271af65408d8b0d7db6c132ca4f0f42f38eba4dee46589a21db9c2a1**, snapshot **90588536e15c3116194a4b82272cc706c12f7c23e97d47dba38f0bd0b50b27c5**. Baseline immutable, Working restored, protected content suite-checked. Separate reviewed-preferences-restoration.json retains native FAIL and records14 settings restored.

First-run original FAIL/restoration findings and quarantine remain. Its separate reviewed-symbol-restoration.json and **seven admitted profile-cache differences** are not erased by later exact restoration to a newer intake. No blanket historical byte-exact profile claim.

[Publishing authority](evidence/finalize-f1/publishing-authority.json) records the owner's exact-image authorization, prior/new helper hashes and six exceptions. Every other publishing guard is preserved.

## Next decision

Keep PR16 draft. No further live execution fits the selected allowance/reservation; no further increase or owner test is requested automatically. The original small owner playcheck remains Mobility/selection/navigation plus art/copy/UMM after engineering correction, with art approval separate. Stop at review: no merge, tag, release, permanent installation or F2.

Earlier detailed chronology remains in [the preserved handoff](history/FINALIZE-F1-HANDOFF-before-native-20261105.md) and [implementation record](FINALIZE-F1-CREATION-UI-ICONS.md).
