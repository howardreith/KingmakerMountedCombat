# Chunk 7 finalization takeover — IN PROGRESS

The October 26 owner assignment continues the existing completion mission and
explicitly authorizes merging PR #5 after engineering qualification, independent
review, restoration and repository requirements pass. The selected endpoint is
**CHUNK 7 ENGINEERING COMPLETE — PR #5 MERGED — OWNER PLAY/VISUAL ACCEPTANCE PENDING**.
This supersedes earlier no-merge task instructions; their historical text remains
intact. Release/tag publication, permanent installation and human acceptance are
not selected. The full assignment was supplied in this conversation; the exact
named Markdown attachment was absent from `handoffs` at intake.

## Verified intake

- Machine: `DESKTOP-SRJJ623`. Restricted tools run as `CodexSandboxOnline`;
  approved elevated inspection runs as the existing `DESKTOP-SRJJ623\Howie`.
  No account, ACL, permission profile or security setting was changed.
- Actual successor worktree: `C:\Dev\KingmakerMountedCombatLab\repo\KingmakerMountedCombat`.
  Branch `codex/mounted-combat-chunk7-successor`, intake HEAD
  `dbd58bb27746539f08f1db36b16271ea7bbdda4d`, clean, nine unpublished commits.
  All existing frozen worktrees were preserved, including F1–F5 and b215.
- Fresh fetch and GitHub read: PR #5 open/draft, target `main`; remote review head
  `15f58fd7540b0a351f7dbd864f74c3ca24b97fe7`; main
  `d56ff670cca263da5197fd89f69cefd967e2d6b4` is already an ancestor of local HEAD.
  No integration merge is needed at intake. Recheck before final promotion.
- No Kingmaker/Wrath process, Claude process or native wrapper was running.
  The surviving PowerShell console belongs to the current Codex session.
  Steam PID 2972 remains untouched. No transaction lock; installed Mods empty.

## Interrupted pilot actually completed

`analysis-cache/chunk7-continuation/b215-identity.json`, `b215-runs-all.json`,
five `b215-native-*-exit.json` receipts and corresponding runtime transactions
establish r01, r02, r07, r08 and r11 PASS, exit 0, settled. Intake rehashed every
result successfully. The last profile snapshot was checked against actual Params,
PlayerPrefs and profile inventory: exact equality, zero admitted cache changes.
The run transactions report all protected-save/Working/baseline checks true and
restored digest `60e3d6cc271af65408d8b0d7db6c132ca4f0f42f38eba4dee46589a21db9c2a1`.
No recovery or lock deletion was performed.

Executed b215 identity:

- Source `ab3bbce2bee621018847ea78e9a008484d4b4e1d`, tree
  `7a457b9b6a9e5c99ec6bb8658d4fd3a60ec0c304`.
- Package `artifacts/KingmakerMountedCombat-0.1.0-chunk7b-preview.94-c7e-b215-pilots-diagnostic.zip`,
  SHA-256 `ed4d0a3ad75ba5a752c22ff4e2e61c7da8d5b0c1df20ec50636c6e2eb96aa2bf`.
- DLL `09552ecd82ae1458715abc32d4b1bd6bae857ed27d40d21116dc456176a548fa`,
  MVID `8819576f-49b6-47da-8910-f4e73537de8a`.
- Suite `chunk7e-b215-pilots-20261027`,
  SHA-256 `e3c1c9e0c9bca0458257bc72fa4c2dc44915a0584088463520c6d25638e22ab3`.

These results are development evidence for those exact bytes. They do not cover
the later spare-preference (`c01f0373`) or paired-travel (`db51def2`) changes.

## Correction and final-campaign state

All reported changes are committed: r11 native waits/sequential attacks
`8e8b9dec`; reference-stable subscriber serials `1d079c1e`; r07/r08 direction
search `d81c8e85`; interior range target `be9a6ee3`; optional spare `c01f0373`;
paired travelled record `db51def2`. The three other intake commits update the plan.
Independent review of the exact final delta and focused review of the preceding
corrections found the schema-admission gaps below. Both corrected sets received
independent cross-review PASS; earlier prose approval was not substituted for a receipt.

Review found evidence-schema downgrade paths. The corrected candidate is preview.95
so readers can require the new observations using the established DLL-bound product
revision discriminator while preserving preview.94 historical verdicts. Native
accounting, path/range assertions and all timing/performance limits stay unchanged.
The actual API is `RuntimeHelpers.GetHashCode`; its observed reuse is established,
while address reuse is a possible cause rather than proven unique causation. Source
comments now distinguish that inference. The travelled bearing uses the target
position observed when movement finishes; it does not independently prove the
search-time target direction.

Focused checks: repeated-world reader 166/0; RT persistence reader 90/0; staged
geometry actual-policy 482/0 and reader 42/0, including the full 64-candidate
spareless fallback and refusal when the primary path is invalid. The owned-pairs
reader passed 1462/0, exit 0, in 612.853 seconds. Review receipts are
`takeover-f6-prior-review.md` and `takeover-f6-delta-review-20261026.md` under
`analysis-cache/chunk7-continuation`; they separate authored corrections from
independent rereview. Full old-result replay with the .95 reader was refused by
the existing exact-product-version guard; focused historical subreader replays
and original frozen-reader verdicts are kept separate.

Repository inspection at intake: merge commits permitted; main has no branch
protection, required checks or branch rules; PR #5 has no review submissions or
review threads. This is a read-only snapshot, not permission to omit engineering
review. All repository conditions will be rechecked before promotion.

F5 is retained incomplete: 82 planned tags, 81 launches, 79 PASS, r11 and r27 FAIL,
r12 not run because its producer failed. F11 passed on F5 (r40 two-pair 0.2675 ms;
r39 two-pair 0.4115 ms, six-pair 0.494 ms); no final credit transfers to changed bytes.
Raw FULL remains FAIL at the classified SupportingEvidence check. The remaining
inventory had 243 PASS/6 classified FAIL; all current checks passed. Exact receipts
are `final5-ledger.json`, `final5-f11.json`, `final5-full-classification.json` and
`final5-progress.md` under `analysis-cache/chunk7-continuation`.

No F6 final freeze exists at intake. The unbuilt b216 worktree at `dbd58bb2` was
created before the schema-review findings and is retained unused. Finish the
pilot successor below. Run
only required changed paths with valid existing fixture lineage; finish the pilot
gate before publication and the single final freeze. The existing final matrix
and run plan govern all final rows, exclusions, performance and raw offline outcomes.

## b217 preflight correction

The reviewed changes were committed as `9f8ebc69eaa1d855dbbd963faa89897e1797d49c`.
b217's isolated package attempt stopped before compilation with source validation
148/1: `BuildIdentity.ProductVersion` still read .94 after the Info/version bump.
Its worktree and `b217-package-01-exit.json` (exit 1) remain intact; there was no
native launch or package. The compiled constant is now .95 too. The existing
source identity regression passes 149/0 (`takeover-version-source-check-01.log`),
and a focused independent identity rereview is requested. The next pilot is b218.
