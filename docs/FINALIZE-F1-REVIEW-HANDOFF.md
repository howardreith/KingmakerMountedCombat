# F1 final review handoff

## Disposition

**F1 ENGINEERING COMPLETE — READY FOR FINAL REVIEW; OWNER ACCEPTANCE SEPARATE.** The one requested-change outcome in UI-02 is repaired and confirmed on the actual screen: after a rank removal the Mounted Combat row is an ordinary readable unavailable row. The performance, scroll, owner-stack and art limitations below stay open as accepted or separate items.

No new manual test or campaign save is requested from Howie. PR16 stays draft for the reviewer. No merge, tag, release, permanent installation or F2 was performed.

## Identities

- Repository howardreith/KingmakerMountedCombat; draft [PR16](https://github.com/howardreith/KingmakerMountedCombat/pull/16), branch `codex/kmc-finalize-f1-creation-ui-icons`. Integrated base `d2e26174d391a8c70fd159beed476035bb5f84eb`. Review baseline `d81913dea5ebc8ee4854847ec9f502c04f4bf38f`. Execution head of the F1d run `84cc0c99392d2ef0b45c8326dcb22f084d9b5d9e`.
- **New candidate F1d:** source `8c5be488d11bb31b00a91b3213e18d12e617d250`, version 0.1.0-chunk7b-preview.103. DLL SHA256 `cd16000d24440d72fe9500fc630dc3d6ee7f71d13f6d09cc4c83d96fa0cc6a34`, MVID `d72ca1ea-02fc-4796-938a-608b38d73b22`.
- Package: lab `artifacts/KingmakerMountedCombat-0.1.0-chunk7b-preview.103-finalize-f1d-diagnostic-diagnostic.zip`, SHA256 `40c47185253fe2babb7e19e94954bb2b48d133f778880f7290bf23d78dc75eae`. The doubled suffix came from my qualifier; I kept the one build rather than rebuild for a name. Suite `finalize-f1d-8c5be488-20261105`, SHA256 `13dec54e92437da65e9c3e46ce6887a2e7502900561feb1daf0013f4bd543198`. Freeze `b77c58bc0f12c5d4ac0ecbb44934d9b70db3db6003acda155c8d60a04b311f35`; frozen worktree `finalize-f1d-8c5be488` retained.
- Observer 0.1.0-observer.8: producer `259d6106f34e9f5d1393c4e63b7d9dd91ca1717f`, package `KmcRemovalObserver-0.1.0-observer.8-finalize-f1d-diagnostic.zip`, SHA256 `b40a2c06908694c1d83e62c14627d6e64da7965f552a917e3e0c6793524079c2`, DLL SHA256 `8f478aced99656e203e5483e5da76db62119a2de49121a783597c8ea334e4561`, MVID `aee2d386-c516-4232-ba5d-5e31aa7ccb51`.
- **Retained, unchanged:** F1c source `39a153cf9524683d615f9efef60ff38c45fcd123`, DLL `8625d1fa64b6036bff6cafcce2a0f7a3f7ec16829b1306c012c697a0d13b3072`, package `4d67c72e3f08c16ef655d262ca0d101b789ef1a86ce66153387b83e920ce7d39`; observer.7; run `20261105-finalize-f1-finish-02` at its own 41/0 PASS and narrower scope. None of these is relabeled as F1d.
- Native inventory seen in UMM: Kingmaker 2.1.7b, UMM 0.33.2.0, KMC F1d, Level-Up Selection Scroll Fix 1.0.0, observer.8, all active, 2560×1440. This is narrower than Howie's laptop stack.

## Root cause and correction

**Cause.** The native light refresh clears rows with `Toggle.isOn = false`. Installed `Toggle.Set` turns the toggle back on when it is active, in a group, no other toggle is on and the group disallows switch-off. So the last selected row stays on, with its black graphic, when nothing replaces it. `Mark(Forbidden)` meanwhile gives the label its dark non-selected colour, which is why the row was dark text on black. Only the native full refresh calls `SetAllTogglesOff`. It was not a retained selection, event-system focus, hover or the disabled transition.

**Direct proof on F1d, run `20261105-finalize-f1d-native-01`.** An observer postfix ahead of KMC's recorded what the native refresh alone left, and the settled row after KMC's clear:

| Moment | toggleIsOn | interactable | groupAllowSwitchOff | graphicAlpha | label |
|---|---|---|---|---|---|
| Selected before removal | true | true | false | 0.93 (mid-fade) | RGBA(245,214,168) |
| **Native refresh left behind** | **true** | false | false | **1.0** | RGBA(55,55,55) |
| After clear, settled | false | false | false | 0.0 | RGBA(55,55,55) |
| Reselected | true | true | false | 1.0 | RGBA(245,214,168) |
| Fresh preview row | false | false | false | 0.0 | RGBA(55,55,55) |

**Correction.** `MountedFeatSelectorRefreshService.ClearStaleRowSelection` runs from the existing `FillFeatureDataLight` postfix. In a selection offering Mounted Combat, with nothing selected and a toggle still on, it calls the native `SetAllTogglesOff` under the native `IgnoreAction` guard. No callback, grant, skill or blueprint change; no repaint, per-frame work or new subscription. A valid selection is untouched, and a selected successor still uses native `NotifyToggleOn`. `FeatSelectorSelectionPolicy` holds the decision. The members are reached by exact token through reflection because the package allowlist keeps `UnityEngine.UI` out of the product's references, and I did not weaken it. Scroll Fix was not involved and is unchanged.

**Scope limit.** Selections not offering Mounted Combat stay native and would keep the same native fault. A native row losing its prerequisite inside the offering selection is covered by the state-transition regression, not exercised on screen.

## Evidence

[Captures](evidence/finalize-f1/README.md), with [the F1d manifest](evidence/finalize-f1/f1d-native-evidence.json). Failing-before evidence is the retained F1c frame [finish-removal-unavailable-tooltip.png](evidence/finalize-f1/captures/finish-removal-unavailable-tooltip.png), the "before the fix" regression, and the native residue row above. I inspected each F1d frame; numeric state does not by itself prove legibility.

| Check / attempt | Outcome and limit |
|---|---|
| Component regressions (9 new) | PASS within 1060/0: losing and regaining the prerequisite, valid rebind, native comparison, same fault on a native row, outside-scope selection, hidden row, new preview, no unguarded change. They model the installed contracts and reach the production decision; rendering stays a screen claim. |
| Assembly contracts | 756/0, including the exact group field and method tokens. |
| Fast / Candidate on `8c5be488` | Fast 5/0 on each pass. Candidate 7/0, 3.95 min. The first build failed because I referenced `UnityEngine.UI`; that attempt is retained on the meter. |
| Independent read-only review | No blockers; causal reasoning and fix held. 5 should-fix items fixed before freeze: native residue now required, stale reads latch, filter instruction and live status, Next/Back enforced, fixture and guard tests updated. Not repeated. Its note on an Animator re-assert did not occur: the settled reads stayed clear. |
| Exact-request preflight | Real `Test-RuntimeRequest.ps1` accepts the next request's exact shape and refuses an unlisted scenario and a wrong DLL. Focused suite 42/0; identity boundaries 33/0. |
| Fourth-start guard | Offline: fourth admitted and marked, fifth refused, history preserved. Cap raised once, from three to exactly four. |
| Development WhatIf | PASS, 31.5 s. Not whole-lab qualification. |
| **`20261105-finalize-f1d-native-01`** | **PASS.** Fourth and final start. Observer 48/0; readers 33/0 and 29/0. Route: rank 0 → rank 1 → select → Next/Back → remove → inspect → regain → reselect → Next/Back → cancel → fresh Rogue → Alertness then Blind Fight → cancel. Result SHA256 `eb737e28b164be2968c32b7dc294c6fda1859a697832700740f287334c5ab704`, observer `922b3d45ffb66e3c903566fde39e115ea3d4927632c633157b0614dd46579827`. |
| Earlier outcomes | UMM page, prerequisite and body formatting, art, reaction on/off and Charge keys stay at their recorded runs, chiefly `finish-02`; not rerun on F1d apart from the disposable scene's reaction toggle. |

The presentation grant still proves presentation only, not ordinary feat acquisition. The reader at this head requires observer.8's row state, so it does not re-evaluate `finish-02`; that run's original verdict stands as recorded.

**Log classification.** F1d's one exception is `BugReportCanvas.OnEnable`, from the stack the observer now keeps: a native startup path, unrelated to feat rows. KMC errors 0. `finish-02` kept only the message line, so its exception is unclassified at its own identity, though it has the same count and order. The earlier F1c log also shows a native `DollRoom.SetFogOfWarEnabled` exception at exit. The two duplicate AssetBundle errors recur and are unclassified; no repair selected.

## Review-worthy risks / explicit remaining work

- No selected blocker remains.
- **Accepted:** no matched performance baseline. Earlier samples stay descriptive; no improvement, no-regression or foreign-cause claim, and no new timing.
- **Limitation:** the `-99` ScrollRect probe gives no valid scroll observation, so a native or foreign reset is not established. Visibly, the list kept its place through Next/Back and returned to the top coming back from Skills.
- **Separate:** owner art approval; owner-stack acceptance on Howie's larger mod stack.
- **F2 owns** the single public Charge and persistent Mount/Dismount control; Mounted Charge remains a separate ability.

## Spend and state

Authoritative **finalize-f1**: **120 minute ceiling, 4/4 actual starts.** Howie's 100→120 and three-to-four-start authorization is appended; every earlier entry is preserved. Spend was 84.22 before this work, 100.17 at the F1d run's end, and the PR records the figure after publication. Run charged 368 s with 27 s of restoration excluded. Full 1320 s was available at admission. [Checkpoint](evidence/finalize-f1/final-spend-after-f1d.json). Block and active-work limits and the single live executor held. No fifth start or further increase.

Restoration, read from this run's own records:

- Mods digest `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` before and after (the live tree was empty at intake).
- Save inventory `60e3d6cc271af65408d8b0d7db6c132ca4f0f42f38eba4dee46589a21db9c2a1` before and after; baseline immutable; Working restored; write allowlist passed.
- 13 creation preferences restored exactly to this run's intake; profile bytes exact, no accepted cache changes.
- No game process, lock or installed candidate remains. Owner saves and foreign installations untouched.

Operation: Claude drove the game with synthetic Win32 input and GDI capture, refused unless the game was foreground. Howie authorized this screen takeover separately. [Publishing authority](evidence/finalize-f1/publishing-authority.json) records his approvals; the helper gained exactly 4 further PNG path/hash lines and no other guard changed.

## Next decision

Final review of PR16. The owner playcheck stays small: Mobility, selection and navigation, plus art, copy and UMM, with art approval separate. A corrected build needs an approved handoff; this workflow installs nothing.

Earlier chronology: [before F1d](history/FINALIZE-F1-HANDOFF-before-f1d-20261105.md), [before finishing](history/FINALIZE-F1-HANDOFF-before-finish-20261105.md), [before native](history/FINALIZE-F1-HANDOFF-before-native-20261105.md), [implementation record](FINALIZE-F1-CREATION-UI-ICONS.md).
