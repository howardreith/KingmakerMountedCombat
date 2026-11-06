# F2 review handoff

## Disposition

**F2: PARTIAL — READY FOR REVIEW; OWNER ACCEPTANCE SEPARATE.**

Demonstrated natively on candidate F2f (run `20261105-finalize-f2-r05`, PASS, external reader included):
- **Repaint:** the first automatic placement was drawn on the toolbar without any other refresh.
- **Legacy input:** the authentic preview.211 archive's old Dismount row was migrated in place.
- **Control cycle:** Dismount → Mount ran through the public slot.
- **Native saves:** two native Manual saves carry exactly the kind-6/7 rows of a kept and of a removed control.

**Not demonstrated:** the separate-process reload of those saves (the consumer).
- Consumer run `20261105-finalize-f2-r06` refused itself at the isolation bootstrap, before any native load. Its enumeration check still expected one save in the isolated root.
- The gap is fixed offline at `8b610af1` and has not run.
- All six authorized starts are used, so restoration of the kept slot, its drawing, the cycle after load and the remembered removal across a load remain **NOT RUN** natively.

Nothing was merged, tagged, released or installed. No owner test is requested for this engineering gap.

## Identities

- Repository howardreith/KingmakerMountedCombat. Branch `codex/kmc-finalize-f2-canonical-controls`. Base `66e96f90` (F1 merge). Reviewed head `befbd73c`. Current head `38e599e6`.
- **F2f, the native candidate of r05/r06:**
  - source `ff1f2e5a1200b3f2ecfe3be04304a08768e9e280`, version 0.1.0-chunk7b-preview.103
  - DLL SHA256 `9e4180bacd6258a2d0a450d13faeb810b5ced2df82265eb5f92460fed2cbe81e`
  - package lab `artifacts/KingmakerMountedCombat-0.1.0-chunk7b-preview.103-finalize-f2f-diagnostic.zip`, SHA256 `2b2c9699ffeec28cf2120e16e39d7f279d1fc379e51aef77d29e5ad0e61c68b2`
  - suite `finalize-f2f-ff1f2e5a-20261105`, SHA256 `beafa8dd9bfd185982617e40ce5a90a11dc43370b2f58ddf45694921f5ba4964`
- **F2e, r04:** source `a44ead69`, DLL `735f4c8e…`, MVID `c69efc86-7c09-4bcf-b473-f9a2fc6c0caa`, package `f9109825…`, suite `finalize-f2e-a44ead69-20261105` (`be1b94eb…`).
- **F2d** (`7879e463`, DLL `70b55746…`) never ran.
  - Production code: F2d → F2f differs only in `CompositionRoot.cs`, where automation runs of this route do not select the retired helpers (ordinary-play semantics). Every other change is Diagnostics, harness, tests or docs.
  - F2e/F2f therefore carry F2d's repaint fix unchanged.
- **Since F2f, not packaged:** `8b610af1` (consumer enumeration) and `38e599e6` (the capture closes the mod-manager window). Both are diagnostics only.
- **Environment:**
  - Kingmaker 2.1.7b.
  - Base-game-only KMC staging with the archive's retained dependencies (CotW 1.14.4c, CraftMagicItems; `analysis-cache/sprint1/retained-dependencies-final211.json`).
  - Real-time Working-fixture lineage: rider `b6628a77…` with Mammoth `d79a4f6c…`.

## Route chosen

- **Old-toolbar input:** the earlier handoff said a genuine preview.103 archive had been examined offline. That was inaccurate: none had been loaded.
- **Scan:** a read-only scan of the retained lab archives found authentic old-toolbar rows only from released preview.211 and from Sprint 7–9 development builds. No kind 1 or kind 5 row exists anywhere.
- **Archive chosen:** the released preview.211 Quick `runtime-staging/persistence-final211-p05-save-quick/Saved Games/Quick_1.zks`, SHA256 `83e92e1a…`.
  - Contents: schema 2, mounted; Dismount at 14 (kind 2) and the retired rider primary at 15 (kind 3).
  - Sprint 9 had already loaded it natively on the guarded UPG-01 chain (e06/e07 PASS).
