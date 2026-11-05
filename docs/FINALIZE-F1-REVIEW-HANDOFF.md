# F1 review handoff — IN PROGRESS

F1 is partial. The requested accounting and presentation corrections, preview fixes and focused checks are finished. Native acceptance for UI-01, UI-02, ART-01, PERF-01, UI-03 and UI-04 remains TODO: Windows frame capture fails for Explorer and Kingmaker. Owner art approval is separate. Keep PR #16 draft.

## Identities

- Repository howardreith/KingmakerMountedCombat; branch codex/kmc-finalize-f1-creation-ui-icons; [PR #16](https://github.com/howardreith/KingmakerMountedCombat/pull/16). Base d2e26174d391a8c70fd159beed476035bb5f84eb. Product source **39a153cf9524683d615f9efef60ff38c45fcd123**; subsequent e60fee3d2e78499be28488d246949a9223279c4e changes external harness/recovery only. Later documentation does not change product bytes.
- Version 0.1.0-chunk7b-preview.103, qualifier finalize-f1c. DLL SHA256 **8625d1fa64b6036bff6cafcce2a0f7a3f7ec16829b1306c012c697a0d13b3072**, MVID **89c5fdb6-6d04-499a-b072-762f96c4a23f**, 8,884,736 bytes.
- Package `C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.1.0-chunk7b-preview.103-finalize-f1c-diagnostic.zip`, SHA256 **4d67c72e3f08c16ef655d262ca0d101b789ef1a86ce66153387b83e920ce7d39**. Exactly Info.json and mod DLL under KingmakerMountedCombat/, including embedded original icons. Manifest SHA256 **5d78e232981d251d362d21c737d058abd794d87c05c194560e0f7b5ddd0ec22e**.
- Suite finalize-f1c-39a153cf-20261104, snapshot SHA256 **11072f7c478d64d4cef942f213f22a0296b6ef07857ac10a667f4fd513c3c950**. Clean detached product worktree `C:\Dev\KingmakerMountedCombatLab\worktrees\finalize-f1c-39a153cf`. Freeze `analysis-cache\finalize-f1-contracts\f1c-product-freeze.json`, SHA256 **1c0889f3de44e4021cb547944064001a11c34e407794e3fc9a5decd026445360**. Use existing FrozenProductSource admission after harness/docs commits; preserve the package.
- Engine Assembly-CSharp SHA256 **3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb**, MVID 07fa1e4d-8618-41b3-9b8d-faa17d3b26f7. Actual startup log reports **Kingmaker 2.1.7b / UMM 0.33.2.0**, KMC and Scroll Fix1.0.0 active,2/2 loaded. UMM SHA256 **f6f7e18aff88910b01d23e3ce0829f3b4188c6784b0fcea56f9b3af8aa14d0a9**. Structured loader inventory was not produced.
- Supplied Scroll Fix DLL SHA256 **ca5c1cf454d1a66e44dc067dae2023c1d13cca3143730545e1ec5b317e440136**; selection `analysis-cache\finalize-f1-contracts\owner-scroll-selection.json`. This lab stack differs from Howie's incompletely inventoried laptop (screenshot UMM0.33.0 and extra mods).

## Causal correction and requirement disposition

| Requirement | Before / causal boundary | Change and outcome |
|---|---|---|
| UI-01 | Routine accepted/active feedback exposed RiderMelee and movement-authority prose | Exact routine allowlist moves it behind Diagnostics. Errors/refusals/unknown feedback, version, settings, removal and save warnings remain visible. Behavioral check PASS; ordinary native page TODO. |
| UI-02 | Source: same-index replacement skips grouping/layout. Native ReInit compares selection states by value, so equal-value replacement can leave old row state/tooltip references. Owner disappearance not reproduced. | Conditional native sort/layout plus exact ReInit prefix delegates skipped replacement to native Init with current callback. Native comparator, identity and unrelated rows retained; no full redraw, added listeners or facts. Actual rank/select/Next/Back/removal/cancel/fresh/control sequence TODO. |
| ART-01 | Shared saddle did not identify feat | Original combat-ring emblem, cached export; exact identities in assets-source/finalize-f1/manifest.json. Technical source/export review PASS; native row/selected/disabled/sheet TODO; owner art approval separate. |
| PERF-01 | Lag report on broader stack; no causal timing | Bounded SetFeature endpoint reaches rendered frame with current-preview-bound rows, settled layout and visible canvas alpha in either pane. Older next-frame bound stays separately labeled. Cold=0,warm=0; median/worst unavailable. No performance PASS or external blame. |
| UI-03 | Manually repeated prerequisite | Preserve native one-trained-Mobility-rank metadata, remove duplicate body line. Source/key checks PASS; actual tooltip formatting TODO. |
| UI-04 | Shared reaction art and awkward wording | Distinct defensive glyph and concise setting copy; economy unchanged. Source/export review PASS; native toolbar/on/off TODO. |
| Charge neighbor | Two referenced localization keys absent | Register both in existing family;23 consumer keys checked. Actual tooltip TODO. Single-Charge behavior stays F2. |

Scroll Fix Harmony owner KingmakerLevelUpSelectionScrollFix patches CharBSelectionSwitchFeatures.SetupFeatureSelection postfix. Its collection-switcher scroll wrapper does not directly patch the row light refresh. Foreign code/installation unchanged.

Launch accounting commits actualGameStartAttemptUtc immediately before the start callback. Preflight failures retain failure/time without consuming a real launch; thrown starts count; three actual attempts refuse a fourth. Historical entries, reservations and equivalent-failure policy remain intact. Both request validators additionally admit only the exact selected F1 branch; foreign/case/suffix variants are refused.

## Evidence, including original failures

| Evidence | Result and identity boundary |
|---|---|
| Candidate-b |10 stages PASS,4.05min;1051 component/149 source plus accounting/compatibility/localization. Predates final row/visible-endpoint corrections. Candidate includes Fast. |
| Final row/endpoint focused Fast |4 stages PASS,0.62min;1051 component,149 source,777 assembly checks. Direct assembly script used default Both,read-only; no Wrath launch/mutation. |
| Exact branch fix |Fast build/components/source PASS; focused fixture initially FAIL for BOM; corrected no-BOM test10/0 PASS. New product source39a153cf. |
| F1c package |Clean build and11 package assertions PASS. F1a/F1b remain immutable. F1b outer completion-status error remains recorded FAIL despite successful package creation. |
| Symbol cache safety |8 positive/negative cases PASS; independent review; exact recovery WhatIf and execution PASS. Modified/orphaned/missing-source symbols and wrong DLL cache refused. External harness only. |
| F1b preflight |Original FAIL for old branch prefix; zero actual starts; Mods/saves restored. Evidence `runtime-evidence\20261104-finalize-f1b-creator-scroll`. |
| F1c creator WhatIf |Development-scoped PASS on exact package/suite with supplied Scroll Fix. Not native acceptance or whole-lab purity. |
| F1c actual launch |Original FAIL; actual start2026-11-05T00:43:34.9629865Z. Normal game exit before atomic result because screenshot host failed. No creator inputs, timings or screenshots. |

Retained startup log `runtime-evidence\20261104-finalize-f1c-creator-scroll\retained-output-log-after-exit.txt`, SHA256 **2c55f55b5df7073dce14d99b21098f0ca68edcc119677f94e3f1ed61ec8b8b86**. Confirms versions/active mods and retains a native BugReportCanvas.OnEnable NullReferenceException without assigning cause or claiming a clean control.

Original art is openable at [pilot-thumbnails.png](../assets-source/finalize-f1/pilot-thumbnails.png). No current native frames exist; neither art pilot nor owner screenshots substitute for new-build visual evidence. Older qualification remains labeled at its original identity in the implementation record.

## Concrete external blocker

Fresh CUA recovered inventory, accessibility and keyboard input. Explorer observation and Escape succeeded before staging. A pixel capture should also have been verified before launch; accessibility alone did not establish game supervision. Kingmaker capture returned `FrameArrived timed out`, then `window capture timed out: timed out waiting on channel`. Alt+Enter did not recover it. Explorer fails identically after the game exits and after refreshing its binding. No blind gameplay inputs were sent; Alt+F4 closed the test game normally.

All independent corrections/checks are finished. Asked once for a full ChatGPT desktop restart with Windows unlocked/display on and a ready reply. No Any App setting change requested. First proof after recovery must be an actual Explorer screenshot plus harmless input before further staging. The computer-use skill prescribes refreshing selection and one retry, then reporting the exact capture error; no unofficial runtime or alternate UI protocol used.

## Spend and restoration

Authoritative meter `runtime-state\workflow-budgets\finalize-f1.json`; checkpoint **22.23/60min,37.77min remaining,1/3 actual starts**. Prelaunch branch failure has launch=true reservation but no actual-start marker. All failures/time retained. Restoration time excluded by existing runner. Roughly45min continuation work plus prior recorded~42min, separated from owner idle; four-hour active ceiling remains. No hidden allowance/parallel executor/shortened1200s reservation.

Original F1c runtime result remains FAIL with modsRestored=false: copied PDB was originally unexpected. Live Mods had already been restored byte-exact. Exact reviewed recovery preserves original combined bytes, runtime result and Mods findings, reclassifies unchanged quarantine, proves current suite/protected saves/Mods, writes a separate receipt and closes the stale lock. Current combined lifecycle is restored; native verdict unchanged.

- Review receipt: `runtime-evidence\20261104-finalize-f1c-creator-scroll\reviewed-symbol-restoration.json`; original combined copy `combined-before-symbol-review.json`, SHA256 **e731c0c08bb672c05a8fa2994aa8ab3d906a872380cb2d329846ef7024caba82**.
- Original Mods state SHA256 **3bca2b6a00ae43b2e5bdacf32713ff27922fbcf846ad1fbaf53e6a60925b2ff9**. Quarantine retained, digest **4feba695ffd72d2ebfc8843e37c0df3b0d94b1320f703aff59f15d2990dc7a0d**. Cached/original PDB both **b3b3342d7008304590d8735a8367db2b0ca4a1ab9f8a3979e9515b6bd4bb29e2**.
- Current Mods digest **e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855**; protected-save metadata digest **60e3d6cc271af65408d8b0d7db6c132ca4f0f42f38eba4dee46589a21db9c2a1**. Protected content proved against suite; baseline immutable, Working restored, allowlist passed.
- No Kingmaker process, live executor, sentinel/lock or installed candidate remains. Profile is not byte-exact: **seven** admitted achievement/analytics cache deltas are enumerated in profile-cache-changes.json, with profileBytesRestoredExactly=false under existing policy. Do not claim all caches restored exactly.

## Next decision

Keep draft. After capture recovery, reuse unchanged F1c product/freeze with updated harness, fresh run-owned IDs and current WhatIf. Read newer meter before work. A successful Explorer frame is the required new observation before another native attempt. Existing batch now contains two differently caused failures; do not reset its guard silently. Any materially changed supervised experiment needs an explicit documented batch disposition while retaining finalize-f1 cumulative allowance and1/3 actual starts. Reserve last1200s native confirmation plus preparation.

Finish the creator sequence/cold+8–10 warm rendered-endpoint timings, then one bounded existing disposable scene for reaction/Charge/ordinary UMM. No fixture hunt, owner campaign-save request or broad combat campaign. Owner check remains original Mobility/select/navigation plus art/copy/UMM, with separate art approval. Stop at review; no merge/tag/release/permanent install/F2.

Final read-only frozen-product/current-harness and restored-admission checks PASS at harness009766fb. No further build or native attempt. Final meter22.23/60min,37.77min remaining,1/3 actual starts.

## F1c continuation capture gate (2026-11-05 01:39 UTC)

Verified clean reviewed head f93245b28c8183e41c304f05c0e3071644af3aab and existing finalize-f1 meter before execution. Explorer pixel capture failed with `window capture timed out: timed out waiting on channel`. After a fresh CUA runtime reset and current-window rebind, the retry failed with `FrameArrived timed out: timed out waiting on channel`. No readable frame or visibly verified harmless input was obtained. No staging, game start, package rebuild, offline campaign, or settings change occurred.

The owner's conditional batch finalize-f1:capture-restored-1 is selected only after successful pixels; it was NOT activated. The prior branch-admission and game-capture failures, their corrections and separate restoration review remain intact. Frozen product/package/freeze remain F1c. Metered8.9 seconds of reported tool wall time as a capture-FAIL accounting import (completed import is not a PASS verdict). Current meter22.38/60min,37.62min remaining,1/3 actual starts. Evidence: analysis-cache/finalize-f1-contracts/capture-prerequisite-20261105T013914Z.json. Native acceptance remains TODO; host capture recovery is still required. No repeated settings request or further launch is authorized on accessibility alone.

Owner-operated creator preparation PASS at docs head1d73bcf5: development-scoped WhatIf for20261105-finalize-f1c-owner-creator-01 on unchanged F1c and supplied Scroll Fix; native run/evidence directory remains absent. Log logs/finalize-f1c-owner-creator-whatif.log. Meter22.72/60min,37.28min remaining,1/3 starts. Final1320s planning reserve retained. Manual Explorer image/operator availability and exact suitable presentation-scene reference remain prerequisites; neither owner-operated-1 nor capture-restored-1 activated. No product build/Candidate replay/installation/launch/CUA retry.
