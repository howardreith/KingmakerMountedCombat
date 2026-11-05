# F1 final review handoff

## Disposition

**F1: PARTIAL — READY FOR REVIEW; OWNER ACCEPTANCE SEPARATE.** The prepared finishing run completed natively with a PASS verdict and exact restoration, and every remaining selected observation was made. One concrete visual defect is open in UI-02: after a rank removal the Mounted Combat row keeps the black selected style, with a nearly unreadable label, although the selection really is cleared. All three F1 starts are used, so a corrected build needs a new owner decision.

No new manual test or campaign save is requested from Howie.

## Identities

- Repository howardreith/KingmakerMountedCombat; draft [PR16](https://github.com/howardreith/KingmakerMountedCombat/pull/16), branch `codex/kmc-finalize-f1-creation-ui-icons`. Integrated base `d2e26174d391a8c70fd159beed476035bb5f84eb`. Finishing-run execution head `a53e6275c033dc87c30390513ddc05242caddc91`.
- Frozen product source `39a153cf9524683d615f9efef60ff38c45fcd123`, version 0.1.0-chunk7b-preview.103, F1c. DLL SHA256 `8625d1fa64b6036bff6cafcce2a0f7a3f7ec16829b1306c012c697a0d13b3072`, MVID `89c5fdb6-6d04-499a-b072-762f96c4a23f`, 8,884,736 bytes.
- Main package, unchanged: lab `artifacts/KingmakerMountedCombat-0.1.0-chunk7b-preview.103-finalize-f1c-diagnostic.zip`, SHA256 `4d67c72e3f08c16ef655d262ca0d101b789ef1a86ce66153387b83e920ce7d39`. Exactly Info.json and the DLL. Product freeze `1c0889f3de44e4021cb547944064001a11c34e407794e3fc9a5decd026445360`; suite `finalize-f1c-39a153cf-20261104`, SHA256 `11072f7c478d64d4cef942f213f22a0296b6ef07857ac10a667f4fd513c3c950`.
- External observer, unchanged: 0.1.0-observer.7, producer `4454a7b3136d4245303a711d888d56b58b423cdf`, package SHA256 `a90df23f839209b2e196ecefb64f368d58e570d8ed0ba6d5efa4520438fca966`, DLL SHA256 `5c60d6ff428c05b095802c4a54032de4fad6beda4cb1355f2a6128bd4b254516`, MVID `d904cee8-a9b9-42f9-8cf7-a5b127d0308e`.
- Native inventory observed in UMM: Kingmaker 2.1.7b, UMM 0.33.2.0, KMC preview.103 F1c, Level-Up Selection Scroll Fix 1.0.0, KMC Removal Observer 0.1.0-observer.7, all active, 2560×1440. This is narrower than Howie's laptop stack.
- Scene `Manual_308_KMC_C7_SIX_HORSE_WORLD.zks`, SHA256 `b1e817a60c8bbe35b5947b4eab3b3f35060493ca20873d5de5fdf8a81efbc291`. Disposable rider `73681ef2-b281-4bc6-bc05-2de68c1616cb`, Horse `30b79228-3fe5-48ac-aa88-2de9085837b7`.

## Causal findings and outcomes

| Requirement | Causal boundary / correction | Outcome on native evidence |
|---|---|---|
| UI-01 | Routine engineering prose moved behind diagnostics; errors, version, settings, removal and save warnings kept | **Observed.** After real actions the ordinary page shows version, how-to, Charge setting, save-dependency warning, removal action and collapsed diagnostics. Limit: a genuine failure message was not provoked. The page also carried one sticky feedback line, "Mounted combat cancelled: ground command", from a ground click. |
| UI-02 | Same-index preview replacement missed grouping/layout; equal-value ReInit skipped new state and callback references. Narrow native sorting/layout and Init correction | **Grouping, clearing, cancel and fresh preview observed; one defect open.** Removing the rank moved Mounted Combat from the eligible group to the unavailable group, cleared the feat slot and showed a red prerequisite. Two cancellations and a fresh controller followed, with no inherited fact or selection, and a row click worked. **Open:** the removed row keeps the black selected style. Fresh previews do not show it. Cause not established. |
| ART-01 | Original distinct feat and reaction artwork | **Observed readable** in the feat row, slot and real toolbar, on and off. Owner art approval separate. |
| PERF-01 | Existing rendered-frame endpoint | Earlier descriptive samples retained: cold 38.22 ms, warm 10 mixed rows median 302.52 ms, worst 341.52 ms. **Howie accepted the missing matched baseline as an F1 limitation.** No improvement, no-regression or external-cause claim. No new timing was collected. |
| UI-03 | Keep native prerequisite, remove duplicate body line | **PASS,** retained from the second run and seen again: red native prerequisite, resolved body. |
| UI-04 | Distinct glyph and revised wording, same rank and economy | **Observed.** Real toolbar icon, resolved tooltip, and native off and on transitions. Presentation grant only; ordinary acquisition not proven here. |
| Charge neighbor | Register two localization keys | **Observed.** Mounted Charge tooltip fully resolved, no raw key. It remains a separate public ability until F2 owns the single-Charge interaction. |

Black row: the selection state was empty and the observer's `f1-rank-removed` passed, so this is a presentation fault, not a retained selection. The likely area is the row's toggle state surviving the rebind in `MountedFeatSelectorRefreshService`/native `Init`, but that is a hypothesis; I did not trace it. The product was not changed, because F1c is frozen and no start remains to confirm a fix.

Scroll: the list kept its position through a selection and returned to the top after Next from Skills. The new before/after refresh observer returned only its -99 sentinel on all six events, because no parent `ScrollRect` was found from the layer. It gives no scroll evidence, so a native or foreign reset writer remains **not causally established**.

## Evidence

[Native captures](evidence/finalize-f1/README.md), with [the finishing manifest](evidence/finalize-f1/finish-native-evidence.json) and [the earlier manifest](evidence/finalize-f1/native-evidence.json). Earlier images keep their original second-run identity. Static frames supplement actual inputs and the observer log; they do not alone establish asynchronous behavior.

| Check / attempt | Original outcome and limit |
|---|---|
| Earlier Candidate-b, final row Fast | 10 and 4 stages PASS on their original inputs; not replayed. |
| First and second native F1c launches | Both original **FAIL** (capture failure; 1100 s deadline). Partial observations retained at their identities. Their separate recoveries remain separate. |
| Corrected development WhatIf | PASS, 69 s, reused. It does not reach the request validator. |
| `20261105-finalize-f1-finish-01` | **FAIL before game start,** no actual start counted: `Schema-v2 scenario is outside the save-backed mission allowlist.` `finalize-f1-finish` was missing from `Test-RuntimeRequest.ps1`. Restored; 36 s charged. Evidence retained. |
| Validator correction `a53e6275` | One allowlist entry and one regression. Real validator on the refused request: 33/0, and it refuses an unlisted scenario. Focused finishing suite 23/0. Went live without a separate independent review, on a single-string change. |
| `20261105-finalize-f1-finish-02` | **PASS.** Third start. Observer checks 41/0; readers 33/0 and 29/0. Result SHA256 `578e79b3286efc8a9d97ac613c1ff9f29c8c462d4d9523e045cb6781db9c8bbd`, observer `d677e59c0e404f97dc8992657d950ad0f8aadfd5c0dfcba14f0bb427c46187a4`. |
| Publication boundary check | First run failed on its existing clean-worktree requirement after the image checks; retained on the meter. Rerun on the committed tree: see the PR. |

Run facts: the first creator was Elf/Rogue 1. The fresh creator came out Elf/Slayer 1 because my click landed one row off; this does not affect the fresh-controller check, but the fresh row interaction is Slayer, not Rogue. My first Ctrl+F10 did not register, and the following click became a ground command that moved the party a short way in the disposable scene. That is the source of the UMM feedback line. Native saves stayed blocked, and the combat-free scene rule held.

Game log: 3 errors and 1 exception, none attributed: one `NullReferenceException` and two AssetBundle duplicate-load errors. KMC log errors: 0.

Operation: Claude drove the game with synthetic Win32 input and GDI full-screen capture, refused unless the game was foreground. Howie authorized the screen takeover for this run.

## Review-worthy risks / explicit remaining work

- **Selected blocker:** the stale black row after rank removal. A fix changes the product and needs a new package, a new start and a separately authorized allowance.
- Owner art approval and broader owner-stack acceptance are separate. The owner's original report was on a larger stack.
- Limitation: scroll cause and performance comparison remain unestablished; a genuine failure message on the UMM page was not exercised.
- The in-memory grant proves presentation only, not ordinary feat acquisition.

## Spend and state

Authoritative **finalize-f1** record: **100 minute ceiling, 3/3 actual starts.** Spend was 83.95 minutes at this document's commit; the PR records the figure after publication, and [the checkpoint](evidence/finalize-f1/final-spend-after-finish.json) records the allowance history. Howie's 90→100 increase is appended; every earlier approval and entry is preserved. Finishing run charged 650 s, with 27 s of restoration excluded by the meter. Block ceiling and the single live executor held. No reset or further increase.

Restoration, read from run -02's own records:

- Mods digest `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` before and after (the live tree was empty at intake).
- Save inventory `60e3d6cc271af65408d8b0d7db6c132ca4f0f42f38eba4dee46589a21db9c2a1` before and after; baseline immutable; Working restored; write allowlist passed.
- 13 creation preferences restored exactly to this run's own intake; profile bytes exact, with no accepted cache changes.
- No game process, lock or installed candidate remains. Owner saves and foreign installations untouched.

[Publishing authority](evidence/finalize-f1/publishing-authority.json) records Howie's approvals. The helper gained exactly 8 further PNG path/hash lines; all other guards are unchanged.

## Next decision

One decision for the manager or Howie: fix the black row in F1 under a new bounded allowance, or accept it as a known limitation and close F1 into F2. Keep PR16 draft until then. The original small owner playcheck remains Mobility/selection/navigation plus art, copy and UMM. No merge, tag, release, permanent installation or F2 was performed.

Earlier chronology: [before finishing](history/FINALIZE-F1-HANDOFF-before-finish-20261105.md), [before native](history/FINALIZE-F1-HANDOFF-before-native-20261105.md), [implementation record](FINALIZE-F1-CREATION-UI-ICONS.md).