- **Why the P05 chain:**
  - Only one eligible actor exists in that world, so the mission's one-actor shape applies: the producer writes two declared Manuals (kept and removed), and one separate consumer process loads both.
  - The P05 chain already admits a cross-candidate archive and has the alternating two-archive pattern.
  - The unreviewed local draft (`codex/kmc-finalize-f2-persistence-draft`, `741af86a`, Chunk 7 two-rider case) was reviewed and **not integrated**. It has no authentic old input, and its first rider's full main bar would have failed its rearrangement. Its branch and identity are untouched.
- **Mode:** the F2 mode is `finalize2Controls=true`, admitted only beside `upgradeChain=true` by both validators.
  - **Producer** (P05 Quick cold load): records archive rows against live slots. Then, as disclosed setup:
    1. removes the migrated control;
    2. empties main-bar slot 13 and waits until the bar draws it empty;
    3. checks that native autofill is on;
    4. calls `BeginLoadedWorld` (the r03 recipe for a first-placement event);
    5. reads the visible bar every frame;
    6. cycles Dismount → Mount through the slot's native `OnClick`, waiting for the native slot to admit each click;
    7. swaps the control into slot 9;
    8. saves `KMC_P01`;
    9. removes the control;
    10. saves `KMC_F2_REMOVED`.
  - **Consumer** (P05 Manual cold load, new process):
    1. cold-loads the kept archive and reads the restored, drawn slot before any input;
    2. runs the ordinary continuation;
    3. cycles the control;
    4. loads the removed archive in-process, then watches 60 update frames, a reselection through another party member and a drawer-entry Dismount/Mount.

## Native outcomes, each at its original verdict

