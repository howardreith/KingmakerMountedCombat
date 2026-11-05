# F2 review handoff

## Disposition

**F2: PARTIAL — READY FOR REVIEW; OWNER ACCEPTANCE SEPARATE.** ART-02, CTRL-01, CTRL-02 and CTRL-03 are implemented. Every in-game behaviour except one was observed on candidate F2c: the persistent Mount / Dismount slot, first placement and full-bar safety, old-slot repair, ordinary Charge mounted and unmounted, and refusals. The exception is the toolbar repainting right after an automatic placement, which r03 showed was missing. The fix is on corrected candidate F2d, which has not run natively; all three starts are used. The save → reload round trip of the slot has not run natively either.

Nothing was merged, tagged, released or installed. No owner test is requested for the known repaint defect; F2d is the corrected candidate.

## Identities

- Repository howardreith/KingmakerMountedCombat. Branch `codex/kmc-finalize-f2-canonical-controls`. Base `66e96f90` (the F1 PR #16 merge, head `8c7d3722`).
- **Corrected candidate F2d (not run):**
  - source `7879e4634a45e8698e53cb784bc6e9e6016f35ef`, version 0.1.0-chunk7b-preview.103
  - DLL SHA256 `70b557461f19b499a52db221a2050c94cf15ed70d2a0883c6658f7886b8a4285`, MVID `4f1f4f79-7ee5-4bc6-9ebd-b6188919cdf0`
  - package lab `artifacts/KingmakerMountedCombat-0.1.0-chunk7b-preview.103-finalize-f2d-diagnostic.zip`, SHA256 `9c51a9b3c07f95cfec24ae901ff216555691574a8608eb2e28885d7626abdb32`
  - suite `finalize-f2d-7879e463-20261105`, SHA256 `b82fd612d13d49d044e2942359ae67e66e8ba77a80fc3d7b4efde61bd182420b`
  - WhatIf `20261105-finalize-f2-w04` PASS.
- **Native runs**, each at its own identity:
  - r01: F2a, source `c19dcc44`, DLL `3737cf87…`, package `e86befde…`.
  - r02: F2b, source `5017fe0e`, DLL `58aa7257…`, package `5af0d1fd…`.
  - r03: F2c, source `66bed248`, DLL `21a637d7…`, package `9b5297d9…`, suite `finalize-f2c-66bed248-20261105`.
- **Environment:** Kingmaker 2.1.7b. Base-game-only KMC staging plus the retained Working-fixture dependencies (CotW 1.14.4c, CraftMagicItems), not Howie's laptop stack. Real-time Working fixture: rider with Mastodon, plus a hostile.

## Identity and dispatch

See [the identity table](FINALIZE-F2-CANONICAL-CONTROLS.md).
- **New public control:** Mount / Dismount (`26151090…`).
- **Hidden delivery shells:** Mount `f053…`, Dismount `3af2…` and the Mounted Charge shell `d79e…`. Every saved GUID still resolves.
- **Charge routing:** stock Charge (`c78506dd…`) routes to the shell for the mounted rider.
- **Unchanged:** the retired primaries and the reaction toggle.

## Causal findings and correction

| Requirement | Before | Change | Direct outcome |
|---|---|---|---|
| CTRL-01 | S10: mounted stock Charge refused with "use Mounted Charge". S08: a separate public Mounted Charge with unknown keys; F1 added the keys. | The stock Charge's availability, reason and target come from the mounted evaluator for the rider. Its slot click selects the hidden shell. A stock Charge command for a mounted pair is cancelled, never converted. The shell is hidden and labelled "Charge", and its reasons read "Charge …". | r03: the Charge slot selected the shell, which held the selection at end of frame. One shell command; one admission at delivery; Success with one rider attack. The mount paid nothing; rider Standard 3.59 s. Spent refusal: "Charge requires the rider's standard action." Unmounted: native stock Charge ran, Success, one attack, mounted admission unchanged. |
| CTRL-02 | S09: Mount and Dismount were separate facts that appeared and disappeared with state. | One public fact stays leased in every state. The slot click runs the current shell through the native `OnClick` body. The intent is the shell blueprint, and the existing shell binding refuses a stale one. A direct cast is refused before cost. | r03: dismount and remount through slot 13, watched for 24 frames. The control was never absent, and its slot index and fact never changed. A direct cast was refused with no cost. |
| CTRL-03 | Never placed. | Placed once into the first empty main-bar slot after restoration, with the native autofill setting on. Never overwrites. Records persist as archive kinds 6/7. Removal is remembered. Old kinds 1/2 restore as the public control, kind 5 as stock Charge. | r03: the fixture's full main bar was left intact. After one slot was freed (disclosed), the first placement went exactly there with no other slot changed. A deliberate removal was not reinserted. Old Dismount and Mounted Charge bindings were repaired in place to the public control and stock Charge. **Defect:** the placed control was in the bar data but not on screen until a later rebuild. Fixed on F2d (bar marked dirty after KMC writes); not run natively. |
| ART-02 | Dark saddle. | Original horse, rider and two-way arc emblem from F1's pilot, from `scripts/Export-MountStateIcon.ps1`. | Visible in r03's mounted and target-selection frames. Owner art approval is separate. |
| Strings | Locale reload could drop owned keys. | Keys are registered again when the native pack is replaced. | r03: every KMC control key resolves; names are "Mount / Dismount" and "Charge". |

## Evidence

Frames from r03, under lab `runtime-evidence/20261105-finalize-f2-r03/movement-visuals/`. Not committed: new tracked images need Howie's publishing approval.
- `finalize-f2-public-control-unmounted.png`: shows the repaint defect. Slot 13 still draws the removed ability.
- `finalize-f2-public-control-mounted.png`: the control in slot 13.
- `finalize-f2-charge-target-selection-mounted.png`: the Charge shell selected, with the control in slot 13.

| Run | Original verdict | Re-evaluation |
|---|---|---|
| r01 (F2a) | FAIL. The fixture rider's main bar is full, so placement correctly wrote nothing and the scenario had no slot to click. | None. The experiment was changed for r02. |
| r02 (F2b) | FAIL: "not injected on the main bar". | Two reader-only corrections. Exact old slots are repaired wherever they are, and Charge admission counts at delivery. The result is still FAIL, only on the target-selection frame, which the harness's own `SelectUnit` dropped. |
| r03 (F2c) | FAIL: "the toolbar did not show the public control". | No reader change. A diagnostic read that skips only this check finds no other failure. It is a genuine product finding, fixed on F2d. |

Restoration on every run: Mods restored and Working restored. Save inventory digest is `60e3d6cc…a1` before and after, unchanged from F1's record.

Offline, on each candidate:
- Fast passed: unit tests 1068/0, plus source validation.
- Readers: F2 111/0, F2 branch 16/0, persistence contracts 190/0, 6B 10-GUID set.
- Candidate on F2c passed 12/0 (3.91 min, Test-Harness included).
- Fast on F2d passed 5/0.
- Exact request accepted by the live validator; an unlisted scenario was refused.
- Two independent read-only reviews. The first found one blocker (the removal cleanup save carried control rows), now fixed. The second found no blocker, and its should-fixes are fixed.

## Review-worthy risks / explicit remaining work

- **Selected, needs one start:** the native repaint confirmation on F2d. That is one approved real-time start of `finalize-f2-controls-rt`; its reader already checks the toolbar right after placement.
- **Not run natively:**
  - The save → cold-load round trip for kinds 6/7, and migration from a genuine preview.103 archive. The saved-kind mapping and archive validation are offline only.
  - A ready but unreviewed draft case `public-control-slots-manual` sits on local branch `codex/kmc-finalize-f2-persistence-draft` (`741af86a`). It needs a rebase, a review and two starts.
- **Known limitations:**
  - The selected-slot highlight does not show while a swapped shell is in target selection (cosmetic).
  - The hidden shells' own names appear in combat text ("Dismount", "Mount Companion").
  - An older KMC build reads an F2 archive with kinds 6/7 as invalid metadata; downgrade is unsupported.
  - Historical diagnostic scenarios that assume no automatic placement are not updated or rerun. Among them, Chunk7 exploration cases expect exactly four slot rows, and the six-pair case binds twelve Mount/Dismount slots.
- **F3 owns** the post-Charge movement policy and the action preview; neither was changed here.

## Spend and state

- **Budget:** `finalize-f2` 20.97 of 60 min; 3 of 3 real starts (r01–r03). No reset, increase or parallel allowance.
- **Checks run:** Fast ×6, Candidate ×1, package+suite ×4, WhatIf ×4, live-validator request check ×1.
- **Live state:** no game process, lock or installed candidate. Owner saves and foreign installations untouched. The live Mods tree is restored exactly to its intake (empty, as at F1).
- **Push:** the guarded push helper does not admit this branch and was not changed. The branch was pushed with an audited ordinary push: clean worktree, approved origin, new remote branch.

## Next decision

Approve one real-time start on F2d to confirm the repaint, or accept F2d with that limitation. Optionally approve two more starts for the persistence round trip. Small owner playcheck afterwards: put the control on the bar, mount, dismount, restart, then use the ordinary Charge both mounted and unmounted. No merge, release or install is assumed.