| Run | Candidate | Original verdict | What it showed |
|---|---|---|---|
| r01 | F2a `c19dcc44` | FAIL | Full fixture bar; no slot for the cycle (experiment changed). |
| r02 | F2b `5017fe0e` | FAIL | Re-evaluation with two reader-only corrections: still FAIL (the harness's own selection dropped the Charge selection frame). |
| r03 | F2c `66bed248` | FAIL | Toolbar not repainted after placement: product defect, fixed in F2d. |
| r04 | F2e `a44ead69` | **FAIL** (39 pass, 1 fail) | Passing sub-observations, kept: authentic legacy row migrated in place (14/2 → 14/6 + 0/7, one repair, 15 cleared); in-session removal stayed removed for 30 frames; **repaint**: slot 13 drawn empty, placement at frame 4601, control drawn at 4602; Dismount from the slot. Failed at the Mount click: Dismount had spent the rider's native Move action in real-time combat (2.82 s left), so the slot correctly reported Mount not possible 10 frames later. Experiment timing, corrected in F2f. |
| r05 | F2f `ff1f2e5a` | **PASS** | Everything in r04, plus Mount after a 2.18 s wait for the native Move cooldown (one exact Mount shell). Kept Manual `7b0ac4bc…` = {6@9, 7@0}; removed Manual `b323e108…` = {7@0}. No native member names a KMC control identity (7 members scanned). Producer root held exactly the input and the two outputs; input unchanged. |
| r06 | F2f `ff1f2e5a` | **FAIL** | Refused at the isolation bootstrap ("Isolated native enumeration differs from the single copied fixture"). No load, no write, no F2 observation. Harness gap; fixed at `8b610af1`, not run. |

Restoration after r04, r05 and r06:
- Mods and Working were restored and the baseline was immutable.
- Save-protection and write-allowlist checks passed.
- The save inventory digest stayed `60e3d6cc…a1`, the same as F1's record.

## Visual packet (private, not committed)

- r04/r05 captures under lab `runtime-evidence/20261105-finalize-f2-r05/movement-visuals/` (`finalize-f2-p05-placed.png`, `finalize-f2-p05-removed.png`):
  - The mod-manager window, which base-game staging opens at start, covers the action bar, so these frames do **not** show the toolbar.
  - The repaint claim rests on the structured visible-bar reads: the ActionBarManager's bound slot objects, empty at 13 before and the public control at 13 after.
  - The capture now closes that window for its frame (`38e599e6`, not run).
- The r03 frames, `runtime-evidence/20261105-finalize-f2-r03/movement-visuals/`, still show the icon in the bar and in target selection.
- Owner art approval is separate.

## Offline checks

- **Candidate** on `a8dd5875`: 19/0 (4.48 min), including Test-Harness and the focused list below.
- **Fast** with focused readers on each later commit: 7/0, 5/0, 5/0, 5/0, 4/0.
- **F2 upgrade reader self-test:** 57/0. Producer and consumer positives with real files, targeted negatives for each decisive fact, and source contracts for:
  - the removal-cleanup suppression of control rows;
  - the request flags;
  - the isolated entries and quotas;
  - the two-archive consumer enumeration.
- **Kind-6/7 codec regression** in `tests/Chunk7PersistenceTests.cs` (production `MountedArchiveCodec`; Test-PersistenceData 91/0):
  - round-trips slot 0 beside its record, a record-only removed control, and old aliases 1/2/5;
  - refuses a second record, two rows in one slot, a record off index 0, index 128 and an unknown kind.
- **Seven persistence test compile lists repaired.** F2 had left them without `MountedControlSlotPolicy.cs`, so Test-PersistenceData would not compile; that was a regression the first F2 review missed. All pass:
  - Test-PersistenceData 85/0 + 91/0
  - Test-Chunk7CombatDebtDiagnostics 61/0
  - Test-Chunk7CurrentMigrationNative 57/0
  - Test-Chunk7LoadRefusalActualGuards 71/0
  - Test-Chunk7OwnedSchema1Native 187/0
  - Test-Chunk7Schema2Resave 41/0
  - Test-Chunk7CurrentRemovalArchive 45/0, only with `-RepositoryRoot` at the worktree, since its default reads the main checkout.
- **Live request validators** (external `Test-RuntimeRequest.ps1` and compiled `RuntimeRequest.Validate`):
  - The exact producer requests (F2e, F2f) and the exact consumer request against r05's real archives were all admitted.
  - Refused, each by both validators: an unlisted scenario, a foreign branch, F2 without the upgrade chain, a producer with a second archive, a consumer without the removed archive, a wrong removed leaf, aliased archives.
- **WhatIf:** w05, w06 and w07 PASS.
- **Independent read-only reviews:**
  - The `befbd73c..a8dd5875` delta: no blocker. Three should-fixes, all applied at `a44ead69`: autofill precondition, a real reselection, and the mounted-row invariant. Two hardening notes, also applied: pin the authentic input; require a PASS producer.
  - The r04 timing fix: no blocker. Should-fixes applied at `ff1f2e5a`: a 15 s real-time bound, the recorded wait cause, and the reader requiring a native-admitted click and a Move cooldown to explain any wait.
  - **Neither review caught the r06 enumeration gap.**

## Remaining limitations

- **Not run natively:**
  - The separate-process reload: restored kept slot, its drawing, the cycle after load, the removed control staying removed across a load, ordinary Charge and strings after the cold load.
  - Old kinds 1 and 5: no authentic archive carries them; offline coverage only.
- **Downgrade:** an older KMC reads an F2 archive with kinds 6/7 as invalid metadata. Rollback is unsafe for F2-written saves; keep pre-F2 backups and the matching older package.
- **Identity:** several development builds share the label preview.103; only the full source/DLL/package identity names a candidate.
- **Cosmetic:** the selected-slot highlight is not shown while a swapped shell is in target selection. Hidden shell names appear in combat text.
- **Historical diagnostic scenarios** that assume no automatic placement were not updated.
- **F3 owns** post-Charge movement and the action preview.

## Spend and state

- **Budget:** `finalize-f2` 37.22 of 60 min. Starts 6 of 6: r01–r03 earlier; r04, r05, r06 on this mission's added three.
  - The start cap is not enforced in repository code, so no budget code changed.
  - No time increase, reset or new budget id.
- **Live state:** no game process, lock or installed candidate; the live Mods tree is empty, as at intake.
- **Push:** audited ordinary push (the guarded helper does not admit this branch and was not changed).

## Next decision

The consumer needs native starts the current authority does not cover. The fixed consumer has not run, so a further failure is possible. Options:
- **(Recommended) Two more starts on a new package of the current head:** producer + consumer, readers unchanged.
  - Fits the remaining 22.8 minutes of the same 60-minute ceiling: package about 1.4 min, two WhatIfs about 0.6 min, two launches of about 2 min each, each with its 10-minute admission reservation.
- **One start:** a consumer on a new package of the current head, against r05's F2f outputs. This needs an owner-approved, narrowly pinned exception to the reader's same-candidate rule; production source between F2f and the head is identical, and only Diagnostics differ.
- Or accept F2 with the reload unrun.
